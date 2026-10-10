"""Offline combat research; reads decoded frames, never captures or modifies the game.

Protocol references: SkeeveAN/Aion-DPS-Meter (MIT), see THIRD-PARTY.md
and protocol/LICENSE-MIT.txt. Output contains private character names.
Only observed layouts are accepted. Periodic totals require an explicit research
opt-in and remain candidates pending comparison with on-screen damage.
"""
import argparse
import collections
import json
import pathlib
import struct
import uuid

MAX_AMOUNT = 100_000_000
MAX_IDENTITIES = 16384


class DecodeError(ValueError):
    pass


def uint(body, pos):
    value = 0
    for shift in range(0, 35, 7):
        if pos >= len(body):
            raise DecodeError('truncated integer')
        byte = body[pos]
        pos += 1
        value |= (byte & 127) << shift
        if not byte & 128:
            if value > 0xffffffff:
                break
            return value, pos
    raise DecodeError('overflow integer')


def name_at(body, pos):
    if pos >= len(body) or not 2 <= body[pos] <= 24:
        return None
    end = pos+1+body[pos]
    if end > len(body):
        return None
    try:
        name = body[pos+1:end].decode('utf-8')
    except UnicodeDecodeError:
        return None
    return name if name.isalnum() else None


def party_members(body):
    """Observed global-client 0092 roster / 0D92 join member records.

    UUIDs are used only to validate the record shape and are never exported.
    The observed leave snapshot contains only the local character.
    Unknown/empty layouts are not interpreted as a roster reset.
    """
    if body[:2] not in (b'\x00\x92', b'\x0d\x92') or len(body) > 65536:
        return []
    members = []
    for p in range(2, len(body)-64):
        # actor u32, three matching server fields around UUID/database fields.
        if body[p+8] != 36:
            continue
        actor, server, repeated = struct.unpack_from('<IHH', body, p)
        if not actor or not 1 <= server <= 9999 or repeated != server:
            continue
        if struct.unpack_from('<H', body, p+51)[0] != server:
            continue
        try:
            uuid.UUID(body[p+9:p+45].decode('ascii'))
        except (ValueError, UnicodeDecodeError):
            continue
        name = name_at(body, p+53)
        end = p+54+body[p+53]
        if not name or end+4 > len(body):
            continue
        class_code = struct.unpack_from('<I', body, end)[0]
        if class_code % 4 not in (1, 2) or not 1 <= class_code//4 <= 9:
            continue
        members.append(dict(actor=actor, name=name, server=server, classCode=class_code))
        if len(members) > 12 or len({m['actor'] for m in members}) != len(members):
            return []
    if body[:2] == b'\x0d\x92' and len(members) != 1:
        return []
    return members


def direct(body, timestamp):
    if body[:2] != b'\x04\x38' or len(body) > 65536:
        raise DecodeError('unsupported frame')
    target, p = uint(body, 2)
    flags, p = uint(body, p)
    _, p = uint(body, p)
    source, p = uint(body, p)
    if not flags & 4:
        return None  # Skill companion notice, not another hit.
    if flags & 15 not in (4, 6):
        raise DecodeError('unverified layout')
    if p+6 > len(body):
        raise DecodeError('missing skill')
    skill = struct.unpack_from('<I', body, p)[0]
    p += 5  # Four-byte skill, one-byte per-hit identifier.
    hit_type, p = uint(body, p)
    markers = [i for i in range(p, len(body)-3)
               if 1 <= body[i] <= 9 and body[i+1:i+4] == b'\0\0\0']
    if len(markers) != 1:
        raise DecodeError('ambiguous damage block')
    scalar, p = uint(body, markers[0]+4)
    amount, p = uint(body, p)
    if not 0 < amount <= MAX_AMOUNT or not source or not target:
        raise DecodeError('invalid amount or actor')
    extras = []
    if flags & 32:
        count, p = uint(body, p)
        if not 1 <= count <= 25:
            raise DecodeError('invalid additional-hit count')
        for _ in range(count):
            value, p = uint(body, p)
            extras.append(value)
        if sum(extras) >= amount:
            raise DecodeError('invalid additional-hit amounts')
    return dict(timestamp=timestamp, source=source, target=target, skill=skill,
                amount=amount, primaryAmount=amount-sum(extras),
                additionalAmounts=extras, critical=hit_type == 3,
                scalar=scalar, remainingBytes=len(body)-p)


def periodic(body, timestamp):
    """Decode observed 0538 fields without treating their presence as damage.

    Bit 1 carries remaining/pending effect magnitude, bit 2 the current impact.
    Potions and support effects use this same envelope. Unknown flags fail closed.
    """
    if body[:2] != b'\x05\x38' or len(body) > 128:
        raise DecodeError('unsupported periodic frame')
    target, p = uint(body, 2)
    if p >= len(body):
        raise DecodeError('missing periodic flags')
    flags = body[p]
    p += 1
    if flags not in (0, 1, 2, 8, 9, 10, 11):
        raise DecodeError('unverified periodic layout')
    source, p = uint(body, p)
    instance, p = uint(body, p)
    if p+4 > len(body):
        raise DecodeError('missing periodic effect')
    effect = struct.unpack_from('<I', body, p)[0]
    p += 4
    pending, amount, skill = None, None, 0
    if flags & 1:
        pending, p = uint(body, p)
    if flags & 2:
        amount, p = uint(body, p)
    if flags & 8:
        if p+4 > len(body):
            raise DecodeError('missing periodic skill')
        skill = struct.unpack_from('<I', body, p)[0]
        p += 4
    if p != len(body) or not source or not target:
        raise DecodeError('invalid periodic boundary or actor')
    if any(value is not None and value > MAX_AMOUNT for value in (pending, amount)):
        raise DecodeError('invalid periodic magnitude')
    return dict(timestamp=timestamp, source=source, target=target, flags=flags,
                effect=effect, instance=instance, pendingAmount=pending,
                amount=amount, skill=skill, additionalAmounts=[], critical=False)


def effect_parent(body):
    """Narrow, independently checked global 4136 effect-spawn layout.

    Accepts the two observed Sorcerer effect NPC types only. The parent is a
    structural u32 field, not a byte-pattern/name/class guess. Other spawn layouts
    stay unassigned. All fixed fields and the complete trailer must be present.
    """
    if body[:2] != b'\x41\x36' or len(body) > 512:
        raise DecodeError('unsupported effect spawn')
    actor, p = uint(body, 2)
    if p+9 > len(body):
        raise DecodeError('truncated effect header')
    outer, description, code, state = struct.unpack_from('<HBIH', body, p)
    p += 9
    if (outer, description, state) != (31, 0, 576) or code not in (2920011, 2920551):
        raise DecodeError('unverified effect spawn layout')
    # Position/state bytes, one state byte, then two observed u32 HP varints.
    p += 19
    hp, p = uint(body, p)
    maximum, p = uint(body, p)
    if hp > maximum or not maximum:
        raise DecodeError('invalid effect health')
    p += 54  # Fixed 52-byte state and two selected one-byte state fields.
    count, p = uint(body, p)
    if count != 1 or p >= len(body) or body[p] != 17:
        raise DecodeError('unverified effect status layout')
    p += 1
    _, p = uint(body, p)  # Status instance.
    if p+20 > len(body):
        raise DecodeError('truncated effect status')
    p += 20  # Status code and two timestamps.
    status_caster, p = uint(body, p)
    if status_caster != actor or p+17 > len(body):
        raise DecodeError('invalid effect status actor')
    p += 14  # Level, packed Boolean bank, position (12 bytes).
    p += 3  # Three outer fields; its Boolean shares the status Boolean bank.
    if p+4 > len(body):
        raise DecodeError('missing effect parent')
    parent = struct.unpack_from('<I', body, p)[0]
    p += 4
    trailer_count, p = uint(body, p)
    if trailer_count != 2 or p+16 >= len(body):
        raise DecodeError('unverified effect trailer')
    p += 16  # Two six-byte entries and one u32 field.
    last_count, p = uint(body, p)
    if last_count or p != len(body) or not actor or not 0 < parent <= 0x7fffffff or parent == actor:
        raise DecodeError('invalid effect parent or boundary')
    return actor, parent, code


def rows(path):
    with pathlib.Path(path).open(encoding='utf-8-sig') as source:
        for line in source:
            try:
                row = json.loads(line)
                body = bytes.fromhex(row['hex'])
                timestamp = float(row['ts'])
            except (ValueError, KeyError, TypeError):
                continue
            yield body, timestamp


def identities(path):
    names, monsters, conflicted = {}, set(), set()
    for body, _ in rows(path):
        try:
            for member in party_members(body):
                actor, name = member['actor'], member['name']
                if actor in names and names[actor] != name:
                    conflicted.add(actor)
                if len(names) < MAX_IDENTITIES:
                    names[actor] = name
            actor = None
            name = None
            if body[:2] == b'\x04\x8d':
                _, p = uint(body, 2)
                actor, p = uint(body, p+4)
                name = name_at(body, p+2)
            elif body[:2] == b'\x33\x36':
                actor, p = uint(body, 2)
                for i in range(p, min(len(body)-12, p+16)):
                    candidate = name_at(body, i)
                    if not candidate:
                        continue
                    end = i+1+body[i]
                    code = struct.unpack_from('<I', body, end+2)[0]
                    if body[end+6] == 1 and code % 4 in (1, 2) and 1 <= code//4 <= 9:
                        name = candidate
                        break
            elif body[:2] == b'\x41\x36':
                actor, p = uint(body, 2)
                if p+3 <= len(body) and body[p+2] in (0, 1) and len(monsters) < MAX_IDENTITIES:
                    monsters.add(actor)
            if actor and name and len(names) < MAX_IDENTITIES:
                if actor in names and names[actor] != name:
                    conflicted.add(actor)
                names[actor] = name
        except (DecodeError, IndexError, struct.error):
            continue
    for actor in conflicted:
        names.pop(actor, None)  # Do not silently join reused entity identifiers.
    return names, monsters, conflicted


def analyze(path, self_name=None, party=(), idle_seconds=10, candidate_periodic=False):
    names, monsters, conflicts = identities(path)
    selected_party = set(party)
    if self_name:
        selected_party.add(self_name)
    counts = collections.Counter()
    detected_party = collections.deque(maxlen=64)
    current_party_ids = None
    encounters = collections.deque(maxlen=32)
    current = None
    previous = None
    owners = {}
    for body, timestamp in rows(path):
        if body[:2] == b'\x41\x36':
            try:
                effect, parent, code = effect_parent(body)
                if parent in names and effect not in names and len(owners) < MAX_IDENTITIES:
                    owners[effect] = (parent, timestamp)
                    counts['observed_effect_parent_links'] += 1
            except DecodeError:
                # A different/reused spawn must never retain an old parent.
                try:
                    actor_id, _ = uint(body, 2)
                    owners.pop(actor_id, None)
                except DecodeError:
                    pass
        members = party_members(body)
        if members:
            opcode = body[:2].hex().upper()
            detected_party.append(dict(timestamp=timestamp, opcode=opcode, members=members))
            if body[:2] == b'\x0d\x92' and current_party_ids is not None:
                current_party_ids = current_party_ids | {member['actor'] for member in members}
                if current is not None:
                    current['partyActors'] = current_party_ids
            elif self_name and self_name in {member['name'] for member in members}:
                current_party_ids = {member['actor'] for member in members}
                if current is not None:
                    current['partyActors'] = current_party_ids
        is_periodic = body[:2] == b'\x05\x38'
        if not is_periodic and body[:2] != b'\x04\x38':
            continue
        try:
            event = periodic(body, timestamp) if is_periodic else direct(body, timestamp)
        except DecodeError as ex:
            counts[str(ex)] += 1
            continue
        if event is None:
            counts['companion_notices_excluded'] += 1
            continue
        if is_periodic:
            # Observed Fire Wall family only; presence of an amount is not proof.
            if (event['flags'] != 10 or event['skill'] != 15390002 or
                    event['effect'] not in (1539000011, 1539000012) or not event['amount']):
                counts['other_periodic_effects_pending_validation'] += 1
                continue
            counts['candidate_fire_wall_ticks'] += 1
            if not candidate_periodic:
                counts['periodic_frames_pending_validation'] += 1
                continue
        if event['source'] == event['target']:
            counts['self_effects_excluded'] += 1
            continue
        if event['skill'] == 11000100:
            counts['dodges_excluded'] += 1
            continue
        raw_source = event['source']
        owner, spawned = owners.get(raw_source, (None, None))
        if owner is not None and 0 <= timestamp-spawned <= 120:
            event['source'] = owner
        source_name = names.get(event['source'])
        if source_name is None:
            counts['unattributed_events'] += 1
            continue
        if event['target'] not in monsters or event['target'] in names or event['target'] in owners:
            counts['unverified_target_events'] += 1
            continue
        if not 11 <= event['skill']//1_000_000 <= 19:
            counts['non_class_effects_pending_validation'] += 1
            continue
        if previous is not None and timestamp < previous:
            counts['out_of_order_timestamp'] += 1
            continue
        if current is None or timestamp-previous >= idle_seconds:
            current = dict(start=timestamp, end=timestamp, actors={}, partyActors=current_party_ids)
            encounters.append(current)
        current['end'] = timestamp
        previous = timestamp
        actors = current['actors']
        if event['source'] not in actors and len(actors) >= 512:
            counts['actor_limit_exceeded'] += 1
            continue
        actor = actors.setdefault(event['source'], dict(name=source_name, damage=0, records=0, displayedHits=0, criticalRecords=0,
                                                       directDamage=0, candidatePeriodicDamage=0, candidatePeriodicRecords=0))
        actor['damage'] += event['amount']  # Additional impacts are already in this total.
        actor['records'] += 1
        actor['displayedHits'] += 1+len(event['additionalAmounts'])
        actor['criticalRecords'] += int(event['critical'])
        if is_periodic:
            actor['candidatePeriodicDamage'] += event['amount']
            actor['candidatePeriodicRecords'] += 1
            counts['included_candidate_periodic_records'] += 1
        else:
            actor['directDamage'] += event['amount']
            counts['accepted_direct_records'] += 1
        if raw_source != event['source']:
            counts['parent_attributed_records'] += 1
    results = []
    for encounter in encounters:
        duration = max(1., encounter['end']-encounter['start'])
        actors = [dict(v, actor=k, encounterDps=v['damage']/duration) for k,v in encounter['actors'].items()]
        actors.sort(key=lambda actor: actor['damage'], reverse=True)
        results.append(dict(start=encounter['start'], end=encounter['end'], durationSeconds=duration,
                            globalVisible=actors,
                            self=[actor for actor in actors if actor['name'] == self_name],
                            party=[actor for actor in actors if
                                   (actor['actor'] in encounter['partyActors'] if encounter['partyActors'] is not None
                                    else actor['name'] in selected_party)],
                            partyBasis='Observed 0092 snapshot' if encounter['partyActors'] is not None else 'Manual fallback'))
    return dict(experimental=True, completeDamage=False,
                candidatePeriodicIncluded=candidate_periodic,
                warning='Incomplete research: Fire Wall ticks are candidates pending visual validation and included only by opt-in. Explicit parent links cover two observed Sorcerer effects; other summons/effects remain unverified. Party join/leave observed in one global-client session; other roster layouts remain unverified. Global means identified players observed by this client. HP scaling is unresolved. Names are private.',
                denominator='Shared encounter duration, minimum one second; new encounter after idle gap.',
                idleSeconds=idle_seconds, counts=dict(counts), conflictingIdentities=len(conflicts),
                partyMembership='Observed 0092 snapshots containing self when available; manual fallback before discovery',
                detectedPartySnapshots=list(detected_party), encounters=results)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('frames', type=pathlib.Path)
    parser.add_argument('--self', dest='self_name')
    parser.add_argument('--party', nargs='*', default=[])
    parser.add_argument('--idle', type=float, default=10)
    parser.add_argument('--candidate-periodic', action='store_true', help='Include unverified Fire Wall tick candidates in research totals')
    parser.add_argument('--output', type=pathlib.Path, required=True)
    args = parser.parse_args()
    if not 1 <= args.idle <= 120:
        parser.error('--idle must be between 1 and 120 seconds')
    report = analyze(args.frames, args.self_name, args.party, args.idle, args.candidate_periodic)
    args.output.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
    print(json.dumps(dict(counts=report['counts'], encounters=len(report['encounters']), experimental=True)))


if __name__ == '__main__':
    main()

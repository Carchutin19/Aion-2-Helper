"""Synthetic protocol tests. No real packet captures or character names."""
import json
import pathlib
import struct
import tempfile
import unittest

from analyze_combat import DecodeError, analyze, direct, effect_parent, party_members, periodic, uint

TEST_ROOT = pathlib.Path(__file__).resolve().parents[1]/'.local-tools'
TEST_ROOT.mkdir(exist_ok=True)


def var(value):
    result = bytearray()
    while value >= 128:
        result.append((value & 127) | 128)
        value >>= 7
    result.append(value)
    return bytes(result)


def hit(source=42, target=99, amount=1263, extras=(), scalar=10817):
    flags = 6 | (32 if extras else 0)
    block = b'\x80\0\x01\x55\x66\x77\x88'+struct.pack('<I', 1)
    tail = var(len(extras))+b''.join(var(v) for v in extras) if extras else b''
    return b'\x04\x38'+var(target)+var(flags)+b'\0'+var(source)+struct.pack('<I', 15210000)+b'\x01\x02'+block+var(scalar)+var(amount)+tail+b'\x01\0'


def identity(actor, name):
    name = name.encode('utf-8')
    return b'\x04\x8d\x01'+b'\0'*4+var(actor)+b'\x01\0'+bytes([len(name)])+name


def tick(source=98, target=99, flags=10, amount=194, pending=1000,
         effect=1539000011, skill=15390002):
    b = b'\x05\x38'+var(target)+bytes([flags])+var(source)+var(1)+struct.pack('<I', effect)
    if flags & 1:
        b += var(pending)
    if flags & 2:
        b += var(amount)
    if flags & 8:
        b += struct.pack('<I', skill)
    return b


def effect_spawn(actor=98, parent=42, hp=1000, code=2920551):
    b = b'\x41\x36'+var(actor)+struct.pack('<HBIH', 31, 0, code, 576)
    b += b'\0'*19+var(hp)+var(hp)+b'\0'*54+var(1)
    b += b'\x11'+var(1)+struct.pack('<IQQ', 1, 0, 0)+var(actor)
    b += b'\x01\x02'+b'\0'*12+b'\0'*3+struct.pack('<I', parent)
    return b+var(2)+b'\0'*12+struct.pack('<I', 0)+var(0)


def party_member(actor, name):
    name = name.encode('utf-8')
    return struct.pack('<IHH', actor, 1301, 1301)+b'\x24'+b'00000000-0000-0000-0000-000000000000'+b'\0'*6+struct.pack('<H', 1301)+bytes([len(name)])+name+struct.pack('<I', 26)+b'\0'*12


class CombatTests(unittest.TestCase):
    def test_periodic_current_and_remaining_are_distinct(self):
        e = periodic(tick(flags=11, pending=1682, amount=420, skill=2011101), 1.)
        self.assertEqual((e['pendingAmount'], e['amount']), (1682, 420))
        e = periodic(tick(flags=9, pending=289, skill=18730002), 1.)
        self.assertEqual((e['pendingAmount'], e['amount']), (289, None))
        e = periodic(tick(), 1.)
        self.assertEqual((e['amount'], e['pendingAmount']), (194, None))

    def test_periodic_boundaries_and_unobserved_flags_fail_closed(self):
        b = tick()
        for end in range(len(b)):
            with self.assertRaises(DecodeError):
                periodic(b[:end], 1.)
        for malformed in (b+b'\0', tick(flags=14), tick(flags=128), tick(amount=100_000_001)):
            with self.assertRaises(DecodeError):
                periodic(malformed, 1.)

    def test_parent_comes_from_full_structural_spawn(self):
        for actor, parent, hp in ((98, 42, 1000), (50000, 12000, 100000)):
            b = effect_spawn(actor, parent, hp)
            self.assertEqual(effect_parent(b), (actor, parent, 2920551))
            for end in range(len(b)):
                with self.assertRaises(DecodeError):
                    effect_parent(b[:end])
        for malformed in (effect_spawn(parent=98), effect_spawn(parent=0),
                          effect_spawn(code=1234), effect_spawn()+b'\0'):
            with self.assertRaises(DecodeError):
                effect_parent(malformed)

    def test_parent_attribution_and_periodic_opt_in_exclude_support(self):
        frames = [(identity(42, 'Alpha'), 0.), (identity(43, 'Beta'), 0.),
                  (b'\x41\x36'+var(99)+b'\0\0\0'+struct.pack('<I', 12345), 0.),
                  (effect_spawn(), 1.), (hit(source=98, amount=500), 2.),
                  (tick(), 3.), (tick(amount=235, effect=1539000012), 4.),
                  (tick(flags=11, pending=1000, amount=420, skill=2011101), 5.),
                  (tick(source=43, flags=10, skill=18730002), 6.),
                  (tick(skill=17080340, effect=1708034011), 7.),
                  (tick(target=43), 8.),
                  (tick(), 122.)]  # Parent attribution expires, not guessed.
        with tempfile.TemporaryDirectory(prefix='combat-test-', dir=TEST_ROOT) as folder:
            path = pathlib.Path(folder)/'frames.jsonl'
            path.write_text(''.join(json.dumps(dict(hex=b.hex(), ts=t))+'\n' for b,t in frames), encoding='utf-8')
            direct_only = analyze(path, 'Alpha')
            candidate = analyze(path, 'Alpha', candidate_periodic=True)
        self.assertEqual(direct_only['encounters'][0]['self'][0]['damage'], 500)
        row = candidate['encounters'][0]['self'][0]
        self.assertEqual((row['damage'], row['directDamage'], row['candidatePeriodicDamage']), (929, 500, 429))
        self.assertEqual(row['candidatePeriodicRecords'], 2)
        self.assertEqual(candidate['counts']['other_periodic_effects_pending_validation'], 3)
        self.assertEqual(candidate['counts']['unattributed_events'], 1)
        self.assertEqual(candidate['counts']['unverified_target_events'], 1)
        self.assertFalse(candidate['completeDamage'])

    def test_reused_effect_spawn_clears_ownership(self):
        frames = [(identity(42, 'Alpha'), 0.),
                  (b'\x41\x36'+var(99)+b'\0\0\0'+struct.pack('<I', 12345), 0.),
                  (effect_spawn(), 1.), (hit(source=98, amount=100), 2.),
                  (effect_spawn(code=1234), 3.), (hit(source=98, amount=200), 4.)]
        with tempfile.TemporaryDirectory(prefix='combat-test-', dir=TEST_ROOT) as folder:
            path = pathlib.Path(folder)/'frames.jsonl'
            path.write_text(''.join(json.dumps(dict(hex=b.hex(), ts=t))+'\n' for b,t in frames), encoding='utf-8')
            report = analyze(path, 'Alpha')
        self.assertEqual(report['encounters'][0]['self'][0]['damage'], 100)
        self.assertEqual(report['counts']['unattributed_events'], 1)

    def test_global_party_identity_and_shape_validation(self):
        name = b'Alpha'
        member = struct.pack('<IHH', 42, 1301, 1301)+b'\x24'+b'00000000-0000-0000-0000-000000000000'+b'\0'*6+struct.pack('<H', 1301)+bytes([len(name)])+name+struct.pack('<I', 26)+b'\0'*12
        body = b'\x00\x92\x08'+member
        self.assertEqual(party_members(body), [dict(actor=42, name='Alpha', server=1301, classCode=26)])
        self.assertEqual(party_members(b'\x01\x97'+body[2:]), [])
        self.assertEqual(party_members(body[:55]), [])
        corrupted = bytearray(body)
        corrupted[3+6] = 99
        self.assertEqual(party_members(corrupted), [])
        corrupted = bytearray(body)
        corrupted[3+9] = ord('z')
        self.assertEqual(party_members(corrupted), [])
        self.assertEqual(party_members(body+member), [])

    def test_additional_hits_are_already_in_total(self):
        event = direct(hit(extras=(11, 11)), 1.)
        self.assertEqual(event['amount'], 1263)
        self.assertEqual(event['primaryAmount'], 1241)
        self.assertEqual(event['additionalAmounts'], [11, 11])

    def test_scalar_length_does_not_shift_damage(self):
        for scalar in (10817, 20000, 100000):
            event = direct(hit(scalar=scalar), 1.)
            self.assertEqual((event['scalar'], event['amount']), (scalar, 1263))

    def test_notice_is_not_damage(self):
        notice = b'\x04\x38'+var(99)+b'\0\0'+var(42)
        self.assertIsNone(direct(notice, 1.))

    def test_malformed_or_ambiguous_damage_is_rejected(self):
        data = hit(extras=(11, 11))
        for end in range(len(data)-2):
            with self.assertRaises(DecodeError):
                direct(data[:end], 1.)
        for amount in (0, 100_000_001):
            with self.assertRaises(DecodeError):
                direct(hit(amount=amount), 1.)
        with self.assertRaises(DecodeError):
            direct(hit(amount=20, extras=(11, 11)), 1.)
        with self.assertRaises(DecodeError):
            direct(hit()+b'\x02\0\0\0', 1.)
        with self.assertRaises(DecodeError):
            uint(b'\xff'*6, 0)

    def test_filters_share_duration_and_idle_resets(self):
        frames = [(identity(42, 'Alpha'), 0.), (identity(43, 'Beta'), 0.),
                  (identity(44, 'Gamma'), 0.),
                  (b'\x41\x36'+var(99)+b'\0\0\0'+struct.pack('<I', 12345), 0.),
                  (hit(extras=(11, 11)), 1.),
                  (hit(source=43, amount=400), 2.), (hit(source=44, amount=600), 3.),
                  (hit(source=42, target=42, amount=55), 4.),
                  (hit(source=45, amount=777), 4.),
                  (hit(source=42, target=43, amount=888), 4.),
                  (hit(source=42, amount=100), 20.)]
        with tempfile.TemporaryDirectory(prefix='combat-test-', dir=TEST_ROOT) as folder:
            path = pathlib.Path(folder)/'frames.jsonl'
            path.write_text(''.join(json.dumps(dict(hex=b.hex(), ts=t))+'\n' for b,t in frames), encoding='utf-8')
            report = analyze(path, 'Alpha', ['Beta'])
        first, second = report['encounters']
        self.assertEqual(first['durationSeconds'], 2.)
        self.assertEqual(first['self'][0]['damage'], 1263)
        self.assertEqual(first['self'][0]['encounterDps'], 631.5)
        self.assertEqual({r['name'] for r in first['party']}, {'Alpha', 'Beta'})
        self.assertEqual(len(first['globalVisible']), 3)
        self.assertEqual(second['self'][0]['damage'], 100)
        self.assertEqual(report['counts']['self_effects_excluded'], 1)
        self.assertEqual(report['counts']['unattributed_events'], 1)
        self.assertEqual(report['counts']['unverified_target_events'], 1)
        self.assertFalse(report['completeDamage'])

    def test_reused_identity_is_not_merged(self):
        frames = [(identity(42, 'Alpha'), 0.), (identity(42, 'Beta'), 1.)]
        with tempfile.TemporaryDirectory(prefix='combat-test-', dir=TEST_ROOT) as folder:
            path = pathlib.Path(folder)/'frames.jsonl'
            path.write_text(''.join(json.dumps(dict(hex=b.hex(), ts=t))+'\n' for b,t in frames), encoding='utf-8')
            report = analyze(path)
        self.assertEqual(report['conflictingIdentities'], 1)

    def test_party_snapshot_replaces_members_without_losing_global_damage(self):
        frames = [(identity(44, 'Gamma'), 0.),
                  (b'\x41\x36'+var(99)+b'\0\0\0'+struct.pack('<I', 12345), 0.),
                  (b'\x00\x92\x08'+party_member(42, 'Alpha')+party_member(43, 'Beta'), 0.),
                  (hit(source=42, amount=100), 1.), (hit(source=43, amount=200), 2.),
                  (hit(source=44, amount=300), 3.),
                  (b'\x00\x92\0'+party_member(42, 'Alpha'), 4.)]
        with tempfile.TemporaryDirectory(prefix='combat-test-', dir=TEST_ROOT) as folder:
            path = pathlib.Path(folder)/'frames.jsonl'
            path.write_text(''.join(json.dumps(dict(hex=b.hex(), ts=t))+'\n' for b,t in frames), encoding='utf-8')
            report = analyze(path, 'Alpha', ['Beta'])
        self.assertEqual(len(report['detectedPartySnapshots']), 2)
        encounter = report['encounters'][0]
        self.assertEqual(len(encounter['globalVisible']), 3)
        self.assertEqual([actor['name'] for actor in encounter['party']], ['Alpha'])
        self.assertEqual(encounter['partyBasis'], 'Observed 0092 snapshot')

    def test_party_join_delta_keeps_self_and_adds_only_validated_member(self):
        delta = b'\x0d\x92\0\2'+party_member(43, 'Beta')
        self.assertEqual(party_members(delta)[0]['actor'], 43)
        self.assertEqual(party_members(delta+party_member(44, 'Gamma')), [])
        self.assertEqual(party_members(delta[:40]), [])
        frames = [(identity(42, 'Alpha'), 0.), (identity(43, 'Beta'), 0.),
                  (b'\x41\x36'+var(99)+b'\0\0\0'+struct.pack('<I', 12345), 0.),
                  (b'\x00\x92\0'+party_member(42, 'Alpha'), 0.),
                  (hit(source=42, amount=100), 1.), (delta, 2.),
                  (hit(source=43, amount=200), 3.)]
        with tempfile.TemporaryDirectory(prefix='combat-test-', dir=TEST_ROOT) as folder:
            path = pathlib.Path(folder)/'frames.jsonl'
            path.write_text(''.join(json.dumps(dict(hex=b.hex(), ts=t))+'\n' for b,t in frames), encoding='utf-8')
            report = analyze(path, 'Alpha')
        self.assertEqual({actor['name'] for actor in report['encounters'][0]['party']}, {'Alpha', 'Beta'})
        self.assertEqual(report['detectedPartySnapshots'][-1]['opcode'], '0D92')


if __name__ == '__main__':
    unittest.main()

"""Offline signal investigation. Candidate fields are hypotheses, never stamina readings."""
import argparse
import bisect
import collections
import json
import math
from pathlib import Path
import statistics
import struct


def varint(data, pos):
    value = 0
    for shift in range(0, 35, 7):
        if pos >= len(data):
            return None
        byte = data[pos]
        pos += 1
        value |= (byte & 127) << shift
        if byte < 128:
            return value, pos
    return None


def lz4_block(data, expected):
    if not 0 < expected <= 1_000_000:
        raise ValueError("bundle size")
    out = bytearray()
    pos = 0

    def length(base):
        nonlocal pos
        if base == 15:
            while True:
                if pos >= len(data):
                    raise ValueError("truncated length")
                n = data[pos]
                pos += 1
                base += n
                if n != 255:
                    break
        return base

    while pos < len(data):
        token = data[pos]
        pos += 1
        literal = length(token >> 4)
        if pos + literal > len(data) or len(out) + literal > expected:
            raise ValueError("literal bounds")
        out.extend(data[pos:pos + literal])
        pos += literal
        if pos == len(data):
            break
        if pos + 2 > len(data):
            raise ValueError("missing match")
        offset = int.from_bytes(data[pos:pos + 2], "little")
        pos += 2
        match = length(token & 15) + 4
        if not 0 < offset <= len(out) or len(out) + match > expected:
            raise ValueError("match bounds")
        for _ in range(match):
            out.append(out[-offset])
    if len(out) != expected:
        raise ValueError("bundle length mismatch")
    return bytes(out)


def frame_at(data, pos):
    info = varint(data, pos)
    if info is None:
        return None
    length, body = info
    end = body + length - 4
    if length < 6 or end - pos > 65535:
        return False
    return body, end


def inner_frames(data, ts, stream, direction, counts, depth=0):
    if depth > 4:
        counts["depth_limit"] += 1
        return
    pos = 0
    while pos < len(data):
        if data[pos] == 0:
            pos += 1
            continue
        found = frame_at(data, pos)
        if not found or found[1] > len(data):
            counts["inner_invalid"] += 1
            return
        body, end = found
        yield from decode_body(data[body:end], ts, stream, direction, counts, depth)
        pos = end


def decode_body(body, ts, stream, direction, counts, depth=0):
    if body[:2] == b"\xff\xff":
        counts["bundles"] += 1
        try:
            decoded = lz4_block(body[6:], int.from_bytes(body[2:6], "little"))
        except (ValueError, IndexError):
            counts["bundle_errors"] += 1
            return
        yield from inner_frames(decoded, ts, stream, direction, counts, depth + 1)
    elif len(body) >= 2:
        yield {"ts": ts, "stream": stream, "direction": direction,
               "opcode": body[:2].hex().upper(), "hex": body.hex().upper()}


class Stream:
    def __init__(self):
        self.next = None
        self.pending = {}
        self.buffer = bytearray()
        self.synced = False

    def push(self, row, sync_ops, counts):
        seq = row["seq"]
        data = bytes.fromhex(row["hex"])
        if row.get("flags", 0) & 2:
            seq = (seq + 1) & 0xffffffff
        if self.next is None:
            self.next = seq
        delta = (seq - self.next + 0x80000000) % 0x100000000 - 0x80000000
        if delta > 0:
            self.pending[seq] = (data, row)
            if len(self.pending) > 64 or sum(len(v[0]) for v in self.pending.values()) > 262144:
                counts["tcp_gaps"] += 1
                self.buffer.clear()
                self.synced = False
                self.next = min(self.pending, key=lambda x: (x - self.next) % 0x100000000)
            else:
                return []
        else:
            overlap = -delta
            if overlap >= len(data):
                counts["duplicates"] += 1
                return []
            self.buffer.extend(data[overlap:])
            self.next = (seq + len(data)) & 0xffffffff
        while self.pending:
            seq = min(self.pending, key=lambda x: (x - self.next + 0x80000000) % 0x100000000)
            delta = (seq - self.next + 0x80000000) % 0x100000000 - 0x80000000
            if delta > 0:
                break
            data, _ = self.pending.pop(seq)
            if -delta < len(data):
                self.buffer.extend(data[-delta:])
                self.next = (seq + len(data)) & 0xffffffff
        frames = []
        pos = 0
        # Start mid-connection: require three consecutive known frames to align.
        if not self.synced:
            for candidate in range(len(self.buffer)):
                at = candidate
                for _ in range(3):
                    found = frame_at(self.buffer, at)
                    if not found or found[1] > len(self.buffer):
                        break
                    body, at = found
                    if int.from_bytes(self.buffer[body:body + 2], "big") not in sync_ops:
                        break
                else:
                    counts["resync_bytes"] += candidate
                    pos = candidate
                    self.synced = True
                    break
            if not self.synced:
                if len(self.buffer) > 65535:
                    counts["resync_bytes"] += len(self.buffer) - 65535
                    del self.buffer[:-65535]
                return []
        while pos < len(self.buffer):
            if self.buffer[pos] == 0:
                pos += 1
                continue
            if len(self.buffer) - pos >= 5 and 20 <= self.buffer[pos] <= 23 and self.buffer[pos + 1] == 3 and 1 <= self.buffer[pos + 2] <= 4:
                n = int.from_bytes(self.buffer[pos + 3:pos + 5], "big")
                if 0 < n <= 16640:
                    if pos + 5 + n > len(self.buffer):
                        break
                    counts["tls_skipped"] += 1
                    pos += 5 + n
                    continue
            found = frame_at(self.buffer, pos)
            if found is None:
                break
            if found is False:
                counts["invalid_frame"] += 1
                pos += 1
                continue
            body, end = found
            if end > len(self.buffer):
                break
            frames.extend(decode_body(bytes(self.buffer[body:end]), row["ts"], row["stream"], row["direction"], counts))
            pos = end
        del self.buffer[:pos]
        return frames


def stat_values(body):
    if body[:2] != b"\x00\x8d":
        return []
    entity = varint(body, 2)
    if entity is None:
        return []
    actor, p = entity
    if p >= len(body):
        return []
    flags = body[p]
    p += 1
    if flags & ~3:
        return []
    values = []
    for bit, width in [(1, 4), (2, 8)]:
        if flags & bit:
            if p >= len(body):
                return []
            count = body[p]
            p += 1
            for _ in range(count):
                if p + 1 + width > len(body):
                    return []
                kind = body[p]
                value = int.from_bytes(body[p + 1:p + 1 + width], "little", signed=width == 8)
                values.append((actor, width, kind, value))
                p += width + 1
    return values if p == len(body) else []


def main(folder):
    folder = Path(folder)
    protocol = Path(__file__).resolve().parents[1] / "research/Aion-DPS-Meter/Client/assets/aion2/protocol/opcodes.json"
    sync_ops = set(json.loads(protocol.read_text(encoding="utf-8"))["syncOpcodes"]) | {65535}
    counts = collections.Counter()
    streams = collections.defaultdict(Stream)
    frames = []
    for line in (folder / "segments.jsonl").read_text(encoding="utf-8-sig").splitlines():
        row = json.loads(line)
        counts["segments"] += 1
        # This EU session's game connection uses 13328. HTTPS/login and the
        # game's local IPC streams cannot be parsed using the game framing.
        endpoints = row["stream"].split(">")
        if not any(endpoint.rsplit(":", 1)[-1] == "13328" for endpoint in endpoints):
            counts["non_game_segments"] += 1
            continue
        frames.extend(streams[row["stream"]].push(row, sync_ops, counts))
    frames.sort(key=lambda f: f["ts"])
    with (folder / "frames.jsonl").open("w", encoding="utf-8") as out:
        for f in frames:
            out.write(json.dumps(f) + "\n")
    markers = [json.loads(line) for line in (folder / "markers.jsonl").read_text(encoding="utf-8-sig").splitlines()]
    base = markers[0]["ts"] if markers else (frames[0]["ts"] if frames else 0)
    opcodes = collections.Counter((f["direction"], f["opcode"]) for f in frames)
    stats = collections.defaultdict(list)
    for f in frames:
        if f["direction"] != "in":
            continue
        for actor, width, kind, value in stat_values(bytes.fromhex(f["hex"])):
            stats[(actor, width, kind)].append([round(f["ts"] - base, 3), value])
    report = {"counts": dict(counts), "frames": len(frames),
              "markers": [{"t": round(m["ts"] - base, 3), "event": m["event"]} for m in markers],
              "opcodes": [{"direction": d, "opcode": o, "count": c} for (d, o), c in opcodes.most_common()],
              "stats": [{"actor": a, "width": w, "kind": k, "series": v} for (a, w, k), v in stats.items()],
              "warning": "Unidentified fields. No candidate has been verified as dash energy."}
    (folder / "analysis.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"folder": folder.name, "frames": len(frames), "counts": dict(counts),
                      "markers": report["markers"], "opcodes": report["opcodes"][:25], "stat_fields": len(stats)}, indent=2))


def selftest():
    assert varint(b"\xac\x02", 0) == (300, 2)
    assert varint(b"\x80", 0) is None
    assert lz4_block(b"\x50hello", 5) == b"hello"
    assert lz4_block(b"\x10a\x01\x00", 5) == b"aaaaa"
    try:
        lz4_block(b"\x00\x00\x00", 4)
    except ValueError:
        pass
    else:
        raise AssertionError("Invalid offset accepted")
    assert frame_at(b"\x06\x00\x8d", 0) == (1, 3)
    assert stat_values(bytes.fromhex("008D010201076400000000000000")) == [(1, 8, 7, 100)]
    known = {0x1234}
    s = Stream()
    cnt = collections.Counter()
    def row(seq, data):
        return {"seq": seq, "hex": data.hex(), "ts": 1.0, "direction": "in", "stream": "test"}
    data = bytes.fromhex("061234") * 4
    assert not s.push(row(100, data[:6]), known, cnt)
    assert not s.push(row(109, data[9:]), known, cnt)
    assert len(s.push(row(106, data[6:9]), known, cnt)) == 4
    assert not s.push(row(100, data), known, cnt)
    print("PASS: varints, LZ4 literals/overlap/bounds, framing, stat records, out-of-order TCP, retransmissions")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("folder", nargs="?")
    parser.add_argument("--selftest", action="store_true")
    args = parser.parse_args()
    if args.selftest:
        selftest()
    else:
        main(args.folder)

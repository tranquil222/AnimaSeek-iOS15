#!/usr/bin/env python3
"""Reject an IPA candidate containing native code with a deployment floor above iOS 15."""
import pathlib
import plistlib
import struct
import sys

MAX_IOS = (15, 0, 0)


def version(value):
    return value >> 16, (value >> 8) & 255, value & 255


def slices(data):
    magic = data[:4]
    if magic in (b"\xca\xfe\xba\xbe", b"\xbe\xba\xfe\xca"):
        endian = ">" if magic == b"\xca\xfe\xba\xbe" else "<"
        count = struct.unpack_from(endian + "I", data, 4)[0]
        for i in range(count):
            _, _, offset, size, _ = struct.unpack_from(endian + "IIIII", data, 8 + i * 20)
            yield data[offset:offset + size]
    elif magic in (b"\xcf\xfa\xed\xfe", b"\xce\xfa\xed\xfe"):
        yield data


def main():
    bundle = pathlib.Path(sys.argv[1])
    info = plistlib.loads((bundle / "Info.plist").read_bytes())
    floor = tuple(map(int, info["MinimumOSVersion"].split(".")))
    assert floor + (0,) * (3 - len(floor)) <= MAX_IOS, info["MinimumOSVersion"]
    assert info["CFBundleIdentifier"] == "com.animaseek.app"
    binaries = 0
    for path in sorted(bundle.rglob("*")):
        if not path.is_file() or path.is_symlink():
            continue
        for data in slices(path.read_bytes()):
            binaries += 1
            is64 = data[:4] == b"\xcf\xfa\xed\xfe"
            ncmds = struct.unpack_from("<I", data, 16)[0]
            offset = 32 if is64 else 28
            targets = []
            for _ in range(ncmds):
                cmd, size = struct.unpack_from("<II", data, offset)
                assert size >= 8 and offset + size <= len(data), str(path)
                if cmd == 0x32:  # LC_BUILD_VERSION
                    platform, minimum = struct.unpack_from("<II", data, offset + 8)
                    assert platform == 2, f"Non-iOS binary: {path} (platform {platform})"
                    targets.append(version(minimum))
                elif cmd == 0x25:  # LC_VERSION_MIN_IPHONEOS
                    targets.append(version(struct.unpack_from("<I", data, offset + 8)[0]))
                elif cmd in (0x21, 0x2C):  # LC_ENCRYPTION_INFO / _64
                    assert struct.unpack_from("<I", data, offset + 16)[0] == 0, f"Encrypted: {path}"
                offset += size
            assert targets, f"No iOS deployment target: {path}"
            assert all(target <= MAX_IOS for target in targets), f"Needs newer iOS: {path}: {targets}"
            print(f"{path.relative_to(bundle)}: minimum iOS {max(targets)}")
    assert binaries, "No Mach-O binaries found"
    print(f"Verified {binaries} native slices; bundle minimum iOS {info['MinimumOSVersion']}.")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""MS 바이너리의 PDB를 MS 심볼 서버에서 받는다.

사용법 (레포 루트에서):
    python3 .claude/skills/ghidra-headless-export/scripts/fetch_pdb.py ntoskrnl.exe ntdll.dll
    python3 .../fetch_pdb.py --info ntdll.dll          # 받지 않고 GUID만 출력

- 바이너리 위치: ms/bin/<이름> 이 있으면 그것, 없으면 ms/bin 아래에서 */System32/<이름> 을 찾는다.
- 저장 위치: ms/sym/<바이너리이름>.pdb  (예: ms/sym/ntoskrnl.exe.pdb)
           ms/sym/<바이너리이름>.pdb.info.txt  (원래 pdb 이름, GUID+Age, 받은 URL)
- 외부 패키지 없이 표준 라이브러리만 쓴다.
"""
import os
import struct
import subprocess
import sys
import urllib.request

SYMSRV = "https://msdl.microsoft.com/download/symbols"
UA = "Microsoft-Symbol-Server/10.0.0.0"


def repo_root():
    try:
        return subprocess.check_output(["git", "rev-parse", "--show-toplevel"], text=True).strip()
    except Exception:
        return os.getcwd()


def find_binary(root, name):
    direct = os.path.join(root, "ms", "bin", name)
    if os.path.isfile(direct):
        return direct
    lname = name.lower()
    hits = []
    for dirpath, _dirs, files in os.walk(os.path.join(root, "ms", "bin")):
        for f in files:
            if f.lower() == lname:
                hits.append(os.path.join(dirpath, f))
    # System32 를 SysWOW64/WinSxS 보다 우선
    hits.sort(key=lambda p: (0 if os.sep + "system32" + os.sep in p.lower() else 1, len(p)))
    return hits[0] if hits else None


def read_codeview(path):
    """PE의 디버그 디렉터리에서 RSDS(CodeView) 정보를 읽는다 -> (pdb이름, GUID+Age 문자열)."""
    with open(path, "rb") as fp:
        data = fp.read()
    if data[:2] != b"MZ":
        raise ValueError("MZ 헤더 아님")
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        raise ValueError("PE 시그니처 아님")
    nsec, = struct.unpack_from("<H", data, pe + 6)
    opt_size, = struct.unpack_from("<H", data, pe + 20)
    opt = pe + 24
    magic, = struct.unpack_from("<H", data, opt)
    dd = opt + (112 if magic == 0x20B else 96)          # DataDirectory 시작
    dbg_rva, dbg_size = struct.unpack_from("<II", data, dd + 6 * 8)  # [6] = DEBUG
    if dbg_rva == 0:
        raise ValueError("디버그 디렉터리 없음")
    sec = opt + opt_size

    def rva2off(rva):
        for i in range(nsec):
            s = sec + i * 40
            vsize, vaddr, rsize, rptr = struct.unpack_from("<IIII", data, s + 8)
            if vaddr <= rva < vaddr + max(vsize, rsize):
                return rva - vaddr + rptr
        raise ValueError("RVA 0x%x 를 섹션에서 못 찾음" % rva)

    off = rva2off(dbg_rva)
    for i in range(dbg_size // 28):
        e = off + i * 28
        typ, = struct.unpack_from("<I", data, e + 12)
        raw, = struct.unpack_from("<I", data, e + 24)
        if typ == 2 and data[raw:raw + 4] == b"RSDS":       # IMAGE_DEBUG_TYPE_CODEVIEW
            d1, d2, d3 = struct.unpack_from("<IHH", data, raw + 4)
            d4 = data[raw + 12:raw + 20]
            age, = struct.unpack_from("<I", data, raw + 20)
            name = data[raw + 24:data.index(b"\0", raw + 24)].decode("ascii", "replace")
            guid = "%08X%04X%04X%s%X" % (d1, d2, d3, d4.hex().upper(), age)
            return os.path.basename(name.replace("\\", "/")), guid
    raise ValueError("RSDS(CodeView) 항목 없음")


def main(argv):
    info_only = "--info" in argv
    names = [a for a in argv if not a.startswith("--")]
    if not names:
        print(__doc__)
        return 1
    root = repo_root()
    symdir = os.path.join(root, "ms", "sym")
    os.makedirs(symdir, exist_ok=True)
    rc = 0
    for name in names:
        path = find_binary(root, name)
        if not path:
            print("[!] %s: ms/bin 아래에서 못 찾음" % name)
            rc = 2
            continue
        try:
            pdb, guid = read_codeview(path)
        except Exception as ex:
            print("[!] %s: %s" % (name, ex))
            rc = 2
            continue
        url = "%s/%s/%s/%s" % (SYMSRV, pdb, guid, pdb)
        print("[+] %s\n    binary : %s\n    pdb    : %s\n    guid   : %s\n    url    : %s" % (name, path, pdb, guid, url))
        if info_only:
            continue
        out = os.path.join(symdir, name + ".pdb")
        if os.path.isfile(out) and os.path.getsize(out) > 0:
            print("    (이미 있음) %s" % out)
        else:
            req = urllib.request.Request(url, headers={"User-Agent": UA})
            try:
                with urllib.request.urlopen(req, timeout=300) as r, open(out + ".part", "wb") as w:
                    while True:
                        chunk = r.read(1 << 20)
                        if not chunk:
                            break
                        w.write(chunk)
                os.replace(out + ".part", out)
                print("    저장   : %s (%d bytes)" % (out, os.path.getsize(out)))
            except Exception as ex:
                print("[!] 다운로드 실패: %s" % ex)
                rc = 3
                continue
        with open(out + ".info.txt", "w") as w:
            w.write("binary=%s\npdb=%s\nguid_age=%s\nurl=%s\n" % (path, pdb, guid, url))
    return rc


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

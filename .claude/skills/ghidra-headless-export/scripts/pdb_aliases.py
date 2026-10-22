#!/usr/bin/env python3
"""한 주소에 함수 이름이 여러 개 붙은 곳(별칭 무리)을 PDB 에서 뽑아 표로 만든다.

왜: 링커가 내용이 같은 함수를 한 벌로 합치면(ICF) PDB 에는 그 주소에 이름이 여러 개 남는다.
    Ghidra·WinDbg 는 그중 하나만 보여 주므로, 이름만 믿으면 엉뚱한 함수로 읽거나
    (정적) 멈춤점이 다른 함수 호출에도 걸린다 (동적). 전체 목록을 한 곳에 둔다.

사용법 (레포 루트에서):
    python3 .claude/skills/ghidra-headless-export/scripts/pdb_aliases.py ntoskrnl.exe ntdll.dll
        -> ms/sym/<바이너리이름>.aliases.tsv  (예: ms/sym/ntoskrnl.exe.aliases.tsv)
    python3 .../pdb_aliases.py --lookup ntoskrnl.exe MmIsSessionAddress   # 이름으로 무리 찾기
    python3 .../pdb_aliases.py --lookup ntoskrnl.exe 0x490960             # RVA 로 무리 찾기
    python3 .../pdb_aliases.py --outdir <폴더> ntoskrnl.exe                # 다른 곳에 쓰기 (시험용)
    python3 .../pdb_aliases.py --where ntoskrnl.exe KeWaitForSingleObject  # PDB 이름이 어디 있나 + _index.tsv 에선 무슨 이름인가

--where (sast/08, 2026-10-22): _index.tsv 는 Ghidra 가 고른 이름 하나만 담는다. PDB 이름이 내보내기 이름 등으로
    잡혀 있으면 _index.tsv grep 으로는 "없다"고 나온다 (예: KeWaitForSingleObject → Ghidra 이름 KeWaitForMutexObject).
    --where 는 PDB 공개 심볼 **전부**(별칭 무리가 아니어도)에서 이름을 찾아, RVA·종류·같은 주소의 다른 PDB 이름·
    ms/decomp/<모듈>/_index.tsv 의 그 주소 이름을 보여 준다. 정확히 같은 이름이 없으면 대소문자 무시 부분 일치를 20개까지.

- 입력: ms/sym/<바이너리이름>.pdb (fetch_pdb.py 가 받은 것)
- 필요: llvm-pdbutil (서버엔 llvm-pdbutil-14). PATH 에서 llvm-pdbutil, llvm-pdbutil-NN 순으로 찾는다.
- 출력 표 (탭 구분, RVA 순):
    # rva	count	names
    0x490960	38	WheapDefaultErrSrcRecover,xHalGetInterruptVector,...
  함수(flags 에 function)인 공개 심볼만, 이름이 2개 이상인 주소만.
- MS 심볼에서 뽑은 목록이라 ms/sym 처럼 커밋하지 않는다 (.gitignore).
"""
import glob
import os
import re
import shutil
import subprocess
import sys
from collections import defaultdict


def repo_root():
    try:
        return subprocess.check_output(["git", "rev-parse", "--show-toplevel"], text=True).strip()
    except Exception:
        return os.getcwd()


def find_pdbutil():
    for name in ["llvm-pdbutil"] + sorted(
            (os.path.basename(p) for d in os.environ.get("PATH", "").split(os.pathsep)
             for p in glob.glob(os.path.join(d, "llvm-pdbutil-*"))), reverse=True):
        path = shutil.which(name)
        if path:
            return path
    return None


def run(tool, *args):
    out = subprocess.run([tool, "dump", *args], capture_output=True)
    if out.returncode != 0:
        raise RuntimeError(out.stderr.decode("utf-8", "replace").strip()[-400:])
    return out.stdout.decode("utf-8", "replace")


def section_vas(tool, pdb):
    vas = []
    for line in run(tool, "-section-headers", pdb).splitlines():
        line = line.strip()
        if line.endswith("virtual address"):
            vas.append(int(line.split()[0], 16))
    return vas


PUB = re.compile(r"S_PUB32 \[size = \d+\] `([^`]*)`\s*\n\s*flags = ([^\n]*?), addr = (\d+):(\d+)")


def groups(tool, pdb):
    vas = section_vas(tool, pdb)
    by = defaultdict(list)
    for name, flags, seg, off in PUB.findall(run(tool, "-publics", pdb)):
        seg, off = int(seg), int(off)
        if "function" not in flags or not 1 <= seg <= len(vas):
            continue
        rva = vas[seg - 1] + off
        if name not in by[rva]:
            by[rva].append(name)
    return {rva: names for rva, names in by.items() if len(names) > 1}


def where(tool, pdb, binname, key, root):
    vas = section_vas(tool, pdb)
    pubs = []
    for name, flags, seg, off in PUB.findall(run(tool, "-publics", pdb)):
        seg, off = int(seg), int(off)
        if 1 <= seg <= len(vas):
            pubs.append((name, flags.strip(), vas[seg - 1] + off))
    hits = [p for p in pubs if p[0] == key]
    exact = bool(hits)
    if not hits:
        k = key.lower()
        hits = [p for p in pubs if k in p[0].lower()][:20]
    if not hits:
        print("PDB 공개 심볼에 없음: %s (이 빌드에 없거나, 공개 심볼이 아닌 이름)" % key)
        return 0
    module = os.path.splitext(binname)[0].lower()
    index = defaultdict(list)
    spans = []  # (rva, size, name) — 이 주소를 품은 Ghidra 함수 찾기용 (묶음 O2)
    ipath = os.path.join(root, "ms", "decomp", module, "_index.tsv")
    if os.path.isfile(ipath):
        for line in open(ipath, encoding="utf-8", errors="replace"):
            if line.startswith("#"):
                continue
            cols = line.rstrip("\n").split("\t")
            if len(cols) >= 3 and cols[2].startswith("0x"):
                index[int(cols[2], 16)].append(cols[0])
                if len(cols) >= 4 and cols[3].isdigit():
                    spans.append((int(cols[2], 16), int(cols[3]), cols[0]))
    spans.sort()
    starts = [x[0] for x in spans]

    def containing(r):
        import bisect
        i = bisect.bisect_right(starts, r) - 1
        while i >= 0 and r - spans[i][0] < 0x100000:
            fr, fs, fn = spans[i]
            if fr <= r < fr + fs:
                return "%s +0x%x" % (fn, r - fr)
            i -= 1
        return None
    same = defaultdict(list)
    for n, f, r in pubs:
        if "function" in f:
            same[r].append(n)
    if not exact:
        print("정확히 같은 이름은 없음. 부분 일치 %d개:" % len(hits))
    for n, f, r in hits:
        others = [x for x in same.get(r, []) if x != n]
        gname = index.get(r)
        print("%s  rva=0x%x  [%s]" % (n, r, f))
        print("    _index.tsv 이름: %s" % (", ".join(gname) if gname else
              ("(없음 — 함수 시작이 아니거나 Ghidra 가 함수로 안 잡음)" if os.path.isfile(ipath) else "(_index.tsv 없음: 아직 analyze 전)")))
        if not gname and spans:
            c = containing(r)
            print("    이 주소를 품은 Ghidra 함수: %s" % (c if c else "(없음 — 어느 함수 범위에도 안 듦)"))
        if others:
            print("    같은 주소의 다른 PDB 이름 %d개: %s%s" % (len(others), ", ".join(others[:8]), " ..." if len(others) > 8 else ""))
    return 0


def main(argv):
    outdir = None
    if "--outdir" in argv:
        i = argv.index("--outdir")
        outdir = argv[i + 1]
        del argv[i:i + 2]
    lookup = "--lookup" in argv
    wherex = "--where" in argv
    args = [a for a in argv if a not in ("--lookup", "--where")]
    if not args or ((lookup or wherex) and len(args) != 2):
        print(__doc__)
        return 1
    tool = find_pdbutil()
    if not tool:
        print("[!] llvm-pdbutil 을 못 찾음 (apt install llvm-14 등)")
        return 2
    root = repo_root()
    symdir = os.path.join(root, "ms", "sym")
    outdir = outdir or symdir

    if wherex:
        binname, key = args
        pdb = os.path.join(symdir, binname + ".pdb")
        if not os.path.isfile(pdb):
            print("[!] PDB 없음: %s (먼저 fetch_pdb.py %s)" % (pdb, binname))
            return 3
        return where(tool, pdb, binname, key, root)

    if lookup:
        binname, key = args
        table = os.path.join(outdir, binname + ".aliases.tsv")
        if not os.path.isfile(table):
            print("[!] 표가 없음: %s  (먼저 pdb_aliases.py %s)" % (table, binname))
            return 3
        want_rva = int(key, 16) if key.lower().startswith("0x") else None
        hit = 0
        for line in open(table, encoding="utf-8"):
            if line.startswith("#"):
                continue
            rva, count, names = line.rstrip("\n").split("\t")
            namelist = names.split(",")
            if (want_rva is not None and int(rva, 16) == want_rva) or key in namelist:
                print("%s  %s개: %s" % (rva, count, ", ".join(namelist)))
                hit += 1
        if not hit:
            print("별칭 무리 아님: %s (이 주소엔 이름이 하나뿐이거나 표에 없음)" % key)
        return 0

    rc = 0
    os.makedirs(outdir, exist_ok=True)
    for binname in args:
        pdb = os.path.join(symdir, binname + ".pdb")
        if not os.path.isfile(pdb):
            print("[!] %s: PDB 없음 %s (먼저 fetch_pdb.py %s)" % (binname, pdb, binname))
            rc = 3
            continue
        try:
            g = groups(tool, pdb)
        except Exception as ex:
            print("[!] %s: llvm-pdbutil 실패: %s" % (binname, ex))
            rc = 4
            continue
        out = os.path.join(outdir, binname + ".aliases.tsv")
        with open(out, "w", encoding="utf-8") as w:
            w.write("# rva\tcount\tnames\n")
            for rva in sorted(g):
                w.write("0x%x\t%d\t%s\n" % (rva, len(g[rva]), ",".join(g[rva])))
        print("[+] %s: 별칭 무리 %d곳, 이름 %d개 -> %s" % (
            binname, len(g), sum(len(v) for v in g.values()), out))
    return rc


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

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


def main(argv):
    outdir = None
    if "--outdir" in argv:
        i = argv.index("--outdir")
        outdir = argv[i + 1]
        del argv[i:i + 2]
    lookup = "--lookup" in argv
    args = [a for a in argv if a != "--lookup"]
    if not args or (lookup and len(args) != 2):
        print(__doc__)
        return 1
    tool = find_pdbutil()
    if not tool:
        print("[!] llvm-pdbutil 을 못 찾음 (apt install llvm-14 등)")
        return 2
    root = repo_root()
    symdir = os.path.join(root, "ms", "sym")
    outdir = outdir or symdir

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

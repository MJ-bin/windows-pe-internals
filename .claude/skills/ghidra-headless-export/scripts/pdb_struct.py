#!/usr/bin/env python3
"""PDB 구조체 이름 -> 칸 표 (오프셋, 이름, 타입, 비트). llvm-pdbutil-14 dump -types 를 읽는다.

사용법 (레포 루트에서):
  python3 .claude/skills/ghidra-headless-export/scripts/pdb_struct.py <바이너리> <구조체이름> [...]   예) pdb_struct.py ntdll.dll _HEAP _PEB
  python3 .claude/skills/ghidra-headless-export/scripts/pdb_struct.py --list <바이너리> <정규식>        구조체 이름 찾기
PDB 는 ms/sym/<바이너리>.pdb (fetch_pdb.py 가 받은 것). 출력은 TSV: offset  name  type  bits
"""
import re, subprocess, sys, os

HDR = re.compile(r'^\s+0x([0-9A-F]+) \| (LF_\w+) \[size = \d+\](?: `(.*)`)?$')
MEM = re.compile(r'- LF_MEMBER \[name = `(.*?)`, Type = 0x([0-9A-F]+)(?: \((.*?)\))?, offset = (\d+)')

def load(binary):
    pdb = os.path.join('ms/sym', binary + '.pdb')
    if not os.path.exists(pdb):
        sys.exit(f'없음: {pdb} (fetch_pdb.py {binary} 먼저)')
    out = subprocess.run(['llvm-pdbutil-14', 'dump', '-types', pdb],
                         capture_output=True, text=True, check=True).stdout
    recs, cur = {}, None
    for line in out.splitlines():
        m = HDR.match(line)
        if m:
            cur = [m.group(2), m.group(3), []]
            recs[int(m.group(1), 16)] = cur
        elif cur is not None:
            cur[2].append(line.strip())
    return recs

def body(recs, i, key):
    for l in recs.get(i, (None, None, []))[2]:
        m = re.search(key + r'\s*=?\s*:?\s*(0x[0-9A-F]+|\d+|<no type>)', l)
        if m:
            return m.group(1)
    return None

def tname(recs, i, depth=0):
    if i < 0x1000 or depth > 6:
        return None
    k, n, b = recs.get(i, ('?', None, []))
    j = ' '.join(b)
    def ref(key):
        m = re.search(key + r' = 0x([0-9A-F]+)(?: \((.*?)\))?', j)
        if not m: return '?'
        return m.group(2) or tname(recs, int(m.group(1), 16), depth + 1) or '?'
    if k in ('LF_STRUCTURE', 'LF_UNION', 'LF_ENUM', 'LF_CLASS'):
        return n
    if k == 'LF_POINTER':
        return ref('referent') + '*'
    if k == 'LF_MODIFIER':
        return ref('referent')
    if k == 'LF_ARRAY':
        m = re.search(r'size: (\d+)', j)
        return ref('element type') + f'[{m.group(1) if m else "?"}B]'
    if k == 'LF_PROCEDURE':
        return 'func'
    if k == 'LF_BITFIELD':
        return ref('type')
    return k

def bits(recs, i):
    k, _, b = recs.get(i, ('?', None, []))
    if k != 'LF_BITFIELD':
        return ''
    m = re.search(r'bit offset = (\d+), # bits = (\d+)', ' '.join(b))
    return f'{m.group(1)}:{m.group(2)}' if m else '?'

def find_struct(recs, name):
    for i, (k, n, b) in recs.items():
        if k in ('LF_STRUCTURE', 'LF_UNION') and n == name:
            j = ' '.join(b)
            if 'forward ref' in j:
                continue
            m = re.search(r'field list: 0x([0-9A-F]+)', j)
            s = re.search(r'sizeof (\d+)', j)
            if m:
                return int(m.group(1), 16), int(s.group(1)) if s else None
    return None, None

def main(a):
    if len(a) >= 3 and a[0] == '--list':
        recs = load(a[1]); rx = re.compile(a[2])
        for n in sorted({n for k, n, _ in recs.values()
                         if k in ('LF_STRUCTURE', 'LF_UNION') and n and rx.search(n)}):
            print(n)
        return
    if len(a) < 2:
        sys.exit(__doc__)
    recs = load(a[0])
    for name in a[1:]:
        fl, size = find_struct(recs, name)
        if fl is None:
            print(f'# {name}: 없음', file=sys.stderr); continue
        print(f'# {name}\tsizeof 0x{size:x}' if size is not None else f'# {name}')
        print('offset\tname\ttype\tbits')
        for l in recs[fl][2]:
            m = MEM.search(l)
            if not m: continue
            fname, ti, simple, off = m.group(1), int(m.group(2), 16), m.group(3), int(m.group(4))
            print(f'+0x{off:x}\t{fname}\t{simple or tname(recs, ti) or "?"}\t{bits(recs, ti)}')

if __name__ == '__main__':
    main(sys.argv[1:])

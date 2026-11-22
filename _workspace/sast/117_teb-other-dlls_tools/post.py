#!/usr/bin/env python3
"""all.tsv → 함수 소유 (pdata) + PDB 공개 이름 → 요약.
사용: post.py all.tsv [--mods a,b] [--kinds W,RW,lea] [--fields f1,f2]
"""
import sys, os, re, struct, bisect, subprocess, collections, glob, shutil
import pefile

ROOT = '/home/mjbin/prj/bob/teamprj/windows-pe-internals'
S32 = ROOT + '/ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32'
SYM = ROOT + '/ms/sym'
SKIP_MODS = {'ntoskrnl.exe','ntkrla57.exe','hvix64.exe','hvax64.exe','securekernel.exe','securekernella57.exe'}
PUB = re.compile(r"S_PUB32 \[size = \d+\] `([^`]*)`\s*\n\s*flags = ([^\n]*?), addr = (\d+):(\d+)")

def pdbutil():
    for n in ['llvm-pdbutil', 'llvm-pdbutil-14']:
        p = shutil.which(n)
        if p: return p

_pe = {}
def pe_info(mod):
    if mod in _pe: return _pe[mod]
    path = os.path.join(S32, mod)
    pe = pefile.PE(path, fast_load=True)
    ib = pe.OPTIONAL_HEADER.ImageBase
    d = pe.OPTIONAL_HEADER.DATA_DIRECTORY[3]
    data = pe.get_data(d.VirtualAddress, d.Size)
    ents = []
    for i in range(0, len(data) - 11, 12):
        b, e, u = struct.unpack_from('<III', data, i)
        if b == 0: continue
        ents.append((b, e, u))
    ents.sort()
    starts = [x[0] for x in ents]
    _pe[mod] = (pe, ib, ents, starts)
    return _pe[mod]

def func_start(mod, rva):
    pe, ib, ents, starts = pe_info(mod)
    i = bisect.bisect_right(starts, rva) - 1
    if i < 0: return None
    b, e, u = ents[i]
    if rva >= e: return None
    # chained?  UNWIND_INFO: flags = byte0 >> 3
    for _ in range(8):
        try:
            ui = pe.get_data(u, 4)
        except Exception:
            break
        flags = ui[0] >> 3
        if not (flags & 4): break
        cnt = ui[2]
        off = 4 + ((cnt + 1) & ~1) * 2
        try:
            rf = pe.get_data(u + off, 12)
        except Exception:
            break
        b2, e2, u2 = struct.unpack('<III', rf)
        b, e, u = b2, e2, u2
    return b

_pub = {}
def pub_table(mod):
    if mod in _pub: return _pub[mod]
    pdb = os.path.join(SYM, mod + '.pdb')
    if not os.path.isfile(pdb):
        _pub[mod] = None; return None
    tool = pdbutil()
    out = subprocess.run([tool, 'dump', '-section-headers', pdb], capture_output=True).stdout.decode('utf-8', 'replace')
    vas = [int(l.strip().split()[0], 16) for l in out.splitlines() if l.strip().endswith('virtual address')]
    out = subprocess.run([tool, 'dump', '-publics', pdb], capture_output=True).stdout.decode('utf-8', 'replace')
    by = collections.defaultdict(list)
    for name, flags, seg, off in PUB.findall(out):
        seg, off = int(seg), int(off)
        if not 1 <= seg <= len(vas): continue
        if 'function' not in flags: continue
        r = vas[seg - 1] + off
        if name not in by[r]: by[r].append(name)
    rs = sorted(by)
    _pub[mod] = (by, rs)
    return _pub[mod]

_idx = {}
def idx_table(mod):
    if mod in _idx: return _idx[mod]
    m = os.path.splitext(mod)[0].lower()
    path = os.path.join(ROOT, 'ms', 'decomp', m, '_index.tsv')
    spans = []
    if os.path.isfile(path):
        for l in open(path, encoding='utf-8', errors='replace'):
            if l.startswith('#'): continue
            c = l.rstrip('\n').split('\t')
            if len(c) >= 4 and c[2].startswith('0x') and c[3].isdigit():
                spans.append((int(c[2], 16), int(c[3]), c[0]))
    spans.sort()
    _idx[mod] = (spans, [x[0] for x in spans]) if spans else None
    return _idx[mod]

def name_of(mod, rva):
    t0 = idx_table(mod)
    if t0:
        spans, st = t0
        i = bisect.bisect_right(st, rva) - 1
        while i >= 0 and rva - spans[i][0] < 0x200000:
            fr, fs, fn = spans[i]
            if fr <= rva < fr + fs:
                return '[Ghidra] %s +0x%x' % (fn, rva - fr)
            i -= 1
    t = pub_table(mod)
    if not t: return None
    by, rs = t
    if rva in by:
        names = by[rva]
        names2 = [n for n in names if not n.startswith('?')] or names
        return names2[0] + ('' if len(names) == 1 else ' (+%d)' % (len(names) - 1))
    i = bisect.bisect_right(rs, rva) - 1
    if i >= 0:
        n = by[rs[i]][0]
        return '~%s+0x%x' % (n, rva - rs[i])
    return None

def main():
    path = sys.argv[1]
    mods = None; kinds = None; fields = None
    for a in sys.argv[2:]:
        if a.startswith('--mods='): mods = set(a[7:].split(','))
        if a.startswith('--kinds='): kinds = set(a[8:].split(','))
        if a.startswith('--fields='): fields = set(a[9:].split(','))
    rows = collections.defaultdict(list)  # (mod, field) -> [(funcrva, off_in_func, kind, text, addr)]
    for l in open(path):
        if l.startswith('#'): continue
        f = l.rstrip('\n').split('\t')
        mod, addr, fld, off, kind, sz, txt = f[:7]
        if mod in SKIP_MODS: continue
        if mods and mod not in mods: continue
        if fields and fld not in fields: continue
        base = kind.split('-')[0] if not kind.startswith('unk') else kind
        if kinds and base not in kinds and kind not in kinds: continue
        pe, ib, ents, starts = pe_info(mod)
        a = int(addr, 16); rva = a - ib
        fs = func_start(mod, rva)
        rows[(mod, fld)].append((fs, rva, int(off, 16), kind, txt))
    for (mod, fld), lst in sorted(rows.items()):
        print('=== %s  %s' % (mod, fld))
        byfunc = collections.defaultdict(list)
        for fs, rva, off, kind, txt in lst:
            byfunc[fs].append((rva, off, kind, txt))
        for fs in sorted(byfunc, key=lambda x: (x is None, x)):
            nm = name_of(mod, fs) if fs is not None else None
            print('  func rva=%s %s' % (hex(fs) if fs is not None else '?', nm or ''))
            for rva, off, kind, txt in byfunc[fs][:12]:
                print('      +0x%x (rva 0x%x) off=0x%x %-8s %s' % (rva - (fs or 0), rva, off, kind, txt))
            if len(byfunc[fs]) > 12:
                print('      ... %d more' % (len(byfunc[fs]) - 12))

if __name__ == '__main__':
    main()

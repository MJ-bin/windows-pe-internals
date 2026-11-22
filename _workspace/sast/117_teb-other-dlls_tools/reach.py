#!/usr/bin/env python3
"""KernelBase/kernel32 직접 호출 그래프 (objdump 텍스트 + pdata) 에서
시작 루트 (DllMain 입구 · BaseThreadInitThunk) 로부터 작성 함수가 닿나 본다.
사용: reach.py FUNCNAME_OR_RVA...   (KernelBase 기준)
"""
import sys, re, bisect, collections, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import post, pefile

mods = {'KernelBase': 'KernelBase.dll', 'kernel32': 'kernel32.dll'}
info = {}
for k, f in mods.items():
    pe = pefile.PE(os.path.join(post.S32, f))
    pe.parse_data_directories()
    ib = pe.OPTIONAL_HEADER.ImageBase
    iat = {}
    for e in pe.DIRECTORY_ENTRY_IMPORT:
        for i in e.imports:
            if i.name: iat[i.address] = (e.dll.decode().lower(), i.name.decode())
    exp = {}
    for s in pe.DIRECTORY_ENTRY_EXPORT.symbols:
        if s.name and not s.forwarder: exp[s.name.decode()] = s.address
    info[k] = dict(pe=pe, ib=ib, iat=iat, exp=exp, entry=pe.OPTIONAL_HEADER.AddressOfEntryPoint)

# 이름 표 (Ghidra _index.tsv)
def nm(mod, rva):
    r = post.name_of(mod + '.dll', rva)
    return r.replace('[Ghidra] ', '') if r else hex(rva)

INS = re.compile(r'^\s*([0-9a-f]+):\t(.*)$')
CALL_DIRECT = re.compile(r'^(?:rex\.W )?(call|jmp)\s+(0x[0-9a-f]+)\s*$')
CALL_MEM = re.compile(r'(?:call|jmp)\s+QWORD PTR \[rip\+0x[0-9a-f]+\]\s+#\s+(0x[0-9a-f]+)')
LEA_REF = re.compile(r'lea\s+\w+,\[rip\+0x[0-9a-f]+\]\s+#\s+(0x[0-9a-f]+)')

edges = collections.defaultdict(set)    # (mod,func_rva) -> {(mod,func_rva)}  직접
refs = collections.defaultdict(set)     # 주소 꺼냄 (lea)
def owner(mod, rva):
    f = post.func_start(mod + '.dll', rva)
    return f

def build(mod):
    ib = info[mod]['ib']; iat = info[mod]['iat']
    pe, _, ents, starts = post.pe_info(mod + '.dll')
    startset = set(starts)
    for line in open(mod + '.dis', errors='replace'):
        m = INS.match(line)
        if not m: continue
        addr = int(m.group(1), 16)
        txt = m.group(2).split('#')[0].strip()
        full = m.group(2)
        src = owner(mod, addr - ib) if False else None
        if 'call' not in txt and 'jmp' not in txt and 'lea' not in txt: continue
        src = owner(mod, addr - ib)
        if src is None: continue
        mc = CALL_DIRECT.match(txt)
        if mc:
            t = int(mc.group(2), 16) - ib
            to = owner(mod, t)
            if to is not None and to != src:
                if mc.group(1) == 'call' or t in startset or True:
                    edges[(mod, src)].add((mod, to))
            continue
        mm = CALL_MEM.search(full)
        if mm:
            slot = int(mm.group(1), 16)
            if slot in iat:
                dll, name = iat[slot]
                for om in mods:
                    if name in info[om]['exp'] and om != mod or (om == mod and False):
                        t = info[om]['exp'][name]
                        edges[(mod, src)].add((om, owner(om, t) or t))
            continue
        ml = LEA_REF.search(full)
        if ml and 'lea' in txt:
            t = int(ml.group(1), 16) - ib
            if t in startset or True:
                to = owner(mod, t)
                if to is not None and to == t and to != src:
                    refs[(mod, src)].add((mod, to))

for m in mods:
    build(m)
print('edges', sum(len(v) for v in edges.values()), 'refs', sum(len(v) for v in refs.values()), file=sys.stderr)

roots = [('KernelBase', info['KernelBase']['entry']), ('kernel32', info['kernel32']['entry']),
         ('kernel32', info['kernel32']['exp'].get('BaseThreadInitThunk'))]
def bfs(use_refs):
    par = {}
    q = collections.deque()
    for r in roots:
        if r[1] is None: continue
        rr = (r[0], owner(r[0], r[1]) or r[1])
        par[rr] = None; q.append(rr)
    while q:
        u = q.popleft()
        nxt = set(edges.get(u, ()))
        if use_refs: nxt |= set(refs.get(u, ()))
        for v in nxt:
            if v not in par:
                par[v] = u; q.append(v)
    return par

def chain(par, v):
    out = []
    while v is not None:
        out.append(v); v = par[v]
    return out[::-1]

if __name__ == '__main__':
    pd = bfs(False); pr = bfs(True)
    print('직접 호출만으로 닿는 함수 수:', len(pd), ' (주소 꺼냄 포함:', len(pr), ')')
    for a in sys.argv[1:]:
        mod = 'KernelBase'
        if ':' in a: mod, a = a.split(':')
        if a.startswith('0x'): rva = int(a, 16)
        else:
            # 이름 → rva (Ghidra index)
            rva = None
            idxp = os.path.join(post.ROOT, 'ms', 'decomp', mod.lower(), '_index.tsv')
            for l in open(idxp, errors='replace'):
                c = l.rstrip('\n').split('\t')
                if c[0] == a and len(c) > 2: rva = int(c[2], 16); break
            if rva is None:
                print(a, '이름 없음'); continue
        f = owner(mod, rva) or rva
        key = (mod, f)
        if key in pd:
            print('%s:%s  직접 호출로 닿음: %s' % (mod, nm(mod, f), ' -> '.join(nm(m, r) for m, r in chain(pd, key))))
        elif key in pr:
            print('%s:%s  (주소 꺼냄 포함시) 닿음: %s' % (mod, nm(mod, f), ' -> '.join(nm(m, r) for m, r in chain(pr, key))))
        else:
            print('%s:%s  루트에서 닿는 길 없음' % (mod, nm(mod, f)))

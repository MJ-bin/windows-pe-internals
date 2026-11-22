#!/usr/bin/env python3
"""sast/117: objdump -d 로 풀어 TEB 칸 (39칸 대상) 접근 줄을 찾는다.
사용: tebscan_other.py OUT.tsv FILE [FILE...]   (FILE = System32 의 dll/exe)
추적: mov REG,gs:0x30 → REG = TEB + 0, lea/add/mov 로 따라간다. 함수 (.pdata) 시작마다 초기화.
"""
import re, sys, subprocess, os, bisect, struct
import pefile
from multiprocessing import Pool

FIELD_DEFS = [
 ('ActiveRpcHandle',0x50,8),
 ('CsrClientThread',0x70,8),
 ('Win32ThreadInfo',0x78,8),
 ('User32Reserved',0x80,0x68),
 ('UserReserved',0xe8,0x14),
 ('WOW32Reserved',0x100,8),
 ('ReservedForDebuggerInstrumentation',0x110,0x80),
 ('GdiTebBatch',0x2f0,0x4e8),
 ('GdiCachedProcessHandle',0x7e8,8),
 ('GdiClientPID',0x7f0,4),
 ('GdiClientTID',0x7f4,4),
 ('GdiThreadLocalInfo',0x7f8,8),
 ('Win32ClientInfo',0x800,0x1f0),
 ('glDispatchTable',0x9f0,0x748),
 ('glReserved1',0x1138,0xe8),
 ('glReserved2',0x1220,8),
 ('glSectionInfo',0x1228,8),
 ('glSection',0x1230,8),
 ('glTable',0x1238,8),
 ('glCurrentRC',0x1240,8),
 ('glContext',0x1248,8),
 ('StaticUnicodeBuffer',0x1268,0x20a),
 ('ReservedForNtRpc',0x1698,8),
 ('PerflibData',0x1728,8),
 ('EtwTraceData',0x1730,8),
 ('WinSockData',0x1738,8),
 ('GdiBatchCount',0x1740,4),
 ('GuaranteedStackBytes',0x1748,4),
 ('ReservedForPerf',0x1750,8),
 ('ReservedForOle',0x1758,8),
 ('SavedPriorityState',0x1768,8),
 ('ReservedForCodeCoverage',0x1770,8),
 ('NlsCache',0x17a0,8),
 ('pShimData',0x17a8,8),
 ('TxnScopeEnterCallback',0x17f0,8),
 ('TxnScopeExitCallback',0x17f8,8),
 ('TxnScopeContext',0x1800,8),
 ('ReservedForWdf',0x1818,8),
 ('ReservedForCrt',0x1820,8),
]
FIELD_DEFS.sort(key=lambda x: x[1])
F_START = [f[1] for f in FIELD_DEFS]

def field_of(off):
    i = bisect.bisect_right(F_START, off) - 1
    if i < 0: return None
    n, s, sz = FIELD_DEFS[i]
    if s <= off < s + sz: return n
    return None

def field_exact(off):
    for n, s, sz in FIELD_DEFS:
        if s == off and sz <= 8 and s >= 0x100: return n
    return None

KMODE = bool(os.environ.get('KMODE'))
ALIAS = {}
for r, (a, b, c, d) in {'rax':('eax','ax','al','ah'),'rcx':('ecx','cx','cl','ch'),'rdx':('edx','dx','dl','dh'),
                        'rbx':('ebx','bx','bl','bh'),'rsp':('esp','sp','spl',''),'rbp':('ebp','bp','bpl',''),
                        'rsi':('esi','si','sil',''),'rdi':('edi','di','dil','')}.items():
    ALIAS[r] = (r, 64); ALIAS[a] = (r, 32); ALIAS[b] = (r, 16); ALIAS[c] = (r, 8)
    if d: ALIAS[d] = (r, 8)
for i in range(8, 16):
    ALIAS[f'r{i}'] = (f'r{i}', 64); ALIAS[f'r{i}d'] = (f'r{i}', 32)
    ALIAS[f'r{i}w'] = (f'r{i}', 16); ALIAS[f'r{i}b'] = (f'r{i}', 8)
VOLATILE = ('rax','rcx','rdx','r8','r9','r10','r11')

PURE_STORE = {'mov','movups','movaps','movdqu','movdqa','movq','movd','movss','movsd','movapd','movupd','vmovups',
              'vmovdqu','vmovaps','vmovdqa','vmovq','vmovd','movnti','movntdq','movdir64b','vmovdqu8','vmovdqu16',
              'vmovdqu32','vmovdqu64','stmxcsr','fstp','fst','fist','fistp','fnstcw','fnstsw','movbe','vmovss','vmovsd',
              'setne','sete','seta','setb','setae','setbe','setg','setl','setge','setle','setns','sets','setnz','setz','setp','setnp','seto','setno','setnb','setnbe','setna','setnae','setng','setnl','setnle','setnge'}
RMW = {'add','sub','and','or','xor','inc','dec','neg','not','bts','btr','btc','xadd','cmpxchg','xchg','shl','shr','sar','sal','rol','ror','adc','sbb','cmpxchg16b','cmpxchg8b','rcl','rcr'}
READ_ONLY_FIRST = {'cmp','test','bt','push','jmp','call','ret','nop','ucomiss','ucomisd','comiss','comisd','prefetchw','prefetcht0','prefetchnta','clflush','fld','fild','fadd','fmul','fcom','fcomp','fxch','invlpg','lgdt','sgdt'}
PREFIXES = {'lock','rep','repz','repnz','repe','repne','bnd','notrack','data16','cs','ds','es','ss','xacquire','xrelease','rex','rex.w','rex.wb','rex.wrxb','rex.wr','rex.wx','rex.b','rex.r','rex.x','fs'}

INS_RE = re.compile(r'^\s*([0-9a-f]+):\t(.*)$')
NUM_RE = re.compile(r'^0x[0-9a-f]+$')
MEM_RE = re.compile(r'^(?:(BYTE|WORD|DWORD|QWORD|XMMWORD|YMMWORD|ZMMWORD|TBYTE|FWORD|OWORD|TWORD) PTR )?(?:(gs|fs|cs|ds|es|ss):)?(.*)$')
SZ = {'BYTE':1,'WORD':2,'DWORD':4,'QWORD':8,'XMMWORD':16,'YMMWORD':32,'ZMMWORD':64,'TBYTE':10,'FWORD':6,'OWORD':16,'TWORD':10}

def split_ops(s):
    ops = []; depth = 0; cur = ''
    for ch in s:
        if ch == '[': depth += 1
        if ch == ']': depth -= 1
        if ch == ',' and depth == 0:
            ops.append(cur.strip()); cur = ''
        else: cur += ch
    if cur.strip(): ops.append(cur.strip())
    return ops

def parse_mem(op):
    """(size, seg, base, index, disp) 또는 None"""
    if '[' not in op and 'gs:' not in op: return None
    m = MEM_RE.match(op)
    if not m: return None
    size, seg, rest = m.groups()
    sz = SZ.get(size, 0)
    rest = rest.strip()
    if rest.startswith('['):
        inner = rest[1:rest.index(']')]
        disp = 0; base = None; idx = None
        for t in re.findall(r'[+-]?[^+-]+', inner):
            sign = -1 if t.startswith('-') else 1
            t = t.lstrip('+-').strip()
            if NUM_RE.match(t): disp += sign * int(t, 16)
            elif '*' in t: idx = t
            elif t in ALIAS or t == 'rip':
                if base is None: base = t
                else: idx = t
        return sz, seg, base, idx, disp
    if NUM_RE.match(rest):
        return sz, seg, None, None, int(rest, 16)
    return None

def load_funcs(path):
    pe = pefile.PE(path, fast_load=True)
    if pe.FILE_HEADER.Machine != 0x8664:
        return None, None, None
    ib = pe.OPTIONAL_HEADER.ImageBase
    d = pe.OPTIONAL_HEADER.DATA_DIRECTORY[3]
    starts = set()
    if d.VirtualAddress and d.Size:
        data = pe.get_data(d.VirtualAddress, d.Size)
        for i in range(0, len(data) - 11, 12):
            b, e, u = struct.unpack_from('<III', data, i)
            if b == 0: continue
            try:
                fl = pe.get_data(u & ~1, 1)[0] >> 3
            except Exception:
                fl = 0
            if fl & 4: continue          # 연결된 조각 (chained) 은 부모 함수의 이어짐 — 상태 초기화 안 함
            starts.add(ib + b)
    pe.close()
    return ib, starts, True

def merge(a, b):
    r = dict(a)
    for k, v in b.items():
        if k in r and r[k] != v: del r[k]
        elif k not in r: r[k] = v
    return r

JCC_RE = re.compile(r'^j[a-z]+$')

def scan(path):
    out = []
    try:
        ib, starts, ok = load_funcs(path)
    except Exception as e:
        return path, [], 'pe-error %s' % e
    if not ok:
        return path, [], 'not-x64'
    p = subprocess.Popen(['objdump', '-d', '-M', 'intel', '--no-show-raw-insn', path],
                         stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, errors='replace')
    state = {}   # reg64 -> delta from TEB
    kthr = set()
    pending = {}
    flow_open = True
    tebcalls = set()
    if KMODE:
        pe2 = pefile.PE(path); pe2.parse_data_directories()
        try:
            for e in pe2.DIRECTORY_ENTRY_IMPORT:
                for i in e.imports:
                    if i.name and i.name.decode() in ('PsGetCurrentThreadTeb', 'PsGetThreadTeb'):
                        tebcalls.add(i.address)
        except Exception: pass
        pe2.close()
    for line in p.stdout:
        m = INS_RE.match(line)
        if not m: continue
        addr = int(m.group(1), 16)
        if addr in starts:
            state = {}; kthr.clear(); pending.clear(); flow_open = True
        if addr in pending:
            ps = pending.pop(addr)
            state = merge(state, ps) if flow_open else dict(ps)
            flow_open = True
        elif not flow_open:
            state = {}; flow_open = True
        has_gs = 'gs:' in line
        if not state and not has_gs:
            # 'unk' (대조용) 만 보려면 [reg+0x12xx] 꼴이 있는지 빠르게 거른다
            if not (KMODE and ('+0xf0]' in line or 'gs:0x188' in line or 'call' in line)):
                if '+0x1' not in line and '+0x7f' not in line and '+0x8' not in line and '+0x9' not in line: continue
        txt = m.group(2).split('#')[0].strip()
        toks = txt.split(None, 1)
        if not toks: continue
        mn = toks[0]; ops = toks[1] if len(toks) > 1 else ''
        while (mn.lower() in PREFIXES or mn.lower().startswith('rex')) and ops:
            t2 = ops.split(None, 1); mn = t2[0]; ops = t2[1] if len(t2) > 1 else ''
        opl = split_ops(ops) if ops else []
        if (JCC_RE.match(mn) or mn in ('loop', 'loope', 'loopne', 'jrcxz')) :
            mt = re.match(r'^(0x[0-9a-f]+)\s*$', ops.strip())
            if mt and state:
                t = int(mt.group(1), 16)
                if t > addr:
                    pending[t] = merge(pending[t], state) if t in pending else dict(state)
            if mn == 'jmp': flow_open = False
            continue
        if mn in ('ret', 'retn', 'ud2', 'hlt', 'int3'):
            flow_open = False
            continue
        if mn == 'call':
            # 메모리 피연산자 (call QWORD PTR gs:... 등) 도 본 다음 volatile 비움
            for i, op in enumerate(opl):
                mm = parse_mem(op)
                if mm:
                    sz, seg, b, idx, disp = mm
                    off = None
                    if seg == 'gs' and b is None: off = disp
                    elif b in ALIAS and ALIAS[b][1] == 64 and ALIAS[b][0] in state and seg is None: off = state[ALIAS[b][0]] + disp
                    if off is not None:
                        f = field_of(off)
                        if f: out.append((addr, f, off, 'R-call', 0, txt))
            for r in VOLATILE: state.pop(r, None); kthr.discard(r)
            if KMODE and tebcalls:
                mt = re.search(r'#\s*(0x[0-9a-f]+)', line)
                if mt and int(mt.group(1), 16) in tebcalls: state['rax'] = 0
            continue
        if mn in ('syscall', 'sysenter', 'cpuid', 'rdtsc', 'rdtscp', 'xgetbv', 'rdmsr'):
            for r in ('rax', 'rcx', 'rdx', 'rbx') + VOLATILE: state.pop(r, None)
            continue
        dest = opl[0] if opl else ''
        src = opl[1] if len(opl) > 1 else ''
        # 1) TEB 로드: mov REG, gs:0x30
        if mn == 'mov' and dest in ALIAS and ALIAS[dest][1] == 64:
            d = ALIAS[dest][0]
            mm = parse_mem(src) if ('[' in src or 'gs:' in src) else None
            if mm and mm[1] == 'gs' and mm[2] is None and mm[3] is None and mm[4] == 0x30:
                state[d] = 0; continue
        if KMODE and mn == 'mov' and dest in ALIAS and ALIAS[dest][1] == 64:
            dd = ALIAS[dest][0]
            mmk = parse_mem(src) if ('[' in src or 'gs:' in src) else None
            if mmk and mmk[1] == 'gs' and mmk[2] is None and mmk[4] == 0x188:
                kthr.add(dd); state.pop(dd, None); continue
            if mmk and mmk[1] is None and mmk[2] in ALIAS and (ALIAS[mmk[2]][0] in kthr or os.environ.get('KMODE') == '2') and mmk[3] is None and mmk[4] == 0xf0 and mmk[0] == 8:
                state[dd] = 0; kthr.discard(dd); continue
            if src in ALIAS and ALIAS[src][1] == 64 and ALIAS[src][0] in kthr:
                kthr.add(dd); state.pop(dd, None); continue
            kthr.discard(dd)
        # 2) 메모리 피연산자 훑기
        newtracked = None
        for i, op in enumerate(opl):
            mm = parse_mem(op)
            if not mm: continue
            sz, seg, b, idx, disp = mm
            off = None; how = None
            if seg == 'gs' and b is None:
                off = disp; how = 'gs'
                if mn == 'mov' and i == 1 and dest in ALIAS and disp == 0x30: continue
            elif seg is None and b in ALIAS and ALIAS[b][1] == 64 and ALIAS[b][0] in state:
                off = state[ALIAS[b][0]] + disp; how = 'reg'
            elif seg is None and b is not None and b not in ('rsp','rbp','rip') and idx is None and mn != 'lea':
                f2 = field_exact(disp)
                if f2: out.append((addr, f2, disp, 'unk-' + ('W' if (i == 0 and (mn in PURE_STORE or mn in RMW)) else 'R'), sz, txt))
                continue
            if off is None: continue
            f = field_of(off)
            if mn == 'lea' and i == 1:
                if dest in ALIAS and ALIAS[dest][1] == 64 and how == 'reg':
                    newtracked = (ALIAS[dest][0], off)
                    if f: out.append((addr, f, off, 'lea', 0, txt))
                continue
            if not f: continue
            if i == 0 and mn in PURE_STORE: kind = 'W'
            elif i == 0 and mn in RMW: kind = 'RW'
            elif mn in ('xchg', 'xadd', 'cmpxchg') : kind = 'RW'
            else: kind = 'R'
            if idx: kind += '-idx'
            out.append((addr, f, off, kind, sz, txt))
        # 3) 레지스터 상태 갱신
        if newtracked:
            state[newtracked[0]] = newtracked[1]
            continue
        if dest in ALIAS and mn not in READ_ONLY_FIRST and not (mn in PURE_STORE and False):
            r, w = ALIAS[dest]
            if mn in ('mov',) and w == 64 and src in ALIAS and ALIAS[src][1] == 64 and ALIAS[src][0] in state:
                state[r] = state[ALIAS[src][0]]; continue
            if mn in ('add', 'sub') and w == 64 and r in state and NUM_RE.match(src):
                v = int(src, 16); state[r] += v if mn == 'add' else -v
                f = field_of(state[r])
                if f: out.append((addr, f, state[r], 'lea', 0, txt))
                continue
            if mn == 'xchg' and src in ALIAS: state.pop(ALIAS[src][0], None)
            if mn.startswith('cmp') and not mn.startswith('cmpxchg'): continue
            if mn.startswith('j') or mn.startswith('ucomi') or mn.startswith('comi'): continue
            state.pop(r, None)
        if mn in ('mul', 'imul', 'div', 'idiv', 'cqo', 'cdq', 'cdqe', 'cwde', 'cwd', 'cbw', 'lahf', 'rdrand', 'rdseed') and len(opl) < 2:
            state.pop('rax', None); state.pop('rdx', None)
        if mn.startswith(('movs', 'stos', 'lods', 'scas', 'cmps')) and not opl:
            for r in ('rax', 'rcx', 'rsi', 'rdi'): state.pop(r, None)
        if mn in ('pop',) and dest in ALIAS: state.pop(ALIAS[dest][0], None)
    p.wait()
    return path, out, 'ok'

def main():
    outp = sys.argv[1]
    files = sys.argv[2:]
    if files and files[0].startswith('@'):
        files = [l.rstrip('\n') for l in open(files[0][1:]) if l.strip()]
    with Pool(min(28, len(files))) as pool, open(outp, 'w') as fo:
        for path, evs, st in pool.imap_unordered(scan, files, chunksize=1):
            name = os.path.basename(path)
            if st != 'ok':
                fo.write('#\t%s\t%s\n' % (name, st))
            for (addr, f, off, kind, sz, txt) in evs:
                fo.write('%s\t%x\t%s\t%x\t%s\t%d\t%s\n' % (name, addr, f, off, kind, sz, txt))
            fo.flush()

if __name__ == '__main__':
    main()

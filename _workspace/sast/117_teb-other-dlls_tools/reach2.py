import sys, re, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reach, post
K='KernelBase'
ib=reach.info[K]['ib']
F0=0x4e6c0            # _KernelBaseBaseDllInitialize
START=0x18004e855     # reason==1 (PROCESS_ATTACH) 갈래 첫 줄
# 함수 안 명령 읽기
ins={}
order=[]
for line in open('KernelBase.dis',errors='replace'):
    m=reach.INS.match(line)
    if not m: continue
    a=int(m.group(1),16)
    r=a-ib
    if F0<=r<F0+1835:
        ins[a]=m.group(2); order.append(a)
nxt={a:(order[i+1] if i+1<len(order) else None) for i,a in enumerate(order)}
seen=set(); st=[START]
calls=[]
while st:
    a=st.pop()
    if a in seen or a not in ins: continue
    seen.add(a)
    t=ins[a].split('#')[0].strip()
    mn=t.split(None,1)[0]
    mj=re.match(r'^(j\w+)\s+(0x[0-9a-f]+)$',t)
    if mj:
        tgt=int(mj.group(2),16)
        st.append(tgt)
        if mj.group(1)!='jmp': st.append(nxt[a])
        continue
    if mn.startswith('ret'): continue
    if mn=='jmp': continue
    mc=reach.CALL_DIRECT.match(t)
    if mc and mc.group(1)=='call': calls.append(('direct',a-ib,int(mc.group(2),16)-ib))
    else:
        mm=reach.CALL_MEM.search(ins[a])
        if mm and 'call' in t:
            slot=int(mm.group(1),16)
            if slot in reach.info[K]['iat']: calls.append(('iat',a-ib,reach.info[K]['iat'][slot]))
    st.append(nxt[a])
print('PROCESS_ATTACH 갈래 안 호출 지점 수',len(calls))
for c in sorted(calls): 
    if c[0]=='direct': print('  +0x%x -> %s'%(c[1]-F0,reach.nm(K,reach.owner(K,c[2]) or c[2])))
    else: print('  +0x%x -> (import) %s!%s'%(c[1]-F0,c[2][0],c[2][1]))

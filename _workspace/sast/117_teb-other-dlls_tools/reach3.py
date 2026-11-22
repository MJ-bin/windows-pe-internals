import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reach, post
K='KernelBase'
def rva_of(mod,name):
    for l in open(os.path.join(post.ROOT,'ms','decomp',mod.lower(),'_index.tsv'),errors='replace'):
        c=l.rstrip('\n').split('\t')
        if c[0]==name and len(c)>2: return int(c[2],16)
ent=reach.owner(K,reach.info[K]['entry'])
# entry 에서 부르는 것 (_KernelBaseBaseDllInitialize 쪽 간선은 process-attach 갈래만으로 대체)
F0=0x4e6c0
roots=[]
for v in reach.edges[(K,ent)]:
    if v[1]==0x51d0: continue    # KernelBaseBaseDllInitialize (아래에서 갈래로 대체)
    roots.append(v)
pa=['BaseNlsDllInitialize','ConsoleInitialize','CsrpLocalSetupForSecureProcess','DisableRegCachingForConstrainedImpersonation','KernelBaseDllInitializeMemoryManager','StringCbPrintfW']
for n in pa:
    r=rva_of('kernelbase',n); roots.append((K,reach.owner(K,r)))
# kernel32 entry + BaseThreadInitThunk (보수적: 함수 전체)
k32=reach.info['kernel32']
for r in (k32['entry'],k32['exp'].get('BaseThreadInitThunk')):
    roots.append(('kernel32',reach.owner('kernel32',r)))
# BFS: 간선에서 F0 의 이웃 제거 (thread attach/detach 갈래 때문에)
edges=dict((k,set(v)) for k,v in reach.edges.items())
edges[(K,F0)]=set()
par={}
q=collections.deque()
for r in roots:
    if r not in par: par[r]=None; q.append(r)
while q:
    u=q.popleft()
    for v in edges.get(u,()):
        if v not in par: par[v]=u; q.append(v)
def chain(v):
    out=[]
    while v is not None: out.append(v); v=par[v]
    return out[::-1]
names=sys.argv[1:] or ['InternalLcidToName','CheckSpecialLocales','CompareStringW','GetSortNode','CompareStringA','GetStringTypeExW','GetCurrentNlsCache','NlsValidateLocale','GetLocaleInfoHelper','BaseCopyStream','SetThreadStackGuarantee','SwitchToFiberContext','Wow64SetThreadDefaultGuestMachine','SetThreadPriority','FatalAppExitA']
print('process-attach 갈래 + kernel32 시작 뒤 직접 호출로 닿는 KernelBase·kernel32 함수 수:',len(par))
for n in names:
    r=int(n,16) if n.startswith('0x') else rva_of('kernelbase',n)
    f=reach.owner(K,r) or r
    key=(K,f)
    if key in par: print('닿음  ',reach.nm(K,f),' <= ',' -> '.join(reach.nm(m,x) for m,x in chain(key)))
    else: print('안 닿음',reach.nm(K,f))

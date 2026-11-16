# 109 ntdll-user-structs — 정적 분석 결과 (Q78e1: 유저 구조체 칸)

- 요청서: `_workspace/sast/109_ntdll-user-structs.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`). 커널 `MmCreatePeb` 등은 **이미 있는 `.asm` 읽기만 + 커널 objdump(스크래치) 읽기만**. **ntoskrnl Ghidra 프로젝트는 열지 않았다** (sast/108 이 도는 중이라서). 다른 DLL (user32·gdi32·gdi32full·kernel32·KernelBase·win32kbase) 은 objdump 로만 봤다 (`.asm` 안 뽑음).
- **PDB 일치**: ntdll `ntdll.pdb` guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`). ntoskrnl `ntkrnlmp.pdb` `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) = 기준값. 둘 다 sast/48 이 사진 로그와 대조한 값과 같다.
- 분석: 이미 돼 있어서 다시 안 걸었다 (ntdll `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음). Ghidra 프로세스는 내가 시작할 때 없었고, 내 export 한 번(ntdll 프로젝트) 말고 안 돌렸다.
- **새로 뽑은 함수 11개, 전부 depth 0** (`run_ghidra.sh export ntdll.dll '^(…)$' 0`, exit 0, 몇 초): `LdrpFindExistingModule` · `LdrpIncrementModuleLoadCount` · `LdrpUnloadNode` · `LdrpLoadShimEngine` · `LdrpGetProcApphelpCheckModule` · `AVrfpLoadAndInitializeProvider` · `RtlpRemoveVectoredHandler` · `SetAssertBufferPtrinPeb` · `RtlSetProcessPlaceholderCompatibilityMode` · `RtlpWakeByAddress` · `LdrpDecrementNodeLoadCountLockHeld`. 이유: 아래 칸 쓰는 줄이 든 함수인데 `.asm` 이 없었다. `_calls.tsv` 4,083 → 4,192 줄 (`comm -23` 0줄 — 안 사라짐).
- 이미 있어서 **읽기만 한 것** (다시 안 뽑음): `LdrpAllocateModuleEntry` · `LdrpInsertDataTableEntry` · `LdrpInitializeNtdllDataTableEntry` · `LdrpProcessMappedModule` · `LdrpCompleteMapModule` · `LdrpInitializeNode` · `LdrpInitializeProcess` · `_LdrpInitialize` · `LdrpInitializeExecutionOptions` · `LdrpInitializeNlsInfo` · `LdrpInitializePatchData` · `LdrpSendPostSnapNotifications` · `SbUpdateSwitchContextBasedOnDll` · `LdrpCondenseGraphRecurse` · `LdrpMergeNodes` · `LdrpPrepareModuleForExecution` · `LdrpAddNodeServiceTag` · `LdrpSetModuleSigningLevel` · `LdrpValidateIntegrityContinuity` · `LdrpMapDllSearchPath` · `LdrpMapDllNtFileName` · `LdrpApplyPatchImage` · `RtlpAddVectoredHandler` · `RtlpCallVectoredHandlers` · `LdrpFreeReplacedModule` 등 (`.asm`). 커널 `MmCreatePeb.asm` · `DbgkCopyProcessDebugPort.asm` (읽기만).
- **훑기 방법 + 한계 (한 줄씩)**
  - PEB: ntdll 전체 objdump 풀이(스크래치 `ntdll.dis`, 421,500줄)에서 **레지스터 추적** (`gs:0x60` · `[TEB+0x60]` · 레지스터 복사 · 스택 칸 · `add/lea` 로 오프셋 이동 · 직접 호출/꼬리 점프에 PEB 를 인자로 넘기면 받은 함수에 씨앗 7라운드 · 분기 합침). 희귀 오프셋 (`≥ 0x2c8`) 은 추적 없이 **전체 바이너리 오프셋 일치**로 한 번 더. 한계: PEB 포인터를 전역·구조체 칸·간접 호출 인자로 받으면 놓칠 수 있다. 작은 오프셋 (`+0x1`·`+0x28`·`+0x40`…) 은 추적 없이는 오탐이 수천 줄이라 추적 결과만 믿는다.
  - 모듈 항목: 포인터를 얻는 방식이 여럿이라 (할당 반환 · 목록 고리 7종 `+0x0/+0x10/+0x20/+0x70/+0xa0/+0xc8/+0xe0` 기준 · 노드 `Modules`) **오프셋+고리 기준 훑기** (로더 이름 함수 `Ldr*`·`AVrf*`·지도 안 함수만) + `lea 칸주소` 훑기 (출력 인자로 넘기는 칸). 눈으로 오탐을 가렸다. 한계: 지도 밖 함수·색인 꼴 `[r+i*8+..]` 쓰기 놓칠 수 있음. `Flags` 는 `or/and/bts/btr/mov` 꼴 **전수**로 봤다 (아래 3.5 표).
  - 커널: `[EPROCESS+0x2e0]` 로 읽은 PEB 포인터를 따라가는 **직선 훑기** (분기 합침 없음). 첫 판은 `test r,r` 이 레지스터 표시를 지워서 `PspApplyWorkingSetLimitsToProcess` 를 놓쳤다 → 고쳐서 다시 돌렸다. **그래서 nt 쪽 "못 찾음" 은 "없다" 가 아니라 `[공백]`** 이다.
  - 다른 DLL: user32 · gdi32 · gdi32full 은 PEB 추적, kernel32 · KernelBase 는 `gs:0x60` 뒤 25줄 창 훑기 (같은 추적을 kernel32 에 돌렸더니 오탐 3만 줄이라 버렸다), win32kbase 는 `GdiProcessCallout` 하나만 직접.
- **버전 확인: 일치.** 알려진 사실 `_calls.tsv` 대조: ntdll `LdrInitializeThunk +0x9 / +0xe → LdrpInitialize` · `_LdrpInitialize +0x167 / +0x16c → LdrpInitializeProcess` · `LdrpInitializeProcess +0x1bb6 / +0x1bbb → LdrpInitializeGraphRecurse` (옆에 `+0x16e6`·`+0x193c` 둘 더) · `LdrpInitializeNode +0x166 / +0x16b → LdrpCallTlsInitializers` · `+0x197 / +0x19c → LdrpCallInitRoutine`. 커널 `NtCreateUserProcess +0xc31 / +0xc36 → PspAllocateProcess` · `PspInsertThread +0x802 / +0x807 → PspCallProcessNotifyRoutines`. 이번 길: `NtCreateUserProcess +0xfbd / +0xfc2 → PspInsertProcess`.
- **오프셋 대조**: 아래 `+0x` 는 전부 Ghidra `.asm` 줄에서 다시 찾아 맞춰 봤다 (새로 뽑은 11개 + 있던 것, 64곳). objdump 풀이의 `함수 시작 + 오프셋` 이 `.asm` 와 한 줄도 안 어긋났다.

## 스크립트 문제

없다. `run_ghidra.sh export` 한 번, `pdb_struct.py` (`_PEB` · `_PEB_LDR_DATA` · `_LDR_DATA_TABLE_ENTRY` · `_LDR_DDAG_NODE`) 문제없이 돌았다. 임시 스크립트는 전부 스크래치 (`…/scratchpad/s109/`, 레포 밖). 제안은 맨 아래.

## 판단 기준 (한 줄)

sast/46·47·48 과 같다. **길** = 커널 `NtCreateUserProcess` 아래 (`PspAllocateProcess` · `PspInsertProcess` · `MmCreatePeb`) + ntdll **지도 안** (`_map/LdrInitializeThunk_nodes.tsv`, 1,268개). 길 안 줄에 칸을 쓰는 줄이 있고 값이 0 (명시 0 저장 · 입력값 0 쓰기 · 같은 DWORD 되쓰기) 이면 "0 을 씀"; 켜는 줄이 길 안에 있는데 test.exe 가 조건을 안 채우면 "조건부 안 탐"; 길 안 어디에도 쓰는 줄이 없고 이름 있는 함수가 길 밖에서 쓰면 "범위 밖 길" (ntdll · nt · 다른 DLL); 어디서도 못 찾으면 "아무도 안 씀 (초기화 길 안)". 몸통 0 채움만으로는 안 센다.

- **경계 사례 (한 곳)**: 모듈 항목 `Flags` 에는 켜는 줄이 아예 없는 비트가 10개 있다. 그런데 exe 항목을 만들 때 `LdrpInitializeProcess +0x13be ~ +0x13c4` 가 `mov eax,[rcx+0x68]; or eax,4; mov [rcx+0x68],eax` 로 **같은 DWORD 를 되쓴다** (`.asm` 줄 확인). 요청서 기준 ("같은 DWORD 되쓰기") 대로 이 10비트는 **"0 을 씀"** 으로 셌다. 되쓰기를 "0 을 씀"으로 안 치면 이 10칸은 "아무도 안 씀"이 된다 — 아래 요약 표에 두 숫자를 다 적었다.
- 겹치는 칸: `FlagGroup` (`?[4B]`, +0x68) 은 `Flags` 와 **같은 DWORD**. `KernelCallbackTable` · `UserSharedInfoPtr` 는 PEB `+0x58` 한 칸에 겹친다.

## 요약 표 (93칸 — 요청서는 "약 92", PDB 칸 이름을 다시 세니 63 + 2 + 24 + 4 = 93)

| 구조체 · 구간 | 칸 | 아는 칸 | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 | 범위 밖 ntdll | 범위 밖 nt | 범위 밖 다른 DLL·모듈 | 모름 |
|---|---|---|---|---|---|---|---|---|---|
| `_PEB` `P1~P1a` | 1 | **1** | — | — | — | — | — | — | 0 |
| `_PEB` `P1a~P1b` | 1 | **1** | — | — | — | — | — | — | 0 |
| `_PEB` `끝까지 0` | 61 | — | **4** | **9** | **26** | **4** | **6** | **12** | 0 |
| `_PEB_LDR_DATA` `끝까지 0` | 2 | — | — | — | **2** | — | — | — | 0 |
| `_LDR_DATA_TABLE_ENTRY` `P1~P1a` | 3 | **3** | — | — | — | — | — | — | 0 |
| `_LDR_DATA_TABLE_ENTRY` `P1a~P1b` | 1 | **1** | — | — | — | — | — | — | 0 |
| `_LDR_DATA_TABLE_ENTRY` `끝까지 0` | 20 | — | **11** (명시 1 · 되쓰기 10) | **9** | 0 | — | — | — | 0 |
| `_LDR_DDAG_NODE` `P1a~P1b` | 1 | **1** | — | — | — | — | — | — | 0 |
| `_LDR_DDAG_NODE` `끝까지 0` | 3 | — | **1** | **2** | 0 | — | — | — | 0 |
| **합** | **93** | **7** | **16** | **20** | **28** | **4** | **6** | **12** | **0** |

- 되쓰기를 "0 을 씀"으로 안 칠 때: 0 을 씀 **6** (PEB 4 · 항목 `Lock` · 노드 `CondenseLink`) · 아무도 안 씀 **38** (28 + 10), 나머지 같음.
- 합 확인: 7 + 16 + 20 + 28 + (4 + 6 + 12) = 93.
- baggage 2장 합치기 제안 (run54 방식): `_PEB` 모름 63 → **0** (아는 칸 +2 · 0 을 씀 4 · 조건부 9 · 아무도 안 씀 26 · 범위 밖 22), `_PEB_LDR_DATA` 모름 2 → 0, `_LDR_DATA_TABLE_ENTRY` 모름 24 → 0 (아는 칸 +4 · 0 을 씀 11 · 조건부 9), `_LDR_DDAG_NODE` 모름 4 → 0 (아는 칸 +1 · 0 을 씀 1 · 조건부 2). 모름 301 → **208**.

## 한눈에

- **`PEB`·모듈 항목 칸 대부분은 "쓰는 줄이 아예 없다"가 답이다.** PEB 61칸 중 26칸은 ntdll·nt·(본 DLL 다섯)에 쓰는 줄이 없다 (정렬 빈칸 · 옛 칸 · ARM64 전용 · 요청서가 IFEO 라고 짐작한 `IFEOKey`·`ReadImageFileExecOptions` 둘 포함).
- **이 61칸 중 IFEO 가 쓰는 PEB 칸은 둘뿐이다**: `TracingFlags` (`LdrpInitializeExecutionOptions +0x2b6`) · `NtGlobalFlag2` (`+0x5a9`) (`NtGlobalFlag` · `MinimumStackCommit` 은 이미 아는 칸). `IFEOKey` 는 키 핸들을 지역 변수 `[rsp+0x68]` 에만 두고 PEB 에 안 넣는다.
- **`MmCreatePeb` 는 이 61칸 중 4칸에 0 을 쓴다**: `LeapSecondFlags` (`+0x57a AND dword [RBX+0x7c0],0` — 명시 0) 와 그 비트 둘, `NtGlobalFlag2` (`+0x4b2`, 커널 전역 복사 — 값 0). 나머지는 `MiCreatePebOrTeb` 가 준 새 0 페이지다 (`MmCreatePeb` 의 `memset` 은 지역 견본용 `+0x78` 하나뿐, PEB 가 아님).
- **`FlagGroup`·`NodeModuleLink`·`LoadTime` 은 모두 `LdrpAllocateModuleEntry` / `LdrpProcessMappedModule` 이 쓴다** (이미 길 안, 사진 `P1~P1a` 와 맞음). `LoadTime` 은 KUSD `SystemTime` (`0x7ffe0014`) 을 8바이트로 베낀 값.
- **`SwitchBackContext` 는 요청서 짐작대로** `SbUpdateSwitchContextBasedOnDll +0xd9`. **`PreorderNumber` 는 짐작과 달랐다**: 그래프 걷기 (`LdrpInitializeGraphRecurse`) 가 아니라 **`LdrpCondenseGraphRecurse +0x1e`** (같은 함수가 `LowestLink` · `CondenseLink` 도 쓴다 — Tarjan 번호 매기기).
- **간접 호출 0개** (이 칸들을 쓰는 줄이 든 함수 안): `.asm` 의 `indirect:` 는 shim 콜아웃 둘 (`LdrpSendPostSnapNotifications +0x122`, `LdrpMapDllSearchPath +0x3b4`) · `LdrpInitShimEngine +0x160` · `LdrpInitializeProcess` 넷 (`+0x17fc` WOW64, `+0x1b59` `Kernel32ThreadInitThunkFunction`, `+0x1cdf`, `+0x1d16 [RDI+0x230]` = `PostProcessInitRoutine` 호출) · `_LdrpInitialize +0x9a` (WOW64) — 전부 이미 알려졌거나 칸을 안 쓴다.

## 질문별 답 [정적]

경로 약어 (baggage 3장 줄임 그대로): `②…` = `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess`. `②N…` = `…→ LdrpInitializeGraphRecurse → LdrpInitializeNode`. `①P…` = `NtCreateUserProcess → PspAllocateProcess`. **새로 제안하는 줄임 둘**: `①I…` = `NtCreateUserProcess (호출 +0xfbd) → PspInsertProcess`. `②C…` = `②… → LdrpPrepareModuleForExecution → LdrpCondenseGraph → LdrpCondenseGraphRecurse` (3.6 `LowestLink` 줄이 이미 쓰는 길).

### 1. `P1~P1a` 4칸

**답: 4칸 모두 줄을 찾았고 이미 아는 줄과 겹친다. 쓴 주인 전부 ntdll (사진 `P1~P1a` 와 맞음).**

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| PEB `+0x50` `CrossProcessFlags*` | ② | `_LdrpInitialize +0x107 BTS.LOCK [RSI+0x50],1` = 비트 1 `ProcessInitializing` 켬 → 값 `2`. 끄기 `+0x20f BTR.LOCK`. 3.1 의 `ProcessInitializing` 줄과 **같은 칸** (PDB 가 정수 칸·비트 칸을 따로 세서 모름에 남았을 뿐). 사진 P1 `0` → P1a `2` → P1b `2` → P2 `0` (`04_baggage-read_P1.txt:1576` · `_P1a.txt:1743` · `_P1b.txt:2441` · `_P2.txt:2453`) | 직접 (원자 비트 연산) | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x107 · +0x20f` | `[정적]` sast109 · 정확 · 사진 `P1~P1a` → 바뀜 `P1b~P2` |
| 항목 `+0x68` `FlagGroup` (= `Flags`) | ② | 새 항목 `Flags` 처음 값. 할당은 `HEAP_ZERO_MEMORY` (`LdrpAllocateModuleEntry +0x1a ADD`·`+0x20 OR EDX,8`) 뒤 `+0x10d`(비트 28, 조건부) · `+0x11a` (`ImageDll`, `LoadContext.Flags & 0x40` 꺼졌을 때) · `+0x129` (비트 26, 조건부) · `+0x194` (`ProcessStaticImport`, 조건부). ntdll 항목은 `LdrpInitializeNtdllDataTableEntry +0x93 OR 0x204`, exe 항목은 `LdrpInitializeProcess +0x13c4` (`\|4`). 사진 exe `0x22c4` · ntdll `0x20a2c4` (`_P1a.txt:2690` · `:2795`) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0x11a` 외 | `[정적]` sast109 · 3.5 `Flags` 줄과 같은 칸 · 사진 `P1~P1a` |
| 항목 `+0xa0` `NodeModuleLink` | ② | 새 노드 `Modules` 와 항목을 **서로 가리키게** 만든다: `LdrpAllocateModuleEntry +0xd4 LEA RAX,[RBX+0xa0]`, `+0xdb [RAX]=RDI`, `+0xde [RAX+8]=RDI` (노드 주소), 노드 쪽 `+0xe2 · +0xe5`. 사진 `NodeModuleLink [노드 - 노드]` (`_P1a.txt:2729`) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0xdb · +0xde` (노드 합칠 때 다시 옮기는 쪽 `LdrpCondenseGraphRecurse +0xd3 → LdrpMergeNodes`, 순환이 있어야 탐 — test.exe 는 비순환) | `[정적]` sast109 · 정확 · 사진 `P1~P1a` |
| 항목 `+0x100` `LoadTime` (8B) | ② | KUSD `SystemTime` (`mov eax,0x7ffe0014; mov rax,[rax]`) 8바이트를 베낀 값. 항목이 처음 "붙을 때" 한 번. 사진 exe·ntdll 둘 다 `0x01dd646b35091f27` (같은 값 — 둘 다 이 줄이 한 번에 쓴 것) (`_P1a.txt:2736` · `:2841`), k32 쪽 `0x01dd646bc4394db7` 은 뒤 모듈 (`_P1b.txt:3644`) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpProcessMappedModule +0xa6` (ntdll: `②… → LdrpInitializeNtdllDataTableEntry (호출 +0x11f) → LdrpProcessMappedModule`) | `[정적]` sast109 · 정확 · 사진 `P1~P1a` |

- **지금 우리 (PE_emulator)**: `CrossProcessFlags` 는 **확인 필요** (코드 안 봄). 맞게 하려면 `_LdrpInitialize` 흉내가 비트 1 을 켜고 `LdrpInitializeProcess` 끝에서 끄면 된다 (사진 `2 → 0`). `LoadTime` 은 에뮬레이터가 KUSD `SystemTime` 을 읽어 쓰면 맞고, 상수로 박으면 사진과 값이 다르다 (값 자체는 시간이라 어차피 달라짐).

### 2. `P1a~P1b` 3칸

**답: 3칸 모두 줄을 찾았다. `SwitchBackContext` 는 요청서 짐작이 맞고, `PreorderNumber` 는 짐작이 틀렸다 (그래프 걷기가 아니라 묶기).**

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| PEB `+0x330` `PatchLoaderData` | ② | **0x18 바이트 블록** 주소. `LdrpHeap` 에서 `HEAP_ZERO_MEMORY` 로 받아 (`+0x4 ~ +0x20`) `[0]=1` (`+0x2d`), `[8]=&LdrLoadDll` (`+0x33 · +0x3a`), `[0x10]=&LdrGetProcedureAddress` (`+0x3e · +0x45`), 그 주소를 PEB 에 (`+0x49 MOV RAX,GS:[0x60]` → `+0x52 MOV [RAX+0x330],RCX`). 호출은 `LdrInitState = 2` (`LdrpInitializeProcess +0x1b1e`) 바로 뒤 무조건 (`+0x1b24`, ret `+0x1b29`). 사진 P1a `(null)` → P1b `0x25996f3bfa0` (프로세스 힙 `0x25996f30000` 구역 안 → `LdrpHeap` 이 프로세스 힙과 같은 힙으로 보임 `[추측]`) (`_P1a.txt:1811` · `_P1b.txt:2509`) | 직접 | ntdll | `②… → LdrpInitializeProcess (호출 +0x1b24) → LdrpInitializePatchData +0x52` | `[정적]` sast109 · 정확 · 사진 `P1a~P1b` |
| 항목 `+0xc0` `SwitchBackContext` | ② | `SbpDetermineDllContext(entry.DllBase)` 가 돌려준 호환 문맥 블록의 **`+4` 위치 주소** (`+0xb5 ~ +0xd9`: `LEA RDI,[RDX+0x4]` → `MOV [R14+0xc0],RDI`). 인자 `r8d != 0` 이면 건너뜀 (`+0xac TEST R8D · +0xaf JNZ`). 사진 exe `0x7ffafb7d3014`, k32 쪽 `0x7ffafb7d3094` — ntdll 기준 주소 `0x7ffafb660000` (= PEB `Ldr` `0x7ffafb832880` − `PebLdr` rva `0x1d2880`) 에서 `+0x173014` · `+0x173094` = 표 `rva 0x173010 + 번호×0x20` 의 번호 0 · 4 칸 `+4` (`+0x6a ~ +0xa4` 가 그 표를 찾는 줄) `[관찰 계산]`. 노드 알림 때 모듈마다 | 직접 | ntdll | `②N… (노드 알림) LdrpSendPostSnapNotifications (호출 +0xa3 → ret +0xa8) → SbUpdateSwitchContextBasedOnDll +0xd9` (`LdrpUnloadNode +0xc5` 도 부름 — 언로드 때만) | `[정적]` sast109 · 요청서 짐작 맞음 · 정확 · 사진 `P1a~P1b` (P1a null → P1b exe 값 `_P1b.txt:3430`) |
| 노드 `+0x48` `PreorderNumber` | ② | 묶기 번호 매기기 (Tarjan): 재귀 입구에서 `inc [RDX]` (공용 카운터, `+0x13`) → `+0x19 EAX` → **`+0x1e MOV [RCX+0x48],EAX`**, 같은 값을 `LowestLink` (`+0x24 MOV [RCX+0x20],EAX`) 에도. 사진 P1a `0` → P1b exe `1` · ntdll `0` · k32 `1` · kb `2` · dll1 `2` · dll2 `3` (`_P1b.txt:3474 · 3579 · 3684 · 3789 · 3894 · 3999`; ntdll 노드는 `State 9` 라 안 걷는다) | 직접 | ntdll | `②C… (LdrpCondenseGraphRecurse 호출 …) +0x1e` — `LdrpPrepareModuleForExecution → LdrpCondenseGraph → LdrpCondenseGraphRecurse` | `[정적]` sast109 · 정확 (짐작 `LdrpInitializeGraphRecurse` 아님) · 사진 `P1a~P1b` |

- **지금 우리**: `PreorderNumber` 를 "그래프 걷기에서 번호 매김" 으로 만들었으면 **틀림** — 묶기 함수가 하는 일이다 (3.6 `LowestLink` 줄과 같은 함수). `SwitchBackContext` · `PatchLoaderData` 는 **확인 필요**; 에뮬레이터가 이 칸을 0 으로 두면 사진과 다르지만, 읽는 쪽 (호환 표 · `LdrLoadDll` 포인터 3칸) 이 있을 때만 영향.

### 3. `끝까지 0` 85칸 가르기 (PEB 61 · 항목 20 · 노드 3 · Ldr 2 → 아래 4개 묶음 + 근거)

**답: 0 을 씀 16 (PEB 4 · 항목 11 · 노드 1) · 조건부 안 탐 20 (PEB 9 · 항목 9 · 노드 2) · 아무도 안 씀 28 (PEB 26 · Ldr 2) · 범위 밖 길 22 (ntdll 4 · nt 6 · 다른 DLL·모듈 12) = 86 (= 61 + 20 + 3 + 2).** 사진 확인: 이 칸들은 `끝까지 0` 구간이라 사진 8장에서 처음부터 끝까지 0 이다 (요청서 정의).

#### 3.1 0 을 씀 (16)

| 구조체 | 칸 | 쓰는 줄 | 비고 |
|---|---|---|---|
| PEB | `LeapSecondFlags` (`+0x7c0`, DWORD) · `.SixtySecondEnabled` (비트 0) · `.Reserved` (비트 1~31) (3) | **nt `MmCreatePeb +0x57a`** `AND dword [RBX+0x7c0],0x0` (명시 0, `+0x573` 이 `LeapSecondData` 쓴 바로 뒤, 무조건) | 비트 둘은 같은 DWORD 의 0. ntdll 이 `SixtySecondEnabled` 를 **켜는** 줄은 `LdrpInitializeExecutionOptions +0x725 OR [RBX+0x7c0],1` (조건: `test byte [RBX+0x7c4],1` = `NtGlobalFlag2` 비트 0, `+0x71c` — 값 0 이라 안 탐). 이 비트는 "0 을 씀 + ntdll 조건부 켬" 둘 다다 (길 안 분류가 둘 — 0 쪽으로 셈, sast/48 의 `SameTebFlags` 방식) |
| PEB | `NtGlobalFlag2` (`+0x7c4`) (1) | **nt `MmCreatePeb +0x4ac ~ +0x4b2`** `MOV EAX,[0x140fc3a00]` (커널 `NtGlobalFlag2`, 부팅 값 0) → `MOV [RBX+0x7c4],EAX` (입력값 쓰기) | ntdll 이 IFEO `GlobalFlag2` 로 **덮는** 줄 `LdrpInitializeExecutionOptions +0x5a9` 은 조건부 안 탐 (IFEO 키 없음 — 아래 3.2). sast/107 이 말한 `MmCreatePeb` 복사가 이 줄 |
| 항목 | `Lock` (`+0x90`) (1) | `LdrpAllocateModuleEntry +0xe9 MOV qword [RBX+0x90],0x0` (명시 0) | 칸 이름 (모듈별 잠금 포인터) 으로 보아 나중에 쓰는 쪽이 있을 법하나 이 길엔 없다 `[추측]` |
| 항목 | `Flags` 안 비트 10개: `TelemetryEntryProcessed` (4) · `LoadInProgress` (12) · `EntryProcessed` (14) · `AuxIatCopyPrivate` (16) · `ReservedFlags3` (17) · `DontRelocate` (23) · `ChpeImage` (25) · `ReservedFlags5` (27) · `ReservedFlags6` (29~30) · `CompatDatabaseProcessed` (31) | **같은 DWORD 되쓰기**: `LdrpInitializeProcess +0x13be MOV EAX,[RCX+0x68]` → `+0x13c1 OR EAX,4` → `+0x13c4 MOV [RCX+0x68],EAX` (exe 항목, 무조건; 이어 `+0x13cf · +0x13e4 · +0x13f0` 가 조건부로 한 번 더) | 이 비트들을 **켜는 줄은 ntdll 전체에 없다** (`or/and/bts/btr/xchg/mov` 전수 — 아래 3.5 표; 바이트 꼴 `[+0x69]`·`[+0x6a]` 도 확인). 경계 사례 — 판단 기준 절 |
| 노드 | `CondenseLink` (`+0x40`) (1) | `LdrpCondenseGraphRecurse +0x6a MOV [RCX],RAX` (`RAX = 0`, `+0x63 XOR`; 스택에서 뽑는 링크마다) · `+0x9a MOV qword [R15],0x0` (명시 0, `R15 = 노드+0x40`, `+0x15 LEA`) | 먼저 `+0x2a MOV [R15],RAX` 가 **이전 머리 포인터** 를 넣는다 (스택에 올림, `+0x39 [R8]=R15`). 비순환 그래프 (test.exe) 는 노드마다 `LowestLink == PreorderNumber` (`+0x4d CMP · +0x50 JNZ` 안 감) 라 바로 뽑혀 0 으로 끝남 — 사진 8장 0 과 맞음 `[정적 + 사진 간접]` |

#### 3.2 조건부 안 탐 (20) — test.exe 가 왜 안 타나

| 구조체 | 칸 | 켜는 줄 | 조건 | test.exe 가 왜 안 탐 |
|---|---|---|---|---|
| PEB | `BeingDebugged` (`+0x2`) | nt **`DbgkpMarkProcessPeb +0x84`** `MOV byte [RAX+0x2],CL` (`CL = EPROCESS.DebugPort != 0`, `+0x72 CMP [RBX+0x308] · +0x7a SETNE`) | `PspInsertProcess (호출 +0x13e) → DbgkCopyProcessDebugPort` 이 부모 `DebugPort` (`[RDX+0x308]`) 가 0 이 아닐 때만 `DbgkpMarkProcessPeb` 까지 (`+0x3c CMP · +0x43 JNZ`). 붙을 때 `DbgkpSetProcessDebugObject +0x234` · 뗄 때 `DbgkClearProcessDebugObject +0x155` | 부모 (cmd) 가 디버거 아래 아님. 사진 `BeingDebugged 0` (run44 `[관찰]`). (커널 디버거 KDNET 은 `DebugPort` 가 아니라 별개 — 디버거가 붙어서 달라지는 점 없음) |
| PEB | `CrossProcessFlags.ProcessInJob` (비트 0) | nt **`PspApplyWorkingSetLimitsToProcess +0x19b`** `LOCK BTS [R14+0x50],0` (`R14 = [RSI+0x2e0]` = `EPROCESS.Peb`, `+0x158`) | `NtCreateUserProcess (호출 +0xfbd) → PspInsertProcess (+0xd4) → PspImplicitAssignProcessToJob (+0x145) → 이 함수` — 부모가 작업 (job) 안일 때만 | 부모 cmd 가 job 안이 아님. 사진 `ProcessInJob 0y0` (`_P2.txt:2432 ~` PEB 덤프) |
| PEB | `AppCompatInfo` (`+0x2e0`) | `LdrpInitializeProcess +0x19e5 MOV [RDI+0x2e0],RAX` (`RAX = 0`, `+0x19d5 XOR`) | `+0x19d7 TEST R13,R13 · +0x19da JE` 와 `+0x19dc CMP [R13+0x210],EAX · +0x19e3 JE` 둘 다 통과해야 (R13 = 시작 때 모은 앱 호환 블록 `[추측]`), 바로 뒤 `+0x19ef LdrpInitShimEngine` | `g_ShimsEnabled` 0 (run66 · sast/61) 과 같은 길 — shim 안 켜짐. 값은 0 이니 탔어도 0 |
| PEB | `TracingFlags` (`+0x378`) · `.HeapTracingEnabled` · `.CritSecTracingEnabled` · `.LibLoaderTracingEnabled` · `.SpareTracingBits` (5) | ntdll `LdrpInitializeExecutionOptions +0x2b6 LOCK OR [RBX+0x378],EAX` (EAX = IFEO `TracingFlags` 값, `+0x29e ~ +0x2a9 RtlQueryApplicationKeyOption`, 0 이면 건너뜀 `+0x2b4 JE`) | IFEO 키가 있어야 — 이 함수는 **`+0xa6 TEST [RAX+0x8],0x4000 · +0xad JNZ` / `+0xd1 JS` 로 IFEO 부분을 통째 건너뜀** (`+0x604`). test.exe 는 `Flags 0x4001` (3.1 `MinimumStackCommit` 줄과 같은 근거) | IFEO 키 없음. 범위 밖에서도 씀 `[정적]`: nt `EtwpUpdateProcessTracingCallback +0x7e LOCK BTS [RSI+0x378],EDX` · `+0x8d LOCK BTR` (ETW 세션이 켜질 때) |
| PEB | `WaitOnAddressHashTable` (`+0x3a0`, 1024B) | ntdll `RtlpWakeByAddress +0x5d · +0x84 · +0xdb · +0x18f` `LOCK CMPXCHG [RBX+RDI*8+0x3a0]` (슬롯 비움·차지) | 어떤 스레드가 실제로 **주소에서 기다려야** 슬롯이 쓰임. 지도 안 `RtlpWaitOnAddress` (깊이 8) 는 경합 길 (`RtlpHpEnvAcquireGlobalLockSharedContended` · `RtlUnsubscribeWnfNotificationWaitForCompletion` 등)에서만 불린다 | 초기화 중 경합 없음. 사진 8장 0 `[관찰 간접]` |
| 항목 | `Flags.PackagedBinary` (비트 0) | `LdrpMapDllSearchPath +0x13a OR [RSI+0x68],1` · `+0x370 OR [RAX+0x68],R12D` (=1) · `LdrpFindExistingModule +0xaf OR [RAX+0x68],1` · `LdrpInitializeProcess +0x13cd OR EAX,EBX` (`EBX=1`, `+0x137e`) | 패키지 경로에서 찾았을 때 (`LdrpSearchPath` 출력 바이트 `[RSP+0x50]`, `+0x131 CMP · +0x138 JZ`) / 두 번째 찾기가 성공 (`+0x206 TEST R15B,8`) / **exe 면 PEB `BitField` 비트 4 `IsPackagedProcess`** 가 켜졌을 때 (`+0x13c7 TEST BYTE [RDI+3],0x10`) | test.exe 는 패키지 앱 아님 (`BitField = 0x4`, 3.1). 사진 6모듈 모두 비트 0 꺼짐 |
| 항목 | `Flags.MarkedForRemoval` (비트 1) | `LdrpUnloadNode +0x211 OR [RDI-0x38],2` (`RDI = &NodeModuleLink`) | 언로드 때만 (`LdrUnloadDll +0x12c` · `LdrpDecrementModuleLoadCountEx +0xaf` 가 부름) | 초기화 중 언로드 없음 |
| 항목 | `Flags.ShimDll` (비트 8) | `LdrpLoadShimEngine +0x1d9 BTS [RBX+0x68],8` · `LdrpInitShimEngine +0xed OR [RBX+0x68],R14D` (`R14D=0x100`, `+0x94`) · `LdrpGetProcApphelpCheckModule +0x271 OR [RAX+0x68],R13D` (`R13D=0x100`, `+0x76`) | shim 엔진을 읽을 때만 (`g_ShimsEnabled`) | shim 안 켜짐 (위 `AppCompatInfo` 와 같은 근거) |
| 항목 | `Flags.VerifierProvider` (비트 10) | `AVrfpLoadAndInitializeProvider +0x186 BTS [RAX+0x68],0xa` | Application Verifier 가 켜져 제공자 DLL 을 올릴 때만 | verifier 안 켜짐 (`AVrfpVerifierFlags` 길 안 탐, run59 이후) |
| 항목 | `Flags.ProcessAttachFailed` (비트 20) | `LdrpInitializeNode +0x29e OR [RDI+0x68],0x100000` (`+0x298 MOV R14D,0xc0000142`, 바로 앞 `LdrpLogInternal` "Init routine … failed during DLL_PROCESS_ATTACH") | DLL 시작 코드가 실패를 돌려줄 때 | 시작 코드 다 성공 (사진 `ProcessAttachCalled` 1, 실패 비트 0) |
| 항목 | `Flags.ChpeEmulatorImage` (비트 26) | `LdrpAllocateModuleEntry +0x129 OR [RBX+0x68],0x4000000` | 로드 문맥 `+0x20` 비트 27 (`+0x11e BT EBP,0x1b`) | CHPE 에뮬레이터 로드 아님 `[추측: 비트 뜻]` |
| 항목 | `Flags.Redirected` (비트 28) | `LdrpAllocateModuleEntry +0x10d OR [RBX+0x68],0x10000000` | 로드 문맥 `+0x20` 비트 2 (`+0x107 TEST BPL,4`) | 리다이렉트로 찾은 모듈 없음 `[추측: 비트 뜻]` (사진 6모듈 비트 28 꺼짐) |
| 항목 | `SigningLevel` (`+0x11c`, 1B) | `LdrpSetModuleSigningLevel +0x1e LEA RSI,[RDX+0x11c]` → `+0x42 NtGetCachedSigningLevel` (커널이 출력으로 씀). 실패 갈래는 `LdrpValidateIntegrityContinuity +0x9d · +0xd1 MOV byte [..+0x11c],1` | `LdrpMapDllNtFileName +0x141 CMP [0x1801d2918] (LdrpAuditIntegrityContinuity),0 → +0x148 JNZ → +0x400 → +0x407 CALL LdrpValidateIntegrityContinuity (+0x69 → LdrpSetModuleSigningLevel)` | 전역 `LdrpAuditIntegrityContinuity` 0 (`LdrpInitializeProcess +0x4fe` 가 시작 정보 비트로 정함, 사진 0 — 7.5) |
| 항목 | `ActivePatchImageBase` (`+0x128`) | `LdrpInitializeNtdllDataTableEntry +0x264 MOV [RBP+0x128],RAX` (`RAX = 원본 ntdll DllBase`, `+0x256 [RBP+0x130]=3`, `+0x26b [RBX+0x130]=2` 와 한 묶음) · `LdrpApplyPatchImage +0x15d` | 핫패치 ntdll (`LdrpLoadPatchedNtdll`) 을 올릴 때 · 패치 이미지 적용 때 (`LdrpInitializeNode +0x2fd` · `LdrpLoadDllInternal +0x3ac`) | 이 VM 은 핫패치 ntdll 아님 (`HotPatchState` 0 — 3.5) |
| 노드 | `ServiceTagList` (`+0x10`) | `LdrpAddNodeServiceTag +0x67 MOV [RBX+0x10],RAX` (16B 기록 머리에 끼움, 의존 노드로 재귀 `+0x6b ~`) | `LdrpPrepareModuleForExecution +0x50 TEST [RDI+0x68],0x20` 꺼짐 **그리고** `+0x56 MOV RDX,GS:[0x1720]` (TEB `SubProcessTag`) ≠ 0 → `+0x6a CALL` | `SubProcessTag` 0 (sast/48 범위 밖 `RtlSetThreadSubProcessTag` 만 씀) |
| 노드 | `LoadWhileUnloadingCount` (`+0x1c`) | `LdrpIncrementModuleLoadCount +0x201 INC [RCX+0x1c]` · 줄이는 쪽 `LdrpDecrementNodeLoadCountLockHeld +0x84` | 노드 `LoadCount` 가 0 (내려가는 중) 인데 (`+0xf0 JZ → +0x1ea`) TEB `SameTebFlags.LoadOwner` (`+0x248 TEST [RAX+0x17ee],0x1000`) 켜진 스레드가 다시 늘릴 때 | 내려가는 노드를 다시 참조하는 일 없음 |

#### 3.3 아무도 안 씀 (초기화 길 안) (28)

- **PEB (26)**: `ReadImageFileExecOptions` · `Padding0` · `SubSystemData` · `IFEOKey` · `ReservedBits0` · `Padding1` · `SystemReserved` · `AtlThunkSListPtr32` · `TlsExpansionCounter` · `Padding2` · `AnsiCodePageData` · `OemCodePageData` · `UnicodeCaseTableData` · `ProcessStarterHelper` · `Padding3` · `Padding4` · `Padding5` · `SparePointers` · `ChpeV2ProcessInfo` · `AppModelFeatureState` · `SpareUlongs` · `UnusedNlsField` · `EcCodeBitMap` · `pImageHeaderHash` · `Padding6` · `PlaceholderCompatibilityModeReserved`
- **`_PEB_LDR_DATA` (2)**: `SsHandle` (`+0x8`) · `EntryInProgress` (`+0x40`)
- 근거: ntdll 전체 PEB 경유 쓰기 전수 (위 훑기) 에 이 오프셋이 없다 (`+0x1 · +0x28 · +0x40 · +0x48 · +0x64 · +0x70 · +0x100 · +0xa0 · +0xa8 · +0xb0 · +0x2e0(AppCompatInfo 만 있음) · +0x320 · +0x338 · +0x340 · +0x344 · +0x352 · +0x368 · +0x370` 모두). 커널 `MmCreatePeb` 는 `+0x0 · +0x3 · +0x8 · +0x10` 과 앞의 값 칸만 쓰고 이 칸들은 안 쓴다. `PebLdr` 쪽은 전역 `PebLdr` (`0x1801d2880`) 의 `+0x8` (`0x1801d2888`) · `+0x40` (`0x1801d28c0`) 를 직접 가리키는 명령이 ntdll 전체에 **0건**이고 `[PEB+0x18]` 으로 얻은 포인터 추적에도 `+0x8 · +0x40` 쓰기가 없다 (있는 건 목록 머리 `+0x10 · +0x18`, `ShutdownInProgress +0x48` 의 `RtlpProcessReflectionStartup +0x280` 뿐 — 이 둘은 3.4 에 이미 있음).
- **요청서 짐작과 다른 점 둘**:
  - `IFEOKey` · `ReadImageFileExecOptions` 는 "IFEO 키 없음이라 안 탐" 이 아니라 **쓰는 줄이 없다.** `LdrpInitializeExecutionOptions` 는 키 핸들을 지역 `[rsp+0x68]` (`+0xb8 LEA R9`) 에만 두고 끝에 닫는다 (run46 이 이미 "호출자에게 돌려줄 뿐" 이라고 적음). 이름이 IFEO 를 가리켜도 24H2 코드는 이 두 칸을 안 쓴다 `[정적]`.
  - `AnsiCodePageData` · `OemCodePageData` · `UnicodeCaseTableData` 는 사진 P2 도 null (`_P2.txt` PEB 덤프) — 24H2 는 옛 Windows 처럼 커널이 NLS 표 주소를 PEB 에 넣지 않는다 (`MmCreatePeb` 는 `ActiveCodePage` · `OemCodePage` · `UseCaseMapping` 숫자만). 읽는 쪽도 이 칸이 null 인 채로 돈다 (`LdrpInitializeNlsInfo +0x61` 은 `UseCaseMapping` 을 읽음).
- `Padding0 ~ Padding6` 7칸 정렬 빈칸, `ReservedBits0` · `SystemReserved` · `SparePointers` · `SpareUlongs` · `UnusedNlsField` · `PlaceholderCompatibilityModeReserved` 예약, `AtlThunkSListPtr32` · `ChpeV2ProcessInfo` · `EcCodeBitMap` 은 WOW64·ARM64 전용 (x64 test.exe 안 씀), `ProcessStarterHelper` · `SubSystemData` 는 옛 칸, `TlsExpansionCounter` (`+0x70`) 는 `RtlTlsAlloc` 가 쓰지도 읽지도 않는다 (`RtlTlsAlloc.asm` 은 `TlsBitmap` `+0x78` · `TlsExpansionBitmap` `+0x238` 만 읽음). **`AppModelFeatureState` · `pImageHeaderHash` 는 이름에 쓰는 쪽이 있을 법한데 못 찾았다 `[공백]`** (ntdll · nt · user32 · gdi32 · kernel32 · KernelBase 훑기 안; nt 훑기 한계는 위).

#### 3.4 범위 밖 길에서 씀 (22)

**ntdll (4)** — 지도 밖 함수 (이번에 `.asm` 뽑음, 줄 확인):

| 칸 | 쓰는 함수 (오프셋) | 비고 |
|---|---|---|
| `CrossProcessFlags.ProcessUsingVEH` (비트 2) · `.ProcessUsingVCH` (비트 3) | `RtlpAddVectoredHandler +0x1af LOCK BTS [RAX+0x50],ECX` (켬) · `RtlpRemoveVectoredHandler +0x1b8 LOCK BTR` · `RtlpCallVectoredHandlers +0x262 LOCK BTR` (마지막 노드 뺄 때) | run82 (sast/79) 근거 그대로 — `RtlAddVectoredExceptionHandler` 를 ntdll 안에서 부르는 곳 없음. 이번 훑기에서 같은 세 곳만 나왔다 (비트 번호는 `ECX` 값 — `VEH`/`VCH` 구분) |
| `WerShipAssertPtr` (`+0x360`) | `SetAssertBufferPtrinPeb +0x52 MOV [RAX+0x360],RDI` | `ShipAssert` 길 (WER 용 어서션 버퍼). 부르는 쪽은 안 봄 |
| `PlaceholderCompatibilityMode` (`+0x7b0`) | `RtlSetProcessPlaceholderCompatibilityMode +0x1d MOV byte [RDX+0x7b0],CL` | 같은 이름 nt 함수 `+0x2c` 도 씀 (클라우드 파일 플레이스홀더 호환 설정을 프로세스가 직접 바꿀 때) |

**nt (6)**:

| 칸 | 쓰는 함수 (오프셋) | 비고 |
|---|---|---|
| `ProcessPreviouslyThrottled` (비트 5) · `ProcessCurrentlyThrottled` (비트 6) | `PspTrySetProcessPebThrottlingFlags +0x5f LOCK OR [R8+0x50],0x60` (둘 다 켬) · `+0x71 LOCK AND [R8+0x50],0xffffffbf` (현재만 끔) | 전원 스로틀 상태가 바뀔 때. `R8 = [RBX+0x2e0]` (`+0x3d`) |
| `ProcessImagesHotPatched` (비트 7) | 쓰는 줄 못 찾음 `[공백]` — 핫패치 적용 길 (nt) 짐작 `[추측]` | `ProcessInJob`·throttled 와 달리 이름 있는 함수를 못 짚음 |
| `TelemetryCoverageHeader` (`+0x7a0`) | `EtwpCoverageEnsureUserModeView +0x140 MOV [R14+0x7a0],RAX` ← `EtwSetProcessTelemetryCoverage +0x4f` | 프로세스가 텔레메트리 커버리지를 켤 때 |
| `CloudFileFlags` (`+0x7a8`) · `CloudFileDiagFlags` (`+0x7ac`) | 쓰는 줄 못 찾음 `[공백]` — 클라우드 파일 필터 (cldflt) 가 `PsGetProcessPeb` 로 쓸 것으로 짐작 `[추측]` | nt·ntdll·본 DLL 다섯에는 없음 |

**다른 DLL·모듈 (12)** — 쓰는 줄 확인 (○) 또는 짐작 (`[추측]`):

| 칸 | 쓰는 곳 | 근거 |
|---|---|---|
| `KernelCallbackTable` · `UserSharedInfoPtr` (`+0x58`, 겹침) | ○ user32 `_UserClientDllInitialize +0x244 · +0x3fc · +0x524` `MOV [RAX+0x58],…` | 이번 훑기 + run85 (sast/82). user32 를 올리는 프로세스만 |
| `PostProcessInitRoutine` (`+0x230`) | ○ user32 `_UserClientDllInitialize +0x409` (`R15 = 0` 을 씀) | run85. 읽는 쪽 ntdll `LdrpInitializeProcess +0x1d16 CALL [RDI+0x230]` (CFG 간접 호출, null 이면 건너뜀) |
| `Win32ClientProcInfo` (`+0x7d0`) | ○ user32 `_UserClientDllInitialize +0xae` (0) · `+0x760` (`gpClientProcInfo` 힙 블록) | run85 |
| `AppCompatFlagsUser` (`+0x2d0`) | ○ user32 `InitProcessGdiScaling +0xc8 MOV [RAX+0x2d0],RCX` (그리고 `InitProcessSystemDpiBehavior`·`InitProcessDpiAwareness`) | run85 |
| `GdiSharedHandleTable` (`+0xf8`) | ○ win32kbase.sys `GdiProcessCallout +0x22b MOV [R13+0xf8],RAX` | 이번 + run86 (sast/83) |
| `GdiDCAttributeList` (`+0x108`) | ○ win32kbase.sys **`GdiProcessCallout +0xe9 MOV [RAX+0x108],ECX`** (`ECX = [R14+0x118]`, `RAX = PEB`) | **이번에 처음 찾음** (run86 은 "이번 줄기에 안 나옴" 이라 했던 칸) — GUI 프로세스로 변환될 때 `[추측]` |
| `GdiHandleBuffer` (`+0x140`, 240B) | ○ win32kbase.sys `GdiProcessCallout +0xef LEA RCX,[RAX+0x140]` · `+0xf6 XOR EDX,EDX` · `+0xf8 MOV R8D,0xf0` · `+0xfe CALL` (= **명시 `memset(…, 0, 0xf0)`**) + ○ gdi32 `hGetPEBHandle +0x57 · +0x159` (칸을 핸들 캐시로 씀) | **이번에 처음 찾음**. 쓰는 때는 GDI 가 처음 켜지는 프로세스 |
| `AppCompatFlags` (`+0x2c8`) | `[추측]` shim 엔진 · apphelp (`SE_*`) — ntdll 은 **읽기만** (`LdrpInitializeProcess +0x19fd MOV EDX,[RAX+0x2c8]` → `bt 0x16` → `RtlpForceCSDebugInfoCreation`) | 본 DLL 여섯에 쓰는 줄 없음 |
| `AtlThunkSListPtr` (`+0x40`) | `[추측]` atlthunk.dll (ATL 썽크 목록) | 본 DLL 에 없음 |
| `ProcessUsingFTH` (비트 4) | `[추측]` 장애 허용 힙 (fthsvc 가 넣는 `fth.dll`) | 본 DLL·nt 에 없음 |
| `WerRegistrationData` (`+0x358`) | `[추측]` WER (`wer.dll` / `WerRegisterRuntimeExceptionModule` 쪽) | 본 DLL 에 없음 |

- 합: ntdll 4 + nt 6 + 다른 12 = 22.

### 4. 간접 호출

**답: 이 칸들을 쓰는 간접 호출 0개.** 줄이 든 함수 20여 개의 `.asm` 에서 `CALL reg` · `indirect:` 를 찾았다: `LdrpSendPostSnapNotifications +0x122` (`g_pfnSE_DllLoaded`, shim — **`ShimEngineCalloutSent` 가 이미 조건부 안 탐**) · `LdrpMapDllSearchPath +0x3b4` (`g_pfnSE_LdrResolveDllName`, shim) · `LdrpInitShimEngine +0x160` · `LdrpInitializeProcess +0x17fc` (WOW64) · `+0x1b59` (`Kernel32ThreadInitThunkFunction` — 칸 안 씀) · `+0x1cdf` · `+0x1d16` (`PostProcessInitRoutine` — **DLL 이 쓴 칸을 부르는** 쪽, ntdll 은 안 씀) · `_LdrpInitialize +0x9a` (WOW64). **이 칸을 쓰게 만드는 간접 호출은 shim 콜아웃 (`g_pfnSE_*`) 뿐이고 `g_ShimsEnabled` = 0 이라 안 탄다.** → Q35 에 새로 넣을 것 없음 (shim 콜아웃이 이 칸들을 바꿀 수 있다는 한 줄 메모만). DLL 시작 코드 (`LdrpCallInitRoutine` 이 부르는 `DllMain`) 가 PEB 칸을 쓰는 것은 "범위 밖 길 (다른 DLL)" 에 셌다.

### 5. 줄기와 경로

- **줄기** (칸 값을 정하는 함수까지): nt `MmCreatePeb` (이미 닫힘) · `PspInsertProcess → DbgkCopyProcessDebugPort / PspImplicitAssignProcessToJob → PspApplyWorkingSetLimitsToProcess`; ntdll `_LdrpInitialize` · `LdrpInitializeProcess` · `LdrpInitializePatchData` · `LdrpInitializeExecutionOptions` · `LdrpAllocateModuleEntry` · `LdrpInitializeNtdllDataTableEntry` · `LdrpInsertDataTableEntry` · `LdrpProcessMappedModule` · `LdrpSendPostSnapNotifications → SbUpdateSwitchContextBasedOnDll` · `LdrpPrepareModuleForExecution → LdrpCondenseGraph → LdrpCondenseGraphRecurse`.
- **줄기 밖 (이름만)**: `LdrpMergeNodes` (순환일 때) · `LdrpAddNodeServiceTag` · `LdrpIncrementModuleLoadCount` · `LdrpUnloadNode` · `LdrpSetModuleSigningLevel` · `LdrpLoadShimEngine` · `AVrfpLoadAndInitializeProvider` · 범위 밖 3.4 표의 함수 전부.
- 공용 부품은 이름으로만: `RtlAllocateHeap` (`LdrpHeap`) · `RtlQueryApplicationKeyOption` · `NtGetCachedSigningLevel` · `RtlAcquireSRWLockExclusive`.

## 동적으로 확인할 것

"사진" = 이미 있는 덤프. **dast 요청 필요: 없음** (전부 낮음 — 사진 8장이 간접 증거).

| # | 질문 | 추천 멈춤점 / 찍을 것 | 사진으로? |
|---|---|---|---|
| D1 | `LdrpMapDllSearchPath +0x131 CMP byte [RSP+0x50],0` 의 `LdrpSearchPath` 출력 바이트가 test.exe 모듈마다 0 인가 (`PackagedBinary` 안 켜짐의 직접 근거) | `bp ntdll!LdrpMapDllSearchPath+0x131` (조건: 현재 프로세스 test.exe) 에서 `db @rsp+0x50 L1` | 가능 (사진 6모듈 `PackagedBinary` 0 이 간접 증거) → 낮음 |
| D2 | `LdrpInitializeProcess +0x19dc` 의 `[R13+0x210]` — shim 길 입구가 정말 0 인가 | `+0x19dc` 에서 `r r13; dd @r13+0x210 L1` | 불가 (지역 값). 낮음 (`g_ShimsEnabled` 0 이 간접 증거) |
| D3 | `WaitOnAddressHashTable` — 초기화 동안 `RtlpWakeByAddress` 가 한 번이라도 불리나 | `bp ntdll!RtlpWakeByAddress` hit 수 (NtContinue 전까지) | 불가 (호출 여부). 낮음 |
| D4 | `TracingFlags` — ETW 세션이 NtContinue 전에 프로세스 추적을 켰나 (`EtwpUpdateProcessTracingCallback`) | `bp nt!EtwpUpdateProcessTracingCallback` hit | 사진 `TracingFlags 0` ×8 이 간접 증거. 낮음 |
| D5 | `LdrpHeap` 이 프로세스 힙과 같은 힙인가 (`PatchLoaderData` · 노드·항목 주소가 프로세스 힙 구역) | `dq ntdll!LdrpHeap L1` vs `@$peb+0x30` | **가능** (전역은 덤프에 있음) → 다음 요청 후보와 같이 |

- 디버거가 붙어서 달라지는 점: 없음 (KDNET 은 PEB `BeingDebugged`·`DebugPort` 를 안 건드린다).

## 다음 요청 후보 (Q78e2 로 넘길 것)

- **Q78e2 (ntdll `.data`/`.mrdata` 전역 `≤P1a` + 힙 뒤 부분 + 조건부 힙 전역 8 + `EnvironmentVersion` +2) 에 새로 얹을 전역**: 이번에 이름이 나온 `LdrpAuditIntegrityContinuity` · `LdrpEnforceIntegrityContinuity` (7.5 에 이미 있음 — `LdrpInitializeProcess +0x4fe` 줄과 읽는 줄 `LdrpMapDllNtFileName +0x141` 을 보탬) · `LdrpModuleDatatableLock` (`LdrpFindExistingModule +0x21 LEA`, 잡고 푸는 줄 `+0x35` · `+0x6f`) · `g_pShimEngineModule` (쓰는 줄 `LdrpInitShimEngine +0xf5`, `LdrpGetProcApphelpCheckModule +0x27e`, 조건부 안 탐) · `LdrpHeap` (`.data`, 위 D5). 새로 모르는 전역은 이 중 `g_pShimEngineModule` 하나로 보임 (7.3 에 이미 있으면 줄만 보탬).
- 이 바퀴로 `_PEB` · `_PEB_LDR_DATA` · `_LDR_DATA_TABLE_ENTRY` · `_LDR_DDAG_NODE` 의 "모름" 칸이 0 이 된다. 단, **5칸은 "쓰는 줄을 못 짚었다" (`[공백]`)**: `ProcessImagesHotPatched` · `CloudFileFlags` · `CloudFileDiagFlags` (nt 쪽) · `AppModelFeatureState` · `pImageHeaderHash` — 위 3.3/3.4 에 표시. 새 요청 후보는 nt 쪽 PEB 쓰기를 분기 합침이 있는 추적으로 다시 훑는 일.
- 한 가지 더: **user32 · gdi32full 시작 함수가 쓰는 PEB 칸 중 `AppCompatFlags` (`+0x2c8`) 를 쓰는 곳** (apphelp · aclayers 쪽) 은 이번엔 `[추측]` — 그 DLL 을 안 봤다 (후보: apphelp `SE_*`).

## baggage.md 에 넣을 줄 (3장 양식)

### 3.1 `_PEB` (아는 칸 +2 · 줄 +11)

머리 문장 후보: "(run?, sast/109 — **Q78e1**) 모르던 칸 63개가 모두 가려졌다: 아는 칸 +2 · 0 을 씀 4 · 조건부 안 탐 9 · 아무도 안 씀 26 · 범위 밖 길 22. 단계 ①·② 줄은 아래."

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `+0x50` `CrossProcessFlags` (정수 칸) | ② | 위 `ProcessInitializing` 줄과 **같은 칸**. 처음 값 `2` (`_LdrpInitialize +0x107` 비트 1 켬), 끝에 `0` (`+0x20f`) | 직접 | ntdll | `②… _LdrpInitialize +0x107 · +0x20f` | `[정적]` sast109 · 정확 · 사진 `P1~P1a` (`2`) → 바뀜 `P1b~P2` (`0`) |
| `+0x330` `PatchLoaderData` | ② | `LdrpHeap` 0x18B 블록 (`[0]=1 · [8]=&LdrLoadDll · [0x10]=&LdrGetProcedureAddress`) 주소. `LdrInitState = 2` 직후 무조건 | 직접 | ntdll | `②… → LdrpInitializeProcess (호출 +0x1b24) → LdrpInitializePatchData +0x52` | `[정적]` sast109 · 정확 · 사진 `P1a~P1b` |
| `+0x7c0` `LeapSecondFlags` (+ `SixtySecondEnabled` · `Reserved`) | ① | **0** 을 씀 (명시 `AND dword [RBX+0x7c0],0`, `LeapSecondData` 쓴 직후) | 직접 | nt | `①P… → MmCreatePeb (호출 +0x230f) +0x57a` | `[정적]` sast109 · 정확 · 사진 `끝까지 0`. 뒤에 `NtGlobalFlag2` 비트 0 이 켜져 있으면 ntdll 이 비트 0 을 켬 (`LdrpInitializeExecutionOptions +0x725`, 조건부 안 탐) |
| `+0x7c4` `NtGlobalFlag2` | ① | 커널 전역 `NtGlobalFlag2` (`0x140fc3a00`, 값 0) 복사 | 직접 (값은 부팅 전역 = 입력) | nt | `①P… → MmCreatePeb +0x4ac ~ +0x4b2` | `[정적]` sast109 · sast107 · 정확 · 사진 `끝까지 0` |
| `+0x7c4` `NtGlobalFlag2` | ② (조건부 — test.exe 안 탐) | IFEO `GlobalFlag2` 값으로 덮음 | 직접 | ntdll | `②… → LdrpInitializeExecutionOptions (호출 +0x531) +0x5a9` | `[정적]` sast109 · 정확 · 사진 `끝까지 0` (IFEO 부분 통째 건너뜀 `+0xad JNZ`·`+0xd1 JS`) |
| `+0x378` `TracingFlags` (+ 비트 4칸) | ② (조건부) | IFEO `TracingFlags` 값을 OR (`RtlQueryApplicationKeyOption`, 0 이면 건너뜀) | 직접 | ntdll | `②… → LdrpInitializeExecutionOptions (호출 +0x531) +0x2b6` | `[정적]` sast109 · 정확 · 사진 `끝까지 0`. 범위 밖에서도: nt `EtwpUpdateProcessTracingCallback +0x7e · +0x8d` |
| `+0x2e0` `AppCompatInfo` | ② (조건부) | 0 을 씀 — shim 블록이 있고 `[R13+0x210] != 0` 일 때만 (바로 뒤 `LdrpInitShimEngine`) | 직접 | ntdll | `②… +0x19e5` | `[정적]` sast109 · 정확 · 사진 `끝까지 0` (shim 안 켜짐) |
| `+0x2` `BeingDebugged` | ① (조건부 — 안 탐) | `EPROCESS.DebugPort != 0` 이면 1 | 직접 | nt | `①I… (PspInsertProcess +0x13e) → DbgkCopyProcessDebugPort → DbgkpMarkProcessPeb +0x84` (조각 안 call, ret `+0x2c8972`) | `[정적]` sast109 · 정확 · 사진 `끝까지 0` (run44 `[관찰]`) |
| `+0x50` `ProcessInJob` (비트 0) | ① (조건부 — 안 탐) | 작업 안 프로세스면 켬 | 직접 | nt | `①I… (PspInsertProcess +0xd4) → PspImplicitAssignProcessToJob (+0x145) → PspApplyWorkingSetLimitsToProcess +0x19b` | `[정적]` sast109 · 정확 · 사진 `끝까지 0` |
| `+0x3a0` `WaitOnAddressHashTable` | ② (조건부 — 경합 때만) | 주소 대기 슬롯 (`LOCK CMPXCHG [표+해시×8]`) | 직접 | ntdll | `RtlpWakeByAddress +0x5d · +0x84 · +0xdb · +0x18f` (지도 밖; 지도 안 `RtlpWaitOnAddress` 가 경합 길로 부름) | `[정적]` sast109 · 정확 · 사진 `끝까지 0` |
| 범위 밖 길 묶음 22칸 | — | 이름만: ntdll (`VEH/VCH` · `WerShipAssertPtr` · `PlaceholderCompatibilityMode`), nt (`throttled` 둘 · `TelemetryCoverageHeader` · `ProcessImagesHotPatched`·`CloudFile*` 짐작), user32·gdi32·win32kbase 외 (3.4 표) | — | — | 위 3.4 표 | `[정적]`/`[추측]` sast109 |

### 3.4 `_PEB_LDR_DATA` (모름 2 → 0, 줄 변화 없음)

- `SsHandle` · `EntryInProgress`: **아무도 안 씀 (초기화 길 안)**. ntdll 전체에 이 칸을 쓰는 줄 없음 (`PebLdr+0x8` · `+0x40` 직접 참조 0건, `[PEB+0x18]` 추적에도 없음). 사진 `끝까지 0`. 에뮬레이터는 0 으로 두면 맞다 (확인 필요 — 코드 안 봄).

### 3.5 `_LDR_DATA_TABLE_ENTRY` (아는 칸 +4 · 줄 +13)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `+0xa0` `NodeModuleLink` | ② | 노드 `Modules` 와 서로 가리키게 (`[RAX]=[RAX+8]=노드`, 노드 `[0]=[8]=&NodeModuleLink`) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0xd4 · +0xdb · +0xde` | `[정적]` sast109 · 정확 · 사진 `P1~P1a` |
| `+0x100` `LoadTime` | ② | KUSD `SystemTime` (`[0x7ffe0014]`) 8B | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpProcessMappedModule +0xa6` | `[정적]` sast109 · 정확 · 사진 `P1~P1a` |
| `+0xc0` `SwitchBackContext` | ② | `SbpDetermineDllContext` 가 돌려준 블록 `+4` 주소. 인자 `r8d != 0` 이면 안 씀 | 직접 | ntdll | `②N… LdrpSendPostSnapNotifications (호출 +0xa3) → SbUpdateSwitchContextBasedOnDll +0xd9` | `[정적]` sast109 · 정확 · 사진 `P1a~P1b` |
| `+0x90` `Lock` | ② | 0 | 직접 | ntdll | `②… → LdrpAllocateModuleEntry +0xe9` | `[정적]` sast109 · 정확 · 사진 `끝까지 0` |
| `+0x68` `Flags` 되쓰기 | ② | exe 항목 `Flags \|= 4` 를 DWORD 로 되씀 (비트 10개의 "0 을 씀" 근거) | 직접 | ntdll | `②… +0x13be ~ +0x13c4` | `[정적]` sast109 · 정확 · 사진 exe `끝까지 0` (해당 비트들) |
| `+0x68` `PackagedBinary` · `MarkedForRemoval` · `ShimDll` · `VerifierProvider` · `ProcessAttachFailed` · `ChpeEmulatorImage` · `Redirected` (조건부, 안 탐) | ② | 줄은 위 3.2 표 (각각 `+0x13a`/`+0x370`/`+0xaf`/`+0x13cd`, `LdrpUnloadNode +0x211`, `LdrpLoadShimEngine +0x1d9`·`LdrpInitShimEngine +0xed`·`LdrpGetProcApphelpCheckModule +0x271`, `AVrfpLoadAndInitializeProvider +0x186`, `LdrpInitializeNode +0x29e`, `LdrpAllocateModuleEntry +0x129`·`+0x10d`) | 직접 | ntdll | 3.2 표 | `[정적]` sast109 · 정확 · 사진 6모듈 꺼짐 |
| `+0x11c` `SigningLevel` | ② (조건부) | `NtGetCachedSigningLevel` 가 칸 주소로 써 줌. `LdrpAuditIntegrityContinuity != 0` 일 때만 | syscall (`NtGetCachedSigningLevel`) | nt | `②M… LdrpMapDllNtFileName (+0x407) → LdrpValidateIntegrityContinuity (+0x69) → LdrpSetModuleSigningLevel +0x1e · +0x42` | `[정적]` sast109 · 정확 · 사진 `끝까지 0` |
| `+0x128` `ActivePatchImageBase` | ② (조건부) | 핫패치 ntdll 일 때 원본 DllBase | 직접 | ntdll | `LdrpInitializeNtdllDataTableEntry +0x264`, `LdrpApplyPatchImage +0x15d` | `[정적]` sast109 · 정확 · 사진 `끝까지 0` |

### 3.6 `_LDR_DDAG_NODE` (아는 칸 +1 · 줄 +4)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `+0x48` `PreorderNumber` | ② | 묶기 번호 (공용 카운터 `++`), 같은 값을 `LowestLink` 에도 | 직접 | ntdll | `②C… LdrpCondenseGraphRecurse +0x1e` | `[정적]` sast109 · 정확 (짐작 `LdrpInitializeGraphRecurse` 아님) · 사진 `P1a~P1b` |
| `+0x40` `CondenseLink` | ② | 스택에 올릴 때 이전 머리 포인터 (`+0x2a`), 뽑을 때 **0** (`+0x6a` · `+0x9a`) | 직접 | ntdll | `②C… LdrpCondenseGraphRecurse +0x2a · +0x6a · +0x9a` | `[정적]` sast109 · 정확 · 사진 `끝까지 0` |
| `+0x10` `ServiceTagList` (조건부) | ② | 서비스 태그 16B 기록을 머리에 끼움 | 직접 | ntdll | `②… → LdrpPrepareModuleForExecution (호출 +0x6a) → LdrpAddNodeServiceTag +0x67` | `[정적]` sast109 · 정확 · 사진 `끝까지 0` (`SubProcessTag` 0) |
| `+0x1c` `LoadWhileUnloadingCount` (조건부) | ② | 내려가는 노드를 로드 주인 스레드가 다시 늘릴 때 `+1` | 직접 | ntdll | `LdrpIncrementModuleLoadCount +0x201` | `[정적]` sast109 · 정확 · 사진 `끝까지 0` |

## 정정 · 새로 알게 된 것 (flow-writer 용)

1. **`PreorderNumber` = 묶기 (`LdrpCondenseGraphRecurse +0x1e`)**. 7.2 요청서 가설·`flow.md` 에 "그래프 걷기에서 번호" 로 적혀 있으면 고친다. 노드 `LowestLink` (3.6) 와 같은 함수·같은 값.
2. **`IFEOKey` 는 PEB 칸이 아니라 지역 변수** (이 빌드 코드 기준): `LdrpInitializeExecutionOptions` 가 키를 `[rsp+0x68]` 에만 쓴다. `ReadImageFileExecOptions` 도 쓰는 줄이 없다. run46 의 "호출자에게 돌려줄 뿐" 을 더 강하게 확인.
3. **`MmCreatePeb +0x57a` 명시 0** — `LeapSecondFlags` 한 DWORD (3.1 `LeapSecondData` 줄 옆). 에뮬레이터가 `MmCreatePeb` 를 구현하면 이 칸도 0 으로 쓰게 둔다 (페이지가 0 이라 결과는 같음).
4. **`GdiDCAttributeList` · `GdiHandleBuffer` 의 쓰는 곳은 win32kbase `GdiProcessCallout`** (`+0xe9`, `+0xf8 memset 0xf0`) — run86 이 못 찾았던 줄. `GdiSharedHandleTable` (`+0x22b`) 과 같은 함수.
5. **모듈 항목 할당은 `HEAP_ZERO_MEMORY`** (`NtdllBaseTag + 0x40000 \| 8`, 항목 0x138 + 노드 0x50 따로 두 번 할당). 3.5 `DdagNode` 줄 설명 ("항목 0x138 + 노드 0x50 한 쌍") 의 "한 쌍" 은 **한 덩어리가 아니라 두 번 할당**이다 (`LdrpAllocateModuleEntry +0x29` · `+0x5b` `CALL RtlAllocateHeap` ×2, 크기 `0x138` · `0x50`, 둘 다 `HEAP_ZERO_MEMORY`). 항목 뒤에 노드가 이어 붙어 있다는 가정은 코드와 안 맞는다 — `entry+0x138` 로 노드를 읽는 코드는 만들지 않는다 (항상 `entry+0x98 DdagNode` 로).
6. **`Flags` 에 쓰는 줄은 전수 확인** (dword 읽기-고쳐-쓰기 `or/and/bts/btr` 26곳 + 바이트 꼴 `[+0x69]`·`[+0x6a]` 5곳 + 통째 저장 `mov` 몇 곳 — `Flags` 칸 훑기), 그중 길 안 무조건 줄은 `LdrpAllocateModuleEntry +0x11a` · `LdrpInsertDataTableEntry +0x5e` · `LdrpInsertModuleToIndexLockHeld +0xec` · `LdrpSendPostSnapNotifications +0xce` · `LdrpProcessMappedModule +0x154` · `LdrpInitializeNode +0x231` · 노드 갈래 몇 개뿐. 켜는 줄이 없는 비트 10개는 3.1 "0 을 씀 (되쓰기)".

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **고칠 버그는 없다.** `run_ghidra.sh export` 한 번 (11개, 몇 초) · `pdb_struct.py` 4구조체 문제없이 돌았다. `ExportFunctions` 가 `_calls.tsv` 를 줄 안 잃고 늘렸다.
- **제안 1 (sast/43·44·46·47·48 제안과 이어짐): `scripts/field_writers.py <바이너리> --struct _PEB|_LDR_DATA_TABLE_ENTRY|...`.** 이번에도 임시 스크립트를 짰다 (스크래치 `s109/`: `flow.py` 레지스터·스택·오프셋 추적, `raw.py`·`raw3.py` 전체 오프셋 일치, `entry_scan.py` 고리 7종 기준, `lea_scan.py` 칸 주소, `node_scan.py`, `callers.py`, `show.py` (이름 붙은 호출 보기), `kpeb.py` 커널 PEB 추적, `dllscan.py`·`win.py` 다른 DLL). 합치면 좋은 기능: (a) 오프셋을 더하며 따라가는 레지스터 추적 (`add rax,0x390` 를 따라가야 `TpInitializePackage +0xa9` 가 잡힌다), (b) 인자로 PEB 를 받는 함수에 씨앗 (직접 호출·꼬리 점프), (c) `.asm` 이 없으면 자동으로 export 후보를 뽑아 줌, (d) 구조체가 **여러 칸 겹치는 비트** (`Flags` 32비트) 를 비트 단위로 쓰기 마스크 계산 (`or imm` → 켜는 비트, `and imm` → 끄는 비트, `bts/btr`).
- **제안 2**: 커널 쪽 추적은 분기 합침이 필요하다. 첫 판의 `test r,r` 가 레지스터 표시를 지우는 버그 하나로 nt 쪽 쓰기 하나 (`ProcessInJob`) 를 놓쳤다. 결과서에 nt "못 찾음" 은 `[공백]` 으로만 적는 이유.
- **제안 3 (사소)**: `pdb_struct.py` 가 `FlagGroup` 같은 "`?[4B]` 겹침 칸" 을 같은 오프셋의 정수 칸과 한 줄로 묶어 보여 주는 옵션 (sast/47 제안 2 와 같음).

## 이름 확인 결과

이 결과서의 함수 이름을 `ms/decomp/ntdll/_index.tsv` 에 대 봤다. **전부 있음** (`LdrpAllocateModuleEntry` 0x6fab0 · `LdrpInsertDataTableEntry` · `LdrpInitializeNtdllDataTableEntry` · `LdrpProcessMappedModule` · `LdrpCondenseGraphRecurse` 0xe91a0 · `LdrpMergeNodes` 0xed9f4 · `LdrpInitializePatchData` 0xfb6e0 · `SbUpdateSwitchContextBasedOnDll` 0xc9a0 · `LdrpSetModuleSigningLevel` 0x109f7c · `LdrpValidateIntegrityContinuity` 0x109e80 · `LdrpFindExistingModule` 0xebffc · `LdrpAddNodeServiceTag` 0x8702c · `LdrpIncrementModuleLoadCount` 0x18050 · `RtlpWakeByAddress` 등). 요청서의 `LdrpCreateNodeForModule` 은 **없다** (`_index.tsv` · `pdb_aliases.py --where` 둘 다 0개) — 노드는 `LdrpAllocateModuleEntry` 가 항목과 함께 만든다 (+0x5b 둘째 `RtlAllocateHeap`). 요청서의 `LdrpBuildDependencyGraph` 도 이 빌드엔 안 쓴다 (`PreorderNumber` 는 위 `LdrpCondenseGraphRecurse`). 커널 이름 (`MmCreatePeb` 0x930f94 · `DbgkpMarkProcessPeb` 0xaccfec · `PspTrySetProcessPebThrottlingFlags` 0x781940 · `PspApplyWorkingSetLimitsToProcess` · `PspImplicitAssignProcessToJob` · `EtwpCoverageEnsureUserModeView` · `EtwpUpdateProcessTracingCallback` · `PspInsertProcess`) 은 `ms/decomp/ntoskrnl/_index.tsv` 에 있다. `_index.tsv` 에 없는 것: 구조체 칸 이름 · 전역 (`LdrpAuditIntegrityContinuity` 등 — PDB 이름으로 `.asm` 주석에 나옴).

## 판정

**분석성공.** 요청서 질문 1~5 모두 답했다. 93칸 (요청서 "약 92" 는 PDB 이름으로 다시 센 결과 93) → 아는 칸 7 · 0 을 씀 16 (되쓰기 10 포함) · 조건부 안 탐 20 · 아무도 안 씀 28 · 범위 밖 길 22 (ntdll 4 · nt 6 · 다른 DLL·모듈 12) · 여전히 모름 0. **PDB 일치**: ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1`, ntdll `C3093720177DA6851519DCFFE8DF53FE1`; 알려진 사실 호출 지점 (`+0xc36` · `+0x807` · `+0xe` · `+0x16c` · `+0x1bbb` · `+0x16b/+0x19c`) 모두 맞다. 인용한 `+0x` 64곳은 `.asm` 줄에서 다시 찾아 대조했다. **정적으로 못 닫은 것**: nt 쪽 쓰는 줄 `ProcessImagesHotPatched` · `CloudFileFlags` · `CloudFileDiagFlags` · `AppModelFeatureState` · `pImageHeaderHash` (`[공백]` — 커널 훑기 한계), `AppCompatFlags` · `AtlThunkSListPtr` · `ProcessUsingFTH` · `WerRegistrationData` 의 **쓰는 DLL 이름** (`[추측]`), 조건부 갈래 몇 개의 비트 뜻 (`Redirected` · `ChpeEmulatorImage` 의 적재 문맥 비트). 요청서 가설과 달랐던 것: `IFEOKey` · `ReadImageFileExecOptions` 는 "조건부 안 탐" 이 아니라 쓰는 줄 없음, `PreorderNumber` 는 그래프 걷기가 아니라 묶기, `LdrpCreateNodeForModule` 이름 없음. 경계 사례: `Flags` 비트 10개의 "0 을 씀" 은 같은 DWORD 되쓰기 기준 (안 치면 "아무도 안 씀" — 요약 표에 두 숫자).

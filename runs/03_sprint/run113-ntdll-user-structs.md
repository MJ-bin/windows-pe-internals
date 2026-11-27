# 2026-11-16 (작성일) run113 — 유저 구조체 모르는 칸 (Q78e1, 정적만): PEB · `PebLdr` · 모듈 항목 · 노드 모르는 칸 92 → **여전히 모름 0** — 아는 칸 42 · 아무도 안 씀 28 · 범위 밖 길 22. `P1~P1a` · `P1a~P1b` 7칸은 모두 이미 아는 길 안 줄 (`_LdrpInitialize` · `LdrpAllocateModuleEntry` · `LdrpProcessMappedModule` · `LdrpInitializePatchData` · `SbUpdateSwitchContextBasedOnDll` · `LdrpCondenseGraphRecurse`), `끝까지 0` 은 커널 `MmCreatePeb +0x57a` 명시 0 · 항목 `Flags` DWORD 되쓰기 · 조건부 갈래 20칸 · 쓰는 줄 없음 28 · 다른 길 22. 모듈 항목 `Flags` 비트 10개는 `LdrpInitializeProcess +0x13be ~ +0x13c4` DWORD 되쓰기라 **"0 을 씀" 으로 셈** (오케스트레이터 결정, run112 `CrossThreadFlags` 와 같은 규칙).

> **표기 정정 (run154, sast/153)**: 아래 `DbgkCopyProcessDebugPort` 줄의 "`DbgkpMarkProcessPeb` (ret `+0x2c8972`)" 는 **조각 `FUN_140845053` 시작 기준** 수다 — 함수 기준으로는 **call `+0x2c8a0c` · ret `+0x2c8a11`** (같은 줄의 `+0x3c` · `+0x43` 은 함수 기준). 또 "부모 `DebugPort`" 는 정확히는 `NtCreateUserProcess` 를 부른 **현재 프로세스** (`PspInsertProcess +0x40` R13) 의 것이고, 디버그 객체 핸들 속성이 없을 때만 본다 → [run154](run154-dbgk-events.md)

(글의 오프셋은 call 자리 · 쓰는 줄 자리. "호출 관계" 나무의 callee 앞 오프셋은 ret 자리 (콜스택 표시) — 둘 다 아래 "정적 — 호출 지점" 표.)

> **지도 범위**: 그림은 안 고쳤다 (run52 · run53 · run112 와 같은 까닭). 새 단계 · 새 caller 가 없고, 이미 있는 함수 **안쪽** 쓰는 줄만 늘었다. 글은 flow.md 기준 · 2.1 · 2.4 · 2.5 · 2.6 · 4장 · 5장, 짐 지도는 3.1 · 3.4 · 3.5 · 3.6 과 2장 · 7.1 · 7.2 를 고쳤다.

## 요약 표 (모르는 칸 92 → ?)

결과서 맨 앞 표 (93칸) 에서 **`NtGlobalFlag2` 1칸을 뺐다**. 그 칸은 run111 이 이미 짐 지도 3.1 로 옮긴 아는 칸이다 (요청서가 7.2 옛 목록을 옮기며 같이 넣음). 결과서는 이 칸을 "0 을 씀 (nt `MmCreatePeb +0x4b2`)" 으로 셌는데, 짐 지도 줄과 같은 근거라 새로 늘 칸은 없다.

| | 칸 | 아는 칸 (구간 줄) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 | 범위 밖 (ntdll) | 범위 밖 (nt) | 범위 밖 (다른 DLL·모듈) | 여전히 모름 |
|---|---|---|---|---|---|---|---|---|---|
| `_PEB` `P1~P1a` · `P1a~P1b` | 2 | 2 | — | — | — | — | — | — | 0 |
| `_PEB` `끝까지 0` | 60 | — | 3 | 9 | 26 | 4 | 6 | 12 | 0 |
| `_PEB_LDR_DATA` `끝까지 0` | 2 | — | — | — | 2 | — | — | — | 0 |
| `_LDR_DATA_TABLE_ENTRY` `P1~P1a` · `P1a~P1b` | 4 | 4 | — | — | — | — | — | — | 0 |
| `_LDR_DATA_TABLE_ENTRY` `끝까지 0` | 20 | — | 11 (명시 1 · 되쓰기 10) | 9 | — | — | — | — | 0 |
| `_LDR_DDAG_NODE` `P1a~P1b` | 1 | 1 | — | — | — | — | — | — | 0 |
| `_LDR_DDAG_NODE` `끝까지 0` | 3 | — | 1 | 2 | — | — | — | — | 0 |
| **합** | **92** | **7** | **15** | **20** | **28** | **4** | **6** | **12** | **0** |

- 아는 칸 = 7 + 15 + 20 = **42** (모두 정확). 아무도 안 씀 28 · 범위 밖 22 는 2장 아래 줄에서 따로 센다 (run54 `_TEB` 와 같은 꼴 — 유저 구조체라 범위 밖을 셋으로 나눔).
- 결과서 숫자 (93칸) 와의 차이는 "0 을 씀" 16 → 15 하나뿐이다.
- **셈 결정 (오케스트레이터)**: 모듈 항목 `Flags` 비트 10개 (`TelemetryEntryProcessed` · `LoadInProgress` · `EntryProcessed` · `AuxIatCopyPrivate` · `ReservedFlags3` · `DontRelocate` · `ChpeImage` · `ReservedFlags5` · `ReservedFlags6` · `CompatDatabaseProcessed`) 는 켜는 줄이 ntdll 어디에도 없다. 그래도 exe 항목을 만들 때 `LdrpInitializeProcess +0x13be MOV EAX,[RCX+0x68]` → `+0x13c1 OR EAX,4` → `+0x13c4 MOV [RCX+0x68],EAX` 가 같은 DWORD 를 통째로 되쓴다 `[정적]` (flow-writer `.asm`). run52 · run53 · run112 기준 ("같은 DWORD 되쓰기 = 0 을 씀") 그대로 **"0 을 씀"** 으로 셌다. 안 치면 0 을 씀 5 · 아무도 안 씀 38 (결과서 둘째 숫자).
- 그래서 짐 지도 숫자 (2장):
  - `_PEB` nt 42 → **47** · ntdll 25 → **35** (그중 `LeapSecondFlags.SixtySecondEnabled` 는 nt 0 · ntdll 조건부 켬 양쪽) · 아무도 안 씀 **26** · 범위 밖 **22** · 모름 62 → **0**.
  - `_PEB_LDR_DATA` 아무도 안 씀 **2** · 모름 2 → **0**.
  - `_LDR_DATA_TABLE_ENTRY` nt 1 → **2** (`SigningLevel` — `NtGetCachedSigningLevel` 출력, 조건부) · ntdll 41 → **65** · 모름 24 → **0**.
  - `_LDR_DDAG_NODE` ntdll 6 → **10** · 모름 4 → **0**.
  - 유저 7개 합 nt 118 → **124** · ntdll 136 → **174** · 모름 194 → **102** (남은 건 KUSD 102 뿐).
  - 구조체 21개 1,456 = 아는 칸 816 → **858** (정확 785 → **827** · 짐작 31) + 아무도 안 씀 157 → **185** + 범위 밖 240 → **262** + 덮임 25 + 모름 218 → **126**.
  - 아는 칸 중 끝까지 0 (짐 지도 7.1): 327 → **362** (0 을 씀 15 + 조건부 안 탐 20).

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: **ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`**, ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음 — 결과서, sast/48 이 사진 로그와 맞춘 값).
  - 다시 analyze 안 함. ntdll 새로 뽑은 것 11개 (아래 "방법"), 나머지는 이미 있던 `.asm` 읽기만. **ntoskrnl Ghidra 프로젝트는 안 열었다** (그때 sast/108 이 돌던 중) — 커널은 있던 `.asm` (`MmCreatePeb` · `DbgkCopyProcessDebugPort`) + 결과서 스크래치 `objdump` 만.
- 버전 대조 (결과서): ntdll `LdrInitializeThunk +0xe → LdrpInitialize` · `_LdrpInitialize +0x16c → LdrpInitializeProcess` · `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse`, 커널 `NtCreateUserProcess +0xc36 → PspAllocateProcess` · `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다. 인용한 `+0x` 64곳을 결과서가 `.asm` 줄로 다시 맞췄다.
- 사진 값 대조: 결과서가 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_{P1,P1a,P1b,P2}.txt`) 를 칸마다 다시 읽었다. 새 동적 실행은 없다.
- **sast/109 결과만 넣었다.** sast/110 ~ 120 결과는 이 문서에 안 넣었다 (다음 바퀴들 몫).

## 목적

- **Q78e1** (Q78e "ntdll 앞쪽, 힙 빼고" 의 유저 구조체 몫): 짐 지도 7.2 의 `_PEB` 62 · `_PEB_LDR_DATA` 2 · `_LDR_DATA_TABLE_ENTRY` 24 · `_LDR_DDAG_NODE` 4 칸이 언제 · 누가 처음 쓰나.
  - `P1~P1a` 4칸 (`CrossProcessFlags` · `FlagGroup` · `NodeModuleLink` · `LoadTime`) 과 `P1a~P1b` 3칸 (`PatchLoaderData` · `SwitchBackContext` · `PreorderNumber`) 의 쓰는 줄.
  - `끝까지 0` 85칸을 네 가지 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길) 로 가르기.

## 방법

- 정적: `_workspace/sast/109_ntdll-user-structs.md` — ntdll, 모두 depth 0 `^이름$`.
  - 새로 뽑음 11개: `LdrpFindExistingModule` · `LdrpIncrementModuleLoadCount` · `LdrpUnloadNode` · `LdrpLoadShimEngine` · `LdrpGetProcApphelpCheckModule` · `AVrfpLoadAndInitializeProvider` · `RtlpRemoveVectoredHandler` · `SetAssertBufferPtrinPeb` · `RtlSetProcessPlaceholderCompatibilityMode` · `RtlpWakeByAddress` · `LdrpDecrementNodeLoadCountLockHeld` (칸을 쓰는 줄이 든 함수인데 `.asm` 이 없던 것). `_calls.tsv` 4,083 → 4,192 줄.
  - 읽기만: `_LdrpInitialize` · `LdrpInitializeProcess` · `LdrpAllocateModuleEntry` · `LdrpInsertDataTableEntry` · `LdrpInitializeNtdllDataTableEntry` · `LdrpProcessMappedModule` · `LdrpInitializePatchData` · `LdrpInitializeExecutionOptions` · `LdrpSendPostSnapNotifications` · `SbUpdateSwitchContextBasedOnDll` · `LdrpCondenseGraphRecurse` · `LdrpMergeNodes` · `LdrpPrepareModuleForExecution` · `LdrpAddNodeServiceTag` · `LdrpSetModuleSigningLevel` · `LdrpValidateIntegrityContinuity` · `LdrpMapDllSearchPath` · `LdrpMapDllNtFileName` · `LdrpApplyPatchImage` · `RtlpAddVectoredHandler` · `RtlpCallVectoredHandlers` 등. 커널 `MmCreatePeb.asm` · `DbgkCopyProcessDebugPort.asm`.
  - 훑기 (결과서 스크래치, 커밋 안 함): ntdll 전체 `objdump` 풀이에서 PEB 레지스터 추적 (`gs:0x60` · `[TEB+0x60]` · 레지스터 · 스택 칸 · 오프셋 이동 · 인자로 넘긴 함수에 씨앗), 모듈 항목은 오프셋 + 목록 고리 7종 기준, `Flags` 는 `or/and/bts/btr/mov` 꼴 전수 + 바이트 꼴 `[+0x69]` · `[+0x6a]`. 커널은 `[EPROCESS+0x2e0]` 로 읽은 PEB 를 따라가는 **직선 훑기** (분기 합침 없음), 다른 DLL (user32 · gdi32 · gdi32full · kernel32 · KernelBase) 은 `objdump` 만, win32kbase 는 `GdiProcessCallout` 하나만.
  - **훑기 한계**: PEB 포인터를 전역 · 구조체 칸 · 간접 호출 인자로 받으면 놓칠 수 있다. 커널 쪽 "못 찾음" 은 "없다" 가 아니라 `[공백]` 이다 (첫 판은 `test r,r` 처리 버그로 `PspApplyWorkingSetLimitsToProcess` 를 놓쳤다가 고쳐 다시 돌림 — 결과서).
- 결과서: `_workspace/sast/109_ntdll-user-structs_result.md` (**분석성공** — 스크립트 문제 없음, 제안 셋).
- flow-writer 가 다시 본 것:
  - ntdll `.asm` 줄: `LdrpAllocateModuleEntry +0xa` · `+0x1a` · `+0x20` · `+0x23` · `+0x29` · `+0x3a` · `+0x40` · `+0x4d` · `+0x53` · `+0x5b` · `+0x73` · `+0xd4` · `+0xdb` · `+0xde` · `+0xe2` · `+0xe5` · `+0xe9` · `+0x10d` · `+0x11a` · `+0x129`, `LdrpProcessMappedModule +0xa6`, `LdrpCondenseGraphRecurse +0x13` · `+0x1e` · `+0x24` · `+0x2a` · `+0x6a` · `+0x9a` · `+0xd3`, `LdrpInitializePatchData +0x2d` · `+0x49` · `+0x52`, `LdrpInitializeProcess +0x13be` · `+0x13c1` · `+0x13c4` · `+0x19e5` · `+0x1b1e` · `+0x1b24` · `+0x1d16`, `SbUpdateSwitchContextBasedOnDll +0xac` · `+0xd9`, `_LdrpInitialize +0x107` · `+0x20f`, `LdrpInitializeExecutionOptions +0xa6` · `+0xad` · `+0xd1` · `+0x2b6` · `+0x5a9` · `+0x71c` · `+0x725`, `LdrpSendPostSnapNotifications +0xa3` · `+0xce`.
  - ntoskrnl `.asm` 줄: `MmCreatePeb +0x4ac` · `+0x4b2` · `+0x573` · `+0x57a`, `DbgkCopyProcessDebugPort +0x3c` · `+0x43`, `KiBreakpointTrap +0x14 ~ +0x30` (덤 확인 — 아래 "정정").
  - `_calls.tsv`: 아래 표의 call · ret 줄. `DbgkpMarkProcessPeb` · `PspApplyWorkingSetLimitsToProcess` · `PspTrySetProcessPebThrottlingFlags` · `EtwpCoverageEnsureUserModeView` · `EtwpUpdateProcessTracingCallback` 은 `.asm` 이 없다 — 오프셋은 결과서 커널 `objdump` `[정적: objdump]`.
  - 사진: `ms/logs/04_baggage-read_P1.txt:1061 ~ 1069` (덤 확인).

## 결과

비유 (새 가게 = test.exe 프로세스):

- **PEB** 는 가게 문 앞 안내판이다. 칸이 124개인데, 이번에 남은 62칸을 다 봤더니 "누가 어떤 때 적는 칸" 이 대부분 **우리 가게엔 해당 없음** 이었다 — 디버거 손님이 있을 때 (`BeingDebugged`), 체인점일 때 (`ProcessInJob`), 그림 그리는 가게일 때 (GDI 칸 셋) 만 적는 칸이다.
- **모듈 항목** 은 직원 명부의 한 줄이다. 새 줄을 만들 때 (`LdrpAllocateModuleEntry`) 연필 자국 없는 종이 (`HEAP_ZERO_MEMORY`) 를 쓰고, 명부 줄과 조직도 칸 (노드) 을 서로 가리키게 묶고 (`NodeModuleLink`), 입사 시각 (`LoadTime`) 을 공용 시계 (KUSD `SystemTime`) 에서 베낀다.
- **노드 번호** (`PreorderNumber`) 는 조직도를 처음 걸어 볼 때 붙이는 "방문 순번" 이다. 걷는 사람은 켜기 담당 (`LdrpInitializeGraphRecurse`) 이 아니라 **묶기 담당** (`LdrpCondenseGraphRecurse`) 이다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x1b24` | `+0x1b29` | `LdrpInitializePatchData` | `+0x1b1e LdrInitState = 2` 바로 뒤, 무조건 → PEB `PatchLoaderData` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1d16` | `+0x1d1b` | `CALL [RDI+0x230]` (CFG 간접) | PEB `PostProcessInitRoutine` 을 **부르는** 쪽 (null 이면 건너뜀). 이 칸을 쓰는 쪽은 user32 | [정적] |
| `ntdll!LdrpAllocateModuleEntry` | `+0x29` · `+0x5b` | `+0x2e` · `+0x60` | `RtlAllocateHeap` ×2 | 항목 `0x138` · 노드 `0x50` **따로 두 번**, 둘 다 `LdrpHeap` · `NtdllBaseTag + 0x40000 \| 8` (`HEAP_ZERO_MEMORY`) | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0xa3` | `+0xa8` | `SbUpdateSwitchContextBasedOnDll` | 노드 알림 때 모듈마다 → 항목 `SwitchBackContext` (`+0xd9`) | [정적] (run66 그대로) |
| `ntdll!LdrpPrepareModuleForExecution` | `+0x12a` | `+0x12f` | `LdrpCondenseGraph` | 노드 `State 5` 일 때 (run21) | [정적] (run24 그대로) |
| `ntdll!LdrpCondenseGraph` | `+0x21` | `+0x26` | `LdrpCondenseGraphRecurse` | → `+0x1e` `PreorderNumber` · `+0x24` `LowestLink` · `+0x2a` / `+0x6a` / `+0x9a` `CondenseLink` | [정적] |
| `ntdll!LdrpCondenseGraphRecurse` | `+0xd3` | `+0xd8` | `LdrpMergeNodes` | 고리 (여럿 묶음) 일 때만 — test.exe 비순환이라 안 탐 | [정적] (run24 그대로) |
| `ntdll!LdrpPrepareModuleForExecution` | `+0x6a` | `+0x6f` | `LdrpAddNodeServiceTag` | 항목 `Flags & 0x20` 꺼짐 · TEB `SubProcessTag` ≠ 0 일 때 → 노드 `ServiceTagList` (조건부 안 탐) | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0x29e ~ +0x2a9` · `+0x59c` | — | `RtlQueryApplicationKeyOption` | IFEO `TracingFlags` · `GlobalFlag2` (IFEO 부분 `+0xad` · `+0xd1` 로 통째 건너뜀 — test.exe) | [정적] |
| `nt!PspInsertProcess` | `+0x13e` | `+0x143` | `DbgkCopyProcessDebugPort` | → 부모 `DebugPort` (`+0x3c CMP [RDX+0x308]`) ≠ 0 일 때만 조각 안 `DbgkpMarkProcessPeb` (ret `+0x2c8972`) → PEB `BeingDebugged` | [정적] (run11 + 이번) |
| `nt!PspInsertProcess` | `+0xd4` | `+0xd9` | `PspImplicitAssignProcessToJob` | 부모가 작업 (job) 안일 때 → `+0x145` `PspApplyWorkingSetLimitsToProcess` (`+0x19b` PEB `ProcessInJob`) | [정적] (run11 + 이번, 안쪽은 `objdump`) |
| `nt!PspAllocateProcess` | `+0x230f` | `+0x2314` | `MmCreatePeb` | `+0x57a` `LeapSecondFlags` 명시 0 (`+0x573` `LeapSecondData` 바로 뒤) | [정적] (run10 그대로) |
| `ntdll!LdrpInitializeProcess` | `+0x14e9` | `+0x14ee` | `LdrpProcessMappedModule` | exe 항목 → `+0xa6` `LoadTime` (ntdll 항목은 `LdrpInitializeNtdllDataTableEntry +0x11f`, DLL 은 `LdrpMapDllWithSectionHandle +0x267`) | [정적] (run35 그대로) |

### 호출 관계

```
[test.exe 첫 스레드 — ② ntdll]
_LdrpInitialize  +0x107 CrossProcessFlags 비트 1 켬 (BTS.LOCK) … +0x20f 끔
 └─ +0x16c  LdrpInitializeProcess
     ├─ (exe) +0x135e  LdrpAllocateModuleEntry   (아래)
     │        +0x13be ~ +0x13c4  exe 항목 Flags DWORD 되쓰기 (|= 4)
     ├─ +0x14ee  LdrpProcessMappedModule  +0xa6 LoadTime ← KUSD SystemTime
     ├─ +0x19e5  AppCompatInfo = 0   (shim 블록 · [R13+0x210] ≠ 0 일 때만 — 안 탐)
     ├─ +0x1af2  LdrpPrepareModuleForExecution
     │            ├─ +0x6f   LdrpAddNodeServiceTag        (SubProcessTag ≠ 0 일 때만 — 안 탐)
     │            ├─ +0x12f  LdrpCondenseGraph ─ +0x26 LdrpCondenseGraphRecurse
     │            │                                   +0x1e PreorderNumber · +0x24 LowestLink
     │            │                                   +0x2a / +0x6a / +0x9a CondenseLink (끝에 0)
     │            └─ +0x7b   LdrpNotifyLoadOfGraph ─ +0x25 LdrpSendPostSnapNotifications
     │                                                  └─ +0xa8 SbUpdateSwitchContextBasedOnDll  +0xd9 SwitchBackContext
     └─ +0x1b29  LdrpInitializePatchData  +0x52 PEB PatchLoaderData ← LdrpHeap 0x18B 블록

LdrpAllocateModuleEntry  (+0x2e 항목 0x138 · +0x60 노드 0x50, 둘 다 HEAP_ZERO_MEMORY)
  +0x73 DdagNode · +0xdb/+0xde NodeModuleLink ↔ 노드 +0xe2/+0xe5 Modules · +0xe9 Lock = 0
  +0x10d 비트 28 / +0x129 비트 26 (로드 문맥 비트, 조건부) · +0x11a ImageDll

[① 커널 — 부모 cmd 스레드]
PspAllocateProcess ─ +0x2314 MmCreatePeb  +0x4b2 NtGlobalFlag2 (run111) · +0x57a LeapSecondFlags = 0
PspInsertProcess ─ +0xd9  PspImplicitAssignProcessToJob ─ PspApplyWorkingSetLimitsToProcess +0x19b ProcessInJob   (job 일 때만)
                 └ +0x143 DbgkCopyProcessDebugPort ─ DbgkpMarkProcessPeb +0x84 BeingDebugged                  (DebugPort 일 때만)
```

### 1. `P1~P1a` 4칸 · `P1a~P1b` 3칸 (결과서 질문 1 · 2)

**답: 7칸 모두 쓰는 줄을 찾았고 이미 아는 길 안이다. 쓴 주인 모두 ntdll** `[정적]`. 사진 구간과 맞다 `[관찰]` run44.

| 칸 | 쓰는 줄 | 값 · 모양 | 사진 |
|---|---|---|---|
| PEB `+0x50` `CrossProcessFlags` (정수 칸) | `_LdrpInitialize +0x107 BTS.LOCK [RSI+0x50],1` → `+0x20f BTR.LOCK` | 비트 1 `ProcessInitializing` — 짐 지도 3.1 의 그 줄과 **같은 칸** (PDB 가 정수 칸 · 비트 칸을 따로 세서 모름에 남았을 뿐) | P1 0 → P1a 2 → P1b 2 → P2 0 (`_P1.txt:1576` · `_P1a.txt:1743` · `_P1b.txt:2441` · `_P2.txt:2453`) |
| 항목 `+0x68` `FlagGroup` | 같은 DWORD 가 `Flags` — 처음 값 줄은 `LdrpAllocateModuleEntry +0x10d` · `+0x11a` · `+0x129` · `+0x194`, ntdll 항목 `LdrpInitializeNtdllDataTableEntry +0x93 OR 0x204`, exe 항목 `LdrpInitializeProcess +0x13c4` | 3.5 `Flags` 줄과 같은 칸 | exe `0x22c4` · ntdll `0x20a2c4` (`_P1a.txt:2690` · `:2795`) |
| 항목 `+0xa0` `NodeModuleLink` | `LdrpAllocateModuleEntry +0xd4 LEA RAX,[RBX+0xa0]` → `+0xdb` · `+0xde` (= 노드 주소), 노드 쪽 `+0xe2` · `+0xe5` | 노드 `Modules` 와 서로 가리킴 (모듈 하나든 목록). 다시 옮기는 쪽은 `LdrpMergeNodes` (고리일 때만) | `[노드 - 노드]` (`_P1a.txt:2729`) |
| 항목 `+0x100` `LoadTime` | `LdrpProcessMappedModule +0xa6` | KUSD `SystemTime` (`0x7ffe0014`) 8B 사본 | exe · ntdll 같은 `0x01dd646b35091f27` (`_P1a.txt:2736` · `:2841`), k32 쪽은 뒤 값 (`_P1b.txt:3644`) |
| PEB `+0x330` `PatchLoaderData` | `LdrpInitializePatchData +0x52` (`+0x49` `GS:[0x60]`) ← `LdrpInitializeProcess +0x1b24` | `LdrpHeap` `HEAP_ZERO_MEMORY` 0x18B 블록 — `[0] = 1` (`+0x2d`) · `[8] = &LdrLoadDll` · `[0x10] = &LdrGetProcedureAddress` | P1a (null) → P1b `0x25996f3bfa0` (`_P1a.txt:1811` · `_P1b.txt:2509`) |
| 항목 `+0xc0` `SwitchBackContext` | `SbUpdateSwitchContextBasedOnDll +0xd9 MOV [R14+0xc0],RDI` ← `LdrpSendPostSnapNotifications +0xa3` | `SbpDetermineDllContext` 가 돌려준 호환 문맥 블록 `+4` 주소. 인자 `R8D ≠ 0` 이면 건너뜀 (`+0xac` · `+0xaf`) | P1a null → P1b exe `0x7ffafb7d3014` (`_P1b.txt:3430`) |
| 노드 `+0x48` `PreorderNumber` | `LdrpCondenseGraphRecurse +0x13 INC [RDX]` → `+0x1e MOV [RCX+0x48],EAX` (같은 값 `+0x24` `LowestLink`) | 묶기 방문 순번 (공용 카운터). run24 · flow.md 2.6 이 이미 적은 "번호 매기기" 의 줄 | P1b exe 1 · ntdll 0 · k32 1 · kb 2 · dll1 2 · dll2 3 (`_P1b.txt:3474 ~ 3999`) |

- `SwitchBackContext` 값의 출처: ntdll 기준 주소 `0x7ffafb660000` (= PEB `Ldr` − `PebLdr` rva `0x1d2880`) 에서 `+0x173014` = 표 rva `0x173010 + 번호 × 0x20` 의 번호 0 칸 `+4`, k32 쪽 `+0x173094` 는 번호 4 (결과서 `[관찰 계산]`). run66 4장 정적 ③ "`SbUpdateSwitchContextBasedOnDll` 이 이 칸을 쓰나" 가 닫혔다.
- `PatchLoaderData` 블록이 프로세스 힙 구역 (`0x25996f30000`) 안이라 `LdrpHeap` = 프로세스 힙으로 보인다고 결과서가 `[추측]` 으로 적고 D5 로 넘겼다. **사진에 이미 답이 있다** (flow-writer): P1a `LdrpHeap` (`ntdll+0x1d2860` = `0x7ffafb832860`) = `0x25996f30000` (`ms/logs/04_baggage-read_P1a.txt:5120`) = PEB `ProcessHeap` (`:1739`) `[관찰]`. 짐 지도 1장 "항목 주소가 프로세스 힙 = `LdrpHeap` 영역 안 (run44)" 과도 같다.

### 2. `끝까지 0` 85칸 가르기 (결과서 질문 3)

**답: 0 을 씀 15 · 조건부 안 탐 20 · 아무도 안 씀 28 · 범위 밖 22** `[정적]`. 85칸 모두 사진 8장에서 0 이다 (`끝까지 0` 정의). 칸마다 줄 · 경로는 짐 지도 3.1 · 3.4 · 3.5 · 3.6 의 새 줄 (근거 칸 `sast109`).

#### 2.1 0 을 씀 (15)

- PEB `LeapSecondFlags` (`+0x7c0` DWORD) · `.SixtySecondEnabled` · `.Reserved` (3) — **nt `MmCreatePeb +0x57a AND dword [RBX+0x7c0],0`** (명시 0, `+0x573 LeapSecondData` 바로 뒤, 무조건). ntdll 이 `SixtySecondEnabled` 를 켜는 줄 `LdrpInitializeExecutionOptions +0x725 OR [RBX+0x7c0],1` 은 `+0x71c TEST byte [RBX+0x7c4],1` (`NtGlobalFlag2` 비트 0) 이 켜졌을 때만 — 값 0 이라 안 탐. 이 줄은 IFEO 건너뛰기 (`+0x604`) **뒤**라 IFEO 키와 상관없다 (flow-writer `.asm`).
- 항목 `Lock` (`+0x90`) — `LdrpAllocateModuleEntry +0xe9 MOV qword [RBX+0x90],0`.
- 항목 `Flags` 비트 10개 — `LdrpInitializeProcess +0x13be ~ +0x13c4` DWORD 되쓰기 (위 셈 결정). exe 항목만이고, 다른 항목은 할당 0 (`HEAP_ZERO_MEMORY`) 그대로다 — 비트가 켜지는 줄이 없으니 값은 같다.
- 노드 `CondenseLink` (`+0x40`) — `LdrpCondenseGraphRecurse +0x2a` 가 스택에 올릴 때 이전 머리를 넣고, 뽑을 때 `+0x6a` (`RAX = 0`, `+0x63 XOR`) · `+0x9a MOV qword [R15],0` (명시 0). 비순환 (test.exe) 이면 노드마다 `LowestLink == PreorderNumber` (`+0x4d CMP` · `+0x50 JNZ` 안 감) 라 바로 뽑혀 0 으로 끝난다 — 사진 0 과 맞음.

#### 2.2 조건부 안 탐 (20) — test.exe 가 왜 안 타나

| 칸 | 켜는 줄 | test.exe 가 안 타는 까닭 |
|---|---|---|
| PEB `BeingDebugged` | nt `DbgkpMarkProcessPeb +0x84` (`CL` = `EPROCESS.DebugPort ≠ 0`) ← `PspInsertProcess +0x13e DbgkCopyProcessDebugPort` (`+0x3c` 부모 `DebugPort` 비교 · `+0x43 JNZ`) | 부모 cmd 가 디버거 아래 아님. KDNET 은 `DebugPort` 를 안 쓴다 (결과서) — run44 `BeingDebugged 0` `[관찰]` |
| PEB `CrossProcessFlags.ProcessInJob` (비트 0) | nt `PspApplyWorkingSetLimitsToProcess +0x19b LOCK BTS [R14+0x50],0` ← `PspInsertProcess +0xd4 PspImplicitAssignProcessToJob` (`+0x145`) | 부모가 작업 (job) 안이 아님. 사진 `ProcessInJob 0y0` |
| PEB `AppCompatInfo` (`+0x2e0`) | `LdrpInitializeProcess +0x19e5` (0 을 씀) | `+0x19d7 TEST R13` · `+0x19dc CMP [R13+0x210]` 를 통과해야 — 바로 뒤 `LdrpInitShimEngine`. shim 안 켜짐 (`g_ShimsEnabled` 0, run66). 탔어도 0 |
| PEB `TracingFlags` + 비트 4칸 (5) | `LdrpInitializeExecutionOptions +0x2b6 LOCK OR [RBX+0x378],EAX` (IFEO 값) | IFEO 부분을 `+0xa6 TEST [RAX+0x8],0x4000` · `+0xad JNZ` / `+0xd1 JS` 로 통째 건너뜀 (매개변수 `Flags 0x4001`). 범위 밖에서도 씀: nt `EtwpUpdateProcessTracingCallback +0x7e` · `+0x8d` (ETW 세션) |
| PEB `WaitOnAddressHashTable` (1024B) | `RtlpWakeByAddress +0x5d` · `+0x84` · `+0xdb` · `+0x18f` (`LOCK CMPXCHG` 슬롯) | 어떤 스레드가 실제로 주소에서 기다려야 쓰임. 지도 안 `RtlpWaitOnAddress` 는 경합 길에서만 불림 |
| 항목 `Flags.PackagedBinary` (비트 0) | `LdrpMapDllSearchPath +0x13a` · `+0x370`, `LdrpFindExistingModule +0xaf`, exe `LdrpInitializeProcess +0x13cd` | 패키지 앱 아님 (PEB `BitField` 비트 4 `IsPackagedProcess` 0) |
| 항목 `Flags.MarkedForRemoval` (비트 1) | `LdrpUnloadNode +0x211` | 언로드 때만 |
| 항목 `Flags.ShimDll` (비트 8) | `LdrpLoadShimEngine +0x1d9` · `LdrpInitShimEngine +0xed` · `LdrpGetProcApphelpCheckModule +0x271` | shim 안 켜짐 |
| 항목 `Flags.VerifierProvider` (비트 10) | `AVrfpLoadAndInitializeProvider +0x186` | App Verifier 안 켜짐 |
| 항목 `Flags.ProcessAttachFailed` (비트 20) | `LdrpInitializeNode +0x29e` (`0xc0000142`, DLL 시작 실패) | 시작 코드 다 성공 |
| 항목 `Flags.ChpeEmulatorImage` (비트 26) | `LdrpAllocateModuleEntry +0x129` (로드 문맥 `+0x20` 비트 27, `+0x11e BT`) | CHPE 에뮬레이터 로드 아님 `[추측: 비트 뜻]` |
| 항목 `Flags.Redirected` (비트 28) | `LdrpAllocateModuleEntry +0x10d` (로드 문맥 `+0x20` 비트 2, `+0x107 TEST`) | 리다이렉트로 찾은 모듈 없음 `[추측: 비트 뜻]` |
| 항목 `SigningLevel` (`+0x11c`) | `LdrpSetModuleSigningLevel +0x1e LEA` → `+0x42 NtGetCachedSigningLevel` (커널이 씀) · 실패 갈래 `LdrpValidateIntegrityContinuity +0x9d` · `+0xd1` (1) | `LdrpMapDllNtFileName +0x141 CMP [LdrpAuditIntegrityContinuity],0` — 전역 0 (사진) |
| 항목 `ActivePatchImageBase` (`+0x128`) | `LdrpInitializeNtdllDataTableEntry +0x264` · `LdrpApplyPatchImage +0x15d` | 핫패치 ntdll 아님 (`HotPatchState` 0) |
| 노드 `ServiceTagList` (`+0x10`) | `LdrpAddNodeServiceTag +0x67` ← `LdrpPrepareModuleForExecution +0x6a` | TEB `SubProcessTag` 0 (run54) |
| 노드 `LoadWhileUnloadingCount` (`+0x1c`) | `LdrpIncrementModuleLoadCount +0x201 INC` (줄이는 쪽 `LdrpDecrementNodeLoadCountLockHeld +0x84`) | 내려가는 노드를 다시 늘리는 일 없음 |

#### 2.3 아무도 안 씀 (초기화 길 안) (28)

- PEB 26: `ReadImageFileExecOptions` · `Padding0` · `SubSystemData` · `IFEOKey` · `ReservedBits0` · `Padding1` · `SystemReserved` · `AtlThunkSListPtr32` · `TlsExpansionCounter` · `Padding2` · `AnsiCodePageData` · `OemCodePageData` · `UnicodeCaseTableData` · `ProcessStarterHelper` · `Padding3` · `Padding4` · `Padding5` · `SparePointers` · `ChpeV2ProcessInfo` · `AppModelFeatureState` · `SpareUlongs` · `UnusedNlsField` · `EcCodeBitMap` · `pImageHeaderHash` · `Padding6` · `PlaceholderCompatibilityModeReserved`.
- `_PEB_LDR_DATA` 2: `SsHandle` · `EntryInProgress` — 전역 `PebLdr` `+0x8` · `+0x40` 을 가리키는 명령 0건, `[PEB+0x18]` 추적에도 없음.
- **요청서 짐작과 다른 점**:
  - `IFEOKey` · `ReadImageFileExecOptions` 는 "IFEO 키 없음이라 안 탐" 이 아니라 **쓰는 줄 자체가 없다**. `LdrpInitializeExecutionOptions` 는 키 핸들을 지역 `[RSP+0x68]` (`+0xb8 LEA R9`) 에만 두고 끝에 닫는다 (run46 "호출자에게 돌려줄 뿐" 을 더 강하게 확인).
  - `AnsiCodePageData` · `OemCodePageData` · `UnicodeCaseTableData` 는 24H2 커널이 안 채운다 — `MmCreatePeb` 은 코드 페이지 **번호** 셋 (`ActiveCodePage` · `OemCodePage` · `UseCaseMapping`) 만 쓴다. 사진 P2 도 null.
- `Padding*` 7 · 예약 6 · WOW64 · ARM64 전용 3 · 옛 칸 2 · `TlsExpansionCounter` (`RtlTlsAlloc` 이 안 읽음) 는 까닭이 있다. **`AppModelFeatureState` · `pImageHeaderHash` 는 이름에 쓰는 쪽이 있을 법한데 못 찾았다** `[공백]` (ntdll · nt · user32 · gdi32 · kernel32 · KernelBase 훑기 안).

#### 2.4 범위 밖 길에서 씀 (22)

- **ntdll 4** (지도 밖 함수, 이번에 `.asm` 뽑음): `ProcessUsingVEH` · `ProcessUsingVCH` (`RtlpAddVectoredHandler +0x1af` 켬 · `RtlpRemoveVectoredHandler +0x1b8` · `RtlpCallVectoredHandlers +0x262` 끔 — run82 그대로) · `WerShipAssertPtr` (`SetAssertBufferPtrinPeb +0x52`) · `PlaceholderCompatibilityMode` (`RtlSetProcessPlaceholderCompatibilityMode +0x1d`, 같은 이름 nt 함수 `+0x2c` 도).
- **nt 6**: `ProcessPreviouslyThrottled` · `ProcessCurrentlyThrottled` (`PspTrySetProcessPebThrottlingFlags +0x5f` · `+0x71`) · `TelemetryCoverageHeader` (`EtwpCoverageEnsureUserModeView +0x140` ← `EtwSetProcessTelemetryCoverage +0x4f`) · **쓰는 줄 못 찾음 `[공백]` 셋**: `ProcessImagesHotPatched` (핫패치 길 `[추측]`) · `CloudFileFlags` · `CloudFileDiagFlags` (클라우드 파일 필터 cldflt 짐작 `[추측]`).
- **다른 DLL · 모듈 12**: user32 `_UserClientDllInitialize` — `KernelCallbackTable` · `UserSharedInfoPtr` (`+0x58` 겹침, `+0x244` · `+0x3fc` · `+0x524`) · `PostProcessInitRoutine` (`+0x409`, 0) · `Win32ClientProcInfo` (`+0xae` · `+0x760`), user32 `InitProcessGdiScaling +0xc8` — `AppCompatFlagsUser`, win32kbase `GdiProcessCallout` — `GdiSharedHandleTable` (`+0x22b`, run86) · **`GdiDCAttributeList` (`+0xe9`) · `GdiHandleBuffer` (`+0xef ~ +0xfe` 명시 `memset(…, 0, 0xf0)`)** (이번에 처음 — run86 은 "이번 줄기에 안 나옴"), gdi32 `hGetPEBHandle +0x57` · `+0x159` (`GdiHandleBuffer` 를 핸들 캐시로), 짐작 `[추측]` 넷: `AppCompatFlags` (shim · apphelp — ntdll 은 `LdrpInitializeProcess +0x19fd` 읽기만) · `AtlThunkSListPtr` (atlthunk) · `ProcessUsingFTH` (fth) · `WerRegistrationData` (wer).

### 3. 간접 호출 (결과서 질문 4)

- **칸을 쓰는 간접 호출 0개** `[정적]`. 줄이 든 함수 20여 개의 `indirect:` 는 shim 콜아웃 둘 (`LdrpSendPostSnapNotifications +0x122` · `LdrpMapDllSearchPath +0x3b4`) · `LdrpInitShimEngine +0x160` · `LdrpInitializeProcess` 넷 (`+0x17fc` WOW64 · `+0x1b59` `Kernel32ThreadInitThunkFunction` · `+0x1cdf` · `+0x1d16` `PostProcessInitRoutine`) · `_LdrpInitialize +0x9a` (WOW64) — 이미 알려졌거나 칸을 안 쓴다.
- 칸을 바꿀 수 있는 간접 호출은 shim 콜아웃 (`g_pfnSE_*`) 뿐이고 `g_ShimsEnabled` 0 이라 안 탄다 → Q35 에 새로 넣을 것 없음 (메모 한 줄).

## 확인된 것

- `[정적]` PEB `CrossProcessFlags` 정수 칸 = `_LdrpInitialize +0x107` · `+0x20f` (`ProcessInitializing` 줄과 같은 칸).
- `[정적]` 모듈 항목 `NodeModuleLink` · `Lock` · `Flags` 비트 26 · 28 = `LdrpAllocateModuleEntry +0xdb` · `+0xde` · `+0xe9` · `+0x129` · `+0x10d`, `LoadTime` = `LdrpProcessMappedModule +0xa6` (KUSD `SystemTime`).
- `[정적]` **항목 `0x138` 과 노드 `0x50` 은 따로 두 번 할당**한다 (`+0x29` · `+0x5b`, 둘 다 `LdrpHeap` · `HEAP_ZERO_MEMORY`). 항목 뒤에 노드가 붙어 있는 한 덩어리가 아니다 — 노드는 늘 `DdagNode` (`+0x98`, `+0x73` 이 씀) 로 찾는다.
- `[정적]` PEB `PatchLoaderData` = `LdrpInitializeProcess +0x1b24 → LdrpInitializePatchData +0x52` (무조건, `LdrInitState = 2` 바로 뒤).
- `[정적]` 항목 `SwitchBackContext` = `LdrpSendPostSnapNotifications +0xa3 → SbUpdateSwitchContextBasedOnDll +0xd9`.
- `[정적]` 노드 `PreorderNumber` · `LowestLink` · `CondenseLink` = `LdrpCondenseGraphRecurse +0x1e` · `+0x24` · `+0x2a / +0x6a / +0x9a`.
- `[정적]` PEB `LeapSecondFlags` = `MmCreatePeb +0x57a` 명시 0.
- `[정적]` `IFEOKey` · `ReadImageFileExecOptions` 를 쓰는 줄은 ntdll · nt 에 없다.
- `[정적: objdump]` PEB `BeingDebugged` · `ProcessInJob` 을 켜는 커널 줄 = `DbgkpMarkProcessPeb +0x84` · `PspApplyWorkingSetLimitsToProcess +0x19b` (부모가 디버거 아래 / 작업 안일 때만).
- `[정적: objdump]` PEB `GdiDCAttributeList` · `GdiHandleBuffer` 를 쓰는 곳 = win32kbase `GdiProcessCallout +0xe9` · `+0xfe` (GDI 를 켜는 프로세스만).

## 결과서와 다르게 적은 곳 · 보탠 것

- **칸 수**: 결과서 93칸 → 이 문서 92칸 (`NtGlobalFlag2` 는 run111 이 이미 3.1 로 옮김). 그래서 "0 을 씀" 16 → 15.
- **양쪽 셈 하나 · 줄 나눔 하나**: `LeapSecondFlags.SixtySecondEnabled` 는 결과서가 "0 쪽으로 셈" 했지만, 짐 지도 1장 규칙 ("한 칸을 nt 와 ntdll 이 둘 다 쓰면 양쪽에 센다" — run70 `MinimumStackCommit` · run111 `NtGlobalFlag2` 와 같은 꼴) 대로 nt 줄 (0) · ntdll 줄 (조건부 켬) 둘로 나눠 **양쪽에 셌다**. `SigningLevel` 도 같다 — nt (`NtGetCachedSigningLevel` 출력) · ntdll (`LdrpValidateIntegrityContinuity` 실패 갈래) 둘. 가름 (0 을 씀 / 조건부 안 탐) 숫자는 그대로다.
- **경로 줄임**: 결과서의 `①I…` 는 짐 지도에 이미 같은 뜻 (run50 `①… → PspInsertProcess (호출 +0xfbd)`) 으로 있어 그대로 썼다. `②C…` 는 새로 뒀다 — **`②C…` = `②P… → LdrpCondenseGraph (호출 +0x12a) → LdrpCondenseGraphRecurse (호출 +0x21)`** (`_calls.tsv`). 결과서는 `②… →` 로 적었지만 `LdrpPrepareModuleForExecution` 은 짐 지도에서 `②P…` 다.
- 결과서 3.4 "이 둘은 3.4 에 이미 있음" 의 `ShutdownInProgress` (`RtlpProcessReflectionStartup +0x280`) 는 짐 지도 3.4 에 **없다** (있는 건 `LdrShutdownProcess +0xae`). 프로세스 반사 길 (범위 밖) 이라 칸 셈은 그대로, 3.4 줄 근거에 이름만 보탰다 (flow-writer 가 `.asm` 은 안 봄 — 결과서 훑기 줄).
- 커널 쪽 `DbgkpMarkProcessPeb` · `PspApplyWorkingSetLimitsToProcess` · `PspTrySetProcessPebThrottlingFlags` · `Etwp*` 오프셋은 `.asm` 이 없어 결과서 `objdump` 로만 있다 — 짐 지도 근거 칸에 `[정적: objdump]` 로 적었다 (결과서 "64곳 `.asm` 대조" 는 ntdll · 있던 커널 `.asm` 몫).

## 정적과 동적이 다른 곳

- 없다. 7칸 처음 값 · 85칸 0 모두 사진 구간과 맞다.
- **디버거가 붙어서 달라지는 점**: 없음 (결과서 — KDNET 은 `EPROCESS.DebugPort` · PEB `BeingDebugged` 를 안 건드린다). 사용자 모드 디버거 아래서 띄우면 `BeingDebugged` 가 1 이 될 것 `[추측]`.

## 정정

- **run112** (`runs/03_sprint/run112-thread-later.md` 3절 트랩 프레임 표 `Rax · R8 ~ R11` 줄과 그 아래 "이 24칸은 P1 에서도 0") — 두 말이 어긋나 보였는데 **둘 다 맞다**. `KiBreakpointTrap +0x18 · +0x24 ~ +0x30` 은 P1 에서도 이 다섯 칸을 쓴다 (`.asm` — `[RBP-0x50]` 등, `RBP` = 프레임 + `0x80`). 쓴 값이 첫 유저 레지스터라 0 이다. 같은 입구가 같은 때 쓴 `Rcx` (`+0x1c`, 맡긴 문맥 주소 `0x9b2fcffa40`) · `Rdx` (`+0x20`, ntdll 기준 주소 `0x7ffafb660000`) · `ExceptionActive` (`+0x14`, 1) 는 P1 사진에서 0 이 아니다 (`ms/logs/04_baggage-read_P1.txt:1061 ~ 1069`, `==== P1 03 KTRAP_FRAME`). 그래서 다섯 칸의 "P1 에서도 0" 은 "안 써서 0" 이 아니라 **"0 을 써서 0"**, 표의 "P1a 부터 유저 값" 은 "0 이 아닌 첫 값" 이다. → run112 문서에 "(run113 보탬)" 한 줄. 짐 지도 2.1 아래 run112 줄 · 3.15 는 셈 (덮임 25) 이 그대로라 2.1 줄에 한 마디만 보탰다.
- **짐 지도 3.5 `DdagNode` 줄** "(항목 0x138 + 노드 0x50 한 쌍)" → "따로 두 번 할당 (둘 다 `HEAP_ZERO_MEMORY`), 한 덩어리 아님" (결과서 정정 5). 1장 그림의 "모듈마다 한 쌍 (… 같이 만듦)" 도 같이 고쳤다. flow.md 2.6 `LdrpAllocateModuleEntry` 절은 이미 두 번 할당 (`+0x2e` · `+0x60`) 으로 적혀 있어 run113 줄만 보탰다.
- **요청서 이름 둘**: `LdrpCreateNodeForModule` · `LdrpBuildDependencyGraph` 는 이 빌드에 없다 (`_index.tsv` · `pdb_aliases.py --where` 0개 — 결과서). 노드는 `LdrpAllocateModuleEntry` 가 항목과 같이 만든다. 요청서는 고치지 않는다 (중간 산출물).
- **요청서 짐작 둘**: `PreorderNumber` 는 그래프 걷기 (`LdrpInitializeGraphRecurse`) 가 아니라 묶기 (`LdrpCondenseGraphRecurse +0x1e`) — flow.md 2.6 run24 절은 처음부터 맞게 적혀 있었다. `IFEOKey` · `ReadImageFileExecOptions` 는 "조건부 안 탐" 이 아니라 쓰는 줄 없음.

## 이전 run 에서 닫은 것

- run66 "아직 확인 필요" 정적 ③ (`SbUpdateSwitchContextBasedOnDll` 이 `SwitchBackContext` 를 쓰나) → 닫힘 (`+0xd9`).
- run86 "`GdiDCAttributeList` · `GdiHandleBuffer` 는 이번 줄기에서 안 나옴" → win32kbase `GdiProcessCallout +0xe9` · `+0xfe` (범위 밖 다른 모듈로 셈). run85 · run86 이 "범위 밖으로 셀지는 결정" 으로 남긴 PEB user32 · GDI 칸 — 이번 셈에 넣었다 (run54 `_TEB` 다른 DLL 과 같은 꼴).
- run110 · run111 의 `NtGlobalFlag2` 는 그대로 (이번 셈에서 뺌).
- run82 의 VEH · VCH "초기화 동안 아무도 안 씀" → 이번 가름 "범위 밖 (ntdll)" (쓰는 함수가 지도 밖에 있음 — 뜻은 같음).
- run19 · run85 "`PostProcessInitRoutine` 넣는 쪽" → 범위 밖 (다른 DLL — user32 가 0 을 넣음). 0 이 아닌 값을 넣는 쪽은 그대로 모름.

## 아직 확인 필요

### 동적 (결과서 "동적으로 확인할 것" D1 ~ D5 — 결과서 판단 "dast 요청 필요 없음 (전부 낮음)". 대기열 번호는 오케스트레이터가 붙임, 멈춤점은 test.exe 조건)

- ~~**D5** `LdrpHeap` 이 프로세스 힙과 같은가~~ → **(사진으로 닫힘)** P1a `LdrpHeap` = `0x25996f30000` = PEB `ProcessHeap` (`ms/logs/04_baggage-read_P1a.txt:5120` · `:1739`) `[관찰]` — 위 1절 아래 줄. 새 dast 필요 없음.
- D1 (낮음) `bp ntdll!LdrpMapDllSearchPath+0x131` 에서 `db @rsp+0x50 L1` — 모듈마다 0 인가 (`PackagedBinary` 안 켜짐의 직접 근거). 사진 6모듈 비트 0 이 간접 증거.
- D2 (낮음) `LdrpInitializeProcess +0x19dc` 에서 `r r13; dd @r13+0x210 L1` — shim 길 입구가 0 인가 (지역 값, 사진 불가).
- D3 (낮음) `bp ntdll!RtlpWakeByAddress` 가 `NtContinue` 전에 한 번이라도 서나.
- D4 (낮음) `bp nt!EtwpUpdateProcessTracingCallback` 가 은퇴 전에 서나 (사진 `TracingFlags 0` ×8 이 간접 증거).

### 정적 (결과서 "다음 요청 후보")

- ① **nt 쪽 PEB 쓰기 다시 훑기** (분기 합침이 있는 추적) — 쓰는 줄 못 짚은 5칸 `[공백]`: `ProcessImagesHotPatched` · `CloudFileFlags` · `CloudFileDiagFlags` (범위 밖 nt 로 셈) · `AppModelFeatureState` · `pImageHeaderHash` (아무도 안 씀으로 셈). cldflt.sys 는 안 봄.
- ② PEB `AppCompatFlags` (`+0x2c8`) 를 쓰는 DLL (apphelp `SE_*` · aclayers 후보) — 지금 `[추측]`. `AtlThunkSListPtr` · `ProcessUsingFTH` · `WerRegistrationData` 의 쓰는 DLL 이름도 `[추측]`.
- ③ **Q78e2 에 얹을 전역** (결과서): `LdrpAuditIntegrityContinuity` · `LdrpEnforceIntegrityContinuity` (짐 지도 7.5 — 읽는 줄 `LdrpMapDllNtFileName +0x141` 을 보탬), `LdrpModuleDatatableLock` (`LdrpFindExistingModule +0x21` · `+0x35` · `+0x6f`), `g_pShimEngineModule` (`LdrpInitShimEngine +0xf5` · `LdrpGetProcApphelpCheckModule +0x27e`, 조건부 안 탐). (`LdrpHeap` 은 이미 3.8 아는 칸 — D5 는 사진으로 닫힘.)
- ④ (낮음) 로드 문맥 `+0x20` 비트 2 · 27 의 뜻 (`Redirected` · `ChpeEmulatorImage` 를 켜는 갈래 — 지금 `[추측: 비트 뜻]`).
- ⑤ (낮음) `SbpDetermineDllContext` 와 호환 문맥 표 (ntdll rva `0x173010`, `0x20`B 칸) — `SwitchBackContext` 값을 정하는 본문.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 결과서도 flow-writer 도 안 읽음, 고칠지는 사용자 결정)

- `CrossProcessFlags`: **확인 필요**. 맞추려면 `_LdrpInitialize` 흉내가 비트 1 을 켜고 끝에 끈다 (사진 `2 → 0`).
- `LoadTime`: KUSD `SystemTime` 을 읽어 쓰면 맞음, 상수면 사진과 다름 (시간이라 어차피 다름).
- `PreorderNumber`: "그래프 걷기에서 번호" 로 만들었으면 **틀림** — 묶기 함수 몫 (3.6 `LowestLink` 와 같은 함수 · 같은 값).
- `SwitchBackContext` · `PatchLoaderData`: **확인 필요**. 0 으로 두면 사진과 다르지만, 읽는 쪽 (호환 표 · `LdrLoadDll` 포인터 블록) 이 있을 때만 영향.
- `_PEB_LDR_DATA` `SsHandle` · `EntryInProgress`: 0 으로 두면 맞음 (확인 필요).
- 모듈 항목 · 노드 할당: **`entry + 0x138` 로 노드를 읽는 코드는 만들지 않는다** (늘 `entry + 0x98`).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntdll pdb 줄 run113 · 근거 로그 run113 줄), 2.1 `MmCreatePeb` (표 `+0x7c0` 줄) · `PspInsertProcess` (조건부 PEB 두 칸), 2.4 `_LdrpInitialize` (`CrossProcessFlags`) · `LdrpInitializeProcess` (exe `Flags` 되쓰기 · `AppCompatInfo` · `PatchLoaderData` · `PostProcessInitRoutine` 쓰는 쪽) · `LdrpInitializeNtdllDataTableEntry` (`LoadTime` · `ActivePatchImageBase`), 2.1 · 2.4 · 2.5 · 2.6 · 2.7 머리 run 목록, 2.6 `LdrpAllocateModuleEntry` (두 번 할당 · `NodeModuleLink` · `Lock` · 비트 26 · 28 — run22 `[추측]` "0 으로 채움" 이 `[정적]`) · 올린 뒤 알림 절 (`SwitchBackContext`) · `LdrpCondenseGraph` 절 (오프셋), 4장 run66 정적 ③ `(해결됨)` · 2.6 이름 못 정한 것 줄 (`| 8` · 문맥 비트 일부) + 새 줄 넷 (해결됨 · 동적 · 정적 · PE_emulator), 5장 두 표. 그림은 안 고침.
- 짐 지도 (`baggage.md`): 기준 · 1장 (PEB · `PebLdr` · 항목 · 노드 줄, "한 쌍" 정정) · 2장 표 넷 줄 + 합 · 2.1 아래 모름 줄 · 3장 줄임 `②C…` · 4장 짝 새 1줄 (PEB `BeingDebugged` · `ProcessInJob` ↔ `DebugPort` · job) · 5장 ① · ② · **3.1 새 줄 13 · 3.4 새 줄 1 · 3.5 새 줄 16 + 고친 줄 2 (`Flags` 첫 줄 · `DdagNode`) · 3.6 새 줄 4 + 고친 줄 1 (`LowestLink` 오프셋)** · 3장 머리 run113 줄 · 7장 머리 · 7.1 · 7.2 `(해결됨 → run113)` · 7.5 두 줄 · 새 7.44 · 8장.

## 디버거 메모 / 도구 메모

- 결과서 제안 (하네스, 직접 안 고침): ① `scripts/field_writers.py <바이너리> --struct …` — 오프셋을 더하며 따라가는 레지스터 추적 · 인자로 구조체를 받는 함수에 씨앗 · `.asm` 없으면 export 후보 · 겹친 비트 칸 쓰기 마스크 (sast/43 · 44 · 46 · 47 · 48 제안과 이어짐) ② 커널 쪽 추적에 분기 합침 (첫 판 버그 하나로 `ProcessInJob` 을 놓침) ③ `pdb_struct.py` 가 `FlagGroup` 같은 `?[4B]` 겹침 칸을 같은 오프셋 정수 칸과 한 줄로 보여 주는 옵션.
- 사진 읽기 메모 (windbg-kd-runbook pitfalls 후보): **`int3` 로 멈춘 사진의 트랩 프레임에서 "0" 은 두 가지다** — 입구가 0 을 썼거나 (첫 명령 때 유저 레지스터 `Rax` · `R8 ~ R11` 이 0) 아예 안 썼거나. 같은 입구가 쓴 이웃 칸 (`Rcx` · `Rdx` · `ExceptionActive`) 을 같이 보면 가린다 (위 정정).

# 2026-10-30 run53 — 스레드 객체 나머지와 트랩 프레임을 만들 때 (Q78d2): `_ETHREAD`·`_KTRAP_FRAME` 모르는 칸 169 → **여전히 모름 0**. `_ETHREAD` 칸은 거의 `PspAllocateThread` 한 함수, 트랩 프레임은 `KiInitializeContextThread` 한 함수가 만들고, 끝까지 0 139칸은 0 을 씀 7 · 조건부 안 탐 23 · 아무도 안 씀 34 · 범위 밖 길 75 `[추측]` 으로 갈림. **`ContextFlags` 를 줄여 넘겨서 FP·디버그 레지스터 칸은 이 길에서 늘 안 탄다** (정적)

> **정정 (run79, sast/75)**: 아래 "범위 밖 길에서 씀" 표 `ReaperLink` 줄의 "reaper 줄에 매다는 쪽 (PspExitThread)" 은 틀렸다. 매다는 쪽은 **`KeTerminateThread`** 다 (`+0x1d3 LEA R8,[RBX+0x4e8]` → `+0x1e8` → `+0x1ee CMPXCHG.LOCK [PsReaperListHead]`) `[정적]`. `PspExitThread` 는 같은 칸을 `+0x225` 에서 읽기만 한다 (`TerminationPort` 목록). 자세한 줄은 `runs/03_sprint/run79-thread-exit.md` "정정".

## 요약 표 (모르는 칸 169 → ?)

- 결과서 맨 앞 표 그대로. 분류 뜻은 run50·run52 와 같다.
- **셈 방식 (오케스트레이터 결정)**: 결과서 주 표를 따른다. 몸통 `memset` 만으로는 "0 을 씀" 으로 세지 않고, **명시 0 저장·입력값 쓰기·같은 DWORD 되쓰기만** "0 을 씀" 이다 (run50·run52 와 같음). 요청서 "주의" 문구 ("`memset` 이 덮으면 0 을 씀") 는 이전 바퀴들과 어긋났던 것이라 따르지 않았다.
  - 그 문구대로 셌을 때의 대안 숫자 (결과서): 끝까지 0 139 → 0 을 씀 139, 아는 칸에 들어가는 수 `30 + 139 = 169`.
  - 트랩 프레임 "아무도 안 씀" 12 는 **0 이 `memset` 에서 옴** (`KiInitializeContextThread +0x187` 이 0 을 보장하는 유일한 줄. 스택이 새것이라 앞서 0 이었는지 정적으로 모른다).

| | 칸 | 아는 칸 (nt) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (만들기 길 안) | 범위 밖 길에서 씀 `[추측]` | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `_ETHREAD` `K1~K2` | 27 | **27** | — | — | — | — | 0 |
| `_ETHREAD` `끝까지 0` | 107 | — | **6** | **10** | **22** | **69** | 0 |
| `_KTRAP_FRAME` `K1~K2` | 3 | **3** | — | — | — | — | 0 |
| `_KTRAP_FRAME` `끝까지 0` | 32 | — | **1** | **13** | **12** | **6** | 0 |
| **합** | **169** | **30** | **7** | **23** | **34** | **75** | **0** |

- **"범위 밖 길" 75 와 "아무도 안 씀" 34 는 커널 전체를 칸 오프셋 숫자로 훑은 결과가 근거다** (`objdump -d`, `.asm` 없음). 한계:
  - 오프셋 숫자만 같으면 걸린다 → 오탐이 있다. `_ETHREAD` 오프셋은 EPROCESS·Job·ALPC 와 많이 겹친다 (예: `0x4e8`·`0x5b8`·`0x6e8`).
  - `LEA` 로 칸 주소를 만든 뒤 쓰는 줄·이동 명령은 안 잡힌다 (`AlpcWaitListEntry`·`CmCallbackListHead` 가 후보 `[추측]`).
  - 이번엔 먼 조각 (`PspAllocateThread +0x2ca9a9` 같은 곳) 을 주인 함수에 붙여 넓혔지만, 주인이 불확실한 `FUN_…` 조각은 남는다.
  - 트랩 프레임은 기준 레지스터가 `rbp = 프레임 + 0x80` 이라 보정해서 훑었다. 오탐이 많아 이름이 칸 뜻과 맞는 것만 "범위 밖" 에 넣었다.
  - 그러니 두 묶음 사이 경계가 약하다. 둘 다 "초기화 동안 쓰는 줄이 없다" 는 점만 `[정적]`.

짐 지도 (`baggage.md` 2장) 로 옮길 때 — run52 와 같은 규칙:

- "아는 칸" 30 + "0 을 씀" 7 + "조건부 안 탐" 23 = **60** → "nt 가 씀" (줄이 있는 칸)
- "아무도 안 씀" **34** → 2장 표 아래 줄
- "범위 밖 길에서만 씀 `[추측]`" **75** → 모름에서 빼고 따로 한 줄
- 그리고 이미 아는 칸 **`SegCs`·`SegSs` 가 짐작 → 정확** (아래 "정정")

| | nt 가 씀 | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|
| `_ETHREAD` (149칸) | 5 → **48** | 0 → **22** | 0 → **69** | 144 → **10** | 5 + 0 → **48 + 0** |
| `_KTRAP_FRAME` (66칸) | 6 → **23** | 0 → **12** | 0 → **6** | 60 → **25** | 4 + 2 → **23 + 0** |

- 새 아는 칸 60 은 모두 정확. 옛 짐작 2 → 정확. 그래서 정확 +62 · 짐작 −2.
- 남은 모름 35 = `_ETHREAD` 10 (`K2~K3` 5 · `K3~P0` 4 · `P1~P1a` 1) + `_KTRAP_FRAME` 25 (`P1~P1a` 24 · `P1a~P1b` 1) → **Q78d4**.
- 짐 지도 전체: 구조체 21개 직접 칸 1,456 = 아는 칸 596 → **656** (정확 539 → **601** · 짐작 57 → **55**) + 아무도 안 씀 98 → **132** + 범위 밖 길에서만 씀 123 → **198** + 모름 639 → **470**.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 03_sprint 열한째 바퀴 (짐 지도 바퀴, plan **Q78d2** — Q78d 첫 스레드의 둘째 조각).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음, 결과서 **PDB 일치**). 이미 analyze 된 프로젝트에서 뽑기만 했다.
- 버전 대조 (결과서): `_calls.tsv` 호출 지점 (아래 표) 과 **사진 K2 값이 칸마다 맞는다**:
  - 자기 연결 리스트 머리 8개 = 스레드 주소 + 칸 오프셋 (`KeyedWaitChain` `…5548` = `…5080 + 0x4c8`).
  - `EnergyValues` = 스레드 + `0x798`, `ProcessFastRef` = 프로세스 + `0xf` (`…b08f`), `ReadClusterSize` `0xf`, `Silo` −3, `MmSlabIdentity` `0xff`, 세마포어 덩어리 4/4.
  - 트랩 프레임 `ExceptionActive` 1 · `MxCsr` `0x1f80` · `EFlags` `0x200`.
  - 끝까지 0 139칸은 K2 ~ P2 일곱 장에서 모두 0 (K1 은 옛 객체·옛 스택이라 뺌, run52 와 같음).
- 오케스트레이터가 `.asm`·PDB 로 확인한 것: `KxContextToKframes +0x5f` (`EFlags` +0x178) · `+0x91` (`SegSs`) · `+0xa7` (`SegCs`), `KiInitializeContextThread +0x157 AND EAX,6` → `+0x15f`, `PspAllocateThread +0x3c7 memset` → `+0x3cc MOV [RSI+0x588],RDI` (`RundownProtect`), `ObCreateObjectEx` call `+0x2e2` / ret `+0x2e7`.
- 이 문서를 쓰며 다시 맞춘 것 (flow-writer, `.asm`): `PspAllocateThread +0x3da`·`+0x410`·`+0x41a`·`+0x45c`·`+0x483`·`+0x48a`·`+0x49f`·`+0x4d2`·`+0x4e7`·`+0x31a`·`+0xb04`·`+0xaea`·`+0xaf8`·`+0xbb2` 와 리스트 머리 줄, `KiInitializeContextThread +0x291`·`+0x40c`, `KxContextToKframes +0xdd`·`+0xfe ~ +0x11f`·`+0x12a`·`+0x240`·`+0x259`·`+0x357`·`+0x395`, `PspInsertThread +0x2cd`·`+0x617`·`+0x649`·`+0x6e3`.
- `.asm` 이 없는 곳 (결과서가 `objdump -d` 로만 봄): 커널 전체 쓰기 훑기, 트랩 프레임 진입 코드 (`KiSystemServiceUser`·`KiSaveDebugRegisterState`·`KiInterruptSubDispatch`·`KiServiceInternal`), "범위 밖 길" 표의 함수 대부분. 이 문서에선 결과서의 함수+오프셋 그대로 쓴다.

## 목적

- 짐 지도 Q78 의 일곱째 조각. `_ETHREAD` (baggage 3.13) 모르는 칸 144 중 134, `_KTRAP_FRAME` (3.15) 모르는 칸 60 중 35 를 닫는다.
  - `K1~K2` 30 — 칸마다 쓰는 줄과 값의 출처.
  - 끝까지 0 139 — 네 가지로 가르기.
- 갈림길과 간접 호출.
- 범위 밖: `_ETHREAD` 뒤 구간 10칸·`_KTRAP_FRAME` `P1~P1b` 25칸 (Q78d4), `_TEB` (Q78d3), 리스트에 나중에 달리는 항목 내용.

## 방법

- 정적: `_workspace/sast/47_ethread-create.md` — ntoskrnl. 새로 depth 0 **10개** (`KeInitializeSemaphore`·`PsInitializeThreadRngState`·`RtlInitializeTRng`·`PsAssignThreadId`·`KeQuerySystemTimePrecise`·`KeQuerySystemTimeUnsafe`·`PoEnergyEstimationEnabled`·`KeQueryMaximumGroupCount`·`PspMapThreadCreationFlags`·`ExBuildPasidMsrForThread`), 나머지는 이미 있어 읽기만. `_calls.tsv` 4,304 → 4,307줄 (줄이 안 사라짐).
  - 칸 이름·오프셋은 `pdb_struct.py` + sast/38 `struct__ETHREAD.tsv`·`struct__KTRAP_FRAME.tsv`.
  - 값 대조: run44 덤프 읽기 로그 (K2 ~ P2 일곱 장). 새 동적 실행은 없다.
- 결과서: `_workspace/sast/47_ethread-create_result.md` (**분석성공**, PDB 일치)
- 문서: 이 run 문서, `baggage.md` (머리, 1장, 2장·2.1, 3장 줄임·3.3 머리·3.13·3.15 다시 씀, 5장 ①, 6장, 7장 머리·7.1·7.2·7.4·7.6·7.7·7.8, 새 7.16, 8장), `flow.md` (기준·2.1 트리와 `PspAllocateThread`·`KeInitThread` 절·4장·5장). 그림 (mermaid) 은 안 바꿨다.
- **줄임 주의**: 결과서의 `①C`·`①KC` 는 짐 지도의 `①KC…` (`KiInitializeContextThread`)·**`①KX…`** (`KxContextToKframes`, 새 줄임) 다. 짐 지도 `①C…` 는 run50 의 `MmCreateProcessAddressSpace` 라 그대로 옮기면 겹친다. 이 문서는 함수 이름이나 짐 지도 줄임으로 적는다.

## 결과

비유 (새 직원 책상 차리기, 이어서):

- `_ETHREAD` = 새 직원의 **인사 카드 뒷면**. 본사 (`PspAllocateThread`) 가 지우개로 밀고 나서, 빈 서류함 (리스트 머리) 여덟 개와 입사 시각, 기본값 몇 개를 적는다.
- 트랩 프레임 = **첫 출근 때 쓸 출발 메모**. 비서 (`KiInitializeContextThread`) 가 메모를 지우고, 출근 위치·레지스터만 옮겨 적는다.
- 비서는 양식에서 수학 도구·디버그 칸의 "옮겨 적을 칸" 체크를 **미리 지워서** 넘긴다. 그래서 그 칸들은 늘 빈칸이다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0xe57` | `+0xe5c` | `nt!PspAllocateThread` | [정적] |
| `nt!PspAllocateThread` | `+0x2e2` | `+0x2e7` | `ObCreateObjectEx` (스레드 객체) | [정적] |
| `nt!PspAllocateThread` | `+0x307` | `+0x30c` | `memset` (몸통 + 확장 영역 전부) | [정적] |
| `nt!PspAllocateThread` | `+0x42d` | `+0x432` | `KeInitializeSemaphore` (세마포어 칸) | [정적] |
| `nt!PspAllocateThread` | `+0x4bf` | `+0x4c4` | `KeQuerySystemTimePrecise` (`CreateTime`) | [정적] |
| `nt!PspAllocateThread` | `+0x6f4` | `+0x6f9` | `nt!KeInitThread` | [정적] |
| `nt!PspAllocateThread` | `+0x70b` | `+0x710` | `PsInitializeThreadRngState` (→ `RtlInitializeTRng`, 호출 `+0x70`) | [정적] |
| `nt!PspAllocateThread` | `+0x71a` | `+0x71f` | `ObInitializeFastReference` (`ProcessFastRef`) | [정적] |
| `nt!KeInitThread` | `+0x2fc` | `+0x301` | `nt!KiInitializeContextThread` | [정적] |
| `nt!KeInitThread` | `+0x368` | `+0x36d` | `KeInitializeApc` (`UpdateTebApc`, `KiUpdateTebApc` rva `0x312820`) | [정적] |
| `nt!KiInitializeContextThread` | `+0x187` | `+0x18c` | `memset` (트랩 프레임, `0x190`B) | [정적] |
| `nt!KiInitializeContextThread` | `+0x1ac` | `+0x1b1` | `KxContextToKframes` | [정적] |
| `nt!KiInitializeContextThread` | `+0x40c` | `+0x411` | `ExBuildPasidMsrForThread` (조건부, 안 탐) | [정적] |

### 호출 관계 (대표 지점)

```
NtCreateUserProcess
 └─ +0xe5c  PspAllocateThread                                                                 [정적]
         │  +0x2e7 ObCreateObjectEx → +0x30c memset
         │  +0x31a EnergyValues · +0x3cc RundownProtect · +0x410 ReadClusterSize · +0x41a MmSlabIdentity
         ├─ +0x432  KeInitializeSemaphore      KeyedWaitSemaphore·AlpcWaitSemaphore
         │  +0x439 ~ +0x4e7 리스트 머리 여덟·락 칸 다섯·Silo
         ├─ +0x4c4  KeQuerySystemTimePrecise   CreateTime
         ├─ +0x6f9  KeInitThread
         │       ├─ +0x301  KiInitializeContextThread
         │       │       │  +0x157 ~ +0x15f ContextFlags 줄이기 · +0x166/+0x16e 로컬 SegCs·SegSs
         │       │       │  +0x18c memset(트랩 프레임) → +0x198 ExceptionActive
         │       │       ├─ +0x1b1  KxContextToKframes  EFlags·Rip·Rsp·SegSs·SegCs·Rax·Rcx·Rdx·R8~R11·Rbp
         │       │       │  +0x1f1 MxCsr
         │       └─ +0x36d  KeInitializeApc   UpdateTebApc
         ├─ +0x710  PsInitializeThreadRngState → RtlInitializeTRng   RngState·UsedByRngState
         └─ +0x71f  ObInitializeFastReference  ProcessFastRef
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 쓰는 줄.
- 코드 순서는 오프셋 순이다. 단 오프셋 순서가 늘 실행 순서는 아니다.

### 몸통 0 채움 — 두 군데

- `_ETHREAD`: `PspAllocateThread +0x307 memset(스레드, 0, [RSP+0x98])`. 크기는 `ObCreateObjectEx` 에 넘긴 객체 크기 그대로 (`+0x2ab` 가 계산). 사용자 스레드 = `0x880 + 0x220 + 0x2d0 = 0xd70`, 시스템 스레드 = `0xaa0`. **몸통 `0x798` + 확장 영역 전부** `[정적]`.
- 트랩 프레임: `KiInitializeContextThread +0x187 memset(R15 − 0x190, 0, 0x190)`. K1 사진의 같은 주소는 옛 내용 (`ExceptionActive` `0xb3`, `Dr3` `0x200001` …), K2 에서 한꺼번에 0.

### `K1~K2` 30칸 (질문 1)

- 칸마다 쓰는 줄·값·경로는 **짐 지도 3.13·3.15** (결과서 1.1·1.2 표를 옮김). 묶어 보면:
  - `PspAllocateThread` 직접 — 자기 연결 리스트 머리 8 (`KeyedWaitChain`·`ActiveTimerListHead`·`IrpList`·`BoostList`·`DeboostList`·`PropertySet`·`OwnerEntryListHead`·`DisownedOwnerEntryListHead`), 상수 3 (`ReadClusterSize` `0xf` `+0x410` · `Silo` −3 `+0x49f` · `MmSlabIdentity` `0xff` `+0x41a`), `EnergyValues` (`+0x31a`).
  - 도우미 — `KeQuerySystemTimePrecise` (`CreateTime`), `KeInitializeSemaphore` (세마포어 두 칸), `KeInitializeApc` (`UpdateTebApc` 묶음 6칸), `RtlInitializeTRng` (`RngState`·`UsedByRngState`), `ObInitializeFastReference` (`ProcessFastRef`).
  - 겹쳐서 같이 닫힘 — `ExitTime` (= `KeyedWaitChain.Flink`), `PostBlockList` (뒤 8B = `StartAddress`), `Tcb` (안쪽 칸 3.14 로 셈).
  - 트랩 프레임 — `ExceptionActive` 1 (`KiInitializeContextThread +0x198`), `MxCsr` `0x1f80` (`+0x1f1`, CONTEXT 값이 아니라 상수), `EFlags` (`KxContextToKframes +0x5f`, `(CONTEXT.EFlags & 0x210dd5) | 0x200` → `0x200`).
- **`UpdateTebApc` 는 run52 의 둘째 `KeInitializeApc` 와 같은 호출이다** (`KeInitThread +0x339 LEA RCX,[RDI+0x720]` → `+0x368`). `KernelRoutine` 은 하는 일 없는 빈 함수 (rva `0x4a10c0`, 같은 주소 이름 100개, 후보 `KiSchedulerApcNop`·`KiSchedulerApcRundown`), `NormalRoutine` = `KiUpdateTebApc`.
- `*` 칸 8개 (`Tcb`·`UpdateTebApc`·`Fill3~5`·`RngState`·`UsedByRngState`·`EFlags`) 는 **처음 값 줄만** 닫았다. 뒤에 바꾸는 쪽은 이름만: `KiUpdateSoftParkElectionStatisticsOnInsertion` `[추측]`, 풀 할당 쪽 난수 소비 `[추측]`, `PspSetContext +0x5a`·시스템 호출 진입 `[추측]`.
- 칸 수 대조 (결과서): 7.2 목록 이름을 하나씩 짝지어 빠진 이름 0 · 더 들어간 이름 0. flow-writer 도 7.2 `끝까지 0` 107 이름과 결과서 2.1 ~ 2.4 를 스크립트로 짝지었다 (빠짐 0).

### 끝까지 0 — `_ETHREAD` 107칸 (질문 2)

- **0 을 씀 6** — 값 0 인 `RDI` 를 명시 저장: `ActiveTimerListLock` `+0x48a`, `RundownProtect` `+0x3cc`, `BoostListLock` `+0x45c`, `IrpListLock` `+0x483`, `DisownedOwnerEntryListLock` `+0x4d2`, `Win32kThreadLock` `+0x4e7`. 비트 묶음 DWORD 를 되쓰는 줄은 만들기 길 안에 없다.
- **조건부 안 탐 10**:

| 칸 | 켜는 줄 | test.exe 가 왜 안 탐 |
|---|---|---|
| `CrossThreadFlags.HideFromDebugger` | `PspAllocateThread +0xb04` | 만들기 플래그 비트 2 (`+0x3eb`) 꺼짐 — 입력에 숨김 비트 안 줌 `[추측]`, 사진 비트 = 0 `[관찰 간접]` |
| `CrossThreadFlags.IndirectCpuSets` · `SelectedCpuSets` · `SelectedCpuSetsIndirect` | `+0xaea` · `+0xaf8` | 프로세서 그룹 1개 (`KeQueryMaximumGroupCount`, `+0x23c`) `[관찰 간접]`, 전역 값은 D1 |
| `SameThreadPassiveFlags.ClonedThread` · `UserFsBase` · `UserGsBase` · `SetContextState` | `+0x2caa99` · `+0x2ca9d7` · `+0x2ca9e2` · `+0x2caa31` | 사용자 스택 인자 ≠ 0 → 복제·피코 갈래 (`+0x5b3 → +0x2ca9a9`) 안 탐 `[관찰 간접]` |
| `SameThreadPassiveFlags.SecureContext` | `+0xbb2` | `ThreadContext ≠ 0` → 시스템 스레드 갈래 안 탐 |
| `SameThreadApcFlags.PasidMsrValid` | `ExBuildPasidMsrForThread +0x1c` (호출 `KiInitializeContextThread +0x40c`) | `NpxState` `0xe5` 에 비트 10 없음. `DeviceAsid` = 0. 전역은 D1 |

- **아무도 안 씀 22**: `ForwardLinkShadow`, `MmLockOrdering`, `CrossThreadFlags.RundownFail`, `.UmsForceQueueTermination`, `.ReservedCrossThreadFlags`, `SameThreadPassiveFlags.ActivePageLists`, `.WorkloadClass`, `.GenerateDumpOnBadHandleAccess`, `.ReservedSameThreadPassiveFlags`, `SameThreadApcFlags.StartAddressInvalid`, `.SuppressSymbolLoad`, `AllowUserWritesToExecutableMemory`, `AllowKernelWritesToExecutableMemory`, `DisablePageFaultClustering`, `LockOrderState`, `MmReserved`, `AlpcWaitListEntry`, `CmCallbackListHead`, `VerifierContext`, `CmThreadInfo`, `UpdateTebSpareByte2`, `MmFaultClusterContext`. 값 0 은 `+0x307 memset`.
- **범위 밖 길에서 씀 `[추측]` 69** (전체 훑기, 오탐 가능. `PspInsertThread` 줄만 `.asm` 있음, Q78d4):

| 칸 | 쓰는 곳 (범위 밖) |
|---|---|
| `TerminationPort` | NtRegisterThreadTerminatePort +0x71 (같은 8B 무리) |
| `ReaperLink` | 같은 8B (0x4e8) 무리 — 이름 후보는 위와 같음. reaper 줄에 매다는 쪽 (PspExitThread) 은 훑기에 안 잡힘 |
| `KeyedWaitValue` | NtWaitForKeyedEvent +0x108·+0x1ef, NtReleaseKeyedEvent +0x170 |
| `ClientSecurity` | PsImpersonateClient +0x14c, PsRestoreImpersonation +0x8f, NtOpenThreadTokenEx |
| `TopLevelIrp` | IoSetTopLevelIrp +0x9 |
| `DeviceToVerify` | IoSetDeviceToVerify +0x0, IoSetHardErrorOrVerifyDevice +0xc |
| `ChargeOnlySession` | PspExitThread +0x790 (정리 쪽, 오탐 가능) |
| `LegacyPowerObject` | PspExitThread +0x718 (정리 쪽, 오탐 가능) |
| `CrossThreadFlags.Terminated` | PspTerminateThreadByPointer, PspInsertThread 실패 경로 `+0x96f` (BTS.LOCK, `.asm` 있음, Q78d4) |
| `CrossThreadFlags.ActiveImpersonationInfo` | PsImpersonateClient, PsRestoreImpersonation, PsDisableImpersonation, NtOpenThreadTokenEx |
| `CrossThreadFlags.HardErrorsAreDisabled` | PsSetThreadHardErrorsAreDisabled, IoSetThreadHardErrorMode |
| `CrossThreadFlags.BreakOnTermination` | NtSetInformationThread |
| `CrossThreadFlags.SkipCreationMsg` | DbgkpSetProcessDebugObject (떨어진 조각 `FUN_140b2dea5`, `lock or [r13+0x5a0], 0x40`) |
| `CrossThreadFlags.SkipTerminationMsg` | DbgkpSetProcessDebugObject |
| `CrossThreadFlags.CopyTokenOnOpen` | NtOpenThreadTokenEx, PsImpersonateClient, PsRestoreImpersonation |
| `CrossThreadFlags.DisableDynamicCodeOptOut` | NtSetInformationThread |
| `CrossThreadFlags.ExplicitCaseSensitivity` | NtSetInformationThread |
| `CrossThreadFlags.PicoNotifyExit` | PspCreatePicoThread |
| `CrossThreadFlags.DbgWerUserReportActive` | DbgkQueueUserExceptionReport, DbgkUserReportWorkRoutine, NtSetInformationThread |
| `CrossThreadFlags.ForcedSelfTrimActive` | MiSelfTrim, MiUnlockWorkingSetShared |
| `CrossThreadFlags.SamplingCoverage` | EtwpCovSampCaptureApc, EtwpCovSampCaptureSample |
| `CrossThreadFlags.ImpersonationSchedulingGroup` | IopProcessWorkItem, PspUpdateContainerImpersonation |
| `SameThreadPassiveFlags` | DWORD 통째: ExpWorkerThread, MmStoreProbeAndLockPages, MiBackgroundZeroLocalPages, CcWorkerThread, SmStWorker 등 (비트별 아래) |
| `SameThreadPassiveFlags.ActiveExWorker` | ExpWorkerThread |
| `SameThreadPassiveFlags.MemoryMaker` | CcWorkerThread, CcAsyncLazywriteWorker, MiDereferenceSegmentThread, SmKmStoreHelperWorker |
| `SameThreadPassiveFlags.StoreLockThread` | MmStoreProbeAndLockPages |
| `SameThreadPassiveFlags.KeyedEventInUse` | NtWaitForKeyedEvent, NtReleaseKeyedEvent |
| `SameThreadPassiveFlags.SelfTerminate` | NtTerminateProcess +0x1f5, PspInsertThread `+0x9d6` (`.asm` 있음, Q78d4) |
| `SameThreadPassiveFlags.RespectIoPriority` | CmpSetRespectIoPriorityThread +0x21 |
| `SameThreadPassiveFlags.ZeroPageThread` | MiBackgroundZeroLocalPages |
| `SameThreadPassiveFlags.BalanceSetManager` | MiWorkingSetManager, MiPartitionWorkingSetManager |
| `SameThreadApcFlags` | DWORD 통째: LOCK_ADDRESS_SPACE, UNLOCK_ADDRESS_SPACE_*, MiLockVad, MiUnlockVad, PsSetSystemPagePriorityThread 등 (비트별 아래) |
| `SameThreadApcFlags.OwnsProcessAddressSpaceExclusive` | LOCK_ADDRESS_SPACE, UNLOCK_ADDRESS_SPACE_UNORDERED, MiLockAddressSpaceToo |
| `SameThreadApcFlags.OwnsProcessAddressSpaceShared` | LOCK_ADDRESS_SPACE_SHARED, UNLOCK_ADDRESS_SPACE_SHARED |
| `SameThreadApcFlags.HardFaultBehavior` | SmStWorker |
| `SameThreadApcFlags.EtwCalloutActive` | EtwTracePageFault +0x98·+0xde |
| `SameThreadApcFlags.Prefetching` | PsSetCurrentThreadPrefetching +0x2f |
| `SameThreadApcFlags.OwnsVadExclusive` | MiLockVad, MiUnlockVad, MiObtainReferencedVadEx |
| `SystemPagePriorityActive` | PsSetSystemPagePriorityThread +0x2f, PsRevertToUserPagePriorityThread +0x20 |
| `SystemPagePriority` | 같은 두 함수 |
| `OwnsVadShared` | MiLockVadShared, MiUnlockVadShared |
| `SlabReplenishInProgress` | MiCreateSlabEntry +0x1e5·+0x5ad |
| `CacheManagerActive` | MmCheckCachedPageStates, MmFlushSection, MiPfExecuteReadList |
| `ActiveFaultCount` | MiPrefetchVirtualMemory, MiIssueHardFault, MmPrefetchPagesEx, MiPrefetchControlArea (inc/dec) |
| `SharedPsModuleLockAcquires` | MiAcquireLoadLock +0x3b, MiReleaseLoadLock +0x15 |
| `AlpcMessageId` | AlpcpDispatchNewMessage, AlpcpDispatchReplyToWaitingThread, AlpcpDispatchReplyToPort, AlpcpCancelMessage, AlpcpReceiveSynchronousReply |
| `AlpcMessage` | AlpcpReceiveMessagePort, AlpcpSendLegacySynchronousRequest |
| `AlpcReceiveAttributeSet` | 같은 칸 앞 4B — 위와 같은 함수 |
| `ExitStatus` | PspExitThread +0x100, PspTerminateThreadByPointer +0x6e, PspInsertThread 실패 경로 `+0x2ca1d6` (`.asm` 있음, Q78d4) |
| `CacheManagerCount` | IoPageReadEx +0x2b3 |
| `IoBoostCount` | ExpApplyPriorityBoost, PsBoostThreadIo, PsEnterPriorityRegion, PsLeavePriorityRegion |
| `IoQoSBoostCount` | ExpApplyPriorityBoost, ExpBoostIoAfterAcquire |
| `IoQoSThrottleCount` | IoUpdateThreadIoRateThrottle +0x6 |
| `ReservedForSynchTracking` | ExpAcquireResourceSharedLite, ExpAcquireSharedStarveExclusive, ExReleaseResourceLite, EtwpGetTrackingLockSlotForThread |
| `ActivityId` | IoSetActivityIdThread, IoClearActivityIdThread, IoPropagateActivityIdToThread, IopCallDriverReference |
| `SeLearningModeListHead` | SeSetLearningModeObjectInformation, SeClearLearningModeObjectInformation |
| `AdjustedClientToken` | PsImpersonateClient, PsRestoreImpersonation, PsDisableImpersonation, NtOpenThreadTokenEx, PspThreadDelete |
| `WorkOnBehalfThread` | PspUpdateContainerImpersonation, PspRevertContainerImpersonation, IopProcessWorkItem |
| `PicoContext` | PspCreatePicoThread +0x330, VmpProcessContextSetup |
| `ThreadName` | NtSetInformationThread +0x9df |
| `EtwSupport` | EtwpTraceStackWalk, EtwpExpandStackAndWalkFrameChain |
| `FlsData` | PspExitThread +0x311·+0x50e, PsTlsSetValue +0x7a |
| `LastSoftParkElectionRunTime` | KiUpdateSoftParkElectionStatisticsOnInsertion |
| `LastSoftParkElectionGeneration` | KiUpdateSoftParkElectionStatisticsOnInsertion, KiCommitThreadWait |
| `UserIsolationDomain` | KiAdjustUserIsolationDomainThread, KiIpiUpdateUserIsolationDomain |
| `Win32kPriorityFloor` | PsAdjustWin32kPriorityFloor +0x4d |
| `LastSoftParkElectionWorkloadType` | KiUpdateSoftParkElectionStatisticsOnInsertion +0x4e |
| `HeapData` | RtlpHpAllocateHeap 계열 (`gs:0x188` 에서 온 스레드, 오탐 가능) |
| `ThreadIndex` | PspQueryThreadIndexInformation +0xe5 |

- 비트 묶음의 셈: `CrossThreadFlags` 19비트 = 조건부 2 · 아무도 안 씀 3 · 범위 밖 14. `SameThreadPassiveFlags` 칸 15 = 조건부 2 · 아무도 안 씀 4 · 범위 밖 9. `SameThreadApcFlags` 영역 16 = 조건부 1 · 아무도 안 씀 4 · 범위 밖 11.

### 끝까지 0 — `_KTRAP_FRAME` 32칸 (질문 2)

- **`KiInitializeContextThread +0x157 ~ +0x15f` 가 `ContextFlags` 를 `(원래 & 6) | 0x100001` 로 줄여 넘긴다** `[정적]`. `KxContextToKframes` 안 블록:

| 블록 | 조건 | 이 호출에서 | 트랩 프레임 칸 |
|---|---|---|---|
| CONTROL | `& 0x100001` (`+0x21 ~ +0x35`) | **탐** | `EFlags`·`Rip`·`Rsp`·`SegSs`·`SegCs` (`+0x5f · +0x6d · +0x7b · +0x91 · +0xa7`) |
| INTEGER | `& 0x100002` (`+0xc9 ~ +0xd3`) | **탐** (원래 `0x10000b` 의 비트 1) | `Rax`·`Rcx`·`Rdx`·`R8~R11` (`+0xdd ~ +0x11f`), `Rbp` (`+0x12a`) |
| XSTATE | `& 0x100040` + 모드 1 (`+0x193 ~ +0x1a5`) | 안 탐 | — |
| CET | `& 0x100080` (`+0x215 ~ +0x221`) | 안 탐 | `ShadowStackFrame` 읽기 |
| FLOATING_POINT | `& 0x100008` (`+0x227 ~ +0x233`) | **안 탐** | `Xmm0~5` (`+0x240 ~ +0x283`), `MxCsr` (`+0x310`) |
| DEBUG_REGISTERS | `& 0x100010` (`+0x2f8 ~ +0x344`) | **안 탐** | `Dr0~3`·`Dr6`·`Dr7` (`+0x357 ~ +0x395`) |
| SEGMENTS (`0x4`) | 이 함수는 비트를 **검사하지 않음** | — | `SegDs ~ SegGs` 안 씀 |

| 가름 | 칸 | 근거 |
|---|---|---|
| **0 을 씀 1** | `Rbp` | `KxContextToKframes +0x12a` — 입력값 `CONTEXT.Rbp` 0 을 씀. 범위 밖에서도 씀 `[추측]`: `PspInitializeThunkContext`·`PspSetContext`·`KiCallUserMode` |
| **조건부 안 탐 13** | `Xmm0`·`Xmm2~5` (5) · `Dr0~3`·`Dr6`·`Dr7` (6) · `ShadowStackFrame`·`Spare` (2, 겹침) | 호출자가 비트를 지워 **늘 안 탐**. 범위 밖에서도 씀 `[추측]`: `KiSystemServiceUser`, `KiSaveDebugRegisterState`, 다른 호출자의 `KxContextToKframes` |
| **아무도 안 씀 12** (0 은 `memset` 에서 옴) | `P5`·`DebugControl`·`SegDs`·`SegEs`·`SegFs`·`SegGs`·`Fill0`·`Logging`·`Fill1`·`Fill2`·`Fill3`·`Fill4` | 만들기 길 쓰는 줄 없음. 훑기에도 이름 있는 쓰는 곳 없음 |
| **범위 밖 길 `[추측]` 6** | `PreviousIrql` · `LastBranchToRip`·`LastBranchFromRip`·`LastExceptionToRip`·`LastExceptionFromRip` · `TrapFrame` | `KiInterruptSubDispatch +0x20` 등 · `KiSaveDebugRegisterState +0x9e ~ +0xe3` · `KiServiceInternal +0x5b` |

### test.exe 가 탄 길 (갈림길 다섯)

| 갈림길 | 가르는 곳 | test.exe |
|---|---|---|
| 숨김 플래그 | 만들기 플래그 비트 2 (`PspAllocateThread +0x3eb`) | 꺼짐 |
| 프로세서 그룹 | `KeQueryMaximumGroupCount() > 1` (`+0x252 ~ +0x259`) | 1개 |
| 피코·복제 | 사용자 스택 인자 = 0 (`+0x5ab ~ +0x5b3`) | 인자 ≠ 0 → 일반 길 |
| 시스템 스레드 | `ThreadContext == 0` (`+0x541`) | 사용자 스레드 |
| PASID | `NpxState` 비트 10 (`KiInitializeContextThread +0x3f7 ~ +0x403`) | 없음 → `ExBuildPasidMsrForThread` 안 부름 |

- 이 값들은 사진 값으로 맞춰 본 것 `[관찰 간접]`. 입력값 자체는 D1·D2.

### 간접 호출 (질문 3)

- **만들기 길 안에 이 두 구조체 칸을 쓰는 간접 호출은 없다** `[정적]`. `.asm` 26개에서 `CALL reg`·`CALL [mem]`·`JMP reg` 0개 (목록은 결과서 3번) → Q35 와 무관.
- `KeInitThread +0x34a` (`KiUpdateTebApc`)·`+0x35b` (빈 함수 rva `0x4a10c0`) 는 함수 주소를 **값으로** 넘길 뿐, 이 길에서 부르지 않는다.

### 부모 (지금 도는) 스레드 칸 — 새 스레드 짐이 아님

- `KxContextToKframes +0x3ba` (`BTR.LOCK [RCX],0x18`, `RCX` = `gs:[0x188]`) 는 부모 스레드 칸이라 안 셌다 (run52 와 같음).

## 확인된 것

- **`K1~K2` 30칸 모두 처음 값을 쓰는 줄** `[정적]` · 사진 K2 값과 칸마다 맞음 (짐 지도 3.13·3.15)
- **몸통 0 채움 두 곳**: `PspAllocateThread +0x307`, `KiInitializeContextThread +0x187` `[정적]`
- **`ContextFlags` 줄이기**: `KiInitializeContextThread +0x157 ~ +0x15f` — FP·DEBUG·XSTATE·CET 블록은 이 호출에서 늘 안 탐 `[정적]` (오케스트레이터 `.asm` 확인)
- **`MxCsr` 는 CONTEXT 값이 아니라 상수** (`+0x1f1`) `[정적]`
- **`SegCs` = `KxContextToKframes +0xa7`, `SegSs` = `+0x91`, `EFlags` = `+0x5f`** `[정적]` (오케스트레이터 `.asm` 확인)
- **`UpdateTebApc` = `KeInitThread +0x368` `KeInitializeApc`**. `_KAPC` 안 쓰는 자리에 다른 칸이 박혀 있다 (`MmSlabIdentity`·`RngState` 등) `[정적]`
- **`PsAssignThreadId` 는 `Cid.UniqueThread` 하나만 쓴다** (`+0x18`). `Cid.UniqueProcess` 는 `PspAllocateThread +0x3da` `[정적]`
- **만들기 길 안 간접 호출 없음** `[정적]`
- 끝까지 0 139칸은 0 을 씀 7 · 조건부 안 탐 23 · 아무도 안 씀 34 · 범위 밖 길 75 `[추측]` 로 갈렸다. 여전히 모름 0

## 정정

- **baggage 3.15 `SegCs`·`SegSs` 짐작 → 정확**: 경로 칸의 `KiInitializeContextThread +0xb5` 는 `ThreadContext` 읽기 줄 (`MOV R12,[R13+0x20]`) 이라 고쳤다. 쓰는 줄은 `KxContextToKframes +0xa7`·`+0x91`, 로컬 CONTEXT 에 미리 쓰는 줄은 `KiInitializeContextThread +0x166`·`+0x16e`. 짐 지도 7.4 줄 `(해결됨 → run53)`.
- **baggage 6장 `ObCreateObjectEx`**: `PspAllocateThread` 호출 `+0x2e2`, ret `+0x2e7` 로 적음. flow.md 의 `+0x2e7` 표기 (그림·트리·목록) 는 ret 자리를 쓰는 규칙이라 그대로 뒀다.
- **요청서의 경로 줄임 겹침**: 결과서 `①C`·`①KC` ≠ 짐 지도 `①C…`·`①KC…` (위 "방법").
- run16 문장 ("`+0xb5` 가 CONTEXT 를 꺼내 … 손본 뒤 넘긴다") 은 요약으로 틀리지 않아 run16 문서는 안 고쳤다. flow.md 해당 줄에 정확한 오프셋을 보탰다. 이전 run 문서 맨 위 정정 안내: **없음**.

## 아직 확인 필요

- 동적 (결과서 번호 그대로, D1 ~ D4 — run52 의 D1 ~ D4 와 다른 질문). 급한 것은 없다.
  - **D1** (덤프, **Q79**) 커널 전역: `PopEnergyEstimationEnabled` (nt+`0xf0e670`, 기대 1), `KiMaximumGroups` (nt+`0xfc5014`, 기대 1), `KeFeatureBits` (nt+`0xfc6b70`) **비트 `0x38`** (run52 D1 의 비트 `0x37` 과 같이 읽기), KUSD `+0x3d8` `XState.EnabledFeatures`, KUSD `+0x14` `SystemTime` (`dq 0xfffff78000000014 L1`). → 전역에 걸린 `IndirectCpuSets`·`PasidMsrValid`·`EnergyValues` 의 이유가 닫힌다.
  - **D2** (멈춤, **Q100** 낮음) `nt!NtCreateUserProcess+0xe57` 에서 `dd poi(@rsp+0x40) L1` (만들기 플래그, 기대 `0x70`), `dq @rsp+0x50 L1` (사용자 스택 인자, 기대 ≠ 0). 조건: 지금 도는 프로세스 = cmd.exe.
  - **D3** (멈춤, **Q100** 낮음) `nt!KxContextToKframes` 입구 `r r9d` (기대 `0x100003`). 코드로 이미 닫혀 값만 확인.
  - **D4** (**Q100** 낮음) 사진 K1 자리 옛 객체·옛 스택 값이 왜 남아 있나. 판정에 안 씀.
- 정적 (넘길 것 — 짐 지도 절 머리 메모):
  - **Q78d3** (`_TEB`, 3.3 머리): `PspAllocateThread +0x793` `SameTebFlags`, 복제 갈래 TEB 쓰기 (`+0x2cab6d ~ +0x2cabbd`, 안 탐), `MmCreateTeb +0x175 · +0x17e` 는 TEB 쪽 기준 (`EtwSupport` 아님).
  - **Q78d4** (3.13·3.15 머리): `PspInsertThread` — `+0x2cd` `KernelStackReference`, **`+0x617 ~ +0x649` `CrossThreadFlags` 비트 9~14 갈아 끼우고 DWORD 되쓰기** (→ 비트 칸 19개의 가름이 바뀔 수 있음), `+0x6e3` `ThreadListEntry`, `+0x96f` `Terminated`, `+0x9d6` `SelfTerminate`, `+0x2ca1d6` `ExitStatus`. `LastSoftParkElection*`·`UpdateTebApc` 바이트 `0x724 ~ 0x726`. 트랩 프레임 `Rax`·`R8~R11` (`KxContextToKframes` 줄) 과 `KiSystemServiceUser +0x2c ~ +0x38`.
  - (작은 것) "범위 밖 길" 75·"아무도 안 씀" 34 의 쓰는 함수를 `.asm` 로 한 줄씩 확인.
- 칸 표가 없는 짐 후보 → 짐 지도 7.6 에 **기록만**: 에너지 칸 (`+0x798 ~ +0x860`, `_THREAD_ENERGY_VALUES`), 그룹 2개 이상일 때 붙는 CPU 집합 칸 (안 탐).

## 그림

- flow.md 그림 (mermaid) 은 안 바꿨다. 새 단계·새 caller 가 없고 `PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread` 안 대표 지점만 늘었다. 2.1 글 트리에 `·` 줄로만 넣었다 ("그림엔 없음").

## 도구 메모

- 결과서 "도구 평가" — 하네스라 오케스트레이터가 사용자에게:
  - **제안 1 (다시, 더 구체적으로)**: `scripts/field_writers.py <바이너리> --struct _ETHREAD [--cells 목록]` — (a) 먼 조각 주인 붙이기 (`_index.tsv` 크기 안만 훑으면 `PspInsertThread +0x9d6` 같은 줄이 빠짐), (b) 비트 묶음 풀기, (c) 기준 레지스터 보정 (`rbp = 프레임 + 0x80`, `gs:[0x188]` 표시).
  - 제안 2: `pdb_struct.py` 의 겹치는 칸을 "같은 바이트 묶음" 으로 보여 주는 옵션.
  - 제안 3 (사소): `ExportFunctions` 가 이미 `.asm` 이 있는 함수는 건너뛴다고 알려 주기.
- 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

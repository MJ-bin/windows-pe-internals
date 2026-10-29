# 2026-10-29 run49 — 프로세스 객체 앞부분 (Q78c1): `_KPROCESS`·`_HANDLE_TABLE`·`_TOKEN` 모르는 칸 137 → **여전히 모름 0**. 토큰은 부모 것을 **복제**하고 (`SepDuplicateToken` 이 새 객체), 핸들 표는 **부모 표 복제** 길 (`ExDupHandleTable`), `HANDLE_TABLE.UniqueProcessId` 를 자기 PID 로 바꾸는 줄은 `PspInsertProcess +0x4c` (정적)

## 구조체별 요약 (모르는 칸 137 → ?)

- 결과서 맨 앞 표 그대로. 분류 뜻:
  - **아는 칸 (nt)** = 사진에서 값이 있거나 바뀐 칸이고, nt 에서 그 칸을 쓰는 줄을 찾음
  - **0 그대로 · 줄 있음** = 사진 값이 0 이고, nt 에 쓰는 줄은 있음. 이유는 셋 중 하나: 코드가 0 을 씀 / 쓰는 줄이 조건부라 안 탐 / 잡았다 놓아서 0 으로 돌아옴
  - **아무도 안 씀** = 만들기 길 (+ 부른 함수) 에 쓰는 줄이 없고, 할당 때 0 으로 채운 그대로 (`memset`·풀 0)
  - **(참고) 부모 토큰에서 복사** = 토큰 칸 중 값이 부모 토큰에서 온 것. 따로 센 열이 아니라 "아는 칸" 안에 들어 있다

| 구조체 | 모르는 칸 | 아는 칸 (nt) | 0 그대로 · 줄 있음 | 아무도 안 씀 | 여전히 모름 | (참고) 부모 토큰에서 복사 |
|---|---|---|---|---|---|---|
| `_KPROCESS` | 65 | 19 | 10 (0 을 씀 3 · 조건부 6 · 되돌림 1) | 36 | 0 | — |
| `_HANDLE_TABLE` | 17 | 10 | 3 (0 을 씀 2 · 조건부 1) | 4 | 0 | — (핸들 항목 안은 범위 밖) |
| `_TOKEN` | 55 | 29 | 26 (0 을 씀 15 · 값 0 인 복사·계산 4 · 조건부 7) | 0 | 0 | 16 (+ 값 0 인 복사 3) |
| **합** | **137** | **58** | **39** | **40** | **0** | |

- 짐 지도 (`baggage.md` 2장) 로 옮길 때는 run47 과 같은 규칙으로 합쳤다:
  - "아는 칸" 58 + "0 그대로 · 줄 있음" 39 = **97** → "nt 가 씀"
  - "아무도 안 씀" **40** → 2장 표 아래 줄 (표에 열 없음)
- 토큰에서 부모 값을 복사한 칸은 3장 "놓는 법" 칸에 **`입력 (부모 토큰 복제)`** 로 적었다. 복사하는 코드는 nt (`SepDuplicateToken`) 라 "쓴 주인" 은 nt 다.

짐 지도 2장 숫자 변화:

| 구조체 | nt 가 씀 | 아무도 안 씀 | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|
| `_KPROCESS` | 0 → **29** | **36** | 65 → **0** | 27 + 2 |
| `_HANDLE_TABLE` | 0 → **13** | **4** | 17 → **0** | 13 + 0 |
| `_TOKEN` | 0 → **55** | 0 | 55 → **0** | 53 + 2 |

- 정확·짐작은 짐 지도 3장 규칙 (**한 칸에 정확 줄이 하나라도 있으면 정확**) 으로 flow-writer 가 다시 셌다.
  - `_KPROCESS` 짐작 2 = `ProcessFlags`·`ProcessFlags.PpmPolicy` (쓰는 줄은 `[정적]` 인데 부르는 쪽이 `[추측]` — run47 `ReadAccess` 줄과 같은 꼴).
  - 결과서 4.1 은 "정확 18 · 짐작 1 (`Header.WaitListHead`)" 이라 했는데, `Header` 칸은 `KeInitializeProcess +0x48` 줄이 정확이라 정확으로 셌다. `WaitListHead` 의 뒤 바뀜 (`K3~P0`) 줄만 짐작이다.
  - `_TOKEN` 짐작 2 = `LogonSession`·`SidHash` (쓰는 함수 이름만 봄, 결과서와 같음).
- 짐 지도 전체: 구조체 21개 직접 칸 1,456 = 아는 칸 249 → **346** (정확 195 → **291** · 짐작 54 → **55**) + 아무도 안 씀 11 → **51** + 모름 1,196 → **1,059**.
  - 정확·짐작 합이 97 이 아니라 +96 · +1 인 것은 `_EPROCESS` 이미 아는 칸 셋 (`Pcb`·`Token`·`ObjectTable`) 이 짐작 → 정확으로 바뀌었기 때문이다 (아래 "정정").

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 03_sprint 일곱째 바퀴 (짐 지도 바퀴, plan **Q78c1** — Q78c 프로세스 객체를 구조체별로 나눈 첫 조각, 오케스트레이터 기본값 2026-10-29).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음). 이미 analyze 된 프로젝트에서 뽑기만 했다.
- 버전 대조 (결과서): `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` call `+0x802` / ret `+0x807` → `PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다. 코드로 계산한 자리가 run44 K1 사진과 칸마다 맞는다:
  - `KeInitializeProcess` 가 `KiMaximumGroups` (1) 로 깐 확장 메모리 자리 5개 (`ActiveProcessors`·`Affinity`·`AvailableCpuState`·`IdealProcessorAssignmentBlock`·`AutoBoostState` 포인터) 가 K1 값과 전부 같다.
  - 핸들 표 `Flags` = `0x18` (`Duplicated` 켜짐) = 복제 길.
  - 새 토큰의 `TokenLock`·`pSecurityAttributes`·`DynamicPart` 가 서로 다른 풀 주소 = `SepDuplicateToken` 이 따로 받는 풀 블록 셋.
  - `PspPriorityTable[2]` = 8 (PE 에서 읽음) = K1 `BasePriority` 8.
- 이 문서를 쓰며 다시 맞춘 것: 아래 호출 지점 표의 call/ret 을 `ms/disassemble/ntoskrnl/_calls.tsv` 로, 쓰는 줄 (`PspInsertProcess +0x27`·`+0x31`·`+0x4c`, `ExDupHandleTable +0xa6`, `ExpAllocateHandleTable +0x5c`·`+0x7c`, `ObInitProcess +0x53`, `KeInitializeProcess +0x48`·`+0x6e`·`+0x7d`·`+0x196`·`+0x1bf`·`+0x1dc`·`+0x2ac`·`+0x351`, `PspAllocateProcess +0x1a64`·`+0x1a72`, `KeStartThread +0x247`·`+0x2f4`·`+0x2fe`·`+0x3e9`, `KeSetProcessPpmPolicy +0x43`·`+0x4b`, `SepDuplicateToken +0x1a1`·`+0x1c6`·`+0x1e8`, `SeSubProcessToken +0x50d`·`+0x86c`) 을 `.asm` 으로 한 번 더 봤다. 칸 오프셋은 `pdb_struct.py` 로 봤다.
  - `.asm` 이 없는 곳 (결과서가 `objdump -d` 로만 봄): `MiCreateNewProcessTopLevelMappings` (`DirectoryTableBase` 쓰기 `+0x6e`), `MiAllocateProcessShadow` (`UserDirectoryTableBase` `+0xaa`), `PspComputeQuantum`, `ObpAllocateObject +0x191`·`ExAllocatePoolWithTag +0x48` (토큰 몸통 0 채움), `PspInitPhase1 +0xb3`. 이 문서에선 결과서의 함수+오프셋 그대로 쓰고 "`.asm` 없음" 이라고 붙였다.

## 목적

- 짐 지도 Q78 의 셋째 조각 (Q78c 의 첫 조각). 프로세스 객체 중 **`_KPROCESS`·`_HANDLE_TABLE`·`_TOKEN`** (baggage 3.12·3.16·3.19) 의 모르는 칸 137 을 닫는다.
  - run44 사진은 이 칸들이 거의 다 **`K1 이전`** 에 값이 있다는 것, `HANDLE_TABLE.UniqueProcessId` 가 `K2~K3` 에 부모 PID → 자기 PID 로 바뀐다는 것, 토큰 주소가 8장 모두 같다는 것만 보여 줬다.
- 요청서 질문 5개: (1) `_KPROCESS` 65칸, (2) `_TOKEN` 55칸 — 복제인가 공유인가, (3) `_HANDLE_TABLE` 17칸 — 새 표인가 부모 표 복제인가, `UniqueProcessId` 를 바꾸는 줄, (4) 칸별 경로, (5) 갈림길·입력.
- 범위 밖: `_EPROCESS` 칸 (Q78c2), 핸들 표 **안 항목** (상속 핸들 내용), 토큰 가변 영역 안 (SID·권한 배열 내용), 페이지 표 내용.

## 방법

- 정적: `_workspace/sast/43_kprocess-token-htable.md` — ntoskrnl. 새로 depth 0 **35개** (요청서 표 15 + 칸을 실제로 쓰는 함수 20: `ExpAllocateHandleTable`·`ExpInsertLowLevelTableIntoFreeList`·`KeStartThread`·`KeSetProcessPpmPolicy`·`PspSetProcessPpmPolicy`·`PspReferenceTokenForNewProcess`·`SepSetTokenSessionById` 등). 이미 `.c` 가 있던 8개 (`PspAllocateProcess`·`PspInsertProcess`·`MmCreateProcessAddressSpace`·`PspUserThreadStartup` 등) 는 읽기만.
  - `_calls.tsv` 3,284 → 3,598줄 (export 전 줄이 하나도 안 사라짐).
  - `refs` 1번 (`PspBamHostInterface`) → `ms/disassemble/ntoskrnl/_refs/PspBamHostInterface.tsv`.
  - 구조체 칸 표: `pdb_struct.py` (`_KPROCESS`·`_HANDLE_TABLE`·`_TOKEN` 직접 칸 65·17·55 = 요청서와 같음).
  - 값 대조: run44 덤프 읽기 로그 `ms/logs/04_baggage-read_K1.txt` (`K1 01c EPROCESS_RAW`, `K1 04 HANDLE_TABLE`), `ms/logs/04_baggage-read_supp_K1.txt` (`K1 05s TOKEN`), 지점별 EPROCESS raw 8장 비교. 새 동적 실행은 없다.
- 결과서: `_workspace/sast/43_kprocess-token-htable_result.md` (**분석성공**)
- 문서: 이 run 문서, `baggage.md` (3.11 고침·3.12·3.16·3.19 새로, 2장·2.1·7.1·7.2·7.4·7.6·7.7·7.8, 새 7.12), `flow.md` 2.1 (`PspAllocateProcess`·`PspInsertProcess` 절), 2.2 (`KeStartThread` 절), 2.3 (`PspUserThreadStartup` 절), 4장·5장. 그림은 안 바꿨다 (아래 "그림").

## 결과

비유 (새 가게 열기):

- **`_KPROCESS`** = 가게 운영 카드. 본사 (`KeInitializeProcess`) 가 카드 한 장을 거의 다 채운다. 우선순위·영업 시간 칸만 나중에 계산해서 덮어 쓰고, 첫 직원이 들어올 때 (`KeStartThread`) 직원 명단·전체 가게 명부 칸이 채워진다.
- **`_TOKEN`** = 출입증. 부모 가게 (cmd) 의 출입증을 **복사기로 새로 한 장 뽑는다** (`SepDuplicateToken`). 이름·권한은 그대로 옮겨 적고, 일련번호 (`TokenId`) 와 자물쇠 (`TokenLock`) 는 새것이다. 같은 출입증을 나눠 쓰는 게 아니다.
- **`_HANDLE_TABLE`** = 열쇠 꾸러미. 부모 꾸러미에서 "물려줘도 됨" 표시가 있는 열쇠만 골라 새 꾸러미에 복사한다 (`ExDupHandleTable`). 꾸러미 이름표의 주인 칸은 처음엔 **복사하던 사람 (부모 cmd)** 이 적히고, 가게가 명부에 오를 때 (`PspInsertProcess`) 자기 번호로 고쳐 쓴다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0x417` | `+0x41c` | `nt!PspReferenceTokenForNewProcess` (입력 토큰 고르기) | [정적] |
| `nt!NtCreateUserProcess` | `+0xc31` | `+0xc36` | `nt!PspAllocateProcess` | [정적] |
| `nt!NtCreateUserProcess` | `+0xfbd` | `+0xfc2` | `nt!PspInsertProcess` (`HANDLE_TABLE.UniqueProcessId` 를 자기 PID 로 — 입구 `+0x4c`) | [정적] |
| `nt!PspReferenceTokenForNewProcess` | `+0x3b` · `+0x64` | `+0x40` · `+0x69` | `ObReferenceObjectByHandle` (토큰 핸들이 왔을 때) · `PsReferencePrimaryTokenWithTag` (없으면 부모 주 토큰, 태그 `'Dflt'`). 부모도 없으면 `+0x6e` 전역 `PspBootAccessToken` | [정적] |
| `nt!PspAllocateProcess` | `+0x534` · `+0x55f` | `+0x539` · `+0x564` | `ObCreateObjectEx` (프로세스 객체) · `memset` (몸통 + `_KPROCESS` 확장 메모리 한꺼번에 0) | [정적] |
| `nt!PspAllocateProcess` | `+0x1637` | `+0x163c` | `nt!MmCreateProcessAddressSpace` (→ `+0x203` `MiCreateNewProcessTopLevelMappings` 가 `DirectoryTableBase`) | [정적] |
| `nt!PspAllocateProcess` | `+0x1719` | `+0x171e` | **`nt!KeInitializeProcess`** (`_KPROCESS` 몸통) | [정적] |
| `nt!PspAllocateProcess` | `+0x1922` | `+0x1927` | **`nt!PspInitializeProcessSecurity`** (토큰) | [정적] |
| `nt!PspAllocateProcess` | `+0x198c` | `+0x1991` | **`nt!ObInitProcess`** (핸들 표) | [정적] |
| `nt!PspAllocateProcess` | `+0x1a5f` | `+0x1a64` | `PspComputeQuantumAndPriority` (→ `+0x1a64` `BasePriority`·`+0x1a72` `QuantumReset` 덮어 씀) | [정적] |
| `nt!PspAllocateProcess` | `+0x1a00` · `+0x1a19` · `+0x1a3b` · `+0x2008` · `+0x235f` · `+0x279b` | `+0x1a05` · `+0x1a1e` · `+0x1a40` · `+0x200d` · `+0x2364` · `+0x27a0` | `KeSetCpuSetsProcess` · `KeSetExpectedConcurrencyCountProcess` · `PspSetProcessPriorityClass` · `KeInitializeProcessUserCetLogging` · `PspSetProcessAffinitySafe` · `KeSecureProcess` — 모두 조건부, test.exe 는 안 탄 것 (아래 갈림길) | [정적] |
| `nt!KeInitializeProcess` | `+0x191` · `+0x2c6` | `+0x196` · `+0x2cb` | `KeSetGroupMaskProcess` (`ActiveGroupsMask`) · `ExSaAllocate` (`PerProcessorCycleTimes`) | [정적] |
| `nt!PspInitializeProcessSecurity` | `+0xa7` | `+0xac` | 세션 번호 묻기 (같은 주소 이름 2개, 후보 `PsGetProcessSessionId`·`PsGetSessionId`) | [정적] |
| `nt!PspInitializeProcessSecurity` | `+0xf7` | `+0xfc` | **`nt!SeSubProcessToken`** | [정적] |
| `nt!PspInitializeProcessSecurity` | `+0x110` | `+0x115` | `ObInitializeFastReference` (`EPROCESS.Token` = 새 토큰 `\| 0xf`) | [정적] |
| `nt!PspInitializeProcessSecurity` | `+0x1d7` | `+0x1dc` | `SeAssignPrimaryToken` — 부모 없는 길 (`+0x53 JZ`) 만, test.exe 는 안 탐 | [정적] |
| `nt!SeSubProcessToken` | `+0x43c` | `+0x441` | **`nt!SepDuplicateToken`** (새 토큰 객체 + 칸 복사) | [정적] |
| `nt!SeSubProcessToken` | `+0x503` · `+0x5bb` · `+0x80e` | `+0x508` · `+0x5c0` · `+0x813` | `SepSetTokenSessionById` (새 프로세스 세션으로) · `SepMandatorySubProcessToken` (`TokenFlags` 일부) · `ObInsertObjectEx` | [정적] |
| `nt!SepDuplicateToken` | `+0x158` | `+0x15d` | `ObCreateObjectEx` (`SeTokenObjectType`, 크기 = 원본 `VariableLength` + `0x5c0`) — **새 객체** | [정적] |
| `nt!SepDuplicateToken` | `+0x31b` · `+0x415` · `+0x4cd` · `+0x81b` | `+0x320` · `+0x41a` · `+0x4d2` · `+0x820` | `SepSetTokenSessionById` · `SepDuplicateLogonSessionReference` · 메모리 복사 (가변 영역 통째. 같은 주소 이름 4개, 후보 `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) · `RtlSidHashInitialize` | [정적] |
| `nt!SepSetTokenSessionById` | `+0x33` | `+0x38` | `PsGetSessionObjectById` (→ `+0x38` `SessionObject`) | [정적] |
| `nt!ObInitProcess` | `+0x40` | `+0x45` | `ExCreateHandleTable` — 상속 비트가 꺼졌을 때 (빈 새 표). test.exe 는 안 탐 | [정적] |
| `nt!ObInitProcess` | `+0xaa` · `+0xcc` | `+0xaf` · `+0xd1` | `ObReferenceProcessHandleTable` (부모 표) · **`nt!ExDupHandleTable`** (부모 표 복제) | [정적] |
| `nt!ExDupHandleTable` | `+0x5f` | `+0x64` | `nt!ExpAllocateHandleTable` (표 머리 할당·칸 채우기) | [정적] |
| `nt!ExpAllocateHandleTable` | `+0xb4` | `+0xb9` | `ExpInsertLowLevelTableIntoFreeList` (`NextHandleNeedingPool` `+0x400`) | [정적] |
| `nt!PspInsertThread` | `+0x5dd` | `+0x5e2` | `nt!KeStartThread` (`StackCount`·`ProcessListEntry`·`ThreadListHead`) | [정적] |
| `nt!PspSetProcessPpmPolicy` | `+0x2b` · `+0xbe` | `+0x30` · `+0xc3` | `KeSetProcessPpmPolicy` (`ProcessFlags.PpmPolicy`). `PspSetProcessPpmPolicy` 를 직접 부르는 곳은 없다 — 함수 표 `PspBamHostInterface +0x8` 로만 가리켜짐 | [정적] |
| `nt!PspUserThreadStartup` | `+0x37` | `+0x3c` | `PspDisablePrimaryTokenExchange` — 토큰 칸은 **안 쓴다** (EPROCESS 쪽만) | [정적] |

### 호출 관계 (대표 지점)

```
NtCreateUserProcess
 ├─ (1) +0x41c  PspReferenceTokenForNewProcess      입력 토큰 = 부모 cmd 주 토큰 (+0x64 PsReferencePrimaryTokenWithTag)  [정적]
 ├─ (2) +0xc36  PspAllocateProcess                                                                              [정적]
 │       │  +0x539 ObCreateObjectEx → +0x564 memset (몸통 + KPROCESS 확장 메모리 0)
 │       ├─ (a) +0x163c MmCreateProcessAddressSpace
 │       │       └─ +0x208  MiCreateNewProcessTopLevelMappings   +0x6e DirectoryTableBase (.asm 없음)
 │       ├─ (b) +0x171e KeInitializeProcess                KPROCESS 몸통 거의 다 (Header·목록 머리·친화도·우선순위 처음 값 …)
 │       │       └─ +0x196  KeSetGroupMaskProcess          ActiveGroupsMask
 │       ├─ (c) +0x1927 PspInitializeProcessSecurity
 │       │       ├─ +0xfc   SeSubProcessToken
 │       │       │       ├─ +0x441  SepDuplicateToken      +0x15d ObCreateObjectEx (새 토큰) → 칸 복사·새 TokenId·새 TokenLock
 │       │       │       ├─ +0x508  SepSetTokenSessionById SessionId·SessionObject (새 프로세스 세션)
 │       │       │       ├─ +0x5c0  SepMandatorySubProcessToken   TokenFlags 일부
 │       │       │       │  +0x86c  TokenInUse = 1
 │       │       └─ +0x115  ObInitializeFastReference      EPROCESS.Token = 새 토큰 | 0xf  (Q78c2)
 │       ├─ (d) +0x1991 ObInitProcess
 │       │       ├─ +0xaf   ObReferenceProcessHandleTable  부모 표
 │       │       ├─ +0xd1   ExDupHandleTable               +0xa6 Flags.Duplicated · 상속 항목 복사 · +0x493~+0x49d 전역 목록
 │       │       │       └─ +0x64  ExpAllocateHandleTable  QuotaProcess · UniqueProcessId (= 지금 도는 프로세스 cmd) · Flags bit4
 │       │       │  +0x53   EPROCESS.ObjectTable  (Q78c2)
 │       └─ (e) +0x1a64 PspComputeQuantumAndPriority 뒤   +0x1a64 BasePriority · +0x1a72 QuantumReset 덮어 씀
 ├─ (3) +0xfc2  PspInsertProcess                    +0x4c HANDLE_TABLE.UniqueProcessId = 자기 PID (입구 바로, 분기 앞)  [정적]
 └─ (4) +0x101a PspInsertThread
         └─ +0x5e2  KeStartThread                   +0x247~+0x270 ProcessListEntry · +0x2f4~+0x2fe ThreadListHead · +0x3e9 StackCount
(그 뒤, 부르는 쪽 [추측] BAM)  PspBamHostInterface 표 → PspSetProcessPpmPolicy → +0x30 KeSetProcessPpmPolicy   +0x43/+0x4b ProcessFlags.PpmPolicy
(test.exe 스레드)  PspUserThreadStartup → +0x3c PspDisablePrimaryTokenExchange   토큰은 읽기만, EPROCESS Flags2·Flags3 를 씀
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 쓰는 줄.
- `(a)`~`(e)` 는 이 트리 안 이름표다. 코드 순서는 오프셋 순 (`+0x163c` → `+0x171e` → `+0x1927` → `+0x1991` → `+0x1a64`).

### 토큰 — 복제다, 공유가 아니다 (질문 2)

- **입력 토큰**: `PspReferenceTokenForNewProcess` 가 고른다. 토큰 핸들 속성이 오면 그 토큰, 없으면 **부모의 `EPROCESS.Token`** (`+0x64`), 부모도 없으면 `PspBootAccessToken`. cmd 가 띄운 test.exe 는 둘째 갈래로 보인다 `[정적 + 추측: 핸들 속성이 없다는 것은 동적 D1]`.
- **복제 길로 가는 조건** (`SeSubProcessToken`): 자식 금지 제한이 없고 (`+0x117 ~ +0x12b`), AppContainer 속성 (`[R15+0x20]`) 이 없으면 `+0x43c SepDuplicateToken`. 있으면 `SepCreateAppContainerToken` (`+0x347`). 사진 토큰 `Package` = 널 → 복제 길 `[정적 조건 + 관찰 간접]`.
- **새 객체인 근거** `[정적]`:
  - `SepDuplicateToken +0x158` 가 `ObCreateObjectEx` 로 새 몸통을 받는다. 원본과 새 토큰을 따로 다룬다.
  - 새 `TokenId` = 전역 LUID 카운터 (`+0x1a1 XADD.LOCK [ExpLuid]`). K1 `0x24c39e` — 원본 쪽 LUID (`0x21d70 ~ 0x21d7c`) 와 크게 다르다.
  - 새 `TokenLock` (`+0xb3` 풀 `SeTl` + `+0x1d8 ExInitializeResourceLite`), 새 `pSecurityAttributes` (풀 `SeAt`), 새 `DynamicPart` (풀 `SeDT`).
  - 그래서 사진 8장이 같은 토큰 주소 (`ffffb689ae6b6060`) 인 건 **이 프로세스 안에서 토큰이 안 바뀐다**는 뜻이고, 부모 cmd 토큰과 같은 주소라는 뜻이 아니다. 부모 토큰과 값으로 맞춰 보기는 동적 D1.
- **몸통은 0 으로 채워져 온다**: `ObpAllocateObject +0x191` 이 풀 종류에 0 채움 비트를 켜고, `ExAllocatePoolWithTag +0x48` 이 그 비트가 있으면 "초기화 안 함" 을 안 붙인다 `[정적, .asm 없음 — objdump]`. 그래서 `SepDuplicateToken` 이 안 쓰는 칸은 0 이다.
- **칸 셋으로 나뉜다** (결과서 2.2·2.3):
  - 부모 토큰에서 복사 16 (`TokenSource`·`AuthenticationId`·`ParentTokenId`·`ExpirationTime`·`ModifiedId`·`Privileges`·`AuditPolicy`·`UserAndGroupCount`·`VariableLength`·`DynamicCharged`·`IntegrityLevelIndex`·`MandatoryPolicy`·`LogonSession`·`OriginatingLogonSession`·`IndexEntry`·`VariablePart`). `ParentTokenId` 는 원본의 `ParentTokenId` 를 그대로 복사한다 (원본의 `TokenId` 가 아님). `LogonSession`·`IndexEntry` 는 같은 객체를 **참조만 늘려 함께 쓴다**.
  - 새로 정함·계산 13 (`TokenId`·`TokenLock`·`SessionId`·`UserAndGroups`·`PrimaryGroup`·`DynamicPart`·`DefaultDacl`·`TokenType`·`TokenFlags`·`TokenInUse`·`SidHash`·`pSecurityAttributes`·`SessionObject`).
  - 0 을 씀 15 · 값 0 인 복사·계산 4 · 조건부 안 탐 7 (3장 3.19).
- **`PspDisablePrimaryTokenExchange` 는 토큰 칸을 안 쓴다** `[정적]`: 토큰은 `+0x4e PsReferencePrimaryTokenWithTag` → `+0x59 SeTokenIsAdmin` 으로 읽기만. 쓰는 건 EPROCESS `Flags2.PrimaryTokenFrozen` (`+0x22`), `Flags3.AddressPolicyFrozen`·`SecurityFreezeComplete` (`+0xa4`·`+0xbb`·`+0x111`), 조건부 `SecurityDomain`·`ParentSecurityDomain` 지움 (`+0x94`·`+0xa2`), `KPROCESS.AddressPolicy` (`+0xfd`, 안 탐). 사진 EPROCESS `Flags2` raw `0x5000 → 0xd000` (`P0~P1`) 와 맞다.

### 핸들 표 — 부모 표 복제 길 (질문 3)

- **가르는 줄**: `PspAllocateProcess +0x1978 ~ +0x1986` 이 `ProcessFlags` (`NtCreateUserProcess` 7번째 인자) **비트 2** 로 `ObInitProcess` 첫 인자를 부모 / 0 으로 정한다. 비트 2 이름 `PROCESS_CREATE_FLAGS_INHERIT_HANDLES` 는 알려진 값 `[추측]`.
  - 0 → `ObInitProcess +0x40 ExCreateHandleTable` (빈 새 표).
  - 부모 → `+0xaa ObReferenceProcessHandleTable` → `+0xcc ExDupHandleTable` (복제).
  - 두 길 다 끝에서 `+0x53` 이 `EPROCESS.ObjectTable` 에 표 주소를 넣는다.
- **test.exe 는 복제 길이다 — 사진 + 코드로 확정**: K1 `Flags` = `0x18` → `Duplicated` (bit 3) = 1. 이 비트를 켜는 줄은 `ExDupHandleTable +0xa6 OR byte [RCX+0x2c],8` 하나뿐이다 (`ExCreateHandleTable` 길엔 없음).
  - 핸들 수 4 = `ExDupHandleTable +0x3b1 INC [RCX+0x58]` 이 4번 = 부모 cmd 표에서 상속 비트가 켜진 항목 4개 (`+0x337 ~ +0x33f`).
  - 넘겨받은 "상속할 핸들 목록" 속성 (개수·배열) 이 비었는지는 사진으로 못 가린다 (인자가 사라짐) → 동적 D2.
- **`UniqueProcessId` 를 쓰는 줄 둘** `[정적]`:
  - 처음 값 = **`ExpAllocateHandleTable +0x7c MOV dword [RDI+0x28],EAX`** — EAX 는 "지금 도는 스레드의 프로세스" 의 PID (`GS:[0x188] → +0xb8 → +0x1d0`). 지금 도는 건 부모 cmd 스레드라 부모 PID `0x19a8` 가 들어간다.
  - 자기 PID 로 = **`PspInsertProcess +0x4c MOV dword [R10+0x28],EAX`** — `+0x27` 이 `R10 = EPROCESS.ObjectTable` (`[RCX+0x300]`), `+0x31` 이 `EAX = EPROCESS.UniqueProcessId` (`[RCX+0x1d0]`). 함수 입구 바로, 분기 앞이다.
  - 사진과 맞다: K2 (= `PspInsertProcess` 입구, 아직 안 씀) `0x19a8` → K3 `0x1dd0`.

### `_KPROCESS` — 몸통은 한 함수, 덮는 곳은 따로 (질문 1)

- `PspAllocateProcess +0x55f memset` 이 몸통 + 확장 메모리를 한꺼번에 0 으로 민 뒤, `+0x1719 KeInitializeProcess` 가 칸 대부분을 쓴다 (인자 9개: 객체 · 처음 우선순위 `8` · 주 그룹 · 친화도 · 노드 · 스케줄링 그룹 · (안 씀) · 플래그 · 확장 메모리 시작).
- 값이 정해진 뒤 **덮어 쓰는 곳**:
  - `BasePriority` = `PspPriorityTable[PriorityClass]` (`+0x1a64`), `QuantumReset` = `PspComputeQuantum` 결과 (`+0x1a72`, `PspComputeQuantum` 은 `.asm` 없음).
  - `DirectoryTableBase` = 최상위 페이지 쪽 번호 `<< 12` (`MiCreateNewProcessTopLevelMappings +0x6e`, `.asm` 없음). K1 bit 1 꺼짐 = `KiFlushPcid` 0 `[관찰 간접]`.
- **K2~K3 에 바뀌는 칸**은 첫 스레드를 달 때 `KeStartThread` (`PspInsertThread +0x5dd`) 가 쓴다: `ProcessListEntry` (전역 `KiProcessListHead` 꼬리, 첫 스레드이고 `KiInitialProcess` 가 아닐 때 `+0x247 ~ +0x270`), `ThreadListHead` (`+0x2f4 ~ +0x2fe`), `StackCount` (`+0x3e9 ADD.LOCK [RDI+0x108],8` = 스레드 하나). `P1a~P1b` 의 `StackCount` `8 → 0x18` (+0x10 = 스레드 둘) 은 일꾼 스레드 `[추측]` (Q78d).
- **K3~P0 의 `ProcessFlags.PpmPolicy` (`0x380`, 정책 7)** 은 `KeSetProcessPpmPolicy +0x43 AND ECX,0xfffff87f` · `+0x4b CMPXCHG.LOCK [R14+0x88]` 이 쓴다 (PE 전체에서 이 마스크로 쓰는 곳이 여기 하나 `[정적 스캔]`).
  - 부르는 `PspSetProcessPpmPolicy` 는 직접 호출자가 없고 함수 표 `PspBamHostInterface +0x8` 로만 가리켜진다. 그 표는 `PspInitPhase1 +0xb3` 이 `ExRegisterHost(PspBamExtensionHost, …)` 에 넘긴다 (`.asm` 없음) → **부르는 쪽은 nt 밖 BAM 확장 (bam.sys) `[추측]`** → 동적 D3.
  - 같은 함수 `+0xa5` 가 `EPROCESS.BamPpmPolicy` (`+0x820`) 도 쓴다 — 사진에서 같은 `K3~P0` 구간 (Q78c2).
- `Header.WaitListHead` 가 `K3~P0` 에 대기 블록 (`ffff8285'98adc1c0`) 을 단다 — 누군가 이 프로세스 객체를 기다림 (부모 cmd 스레드 `[추측]`). 줄기 밖. 덤프로 확인 가능 (D4).
- **끝까지 0 46칸**: 0 을 씀 3 (`SchedulingGroup`·`PrimaryGroup`·`IdealGlobalNode` — 인자 값이 0) · 조건부 안 탐 6 (`AutoAlignment`·`ExtendedFeatureDisableMask`·`SecureState`·`AddressPolicy`·`UserCetLogging`·`UserDirectoryTableBase`) · 되돌림 1 (`ProcessLock`) · **아무도 안 씀 36** (`CycleTime`·`KernelTime`·`InstrumentationCallback`·`Spare*` 등). 아무도 안 씀 36 중엔 런타임에 스케줄러·다른 시스템 콜이 쓸 칸이 섞여 있다 `[추측]` — 사진 8장에선 0 이라 "초기화 동안 아무도 안 쓴다" 까지만 말한다.
- 확장 메모리 안 블록 셋 (`_KPROCESS_AVAILABLE_CPU_STATE` 0x78 · `_KI_IDEAL_PROCESSOR_ASSIGNMENT_BLOCK` 0x1d8 · `_KAB_UM_PROCESS_TREE`) 은 `_KPROCESS` 칸이 아니다 → 칸 표 없는 짐 (baggage 7.6).
- `PerProcessorCycleTimes` 값 `0x9300` 은 포인터가 아니라 `ExSaAllocate` 가 돌려준 "슬랩 핸들" 로 보인다 `[추측]` (`+0x2eb ~ +0x335` 가 그 값에서 비트를 꺼내 씀).

### 갈림길 — test.exe 가 가는 쪽 (질문 5)

| 갈림길 | 줄 | test.exe | 근거 |
|---|---|---|---|
| 상속 핸들 (복제) / 빈 표 | `PspAllocateProcess +0x1978 ~ +0x1986` ← `ProcessFlags` 비트 2 | **복제** | 사진 `Flags.Duplicated` = 1 + 코드 |
| 핸들 목록 속성 (일부만) / 전부 | `ExDupHandleTable +0x43` ← 속성 모음 `[+0x124]` | 전부 `[추측]` | 동적 D2 |
| 토큰 핸들 속성 / 부모 토큰 / 부팅 토큰 | `PspReferenceTokenForNewProcess +0x3b` / `+0x64` / `+0x6e` | 부모 토큰 `[추측]` | 동적 D1 |
| AppContainer 토큰 | `SeSubProcessToken +0x2f8 ~ +0x305` | **아님** (복제 길) | 사진 `Package` 널 (간접) |
| 자식 금지 토큰 | `SeSubProcessToken +0x117 ~ +0x12b` | 아님 `[추측]` | `TokenFlags` `0x2a00` 로는 못 가림 |
| 작업 (Job) | `EPROCESS.Job` | **없음** | 사진 `Job` = 0 |
| CET 로그 | `PspAllocateProcess +0x1ff8` (`MitigationFlags2 & 0x4000`) | **아님** | 사진 raw `+0x754` = `0x40000000` |
| VBS 보안 프로세스 | `EBX` bit 16 / 인자 8 bit 1 | **아님** | 사진 `SecureState` = 0 |
| WoW64 x86 (정렬 끔) | `PspAllocateProcess +0x2216` (`Machine == 0x14c`) | **아님** | 사진 raw `0x8664` |
| 정렬 오류 끔 | `DefaultHardErrorProcessing & 4` | **아님** | 사진 raw `= 1` |
| 이미지 `ProcessAffinityMask` | `PspAllocateProcess +0x2323` | 아님 `[추측]` | PE 헤더 읽기로 확인 가능 |
| 커널 주소 격리 / PCID | `MiFlags & 0x600000` / `KiFlushPcid` | 둘 다 꺼짐 `[관찰 간접]` | `UserDirectoryTableBase` = 0, `DirectoryTableBase` bit 1 = 0 |
| 캐시 인지 스케줄링 | `KiCacheAwareScheduling & 8` | **켜짐** | 사진 `PerProcessorCycleTimes` 값 있음 |
| CPU 기능 비트 `0x37` | `KeInitializeProcess +0x369` | **못 가림** (안 탔거나 KUSD 값 0) | 동적 D5 (덤프로) |
| NUMA 노드 | `PspSelectNodeForProcess` / 부모 `IdealGlobalNode` / `KeSelectNodeForAffinity` | 노드 0 — 어느 갈래인지 `[추측]` | 사진 값만 |
| 부모 없는 프로세스 | `PspInitializeProcessSecurity +0x53`, `ObInitProcess +0x36` | 아님 | — |

### 읽는 입력

- 부모 프로세스 (= 지금 도는 cmd): 핸들 표 원본, `HANDLE_TABLE.UniqueProcessId` 처음 값, 토큰 원본.
- **부모 토큰** → 토큰 복사 16칸 (+ 값 0 인 복사 3칸).
- **부모 핸들 표** → 상속 항목 4개 (`HandleCount`·`HighWaterMark`·빈 항목 칸). 항목 안 내용은 범위 밖 (baggage 7.6 C11).
- `ProcessFlags` (비트 2 = 핸들 상속), 속성 모음 (`[+8]` 비트, `[+0x124]/[+0x128]` 핸들 목록, `[+0x20]` AppContainer).
- 부모 EPROCESS 칸 (`DefaultHardErrorProcessing`, `Flags2`, `KPROCESS.IdealGlobalNode`, `MitigationFlags*`) 와 새 프로세스 `PriorityClass`.
- 커널 전역: `KiMaximumGroups`·`KeNodeBlock`·`KiCacheAwareScheduling`·`KeFeatureBits`·`KiFlushPcid`·`MiFlags`·`PspPriorityTable`·`PspForegroundQuantum`·`PsPrioritySeparation`·`HandleTableListHead/Lock`·`KiProcessListHead/Lock`·`ExpLuid`·`ExpLuidIncrement`·`SeTokenObjectType`·`SeTokenLeakTracking`·`SepTokenSidSharingEnabled`·`PspBootAccessToken`.
- 이 조각 안에는 `syscall` 이 없다 (전부 부모 `NtCreateUserProcess` 시스템 콜 안. `PpmPolicy` 만 그 뒤 `[추측]` BAM).

## 확인된 것

- **토큰은 부모 토큰의 복제다** — `SeSubProcessToken +0x43c → SepDuplicateToken +0x158 ObCreateObjectEx` 가 새 객체를 만들고 칸을 복사한다. 새 `TokenId`·`TokenLock`·`pSecurityAttributes`·`DynamicPart` 는 새로 받는다. `[정적]` · 사진 K1 (`TokenId` `0x24c39e`, 풀 주소 셋이 서로 다름) 과 맞음
- **핸들 표는 부모 표 복제 길이다** — `ObInitProcess +0xcc ExDupHandleTable`. `Flags.Duplicated` 를 켜는 줄이 `ExDupHandleTable +0xa6` 하나뿐인데 K1 `Flags` = `0x18`. `[정적]` + run44 `[관찰]`
- **`HANDLE_TABLE.UniqueProcessId` 처음 값은 `ExpAllocateHandleTable +0x7c` (지금 도는 프로세스 = 부모), 자기 PID 로 바꾸는 줄은 `PspInsertProcess +0x4c`** `[정적]` (오케스트레이터·이 문서 둘 다 `.asm` 확인). run44 `K2~K3` 바뀜과 맞음 → flow.md 2.1 `[공백]`·baggage 7.8 닫힘
- **`_KPROCESS` 몸통은 `KeInitializeProcess` (`PspAllocateProcess +0x1719`)** 가 쓰고, `BasePriority`·`QuantumReset` (`+0x1a64`·`+0x1a72`)·`DirectoryTableBase` (`MiCreateNewProcessTopLevelMappings +0x6e`) 는 따로 덮는다. `[정적]`
- **`KeStartThread` 가 쓰는 프로세스 칸**: `ProcessListEntry`·`ThreadListHead`·`StackCount`, 그리고 프로세스 락 (`+0x99` 잡기 · `+0x38d` 놓기). run12 의 "스케줄링 준비 `[추측]`" 에 더해, 프로세스 쪽 목록 달기도 여기서 한다. `[정적]` · 사진 `K2~K3` 과 맞음
- **`ProcessFlags.PpmPolicy` 를 쓰는 줄은 `KeSetProcessPpmPolicy +0x43`·`+0x4b`** 이다. `[정적]`. 부르는 쪽 (BAM) 은 `[추측]`
- **`PspDisablePrimaryTokenExchange` 는 토큰 칸을 안 쓴다** (EPROCESS `Flags2`·`Flags3` 등만). `[정적]`
- 세 구조체 137칸 모두 쓰는 줄이 있거나 (97) "아무도 안 씀" (40) 으로 갈렸다. 여전히 모름 0. `[정적]`

## 정정

- **baggage 3.19 "붙이는 곳 `PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange`" — 토큰을 붙이는 함수가 아니다.** 토큰 칸은 읽기만 하고, 쓰는 건 EPROCESS 쪽 (`Flags2.PrimaryTokenFrozen` 등 — 7.2 `P0~P1` 칸) 이다. 짐 지도는 이 스프린트 문서라 바로 고쳤다.
  - 출처: run43 (sast/38 대조) 이 run15 의 "`PspDisablePrimaryTokenExchange(스레드)` — 늘" (이름으로 본 `[추측]`) 을 토큰 절에 붙였다. run15 는 "이름으로 본 것" 이라고 적었으니 틀린 말은 아니라서 run15 문서는 안 고쳤다.
- **baggage 3.11·3.12·3.16·3.19 의 `+0x171e`·`+0x1927`·`+0x1991` 은 ret 이다.** call 은 `+0x1719`·`+0x1922`·`+0x198c`. 짐 지도 3장 경로 칸 규칙 ("`호출 +0x..` 은 부르는 자리") 에 맞춰 call 로 고치고 ret 은 괄호에 남겼다. 5장 ① 순서 줄도 call 로 고쳤다. 값이 틀린 건 아니다.
- **baggage 3.11 `Pcb`·`Token`·`ObjectTable` — `[추측]` 짐작 → `[정적]` 정확.** 셋 다 "부르는 줄만 앎 (callee 본문 안 봄)" 이었다.
  - `Pcb`: `KeInitializeProcess` 첫 인자 `RCX = R14` (새 프로세스, `PspAllocateProcess +0x16df`) — 칸마다는 3.12.
  - `Token`: `PspInitializeProcessSecurity +0x109 LEA RCX,[RDI+0x248]` → `+0x110 ObInitializeFastReference` 가 "새 토큰 `| 0xf`" 를 쓴다 (`+0x23 ~ +0x30`, 결과서). K1 `…606f` 와 맞음.
  - `ObjectTable`: `ObInitProcess +0x53 MOV [RDI+0x300],R14` (`.asm` 확인).
- **baggage 7.8 / flow.md 2.1·4장 `HANDLE_TABLE.UniqueProcessId` 쓰는 줄 `[공백]` → 닫힘** (위 "확인된 것").
- flow.md 2.2 `KeStartThread` 절의 "`[추측]` 본문은 안 뽑았다 … 스케줄링 준비로 보인다" 는 틀린 말은 아니라 정정이 아니라 보탬 (`[정적]` 줄 셋을 더함). run12 문서는 안 고친다.

## 아직 확인 필요

- 동적 (결과서 번호 그대로, D1 ~ D6). **D1·D4·D5 는 이미 있는 덤프로 답할 수 있음 → plan Q79 에 출처로**, **D2·D3·D6 은 실행 중 멈춤이 필요 → plan Q96**. 멈춤점은 결과서 "동적분석으로 확인해야 할 것" 표, test.exe 일 때만 서게 하는 조건은 dast-mentor 몫.
  - **D1** (덤프, Q79) 새 토큰이 부모 cmd 토큰의 복제임을 값으로: K1 덤프에서 `!process 19a8 0` → cmd `EPROCESS.Token & ~0xf` → `dt nt!_TOKEN`. `TokenId`·`ParentTokenId` (`0x21d73` 예상)·`ModifiedId` (`0x21d7c`)·`AuthenticationId` (`0x21d70`) 가 어느 칸은 같고 어느 칸은 다른가, 주소는 다른가. (덤프에 커널 풀이 있어야 함. 없으면 `nt!SepDuplicateToken` 입구 `rcx` 로 — Q96 쪽.)
  - **D2** (멈춤, Q96) `ObInitProcess` 입구에 넘어간 핸들 목록 속성: `r8d` (개수) = 0, `r9` (배열) = 널, `rcx` ≠ 0 인가. 조건은 지금 도는 프로세스 = cmd.exe (새 프로세스는 아직 이름이 없음). 상속 길 자체는 이미 사진으로 확정.
  - **D3** (멈춤, Q96) `ProcessFlags.PpmPolicy` = 7 을 누가 부르나: `nt!KeSetProcessPpmPolicy` 입구 `k`, `rdx` = 7, `rcx` = 새 EPROCESS (조건: `ImageFileName` = test.exe). 부르는 모듈이 bam.sys 인가, 어느 시점 (`PspCallProcessNotifyRoutines` 알림 안?) 인가.
  - **D4** (덤프, Q79) `Header.WaitListHead` `K3~P0` 의 `ffff8285'98adc1c0` 은 누구의 대기 블록인가 — P0 덤프에서 `dt nt!_KWAIT_BLOCK ffff828598adc1c0` (`.Thread`·`.Object`).
  - **D5** (덤프, Q79) 조건 전역 값: `KiCacheAwareScheduling` (bit 3)·`KeFeatureBits` (bit `0x37`)·`MiFlags` (`& 0x600000`)·`KiFlushPcid`·`KiMaximumGroups`·KUSD `+0x710`·`PspForegroundQuantum[0]`·`PsPrioritySeparation` 를 `dd`/`dq`/`db` 한 번씩. `ExtendedFeatureDisableMask` 0 이 "안 탐" 인지 "0 을 씀" 인지 닫힌다.
  - **D6** (멈춤, Q96, 급하지 않음) `TokenFlags` `0x2a00` 의 단계별 분해: `nt!SepDuplicateToken` 반환 직후 `dd poi(@rax)+0xc8`, `nt!SeSubProcessToken` 반환 직후.
- 정적 (Q78c2 `_EPROCESS` 로 넘김 — 결과서 "Q78c2 로 넘길 것" 표. 짐 지도 3.11 머리 "Q78c2 에서 볼 것" 에 메모):
  - `SecurityDomain`·`ParentSecurityDomain` (`PspInitializeProcessSecurity +0x1a0`·`+0x1a7`, 지움 `PspDisablePrimaryTokenExchange +0x94`·`+0xa2`), `Flags2.PrimaryTokenFrozen`·`Flags3.AddressPolicyFrozen`·`SecurityFreezeComplete` (`PspDisablePrimaryTokenExchange +0x22`·`+0xbb`·`+0xa4`·`+0x111`), `PpmPolicyLock`·`BamPpmPolicy` (`PspSetProcessPpmPolicy +0x70 ~ +0x9a`·`+0xa5`), `PriorityClass` (`PspSetProcessPriorityClass +0x72`, 기본값 줄은 못 찾음), `Flags.AddressSpaceInitialized` 의 bit 11 (`MiCreateNewProcessTopLevelMappings +0x72`), `+0x540` 목록·`+0x3d8`·`+0x410` (이름 모름), `PspAllocateProcess +0x564 ~ +0x5fc` 초기화 묶음, 몸통 크기 계산 (`+0x450 ~ +0x4f7`).
- 칸 표가 없는 짐 후보 (결과서 정정 6) → baggage 7.6 에 **기록만**: `_KPROCESS_AVAILABLE_CPU_STATE` (0x78), `_KI_IDEAL_PROCESSOR_ASSIGNMENT_BLOCK` (0x1d8), `_KAB_UM_PROCESS_TREE`, `_HANDLE_TABLE_FREE_LIST` (0x40), 핸들 표 낮은 단계 페이지 (4KB, 항목 0x10씩), `_SEP_LOGON_SESSION_REFERENCES`, `_SEP_LUID_TO_INDEX_MAP_ENTRY`. (결과서는 "6개" 라 했지만 목록은 7개다 — 그대로 7개를 적었다.)
- 이름만 둔 줄기 밖 함수: 토큰 마무리 `SepSetTokenBnoIsolation`·`SepDesktopAppxSubProcessToken`·`SepSetTrustLevelForProcessToken`·`SepSetProcessUniqueAttribute`·`SepAddTokenOriginClaim`·`SepFinalizeTokenAcls`, 핸들 만들기 공용 부품 `ExCreateHandleEx`·`ObpCreateHandle`·`ObDuplicateObject`·`ExFastReplenishHandleTableEntry`·`ExSlowReplenishHandleTableEntry`·`ExDestroyHandle` (핸들 수 4 → 26 → 44 → 46 때 `FreeLists`·`ActualEntry` 를 바꾸는 쪽, 어느 것이 어느 칸인지 `[추측]`).

## 도구 메모

- 결과서 "도구 평가" — 하네스라 오케스트레이터가 사용자에게:
  - **제안 1 — "이 구조체 칸을 누가 쓰나" 도구**: `refs` 는 전역·함수 주소만 찾는다. 이번에 `PpmPolicy` (`+0x88` 4비트)·`UserDirectoryTableBase` (`+0x158`)·`HANDLE_TABLE.UniqueProcessId` (`+0x28`)·`DirectoryTableBase` (`+0x28`) 를 `objdump -d` + 정규식으로 훑었다 (스크래치 임시 스크립트, 레포 밖). 제안: `field_writers.py <바이너리> <오프셋> [마스크 …]` — "`[reg+off]` 에 쓰는 명령 + 근처 마스크 상수" 를 `함수+오프셋` 으로. 오탐이 많으니 "후보 줄" 로만 쓰고 `.asm` 로 확인하는 약속과 함께.
  - **제안 2 — 함수 포인터 표 경유 호출자**: `PspSetProcessPpmPolicy` 는 직접 호출자 0, `PspBamHostInterface +0x8` 표로만 닿는다. `refs <함수> --through-ptr` 처럼 "함수 → 표 이름 (PDB) → 표를 참조하는 곳" 을 한 번에 이어 주면 좋겠다.
  - `pdb_struct.py` 의 앞 바퀴 제안 (`--expand`, `overlap` 열) 은 이번에도 필요했다 (`_KPROCESS.SecureState` 안쪽은 sast/38 `struct__KPROCESS.tsv` 로 대신 봄).
- `pdb_aliases.py --lookup ntoskrnl …` 은 표 (`ms/sym/ntoskrnl.aliases.tsv`) 가 없어서 안 됐다. 같은 주소 이름은 `.asm` 줄 끝 `ALIASES(n)` 주석으로 적었다 (세션 번호 묻기·메모리 복사).
- 이번 바퀴엔 디버거를 안 붙였다. 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

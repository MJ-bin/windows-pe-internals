# 43 kprocess-token-htable — 정적 분석 결과 (Q78c1)

- 요청서: `_workspace/sast/43_kprocess-token-htable.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`, 기준과 같음).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 다 있음).
- 새로 뽑은 함수 **35개, 전부 depth 0** (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`). 요청서 표는 depth 1 이었지만 depth 1 이면 `memset`·락·그룹 마스크 도우미까지 따라와서, **지도로 필요한 것만 골라 depth 0 으로 네 번에 나눠** 뽑았다.
  - 요청서 표 15개: `KeInitializeProcess`, `PspInitializeProcessSecurity`, `SeSubProcessToken`, `SepDuplicateToken`, `ObInitProcess`, `ExCreateHandleTable`, `ExDupHandleTable`, `SeAssignPrimaryToken`, `KeSetCpuSetsProcess`, `KeSetExpectedConcurrencyCountProcess`, `PspSetProcessPriorityClass`, `KeSecureProcess`, `PspSetProcessAffinitySafe`, `KeInitializeProcessUserCetLogging`, `PspDisablePrimaryTokenExchange`.
  - 넓힌 것 (**요청서 표에 없던 것 — 칸을 실제로 쓰는 함수라 넣음**): 핸들 표 `ExpAllocateHandleTable` (**`QuotaProcess`·`UniqueProcessId` 처음 값·`Flags`·`FreeLists` 를 쓰는 함수**), `ExpAllocateHandleTableEntrySlow`, `ObReferenceProcessHandleTable`, `ExpAllocateLowLevelTable`, `ExpInsertLowLevelTableIntoFreeList` (**`NextHandleNeedingPool` 을 쓰는 함수**). `_KPROCESS` 쪽 `KeSetGroupMaskProcess`, `KiSetIdealNodeProcessByGroup`, `KiInitializeProcessAvailableCpuState`, `KiInitializeIdealProcessorAssignmentBlock`, `KiStartIdealProcessorAssignmentBlock`, `KiComputeProcessAutoBoostTreeCount`, `PspComputeQuantumAndPriority`, `KeSelectNodeForAffinity`, **`KeStartThread`** (K2~K3 에 `StackCount`·`ProcessListEntry`·`ThreadListHead` 를 쓰는 함수), `KeSetProcessPpmPolicy`·`PspSetProcessPpmPolicy` (**`PpmPolicy` 를 쓰는 함수**). 토큰 쪽 `PspReferenceTokenForNewProcess` (**입력 토큰이 어디서 오나**), `PsReferencePrimaryTokenWithTag`, `SepSetTokenSessionById` (**`SessionId`·`SessionObject` 를 쓰는 함수**), `SepSetTrustLevelForProcessToken`.
  - 이미 `.c` 가 있어서 **읽기만 한 것**: `PspAllocateProcess`, `PspInsertProcess`, `ObInitializeFastReference`, `MmCreateProcessAddressSpace`, `SepMandatorySubProcessToken`, `NtCreateUserProcess`, `PspInsertThread`, `PspUserThreadStartup`. `_calls.tsv` 는 export 전 줄이 하나도 안 사라졌다 (`comm -23` 0줄, 3,284 → 3,598줄).
  - **Ghidra 로 안 뽑고 `objdump -d` 로만 본 곳** (함수+오프셋으로만 적는다, `.asm` 근거 파일은 없음): `MiCreateNewProcessTopLevelMappings`, `MiAllocateProcessShadow`, `PspComputeQuantum`, `ObpAllocateObject`, `ExAllocatePoolWithTag` (할당 때 0 으로 채우나), `PspInitPhase1 +0xb3`.
- `refs` 1번: `PspBamHostInterface` (0x28바이트) → `ms/disassemble/ntoskrnl/_refs/PspBamHostInterface.tsv`.
- 구조체 칸 표: **`pdb_struct.py` 로 뽑았다** (`_KPROCESS`·`_HANDLE_TABLE`·`_TOKEN` 직접 칸 수 65·17·55 = 요청서와 같음; `_KSTACK_COUNT`·`_HANDLE_TABLE_FREE_LIST`·`_KNODE`·`_KAFFINITY_EX`·`_EJOB` 등도 같은 도구). 사진 값은 `ms/logs/04_baggage-read_K1.txt` (`K1 01c EPROCESS_RAW`, `K1 04 HANDLE_TABLE`), `04_baggage-read_supp_K1.txt` (`K1 05s TOKEN` dt+dq), 지점별 EPROCESS raw 8장 비교.
- 버전 확인: **일치.**
  - `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess`. `PspInsertThread` call `+0x802` / ret **`+0x807`** → `PspCallProcessNotifyRoutines` (`_calls.tsv`).
  - 코드로 계산한 자리와 사진 K1 값이 칸마다 맞는다: `KeInitializeProcess` 가 `KiMaximumGroups` 로 깔아 놓은 확장 메모리 배치 (아래 1.2 표)가 K1 의 `ActiveProcessors`·`Affinity`·`AvailableCpuState`·`IdealProcessorAssignmentBlock`·`AutoBoostState` 포인터 5개와 **전부 정확히 일치**. `ObInitProcess` 가 부모 길이면 `Flags.Duplicated` 가 켜지는데 K1 `Flags`=`0x18`. `SepDuplicateToken` 이 새 토큰 풀 블록 3개 (`SeTl`·`SeAt`·`SeDT`)를 따로 받는데 K1 의 `TokenLock`·`pSecurityAttributes`·`DynamicPart` 가 서로 다른 풀 주소다. `PspPriorityTable[2]`=8 (PE 파일에서 직접 읽음) = K1 `BasePriority` 8. 바이너리와 타깃 VM 이 같다고 볼 수 있다.

## 스크립트 문제

없다. 아래 "도구 평가" 에 **없어서 불편했던 것** 두 가지를 하네스 제안으로 적었다.

## 한눈에

- **토큰은 부모 것을 "복제"한다 — 같은 객체를 공유하지 않는다.** `PspReferenceTokenForNewProcess` (`NtCreateUserProcess +0x417`) 가 부모의 주 토큰을 참조로 집어 오고, `SeSubProcessToken +0x43c` → `SepDuplicateToken` 이 **새 `_TOKEN` 객체를 `ObCreateObjectEx` (`+0x158`) 로 만들어** 칸을 복사한다 (AppContainer 속성이 없으면). 새 `TokenId` 는 전역 LUID 카운터로 새로 정한다 (K1 `0x24c39e`). 그 뒤 `ObInitializeFastReference` 가 `EPROCESS.Token` 에 "토큰 주소 | 0xf" 를 넣는다. 사진 8장이 모두 같은 주소인 건 **이 프로세스 안에서** 토큰이 안 바뀐다는 뜻이고, 부모 cmd 토큰과 같다는 뜻이 아니다.
- **핸들 표는 "부모 표 복제" 길 (`ExDupHandleTable`) 이다.** 가르는 조건은 `NtCreateUserProcess` 의 `ProcessFlags` 비트 2 (상속). K1 의 `Flags`=`0x18` 에 `Duplicated` (bit 3) 가 켜져 있고, 이 비트를 켜는 줄은 `ExDupHandleTable +0xa6` 하나뿐이라 **사진 + 코드로 확정**. 핸들 수 4 = 부모 cmd 표에서 상속 비트가 켜진 항목 4개 (`ExDupHandleTable +0x3b1 INC`).
- **`HANDLE_TABLE.UniqueProcessId` 의 쓰는 줄 두 곳을 다 찾았다.** 처음 값 (부모 PID) = `ExpAllocateHandleTable +0x7c` 가 **지금 도는 스레드의 프로세스 (cmd)** 의 PID 를 읽어 넣음. 자기 PID 로 바꾸는 줄 = **`PspInsertProcess +0x4c`** (`MOV [ObjectTable+0x28], EPROCESS.UniqueProcessId`). baggage 7.8 의 `[공백]` 닫힘.
- **`_KPROCESS` 의 몸통은 `KeInitializeProcess` 한 함수가 쓰고, 값이 정해진 뒤 덮는 곳이 따로 있다**: `BasePriority`·`QuantumReset` 은 `PspAllocateProcess +0x1a64 · +0x1a72`, `DirectoryTableBase` 는 `MiCreateNewProcessTopLevelMappings +0x6e`, K2~K3 에 바뀌는 `StackCount`·`ProcessListEntry`·`ThreadListHead` 는 **`KeStartThread`** (`PspInsertThread +0x5dd`), K3~P0 의 `ProcessFlags.PpmPolicy` 는 **`KeSetProcessPpmPolicy +0x43`** (부르는 쪽은 BAM 확장 `[추측]`).
- **`PspDisablePrimaryTokenExchange` 는 토큰 칸을 안 쓴다 (아니오).** 토큰은 읽기만 (`PsReferencePrimaryTokenWithTag`, `SeTokenIsAdmin`) 하고, 쓰는 건 `EPROCESS.Flags2.PrimaryTokenFrozen` 등 EPROCESS 쪽 (Q78c2).

## 요약 표 (구조체별 모르는 칸 137 → ?)

분류: **아는 칸(nt)** = 사진에서 값이 있거나 바뀐 칸이고 nt 쓰는 줄을 찾음 / **0 그대로 · 줄 있음** = 사진 값 0 이고 nt 쓰는 줄은 있음 (0 을 씀 · 조건부 안 탐 · 썼다 되돌림) / **아무도 안 씀** = 이 조각 (만들기 경로 + 호출한 함수) 에 쓰는 줄이 없고 할당 때 `memset`·풀 0 그대로 / **여전히 모름**.

| 구조체 | 모르는 칸 | 아는 칸(nt) | 0 그대로 · 줄 있음 | 아무도 안 씀 | 여전히 모름 | (참고) 부모 토큰에서 복사 |
|---|---|---|---|---|---|---|
| `_KPROCESS` | 65 | 19 | 10 (0 을 씀 3 · 조건부 6 · 되돌림 1) | 36 | 0 | — |
| `_HANDLE_TABLE` | 17 | 10 | 3 (0 을 씀 2 · 조건부 1) | 4 | 0 | — (핸들 항목 안은 범위 밖) |
| `_TOKEN` | 55 | 29 | 26 (0 을 씀 15 · 값 0 인 복사/계산 4 · 조건부 7) | 0 | 0 | 16 (+ 값 0 인 복사 3) |
| **합** | **137** | **58** | **39** | **40** | **0** | |

- run47 방식으로 합치면 baggage 2장에서는 "아는 칸" 에 `58 + 39 = 97`, "아무도 안 씀" 에 `40`. 합치는 건 flow-writer 가 정한다 (run47 도 "0 그대로" 33 중 쓰는 줄이 있는 22 는 아는 칸으로, 줄이 없는 11 은 "아무도 안 씀" 으로 셌다 — baggage 2장 아래 줄 숫자에서 거꾸로 계산).
- 아는 칸(nt) 중 **줄은 알아도 부르는 쪽/값이 정해진 조건은 동적**인 것: `PpmPolicy`·`ProcessFlags` (부르는 쪽 BAM `[추측]`), `Header.WaitListHead` (대기 함수 이름 `[추측]`), `TokenFlags` (여러 단계가 켜고 끔 — 최종 `0x2a00` 의 단계별 분해는 못 함), `Affinity`·`ActiveGroupsMask` (이미지 `ProcessAffinityMask` 가 있으면 `PspSetProcessAffinitySafe` 가 다시 씀 — 이 실행은 아님 `[관찰 간접]`).
- "아무도 안 씀" 40 은 **런타임에 다른 시스템 콜·스케줄러가 쓸 수 있는 칸** (`CycleTime`·`KernelTime`·`DisableBoost`·`ForegroundProcess`·`InstrumentationCallback` 등)이 섞여 있다 `[추측]`. 사진 8장에선 모두 0 이라 "초기화 동안 아무도 안 쓴다" 까지만 말할 수 있다.

## 질문별 답 [정적]

### 1. `_KPROCESS` 65칸

**답: 칸 대부분 (15 + 자기 연결 목록 등) 은 `PspAllocateProcess +0x1719` (ret `+0x171e`) 가 부르는 `KeInitializeProcess` 가 쓴다. 이 함수는 먼저 `PspAllocateProcess +0x55f` 의 `memset` 이 0 으로 만든 객체 위에 쓴다. 값이 나중에 다른 함수로 덮이는 칸 (`BasePriority`·`QuantumReset`·`DirectoryTableBase`) 과 K2 이후에 바뀌는 칸 5개가 따로 있다.**

#### 1.1 호출 사슬과 인자

- 경로: `NtCreateUserProcess +0xc31 (ret +0xc36) → PspAllocateProcess +0x1719 (ret +0x171e) → KeInitializeProcess`. `PspAllocateProcess` 는 `+0x534 ObCreateObjectEx` 로 객체를 받고 **`+0x55f (ret +0x564) memset(…, 0, 크기)`** 로 몸통 + 확장 메모리를 한꺼번에 0 으로 민다 (크기 `R12D` = 객체 몸통 + `KeSizeOfKProcessExtension` 결과, `+0x4ea` ret `+0x4ef`·`+0x4f7 ADD`). → 아래 "0 그대로" 의 기준선.
- `KeInitializeProcess` 인자 (`PspAllocateProcess +0x16df ~ +0x1719`, `KeInitializeProcess +0x25 ~ +0x44` 와 맞춤):

| 인자 | 값 | 어디서 | 쓰이는 칸 |
|---|---|---|---|
| 1 `RCX` | 새 프로세스 객체 | `+0x16df MOV RCX,R14` | — |
| 2 `EDX` | `8` (상수) | `+0x1714 MOV EDX,8` | `BasePriority` (`KeInitializeProcess +0x6e MOV [RDI+0x90],DL`) |
| 3 `R8W` | 주 그룹 번호 | `[RSP+0x8c]` (`+0x12cb`·`+0x132b`·`+0x1305` 에서 `KeSelectGroupFromNode` 결과나 0) | `PrimaryGroup` (`+0x196`) |
| 4 `R9` | 친화도 (`_KAFFINITY_EX`) | `LEA R9,[RSP+0x490]` — PAP 가 `+0x1317 KeAddProcessorAffinityEx`·`+0x134d KiCopyAffinityEx`·`+0x13c7 KeQueryAffinityProcess` 로 만든 입력 | `Affinity` 내용, `ActiveGroupsMask`, `AvailableCpuState` 쪽 사본 |
| 5 `[RBP+0x60]` | 노드 (`_KNODE*`) | `[RSP+0xb8]` ← `+0x12b3 PspSelectNodeForProcess` · `+0x1294~+0x12a6` 부모 `IdealGlobalNode`로 `KeNodeBlock` 색인 · `+0x139d KeSelectNodeForAffinity` 중 하나. 부르는 쪽이 `_KNODE.NodeNumber` (+0x0, `pdb_struct`) 를 읽는다 | `IdealGlobalNode` (`+0x1dc MOV [RDI+0x104],CX`) |
| 6 `[RBP+0x68]` | 스케줄링 그룹 | `+0x16e2 PsGetSessionSchedulingGroupByProcess` 반환 | `SchedulingGroup` (`+0x2ac`) |
| 7 `[RBP+0x70]` | (안 씀) | — | — |
| 8 `[RBP+0x78]` | 플래그 | `[RSP+0xf8]` (부모가 있고 `EBX & 0x10002`==0 이면 `4`: `+0x4cb~+0x4db`) `\| 1` (`EPROCESS+0x388` bit 2: `+0x16b0~+0x16c2`) `\| 2` (`EBX` bit 16: `+0x16d7 BT EBX,0x10`) | bit0 `ProcessFlags \|= 1` (`+0x74`·`+0x410`), bit1 `VslCreateSecureProcess` 갈래 (`+0x358`·`+0x44d`), bit2 `AutoBoostState` 갈래 (`+0x378`·`+0x38c`) |
| 9 `[RBP+0x80]` | 확장 메모리 시작 | `+0x16cd ADD R9,R14` (`[RSP+0x168]` + 프로세스 주소) | `ActiveProcessors` 등 아래 1.2 |

- `EBX` bit 16 의 출처: `PspAllocateProcess +0x3e7 BTS EBX,0x10` — 속성 모음 (`R15` = `NtCreateUserProcess` 의 `[RBP+0x1a0]`) 의 `[R15+8]` bit 18 이 켜졌을 때. test.exe 는 `SecureState`=0 이라 아님 `[관찰 간접]`.

#### 1.2 확장 메모리 배치 (아홉째 인자 이후)

`KiMaximumGroups` (`+0x2c`) 가 1 이라 `_KAFFINITY_EX` 하나가 0x10 바이트다 (`RBX = 그룹수*8+8`, `+0x5a`). K1 값 (프로세스 `…b080` 기준 차이) 과 대조했다.

| 위치 (프로세스 + ) | 무엇 | 쓰는 줄 (`KeInitializeProcess`) | K1 |
|---|---|---|---|
| `+0xa60` | `ActiveProcessors` 가 가리키는 `_KAFFINITY_EX` | 포인터 `+0x7d`, `Count`·`Size` `+0x88~+0x97`, 내용 `memset` `+0x9b` | `…bae0` ✓ |
| `+0xa70` | `Affinity` 가 가리키는 `_KAFFINITY_EX` | 포인터 `+0xa0`, 내용 `memset` `+0xcb`, **`KiCopyAffinityEx` `+0xdb` 로 인자 4 를 복사** | `…baf0` ✓ |
| `+0xa80` | `AvailableCpuState` (0x78바이트) | 포인터 `+0xea`, 블록 안 `KiInitializeProcessAvailableCpuState +0x12e` | `…bb00` ✓ |
| `+0xaf8` | 위 블록이 쓰는 `_KAFFINITY_EX` 사본 | `+0x10d memset`, `+0x11c KiCopyAffinityEx` | — |
| `+0xb08` | `IdealProcessorAssignmentBlock` (0x1d8바이트) | 포인터 `+0x136`, 블록 안 `KiInitializeIdealProcessorAssignmentBlock +0x148` (`memset` + 초기값), `KiSetIdealNodeProcessByGroup +0x25a`, `KiStartIdealProcessorAssignmentBlock +0x3c0` | `…bb88` ✓ |
| `+0xce0` | `AutoBoostState` 저장소 (트리마다 24바이트, 트리 수 = `KiComputeProcessAutoBoostTreeCount` = `(KeMaximumProcessors-1)/8+1`) | 포인터 `+0x3af`, 개수 `+0x3b3`, 0 채움 `+0x39b~+0x3a3` | `…bd60`, 개수 `1` ✓ |

- 이 블록들 **안**의 칸은 `_KPROCESS` 의 칸이 아니다 (이름만; 칸 표 없는 짐 — 7.6 에 올릴 후보: `_KPROCESS_AVAILABLE_CPU_STATE` 0x78, `_KI_IDEAL_PROCESSOR_ASSIGNMENT_BLOCK` 0x1d8, `_KAB_UM_PROCESS_TREE`).
- `PerProcessorCycleTimes` 값 `0x9300` 은 포인터가 아니다: `ExSaAllocate(ECX=0x10)` (`+0x2c6`) 가 돌려준 "슬랩 핸들" 로 보인다 (`+0x2eb~+0x335` 가 그 값에서 페이지 번호·칸 번호를 비트로 꺼내 쓴다) `[추측]`.

#### 1.3 칸마다 쓰는 줄 — 아는 칸 19

| 칸 | 값의 출처 | 쓰는 줄 | 사진 |
|---|---|---|---|
| `Header` +0x0 (Type) | 상수 `3` (프로세스 객체) | `KeInitializeProcess +0x48 MOV byte [RDI],3` | `K1 이전` |
| `Header.WaitListHead` +0x8 | 자기 자신을 가리킴 (주소 계산) | `+0x44 LEA RAX,[RDI+8]`, `+0x4b`, `+0x57` | `K1 이전` → **`K3~P0` 바뀜** (아래 1.5) |
| `ProfileListHead` +0x18 | 자기 자신 | `+0x19e~+0x1a6` | `K1 이전` |
| `DirectoryTableBase` +0x28 | **계산**: `MiAllocateTopLevelPage` 가 돌려준 쪽 번호 `<< 12`, `KiFlushPcid` 가 켜져 있으면 `\| 2` | **`MiCreateNewProcessTopLevelMappings +0x6e MOV [RDI+0x28],RBX`** (`+0x57 SHL RBX,0xc`, `+0x5b CMP [KiFlushPcid]`, `+0x9d OR RBX,2`). 부르는 쪽 `MmCreateProcessAddressSpace +0x1f8 (ret +0x1fd) MiAllocateTopLevelPage` → `+0x203 (ret +0x208) MiCreateNewProcessTopLevelMappings`, 그걸 `PspAllocateProcess +0x1637 (ret +0x163c)` 이 부름 (`MmCreateProcessAddressSpace` 는 sast/04 에 이미 있음) | `K1 이전` — K1 `0x192c2e000` (bit 1 꺼짐 = `KiFlushPcid` 0 `[관찰 간접]`) |
| `ThreadListHead` +0x30 | 자기 자신 → 첫 스레드 항목 | 처음 `KeInitializeProcess +0x1b4~+0x1bc`. K2~K3 에 스레드 꼬리 달기 **`KeStartThread +0x2f4 ~ +0x2fe`** (Blink `+0x2fe`) ← `PspInsertThread +0x5dd (ret +0x5e2)` | `K1 이전` → **`K2~K3`·`P1a~P1b`** (둘째 스레드) |
| `Affinity` +0x50 | 포인터 = 확장 메모리, 내용 = 인자 4 복사 | `+0xa0`, `+0xdb` | `K1 이전` |
| `AutoBoostState` +0x58 (+0x60) | 저장소 포인터, 트리 수 | `+0x3af`, `+0x3b3` (조건: 인자 8 의 bit 2 = "부모가 있는 일반 프로세스", `PspAllocateProcess +0x4cb~+0x4db`) | `K1 이전` (K1 raw `+0x60`=1 ✓) |
| `ReadyListHead` +0x68 | 자기 자신 | `+0x1a9~+0x1b1` | `K1 이전` |
| `ActiveProcessors` +0x80 | 포인터 = 인자 9 | `+0x7d MOV [RDI+0x80],R9` | `K1 이전` |
| `BasePriority` +0x90 | 처음 상수 `8`, **나중에 계산값으로 덮음**: `PspComputeQuantumAndPriority` 가 `PspPriorityTable[EPROCESS.PriorityClass]` 를 돌려줌 (`PriorityClass`=2 → 8; 표는 `[8,4,8,13,24,6,10]` 를 PE 에서 직접 읽음) | `KeInitializeProcess +0x6e`; **`PspAllocateProcess +0x1a64 MOV [R14+0x90],AL`** (ret `+0x1a64` ← `+0x1a5f` 호출) | `K1 이전` |
| `QuantumReset` +0x91 | 처음 상수 `6`, **나중에 계산값으로 덮음**: `PspComputeQuantum` (표 `PspForegroundQuantum`, 색인은 `PsPrioritySeparation` 과 전경 여부) | `KeInitializeProcess +0x1bf`; **`PspAllocateProcess +0x1a72 MOV [R14+0x91],AL`** (`[RSP+0x94]` ← `PspComputeQuantumAndPriority` 출력) | `K1 이전` — K1 값 `2` (표의 런타임 값, PE 안은 0) |
| `ActiveGroupsMask` +0x98 | 계산: 인자 4 의 그룹별 비트를 모음 | `KeInitializeProcess +0x15b~+0x181` 가 `KeSetBitGroupMask +0x173` 로 만들고 **`KeSetGroupMaskProcess +0x3 MOVDQU [RCX+0x98]`** (16바이트 통째) ← `+0x191 (ret +0x196)` | `K1 이전` |
| `IdealProcessorAssignmentBlock` +0xb8 | 포인터 = 확장 메모리 | `+0x136` | `K1 이전` |
| `PerProcessorCycleTimes` +0x190 | `ExSaAllocate` 결과 (실패하면 0) | `+0x351 MOV [RDI+0x190],R14`. 조건: `KiCacheAwareScheduling` bit 3 (`+0x2b3~+0x2bb`) — K1 값이 있으니 켜져 있었다 | `K1 이전` |
| `CpuPartitionList` +0x1b0 | 자기 자신 | `+0x37c~+0x387` | `K1 이전` |
| `AvailableCpuState` +0x1c0 | 포인터 = 확장 메모리 | `+0xea` | `K1 이전` |
| `StackCount` +0x108 | 처음 `0`(상수, `R8D`), K2~K3 에 `+8` (`_KSTACK_COUNT.StackCount` 3:29 비트 +1 = 스레드 하나) | `KeInitializeProcess +0x1c6`; **`KeStartThread +0x3e9 ADD.LOCK [RDI+0x108],8`** | **`K2~K3`** (raw `0 → 8`), `P1a~P1b` (`8 → 0x18`) |
| `ProcessListEntry` +0x110 | 첫 스레드이고 `KiInitialProcess` 가 아닐 때 전역 목록 `KiProcessListHead` 꼬리에 달기 (큐 스핀락 `KiProcessListLock` 안) | **`KeStartThread +0x247 ~ +0x270`** (조건 `+0x19c CMP [RSI],RSI`=`ThreadListHead` 빔, `+0x221 CMP RDI,KiInitialProcess`) | **`K2~K3`** (raw `0 → fffff805'b3b224c0` = 목록 머리) |
| `ProcessFlags.PpmPolicy` 7:4 + `ProcessFlags` +0x88 | 전화 정책 값 (**관찰 `7`** → `0x380`): `(ProcessFlags & 0xfffff87f) \| (정책<<7)` 를 `CMPXCHG` | **`KeSetProcessPpmPolicy +0x43 AND ECX,0xfffff87f` · `+0x4b CMPXCHG.LOCK [R14+0x88]`** (PE 전체에서 이 마스크로 쓰는 곳 이 한 곳뿐 `[정적 스캔]`). 부르는 쪽 `PspSetProcessPpmPolicy +0x2b` (ret `+0x30`)·`+0xbe`, 그것은 함수 표 `PspBamHostInterface +0x8` 로만 가리켜짐 (`refs`/PE 스캔) → 표는 `PspInitPhase1 +0xb3` 이 `ExRegisterHost(PspBamExtensionHost, …)` 에 넘김 → **부르는 쪽은 nt 밖의 BAM 확장 (bam.sys) `[추측]`** | **`K3~P0`** |

- `ProcessFlags` 한 칸은 이 둘 (`PpmPolicy` 비트 칸과 통째 `ProcessFlags`) 이 같은 4바이트라 둘 다 이 줄로 닫힌다.
- 같은 함수 `PspSetProcessPpmPolicy +0xa5` 가 `EPROCESS+0x820` (`BamPpmPolicy`) 도 쓴다 — baggage 7.2 `_EPROCESS` 의 `BamPpmPolicy` `K3~P0` 와 **같은 구간**. → Q78c2 로 넘김.

#### 1.4 `PspAllocateProcess` 안 다른 호출이 `_KPROCESS` 를 쓰나

| 호출 (call / ret) | 막는 조건 | test.exe | 쓰는 `_KPROCESS` 칸 |
|---|---|---|---|
| `+0x1a00 / +0x1a05 KeSetCpuSetsProcess` | `EBX` bit 21 (`+0xdcc BTS EBX,0x15`) = CPU 세트 속성 | 아님 `[추측]` (`EPROCESS.IndirectCpuSets`=0, 사진) | 직접 칸은 `ProcessLock` 잡았다 놓기만 (`+0x44`·`+0xab`). 마스크는 EPROCESS 쪽 `KiGetProcessCpuSetMaskPointer` (Q78c2) |
| `+0x1a19 / +0x1a1e KeSetExpectedConcurrencyCountProcess` | IFEO 성능 옵션 (`[RSP+0x43c]` bit 1) | 아님 `[추측]` | `ProcessLock` 만 (`+0x46`·`+0x64`). 나머지는 `IdealProcessorAssignmentBlock+0xc \|= 1` (`+0x60`, 블록 안) |
| `+0x1a3b / +0x1a40 PspSetProcessPriorityClass` | 속성 모음 `[R15+8]` bit 8 | 아님 `[추측]` | **없음.** `EPROCESS.PriorityClass` (+0x347) 만 씀 (`+0x72`). `BasePriority` 는 위 `+0x1a64` 가 이어서 계산 |
| `+0x279b / +0x27a0 KeSecureProcess` | `EBX` bit 16 (`+0x2728 BT EBX,0x10`) | 아님 (`SecureState`=0, 사진) | `SecureState` (+0x170): `VslInitializeSecureProcess` 뒤 `+0xa7 OR [R14+0x170],2` (TrustedApp) |
| `+0x235f / +0x2364 PspSetProcessAffinitySafe` | `MmCreatePeb` 출력 `[RSP+0x210]`≠0 (이미지 로드 구성의 `ProcessAffinityMask`; `+0x2323 CMP` → `JZ +0x232b`) | 아님 `[추측]` (test.exe 로드 구성 확인은 PE 헤더 읽기로 가능) | `KeSetAffinityProcess` (+0xf9) 가 `Affinity` 내용·`ActiveGroupsMask` 를 다시 씀 |
| `+0x2008 / +0x200d KeInitializeProcessUserCetLogging` | `EPROCESS.MitigationFlags2 (+0x754) & 0x4000` | **아님** (K1 raw `+0x754`=`0x40000000`) | `UserCetLogging` (+0x1a8) ← 새 풀 0xc0바이트 (`+0x57`) |
| `+0x12b3 PspSelectNodeForProcess`, `+0x12c3/+0x1326 KeSelectGroupFromNode`, `+0x139d KeSelectNodeForAffinity` | — | 탐 | **칸을 안 쓴다.** 인자 3·5 (`PrimaryGroup`·`IdealGlobalNode`) 값만 정함 |
| `+0x2689 PsReferencePrimaryTokenWithTag`, `+0x26ac SeSetTokenTamperProtection`, `+0x264a SeConstructAntiTamperPolicy` | `Feature_OutsideInProtection` 와 IFEO 안티템퍼 정책 | 아님 (토큰 `AntiTamper*` 0, 사진) | `_KPROCESS` 아님 — 토큰 `AntiTamper*` (아래 2장) |
| `PspAllocateProcess +0x2216 OR [R14+0x88],R13D` | `EPROCESS.Machine (+0x6ec)==0x14c` (WoW64 x86) | **아님** (K1 raw `+0x6ec`=`0x8664`) | `ProcessFlags.AutoAlignment` |
| `PspAllocateProcess +0x1686 MOV [R14+0x160],R13B` | 부모 없는 "손수 만든 프로세스" 갈래 (`+0x1416 JZ +0x167c`) | 아님 | `AddressPolicy` (+0x160)=1 |

#### 1.5 사진에서 뒤에 바뀐 칸 (정적 이름)

K1 ~ P2 여덟 장 raw 를 칸마다 비교했다 (`_KPROCESS` 0x0~0x1c8). **바뀐 칸은 아래뿐**, 나머지는 여덟 장 모두 같다.

| 칸 | 바뀐 구간 (값) | 쓰는 함수 |
|---|---|---|
| `Header.WaitListHead` (+0x8, +0x10) | `K3~P0` (`…b088` 자기 자신 → `ffff8285'98adc1c0`) | 누군가 이 프로세스 객체를 기다림 → 대기 블록이 달림 (`KeWait…` 계열 `[추측]`; 대기자는 부모 cmd 스레드 `[추측]`). **줄기 밖.** 값의 주인은 P0 덤프에서 `dt nt!_KWAIT_BLOCK ffff828598adc1c0` 로 (D4) |
| `ThreadListHead` (+0x30, +0x38) | `K2~K3` Flink·Blink 둘 다 (첫 스레드 항목 `…5378`), `P1a~P1b` Blink 만 (둘째 스레드 `…2378`) | `KeStartThread +0x2f4~+0x2fe` (둘 다 같은 줄; 둘째는 ntdll 이 만드는 일꾼 스레드 `[추측]` — Q78d) |
| `StackCount` (+0x108) | `K2~K3` `0→8`, `P1a~P1b` `8→0x18` | `KeStartThread +0x3e9` |
| `ProcessListEntry` (+0x110) | `K2~K3` | `KeStartThread +0x247~+0x270` |
| `ProcessFlags` (+0x88) | `K3~P0` `0→0x380` | `KeSetProcessPpmPolicy +0x43/+0x4b` |

- `StackCount` 의 `P1a~P1b` 변화량이 `+0x10` (= 두 스레드) 인 것은 `KeStartThread` 가 두 번 불렸다는 뜻과 맞다. 정확히 몇 번째 스레드인지는 Q78d.

#### 1.6 끝까지 0 인 46칸 — 세 가지로 가름

| 가름 | 칸 | 근거 |
|---|---|---|
| **0 을 씀** (3) | `SchedulingGroup` (인자 6=널), `PrimaryGroup` (인자 3=0), `IdealGlobalNode` (인자 5 의 `NodeNumber`=0) | `KeInitializeProcess +0x2ac`, `+0x196`, `+0x1dc` |
| **조건부 안 탐** (6) | `AutoAlignment` (`ProcessFlags` bit 0) · `ExtendedFeatureDisableMask` · `SecureState` · `AddressPolicy` · `UserCetLogging` · `UserDirectoryTableBase` | 아래 표 |
| **썼다 되돌림** (1) | `ProcessLock` | `KeStartThread +0x99 KiAcquireProcessLockExclusive` / `+0x38d KiReleaseProcessLockExclusive` 등 (`KeSetProcessPpmPolicy +0x32/+0x90` 도 같은 락). 스핀락 칸이라 잡았다 놓으면 0 으로 돌아옴 (사진은 놓은 뒤) |
| **아무도 안 씀** (36) | `ProcessTimerDelay`, `DeepFreezeStartTime`, `SwapListEntry`, `DisableBoost`, `DisableQuantum`, `DeepFreeze`, `TimerVirtualization`, `CheckStackExtents`, `CacheIsolationEnabled`, `VaSpaceDeleted`, `MultiGroup`, `ForegroundProcess`, `ReservedFlags`, `Spare0c`, `Visited`, `Flags` (`_KEXECUTE_OPTIONS`), `ActiveGroupPadding`, `Padding`, `Padding2`, `SchedulerAssistYieldBoostCount`, `SchedulerAssistYieldBoostAllowedTime`, `Spare0d`, `Spare1`, `CycleTime`, `ContextSwitches`, `KernelTime`, `UserTime`, `ReadyTime`, `FreezeCount`, `Spare4`, `Spare2`, `InstrumentationCallback`, `KernelWaitTime`, `UserWaitTime`, `LastRebalanceQpc`, `Spare3` | 만들기 경로 (`KeInitializeProcess`·`PspAllocateProcess` 의 `[R14+0x0~0x1c7]` 쓰기 전수) 와 호출한 함수에 쓰는 줄 없음. `memset` (`PspAllocateProcess +0x55f`) 0 그대로 |

조건부 안 탐 여섯:

| 칸 | 쓰는 줄 | 막는 조건 | test.exe |
|---|---|---|---|
| `ProcessFlags.AutoAlignment` | `KeInitializeProcess +0x74 TEST [RBP+0x78],1` → `+0x410 OR [RDI+0x88],EAX` | 인자 8 bit 0 = `EPROCESS.DefaultHardErrorProcessing (+0x388)` bit 2 (`PspAllocateProcess +0x16b0~+0x16c2`) 또는 WoW64 x86 (`+0x2216`) | `+0x388`=1 → bit 2 꺼짐, `Machine`=`0x8664` — 안 탐 `[관찰]` |
| `ExtendedFeatureDisableMask` | `KeInitializeProcess +0x47b MOV RAX,[0xfffff78000000710]` → `+0x485 MOV [RDI+0x198],RAX` | `KeFeatureBits` bit 0x37 (`+0x369 BT`) | 안 탐 또는 KUSD `+0x710`=0 — **갈림은 못 가름** (D5) |
| `SecureState` | `+0x45c VslCreateSecureProcess` → `+0x473 OR [RBX],R9`; `KeSecureProcess +0xa7` | 인자 8 bit 1 / `EBX` bit 16 | 안 탐 |
| `AddressPolicy` | `PspAllocateProcess +0x1686`; `PspDisablePrimaryTokenExchange +0xfd` | 손수 만든 프로세스 갈래 / `KeKvaShadowingActive` | 안 탐 |
| `UserCetLogging` | `KeInitializeProcessUserCetLogging +0x57` | `MitigationFlags2 & 0x4000` | 안 탐 `[관찰]` |
| `UserDirectoryTableBase` | `MiAllocateProcessShadow +0xaa MOV [RSI+0x158],RAX` (`MmCreateProcessAddressSpace +0x1c6` 이 부름) | `MiFlags & 0x600000` (`MiAllocateProcessShadow +0x14`) | 안 탐 (= 커널 주소 격리 꺼짐 `[추측]`) |

---

### 2. `_TOKEN` 55칸

**답: 복제다. 공유가 아니다.**

#### 2.1 사슬과 갈림

```
NtCreateUserProcess +0x417 (ret +0x41c)  PspReferenceTokenForNewProcess   ← 입력 토큰 고르기
PspAllocateProcess  +0x1922 (ret +0x1927) PspInitializeProcessSecurity
  PspInitializeProcessSecurity +0xf7 (ret +0xfc) SeSubProcessToken
    SeSubProcessToken +0x43c (ret +0x441)  SepDuplicateToken             ← 새 토큰 객체 + 칸 복사
    SeSubProcessToken +0x4f5.. ~ +0x86c    세션·플래그·무결성 등 마무리
  PspInitializeProcessSecurity +0x110 (ret +0x115) ObInitializeFastReference → EPROCESS.Token
```

- **입력 토큰** = `PspReferenceTokenForNewProcess(RCX=부모, RDX=토큰 핸들 속성, R9=&출력)` (`+0x0 ~ +0x7d`): 핸들이 오면 `ObReferenceObjectByHandle(…, SeTokenObjectType)` (`+0x3b`), 없으면 **`PsReferencePrimaryTokenWithTag(부모, 'Dflt')`** (`+0x64`), 부모도 없으면 `PspBootAccessToken` (`+0x6e`). cmd 가 `CreateProcess` 로 띄운 test.exe 는 핸들 속성이 없으면 둘째 갈래 = **부모 cmd 의 `EPROCESS.Token`** `[정적 + 추측: 핸들 속성이 없다는 점은 동적 D1]`.
- `SeSubProcessToken` 안에서 복제 길로 가려면 (모두 test.exe 가 이쪽 `[정적 조건 + 관찰 간접]`):
  1. `+0x117 SeTokenGetNoChildProcessRestricted` 결과 두 바이트가 0 (자식 금지 제한 없음) → `+0x12b JZ +0x25e`.
  2. `Feature_DecoupleAppContainerAndIntegrityLevel` 가 켜져 있고 **`[R15+0x20]` (AppContainer 속성)이 널이 아닐 때만** `SepCreateAppContainerToken` (`+0x347`); 아니면 **`+0x43c SepDuplicateToken`**. 토큰 `Package` (+0x310) 가 널 (사진) = AppContainer 아님.
- **새 객체인 근거** (`SepDuplicateToken`): `+0x158 ObCreateObjectEx(SeTokenObjectType, 크기 = 원본 VariableLength + 0x5c0)` 로 새 몸통을 받고, 원본(`RDI`)과 새 토큰(`R14`/`RBX`)을 따로 다룬다. 새 `TokenId` (`+0x1a1 XADD.LOCK [ExpLuid]` = `ExAllocateLocallyUniqueId` 를 인라인한 것) — K1 `0x24c39e` (원본 쪽 LUID 들 `0x21d70~0x21d7c` 와 크게 다름). `TokenLock` 은 새 `ERESOURCE` (`+0xb3` 풀 `SeTl` + `+0x1d8 ExInitializeResourceLite`).
- 오브젝트 몸통은 **0 으로 채워져 온다**: `ObpAllocateObject +0x191 BTS ECX,0xa` (풀 종류에 "0 채움" 비트 0x400) → `ExAllocatePoolWithTag` 가 이 비트가 있으면 "초기화 안 함" 플래그 (0x2) 를 안 붙인다 (`ExAllocatePoolWithTag +0x48 BT EBX,0xa` → `CMOVB`) `[정적, objdump 로 읽음]`. 그래서 `SepDuplicateToken` 이 안 쓰는 칸은 0.
- 사진 8장 같은 토큰 주소 `ffffb689ae6b6060` — 위 새 객체 하나가 이 프로세스에 붙은 뒤 안 바뀌는 것. **부모 cmd 의 토큰과 같은 주소일 수 없다** (새 `TokenId` 가 있고 새 풀 블록 3개를 받았음) `[정적]`. 값 대조 (cmd 토큰의 `TokenId`·`ParentTokenId`·`ModifiedId` 가 칸 설명대로 복사됐는지)는 동적 D1.
- `EPROCESS.Token` 쪽은 Q78c2 로 넘기되 줄: `PspInitializeProcessSecurity +0x109~+0x110 (ret +0x115) ObInitializeFastReference(&프로세스+0x248, 새 토큰)`. 이 함수가 `ObpIncrPointerCountEx(토큰 머리, 0xf)` (`+0x1e`) 하고 **`Token = 토큰 | 0xf`** 를 씀 (`+0x23~+0x30`). K1 `EPROCESS.Token`=`…606f` ✓ (P0 `…6066`, P2 `…6063` 는 빠른 참조 카운트가 줄어든 것 — 읽기 쪽).

#### 2.2 칸마다 — 아는 칸 29 (`SepDuplicateToken` 이 `RDI`=원본 → `R14`/`RBX`=새 토큰)

| 칸 | 분류 | 쓰는 줄 | 값 |
|---|---|---|---|
| `TokenSource` +0x0 (16B) | **복사** | `SepDuplicateToken +0x1e8 MOVUPS XMM0,[RDI]` → `+0x1eb [R14]` | K1 `"User32"` (원본 토큰이 로그온 때 User32 가 만든 것) |
| `TokenId` +0x10 | **새로 정함** | `+0x1a1 XADD.LOCK [ExpLuid]` → `+0x1b1`·`+0x1bb` (`ExpLuidIncrement` 더함) | `0x24c39e` |
| `AuthenticationId` +0x18 | 복사 | `+0x1dd~+0x1e4` | `0x21d70` |
| `ParentTokenId` +0x20 | 복사 (**원본의 `ParentTokenId`**, 원본의 `TokenId` 아님) | `+0x1f7~+0x1fb` | `0x21d73` |
| `ExpirationTime` +0x28 | 복사 | `+0x1ff~+0x203` | `0x7fffffffffffffff` |
| `TokenLock` +0x30 | 새로 정함 | `+0xb3` (풀 `SeTl`), `+0x1d4 MOV [R14+0x30],R15`, `+0x1d8 ExInitializeResourceLite` | 새 `ERESOURCE` |
| `ModifiedId` +0x38 | 복사 (원본 `TokenLock` 을 공유로 잡고) | `+0x29a~+0x2a2` | `0x21d7c` |
| `Privileges` +0x40 (24B) | 복사 | `+0x34f MOVUPS`, `+0x357 MOVSD` | 5개 권한 |
| `AuditPolicy` +0x58 (32B) | 복사 | `+0x327 MOVUPS`, `+0x32f MOVUPS` (+ `+0x402 CMP [+0x77],2 → SepModifyTokenPolicyCounter`) | |
| `SessionId` +0x78 | **복사한 뒤 프로세스 세션으로 덮음** | `+0x231` (0) → `+0x31b SepSetTokenSessionById` + `+0x323 MOV [R14+0x78],EAX` (원본 값). 그 뒤 `SeSubProcessToken +0x4f5` 가 `EDI` = **새 프로세스의 세션 번호** (`PspInitializeProcessSecurity +0xa7 PsGetProcessSessionId` → 5번째 인자) 로 다시 `SepSetTokenSessionById` (`+0x503`) + `+0x50d MOV [RAX+0x78],EDI` | `1` |
| `UserAndGroupCount` +0x7c | 복사 | `SepTokenSidSharingEnabled`=0 이면 `+0x513~+0x516`, 켜지면 `SepDuplicateTokenUserAndGroups +0x4fc` | `0xf` |
| `VariableLength` +0x84 | 복사 | `+0x2ca~+0x2d0` | `0x1e0` |
| `DynamicCharged` +0x88 | 복사 | `+0x2a6~+0x2ac` | `0x1000` |
| `UserAndGroups` +0x98 | **계산** (새 토큰 가변 영역 안으로 포인터 옮김) | `+0x4e5~+0x4f2` (또는 `+0x51f~+0x523` + 항목마다 `+0x531` 더하기) | `…6620` (= 토큰+0x5c0) |
| `PrimaryGroup` +0xa8 · `DynamicPart` +0xb0 · `DefaultDacl` +0xb8 | **새로 정함 + 복사 + 옮김** | `+0x5a7 MOV [RBX+0xb0],RAX` (풀 `SeDT` `+0x59c`), 원본 동적 영역 `memcpy` `+0x5cd`, 포인터 옮김 `+0x5e0~+0x600` | K1 `…0560` (`DefaultDacl`=+0x1c) |
| `TokenType` +0xc0 | 인자 | `+0x1c6 MOV [R14+0xc0],R13D` (`SeSubProcessToken +0x419` 이 `R9D=1`) | `1` (Primary) |
| `TokenFlags` +0xc8 | 복사 (비트 0x20·0x400 제외) + **뒤에서 여러 번 켜고 끔** | `+0x2db~+0x2f3` (마스크 `0xfffffbdf` 또는 `0xeffffbdf`); `SeSubProcessToken +0x515 BTR 0x15`, `+0x529 BTS 0x13`, `+0x53d BTS 0x14`, `+0x63c OR 0x2000`; `SepMandatorySubProcessToken +0x1e7·+0x247·+0x290·+0x379` | `0x2a00` — 단계별 분해는 못 함 |
| `TokenInUse` +0xcc | 인자 비트 | `+0x1be` (0) → **`SeSubProcessToken +0x86c MOV [RAX+0xcc],CL`** (`CL = [RSP+0x58] & 1`, 부모 있는 길은 항상 1) | `1`. (부모 없는 길은 `SeAssignPrimaryToken +0xab`) |
| `IntegrityLevelIndex` +0xd0 · `MandatoryPolicy` +0xd4 | 복사 (`SepMandatorySubProcessToken` 이 이어서 만질 수 있음 — 이 함수는 `+0xd4` 를 읽고 `TokenFlags` 를 씀. 이 두 칸을 바꾸는 줄은 못 찾음) | `+0x337~+0x349` | `0xe` · `3` |
| `LogonSession` +0xd8 | **같은 로그온 세션 객체를 공유** (참조만 늘림) | `+0x415 (ret +0x41a) SepDuplicateLogonSessionReference` | `…9a20` |
| `OriginatingLogonSession` +0xe0 | 복사 | `+0x207~+0x20e` | `0x3e7` |
| `SidHash` +0xe8 (0x110B) | **계산** | `+0x81b RtlSidHashInitialize` | |
| `pSecurityAttributes` +0x308 | 새로 정함 (빈 목록 풀 `SeAt` 0x30바이트; 원본 목록이 비어 있지 않을 때만 `AuthzBasepDuplicateSecurityAttributes` `+0x4a9`) | `+0x55~+0x60` (풀 `SeAt`), `+0x39a MOV [RBX+0x308],RSI` | `…0e10` |
| `IndexEntry` +0x470 | 복사 (공유 항목, 참조 갱신) | `+0x47f SepRefDerefLuidToIndexEntryIfNecessary` → `+0x484~+0x48b` | `…dec0` |
| `SessionObject` +0x488 | **새로 정함**: 세션 번호로 세션 객체 찾음 | `+0x235` (0) → **`SepSetTokenSessionById +0x33 (ret +0x38) PsGetSessionObjectById` → `+0x38 MOV [RBX+0x488],RAX`** | `…3d40` |
| `VariablePart` +0x5c0 | 복사 (가변 영역 통째 `VariableLength` 만큼) | `+0x4cd (ret +0x4d2) RtlCopyMemory` | |

→ **복사 16** (`TokenSource`·`AuthenticationId`·`ParentTokenId`·`ExpirationTime`·`ModifiedId`·`Privileges`·`AuditPolicy`·`UserAndGroupCount`·`VariableLength`·`DynamicCharged`·`IntegrityLevelIndex`·`MandatoryPolicy`·`LogonSession`·`OriginatingLogonSession`·`IndexEntry`·`VariablePart`) + **새로 정함·계산 13** (`TokenId`·`TokenLock`·`SessionId`·`UserAndGroups`·`PrimaryGroup`·`DynamicPart`·`DefaultDacl`·`TokenType`·`TokenFlags`·`TokenInUse`·`SidHash`·`pSecurityAttributes`·`SessionObject`) = 29.

- 이름 (줄기 밖, 이름만): `SepSetTokenBnoIsolation`, `SepDesktopAppxSubProcessToken`, `SepSetTrustLevelForProcessToken`, `SepSetProcessUniqueAttribute`, `SepAddTokenOriginClaim`, `SepFinalizeTokenAcls`, `ObInsertObjectEx` (`SeSubProcessToken +0x80e`) 가 새 토큰에 이어서 일한다. 위 29칸 값이 사진과 같은 걸로 보아 test.exe 에선 이들이 바꾼 칸은 `TokenFlags` 외엔 못 찾음.

#### 2.3 끝까지 0 인 26칸

| 가름 | 칸 | 쓰는 줄 |
|---|---|---|
| **0 을 씀** (15) | `DynamicAvailable` (`+0x1f0`) · `ImpersonationLevel` (`+0x1cd` ← 인자 0) · `Package` (`+0x3cf`) · `Capabilities` (`+0x3c8`) · `CapabilityCount` (`+0x3d6`) · `CapabilitiesHash` (`+0x3dd memset 0x110`) · `LowboxNumberEntry` (`+0x3a8`) · `LowboxHandlesEntry` (`+0x3b1`) · `pClaimAttributes` (`+0x223`) · `TrustLevelSid` (`+0x3e9`, 뒤에 `+0x71e SepSetTokenTrust` 가 원본 값을 옮김) · `TrustLinkedToken` (`+0x3f0`) · `IntegrityLevelSidValue` (`+0x215`) · `TokenSidValues` (`+0x21c`) · `DiagnosticInfo` (`+0x22a`, 누수 추적 꺼짐 `SeTokenLeakTracking`=0) · `BnoIsolationHandlesEntry` (`+0x3be`) | 위 줄. 원본이 이 칸을 가지고 있으면 뒤에 복사하는 갈래 (`Lowbox*` `+0x64f~+0x6f0`, 클레임 `+0x6f5`, 능력 `+0x607`, 패키지 `+0x63f`)가 있는데 **K1 이 널인 걸로 원본에도 없었다** `[추론]` |
| **값 0 인 복사·계산** (4) | `RestrictedSidCount` (`+0x2be`) · `DefaultOwnerIndex` (`+0x2b8`) · `RestrictedSids` (`+0x53d`) · `RestrictedSidHash` (`+0x839 RtlSidHashInitialize`, SID 0개) | 위 줄 |
| **조건부 안 탐** (7) | `TokenFlags2` · `IsInstaller` · `IsAgentic`: `SepDuplicateToken +0x361~+0x394` (`Feature_ShadowAdminAppCompat`·`Feature_AgentId` 켜졌을 때 원본 값 복사), `SeSubProcessToken +0x741 OR [..+0x490],2` (`Feature_AgentId` 와 클레임 있을 때) / `AntiTamperSids`·`AntiTamperSidCount`·`AntiTamperSidHash`·`AntiTamperEntry`: `SeSetTokenTamperProtection` (`PspAllocateProcess +0x26ac`) | IFEO 안티템퍼 정책이 없으면 `SeConstructAntiTamperPolicy` 가 널을 돌려줘 `+0x2664 JZ` 로 건너뜀 (사진 `AntiTamper*`=0) |

#### 2.4 `PspDisablePrimaryTokenExchange` (`PspUserThreadStartup +0x37`, ret `+0x3c`) 가 토큰 칸을 쓰나 — **아니오**

- 읽기: `+0x4e (ret +0x53) PsReferencePrimaryTokenWithTag` → `+0x59 SeTokenIsAdmin` → `+0x64 ObfDereferenceObject`.
- 쓰기는 모두 EPROCESS·KPROCESS: `+0x22 BTS.LOCK [RSI+0x1f0],0xf` (`Flags2.PrimaryTokenFrozen`) · `+0xbb`/`+0xa4`/`+0x111 BTS.LOCK [RSI+0x5fc]` (`Flags3.AddressPolicyFrozen` 0xe, `SecurityFreezeComplete` 0x16) · `+0x94`/`+0xa2` `[RSI+0x760]`·`[RSI+0x768]` = 0 (`SecurityDomain`·`ParentSecurityDomain`, 조건) · `+0xfd MOV byte [RSI+0x160],DIL` (`_KPROCESS.AddressPolicy`=1, `KeKvaShadowingActive` 갈래라 이 실행에선 안 탐). 사진 `Flags2` raw `0x5000 → 0xd000` (`P0~P1`) 와 맞음.
- → baggage 3.19 의 "붙이는 곳: `PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange`" 는 **붙이는 곳이 아니다** (정정).

---

### 3. `_HANDLE_TABLE` 17칸

#### 3.1 test.exe 는 어느 길인가 — **부모 표 복제 (`ExDupHandleTable`)**

- 가르는 줄: `PspAllocateProcess +0x1978 ~ +0x1986` — `[RSP+0x8b0] & 4` 를 `NEG`/`SBB` 로 마스크 만들어 **`RCX = 부모`(켜짐) 또는 `0`(꺼짐)** 으로 `ObInitProcess` (`+0x198c`, ret `+0x1991`) 에 넘김. `[RSP+0x8b0]` = `PspAllocateProcess` 아홉째 인자 = `NtCreateUserProcess` 의 `ProcessFlags` (`NtCreateUserProcess +0xbf6 MOV EAX,[RBP+0x7d0]` → `[RSP+0x40]`) 비트 2 = 상속 (`PROCESS_CREATE_FLAGS_INHERIT_HANDLES`, 이름은 알려진 값 `[추측]`).
- `ObInitProcess`: `RCX`==0 → `+0x40 (ret +0x45) ExCreateHandleTable(새 프로세스, 1)` (빈 새 표). `RCX`≠0 → `+0xaa (ret +0xaf) ObReferenceProcessHandleTable(부모)` → `+0xcc (ret +0xd1) ExDupHandleTable(새 프로세스, 부모 표, R8D=개수, R9=배열, &출력)`. 두 길 모두 끝에서 **`+0x53 MOV [RDI+0x300],R14`** (`EPROCESS.ObjectTable`, Q78c2).
- **사진으로 확정**: K1 `Flags`=`0x18` → `Duplicated`(bit 3)=1. 이 비트를 켜는 줄은 `ExDupHandleTable +0xa6 OR byte [RCX+0x2c],8` 뿐 (`ExCreateHandleTable` 길에는 없음). 핸들 수 4 (`!handle`) = `ExDupHandleTable +0x3b1 INC [RCX+0x58]` 이 4번 (항목 상속 비트 `BT RAX,0x12` 켜진 것만 복사, `+0x337~+0x33f`).
- `R8D`·`R9` (= `ObInitProcess` 의 인자 3·4 = `PspAllocateProcess +0x196c`/`+0x195b` 의 `[R15+0x124]`·`[R15+0x128]`): `ExDupHandleTable` 이 `R14D`=개수, `R15`=정렬된 핸들 배열 로 쓴다 (`+0x4f3~+0x55b`, `+0x18c~+0x198`) → **"상속할 핸들 목록" 속성 (개수, 배열)**. 개수 0 이면 상속 가능 항목 전부. cmd 는 목록을 안 줄 것 `[추측]` — 사진으로는 인자가 사라져서 못 가름 (D2).
- 갈림 정리:
  - `ProcessFlags` 비트 2 꺼짐 → `ExCreateHandleTable` (빈 표, 핸들 0개).
  - 켜짐 + 핸들 목록 없음 → 상속 가능 핸들 전부 복제 (**test.exe**).
  - 켜짐 + 핸들 목록 있음 → 목록의 것만.
  - 부모 프로세스 없음 (시스템/최소 프로세스) → `ObInitProcess` RCX=0, 위 첫째.
  - 부모 표를 못 얻음 (`ObReferenceProcessHandleTable` 널) → `0xc000010a` 로 실패 (`+0xe1`).

#### 3.2 칸마다 — 아는 칸 10

| 칸 | 값의 출처 | 쓰는 줄 | 사진 |
|---|---|---|---|
| `NextHandleNeedingPool` +0x0 | 상수 `0x400` (낮은 단계 표 하나 = 1024 핸들) | **`ExpInsertLowLevelTableIntoFreeList +0xab ADD.LOCK [RCX],0x400`** ← `ExpAllocateHandleTable +0xb4 (ret +0xb9)` | `K1 이전` (`0x400`, 안 바뀜) |
| `TableCode` +0x8 | 낮은 단계 표 페이지 주소 (4KB, `ExpAllocateLowLevelTable +0x12 ExpAllocateTablePagedPoolNoZero`; 레벨 비트 0). 복제가 커지면 `ExpAllocateHandleTableEntrySlow` (`ExDupHandleTable +0x7a`) 가 다시 씀 | `ExpAllocateHandleTable +0xb9 MOV [RDI+8],RBP` | `K1 이전` `…e000` |
| `QuotaProcess` +0x10 | 입력: **새 프로세스** | `ExpAllocateHandleTable +0x5c MOV [RDI+0x10],RSI` | `K1 이전` |
| `HandleTableList` +0x18 | 전역 `HandleTableListHead` 꼬리에 달기 (`HandleTableListLock` 푸시락 안) | 복제 길 **`ExDupHandleTable +0x493 ~ +0x49d`** (Flink 가 머리를 가리키게 `+0x493`, `+0x49d` 로 머리 Blink 갱신). 빈 표 길은 `ExCreateHandleTable +0xc3 ~ +0xcd` | `K1 이전` (Flink = 머리 `…9fb8` ✓ 꼬리) |
| `UniqueProcessId` +0x28 | **처음**: 지금 도는 스레드 (`GS:[0x188]`)의 프로세스 PID = **부모 cmd** (`GS:[0x188] → +0xb8 → +0x1d0`). **다음**: 새 프로세스 자신 PID | **`ExpAllocateHandleTable +0x7c MOV [RDI+0x28],EAX`**; **`PspInsertProcess +0x4c MOV [R10+0x28],EAX`** (`+0x27 R10=[RCX+0x300]` ObjectTable, `+0x31 EAX=[RCX+0x1d0]` UniqueProcessId; 함수 입구에서 바로, 분기 앞) | `K1 이전` `0x19a8` → **`K2~K3` `0x1dd0`** ✓ (K2 는 `PspInsertProcess` 입구라 아직 못 썼고 K3 는 지난 뒤) |
| `Flags` +0x2c | 비트 모음 | `ExDupHandleTable +0xa6` (bit 3), `ExpAllocateHandleTable +0xc2` (bit 4) | `K1 이전` `0x18` |
| `Flags.Duplicated` (bit 3) | 부모 표 복제 길 표시 | **`ExDupHandleTable +0xa6 OR byte [RCX+0x2c],8`** | `K1 이전` 1 |
| `Flags.RaiseUMExceptionOnInvalidHandleClose` (bit 4) | 프로세스 표면 켬 | **`ExpAllocateHandleTable +0xc2 OR byte [RDI+0x2c],0x10`** (`RSI`≠0 일 때) | `K1 이전` 1 |
| `FreeLists` +0x40 (0x40B) · `ActualEntry` +0x40 (0x20B) | `ActualEntry` 는 `FreeLists[0]` 의 앞 32바이트와 **같은 자리** (`_HANDLE_TABLE_FREE_LIST`: `FreeListLock` +0, `FirstFreeHandleEntry` +8, `LastFreeHandleEntry` +0x10, `HandleCount` +0x18, `HighWaterMark` +0x1c — `pdb_struct`). 복제 길 값: 처음 잠금 0 (`ExpAllocateHandleTable +0x83~+0x93` 반복), 핸들 하나 복사마다 `HandleCount`+1 (`ExDupHandleTable +0x3b1 INC [RCX+0x58]`), 항목 이음 `+0x1b4 MOV [RAX+0x50],RCX` (`LastFreeHandleEntry`), 끝에 `HighWaterMark = HandleCount` (`+0x215~+0x21b MOV [RCX+0x5c],EAX`) | 위 줄들 | `K1 이전` raw `+0x48`=`…e020`, `+0x50`=`…eff0`, `+0x58`=4, `+0x5c`=4 → **`P1b` 이후** 핸들 수 26·44·46 은 만들기 공용 부품이 같은 칸을 갱신 (이름만 아래) |

- 뒤에 바뀐 칸 (`UniqueProcessId`·`FreeLists`·`ActualEntry`) 정리: `UniqueProcessId` 는 위 `PspInsertProcess +0x4c`. 핸들 수가 4 → 26 → 44 → 46 으로 늘 때 `FreeLists`/`ActualEntry` 가 바뀌는 건 **핸들 만들기 공용 부품** — `ExCreateHandleEx` (`PspAllocateProcess +0x26fe` 가 쓰는 그 함수), `ObpCreateHandle`, `ObDuplicateObject`, 복구 쪽 `ExFastReplenishHandleTableEntry`·`ExSlowReplenishHandleTableEntry`, 해제 `ExDestroyHandle` (이름만, 그 안은 안 읽음; 어느 것이 어느 칸을 쓰는지는 `[추측]`).

#### 3.3 끝까지 0 인 7칸

| 가름 | 칸 | 근거 |
|---|---|---|
| **0 을 씀** (2) | `HandleContentionEvent` +0x30, `HandleTableLock` +0x38 | `ExpAllocateHandleTable +0xce AND [RDI+0x30],0`, `+0xc6 AND [RDI+0x38],0` |
| **조건부 안 탐** (1) | `Flags.Rundown` (bit 2) | `ExDupHandleTable +0x59b OR byte [RCX+0x2c],4` — 복제 실패 길 (`+0x20f JS +0x59b`) |
| **아무도 안 씀** (4) | `ExtraInfoPages` +0x4, `Flags.StrictFIFO` (bit 0), `Flags.EnableHandleExceptions` (bit 1), `DebugInfo` +0x60 | 두 `ExpAllocate…`·`ExDupHandleTable`·`ExCreateHandleTable` 에 쓰는 줄 없음 (`DebugInfo` 는 `ExDupHandleTable +0x3b9 CMP [RCX+0x60],0` 로 읽기만 — 켜져 있으면 `+0x3d6 ExpUpdateDebugInfo`). 풀 `0x108` (초기화 안 함 비트 없음) → 0 |

---

### 4. 줄기와 경로, 3장 양식

경로 줄임: `①…` = `NtCreateUserProcess` (3장 쓰는 말 그대로). 아래는 이 조각에서 새로 쓰는 사슬을 풀어 썼다 (`호출 +0x..` = 부르는 자리, `ret` 아님).

- `①P…` = `①… → PspAllocateProcess (호출 +0xc31)`
- 핸들 표: `①P… → ObInitProcess (호출 +0x198c) → ExDupHandleTable (호출 +0xcc) → ExpAllocateHandleTable (호출 +0x5f)`
- 토큰: `①P… → PspInitializeProcessSecurity (호출 +0x1922) → SeSubProcessToken (호출 +0xf7) → SepDuplicateToken (호출 +0x43c)`

**줄기** = 칸을 정하는 함수까지 끝까지: `KeInitializeProcess`, `KeSetGroupMaskProcess`, `KeStartThread`, `KeSetProcessPpmPolicy`, `MiCreateNewProcessTopLevelMappings`, `ObInitProcess`, `ExDupHandleTable`, `ExpAllocateHandleTable`, `ExpInsertLowLevelTableIntoFreeList`, `PspInsertProcess`, `PspInitializeProcessSecurity`, `SeSubProcessToken`, `SepDuplicateToken`, `SepSetTokenSessionById`. **줄기 밖 (이름만)**: `memset`·`ExAllocatePool2`·`ExFreePoolWithTag`·락 (`KeAbPreAcquire`·`ExfAcquirePushLockExclusiveEx`·`KiAcquireProcessLockExclusive`), `KiCopyAffinityEx`, `Ke*GroupMask`, `ExSaAllocate`, `KiStackAttachProcess`, `ObCreateObjectEx`·`ObpAllocateObject`, `ExpAllocateHandleTableEntrySlow`·`ExpAllocateLowLevelTable`, `RtlSidHashInitialize`, `SepDuplicateTokenUserAndGroups`·`SepDuplicateLogonSessionReference`, `SepMandatorySubProcessToken` 이하 토큰 마무리.

#### 4.1 baggage 3.12 `_KPROCESS` 에 바로 옮길 줄

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `Header` | ① | 종류 `3` (프로세스 객체), `WaitListHead` 자기 연결 | 직접 | nt | `①P… → KeInitializeProcess (호출 +0x1719) +0x48 · +0x4b · +0x57` | `[정적]` sast43 · 정확 · 사진 `K1 이전` |
| `+0x0` `Header` (잠금 비트) | ① | 스레드 달 때 잠금 비트 잡고 놓음 (net 0) | 직접 | nt | `①… → PspInsertThread (호출 +0x5dd) → KeStartThread +0x1e0 · +0x302` | `[추측]` sast43 · 짐작 (잡는 쪽은 이름으로 본 것) · 사진 `K1 이전` |
| `+0x8` `Header.WaitListHead` | ① 뒤 (부모 대기) | 대기 블록이 달림 (`98adc1c0`) | ? | nt `[추측]` | `?` | `[추측]` sast43 · 짐작 · 사진 `K3~P0` |
| `+0x18` `ProfileListHead` | ① | 자기 연결 | 직접 | nt | `①P… → KeInitializeProcess +0x19e` | `[정적]` · 정확 · `K1 이전` |
| `+0x28` `DirectoryTableBase` | ① | 최상위 페이지 표 물리 주소 (`PFN<<12`, PCID 면 `\|2`) | 직접 | nt | `①P… → MmCreateProcessAddressSpace (호출 +0x1637) → MiCreateNewProcessTopLevelMappings (호출 +0x203) +0x6e` | `[정적]` · 정확 · `K1 이전` |
| `+0x30` `ThreadListHead` | ① | 자기 연결 → 첫 스레드 꼬리 달기 | 직접 | nt | `①P… → KeInitializeProcess +0x1b4`; `①… → PspInsertThread (호출 +0x5dd) → KeStartThread +0x2f4 · +0x2fe` | `[정적]` · 정확 · `K1 이전` → 바뀜 `K2~K3`·`P1a~P1b` |
| `+0x40` `ProcessLock` | ① | 스핀락 잡았다 놓음 (0 으로 돌아옴) | 직접 | nt | `①… → PspInsertThread → KeStartThread +0x99 · +0x38d` | `[정적]` · 정확 · `끝까지 0` |
| `+0x50` `Affinity` | ① | 확장 메모리 포인터 + 인자 친화도 복사 | 직접 | nt | `①P… → KeInitializeProcess +0xa0 · +0xdb` | `[정적]` · 정확 · `K1 이전` |
| `+0x58` `AutoBoostState` | ① | 저장소 포인터 + 트리 수 (부모 있는 프로세스일 때) | 직접 | nt | `①P… → KeInitializeProcess +0x3af · +0x3b3` | `[정적]` · 정확 · `K1 이전` |
| `+0x68` `ReadyListHead` | ① | 자기 연결 | 직접 | nt | `①P… → KeInitializeProcess +0x1a9` | `[정적]` · 정확 · `K1 이전` |
| `+0x80` `ActiveProcessors` | ① | 확장 메모리 포인터 | 직접 | nt | `①P… → KeInitializeProcess +0x7d` | `[정적]` · 정확 · `K1 이전` |
| `+0x88` `ProcessFlags` · `PpmPolicy` | ① (부모 쪽 알림 구간) | 전력 정책 `7` | 직접 | nt (부르는 쪽 BAM `[추측]`) | `(알림 구간 `[추측]`) → PspBamHostInterface 표 → PspSetProcessPpmPolicy (호출 +0x2b · +0xbe) → KeSetProcessPpmPolicy +0x43 · +0x4b` | `[정적]` 쓰는 줄 · 부르는 쪽 `[추측]` · 짐작 · 사진 `K3~P0` |
| `+0x88` `ProcessFlags.AutoAlignment` | ① | 조건부 켜기 (안 탐) | 직접 | nt | `①P… → KeInitializeProcess +0x410`; `①P… +0x2216` | `[정적]` · 정확 · `끝까지 0` (조건부 안 탐) |
| `+0x90` `BasePriority` | ① | 처음 `8`, 곧 `PspPriorityTable[PriorityClass]` 로 덮음 | 직접 | nt | `①P… → KeInitializeProcess +0x6e`; `①P… +0x1a64` (← `PspComputeQuantumAndPriority` 호출 `+0x1a5f`) | `[정적]` · 정확 · `K1 이전` |
| `+0x91` `QuantumReset` | ① | 처음 `6`, 곧 `PspComputeQuantum` 결과로 덮음 | 직접 | nt | `①P… → KeInitializeProcess +0x1bf`; `①P… +0x1a72` | `[정적]` · 정확 · `K1 이전` |
| `+0x98` `ActiveGroupsMask` | ① | 입력 친화도의 그룹 비트 모음 | 주소 넘김 (→ `KeSetGroupMaskProcess`) | nt | `①P… → KeInitializeProcess (호출 +0x191) → KeSetGroupMaskProcess +0x3` | `[정적]` · 정확 · `K1 이전` |
| `+0xb8` `IdealProcessorAssignmentBlock` | ① | 확장 메모리 포인터 (블록 안은 칸 표 없음) | 직접 | nt | `①P… → KeInitializeProcess +0x136` | `[정적]` · 정확 · `K1 이전` |
| `+0x104` `IdealGlobalNode` | ① | 노드 번호 `0` | 직접 | nt | `①P… → KeInitializeProcess +0x1dc` | `[정적]` · 정확 · `끝까지 0` (0 을 씀) |
| `+0x108` `StackCount` | ① | `0` → 스레드마다 `+8` | 직접 | nt | `①P… +0x1c6`; `①… → PspInsertThread → KeStartThread +0x3e9` | `[정적]` · 정확 · `K2~K3` → `P1a~P1b` |
| `+0x110` `ProcessListEntry` | ① | 전역 `KiProcessListHead` 꼬리에 달기 (첫 스레드일 때) | 직접 | nt | `①… → PspInsertThread → KeStartThread +0x247 ~ +0x270` | `[정적]` · 정확 · `K2~K3` |
| `+0x130` `SchedulingGroup` | ① | 널 (세션 스케줄링 그룹 없음) | 직접 | nt | `①P… → KeInitializeProcess +0x2ac` | `[정적]` · 정확 · `끝까지 0` (0 을 씀) |
| `+0x158` `UserDirectoryTableBase` | ① | 커널 주소 격리 켜졌을 때만 | 직접 | nt | `①P… → MmCreateProcessAddressSpace (호출 +0x1c6) → MiAllocateProcessShadow +0xaa` | `[정적]` · 정확 · `끝까지 0` (조건부 안 탐) |
| `+0x160` `AddressPolicy` | ① | 조건부 (안 탐) | 직접 | nt | `①P… +0x1686`; `①T… → PspUserThreadStartup (호출 +0x37) → PspDisablePrimaryTokenExchange +0xfd` | `[정적]` · 정확 · `끝까지 0` (조건부 안 탐) |
| `+0x170` `SecureState` | ① | 조건부 (안 탐) | 직접/주소 넘김 | nt | `①P… → KeInitializeProcess +0x45c → VslCreateSecureProcess`; `①P… → KeSecureProcess +0xa7` | `[정적]` · 정확 · `끝까지 0` (조건부 안 탐) |
| `+0x190` `PerProcessorCycleTimes` | ① | 슬랩 핸들 `0x9300` | 직접 | nt | `①P… → KeInitializeProcess (호출 +0x2c6 ExSaAllocate) +0x351` | `[정적]` · 정확 · `K1 이전` |
| `+0x198` `ExtendedFeatureDisableMask` | ① | 조건부 (CPU 기능 비트 0x37) | 직접 | nt | `①P… → KeInitializeProcess +0x485` | `[정적]` · 정확 · `끝까지 0` (조건부 안 탐 또는 값 0) |
| `+0x1a0` `PrimaryGroup` | ① | 주 그룹 번호 `0` | 직접 | nt | `①P… → KeInitializeProcess +0x196` | `[정적]` · 정확 · `끝까지 0` (0 을 씀) |
| `+0x1a8` `UserCetLogging` | ① | 조건부 (안 탐) | 직접 | nt | `①P… → KeInitializeProcessUserCetLogging (호출 +0x2008) +0x57` | `[정적]` · 정확 · `끝까지 0` (조건부 안 탐) |
| `+0x1b0` `CpuPartitionList` | ① | 자기 연결 | 직접 | nt | `①P… → KeInitializeProcess +0x37c` | `[정적]` · 정확 · `K1 이전` |
| `+0x1c0` `AvailableCpuState` | ① | 확장 메모리 포인터 | 직접 | nt | `①P… → KeInitializeProcess +0xea` | `[정적]` · 정확 · `K1 이전` |

- 이 표의 아는 칸 19 = `Header`·`ProfileListHead`·`DirectoryTableBase`·`ThreadListHead`·`Affinity`·`AutoBoostState`·`ReadyListHead`·`ActiveProcessors`·`BasePriority`·`QuantumReset`·`ActiveGroupsMask`·`IdealProcessorAssignmentBlock`·`PerProcessorCycleTimes`·`CpuPartitionList`·`AvailableCpuState`(15) + `StackCount`·`ProcessListEntry`(2) + `PpmPolicy`·`ProcessFlags`(2). 정확 18 · 짐작 1 (`Header.WaitListHead` 대기 쪽; `Header` 첫 줄은 정확). "0 그대로 · 줄 있음" 10 칸도 같은 표에 줄이 있다 (`IdealGlobalNode`·`SchedulingGroup`·`PrimaryGroup`·`AutoAlignment`·`ExtendedFeatureDisableMask`·`SecureState`·`AddressPolicy`·`UserCetLogging`·`UserDirectoryTableBase`·`ProcessLock`).
- 아무도 안 씀 36 은 3.12 아래 줄 ("아무도 안 씀 36: …") 로만 (3장 표에는 줄 없음, run47 방식).

#### 4.2 baggage 3.16 `_HANDLE_TABLE` 에 바로 옮길 줄

| 칸 | 단계 | 무엇을 | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `NextHandleNeedingPool` | ① | `+0x400` | 직접 | nt | `①P… → ObInitProcess (호출 +0x198c) → ExDupHandleTable (호출 +0xcc) → ExpAllocateHandleTable (호출 +0x5f) → ExpInsertLowLevelTableIntoFreeList (호출 +0xb4) +0xab` | `[정적]` · 정확 · `K1 이전` |
| `+0x8` `TableCode` | ① | 낮은 단계 표 페이지 (4KB) | 직접 | nt | `… → ExpAllocateHandleTable +0xb9` (표 크기가 커지면 `ExDupHandleTable (호출 +0x7a) → ExpAllocateHandleTableEntrySlow`) | `[정적]` · 정확 · `K1 이전` |
| `+0x10` `QuotaProcess` | ① | 새 프로세스 | 직접 | nt | `… → ExpAllocateHandleTable +0x5c` | `[정적]` · 정확 · `K1 이전` |
| `+0x18` `HandleTableList` | ① | 전역 `HandleTableListHead` 꼬리에 달기 | 직접 | nt | `①P… → ObInitProcess → ExDupHandleTable +0x493 ~ +0x49d` (빈 표 길: `ExCreateHandleTable +0xc3 ~ +0xcd`) | `[정적]` · 정확 · `K1 이전` |
| `+0x28` `UniqueProcessId` (처음) | ① | 부모 cmd PID (`0x19a8`) | 직접 | nt | `… → ExpAllocateHandleTable +0x7c` | `[정적]` · 정확 · `K1 이전` |
| `+0x28` `UniqueProcessId` (다음) | ① | 새 프로세스 PID (`0x1dd0`) | 직접 | nt | `①… → PspInsertProcess +0x4c` | `[정적]` · 정확 · 바뀜 `K2~K3` |
| `+0x2c` `Flags` · `Duplicated` | ① | bit 3 켬 (부모 표 복제 길) | 직접 | nt | `①P… → ObInitProcess → ExDupHandleTable +0xa6` | `[정적]` · 정확 · `K1 이전` |
| `+0x2c` `Flags` · `RaiseUMExceptionOnInvalidHandleClose` | ① | bit 4 켬 | 직접 | nt | `… → ExpAllocateHandleTable +0xc2` | `[정적]` · 정확 · `K1 이전` |
| `+0x2c` `Flags.Rundown` | — (실패 길) | bit 2 (안 탐) | 직접 | nt | `… → ExDupHandleTable +0x59b` | `[정적]` · 정확 · `끝까지 0` (조건부 안 탐) |
| `+0x30` `HandleContentionEvent` · `+0x38` `HandleTableLock` | ① | `0` | 직접 | nt | `… → ExpAllocateHandleTable +0xce · +0xc6` | `[정적]` · 정확 · `끝까지 0` (0 을 씀) |
| `+0x40` `FreeLists` · `ActualEntry` | ① | 잠금 0, 첫/마지막 빈 항목, `HandleCount`·`HighWaterMark` (상속한 핸들 수) | 직접 | nt | `… → ExpAllocateHandleTable +0x83~+0x93`; `ExDupHandleTable +0x1b4 · +0x3b1 · +0x215` | `[정적]` · 정확 · `K1 이전` → 바뀜 (핸들 만들기 공용 부품, 이름만) |

- 아무도 안 씀 4: `ExtraInfoPages`, `Flags.StrictFIFO`, `Flags.EnableHandleExceptions`, `DebugInfo`.
- 3.16 "만드는 곳 `+0x1991 ObInitProcess`" 는 call `+0x198c` / ret `+0x1991` 의 ret 이다 (정정 아님, 표기만 call 로 통일 제안).

#### 4.3 baggage 3.19 `_TOKEN` 에 바로 옮길 줄

| 칸 | 단계 | 무엇을 | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `TokenSource` · `+0x18` `AuthenticationId` · `+0x20` `ParentTokenId` · `+0x28` `ExpirationTime` | ① | 부모 토큰 값 복사 (입력) | 직접 | nt | `①P… → PspInitializeProcessSecurity (호출 +0x1922) → SeSubProcessToken (호출 +0xf7) → SepDuplicateToken (호출 +0x43c) +0x1e8 · +0x1dd · +0x1f7 · +0x1ff` | `[정적]` · 정확 · `K1 이전` |
| `+0x10` `TokenId` | ① | 새 LUID (전역 카운터 `ExpLuid` 에 `ExpLuidIncrement` 더함) | 직접 | nt | `… → SepDuplicateToken +0x1a1 · +0x1b1 · +0x1bb` | `[정적]` · 정확 · `K1 이전` |
| `+0x30` `TokenLock` | ① | 새 `ERESOURCE` | 직접 | nt | `… → SepDuplicateToken +0xb3 · +0x1d4 · +0x1d8` | `[정적]` · 정확 · `K1 이전` |
| `+0x38` `ModifiedId` · `+0x40` `Privileges` · `+0x58` `AuditPolicy` | ① | 복사 | 직접 | nt | `… → SepDuplicateToken +0x29a · +0x34f · +0x327` | `[정적]` · 정확 · `K1 이전` |
| `+0x78` `SessionId` | ① | 원본 값 복사 → 새 프로세스 세션 번호로 덮음 | 직접 | nt | `… → SepDuplicateToken (호출 +0x31b) → SepSetTokenSessionById +0x14`; `… → SeSubProcessToken (호출 +0x503) → SepSetTokenSessionById +0x14` · `SeSubProcessToken +0x50d` | `[정적]` · 정확 · `K1 이전` |
| `+0x7c` `UserAndGroupCount` · `+0x84` `VariableLength` · `+0x88` `DynamicCharged` · `+0xd0` `IntegrityLevelIndex` · `+0xd4` `MandatoryPolicy` · `+0xe0` `OriginatingLogonSession` | ① | 복사 | 직접 | nt | `… → SepDuplicateToken +0x513 · +0x2ca · +0x2a6 · +0x337 · +0x343 · +0x207` | `[정적]` · 정확 · `K1 이전` |
| `+0x98` `UserAndGroups` · `+0xa8` `PrimaryGroup` · `+0xb0` `DynamicPart` · `+0xb8` `DefaultDacl` · `+0x5c0` `VariablePart` | ① | 가변 영역·동적 영역 복사 + 포인터 옮김 (풀 `SeDT`) | 직접 | nt | `… → SepDuplicateToken +0x4cd · +0x4e5 · +0x5a7 · +0x5cd · +0x600` | `[정적]` · 정확 · `K1 이전` |
| `+0xc0` `TokenType` | ① | `1` (Primary) | 직접 | nt | `… → SepDuplicateToken +0x1c6` | `[정적]` · 정확 · `K1 이전` |
| `+0xc8` `TokenFlags` | ① | 복사 (일부 비트 제외) 후 여러 단계가 켜고 끔 | 직접 | nt | `… → SepDuplicateToken +0x2f3`; `… → SeSubProcessToken +0x515 · +0x529 · +0x53d · +0x63c`; `… → SepMandatorySubProcessToken (호출 +0x5bb) +0x247 · +0x379` | `[정적]` · 짐작 (칸의 최종 비트 구성은 못 정함) · `K1 이전` |
| `+0xcc` `TokenInUse` | ① | 1 | 직접 | nt | `… → SeSubProcessToken +0x86c` (부모 없는 길: `SeAssignPrimaryToken +0xab`) | `[정적]` · 정확 · `K1 이전` |
| `+0xd8` `LogonSession` | ① | 같은 로그온 세션 참조 (+1) | 직접 | nt | `… → SepDuplicateToken (호출 +0x415) → SepDuplicateLogonSessionReference` | `[정적]` · 짐작 (그 안은 안 봄, 이름으로 본 것) · `K1 이전` |
| `+0xe8` `SidHash` | ① | SID 해시 계산 | 직접 | nt | `… → SepDuplicateToken (호출 +0x81b) → RtlSidHashInitialize` | `[정적]` · 짐작 (안은 안 봄) · `K1 이전` |
| `+0x308` `pSecurityAttributes` | ① | 빈 목록 풀 (`SeAt`) | 직접 | nt | `… → SepDuplicateToken +0x60 · +0x39a` | `[정적]` · 정확 · `K1 이전` |
| `+0x470` `IndexEntry` | ① | 복사 (공유) | 직접 | nt | `… → SepDuplicateToken +0x484 · +0x48b` | `[정적]` · 정확 · `K1 이전` |
| `+0x488` `SessionObject` | ① | 세션 객체 | 직접 | nt | `… → SepSetTokenSessionById (호출 +0x33) → PsGetSessionObjectById`, `+0x38` | `[정적]` · 정확 · `K1 이전` |

- 아는 칸 29 = 정확 27 · 짐작 2 (`LogonSession`·`SidHash`: 쓰는 함수 이름만 봄), `TokenFlags` 는 정확 줄 + 최종 구성 못 정함으로 정확 쪽에 넣음. 0 그대로 26 은 2.3 표 (3.19 에는 "0 을 씀"/"조건부" 줄을 `SepDuplicateToken +0x3a8…` 식으로 한 줄에 묶어 옮겨도 됨).
- `EPROCESS.Token` 줄은 3.11 쪽 (Q78c2): `+0x248 Token` — `PspInitializeProcessSecurity (호출 +0x1922) +0x110 ObInitializeFastReference` (이미 3.11 에 있는 `Token` 줄 "붙이기 `+0x1927`" 은 이 ret 에서 호출 `+0x1922` 안의 `+0x110` 로 구체화됨).

---

### 5. 갈림길과 입력

#### 5.1 갈림길 — test.exe (cmd 가 띄움, 같은 세션, 작업 없음, x64) 가 탄 쪽

| 갈림 | 가르는 곳 | test.exe | 알아낸 방법 |
|---|---|---|---|
| 상속 핸들 (복제) / 빈 표 | `PspAllocateProcess +0x1978~+0x1986` ← `ProcessFlags` 비트 2 | **복제** | 사진 `Flags.Duplicated`=1 + 코드 |
| 핸들 목록 속성 (일부만) / 전부 | `ExDupHandleTable +0x43 TEST R8D` ← `[R15+0x124]` | 전부 `[추측]` | D2 |
| 작업 (Job) | `EPROCESS.Job (+0x2a0)` — `PspSetProcessPriorityClass` 가 읽음, `PspInsertProcess +0xd4 PspImplicitAssignProcessToJob` | **작업 없음** (K1·K3·P0·P2 `Job`=0) | 사진 |
| 앱 컨테이너 / `Feature_DecoupleAppContainerAndIntegrityLevel` | `SeSubProcessToken +0x2f8~+0x305` (`[R15+0x20]`) | **아님** — 복제 길 (`Package`=널) | 사진 간접 |
| 자식 프로세스 금지 토큰 | `SeSubProcessToken +0x117~+0x12b` | 아님 `[추측]` | 토큰 플래그 `0x2a00` 에서 못 가림 |
| CET | `PspAllocateProcess +0x1ff8` (`MitigationFlags2 & 0x4000`) | **아님** | 사진 raw |
| 가상 보안 (VBS) 보안 프로세스 | `EBX` bit 16, 인자 8 bit 1 | **아님** | `SecureState`=0 |
| NUMA 노드 | `PspSelectNodeForProcess` (`+0x12b3`) / 부모 `IdealGlobalNode` / `KeSelectNodeForAffinity` | 노드 0 (`IdealGlobalNode`=0) — 어느 갈래인지는 `[추측]` | 사진 값만 |
| WoW64 x86 | `EPROCESS.Machine==0x14c` (`+0x2216`) | **아님** | 사진 raw `0x8664` |
| 정렬 오류 끔 | `DefaultHardErrorProcessing & 4` | **아님** (`=1`) | 사진 raw |
| 이미지 `ProcessAffinityMask` | `PspAllocateProcess +0x2323` | 아님 `[추측]` | PE 헤더 읽기로 확인 가능 |
| 커널 주소 격리 / PCID | `MiFlags & 0x600000`, `KiFlushPcid` | 둘 다 꺼짐 `[관찰 간접]` | `UserDirectoryTableBase`=0, `DirectoryTableBase` bit 1=0 |
| 캐시 인지 스케줄링 | `KiCacheAwareScheduling & 8` | **켜짐** (`PerProcessorCycleTimes` 값 있음) | 사진 |
| 부모 없는 프로세스 (시스템/최소) | `PspInitializeProcessSecurity +0x53 JZ → +0x1ca`, `ObInitProcess +0x36 JNZ` | 아님 | — |

#### 5.2 입력 (칸과 함께)

| 입력 | 어디서 | 쓰이는 칸 |
|---|---|---|
| 부모 프로세스 (= 지금 도는 프로세스 cmd) | `PspAllocateProcess` 인자 1 (`NtCreateUserProcess +0xc2e MOV RCX,R15`) | 핸들 표 원본 (`ObReferenceProcessHandleTable`), `HANDLE_TABLE.UniqueProcessId` 처음 값 (`GS:[0x188]` 프로세스), 토큰 원본 |
| **부모 토큰** | `PspReferenceTokenForNewProcess` → `PsReferencePrimaryTokenWithTag(부모, 'Dflt')` | 위 2.2 복사 16칸 (+ 값 0 인 복사 3칸 `RestrictedSidCount`·`DefaultOwnerIndex`·`RestrictedSids`) |
| **부모 핸들 표** | `ObInitProcess +0xaa` | 상속 가능 항목 4개 → `HandleCount`·`HighWaterMark`·`FirstFree/LastFree` (항목 안 내용은 범위 밖, 7.6 C 류) |
| `ProcessFlags` (=`NtCreateUserProcess` 7번째 인자) | `PspAllocateProcess` 인자 9 | 핸들 표 길, `[RSP+0xf8]` 일부 |
| 속성 모음 `R15` (=`NtCreateUserProcess [RBP+0x1a0]`) | `PspAllocateProcess` 인자 11 | `[+8]` 비트 (우선순위·보안·핸들 목록·앱 컨테이너 …), `[+0x124]/[+0x128]` 핸들 목록, `[+0x20]` 앱 컨테이너 속성 |
| 부모 `EPROCESS` 필드 | `+0x388 DefaultHardErrorProcessing`, `+0x1f0 Flags2` (PropagateNode), `+0x104 KPROCESS.IdealGlobalNode`, `+0x750/+0x754 MitigationFlags*`, `+0x347 PriorityClass`(새 프로세스 것) | `AutoAlignment`, `IdealGlobalNode`, `UserCetLogging`, `BasePriority`·`QuantumReset` |
| 커널 전역 | `KiMaximumGroups`, `KeNodeBlock`, `KiCacheAwareScheduling`, `KeFeatureBits`, `KiFlushPcid`, `MiFlags`, `PspPriorityTable`, `PspForegroundQuantum`, `PsPrioritySeparation`, `HandleTableListHead/Lock`, `KiProcessListHead/Lock`, `ExpLuid`, `ExpLuidIncrement`, `PsNextSecurityDomain` (EPROCESS 쪽), `SeTokenObjectType`, `SeTokenLeakTracking`, `SepTokenSidSharingEnabled`, `PspBootAccessToken` | 위 표에서 쓰인 줄 |
| 세션 번호 | `PspInitializeProcessSecurity +0xa7 PsGetProcessSessionId(새 프로세스)` | 토큰 `SessionId`·`SessionObject` |

## 동적분석으로 확인해야 할 것

번호는 이 조각 안 번호 (D1~D6). "사진" = 이미 있는 덤프 (`ms/dump/03_baggage-snap/`, 커널 메모리 포함이면) 로 답할 수 있는지.

| # | 질문 | 추천 멈춤점 / 찍을 것 | 사진으로? |
|---|---|---|---|
| D1 | 새 토큰이 부모 cmd 토큰의 **복제**임을 값으로 보이기: cmd 토큰의 `TokenId`·`ParentTokenId`(=`0x21d73` 예상)·`ModifiedId`(=`0x21d7c` 예상)·`AuthenticationId`(=`0x21d70`) 가 새 토큰과 어느 칸이 같고 어느 칸이 다른가, 주소는 다른가. `PspReferenceTokenForNewProcess` 가 핸들 속성이 없어 `PsReferencePrimaryTokenWithTag(부모)` 쪽으로 갔는가 | `nt!SepDuplicateToken` 입구 `rcx`=원본 토큰, 반환 직후 `[rsp+0x48]`(SeSubProcessToken 지역)=새 토큰; 또는 `nt!SeSubProcessToken` 입구에서 `rdx` | **가능성 있음**: K1 덤프에서 `!process 19a8 0` 로 cmd EPROCESS → `Token & ~0xf` → `dt nt!_TOKEN`. 덤프가 커널 풀을 포함하면 (D 번호 새로 안 만들어도 됨, dast/04 방식으로 추가 읽기) |
| D2 | `ObInitProcess` 에 넘어간 핸들 목록 속성: `r8d`(개수)=0 이고 `r9`(배열)=널인가, `rcx`≠0 인가 | `nt!ObInitProcess` 입구 (조건: 현재 프로세스 이미지 = cmd.exe — 새 프로세스는 아직 이름이 없다), `rcx`·`r8d`·`r9`·`@rdx` | 불가 (인자는 사라짐). 상속 길 자체는 이미 사진으로 확정 |
| D3 | `ProcessFlags.PpmPolicy`=7 (`0x380`) 을 누가 쓰나: `KeSetProcessPpmPolicy` 의 콜스택 → `PspSetProcessPpmPolicy` ← 어느 드라이버 모듈 (bam.sys 짐작)인가, 어느 시점 (`PspCallProcessNotifyRoutines` 알림 안?) | `nt!KeSetProcessPpmPolicy` 입구 `k`, `rdx`=7, `rcx`=새 EPROCESS (조건: `rcx` 의 `ImageFileName`="test.exe" 또는 `UniqueProcessId`) | 불가 |
| D4 | `Header.WaitListHead` K3~P0 의 `ffff8285'98adc1c0` 은 누구의 대기 블록인가 (부모 cmd 스레드 짐작) | — | **가능**: P0 덤프에서 `dt nt!_KWAIT_BLOCK ffff828598adc1c0` (`.Thread`·`.Object`) |
| D5 | 조건 전역 값: `KiCacheAwareScheduling`(bit 3), `KeFeatureBits`(bit 0x37), `MiFlags`(& 0x600000), `KiFlushPcid`, `KiMaximumGroups`, KUSD `+0x710`, `PspForegroundQuantum[0]`, `PsPrioritySeparation` | `dd`/`dq`/`db` 한 번씩 | **가능** (전역은 덤프에 있음). `ExtendedFeatureDisableMask` 0 이 "안 탐"인지 "0 씀"인지 닫힘 |
| D6 | `TokenFlags` `0x2a00` 의 단계별 분해 (복사 직후 / `SeSubProcessToken` 마무리 후 / `SepMandatorySubProcessToken` 뒤) | `nt!SepDuplicateToken` 반환 직후 `dd poi(@rax)+0xc8`, `nt!SeSubProcessToken` 반환 직후 | 불가. 급하지 않음 (값이 이미 사진에 있음) |

- 추천 멈춤점이 새 요청서를 꼭 필요로 하는 건 D1(덤프로 안 되면)·D3 정도다. 나머지는 사진으로 닫히거나 닫지 않아도 칸 표가 완성된다.
- 디버거가 붙어서 달라지는 점: 없음 (모든 근거가 정적 + 이미 찍은 사진).

## Q78c2 (`_EPROCESS`) 로 넘길 것 — 이 조각에서 본 `_EPROCESS` 쓰기 줄

| 칸 | 쓰는 줄 | 비고 |
|---|---|---|
| `+0x248` `Token` | `PspInitializeProcessSecurity +0x109~+0x110 (ret +0x115)` → `ObInitializeFastReference +0x23~+0x30` (`토큰 \| 0xf`, 참조 `+0xf` 는 `+0x1e`) | 부모 없는 길 `+0x1ca` (널) → `SeAssignPrimaryToken +0xb5` |
| `+0x300` `ObjectTable` | `ObInitProcess +0x53 MOV [RDI+0x300],R14` (실패 정리 `+0x102` 널) | 상속/새 표 둘 다 |
| `+0x760` `SecurityDomain` · `+0x768` `ParentSecurityDomain` | `PspInitializeProcessSecurity +0x1a0 · +0x1a7` (부모 값 또는 `PsNextSecurityDomain` `XADD +0x194` +1), 지움: `PspDisablePrimaryTokenExchange +0x94 · +0xa2` | baggage 7.2 `P0~P1`·`끝까지 0` 후보 |
| `+0x1f0` `Flags2.PrimaryTokenFrozen` (bit 15) · `+0x5fc` `Flags3.AddressPolicyFrozen` (14) · `SecurityFreezeComplete` (22) | `PspDisablePrimaryTokenExchange +0x22 · +0xbb · +0xa4 · +0x111` (`PspUserThreadStartup +0x37`) | 7.2 `P0~P1` 칸의 쓰는 줄 |
| `+0x818` `PpmPolicyLock` · `+0x820` `BamPpmPolicy` | `PspSetProcessPpmPolicy +0x70~+0x9a` (푸시락), `+0xa5 MOV [RBP+0x820],EDI` | 7.2 `K3~P0` `BamPpmPolicy` 와 같은 구간 |
| `+0x347` `PriorityClass` | `PspSetProcessPriorityClass +0x72` (속성 모음 bit 8 일 때) | K1 값 `2` — 부모에서 물려받은 값 또는 기본 `[추측]` (기본값을 정하는 줄은 `PspAllocateProcess` 안 어딘가) |
| `+0x1f4` `Flags` 일부 (`AddressSpaceInitialized` 10:2 의 bit 11) | `MiCreateNewProcessTopLevelMappings +0x72 lock or [rdi+0x1f4],0x800` | `MmCreateProcessAddressSpace` 아래 |
| `+0x540` 목록 (전역 `0x140e2db98` 에 이음) · `+0x3d8`=4 · `+0x410` | `MiCreateNewProcessTopLevelMappings +0x3d~+0x65`, `MmCreateProcessAddressSpace +0x1ed · +0x213` | 이름 모름 |
| `+0x1d0` `UniqueProcessId` | `PspAllocateProcess +0x26fe ExCreateHandleEx(PspCidTable)` — **이 값을 `PspInsertProcess +0x31` 이 읽어 핸들 표로 옮긴다** | 3.11 에 이미 줄 있음 |
| `PspAllocateProcess +0x564 ~ +0x5fc` 초기화 묶음 | `+0x580`·`+0x590`·`+0x588`·`+0x1e8`·`+0x1c8`·`+0x370`·`+0x620`·`+0x710` 목록 머리 자기 연결, `+0x5fa Protection`·`+0x5f8 SignatureLevel`·`+0x5f9 SectionSignatureLevel`·`+0x658`·`+0x788`·`+0x780`·`+0x798` | `memset` 직후 |
| 몸통 크기 | `PspAllocateProcess +0x450 ~ +0x4f7`: `[RSP+0x168]`(K1 에서 `0xa60`)= 몸통 + `PoEnergyEstimationEnabled` 이면 +0x1e8 + 그룹 여럿이면 그룹당 0x10 …. `_KPROCESS` 확장 메모리가 그 뒤에 붙음 (이 보고서 1.2) | |

## 정정 · 새로 알게 된 것 (flow-writer 용)

1. baggage 3.19 "붙이는 곳 `PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange`" — 토큰을 붙이는 함수가 아니다 (2.4). 같은 함수는 7.2 `EPROCESS` `P0~P1` 의 `Flags2.PrimaryTokenFrozen`·`Flags3.AddressPolicyFrozen`·`SecurityFreezeComplete` 를 쓰는 줄이다.
2. baggage 7.8 `HANDLE_TABLE.UniqueProcessId` — 닫힘 (`PspInsertProcess +0x4c`, 처음 값은 `ExpAllocateHandleTable +0x7c` 가 "지금 도는 프로세스" 를 읽은 것이라 부모 PID).
3. baggage 7.8 "부모 쪽이 ⓪ 전에 자식에 쓰는 칸 (`K3~P0`)": `EPROCESS.BamPpmPolicy`·`KPROCESS.ProcessFlags` (+`PpmPolicy`) 는 **같은 함수 `PspSetProcessPpmPolicy`** (BAM 호스트 인터페이스 표 경유) 가 쓰는 걸로 보인다 `[추측: 부르는 쪽]`. `EPROCESS.ExceptionPortData`·`Enable…Logging` 비트·`WakeChannel` 은 이 조각에서 못 닫음.
4. 3.11 `Pcb`/`ObjectTable`/`Token` 줄의 "호출 `+0x171e` · `+0x1991` · `+0x1927`" 는 ret 오프셋이다. call 오프셋은 `+0x1719` · `+0x198c` · `+0x1922` (이번 표는 call 로 통일). 값이 틀린 건 아니다.
5. baggage 3.12 "초기화: `KeInitializeProcess` 칸 단위 근거 없음" — 65칸 전부 근거 생김.
6. 새 칸 표 없는 짐 후보 (7.6 에 올릴지는 사용자와): `_KPROCESS_AVAILABLE_CPU_STATE` (0x78), `_KI_IDEAL_PROCESSOR_ASSIGNMENT_BLOCK` (0x1d8), `_KAB_UM_PROCESS_TREE`, `_HANDLE_TABLE_FREE_LIST` (0x40, 이번에 `pdb_struct` 로 칸 확인), 핸들 표 낮은 단계 페이지 (4KB, 항목 0x10바이트씩), `_SEP_LOGON_SESSION_REFERENCES`, `_SEP_LUID_TO_INDEX_MAP_ENTRY`.

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **고칠 버그는 없다.** `pdb_struct.py`·`run_ghidra.sh export/refs`·`_calls.tsv` 모두 문제없이 돌았다. 한 번에 4초 안팎.
- **제안 1 — "이 구조체 칸을 누가 쓰나" 도구가 없다.** `refs` 는 전역 변수·함수 주소만 찾는다 (`[reg+0x88]` 같은 구조체 안 칸은 못 찾음). 이번에 `PpmPolicy` (`+0x88` 의 4비트), `UserDirectoryTableBase` (`+0x158`), `HANDLE_TABLE.UniqueProcessId` (`+0x28`), `DirectoryTableBase` (`+0x28`) 의 쓰는 곳을 찾으려고 `objdump -d` 를 한 번 떠서 (0.5초, 106MB) 정규식으로 훑었다. 그 임시 스크립트 (`fnmap.py`/`scan.py`/`callers.py`/`symat.py`) 는 scratchpad 에만 있고 레포에는 안 넣었다. 제안: `scripts/field_writers.py <바이너리> <오프셋 16진> [마스크 상수 …]` — objdump 결과를 캐시해 두고 "`[reg+off]` 에 쓰는 명령 (mov/or/and/xchg/cmpxchg/lock …) + 같은 근처의 마스크 상수" 를 `함수+오프셋` 으로 출력. 함수 이름은 `_index.tsv` 로 붙이면 된다. 구조체 오프셋은 이름이 아니라 숫자라 **오탐이 많으므로** "후보 줄" 로만 쓰고 `.asm` 로 확인하는 약속과 함께.
- **제안 2 — "이 함수를 누가 부르나" 가 함수 포인터 표 경유면 `_calls.tsv` 에 없다.** `PspSetProcessPpmPolicy` 는 직접 호출자가 0 이고 `PspBamHostInterface +0x8` 포인터 표로만 닿는다. 이번엔 PE 에서 8바이트 주소를 찾고 (`PspBamHostInterface +0x8`) → PDB 공개 이름으로 표 이름을 붙이고 → 그 표를 참조하는 `refs` (`PspInitPhase1 +0xb3`) 로 이어갔다. `refs <함수 이름>` 이 이미 `ptr` 줄 (메모리 안 8바이트 값) 을 찾으니, "함수 → 표 이름 (PDB) 까지 자동으로 이어 주는 `--through-ptr` 옵션" 이면 한 번에 되었을 것. (`refs ntoskrnl.exe PspSetProcessPpmPolicy` 로 `ptr` 줄이 나오는지는 이번에 안 해 봤다.)
- 겹침: `pdb_struct.py` 의 앞 바퀴 제안 (`--expand`, `overlap` 열) 은 이번에도 필요했다 — `_KPROCESS.SecureState` (`<unnamed-tag>`) 안쪽 `SecureHandle`·`Flags.SecureProcess`·`Flags.TrustedApp` 은 sast/38 의 `struct__KPROCESS.tsv` 로 대신 봤다 (직접 칸 65 에는 `SecureState` 한 칸으로만 센다).

## 판정

**분석성공.** 요청서 질문 1~5 모두 답했다. 모르는 칸 137 → 아는 칸(nt) 58 · 0 그대로 중 줄 있음 39 · 아무도 안 씀 40 · 여전히 모름 0. `HANDLE_TABLE.UniqueProcessId` 의 쓰는 줄 (baggage 7.8 `[공백]`) 두 곳과, 토큰이 복제라는 것, 핸들 표가 복제 길이라는 것을 사진 + 코드로 확정. 정적으로 못 닫은 것은 D1~D6 (대부분 부르는 쪽 모듈·값 대조 수준).

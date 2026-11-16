# 2026-10-30 run50 — 프로세스 객체를 만들 때 (Q78c2a): `_EPROCESS` 모르는 칸 212 → **여전히 모름 1** (`SharedCommitCharge`). `K1 이전` 50칸은 `PspAllocateProcess` 와 callee 몇 개가 쓰고, 끝까지 0 160칸은 0 을 씀 50 · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 길 52 `[추측]` 으로 갈림. test.exe 는 **IFEO 키가 없는 길** — 커널이 IFEO 값을 하나도 안 읽고, 그 표시가 매개변수 `Flags 0x4000` 으로 ntdll 까지 간다 (정적)

> 정정 (run120, sast/115): 아래 "범위 밖 길에서 씀" 의 `InvertedFunctionTableLock` (`+0x5d0`) · `ThreadListLock` (`+0x5e0`) 은 **초기화 길에서 잡고 푼다** — ② `NtSetInformationProcess(0x35) +0x566 → RtlInsertDynamicUserFunctionTable +0xbc BTS` … `+0x125 XADD −1`, ① `PspInsertThread +0x6c3 BTS` … `+0x24e XADD −1`, 끝 값 0. 짐 지도 3.11 아는 칸으로 옮김 (범위 밖 53 → 51). run120 문서.

## 요약 표 (모르는 칸 212 → ?)

- 결과서 맨 앞 표 그대로. 분류 뜻:
  - **아는 칸 (nt)** = 사진에서 값이 있고 (`K1 이전`), nt 에서 그 칸을 쓰는 줄을 찾음. `*` 칸 (사진에서 뒤에 또 바뀜) 은 **처음 값을 쓰는 줄만** 닫았다
  - **0 을 씀** = 사진 값이 0 이고, 만들기 길이 그 칸에 0 을 씀 (명시 0 저장 · 입력값 0 · 같은 DWORD 를 읽어 되쓰기)
  - **조건부 안 탐** = 켜는 줄이 있는데 test.exe 에선 조건이 안 맞음
  - **아무도 안 씀 (만들기 길 안)** = 만들기 길 (+ 부른 함수) 에 쓰는 줄이 없고, 커널 전체를 훑어도 이름 있는 쓰는 곳이 안 보임. 할당 때 0 그대로
  - **범위 밖 길에서 씀 `[추측]`** = 만들기 길에는 쓰는 줄이 없고, 종료·작업·타이머·페이지 오류 같은 **다른 길**의 이름 있는 함수가 쓰는 것으로 보임
  - **여전히 모름** = 줄 후보만 있고 못 정함

| | 칸 | 아는 칸 (nt) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (만들기 길 안) | 범위 밖 길에서 씀 `[추측]` | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `K1 이전` | 52 | **50** | — | — | — | 1 (`NumberOfPrivatePages`) | 1 (`SharedCommitCharge`) |
| 끝까지 0 | 160 | — | **50** | **30** | **28** | **52** | 0 |
| **합** | **212** | **50** | **50** | **30** | **28** | **53** | **1** |

- "범위 밖 길에서 씀" 과 "아무도 안 씀" 은 **커널 전체를 오프셋 숫자로 훑은 결과**가 근거라 오탐이 있을 수 있다 (`KTHREAD`·PEB·`ERESOURCE` 가 같은 숫자를 씀). 둘 사이 경계가 약하다는 뜻이다.

짐 지도 (`baggage.md` 2장) 로 옮길 때 — run47·run49 와 같은 규칙에 하나를 더했다:

- "아는 칸" 50 + "0 을 씀" 50 + "조건부 안 탐" 30 = **130** → "nt 가 씀" (줄이 있는 칸)
- "아무도 안 씀" **28** → 2장 표 아래 줄 (run47·run49 와 같음, 표에 열 없음)
- "범위 밖 길에서 씀 `[추측]`" **53** → **모름에서 빼고, "아무도 안 씀" 옆에 따로 한 줄**. 이유:
  - 초기화 동안 (만들기 길) 에 쓰는 줄이 없다는 점은 "아무도 안 씀" 과 같다 `[정적]` 부재. 그래서 더는 Q78 거리가 아니다.
  - 그런데 다른 길의 쓰는 함수는 오프셋 숫자 훑기로만 봤다 (`[추측]`, 줄을 `.asm` 로 안 읽음). "nt 가 씀" 에 넣으면 3장 `—` 단계 줄 (run46 `LdrpDllDirectory` 처럼 쓰는 줄이 `[정적]`) 과 섞인다. "아무도 안 씀" 에 넣으면 정말로 아무도 안 쓰는 28 과 섞인다.
  - 예외 하나: `NumberOfPrivatePages` 는 K1 값이 `0x20` 이다 (0 아님). 페이지 오류가 올린 것으로 보이는데 `[추측]`, 그래서 짐 지도 7.4 "짐작이라 다시 볼 것" 에도 올렸다 (동적 E6).
- 여전히 모름 1 (`SharedCommitCharge`) 은 모름에 남긴다 (7.2 `K1 이전`, 줄 후보는 7.5).

짐 지도 2장 숫자 변화 (`_EPROCESS`):

| | nt 가 씀 | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|
| `_EPROCESS` (263칸) | 22 → **152** | 0 → **28** | 0 → **53** | 241 → **30** | 20 + 2 → **146 + 6** |

- 정확·짐작은 짐 지도 3장 규칙 (**한 칸에 정확 줄이 하나라도 있으면 정확**) 으로 flow-writer 가 셌다. 새 짐작 4 = `Job`·`JobLinks`·`CommitChargeJob` (부르는 줄 `PspInsertProcess +0xd4` 와 조건만 앎, 칸을 쓰는 줄은 callee 안이라 안 읽음) · `Flags.ProcessDelete` (켜는 줄이 이 칸인지 이름이 어색함 `[추측]`).
- 남은 모름 30 = `K1 이전` 1 (`SharedCommitCharge`) + 뒤 구간 29 (`K2~K3` 3 · `K3~P0` 11 · `P0~P1` 9 · `P1~P1a` 5 · `P1a~P1b` 1 → **Q78c2b**).
- 짐 지도 전체: 구조체 21개 직접 칸 1,456 = 아는 칸 346 → **476** (정확 291 → **417** · 짐작 55 → **59**) + 아무도 안 씀 51 → **79** + 범위 밖 길에서만 씀 0 → **53** + 모름 1,059 → **848**.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 요청서·결과서 날짜.
- 03_sprint 여덟째 바퀴 (짐 지도 바퀴, plan **Q78c2a** — Q78c2 `_EPROCESS` 를 시간 순서로 나눈 첫 조각, 오케스트레이터 기본값 2026-10-30).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음). 이미 analyze 된 프로젝트에서 뽑기만 했다.
- 버전 대조 (결과서): `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`, `PspAllocateProcess +0x1719` (ret `+0x171e`) → `KeInitializeProcess` 가 `_calls.tsv` 와 같다. 그리고 **사진 값과 코드가 칸마다 맞는다**:
  - K1 `Flags` `0x10050c00` = 비트 10·11 (`AddressSpaceInitialized` 3) · 16 (`ProcessInSession`) · 18 (`HasAddressSpace`) · 28 (`DefaultIoPriority` 2). 켜는 줄 넷과 정확히 같다.
  - K1 `MitigationFlags` `0x20` = `sample/bin/test.exe` 의 `DllCharacteristics` `0x8160` 에 HIGH_ENTROPY_VA (`0x20`) 는 있고 CFG (`0x4000`) 는 없는 것과 맞다.
  - `ImageFileName` `"test.exe"`, `Machine` `0x8664`, `HighestUserAddress` `0x7fffffff0000`, `ExitStatus` `0x103`.
- 이 문서를 쓰며 다시 맞춘 것: 아래 호출 지점 표의 call/ret 을 `ms/disassemble/ntoskrnl/_calls.tsv` (3,744줄) 로, 쓰는 줄 (`PspAllocateProcess +0x7a0`·`+0x7fe`·`+0x819`·`+0xa61`·`+0xa6c`·`+0xa71`·`+0xad0`·`+0x169c`·`+0x1931`·`+0x194f`, `PspCopyAndFixupParameters +0x74`·`+0x7b`·`+0x82`, `MiCreateNewProcessTopLevelMappings +0x72`, `MmInitializeProcessAddressSpace +0x117`, `PsJoinSession +0x21`, `PspInsertProcess +0x202`, `PspInsertThread +0x77b`) 를 `.asm` 으로 한 번 더 봤다. `PspAllocateProcess +0x1931 MOV byte [R14+0x347],2` 는 오케스트레이터도 `.asm` 으로 확인했다. 칸 오프셋은 sast/38 `struct__EPROCESS.tsv` 로 봤다.
  - `.asm` 이 없는 곳 (결과서가 `objdump -d` 로만 봄): `PspReadOptionsMapFromIFEO` (`+0x1f ~ +0x2b`), `PspReadIFEOAntiTamperOptions`·`PspReadIFEOMitigationAuditOptions`·`PspReadIFEOPerfOptions` 앞부분, `PsChargeProcessPagedPoolQuota`·`PsChargeProcessNonPagedPoolQuota`, `ObCreateObjectEx +0x148`, `ObpAllocateObject +0x191`·`+0x197`, `ExAllocatePoolWithTag +0x48`·`+0x9f`, `MiCompletePrivateZeroFault`·`MiInsertSharedCommitNode` (전체 훑기). 이 문서에선 결과서의 함수+오프셋 그대로 쓰고 "`.asm` 없음" 이라고 붙였다.

## 목적

- 짐 지도 Q78 의 넷째 조각 (Q78c2 의 첫 조각). `_EPROCESS` (baggage 3.11) 모르는 칸 241 중 **만들 때 몫 212** 를 닫는다.
  - `K1 이전` 52 — 칸마다 쓰는 줄 (함수+오프셋) 과 값의 출처 (부모 / 인자 / 섹션 / IFEO / 계산 / 상수).
  - 끝까지 0 160 — 만들기 길 (`NtCreateUserProcess → PspAllocateProcess` 와 callee, `PspInsertProcess` 까지) 안에서 0 을 씀 / 조건부 안 탐 / 아무도 안 씀 으로 가르기.
  - 3.11 "(run49) Q78c2 에서 볼 것" 메모를 확인해서 옮기기, sast/41 이 넘긴 `SectionObject`·`ImageFilePointer`.
- 갈림길과 입력: IFEO 읽기가 어느 칸에 들어가나, test.exe 는 IFEO 키가 없을 때의 길인가 (run46 ntdll 쪽과 같은 결과인가), 간접 호출 두 곳이 쓰는 칸.
- 범위 밖: 뒤 구간 29칸 (Q78c2b), `Vm` (`_MMSUPPORT_FULL`)·`VadRoot` 안쪽 내용, 페이지 표 내용.

## 방법

- 정적: `_workspace/sast/44_eprocess-create.md` — ntoskrnl. 새로 depth 0 **27개**, 두 번에 나눠 뽑음:
  - 1차 11개 (요청서 표): `PspApplyMitigationOptions`·`PspInheritMitigationOptions`·`PspHardenMitigationOptions`·`PspReadIFEOMitigationOptions`·`PspAssignProcessQuotaBlock`·`PspInheritQuota`·`PspInitializeFullProcessImageName`·`PspAttachSession`·`PspSelectMachineForProcess`·`PoEnergyContextInitialize`·`PspUpdateCreateInfo`.
  - 2차 16개 (칸을 실제로 쓰는 줄이 있어서): `MiCreateNewProcessTopLevelMappings` (run49 에선 objdump 만 — 이제 `.asm` 있음)·`PsJoinSession`·`PsReserveSessionMembership`·`PsChargeProcessQuota`·`PspChargeQuota`·`MiInsertVad`·`MiInsertProcessVads`·`MiInsertVadCharges`·`MiComputeProcessUserVa`·`MiInitializeProcessBottomUpEntropy`·`PspSetProcessShortName`·`PspReadIFEONodeOptions`·`PspApplyIFEOPerfOptions`·`PspInheritSyscallProvider`·`DbgkCopyProcessDebugPort`·`PspSelectNodeForProcess`.
  - 이미 `.c` 가 있던 12개 (`PspAllocateProcess`·`PspInsertProcess`·`MmCreateProcessAddressSpace`·`MmInitializeProcessAddressSpace`·`PspSetupUserProcessAddressSpace`·`PspComputeQuantumAndPriority`·`PspInitializeProcessSecurity`·`ObInitProcess`·`PspSetProcessPriorityClass`·`MmCreatePeb`·`NtCreateUserProcess`·`PspCopyAndFixupParameters`) 는 읽기만.
  - `_calls.tsv` 3,598 → 3,744줄 (export 전 줄이 하나도 안 사라짐).
  - 끝까지 0 160칸 전체 훑기: 결과서가 `objdump -d` 한 번 + 임시 스크립트 (스크래치, 레포 밖) 로 `[reg+칸오프셋]` 쓰기를 찾았다. 오프셋 숫자 일치라 오탐 있음 (결과서 "판단 기준").
  - 값 대조: run44 덤프 읽기 로그 `ms/logs/04_baggage-read_<K1~P2>.txt` 의 `01c EPROCESS_RAW` 8장. 새 동적 실행은 없다.
- 결과서: `_workspace/sast/44_eprocess-create_result.md` (**분석성공**)
- 문서: 이 run 문서, `baggage.md` (기준, 쓰는 말, 1장, 2장·2.1, 3장 줄임·3.2 `Flags` 줄·3.11 다시 씀, 5장 ①, 6장, 7.1·7.2·7.4·7.5·7.6·7.8, 새 7.13, 8장), `flow.md` 기준·2.1 (트리, `PspAllocateProcess`·`MmInitializeProcessAddressSpace`·`PspCopyAndFixupParameters` 절)·2.4 프로세스 힙 절 한 줄·4장·4.1·5장. 그림 (mermaid) 은 안 바꿨다 (아래 "그림"). 이전 문서는 `runs/02_sprint/run10-allocate-process.md` 맨 위 정정 한 줄만.

## 결과

비유 (새 가게 열기):

- `_EPROCESS` = 가게 **영업 신고서**. 본사 (`PspAllocateProcess`) 가 빈 신고서를 받아 (`+0x534`) 지우개로 한 번 더 밀고 (`+0x55f memset`), 칸을 채운다.
- 칸을 채우는 출처가 넷이다:
  - **부모 가게 (cmd) 에서 베낌** — 부모 번호 (`InheritedFromUniqueProcessId`), 입출력·페이지 우선순위 비트, 오류 처리 방식, 쿼터 장부 (같이 씀), 보안 구역 번호.
  - **받은 서류 (인자)** — 섹션 객체, 이미지 파일 객체, 기계 종류.
  - **땅 다지기 (주소 공간)** — 페이지 표 시작, 작업 집합, VAD, 최고 주소, 커밋 양.
  - **상수** — 종료 상태 `0x103` ("아직 안 끝남"), 우선순위 클래스 `2` ("보통").
- 단 하나, **세션은 부모가 아니라 "지금 서류를 쓰는 사람" (호출한 쪽) 의 세션**이다. cmd 가 띄우면 둘이 같은 사람이라 결과는 같다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0xc31` | `+0xc36` | `nt!PspAllocateProcess` | [정적] |
| `nt!NtCreateUserProcess` | `+0xfbd` | `+0xfc2` | `nt!PspInsertProcess` | [정적] |
| `nt!PspAllocateProcess` | `+0x534` · `+0x55f` | `+0x539` · `+0x564` | `ObCreateObjectEx` (객체, 0 채움) · `memset` (몸통 + 확장 영역 한 번 더 0) | [정적] |
| `nt!PspAllocateProcess` | `+0x757` | `+0x75c` | `PoEnergyContextInitialize` (`EnergyContext` 가 가리키는 확장 칸) | [정적] |
| `nt!PspAllocateProcess` | `+0x7e4` | `+0x7e9` | 부모 없을 때 기본 페이지 우선순위 묻기 (같은 주소 이름 2개, 후보 `KeQueryHeteroCpuPolicyThread`·`MmGetDefaultPagePriority` — 결과서는 뜻이 맞는 `MmGetDefaultPagePriority` 로 읽음). test.exe 는 부모가 있어 안 탐 | [정적] |
| `nt!PspAllocateProcess` | `+0x830` | `+0x835` | `PsReferencePartitionSafe` — 파티션 속성이 있을 때만 (없으면 전역 시스템 파티션) | [정적] |
| `nt!PspAllocateProcess` | `+0xa5c` | `+0xa61` | **`RtlpOpenImageFileOptionsKeyEx`** (IFEO 키 열기) — 실패 `0xC0000034` 면 `+0xa6c` 표시 · `+0xad0` 이후 값 읽기 전부 건너뜀 | [정적] |
| `nt!PspAllocateProcess` | `+0xa87` | `+0xa8c` | `PspSelectMachineForProcess` (`Machine` 값) | [정적] |
| `nt!PspAllocateProcess` | `+0xccd` · `+0xe54` · `+0x19db` | `+0xcd2` · `+0xe59` · `+0x19e0` | `PspReadIFEONodeOptions` · `PspReadIFEOPerfOptions` · `PspApplyIFEOPerfOptions` — IFEO 키가 있을 때만 (test.exe 안 탐) | [정적] |
| `nt!PspAllocateProcess` | `+0x151f` · `+0x158f` | `+0x1524` · `+0x1594` | `PspAttachSession` · `PspAssignProcessQuotaBlock` — 갈래 (토큰 핸들 속성 / 세션 다름). test.exe 안 탐 | [정적] |
| `nt!PspAllocateProcess` | `+0x15d7` | `+0x15dc` | **`PspInheritQuota`** (`QuotaBlock` = 부모 쿼터 블록). `+0x1681` 은 다른 갈래 | [정적] |
| `nt!PspAllocateProcess` | `+0x1637` | `+0x163c` | `MmCreateProcessAddressSpace` (세션·전역 목록·커밋 처음 값·작업 집합 시작) | [정적] |
| `nt!PspAllocateProcess` | `+0x1922` | `+0x1927` | `PspInitializeProcessSecurity` (`SecurityDomain`·`ParentSecurityDomain` `+0x183 ~ +0x1a7`) | [정적] |
| `nt!PspAllocateProcess` | `+0x1a8b` · `+0x1afa` · `+0x1aea` / `+0x1be0` · `+0x1c82` · `+0x1eb3` | `+0x1a90` · `+0x1aff` · `+0x1aef` / `+0x1be5` · `+0x1c87` · `+0x1eb8` | 완화 옵션: IFEO 읽기 `PspReadIFEOMitigationOptions`·`PspReadIFEOMitigationAuditOptions` → 합치기 `PspInheritMitigationOptions` → 굳히기 `PspHardenMitigationOptions` → **켜기 `PspApplyMitigationOptions`** (`MitigationFlags` `+0xd1`, `MitigationFlags2` `+0x5a5`) | [정적] |
| `nt!PspAllocateProcess` | `+0x1e68` | `+0x1e6d` | `PspReadIFEOAntiTamperOptions` — 조건부, EPROCESS 칸 아님 (토큰 쪽) | [정적] |
| `nt!PspAllocateProcess` | `+0x2065` | `+0x206a` | **`PspInitializeFullProcessImageName`** (`SeAuditProcessCreationInfo` `+0x115`) | [정적] |
| `nt!PspInitializeFullProcessImageName` | `+0x77` | `+0x7c` | `PspSetProcessShortName` (`ImageFileName` 15바이트 `+0x48`·`+0x52`) | [정적] |
| `nt!PspAllocateProcess` | `+0x2090` | `+0x2095` | `MmInitializeProcessAddressSpace` (`VadRoot`·작업 집합·최고 주소·VAD) | [정적] |
| `nt!PspAllocateProcess` | `+0x2397` | `+0x239c` | `PspSetupUserProcessAddressSpace` → `+0x1b4` (ret `+0x1b9`) `PspCopyAndFixupParameters` (`+0x74 ~ +0x82` IFEO 키 없음 표시 → 매개변수 `Flags \|= 0x4000`) | [정적] |
| `nt!PspAllocateProcess` | `+0x27ff` | `+0x2804` | `KiQueryUnbiasedInterruptTime` (`CreateUnbiasedInterruptTime` `+0x280c`) | [정적] |
| `nt!PspAllocateProcess` | `+0x1215` · `+0x17bf` | `+0x121a` · `+0x17c4` | 간접 `cfg:@0xf04d28` · `cfg:@0xf04d20` (= `SeCiCallbacks+0xc8` · `+0xc0`) — **EPROCESS 칸은 안 씀**, 대상은 Q35 | [정적] |
| `nt!MmCreateProcessAddressSpace` | `+0x1da` · `+0x203` · `+0x21a` | `+0x1df` · `+0x208` · `+0x21f` | `PsReserveSessionMembership` (지금 도는 프로세스의 세션 + 참조) · `MiCreateNewProcessTopLevelMappings` (`MmProcessLinks` `+0x3d ~ +0x65`, `AddressSpaceInitialized` 비트 11 `+0x72`) · `PsJoinSession` (`Session` `+0x17`, `ProcessInSession` `+0x21`, `SessionProcessLinks` `+0x41 ~ +0x65`) | [정적] |
| `nt!MmInitializeProcessAddressSpace` | `+0xe8` · `+0x136` · `+0x181` · `+0x1a3` | `+0xed` · `+0x13b` · `+0x186` · `+0x1a8` | `MiInitializeWorkingSetList` · `MiComputeProcessUserVa` (`HighestUserAddress` `+0xa8`) · `MiInitializeProcessBottomUpEntropy` (`MitigationFlags` 후보 ⓑ `+0x19d`) · `MiInsertProcessVads` | [정적] |
| `nt!MiInsertProcessVads` | `+0x49` · `+0x5d` | `+0x4e` · `+0x62` | `MiInsertVadCharges` (쿼터 → `PsCharge…Quota` `+0x74`·`+0x153`) · `MiInsertVad` (`VadCount`·`VadHint`·`VirtualSize`·`PeakVirtualSize`) | [정적] |
| `nt!PspInsertProcess` | `+0x8f` · `+0xd4` · `+0x13e` | `+0x94` · `+0xd9` · `+0x143` | `PspInheritSyscallProvider` · `PspImplicitAssignProcessToJob` · `DbgkCopyProcessDebugPort` (`DebugPort` 0 `+0x27`) — 앞 둘은 부모에 값이 있을 때만 | [정적] |

### 호출 관계 (대표 지점)

```
NtCreateUserProcess
 ├─ (1) +0xc36  PspAllocateProcess                                                                  [정적]
 │       │  +0x539 ObCreateObjectEx (0 채움) → +0x564 memset → +0x564~+0x611 목록 머리 자기 연결·락 0
 │       │  +0x7a0 ExitStatus=0x103 · +0x7d6 InheritedFromUniqueProcessId · +0x799 DefaultHardErrorProcessing
 │       │  +0x7fe Flags · +0x819 Flags2 (부모 우선순위 비트만 바꿔 같은 DWORD 되쓰기)
 │       │  +0x728~+0x75c 확장 칸 포인터 셋 (디스크·네트워크·에너지 → PoEnergyContextInitialize)
 │       │  +0x88a PartitionObject · +0x95f SectionObject · +0xa37 ImageFilePointer
 │       ├─ +0xa61  RtlpOpenImageFileOptionsKeyEx      IFEO 키 없음 → +0xa6c CreateInfo+0x10 |= 0x40, +0xad0 값 읽기 건너뜀
 │       │  +0x1043 Machine (← +0xa8c PspSelectMachineForProcess)
 │       ├─ +0x15dc PspInheritQuota                    QuotaBlock = 부모 쿼터 블록
 │       ├─ +0x163c MmCreateProcessAddressSpace        Vm 시작 · SharedCommitLinks · +0x1ed CommitCharge=4 · +0x84 MmSlabIdentity · +0x73 PageCombineSequence
 │       │       ├─ +0x1df  PsReserveSessionMembership  지금 도는 프로세스의 세션 (+ 참조)
 │       │       ├─ +0x208  MiCreateNewProcessTopLevelMappings   +0x3d~+0x65 MmProcessLinks · +0x72 Flags 비트 11
 │       │       └─ +0x21f  PsJoinSession               +0x17 Session · +0x21 Flags 비트 16 · +0x41~+0x65 SessionProcessLinks
 │       │  +0x169c Flags 비트 18 (HasAddressSpace)
 │       ├─ +0x1927 PspInitializeProcessSecurity       +0x183~+0x1a7 SecurityDomain · ParentSecurityDomain (부모 값 복사)
 │       │  +0x1931 PriorityClass=2 → +0x194f 부모 클래스가 1·5 면 복사
 │       ├─ +0x1eb8 PspApplyMitigationOptions          +0xd1 MitigationFlags 0x20 · +0x5a5 MitigationFlags2 비트 30
 │       ├─ +0x206a PspInitializeFullProcessImageName  +0x115 SeAuditProcessCreationInfo
 │       │       └─ +0x7c   PspSetProcessShortName     +0x48·+0x52 ImageFileName "test.exe"
 │       ├─ +0x2095 MmInitializeProcessAddressSpace    +0xc3 VadRoot=0 · +0x117 Flags 비트 10
 │       │       ├─ +0x13b  MiComputeProcessUserVa     +0xa8 HighestUserAddress
 │       │       └─ +0x1a8  MiInsertProcessVads
 │       │               ├─ +0x4e  MiInsertVadCharges  쿼터 사용량 (PspChargeQuota 공용 부품)
 │       │               └─ +0x62  MiInsertVad         VadCount · VadHint · VirtualSize · PeakVirtualSize
 │       ├─ +0x239c PspSetupUserProcessAddressSpace
 │       │       └─ +0x1b9  PspCopyAndFixupParameters  +0x74~+0x82 CreateInfo+0x10 비트 0x40 → 매개변수 Flags |= 0x4000
 │       │  +0x27f5 CreateInterruptTime · +0x280c CreateUnbiasedInterruptTime (← +0x2804) · +0x281a LastAppStateUpdateTime
 └─ (2) +0xfc2  PspInsertProcess
         ├─ +0x94   PspInheritSyscallProvider          부모에 없음 → 안 탐
         ├─ +0xd9   PspImplicitAssignProcessToJob      부모 Job = 0 → 안 탐
         ├─ +0x143  DbgkCopyProcessDebugPort           +0x27 DebugPort = 0
         │  +0x202 Flags 비트 26 ProcessInserted (K2~K3 — Q78c2b 몫)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 쓰는 줄.
- 코드 순서는 오프셋 순이다. 단 오프셋 순서가 늘 실행 순서는 아니다 (flow.md 2.1 주의).

### 할당 — 층이 둘 (정정 보탬)

- `PspAllocateProcess +0x534 ObCreateObjectEx` → (`ObCreateObjectEx +0x148`) `ObpAllocateObject` → (**`ObpAllocateObject +0x197`**) **`ExAllocatePoolWithTag`** (rva `0xb80010`, 얇은 겉포장) → (**`ExAllocatePoolWithTag +0x9f`**) **`ExAllocatePool2`** (rva `0xb800f0`, 실제 할당) `[정적, .asm 없음 — objdump]`.
- 0 채움 비트: `ObpAllocateObject +0x191 BTS ECX,0xa` 가 켜고, `ExAllocatePoolWithTag +0x48 BT EBX,0xa; CMOVB` 가 그 비트로 "초기화 안 함" 을 안 붙이는 쪽으로 가른다. **run49 가 적은 `ExAllocatePoolWithTag +0x48` 은 그대로 맞다** — 그 줄은 0 채움을 가르는 줄이고, 실제 할당은 한 층 아래 `+0x9f` 다.
- 그 위에 `PspAllocateProcess +0x55f memset(…, 0, 크기)` 가 몸통 + 확장 영역을 한 번 더 0 으로 민다 (겹치는 안전장치).
- `PspAllocateProcess` 자기 안의 다른 풀 할당은 `ExAllocatePool2` 직접: `+0xd75` `'PsCs'` (허용 CPU 집합, IFEO 갈래), `+0xfc4` `'WowP'` (WoW64 칸 0x10B, 안 탐), `+0x213a` `'SePa'` (복제 길 이미지 경로, 안 탐).

### IFEO 키가 없는 길 (질문 3)

- **열기**: `+0xa5c RtlpOpenImageFileOptionsKeyEx` (이미지 이름 `CreateInfo+0xf0` → 키 핸들 `CreateInfo+0xc8`).
- **실패 `0xC0000034` (키 없음) 이면** `+0xa6c OR byte [R15+0x10],0x40` (`R15` = `CreateInfo`), `+0xa71` 이 키 핸들 칸을 0 으로. 그 뒤 `+0xad0` 이 `CreateInfo+0xc8` 을 읽어 0 이면 `+0xfa5` 로 **IFEO 값 읽기 구간을 통째로 건너뛴다** `[정적]`.
  - `PspReadOptionsMapFromIFEO` (완화 옵션 읽기 아래) 도 `+0xc8` 이 0 이면 `0xC000000D` 로 바로 돌아간다 (`+0x1f ~ +0x2b`, `.asm` 없음). 그래서 완화 옵션 칸 값은 **부모·시스템 기본 (`PspSystemMitigationOptions`)·이미지 헤더**에서만 온다.
- **IFEO 값 → 칸** (키가 있을 때, test.exe 는 다 안 탐 — 결과서 3.1 표):
  - `Debugger`·`AppExecutionAliasRedirect` → 만들기 포기 (칸 없음)
  - `UseLargePages` → 지역 플래그 (EPROCESS 칸 아님)
  - `NodeOptions` → `KPROCESS.IdealGlobalNode`, `Flags2.PropagateNode`
  - `ForceWakeCharge` → `Flags2.ForceWakeCharge`
  - `AllowedCpuSets` → 허용 CPU 집합 칸
  - `PerfOptions` (`IoPriority`·`PagePriority`) → `Flags.DefaultIoPriority`·`Flags2.DefaultPagePriority`·`PriorityClass`
  - `ImageExpansionMitigation` = 2 → `Flags4.ImageExpansionDisable`
  - `MitigationOptions`·`MitigationAuditOptions` → `MitigationFlags*`, `SignatureLevel`·`SectionSignatureLevel` (조건부)
  - `AntiTamperOptions` → 토큰 쪽 (EPROCESS 칸 아님)
- **표시가 ntdll 까지 간다**: `CreateInfo+0x10` 비트 `0x40` 을 `PspSetupUserProcessAddressSpace +0x1b4 → PspCopyAndFixupParameters +0x74 TEST byte [R8+0x10],0x40` 이 보고, `+0x7b` 가 `CreateInfo+0x11` 비트 0 이 꺼졌는지 본 뒤 **`+0x82 BTS dword [RBX+0x8],0xe`** — 새 프로세스 매개변수 `Flags |= 0x4000` `[정적]`.
  - 이것이 run44 사진 P1 `Flags = 0x4001` 의 `0x4000` 이고, run46 이 본 "ntdll `LdrpInitializeExecutionOptions` 가 `0x4000` 때문에 IFEO 를 건너뛴다" 의 출처다 → **run46 ntdll 쪽과 같은 결과** (이 exe 는 IFEO 키가 없다).
  - `PspCopyAndFixupParameters` 가 받는 `R8` 이 `CreateInfo` 라는 연결은 `.asm` 인자 흐름으로 본 것 `[정적 + 추측]`.
  - run46 이 본 대로 ntdll 힙 관리자는 IFEO **바탕 키**를 따로 열어 `GCInterval` 을 읽는다. 커널의 이 읽기와는 별개다.
  - 같이 기억할 것: test.exe 의 `MitigationFlags = 0x20` (HIGH_ENTROPY_VA) 은 IFEO 가 아니라 **PE `DllCharacteristics`** (`CreateInfo+0x66`) → `PspApplyMitigationOptions` 인자 비트 → `+0xd1`.

### 끝까지 0 160칸 가르기 (질문 2)

- 판단 기준 (결과서 한 줄): 만들기 길 함수의 `.asm` 에서 칸 오프셋으로 쓰는 줄이 있으면 → 값 0 을 쓰거나 같은 DWORD·QWORD 를 읽어 되쓰면 **0 을 씀**, 켜는 줄인데 test.exe 조건이 안 맞으면 **조건부 안 탐**. 줄이 없으면 커널 전체 훑기로 이름 있는 EPROCESS 쪽 쓰는 함수가 보이면 **범위 밖 길 `[추측]`**, 안 보이면 **아무도 안 씀**.
- **0 을 씀 50**:
  - 명시 0 저장 11 — `RundownProtect` (`PAP +0x58c`) · `AddressCreationLock`·`PageTableCommitmentLock` (`MmInitializeProcessAddressSpace +0xb5`·`+0xbc`) · `SharedCommitLock` (`MmCreateProcessAddressSpace +0xee`) · `EnclaveLock`·`DynamicEHContinuationTargetsTree`·`DynamicEHContinuationTargetsLock`·`DynamicEnforcedCetCompatibleRanges`·`EnableOptionalXStateFeaturesLock`·`PpmPolicyLock` (`PAP +0x5e7 ~ +0x611`, `memset` 직후 묶음) · `DebugPort` (`DbgkCopyProcessDebugPort +0x27`)
  - 입력값 0 을 씀 4 — `Protection`·`SignatureLevel`·`SectionSignatureLevel` (`PAP +0x5c4`·`+0x5d2`·`+0x5e0`, 값 = 인자 4·5·6 — 부모 보호 속성을 검증한 값, cmd 가 보호 프로세스가 아니라 0) · `ServerSilo` (`PAP +0x97f`, 서버 실로 속성 없음)
  - 같은 DWORD 되쓰기 35 — `Flags2` 19비트 (`PAP +0x819`) · `Flags` 16비트 (`PAP +0x7fe`). 비트 이름은 짐 지도 3.11 줄
- **조건부 안 탐 30** — 갈래별로 (칸마다 줄·조건은 짐 지도 3.11):
  - 복제 길 (섹션 없음): `Flags2.EnableReadVmLogging`, `Flags.ProcessDelete` (이름 어색 `[추측]`), `ForkInProgress`
  - WoW64 x86: `WoW64Process`, `Flags.Wow64VaSpace4Gb`, `Flags.OverrideAddressSpace` (기계 고르기 출력)
  - 작업 (Job): `Job`·`JobLinks`·`CommitChargeJob` (부모 `Job` = 0, 칸 쓰는 줄은 callee 안이라 안 읽음 `[추측]`)
  - 디버그 포트: `Flags.NoDebugInherit` (부모 디버그 포트 없음)
  - IFEO 값: `Flags2.ForceWakeCharge`, `Flags2.PropagateNode`·`ExplicitAffinity` (부모 비트도 없음), `Flags4.ImageExpansionDisable`
  - 세션: `Flags2.CrossSessionCreate` (`PspAttachSession` 갈래), `Flags4.SessionFirstProcess` (세션 새로 만들기 — smss 만)
  - CPU 집합 (그룹 2개 이상 / IFEO): `AllowedCpuSets`·`DefaultCpuSets`·`AllowedCpuSetsIndirect`·`DefaultCpuSetsIndirect`
  - 속성 없음: `Win32KFilterSet`, `DisabledComponentFlags`
  - 부모에 없음: `SyscallProvider`·`SyscallProviderProcessLinks`·`SyscallProviderDispatchContext`
  - 전역 조건: `Flags.VmTopDown` (`MmRegistryState+0x20`), `LockedPagesList` (`MmTrackLockedPages`) — 사진 0 으로 꺼짐 `[관찰 간접]`, 전역 값은 E2
  - 그 밖: `LargePrivateVadCount` (큰 페이지 VAD 아님), `MitigationFlags3`·`MitigationFlags3Values` (합친 옵션 비트 꺼짐)
- **아무도 안 씀 (만들기 길 안) 28**: `ExceptionPortValue.ExceptionPortState`, `MmReserved`, `RotateInProgress`, `CloneRoot`, `MmReserved2`, `Win32WindowStation`, `Spare1`, `PageDirectoryPte`, `SecurityPort`, `WorkingSetWatch`, `CommitChargeLimit`, `VadPhysicalPagesLimit`, `SvmProcessLock`, `SvmLock`, `LastFreezeInterruptTime`, `EnclaveTable`, `EnclaveNumber`, `HighPriorityFaultsAllowed`, `DxgProcess`, `Spare0`, `ProcessTimerDelay`, `VirtualTimerListLock`, `SupervisorDeviceAsid`, `SupervisorSvmData`, `FreezeWorkLinks`, `Flags3.SubsystemProcess`, `Flags3.VmProcessorHostTransition`, `Flags3.EnableProcessImpersonationLogging`.
  - 훑기에 걸린 것은 오탐뿐이었다 (예: `SecurityPort +0x348` 은 세션 전역·PEB 칸, `WorkingSetWatch +0x2c0` 은 `MmCreatePeb` 의 PEB 칸). `DxgProcess` 는 `dxgkrnl.sys` 가 쓸 칸 `[추측]`.
- **범위 밖 길에서 씀 `[추측]` 52** (전체 훑기, 오프셋 숫자 일치라 틀릴 수 있음):

| 칸 | 쓰는 곳 (범위 밖) `[추측]` |
|---|---|
| `Flags3.Crashed` · `.HangCount` · `.GhostCount` · `.PrefilterException` | `PsSetProcessFaultInformation`, `DbgkForwardException` |
| `Flags3.JobVadsAreTracked` · `.VadTrackingDisabled` | `MmLinkJobProcess +0x12f`, 이름 없는 조각 `FUN_140b18863 +0x19/+0x21` |
| `Flags3.ReplacingPageRoot` · `.RelinquishedCommit` | `MiStealPage +0x140b`, `MiReAcquireOutSwappedProcessCommit +0xd9` |
| `Flags3.HighGraphicsPriority` | `NtSetInformationProcess` (짐작) |
| `Flags3.CommitFailLogged` · `.ReserveFailLogged` | `MiLogCommitRequestFailed`, `MiLogReserveVaFailed` |
| `Flags3.AllImagesAtBasePristineBase` | `VmCreateMemoryProcess +0x17a` |
| `Flags3.ForegroundExternal` · `.ForegroundSystem` | `PspSetProcessForegroundBackgroundRequest` |
| `Flags3.HighMemoryPriority` | `SmProcessStoreMemoryPriorityRequest +0x105` |
| `Flags3.SecurityDomainChanged` | `PspCombineSecurityDomains +0x273`, `PspOneDirectionSecurityDomainCombine +0x1bc` |
| `Flags3.AltSyscall` · `Flags3.TimerResolutionIgnore` | `PspSyscallProviderOptIn +0x6b`, `ExUpdateTimerResolutionPolicy` |
| `LastThreadExitStatus` · `ExitTime` · `ThreadListLock` | `PspExitThread`, `PspExitLastThread`, `PspInsertThread` (푸시락 잡았다 놓음) |
| `VadPhysicalPages` | `MiCleanProcessPhysicalPages`, `MiAllocateUserPhysicalPages` |
| `TimerResolutionLink` · `TimerResolutionStackRecord` · `RequestedTimerResolution` · `SmallestTimerResolution` · `KTimerSets` · `KTimer2Sets` · `ThreadTimerSets` | `NtSetTimerResolution`, `ExCleanTimerResolutionRequest` 와 타이머·스케줄러 카운터 |
| `InvertedFunctionTableLock` | `RtlInsertDynamicUserFunctionTable` |
| `DeviceAsid` · `SvmData` | `ExpShareAddressSpaceWithDevice` |
| `PicoContext` · `VmContext` | `PsCreateMinimalProcess`/피코 쪽, `VmpProcessContextSetup` |
| `TotalUnbiasedFrozenTime` | `PsThawMultiProcess` |
| `.LastAppStateUptime` · `.LastAppState` | `NtSetInformationProcess` 쪽 |
| `DiskIoAttribution` | `IoSetDiskIoAttributionOnProcess` |
| `Read/Write/OtherOperationCount` · `Read/Write/OtherTransferCount` (6) | I/O 완료 `IopUpdateIrpTransferCount`, `KeFoldProcessStatisticsThread` |
| `CoverageSamplerContext` · `MmHotPatchContext` | `EtwpCovSampProcessEnsureContext`, `MiGetProcessHotPatchContext` |
| `PathRedirectionHashes` | `PspRundownSingleProcess` (실패 정리 길 `PAP +0x3727` 이기도), `RtlSetProcessPlaceholderCompatibilityMode` |
| `SyscallUsage` · `SyscallUsageValues` | `EtwTiLogSyscallUsage` |
| `Execution` · `ThreadIndexTable` · `Flags4.MinimalTerminate` | `PspChangeProcessExecutionState`, `PspQueryThreadIndexInformation`, `PsTerminateMinimalProcess` |

- 이 52 + `K1 이전` 의 `NumberOfPrivatePages` 1 (K1 `0x20`, 페이지 오류 `MiCompletePrivateZeroFault +0xc4a`·`+0x1144` 의 `lock add/inc [+0x288]` 로 보임 `[추측]`) = 53.

### test.exe 가 안 타는 큰 갈래

| 갈래 | 가르는 곳 | test.exe |
|---|---|---|
| 작업 (Job) | `PspInsertProcess +0x7d` 부모 `Job` | 안 탐 (사진 `Job` = 0) |
| 디버그 포트 | `DbgkCopyProcessDebugPort` 가 부모 `+0x308` 복사 / 디버그 객체 핸들 (`PspInsertProcess +0x2f3`) | 안 탐 (사진 `DebugPort` = 0) |
| WoW64 | `Machine == 0x14c` · 지역 플래그 `0x40` | 안 탐 (`Machine` = `0x8664`) |
| 보호 프로세스 | `Protection`·`SignatureLevel`·`SectionSignatureLevel` 입력 | 값 0 을 씀 (cmd 가 보호 프로세스 아님 `[관찰 간접]`) |
| 서버 실로 | `CreateInfo+0x1a8` | 0 을 씀 |
| 피코 | `PspCreatePicoProcess` — 다른 시스템 콜 | `NtCreateUserProcess` 길 아님 |
| 서브시스템 프로세스 | 만들기 길에 켜는 줄 없음 | 아무도 안 씀 |
| 복제 (섹션 없음) | 지역 플래그 비트 `4` (`PAP +0x2ec ~ +0x317`) | 안 탐 (섹션 있음) |
| 세션 새로 만들기 | ProcessFlags 비트 7 (`MmInitializeProcessAddressSpace +0x392`) | 안 탐 (smss 만) |
| 세션 다름 → `PspAttachSession` | 토큰 핸들 속성 / 복제 | 안 탐 |
| 쿼터 블록 새로 → `PspAssignProcessQuotaBlock` | 12번째 인자 (토큰 핸들 속성) ≠ 0 (`+0x1574`) | 안 탐 |

### 간접 호출 두 곳 (질문 3)

- `PAP +0x1215` (ret `+0x121a`) — `SeCiCallbacks+0xc8` (`+0x1201` 이 `[0x140f04d28]` 을 읽음). 인자는 0x218B 지역 버퍼, 실패·정리 블록 (`+0x1189 ~ +0x121c`) 안이라 버퍼를 비우는 호출로 보임 `[추측]`. **EPROCESS 칸은 안 쓴다**.
- `PAP +0x17bf` (ret `+0x17c4`) — `SeCiCallbacks+0xc0` (`+0x17a7`). 인자 `RCX = CreateInfo+0xb8` (이미지 파일 객체), `RDX` = 같은 지역 버퍼. 결과 버퍼가 `PspInitializeProcessSecurity` 인자로 넘어가 토큰 쪽 입력이 된다. **EPROCESS 칸은 안 쓴다**.
- 둘 다 대상은 CI 가 등록한 콜백으로 보이지만 `[추측]` (`_refs/SeCiCallbacks.tsv`: 읽는 줄은 이 둘뿐, 쓰는 줄은 표 통째 복사라 안 보임) → 대상은 그대로 **Q35**. 이번에 새로 안 것은 "칸을 직접 쓰지 않는다" 와 인자.

### 읽는 입력

- **부모 프로세스** (cmd): `UniqueProcessId`, `Flags` 비트 27~29, `Flags2` 비트 12~14, `DefaultHardErrorProcessing`, `PriorityClass` (1·5 일 때만), 쿼터 블록, `SecurityDomain`, `Job`, `DebugPort`, `SyscallProvider`, 보호 속성 (`NtCreateUserProcess +0xb5d ~ +0xb71`).
- **지금 도는 프로세스** (= cmd): 세션 (`PsReserveSessionMembership`, `GS:[0x188]` → 프로세스 `+0x2e8`).
- 인자·속성 모음: 섹션 (7번째), 토큰 핸들 속성 유무 (12번째), 파티션 속성 (13번째), `CreateInfo` (`+0xb8` 이미지 파일 객체, `+0xf0` 이미지 이름, `+0x66` `DllCharacteristics`, `+0x1a8` 서버 실로, `+0x1b0`·`+0x1b8` Win32k 필터·끈 부품).
- 레지스트리: IFEO 키 (test.exe 는 없음).
- 커널 전역: `PspSystemPartition`, `PspSystemMitigationOptions`, `PsNextSecurityDomain`, `PsDisableDiskCounters`, `PoEnergyEstimationEnabled`, `MmRegistryState+0x20`, `MmTrackLockedPages`, 전역 프로세스 목록 (`0x140e2db98`, PDB 이름 안 찾음), 공유 데이터 `0xfffff78000000008` (InterruptTime).
- 이 조각 안에는 `syscall` 이 없다 (전부 부모 `NtCreateUserProcess` 시스템 콜 안).

## 확인된 것

- **`K1 이전` 52칸 중 50칸의 처음 값을 쓰는 줄** — `PspAllocateProcess` 본문과 `MmCreateProcessAddressSpace`·`MiCreateNewProcessTopLevelMappings`·`PsReserveSessionMembership`·`PsJoinSession`·`MmInitializeProcessAddressSpace`·`MiComputeProcessUserVa`·`MiInsertProcessVads`·`MiInsertVad`·`PspInheritQuota`·`PspInitializeFullProcessImageName`·`PspSetProcessShortName`·`PspInitializeProcessSecurity`·`PspApplyMitigationOptions`. `[정적]` · 사진 K1 값과 칸마다 맞음 (짐 지도 3.11)
- **`PriorityClass` 2 를 처음 넣는 줄은 `PspAllocateProcess +0x1931 MOV byte [R14+0x347],2`**, 이어 `+0x194f` 가 부모 클래스가 1·5 면 복사 `[정적]` (오케스트레이터·이 문서 둘 다 `.asm` 확인). run49 메모 "처음 넣는 줄 못 찾음" 닫힘
- **`Flags.AddressSpaceInitialized` 두 비트는 다른 함수**: 비트 11 = `MiCreateNewProcessTopLevelMappings +0x72` (`lock or 0x800`, 최상위 페이지 표 직후), 비트 10 = `MmInitializeProcessAddressSpace +0x117` (`lock or 0x400`, 작업 집합·VAD 준비 끝). 사진 K1 값 3 과 맞음 `[정적]`
- **run49 의 이름 모르던 셋**: `+0x540` = `MmProcessLinks` (전역 프로세스 목록 꼬리, `MiCreateNewProcessTopLevelMappings +0x3d ~ +0x65`), `+0x3d8` = `CommitCharge` (처음 값 4, `MmCreateProcessAddressSpace +0x1ed`), `+0x410` = `Vm.Instance.VmWorkingSetList` (`Vm+0x10`, 풀 블록 포인터 `'MiP2'`) `[정적]`
- **`Session` 은 부모가 아니라 호출한 쪽 (지금 도는 프로세스) 의 세션** — `PsReserveSessionMembership +0x23`·`+0x3e` → `PsJoinSession +0x17` `[정적]`. `InheritedFromUniqueProcessId`·우선순위 비트는 **부모** 에서 온다. cmd 가 띄우는 test.exe 는 둘이 같다
- **`SecurityDomain` 기본은 부모 값 복사**, 새 번호 (`PsNextSecurityDomain` +1) 는 예외 갈래 셋 (작업 breakaway · 부모 `IsolateSecurityDomain` · `VmProcessorHost` 이면서 부모 값 0) `[정적]`. 부모 값과 같은지는 E1
- **IFEO 키가 없으면 커널은 IFEO 값을 하나도 안 읽고, 그 표시가 매개변수 `Flags 0x4000` 이 된다** — `PAP +0xa6c` → `+0xad0` 건너뜀, `PspCopyAndFixupParameters +0x74 ~ +0x82` `[정적]`. run46 의 ntdll 쪽 (`0x4001`) 과 이어짐
- **간접 호출 두 곳은 EPROCESS 칸을 안 쓴다** `[정적]`
- 끝까지 0 160칸은 만들기 길 안에서 0 을 씀 50 · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 길 52 `[추측]` 로 갈렸다. 여전히 모름은 `SharedCommitCharge` 1칸

## 정정

- **run49 `ObpAllocateObject +0x191` → `ExAllocatePoolWithTag +0x48` — 틀린 게 아니라 한 층 더 있다** (보탬). `ExAllocatePoolWithTag` 는 겉포장이고, 그 `+0x9f` 가 실제 할당 `ExAllocatePool2` 를 부른다. `ObpAllocateObject` 가 `ExAllocatePoolWithTag` 를 부르는 자리는 `+0x197` (`+0x191` 은 0 채움 비트를 켜는 줄). 결과서가 "문서 사이에서 할당 함수 이름이 어긋났던 것" 이라 부른 건 이 두 층을 섞어 부른 것이다. run49 문서는 안 고쳤다 (그 문장 그대로 맞음).
- **baggage 3.11 "(run49) Q78c2 에서 볼 것" 메모 — 고친 셋**:
  - `SecurityDomain`·`ParentSecurityDomain` "부모 값 또는 `PsNextSecurityDomain` +1" → **기본은 부모 값 복사, +1 은 예외 갈래 셋** (조건을 붙임).
  - `PriorityClass` "K1 값 2 를 처음 넣는 줄은 못 찾음" → `PspAllocateProcess +0x1931`.
  - `Flags.AddressSpaceInitialized` "bit 11 ← `MiCreateNewProcessTopLevelMappings +0x72`" → 맞음, 그런데 **bit 10 은 다른 함수** (`MmInitializeProcessAddressSpace +0x117`).
  - 짐 지도는 이 스프린트 문서라 메모 자리를 이번에 옮긴 줄로 바로 고쳤다.
- 결과서 1.1 `MitigationFlags2` 경로 칸의 "`PspInheritMitigationOptions (호출 +0x1aea) → PspApplyMitigationOptions (호출 +0x1eb3)`" 는 `_calls.tsv` 로는 둘 다 `PspAllocateProcess` 가 부른다 (`+0x1aea`·`+0x1eb3`). 짐 지도엔 `①P… → PspApplyMitigationOptions (호출 +0x1eb3) +0x5a5` 로 적고, 합친 옵션 입력이 `+0x1aea` 에서 온다고 따로 적었다.
- **run10 "`+0x2095` 보통 길 (부모 없음, 섹션 있음) / `+0x2113` 복제 길 (부모 있음, 섹션 없음)" — 부모 있음·없음은 틀렸다** (결과서 정정 7). `PspAllocateProcess` 지역 플래그 비트 `4` 는 **"부모를 복제 (섹션 없음)"** 이다 (`+0x2ec ~ +0x317`: 7번째 인자 섹션이 0 일 때만 켜짐). test.exe 는 부모 cmd 가 있는데 섹션이 있어 보통 길이다. run10 문서 맨 위에 정정 한 줄, flow.md 2.1 `MmInitializeProcessAddressSpace` 절 두 줄을 고쳤다. run49 문서·짐 지도에는 같은 표현이 없다 (run49 의 `[RSP+0xf8]=4` 는 다른 변수 — `EBX & 0x10002 == 0` 이고 부모가 있을 때 — 라 그대로 맞음).

## 아직 확인 필요

- 동적 (결과서 번호 그대로, E1 ~ E9). **E1·E2·E9 는 이미 있는 덤프로 → plan Q79 에 출처로**, **E4 → plan Q35 에 출처로**, **E3·E5 ~ E8 → plan Q97 (낮은 우선순위)**. 멈춤점은 결과서 "동적분석으로 확인해야 할 것" 표, test.exe 일 때만 서게 하는 조건은 dast-mentor 몫. 급한 것은 없다 — 이것들을 안 닫아도 칸 표는 완성돼 있다.
  - **E1** (덤프, Q79) 자식 값이 부모 (cmd) 값 복사가 맞나: `SecurityDomain`·`ParentSecurityDomain`, `Flags` 비트 27:3, `Flags2` 비트 12:3, `DefaultHardErrorProcessing`, `PriorityClass`, `QuotaBlock`, `Session`, `Job`, `DebugPort`, `SyscallProvider`, `Protection`/`SignatureLevel`. K1 덤프에서 `!process 19a8 0` → `dt nt!_EPROCESS` 후 자식 K1 값과 대조 (sast/43 D1 과 같은 덤프).
  - **E2** (덤프, Q79) 커널 전역 값: `PspSystemPartition` (`PartitionObject` 대조), `PspSystemMitigationOptions` 24B (`MitigationFlags2` 비트 30 의 뿌리), `nt+0xfc6ac8` 바이트 (`& 0x30`, `MitigationFlags` 후보 ⓑ 조건), `MmRegistryState+0x20` (`nt+0xfc5210`, `VmTopDown`), `MmTrackLockedPages` (`nt+0xfc6bfc`). `dq`/`dd`/`db` 한 번씩.
  - **E3** (멈춤, Q97) IFEO 키 없음의 직접 증거: `nt!PspAllocateProcess+0xa61` (rva `0xa02fc9`) 에서 `r eax` (= `0xC0000034`), `db @r15+0x10 L1` (비트 `0x40`). 조건: 지금 도는 프로세스 이미지 = cmd.exe (새 프로세스는 아직 이름이 없음). 결과는 이미 사진 P1 `Flags=0x4001` 로 간접 확인됨.
  - **E4** (멈춤, Q35) 간접 호출 `SeCiCallbacks+0xc0`/`+0xc8` 대상과 시점 — Q35 그대로.
  - **E5** (멈춤, Q97) `SharedCommitCharge` K1 `0x2b` 를 올린 줄: `nt!MiInsertSharedCommitNode+0x33e` 에서 `k` (조건: `rcx` = 새 EPROCESS).
  - **E6** (멈춤, Q97) `NumberOfPrivatePages` K1 `0x20` 이 페이지 오류로 오른 것인가: 새 프로세스를 만든 직후 `ba w8 <eproc>+0x288` 콜스택.
  - **E7** (멈춤, Q97) `MitigationFlags = 0x20` 을 실제로 켠 줄: `PspApplyMitigationOptions +0xd1` / `MiInitializeProcessBottomUpEntropy +0x19d` 두 줄에 걸고 먼저 오는 쪽.
  - **E8** (멈춤, Q97) 쿼터 두 칸 (`0x4d8`·`0x2998`) 의 종류 번호 ↔ 칸 대응과 어느 호출이 만들었나: `nt!PspChargeQuota+0x95` 에서 `r8d` (종류)·`r9` (양).
  - **E9** (덤프, Q79) `Job`·`DebugPort` 부모 값이 정말 0 인가 (안 탐 확정) — E1 과 같은 덤프.
- 정적 (Q78c2b 로 넘김 — 짐 지도 3.11 머리 "Q78c2b 에서 볼 것" 에 메모):
  - `Flags.ProcessInserted` (`K2~K3`, 비트 26) ← **`PspInsertProcess +0x202`** (`lock or 0x4000000`, 이 문서도 `.asm` 확인). 사진 `K2~K3` 와 맞음 → 닫을 수 있음.
  - `ActiveThreadsHighWatermark` (`K2~K3`) ← `PspInsertThread +0x678`, `Flags4.ThreadWasActive` ← `+0x67f`.
  - `Flags2.PrimaryTokenFrozen` (`P0~P1`) 을 켜는 줄이 둘 — `PspDisablePrimaryTokenExchange +0x22` (run49) 와 **`PspInsertThread +0x77b`** (`BTS.LOCK [R15+0x1f0],0xf`, 이 문서도 `.asm` 확인. 조건은 안 읽음).
  - `ExceptionPortData` (`K3~P0`): `PspAllocateProcess` 에 직접 쓰는 줄 없음. `Flags2.EnableWriteVmLogging`·`Flags3.Enable…Logging` 비트·`WakeChannel`·`WakeInfo`·`Flags.CreateReported`·`LaunchPrefetched`·`ImageNotifyDone`: 만들기 길에서 못 찾음 → `NtSetInformationProcess`·프로세스 알림 쪽.
- 정적, 작은 것 (갈 곳은 오케스트레이터):
  - `Flags.ProcessDelete` 를 켜는 줄 (`PAP +0xf93` → `+0x13f6`) 이 정말 이 칸인지 — 비트 이름과 뜻이 어색함 `[추측]`.
  - `Job`·`JobLinks`·`CommitChargeJob` 을 실제로 쓰는 줄 (`PspImplicitAssignProcessToJob` 아래, 안 읽음).
  - 전역 프로세스 목록 머리 `0x140e2db98` 의 PDB 이름 (`pdb_aliases.py --lookup`).
  - "범위 밖 길" 52·"아무도 안 씀" 28 의 쓰는 함수를 `.asm` 로 한 줄씩 확인 (지금은 오프셋 숫자 훑기). 결과서 도구 제안 (`field_writers.py`) 이 있으면 쉬워짐.
- 칸 표가 없는 짐 후보 (결과서 정정 8) → 짐 지도 7.6 에 **기록만**: 확장 영역 안 `_PROCESS_DISK_COUNTERS` (0x28B) · `_PO_PROCESS_ENERGY_CONTEXT` (0x1e8B) · `_PROCESS_NETWORK_COUNTERS` (크기 안 봄), 풀 블록 `_MMWSL_INSTANCE` (0x580B, `'MiP2'`), 쿼터 블록 `_EPROCESS_QUOTA_BLOCK` (PDB 정의 없음, 부모와 공유), 세션 구조 `_PSP_SESSION_SPACE`.

## 그림

- flow.md 그림 (mermaid) 은 안 바꿨다. 이번 바퀴는 `PspAllocateProcess` 안 대표 지점이 늘었을 뿐 (쿼터·세션·완화 옵션·이미지 이름·IFEO 키 열기) 새 단계·새 caller 가 없다. 2.1 글 트리에 run49 처럼 `·` 줄로만 넣었다 ("그림엔 없음").

## 도구 메모

- 결과서 "도구 평가" — 하네스라 오케스트레이터가 사용자에게:
  - **제안 (sast/43 제안 1 을 다시, 더 구체적으로)**: `scripts/field_writers.py <바이너리> --struct _EPROCESS [--cells K1.tsv]` — `pdb_struct.py` 의 직접 칸 오프셋 전체에 대해 `[reg+오프셋]` 쓰기 (mov/or/and/bts/btr/xadd/cmpxchg, `lock` 포함) 를 `칸 / 함수+오프셋 / 명령 / 지도 깊이` TSV 로. 오탐 (`KeWaitForMutexObject`·`MmCreatePeb`·`ExReleaseResourceLite` 가 걸림) 을 줄이려면 **레지스터가 어디서 왔는지** (예: `MOV R14,RCX` 가 첫 인자인지) 도 같이.
  - `pdb_struct.py --expand` (앞 바퀴 제안) 는 이번에도 `Flags`·`Flags2`·`Flags3`·`MitigationFlags*` 비트에 필요했다 (sast/38 `struct__EPROCESS.tsv` 로 대신 봄).
- 이번 바퀴엔 디버거를 안 붙였다. 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

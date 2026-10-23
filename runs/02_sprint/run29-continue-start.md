# 2026-10-23 run29 — 로더 끝 → 진입점: `NtContinue` 는 **SYSCALL `0x43`** 으로 커널 `NtContinueEx` 에 가고, 유저 길 **`KiContinuePreviousModeUser +0x422`** 가 맡긴 CONTEXT 를 트랩 프레임에 옮겨 적은 뒤 **`NtContinueEx` 가 반환 없이 직접 `IRETQ`** 로 `RtlUserThreadStart` 에 보낸다. `RtlUserThreadStart` 는 칸 `Kernel32ThreadInitThunkFunction` 을 **`(0, 진입점, PEB)`** 로 부른다 (ret `+0x2c`) (정적)

> **정정 (run30)**: 아래 "`main` 에서 `k` 예상" 의 `test!mainCRTStartup+?` 프레임은 안 보일 것이다. `mainCRTStartup` 은 `+0xd` 에서 `JMP` 로 `__scrt_common_main_seh` 에 넘겨 프레임이 없다 (run30 `[정적]`). 예상은 `test!main` → `test!__scrt_common_main_seh+0x10f` → `BaseThreadInitThunk+0x17` → `RtlUserThreadStart+0x2c`.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntoskrnl 26100.9457, pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` · ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE1` (둘 다 flow.md 기준과 같음). 이미 analyze 된 프로젝트를 다시 썼다 (analyze 안 돌림).
- 버전 대조 (결과서): `NtCreateUserProcess` ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` ret `+0x807` → `PspCallProcessNotifyRoutines`, ntdll `LdrInitializeThunk` ret `+0xe` → `LdrpInitialize` 가 `_calls.tsv` 와 같다. run16 의 `LdrInitializeThunk` ret `+0x18` (`NtContinue`)·`+0x1f` (`RtlRaiseStatus`) 도 `.asm` 과 같다.
- 이름: 대상 여섯 모두 `_index.tsv` 에 PDB 이름 그대로 있다.

| 함수 | rva | 크기 (`_index.tsv`) | 직접 호출 (`_calls.tsv`) |
|---|---|---|---|
| `nt!NtContinue` | `0x6adde0` | 5바이트 | 0 (꼬리 `JMP` 1) |
| `nt!NtContinueEx` | `0x6addf0` | 710바이트 | 7곳 + 꼬리 `JMP` 1 |
| `nt!KiContinueEx` | `0x4241b0` | 512바이트 | 9곳 |
| `nt!KiContinuePreviousModeUser` | `0x2bc750` | 1226바이트 | 16곳 + 조각 1곳 (조각 안 2곳) |
| `ntdll!RtlUserThreadStart` | `0xacac0` | 91바이트 | 5곳 + 예외 필터 3곳 |
| `ntdll!NtContinue` | `0x161610` | 24바이트 | 0 (`SYSCALL`) |

- `_KCONTINUE_ARGUMENT` 구조체는 공개 PDB 에 없다 (열거형 `_KCONTINUE_TYPE` 만). 그 칸은 결과서가 `.asm` 으로 읽은 오프셋+역할이다.

## 목적

- Q17: 그림 2 `PH4` 의 "커널 `NtContinue` 안은 공백 (추측)" 굵은 화살표를 코드 사슬로 채우기.
  - 커널 입구 `nt!NtContinue` → `NtContinueEx` 에서 둘째 인자(`TRUE`)가 어떻게 되나, 유저 길이 어느 함수로 가나.
  - 맡긴 CONTEXT 를 어느 함수 어느 줄에서 트랩 프레임에 옮기나 (run16 후보 둘 중 어느 쪽), 그 전 검사, CET 맞춰 보기.
  - TestAlert 가 하는 일, 유저로 돌아가는 방법 (`Rcx` = 진입점이 살아서 가나), 실패하면 무엇을 돌려주나.
  - `ntdll!RtlUserThreadStart` 가 받는 인자, 칸 `Kernel32ThreadInitThunkFunction` 을 어떤 인자로 부르나 (ret `+0x2c` 인가), 칸이 0 일 때, 예외 처리.

## 방법

- 정적: `_workspace/sast/24_continue-start.md` — ntoskrnl `^(NtContinue|NtContinueEx|KiContinueEx|KiContinuePreviousModeUser)$` depth 1 (31개), ntdll `^(RtlUserThreadStart|NtContinue)$` depth 1 (6개). 지도(`map`)는 안 돌림.
  - callee 본문은 거의 안 읽었다. 예외: `KeTestAlertThread` 전체, `KiVerifyContextIpForUserCet` 앞쪽, `KePopulateContinuationContext` (run16 에서 뽑힘), `KxContextToKframes`·`KeContextToKframes` 는 `ContextFlags` 거르는 줄만, `PspInitializeThunkContext` 는 `ContextFlags` 값 몇 줄.
  - 시스템 호출 출구(`KiSystemCall64`·`KiSystemServiceExit`·`KiKernelExit`)는 본문을 안 읽었다. 위치만 `pdb_aliases.py --where` 로.
- 결과서: `_workspace/sast/24_continue-start_result.md` (**분석성공**)
- 근거 파일 (레포 밖, 커밋 안 함): `ms/disassemble/ntoskrnl/{NtContinue,NtContinueEx,KiContinueEx,KiContinuePreviousModeUser,KeTestAlertThread,KiVerifyContextIpForUserCet}.asm`, `ms/disassemble/ntdll/{RtlUserThreadStart,NtContinue}.asm`, `ms/disassemble/kernel32/BaseThreadInitThunk.asm`·CFG 중개 `.asm`, 각 `_calls.tsv`.
- 예외 범위는 결과서가 PE unwind 표(`pefile`)로, 서비스 표 `KiServiceTable` (PDB 공개 이름, rva `0xdadc0`, 489칸) 은 PE 파일에서 읽었다. 구조체 칸은 `llvm-pdbutil` 로 PDB 타입을 봤다.
- 이번에 `.asm` 과 다시 맞춰 본 줄: `NtContinueEx +0x81/+0x86`·`+0x88`·`+0x148`·`+0x3ea`·`+0x403`·`+0x40c`·`+0x426`, `KiContinueEx +0x5e`·`+0x66`·`+0x71`·`+0x78`·`+0xc9`·`+0xeb`, `RtlUserThreadStart +0x7`·`+0x11`·`+0x25`·`+0x27/+0x2c`·`+0x4a`·`+0x50`, ntdll `NtContinue +0x3`·`+0x12`, kernel32 `BaseThreadInitThunk +0x12/+0x17` 과 그 CFG 중개(`JMP` 둘). 어긋난 곳 없음.

## 결과

### 정적 — 호출 지점 (`_calls.tsv`, 대표만)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 (요약) | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrInitializeThunk` | `+0x13` | `+0x18` | `ntdll!NtContinue(맡긴 CONTEXT, DL = 1)` | 늘 (run16) | [정적] |
| `ntdll!NtContinue` | `+0x12` `SYSCALL` | (`+0x14` = 돌아올 칸) | 커널 서비스 `0x43` | `+0x3 MOV EAX,0x43` | [정적] |
| `nt!NtContinue` | `+0x3` `JMP` | (꼬리) | `nt!NtContinueEx` | 늘. 앞 `+0x0 MOVZX EDX,DL` | [정적] |
| `nt!NtContinueEx` | `+0x81` | `+0x86` | `nt!KiContinueEx(CONTEXT, 둘째 인자, 예외 프레임, 트랩 프레임)` | 늘 | [정적] |
| `nt!KiContinueEx` | `+0xc9` | `+0xce` | `nt!KeTestAlertThread(1 = UserMode)` | 유저 길 + TestAlert 참 | [정적] |
| `nt!KiContinueEx` | `+0xeb` | `+0xf0` | `nt!KiContinuePreviousModeUser(CONTEXT, 예외, 트랩, 둘째 인자)` | `PreviousMode ≠ 0` | [정적] |
| `nt!KiContinueEx` | `+0x1e0` | `+0x1e5` | `nt!KeContextToKframes` | `PreviousMode == 0` (커널 길) | [정적] |
| `nt!KiContinueEx` | `+0x1ec` | `+0x1f1` | `nt!KeTestAlertThread(0)` | 커널 길 + TestAlert | [정적] |
| `nt!KiContinueEx` | `+0x114` | `+0x119` | `nt!KiDeliverApc(1, 0, 트랩)` | 인자 구조체 bit1 + TestAlert + 유저 APC 대기 (BOOLEAN 길은 못 탐) | [정적] |
| `nt!KiContinueEx` | `+0xa3` | `+0xa8` | `nt!ExRaiseDatatypeMisalignment` | CONTEXT 가 16바이트 정렬 아님 | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0xcf`·`+0x106`·`+0x186` | `+0xd4`·`+0x10b`·`+0x18b` | `nt!RtlpValidateContextFlags2` | `ContextFlags` 검사 (셋) | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x133` | `+0x138` | `nt!RtlGetExtendedContextLength3` | 복사 길이 | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x15b` | `+0x160` | 스택에 큰 자리 잡기 (같은 주소 이름 2개, 후보: `__chkstk`, `_alloca_probe` — `.asm` 표시) | 늘 | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x1a9` | `+0x1ae` | `nt!RtlInitializeExtendedContext2` | 복사본 틀 | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x1c9` | `+0x1ce` | **`nt!RtlpReadExtendedContext`** | 유저 CONTEXT → 커널 복사본 | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x24a` | `+0x24f` | `nt!RtlGuardIsValidStackPointer(Rsp, TEB)` | `KPROCESS.CheckStackExtents` + CONTROL | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x27a` | `+0x27f` | `nt!PsWow64GetProcessMachine` | 늘 (WoW64·Pico 아니면 `+0x293` `SegCs = 0x33`) | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x399` | `+0x39e` | `nt!KiVerifyContextIpForUserCet` | 스레드 `CetUserShadowStack` + 프로세스 `UserCetSetContextIpValidation` | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x3d5` | `+0x3da` | `nt!KiLogUserCetSetContextIpValidationFailure` | 위 검사 거절 + 감사 모드 | [정적] |
| `nt!KiContinuePreviousModeUser` | **`+0x422`** | **`+0x427`** | **`nt!KeContextToKframes(트랩, 예외, 복사본, 플래그, 1)`** | 검사 다 통과 | [정적] |
| `nt!KeContextToKframes` | `+0x78` | `+0x7d` | `nt!KxContextToKframes` | (run16) | [정적] |
| `nt!NtContinueEx` | `+0x15b` | `+0x160` | `nt!KiInitiateUserApc` | 출구 고리 `+0x148` 유저 APC 대기 비트 | [정적] |
| `nt!NtContinueEx` | `+0x16c`·`+0x186`·`+0x19c`·`+0x1b8`·`+0x1d4` | `+0x171`·`+0x18b`·`+0x1a1`·`+0x1bd`·`+0x1d9` | `KiSynchronizeUserIsolationDomainEntry`·`KiUpdateStibpPairing`·`KiRestoreSetContextState`·`KiCopyCounters`·`KiRestoreDebugRegisterState` | 출구 마무리 (조건부 섞임) | [정적] |
| `nt!NtContinueEx` | `+0x40c` `JMP` | (꼬리) | `nt!KiKernelExit` | `KiKvaShadow` 켜짐 | [정적] |
| `nt!NtContinueEx` | `+0x426` `IRETQ` | — | 유저로 | `KiKvaShadow` 꺼짐 | [정적] |
| `ntdll!RtlUserThreadStart` | `+0x27` | **`+0x2c`** | CFG 간접: 칸 `Kernel32ThreadInitThunkFunction(0, 시작 주소, 파라미터)` | 칸 ≠ 0 (그리고 `RtlLeaveCriticalSection` 아님) | [정적] + 01_sprint Worker 종료 콜스택 `RtlUserThreadStart+0x2c` [관찰] 과 같은 자리 |
| `ntdll!RtlUserThreadStart` | `+0x30` | `+0x35` | `ntdll!RtlLeaveCriticalSection(0, rcx, rdx)` | 칸 값 == 그 함수 주소 (특례) | [정적] |
| `ntdll!RtlUserThreadStart` | `+0x3d` | `+0x42` | CFG 간접: 시작 주소(파라미터) | 칸 == 0 | [정적] |
| `ntdll!RtlUserThreadStart` | `+0x44` | `+0x49` | `ntdll!RtlExitUserThread(결과)` | 칸 == 0 갈래 끝 | [정적] |
| `ntdll!RtlUserThreadStart` | `+0x50` | `+0x55` | `ntdll!NtTerminateProcess(-1, 예외 코드)` | `__except` 착지 `+0x4a` | [정적] |
| `RtlUserThreadStart$filt$0` | `+0x20`·`+0x3a`·`+0x4c` | `+0x25`·`+0x3f`·`+0x51` | `RtlDecodePointer` → CFG 간접(`RtlpUnhandledExceptionFilter` 값) 또는 `RtlUnhandledExceptionFilter2` | 예외 필터 | [정적] |

### 정적 — 둘째 인자의 두 얼굴 (`KiContinueEx +0x71 CMP RDI,0xff` / `+0x78 JA`)

- `0xff` 이하 → **BOOLEAN `TestAlert`**. 이번 길 (`nt!NtContinue` 가 `DL` 만 남겨 0/1 로 넘김).
- `0xff` 보다 큼 → **`_KCONTINUE_ARGUMENT` 유저 포인터** (0x18바이트, 8바이트 정렬): `+0x0` `ContinueType` (4 미만), `+0x4` 플래그 (bit0 TestAlert, bit1 "APC 먼저" `[추측: 이름]`, bit2 이상 거절), `+0x8`·`+0x10` 예약 (0 이어야 함). 이번 길엔 해당 없음.

### 정적 — 유저 길 검사 순서 (`KiContinueEx` → `KiContinuePreviousModeUser`)

| 순서 | 무엇 | 자리 | 실패하면 |
|---|---|---|---|
| ① | IRQL 을 APC_LEVEL 로 (낮을 때만) | `KiContinueEx +0x2a~+0x40`, 되돌림 `+0x180~+0x19d` | — |
| ② | CONTEXT 16바이트 정렬 | `KiContinueEx +0x93`/`+0xa9` | 정렬 예외 → ④ |
| ③ | TestAlert 참이면 CONTEXT 첫·끝 바이트 읽고 다시 쓰기 (쓰기 가능 검사) | `+0x82~+0xc1` | 접근 예외 → ④ |
| ④ | `KiContinueEx` 전체 `__try` `+0x6c~+0x177` | `.pdata` | 착지 `+0x177`: 예외 코드를 반환값으로 (착지점 안 호출 없음) |
| ⑤ | `KiContinuePreviousModeUser` `__try` `+0x58~+0xb9`: 유저 CONTEXT 의 `ContextFlags` 읽기 + 인자 구조체 모양 | `.pdata` | 착지 `+0xb9` → `+0x2be` (예외 코드), 모양 틀림 `0xc000000d` |
| ⑥ | `ContextFlags` 검사 셋 | `+0xcf`·`+0x106`·`+0x186` | 음수면 반환. 단 `0xc000000d` 이거나 아키텍처 비트가 없으면 **거절 대신 깎기** (`+0x459 AND EDI,0xf810001f` / `+0x45f BTS EDI,0x14`) |
| ⑦ | **커널로 복사** (길이 → 스택 자리 → `memset` → 틀 → `RtlpReadExtendedContext`) | `+0x133`~`+0x1ce` | 음수 반환, 접근 예외는 ④ |
| ⑧ | `SegCs = 0x33` 고정 (WoW64·Pico 아니면) | `+0x27a`, `+0x293` | — |
| ⑨ | `Rsp` 검사 (`CheckStackExtents` 켜졌고 CONTROL 이 있으면) | `+0x212~+0x251` | `0xc000000d` |
| ⑩ | **`Rip` 검사 (CET 일 때만)** | `+0x367~+0x3da` | `0xc000060a` (감사 모드면 로그만 남기고 통과) |
| ⑪ | 확장 기능 막기 마스크 | `+0x403~+0x44e` | `0xc000000d` |
| ⑫ | **`KeContextToKframes`** | `+0x422` (ret `+0x427`) | — |

- 32비트(WoW64)·Pico 갈래는 64비트 test.exe 가 안 탈 것 `[추측: test.exe 는 64비트]`.

### 정적 — 트랩 프레임에 옮기는 칸 (`KxContextToKframes`, run16 표 + 거르는 줄)

| CONTEXT 칸 | 트랩 프레임 칸 | 옮기는 조건 | 이번 길의 값 (run16 `[정적]`) |
|---|---|---|---|
| `Rip` `+0xf8` | `+0x168` | `(플래그 & 0x100001) == 0x100001` (`+0x21`) | `ntdll!RtlUserThreadStart` |
| `Rsp` `+0x98` | `+0x180` | 같음 (`+0x35`) | 맡긴 문맥의 유저 스택 |
| `Rcx` `+0x80` | `+0x38` | `(플래그 & 0x100002) == 0x100002` (`+0xc9`/`+0xce`) | exe 진입점 (`TransferAddress`) |
| `Rdx` `+0x88` | `+0x40` | 같음 | PEB |

- 맡긴 문맥의 `ContextFlags` = `PspInitializeThunkContext +0x8d ADD R12D,0x10001b` 값으로 보인다 (CONTROL + INTEGER + 둘 더). 그러면 넷 다 옮겨진다 `[추측: RtlInitializeExtendedContext2 가 값을 그대로 쓴다고 가정]`. 실제 값은 동적 (Q29).
- 덤: 유저 모드면 `SegSs = 0x2b`, `SegCs` 는 `0x33`/`0x23`, `CS = 0x33` 이면 `Rip` 를 48비트 부호 확장 (`KxContextToKframes +0x8c~+0xc0`). 목적은 `[추측]` (비정규 주소로 `IRETQ` 가 죽지 않게).

### 정적 — CET 맞춰 보기

- 넣는 쪽 (run16): `PspInitializeThunkContext +0x295` `KePopulateContinuationContext(원래 Rip)` — 스레드 `+0x74` bit20 이면 유저 그림자 스택에 원래 `Rip`(= `RtlUserThreadStart`) 한 칸을 올린다.
- 맞춰 보는 쪽: `KiVerifyContextIpForUserCet` 가 그림자 스택 포인터를 읽고 (`+0x13a~+0x145`) **그 칸과 `CONTEXT.Rip` 를 비교**한다 (`+0xfc~+0x101`). 같으면 포인터를 한 칸 올려 성공.
- `Rip == PsNtdllExports+0x10` (= `RtlUserThreadStart`) 이면 `ContinueType` 0 → 1 로 바꿔 그림자 스택 대조 쪽으로 보낸다 (`+0x9d`/`+0xa6`). BOOLEAN 길이 이 특례를 탄다.
- `Rip` 가 `0x10000 ~ 0x7ffffffdffff` 밖이면 바로 거절 (`+0x2e~+0x42`).
- 이 모두가 **스레드 `CetUserShadowStack`(`_KTHREAD +0x74` bit20) + 프로세스 `UserCetSetContextIpValidation`(`_EPROCESS +0x754` bit17)** 둘 다일 때만이다. 아니면 `KiContinuePreviousModeUser +0x2a0` (또는 `+0x378`) 에서 건너뛴다. 이 VM 값은 동적 (Q57).
- 검사가 올린 새 그림자 스택 포인터를 `KiContinuePreviousModeUser` 안에서 다시 쓰는 줄은 못 찾았다. 유저 SSP 가 어떻게 되는지는 `[공백]`.

### 정적 — TestAlert

- 유저 길은 `KiContinueEx +0xc9` (ret `+0xce`) `KeTestAlertThread(1)`. **CONTEXT 를 트랩 프레임에 쓰기 전**이다.
- `KeTestAlertThread`: `Alerted[모드]` 가 켜졌으면 끄고 TRUE. 아니고 유저 모드이며 유저 APC 큐가 비어 있지 않으면 `+0x83 OR [thread+0xc2],2` = `ApcState.UserApcPending` 를 켠다. 반환값은 `KiContinueEx` 가 안 쓴다.
- 실제 배달은 `NtContinueEx` 출구 고리: `+0x148 TEST byte [thread+0xc2],3` → 0 이 아니면 `+0x15b` (ret `+0x160`) `KiInitiateUserApc` 후 처음부터 다시. TestAlert 가 거짓이어도 이미 켜져 있으면 돈다.
- 배달할 APC 가 있으면 트랩 프레임이 CONTEXT 로 덮인 **뒤에** 그 위에 APC 진입을 만든다 `[추측: KiInitiateUserApc 본문 안 읽음]`.
- test.exe 시동 때 배달될 유저 APC 는 `[추측]` 보통 없다 (sast/10 에서 유저 APC 를 넣는 줄을 못 봄). 동적 Q57.

### 정적 — 유저로 돌아가는 법 (`NtContinueEx`)

- **성공하면 반환하지 않는다.** `+0x86 TEST EAX,EAX` / `+0x88 JLE +0x473` — `KiContinueEx` 가 1 을 돌려주면 성공. 그 뒤 순서:
  1. `+0xb7~+0x10b` 예외 프레임에서 비휘발 레지스터 되살림.
  2. `+0x10f CLI`, 커널 그림자 스택 한 칸 건너뛰기 (`+0x112`·`+0x121`, 목적은 `[추측]`), `+0x126` 새 트랩 프레임 `SegCs` 가 유저면 계속.
  3. `+0x133~+0x16a` 유저 APC 고리 (위), `+0x16c`~`+0x1d4` 출구 마무리 호출들, 커널 그림자 스택·MSR·RSB 채우기 사다리 (추측 실행 완화 `[추측]`).
  4. **`+0x3be~+0x3ee` 트랩 프레임에서 `XMM0~5`·`R11`·`R10`·`R9`·`R8`·`RDX`·`RCX`(`+0x3ea`)·`RAX` 되살림.**
  5. `+0x3f2`~`+0x3fc` `RSP` = 트랩 프레임 `Rip` 칸.
  6. `+0x403` `KiKvaShadow` 로 갈림: 켜짐 → **`+0x40c JMP KiKernelExit`**, 꺼짐 → `+0x423 SWAPGS` → **`+0x426 IRETQ`**.
- 시스템 호출의 보통 출구(`SYSRET` 쪽)를 안 탄다. 그래서 휘발성 `Rcx` = 진입점이 산다. **스레드 칸·플래그 같은 표시는 없고, 경로 자체가 보장한다** (`KxContextToKframes` 가 `+0x38` 에 쓰고 → `NtContinueEx +0x3ea` 가 읽음).
- 실패하면 `+0x473 ADD RSP,0x138` / `+0x47a RET`, `EAX` = 상태. 보통 출구로 `ntdll!NtContinue +0x14 RET` → `LdrInitializeThunk +0x18` → `+0x1a` (ret `+0x1f`) `RtlRaiseStatus(상태)` (run16). 보이는 값: `0xc000000d`, `0xc000060a`, 정렬·접근 예외 코드, (BOOLEAN 길은 못 타는) 0.
- 커널 모드로 돌아가는 갈래도 있다 (`+0x12d JZ +0x428`, `+0x471 IRETQ`). 유저 복귀에선 안 탄다.

### 정적 — `ntdll!RtlUserThreadStart`

- 받는 것: `rcx` = 시작 주소 (`+0x4 MOV R9,RCX`), `rdx` = 파라미터 (`+0x1a MOV R8,RDX`). 첫 스레드면 진입점과 PEB (run16 칸 + 위 옮기기).
- `+0x7` 칸 `Kernel32ThreadInitThunkFunction` (rva `0x1d2760`) 을 읽고 `+0xe`/`+0x11 JZ +0x37`:
  - **칸 ≠ 0**: `+0x1d RDX = 시작 주소`, `+0x25 ECX = 0` → **`+0x27` CFG 간접 호출 (ret `+0x2c`)** = `칸(0, 시작 주소, 파라미터)`. run25 `BaseThreadInitThunk` 의 "첫 인자 0" 갈래와 맞는 인자다.
  - 특례: 칸 값 == `RtlLeaveCriticalSection` 주소면 `+0x30` (ret `+0x35`) 직접 호출. 왜 있는지는 `[추측]` (컴파일러가 흔한 대상을 직접 호출로 바꾼 모양).
  - **칸 == 0**: `+0x3d` (ret `+0x42`) 시작 주소를 CFG 간접으로 직접 → `+0x44` (ret `+0x49`) `RtlExitUserThread(결과)`.
- CFG 중개: `CALL 0x180172020` → `_guard_dispatch_icall` → 칸, **전부 `JMP`** 라 콜스택 프레임이 없다 (run28 과 같음). kernel32 `BaseThreadInitThunk +0x12` 의 중개(`0x180087010`)도 `JMP` → `JMP [칸]` 둘뿐이다 (`.asm` 확인).
- 예외: `__try` `+0x7~+0x4a`, 필터 `RtlUserThreadStart$filt$0` (전역 `RtlpUnhandledExceptionFilter` 를 풀어 있으면 그걸, 없으면 `RtlUnhandledExceptionFilter2`), 착지 `+0x4a` → `+0x50` (ret `+0x55`) `NtTerminateProcess(-1, 예외 코드)`. 시작 함수 안 처리 안 된 예외는 여기서 프로세스를 끝낸다.
- 칸 ≠ 0 갈래는 `+0x2c JMP +0x56` → `RET` 이지만, `BaseThreadInitThunk` 가 첫 인자 0 에서 `RtlExitUserThread` 로 끝나 안 돌아온다 (run25) → **돌아오지 않는 설계** `[정적: run25 + 이번]`.

### 정적 — ntdll `NtContinue` 스텁과 서비스 표

- `+0x0 MOV R10,RCX` / **`+0x3 MOV EAX,0x43`** / `+0x8 TEST [0x7ffe0308],1` → `INT 0x2e` 갈래 / **`+0x12 SYSCALL`** / `+0x14 RET`.
- 커널 `KiServiceTable` (489칸) `[0x43]` = rva `0x6adde0` = **`nt!NtContinue`**. 앞뒤 `[0x42] NtDuplicateToken`, `[0x44] NtQueryDefaultUILanguage`, 489칸 모두 `_index.tsv` 함수 시작과 맞음 (결과서).

### 호출 관계

```
[유저 ntdll]  LdrInitializeThunk
 └─ (2) +0x18  NtContinue(맡긴 CONTEXT, TRUE)              (run16)
      │  +0x3 EAX = 0x43
      ══▶ +0x12 SYSCALL  (돌아올 칸 +0x14, 성공하면 안 돌아옴)
[커널 nt]  (시스템 호출 처리기 — 본문 안 읽음)
      └─ nt!NtContinue          +0x0 MOVZX EDX,DL
          ══▶ +0x3 JMP  NtContinueEx
               ├─ (1) +0x86  KiContinueEx(ctx, 1, 예외 프레임, 트랩 프레임)
               │      │  +0x5e PreviousMode ≠ 0 → 유저 길
               │      ├─ (1) +0xce  KeTestAlertThread(1)        UserApcPending 켤 수 있음
               │      └─ (2) +0xf0  KiContinuePreviousModeUser
               │              ├─ (1) +0x1ce  RtlpReadExtendedContext     유저 CONTEXT → 커널 복사본
               │              │      (+0x293 SegCs = 0x33)
               │              ├─ (2) +0x39e  KiVerifyContextIpForUserCet  (CET 둘 다 켜졌을 때만)
               │              └─ (3) +0x427  KeContextToKframes
               │                      └─ +0x7d  KxContextToKframes
               │                           트랩.Rip ← RtlUserThreadStart, 트랩.Rcx ← 진입점,
               │                           트랩.Rdx ← PEB, 트랩.Rsp ← 맡긴 Rsp
               ├─ (2) +0x160  KiInitiateUserApc      (유저 APC 대기 비트가 있을 때만)
               │  +0x3ea RCX ← 트랩.Rcx … 휘발 레지스터 되살림
               ├─ (3) ══▶ +0x426 IRETQ                (KiKvaShadow 꺼짐)
               └─ (3) ══▶ +0x40c JMP KiKernelExit     (KiKvaShadow 켜짐) ══▶ +0xa5 IRETQ (run15)
[유저 ntdll]  RtlUserThreadStart(rcx = 진입점, rdx = PEB)
 └─ +0x2c  (CFG 중개 JMP, 프레임 없음) 칸 Kernel32ThreadInitThunkFunction(0, 진입점, PEB)
      = KERNEL32!BaseThreadInitThunk 추정 (칸 값은 Q48)
        └─ +0x17  (CFG 중개 JMP) 진입점(PEB) = test!mainCRTStartup 추정 (Q18·Q30)
```

- `main` 에서 `k` 예상 (아래부터): `ntdll!RtlUserThreadStart+0x2c` → `KERNEL32!BaseThreadInitThunk+0x17` → `test!mainCRTStartup+?` → … → `test!main`. 앞 둘은 `[정적]`, 사이 CRT 함수는 이번 범위 밖. CFG 중개 프레임은 안 보일 것.
- `RtlUserThreadStart` 입구에서 `k`: 돌아올 칸을 아무도 안 썼으니 혼자 보이거나 아래가 쓰레기일 수 있다 `[추측]`.

## 확인된 것

- (`[정적]`) ntdll `NtContinue` 의 시스템 호출 번호는 **`0x43`** 이고, 커널 서비스 표 `[0x43]` 은 **`nt!NtContinue`** 다.
- (`[정적]`) `nt!NtContinue` (5바이트) 는 `DL` 만 남기고 **`NtContinueEx` 로 꼬리 점프**한다. `NtContinueEx` 는 늘 `KiContinueEx` (ret `+0x86`) 를 부르고, 유저/커널 갈림은 `KiContinueEx +0x5e` (`_KTHREAD.PreviousMode`) 에 있다.
- (`[정적]`) **유저 길에서 CONTEXT 를 트랩 프레임에 쓰는 곳은 `KiContinuePreviousModeUser +0x422` (ret `+0x427`) `KeContextToKframes`** 다. run16 이 이름만 본 두 후보 중 `KiContinueEx +0x1e0` (ret `+0x1e5`) 은 **커널 모드 길**이다.
- (`[정적]`) 유저 길은 CONTEXT 를 **커널로 복사**(`RtlpReadExtendedContext`, ret `+0x1ce`)해서 쓰고, 그 전에 정렬·쓰기 가능·`ContextFlags`·`Rsp`·(CET 일 때) `Rip` 를 검사한다. `ContextFlags` 는 거절 대신 **깎일** 수 있다.
- (`[정적]`) TestAlert(`TRUE`) 는 `KeTestAlertThread(1)` (ret `+0xce`) 로 큐에 든 유저 APC 를 "배달 대상" 으로 올릴 뿐이고, 배달은 `NtContinueEx +0x15b` (ret `+0x160`) `KiInitiateUserApc` 가 한다.
- (`[정적]`) **`NtContinueEx` 는 성공하면 반환하지 않고 자기 몸에서 `IRETQ` (`+0x426`) 또는 `KiKernelExit` 꼬리 점프 (`+0x40c`) 로 유저에 간다.** 휘발성 레지스터를 트랩 프레임에서 되살리므로 `Rcx` = 진입점, `Rdx` = PEB 가 그대로 `RtlUserThreadStart` 에 간다. 표시 칸은 없고 경로가 보장한다.
- (`[정적]`) run16 의 `[추측]` "`KePopulateContinuationContext` 는 `NtContinue` 로 돌아갈 때 검사 맞추기" 는 **코드로 뒷받침된다**: `KiVerifyContextIpForUserCet` 가 그림자 스택 칸과 `CONTEXT.Rip` 를 비교하고, `Rip == RtlUserThreadStart` 면 특례(`ContinueType` 0 → 1)로 그 대조를 탄다. 단 CET 비트 둘 다 켜졌을 때만.
- (`[정적]`) `RtlUserThreadStart` 는 칸 `Kernel32ThreadInitThunkFunction` 이 0 이 아니면 **`(0, rcx, rdx)` 로 CFG 간접 호출 (call `+0x27`, ret `+0x2c`)** 한다. 01_sprint Worker 종료 콜스택 `RtlUserThreadStart+0x2c` `[관찰]` 과 같은 자리다.
- (`[정적]`) 같은 칸을 `LdrpInitializeProcess +0x1b5e` 는 `(1,0,0)`, `RtlUserThreadStart +0x2c` 는 `(0, 진입점, 파라미터)` 로 부른다. run25 `BaseThreadInitThunk` 의 두 갈래(첫 인자 0 / 0 아님)와 맞는다.
- (`[정적]`) `RtlUserThreadStart` 는 전체가 `__try` (`+0x7~+0x4a`) 이고, 처리 안 된 예외는 착지 `+0x4a` → `NtTerminateProcess` (ret `+0x55`) 로 프로세스를 끝낸다. 칸이 0 이면 시작 주소를 직접 부르고 `RtlExitUserThread` (ret `+0x49`).

### flow.md 2.10 `[추측]` 두 줄 판정

| 예전 `[추측]` | 판정 | 근거 |
|---|---|---|
| "로더 일이 끝나면 `ntdll!NtContinue` 로 실행이 `ntdll!RtlUserThreadStart` 로 넘어간다" | **코드로는 맞음 `[정적]`.** 실행은 여전히 동적 (Q29). 갈림 둘: CET 검사 (Q57), `KiKvaShadow` (`IRETQ` 직접 / `KiKernelExit`, Q42) | 위 사슬 |
| "`RtlUserThreadStart` → `KERNEL32!BaseThreadInitThunk` → `test!mainCRTStartup` 순서로 불린다" | 앞 화살표: **호출 자리는 `[정적]`** (`+0x27`, ret `+0x2c`, CFG 간접, 인자 `(0, 진입점, PEB)`). 대상이 `BaseThreadInitThunk` 인지는 칸 값이라 동적 (Q48). 뒤 화살표는 Q18 (정적) · Q30 (동적) | 위 `RtlUserThreadStart` 절 |

## 아직 확인 필요

동적 확인거리 D1~D8 은 오케스트레이터가 대기열에 넣었다. 오프셋은 `nt + rva` / `ntdll + rva`, 이번 부팅 값 아님. 모든 프로세스가 지나는 함수라 test.exe 조건이 필요하다.

- **Q29** (D1~D4):
  - (D1) `nt!NtContinue` (`nt+0x6adde0`) 입구: `rcx` = 맡긴 CONTEXT, `dl` = 1, `dt nt!_CONTEXT @rcx ContextFlags Rip Rcx Rdx Rsp SegCs` (기대 `Rip = ntdll!RtlUserThreadStart`, `Rcx` = 진입점, `Rdx` = PEB). 트랩 프레임 `Rip` = `ntdll!NtContinue+0x14`.
  - (D2) `nt+0x2bcb72` (`KiContinuePreviousModeUser +0x422`, call 직전) 과 `nt+0x2bcb77` (ret): `r8` 복사본 `Rip`, `r9d` 플래그 (기대 `0x10001b`?), 뒤에서 `dt nt!_KTRAP_FRAME @<rcx> Rip Rcx Rdx Rsp SegCs`.
  - (D3) `nt+0x6ae1f3` (`NtContinueEx +0x403`), `nt+0x6ae216` (`+0x426 IRETQ`) / `nt+0x6ae1fc` (`+0x40c`): 어느 쪽을 타나, `dq @rsp L5`, `rcx` = 진입점.
  - (D4) `ntdll+0xacac0` (`RtlUserThreadStart`) 입구: `r rcx rdx rsp`, `k`, `ln @rcx` (= `test!mainCRTStartup`).
- **Q30** (D4·D7·D8): `ntdll+0xacae7` (`RtlUserThreadStart +0x27`) 에서 `ln @rax`·`rcx = 0`·`rdx` = 진입점·`r8` = PEB, `test!main` 에서 `k` (`… → BaseThreadInitThunk+0x17 → RtlUserThreadStart+0x2c`, CFG 중개 프레임 없음).
- **Q42** (D3): `KiKvaShadow` 값 — 2.3 의 `KiStartUserThread` 출구와 같은 변수다.
- **Q48** (D7): 칸 `Kernel32ThreadInitThunkFunction` 값 `ln` = `KERNEL32!BaseThreadInitThunk` (rva `0x2cd70`) 인가.
- **Q57** (D5·D6): `KiContinuePreviousModeUser +0x2a0` (`nt+0x2bc9f0`) 에서 스레드 `+0x74` bit20, 프로세스 `+0x754` bit17·18, `+0x88` bit5. `+0x399` (`nt+0x2bcae9`) / ret `nt+0x2bcaee` 의 `eax` (0 / `c000060a`), `rdmsr 6a7` 칸이 `RtlUserThreadStart` 인가. `NtContinueEx +0x148` (`nt+0x6adf38`) `db @rcx+0xc2 L1`, `KiInitiateUserApc` 가 이때 걸리나.
- 정적으로 남은 것 → **Q58** (정적, 아래 넷을 한 항목으로):
  - `KiVerifyContextIpForUserCet` 가 올린 새 그림자 스택 포인터를 누가 유저 SSP 로 되돌려 쓰나 (`KiContinuePreviousModeUser` 안엔 없음) `[공백]`.
  - `KiInitiateUserApc` 본문 (배달할 APC 가 있을 때 원래 문맥을 어디 보관하나) `[추측]`.
  - 맡긴 문맥 `ContextFlags` 가 정말 `0x10001b` 인가 — `RtlInitializeExtendedContext2` 가 받은 값을 그대로 쓰나 (정적으로도 답할 수 있음).
  - `RtlUserThreadStart` 의 `RtlLeaveCriticalSection` 특례 갈래(`+0x30`, ret `+0x35`)가 있는 이유 `[추측]`.
  - (Q58 목록 밖, `[추측]` 만) 출구 `KiKernelExit` 를 `NtContinueEx` 가 부를 때도 run15 와 같은 일(`+0xa5 IRETQ`)을 하나 — 같은 함수 입구로 꼬리 점프하니 같을 것 `[추측]`.

## 디버거 메모 / 도구 메모

- **WinDbg 오프셋 표시 주의 (`[추측]`)**: `NtContinueEx` 뒤쪽(RSB 채우기 사다리 뒤 `+0x38a ~`) 은 `.asm` 에서 "조각 `FUN_1406ae17a`" 로 보이지만 `.pdata` 상 같은 함수 범위(`0x6addf0~0x6ae26b`)다. WinDbg 는 `nt!NtContinueEx+0x426` 로 보일 가능성이 높다. 안 맞으면 `bp nt+0x6ae216` 처럼 rva 로 건다. → windbg-kd-runbook pitfalls 에 추가 제안.
- `_calls.tsv` 의 조각 안 호출은 조각 기준(`frag:FUN_1406ae17a+0x82 tail KiKernelExit`)으로만 찍힌다. 본래 함수 기준(`NtContinueEx +0x40c`) 은 `.asm` 줄 끝 주석에만 있다 (결과서 스크립트 문제 1·2, 급하지 않음).
- Ghidra 가 `KePopulateContinuationContext +0x30` 을 4바이트 `WRUSSD` 로 읽었지만 원본 바이트는 `REX.W` 가 붙은 8바이트 `WRUSSQ` 다 (결과서). 답에는 영향 없음.
- `RtlUserThreadStart.c` 는 `NtTerminateProcess` 를 정상 흐름처럼 그렸고 실제론 `__except` 착지점 `+0x4a` 다. `NtContinueEx.c` 는 많이 틀어졌다. 둘 다 `.asm` 만 믿을 것.
- **디버거가 붙어서 달라질 수 있는 점** `[추측]`: 이 길의 `KeTestAlertThread`·`KiInitiateUserApc` 에 디버거가 거는 유저 APC 는 보이지 않을 것. 소프트웨어 중단점은 명령을 `0xcc` 로 바꿀 뿐이라 `IRETQ` 직전 상태를 바꾸진 않는다.

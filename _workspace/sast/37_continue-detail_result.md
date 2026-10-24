# 37 continue-detail — 정적 분석 결과

- 요청서: `_workspace/sast/37_continue-detail.md` (Q58, 02_sprint)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid+age = `C29EBFB06B78B3C020DCA66D99713F9E1` → 기준(26100.9457)과 **같음**. 다시 analyze 안 함 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음).
- 요청서 대상 6개는 **모두 이미 `ms/decomp`·`ms/disassemble/ntoskrnl` 에 있었음** → 새로 안 뽑고 그대로 읽음. 단 `RtlInitializeExtendedContext2`·`KiInitiateUserApc` 의 `.asm` 도 이미 있었음.
- 질문에 답하려고 **depth 0 으로 몇 개만 더 뽑음** (이유는 맨 아래 "추가로 뽑은 것"): `RtlCopyContext`, `RtlpCopyLegacyContext`, `RtlpGetContextFlagsLocation`, `KiInitializeUserApc`, `KeVerifyContextIpForUserCet`, `KeVerifyContextRecord` (+ 이름이 비슷해 정규식에 같이 걸린 `RtlpCopyContext`, 결과서엔 안 씀).
- 버전 확인: `NtCreateUserProcess +0xc36 → PspAllocateProcess` 일치, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv` 에서 확인, call 은 +0xc31 / +0x802). 이번에 읽은 `.asm` 의 `+0x..` 도 요청서(sast/24)에 적힌 값(`+0x8d`, `+0xf0`, `+0x128`, `+0x399/+0x39e`, `+0x15b/+0x160`)과 줄 단위로 맞음.
- 스크립트 문제: 없음.

## 한눈에

1. `ContextFlags` 는 **정말 `0x10001b`** (CET 아닌 스레드). CET 스레드는 **`0x10005b`**. `RtlInitializeExtendedContext2` 는 받은 값을 **그대로** 쓴다 (더하지도 깎지도 않음). `PspInitializeThunkContext` 안에서 다시 바꾸는 줄도 없다.
2. 한 칸 올린 새 SSP 를 **쓰는 곳은 없다.** 부른 쪽(`KiContinuePreviousModeUser`)이 `+0x39e` 뒤에 그 칸을 안 읽고, `WRMSR 0x6a7` 을 하는 함수를 커널 전체에서 찾아봐도 `NtContinue` 길 안엔 하나도 없다. 대신 이 길에서 유저 SSP 를 기계에 쓸 수 있는 건 문맥 속 CET_U 칸을 `XRSTORS` 로 되돌리는 곳 하나 (`KxContextToKframes +0x211`).
3. `KiInitiateUserApc` 는 **자기가 문맥을 보관하지 않는다.** 틀만 짜서 `KiDeliverApc` 하나를 부른다. 원래 문맥을 유저 스택에 복사하는 건 `KiDeliverApc +0x5ee` 가 부르는 `KiInitializeUserApc` (`+0x1fb KeContextFromKframes`).

## 질문별 답 [정적]

### 질문 1. 맡긴 문맥의 `ContextFlags` 는 정말 `0x10001b` 인가

- **답: 예. CET 아님 `0x10001b` / CET `0x10005b`.**

| 단계 | 줄 | 하는 일 |
|---|---|---|
| 값 정하기 | `PspInitializeThunkContext +0x69~+0x94` | `[thread+0x74] & 0x100000` (CET 칸) 이 켜져 있으면 `0x40` 을 만들고, `ADD R12D,0x10001b` (`+0x8d`) 해서 `[RBP+0x8]` 와 `R12D` 에 둠. 같은 곳에서 XState 를 쓸 칸 `R14` = CET 면 `0x800`, 아니면 0 (`+0x72~+0x79`). |
| 임시 문맥 | `+0xea~+0xf0` | `RtlInitializeExtendedContext2(R13, R12D, ..., R14)` — 커널 스택의 **임시** 문맥. 여기에 `PspGetContextThreadInternal` (`+0x168`) 로 지금 스레드 상태를 받음. |
| **맡길 문맥** | `+0x263~+0x269` | `RtlInitializeExtendedContext2(R15, R12D, &[RBP+0x20], [RBP+0x18])` — **유저 스택(R15) 위** 문맥. `R12D` 는 같은 값 (스레드 만들기 알림 루프가 `R12` 를 카운터로 써서, 그 루프를 탔으면 `+0x153` 에서 `[RBP+0x8]` 로 다시 읽음). |
| 복사 | `+0x27a~+0x280` | `RtlCopyContext(R15, R12D, R13)` — 임시 문맥 → 유저 스택 문맥. |
| 한 칸 올리기 | `+0x28e~+0x295` | `KePopulateContinuationContext([R13+0xf8])` — CET 스레드면 `Rip` 를 유저 그림자 스택에 올림. |

- 첫째, `RtlInitializeExtendedContext2` 가 값을 그대로 쓰나: **예, 더하거나 깎지 않는다.**
  - 쓰는 줄: AMD64 갈래 `+0xf5 MOV [RCX+0x30],EDI` (`RCX` = 문맥 시작을 16바이트에 맞춘 주소, `EDI` = 받은 `ContextFlags`). 같은 함수에서 이 칸을 다시 쓰는 줄은 없음.
  - `EDI` 에 `OR`/`AND` 하는 줄이 없다. 함수가 값에 하는 일은 **검사**뿐: `+0x3c RtlpValidateContextFlags2` 를 부르고 음수면 그대로 돌려줌 (`+0x43 JS`).
  - 그 검사 함수는 값을 받기만 한다 (`ECX` 로 받아 읽기만, `RtlpValidateContextFlags2.asm`). `0x10001b`·`0x10005b` 둘 다 통과하는 모양 (`& 0x7ffff20 == 0x100000`, `+0x1e~+0x23`).
  - 비트 `0x40` (= `0x100040` 이 켜지면) 은 "XState 영역 요청" 으로 읽힌다: 검사 함수가 이때 `KUSER_SHARED_DATA` 쪽 `EnabledFeatures` (`0xfffff780000003d8`) 가 0 이면 `0xc00000bb` 로 거절 (`+0x90~+0xb2`), 아니면 결과 비트 `2` 를 켠다. 이 결과 비트로 `RtlInitializeExtendedContext2` 가 `CONTEXT_EX` 뒤에 XState 칸을 만든다 (`+0x9a~+0xa7`, `+0x104~+0x1a8`). **만드는 건 칸이지 `ContextFlags` 가 아니다.**
  - XState 에 넣을 기능 마스크(4번째 인자, 여기선 `0x800`)는 `KUSER` 의 켜진 기능으로 깎인다 (`+0x12d~+0x14f`, `RtlpRemoveArchDisallowedXStateFeatures`). 이건 `ContextFlags` 가 아니라 마스크 얘기.
- 둘째, 그 뒤 `PspInitializeThunkContext` 가 `ContextFlags` 를 다시 바꾸나: **아니오.**
  - 유저 스택 문맥(`R15`)의 `+0x30` 에 쓰는 줄이 이 함수에 없다. `+0x30` 으로 쓰는 줄은 `+0x38a MOV [RBP+0x30],EBX` 하나인데 이건 예외 레코드 (`KiDispatchException` 에 넘길 `RBP+0x30` 칸, 오류 갈래).
  - 부르는 함수 중 `RtlCopyContext` 만 문맥 `ContextFlags` 를 건드릴 수 있는데, **결과는 같다**: `RtlCopyContext +0xbd` 가 부르는 `RtlpCopyLegacyContext` 가 AMD64 갈래에서 `+0x22 [RDX+0x30] = (원본 플래그 & 요청 플래그) & 0x67ffffff` 로 쓰고, 돌아와서 `RtlCopyContext +0xc7 OR [대상 플래그], R12D(대상이 원래 갖던 플래그)` 로 원래 값을 합쳐 넣는다. 대상이 원래 `0x10001b`/`0x10005b` 이고 거기에 들어갈 값은 그 부분집합이라 **그대로 남는다.**
- 최종 값 표:

| 스레드 | `ContextFlags` | 풀이 | XState 마스크 (`R14`) |
|---|---|---|---|
| CET 아님 | **`0x10001b`** | `0x100000`(AMD64) + `0x1`(제어) + `0x2`(정수) + `0x8`(부동소수) + `0x10`(디버그 레지스터). 세그먼트(`0x4`)·XState(`0x40`)는 **없음** | 0 |
| CET (`[thread+0x74]` 비트 20) | **`0x10005b`** | 위 + `0x40` (XState). 비트 이름은 공개 헤더 값이고, `0x40`=XState 는 `RtlpValidateContextFlags2` 가 `0x100040` 을 XState 로 다루는 것으로 이 바이너리에서도 맞음 | `0x800` (CET_U 칸 하나) |

- **헷갈리기 쉬운 것 (run16 에서 이미 정리됨, 다시 한 번 구분):** `PspInitializeThunkContext` 에는 문맥이 **둘**이다.
  - (가) 유저 스택의 문맥(`R15`): 위 표의 값. `LdrInitializeThunk` 가 `Rcx` 로 받아 나중에 `NtContinue` 에 넘기는 것. `[RBP+0x150]`(= 새 스레드 문맥의 `Rcx`) 에 `R15` 를 넣음 (`+0x1b9`).
  - (나) 스레드를 `LdrInitializeThunk` 로 보내는 **작은 문맥** `[RBP+0xd0]`: `+0x1a8 MOV [RBP+0x100],0x10000b` → 플래그 `0x10000b` (AMD64 + 제어 + 정수 + 부동소수, 디버그 레지스터·XState 없음). `PspSetContextThreadInternal` (`+0x2d6`) 에 넘겨 스레드의 진짜 시작 상태(`Rip`=`LdrInitializeThunk`, `Rsp`=`R15−0x28`, `Rcx`=`R15`, `Rdx`=`PspSystemDlls[0]` 칸 `+0x20` 값. `Rip` 는 보통 `PsNtdllExports+0x8`, 프로세스 `+0x168` 칸이 0 이 아니면 그 값으로 바뀌는 갈래가 `+0x215~+0x228` 에 있음)로 쓴다. 이건 run16 표와 같다.
  - 질문의 "맡긴 문맥" 은 (가). (나) 의 `0x10000b` 와 헷갈리지 않게 문서에 구분해 적을 것.
- 겹치는 증거: `KiInitializeUserApc` (질문 3, 유저 APC 가 문맥을 유저 스택에 맡길 때) 도 **같은 값**을 쓴다: `+0x81 MOV R15D,0x10001b`, CET 스레드면 `+0x32c OR R15D,0x100040` 으로 `0x10005b` (또는 `+0x374 MOV R15D,0x10005b` 갈래). 그리고 `+0x1ec` 에서 `[RDI+0x30] = R15D`. 이 커널은 "유저 스택에 맡기는 문맥" 에 `0x10001b`(+CET면 `0x40`) 를 쓰는 관례로 보인다 `[추측]`.
- 덧붙임 (`NtContinue` 가 받을 때): `KiContinuePreviousModeUser +0x58` 이 **유저 메모리에서** `[문맥+0x30]` 을 읽는다 (`EDI`). 값이 틀리면 `+0x459 AND EDI,0xf810001f; BTS EDI,0x14` 로 고쳐 쓰는 갈래가 있음 (`+0xe2~+0xeb`, `+0x453`). 이 길은 시동 때 문맥이 멀쩡하면 안 탐 `[추측]`.

### 질문 2. 한 칸 올린 새 그림자 스택 포인터(SSP)를 누가 유저 SSP(MSR `0x6a7`)로 쓰나

- **답: 아무도 안 쓴다.** (이 길에서 값 `[R15]` 는 부른 쪽 지역 칸에서 죽는다.)
- 그 칸이 뭔가:
  - `KiVerifyContextIpForUserCet` 의 `R15` 는 **5번째 인자** = `KiContinuePreviousModeUser` 가 넘긴 포인터 (`+0x69 MOV R15,[RSP+0x90]`). 처음 값을 `[R15]` 에서 읽고 (`+0x71`), 일치하면 `+0x128 ADD RAX,8`, `+0x12c MOV [R15],RAX` 로 돌려줌. 이 함수에서 `[R15]` 에 쓰는 줄은 이 하나뿐 (`+0x12c`). 트랩 프레임·스레드 칸·MSR 에 쓰는 줄은 없음 (`0x6a7` 은 `+0x13a` 의 **RDMSR** 만).
  - 부르는 쪽: `KiContinuePreviousModeUser +0x382 LEA RAX,[RBP+0x8]` → `[RSP+0x20]` → `+0x399 CALL`. 그 칸 `[RBP+0x8]` 의 쓰임을 이 함수 `.asm` 전체에서 찾으면:
    - `+0x47` 0으로 시작, `+0x1a0` 문맥 만들기 출력 칸으로 `LEA`, `+0x1c3` 그 값을 읽음, `+0x1d6` 0으로 되돌림,
    - `+0x323` **`RDMSR 0x6a7` 한 현재 SSP 를 씀** (XState 에 CET 칸이 있을 때만, `+0x315~+0x323`), `+0x382` 위 `LEA`.
    - **`+0x39e` 뒤에 읽는 줄 없음** (`+0x399` 뒤 코드는 `+0x39e` 결과 비교 → `+0x2b0` 성공이면 `+0x403 KeContextToKframes`, 실패 `0xc000060a` 면 `+0x3ad` 로그). → 죽은 값.
  - `KeContextToKframes` (`+0x422` 호출) 의 인자는 `(트랩 프레임, 예외 프레임, 커널 문맥 복사본, 플래그, 이전 모드=1)` 이라 SSP 를 안 받음.
- **`WRMSR 0x6a7` 을 하는 곳을 ntoskrnl 코드 영역 전체에서 바이트로 찾음** (`MOV ECX,0x6a7` = `B9 A7 06 00 00` 뒤 40바이트 안에 `WRMSR`). `NtContinue` 길 함수 (`NtContinueEx`, `KiContinueEx`, `KiContinuePreviousModeUser`, `KiVerifyContextIpForUserCet`, `KeContextToKframes`, `KxContextToKframes`, `KiRestoreSetContextState`, `KiKernelExit`) 에는 **WRMSR 이 없다** (RDMSR 은 `KiContinuePreviousModeUser +0x315`, `KiVerifyContextIpForUserCet +0x13a` 뿐). 다른 곳:

| 위치 (함수 +오프셋, nt+rva) | 하는 일 | NtContinue 길? |
|---|---|---|
| `KePopulateContinuationContext +0x17~+0x36` (`nt+0x3ecac7`) | `RDMSR`→−8→`WRUSSD`→`WRMSR` (칸 하나 올림) | 아니오 (시동 때 `PspInitializeThunkContext +0x295`, APC 때 `KiInitializeUserApc +0x21d` 가 부름) |
| `RtlPrepareEnclaveCall +0x60` (`nt+0x46d890`) | `RDMSR`→**+8**→`WRMSR` (칸 하나 내림) | 아니오 (엔클레이브 호출) |
| `KeUserModeCallback +0x37f` (`nt+0x8e7edf`) | 저장해 둔 SSP(`R12`)를 `WRMSR` | 아니오 (유저 콜백에서 돌아올 때) |
| `KiProcessControlProtection` 의 조각 (`nt+0x445be6`, 함수 시작 +0x2b6) | `WRMSR` | 아니오 (프로세스 CET 정책 바꿀 때) |
| 이름 없는 조각 `FUN_1406f9e12 +0xf4` (`nt+0x6f9f06`) | `RDMSR`→−8→`WRUSSQ`→`WRMSR` | 아니오 (`KeUserModeCallback` 쪽 조각 `[추측]`, 이름 안 확인) |
| 이름 없는 작은 함수 (`SepIsAppSiloCapability` 바로 뒤, `nt+0x48f164`) | `WRUSSQ [rcx−8]` → `WRMSR` | 아니오 |
| 트랩 진입 조각 약 40개 (`KiApcInterrupt`, `KiInterruptDispatch`, `KiNmiInterrupt`, `KiMcheckAbort` 등 `FUN_14…` 의 `+0x40/+0x58`, KVA 쪽 `nt+0xbcb122`) | `KiUserCetPl3SspCanonicalizeUpperMask` 로 SSP 윗 32비트가 비정상이면 고쳐 씀 (`KiApcInterrupt` 조각에서 확인) | 아니오 (트랩 **들어올 때**) |

- **대신 이 길에서 유저 SSP 를 기계에 쓸 수 있는 곳 (한 곳):**
  - `KxContextToKframes +0x211 XRSTORS [R8]` (`nt+0x3ea1d1`). 조건: 문맥 플래그에 XState 가 있고(`+0x193~+0x19f`, `0x100040`), 이전 모드가 유저(`+0x1a1 CMP R14B,1`). `R8` = 문맥의 XState 영역 (`+0x1b0~+0x1be`). 마스크는 영역 안 `[R8+0x208]` (압축 마스크) 과 켜진 기능을 AND (`+0x1c0~+0x207`).
  - XState 영역 안에 CET_U 칸이 있다는 증거: `KiContinuePreviousModeUser +0x1e2~+0x206` 이 기능 번호 `0xb`(=11, `0x800`) 칸을 찾고 (`RtlLocateExtendedFeature2`), 그 칸이 `[0]==1`, `[8]`=SSP 모양임 (`KiVerifyContextXStateCetUEnabled +0x6` 이 `[RCX]==1` 이어야 하고, `+0x21` `[RCX+0x8]` 이 8의 배수·현재 SSP 이상이고 그림자 스택 영역 안(`ZwQueryVirtualMemory` 로 확인)이어야 함 → 아니면 `0xc000060a`).
  - 예외 갈래 (이 길 밖일 가능성이 큼 `[추측]`): `+0x1cb JNZ +0x4e2` — 스레드 `+0x198` 칸이 0 이 아니면 `XRSTORS` 대신 `RtlCopyXStateArea` (`+0x55c`) 로 복사하고 `+0x1d5` 로 감. `+0x1fa JZ +0x3df` — `KUSER` 의 `ControlFlags` 비트 1(압축) 이 꺼져 있으면 `RtlXRestore` (`+0x3e5`) 로 되돌림. 어느 쪽인지는 동적.
  - 그 칸에 들어가는 값이 **새 SSP(`+8`) 가 아니다**: `KiContinuePreviousModeUser` 가 칸을 채우는 갈래(`+0x46b BTS R9,0xb`, `+0x478 [R8]=1`, `+0x47f [R8+8]=RAX`)는 `+0x31a` 의 **`RDMSR` 로 읽은 현재 SSP** 를 넣고, 이건 `+0x399` 호출보다 **앞**에 끝난다. `+0x399` 뒤에 `[R8+8]` 을 고치는 줄은 없음. 칸이 이미 있으면(유저/시동 문맥이 가져온 값) 그 값이 쓰이고 `+0x355` 가 검사만 함.
  - `XRSTORS` 가 CET_U 요소로 `IA32_PL3_SSP`(MSR `0x6a7`) 를 되돌린다는 건 **코드에서 안 보이는 CPU 설명서 지식** `[추측]` (Intel SDM: 요소 11 = `IA32_U_CET` + `IA32_PL3_SSP`). 유저로 `IRETQ` 할 때 CPU 가 `IA32_PL3_SSP` 를 SSP 로 올리는 것도 같은 종류의 지식.
- 결론 (한 줄):
  - **`KiVerifyContextIpForUserCet` 가 올린 `+8` 은 `KiContinuePreviousModeUser` 지역 칸에서 사라지고 MSR 로 안 간다.** 유저 SSP 는 (가) XState 가 없는 문맥이면 **MSR 그대로** (= 지금 SSP), (나) XState+CET_U 칸이 있으면 **그 칸의 `[8]` 값** 으로 `XRSTORS` 때 바뀐다.
  - 시동 문맥에 이걸 대 보면 `[추측]`: CET 스레드의 맡긴 문맥은 `0x10005b`(XState 있음) 이고 칸은 `RtlCopyContext`(`+0x280`) 때 임시 문맥에서 복사됨. 이 복사는 `KePopulateContinuationContext`(`+0x295`) 보다 **앞** 이라, 칸의 SSP 가 "한 칸 올리기 전 값" 이면 `NtContinue` 가 끝났을 때 SSP 는 `RtlUserThreadStart` 칸을 **지나 올라가 있게** 된다 (한 칸 되돌림). 칸 안 값은 `PspGetContextThreadInternal` / `RtlpCopyXStateChunk` 가 채우는 거라 이번엔 안 읽음 → "새 정적 공백" 1번, 동적으로도 확인 가능.
  - 사용자가 적은 가정 "SSP 는 `RtlUserThreadStart` 칸을 가리킨 채로 돌아간다" 는 **XState 가 없는 문맥일 때만** 맞다. CET 스레드 시동 문맥은 XState 가 있으므로 그 가정이 안 맞을 수 있음 `[추측]`.
- 다른 호출자 (`NtContinue` 길 밖): `KiVerifyContextIpForUserCet` 의 호출자는 `KiContinuePreviousModeUser` 말고도 `KeVerifyContextIpForUserCet`, `KeVerifyContextRecord` (둘 다 `PspGetSetContextInternal`/`KyRaiseException` 쪽, `SetThreadContext`·예외 올리기 길). 5번째 인자를 위에서 받은 대로 넘길 뿐 (`KeVerifyContextIpForUserCet +0x5d`, `KeVerifyContextRecord +0xdc`) 직접 쓰는 줄 없음. 그 위쪽이 쓰는지는 안 봤음.

### 질문 3. `KiInitiateUserApc` 본문: 유저 APC 가 있을 때 원래 문맥을 어디에 보관하나

- **답: `KiInitiateUserApc` 자신은 보관하지 않는다.** 하는 일은 틀 짜기 + `KiDeliverApc` 하나 부르기.
  - 본문 (208바이트, `.asm`): `+0x0 SUB RSP,0x138` → XMM6~15 와 `RBX,RDI,RSI,R12~R15` 를 `[RSP+0x30..]`/`[RSP+0x100..]` 에 저장 (예외 프레임 칸 만들기) → `+0x5f MOV ECX,1` (이전 모드=유저), `+0x64 MOV RDX,RSP` (예외 프레임), `+0x67 LEA R8,[RBP-0x80]` (트랩 프레임) → **`+0x6b CALL KiDeliverApc` (ret `+0x70`)** → 저장한 레지스터 되돌리고 `RET`. 호출은 이 하나뿐 (`_calls.tsv`).
  - 트랩 프레임이 이미 "원래 문맥" 을 갖고 있으므로 이 함수는 그 프레임 주소만 넘긴다.
- **복사하는 곳 (본문 요약, 이번에 한 단계 더 봄):** `KiDeliverApc +0x5ee` (`nt+0x30a11e`, ret `+0x5f3`) → **`KiInitializeUserApc`** (`nt+0x3ec4a8`). 거기서:
  - `+0x81` / `+0x374` 플래그 `0x10001b` (CET 면 `0x10005b`) 정함 (질문 1 참고).
  - `+0x14b~+0x16e`: 트랩 프레임 `Rsp` (`[RSI+0x180]`) 에서 `0x4d0`바이트(+XState 길이)만큼 내린 자리 `RDI` 를 잡고 `ProbeForWrite` 로 유저 스택에 쓸 수 있는지 확인.
  - **`+0x1ec [RDI+0x30]=플래그`, `+0x1fb CALL KeContextFromKframes(트랩 프레임, 예외 프레임, RDI)`** → 원래 문맥을 **유저 스택 위 `CONTEXT`(0x4d0바이트 + 확장)** 로 복사.
  - `+0x21d CALL KePopulateContinuationContext` (CET 스레드면 원래 `Rip` 를 유저 그림자 스택에 올림, 시동 문맥과 같은 도우미). CET 스레드면 `+0x3e5 KiUnwindUserSspForApcContextCopyBypass(RDI)` 도 부름.
  - `+0x260~+0x27f`: APC 인자 (`NormalContext`, `SystemArgument1/2`, `NormalRoutine`, 플래그) 를 그 `CONTEXT` 맨 앞 홈 칸 `[RDI+0x0~0x20]` 에 적음.
  - `+0x283 [RSI+0x180]=RDI` (트랩 프레임의 유저 `Rsp` 를 그 문맥 자리로), `+0x28a~+0x291 [RSI+0x168]=PsNtdllExports+0x28` (트랩 프레임의 `Rip`) — `+0x28` 칸은 sast/11 에서 `KiUserApcDispatcher` 로 정리됨, `+0x29d` `SegCs=0x33`.
  - 한 줄: **원래 문맥은 유저 스택에 `CONTEXT` 로 복사해 두고, 트랩 프레임의 `Rsp`/`Rip` 를 그 자리와 `KiUserApcDispatcher` 로 바꿔서 유저로 보낸다.** `NtContinue` 로 돌려받는 건 유저 쪽 (`KiUserApcDispatcher`) 몫이고 이 함수들 안에선 안 보임.
- `NtContinueEx` 쪽: `+0x148 TEST byte [thread+0xc2],3` → `+0x151~+0x15a` IRQL 1 로 올리고 `STI` → **`+0x15b CALL KiInitiateUserApc` (ret `+0x160`)** → `CLI`, IRQL 0 → `+0x16a JMP` 로 `+0x13f` (`nt+0x6adf2f`) 에 돌아가 다시 검사. `KiContinueEx +0x114` 도 `KiDeliverApc` 를 직접 부르는 갈래가 있음 (`TEST [thread+0xc2],2`, `+0xd5`).
- test.exe 시동 때 이 길이 불리는지는 정적으로 못 정함 (동적 Q57 몫).

## 동적분석으로 확인해야 할 것

- (Q57) CET 스레드인가: `[thread+0x74]` 비트 20 (`0x100000`). 이게 꺼져 있으면 질문 1 의 값은 `0x10001b`, 질문 2 의 SSP 이야기 전체가 해당 없음.
- (Q57) 시동 때 유저 APC 가 실제로 배달되나: `KiInitiateUserApc` 가 불리나.
- 질문 2 의 `[추측]` 확인: 시동 `NtContinue` 직전·직후 유저 SSP(MSR `0x6a7`) 값 비교.

추천 멈춤점 (모두 조건에 `test.exe` 프로세스 걸 것. `nt` + rva):

| 멈춤점 (함수+오프셋) | nt + rva | 볼 것 |
|---|---|---|
| `PspInitializeThunkContext +0x29a` | `nt+0x910982` | `dd @r15+0x30 L1` → `0x10001b` / `0x10005b`. `RtlCopyContext` 가 끝난 뒤의 맡긴 문맥 플래그 (질문 1 최종값) |
| `PspInitializeThunkContext +0x295` / `+0x29a` | `nt+0x91097d` / `nt+0x910982` | 각각 `rdmsr 6a7` — `KePopulateContinuationContext` 전후 SSP (한 칸 `−8` 확인) |
| `RtlInitializeExtendedContext2 +0xf5` | `nt+0x2b86b5` | `edi` (받은 값) = 쓰는 값. 두 번 걸림(임시 / 유저 스택) |
| `KiContinuePreviousModeUser +0x399` / `+0x39e` | `nt+0x2bcae9` / `nt+0x2bcaee` | `+0x399` 에서 `dq @rbp+8 L1` (넘기는 칸), `+0x39e` 에서 같은 칸 (올라갔나) + 양쪽 `rdmsr 6a7` (MSR 은 **안 바뀜**이면 질문 2 답 확정) |
| `KxContextToKframes +0x211` / `+0x215` | `nt+0x3ea1d1` / `nt+0x3ea1d5` | 양쪽 `rdmsr 6a7`. `XRSTORS` 앞뒤 값이 다르면 이 줄이 유저 SSP 를 쓰는 곳. `+0x211` 에서 `dq @r8+...` 로 CET_U 칸(`[칸]=1`, `[칸+8]`=SSP) 도 볼 수 있음 |
| `KiVerifyContextIpForUserCet +0x128` | `nt+0x411ca0` | 이 줄에 닿으면 "칸 값 == `Rip`" 이 맞은 것 (그림자 스택 맨 위 칸 == `RtlUserThreadStart`) |
| `NtContinueEx +0x15b` | `nt+0x6adf4b` | 닿으면 유저 APC 가 있는 것 (Q57). `KiInitiateUserApc` 입구 `nt+0x6b3260` 은 호출자가 20곳 넘어서 시끄러움 |
| `KiInitializeUserApc +0x1fb` | `nt+0x3ec6a3` | 유저 APC 가 정말 배달될 때만 닿음. `r8` = 유저 스택 문맥 자리 |

## 새 정적 공백

1. **CET_U 칸의 SSP 값이 어디서 오나.** 시동 문맥(CET 스레드)에서 `RtlCopyContext` (`+0x280`) 가 임시 문맥의 CET_U 칸을 복사하는데, 그 칸을 `PspGetContextThreadInternal` (rva `0x90fa40`, 736바이트) / `KeContextFromKframes` (rva `0x3ecaf0`, 1493바이트) 가 어떻게 채우는지 (`XSAVES`? 현재 MSR?), `RtlpCopyXStateChunk` (rva `0x3f5db4`, 700바이트) 가 CET_U 요소를 복사하는지 안 읽음. 이게 "시동 `NtContinue` 뒤 SSP 가 `S0` 인가 `S0−8` 인가" 를 정함. 작은 정적 요청 하나(`^(PspGetContextThreadInternal|RtlpCopyXStateChunk)$` depth 0) 또는 위 `rdmsr 6a7` 동적 한 번으로 풀림.
2. `KeVerifyContextIpForUserCet`/`KeVerifyContextRecord` 를 부르는 `PspGetSetContextInternal` 이 5번째 칸 값을 쓰는지 (이 요청 질문 밖, `SetThreadContext` 길).
3. `FUN_1406f9e12` (이름 없는 조각, `+0xf4` 에서 `WRUSSQ`+`WRMSR 0x6a7`) 가 어느 함수의 조각인지 이름 확인 안 함 (`pdb_aliases.py --where` 안 돌림). `NtContinue` 길과 무관.

## 추가로 뽑은 것 (요청서 표 밖, 이유)

- `RtlCopyContext`, `RtlpCopyLegacyContext`, `RtlpGetContextFlagsLocation` (depth 0): 질문 1 "최종값" 은 `RtlCopyContext` 가 대상 `ContextFlags` 를 건드리는지 봐야 답이 닫혀서.
- `KiInitializeUserApc` (depth 0): 질문 3 "복사하는지" 는 `KiInitiateUserApc` 본문에서 안 보이고 `KiDeliverApc +0x5ee` 가 부르는 이 함수에 있어서. 이미 `_calls.tsv` 에 `KiDeliverApc` 줄이 있어 `KiDeliverApc` 본문은 안 읽음 (범위 밖).
- `KeVerifyContextIpForUserCet`, `KeVerifyContextRecord` (depth 0): `KiVerifyContextIpForUserCet` 의 다른 호출자라서 "새 SSP 를 쓰는 곳" 후보를 지우려고.
- `RtlpCopyContext`: 정규식을 `RtlCopyContext` 와 같이 넣다 이름이 비슷해 같이 뽑힘. 쓰지 않음.
- 임시 파이썬(pefile)로 `ntoskrnl.exe` 코드 영역에서 `B9 A7 06 00 00` 바이트 검색 (`.claude` 스크립트 아님, 임시 파일은 scratchpad). 한계: `MOV ECX,0x6a7` 꼴만 잡음. 다른 식으로 `ECX` 를 채우거나 `XRSTORS`/`XSAVES` 로 MSR 을 건드리는 건 이 검색에 안 잡힘 (그래서 `XRSTORS` 는 따로 `KxContextToKframes` 에서 읽음).

## 판정

**분석성공.** 세 질문 모두 근거 줄과 함께 답이 나옴. 질문 1 은 값과 CET/비CET 표까지 닫힘 (`0x10001b` / `0x10005b`). 질문 2 는 "쓰는 곳 없음" 이 닫히고(전수 바이트 검색 + 부른 쪽 지역 칸 추적), 실제로 SSP 가 어떤 값으로 돌아가는지는 `[추측]` 이라 동적 한 번 권장. 질문 3 은 `KiInitiateUserApc` 본문 요약 + 복사 위치(`KiInitializeUserApc`)까지 닫힘.

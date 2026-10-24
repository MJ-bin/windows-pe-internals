# 2026-10-24 run42 — `NtContinue` 길 세부: 맡긴 문맥 `ContextFlags` 는 **정말 `0x10001b`** (CET 스레드는 `0x10005b`). `KiVerifyContextIpForUserCet` 가 올린 (+8) 새 그림자 스택 포인터는 **아무도 MSR 에 안 쓴다** (지역 칸에서 버려짐). `KiInitiateUserApc` 는 문맥을 보관하지 않고 `KiDeliverApc` 하나를 부르며, 복사는 **`KiInitializeUserApc +0x1fb`** 가 유저 스택에 한다 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 정적: ntoskrnl 26100.9457, pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음). 다시 analyze 안 함.
  - 요청서 대상 6개는 **이미 `ms/decomp`·`ms/disassemble/ntoskrnl` 에 있었다** → 새로 안 뽑고 그대로 읽음.
  - 답을 닫으려고 depth 0 으로 몇 개만 더 뽑음 (요청서 밖): `RtlCopyContext`, `RtlpCopyLegacyContext`, `RtlpGetContextFlagsLocation`, `KiInitializeUserApc`, `KeVerifyContextIpForUserCet`, `KeVerifyContextRecord`. (`RtlpCopyContext` 는 정규식에 같이 걸렸을 뿐 안 씀.)
- 버전 대조 (결과서): `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다. sast/24 (run29) 에 적힌 `+0x8d`·`+0xf0`·`+0x128`·`+0x399/+0x39e`·`+0x15b/+0x160` 도 `.asm` 과 줄 단위로 맞다.
- 이 문서를 쓰며 아래 인용 줄 (`+0x..`) 을 `.asm`·`_calls.tsv` 로 다시 맞춰 봤다. 하나만 결과서 적은 꼴과 다르다 (아래 "도구 메모").
- `WRMSR 0x6a7` 찾기는 결과서가 **임시 파이썬(pefile)** 으로 `ntoskrnl.exe` 코드 영역을 바이트로 훑은 것이다 (`B9 A7 06 00 00` = `MOV ECX,0x6a7` 뒤 40바이트 안에 `WRMSR`). 하네스 스크립트 아님. 한계: `MOV ECX,0x6a7` 꼴만 잡는다. `XRSTORS`/`XSAVES` 로 MSR 을 건드리는 건 이 검색에 안 잡혀서 따로 읽었다.

| 함수 | rva | 이번에 본 것 |
|---|---|---|
| `RtlInitializeExtendedContext2` | `0x2b85c0` | 받은 `ContextFlags` 를 쓰는 줄 `+0xf5`, 검사 `+0x3c` |
| `PspInitializeThunkContext` | `0x9106e8` | 값 정하기 `+0x69~+0x94`, 문맥 둘 (`+0xf0` / `+0x269`), 복사 `+0x280` |
| `RtlCopyContext` · `RtlpCopyLegacyContext` | — | 대상 `ContextFlags` 를 고치나 (`+0xbd`·`+0xc7` / `+0x22`) |
| `KiVerifyContextIpForUserCet` | `0x411b78` | `[R15]` 가 무엇인가 (`+0x69`, `+0x12c`) |
| `KiContinuePreviousModeUser` | `0x2bc750` | 칸 `[RBP+0x8]` 의 쓰임 전부, CET_U 칸 채우기 `+0x46b~+0x47f` |
| `KxContextToKframes` | — | `+0x211 XRSTORS` 조건 |
| `KiInitiateUserApc` | `0x6b3260` | 본문 전체 (208바이트) |
| `KiInitializeUserApc` | `0x3ec4a8` | 원래 문맥을 유저 스택에 복사하는 곳 |
| `NtContinueEx` | `0x6addf0` | 출구 APC 고리 `+0x148~+0x16a` |

## 목적

- Q58 (run29 "아직 확인 필요" 정적 넷 중 셋). 각각 값 하나·함수 이름 하나·예/아니오로.
  1. 맡긴 문맥의 `ContextFlags` 가 정말 `0x10001b` 인가 (run29 `[추측: RtlInitializeExtendedContext2 가 그대로 쓴다고 가정]`).
  2. `KiVerifyContextIpForUserCet` 가 한 칸 올린 (+8) 새 그림자 스택 포인터(SSP)를 누가 유저 SSP (MSR `0x6a7`) 로 쓰나 (run29 `[공백]`).
  3. `KiInitiateUserApc` 본문 — 배달할 유저 APC 가 있을 때 원래 문맥을 어디 보관하나 (run29 `[추측]`).
- 넷째 (`RtlUserThreadStart` 의 `RtlLeaveCriticalSection` 특례 이유) 는 run41 로 답한 것으로 보고 뺐다 (요청서 "넷째 질문은 뺐음").

## 방법

- 정적: `_workspace/sast/37_continue-detail.md` — ntoskrnl, `^RtlInitializeExtendedContext2$`·`^PspInitializeThunkContext$`·`^KiVerifyContextIpForUserCet$`·`^KiContinuePreviousModeUser$`·`^NtContinueEx$` depth 0, `^KiInitiateUserApc$` depth 1. 전부 이미 있던 파일 + 위 depth 0 추가분.
- 결과서: `_workspace/sast/37_continue-detail_result.md` (**분석성공**)

## 결과

비유 (택배 상자에 붙은 체크리스트):

- `ContextFlags` 는 **"이 상자에 뭐가 들었나" 체크리스트**다. 제어(`Rip`·`Rsp`)·정수 레지스터·부동소수·디버그 레지스터 칸에 체크, CET 스레드면 "확장 칸(XState)" 에도 체크.
- 상자를 만드는 함수(`RtlInitializeExtendedContext2`)는 체크리스트를 **받은 그대로 붙인다.** 검사만 하고 고치지 않는다.

### 정적 — 호출 지점 (`_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 / 하는 일 | 표시 |
|---|---|---|---|---|---|
| `nt!PspInitializeThunkContext` | `+0xf0` | `+0xf5` | `RtlInitializeExtendedContext2(R13, R12D, …, R14)` | 커널 스택 **임시** 문맥 | [정적] |
| `nt!PspInitializeThunkContext` | `+0x269` | `+0x26e` | `RtlInitializeExtendedContext2(R15, R12D, …)` | **유저 스택 위 맡길 문맥** | [정적] |
| `nt!PspInitializeThunkContext` | `+0x280` | `+0x285` | `RtlCopyContext(R15, R12D, R13)` | 임시 → 맡길 문맥 | [정적] (run16) |
| `nt!PspInitializeThunkContext` | `+0x295` | `+0x29a` | `KePopulateContinuationContext` | CET 스레드면 그림자 스택에 `Rip` 한 칸 | [정적] (run16) |
| `nt!RtlInitializeExtendedContext2` | `+0x3c` | `+0x41` | `RtlpValidateContextFlags2` | 검사만 (음수면 그대로 돌려줌) | [정적] |
| `nt!RtlCopyContext` | `+0xbd` | `+0xc2` | `RtlpCopyLegacyContext` | 기본 칸 복사 | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x399` | `+0x39e` | `KiVerifyContextIpForUserCet(…, &[RBP+0x8])` | CET 비트 둘 (run29) | [정적] (run29) |
| `nt!NtContinueEx` | `+0x15b` | `+0x160` | `KiInitiateUserApc` | 스레드 `+0xc2` 유저 APC 대기 비트 | [정적] (run29) |
| `nt!KiInitiateUserApc` | `+0x6b` | `+0x70` | **`KiDeliverApc(1, 예외 프레임, 트랩 프레임)`** | 이 함수의 유일한 호출 | [정적] |
| `nt!KiDeliverApc` | `+0x5ee` | `+0x5f3` | **`KiInitializeUserApc`** | 유저 APC 배달 | [정적] |
| `nt!KiInitializeUserApc` | `+0x16e` | `+0x173` | `ProbeForWrite` | 유저 스택에 쓸 수 있나 | [정적] |
| `nt!KiInitializeUserApc` | `+0x1fb` | `+0x200` | **`KeContextFromKframes(트랩, 예외, RDI)`** | 원래 문맥 → 유저 스택 `CONTEXT` | [정적] |
| `nt!KiInitializeUserApc` | `+0x21d` | `+0x222` | `KePopulateContinuationContext` | CET 스레드면 원래 `Rip` 를 그림자 스택에 | [정적] |
| `nt!KiInitializeUserApc` | `+0x3e5` | `+0x3ea` | `KiUnwindUserSspForApcContextCopyBypass(RDI)` | CET 스레드일 때 | [정적] |

### 1. `ContextFlags` = `0x10001b` (CET 스레드 `0x10005b`)

- `[정적]` **값 정하기**: `PspInitializeThunkContext +0x6c AND ECX,0x100000` (스레드 `+0x74` 의 CET 칸) → 켜졌으면 `0x40`, 그리고 **`+0x8d ADD R12D,0x10001b`**. 같은 곳에서 XState 기능 마스크 `R14` = CET 면 `0x800`, 아니면 0 (`+0x72~+0x79`).
- `[정적]` 문맥이 **둘** 만들어지고 둘 다 같은 `R12D` 를 받는다.
  - 임시 (커널 스택, `R13`): `+0xf0` (ret `+0xf5`). 여기에 `PspGetContextThreadInternal` (`+0x168`) 로 지금 스레드 상태를 받는다.
  - **맡길 문맥 (유저 스택, `R15`)**: `+0x263 MOV EDX,R12D` → `+0x269` (ret `+0x26e`). 알림 루프가 `R12` 를 카운터로 쓰면 `+0x153` 에서 `[RBP+0x8]` 로 다시 읽어 같은 값.
- `[정적]` **`RtlInitializeExtendedContext2` 는 받은 값을 그대로 쓴다.**
  - 쓰는 줄: `+0xf5 MOV [RCX+0x30],EDI` (`EDI` = 받은 `ContextFlags`, `RCX` = 16바이트 맞춘 문맥 시작). 이 칸을 다시 쓰는 줄은 없다.
  - `EDI` 에 `OR`/`AND` 하는 줄이 없다. 하는 일은 검사뿐: `+0x3c` `RtlpValidateContextFlags2` → 음수면 그대로 반환 (`+0x43 JS`).
  - 그 검사 함수는 값을 읽기만 한다. `0x10001b`·`0x10005b` 둘 다 통과하는 모양 (`& 0x7ffff20 == 0x100000`, `+0x1e~+0x23`).
  - `0x40` 은 "XState 영역 요청" 으로 읽힌다: `KUSER_SHARED_DATA` 의 `EnabledFeatures` 가 0 이면 `0xc00000bb` 로 거절 (`+0x90~+0xb2`), 아니면 `RtlInitializeExtendedContext2` 가 `CONTEXT_EX` 뒤에 XState **칸**을 만든다 (`+0x9a~+0xa7`, `+0x104~+0x1a8`). **만드는 건 칸이지 `ContextFlags` 가 아니다.**
  - XState 기능 마스크 (4번째 인자, 여기선 `0x800`) 는 켜진 기능으로 깎인다 (`+0x12d~+0x14f`, ret `+0x154` `RtlpRemoveArchDisallowedXStateFeatures`). 이건 마스크 얘기다.
- `[정적]` **그 뒤 `PspInitializeThunkContext` 는 맡긴 문맥의 `ContextFlags` 를 안 고친다.**
  - 이 함수에서 `+0x30` 에 쓰는 줄은 `+0x38a MOV [RBP+0x30],EBX` 하나인데, 이건 오류 갈래의 예외 레코드 칸이다.
  - `RtlCopyContext` (`+0x280`) 는 플래그를 건드리지만 **결과는 같다**: `RtlpCopyLegacyContext +0x22` 가 `(원본 & 요청) & 0x67ffffff` 를 쓰고, 돌아와서 `RtlCopyContext +0xc7 OR [대상 플래그],R12D` 로 대상이 원래 갖던 값을 합친다. 대상이 원래 `0x10001b`/`0x10005b` 이고 들어갈 값은 그 부분집합이라 그대로 남는다.

| 스레드 | 맡긴 문맥 `ContextFlags` | 풀이 | XState 마스크 (`R14`) | 표시 |
|---|---|---|---|---|
| CET 아님 | **`0x10001b`** | `0x100000`(AMD64) + `0x1`(제어) + `0x2`(정수) + `0x8`(부동소수) + `0x10`(디버그 레지스터). 세그먼트 `0x4`·XState `0x40` 없음 | 0 | [정적] |
| CET (`[thread+0x74]` bit 20) | **`0x10005b`** | 위 + `0x40` (XState) | `0x800` (CET_U 칸 하나) | [정적] |

- 비트 이름은 공개 헤더 값이다. `0x40` = XState 인 건 이 바이너리의 `RtlpValidateContextFlags2` 가 `0x100040` 을 XState 로 다루는 것으로도 맞다.
- **헷갈리지 말 것** (run16 에서 이미 정리): 이 함수엔 문맥이 둘이다.
  - (가) 유저 스택 **맡긴 문맥** (`R15`) = 위 표. `LdrInitializeThunk` 가 `Rcx` 로 받아 나중에 `NtContinue` 에 넘긴다.
  - (나) 스레드를 `LdrInitializeThunk` 로 보내는 **작은 문맥** (`[RBP+0xd0]`): `+0x1a8 MOV [RBP+0x100],0x10000b` → **`0x10000b`** (디버그 레지스터·XState 없음). `PspSetContextThreadInternal` (`+0x2d6`) 로 쓴다. run16 표와 같다.
- `[정적]` 겹치는 증거: 유저 APC 가 문맥을 유저 스택에 맡기는 `KiInitializeUserApc` 도 같은 값을 쓴다 — `+0x81 MOV R15D,0x10001b`, CET 면 `+0x32c OR R15D,R12D` (`R12D` = `0x100040`, `+0xb3`) 또는 `+0x374 MOV R15D,0x10005b`, 그리고 `+0x1ec [RDI+0x30] = R15D`. "유저 스택에 맡기는 문맥엔 `0x10001b` (+CET 면 `0x40`)" 가 이 커널의 관례로 보인다 `[추측]`.
- `[정적]` 덧붙임 (`NtContinue` 가 받을 때): `KiContinuePreviousModeUser +0x58` 이 유저 메모리의 `[문맥+0x30]` 을 `EDI` 로 읽는다. 값이 틀리면 `+0x459 AND EDI,0xf810001f` / `BTS EDI,0x14` 로 고쳐 쓰는 갈래가 있다 (`+0xe2~+0xeb`, `+0x453`). 시동 문맥이 멀쩡하면 안 탈 것 `[추측]`.

### 2. 올린 (+8) 새 SSP 는 아무도 MSR 에 안 쓴다

- 비유: 출입 명부(그림자 스택) 맨 위 줄이 맞는지 확인한 사람이 "다음 줄은 여기" 라고 **메모지에 적어서 건넸는데, 받은 사람이 그 메모를 안 보고 버린다.** 명부의 실제 책갈피(MSR)는 다른 경로로 정해진다.
- `[정적]` **그 칸이 무엇인가**:
  - `KiVerifyContextIpForUserCet` 의 `R15` = **5번째 인자** (`+0x69 MOV R15,[RSP+0x90]`). 처음 값을 `[R15]` 에서 읽고 (`+0x71`), 맞으면 **`+0x128 ADD RAX,8` → `+0x12c MOV [R15],RAX`** 로 돌려준다. `[R15]` 에 쓰는 줄은 이 하나뿐. 트랩 프레임·스레드 칸·MSR 에 쓰는 줄은 없다 (`0x6a7` 은 `+0x13a` 의 **RDMSR** 만).
  - 부르는 쪽 `KiContinuePreviousModeUser +0x382 LEA RAX,[RBP+0x8]` → `[RSP+0x20]` → `+0x399` call. 그 칸 `[RBP+0x8]` 의 쓰임 전부:
    - `+0x47` 0 으로 시작, `+0x1a0` 문맥 틀 출력 칸으로 `LEA`, `+0x1c3` 읽음, `+0x1d6` 0 으로 되돌림.
    - **`+0x31a RDMSR` → `+0x323 MOV [RBP+0x8],RAX`** (현재 SSP, XState 에 CET 칸이 있을 때만 `+0x315~+0x323`), `+0x382` 위 `LEA`.
    - **`+0x39e` 뒤로 읽는 줄이 없다.** 그 뒤는 결과 비교 → 성공이면 `+0x422 KeContextToKframes`, 실패 `0xc000060a` 면 `+0x3ad` 로그. → **죽은 값.**
  - `KeContextToKframes` (`+0x422`) 의 인자는 `(트랩, 예외, 커널 복사본, 플래그, 1)` 이라 SSP 를 안 받는다.
- `[정적]` **`WRMSR 0x6a7` 은 `NtContinue` 길에 하나도 없다** (위 바이트 검색). `NtContinueEx`, `KiContinueEx`, `KiContinuePreviousModeUser`, `KiVerifyContextIpForUserCet`, `KeContextToKframes`, `KxContextToKframes`, `KiRestoreSetContextState`, `KiKernelExit` 모두 WRMSR 없음. RDMSR 은 `KiContinuePreviousModeUser +0x315` 쪽과 `KiVerifyContextIpForUserCet +0x13a` 뿐.

| `WRMSR 0x6a7` 이 있는 곳 (함수 +오프셋, nt+rva) | 하는 일 | `NtContinue` 길? |
|---|---|---|
| `KePopulateContinuationContext +0x17~+0x36` (`nt+0x3ecac7`) | `RDMSR` → −8 → `WRUSSQ` → `WRMSR` (칸 하나 넣기 (−8)) | 아니오 (시동 `PspInitializeThunkContext +0x295`, APC `KiInitializeUserApc +0x21d`) |
| `RtlPrepareEnclaveCall +0x60` (`nt+0x46d890`) | `RDMSR` → **+8** → `WRMSR` (칸 하나 빼기 (+8)) | 아니오 (엔클레이브 호출) |
| `KeUserModeCallback +0x37f` (`nt+0x8e7edf`) | 저장해 둔 SSP (`R12`) 를 `WRMSR` | 아니오 (유저 콜백에서 돌아올 때) |
| `KiProcessControlProtection` 조각 (`nt+0x445be6`, 함수 시작 +0x2b6) | `WRMSR` | 아니오 (프로세스 CET 정책 바꿀 때) |
| 이름 없는 조각 `FUN_1406f9e12 +0xf4` (`nt+0x6f9f06`) | `RDMSR` → −8 → `WRUSSQ` → `WRMSR` (칸 하나 넣기 (−8)) | 아니오 (어느 함수 조각인지 이름 안 확인, `KeUserModeCallback` 쪽 `[추측]`) |
| 이름 없는 작은 함수 (`SepIsAppSiloCapability` 바로 뒤, `nt+0x48f164`) | `WRUSSQ [rcx−8]` → `WRMSR` | 아니오 |
| 트랩 진입 조각 약 40개 (`KiApcInterrupt`, `KiInterruptDispatch`, `KiNmiInterrupt` 등, KVA 쪽 `nt+0xbcb122`) | SSP 윗 32비트가 비정상이면 고쳐 씀 (`KiApcInterrupt` 조각에서 확인) | 아니오 (트랩 **들어올 때**) |

- `[정적]` **대신 이 길에서 유저 SSP 를 기계에 쓸 수 있는 곳은 하나**: `KxContextToKframes +0x211 XRSTORS [R8]` (`nt+0x3ea1d1`).
  - 조건: 문맥 플래그에 XState 가 있고 (`+0x193~+0x19f`, `0x100040`), 이전 모드가 유저 (`+0x1a1 CMP R14B,1`). `R8` = 문맥의 XState 영역 (`+0x1b0~+0x1be`). 마스크는 영역 안 압축 마스크 `[R8+0x208]` 과 켜진 기능을 AND (`+0x1c0~+0x207`).
  - 그 영역에 CET_U 칸이 있다는 증거: `KiContinuePreviousModeUser` 가 기능 번호 `0xb` (= 11, 마스크 `0x800`) 칸을 찾고 (`+0x201` ret `+0x206` `RtlLocateExtendedFeature2`), 칸 모양은 `[0] == 1`, `[8]` = SSP (`+0x355` ret `+0x35a` `KiVerifyContextXStateCetUEnabled` 가 `[RCX] == 1`, `[RCX+8]` 이 8의 배수·현재 SSP 이상·그림자 스택 영역 안인지 확인 → 아니면 `0xc000060a`).
  - 다른 갈래 (이 길 밖일 가능성 `[추측]`): 스레드 `+0x198` 칸이 0 이 아니면 `XRSTORS` 대신 `+0x55c` (ret `+0x561`) `RtlCopyXStateArea`, `KUSER` `ControlFlags` 압축 비트가 꺼져 있으면 `+0x3e5` (ret `+0x3ea`) `RtlXRestore`. 어느 쪽인지는 동적.
  - **그 칸에 들어가는 값은 새 SSP(`+8`) 가 아니다.** `KiContinuePreviousModeUser` 가 칸을 채우는 갈래 (`+0x46b BTS R9,0xb`, `+0x478 [R8] = 1`, `+0x47f [R8+8] = RAX`) 는 `+0x31a RDMSR` 로 읽은 **현재 SSP** 를 넣고, 이건 `+0x399` 호출보다 **앞**이다. `+0x399` 뒤에 `[R8+8]` 을 고치는 줄은 없다. 칸이 이미 있으면 (문맥이 가져온 값) 그 값이 쓰이고 `+0x355` 는 검사만 한다.
- `[추측: CPU 설명서 지식, 코드에선 안 보임]` `XRSTORS` 가 CET_U 요소(기능 11)로 `IA32_U_CET` 와 `IA32_PL3_SSP` (MSR `0x6a7`) 를 되돌린다 (Intel SDM). 유저로 `IRETQ` 할 때 CPU 가 `IA32_PL3_SSP` 를 SSP 로 올리는 것도 같은 종류의 지식이다.
- 결론 한 줄:
  - `[정적]` **올린 (+8) 값은 `KiContinuePreviousModeUser` 지역 칸에서 사라지고 MSR 로 안 간다.**
  - `[정적]` + `[추측: 위 CPU 지식]` 유저 SSP 는 (가) XState 가 없는 문맥이면 **MSR 그대로**, (나) XState + CET_U 칸이 있으면 **그 칸의 `[8]` 값** 으로 `XRSTORS` 때 바뀐다.
- `[추측]` 시동 문맥에 대 보면: CET 스레드의 맡긴 문맥은 `0x10005b` (XState 있음) 이고, 그 CET_U 칸은 `RtlCopyContext` (`+0x280`) 가 임시 문맥에서 복사한다. 이 복사는 `KePopulateContinuationContext` (`+0x295`) 보다 **앞**이다. 칸의 SSP 가 "`KePopulateContinuationContext` 가 칸을 넣기 (−8) 전 값" 이라면 `NtContinue` 가 끝났을 때 SSP 는 `RtlUserThreadStart` 칸을 **지나 그 위** 에 있게 된다 (넣은 칸만큼 되돌림 (+8)).
  - 그 칸 값을 누가 어떻게 채우는지 (`PspGetContextThreadInternal` / `RtlpCopyXStateChunk`) 는 이번에 안 읽었다 → 정적 **Q75** (a).
  - 그러니 요청서 가정 "SSP 는 `RtlUserThreadStart` 칸을 가리킨 채로 돌아간다" 는 **XState 가 없는 문맥 (CET 아님) 일 때만** 맞는다. 그런데 CET 아닌 스레드는 그 칸을 애초에 안 넣는다 (−8 없음, `KePopulateContinuationContext` 조건). CET 스레드 시동 문맥은 XState 가 있어서 이 가정이 안 맞을 수 있다 `[추측]`.
- `[정적]` 다른 호출자 (`NtContinue` 길 밖): `KiVerifyContextIpForUserCet` 는 `KeVerifyContextIpForUserCet` (`+0x5d`), `KeVerifyContextRecord` (`+0xdc`) 도 부른다 (`SetThreadContext`·예외 올리기 길). 둘 다 5번째 인자를 받은 대로 넘길 뿐이다. 그 위쪽 (`PspGetSetContextInternal`) 이 그 칸을 쓰는지는 안 봤다 → Q75 (b).

### 3. `KiInitiateUserApc` 는 틀만 짜고, 복사는 `KiInitializeUserApc` 가 유저 스택에

- 비유: 택배 기사가 **배달 전에 원래 하던 일을 메모지(`CONTEXT`)에 적어 받는 사람 문 앞(유저 스택)에 붙여 둔다.** 그리고 받는 사람을 "택배 받는 창구"(`KiUserApcDispatcher`) 로 먼저 보낸다. 다 받으면 그 메모를 보고 원래 일로 돌아간다 (`NtContinue`).
- `[정적]` **`KiInitiateUserApc` (208바이트) 는 자기가 문맥을 보관하지 않는다.**
  - `+0x0 SUB RSP,0x138` → XMM6~15, `RBX`·`RDI`·`RSI`·`R12~R15` 를 스택에 저장 (= 예외 프레임 칸) → `+0x5f ECX = 1` (이전 모드 = 유저), `+0x64 RDX = RSP` (예외 프레임), `+0x67 R8 = [RBP−0x80]` (트랩 프레임) → **`+0x6b CALL KiDeliverApc` (ret `+0x70`)** → 되돌리고 `RET`. 호출은 이 하나뿐.
  - 트랩 프레임이 이미 "원래 문맥" 이라 주소만 넘긴다.
- `[정적]` **복사하는 곳 = `KiDeliverApc +0x5ee` (ret `+0x5f3`) → `KiInitializeUserApc`** (`nt+0x3ec4a8`). `KiDeliverApc` 본문은 안 읽었다 (범위 밖, `_calls.tsv` 줄만).
  1. `+0x81` / `+0x374` 플래그 `0x10001b` (CET 면 `0x10005b`) — 질문 1 과 같은 값.
  2. `+0x14b~+0x16e` 트랩 프레임 `Rsp` (`[RSI+0x180]`) 에서 `0x4d0`바이트 (+XState 길이) 내린 자리 `RDI` → `+0x16e` (ret `+0x173`) `ProbeForWrite`.
  3. **`+0x1ec [RDI+0x30] = 플래그`, `+0x1fb` (ret `+0x200`) `KeContextFromKframes(트랩, 예외, RDI)`** → 원래 문맥을 **유저 스택 위 `CONTEXT`** 로 복사.
  4. `+0x21d` (ret `+0x222`) `KePopulateContinuationContext` (CET 면 원래 `Rip` 를 그림자 스택에, 시동과 같은 도우미). CET 면 `+0x3e5` (ret `+0x3ea`) `KiUnwindUserSspForApcContextCopyBypass(RDI)` 도.
  5. `+0x260~+0x27f` APC 인자 (`NormalContext`, `SystemArgument1/2`, `NormalRoutine`, 플래그) 를 그 `CONTEXT` 앞 홈 칸 `[RDI+0x0~0x20]` 에.
  6. **`+0x283 [RSI+0x180] = RDI`** (트랩 `Rsp` → 그 문맥 자리), **`+0x28a~+0x291 [RSI+0x168] = PsNtdllExports+0x28`** (트랩 `Rip`), `+0x29d SegCs = 0x33`. `+0x28` 칸 = `KiUserApcDispatcher` (sast/11 에서 정리).
- 한 줄: **원래 문맥은 유저 스택에 `CONTEXT` 로 복사해 두고, 트랩 프레임 `Rsp`/`Rip` 를 그 자리와 `KiUserApcDispatcher` 로 바꿔 유저로 보낸다.** 그걸 `NtContinue` 로 돌려받는 건 유저 쪽 (`KiUserApcDispatcher`) 몫이라 이 함수들 안에선 안 보인다.
- `[정적]` `NtContinueEx` 쪽: `+0x148 TEST byte [thread+0xc2],3` → `+0x151~+0x15a` IRQL 1 + `STI` → `+0x15b` (ret `+0x160`) `KiInitiateUserApc` → `CLI`, IRQL 0 → `+0x16a JMP` 로 `+0x13f` (`nt+0x6adf2f`) 돌아가 다시 검사. `KiContinueEx +0x114` 도 `KiDeliverApc` 를 직접 부르는 갈래가 있다 (인자 구조체 길, BOOLEAN 길은 못 탐 — run29).
- test.exe 시동 때 이 길을 타는지는 정적으로 못 정한다 → 동적 **Q57**.

### 호출 관계

```
PspInitializeThunkContext                                    (2.3, 시동 때 한 번)
 ├─ +0x8d    R12D = 0x10001b  (+0x40 if CET)
 ├─ (1) +0xf5   RtlInitializeExtendedContext2(임시 R13, R12D)
 ├─ (2) +0x26e  RtlInitializeExtendedContext2(맡길 R15, R12D)   +0xf5 [CONTEXT+0x30] = 받은 값 그대로
 ├─ (3) +0x285  RtlCopyContext(R15, R12D, R13)                  플래그 그대로 남음
 └─ (4) +0x29a  KePopulateContinuationContext                   CET: SSP (−8), 칸 = RtlUserThreadStart
                       ...  (로더)  ...
NtContinueEx
 ├─ (1) +0x86  KiContinueEx ─ +0xf0 KiContinuePreviousModeUser
 │               ├─ +0x31a RDMSR 0x6a7 → [RBP+0x8]            (CET 칸 있을 때)
 │               ├─ (2) +0x39e  KiVerifyContextIpForUserCet(…, &[RBP+0x8])
 │               │               └─ +0x12c [RBP+0x8] = SSP (+8)   ← 이후 아무도 안 읽음
 │               └─ (3) +0x427  KeContextToKframes ─ +0x7d KxContextToKframes
 │                                └─ +0x211 XRSTORS            (XState 있을 때만, CET_U 칸 [8] → 유저 SSP  [추측: CPU 지식])
 ├─ (2) +0x160  KiInitiateUserApc                              (유저 APC 대기일 때만)
 │               └─ +0x70  KiDeliverApc
 │                          └─ +0x5f3  KiInitializeUserApc
 │                                      ├─ +0x200  KeContextFromKframes   원래 문맥 → 유저 스택 CONTEXT
 │                                      ├─ +0x222  KePopulateContinuationContext
 │                                      └─ +0x283/+0x28a  트랩 Rsp = 그 자리, Rip = KiUserApcDispatcher
 └─ (3) IRETQ / KiKernelExit
```

## 확인된 것

- `[정적]` 맡긴 문맥 (`PspInitializeThunkContext` 의 `R15`) `ContextFlags` = **`0x10001b`** (CET 아님) / **`0x10005b`** (CET). `RtlInitializeExtendedContext2 +0xf5` 가 받은 값을 그대로 쓰고, 그 뒤 `PspInitializeThunkContext`·`RtlCopyContext` 가 바꾸지 않는다.
- `[정적]` 작은 문맥 (`LdrInitializeThunk` 로 보내는 것) 은 따로 `0x10000b` (run16 그대로).
- `[정적]` 그러면 `KxContextToKframes` 가 `Rip`·`Rsp`·`Rcx`·`Rdx` 를 다 옮긴다 (run29 표의 조건 `0x100001`·`0x100002` 둘 다 켜짐). run29 의 "넷 다 옮겨진다" 는 이제 가정 없이 `[정적]`.
- `[정적]` `KiVerifyContextIpForUserCet` 가 올린 (+8) SSP (`+0x12c`) 는 부른 쪽 지역 칸 `[RBP+0x8]` 에서 버려진다. `NtContinue` 길 함수에 `WRMSR 0x6a7` 이 없다 (바이트 검색, `MOV ECX,0x6a7` 꼴 기준).
- `[정적]` 이 길에서 유저 SSP 를 바꿀 수 있는 줄은 `KxContextToKframes +0x211 XRSTORS` 하나 (XState 있는 문맥 + 이전 모드 유저). 그 칸 값은 `+0x399` 보다 앞서 정해진다.
- `[정적]` `KiInitiateUserApc` 는 `+0x6b` (ret `+0x70`) `KiDeliverApc` 하나만 부른다. 원래 문맥을 유저 스택 `CONTEXT` 로 복사하는 건 `KiDeliverApc +0x5ee` (ret `+0x5f3`) → `KiInitializeUserApc +0x1fb` (ret `+0x200`) `KeContextFromKframes`. 그 뒤 트랩 `Rsp`/`Rip` 를 그 자리 / `KiUserApcDispatcher` 로 바꾼다 (`+0x283`, `+0x28a`).

## 정정

- **run29** (맨 위에도 한 줄 넣음):
  - "맡긴 문맥의 `ContextFlags` = `0x10001b` 로 보인다 `[추측: RtlInitializeExtendedContext2 가 값을 그대로 쓴다고 가정]`" → 틀린 건 아니고 **`[정적]` 으로 확정**. 단 CET 스레드는 `0x10005b`. D2 의 "기대 `0x10001b`?" 도 CET 면 `0x10005b`.
  - "검사가 올린 새 그림자 스택 포인터를 … 다시 쓰는 줄은 못 찾았다. 유저 SSP 가 어떻게 되는지는 `[공백]`" → **아무도 안 쓴다 `[정적]`.** 유저 SSP 는 XState 문맥이면 `KxContextToKframes +0x211 XRSTORS` 로 칸 값이 된다 (`[추측: CPU 지식]`).
  - "배달할 APC 가 있으면 트랩 프레임이 CONTEXT 로 덮인 뒤에 그 위에 APC 진입을 만든다 `[추측: KiInitiateUserApc 본문 안 읽음]`" → 방향은 맞다. 다만 만드는 곳이 `KiInitiateUserApc` 자신이 아니라 **`KiDeliverApc` → `KiInitializeUserApc`** 이고, 원래 문맥은 **유저 스택 `CONTEXT`** 로 복사된다 `[정적]`.
- **flow.md 2.10** 의 "원래 문맥은 APC 틀에 보관될 것 `[추측]`" → 위대로 고침 (APC "틀" 이 아니라 유저 스택 `CONTEXT`).

## 아직 확인 필요

동적 (새 번호 없이 출처로. 오프셋은 `nt + rva`, 이번 부팅 값 아님. 모든 프로세스가 지나니 **test.exe 조건 필수**):

- **Q29** (`ContextFlags` 실제 값):
  - `nt+0x910982` (`PspInitializeThunkContext +0x29a`, `RtlCopyContext` 뒤): `dd @r15+0x30 L1` → `0x10001b` / `0x10005b`.
  - (선택) `nt+0x2b86b5` (`RtlInitializeExtendedContext2 +0xf5`): `edi` = 쓰는 값. 두 번 걸린다 (임시 / 유저 스택).
- **Q57** (CET·SSP·유저 APC):
  - CET 스레드인가: `[thread+0x74]` bit 20. 꺼져 있으면 질문 1 은 `0x10001b`, 질문 2 이야기는 전부 해당 없음.
  - `nt+0x91097d` / `nt+0x910982` (`PspInitializeThunkContext +0x295` / `+0x29a`) 양쪽 `rdmsr 6a7` — 칸 넣기 (−8) 확인.
  - `nt+0x2bcae9` / `nt+0x2bcaee` (`KiContinuePreviousModeUser +0x399` / `+0x39e`): `dq @rbp+8 L1` 앞뒤, 양쪽 `rdmsr 6a7` (MSR 이 **안 바뀌면** 질문 2 답 확정).
  - `nt+0x411ca0` (`KiVerifyContextIpForUserCet +0x128`): 닿으면 "그림자 스택 맨 위 칸 == `Rip`".
  - `nt+0x3ea1d1` / `nt+0x3ea1d5` (`KxContextToKframes +0x211` / 다음 줄): 양쪽 `rdmsr 6a7`. 다르면 이 줄이 유저 SSP 를 쓰는 곳. `+0x211` 에서 `@r8` 의 CET_U 칸 (`[칸] = 1`, `[칸+8]` = SSP) 도 볼 수 있다.
  - `nt+0x6adf4b` (`NtContinueEx +0x15b`): 닿으면 유저 APC 가 있는 것. `KiInitiateUserApc` 입구 (`nt+0x6b3260`) 는 호출자가 20곳 넘어 시끄럽다.
  - `nt+0x3ec6a3` (`KiInitializeUserApc +0x1fb`): 유저 APC 가 정말 배달될 때만 닿음. `r8` = 유저 스택 문맥 자리.

정적 (작은 것, 대기열 **Q75**):

1. (a) **CET_U 칸의 SSP 값이 어디서 오나.** 임시 문맥의 CET_U 칸을 `PspGetContextThreadInternal` (rva `0x90fa40`) / `KeContextFromKframes` (rva `0x3ecaf0`) 가 어떻게 채우나 (`XSAVES`? 현재 MSR?), `RtlCopyContext` 가 그 칸을 복사하나. `_calls.tsv` 상 `RtlCopyContext` 안엔 `+0x11c` (ret `+0x121`) `RtlpCopyXStateChunk`, `+0x1b3` (ret `+0x1b8`) `RtlpCopyKernelCetChunk` 가 있다 (이름만, 본문 안 읽음). 이게 "시동 `NtContinue` 뒤 SSP 가 칸 넣기 (−8) 전 값인가 넣은 뒤 값인가" 를 정한다. 위 Q57 `rdmsr` 한 번으로도 풀린다.
2. (b) `KeVerifyContextIpForUserCet`/`KeVerifyContextRecord` 를 부르는 `PspGetSetContextInternal` 이 5번째 칸 값을 쓰나 (`SetThreadContext` 길, 범위 밖).
3. (c) 이름 없는 조각 `FUN_1406f9e12` (`+0xf4` 에서 `WRUSSQ` + `WRMSR 0x6a7`) 가 어느 함수 조각인가 (`pdb_aliases.py --where` 안 돌림, `NtContinue` 길과 무관).

## 도구 메모

- 결과서 질문 3 의 "CET 스레드면 `+0x32c OR R15D,0x100040`" 은 `.asm` 으로 보면 **`+0x32c OR R15D,R12D`** 이고 `R12D` 는 `+0xb3 MOV R12D,0x100040` 에서 온 값이다. 뜻은 같다. 이 문서는 `.asm` 을 따랐다.
- 바이트 검색 (`MOV ECX,0x6a7` → `WRMSR`) 은 하네스 밖 임시 스크립트다. 다른 식으로 `ECX` 를 채우는 `WRMSR`·`XRSTORS`·`XSAVES` 는 이 검색에 안 잡힌다. "길에 없다" 는 이 꼴 기준 + `NtContinue` 길 함수 `.asm` 을 따로 읽은 것 두 가지가 근거다.
- 결과서 질문 2 의 "`+0x1cb JNZ +0x4e2`" (`KxContextToKframes`) 의 `+0x4e2` 는 점프 목적지 **주소 끝자리**다 (`0x1403ea4e2`). 함수 시작 `0x1403e9fc0` 기준 오프셋은 **`+0x522`** 다. 이 문서는 그 줄을 인용하지 않고 호출 자리 `+0x55c` (ret `+0x561`) 만 썼다.
- **디버거가 붙어서 달라질 수 있는 점**: 이번 Q57 확인거리는 `rdmsr 6a7`·`dq` 로 **읽기만** 하니 상태를 바꾸지 않을 것 `[추측]`. 새로 겪은 함정은 없다 (동적 없음).

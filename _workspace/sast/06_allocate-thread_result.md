# 06 allocate-thread — 정적 분석 결과

- 요청서: `_workspace/sast/06_allocate-thread.md` (대기열 Q3)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- analyze 안 함 (`.gpr` + `_index.tsv` + `SetPdb:` 줄 다 있음). export 만 했다.
- 뽑은 함수: 2개 (`^(PspAllocateThread|PspInsertProcess)$`, depth 0) → `ms/decomp/ntoskrnl/PspAllocateThread.c`, `PspInsertProcess.c`, `ms/disassemble/ntoskrnl/PspAllocateThread.asm`, `PspInsertProcess.asm`, `_calls.tsv` (이어서 쌓임)
- 범위 넓힘: 없음. 지도(`_map/NtCreateUserProcess_*`)로 이름·caller·깊이만 확인했고 다른 함수 본문은 안 뽑았다.
- 버전 확인: 일치.
  - `NtCreateUserProcess +0xc36 -> PspAllocateProcess` (`_calls.tsv`)
  - `PspInsertThread +0x807 -> PspCallProcessNotifyRoutines`
  - 요청서의 run3 관찰 `NtCreateUserProcess +0xe5c -> PspAllocateThread`, `+0xfc2 -> PspInsertProcess` 도 `_calls.tsv` 와 같다.

## 스크립트 문제 (고치지 않고 제안만)

**한 줄: `.asm` / `_calls.tsv` / 지도는 `PspAllocateThread` 의 호출 10곳을 놓쳤다. 디컴파일(`.c`)에는 있다.**

- 증상
  - `PspAllocateThread` 안에서 Ghidra 가 어느 함수에도 안 넣은 코드 덩어리가 있다.
    - 주소 `0x1408431b5` ~ `0x14084352a` (함수 시작 기준 `+0x849` ~ `+0xbba`, 약 0x376 바이트).
  - 본체(body)도, `FUN_` 조각도 아니라서 `ExportFunctions.java` 가 건너뛴다.
  - 그 안의 호출이 `.asm`, `_calls.tsv`, `.c` 맨 위 `// callees:`, 지도(`_map`)에서 빠진다.
    - 디컴파일러는 점프를 따라가서 이 코드를 그린다. 그래서 `.c` 에는 있다.
  - 빠진 호출 (call_off → ret_off, 직접 Ghidra 로 읽음):

    | call_off | ret_off | callee | 뭐하는 곳 |
    |---|---|---|---|
    | +0x87a | +0x87f | ObfDereferenceObjectWithTag | 실패 정리 |
    | +0x8bc | +0x8c1 | KeGetProcessorNodeNumber | 이상적 프로세서의 NUMA 노드 |
    | +0x9a9 | +0x9ae | PspSetupUserStack | WOW64 프로세스용 (두 번째 호출 자리) |
    | +0x9e6 | +0x9eb | PspWow64SetupUserStack | WOW64 |
    | +0xa98 | +0xa9d | KeSelectInitialIdealProcessorForThread | 이상적 프로세서 고르기 |
    | +0xaa7 | +0xaac | KeGetProcessorNodeNumberByIndex | 노드 번호 |
    | +0xb34 | +0xb39 | ExfTryToWakePushLock | 오류 때 락 풀기 |
    | +0xb60 | +0xb65 | PspSetupUserShadowStack | CET 섀도 스택 |
    | +0xb78 | +0xb7d | PspWow64InitThread | WOW64 |
    | +0x2ca865 | +0x2ca86a | ExRaiseDatatypeMisalignment | 유저 포인터 정렬 검사 (먼 쪽 덩어리) |

  - 이 줄들은 `_calls.tsv` 에 없다. 이 결과서의 표가 대신이다.
- 규모: 이 커널 전체에서 함수에 안 속한 (INT3·NOP 빼고) 코드 덩어리 1225개, 그중 CALL 이 든 것 244개 (CALL 576개). `PspAllocateThread` 만의 일이 아니다. 지도 숫자(함수 5694개)에도 영향이 있을 수 있다.
- `_index.tsv` 의 size(2850) 는 Ghidra 가 본체로 잡은 바이트 합이다. 실제 함수 크기는 다르다 (아래 질문 4, .pdata 기준 4322바이트).
- 원인 추정: Ghidra 의 "함수 본체" 가 점프 따라가기로 만들어졌는데, 이 덩어리는 따로 디스어셈블돼서 어느 함수에도 안 붙었다. 스크립트는 `getBody()` 안과 `FUN_` 조각만 훑는다 (`ExportFunctions.java` 170·180·185줄 근처, `CallGraphMap.java` 219·227줄 근처).
- 고칠 방법 제안
  1. 본체를 훑은 뒤, 본체·조각 안의 점프(JMP/Jcc) 목적지 중 `getFunctionContaining == null` 인 주소에서 흐름(fallthrough + 점프)을 따라가며 명령을 모아 같은 함수의 일부(`+0x..`, 시작 기준)로 처리한다.
  2. 더 확실한 방법은 PE 예외표(.pdata)의 RUNTIME_FUNCTION 범위로 함수 크기를 잡는 것이다. 체인(`UNW_FLAG_CHAININFO`)된 조각은 부모 함수에 붙인다 (질문 4 참고).
  3. 고친 뒤에는 `_calls.tsv` 와 지도를 다시 만든다.
- 다른 함수(`NtCreateUserProcess`, `PspAllocateProcess` 등 앞 요청서)도 같은 구멍이 있을 수 있다. 한 번 검사하길 권한다.

## 질문별 답 [정적]

### 질문 1. `PspAllocateThread` 가 부르는 함수를 일 단위로 묶기

`PspAllocateThread` (rva `0x84296c`, 인자 12개, 주 caller 는 `NtCreateUserProcess +0xe5c`; 다른 caller 는 `PspCreateThread`, `PspCreatePicoThread`).

일하는 순서 = 주소 순서 (큰 갈림길 말고는 위에서 아래로 한 줄로 간다).

| 순서 | 일 | 대표 (ret_off) | 역할 |
|---|---|---|---|
| 1 | 크기 정하기 | `PoEnergyEstimationEnabled` (+0x228), `KeQueryMaximumGroupCount` (+0x241) | `_ETHREAD` 뒤에 붙일 덤 공간(에너지 추정 +0xc8, 프로세서 그룹 수에 비례한 배열) 계산 |
| 2 | (가) 객체 만들기 | **`ObCreateObjectEx` (+0x2e7)**, `memset` (+0x30c) | `PsThreadType` 으로 `_ETHREAD` 를 만들고 0으로 채움 |
| 3 | 필드 채우기 | `KeInitializeSemaphore` (+0x432), `KeQuerySystemTimeUnsafe` (+0x4af) / `KeQuerySystemTimePrecise` (+0x4c4), `memset` (+0x39a, +0x3cc) | 리스트 머리 초기화, 만든 시각(`CreateTime`), 프로세서 그룹 배열 0으로 |
| 4 | 스레드 락 잡기 | `KeAbPreAcquire` (+0x502), `ExfAcquirePushLockExclusiveEx` (+0x51c) | `_ETHREAD.ThreadLock`(+0x590) 독점 잡기 (인라인 푸시락) |
| 5 | (나) 스레드 ID | **`PsAssignThreadId` (+0x532)** | `Cid` 의 스레드 ID 채움 (`Cid` = `_ETHREAD +0x508`) |
| 6 | (다) 유저 스택 | **`PspSetupUserStack` (+0x5ef)** (+ CET면 `PspSetupUserShadowStack` +0xb65) | 유저 스택 만들기 |
| 7 | (라) TEB | **`MmCreateTeb` (+0x631)** | TEB 만들기 (아래 상세) |
| 8 | (마) 커널 쪽 초기화 | **`KeInitThread` (+0x6f9)** | 커널 스레드(KTHREAD) 초기화. 시작 함수는 이때 넘어감 (질문 2) |
| 9 | 마무리 | `PsInitializeThreadRngState` (+0x710), `ObInitializeFastReference` (+0x71f), `__security_check_cookie` (+0x73c) | 난수 상태, `ProcessFastRef`(+0x790) 에 프로세스 연결, 스레드 포인터를 출력 인자에 저장 |

- (가)~(마) 는 모두 **"예"**. 순서는 (가) +0x2e7 < (나) +0x532 < (다) +0x5ef < (라) +0x631 < (마) +0x6f9.
  - (다)(라) 는 **유저 스레드일 때만**이다 (5번째 인자 `param_5` = 유저 CONTEXT 가 0이 아닐 때).
    - `param_5 == 0` 이면 시스템 스레드 길로 간다 (스택·TEB 건너뜀, 시작 함수는 `PspSystemThreadStartup`).
  - (라) 는 (다) 가 성공했을 때만 이어진다 (+0x5f8 `JS` 로 실패면 정리로).
  - `MmCreateTeb` 이름은 points.md 의 `[확인 필요]` 였는데 **PDB 에 있는 이름이 맞다** (`_index.tsv`: rva 0x9318c4, size 811).
    - 인자 모양: (프로세스, InitialTeb(`*param_6`), `&_ETHREAD.Cid`, 출력 TEB 포인터 `[rsp+0x78]`) `[정적]`. 성공하면 TEB 포인터가 `KeInitThread` 입력에 들어간다.
- 유저 스레드 길 세 갈래 (질문 4와 이어짐). 갈림길은 11번째 인자 `param_11` (= 유저 스택 크기 정보 구조체) 이다.
  - `param_11 != 0` → 위 표의 정상 길 (스택 + TEB 새로 만듦). `PspSetupUserStack(프로세스, CONTEXT, InitialTeb, param_11)`.
  - `param_11 == 0` 이고 `프로세스.PicoContext(+0x640) != 0` → Pico 길 (먼 덩어리 +0x2ca9a9~): `CONTEXT.Rip` 에서 시작 주소를 받고, `param_6` 에서 `UserFsBase`/`UserGsBase`(+0x678/+0x680) 를 복사하고, `ExAllocatePool2` (+0x2caa2c, 태그 "PsSc") 로 `SetContextState`(+0x6a8) 확보.
  - `param_11 == 0` 이고 Pico 도 아님 → "지금 스레드를 본뜨는" 길 (먼 덩어리 +0x2caa57~).
    - 현재(부모) 스레드의 TEB(`KTHREAD.Teb`)를 쓰고, `PsQueryThreadStartAddress` (+0x2caa7c) 로 시작 주소를 가져온다.
    - `KiStackAttachProcess` (+0x2cab47) → `MmSecureVirtualMemory` (+0x2cab5d) 로 TEB 에 값을 쓴다.
    - 스택·TEB 를 새로 안 만든다 (프로세스 복제 같은 경우로 보이나 어떤 경우인지는 코드에서 못 정함 `[추측]`).
  - **`NtCreateUserProcess` 가 `param_11` 에 0이 아닌 값을 주는지는 정적으로 못 정한다.** `+0xe0d` 에서 `[rsp+0x50] = R12` 를 넘기고, R12 는 `+0xadd` 에서 `rbp+0x720` 이거나 `+0xbb5` 에서 0이다.
    - 이 구조체는 `+0xd8e~+0xdbc` 에서 스택 크기(최소 0x40000)로 채워진다. 이미지 헤더 값을 옮기는 것으로 보이나 `[추측]`.
    - → 동적 확인 거리 (아래).
- 큰 갈래 하나 더: `[R15+0x310]` (= `EPROCESS.WoW64Process`) 가 0이 아니면 WOW64 길 (`PspSetupUserStack` (+0x9ae) → `PspWow64SetupUserStack` (+0x9eb), TEB 뒤에 `PspWow64InitThread` (+0xb7d)).
  - 위 "스크립트 문제" 표의 호출들이다. test.exe 가 64비트면 이 길은 안 탄다 `[소스 확인 필요]`.
- 실패 정리 (조각 `frag:` 안): `KiUnstackDetachProcess`, `MmDeleteTeb`, `PspDeleteUserStack`, `ExFreePoolWithTag`, `ExfTryToWakePushLock`, `KeAbPostRelease`, `ObfDereferenceObjectWithTag`, `ObfDereferenceObject`. 성공 길에는 안 나온다.
- 참고: 성공하고 돌아올 때까지 스레드 락(`ThreadLock`)을 푸는 호출이 이 함수 안에 안 보인다 (오류 길에만 있다).
  - 푸는 일은 호출자 쪽(`PspInsertThread`)일 수 있다 `[추측]`. 이 범위 밖.
- 근거: `PspAllocateThread.asm` 의 `+0x2e2`, `+0x52d`, `+0x5ea`, `+0x62c`, `+0x6f4` 줄. `.c` 는 같은 순서(`ObCreateObjectEx` → `PsAssignThreadId` → `PspSetupUserStack` → `MmCreateTeb` → `KeInitThread`).

### 질문 2. 첫 스레드가 어디서 출발하나

- **`KeInitThread` 에 넘기는 값 안에 `PspUserThreadStartup` 의 주소가 있나? → 예.**
  - 근거 줄 (`PspAllocateThread.asm`):
    - `+0x681  LEA RAX,[0x140910de0]` (rva `0x910de0` = `PspUserThreadStartup`, `_index.tsv` 와 같음)
    - `+0x688  MOV [RSP+0x128],RAX`
    - `+0x6e9  LEA RDX,[RSP+0x120]` 로 이 구조체를 2번째 인자로 → `+0x6f4  CALL KeInitThread` (ret +0x6f9)
  - 즉 `rsp+0x120` 에서 시작하는 시작 정보 구조체의 `+0x8` 칸이 `PspUserThreadStartup` 이다.
  - 구조체 칸 (`[정적]` 값 출처 + `[추측]` 칸 이름):

    | 칸 | 값 | 이름 추측 (알려진 `KeInitThread` 인자 순서와 같음) |
    |---|---|---|
    | +0x0 | 0 | KernelStack (안에서 할당) |
    | +0x8 | `PspUserThreadStartup` | SystemRoutine |
    | +0x10 | `param_7` (`NtCreateUserProcess` 는 0을 넘김) | StartRoutine |
    | +0x18 | `_ETHREAD.StartAddress` (`+0x4e0`) | StartContext |
    | +0x20 | `param_5` (유저 CONTEXT) | ContextFrame |
    | +0x28 | `MmCreateTeb` 가 낸 TEB | Teb |
    | +0x30 | 프로세스(`param_1`) | Process |

  - 이 주소를 참조하는 곳은 이 함수 안에서 `+0x681`, `+0x688` 두 곳뿐이다 (Ghidra 참조 표). 함수 밖 두 곳(rva 0x1300b8, 0x177e2c)은 `.pdata`·`GFIDS`(CFG 표) 구간이라 코드가 아니라 표로 보인다.
  - 시스템 스레드 길(`param_5 == 0`)은 다른 시작 함수를 쓴다.
    - `+0x949  LEA RAX,[0x140494470]` = `PspSystemThreadStartup`.
    - 플래그 `0x400` + 보안 프로세스면 `+0xb98 LEA RAX,[0x140aaf3b0]` = `PspSecureThreadStartup`.
    - 그래서 run4·5 의 "모든 새 스레드가 `PspUserThreadStartup` 에서 출발" 은 **유저 스레드 길** 얘기다.
  - 이 함수는 **지도에 없던 `PspUserThreadStartup` 로 가는 길**을 보여준다. 호출이 아니라 주소를 값으로 넘기는 것이다 (`KeInitThread` 가 커널 스택에 시작점으로 심을 것으로 보임, Q6·Q7 몫 `[추측]`).
- **유저 쪽 시작 주소는 어떻게 들어오나?** (정상 길, `param_11 != 0`)
  - `+0x640~+0x647`: `[ETHREAD+0x4e0] = [param_5+0xf8]` → `_ETHREAD.StartAddress` ← `CONTEXT.Rip`.
  - `+0x64e~+0x655`: `[ETHREAD+0x560] = [param_5+0x80]` → `_ETHREAD.Win32StartAddress` ← `CONTEXT.Rcx`.
  - 오프셋 이름은 PDB 타입으로 확인했다 (`_ETHREAD` +0x4d8 안 `StartAddress`(+0x8), +0x560 `Win32StartAddress`; `_CONTEXT` +0x80 `Rcx`, +0xf8 `Rip`).
  - 그러니 01_sprint 의 `Win32 Start Address = test!mainCRTStartup` 는 **이 함수가 `CONTEXT.Rcx` 를 옮겨 적은 값**이라는 뜻이다.
    - `CONTEXT.Rcx`/`Rip` 를 누가 어떤 값으로 채웠는지는 이 함수 밖이다 (`NtCreateUserProcess` 의 `PspCreateUserContext` +0xd22. run9 에서 본 것). 이 함수는 받아서 옮기기만 한다.
  - `PsQueryThreadStartAddress` 는 **`param_11 == 0` 이고 Pico 아닌 길**에서만 쓴다 (+0x2caa7c, 현재 스레드의 시작 주소를 가져옴). 정상 길에서는 안 쓴다.
- 인자 요약: 이 함수의 5번째 인자 `param_5` = 유저 CONTEXT (`NtCreateUserProcess` 가 `[rsp+0x20]` 에 R13 으로 넘김), 6번째 `param_6` = InitialTeb 가리키는 포인터, 11번째 `param_11` = 스택 정보. WinDbg 에서 입구 `rsp+0x28`=5번째, `rsp+0x58`=11번째 (리턴 주소 때문에 +8).
- `KeInitThread` 본문은 뽑지 않았다.

### 질문 3. `PspInsertProcess` (rva `0x84480c`, 본체 2덩어리 846바이트, 먼 덩어리·조각·별칭·간접 호출 없음)

직접 부르는 것, 일 단위로 (ret_off, call_off 는 괄호). 순서는 주소 순서가 아니라 **실행 순서**로 적었다.

| 순서 | 일 | 호출 (ret_off) |
|---|---|---|
| 1 | 감사 켜졌나 보기 | `SepAuditingForSubCategory` +0x58 (+0x53) |
| 2 | 사용자 정책 감사 (전역값 `0x140f04a48 != 0` 일 때만) | `SeCaptureSubjectContext` +0x28e, `SepAdtIncorporatePerUserPolicy` +0x2b4, `SeReleaseSubjectContext` +0x2bd |
| 3 | 프로세스 생성 감사 (1번 결과가 참일 때) | `SeAuditProcessCreation` +0x78 (+0x73) |
| 4 | 잡 상속 (부모 프로세스의 `Job`(+0x2a0) 이 있을 때) | `PspImplicitAssignProcessToJob` +0xd9 (+0xd4) |
| 5 | 시스템 콜 제공자 상속 (부모가 있을 때) | `PspInheritSyscallProvider` +0x94 (+0x8f) |
| 6 | **프로세스 목록에 올리기** | `PspLockProcessListExclusive` +0xa6 (+0xa1) → (직접 코드) → `PspUnlockProcessListExclusive` +0x115 (+0x110) |
| 7 | 디버그 포트 | `ObReferenceObjectByHandle` +0x2f8 (+0x2f3, 디버그 객체 핸들 `param_5` 있을 때), `DbgkCopyProcessDebugPort` +0x143 (+0x13e), `ObfDereferenceObject` +0x313 |
| 8 | 접근 권한 상태 + 객체 등록 | `SeCreateAccessStateEx` +0x1aa (+0x1a5), `ObfReferenceObjectWithTag` +0x1c1, **`ObInsertObjectEx` +0x1e3 (+0x1de)**, `ObfDereferenceObjectWithTag` +0x1f6 |
| 9 | 끝맺음 | `PspValidateJobAffinityState` +0x263 (+0x25e, 새 프로세스에 잡이 있을 때), `ObCheckRefTraceProcess` +0x215 (+0x210), `PspUnlockProcessExclusive` +0x33a / +0x34a (호출자가 락 잡고 불렀을 때 `param_6 & 1`) |
| 실패 | | `SeDeleteAccessState` +0x24d (+0x248) |

- (가) **프로세스 목록에 올리기 → 호출이 아니라 함수 안에서 직접 한다.**
  - 잠금 `PspLockProcessListExclusive` (+0xa6) 과 풀기 `PspUnlockProcessListExclusive` (+0x115) 사이(`+0xa6~+0x110`)에 호출이 없다.
  - 그 사이 코드가 하는 일 (`.asm` 의 `+0xa6~+0x109`):
    - 목록 맨 뒤 포인터(`[0x140f05dc8]`, 머리의 `Blink`)를 읽는다.
    - 머리 `0x140f05dc0`(`PsActiveProcessHead`)와 맞는지 검사하고, 다르면 `INT 0x29`(빠른 실패, +0xc5).
    - 새 프로세스의 `ActiveProcessLinks`(`EPROCESS +0x1d8`)를 맨 뒤에 잇는다 (`InsertTailList` 모양: `+0xe4~+0xf1`).
    - 전역 순번(`0x140f05d00`, 디컴파일에서 `PspProcessSequenceNumber`)을 1 올려서 `EPROCESS.SequenceNumber`(+0x678) 에 적는다 (`+0xf8~+0x109`).
  - 이것이 run3 에서 `PspCallProcessNotifyRoutines` 때 `!process 0 0 test.exe` 가 보인 이유의 정적 근거다.
    - `PspInsertProcess` 는 `NtCreateUserProcess +0xfc2` 이고, `PspInsertThread +0x807`(알림 호출)보다 앞이다.
  - 다만 위 두 줄은 "목록에 올리는 코드가 있다"이다. 실제로 이 길을 탔는지는 동적 확인.
- (나) **핸들 만들기 `ObInsertObjectEx` 를 이 함수가 하나?** → **객체를 등록하는 호출은 하나 (+0x1e3), 하지만 부모에게 줄 핸들을 만드는 호출은 아니다.**
  - `+0x1de` 호출 앞에서 6·7번째 인자 자리(`[rsp+0x28]`, `[rsp+0x30]`)에 0을 넣는다 (`+0x1c1`, `+0x1ca`). 새 핸들을 돌려받는 자리가 없는 모양이다. 5번째는 0/1 값 (`param_6 & 2` 로 정함).
    - `ObInsertObjectEx` 의 인자 이름은 확인 못 했다 `[추측]`.
  - 부모(cmd.exe)가 받는 프로세스 핸들은 run9 에서 본 `NtCreateUserProcess +0x10a7` 의 `PspCreateObjectHandle` 이다 (call +0x10a2).
    - 지도상 `PspCreateObjectHandle` 은 `ObOpenObjectByPointer` (+0x3d) 를 부른다.
    - 이 둘은 **다른 일**이다. 앞이 객체 등록(+참조), 뒤가 부모용 핸들 열기이다.
  - 같은 `ACCESS_STATE` 버퍼를 쓴다. 두 호출 모두 `[rbp+0x400]`:
    - `PspInsertProcess` 의 8번째 인자: `NtCreateUserProcess +0xf92 LEA RCX,[RBP+0x400]; MOV [RSP+0x38],RCX`.
    - `PspCreateObjectHandle` 2번째 인자: `+0x1097 LEA RDX,[RBP+0x400]`.
  - 이 버퍼는 `+0x1065 SeDeleteAccessState` 로 정리된다.
- (다) 감사·잡·디버그 포트, 한 줄씩:
  - 감사: `SepAuditingForSubCategory` (+0x58, 카테고리 0x86) 가 참이면 `SeAuditProcessCreation` (+0x78); 전역 정책이 켜져 있으면 `SepAdtIncorporatePerUserPolicy` (+0x2b4) 가 결과를 바꾼다.
  - 잡: 부모가 잡에 속해 있으면 `PspImplicitAssignProcessToJob` (+0xd9) 로 새 프로세스를 같은 잡에 넣는다 (**목록 락을 잡기 전**). 삽입이 끝난 뒤 새 프로세스에 잡이 있으면 `PspValidateJobAffinityState` (+0x263).
  - 디버그 포트: `DbgkCopyProcessDebugPort` (+0x143) 이 현재 프로세스(`[R15+0xb8]`, 부모)의 디버그 포트를 새 프로세스로 복사한다. 핸들 인자 `param_5` 가 있으면 `ObReferenceObjectByHandle` (+0x2f8) 로 디버그 객체를 먼저 얻는다. 복사되었고 `param_4 & 2` 면 새 프로세스 플래그에 `NoDebugInherit`(0x2) 를 켠다 (+0x322).
- 같은 함수 안에서, 목록에서 풀고 난 뒤 `[현재 프로세스 +0x1f4] & 8` (= `ProcessDelete`) 이면 `0xc000010a`(프로세스가 끝나는 중) 로 실패한다 (+0x115~+0x11e).
- 근거: `PspInsertProcess.asm` 의 위 `+0x..` 줄들, `PspInsertProcess.c` 의 순서 (SepAuditing… → SeAuditProcessCreation → 잡/제공자 → 락 → 목록 → 해제 → DbgkCopy… → SeCreateAccessStateEx → ObInsertObjectEx → ObCheckRefTraceProcess).

### 질문 4. 큰 오프셋 줄 (`+0x2caa17` 등)

- **진짜 `PspAllocateThread` 본체의 일부인가 → 예 (확정).**
  - Ghidra 본체 범위 (`getBody`): `[0x14084296c, 0x1408430ba]`, `[0x1408430bc, 0x140843157]`, `[0x140b0d315, 0x140b0d63b]`, `[0x140b0d681, 0x140b0d690]`.
    - 합계 2850바이트 = `_index.tsv` 의 size 와 같다. 먼 덩어리(`0x140b0d315~`)가 본체 안에 들어 있다.
  - PE 자신의 예외표(.pdata)로 확인했다 (이 바이너리를 직접 읽음). RUNTIME_FUNCTION 두 개:
    - `[0x84296c, 0x84354b)` (0xbdf 바이트, 풀이 정보 플래그 3)
    - `[0xb0d18e, 0xb0d691)` (0x503 바이트, 플래그 4 = **체인(`UNW_FLAG_CHAININFO`)**, 부모가 위 `[0x84296c, 0x84354b)`)
    - 그래서 **진짜 함수 크기는 4322바이트**이고 먼 덩어리는 같은 함수의 "자주 안 가는 길 모음"이다. (이 부모를 가리키는 체인은 이 하나뿐.)
  - 코드 모양도 같다. 먼 덩어리에는 머리말(push, sub rsp)이 없고, 본체와 같은 스택 칸(`[rsp+0x68]`, `[rsp+0x78]`)과 레지스터(`R15`=프로세스, `RSI`=스레드, `R13`=CONTEXT)를 쓰고, 본체로 `JMP` 해서 돌아온다.
  - 이 덩어리 안에 점프가 들어가는 곳이 몇 개 더 있다 (`FUN_140b0d1c1`, `FUN_140b0d265` 도 이 체인 범위 안, 조각).
- **어떤 경우에 그리로 가나** (조건 한 줄씩):
  - `+0x1fd JZ 0x140b0d1cb`: 호출자 모드가 유저(`param_3 == 1`)일 때, 유저 포인터(`param_2`) 정렬·읽기 검사 → 정렬이 틀리면 `ExRaiseDatatypeMisalignment` (+0x2ca86a). (`+0x2ca85f~+0x2ca89b`)
  - `+0x5b3 JZ 0x140b0d315`: 유저 스레드인데 `param_11 == 0` 일 때. 그 안에서 `PicoContext(+0x640)` 로 갈린다 (`+0x2ca9a9`): Pico 면 `+0x2ca9b6~+0x2caa52`, 아니면 `+0x2caa57~` (질문 1 의 두 번째·세 번째 길).
  - `+0x2cab42` `KiStackAttachProcess`, `+0x2cab58` `MmSecureVirtualMemory`: 세 번째 길(현재 스레드 본뜨기) 안에서 TEB 에 값을 쓰려고 잠깐 프로세스에 붙는다 (TEB 쓰기 전 보호).
  - `+0x2caa12 RtlGetExtendedContextLength`, `+0x2caa27 ExAllocatePool2`: Pico 길에서 `SetContextState` 버퍼를 만든다. `+0x2caae1 RtlLocateExtendedFeature`: 세 번째 길에서 CET 확장 상태 확인.
  - 실패 정리와 WOW64 길 일부는 `FUN_140b0d265`(청소), `FUN_140b0d1c1`(실패값 `0xc0000030`) 쪽이다.
- **WinDbg 콜스택과 같은 기준인가?** → **오프셋 값(= 주소 - 함수 시작)은 같은 식으로 쓸 수 있다. 그런데 이름은 `PspAllocateThread` 로 안 보일 가능성이 있다.**
  - 계산: `+0x2caa17` = `0x140b0d383` - `0x14084296c` (rva `0xb0d383` - `0x84296c`). 이 표의 모든 `+0x…` 는 이 식이다.
  - PDB 공개 심볼 표(`llvm-pdbutil-14 dump --publics`)에서 rva `0xb0d18e ~ 0xb0d691` 안에 심볼이 **하나도 없다**.
    - 가장 가까운 앞 **함수** 심볼은 `ConvertDevpropertyToString` (rva 0xaf123c), 바로 앞 심볼(종류 무관)은 문자열 데이터(0xb04eb0).
    - WinDbg 는 이름을 공개 심볼로 붙이므로, 이 줄들은 `nt!PspAllocateThread+0x2caa17` 이 아니라 **엉뚱한 이름+큰 오프셋**으로 보일 수 있다 `[추측]`.
    - 스택 풀기(`k` 가 프레임을 이어 가는 것)는 .pdata 체인으로 되니 프레임은 이어질 것 같다 `[추측]`.
  - 본체 쪽 `FUN_140843198`(+0x82c) 조각과 빠진 호출(+0x849~+0xbba)은 `[0x84296c, 0x84354b)` 안이고 가장 가까운 앞 심볼이 `PspAllocateThread` 라서 `PspAllocateThread+0x…` 로 보일 가능성이 높다 `[추측]`.
  - 조각 줄을 `PspAllocateThread` 기준 오프셋으로 바꾼 값 (`frag:FUN_140843198+x` → `+0x82c+x`, `frag:FUN_140b0d265+x` → `+0x2ca8f9+x`):

    | 조각 | callee | ret_off (조각 기준 → 함수 기준) |
    |---|---|---|
    | `FUN_140843198` | `KiUnstackDetachProcess` | `+0xf` → `+0x83b` |
    | `FUN_140843198` | `MmDeleteTeb` | `+0x3aa` → `+0xbd6` |
    | `FUN_140b0d265` | `KeAbPostRelease` | `+0x8` → `+0x2ca901` (다음 `+0x80` → `+0x2ca979`) |
    | `FUN_140b0d265` | `PspDeleteUserStack` | `+0x31` → `+0x2ca92a` |
    | `FUN_140b0d265` | `ExFreePoolWithTag` | `+0x47` → `+0x2ca940` |
    | `FUN_140b0d265` | `ExfTryToWakePushLock` | `+0x74` → `+0x2ca96d` |
    | `FUN_140b0d265` | `ObfDereferenceObjectWithTag` / `ObfDereferenceObject` | `+0xa0` → `+0x2ca999` / `+0xa8` → `+0x2ca9a1` |

  - 먼 덩어리의 실제 표기는 **모름, 동적 확인 거리** (아래 `ln`).

### 질문 5. 간접 호출·꼬리호출·별칭·조각

- `PspAllocateThread`
  - 간접(`indirect:`, `indirect:cfg:`): **없음**.
  - 별칭(`alias:`): **없음**.
  - `tail` 3곳: `+0x7ea`, `+0x2caccb`, `+0x2cad20` → `FUN_140843198`. 이름은 tail 이지만 진짜 꼬리호출이 아니다.
    - 그 조각은 `KiUnstackDetachProcess` 를 부르고 `JNS 0x140842fe5` 로 **본체로 되돌아온다**(성공하면 `KeInitThread` 준비로, 실패면 정리로).
    - 그래서 콜스택에 영향 없음.
  - 조각(`frag:`): `FUN_140843198` 2곳, `FUN_140b0d265` 7곳. 오프셋 환산은 질문 4 표.
- `PspInsertProcess`: 간접·tail·별칭·조각 **없음**. `INT 0x29`(+0xc5)는 목록 훼손 때의 빠른 실패(`; TRAP`, 호출 아님).
- `alias:` 이름은 대표로 고르지 않았다 (이번에 해당 줄 없음).

## 동적분석으로 확인해야 할 것

dast 요청 필요: 아래 중 우선순위 순.

1. **첫 스레드가 어느 길로 가나** (`param_11` 이 0인가, Pico·복제 길인가).
   - 정적으로 못 정함. test.exe 생성 때 `nt!PspAllocateThread` 입구에서 `poi(@rsp+0x58)` (11번째 인자)가 0인지 본다. 0이 아니면 정상 길 (스택·TEB 만들기).
   - 먼 덩어리 입구 `nt+0xb0d315` 에 걸어서 안 걸리면 정상 길이다.
2. **`KeInitThread` 시작 정보 구조체 안의 값**: `PspUserThreadStartup` 주소가 실제로 +0x8 칸에 들어가는지, `StartAddress`(+0x18) 가 무엇인지.
   - `nt!KeInitThread` 입구(또는 `nt!PspAllocateThread+0x6f4` 에서 call 직전)에서 `dqs @rdx l0n0xc`. 심볼 이름이 보인다.
3. **Win32 Start Address 가 `CONTEXT.Rcx` 와 같은가**: `nt!PspAllocateThread+0x6f9` (`KeInitThread` 끝난 뒤, `rsi` = 스레드)에서 `dt nt!_ETHREAD @rsi StartAddress Win32StartAddress`와 입구에서 본 `dt nt!_CONTEXT poi(@rsp+0x28) Rip Rcx` 비교.
4. **`PspInsertProcess` 의 목록 올리기**: `nt!PspInsertProcess+0x115` (언락 직후)에서 `!process 0 0 test.exe` 가 보이는지. 그리고 `+0x1e3` (`ObInsertObjectEx` 끝) 때 `eax` 와 핸들이 새로 생겼는지 (`!handle 0 3 <부모>` 앞뒤 비교).
5. **먼 덩어리 이름 표기**: WinDbg 에서 `ln nt+0xb0d383` (= `+0x2caa17`), `ln nt+0xb0d315`. `PspAllocateThread+0x…` 로 나오는지, 다른 이름으로 나오는지. 결과가 나오면 reference.md 의 `frag:` 설명을 고칠 근거가 된다.
6. 정상 길이면 스택 안 `PspSetupUserStack` (+0x5ef), `MmCreateTeb` (+0x631) 의 반환값과 TEB 주소.
   - 이어서 `KeInitThread` 의 TEB 인자(+0x28 칸)와 같은지.

- 추천 멈춤점 (모두 test.exe 일 때만, `rcx`(프로세스) 의 `ImageFileName` 조건을 붙인다):
  - `nt!PspAllocateThread` (입구; 인자 확인)
  - `nt!PspAllocateThread+0x6f4` (`KeInitThread` 직전; `rdx` 구조체)
  - `nt!PspAllocateThread+0x6f9` (직후; `rsi` = ETHREAD)
  - `nt!PspInsertProcess+0x115` (목록 풀기 직후)
  - `nt+0xb0d315` (먼 덩어리 입구, 안 걸려야 정상)
- 주의: `PspAllocateThread` 는 cmd.exe 가 아닌 다른 프로세스/스레드 생성에도 불린다 (`PspCreateThread` 에서도 부른다). 조건 없이 걸면 많이 걸린다.

## 확인 방법 (재현용)

- 지도: `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_{nodes,edges}.tsv` 에서 `PspAllocateThread`, `PspInsertProcess`, `MmCreateTeb`, `PsQueryThreadStartAddress`, `PspCreateObjectHandle` 의 이름·caller 수·호출 줄 확인.
- 빠진 호출·함수 본체 범위·PDB 타입(`_ETHREAD`, `_EPROCESS`, `_CONTEXT`, `_KTHREAD`)·참조(`PspUserThreadStartup` 등)는 Ghidra headless(읽기 전용, 분석 안 함)에 **임시 스크립트**를 줘서 읽었다. `.claude/` 는 안 건드렸다.
- 함수 크기·체인(`.pdata`)은 `ntoskrnl.exe` 를 파이썬으로 직접 읽었다 (RUNTIME_FUNCTION + UNWIND_INFO 의 체인 플래그).
- 공개 심볼 위치는 `llvm-pdbutil-14 dump --publics ms/sym/ntoskrnl.exe.pdb` 로 봤다.

## 판정

**분석성공.** 질문 1~5 모두 코드 근거로 답했다. 버전 대조는 일치.

- 단, `.asm`/`_calls.tsv`/지도에 호출 10곳이 빠진 스크립트 문제가 있다 (맨 위 절). 빠진 호출은 이 결과서 표로 메웠다.
- 정적으로 못 정한 것은 "실제로 어느 길을 타나"(`param_11` 값) 와 먼 덩어리의 WinDbg 표기이다.

# 2026-11-12 run82 — CPU 예외를 유저 예외로 넘기기, 유저 쪽 (Q148b, PE_emulator 요청 26): **`KiUserExceptionDispatcher` 는 `RSP` 하나만 받아 `RtlDispatchException` 을 부른다. VEH (PEB `+0x50` 비트 2 가 꺼져 있으면 락도 안 잡고 지나감) → SEH (프레임마다 `RtlLookupFunctionEntry` → `RtlpxVirtualUnwind` → 언어 핸들러) 는 syscall 없이 돈다. 끝맺음은 문맥 이름표 (`ContextFlags`) 가 가른다: `__except` 가 받거나 `RtlRaiseException` 이 받아지면 문맥이 `0x10000f` 라 `RtlRestoreContext` 가 유저 안에서 `IRETQ` (syscall 0), 커널이 쌓은 문맥 (`0x10001f`) 을 그대로 되돌릴 때만 `NtContinue`. 아무도 안 받으면 스레드 맨 위 `RtlUserThreadStart` 의 `__except` → kernelbase `UnhandledExceptionFilter` → 1 → `NtTerminateProcess(-1, 예외 코드)` — 커널 둘째 기회까지 안 간다.** `KiUserExceptionDispatcher +0x29 RtlDispatchException` → `+0x200 RtlpCallVectoredHandlers` → `+0x25c RtlpGetStackLimits` → `+0x37a RtlLookupFunctionEntry` → `+0x3e3 RtlpxVirtualUnwind` → `+0x5f0 RtlpExecuteHandlerForException` → `__C_specific_handler +0xd1 RtlUnwindEx` → `+0x606 RtlRestoreContext` (`+0x181 JZ` → `+0x300 IRETQ`) · 처리됨 `KiUserExceptionDispatcher +0x37 RtlGuardRestoreContext` → `RtlRestoreContext +0x185 NtContinue` · 못 받음 `+0x4e NtRaiseException(…, 0)`

## PE_emulator 쪽에서 쓸 것

- **한 줄 (요청 26 전체 — run81 커널 + 이번 유저)**: nt 엔진이 새로 흉내 낼 것은 **커널 쪽 한 덩어리 쌓기 (run81 규칙 1 ~ 6) 와 syscall 셋 (`NtContinue` · `NtRaiseException` · `NtTerminateProcess`)** 이 전부다 `[정적]`. VEH · SEH · 되감기 · 필터는 진짜 ntdll · kernelbase 가 Unicorn 안에서 한다. 가장 흔한 길 (`__except` 로 받음) 은 **syscall 0 개** — 유저 `IRETQ` 로 끝난다. 아무도 안 받으면 커널 둘째 기회가 아니라 `RtlUserThreadStart` 의 `__except` → `UnhandledExceptionFilter` → **`NtTerminateProcess(-1, 예외 코드)`** 로 끝난다.
  - 원문 "지금 우리" 셋째 줄 "소프트웨어 예외 (`RtlRaiseException`) 는 유저 안에서 돈다 `[추측]`" 은 **맞다** (디버거가 없을 때 = `PEB.BeingDebugged` 0). 틀린 점 둘: 못 받으면 `NtRaiseException(…, FALSE)` 로 커널에 간다 (엔진 구현 필요), 유저 `IRETQ` (`CS = 0x33`, `SS` = 문맥 값) 를 Unicorn 이 그대로 실행해야 한다.
  - run81 덧붙임 ① ("`0x10001f` 로 쌓고 KUSD `XState` 는 진짜 값 `0xe7` 이어도 ntdll 이 괜찮나") 은 **괜찮다** `[정적]` — ntdll 은 이름표에 `0x100040` 두 비트가 다 있어야 `CONTEXT_EX` (`+0x4d0`) 를 읽고, 머신 프레임은 `Rip` (`+0x590`) · `Rsp` (`+0x5a8`) 둘만 읽는다.
- 질문마다 (결과서 sast/79 그대로, 근거 줄은 오케스트레이터 · flow-writer 가 `.asm` 으로 다시 봄). 원문 1 · 2 · 3 · 6 (커널 몫) 은 run81.

| 질문 (원문 요청 26 번호 · sast/79 번호) | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 4 · 1 `KiUserExceptionDispatcher` | 입력은 **`RSP` 하나** (레코드 = `RSP+0x4f0`, 문맥 = `RSP`). `Wow64PrepareForException` 은 64비트에서 0 이라 건너뜀. `RtlDispatchException` 이 TRUE 면 `RtlGuardRestoreContext(문맥, NULL)` → 꼬리 `RtlRestoreContext` → 이름표 `0x10001f` 라 **`NtContinue(문맥, 0)`**. FALSE 면 **`NtRaiseException(레코드, 문맥, 0)`**. 어느 쪽이든 돌아오면 (= 실패) `RtlRaiseStatus(상태)` | 진짜 ntdll 이 돈다. 엔진 몫 = run81 쌓기 + `NtContinue` · `NtRaiseException` 구현 (지금 없음). 진입 `RSP` 아래가 쓸 수 있는 스택이어야 함 (이 아래로 `CALL` 을 쌓음) | `KiUserExceptionDispatcher +0x1` · `+0xb JZ` · `+0x1c ~ +0x26` · **`+0x29`** · `+0x30` · **`+0x37`** · **`+0x4e`** · **`+0x55`**, `RtlGuardRestoreContext +0x178` · `+0x1b6 JMP`, `RtlRestoreContext +0x174 ~ +0x185` |
| 4 · 2 `RtlDispatchException` (VEH · SEH) | VEH 먼저 (`+0x200`): PEB `+0x50` 비트 2 가 꺼지면 **락도 안 잡고** 0. 핸들러가 `-1` 이면 SEH 건너뛰고 TRUE. SEH: `+0x25c RtlpGetStackLimits` → 프레임마다 `+0x37a RtlLookupFunctionEntry` → `+0x3e3 RtlpxVirtualUnwind` → `+0x5f0 RtlpExecuteHandlerForException` (언어 핸들러를 **CFG 없이** `CALL RAX`) → 1 다음 프레임 · 0 TRUE · 2 중첩 · 3 충돌. 끝까지 못 찾으면 FALSE (`+0x64b`). 끝에서 TRUE · FALSE 둘 다 VCH (`+0x218`). **본체에 syscall 0** | 진짜 ntdll. 엔진은 PEB `+0x50` 비트 2 · 3 = 0, `LdrInitState` = 3 (로더 끝), TEB `+0x8` · `+0x10` (스택 한계) 만 맞으면 됨. 틀린 곳 없음 | `RtlDispatchException +0x84` · **`+0x200`** · `+0x207` · `+0x209` · `+0x20c ~ +0x218` · **`+0x25c`** · `+0x32b` · `+0x336` · **`+0x37a`** · **`+0x3e3`** · `+0x3ea` · `+0x3f0 ~ +0x40b` · **`+0x5f0`** · `+0x61d ~ +0x66a` · **`+0x64b`**, `RtlpCallVectoredHandlers +0x3b` · `+0x3e` · `+0x45` · `+0x112 ~ +0x120`, `RtlpExecuteHandlerForException +0xd` |
| 7 · 3 `RtlRaiseException` | **커널을 안 거친다** (디버거 없을 때). `+0xe6 RtlpCaptureContext2` (이름표 `0x10000f`) → `+0x147 RtlVirtualUnwind` 로 자기 프레임 한 칸 → `+0x17e` `BeingDebugged` 검사 → **`+0x188 RtlDispatchException`** → TRUE 면 **`+0x19b RtlRestoreContext` = 유저 `IRETQ`** (syscall 0), FALSE 면 **`+0x2a2 NtRaiseException(…, FALSE)`**. 디버거가 붙어 있으면 `RtlDispatchException` 을 건너뛰고 `NtRaiseException(…, TRUE)`. kernelbase `RaiseException +0x83` 이 여기로 오고, 레코드 주소 = `RaiseException +0x8a` | "유저 안에서 돈다" **맞다**. 틀린 점 둘 — 위 "한 줄" | `RtlRaiseException +0x5f` · `+0x64` · `+0xe6` · `+0x147` · `+0x15f` · `+0x17e` · `+0x182` · **`+0x188`** · **`+0x19b`** · `+0x294` · `+0x29f` · **`+0x2a2`**, `RtlCaptureContext2 +0xc0` · **`+0xf3`**, kernelbase `RaiseException +0x4a` · **`+0x83`** |
| 5 (유저 몫) · 4 아무도 안 받을 때 | 스레드 맨 위 `RtlUserThreadStart` 의 `__try [+0x7, +0x4a)` → 필터 `$filt$0 +0x20 RtlDecodePointer(RtlpUnhandledExceptionFilter)` = kernelbase `UnhandledExceptionFilter` (kernelbase 시작 함수 `+0x4ab` 가 넣은 값) → syscall `NtQueryInformationProcess(7)` → `NtSetInformationProcess(0x3f)` ×2 → `NtQueryInformationProcess(0xc)` (비트 `0x2` 면 여기서 1) → `NtQueryInformationJobObject` → (WER 이름만 / 고전 길 `NtRaiseHardError`) → **기본 반환 1** → `RtlUnwindEx` → 유저 `IRETQ` → 착지 `+0x4a` → **`+0x50 NtTerminateProcess(-1, 예외 코드)`**. 커널 둘째 기회까지 안 감 | 진짜 ntdll · kernelbase 가 돈다. 엔진 몫 = 아래 syscall 표. 지금 "Unicorn 오류 → `stop`" 은 이 길 앞 (`KiUserExceptionDispatcher` 이전) 이라 해당 없음 | `RtlUserThreadStart` scope table (결과서 `.pdata`) · `+0x4a ~ +0x55`, `$filt$0 +0x19` · `+0x20` · `+0x3a` · `+0x4c`, `__C_specific_handler +0x91` · `+0x93` · `+0xd1`, `RtlUnwindEx +0x892` · `+0x3f9` · `+0x40c` · `+0x606`, kernelbase `UnhandledExceptionFilter` 아래 표 |
| 6 (유저 몫) · 5 `RaiseFailFastException` | `int 0x29` 와 **다른 길**. 일반 syscall `NtRaiseException(…, FALSE)` (코드 기본 `0xC0000602`, NONCONTINUABLE) 로 유저 SEH · VEH 를 안 거치고 커널 둘째 기회 → 종료. 앞에 WER 신호 (`SignalStartWerSvc` · `NtQuerySystemInformation(0x73)` · `WaitForWerSvc`), 실패면 `NtRaiseHardError` → `TerminateProcess` | `int 0x29` 는 run81 규칙 그대로 (훅 → `0xC0000409` 종료). 이건 `NtRaiseException(FALSE)` → 종료만 있으면 됨 | kernelbase `RaiseFailFastException +0x55` · `+0xa1` · `+0xb3` · `+0xc0` · `+0xd9` · `+0x12e` · `+0x13e` · **`+0x14c`** · `+0x181` |
| run81 덧붙임 ① · 덧붙임 1 · 2 `CONTEXT_EX` · 머신 프레임 | 이름표에 `0x100040` 이 다 있어야 `+0x4d0` 를 읽음 → `0x10001f` 면 안 읽음 (KUSD `0xe7` 이어도). 머신 프레임은 `Rip` · `Rsp` 둘만 (`CS` · `EFlags` · `SS` 안 읽음). `RtlpContinuationContextMachineFrameEntries` 는 CET 그림자 스택용 — test.exe 영향 없음 | run81 규칙 2 · 5 의 "XSAVE 없이 `0x10001f`" 쉬운 길이 ntdll 쪽에서도 **맞다** | `RtlpSanitizeContext +0x39 ~ +0x43`, `RtlpCopyContext +0x1f ~ +0x2b`, `RtlRestoreContext +0x1a3 ~ +0x1e3`, `RtlpUnwindPrologue +0x404 ~ +0x474` · `+0x477` · `+0x487`, `RtlpValidateContextFlags2 +0xa → +0x15` |

### 문맥 이름표 (`ContextFlags`) 가 "`NtContinue` 를 부르나" 를 가른다 (결과서 "먼저 알아 둘 것", flow-writer `.asm` 확인)

| 어디서 만든 문맥 | 이름표 | 끝맺음 | 근거 |
|---|---|---|---|
| 커널이 유저 스택에 쌓은 것 (run81) | `0x10001f` (XSTATE 면 `0x10005f`) — `0x10` (DEBUG_REGISTERS) 포함 | `0x10000f` 와 달라서 **`NtContinue(문맥, 0)`** | run81 질문 3 |
| `RtlRaiseException` 이 잡은 것 | 처음 `0x10000b` (KUSD `+0x3d8` 가 0 이 아니면 `0x10004b`) → 잡은 뒤 **`0x10000f`** (`0x10004f`) | **유저 `IRETQ`** | `RtlRaiseException +0x64` · `+0x8d` · `+0x1e2` · `+0xdc` · `+0xe6`, `RtlpCaptureContext2 +0x6c` (→ `RtlCaptureContext2 +0x3c` 로 점프), `RtlCaptureContext2 +0xc0 AND [ctx+0x30],0x100040` · **`+0xf3 OR [ctx+0x30],0x10000f`** |
| `RtlUnwindEx` 이 잡은 것 (`__except` 로 되감을 때) | **`0x10000f`** (`RtlCaptureContext2`, call `+0x892`) | **유저 `IRETQ`** | 같은 `RtlCaptureContext2 +0xf3` |
| `RtlDispatchException` 의 지역 사본 | `원본 & 0x10000f` | (되감기용, 끝맺음 아님) | `RtlpCopyContext +0x31 ~ +0x3a` |

- 가르는 곳 = **`RtlRestoreContext` (rva `0x123e20`)** 한 곳 `[정적]`:
  - `+0x150 ~ +0x172`: `LdrSystemDllInitBlock +0xa8` 의 비트 `0x3c` · `0xc` (CET) 중 하나라도 켜지면 `+0x183` (`NtContinue`) 으로. test.exe 는 둘 다 꺼짐 (`+0xa8 = 0` `[관찰]` run44 K1 · P1a · P1b · P2, sast/55 결과서).
  - **`+0x174 MOV EAX,[RCX+0x30]` · `+0x177 AND EAX,0xffffff3f` · `+0x17c CMP EAX,0x10000f` · `+0x181 JZ +0x1a3`** → 같으면 유저 안에서 복원 (`+0x1e7 FXRSTOR` · XMM0 ~ 15 · `+0x266 LDMXCSR` · 레지스터 · **`+0x300 IRETQ`**), 다르면 **`+0x183 XOR EDX,EDX` · `+0x185 CALL NtContinue`** (ret `+0x18a`). `+0x18a CMP EAX,0xc000060a` → 같으면 `+0x196 INT 0x29` (ECX `0x30`), 아니면 그냥 돌아감.
- `RtlGuardRestoreContext` (rva `0xa59f0`, PDB 이름 — Ghidra 이름은 `RtlRestoreContext`, 파일 `RtlRestoreContext__a59f0.asm`) 는 얇은 막이다. 레코드 NULL 이면 `+0x178 CMP [LdrSystemDllInitBlock+0xb8],0` → test.exe 는 0 이라 **`+0x1b6 JMP RtlRestoreContext`** (문맥 그대로). `+0xb8 ≠ 0` 인 프로세스만 `RtlGuardIsValidStackPointer` (`+0x192`) 를 부르고 실패면 `+0x210 INT 0x29` (ECX `0xd`).

### nt 엔진이 줄 답 — syscall 표 (결과서 질문 6 그대로, 원문 4 · 5 "나오는 syscall 표")

syscall 번호 = ntdll `Nt…` 스텁의 `MOV EAX,…` (결과서가 `ntdll.dll` 에서 읽음). KUSD `+0x308` 비트 0 이 꺼지면 `syscall`, 켜지면 `int 0x2e`. "UEF" = kernelbase `UnhandledExceptionFilter`. 자리는 `call / ret`.

| syscall (번호) | 언제 (자리) | 인자 | 진짜가 기대하는 답 | 실패 · 다른 답이면 |
|---|---|---|---|---|
| **`NtContinue` (`0x43`)** | 커널이 쌓은 문맥 (`0x10001f`) 이 처리돼 돌아갈 때 — VEH `-1`, SEH `ExceptionContinueExecution` 둘 다: `KiUserExceptionDispatcher +0x37/+0x3c` → `RtlGuardRestoreContext +0x1b6` → `RtlRestoreContext +0x185/+0x18a`. 이름표가 `0x10000f` 가 아닐 때만 | `RCX` = CONTEXT* (= 디스패처 `RSP`, 핸들러가 고친 값 포함), `EDX = 0` (TestAlert 거짓) | **돌아오지 않음.** CONTEXT 로 레지스터 복원: `Rip` · `Rsp` · `EFlags` · `CS` · `SS` + `Rax ~ R15` + XMM · `MxCsr` + (`0x10` 비트면) `Dr0 ~ 7` (run29 · run42) | `0xC000060A` 면 `RtlRestoreContext +0x196 INT 0x29` (ECX `0x30`). 그 밖은 `KiUserExceptionDispatcher +0x53/+0x55 RtlRaiseStatus(상태)` → 되풀이 끝에 `NtRaiseException(FALSE)` |
| **`NtRaiseException` (`0x174`)** | (a) `KiUserExceptionDispatcher +0x4e/+0x53` (디스패치 FALSE) (b) `RtlRaiseException +0x2a2/+0x2a7` (FALSE, 또는 `BeingDebugged` 면 TRUE) (c) `RtlUnwindEx +0x743/+0x748` (목적 프레임 못 찾음) (d) `RtlRaiseNoncontinuableException +0x6b` (꼬리 `JMP`) (e) kernelbase `RaiseFailFastException +0x14c/+0x153` · `+0x1bf/+0x1c6` | `RCX` 레코드, `RDX` 문맥, `R8B` FirstChance | FirstChance **거짓**: 안 돌아옴 — run81 질문 4 (디버거 · 예외 포트 없음 → `ZwTerminateProcess(-1, ExceptionCode)`). **참**: run81 질문 3 · 4 (유저 문맥을 트랩 프레임에 넣고 `KiUserExceptionDispatcher` 로 다시) | 오류 상태 (개수 > 15 → `0xC000000D` 등) 가 돌아오면 `RtlRaiseStatus(상태)` |
| **`NtTerminateProcess` (`0x2c`)** | `RtlUserThreadStart +0x50/+0x55` (필터 1 → 되감은 뒤 `__except` 본문). UEF `+0x578/+0x57f` (`BasepAlreadyHadHardError` 일 때). kernelbase `TerminateProcess` (`RaiseFailFastException` 끝) | `RCX = -1` (현재 프로세스), `EDX` = 예외 코드 | **안 돌아옴** (run79: 자기 프로세스면 스레드들을 끝내고 자기도) | 돌아오면 `RtlUserThreadStart +0x55` 뒤로 흘러 `RET` (정의 안 된 길) |
| **`NtQueryInformationProcess` (`0x19`)** | (i) `BasepIsDebugPortPresent` — UEF `+0xd5` · `+0x202` · `+0x3b0` (클래스 `7` = ProcessDebugPort, 8B) (ii) `GetErrorMode` — UEF `+0x29a` (클래스 `0xc` = ProcessDefaultHardErrorMode, 4B) (iii) `RtlDecodePointer +0x4f` (클래스 `0x24` = 쿠키, 4B, 캐시 없을 때만) | `RCX = -1` | (i) **0**, `STATUS_SUCCESS` (디버거 없음) (ii) `EPROCESS.DefaultHardErrorProcessing` (비트 `0x2` 면 UEF 가 WER 없이 1 로 끝) (iii) 프로세스 쿠키 | (i) 오류면 "디버거 없음" 으로 침 (`+0x30 JS`) (ii) 오류면 값 0 으로 침 `[추측]` |
| **`NtSetInformationProcess` (`0x1c`)** | UEF `+0x110/+0x117` (값 8, `RtlKnownExceptionFilter ≠ -1` 일 때) · `+0x282/+0x289` (값 1) | `RCX = -1`, 클래스 `0x3f`, 8B 버퍼, 길이 8 | `STATUS_SUCCESS` (반환값 안 봄) | 어떤 상태든 무시 |
| **`NtQueryInformationJobObject` (`0x156`)** | UEF `+0x2d1/+0x2d8` (비 secure · `GetErrorMode` 비트 없음 · 스레드 에러 모드 `& 0x20` 없음) | `RCX = NULL` (내 프로세스의 잡), 클래스 2, 버퍼 `0x40`, 반환 길이 NULL | 잡에 안 속하면 오류 상태 | 오류 (< 0) 면 무시하고 WER 길로. 성공이고 `LimitFlags & 0x400` 이면 UEF 가 1 로 끝 |
| **`NtRaiseHardError` (`0x175`)** | UEF `+0x4ec/+0x4f3` (WER 가 안 정한 기본 길), `RaiseFailFastException +0x113/+0x11a` | `ECX = 0xD0000144`, `EDX = 4`, `R8 = 0`, `R9 = &{코드, 주소, 정보0, 정보1}`, 스택 `{옵션 1 또는 2, &응답}` | `STATUS_SUCCESS` + 응답 **6 (OK)** | 오류 심각도 상태면 UEF 가 응답을 6 으로 침 (`+0x4f8 ~ +0x503`). 응답 3 (취소) 이면 AeDebug 쪽 |
| `NtQuerySystemInformation` (`0x36`) | 코드 `0xC0000409` 일 때만 UEF `+0x74/+0x79` (클래스 `0x23`, 2B). `RaiseFailFastException +0xd9/+0xe0` (클래스 `0x73`, 8B) | | 클래스 `0x23`: `{KernelDebuggerEnabled, NotPresent}` (`[0] ≠ 0` 이고 `[1] == 0` 이면 디버거 있음) | 오류면 "없음" |
| 안쪽 (평소 안 탐) | `NtQueryVirtualMemory` (`0x23`, `LdrInitState ≠ 3` 일 때) · `NtAlertThreadByThreadIdEx` (`0x72`) · `NtWaitForAlertByThreadId` (`0x1e3`) · `NtSetInformationThread` (`0xd`) (함수표 락 경합) · `NtProtectVirtualMemory` (`0x50`, VEH 빼기) · `NtContinueEx` (`0xa5`, `RtlContinueLongJump`) | | | |
| 로더 · WER (이름만) | UEF `+0x315 LoadLibraryExW`, delay-load `+0xb8` · `+0x345`, `WerpGetDebugger`, `VirtualAllocExNuma` / `VirtualFreeEx` (`NtAllocateVirtualMemoryEx` `0x78` / `NtFreeVirtualMemory` `0x1e` `[추측]`), `SignalStartWerSvc` / `WaitForWerSvc` (`NtQueryWnfStateNameInformation` · `NtUpdateWnfStateData` · `NtOpenEvent` · `NtWaitForSingleObject` · `NtClose`), WerFault (ALPC) | | | |

- **syscall 이 아예 없는 길** (표에 안 나오는 가장 중요한 것): 처리하는 쪽이 SEH `__except` 면 (`RtlUnwindEx` → `RtlRestoreContext`, 이름표 `0x10000f`) **`NtContinue` 도 안 부르고 유저 `IRETQ`** 로 끝난다. `RtlRaiseException` 이 받아졌을 때도 같다. `NtContinue` 가 필요한 건 커널이 쌓은 문맥을 `KiUserExceptionDispatcher` 가 되돌릴 때 (VEH `-1` · SEH `ExceptionContinueExecution`) 뿐이다.
- PE_emulator 가 꼭 만들 것은 첫 세 줄 (`NtContinue` · `NtRaiseException` · `NtTerminateProcess`). 은퇴용 `NtContinue` 가 있으면 같은 구현을 이름표 `0x10001f` (`Dr0 ~ 7` 포함) 문맥에 쓰면 된다. 나머지 (`NtQueryInformationProcess` 클래스 7 · `0xc` · `0x24`, `NtSetInformationProcess` `0x3f`, `NtQueryInformationJobObject`, `NtRaiseHardError`) 는 "UEF 까지 가는 프로그램" 에서만.

### 요청 26 전체 최소 규칙 (run81 커널 + run82 유저, 결과서 끝 그대로)

전제: 64비트 exe, 유저 모드에서 난 예외, 디버거 (유저 · 커널) 없음, WER 없음 (또는 `ProcessDefaultHardErrorMode & 2`), 비 CET (`LdrSystemDllInitBlock +0xa8 = 0`, `+0xb8 = 0`), 계측 콜백 · Pico 없음. 앞의 **[nt]** = 엔진이 직접, **[ntdll]** = 진짜 ntdll 이 Unicorn 안에서.

1. **[nt] CPU 예외가 남** (Unicorn 오류 · 인터럽트 훅) → run81 예외 코드 표로 `ExceptionCode` · `ExceptionAddress` · `NumberParameters` · `ExceptionInformation` 을 정한다 (`#PF` `0xC0000005` · `[0]` 읽기 0 / 쓰기 1 / 실행 8 · `[1]` 주소 · `#GP` 특권 `0xC0000096` · `#UD` `0xC000001D` · `#DE` `0xC0000094` / `0xC0000095` · `int3` `0x80000003` 주소 = `int3` 자리, 문맥 `Rip` 도 `int3` 자리 · `ExceptionFlags = 0`).
2. **[nt] 문맥 · 배치 만들기** (run81 질문 3): `S0 = RSP`, `M = (S0 − 0x28) & ~0xF`, `E = M − 0xA0`, `X = E − 0x20`, **`C = X − 0x4D0`**. `C` 에 CONTEXT (`ContextFlags = 0x10001f`, `SegCs = 0x33`, `SegSs/Ds/Es/Gs = 0x2b`, `SegFs = 0x53`, `Rsp = S0`, `EFlags` = 순간 값), `C+0x4d0` 에 `CONTEXT_EX` 0x20B (XSTATE 없어도 씀 — **ntdll 은 이름표에 `0x40` 이 없으면 안 읽으니 값은 상관없음**), `C+0x4f0` 에 레코드 (0x98 을 0 으로 채우고 앞 0x20 + 개수 × 8), **`C+0x590` = `Rip`, `C+0x5a8` = `Rsp = S0`** (유저 걷기가 읽는 두 칸. `+0x598` · `+0x5a0` · `+0x5b0` 은 안 읽으니 안 써도 됨). 쓸 수 없는 주소면 run81 3-6 (스택 넘침 `0xC00000FD` 로 한 번 더 / 그 밖 → 둘째 기회).
3. **[nt] 레지스터 바꾸기** (run81): `RSP = C`, `RIP = KiUserExceptionDispatcher` (`PsNtdllExports +0x20`, ntdll rva `0x164e10`), `CS = 0x33`, `EFLAGS = 순간 값 & ~0x100`. **다른 레지스터는 그대로.** TEB `+0x8` · `+0x10` (스택 한계) 이 새 `RSP` 아래를 포함해야 한다 (이번 질문 2: `RtlpGetStackLimits`).
4. **[ntdll] `KiUserExceptionDispatcher`**: `Wow64PrepareForException` 은 0 이라 건너뜀 → `RtlDispatchException(레코드 = RSP+0x4f0, 문맥 = RSP)` (질문 1).
5. **[ntdll] VEH**: PEB `+0x50` 비트 2 가 0 이면 곧장 지나감. 비트가 켜졌고 핸들러가 `-1` 이면 TRUE.
6. **[ntdll] SEH 걷기**: `RtlLookupFunctionEntry` (`LdrInitState == 3` 이라 syscall 없음) + `RtlpxVirtualUnwind` (머신 프레임 `C+0x590` · `+0x5a8` 로 예외 난 함수 프레임으로 넘어감) → 언어 핸들러 `__C_specific_handler` → 필터. 결과 갈래:
   - **필터가 1 (`__except` 받음)** → `RtlUnwindEx` → `RtlRestoreContext(문맥 0x10000f)` → **유저 `IRETQ` (syscall 없음).**
   - **VEH `-1` 또는 SEH `ExceptionContinueExecution`** (TRUE) → `RtlGuardRestoreContext` → `RtlRestoreContext(0x10001f)` → **`NtContinue(문맥, FALSE)`** [nt]: 문맥으로 레지스터 복원 (`Dr0 ~ 7` 포함), 안 돌아옴. `Noncontinuable` 인데 0 이면 `RtlRaiseStatus(0xC0000025)`.
   - **아무 데서도 안 받고 FALSE** → `NtRaiseException(레코드, 문맥, FALSE)` [nt] → 8.
7. **[ntdll → nt] 스레드 맨 위 `RtlUserThreadStart` 의 `__except`** (`$filt$0` → 전역 `RtlpUnhandledExceptionFilter` = kernelbase `UnhandledExceptionFilter`): 평소 "아무도 안 받음" 은 여기서 잡힌다. UEF 가 부르는 syscall (질문 4 표): `NtQueryInformationProcess(ProcessDebugPort)` → 0 · `NtSetInformationProcess(0x3f)` ×2 → 성공 · `NtQueryInformationProcess(ProcessDefaultHardErrorMode)` → 값 (비트 `0x2` 면 여기서 1 로 끝) · `NtQueryInformationJobObject` → 오류 · (WER 길 / 고전 길은 이름만) → 반환 **1** → `__C_specific_handler` → `RtlUnwindEx` → `IRETQ` → `RtlUserThreadStart +0x4a` → **`NtTerminateProcess(-1, 예외 코드)`** [nt]: 프로세스를 끝냄, 끝 상태 = 예외 코드 (run79).
8. **[nt] 둘째 기회** (`NtRaiseException(…, FALSE)` — 위 6 의 FALSE, UEF 가 0 을 돌려줄 때): run81 질문 4 → 디버거 · 예외 포트 없음 → `ZwTerminateProcess(-1, ExceptionCode)`. 쓰기 실패 · `int 0x29` (`__fastfail`) 도 같은 끝 (유저 핸들러 안 탐, 코드 `0xC0000409`).
9. **[ntdll] 소프트웨어 예외** (`RaiseException` / `RtlRaiseException`): 커널을 안 거침 (`BeingDebugged == 0`). 잡기 → `RtlDispatchException` 직접 → 받으면 `RtlRestoreContext(0x10000f)` = 유저 `IRETQ` (syscall 없음), 못 받으면 `NtRaiseException(FALSE)` → 8. `RaiseFailFastException` 은 SEH · VEH 안 거치고 곧바로 `NtRaiseException(FALSE)` (WER 길 syscall 이 실패해도 끝은 `TerminateProcess`).
10. **엔진이 만들어야 하는 syscall 최소 집합**: `NtContinue`, `NtRaiseException`, `NtTerminateProcess` (이 셋은 필수) + UEF 길용 `NtQueryInformationProcess` (클래스 7 · `0xc` · `0x24`), `NtSetInformationProcess` (클래스 `0x3f`), `NtQueryInformationJobObject` (오류 반환), `NtRaiseHardError` (WER 안 쓰는 길일 때). 유저 `IRETQ` 를 Unicorn 이 `CS = 0x33` / `SS` = 문맥 값으로 실행할 수 있어야 한다.
11. **안 흉내 내도 되는 것**: 디버거 갈래, WoW64 (`Wow64PrepareForException`), CET 그림자 스택 (`RtlpContinuationContextMachineFrameEntries` · `RtlpVirtualPopShadowStack`), XSTATE 꼬리, `RtlGuardCheckExceptionHandler` (`+0xb8 = 0`), 예외 로그, WER 본체.
12. **run81 규칙 7 에 보탬**: run81 의 7 번 ("`NtRaiseException(…, FALSE)` … 은 프로세스 종료") 은 맞다. 다만 **평소 "아무도 안 받음" 은 커널 둘째 기회로 안 가고 유저 `RtlUserThreadStart` 의 `__except` 에서 끝난다** (위 7). 둘째 기회 길은 필터가 0 을 돌려줄 때 (디버거 포트 있음 · secure 프로세스 · 중첩 플래그 · WER 답 8) 나 걷기가 FALSE 일 때, 그리고 `RaiseFailFastException` · `int 0x29` 에서만.

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE1`, kernelbase pdb `72CCE506944DE0267C172AD64287F9BD1` (결과서, 기준과 같음 — 다시 analyze 안 함). 보조로 `apisetschema.dll` 한 군데 (`.apiset` 표) 와 kernel32 `_index.tsv` 한 줄씩.
- 버전 대조 (결과서): `LdrInitializeThunk +0x9 / ret +0xe → LdrpInitialize`, `_LdrpInitialize ret +0x16c → LdrpInitializeProcess`, `LdrpInitializeProcess ret +0x1bbb → LdrpInitializeGraphRecurse` 등 일치. 요청서가 준 rva · 크기 (`KiUserExceptionDispatcher` `0x164e10` 92B, `RtlDispatchException` `0xcb730` 2197B, `RtlpCallVectoredHandlers` `0xcc0c0` 983B, `RtlRaiseException` `0xf700` 685B, `RtlUserThreadStart` `0xacac0`, `RtlUnhandledExceptionFilter2` `0x122300`, kernelbase `UnhandledExceptionFilter` `0x10ed90` 1402B · `RaiseException` `0xc4140` · `RaiseFailFastException` `0x10a020`) 도 맞음. run81 이 본 `KiUserExceptionDispatcher +0x1c` · `+0x1f` · `+0x26` 과 `UNWIND_INFO` (`ALLOC_LARGE 0x590` + `PUSH_MACHFRAME`) 도 결과서가 다시 맞춤.
- 칸 이름:
  - `_CONTEXT` (0x4d0): `ContextFlags +0x30` · `Rax +0x78` · `Rsp +0x98` · `Rip +0xf8` (run16 · run81). `CONTEXT_EX` 는 `+0x4d0` (공개 PDB 에 없음, run81)
  - `EXCEPTION_RECORD` (0x98): `ExceptionCode +0x0` · `ExceptionFlags +0x4` · `ExceptionAddress +0x10` · `NumberParameters +0x18` · `ExceptionInformation +0x20` (run81)
  - `ExceptionFlags` 비트: `0x1` NONCONTINUABLE · `0x2` UNWINDING · `0x4` EXIT_UNWIND · `0x8` STACK_INVALID · `0x10` NESTED_CALL · `0x20` TARGET_UNWIND · `0x40` COLLIDED_UNWIND · `0x80` SOFTWARE_ORIGINATE (결과서 줄 읽기 — `0x66` = 되감는 중 묶음)
  - `_PEB` (ntdll PDB): `BeingDebugged +0x2` · `CrossProcessFlags +0x50` (비트 1 `ProcessInitializing` · 비트 2 `ProcessUsingVEH` · 비트 3 `ProcessUsingVCH`) · `NtGlobalFlag +0xbc`. `_TEB`: `StackBase +0x8` · `StackLimit +0x10` · `LastErrorValue +0x68`
  - 언어 핸들러 반환값 (`EXCEPTION_DISPOSITION`): 0 ContinueExecution · 1 ContinueSearch · 2 NestedException · 3 CollidedUnwind. 필터 반환값: 1 EXECUTE_HANDLER · 0 CONTINUE_SEARCH · −1 CONTINUE_EXECUTION

## 목적

- PE_emulator 요청 26 의 유저 쪽 (Q148b, 원문 4 · 5 의 kernelbase 몫 · 6 의 유저 몫 · 7): `KiUserExceptionDispatcher` 본문, `RtlDispatchException` 의 VEH · SEH 줄기, 끝맺음 (`NtContinue` / `NtRaiseException` / 그 밖), `RtlRaiseException` 이 커널을 거치나, 아무도 안 받을 때 `UnhandledExceptionFilter` 가 부르는 syscall, `RaiseFailFastException` 과 `int 0x29` 의 관계, **nt 엔진이 줄 syscall 답 표**.
- run81 (커널 쪽) 이 넘긴 덧붙임 3건: `CONTEXT_EX` 읽는 조건 · 머신 프레임 읽는 칸, `RtlpContinuationContextMachineFrameEntries` 와 머신 프레임, test.exe 의 `OutputDebugStringA` 예외가 커널을 거치나 (run81 동적 후보 (가)).

## 방법

- 정적: `_workspace/sast/79_exception-user.md` — ntdll · kernelbase
  - 지도 (새로, `ms/disassemble/*/_map/`): ntdll `KiUserExceptionDispatcher` (함수 91개, `{helper 17, expanded 63, syscall 11}`, 끊긴 끝 = `+0x1a` 의 `Wow64PrepareForException` 간접 · VEH 핸들러 · 동적 함수표 콜백 · 힙 CFG 간접), kernelbase `UnhandledExceptionFilter` (199개, import 236개), ntdll `__C_specific_handler` (79개).
  - 뽑음 (모두 `export`, SCRIPT ERROR 없음 — 로그 `ms/ghidra/*sast79*.stdout`): ntdll `KiUserExceptionDispatcher` · `RtlDispatchException` · `RtlRaiseException` depth 1 (26개), `RtlpCallVectoredHandlers` · `RtlUserThreadStart` (+ `$filt$0`) · `RtlUnhandledExceptionFilter` · `RtlUnhandledExceptionFilter2` depth 0, kernelbase `UnhandledExceptionFilter` · `RaiseException` · `RaiseFailFastException` depth 1 (22개). 넓힌 것 (depth 0): `__C_specific_handler` · `RtlUnwindEx` (`__except` 끝맺음) · `RtlCaptureContext2` · `RtlCaptureContext` (이름표) · `RtlRaiseNoncontinuableException` · `RtlpAddVectoredHandler` · `RtlKnownExceptionFilter` · `RtlIsAnyDebuggerPresent` · `RtlReportException` · `RtlGuardCheckExceptionHandler` · `RtlpUnwindPrologue` · `RtlpIsContinuationContextMachineFrameEntry`.
  - `_refs` (ntdll): `LdrpVectorHandlerList` · `RtlAddVectoredExceptionHandler` · `RtlAddVectoredContinueHandler` · `Wow64PrepareForException` · `RtlpContinuationContextMachineFrameEntries` · `RtlpIsContinuationContextMachineFrameEntry`.
  - 읽기만: 있던 kernelbase `OutputDebugStringA.asm` · `OutputDebugStringA.handler.objdump.txt` (sast/66, run71).
  - `.asm` 에 없는 곳 — 결과서가 임시 파이썬 (레포 밖) 으로 직접 읽음: `.pdata` → `UNWIND_INFO` → scope table (`RtlUserThreadStart` · `OutputDebugStringA`), `RtlpExecuteHandlerForException` 의 예외 처리기 `0x1654c0` (함수로 안 잡힘 — objdump), `RtlpCaptureContext2` 뒷부분 · `RtlDecodePointer` · `RtlpVirtualPopShadowStack` · kernelbase delay-load 스텁 바이트, `apisetschema.dll` `.apiset` 표.
- 결과서: `_workspace/sast/79_exception-user_result.md` (**분석성공**. 여섯 질문 + 조정자 덧붙임 3건 모두 줄로 답함, 질문 6 표 · "요청 26 전체 최소 규칙" 포함. 스크립트 문제 없음 — 참고 4건)
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `KiUserExceptionDispatcher +0xb` · `+0x29` · `+0x37` · `+0x4e` · `+0x55`, `RtlDispatchException +0x200` · `+0x25c` · `+0x37a` · `+0x5f0` · `+0x64b`, `RtlRaiseException +0x188` · `+0x19b` · `+0x2a2`, `RtlRestoreContext` (`0x123e20`) `+0x174` · `+0x181`, `RtlCaptureContext2 +0xf3`, kernelbase `RaiseException +0x83`.
- flow-writer 도 `.asm` 으로 다시 봤다: `KiUserExceptionDispatcher +0x0 ~ +0x5b` (전부), `RtlRestoreContext +0x150 ~ +0x18a` (전부) · `+0x300`, `RtlGuardRestoreContext` (`RtlRestoreContext__a59f0.asm`) `+0x178` · `+0x180` · `+0x1b6`, `RtlCaptureContext2 +0x3c` · `+0xc0` · `+0xf3`, `RtlpCaptureContext2 +0x6c`, `RtlDispatchException +0x200` · `+0x20c` · `+0x218` · `+0x25c` · `+0x32b` · `+0x336` · `+0x37a` · `+0x3e3` · `+0x5f0` · `+0x64b`, `RtlpCallVectoredHandlers +0x3b` · `+0x3e` · `+0x45` · `+0x262`, `RtlpAddVectoredHandler +0x1af`, `RtlpExecuteHandlerForException +0x0 ~ +0x14` (전부), `RtlRaiseException +0x64` · `+0x8d` · `+0xdc` · `+0xe6` · `+0x147` · `+0x17e` · `+0x182` · `+0x188` · `+0x19b` · `+0x1e2` · `+0x29f` · `+0x2a2`, `RtlRaiseStatus +0x4a`, `RtlRaiseNoncontinuableException +0x60` · `+0x6b`, `RtlUserThreadStart +0x4a ~ +0x55`, `RtlUserThreadStart$filt$0 +0x19` · `+0x20` · `+0x25` · `+0x3a` · `+0x3f` · `+0x4c` · `+0x51`, `__C_specific_handler +0x91` · `+0x93` · `+0xd1`, `RtlUnwindEx +0x5aa` · `+0x606` · `+0x743` · `+0x892`, kernelbase `RaiseException +0x83` · `+0x8a`, `UnhandledExceptionFilter +0xac` · `+0xb8` · `+0xd5` · `+0xe5` · `+0x110` · `+0x126` · `+0x13e ~ +0x157` · `+0x282` · `+0x29a` · `+0x29f` · `+0x2a1` · `+0x2a7` · `+0x2d1` · `+0x2f8` · `+0x315` · `+0x345` · `+0x39d` · `+0x3e0` · `+0x409` · `+0x4ec` · `+0x568` · `+0x578` · `+0x59d` · `+0x5a6`, `RaiseFailFastException +0xa1` · `+0xc0` · `+0xd9` · `+0x12e` · `+0x13e` · `+0x14c` · `+0x181`. 같은 주소 이름은 `pdb_aliases.py` (`--lookup KernelBase.dll 0x129be0`, `--where ntdll.dll RtlGuardRestoreContext`).
- 값 대조 (flow-writer): run44 덤프 읽기 로그 — PEB `BeingDebugged` `ms/logs/04_baggage-read_P1a.txt:1723` · `_P1b.txt:2421` · `_P2.txt:2433` (모두 0), `CrossProcessFlags` 여덟 장 (`_K1.txt:1568` ~ `_P1.txt:1576` 0 · `_P1a.txt:1743` · `_P1b.txt:2441` **2** · `_P2.txt:2453` 0), KUSD `+0x2d4 KdDebuggerEnabled` `_supp_K1.txt:215` · `_P2.txt:4053` (**3**). `LdrSystemDllInitBlock +0xa8` · `+0xb8` 값은 sast/55 결과서 (run60) 가 옮긴 run44 K1 ~ P2 값. 새 동적 실행은 없다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/<모듈>/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!KiUserExceptionDispatcher` | `+0x1a` | `+0x1c` | (간접) `Wow64PrepareForException` 전역 | 전역이 0 이 아닐 때만 — 64비트는 0 (`+0xb JZ`) | [정적] |
| `ntdll!KiUserExceptionDispatcher` | **`+0x29`** | `+0x2e` | `ntdll!RtlDispatchException` | `(RSP+0x4f0, RSP)` → `AL` | [정적] |
| `ntdll!KiUserExceptionDispatcher` | **`+0x37`** | `+0x3c` | `ntdll!RtlGuardRestoreContext` (`0xa59f0`) | TRUE: `(RSP, NULL)` → `+0x1b6 JMP RtlRestoreContext` | [정적] |
| `ntdll!KiUserExceptionDispatcher` | **`+0x4e`** | `+0x53` | `ntdll!NtRaiseException` | FALSE: `(RSP+0x4f0, RSP, R8B = 0)` | [정적] |
| `ntdll!KiUserExceptionDispatcher` | `+0x55` | `+0x5a` | `ntdll!RtlRaiseStatus` | 위 둘 중 하나가 돌아오면 (실패) | [정적] |
| `ntdll!RtlRestoreContext` (`0x123e20`) | `+0x185` | `+0x18a` | `ntdll!NtContinue` | 이름표 `& 0xffffff3f ≠ 0x10000f` 이거나 CET 비트 (`+0x181 JZ` 안 탐) · `EDX = 0` | [정적] |
| `ntdll!RtlDispatchException` | **`+0x200`** | `+0x205` | `ntdll!RtlpCallVectoredHandlers` | `R8D = 0` (VEH) | [정적] |
| `ntdll!RtlDispatchException` | `+0x218` | `+0x21d` | `ntdll!RtlpCallVectoredHandlers` | `R8D = 1` (VCH), 공통 꼬리 — TRUE · FALSE 둘 다 | [정적] |
| `ntdll!RtlDispatchException` | **`+0x25c`** | `+0x261` | `ntdll!RtlpGetStackLimits` | 못 받으면 `+0x263 JZ +0x758` (플래그 `0x8` + FALSE) | [정적] |
| `ntdll!RtlDispatchException` | `+0x32b` | `+0x330` | `ntdll!RtlInitializeExtendedContext2` | 지역 사본 (`0x10000b`, CET 면 `0x10004b`) | [정적] |
| `ntdll!RtlDispatchException` | `+0x336` | `+0x33b` | `ntdll!RtlpCopyContext` | 사본 이름표 `& 0x10000f` | [정적] |
| `ntdll!RtlDispatchException` | **`+0x37a`** | `+0x37f` | `ntdll!RtlLookupFunctionEntry` | 프레임 고리 (`+0x370`) | [정적] |
| `ntdll!RtlDispatchException` | **`+0x3e3`** | `+0x3e8` | `ntdll!RtlpxVirtualUnwind` | `1 = EHANDLER` · `.pdata` 없으면 안에서 리프 처리 (`+0x3d JZ +0x6ba`) | [정적] |
| `ntdll!RtlDispatchException` | **`+0x5f0`** | `+0x5f5` | `ntdll!RtlpExecuteHandlerForException` | 언어 핸들러가 있는 프레임 (`+0x418 JZ` 안 탐) | [정적] |
| `ntdll!RtlpExecuteHandlerForException` | `+0xd` | `+0xf` | (간접, CFG 없음) 언어 핸들러 `[R9+0x30]` | 예: `__C_specific_handler` (`0x124670`) | [정적] |
| `ntdll!__C_specific_handler` | `+0x91` | `+0x93` | (간접) 필터 | 필터 값이 상수 1 이면 안 부름 (`+0x7b` · `+0x80`) | [정적] |
| `ntdll!__C_specific_handler` | **`+0xd1`** | `+0xd6` | `ntdll!RtlUnwindEx` | 필터 > 0. `ReturnValue = (int64)ExceptionCode` (`+0xbc`) | [정적] |
| `ntdll!RtlUnwindEx` | `+0x892` | `+0x897` | `ntdll!RtlCaptureContext2` | 새 문맥 — 이름표 `0x10000f` (`RtlCaptureContext2 +0xf3`) | [정적] |
| `ntdll!RtlUnwindEx` | `+0x5aa` | `+0x5af` | `ntdll!RtlGuardCheckExceptionHandler` | 코드 `0xC0000027` 이고 비 CET 일 때. 안에서 `+0xb8 = 0` 이라 곧장 끝 | [정적] |
| `ntdll!RtlUnwindEx` | **`+0x606`** | `+0x60b` | `ntdll!RtlRestoreContext` (`0x123e20`) | 목적 프레임 · `Rip` = 착지 · `Rax` = `ReturnValue` | [정적] |
| `ntdll!RtlUnwindEx` | `+0x743` | `+0x748` | `ntdll!NtRaiseException` | 목적 프레임 못 찾고 스택 끝 · FirstChance 0 | [정적] |
| `ntdll!RtlRaiseException` | `+0xe6` | `+0xeb` | `ntdll!RtlpCaptureContext2` | → `RtlCaptureContext2 +0x3c` 로 점프 (`+0x6c`) | [정적] |
| `ntdll!RtlRaiseException` | `+0x147` | `+0x14c` | `ntdll!RtlVirtualUnwind` | 자기 프레임 한 칸 → 문맥 = 부른 쪽 | [정적] |
| `ntdll!RtlRaiseException` | **`+0x188`** | `+0x18d` | `ntdll!RtlDispatchException` | `PEB.BeingDebugged == 0` (`+0x17e` · `+0x182 JNZ`) | [정적] |
| `ntdll!RtlRaiseException` | **`+0x19b`** | `+0x1a0` | `ntdll!RtlRestoreContext` (`0x123e20`) | TRUE → 이름표 `0x10000f` → `IRETQ` | [정적] |
| `ntdll!RtlRaiseException` | **`+0x2a2`** | `+0x2a7` | `ntdll!NtRaiseException` | FALSE (`R8D = 0`) 또는 디버거 (`+0x29f R8B = 1`) | [정적] |
| `ntdll!RtlRaiseStatus` | `+0x4a` | `+0x4f` | `ntdll!RtlRaiseNoncontinuableException` | `(레코드 플래그 0x81, 문맥, 1)` | [정적] |
| `ntdll!RtlRaiseNoncontinuableException` | `+0x60` / `+0x6b` | (꼬리 `JMP`) | `RtlDispatchException` / `NtRaiseException` | `R8B ≠ 0` 이고 디버거 없으면 앞, 아니면 뒤 | [정적] |
| `ntdll!RtlUserThreadStart$filt$0` | `+0x20` | `+0x25` | `ntdll!RtlDecodePointer` | `[RtlpUnhandledExceptionFilter]` (`0x1d28e8`) | [정적] |
| `ntdll!RtlUserThreadStart$filt$0` | `+0x3a` | `+0x3f` | (CFG 간접) 풀린 필터 = kernelbase `UnhandledExceptionFilter` | 널이 아닐 때 | [정적] |
| `ntdll!RtlUserThreadStart$filt$0` | `+0x4c` | `+0x51` | `ntdll!RtlUnhandledExceptionFilter2` | 널일 때 | [정적] |
| `ntdll!RtlUserThreadStart` | `+0x50` | `+0x55` | `ntdll!NtTerminateProcess` | `__except` 착지 `+0x4a` (`EDX = EAX` = 예외 코드, `RCX = -1`) | [정적] |
| `KERNELBASE!RaiseException` | **`+0x83`** | `+0x8a` | (IAT) `ntdll!RtlRaiseException` | 레코드 `[RSP+0x20]` · 주소 = 자기 주소 (`+0x4a`) | [정적] |
| `KERNELBASE!UnhandledExceptionFilter` | `+0xd5` | `+0xda` | `BasepIsDebugPortPresent` | → `NtQueryInformationProcess(-1, 7)` | [정적] |
| `KERNELBASE!UnhandledExceptionFilter` | `+0x110` · `+0x282` | `+0x117` · `+0x289` | (IAT) `NtSetInformationProcess` | 클래스 `0x3f`, 값 8 · 1 | [정적] |
| `KERNELBASE!UnhandledExceptionFilter` | `+0x29a` | `+0x29f` | `GetErrorMode` | → `NtQueryInformationProcess(-1, 0xc)`. `+0x29f TEST AL,2` | [정적] |
| `KERNELBASE!UnhandledExceptionFilter` | `+0x2d1` | `+0x2d8` | (IAT) `NtQueryInformationJobObject` | `(NULL, 2, 버퍼 0x40)` | [정적] |
| `KERNELBASE!UnhandledExceptionFilter` | `+0x4ec` | `+0x4f3` | (IAT) `NtRaiseHardError` | `0xD0000144` · 4개 | [정적] |
| `KERNELBASE!RaiseFailFastException` | `+0x14c` | `+0x153` | (IAT) `ntdll!NtRaiseException` | FirstChance 0 (`+0x13e`) · 문맥 받았을 때 | [정적] |

### 질문 1 — `KiUserExceptionDispatcher` (ntdll rva `0x164e10`, 92B)

- `[정적]` `+0x0 CLD` → `+0x1 MOV RAX,[Wow64PrepareForException]` (rva `0x1e70e0`) · `+0xb JZ +0x1c` — 0 이면 건너뜀. 64비트 프로세스에서는 **0** 이다: `_refs/Wow64PrepareForException.tsv` 에 쓰는 코드가 없다 (읽는 곳 = 이 줄 하나와 WoW64 초기화용 포인터 표 한 칸 `0x173848`).
- `[정적]` `+0x1c MOV RCX,RSP` · `+0x1f ADD RCX,0x4f0` · `+0x26 MOV RDX,RSP` → **입력은 `RSP` 하나**. `+0x29 CALL RtlDispatchException` (ret `+0x2e`) · `+0x2e TEST AL,AL` · `+0x30 JZ +0x3e`.
  - TRUE: `+0x32 MOV RCX,RSP` · `+0x35 XOR EDX,EDX` · **`+0x37 RtlGuardRestoreContext`** → 꼬리로 `RtlRestoreContext` → 이름표 `0x10001f` 라 `NtContinue(문맥, 0)`. 이 함수 안에 `NtContinue` 를 직접 부르는 줄은 없다.
  - FALSE: `+0x3e ~ +0x48` (같은 두 주소) · `+0x4b XOR R8B,R8B` · **`+0x4e NtRaiseException`** (FirstChance 거짓, syscall `0x174`).
  - 둘 중 어느 쪽이든 돌아오면 `+0x53 MOV ECX,EAX` · **`+0x55 RtlRaiseStatus`** (ret `+0x5a`) · `+0x5b RET`.
- `[정적]` `RtlRaiseStatus` (rva `0x12ce0`, 107B) 는 레코드 (코드 = 받은 상태, 플래그 `0x81`, 주소 −1, 개수 0) 를 만들어 `+0x4a RtlRaiseNoncontinuableException(레코드, 문맥, 1)` 을 부른다. 거기서 `R8B ≠ 0` 이고 디버거가 없으면 `+0x60 JMP RtlDispatchException` (유저 SEH 먼저), 아니면 `+0x6b JMP NtRaiseException`. 돌아오면 둘째는 `R8B = 0` → `NtRaiseException(…, FALSE)` → 커널 둘째 기회 (run81 질문 4). 그것도 돌아오면 `RtlRaiseStatus` 를 다시 (되풀이).

### 질문 2 — `RtlDispatchException` (rva `0xcb730`, 2197B)

- `[정적]` 시작: `+0x84 TEST [PEB+0xbc],0x800000` · `+0x8e JZ +0x1f7` — `NtGlobalFlag` 예외 로그가 꺼지면 `+0x94 ~ +0x1ed` (`RtlpExceptionLog2`) 를 통째로 건너뜀.
- **(a) VEH** `[정적]` — `+0x200 RtlpCallVectoredHandlers(레코드, 문맥, 0)` (ret `+0x205`) · `+0x207 JZ +0x24b` (0 이면 SEH 로).
  - 목록이 비었을 때: `RtlpCallVectoredHandlers +0x1d LEA EBP,[R8+2]` · `+0x3b MOV EAX,[PEB+0x50]` · **`+0x3e BT EAX,EBP` · `+0x45 JNC +0x134`** → 목록 0 이면 PEB `+0x50` **비트 2 (`ProcessUsingVEH`)**, 목록 1 이면 **비트 3 (`ProcessUsingVCH`)** 가 꺼져 있으면 **락도 안 잡고** 0 을 돌려준다. 비트가 켜져 있어도 목록이 비면 `+0x6c JZ +0x122`.
  - 비트를 켜는 곳 `RtlpAddVectoredHandler +0x1af BTS.LOCK [PEB+0x50],ECX`, 끄는 곳 `RtlpCallVectoredHandlers +0x262 BTR.LOCK` (마지막 노드가 빠질 때). 전역 목록 `LdrpVectorHandlerList` (rva `0x1ea578`, 두 칸 × `0x18` = `{SRW 락, 목록 머리}`, 칸 0 예외 · 칸 1 계속) 은 파일 값부터 빈 원형 목록. **ntdll 안에 `RtlAddVectoredExceptionHandler` · `…ContinueHandler` 를 부르는 코드가 없다** (`_refs`: 내보내기 표 · `.pdata` 뿐) → test.exe 가 직접 안 부르면 비트 2 · 3 은 0.
  - 노드가 있을 때: `+0x61` SRW 락 → 노드마다 참조 올림 → 락 풀고 → `+0xa6 RtlDecodePointer([노드+0x20])` → `+0xd1` CFG 간접 호출 (`EXCEPTION_POINTERS*`) → **`+0x112 CMP [RSP+0xa8],-1` · `+0x11a JNZ +0x69` · `+0x120 MOV BL,1`**: `-1` (CONTINUE_EXECUTION) 이면 멈추고 TRUE, 그 밖 (0 도 1 도) 은 다음 노드.
- **(b) SEH** `[정적]`:
  - 준비: `+0x256 AND ESI,0x81` · **`+0x25c RtlpGetStackLimits`** (ret `+0x261`) — `[RBP+0x48]` = TEB `+0x10` (낮은 쪽), `[RBP+0x50]` = TEB `+0x8` (높은 쪽). `RtlpGetStackLimits` (rva `0xcc530`, 43B) 는 **지금 `RSP` 가 그 사이인지도** 본다 (`+0x1b` · `+0x1e` · `+0x20` · `+0x23`). 실패면 `+0x263 JZ +0x758` (플래그 `0x8` + FALSE).
  - `+0x26c RtlpSanitizeContext` · `+0x2a9 RtlpValidateContextFlags2` · `+0x30e __chkstk` · **`+0x32b RtlInitializeExtendedContext2` · `+0x336 RtlpCopyContext`** — 지역 사본. 이름표는 `(LdrSystemDllInitBlock+0xa8 >> 0x3c) & 3 == 1` (CET) 일 때만 `0x10004b`, 아니면 `0x10000b` (test.exe 는 뒤), 사본은 `RtlpCopyContext` 가 `& 0x10000f` 로 맞춤.
  - 고리 (`+0x370`): **`+0x37a RtlLookupFunctionEntry(Rip, &ImageBase, &히스토리)`** → `+0x385 RtlpSanitizeContext` → **`+0x3e3 RtlpxVirtualUnwind(1, …)`** (PDB 이름. `RtlVirtualUnwind` rva `0x16bc0` 는 얇은 겉포장 — 이 고리는 안쪽을 직접 부름). `.pdata` 가 없으면 `RtlpxVirtualUnwind +0x3a TEST R9,R9` · `+0x3d JZ +0x6ba` 리프 처리 (`[정적: 분기만]`).
  - 검사: `+0x3ea JS +0x64b` (풀기 실패 → FALSE) · `+0x3f0 ~ +0x40b` (`EstablisherFrame` 8 정렬 · 범위 → 아니면 `+0x890` 플래그 `0x8` → FALSE) · `+0x418 JZ +0x622` (핸들러 없으면 다음 프레임).
  - 핸들러: `+0x424 ~ +0x466` `DISPATCHER_CONTEXT` 를 `[RBP+0x60]` 에 채움 → **`+0x5f0 RtlpExecuteHandlerForException(레코드, EstablisherFrame, 원래 문맥, &DC)`** (ret `+0x5f5`). `RtlpExecuteHandlerForException` (rva `0x1654f0`, 21B): `+0x9 MOV RAX,[R9+0x30]` (`LanguageHandler`) · **`+0xd CALL RAX`** — CFG 검사 없음. 자기 `UNWIND_INFO` 에 예외 처리기 `0x1654c0` (함수로 안 잡힘, 결과서 objdump): 되감는 중 (`& 0x66`) 이면 1, 아니면 안쪽 `EstablisherFrame` 을 `DC+0x18` 에 옮기고 2.
  - 돌아온 값: **1** ContinueSearch → `+0x622` 사본 `Rsp` (`+0x98`) · `Rip` (`+0xf8`) 범위 검사 (`+0x630` · `+0x634` · `+0x63a`) → `+0x646 JMP +0x370` · **0** ContinueExecution → `+0x743 TEST SIL,1` (NONCONTINUABLE 이면 `+0x74d RtlRaiseStatus(0xC0000025)`) 아니면 `+0x209` TRUE · **2** Nested → `+0x763` 플래그 `0x10` · **3** Collided (`+0x670 ~ +0x720`, `+0x6bd RtlVirtualUnwind`, CET 일 때만 `+0x6dd` 검사 → 어긋나면 `+0x7f2 INT 0x29`) · 그 밖 → `+0x7f4 RtlRaiseStatus(0xC0000026)`.
  - 끝 (FALSE): **`+0x64b XOR DIL,DIL`** · `+0x64e` 플래그 저장 · `+0x651 JMP +0x20c`.
- **(c) 핸들러 검사** `[정적]` — `RtlDispatchException` 에는 SafeSEH 류 검사가 **없다** (x64 엔 SafeSEH 가 없고, 언어 핸들러는 CFG 없는 `CALL RAX`). 검사로 볼 것 셋: 스택 · 정렬 (→ 플래그 `0x8` + FALSE), 반환값 (`0xC0000025` · `0xC0000026`), 되감기 때 `RtlGuardCheckExceptionHandler` (rva `0xa5d00` — 이름 뜻은 `[추측]` EH 이어하기 표 검사, `RtlUnwindEx +0x5aa` · `RtlGuardRestoreContext +0x1e6` 이 부름) — **첫 줄 `+0x18 CMP [LdrSystemDllInitBlock+0xb8],0` · `+0x2a JZ +0x195` 로 곧장 끝** (test.exe `+0xb8 = 0`). `+0xb8 ≠ 0` 이면 표에 없을 때 `RtlFailFast2(0x26)` = `INT 0x29`.
- **(d) syscall** `[정적]` — **`RtlDispatchException` 본체에는 `syscall` 도 `Nt…` 호출도 없다.** 평소 길 (VEH 없음, 함수표 빠른 길) 은 0. 안쪽에서 syscall 로 이어질 수 있는 것 (지도의 `syscall` 11개 중 이 줄기, 평소엔 안 탐):

| 어디서 | syscall | 언제 |
|---|---|---|
| `RtlLookupFunctionEntry` → `RtlpxLookupFunctionTable +0x2d0 / +0x2d5` | `NtQueryVirtualMemory` (`0x23`, 클래스 6) | `LdrInitState ≠ 3` (로더 초기화 중) 일 때만 (run60 · sast/55 1(e)) |
| 같은 함수 `+0x406 / +0x40b` · `+0x508 / +0x50d` | `NtAlertThreadByThreadIdEx` (`0x72`) · `NtSetInformationThread` (`0xd`, 클래스 `0x38`) · (`RtlpAcquireSRWLockSharedContended`) `NtWaitForAlertByThreadId` (`0x1e3`) | 함수표 락 (`LdrpInvertedFunctionTableSRWLock`) 경합 |
| `RtlLookupFunctionEntry +0x352` → `RtlpLookupDynamicFunctionEntry` | (없음, SRW 락 + CFG 간접) | 동적 함수표 (`RtlAddFunctionTable`) 에 있을 때 — 콜백이 유저 코드 |
| `RtlpCallVectoredHandlers +0xa6` → `RtlDecodePointer` | `NtQueryInformationProcess` (`0x19`, 클래스 `0x24`) | 쿠키 캐시 (`0x1d2408`) 가 0 일 때 한 번 (`+0x1b ~ +0x4f`). 초기화 중에 이미 채워짐 (run59 `LdrpInitializeProcess +0x134`) |
| `RtlpCallVectoredHandlers` 노드 빼기 | `NtProtectVirtualMemory` (`0x50`) | `RtlRemoveVectoredExceptionHandler` — `.mrdata` 보호 (`LdrProtectMrdata`, run60) |
| `RtlRaiseStatus` (이상한 값 · NONCONTINUABLE) | `NtRaiseException` | 오류 갈래 ((c)) |

- **(e) VCH** `[정적]` — 공통 꼬리 **`+0x20c MOV R8D,1` ~ `+0x218 CALL RtlpCallVectoredHandlers`** (ret `+0x21d`) → `+0x21d MOVZX EAX,DIL`. 여기로 오는 길이 셋: VEH 가 처리 (`+0x209`), SEH 가 ContinueExecution (`+0x747 JZ +0x209`), **처리 못 함 (FALSE) 도** (`+0x651` · `+0x75e`). 즉 **TRUE · FALSE 둘 다 끝에서 돈다**. 비트 3 이 꺼지면 곧장 돌아옴. 반환값은 버림.

### 질문 3 — `RtlRaiseException` (rva `0xf700`, 685B) — 원문 7

- `[정적]` **커널을 안 거치고 `RtlDispatchException` 을 부른다 (디버거 없을 때).** 차례: `+0x5f BTS [레코드+4],7` (`ExceptionFlags` 에 `0x80` SOFTWARE_ORIGINATE) · `+0x64 MOV R14D,0x10000b` (`+0x8d` KUSD `+0x3d8` 이 0 이 아니면 `+0x1e2 MOV R14D,0x10004b`) · `+0xa0 RtlGetExtendedContextLength3` · `+0xc2 __chkstk` · `+0xdc RtlInitializeExtendedContext2` → **`+0xe6 RtlpCaptureContext2`** (ret `+0xeb`, 이름표 `0x10000f`) → `+0x111 RtlLookupFunctionEntry` (ret `+0x116`) + **`+0x147 RtlVirtualUnwind`** (ret `+0x14c`) 로 자기 프레임 한 칸 → `+0x158 ~ +0x15f` 레코드 주소 = 문맥 `Rip` → `+0x16a RtlpGuardSynchronizeRestorePc` (한 줄 `RET` 자리) → **`+0x16f MOV RAX,GS:[0x60]` · `+0x17e CMP byte [RAX+2],R15B` · `+0x182 JNZ +0x29f`** → **`+0x188 RtlDispatchException`** (ret `+0x18d`).
  - `+0x14c CMP [레코드],0x80000033` (UNWIND_CONSOLIDATE) 면 `+0x152 JZ +0x242` 로 한 칸 더. `+0x119 JZ +0x1d2`: 자기 함수 항목을 못 찾으면 곧장 `RtlRaiseStatus`.
- 갈래 `[정적]`:
  - **TRUE** → **`+0x195 ~ +0x19b RtlRestoreContext(문맥, 레코드)`** (ret `+0x1a0`, rva `0x123e20`). 이름표 `0x10000f` 라 **`NtContinue` 가 아니라 유저 `IRETQ`** (syscall 0). ret 자리로 돌아오면 오류.
  - **FALSE** → `+0x294 XOR R8D,R8D` · `+0x29d JMP` → **`+0x2a2 NtRaiseException(…, FirstChance 0)`** (ret `+0x2a7`). 돌아오면 `+0x2a7 MOV EBX,EAX` → `+0x1d4 RtlRaiseStatus(EBX)` (ret `+0x1d9`).
  - **디버거가 붙었으면** (`BeingDebugged ≠ 0`): `RtlDispatchException` 을 건너뛰고 `+0x29f MOV R8B,1` → **`NtRaiseException(…, TRUE)`**. 커널이 디버거에게 먼저 묻고 (run81 질문 3 · 4), 안 받으면 `KiUserExceptionDispatcher` 로 보내 그때 유저 디스패치가 돈다.
  - 그래서 "몇 번째 기회로 들어가나": **디버거 없음 → 유저가 먼저 (커널 안 감), 못 받아 커널로 갈 때만 `FirstChance = 거짓`. `FirstChance = 참` 은 디버거가 붙었을 때만.**
- `[정적]` **kernelbase `RaiseException` (rva `0xc4140`, 177B) 이 여기로 온다**: `+0x3a ~ +0x59` 스택에 레코드 (`[RSP+0x20]` 코드 · `[RSP+0x24]` 플래그 `& 1` · `[RSP+0x28]` 0 · **`[RSP+0x30]` = 자기 주소** (`+0x4a LEA`), 개수 `min(n, 15)` (`+0x5b ~ +0x70`), `+0x79 memcpy`) → **`+0x83 CALL [IAT] RtlRaiseException`** (ret `+0x8a`). 최종 `ExceptionAddress` = **`RaiseException +0x8a`** (`RtlRaiseException +0x15f` 가 부른 쪽 반환 주소로 덮음). 처리되면 `RaiseException +0x8a` 로 돌아가 `+0x9a` 쿠키 검사 뒤 `RET` — 비휘발 레지스터는 `RtlVirtualUnwind` 가 `RaiseException` 의 값으로 되돌려 둠.

### 질문 4 — 아무도 안 받을 때 (유저 쪽)

- `[정적]` 스레드 시작 `RtlUserThreadStart` (rva `0xacac0`) 의 scope table (결과서 `.pdata`): **`__try [+0x7, +0x4a)`, 필터 `RtlUserThreadStart$filt$0` (rva `0x169b61`), 착지 `+0x4a`**, 언어 핸들러 `__C_specific_handler` (rva `0x124670`). `__try` 안에서 부르는 것 = `+0x27` / `+0x3d` 시작 함수 (`BaseThreadInitThunk` 또는 사용자 시작 주소) 와 `+0x44 RtlExitUserThread` (run29).
- `[정적]` `RtlDispatchException` 이 프레임을 걷다 이 프레임에서 `__C_specific_handler` 를 부른다 (`+0x5f0`). `__C_specific_handler`: `+0x2b __except_validate_context_record` (ret `+0x30`) → `+0x3e TEST [레코드+4],0x66` (되감는 중 아님) → 범위 표 고리 `+0x5d ~ +0xdf` → **필터 `+0x91 CALL RAX`** (ret `+0x93`, `RCX = &{레코드, 문맥}`, `RDX = EstablisherFrame`) → `+0x95 JS +0xea` (`< 0` → ContinueExecution 0 을 돌려줌) · `+0x97 JLE +0xdb` (0 → 다음 항목) · 양수 → `+0xa9 _NLG_Notify` → **`+0xd1 RtlUnwindEx(EstablisherFrame, 착지, 레코드, ReturnValue = (int64)코드, 문맥, 히스토리)`** (ret `+0xd6`).
- `[정적]` 필터 `$filt$0`: `+0x19 MOV RCX,[RtlpUnhandledExceptionFilter]` (rva `0x1d28e8`) · **`+0x20 RtlDecodePointer`** (ret `+0x25`) · 널이 아니면 **`+0x3a` CFG 간접 호출** (ret `+0x3f`, `RCX = EXCEPTION_POINTERS`), 널이면 **`+0x4c RtlUnhandledExceptionFilter2(포인터, 빈 문자열)`** (ret `+0x51`).
- `[정적]` **이 전역은 kernelbase 가 채운다**: `_KernelBaseBaseDllInitialize +0x4ab` (ret `+0x4b2`) `RtlSetUnhandledExceptionFilter(UnhandledExceptionFilter)` (run68). 사용자가 `SetUnhandledExceptionFilter` 로 바꾸는 건 ntdll 전역이 아니라 kernelbase `BasepCurrentTopLevelFilter`. 그래서 kernelbase 가 올라온 프로세스에서 `$filt$0` 은 **kernelbase `UnhandledExceptionFilter`** (rva `0x10ed90`) 를 부른다. (`RtlUnhandledExceptionFilter2` 는 전역이 널일 때만 — 디버거가 없으면 `RtlIsAnyDebuggerPresent` (`PEB+2` 또는 `KUSD+0x2d4 & 3 == 3`) 가 거짓이라 `+0x391` 로 가서 코드 `0xC0000409` 일 때만 `RtlReportException`, 반환 = `코드 ≠ 0xC0000194 ? 0 : -1` = 보통 0.)
- `[정적]` **kernelbase `UnhandledExceptionFilter` 의 차례** (디버거 없음, 보통 코드, 비 secure, 사용자 필터 없음. `call / ret`):

| # | 자리 | 하는 일 → syscall | 결과로 갈리는 곳 |
|---|---|---|---|
| 0 | `+0x5a` · `+0x5e` | 플래그 `0x10` (NESTED_CALL) 검사 | 켜져 있으면 **0** 로 끝 |
| 1 | `+0x6c` | 코드가 `0xC0000409` 일 때만 `+0x74/+0x79 BasepIsKernelDebuggerPresent` (`NtQuerySystemInformation` 클래스 `0x23`) · `+0x7d/+0x82 BasepIsDebugPortPresent` | 참이면 `+0x8d DbgPrint` + `INT3` |
| 2 | `+0xac/+0xb1` | apiset `ext-ms-win-kernel32-errorhandling-l1-1-0` 이 ApiSetMap 에 있나 묻기 (같은 주소 이름 4개, 후보: `IsWerpGetDebuggerPresent` · `IsWerpLaunchAeDebugPresent` · `IsCheckForReadOnlyResourceFilterPresent` · `IsBasepReportFaultPresent`) → `ApiSetQueryApiSetPresence`, 전역에 캐시 | 있으면 `AL = 1` |
| 3 | `+0xb8/+0xbf` | **delay-load** `CheckForReadOnlyResourceFilter` (스텁 rva `0x129cad` → `0x129c2e`, 첫 호출 때 풀림. kernelbase 가 부르면 호스트 `kernel32.dll` — `apisetschema.dll` 표) | `-1` 이면 **-1** 로 끝 |
| 4 | `+0xd5/+0xda` | `BasepIsDebugPortPresent` → **`NtQueryInformationProcess(-1, 7)`** (8B) | ≠ 0 이면 **0** 로 끝 |
| 5 | `+0xe5/+0xec` | `RtlKnownExceptionFilter` (ntdll `0x10cc20`) — `코드 == 0xC0000194 ? -1 : 0` | `-1` 이면 6 건너뜀 |
| 6 | `+0x110/+0x117` | **`NtSetInformationProcess(-1, 0x3f, 값 8)`** (클래스 이름 `[추측]` ProcessFaultInformation) | 반환값 안 봄 |
| 7 | `+0x126/+0x12d` · `+0x145/+0x14c` | `RtlAcquireSRWLockExclusive(BasepUEFLock)` · `RtlDecodePointer(BasepCurrentTopLevelFilter)` | 널이면 `+0x157 JZ +0x23c`. 널이 아니면 (사용자 필터) `+0x16e BasepFillUEFInfo` → `+0x1ee` CFG 간접 호출 (ret `+0x1f3`) |
| 8 | `+0x24e/+0x255` | `RtlKnownExceptionFilter` (2번째) | `-1` 이면 **-1** 로 끝 |
| 9 | `+0x282/+0x289` | **`NtSetInformationProcess(-1, 0x3f, 값 1)`** | 반환값 안 봄 |
| 10 | `+0x28e` · `+0x294` | `BasepIsSecureProcess` | secure 면 **0** 로 끝 |
| 11 | `+0x29a/+0x29f` | `GetErrorMode` → **`NtQueryInformationProcess(-1, 0xc)`** (4B) | **`+0x29f TEST AL,2` · `+0x2a1 JNZ +0x5a6`** → 비트 `0x2` (SEM_NOGPFAULTERRORBOX) 면 **1** 로 끝 (WER 안 감) |
| 12 | `+0x2a7/+0x2ae` | `RtlGetThreadErrorMode` (TEB) | `& 0x20` 이면 **1** 로 끝 |
| 13 | `+0x2d1/+0x2d8` | **`NtQueryInformationJobObject(NULL, 2, 버퍼 0x40, NULL)`** | 실패면 무시. 성공이고 `LimitFlags & 0x400` 이면 **1** 로 끝 |
| 14 | `+0x2f8/+0x2fd` | 2 와 같은 묻기 (캐시) | 없으면 `+0x2ff JZ +0x3a2` (WER 건너뜀 → 18) |
| 15 | `+0x315/+0x31a` | `LoadLibraryExW(L"ext-ms-win-kernel32-errorhandling-l1-1-0.dll", NULL, 0)` — 로더 일 (이미 올라온 `kernel32.dll` 로 풀리면 로드 없음 `[추측]`) | 실패여도 `LastError` 0 · `0x7e` 면 계속 |
| 16 | `+0x345/+0x34c` | **delay-load `BasepReportFault(ExceptionPointers, 1)`** — WER 본체 (이름만). apiset 못 풀면 스텁 (rva `0x129830`) 이 `LastError = 0x7f`, **4** 를 돌려줌. 호스트 `kernel32!BasepReportFault` (rva `0x622d0`, 42B) 안은 안 읽음 | 반환 `ESI` 로 17 |
| 17 | `+0x380 ~ +0x38d` · `+0x39d/+0x3a2` | `ESI ∈ {3, 9, 10}` 이거나 WER 를 못 불렀으면 → `RaiseFailFastException(레코드, 문맥, 0)` (질문 5). `ESI == 8` → **0** 로 끝 | |
| 18 | `+0x3e0/+0x3e5` | `ESI == 4` → `VirtualAllocExNuma(-1, NULL, 0x250, …)` (`NtAllocateVirtualMemoryEx` `[추측]`) | |
| 19 | `+0x409/+0x40e` | `WerpGetDebugger(버퍼)` (AeDebug 레지스트리 `[추측]`) — apiset 있을 때만 | 설정 없으면 비트 꺼짐 |
| 20 | `+0x4ec/+0x4f3` | **`NtRaiseHardError(0xD0000144, 4, 0, &{코드, 주소, 정보0, 정보1}, 옵션 1 또는 2, &응답)`** | 오류 심각도 상태면 응답 6 으로 침. 응답 3 이면 AeDebug (`+0x550 WerpLaunchAeDebug`) |
| 21 | `+0x568` · `+0x578/+0x57f` | `BasepAlreadyHadHardError` 가 켜져 있으면 `NtTerminateProcess(-1, 코드)` | |
| 22 | `+0x598/+0x59d` | `VirtualFreeEx` (`NtFreeVirtualMemory` `[추측]`) | |
| 끝 | **`+0x59d MOV EAX,[RSP+0x3c]`** · `+0x5a1 JMP +0x20d` | `[RSP+0x3c]` 는 `+0x64` 에서 1 로 시작 (AeDebug 를 띄운 경우만 `+0x562` 에서 0) | **기본 반환 = 1 (EXECUTE_HANDLER)** |

  - 디버거 없을 때 돌려주는 값: **기본 1**. 예외로 0 (디버거 포트 · secure · 중첩 · AeDebug 띄움 · `ESI == 8`) 과 −1 (읽기 전용 자원 필터 · 코드 `0xC0000194`).
  - 흔한 단축 `[정적]`: 4 뒤 **11 의 `ProcessDefaultHardErrorMode & 2` 가 켜지면 syscall 4개 (클래스 7 · `0x3f` · `0x3f` · `0xc`) 로 끝나고 WER · 로드 없음.** 꺼지면 14 부터 WER 길 (이름만) 또는 apiset 이 없을 때 고전 길 (18 ~ 22).
  - 11 의 값 출처: `EPROCESS.DefaultHardErrorProcessing` (짐 지도 `+0x388`, 부모 값 복사 / 부모 없으면 5 — 결과서). 비트 `0x2` 는 보통 꺼져 있다고 보는 게 안전 (부모 cmd 값에 달림 `[동적]`).
- `[정적]` **UEF 가 1 을 돌려주면**: `__C_specific_handler +0xd1 RtlUnwindEx` → 착지 `RtlUserThreadStart +0x4a` 에서 **`+0x4a MOV EDX,EAX` · `+0x4c OR RCX,-1` · `+0x50 CALL NtTerminateProcess`** (ret `+0x55`) = **`NtTerminateProcess(-1, 예외 코드)`**. `EAX` = 예외 코드인 이유: `__C_specific_handler +0xbc MOVSXD R9,[R12]` 가 `ReturnValue = (int64)ExceptionCode` 를 주고, `RtlUnwindEx` 가 목적 프레임 문맥 `Rax` 에 넣음 (`+0x3f2 ~ +0x3f9`). 끝 상태 = 예외 코드 그대로.
- `[정적]` **`RtlUnwindEx` (rva `0x13380`) 끝맺음**: `+0xa7 RtlpGetStackLimits` (ret `+0xac`, 실패면 `+0xb4 ~ +0xb9 RtlRaiseStatus(0xC0000028)`) → `+0x886 RtlInitializeExtendedContext2` · **`+0x892 RtlCaptureContext2`** (ret `+0x897`, 이름표 `0x10000f`) → 프레임마다 `RtlLookupFunctionEntry` (`+0xf5`) · `RtlpCopyContext` (`+0x107`) · `RtlpxVirtualUnwind` (`+0x21e`) · 중간 프레임 `__finally` 는 `RtlpExecuteHandlerForUnwind` (`+0x2fb` / ret `+0x300`) → 목적 프레임 (`+0x3e5 CMP RCX,R12`): `Rax = ReturnValue` (`+0x3f9`) · `Rip = 착지` (`+0x40c`) · 코드를 `0xC0000027` 로 덮음 (`+0x41b`) → `+0x5aa RtlGuardCheckExceptionHandler` (test.exe 는 아무것도 안 함) → **`+0x606 RtlRestoreContext(문맥, 레코드)`** (ret `+0x60b`) → **유저 `IRETQ`, syscall 없음.** 목적 프레임을 못 찾고 스택 끝이면 `+0x72c ~ +0x743 NtRaiseException(…, FALSE)` (ret `+0x748`).
- 정리 `[정적]`: **`RtlUserThreadStart` 의 `__except` 가 있으니 평소 "아무도 안 받음" 은 커널 둘째 기회까지 안 간다.** 흐름 = `UnhandledExceptionFilter` (syscall 위 표) → 1 → `RtlUnwindEx` → `IRETQ` → `NtTerminateProcess(-1, 코드)`. 커널 둘째 기회 (`NtRaiseException(FALSE)` → run81 질문 4 → `ZwTerminateProcess`) 는 필터가 0 을 돌려줄 때나 걷기가 FALSE 일 때만.
- `[정적]` 필터가 −1 이면 (`__C_specific_handler +0xea` → 0 ContinueExecution) → `RtlDispatchException` TRUE → `KiUserExceptionDispatcher` → `NtContinue` (문맥은 커널이 쌓은 원래 것).

### 질문 5 — `__fastfail` 유저 쪽: kernelbase `RaiseFailFastException` (rva `0x10a020`, 493B)

- `[정적]` **`int 0x29` 와 같은 길이 아니다.** `int 0x29` 는 커널 `KiRaiseSecurityCheckFailure` → `KiFastFailDispatch` 에서 `0xC0000409` 로 유저 핸들러 없이 둘째 기회 (run81 질문 5). ntdll 의 `RtlFailFast2` · `RtlpCallVectoredHandlers +0x15e` · `RtlDispatchException +0x7f2` 등의 `INT 0x29` 가 이쪽.
- `[정적]` `RaiseFailFastException(레코드, 문맥, 플래그)` 는 **일반 syscall `NtRaiseException`** 으로 간다:
  - `+0x55 TEST RBX,RBX` · `+0x58 JNZ +0x18f`: 레코드를 받았으면 `+0x18f` NONCONTINUABLE 켬. 없으면 `+0x6f` 코드 `0xC0000602` · `+0x77` 플래그 1, 주소 = 반환 주소.
  - 문맥이 없으면 `+0xa1 RtlCaptureContext` (ret `+0xa8`) · `R14D = 1`, 받았으면 `R14D = 0` (`+0x125`).
  - `+0xb3 CMP [BasepIsSecureProcess],0` · `+0xba JNZ +0x1ad`: secure 면 WER 건너뜀 (`+0x1bf NtRaiseException(FALSE)`).
  - 아니면 **`+0xc0 SignalStartWerSvc`** (ret `+0xc5`, 안쪽 WNF · ETW 이름만) → `+0xd9 NtQuerySystemInformation(0x73, 8B)` (ret `+0xe0`) → `+0x12e WaitForWerSvc` (ret `+0x133`, 안쪽 `NtOpenEvent` · `NtWaitForSingleObject` · `NtClose`). 하나라도 실패 (< 0) · 시간 초과 (`0x102`) 면 `+0xe9`: 플래그 `& 2` 면 `TerminateProcess(-1, 코드)`, 아니면 `+0x113 NtRaiseHardError(코드 | 0x10000000, 0개, …, 옵션 1)` 뒤 `TerminateProcess`.
  - 성공하면 **`+0x13e XOR R8D,R8D` (FirstChance 거짓)** → 문맥 받음 (`R14D == 0`) 이면 **`+0x14c NtRaiseException(레코드, 문맥, FALSE)`** (ret `+0x153`), 여기서 잡았으면 `+0x181 RtlRaiseNoncontinuableException(레코드, 문맥, 0)` → 안에서 `+0x6b JMP NtRaiseException` (FALSE).
  - 즉 **유저 SEH · VEH 를 안 거치고 커널 둘째 기회 → `ZwTerminateProcess(-1, 0xC0000602)`** (run81 질문 4).
- 부르는 쪽: kernelbase `UnhandledExceptionFilter +0x39d` (WER 답 3 · 9 · 10, 또는 WER 를 못 불렀을 때 — 질문 4 표 17).

### 덧붙임 1 — `CONTEXT_EX` · 머신 프레임 칸 (run81 덧붙임 ① 닫힘)

- `[정적]` **ntdll 이 `CONTEXT_EX` (`ctx+0x4d0`) 를 읽는 곳은 모두 이름표에 `0x100040` (AMD64 + XSTATE) 이 다 있어야 읽는다**:
  - `RtlpSanitizeContext` (rva `0xcbfd0`): `+0x39 AND EAX,0x100040` · `+0x3e CMP` · `+0x43 JNZ +0xa0` (끝). 읽는 칸 `+0x4d0` · `+0x4d4` · `+0x4d8` · `+0x4dc` · `+0x4e0` · `+0x4e4`.
  - `RtlpCopyContext` (rva `0xccbe0`): `+0x1f AND EDX,0x100040` · `+0x25 CMP` · `+0x2b JZ +0x1ed` (`+0x1ff LEA RBX,[R9+0x4d0]`) — 주는 쪽 · 받는 쪽 둘 다 `0x40` 일 때만.
  - `RtlpVirtualPopShadowStack` (rva `0xd4760`, 결과서 바이트): `& 0x100040 == 0x100040` 이고 `(KUSD+0x3d8 | KUSD+0x708) & 0x800` (CET_U) 일 때만.
  - `RtlpUnwindPrologue +0x3b6`: 풀고 있는 **지역 사본** 의 `CONTEXT_EX`.
  - `RtlRestoreContext +0x1a3 ~ +0x1e3`: 이름표에 `0x40` 이 있을 때만 `XRSTOR`.
- `[정적]` **커널이 `0x10001f` 로 쌓으면 위 문턱이 다 거짓이라 `+0x4d0` 을 안 읽는다.** KUSD `XState` 가 `0xe7` 이어도 상관없다 — 디스패처의 지역 사본 이름표는 `RtlDispatchException` 이 CET 일 때만 `0x10004b` 로 정하고, `RtlRaiseException` 이 KUSD 를 보는 건 자기 문맥 크기를 정할 때뿐 (`+0x8d`). `0x10001f` 자체는 `RtlpValidateContextFlags2` 가 허용 (`+0xa → +0x15`, `flags & 0x27ffff80 == 0x10000`). (`0xe7` 에 비트 11 `0x800` 도 없음. KUSD `+0x708` 값은 모름.)
- `[정적]` **머신 프레임 (`C+0x590`) 의 `CS` · `EFlags` · `SS` 는 안 읽는다.** `RtlpUnwindPrologue` 의 `UWOP_PUSH_MACHFRAME` 처리 (`+0x404 ~ +0x474`): `ctx.Rip = [프레임 +0x0]` (`+0x45f`), `ctx.Rsp = [프레임 +0x18]` (`+0x469`) 둘만 (오류 코드 있는 꼴이면 `+0x8` · `+0x20`). 대신 두 칸 주소가 `[StackLimit, StackBase − 8]` 안인지 본다 (`+0x440 ~ +0x561`), 벗어나면 `0xC0000028`. → run81 (나) 표의 "`+0x598` · `+0x5a0` · `+0x5b0` 안 씀" 과 맞다.

### 덧붙임 2 — `RtlpContinuationContextMachineFrameEntries` 와 머신 프레임

- `[정적]` 표 (rva `0x1cc208`, 8B × 2) 는 `InitSpecialMachineFrames +0x46` 이 쓰고 (run59: `[0]` = `KiUserExceptionDispatcher` 의 `UNWIND_INFO` 주소, `[1]` = `KiUserApcDispatcher`), 읽는 곳은 **`RtlpIsContinuationContextMachineFrameEntry` (rva `0xee350`, 36B) 하나**, 그걸 부르는 곳도 **`RtlpUnwindPrologue +0x477` (ret `+0x47c`) 하나** (`_refs`).
- `[정적]` 되감기에 쓰나: **예, 그러나 CET 그림자 스택용.** `PUSH_MACHFRAME` 을 만나면 `Rip` · `Rsp` 를 꺼낸 **뒤** `+0x474 MOV RCX,RDI` (지금 풀고 있는 함수의 `UNWIND_INFO`) → `+0x477` → 참이면 `+0x487 RtlpVirtualPopShadowStack(문맥)`, 거짓이면 `+0x47e JZ` 로 건너뜀. `RtlpVirtualPopShadowStack` 은 이름표에 `0x40` 이 없거나 CET_U 가 꺼지면 첫 줄들에서 `RET`. → **test.exe (CET 꺼짐, `+0xa8 = 0`) 에선 결과에 영향 없음.** 엔진은 이 표를 신경 쓸 필요 없다 (진짜 ntdll 이 `LdrpInitialize +0x22 → InitSpecialMachineFrames` 로 스스로 채움).
- 정리: 머신 프레임 `+0x590` 은 **걷기를 가능하게 하는 값** (`Rip` · `Rsp`), `Entries[0]` 은 "이 프레임은 연속 문맥 프레임" 표시로 **그림자 스택 처리에만** 쓰인다.

### 덧붙임 3 — test.exe 의 실제 예외 `OutputDebugStringA +0x77 RaiseException(0x40010006)` (run71 · run81 동적 후보 (가) 닫힘)

- `[정적]` `OutputDebugStringA` (kernelbase rva `0xc3df0`) scope table (결과서 `.pdata`): **`__try [+0x42, +0x81)`**, 필터 `OutputDebugStringA$filt$0` (rva `0x1995f4`: `+0x1a CMP ECX,0x40010006` · `+0x20 SETZ AL` → **코드가 `0x40010006` 이면 1, 아니면 0**), 착지 `+0x81` (DBWin 몸통, run71 · sast/66). 그 뒤 `__try [+0x1a4, +0x29b)` 는 필터 `$filt$1` (`≠ 0xC00000FD` — 스택 넘침만 빼고 다 받음) + 정리 `$fin$2`.
- `[정적]` `+0x6e MOV ECX,0x40010006` · `+0x73 LEA R8D,[RDX+2]` (개수 2) · **`+0x77 CALL RaiseException`** (ret `+0x7c`) → kernelbase `RaiseException +0x83/+0x8a` → `RtlRaiseException` (질문 3). 플래그 0 (이어 할 수 있음).
- `[정적]` + `[관찰]` 값 — **커널을 안 거친다**: `RtlRaiseException +0x17e` 는 `PEB.BeingDebugged` 만 본다. run44 (커널 디버거 KDNET 만 붙은 실행) 의 PEB `BeingDebugged` 는 **0** 이다 (`ms/logs/04_baggage-read_P1a.txt:1723` · `_P1b.txt:2421` · `_P2.txt:2433`, 첫 `OutputDebugStringA` 가 있는 P1b ~ P2 구간 포함) `[관찰]` → 그 실행에서 `+0x188 RtlDispatchException` 을 직접 불렀을 것 `[정적 추론]`.
- `[정적]` + `[관찰]` 값 — **누가 받나**: VEH 비트는 꺼져 있다 (PEB `CrossProcessFlags` 여덟 장 모두 0 또는 2 — 비트 2 · 3 꺼짐 `[관찰]`, 위 "방법" 줄). 그래서 SEH 걷기의 첫 프레임 `OutputDebugStringA` 자기 `__except` 가 `$filt$0` 로 받는다 → `__C_specific_handler` → `RtlUnwindEx` → `RtlRestoreContext(0x10000f)` → **유저 `IRETQ`** → 착지 `+0x81`. **중간에 커널 · `NtContinue` 없음.**
- **"test.exe 로 예외 길을 VM 에서 볼 수 있나" 의 답: 유저 쪽만 볼 수 있다.** 커널 `KiDispatchException` · `KiUserExceptionDispatcher` 는 이 길에서 한 번도 안 불린다. 볼 수 있는 자리 (결과서): `ntdll!RtlRaiseException+0x188` · `RtlDispatchException+0x205` (VEH `AL`) · `RtlpExecuteHandlerForException+0xd` (`RAX` = 언어 핸들러) · `__C_specific_handler+0x93` (필터 `EAX`) · `RtlUnwindEx+0x606` · `RtlRestoreContext+0x181` (`ZF`). 커널 쪽 (run81) 은 **새 샘플 (Q156)** 이 있어야 본다.

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리. 같은 번호 둘 = 값에 따라 둘 중 하나.

```
[CPU 예외 — 커널이 쌓은 뒤 (run81)]
ntdll!KiUserExceptionDispatcher (0x164e10)   RSP = 문맥 (이름표 0x10001f) · RSP + 0x4f0 = 레코드
 │  +0x1 Wow64PrepareForException · +0xb JZ (64비트 = 0 → 건너뜀)
 ├─ (1) +0x2e  RtlDispatchException(레코드, 문맥)                               → AL
 │    │  +0x84 NtGlobalFlag 0x800000 꺼짐 → 로그 건너뜀
 │    ├─ (1) +0x205  RtlpCallVectoredHandlers(…, 0)     VEH. +0x3e BT PEB+0x50 비트 2 → 꺼지면 +0x45 JNC 곧장 0
 │    ├─ (2) +0x261  RtlpGetStackLimits                 TEB +0x10 · +0x8, 지금 RSP 가 그 안인가
 │    ├─ (3) +0x330  RtlInitializeExtendedContext2      지역 사본 0x10000b
 │    ├─ (4) +0x33b  RtlpCopyContext                    사본 & 0x10000f
 │    │  ── 프레임 고리 +0x370 ──
 │    ├─ (5) +0x37f  RtlLookupFunctionEntry             LdrInitState == 3 → syscall 없음
 │    ├─ (6) +0x3e8  RtlpxVirtualUnwind(1, …)            머신 프레임은 Rip · Rsp 둘만 (RtlpUnwindPrologue +0x45f · +0x469)
 │    ├─ (7) +0x5f5  RtlpExecuteHandlerForException      +0xd CALL RAX — 언어 핸들러 (CFG 없음) → 예: __C_specific_handler
 │    │  1 다음 프레임 · 0 TRUE · 2 중첩 · 3 충돌 · 끝 → +0x64b FALSE
 │    └─ (8) +0x21d  RtlpCallVectoredHandlers(…, 1)     VCH. TRUE · FALSE 둘 다. 비트 3 꺼지면 곧장
 ├─ (2) +0x3c  RtlGuardRestoreContext(문맥, NULL)       TRUE (VEH −1 · SEH ContinueExecution)
 │       ══▶ +0x1b6 JMP RtlRestoreContext (0x123e20)     +0x174 ~ +0x181 이름표 0x10001f ≠ 0x10000f
 │              └─ +0x18a  NtContinue(문맥, 0)   ══▶ SYSCALL 0x43, 돌아오지 않음
 ├─ (2) +0x53  NtRaiseException(레코드, 문맥, 0)         FALSE → 커널 둘째 기회 (run81)
 └─ (3) +0x5a  RtlRaiseStatus(상태)                       (2) 가 돌아오면 (실패)
        └─ +0x4f  RtlRaiseNoncontinuableException   ══▶ +0x60 JMP RtlDispatchException / +0x6b JMP NtRaiseException

[__except 가 받을 때 — 위 (7) 안]
ntdll!__C_specific_handler (0x124670)
 ├─ (1) +0x30   __except_validate_context_record
 ├─ (2) +0x93   필터 (CALL RAX — 예: RtlUserThreadStart$filt$0 · OutputDebugStringA$filt$0)   < 0 → 0 · 0 → 다음 · > 0 ↓
 └─ (3) +0xd6   RtlUnwindEx(EstablisherFrame, 착지, 레코드, ReturnValue = 코드, 문맥, 히스토리)
       ├─ (1) +0xac   RtlpGetStackLimits
       ├─ (2) +0x897  RtlCaptureContext2                 +0xf3 OR 이름표 0x10000f
       │  프레임마다 +0xf5 RtlLookupFunctionEntry · +0x107 RtlpCopyContext · +0x21e RtlpxVirtualUnwind · +0x2fb RtlpExecuteHandlerForUnwind (__finally)
       │  목적 프레임: +0x3f9 Rax = ReturnValue · +0x40c Rip = 착지 · +0x41b 코드 0xC0000027
       ├─ (3) +0x5af  RtlGuardCheckExceptionHandler      +0x18 init 블록 +0xb8 = 0 → 곧장 끝
       └─ (4) +0x60b  RtlRestoreContext(문맥, 레코드)    +0x181 JZ (0x10000f) → +0x300 IRETQ   ← syscall 0
          (목적 프레임 못 찾음: +0x743 NtRaiseException(…, 0))

[소프트웨어 예외 — 디버거 없음]
KERNELBASE!RaiseException (0xc4140)    +0x4a 주소 = 자기 주소 · 개수 min(n, 15)
 └─ +0x8a  (IAT) ntdll!RtlRaiseException (0xf700)    +0x5f 플래그 | 0x80 · +0x64 이름표 0x10000b
      ├─ (1) +0xe1   RtlInitializeExtendedContext2
      ├─ (2) +0xeb   RtlpCaptureContext2       → RtlCaptureContext2 +0x3c 로 점프 · +0xf3 0x10000f
      ├─ (3) +0x116  RtlLookupFunctionEntry
      ├─ (4) +0x14c  RtlVirtualUnwind          자기 프레임 한 칸 → Rip = RaiseException +0x8a
      │  +0x17e CMP PEB.BeingDebugged · +0x182 JNZ +0x29f
      ├─ (5) +0x18d  RtlDispatchException(레코드, 문맥)      커널 안 거침
      ├─ (6) +0x1a0  RtlRestoreContext(문맥, 레코드)          TRUE → 0x10000f → +0x300 IRETQ (syscall 0)
      └─ (6) +0x2a7  NtRaiseException(레코드, 문맥, 0)        FALSE   (디버거면 +0x29f R8B = 1 로 곧장)
           (돌아오면 +0x1d9 RtlRaiseStatus)

[아무도 안 받음 — 스레드 맨 위]
ntdll!RtlUserThreadStart (0xacac0)     __try [+0x7, +0x4a) · 핸들러 __C_specific_handler
 │  +0x2c (CFG 간접) BaseThreadInitThunk → … 예외 → 위 걷기가 이 프레임에서 필터를 부름
 │  RtlUserThreadStart$filt$0 (0x169b61)
 │   ├─ (1) +0x25  RtlDecodePointer([RtlpUnhandledExceptionFilter 0x1d28e8])     ← KERNELBASE +0x4ab 가 넣은 값 (run68)
 │   ├─ (2) +0x3f  (CFG 간접) KERNELBASE!UnhandledExceptionFilter (0x10ed90)       널이 아니면 → 기본 1
 │   └─ (2) +0x51  RtlUnhandledExceptionFilter2 (0x122300)                          널이면 → 보통 0
 │  1 → __C_specific_handler +0xd6 RtlUnwindEx → +0x60b RtlRestoreContext → IRETQ → 착지 +0x4a
 └─ +0x55  NtTerminateProcess(-1, 예외 코드)       +0x4a MOV EDX,EAX (Rax = ReturnValue = 코드) · +0x4c OR RCX,-1

KERNELBASE!UnhandledExceptionFilter (0x10ed90)      (디버거 없음 · 비 secure · 사용자 필터 없음, 대표만)
 ├─ (1) +0xbf   [delay-load] CheckForReadOnlyResourceFilter     −1 이면 끝
 ├─ (2) +0xda   BasepIsDebugPortPresent → NtQueryInformationProcess(-1, 7)    ≠ 0 이면 0
 ├─ (3) +0x117  NtSetInformationProcess(-1, 0x3f, 8)
 ├─ (4) +0x289  NtSetInformationProcess(-1, 0x3f, 1)
 ├─ (5) +0x29f  GetErrorMode → NtQueryInformationProcess(-1, 0xc)      +0x29f TEST AL,2 → 1 로 끝
 ├─ (6) +0x2d8  NtQueryInformationJobObject(NULL, 2, …)                LimitFlags & 0x400 → 1
 ├─ (7) +0x34c  [delay-load] BasepReportFault                           WER 본체 (이름만)
 └─ (8) +0x4f3  NtRaiseHardError(0xD0000144, 4, …)                       WER 가 안 정함
   +0x59d 반환 [RSP+0x3c] = 1

[__fastfail 유저 쪽]
KERNELBASE!RaiseFailFastException (0x10a020)    코드 0xC0000602 · NONCONTINUABLE
 ├─ (1) +0xc5   SignalStartWerSvc · (2) +0xe0 NtQuerySystemInformation(0x73) · (3) +0x133 WaitForWerSvc
 └─ (4) +0x153  NtRaiseException(레코드, 문맥, 0)    유저 SEH · VEH 안 거침 → 커널 둘째 기회 (run81)
```

## 확인된 것

- `[정적]` `KiUserExceptionDispatcher` 는 `RSP` 하나만 입력으로 쓰고 (`+0x1c ~ +0x26`), `+0x29 RtlDispatchException` → TRUE `+0x37 RtlGuardRestoreContext` · FALSE `+0x4e NtRaiseException(…, 0)` → 돌아오면 `+0x55 RtlRaiseStatus`. `Wow64PrepareForException` 은 64비트에서 0.
- `[정적]` `RtlDispatchException` 본체엔 syscall 이 없다. VEH 는 PEB `+0x50` 비트 2 로 곧장 지나가고 (`RtlpCallVectoredHandlers +0x3e` · `+0x45`), SEH 는 `+0x25c` · `+0x37a` · `+0x3e3` · `+0x5f0` 고리, VCH 는 TRUE · FALSE 둘 다 끝에서 (`+0x218`).
- `[정적]` 끝맺음은 `RtlRestoreContext +0x174 ~ +0x181` 의 이름표 비교 하나로 갈린다: `0x10000f` (`RtlCaptureContext2 +0xf3` 가 만든 문맥 — `RtlUnwindEx` · `RtlRaiseException`) 면 `+0x300 IRETQ` (syscall 0), 아니면 (`0x10001f` — 커널이 쌓은 것) `+0x185 NtContinue(문맥, 0)`.
- `[정적]` `RtlRaiseException` 은 `BeingDebugged == 0` 이면 `+0x188` 에서 `RtlDispatchException` 을 직접 부른다 (커널 안 거침). FirstChance 참은 디버거가 붙었을 때만 (`+0x29f`). kernelbase `RaiseException +0x83` 이 여기로 오고 주소 = `RaiseException +0x8a`.
- `[정적]` 평소 "아무도 안 받음" 은 `RtlUserThreadStart` 의 `__except` (`[+0x7, +0x4a)`) → `$filt$0 +0x20 RtlDecodePointer` → kernelbase `UnhandledExceptionFilter` (기본 1, `+0x59d`) → `RtlUnwindEx` → `IRETQ` → `+0x50 NtTerminateProcess(-1, 예외 코드)`. 커널 둘째 기회까지 안 간다.
- `[정적]` `RaiseFailFastException` 은 `int 0x29` 가 아니라 `+0x14c NtRaiseException(…, FALSE)` — 유저 SEH · VEH 안 거침.
- `[정적]` ntdll 은 이름표에 `0x100040` 이 다 있어야 `CONTEXT_EX` 를 읽고, 머신 프레임은 `Rip` · `Rsp` 둘만 읽는다 (`RtlpUnwindPrologue +0x45f` · `+0x469`). `RtlpContinuationContextMachineFrameEntries` 는 CET 그림자 스택용 (`+0x477` → `+0x487`).
- `[관찰]` (run44 사진, flow-writer 대조) PEB `BeingDebugged` 0 (P1a · P1b · P2), `CrossProcessFlags` 0 또는 2 (여덟 장 — VEH · VCH 비트 꺼짐), KUSD `KdDebuggerEnabled` 3 (K1 · P2). 첫 둘은 test.exe 의 `OutputDebugStringA` 예외가 커널을 안 거치고 VEH 도 안 도는 근거 값이다.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① flow-writer 보탬: `RtlRestoreContext` 의 CET 검사는 결과서 글 "`+0x15b BT [LdrSystemDllInitBlock+0xa8],0x3c`" 와 같은 뜻이지만 줄은 **`+0x150 LEA RAX,[LdrSystemDllInitBlock+0xa0]` · `+0x157 MOV RAX,[RAX+8]` · `+0x15b BT RAX,0x3c` · `+0x160 JC +0x183`** (그리고 `+0x162 ~ +0x172` 가 비트 `0xc` 로 한 번 더) 이다. 값을 레지스터로 읽은 뒤 비트를 본다.
- ② flow-writer 보탬: UEF 7 번 줄 `RtlDecodePointer(BasepCurrentTopLevelFilter)` 의 call 은 **`+0x145`** (ret `+0x14c`) 이다 (결과서 "`+0x13e/+0x14c`" 의 `+0x13e` 는 인자를 싣는 `MOV RCX,[BasepCurrentTopLevelFilter]` 줄). 그 앞 `+0x132 CMP [BasepIsSecureProcess]` · `+0x138 JNZ` 도 있다.
- ③ flow-writer 보탬: UEF `+0xac` 의 callee (rva `0x129be0`) 는 `_calls.tsv` 에 `alias:` 로 찍힌다. 결과서는 "PDB 별칭 3개 한 함수" 라고 했고, `pdb_aliases.py --lookup KernelBase.dll 0x129be0` 은 **같은 주소 이름 4개** (Ghidra 가 고른 이름 + 별칭 3) — 후보 `IsWerpLaunchAeDebugPresent` · `IsWerpGetDebuggerPresent` · `IsCheckForReadOnlyResourceFilterPresent` · `IsBasepReportFaultPresent`. 글에는 하는 일 ("apiset 있나 묻기") 로 적었다.
- ④ flow-writer 보탬: `RtlGuardRestoreContext` (rva `0xa59f0`) 는 `pdb_aliases.py --where ntdll.dll RtlGuardRestoreContext` 로 PDB 이름이고, Ghidra (`_index.tsv`) 는 같은 주소를 `RtlRestoreContext` 로 잡았다 (파일 `RtlRestoreContext__a59f0.asm`, `_calls.tsv` 표시 `alias:RtlRestoreContext+1`). 글에는 PDB 이름을 쓰고 진짜 `RtlRestoreContext` 는 rva `0x123e20` 으로 가렸다 (결과서 스크립트 문제 3 과 같은 이야기).
- ⑤ flow-writer 보탬 (결과서 `[추측]` "`PEB.BeingDebugged` 는 커널 디버거만 붙으면 0" 에 값 근거): run44 사진의 `BeingDebugged` 0 (P1a · P1b · P2) · `CrossProcessFlags` (여덟 장) — 위 "덧붙임 3". 사진은 은퇴 (P2) 까지라 `main` 의 `OutputDebugStringA` 때 값은 `[추측]` (같은 칸을 바꿀 코드가 이 길엔 없음).
- ⑥ flow-writer 보탬 (디버거 영향): KUSD `+0x2d4 KdDebuggerEnabled` 가 이 VM 에선 **3** `[관찰]` 이라 `RtlIsAnyDebuggerPresent` (결과서: `PEB+2` 또는 `KUSD+0x2d4 & 3 == 3`) 는 **참** 이 된다 `[정적 추론]`. test.exe 는 kernelbase 필터가 있어 `RtlUnhandledExceptionFilter2` 를 안 타니 길은 그대로지만, 디버거 없는 PE_emulator 와 VM 이 다를 수 있는 칸이다 (아래 "디버거 메모").

## 정정

- **고칠 run 없음.** 앞 run 이 틀렸다고 드러난 곳은 없다.
- **run81 에 보탬** (틀린 게 아니라 빠진 것 — run81 문서는 안 고침): run81 "최소 규칙" 7 ("둘째 기회 → 종료") 은 맞지만, **평소 "아무도 안 받음" 은 그 길로 안 가고** 유저 `RtlUserThreadStart` 의 `__except` → `UnhandledExceptionFilter` → `NtTerminateProcess(-1, 예외 코드)` 로 끝난다 (위 "요청 26 전체 최소 규칙" 12). 끝 상태는 둘 다 예외 코드라 결과는 같다.
- **run71 · flow.md 확인 — 고칠 곳 없음**: run71 "`OutputDebugStringA +0x7c RaiseException` … `__try` — 디버거가 받으면 아래 안 탐" 은 맞다 (디버거가 없으면 자기 `__except` 가 받아 `+0x81` 몸통으로). flow.md 2.10 run71 절의 "`[관찰 간접]` 커널 디버거는 `0x40010006` 을 삼키지 않았다" 는 더 정확하게는 **커널까지 안 갔다** `[정적]` — 그 줄 뒤에 (run82) 한 줄을 붙였다.

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 안 바꿈 — 예외 길은 진입점까지의 정상 길이 아님): 3장 "CPU 예외를 유저 예외로 넘기기" 절 끝에 **유저 쪽 (run82)** 하위 절 · 2.10 `RtlUserThreadStart` 에 필터 · 끝내기 한 줄 · 2.10 run71 절 (`OutputDebugStringA`) 에 "커널 안 거침" 한 줄 · 2.5 "예외 필터" 에 읽는 곳 한 줄 · 2.4 `InitSpecialMachineFrames` 에 읽는 곳 한 줄 · 기준 · 4장 (run81 줄 (해결됨 → run82) · Q155 · Q156 번호, run82 새 줄) · 5장.
- 짐 지도 (짐 지도 바퀴 아님 — **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**). 이미 있는 줄에 근거만:
  - 3.8 `RtlpUnhandledExceptionFilter` 둘째 줄 — 읽는 곳 (`RtlUserThreadStart$filt$0 +0x19` · `+0x20`) 과 쓰임 (처리 안 된 예외의 필터).
  - 3.9 `LdrSystemDllInitBlock` — `+0xa8` (CET 비트 `0x3c` · `0xc`) · `+0xb8` 를 예외 길이 읽는 줄.
  - 7.2 `_PEB` `끝까지 0` — `CrossProcessFlags.ProcessUsingVEH` · `ProcessUsingVCH` 쓰는 줄 둘 (`RtlpAddVectoredHandler +0x1af` · `RtlpCallVectoredHandlers +0x262`) 과 "초기화 동안 아무도 안 부름" (옮길지는 짐 지도 바퀴에서).
  - 기준 run82 줄 · 판 목록 (서른여덟째판) · 8장 두 표.

## 아직 확인 필요

동적 — 결과서 "동적분석으로 확인해야 할 것" 번호 그대로. **지금 test.exe 로 되는 것은 Q104 VM 단계, 예외를 일부러 내야 하는 것은 Q156 (새 샘플)** 에 붙인다 (오케스트레이터가 대기열로 옮김. 이번 묶음은 VM 안 함). 멈춤점은 모두 test.exe 조건.

- **Q104 (지금 test.exe 로)** — test.exe 의 `OutputDebugStringA` (dll2 TLS 콜백부터 `Log` 마다) 가 유저 예외 길을 탄다:
  - 9 `OutputDebugStringA` 길 차례: `ntdll!RtlRaiseException+0x188 → RtlDispatchException+0x205 → __C_specific_handler+0x93 → RtlUnwindEx+0x606 → RtlRestoreContext+0x181`, 그리고 `ntdll!NtRaiseException` · `ntdll!NtContinue` 가 **한 번도 안 불리는지**. `nt!KiDispatchException` 도 test.exe 조건으로 안 서는지.
  - 3 (앞 반) `RtlRestoreContext+0x174` 뒤 `eax` (= 이름표 `& 0xffffff3f`) 가 `0x10000f` 인지, `+0x300 IRETQ` 의 `rsp` 틀.
  - 4 `RtlRaiseException+0xa0` 의 `ecx` — `0x10000b` / `0x10004b` (KUSD `+0x3d8` `0xe7` 이면 뒤).
  - 8 `RtlRaiseException+0x188` 에서 `[레코드+0x10]` = `KERNELBASE!RaiseException+0x8a` 인지.
  - 2 `LdrSystemDllInitBlock +0xa8` · `+0xb8` 가 test.exe 밖 프로세스에서도 0 인지 (`ntdll!RtlRestoreContext+0x150` 에서 `dq ntdll!LdrSystemDllInitBlock+a0 l4`). test.exe 값은 run44 `[관찰]` 이미 있음.
  - 디버거 영향 하나: `RtlRaiseException+0x17e` 에서 `db @rax+2 l1` (`BeingDebugged`) — KDNET 만 붙었을 때 0 인지 (run44 사진은 은퇴까지 0).
- **Q156 (새 샘플 — 예외를 일부러 내는 작은 프로그램)** — run81 동적 4 와 같은 샘플에 `__try` 없는 경우 · VEH 를 거는 경우를 더하면 닫힌다:
  - 1 UEF 가 정말 1 로 끝나는 길과 syscall 차례 — `kernelbase!UnhandledExceptionFilter+0x29f` (`GetErrorMode` 뒤 `al`) · `+0xb1` (apiset `al`) · `+0x2d8` · `+0x34c` (`BasepReportFault` `eax`) · `+0x4f3` · `+0x59d` (`[rsp+3c]`).
  - 3 (뒤 반) CPU 예외 길에서 `KiUserExceptionDispatcher` → `RtlRestoreContext+0x185 NtContinue` 가 불리는지 (VEH −1 또는 `EXCEPTION_CONTINUE_EXECUTION`).
  - 5 `RtlDispatchException+0x5f5` 의 `ecx` (disposition) · `+0x205` 의 `al` (VEH) — VEH 를 건 프로그램. PEB `+0x50` 비트 2 · 3 이 켜지는 때 (`RtlpAddVectoredHandler+0x1af`).
  - 6 스택 넘침 예외에서 `RtlpGetStackLimits` 가 실패하나 (`RtlDispatchException+0x263`), 커널이 스택을 늘릴 때 TEB `+0x10` 도 낮추나 (run81 동적 6 과 이음).
  - 7 첫 UEF 호출에서 delay-load 가 호스트 `kernel32.dll` 로 풀리나 (`ntdll!LdrResolveDelayLoadedAPI` 로 들어가나, 새 DLL 을 올리나). 풀리는 이름 `kernel32!CheckForReadOnlyResourceFilter` · `kernel32!BasepReportFault` 는 `[정적: apisetschema.dll 표 + kernel32 _index.tsv]`.
- 추천 멈춤점 (결과서): `ntdll!KiUserExceptionDispatcher` (입구 `RSP` · `RSP+0x4f0`), `ntdll!RtlDispatchException+0x205` · `+0x5f5`, `ntdll!__C_specific_handler+0x93`, `ntdll!RtlUnwindEx+0x606`, `ntdll!RtlRestoreContext+0x174` · `+0x185` · `+0x300`, `ntdll!RtlRaiseException+0x188` · `+0x2a2`, `kernelbase!RaiseException+0x83`, `kernelbase!UnhandledExceptionFilter+0x29f` · `+0x34c` · `+0x4f3`.

정적 — 다음 바퀴 후보 (대기열 번호는 오케스트레이터가):

- ① kernel32 쪽 WER 호스트 `kernel32!CheckForReadOnlyResourceFilter` (rva `0x5c2c0`, 78B) · `kernel32!BasepReportFault` (rva `0x622d0`, 42B) 첫 줄 — 무엇을 부르나 (이번엔 이름만). PE_emulator 가 "WER 없음" 을 흉내 낼 때 어떤 답을 주면 4 를 받는지.
- ② `EPROCESS.DefaultHardErrorProcessing` (`+0x388`) 비트 `0x2` 가 부모 cmd → test.exe 로 어떻게 오나 (결과서 "부모 값 복사 / 부모 없으면 5") — 짐 지도 3.11 줄과 맞춰 볼 것. 이 비트가 UEF 의 WER 갈림 (11 번) 을 정한다.
- ③ `NtSetInformationProcess` 클래스 `0x3f` 의 커널 쪽 (ntoskrnl `NtSetInformationProcess` 갈래) — 값 8 · 1 이 무엇을 바꾸나 (클래스 이름 `[추측]`).
- ④ 작음: `RtlpxVirtualUnwind +0x6ba` 리프 처리 안쪽, `RtlpLookupDynamicFunctionEntry` (동적 함수표 — JIT · 셸코드), `KUSD +0x708` 칸 뜻 (CET_U 비트).
- 짐 지도 바퀴 후보: 7.2 `_PEB` `끝까지 0` 의 `CrossProcessFlags.ProcessUsingVEH` · `ProcessUsingVCH` 를 "쓰는 줄은 있으나 초기화 동안 안 부름" 으로 옮길지.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점** `[정적]`:
  - `RtlRaiseException` 은 `PEB.BeingDebugged` 하나로 갈린다 (`+0x17e`). 유저 디버거가 붙으면 `NtRaiseException(…, TRUE)` 로 커널이 먼저 보고, KDNET 만 붙으면 (run44 사진 0) 커널을 안 거친다. → windbg-kd-runbook pitfalls 에 추가 제안: "**소프트웨어 예외 (`RaiseException`, `OutputDebugStringA` 의 `0x40010006`) 는 유저 디버거가 없으면 커널을 안 거친다 — `nt!KiDispatchException` 멈춤점은 안 선다. 유저 쪽 길은 `ntdll!RtlRaiseException+0x188` 등 ntdll 멈춤점으로 본다**" (명령 이름은 dast-mentor 가 확인).
  - KUSD `+0x2d4 KdDebuggerEnabled` = 3 `[관찰]` (KDNET) → `RtlIsAnyDebuggerPresent` 가 참 `[정적 추론]`. `RtlUnhandledExceptionFilter2` (kernelbase 필터가 없을 때만) 와 UEF 1 번 줄 (코드 `0xC0000409` 일 때만 `BasepIsKernelDebuggerPresent`) 이 VM 에서 디버거 갈래로 갈 수 있다 `[추측]`. test.exe 정상 길엔 영향 없음.
- 도구 (결과서 "스크립트 문제", 오케스트레이터 몫 — 하네스 제안): ① 지도가 delay-load (`UnhandledExceptionFilter +0xb8` · `+0x345`) 를 `external` (import) 로 적는다 — `DelayLoad_*` 스텁이 가리키는 칸은 첫 호출 때 로더가 푼다. `delay-load` 로 따로 표시하면 "이 호출이 로더 일을 부를 수 있다" 를 놓치지 않는다. ② `UNWIND_INFO` 의 예외 처리기 (`RtlpExecuteHandlerForException` 의 `0x1654c0`) 는 함수로 안 잡혀 `.asm` · `pdb_aliases --where` 에 없다 — run81 의 "`__except` 본문이 틈으로만 나옴" 과 같은 꼴. ③ 같은 이름 두 함수 (`RtlRestoreContext` `0xa59f0` · `0x123e20`) 는 파일 이름에 rva 를 붙여 갈라 준다 — 문서에선 PDB 이름 (`RtlGuardRestoreContext`) 으로 부르는 게 헷갈리지 않는다.

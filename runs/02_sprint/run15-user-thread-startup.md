# 2026-10-22 run15 — test.exe 스레드의 첫 커널 코드와 커널 → 유저 전환: `KiStartUserThread` (= `KxStartUserThread +0x60`) → `PspUserThreadStartup` → `IRETQ` (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: ntoskrnl (`ntoskrnl.exe`, 26100.9457), pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음)
  - analyze 는 다시 안 했다. 기존 프로젝트에서 export 만.
  - 버전 대조: 일치. `NtCreateUserProcess +0xc36`, `PspInsertThread +0x807` 이 `_calls.tsv` 와 같고, `PspUserThreadStartup` 호출 9줄이 요청서와 같다 (결과서).
- 뽑은 함수 (모두 depth 0)
  - 요청서 2개: `KxStartUserThread` (rva `0x6afb70`), `PspUserThreadStartup` (rva `0x910de0`, 232바이트)
  - 넓힌 것 2개: `KiKernelExit` (167바이트, 유저로 가는 다른 `IRETQ` 가 여기 있어서), `KiLowerIrqlProcessIrqlFlags` (91바이트, `PspUserThreadStartup +0x1d` 가 APC 와 관계있나 보려고)
  - 이미 있던 것도 근거로 씀: `KiInitializeContextThread.asm`, `SwapContext.asm` (sast/08 때 뽑음)

## 목적

- 대기열 **Q6**. flow.md 2.3 을 채운다.
  1. 이름표 `KiStartUserThread` 가 Ghidra 에서는 어디인가, run4 `[관찰]` `KiStartUserThread+0x28` 바로 앞이 시작 루틴을 부르는 간접 호출인가, 시작 루틴 주소는 어디서 오나.
  2. 콜스택에 보인 `KiStartUserThreadReturn` 은 무엇인가.
  3. **2.3 `[공백]` 커널 → 유저 전환**: 어떤 명령으로, 어디서 유저로 넘어가나. 유저 첫 `RIP` 는 어느 칸에서 꺼내나.
  4. `PspUserThreadStartup` 의 흐름. 특히 run13 `[추측]` "멈춤 APC 는 `+0x22` IRQL 내림 직후" 를 더 굳힐 수 있나, `PspInitializeThunkContext` 는 첫 스레드만인가.

## 방법

- 정적: `_workspace/sast/10_user-thread-startup.md` — ntoskrnl, `^(KxStartUserThread|PspUserThreadStartup)$` depth 0
- 결과서: `_workspace/sast/10_user-thread-startup_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntoskrnl/{KxStartUserThread,PspUserThreadStartup,KiKernelExit,KiLowerIrqlProcessIrqlFlags,KiInitializeContextThread,SwapContext}.asm`, `ms/disassemble/ntoskrnl/_calls.tsv`
- 이름 대응은 오케스트레이터가 `pdb_aliases.py --where` 로 확인했다 (요청서).
  - `KiStartUserThread` = rva `0x6afbd0`, PDB 종류 **함수 아님(none)**, `_index.tsv` 에 없음 → Ghidra 함수 **`KxStartUserThread`**(rva `0x6afb70`) 안 **`+0x60`** 에 붙은 이름표.
  - `KiStartUserThreadReturn` = rva `0x6afb60`, 함수 아님. `KxStartUserThread` 바로 앞 16바이트.
  - 그래서 **`KiStartUserThread+x` = `KxStartUserThread +(x+0x60)`** 이다. 아래에서 `Kx +0x…` 는 `KxStartUserThread` 기준, WinDbg 에 보일 모양은 `KiStartUserThread+…` 다 `[추측]` (가장 가까운 앞 이름으로 보여 주는 규칙, run4 에서 `+0x88` 이 `KiStartUserThread+0x28` 로 보인 것과 같음).
- `.asm` 에 없는 곳은 결과서가 **원본 바이트**로 읽었다 (출처: `ms/bin/.../System32/ntoskrnl.exe` 의 rva 범위를 `objdump -D -b binary -m i386:x86-64 -M intel` 로). 이 문서에서 "(원본 바이트)" 라고 붙인 곳이 그것이다.
  - `KiStartUserThreadReturn` 과 앞뒤 (`0x6af7a0~0x6afb70`), `KxStartUserThread` 의 `.asm` 에 빠진 틈, `PspUserThreadStartup` 의 `.asm` 에 빠진 `+0x80~+0x90`, `_guard_dispatch_icall` 앞부분 (`0x6b77f0~`).
  - 함수 범위·풀어 보기 정보는 같은 파일의 예외 디렉터리(`.pdata`, UNWIND_INFO)로 읽었다.
- 구조체 칸 이름은 **PDB 타입 레코드**로 맞췄다 (`llvm-pdbutil-14 dump -types ms/sym/ntoskrnl.exe.pdb`, 결과서). 자주 나오는 것:
  - `_KTRAP_FRAME` (크기 `0x190`): `MxCsr +0x2c`, `Rbp +0x158`, **`Rip +0x168`**, `SegCs +0x170`, `EFlags +0x178`, `Rsp +0x180`, `SegSs +0x188`
  - `_KEXCEPTION_FRAME` (크기 `0x140`): `P1Home +0x0`, `P2Home +0x8`, `P3Home +0x10`, **`Return +0x138`**
  - `_KTHREAD`: `InitialStack +0x28`, `KernelStack +0x58`, `ApcState.KernelApcPending +0xc1`, `ApcState.UserApcPendingAll +0xc2`, `Teb +0xf0`
  - `_ETHREAD.CrossThreadFlags +0x5a0` (bit0 `Terminated`, bit1 `ThreadInserted`), `_EPROCESS.Flags3 +0x5fc` (bit0 `Minimal`), `_TEB.CurrentLocale +0x108`
  - 전역: `KiIrqlFlags`, `KeSmapEnabled`, `KiKvaShadow` (PDB 공개 심볼)
- 오프셋 읽는 법 (run9 ~ 14 와 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**.

## 결과

### 정적 — 시작 루틴 부르기 (`KxStartUserThread.asm`)

새 스레드는 이 코드에 **call 로 들어오지 않는다.** `SwapContext` 의 `RET` 가 첫 스택에 쌓인 주소로 "돌아가는" 모양으로 들어온다 (아래 "시작 루틴 주소가 오는 길").

| 자리 (Kx) | = WinDbg 모양 | 명령 | 뜻 | 표시 |
|---|---|---|---|---|
| `+0x60` | `KiStartUserThread` | `TEST byte [KeSmapEnabled],0xff` / `+0x67 JZ` / `+0x69 STAC` | 이름표 자리. SMAP 이 켜져 있으면 `STAC` | [정적] |
| `+0x6c`, `+0x71` | `+0xc`, `+0x11` | `MOV ECX,1` / `MOV CR8,RCX` | **IRQL 을 1 (APC 단계)로** | [정적] |
| `+0x75`, `+0x79` | `+0x15`, `+0x19` | `MOV RDX,[RSP]` / `MOV RCX,[RSP+0x8]` | 인자 2 = StartContext, 인자 1 = StartRoutine | [정적] |
| `+0x7e` | `+0x1e` | `MOV RAX,[RSP+0x10]` | **시작 루틴 주소를 꺼낸다** | [정적] |
| `+0x83` (ret **`+0x88`**) | call `+0x23` / ret **`+0x28`** | `CALL 0x1406b77f0` (`_guard_dispatch_icall`) | CFG 검사 뒤 RAX 로 넘어감. `_calls.tsv`: `KxStartUserThread +0x83 +0x88 indirect:cfg:stack[-0x128]` | [정적] |
| `+0x88` | **`+0x28`** (`0x1406afbf8`) | `LEA RCX,[RSP+0x100]` | 시작 루틴이 돌아오는 자리 | [관찰] run4 + [정적] |

- `stack[-0x128]` 은 입구 RSP 기준이고, 지금 RSP(입구 - 0x138) 기준으론 `[RSP+0x10]` — `+0x7e` 와 같은 칸이다 (결과서).
- `_guard_dispatch_icall` 은 앞부분만 원본 바이트로 읽었다: 비트맵 검사 뒤 RAX 로 넘어가는 모양이라 콜스택엔 안 남는다. run4 `[관찰]` 콜스택에 안 보인 것과 맞는다.
- 호출 모양: `PspUserThreadStartup(StartRoutine = 0, StartContext = ETHREAD.StartAddress)`. 하지만 `PspUserThreadStartup` 은 두 인자를 **읽지 않는다** (`+0x17 MOV RCX,CR8`, `+0x1b XOR EDX,EDX` 로 덮어씀).
- `Kx +0x0 ~ +0x5f` 는 실제로 도는 코드가 아니라 **풀어 보기(unwind)용 앞머리**로 보인다 `[정적]+[추측]`. `.pdata` UNWIND_INFO 의 앞머리 설명(스택 0x138 확보, `xmm6~15`·`rbx` 등 저장, 길이 0x5f)과 같고, `0x6afb70` 을 부르는 곳은 아래 `KyStartUserThread` 템플릿 안 `call` 하나뿐이다 (결과서가 실행 구역 전체를 훑음. 계산해서 만드는 주소는 못 잡음).

### 정적 — 시작 루틴 주소가 `[RSP+0x10]` 까지 오는 길

run11·run13 에서 `[추측]` 으로 남겼던 사이 전달이 한 줄씩 이어졌다. 전부 `[정적]`.

| # | 자리 | 하는 일 |
|---|---|---|
| 1 | `PspAllocateThread +0x681 LEA RAX,[0x140910de0]`, `+0x688 MOV [RSP+0x128],RAX` | 시작 정보 묶음(`rsp+0x120`)의 `+0x8` 칸에 `PspUserThreadStartup` (run11). `+0x6e9` 에서 묶음 주소를 `KeInitThread` 2번째 인자로 |
| 2 | `KeInitThread +0x2f6 MOV RDX,R15`, call `+0x2fc` / ret `+0x301` | 그 묶음을 `KiInitializeContextThread` 2번째 인자로 (거기선 `+0x3c MOV R13,RDX`) |
| 3 | `KiInitializeContextThread +0x1c7 MOV RAX,[R13+0x8]` → **`+0x1cf MOV [RBX+0x10],RAX`** | 새 커널 스택의 `_KEXCEPTION_FRAME.P3Home`(`+0x10`) 칸에 `PspUserThreadStartup`. 같은 식으로 `+0x1b1`→`[RBX+0]` StartContext, `+0x1bf`→`[RBX+0x8]` StartRoutine |
| 4 | `KiInitializeContextThread +0x1e1 LEA` / `+0x1e8 MOV [R13+0x38],RAX` | 그 아래 스택 맨 위에 `KiStartUserThread`(`0x1406afbd0`) 주소. `R13 = RBX - 0x40` (`+0x1cb`) 이 새 `KernelStack` (`+0x2ce MOV [RSI+0x58],R13`) |
| 5 | `SwapContext +0xf7 MOV RSP,[RSI+0x58]` … `frag:FUN_1406b686f +0x2ba ADD RSP,0x30` / `+0x2be POP RBP` / **`+0x2bf RET`** | 새 스레드로 바꿀 때 그 스택을 쓰고, `RET` 가 `[R13+0x38]` 을 꺼내 **`KiStartUserThread` 로 뛴다.** 이때 `RSP = RBX` 라서 `[RSP+0x10]` = 3번 칸. `RBP = [R13+0x30]` = 트랩 프레임 + 0x80 (`KiInitializeContextThread +0x274`, `+0x27b`) |

- 그래서 `KiStartUserThread` 를 가리키는 코드 쪽 참조는 `KiInitializeContextThread +0x1e1` 하나뿐이다 (8바이트 주소로는 `CFGRO` 구역 한 곳 — 구역 이름으로 보아 CFG 쪽 표 `[추측]`). `PspUserThreadStartup` 이 `NtCreateUserProcess` 지도에 안 닿는 이유도 이것이다 (sast/05 와 맞음).

### 정적 — `KiStartUserThreadReturn` (rva `0x6afb60`)

- **두 바이트 `90 C3` = `NOP; RET`** (원본 바이트). 뒤는 `INT3` 채움과 `NOP` 정렬.
  - PDB 로는 함수 `KyStartUserThread` (rva `0x6af810~0x6afb62`) 의 **끝 두 바이트**다. Ghidra 는 `FUN_1406afa82` 의 끝으로 본다.
  - `KyStartUserThread` 는 트랩 프레임을 만드는 진입 코드 모양의 **템플릿**이고 끝(`0x6afb4c`)에서 `CALL KxStartUserThread` 한다. 이 템플릿으로 들어오는 참조는 못 찾았다 `[정적]` (원본 바이트·`.pdata`). 쓸모는 풀어 보기 정보로 보인다 (UNWIND_INFO 에 "기계 프레임 있음 + 프레임 레지스터 `rbp`" 모양).
- **실행되는 코드가 아니라 "가짜 돌아올 주소"다** `[정적]`.
  - `KiInitializeContextThread +0x1d3 LEA RAX,[0x1406afb60]` → **`+0x1da MOV [RBX+0x138],RAX`** — `_KEXCEPTION_FRAME.Return` 칸. `KxStartUserThread` 가 확보한 0x138 바이트 바로 위, 즉 **`KxStartUserThread` 프레임의 "돌아올 주소" 자리**다.
  - `KxStartUserThread` 는 `RET` 가 아니라 `IRETQ` 로 끝나니 이 주소로 실제로 돌아가는 일은 없다. 이 주소를 쓰는 코드도 위 `LEA` 하나뿐.
- **콜스택에 이 이름이 보인 이유**: `KiStartUserThread+0x28` 프레임 위 칸이 이 주소라 `KiStartUserThreadReturn` 으로 읽힌다 `[정적]`. 이 주소는 `KyStartUserThread` 범위 안이라 그 풀어 보기 규칙(트랩 프레임에서 `Rip`/`Rsp` 읽기)이 적용되어 다음 칸이 **유저 쪽 자리**로 풀린다 → run4 `[관찰]` 의 `ntdll!RtlUserThreadStart` `[추측]` (관찰과 맞지만 WinDbg 의 풀어 보기를 코드로 따라가진 않음).
- 시작 루틴(`PspUserThreadStartup`)의 돌아올 주소는 이게 아니라 `Kx +0x88` (= `KiStartUserThread+0x28`) 이다. `KiStartUserThreadReturn` 은 그 **한 칸 위**다.

### 정적 — 커널 → 유저 전환 (2.3 `[공백]` 채움)

시작 루틴이 돌아온 뒤 `Kx +0x88` 부터 **"복원 → 인터럽트 끄기 → 유저 APC 확인 → 레지스터 복원 → `IRETQ`"** 로 유저에 들어간다. `SYSRETQ` 가 아니다 (이 길에 `SYSRET` 없음).

| # | 자리 (Kx) | 하는 일 | 표시 |
|---|---|---|---|
| 1 | `+0x88 ~ +0xdc` | `_KEXCEPTION_FRAME` 에서 `xmm6~15`, `rbx`, `rdi`, `rsi`, `r12~r15` 복원 | [정적] |
| 2 | `+0xe0 CLI` | 인터럽트 끔 | [정적] |
| 3 | `+0xe1 TEST byte [RBP+0xf0],1` / `+0xe8 JZ → +0x3e3` | 트랩 프레임 `SegCs` 맨 아래 비트. 0 이면 **커널로 돌아가는 가지** (`+0x3e3 ~ +0x42c`, 끝 `+0x42c IRETQ`). 유저 스레드는 CS `0x33` 이라 안 탄다 (`KiInitializeContextThread +0x163 LEA EAX,[RDX+0x33]`, `+0x16b` SS `0x2b`) | [정적] |
| 4 | `+0xee ~ +0xf7` | `KeSmapEnabled` 이면 `STAC` | [정적] |
| 5 | `+0xfa ~ +0x125` 되돌이 | `ApcState.UserApcPendingAll` (`[스레드+0xc2] & 3`) 이면 IRQL 1·`STI` → **`KiInitiateUserApc`** (call `+0x116` / ret `+0x11b`) → `CLI`·IRQL 0 → `+0xfa` 로 | [정적] 조건부 |
| 6 | `+0x127` (ret `+0x12c`) | `KiSynchronizeUserIsolationDomainEntry` 가 0 이 아니면 `+0x132 JNZ` 로 `+0xfa` 에 되돌아감 | [정적] 조건부 |
| 7 | `+0x13d ~ +0x229` | 조건부 마무리: `KiUpdateStibpPairing` (ret `+0x146`), `KiRestoreSetContextState` (ret `+0x15c`), `KiCopyCounters` (ret `+0x178`), `LDMXCSR` 트랩 프레임 `MxCsr` (`+0x181`), `KiRestoreDebugRegisterState` (ret `+0x194`, `Dr7` ≠ 0 일 때), CET 그림자 스택 처리, 추측 실행 완화용 `WRMSR`, 리턴 예측 버퍼 채우기 덩어리 (`+0x229`). 갈림은 CPU 플래그 (`GS:[0x858]` 등) | [정적] 목적은 [추측] |
| 8 | 조각 `FUN_1406afeb5` (Kx `+0x345~`) | 휘발 레지스터 복원: `xmm0~5`, `r11 … rax` (트랩 프레임에서) | [정적] |
| 9 | 조각 `+0x68 MOV RSP,RBP` / `+0x6b MOV RBP,[RBP+0xd8]` / `+0x72 ADD RSP,0xe8` | `RBP` 를 유저 값(`Rbp +0x158`)으로, **`RSP = 트랩 프레임 + 0x168`** → `[RSP]` 부터 `Rip, SegCs, EFlags, Rsp, SegSs` | [정적] |
| 10 | 조각 `+0x79 TEST byte [KiKvaShadow],1` | 갈림: 아래 (가) / (나) | [정적] |

- **유저로 넘어가는 명령 = `IRETQ`. 자리는 둘 중 하나** (`KiKvaShadow` 값에 따라. 정적으로는 못 정함):
  - (가) `KiKvaShadow` bit0 = 0: 조각 `+0x99 SWAPGS` → **조각 `FUN_1406afeb5 +0x9c IRETQ`** (`0x1406aff51` = **Kx `+0x3e1`** = `KiStartUserThread+0x381` `[추측: 표기]`).
  - (나) `KiKvaShadow` bit0 = 1: 조각 `+0x82` (Kx `+0x3c7`) **`JMP KiKernelExit`** (꼬리 점프, `_calls.tsv`: `frag:FUN_1406afeb5+0x82 tail KiKernelExit`). `KiKernelExit` 는 기계 프레임 5칸을 CPU 별 임시 스택(`GS:[0x38]+0x4200`)으로 옮기고 (`+0x2e ~ +0x3a PUSH`), 조건부로 **`+0x83 MOV CR3,RSP`** (유저 주소 공간으로), `+0xa2 SWAPGS`, **`+0xa5 IRETQ`** (`0x140bcac25`).
  - `Kx +0x42c` 의 `IRETQ` 는 3번의 **커널로 돌아가는 가지** 끝이다. 유저 스레드 길이 아니다.
- **유저 첫 `RIP` 를 꺼내는 칸 = `_KTRAP_FRAME.Rip` (`+0x168`)** `[정적]`. `IRETQ` 는 `[RSP]` 에서 `RIP, CS, RFLAGS, RSP, SS` 를 꺼내고, 9번에서 `RSP` = 트랩 프레임 + 0x168 이다. (나) 도 같은 5칸을 그대로 옮겨 쓴다.
- **트랩 프레임 위치 = `KTHREAD.InitialStack - 0x190`** `[정적]` — `KiInitializeContextThread +0x1b5 LEA R11,[R15-0x190]`, `+0x2ca MOV [RSI+0x28],R15`. 시작 루틴이 도는 동안은 `RBP - 0x80` 이다.
- **그 칸을 누가 채우나 (이름만)**:
  - 처음: `KiInitializeContextThread +0x1ac` call / ret `+0x1b1` `KxContextToKframes` — 유저 CONTEXT 를 트랩 프레임·예외 프레임으로 옮기는 쪽으로 보인다 `[추측]` (이름·인자 모양, 본문 안 뽑음).
  - 다시: `PspUserThreadStartup +0xb3` 의 `PspInitializeThunkContext` (Q7 에서 본다).

### 정적 — `PspUserThreadStartup` 흐름 (`PspUserThreadStartup.asm`, rva `0x910de0`)

점프 따라 본 순서. 자리 = ret_off (괄호 = call_off).

| # | 자리 | 하는 일 | 조건 |
|---|---|---|---|
| 1 | `+0x1d` (ret `+0x22`) | `KiLowerIrqlProcessIrqlFlags(현재 IRQL, 0)` | `KiIrqlFlags` ≠ 0 일 때만 |
| 2 | **`+0x22 MOV CR8,RDI`** (`RDI = 0`) | **IRQL 0 으로** | 늘 |
| 3 | `+0x26 MOV RBX,GS:[0x188]` | 현재 스레드 | |
| 4 | **`+0x3c`** (call `+0x37`) | `PspDisablePrimaryTokenExchange(스레드)` | 늘 |
| 5 | `+0x4b TEST AL,2` → `+0xf5` (call `+0xf0`) | `PspTerminateThreadByPointer(스레드, 0xC000004B, TRUE)` | `ThreadInserted` = **0** 일 때만 |
| 6 | `+0x53 ~ +0x6a` | 프로세스 `Flags3` bit0 `Minimal` = 1 이면 `+0xca` (call `+0xc5`) `DbgkCreateMinimalThread` 로 가서 7·8 건너뜀 | |
| 7 | **`+0x78`** (call `+0x73`) | `TEB.CurrentLocale = PsGetSessionLocaleId()`. `+0x73~+0x80` 은 SEH `try`, 처리기 `+0x80` (원본 바이트) | `Minimal` = 0 |
| 8 | **`+0x99`** (call `+0x94`), **`+0xa1`** (call `+0x9c`) | `KeUpdateTebSchedulingPropertiesCurrentThread(스레드)`, `PspNotifyThreadCreation(스레드)` | `Minimal` = 0 |
| 9 | `+0xa1 TEST [R14],1` → `+0xe3` (call `+0xde`) | `KeWaitForSingleObject(스레드, UserRequest(6), UserMode, 알림 불가, 시간제한 없음)` 하고 10 건너뜀 | `Terminated` = **1** 일 때만 |
| 10 | **`+0xb3`** (call `+0xae`) | **`PspInitializeThunkContext()`** — 인자 레지스터를 새로 안 채움 | `Terminated` = 0 **그리고** `Minimal` = 0 |
| 11 | `+0xb3 ~ +0xc0` | 복원, `RET` → `KiStartUserThread+0x28` | |

- `+0xe3` 대상은 장부에 `alias:KeWaitForMutexObject+1` 로 찍힌다. PDB 공개 이름은 `KeWaitForSingleObject` 하나 (run13 과 같음).
- **정상 길** (`ThreadInserted` = 1, `Terminated` = 0, `Minimal` = 0) 호출 순서 `[정적: 조건 읽음, 값은 동적]`: `+0x3c` `PspDisablePrimaryTokenExchange` → `+0x78` `PsGetSessionLocaleId` → `+0x99` `KeUpdateTebSchedulingPropertiesCurrentThread` → `+0xa1` `PspNotifyThreadCreation` → `+0xb3` `PspInitializeThunkContext` → `RET`. (`KiIrqlFlags` 가 켜져 있으면 `+0x22` 가 앞에 붙음.)
- **`PspInitializeThunkContext` 는 첫 스레드만이 아니라 조건(`Terminated` = 0, `Minimal` = 0)을 만족하는 모든 스레드에 불린다** `[정적]`. 이 함수 안에 "첫 스레드인지" 가르는 줄이 없다. 첫 스레드만 다른 일이 있다면 `PspInitializeThunkContext` 안일 것이다 (Q7).
- 3개 조건 갈래 한 줄씩:
  - `DbgkCreateMinimalThread` (`+0xca`) ← 프로세스가 Minimal 일 때. 로케일·TEB 속성·스레드 만들기 알림·썽크 문맥을 모두 건너뛴다.
  - `KeWaitForSingleObject` (`+0xe3`) ← 이미 "끝내라" 표시(`Terminated`)를 받은 채 여기까지 왔을 때.
  - `PspTerminateThreadByPointer` (`+0xf5`) ← 프로세스에 끝까지 못 들어간 스레드(`ThreadInserted` = 0). 자기 자신이면 돌아오지 않는 호출로 보인다 `[추측]`.

### 정적 — run13 "멈춤 APC 는 `+0x22` 직후" 고리

run13 의 `[추측]` 한 덩어리가 **고리 3개 `[정적]` + 마지막 1개 `[추측]`** 로 나뉘었다.

| 고리 | 내용 | 근거 | 표시 |
|---|---|---|---|
| 1 | `KiStartUserThread` 는 **IRQL 1** 로 시작 루틴을 부른다 | Kx `+0x6c MOV ECX,1`, `+0x71 MOV CR8,RCX` | [정적] |
| 2 | `+0x22` 앞의 `KiLowerIrqlProcessIrqlFlags` 는 APC 를 배달하지 않는다. 옛 IRQL 이 2 이상일 때만 일하는데 지금은 1 이라 **아무것도 안 하고 돌아온다** → `+0x22 MOV CR8,RDI` 가 1 → 0 의 **유일한 자리** | `KiLowerIrqlProcessIrqlFlags +0x1d CMP CL,2` / `JC → 끝` | [정적] |
| 3 | `SwapContext` 끝 조각은 새 스레드의 `KernelApcPending`(`[스레드+0xc1]`) 이 1 이고 (스택 `[RSP+0x28]` 바이트 OR `[스레드+0x1e6]`) ≠ 0 이면 `HalRequestSoftwareInterrupt(1)` (APC 단계 인터럽트 요청). 새 유저 스레드의 그 스택 바이트는 **늘 1** | `frag:FUN_1406b686f +0x292 CMP [RSI+0xc1],1`, `+0x29b MOVZX AX,byte [RSP+0x28]`, `+0x2a1 OR`, call `+0x2af`. `KiInitializeContextThread +0x26f MOV byte [R13+0x28],1` | [정적] |
| 마지막 | 걸린 인터럽트가 실제로 **`+0x22` 의 `MOV CR8` 직후, 곧 `+0x26` 에서** 들어오나 | CR8 을 내리면 걸려 있던 인터럽트가 다음 명령 앞에서 들어온다는 CPU 규칙 | [추측] (Q42) |

- 반대 증거는 없다: 유저로 가는 복귀 길(Kx 5번)은 **유저 APC 만** 본다 (`[스레드+0xc2]`). 커널 APC(멈춤 APC)는 안 본다. 그러니 `PspUserThreadStartup` 안에서 IRQL 이 0 이 되는 순간이 첫 기회다.
- `+0x26` 이후 다른 호출 사이에서 잠들 수도 있다 (예: `+0x37` 이 락을 풀 때) `[추측]`. 코드만으론 못 정한다.

### 호출 관계

```
(test.exe 스레드, 커널)                                      Kx = KxStartUserThread 기준
SwapContext 끝 조각 +0x2bf RET  ── 첫 스택 [R13+0x38] 을 꺼내 "돌아감" (call 아님)
KiStartUserThread  (= Kx +0x60 이름표)                       [관찰] run4 이름 + [정적]
 │  Kx +0x71  IRQL 1
 │  Kx +0x7e  RAX = [RSP+0x10] = PspUserThreadStartup  (KiInitializeContextThread +0x1cf 가 쌓음)
 ├─ (1) +0x28 (Kx +0x88)  PspUserThreadStartup                [관찰] run4 + [정적]
 │       │  +0x22  IRQL 0   ═▶ (멈춘 스레드면) APC 인터럽트 → KiDeliverApc … (2.2)  [추측: 자리]
 │       ├─ (1) +0x3c  PspDisablePrimaryTokenExchange
 │       ├─ (2) +0x78  PsGetSessionLocaleId                         (Minimal = 0)
 │       ├─ (3) +0x99  KeUpdateTebSchedulingPropertiesCurrentThread (Minimal = 0)
 │       ├─ (4) +0xa1  PspNotifyThreadCreation                      (Minimal = 0)
 │       └─ (5) +0xb3  PspInitializeThunkContext   (Terminated = 0, Minimal = 0. 모든 스레드)
 │  Kx +0x88 ~ +0xdc  비휘발 레지스터 복원 → +0xe0 CLI → +0xe1 CS 검사 (유저면 계속)
 │  Kx +0xfa ~ +0x132 유저 APC 있으면 KiInitiateUserApc (ret +0x11b) 되돌이  (조건부)
 │  Kx +0x13d ~       조건부 마무리 → 조각: 휘발 레지스터 복원, RSP = 트랩 프레임 + 0x168
 └─ (2) ═▶ 유저  IRETQ                                          [정적]
          KiKvaShadow 꺼짐: 조각 +0x9c (Kx +0x3e1)
          KiKvaShadow 켜짐: 조각 +0x82 JMP → KiKernelExit +0xa5
          꺼내는 값: _KTRAP_FRAME.Rip(+0x168) … SegSs(+0x188), 트랩 프레임 = InitialStack - 0x190
          ─▶ ntdll!LdrInitializeThunk   [관찰] run4 (이 Rip 값을 누가 정하나는 Q7)
```

## 확인된 것

- **`KiStartUserThread` 는 Ghidra 함수 `KxStartUserThread` 의 `+0x60` 이름표다.** [정적] (PDB 종류 none, `--where`). run4 `[관찰]` `KiStartUserThread+0x28` = **`KxStartUserThread +0x88`** (`0x1406afbf8`), 바로 앞 `+0x83` 이 `[RSP+0x10]` 의 시작 루틴을 CFG 검사 함수 거쳐 부르는 간접 호출이다. [관찰] run4 + [정적]
- **새 스레드는 `KiStartUserThread` 에 call 이 아니라 `SwapContext` 의 `RET` 로 들어온다.** [정적] `KiInitializeContextThread +0x1e8` 이 첫 스택 맨 위에 이 주소를 쌓고, `SwapContext` 끝 조각 `+0x2bf RET` 가 꺼낸다.
- **시작 루틴(`PspUserThreadStartup`) 주소가 오는 길이 끝까지 이어졌다.** [정적] `PspAllocateThread +0x681` → 시작 정보 묶음 `+0x8` → `KeInitThread +0x2fc` → `KiInitializeContextThread +0x1c7`/`+0x1cf` → 첫 스택 `[RSP+0x10]` → `Kx +0x7e`. run11·run13 의 `[추측]` 이 맞았다.
- **`KiStartUserThreadReturn` 은 `NOP; RET` 두 바이트, 실행되지 않는 "가짜 돌아올 주소"다.** [정적] (원본 바이트) `KiInitializeContextThread +0x1da` 가 `_KEXCEPTION_FRAME.Return` 칸에 쌓는다. 그래서 run4 콜스택에 `KiStartUserThread+0x28` 다음 칸으로 보였다.
- **커널 → 유저 전환은 `IRETQ` 다.** [정적] `KiKvaShadow` 에 따라 `KxStartUserThread` 조각 `+0x9c` (Kx `+0x3e1`) 또는 `KiKernelExit +0xa5`. 유저 첫 `RIP` 는 `_KTRAP_FRAME.Rip` (`+0x168`), 트랩 프레임은 `KTHREAD.InitialStack - 0x190`. 실제로 어느 `IRETQ` 를 탔는지·값은 동적 (Q42).
- **`PspUserThreadStartup` 의 정상 길은 IRQL 0 → 토큰 교환 막기 → 로케일 → TEB 스케줄링 속성 → 스레드 만들기 알림 → `PspInitializeThunkContext` → `RET`.** [정적] `PspInitializeThunkContext` 는 첫 스레드만이 아니다 (조건: `Terminated` = 0, `Minimal` = 0).
- **run13 "멈춤 APC 는 `+0x22` IRQL 내림 직후" 는 고리 3개가 `[정적]` 으로 굳었다.** 남은 건 "실제로 `+0x26` 에서 들어오나" 하나 `[추측]`.

## 정정

- run13 은 "`KiStartUserThread` 는 Ghidra 가 함수로 안 잡아서 `ms/decomp`·`ms/disassemble` 에 없다" 라고 썼다. **이름표만 함수가 아니고, 코드는 Ghidra 함수 `KxStartUserThread` 안 `+0x60` 에 있다** (이번에 `KxStartUserThread.asm` 으로 뽑힘). run13 이 원본 바이트로 읽은 내용(IRQL 1, call `+0x23` / ret `+0x28`)은 이번 `.asm` 과 같다 (`Kx +0x83` / `+0x88`). run13 문서 맨 위에 한 줄 넣었다.
- run13 은 `SwapContext` 끝 조각의 APC 요청 조건을 "(인자 CL | `[스레드+0x1e6]`)" 로 적고 CL 의 정체를 못 정했다. `.asm` 으로 보면 그 줄은 **스택 `[RSP+0x28]` 바이트**를 읽는다 (`frag:FUN_1406b686f +0x29b MOVZX AX,byte [RSP+0x28]`). 새 스레드는 이때 RSP 가 새 스택이라 그 값은 `KiInitializeContextThread +0x26f` 가 넣은 **1** 이다 [정적]. 그래서 "CL 이 `WaitIrql` 인지" 라는 질문은 필요 없어졌다.

## 아직 확인 필요

- 동적 → 대기열 **Q42** (조건은 모두 test.exe 일 때만):
  1. `KiKvaShadow` 값 — 유저로 가는 `IRETQ` 가 (가) Kx `+0x3e1` 인지 (나) `KiKernelExit +0xa5` 인지. `dd nt!KiKvaShadow L1`.
  2. `KiIrqlFlags` 값 — `+0x1d` 호출이 실제로 들어가나 (들어가도 IRQL 1 이라 효과 없음). `dd nt!KiIrqlFlags L1`.
  3. 멈춤 APC 가 정말 `PspUserThreadStartup+0x26` 에서 들어오나 — `nt!KiSchedulerApc` 입구에서 `k`, 맨 아래가 `PspUserThreadStartup+0x26` 근처인지 (Q19 의 run13 질문과 같이).
  4. `PspInitializeThunkContext` 앞뒤(`PspUserThreadStartup+0xae` / `+0xb3`)로 트랩 프레임 `Rip`/`Rsp` 칸이 어떻게 바뀌나 — `dq (poi(@$thread+0x28)-0x190)+0x168 L5`. 시작 루틴이 도는 동안 `RBP - 0x80` = `InitialStack - 0x190` 인지.
  5. 유저 진입 직전 값 — `nt!KiStartUserThread+0x28` (= `KxStartUserThread+0x88`) 에서 `dq @rbp+0xe8 L5` (`Rip, CS, EFlags, Rsp, SS`).
  6. 유저 APC 되돌이에서 `KiInitiateUserApc` 가 첫 스레드에 불리나 (`nt!KiInitiateUserApc`, 선택).
  7. `PspUserThreadStartup` 분기 값 `ThreadInserted`·`Terminated`·`Minimal` (1, 0, 0 이어야 정상).
  8. `KyStartUserThread` 템플릿이 정말 안 도나 (선택).
  - `IRETQ` 줄 자체에는 멈춤점을 걸지 않는 편이 좋다 `[추측]` — 바로 앞 `SWAPGS` 뒤라 GS 가 유저 값이고, `KiKernelExit` 는 CR3 도 바꾼 뒤다. 유저 진입 값은 5번 자리에서 읽으면 된다.
- 정적으로 더 볼 것
  - `PspInitializeThunkContext` 본문 — 트랩 프레임 `Rip` 를 `ntdll!LdrInitializeThunk` 로 정하는 곳인지, 첫 스레드만 다른 일이 있나 (Q7).
  - `KxContextToKframes` 본문 — 유저 CONTEXT → 트랩 프레임 `[추측]` (이번엔 안 뽑음).
  - `KiInitiateUserApc` (안 뽑음).

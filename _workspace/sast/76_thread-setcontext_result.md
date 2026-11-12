# 76 thread-setcontext — 정적 분석 결과

- 요청서: `_workspace/sast/76_thread-setcontext.md` (Q141e, PE_emulator 요청 25 ㅁ — 기다리는 **남의** 스레드에 문맥 넣기)
- 앞 바퀴: `73_thread-wake_result.md` (기다림 블록·깨움·`WaitStatus`·`KiTryUnwaitThread`·`0x100` 반복), `74_thread-timer_result.md` (마감 한 번 계산·`KiSwapThread +0x263` 타이머 떼기), `75_thread-exit_result.md` (`KeRequestTerminationThread`·`KiSchedulerApc` = APC 가 기다림을 깨는 다른 예). 겹치는 건 "sast/73 질문 N" 으로 가리키기만 한다.
- 모듈 / 바이너리: ntoskrnl, pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 다 있음).
- 지도: 새로 안 만들었다. 이번 대상은 이미 뽑은 함수들의 줄이 대부분이고, 새로 필요한 것은 이름이 분명한 몇 개뿐이라 이름으로 바로 뽑았다.
- 뽑은 함수 (새 **7개**, 전부 depth 0): `ms/decomp/ntoskrnl/`·`ms/disassemble/ntoskrnl/` 에 `.c`/`.asm`.
  - 요청서 대상: `KeInsertQueueApc`(1401B)
  - **요청서 범위를 넓힌 곳 (이유)**:
    1. `NtSetContextThread`(222B) — 질문 5 (A 가 돌려주는 상태값의 앞쪽 검사). `PspSetContextThreadInternal` 의 caller.
    2. `KeInitializeApc`(91B) — 질문 1 (APC 종류: `KernelRoutine`·`NormalRoutine`·`ApcMode` 가 어느 인자 자리인지).
    3. `PspSetContextState`(219B) — 질문 4(b). `KiSystemServiceStart +0x2d3` 길이 언제 열리는지 (`PspGetSetContextInternal` 의 조각 `FUN_140b2a1a6` 이 부름).
    4. `KeRundownApcQueues`(183B) — 질문 5 (B 가 끝나는 중이면). `PspExitThread +0x4e3` 에서 부름 (sast/75).
    5. `KiRequestSoftwareInterrupt`(211B)·`KiCheckForKernelApcDelivery`(240B) — `KeInsertQueueApc`/`PspSetContextThreadInternal` 이 부르는 줄만 확인용으로 뽑음. 본문은 안 읽음.
  - 이미 있던 것을 읽음: `PspSetContextThreadInternal` `KiInsertQueueApc` `KiSignalThreadForApc` `KiDeliverApc` `PspGetSetContextSpecialApc` `PspGetSetContextInternal` `PspSetContext` `KeContextToKframes` `KxContextToKframes` `KiRestoreSetContextState` `KiSystemServiceStart` `KiSwapThread` `SwapContext` `KiSwapContext` `KiProcessThreadWaitList` `KiCommitThreadWait` `KiExitThreadWait` `KeWaitForMutexObject`(= PDB `KeWaitForSingleObject`) `KeRequestTerminationThread` `KiSchedulerApc` `NtContinueEx` `PspAllocateThread`(SetContextState 잡는 줄 확인용).
  - 안 읽은 것 (이름만): CET·XSTATE (`KeVerifyContextXStateCetU` `KeVerifyContextIpForUserCet` `RtlpValidateContextXStateDisabledFeatures` `RtlCopyXStateArea` `RtlXRestoreS`) · 보안 컨텍스트 (`VslGetSetSecureContext`) · 스택 걷기 (`RtlpGetStackLimits` `RtlpCaptureContext` `RtlVirtualUnwind2` `RtlpLookupFunctionEntryForStackWalks` `RtlpIsFrameInBoundsEx`) · 디버그 레지스터 (`KiRestoreDebugRegisterState`) · WoW64 · ETW (`EtwTiLogSetContextThread` `EtwTiLogInsertQueueUserApc` `PspLogAuditSetContextThreadEvent`) · 스핀·IRQL 도우미 · `KeLeaveCriticalRegionThread` `KiFlushQueueApc` `KeForceResumeThread` · `KiSendSoftwareInterrupt`.
- 버전 확인 (알려진 사실 대조, **일치**): `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines`, sast/73 의 `KiCommitThreadWait +0x220 / ret +0x225 → KiSwapThread` 를 `_calls.tsv` 에서 다시 확인.

## 스크립트 문제

없다. 참고만:

- **구조체 칸을 누가 읽고 쓰나 (`+0x6a8` 같은 오프셋)** 는 `run_ghidra.sh refs` 로 안 된다 (전역·함수 주소용). 이번에 `ETHREAD.SetContextState (+0x6a8)` 를 쓰는 곳을 **빠짐없이** 찾아야 해서 `llvm-objdump-14 -d --x86-asm-syntax=intel ntoskrnl.exe` 전체(모든 코드 구역)를 `ms/` 밖 scratchpad 에 풀어 `[reg + 1704]` 를 grep 했다 (쓰는 곳은 `PspAllocateThread +0x2caa31` 한 곳, 풀어주는 곳은 `PspExitThread +0x85f` 와 `PspAllocateThread` 되돌림 `+0x2ca940`, 나머지 `+1704` 는 잡 객체·`Sm*`·ETW 같은 **다른 구조체**). 이런 "칸 하나를 쓰는 곳 전부" 찾기가 자주 필요하면 `refs` 에 오프셋 모드(`refs <바이너리> --field 0x6a8`)를 하나 두자는 제안 (고치는 건 오케스트레이터).
- `PspGetSetContextInternal` 은 조각 `FUN_140b2a1a6` (→ `PspSetContextState` 를 부름) 이 `.asm` 끝에 붙어 있다. 정상. `_calls.tsv` 에는 `frag:FUN_140b2a1a6+0x7` 로 나온다.
- `PspSetContextThreadInternal` `.asm` 맨 끝의 "읽지 않은 틈 `+0x307~+0x30b`" 는 예외 착지점. 흐름은 `+0x302 JMP +0x94` 로 이어진다.

## 읽는 법 (표기)

- `[정적]` = 코드에 이렇게 쓰여 있다. 실행 때 이 길로 간다는 뜻이 아니다. `[추측]` = 코드 동작에서 짐작한 이름·뜻. `[공백]` = 이번에 못 본 것.
- 칸 이름은 공개 PDB (`pdb_struct.py`) 로 확인한 것만 이름을 쓴다:
  - `_KAPC`(0x58B): `Type +0x0` · `Thread +0x8` · `ApcListEntry +0x10` · (`KernelRoutine +0x20` · `RundownRoutine +0x28` · `NormalRoutine +0x30` — 공개 PDB 는 `Reserved +0x20` 24바이트로만 보임, 이름은 `KeInitializeApc` 줄에서 읽음) · `NormalContext +0x38` · `SystemArgument1 +0x40` · `SystemArgument2 +0x48` · `ApcStateIndex +0x50` · `ApcMode +0x51` · `Inserted +0x52`.
  - `_KTHREAD`: `ThreadLock +0x40` · `WaitRegister +0x70` (비트 `Apc` 5 = `0x20`) · `MiscFlags +0x74` (`ApcQueueable` 비트 14 = `0x4000`, `SystemThread` 비트 10 = `0x400`) · `ApcState +0x98` (커널 APC 목록 머리 = `+0x98`) · `KernelApcInProgress +0xc0` · `KernelApcPending +0xc1` · `UserApcPending +0xc2` (이 셋의 이름은 sast/73 이 쓴 `ApcState.UserApcPending… +0xc2` 와 같은 칸 묶음; 공개 PDB 로 칸 이름까지는 안 맞춤 `[추측: 이름]`) · `WaitStatus +0xc8` · `State +0x184` · `WaitIrql +0x186` · `KernelApcDisable +0x1e4` · `SpecialApcDisable +0x1e6` · `PreviousMode +0x232` · `ApcStateIndex +0x24a` · `InitialStack +0x28` · `TrapFrame +0x90` · `Header` 맨 앞 dword(`+0x0`) 의 **비트 27** (= `0x8000000`, 공개 PDB 이름 없음 — `Reserved4` 칸).
  - `_KTRAP_FRAME`(0x190B): `Rax +0x30` · `Rcx +0x38` · `Rdx +0x40` · `R8 +0x48` · `R9 +0x50` · `R10 +0x58` · `R11 +0x60` · `Xmm0~5 +0x70~+0xc0` · `Rbx +0x140` · `Rdi +0x148` · `Rsi +0x150` · `Rbp +0x158` · `Rip +0x168` · `SegCs +0x170` · `EFlags +0x178` · `Rsp +0x180` · `SegSs +0x188`. `KiSystemServiceStart` 에서 `RBP = TrapFrame + 0x80` 이라 `[RBP-0x50]` = `Rax`, `[RBP+0xe8]` = `Rip`, `[RBP+0xf8]` = `EFlags`, `[RBP+0x100]` = `Rsp`.
  - `_ETHREAD`: `SetContextState +0x6a8` (`_CONTEXT*`) · `PicoContext +0x670` · `SameThreadPassiveFlags +0x5a4` (`SecureContext` 비트 9). `_EPROCESS`: `PicoContext +0x640` · `Flags2 +0x1f0` (`RestrictSetThreadContext` 비트 17).
  - `_CONTEXT` 칸 (x64, 표준): `ContextFlags +0x30` · `EFlags +0x44` · `Rax +0x78` · `Rcx +0x80` · `Rdx +0x88` · `Rbx +0x90` · `Rsp +0x98` · `Rbp +0xa0` · `Rsi +0xa8` · `Rdi +0xb0` · `R8~R11 +0xb8~+0xd0` · `R12~R15 +0xd8~+0xf0` · `Rip +0xf8`. `ContextFlags` 비트(숫자 `[정적]`, 이름 표준): `0x100001` CONTROL · `0x100002` INTEGER · `0x100008` FLOATING_POINT · `0x100010` DEBUG_REGISTERS · `0x100040` XSTATE.
- **"기다림 결과" 는 칸이 아니라 반환 레지스터로 본다** (오케스트레이터 메모): `KeWaitForSingleObject` 는 신호 상태라 안 자고 끝나는 길에서 `Thread.WaitStatus (+0xc8)` 를 **쓰지 않는다** (옛 값이 남을 수 있음). 그 길의 결과는 `R14D = 0` (`+0x1ad MOV R14D,R10D`) → `+0x2c4 MOV EAX,R14D` → `RET`. 잠들었다 깨는 길은 `KiSwapThread` 가 `WaitStatus` 칸을 읽어(`+0x25c`) `RAX` 로 돌려주고 `KiCommitThreadWait` 가 손대지 않고 넘긴다 (sast/73 질문 1(c)). 아래 "돌려받은 값" 은 모두 이 반환 레지스터 기준이다.
- **오케스트레이터 메모 반영 (질문 4(b)·(c))**: 성공한 `NtContinue` 는 `NtContinueEx` 가 자기 몸에서 `IRETQ` 한다 (`NtContinueEx +0x471 IRETQ`, 조각 `FUN_1406ae17a` 에서 `+0x3ee MOV RAX,[RBP-0x50]`) — `KiRestoreSetContextState` 의 `IRETQ` 길이 아니다. 그리고 `KiRestoreSetContextState` 는 **유저로 돌아가는 모든 출구가 "Thread Header 비트 27 이 서 있으면" 부르는 공용 갈고리**다: `KiSystemServiceStart +0x291 TEST [RCX],0x8000000 / +0x2d3` · `NtContinueEx +0x194 / +0x19c` · `KxStartUserThread +0x14f / +0x157` · `KiSystemServiceExitPico +0x83` · `KiApcInterrupt`·`KiDpcInterrupt`·`KiInterruptDispatchNoLock` (`_calls.tsv`). 이 비트를 **켜는** 곳이 질문 4(b) 의 열쇠다 (아래).

## 한눈에 (그림)

```
A: NtSetContextThread(B, ctx)
   +0x48 ObReferenceObjectByHandle(THREAD_SET_CONTEXT 0x10)
   +0x63/+0x6f/+0xa4  막는 검사 (같은 프로세스 제한 · SystemThread · Pico)         → 0xc000060a / 0xc0000008
   +0xc2 PspSetContextThreadInternal(B, ctx, mode, mode, 1)
PspSetContextThreadInternal  (A 문맥)
   +0x252 유저 CONTEXT 를 A 의 커널 스택 버퍼로 복사 (RtlpReadExtendedContext)
   +0xf7 B == 나 ?  예 → +0x131 PspGetSetContextSpecialApc 를 직접 부름
                    아니오(→ +0x264):
      +0x273 KeInitializeEvent(A스택, 알림형, 꺼짐)
      +0x2a9 KeInitializeApc(KernelRoutine=PspGetSetContextSpecialApc, NormalRoutine=0, ApcMode=커널, Env=0)   ← 특별 커널 APC
      +0x2bf KeInsertQueueApc(apc, SysArg1=1(넣기), SysArg2=B, Increment=2)
          B 의 ApcQueueable 꺼짐/이미 넣음 → FALSE → +0x33e 0xc0000001 로 끝
          B 가 State=5(Waiting) · WaitIrql=0 · SpecialApcDisable=0 · 커밋된 기다림 → 그 자리에서 깨움(WaitStatus=0x100, 신호는 안 먹음)
          → KiExitDispatcher → KiProcessThreadWaitList : B 의 블록을 객체 목록에서 떼고 준비 줄로 (AdjustIncrement=2)
      +0x2dd KeWaitForSingleObject(A 스택 이벤트, Executive, KernelMode, 비경보, 마감 없음)  ← A 잠
B: 준비 줄에서 다시 돌 차례 → SwapContext 끝에서 "커널 APC 있음" 이 AL=1 → KiSwapThread
   +0x25c WaitStatus(0x100) 를 읽어 두고 → +0x2ee KiDeliverApc → PspGetSetContextSpecialApc
   → PspGetSetContextInternal → (보통) PspSetContext : B 의 트랩 프레임 칸에 Rip·Rsp·EFlags(CONTROL), Rax…R11(INTEGER) 쓰기
   → KeSetEvent(A 의 이벤트) → A 깨움
   KiSwapThread 는 읽어 둔 0x100 을 돌려줌 → KeWaitForSingleObject +0x860 → +0x891 JMP +0x104 : 처음부터 다시(신호 검사 → 같은 마감으로 다시 잠)
B: 신호/마감으로 끝 → 반환값(EAX) → KiSystemServiceStart +0x25d 가 트랩 프레임 Rax 에 덮어씀 (이 시점에 문맥의 Rax 는 이미 덮임)
   → +0x540 RAX=[RBP-0x50], RCX=[Rip 칸], R11=[EFlags 칸], RSP=[Rsp 칸] → +0x5bb SYSRET   (새 Rip 로 감)
```

## 질문별 답 [정적]

### 1. A 쪽 — `PspSetContextThreadInternal` (834B)

**인자와 하는 일.** `NtSetContextThread` 가 `PspSetContextThreadInternal(RCX=B, RDX=ctx, R8B=PreviousMode, R9B=PreviousMode, [RSP+0x20]=1)` 로 부른다 (`NtSetContextThread +0xbc~+0xc2 / ret +0xc7`, `+0xb1 MOV [RSP+0x20],1`). 안에서 `R12`=유저 호출 표지, `R15`=모드, `R13`=대상 B, `R14`=지금 스레드(A) (`+0x40`·`+0x3c`·`+0x47`·`+0x73`). 지역 구조 하나 (`RBP+0x10`, 이하 "꾸러미") 를 만든다: 앞 `0x58` 바이트가 `KAPC` (`+0x5a / ret +0x5f memset`), 그 뒤 `+0x59` 비트 모음 · `+0x5c` 상태값 · `+0x60` 이벤트 · `+0x78` 컨텍스트 포인터 · `+0x80` 포인터 표 (`+0x6b / ret +0x70 memset 0x127`; 칸 위치는 `PspGetSetContextInternal` 이 읽는 줄에서 맞춤).

- `+0x9c / ret +0xa1 RtlpSanitizeContextFlags` (실패면 `+0xa3 JS +0x169` 로 그 코드 반환). 유저 호출 (`+0xa9 TEST R12B / JNZ +0x1cb`) 이면 **유저 CONTEXT 를 A 의 커널 스택 버퍼로 복사**한다: `+0x1d4 RtlGetExtendedContextLength` → `+0x1f8 __chkstk` → `+0x220 RtlInitializeExtendedContext` → `+0x252 / ret +0x257 RtlpReadExtendedContext`, 그리고 `+0x238 MOV [RBP+0x88],RAX` (꾸러미의 컨텍스트 포인터 = 이 커널 버퍼). → **A 가 끝까지 기다려야 하는 까닭**: APC 도, 입력 CONTEXT 도, 상태값 칸도 A 의 커널 스택 위에 있다 (아래).
- 대상이 `SystemThread` (`MiscFlags` `0x400`) 이고 유저 호출이면 `+0xbe TEST [R13+0x74],0x400 / +0xc6 JNZ +0x1c4 → EBX=0xc0000030` (이름 `[공백]`).
- **남이면**: `+0xf7 CMP R13,R14 / +0xfa JNZ +0x264`. 자기면 `+0x100 [RBP+0x50]=1`, `+0x112 DEC word [R14+0x1e6]` (SpecialApcDisable −1) 하고 `+0x131 / ret +0x136 PspGetSetContextSpecialApc` 직접 (run16) — 이번 범위 밖.
- **APC 를 거는 줄 (남)**:
  - `+0x264 OR BL,1 / +0x267 MOV [RBP+0x69],BL` — 꾸러미 `+0x59` 비트 0 = "남이라 이벤트가 있다" `[추측: 이름]`.
  - **`+0x273 / ret +0x278 KeInitializeEvent(RCX=꾸러미+0x60, EDX=0, R8D=0)`** — `EDX=0` 알림형(수동 재설정), `R8D=0` 꺼진 채로.
  - **`+0x2a9 / ret +0x2ae KeInitializeApc(RCX=KAPC, RDX=B, R8=0, R9=PspGetSetContextSpecialApc, [RSP+0x20]=0, [RSP+0x28]=0, [RSP+0x30]=0, [RSP+0x38]=0)`**. `KeInitializeApc` 의 인자 자리 (`KeInitializeApc.asm`: `+0x1f [RCX+0x50]=R8B` 환경, `+0x3b [RCX+0x20]=R9` KernelRoutine, `+0x26 [RCX+0x28]=` Rundown, `+0x3f [RCX+0x30]=` NormalRoutine, `+0x4b [RCX+0x51]=` ApcMode) 와 맞추면: **KernelRoutine = `PspGetSetContextSpecialApc` · RundownRoutine = 0 · NormalRoutine = 0 · ApcMode = 0(커널) · 환경 0(원래 환경)**. 곧 **특별 커널 APC** (NormalRoutine 이 없는 커널 모드 APC). `KeInitializeApc +0x5 [RCX]=0x12` (`Type`) · `+0x8 [RCX+0x2]=0x58` (`Size`).
  - **`+0x2ae MOV R9D,2 / +0x2b4 MOV R8,R13 / +0x2b7 LEA EDX,[R9-1] / +0x2bf / ret +0x2c4 KeInsertQueueApc`** → `KeInsertQueueApc(Apc, SystemArgument1=1, SystemArgument2=B, PriorityIncrement=2)`. `SysArg1=1` 이 "넣기(SET)", 0 이면 "읽기(GET)" (`PspGetSetContextInternal +0x2c0 CMP [RBP-0x78],R13 / +0x2c4 JNZ +0x370`). **우선순위 올려 주기: 예, 2** — 이 `2` 가 `KeInsertQueueApc +0x25c MOV R9D,R13D` 로 `KiExitDispatcher` 의 `Increment` 가 되고 B 의 `AdjustIncrement(+0x237)` 에 적힌다 (질문 2).
  - 돌려받은 값이 0 이면 (`+0x2c4 TEST AL,AL / +0x2c6 JZ +0x33e`) → **`+0x33e MOV EBX,0xc0000001`** (STATUS_UNSUCCESSFUL) 로 끝. 기다리지 않는다.
- **A 가 기다리는 줄**: **`+0x2dd / ret +0x2e2 KeWaitForSingleObject(RCX=꾸러미+0x60, EDX=0, R8D=0, R9D=0, [RSP+0x20]=0)`** — 객체=그 이벤트, `WaitReason 0`(Executive), `WaitMode 0`(커널), **경보 가능 아님** (`R9D=0`), **마감 없음** (`[RSP+0x20]=0` = Timeout NULL). 반환값은 안 본다 (`+0x2e2 JMP +0x154`). 이벤트는 A 의 커널 스택 위 (`RBP+0x70`).
- **B 가 영영 안 끝나면 A 는 계속 기다리나 — 예.** 마감이 없고 경보도 안 받는다 (A 는 유저 APC 로도 못 깨난다). 깨우는 것은 B 가 APC 안에서 부르는 `PspGetSetContextSpecialApc +0x86 KeSetEvent` 하나뿐 (질문 3·5).
- 끝: `+0x154 MOV EBX,[RBP+0x6c]` (꾸러미 `+0x5c`, B 의 APC 가 써 놓은 상태값) → `+0x15b JS +0x167` (음수면 그대로 반환) → `+0x15d CMP R12B,1 / +0x161 JZ +0x30c` → 유저 호출이면 `+0x316~+0x334 / ret +0x339 EtwTiLogSetContextThread` (이름만) → 반환.

**지금 우리 (PE_emulator):** 남이면 "B 에게 문맥을 넣어 두고 A 는 바로 돌아옴" 이라면 **틀리다** — 진짜는 B 가 APC 를 돌릴 때까지 A 가 (마감 없이) 잔다. "A 도 기다림" 이면 **맞다.**

### 2. APC 가 B 의 기다림을 깨는가 — `KeInsertQueueApc` (= 이 경우 `KiSignalThreadForApc` 와 같은 규칙)

**답. 예, 그 자리에서 깨운다 (B 가 PASSIVE_LEVEL 에서 커밋된 기다림 중일 때). 단 `KiTryUnwaitThread` 를 부르지 않는다 — 같은 일을 `KeInsertQueueApc` 자신이 몸에 풀어 넣었다.** 깨운 이유 값은 **`0x100` (`STATUS_KERNEL_APC`)** 로 직접 쓴다.

`KeInsertQueueApc.asm`:

- 시작: `+0x8d LEA RAX,[0x140ab1f40] / +0x94 CMP [RDI+0x20],RAX / +0x98 JZ +0x4fe` — KernelRoutine 이 어떤 특정 루틴 (`0x140ab1f40`, 이름 `[공백]`) 이면 다른 길. 우리 APC (`0x1406b7180`) 는 아니다.
- `+0xe4 MOV R15,CR8 / +0xed MOV CR8,2` (IRQL 2), `+0x110 BTS.LOCK [RBX+0x40],0` (RBX = **B** 의 `ThreadLock` 스핀).
- **넣어도 되나**: `+0x134 TEST dword [RBX+0x74],0x4000 / +0x13b JZ +0x394` (B 의 `ApcQueueable` 꺼짐 → 반환 FALSE) · `+0x141 CMP byte [RDI+0x52],0 / +0x145 JNZ +0x394` (`Inserted` 이미 켜짐 → FALSE). 아니면 `+0x14b~+0x166` 에서 `SystemArgument1/2` 를 `[RDI+0x40]/[RDI+0x48]` 에 적고 `Inserted=1`.
- **`+0x16a / ret +0x16f KiInsertQueueApc(Apc)`**: B 의 커널 APC 목록 (`ApcState +0x98 + ApcMode×0x10`) 에 끼워 넣는다. NormalRoutine 이 0 이면 (`KiInsertQueueApc +0x28 CMP [RCX+0x30],0 / +0x36 JZ +0x5e`) 특별 APC 라서 목록에서 **일반 커널 APC 보다 앞**, 이미 있는 특별 APC 뒤 (`+0x5e~+0x7c` 가 `[…+0x20]==0` 인 항목을 건너뜀 `[추측: 순서 뜻]`). `ApcStateIndex` 가 0 (원래 환경) 이고 B 가 다른 프로세스에 붙어 있으면(`[RDX+0x24a]≠0`) 저장된 목록 (`+0x258`) 에 들어가고 깨우지 않는다 (`KeInsertQueueApc +0x184 JNZ +0x24f` 건너뜀; 줄기 밖).
- **깨우는 줄** (커널 APC, `ApcMode`=0):
  - `+0x173~+0x18e` `ApcStateIndex` 비교, `+0x18a CMP RSI,[R14+8] / +0x18e JZ +0x296` (대상이 **지금 CPU 에서 도는 나 자신**이면 다른 길 — 우리는 남이므로 아님), `+0x194 TEST DL,DL / +0x196 JNZ +0x408` (유저 모드 APC 길, 아님).
  - **`+0x19c MOV byte [RSI+0xc1],1`** (B 의 `KernelApcPending = 1`) → `+0x1a3 OR.LOCK [RSP],0` (메모리 장벽).
  - **`+0x1a8 MOVZX EAX,[RSI+0x184] / +0x1af CMP AL,2 / +0x1b1 JZ +0x476`** — 이미 **Running(2)** 이면 소프트웨어 인터럽트만 요청 (`+0x498 / ret +0x49f KiRequestSoftwareInterrupt` 또는 다른 CPU 면 `+0x48e KiSendSoftwareInterrupt`). **`+0x1b7 CMP AL,5 / +0x1b9 JNZ +0x24f`** — Waiting(5) 이 아니면 아무것도 안 하고 끝 (`KernelApcPending` 만 서 있음).
  - **(a) 깨우기 조건** (5 일 때):
    | 줄 | 검사 | 아니면 |
    |---|---|---|
    | `+0x1bf CMP [RSI+0x186],DL / +0x1c5 JNZ +0x24f` | `WaitIrql == ApcMode(0)` = **B 가 PASSIVE_LEVEL 에서 기다리는 중** (`WaitIrql` 은 `KeWaitForSingleObject +0x6b2 LEA R13,[RDI+0x186]` 칸) | 안 깨움 |
    | `+0x1cb CMP word [RSI+0x1e6],0 / +0x1d3 JNZ +0x24f` | `SpecialApcDisable == 0` | 안 깨움 |
    | `+0x1d5 CMP [RDI+0x30],0 / +0x1da JNZ +0x39c` | **`NormalRoutine == 0` 이면 아래 두 검사를 건너뜀 (특별 APC).** 있으면 `+0x39c CMP word [RSI+0x1e4],0` (`KernelApcDisable==0`) 와 `+0x3aa CMP byte [RSI+0xc0],0` (`KernelApcInProgress==0`) 도 필요 | 안 깨움 |
    | `+0x1e0 MOVZX EAX,[RSI+0x70] / +0x1e4 AND EAX,7 / +0x1e7 CMP EAX,1` → `+0x1ea JNZ +0x4de (CMP EAX,4 / +0x4e1 JNZ +0x24b)` | `WaitRegister.State` 가 **1(커밋됨) 또는 4** (sast/73 질문 3 의 표와 같은 값) | 그 밖(0 커밋 전·2·3·5…)은 **`+0x24b OR byte [RSI+0x70],0x20` 만** (= `WaitRegister.Apc` 비트, 질문 2(b)) |
  - **깨우는 일 (State 1·4)**: `+0x1f0 MOV RDX,[RSI+0xe8]` 큐가 있으면 동시성 카운트 +1 (`+0x203 / +0x209`) · **`+0x210 / ret +0x215 KiRemovePrcbWaitEntry(B)`** (대기 목록에서 뗌) · **`+0x218 / ret +0x21d KiEnterDeferredReadyState(B)`** (State=7, sast/73) · **`+0x21d~+0x22e`** `B+0xd8` 를 `PRCB+0x2d08` 목록에 push · **`+0x235 MOV qword [RSI+0xc8],0x100`** (`WaitStatus = 0x100`) · `+0x240 MOV [RSI+0x3d0],0` (`AbWaitObject`) · `+0x24b OR byte [RSI+0x70],0x20`.
  - 끝: `+0x24f MOV DIL,1` (반환 TRUE) → `+0x254 MOV [RBX+0x40],0` (B 의 스레드 락 풀기) → `+0x25c MOV R9D,R13D` (Increment=2) · `+0x267 LEA R8D,[RDX+1]` (=1) → **`+0x26b / ret +0x270 KiExitDispatcher(Prcb, 0, 1, 2, 옛 IRQL)`** → (sast/73 질문 2(c)) `KiProcessThreadWaitList(Prcb, 1, Increment)`:
    - **B 의 기다림 블록을 객체(이벤트) 목록에서 뗀다**: `KiProcessThreadWaitList +0x62~+0x7a` 블록마다 `BlockState < 5` 이면 `+0x141` 로 → `+0x148 BTS.LOCK [R12],7` (객체 헤더 락) → **`+0x155 CMP byte [RSI+0x11],4 / +0x159 JNZ +0x260`** (4 인 것만) → `+0x15f~+0x16f` `RemoveEntryList` → `+0x260 AND.LOCK [R12],~0x80`. `KeInsertQueueApc` 는 블록의 `BlockState` 를 안 바꾸므로 (신호가 깨울 때 `KiTryUnwaitThread` 가 4→5 로 올리는 것과 다르다) **깨우는 쪽이 아니라 이 함수가 뗀다.** 뗀 뒤 이벤트는 B 를 모른다.
    - `+0x98 [R13+0x236]=1` (`AdjustReason`) · `+0x9f [R13+0x237]=2` (`AdjustIncrement`) · `+0x101 / ret +0x106 KiDeferredReadySingleThread` (sast/71 질문 3).
- **신호는 안 먹는다**: 이 길에는 이벤트 `SignalState` 를 건드리는 줄이 없다. B 의 블록만 떼고 B 는 `WaitStatus=0x100` 으로 준비 줄에 간다.
- **`KiSignalThreadForApc` 도 같은 몸이다** (`KiSignalThreadForApc +0x3c MOV [RBX+0xc1],1` · `+0x69 CMP [RBX+0x186],CL` · `+0x75 CMP word [RBX+0x1e6],0` · `+0x83 CMP [RDX+0x30],0` · `+0x8e~+0x98` State 1/4 · `+0xbe KiRemovePrcbWaitEntry` · `+0xfc State=7` · `+0x103~+0x114 push` · **`+0x11b MOV [RBX+0xc8],0x100`** · `+0x131 OR [RBX+0x70],0x20`). 차이는 인자 `R9B` 비트 0 (`+0x57 TEST R9B,1 / +0x5b JNZ +0x135` → 깨우지 않음) 뿐. sast/75 의 `KeRequestTerminationThread +0x89` · `KiSchedulerApc +0x201` 이 둘 다 `R9D=0` 으로 부르므로 (`KeRequestTerminationThread +0x7d XOR R9D,R9D`, `KiSchedulerApc +0x1f5 XOR R9D,R9D`) **같은 조건으로 같은 `0x100` 을 쓰고 기다림을 깬다.** → 스레드 끝내기 APC 와 문맥 넣기 APC 는 **같은 규칙 하나**로 모델링하면 된다.

**(b) 경쟁 — B 가 이미 다른 이유로 깨어나고 있으면:**
- 신호가 먼저 B 를 깨웠으면 (`KeSetEvent` 가 `KiTryUnwaitThread` 로 State 5→7, `WaitStatus=0`): 이 함수는 `+0x1b7 CMP AL,5 / +0x1b9 JNZ +0x24f` 로 **아무것도 안 하고 TRUE 반환** (B 의 `KernelApcPending` 은 `+0x19c` 에서 이미 1). B 는 신호 결과 (0) 로 깨고, 돌아오는 길에 APC 를 돌린다 (질문 3(a)).
- 반대로 이 APC 가 먼저 B 를 State=7 로 만든 뒤 신호가 오면: `KeSetEvent` 는 B 의 블록에서 `KiTryUnwaitThread` 의 `State≠5` 갈래 (sast/73 질문 3 표: `+0x57~+0x60 JNZ +0x186`) 로 지나치고 **자동 이벤트는 `SignalState` 를 안 줄이고 다음 블록으로** (sast/73 질문 2(a)). 신호는 이벤트에 남아 B 가 다시 검사할 때 가져간다.
- 커밋 전 (`WaitRegister.State==0`) 이면 `Apc` 비트(`0x20`)만 켠다 → `KiCommitThreadWait +0x97 MOV AL,[RBX+0x70] / +0xa1 TEST AL,0xe7 / +0xa3 JNZ +0x251` 에서 **안 자고** `+0x259 JZ +0x435` → `+0x435 TEST AL,0x20 / +0x437 JNZ +0x4fa` → **`+0x4fa MOV [RBX+0xc8],0x100`** (sast/73 질문 1(c)). 이 길은 `+0x2de / ret +0x2e3 KiExitThreadWait` 로 나가고 거기서 `KiExitThreadWait +0x242 / ret +0x247 KiDeliverApc` 를 부르는 줄이 있다 (`_calls.tsv`; 갈림 조건 `+0x2d~+0x3c`, `+0x20b` 은 안 풀음 `[공백]`).
- B 가 `KeWaitForSingleObject` 에 **들어오기 전에** `KernelApcPending` 이 이미 1 이면: `KeWaitForMutexObject +0x154 CMP byte [RDI+0xc1],0 / +0x15b JNZ +0x6cc` → `+0x6cc CMP word [RDI+0x1e6],0 / JNZ +0x161` · `+0x6da CMP R15B,1 / +0x6de JNC +0x161` (WaitIrql<1 이어야) → `+0x6e6 MOV [RDI+0x40],0` → IRQL 낮춤 → **`+0x708 / ret +0x70d KiDeliverApc(0,0,0)`** → `+0x72e JMP +0x112` (다시 시작). 곧 "아직 안 잔" B 에게도 APC 는 기다림 시작 때 즉시 배달된다 (이 경우 `KeInsertQueueApc` 는 State≠5 라서 안 깨움).

**지금 우리 (PE_emulator):**
- "B 의 `Rip` 만 바꿔 두고 기다림은 이어짐" — 모양은 **반쯤 맞다.** 진짜는 B 의 기다림을 **한 번 끝내고** (객체 목록에서 블록을 떼고 `WaitStatus=0x100`, 준비 줄로) B 가 APC 를 돌린 뒤 **처음부터 다시** 기다린다 (질문 3(c)). 이 사이 B 는 객체의 대기 목록에 없다.
- 깨우는 조건 (`State==Waiting` · `WaitIrql==0` · `SpecialApcDisable==0` · 커밋됨) 이 없으면 **모자라다.**

### 3. B 쪽 — 깨난 뒤

**(a) `0x100` 이면 다시 기다리기 전에 APC 를 배달하나 — 예. 길은 `KiApcInterrupt` 가 아니라 `KiSwapThread` 가 직접 `KiDeliverApc` 를 부르는 것.**

- B 가 준비 줄에서 뽑혀 돌아올 때, 문맥 전환 끝 (`SwapContext` 꼬리) 이 **AL 에 "커널 APC 를 지금 배달해도 된다" 를 넣어 준다**: `SwapContext +0x5c1 CMP byte [RSI+0xc1],1` (`KernelApcPending==1`) → `+0x5ca MOVZX AX,byte [RSP+0x28]` · `+0x5d0 OR AX,word [RSI+0x1e6]` (옛 IRQL 과 `SpecialApcDisable` 둘 다 0 이어야) → `+0x5d7 JZ +0x5e6` → `+0x5e6 SETZ AL`. 하나라도 0 이 아니면 `+0x5de HalRequestSoftwareInterrupt(1)` 으로 APC 인터럽트를 요청하고 AL=0. (`KiSwapContext +0x71 / ret +0x76 SwapContext` 가 AL 을 그대로 돌려줌.)
- `KiSwapThread` (B 의 프레임이 다시 이어짐): `+0x243 / ret +0x248 KiSwapContext` → `+0x24b MOVZX ESI,AL` → **`+0x25c MOV RDI,[R13+0xc8]` (WaitStatus=0x100 을 읽어 둠 — APC 를 돌리기 *전*)** → `+0x263 BTR [R13+0x74],9 / +0x269 JC +0x369` (`TimerActive` 였으면 `KiCancelTimer`, sast/74 질문 4) → `+0x274 TEST SIL,SIL / +0x277 JNZ +0x2ca` → `+0x2ca~+0x2e3` IRQL 을 1(APC_LEVEL)로 → **`+0x2ee / ret +0x2f3 KiDeliverApc(ECX=0 커널, EDX=0, R8D=0)`** → `+0x2f3 JMP +0x279` → `+0x28e MOV CR8,RBX` (WaitIrql 복원) → `+0x29a MOV RAX,RDI` (읽어 둔 `0x100`) → `+0x2ac RET`.
- `KiDeliverApc` (특별 커널 APC 부르는 줄): `+0xa5 LEA R14,[RBX+0x98]` · `+0xb2 CMP [R14],R14 / +0xb5 JZ` (목록 비면 끝) · `+0xbf MOV CR8,R10(2)` · `+0xd3 BTS.LOCK [RBX+0x40],0` · `+0x10a LEA RSI,[RDX-0x10]` (KAPC) · `+0x111 MOV RCX,[RSI+0x30]` (**NormalRoutine**) · `+0x115 MOV R12,[RSI+0x20]` (KernelRoutine) · `+0x135 TEST RCX,RCX / +0x138 JZ +0x2b1` → **NormalRoutine 이 0 이면 특별 APC 길**: `+0x2b1~+0x2c1` 목록에서 뗌 (`+0x4c8~+0x4d2`, `Inserted=0` `+0x4d2`) → `+0x4d6` 스레드 락 풀기 → `+0x4ee MOV EAX,1 / MOV CR8,RAX` (APC_LEVEL) → **`+0x4f7 OR byte [RBX+0xc0],2`** → `+0x4fe LEA RAX,[0x140214750]`(`IopCompleteRequest`) / `+0x514 CMP R12,RAX / +0x517 JNZ +0x14030a187(=+0x657)` → 그 밖의 KernelRoutine 은 알려진 루틴 몇 개와 비교한 뒤 (`+0x657` 부터 `IopCompletePageWrite` `+0x670` · `KiUpdateTebApc` `+0x698` · `EtwpCovSampCaptureApc` `+0x6bb` · `PspUserApcReserveKernelRoutine` `+0x6da` · `MiStoreWriteModifiedCompleteApc` `+0x72c` 등) 마지막에 **`guard_dispatch_icall`** (`+0x76c MOV RAX,R12 / +0x76f CALL 0x1406b77f0`, 간접 호출) 로 `PspGetSetContextSpecialApc(Apc, &Normal, &NormalContext, &SysArg1, &SysArg2)` → `+0x52b AND byte [RBX+0xc0],0xfd` → `+0x535 JMP +0xac` (목록 다시 봄). 어느 APC 인지는 정적으로 못 정함 — 간접 호출 `[공백→동적]`.
- 그 밖에 이 APC 를 배달하는 자리 (이름만): `KiExitDispatcher +0xa2` · `KiExitThreadWait +0x242` (커밋 전 깨움) · `KeWaitForMutexObject +0x708`·`+0xb82`·`+0xefc` · `KiApcInterrupt` (실제 APC 인터럽트) · `KiCheckForKernelApcDelivery +0x2d` (임계 영역 나올 때) · `KiCheckForThreadDispatch +0x61`.

**(b) APC 안이 B 의 트랩 프레임을 바꾸는 칸 — `Rip`·`Rsp`·`Rax` 모두 바꾼다. 단 `Rax` 는 `CONTEXT_INTEGER` 일 때만, 그리고 syscall 출구가 곧 덮어쓴다 (질문 4(b)).**

- 길: `PspGetSetContextSpecialApc +0x59 MOV RDX,[R9]` (SysArg1=1) · `+0x5c LEA R8,[RSP+0xc0]` · **`+0x64 / ret +0x69 PspGetSetContextInternal(RCX=꾸러미, RDX=1, R8=&이벤트칸)`** → `+0x69 TEST RAX,RAX / +0x6e FXRSTOR [RAX]` (돌려받은 포인터가 있을 때만) → `+0x71 MOV RCX,[RSP+0xc0]` 가 0 이 아니면 **`+0x7e MOV EDX,1 / +0x83 XOR R8,R8 / +0x86 / ret +0x8b KeSetEvent(이벤트, 1, FALSE)`** (A 를 깨움).
- **`KeContextToKframes` 는 이 길이 아니다.** `PspGetSetContextInternal` 은 `KeContextToKframes`/`KxContextToKframes` 를 안 부른다 (`_calls.tsv`: 부르는 것은 `RtlpGetStackLimits` `RtlpCaptureContext` `RtlVirtualUnwind2` `PspGetContext` **`PspSetContext`** `KeVerifyContext*` `RtlGetExtendedContextLength` `VslGetSetSecureContext` `RtlCopyContext` 와 조각의 `PspSetContextState`). `KeContextToKframes` 의 caller 는 `KiRestoreSetContextState +0x90`·`KiContinueEx +0x1e0`·`KiContinuePreviousModeUser +0x422` (run16 목록과 다르다 — 정정).
- `PspGetSetContextInternal` (B 문맥, `R15`=B, `R14`=꾸러미, `RDI`=트랩 프레임):
  - `+0x85 MOV R12D,[RAX+0x30]` (컨텍스트의 `ContextFlags`) · `+0x89 CMP byte [R14+0x58],0 / +0x8d JZ +0x5c3` (커널 호출은 `+0x5c3 MOV RDI,[R15+0x90]` = 지금 `TrapFrame`) · `+0x93 TEST [R15+0x5a4],0x200 / +0x9e JNZ +0x5e6` (`SecureContext` 면 `VslGetSetSecureContext`; 이름만).
  - **유저 호출: `+0xa4 MOV RDI,[R15+0x28]` (InitialStack) / `+0xb2 ADD RDI,-0x190`** = B 의 **맨 아래 트랩 프레임** (syscall 로 들어온 그 프레임). (`+0xa8 TEST [RDI+8],1 / +0xac JNZ +0x360` 루프는 뜻 `[공백]`.)
  - **`+0xb9 TEST byte [R14+0x59],2 / +0xbe JZ +0x3f4`**: 꾸러미 비트 1 은 `NtSetContextThread` 가 5번째 인자 `1` 로 켠다 (`+0xee ADD BL,BL`). 켜져 있으면 **스택을 거슬러 올라가며 비휘발 레지스터가 어디 저장돼 있는지 찾는다**: `+0xcc RtlpGetStackLimits` · `+0x104 RtlpCaptureContext` · 포인터 표 `[RSI+0x30~+0xf8]` 를 캡처한 문맥 칸으로 채움 (`+0x110~+0x1ee`) · 루프 `+0x200~+0x2b3` (`+0x237 RtlpLookupFunctionEntryForStackWalks`, **`+0x298 RtlVirtualUnwind2`** 의 마지막 인자 `[RSP+0x40]=RSI` 가 "비휘발 레지스터 포인터 표") · `+0x2af CMP [RBP-0x60],RDI / +0x2b3 JNZ +0x200` (트랩 프레임에 닿을 때까지 `[추측: 닿는 조건]`). 곧 **B 가 `KeWaitForSingleObject` 등 커널 프레임 안에서 자는 동안 B 의 유저 비휘발 레지스터 (`Rbx` `Rbp` `Rsi` `Rdi` `R12~R15` `Xmm6~15`) 는 커널 스택 프레임에 흩어져 저장돼 있는데 그 자리를 찾아 둔다.** 꺼져 있으면 (`+0x3f4`) 고정 위치 (`InitialStack-0x1d0` …) 를 쓴다 (끼어든 문맥 `[추측]`).
  - 갈림: `+0x2b9 AND R12D,0x100008` · **`+0x2c0 CMP [RBP-0x78],R13 / +0x2c4 JNZ +0x370`** — SysArg1≠0 이면 SET 길 (`+0x370`), 0 이면 GET (`+0x2ee / ret +0x2f3 PspGetContext`).
  - SET 길 (`+0x370~`): 검증 (`+0x37d~+0x3cc`: `KeVerifyContextXStateCetU` `+0x397`, `KeVerifyContextIpForUserCet` `+0x587`, `RtlpValidateContextXStateDisabledFeatures` `+0x3c5` — 이름만; 실패면 그 상태를 `[꾸러미+0x5c]` 로). 그다음 **`+0x3d2 CMP [R15+0x6a8],0 / +0x3da JNZ +0x610`** (B 의 `SetContextState` 가 있나):
    - **없으면** (아래 4(b) 에서 이 쪽이 보통): **`+0x3e0 MOV R8,[R14+0x78] / +0x3e4 MOV RDX,RSI / +0x3e7 MOV RCX,RDI / +0x3ea / ret +0x3ef PspSetContext(트랩프레임, 포인터표, 컨텍스트, 모드)`** → `+0x3ef JMP +0x2f3`. 곧 **B 의 트랩 프레임을 그 자리에서 고친다.**
    - 있으면 `+0x610 CMP R9B,1 / +0x614 JNZ +0x3e0` · `+0x61a TEST [R14+0x59],2 / +0x61f JNZ`(조각) · `+0x625 TEST byte [R15+3],8` → **`PspSetContextState`** (조각 `FUN_140b2a1a6 +0x7 / ret +0xc`) 로 미룬다 (4(b)).
  - 끝: `+0x2f8 XOR EAX,EAX`(성공 0) → `+0x302 MOV RCX,R14 / +0x305 MOV [RCX+0x5c],EAX` (**상태값을 꾸러미에 씀**; 실패 길 `+0x4ff~+0x509` 는 `EAX=0xc0000001` 로 `+0x2fa` 에 합류해 `XOR` 를 건너뜀) → `+0x30b TEST byte [R14+0x59],1 / +0x328 JZ +0x34f` → `+0x32a LEA RCX,[R14+0x60] / +0x32e MOV [RDI],RCX` (**남이면 이벤트 주소를 `PspGetSetContextSpecialApc` 에 돌려줌**; 아니면 0).
- **`PspSetContext(RCX=트랩 프레임 RDI, RDX=포인터 표, R8=컨텍스트 RBX, R9B=모드)` 가 쓰는 트랩 프레임 칸** (`PspSetContext.asm`):
  | 컨텍스트 비트 | 줄 | 쓰는 칸 |
  |---|---|---|
  | `CONTROL` (`+0x28 AND EAX,0x100001 / CMP / +0x35 JNZ +0xc2`) | `+0x3b~+0x5a` | `EFlags` → **트랩 `+0x178`** (유저 모드는 `& 0x210dd5` 뒤 IF 켬 `BTS 9`) |
  | | `+0x61~+0x68` | `Rip` ← 컨텍스트 `+0xf8` → **트랩 `+0x168`** |
  | | `+0x6f~+0x76` | `Rsp` ← 컨텍스트 `+0x98` → **트랩 `+0x180`** |
  | | `+0x87~+0xa2` | `SegSs=0x2b` (`+0x8c`), `SegCs = 0x33`(컨텍스트 `SegCs==0x33`) 또는 `0x23` → **트랩 `+0x188`/`+0x170`**. `+0xa9 CMP AX,0x23 / +0xad JZ +0x3c4` → 32비트 코드면 `Rip` 를 `MOV ECX,ECX` 로 32비트에 자름, 아니면 `+0xb3 SHL RCX,0x10 / SAR RCX,0x10` 으로 48비트 부호확장 |
  | `INTEGER` (**`+0xc4 AND EAX,0x100002 / +0xc9 CMP / +0xce JNZ +0x1a6`**) | **`+0xd4 MOV RAX,[RBX+0x78] / +0xd8 MOV [RDI+0x30],RAX`** | **`Rax` → 트랩 `+0x30`** (그리고 `+0xdc~+0x10f` `Rcx +0x38` `Rdx +0x40` `R8 +0x48` `R9 +0x50` `R10 +0x58` `R11 +0x60`) |
  | | `+0x11e~+0x1a3` | `Rbx` `Rbp` `Rsi` `Rdi` `R12~R15` 는 트랩이 아니라 **포인터 표가 가리키는 커널 스택 칸** (`MOV RCX,[RBP+0x98] / MOV [RCX],RAX` …) |
  | `FLOATING_POINT` (`+0x1ba AND EAX,0x100008 / +0x1c4 JNZ +0x2ce`) | `+0x1ca~+0x2c7` | `Xmm0~5` → 트랩 `+0x70~+0xc0`, `Xmm6~15` → 포인터 표, `MxCsr` → 트랩 `+0x2c` |
  | `DEBUG_REGISTERS` (`+0x2d3 AND ESI,0x100010`) | `+0x2f2~+0x35b` | 트랩 `Dr0~Dr3·Dr6·Dr7` — 이름만 |
  - `CONTEXT_INTEGER` 가 아니면 `Rax` 칸은 **안 건드린다** (`+0xce JNZ +0x1a6` 로 건너뜀) — 요청서 질문의 "`CONTEXT_INTEGER` 일 때만 `Rax`?" → **예.**
  - 같은 모양의 쓰기가 `KxContextToKframes +0xd9~+0x11f` (INTEGER → `Rax +0x30` …) 에도 있다. 이쪽은 4(b) 의 미룬 길에서 쓰인다.

**(c) APC 가 끝나면 B 는 다시 기다림으로 돌아가나 — 예, 처음부터 다시 검사하고, 같은 객체·같은 (절대) 마감으로 다시 잔다.**

- `KiSwapThread` 가 `0x100` 을 돌려줌 (위 (a)) → `KiCommitThreadWait +0x225 MOV R15,RAX … +0x23b RET` (sast/73) → `KeWaitForMutexObject +0x846 MOV R14,RAX` → `+0x859 MOV [RDI+0x3d0],R10(0)` → **`+0x860 CMP R14D,0x100 / +0x867 JNZ +0x2c4`** 안 탐 → `+0x86d MOV RSI,CR8 / +0x876 MOV CR8,2` → `+0x88d MOV [R13],SIL` (WaitIrql 복원) → **`+0x891 JMP +0x104`** (sast/73 질문 1(c)). 같은 객체 (`RCX`→`R12`, 인자는 스택에 그대로).
- `+0x104~+0x1ad`: 다시 `AND [RDI+0x74],~0x10` · `[RDI+0x70]=0` · 스레드 락 · **`+0x154 CMP byte [RDI+0xc1],0`** (`KernelApcPending` — APC 를 이미 다 돌렸으니 0) · `+0x1a6 MOV [RDI+0x184],5` … 이어서 **신호 검사가 마감 검사보다 먼저** (sast/74 질문 1(c)) — 이벤트가 그 사이 신호가 됐으면 `+0x5f0` 에서 **가져가고** (자동 이벤트 `SignalState=0`) `R14D=0` 으로 끝.
- **상대 마감은 새로 늘어나지 않는다**: `[RBP-0x51]` 를 `+0x3b6~+0x3d7` 에서 한 번만 계산하고 `+0x3de JMP +0x104` (반복 지점 *아래*) 라서, 반복해도 `InterruptTime + |Timeout|` 이 처음 값 그대로다 (sast/74 질문 1(b)). `KiSwapThread +0x263 BTR TimerActive` 가 타이머를 뗐다가 (sast/74 질문 4) 다시 `KiCommitThreadWait +0x23d` 에서 같은 `DueTime` 으로 건다. APC 가 오래 걸려 마감이 지났으면 신호 검사 뒤 `+0x359 JA +0x240` 으로 `0x102` (STATUS_TIMEOUT) 로 끝난다.
- 곧 "남은 시간으로 다시 계산" 이 아니라 **처음 계산해 둔 절대 마감 그대로** 다. 절대 마감 (양수 Timeout) 은 `KiComputeDueTime` (sast/74 질문 1(b)) 이 매번 시스템 시각 기준으로 다시 바꾼다.
- (이번에 안 읽음 `[공백]`) `KeWaitForMultipleObjects`·`KeDelayExecutionThread` 의 `0x100` 반복. test.exe 는 `WaitForSingleObject` 만 쓴다 (`sample/test.c`).

**지금 우리 (PE_emulator):**
- "기다림은 이어짐" — **반쯤 맞다.** 같은 객체·같은 마감으로 이어지지만, 그 사이 객체 대기 목록에서 빠지고 **다시 처음부터 신호를 검사**한다 (그 사이 신호가 났으면 가져감).
- "기다림 결과는 버림" — **맞다** (`0x100` 은 버리고 루프). 단 최종 결과는 **다시 한 기다림의 결과**다.

### 4. B 가 결국 깨어날 때

**(a) 신호로 깨면 신호를 먹나 — 예. 문맥을 바꿨다는 사실은 기다림 채우기에 영향이 없다 (아니오).**
- 깨난 B 는 위 (c) 의 `+0x104` 부터 평범한 `KeWaitForSingleObject` 로 돈다. 신호가 있으면 `+0x5f0 AND AL,7 / +0x5f4 JZ +0x645` → `+0x645 MOV [R12+4],R10D(0)` (자동 재설정 이벤트 0 으로), 뮤턴트는 `+0x3e3~` 에서 `OwnerThread=B` (sast/73 질문 1(a)). 신호 때문에 깰 때도 같은 규칙: 깨우는 쪽이 `KiTryUnwaitThread` 로 `WaitStatus=WaitKey(0)` 쓰고 이벤트를 내림 (sast/73 질문 2). **"SetContext 가 걸려 있다" 를 읽는 줄이 기다림 코드 어디에도 없다** (`KeWaitForMutexObject`·`KiCommitThreadWait`·`KiSwapThread`·`KiTryUnwaitThread`·`KeInsertQueueApc`·`KiDeliverApc` `.asm` 에서 `0x6a8` 과 `0x8000000` 을 grep 하면 0건. 유일한 일치는 `KeWaitForMutexObject +0x3fd CMP EAX,0x80000000` 뿐이고 이것은 뮤턴트 재귀 한도다).
- 신호가 APC 보다 먼저 와서 B 가 신호로 깨났다면 (질문 2(b)): B 는 이미 신호를 먹었고 (`WaitStatus=0`), 그 뒤 APC 만 돈다. 이때 최종 반환은 `0`.
- **지금 우리 "신호를 안 먹음" — 틀리다.** 진짜는 먹는다. 신호로 깨는 것과 APC 는 서로 상관없다. (APC 가 먼저 깨웠으면 신호는 이벤트에 남았다가 B 가 다시 검사할 때 먹는다.)

**(b) syscall 출구 — 유저로 돌아갔을 때 `RAX` = 기다림 결과다 (보통의 스레드). 문맥에 넣은 `Rax` 는 덮여 사라진다.**

`KiSystemServiceStart`:
- 서비스 호출: `+0x190 MOV RAX,R10 / +0x193 CALL RAX` (ret `+0x195`) → 서비스(= `NtWaitForSingleObject` → `ObWaitForSingleObject` → `KeWaitForSingleObject`)의 반환값이 `RAX` (`+0x2c4 MOV EAX,R14D` 하위 32비트, 위쪽은 0).
- 출구 (`+0x1a0` = `KiSystemServiceExit`): `+0x1be TEST byte [RBP+0xf0],1 / JZ +0x5be` (SegCs 하위 비트; 유저로 돌아갈 때) → `+0x1f1 CLI` → `+0x1fb TEST byte [RCX+0xc2],3` (유저 APC 대기) 이 0 이면 `+0x202 JZ +0x25d`:
  - **`+0x25d MOV qword [RBP-0x50],RAX`** — 서비스 반환값을 **트랩 프레임 `Rax` 칸에 쓴다.** (APC 를 거친 길에서는 `+0x204 MOV [RBP-0x50],RAX` … `+0x257 MOV RAX,[RBP-0x50]` 으로 같은 칸을 지나간다.)
  - `+0x291 TEST dword [RCX],0x8000000 / +0x297 JZ +0x2d8` — **Header 비트 27 이 꺼져 있으면 (보통) `KiRestoreSetContextState` 를 건너뛴다.**
  - 이어서 `+0x540 MOV RAX,[RBP-0x50]` (조각 `FUN_1406c20cc`) → … → `+0x5bb SYSRET`.
- **`PspSetContext` 가 APC 안에서 쓴 트랩 `Rax (+0x30)` 는, 그 뒤 B 가 깨나 서비스를 마치고 `+0x25d` 에서 덮어쓴다 — 나중에 쓰는 쪽은 `+0x25d`.** 결과: 유저로 돌아갔을 때 **`RAX` = 기다림 결과 (신호 `0`, 마감 `0x102`, 에러 코드)**, 문맥에 넣은 `Rax` 가 아니다. 갈림 조건은 "문맥을 넣었나" 가 아니라 "이 스레드가 syscall 출구를 타나 + Header 비트 27".
- **갈래 둘** (갈리는 조건 = **B 의 `SetContextState (+0x6a8)` 가 있나**):
  1. **`SetContextState` 없음** (아래 `[정적: 전수 grep]` 로 보통의 스레드는 이쪽으로 읽힘): 위 그대로. 트랩 `Rip`/`Rsp`/`EFlags` 는 APC 가 쓴 새 값이 그대로 `SYSRET` 에 들어가고 (`+0x56c MOV RCX,[RBP+0xe8]` `+0x573 MOV R11,[RBP+0xf8]` `+0x544 MOV R8,[RBP+0x100]`), `RAX` 만 기다림 결과.
  2. **`SetContextState` 있음**: `PspGetSetContextInternal +0x3da JNZ +0x610` 이후 유저 호출 + (꾸러미 비트 1 이거나 이미 비트 27 이 서 있으면) **`PspSetContextState`** 가 부른다. 그러면 트랩 프레임을 **그 자리에서 안 고치고** `Thread+0x6a8` 의 `_CONTEXT` 에 합쳐 두고 (`PspSetContextState +0x1a TEST byte [RCX+3],8 / +0x21 MOV R14,[RCX+0x6a8]` — 비트 27 이 이미 서 있으면 `+0x28 JNZ +0x77` 로 초기화를 건너뜀; 처음이면 `+0x4a RtlGetExtendedContextLength` · `+0x59 memset` · `+0x6c RtlInitializeExtendedContext` · `+0x71 [R14+0x30]=0x100000`, **`+0x79 BTS.LOCK [RBP],0x1b` = Header 비트 27 켬**; `+0x9a / ret +0x9f RtlCopyContext(R14, 플래그, 입력)`) 유저로 돌아가는 출구에서 적용한다:
     - 출구: `KiSystemServiceStart +0x291/+0x297` 이 비트 27 이 서 있어서 안 건너뜀 → `+0x299 MOV [RBP-0x50],RAX` (기다림 결과를 `Rax` 칸에) · `+0x29f~+0x2cf` 로 트랩의 `Rcx~R11`·`Xmm0~5` 를 **0 으로** → **`+0x2d3 CALL KiRestoreSetContextState`** (ret `+0x2d8` 는 실제로는 안 돌아옴, 아래).
     - `KiRestoreSetContextState`: `+0x72 MOV RCX,[RCX+0x6a8]` · `+0x79 MOV [RSP+0x20],1` · `+0x82 MOV R9D,[RCX+0x30]` (모아 둔 `ContextFlags`) · `+0x8c LEA RCX,[RBP-0x80]` (트랩 프레임) · **`+0x90 / ret +0x95 KeContextToKframes → KxContextToKframes`** (`KeContextToKframes +0x78 / ret +0x7d`) 가 **`ContextFlags` 에 `INTEGER` 가 있으면 `Rax`(트랩 `+0x30`)·`Rcx`·`Rdx`·`R8~R11` 를 컨텍스트 값으로 덮어씀** (`KxContextToKframes +0xc9 AND EAX,0x100002 / +0xd9 MOV RAX,[RBX+0x78] / +0xdd MOV [RDI+0x30],RAX …`) → `+0xa8 BTR.LOCK [RCX],0x1b` (비트 27 끔) → **`+0x363~+0x393` (조각 `FUN_1406aecef`) `XMM0~5`·`R11=[RBP-0x20]` … `RCX=[RBP-0x48]`·`RAX=[RBP-0x50]` 를 트랩에서 다 읽어 `+0x3cb IRETQ`** (또는 `KiKvaShadow` 면 `+0x3b1 JMP KiKernelExit`).
     - **나중에 쓰는 쪽은 `KxContextToKframes`** (`+0x299` 의 기다림 결과 쓰기보다 뒤). 유저로 돌아갔을 때 **`RAX` = 컨텍스트의 `Rax` (`CONTEXT_INTEGER` 를 넣었을 때), 안 넣었으면 기다림 결과** (`+0x299` 가 쓴 값이 안 덮임). `RCX`/`R11` = 컨텍스트 값 (INTEGER) 또는 **0** (안 넣었으면 `+0x29f~+0x2cf` 의 0).

  **어느 갈래가 일반 스레드인가 — `[정적: 전수 grep]`.** `Thread+0x6a8` 을 쓰는 곳은 `PspAllocateThread +0x2caa31 MOV [RSI+0x6a8],RAX` **한 곳** (바이너리 전체 `[reg + 1704]` 검색), 그 줄에 오는 길은 `PspAllocateThread +0x5b3 JZ 0x140b0d315` (스택 인자 `[RSP+0x70]`==0 — `PspSetupUserStack`/`MmCreateTeb` 길로 안 가는 스레드) → `0x140b0d315 CMP [R15+0x640],RDI / JZ 0x140b0d3c3` (**`Process.PicoContext ≠ 0`**) → `RtlGetExtendedContextLength` → `ExAllocatePool2(…, 'PsSc')`. 곧 **Pico(WSL1) 프로세스의 스레드 중 그 스택 인자가 0 인 것에만 잡힌다** (그 인자의 이름·뜻은 `[공백]` — 0 이면 `PspSetupUserStack`/`MmCreateTeb` 를 안 부르는 길이라 "유저 스택·TEB 를 커널이 안 만드는 스레드" 로 읽힘 `[추측]`). `PspAllocateThread +0x5ab MOV RCX,[RSP+0x70]` 이 0 이 아닌 보통 스레드는 `+0x5ea PspSetupUserStack` → `+0x62c MmCreateTeb` 길이라 이 줄을 안 지난다. Header 비트 27 을 켜는 곳도 `PspSetContextState +0x79` **하나** (바이너리 전체에서 `bts/btr dword ptr [reg], 27` 은 이 줄, 끄는 `KiRestoreSetContextState +0xa8`, 그리고 `Mm*` 쪽 셋뿐이고, `or dword ptr [reg], 0x8000000` 은 6곳인데 보안·ALPC·`Mm*`·`StDm*` 같은 **다른 구조체**다 — 스레드 헤더(`+0x0`)로 보이는 것은 없음 `[추측: 이름만 보고 가른 것]`). → **일반 Win32 스레드의 `NtSetContextThread` 는 갈래 1 (그 자리에서 고침 → 출구에서 `RAX` 덮임).**
  - **못 닫은 것**: 보통 스레드가 정말 `[RSP+0x70]≠0` 으로 만들어지는지 — `NtCreateThreadEx +0x14c LEA R14,[RSP+0x70]` / `+0x282 MOV [RSP+0x60],R14` 로 포인터가 넘어가는 줄만 봤고 다른 갈래는 안 봤다 `[공백]`. **한 줄 확인**: VM 에서 `dt nt!_ETHREAD <test.exe 스레드> SetContextState` 가 `0` 인가 (dast 요청 후보 아래).

**(c) 새 `Rip` 로 갈 때 `Rcx`·`R11` — 갈래 1 (보통): `SYSRET` 이라 `RCX = 새 Rip`, `R11 = 새 EFlags`, 문맥에 넣은 `Rcx`·`R11` 값은 쓰이지 않는다.**
- `+0x56c MOV RCX,[RBP+0xe8]` (트랩 `Rip`) · `+0x573 MOV R11,[RBP+0xf8]` (트랩 `EFlags`) · `+0x544 MOV R8,[RBP+0x100]` (`Rsp`) · `+0x54b MOV R9,[RBP+0xd8]` (`Rbp`) · `+0x552 XOR EDX,EDX` · `+0x554~+0x568 PXOR XMM0~5` · `+0x587 MOV RBP,R9 / +0x58a MOV RSP,R8 / +0x58d XOR R9,R9` · `+0x5b8 SWAPGS` · `+0x5bb SYSRET`. 유저에서 보이는 휘발 레지스터: `RAX`=기다림 결과 · `RCX`=Rip · `R11`=EFlags · `RDX`=0 · `R8`=Rsp 값 · `R9`=0 · `R10`=보통 0 (`+0x309 XOR R10,R10`) · `Xmm0~5`=0. 비휘발 `Rbx` `Rdi` `Rsi` `Rbp` 는 `+0x1a0~+0x1ae` 에서 트랩 `+0x140/+0x148/+0x150/+0x158` 을 읽어 복원 (포인터 표가 가리킨 자리를 APC 가 고쳤다면 그 값).
- 갈래 2 (`SetContextState` 있음): `IRETQ` 로 **트랩 프레임 전체를 복원** (위 4(b)) — `RCX`·`R11` 도 트랩 칸 값 (INTEGER 면 컨텍스트 값, 아니면 0).
- 지금 우리: "새 `Rip` 로 감" 만 있고 `RCX`/`R11` 을 신경 안 쓰면 **괜찮다** (유저가 Windows 호출 규약상 syscall 뒤 `RCX`/`R11` 을 믿지 않음). 단 **`RAX` 는 기다림 결과** 여야 한다.

### 5. A 쪽 결과 — `NtSetContextThread` 가 돌려주는 상태값

| 상황 | 상태 | 줄 |
|---|---|---|
| 성공 (B 가 APC 를 돌려 SET 끝) | **`0` (STATUS_SUCCESS)** | `PspGetSetContextInternal +0x2f8 XOR EAX,EAX / +0x305 MOV [RCX+0x5c],EAX` → `PspSetContextThreadInternal +0x154 MOV EBX,[RBP+0x6c]` → `+0x167 MOV EAX,EBX` → `NtSetContextThread +0xc7 MOV EBX,EAX` → `+0x91 MOV EAX,EBX` |
| 핸들이 틀림·권한 없음 | `ObReferenceObjectByHandle` 의 상태 (`DesiredAccess 0x10` = THREAD_SET_CONTEXT, `PsThreadType`) | `NtSetContextThread +0x30 MOV EDX,0x10 / +0x48 / ret +0x4d / +0x51 JS +0x85` |
| 호출한 프로세스에 `RestrictSetThreadContext` (`Flags2` 비트 17, `+0x63 TEST [RAX+0x1f0],0x20000`) 가 있고 대상 스레드가 **같은 프로세스** | **`0xc000060a`** (이름 `[공백]`) | `+0xcb~+0xd6 CMP RBX,RAX`, `+0xd8 MOV EBX,0xc000060a` |
| 대상이 `SystemThread` (`+0x6f TEST [RDI+0x74],0x400`) 또는 Pico 스레드 (`+0xa4 CMP [RDI+0x670],0`) | `0xc0000008` (STATUS_INVALID_HANDLE) | `+0x78 MOV EBX,0xc0000008` |
| `ContextFlags` 가 틀림 | `RtlpSanitizeContextFlags` 의 상태 | `PspSetContextThreadInternal +0x9c / +0xa3 JS +0x169` |
| 유저 호출인데 대상이 `SystemThread` | `0xc0000030` | `+0xbe TEST [R13+0x74],0x400 / +0xc6 JNZ +0x1c4 → +0x1c4 MOV EBX,0xc0000030` |
| **B 가 끝나는 중** (`ApcQueueable` 꺼짐) | **`0xc0000001`** (STATUS_UNSUCCESSFUL) | `KeInsertQueueApc +0x134 TEST [RBX+0x74],0x4000 / +0x13b JZ +0x394 → DIL=0` → `PspSetContextThreadInternal +0x2c4 TEST AL,AL / +0x2c6 JZ +0x33e MOV EBX,0xc0000001`. `KeRundownApcQueues +0x1a BTR dword [RCX+0x74],0xe` 이 `ApcQueueable` 을 끈다 (`PspExitThread +0x4e3`, sast/75 표 31) |
| B 의 APC 안에서 실패 (스택 거슬러 오르기 실패 `+0x504`, 검증 실패 `+0x39e`·`+0x58e`·`+0x3cc` 등) | 그 코드 (`0xc0000001` 등) 가 **꾸러미 `+0x5c` 로 돌아옴** | `PspGetSetContextInternal +0x305` |
- B 가 **끝나는 중이면 실패하나**: `ApcQueueable` 이 꺼진 뒤라면 **예** (`0xc0000001`). 이미 큐에 들어간 뒤라면 B 가 끝나기 전에 APC 가 돌아야 하는데 — `KeRundownApcQueues` 는 커널 APC 가 남아 있으면 `KeBugCheckEx(0x20)` 이다 (`+0x96~+0xb3`) — 그 사이를 막는 줄은 `+0x1f / ret +0x24 KiAcquireReleaseThreadLock` (스레드 락 한 번 잡았다 놓기, 진행 중인 삽입이 끝나길 기다림) 과 `+0x2f / ret +0x34 KeLeaveCriticalRegionThread` (APC 배달) 로 보이나 **본문을 안 읽었다** `[추측]` `[공백]`.
- A 가 `0` 을 돌려받는 시점 = B 가 APC 를 **돌린 뒤** (`KeSetEvent` 뒤). B 가 새 `Rip` 로 **유저에서 돌기 시작한 뒤가 아니다** — B 는 아직 기다림 중일 수 있다 (질문 3(c)).

**지금 우리 (PE_emulator):** A 가 "곧바로 성공" 이면 **틀리다** (B 가 APC 를 돌릴 때까지 A 도 잔다). 성공 값 `0` 은 맞다.

## A·B 시간 순서 표

(`[정적]` — 코드에 쓰인 순서. 실제 실행 때 어느 CPU 에서 언제 도는지는 런타임.)

| # | 누가 | 어디 (call `+0x` / ret) | 하는 일 |
|---|---|---|---|
| 1 | A | `NtSetContextThread +0x48 / +0x4d` | 핸들 → B 객체 (접근 `0x10`). 같은 프로세스 제한·`SystemThread`·Pico 검사 (`+0x63`·`+0x6f`·`+0xa4`) |
| 2 | A | `PspSetContextThreadInternal +0xc2 / +0xc7` → `+0x9c` · `+0x252` | 플래그 정리, 유저 CONTEXT 를 A 커널 스택 버퍼로 복사 |
| 3 | A | `+0xf7 CMP R13,R14 / +0xfa JNZ +0x264` | 남이다 |
| 4 | A | `+0x273`·`+0x2a9` | 알림형 이벤트(꺼짐) 와 특별 커널 APC 를 A 스택에 만듦 |
| 5 | A | `+0x2bf / +0x2c4 KeInsertQueueApc` (`+0x134`·`+0x16a`·`+0x19c`·`+0x1b7`~`+0x24b`) | B 의 커널 APC 목록에 끼움, `KernelApcPending=1`, B 가 Waiting·WaitIrql 0·SpecialApcDisable 0·커밋됨 이면: 대기 목록에서 뗌, `State=7`, `PRCB+0x2d08` 에 push, **`WaitStatus=0x100`**, `WaitRegister.Apc` 켬 |
| 6 | A | `KeInsertQueueApc +0x26b / +0x270 KiExitDispatcher` → `KiProcessThreadWaitList +0x155~+0x16f`·`+0x101 / +0x106 KiDeferredReadySingleThread` | **B 의 블록을 이벤트 목록에서 뗌**, `AdjustIncrement=2`, B 를 준비 줄로 (A 가 곧바로 B 로 넘어갈 수도 있음 — `KiExitDispatcher +0x69 JNZ +0x134`, sast/73 `[추측]`) |
| 7 | A | `PspSetContextThreadInternal +0x2dd / +0x2e2 KeWaitForSingleObject` | **A 가 자기 이벤트에서 잠** (마감 없음) — 이미 B 가 끝냈으면 신호 상태라 안 잠 |
| 8 | B | `SwapContext +0x5c1~+0x5e6` → `KiSwapThread +0x24b`·`+0x25c`·`+0x277` | B 가 다시 돈다. "커널 APC 있음" AL=1. `WaitStatus (0x100)` 를 읽어 둠 |
| 9 | B | `KiSwapThread +0x2ee / +0x2f3 KiDeliverApc` → `KiDeliverApc +0x138 JZ +0x2b1` · `+0x76f` | APC_LEVEL 에서 특별 APC 를 목록에서 떼고 `PspGetSetContextSpecialApc` 호출 |
| 10 | B | `PspGetSetContextSpecialApc +0x64 / +0x69` → `PspGetSetContextInternal +0xa4`·`+0x298`·`+0x3d2`·`+0x3ea / +0x3ef PspSetContext` | 스택 거슬러 올라가 비휘발 저장 자리 찾음, **트랩 프레임 `Rip`/`Rsp`/`EFlags`/세그먼트 (CONTROL), `Rax`~`R11`+비휘발 (INTEGER) 쓰기** |
| 11 | B | `PspGetSetContextInternal +0x305` · `+0x32e` · `PspGetSetContextSpecialApc +0x86 / +0x8b KeSetEvent` | 상태값 `0` 을 꾸러미에, **A 의 이벤트 켬 → A 깨움** (`WaitStatus=0`) |
| 12 | A | `PspSetContextThreadInternal +0x2e2 JMP +0x154` | A 가 `EBX=[RBP+0x6c]=0` 을 받아 `NtSetContextThread` 가 `0` 반환 (B 가 아직 안 자리 잡았을 수도) |
| 13 | B | `KiSwapThread +0x28e`·`+0x29a`·`+0x2ac` → `KiCommitThreadWait +0x225`·`+0x23b` → `KeWaitForSingleObject +0x846`·`+0x860`·`+0x891 JMP +0x104` | 읽어 둔 `0x100` 을 돌려받아 **처음부터 다시** (같은 객체, 같은 절대 마감) |
| 14 | B | `+0x104`~`+0x1ad`·`+0x211`~`+0x5f0` / `+0x7b7`·`+0x841` | 이벤트 신호 있으면 **가져가고** `R14D=0` 으로 끝. 없으면 블록 달고 `KiCommitThreadWait` 로 다시 잠 (마감 지났으면 `0x102`) |
| 15 | B | (신호/마감으로 깸) `KiSwapThread` → … `KeWaitForMutexObject +0x2c4 MOV EAX,R14D` → `NtWaitForSingleObject` RET | 기다림 결과 (EAX) |
| 16 | B | `KiSystemServiceStart +0x195` → `+0x25d MOV [RBP-0x50],RAX` | **트랩 `Rax` ← 기다림 결과** (APC 가 쓴 `Rax` 덮임) |
| 17 | B | `+0x291`·`+0x540`~`+0x5bb SYSRET` | 비트 27 꺼짐(보통) → `RCX=새 Rip`·`R11=새 EFlags`·`RSP=새 Rsp`, `RAX=기다림 결과` 로 **유저의 새 `Rip` 에서 시작** |

## 지금 우리와 비교 (한 곳에 모음)

| 우리가 지금 가정 (`[추측]` 요청서) | 판정 | 줄 |
|---|---|---|
| 남에게 넣어 두고 A 는 바로 돌아옴 | 틀리다 (A 도 B 의 APC 가 끝날 때까지 마감 없이 잠) | `PspSetContextThreadInternal +0x2dd` |
| 기다림은 이어짐 | 반쯤 맞다 (B 는 기다림에서 한 번 나왔다가 처음부터 다시 검사) | `KeInsertQueueApc +0x210~+0x240`, `KeWaitForMutexObject +0x891` |
| 기다림 결과는 버림 | 맞다 (APC 로 깬 `0x100` 은 버림). 단 유저가 보는 `RAX` 는 **다시 한 기다림의 결과** | `+0x860`, `KiSystemServiceStart +0x25d` |
| 신호로 깨도 우리는 신호를 안 먹음 | **틀리다.** 진짜는 먹는다 (SetContext 는 신호 소비에 영향 없음) | sast/73 질문 1·2, 이 문서 4(a) |
| 새 `Rip` 로 감 | 맞다 — 기다림이 끝난 뒤에 간다 (지금 바로가 아니다) | `KiSystemServiceStart +0x56c`, `PspSetContext +0x68` |
| (요청서 질문) 문맥의 `Rax` 가 유저에게 보이나 | 보통 스레드는 **아니다** (기다림 결과로 덮임). `SetContextState` 가 있는 스레드·유저에서 돌다 인터럽트 받은 스레드는 예 | 4(b) |

## PE_emulator 가 흉내 낼 최소 규칙

1. **A 쪽**: 남에게 `SetThreadContext` 가 오면, ① B 가 끝나는 중 (`ApcQueueable` 꺼짐) 이면 즉시 `0xc0000001`. ② 아니면 입력 CONTEXT 를 A 쪽 버퍼에 복사하고, A 전용 "개인 이벤트(꺼짐)" 를 만들어 **B 에 특별 커널 APC 하나를 넣고, A 는 그 이벤트에서 마감 없이 잔다.** 결과값은 `0` (APC 가 쓰는 칸).
2. **B 를 깨우는 조건**: B 가 `Waiting` 이고 기다림이 커밋됐고 `WaitIrql==0`·`SpecialApcDisable==0` 이면 → 객체 대기 목록에서 B 의 블록을 떼고, B 의 `WaitStatus` 를 `0x100` 으로 하고, B 를 준비 줄에 (우선순위 +2). **객체 신호는 건드리지 않는다.** 이미 신호로 깨난 B 면 그대로 두고 `KernelApcPending` 만 켠다. 커밋 전이면 `WaitRegister.Apc` 비트만 켜서 B 가 안 자고 `0x100` 으로 나오게 한다. (스레드 끝내기 APC, sast/75, 도 이 규칙 그대로.)
3. **B 가 다시 돌 때 (기다림 코드에서 돌려받기 직전)**: `KernelApcPending` 이 서 있으면 먼저 APC 를 돌린다. 이때 `WaitStatus` 는 **APC 돌리기 전에** 읽어 두었다가 그대로 돌려준다.
4. **APC 가 하는 일** (트랩 프레임 `Rip`·`Rsp`·`EFlags`·`SegCs/SegSs` 는 `CONTROL`, `Rax`·`Rcx`·`Rdx`·`R8~R11` 와 비휘발 `Rbx Rbp Rsi Rdi R12~R15` 는 `INTEGER`, `Xmm` 은 `FLOATING_POINT` 일 때만): 컨텍스트 플래그에 있는 것만 쓴다. 플래그에 없으면 안 건드린다. 쓴 뒤 A 의 이벤트를 켠다 (A 는 `WaitStatus 0` 으로 깸).
5. **B 는 `0x100` 이면 기다림을 처음부터 다시 한다** — 신호 검사(있으면 먹음) → 마감 검사 → 아니면 같은 **절대 마감** 으로 다시 잠. 상대 마감은 처음 한 번만 계산해 둔 값 그대로 (새로 늘리지 않음).
6. **syscall 에서 유저로 돌아갈 때**: `RAX` = **마지막 기다림의 반환값** (문맥의 `Rax` 가 아니다), `RCX` = 트랩 `Rip` (APC 가 고친 새 값), `R11` = 트랩 `EFlags`, `RSP` = 트랩 `Rsp`. `RDX`·`R9`·`R10`·`Xmm0~5` 는 0 으로 둬도 된다 (휘발 레지스터). `Rbx Rbp Rsi Rdi R12~R15` 는 APC 가 쓴 값. **마지막 기다림이 "신호라서 안 자고 끝남" 이라도 반환값은 레지스터 값 (`0`) 이고 `WaitStatus` 칸이 아니다.**
7. **건너뛰어도 되는 것**: 스택 거슬러 올라가기 (에뮬레이터는 레지스터를 스레드 문맥에 바로 들고 있음), `SetContextState` 미룸·`KiRestoreSetContextState`·`IRETQ` 갈래 (Pico 전용), CET·XSTATE·보안 컨텍스트·디버그 레지스터·WoW64, ETW, `KiApcInterrupt` 소프트웨어 인터럽트 (에뮬레이터에서는 "깨난 직후 APC 확인" 하나로 합쳐도 됨).
8. **유저 모드에서 돌고 있는 B (syscall 안이 아님)**: 이번 코드로는 `RAX` 도 반영될 것으로 읽히나 (`KiApcInterrupt` 의 `IRETQ`) 이 문서에서 줄 단위로는 안 따라갔다 `[공백]`.

## 동적으로 확인할 것 (이번 묶음엔 VM 안 함, Q104 에 붙일 후보)

- **dast 요청 필요 (싸다, 새 샘플 없이 됨)**: test.exe 의 메인·Worker 스레드의 **`SetContextState` 가 0 인가** — `dt nt!_ETHREAD <addr> SetContextState` (또는 `?? ((nt!_ETHREAD*)<addr>)->SetContextState`). 0 이면 질문 4(b) 의 갈래 1 (일반 스레드는 트랩을 그 자리에서 고침, `RAX` 덮임) 이 닫힌다. 함께 `dx -r1 ((nt!_KTHREAD*)<addr>)->Header` 의 비트 27 (`0x8000000`) 이 꺼져 있나.
- **새 샘플이 있어야 닫히는 것** (test.exe 는 `SetThreadContext` 를 안 부른다 `[소스]`): 스레드 둘 (B 는 `WaitForSingleObject(ev, INFINITE)` 에서 기다림, A 가 `SetThreadContext(B, {Rip=X, Rsp=Y, Rax=0x1234, ContextFlags=CONTROL|INTEGER})` → A 가 `SetEvent(ev)`) 를 만들고:
  - `bp nt!KeInsertQueueApc` (조건: 인자 `RDX==1`, 호출 스레드가 A) 에서 `KernelRoutine == nt!PspGetSetContextSpecialApc`, `NormalRoutine == 0`, `r9`(=2).
  - `bp nt!KeInsertQueueApc+0x240` (= `+0x235` 쓰기 직후) `[B+0xc8] == 0x100`, `[B+0x184] == 7`.
  - `bp nt!KiSwapThread+0x2ee` (B 가 부름) → `KiDeliverApc` 안에서 `PspGetSetContextSpecialApc` 로 들어가는가 (`+0x76f` 간접 호출).
  - `bp nt!PspSetContext` → 트랩 프레임 `Rip`/`Rax` 칸 값, 그리고 `bp nt!KiSystemServiceStart+0x25d` 직전 `rax` (= 기다림 결과) 와 직후 `[rbp-0x50]`.
  - 유저로 돌아간 뒤 첫 명령에서 `rax` = 기다림 결과(`0`), `rcx` = X, `r11` = 새 EFlags 인가.
- 신호와 APC 가 겹치는 경쟁 (`KeSetEvent` 가 먼저/나중): CPU 둘 이상이라 순서는 런타임.
- `KeLeaveCriticalRegionThread`·`KiFlushQueueApc` 로 "B 가 끝나는 중에 막 들어온 APC" 가 어떻게 처리되는지 (`KeRundownApcQueues +0x2f`/`+0x39`) — 정적으로 본문 안 읽음.
- `KeWaitForMultipleObjects`·`KeDelayExecutionThread` (Sleep) 도 같은 `0x100` 반복인지 — 이번 안 읽음.

## 판정

**분석성공 (부분).** 요청서 5개 질문 모두 줄 근거와 함께 답했다. 따로 못 닫은 것: (1) 질문 4(b) 에서 **보통 스레드의 `SetContextState` 가 정말 0 인지** — 코드(바이너리 전체 검색)로는 Pico/TEB 없는 스레드에만 잡히는 것으로 읽히나, `NtCreateThreadEx` 가 `PspAllocateThread` 의 그 스택 인자에 0 이 아닌 값을 확실히 넘기는지는 한 갈래만 봄 → VM 한 줄 확인 후보 (위). (2) 질문 5 의 "B 가 끝나는 중에 이미 들어간 APC" 는 `KeRundownApcQueues` 안 `KeLeaveCriticalRegionThread` 본문을 안 읽어 `[추측]`. (3) `PspGetSetContextInternal` 의 스택 거슬러 오르기 끝 조건 (`+0x2af`) 과 `+0xa8 TEST [RDI+8],1` 루프의 뜻, 유저 모드에서 도는 B 의 `Rax` 반영은 줄기 밖이라 이름만 (`[공백]`). 버전 대조는 일치 (알려진 사실 3개).

**정정 두 가지 (오케스트레이터가 반영해 주세요)**
- run16 의 "`PspGetSetContextSpecialApc +0x64 → PspGetSetContextInternal → KeContextToKframes`" 에서 **마지막 고리는 `PspSetContext` (`PspGetSetContextInternal +0x3ea / ret +0x3ef`)** 다. `KeContextToKframes` 는 `KiRestoreSetContextState`·`KiContinueEx`·`KiContinuePreviousModeUser` 가 부른다.
- sast/73 이 적은 "새 문맥 길 `KiSystemServiceStart +0x2d3 KiRestoreSetContextState` → `IRETQ`" 는 **NtContinue 길이 아니라 "Header 비트 27 이 켜진 스레드" 길**이고, 그 비트는 `PspSetContextState +0x79` 만 켠다 (질문 4(b)). 일반 스레드의 SetContext 는 이 길을 타지 않는 것으로 읽힌다.

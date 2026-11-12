# 2026-11-12 run80 — 기다리는 남의 스레드에 문맥 넣기 (Q141e, PE_emulator 요청 25 ㅁ): **A 는 B 에게 특별 커널 APC 하나를 넣고, B 가 그 APC 를 돌릴 때까지 마감 없이 잔다. APC 는 기다리던 B 를 `0x100` 으로 한 번 깨우고 (객체 신호는 안 먹음), B 는 APC 안에서 자기 트랩 프레임을 그 자리에서 고친 뒤 기다림을 처음부터 다시 한다. 유저로 돌아갈 때 `RAX` 는 넣은 문맥의 `Rax` 가 아니라 마지막 기다림의 결과다.** `NtSetContextThread +0xc2` → `PspSetContextThreadInternal +0x2a9 KeInitializeApc (KernelRoutine = PspGetSetContextSpecialApc, NormalRoutine 0)` → `+0x2bf KeInsertQueueApc (…, 1 = 넣기, B, +2)` (`+0x19c KernelApcPending` · `+0x210 KiRemovePrcbWaitEntry` · `+0x218 KiEnterDeferredReadyState` (State 7) · `+0x235 WaitStatus = 0x100`) → `+0x2dd KeWaitForSingleObject` (A 잠). B 쪽: `KiSwapThread +0x25c` (`0x100` 먼저 읽어 둠) → `+0x2ee KiDeliverApc` → `PspGetSetContextSpecialApc +0x64` → `PspGetSetContextInternal +0x3ea PspSetContext` (트랩 `Rip`·`Rsp`·`EFlags`, INTEGER 면 `Rax`…) → `+0x86 KeSetEvent` (A 깸) → `KeWaitForSingleObject +0x860 / +0x891` 처음부터 → … → `KiSystemServiceStart +0x25d` 가 트랩 `Rax` 를 기다림 결과로 덮고 `SYSRET` (새 `Rip` 로)

## PE_emulator 쪽에서 쓸 것

- **한 줄**: 지금 우리 nt 엔진의 "기다림 이어짐 · 결과 버림 · 신호 안 먹음" 은 **하나는 반쯤 맞고, 하나는 맞고, 하나는 틀리다** `[정적]`. 그리고 A 쪽이 하나 빠졌다.
  - **기다림 이어짐 — 반쯤 맞다.** 같은 객체·같은 (절대) 마감으로 이어지긴 한다. 하지만 그 사이 B 는 기다림에서 **한 번 나온다** (`WaitStatus = 0x100`, 객체 대기 목록에서 빠짐). APC 를 돌린 뒤 기다림을 **처음부터 다시 검사**한다.
  - **결과 버림 — 맞다.** `0x100` 은 버리고 다시 돈다. 유저가 받는 `RAX` = **다시 한 기다림의 결과** (신호 0, 마감 `0x102`, 에러 코드).
  - **신호 안 먹음 — 틀리다.** 다시 기다릴 때 신호가 있으면 **먹는다** (자동 이벤트 0, 뮤턴트 주인 = B). "문맥이 들어왔다" 를 읽는 줄이 기다림 코드 어디에도 없다.
  - **A 도 잔다 (빠진 것).** A 는 B 가 APC 를 돌려 `KeSetEvent` 할 때까지 **마감 없이** 잔다 (`PspSetContextThreadInternal +0x2dd`). A 가 바로 돌아온다면 틀리다. A 가 성공 `0` 을 받는 때는 "B 가 APC 를 돌린 뒤" 이지 "B 가 새 `Rip` 에서 돌기 시작한 뒤" 가 아니다.
- 질문마다 (결과서 sast/76 그대로, 근거 줄은 오케스트레이터·flow-writer 가 `.asm` 으로 다시 봄):

| 질문 | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1 A 가 APC 걸기 | 남이면 입력 CONTEXT 를 **A 커널 스택 버퍼로 복사** 한 뒤, A 스택에 알림형 이벤트 (꺼짐) 와 **특별 커널 APC** (`KernelRoutine = PspGetSetContextSpecialApc`, `NormalRoutine 0`, 커널 모드, 원래 환경) 를 만들고 `KeInsertQueueApc(apc, 1 = 넣기, B, 우선순위 +2)`. 넣기 실패면 `0xc0000001` 로 끝 (안 기다림) | "B 에게 넣어 두고 A 는 바로 돌아옴" 이면 **다름** | `PspSetContextThreadInternal +0xf7/+0xfa` · `+0x252` · `+0x264` · `+0x273` · **`+0x2a9`** · `+0x2ae ~ +0x2b7` · **`+0x2bf`** · `+0x2c4/+0x2c6` · `+0x33e`, `KeInitializeApc +0x1f` · `+0x26` · `+0x3b` · `+0x3f` · `+0x4b` |
| 1' A 가 기다림 | **`+0x2dd KeWaitForSingleObject(그 이벤트, Executive, 커널, 경보 아님, 마감 없음)`**. 깨우는 건 B 의 APC 안 `KeSetEvent` 하나. B 가 영영 안 돌면 A 도 영영 잠 | "A 도 기다림" 이면 **맞음** | `+0x2dd/+0x2e2`, `PspGetSetContextSpecialApc +0x86/+0x8b` |
| 2(a) APC 가 B 를 깨우나 | **예, 그 자리에서.** 조건: B 가 `Waiting(5)` · `WaitIrql == 0` · `SpecialApcDisable == 0` · 기다림 커밋됨 (`WaitRegister.State` 1 또는 4). 하는 일: 대기 목록에서 뗌 → `State = 7` → 깨운 목록 push → **`WaitStatus = 0x100`** → `WaitRegister.Apc` 켬. `KiTryUnwaitThread` 는 안 부르고 몸에 풀어 넣음. 이어서 `KiExitDispatcher` → `KiProcessThreadWaitList` 가 B 블록을 **이벤트 목록에서 떼고** 준비 줄로 (`AdjustIncrement 2`). **객체 신호는 안 건드림** | "기다림 이어짐 (그대로 둠)" 은 **반쯤** — 한 번 나왔다 다시 들어감. 조건 넷이 없으면 모자람 | `KeInsertQueueApc +0x134` · `+0x16a` · `+0x19c` · `+0x1a8 ~ +0x1b9` · `+0x1bf` · `+0x1cb` · `+0x1d5` · `+0x1e0 ~ +0x1ea` · **`+0x210`** · **`+0x218`** · `+0x21d ~ +0x22e` · **`+0x235`** · `+0x24b` · `+0x26b`, `KiEnterDeferredReadyState +0x31` · `+0x41` · `+0x4e`, `KiProcessThreadWaitList +0x155 ~ +0x16f` · `+0x9f` |
| 2(b) 경쟁 | 신호가 먼저 깨웠으면 (`State ≠ 5`) `KernelApcPending` 만 켜고 끝 — B 는 신호 결과 (0) 로 깨고 돌아오는 길에 APC 를 돌림. APC 가 먼저면 뒤늦은 신호는 B 를 지나치고 자동 이벤트에 **남았다가** B 가 다시 검사할 때 먹힘. 커밋 전이면 `Apc` 비트만 → B 는 안 자고 `0x100`. 기다림에 들어오기 전에 이미 `KernelApcPending` 이면 시작 때 곧바로 배달 | 겹침 처리가 없으면 더해야 함 (CPU 하나·순서 고정이면 덜 중요 `[추측: 에뮬레이터 범위]`) | `KeInsertQueueApc +0x1b7/+0x1b9` · `+0x4de/+0x4e1 → +0x24b`, `KiCommitThreadWait +0x97 ~ +0xa3` · `+0x435/+0x437` · `+0x4fa`, `KeWaitForSingleObject +0x154/+0x15b` → `+0x708` |
| 3(a) APC 를 어디서 돌리나 | **`KiSwapThread` 가 직접** (`KiApcInterrupt` 아님). `SwapContext` 꼬리가 "커널 APC 있음 · 옛 IRQL 0 · `SpecialApcDisable` 0" 이면 AL = 1 → `KiSwapThread +0x25c` 가 **`WaitStatus (0x100)` 를 먼저 읽어 두고** → `+0x2ee KiDeliverApc` → 특별 APC 길 → 간접 호출로 `PspGetSetContextSpecialApc` | "깨난 직후 APC 확인" 하나로 합쳐도 됨 (결과서 규칙 7) | `SwapContext +0x5c1 ~ +0x5e6`, `KiSwapThread +0x24b` · **`+0x25c`** · `+0x277` · **`+0x2ee/+0x2f3`** · `+0x29a`, `KiDeliverApc +0x111` · `+0x135/+0x138` · `+0x4f7` · `+0x76f` |
| 3(b) 트랩 프레임 칸 | `PspGetSetContextInternal +0x3ea PspSetContext` 가 B 의 **맨 아래 트랩 프레임** (`InitialStack − 0x190`) 을 그 자리에서 고침. CONTROL → `EFlags`·`Rip`·`Rsp`·`SegCs/SegSs`. INTEGER → `Rax`·`Rcx`·`Rdx`·`R8 ~ R11` (트랩) + 비휘발 `Rbx Rbp Rsi Rdi R12 ~ R15` (커널 스택에 흩어진 자리 — 스택을 거슬러 올라가 찾음). **플래그에 없는 칸은 안 건드림** | "B 의 `Rip` 등을 바꿔 둠" **맞음**. `Rax` 는 INTEGER 일 때만 | `PspGetSetContextInternal +0xa4/+0xb2` · `+0xb9` · `+0x298` · `+0x2c0/+0x2c4` · `+0x3d2` · **`+0x3ea/+0x3ef`**, `PspSetContext +0x28/+0x35` · `+0x68` · `+0x76` · **`+0xc4/+0xce`** · **`+0xd4/+0xd8`** |
| 3(c) 다시 기다리나 | **예, 처음부터.** `0x100` → `KeWaitForSingleObject +0x860` → `+0x891 JMP +0x104` — 같은 객체, 같은 **절대** 마감 (상대 마감은 처음 한 번 계산한 값 그대로. "남은 시간으로 다시 계산" 아님) | **반쯤 맞음** — 이어지되 다시 **검사** 함 | `KeWaitForSingleObject +0x846` · `+0x860` · **`+0x891`**, `+0x3b6 ~ +0x3d7` (run78) |
| 4(a) 신호를 먹나 | **예.** 다시 도는 `+0x104` 가 평범한 기다림이라 신호면 먹음 (자동 이벤트 0, 뮤턴트 주인). 기다림 코드 `.asm` 에서 `0x6a8`·`0x8000000` 을 보는 줄 0건 | **틀림** — 먹어야 함 | `KeWaitForSingleObject +0x5f0/+0x5f4` · `+0x645`, 결과서 grep |
| 4(b) 유저 `RAX` | **기다림 결과** (보통 스레드). APC 가 쓴 트랩 `Rax` 를, B 가 서비스를 마친 뒤 `KiSystemServiceStart +0x25d` 가 덮음. 헤더 비트 27 꺼짐 → `KiRestoreSetContextState` 건너뜀 → `SYSRET` | "결과 버림" 이 "`RAX` 를 아무 값으로" 라면 **틀림** — 마지막 기다림 결과 | `KiSystemServiceStart +0x193/+0x195` · **`+0x25d`** · **`+0x291/+0x297`** · `+0x540` · `+0x5bb` |
| 4(b') 헤더 비트 27 길 | **`SetContextState (+0x6a8)` 가 있는 스레드만.** 그 칸에 값을 넣는 곳은 바이너리 전체에서 Pico 쪽 `PspAllocateThread +0x2caa31` 하나. 그런 스레드는 트랩을 안 고치고 칸에 모아 두고 비트 27 을 켬 → 출구에서 `KiRestoreSetContextState` → `KxContextToKframes` (INTEGER 면 `Rax` 도 문맥 값) → `IRETQ` | **건너뛰어도 됨** (Pico 전용, 결과서 규칙 7) | `PspGetSetContextInternal +0x3d2/+0x3da` · `+0x625`, `PspSetContextState +0x1a` · `+0x21` · **`+0x79`** · `+0x9a`, `KiSystemServiceStart +0x299` · **`+0x2d3`**, `KiRestoreSetContextState +0x72` · **`+0x90`** · `+0xa8`, `KxContextToKframes +0xc9` · `+0xd9/+0xdd` |
| 4(c) `RCX`·`R11` | `SYSRET` 이라 **`RCX = 새 Rip`, `R11 = 새 EFlags`**, `RSP = 새 Rsp`. 넣은 문맥의 `Rcx`·`R11` 은 안 쓰임. `RDX`·`R9`·`R10`·`Xmm0 ~ 5` 는 0 | 신경 안 써도 됨 (호출 규약상 유저가 안 믿음) | `KiSystemServiceStart +0x544` · `+0x56c` · `+0x573` · `+0x5bb` (조각 `FUN_1406c20cc`) |
| 5 A 가 받는 값 | 성공 **`0`** (B 의 APC 가 A 스택 꾸러미 칸에 씀). B 가 끝나는 중 (`ApcQueueable` 꺼짐) 이면 **`0xc0000001`**. 그 밖: 같은 프로세스 제한 `0xc000060a` · 시스템/Pico 스레드 `0xc0000008` · 유저 호출인데 시스템 스레드 `0xc0000030` | 성공 값 0 **맞음**. "곧바로 성공" 은 **틀림** | `PspGetSetContextInternal +0x2f8` · `+0x305`, `PspSetContextThreadInternal +0x154` · `+0x2c4/+0x2c6` · `+0x33e`, `KeInsertQueueApc +0x134/+0x13b`, `KeRundownApcQueues +0x1a`, `NtSetContextThread +0x63` · `+0x6f` · `+0x78` · `+0xa4` · `+0xd8` |

### 순서 한 장 — A·B 시간 순서 (결과서 17단계 그대로, 5번 `State = 7` 자리만 flow-writer 가 고쳐 적음)

`[정적]` — 코드에 쓰인 순서다. 실제로 어느 CPU 에서 언제 도는지는 런타임이다.

| # | 누가 | 어디 (call `+0x` / ret) | 하는 일 |
|---|---|---|---|
| 1 | A | `NtSetContextThread +0x48 / +0x4d` | 핸들 → B 객체 (접근 `0x10`). 같은 프로세스 제한·`SystemThread`·Pico 검사 (`+0x63`·`+0x6f`·`+0xa4`) |
| 2 | A | `PspSetContextThreadInternal +0xc2 / +0xc7` → `+0x9c` · `+0x252` | 플래그 정리, 유저 CONTEXT 를 A 커널 스택 버퍼로 복사 |
| 3 | A | `+0xf7 CMP R13,R14 / +0xfa JNZ +0x264` | 남이다 |
| 4 | A | `+0x273`·`+0x2a9` | 알림형 이벤트 (꺼짐) 와 특별 커널 APC 를 A 스택에 만듦 |
| 5 | A | `+0x2bf / +0x2c4 KeInsertQueueApc` (`+0x134`·`+0x16a`·`+0x19c`·`+0x1b7` ~ `+0x24b`) | B 의 커널 APC 목록에 끼움, `KernelApcPending = 1`. B 가 Waiting · `WaitIrql 0` · `SpecialApcDisable 0` · 커밋됨 이면: 대기 목록에서 뗌 (`+0x210`), **`State = 7` (`+0x218 KiEnterDeferredReadyState` 안)**, `PRCB+0x2d08` 에 push, **`WaitStatus = 0x100`** (`+0x235`), `WaitRegister.Apc` 켬 (`+0x24b`) |
| 6 | A | `KeInsertQueueApc +0x26b / +0x270 KiExitDispatcher` → `KiProcessThreadWaitList +0x155 ~ +0x16f` · `+0x101 / +0x106 KiDeferredReadySingleThread` | **B 의 블록을 이벤트 목록에서 뗌**, `AdjustIncrement = 2`, B 를 준비 줄로 (A 가 곧바로 B 로 넘어갈 수도 있음 — `KiExitDispatcher +0x69 JNZ +0x134`, sast/73 `[추측]`) |
| 7 | A | `PspSetContextThreadInternal +0x2dd / +0x2e2 KeWaitForSingleObject` | **A 가 자기 이벤트에서 잠** (마감 없음) — B 가 이미 끝냈으면 신호 상태라 안 잠 |
| 8 | B | `SwapContext +0x5c1 ~ +0x5e6` → `KiSwapThread +0x24b`·`+0x25c`·`+0x277` | B 가 다시 돈다. "커널 APC 있음" AL = 1. `WaitStatus (0x100)` 를 읽어 둠 |
| 9 | B | `KiSwapThread +0x2ee / +0x2f3 KiDeliverApc` → `KiDeliverApc +0x138 JZ +0x2b1` · `+0x76f` | APC_LEVEL 에서 특별 APC 를 목록에서 떼고 `PspGetSetContextSpecialApc` 호출 |
| 10 | B | `PspGetSetContextSpecialApc +0x64 / +0x69` → `PspGetSetContextInternal +0xa4`·`+0x298`·`+0x3d2`·`+0x3ea / +0x3ef PspSetContext` | 스택을 거슬러 올라가 비휘발 저장 자리를 찾음, **트랩 프레임 `Rip`/`Rsp`/`EFlags`/세그먼트 (CONTROL), `Rax` ~ `R11` + 비휘발 (INTEGER) 쓰기** |
| 11 | B | `PspGetSetContextInternal +0x305` · `+0x32e` · `PspGetSetContextSpecialApc +0x86 / +0x8b KeSetEvent` | 상태값 `0` 을 꾸러미에, **A 의 이벤트 켬 → A 깨움** (`WaitStatus = 0`) |
| 12 | A | `PspSetContextThreadInternal +0x2e2 JMP +0x154` | A 가 `EBX = [RBP+0x6c] = 0` 을 받아 `NtSetContextThread` 가 `0` 반환 (B 는 아직 기다림 중일 수 있음) |
| 13 | B | `KiSwapThread +0x28e`·`+0x29a`·`+0x2ac` → `KiCommitThreadWait +0x225`·`+0x23b` → `KeWaitForSingleObject +0x846`·`+0x860`·`+0x891 JMP +0x104` | 읽어 둔 `0x100` 을 돌려받아 **처음부터 다시** (같은 객체, 같은 절대 마감) |
| 14 | B | `+0x104` ~ `+0x1ad`·`+0x211` ~ `+0x5f0` / `+0x7b7`·`+0x841` | 이벤트 신호가 있으면 **가져가고** `R14D = 0` 으로 끝. 없으면 블록 달고 `KiCommitThreadWait` 로 다시 잠 (마감 지났으면 `0x102`) |
| 15 | B | (신호/마감으로 깸) `KiSwapThread` → … `KeWaitForSingleObject +0x2c4 MOV EAX,R14D` → `NtWaitForSingleObject` RET | 기다림 결과 (EAX) |
| 16 | B | `KiSystemServiceStart +0x195` → `+0x25d MOV [RBP-0x50],RAX` | **트랩 `Rax` ← 기다림 결과** (APC 가 쓴 `Rax` 덮임) |
| 17 | B | `+0x291`·`+0x540` ~ `+0x5bb SYSRET` | 비트 27 꺼짐 (보통) → `RCX = 새 Rip`·`R11 = 새 EFlags`·`RSP = 새 Rsp`, `RAX = 기다림 결과` 로 **유저의 새 `Rip` 에서 시작** |

### PE_emulator 가 흉내 낼 최소 규칙 (결과서 sast/76 끝을 그대로 옮김)

1. **A 쪽**: 남에게 `SetThreadContext` 가 오면, ① B 가 끝나는 중 (`ApcQueueable` 꺼짐) 이면 즉시 `0xc0000001`. ② 아니면 입력 CONTEXT 를 A 쪽 버퍼에 복사하고, A 전용 "개인 이벤트 (꺼짐)" 를 만들어 **B 에 특별 커널 APC 하나를 넣고, A 는 그 이벤트에서 마감 없이 잔다.** 결과값은 `0` (APC 가 쓰는 칸).
2. **B 를 깨우는 조건**: B 가 `Waiting` 이고 기다림이 커밋됐고 `WaitIrql == 0`·`SpecialApcDisable == 0` 이면 → 객체 대기 목록에서 B 의 블록을 떼고, B 의 `WaitStatus` 를 `0x100` 으로 하고, B 를 준비 줄에 (우선순위 +2). **객체 신호는 건드리지 않는다.** 이미 신호로 깨난 B 면 그대로 두고 `KernelApcPending` 만 켠다. 커밋 전이면 `WaitRegister.Apc` 비트만 켜서 B 가 안 자고 `0x100` 으로 나오게 한다. (스레드 끝내기 APC, sast/75, 도 이 규칙 그대로 — 아래 flow-writer 덧붙임 ① 의 두 칸 차이 있음.)
3. **B 가 다시 돌 때 (기다림 코드에서 돌려받기 직전)**: `KernelApcPending` 이 서 있으면 먼저 APC 를 돌린다. 이때 `WaitStatus` 는 **APC 돌리기 전에** 읽어 두었다가 그대로 돌려준다.
4. **APC 가 하는 일** (트랩 프레임 `Rip`·`Rsp`·`EFlags`·`SegCs/SegSs` 는 `CONTROL`, `Rax`·`Rcx`·`Rdx`·`R8 ~ R11` 와 비휘발 `Rbx Rbp Rsi Rdi R12 ~ R15` 는 `INTEGER`, `Xmm` 은 `FLOATING_POINT` 일 때만): 컨텍스트 플래그에 있는 것만 쓴다. 플래그에 없으면 안 건드린다. 쓴 뒤 A 의 이벤트를 켠다 (A 는 `WaitStatus 0` 으로 깸).
5. **B 는 `0x100` 이면 기다림을 처음부터 다시 한다** — 신호 검사 (있으면 먹음) → 마감 검사 → 아니면 같은 **절대 마감** 으로 다시 잠. 상대 마감은 처음 한 번만 계산해 둔 값 그대로 (새로 늘리지 않음).
6. **syscall 에서 유저로 돌아갈 때**: `RAX` = **마지막 기다림의 반환값** (문맥의 `Rax` 가 아니다), `RCX` = 트랩 `Rip` (APC 가 고친 새 값), `R11` = 트랩 `EFlags`, `RSP` = 트랩 `Rsp`. `RDX`·`R9`·`R10`·`Xmm0 ~ 5` 는 0 으로 둬도 된다 (휘발 레지스터). `Rbx Rbp Rsi Rdi R12 ~ R15` 는 APC 가 쓴 값. **마지막 기다림이 "신호라서 안 자고 끝남" 이라도 반환값은 레지스터 값 (`0`) 이고 `WaitStatus` 칸이 아니다.**
7. **건너뛰어도 되는 것**: 스택 거슬러 올라가기 (에뮬레이터는 레지스터를 스레드 문맥에 바로 들고 있음), `SetContextState` 미룸·`KiRestoreSetContextState`·`IRETQ` 갈래 (Pico 전용), CET·XSTATE·보안 컨텍스트·디버그 레지스터·WoW64, ETW, `KiApcInterrupt` 소프트웨어 인터럽트 (에뮬레이터에서는 "깨난 직후 APC 확인" 하나로 합쳐도 됨).
8. **유저 모드에서 돌고 있는 B (syscall 안이 아님)**: 이번 코드로는 `RAX` 도 반영될 것으로 읽히나 (`KiApcInterrupt` 의 `IRETQ`) 이 문서에서 줄 단위로는 안 따라갔다 `[공백]`.

- flow-writer 덧붙임 (규칙에 넣을지는 PE_emulator 쪽 판단):
  - ① **끝내기 APC 와 "같은 규칙" 의 차이 두 칸** `[정적]` (flow-writer `.asm`): 문맥 넣기 APC 는 `NormalRoutine 0` 인 **특별** 커널 APC 이고, 끝내기 APC (`SchedulerApc`) 는 `NormalRoutine = KiSchedulerApc` 인 **보통** 커널 APC 다 (`KeInitThread +0x12d` → `KeInitializeApc` 여섯째 인자 (`+0x3f [RCX+0x30]`), 짐 지도 3.14 `SchedulerApc` 줄). 보통 APC 는 기다림을 깨기 전에 **`KernelApcDisable == 0` · `KernelApcInProgress == 0`** 도 본다 — `KiSignalThreadForApc +0x83/+0x88 → +0x219 · +0x227`, `KeInsertQueueApc +0x1d5/+0x1da → +0x39c · +0x3aa`. 규칙 2 에 "보통 커널 APC 면 두 칸 더" 한 줄이면 끝내기 쪽까지 하나로 된다.
  - ② **사진에서 본 것** `[관찰]` (run44, 메인 스레드 `ffff828598445080`, flow-writer 대조): `_ETHREAD.SetContextState` 는 K2 ~ P2 일곱 장 모두 `(null)`, 스레드 헤더 첫 DWORD 는 K2 `0x00200006` · K3 ~ P2 `0x00a00006` — **비트 27 (`0x8000000`) 일곱 장 모두 꺼짐**. 초기화 동안 메인 스레드는 4(b) 의 보통 갈래이고 4(b') 길은 못 탄다. Worker 는 `main` 뒤라 사진 밖이다 (아래 "아직 확인 필요" 동적 1).
  - ③ `KeInsertQueueApc +0x8d ~ +0x98` 이 따로 보내는 "특정 KernelRoutine `0x140ab1f40`" (결과서 이름 `[공백]`) 은 **`KeSpecialUserApcKernelRoutine`** (`ms/decomp/ntoskrnl/_index.tsv`, 같은 주소 이름 하나 — `pdb_aliases.py --lookup` "별칭 무리 아님"). 문맥 넣기 APC 와는 다른 길이다. 이름으로 보아 특별 유저 APC 쪽 `[추측: 이름]`.

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서, 기준 GUID 와 같음). 다시 analyze 안 함
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines`, `KiCommitThreadWait +0x220/+0x225 → KiSwapThread` (run77) 일치
- **이름 주의**:
  - PDB `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject` (run77 과 같음). `PspSetContextThreadInternal +0x2dd` 의 callee 는 `_calls.tsv` 에 `alias:KeWaitForMutexObject+1` 로 찍힌다.
  - `KiDeliverApc +0x76f` 는 `guard_dispatch_icall` (`0x1406b77f0`) 로 가는 **간접 호출**이다 (`_calls.tsv` `indirect:cfg:?`). `PspGetSetContextSpecialApc` 로 간다는 건 APC 의 `KernelRoutine` 칸에서 읽은 짐작이지 줄로 이어진 건 아니다 (`[공백 → 동적]`).
- 칸 이름 (`pdb_struct.py`, 결과서 "읽는 법"):
  - `_KAPC` (`0x58`B): `Type +0x0` · `Thread +0x8` · `ApcListEntry +0x10` · `KernelRoutine +0x20` · `RundownRoutine +0x28` · `NormalRoutine +0x30` (공개 PDB 는 `Reserved +0x20` 24바이트 — 이름은 `KeInitializeApc` 줄에서 읽음) · `NormalContext +0x38` · `SystemArgument1 +0x40` · `SystemArgument2 +0x48` · `ApcStateIndex +0x50` · `ApcMode +0x51` · `Inserted +0x52`
  - `_KTHREAD`: `Header` 첫 DWORD 비트 27 (`0x8000000`, 공개 PDB 이름 없음) · `InitialStack +0x28` · `ThreadLock +0x40` · `WaitRegister +0x70` (`Apc` 비트 5 = `0x20`) · `MiscFlags +0x74` (`SystemThread` 비트 10 · `ApcQueueable` 비트 14) · `TrapFrame +0x90` · `ApcState +0x98` · `KernelApcInProgress +0xc0` · `KernelApcPending +0xc1` · `UserApcPending +0xc2` (이 셋 이름은 `[추측: 이름]`) · `WaitStatus +0xc8` · `State +0x184` · `WaitIrql +0x186` · `KernelApcDisable +0x1e4` · `SpecialApcDisable +0x1e6` · `ApcStateIndex +0x24a` · `AbWaitObject +0x3d0`
  - `_KTRAP_FRAME` (`0x190`B): `Rax +0x30` · `Rcx +0x38` · `Rdx +0x40` · `R8 ~ R11 +0x48 ~ +0x60` · `Xmm0 ~ 5 +0x70 ~ +0xc0` · `Rbx +0x140` · `Rdi +0x148` · `Rsi +0x150` · `Rbp +0x158` · `Rip +0x168` · `SegCs +0x170` · `EFlags +0x178` · `Rsp +0x180` · `SegSs +0x188`. `KiSystemServiceStart` 에서 `RBP = TrapFrame + 0x80` 이라 `[RBP-0x50]` = `Rax`, `[RBP+0xe8]` = `Rip`, `[RBP+0xf0]` = `SegCs`, `[RBP+0xf8]` = `EFlags`, `[RBP+0x100]` = `Rsp`
  - `_ETHREAD`: `SetContextState +0x6a8` (`_CONTEXT*`) · `PicoContext +0x670` · `SameThreadPassiveFlags +0x5a4` (`SecureContext` 비트 9). `_EPROCESS`: `PicoContext +0x640` · `Flags2 +0x1f0` (`RestrictSetThreadContext` 비트 17)
  - `_CONTEXT` (x64): `ContextFlags +0x30` · `EFlags +0x44` · `Rax +0x78` · `Rcx +0x80` · `Rdx +0x88` · `Rsp +0x98` · `Rip +0xf8`. 플래그 숫자 `[정적]`, 이름은 표준: `0x100001` CONTROL · `0x100002` INTEGER · `0x100008` FLOATING_POINT · `0x100010` DEBUG_REGISTERS · `0x100040` XSTATE
- "기다림 결과" 는 칸이 아니라 **반환 레지스터** 로 본다 (결과서 오케스트레이터 메모, run77 덧붙임 ③ 과 같음): 신호라 안 자고 끝나는 길은 `WaitStatus` 칸을 안 쓰고 `R14D = 0` → `+0x2c4 MOV EAX,R14D`.

## 목적

- PE_emulator 요청 25 ㅁ (Q141e): A 가 **syscall 안에서 기다리는 남의 스레드 B** 에게 `NtSetContextThread` 를 할 때 — A 가 APC 를 거는 줄과 기다리는 줄, APC 가 B 의 기다림을 깨는가 (조건·경쟁), B 가 APC 를 어디서 돌리나, 트랩 프레임의 어느 칸을 바꾸나 (`Rax` 포함), B 가 다시 기다리나, 신호를 먹나, 유저로 돌아갈 때 `RAX` 가 무엇인가, A 가 무엇을 돌려받나.
- 기다림·깨움·`0x100` 반복은 run77, 마감은 run78, 끝내기 APC 는 run79, 자기 스레드 문맥 넣기 (초기화 때 `PspInitializeThunkContext`) 는 run16 을 가리키기만 한다.

## 방법

- 정적: `_workspace/sast/76_thread-setcontext.md` — ntoskrnl
  - 지도: 새로 안 만듦 (대상이 거의 이미 뽑은 함수의 줄이라 이름으로 뽑음).
  - 새로 뽑음 **7개** (모두 depth 0): 요청 대상 `KeInsertQueueApc` (1401B) + 넓힌 여섯 — `NtSetContextThread` (222B, 질문 5) · `KeInitializeApc` (91B, 인자 자리) · `PspSetContextState` (219B, 4(b) 비트 27) · `KeRundownApcQueues` (183B, 질문 5) · `KiRequestSoftwareInterrupt` · `KiCheckForKernelApcDelivery` (부르는 줄 확인용, 본문 안 읽음)
  - 있던 것 다시 읽음: `PspSetContextThreadInternal` · `KiInsertQueueApc` · `KiSignalThreadForApc` · `KiDeliverApc` · `PspGetSetContextSpecialApc` · `PspGetSetContextInternal` · `PspSetContext` · `KeContextToKframes` · `KxContextToKframes` · `KiRestoreSetContextState` · `KiSystemServiceStart` · `KiSwapThread` · `SwapContext` · `KiSwapContext` · `KiProcessThreadWaitList` · `KiCommitThreadWait` · `KiExitThreadWait` · `KeWaitForSingleObject` · `KeRequestTerminationThread` · `KiSchedulerApc` · `NtContinueEx` · `PspAllocateThread`
  - 안 읽은 것 (이름만): CET·XSTATE (`KeVerifyContextXStateCetU` `KeVerifyContextIpForUserCet` `RtlpValidateContextXStateDisabledFeatures` 등) · 보안 컨텍스트 (`VslGetSetSecureContext`) · 스택 걷기 (`RtlpGetStackLimits` `RtlpCaptureContext` `RtlVirtualUnwind2` 등) · 디버그 레지스터 · WoW64 · ETW · `KeLeaveCriticalRegionThread` · `KiFlushQueueApc` · `KeForceResumeThread` · `KiSendSoftwareInterrupt`
  - **칸 하나를 쓰는 곳 전부**: `_ETHREAD.SetContextState (+0x6a8)` 와 헤더 비트 27 은 결과서가 `llvm-objdump-14` 로 ntoskrnl 코드 전체를 레포 밖 scratchpad 에 풀어 `[reg + 1704]`·`bts/btr …, 27`·`or …, 0x8000000` 꼴로 찾았다 (`run_ghidra.sh refs` 는 칸 오프셋을 못 찾음). 꼴 기준이라 하한이다.
- 결과서: `_workspace/sast/76_thread-setcontext_result.md` (**분석성공 (부분)**. 다섯 질문 모두 줄로 답함. 못 닫은 셋: 보통 스레드의 `SetContextState` 가 정말 0 인지 (`NtCreateThreadEx` 의 다른 갈래 안 봄), 끝나는 중에 이미 들어간 APC (`KeLeaveCriticalRegionThread` 본문), 스택 거슬러 오르기 끝 조건·유저 모드 B 의 `Rax`)
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `PspSetContextThreadInternal +0x2a9` · `+0x2bf` · `+0x2dd`, `KeInsertQueueApc +0x210` · `+0x235` · `+0x240`, `KiSwapThread +0x2ee`, `PspGetSetContextInternal +0x3d2` · `+0x3ea`, `PspSetContextState +0x79`, `KiSystemServiceStart +0x291` · `+0x2d3`
- flow-writer 도 `.asm` 으로 다시 봤다: `PspSetContextThreadInternal +0xf7` · `+0xfa` · `+0x154` · `+0x252` · `+0x264` · `+0x273` · `+0x2ae` · `+0x2b4` · `+0x2b7` · `+0x2c4` · `+0x2c6` · `+0x2e2` · `+0x33e`, `KeInitializeApc +0x0 ~ +0x5a` (전부), `KeInsertQueueApc +0x16a ~ +0x26b` (전부) · `+0x394 ~ +0x3bc` · `+0x4de ~ +0x4e7`, `KiEnterDeferredReadyState +0x0 ~ +0x55` (전부), `KiSignalThreadForApc +0x0 ~ +0x13f` · `+0x219 ~ +0x234`, `KiSwapThread +0x243` · `+0x24b` · `+0x25c` · `+0x263` · `+0x274` · `+0x277` · `+0x29a` · `+0x2f3`, `SwapContext +0x5c1 ~ +0x5e6` (조각), `KiDeliverApc +0xa5 ~ +0x246` (보통·특별 APC 갈림, `+0x21b KiSchedulerApc`, `+0x241`) · `+0x413 ~ +0x46b` · `+0x4f7` · `+0x76f`, `KiSchedulerApc +0x1c7 ~ +0x209`, `KeInitThread +0x11c ~ +0x152`, `PspGetSetContextInternal +0xa4` · `+0xb2` · `+0xb9` · `+0x298` · `+0x2c0` · `+0x2c4` · `+0x2f8` · `+0x305` · `+0x32e` · `+0x3da` · `+0x3e0` · `+0x625`, `PspSetContext +0x28` · `+0x35` · `+0x68` · `+0x76` · `+0xc4` · `+0xce` · `+0xd4` · `+0xd8`, `PspGetSetContextSpecialApc +0x64` · `+0x86`, `PspSetContextState +0x1a` · `+0x21` · `+0x28` · `+0x9a`, `KiSystemServiceStart +0x193` · `+0x25d` · `+0x297` · `+0x299` · `+0x540` · `+0x56c` · `+0x573` · `+0x5bb` (조각), `KiRestoreSetContextState +0x72` · `+0x90` · `+0xa8` · `+0x12d` · `+0x134` · `+0x3a8 ~ +0x3cb` (조각) · `+0x3cd ~ +0x416`, `KxContextToKframes +0xc9` · `+0xd9` · `+0xdd`, `KeContextToKframes +0x78`, `KeWaitForSingleObject +0x154` · `+0x15b` · `+0x2c4` · `+0x708` · `+0x860` · `+0x891`, `KiCommitThreadWait +0x2de` · `+0x4fa`, `KiExitThreadWait +0x242`, `NtSetContextThread +0x30` · `+0x48` · `+0x63` · `+0x6f` · `+0x78` · `+0xa4` · `+0xb1` · `+0xc2` · `+0xd8`, `KeRundownApcQueues +0x1a` · `+0x1f` · `+0x2f`, `PspExitThread +0x4e3` · `+0x85f`, `PspAllocateThread +0x2caa31` · `+0x2ca940` (조각). `_calls.tsv` 에서 호출 줄 모두 맞춤 (`KeContextToKframes` 를 부르는 곳 셋 포함).
- 값 대조: run44 덤프 읽기 로그의 메인 스레드 `SetContextState` (`ms/logs/04_baggage-read_<K2 ~ P2>.txt` `02 ETHREAD` — K2 646 · K3·P0·P1 651 · P1a·P1b·P2 649줄) 와 스레드 헤더 첫 DWORD (`02c ETHREAD_RAW` 첫 줄 — K2 921 · K3·P0·P1 926 · P1a·P1b·P2 924줄) (flow-writer). 새 동적 실행은 없다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret | callee | 조건·뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtSetContextThread` | `+0x48` | `+0x4d` | `nt!ObReferenceObjectByHandle` | `+0x30 EDX = 0x10` (THREAD_SET_CONTEXT). 실패면 `+0x51 JS` 그 상태 | [정적] |
| `nt!NtSetContextThread` | **`+0xc2`** | **`+0xc7`** | **`nt!PspSetContextThreadInternal`** | `(B, ctx, PreviousMode, PreviousMode, 1)` — `+0xb1 [RSP+0x20] = 1` | [정적] |
| `nt!PspSetContextThreadInternal` | `+0x9c` | `+0xa1` | `nt!RtlpSanitizeContextFlags` | 실패면 `+0xa3 JS +0x169` | [정적] |
| `nt!PspSetContextThreadInternal` | `+0x252` | `+0x257` | `nt!RtlpReadExtendedContext` | 유저 호출일 때 — 유저 CONTEXT → A 커널 스택 버퍼 | [정적] |
| `nt!PspSetContextThreadInternal` | `+0x131` | `+0x136` | `nt!PspGetSetContextSpecialApc` | **자기** (`+0xf7 CMP R13,R14`) — APC 없이 직접 (run16) | [정적] |
| `nt!PspSetContextThreadInternal` | `+0x273` | `+0x278` | `nt!KeInitializeEvent` | 남 — `(꾸러미+0x60, 알림형 0, 꺼짐 0)` | [정적] |
| `nt!PspSetContextThreadInternal` | **`+0x2a9`** | **`+0x2ae`** | **`nt!KeInitializeApc`** | 남 — `(KAPC, B, 환경 0, PspGetSetContextSpecialApc, Rundown 0, Normal 0, 모드 0, 0)` = 특별 커널 APC | [정적] |
| `nt!PspSetContextThreadInternal` | **`+0x2bf`** | **`+0x2c4`** | **`nt!KeInsertQueueApc`** | 남 — `(KAPC, SysArg1 = 1 넣기, SysArg2 = B, Increment = 2)`. `AL = 0` 이면 `+0x2c6 JZ +0x33e` `0xc0000001` | [정적] |
| `nt!PspSetContextThreadInternal` | **`+0x2dd`** | **`+0x2e2`** | **`nt!KeWaitForSingleObject`** (Ghidra `KeWaitForMutexObject`) | 남 — `(이벤트, 0, 커널, 경보 아님, 마감 NULL)`. 반환값 안 봄 (`+0x2e2 JMP +0x154`) | [정적] |
| `nt!KeInsertQueueApc` | `+0x16a` | `+0x16f` | `nt!KiInsertQueueApc` | B 의 커널 APC 목록 (`ApcState +0x98`). `NormalRoutine 0` 이면 특별 APC 자리 | [정적] |
| `nt!KeInsertQueueApc` | **`+0x210`** | **`+0x215`** | **`nt!KiRemovePrcbWaitEntry`** | B 가 Waiting · `WaitIrql 0` · `SpecialApcDisable 0` · 커밋됨 (`+0x1b7 ~ +0x1ea`) | [정적] |
| `nt!KeInsertQueueApc` | **`+0x218`** | **`+0x21d`** | **`nt!KiEnterDeferredReadyState`** | → `+0x31` · `+0x41` · `+0x4e` `MOV byte [RCX+0x184],7` (State 7). 뒤 `+0x235 WaitStatus = 0x100` | [정적] |
| `nt!KeInsertQueueApc` | `+0x26b` | `+0x270` | `nt!KiExitDispatcher` | `(Prcb, 0, 1, Increment 2, 옛 IRQL)` → `KiProcessThreadWaitList` (sast/73) | [정적] |
| `nt!KeInsertQueueApc` | `+0x48e` · `+0x49a` | `+0x493` · `+0x49f` | `nt!KiSendSoftwareInterrupt` · `nt!KiRequestSoftwareInterrupt` | B 가 Running (2) 일 때 (`+0x1af/+0x1b1 → +0x476`) | [정적] |
| `nt!KiSwapThread` | `+0x243` | `+0x248` | `nt!KiSwapContext` | (run75) `SwapContext` 꼬리가 AL = "커널 APC 지금 배달" | [정적] |
| `nt!KiSwapThread` | **`+0x2ee`** | **`+0x2f3`** | **`nt!KiDeliverApc`** | `+0x274 TEST SIL,SIL / +0x277 JNZ` (AL = 1). 앞 `+0x25c` `WaitStatus` 읽어 둠 | [정적] |
| `nt!KiDeliverApc` | `+0x76f` | `+0x774` | (간접) `guard_dispatch_icall` → APC `KernelRoutine` | 특별 APC 길 (`+0x138 JZ +0x2b1`), 알려진 루틴 아닐 때. 이 APC 면 `PspGetSetContextSpecialApc` `[공백 → 동적]` | [정적] |
| `nt!PspGetSetContextSpecialApc` | `+0x64` | `+0x69` | `nt!PspGetSetContextInternal` | `(꾸러미, SysArg1 = 1, &이벤트칸)` (run16) | [정적] |
| `nt!PspGetSetContextInternal` | `+0x298` | `+0x29d` | `nt!RtlVirtualUnwind2` | 꾸러미 비트 1 (`+0xb9`) — 스택 거슬러 올라 비휘발 저장 자리 찾기 | [정적] |
| `nt!PspGetSetContextInternal` | **`+0x3ea`** | **`+0x3ef`** | **`nt!PspSetContext`** | SET (`+0x2c0/+0x2c4`) · `SetContextState == 0` (`+0x3d2/+0x3da`) — 트랩 프레임을 그 자리에서 고침 (run16 과 같은 줄) | [정적] |
| `nt!PspGetSetContextInternal` | 조각 `FUN_140b2a1a6 +0x7` | 조각 `+0xc` | `nt!PspSetContextState` | `SetContextState ≠ 0` (`+0x3da JNZ +0x610`) · `+0x625` 등 — 미룸 | [정적] |
| `nt!PspGetSetContextSpecialApc` | **`+0x86`** | **`+0x8b`** | **`nt!KeSetEvent`** | `(A 의 이벤트, 1, FALSE)` — 남일 때만 (`+0x71` 포인터 ≠ 0) | [정적] |
| `nt!PspSetContextState` | `+0x9a` | `+0x9f` | `nt!RtlCopyContext` | 앞 **`+0x79 BTS.LOCK [RBP],0x1b`** (헤더 비트 27) | [정적] |
| `nt!KiSystemServiceStart` | **`+0x2d3`** | `+0x2d8` | `nt!KiRestoreSetContextState` | `+0x291 TEST [RCX],0x8000000 / +0x297 JZ +0x2d8` — 비트 27 일 때만. 돌아오지 않음 | [정적] |
| `nt!KiRestoreSetContextState` | `+0x90` | `+0x95` | `nt!KeContextToKframes` | `+0x72` `SetContextState` 읽음 → `KeContextToKframes +0x78 KxContextToKframes`. 뒤 `+0xa8 BTR` 비트 27 끔 | [정적] |
| `nt!KeRundownApcQueues` | `+0x1f` · `+0x2f` | `+0x24` · `+0x34` | `nt!KiAcquireReleaseThreadLock` · `nt!KeLeaveCriticalRegionThread` | 앞 `+0x1a BTR [RCX+0x74],0xe` (`ApcQueueable` 끔). 본문 안 읽음 | [정적] |

- `KeContextToKframes` 를 부르는 곳은 `_calls.tsv` 에 셋: `KiRestoreSetContextState +0x90` · `KiContinueEx +0x1e0` · `KiContinuePreviousModeUser +0x422`. **`PspGetSetContextInternal` 은 안 부른다** (요청서 질문 3(b) 의 사슬과 다름 — 아래 "정정").

### A 쪽 — `PspSetContextThreadInternal` (질문 1 · 5)

- 지역 "꾸러미" (`RBP+0x10`) 하나: 앞 `0x58` 바이트가 `KAPC`, `+0x59` 비트 모음, `+0x5c` 상태값, `+0x60` 이벤트, `+0x78` 컨텍스트 포인터, `+0x80` 포인터 표. **APC·입력 CONTEXT·상태값 칸이 모두 A 의 커널 스택 위** — 그래서 A 는 B 가 끝낼 때까지 자리를 지켜야 한다 (결과서).
- 유저 호출이면 `+0x1d4 ~ +0x252` 에서 유저 CONTEXT 를 A 커널 스택 버퍼로 복사 (`RtlpReadExtendedContext`), `+0x238` 꾸러미 포인터 = 그 버퍼.
- `+0xf7 CMP R13,R14 / +0xfa JNZ +0x264` — 남이면:
  - `+0x264 OR BL,1` 꾸러미 비트 0 ("남이라 이벤트 있음" `[추측: 이름]`)
  - `+0x273 KeInitializeEvent(…, 0, 0)` — 알림형, 꺼짐
  - **`+0x2a9 KeInitializeApc`** — 인자 자리를 `KeInitializeApc` 줄 (`+0x3b [RCX+0x20] = R9` · `+0x26 [RCX+0x28]` · `+0x3f [RCX+0x30]` · `+0x1f [RCX+0x50] = R8B` · `+0x4b [RCX+0x51]`; `NormalRoutine 0` 이면 `+0x43 CMOVZ` 로 모드도 0) 과 맞추면 **KernelRoutine = `PspGetSetContextSpecialApc` · Rundown 0 · Normal 0 · 커널 모드 · 원래 환경** = 특별 커널 APC
  - **`+0x2bf KeInsertQueueApc(apc, 1, B, 2)`** — `+0x2ae R9D = 2` · `+0x2b4 R8 = R13 (B)` · `+0x2b7 EDX = 1`. `1` = 넣기 (`PspGetSetContextInternal +0x2c0` 이 0 이면 읽기). `2` 가 `KiExitDispatcher` 의 `Increment` 로 가 B 의 `AdjustIncrement`
  - 넣기 실패 (`+0x2c4 TEST AL,AL / +0x2c6 JZ`) → **`+0x33e EBX = 0xc0000001`**, 안 기다림
  - **`+0x2dd KeWaitForSingleObject`** — 마감 없음, 경보 아님 → `+0x2e2 JMP +0x154`
- 끝: `+0x154 EBX = [RBP+0x6c]` (꾸러미 `+0x5c`, B 의 APC 가 써 놓은 값) → 음수면 그대로, 유저 호출이면 `EtwTiLogSetContextThread` (이름만) → 반환.
- `NtSetContextThread` 앞 검사 (질문 5): `+0x63 Flags2 & 0x20000` (`RestrictSetThreadContext`) + 같은 프로세스 → `+0xd8 0xc000060a` · `+0x6f SystemThread` 또는 `+0xa4 PicoContext ≠ 0` → `+0x78 0xc0000008`.

### 깨우기 — `KeInsertQueueApc` (질문 2)

- 시작 갈림: `+0x8d ~ +0x98` KernelRoutine 이 `0x140ab1f40` (= `KeSpecialUserApcKernelRoutine`, flow-writer `_index.tsv`) 이면 `+0x4fe` 다른 길 — 우리 APC 는 아님.
- IRQL 2 (`+0xed`), B 의 `ThreadLock` (`+0x110`). **넣어도 되나**: `+0x134 ApcQueueable` 꺼짐 → `+0x394 DIL = 0` (FALSE) · `+0x141 Inserted` 이미 켜짐 → FALSE.
- `+0x16a KiInsertQueueApc` → `+0x18a` 대상이 지금 CPU 의 나 자신이면 `+0x296` 다른 길 · `+0x194` 유저 APC 면 `+0x408`.
- **`+0x19c KernelApcPending = 1`** → `+0x1a8 State`:
  - 2 (Running) → `+0x476` 소프트웨어 인터럽트만 (`+0x49a KiRequestSoftwareInterrupt` / 다른 CPU 면 `+0x48e KiSendSoftwareInterrupt`)
  - 5 (Waiting) 이 아니면 → `+0x24f` 아무것도 안 하고 TRUE
- **깨우기 조건** (5 일 때): `+0x1bf WaitIrql == 0` · `+0x1cb SpecialApcDisable == 0` · `+0x1d5 NormalRoutine == 0` 이면 다음 둘 건너뜀 (보통 APC 면 `+0x39c KernelApcDisable == 0` · `+0x3aa KernelApcInProgress == 0`) · `+0x1e0 ~ +0x1ea` `WaitRegister.State` 1 → 깨움, 아니면 `+0x4de CMP 4 / +0x4e1 JNZ +0x24b` (4 면 깨움, 그 밖은 `Apc` 비트만).
- **깨우는 일**: `+0x1f0 ~ +0x209` 큐 동시성 카운트 → **`+0x210 KiRemovePrcbWaitEntry`** → **`+0x218 KiEnterDeferredReadyState`** (`State = 7` — 이 함수 안 `+0x31` · `+0x41` · `+0x4e`; Waiting 이었으면 앞에서 기다린 시간도 더함 `+0xf ~ +0x3a`) → `+0x21d ~ +0x22e` `PRCB+0x2d08` push → **`+0x235 WaitStatus = 0x100`** → `+0x240 AbWaitObject = 0` → `+0x24b WaitRegister.Apc`.
- 끝: `+0x24f DIL = 1` → `+0x254` 스레드 락 풀기 → **`+0x26b KiExitDispatcher(Prcb, 0, 1, 2, 옛 IRQL)`** → (sast/73) `KiProcessThreadWaitList`: 블록마다 `+0x155 BlockState == 4` 인 것을 `+0x15f ~ +0x16f` 객체 목록에서 뗌 (`KeInsertQueueApc` 는 `BlockState` 를 안 바꾸므로 여기서 뗌) · `+0x98 AdjustReason = 1` · `+0x9f AdjustIncrement = 2` · `+0x101 KiDeferredReadySingleThread`.
- **신호는 안 건드린다**: 이 길에 이벤트 `SignalState` 를 쓰는 줄이 없다.
- **`KiSignalThreadForApc` 도 같은 몸** (`+0x3c` `KernelApcPending` · `+0x69` `WaitIrql` · `+0x75` `SpecialApcDisable` · `+0x83` `NormalRoutine` · `+0x8e ~ +0x98` `WaitRegister.State` · `+0xbe KiRemovePrcbWaitEntry` · `+0xfc State = 7` (여기는 몸에 풀어 넣음) · `+0x103 ~ +0x114` push · **`+0x11b WaitStatus = 0x100`** · `+0x131` `Apc` 비트). 차이는 `R9B` 비트 0 (`+0x57/+0x5b` 켜져 있으면 안 깨움) 뿐. 끝내기 쪽 `KeRequestTerminationThread +0x89` · `KiSchedulerApc +0x201` 은 둘 다 `R9D = 0` 으로 부른다 (`+0x7d` · `+0x1f5 XOR R9D,R9D`) → 같은 조건으로 같은 `0x100` (보통 APC 두 칸은 위 덧붙임 ①).
- **경쟁**: 위 표 2(b).

### B 쪽 — 깨난 뒤 (질문 3)

- **(a) 배달 자리**: `SwapContext` 꼬리 `+0x5c1 KernelApcPending == 1` · `+0x5ca ~ +0x5d0` 옛 IRQL 과 `SpecialApcDisable` 둘 다 0 → `+0x5e6 SETZ AL` (하나라도 아니면 `+0x5de HalRequestSoftwareInterrupt(1)`, AL 0). `KiSwapThread +0x24b ESI = AL` → **`+0x25c RDI = [R13+0xc8]` (`0x100`, APC 전에 읽음)** → `+0x263` `TimerActive` 떼기 (run78) → `+0x277 JNZ +0x2ca` → IRQL 1 → **`+0x2ee KiDeliverApc(0, 0, 0)`** → `+0x2f3 JMP +0x279` → `+0x29a RAX = RDI` → `RET`.
- `KiDeliverApc` 특별 APC 길: `+0xa5` 커널 목록 → `+0x10a` KAPC → `+0x111 NormalRoutine` · `+0x115 KernelRoutine` → **`+0x135 TEST / +0x138 JZ +0x2b1`** (`NormalRoutine 0`) → 목록에서 뗌 · APC_LEVEL · `+0x4f7 KernelApcInProgress |= 2` → 알려진 루틴들과 비교 → 마지막 **`+0x76f` 간접 호출** → `+0x52b` `&= ~2` → `+0x535 JMP +0xac` (목록 다시 봄).
- 그 밖에 커널 APC 를 배달하는 자리 (이름만, 결과서): `KiExitDispatcher +0xa2` · `KiExitThreadWait +0x242` (커밋 전 깨움) · `KeWaitForSingleObject +0x708` · `+0xb82` · `+0xefc` · `KiApcInterrupt` · `KiCheckForKernelApcDelivery +0x2d` · `KiCheckForThreadDispatch +0x61`.
- **(b) 트랩 프레임 칸**: `PspGetSetContextSpecialApc +0x64 PspGetSetContextInternal` (B 문맥):
  - 유저 호출: `+0xa4 RDI = [B+0x28]` · `+0xb2 − 0x190` = B 의 **맨 아래 트랩 프레임** (syscall 로 들어온 그 프레임).
  - `+0xb9` 꾸러미 비트 1 (`NtSetContextThread` 다섯째 인자 1 이 켬) → 스택을 거슬러 올라 (`+0x298 RtlVirtualUnwind2` 루프, `+0x2af/+0x2b3` 트랩에 닿을 때까지 `[추측: 닿는 조건]`) **커널 프레임에 흩어진 유저 비휘발 레지스터 자리** 를 포인터 표에 모음.
  - `+0x2c0/+0x2c4` SET → 검증 (CET·XSTATE, 이름만) → **`+0x3d2 SetContextState == 0`** → **`+0x3ea PspSetContext(트랩, 포인터 표, 컨텍스트, 모드)`**.
  - `PspSetContext` 가 쓰는 칸:

    | 플래그 | 줄 | 칸 |
    |---|---|---|
    | CONTROL (`+0x28 AND 0x100001 / +0x35`) | `+0x3b ~ +0x5a` · **`+0x68`** · **`+0x76`** · `+0x87 ~ +0xa2` | `EFlags` (트랩 `+0x178`, 유저면 걸러서 IF 켬) · **`Rip` (`+0x168`)** · **`Rsp` (`+0x180`)** · `SegSs`/`SegCs` |
    | INTEGER (**`+0xc4 AND 0x100002 / +0xce`**) | **`+0xd4 / +0xd8`** · `+0xdc ~ +0x10f` | **`Rax` (`+0x30`)** · `Rcx` · `Rdx` · `R8 ~ R11` |
    | 〃 | `+0x11e ~ +0x1a3` | `Rbx` `Rbp` `Rsi` `Rdi` `R12 ~ R15` → 포인터 표가 가리키는 **커널 스택 칸** |
    | FLOATING_POINT (`+0x1ba`) | `+0x1ca ~ +0x2c7` | `Xmm0 ~ 5` (트랩) · `Xmm6 ~ 15` (포인터 표) · `MxCsr` |
    | DEBUG_REGISTERS (`+0x2d3`) | `+0x2f2 ~ +0x35b` | 이름만 |

  - INTEGER 가 없으면 `+0xce JNZ +0x1a6` 로 `Rax` 칸을 **안 건드린다**.
  - 끝: `+0x2f8 EAX = 0` → **`+0x305 [꾸러미+0x5c] = 0`** → `+0x32e` 남이면 이벤트 주소를 돌려줌 → `PspGetSetContextSpecialApc +0x86 KeSetEvent` (A 깸).
- **(c) 다시 기다림**: `KiSwapThread` 가 `0x100` → `KiCommitThreadWait +0x225 ~ +0x23b` → `KeWaitForSingleObject +0x846` → **`+0x860 CMP R14D,0x100`** → IRQL 2·`WaitIrql` 복원 → **`+0x891 JMP +0x104`**. 다시 `+0x154 KernelApcPending` (이제 0) → `+0x1a6 State = 5` → **신호 검사가 마감 검사보다 먼저** (run78) — 그 사이 신호가 왔으면 `+0x5f0` 에서 가져가고 `R14D = 0`.
  - 상대 마감은 `+0x3b6 ~ +0x3d7` 에서 한 번만 계산 (`+0x3de JMP +0x104` 가 반복 지점 아래) → 반복해도 처음 절대값. APC 가 오래 걸려 지났으면 `0x102`.
  - `KeWaitForMultipleObjects`·`KeDelayExecutionThread` 의 `0x100` 반복은 안 읽음 `[공백]`.

### B 가 결국 깰 때 · syscall 출구 (질문 4)

- **(a) 신호 먹음**: 다시 도는 기다림은 평범하다 — `+0x5f0 AND AL,7 / +0x5f4 JZ +0x645` → `+0x645 [R12+4] = 0` (자동 이벤트), 뮤턴트는 `+0x3e3 ~` (run77). 결과서 grep: `KeWaitForSingleObject`·`KiCommitThreadWait`·`KiSwapThread`·`KiTryUnwaitThread`·`KeInsertQueueApc`·`KiDeliverApc` 에 `0x6a8`·`0x8000000` 0건 (`+0x3fd CMP EAX,0x80000000` 은 뮤턴트 재귀 한도).
- **(b) 유저 `RAX`**: `KiSystemServiceStart +0x193 CALL RAX` (ret `+0x195`) → `+0x1a0` 출구 → `+0x1fb UserApcPending` 0 이면 `+0x202 JZ +0x25d` → **`+0x25d MOV [RBP-0x50],RAX`** (트랩 `Rax` ← 서비스 반환값) → **`+0x291 TEST [RCX],0x8000000 / +0x297 JZ +0x2d8`** (보통 꺼짐) → `+0x540 RAX = [RBP-0x50]` → `+0x5bb SYSRET`. **나중에 쓰는 쪽은 `+0x25d`** — APC 가 쓴 문맥 `Rax` 는 사라진다.
- **(b') 갈래 2 — `SetContextState` 있는 스레드**: `PspGetSetContextInternal +0x3da JNZ +0x610` → (`+0x610` 유저 모드 · `+0x61a` 꾸러미 비트 1 · `+0x625 [B+3] & 8`) → `PspSetContextState`: `+0x1a TEST [RCX+3],8` · `+0x21 R14 = [RCX+0x6a8]` · 이미 서 있으면 `+0x28 JNZ +0x77` · 처음이면 확장 문맥 준비 → **`+0x79 BTS.LOCK [RBP],0x1b`** → `+0x9a RtlCopyContext` (칸에 합쳐 둠). 출구: `+0x297` 안 건너뜀 → `+0x299 [RBP-0x50] = RAX` (기다림 결과) → `+0x29f ~ +0x2cf` 트랩 `Rcx ~ R11`·`Xmm0 ~ 5` 를 0 → **`+0x2d3 KiRestoreSetContextState`**: `+0x72 [Thread+0x6a8]` → **`+0x90 KeContextToKframes`** → `KxContextToKframes +0xc9 AND 0x100002` · **`+0xd9 / +0xdd` 트랩 `Rax` ← 문맥 `Rax`** → `+0xa8 BTR` 비트 27 끔 → (유저면) 조각 `+0x393 RAX = [RBP-0x50]` · `+0x3c8 SWAPGS` · **`+0x3cb IRETQ`** (`KiKvaShadow` 면 `+0x3b1 JMP KiKernelExit`). 이 갈래는 **나중에 쓰는 쪽이 `KxContextToKframes`** — INTEGER 를 넣었으면 유저 `RAX` = 문맥 값, 안 넣었으면 기다림 결과, `RCX`·`R11` = 문맥 값 또는 0.
- **어느 갈래가 보통인가** `[정적: 전수 grep]` (결과서):
  - `Thread+0x6a8` 에 값을 넣는 곳은 **`PspAllocateThread +0x2caa31` 하나** (`ExAllocatePool2 … 'PsSc'`). 오는 길: `+0x5b3 JZ` (스택 인자 `[RSP+0x70] == 0`) → `Process.PicoContext ≠ 0`. 풀고 (`ExFreePoolWithTag`, 태그 `PsSc`) 칸을 되쓰는 곳은 `PspExitThread +0x85f` · `PspAllocateThread +0x2ca940` (만들기 실패 되돌림). 나머지 `+1704` 는 다른 구조체.
  - 헤더 비트 27 을 켜는 곳은 **`PspSetContextState +0x79` 하나**, 끄는 곳 `KiRestoreSetContextState +0xa8`. (`or …, 0x8000000` 6곳은 다른 구조체로 읽힘 `[추측: 이름만 보고 가름]`.)
  - → **보통 Win32 스레드의 `NtSetContextThread` 는 갈래 1** (그 자리에서 고침 → `+0x25d` 가 `RAX` 덮음 → `SYSRET`).
  - `[관찰]` (flow-writer 사진 대조) run44 메인 스레드: `SetContextState` 일곱 장 `(null)`, 헤더 비트 27 일곱 장 꺼짐 (위 덧붙임 ②). 보통 갈래와 맞다.
  - 못 닫은 것: `NtCreateThreadEx` 가 `PspAllocateThread` 의 그 스택 인자에 늘 0 이 아닌 값을 넘기는지 — 결과서는 `NtCreateThreadEx +0x14c LEA R14,[RSP+0x70]` · `+0x282` 한 갈래만 봄 `[공백]` (Worker 스레드는 동적 1).
- **(c) `RCX`·`R11`** (갈래 1): `+0x56c RCX = [RBP+0xe8]` (트랩 `Rip`) · `+0x573 R11 = [RBP+0xf8]` (`EFlags`) · `+0x544 R8 = [RBP+0x100]` (`Rsp`) · `+0x552 EDX = 0` · `+0x554 ~ +0x568 Xmm0 ~ 5 = 0` · `+0x58a RSP = R8` · `+0x5b8 SWAPGS` · `+0x5bb SYSRET`. 비휘발 `Rbx Rdi Rsi Rbp` 는 `+0x1a0 ~ +0x1ae` 에서 트랩 칸을 읽어 복원.

### A 가 받는 값 (질문 5)

- 성공 `0`: `PspGetSetContextInternal +0x2f8 / +0x305` → `PspSetContextThreadInternal +0x154` → `+0x167` → `NtSetContextThread +0xc7` → `+0x91`.
- **B 가 끝나는 중**: `PspExitThread +0x4e3 KeRundownApcQueues` 가 `+0x1a BTR ApcQueueable` → 그 뒤 들어온 `KeInsertQueueApc` 는 `+0x134/+0x13b` FALSE → `0xc0000001`. 이미 큐에 들어간 뒤 B 가 끝나는 사이를 막는 줄은 `+0x1f KiAcquireReleaseThreadLock` · `+0x2f KeLeaveCriticalRegionThread` (APC 배달) 로 보이나 본문 안 읽음 `[추측]` `[공백]`. 커널 APC 가 남아 있으면 `KeRundownApcQueues +0x96 ~ +0xb3 KeBugCheckEx(0x20)`.
- B 의 APC 안에서 실패 (스택 거슬러 오르기 `+0x504`, 검증 `+0x39e`·`+0x58e`·`+0x3cc`) 면 그 코드가 꾸러미 `+0x5c` 로 돌아온다.

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리.

```
[A — 문맥을 넣는 스레드]
nt!NtSetContextThread (핸들, ctx)
 │  +0x63 / +0x6f / +0xa4  같은 프로세스 제한 · SystemThread · Pico → 0xc000060a / 0xc0000008
 ├─ (1) +0x4d  ObReferenceObjectByHandle(THREAD_SET_CONTEXT 0x10)
 └─ (2) +0xc7  PspSetContextThreadInternal(B, ctx, 모드, 모드, 1)
      ├─ (1) +0xa1   RtlpSanitizeContextFlags
      ├─ (2) +0x257  RtlpReadExtendedContext               유저 CONTEXT → A 커널 스택 버퍼
      │  +0xf7 / +0xfa  B == 나 ? 예 → +0x136 PspGetSetContextSpecialApc 직접 (run16)
      ├─ (3) +0x278  KeInitializeEvent(꾸러미+0x60, 알림형, 꺼짐)
      ├─ (4) +0x2ae  KeInitializeApc(…, PspGetSetContextSpecialApc, 0, 0, 커널)   특별 커널 APC
      ├─ (5) +0x2c4  KeInsertQueueApc(apc, 1, B, 2)          FALSE → +0x33e 0xc0000001
      │    │  +0x134 ApcQueueable · +0x19c KernelApcPending = 1
      │    │  +0x1b7 State == 5 · +0x1bf WaitIrql 0 · +0x1cb SpecialApcDisable 0 · +0x1e0 ~ +0x1ea 커밋됨
      │    ├─ (1) +0x16f  KiInsertQueueApc
      │    ├─ (2) +0x215  KiRemovePrcbWaitEntry(B)
      │    ├─ (3) +0x21d  KiEnterDeferredReadyState(B)     +0x31 / +0x41 / +0x4e State = 7
      │    │  +0x21d ~ +0x22e PRCB+0x2d08 push · +0x235 WaitStatus = 0x100 · +0x24b WaitRegister.Apc
      │    └─ (4) +0x270  KiExitDispatcher(Prcb, 0, 1, 2, …)
      │         └─ KiProcessThreadWaitList   +0x155 ~ +0x16f B 블록을 이벤트 목록에서 뗌 · +0x9f AdjustIncrement 2 → +0x106 KiDeferredReadySingleThread (run77)
      └─ (6) +0x2e2  KeWaitForSingleObject(이벤트, 0, 커널, FALSE, NULL)   A 잠 → 깨면 +0x154 EBX = [꾸러미+0x5c]

[B — 기다리던 스레드, 다시 돌 차례]
KiSwapThread (run75)
 ├─ (1) +0x248  KiSwapContext → SwapContext +0x5c1 ~ +0x5e6 AL = 커널 APC 지금 배달
 │  +0x25c  RDI = WaitStatus (0x100)   ← APC 보다 먼저
 └─ (2) +0x2f3  KiDeliverApc(0, 0, 0)
      │  +0x138 NormalRoutine 0 → 특별 APC 길 · +0x4f7 KernelApcInProgress |= 2
      └─ +0x774  (간접, guard_dispatch_icall) PspGetSetContextSpecialApc     [공백 → 동적: 간접 호출]
           ├─ (1) +0x69  PspGetSetContextInternal(꾸러미, 1, &이벤트칸)
           │    │  +0xa4 / +0xb2 트랩 = InitialStack − 0x190 · +0xb9 → 스택 거슬러 오르기 (+0x29d RtlVirtualUnwind2)
           │    │  +0x2c0 SET · +0x3d2 SetContextState == 0 (보통)
           │    └─ +0x3ef  PspSetContext    +0x68 Rip · +0x76 Rsp · +0xd8 Rax (INTEGER) … · +0x305 상태값 0
           └─ (2) +0x8b  KeSetEvent(A 의 이벤트, 1, FALSE)       → A 깸
 │  +0x29a RAX = 0x100 → KiCommitThreadWait +0x225 → KeWaitForSingleObject +0x860 / +0x891 JMP +0x104 (처음부터, 같은 마감)
 │  … 신호면 +0x5f0 먹고 R14D = 0 / 마감이면 0x102 → +0x2c4 EAX
KiSystemServiceStart
 │  +0x25d  트랩 Rax ← 기다림 결과 (APC 가 쓴 Rax 덮음)
 │  +0x291 / +0x297  헤더 비트 27 꺼짐 (보통) → 건너뜀
 └─ ══▶ +0x540 RAX · +0x56c RCX = 새 Rip · +0x573 R11 = 새 EFlags · +0x5bb SYSRET   (유저 새 Rip)

[B — 헤더 비트 27 갈래 (SetContextState 있는 스레드 = Pico 쪽만)]
PspGetSetContextInternal +0x3da → 조각 FUN_140b2a1a6 +0xc PspSetContextState   +0x79 BTS 비트 27 · +0x9f RtlCopyContext
KiSystemServiceStart +0x299 트랩 Rax ← 기다림 결과 · +0x29f ~ +0x2cf Rcx ~ R11 = 0
 └─ +0x2d8  KiRestoreSetContextState          돌아오지 않음
      ├─ (1) +0x95  KeContextToKframes ─ +0x7d KxContextToKframes   +0xdd 트랩 Rax ← 문맥 Rax (INTEGER)
      │  +0xa8 BTR 비트 27
      └─ ══▶ (유저) 조각 +0x393 RAX · +0x3c8 SWAPGS · +0x3cb IRETQ   (KiKvaShadow → +0x3b1 KiKernelExit)
```

## 확인된 것

- `[정적]` 남이면 `PspSetContextThreadInternal +0x2a9` 특별 커널 APC (`KernelRoutine = PspGetSetContextSpecialApc`, `NormalRoutine 0`) → `+0x2bf KeInsertQueueApc(…, 1, B, 2)` → `+0x2dd KeWaitForSingleObject` (마감 없음). 넣기 실패면 `+0x33e 0xc0000001`.
- `[정적]` `KeInsertQueueApc` 는 `Waiting` · `WaitIrql 0` · `SpecialApcDisable 0` · 커밋됨 인 B 를 그 자리에서 깨운다: `+0x210 KiRemovePrcbWaitEntry` → `+0x218 KiEnterDeferredReadyState` (`State = 7`, 그 함수 `+0x31`·`+0x41`·`+0x4e`) → `+0x235 WaitStatus = 0x100`. 객체 신호는 안 건드림. 블록은 `KiExitDispatcher → KiProcessThreadWaitList` 가 뗌.
- `[정적]` `KiSignalThreadForApc` 도 같은 몸 (`+0xfc State = 7` · `+0x11b WaitStatus = 0x100`), `R9B` 비트 0 만 다름. 끝내기 쪽 두 곳은 `R9D = 0`.
- `[정적]` B 는 `KiSwapThread +0x25c` 에서 `0x100` 을 먼저 읽고 `+0x2ee KiDeliverApc` 로 APC 를 돌린 뒤 그 값을 돌려준다 → `KeWaitForSingleObject +0x860 / +0x891` 처음부터.
- `[정적]` APC 안 `PspGetSetContextInternal +0x3ea PspSetContext` 가 맨 아래 트랩 프레임을 고친다 — CONTROL 이면 `Rip` (`+0x68`)·`Rsp` (`+0x76`)·`EFlags`, INTEGER 면 `Rax` (`+0xd8`) 등. 플래그에 없는 칸은 안 건드림.
- `[정적]` 보통 출구 `KiSystemServiceStart +0x25d` 가 트랩 `Rax` 를 서비스 반환값으로 덮고, 헤더 비트 27 이 꺼져 있으면 (`+0x291/+0x297`) `SYSRET` — 유저 `RAX` = 기다림 결과, `RCX` = 새 `Rip`.
- `[정적: 전수 grep]` `SetContextState` 에 값을 넣는 곳은 `PspAllocateThread +0x2caa31` 하나 (Pico 쪽 갈래), 헤더 비트 27 을 켜는 곳은 `PspSetContextState +0x79` 하나.
- `[관찰]` run44 메인 스레드: `SetContextState` 일곱 장 `(null)`, 헤더 비트 27 일곱 장 꺼짐 (K2 ~ P2).
- `[정적]` A 는 성공 `0` 을 B 가 APC 를 돌린 뒤 받는다. B 가 끝나는 중 (`ApcQueueable` 꺼짐, `KeRundownApcQueues +0x1a`) 이면 `0xc0000001`.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① **오케스트레이터 확인 부탁 — "`KeInsertQueueApc +0x210 ~ +0x240` 이 `State = 7` 로 바꾼다"**: `KeInsertQueueApc.asm` 안에는 `[RSI+0x184]` 에 **쓰는 줄이 없다** (`+0x1a8` · `+0x408` 두 곳 모두 `MOVZX` 읽기). `State = 7` 은 **`+0x218` (ret `+0x21d`) 로 부르는 `KiEnterDeferredReadyState` 안** `+0x31` · `+0x41` · `+0x4e` `MOV byte [RCX+0x184],0x7` 이다 (85바이트 도우미, sast/73 이 "State = 7 만 쓰는 도우미" 라 적은 그것). 결과서 본문 (`+0x218 KiEnterDeferredReadyState(B)` (State=7, sast/73)) 은 맞고, 시간 순서 표 5번의 "`State=7`" 만 줄 자리가 빠졌다 — 위 순서 표는 그 자리를 넣어 적었다. 반대로 `KiSignalThreadForApc` 는 같은 일을 몸에 풀어 넣어 `+0xfc` 에서 직접 쓴다.
- ② flow-writer 보탬: 보통 커널 APC 의 두 칸 (`KernelApcDisable` · `KernelApcInProgress`) — 위 덧붙임 ①. 결과서 "스레드 끝내기 APC 와 문맥 넣기 APC 는 같은 규칙 하나" 에 붙는 조건.
- ③ flow-writer 보탬: `KeInsertQueueApc +0x8d` 의 루틴 `0x140ab1f40` 이름 = `KeSpecialUserApcKernelRoutine` (결과서 `[공백]`) — 덧붙임 ③.
- ④ flow-writer 보탬: 사진 대조 (`SetContextState` · 헤더 비트 27) — 덧붙임 ②. 결과서 동적 후보 첫 줄의 메인 스레드 몫은 이것으로 `[관찰]` (초기화 구간). Worker 몫만 남는다.
- ⑤ flow-writer 보탬 (run77 정정 ② 의 근거): `KiRestoreSetContextState` 의 `IRETQ` 는 둘이다 — `+0x12d TEST byte [RBP+0xf0],1` (트랩 `SegCs` 하위 비트) `/ +0x134 JZ +0x3cd` 로 갈려, **유저로 갈 때는 조각 `FUN_1406aecef` 의 `+0x393 MOV RAX,[RBP-0x50]` · `+0x3c8 SWAPGS` · `+0x3cb IRETQ`**, 커널 모드로 돌아갈 때는 `+0x401 MOV RAX,[RBP-0x50]` · `+0x416 IRETQ` (SWAPGS 없음). 결과서 4(b) 는 유저 쪽을 맞게 적었다.
- ⑥ flow-writer 보탬: `KeInitializeApc +0x43 CMOVZ` — `NormalRoutine 0` 이면 `ApcMode` 를 0 (커널) 으로 강제. 결과서 "ApcMode = 0" 의 이유가 인자 0 하나만이 아니다.

## 정정

- **run77 (덧붙임 ① 끝과 "결과서와 다르게 적은 곳" ⑤ 끝)** — "`KiRestoreSetContextState` (`KiSystemServiceStart +0x2d3`) 는 출구에서 스레드 헤더 비트 `0x8000000` 이 설 때만 — **남이 문맥을 넣은 경우 (Q141e) 로 보인다** `[추측]`"
  - → **보통 스레드에 남이 문맥을 넣어도 이 길은 안 탄다** `[정적]`. 비트 27 을 켜는 곳은 `PspSetContextState +0x79` 하나이고, 거기 오는 건 `_ETHREAD.SetContextState (+0x6a8)` 가 있는 스레드뿐이다. 그 칸에 값을 넣는 곳은 Pico 쪽 `PspAllocateThread +0x2caa31` 하나 (결과서 전수 grep). 보통 스레드는 APC 안 `PspGetSetContextInternal +0x3ea PspSetContext` 가 트랩을 그 자리에서 고치고 `SYSRET` 길로 간다 — 유저 `RAX` 는 `+0x25d` 가 덮은 서비스 반환값.
  - 같은 문서의 호출 표·`RAX` 절의 "돌아오지 않고 `KiRestoreSetContextState +0x416 IRETQ` (앞 `+0x401 MOV RAX,[RBP-0x50]`)" → 유저로 가는 `IRETQ` 는 **조각 `+0x3cb`** (앞 `+0x393` · `+0x3c8 SWAPGS`), `+0x416` 은 트랩 `SegCs` 가 커널일 때 (`+0x12d/+0x134`) — 위 ⑤. "트랩 `Rax` 칸에서 `RAX` 를 읽는다" 는 결론은 그대로.
  - run77 문서 맨 위에 정정 안내 한 줄, 덧붙임 ① 끝과 호출 표 `+0x2d3` 줄에 `(run80 정정)` 표시를 넣었다.
- **요청서 sast/76 질문 3(b) 의 사슬 `PspGetSetContextSpecialApc → PspGetSetContextInternal → KeContextToKframes`** (결과서 맺음 "run16 정정")
  - → 마지막 고리는 **`PspSetContext` (`PspGetSetContextInternal +0x3ea` / ret `+0x3ef`)** 다. `KeContextToKframes` 를 부르는 곳은 `KiRestoreSetContextState +0x90` · `KiContinueEx +0x1e0` · `KiContinuePreviousModeUser +0x422` (`_calls.tsv`).
  - **run16 문서와 flow.md 에는 이 틀린 사슬이 없다** — run16 은 호출 표 (`PspGetSetContextInternal +0x3ea / +0x3ef PspSetContext`), 2.3 ⑤ 절, 호출 관계 (`+0x3ef PspSetContext +0x68`) 모두 이미 `PspSetContext` 로 적었고, flow.md 2.3 `PspInitializeThunkContext` 8번 줄도 `+0x3ef PspSetContext` 다. run16 의 `KeContextToKframes` 줄 (`KiContinuePreviousModeUser +0x422`, `KiContinueEx +0x1e0` "이름만 본 후보") 은 `NtContinue` 얘기라 맞다. 틀린 사슬은 요청서 문장에만 있었다 → **run16 문서·flow.md 는 고치지 않았다** (정정 안내도 안 넣음).

## run16 · run77 · run79 에 반영한 것

- run16: 없음 (위 "정정" 둘째 — 고칠 줄이 없음).
- run77: 맨 위 정정 안내 한 줄 · 덧붙임 ① 끝 · 호출 표 `KiSystemServiceStart +0x2d3` 줄 에 `(run80 정정)`. `RAX` 절의 `[공백] → Q141e` 줄 · "결과서와 다르게 적은 곳" ⑤ 끝 · "아직 확인 필요" 정적 ⑤ 에 "(→ run80 …)" (닫힌 몫 `KiRestoreSetContextState`·`KeContextToKframes`, 남은 몫 `KiInitiateUserApc`).
- run79: "남의 스레드 끝내기" (a) 의 `[추측: 중간 한 걸음]` 줄 끝과 "아직 확인 필요" 정적 ① 에 run80 이 닫은 몫·남은 몫 (아래 정적 ①).

## 짐 지도에 반영한 것 (짐 지도 바퀴 아님)

- 남의 스레드 문맥 넣기는 `main` 뒤 일이라 (test.exe 는 `SetThreadContext` 를 안 부름 `[소스]`) **새 짐 칸 없음, 칸 수 그대로.** 이미 있는 줄에 근거만:
  - 3.13 `+0x6a8 SetContextState` — 값을 넣는 곳은 바이너리 전체에서 `+0x2caa31` 하나 (결과서 전수 grep), 풀고 칸을 되쓰는 곳 `PspExitThread +0x85f` · `PspAllocateThread +0x2ca940` (앞 줄이 `ExFreePoolWithTag` `PsSc`), 읽는 곳 `PspGetSetContextInternal +0x3d2` · `PspSetContextState +0x21` · `KiRestoreSetContextState +0x72`. 사진 일곱 장 `(null)` 줄 번호.
  - 3.14 `+0x0 Header*` — 첫 DWORD 비트 27 (`0x8000000`): 켜는 곳 `PspSetContextState +0x79` 하나, 끄는 곳 `KiRestoreSetContextState +0xa8`, 보는 곳 유저로 돌아가는 출구들. 사진 일곱 장 꺼짐.
  - 3.14 `+0x288 SchedulerApc` — 보통 커널 APC 라 깨울 때 두 칸 더 (`KiSignalThreadForApc +0x219` · `+0x227`), 배달은 `KiDeliverApc +0x20e / +0x21b KiSchedulerApc` 직접 → `+0x241 JMP +0xac`.
  - 3.14 머리 메모 "(run80) 문맥 넣기 줄" (은퇴 뒤라 표에 안 넣음).
  - 기준 근거 목록·판 목록 (서른여섯째판)·8장 두 표에 한 줄씩.

## 아직 확인 필요

동적 — **Q104 VM 단계** 에 붙인다 (오케스트레이터. 이번 묶음은 VM 안 함). 커널 멈춤점은 모든 프로세스에 걸리니 test.exe 스레드일 때만. WinDbg 이름은 `nt!KeWaitForSingleObject`.

1. **(싸다, 새 샘플 없이)** test.exe **Worker** 스레드의 `SetContextState` 가 0 인가, 헤더 비트 27 이 꺼져 있나 — `dt nt!_ETHREAD <Worker> SetContextState` · `dd <Worker> L1` (`& 0x8000000` = 0 기대). **메인 스레드는 run44 사진으로 이미 `[관찰]`** (일곱 장 `(null)` · 꺼짐) 이니 Worker 만. Worker 를 `PspAllocateThread` 의 어느 갈래가 만들었는지도 같이 (`nt!PspAllocateThread+0x5ab` 에서 `poi(@rsp+0x70)` ≠ 0 기대 → `+0x5ea PspSetupUserStack` 길).

**새 샘플이 있어야 닫히는 것** — test.exe 에는 `SetThreadContext` 가 없다 `[소스]` (`sample/test.c`). 스레드 둘: B 는 `WaitForSingleObject(ev, INFINITE)` 중, A 가 `SetThreadContext(B, {Rip = X, Rsp = Y, Rax = 0x1234, ContextFlags = CONTROL | INTEGER})` → 그다음 A 가 `SetEvent(ev)`. 멈춤점 (모두 그 프로세스 조건):

2. `nt!KeInsertQueueApc` 입구 (`rdx == 1`, 부른 스레드 = A) — `dt nt!_KAPC @rcx` 의 `+0x20` = `nt!PspGetSetContextSpecialApc`, `+0x30` = 0, `r9` = 2.
3. `nt!KeInsertQueueApc+0x240` (`+0x235` 쓰기 직후) — `dq <B>+0xc8 L1` = `0x100`, `db <B>+0x184 L1` = 7 (`KiEnterDeferredReadyState` 뒤라 이미 7), 같은 때 `dd <ev>+4 L1` (`SignalState`) 그대로.
4. `nt!PspSetContextThreadInternal+0x2dd` — A 가 정말 자나 (`k`, 그 뒤 `+0x2e2` 에 B 의 `KeSetEvent` 뒤에 오나).
5. `nt!KiSwapThread+0x2ee` (부른 스레드 = B) → `nt!KiDeliverApc+0x76f` 의 `rax` = `nt!PspGetSetContextSpecialApc` (간접 호출 대상 — 지금 `[공백]`).
6. `nt!PspSetContext` 입구·뒤 — `dt nt!_KTRAP_FRAME @rcx Rip Rsp Rax` (뒤에 X·Y·`0x1234`).
7. `nt!KeWaitForSingleObject+0x860` (B) `r14d == 0x100` → `+0x891` 로 다시 도나. A 의 `SetEvent` 뒤 `+0x645` (자동 이벤트면) 로 신호를 먹나.
8. `nt!KiSystemServiceStart+0x25d` 직전 `rax` (= 기다림 결과 0) 와 직후 `dq @rbp-0x50 L1`, `+0x291` 에서 `dd @rcx L1` (비트 27 꺼짐), 유저로 돌아간 뒤 첫 명령에서 `rax` = 0 (`0x1234` 아님) · `rcx` = X · `r11` = 새 EFlags.
9. 경쟁 (신호가 먼저 / APC 가 먼저) 은 CPU 둘 이상이라 순서가 런타임 — 2 ~ 8 을 여러 번.

정적 — 다음 바퀴 후보 (대기열 번호는 오케스트레이터가):

- ① **Q147 ① (run79 "`KiSchedulerApc` ↔ `KiDeliverApc` 중간 한 걸음") — 일부만 닫힘.**
  - 닫힌 몫 `[정적]`: 끝내기 APC 를 넣는 `KiSignalThreadForApc` 의 깨우기 규칙 = 이번 `KeInsertQueueApc` 와 같은 몸 (`+0xfc State 7` · `+0x11b WaitStatus 0x100`, `R9D = 0` 으로 불림 — 결과서 질문 2) + 보통 APC 두 칸 (`+0x219` · `+0x227`, flow-writer). 기다리던 대상이 깨난 뒤 배달 자리는 `KiSwapThread +0x2ee KiDeliverApc` (결과서 질문 3(a)). `KiDeliverApc` 는 `NormalRoutine == KiSchedulerApc` 면 **`+0x20e / +0x21b` 로 직접 부르고 돌아와 `+0x241 JMP +0xac` 로 목록을 다시 본다** (flow-writer `.asm`). `KiSchedulerApc` 안: `+0x1d4 / +0x1db KernelRoutine = KiSchedulerApcTerminate` · `+0x1df ApcMode = R13B` · `+0x1f0 KiInsertQueueApc` · `+0x201 KiSignalThreadForApc(R9D = 0)`.
  - 남은 몫: 다시 넣은 APC 가 **어느 목록 (`+0x1df` 의 `R13B` 값 — 커널/유저) 으로 들어가 어느 `KiDeliverApc` 호출의 어느 갈래로 `+0x443 ~ +0x46b KiSchedulerApcTerminate` 에 닿나** — `+0x46b` 는 `+0x3fb ~ +0x43d` 블록 (`+0x405 R13 = R8`) 뒤인데 그 블록으로 오는 길은 안 따라감 `[추측]`. 함수 둘 (`KiSchedulerApc +0x0 ~ +0x1df` 의 `R13`, `KiDeliverApc +0x392 ~ +0x443`) 만 보면 된다.
- ② 결과서 못 닫은 것 (1): `NtCreateThreadEx` → `PspCreateThread` → `PspAllocateThread` 의 스택 인자 (`[RSP+0x70]`) 가 보통 스레드에서 늘 0 이 아닌지 — `NtCreateThreadEx +0x14c` · `+0x282` 말고 다른 갈래 (run74 지도로). 동적 1 로도 닫힘.
- ③ 결과서 못 닫은 것 (2): `KeRundownApcQueues +0x2f KeLeaveCriticalRegionThread` · `+0x39 / +0x4b KiFlushQueueApc` — "B 가 끝나는 중에 이미 들어간 문맥 넣기 APC" 가 배달되나 버려지나 (버려지면 A 는 영영 잠? — `KeBugCheckEx(0x20)` 줄과 함께).
- ④ 결과서 `[공백]`: `KeWaitForMultipleObjects`·`KeDelayExecutionThread` (Sleep) 의 `0x100` 반복 — run77 **Q145** ② 와 묶을 만함.
- ⑤ 결과서 규칙 8: **유저 모드에서 돌던 B** (syscall 안이 아님) 에게 문맥을 넣을 때 — `KiApcInterrupt` → `KiDeliverApc` → … → `IRETQ` 에서 `Rax` 가 문맥 값으로 가나 (이 경우 덮는 `+0x25d` 가 없음). PE_emulator 가 "돌고 있는 스레드" 를 다룰 때 필요.
- ⑥ 작음: `PspGetSetContextInternal +0xa8 TEST [RDI+8],1 / +0xac JNZ +0x360` 루프의 뜻, `+0x2af/+0x2b3` 스택 걷기 끝 조건 — 에뮬레이터 규칙 7 로 건너뛰므로 급하지 않음.

## 디버거 메모 / 도구 메모

- 정적만이라 디버거 메모 없음.
- 도구 (결과서 제안, 오케스트레이터 몫): "구조체 칸 하나를 쓰는 곳 전부" (`+0x6a8` 같은 오프셋) 는 `run_ghidra.sh refs` 로 안 된다 — 결과서가 `llvm-objdump` 전체 풀기 + grep 로 했다. `refs` 에 오프셋 모드 (`--field 0x6a8`) 를 두자는 제안 (plan.md 결정 기록의 하네스 제안 ④, 보류).
- 도구: `KeInsertQueueApc` 처럼 "칸에 쓰는 줄" 이 **불린 도우미 안** 에 있을 때 (`State = 7` ← `KiEnterDeferredReadyState`) 는 caller `.asm` grep 으로 안 나온다. 같은 일을 몸에 풀어 넣은 쌍둥이 (`KiSignalThreadForApc +0xfc`) 가 있으면 둘을 나란히 보면 빨리 찾는다.
- 도구: `KiRestoreSetContextState` · `KiSystemServiceStart` 처럼 출구가 조각 (`FUN_…`) 과 본문에 나뉘어 `IRETQ`·`SYSRET` 이 여러 개인 함수는 갈림 줄 (`TEST [RBP+0xf0],1` = 트랩 `SegCs`) 을 먼저 찾아야 "유저로 가는 출구" 를 고를 수 있다 (run77 이 커널 쪽 `+0x416` 을 적은 까닭).

# 2026-11-12 run81 — CPU 예외를 유저 예외로 넘기기, 커널 쪽 (Q148a, PE_emulator 요청 26): **입구 (`KiPageFault` 등) 는 진짜 코드 대신 안쪽 표시 코드 (`0x1000000x`) 를 담아 `KiExceptionDispatch` 로 보낸다. `KiDispatchException` 이 커널 스택에서 문맥을 만들고 표시 코드를 진짜 코드로 푼 뒤, 유저 스택에 `CONTEXT` (`+0`) · `CONTEXT_EX` (`+0x4d0`) · `EXCEPTION_RECORD` (`+0x4f0`) · 머신 프레임 (`+0x590`) 을 한 덩어리로 쌓고, 트랩 프레임 `Rsp`·`SegCs`·`Rip` 만 바꿔 `IRETQ` 로 `ntdll!KiUserExceptionDispatcher` 에 보낸다. 둘째 기회·`__fastfail`·스택 쓰기 실패는 유저 쪽으로 안 가고 `ZwTerminateProcess(-1, 예외 코드)`.** `KiPageFault +0x386 MmAccessFault` (실패일 때만 아래로) → `+0x43d KiExceptionDispatch` → `+0x140 KiDispatchException` → `+0x13d KeContextFromKframes` → `+0x15e KiPreprocessFault` → (디버거 없음) `+0x2be KdTrap` · `+0x3d6 DbgkForwardException` → `+0x4ab ProbeForWrite` → `+0x4cc KeCopyExceptionRecord` → `+0x52f RtlpCopyExtendedContext` → `+0x578 Rsp` · `+0x583 SegCs` · `+0x593 Rip = PsNtdllExports+0x20` → `KiExceptionDispatch` 조각 `+0x4b4 IRETQ`

## PE_emulator 쪽에서 쓸 것

- **한 줄**: 지금 우리 nt 엔진의 "CPU 예외면 멈춤" 은 **틀린 게 아니라 없는 것** 이다 `[정적]`. 커널 쪽 흉내 하나 (아래 "최소 규칙" 9단계 — 코드·주소 정하기 → 유저 스택에 한 덩어리 쓰기 → `RSP`·`RIP` 바꾸기) 를 더하면, 그 뒤는 진짜 ntdll 이 한다 (Q148b, run82).
  - 메모리 syscall 이 잘못된 유저 포인터에서 **상태 코드를 돌려주는 것** 은 기본 길과 **맞다** (커널 모드 예외 → 커널 SEH). 다만 코드는 `0xC0000005` 하나가 아니다.
  - `Src/nt/Process.cpp:1953` 주석은 메모리 syscall 이야기가 아니라 **`PspInitializeThunkContext` (프로세스 시작 때 문맥 맡기기) 이야기** 이고, **반만 맞다**: `KiDispatchException` 로 가는 건 맞지만 **첫 기회 0** 이라 유저 처리기 없이 **둘째 기회 = 프로세스 종료** 다. "에뮬레이터 규칙: 멈춘다" 는 결과가 같다 (아래 "정정").
- 질문마다 (결과서 sast/78 그대로, 근거 줄은 오케스트레이터·flow-writer 가 `.asm` 으로 다시 봄). 원문 질문 4 (ntdll 본문)·7 (`RtlRaiseException`) 과 5 의 kernelbase 몫은 **Q148b (run82)** 다.

| 질문 (원문 요청 26 번호 · sast/78 번호) | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1 · 1 입구 → 디스패치 사슬 | 입구 `KiXxx` (손으로 쓴 asm) 가 트랩 프레임을 만들고 `ECX` 코드 · `EDX` 개수 · `R8` 주소 · `R9`/`R10`/`R11` 정보 를 채워 `KiExceptionDispatch` 를 부름 → 레코드 (`RSP+0x138`) 를 채우고 **첫 기회 = 1** 로 `KiDispatchException` (유저 모드면 곧장). 돌아오지 않고 `IRETQ`. `#PF` 는 먼저 `MmAccessFault` — **음수 (실패) 일 때만** 예외. `KiPreprocessFault` 는 입구가 아니라 `KiDispatchException` 안 (문맥 만든 뒤) | 사슬 통째로 **없음**. "Unicorn 오류·인터럽트 번호 → 입구" 표 하나 + 인자 채우기가 필요 | 입구 표 (아래 "결과" 질문 1), `KiExceptionDispatch +0x67 ~ +0x81` · `+0x8c` · **`+0x90`** · `+0x9f` · **`+0x140`**, `KiPageFault +0x386` · **`+0x38d JGE`** · `+0x43d`, `KiDispatchException +0x13d` · **`+0x15e`** |
| 2 · 2 `EXCEPTION_RECORD` | `ExceptionFlags` 0 (`__fastfail` 만 1), 링크 0. **`int3` 주소 = `int3` 자기 자리** (입구 `DEC R8`), 문맥 `Rip` 도 −1. AV `[0]` = 읽기 **0** / 쓰기 **1** / 실행 **8**, `[1]` = `CR2`. 특권 명령 `0xC0000096` (개수 0), `ud2` `0xC000001D`, 0 나누기 `0xC0000094` (몫 넘침은 `0xC0000095`) — 예외 코드 표 아래 | 없음 (멈춤). `UC_ERR_*_UNMAPPED` 읽기/쓰기/가져오기 ↔ 0/1/8, `UC_ERR_INSN_INVALID` 는 `#UD`/특권을 Unicorn 이 가르는지 모름 `[추측]` — 못 가르면 명령 바이트를 읽어야 함 | `KiBreakpointTrap +0x34d DEC R8` · `+0x350`, `KiDispatchException +0x14e` · `+0x707`, `KiPageFault +0x37e` · `+0x380` · `+0x383` · `+0x348` · `+0x358`, `KiPreprocessFault +0x9b` · `+0x1b5` · `+0x231` · `+0x1f8 ~ +0x203`, `KiOp_Priv +0x4` · `+0x8`, `KiOp_Div +0x104` |
| 3 · 3 유저 스택 모양 | **표 셋 아래** — 배치 계산 · `RSP` 기준 오프셋 표 · 트랩 프레임 바뀌는 칸. 커널 스택에서 문맥을 먼저 만들고 유저 스택엔 한꺼번에 복사. 새 `RSP` = `((A − 0x28) & ~0xF) − 0x590`, `RIP` = `PsNtdllExports+0x20` | 이 배치를 그대로 쌓으면 `KiUserExceptionDispatcher` 가 읽는 모양 (`RSP` = CONTEXT, `RSP+0x4f0` = 레코드) 과 **맞는다**. XSAVE 꼬리 없이 `0x10001f` 로 쌓는 길도 커널에 있다 (`KeFeatureBits` 비트 23 꺼짐 갈래) — 단 이 VM 은 켜짐 쪽으로 보인다 (덧붙임 ①) | `KiDispatchException +0xa2` · `+0x2d4` · `+0x2e5` · `+0x430 ~ +0x452` · `+0x460 ~ +0x49c` · `+0x4ab` · `+0x4b4 ~ +0x4c3` · `+0x4cc` · `+0x52f` · `+0x534 ~ +0x545` · `+0x578` · `+0x583` · `+0x593`, ntdll `KiUserExceptionDispatcher +0x1c ~ +0x26` |
| 3 (끝) · 3-6 스택이 모자랄 때 | 유저 스택 쓰기는 모두 `__try` 안. 새 예외가 **`0xC00000FD` (스택 넘침)** 면 레코드를 그 코드로 바꿔 `+0x419` 부터 **한 번 더**, **그 밖 (AV·정렬·가드 페이지)** 이면 유저 처리기 없이 **둘째 기회 (`+0x7a5`) → 종료**. 상태 코드를 돌려주는 길은 없음 | 지금 "멈춤" → 둘째 기회 (종료) 로 바꾸면 진짜와 같음 | 처리기 `+0x652 ~ +0x6cf` (`.asm` 틈 — 결과서 `.pdata` scope table + objdump), `ProbeForWrite +0x61` · `+0x67` |
| 5 (커널 몫) · 4 두 번째 기회 | `NtRaiseException(…, FirstChance = 0)` → `KiRaiseException` (`NumberParameters > 15` 면 `0xC000000D`, 코드 비트 28 지움) → `KiDispatchException` 둘째 기회: `DbgkForwardException` 두 번 (디버그 포트 · 예외 포트) → 둘 다 거짓이면 **`ZwTerminateProcess(-1, ExceptionCode)`** — 끝 코드 = 예외 코드 그대로 | 디버거·WER 없음이면 `ZwTerminateProcess(-1, 코드)` 로 가면 됨 (run79 끝내기 길) | `NtRaiseException +0x85` · `+0x91`, `KiRaiseException +0x79` · `+0x9c` · `+0xd8` · `+0xe6` · `+0xfe`, `KiDispatchException +0x250` · **`+0x7ae`** · **`+0x7c3`** · `+0x7d0` · `+0x7d3` · **`+0x7d7`** |
| 6 · 5 `__fastfail` (`int 0x29`) | `KiRaiseSecurityCheckFailure` → `KiFastFailDispatch`: 코드 **`0xC0000409`**, 플래그 **1**, 주소 = `int 0x29` 자기 자리, `[0]` = `RCX` (실패 코드), 개수 1. 유저면 **첫 기회 0** → 유저 처리기 한 번도 안 거치고 둘째 기회 → 종료. 커널이면 버그체크 `0x139` | 인터럽트 훅으로 받아 곧장 종료 (`0xC0000409`) 하면 진짜와 같음 | `KiRaiseSecurityCheckFailure +0x0` · `+0x34e` · `+0x352` · `+0x363`, `KiFastFailDispatch +0x6b` · `+0x89 ~ +0x98` · `+0xad` · **`+0xb2`** · **`+0xc1`** |
| "지금 우리" 둘째 줄 · 6 메모리 syscall · `Process.cpp:1953` | syscall 안 포인터 실패는 **커널 모드 예외** → `KiDispatchException` 커널 갈래 → `RtlDispatchException` 이 **커널 프레임의 SEH** 를 찾음 (없으면 버그체크 `0x1e`). 유저 처리기는 안 거침. `Process.cpp:1953` 주석의 `+0x2ab`·`+0x37b`·`+0x3c3` 은 `PspInitializeThunkContext` 의 줄이고, 그 길은 **첫 기회 0** → 종료 | 상태 코드 돌려주기 **맞음** (처리기가 상태를 고르는 줄은 안 읽음 `[추측]`). 주석은 "첫 기회 0 인 유저 예외 = 종료" 로 고치면 정확 | `KiDispatchException +0x16e` · `+0x367 ~ +0x372` · `+0x3b1` · `+0x3b6`, `NtAllocateVirtualMemory` `$filt$0` (결과서 `.pdata`), `PspInitializeThunkContext +0x2ab` (틈) · `+0x2fa` · `+0x2fc` · `+0x37b ~ +0x391` · **`+0x3b4`** · **`+0x3c3`** |

### 유저 스택에 쌓는 모양 — 원문 3 번 "표 하나" (결과서 3-2 · 3-3 그대로, flow-writer 가 `.asm` 으로 다시 봄)

`S0` = 예외 순간의 유저 `RSP` (= `CONTEXT.Rsp`, `KiDispatchException +0x22e MOV R15,[RBX+0x98]`). 모두 `[정적]`.

**(가) 배치 계산** — 줄 `+0x430 ~ +0x452` (공통), XSTATE 일 때만 앞에 `+0x62a ~ +0x640`

| 이름 | 계산 | 줄 | 크기·정렬 |
|---|---|---|---|
| `A` (XSAVE 꼬리 시작) | XSTATE 없음: **`A = S0`**. 있음: **`A = (S0 − XState.Length) & ~0x3F`** (`XState.Length` = 커널 버퍼 확장 문맥 `[+0x14]`) | `+0x62e` · `+0x635` · `+0x63c` | 64바이트 정렬 |
| `M` (머신 프레임) | **`M = (A − 0x28) & ~0xF`** | `+0x430 LEA RDI,[R15-0x28]` · `+0x434 AND RDI,-0x10` | 0x28, 16 정렬 |
| `E` (예외 레코드) | `E = M − 0xA0` | `+0x43c LEA RBX,[RDI-0xa0]` | 0x98 (칸 0xA0) |
| `X` (`CONTEXT_EX`) | `X = E − 0x20` | `+0x447 LEA R8,[RBX-0x20]` | 0x20 |
| `C` (`CONTEXT` = **새 `RSP`**) | **`C = X − 0x4D0`** | `+0x452 LEA R9,[R8-0x4d0]` | 0x4D0, 16 정렬 |

- **새 `RSP` = `C` = `((A − 0x28) & ~0xF) − 0x590`.** XSTATE 없음이고 `S0` 가 16의 배수 + 8 (함수 안에서 보통) 이면 `C = S0 − 0x5B8`, 16의 배수면 `S0 − 0x5C0`. 예 (꾸민 숫자, 결과서): `S0 = …F6E8` → `M = …F6C0`, `E = …F620`, `X = …F600`, `C = …F130`. 레드 존 없음 (`S0` 바로 아래부터 씀).
- 교차 확인 (결과서가 `.pdata` 로 읽음): ntdll `KiUserExceptionDispatcher` 의 `UNWIND_INFO` = `UWOP_ALLOC_LARGE 0x590` + `UWOP_PUSH_MACHFRAME` (오류 코드 없음) — 시작 `RSP` 에서 `0x590` 위에 머신 프레임. 위 `M − C = 0x590` 과 같다.

**(나) 시작 `RSP` (= `C`) 기준 오프셋 표**

| `RSP` 기준 | 크기 | 무엇 | 값 / 누가 쓰나 | 줄 |
|---|---|---|---|---|
| `+0x000` | 0x30 | `CONTEXT.P1Home ~ P6Home` | **안 씀** (유저 스택에 원래 있던 값) | `RtlpCopyExtendedContext` 복사 목록 밖 |
| `+0x030` | 4 | `ContextFlags` | 커널 이름표 `& 0x67ffffff` = **`0x10001f`** (XSTATE 켜짐이면 **`0x10005f`**) | `RtlpCopyExtendedContext +0x11f ~ +0x127` |
| `+0x034` | 4 | `MxCsr` | 트랩 프레임 `+0x2c` | `KeContextFromKframes +0x34b` · `+0x34e` (결과서) |
| `+0x038 ~ +0x047` | 0x10 | `SegCs`·`SegDs`·`SegEs`·`SegFs`·`SegGs`·`SegSs`·`EFlags` | `SegCs 0x33`·`SegSs 0x2b`·`EFlags` = 트랩 프레임 값 (**TF 지우기 전 사진**), `SegDs`·`SegEs`·`SegGs` = `0x2b`, `SegFs` = `0x53` 고정 | `KeContextFromKframes +0x6d ~ +0x94` · `+0xa5` · `+0xac` |
| `+0x048 ~ +0x077` | 0x30 | `Dr0 ~ Dr7` | 트랩 프레임 디버그 레지스터 (`Dr7` 켜진 비트 없으면 0) | `KeContextFromKframes +0x35a ~ +0x3bd` |
| `+0x078 ~ +0x0f7` | 0x80 | `Rax ~ R15` | `Rax ~ R11`·`Rbp` = 트랩 프레임, `Rbx`·`Rdi`·`Rsi`·`R12 ~ R15` = **예외 프레임** (`KiExceptionDispatch` 가 따로 저장), `Rsp` (`+0x98`) = `S0` | `KeContextFromKframes +0xc5 ~ +0x17f` |
| `+0x0f8` | 8 | `Rip` | 못 한 명령 자리 (`#BP` 면 `int3` 자리 = 트랩 `Rip − 1`) | `KeContextFromKframes +0x5f`, `KiDispatchException +0x707` |
| `+0x100 ~ +0x2ff` | 0x200 | `FltSave` (x87·XMM) | `FXSAVE` 결과 + `Xmm0 ~ 5` = 트랩 프레임, `Xmm6 ~ 15` = 예외 프레임 | `KeContextFromKframes +0x26f` · `+0x27d ~ +0x33d` |
| `+0x300 ~ +0x4af` | 0x1b0 | `VectorRegister`·`VectorControl`·`DebugControl` | **안 씀** (스택 쓰레기) | 복사 범위 밖 |
| `+0x4b0 ~ +0x4cf` | 0x20 | `LastBranch*`·`LastException*` | 디버그 묶음에 같이 복사 | `RtlpCopyExtendedContext +0x2f8 ~ +0x30f` |
| **`+0x4d0`** | **0x20** | `CONTEXT_EX` (8바이트 `{Offset, Length}` 4개 — 이름은 `[추측: 이름]`, 아래 덧붙임 ②) | **XSTATE 없어도 늘 씀**: `+0x4d0 {−0x4D0 (0xfffffb30), S0 − C}` · `+0x4d8 {−0x4D0, 0x4D0}` · `+0x4e0 {A − X, S0 − A}` · `+0x4e8 {0, 0}` | 만들기 `+0x60 ~ +0x6a` (0) · `+0x460 ~ +0x49c`, 쓰기 `+0x534 ~ +0x545` (16바이트 두 번) |
| **`+0x4f0`** | 0x98 (칸 0xA0) | `EXCEPTION_RECORD` | 0x98 을 0 으로 지운 뒤 앞 0x20 + `NumberParameters × 8` 만 복사 | `+0x4cc KeCopyExceptionRecord` (그 안 `+0x9` · `+0x5c`) |
| **`+0x590`** | 8 | 머신 프레임 `Rip` | `CONTEXT.Rip` 과 같은 값 | `+0x4b8 ~ +0x4c3` |
| `+0x598` · `+0x5a0` | 8 · 8 | 머신 프레임 `SegCs` · `EFlags` | **안 씀** | — |
| **`+0x5a8`** | 8 | 머신 프레임 `Rsp` | `S0` | `+0x4b0` · `+0x4b4` |
| `+0x5b0` | 8 | 머신 프레임 `SegSs` | **안 씀** | — |
| `+0x5b8 ~ A − C` | 0 ~ 0xF | 정렬 빈틈 | 안 씀 | — |
| `A − C ~ S0 − C` | `S0 − A` | XSAVE 꼬리 (XSTATE 일 때만) | 앞 0x40 (XSAVE 머리) 는 `+0x4da ~ +0x502` 가 직접, 나머지는 `RtlpCopyExtendedContext +0x330 RtlpCopyXStateChunk` | `+0x4d1 CMP [RBP+8],0x100040` |
| `S0 − C` | — | 예외 순간의 원래 `RSP` | 커널은 안 건드림 | — |

- **쓰는 차례** (모두 `__try` 안): ① `+0x4ab ProbeForWrite(C, S0 − C, 16)` → ② 머신 프레임 `Rsp`·`Rip` (`+0x4b4 ~ +0x4c3`) → ③ 레코드 (`+0x4cc`) → ④ (XSTATE) XSAVE 머리 (`+0x4da ~ +0x502`) → ⑤ `+0x52f RtlpCopyExtendedContext(1, X, 설명 [RBP+0x140], 이름표, 커널 확장 문맥)` 가 `CONTEXT` 와 XSAVE 꼬리 → ⑥ `CONTEXT_EX` 0x20 (`+0x534 ~ +0x545`).
- `ProbeForWrite(주소, 길이, 정렬)`: 길이 0 이면 끝 · 정렬 어긋남 → `+0x67 ExRaiseDatatypeMisalignment` (`0x80000002`) · 끝 주소가 감기거나 `≥ 0x7fffffff0000` → `+0x61 ExRaiseAccessViolation` (`0xC0000005`) · 아니면 쪽마다 한 바이트 읽고 다시 씀 (`+0x40 ~ +0x56` — 아직 안 올라온 스택 쪽은 여기서 `MmAccessFault` 가 올려 줌 `[추측]`). `C` 는 계산상 늘 16 정렬이다 (`0x590` 이 16의 배수).
- `KiCopy*` 는 안 쓴다. 쓰는 함수는 `ProbeForWrite` · `KeCopyExceptionRecord` (안쪽 메모리 복사 — 같은 주소 이름 4개, 후보 `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) · `RtlpCopyExtendedContext`, 그리고 `KiDispatchException` 이 직접 `MOV`/`MOVUPS`.

### 트랩 프레임에서 바뀌는 칸 = `KiUserExceptionDispatcher` 가 시작할 때 레지스터 (첫 기회 · 디버거 없음 · 64비트 유저)

| 트랩 프레임 칸 | 새 값 | 줄 | 원래 값 |
|---|---|---|---|
| `EFlags` (`+0x178`) | **TF (비트 8) 지움** | `+0x3e3 CLI` · `+0x3e4 BTR [R12+0x178],8` | 나머지 비트 그대로. 유저 `CONTEXT.EFlags` (`C+0x44`) 는 지우기 **전** 사진 |
| **`Rsp` (`+0x180`)** | **`C`** (= `X − 0x4D0`) | `+0x571 LEA R10,[RDI-0x4d0]` · `+0x578` | `S0` |
| **`SegCs` (`+0x170`)** | **`0x33`** | `+0x580 LEA EAX,[RBX+0x32]` (`RBX = 1`) · `+0x583` | 이미 `0x33` |
| **`Rip` (`+0x168`)** | **`PsNtdllExports+0x20`** = `KiUserExceptionDispatcher` (ntdll rva `0x164e10`, run59) | `+0x58c MOV RAX,[PsNtdllExports+0x20]` · `+0x593` | 못 한 명령 자리 (문맥·머신 프레임에 이미 복사됨) |
| `SegSs` · `Rax ~ R11` · `Rbp` · `Xmm0 ~ 5` · 나머지 | **그대로** | — | 입구가 만든 값 |

- 그다음 (역시 `__try` 안) `+0x56c KePopulateContinuationContext(원래 Rip)` — CET 스레드 (`KTHREAD +0x74` 비트 20) 만 그림자 스택에 한 칸, 비 CET 는 `+0x15 RET`. `+0x59e KiSetupForInstrumentationReturn` — 프로세스 계측 콜백 (`[프로세스+0x168]`) 이 있을 때만 `Rip` 를 콜백으로, 원래 `Rip` 는 `R10`. test.exe 는 둘 다 안 탄다고 봄 (계측 콜백은 `[동적]`).
- `+0x649 [RBP+0xc] = 0` · `JMP` — **`KeContextToKframes` 를 건너뛴다** ("처리됨" 길 `+0x1b8` 에서만). 바뀐 트랩 프레임 그대로 `KiExceptionDispatch +0x145` 로 돌아가 예외 프레임 복원 → `+0x1b4` 유저 → `+0x1e9 KiInitiateUserApc` 루프 → `+0x22a KiRestoreSetContextState` (헤더 비트 27, run80) → `+0x262 KiRestoreDebugRegisterState` → 조각 `FUN_1406c2b18` (= `+0x418`): `+0x49a JMP KiKernelExit` (KVA 그림자) 또는 **`+0x4b1 SWAPGS` · `+0x4b4 IRETQ`**. **`SYSRET` 이 아니라 `IRETQ`.**
- **`KiUserExceptionDispatcher` 가 받는 것** (ntdll `.asm` `+0x0 ~ +0x2e`): 입력은 **`RSP` 하나** (= `C` = `CONTEXT`). `+0x1c MOV RCX,RSP` · `+0x1f ADD RCX,0x4f0` (레코드) · `+0x26 MOV RDX,RSP` (문맥) — `RCX`·`RDX` 는 입력이 아니라 바로 덮는다. `RIP` = 진입점, `CS` = `0x33`, 그 밖 레지스터는 예외 순간 값 그대로 (그 뒤 안 읽음). 첫 줄 `CLD`, `Wow64PrepareForException` 이 0 이 아니면 부름 (`+0x1 ~ +0x1a`). 나머지는 Q148b.

### 예외 코드 표 (유저가 보는 값 = `KiPreprocessFault` 가 표시 코드를 푼 뒤)

| 예외 | 입구 | `ExceptionCode` | `ExceptionAddress` | 개수 | `ExceptionInformation` | 근거 줄 |
|---|---|---|---|---|---|---|
| 접근 위반 (`#PF`) | `KiPageFault` | `0xC0000005` (입구 `0x10000004` → `KiPreprocessFault +0x9b`) | `Rip` (`+0x436`) | 2 | `[0]` 읽기 **0** / 쓰기 **1** / 실행 **8**, `[1]` = `CR2` | `KiPageFault +0x37e ~ +0x383` · `+0x428 ~ +0x43d` |
| 가드 페이지 | `KiPageFault` | `0x80000001` | `Rip` | 2 | 〃 | `+0x409` |
| 스택 넘침 | `KiPageFault` | `0xC00000FD` | `Rip` | 2 | 〃 | `+0x411` |
| 페이지 읽기 실패 | `KiPageFault` | `0xC0000006` | `Rip` | **3** | 〃 + `[2]` = `MmAccessFault` 의 NTSTATUS | `+0x419 ~ +0x423` |
| 특권 명령 (`#GP` — `hlt` `cli` `in` `out` `mov cr` 등) | `KiGeneralProtectionFault` | `0xC0000096` | `Rip` | **0** | 없음 | `KiPreprocessFault +0x1d8` → `KiOp_Priv +0x4` · `+0x8` |
| 그 밖의 `#GP` (정규화 안 된 주소 등) | 〃 | `0xC0000005` | `Rip` | 2 | `[0]` = **0**, `[1]` = **`0xFFFFFFFFFFFFFFFF`** | `KiPreprocessFault +0x1f8 · +0x1ff · +0x203` |
| `ud2`·잘못된 명령 (`#UD`) | `KiInvalidOpcodeFault` | `0xC000001D` (입구 `0x10000002`) | `Rip` | 0 | 없음 | `KiPreprocessFault +0x1b5` |
| 0 나누기 (`#DE`) | `KiDivideErrorFault` | **`0xC0000094`** (입구 `0x10000003`) — 나누는 값이 0 이 아니면 (`INT_MIN / −1` 등) **`0xC0000095`** | `Rip` | 0 | 없음 | `KiPreprocessFault +0x231`, `KiOp_Div +0x21` · `+0xdd` · `+0x104` |
| `int3` (`#BP`) | `KiBreakpointTrap` | `0x80000003` (입구가 진짜 코드 그대로) | **`int3` 자기 자리** (`+0x34d DEC R8`) | **1** | `[0]` = 0 (`+0x350`) | `KiBreakpointTrap +0x33c ~ +0x356`, 문맥 `Rip` −1 `KiDispatchException +0x707` |
| 한 걸음 (`#DB`) | `KiDebugTrapOrFault` | `0x80000004` | `Rip` | 0 (분기 추적이면 2) | 없음 | `+0x5ea` (결과서) |
| 정수 오버플로 (`into`) | `KiOverflowTrap` | `0xC0000095` | `Rip − 1` | 0 | 없음 | 이름·코드만 |
| 배열 범위 (`bound`) | `KiBoundFault` | `0xC000008C` | `Rip` | 0 | 없음 | 〃 |
| 정렬 (`#AC`) | `KiAlignmentFault` | `0x80000002` | `Rip` | 0 | 없음 | 〃 |
| x87 (`#MF`) | `KiFloatingErrorFault` | `0xC000008E` (÷0) · `0xC0000090` · `0xC0000091` · `0xC0000092` · `0xC0000093` · `0xC000008F` | `Rip` | 1 | `[0]` = 0 | 〃 |
| SIMD (`#XM`) | `KiXmmException` | 위 x87 코드 + `0xC00002B4` | `Rip` | 2 | `[1]` = 상태 레지스터 값 | 〃 |
| `__fastfail` (`int 0x29`) | `KiRaiseSecurityCheckFailure` | `0xC0000409`, 플래그 1 | `int 0x29` 자기 자리 (`+0x0 SUB [RSP],2`) | 1 | `[0]` = `RCX` | 질문 6 |
| `int 0x2c` | `KiRaiseAssertion` | `0xC0000420` | `int 0x2c` 자기 자리 | 0 | 없음 | 이름·코드만 |
| `int 0x2d` | `KiDebugServiceTrap` | `0x80000003` | 저장 `Rip` + 1 (`+0x0 INC [RSP]`), `DEC` 없음 | 1 | `[0]` = `RAX` | 이름·코드만 |
| `#NM` (XSTATE 첫 사용) | `KiNpxNotAvailableFault` | 입구 `0x10000010` → `KiPreprocessFault +0x256 ~ +0x268` 이 `KiEnableOptionalXStateFeatures` 로 켜면 "처리됨" (예외 안 감). 실패면 `0xC000001D` | `Rip` | 1 | `[0]` = `RAX` | 이름만 |

- 입구 ↔ CPU 벡터 번호 (`#PF` = 14 등) 는 **이름으로 짝지었다** `[추측: 이름]` — IDT 표 바이트는 안 봤다 (아래 "아직 확인 필요" 정적 ④).

### nt 엔진이 흉내 낼 최소 규칙 (결과서 sast/78 끝을 그대로 옮김)

CPU 예외가 나서 은퇴 뒤 Unicorn 에서 **유저 모드** 코드가 멈췄을 때 — 디버거·WER 없음, 64비트 프로세스, 비 CET, 계측 콜백 없음:

1. **코드·주소·인자 정하기** (예외 코드 표): `#PF` = `0xC0000005`, 개수 2, `[0]` = 읽기 0 / 쓰기 1 / 실행 8, `[1]` = 주소; 특권 명령 `0xC0000096` (개수 0); `#UD` `0xC000001D`; `#DE` `0xC0000094` / `0xC0000095`; `int3` `0x80000003`, 주소 = `int3` 자리, 개수 1, `[0] = 0`, **문맥 `Rip` 도 `int3` 자리**; `ExceptionFlags = 0`.
2. **문맥 만들기:** 순간의 레지스터로 `CONTEXT` (0x4d0): `ContextFlags = 0x10001f`, `SegCs = 0x33`, `SegSs = 0x2b`, `SegDs = SegEs = SegGs = 0x2b`, `SegFs = 0x53`, `EFlags`/`Rsp`/`Rip` = 순간 값 (`int3` 만 `Rip − 1`), `MxCsr`·`XMM`·`FltSave` 채움. `P1Home ~ P6Home` 는 안 씀.
3. **배치 계산:** `S0 = RSP`. `M = (S0 − 0x28) & ~0xF`, `E = M − 0xA0`, `X = E − 0x20`, `C = X − 0x4D0`.
4. **쓸 수 있나 확인:** `C` 가 16 정렬이고 `[C, S0)` 의 끝 주소 `< 0x7fffffff0000` 이고 쓰기 가능 (아직 안 올라온 스택 쪽은 올려 줌). 스택 넘침 `0xC00000FD` 로 실패하면 레코드를 그 코드로 바꿔 3 단계부터 한 번 더, 그 밖의 실패는 **둘째 기회로 곧장** (7 단계).
5. **유저 스택에 쓰기** (차례 상관없음, 모두 `C` 기준): `C` (0x4d0 `CONTEXT`) · `C+0x4d0` (0x20 `CONTEXT_EX`: `{−0x4D0, S0−C}, {−0x4D0, 0x4D0}, {S0−X, 0}, {0, 0}`) · `C+0x4f0` (레코드: 0x98 을 0 으로 채우고 앞 0x20 + `개수 × 8`) · `C+0x590` 에 `Rip` (= 문맥 `Rip`), `C+0x5a8` 에 `Rsp` (= `S0`).
6. **레지스터 바꾸기:** `RSP = C`, `RIP = ntdll!KiUserExceptionDispatcher` (`PsNtdllExports+0x20`, ntdll rva `0x164e10`), `CS = 0x33`, `EFLAGS = 순간 값 & ~0x100` (TF 지움). **다른 레지스터는 그대로.** (`RCX`/`RDX` 는 필요 없음.) 그 뒤 Unicorn 을 이어 돌린다.
7. **둘째 기회 (`NtRaiseException(…, FALSE)` · `__fastfail` · 쓰기 실패):** 유저 쪽으로 안 보내고 프로세스 종료 `ZwTerminateProcess(-1, ExceptionCode)` (run79).
8. **`NtRaiseException(레코드, 문맥, TRUE)`:** `NumberParameters ≤ 15` 아니면 `0xC000000D`, 코드 비트 28 지우고, 유저 문맥을 레지스터 (트랩 프레임) 로 읽어 들인 뒤 1 ~ 6 단계를 새로 한다.
9. 안 흉내 내도 되는 것: 디버거 갈래, `KdTrap`, WoW64, Pico, CET 그림자 스택, 계측 콜백, `KiPreprocessFault` 의 ATL thunk·XSTATE 켜기·보안 PCI.

- flow-writer 덧붙임 (규칙에 넣을지는 PE_emulator 쪽 판단):
  - ① **이 VM 은 XSTATE 갈래 (`0x10005f`) 로 보인다** `[추측: 사진 간접]`. `KiDispatchException +0x2d4 TEST [KeFeatureBits],0x800000` 은 비트 23 이다. 이 비트는 부팅 때 `KiInitializeKernel +0x2e5 BTS RBX,0x17` 이 KUSD `+0x3d8` (`XState.EnabledFeatures`) ≠ 0 일 때 켜고, 같은 비트가 KUSD `ProcessorFeatures[17]` (XSAVE_ENABLED) 가 된다 `[정적]` (run70, 짐 지도 7.6.10). run44 사진: KUSD `+0x285` (= `ProcessorFeatures[17]`) **1**, `+0x3d8` `EnabledFeatures` **`0xe7`**, `+0x3e0` `EnabledVolatileFeatures` `0xe7`, `+0x3e8` `Size` **`0x980`**, `+0x3ec` `ControlFlags` **3** (`OptimizedSave`·`CompactionEnabled`) `[관찰]` (`ms/logs/04_baggage-read_supp_K1.txt:327 · 348 · 349`, `_P2.txt:4165 · 4186 · 4187`, 칸 이름은 `pdb_struct.py` `_XSTATE_CONFIGURATION`). `KeFeatureBits` 값 자체는 사진에 없다 — 동적 1. 그러니 규칙 2 의 `0x10001f` 는 "XSAVE 를 흉내 내지 않는 쉬운 길" 이고, 진짜 VM 과 같게 하려면 `0x10005f` + XSAVE 꼬리 (규칙 3 에 `A = (S0 − 길이) & ~0x3F`) 다. 꼬리 길이는 `RtlGetExtendedContextLength3` (`+0xe3`) 가 정하는 런타임 값이라 이번엔 숫자로 못 정함. KUSD 를 진짜 값 (`0xe7`) 으로 두면서 `0x10001f` 로 쌓아도 ntdll 쪽이 괜찮은지는 Q148b.
  - ② `_CONTEXT_EX` 는 **공개 PDB 에 없다** (`pdb_struct.py ntoskrnl.exe _CONTEXT_EX` → 없음). 그래서 `All`·`Legacy`·`XState`·`KernelCet` 이름은 `[추측: 이름]` 이고, 값 (4칸 × `{Offset 4B, Length 4B}`) 은 줄 (`+0x460 ~ +0x49c`) 에서 읽은 것이다. 넷째 칸 `{0, 0}` 은 함수 앞머리 `+0x60 XORPS` · `+0x63` · `+0x6a` 가 버퍼 0x20 바이트를 0 으로 지운 것 (flow-writer `.asm`).
  - ③ test.exe 도 초기화 중 **소프트웨어 예외 하나** 는 낸다: `KERNELBASE!OutputDebugStringA +0x77 RaiseException(0x40010006)` (run71 `[정적]` — TLS 콜백·`DllMain`·`main` 의 `Log`). 이것이 커널 (`NtRaiseException` → `KiDispatchException` 첫 기회) 을 거치는지는 `RtlRaiseException` 몫이라 **Q148b**. 거친다면 아래 동적 1·2·5 는 새 샘플 없이 test.exe 로 걸린다 `[추측]`.
  - ④ 원문 5 의 "`NtRaiseException(…, FirstChance 거짓)`" 을 부르는 유저 줄은 ntdll `KiUserExceptionDispatcher +0x4b XOR R8B,R8B` · `+0x4e` (ret `+0x53`) `NtRaiseException` 이다 (`.asm`, `RtlDispatchException` `+0x29` 가 거짓일 때) — 풀이는 run82.

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서, 기준과 같음). 질문 3 의 시작 레지스터만 보려고 ntdll `KiUserExceptionDispatcher` 도 한 함수 (ntdll pdb 는 기준 `C3093720177DA6851519DCFFE8DF53FE1`, 다시 analyze 안 함)
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치. 요청서가 준 rva·크기 (`KiDispatchException` `0x3eadc0` 2044B, `KiExceptionDispatch` `0x6c2700` 844B, `KiPreprocessFault` `0x3eb5e0` 878B, 입구 여섯) 도 맞음
- **오프셋 읽는 법**: 입구 여섯은 손으로 쓴 asm 이라 Ghidra 가 조각 (`FUN_…`) 으로 쪼갰다. 이 문서의 입구 오프셋은 `.asm` 줄 끝 `(= KiPageFault +0x…)` 값 (함수 시작 기준) 이다. 조각 기준 값과 rva 는 결과서 질문 1 표에 같이 있다. WinDbg 가 조각 주소를 `nt!KiPageFault+0x…` 로 보여 줄지는 `[추측]`.
- 칸 이름:
  - `_KTRAP_FRAME` (`pdb_struct.py`, 결과서): `FaultIndicator +0x2a` · `ExceptionActive +0x2b` · `MxCsr +0x2c` · `FaultAddress +0xd0` · `Rip +0x168` · `SegCs +0x170` · `EFlags +0x178` · `Rsp +0x180` · `SegSs +0x188`. 입구는 `RBP = 트랩 + 0x80` 이라 `[RBP+x]` = 트랩 `+x+0x80` (`[RBP+0x50]` = `FaultAddress`, `[RBP-0x56]` = `FaultIndicator`, `[RBP+0xe8]` = `Rip`, `[RBP+0xf0]` = `SegCs`)
  - `EXCEPTION_RECORD` (0x98): `ExceptionCode +0x0` · `ExceptionFlags +0x4` · `ExceptionRecord +0x8` · `ExceptionAddress +0x10` · `NumberParameters +0x18` · `ExceptionInformation[15] +0x20` (`KiExceptionDispatch +0x67 ~ +0x81` 줄과 맞음)
  - `_CONTEXT` (0x4d0): `ContextFlags +0x30` · `EFlags +0x44` · `Rsp +0x98` · `Rip +0xf8` (run16)
  - `_XSTATE_CONFIGURATION` (`pdb_struct.py`, flow-writer): `EnabledFeatures +0x0` · `EnabledVolatileFeatures +0x8` · `Size +0x10` · `ControlFlags +0x14` (`OptimizedSave` 비트 0 · `CompactionEnabled` 비트 1 · `ExtendedFeatureDisable` 비트 2). KUSD `+0x3d8` 에 있음
  - `ContextFlags` 숫자 (run80 과 같음): `0x100001` CONTROL · `0x100002` INTEGER · `0x100004` SEGMENTS · `0x100008` FLOATING_POINT · `0x100010` DEBUG_REGISTERS · `0x100040` XSTATE. `0x10001f` = 앞 다섯, `0x10005f` = + XSTATE

## 목적

- PE_emulator 요청 26 의 커널 쪽 (Q148a, 원문 급함 가장 높음): CPU 예외가 유저 모드에서 났을 때 커널이 하는 일 — 입구 → 디스패치 사슬, 예외마다 `EXCEPTION_RECORD`, **유저 스택에 쌓는 모양 (오프셋 · 크기 · 무엇 · 값)**, `KiUserExceptionDispatcher` 로 갈 때의 레지스터, 스택 모자람, 두 번째 기회 (커널 쪽), `__fastfail`, 메모리 syscall 의 포인터 실패.
- 유저 APC 가 유저 스택에 문맥을 쌓는 꼴 (run42 `KiInitializeUserApc`), 맡긴 문맥 길이 (run55), syscall 출구 트랩 `Rax` (run77), 헤더 비트 27 (run80), 끝내기 길 (run79) 은 가리키기만 한다.

## 방법

- 정적: `_workspace/sast/78_exception-kernel.md` — ntoskrnl (+ ntdll 한 함수)
  - 지도: `run_ghidra.sh map ntoskrnl.exe KiDispatchException` · `KiExceptionDispatch` (`ms/disassemble/ntoskrnl/_map/`). `KiDispatchException` 깊이 1 = 33줄 (callee 25개). 부르는 쪽 (`_calls.tsv`) = `KiExceptionDispatch +0x140` · `KiFastFailDispatch +0xc1` · `KiRaiseException +0xfe` · `PspInitializeThunkContext +0x3c3` 넷.
  - 뽑음 (모두 depth 0 — 요청서는 depth 1 이었으나 지도로 필요한 callee 만 골라 뽑음, 결과서): 대상 15개 (`KiPageFault`·`KiGeneralProtectionFault`·`KiBreakpointTrap`·`KiDivideErrorFault`·`KiInvalidOpcodeFault`·`KiDebugTrapOrFault`·`KiExceptionDispatch`·`KiDispatchException`·`KiPreprocessFault`·`KeCopyExceptionRecord`·`NtRaiseException`·`KiRaiseException`·`KiRaiseSecurityCheckFailure`·`KiFastFailDispatch`·`KeContextFromKframes`) + 넓힌 것 — 질문 3: `RtlpCopyExtendedContext`·`ProbeForWrite`·`KePopulateContinuationContext`·`KiSetupForInstrumentationReturn`·`RtlInitializeExtendedContext2`·`RtlGetExtendedContextLength3`; 질문 2: `KiOpDecode`·`KiOp_Priv`·`KiOp_Illegal`·`KiOp_Div`·`KiOp_INT`·`KiOp_Mov` + 입구 11개 (이름과 코드만); 질문 4: `DbgkForwardException` (첫 줄들)·`KyRaiseException`·`KiExceptionDispatchOnExceptionStack` (안 읽음); 질문 6: `KiSystemServiceUser`; ntdll `KiUserExceptionDispatcher` (92B).
  - 읽기만: 있던 `PspInitializeThunkContext.asm`.
  - `.asm` 에 없는 곳 — **`__except` 처리기 본문** (`KiDispatchException +0x652 ~ +0x6cf` 126바이트, `PspInitializeThunkContext +0x2ab ~ +0x2b9` 15바이트) 은 `.asm` 의 "읽지 않은 틈" 이다. 결과서가 `.pdata` → `UNWIND_INFO` → `C_SCOPE_TABLE` 을 직접 읽어 처리기 자리를 찾고 `KiDispatchException` 쪽만 objdump 로 풀었다. `KiUserExceptionDispatcher` 의 `UNWIND_INFO` · `NtAllocateVirtualMemory` 의 scope table 도 결과서가 직접 읽음.
- 결과서: `_workspace/sast/78_exception-kernel_result.md` (**분석성공**. 여섯 질문 모두 줄로 답함, 스크립트 문제 없음 — 참고 2건)
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `KiPageFault` 조각 `FUN_1406bd7ae` `+0x37e` · `+0x380` · `+0x383` · `+0x386 MmAccessFault` · `+0x38d JGE` · `+0x43d KiExceptionDispatch`, `KiBreakpointTrap +0x34d DEC R8`, `KiDispatchException +0x13d` · `+0x15e` · `+0x4ab` · `+0x4cc` · `+0x52f` · `+0x707` · `+0x7ae` · `+0x7c3` · `+0x7d7`, `KiFastFailDispatch +0xb2` · `+0xc1`
- flow-writer 도 `.asm` 으로 다시 봤다: `KiDispatchException +0x20 ~ +0x9c` (앞머리, `+0x60 ~ +0x6a` 버퍼 지우기) · `+0xa2` · `+0xa8` · `+0xae` · `+0xb2` · `+0xe3` · `+0x103` · `+0x108` · `+0x12b` · `+0x142` · `+0x147` · `+0x14e` · `+0x163 ~ +0x16e` · `+0x22e ~ +0x26c` · `+0x2be` · `+0x2d4 ~ +0x2e5` · `+0x3bc ~ +0x3e4` · `+0x3ee ~ +0x5af` (전부) · `+0x62a ~ +0x64d` · `+0x707 ~ +0x74a` · `+0x79c ~ +0x7dc` (전부), `KiExceptionDispatch +0x67 ~ +0x9f` · `+0x139` · `+0x140` · `+0x1b4` · `+0x1e9` · `+0x22a` · `+0x262` · 조각 `+0x418` · `+0x49a` · `+0x4b1` · `+0x4b4`, `KiPageFault +0x342` · `+0x348` · `+0x358` · `+0x364` · `+0x38b` · `+0x393` · `+0x39c` · `+0x3f2 ~ +0x442` · `+0x46e` · `+0x489`, `KiBreakpointTrap +0x0 ~ +0xc` · `+0x33c ~ +0x35c`, `KiPreprocessFault +0x57` · `+0x9b` · `+0xc3` · `+0x1b5` · `+0x1d8` · `+0x1f8` · `+0x1ff` · `+0x203` · `+0x22b` · `+0x231`, `KiOp_Priv +0x4` · `+0x8`, `KiOp_Div +0x21` · `+0xdd` · `+0x104`, `KiRaiseException +0x4a` · `+0x71` · `+0x79` · `+0x7c` · `+0x9c` · `+0xb3` · `+0xd8` · `+0xe6` · `+0xfe`, `NtRaiseException +0x15` · `+0x85` · `+0x91` · `+0x98`, `KyRaiseException +0x16f` · `+0x18d`, `DbgkForwardException +0x59` · `+0x161` · `+0x169` · `+0x268` · `+0x39c`, `KeContextFromKframes +0x47` · `+0x5f` · `+0x6d` · `+0xa5` · `+0xac`, `RtlpCopyExtendedContext +0x11f` · `+0x127` · `+0x330`, `KeCopyExceptionRecord +0x9` · `+0x5c`, `ProbeForWrite +0x40` · `+0x61` · `+0x67`, `KePopulateContinuationContext +0xc` · `+0x13` · `+0x15`, `KiRaiseSecurityCheckFailure +0x0 ~ +0x12` · `+0x34e ~ +0x368` (조각), `KiFastFailDispatch +0x6b` · `+0x89` · `+0x90` · `+0x98` · `+0xad` · `+0xc6`, `KiSystemServiceUser +0x84 ~ +0xa6`, `PspInitializeThunkContext +0x121` · `+0x26e` · `+0x285` · `+0x2a2 ~ +0x2fc` (전부) · `+0x37b ~ +0x3c8` (전부), ntdll `KiUserExceptionDispatcher +0x0 ~ +0x5b` (전부), `KiInitializeKernel +0x2e5` · `+0x636` (run70 줄 맞춤). `_calls.tsv` 로 호출 줄 모두 맞춤.
- 값 대조 (flow-writer): run44 덤프 읽기 로그의 KUSD 줄 — `ms/logs/04_baggage-read_supp_K1.txt:327` (`+0x280` 줄 — `+0x285` = 1) · `:348` (`+0x3d8` = `0xe7`) · `:349` (`+0x3e0` `0xe7` · `+0x3e8` `0x980` · `+0x3ec` 3), `_supp_P2.txt` 같은 줄, `_P2.txt:4165 · 4186 · 4187` (유저 주소 `0x7ffe…`, 같은 값). 새 동적 실행은 없다.
- 소스: `sample/*.c` 에 `__try`·`RaiseException`·`DebugBreak`·`__debugbreak`·`int3`·0 나누기 글자가 없다 (grep, flow-writer) `[소스]`.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`, 입구 조각은 `.asm` `(= …)` 값)

| caller | call | ret | callee | 조건·뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!KiPageFault` | `+0x386` | `+0x38b` | `nt!MmAccessFault` | `ECX` 오류 코드 · `RDX` `CR2` · `R8B` `SegCs & 1` · `R9` 트랩. `+0x38d JGE` ≥ 0 (해결) 이면 예외 없음 | [정적] |
| `nt!KiPageFault` | **`+0x43d`** | `+0x442` | `nt!KiExceptionDispatch` | `MmAccessFault` 실패 → `+0x3f2 ~ +0x428` 상태 → 코드 | [정적] |
| `nt!KiGeneralProtectionFault` | `+0x360` | `+0x365` | `nt!KiExceptionDispatch` | `ECX = 0x10000001`, `EDX = 2`, `R9` = 선택자 오류 코드, `R10 = 0` | [정적] |
| `nt!KiBreakpointTrap` | `+0x356` | `+0x35b` | `nt!KiExceptionDispatch` | `ECX = 0x80000003`, `EDX = 1`, `R8 = Rip − 1` (`+0x34d`), `R9 = 0` | [정적] |
| `nt!KiDivideErrorFault` | `+0x34a` | `+0x34f` | `nt!KiExceptionDispatch` | `ECX = 0x10000003` | [정적] |
| `nt!KiInvalidOpcodeFault` | `+0x34a` | `+0x34f` | `nt!KiExceptionDispatch` | `ECX = 0x10000002` | [정적] |
| `nt!KiDebugTrapOrFault` | `+0x600` | `+0x605` | `nt!KiExceptionDispatch` | `ECX = 0x80000004` | [정적] |
| `nt!KiExceptionDispatch` | **`+0x140`** | `+0x145` | `nt!KiDispatchException` | `(레코드 RSP+0x138, 예외 프레임 RSP, 트랩 RBP−0x80, 이전 모드, 첫 기회 1 (+0x90))`. 유저면 `+0x9f JNZ` 로 곧장 | [정적] |
| `nt!KiExceptionDispatch` | `+0x139` | `+0x13e` | `nt!KiExceptionDispatchOnExceptionStack` | 커널 모드 갈래 (범위 밖) | [정적] |
| `nt!KiDispatchException` | `+0xe3` | `+0xe8` | `nt!RtlGetExtendedContextLength3` | 문맥 + 꼬리 길이 (`R9` = KUSD `+0x3d8`) | [정적] |
| `nt!KiDispatchException` | `+0x103` | `+0x108` | 스택 늘리기 확인 (같은 주소 이름 2개, 후보 `__chkstk`·`_alloca_probe`) | 뒤 `+0x108 SUB RSP,RAX` — 커널 스택에 문맥 버퍼 | [정적] |
| `nt!KiDispatchException` | `+0x12b` | `+0x130` | `nt!RtlInitializeExtendedContext2` | 이름표 (`0x10001f`/`0x10005f`) 넣음 | [정적] |
| `nt!KiDispatchException` | **`+0x13d`** | `+0x142` | `nt!KeContextFromKframes` | `(트랩, 예외 프레임, 커널 버퍼)` | [정적] |
| `nt!KiDispatchException` | **`+0x15e`** | `+0x163` | `nt!KiPreprocessFault` | 표시 코드 풀기. `AL ≠ 0` 이면 "처리됨" `+0x165 JNZ` | [정적] |
| `nt!KiDispatchException` | `+0x267` | `+0x26c` | `nt!KdIsThisAKdTrap` | 첫 기회 · 유저 | [정적] |
| `nt!KiDispatchException` | `+0x2be` | `+0x2c3` | `nt!KdTrap` | `DebugPort == 0` · `KdIgnoreUmExceptions == 0` 일 때 (커널 디버거) | [정적] |
| `nt!KiDispatchException` | `+0x3d6` | `+0x3db` | `nt!DbgkForwardException` | `(레코드, 1, 0)` — 디버그 포트 없으면 거짓 (`DbgkForwardException +0x161 / +0x169 → +0x268`) | [정적] |
| `nt!KiDispatchException` | **`+0x4ab`** | `+0x4b0` | `nt!ProbeForWrite` | `(C, S0 − C, 16)` — `__try` 안 | [정적] |
| `nt!KiDispatchException` | **`+0x4cc`** | `+0x4d1` | `nt!KeCopyExceptionRecord` | `(E, 레코드)` | [정적] |
| `nt!KiDispatchException` | **`+0x52f`** | `+0x534` | `nt!RtlpCopyExtendedContext` | `(1, X, [RBP+0x140], 이름표, 커널 확장)` | [정적] |
| `nt!KiDispatchException` | `+0x56c` | `+0x571` | `nt!KePopulateContinuationContext` | CET 만 일함 | [정적] |
| `nt!KiDispatchException` | `+0x59e` | `+0x5a3` | `nt!KiSetupForInstrumentationReturn` | 계측 콜백 있을 때만 일함 | [정적] |
| `nt!KiDispatchException` | **`+0x7ae`** | `+0x7b3` | `nt!DbgkForwardException` | 둘째 기회 `(레코드, 1, 1)` | [정적] |
| `nt!KiDispatchException` | **`+0x7c3`** | `+0x7c8` | `nt!DbgkForwardException` | 둘째 기회 `(레코드, 0, 1)` — 예외 포트 0 이면 `+0x39c DbgkpSendErrorMessage` | [정적] |
| `nt!KiDispatchException` | **`+0x7d7`** | `+0x7dc` | `nt!ZwTerminateProcess` | `(-1, [레코드])` (`+0x7d0` · `+0x7d3`) | [정적] |
| `nt!KiPreprocessFault` | `+0xc3` | `+0xc8` | `nt!KiOpDecode` | 명령 읽기 (`#GP`·`#DE`) | [정적] |
| `nt!KiPreprocessFault` | `+0x1d8` | `+0x1dd` | `nt!KiOp_Priv` | 특권 명령 → `0xC0000096` | [정적] |
| `nt!NtRaiseException` | `+0x91` | `+0x96` | `nt!KiRaiseException` | `(레코드, 문맥, 예외 프레임, 트랩, 첫 기회 R8B → +0x85)` | [정적] |
| `nt!KiRaiseException` | `+0xb3` | `+0xb8` | 메모리 복사 (같은 주소 이름 4개, 후보 `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) | 레코드 `0x20 + 8 × 개수` 를 커널로 | [정적] |
| `nt!KiRaiseException` | `+0xd8` | `+0xdd` | `nt!KyRaiseException` | 유저 문맥 읽기·검증 → `+0x16f ExceptionActive = 1` · `+0x18d KeContextToKframes` | [정적] |
| `nt!KiRaiseException` | `+0xfe` | `+0x103` | `nt!KiDispatchException` | `+0xe6 BTR [레코드],0x1c` 뒤 | [정적] |
| `nt!KiRaiseSecurityCheckFailure` | `+0x363` | `+0x368` | `nt!KiFastFailDispatch` | `ECX = 0xC0000409` · `EDX = 1` · `R9 = [RBP−0x48]` (트랩 `Rcx`) | [정적] |
| `nt!KiFastFailDispatch` | `+0xad` | `+0xb2` | `nt!KiBugCheckDispatch` | 커널 모드 (`0x139`) | [정적] |
| `nt!KiFastFailDispatch` | **`+0xc1`** | `+0xc6` | `nt!KiDispatchException` | 유저 — **첫 기회 0** (`+0xb2`) | [정적] |
| `nt!PspInitializeThunkContext` | **`+0x3c3`** | `+0x3c8` | `nt!KiDispatchException` | `EBX < 0` (`+0x2fc JS`) — **첫 기회 0** (`+0x3b4`), 이전 모드 `R14B` (= 1, `+0x121`) | [정적] |
| `nt!KiSystemServiceUser` | `+0xa1` | `+0xa6` | `nt!KiExceptionDispatch` | `ECX = 0xC000001C` — `PsSyscallProviderDispatch` (`+0x84`) 갈래, 포인터 실패와 무관 (이름만) | [정적] |
| `ntdll!KiUserExceptionDispatcher` | `+0x29` | `+0x2e` | `ntdll!RtlDispatchException` | `(RSP+0x4f0, RSP)` — Q148b | [정적] |
| `ntdll!KiUserExceptionDispatcher` | `+0x4e` | `+0x53` | `ntdll!NtRaiseException` | `RtlDispatchException` 거짓이면 `(…, R8B = 0)` — Q148b | [정적] |

### 질문 1 — 입구 → 디스패치

- **입구는 진짜 코드를 바로 안 넘긴다.** `#PF`·`#GP`·`#UD`·`#DE`·`#NM` 은 `ECX` 에 **안쪽 표시 코드** `0x10000004`·`0x10000001`·`0x10000002`·`0x10000003`·`0x10000010` 을 담고, `KiDispatchException +0x15e KiPreprocessFault` 가 진짜 코드로 바꾼다. 유저는 표시 코드를 못 본다. `KiRaiseException +0xe6 BTR [레코드],0x1c` 가 `NtRaiseException` 으로 들어온 코드의 비트 28 (`0x10000000`) 을 지운다 — 유저가 표시 코드를 만들어 보내는 길을 막는 것으로 보임 `[추측: 의도]`.
- 입구 머리 (결과서): 오류 코드가 없는 입구 (`#BP`·`#DE`·`#UD`·`#DB`) 는 `SUB RSP,8` (가짜 칸), 있는 입구 (`#PF`·`#GP`) 는 바로 `PUSH RBP`, 이어서 `SUB RSP,0x158` · `LEA RBP,[RSP+0x80]` (`KiBreakpointTrap +0x0 ~ +0xc`). 공통 줄기 밖: `KiFlushBhbDuringTrapEntryOrExit` · `KiSaveDebugRegisterState` · `KiSynchronizeUserIsolationDomainExit` (이름만).
- **`KiPageFault` 갈림**: `+0x342` 오류 코드 · `+0x348 MOV RCX,CR2` · `+0x358 [RBP+0x50] = CR2` (`FaultAddress`) · `+0x37e SHR EAX,1` · `+0x380 AND EAX,9` · `+0x383 [RBP−0x56] = AL` (`FaultIndicator`) → `+0x386 MmAccessFault` → **`+0x38d JGE`** 이면 예외 없이 나감 (`+0x46e PsWatchEnabled` …). 실패면 `+0x393` 유저 CS? · `+0x39c SegCs == 0x33` (아니면 WoW64) · `+0x3a6 ~ +0x3c8` 유저 GS 베이스가 기대와 다르면 MSR 을 고치고 예외 없이 돌아감 `[추측: 망가진 GS 베이스 구제]` → `+0x3f2 ~ +0x428` 상태 → 코드 (`0xD0000006` 이면 예외 아님 — `KiBugCheckDispatch 0xA` 길). `+0x364 JNC` — 불린 쪽 IF 가 꺼져 있으면 바로 버그체크 (유저 코드는 해당 없음).
- **`KiExceptionDispatch`**: `+0x67 ~ +0x81` 레코드 채움 (코드 · 플래그 0 · 링크 0 · 주소 `R8` · 개수 `EDX` · 정보 `R9`/`R10`/`R11`), `+0x8c AND R9B,1` (이전 모드), `+0x90 [RSP+0x20] = 1` (첫 기회), `+0x9f JNZ` 유저면 곧장 `+0x140`. 커널이면 `+0xa5 ~ +0x12d` 검사 뒤 `+0x139 KiExceptionDispatchOnExceptionStack` (범위 밖).
- **`KiPreprocessFault` 자리**: `KiDispatchException` 안, `+0x13d KeContextFromKframes` **뒤**, 유저·커널 갈림 (`+0x167 ~ +0x16e`) **앞**. "처리됨" (`AL ≠ 0`) 이면 `+0x1b8 KeContextToKframes` 로 문맥을 트랩에 되돌리고 예외 없이 나감 (ATL thunk 흉내·XSTATE 켜기 등). `#BP` 면 먼저 `+0x147 CMP` · `+0x14e JZ +0x707` (`DEC` 문맥 `Rip`) 를 거쳐 `+0x716 JZ` 로 돌아와 **그래도 `KiPreprocessFault` 를 부른다** (`KiDynamicTraceMask` 비트 1 이 켜졌으면 `+0x735 KiTpHandleTrap` 갈래 — 이름만).
- 첫 기회일 때 `+0x9c JNZ` 가 먼저 Pico 프로세스인지 본다 (`[프로세스+0x640]` `PicoContext`). 보통 프로세스는 0 이라 `+0x1f9 JZ +0xa2` 로 안 탄다.

### 질문 2 — 예외마다 `EXCEPTION_RECORD`

- 표는 위 "예외 코드 표". 줄 세부만 (결과서):
  - **`#PF` 읽기/쓰기/실행 값**: 오류 코드 비트 1 (쓰기) → 값 비트 0, 비트 4 (명령 가져오기) → 값 비트 3 → **0 읽기 · 1 쓰기 · 8 실행** (9 는 이론상). `P` 비트 (비존재/권한) 는 안 본다. 접근 위반은 `KiPreprocessFault +0x57 CMP EAX,0x10000004` → `+0x91 ~ +0x9b` (실행 + 유저면 `+0x186 ~ +0x18b` ATL thunk 확인 `KiCheckForAtlThunk +0x240`) → **코드만** `0xC0000005` 로, 개수·`[0]`·`[1]` 은 그대로.
  - **`#GP`**: 입구 `+0x33e ~ +0x356` (`ECX 0x10000001`, `EDX 2`, `R9 = [RBP+0xe0] & 0xFFFF`, `R10 0`) → `KiPreprocessFault +0x21a / +0x220` → `+0xc3 KiOpDecode` → 특권이면 `+0x1d8 KiOp_Priv` (`0xC0000096`, `+0x4 AND [레코드+0x18],0` 개수 0). 해석 못 하거나 특권이 아니면 `+0x1e2 ~ +0x20b` 가 `0xC0000005`, 개수 2, `[0] = 0`, `[1] = −1` (디코더 결과 `[RBP+0x28]` 이 0 일 때 — 칸 뜻 `[추측]`).
  - **`#DE`**: `KiPreprocessFault +0x22b` (`R14D = 0x20000000`) · `+0x231` (`0xC0000094`) → 나누기 명령이면 `KiOp_Div` — `+0x21 CMP [레코드],0xc0000094` · 나누는 값을 읽어 `+0xdd TEST RCX,RCX` · `JNZ` 면 **`+0x104 MOV [RDI],0xc0000095`**.
  - **`#BP` 두 군데 −1**: 레코드 주소 (`KiBreakpointTrap +0x34d DEC R8`) 와 문맥 `Rip` (`KiDispatchException +0x707 DEC [RBX+0xf8]`). 트랩 `Rip` 는 그대로 두었다가 질문 3 에서 새 값으로 덮인다. 그래서 처리기가 `Rip` 를 안 고치면 같은 `int3` 을 또 실행한다.

### 질문 3 — 유저 스택에 쌓기 · 쓰기 실패

- 모양·차례는 맨 위 표들. 여기는 앞뒤 준비만.
- **커널 스택에서 먼저 만든다**: `+0xa2 R15D = 0x10001f` · `+0xa8 [RBP+4]` (이름표) → `+0xae CMP [RBP],DIL` · `+0xb2 JNZ` (유저) → `+0x2d4 TEST [KeFeatureBits],0x800000` → 켜져 있으면 `+0x2e5 R15D = 0x10005f` (꺼지면 `+0x2df JZ` 로 `0x10001f` 그대로). CET 의 `0x10009f` (`+0xb8 ~ +0xc4`) 는 커널 모드 갈래라 유저는 안 탐. → `+0xe3 RtlGetExtendedContextLength3` (KUSD `+0x3ec` 비트 1 `CompactionEnabled` 면 `+0x300 ~ +0x362` 에서 `EnabledFeatures | EnabledUserVisibleSupervisorFeatures` 마스크) → `+0x103` · `+0x108 SUB RSP,RAX` → `+0x12b RtlInitializeExtendedContext2` → `+0x13d KeContextFromKframes`. `KeContextFromKframes` 는 이름표를 **받은 버퍼 `+0x30`** 에서 읽어 (`+0x47`) 덩어리마다 채운다 (CONTROL `+0x53` · SEGMENTS `+0x99` · INTEGER `+0xb5` · XSTATE `+0x188` · FLOATING_POINT `+0x259` · DEBUG `+0x35a`).
- run42 와 비교: 유저 APC (`KiInitializeUserApc +0x1fb`) 도 같은 `KeContextFromKframes` 를 쓰지만 **유저 스택에 바로** 만들고, 예외는 **커널 버퍼에 만든 뒤 복사** 한다 (결과서).
- **디버거 없는 첫 기회 흐름** (줄 기준): `+0x22e ~ +0x24a` 보통 64비트 프로세스 (아니면 `+0x5d1 ~ +0x62a` WoW64·최소 프로세스 — 이름만) → `+0x250` 첫 기회 → `+0x267 KdIsThisAKdTrap` → `+0x27f ~ +0x2a1` (`KdIgnoreUmExceptions`·`[프로세스+0x308]` DebugPort·`0x80000033`) → `DebugPort == 0` 이고 `KdIgnoreUmExceptions == 0` 이면 `+0x2be KdTrap` (**커널 디버거가 붙어 있으면 여기서 멈출 수 있음** — 아래 디버거 메모) → `KdTrap` 0 이면 `+0x3d6 DbgkForwardException(레코드, 1, 0)` (포트 없으면 `+0x268 XOR AL,AL`) → `+0x3dd JNZ` 안 탐 → `+0x3e3` 유저 처리기로 보냄.
- **쓰기 실패**: `__try` 범위 `[+0x41a, +0x5d1)` · `[+0x62a, +0x652)`, 필터 `KiDispatchException$filt$0` (rva `0x6cfba0` — 새 레코드를 `[RBP+0xa0]` 에 복사하고 늘 1), 처리기 `+0x652` (결과서). 처리기: `SpecialApcDisable` 되돌림 (`+0x54d ~ +0x558` 에서 1 뺀 것, `[RBP+0xc]` 가 켜졌을 때) → 새 코드 `0xC00000FD` 면 원래 레코드를 스택 넘침 레코드로 덮고 (`ExceptionAddress` 는 유지) `R15 = S0` 로 `+0x419` 다시 → 그 밖이면 `EDI = 1` 로 `+0x7a5` 둘째 기회. 스택 넘침 되풀이를 막는 줄은 없다 (`[동적]` 6).

### 질문 4 — 두 번째 기회 (커널 쪽)

- `NtRaiseException` (손으로 쓴 asm): `RCX` 레코드 · `RDX` 문맥 · **`R8B` 첫 기회**. `+0x15` 트랩 `Rax` 칸을 0 (run77 — 성공으로 나가게), 비휘발·`Xmm6 ~ 15` 를 예외 프레임에, `+0x85 [RSP+0x20] = R8B` → `+0x91 KiRaiseException` → 0 이면 `IRETQ` 나가기 (`KiInitiateUserApc +0x16b` 루프 등 `KiExceptionDispatch` 와 같은 나가기, `SYSRET` 아님), 아니면 `+0x98 JNZ` 오류 상태로.
- `KiRaiseException`: 유저 호출이면 (`+0x4a SIL = [스레드+0x232]`) `+0x71` 개수를 읽어 **`> 15` 면 `0xC000000D`** (`+0x79 · +0x7c · +0x9c`) · 아니면 `+0xb3` 복사 → `+0xd8 KyRaiseException` (유저 `CONTEXT` 를 읽고 검증, **`+0x16f ExceptionActive = 1`**, `+0x18d KeContextToKframes` 로 트랩에 써 넣음) → `+0xe6 BTR` → `+0xfe KiDispatchException`.
- **둘째 기회** (`[RBP+0x1d0] == 0`, 유저): `+0x95` 첫 기회 갈래 건너뜀 → 문맥·`KiPreprocessFault` 까지 같음 → `+0x250 JZ +0x7a5` →
  1. `+0x7ae DbgkForwardException(레코드, 1, 1)` — 디버그 포트 없으면 거짓. 둘째 기회면 그 안에서 먼저 프로세스 `Flags3.Crashed` (비트 2) 를 켜고 `PspRecordCrashedProcessIntoBlackbox`·`PsSetProcessTelemetryAppState(…, 4)` 를 부른다 (`DbgkForwardException +0x59 / +0x61 ~ +0x8b`, 결과서).
  2. `+0x7c3 DbgkForwardException(레코드, 0, 1)` — 예외 포트 (`[프로세스+0x240]`) 가 0 이면 `+0x39c DbgkpSendErrorMessage` 로 보내고, 그 반환 ≥ 0 이면 "처리됨" (참). 보내기 실패면 거짓.
  3. 둘 다 거짓이면 **`+0x7d0 EDX = [레코드]` · `+0x7d3 RCX = −1` · `+0x7d7 ZwTerminateProcess`**. 끝 상태 = 예외 코드 그대로 (표시 코드는 이미 풀린 뒤). `ZwTerminateProcess(-1, …)` 는 run79 의 `NtTerminateProcess` 자기 프로세스 길 (`+0x243 PspTerminateAllThreads` 뒤 `+0x161 PspExitThread`) 이라 `+0x7dc` 로 돌아오는 건 실패 때뿐이다 `[정적, run79]`.
- 예외 포트가 있는 보통 데스크톱 (WER) 이면 2 에서 "처리됨" 으로 끝날 수 있다 — 그 뒤 프로세스가 어떻게 끝나는지는 이번 범위 밖 (동적 5).

### 질문 5 — `__fastfail` (`int 0x29`)

- `KiRaiseSecurityCheckFailure +0x0 SUB [RSP],2` — 돌아올 `Rip` 를 `int 0x29` 자기 자리로. `CS = 0x23` (WoW64) 면 `Rip` 위 32비트 지움 (`+0x5 ~ +0xd`). 조각 끝 `+0x34e R9 = [RBP−0x48]` (트랩 `Rcx` = 실패 코드) · `+0x352 ECX = 0xC0000409` · `+0x357 EDX = 1` · `+0x35c R8 = Rip` → `+0x363 KiFastFailDispatch`.
- `KiFastFailDispatch`: 레코드를 `KiExceptionDispatch` 와 같은 칸에 채우되 **`+0x6b ExceptionFlags = 1`** (EXCEPTION_NONCONTINUABLE). `+0x89 ~ +0x98` 이전 모드 → 커널이면 `+0xad KiBugCheckDispatch(0x139)`, 유저면 **`+0xb2 [RSP+0x20] = 0` (첫 기회 0)** → `+0xc1 KiDispatchException` → 질문 4 의 `+0x7a5` → `ZwTerminateProcess(-1, 0xC0000409)`. `KiUserExceptionDispatcher` 에 닿지 않는다.
- 같은 길을 쓰는 입구 (이름만): `KiControlProtectionFault` (유저면 `ECX 0xC0000409`, `R9 0x39`), `KiBoundFault` 의 한 갈래 (`R9 0x1c`).
- 지도 안에서 `__fastfail` 이 나올 수 있는 자리: `LdrpInitializeProcess` 강제 종료 `__fastfail(3)` 2곳 · `__fastfail(0x45)` 1곳 (run18·run19), `LdrpInsertDataTableEntry +0x46 / +0x4b INT 0x29` (run35). 그 커널 쪽 끝이 이 길이다 `[정적]` (test.exe 는 안 탈 것 `[추측]`, flow.md 2.4).

### 질문 6 — syscall 안 포인터 실패 · `PspInitializeThunkContext` 실패 갈래

- **syscall 안 실패는 커널 모드 예외** (`KiPageFault` 의 이전 모드 = 커널) → `KiDispatchException +0x16e` 에서 유저 갈래로 안 감 → `+0x174 ~ +0x19c KdTrap` → `+0x367 ~ +0x372 RtlDispatchException(레코드, 문맥)` 가 커널 프레임의 SEH 를 찾고 그 `__except` 로 → syscall 이 돌아온다. 없으면 `+0x37a ~ +0x395 KdTrap` 뒤 `+0x3b1 ECX = 0x1e` · `+0x3b6 KeBugCheckEx` (KMODE_EXCEPTION_NOT_HANDLED). 유저 처리기는 어느 쪽도 안 거친다 `[정적]` (결과서).
  - 예: `NtAllocateVirtualMemory` (rva `0x904f90`) `__try` `[+0x79, +0xc2)` · `[+0x257, +0x261)`, 필터 `$filt$0` (rva `0xb063a0`) 는 `PreviousMode (+0x232) ≠ 0` 이면 1 — **유저가 부른 syscall 안에서 난 예외만 자기 처리기가 잡는다** (결과서가 `.pdata` 로 읽음, `$filt$1` 은 같은 꼴 `[추측]`). 처리기 본문이 상태를 어떻게 돌려주는지는 안 읽음 `[추측]`.
- **`PspInitializeThunkContext` 실패 갈래** (run16 표 6번 "오류 길" 의 정체, 2.3):
  - `__try` `[+0x22f, +0x2ab)`, 필터 상수 1, 처리기 `+0x2ab` (`.asm` 틈 `+0x2ab ~ +0x2b9` — 결과서 `.pdata`. 처리기 15바이트 본문은 안 읽음).
  - `EBX` 를 정하는 줄 (flow-writer `.asm`): `+0x26e` (`RtlInitializeExtendedContext2` 반환) · `+0x285` (`RtlCopyContext` 반환) · `+0x2db` (`PspSetContextThreadInternal` 반환) · 처리기 `+0x2ab` (무엇을 넣는지 `[공백]` — 예외 코드 `[추측]`).
  - `+0x2fa TEST EBX,EBX` · `+0x2fc JS +0x37b` → 레코드를 `[RBP+0x30]` 에 만듦: `+0x38a` 코드 = `EBX` · `+0x37b` 플래그 0 · `+0x391` 링크 0 · `+0x37f / +0x386` 주소 = **`PsNtdllExports+0x8` (`LdrInitializeThunk`)** · `+0x38d` 개수 0 → `+0x39b` · `+0x3a8 PspGetBaseTrapFrame` (트랩 · 예외 프레임 = 트랩 − 0x140) → **`+0x3b4 [RSP+0x20] = 0` (첫 기회 0)** · `+0x3b9 R9B = R14B` (= 1 유저, `+0x121 MOV R14D,1`) → **`+0x3c3 KiDispatchException`** → 질문 4 의 둘째 기회 → `ZwTerminateProcess(-1, EBX)`.
  - 그러니 "유저 예외" 이긴 하나 **유저 처리기 기회가 없다** — 새 스레드가 `LdrInitializeThunk` 에 가기도 전에 프로세스가 끝난다 `[정적]`.

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리.

```
[유저 모드에서 CPU 예외 — 예: 잘못된 주소 읽기]
nt!KiPageFault   (입구 asm, 트랩 프레임 = RBP − 0x80)
 │  +0x348 CR2 · +0x358 FaultAddress · +0x37e ~ +0x383 FaultIndicator (0 / 1 / 8)
 ├─ (1) +0x38b  MmAccessFault(오류 코드, CR2, 모드, 트랩)       +0x38d JGE ≥ 0 → 예외 없이 나감
 │  +0x3f2 ~ +0x428  0xC0000005 → ECX = 0x10000004 (표시 코드), EDX = 2
 └─ (2) +0x442  KiExceptionDispatch(ECX, EDX, R8 = Rip, R9 = 0/1/8, R10 = CR2)   돌아오지 않음
      │  +0x67 ~ +0x81 레코드 · +0x8c 이전 모드 · +0x90 첫 기회 1 · +0x9f 유저면 곧장
      ├─ (1) +0x145  KiDispatchException(레코드, 예외 프레임, 트랩, 1, 1)
      │    │  +0xa2 이름표 0x10001f · +0x2d4 KeFeatureBits 비트 23 → +0x2e5 0x10005f
      │    ├─ (1) +0xe8   RtlGetExtendedContextLength3
      │    ├─ (2) +0x108  스택 늘리기 확인 (__chkstk 후보) · SUB RSP
      │    ├─ (3) +0x130  RtlInitializeExtendedContext2
      │    ├─ (4) +0x142  KeContextFromKframes(트랩, 예외 프레임, 커널 버퍼)
      │    ├─ (5) +0x163  KiPreprocessFault        0x10000004 → 0xC0000005 (+0x9b)
      │    │  +0x16e 유저 갈래 · +0x250 첫 기회
      │    ├─ (6) +0x26c  KdIsThisAKdTrap
      │    ├─ (7) +0x2c3  KdTrap                   DebugPort 0 · KdIgnoreUmExceptions 0 일 때 (커널 디버거)
      │    ├─ (8) +0x3db  DbgkForwardException(레코드, 1, 0)    포트 없음 → 거짓
      │    │  +0x3e3 CLI · +0x3e4 트랩 EFlags TF 지움
      │    │  +0x430 ~ +0x452  M · E · X · C 계산 · +0x460 ~ +0x49c CONTEXT_EX 만들기     __try 시작 +0x41a
      │    ├─ (9) +0x4b0  ProbeForWrite(C, S0 − C, 16)
      │    │  +0x4b4 [M+0x18] = S0 · +0x4c3 [M] = 문맥 Rip
      │    ├─ (10) +0x4d1 KeCopyExceptionRecord(E, 레코드)
      │    ├─ (11) +0x534 RtlpCopyExtendedContext(1, X, …)    CONTEXT (C) · XSAVE 꼬리
      │    │  +0x53b · +0x545 CONTEXT_EX 0x20
      │    ├─ (12) +0x571 KePopulateContinuationContext       CET 만
      │    │  +0x578 트랩 Rsp = C · +0x583 SegCs = 0x33 · +0x593 Rip = PsNtdllExports+0x20
      │    └─ (13) +0x5a3 KiSetupForInstrumentationReturn     계측 콜백 있을 때만
      │  +0x1b4 유저 · +0x1e9 KiInitiateUserApc 루프 · +0x22a KiRestoreSetContextState (비트 27) · +0x262
      └─ ══▶ 조각 +0x4b1 SWAPGS · +0x4b4 IRETQ   (KVA 그림자면 +0x49a KiKernelExit)
ntdll!KiUserExceptionDispatcher   RSP = C (CONTEXT), RSP + 0x4f0 = 레코드          → Q148b (run82)
 ├─ (1) +0x2e  RtlDispatchException(RSP + 0x4f0, RSP)
 └─ (2) +0x53  NtRaiseException(…, R8B = 0)        RtlDispatchException 거짓일 때

[스택 쓰기 실패]  처리기 +0x652 (틈): 0xC00000FD → 레코드 덮고 +0x419 다시 · 그 밖 → +0x7a5

[둘째 기회 — NtRaiseException(…, 0) · __fastfail · PspInitializeThunkContext 실패 · 쓰기 실패]
nt!KiDispatchException  +0x250 JZ → +0x7a5
 ├─ (1) +0x7b3  DbgkForwardException(레코드, 1, 1)      Crashed · 블랙박스 · 디버그 포트
 ├─ (2) +0x7c8  DbgkForwardException(레코드, 0, 1)      예외 포트 · +0x3a1 DbgkpSendErrorMessage
 └─ (3) +0x7dc  ZwTerminateProcess(-1, ExceptionCode)   → run79 NtTerminateProcess 길

[들어오는 쪽 셋]
nt!NtRaiseException  +0x85 첫 기회 → +0x96 KiRaiseException  +0x7c > 15 → 0xC000000D · +0xdd KyRaiseException (+0x16f ExceptionActive · +0x192 KeContextToKframes) · +0xe6 BTR 비트 28 · +0x103 KiDispatchException
nt!KiRaiseSecurityCheckFailure  +0x0 Rip − 2 → +0x368 KiFastFailDispatch  +0x6b 플래그 1 · +0xb2 첫 기회 0 → +0xc6 KiDispatchException   (커널이면 +0xb2 ← KiBugCheckDispatch 0x139)
nt!PspInitializeThunkContext  +0x2fc EBX < 0 → +0x37b 레코드 (주소 PsNtdllExports+0x8) · +0x3b4 첫 기회 0 → +0x3c8 KiDispatchException
```

## 확인된 것

- `[정적]` 유저 모드 CPU 예외는 입구 → `KiExceptionDispatch +0x140` (첫 기회 1) → `KiDispatchException` → `+0x13d KeContextFromKframes` (커널 버퍼) → `+0x15e KiPreprocessFault` (표시 코드 → 진짜 코드) 로 간다. `#PF` 는 `+0x386 MmAccessFault` 가 실패 (`+0x38d JGE` 안 탐) 일 때만.
- `[정적]` 유저 스택 배치: `M = (A − 0x28) & ~0xF`, `E = M − 0xA0`, `X = E − 0x20`, `C = X − 0x4D0` (`+0x430 ~ +0x452`), 새 `RSP = C`. `CONTEXT` `C+0` · `CONTEXT_EX` `C+0x4d0` (늘) · 레코드 `C+0x4f0` · 머신 프레임 `Rip` `C+0x590` · `Rsp` `C+0x5a8`. ntdll 쪽 `+0x1f ADD RCX,0x4f0` · `UNWIND_INFO 0x590` 과 맞는다.
- `[정적]` 트랩 프레임은 `EFlags` TF (`+0x3e4`) · `Rsp` (`+0x578`) · `SegCs` (`+0x583`) · `Rip = PsNtdllExports+0x20` (`+0x593`) 만 바뀌고, `KeContextToKframes` 없이 `IRETQ` 로 나간다.
- `[정적]` 기본 `ContextFlags` `0x10001f`, `KeFeatureBits` 비트 23 이면 `0x10005f` (`+0xa2` · `+0x2d4` · `+0x2e5`).
- `[정적]` `int3` 은 레코드 주소·문맥 `Rip` 모두 `int3` 자리 (`KiBreakpointTrap +0x34d`, `KiDispatchException +0x707`). AV 정보 `[0]` 0/1/8 (`KiPageFault +0x37e ~ +0x383`).
- `[정적]` 둘째 기회 · `__fastfail` (`KiFastFailDispatch +0xb2` 첫 기회 0) · 스택 쓰기 실패 · `PspInitializeThunkContext` 실패 (`+0x3b4` 첫 기회 0) 는 모두 `KiDispatchException +0x7a5` → `DbgkForwardException` 둘 → `+0x7d7 ZwTerminateProcess(-1, 예외 코드)`.
- `[정적]` syscall 안 포인터 실패는 커널 모드 예외라 커널 SEH (`+0x367 ~ +0x372 RtlDispatchException`) 로 끝나고 유저 처리기는 안 거친다.
- `[관찰]` (run44 사진, flow-writer 대조) KUSD `ProcessorFeatures[17]` 1 · `XState.EnabledFeatures` `0xe7` · `Size` `0x980` · `ControlFlags` 3 — `KeFeatureBits` 비트 23 이 켜졌다는 간접 근거 (위 덧붙임 ①). 비트 자체는 사진에 없다.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① flow-writer 보탬: `CONTEXT_EX` 넷째 칸 `{0, 0}` 의 출처 = 앞머리 `+0x60 XORPS XMM0,XMM0` · `+0x63` · `+0x6a` 가 `[RBP+0x140 ~ +0x15f]` 를 지움 (결과서는 "16바이트씩 두 번 씀" 만). 그리고 `_CONTEXT_EX` 는 공개 PDB 에 없어 칸 이름은 `[추측: 이름]` (덧붙임 ②).
- ② flow-writer 보탬 (결과서 `[추측]` "XSAVE 계열" 을 근거로 이음): `KeFeatureBits` 비트 `0x800000` = 비트 23 = `KiInitializeKernel +0x2e5 BTS RBX,0x17` (KUSD `+0x3d8 ≠ 0` 일 때) = KUSD `ProcessorFeatures[17]` (run70, 짐 지도 7.6.10). 사진 값으로 이 VM 은 켜짐 쪽 `[추측: 사진 간접]` (덧붙임 ①).
- ③ flow-writer 보탬: `PspInitializeThunkContext` 실패 갈래의 `EBX` 출처에 **`+0x2db` (`PspSetContextThreadInternal` 반환)** 도 있다 (결과서는 `+0x26e` · `+0x285` 둘). 이전 모드 `R14B = 1` 은 `+0x121 MOV R14D,1`, 트랩·예외 프레임은 `+0x39b` · `+0x3a8 PspGetBaseTrapFrame` (결과서는 "이전 모드 R14B" 만).
- ④ flow-writer 보탬: 원문 5 의 `NtRaiseException(…, FALSE)` 를 부르는 유저 줄 = `KiUserExceptionDispatcher +0x4b XOR R8B,R8B` · `+0x4e` (결과서는 "나머지는 Q148b").
- ⑤ flow-writer 보탬: `KiDispatchException +0x103` · `KiRaiseException +0xb3` · `KeCopyExceptionRecord +0x5c` 의 callee 는 `_calls.tsv` 에 `alias:` 로 찍힌다 — 글에는 하는 일 (스택 늘리기 확인 · 메모리 복사) 로 적고 후보 이름을 붙였다.
- ⑥ flow-writer 보탬: 짐 지도 3.15 머리에 "유저 예외 줄" (트랩 프레임 `FaultAddress`·`FaultIndicator` 쓰는 줄 = 유저 모드 페이지 폴트 입구 — 7.2 `P1~P1a` 모름 칸의 후보) — 아래 "짐 지도에 반영한 것".

## 정정

- **run48 (호출 사슬 뒤 "① 에서 읽는 자리" 표 `PsNtdllExports` 줄)** — "`PspInitializeThunkContext +0x1a1 · +0x37f` (`+0x8` → 새 스레드 `Rip`)"
  - → `+0x1a1` 만 새 스레드 `Rip` (`ctx2.Rip`, run16) 이다. **`+0x37f` 는 실패 갈래** (`+0x2fc JS +0x37b`) 에서 같은 `+0x8` (`LdrInitializeThunk`) 을 **예외 레코드 `ExceptionAddress`** (`+0x386 [RBP+0x40]`) 로 읽는 줄이다 `[정적]` (flow-writer `.asm`). 짐 지도 7.6.2 짝 표의 같은 줄 (`(+0x8 → 트랩 Rip, 3.15)`) 도 고쳤다. run48 문서 맨 위에 정정 안내 한 줄.
- **PE_emulator `Src/nt/Process.cpp:1953` 주석** (우리 문서 아님 — PE_emulator 가 고칠 몫): "`STATUS_ACCESS_VIOLATION` … 진짜는 SEH `+0x2ab` → 유저 예외 `KiDispatchException`"
  - 줄 번호 (`+0x2ab` 처리기 · `+0x37b` · `+0x3c3`) 는 **맞다**. 다만 이 줄은 `RtlCopyContext` 흉내 (`PspInitializeThunkContext`) 안이라 메모리 syscall 이야기가 아니고, 그 `KiDispatchException` 은 **첫 기회 0** (`+0x3b4`) → 유저 처리기 없이 둘째 기회 → `ZwTerminateProcess(-1, EBX)` 이다. "에뮬레이터 규칙: 멈춘다" 와 결과가 같다.
- **run16 확인 — 고칠 곳 없음**: run16 ③ 표 6번 "쓰기 실패면 SEH 착지점 `+0x2ab` (원본 바이트) → 오류 길" 과 9번 `+0x2f5` (ret `+0x2fa`) 는 맞다. "오류 길" 이 무엇인지만 비어 있었고 이번에 채웠다 (질문 6). run16 문서는 고치지 않았고 정정 안내도 안 넣었다. flow.md 2.3 6번에 (run81) 한 줄을 붙였다.

## flow.md · 짐 지도에 반영한 것

- flow.md: 3장 새 절 "CPU 예외를 유저 예외로 넘기기" (그림엔 안 넣음 — test.exe 정상 길엔 CPU 예외가 없고 `[추측]`, 흐름도 범위 (진입점까지) 밖) · 2.3 `PspInitializeThunkContext` 6번 아래 실패 갈래 한 줄 · 2.4 "일찍 끝나는 길" 끝에 `__fastfail` 커널 쪽 끝 한 마디 · 기준 · 4장 · 5장.
- 짐 지도 (짐 지도 바퀴 아님 — **새 짐 칸 없음, 칸 수·2장 숫자 그대로**). 이미 있는 줄에 근거만:
  - 7.6.2 짝 표 `PsNtdllExports` — 읽는 줄 `KiDispatchException +0x58c` (`+0x20` `KiUserExceptionDispatcher` → 트랩 `Rip`, 유저 예외 때) 더함, `PspInitializeThunkContext +0x37f` 뜻 고침 (위 정정).
  - 7.6.10 `KeFeatureBits` — 읽는 줄 `KiDispatchException +0x2d4` (비트 23 → 유저 예외 문맥 이름표 `0x10005f` / `0x10001f`).
  - 3.15 `_KTRAP_FRAME` 머리 "(run81) 유저 예외 줄" (은퇴 뒤라 표에 안 넣음): 바뀌는 칸 넷, `ExceptionActive` ← `KyRaiseException +0x16f`, 그리고 7.2 `P1~P1a` 모름 칸 중 `FaultAddress` (← `KiPageFault +0x358`) · `FaultIndicator` (← `+0x383`) 의 쓰는 줄 후보 `[추측: 유저 모드 페이지 폴트도 같은 맨 아래 트랩 프레임]` (Q78d4).
  - 기준 run81 줄 · 판 목록 (서른일곱째판) · 8장 두 표.

## 아직 확인 필요

동적 — **Q104 VM 단계** 에 붙인다 (결과서 "동적분석으로 확인해야 할 것" 번호 그대로, 오케스트레이터. 이번 묶음은 VM 안 함). 커널 멈춤점은 모든 프로세스에 걸리니 test.exe 일 때만.

- **먼저 알아 둘 것**: 아래 멈춤점은 **예외가 실제로 나야** 선다. test.exe 정상 길엔 CPU 예외가 없다 `[추측]` (`sample/*.c` 에 그런 글자 없음 `[소스]`). 후보는 셋 — (가) `OutputDebugStringA` 의 `RaiseException(0x40010006)` 이 커널을 거친다면 그것 (Q148b 가 정하면 1·2·5 를 test.exe 로) → **(해결됨 → run82)** 디버거가 없으면 커널을 안 거친다 (`RtlRaiseException +0x17e` `BeingDebugged` → `+0x188 RtlDispatchException`) — test.exe 로는 유저 쪽만 보이니 1·2·5 는 (다) 로, (나) 조건 없이 다른 프로세스의 예외, (다) 아래 4 의 새 샘플.

1. 실제 `ContextFlags` (`0x10001f` / `0x10005f`) 와 `XState.Length`·꼬리 크기 — `nt!KiDispatchException+0x142` 직후 `dd @rbp+4 L1`, 또는 `ntdll!KiUserExceptionDispatcher` 입구 `dd @rsp+0x30 L1`. 같이 `dq nt!KeFeatureBits L1` (`& 0x800000`) — 짐 지도 7.29 1 과 같은 읽기 (덧붙임 ① 닫힘).
2. 새 `RSP` 가 `((A − 0x28) & ~0xF) − 0x590` 인지, `RSP+0x590`·`+0x5a8` 이 `Rip`·`S0` 인지 — `nt!KiDispatchException+0x5a3` (`dq @r12+0x168 L1` · `+0x180` · `dw @r12+0x170`), `ntdll!KiUserExceptionDispatcher` 입구 `dq @rsp+0x590 L1` · `dq @rsp+0x5a8 L1`.
3. 표시 코드 `0x10000004` → `0xC0000005` 바뀌는 때 — `nt!KiDispatchException+0x163` 앞뒤 `dd poi(@r14) L1` (`r14` = 레코드, `+0x38`).
4. **새 샘플 필요** (대기열 **Q156**) — `#GP` 의 `[0] = 0`·`[1] = −1`, `#DE` 의 `0xC0000095` 갈래, `int3` 의 `CONTEXT.Rip` (= `int3` 자리) 는 **예외를 일부러 내는 작은 프로그램** 이 있어야 한다 (`sample/` 에 없음 `[소스]`). 잘못된 주소 읽기·쓰기·실행 (`[0]` 0/1/8) · `ud2` · 특권 명령 (`hlt`) · 0 나누기 · `INT_MIN / −1` · `int3` · `__fastfail` 를 `__try` 로 하나씩. 이 샘플이 있으면 1·2·3·5·6 도 같이 닫힌다. 만들지·언제 할지는 **사용자 결정** (run80 의 Q154 와 같은 꼴).
5. 둘째 기회에서 `DbgkForwardException(…, 0, 1)` 이 참을 돌려주나 (WER 있는 VM) — `nt!KiDispatchException+0x7c8` 뒤 `al`. 거짓이면 `+0x7dc` 에서 종료 상태. 그리고 **KDNET 이 붙어 있어 `+0x2c3 KdTrap` 이 유저 예외를 먼저 가로채나** (`dd nt!KdIgnoreUmExceptions L1`) — 디버거 메모.
6. 스택 넘침 되풀이 (`+0x652` 처리기 → `+0x419` 다시) 가 끝나나.
7. 질문 6 의 syscall 처리기 본문 — 상태를 `RAX` 로 돌려주는 줄 (아래 정적 ③ 으로도 닫힘).
8. 계측 콜백 (`KiSetupForInstrumentationReturn`, `dq <프로세스>+0x168 L1` = 0 기대)·CET (`KePopulateContinuationContext`, `KTHREAD +0x74` 비트 20 = 0 기대) 이 정말 안 타나. 계측 칸은 run16 의 `PspInitializeThunkContext +0x20e` 갈래와 같은 칸이라 한 번에.

- 추천 멈춤점 (결과서): `nt!KiExceptionDispatch+0x140` (`rcx` 레코드) · `nt!KiDispatchException+0x163` · `+0x4b0` · `+0x571` · `+0x5a3` · `+0x7c8` · `+0x7dc`, 유저 `ntdll!KiUserExceptionDispatcher`.

정적 — 다음 바퀴 후보 (대기열 **Q155** = ② ~ ⑦, ① 은 Q148b 로):

- ① **(해결됨 → run82)** (sast/79 조정자 덧붙임 1 · 2 · 3 — `CONTEXT_EX` 는 이름표에 `0x100040` 이 다 있을 때만 읽음, 머신 프레임은 `Rip` · `Rsp` 둘만, `Entries` 는 CET 그림자 스택용, `RtlRaiseException` 은 커널 안 거침) **Q148b (run82, 이미 대기열)** 에 같이 볼 것: `KiUserExceptionDispatcher` 이하 · `CONTEXT_EX` 0x20 을 읽는 쪽 (`RtlpGetExtendedContext…` — 이름표 비트 `0x40` 만 보고 건너뛰나) · `0x10001f` 로 쌓고 KUSD `XState` 는 진짜 값 (`0xe7`) 일 때 ntdll 이 괜찮나 · `P1Home ~ P6Home`·`VectorRegister` (쓰레기) 를 읽나 · `RtlRaiseException` 이 커널을 거치나 (위 동적 (가)) · 짐 지도 3.8 `RtlpContinuationContextMachineFrameEntries` (= `KiUserExceptionDispatcher` 의 `UnwindData`) 가 머신 프레임 (`+0x590`) 과 어떻게 이어지나.
- ② `PspInitializeThunkContext +0x2ab ~ +0x2b9` 처리기 15바이트 — `EBX` 에 무엇을 넣나 (= 프로세스 끝 코드). objdump 한 번.
- ③ `NtAllocateVirtualMemory` 등 메모리 syscall `__except` 본문 — 상태를 어떻게 돌려주나 (`0xC0000005` 그대로? 동적 7 의 정적 몫).
- ④ **IDT 표** — CPU 벡터 번호 ↔ 입구 함수 (이번엔 이름으로 짝지음 `[추측: 이름]`). **Q144 ①** (IDT 표 — 시계 벡터) 과 같은 표라 묶을 만함.
- ⑤ `KiPreprocessFault` `#GP` 디코더 칸 `[RBP+0x28]` 뜻·`KiOpDecode` 해석기 표 — 어떤 명령이 `KiOp_Priv` (`0xC0000096`) 로 가고 어떤 게 `0xC0000005` 로 가나. Unicorn 이 `#UD`/특권을 못 가르면 PE_emulator 에 필요.
- ⑥ `KiDispatchException +0x652` 되풀이 끝 조건 (동적 6 의 정적 몫) — 두 번째 시도에서도 `0xC00000FD` 면 끝없이 도나.
- ⑦ 작음: `KiPageFault +0x3a6 ~ +0x3c8` GS 베이스 구제 갈래의 뜻 `[추측]`, WoW64·Pico·CET 갈래 (이름만 — PE_emulator 가 x64 만이면 불필요).

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점** `[정적]`: 커널 디버거 (KDNET) 가 붙어 있으면 유저 예외 첫 기회에 `KiDispatchException +0x2be KdTrap` 을 먼저 부른다 (`DebugPort == 0` · `KdIgnoreUmExceptions == 0` 일 때). WinDbg 가 거기서 멈추면 `KiUserExceptionDispatcher` 까지 안 가 보일 수 있다 `[추측]`. → windbg-kd-runbook pitfalls 에 추가 제안: "유저 예외를 보려는 dast 는 `KiDispatchException+0x2c3` 에서 `al` 을 보거나 `sxd` 류로 첫 기회를 넘겨야 할 수 있다. `KdIgnoreUmExceptions` 값을 먼저 읽는다" (명령 이름은 dast-mentor 가 확인).
- 도구 (결과서 "스크립트 문제", 오케스트레이터 몫 — 하네스 제안): ① `.asm` 에서 **`__except` 본문이 "읽지 않은 틈" 으로만** 나온다 (`KiDispatchException +0x652 ~ +0x6cf`, `PspInitializeThunkContext +0x2ab ~ +0x2b9`). scope table 의 `JumpTarget` 을 따라가 본문으로 붙이면 놓치지 않는다 (run27·run28·run81 같은 일). ② 손으로 쓴 asm 입구는 `_calls.tsv` 에 `frag:FUN_…+0x…` 로만 찍히고 함수 기준 오프셋은 `.asm` 줄 끝 `(= … +0x…)` 에만 있다 — `_calls.tsv` 에 함수 기준 칸을 같이 두면 좋다.
- 도구 (flow-writer): `_CONTEXT_EX` 처럼 공개 PDB 에 없는 구조체는 `pdb_struct.py` 가 "없음" 을 준다. 칸 이름을 쓸 땐 `[추측: 이름]` 으로.

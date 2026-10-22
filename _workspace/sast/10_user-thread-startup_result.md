# 10 user-thread-startup — 정적 분석 결과

- 요청서: `_workspace/sast/10_user-thread-startup.md` (Q6)
- 날짜: 2026-10-22 (02_sprint)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`, sha256 `d90c69cf…fde2f2`), pdb guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID와 같음)
- ntoskrnl analyze: **다시 하지 않음** (`.gpr` + `_index.tsv` + analyze 로그의 `SetPdb:` 줄 모두 있음). export 만 했다.
- 뽑은 함수 (모두 depth 0, 로그 `ms/ghidra/ntoskrnl.export.sast10{,b,c}.stdout`)
  - 요청서 정규식 `^(KxStartUserThread|PspUserThreadStartup)$` → 2개
  - 넓힌 것 2개 (이유는 "범위를 넓힌 이유"): `KiKernelExit`, `KiLowerIrqlProcessIrqlFlags`
  - 위치: `ms/decomp/ntoskrnl/<함수>.c`, `ms/disassemble/ntoskrnl/<함수>.asm`, 호출표 `ms/disassemble/ntoskrnl/_calls.tsv`
- 버전 확인: **일치**
  - `NtCreateUserProcess +0xc31 (ret +0xc36) → PspAllocateProcess`, `PspInsertThread +0x802 (ret +0x807) → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv`)
  - `PspUserThreadStartup` 호출 9줄이 요청서가 적은 9줄과 같다.
  - 요청서의 계산 `KiStartUserThread+0x28 = KxStartUserThread+0x88` 이 맞다 (run4 `[관찰]` 오프셋과 같음).
  - PDB 자료형 오프셋이 코드가 쓰는 오프셋과 서로 맞는다 (아래 "PDB 자료형으로 확인한 칸").

## 스크립트 문제

실행을 막는 문제는 없다. 다만 이번에 읽기를 어렵게 한 점이 있어 제안만 적는다 (`.claude/` 는 안 고쳤다).

1. **`.asm` 에 "읽지 않은 틈" 표시가 없다.** 함수 범위 안인데 명령이 안 찍힌 곳이 있어도 아무 말이 없다.
   - `PspUserThreadStartup.asm` 의 `+0x7e` 다음이 바로 `+0x91` 이다. 빠진 `+0x80~+0x90` (17바이트)은 SEH `__except` 착지점이다 (앞에서 점프하는 코드가 없어 Ghidra 가 코드로 안 잡음). 원본 바이트로 읽었다.
   - `KxStartUserThread.asm` 의 `+0x22e` 다음이 `+0x3e3` 이다. 빠진 곳은 `call` 과 `add rsp,8` 을 33번 되풀이하는 덩어리(리턴 예측 버퍼를 채우는 모양 `[추측]`, rva `0x6afd9e~0x6afeb4`)다. Ghidra 는 이걸 9바이트 `FUN_` 33개로 쪼갰다. `+0x3e3` 부터는 커널 모드로 돌아가는 가지이고, 유저로 돌아가는 가지는 조각 `FUN_1406afeb5` (맨 아래 `; --- 조각`) 에 있다.
   - 제안: 함수 본체 범위(`.pdata` 범위 또는 Ghidra 본체)와 찍힌 명령 범위 사이 틈을 `; GAP +0x80..+0x90 (명령 없음 — 원본 바이트로 읽을 것)` 한 줄로 표시. `.pdata` 의 `__except` 점프 목표를 같이 알려 주면 더 좋다.
2. **`_calls.tsv` 의 `KxStartUserThread +0x229 +0x22e FUN_1406afeac`** 는 위 리턴 예측 버퍼 채우기 덩어리의 첫 줄이다. 진짜 함수 호출이 아닌데 일반 호출처럼 적힌다. 제안: 대상이 9바이트짜리 `call X; add rsp,8` 연쇄면 `rsbfill` 같은 표시.
3. **조각 오프셋을 WinDbg 식으로 바꾸려면 손 계산이 필요하다.** 조각 `FUN_1406afeb5` 는 함수 시작 +0x345 에 있다 (`.pdata` 는 `0x6afb70~0x6aff9e` 한 덩어리). 조각 머리에 "함수 시작 기준 +0x345" 를 같이 적어 주면 좋다. 이번엔 아래 표에서 직접 계산했다.
4. **`pdb_aliases.py --where KyStartUserThread`** 는 `[function]` 이라고만 하고 "Ghidra 에선 어느 `FUN_` 이 이 범위를 덮나"는 안 알려 준다. 이번엔 `_index.tsv` 의 `FUN_1406afa82` (rva 0x6afa82, 224바이트)가 `KyStartUserThread` 끝부분임을 `.pdata` 로 직접 알아냈다. 제안: `--where` 에 "이 RVA 를 덮는 Ghidra 함수" 한 줄 추가.
5. **`reference.md` 의 ".c 읽을 때 주의" 에 한 줄 더 쓰면 좋다.** `KxStartUserThread.c` 에는 IRQL 올림(`MOV CR8`), `STAC`, `SWAPGS`, `IRETQ` 가 안 보이고, 현재 스레드 읽기(`GS:[0x188]`)가 `SystemReserved1[0xf]` 같은 엉뚱한 전역 이름으로 나온다 (`KxStartUserThread.c`, `PspUserThreadStartup.c`). 맨 위 `// callers: FUN_1406afa82` 도 Ghidra 가 템플릿 코드(`KyStartUserThread`)의 `call` 을 호출자로 잡은 것이다 (실제로 누가 부르는지가 아님). 특권 명령·IRQL 은 `.asm` 만 믿는다.

## 원본 바이트로 읽은 곳과 출처 (Ghidra 함수가 아니거나 `.asm` 에 빠진 곳)

모두 `ms/bin/.../System32/ntoskrnl.exe` 에서 rva 범위를 잘라 `objdump -D -b binary -m i386:x86-64 -M intel` 로 읽었다 (바이너리를 고치지 않음). `.pdata`(RUNTIME_FUNCTION·UNWIND_INFO·SEH 범위 표)는 같은 파일의 예외 디렉터리를 파이썬(임시 폴더 스크립트)으로 읽었다. PDB 이름·자료형은 `llvm-pdbutil-14 dump -publics/-types ms/sym/ntoskrnl.exe.pdb` 로 읽었다. 임시 스크립트는 저장소에 넣지 않았다.

| 읽은 곳 | rva 범위 | 이유 |
|---|---|---|
| `KiStartUserThreadReturn` 과 앞뒤 | `0x6af7a0~0x6afb70` | Ghidra 함수 아님 (`FUN_1406afa82` 끝에 들어 있음) |
| `KxStartUserThread` 전체 | `0x6afb60~0x6aff60` | `.asm` 에 빠진 틈 (`+0x22e~+0x345`) 확인, `.asm` 과 명령이 같은지 대조 |
| `PspUserThreadStartup` | `0x910de0~0x910ee0` | `.asm` 에 빠진 `+0x80~+0x90` |
| `_guard_dispatch_icall` 앞부분 | `0x6b77f0~` | `+0x83` 의 `CALL` 대상 확인 |

`.asm` 의 명령 이름을 같은 주소의 objdump 와 줄마다 맞춰 봤다: `PspUserThreadStartup` 63줄 전부 같고, `KxStartUserThread` 는 뽑은 범위(`rva ~0x6aff5f`) 안 줄이 모두 같았다 (커널 복귀 가지 끝 `+0x3e7~+0x42c` 는 뽑은 범위 밖이라 대조 안 함).

### PDB 자료형으로 확인한 칸 (`[정적]`, PDB 타입 레코드)

- `_KTRAP_FRAME` (크기 0x190): `MxCsr +0x2c`, `Rax +0x30 … R11 +0x60`, `Xmm0 +0x70 … Xmm5 +0xc0`, `Dr7 +0x100`, `Rbp +0x158`, **`Rip +0x168`**, `SegCs +0x170`, `EFlags +0x178`, `Rsp +0x180`, `SegSs +0x188`.
  - 코드의 `RBP` 는 트랩 프레임 시작 + 0x80 이다 (`[RBP-0x54]` = `MxCsr`, `[RBP-0x50]` = `Rax`, `[RBP+0xd8]` = `Rbp`, `[RBP+0xf0]` = `SegCs` 가 모두 맞아떨어진다).
- `_KEXCEPTION_FRAME` (크기 0x140): `P1Home +0x0`, `P2Home +0x8`, `P3Home +0x10`, `Xmm6~15 +0x30~+0xc0`, `Rbx +0x100`, `Rdi +0x108`, `Rsi +0x110`, `R12~R15 +0x118~+0x130`, **`Return +0x138`**.
- `_KTHREAD`: `InitialStack +0x28`, `KernelStack +0x58`, `StateSaveArea +0x60`, `ApcState +0x98` (`Process +0x20`→0xb8, `KernelApcPending +0x29`→0xc1, `UserApcPendingAll +0x2a`→0xc2), `Teb +0xf0`, `Process +0x220`.
- `_ETHREAD.CrossThreadFlags +0x5a0`: bit0 `Terminated`, bit1 `ThreadInserted`.
- `_EPROCESS.Flags3 +0x5fc`: bit0 `Minimal`.
- `_TEB.CurrentLocale +0x108`. `_KWAIT_REASON`: 5 `Suspended`, 6 `UserRequest`.
- 전역 이름 (PDB 공개 심볼): `KiIrqlFlags` (0x140fc6d60), `KeSmapEnabled` (0x140fc6c30), `KiKvaShadow` (0x141201840).

## 질문별 답 [정적]

### 질문 1. 이름표와 `+0x88`

- **답: 예.** `KxStartUserThread +0x88` 은 run4 의 `KiStartUserThread+0x28` 과 같은 주소(`0x1406afbf8`)이고, 그 바로 앞 `+0x83` 이 시작 루틴을 부르는 간접 호출이다 (`_guard_dispatch_icall` 거침).
- 주소 대응: `KxStartUserThread` = `0x1406afb70`, 이름표 `KiStartUserThread` = `0x1406afbd0` = `KxStartUserThread +0x60`.
  - **`+0x0~+0x5f` 는 실제로 도는 코드가 아니라 "풀어 보기용 앞머리"로 보인다 `[정적]+[추측]`.** 근거: 이 범위는 `.pdata` UNWIND_INFO 의 앞머리 설명(스택 0x138 확보, `xmm6~15`·`rbx` 등 저장, 앞머리 길이 0x5f)과 정확히 같고, 새 스레드는 `SwapContext` 의 `RET` 가 곧바로 `+0x60` 으로 보낸다 (아래). `0x6afb70` 을 부르는 곳은 `KyStartUserThread` 템플릿 안 `call` 한 곳(0x6afb4c) 뿐이다 (실행 구역 전체에서 `call`/`jmp`/`jcc`/`lea [rip+..]` 의 목적지와 8바이트 주소를 훑은 결과. 계산해서 만드는 주소는 못 잡는다).
- 명령 줄 (`KxStartUserThread.asm`):

  | 자리 | 주소 | 명령 | 뜻 |
  |---|---|---|---|
  | `+0x60` (= `KiStartUserThread`) | `1406afbd0` | `TEST byte [KeSmapEnabled],0xff` / `+0x67 JZ` / `+0x69 STAC` | SMAP 이 켜져 있으면 `STAC` |
  | `+0x6c`, `+0x71` | `1406afbdc`, `1406afbe1` | `MOV ECX,1` / `MOV CR8,RCX` | **IRQL 을 1 (APC 단계)로 올림** |
  | `+0x75` | `1406afbe5` | `MOV RDX,[RSP]` | 인자 2 = `StartContext` |
  | `+0x79` | `1406afbe9` | `MOV RCX,[RSP+0x8]` | 인자 1 = `StartRoutine` |
  | `+0x7e` | `1406afbee` | `MOV RAX,[RSP+0x10]` | **시작 루틴 주소를 여기서 꺼낸다** |
  | `+0x83` (ret `+0x88`) | `1406afbf3` | `CALL 0x1406b77f0` | `_guard_dispatch_icall` (CFG 검사 뒤 RAX 로 넘어감). `_calls.tsv`: `KxStartUserThread +0x83 +0x88 indirect:cfg:stack[-0x128]` |
  | `+0x88` (= `KiStartUserThread+0x28`) | `1406afbf8` | `LEA RCX,[RSP+0x100]` | 시작 루틴이 돌아오는 자리 (복원 시작) |

  - `stack[-0x128]` 는 "함수 입구 RSP 기준 -0x128" 이고, 지금 RSP(= 입구 - 0x138) 기준으론 `[RSP+0x10]` 이다. `+0x7e` 와 같은 칸이다.
- **시작 루틴 주소가 `[RSP+0x10]` 까지 오는 길 (전부 `[정적]`, 이번에 `[추측]` 에서 굳어짐):**
  1. `PspAllocateThread +0x681 LEA RAX,[0x140910de0]`, `+0x688 MOV [RSP+0x128],RAX` → 시작 정보 묶음(`rsp+0x120`) 의 `+0x8` 칸. 이 묶음 주소를 `+0x6e9` 에서 `KeInitThread` 둘째 인자로 (sast/06).
  2. `KeInitThread +0x2f6 MOV RDX,R15` / `+0x2fc CALL KiInitializeContextThread` (ret `+0x301`). 그러니 이 묶음이 `KiInitializeContextThread` 의 둘째 인자 (`+0x3c MOV R13,RDX`).
  3. `KiInitializeContextThread +0x1c7 MOV RAX,[R13+0x8]` → `+0x1cf MOV [RBX+0x10],RAX` → 새 커널 스택의 `_KEXCEPTION_FRAME.P3Home` (`+0x10`) 에 `PspUserThreadStartup` 주소가 들어간다. 같은 식으로 `+0x1b1 MOV RAX,[R13+0x18]`→`[RBX+0]` (StartContext), `+0x1bf MOV RAX,[R13+0x10]`→`[RBX+0x8]` (StartRoutine).
  4. `RBX` = `R15 - 0x2d0` (`+0x120`), 새 스레드 `KernelStack` 은 `R13 = RBX - 0x40` (`+0x1cb`) 이고 `+0x2ce MOV [RSI+0x58],R13` 로 저장된다. 스택 맨 위(`[R13+0x38]`)에 **`KiStartUserThread` 주소(`+0x1e1 LEA`, `+0x1e8 MOV [R13+0x38],RAX`)** 가 있다.
  5. `SwapContext` 가 새 스레드로 바꿀 때 `+0xf7 MOV RSP,[RSI+0x58]` 로 그 스택을 쓰고, 끝 조각의 `+0x2ba ADD RSP,0x30` → `+0x2be POP RBP` → **`+0x2bf RET`** (`frag:FUN_1406b686f`) 가 `[R13+0x38]` 을 꺼내 `KiStartUserThread` 로 뛴다. 이때 `RSP = RBX`, `RBP = [R13+0x30]` (= 트랩 프레임 + 0x80, `KiInitializeContextThread +0x274 LEA RAX,[R11+0x80]`, `+0x27b MOV [R13+0x30],RAX`).
  - 결과: `KiStartUserThread` 는 **`call` 로 불리는 게 아니라 `SwapContext` 의 `RET` 로 들어온다** (`0x6afbd0` 을 가리키는 코드 쪽 참조는 `KiInitializeContextThread +0x1e1` 하나뿐, 8바이트 주소로는 `CFGRO` 구역(rva 0x1201cd8) 한 곳 — 구역 이름으로 보아 CFG 쪽 표로 보인다 `[추측]`). 그래서 `PspUserThreadStartup` 은 지도(`NtCreateUserProcess`)에 안 닿는 것이다 (sast/05 와 맞음).
- 호출 모양: `PspUserThreadStartup(StartRoutine = 0, StartContext = ETHREAD.StartAddress)`. 시작 루틴 `PspUserThreadStartup` 은 이 두 인자를 읽지 않는다 (`+0x17 MOV RCX,CR8`, `+0x1b XOR EDX,EDX` 로 덮어씀). `StartRoutine` 은 `NtCreateUserProcess` 가 0 을 넘긴다 (sast/06), `StartContext` 는 `PspAllocateThread +0x6a0~+0x6a7` 이 `[ETHREAD+0x4e0]` 를 넣는다.
- `[정적]` CFG 검사 함수 `_guard_dispatch_icall` 은 앞부분 바이트(`0x6b77f0~`)만 읽었다: 비트맵 검사 뒤 RAX 로 넘어가는 모양이고 콜스택엔 안 남는다 (run4 `[관찰]` 과 맞음). 모든 갈래는 안 따라갔다.

### 질문 2. `KiStartUserThreadReturn` (0x6afb60)

- **무슨 명령인가:** 두 바이트 **`90 C3` = `NOP; RET`** (`0x1406afb60`, `0x1406afb61`). 뒤는 `INT3` 패딩 (`0x6afb62~0x6afb67`) 과 `NOP` 정렬 (`0x6afb68~0x6afb6f`).
  - Ghidra 는 이 두 바이트를 `FUN_1406afa82` (rva 0x6afa82, 224바이트) 의 끝으로 본다. PDB 는 이 자리를 **함수 `KyStartUserThread` (rva `0x6af810~0x6afb62`) 의 끝 두 바이트**로 본다 (`.pdata` 범위와 같음).
- **`KyStartUserThread` 는 무엇인가:** 트랩 프레임을 만드는 진입 코드 모양의 템플릿이다 (`+0x0` 부터 `sub rsp,8; push rbp; sub rsp,0x158; lea rbp,[rsp+0x80]`, `rax~r11` 저장, `swapgs`, MSR 쓰기 …) 이고 맨 끝(`0x6afb4c`)에서 `CALL KxStartUserThread` 한다.
  - `[정적]` 이 템플릿으로 들어오는 참조(`call`/`jmp`/`lea`/8바이트 주소)를 ntoskrnl 에서 못 찾았다 (`0x6af810`). 실제로 안 도는지는 동적으로.
  - 쓸모는 **풀어 보기 정보**다: `.pdata` 의 UNWIND_INFO 가 "기계 프레임 있음(`PUSH_MACHFRAME`) + 프레임 레지스터 `rbp` -0x80 + 0x158 확보 + `rbp` 저장" 모양이다.
- **콜스택에 이 이름이 보인 이유 `[정적]` (스택 칸) + `[추측]` (풀어 보기 규칙):**
  1. `KiInitializeContextThread +0x1d3 LEA RAX,[0x1406afb60]`, `+0x1da MOV [RBX+0x138],RAX` — `_KEXCEPTION_FRAME.Return` (`+0x138`) 칸에 이 주소를 넣는다. 이 칸은 `KxStartUserThread` 가 확보한 0x138 바이트 바로 위, 즉 `KxStartUserThread` 프레임의 **"돌아올 주소" 자리**다 (UNWIND_INFO `ALLOC_LARGE 0x138`, 저장 레지스터는 모두 그 안).
  2. 그러니 `KiStartUserThread+0x28` 프레임 위는 `KiStartUserThreadReturn` 으로 읽히고, 이 주소는 `KyStartUserThread` 범위 안이라 `KyStartUserThread` 풀어 보기 규칙(트랩 프레임: `rbp-0x80` 에서 0x158 올리고 `rbp` 복원, 기계 프레임에서 `RIP`/`RSP` 읽기)이 적용된다. 그 결과 다음 칸이 **트랩 프레임의 `Rip`/`Rsp` = 유저 쪽 자리**로 풀린다 (run4 `[관찰]` 의 `ntdll!RtlUserThreadStart`). `[추측]` 풀어 보기 규칙 적용은 관찰과 맞지만 규칙을 코드로 따라가진 않았다.
- **시작 루틴(`PspUserThreadStartup`)이 돌아올 주소가 아니다.** 시작 루틴의 돌아올 주소는 `+0x83` 의 `CALL` 이 쌓는 `KxStartUserThread +0x88` 이다 (= `KiStartUserThread+0x28`). `KiStartUserThreadReturn` 은 그 한 칸 위 (`KxStartUserThread` 프레임 자신의 돌아올 주소) 이다.
- **실제로 실행되는 코드가 아니다 `[정적]`.** `KxStartUserThread` 는 `RET` 로 끝나지 않고 `IRETQ` 로 끝난다 (질문 3). 이 자리로 뛰거나 부르는 코드도 없고, 참조는 `KiInitializeContextThread +0x1d3` `LEA` 하나뿐이다.

### 질문 3. 커널 → 유저 전환 (2.3 `[공백]`)

- **답: 시작 루틴이 돌아온 뒤 `KxStartUserThread +0x88` 부터 "복원 → 인터럽트 끄기 → 유저 APC 확인 → 레지스터 복원 → `IRETQ`" 로 유저에 들어간다. 유저의 첫 `RIP` 는 트랩 프레임의 `Rip` 칸 (`+0x168`) 에서 꺼낸다.** (`SYSRETQ` 가 아니다 — 이 길엔 `SYSRET` 가 없다.)
- 순서 (대표 지점. 자리 = `KxStartUserThread` 기준, 괄호는 `KiStartUserThread` 기준 = WinDbg 에 보일 모양 `[추측]`, `-0x60`):

  | # | 자리 | 하는 일 |
  |---|---|---|
  | 1 | `+0x88~+0xdc` (`+0x28~+0x7c`) | `_KEXCEPTION_FRAME` 에서 `xmm6~15`, `rbx`, `rdi`, `rsi`, `r12~r15` 를 읽어 복원 (`RCX = RSP+0x100`). 유저가 쓸 비휘발 레지스터 값이 여기서 정해진다. |
  | 2 | `+0xe0 CLI` (`+0x80`) | 인터럽트 끔. |
  | 3 | `+0xe1 TEST byte [RBP+0xf0],1`, `+0xe8 JZ → +0x3e3` | 트랩 프레임 `SegCs` (`+0x170`) 의 맨 아래 비트. **0 이면 커널로 돌아가는 가지** (`+0x3e3~+0x42c`, 끝 `IRETQ` @`0x1406aff9c`). 유저 스레드는 CS=`0x33` 이라 안 탄다 (`KiInitializeContextThread +0x163 LEA EAX,[RDX+0x33]`, `+0x16b +0x2b`, RDX=0 → CS `0x33`, SS `0x2b`). |
  | 4 | `+0xee~+0xf7` | `KeSmapEnabled` 이면 `STAC`. |
  | 5 | `+0xfa~+0x125` 되돌이 | 현재 스레드 `ApcState.UserApcPendingAll` (`[스레드+0xc2] & 3`) 이면: `CR8=1`, `STI`, **`KiInitiateUserApc`** (call `+0x116`, ret `+0x11b`), `CLI`, `CR8=0`, 처음(`+0xfa`)으로. |
  | 6 | `+0x127` (ret `+0x12c`) | `KiSynchronizeUserIsolationDomainEntry` 가 0 이 아니면 `+0xfa` 로 되돌아감 (`+0x132 JNZ`). |
  | 7 | `+0x13d~+0x229` | 조건부 마무리: `KiUpdateStibpPairing(0)` (call `+0x141`, ret `+0x146`), `KiRestoreSetContextState` (`+0x157`, ret `+0x15c`), `KiCopyCounters` (`+0x173`, ret `+0x178`), `LDMXCSR [RBP-0x54]` (`+0x181`, 트랩 프레임 `MxCsr`), `KiRestoreDebugRegisterState` (`+0x18f`, ret `+0x194`, `[RBP+0x80]` = `Dr7` 가 0 이 아닐 때), CET 그림자 스택 처리 (`+0x1a6 RDSSPD`, `+0x1c6 RSTORSSP`, `+0x1ca SAVEPREVSSP`), 추측 완화용 MSR 쓰기 (`+0x1fb WRMSR` ECX=0x48, `+0x216 WRMSR` ECX=0x49), 리턴 버퍼 채우기 덩어리 (`+0x229`, 위 "스크립트 문제"). 모두 `GS:[0x858]` 등 CPU 플래그로 갈리는 조건부 `[추측: 목적]`. |
  | 8 | 조각 `FUN_1406afeb5` (`+0x345~`) | 휘발 레지스터 복원: `xmm0~5` (`[RBP-0x10]~[RBP+0x40]`), `r11`, `r10`, `r9`, `r8`, `rdx`, `rcx`, `rax` (`[RBP-0x20]~[RBP-0x50]`). |
  | 9 | 조각 `+0x68 MOV RSP,RBP` / `+0x6b MOV RBP,[RBP+0xd8]` / `+0x72 ADD RSP,0xe8` | `RBP`(= 트랩 프레임+0x80) 를 유저 값(`Rbp +0x158`)으로 되돌리고, **`RSP = 트랩 프레임 + 0x168`** 로 옮김 → 이제 `[RSP]` 부터가 `Rip, SegCs, EFlags, Rsp, SegSs`. |
  | 10 | 조각 `+0x79 TEST byte [KiKvaShadow],1` | **켜짐 → `+0x82 JMP KiKernelExit`** (꼬리호출). **꺼짐 → `+0x87~+0x99`** (`VERW` 선택, `SWAPGS`) 후 `+0x9c IRETQ`. |

- **유저로 넘어가는 명령 = `IRETQ`, 두 자리 중 하나:**
  - (가) `KiKvaShadow` 비트0 = 0 일 때: **`KxStartUserThread` 조각 `FUN_1406afeb5 +0x9c`**, 주소 `0x1406aff51` = `KxStartUserThread +0x3e1` (= `KiStartUserThread+0x381`). 바로 앞에 `+0x99 SWAPGS`.
  - (나) `KiKvaShadow` 비트0 = 1 일 때: 조각 `+0x82` (`0x1406aff37` = `KxStartUserThread +0x3c7`) 가 **`KiKernelExit` (`0x140bcab80`) 로 `JMP`**. `KiKernelExit` 는 기계 프레임 5칸을 `GS:[0x38]+0x4200` 의 임시 스택으로 복사하고 (`+0x2e~+0x3a PUSH`), 조건부로 **`MOV CR3,RSP`** (`+0x83`, 유저 주소 공간으로) 한 뒤 `+0xa2 SWAPGS`, **`+0xa5 IRETQ`** (`0x140bcac25`).
  - 어느 쪽이 타는지는 `KiKvaShadow` 값(CPU 보안 완화 설정)에 달렸다 — 정적으로는 못 정한다 (동적).
- **유저 첫 `RIP` 를 어디서 꺼내나:**
  - `IRETQ` 는 `[RSP]` 에서 `RIP, CS, RFLAGS, RSP, SS` 를 차례로 꺼낸다. `RSP` 가 위 9번에서 트랩 프레임 + 0x168 이므로 **`RIP` = `_KTRAP_FRAME.Rip` (`+0x168`)**, `CS` = `SegCs` (`+0x170`), `RFLAGS` = `EFlags` (`+0x178`), `RSP` = `Rsp` (`+0x180`), `SS` = `SegSs` (`+0x188`). (나) 에서도 `KiKernelExit` 가 같은 5칸을 그대로 옮겨 쓴다.
  - 트랩 프레임 위치 = 시작 루틴이 도는 동안 `RBP - 0x80` 이다 (`RBP` 는 모든 호출이 보존, `SwapContext` 가 `[R13+0x30]` 에서 읽음). `KiInitializeContextThread` 가 `KTHREAD.InitialStack` 을 `R15` 로 바꿔 쓰고 (`+0x2ca MOV [RSI+0x28],R15`) 트랩 프레임을 `R15 - 0x190` 에 둔다 (`+0x1b5 LEA R11,[R15+-0x190]`) → **트랩 프레임 = `KTHREAD.InitialStack - 0x190`** `[정적]`.
- **그 칸을 누가 채우나 (이름만):**
  - 처음 채우는 쪽: `KiInitializeContextThread +0x1ac CALL KxContextToKframes` (ret `+0x1b1`) — 유저 `CONTEXT`(복사본, CS/SS 를 `0x33`/`0x2b` 로 고친 것) → 트랩 프레임·예외 프레임. `[추측]` 이름·인자 모양으로만 판단, 본문은 안 뽑음.
  - 그 뒤 첫 유저 위치를 `ntdll!LdrInitializeThunk` 로 정하는 쪽: **`PspUserThreadStartup +0xae` 의 `PspInitializeThunkContext`** (Q7).
  - `[추측]` run4 콜스택의 유저 칸 `RtlUserThreadStart` 는 이 `Rip` 칸 값으로 풀린 것이고, `PspInitializeThunkContext` 가 이 칸을 `LdrInitializeThunk` 로 바꾼 뒤엔 `LdrInitializeThunk` 가 "부른 곳 없이" 보인다 (run4 `[관찰]`). 연결은 Q7 에서.

### 질문 4. `PspUserThreadStartup` 흐름 (rva `0x910de0`, 232바이트)

실제 순서 (점프 따라, 정상 길 = 일반 유저 스레드). 자리 = ret_off, 괄호 = call_off.

| # | 자리 | 하는 일 | 조건 |
|---|---|---|---|
| 1 | `+0x0~+0xd` | 저장 (`rbx`, `rsi`, `rdi`, `r14`), 스택 0x40 | |
| 2 | `+0xf~+0x22` (call `+0x1d`, ret `+0x22`) | `KiLowerIrqlProcessIrqlFlags(현재 IRQL, 0)` | `[KiIrqlFlags] != 0` 일 때만 |
| 3 | **`+0x22 MOV CR8,RDI`** (`RDI=0`) | **IRQL 을 0 으로 내림** | 항상 |
| 4 | `+0x26 MOV RBX,GS:[0x188]` | 현재 스레드 | |
| 5 | `+0x37` (ret `+0x3c`) | `PspDisablePrimaryTokenExchange(스레드)` | 항상 |
| 6 | `+0x4b TEST AL,2` → `+0xe5` (call `+0xf0`, ret `+0xf5`) | `PspTerminateThreadByPointer(스레드, 0xC000004B, TRUE)` 하고 `+0x53` 으로 | `ThreadInserted`(bit1) 가 **0** 일 때만. 정상 길은 건너뜀 |
| 7 | `+0x53~+0x6a` | `RSI = 프로세스+0x5fc` (`Flags3`). bit0 `Minimal` 이 1 이면 `+0xc2` 로 | |
| 8 | (`Minimal`=1) `+0xc2` (call `+0xc5`, ret `+0xca`) → `+0xa1` | `DbgkCreateMinimalThread(스레드)` (아래 9~10 건너뜀) | `Minimal` 일 때만 |
| 9 | `+0x6c~+0x7e` (call `+0x73`, ret `+0x78`) | `[TEB+0x108] = PsGetSessionLocaleId()` (`TEB.CurrentLocale`) | `Minimal`=0 |
| 10 | `+0x91` (call `+0x94`, ret `+0x99`) / `+0x9c` (call `+0x9c`, ret `+0xa1`) | `KeUpdateTebSchedulingPropertiesCurrentThread(스레드)`, `PspNotifyThreadCreation(스레드)` | `Minimal`=0 |
| 11 | `+0xa1 TEST [R14],1` → `+0xcc` (call `+0xde`, ret `+0xe3`) → `+0xb3` | `KeWaitForSingleObject(스레드, 6 UserRequest, UserMode(1), 알림 불가(0), 시간제한 없음)` | `Terminated`(bit0) 가 **1** 일 때만 |
| 12 | `+0xa8~+0xac` (프로세스 `Minimal` 확인), `+0xae` (ret `+0xb3`) | `PspInitializeThunkContext()` | `Terminated`=0 **그리고** `Minimal`=0 |
| 13 | `+0xb3~+0xc0` | 복원, `RET` → `KiStartUserThread+0x28` | |

- 이름: `+0xde` 의 대상은 장부에 `alias:KeWaitForMutexObject+1` 로 찍혀 있다. PDB 이름은 **`KeWaitForSingleObject`** 하나 (`ALIASES(1)`, rva 0x217eb0) — run13 과 같다.
- 정상 길(test.exe 스레드: `ThreadInserted`=1, `Terminated`=0, `Minimal`=0) 호출 순서: `PspDisablePrimaryTokenExchange` → `PsGetSessionLocaleId` → `KeUpdateTebSchedulingPropertiesCurrentThread` → `PspNotifyThreadCreation` → `PspInitializeThunkContext` (5개, 여기에 2번이 `KiIrqlFlags` 가 켜졌을 때만 앞에 붙는다).
- `+0x73~+0x80` 은 SEH `try` 범위, 처리기는 `+0x80` 이다 (`.pdata` 의 `__C_specific_handler` 범위 표: 시작 `0x910e53`, 끝 `0x910e60`, 처리기 상수 1, 점프 목표 `0x910e60`). TEB(유저 메모리) 쓰기가 실패해도 `+0x80` 에서 레지스터만 되살리고 `+0x91` 로 이어진다 (`.asm` 에는 빠진 곳, 원본 바이트로 읽음).

**4-1. `+0x22` IRQL 내리기 직후 멈춤 APC 가 배달될 수 있다는 run13 `[추측]` 을 코드로 더 굳힐 수 있나 — 고리 3개를 더 굳혔다, 마지막 한 고리만 `[추측]` 로 남음.**

- 고리 1 `[정적]` (새로 확인): **`KiStartUserThread` 는 IRQL 1 로 시작 루틴을 부른다** — `KxStartUserThread +0x6c MOV ECX,1`, `+0x71 MOV CR8,RCX`. `SwapContext` 는 보통 DISPATCH 단계(2)에서 돌고 `[추측]`, 새 스레드가 이 자리에서 1 로 내려온다. 시작 루틴이 도는 동안 처음엔 APC 단계(1)라서 커널 APC 는 막혀 있다고 볼 수 있다 `[추측: IRQL 규칙]`.
- 고리 2 `[정적]` (새로 확인): `+0x22` 앞의 `KiLowerIrqlProcessIrqlFlags` (`+0x1d` 호출)는 **APC 를 배달하지 않는다.** 이 함수는 `KiIrqlFlags` bit0 이 켜져 있고 옛 IRQL ≥ 2 일 때만 CPU 별 장부를 지우고 `KiRemoveSystemWorkPriorityKick` 을 부른다 (`KiLowerIrqlProcessIrqlFlags.asm` `+0x1d CMP CL,2 / JC → 끝`). 옛 IRQL 이 1 이라 `+0x1d` 호출은 **아무것도 안 하고 돌아온다.** 그러니 `+0x22 MOV CR8,RDI` 가 IRQL 을 1 → 0 으로 내리는 **이 구간의 유일한 자리**다.
- 고리 3 `[정적]` (새로 확인): `SwapContext` 가 새 스레드로 바꾸는 끝 조각(`frag:FUN_1406b686f +0x292~+0x2af`)이 **"새 스레드 `ApcState.KernelApcPending`(`[스레드+0xc1]`) == 1 이고 (스위치 프레임 `+0x28` 바이트 OR `[스레드+0x1e6]`) 가 0 이 아니면 `HalRequestSoftwareInterrupt(1)` (APC 단계 소프트웨어 인터럽트 요청)"** 를 한다. 새 유저 스레드의 스위치 프레임 `+0x28` 바이트는 `KiInitializeContextThread +0x26f MOV byte [R13+0x28],1` 로 **항상 1** 이다. 그러니 부모가 멈춤 APC 를 걸어 둔 새 스레드(run13: `KernelApcPending` 1)는 처음 CPU 에 오를 때 APC 인터럽트를 미리 걸고 시작한다.
- 남은 `[추측]` (동적 몫): 걸린 APC 인터럽트가 **`+0x22` 의 `MOV CR8` 직후, 곧 `+0x26` 에서** 실제로 들어오는가. (CR8 을 내리면 걸려 있던 인터럽트가 다음 명령 앞에서 들어온다고 보는 CPU 규칙, 그리고 이어서 `KiDeliverApc +0x220 → KiSchedulerApc +0xf7` 로 간다는 run13 연결.)
- 반대 증거는 없다: `KxStartUserThread` 의 유저 복귀 길은 **유저 APC 만** 확인한다 (`[스레드+0xc2]`, 5번). 커널 APC 는 보지 않는다. 그러니 멈춤 APC(커널 모드 APC) 가 이 복귀 길에서 처리될 일은 없고, `PspUserThreadStartup` 안 IRQL 이 0 이 되는 순간이 그 첫 기회다.
- 따라서 run13 의 `[추측]` 은 **"고리 1·2·3 = `[정적]`, 마지막 = `[추측]`"** 로 바꿔 쓸 수 있다. 같은 이유로 `+0x26` 이후 어느 호출 사이에서 잠드는지는 이 코드만으론 정할 수 없다 (다른 호출 `+0x37` 이 락을 풀 때 걸릴 수도 있음 `[추측]`).

**4-2. `+0xb3 PspInitializeThunkContext` — 첫 스레드일 때만인가?**

- **아니오. 이 함수 안에는 "첫 스레드인지" 가르는 조건이 없다.** 조건은 두 개: 스레드 `CrossThreadFlags` bit0 (`Terminated`) 가 0 이고 (`+0xa1~+0xa6`), 프로세스 `Flags3` bit0 (`Minimal`) 가 0 일 것 (`+0xa8~+0xac`). 둘 다 만족하는 일반 유저 스레드는 **모두** 부른다.
- 인자: `+0xae` 앞에서 인자 레지스터를 새로 채우지 않는다. `.c` 에서도 `PspInitializeThunkContext(void)` 다 (앞 호출 `+0x9c` 때 `RCX = 스레드` 였지만 호출 규약상 믿을 수 없다). 본문이 현재 스레드를 직접 구한다고 보는 게 자연스럽다 `[추측]`. 본문은 Q7.
- run4 `[관찰]` (스레드 4개 모두 `LdrInitializeThunk` 에서 출발) 과 모양이 같다. "첫 스레드만 다르게" 가 있다면 `PspInitializeThunkContext` 안에 있을 것이다 (Q7 에서 확인).

**4-3. `+0xca DbgkCreateMinimalThread`, `+0xe3` 기다림, `+0xf5` 종료의 조건 — 한 줄씩**

- `+0xca` `DbgkCreateMinimalThread` ← **프로세스가 Minimal 일 때** (`EPROCESS.Flags3.Minimal`, `+0x5fc` bit0). 이때는 로케일 쓰기·TEB 스케줄링 속성·`PspNotifyThreadCreation`·`PspInitializeThunkContext` 를 모두 건너뛴다. test.exe 같은 보통 프로세스는 안 탄다 `[정적: 조건]`, 실제 값은 동적.
- `+0xe3` `KeWaitForSingleObject` ← **스레드가 이미 끝내라는 표시(`Terminated`)를 받은 채 시작 루틴까지 왔을 때.** 자기 스레드 개체를 사용자 모드로, 시간제한 없이 기다린다 (이유 6 `UserRequest`). `PspInitializeThunkContext` 는 건너뛴다.
- `+0xf5` `PspTerminateThreadByPointer(…, 0xC000004B, TRUE)` ← **`ThreadInserted`(bit1) 가 0 일 때.** 스레드가 프로세스에 끝까지 못 들어갔는데(만들기 실패 길) 시작 루틴까지 온 경우다. 자기를 끝낸다. 그 뒤 코드(`+0xf5 JMP +0x53`)가 있지만 자기 자신이면 돌아오지 않는 호출로 보인다 `[추측]`.
- **test.exe 첫 스레드가 타는 길 한 줄 `[정적: 조건 읽음, 값은 동적]`:** `+0x22` IRQL 0 → `+0x37` 토큰 교환 막기 → 6번 건너뜀 → `+0x73` 로케일 → `+0x94` TEB 스케줄링 속성 → `+0x9c` 스레드 만들기 알림 → 11번 건너뜀 → `+0xae` 썽크 문맥 → `+0xc0` `RET`.

### 질문 5. 표기 규칙

- tail/frag: `KxStartUserThread` 조각 `FUN_1406afeb5` 안 `+0x82` 의 `TAILJMP -> KiKernelExit` 가 `_calls.tsv` 에 `frag:FUN_1406afeb5+0x82  tail  KiKernelExit` 로 있다 (이 경우는 다른 함수로 가는 꼬리호출이다).
- indirect-cfg: `KxStartUserThread +0x83` = `indirect:cfg:stack[-0x128]` (입구 RSP 기준). 실제 대상은 `[RSP+0x10]` 에 쌓인 값 = `PspUserThreadStartup` (정적 사슬은 질문 1).
- alias: `PspUserThreadStartup +0xde` = `alias:KeWaitForMutexObject+1` → 문서엔 `KeWaitForSingleObject`.
- 떨어진 범위: Ghidra 는 `KxStartUserThread` 를 633바이트 + 조각으로 잡았지만 `.pdata` 는 `0x6afb70~0x6aff9e` (1070바이트) 한 덩어리다. 조각 오프셋을 `KxStartUserThread +0x…` 로 환산한 값은 위 표대로다.
- `.c` 는 쓰지 않고 `.asm` 과 원본 바이트로 흐름을 읽었다.

## 범위를 넓힌 이유

- `KiKernelExit` (167바이트): `KxStartUserThread` 조각 `+0x82` 가 `JMP` 하는 유저 복귀의 다른 끝(`IRETQ`)이라 읽어야 질문 3 이 닫힌다. 요청서가 허용한 "복원·출구 계열" 에 해당. 지도엔 없는 함수.
- `KiLowerIrqlProcessIrqlFlags` (91바이트): 질문 4 의 `+0x22` 앞 호출이 IRQL 내리기·APC 배달과 무슨 관계인지 확인하려고. 부르는 곳이 매우 많은 공용 함수라 이 함수의 호출자 목록은 보지 않는다.
- 안 뽑은 것: `KxContextToKframes` (유저 `CONTEXT` 를 트랩 프레임으로 옮기는 쪽 `[추측]`), `PspInitializeThunkContext` (Q7), `KiInitiateUserApc`.

## 동적분석으로 확인해야 할 것

정적으로 못 정한 것:

1. **`KiKvaShadow` 값** — 유저로 가는 `IRETQ` 가 (가) `KxStartUserThread` 조각인지 (나) `KiKernelExit` 인지.
2. **`KiIrqlFlags` 값** — `+0x1d` 호출이 실제로 들어가는지 (들어가도 IRQL 1 이라 효과는 없음).
3. **IRQL 1 → 0 에서 멈춤 APC 가 정말 `+0x26` 에서 들어오는가**, 아니면 다른 자리에서 잠드는가 (run13 마지막 고리).
4. **복귀 전 트랩 프레임 `Rip` 값이 `PspInitializeThunkContext` 앞뒤로 어떻게 바뀌는가** (Q7 과 이어짐). 시작 루틴이 도는 동안 `RBP - 0x80` = `KTHREAD.InitialStack - 0x190` 인지.
5. **유저 APC 되돌이(`+0xfa`) 에서 `KiInitiateUserApc` 가 첫 스레드에 실제로 불리는가** (정적으론 조건부, run4 의 "부른 곳 없는 `LdrInitializeThunk`" 와 연결).
6. `KyStartUserThread` 템플릿이 정말 안 도는가 (정적으론 참조 없음).
7. `PspUserThreadStartup` 분기의 실제 값: `ThreadInserted`, `Terminated`, `Minimal` (test.exe 스레드는 1, 0, 0 이어야 정상).

추천 멈춤점 (모두 test.exe 일 때만 서게 조건. 조건식은 dast 가 정한다):

- `nt!PspUserThreadStartup` 입구: `dq @rsp L1` 이 `nt!KiStartUserThread+0x28` (= `KxStartUserThread +0x88`, `0x1406afbf8`) 인지, `dd nt!KiIrqlFlags L1`, `dd nt!KiKvaShadow L1`. 트랩 프레임 `dt nt!_KTRAP_FRAME (poi(@$thread+0x28)-0x190)` 의 `Rip` 칸.
- `nt!PspUserThreadStartup+0x22` 와 `+0x26` (IRQL 내림, APC 가 어디서 들어오나). 같이 `nt!KiSchedulerApc` 입구에서 `k` 를 보고 맨 아래가 `PspUserThreadStartup+0x26` 근처인지 (run13 과 합쳐서).
- `nt!PspUserThreadStartup+0xae` (`PspInitializeThunkContext` 앞) 와 `+0xb3` (뒤): 트랩 프레임 `Rip`/`Rsp` 칸이 어떻게 달라지나. 값 읽기 `dq (poi(@$thread+0x28)-0x190)+0x168 L5` (= `Rip, CS, EFlags, Rsp, SS`).
- `nt!KiStartUserThread+0x28` (= `KxStartUserThread +0x88`): 시작 루틴이 돌아온 직후. `dq @rbp+0xe8 L5` 로 `Rip, CS, EFlags, Rsp, SS` 를 읽으면 **유저로 들어가기 직전 값**이다.
- 선택: `nt!KiInitiateUserApc` (유저 APC 되돌이 `+0x116`). 첫 스레드에서 불리는가.
- **`IRETQ` 줄 자체(`KxStartUserThread +0x3e1`, `KiKernelExit +0xa5`) 에는 멈춤점을 걸지 않는 편이 좋다 `[추측]`.** 바로 앞 `SWAPGS` 이후라 GS 가 유저 값이고, `KiKernelExit` 는 CR3 도 유저 쪽으로 바꾼 뒤다. 유저 진입 값은 위 `+0x88` 에서 읽는 걸로 충분하다.
- 이름 주의: WinDbg 는 이 함수 안 주소를 가장 가까운 앞 이름 `KiStartUserThread+0x…` 로 보여 줄 것이다 `[추측]` (run4 에서 `+0x88` 이 `KiStartUserThread+0x28` 로 보인 것과 같은 규칙). 멈춤점은 `nt!KxStartUserThread+0x88` 처럼 어느 이름 + 오프셋으로 써도 같은 주소다.

dast 요청 필요: 위 목록 중 1·3·4 는 run 문서의 `[공백]` 을 닫으려면 필요하다 (2.3 커널 → 유저 전환). Q7(`PspInitializeThunkContext`)과 같이 요청서를 쓰면 한 번에 볼 수 있다.

## 판정

**분석성공.** 질문 1~5 에 모두 답이 있고 근거 줄(`.asm` `+0x…`, 원본 바이트 rva, PDB 자료형)도 있다. 핵심은 네 가지다.

1. `KiStartUserThread+0x28` = `KxStartUserThread +0x88`, 바로 앞 `+0x83` 이 `[RSP+0x10]`(= `PspUserThreadStartup`)를 CFG 검사 함수로 부르는 간접 호출이다. 이 칸의 출처 사슬(`PspAllocateThread +0x681` → `KeInitThread` → `KiInitializeContextThread +0x1c7` → 스택)과 `SwapContext` 가 `RET` 로 `KiStartUserThread` 에 들어오는 길까지 `[정적]`.
2. `KiStartUserThreadReturn` = `NOP; RET` 두 바이트, 실행되는 코드가 아니라 `_KEXCEPTION_FRAME.Return` 칸(`+0x138`)에 쌓인 "가짜 돌아올 주소"이고, 풀어 보기용 `KyStartUserThread` 범위의 끝이다.
3. 유저로 넘어가는 명령은 `IRETQ` (가: `KxStartUserThread +0x3e1` / 나: `KiKernelExit +0xa5`, `KiKvaShadow` 로 갈림), 첫 `RIP` 는 트랩 프레임 `Rip` 칸 (`+0x168`) = `KTHREAD.InitialStack - 0x190` + 0x168. 그 칸을 `KxContextToKframes` 가 처음 채우고 `PspInitializeThunkContext` (Q7) 가 다시 정한다.
4. `PspUserThreadStartup` 안에 "첫 스레드" 조건은 없다. 조건은 `ThreadInserted`·`Terminated`·`Minimal` 셋이고, run13 의 "IRQL 내림 직후 멈춤 APC" 는 고리 3개를 `[정적]` 으로 굳혔고 마지막 한 고리(인터럽트가 `+0x26` 에서 들어옴)만 동적으로 남는다.

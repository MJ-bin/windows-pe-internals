# 24 continue-start — 정적 분석 결과

- 요청서: `_workspace/sast/24_continue-start.md` (대기열 Q17, 02_sprint 예정 run29)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`) + ntdll (`.../ntdll.dll`). 커널 PDB guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) = 기준과 **같음**. 두 모듈 다 이미 analyze 됨 (`.gpr` + `_index.tsv` + `SetPdb:` 줄 모두 있음) — 다시 안 돌림. 실행 중인 Ghidra 없음.
- 뽑은 함수:
  - ntoskrnl `^(NtContinue|NtContinueEx|KiContinueEx|KiContinuePreviousModeUser)$` depth 1 → 31개 (네 함수 + callee 27). 지도(`map`) 는 안 돌림 (callee 가 depth 1 로 다 잡혀서 필요 없었음).
  - ntdll `^(RtlUserThreadStart|NtContinue)$` depth 1 → 6개 (`RtlLeaveCriticalSection`·`RtlExitUserThread`·`NtTerminateProcess`·CFG 중개 `_guard_dispatch_icall$thunk$…` 포함. ntdll `NtContinue` 는 depth 0 대신 1 이지만 callee 없음).
  - 출력: `ms/decomp/{ntoskrnl,ntdll}/`, `ms/disassemble/{ntoskrnl,ntdll}/` (`NtContinue.asm`, `NtContinueEx.asm`, `KiContinueEx.asm`, `KiContinuePreviousModeUser.asm`, `RtlUserThreadStart.asm` …), 각 `_calls.tsv`.
- **callee 본문은 거의 안 읽었다.** 읽은 것과 이유:
  - `KeTestAlertThread` 전체 (질문 3: 이게 정말 유저 APC 를 "걸리게" 하는지),
  - `KiVerifyContextIpForUserCet` 앞쪽 (질문 2 CET: 그림자 스택 값을 맞춰 보는 줄이 있는지), `KePopulateContinuationContext` (run16 에서 이미 뽑힘),
  - `KxContextToKframes`·`KeContextToKframes` (이미 뽑힘): 칸 이동은 run16 표 그대로, 여기선 **`ContextFlags` 로 거르는 줄**(`+0x21`, `+0xc9`)만 다시 확인,
  - `PspInitializeThunkContext`·`PspCreateUserContext` (이미 뽑힘): 맡긴 문맥의 `ContextFlags` 값 확인용 몇 줄.
  - 시스템 호출 출구(`KiSystemCall64`·`KiSystemServiceExit`·`KiKernelExit`)는 **본문 안 읽음**. 이름·위치만 `pdb_aliases.py --where` 로: `KiKernelExit` rva `0xbcab80` (함수), `KiSystemCall64` rva `0x6c1840` (함수), `KiSystemServiceExit` rva `0x6c1d60` (Ghidra 에선 `KiSystemServiceStart +0x1a0` 안).
- 구조체 칸: `llvm-pdbutil-14 dump -types` 로 `_KTRAP_FRAME`·`_KEXCEPTION_FRAME`·`_KTHREAD`·`_KAPC_STATE`·`_KPROCESS`·`_EPROCESS` 비트칸 확인. **`_KCONTINUE_ARGUMENT` 구조체는 공개 PDB 에 없다** (열거형 `_KCONTINUE_TYPE` 만: UNWIND 0, RESUME 1, LONGJUMP 2, SET 3, LAST/INVALID 4). 아래 그 구조체 칸은 `.asm` 로 읽은 오프셋+역할.
- unwind 범위 표 (`.pdata`/`UNWIND_INFO`): 임시 파이썬(`pefile`)으로 직접 읽음 (레포 스크립트 아님). 결과는 질문 2·5 에.
- 버전 확인 (알려진 사실 대조): `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` call `+0x802` / ret `+0x807` → `PspCallProcessNotifyRoutines`, ntdll `LdrInitializeThunk` ret `+0xe` → `LdrpInitialize` 모두 `_calls.tsv` 와 **일치**. 이번에 새로 확인된 `LdrInitializeThunk` ret `+0x18` (NtContinue) · `+0x1f` (RtlRaiseStatus) 도 `.asm` 과 같음. 점프 목적지는 전부 `절대 주소 − 함수 시작` 으로 환산하는 스크립트로 `.asm` 의 `+0x..` 줄과 다시 맞춰 봤다 (NtContinueEx·KiContinueEx·RtlUserThreadStart 전 분기).

## 스크립트 문제

막힌 것 없음. 사소한 것 넷 (읽는 쪽이 헷갈릴 수 있는 것):

1. **`NtContinueEx.asm` 의 "조각 `FUN_1406ae17a`" 는 멀리 떨어진 창고가 아니다.** `+0x26e` 의 `FAKECALL` 로 시작하는 RSB 채우기 사다리(`+0x273~+0x389`, 원본 바이트로 보면 `CALL` 31개가 뒤로 거슬러 가며 `ADD RSP,8` 로 바로 버림) 의 **마지막 `CALL` 이 닿는 곳**이고, 주소도 사다리 바로 뒤에 붙어 있는 **같은 함수 몸통**이다. `.asm` 머리는 "점프로 옴" 이라고 쓰는데, 실제로는 사다리를 지나 이어지는 흐름이라 헷갈린다. 오프셋은 `(= NtContinueEx +0x38a …)` 로 환산돼 있어서 쓰는 데엔 문제 없다. `.pdata` 상 `NtContinueEx` 범위가 `0x6addf0~0x6ae26b` 라 WinDbg 도 `nt!NtContinueEx+0x426` 로 보일 가능성이 높다 `[추측]`.
   - 제안: 조각 시작이 본래 함수의 `.pdata` 범위 안이면 "같은 범위 몸통" 으로 표시. (급하지 않음)
2. `_calls.tsv` 의 조각 안 호출은 조각 기준(`frag:FUN_1406ae17a+0x82 tail KiKernelExit`)으로만 찍힌다. 본래 함수 기준(`NtContinueEx +0x40c`)은 `.asm` 줄 끝 주석에서만 본다. 제안: `_calls.tsv` 에 환산 열 추가.
3. **Ghidra 한계 (스크립트 아님)**: `KePopulateContinuationContext +0x30` 을 `WRUSSD [RAX],R8D` (4바이트) 로 읽었지만 원본 바이트는 `66 4c 0f 38 f5 00` — `REX.W` 가 있으니 **`WRUSSQ [RAX],R8` (8바이트)** 다. 질문 2 CET 답에 영향 없음 (한 칸 = Rip 한 개).
4. `RtlUserThreadStart.c` 는 `NtTerminateProcess` 를 `RtlExitUserThread` 뒤 정상 흐름처럼 그렸지만 실제론 **`__except` 착지점 `+0x4a`** 다 (`.asm` 과 unwind 표). `NtContinueEx.c` 는 `unaff_RBP`·`FUN_` 사다리로 많이 틀어졌다 (reference.md N3 와 같은 종류). 둘 다 `.asm` 만 믿을 것.

## 질문별 답 [정적]

근거 표기: `.asm` = `ms/disassemble/ntoskrnl/<함수>.asm` (ntdll 은 `…/ntdll/…`). 오프셋은 함수 시작 기준. 라벨은 ret (call 은 괄호). 분기 오프셋은 `.asm` 과 다시 맞춰 봤다.

### 1. 커널 입구: `nt!NtContinue` → `NtContinueEx`

1. **`NtContinue`(5바이트)는 `NtContinueEx` 로 그대로 넘기나: 예 (한 줄만 바꿔서).**
   - `NtContinue.asm`: `+0x0 MOVZX EDX,DL` → `+0x3 JMP NtContinueEx` (꼬리호출, `_calls.tsv`: `NtContinue +0x3 tail NtContinueEx`). `RCX`(CONTEXT)는 그대로. `RDX` 는 `DL` 만 남기고 위쪽을 0 으로 지운다 (유저가 `DL=1` 만 정했고 위쪽 비트는 쓰레기일 수 있어서 `[추측: 이유]`).
   - `.pdata`: `NtContinue` 는 `0x6adde0~0x6adde5` 의 잎 함수 (프롤로그 없음, 예외 처리기 없음). 그래서 `NtContinueEx` 는 시스템 호출 처리기가 남겨 둔 상태 그대로 이어서 돈다.
2. **둘째 인자 (유저가 준 `TRUE`) 가 `NtContinueEx` 에서 어떤 모양이 되나: "작은 정수(0 또는 1) 그대로, 다만 이 자리는 두 가지 뜻이 겹쳐 있다."**
   - `KiContinueEx +0x71 CMP RDI,0xff` / `+0x78 JA +0x123` (`RDI` = 둘째 인자): **`0xff` 이하면 BOOLEAN (`TestAlert`)**, 더 크면 **`_KCONTINUE_ARGUMENT` 를 가리키는 유저 포인터**. 이번 길은 `NtContinue` 가 `0/1` 로 넘기니 앞쪽 (`+0x7e MOVZX ECX,DIL` → TestAlert).
   - 포인터 모양일 때 구조체 (오프셋+역할, `KiContinueEx +0x123~+0x156`, `KiContinuePreviousModeUser +0x76~+0xb2`; 크기 `0x18`, 8바이트 정렬 필수):

     | 칸 | 뜻 | 줄 |
     |---|---|---|
     | `+0x0` (4) | `ContinueType` (`_KCONTINUE_TYPE`, **4 미만**이어야 함) | KPMU `+0xa0 CMP [RBP+0x18],4` |
     | `+0x4` (4) | 플래그: bit0 = TestAlert, bit1 = "APC 먼저 배달" `[추측: 이름]`. **bit2 이상이면 거절** | KiContinueEx `+0x141`~`+0x152`, KPMU `+0x8e~+0x97` |
     | `+0x8`, `+0x10` (8, 8) | 예약. **0 이어야 함** | KPMU `+0x99`, `+0xa6` |

   - 플래그 bit1 은 TestAlert(bit0) 와 **같이 있을 때만** 읽힌다 (`KiContinueEx +0x170 JMP +0xdf` 가 `+0xce` 검사를 건너뜀). 이번 길(BOOLEAN)엔 해당 없음.
3. **이전 모드가 유저일 때 어디로 가나: 둘 다, 순서대로 — `NtContinueEx` → `KiContinueEx` (항상) → 유저면 `KiContinuePreviousModeUser`.**
   - `NtContinueEx +0x81` (ret **`+0x86`**) `KiContinueEx(RCX=CONTEXT, RDX=둘째 인자, R8=예외 프레임(=자기 RSP), R9=트랩 프레임(=RBP−0x80))`. `NtContinueEx` 자신에는 이전 모드 갈림이 없다.
   - **갈림 줄**: `KiContinueEx +0x55 MOV R14,GS:[0x188]` (현재 스레드) → `+0x5e CMP byte [R14+0x232],0` (= `_KTHREAD.PreviousMode`) → `+0x66 JZ +0x1c5`.
     - **0 (커널 모드)**: `+0x1c5` → `+0x1e0` (ret **`+0x1e5`**) `KeContextToKframes(트랩, 예외, R15=받은 CONTEXT 포인터 그대로, 그 안의 ContextFlags, 0)`. 복사·검사 없음 (커널 메모리라고 믿는 길). (`+0x1ec` ret `+0x1f1` `KeTestAlertThread(0)` 은 TestAlert 일 때만.)
     - **0 아님 (유저)**: `+0xeb` (ret **`+0xf0`**) `KiContinuePreviousModeUser(RCX=CONTEXT, RDX=예외 프레임, R8=트랩 프레임, R9=둘째 인자)`.
   - 이 둘 말고 이 함수들을 부르는 곳은 없다 (실행 영역 전체에서 `call/jmp rel32` 찾기: `KiContinueEx` ← `NtContinueEx +0x81` 한 곳, `KiContinuePreviousModeUser` ← `KiContinueEx +0xeb` 한 곳. `NtContinueEx` 는 그 밖에 시스템 호출 표로도 닿음).

### 2. 문맥 → 트랩 프레임

1. **어느 함수, 어느 줄에서 옮기나: 유저 길은 `KiContinuePreviousModeUser +0x422` (ret `+0x427`).** run16 의 두 후보 중 `KiContinueEx +0x1e0` 은 **커널 모드 쪽**이다 (위 갈림). 유저가 부르면 `+0x422` 만 탄다.
   - 인자 (`KiContinuePreviousModeUser +0x40f~+0x41e`): `RCX` = 트랩 프레임 (`[RBP+0x40]`), `RDX` = 예외 프레임 (`[RBP+0x38]`), `R8` = **커널에 복사해 둔 CONTEXT** (`RBX`), `R9D` = `ContextFlags`(`EDI`), 5번째(스택) = `1` (유저 모드).
   - `KeContextToKframes +0x78` (ret `+0x7d`) → `KxContextToKframes` (run16 표 그대로, 줄 `+0x66/+0x6d` 등). 거르는 줄: **`(플래그 & 0x100001) == 0x100001`** 이면 `Rip(+0xf8→트랩 +0x168)`·`Rsp(+0x98→+0x180)` (`KxContextToKframes +0x21/+0x35`), **`(플래그 & 0x100002) == 0x100002`** 이면 `Rax·Rcx(+0x80→+0x38)·Rdx(+0x88→+0x40)·R8~R11…` (`+0xc9/+0xce`). 즉 **`Rip`·`Rsp` 는 CONTEXT_CONTROL 비트, `Rcx`·`Rdx` 는 CONTEXT_INTEGER 비트가 켜져 있을 때 트랩 프레임으로 간다** `[정적]`.
   - 맡긴 문맥의 `ContextFlags` 는 `PspInitializeThunkContext +0x8d ADD R12D,0x10001b` (CET 스레드면 `+0x40` 더함, `+0x6c~+0x76`) 로 정한 값을 `RtlInitializeExtendedContext2` (`+0xf0`) 에 준 것으로 보인다 `[추측: 그 함수가 값을 그대로 쓴다고 가정]` → `0x10001b` = 아키텍처 비트 + CONTROL + INTEGER + FLOATING_POINT + DEBUG_REGISTERS. 그러면 `Rip`·`Rsp`·`Rcx`·`Rdx` 모두 옮겨진다. 실제 값은 동적.
   - 덤 (`KxContextToKframes +0x8c~+0xc0`): 유저 모드 호출이면 `SegSs = 0x2b` 고정, `SegCs` 는 CONTEXT 가 `0x33` 이면 `0x33` 아니면 `0x23`, `CS=0x33` 이면 **`Rip` 를 48비트 부호 확장** (`SHL 0x10` / `SAR 0x10`, 비정규 주소로 `IRETQ` 가 죽지 않게 `[추측: 목적]`).
   - 옮긴 뒤 **`NtContinueEx` 가 트랩 프레임에서 `RAX·RCX·RDX·R8~R11·XMM0~5` 를 다시 읽어 간다** (질문 4). 예외 프레임(`RBX·RDI·RSI·R12~R15·XMM6~15`)은 `NtContinueEx +0x7a` 에서 자기 스택(`RSP`)에 만들어 `R8` 로 넘긴 것이고, `KxContextToKframes` 가 거기에 `Rbx(+0x100)·Rdi(+0x108)·Rsi(+0x110)·R12~R15` 를 쓴다 (`_KEXCEPTION_FRAME` PDB 칸과 맞음).

2. **옮기기 전 검사** (유저 길에서 타는 순서대로. "실패 시" 는 `.asm` 의 분기):

   | 순서 | 검사 | 자리 | 실패 시 |
   |---|---|---|---|
   | ① | IRQL 을 APC_LEVEL(1) 로 올림 (낮을 때만) | `KiContinueEx +0x2a~+0x40`, 되돌림 `+0x180~+0x19d` | — |
   | ② | CONTEXT **16바이트 정렬** | `KiContinueEx +0x93 AND EAX,0xf` / `+0xa9 TEST RAX,RAX` → `+0xa3` | `ExRaiseDatatypeMisalignment` (call `+0xa3`, ret `+0xa8`) → 예외 → ④ |
   | ③ | **TestAlert 참일 때만**: CONTEXT 첫 바이트와 끝 바이트(`+0x4cf`, 크기 `0x4d0`) 를 읽고 **같은 값으로 다시 씀** (쓰기 가능 검사). TestAlert 거짓이면 첫 바이트 읽기만 (`+0x165~+0x170`). 포인터는 `0x7fffffff0000` 에 눌러 붙임 (`CMOVC`) 해서 그 위 주소는 읽기에서 바로 터짐 | `+0x82~+0xc1` | 접근 예외 → ④ |
   | ④ | **`KiContinueEx` 전체 `__try`** `+0x6c~+0x177` (필터 = 상수 1 = EXECUTE_HANDLER, 처리기 `__C_specific_handler`) | `.pdata` scope | 착지 `+0x177`: `MOV [RSP+0x34],EAX` (**예외 코드를 반환값으로**) + IRQL 되돌림 → `+0x180`. 착지점 안에 호출 없음 (바이트 `89 44 24 34 / 0f b6 5c 24 32`). 아래 ⑤~⑩ 의 예외도 여기로 온다 |
   | ⑤ | `KiContinuePreviousModeUser` 의 `__try` `+0x58~+0xb9`: **유저 CONTEXT 에서 `ContextFlags` 읽기(`+0x58`)** + 인자 구조체 복사·모양 검사 | `.pdata` scope (처리기 `__GSHandlerCheck_SEH`, 필터 = 상수 1) | 착지 `+0xb9`: `JMP +0x2be` (5바이트, 호출 없음) → 예외 코드를 반환. 구조체 모양 틀림은 `+0xad MOV EAX,0xc000000d` (STATUS_INVALID_PARAMETER) → `+0x2be` |
   | ⑥ | **`ContextFlags` 검사** `RtlpValidateContextFlags2` (call `+0xcf` ret `+0xd4`, call `+0x106` ret `+0x10b`, call `+0x186` ret `+0x18b`) | KPMU | 음수 상태면 그대로 반환 (`+0xf3`, `+0x117`, `+0x18d` → `+0x2be`). **단 `0xc000000d` 이거나 아키텍처 비트(0x100000, `+0xd8 BT EDI,0x14`)가 없으면 거절하지 않고 깎는다**: `+0x459 AND EDI,0xf810001f` / `+0x45f BTS EDI,0x14` 로 알려진 비트만 남기고 계속 |
   | ⑦ | **커널로 복사**: 길이 `RtlGetExtendedContextLength3` (call `+0x133` ret `+0x138`) → 스택에 잡기 `__chkstk` (`+0x15b`, ret `+0x160`) → `memset` (`+0x16d`) → `RtlInitializeExtendedContext2` (`+0x1a9`, ret `+0x1ae`) → **`RtlpReadExtendedContext` (call `+0x1c9`, ret `+0x1ce`) = 유저 CONTEXT → 커널 복사본(`RBX`)** | KPMU | 음수 상태 반환 (`+0x1b0`, `+0x1d0` → `+0x2be`). 복사 중 접근 예외는 ④ 로 |
   | ⑧ | **`SegCs` 고정**: WoW64 가 아니고 Pico 가 아니면 복사본의 `SegCs = 0x33` 로 덮음 | `+0x27a` (ret `+0x27f`) `PsWow64GetProcessMachine`, `+0x293 MOV word [RBX+0x38],0x33` | — |
   | ⑨ | **`Rsp` 검사**: 프로세스의 `KPROCESS.CheckStackExtents` (`+0x88` bit5, PDB) 가 켜졌고 플래그에 CONTROL 이 있으면 `RtlGuardIsValidStackPointer(Rsp, TEB)` (call `+0x24a`, ret `+0x24f`) | `+0x212~+0x251` | `+0x488` → 64비트면 `+0x2bb` 로 `0xc000000d` 반환 |
   | ⑩ | **`Rip` 검사 (CET)**: **스레드가 `CetUserShadowStack`(`_KTHREAD +0x74` bit20) 이고(`+0x2a0`) 프로세스 `MitigationFlags2.UserCetSetContextIpValidation`(`_EPROCESS +0x754` bit17, `+0x36e BT R12D,0x11`) 일 때만** `KiVerifyContextIpForUserCet` (call `+0x399`, ret `+0x39e`) | KPMU `+0x367~+0x3da` | 결과 `0xc000060a` (STATUS_SET_CONTEXT_DENIED `[추측: 이름]`) 이면: 감사 모드(`+0x3bf TEST [R13+0x754],0x40000` = `AuditUserCetSetContextIpValidation`)는 로그만 남기고 통과 (`+0x3d5` ret `+0x3da` 인자 1), 아니면 로그(조각 `+0x2c`, 인자 2) 후 **`0xc000060a` 반환** (`+0x2b0` 이 `ESI` 를 안 지움). 다른 음수는 그대로 반환 |
   | ⑪ | 확장 기능 막기: 스레드 `ExtendedFeatureDisableMask`(`_KTHREAD +0x428`) 가 0 이 아니고 복사본이 XSTATE 이면 마스크와 겹치는지 | `+0x403~+0x44e` | `+0x44e JMP +0x2bb` → `0xc000000d` |
   | ⑫ | (여기까지 통과) `KeContextToKframes` | `+0x422` | — |

   - ⑩ 의 `Rip` 검사 자체는 `CONTEXT.Rip` 가 `0x10000~0x7ffffffdffff` 밖이면 바로 `0xc000060a` (`KiVerifyContextIpForUserCet +0x2e~+0x42`, `+0x1bf`) 이고, 안이면 아래 CET 줄 갈래로 간다.
   - 32비트(WoW64)·Pico 갈래 (`+0x27a`, `+0x4c0`, 조각 `FUN_1406d7d30` 의 `RtlGuardIsValidWow64StackPointer`)는 64비트 test.exe 가 안 탄다 `[추측: test.exe 는 64비트]`.

3. **CET: `PspInitializeThunkContext +0x295 KePopulateContinuationContext(원래 Rip)` 가 넣어 둔 값을 이 길에서 맞춰 보나: 예 (CET 가 켜진 스레드일 때만).**
   - 넣는 쪽 (`KePopulateContinuationContext.asm`): 스레드 `+0x74` bit20 이면 `RDMSR 0x6a7` (유저 그림자 스택 포인터) → `−8` → **`WRUSSQ [칸], 원래 Rip`** → `WRMSR 0x6a7` = 유저 그림자 스택에 원래 `Rip`(= `RtlUserThreadStart`) 한 칸을 올려 둔다.
   - 맞춰 보는 쪽 (`KiVerifyContextIpForUserCet`, KPMU `+0x399` 가 부름): 그림자 스택 포인터를 `RDMSR 0x6a7` 로 읽고 (`+0x13a~+0x145`), **그 칸의 값과 `CONTEXT.Rip` 를 비교** (`+0xfc~+0x101 CMP [RAX],RDI`). 같으면 `+0x128 ADD RAX,8` 로 포인터 한 칸 올려 `[R15]` 에 쓰고 성공 (`XOR EAX,EAX`).
   - **`Rip == PsNtdllExports+0x10`(= `RtlUserThreadStart`) 이면 `ContinueType` 을 0(UNWIND) → 1(RESUME) 로 바꿔서 그림자 스택 대조 쪽으로 보낸다** (`+0x9d CMP RDI,[PsNtdllExports+0x10]` / `+0xa6 MOV [RSI],EBX`). BOOLEAN 길은 `ContinueType = 0` 이라 이 특례를 정확히 탄다.
   - 그래서 run16 의 `[추측]` "NtContinue 로 돌아갈 때 검사 맞추기" 는 **코드로 뒷받침됨** `[정적]`. 단 이 전체가 **스레드 `CetUserShadowStack` + 프로세스 `UserCetSetContextIpValidation` 둘 다 켜졌을 때만** 이고, 안 켜졌으면 `KiContinuePreviousModeUser +0x2a0` (또는 `+0x378`) 에서 건너뛴다 (`+0x2ae XOR ESI,ESI`). 이 VM 에서 켜졌는지는 동적 (D6).
   - 새 그림자 스택 포인터를 `KiContinuePreviousModeUser` 안에서 되돌려 쓰는 줄은 못 찾았다 (`[RBP+0x8]` 는 `+0x39e` 이후 안 읽음). 실제로 유저 SSP 가 어떻게 되는지는 이 요청 밖 `[공백]`.

### 3. TestAlert (`TRUE`)

- **경보 검사를 부른다: `KeTestAlertThread`.** 유저 모드 길: `KiContinueEx +0xc9` (ret **`+0xce`**) `CL = 1` (UserMode). TestAlert 가 거짓이면 안 부른다 (`+0x98 JNZ` 안 타고 `+0x9d JZ +0x165` → `+0xdf`). (커널 모드 길은 `+0x1ec` ret `+0x1f1`, `CL = 0`.) 부르는 시점은 **CONTEXT 를 트랩 프레임에 쓰기 전** (`+0xeb` 보다 앞).
- `KeTestAlertThread` 가 하는 일 (`KeTestAlertThread.asm`): IRQL 2 로 올리고 스레드 잠금 (`+0x43 BTS.LOCK [thread+0x40]`) → `Alerted[모드]` (`+0x72+모드`) 가 켜졌으면 끄고 TRUE (`+0x67~+0xce`). 아니고 **모드가 유저이며 유저 APC 큐 (`+0xa8`, `ApcState.ApcListHead[1]`) 가 비어 있지 않으면 `+0x83 OR [thread+0xc2],2` = `ApcState.UserApcPending` 를 켠다.** 반환값은 `KiContinueEx` 가 안 쓴다.
- **유저 APC 를 배달하게 만드나: 간접으로 그렇다.** 배달은 `NtContinueEx` 의 출구 고리가 한다: `+0x148 TEST byte [thread+0xc2],3` (bit0 `SpecialUserApcPending` 또는 bit1 `UserApcPending`) 이 0 이 아니면 `+0x15b` (ret **`+0x160`**) `KiInitiateUserApc` (IRQL 1, `STI`) 후 다시 처음부터 검사. 이건 **TestAlert 가 거짓이어도 돈다** (이미 `UserApcPending` 이 켜져 있으면). TestAlert 는 "큐에 든 APC 를 지금 배달 대상으로 올리는" 역할이다.
- 이 순서라서, 배달할 APC 가 있으면 **먼저 트랩 프레임이 CONTEXT 로 덮이고 나서** `KiInitiateUserApc` 가 그 위에 APC 진입을 만든다 (그러면 유저는 APC 디스패처로 가고, 원래 문맥은 APC 틀에 보관됨 `[추측: KiInitiateUserApc 본문 안 읽음]`).
- test.exe 시동 때 배달될 유저 APC 가 있나: **`[추측]` 보통 없다** (이 길의 `PspUserThreadStartup` 은 커널 APC 를 쓰고 유저 APC 를 큐에 넣는 줄은 못 봄 — sast/10). 정적으로 정할 수 없음 → 동적 (D5, Q42 의 `KiInitiateUserApc` 와 겹침).

### 4. 유저로 돌아가는 법

1. **성공하면 반환하지 않는다. `NtContinueEx` 가 자기 몸 안에서 직접 유저로 간다 (`IRETQ`).** 시스템 호출 처리기의 보통 출구(`SYSRET`)를 안 탄다.
   - 성공 판정: `NtContinueEx +0x86 TEST EAX,EAX` / `+0x88 JLE +0x473` (`KiContinueEx` 반환값 **1 이상이면 성공**). `KiContinueEx` 는 성공 때 `1` (`+0x51 MOV [RSP+0x34],ESI`, `+0xf6 CMOVNS EAX,ESI`) 을 돌려준다.
   - 유저로 돌아가는 사슬 (`NtContinueEx.asm`, 모두 `NtContinueEx +` 기준; 사다리 뒤는 `.asm` 에서 "조각 `FUN_1406ae17a`" 로 보이는 이어지는 몸통):
     - `+0xb7~+0x10b` 예외 프레임(`RBX·RDI·RSI·R12~R15·XMM6~15`) 되살림.
     - `+0x10f CLI`, `+0x112 RDSSPD`/`+0x121 INCSSPD` (커널 그림자 스택이 켜졌으면 한 칸 건너뜀. 목적은 "이 함수가 `RET` 하지 않으니 반환 주소 칸을 버리는 것" `[추측]`), `+0x126 TEST byte [RBP+0xf0],1` = 새 트랩 프레임 `SegCs` 의 RPL → 유저면 계속.
     - `+0x133~+0x16a` **유저 APC 고리** (위 질문 3), `+0x16c` (ret `+0x171`) `KiSynchronizeUserIsolationDomainEntry`, `+0x186` `KiUpdateStibpPairing`, `+0x19c` `KiRestoreSetContextState`, `+0x1b8` `KiCopyCounters`, `+0x1c6 LDMXCSR`, `+0x1d4` `KiRestoreDebugRegisterState` (트랩 프레임 `Dr7` 하위 16비트가 0 이 아닐 때), `+0x1e2~+0x20f` 커널 그림자 스택 처리 (`RSTORSSP`/`SAVEPREVSSP`), `+0x213~+0x25d` MSR 쓰기 (`0x48`, `0x49`: 추측 실행 막기), `+0x26e` RSB 채우기 사다리 (추측 실행 완화 `[추측]`), 이어서 `+0x3a5~+0x3bb` 조건부 `DIV` (완화 장치 `[추측]`, 비트 `GS:[0x858] & 0x100`).
     - **`+0x3be~+0x3ee` 트랩 프레임에서 되살림: `XMM0~5`, `R11`(`RBP−0x20`), `R10`, `R9`, `R8`, `RDX`(`RBP−0x40`), `RCX`(`RBP−0x48`), `RAX`(`RBP−0x50`)**.
     - `+0x3f2 MOV RSP,RBP` / `+0x3f5 MOV RBP,[RBP+0xd8]` (= 트랩 프레임 `Rbp`, +0x158) / `+0x3fc ADD RSP,0xe8` (이제 `RSP` = 트랩 프레임 `Rip` 칸 +0x168).
     - 갈림: `+0x403 TEST [KiKvaShadow],1` → 켜졌으면 **`+0x40c JMP KiKernelExit`** (꼬리호출, 출구 본문은 범위 밖), 꺼졌으면 `+0x411~+0x41e` (`VERW` 조건부) → `+0x423 SWAPGS` → **`+0x426 IRETQ`**. 둘 다 트랩 프레임 `Rip·CS·EFlags·Rsp·SS`(+0x168~+0x188) 를 쓴다.
   - **`Rcx` = 진입점이 살아서 유저로 가는 것을 보장하는 표시**: 스레드 칸·플래그 같은 건 **없다**. 대신 **경로 자체**가 보장한다 — `KxContextToKframes` 가 `Rcx` 를 트랩 프레임 +0x38 에 쓰고 (질문 2), `NtContinueEx +0x3ea MOV RCX,[RBP−0x48]` 이 그것을 읽어 `IRETQ` 직전에 되살린다. 시작에 `+0x17 MOV [RBP−0x50],RAX(=0)` 로 트랩 프레임 `Rax` 를 0 으로 미리 써 두는데, CONTEXT 에 INTEGER 비트가 있으면 `Rax` 도 CONTEXT 값으로 덮인다.
   - 커널 모드로 돌아가는 경우도 있다 (`+0x12d JZ +0x428`): 같은 되살림 후 `+0x471 IRETQ`. 그 앞 `+0x8e~+0xb1` 에서 스레드의 `TrapFrame(+0x90)`·`PreviousMode(+0x232)` 를 트랩 프레임 값으로 되돌린다 (유저 복귀에선 건너뜀: `+0x9e JNZ +0xb7`).
2. **실패하면 무엇을 돌려주나: `NtContinueEx +0x473 ADD RSP,0x138` / `+0x47a RET`, `EAX` = 상태(NTSTATUS).** 정상 시스템 호출 반환이라 이 뒤는 보통 출구(범위 밖)로 `ntdll!NtContinue +0x14` `RET` → `LdrInitializeThunk +0x18` 로 돌아온다. 코드에 보이는 값:
   - 0 (STATUS_SUCCESS): `KiContinueEx +0xff~+0x119` — 플래그 bit1("APC 먼저") + TestAlert 가 있고 `UserApcPending` 이면 CONTEXT·예외 프레임 포인터를 트랩 프레임에 적고 (`+0xff [R13+0xd0]`, `+0x106 [R13+0x160]`) `KiDeliverApc(1,0,트랩)` (call `+0x114`, ret `+0x119`) 후 0. **BOOLEAN 길에서는 못 탄다.**
   - `0xc000000d` (STATUS_INVALID_PARAMETER): 인자 구조체 모양 틀림, `Rsp`·확장기능 검사 실패 등 (질문 2 표).
   - `0xc000060a`: `Rip` 검사 거절 (질문 2 ⑩).
   - 예외 코드 그대로: 정렬 틀림 `ExRaiseDatatypeMisalignment` (0x80000002 `[추측: 값은 알려진 것]`), 접근 예외 (0xc0000005) 등.
   - `LdrInitializeThunk +0x18 MOV ECX,EAX` / `+0x1a` (ret `+0x1f`) `RtlRaiseStatus(그 상태)` — run16 그대로.

### 5. `ntdll!RtlUserThreadStart` (rva `0xacac0`, 91바이트)

1. **인자: `rcx` = 시작 주소 (exe 진입점), `rdx` = 파라미터. 맞다.** `+0x4 MOV R9,RCX` 로 `rcx` 를 보관하고 `+0x1a MOV R8,RDX`. 처음 스레드는 `Rcx = TransferAddress`, `Rdx = PEB` (run16 `[정적]` 칸 이동 + 위 질문 2 에서 `Rdx +0x88→+0x40` 되살림). 값 자체는 동적 (D2).
2. **`+0x7 MOV RAX,[Kernel32ThreadInitThunkFunction]` (rva `0x1d2760`) 뒤, 값이 있으면 `(0, rcx, rdx)` 로 부른다: 예.**
   - `+0xe TEST RAX,RAX` / `+0x11 JZ +0x37` (0 이면 다른 갈래). 값이 있으면 `+0x1a MOV R8,RDX` (파라미터 → 3번째), `+0x1d MOV RDX,R9` (시작 주소 → 2번째), `+0x25 XOR ECX,ECX` (**1번째 = 0**) → **`+0x27 CALL` (ret `+0x2c`)**.
   - 호출 방식: **CFG 간접.** `CALL 0x180172020` = 링커 중개 `_guard_dispatch_icall$thunk$…` (`JMP`, 5바이트) → `_guard_dispatch_icall` (`JMP [__guard_dispatch_icall_fptr]`) → 전부 `JMP` 라서 **콜스택에 프레임이 안 생긴다** (run28 과 같음). 대상의 돌아올 주소 = **`RtlUserThreadStart +0x2c`**. `_calls.tsv`: `RtlUserThreadStart +0x27 +0x2c indirect:cfg:Kernel32ThreadInitThunkFunction`. → **ret 이 `+0x2c` 이다** = 01_sprint Worker 종료 콜스택 `RtlUserThreadStart+0x2c` `[관찰]` 과 **일치**.
   - 이 호출은 run25 `BaseThreadInitThunk(ECX, RDX = 시작 주소, R8 = 파라미터)` 의 `ECX = 0` 갈래와 맞다. 칸 값이 정말 `BaseThreadInitThunk` 인지는 동적 (Q48).
   - 특례: `+0x13 LEA RCX,[RtlLeaveCriticalSection]` / `+0x20 CMP RAX,RCX` / `+0x23 JZ +0x2e` → 칸 값이 `RtlLeaveCriticalSection` 주소와 같으면 CFG 없이 `+0x30` (ret `+0x35`) 직접 `RtlLeaveCriticalSection(0, rcx, rdx)`. 왜 이런 갈래가 있는지는 모름 (컴파일러가 가장 흔한 대상을 직접 호출로 바꾼 것 같은 모양 `[추측]`). test.exe 는 칸 값이 `BaseThreadInitThunk` 면 안 탄다.
   - 참고 (다른 읽는 곳): `LdrpInitializeProcess +0x1b45` 도 같은 칸을 읽어 `(1,0,0)` 으로 부른다 (call `+0x1b59`, ret `+0x1b5e`, run19·20).
3. **값이 0 이면** (`+0x37`): `+0x37 MOV RCX,RDX` (파라미터 → 1번째) / `+0x3a MOV RAX,R9` (시작 주소) / **`+0x3d CALL` (ret `+0x42`) CFG 간접으로 시작 주소를 직접 부른다** (`indirect:cfg:?`, 대상은 `RAX`) → 결과 `EAX` 를 `ECX` 로 → **`+0x44` (ret `+0x49`) `RtlExitUserThread(결과)`**. 즉 시작 함수의 반환값이 스레드 종료 코드가 된다.
4. **예외 처리**: `.pdata` 상 `__try` 범위 **`+0x7~+0x4a`** (위 호출 전부 포함), 처리기 `__C_specific_handler`, 필터 **`RtlUserThreadStart$filt$0`** (rva `0x169b61`), 착지점 **`+0x4a`**.
   - 필터 안 (오프셋은 필터 함수 `$filt$0` 시작 기준, `_calls.tsv` 의 `seh:` 줄): `+0x19` 전역 **`RtlpUnhandledExceptionFilter`**(rva `0x1d28e8`) 를 `+0x20` (ret `+0x25`) `RtlDecodePointer` 로 풀고, 값이 있으면 `+0x3a` (ret `+0x3f`) CFG 간접 호출 (인자 = `EXCEPTION_POINTERS`), 없으면 `+0x4c` (ret `+0x51`) **`RtlUnhandledExceptionFilter2(EXCEPTION_POINTERS, 상수)`**. 필터가 돌려준 값이 "처리할래" 면 착지.
   - 착지 `+0x4a`: `MOV EDX,EAX` (예외 코드) / `OR RCX,-1` / **`+0x50` (ret `+0x55`) `NtTerminateProcess(현재 프로세스, 예외 코드)`**. 즉 **스레드 시작 함수 안에서 처리 안 된 예외가 여기까지 오면 프로세스가 죽는다** `[정적]`.
5. **돌아오면 무엇을 하나**: 값 있는 갈래는 `+0x2c JMP +0x56` → `+0x56 ADD RSP,0x48` / `+0x5a RET` (평범한 반환). 그러나 `BaseThreadInitThunk` 는 `ECX = 0` 에서 `RtlExitUserThread` 로 끝나 돌아오지 않는다 (run25 `+0x19`, ret `+0x20`) → **돌아오지 않는 설계** `[정적: run25 + 이번]`. 어쩌다 돌아와도 `RET` 가 가리킬 칸은 아무도 안 썼다 (run16: `Rsp = R15−0x28` 의 칸에 쓰는 줄 없음) `[추측]`.

### 6. ntdll `NtContinue` 스텁 (rva `0x161610`, 24바이트)

- `+0x0 MOV R10,RCX` / **`+0x3 MOV EAX,0x43`** / `+0x8 TEST byte [0x7ffe0308],1` / `+0x10 JNZ +0x15` (→ `INT 0x2e`) / **`+0x12 SYSCALL`** / `+0x14 RET`. **SYSCALL 번호 = `0x43`.**
- **서비스 표 확인 (쉽게 됨)**: ntoskrnl `KiServiceTable` (PDB 공개 이름, rva `0xdadc0`, 4바이트 항목 489개; `KiServiceLimit` = 489) 의 **`[0x43]` = rva `0x6adde0` = `nt!NtContinue`** (이 빌드 표 항목은 압축 없는 rva 로 읽힘). 489개 모두 `_index.tsv` 의 함수 시작과 맞아떨어졌고 앞뒤 `[0x42] = NtDuplicateToken`, `[0x44] = NtQueryDefaultUILanguage` 라서 읽는 방법이 맞다.
- `ret_off`: `LdrInitializeThunk +0x13` call (ret `+0x18`) → 이 스텁 `+0x12 SYSCALL` (유저로 돌아올 칸 `+0x14`). 다만 성공하면 `+0x14` 로 돌아오지 않는다 (질문 4).

## 사슬 하나: `LdrInitializeThunk +0x18` 에서 `BaseThreadInitThunk` 까지 `[정적]`

`Rip`·`Rcx`·`Rdx` 가 어떻게 옮겨 가는지 같이 적는다 (`Rcx` = 첫 인자 칸, `Rdx` = 둘째).

```
[유저 ntdll]  LdrInitializeThunk
   +0x13 call (ret +0x18)  NtContinue(RCX = 맡긴 CONTEXT, DL = 1)        Rip=ntdll!NtContinue  RCX=ctx  RDX=…01
   ntdll!NtContinue  +0x3 MOV EAX,0x43 ; +0x12 SYSCALL (ret +0x14)
════════ SYSCALL: 유저 → 커널 (시스템 호출 처리기, 범위 밖) ════════
[커널 nt]  트랩 프레임: Rip=ntdll!NtContinue+0x14, Rcx=…, 아직 옛 값
   nt!NtContinue   +0x0 MOVZX EDX,DL ; +0x3 JMP                         RCX=ctx  RDX=0x1(BOOLEAN)
   nt!NtContinueEx +0x81 call (ret +0x86)  KiContinueEx(ctx, 1, 예외프레임, 트랩프레임)
   nt!KiContinueEx +0x5e PreviousMode≠0 → 유저 길
                   +0xc9 call (ret +0xce)  KeTestAlertThread(1)           (TestAlert=TRUE 라서)
                   +0xeb call (ret +0xf0)  KiContinuePreviousModeUser(ctx, 예외, 트랩, 1)
   nt!KiContinuePreviousModeUser
                   +0x58 ContextFlags 읽기 → +0xcf.. 검사 → +0x1c9 call (ret +0x1ce) RtlpReadExtendedContext = 커널 복사본
                   (+0x293 복사본 SegCs=0x33 ; CET 켜졌으면 +0x399 call (ret +0x39e) KiVerifyContextIpForUserCet)
                   +0x422 call (ret +0x427)  KeContextToKframes(트랩, 예외, 복사본, 플래그, 1)
      → KeContextToKframes +0x78 (ret +0x7d) KxContextToKframes:
            트랩.Rip(+0x168) ← ctx.Rip(+0xf8) = RtlUserThreadStart      ← 이 순간 Rip 가 바뀜
            트랩.Rcx(+0x38)  ← ctx.Rcx(+0x80) = exe 진입점
            트랩.Rdx(+0x40)  ← ctx.Rdx(+0x88) = PEB
            트랩.Rsp(+0x180) ← ctx.Rsp(+0x98)
   (KiContinueEx 가 1 을 돌려줌) → NtContinueEx +0x86/+0x88 성공 → 유저 APC 고리 +0x148 …
   nt!NtContinueEx +0x3ea MOV RCX,[RBP-0x48] (트랩.Rcx) ; +0x3e6 RDX ; … ; +0x403 KiKvaShadow
                   +0x426 IRETQ   (켜졌으면 +0x40c JMP KiKernelExit)     Rip=RtlUserThreadStart  RCX=진입점  RDX=PEB
════════ IRETQ: 커널 → 유저 ════════
[유저 ntdll]  RtlUserThreadStart (rva 0xacac0)                         RCX=진입점  RDX=PEB
   +0x7  [Kernel32ThreadInitThunkFunction] 읽기
   +0x1a R8←RDX(PEB)  +0x1d RDX←R9(진입점)  +0x25 ECX=0
   +0x27 call (ret +0x2c)  CFG 간접 → BaseThreadInitThunk(0, 진입점, PEB)   [칸 값은 동적 Q48]
[kernel32]  BaseThreadInitThunk (rva 0x2cd70, run25)
   +0x12 call (ret +0x17)  CFG 간접 → 진입점(PEB)  (RCX = R8)             → test!mainCRTStartup (Q18)
```

- 커널 ↔ 유저 경계 둘: `ntdll!NtContinue +0x12 SYSCALL` 과 `nt!NtContinueEx +0x426 IRETQ` (또는 `+0x40c` 로 `KiKernelExit`).
- **`main` 에서 `k` 예상 (아래부터)**: `ntdll!RtlUserThreadStart+0x2c` → `KERNEL32!BaseThreadInitThunk+0x17` → `test!mainCRTStartup+?` (Q18) → … → `test!main`. 앞 둘은 `[정적]` (`+0x2c` 는 01_sprint Worker 종료 `[관찰]` 과 같음, `+0x17` 은 run25 ret). 가운뎃 줄 `test!mainCRTStartup+?`·그 사이 CRT 함수(`__scrt_common_main_seh`·`invoke_main` 등 MSVC 모양 `[추측]`)는 test.exe 쪽이라 이번 범위 밖. **CFG 중개(`_guard_dispatch_icall` 류) 프레임은 안 보인다** (`RtlUserThreadStart +0x27` 도, `BaseThreadInitThunk +0x12` 도 `JMP` 만; run28 과 같은 식).
- 진입 시점 `k` (`RtlUserThreadStart` 입구): 돌아올 주소를 아무도 안 쓴 칸이라 `RtlUserThreadStart` 한 줄만 보이고 아래는 쓰레기이거나 비어 보일 수 있다 `[추측]` (D2 에서 확인).

## flow.md 2.10 `[추측]` 두 줄 판정

| `[추측]` | 판정 | 근거 |
|---|---|---|
| "로더 일이 끝나면 `ntdll!NtContinue` 로 실행이 `ntdll!RtlUserThreadStart` 로 넘어간다" | **코드로는 맞음 `[정적]`. 실제로 그렇게 가는지는 여전히 동적 (Q29).** 코드 사슬: `NtContinue` → (`SYSCALL` 0x43) → `nt!NtContinue` → `NtContinueEx` → `KiContinueEx` → `KiContinuePreviousModeUser +0x422` `KeContextToKframes` (트랩 프레임 `Rip` ← CONTEXT `Rip` = `RtlUserThreadStart`) → `NtContinueEx +0x426 IRETQ`. 갈림 둘: CET 검사(스레드·프로세스 비트), `KiKvaShadow`(`IRETQ` 직접 vs `KiKernelExit`) | 질문 1~4, 사슬 |
| "`RtlUserThreadStart` → `KERNEL32!BaseThreadInitThunk` → `test!mainCRTStartup` 순서로 불린다" | 앞 화살표: **코드로는 맞음 (`RtlUserThreadStart +0x27`, ret `+0x2c`, CFG 간접) `[정적]`, 단 대상이 `BaseThreadInitThunk` 인지는 칸 값이라 동적 (Q48).** 뒤 화살표는 Q18 몫 (run25 에 `+0x12` ret `+0x17` 까지 있음) | 질문 5 |

## 그림 2 반영 제안

- **`PH4` 화살표 바꾸기**: `q1 ntdll!NtContinue ==> r1 ntdll!RtlUserThreadStart` 의 "커널 NtContinue 안은 공백 (추측)" 을 지우고, 둘 사이에 **경계 상자 하나**를 둔다:
  - 상자 글: "커널 안: `nt!NtContinue`→`NtContinueEx`→`KiContinueEx`→`KiContinuePreviousModeUser +0x422` `KeContextToKframes` (CONTEXT 의 `Rip`·`Rcx`·`Rdx`·`Rsp` 를 트랩 프레임에 덮어씀) → `NtContinueEx +0x426 IRETQ` (또는 `+0x40c KiKernelExit`). 성공하면 `NtContinue` 로 안 돌아옴." `[정적]`
  - 화살표 앞뒤 라벨: `SYSCALL (0x43)` / `IRETQ`. "실제로 이 길로 갔나" 는 `(동적 Q29)` 로 따로.
- **`(정적)` 상자 후보** (3개):
  1. **`nt!NtContinueEx` 경계 상자** (위): 이 칸의 핵심 — "CONTEXT 를 트랩 프레임에 쓰고 `IRETQ` 로 직접 복귀 (`SYSRET` 아님)". call `+0x81` (ret `+0x86`) / `+0x426`.
  2. **`ntdll!RtlUserThreadStart`**: "`rcx`(진입점)·`rdx`(PEB)를 `(0, rcx, rdx)` 로 바꿔 `Kernel32ThreadInitThunkFunction` 을 CFG 간접 호출 (`+0x27`, ret `+0x2c`). 0 이면 진입점을 직접 부르고 `RtlExitUserThread`. 안 돌아오는 설계. `__except` → `NtTerminateProcess`." (이유: 그림 2 의 `r1` 칸이 비어 있었다)
  3. **`KiContinuePreviousModeUser` 검사 묶음** (선택, 그림 1 쪽이 낫다): "CONTEXT 를 커널에 복사(`+0x1c9`) → `SegCs=0x33` 고정 → (CET 일 때만) `Rip` 를 그림자 스택과 대조(`+0x399`) → `KeContextToKframes`".
- **커널 쪽 상자 둘 곳**: **그림 1 (nt) 에 "NtContinue 길" 상자 세트를 새로 두고**, 그림 2 (ntdll) 에는 경계 상자 **하나만** 점선으로 두어 "자세히는 그림 1" 이라고 쓰기를 권한다. 이유: 그림 2 의 중심은 ntdll 이고, 커널 함수 네 개를 다 넣으면 `PH4` 한 칸이 그림 전체보다 커진다. 경계(SYSCALL / IRETQ)는 굵은 선 두 개로 표시.

## 동적분석으로 확인해야 할 것

대기 번호 겹침: Q29·Q30·Q42·Q48. `nt+rva` 는 커널 기준 (WinDbg `nt!NtContinueEx+0x426` 도 같은 주소일 것 `[추측]`; 안 맞으면 `bp nt+0x6ae216`), `ntdll+rva` 는 ntdll 시작 기준. test.exe 일 때만 멈추게 조건은 dast-mentor 몫 (이 함수들은 모든 프로세스가 지난다).

| # | 확인할 것 | 멈춤점 | 볼 것 | 대기 번호 |
|---|---|---|---|---|
| D1 | `NtContinue` 입구: 맡긴 CONTEXT 와 둘째 인자 | `nt!NtContinue` (rva `0x6adde0`) | `rcx` = 유저 CONTEXT (= `LdrInitializeThunk` 의 `rbx`), `dl`=1. `dt nt!_CONTEXT @rcx ContextFlags Rip Rcx Rdx Rsp SegCs` (기대: `Rip=ntdll!RtlUserThreadStart`, `Rcx`=진입점, `Rdx`=PEB). `dt nt!_KTRAP_FRAME @rbp-0x80 Rip` = `ntdll!NtContinue+0x14` (`RBP−0x80` 이 트랩 프레임이라는 근거는 `NtContinueEx +0x7d LEA R9,[RBP−0x80]`; 처리기가 `RBP` 를 그렇게 두는 건 범위 밖 `[추측]`) | Q29 |
| D2 | 옮긴 직후 트랩 프레임 | `nt+0x2bcb72` (`KiContinuePreviousModeUser +0x422`, call 직전), `nt+0x2bcb77` (ret) | 앞: `rcx`=트랩, `r8`=커널 복사본 CONTEXT (`dt nt!_CONTEXT @r8` `Rip` 이 `RtlUserThreadStart`), `r9d`=플래그 (기대 `0x10001b`?). 뒤: `dt nt!_KTRAP_FRAME @<rcx 값> Rip Rcx Rdx Rsp SegCs` | Q29 |
| D3 | 유저 복귀 직전 | `nt+0x6ae1f3` (`NtContinueEx +0x403`, `KiKvaShadow`), 이어 `nt+0x6ae216` (`+0x426 IRETQ`) 또는 `nt+0x6ae1fc` (`+0x40c`, `KiKernelExit`로 점프) | `db nt!KiKvaShadow L1`, 어느 쪽을 타나. `IRETQ` 에서 `dq @rsp L5` (`Rip·CS=33·EFlags·Rsp·SS=2b`), `rcx`=진입점, `rdx`=PEB | Q29, Q42 (`KiKvaShadow`) |
| D4 | 유저 입구: 받은 인자와 콜스택 | `ntdll+0xacac0` (`RtlUserThreadStart`) | `r rcx rdx rsp`, `k`, `dps @rsp L2`, `!peb`/`r $peb` 와 `rdx` 비교, `ln @rcx` (= `test!mainCRTStartup`) | Q29, Q30 |
| D5 | CET·APC 갈림 값 (정적으로 못 정함) | `nt+0x2bc9f0` (`KiContinuePreviousModeUser +0x2a0`), `nt+0x6adf38` (`NtContinueEx +0x148`) | `+0x2a0` 앞에서 `r14` = 스레드: `dd @r14+0x74 L1` (bit20 = `CetUserShadowStack`), `r13`→프로세스 `+0x754` bit17(`UserCetSetContextIpValidation`)·bit18(감사), `+0x88` bit5(`CheckStackExtents`). `+0x148` 에서 `db @rcx+0xc2 L1` (bit0/1 = 유저 APC 대기). `bp nt!KiInitiateUserApc` 가 이때 한 번이라도 걸리는지 | 새 항목 후보 (Q57 쯤), Q42 (`KiInitiateUserApc`) 에 더함 |
| D6 | CET 켜졌으면 `Rip` 검사 결과와 그림자 스택 | `nt+0x2bcae9` (`KiContinuePreviousModeUser +0x399`), ret `nt+0x2bcaee` | 앞: `rdx`=복사본 CONTEXT, `r9b`=완화 모드 비트. 뒤: `eax` (0 이면 통과, `c000060a` 이면 거절). `rdmsr 6a7` 이 `PspInitializeThunkContext +0x295` 가 올린 칸(`RtlUserThreadStart`) 을 가리키나 (`dq` 로 읽기) | 새 항목 후보 (D5 와 합쳐도 됨) |
| D7 | `Kernel32ThreadInitThunkFunction` 값과 호출 인자 | `ntdll+0xacae7` (`RtlUserThreadStart +0x27`, call 직전) | `ln @rax` = `KERNEL32!BaseThreadInitThunk` (rva `0x2cd70`), `rcx`=0, `rdx`=진입점, `r8`=PEB. 진입한 뒤 `k` 에서 `RtlUserThreadStart+0x2c` | Q48, Q30 |
| D8 | `main` 에서 콜스택 | `test!main` | `k` → `…mainCRTStartup+? → BaseThreadInitThunk+0x17 → RtlUserThreadStart+0x2c` 순서, CFG 중개 프레임 없음 | Q30 |

- 정적으로 못 정한 것: 갈림 둘(CET 켜짐·`KiKvaShadow`), 유저 APC 가 있는지(D5), 칸 값(D7), 맡긴 `ContextFlags` 실제 값(D2), `WinDbg` 가 `NtContinueEx` 뒤쪽을 `nt!NtContinueEx+0x426` 로 보여 주는지.
- 디버거가 붙어서 달라질 점: `KeTestAlertThread`·`KiInitiateUserApc` 쪽에 디버거 자체가 거는 APC 는 보이지 않음 `[추측]`. 커널 디버거는 `INT3`/`bp` 로 `IRETQ` 직전 상태를 못 바꾼다 (소프트웨어 중단점은 명령을 `0xcc` 로 바꿀 뿐).

## 정리: 새로 알게 된 것 (flow.md / run 문서용 후보)

- **`NtContinueEx` 는 성공하면 반환하지 않고 직접 `IRETQ` 한다.** 휘발성 레지스터(`RAX·RCX·RDX·R8~R11·XMM0~5`)는 트랩 프레임에서, 비휘발은 자기가 만든 예외 프레임에서 되살린다. 그래서 `Rcx` = 진입점이 산다 (표시 플래그 없음, 경로가 보장).
- **둘째 인자는 두 얼굴**: `0xff` 이하면 TestAlert BOOLEAN, 아니면 `_KCONTINUE_ARGUMENT`(0x18바이트: `ContinueType`·플래그·예약 둘) 포인터. 이번 길은 BOOLEAN.
- **유저 길은 `KiContinuePreviousModeUser +0x422`**, `KiContinueEx +0x1e0` 은 커널 모드 길. run16 의 두 후보 중 하나가 정해졌다.
- 이 길이 CONTEXT 를 **커널로 복사**하고 `ContextFlags` 를 **깎을 수 있다**. `Rip` 검사는 **CET(스레드+프로세스 비트) 일 때만**이고, 그때 `PspInitializeThunkContext` 가 올린 그림자 스택 값과 맞춰 본다. `Rip == RtlUserThreadStart` 는 특례(`ContinueType` 0→1).
- `NtContinue` 의 SYSCALL 번호는 `0x43`, 서비스 표 `[0x43]` = `nt!NtContinue` (확인됨).
- `RtlUserThreadStart`: `(0, rcx, rdx)` CFG 호출, ret `+0x2c`. 칸이 0 이면 시작 주소를 직접 부르고 `RtlExitUserThread`. 전체가 `__try` (`+0x7~+0x4a`), 착지 `+0x4a` `NtTerminateProcess`. 필터 `RtlUserThreadStart$filt$0` → `RtlpUnhandledExceptionFilter` 또는 `RtlUnhandledExceptionFilter2`.
- `LdrpInitializeProcess +0x1b5e` 의 `(1,0,0)` 호출과 `RtlUserThreadStart +0x2c` 의 `(0, 진입점, 파라미터)` 호출은 같은 칸의 다른 갈래다 (첫 인자 1 vs 0).

## 판정

**분석성공.** 요청서 질문 1~7 을 `.asm`·`.pdata`·PDB·서비스 표로 답했고, 정적으로 못 정하는 것(CET·`KiKvaShadow`·APC·칸 값)은 D1~D8 로 넘겼다. 알려진 사실 대조 일치, 커널 PDB GUID 기준과 같음. 스크립트 막힌 곳 없음 (사소한 표시 문제 넷은 위 "스크립트 문제"). 오래 도는 작업 없음.

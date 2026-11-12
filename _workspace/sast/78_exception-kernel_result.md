# 78 exception-kernel — 정적 분석 결과

- 요청서: `_workspace/sast/78_exception-kernel.md` (Q148a, PE_emulator 요청 26 의 커널 쪽)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음). 질문 3 에서 `KiUserExceptionDispatcher` 첫 줄만 보려고 ntdll 도 한 함수 뽑음.
- 분석: **다시 안 함** (ntoskrnl `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음). 돌아가는 Ghidra 작업 없음.
- 지도: `run_ghidra.sh map ntoskrnl.exe KiDispatchException` 과 `KiExceptionDispatch` 둘 다 만듦 (`ms/disassemble/ntoskrnl/_map/`). 지도에서 본 것: `KiDispatchException` 의 깊이 1 줄 33개 (서로 다른 callee 25개, `_edges.tsv`), 부르는 쪽은 `_calls.tsv` 로 `KiExceptionDispatch +0x140`, `KiFastFailDispatch +0xc1`, `KiRaiseException +0xfe`, `PspInitializeThunkContext +0x3c3` 넷 (디컴파일 머리글에는 `KyExceptionDispatchOnExceptionStack`·`PspFreeUserFiberShadowStack` 도 있음, 둘 다 이 요청 범위 밖).
- 뽑은 함수 (전부 `export`, SCRIPT ERROR 없음):
  - 요청서 대상 15개, **depth 0** (`KiPageFault`·`KiGeneralProtectionFault`·`KiBreakpointTrap`·`KiDivideErrorFault`·`KiInvalidOpcodeFault`·`KiDebugTrapOrFault`·`KiExceptionDispatch`·`KiDispatchException`·`KiPreprocessFault`·`KeCopyExceptionRecord`·`NtRaiseException`·`KiRaiseException`·`KiRaiseSecurityCheckFailure`·`KiFastFailDispatch`·`KeContextFromKframes`).
  - **요청서(depth 1)와 다르게 한 것**: depth 1 로 통째 뽑지 않고, 지도로 필요한 callee 만 골라 depth 0 으로 뽑았다. 보탠 것과 이유:
    - 질문 3 (유저 스택에 쓰는 함수): `RtlpCopyExtendedContext`·`ProbeForWrite`·`KePopulateContinuationContext`·`KiSetupForInstrumentationReturn`·`RtlInitializeExtendedContext2`·`RtlGetExtendedContextLength3`.
    - 질문 2 (예외 코드가 정해지는 곳): `KiOpDecode`·`KiOp_Priv`·`KiOp_Illegal`·`KiOp_Div`·`KiOp_INT`·`KiOp_Mov`, 그리고 "이름과 코드만" 이라 한 입구 11개 (`KiOverflowTrap`·`KiBoundFault`·`KiNpxNotAvailableFault`·`KiFloatingErrorFault`·`KiAlignmentFault`·`KiXmmException`·`KiStackFault`·`KiSegmentNotPresentFault`·`KiControlProtectionFault`·`KiRaiseAssertion`·`KiDebugServiceTrap`).
    - 질문 4: `DbgkForwardException` (첫 줄들만 — "디버거 없음" 갈래가 어디서 끝나는지 보려고), `KyRaiseException`, `KiExceptionDispatchOnExceptionStack` (커널 모드 갈래라 안 읽음).
    - 질문 6: `KiSystemServiceUser` (`KiExceptionDispatch` 를 부르는 곳이라).
    - ntdll `KiUserExceptionDispatcher` depth 0 (92B, 시작 레지스터 확인용 첫 줄만).
- 읽기만 한 것: 이미 있던 `PspInitializeThunkContext.asm` (질문 6). 그 밖에 `.pdata`/`UNWIND_INFO` 를 파이썬으로 직접 읽어 확인한 것 2건 (아래 표시).
- 버전 확인 (알려진 사실 대조): **일치**
  - `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines` (`_calls.tsv` 그대로).
  - 요청서가 준 rva·크기도 맞음: `KiDispatchException` `0x3eadc0` 2044B, `KiExceptionDispatch` `0x6c2700` 844B, `KiPreprocessFault` `0x3eb5e0` 878B, 입구 여섯 rva 전부.

## 스크립트 문제

막힌 것은 없다. 참고만 (고치지 않음):

1. `.asm` 의 **`__except` 본문이 "읽지 않은 틈" 으로만 나온다.** `KiDispatchException +0x652 ~ +0x6cf` (126바이트) 가 바로 유저 스택에 쓰다 실패했을 때의 처리기인데 점프로 안 닿아 틈에 들어갔다. `PspInitializeThunkContext +0x2ab ~ +0x2b9` 도 같다. 나는 `.pdata` → `UNWIND_INFO` → `C_SCOPE_TABLE` 을 직접 읽어 `JumpTarget` 을 찾고 objdump 로 풀었다. 스크립트가 scope table 의 `JumpTarget` 을 따라가 틈 대신 본문으로 붙이면 이런 처리기를 놓치지 않는다.
2. `_calls.tsv` 의 조각 호출은 `frag:FUN_…+0x…` 로만 있고 **함수 기준 `+0x…` 는 `.asm` 줄 끝 `(= KiPageFault +0x…)` 에만** 있다. 입구가 손으로 쓴 asm 이라 이 결과서는 전부 `.asm` 의 `(= …)` 값을 썼다 (아래 표에 둘 다 적음).

## 질문별 답 [정적]

읽는 법. 오프셋은 `.asm` 줄 앞의 `+0x…` (함수 시작 기준). 조각 안 줄은 `frag:FUN_xxxx+0x…` 와 `(= 함수 +0x…)` 를 같이 적는다. rva = 절대 주소 − `0x140000000`. WinDbg 가 조각 주소를 `nt!KiPageFault+0x…` 로 보여 줄지는 확인 전 `[추측]` (rva 도 같이 적어 둠).

### 먼저 알아 둘 것 (모든 질문에 걸림)

- 입구 함수들은 **진짜 예외 코드를 바로 안 넘긴다.** `ECX` 에 `0x10000001`~`0x10000010` 같은 **안쪽 표시 코드**를 담아 `KiExceptionDispatch` 로 보내고, `KiDispatchException` 안의 **`KiPreprocessFault` 가 진짜 코드로 바꾼다** (`#PF`→`0x10000004`, `#GP`→`0x10000001`, `#UD`→`0x10000002`, `#DE`→`0x10000003`). 유저는 이 표시 코드를 못 본다. `KiRaiseException +0xe6 BTR [레코드],0x1c` 가 `NtRaiseException` 으로 들어온 레코드의 코드 비트 28 (`0x10000000`) 을 지워서 유저가 표시 코드를 만들어 보내는 길도 막는 것으로 보인다 `[추측: 의도]`.
- `KiExceptionDispatch` 의 **인자 약속** (입구가 채워서 부름): `ECX`=ExceptionCode, `EDX`=NumberParameters, `R8`=ExceptionAddress, `R9`/`R10`/`R11`=ExceptionInformation[0]/[1]/[2]. 트랩 프레임은 `RBP-0x80` (입구가 `RBP = RSP+0x80` 로 잡아 둠). `KiFastFailDispatch` 도 같은 약속.
- 트랩 프레임 칸 (PDB `_KTRAP_FRAME`, `pdb_struct.py`): `FaultAddress` `+0xd0`, `FaultIndicator` `+0x2a`, `ExceptionActive` `+0x2b`, `Rip` `+0x168`, `SegCs` `+0x170`, `EFlags` `+0x178`, `Rsp` `+0x180`, `SegSs` `+0x188`. (`[RBP+x]` 는 트랩 프레임 `+x+0x80`.)

### 1. 입구 → 디스패치 사슬

**사슬 (유저 모드에서 난 예외):** 입구 `KiXxx` → `KiExceptionDispatch` → `KiDispatchException` → (`KeContextFromKframes` → `KiPreprocessFault` → …). `KiExceptionDispatch` 는 돌아오지 않는다 (끝이 `IRETQ`, 아래).

| 입구 (rva) | `KiExceptionDispatch` 부르는 곳 (call / ret) | 조각 | 입구가 하는 일 (`ECX`) |
|---|---|---|---|
| `KiPageFault` `0x6bd540` | `frag:FUN_1406bd7ae+0x1cf` / `+0x1d4` = **`+0x43d` / `+0x442`** (rva `0x6bd97d` / `0x6bd982`) | `FUN_1406bd7ae` = 함수 `+0x26e` 부터, 둘째 조각 `FUN_1406bdc56` = `+0x716` (나가는 길) | `MmAccessFault` 먼저, 아래 |
| `KiGeneralProtectionFault` `0x6bd1c0` | `frag:FUN_1406bd42e+0xf2` / `+0xf7` = **`+0x360` / `+0x365`** (rva `0x6bd520` / `0x6bd525`) | `FUN_1406bd42e` = `+0x26e` | `0x10000001` |
| `KiBreakpointTrap` `0x6ba740` | `frag:FUN_1406ba9b2+0xe4` / `+0xe9` = **`+0x356` / `+0x35b`** (rva `0x6baa96` / `0x6baa9b`) | `FUN_1406ba9b2` = `+0x272` | `0x80000003` (진짜 코드 그대로) |
| `KiDivideErrorFault` `0x6b9800` | `frag:FUN_1406b9a72+0xd8` / `+0xdd` = **`+0x34a` / `+0x34f`** (rva `0x6b9b4a` / `0x6b9b4f`) | `FUN_1406b9a72` = `+0x272` | `0x10000003` |
| `KiInvalidOpcodeFault` `0x6bb540` | `frag:FUN_1406bb7b2+0xd8` / `+0xdd` = **`+0x34a` / `+0x34f`** (rva `0x6bb88a` / `0x6bb88f`) | `FUN_1406bb7b2` = `+0x272` | `0x10000002` |
| `KiDebugTrapOrFault` `0x6b9b80` | `frag:FUN_1406b9fb2+0x1ce` / `+0x1d3` = **`+0x600` / `+0x605`** (rva `0x6ba180` / `0x6ba185`) | `FUN_1406b9fb2` = `+0x432` | `0x80000004` |

- 오류 코드가 없는 입구 (`#BP`·`#DE`·`#UD`·`#DB`) 는 첫 줄이 `SUB RSP,8` (가짜 칸), 오류 코드가 있는 입구 (`#PF`·`#GP`) 는 바로 `PUSH RBP`. 이어서 `SUB RSP,0x158; LEA RBP,[RSP+0x80]` 로 트랩 프레임을 만들고 레지스터를 채운다 (`KiPageFault +0x0 ~ +0x2c`).
- 입구 공통 머리: `KiFlushBhbDuringTrapEntryOrExit` (예: `KiPageFault` 조각 `+0x26`/`+0x2b` = `+0x294`/`+0x299`), `KiSaveDebugRegisterState` (`+0x2f4`/`+0x2f9`, 디버그 레지스터 쓰는 스레드만), `KiSynchronizeUserIsolationDomainExit` (`+0x328`/`+0x32d`, 유저 모드에서 온 것만). 줄기 밖이라 이름만.
- `KiExceptionDispatch` 가 `KiDispatchException` 을 부르는 곳: **`+0x140` / ret `+0x145`** (rva `0x6c2840` / `0x6c2845`). 갈림: `+0x8c AND R9B,1` (이전 모드 = `SegCs & 1`) 직후 `+0x9f JNZ 0x1406c2840` → **유저 모드면 바로 `KiDispatchException`** (사이의 줄들은 플래그를 안 건드림). 커널 모드면 IRQL·스택 검사 (`+0xa5 ~ +0x12d`) 를 거쳐 `+0x139 / ret +0x13e KiExceptionDispatchOnExceptionStack` 로 갈 수 있음 — 이번 범위 밖.
- `KiDispatchException` 에 넘기는 인자 (`KiExceptionDispatch +0x90 ~ +0x9c`): `RCX`=`RSP+0x138` (EXCEPTION_RECORD 0x98), `RDX`=`RSP` (예외 프레임), `R8`=`RBP-0x80` (트랩 프레임), `R9B`=이전 모드, **`[RSP+0x20]`=1 (첫 기회, `+0x90 MOV byte [RSP+0x20],1`)**. 레코드를 채우는 줄: `+0x67`(코드) `+0x6b`(플래그 0) `+0x6e`(ExceptionRecord 0) `+0x72`(주소 R8) `+0x76`(개수 EDX) `+0x79/+0x7d/+0x81`(정보 R9/R10/R11).
- **`KiPageFault` 의 갈림 (`MmAccessFault` 먼저):**
  - `MmAccessFault` 부르는 곳: 조각 `+0x118` / `+0x11d` = **`+0x386` / `+0x38b`** (rva `0x6bd8c6` / `0x6bd8cb`). 인자 `ECX`=오류 코드 (`+0x342 MOV EAX,[RBP+0xe0]` 에서 가져옴), `RDX`=`CR2` (`+0x348 MOV RCX,CR2`, `+0x358` 에서 `[RBP+0x50]` = `FaultAddress` 에 저장), `R8B`=`SegCs & 1`, `R9`=트랩 프레임.
  - **`+0x38d JGE 0x1406bd9ae`**: 반환 ≥ 0 (해결됨) → 예외 없이 `+0x46e` (`PsWatchEnabled` 면 `PsWatchWorkingSet`) → `+0x489` 로 나가는 길. 음수 (실패) 일 때만 아래로.
  - 실패 갈림: `+0x393 TEST [RBP+0xf0],1` (유저 CS?) · 유저면 `+0x39c CMP SegCs,0x33` (아니면 WoW64 쪽 `+0x3e4`) · `0x33` 이면 `+0x3a6 ~ +0x3c8` 에서 유저 GS 베이스 (`[RBP-0x18]`, 입구에서 MSR `0xC0000102` 를 읽어 둔 것) 를 스레드가 기대하는 값과 비교, **다르면 MSR 을 고치고 예외 없이 `+0x489` 로 돌아감** `[추측: 유저가 GS 베이스를 망가뜨린 경우 구제]`. 같으면 `+0x3f2` 로.
  - **`+0x3f2 ~ +0x428` 상태 → 예외 코드:** `MmAccessFault` 반환값 `EAX` 를 비교해서
    - `0xD0000006` (`+0x3f9`) → 예외 아님, `+0x442` 의 `KiBugCheckDispatch(ECX=0xA)` 길 (IRQL_NOT_LESS_OR_EQUAL).
    - `0xC0000005` (`+0x401`) → `ECX = 0x10000004` (`+0x428`, **안쪽 표시 코드**), `EDX=2`.
    - `0x80000001` (`+0x409`, 가드 페이지) · `0xC00000FD` (`+0x411`, 스택 넘침) → 그 코드 그대로, `EDX=2`.
    - 그 밖 → `ECX = 0xC0000006` (STATUS_IN_PAGE_ERROR, `+0x419`), `EDX=3`, `R11D` = 원래 `MmAccessFault` 상태 (`+0x423`).
  - `+0x42d` `R10`=`[RBP+0x50]` (CR2), `+0x431` `R9`=`[RBP-0x56]` (읽기/쓰기/실행 값), `+0x436` `R8`=`[RBP+0xe8]` (Rip), `+0x43d` call.
  - `+0x364 JNC` : 불린 쪽 EFLAGS 의 IF (비트 9) 가 꺼져 있으면 `MmAccessFault` 도 안 부르고 바로 `+0x468` 버그체크 길 (`KiBugCheckDispatch` ECX=0xA, IRQL=0xff). 유저 코드는 IF 가 항상 켜져 있어 해당 없음.
- **`KiPreprocessFault` 는 어디에 끼나:** 입구에도 `KiExceptionDispatch` 에도 없다. **`KiDispatchException` 안**: `KeContextFromKframes` (`+0x13d` / ret `+0x142`) 로 문맥을 만든 **뒤**, 유저·커널 갈림 (`+0x167 ~ +0x16e`) **앞**에서 **`+0x15e` / ret `+0x163`** (인자 `RCX`=레코드, `RDX`=커널 문맥, `R8B`=이전 모드). 반환 `AL != 0` 이면 (`+0x165 JNZ 0x1403eaf64`) "처리됨" — 문맥을 트랩 프레임에 되돌려 쓰고 (`+0x1b8 KeContextToKframes`) 예외 없이 나간다 (ATL thunk 흉내, XSTATE 켜기 등).
  - 건너뛰는 갈래: `+0x147 CMP [레코드],0x80000003; JZ 0x1403eb4c7` — 코드가 `STATUS_BREAKPOINT` 면 `+0x707 DEC [문맥+0xf8]` (문맥 Rip −1, 질문 2) 후 `+0x154` 로 돌아와 (`KiDynamicTraceMask` 비트 1 이 꺼져 있으면 `+0x716 JZ`) **그래도 `KiPreprocessFault` 를 부른다**.

**지금 우리 (PE_emulator):** 이 사슬이 통째로 없다 (Unicorn 오류에서 멈춤). 필요한 것은 "Unicorn 오류/인터럽트 번호 → 입구" 표 하나와 그 뒤 `KiExceptionDispatch` 인자를 채우는 일. 표 자체는 이 결과서 질문 2.

### 2. 예외마다 `EXCEPTION_RECORD`

`KiPreprocessFault` 가 표시 코드를 진짜 코드로 바꾼 **뒤의 값**이 유저가 본다. `ExceptionFlags` 는 전부 0 (`KiExceptionDispatch +0x6b`), 단 `__fastfail` 만 1 (`KiFastFailDispatch +0x6b MOV [RAX+4],1` = EXCEPTION_NONCONTINUABLE). `ExceptionRecord`(링크) 는 0.

| 예외 | 입구 | `ExceptionCode` (유저가 보는 값) | `ExceptionAddress` | 개수 | `ExceptionInformation` | 근거 줄 |
|---|---|---|---|---|---|---|
| 접근 위반 (`#PF`) | `KiPageFault` | `0xC0000005` (입구는 `0x10000004`, `KiPreprocessFault +0x9b MOV [RBX],0xc0000005` 가 바꿈) | `Rip` = 못 한 명령 자리 (`+0x436 MOV R8,[RBP+0xe8]`) | 2 | `[0]` 읽기 **0** / 쓰기 **1** / 실행 **8**, `[1]` = `CR2` | 아래 |
| 가드 페이지 | `KiPageFault` | `0x80000001` | `Rip` | 2 | 〃 | `+0x409` |
| 스택 넘침 | `KiPageFault` | `0xC00000FD` | `Rip` | 2 | 〃 | `+0x411` |
| 페이지 읽기 실패 | `KiPageFault` | `0xC0000006` | `Rip` | **3** | 〃 + `[2]` = `MmAccessFault` 가 돌려준 NTSTATUS | `+0x419 ~ +0x423` |
| 특권 명령 (`#GP`, 예: `hlt` `cli` `in` `out` `mov cr`) | `KiGeneralProtectionFault` | `0xC0000096` (`KiOp_Priv +0x8`) | `Rip` | **0** (`KiOp_Priv +0x4 AND [레코드+0x18],0`) | 없음 | 아래 |
| 그 밖의 `#GP` (예: 정규화 안 된 주소 접근) | 〃 | `0xC0000005` | `Rip` | 2 | `[0]`=**0**, `[1]`=**`0xFFFFFFFFFFFFFFFF`** (`KiPreprocessFault +0x1f8 / +0x1ff / +0x203`) | 아래 |
| `ud2`·잘못된 명령 (`#UD`) | `KiInvalidOpcodeFault` | `0xC000001D` (입구 `0x10000002`, `KiPreprocessFault +0x1b5`; `KiOp_Illegal +0x8` 도 같은 코드) | `Rip` | 0 | 없음 | `+0x33c` |
| 0 나누기 (`#DE`) | `KiDivideErrorFault` | **`0xC0000094`** (입구 `0x10000003`, `KiPreprocessFault +0x231`) — 단 **나누는 값이 0 이 아니면 `0xC0000095`** (정수 오버플로, `KiOp_Div +0x104`) | `Rip` | 0 | 없음 | `+0x33c` |
| `int3` (`#BP`) | `KiBreakpointTrap` | `0x80000003` | **`Rip − 1` = `int3` 자기 자리** (`+0x34d DEC R8`) | **1** | `[0]` = 0 (`+0x350 MOV R9D,0`) | `+0x33c ~ +0x350` |
| 한 걸음 (`#DB`) | `KiDebugTrapOrFault` | `0x80000004` | `Rip` | 0 (분기 추적 켠 경우만 2) | 없음 | `+0x5ea` |
| 정수 오버플로 (벡터 4) | `KiOverflowTrap` | `0xC0000095` | `Rip − 1` | 0 | 없음 | 이름·코드만 |
| 배열 범위 | `KiBoundFault` | `0xC000008C` | `Rip` | 0 | 없음 | 〃 |
| 정렬 (`#AC`) | `KiAlignmentFault` | `0x80000002` | `Rip` | 0 | 없음 | 〃 |
| x87 (`#MF`) | `KiFloatingErrorFault` | `0xC000008E`(÷0) `0xC0000090` `0xC0000091`(오버) `0xC0000092`(스택) `0xC0000093`(언더) `0xC000008F`(부정확) | `Rip` | 1 | `[0]`=0 | 〃 |
| SIMD (`#XM`) | `KiXmmException` | 위 x87 코드 + `0xC00002B4` | `Rip` | 2 | `[1]` = 상태 레지스터 값 | 〃 |
| `__fastfail` (`int 0x29`) | `KiRaiseSecurityCheckFailure` | `0xC0000409` | `int 0x29` **자기 자리** (`+0x0 SUB [RSP],2`) | 1 | `[0]` = `RCX` (실패 코드) | 질문 5 |
| `int 0x2c` | `KiRaiseAssertion` | `0xC0000420` | `int 0x2c` 자기 자리 (`+0x0 SUB [RSP],2`) | 0 | 없음 | 이름·코드만 |
| `int 0x2d` | `KiDebugServiceTrap` | `0x80000003` | 저장 `Rip` 를 먼저 1 올린 값 (`+0x0 INC [RSP]`), `DEC` 없음 | 1 | `[0]` = `RAX` (서비스 번호) | 이름·코드만 |
| `#NM` (XSTATE 첫 사용) | `KiNpxNotAvailableFault` | 입구 `0x10000010` → `KiPreprocessFault +0x256 ~ +0x268` 이 `KiEnableOptionalXStateFeatures` 를 불러 성공하면 "처리됨" 으로 끝나 예외가 안 간다. 실패 (`EAX<0`) 면 `+0x26a JS +0x1af` 로 가서 `0xC000001D` | `Rip` | 1 | `[0]` = `RAX` | 이름만 |

- **`#PF` 의 읽기/쓰기/실행 값 만드는 줄:** `KiPageFault +0x37e SHR EAX,1` → `+0x380 AND EAX,9` → `+0x383 MOV [RBP-0x56],AL` (`FaultIndicator`). 오류 코드 비트 1 (쓰기) → 값 비트 0, 비트 4 (명령 가져오기) → 값 비트 3. 그래서 **0 = 읽기, 1 = 쓰기, 8 = 실행** (9 는 이론상 쓰기+실행). 주소는 `CR2` (`+0x348` 읽고 `+0x358` 저장). 비존재·권한 없음 구별은 안 한다 (`P` 비트 안 봄).
- **`#PF` → 접근 위반 표시 코드가 풀리는 곳:** `KiPreprocessFault +0x57 CMP EAX,0x10000004` → `+0x91 ~ +0x9b` 에서 `NumberParameters ≥ 2` 이고 `[0]&8` (실행) 이면서 유저 모드면 `+0x186 ~ +0x18b` 로 실행 위반 해석 (ATL thunk 확인 `KiCheckForAtlThunk +0x240`), 아니면 일반 읽기/쓰기 해석. 일반 접근 위반은 `NumberParameters`·`[0]`·`[1]` **그대로 두고 코드만 `0xC0000005` 로.**
- **`#GP` 가르는 곳:** 입구는 `ECX=0x10000001`, `EDX=2`, `R9 = [RBP+0xe0] & 0xFFFF` (선택자 오류 코드), `R10=0` (`+0x33e ~ +0x356`). `KiPreprocessFault +0x21a / +0x220` (표시 코드 1 → `R14D=0x80000000`, 코드 `0xC0000005`) → `KiOpDecode` (`+0xc3`) 가 명령을 읽음 → 해석기가 `KiOp_Priv` 면 (`+0x1d8 / ret +0x1dd`) **`0xC0000096`, 개수 0**. 해석 못 하거나 특권 명령이 아니면 `+0x1e2 ~ +0x20b` 가 `0xC0000005` 로 개수 2, `[0]=0`, `[1]=-1` 로 고친다 (디코더 결과 `[RBP+0x28]` 이 0 일 때; 칸 뜻은 `[추측]`).
- **`#DE`**: `KiPreprocessFault +0x22b` (표시 코드 3 → 코드 `0xC0000094`, `R14D=0x20000000`) → `KiOpDecode` 가 나누기 명령이면 `KiOp_Div` (`KiOp_Div +0x21 CMP [레코드],0xc0000094`, 나누는 값을 읽어 `+0xdd TEST RCX,RCX; JNZ +0x104` 이면 **`MOV [RDI],0xc0000095`**). 즉 0 으로 나누면 `0xC0000094`, `INT_MIN / -1` 같은 몫 넘침이면 `0xC0000095`.
- **`#BP` 주소 두 군데가 `−1`:** (a) 레코드 `ExceptionAddress` 는 입구에서 `DEC R8` (`KiBreakpointTrap +0x34d`). (b) 문맥 `Rip` 도 `KiDispatchException +0x14e JZ 0x1403eb4c7` → `+0x707 DEC qword [RBX+0xf8]` 로 −1. 트랩 프레임의 `Rip` 은 그대로라서 (질문 3 에서 새 값으로 덮음) 유저가 보는 `CONTEXT.Rip` = `int3` 자리 = 처리기가 안 고치면 같은 `int3` 을 또 실행.

**지금 우리 (PE_emulator):** 지금은 이 표 전부가 없음 (멈춤). `UC_ERR_READ_UNMAPPED`/`WRITE_UNMAPPED`/`FETCH_UNMAPPED` ↔ `[0]` 0/1/8, `UC_ERR_INSN_INVALID` ↔ `#UD`(`0xC000001D`) 또는 특권 명령(`0xC0000096`) 은 Unicorn 이 둘을 구별해 주는지 모름 `[추측]` — 구별 못 하면 우리가 명령 바이트를 읽어야 한다. 0 나누기는 나누는 값이 0 인지 보고 `0xC0000094`/`0xC0000095` 를 가름.

### 3. 유저 스택에 쌓는 모양 (가장 중요)

**한 줄 요약:** 커널이 **커널 스택에서** 문맥을 먼저 만들고 (`KeContextFromKframes`, `+0x13d` / ret `+0x142`), **유저 스택에는 한꺼번에 복사**한다 (`RtlpCopyExtendedContext` `+0x52f` / ret `+0x534`, `KeCopyExceptionRecord` `+0x4cc` / ret `+0x4d1`). `KiInitializeUserApc +0x1fb` (run42, 유저 APC) 는 같은 `KeContextFromKframes` 를 쓰지만 거기선 유저 스택에 바로 만든다는 점이 다르다 (`KiDispatchException` 은 커널 버퍼에 만든 뒤 복사).

**3-1. 가기 전 준비 (`KiDispatchException`)**

| 일 | 줄 | 내용 |
|---|---|---|
| 문맥 이름표 | `+0xa2 MOV R15D,0x10001f` · `+0xa8 [RBP+4]=R15D` | 기본 `ContextFlags` = `0x10001f` (AMD64 \| CONTROL \| INTEGER \| SEGMENTS \| FLOATING_POINT \| DEBUG_REGISTERS = `CONTEXT_ALL`, XSTATE 없음) |
| 〃 XSTATE | `+0xae CMP [RBP],DIL` → `+0xb2 JNZ 0x1403eb094` (유저 모드) → `+0x2d4 TEST [KeFeatureBits],0x800000` → `+0x2e5 MOV R15D,0x10005f` | 유저 모드이고 `KeFeatureBits` 비트 `0x800000` (XSAVE 계열 `[추측]`) 이 켜져 있으면 **`0x10005f`** (= 위 + `CONTEXT_XSTATE` `0x40`). 꺼져 있으면 `0x10001f` 그대로 (`+0x2df JZ +0xcc`). (CET: `+0xb8 ~ +0xc4` 의 `0x10009f` 는 **커널 모드 갈래**라 유저에서는 안 탐.) |
| 길이 (+ 압축 형식이면 마스크) | `+0xe3 / ret +0xe8 RtlGetExtendedContextLength3` (인자 `ECX`=이름표, `RDX`=`[RBP+0x10]`, `R8`=기능 마스크 — `KUSER_SHARED_DATA+0x3ec` 비트 1 `CompactionEnabled` 가 켜져 있으면 `+0x300 ~ +0x362` 에서 `EnabledFeatures \| EnabledUserVisibleSupervisorFeatures` 로 만들고, 아니면 0 —, `R9`=`KUSER_SHARED_DATA+0x3d8` = `XState`) | 문맥+꼬리 전체 길이 → `+0x103 / ret +0x108 __chkstk` 뒤 `+0x108 SUB RSP,RAX` 로 **커널 스택에** 확보 (`+0x10b LEA RBX,[RSP+0x30]`) |
| 빈 문맥 | `+0x12b / ret +0x130 RtlInitializeExtendedContext2` | `ContextFlags` 가 여기서 들어감, 확장 포인터가 `[RBP+0x20]` |
| 채우기 | **`+0x13d / ret +0x142 KeContextFromKframes`**(`RCX`=트랩 프레임, `RDX`=예외 프레임, `R8`=버퍼) | 아래 3-4 |
| 표시 코드 풀기 | `+0x15e / ret +0x163 KiPreprocessFault` | 질문 1·2 |
| (첫 기회, 디버거 없음) | 아래 3-5 | 유저 처리기로 보낼지 정함 |

- 첫 기회일 때만 `+0x9c JNZ 0x1403eafa8` 가 먼저 가서 "피코(Pico) 프로세스" 면 표시 코드 `0x10000002/3/4` 를 `0xC000001D`/`0xC0000094`/`0xC0000005` 로 미리 바꾸고 `PsPicoDispatchException` 에 넘긴다 (`+0x1e8 ~ +0x229`, `+0x2fe7c2 ~ +0x2fe813`). 보통 프로세스는 `[프로세스+0x640]` (`PicoContext`) 가 0 이라 `+0x1f9 JZ +0xa2` 로 안 탄다.

**3-2. 얼마를 빼나 · 차례 · 크기 · 정렬 (유저 스택 배치 계산)**

`S0` = 예외 순간의 유저 `RSP` (= `CONTEXT.Rsp`, `+0x22e MOV R15,[RBX+0x98]`). 계산 줄은 `+0x430 ~ +0x4a8` (공통), XSTATE 전처리 `+0x62a ~ +0x644`.

| 이름 | 계산 | 줄 | 크기·정렬 |
|---|---|---|---|
| `A` (XSTATE 꼬리 시작) | XSTATE 없음: `A = S0`. 있음: **`A = (S0 − XState.Length) & ~0x3F`** (`XState.Length` = 커널 확장 문맥 `[+0x14]`) | `+0x62e ~ +0x640` (XSTATE 만), `[RBP+0x38]` | 64바이트 정렬 |
| `M` (머신 프레임) | **`M = (A − 0x28) & ~0xF`** | `+0x430 LEA RDI,[R15-0x28]` · `+0x434 AND RDI,-0x10` | 0x28 바이트, 16바이트 정렬 |
| `E` (예외 레코드) | `E = M − 0xA0` | `+0x43c LEA RBX,[RDI-0xa0]` | 0x98 바이트 (칸은 0xA0, 16 정렬) |
| `X` (CONTEXT_EX) | `X = E − 0x20` | `+0x447 LEA R8,[RBX-0x20]` | 0x20 바이트 (칸) |
| `C` (CONTEXT, **새 RSP**) | **`C = X − 0x4D0`** | `+0x452 LEA R9,[R8-0x4d0]` | 0x4D0 바이트, 16 정렬 (`C` 는 `M` 에서 0x590 아래) |

→ **새 `RSP` = `C = ((A − 0x28) & ~0xF) − 0x590`.** XSTATE 없음이고 `S0` 가 16 의 배수 + 8 (함수 안에서 보통) 이면 `C = S0 − 0x5B8`, 16 의 배수면 `C = S0 − 0x5C0`. 예 (꾸민 숫자): `S0=…F6E8` → `M=…F6C0`, `E=…F620`, `X=…F600`, `C=…F130`. 레드 존은 없음 (`S0` 바로 아래부터 씀).
- 교차 확인 `[정적]`: ntdll `KiUserExceptionDispatcher` (rva `0x164e10`) 의 `UNWIND_INFO` (내가 `.pdata` 로 읽음) 에 **`UWOP_ALLOC_LARGE 0x590` + `UWOP_PUSH_MACHFRAME(오류 코드 없음)`** 이 있다 = 시작 `RSP` 에서 0x590 위에 머신 프레임이 있다는 뜻, 위 `M − C = 0x590` 과 같다.

**3-3. 표 하나로: 시작 `RSP` (= `C`) 기준**

| `RSP` 기준 오프셋 | 크기 | 무엇 | 값 / 누가 쓰나 | 줄 |
|---|---|---|---|---|
| `+0x000` | 0x30 | `CONTEXT.P1Home`~`P6Home` | **안 씀** (유저 스택에 원래 있던 값) | 복사 목록에 없음 (`RtlpCopyExtendedContext +0x127 ~ +0x30f`) |
| `+0x030` | 4 | `ContextFlags` | 커널 이름표 `& 0x67ffffff` = **`0x10001f`** (또는 XSAVE 켜짐이면 **`0x10005f`**) | `RtlpCopyExtendedContext +0x11f ~ +0x127` |
| `+0x034` | 4 | `MxCsr` | 트랩 프레임 `+0x2c` (FLOATING_POINT 일 때) | `KeContextFromKframes +0x34b / +0x34e` |
| `+0x038`~`+0x047` | 0x10 | `SegCs`·`SegDs`·`SegEs`·`SegFs`·`SegGs`·`SegSs`·`EFlags` | `SegCs`·`SegSs`·`EFlags` = 트랩 프레임 값 (`SegCs=0x33`, `SegSs=0x2b`, `EFlags` = 예외 순간 값 — **TF 지우기 전 사진**), `SegDs`=`SegEs`=`SegGs`=`0x2b`, `SegFs`=`0x53` 고정 | `KeContextFromKframes +0x6d ~ +0x94`, `+0xa5`, `+0xac` |
| `+0x048`~`+0x077` | 0x30 | `Dr0`~`Dr7` | 트랩 프레임의 디버그 레지스터 (`Dr7` 에 켜진 비트 없으면 0) | `KeContextFromKframes +0x35a ~ +0x3bd` |
| `+0x078`~`+0x0f7` | 0x80 | `Rax`~`R15` | `Rax`~`R11`·`Rbp` = 트랩 프레임, `Rbx`·`Rdi`·`Rsi`·`R12`~`R15` = **예외 프레임** (`KiExceptionDispatch` 가 입구에서 따로 저장한 값), `Rsp` (`+0x98`) = `S0` | `KeContextFromKframes +0xc5 ~ +0x17f` |
| `+0x0f8` | 8 | `Rip` | 못 한 명령 자리 (`#BP` 면 `int3` 자리 = 트랩 `Rip − 1`) | `KeContextFromKframes +0x5f`, `KiDispatchException +0x707` |
| `+0x100`~`+0x2ff` | 0x200 | `FltSave` (XSAVE_FORMAT, x87·XMM) | `FXSAVE` 결과 + `XMM0~5` = 트랩 프레임, `XMM6~15` = 예외 프레임 | `KeContextFromKframes +0x26f`, `+0x27d ~ +0x33d` |
| `+0x300`~`+0x4af` | 0x1b0 | `VectorRegister` `VectorControl` `DebugControl` | **복사 목록에 없음** (스택 쓰레기) | `RtlpCopyExtendedContext` 복사 범위 밖 |
| `+0x4b0`~`+0x4cf` | 0x20 | `LastBranch*`·`LastException*` | 디버그 레지스터 묶음에 같이 복사 | `RtlpCopyExtendedContext +0x2f8 ~ +0x30f` |
| **`+0x4d0`** | **0x20** (8바이트짜리 `{Offset, Length}` 4개) | `CONTEXT_EX` | 항상 씀 (XSTATE 없어도): `+0x4d0` `All{Offset=−0x4D0 (0xfffffb30), Length=S0−C}` · `+0x4d8` `Legacy{Offset=−0x4D0, Length=0x4D0}` · `+0x4e0` `XState{Offset=A−X, Length=S0−A}` · `+0x4e8` `KernelCet{0,0}` (16바이트씩 두 번 씀) | `+0x465 ~ +0x49c` (만들기), `+0x534 ~ +0x545` (쓰기) |
| **`+0x4f0`** | 0x98 (칸 0xA0) | `EXCEPTION_RECORD` | 0x98 전부 0 으로 지운 뒤 앞 0x20 + `NumberParameters × 8` 만 복사 (`KeCopyExceptionRecord`) | `+0x4cc / ret +0x4d1`, `KeCopyExceptionRecord +0x9 ~ +0x5c` |
| **`+0x590`** | 8 | 머신 프레임 `Rip` | `CONTEXT.Rip` 와 같은 값 | `+0x4b8 ~ +0x4c3` |
| `+0x598` · `+0x5a0` | 8 · 8 | 머신 프레임 `SegCs` · `EFlags` | **안 씀** | — |
| `+0x5a8` | 8 | 머신 프레임 `Rsp` | `S0` | `+0x4b0 ~ +0x4b4` |
| `+0x5b0` | 8 | 머신 프레임 `SegSs` | **안 씀** | — |
| `+0x5b8` ~ `A − C` | 0 ~ 0xF | 빈틈 (정렬) | 안 씀 | — |
| `A − C` ~ `S0 − C` | `S0 − A` | XSAVE 꼬리 (XSTATE 있을 때만) | 처음 0x40 (XSAVE 머리) 는 `+0x4da ~ +0x502` 가 복사, 나머지는 `RtlpCopyExtendedContext → RtlpCopyXStateChunk` (`RtlpCopyExtendedContext +0x330 / ret +0x335`) | `+0x4d1 CMP [RBP+8],0x100040` |
| `S0 − C` | — | 예외 순간의 원래 `RSP` | 커널은 안 건드림 | — |

- **쓰는 차례 (모두 `__try` 안, 아래 3-6):** ① `ProbeForWrite(C, S0−C, 16)` (`+0x4ab / ret +0x4b0`) → ② 머신 프레임 `Rsp`·`Rip` (`+0x4b4 ~ +0x4c3`) → ③ 레코드 (`+0x4cc`) → ④ (XSTATE) XSAVE 머리 (`+0x4da`) → ⑤ `RtlpCopyExtendedContext(복사 방향 1, 목적=X, 설명=[RBP+0x140], 이름표, 원본 확장)` (`+0x52f / ret +0x534`) 가 CONTEXT (`C`) 와 XSTATE 꼬리를 씀 → ⑥ `CONTEXT_EX` 0x20 바이트 (`+0x534 ~ +0x545`).
- 유저 스택에 쓰는 함수: `ProbeForWrite` (`rva 0x8e8320`, 페이지마다 읽고 다시 씀) · `KeCopyExceptionRecord` (`rva 0x43e3e0`, 안쪽에서 `RtlCopyMemory`) · `RtlpCopyExtendedContext` (`rva 0x3f59b0`, 이름표 비트별로 덩어리 복사, 오류 시 `0xC000000D`/`0xC00000BB` 반환) · 머신 프레임 두 칸과 `CONTEXT_EX` 와 XSAVE 머리는 `KiDispatchException` 이 직접 `MOV`/`MOVUPS`. `KiCopy*` 는 안 쓴다.
- `ProbeForWrite(Addr, Len, Align)`: `Len=0` 이면 끝 · `Addr & (Align−1)` 이면 **`ExRaiseDatatypeMisalignment`** (`+0x67`, 코드 `0x80000002`) · `Addr+Len−1` 이 감기거나 `≥ 0x7fffffff0000` 이면 **`ExRaiseAccessViolation`** (`+0x61`, `0xC0000005`) · 아니면 페이지마다 한 바이트 읽고 다시 씀 (`+0x40 ~ +0x56`; 아직 안 올라온 스택 페이지는 여기서 `MmAccessFault` 가 올려 준다).

**3-4. `KeContextFromKframes` 가 `ContextFlags` 를 쓰는 법** (`rva 0x3ecaf0`, 1493B)

- 이름표는 **받은 버퍼의 `+0x30`** (`+0x47 MOV ESI,[RBX+0x30]`) 이다. 덩어리마다 `AND`+`CMP` 로 켜졌나 보고 (CONTROL `+0x53`, SEGMENTS `+0x99`, INTEGER `+0xb5`, XSTATE `+0x188`, KERNEL_CET `+0x244`, FLOATING_POINT `+0x259`, DEBUG `+0x35a`) 채운다. `CONTEXT_FULL` 이 아니라 `0x10001f` 라서 CONTROL·INTEGER·SEGMENTS·FLOATING_POINT·DEBUG_REGISTERS 가 모두 켜짐.
- IRQL 이 0 이면 올렸다 내림 (`+0x23 ~ +0x3a`, `+0x3c4 ~ +0x3e4`).
- XSTATE 비트가 켜지면 (`+0x188 ~ +0x192`) 유저 모드에서 온 트랩일 때 (`+0x198 TEST [SegCs],1`) `XSAVES`/`XRSTORS` 로 현재 확장 상태를 모은다 (`+0x1a2 ~ +0x225`). 줄기 밖, 이름만.

**3-5. 트랩 프레임에서 바뀌는 칸 (첫 기회 · 디버거 없음 · 유저 모드)**

디버거 없는 흐름 (줄 기준): `+0x13d` 문맥 → `+0x15e` 표시 코드 풀기(`AL=0`) → `+0x167 ~ +0x16e` 유저 갈래 → `+0x22e ~ +0x24a` (`Flags3.Minimal` 비트 0 이 꺼져 있고 `[프로세스+0x310]` WoW64 가 0 이고 `Context.SegCs & 0xfff8 != 0x20` 이면 보통 64비트 프로세스, 아니면 `+0x5d1 ~ +0x62a` WoW64·최소 프로세스 갈래 — 이름만) → `+0x250` 첫 기회 → `+0x267 / ret +0x26c KdIsThisAKdTrap` → `+0x27f ~ +0x2a1` (`KdIgnoreUmExceptions`·`[프로세스+0x308]` DebugPort·`0x80000033` 검사) → `DebugPort` 가 0 이고 `KdIgnoreUmExceptions` 도 0 이면 `+0x2be / ret +0x2c3 KdTrap` (커널 디버거가 붙어 있으면 여기서 멈출 수 있음 `[동적]`), `KdTrap` 이 0 을 돌려주면 (`+0x2c5 JZ` → `+0x79c` → `+0x3c4`) `+0x3d6 / ret +0x3db DbgkForwardException(레코드, 디버그=참, 두번째=거짓)` → `AL=0` 이면 (`+0x3dd JNZ` 안 탐) `+0x3e3` 유저 처리기로 보냄. (`DebugPort`≠0 이나 `KdIgnoreUmExceptions`≠0 이면 `+0x3bc` 로 곧장 가서 `KdIsThisAKdTrap` 결과가 참일 때만 `KdTrap`, 아니면 바로 `+0x3c4`.) `DbgkForwardException` 은 `DebugPort` 가 0 이고 `DebugException` 이 참이면 곧바로 거짓으로 끝낸다 (`DbgkForwardException +0x161 TEST RSI,RSI` · `+0x169 JNZ 0x1408bf6e8` = `+0x268 XOR AL,AL`).

| 트랩 프레임 칸 | 새 값 | 줄 | 원래 값 |
|---|---|---|---|
| `EFlags` (`+0x178`) | `TF` (비트 8) 지움 | `+0x3e4 BTR [R12+0x178],8` (`+0x3e3 CLI` 뒤) | 나머지 비트 그대로. 트랩 프레임의 `EFlags` 는 **TF 지운 값**이고, 유저 `CONTEXT.EFlags` (`C+0x44`) 는 지우기 전에 찍은 사진 |
| **`Rsp` (`+0x180`)** | **`C`** (= `X − 0x4D0`) | `+0x571 LEA R10,[RDI-0x4d0]` · `+0x578 MOV [R12+0x180],R10` | `S0` |
| **`SegCs` (`+0x170`)** | **`0x33`** | `+0x580 LEA EAX,[RBX+0x32]` · `+0x583 MOV [R12+0x170],AX` (`RBX=1`) | 이미 `0x33` (64비트 유저) |
| **`Rip` (`+0x168`)** | **`PsNtdllExports+0x20`** = `KiUserExceptionDispatcher` (`[0x140fc74d0]`, ntdll rva `0x164e10`, run59) | `+0x58c MOV RAX,[PsNtdllExports+0x20]` · `+0x593 MOV [R12+0x168],RAX` | 못 한 명령 자리 (문맥·머신 프레임에 이미 복사됨) |
| `SegSs`·나머지 레지스터·`Rax`~`R11`·`XMM0~5`·`Rbp` | **그대로** | — | 입구가 만든 것 |

- 이어서 (역시 `__try` 안): `+0x56c / ret +0x571 KePopulateContinuationContext(원래 Rip)` — **CET 스레드 (`KTHREAD +0x74` 비트 20) 일 때만** 유저 그림자 스택에 원래 `Rip` 를 밀어 넣는다 (`KePopulateContinuationContext +0xc / +0x13`, 비 CET 는 `+0x15 RET`). `+0x59e / ret +0x5a3 KiSetupForInstrumentationReturn` — 프로세스에 계측 콜백이 설정돼 있을 때만 `Rip` 를 콜백으로 바꾸고 원래 `Rip` 를 `R10` 에 둔다 (`KiSetupForInstrumentationReturn +0x0 ~ +0x33`, `[프로세스+0x168]` 가 0 이면 그냥 끝). test.exe 는 둘 다 안 탄다고 보지만 계측 콜백 설정 여부는 `[동적]`.
- 그다음 `+0x649 [RBP+0xc]=0; JMP +0x1c7` — **`KeContextToKframes` 를 건너뛴다** (그건 `+0x1b8` 의 "처리됨" 길에서만). 그래서 레지스터는 바뀐 트랩 프레임 그대로 `KiExceptionDispatch +0x145` 로 돌아가 `+0x145 ~ +0x19d` 에서 예외 프레임 복원 → `+0x1b4 TEST [RBP+0xf0],1` 유저 → `+0x1e9 KiInitiateUserApc` 루프(`+0x1c1 ~ +0x1f8`) → `+0x22a KiRestoreSetContextState`(`[스레드]` 비트 27) → `+0x262 KiRestoreDebugRegisterState` → 조각 `FUN_1406c2b18` (= `+0x418`): 조각 `+0x82` (= `+0x49a`) `JMP KiKernelExit` (KVA 그림자 켜짐) 또는 `+0x49f ~ +0x4b4` (`+0x4b1 SWAPGS`, `+0x4b4 IRETQ`). **`SYSRET` 가 아니라 `IRETQ`** 로 유저로 간다.

**`KiUserExceptionDispatcher` 가 시작할 때 (ntdll `+0x0 ~ +0x2e` 첫 줄만 봄):**
- `RSP` = `C` = **`CONTEXT` 시작** (`+0x1c MOV RCX,RSP` · `+0x26 MOV RDX,RSP` → `ContextRecord=RSP`), `ExceptionRecord = RSP + 0x4f0` (`+0x1f ADD RCX,0x4f0`) — 위 표와 같다. `RCX`/`RDX` 는 **입력이 아니다** (바로 덮어씀). 입력은 `RSP` 하나, `RIP` = 진입점, `CS`=0x33. 그 밖의 레지스터는 예외 순간 값 그대로 (그 뒤에 읽지 않음).
- 첫 줄 `CLD`, `Wow64PrepareForException` 이 0 이 아니면 부름 (`+0x1`~`+0x1a`). 나머지는 Q148b.

**3-6. 쓰기 실패 (스택 모자람 · 잘못된 RSP) 때 갈래**

- 유저 스택에 쓰는 구간이 **`__try`** 안: `.pdata` scope table (내가 읽음) = `[+0x41a, +0x5d1)` 과 `[+0x62a, +0x652)`, 필터 = `KiDispatchException$filt$0` (rva `0x6cfba0`: 새로 난 예외 레코드를 `[RBP+0xa0]` 에 `KeCopyExceptionRecord` 로 복사하고 **1 반환 = 무조건 처리기 실행**), **처리기 = `+0x652`**.
- 처리기 (`.asm` 의 "읽지 않은 틈" 을 objdump 로 풂, `+0x652 ~ +0x6cf`): `[RBP+0xc]` 가 켜져 있으면 `SpecialApcDisable` (`[스레드+0x1e6]`) 를 되돌림 (0 이 되고 APC 가 기다리면 `KiCheckForKernelApcDelivery`) → **새 코드가 `0xC00000FD` (STATUS_STACK_OVERFLOW) 면** `KeCopyExceptionRecord` 로 **원래 레코드를 STATUS_STACK_OVERFLOW 레코드로 덮고** (`ExceptionAddress` 는 원래 것 유지), `R15` 를 원래 `S0` 로 되돌려 **`+0x419` 로 한 번 더 시도** (유저 스택을 다시 계산해 씀) · **그 밖 코드 (`0xC0000005` 접근 위반, `0x80000002` 정렬, `0x80000001` 가드 페이지 등)** 면 `EDI=1` 로 **`+0x7a5` 두 번째 기회 갈래로 곧장 감** (유저 처리기 기회 없음).
- 즉 **상태 코드가 돌아가는 게 아니라**, 원래 예외의 `ExceptionCode` 로 프로세스가 끝난다 (질문 4). 잘못된 `RSP` (`C` 가 감기거나 ≥ `0x7fffffff0000`, 또는 16 정렬 아님) = `ProbeForWrite` 가 `ExRaiseAccessViolation`/`ExRaiseDatatypeMisalignment` → 이 처리기 → 두 번째 기회.
- 스택 넘침의 되풀이는 여기서 막지 않는다 (`+0x419` 로 돌아가 같은 계산) → 두 번째 시도에서 또 `0xC00000FD` 면 또 처리기 `[동적]`.

**지금 우리 (PE_emulator):** 이 배치를 그대로 쌓으면 `KiUserExceptionDispatcher` 가 읽는 모양 (`RSP`=CONTEXT, `RSP+0x4f0`=EXCEPTION_RECORD, `RSP+0x590`=머신 프레임) 과 맞는다. XSAVE 를 흉내 내지 않고 `ContextFlags=0x10001f` 로 쌓는 길(= 확장 꼬리 없이 `CONTEXT_EX` 만 `XState.Length=0`) 도 커널 코드에 이미 있다 (`KeFeatureBits` 비트 꺼짐 갈래). 단 **`CONTEXT_EX` 0x20 바이트는 XSTATE 가 없어도 쓴다** — 읽는 쪽 (`RtlpGetExtendedContext…`) 이 이름표 비트 `0x40` 만 보고 건너뛰는지는 질문 Q148b 몫.

### 4. 두 번째 기회 (커널 쪽)

- **`NtRaiseException` 의 모양:** 손으로 쓴 asm. 인자 `RCX`=레코드, `RDX`=문맥, **`R8B`=첫 기회**. `+0x15 ~ +0x17` 로 트랩 프레임 `Rax` 칸 (`[RBP-0x50]`) 을 0 으로 만들고 (run77, 성공 `STATUS_SUCCESS` 로 나가도록), 비휘발 레지스터·`XMM6~15` 를 예외 프레임 (`RSP` 쪽, `+0x2a ~ +0x76`) 에 저장, `+0x85 MOV [RSP+0x20],R8B` 후 **`+0x91 / ret +0x96 KiRaiseException(레코드, 문맥, 예외 프레임, 트랩 프레임, 첫 기회)`**. 반환 `EAX != 0` 이면 (`+0x98 JNZ +0x483`) 오류 상태로 나감, `0` 이면 `+0x9e ~ +0xc7` (유저 호출이면 `+0xa5 JNZ +0xc7` 로 건너뜀) 뒤 레지스터 복원 → `IRETQ` 길 (`+0x11f` 부터 `KiInitiateUserApc +0x16b / ret +0x170` 루프 등 `KiExceptionDispatch` 와 같은 나가기, `SYSRET` 아님).
- **`KiRaiseException`** (rva `0x3e9410`): 유저 모드 호출이면 (`+0x4a SIL=[스레드+0x232]` 이전 모드) 레코드의 `NumberParameters` 를 읽어 (`+0x71`, `0x7fffffff0000` 으로 눌러서 잘못된 주소면 읽다가 터지게) **15 보다 크면 `STATUS_INVALID_PARAMETER 0xC000000D`** (`+0x79 / +0x7c / +0x9c`), 아니면 `0x20 + 8×개수` 만 커널 스택으로 복사 (`+0xb3 / ret +0xb8 RtlCopyMemory`). 그다음 **`+0xd8 / ret +0xdd KyRaiseException(레코드, 문맥, 예외 프레임, 트랩 프레임)`** — 유저가 준 `CONTEXT` 를 읽고(`RtlpSanitizeContextFlags +0x89`, `RtlGetExtendedContextLength +0x9f`, `RtlInitializeExtendedContext +0xdc`, `RtlpReadExtendedContext +0x104`) 검증하고 (`RtlpValidateContextXStateDisabledFeatures +0x15f`), 트랩 프레임에 **`ExceptionActive=1`** (`+0x16f MOV [RAX+0x2b],BL`) 와 함께 `KeContextToKframes` (`+0x18d`) 로 **써 넣는다**. 이어서 `+0xe6 BTR [레코드],0x1c` (코드 비트 28 지움), `+0xfe / ret +0x103 KiDispatchException(레코드, 예외 프레임, 트랩 프레임, 이전 모드=SIL, 첫 기회=스택 인자)`.
- **둘째 기회로 들어가면** (`[RBP+0x1d0]==0`, 유저 모드): `+0x95 CMP [RBP+0x1d0],0` → 첫 기회 갈래(`+0x1e8`)를 건너뛰고 `+0xa2` 에서 문맥을 만든 뒤 (`KiPreprocessFault` 까지 같음) `+0x250` (`CMP [RBP+0x1d0],0; JZ +0x7a5`) 로 **`+0x7a5`** 로 간다. 거기서 순서대로:
  1. **`+0x7ae / ret +0x7b3 DbgkForwardException(레코드, 디버그=참, 두번째=참)`** — 디버거 포트가 없으면 `+0x268` 에서 거짓. (`DbgkForwardException +0x59 / +0x61 ~ +0x8b` : 두번째 기회면 먼저 프로세스 `Flags3` 에 `Crashed`(비트 2) 를 켜고 `PspRecordCrashedProcessIntoBlackbox` 와 `PsSetProcessTelemetryAppState(…,4)` 를 부른다.)
  2. **`+0x7c3 / ret +0x7c8 DbgkForwardException(레코드, 디버그=거짓, 두번째=참)`** — 예외 포트 (`[프로세스+0x240]` `ExceptionPortData`) 가 0 이면 `+0x3a6` → `+0x386` 에서 `DbgkpSendErrorMessage` (`DbgkForwardException +0x39c / ret +0x3a1`) 를 불러 보내고, 그 반환이 ≥ 0 이면 "처리됨"(참) 으로 끝낸다. 보내기가 실패하면 거짓.
  3. 둘 다 거짓이면 **`+0x7d0 MOV EDX,[R14]` (= 원래 `ExceptionCode`) · `+0x7d3 OR RCX,-1` (= 현재 프로세스) · `+0x7d7 / ret +0x7dc ZwTerminateProcess`**.
- **끝내는 상태 코드 = 예외 코드 그대로** (`EDX = [레코드]`). 〈`KiFastFailDispatch` 면 `0xC0000409`〉. `ZwTerminateProcess(-1, …)` 은 run79 의 `NtTerminateProcess` 길 (자기 프로세스면 `PspTerminateAllThreads +0x243` 뒤 자기도 `PspExitThread +0x161`) 로 이어지고 `[정적, run79]` 따라서 `+0x7dc` 로 돌아오는 건 실패 때만이다.
- 종료 상태로 넘기는 값에 다른 변환은 없다 (`EDX` = 레코드의 `ExceptionCode` 그대로; 표시 코드는 이미 `KiPreprocessFault` 가 풀어 둔 뒤).

**지금 우리 (PE_emulator):** 은퇴 뒤 진짜 ntdll 이 `NtRaiseException(…, FALSE)` 를 부르면 엔진은 (디버거·WER 가 없으니) `ZwTerminateProcess(-1, ExceptionCode)` 로 가면 된다. 첫 기회 (`FirstChance=TRUE`) 로 다시 오면 (`RtlRaiseException`) 유저 문맥을 트랩 프레임에 써 넣고 (`KyRaiseException` 이 하는 일) 질문 3 의 배치를 다시 만든다.

### 5. `__fastfail` (`int 0x29`)

- **`KiRaiseSecurityCheckFailure`** (rva `0x6c05c0`) → **`KiFastFailDispatch`** (rva `0x6c2c40`): 입구 첫 줄 `+0x0 SUB qword [RSP],2` 로 **되돌아올 `Rip` 를 `int 0x29` 자기 자리로** 만들고, `CS` 가 `0x23` (WoW64) 면 `Rip` 위쪽 32비트를 지움 (`+0x5 ~ +0xd`). 끝 줄: 조각 `FUN_1406c0844+0xce MOV ECX,0xc0000409` (`+0x352`) · `EDX=1` · `R8=Rip` · `R9=[RBP-0x48]` (트랩 프레임에 저장해 둔 `RCX` = 실패 코드, `+0x34e`) → **`frag+0xdf` / `+0xe4` = `+0x363` / `+0x368` (rva `0x6c0923` / `0x6c0928`)** 에서 `KiFastFailDispatch` 를 부른다.
- `KiFastFailDispatch` (손으로 쓴 asm): 레코드를 `KiExceptionDispatch` 와 같은 칸에 채우되 **`ExceptionFlags=1` (`+0x6b`)**, `[0]`=`R9`, 개수=`EDX`=1. 갈림 `+0x89 ~ +0x98`: 이전 모드가 **커널이면** `+0xad / ret +0xb2 KiBugCheckDispatch(ECX=0x139, KERNEL_SECURITY_CHECK_FAILURE)`, **유저면** 건너뛰고 `+0xb2 MOV byte [RSP+0x20],0` (**첫 기회 = 0**) → **`+0xc1 / ret +0xc6 KiDispatchException`**.
- 결과: **유저 처리기(SEH 등)에 한 번도 안 보내고 바로 둘째 기회 갈래(질문 4 의 `+0x7a5`)** → `DbgkForwardException` 둘 → 안 처리되면 `ZwTerminateProcess(-1, 0xC0000409)`. `ExceptionInformation[0]` = 실패 코드 (`RCX`), 개수 1. `KiUserExceptionDispatcher` 에 닿지 않는다.
- 같은 길을 쓰는 입구: `KiControlProtectionFault` (유저면 `ECX=0xC0000409`, `R9=0x39`), `KiBoundFault` 의 한 갈래 (`R9=0x1c`) 도 `KiFastFailDispatch` 를 부름 (이름만).

**지금 우리 (PE_emulator):** `int 0x29` 는 인터럽트 훅으로 받아 `Rcx` 를 실패 코드로 해서 곧바로 프로세스 종료 (`0xC0000409`) 로 보내면 진짜와 같다 (WER 없음 가정). 유저 `SEH` 를 기다릴 필요 없음.

### 6. (덤) 메모리 syscall 이 유저 포인터를 읽다 실패할 때

- **유저 예외가 아니라 syscall 이 상태 코드를 돌려주는 길이 기본이다.** 실패는 **커널 모드 예외** (`KiPageFault` 의 이전 모드 = 커널, `SegCs & 1 = 0`) 라서 `KiDispatchException` 의 **커널 갈래**: `+0x16e` 에서 유저 갈래로 안 가고 `+0x174 ~ +0x19c KdTrap` → `+0x367 ~ +0x372 RtlDispatchException(레코드, 문맥)` 가 **커널 프레임들의 SEH** 를 찾고, 있으면 그 `__except` 로 가서 syscall 이 돌아온다. **없으면** `+0x37a ~ +0x395 KdTrap` 뒤 **`+0x3b1 ECX=0x1e` · `+0x3b6 / ret +0x3bb KeBugCheckEx` (KMODE_EXCEPTION_NOT_HANDLED)** — 어느 쪽도 유저 처리기는 안 거친다.
- 한 예: `NtAllocateVirtualMemory` (rva `0x904f90`) 의 `__try` 구간 `[+0x79, +0xc2)`·`[+0x257, +0x261)` 필터 (`$filt$0` rva `0xb063a0`, 내가 `.pdata` 로 읽고 objdump 로 풂; `$filt$1` rva `0xb063dc` 은 첫 줄들만 봤고 같은 모양 `[추측]`) 는 `Thread->PreviousMode (+0x232) != 0` 이면 1 (처리기 실행) 반환 — 즉 **유저에서 부른 syscall 안에서 난 예외만 자기 처리기가 잡는다.** 처리기 본문이 상태를 어떻게 돌려주는지는 이번에 안 읽었다 `[동적/후속]`.
- **`Process.cpp:1953` 주석은 다른 일의 이야기다.** 거기 주석 "진짜는 SEH `+0x2ab` → 유저 예외 `KiDispatchException` [정적] `PspInitializeThunkContext.asm` `+0x37b`, `+0x3c3`" 는 **프로세스 시작 때 커널이 유저 스택에 문맥을 쓰는 `PspInitializeThunkContext`** (syscall 아님) 이다. 줄 확인:
  - `.pdata` scope table: `[+0x22f, +0x2ab)` `__try`, 필터 값이 상수 **1**(항상 처리), **처리기 `+0x2ab`** (`.asm` 의 "읽지 않은 틈 `+0x2ab ~ +0x2b9`") — 주석의 `+0x2ab` 맞다.
  - `+0x2fa TEST EBX,EBX` · `+0x2fc JS 0x140910a63` (= `+0x37b`) → 레코드 만들기 (`+0x37b ~ +0x391`: 코드 `EBX`, 플래그 0, 주소 `PsNtdllExports+0x8`) → **`+0x3c3 / ret +0x3c8 KiDispatchException(…, 이전 모드 R14B, 첫 기회 = 0 (`+0x3b4 MOV byte [RSP+0x20],0`))`**.
  - **정정 한 가지:** 이건 "유저 예외" 이긴 하나 **첫 기회가 0 이라 유저 처리기 기회 없이 둘째 기회 갈래 (질문 4) = 프로세스 종료**이다. `Process.cpp` 의 "에뮬레이터 규칙: 멈춘다" 는 결과가 같다 (종료 코드는 `EBX` = `RtlInitializeExtendedContext2` (`+0x26e`) 또는 `RtlCopyContext` (`+0x285`) 가 돌려준 상태 값).
- 참고: `KiSystemServiceUser +0xa1 / ret +0xa6` 도 `KiExceptionDispatch` 를 부르지만 `ECX=0xC000001C` (STATUS_INVALID_SYSTEM_SERVICE) 라 포인터 읽기 실패와 무관하다 (이름만).

**지금 우리 (PE_emulator):** 메모리 syscall 이 잘못된 포인터에 `STATUS_ACCESS_VIOLATION` 돌려주는 것은 위 기본 길과 맞다 (다만 코드는 `AV` 만이 아니다: 정렬이면 `0x80000002`, 페이지 읽기 실패면 `0xC0000006`). `Process.cpp:1953` 주석은 위처럼 "첫 기회 0 인 유저 예외 = 종료" 로 고치면 정확하다.

## nt 엔진이 흉내 낼 최소 규칙 (CPU 예외 → 유저 스택 프레임 → `KiUserExceptionDispatcher`)

CPU 예외가 나서 은퇴 뒤 Unicorn 에서 **유저 모드** 코드가 멈췄을 때, 디버거·WER 없음, 64비트 프로세스, 비 CET, 계측 콜백 없음:

1. **코드·주소·인자 정하기** (질문 2 표): `#PF`=`0xC0000005`, 개수 2, `[0]` = 읽기 0 / 쓰기 1 / 실행 8, `[1]`=주소; 특권 명령 `0xC0000096`(개수 0); `#UD` `0xC000001D`; `#DE` `0xC0000094`/`0xC0000095`; `int3` `0x80000003`, 주소 = `int3` 자리, 개수 1, `[0]=0`, **문맥 `Rip` 도 `int3` 자리**; `ExceptionFlags=0`.
2. **문맥 만들기:** 순간의 레지스터로 `CONTEXT` (0x4d0): `ContextFlags=0x10001f`, `SegCs=0x33`, `SegSs=0x2b`, `SegDs=SegEs=SegGs=0x2b`, `SegFs=0x53`, `EFlags`/`Rsp`/`Rip` = 순간 값 (`int3` 만 `Rip−1`), `MxCsr`·`XMM`·`FltSave` 채움. `P1Home~P6Home` 는 안 씀.
3. **배치 계산:** `S0=RSP`. `M=(S0−0x28)&~0xF`, `E=M−0xA0`, `X=E−0x20`, `C=X−0x4D0`.
4. **쓸 수 있나 확인:** `C` 가 16 정렬이고 `[C, S0)` 의 끝 주소 `< 0x7fffffff0000` 이고 쓰기 가능 (아직 안 올라온 스택 페이지는 올려 줌. 스택 넘침 `0xC00000FD` 로 실패하면 레코드를 그 코드로 바꿔 3 단계부터 한 번 더, 그 밖의 실패는 **둘째 기회로 곧장**: 7 단계).
5. **유저 스택에 쓰기** (차례 상관없음, 모두 `C` 기준): `C`(0x4d0 CONTEXT) · `C+0x4d0`(0x20 `CONTEXT_EX`: `{−0x4D0, S0−C}, {−0x4D0, 0x4D0}, {S0−X, 0}, {0, 0}`) · `C+0x4f0`(레코드: 0x98 을 0 으로 채우고 앞 0x20 + `개수×8`) · `C+0x590` 에 `Rip` (= 문맥 `Rip`), `C+0x5a8` 에 `Rsp` (= `S0`).
6. **레지스터 바꾸기:** `RSP=C`, `RIP=ntdll!KiUserExceptionDispatcher` (`PsNtdllExports+0x20`, ntdll rva `0x164e10`), `CS=0x33`, `EFLAGS = 순간 값 & ~0x100` (TF 지움). **다른 레지스터는 그대로.** (`RCX`/`RDX` 는 필요 없음.) 그 뒤 Unicorn 을 이어 돌린다.
7. **둘째 기회 (`NtRaiseException(…, FALSE)`·`__fastfail`·쓰기 실패):** 유저 쪽으로 안 보내고 프로세스 종료 `ZwTerminateProcess(-1, ExceptionCode)` (run79).
8. **`NtRaiseException(레코드, 문맥, TRUE)`:** `NumberParameters ≤ 15` 아니면 `0xC000000D`, 코드 비트 28 지우고, 유저 문맥을 레지스터(트랩 프레임)로 읽어 들인 뒤 1~6 단계를 새로 한다.
9. 안 흉내 내도 되는 것: 디버거 갈래, `KdTrap`, WoW64, Pico, CET 그림자 스택, 계측 콜백, `KiPreprocessFault` 의 ATL thunk·XSTATE 켜기·보안 PCI.

## 동적분석으로 확인해야 할 것 (Q104 에 붙임, 이번 묶음엔 VM 안 함)

1. 실제 `ContextFlags` 가 `0x10001f` 인지 `0x10005f` 인지, 그리고 `XState.Length` / 꼬리 크기 (CPU·`KeFeatureBits`·`KUSER_SHARED_DATA+0x3d8` 에 달림). 멈춤점: `nt!KiDispatchException+0x142` 직후 `dd @rbp+4 l1` (`[RBP+4]`), 또는 `ntdll!KiUserExceptionDispatcher` 에서 `dd @rsp+0x30 l1`.
2. 새 `RSP` 가 정말 `((A−0x28)&~0xF)−0x590` 인지, `RSP+0x590`/`+0x5a8` 값이 `Rip`/`S0` 인지. 멈춤점: `nt!KiDispatchException+0x5a3` (`dq @r12+0x168 l1`·`+0x180`·`dw @r12+0x170`), 그리고 `ntdll!KiUserExceptionDispatcher` 입구에서 `dq @rsp+0x590 l1`, `dq @rsp+0x5a8 l1`.
3. 표시 코드 `0x10000004` 가 `0xC0000005` 로 바뀌는 시점: `nt!KiDispatchException+0x163` 전후 `dd poi(@r14) l1`.
4. `#GP` 의 `[0]=0`, `[1]=-1`, `#DE` 의 `0xC0000095` 갈래, `int3` 의 `CONTEXT.Rip` — test.exe 용 작은 예외 발생 프로그램이 필요 (이 저장소 `sample/` 에는 없음).
5. 둘째 기회에서 `DbgkForwardException(…FALSE,TRUE)` 가 참을 돌려주는지 (WER 있는 VM): `nt!KiDispatchException+0x7c8` 뒤 `al`. 거짓이면 `+0x7dc` 에서 종료 상태 확인. 그리고 커널 디버거(KDNET) 가 붙어 있어서 `+0x2c3 KdTrap` 이 유저 예외를 가로채는지 (`KdIgnoreUmExceptions` 값).
6. 스택 넘침 되풀이 (`+0x652` 처리기 → `+0x419` 재시도) 가 끝나는지.
7. 질문 6 의 일반 syscall 처리기 본문 (상태를 `RAX` 로 돌려주는 줄).
8. 이 프로세스에서 계측 콜백(`KiSetupForInstrumentationReturn`)·CET(`KePopulateContinuationContext`) 이 정말 안 타는지.

추천 멈춤점 (모든 프로세스에 걸리니 test.exe 조건 필요): `nt!KiExceptionDispatch+0x140` (인자 `RCX`=레코드), `nt!KiDispatchException+0x163`, `+0x4b0`, `+0x571`, `+0x5a3`, `+0x7c8`, `+0x7dc`; 유저 쪽 `ntdll!KiUserExceptionDispatcher`.

## 판정

**분석성공.** 요청서 여섯 질문 모두 줄 근거로 답함 (질문 3 표 포함). 정적으로 못 정한 것은 위 "동적분석으로 확인해야 할 것" 에 모아 두었다. 알려진 사실 대조 일치, 스크립트 문제 없음 (참고 2건).

dast 요청 필요: 위 1~8 중 1·2·4·5 가 가장 중요 (test.exe 에서 예외를 일부러 내는 작은 프로그램 + 위 멈춤점).

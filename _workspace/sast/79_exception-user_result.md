# 79 exception-user — 정적 분석 결과

- 요청서: `_workspace/sast/79_exception-user.md` (Q148b, PE_emulator 요청 26 의 유저 쪽 — 4·5·7). 앞 바퀴 `_workspace/sast/78_exception-kernel_result.md` 먼저 읽음 (진입 때 `RSP` = CONTEXT, `RSP+0x4f0` = EXCEPTION_RECORD, `RSP+0x590` = 머신 프레임 — 이번에 asm 으로 다시 맞춤).
- 모듈: ntdll (`ms/bin/.../System32/ntdll.dll`, PDB guid_age `C3093720177DA6851519DCFFE8DF53FE1`), kernelbase (`KernelBase.dll`, `72CCE506944DE0267C172AD64287F9BD1`). 두 값 모두 앞 결과서 (ntdll: sast/14·16·17, kernelbase: sast/08·09·62) 와 같다 = 같은 빌드. ntoskrnl 은 안 씀. 보조로 `apisetschema.dll` (같은 이미지) 한 군데와 `kernel32` 의 `_index.tsv` 한 줄씩만 봄.
- 분석: **다시 안 함** (ntdll·kernelbase 둘 다 `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 있음, 도는 Ghidra 작업 없음).
- 지도 (새로 만듦, `ms/disassemble/*/_map/`):
  - ntdll `KiUserExceptionDispatcher` — 함수 91개, 상태 `{helper=17, expanded=63, syscall=11}`. 끊긴 끝 3종 (`indirect` = `KiUserExceptionDispatcher +0x1a` 의 `Wow64PrepareForException`, `indirect-cfg` = VEH 핸들러 호출 · 동적 함수표 콜백 · 힙, `cfg-check`).
  - kernelbase `UnhandledExceptionFilter` — 199개 (`external` 236개 = import 호출).
  - ntdll `__C_specific_handler` — 79개 (`RtlUnwindEx` 가 어디서 `RtlRestoreContext`/`NtRaiseException` 으로 끝나는지 보려고, 아래 "넓힌 이유").
- 뽑은 함수 (전부 `export`, **SCRIPT ERROR 없음**; 로그는 `ms/ghidra/*sast79*.stdout`):
  - 요청서 대로: ntdll `KiUserExceptionDispatcher`·`RtlDispatchException`·`RtlRaiseException` depth 1 (→ 26개), `RtlpCallVectoredHandlers`·`RtlUserThreadStart`(+`$filt$0`)·`RtlUnhandledExceptionFilter`·`…2` depth 0 (5개), kernelbase `UnhandledExceptionFilter`·`RaiseException`·`RaiseFailFastException` depth 1 (22개).
  - **넓힌 것과 이유** (모두 depth 0, 지도로 길 찾은 뒤 필요한 함수만):
    - `__C_specific_handler`·`RtlUnwindEx` — SEH `__except` 가 받았을 때 어디서 끝나는지 (`NtContinue` 인지 아닌지) 가 요청 26 에서 가장 흔한 길이라서.
    - `RtlCaptureContext2`·`RtlCaptureContext` — 위 끝맺음의 `ContextFlags` 값을 정하는 곳.
    - `RtlRaiseNoncontinuableException`·`RtlpAddVectoredHandler`·`RtlKnownExceptionFilter`·`RtlIsAnyDebuggerPresent`·`RtlReportException` — 질문 1·2·4 의 갈림 줄.
    - `RtlGuardCheckExceptionHandler`·`RtlpUnwindPrologue`·`RtlpIsContinuationContextMachineFrameEntry` — 질문 2(c) 와 조정자 덧붙임 2.
  - `refs`: `LdrpVectorHandlerList`·`RtlAddVectoredExceptionHandler`·`RtlAddVectoredContinueHandler`·`Wow64PrepareForException`·`RtlpContinuationContextMachineFrameEntries`·`RtlpIsContinuationContextMachineFrameEntry` (`ms/disassemble/ntdll/_refs/`).
- 읽기만 한 것 (이미 있던 것): kernelbase `OutputDebugStringA.asm` + `OutputDebugStringA.handler.objdump.txt` (sast/66, 조정자 덧붙임 3).
- 내가 파이썬으로 직접 읽은 것 (임시 스크립트, 레포 밖): `.pdata` → `UNWIND_INFO` → scope table (`RtlUserThreadStart`·`OutputDebugStringA`), 핸들러 조각 원본 바이트 (`RtlpExecuteHandlerForException` 의 예외 처리기 `0x1654c0`, `RtlpCaptureContext2` 의 뒷부분, `RtlDecodePointer`, `RtlpVirtualPopShadowStack`, kernelbase delay-load 스텁), `apisetschema.dll` 의 `.apiset` 표.
- **버전 확인 (알려진 사실 대조): 일치** — `_calls.tsv` 로 확인:
  - `LdrInitializeThunk +0x9 / ret +0xe → LdrpInitialize`, `LdrpInitializeInternal ret +0x5a → _LdrpInitialize`, `_LdrpInitialize ret +0x16c → LdrpInitializeProcess`, `LdrpInitializeProcess ret +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode ret +0x16b / +0x19c`, `LdrpLoadDll ret +0x100` 모두 스킬 표와 같음.
  - 요청서가 준 rva·크기도 모두 맞음: `KiUserExceptionDispatcher 0x164e10 (92B)`, `RtlDispatchException 0xcb730 (2197B)`, `RtlpCallVectoredHandlers 0xcc0c0 (983B)`, `RtlRaiseException 0xf700 (685B)`, `RtlUserThreadStart 0xacac0`, `RtlUnhandledExceptionFilter2 0x122300`, kernelbase `UnhandledExceptionFilter 0x10ed90 (1402B)`·`RaiseException 0xc4140`·`RaiseFailFastException 0x10a020`.
  - sast/78 의 `KiUserExceptionDispatcher` 진입 값 (`+0x1c MOV RCX,RSP` · `+0x1f ADD RCX,0x4f0` · `+0x26 MOV RDX,RSP`) 이 이 `.asm` 과 같음. `UNWIND_INFO` 끝의 `ALLOC_LARGE 0xb2 × 8 = 0x590` + `PUSH_MACHFRAME (오류 코드 없음)` 도 내가 다시 읽어 맞음.

## 스크립트 문제

막힌 것은 없다. 참고만 (고치지 않음):

1. **지도가 delay-load 를 `external` (import) 로 적는다.** kernelbase `UnhandledExceptionFilter +0xb8` (`CheckForReadOnlyResourceFilter`)·`+0x345` (`BasepReportFault`) 는 import 줄처럼 나오지만 실제는 `DelayLoad_*` 스텁 (`0x129cad`/`0x129cbf`: `lea rax,[IAT 칸]; jmp 0x129c2e`) 이 가리키는 **첫 호출 때 풀리는 칸** (apiset → 호스트 DLL) 이다. 지도에서 `delay-load` 로 따로 표시하면 "이 호출이 로더 일을 부를 수 있다" 를 놓치지 않는다.
2. **`RtlpExecuteHandlerForException` 의 예외 처리기 (`0x1654c0`) 는 함수로 안 잡힌다** (`pdb_aliases --where` 도 "없음"). `UNWIND_INFO` 의 핸들러라서 호출로 안 닿는다. 바이트를 objdump 로 직접 풂 (질문 2(b)).
3. 같은 이름이 두 함수: `RtlRestoreContext` 가 `0xa59f0` (PDB 별칭 `RtlGuardRestoreContext`) 과 `0x123e20` 둘. 스크립트는 파일 이름을 `RtlRestoreContext__a59f0.asm` / `RtlRestoreContext.asm` 으로 갈라 줬고, 이 결과서는 rva 로 구분한다.
4. `UnhandledExceptionFilter.asm` 의 "읽지 않은 틈" 3곳 (`+0x93~+0xab`, `+0x36e~+0x37f`, `+0x4a1~+0x4b2`) 은 전부 `INT3` 바로 뒤라 이번 질문과 상관없다고 보고 안 읽음.

## 먼저 알아 둘 것 (모든 질문에 걸림)

- 읽는 법: `+0x…` 는 `.asm` 줄 앞 오프셋 (함수 시작 기준), `call` 은 `call / ret` 를 같이 적음, 점프 목적지는 "목적지 − 함수 시작". 이름 앞 `PDB:` = PDB 이름.
- **`[관찰]` 값 (run44 K1 · sast/55)**: `LdrSystemDllInitBlock +0x9c = 0`, `+0xa0 = 0`, **`+0xa8 = 0`**, `+0xb0 = 0x02002002`, **`+0xb8 = 0`**. 아래의 "CET·가드 갈래" (`(+0xa8 >> 0x3c) & 3 == 1`, `BT +0xa8, 0x3c / 0xc`, `+0xb8 != 0`) 는 test.exe 에서 **전부 안 탄다**. (다른 exe 도 같은지는 `[추측]`; 커널이 같은 규칙으로 채우면 같음.)
- **문맥 이름표 (`ContextFlags`) 가 어디서 어떤 값이 되나 — 이게 "`NtContinue` 를 부르나" 를 가른다:**

  | 어디서 만든 문맥 | `ContextFlags` | 근거 |
  |---|---|---|
  | 커널이 유저 스택에 쌓은 것 (sast/78) | `0x10001f` (또는 `0x10005f`) — `DEBUG_REGISTERS` 비트 `0x10` 포함 | sast/78 질문 3 |
  | `RtlRaiseException` 이 잡은 것 | 처음 `0x10000b` (KUSD `+0x3d8` XState 가 켜져 있으면 `0x10004b`), **잡은 뒤 `0x10000f` (`0x10004f`)** — `RtlpCaptureContext2` 가 `RtlCaptureContext2 +0x3c` 의 뒷부분으로 점프해서 거기서 `+0xc0 AND [ctx+0x30],0x100040` → `+0xf3 OR [ctx+0x30],0x10000f` | `RtlRaiseException.asm +0x64 / +0xdc / +0xe6`, `RtlpCaptureContext2.asm +0x6c ~ +0x73`, `RtlCaptureContext2.asm +0xc0 ~ +0xf3` |
  | `RtlUnwindEx` 이 잡은 것 (`__except` 로 되감을 때) | `RtlCaptureContext2` 가 `0x10000f` 로 만듦 (`RtlUnwindEx +0x892 / ret +0x897`) | 같은 `RtlCaptureContext2.asm +0xf3` |
  | `RtlDispatchException` 의 지역 사본 | `RtlpCopyContext` 가 `원본 & 0x10000f` 로 맞춤 (`RtlpCopyContext +0x31 ~ +0x3a`) → `0x10000f` | |

- **`RtlRestoreContext` (`0x123e20`) 의 갈림** — 이 한 곳이 "유저 안에서 끝나나, `NtContinue` 인가" 를 정한다:
  - `+0xe TEST RDX,RDX` / `+0x11 JZ +0x150` (레코드 NULL) 또는 코드가 `0x80000029`·`0x80000026` 이 아니면 (`+0x17 CMP` / `+0x1d JNZ +0x29`, `+0x29 CMP` / `+0x2f JNZ +0x150`) **`+0x150`** 으로.
  - `+0x15b BT [LdrSystemDllInitBlock+0xa8],0x3c` · `+0x16d BT …,0xc` 둘 중 하나라도 켜짐 → `+0x183`. test.exe 는 둘 다 꺼짐.
  - **`+0x174 MOV EAX,[RCX+0x30]` · `+0x177 AND EAX,0xffffff3f` · `+0x17c CMP EAX,0x10000f` · `+0x181 JZ +0x1a3`** → 같으면 **유저 안에서 복원: `FXRSTOR` (`+0x1e7`), XMM0~15, `LDMXCSR` (`+0x266`), 레지스터 로드, `+0x300 IRETQ`** (syscall 없음; `+0x1a3 ~ +0x1e0` 은 `0x40` 비트가 있을 때만 `XRSTOR`).
  - 다르면 **`+0x183 XOR EDX,EDX` · `+0x185 CALL NtContinue` (ret `+0x18a`)** — `NtContinue(RCX=문맥, EDX=0 = TestAlert 거짓)`. `+0x18a CMP EAX,0xc000060a; +0x18f JNZ +0x198(그냥 돌아감)`, 같으면 `+0x191 MOV ECX,0x30; +0x196 INT 0x29`.
  - 결론 `[정적]`: **커널이 쌓은 문맥 (`0x10001f`) 은 `0x10000f` 와 달라서 항상 `NtContinue`**. `RtlRaiseException` 과 `RtlUnwindEx` 가 만든 문맥은 `0x10000f` 라서 **`NtContinue` 없이 유저 안 `IRETQ`**.
- `RtlGuardRestoreContext` (rva `0xa59f0`, PDB 별칭; Ghidra 이름은 `RtlRestoreContext`) 는 위 `RtlRestoreContext` 앞의 얇은 막이다. 레코드 NULL 이면 `+0x18 TEST RDX,RDX` / `+0x1b JZ +0x178` → `+0x178 CMP [LdrSystemDllInitBlock+0xb8],0` / `+0x180 JZ +0x19b` (test.exe: `+0xb8 = 0` 이라 여기로) → **`+0x1b6 JMP RtlRestoreContext`** (꼬리 호출, 문맥 값 안 바꿈). `+0xb8 != 0` 이고 `+0x9c` 비트 0 이 꺼지면 `RtlGuardIsValidStackPointer(Rsp)` (`+0x192`) 를 부르고 실패면 `+0x210 MOV ECX,0xd; INT 0x29`.
- ntdll 이 이 길에서 **읽는 입력** (엔진이 이 칸을 맞춰야 진짜 코드가 같은 길을 탄다): PEB `+0x2` BeingDebugged · `+0x50` CrossProcessFlags (비트 2 = VEH 있음, 비트 3 = VCH 있음) · `+0xbc` NtGlobalFlag (`0x800000` = 예외 로그, 켜져 있지 않다고 봄 `[추측]`); TEB `+0x8` StackBase · `+0x10` StackLimit · `+0x68` LastError; KUSD `+0x2d4` (KdDebuggerEnabled) · `+0x308` (SystemCall 비트: `NtXxx` 스텁이 `syscall` 인지 `int 0x2e` 인지) · `+0x3d8`/`+0x3ec`/`+0x708` (XState); ntdll 전역 `LdrInitState` (`0x1d23c8`, `== 3` 이어야 함수표 빠른 길), `LdrpInvertedFunctionTables`, `LdrpVectorHandlerList` (`0x1ea578`), `RtlpUnhandledExceptionFilter` (`0x1d28e8`), 쿠키 캐시 (`0x1d2408`).

## 질문별 답 [정적]

### 1. `KiUserExceptionDispatcher` (ntdll rva `0x164e10`, 92B)

`.asm` 전체가 `ms/disassemble/ntdll/KiUserExceptionDispatcher.asm` 한 장이다.

| 줄 | 하는 일 |
|---|---|
| `+0x0 CLD` | 방향 플래그 지움 |
| `+0x1 MOV RAX,[Wow64PrepareForException]` (rva `0x1e70e0`) · `+0x8 TEST` · **`+0xb JZ +0x1c`** | 이 전역이 0 이면 건너뜀 |
| `+0xd ~ +0x17` (`RCX = RSP + 0x4f0`, `RDX = RSP`) · **`+0x1a CALL RAX` (ret `+0x1c`)** | 전역이 0 이 아닐 때만 `(레코드, 문맥)` 으로 부름 (CFG 검사 없는 `CALL RAX`) |
| `+0x1c MOV RCX,RSP` · `+0x1f ADD RCX,0x4f0` · `+0x26 MOV RDX,RSP` | **RCX = EXCEPTION_RECORD = RSP + 0x4f0, RDX = CONTEXT = RSP** (입력은 `RSP` 하나, `RCX`/`RDX` 는 안 읽고 덮어씀) |
| **`+0x29 CALL RtlDispatchException` (ret `+0x2e`)** | `RtlDispatchException(레코드, 문맥)` → `AL` |
| `+0x2e TEST AL,AL` · `+0x30 JZ +0x3e` | 갈림 |
| **TRUE (`AL != 0`)**: `+0x32 MOV RCX,RSP` · `+0x35 XOR EDX,EDX` · **`+0x37 CALL RtlGuardRestoreContext` (ret `+0x3c`)** · `+0x3c JMP +0x53` | `RtlGuardRestoreContext(문맥 = RSP, 레코드 = NULL)` — 꼬리로 `RtlRestoreContext` → 문맥 이름표 `0x10001f` 라서 **`NtContinue(문맥, TestAlert = 0)`** (위 "먼저 알아 둘 것"). 직접 `NtContinue` 를 부르는 줄은 이 함수에 없다. |
| **FALSE (`AL == 0`)**: `+0x3e MOV RCX,RSP` · `+0x41 ADD RCX,0x4f0` · `+0x48 MOV RDX,RSP` · `+0x4b XOR R8B,R8B` · **`+0x4e CALL NtRaiseException` (ret `+0x53`)** | **`NtRaiseException(레코드, 문맥, FirstChance = 거짓)`** (syscall 번호 `0x174`) |
| `+0x53 MOV ECX,EAX` · **`+0x55 CALL RtlRaiseStatus` (ret `+0x5a`)** · `+0x5a NOP` · `+0x5b RET` | **둘 중 어느 쪽 호출이든 돌아오면 (= 실패) 그 반환 상태로 `RtlRaiseStatus`** |

- `Wow64PrepareForException`: 64비트 프로세스에서는 **0** `[정적]` — `_refs/Wow64PrepareForException.tsv` 에 쓰는 코드가 없다 (읽는 곳 = 이 함수 `+0x1` 하나, 그리고 WoW64 초기화용 포인터 표 한 칸 `0x173848` 뿐). 그래서 `+0xb JZ` 가 항상 간다.
- `RtlRaiseStatus` (rva `0x12ce0`, 107B) 가 돌아온 뒤에 하는 일: 레코드 (코드 = 받은 상태, 플래그 `0x81` = NONCONTINUABLE | SOFTWARE_ORIGINATE, 주소 −1, 개수 0) 와 CONTEXT 를 자기 스택에 만들어 **`+0x4a RtlRaiseNoncontinuableException(레코드, 문맥, 1)` (ret `+0x4f`)** 를 부른다.
  - `RtlRaiseNoncontinuableException` (rva `0x1243a0`): `RtlpCaptureContext` 로 문맥을 잡고 `+0x31 ~ +0x42` 에서 주소가 −1 이면 `문맥.Rip` 로 채움 → `R8B != 0` 이고 `BeingDebugged == 0` 이면 **`+0x60 JMP RtlDispatchException`** (유저 SEH 먼저), 아니면 `+0x6b JMP NtRaiseException` (`R8B` 가 FirstChance).
  - `RtlRaiseStatus` 는 첫 번째에 `R8B = 1` → 유저 SEH 에 한 번 맡기고, 돌아오면 (`BeingDebugged == 0` 일 때) `+0x5e ADD BL,0xff / +0x61 JZ +0x3a` 로 **두 번째는 `R8B = 0`** → `NtRaiseException(…, FALSE)` → 커널 둘째 기회 (sast/78 질문 4). 그것도 돌아오면 `+0x65` 에서 `RtlRaiseStatus` 를 다시 (되풀이).

**지금 우리 (PE_emulator):** 이 함수는 진짜 ntdll 이 Unicorn 안에서 그대로 돈다. 엔진이 줄 것은 (1) sast/78 의 진입 모양 (`RSP` = CONTEXT, `+0x4f0` 레코드, `+0x590/+0x5a8` 머신 프레임), (2) 이 함수 밑의 `RtlpExecuteHandlerForException` 이 돌 스택 (진입 `RSP` 아래쪽 페이지가 쓸 수 있어야 함: 이 함수는 `RSP` 아래로 `CALL` 을 쌓는다), (3) 위 syscall 둘 (`NtContinue`·`NtRaiseException`) 의 구현. 이 줄들은 지금 없음.

### 2. `RtlDispatchException` (rva `0xcb730`, 2197B) 의 큰 줄기

호출 순서 (`.asm` 줄):

| 단계 | 줄 | 내용 |
|---|---|---|
| 0 | `+0x46`/`+0x76` memset · `+0x84 TEST [PEB+0xbc],0x800000` · `+0x8e JZ +0x1f7` | 지역 변수 지움. `NtGlobalFlag & 0x800000` (예외 로그 켜짐) 이 꺼져 있으면 `+0x94 ~ +0x1ed` (로그 기록: `RtlpExceptionLog2`) 를 통째로 건너뜀. |
| (a) VEH | **`+0x1fd ~ +0x200 CALL RtlpCallVectoredHandlers(레코드, 문맥, 0)` (ret `+0x205`)** | 아래 (a) |
| | `+0x205 TEST AL,AL` · `+0x207 JZ +0x24b` | 0 이면 SEH 로. 아니면 `+0x209 MOV DIL,1` → `+0x20c` 로 (아래 (e)). |
| (b) SEH 준비 | `+0x24b MOV ESI,[RBX+4]` · `+0x256 AND ESI,0x81` · **`+0x25c CALL RtlpGetStackLimits` (ret `+0x261`)** · `+0x26c CALL RtlpSanitizeContext` (ret `+0x271`) | 스택 한계를 `[RBP+0x48]` (낮은 쪽 = TEB+0x10) / `[RBP+0x50]` (높은 쪽 = TEB+0x8) 에 받아 둠. 못 받으면 `+0x263 JZ +0x758`: 플래그에 `0x8` (EXCEPTION_STACK_INVALID) 을 켜고 FALSE. |
| | `+0x271 ~ +0x2a9` 이름표 고르기 · `+0x2a9 RtlpValidateContextFlags2` (ret `+0x2ae`) · `+0x30e __chkstk` · **`+0x32b RtlInitializeExtendedContext2` (ret `+0x330`) · `+0x336 RtlpCopyContext` (ret `+0x33b`)** | 지역 사본을 만든다. 이름표는 `(LdrSystemDllInitBlock+0xa8 >> 0x3c) & 3 == 1` (CET) 일 때만 `0x10004b` + 기능 마스크 `0x800`, 아니면 `0x10000b` (test.exe 는 후자). 사본 이름표는 `RtlpCopyContext` 가 `0x10000f` 로 맞춤. |
| 프레임 걷기 (고리) | `+0x370` 고리 머리: **`+0x37a CALL RtlLookupFunctionEntry(Rip, &ImageBase, &히스토리)` (ret `+0x37f)`** → `+0x385 RtlpSanitizeContext` → **`+0x3e3 CALL RtlpxVirtualUnwind(1 = EHANDLER, ImageBase, Rip, 함수항목, 사본, &핸들러데이터, &EstablisherFrame, …)` (ret `+0x3e8)`** | 프레임마다 한 칸씩 풂. `RtlpxVirtualUnwind` 는 PDB 이름이고 `RtlVirtualUnwind` (rva `0x16bc0`) 는 얇은 겉포장이다 (이 고리는 안쪽을 직접 부름; 겉포장은 `+0x6bd` 에서만). **`.pdata` 가 없으면 (`함수항목 = NULL`): `RtlpxVirtualUnwind +0x3a TEST R9,R9` / `+0x3d JZ +0x6ba` 로 가서 리프 처리 (반환 주소 = `[Rsp]` 로 풂)** `[정적: 분기만 확인, 안쪽은 이름만]`. |
| 검사 | `+0x3e8 TEST EAX,EAX` · `+0x3ea JS +0x64b` | 풀기 실패 (상태 < 0) → FALSE |
| | `+0x3f0 ~ +0x40b`: `EstablisherFrame` 이 8 정렬 아니거나 (`+0x3f4 TEST CL,7`) 낮은 쪽 미만 (`+0x401 JC`) 이거나 높은 쪽 이상 (`+0x40b JNC`) → `+0x890` (`플래그 \|= 8`) → FALSE | |
| | `+0x411 MOV R8,[RBP+0x10]` · `+0x418 JZ +0x622` | 이 프레임에 언어 핸들러가 없으면 (`R8 = 0`) 다음 프레임으로 |
| 핸들러 부름 | `+0x424 ~ +0x466`: `DISPATCHER_CONTEXT` 를 `[RBP+0x60]` 에 채움 (`ControlPc`, `ImageBase`, `FunctionEntry`, `EstablisherFrame`, `ContextRecord` = 사본, `LanguageHandler`, `HandlerData`, `HistoryTable`, `ScopeIndex = 0`) · **`+0x5ea ~ +0x5f0 CALL RtlpExecuteHandlerForException(레코드, EstablisherFrame, 원래 문맥, &DISPATCHER_CONTEXT)` (ret `+0x5f5)`** | `RtlpExecuteHandlerForException` (rva `0x1654f0`, 21B): `+0x9 MOV RAX,[R9+0x30]` (= `LanguageHandler`) · **`+0xd CALL RAX`** (ret `+0xf`) — **CFG 검사 없이** 그냥 부름. 이 함수 자신이 `UNWIND_INFO` 에 예외 처리기 `0x1654c0` 을 달고 있다: `플래그 & 0x66` (되감는 중) 이면 1 (ContinueSearch) 을, 아니면 안쪽 `EstablisherFrame` 을 `DISPATCHER_CONTEXT +0x18` 에 옮기고 2 (NestedException) 를 돌려줌. |
| 돌아온 값 | `+0x5f5 MOV ECX,EAX` · `+0x61d CMP ECX,1` · `+0x620 JNZ +0x656` | **1 = ExceptionContinueSearch** → `+0x622`: `Rsp = 사본[+0x98]` 과 `Rip = 사본[+0xf8]` 를 꺼내 `+0x630 TEST AL,7`·`+0x634 CMP RAX,[RBP+0x50]`·`+0x63a CMP RAX,[RBP+0x48]` 로 스택 범위 검사 (벗어나면 `+0x64b` FALSE) → `+0x646 JMP +0x370` (다음 프레임). |
| | `+0x656 TEST ECX,ECX` · `+0x658 JZ +0x743` | **0 = ExceptionContinueExecution** → `+0x743 TEST SIL,1` (NONCONTINUABLE 이면 `+0x74d MOV ECX,0xc0000025; +0x752 RtlRaiseStatus`) 아니면 `+0x209` → **TRUE** |
| | `+0x65e SUB ECX,2` · `+0x661 JZ +0x763` | **2 = NestedException** → `+0x763` 플래그 `\|= 0x10` (NESTED_CALL), 안쪽 프레임을 `[RBP+0x40]` 에 기억하고 계속 |
| | `+0x667 CMP ECX,1` · `+0x66a JNZ +0x7f4` | **3 = CollidedUnwind** (`+0x670 ~ +0x720`: 사본 복원 → `RtlVirtualUnwind` (`+0x6bd`) → 다시 부름. `(+0xa8 >> 0xc) & 3 == 1` 일 때만 `+0x6dd` 에서 CET 일치 검사, 어긋나면 `+0x7f2 INT 0x29` ECX=0x27). 그 밖의 값은 **`+0x7f4 MOV ECX,0xc0000026; +0x7f9 RtlRaiseStatus`** (STATUS_INVALID_DISPOSITION) |
| 끝 (FALSE) | `+0x64b XOR DIL,DIL` · `+0x64e MOV [RBX+4],ESI` · `+0x651 JMP +0x20c` | 프레임 끝까지 갔거나 풀기 실패 → FALSE (아래 (e)) |

**(a) VEH — `RtlpCallVectoredHandlers(레코드, 문맥, 어느 목록)` (rva `0xcc0c0`, 983B)**
- 부르는 오프셋: `RtlDispatchException +0x200 / ret +0x205` (어느 목록 = 0 = 예외 핸들러).
- **목록이 비었을 때 지나가는 줄**: `+0x1d LEA EBP,[R8+2]` · `+0x3b MOV EAX,[PEB+0x50]` (CrossProcessFlags) · **`+0x3e BT EAX,EBP` · `+0x45 JNC +0x134`** → 어느 목록=0 이면 PEB `+0x50` 의 **비트 2 (`ProcessUsingVEH`)**, =1 이면 **비트 3 (`ProcessUsingVCH`)** 가 꺼져 있으면 **락도 안 잡고** `+0x134 MOVZX EAX,BL` (BL = 0) 로 곧바로 돌아간다. 이 비트는 `RtlpAddVectoredHandler +0x1af BTS.LOCK [PEB+0x50],ECX` 가 켜고, 마지막 노드가 빠질 때 `RtlpCallVectoredHandlers +0x262 BTR.LOCK` 이 끈다. 비트가 켜져 있어도 목록이 비어 있으면 `+0x6c JZ +0x122` 로 빠짐.
- 전역 목록 `LdrpVectorHandlerList` (rva `0x1ea578`) = 두 칸 × 0x18 (`{SRWLock 8, 목록머리 Flink/Blink 16}`; 칸 0 = 예외, 칸 1 = 계속) 이 **파일 값부터 빈 원형 목록** (`_refs`: 머리가 자기 자신을 가리키는 포인터 `0x1ea580/588/598/5a0`). `RtlAddVectoredExceptionHandler`/`…ContinueHandler` (`0x10aad0`/`0x114490`, 8~11B 겉포장) 를 ntdll 안에서 부르는 코드는 **없다** (`_refs` 에 내보내기 표·`.pdata` 말고 호출 없음) → test.exe 가 직접 안 부르면 비트 2·3 은 0 이고 VEH 는 안 돈다.
- 노드가 있을 때: `+0x61 RtlAcquireSRWLockExclusive` → 노드마다 참조 올림 (`+0x88 XADD.LOCK`) · `+0x9d RtlReleaseSRWLockExclusive` · **`+0xa6 RtlDecodePointer([노드+0x20])`** (ret `+0xab`) · `+0xbd`~ `{레코드, 문맥}` 쌍을 `[RSP+0x40]` 에 만듦 · **`+0xd1 CALL (CFG 검사 거친 간접 호출)` (ret `+0xd6)`** 로 핸들러를 `EXCEPTION_POINTERS*` 로 부름. **`+0x112 CMP [RSP+0xa8],-1` / `+0x11a JNZ +0x69` / `+0x120 MOV BL,1`** → 핸들러가 `-1` (EXCEPTION_CONTINUE_EXECUTION) 을 돌려주면 그 자리서 멈추고 TRUE, 그 밖의 값 (0 도 1 도) 은 다음 노드로. (`NtGlobalFlag & 0x800000` 이면 `+0xc6 JNZ +0x16a` 로 `RtlpLogExceptionHandler`.)
- 핸들러가 `-1` 이면 `RtlDispatchException` 은 **SEH 를 건너뛰고 바로 TRUE** (`+0x207 → +0x209`).

**(b) SEH** — 위 표. 정리: 스택 한계 `+0x25c/+0x261` · 프레임마다 `RtlLookupFunctionEntry` `+0x37a/+0x37f` → `RtlpxVirtualUnwind` `+0x3e3/+0x3e8` → 언어 핸들러를 `RtlpExecuteHandlerForException` `+0x5ea ~ +0x5f0 / +0x5f5` 로 부름. 핸들러를 못 찾고 스택 끝까지 가면 FALSE (`+0x64b`). 언어 핸들러가 `__C_specific_handler` 인 프레임 (`0x124670`) 은 질문 4 에서 본다.
- 머신 프레임을 걸어 넘는 법 (`KiUserExceptionDispatcher` 프레임에서 예외가 난 함수의 프레임으로 넘어가는 곳): 조정자 덧붙임 2.
- **스택 위치 조건 `[정적]`**: `RtlpGetStackLimits` (rva `0xcc530`, 43B) 는 `[높은 쪽] = TEB+0x8`, `[낮은 쪽] = TEB+0x10` 을 받고 **지금 `RSP` 가 `[낮은 쪽, 높은 쪽)` 안인지도 같이 본다** (`+0x1b CMP RAX,R9; +0x1e JA 실패; +0x20 CMP R9,[RDX]; +0x23 JNC 실패`). 즉 새 `RSP` (`C`) 아래에서 도는 디스패처 스택이 TEB 한계 안이어야 하고, 되감을 때 만나는 `EstablisherFrame`·머신 프레임 칸도 이 범위 안이어야 한다. 아니면 플래그 `0x8` + FALSE (또는 `RtlUnwindEx` 의 `0xC0000028` BAD_STACK). 커널이 스택을 늘릴 때 `TEB+0x10` 을 같이 낮추는지는 이번에 안 봄 `[동적/후속]`.

**(c) 핸들러 검사가 실패하면** — `RtlDispatchException` 자체에는 SafeSEH 류 (`RtlIsValidHandler`) 검사가 **없다** (x64 에는 SafeSEH 가 없고, 언어 핸들러는 `RtlpExecuteHandlerForException +0xd` 의 CFG 없는 `CALL RAX`). 이 길에서 "검사" 로 볼 만한 것 셋:
  1. 스택·정렬 검사 (위): 실패 → 플래그 `0x8` + FALSE → `NtRaiseException(…, FALSE)`.
  2. 반환값 검사: `Noncontinuable` 인데 0 이면 `RtlRaiseStatus(0xC0000025)`, 이상한 값이면 `RtlRaiseStatus(0xC0000026)` (`+0x74d`/`+0x7f4`).
  3. **되감을 때** `RtlGuardCheckExceptionHandler` (rva `0xa5d00`, EH 이어하기 표 검사 `[추측: 이름 뜻]`) 가 `RtlUnwindEx +0x5aa / ret +0x5af` (코드 `0xC0000027` 일 때, `(+0xa8 >> 0x3c)&3 != 1` 이면 부름) 와 `RtlGuardRestoreContext +0x1e6 / ret +0x1eb` 에서 불린다. **그런데 이 함수는 첫 줄 `+0x18 CMP [LdrSystemDllInitBlock+0xb8],0` / `+0x2a JZ +0x195` 로 바로 끝난다 (test.exe: `+0xb8 = 0`)**, 즉 test.exe 에서는 아무것도 안 함. `+0xb8 != 0` 인 프로세스에서 표에 없으면 `+0x1a0 ~ +0x1ad RtlFailFast2(0x26)` = `INT 0x29` (sast/78 질문 5 의 커널 쪽 `__fastfail` 길, 유저 핸들러 안 탐).

**(d) 이 길에서 syscall 이 있나 — `RtlDispatchException` 본체에는 `syscall` 도 `Nt…` 호출도 없다.** 평소 길 (VEH 없음, 함수표 빠른 길) 은 **syscall 0**. 안쪽 호출 중 syscall 로 이어질 수 있는 것 (지도 `KiUserExceptionDispatcher_nodes.tsv` 의 `syscall` 11개 중 이 줄기에 닿는 것):

| 어디서 | syscall | 언제 (평소엔 안 탐) |
|---|---|---|
| `RtlLookupFunctionEntry` → `RtlpxLookupFunctionTable +0x2d0 / ret +0x2d5` | `NtQueryVirtualMemory` (`0x23`, 클래스 6) | `LdrInitState != 3` (로더 초기화 중) 일 때만 — 함수표 안 보고 PE 헤더로 (`sast/55` 1(e)). 예외가 로더 초기화가 끝난 뒤 나면 안 탐. |
| 같은 함수 `+0x406 / +0x40b`, `+0x508 / +0x50d` | `NtAlertThreadByThreadIdEx` (`0x72`), `NtSetInformationThread` (`0xd`, 클래스 `0x38`) | 함수표 락 (`LdrpInvertedFunctionTableSRWLock`) 경합 때 (스레드 하나면 안 탐). `RtlpAcquireSRWLockSharedContended` 쪽은 `NtWaitForAlertByThreadId` (`0x1e3`) 도. |
| `RtlLookupFunctionEntry +0x352` → `RtlpLookupDynamicFunctionEntry` | (없음, SRW 락 + `[인라인 CFG 간접 호출]`) | 주소가 모듈 표에 없고 동적 함수표 (JIT·셸코드가 `RtlAddFunctionTable` 으로 등록) 에 있을 때 — 등록된 **콜백이 유저 코드**. |
| `RtlpCallVectoredHandlers +0xa6` → `RtlDecodePointer` | `NtQueryInformationProcess` (`0x19`, 클래스 `0x24` = 쿠키) | 쿠키 캐시 (`0x1d2408`) 가 0 일 때 한 번만 (`RtlDecodePointer +0x1b ~ +0x4f`). 프로세스 초기화 중에 이미 채워졌다고 봄 `[추측]`. |
| `RtlpCallVectoredHandlers` 노드 제거 | `NtProtectVirtualMemory` (`0x50`) | VEH 를 `RtlRemoveVectoredExceptionHandler` 로 뺄 때 `.mrdata` 보호 (`LdrProtectMrdata`) |
| `RtlRaiseStatus` (이상한 값·NONCONTINUABLE) | `NtRaiseException` | 오류 갈래 (위 (c)) |

**(e) VEH continue 핸들러 (`RtlpCallVectoredHandlers(…, 1)`)** — `RtlDispatchException` 의 **공통 꼬리 `+0x20c MOV R8D,1`** (`+0x20c ~ +0x218`, call ret `+0x21d`, 그 뒤 `+0x21d MOVZX EAX,DIL` 로 반환) 에서 불린다. 여기로 오는 길이 **셋**: (1) VEH 가 처리함 (`+0x209 MOV DIL,1` 거쳐), (2) SEH 가 `ExceptionContinueExecution` 으로 처리함 (`+0x743 TEST SIL,1` / `+0x747 JZ +0x209`), (3) **처리 못 함 (FALSE) 일 때도** — `+0x64b XOR DIL,DIL; +0x651 JMP +0x20c`, 스택 무효 `+0x75e JMP +0x20c`. 즉 TRUE 뒤에만 도는 게 **아니라 TRUE·FALSE 둘 다 끝에서 돈다**. 목록이 비면 (PEB 비트 3 이 꺼짐) 즉시 돌아와 아무 일도 없음. 반환값은 버림 (`DIL` 이 최종값).

**지금 우리 (PE_emulator):** 이 함수와 아래 부품들 (`RtlpCallVectoredHandlers`·`RtlLookupFunctionEntry`·`RtlpxVirtualUnwind`) 은 진짜 ntdll 이 돈다 — 엔진이 흉내 낼 것은 없고 **PEB `+0x50` 비트 2·3 이 0, `LdrInitState` 가 로더 끝난 뒤 값, TEB `+0x8/+0x10` 이 맞는 것** 만 맞으면 된다. 틀린 점은 없음. 함수표 빠른 길은 syscall 이 없어서 `NtQueryVirtualMemory` 구현이 이 길에 필요한 건 아니다.

### 3. `RtlRaiseException` (rva `0xf700`, 685B) — 요청 26 의 7

- **커널을 안 거치고 `RtlDispatchException` 을 부르나: 예 (디버거가 없을 때).** 줄: `+0xe6 RtlpCaptureContext2` (ret `+0xeb`) 로 문맥을 잡음 → `+0x111 RtlLookupFunctionEntry` (ret `+0x116`) + **`+0x147 RtlVirtualUnwind` (ret `+0x14c)`** 로 `RtlRaiseException` 자기 프레임을 한 칸 풂 (그래서 문맥의 `Rip`/`Rsp` 는 부른 쪽의 것) → `+0x158 ~ +0x15f` `레코드.ExceptionAddress = 문맥.Rip` → `+0x16a RtlpGuardSynchronizeRestorePc` (ret `+0x16f`, 한 줄 `RET` 인 자리) → **`+0x16f MOV RAX,GS:[0x60]` · `+0x17e CMP byte [RAX+2],R15B` · `+0x182 JNZ +0x29f`** (`PEB.BeingDebugged != 0` 이면 `+0x29f` 로) → 아니면 **`+0x188 CALL RtlDispatchException` (ret `+0x18d)`**.
  - 앞에서: `+0x5f BTS [레코드+4],7` → `ExceptionFlags |= 0x80` (EXCEPTION_SOFTWARE_ORIGINATE). 이름표 `+0x64 MOV R14D,0x10000b`, `+0x8d CMP [KUSD+0x3d8],0` (XState 켜져 있으면 `+0x1e2 MOV R14D,0x10004b`), `+0xa0 RtlGetExtendedContextLength3`·`+0xc2 __chkstk`·`+0xdc RtlInitializeExtendedContext2`.
  - `+0x14c CMP [레코드],0x80000033` (STATUS_UNWIND_CONSOLIDATE) 면 `+0x152 JZ +0x242` 로 한 칸 더 풂.
- **갈래:**
  - `RtlDispatchException` 이 **TRUE** (`+0x18f JZ +0x294` 안 감) → **`+0x195 ~ +0x19b CALL RtlRestoreContext(문맥, 레코드)` (ret `+0x1a0)`** (rva `0x123e20`). 문맥 이름표 = `0x10000f` (위 표) 라서 **`NtContinue` 가 아니라 유저 `IRETQ`** (syscall 0). 못 돌아오고 여기 ret 으로 오면 오류.
  - **FALSE** → `+0x294 XOR R8D,R8D` · `+0x29d JMP +0x2a2` → **`+0x2a2 CALL NtRaiseException` (ret `+0x2a7)`, `FirstChance = 거짓 (R8D = 0)`**. 돌아오면 `+0x2a7 MOV EBX,EAX` · `+0x2a9 JMP +0x1d2` → `+0x1d4 RtlRaiseStatus(EBX)` (ret `+0x1d9`, 뒤는 `INT3`).
  - **디버거가 붙었으면 (`BeingDebugged != 0`)**: `RtlDispatchException` 을 **건너뛰고** `+0x29f MOV R8B,1` → **`NtRaiseException(레코드, 문맥, FirstChance = 참)`** (`+0x2a2`). 커널이 디버거에게 먼저 묻고 (sast/78 질문 3·4), 안 받으면 커널이 `KiUserExceptionDispatcher` 로 보내 유저 디스패치가 그때 돈다. 이 때문에 "몇 번째 기회로 들어가나" 의 답: **디버거 없음 → 유저가 먼저 (커널 안 감); 못 받아서 커널로 갈 때만 `FirstChance = 거짓`. `FirstChance = 참` 은 디버거가 붙었을 때만.** (`_PEB +0x2 = BeingDebugged` 는 PDB 구조체로 확인 `[정적]`. 유저 모드 디버거가 붙을 때 켜지고 커널 디버거만 붙으면 0 이라는 것은 `[추측]` — 동적 확인 3·9.)
  - `+0x119 JZ +0x1d2` : 자기 프레임 함수 항목을 못 찾으면 바로 `RtlRaiseStatus`.
- **kernelbase `RaiseException` (rva `0xc4140`, 177B) 이 여기로 오나: 예.** `+0x3a ~ +0x59`: 스택에 레코드를 만들고 (`[RSP+0x20]` = 코드, `[RSP+0x24] = 플래그 & 1`, `[RSP+0x28] = 0`, **`[RSP+0x30] = RaiseException 자기 주소`** (`+0x4a LEA RAX,[0x1800c4140]`), 개수 `min(n, 15)` (`+0x5b ~ +0x70`), 인자는 `+0x79 memcpy`) → **`+0x83 CALL [IAT] RtlRaiseException` (ret `+0x8a)`**. 그러므로 최종 `ExceptionAddress` = **`kernelbase!RaiseException +0x8a`** (`RtlRaiseException +0x15f` 가 자기 주소를 부른 쪽 반환 주소로 덮어씀).
  - 처리되면 (`IRETQ`) `kernelbase!RaiseException +0x8a` 로 돌아가 `+0x9a` 쿠키 검사 후 `RET`. 비휘발 레지스터는 `RtlVirtualUnwind` 가 `RaiseException` 의 값으로 되돌려 둠.

**지금 우리 (PE_emulator):** "소프트웨어 예외 (`RtlRaiseException`) 는 유저 안에서 돈다" **맞다** — 디버거가 없고 (`PEB.BeingDebugged = 0`) 받는 쪽이 있으면 **syscall 이 하나도 없이** 유저 안에서 `IRETQ` 로 끝난다 (`NtContinue` 도 안 부름). 틀린 점 둘: (1) 못 받으면 `NtRaiseException(…, FALSE)` 로 커널에 간다 (엔진 구현 필요), (2) 유저 안 `IRETQ` (`CS = 0x33`, `SS = 문맥.SegSs`) 를 Unicorn 이 그대로 실행해야 한다 (GDT·세그먼트 설정 `[동적/엔진]`).

### 4. 아무도 안 받을 때 (유저 쪽)

**길 (줄 단위):**

1. 스레드 시작 `RtlUserThreadStart` (rva `0xacac0`) 의 SEH: `.pdata` 의 scope table (내가 읽음) = **`[+0x7, +0x4a)` 가 `__try`, 필터 = `RtlUserThreadStart$filt$0` (rva `0x169b61`), 착지 (= `__except` 본문) = `+0x4a`**, 언어 핸들러 = `__C_specific_handler` (rva `0x124670`). `__try` 안에서 부르는 것은 `+0x27 / +0x3d` 의 시작 함수 (`BaseThreadInitThunk` 또는 사용자 시작 주소) 와 `+0x44 RtlExitUserThread`.
2. `RtlDispatchException` 이 프레임을 걷다 `RtlUserThreadStart` 프레임에서 `__C_specific_handler` 를 부름 (질문 2 `+0x5f0`). `__C_specific_handler`: `+0x2b __except_validate_context_record` (ret `+0x30`) → `+0x3e TEST [레코드+4],0x66` (되감는 중이 아님) → 범위 표 고리 `+0x5d ~ +0xdf` → **필터 호출 `+0x4e ~ +0x91 CALL RAX` (ret `+0x93)`** (`RCX = &{레코드, 문맥}`, `RDX = EstablisherFrame`; 필터 값이 상수 1 이면 안 부름 `+0x7b/+0x80`) → `+0x93 TEST EAX,EAX` · `+0x95 JS +0xea` (`< 0` → 0 = ExceptionContinueExecution 을 돌려줌) · `+0x97 JLE +0xdb` (`0` → 다음 항목, `+0xdb INC EDI`) · 양수면 `+0xa9 _NLG_Notify` → **`+0xd1 CALL RtlUnwindEx(EstablisherFrame, 착지, 레코드, ReturnValue = (int64)ExceptionCode, 문맥, 히스토리)` (ret `+0xd6)`**.
3. 필터 `RtlUserThreadStart$filt$0` (`.asm` 끝의 `seh:` 줄): `+0x19 MOV RCX,[RtlpUnhandledExceptionFilter]` (rva `0x1d28e8`, 인코딩된 포인터) · **`+0x20 RtlDecodePointer` (ret `+0x25)`** · 널이 아니면 `+0x3a CALL (CFG 검사 거친 간접 호출) RAX` (ret `+0x3f`, `RCX = EXCEPTION_POINTERS`) → 그 반환값이 필터 값; **널이면 `+0x4c CALL RtlUnhandledExceptionFilter2(포인터, 빈 문자열)`** (ret `+0x51`).
4. **전역 `RtlpUnhandledExceptionFilter` 는 kernelbase 가 채운다:** `_KernelBaseBaseDllInitialize +0x4ab / ret +0x4b2` 가 `RtlSetUnhandledExceptionFilter(UnhandledExceptionFilter)` (ntdll `0xe95f0`: `전역 = RtlEncodePointer(인자)`) 를 부름. 사용자가 `SetUnhandledExceptionFilter` 로 바꾸는 것은 ntdll 전역이 아니라 kernelbase 의 `BasepCurrentTopLevelFilter` (`+0x4b2` 직후 `RtlEncodePointer(0)` 로 비워 둠). 그래서 kernelbase 가 올라온 프로세스에서는 `filt$0` 이 **kernelbase `UnhandledExceptionFilter` (rva `0x10ed90`)** 를 부른다. (ntdll `RtlUnhandledExceptionFilter2`, rva `0x122300`, 은 그 전역이 널일 때만. 디버거 없으면 `RtlIsAnyDebuggerPresent` (`PEB+2` 또는 `KUSD+0x2d4 & 3 == 3`) 가 거짓이라 `+0x391` 로 가서 코드 `0xC0000409` 일 때만 `RtlReportException`, 돌려주는 값 = `(코드 != 0xC0000194) ? 0 : -1` = 보통 **0 (CONTINUE_SEARCH)**.)
5. **kernelbase `UnhandledExceptionFilter` 가 부르는 syscall 과 차례** (디버거 없음, 보통 코드 `0xC0000005` 같은 것, 비 secure 프로세스, 사용자 최상위 필터 없음; 오프셋은 `UnhandledExceptionFilter.asm` 줄, ret 포함):

| # | 줄 (call / ret) | 부르는 것 → syscall | 인자 | 결과로 갈리는 곳 |
|---|---|---|---|---|
| 0 | `+0x5a TEST [레코드+4],0x10` · `+0x5e JNZ +0x20b` | (없음) | | 플래그 `0x10` (NESTED_CALL) 켜져 있으면 **0 (CONTINUE_SEARCH)** 로 바로 끝 |
| 1 | `+0x6c CMP [레코드],0xc0000409` | (코드가 `0xC0000409` 일 때만) `+0x74/+0x79 BasepIsKernelDebuggerPresent` → **`NtQuerySystemInformation` (`0x36`, 클래스 `0x23`, 2바이트)**, `+0x7d/+0x82 BasepIsDebugPortPresent` | | 둘 중 하나가 참이면 `+0x8d DbgPrint` + `INT3` |
| 2 | `+0xac/+0xb1 IsWerpGetDebuggerPresent` (PDB 별칭 3개 한 함수, rva `0x129be0`) | `ApiSetQueryApiSetPresence` — apiset **`ext-ms-win-kernel32-errorhandling-l1-1-0`** 이 PEB 의 ApiSetMap 에 있나 (`UNICODE_STRING` 표 `0x231960` → 문자열 `0x2d7f00` 로 확인), 결과는 전역에 캐시 (`[0x1803a9df8]`) | | 있으면 `AL = 1` |
| 3 | `+0xb8 CALL [0x1803c6508]` (ret `+0xbf`) | **delay-load `CheckForReadOnlyResourceFilter`** (스텁 `DelayLoad_CheckForReadOnlyResourceFilter` rva `0x129cad` → 공통 `0x129c2e` → 첫 호출 때 apiset 로 풀림). 위 apiset 은 `apisetschema.dll` 에서 **kernelbase 가 부르면 호스트 = `kernel32.dll`** (`kernel32.dll` 이 부를 때만 `faultrep.dll`). kernel32 에 `CheckForReadOnlyResourceFilter` (rva `0x5c2c0`, 78B) 가 있음 — 안은 이번에 안 읽음 | `ExceptionPointers` | 반환이 `-1` 이면 `+0xcf JZ +0x5b0` 으로 **-1 로 끝** |
| 4 | `+0xd5/+0xda BasepIsDebugPortPresent` | **`NtQueryInformationProcess` (`0x19`, 클래스 `7` = ProcessDebugPort, 8바이트)** (`BasepIsDebugPortPresent +0x22`) | `RCX = -1`, 버퍼 8B | 0 이 아니면 `+0xdc JNZ +0x20b` → **0 로 끝** |
| 5 | `+0xe5/+0xec RtlKnownExceptionFilter` (ntdll `0x10cc20`) | (syscall 없음) `코드 == 0xC0000194 ? -1 : 0` | | `-1` 이면 6 건너뜀 (`+0xf4 JZ +0x11c`) |
| 6 | **`+0x110/+0x117 NtSetInformationProcess`** | **`NtSetInformationProcess` (`0x1c`), 클래스 `0x3f` (63, 공개 열거형 이름은 ProcessFaultInformation `[추측]`), 값 8 (8바이트: 첫 4바이트 = 8)** | `RCX = -1`, `R9 = 8` | 반환값 안 봄 |
| 7 | `+0x126/+0x12d RtlAcquireSRWLockExclusive(BasepUEFLock)` · `+0x13e/+0x14c RtlDecodePointer(BasepCurrentTopLevelFilter)` | (쿠키 캐시 있으면 syscall 없음) | | 널이면 `+0x157 JZ +0x23c` (락 풀고 계속). **널이 아니면** (사용자가 `SetUnhandledExceptionFilter` 함) `+0x16e BasepFillUEFInfo` (안쪽 `NtQueryVirtualMemory` 3곳 `+0x66/+0xaf/+0xf8`) → 이름 비교 → `+0x1ee CFG 간접 호출` (사용자 필터, ret `+0x1f3`): 돌려준 값이 `-1` 또는 `1` 이면 그 값으로 끝, `0` 이면 `+0x202 BasepIsDebugPortPresent` 한 번 더 |
| 8 | `+0x24e/+0x255 RtlKnownExceptionFilter` (2번째) | | `-1` 이면 `+0x25d JZ +0x5b0` → -1 로 끝 |
| 9 | **`+0x282/+0x289 NtSetInformationProcess`** | **같은 클래스 `0x3f`, 값 1** | | 반환값 안 봄 |
| 10 | `+0x28e CMP [BasepIsSecureProcess],0` · `+0x294 JNZ +0x20b` | | secure 프로세스면 **0 로 끝** |
| 11 | **`+0x29a/+0x29f GetErrorMode`** | **`NtQueryInformationProcess` (`0x19`, 클래스 `0xc` = ProcessDefaultHardErrorMode, 4바이트)** (`GetErrorMode +0x1f`) | | **`+0x29f TEST AL,2` / `+0x2a1 JNZ +0x5a6` → 값 비트 1 (`0x2`, SEM_NOGPFAULTERRORBOX) 이 있으면 `+0x5a6 MOV EAX,1` = EXCEPTION_EXECUTE_HANDLER 로 끝** (WER 안 감) |
| 12 | `+0x2a7/+0x2ae RtlGetThreadErrorMode` | (TEB 읽기) | | `AL & 0x20` 이면 **1 로 끝** |
| 13 | **`+0x2d1/+0x2d8 NtQueryInformationJobObject`** | **`NtQueryInformationJobObject` (`0x156`), `(NULL, 클래스 2, 버퍼 0x40, NULL)`** | | 실패 (< 0) 면 무시 (`+0x2df JS +0x2ef`), 성공이고 버퍼 `+0x10` (LimitFlags) 의 `0x400` (DIE_ON_UNHANDLED_EXCEPTION) 이면 **1 로 끝** |
| 14 | `+0x2f8/+0x2fd` apiset 있나 (2번과 같은 함수, 캐시) | | 없으면 `+0x2ff JZ +0x3a2` (WER 건너뛰고 18 로) |
| 15 | **`+0x315/+0x31a LoadLibraryExW(L"ext-ms-win-kernel32-errorhandling-l1-1-0.dll", NULL, 0)`** (문자열 `0x2e52f0`) | 로더 일 (apiset 로 `kernel32.dll` 에 풀리므로 이미 올라와 있으면 로드 없음 `[추측]`) | | 실패해도 `LastError == 0` 이거나 `0x7e` (ERROR_MOD_NOT_FOUND) 이면 계속, 그 밖이면 `+0x33b JNZ +0x35f` 로 가서 `[RSP+0x38] = 0` 인 채 17 로 |
| 16 | **`+0x345/+0x34c CALL [0x1803c6510]` = `BasepReportFault(ExceptionPointers, 1)`** (delay-load; 꼬리 `+0x367 FreeLibrary`) | **WER 본체 — 이름만.** 스텁 `ext_ms_win_kernel32_errorhandling_l1_1_0_BasepReportFault` (rva `0x129830`) 는 apiset 못 풀 때 `LastError = 0x7f` 로 두고 **4 를 돌려준다**. 호스트 `kernel32!BasepReportFault` (rva `0x622d0`, 42B) 이후는 안 읽음 | | 반환값 `ESI` 로 17 |
| 17 | `+0x380 CMP ESI,3` · `+0x385 LEA EAX,[RSI-9]; CMP EAX,1` · `+0x38d CMP [RSP+0x38],0` | | `ESI ∈ {3, 9, 10}` 이거나 WER 를 안 불렀으면 (`[RSP+0x38] == 0`) → **`+0x39d RaiseFailFastException(레코드, 문맥, 0)`** (ret `+0x3a2`) — 질문 5. `ESI == 8` → **0 로 끝** (`+0x3a5`). `ESI == 6` 이고 디버그 포트 있음 → 0 로 끝 |
| 18 | `ESI == 4` (WER 가 안 정했거나 스텁이 4 를 줌, `+0x3c0 JNZ` 안 감) → `+0x3e0/+0x3e5 VirtualAllocExNuma(-1, NULL, 0x250, MEM_COMMIT, PAGE_READWRITE, 노드 -1)` | `NtAllocateVirtualMemoryEx` (`0x78`) `[추측: 래퍼 안 읽음]` | | 버퍼 `RDI` (0x250B, 0 으로 채워짐) |
| 19 | `+0x409/+0x40e WerpGetDebugger(버퍼)` (apiset 있을 때만, `+0x404 JZ`) | AeDebug 레지스트리 읽기 (`NtOpenKey`…) `[추측]` | | 설정 없으면 `[버퍼] & 1`·`& 2` 꺼짐 |
| 20 | **`+0x4ec/+0x4f3 NtRaiseHardError`** | **`NtRaiseHardError` (`0x175`), `(0xD0000144 = STATUS_UNHANDLED_EXCEPTION, 4개, 0, &{코드, 주소, 정보0, 정보1}, 응답 옵션 1 또는 2, &응답)`** | `[버퍼]&1` 꺼짐 → 옵션 1 (OK) | `+0x4f8 AND EAX,0xc0000000 / CMP / +0x501 JNZ` : 반환 상태가 오류 심각도면 응답을 **6 (OK)** 로 둠. 응답이 3 (취소) 이면 AeDebug 쪽 (`+0x550 WerpLaunchAeDebug`, 설정 있을 때만) |
| 21 | `+0x568 CMP [BasepAlreadyHadHardError],0` · `+0x56e JZ +0x584` | | 아직 안 겪었으면 (0) 건너뜀. 겪었으면 `+0x578/+0x57f NtTerminateProcess(-1, 코드)` (`0x2c`) |
| 22 | `+0x598/+0x59d VirtualFreeEx(…, 0x8000)` | `NtFreeVirtualMemory` (`0x1e`) `[추측]` | | |
| 끝 | **`+0x59d MOV EAX,[RSP+0x3c]` · `+0x5a1 JMP +0x20d`** | | `[RSP+0x3c]` 는 `+0x64` 에서 **1** 로 시작 (AeDebug 를 띄운 경우만 `+0x562` 에서 0) | **기본 반환 = 1 = EXCEPTION_EXECUTE_HANDLER** |

   - **디버거 없을 때 돌려주는 값: 기본 `EXCEPTION_EXECUTE_HANDLER` (1)** (위 11·12·13·끝), 예외로 0 (디버거/secure/중첩/AeDebug 띄움/`ESI == 8`) 과 -1 (읽기 전용 자원 필터 / 데드락 코드 `0xC0000194`).
   - 흔한 단축 `[정적]`: 4번 이후 **`ProcessDefaultHardErrorMode & 2` (11번) 가 켜져 있으면 syscall 이 4개 (7, 0x3f, 0x3f, 0xc) 에서 끝나고 WER·로드 없음.** 꺼져 있으면 14번부터 WER 길 (이름만) 또는 `apiset` 이 없을 때의 고전 길 (18~22번).
   - 11번 값의 출처 `[정적]` : `EPROCESS.DefaultHardErrorProcessing` (짐 지도 `+0x388`, 부모 값 복사 / 부모 없으면 5). 비트 `0x2` 는 보통 꺼져 있으므로 (부모 cmd 의 값에 달림 `[동적]`) 진짜는 WER 길로 간다고 보는 게 안전.
6. **`UnhandledExceptionFilter` 가 1 을 돌려주면:** `__C_specific_handler +0xd1 RtlUnwindEx` → (아래) → 착지 `RtlUserThreadStart +0x4a` 에서 **`+0x4a MOV EDX,EAX` · `+0x4c OR RCX,-1` · `+0x50 CALL NtTerminateProcess` (ret `+0x55)`** = **`NtTerminateProcess(현재 프로세스 (-1), 예외 코드)`**. `EAX` = 예외 코드인 이유: `__C_specific_handler +0xbc MOVSXD R9,[R12]` 가 `RtlUnwindEx` 의 `ReturnValue` 로 `(int64)ExceptionCode` 를 주고, `RtlUnwindEx` 가 목적 프레임에서 문맥 `Rax = ReturnValue` 로 둠 (`RtlUnwindEx +0x3f2 ~ +0x3f9 MOV [R15+0x78],RAX`). 즉 종료 상태 = 예외 코드 그대로.
   - **`RtlUnwindEx` (rva `0x13380`) 끝맺음 `[정적]`**: 시작 `+0xa7 RtlpGetStackLimits` (ret `+0xac`, 실패면 `+0xb4 ~ +0xb9 RtlRaiseStatus(0xC0000028)`) → 새 문맥 `+0x886 RtlInitializeExtendedContext2` · **`+0x892 RtlCaptureContext2` (ret `+0x897)` → 이름표 `0x10000f`** → 프레임마다 `RtlLookupFunctionEntry` (`+0xf5`) · `RtlpCopyContext` (`+0x107`) · `RtlpxVirtualUnwind` (`+0x21e`) · 중간 프레임의 `__finally`/종료 핸들러는 `RtlpExecuteHandlerForUnwind` (`+0x2fb / ret +0x300`) → 목적 프레임 (`+0x3e5 CMP RCX,R12`): `Rax = ReturnValue` (`+0x3f9`), `Rip = 착지` (`+0x40c`), 레코드 코드를 `0xC0000027` 로 덮음 (`+0x41b`, 코드가 `0x80000029`/`0x80000026` 이 아닐 때) → `+0x5aa RtlGuardCheckExceptionHandler` (test.exe 에선 아무것도 안 함, 질문 2(c)) → **`+0x606 CALL RtlRestoreContext(문맥, 레코드)` (ret `+0x60b)`** → 문맥 이름표 `0x10000f` 이므로 **유저 안 `IRETQ`, syscall 없음.** 목적 프레임을 못 찾고 스택 끝에 닿으면 `+0x72c ~ +0x743 NtRaiseException(레코드, 문맥, FirstChance = 거짓)` (ret `+0x748`).
   - 정리: **`RtlUserThreadStart` 의 `__except` 가 있으므로 평소 "아무도 안 받음" 은 커널의 둘째 기회까지 가지 않는다.** 흐름은 `UnhandledExceptionFilter` (syscall 위 표) → 1 → `RtlUnwindEx` → `IRETQ` → `NtTerminateProcess(-1, 코드)`. 커널의 둘째 기회 (`NtRaiseException(FALSE)` → sast/78 질문 4 → `ZwTerminateProcess`) 는 필터가 0 을 돌려줄 때 (디버거 포트 있음 · secure 프로세스 · 중첩 플래그 · `ESI == 8`) 나 걷기가 FALSE 일 때만.
7. 필터가 `-1` 을 돌려주면 (`__C_specific_handler +0xea` → 0 = ExceptionContinueExecution) → `RtlDispatchException` TRUE → `KiUserExceptionDispatcher` → `NtContinue` (문맥은 원래 커널이 쌓은 것).

**지금 우리 (PE_emulator):** 이 길은 진짜 ntdll·kernelbase 가 돈다. 엔진이 줘야 할 답은 질문 6 표. 가장 중요한 것 하나: **`NtQueryInformationProcess(ProcessDefaultHardErrorMode)` 값에 비트 `0x2` 가 있느냐 없느냐** 와 **apiset `ext-ms-win-kernel32-errorhandling-l1-1-0` 가 ApiSetMap 에 있느냐** 가 WER 로 들어가는 길을 가른다. 지금 "Unicorn 오류 → `stop` 줄" 로 멈추는 것은 이 길의 맨 처음 (`KiUserExceptionDispatcher`) 이전이라 해당 없음.

### 5. `__fastfail` 유저 쪽 — kernelbase `RaiseFailFastException` (rva `0x10a020`, 493B)

- **`int 0x29` 와 같은 길인가: 아니다.** `int 0x29` (`__fastfail`) 는 커널 `KiRaiseSecurityCheckFailure` → `KiFastFailDispatch` 에서 코드 `0xC0000409` 로 **유저 핸들러를 한 번도 안 부르고 둘째 기회로** 간다 (sast/78 질문 5). ntdll 의 `RtlFailFast2`·`RtlpCallVectoredHandlers +0x15e/+0x163`·`RtlDispatchException +0x7f2` 등의 `INT 0x29` 가 이쪽. 유저 코드는 `int 0x29` 뒤를 못 본다.
- `RaiseFailFastException(레코드, 문맥, 플래그)` 는 **일반 syscall `NtRaiseException`** 으로 간다 (코드 기본 `0xC0000602`, 플래그 `1` = NONCONTINUABLE):
  - `+0x55 TEST RBX,RBX` / `+0x58 JNZ +0x18f`: 레코드를 받았으면 `+0x18f OR [레코드+4],1` (NONCONTINUABLE 켬). 없으면 `+0x6f MOV [RSP+0x40],0xc0000602` · `+0x77 [RSP+0x44],1`, 주소 = 반환 주소.
  - 문맥이 없으면 `+0xa1 RtlCaptureContext` (ret `+0xa8`) 로 잡고 `R14D = 1` (`+0xad`), 받았으면 `R14D = 0` (`+0x125`).
  - `+0xb3 CMP [BasepIsSecureProcess],0` / `+0xba JNZ +0x1ad` : secure 프로세스는 WER 건너뜀 (`+0x1bf NtRaiseException(…, FALSE)` ret `+0x1c6`, 또는 `R14 != 0` 이면 `+0x9dc3a RtlRaiseNoncontinuableException`).
  - 아니면 **`+0xc0 SignalStartWerSvc` (ret `+0xc5`)** (안쪽 `ZwQueryWnfStateNameInformation`·`ZwUpdateWnfStateData`·`EtwEventWriteNoRegistration`, 이름만) → `+0xd9 NtQuerySystemInformation` (`0x36`, 클래스 `0x73`, 8B, ret `+0xe0`) → `+0x12e WaitForWerSvc` (ret `+0x133`; 안쪽 `NtOpenEvent`·`NtWaitForSingleObject`·`NtClose`). 어느 하나라도 실패 (< 0) 거나 시간 초과 (`0x102`) 면 `+0xe9`: 플래그 `& 2` (NO_HARD_ERROR_DLG) 이면 `+0x9dc46 → TerminateProcess(-1, 코드)`, 아니면 `+0x113 NtRaiseHardError(코드 | 0x10000000, 0개, …, 옵션 1)` (ret `+0x11a`) 뒤 `TerminateProcess`.
  - 성공하면 `+0x13e XOR R8D,R8D` (**FirstChance = 거짓**) → `R14D == 0` (문맥 받음) 이면 **`+0x14c CALL NtRaiseException(레코드, 문맥, FALSE)` (ret `+0x153)`**, `R14D != 0` (여기서 잡음) 이면 `+0x181 RtlRaiseNoncontinuableException(레코드, 문맥, 0)` → 안에서 `R8B == 0` 이라 `+0x6b JMP NtRaiseException` (FALSE).
  - 즉 **유저 SEH·VEH 를 거치지 않고 (FirstChance 거짓) 커널 둘째 기회 → `ZwTerminateProcess(-1, 0xC0000602)`** (sast/78 질문 4).
- 호출하는 쪽: kernelbase `UnhandledExceptionFilter +0x39d` (WER 응답이 3·9·10 이거나 WER 를 못 불렀을 때, 질문 4 표 17번).

**지금 우리 (PE_emulator):** `int 0x29` 는 sast/78 규칙 (인터럽트 훅 → 곧바로 `0xC0000409` 종료) 그대로. `RaiseFailFastException` 은 진짜 kernelbase 가 돌고, 엔진이 줘야 하는 것은 `NtRaiseException(FALSE)` → 종료와 (WER 길을 안 타려면) `SignalStartWerSvc` 안쪽 syscall 의 오류 상태.

### 6. nt 엔진이 줄 답 표

(syscall 번호 = `Nt…` 스텁의 `MOV EAX,…`, 내가 `ntdll.dll` 에서 읽음. `KUSD+0x308` 비트 0 이 꺼지면 `syscall`, 켜지면 `int 0x2e`.)

| syscall (번호) | 언제 (자리 `call / ret`) | 인자 | 진짜가 기대하는 답 | 실패·다른 답이면 |
|---|---|---|---|---|
| **`NtContinue` (`0x43`)** | 커널이 쌓은 문맥 (`0x10001f`) 이 처리돼 돌아갈 때: `KiUserExceptionDispatcher +0x37/+0x3c` → `RtlGuardRestoreContext +0x1b6` → `RtlRestoreContext +0x185 / +0x18a`. (VEH `-1`, SEH `ExceptionContinueExecution` 둘 다.) 문맥 이름표가 `0x10000f` 가 아닐 때만 | `RCX` = CONTEXT* (= 디스패처 `RSP`, 핸들러가 고친 값 포함), `EDX = 0` (TestAlert 거짓) | **돌아오지 않음.** 엔진은 CONTEXT 로 레지스터를 복원: `Rip`·`Rsp`·`EFlags`·`CS`·`SS` + `Rax~R15` + `XMM`/`MxCsr` + (`0x10` 비트면) `Dr0~7` (run29·42) | 상태 반환 시: `0xC000060A` 면 `RtlRestoreContext +0x196 INT 0x29` (ECX=0x30), 그 밖은 `KiUserExceptionDispatcher +0x53/+0x55 RtlRaiseStatus(상태)` → (재귀) 결국 `NtRaiseException(FALSE)` |
| **`NtRaiseException` (`0x174`)** | (a) `KiUserExceptionDispatcher +0x4e/+0x53` (디스패치 FALSE) · (b) `RtlRaiseException +0x2a2/+0x2a7` (FALSE 또는 `BeingDebugged` 면 TRUE) · (c) `RtlUnwindEx +0x743/+0x748` (목적 프레임 못 찾음) · (d) `RtlRaiseNoncontinuableException +0x6b` (꼬리) · (e) kernelbase `RaiseFailFastException +0x14c/+0x153`·`+0x1bf/+0x1c6` | `RCX` = 레코드, `RDX` = 문맥, `R8B` = FirstChance | FirstChance **거짓**: 안 돌아옴 — sast/78 질문 4 (디버거·예외 포트 없음 → `ZwTerminateProcess(-1, ExceptionCode)`). **참**: sast/78 질문 3·4 (유저 문맥을 트랩 프레임에 써 넣고 `KiUserExceptionDispatcher` 로 다시) | 오류 상태 (개수 > 15 → `0xC000000D` 등) 가 돌아오면 `RtlRaiseStatus(상태)` |
| **`NtTerminateProcess` (`0x2c`)** | `RtlUserThreadStart +0x50/+0x55` (필터 1 → 되감은 뒤 `__except` 본문). `UnhandledExceptionFilter +0x578/+0x57f` (`BasepAlreadyHadHardError` 일 때). kernelbase `TerminateProcess` (`RaiseFailFastException` 끝, `+0x9dc4c`) | `RCX = -1` (현재 프로세스), `EDX` = 예외 코드 | **안 돌아옴** (run79: 자기 프로세스면 스레드들을 끝내고 자기도) | 돌아오면 `RtlUserThreadStart +0x55` 뒤로 흘러 `RET` (정의 안 된 길) |
| **`NtQueryInformationProcess` (`0x19`)** | (i) `BasepIsDebugPortPresent` — UEF `+0xd5`·`+0x202`·`+0x3b0` (`ProcessDebugPort` = 클래스 `7`, 8B) (ii) `GetErrorMode` — UEF `+0x29a` (`ProcessDefaultHardErrorMode` = 클래스 `0xc`, 4B) (iii) `RtlDecodePointer +0x4f` (`ProcessCookie` = 클래스 `0x24`, 4B, 캐시 없을 때만) | `RCX = -1` | (i) 버퍼 8B = **0**, `STATUS_SUCCESS` (디버거 없음) (ii) 4B = `EPROCESS.DefaultHardErrorProcessing` (비트 `0x2` 가 있으면 UEF 가 WER 없이 1 로 끝) (iii) 프로세스 쿠키 | (i) 오류면 "디버거 없음" 으로 침 (`+0x30 JS`) (ii) 오류면 값 0 으로 침 `[추측]` |
| **`NtSetInformationProcess` (`0x1c`)** | UEF `+0x110/+0x117` (값 8, `RtlKnownExceptionFilter != -1` 일 때) 와 `+0x282/+0x289` (값 1) | `RCX = -1`, 클래스 `0x3f`, 8바이트 버퍼, 길이 8 | `STATUS_SUCCESS` (반환값 안 봄) | 어떤 상태든 무시 |
| **`NtQueryInformationJobObject` (`0x156`)** | UEF `+0x2d1/+0x2d8` (비 secure·`GetErrorMode` 비트 없음·스레드 에러모드 `& 0x20` 없음일 때) | `RCX = NULL` (내 프로세스의 잡), 클래스 2, 버퍼 0x40, 반환 길이 NULL | 잡에 안 속하면 오류 상태 (예: `STATUS_ACCESS_DENIED` 류) | 오류 (< 0) 면 무시하고 WER 길로. 성공이고 `LimitFlags & 0x400` 이면 UEF 가 1 로 끝 |
| **`NtRaiseHardError` (`0x175`)** | UEF `+0x4ec/+0x4f3` (WER 가 안 정한 기본 길), `RaiseFailFastException +0x113/+0x11a` | `ECX = 0xD0000144`, `EDX = 4`, `R8 = 0`, `R9 = &{코드, 주소, 정보0, 정보1}`, 스택 `{옵션 1 또는 2, &응답}` | `STATUS_SUCCESS` + 응답 **6 (OK)** | 오류 심각도 (`& 0xC0000000 == 0xC0000000`) 상태면 UEF 가 응답을 6 으로 간주 (`+0x4f8 ~ +0x503`). 응답 3 (취소) 이면 AeDebug 쪽 |
| `NtQuerySystemInformation` (`0x36`) | 코드 `0xC0000409` 일 때만 UEF `+0x74/+0x79` (클래스 `0x23`, 2B); `RaiseFailFastException +0xd9/+0xe0` (클래스 `0x73`, 8B) | | 클래스 `0x23` : `{KernelDebuggerEnabled, NotPresent}` 두 바이트 (`[0]!=0 && [1]==0` 이 디버거 있음) | 오류면 "없음" |
| 안쪽 (평소 안 탐, 위 2(d)) | `NtQueryVirtualMemory` (`0x23`) · `NtAlertThreadByThreadIdEx` (`0x72`) · `NtWaitForAlertByThreadId` (`0x1e3`) · `NtSetInformationThread` (`0xd`) · `NtProtectVirtualMemory` (`0x50`) · `NtContinueEx` (`0xa5`, `STATUS_LONGJUMP`/`UNWIND_CONSOLIDATE` 일 때 `RtlContinueLongJump` 에서) | | | |
| 로더·WER (이름만) | UEF `+0x315 LoadLibraryExW`, delay-load `+0xb8`·`+0x345`, `WerpGetDebugger`, `VirtualAllocExNuma`/`VirtualFreeEx` (`NtAllocateVirtualMemoryEx 0x78` / `NtFreeVirtualMemory 0x1e` `[추측]`), `SignalStartWerSvc`/`WaitForWerSvc` (`NtQueryWnfStateNameInformation`·`NtUpdateWnfStateData`·`NtOpenEvent`·`NtWaitForSingleObject`·`NtClose`), WerFault (ALPC) | | | |

- **syscall 이 아예 없는 길 (표에 안 나오는 중요한 것):** 처리하는 쪽이 SEH `__except` 면 (`RtlUnwindEx` → `RtlRestoreContext` 의 문맥 `0x10000f`) **`NtContinue` 도 안 부르고 유저 `IRETQ` 로 끝난다**. `RtlRaiseException` 이 받아졌을 때도 같다. `NtContinue` 가 필요한 건 커널이 쌓은 문맥을 `KiUserExceptionDispatcher` 가 되돌릴 때 (VEH `-1` / SEH `ExceptionContinueExecution`) 뿐.

**지금 우리 (PE_emulator):** 표의 첫 세 줄 (`NtContinue`·`NtRaiseException`·`NtTerminateProcess`) 이 있어야 한다. 은퇴용 `NtContinue` 가 이미 있다면 같은 구현을 `KiUserExceptionDispatcher` 에서 오는 CONTEXT (flags `0x10001f`, `Dr0~7` 포함) 로 쓰면 된다. 나머지 (`NtQueryInformationProcess` 클래스 7·0xc·0x24, `NtSetInformationProcess` 0x3f, `NtQueryInformationJobObject`, `NtRaiseHardError`) 는 "UEF 까지 가는 프로그램" 에서만 필요.

## 조정자 덧붙임 3건 (run81 flow-writer)

**1. ntdll 이 `CONTEXT_EX` (`RSP+0x4d0`) 를 읽는 함수·줄, KUSD XState 가 켜져 있을 때 (`0xe7`) 커널이 `0x10001f` 로 쌓으면 괜찮은가, 머신 프레임 `CS`/`EFlags`/`SS` 를 읽나**

- **읽는 함수와 문턱 `[정적]`** — 전부 `ContextFlags` 가 `0x100040` (AMD64 + XSTATE) 둘 다 있어야 읽는다:
  - `RtlpSanitizeContext` (rva `0xcbfd0`): `+0x39 AND EAX,0x100040` · `+0x3e CMP EAX,0x100040` · `+0x43 JNZ +0xa0` (끝). 읽는 칸: `+0x45 [ctx+0x4d0]`, `+0x4b [ctx+0x4e0]`, `+0x55 [+0x4d4]`, `+0x5c [+0x4e4]`, `+0x78 [+0x4dc]`, `+0x8a [+0x4d8]`.
  - `RtlpCopyContext` (rva `0xccbe0`): `+0x1f AND EDX,0x100040` · `+0x25 CMP` · `+0x2b JZ +0x1ed` (XSTATE 갈래, `+0x1ff LEA RBX,[R9+0x4d0]`) — 복사 **받는 쪽과 주는 쪽이 둘 다** `0x40` 일 때만.
  - `RtlpVirtualPopShadowStack` (rva `0xd4760`, 바이트로 읽음): `ContextFlags & 0x100040 == 0x100040` 이고 `(KUSD+0x3d8 | KUSD+0x708) & 0x800` (CET_U) 일 때만 `ctx+0x4d0` 을 봄.
  - `RtlpUnwindPrologue +0x3b6` (`LEA RCX,[R14+0x4d0]` → `RtlLocateExtendedFeature2(…, 0xb)`): 풀고 있는 **지역 사본** 의 CONTEXT_EX.
  - `RtlRestoreContext +0x1a3 ~ +0x1e3` (`AND EAX,0x100040` → `[RCX+0x4e0]` 로 XSTATE 위치 → `XRSTOR`): 이름표에 `0x40` 이 있을 때만.
- **커널이 `0x10001f` (XSTATE 없음) 로 쌓으면:** 위 문턱이 전부 거짓이라 **ntdll 은 `+0x4d0` 의 `CONTEXT_EX` 를 읽지 않는다.** KUSD `XState` 가 켜져 있어도 (`0xe7`) 상관없다 — 디스패처가 만드는 지역 사본의 이름표는 `RtlDispatchException` 이 CET 일 때만 `0x10004b`, 아니면 `0x10000b` 로 정하고 (질문 2), `RtlRaiseException` 이 KUSD 를 보는 건 자기 문맥 크기를 정할 때뿐이다 (`+0x8d CMP [KUSD+0x3d8]`). 커널 문맥 이름표 `0x10001f` 자체는 `RtlpValidateContextFlags2` 가 허용한다 (`+0xa → +0x15` 에서 `flags & 0x27ffff80 == 0x10000`). 즉 sast/78 이 걱정한 "XSTATE 없이 쌓은 경우" 는 **ntdll 쪽에서 문제 없음** `[정적]`. (`0xe7` 에 비트 11 `0x800` 도 없음; `+0x708` 값은 모름.)
- **머신 프레임 (`C+0x590`) `CS`/`EFlags`/`SS` 를 읽나: 아니오.** `RtlpUnwindPrologue` 의 `UWOP_PUSH_MACHFRAME` 처리 (`+0x404 ~ +0x474`): `R8 = &프레임.Rip` (오류 코드 없으면 `프레임 +0x0`, 있으면 `+0x8`), `RDX = &프레임.Rsp` (`R12 = 0x18` 이라 `+0x18`, 오류 코드 있으면 `+0x20`) → `+0x45f ctx.Rip = [R8]`, `+0x469 ctx.Rsp = [RDX]` **둘만** 읽고 끝. `+0x8 CS`·`+0x10 EFlags`·`+0x20 SS` 는 안 읽음. 대신 두 칸 주소가 스택 범위 (`[StackLimit, StackBase-8]`) 안인지 본다 (`+0x440 ~ +0x561`), 벗어나면 `0xC0000028`. → 커널이 `+0x598`·`+0x5a0`·`+0x5b0` 을 안 써도 된다 (sast/78 질문 3 표와 일치).

**2. `RtlpContinuationContextMachineFrameEntries[0]` 와 머신 프레임 `+0x590` 의 관계 (되감기에 쓰는지)**

- `RtlpContinuationContextMachineFrameEntries` (rva `0x1cc208`, 8B × 2) 는 `InitSpecialMachineFrames +0x46` 이 쓰고 (run59: `[0]` = `KiUserExceptionDispatcher` 의 **`UNWIND_INFO` 주소**, `[1]` = `KiUserApcDispatcher`), 읽는 곳은 **`RtlpIsContinuationContextMachineFrameEntry` (rva `0xee350`, 36B) 하나**, 그것을 부르는 곳도 **`RtlpUnwindPrologue +0x477 / ret +0x47c` 하나** (`_refs` 로 확인).
- 되감기 때 쓰나: **예, 그러나 CET 그림자 스택용이다.** `RtlpUnwindPrologue` 가 `PUSH_MACHFRAME` 을 만나면 위 1 의 `Rip`/`Rsp` 를 꺼낸 **뒤** `+0x474 MOV RCX,RDI` (지금 풀고 있는 함수의 `UNWIND_INFO` 주소) → `+0x477 RtlpIsContinuationContextMachineFrameEntry` → `AL` 이 참이면 (= 이 함수가 `KiUserExceptionDispatcher` 또는 `KiUserApcDispatcher`) `+0x487 RtlpVirtualPopShadowStack(문맥)` (그림자 스택 한 칸 뺌), 거짓이면 건너뜀 (`+0x47e JZ +0x13b`). `RtlpVirtualPopShadowStack` 은 문맥 이름표에 `0x40` 이 없거나 CET_U 가 꺼져 있으면 첫 줄들에서 `RET`. → **test.exe (CET 꺼짐, `+0xa8 = 0`) 에서는 되감기 결과에 영향 없음.**
- 정리: 머신 프레임 `+0x590` 이 **걷기를 가능하게 하는 값** (Rip/Rsp 둘) 이고, `Entries[0]` 은 "이 프레임이 연속 문맥 프레임이다" 표시로 **그림자 스택 처리에만** 쓰인다. 엔진은 `Entries` 를 신경 쓸 필요 없음 (진짜 ntdll 이 `LdrpInitialize +0x22 → InitSpecialMachineFrames` 로 스스로 채움).

**3. test.exe 실제 길 후보 `OutputDebugStringA +0x77 RaiseException(0x40010006)` (run71)**

- `OutputDebugStringA` (kernelbase rva `0xc3df0`) `.pdata` scope (내가 읽음): **`__try [+0x42, +0x81)`**, 필터 `OutputDebugStringA$filt$0` (rva `0x1995f4`: `+0x1a CMP ECX,0x40010006` / `+0x20 SETZ AL` → **코드가 `0x40010006` 이면 1, 아니면 0**), 착지 `+0x81` (DBWin 프로토콜 몸통: `CreateDBWinMutex`·`WaitForSingleObjectEx`·`OpenFileMappingW(L"DBWIN_BUFFER")` … , sast/66). 그 뒤 `__try [+0x1a4, +0x29b)` 는 필터 `…$filt$1` (`!= 0xC00000FD`, 스택 넘침만 빼고 다 받음) + 정리 `…$fin$2`.
- `+0x6e MOV ECX,0x40010006` · `+0x73 LEA R8D,[RDX+2]` (개수 2) · **`+0x77 CALL RaiseException` (ret `+0x7c`)** → kernelbase `RaiseException +0x83 / +0x8a` → `RtlRaiseException` (질문 3). 플래그 0 (continuable).
- **`RtlRaiseException` 이 커널을 거치나: 디버거가 없으면 아니다** (`PEB.BeingDebugged == 0`). 커널 디버거 (KDNET) 만 붙은 VM 에서도 이 칸은 0 이라 `RtlDispatchException` 을 직접 부른다 (`+0x188`). 디버거가 붙어 있으면 `NtRaiseException(…, TRUE)` 로 커널이 먼저 본다.
- **디버거 없을 때 `0x40010006` 은 누가 받나:** VEH 가 없으면 (PEB 비트 2 = 0) SEH 걷기에서 첫 번째로 `OutputDebugStringA` 자기 프레임의 `__except` 가 필터 `$filt$0` 를 통과시켜 받는다 → `__C_specific_handler` → `RtlUnwindEx` → `RtlRestoreContext(0x10000f)` → **유저 `IRETQ`** → 착지 `+0x81` (DBWin 몸통). 중간에 커널·`NtContinue` 없음.
- **"test.exe 에서 예외 길을 VM 으로 볼 수 있나" 의 답:** **유저 쪽 길만 볼 수 있다.** 커널 `nt!KiDispatchException`/`KiUserExceptionDispatcher` 는 이 길에서 한 번도 안 불린다 (디버거 없음 → 커널 안 거침). 볼 수 있는 자리: `ntdll!RtlRaiseException+0x188` (call) · `ntdll!RtlDispatchException+0x205` (VEH 결과 `AL`) · `ntdll!RtlpExecuteHandlerForException+0xd` (`CALL RAX` 직전 `RAX = LanguageHandler`) · `ntdll!__C_specific_handler+0x93` (필터 반환 `EAX`) · `ntdll!RtlUnwindEx+0x606` · `ntdll!RtlRestoreContext+0x181` (`ZF` = 유저 `IRETQ` 로 감). 커널 쪽 (sast/78) 은 test.exe 에서 일부러 하드웨어 예외 (`int3`·0 나누기·잘못된 주소) 를 내는 작은 프로그램이 따로 필요하다 (이 저장소 `sample/` 에는 없음).

## 요청 26 전체 최소 규칙 (sast/78 커널 + 이번 유저)

전제: 64비트 exe, 유저 모드에서 난 예외, 디버거 (유저·커널) 없음, WER 없음 (또는 `ProcessDefaultHardErrorMode & 2`), 비 CET (`LdrSystemDllInitBlock +0xa8 = 0`, `+0xb8 = 0`), 계측 콜백·Pico 없음. 번호 앞 **[nt]** = 엔진이 직접, **[ntdll]** = 진짜 ntdll 이 Unicorn 안에서.

1. **[nt] CPU 예외가 남** (Unicorn 오류·인터럽트 훅) → 질문 78-2 표로 `ExceptionCode`·`ExceptionAddress`·`NumberParameters`·`ExceptionInformation` 을 정함 (`#PF` `0xC0000005` · `[0]` 읽기 0 / 쓰기 1 / 실행 8 · `[1]` 주소 · `#GP` 특권 `0xC0000096` · `#UD` `0xC000001D` · `#DE` `0xC0000094`/`0xC0000095` · `int3` `0x80000003` 주소 = `int3` 자리 + 문맥 `Rip` 도 `int3` 자리 · `ExceptionFlags = 0`).
2. **[nt] 문맥·배치 만들기** (78-3): `S0 = RSP`, `M = (S0 − 0x28) & ~0xF`, `E = M − 0xA0`, `X = E − 0x20`, **`C = X − 0x4D0`**. `C` 에 CONTEXT (`ContextFlags = 0x10001f`, `SegCs = 0x33`, `SegSs/Ds/Es/Gs = 0x2b`, `SegFs = 0x53`, `Rsp = S0`, `EFlags` = 순간 값), `C+0x4d0` 에 `CONTEXT_EX` 0x20B (XSTATE 없어도 씀; **ntdll 은 이름표에 `0x40` 이 없으면 안 읽으므로 값은 상관없음**), `C+0x4f0` 에 레코드 (0x98 을 0 으로 채우고 앞 0x20 + 개수×8), **`C+0x590` = `Rip`, `C+0x5a8` = `Rsp = S0`** (유저 걷기가 읽는 두 칸; `+0x598`·`+0x5a0`·`+0x5b0` 은 안 읽으니 안 써도 됨). 쓸 수 있는 주소가 아니면 78-3.6 (스택 넘침 `0xC00000FD` 한 번 더 / 그 밖 → 둘째 기회).
3. **[nt] 레지스터 바꾸기** (78-3.5): `RSP = C`, `RIP = KiUserExceptionDispatcher` (`PsNtdllExports +0x20`, ntdll rva `0x164e10`), `CS = 0x33`, `EFLAGS = 순간 값 & ~0x100`. **다른 레지스터는 그대로.** TEB `+0x8/+0x10` (스택 한계) 이 새 `RSP` 아래를 포함해야 함 (이번 질문 2: `RtlpGetStackLimits`).
4. **[ntdll] `KiUserExceptionDispatcher`**: `Wow64PrepareForException` 은 0 이라 건너뜀 → `RtlDispatchException(레코드 = RSP+0x4f0, 문맥 = RSP)` (질문 1).
5. **[ntdll] VEH**: PEB `+0x50` 비트 2 가 0 이면 즉시 지나감. 비트가 켜졌고 핸들러가 `-1` 을 돌려주면 TRUE.
6. **[ntdll] SEH 걷기**: `RtlLookupFunctionEntry` (`LdrInitState == 3` 이므로 syscall 없음) + `RtlpxVirtualUnwind` (머신 프레임 `C+0x590/+0x5a8` 로 예외 난 함수 프레임으로 넘어감) → 언어 핸들러 `__C_specific_handler` → 필터. 결과 갈래:
   - **필터가 1 (`__except` 받음)** → `RtlUnwindEx` → `RtlRestoreContext(문맥 0x10000f)` → **유저 `IRETQ` (syscall 없음).**
   - **VEH `-1` 또는 SEH `ExceptionContinueExecution`** (TRUE) → `RtlGuardRestoreContext` → `RtlRestoreContext(0x10001f)` → **`NtContinue(문맥, FALSE)`** [nt]: 문맥으로 레지스터 복원 (`Dr0~7` 포함), 안 돌아옴. `Noncontinuable` 인데 0 이면 `RtlRaiseStatus(0xC0000025)`.
   - **아무 데서도 안 받고 FALSE** → `NtRaiseException(레코드, 문맥, FALSE)` [nt] → 7.
7. **[ntdll→nt] 스레드 맨 위 `RtlUserThreadStart` 의 `__except`** (`filt$0` → 전역 `RtlpUnhandledExceptionFilter` = kernelbase `UnhandledExceptionFilter`): 평소 "아무도 안 받음" 은 여기서 잡힌다. UEF 가 부르는 syscall (질문 4 표): `NtQueryInformationProcess(ProcessDebugPort)` → 0 · `NtSetInformationProcess(0x3f)` ×2 → 성공 · `NtQueryInformationProcess(ProcessDefaultHardErrorMode)` → 값 (비트 `0x2` 있으면 여기서 1 로 끝) · `NtQueryInformationJobObject` → 오류 · (WER 길 / 고전 길은 이름만) → 반환 **1** → `__C_specific_handler` → `RtlUnwindEx` → `IRETQ` → `RtlUserThreadStart +0x4a` → **`NtTerminateProcess(-1, 예외 코드)`** [nt]: 프로세스를 끝냄, 종료 상태 = 예외 코드 (run79).
8. **[nt] 둘째 기회** (`NtRaiseException(…, FALSE)`, 위 6 의 FALSE, UEF 가 0 을 돌려줄 때): sast/78-4 → 디버거·예외 포트 없음 → `ZwTerminateProcess(-1, ExceptionCode)`. 쓰기 실패·`int 0x29` (`__fastfail`) 도 같은 끝 (유저 핸들러 안 탐, 코드 `0xC0000409`).
9. **[ntdll] 소프트웨어 예외** (`RaiseException`/`RtlRaiseException`): 커널을 안 거침 (`BeingDebugged == 0`). 잡기 → `RtlDispatchException` 직접 → 받으면 `RtlRestoreContext(0x10000f)` = 유저 `IRETQ` (syscall 없음), 못 받으면 `NtRaiseException(FALSE)` → 8. `RaiseFailFastException` 은 SEH·VEH 안 거치고 곧바로 `NtRaiseException(FALSE)` (+ WER 길의 syscall 이 실패해도 끝은 `TerminateProcess`).
10. **엔진이 만들어야 하는 syscall 최소 집합:** `NtContinue`, `NtRaiseException`, `NtTerminateProcess` (이 셋은 필수) + UEF 길용 `NtQueryInformationProcess` (클래스 7·0xc·0x24), `NtSetInformationProcess` (클래스 0x3f), `NtQueryInformationJobObject` (오류 반환), `NtRaiseHardError` (WER 안 쓰는 길일 때). 유저 `IRETQ` 를 Unicorn 이 CS=0x33/SS=문맥값 으로 실행할 수 있어야 함.
11. **안 흉내 내도 되는 것**: 디버거 갈래, WoW64 (`Wow64PrepareForException`), CET 그림자 스택 (`RtlpContinuationContextMachineFrameEntries`·`RtlpVirtualPopShadowStack`), XSTATE 꼬리, `RtlGuardCheckExceptionHandler` (`+0xb8 = 0`), 예외 로그, WER 본체.
12. **sast/78 규칙 7 의 고침**: 78 의 7 번 ("`NtRaiseException(…, FALSE)` … 은 프로세스 종료") 은 맞지만, **평소 "아무도 안 받음" 은 커널 둘째 기회에 안 가고 유저 `RtlUserThreadStart` 의 `__except` 에서 끝난다 (위 7).** 둘째 기회 길은 필터가 0 을 돌려주는 경우나 `RaiseFailFastException`·`int 0x29` 에서만.

## 동적분석으로 확인해야 할 것 (Q104 에 붙임, 이번 묶음엔 VM 안 함)

1. **UEF 가 정말 `1` 로 끝나는 길과 syscall 순서** — 예외를 일부러 내는 작은 test.exe (또는 `OutputDebugStringA` 에 필터를 걸지 않는 길) 가 필요. 멈춤점 (test.exe 조건): `kernelbase!UnhandledExceptionFilter+0x29f` (`GetErrorMode` 직후 `al`) · `+0x2d8` · `+0x34c` (`BasepReportFault` 반환 `eax`) · `+0x4f3` (`NtRaiseHardError` 반환) · `+0x59d` (최종 반환 `[rsp+3c]`). 질문 4 표의 apiset 있음·없음 (`+0xb1` 의 `al`) 과 11번 값 (`ProcessDefaultHardErrorMode`) 을 이 자리에서 본다.
2. **`LdrSystemDllInitBlock +0xa8/+0xb8` 가 0 인지** (test.exe 외 프로세스에서도) — 다르면 `RtlRestoreContext`·`RtlGuardCheckExceptionHandler`·`RtlDispatchException +0x280` 갈래가 바뀐다. 멈춤점: `ntdll!RtlRestoreContext+0x150` 에서 `dq ntdll!LdrSystemDllInitBlock+a0 l4`.
3. **`RtlRestoreContext +0x181` 이 정말 `ZF=1` 로 유저 `IRETQ` 로 가는지** — `__except` 로 받는 길과 `RtlRaiseException` 길에서. 멈춤점 `ntdll!RtlRestoreContext+0x174` 뒤 `eax`(= `ContextFlags & 0xffffff3f`) 가 `0x10000f` 인지, `+0x300 IRETQ` 에서 `rsp` 틀. 그리고 `KiUserExceptionDispatcher` 길에서는 `+0x185 NtContinue` 가 불리는지.
4. **`RtlRaiseException +0xa0` 의 `ecx`**: `0x10000b` 인지 `0x10004b` 인지 (KUSD `+0x3d8` XState 가 `0xe7` 이면 `0x10004b` 로 보임 — 이때 `RtlRaiseException` 의 문맥 크기와 `XRSTOR` 길을 탐).
5. **`RtlDispatchException +0x5f5` 의 `ecx` (disposition)** 와 `+0x205` 의 `al` (VEH) — VEH 를 건 프로그램에서. PEB `+0x50` 비트 2·3 이 켜지는 시점 (`RtlpAddVectoredHandler +0x1af`).
6. **`TEB.StackLimit` 갱신**: 스택이 모자란 예외에서 `RtlpGetStackLimits` 가 실패하는지 (`RtlDispatchException +0x263`), 커널이 스택을 늘릴 때 `TEB+0x10` 도 낮추는지 (sast/78 질문 3.6 과 이음).
7. **첫 `UnhandledExceptionFilter` 호출에서 delay-load 가 호스트 `kernel32.dll` 에 풀리는지** (`ntdll!LdrResolveDelayLoadedAPI` 안으로 들어가는지, 새 DLL 을 `NtCreateSection` 으로 로드하는지). 풀리는 이름은 `kernel32!CheckForReadOnlyResourceFilter`·`kernel32!BasepReportFault` 로 보이지만 `[정적: apisetschema.dll 표 + kernel32 `_index.tsv`]`, 실제 PEB `ApiSetMap` 확인은 동적.
8. **kernelbase `RaiseException` 의 `ExceptionAddress`** : `RtlRaiseException+0x188` 에서 `[레코드+10]` 이 `RaiseException+0x8a` 인지.
9. **`OutputDebugStringA` 길 (조정자 3)**: test.exe 가 실제로 부르는지, 부르면 `ntdll!RtlRaiseException+0x188 → RtlDispatchException+0x205 → __C_specific_handler+0x93 → RtlUnwindEx+0x606 → RtlRestoreContext+0x181` 차례와 `ntdll!NtRaiseException`·`NtContinue` 가 **한 번도 안 불리는지**.

추천 멈춤점 (모두 test.exe 조건 필요): `ntdll!KiUserExceptionDispatcher` (진입 `RSP`/`RSP+0x4f0`), `ntdll!RtlDispatchException+0x205`, `+0x5f5`, `ntdll!__C_specific_handler+0x93`, `ntdll!RtlUnwindEx+0x606`, `ntdll!RtlRestoreContext+0x174`·`+0x185`·`+0x300`, `ntdll!RtlRaiseException+0x188`·`+0x2a2`, `kernelbase!RaiseException+0x83`, `kernelbase!UnhandledExceptionFilter+0x29f`·`+0x34c`·`+0x4f3`.

## 판정

**분석성공.** 요청서 여섯 질문과 조정자 덧붙임 3건 모두 줄 근거로 답함 (질문 6 표 포함, "요청 26 전체 최소 규칙" 한 장 포함). 정적으로 못 정한 것은 위 "동적분석으로 확인해야 할 것" 에 모았다 (UEF 가 실제로 어디서 끝나는지, apiset·`ProcessDefaultHardErrorMode` 값, 유저 `IRETQ` 가 실제로 타는지). 알려진 사실 대조 일치, 스크립트 문제 없음 (참고 4건). 새로 알게 된 것 중 PE_emulator 에 가장 큰 영향: (1) `__except`/`RtlRaiseException` 이 받아지는 길은 **syscall 이 없다** (유저 `IRETQ`), (2) `NtContinue` 는 커널 문맥 (`0x10001f`) 을 되돌릴 때만, (3) 평소 "아무도 안 받음" 은 `UnhandledExceptionFilter` → 1 → `RtlUserThreadStart` 의 `NtTerminateProcess(-1, 코드)` 로 끝나고 커널 둘째 기회로 안 간다.

dast 요청 필요: 위 1·3·5·9 (예외를 일부러 내는 작은 test.exe + 멈춤점; `OutputDebugStringA` 길은 기존 test.exe 로 가능한지 먼저 확인).

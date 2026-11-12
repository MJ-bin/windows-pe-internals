# 79 exception-user — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q148b** (PE_emulator 요청 26 의 유저 쪽 — 4·5·7). 사용자 결정 2026-11-12: 요청 26 ~ 30 을 맨 앞, 26 이 가장 급함. 정적만 — VM 몫은 Q104 출처로.
- 원문: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-PE-emulator/d6bd3369-b666-4505-9231-47b12d2704dd/scratchpad/internals_requests/request26_cpu_exception_dispatch.md` (읽어도 됨).
- 앞 바퀴: sast/78 exception-kernel (Q148a — 커널이 유저 스택에 쌓는 모양·`KiUserExceptionDispatcher` 진입 값). **결과서 `_workspace/sast/78_exception-kernel_result.md` 를 먼저 읽고**, 진입 때 스택·레지스터 모양은 거기서 가져온다.
- 관련: run63 (`RtlInitializeHistoryTable` — `RtlUnwindEx`·`__C_specific_handler`·`RtlpExecuteHandlerForException`·`RtlDispatchException`·`RtlRaiseException`·`KiUserExceptionDispatcher` 가 함수 표에 있음, CFG·함수 표), run59 (`InitSpecialMachineFrames`·`RtlpContinuationContextMachineFrameEntries[0]` = `KiUserExceptionDispatcher` 쪽), run29·run42 (`NtContinue` 길), run79 (스레드·프로세스 끝내기)
- 빌드: 26100.9457 (ntdll·kernelbase). PDB GUID 가 다르면 멈추고 보고.

## 배경

- 은퇴 (`NtContinue`) 뒤엔 ntdll·kernelbase 가 PE_emulator 의 Unicorn 안에서 **진짜로** 돈다. 그러니 유저 쪽은 흉내가 아니라 **"진짜 ntdll 이 이 길에서 부르는 syscall 과, nt 엔진이 그 syscall 에 줘야 할 답"** 이 필요하다.
- 지금 우리 (PE_emulator): 소프트웨어 예외 (`RtlRaiseException`) 는 유저 안에서 돈다고 보고 있음 `[추측]`.

## 알고 싶은 것

1. **`KiUserExceptionDispatcher`** (ntdll rva `0x164e10`, 92B — `.asm` 있음): 진입 때 RSP 가 가리키는 것 (sast/78 표와 맞춤) 에서 `EXCEPTION_RECORD`·`CONTEXT` 주소를 어떻게 얻나, `Wow64PrepareForException` 같은 간접 호출이 있나 (64비트 프로세스에선 0?), `RtlDispatchException` 을 부르는 오프셋, 돌아온 값으로 갈리는 줄:
   - 처리됨 (TRUE) → `NtContinue` (·`RtlGuardRestoreContext`/`RtlRestoreContext`?) — 넘기는 인자 (`CONTEXT`, `TestAlert`).
   - 처리 안 됨 (FALSE) → `NtRaiseException(record, context, FirstChance = FALSE)`.
   - 그 syscall 이 돌아오면 (실패) 무엇을 하나 (`RtlRaiseStatus`?).
2. **`RtlDispatchException`** (rva `0xcb730`, 2197B) 의 큰 줄기:
   - (a) VEH 먼저: `RtlpCallVectoredHandlers` (rva `0xcc0c0`) 를 부르는 오프셋, VEH 목록이 비었을 때 바로 지나가는 줄 (PEB `ProcessUsingVEH` 비트? 전역 목록 머리?), 핸들러가 `EXCEPTION_CONTINUE_EXECUTION` (-1) 이면 바로 TRUE.
   - (b) SEH: 스택 한계 (`RtlpGetStackLimits`), 프레임마다 `RtlLookupFunctionEntry` → (`.pdata` 없으면 leaf 처리) → `RtlVirtualUnwind` → 언어 핸들러 (`__C_specific_handler`) 를 `RtlpExecuteHandlerForException` 으로 부름 — 오프셋. 핸들러를 못 찾고 스택 끝까지 가면 FALSE.
   - (c) 핸들러 검사 (CFG·SafeSEH 비슷한 `RtlIsValidHandler` 류 — 이름은 PDB 로) 가 실패하면.
   - (d) **이 길에서 syscall 이 있나** (없으면 "syscall 없음" 이 답). 있으면 표.
   - (e) VEH continue 핸들러 (`RtlpCallVectoredContinueHandlers` 류) 가 TRUE 뒤에 도나.
3. **`RtlRaiseException`** (rva `0xf700`, 685B) — 요청 26 의 7: `RtlCaptureContext` 로 문맥을 잡고 **커널을 안 거치고** `RtlDispatchException` 을 부르나 (예/아니오, 오프셋). 처리되면 `NtContinue`, 안 되면 `NtRaiseException(…, FirstChance = TRUE? FALSE?)` — 몇 번째 기회로 들어가나. kernelbase `RaiseException` (rva `0xc4140`) 이 여기로 오나.
4. **아무도 안 받을 때 (유저 쪽)**: 스레드 시작 `RtlUserThreadStart` (rva `0xacac0`, `.asm` 있음) 의 `__except` 필터 → `RtlUnhandledExceptionFilter`/`RtlUnhandledExceptionFilter2` (rva `0x122300`) → kernelbase `UnhandledExceptionFilter` (rva `0x10ed90`, 1402B) 로 가는 길. `UnhandledExceptionFilter` 가 **부르는 syscall** (`NtQueryInformationProcess(ProcessDebugPort)`, `NtRaiseHardError`?, WER 쪽 `NtAlpc*` 등) 과 차례, 디버거 없을 때 돌려주는 값 (`EXCEPTION_EXECUTE_HANDLER`) → 결국 `NtTerminateProcess(-1, 예외 코드)` 인가. **WER 본체는 이름만**.
5. **`__fastfail` 유저 쪽**: kernelbase `RaiseFailFastException` (rva `0x10a020`) 과 `int 0x29` 가 같은 길인가 (sast/78 질문 5 의 커널 쪽과 이음).
6. **nt 엔진이 줄 답 표**: 위 길에서 나오는 syscall 마다 (syscall · 언제 · 인자 · 진짜가 기대하는 답 · 실패면 어떻게 되나).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^KiUserExceptionDispatcher$` | 1 | 질문 1 — 이미 있음 |
| ntdll | `^RtlDispatchException$` (2197B) | 1 | 질문 2 — 줄기만, 프레임 걷기 안쪽은 오프셋만 |
| ntdll | `^RtlpCallVectoredHandlers$` (983B) | 0 | 질문 2(a)(e) |
| ntdll | `^RtlRaiseException$` (685B) | 1 | 질문 3 |
| ntdll | `^(RtlUserThreadStart|RtlUnhandledExceptionFilter|RtlUnhandledExceptionFilter2)$` | 0 | 질문 4 — `RtlUserThreadStart` 는 이미 있음 |
| kernelbase | `^(UnhandledExceptionFilter|RaiseException|RaiseFailFastException)$` | 1 | 질문 4·3·5 — `UnhandledExceptionFilter` 는 syscall 과 갈림 줄만 |

- 지도: `run_ghidra.sh map ntdll.dll KiUserExceptionDispatcher` (와 kernelbase `UnhandledExceptionFilter`).
- 줄기 밖 (`RtlVirtualUnwind`·`RtlLookupFunctionEntry` 안쪽·CFG 검사 안쪽·WER·ETW·SafeSEH 표) 은 이름만.

## 이미 아는 것

- `[정적]` run59: `_PS_NTDLL_EXPORTS +0x20` = `KiUserExceptionDispatcher` (rva `0x164e10`); `RtlpContinuationContextMachineFrameEntries[0]` = `KiUserExceptionDispatcher` 쪽.
- `[정적]` run63: 함수 표 (`RtlInitializeHistoryTable`) 에 `RtlUnwindEx`·`__C_specific_handler`·`RtlpExecuteHandlerForException`·`RtlDispatchException`·`RtlRaiseException`·`KiUserExceptionDispatcher`.
- sast/78 결과 (이 요청을 처리할 때 이미 있음): 커널이 쌓는 모양·진입 값·두 번째 기회의 커널 쪽·`__fastfail` 커널 쪽.
- `[추측]` (PE_emulator) `RtlRaiseException` 은 유저 안에서만.

## 받고 싶은 것

- 결과서 `_workspace/sast/79_exception-user_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- **질문 6 표** (syscall · 언제 · 인자 · 기대 답 · 실패면).
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 **"요청 26 전체 최소 규칙"** — sast/78 (커널) 과 이번 (유저) 을 이은 한 장: CPU 예외 → 커널 프레임 → `KiUserExceptionDispatcher` → VEH/SEH → `NtContinue` / 두 번째 기회 → 끝내기.
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다.

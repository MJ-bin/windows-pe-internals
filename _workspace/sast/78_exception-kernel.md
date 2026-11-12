# 78 exception-kernel — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q148a** (PE_emulator 요청 26 의 커널 쪽 — CPU 예외를 유저 예외로 넘기기). 사용자 결정 2026-11-12: 요청 26 ~ 30 을 맨 앞, 26 이 가장 급함 (PE_emulator 첫 실험 회차 샘플 9,176 개의 병목 후보). 정적만 — VM 몫은 Q104 출처로.
- 원문: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-PE-emulator/d6bd3369-b666-4505-9231-47b12d2704dd/scratchpad/internals_requests/request26_cpu_exception_dispatch.md` (읽어도 됨). 유저 쪽 (ntdll `KiUserExceptionDispatcher` 아래·kernelbase `UnhandledExceptionFilter`·`RtlRaiseException`) 은 다음 바퀴 Q148b — **이번엔 안 본다**.
- 관련: run42 (`KiDeliverApc +0x5ee → KiInitializeUserApc +0x1fb KeContextFromKframes(트랩, 예외, RDI)` — 유저 APC 가 유저 스택에 `CONTEXT` 를 쌓는 꼴, 예외 쪽과 비교하기 좋음), run59 (`_PS_NTDLL_EXPORTS +0x20 = KiUserExceptionDispatcher`), run55 (`RtlGetExtendedContextLength2/3` — 비 CET 스레드 문맥 길이 `0x4ff`, `CONTEXT` `0x4d0`), run16 (`KeContextToKframes`·`PspSetContext`), run77 (syscall 출구 트랩 프레임 `Rax` 칸), run80 (`KiRestoreSetContextState`·헤더 비트 27)
- 빌드: 26100.9457 (ntoskrnl). PDB GUID 가 다르면 멈추고 보고.

## 배경

- PE_emulator 는 Unicorn (CPU·주소 공간) + nt 엔진 (Unicorn 밖, 커널 몫). 은퇴 (`NtContinue`) 뒤엔 ntdll·kernelbase 가 Unicorn 안에서 진짜로 돈다. 그러니 **커널 쪽 (예외 프레임을 유저 스택에 쌓고 `KiUserExceptionDispatcher` 로 보내기) 을 nt 엔진이 흉내** 내야 하고, 쌓는 모양이 정확해야 한다.
- 지금 우리 (PE_emulator): CPU 예외는 Unicorn 오류 (`UC_ERR_READ_UNMAPPED`·`UC_ERR_INSN_INVALID` 등) 로 멈춤. 인터럽트 훅이 없어 `int3`·0 나누기도 멈춤. 메모리 syscall 이 잘못된 유저 포인터를 만나면 `STATUS_ACCESS_VIOLATION` 을 돌려주고 멈춤.
- 이번엔 **유저 모드에서 난 예외만**. 커널 모드 예외·디버거가 붙었을 때 갈래 (`DbgkForwardException` 이 받는 쪽) 는 넣지 않는다 — 디버거 없음 갈래만 따라가고 디버거 갈래는 갈리는 줄만 적는다.

## 알고 싶은 것

1. **입구 → 디스패치 사슬**: `KiPageFault` (rva `0x6bd540`) · `KiGeneralProtectionFault` (`0x6bd1c0`) · `KiBreakpointTrap` (`0x6ba740`) · `KiDivideErrorFault` (`0x6b9800`) · `KiInvalidOpcodeFault` (`0x6bb540`) · `KiDebugTrapOrFault` (`0x6b9b80`) → `KiExceptionDispatch` (`0x6c2700`) → `KiDispatchException` (`0x3eadc0`) 유저 모드 갈래 — 부르는 순서 (CALL 오프셋 + ret). `KiPageFault` 는 먼저 `MmAccessFault` 를 부르고 실패일 때만 예외로 가나 — 갈리는 줄. `KiPreprocessFault` (`0x3eb5e0`) 는 어디에 끼나.
2. **예외마다 `EXCEPTION_RECORD`**: 입구마다 `ExceptionCode` (상수와 줄), `ExceptionAddress` — **`int3` 은 `int3` 자리인가 다음 명령인가** (RIP 를 `-1` 하는 줄), `NumberParameters`·`ExceptionInformation[0..1]` (AV 의 읽기 0 / 쓰기 1 / 실행 8 값과 주소 — 페이지 폴트 오류 코드 비트에서 만드는 줄), 특권 명령 (`#GP`) · `ud2` (`#UD`) · 0 나누기 (`#DE`) 의 코드. 정수 오버플로·정렬 (`#AC`)·x87/SIMD 는 이름과 코드만.
3. **유저 스택에 쌓는 모양** (가장 중요): `KiDispatchException` 유저 갈래가
   - `CONTEXT` (`ContextFlags` 값 — `CONTEXT_FULL`/`CONTEXT_ALL`/XSTATE 포함?, 만드는 함수 `KeContextFromKframes` 를 부르는 오프셋), `EXCEPTION_RECORD`, 머신 프레임 (`MACHINE_FRAME`?), XSTATE 꼬리 (`CONTEXT_EX`) 를
   - 어떤 **차례·크기·정렬** 로 쌓나 (원래 RSP 에서 얼마를 빼나, `0x10`/`0x40` 정렬),
   - 유저 스택에 쓰는 함수 (`RtlCopyMemory`·`KiCopy*` 등) 와 쓰기 실패 (스택 모자람·잘못된 RSP) 때 갈래 — 상태 코드, 두 번째 기회로 가나.
   - 그다음 트랩 프레임을 고치는 줄: 새 `Rsp`, 새 `Rip` = `PsNtdllExports+0x20` (`KiUserExceptionDispatcher`) 인가 (`KeUserExceptionDispatcher` 전역이면 그 이름), `SegCs`·`EFlags` 등. `KiUserExceptionDispatcher` 가 시작할 때 레지스터에 무엇이 들어 있나 (RSP 가 `CONTEXT` 를 가리키나, RCX/RDX 를 쓰나).
   - **표 하나로**: (RSP 기준 오프셋 · 크기 · 무엇 · 값).
4. **두 번째 기회 (커널 쪽)**: 유저가 `NtRaiseException(…, FirstChance = FALSE)` 로 다시 오면 (`NtRaiseException` rva `0x6ae280` → `KiRaiseException` `0x3e9410` → `KiDispatchException` 둘째 기회) 디버거 없을 때 무엇을 하나 — 프로세스를 끝내는 줄과 상태 코드 (예외 코드 그대로?), 부르는 함수 (`ZwTerminateProcess`·`PspTerminateProcess` 등 — run79 끝내기와 이어서).
5. **`__fastfail` (`int 0x29`)**: `KiRaiseSecurityCheckFailure` (`0x6c05c0`) → `KiFastFailDispatch` (`0x6c2c40`) 길 — 유저 예외로 보내나 (`STATUS_STACK_BUFFER_OVERRUN` `0xC0000409`, 첫 기회 없이?) 아니면 바로 끝내나, `ExceptionInformation[0]` = 실패 코드 (`RCX`).
6. (덤) 메모리 syscall 이 유저 포인터를 읽다 실패할 때 (예: `ProbeForRead` 실패) 는 **syscall 이 상태 코드를 돌려주는 것** 인지 유저 예외가 나는 것인지 — 한 줄 (PE_emulator `Src/nt/Process.cpp:1953` 주석 "진짜는 SEH `+0x2ab` → 유저 예외 `KiDispatchException`" 이 맞나).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^(KiPageFault|KiGeneralProtectionFault|KiBreakpointTrap|KiDivideErrorFault|KiInvalidOpcodeFault|KiDebugTrapOrFault)$` | 1 | 질문 1·2 — 손으로 쓴 asm 입구 (각 ~0x150B). 조각 (`frag:`) 오프셋 주의 |
| ntoskrnl | `^KiExceptionDispatch$` (844B) | 1 | 질문 1·3 |
| ntoskrnl | `^KiDispatchException$` (2044B) | 1 | 질문 1·3·4 — 유저 갈래만, 커널 모드·디버거 갈래는 갈리는 줄만 |
| ntoskrnl | `^(KiPreprocessFault|KeCopyExceptionRecord)$` | 0 | 질문 1·2 |
| ntoskrnl | `^(NtRaiseException|KiRaiseException)$` | 0 | 질문 4 |
| ntoskrnl | `^(KiRaiseSecurityCheckFailure|KiFastFailDispatch)$` | 0 | 질문 5 |
| ntoskrnl | `^KeContextFromKframes$` | 0 | 질문 3 — 이미 있으면 다시 안 뽑음, `ContextFlags` 줄만 |

- 지도: `run_ghidra.sh map ntoskrnl.exe KiDispatchException` (와 `KiExceptionDispatch`).
- 줄기 밖 (ETW·감사·WoW64·CET 그림자 스택·`DbgkForwardException` 안쪽·`MmAccessFault` 안쪽) 은 이름만. CET 이 쌓는 모양을 바꾸면 그 갈래는 갈리는 줄만 (test.exe 는 비 CET — run55).

## 이미 아는 것

- `[정적]` run59: `_PS_NTDLL_EXPORTS` `+0x20` = `KiUserExceptionDispatcher` (ntdll rva `0x164e10`).
- `[정적]` run42: 유저 APC 는 `KiDeliverApc +0x5ee → KiInitializeUserApc +0x1fb KeContextFromKframes(트랩, 예외, RDI)` 로 원래 문맥을 유저 스택 `CONTEXT` 에 쌓는다 — 예외 쪽도 같은 함수를 쓰는지 비교.
- `[정적]` run55: 비 CET 스레드 `CONTEXT` `0x4d0`, 맡긴 문맥 길이 `0x4ff`.
- `[정적]` run77: syscall 출구는 트랩 프레임 `Rax` 칸 (`[RBP-0x50]`) 을 다시 읽어 `SYSRET`. run80: 헤더 비트 27 → `KiRestoreSetContextState` → `IRETQ`.
- `[추측]` (PE_emulator) 메모리 syscall 의 잘못된 포인터 → 진짜는 SEH → 유저 예외.

## 받고 싶은 것

- 결과서 `_workspace/sast/78_exception-kernel_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- **질문 3 은 표 하나** (RSP 기준 오프셋 · 크기 · 무엇 · 값) + 트랩 프레임에서 바뀌는 칸 표.
- 질문 2 는 표 (예외 · 입구 함수 · `ExceptionCode` · `ExceptionAddress` · 매개변수).
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 **"nt 엔진이 흉내 낼 최소 규칙"** (CPU 예외 → 유저 스택 프레임 → `KiUserExceptionDispatcher` 진입 값).
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다 (이번 묶음엔 VM 안 함).

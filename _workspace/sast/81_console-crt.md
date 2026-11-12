# 81 console-crt — 정적 분석 요청

- 날짜: 2026-11-13
- 스프린트: 03_sprint
- 대기열: **Q149b** (PE_emulator 요청 27 의 CRT·condrv 쪽과 최소 조건). 사용자 결정 2026-11-12: 요청 26 ~ 30 맨 앞, 27 은 2순위. 정적만 — VM 몫은 Q104 출처로.
- 원문: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-PE-emulator/d6bd3369-b666-4505-9231-47b12d2704dd/scratchpad/internals_requests/request27_console_io.md` (읽어도 됨).
- 짝 바퀴: sast/80 console-kernelbase (Q149a — kernelbase 쪽: 콘솔 얻는 길 셋, API → IOCTL 표, `GetFileType`, 핸들 0·리다이렉트). **sast/80 은 지금 다른 decompiler 가 도는 중** — 결과서 `_workspace/sast/80_console-kernelbase_result.md` 가 생기면 읽고 겹치는 건 가리키기만. 아직 없으면 condrv 쪽 IOCTL 목록은 condrv 자기 디스패치에서 뽑는다.
- 빌드: 26100.9457. **ucrtbase.dll (1.3MB)·`drivers/condrv.sys` (90KB) 는 아직 analyze 안 됨** (`ms/ghidra/` 에 `.gpr` 없음) → 먼저 analyze (PDB 받기 포함). PDB GUID 를 결과서에 적는다. kernelbase 는 sast/80 이 쓰는 중이니 **이번엔 kernelbase 프로젝트를 열지 않는다** (필요한 kernelbase 줄은 sast/80 결과서·이미 있는 `.asm` 으로).

## 배경

- PE_emulator 첫 실험 회차 정상 대상 약 7 천 개가 콘솔 프로그램. 은퇴 뒤엔 kernelbase·CRT 가 Unicorn 안에서 진짜로 돌고 syscall 만 nt 엔진·팀원 몸이 대답. 지금 우리: `ConsoleHandle`·표준 핸들 0, test.exe 는 `NtWriteFile(0)` 26 번 `0xc0000008`.
- PE_emulator 가 고를 것: **(가)** 표준 출력을 파일로 리다이렉트된 것처럼 세우기 / **(나)** 가짜 condrv 콘솔 — 각각의 최소 조건이 필요.

## 알고 싶은 것

1. **CRT 가 처음 핸들 종류를 묻는 길** (ucrtbase): 프로세스 시작 때 lowio 초기화 (`__acrt_initialize_lowio`·`initialize_stdio_handles_nolock` 류 — 이름은 PDB 로) 가 `GetStdHandle`·`GetFileType` 을 부르는 줄, 결과를 저장하는 칸 (파일 핸들 표의 `osfile` 플래그 — `FDEV`·`FPIPE` 류). 표준 핸들이 0 / `INVALID_HANDLE_VALUE` 일 때 그 칸을 어떻게 두나 (`_NO_CONSOLE_FILENO` (-2)?).
2. **`printf`·`puts`·`std::cout` (CRT 쪽 `_write`)**: 콘솔 (`FDEV` + `GetConsoleMode` 성공) 이면 `WriteConsoleW` (유니코드 변환), 아니면 `WriteFile` — 갈림 줄. `_isatty` 가 보는 것. 콘솔 모드 확인 (`GetConsoleMode`) 을 언제 부르나 (첫 쓰기 때? 매번?).
3. **표준 핸들이 0 (콘솔 없음) 일 때 CRT 결과**: `printf` 가 조용히 버리나 (반환값·`errno`), 실패로 끝나나 (`abort`?), 프로그램이 계속 도나.
4. **condrv.sys 가 받는 IOCTL** (선택, 대조용): 디스패치 (`IRP_MJ_CREATE`·`IRP_MJ_DEVICE_CONTROL`·`IRP_MJ_WRITE`) 에서 받는 IOCTL 코드 목록과 이름 (PDB 로) — sast/80 의 kernelbase 쪽 IOCTL 과 짝지음. condrv 안쪽 (conhost 로 넘기는 길) 은 이름만.
5. **최소 조건 표 둘** (가장 중요 — sast/80 결과와 합쳐서):
   - **(가) 파일 리다이렉트처럼**: 매개변수 블록 `StandardOutput`/`StandardError` (·`StandardInput`) 에 둘 핸들, `ConsoleHandle`·`ConsoleFlags` 값, 열어 둘 핸들의 종류, `NtQueryVolumeInformationFile(FileFsDeviceInformation)` 에 줄 답 (`DeviceType` = 디스크·`FILE_DEVICE_NAMED_PIPE` 등), `NtWriteFile` 에 줄 답 — CRT·kernelbase 가 이것만으로 끝까지 가나.
   - **(나) 가짜 condrv 콘솔**: `ConsoleHandle` 값, `\Device\ConDrv\…` 열기 답, 꼭 답해야 하는 IOCTL 과 그 답 (`GetConsoleMode` 성공·`GetConsoleOutputCP` 값·`WriteConsole` 이 쓴 글자 수 등).
   - 표: (항목 · 값 · 근거 줄 · 이게 없으면 무슨 일).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ucrtbase | lowio·stdio 초기화 함수 (`__acrt_initialize_lowio`·`initialize_inherited_file_handles_nolock`·`initialize_stdio_handles_nolock` 류, 이름은 `_index.tsv` 로) | 1 | 질문 1 |
| ucrtbase | `_write`·`_write_nolock`·`write_text_ansi_nolock`·`write_double_translated_ansi_nolock` 류, `_isatty` | 1 | 질문 2·3 — 갈림 줄만 |
| ucrtbase | `printf`·`puts` → `_write` 까지 사슬 (이름만) | 0 | 질문 2 |
| condrv.sys | `DriverEntry`·디스패치 표, `IRP_MJ_DEVICE_CONTROL` 처리 함수 | 1 | 질문 4 — IOCTL 목록만 |

- analyze: `run_ghidra.sh analyze ucrtbase.dll` · `analyze drivers/condrv.sys` (경로는 스킬 규칙대로). 지도는 질문 1 의 초기화 함수에서.
- C++ `std::cout` 은 msvcp140 쪽이라 덤프에 있으면 이름만, 없으면 "CRT `_write` 로 모인다 `[추측]`" 한 줄.
- 줄기 밖 (로캘·멀티바이트 변환 안쪽·`_set_invalid_parameter_handler`) 은 이름만.

## 이미 아는 것

- `[정적]` run68 (sast/63): test.exe `ConsoleHandle = 4` → 붙기 갈래, `NtCreateFile(<4> 기준 "\Connect")`, `ConsoleCommitState` → `ConsoleHandle` `0x60`.
- `[관찰 — PE_emulator 흉내]` 부모 없음 → 표준 핸들 0 → `NtWriteFile(0)` 26 번 `0xc0000008`.
- sast/80 결과 (있으면): kernelbase 쪽 API → IOCTL 표, `GetFileType` syscall, 핸들 0·리다이렉트 갈래.

## 받고 싶은 것

- 결과서 `_workspace/sast/81_console-crt_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`. ucrtbase·condrv PDB GUID.
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- **질문 5 의 (가)·(나) 최소 조건 표** — 요청 27 의 마지막 답.
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다.

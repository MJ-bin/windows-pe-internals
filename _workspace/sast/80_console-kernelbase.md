# 80 console-kernelbase — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q149a** (PE_emulator 요청 27 의 kernelbase 쪽 — 콘솔 입출력 길). 사용자 결정 2026-11-12: 요청 26 ~ 30 맨 앞, 27 은 2순위 (PE_emulator 첫 실험 회차 정상 8,056 개 중 약 7 천 개가 콘솔 프로그램). 정적만 — VM 몫은 Q104 출처로. CRT (ucrtbase)·condrv.sys·최소 조건 표는 다음 바퀴 Q149b — **이번엔 안 본다**.
- 원문: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-PE-emulator/d6bd3369-b666-4505-9231-47b12d2704dd/scratchpad/internals_requests/request27_console_io.md` (읽어도 됨).
- 관련: run68 (sast/63 — kernelbase `ConsoleInitialize` (`0xbeb60`): test.exe 는 `ConsoleHandle = 4`·`ConsoleFlags = 0` → "부모 콘솔에 붙기" 갈래, `NtCreateFile(<핸들 4> 기준 "\Connect")` → `ConsoleCommitState` 가 매개변수 `ConsoleHandle` 을 `0x60` 으로; `ConsoleAllocate` (새 콘솔) 는 `ConsoleHandle` 이 0·−1·−2·−3 일 때만 `+0x170 ~ +0x185`), run51 (`ConsoleCommitState` 쪽), 대기열 Q84 (P1a ~ P1b `ConsoleHandle` 4 → `0x60` 순간, 동적)
- 빌드: 26100.9457 (kernelbase). PDB GUID 가 다르면 멈추고 보고. kernelbase 는 이미 analyze 됨.

## 배경

- 은퇴 뒤엔 kernelbase·CRT 가 PE_emulator 의 Unicorn 안에서 진짜로 돌고, syscall (`NtCreateFile`·`NtDeviceIoControlFile`·`NtWriteFile` …) 만 nt 엔진·팀원 몸이 대답한다. "가짜 콘솔" 을 세우려면 **kernelbase 가 condrv 에 무엇을 보내고 어떤 답을 기대하는지**, 매개변수 블록 칸에 무엇이 있어야 하는지가 필요하다.
- 지금 우리 (PE_emulator): 부모가 없어 `ConsoleHandle`·표준 핸들 모두 0. test.exe 는 은퇴 뒤 `NtWriteFile(핸들 0)` 이 26 번 `0xc0000008` (팀원 몸). `ConsoleCommitState` 쪽은 공사 중.

## 알고 싶은 것

1. **콘솔을 얻는 길 셋** — `ConsoleInitialize` (이미 있음) 와 그 아래 (`ConsoleCreateConnectionObject`·`ConsoleCommitState`·`ConsoleAllocate`·`ConsoleLaunchServerProcess`, 모두 `_index.tsv` 에 있음):
   - (a) **부모가 물려줌** (`ConsoleHandle` = 진짜 핸들): run68 의 길을 syscall 표로 — `NtCreateFile` 이름 (`\Connect`?)·`RootDirectory`·접근·`EaBuffer` (연결 정보 — 모양과 크기), 이어지는 `NtDeviceIoControlFile` (IOCTL 코드·입력·출력 모양), `ConsoleCommitState` 가 쓰는 매개변수 블록 칸 (`ConsoleHandle`·`StandardInput/Output/Error`·`ConsoleFlags`) 과 PEB 칸.
   - (b) **새 콘솔** (`ConsoleHandle` = −1/−2/−3 류, `CREATE_NEW_CONSOLE`): `ConsoleAllocate` → `ConsoleLaunchServerProcess` (conhost 띄우기 — 쓰는 syscall·이름만), 연결.
   - (c) **콘솔 없음** (`ConsoleHandle` = 0, `DETACHED_PROCESS`): 어디서 갈리고 무엇을 안 하나. 표준 핸들은 무엇이 되나 (매개변수 블록 값 그대로?).
   - PE_emulator 는 (c) 상태 (모두 0) — 이 갈래가 `ConsoleInitialize` 를 성공 (AL 1) 으로 끝내나.
2. **자주 쓰는 콘솔 API 표**: `GetStdHandle` (`0xec300`) · `WriteFile` (`0xb2070`) / `WriteConsoleW` (`0xf7c80` → `WriteConsoleInternal`) · `ReadFile` (`0x22a90`) / `ReadConsoleW` (`0xdeef0`) · `GetConsoleMode` (`0xf2860`) / `SetConsoleMode` (`0xbe6f0`) · `GetConsoleScreenBufferInfo` (`0xbe7f0`, `Ex` `0xbe870`) · `GetConsoleOutputCP` (`0xb9db0`) / `SetConsoleOutputCP` (`0xbe1a0`):
   - 각각 → 공통 경로 (`ConsoleCallServer`·`ConsoleCallServerGeneric`·`ConsoleCallServerWithBuffers`) → **syscall · IOCTL 코드 · 입력 버퍼 모양 (API 번호·크기) · 기대하는 답 (출력 모양·상태 코드) · 실패면 무엇을 돌려주나**.
   - `WriteFile` 이 콘솔 핸들이면 condrv 로 가고 아니면 `NtWriteFile` 로 가는 갈림 줄 (핸들 하위 비트? `ConsoleHandle` 비교?).
3. **`GetFileType`** (`0xc6290`): 부르는 syscall (`NtQueryVolumeInformationFile(FileFsDeviceInformation)`?) 과 콘솔 핸들일 때 갈래 (`FILE_TYPE_CHAR`), 핸들 0 / 잘못된 핸들일 때 돌려주는 값.
4. **표준 핸들이 0 일 때** (콘솔 없음): `GetStdHandle` 은 0 을 그대로 주나, `WriteFile(0, …)` 는 syscall 까지 가나 (`NtWriteFile(0)` → `0xc0000008`) 아니면 kernelbase 가 먼저 막나 — 줄.
5. **표준 출력이 파일·파이프일 때**: `WriteFile` 이 `NtWriteFile` 만 부르나, condrv 가 끼나 (예/아니오, 줄).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernelbase | `^(ConsoleInitialize|ConsoleCommitState|ConsoleAllocate|ConsoleCreateConnectionObject)$` | 1 | 질문 1 — 앞 둘은 이미 있음 |
| kernelbase | `^ConsoleLaunchServerProcess$` | 0 | 질문 1(b) — syscall·이름만 |
| kernelbase | `^(ConsoleCallServer|ConsoleCallServerGeneric|ConsoleCallServerWithBuffers)$` | 1 | 질문 2 — IOCTL·버퍼 모양 |
| kernelbase | `^(GetStdHandle|WriteFile|WriteConsoleW|WriteConsoleInternal|ReadFile|ReadConsoleW|GetConsoleMode|SetConsoleMode|GetConsoleScreenBufferInfo|GetConsoleScreenBufferInfoEx|GetConsoleOutputCP|SetConsoleOutputCP|GetFileType)$` | 0 | 질문 2·3·4·5 — 갈림 줄·API 번호만 |

- 지도: `run_ghidra.sh map kernelbase.dll ConsoleInitialize` (와 `ConsoleCallServerGeneric`).
- 함수가 많다. **질문에 답하는 줄만** 읽는다. conhost 안쪽·렌더링·ConPTY·콘솔 창 (GUI) 은 넣지 않는다.
- PDB 에 `CONSOLE_*` 구조체·API 번호 열거형이 있으면 이름을 쓰고, 없으면 `[추측: 이름]`.

## 이미 아는 것

- `[정적]` run68 (sast/63): test.exe `ConsoleHandle = 4`, `ConsoleFlags = 0` → 붙기 갈래, `NtCreateFile(<핸들 4> 기준 "\Connect")`, `ConsoleCommitState` 가 `ConsoleHandle` 4 → `0x60`. `ProcessGroupId` 는 0 일 때만 씀 (test.exe `0x2f0`). `ConsoleAllocate` 는 `ConsoleHandle` 0·−1·−2·−3 일 때 (`ConsoleInitialize +0x170 ~ +0x185`).
- `[관찰 — PE_emulator 흉내]` 부모 없음 → `ConsoleHandle`·표준 핸들 0 → 은퇴 뒤 `NtWriteFile(0)` 26 번 `0xc0000008`.

## 받고 싶은 것

- 결과서 `_workspace/sast/80_console-kernelbase_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- **질문 1 은 갈래별 syscall 표**, **질문 2 는 표 (API · syscall · IOCTL · 입력 · 기대 답 · 실패면)**.
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 Q149b 로 넘길 것 (condrv 쪽에서 확인할 IOCTL 목록, CRT 가 부를 API 목록).
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다.

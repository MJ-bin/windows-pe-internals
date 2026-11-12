# 2026-11-13 run84 — 콘솔 입출력, CRT · condrv 쪽과 최소 조건 (Q149b, PE_emulator 요청 27 마지막 바퀴): **요청 27 결론 — 콘솔 프로그램이 화면에 쓰는 길은 거의 다 `NtWriteFile` 하나다. CRT 는 시작 때 표준 핸들마다 `GetStdHandle` (syscall 없음) · `GetFileType` (= `NtQueryVolumeInformationFile(FileFsDeviceInformation)`) 두 개만 묻고, 기본 (C 로캘) `printf` 는 콘솔이어도 `WriteConsoleW` 가 아니라 `WriteFile` → `NtWriteFile` 이며, kernelbase `WriteFile` 도 콘솔인지 안 가른다 (run83). 막히는 곳은 쓰기가 아니라 시작이다 — 콘솔 앱 + `ConsoleHandle 0` 이면 kernelbase 가 `ConsoleAllocate` (condrv 필요) 로 가고, 그게 실패하면 kernelbase 시작 함수가 FALSE 다 (run83). 그래서 PE_emulator 가 고를 길은 둘: (가) `ConsoleHandle = −1` + 표준 핸들에 파일 핸들 (syscall 은 `NtSetInformationProcess` 1 + 핸들마다 `NtQueryVolumeInformationFile` + `NtWriteFile`) / (나) 가짜 condrv (앱이 condrv 에 보내는 IOCTL 은 `0x500016` · `0x500023` · `0x500037` 셋뿐, 나머지는 conhost 몫). 표준 핸들이 0 이면 CRT `printf` 는 abort 없이 조용히 버려지고 (`NtWriteFile` 0번), test.exe 의 `NtWriteFile(0)` 26번은 CRT 가 아니라 `sample/log.h:38` 의 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)` 이다.** ucrtbase `initialize_stdio_handles_nolock +0x62 GetStdHandle` · `+0xc6 GetFileType` · `_write_nolock +0xc5 write_requires_double_translation_nolock` → `+0x190 write_text_ansi_nolock` → `+0xc1 WriteFile` · condrv `DriverEntry +0x128 IoCreateDevice(…, DeviceType 0x50, …)`

## PE_emulator 쪽에서 쓸 것

### 요청 27 전체 결론 (run83 + run84)

- **쓰기는 `NtWriteFile` 한 곳이면 된다** `[정적]`.
  - kernelbase `WriteFile` 은 콘솔 · 파일 · 파이프를 안 가르고 `+0x86 NtWriteFile` 로 간다 (run83).
  - CRT `printf` · `puts` · `fwrite(stdout)` 도 기본 (C 로캘, `_setmode` 안 함) 이면 `_write_nolock +0x190 write_text_ansi_nolock` → `+0xc1 WriteFile` 이다. `GetConsoleMode` 도 안 부른다 (이번).
  - 그러니 에뮬레이터에서 "콘솔" 을 아는 곳은 `NtWriteFile` 몸 하나면 된다. 콘솔 IOCTL (`0x500016`) 은 앱이 콘솔 API (`GetConsoleMode` · `WriteConsoleW` 등) 를 직접 부를 때만 나간다.
- **CRT 가 시작 때 묻는 것은 둘뿐** `[정적]`: 표준 핸들마다 `GetStdHandle` + `GetFileType`. 답 (`DeviceType`) 이 `0x50` 이면 CRT 는 "콘솔류 (FDEV)" 로 보고, 그 밖이면 파일 · 파이프로 본다.
- **막히는 곳은 시작이다** `[정적]` (run83): 콘솔 앱 + `ConsoleHandle = 0` 이면 `ConsoleInitialize +0x177 JZ → +0x146 ConsoleAllocate` (condrv 쪽 syscall 8개 — run83 b1 ~ b8). `ConsoleAllocate` 가 실패를 돌려주면 `_KernelBaseBaseDllInitialize +0x526 TEST AL` · `+0x528 JZ` → kernelbase 시작 함수 FALSE.
- **고를 길은 둘** (아래 (가) · (나) 표):
  - (가) 파일 리다이렉트처럼 — **`ConsoleHandle = −1` 이 꼭 있어야** condrv 를 안 거친다. 표준 핸들만 파일로 바꾸고 `ConsoleHandle` 을 0 으로 두면 그래도 `ConsoleAllocate` 로 간다.
  - (나) 가짜 condrv — 지금 값 (`ConsoleHandle 0`) 그대로 엔진이 syscall 답만 주는 길 B 가 가장 가깝다 (`params` 안 바꿈, 표준 핸들은 kernelbase 가 채움).
- **표준 핸들 0 이면 CRT 는 걱정 없다** `[정적]`: 시작 성공, `printf` 는 글자를 `stdout` 버퍼에 쌓고, 비울 때 `_write_internal +0x29 CMP EBX,-2` → `errno 9` · −1 로 조용히 실패. abort · 예외 없음, `NtWriteFile` 은 한 번도 안 나간다.
- **test.exe 의 `NtWriteFile(0)` 26번은 CRT 가 아니다** `[소스]` + `[정적]`: `Log` 가 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), buf, i, &written, NULL)` 을 직접 부른다 (`sample/log.h:38`, `printf` 는 일부러 안 씀 `log.h:2`, 빌드 `/MT` `sample/build.bat:12`). 표준 출력 칸이 0 이라 `GetStdHandle` → 0 → `WriteFile +0x86 NtWriteFile(0)` (run83). 26 이 `Log` 횟수와 같은지는 여전히 안 셌다 `[추측]`.

### 원문 질문이 어디서 답해졌나

| 원문 요청 27 | 답한 곳 |
|---|---|
| 1 콘솔을 얻는 길 셋 | run83 "질문 1" (갈래별 syscall 표 (a) · (b) · (c)) |
| 2 자주 쓰는 콘솔 API 표 | run83 표 A · B · C |
| 3 CRT 가 핸들 종류를 묻는 길 · 갈라 쓰는 자리 | **run84** (아래 질문 3 · 2) + run83 질문 3 (`GetFileType`) |
| 4 콘솔이 없을 때 | run83 질문 4 (kernelbase) + **run84** 질문 3 (CRT) |
| 5 리다이렉트 | run83 질문 5 (kernelbase) + **run84** (CRT 도 `WriteFile` 만) |
| 목표 (가) · (나) 최소 조건 | **run84** (아래 표) |

### 질문마다 (결과서 sast/81 그대로, 근거 줄은 오케스트레이터 · flow-writer 가 `.asm` 으로 다시 봄)

| 질문 (원문 번호 · sast/81 번호) | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 3 · 1 CRT 가 처음 핸들 종류를 묻는 길 | ucrtbase 시작 (`DllMainDispatch` 이유 1) → 초기화 표 8번 **`__acrt_initialize_lowio`** → `initialize_stdio_handles_nolock` 이 fd 0 · 1 · 2 마다 `GetStdHandle(−10 · −11 · −12)` → 핸들이 0 · −1 이 아니면 `GetFileType`. CHAR → `osfile \|= 0x40` (FDEV), PIPE → `\|= 0x08`, DISK → 그대로. **0 · −1 이거나 `GetFileType` 이 0 이면 `osfhnd = −2` (콘솔 없음)**. `GetConsoleMode` 는 안 부름 | **맞다** — 표준 핸들 0 이면 CRT 는 `GetFileType` 도 안 부르고 −2 로 둔다 | `DllMainDispatch +0xe1/+0xe6`, `__acrt_initialize_lowio +0x25/+0x2a`, `initialize_stdio_handles_nolock +0x3a ~ +0x47` · `+0x62/+0x68` · `+0x6b ~ +0x73` · **`+0xc6/+0xcc`** · `+0xce JZ` · `+0xd8 ~ +0xe9` · `+0x75` · `+0x81` |
| 3 · 2 콘솔 · 파일 · 파이프로 갈라 쓰는 자리 | 갈림은 `_write_nolock +0xc5 write_requires_double_translation_nolock` 하나. 참이 되려면 **FDEV ∧ 텍스트 모드 ∧ (로캘 데이터 `+0x138` ≠ 0 ∨ `pmode` ≠ 0) ∧ `GetConsoleMode` 성공**. 기본 상태는 로캘 `+0x138` = 0 · `pmode` = 0 이라 거짓 → `write_text_ansi_nolock` → **`WriteFile`**. 참이어도 ANSI 는 `WriteFile`, `WriteConsoleW` 는 UTF-8 / UTF-16 모드에서 CRT 가 따로 연 `CONOUT$` 로 글자 하나씩 | 요청서 가정 "FDEV + `GetConsoleMode` 성공이면 `WriteConsoleW`" 는 **틀림**. 엔진은 `NtWriteFile` 한 곳만 콘솔을 알면 된다 | `_write_nolock +0xc5/+0xca` · `+0xcc JNZ` · `+0xd6/+0xdb` · `+0x190/+0x195` · `+0xff/+0x105` · `+0x217/+0x21c` · `+0x2af/+0x2b4`, `write_requires_double_translation_nolock +0x17/+0x1c` · `+0x4b/+0x50` · `+0x64/+0x6c` · `+0xa1/+0xa6` · `+0x83/+0x89`, `write_text_ansi_nolock +0xc1/+0xc7` · `+0xd2` |
| 4 · 3 표준 핸들 0 일 때 CRT | 시작 성공 (fd 0 · 1 · 2 모두 `osfhnd −2` · `osfile 0xC1` · `FILE._file −2`). `printf` 는 조용히 돈다 — `_isatty(−2)` 거짓이라 임시 버퍼 없이 `stdout` 버퍼에 쌓고, 비울 때 `_write_internal` 이 `errno 9` · −1. `_invalid_parameter` 안 부름 → abort 없음, 프로그램 계속 | **맞다 · 걱정 없음**. `NtWriteFile` 은 안 나간다. test.exe 의 `NtWriteFile(0)` 은 CRT 가 아니라 `Log` (위) | `initialize_stdio_handles_nolock +0x6b ~ +0x8d`, `__acrt_initialize_stdio` (`.c`), `__acrt_stdio_begin_temporary_buffering_nolock +0x39/+0x3e`, **`_write_internal +0x29` · `+0x2c JZ → +0x122`** |
| 5 · (2 · 5) 리다이렉트 | CRT 도 표준 출력이 파일 · 파이프면 `_isatty` 거짓 (`write_requires_double_translation_nolock +0x1e JZ` 로 끝) → `WriteFile` → `NtWriteFile` 만. condrv 는 안 낀다. 단 파일 · 파이프는 버퍼링이라 `NtWriteFile` 은 버퍼가 찰 때 · `fflush` · 종료 때 나간다 (종료 길은 안 따라감 `[공백]`) | **맞다 (쓰기는)**. 시작 때 `ConsoleHandle` 이 문제 → (가) 표 | 같은 줄 + run83 질문 5 |
| (sast/81 4) condrv 가 받는 IOCTL | condrv 가 **앱 쪽 (kernelbase) 에게서 받는 IOCTL 은 셋**: `0x500016` (Client · Connection), `0x500023` (Connection, 빠른 길), `0x500037` (Server, 빠른 길 → `CdpLaunchServerProcess`). 나머지 `0x500006` · `0x50000b` ~ `0x50003b` 는 Server · Broker · Display 객체만 받는다 — conhost 몫 `[추측: 받는 객체로 짐작]`. 쓰기 · 읽기 · Flush 는 Client 객체만 | 흉내 낼 것은 IOCTL 셋 + Client 객체의 `NtWriteFile` / `NtReadFile` / `NtFlushBuffersFile` + 열기 이름 5 ~ 6개 | `CdpClientIoctl +0x13 CMP …,0x500016`, `CdpConnectionIoctl +0x2d`, `CdpConnectionFastIoctl +0x26 CMP …,0x500023`, `CdpServerFastIoctl` 점프표 (결과서), `CdpDispatchCreate +0x31` · `+0x47/+0x4e`, `CdpClientWrite +0xe1/+0xe6` · `+0x100` |
| (sast/81 4) `GetFileType` 이 콘솔에 CHAR 를 주는 근거 | condrv 가 부팅 때 `IoCreateDevice(DriverObject, 0, "\Device\ConDrv", DeviceType 0x50, 특성 0x20000, FALSE, &장치)` 로 장치를 만든다 → 콘솔 객체에 `FileFsDeviceInformation` 을 물으면 `DeviceType 0x50` → kernelbase `GetFileType +0x87 CMP EAX,0x50` → 2 (CHAR) | 가짜 콘솔 핸들엔 `DeviceType = 0x50` 을 줘야 CRT 가 FDEV 로 본다 | **`DriverEntry +0x77 MOV R9D,0x50` · `+0x120 MOV [RSP+0x20],0x20000` · `+0x128/+0x12f IoCreateDevice`**, run83 `GetFileType +0x87` · `+0x8c` |

### CRT 시작 — 표준 핸들마다 무엇을 적나 (`initialize_stdio_handles_nolock`, ucrtbase rva `0x720e4`)

부모가 C 런타임 핸들 표를 물려주지 않은 칸 (`osfhnd + 2 ≤ 1`, `+0x3a ~ +0x47`) 만 새로 정한다. 먼저 `osfile = 0x81` (`+0x49`).

| `GetStdHandle` 값 | `GetFileType` | `osfhnd` (칸 `+0x28`) | `osfile` (칸 `+0x38`) | `FILE._file` |
|---|---|---|---|---|
| **0** 또는 **−1** | **안 부름** (`+0x6b ~ +0x73`) | **−2** (`+0x81`) | `0xC1` (`+0x75 \|= 0x40`) | **−2** (`__acrt_initialize_stdio`) |
| 그 밖 | 0 (모름 · 오류) | −2 | `0xC1` | −2 |
| 그 밖 | 1 (DISK) | 핸들 (`+0xd3`) | `0x81` | 0 · 1 · 2 그대로 |
| 그 밖 | 2 (CHAR) | 핸들 | `0xC1` (`+0xe9 \|= 0x40` FDEV) | 그대로 |
| 그 밖 | 3 (PIPE) | 핸들 | `0x89` (`+0xe2 \|= 0x08`) | 그대로 |

- `osfile` 비트 값은 `[정적]`, 이름 (`FOPEN 0x01` · `FPIPE 0x08` · `FDEV 0x40` · `FTEXT 0x80`) 은 CRT 관례 `[추측: 이름]` — ucrtbase PDB 에 타입이 없다.
- `_isatty` 는 `osfile & 0x40` 하나만 본다. API 를 안 부른다.
- 표준 핸들이 NUL 장치 (`DeviceType 0x15`) 여도 kernelbase 가 CHAR 로 바꿔 주므로 (run83 점프표) CRT 는 FDEV 로 본다.
- 앞에 `initialize_inherited_file_handles_nolock` (`+0x36 GetStartupInfoW`) 가 매개변수 `RuntimeData` (`lpReserved2` · `cbReserved2`) 에 C 런타임 핸들 표가 있으면 먼저 복사한다. 비어 있으면 아무것도 안 한다.

### CRT 쓰기 — 언제 `_write` 까지 가나

- `stdout` 이 콘솔류 (FDEV) 면 `printf` 한 번의 출력을 4096B 임시 버퍼에 모았다가 끝에서 한 번 비운다 → **`printf` 한 번 = `_write` 한 번** (`__acrt_stdio_begin_temporary_buffering_nolock +0x39/+0x3e _isatty`, 람다 `operator()` `+0x114/+0x119 __acrt_stdio_flush_nolock`). `stderr` 는 늘 임시 버퍼.
- 파일 · 파이프면 보통 버퍼링 — 버퍼가 찰 때 · `fflush` · 종료 때 `_write` (버퍼 크기는 `__acrt_stdio_allocate_buffer_nolock` 안이라 안 읽음 `[공백]`, 보통 4096 `[추측]`).
- `write_text_ansi_nolock` 은 `\n` → `\r\n` 으로 바꿔 스택 버퍼 (최대 `0x13ff`B) 단위로 `WriteFile` 하고, **쓴 바이트가 요청보다 적으면 거기서 멈춘다** (`+0xd2 CMP EAX,EBX`). 그러니 `NtWriteFile` 의 `IoStatusBlock.Information` 은 `Length` 와 같아야 한다.

### (가) 최소 조건 — 파일 리다이렉트처럼 (콘솔 서버 없이)

핵심: 표준 핸들이 파일이어도 `ConsoleHandle = 0` 이면 `ConsoleAllocate` 가 불려 condrv 가 필요해진다. 이걸 피하는 값이 **−1** 이다 (`ConsoleInitialize +0x179 CMP RDX,-1` · `+0x17d JZ → +0x1c6` → 빈 상태로 `ConsoleCommitState`, run83).

| 항목 | 값 | 근거 줄 | 이게 없으면 |
|---|---|---|---|
| exe `Subsystem` | 3 (콘솔) 그대로 | `ConsoleInitialize +0x7b CMP [NT+0x5c],3` (run83) | GUI 로 바꾸면 다른 갈래 — 콘솔 프로그램 7천 개의 실제 모습과 달라진다 |
| `params.ConsoleHandle` (`+0x10`) | **−1** (`0xFFFFFFFFFFFFFFFF`) | `ConsoleInitialize +0x179` · `+0x17d JZ → +0x1c6` · `+0x1ca ConsoleCommitState`. commit 이 `+0x40` 에서 `ConsoleHandle ← 0` 으로 바꾼다 (run83 ③). −1 이 실제 Windows 의 어느 시작 방식에서 오는지는 `[추측]` (Q158 ①) | 0 · −2 · −3 → **`+0x146 ConsoleAllocate`** (condrv 를 열려 함, 실패면 `+0x528 JZ` → kernelbase 시작 FALSE). 진짜 핸들 → (나) 길 A |
| `params.ConsoleFlags` (`+0x18`) | 0 | `ConsoleInitialize +0x166 TEST [RBX+0x18],4` · `+0x1b9 TEST …,2` (run83) | 비트 4 · 2 가 켜지면 표준 핸들 검사 · 교체가 끼어 `NtQueryVolumeInformationFile` (+ 콘솔 장치면 `NtClose`) 이 더 나간다 |
| `params.WindowFlags` (`+0xa4`) 의 `0x200` · `0x400` | 둘 다 0 | `GetStdHandle +0x1d TEST [+0xa4],0x400` · `+0x27 JNZ`, `+0x49 … 0x200` · `+0x53 JNZ` → `+0x98 XOR EBX,EBX` (결과서 · run83) | 켜져 있으면 `GetStdHandle` 이 칸을 읽지도 않고 0 → 표준 핸들 0 처럼 보인다 |
| `params.StandardOutput` (`+0x28`) · `StandardError` (`+0x30`) (· `StandardInput` `+0x20`) | 0 도 −1 도 아닌 핸들 값 (엔진이 연 파일 핸들, 서로 같아도 됨) | `initialize_stdio_handles_nolock +0x6b ~ +0x73`, `GetStdHandle +0x66 CMP RBX,-1` | 0 · −1 이면 CRT 가 "콘솔 없음 (−2)" 으로 두고 `printf` 를 조용히 버린다 |
| 열어 둘 핸들의 종류 | 동기 (synchronous) 파일 핸들 또는 이름 있는 파이프, 쓰기 권한 | `WriteFile` 은 `NtWriteFile(h, 0, 0, 0, &IoStatus, buf, len, 0, 0)` — `ByteOffset` NULL 이라 파일 객체의 현재 위치에 쓴다 (run83 표 A) | 비동기 핸들이면 `0x103` 뒤 `WriteFile +0x1d1 NtWaitForSingleObject` 기다림 길을 엔진이 흉내 내야 한다 |
| `NtQueryVolumeInformationFile(h, FileFsDeviceInformation = 4, 8B)` 답 | `STATUS_SUCCESS`, `DeviceType = 7` (디스크) 또는 `0x11` (이름 있는 파이프). `Characteristics` 는 아무 값 | `GetFileType +0x72/+0x79` · `+0x5a R9D 8` · `+0x60 [RSP+0x20] 4`, 점프표 `2 ~ 8` · `0x24` → 1, `0x11` → 3 (run83) | `0x50` 이면 CRT 가 콘솔류로 본다 (아래 메모). 상태가 음수면 `GetFileType` 0 → CRT 가 그 표준 핸들을 −2 로 버린다 |
| `NtWriteFile` 답 | `STATUS_SUCCESS` (바로, `0x103` 없이), `IoStatusBlock.Information = Length` | `WriteFile +0xac ~ +0xb0` (run83 표 B), `write_text_ansi_nolock +0xc7` · `+0xd2 CMP EAX,EBX`, `_write_nolock` (0 바이트면 `errno 28`) | `Information` 이 작으면 CRT 가 거기서 멈춘다. 실패 상태면 `_write` −1 |
| `NtSetInformationProcess(−1, 0x31, &{1}, 8)` | `STATUS_SUCCESS` 권장 | `ConsoleCommitState +0xe9/+0xf0` (늘 불림, 값 `0 \| 1`) | **실패해도 시작 실패는 아니다** `[정적]` — commit 이 이 상태를 안 보고, `ConsoleInitialize` 는 commit 뒤 늘 AL 1 (아래 "보탠 것" ③). 커널 쪽 `EPROCESS.OwnerProcessId` 둘째 쓰기 (run51) 는 빠진다 |
| `params.RuntimeData` (`lpReserved2` · `cbReserved2`) | 비움 (0) | `initialize_inherited_file_handles_nolock +0x30/+0x36 GetStartupInfoW` 뒤 `cbReserved2 ≠ 0` 일 때만 | 채우면 CRT 가 그 표를 표준 핸들보다 먼저 `osfhnd` 에 복사 |
| 콘솔 API 답 (`GetConsoleMode` 등) | 필요 없음 | commit 뒤 `ConsoleHandle = 0` → `GetConsoleMode +0x5f/+0x62` 등이 syscall 없이 `0xC0000008` (run83 표 B) | 앱이 `GetConsoleMode(stdout)` 로 리다이렉트를 가려내면 "콘솔 아님" — 진짜 리다이렉트와 같다 |
| KUSD `SharedDataFlags` (`0x7ffe02f0`) | 상관없음 | −1 갈래는 `ConsoleAllocate` 를 안 탄다 | |

- **이것만으로 끝까지 가나**: 정적으로는 **예**. syscall 은 시작 때 `NtSetInformationProcess` 1번, 표준 핸들마다 `NtQueryVolumeInformationFile` (CRT `GetFileType` 3번 + 앱이 따로 부르면 더), 그리고 `NtWriteFile`.
- 단점: 파일 · 파이프는 `stdout` 이 버퍼링이라 `NtWriteFile` 이 버퍼 찰 때 · 종료 때 몰아서 나간다. 종료 때 비우는 길 (`exit` → `common_flush_all`) 은 안 따라갔다 `[공백]`.
- 메모 `[추측: 정적 줄 셋을 이은 것]`: (가) 에서 `DeviceType` 을 `0x50` 으로 답하면 CRT 는 콘솔류로 보고 `printf` 마다 `_write` 한다. C 로캘이면 그대로 `WriteFile` 이고, 로캘을 바꿔 `write_requires_double_translation_nolock +0x83 GetConsoleMode` 를 불러도 `ConsoleHandle = 0` 이라 syscall 없이 실패 → 거짓 → `WriteFile`. 곧 condrv 없이 줄마다 나가는 출력이 될 것이다. 실제로 그런지는 동적 (Q104 2 · 새 샘플).

### (나) 최소 조건 — 가짜 condrv 콘솔, 길 둘

**길 A: 붙기** (`ConsoleHandle` = 진짜 핸들 H0. VM 의 test.exe 가 이 길, H0 = 4 — run44 `[관찰]`). syscall 4 (run83 (a)).

| 항목 | 값 | 근거 줄 | 이게 없으면 |
|---|---|---|---|
| `params.ConsoleHandle` | 0 · −1 · −2 · −3 이 아닌 양수 핸들 H0. condrv 쪽에선 `\Reference` 객체여야 맞다 | `ConsoleInitialize +0x170 ~ +0x189` → `+0x19a ConsoleCreateConnectionObject` (run83). condrv `CdCreateConnection`: 열 때 `RelatedFileObject` (= `RootDirectory`) 의 종류가 Reference 여야 서버 연결 (`CdpCreateServerConnectionIo`), 아니면 `0xC0000008` (결과서) | 0 → 길 B. 음수 → 다른 갈래 |
| `params.ConsoleFlags` · `WindowFlags & 0x600` | 0 · 0 | (가) 와 같음 | (가) 와 같음 |
| `params.StandardInput` · `Output` · `Error` | condrv Client 객체 핸들 셋 (`\Input` · `\Output`, 에러는 출력 복사본) — 부모 (엔진) 가 미리 열어 둠 | 붙기 길은 commit 이 표준 핸들을 안 채운다 (상태 비트 0, run83 ④). condrv 열기 이름 표 3 · 4번 (`CdCreateDefaultObjectClient` 인자 1 · 2) | 0 · −1 이면 CRT 가 −2 (질문 3) |
| a1 `NtCreateFile` | 이름 `\Connect`, `RootDirectory = H0`, 접근 `0x12019f`, `Attributes 0x40`, 공유 7, 방식 2, 옵션 `0x20`, EA `"server"` + 연결 데이터 `0x53c`B. **답: SUCCESS + 새 핸들 H1 (≠ 0)** | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleInitialize +0x19a/+0x19f` (run83 a1). condrv `CdpDispatchCreate +0x47/+0x4e` → 표 0번 `\Connect` → `CdCreateConnection` | 실패 → `ConsoleInitialize` AL 0 → **kernelbase 시작 FALSE** (`0xc0000022` 면 로우박스 확인 뒤 길 B 로, run83) |
| a2 `NtDeviceIoControlFile(H1, 0x500023, 출력 8B)` | SUCCESS, 출력 8B 에 서버 PID (아무 수, 가짜 conhost PID). 출력 길이가 8 이 아니면 condrv 는 `0xC0000206` | `ConsoleCommitState +0x8a/+0x91`, condrv `CdpConnectionFastIoctl +0x26` | 실패해도 시작은 계속 — 값이 `0 \| 1` 이 될 뿐 (`+0x44` 가 출력 칸을 0 으로 둠, "보탠 것" ③) |
| a3 `NtSetInformationProcess(−1, 0x31, &(PID \| 1), 8)` | SUCCESS | `ConsoleCommitState +0xe9/+0xf0` | (가) 와 같음 — 시작 실패로는 안 이어짐 `[정적]` |
| a4 `NtDeviceIoControlFile(H1, 0x500016, API 0x01000008 GetLangId, body 2B)` | SUCCESS, 출력 `WORD` 언어 ID (VM 값은 동적. run44 TEB `CurrentLocale` 이 P1 ~ P2 `0x412` `[관찰]` 이라 같은 값일 것 `[추측]`) | `ConsoleCommitState +0xfa/+0xff` → `SetTEBLangID +0x12/+0x17` → `GetConsoleLangId +0xe1/+0xe8` (run83) | 실패해도 오류 코드만 세우고 넘어감. 성공이면 `SetThreadLocale(LangId)` 가 이어서 TEB `CurrentLocale` 을 쓴다 |
| 표준 핸들의 `NtQueryVolumeInformationFile(FileFsDeviceInformation)` | SUCCESS, **`DeviceType = 0x50`**, `Characteristics = 0x20000` `[추측: 장치 객체 값 그대로]` | `GetFileType +0x87 CMP EAX,0x50` · `+0x8c MOV EAX,2` (run83), condrv `DriverEntry +0x77` · `+0x120` | 다른 값이면 CRT 가 FDEV 를 안 켜 `stdout` 이 파일처럼 버퍼링 (종료 때만 출력), `_isatty` 거짓 |
| `NtWriteFile(표준 출력 핸들)` | SUCCESS, `Information = Length`. 진짜 condrv 는 `0x103` 을 주고 conhost 가 나중에 `0x50000b` 로 끝낸다 — 엔진은 바로 끝내 주면 된다 | condrv `CdpClientWrite +0xe1/+0xe6 CdAddIoToPipe` · `+0x100 MOV EAX,0x103`, `CdCompleteIo` (결과서), CRT `write_text_ansi_nolock +0xd2` | (가) 와 같음 |
| 모르는 IOCTL · Connection 객체에 쓰기 | `0xC0000010` · `0xC00000BB` | `CdpClientIoctl` · `CdpConnectionIoctl` · `CdpDispatchWrite` (결과서) | 앱이 오류를 받음 |

**길 B: 새 콘솔** (`ConsoleHandle = 0` 그대로 — 지금 PE_emulator 값). `params` 를 바꾸지 않고 엔진의 syscall 답만으로 된다. 표준 핸들은 commit 이 `params.Standard*` 에 채운다 (`ConsoleCommitState +0x11c` · `+0x12e` · `+0x140`, run83).

| 순서 | syscall | 답 | 근거 (run83 (b)) |
|---|---|---|---|
| 전제 | KUSD `SharedDataFlags` (`0x7ffe02f0`) 비트 6 = 0 | VM 은 `0x10e` `[관찰]` (run68) | `ConsoleAllocate +0x52` · `+0x5a JNZ` (켜지면 중개 길 — 안 읽음) |
| b1 | `NtCreateFile("\Device\ConDrv\Server", 0x12019f, Attributes 0x42, 방식 2, 옵션 0)` | SUCCESS + Hs (실패면 `NtSetSystemInformation(0x84, {1}, 4)` 뒤 한 번 더) | `ConsoleAllocate +0x149/+0x150` · `+0x175/+0x17c` · `+0x20b/+0x212` |
| b2 | `NtCreateFile("\Reference", root = Hs, 옵션 0x20)` | SUCCESS + Hr | `+0x2b4/+0x2bb` |
| b3 | `NtDeviceIoControlFile(Hs, 0x500037, 입력 = 매개변수 블록)` | SUCCESS (진짜로 conhost 를 안 띄워도 됨) | `ConsoleLaunchServerProcess +0xc6/+0xcd`, condrv Server 빠른 길 → `CdpLaunchServerProcess` |
| b4 | `NtClose(Hs)` | SUCCESS | `+0x332/+0x339` |
| b5 | `NtCreateFile("\Connect", root = Hr, EA "server")` | SUCCESS + Hc | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleAllocate +0x357/+0x35c` |
| b6 | `NtCreateFile("\Input", root = Hc, Attributes 0x42 (상속))` | SUCCESS + Hin | `ConsoleCreateStandardIoObjects +0xb1/+0xb8` |
| b7 | `NtCreateFile("\Output", …)` | SUCCESS + Hout | `+0x151/+0x158` |
| b8 | `NtDuplicateObject(−1, Hout, −1, …, 옵션 6)` | SUCCESS + Herr | `+0x1a8/+0x1af` |
| b9 ~ b11 | a2 · a3 · a4 와 같음 (핸들 = Hc) | 위 길 A 표 | `ConsoleCommitState` |

- **두 길 모두**: 그 뒤 표준 핸들 셋이 CHAR 로 보이고 `NtWriteFile` 이 성공하면 `printf` 는 끝까지 간다. C 로캘 기본이면 **콘솔 IOCTL 은 commit 의 GetLangId 하나뿐** (질문 2).
- 시작 실패 조건 (run83 다듬음, 아래 "정정"): 길 A 는 a1 실패, 길 B 는 `ConsoleAllocate` 가 실패를 돌려줄 때. commit 쪽 a2 · a3 · a4 실패로는 시작 실패가 안 된다 `[정적]`.
- **지금 우리**: 길 B 가 지금 값에 가장 가깝다 — 엔진이 b1 ~ b11 을 답하면 `params` 의 표준 핸들이 저절로 채워져 `WriteFile` · `printf` 가 `NtWriteFile` 까지 간다. (가) 는 더 단순하지만 `ConsoleHandle` 을 −1 로 바꿔야 한다. **`ConsoleHandle = 0` 인데 표준 핸들만 파일로 바꾸는 절충안은 condrv 를 못 피한다.**
- 지금 `NtWriteFile(0)` 까지 온 것이 무엇을 뜻하는지는 run83 `[공백]` 그대로 (진짜 `ConsoleInitialize` 를 안 지났는지 / `ConsoleAllocate` 에 가짜 성공을 줬는지 — PE_emulator 쪽 확인, 아래 동적 1).

**앱이 콘솔 API 를 직접 부를 때 더 답할 것** (선택 — 결과서 · run83 표 A · B 짝)

| 호출 | `NtDeviceIoControlFile` 인자 | 줄 답 | 값 출처 |
|---|---|---|---|
| `GetConsoleMode` | 핸들 = `params.ConsoleHandle`, `0x500016`, API `0x01000001`, body 4B | SUCCESS, 출력 `DWORD` 모드 | 서버가 정함 → VM 값 (Q104) |
| `GetConsoleOutputCP` | 같은 핸들, `0x500016`, API `0x01000000`, body 8B (`+4 = 1`) | SUCCESS, `+0` 코드 페이지 | VM 값. 0 이 오면 `GetConsoleOutputCP` 가 오류 코드 + 0 반환 (run83 표 B) |
| `WriteConsoleW` | 핸들 = **앱 출력 핸들 그 자체**, `0x500016`, API `0x01000006`, body 8B, 입력 버퍼 2 (메시지 + 글자 `nChars × 2`B) | SUCCESS, body `+0` = 쓴 **바이트** 수 → `*lpNumberOfCharsWritten = 바이트 / 2` | run83 `WriteConsoleInternal +0x14f ~ +0x161` |
| `GetConsoleScreenBufferInfo(Ex)` | `params.ConsoleHandle`, `0x500016`, API `0x02000007`, body `0x5c`B | SUCCESS + 화면 정보 (run83 표 C) | VM 값 |
| CRT `_putwch_nolock` 의 `CONOUT$` 열기 | `CreateFileW(L"CONOUT$", GENERIC_WRITE, 공유 3, …, OPEN_EXISTING)` → kernelbase → ntdll 이름 바꾸기 → `NtCreateFile` (condrv 쪽 이름은 `\CurrentOut` 일 것 `[추측]` — ntdll 이 어떤 NT 이름 · 루트로 바꾸는지 `[공백]`) | SUCCESS + 핸들 (CHAR 로 보이면 `WriteConsoleW` 가 `0x500016` / API 6 으로 감) | 유니코드 모드 `printf` 일 때만 |

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적 (이번에 처음 analyze — 결과서):
  - ucrtbase pdb `158F9F38CC492128FE8C9354A209CFB51` (`ms/sym/ucrtbase.dll.pdb.info.txt`), 함수 5165개
  - condrv pdb `B37C9D13666B9A282B3CEA43C4AECBF31` (`ms/sym/condrv.sys.pdb.info.txt`, `System32\drivers\condrv.sys`), 함수 116개
  - 두 PDB 모두 **타입이 없다** (`pdb_struct.py --list` 0개). 구조체 칸 이름 · `osfile` 비트 이름 · IOCTL 이름은 모두 `[추측: 이름]`, 칸 위치 · 값은 `.asm` 의 `[정적]`.
  - kernelbase pdb `72CCE506944DE0267C172AD64287F9BD1` · ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` (기준과 같음 — 다시 analyze 안 함)
- 버전 대조 (결과서): "버전 확인용 알려진 사실" 표엔 ucrtbase · condrv 줄이 없다. 대신 condrv 가 받는 IOCTL `0x500016` · `0x500023` · `0x500037` 과 열기 이름 `\Connect` · `\Reference` · `\Input` · `\Output` · `\Server` 가 kernelbase (sast/80) 가 보내는 값과 정확히 같다. `printf` 람다 `0x1aa00` 의 ret 셋은 `objdump` 바이트와 맞췄다.

## 목적

- PE_emulator 요청 27 (콘솔 입출력) 의 남은 몫 (Q149b): 원문 3 (CRT 가 핸들 종류를 묻는 길 · 갈라 쓰는 자리), 4 · 5 의 CRT 몫, condrv.sys 가 받는 IOCTL (대조), 그리고 **(가) 파일 리다이렉트 / (나) 가짜 condrv 최소 조건 표** — 요청 27 의 마지막 답.
- kernelbase 쪽 (콘솔 얻는 길 · 콘솔 API 표) 은 짝 바퀴 run83 (Q149a).

## 방법

- 정적: `_workspace/sast/81_console-crt.md` — ucrtbase · condrv (새 analyze)
  - 지도 (새로, `ms/disassemble/ucrtbase/_map/`): `__acrt_initialize_lowio` (함수 10개, 끊긴 끝 모두 import), `__stdio_common_vfprintf` (깊이 8, 92개). condrv 는 지도 안 만듦 (IRP 처리 함수가 호출이 아니라 함수 표로만 닿음 — 표는 `.rdata` 에서 직접).
  - 뽑음 (모두 `export`, SCRIPT ERROR 없음 — 로그 `ms/ghidra/*.export.sast81*.stdout`): ucrtbase 82개 (lowio · stdio 초기화 14 depth 1, 쓰기 13 depth 1, `printf` · `puts` 사슬 18 depth 0, 넓힌 것 `initialize_c` · `initialize_global_variables` · `initialize_pointers` · `_putwch_nolock` · `_cputws` · `_cputs` · `console_output_reopen_and_retry` depth 0), condrv 30개 (`DriverEntry` · `CdpDispatch*` 10 · IOCTL 처리 · `CdpClientWrite/Read/FlushBuffers` 등 depth 0).
  - 같은 때 읽기 전용으로 한 번씩 (결과서): kernelbase `CreateFileW` · `CreateFileInternal` depth 1, ntdll `RtlpDosPathNameToRelativeNtPathName` · `RtlpIsDosDeviceName_Ustr` depth 0 (`refs RtlpDosCONOUTDevice`) — CRT 가 여는 `CONOUT$` 의 이름을 보려고.
  - `.asm` 에 없는 곳 — 결과서가 임시 스크립트 (레포 밖) 로 직접 읽음: condrv `.rdata` 의 `CdpObjectCreationTable` (12칸) · 객체 종류 표 6개, ucrtbase `.rdata` 초기화 표 (`0xf94d0`) · `__xi_a` ~ `__xi_z` (`0xfdbd8`), `.data` 의 `_iob` 첫 값 · `__acrt_initial_locale_data`, `objdump` 로 `printf` 안쪽 람다 `operator()` (`0x1aa00`, 같은 이름이 많아 export 안 함).
- 결과서: `_workspace/sast/81_console-crt_result.md` (**분석성공**. 질문 1 ~ 5 모두 `.asm` 줄로 답함. 스크립트 문제 없음)
- 오케스트레이터가 `.asm` · 소스로 다시 맞춘 줄: ucrtbase `initialize_stdio_handles_nolock +0x62 GetStdHandle` · `+0xc6 GetFileType`, condrv `DriverEntry +0x120 [RSP+0x20] = 0x20000` · `+0x128 IoCreateDevice`, kernelbase `_KernelBaseBaseDllInitialize +0x521` · `+0x526` · `+0x528`, `sample/log.h` 의 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE))`.
- flow-writer 도 `.asm` 으로 다시 봤다: ucrtbase `initialize_stdio_handles_nolock` (전부), `__acrt_initialize_lowio` · `entry` · `DllMainDispatch` · `__acrt_stdio_begin_temporary_buffering_nolock` 의 call 줄, `_write_nolock +0xc5 ~ +0xdb` · `+0xff` · `+0x190` · `+0x217` · `+0x24a` · `+0x272` · `+0x2af`, `write_requires_double_translation_nolock +0x17 ~ +0xa6`, `write_text_ansi_nolock +0xc1` · `+0xd2`, `_write_internal +0x29` · `+0x2c` · `+0x85` · `+0xa9` · `+0x122`, `_calls.tsv` 의 이번 함수 줄. condrv `DriverEntry` (전부), `CdpDispatchCreate +0x31` · `+0x47` · `+0x68` · `+0x8b` · `+0xa9`, `CdpClientIoctl +0x13` · `+0x2a`, `CdpConnectionIoctl +0x2d`, `CdpConnectionFastIoctl +0x26`, `CdpClientWrite` call 줄 · `+0x100`, `CdCreateConnection` call 줄. kernelbase `ConsoleInitialize +0x0 ~ +0x80` · `+0x126 ~ +0x164` · `+0x1b9 ~ +0x1cf` · `+0x22f ~ +0x23c`, `ConsoleCommitState +0x0 ~ +0x10c`.
- 이름 확인 (flow-writer): sample 세 모듈의 `_index.tsv` 에서 CRT lowio 함수 이름 · `GetFileType` import 썽크 (아래 "보탠 것" ④). 본문은 안 읽었다.
- 값 대조: CRT 는 진입점 뒤, condrv 는 커널 드라이버라 run44 사진에 대조할 칸이 없다. run83 이 옮긴 run44 매개변수 값 (`ConsoleHandle` 4 → `0x60`, 표준 핸들 `0x68` · `0x6c` · `0x70`) 만 다시 썼다. 새 동적 실행은 없다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/{ucrtbase,condrv}/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ucrtbase!entry` (`0x63c80`) | `+0x23` | (꼬리 `JMP`) | `DllMainDispatch` | 이유 1 이면 먼저 `+0x28 __security_init_cookie` | [정적] |
| `ucrtbase!DllMainDispatch` (`0xbce0`) | `+0xe1` | `+0xe6` | `__acrt_execute_initializers` | 이유 1. 표 `0xf94d0` — 8번 `__acrt_initialize_lowio` · 15번 `initialize_c` (→ `__xi` 표의 `__acrt_initialize_stdio`) | [정적] |
| `ucrtbase!__acrt_initialize_lowio` (`0x71f20`) | `+0x17` · `+0x20` · `+0x25` | `+0x1c` · `+0x25` · `+0x2a` | `__acrt_lowio_ensure_fh_exists` · `initialize_inherited_file_handles_nolock` · `initialize_stdio_handles_nolock` | 늘 (락 안) | [정적] |
| `ucrtbase!initialize_inherited_file_handles_nolock` (`0x721f4`) | `+0x30` · `+0xf0` | `+0x36` · `+0xf6` | `GetStartupInfoW` · `GetFileType` | 뒤는 `cbReserved2 ≠ 0` 이고 칸 플래그에 `8` 이 없을 때 | [정적] |
| `ucrtbase!initialize_stdio_handles_nolock` (`0x720e4`) | **`+0x62`** | `+0x68` | `GetStdHandle` | fd 0 · 1 · 2 (`−10` · `−11` · `−12`), 칸이 비었을 때만 | [정적] |
| `ucrtbase!initialize_stdio_handles_nolock` | **`+0xc6`** | `+0xcc` | `GetFileType` | 핸들이 0 · −1 이 아닐 때 (`+0x6b ~ +0x73`) | [정적] |
| `ucrtbase!__stdio_common_vfprintf` (`0x19df0`) | `+0x4e` | `+0x53` | `common_vfprintf<standard_base,char>` | `printf` 는 ucrtbase 에 없음 — 앱 헤더가 이걸로 풀어 씀 | [정적] |
| 람다 `operator()` (`0x1aa00`, `objdump`) | `+0x43` · `+0xb6` · `+0x114` | `+0x48` · `+0xbb` · `+0x119` | `__acrt_stdio_begin_temporary_buffering_nolock` · `process` · `__acrt_stdio_flush_nolock` | 끝 비우기는 `FILE` 플래그 비트 9 (임시 버퍼) 일 때 | [정적] |
| `ucrtbase!__acrt_stdio_begin_temporary_buffering_nolock` (`0x1bcc0`) | `+0x39` | `+0x3e` | `_isatty` | `stdout` 이면 FDEV 일 때만 임시 버퍼 | [정적] |
| `ucrtbase!__acrt_stdio_flush_nolock` (`0x1c100`) | `+0x51` | `+0x56` | `_write_internal` | | [정적] |
| `ucrtbase!_write_internal` (`0x1c310`) | `+0xa9` | `+0xae` | `_write_nolock` | fd −2 면 `+0x2c JZ → +0x122` (`errno 9`, −1) 로 안 감 | [정적] |
| `ucrtbase!_write_nolock` (`0x1c620`) | `+0xc5` | `+0xca` | `write_requires_double_translation_nolock` | `+0xcc JNZ` → 이중 번역 | [정적] |
| `ucrtbase!_write_nolock` | `+0x190` | `+0x195` | `write_text_ansi_nolock` | 텍스트 · `칸+0x39 = 0` (ANSI) · 이중 번역 아님 — **C 로캘 기본** | [정적] |
| `ucrtbase!_write_nolock` | `+0xff` | `+0x105` | `WriteFile` | 바이너리 모드 | [정적] |
| `ucrtbase!_write_nolock` | `+0x2af` · `+0x217` | `+0x2b4` · `+0x21c` | `write_double_translated_ansi_nolock` · `write_double_translated_unicode_nolock` | 이중 번역 — ANSI / UTF-8 · UTF-16 | [정적] |
| `ucrtbase!write_requires_double_translation_nolock` (`0x1c940`) | `+0x17` · `+0x83` | `+0x1c` · `+0x89` | `_isatty` · `GetConsoleMode` | 뒤는 FDEV ∧ 텍스트 ∧ (로캘 `+0x138` ≠ 0 ∨ `pmode` ≠ 0) | [정적] |
| `ucrtbase!write_text_ansi_nolock` (`0x1d450`) | **`+0xc1`** | `+0xc7` | `WriteFile` | `+0xd2 CMP EAX,EBX` — 덜 쓰면 멈춤 | [정적] |
| `ucrtbase!write_double_translated_ansi_nolock` (`0x803e8`) | `+0x76` · `+0x195` · `+0x1c0` · `+0x2ee` | `+0x7c` · `+0x19b` · `+0x1c6` · `+0x2f4` | `GetConsoleOutputCP` · `WideCharToMultiByte` · `WriteFile` · `WriteFile` | 이중 번역 ANSI 도 끝은 `WriteFile` | [정적] |
| `ucrtbase!write_double_translated_unicode_nolock` (`0x874f8`) | `+0x3c` · `+0x53` | `+0x41` · `+0x58` | `_putwch_nolock` | 글자마다 | [정적] |
| `ucrtbase!_putwch_nolock` (`0x87590`) | `+0x9` · `+0x38` | `+0xe` · `+0x3d` | `__dcrt_lowio_ensure_console_output_initialized` · `console_output_reopen_and_retry` | | [정적] |
| `ucrtbase!__dcrt_lowio_ensure_console_output_initialized` (`0x87764`) | `+0x47` | `+0x4d` | `CreateFileW` | 처음 한 번 `L"CONOUT$"` (표준 핸들과 별개) | [정적] |
| `ucrtbase!console_output_reopen_and_retry` (`0x875e8`) | `+0x25` · `+0x3c` · `+0x63` · `+0x84` | `+0x2b` · `+0x41` · `+0x69` · `+0x8a` | `WriteConsoleW` · (닫기) · `CreateFileW` · `WriteConsoleW` | 오류 6 이면 다시 열고 한 번 더 | [정적] |
| `condrv!DriverEntry` (`0x10010`) | **`+0x128`** | `+0x12f` | `IoCreateDevice` | `+0x77 R9D 0x50` · `+0x120 [RSP+0x20] 0x20000` · 이름 `[RSP+0x40]` (`\Device\ConDrv`) | [정적] |
| `condrv!CdpDispatchCreate` (`0xebd0`) | `+0x47` · `+0x68` | `+0x4e` · `+0x6d` | `RtlCompareUnicodeString` · (간접) 표의 처리 함수 | 이름 표 12칸 (`+0x31 CMP EBX,0xc`), 못 찾으면 `+0x8b` · `+0xa9` `0xC0000225` | [정적] |
| `condrv!CdCreateConnection` (`0xd5a0`) | `+0x3b` · `+0x8c` · `+0x125` · `+0x13c` | `+0x40` · `+0x91` · `+0x12a` · `+0x141` | `CdpFindEaBufferItem` · `CdpCreateServerConnectionIo` · `CdpCreateServerConnectionIo` · `CdpCreateBrokerConnectionIo` | EA `"attach"` · `RelatedFileObject` 로 갈림 (결과서) | [정적] |
| `condrv!CdpClientIoctl` | `+0x2a` | `+0x2f` | `CdClientUserIo` | `+0x13 CMP …,0x500016` 일 때만 | [정적] |
| `condrv!CdpClientWrite` (`0xa890`) | `+0x5b` · `+0xe1` | `+0x60` · `+0xe6` | `CdGetProcessConnection` · `CdAddIoToPipe` | `+0x100 MOV EAX,0x103` (보류) | [정적] |

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리. 같은 번호 둘 = 값에 따라 둘 중 하나.

```
[CRT 시작 — ucrtbase 시작 함수 (/MD 프로그램). /MT 는 같은 이름 함수가 모듈 안에 — 아래 "보탠 것" ④]
ucrtbase!entry (0x63c80)                       +0x23 JMP ▶ DllMainDispatch
ucrtbase!DllMainDispatch (0xbce0)              이유 1
 └─ +0xe6  __acrt_execute_initializers(표 0xf94d0)      0 전역 · … · 5 힙 · 7 ptd · 8 lowio · 9 명령줄 · 10 멀티바이트 · 14 환경 · 15 initialize_c
      ├─ (8)  __acrt_initialize_lowio (0x71f20)
      │       ├─ (1) +0x1c  __acrt_lowio_ensure_fh_exists(0)         핸들 표 64칸 × 0x48B (__pioinfo)
      │       ├─ (2) +0x25  initialize_inherited_file_handles_nolock (0x721f4)
      │       │       └─ +0x36  GetStartupInfoW                    cbReserved2 ≠ 0 일 때만 복사 (+0xf6 GetFileType)
      │       └─ (3) +0x2a  initialize_stdio_handles_nolock (0x720e4)        fd 0 · 1 · 2
      │               ├─ (1) +0x68  GetStdHandle(−10 · −11 · −12)        syscall 없음 (run83)
      │               │   +0x6b ~ +0x73 핸들 0 · −1 → +0x75 osfile |= 0x40 · +0x81 osfhnd = −2
      │               └─ (2) +0xcc  GetFileType(핸들)  → NtQueryVolumeInformationFile (run83)
      │                   +0xce 0 → −2 · +0xd3 osfhnd = 핸들 · +0xe9 CHAR |= 0x40 · +0xe2 PIPE |= 0x08
      └─ (15) initialize_c → __xi 표 → __acrt_initialize_stdio      FILE._file = −2 (osfhnd 가 −2 · −1 · 0 이면)

[CRT 쓰기 — printf (앱 헤더)]
ucrtbase!__stdio_common_vfprintf (0x19df0)
 └─ +0x53  common_vfprintf<standard_base,char> → SEH 람다 → operator() (0x1aa00)
      ├─ (1) +0x48   __acrt_stdio_begin_temporary_buffering_nolock    +0x3e _isatty → stdout 이 FDEV 면 4096B 임시 버퍼
      ├─ (2) +0xbb   process (서식 풀기) → … → common_flush_and_write_nolock → write_buffer_nolock → _write_internal
      └─ (3) +0x119  __acrt_stdio_flush_nolock → +0x56 _write_internal
ucrtbase!_write_internal (0x1c310)             +0x29 fd == −2 → +0x2c JZ → +0x122 errno 9 · −1 (abort 없음)
 └─ +0xae  _write_nolock (0x1c620)
      ├─ (1) +0xca   write_requires_double_translation_nolock (0x1c940)
      │       ├─ (1) +0x1c  _isatty                 +0x1e JZ (FDEV 아님) → 거짓
      │       │   +0x4b 텍스트? · +0x64 로캘 +0x138 ≠ 0? · +0xa1 pmode ≠ 0?   둘 다 0 이면 거짓 (기본)
      │       └─ (2) +0x89  GetConsoleMode(osfhnd)  성공이면 참
      ├─ (2) +0x195  write_text_ansi_nolock (0x1d450)      ← C 로캘 기본
      │       └─ +0xc7   WriteFile(osfhnd, …) → NtWriteFile (run83)      +0xd2 덜 쓰면 멈춤
      ├─ (2) +0x105  WriteFile                              바이너리 모드
      ├─ (2) +0x2b4  write_double_translated_ansi_nolock (0x803e8)     +0x7c GetConsoleOutputCP · +0x19b WideCharToMultiByte
      │       └─ +0x1c6 · +0x2f4  WriteFile
      └─ (2) +0x21c  write_double_translated_unicode_nolock (0x874f8)
              └─ +0x41 · +0x58  _putwch_nolock (0x87590)
                   ├─ (1) +0xe   __dcrt_lowio_ensure_console_output_initialized → +0x4d CreateFileW(L"CONOUT$")  처음 한 번
                   └─ (2) +0x3d  console_output_reopen_and_retry → +0x2b WriteConsoleW (글자 하나)  오류 6 → +0x69 다시 열기 · +0x8a 한 번 더

[condrv — 커널 드라이버]
condrv!DriverEntry (0x10010)                   부팅 때 (흐름도 범위 앞)
 └─ +0x12f  IoCreateDevice(DriverObject, 0, "\Device\ConDrv", 0x50, 0x20000, FALSE, &장치)   +0x144 StackSize 2 · +0x14d DO_DIRECT_IO
condrv!CdpDispatchCreate (0xebd0)              IRP_MJ_CREATE
 ├─ (1) +0x4e  RtlCompareUnicodeString × 12칸      \Connect · \KernelConnect · \Reference · \Input · \Output · \Server · \Console · \CurrentIn · \CurrentOut · \ScreenBuffer · \Broker · \Display
 └─ (2) +0x6d  (간접) 처리 함수 — \Connect 면 CdCreateConnection (0xd5a0)
               ├─ (1) +0x40   CdpFindEaBufferItem("attach")
               ├─ (2) +0x91 · +0x12a  CdpCreateServerConnectionIo     RelatedFileObject = Reference 종류 (아니면 0xC0000008) · "attach" 길
               └─ (2) +0x141  CdpCreateBrokerConnectionIo             RelatedFileObject 없음
condrv!CdpDispatchDeviceControl → FileObject→FsContext2→종류 표 IOCTL 칸 (간접)
   Client  CdpClientIoctl      +0x13 0x500016 만 → +0x2f CdClientUserIo
   Connection  CdpConnectionIoctl +0x2d 0x500016 · CdpConnectionFastIoctl +0x26 0x500023 (출력 8B)
   Server  CdpServerFastIoctl  점프표 → 0x500037 CdpLaunchServerProcess (나머지 코드는 conhost 몫 [추측])
condrv!CdpDispatchWrite → (Client 만) CdpClientWrite (0xa890)
 ├─ (1) +0x60  CdGetProcessConnection
 └─ (2) +0xe6  CdAddIoToPipe           +0x100 0x103 (보류 — 서버가 0x50000b 로 상태 · Information 을 채움)
```

## 확인된 것

- `[정적]` ucrtbase 시작 (이유 1) 은 초기화 표 8번 `__acrt_initialize_lowio` 에서 `initialize_stdio_handles_nolock` 을 부르고, 그 함수가 표준 핸들마다 `GetStdHandle` (`+0x62`) 과 — 핸들이 0 · −1 이 아니면 — `GetFileType` (`+0xc6`) 만 부른다. `GetConsoleMode` 는 시작 때 안 부른다. 0 · −1 · `GetFileType` 0 이면 `osfhnd = −2` · `osfile 0xC1`, CHAR 면 FDEV (`0x40`), PIPE 면 `0x08`.
- `[정적]` lowio (8번) 가 stdio (15번 `initialize_c` → `__acrt_initialize_stdio`) 보다 먼저다. `FILE._file = −2` 는 stdio 쪽이 정한다.
- `[정적]` CRT 의 콘솔 / 파일 갈림은 `_write_nolock +0xc5 write_requires_double_translation_nolock` 하나다. 기본 상태 (로캘 `+0x138` = 0, `pmode` = 0) 면 거짓이라 **`write_text_ansi_nolock +0xc1 WriteFile`** — 콘솔이어도 `NtWriteFile`. `WriteConsoleW` 는 UTF-8 · UTF-16 텍스트 모드 + 콘솔 + `GetConsoleMode` 성공일 때 CRT 가 따로 연 `CONOUT$` 로만.
- `[정적]` 표준 핸들 0 이면 CRT 시작은 성공하고, `printf` 는 버퍼에 쌓았다가 비울 때 `_write_internal +0x29` · `+0x2c JZ → +0x122` 로 `errno 9` · −1 — `_invalid_parameter` 를 안 부르니 abort 없음.
- `[정적]` condrv 는 `\Device\ConDrv` 를 `DeviceType 0x50` · 특성 `0x20000` 으로 만든다 (`DriverEntry +0x77` · `+0x120` · `+0x128`). kernelbase `GetFileType +0x87` 의 `0x50` → CHAR 와 짝이다.
- `[정적]` condrv 가 받는 IOCTL 가운데 kernelbase 가 보내는 것 (sast/80) 은 `0x500016` (Client · Connection 객체) · `0x500023` (Connection) · `0x500037` (Server) 셋이고, condrv 쪽 처리 줄과 값이 맞는다. 쓰기 · 읽기 · Flush 는 Client 객체만 받고, 쓰기는 `0x103` (보류) 를 돌려준 뒤 서버 (conhost) 가 `0x50000b` 로 끝낸다.
- `[정적]` condrv `\Connect` 열기는 열 때 기준 객체 (`RootDirectory`) 가 Reference 종류여야 서버 연결이 된다 (`CdCreateConnection`, 결과서). kernelbase 붙기 길이 `RootDirectory = ConsoleHandle (4)` 로 `\Connect` 를 여는 것 (run83 a1) 과 맞고, run44 `[관찰]` (P2 `!handle` — 핸들 4 = `\Reference {ConDrv}`, 새 `0x60` = `\Connect {ConDrv}`, run44 문서 "새로 확인된 것" 4번) 과도 맞는다.
- `[소스]` test.exe · dll1 · dll2 의 콘솔 출력은 CRT 가 아니라 `Log` 의 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)` (`sample/log.h:38`). 빌드는 `/MT` (`sample/build.bat:12`) 라 ucrtbase.dll 도 안 쓴다.
- `[정적]` (flow-writer `.asm`) kernelbase `ConsoleCommitState` 는 a2 (`+0x91`) · a3 (`+0xf0`) 의 상태를 안 보고, `ConsoleInitialize` 는 commit 뒤 늘 AL 1 이다 — commit 쪽 syscall 실패로는 kernelbase 시작이 실패하지 않는다 (아래 ③).

## 결과서와 다르게 적은 곳 · 보탠 것

- ① `GetFileType` 을 부르는 줄은 **call `+0xc6` / ret `+0xcc`** 다. 결과서는 `+0xc3 ~ +0xcc` 범위로 적었다 (`+0xc3` 은 `MOV RCX,RBP` — 인자 싣기). 실패 갈래의 `FILE._file` 쓰기는 `+0x8d JNZ → +0xfd` · `+0x101` (`__piob` 가 이미 있을 때만).
- ② condrv `DriverEntry` 줄 맞춤 (flow-writer `.asm`): `+0x77 MOV R9D,0x50` (DeviceType) · `+0x120 MOV dword [RSP+0x20],0x20000` (Characteristics) · call `+0x128` / ret `+0x12f IoCreateDevice` · 이름 `[RSP+0x40]` = 길이 `0x1c` (14글자, 결과서 `\Device\ConDrv`) · `+0x144 StackSize ← 2` · `+0x14d Flags |= 0x10` · `+0x156 Flags &= ~0x80`. 결과서와 같은 값, 줄만 붙였다.
- ③ **commit 쪽 syscall 실패는 시작 실패가 아니다** (flow-writer `.asm`): `ConsoleCommitState` 는 `+0x8a/+0x91 NtDeviceIoControlFile(0x500023)` 뒤 `+0x91 NOP` → `+0x96`, `+0xe9/+0xf0 NtSetInformationProcess` 뒤 `+0xf0 NOP` → `+0xf5 CALL` 로 상태를 안 본다. 출력 칸 `[RSP+0x70]` 은 `+0x44` 에서 0 으로 두니 a2 가 실패하면 a3 값은 `0 | 1`. 그리고 `ConsoleInitialize` 는 commit 뒤 `+0x1cf JMP → +0x126 MOV AL,SIL` 로 끝나는데, `SIL` 은 `+0x1b XOR EDI,EDI` · `+0x6f LEA ESI,[RDI+0x1]` 로 1 이고 그 뒤 안 바뀐다. 그래서 결과서 (가) 표의 "`NtSetInformationProcess` 실패를 `ConsoleCommitState` 가 어떻게 다루는지 안 읽음 `[공백]` — `ConsoleInitialize` 가 AL=0 일 수 있다" 는 **시작 실패로는 안 이어진다 `[정적]`** 로 좁혔다. a4 는 결과서대로 오류 코드만.
- ④ (flow-writer, 이름만) sample 세 모듈의 `/MT` CRT 에도 같은 이름 함수가 있다 (`_index.tsv`): `initialize_stdio_handles_nolock` — test rva `0x5232c` · dll1 `0x50d04` · dll2 `0x50cf4` (모두 264B, ucrtbase 는 266B), `__acrt_initialize_lowio` — test `0x52434` · dll1 `0x50e0c` · dll2 `0x50dfc`, 그리고 세 모듈 모두 `GetFileType` 을 가져온다 (import 썽크 — test `0x6b8d5` · dll1 `0x6b637` · dll2 `0x6b627`). 본문은 안 읽었다. 결과서 "exe 쪽은 안 읽음 `[추측]`" 에 이름만 보탠다.
  - 그래서 `[추측]`: VM 에서는 dll2 · dll1 의 CRT 준비 (`dllmain_crt_process_attach`, 2.8 — **은퇴 앞**) 와 test.exe 의 CRT 준비 (`mainCRTStartup`, 은퇴 뒤) 가 각자 표준 핸들 셋에 `GetFileType` → `NtQueryVolumeInformationFile` 을 할 수 있다. run83 동적 4 의 "IOCTL 이 서는 건 … 있다면 CRT 초기화 쪽 `[추측]`" 은 ucrtbase 기준으로는 IOCTL 이 아니라 이 질의다. PE_emulator 는 표준 핸들이 0 이라 `GetFileType` 도 안 부른다 (위 표 1줄).
- ⑤ 결과서 질문 4 표의 "앱이 보내나 — 아니오 (conhost 몫)" 는 `[추측]` 으로 적었다 — 그 코드를 받는 객체가 Server · Broker · Display 뿐이고 (`[정적]`), kernelbase 가 그 코드를 안 보낸다 (sast/80 `[정적]`) 는 데서 짐작한 것이다.
- ⑥ condrv 쪽 `\Connect` 기준 객체 확인 (`CdCreateConnection` — Reference 종류여야 서버 연결) 으로 run44 `[관찰]` "핸들 4 = `\Reference {ConDrv}`" 이 코드와도 맞는다 (위 "확인된 것").
- `_calls.tsv` 의 `alias:` callee (`update_locale_slow+1` · `__mbsrtowcs_utf8+1` · `_o_memset+1` 등) 는 글에서 이름으로 안 썼다 — 줄기 밖 (로캘 · 변환 · 메모리 채우기).

## 정정

- **run83 뒷정리** (explainer 알림 — 같은 스프린트 문서라 그 자리에서 고침, 맨 위 정정 줄은 안 넣음):
  - ① run83 (b) "지금 우리" 줄의 "하나라도 실패를 돌려주면 kernelbase 시작 함수가 FALSE 다" 는 너무 넓다 → **"`ConsoleAllocate` 가 실패를 돌려주면 FALSE (`ConsoleInitialize +0x14d JNS` 안 탐 → AL 0 → `_KernelBaseBaseDllInitialize +0x528 JZ`) · commit 쪽 a4 (`GetConsoleLangId`) 실패는 오류 코드만 · a2 · a3 실패는 FALSE 근거 없음"** 으로 다듬었다. 근거를 더 본 것은 위 "보탠 것" ③ (run83 줄에도 한 줄 붙임).
  - ② run83 "결과서와 다르게 적은 곳 · 보탠 것" 에 ⑨ 한 줄: `ConsoleFlags & 2` 갈래의 `ConsoleSanitizeStandardIoObjects` 는 **call `+0x233` / ret `+0x238`** (결과서 sast/80 `+0x22f` 는 `+0x1c4 JNZ` 의 목적지 `LEA RCX,[RBP-0x40]`). 성공이면 `+0x23a JNS → +0x1c6` commit.
  - ③ run83 "정적 — 다음 바퀴 후보" 와 flow.md 4장 run83 정적 줄 → 대기열 **Q158** 로 적음.
- 이전 run 이 틀린 것은 없다. 결과서 sast/80 과 sast/81 도 서로 어긋난 곳이 없다 (결과서).

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 늘리지 않음): 기준 (ucrtbase · condrv pdb 두 줄 새로, kernelbase · ntdll pdb 줄에 run84 읽기 전용 몫, 근거 로그 run84 줄) · 2.8 `Log` 절 한 줄 (CRT 아님 + sample `/MT` CRT 이름만) · **3장 "콘솔 입출력" 절에 CRT · condrv 하위 절** ((가) · (나) 요약 포함) · 4장 (run83 정적 줄 → Q158 · "Q149b 로 넘길 것" 해결됨, run84 새 줄 셋) · 5장 (run 표 · 03_sprint 요청서 번호, run83 줄 Q158).
- 짐 지도 (짐 지도 바퀴 아님 — **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**): 7.6.8 "(커널) … condrv `\Connect` 파일 객체" 줄에 condrv 쪽 만드는 길 (`CdpDispatchCreate +0x47` → 표 0번 → `CdCreateConnection`, 기준 객체 Reference 확인, 종류 표 Connection) 을 근거로 붙임. 기준 run84 줄 · 판 목록 (마흔째판) · 8장 두 표.

## 아직 확인 필요

동적 — 결과서 "동적으로 확인해야 할 것" 번호 그대로, 모두 **Q104** 로 (이번 묶음은 VM 안 함. 멈춤점은 test.exe 조건). 무엇으로 볼 수 있나를 앞에 붙였다:

- 1 **[엔진 쪽 — PE_emulator]** 지금 PE_emulator 가 `kernelbase!ConsoleInitialize` 를 지났나, `ConsoleHandle` · `params+0xa4` (`WindowFlags`) 값, `ConsoleAllocate` 의 `NtCreateFile` 들에 무엇을 답하나 (run83 PE_emulator `[공백]` 과 같은 몫).
- 2 **[지금 test.exe 로 — cmd 리다이렉트만]** `test.exe > out.txt` 로 돌릴 때 `PEB→ProcessParameters` 의 `ConsoleHandle` · `StandardOutput` · `ConsoleFlags` · `WindowFlags`, `kernelbase!GetFileType+0x79` 의 `eax` · `DeviceType` (기대 7). 부모 cmd 가 `ConsoleHandle` 로 무엇을 넘기나 (진짜 핸들일 것 `[추측]`).
- 3 **[띄우는 쪽 새 샘플 필요 — 또는 정적 Q158 ① 로 먼저]** `DETACHED_PROCESS` 로 띄운 자식의 `params.ConsoleHandle` 이 −1 인가.
- 4 **[새 샘플 필요 — `printf` 만 쓰는 `/MD` 콘솔 프로그램]** C 로캘 CRT 가 콘솔에 실제로 `WriteFile` 로 쓰나: `ucrtbase!write_text_ansi_nolock+0xc7` (ret), `ucrtbase!write_requires_double_translation_nolock+0x89` (안 서야 함), `ucrtbase!initialize_stdio_handles_nolock+0x68` (ret 뒤 `rax` = 핸들) · `+0xcc` (ret 뒤 `eax` = 파일 종류). 지금 `sample/` 은 `/MT` + 직접 `WriteFile` 이라 ucrtbase 길을 안 탄다 `[소스]`.
- 5 **[새 샘플 필요 — 4 의 샘플 + `setlocale(LC_ALL, "")`]** 로캘 데이터 `+0x138` 이 0 이 아니게 되나, 그러면 `GetConsoleMode` 가 실제로 불리나 (`write_requires_double_translation_nolock+0x89`).
- 6 서버가 돌려주는 값 — **[지금 test.exe 로]** `NtWriteFile` 의 `Information` (`Log` 의 `WriteFile`, 진짜 콘솔에서 `Length` 와 같은가 — `nt!NtWriteFile` 반환 · `kernelbase!WriteFile+0x8d`), GetLangId `WORD` (`kernelbase!GetConsoleLangId+0xe8`, run83 Q104 2 와 같은 자리). **[새 샘플 필요]** `GetConsoleMode` 모드 · `GetConsoleOutputCP` 코드 페이지 (`kernelbase!GetConsoleMode+0xe2` · `GetConsoleOutputCP+0xe5`) — test.exe 는 콘솔 API 를 직접 안 부른다 `[소스]`.
- 7 **[새 샘플 필요 — 4 의 샘플을 리다이렉트로]** `stdout` 이 파일일 때 버퍼 크기 · 비우는 시점 (`ucrtbase!__acrt_stdio_flush_nolock+0x56`, 종료 때 `common_flush_all`). 정적으로도 일부 (아래 ②).
- 8 **[정적 후보로]** `CONOUT$` 의 NT 이름 · 루트 — 아래 ③.
- 9 **[정적 후보로 — msvcp140 analyze 필요]** `std::cout` 쓰기 길 — 아래 ④.
- 10 **[범위 밖 — 기록만]** 입력 쪽 (`_read_nolock` · `ReadConsoleW` · `std::cin`).
- (flow-writer 더함) **[지금 test.exe 로]** 위 "보탠 것" ④ — `nt!NtQueryVolumeInformationFile` 이 test.exe 조건으로 **은퇴 앞** (dll2 · dll1 CRT 준비) 에 서나, 선다면 몇 번 · 어느 핸들 (`0x68` · `0x6c` · `0x70`?) · 콜스택이 `dllN!initialize_stdio_handles_nolock` 인가 (`k`).
- 새 샘플 (4 · 5 · 6 일부 · 7) 은 하나로 묶을 수 있다: `printf` 만 쓰는 `/MD` 콘솔 프로그램 (+ `setlocale` 판, 리다이렉트로 한 번 더). 만들지 · 언제는 사용자 결정.

정적 — 다음 바퀴 후보 (대기열 번호는 오케스트레이터가):

- ① sample `/MT` CRT 의 lowio 초기화 (test · dll1 · dll2, 위 ④) — 초기화 표에 lowio 가 있나, dll1 · dll2 `dllmain_crt_process_attach` (run40) 아래에서 닿나 = 은퇴 앞 syscall 이 되나. ucrtbase 와 같은 갈림인지.
- ② `__acrt_stdio_allocate_buffer_nolock` (비 tty `stdout` 버퍼 크기) · 종료 때 비우는 길 (`exit` → `common_flush_all`) — (가) 에서 출력이 언제 나가나.
- ③ `CONOUT$` 의 NT 이름 · 루트 — ntdll `RtlDosPathNameToRelativeNtPathName_U_WithStatus` → `RtlpIsDosDeviceName_Ustr +0x2c7` (`RtlpDosCONOUTDevice` 읽기) 뒤 (결과서 `[공백]`).
- ④ msvcp140 `std::cout` 쓰기 길 (`fwrite` → `_fwrite_nolock_internal`? — 결과서 `[추측]`).
- ⑤ condrv `CdpCreateServerConnectionIo` — EA `"server"` 와 연결 데이터 `0x53c`B 를 읽는 자리 (결과서 `[공백]`), 중개 길 `CdpCreateBrokerConnectionIo` 는 이름만.
- ⑥ 작음: `process` 가 글자를 세는 줄 (`printf` 반환값 — 결과서 `[추측]`), `puts` SEH 람다 안쪽.
- (Q158 ① `CreateProcessInternalW` 의 −1 · −2 · −3 은 위 동적 3 과 같이 닫힘.)

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음. 새 함정 없음.
- **디버거가 붙어서 달라지는 점**: 이번 갈래를 가르는 칸 (`osfile` · `osfhnd` · 로캘 `+0x138` · `pmode` · `ConsoleHandle`) 을 읽는 줄 가운데 디버거 여부를 보는 줄은 없었다 `[정적: 읽은 줄 안에서]`.
- 도구 (결과서): `run_ghidra.sh` 에 `drivers/condrv.sys` 를 주면 모듈 이름이 `drivers/condrv` 가 되어 `condrv.sys` 로 불렀다 (`find_bin` 이 `drivers/` 아래를 찾아 줌). 요청서 문구와 달라서 적어 둔다. ucrtbase · condrv PDB 에 타입이 없어 (`pdb_struct.py --list` 0개) 칸 · 비트 · IOCTL 이름은 모두 `[추측: 이름]`.

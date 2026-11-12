# 81 console-crt — 정적 분석 결과

- 요청서: `_workspace/sast/81_console-crt.md` (Q149b, PE_emulator 요청 27 의 CRT·condrv 쪽 + 최소 조건 표). 짝 결과서 `_workspace/sast/80_console-kernelbase_result.md` (이하 "sast/80") 는 **읽고**, 겹치는 건 가리키기만 했다.
- 빌드: 26100.9457 (`ms/bin/.../System32/`).
  - `ucrtbase.dll` — PDB `ucrtbase.pdb`, guid_age `158F9F38CC492128FE8C9354A209CFB51` (`ms/sym/ucrtbase.dll.pdb`). 함수 5165개.
  - `drivers/condrv.sys` — PDB `condrv.pdb`, guid_age `B37C9D13666B9A282B3CEA43C4AECBF31` (`ms/sym/condrv.sys.pdb`). 함수 116개.
  - 커널 GUID 검사는 해당 없음 (ntoskrnl 안 씀).
  - 두 PDB 모두 **타입이 없다** (`pdb_struct.py --list ucrtbase.dll` 0개, condrv 도 0개). 그래서 구조체 칸 이름·`FILE` 비트 이름·IOCTL 이름은 전부 `[추측: 이름]` 이고, **칸 위치·값은 `.asm` 에서 읽은 `[정적]`** 이다.
- 분석: 이번에 처음 함 (둘 다 `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 확인, SCRIPT ERROR 없음). 로그 `ms/ghidra/ucrtbase.analyze.log`, `condrv.analyze.log`. ucrtbase 는 몇 분, condrv 는 6초.
- 지도 (`ms/disassemble/ucrtbase/_map/`): `__acrt_initialize_lowio` (함수 10개, 끊긴 끝 전부 import), `__stdio_common_vfprintf` (깊이 8, 92개, 공용 도구 15). condrv 는 지도 안 만듦 (IRP 처리 함수가 호출로 안 닿고 함수 표로만 닿아서 — 표는 `.rdata` 에서 직접 읽음).
- 뽑은 함수 (`run_ghidra.sh export`, 로그 `ms/ghidra/*.export.sast81*.stdout`, **SCRIPT ERROR 없음**):
  - ucrtbase 82개 (`ms/decomp/ucrtbase/`, `ms/disassemble/ucrtbase/`):
    - lowio·stdio 초기화 14개 depth 1 (`__acrt_initialize_lowio`·`initialize_stdio_handles_nolock`·`initialize_inherited_file_handles_nolock`·`__acrt_lowio_ensure_fh_exists`·`__acrt_lowio_create_handle_array`·`__acrt_initialize_stdio`·`__acrt_execute_initializers`·`_isatty`·`_isatty_proc`·`DllMainDispatch`·`entry` 등)
    - 쓰기 13개 depth 1 (`_write`·`_write_internal`·`_write_nolock`·`write_requires_double_translation_nolock`·`write_text_{ansi,utf8,utf16le}_nolock`·`write_double_translated_{ansi,unicode}_nolock`·`__acrt_stdio_begin_temporary_buffering_nolock`·`_get_osfhandle` 등)
    - printf·puts 사슬 18개 depth 0 (`puts`·`__stdio_common_vfprintf`·`common_vfprintf<standard_base,char>`·`common_flush_and_write_nolock<char>`·`write_buffer_nolock<char>`·`__acrt_stdio_flush_nolock`·`_fflush_nolock`·`_fwrite_nolock_internal` 등)
    - **넓힌 것**: `initialize_c`·`initialize_global_variables`·`initialize_pointers` (depth 0 — `__acrt_initialize_stdio` 가 어디서 불리는지 찾으려고), `_putwch_nolock`·`_cputws`·`_cputs`·`console_output_reopen_and_retry<…>` (depth 0 — `WriteConsoleW` 를 부르는 CRT 쪽 자리를 찾으려고).
  - condrv 30개 (`ms/decomp/condrv/`, `ms/disassemble/condrv/`): `DriverEntry`·`CdpDispatch*` 10개·`CdpFastIoDeviceControl`·종류별 `Cdp*Ioctl`/`Cdp*FastIoctl` 8개·`CdpClientWrite/Read/FlushBuffers`·`CdClientUserIo`·`CdCompleteIo`·`CdCreateConnection`·`CdCreateClient`·`CdCreateServer` 등 depth 0.
  - **같은 시간 다른 프로젝트**: sast/80 결과서가 나온 뒤 kernelbase `CreateFileW`·`CreateFileInternal` (depth 1) 과 ntdll `RtlpDosPathNameToRelativeNtPathName`·`RtlpIsDosDeviceName_Ustr` (depth 0, `refs RtlpDosCONOUTDevice`) 를 **읽기 전용으로 한 번씩** 뽑았다 (CRT 가 여는 `CONOUT$` 가 어떤 NT 이름이 되나 보려고). 코디네이터의 "열지 마" 알림이 오기 전에 끝났고, 그 뒤로는 kernelbase·user32·win32u·sxs·sxssrv·basesrv 를 안 열었다.
- 바이너리를 직접 읽은 것 (임시 스크립트, 레포 밖 scratchpad): condrv `.rdata` 의 `CdpObjectCreationTable`(12칸)·객체 종류 표 6개, ucrtbase `.rdata` 의 초기화 표(`0xf94d0`)·`__xi_a`~`__xi_z`(`0xfdbd8`), `.data` 의 `_iob` 초기값·`__acrt_initial_locale_data`. 그리고 `objdump -d` 로 `printf` 안쪽 람다(`0x1aa00`, 이름 없는 `operator()` — 같은 이름이 많아 export 안 함) 한 개.
- **버전 확인: 일치.** "버전 확인용 알려진 사실" 표에 ucrtbase·condrv 줄은 없다. 대신 두 가지로 대조했다.
  - (1) condrv 가 받는 IOCTL `0x500016`·`0x500023`·`0x500037` 과 열기 이름 `\Connect`·`\Reference`·`\Input`·`\Output`·`\Server` 가 **kernelbase 쪽 (sast/80) 이 보내는 값과 정확히 같다** (서로 다른 바이너리가 맞물림).
  - (2) `_calls.tsv` 의 `ret_off` 를 `objdump` 실제 바이트와 맞췄다 (`printf` 안쪽 람다 `0x1aa00` +0x43 `call 0x18001bcc0`, +0xb6 `call 0x18001ab40`, +0x114 `call 0x18001c100` 모두 지도의 `ret=+0x48/+0xbb/+0x119` 와 일치).
- 이 결과서의 `+0x` 는 `.asm` 줄에서 다시 찾아 확인했다 (부르기는 `ret=` 줄, 점프 목적지는 "목적지 − 함수 시작").
- **스크립트 문제: 없음.** (참고: `run_ghidra.sh` 는 `drivers/condrv.sys` 를 인자로 주면 모듈 이름이 `drivers/condrv` 가 되므로 `condrv.sys` 로 불렀다. 요청서 문구와 달라서 적어 둔다 — `find_bin` 이 `drivers/` 아래를 찾아 주긴 한다.)

## 한 줄 결론 (PE_emulator 용)

1. **test.exe 의 `NtWriteFile(0)` 26번은 CRT 가 아니다.** `sample/log.h:38` 의 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)` 직접 호출이고 (`/MT` 라 ucrtbase.dll 도 안 씀, `sample/build.bat`), 표준 핸들 칸이 0 이라 `NtWriteFile(0)` 이다. 콘솔 프로그램 7천 개 쪽 (CRT `printf`/`puts`) 은 아래처럼 다르게 움직인다.
2. **CRT 는 시작 때 표준 핸들마다 `GetStdHandle` + `GetFileType` 두 API 만 부른다** (`GetConsoleMode` 안 부름). 결과를 `osfile` 칸의 `0x40` (콘솔류 `FDEV`) / `0x08` (파이프) 비트로 적는다. 표준 핸들이 **0 이나 -1 이면 `GetFileType` 도 안 부르고** 그 칸을 `-2` (`_NO_CONSOLE_FILENO`) 로 두고 `FDEV` 를 켠다.
3. **기본 (C 로캘) `printf` 는 콘솔이어도 `WriteConsoleW` 가 아니라 `WriteFile` → `NtWriteFile` 로 간다.** `GetConsoleMode` 조차 안 부른다. 로캘이 C 가 아니거나 `_setmode(_O_U16TEXT 류)` 일 때만 `GetConsoleMode` 를 부르고, 그 안에서도 `WriteConsoleW` 는 유니코드 모드일 때만 (그것도 `CONOUT$` 를 CRT 가 따로 열어서).
4. **표준 핸들 0 이면 `printf` 는 조용히 계속 돈다.** abort 없음, 쓴 글자는 `stdout` 버퍼에 쌓이고 반환값은 글자 수, 실제 쓰기 (버퍼 비울 때) 에서 `errno = EBADF (9)` / `-1`. `NtWriteFile` 은 한 번도 안 나간다.
5. **최소 조건**: (가) 는 **`ConsoleHandle = -1`** 이어야 condrv 를 안 거친다 (0 은 표준 핸들이 파일이어도 `ConsoleAllocate` 로 간다 — sast/80). 이때 필요한 syscall 은 `NtSetInformationProcess` 1번 + 표준 핸들마다 `NtQueryVolumeInformationFile(FileFsDeviceInformation)` + `NtWriteFile` 뿐. (나) 는 syscall 이 `NtCreateFile` 1 + `NtDeviceIoControlFile` 2 (`0x500023`, `0x500016`+LangId) + `NtSetInformationProcess` 1 + 표준 핸들마다 `DeviceType = 0x50` 질의 + `NtWriteFile`. **CRT `printf` 만 쓰면 (나) 에서도 콘솔 IOCTL 은 `GetLangId` 하나뿐이다.**

## 질문별 답 [정적]

### 질문 1. CRT 가 처음 핸들 종류를 묻는 길

**어디서 불리나 (초기화 순서)**
- 진입 `entry` (`0x63c80`) `+0x23 tail → DllMainDispatch` (`0xbce0`). `DllMainDispatch` 가 `DLL_PROCESS_ATTACH` (인자 1) 일 때 `+0xc8` `__vcrt_initialize` → `+0xd5` `__isa_available_init` → `+0xe1/+0xe6` **`__acrt_execute_initializers(…, &PTR_s__1800f95d0)`**.
- 초기화 표 (`.rdata` rva `0xf94d0`, 16바이트 × 16칸, 앞 8바이트가 초기화 함수) 에서 읽은 순서: 0 `initialize_global_variables` · 1 `initialize_pointers` · 2 `__acrt_initialize_winapi_thunks` · 3 `initialize_global_state_isolation` · 4 `__acrt_initialize_locks` · 5 `__acrt_initialize_heap` · 6 (끄는 함수만) · 7 `__acrt_initialize_ptd` · **8 `__acrt_initialize_lowio`** · 9 `__acrt_initialize_command_line` · 10 `__acrt_initialize_multibyte` · 14 `initialize_environment` · **15 `initialize_c`**.
- `initialize_c` (`0x74b60`) 는 `__xi_a`~`__xi_z` (rva `0xfdbd8`) 를 돈다: `__acrt_initialize_clock`·`__acrt_initialize_fma3`·`__acrt_initialize_fmode`·`initialize_multibyte`·**`__acrt_initialize_stdio`**·`__acrt_initialize_timeset`. 곧 **lowio (8번) 가 stdio (15번) 보다 먼저**다.
- (정적으로 링크한 `/MT` 프로그램은 같은 함수가 exe 안에 들어 있다. 같은 소스라 같은 길일 것이라 보지만 exe 쪽은 이번에 안 읽음 `[추측]`.)

**lowio 초기화가 하는 일 (`__acrt_initialize_lowio`, `0x71f20`)**
- `+0xd` `EnterCriticalSection` → `+0x17/+0x1c` `__acrt_lowio_ensure_fh_exists(0)` (핸들 표 첫 묶음 64칸을 `_calloc_base(0x40, 0x48)` 로 만듦, 칸 하나 0x48바이트) → `+0x20/+0x25` **`initialize_inherited_file_handles_nolock`** → `+0x25/+0x2a` **`initialize_stdio_handles_nolock`**.
- 핸들 표 (`[추측: ioinfo]`): 표 주소 `__pioinfo` (rva `0x139e80`), 개수 `_nhandle` (`0x13a280`), 칸 크기 `0x48`. 칸 안: `+0x28` OS 핸들 (`osfhnd`) · `+0x38` 플래그 바이트 (`osfile`) · `+0x39` 텍스트 모드 종류 (0 ANSI / 1 UTF-8 / 2 UTF-16 — `_write_nolock` 의 분기로 확인).
- `osfile` 비트 (값은 `[정적]`, 이름은 CRT 관례 `[추측]`): `0x01` FOPEN · `0x08` FPIPE · `0x20` FAPPEND · `0x40` **FDEV** · `0x80` FTEXT.
- `FILE` 표 (`_iob`, rva `0x138c40`, 칸 `0x58`): `+0x14` 플래그, **`+0x18` `_file`(= fd)**. `__piob` (`0x139800`) 는 그 포인터 표.

**`initialize_inherited_file_handles_nolock` (`0x721f4`)** — 부모가 물려준 C 런타임 핸들 표
- `+0x26/+0x2b` `memset` 후 `+0x30/+0x36` **`GetStartupInfoW`** → `lpReserved2` (=`params` 의 `RuntimeData`) 와 `cbReserved2` 를 읽는다. 둘 다 있으면 `[개수 4바이트][플래그 개수바이트][핸들 8바이트 × 개수]` 모양으로 읽어, 칸마다 `핸들 ≠ -1·-2` 이고 `플래그 & 1` 이고 (`플래그 & 8` 이거나 `+0xf0/+0xf6` **`GetFileType` ≠ 0**) 이면 `osfhnd`·`osfile` 에 복사.
- 없으면 (PE_emulator 는 `RuntimeData` 를 안 채울 것 `[추측]`) 아무것도 안 함.

**`initialize_stdio_handles_nolock` (`0x720e4`) — 질문 1 의 핵심.** fd 0·1·2 에 대해:
- `+0x3a ~ +0x47`: 그 칸 `osfhnd + 2 ≤ 1` (즉 `-2` 또는 `-1`, 부모가 안 채운 칸) 일 때만 새로 정함. 이미 채워져 있으면 `+0xbc` 로 가서 `osfile |= 0x80` 만.
- `+0x49` `osfile = 0x81` (FOPEN | FTEXT). `+0x4e ~ +0xf8` 로 fd 0 → `GetStdHandle(-10)` · fd 1 → `-11` · fd 2 → `-12`. 호출은 `+0x62/+0x68` (**`GetStdHandle`**).
- `+0x6b ~ +0x73`: **`핸들 + 1 > 1`** 이 아니면 (= 핸들이 **0 또는 -1**) `GetFileType` 을 **안 부르고** 실패 갈래 `+0x75` 로.
- 아니면 `+0xc3 ~ +0xcc` **`GetFileType`** (`ret=+0xcc`). 결과 0 이면 실패 갈래 `+0x75`.
- 성공이면 `+0xd3` `osfhnd = 핸들`, `+0xd8 ~ +0xe9`: 2 (`FILE_TYPE_CHAR`) → **`|= 0x40` (FDEV)**, 3 (`FILE_TYPE_PIPE`) → **`|= 0x08` (FPIPE)**, 1 (`DISK`) → 더 안 함.
- 실패 갈래 `+0x75 ~ +0x8d`: **`osfile |= 0x40` (FDEV), `osfhnd = -2`**, 그리고 (`__piob` 가 이미 있으면) `+0x101` `FILE._file = -2`. `__piob` 는 이 시점에 아직 없으니 (stdio 가 나중) 진짜 `_file = -2` 는 `__acrt_initialize_stdio` 가 정한다.
- `__acrt_initialize_stdio` (`0x7b860`): 표준 `FILE` 셋의 잠금을 만들고, 해당 fd 의 `osfhnd + 2 < 3` (`-2`·`-1`·`0`) 이면 `FILE._file = 0xfffffffe` (**-2 = `_NO_CONSOLE_FILENO`**) 로 쓴다 (`.c` 의 `*puVar3 = 0xfffffffe`).

| `GetStdHandle` 값 | `GetFileType` | `osfhnd` | `osfile` | `FILE._file` |
|---|---|---|---|---|
| **0** 또는 **-1** | 안 부름 | **-2** | `0xC1` (FOPEN·FDEV·FTEXT) | **-2** |
| 그 밖 | 0 (UNKNOWN, 오류 포함) | **-2** | `0xC1` | **-2** |
| 그 밖 | 1 (DISK) | 핸들 | `0x81` | 0/1/2 그대로 |
| 그 밖 | 2 (CHAR) | 핸들 | `0xC1` (FDEV) | 그대로 |
| 그 밖 | 3 (PIPE) | 핸들 | `0x89` (FPIPE) | 그대로 |

- `_isatty` (`0xed400`) 가 보는 것: **`osfile & 0x40` 하나** — API 를 안 부른다. fd -2 면 `errno = 9` 하고 0 (abort 없음), 범위 밖 fd 면 `errno = 9` + `_invalid_parameter_noinfo`.
- **중요한 부작용**: 표준 핸들이 **NUL 장치** (`DeviceType 0x15` → kernelbase 가 `FILE_TYPE_CHAR` 로 바꿈, sast/80 질문 3) 이면 CRT 는 이것도 `FDEV` 로 본다.
- **지금 우리 (PE_emulator)**: **맞다** — 표준 핸들 0 이면 CRT 가 `GetFileType` 도 안 부르고 "콘솔 없음 (-2)" 로 둔다. 단 test.exe 는 CRT 를 안 거친다 (결론 1).

### 질문 2. `printf`·`puts`·`std::cout` — 콘솔이면 `WriteConsoleW`, 아니면 `WriteFile`?

**`printf` 는 ucrtbase.dll 에 없다** (`_index.tsv` 에 `printf` 0개). 응용 프로그램 쪽 헤더가 `__stdio_common_vfprintf(옵션, __acrt_iob_func(1), 서식, …)` 로 풀어 쓴다. `puts` (`0x1b480`) 는 ucrtbase 에 있다. 이름만 적는 사슬 (`.asm` ret 로 확인):

| 단계 | 함수 (rva) | 다음을 부르는 자리 (call/ret) |
|---|---|---|
| 1 | `__stdio_common_vfprintf` (`0x19df0`) | `+0x4e/+0x53` → `common_vfprintf<standard_base,char>` |
| 2 | `common_vfprintf<standard_base,char>` (`0x19e64`) | `+0x89/+0x8e` → SEH 람다 (`0x1a224`) — 안에서 `EnterCriticalSection` (`+0x1c`) |
| 3 | 람다 → 안쪽 `operator()` (`0x1aa00`) | `+0x43/+0x48` `__acrt_stdio_begin_temporary_buffering_nolock`, `+0xb6/+0xbb` `process` (서식 풀기), `+0x114/+0x119` `__acrt_stdio_flush_nolock` |
| 4 | `process` (`0x1ab40`) | `+0x1f9` `write_character_without_count_update` … |
| 5 | `write_character_without_count_update` (`0x1a960`) | `+0x38/+0x3d` `common_flush_and_write_nolock<char>` |
| 6 | `common_flush_and_write_nolock<char>` (`0x1bda0`) | → `write_buffer_nolock<char>` (`0x1bea0`) → `_write_internal` |
| 7 | `__acrt_stdio_flush_nolock` (`0x1c100`) | `+0x51/+0x56` `_write_internal` |
| 8 | `_write` (`0x830a0`) / `_write_internal` (`0x1c310`) | `+0xa9/+0xae` **`_write_nolock`** (`0x1c620`) — 잠금 안 |

- `puts`·`fputs`·`fwrite`·`fputc` 도 같은 `common_flush_and_write_nolock`/`__acrt_stdio_flush_nolock` → `_write_internal` 로 모인다 (`_calls.tsv` 의 callers 줄). `puts` 의 SEH 람다 안쪽은 안 읽음 `[추측]`.
- **`std::cout`**: msvcp140 은 안 열었다. MSVC STL 의 `cout` 이 `stdout` `FILE` 위의 `fwrite` 로 쓴다고 알려져 있어서 `_write` 로 모인다고 `[추측]`.

**버퍼링 (언제 `_write` 까지 가나)**
- `__acrt_stdio_begin_temporary_buffering_nolock` (`0x1bcc0`): `stderr` 는 **항상** 임시 버퍼 (`__acrt_stderr_buffer`), `stdout` 은 `+0x32/+0x37 _fileno` → `+0x39/+0x3e` **`_isatty`** 가 참일 때만 임시 버퍼 (4096바이트, `+0x75 _malloc_base(0x1000)`).
  - **표준 출력이 콘솔류 (FDEV) 이면**: `printf` 한 번의 출력을 임시 버퍼에 모았다가 끝에서 한 번 비운다 (`operator()` 의 `+0x103 ~ +0x10c` — `FILE` 플래그 비트 9 가 켜져 있으면 `+0x114` `__acrt_stdio_flush_nolock`). 곧 **`printf` 한 번 = `_write` 한 번 (≤ 4096바이트)**.
  - **아니면 (파일·파이프)**: `stdout` 은 보통 버퍼링이라 `_write` 는 버퍼가 차거나 `fflush`/종료 때 나간다. 버퍼 크기는 `__acrt_stdio_allocate_buffer_nolock` 안이라 안 읽음 `[공백]` (보통 4096 `[추측]`).
- `stdout`·`stderr` 의 `FILE._flag` 초기값은 `0x2002` (쓰기), `stdin` 은 `0x2001` (`.data` `_iob`).

**`_write_nolock` 의 갈림 줄 — 질문 2 의 핵심**
- `+0xc5/+0xca` **`write_requires_double_translation_nolock`**, `+0xca TEST AL,AL / +0xcc JNZ` → 참이면 "두 번 번역" 갈래.
- 거짓이면: `+0xd6 CMP byte [칸+0x38],0 / +0xdb JL` — **텍스트 모드 (FTEXT)** 면 `+0xdb JL → +0x176` 쪽에서 `칸+0x39` 로 갈린다 (0 → `+0x190/+0x195` **`write_text_ansi_nolock`**, 1 → `+0x24a` `write_text_utf8_nolock`, 2 → `+0x272` `write_text_utf16le_nolock`). **바이너리 모드면** `+0xff/+0x105` **`WriteFile`** 직접.
- `write_text_ansi_nolock` (`0x1d450`): `\n` 을 `\r\n` 으로 바꿔 (`+0x81 CMP AL,0xa`, `+0x88 MOV byte [RBX],0xd`) 스택 버퍼 (최대 `0x13ff` 바이트, `+0x9b CMP`) 에 담고 `+0xc1/+0xc7` **`WriteFile(osfhnd, 버퍼, 크기, &쓴수, NULL)`**. `+0xd2 CMP EAX,EBX`: **쓴 바이트가 요청보다 적으면 거기서 멈춘다.** → `NtWriteFile` 의 `IoStatusBlock.Information` 이 `Length` 와 같아야 한다. `_write_nolock` 끝에서 쓴 수가 0 이면 `errno = 28 (ENOSPC)` 하고 -1.
- 참이면 ("두 번 번역" = 콘솔에 로캘 글자를 올바르게 쓰려는 것):
  - 텍스트 ANSI (`+0x2af/+0x2b4` **`write_double_translated_ansi_nolock`**, `0x803e8`): `+0x76/+0x7c` `GetConsoleOutputCP` → 로캘 글자 → UTF-16 (`_mbtowc_internal`/`__mbsrtowcs_utf8`) → `+0x195/+0x19b` `WideCharToMultiByte(콘솔 출력 코드 페이지)` → **`+0x1c0/+0x1c6` 또는 `+0x2ee/+0x2f4` `WriteFile`**. 곧 이 갈래도 **`WriteConsoleW` 가 아니라 `WriteFile`**.
  - 텍스트 UTF-8/UTF-16 모드 (`+0x217/+0x21c` **`write_double_translated_unicode_nolock`**, `0x874f8`): 글자마다 `+0x3c/+0x41`, `+0x53/+0x58` **`_putwch_nolock`** (`0x87590`) → `+0x9/+0xe` `__dcrt_lowio_ensure_console_output_initialized` (`0x87764`: 처음 한 번 **`CreateFileW(L"CONOUT$", 0x40000000(GENERIC_WRITE), 공유 3, …, OPEN_EXISTING(3))`** — 이 CRT 핸들은 표준 핸들과 **별개**, 전역 `0x180138d50`) → `+0x38/+0x3d` `console_output_reopen_and_retry` (`0x875e8`) 의 **`+0x25/+0x2b` `WriteConsoleW(CONOUT$ 핸들, 글자 1개, 1, …)`**. 오류 6 (`ERROR_INVALID_HANDLE`) 이면 `+0x41` 닫고 `+0x63` `CreateFileW` 다시 열고 `+0x84` 한 번 더. 곧 **유니코드 모드 `printf` 는 글자 1개 = IOCTL 1번**.
  - `conio` (`_cputs`·`_cputws`·`_putch`) 도 같은 `_putwch_nolock` 길 (`_putch_nolock_internal +0xc0`).

**`write_requires_double_translation_nolock` (`0x1c940`) — "콘솔이면" 의 정확한 조건**
- `+0x17/+0x1c` `_isatty(fd)` → `+0x1e JZ` 거짓이면 끝 (FDEV 아님).
- `+0x4b CMP byte [칸+0x38],0 / +0x50 JGE` — 텍스트 모드 (FTEXT) 가 아니면 끝.
- **`+0x64 CMP qword [로캘데이터+0x138],0 / +0x6c JZ → +0x9e`**: 로캘 데이터 `+0x138` 이 0 이면 `+0xa1 CMP byte [칸+0x39],0 / +0xa6 JNZ` — **`pmode` (칸 `+0x39`) 도 0 이면 거짓으로 끝**.
- 둘 중 하나라도 켜져 있으면 `+0x7e` 에서 `칸+0x28` (osfhnd) 을 꺼내 `+0x83/+0x89` **`GetConsoleMode(osfhnd, &모드)`**, 성공 (≠0) 이면 참.
- **기본 상태**: ucrtbase 파일 그림의 `__acrt_initial_locale_data` (rva `0x1380b0`) 의 `+0x138` = **0**, 표준 칸의 `pmode` = 0 (`__acrt_lowio_create_handle_array` 가 `+0x39` 를 0 으로 둠). 그러니 **`setlocale` 도 `_setmode` 도 안 한 프로그램은 `GetConsoleMode` 를 한 번도 안 부르고 `write_text_ansi_nolock` → `WriteFile`** 로 간다. `+0x138` 칸 이름은 `[추측: 로캘 이름 포인터]`, `setlocale(LC_ALL, "")` 뒤 값은 `[동적]`.

**`GetConsoleMode` 를 언제 부르나 (질문 2 끝)**
- CRT 시작 (질문 1) 에서는 **안 부른다.**
- 쓸 때는 위 조건 (FDEV ∧ 텍스트 ∧ (로캘 `+0x138` ≠ 0 ∨ `pmode` ≠ 0)) 에서만, **`_write_nolock` 한 번 부를 때마다** (캐시 없음). `printf` 가 임시 버퍼를 한 번 비우는 것이 `_write` 한 번이다.
- 읽기 쪽 (`_read_nolock` 등) 과 `std::cin` 은 이번에 안 읽었다 `[공백]`. `__dcrt_get_input_console_mode`/`__dcrt_set_input_console_mode`/`__dcrt_read_console_input` (conio 입력) 이름만 확인.

- **지금 우리 (PE_emulator)**: **일부 틀린 가정을 고쳐야 한다.** 요청서의 "FDEV + `GetConsoleMode` 성공이면 `WriteConsoleW`" 는 실제로는 "**(로캘 ≠ C 또는 유니코드 모드) ∧ FDEV ∧ 텍스트 ∧ `GetConsoleMode` 성공** 이면 이중 번역, 그것도 ANSI 는 `WriteFile`, 유니코드만 `WriteConsoleW`(`CONOUT$`)". 기본 `printf` 는 **`WriteFile` → `NtWriteFile`** 이라, kernelbase 에 콘솔 갈림이 없다는 sast/80 결론과 이어져 **에뮬레이터는 `NtWriteFile` 한 곳만 콘솔을 알면 된다.**

### 질문 3. 표준 핸들이 0 (콘솔 없음) 일 때 CRT 결과

| 상황 | 결과 | 근거 |
|---|---|---|
| CRT 시작 | fd 0·1·2 모두 `osfhnd = -2`, `osfile = 0xC1`, `FILE._file = -2`. 시작은 성공 (`__acrt_initialize_lowio` 반환 true) | `initialize_stdio_handles_nolock +0x6b ~ +0x8d`, `__acrt_initialize_stdio` |
| `printf`/`puts`/`fwrite(stdout)` | **조용히 성공한 것처럼 돈다.** `stdout` 은 `_isatty(-2)` 가 0 (`errno = 9` 만 세움) 이라 임시 버퍼를 안 쓰고, 첫 글자는 `common_flush_and_write_nolock<char>` 가 버퍼 (`__acrt_stdio_allocate_buffer_nolock`) 를 만들어 담는다 (`_flag = 0x2002` 라서 `common_flush_and_write_nolock<char>` 첫머리의 쓰기 비트 검사 통과). **반환값은 글자 수일 것** (`process` 가 글자를 세는 줄은 직접 안 읽음 `[추측]`). `_write` 는 버퍼가 차기 전엔 안 불린다 | `__acrt_stdio_begin_temporary_buffering_nolock +0x39`, `common_flush_and_write_nolock<char>`, `write_buffer_nolock<char>` (`fd+2 < 2` 면 `__badioinfo` 를 써서 안 터짐) |
| 버퍼를 비울 때 (`fflush`·4096 넘음·종료) | `_write_internal(-2, …)`: **`+0x29 CMP EBX,-2 / +0x2c JZ → +0x122`** → `errno = 9 (EBADF)`, `_doserrno = 0`, **-1 반환. `_invalid_parameter` 안 부름** (그래서 abort·예외 없음). `__acrt_stdio_flush_nolock` 은 `FILE` 플래그 `0x10` 을 세우고 -1 (`[추측: _IOERROR]`). `fflush` 는 오류 반환 `[추측]` | `_write_internal +0x122 ~ +0x13d`, `__acrt_stdio_flush_nolock` (`.c`) |
| 프로그램 | **계속 돈다.** `printf` 가 중간에 실패하지 않고, 마지막 버퍼 비우기만 조용히 실패 | 같음 |
| `_write(1, …)` 를 앱이 직접 | fd 1 은 `FOPEN` 이 켜져 있어 통과 → `_write_nolock` → (C 로캘) `write_text_ansi_nolock` → **`WriteFile((HANDLE)-2, …)` → `NtWriteFile(-2, …)`**. -2 는 `NtCurrentThread` 와 같은 값이라 커널이 무엇을 돌려주는지는 `[동적]` (아마 `STATUS_OBJECT_TYPE_MISMATCH` `[추측]`) | `_write_nolock +0x190`, `write_text_ansi_nolock +0xc1` |
| `_get_osfhandle(fd)` | fd = -2 (`_fileno(stdout)`) 면 `errno = 9`, -1 반환. fd 1 이면 -2 (그대로) 반환 | `_get_osfhandle (.c)` |
| 앱이 `GetStdHandle(-11)` 을 직접 | 0 그대로 (sast/80 질문 4) → `WriteFile(0, …)` → `NtWriteFile(0)` → `0xc0000008` ← **test.exe 가 이 경우** | `sample/log.h:38` |

- 표준 핸들 칸이 0 인 이유가 `params.WindowFlags` 의 `0x200`/`0x400` 비트일 수도 있다: `GetStdHandle +0x1d TEST [params+0xa4],0x400 / +0x27 JNZ` (출력), `+0x49 … 0x200 / +0x53 JNZ` (입력) → `+0x98 XOR EBX,EBX` 로 **칸을 읽지도 않고 0** (`kernelbase!GetStdHandle`, 이번에 `.asm` 로 다시 확인). 이 비트는 `STARTF_USEHOTKEY`(0x200)/`STARTF_HASSHELLDATA`(0x400) 로 `[추측]`. → 동적 확인 3번.
- **지금 우리 (PE_emulator)**: CRT `printf` 쪽은 **걱정 안 해도 된다** — 표준 핸들 0 이어도 abort 없이 돈다. 다만 test.exe 같은 `WriteFile(GetStdHandle(...))` 직접 호출은 `0xc0000008` 로 실패한다 (이건 코드대로).

### 질문 4. condrv.sys 가 받는 IOCTL (선택, 대조용)

**기본 모양 (`DriverEntry`, `0x10010`)**
- `+0x77` 에서 `R9D = 0x50`, `+0x120` 스택에 `0x20000` 을 놓고 `+0x128/+0x12f` **`IoCreateDevice(DriverObject, 0, L"\Device\ConDrv", DeviceType = 0x50 (FILE_DEVICE_CONSOLE), 특성 = 0x20000, FALSE, &장치)`**. 장치 `StackSize = 2`, `Flags |= 0x10 (DO_DIRECT_IO)`. → 콘솔 객체에 `NtQueryVolumeInformationFile(FileFsDeviceInformation)` 하면 `DeviceType = 0x50` 이 나온다 (sast/80 `GetFileType +0x87 CMP EAX,0x50`).
- 디스패치 표 (`DriverObject + 오프셋`): `+0x70` Create=`CdpDispatchCreate` · `+0x80` Close · `+0x88` Read · `+0x90` Write · `+0xb8` FlushBuffers · `+0xe0` DeviceControl=`CdpDispatchDeviceControl` · `+0x100` Cleanup · `+0x110`/`+0x118` Query/SetSecurity · `+0x68` Unload · `+0x50` FastIo 표 (`CdpFastIoDispatchTable`, **`FastIoDeviceControl` 만** `CdpFastIoDeviceControl`).
- 읽기·쓰기·IOCTL 은 모두 **"파일 객체의 종류 표"** 를 따라 간다: `CdpDispatchWrite`/`Read`/`DeviceControl` 이 `FileObject → FsContext2 → 종류 표` 를 찾고 (없으면 `0xC000000D` `STATUS_INVALID_PARAMETER`), 표의 해당 칸이 비어 있으면 Write·Read·Flush 는 `0xC00000BB` (`STATUS_NOT_SUPPORTED`), IOCTL 은 `0xC0000010` (`STATUS_INVALID_DEVICE_REQUEST`).
- 종류 표 칸 (`+8` Read · `+0x10` Write · `+0x18` IOCTL · `+0x20` Cleanup · `+0x28` Close · `+0x30` FastIoctl · `+0x38` Flush · `+0x40` QuerySecurity). `.rdata` 에서 읽은 값:

| 종류 (`CdXxxType`, rva) | Read/Write/Flush | IOCTL (IRP) | FastIoctl |
|---|---|---|---|
| **Client** (`0x43c0`) | `CdpClientRead` · `CdpClientWrite` · `CdpClientFlushBuffers` | `CdpClientIoctl` | 없음 |
| **Connection** (`0x42e0`) | 없음 | `CdpConnectionIoctl` | `CdpConnectionFastIoctl` |
| **Server** (`0x4290`) | 없음 | `CdpServerIoctl` | `CdpServerFastIoctl` |
| **Reference** (`0x4370`) | 없음 | 없음 | 없음 (닫기만) |
| Broker (`0x4410`) | 없음 | `CdpBrokerIoctl` | `CdpBrokerFastIoctl` |
| Display (`0x4460`) | 없음 | 없음 | `CdpDisplayFastIoctl` |

  → **응용 프로그램 쪽 (콘솔 입출력)에서 `NtWriteFile`/`NtReadFile`/`FlushFileBuffers` 를 받을 수 있는 건 Client 종류뿐.** Connection 에 쓰면 `0xC00000BB`.

**`IRP_MJ_CREATE` (`CdpDispatchCreate`, `0xebd0`)** — 열기 이름 12개를 `+0x47/+0x4e RtlCompareUnicodeString(FileObject 이름 = [파일객체+0x58], 표[i], 대소문자 무시)` 로 차례로 맞춰 본다 (`+0x31 CMP EBX,0xc`). 못 찾으면 `+0x8b`/`+0xa9` **`0xC0000225` (`STATUS_NOT_FOUND`)**. `.rdata` `CdpObjectCreationTable` (rva `0x44b0`, 칸 `0x20`: 이름 `UNICODE_STRING` 16B + 함수 8B + 인자 8B):

| # | 이름 | 처리 함수 | 인자 | kernelbase 가 여는 자리 (sast/80) |
|---|---|---|---|---|
| 0 | `\Connect` | `CdCreateConnection` | 0 | a1, b5 |
| 1 | `\KernelConnect` | `CdCreateKernelConnection` | 0 | (kernelbase 는 안 엶 `[추측: 커널 내부용]`) |
| 2 | `\Reference` | `CdCreateReferenceObject` | 0 | b2 |
| 3 | `\Input` | `CdCreateDefaultObjectClient` | 1 | b6 |
| 4 | `\Output` | `CdCreateDefaultObjectClient` | 2 | b7 |
| 5 | `\Server` | `CdCreateServer` | 0 | b1 |
| 6 | `\Console` | `CdCreateClient` | 4 | |
| 7 | `\CurrentIn` | `CdCreateClient` | 1 | `CONIN$` 일 것 `[추측]` |
| 8 | `\CurrentOut` | `CdCreateClient` | 2 | `CONOUT$` 일 것 `[추측]` |
| 9 | `\ScreenBuffer` | `CdCreateClient` | 3 | |
| 10 | `\Broker` | `CdCreateBroker` | 0 | 중개 길 |
| 11 | `\Display` | `CdCreateDisplayObject` | 0 | |

- `CdCreateConnection` (`0xd5a0`): EA 이름 **`"attach"`** (`CdpFindEaBufferItem`) 가 있고 값 크기가 8 이면 서버 연결 (`CdpGetProcessServerFromConnection`, 접근 검사 `CdAccessCheck`). 없으면 `RelatedFileObject` (= 열 때 `RootDirectory`) 가 없을 때 `CdpCreateBrokerConnectionIo`, 있을 때 **그 객체 종류가 Reference 여야** (아니면 `0xC0000008`) `CdpCreateServerConnectionIo`. 곧 kernelbase 가 `\Connect` 를 `RootDirectory = \Reference 핸들` 로 여는 것 (sast/80 a1·b5) 과 맞는다. `"server"` EA 를 condrv 쪽에서 읽는 줄은 이 함수엔 없다 (더 안쪽 `CdpCreateServerConnectionIo` `[공백]`).

**IOCTL 목록** (`CTL_CODE` 규칙으로 푼 함수 번호 = `(코드 >> 2) & 0xfff`, 방식 = `코드 & 3`. 장치 종류는 모두 `0x50`. **이름은 PDB 에 없어서 전부 `[추측]`**, 값·처리 함수는 `.asm` 의 비교·점프표):

| 코드 | 번호·방식 | 받는 종류 (함수, 근거) | 하는 일 (condrv 안쪽은 이름만) | 앱이 보내나 |
|---|---|---|---|---|
| **`0x500016`** | 5·OUT_DIRECT | **Client** `CdpClientIoctl +0x13 CMP [..+0x18],0x500016` (맞으면 `CdClientUserIo`; 틀리면 `0xC0000010`; 파일 접근에 쓰기 비트(2) 없으면 `0xC000000D`) · **Connection** `CdpConnectionIoctl +0x2d` | 설명서 (sast/80 의 `CD_USER_DEFINED_IO` 칸) 를 읽어 입출력 버퍼를 MDL 로 잡고 `CdAddIoToPipe` 로 서버(conhost)에 넘김 `[추측: ISSUE_USER_IO]` | **예** (`GetConsoleMode` 류, `WriteConsoleW`, `ReadConsoleW`, `GetLangId`) |
| **`0x500023`** | 8·NEITHER | Connection `CdpConnectionFastIoctl +0x26 CMP [..],0x500023` (**출력 길이가 8 이어야**, 아니면 `0xC0000206`) | 서버 쪽 값 8바이트를 출력에 씀 `[추측: GET_SERVER_PID]` | **예** (kernelbase a2) |
| **`0x500037`** | 13·NEITHER | Server `CdpServerFastIoctl` 점프표 칸 `0x2c` → `CdpLaunchServerProcess` | 입력의 `RTL_USER_PROCESS_PARAMETERS` 로 conhost 를 띄움 `[추측: LAUNCH_SERVER]` | **예** (kernelbase b3) |
| `0x500006` | 1·OUT_DIRECT | Server `CdpServerIoctl +0x14`/`CdpServerFastIoctl +0x37`, Broker `CdpBrokerIoctl +0xe` | 서버가 "다음 일 줘" (`CdReadNextIo`/`CdpCompleteReadIoIrp`) `[추측: READ_IO]` | 아니오 (conhost 몫) |
| `0x50000b` | 2·NEITHER | Server·Broker FastIoctl → `CdCompleteIo` / `CdpCompleteBrokerIo` | 서버가 일을 끝냄 — **보류 중인 IRP 의 상태·`Information` 을 서버가 준 값으로 채워 완료** (`CdCompleteIo` `.c`: `IRP.IoStatus.Status ← iStack_30`, `.Information ← local_28`) `[추측: COMPLETE_IO]` | 아니오 |
| `0x50000f` | 3·NEITHER | Server·Broker FastIoctl → `CdReadIoInput` | `[추측: READ_INPUT]` | 아니오 |
| `0x500013` | 4·NEITHER | Server FastIoctl → `CdWriteIoOutput` | `[추측: WRITE_OUTPUT]` | 아니오 |
| `0x50001b` | 6·NEITHER | Server·Broker FastIoctl → `CdDisconnectIoPipe` | `[추측: DISCONNECT_PIPE]` | 아니오 |
| `0x50001f` | 7·NEITHER | Server FastIoctl → `CdpSetServerInformation` | `[추측: SET_SERVER_INFORMATION]` | 아니오 |
| `0x500033` | 12·NEITHER | Server FastIoctl 칸 `0x28`: 서버 객체 `+0x30 |= 1` (푸시락 안) | `[추측: ALLOW_VIA_UIACCESS]` | 아니오 |
| `0x500027` / `0x50002b` / `0x50002f` / `0x50003b` | 9 / 10 / 11 / 14 | Display `CdpDisplayFastIoctl` → `CdpGetDisplaySize` / `CdpUpdateDisplay` / `CdpSetCursorInformation` / `CdpGetFontSize` | 화면 크기·갱신·커서·글꼴 `[추측]` | 아니오 (conhost 몫) |

- 그 밖의 코드: Client·Connection·Broker·Server 의 IRP 경로는 **`0xC0000010`** (`STATUS_INVALID_DEVICE_REQUEST`) 로 끝낸다 (`CdpClientIoctl`·`CdpBrokerIoctl`·`CdpServerIoctl` `.c`, 서버는 반환값만 `-0x3ffffff0` 로 보임). Display 는 `0xC0000010`.
- **쓰기·읽기 (`IRP_MJ_WRITE`/`READ`, Client 만)**: `CdpClientWrite` (`0xa890`) 는 요청한 프로세스의 연결 (`CdGetProcessConnection`) 을 찾아 `CdAddIoToPipe` 에 넣고 **`0x103` (`STATUS_PENDING`)** 을 돌려준다. 실제 완료 (상태·쓴 바이트 수 = `Information`) 는 서버가 `0x50000b` 로 채운다. 요청 프로세스를 못 찾으면 `0xC000000B`, 연결이 맞지 않으면 `0xC0000008` (`INVALID_HANDLE`). kernelbase `WriteFile` 은 `0x103` 이면 `NtWaitForSingleObject` (sast/80 표 A) 로 기다린다. `CdpClientRead` 는 같은 모양 (`CdInitializeIo` 에 내부 번호 6), `CdpClientFlushBuffers` 는 내부 번호 8.
- **`FileFsDeviceInformation` 질의**: condrv 에 처리 함수가 따로 없다 (`NtQueryVolumeInformationFile` 은 I/O 관리자가 장치 객체로 답함) → `DeviceType = 0x50`, `Characteristics = 0x20000` `[추측: 장치 객체 값 그대로]` (`DriverEntry` 인자).
- **condrv 안쪽 (conhost 로 넘기는 길)**: `CdAddIoToPipe`·`CdReadNextIo`·`CdpCompleteReadIoIrp`·`CdCompleteIo`·`CdpCreateMdlChain`·`CdpLaunchServerProcess` — 이름만.
- **sast/80 의 "Q149b 로 넘길 것" 대조**: IRP_MJ_CREATE 이름 6개 ✔(`\Server`·`\Connect`·`\Reference`·`\Input`·`\Output` + 상대 열기), EA `"attach"` ✔ (위), `"broker"` — 이름 문자열 자체는 `CdCreateConnection` 이 `"attach"` 만 찾고 `\Broker` 는 별도 열기 이름 (`CdCreateBroker`), `"server"` EA 는 `CdCreateConnection` 안에선 안 찾음 `[공백]`, IOCTL `0x500016`/`0x500023`/`0x500037` ✔, `IRP_MJ_WRITE`/`READ` ✔ (Client 만), `FileFsDeviceInformation` ✔ (장치 종류 `0x50`).
- **지금 우리 (PE_emulator)**: condrv 를 흉내 낼 필요가 있는 건 **앱이 보내는 IOCTL 3개 (`0x500016`, `0x500023`, `0x500037`)** 와 Client 객체의 `NtWriteFile`/`NtReadFile`/`NtFlushBuffersFile`, 열기 이름 5~6개뿐이다. conhost 쪽 IOCTL(`0x500006`·`0x50000b`~`0x50003b`) 은 에뮬레이터가 "서버 몫을 즉시 끝내 주는" 식이면 안 흉내 내도 된다.

### 질문 5. 최소 조건 표 둘

> 합치는 근거: kernelbase 쪽 줄 번호는 sast/80 (`ConsoleInitialize`·`ConsoleCommitState`·`GetStdHandle`·`GetFileType`·`WriteFile`·`ConsoleCallServer*`·`WriteConsoleInternal`) 과 `ms/disassemble/kernelbase/*.asm`. 이번에 `.asm` 에서 다시 본 kernelbase 줄: `ConsoleInitialize +0x166/+0x170/+0x177/+0x179/+0x183/+0x189/+0x19a/+0x1ca`, `GetFileType +0x15/+0x18/+0x55/+0x58/+0x72/+0x87/+0x8c/+0xa6`, `GetStdHandle +0x1d/+0x27/+0x49/+0x53/+0x66/+0x98`, `_KernelBaseBaseDllInitialize +0x521/+0x526/+0x528`.
> sast/80 의 두 가지 중요한 결론을 반영했다: (1) **`WriteFile`/`ReadFile` 에는 콘솔 갈림이 없고 `NtWriteFile`/`NtReadFile` 로 바로 간다.** (2) **콘솔 앱 (Subsystem 3) 이 `ConsoleHandle = 0` 이면 조용히 끝나지 않고 `ConsoleAllocate` (새 콘솔) 로 간다.**

#### (가) 파일 리다이렉트처럼 — 콘솔 서버 없이 가는 길

핵심: 표준 핸들이 파일·파이프여도 `ConsoleHandle = 0` 이면 `ConsoleAllocate` 가 불려 **condrv 가 필요해진다** (`ConsoleInitialize +0x170 MOV RDX,[RBX+0x10]` / `+0x177 JZ → +0x142 ConsoleAllocate`). 이걸 피하는 값이 `-1` (`+0x179 CMP RDX,-1` → `+0x1c6 → ConsoleCommitState` 빈 상태, `+0x1ca/+0x1cf`).

| 항목 | 값 | 근거 줄 | 이게 없으면 |
|---|---|---|---|
| exe 의 `Subsystem` | 3 (콘솔) 그대로 | `ConsoleInitialize +0x7b CMP [NT+0x5c],3` (sast/80 질문 1 표) | GUI 로 바꾸면 `ConsoleIsConsoleApplication = 0` 의 다른 갈래를 탐 — 7천 개 콘솔 프로그램의 실제 모습과 달라짐 |
| `params.ConsoleHandle` (`+0x10`) | **`-1` (`0xFFFFFFFFFFFFFFFF`)** | `ConsoleInitialize +0x179 CMP RDX,-1 / JZ → +0x1c6` → `ConsoleCommitState` 빈 상태 → AL=1 (sast/80 질문 1 표 3번째 줄). `-1` 이 무슨 `CreateProcess` 플래그에서 오는지는 `[추측]` | `0` / `-2` / `-3` → **`+0x142 ConsoleAllocate`** (condrv 를 열려 시도, 실패하면 AL=0 → `_KernelBaseBaseDllInitialize +0x526 TEST AL / +0x528 JZ` → **kernelbase DllMain FALSE = 프로세스 시작 실패**). 진짜 핸들 → (나) 갈래 |
| `params.ConsoleFlags` (`+0x18`) | `0` | `ConsoleInitialize +0x166 TEST [RBX+0x18],4` (콘솔 핸들이면 닫음), `+0x1b9` 비트 2 (표준 핸들 검사·교체 `ConsoleSanitizeStandardIoObjects`) | 비트 4·2 가 켜지면 표준 핸들마다 `NtQueryVolumeInformationFile` (+ 콘솔 장치면 `NtClose`) 이 추가로 나감 (sast/80) |
| `params.WindowFlags` (`+0xa4`) 의 `0x200`, `0x400` 비트 | 둘 다 **0** | `GetStdHandle +0x1d TEST [+0xa4],0x400 / +0x27 JNZ`, `+0x49 … 0x200 / +0x53 JNZ` → `+0x98 XOR EBX,EBX` | 켜져 있으면 입력/출력 `GetStdHandle` 이 칸을 읽지도 않고 **0 을 돌려줌** → 표준 핸들 0 처럼 보임 |
| `params.StandardOutput` (`+0x28`)·`StandardError` (`+0x30`) (·`StandardInput` `+0x20`) | **서로 다를 필요 없는, 0 도 -1 도 아닌 핸들 값** (예: 엔진이 연 파일 핸들) | `initialize_stdio_handles_nolock +0x6b ~ +0x73` (0·-1 이면 `GetFileType` 도 안 부름), `GetStdHandle +0x66 CMP RBX,-1` | 0·-1 이면 CRT 는 "콘솔 없음 (-2)" 로 둠 — `printf` 는 조용히 버려짐 (질문 3) |
| 열어 둘 핸들의 종류 | **동기(synchronous) 파일 핸들** (디스크 파일) 또는 이름 있는 파이프. 쓰기 권한 | `WriteFile (sast/80 표 A)`: `NtWriteFile(hFile,0,0,0,&IoStatus,buf,len,0,0)` — `ByteOffset` NULL 이라 파일 객체의 현재 위치에 씀. `0x103` 이면 `NtWaitForSingleObject` | 비동기 핸들이면 `0x103` 후 대기 경로 (`WriteFile +0x1d1`) 를 엔진이 흉내 내야 함 |
| `NtQueryVolumeInformationFile(FileFsDeviceInformation = 4, 길이 8)` 에 줄 답 | 상태 `STATUS_SUCCESS`, 출력 `{ DeviceType, Characteristics }` 의 **`DeviceType = 7 (FILE_DEVICE_DISK)`** (파일) 또는 **`0x11 (FILE_DEVICE_NAMED_PIPE)`** (파이프). `Characteristics` 는 아무 값 | `GetFileType +0x72/+0x79` (호출), `+0x5a R9D=8`·`+0x60 [RSP+0x20]=4`; 점프표 (sast/80 질문 3): `2~8, 0x24 → DISK(1)`, `0x11 → PIPE(3)` | `DeviceType` 이 `0x50` 이면 CHAR → CRT 가 `FDEV` 로 봄 (콘솔 취급). 상태가 음수면 `GetFileType` 이 0 → CRT 가 표준 핸들을 **-2 (없음) 로 버림** (질문 1) |
| `NtWriteFile` 에 줄 답 | `STATUS_SUCCESS` (또는 `0x103` 없이 바로), `IoStatusBlock.Information = Length` (요청한 바이트 수 그대로) | `WriteFile +0xac ~ +0xb0` (Information → `*lpNumberOfBytesWritten`), CRT `write_text_ansi_nolock +0xc7 TEST EAX / +0xd2 CMP EAX,EBX` (적게 쓰면 멈춤), `_write_nolock` (0 바이트면 `errno 28`) | `Information` 이 작으면 `printf` 반환값·쓴 양이 줄고 멈춤. 실패 상태면 `_write` 가 -1, `errno` 는 `__acrt_errno_from_os_error` |
| `NtSetInformationProcess(-1, 0x31 (ProcessConsoleHostProcess), &{1}, 8)` | `STATUS_SUCCESS` | `ConsoleCommitState +0xe9/+0xf0` (항상 불림), `ConsoleInitialize +0x1c6 → +0x1ca/+0x1cf` | 실패 상태를 `ConsoleCommitState` 가 어떻게 다루는지는 안 읽음 `[공백]` — 실패하면 `ConsoleInitialize` 가 AL=0 일 수 있다고 보고 성공을 줄 것 |
| `params.RuntimeData` (`lpReserved2`/`cbReserved2`) | **비움 (0)** | `initialize_inherited_file_handles_nolock +0x30/+0x36 GetStartupInfoW` 뒤 `cbReserved2 ≠ 0` 일 때만 복사 | 채우면 CRT 가 거기 적힌 핸들·플래그를 표준 핸들보다 먼저 `osfhnd` 에 복사 (`GetStdHandle` 값을 덮어씀) |
| 콘솔 API 답 (`GetConsoleMode` 등) | 필요 없음 — kernelbase 가 `ConsoleHandle ≤ 0` 이면 **syscall 없이 `0xC0000008`** | sast/80 표 B (`GetConsoleMode +0x5f/+0x62`) | 앱이 `GetConsoleMode(stdout)` 으로 리다이렉트를 가려내면 "콘솔 아님" 으로 봄 (진짜 리다이렉트와 같음) |
| `KUSER_SHARED_DATA.SharedDataFlags` (`0x7ffe02f0`) | 상관없음 | `-1` 갈래는 `ConsoleAllocate` 를 안 탐 | |

- **CRT·kernelbase 가 이것만으로 끝까지 가나**: **예** (정적으로는). 필요한 syscall 은 시작 때 `NtSetInformationProcess` 1번, 표준 핸들마다 `NtQueryVolumeInformationFile` (CRT `GetFileType` 3개 + 앱이 따로 부르면 더), 그리고 `NtWriteFile`. `printf`/`puts` 는 `stdout` 이 파일이라 버퍼링이어서 **`NtWriteFile` 은 버퍼 비울 때 (종료 포함) 나간다** — 종료 때 비우는 길 (`exit` → `common_flush_all`) 은 이번에 안 따라갔다 `[공백]`.
- 단점: `-1` 이 실제 Windows 의 어떤 시작 방식에서 나오는지 모른다 (`[추측]`). 또 앱이 `GetConsoleMode`/`WriteConsoleW`/`SetConsoleTitle` 처럼 콘솔 API 를 직접 부르면 `0xC0000008`/`ERROR_INVALID_HANDLE` 가 난다 — 진짜 리다이렉트 때와 같다.

#### (나) 가짜 condrv 콘솔 — 두 가지 길

**길 A: 붙기 (`ConsoleHandle` = 진짜 핸들 H0, VM 의 test.exe 가 이 길: H0 = 4)** — sast/80 질문 1 (a) 와 같은 syscall 4개.

| 항목 | 값 | 근거 줄 | 이게 없으면 |
|---|---|---|---|
| `params.ConsoleHandle` | **양수 핸들 H0** (0, -1, -2, -3 이 아닌 값; VM 은 `4`). `\Reference` 객체 핸들이어야 맞다 | `ConsoleInitialize +0x170 ~ +0x189` (0·-1·-3·-2 아니면 `+0x19a ConsoleCreateConnectionObject`), condrv `CdCreateConnection`: `RelatedFileObject` 의 종류가 Reference 여야 서버 연결 (아니면 `0xC0000008`) | 0 → 길 B. 음수 → 다른 갈래 |
| `params.ConsoleFlags` / `WindowFlags & 0x600` | `0` / 0 | (가) 와 같음 | (가) 와 같음 |
| `params.StandardInput/Output/Error` | **ConDrv 클라이언트 객체 핸들 세 개** (`\Input`·`\Output`, 에러는 출력의 복사본) — 부모(엔진)가 미리 열어 둠 | 이 길은 표준 핸들을 `ConsoleCommitState` 가 안 채움 (상태 첫 비트 = 0, sast/80 (a) 쓰는 칸 목록). `\Input`/`\Output` = condrv 표 3·4 번 (`CdCreateDefaultObjectClient` 인자 1·2) | 0·-1 이면 CRT 가 -2 (질문 3) |
| `NtCreateFile` #1 (a1) | 이름 `\Connect`, `RootDirectory = H0`, 접근 `0x12019f`, `Attributes 0x40`, 공유 7, 방식 2 (`FILE_CREATE`), 옵션 `0x20`, `EaBuffer` = `"server"` + 연결 데이터 `0x53c`B (`EaLength 0x54b`). **답: `STATUS_SUCCESS` + 새 핸들 H1 (≠ 0)** | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleInitialize +0x19a/+0x19f` (sast/80 (a) 표 a1, EA 칸 표). condrv 쪽: 이름 `\Connect` → `CdCreateConnection` | 실패 → `ConsoleInitialize` AL=0 (`0xc0000022` 이면 로우박스 확인 후 길 B 로) → **프로세스 시작 실패** |
| `NtDeviceIoControlFile(H1, 0x500023, 출력 8B)` | 상태 `STATUS_SUCCESS`, **출력 8바이트에 서버 PID** (아무 수; 가짜 conhost PID). 출력 길이가 8 이 아니면 `0xC0000206` | `ConsoleCommitState +0x8a/+0x91`; condrv `CdpConnectionFastIoctl +0x26 CMP …,0x500023` 와 `param_5 == 8` | 실패해도 다음 단계로 가지만 값(`PID \| 1`)이 달라짐. H1 이 0 이면 이 호출을 건너뜀 (`+0x4e/+0x51`) |
| `NtSetInformationProcess(-1, 0x31, &(PID\|1), 8)` | `STATUS_SUCCESS` | `ConsoleCommitState +0xe9/+0xf0` | (가) 와 같음 |
| `NtDeviceIoControlFile(params.ConsoleHandle = H1, 0x500016, API 0x01000008 (GetLangId), 출력 2B)` | 상태 `STATUS_SUCCESS`, 출력 `WORD` = 언어 ID (VM 값 `[동적]`, 예 `0x0409`) | `ConsoleCommitState +0xfa/+0xff` → `SetTEBLangID +0x12/+0x17` → `GetConsoleLangId +0xe1/+0xe8` (sast/80 a4) | **실패해도 무시됨** (`RtlSetLastWin32ErrorAndNtStatusFromNtStatus` 만) — 그래서 이 답은 "선택" 이지만 성공을 주면 `SetThreadLocale(LangId)` 가 이어서 불려 TEB 로캘이 바뀜 |
| 표준 핸들의 `NtQueryVolumeInformationFile(FileFsDeviceInformation)` | 상태 `STATUS_SUCCESS`, **`DeviceType = 0x50 (FILE_DEVICE_CONSOLE)`**, `Characteristics = 0x20000` `[추측]` | `GetFileType +0x87 CMP EAX,0x50 / +0x8c MOV EAX,2`; condrv `DriverEntry +0x77 (R9D=0x50)`, `+0x120 (0x20000)` | 다른 값이면 `FILE_TYPE_CHAR` 가 안 되어 CRT 가 `FDEV` 를 안 켬 → `stdout` 이 파일처럼 완전 버퍼링 (종료 때만 출력), `_isatty` 거짓 |
| `NtWriteFile(표준 출력 핸들)` | `STATUS_SUCCESS`, `Information = Length`. 진짜 condrv 는 `0x103` 을 주고 서버(conhost) 가 나중에 `0x50000b` 로 완료 | condrv `CdpClientWrite` → `CdAddIoToPipe` → `0x103`; `CdCompleteIo` 가 서버 값으로 상태·`Information` 채움; CRT `write_text_ansi_nolock +0xd2 CMP EAX,EBX` | 위 (가) 와 같음 |
| 알 수 없는 IOCTL / Connection 객체에 쓰기 | `0xC0000010` / `0xC00000BB` | `CdpClientIoctl`·`CdpConnectionIoctl`·`CdpDispatchWrite` | 앱 쪽이 오류를 받음 |

**길 B: 새 콘솔 (`ConsoleHandle = 0` 그대로 — 지금 PE_emulator 값)** — sast/80 (b) 표 b1~b8 + commit. 표준 핸들은 `ConsoleCommitState` 가 `params.Standard*` 에 채워 준다 (b6~b8, `+0x10e ~ +0x144`). **`params` 를 바꾸지 않고 엔진의 syscall 답만으로 된다.**

| 순서 | syscall | 답 | 근거 |
|---|---|---|---|
| b1 | `NtCreateFile("\Device\ConDrv\Server", 접근 0x12019f, Attributes 0x42, 방식 2, 옵션 0)` | SUCCESS + 핸들 Hs. 실패하면 `NtSetSystemInformation(0x84, {1}, 4)` 하고 한 번 더 | `ConsoleAllocate +0x149/+0x150` (sast/80 b1·b1′) |
| b2 | `NtCreateFile("\Reference", root = Hs, 옵션 0x20)` | SUCCESS + Hr | `ConsoleAllocate +0x2b4/+0x2bb` |
| b3 | `NtDeviceIoControlFile(Hs, 0x500037, 입력 = RTL_USER_PROCESS_PARAMETERS)` | SUCCESS (실제로 conhost 를 안 띄워도 됨) | `ConsoleLaunchServerProcess +0xc6/+0xcd` |
| b4 | `NtClose(Hs)` | SUCCESS | `ConsoleAllocate +0x332/+0x339` |
| b5 | `NtCreateFile("\Connect", root = Hr, EA "server")` | SUCCESS + Hc | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleAllocate +0x357/+0x35c` |
| b6 | `NtCreateFile("\Input", root = Hc, Attributes 0x42(상속))` | SUCCESS + Hin | `ConsoleCreateStandardIoObjects +0xb1/+0xb8` |
| b7 | `NtCreateFile("\Output", …)` | SUCCESS + Hout | `+0x151/+0x158` |
| b8 | `NtDuplicateObject(-1, Hout, -1, …, 옵션 6)` | SUCCESS + Herr | `+0x1a8/+0x1af` |
| b9~b12 | a2·a3·a4 와 같음 (핸들 = Hc) | 위 길 A 표 | `ConsoleCommitState` |

- 길 B 의 전제: `KUSER_SHARED_DATA.SharedDataFlags` (`0x7ffe02f0`) 비트 6 이 **0** (VM 은 `0x10e`, sast/80). 켜져 있으면 중개(broker) 길 (`ConsoleAttachOrAllocate`) — 이 VM 이 안 타서 이번엔 안 읽음.
- 두 길 모두 **그 뒤 표준 핸들 3개가 CHAR 로 보이고 `NtWriteFile` 이 성공하면 `printf` 는 끝까지 간다.** (C 로캘 기본이면 위 IOCTL 외에 콘솔 IOCTL 이 하나도 더 필요 없음 — 질문 2.)

**앱이 콘솔 API 를 직접 부를 때 더 답해야 하는 것 (선택, sast/80 표 A·B 와 짝)**

| 호출 | `NtDeviceIoControlFile` 인자 | 줄 답 | 값 출처 |
|---|---|---|---|
| `GetConsoleMode` | 핸들 = `params.ConsoleHandle`, `0x500016`, API `0x01000001`, body 4B | SUCCESS, 출력 `DWORD` 모드 | 값은 서버가 정함 → **VM 값을 Q104 로 떠서 쓸 것** (입력 모드 예 `0x1f7`, 출력 모드 예 `0x7`/`0x3` — `[추측]`) |
| `GetConsoleOutputCP` | `0x500016`, API `0x01000000`, body 8B (`+4 = 1`) | SUCCESS, `+0` `DWORD` 코드 페이지 | VM 값 (예 437/949 `[추측]`) — 0 이 오면 `GetConsoleOutputCP` 가 오류 코드를 세우고 0 반환 (`+0xee ~ +0x10a`) |
| `WriteConsoleW` | 핸들 = **앱 출력 핸들 그 자체**, `0x500016`, API `0x01000006`, body 8B, 입력 버퍼 2 (메시지 + 글자 `nChars×2` 바이트) | SUCCESS, 출력 body `+0` = **쓴 바이트 수** (= `nChars×2`) → `*lpNumberOfCharsWritten = 바이트/2` | sast/80 `WriteConsoleInternal +0x14f ~ +0x161` |
| `GetConsoleScreenBufferInfo[Ex]` | `0x500016`, API `0x02000007`, body `0x5c`B | SUCCESS + 화면 정보 (sast/80 표 C) | VM 값 |
| CRT `_putwch_nolock` 용 `CONOUT$` 열기 | `NtCreateFile` 으로 `\CurrentOut` 류 `[추측]` (CRT 가 `CreateFileW(L"CONOUT$")`; kernelbase `CreateFileInternal +0x138` → ntdll `RtlDosPathNameToRelativeNtPathName_U_WithStatus` 가 이름을 바꿈. ntdll 쪽 `RtlpIsDosDeviceName_Ustr +0x2c7` 이 `RtlpDosCONOUTDevice` 를 읽는 것까지만 봄, 어떤 NT 이름·루트가 되는지는 `[공백]`) | SUCCESS + 핸들 (CHAR 취급이면 `WriteConsoleW` 가 `0x500016`/API 6 으로 감) | 유니코드 모드 `printf` 일 때만 |

- **지금 우리 (PE_emulator)**: 첫 실험 회차 7천 개 콘솔 프로그램을 돌리려면 **(나) 길 B 가 현재 값 (`ConsoleHandle = 0`) 에 가장 가깝다** — 엔진이 위 b1~b12 를 답하면 `params` 의 표준 핸들이 자동으로 채워져 `WriteFile`/`printf` 가 `NtWriteFile` 까지 간다. (가) 는 더 단순하지만 **`ConsoleHandle` 을 `-1` 로 바꿔야** 한다. `ConsoleHandle = 0` 인데 표준 핸들만 파일로 바꾸는 절충안은 **condrv 를 안 피한다** (`ConsoleAllocate` 가 실패하면 시작 자체가 실패).
- 현재 상태에서 `NtWriteFile(0)` 이 도달했다는 사실이 가리키는 가능성 (sast/80 [공백] 과 같음): (i) `ConsoleInitialize` 가 `ConsoleAllocate` 의 `NtCreateFile` 들을 이미 가짜 성공으로 지났고 `params.Standard*` 가 채워지지 않았거나 `WindowFlags & 0x400` 으로 가려짐, (ii) `ConsoleInitialize` 를 아직 안 지남. → "동적/엔진 확인" 1·2번.

## 동적으로 확인해야 할 것 (Q104 에 붙임)

1. **엔진 쪽**: 지금 PE_emulator 가 `kernelbase!ConsoleInitialize` 를 지났나, `ConsoleHandle`·`params+0xa4` (`WindowFlags`) 값이 무엇인가 (`GetStdHandle +0x1d`/`+0x49` 의 `0x400`/`0x200` 비트로 표준 핸들이 0 이 되는지). `ConsoleAllocate` 의 `NtCreateFile` 들에 무엇을 답하고 있나.
2. **VM 에서 파일 리다이렉트로 돌릴 때** (`test.exe > out.txt` 를 cmd 에서): `PEB→ProcessParameters` 의 `ConsoleHandle`·`StandardOutput`·`ConsoleFlags`·`WindowFlags` 값, `GetFileType` 이 파일 핸들에 돌려주는 `DeviceType` (기대 7). 같은 값을 (가) 표의 "값" 칸에 넣을 수 있는지 (그리고 부모 `cmd` 가 `ConsoleHandle` 로 무엇을 넘기나 — 진짜 핸들일 것).
3. **`-1` 의 출처**: `DETACHED_PROCESS` 로 띄운 자식의 `params.ConsoleHandle` 이 `-1` 인지 (또는 `CreateProcessInternalW` 정적 후속 — sast/80 후속 후보 ①).
4. **CRT 가 콘솔에 쓸 때 실제로 `WriteFile` 인지** (C 로캘): 콘솔 앱 (`printf` 만 쓰는 `/MD` 샘플 하나 필요) 에서 `ucrtbase!write_text_ansi_nolock+0xc7` (ret), `ucrtbase!write_requires_double_translation_nolock+0x89` (안 걸려야 함), `ucrtbase!initialize_stdio_handles_nolock+0x68` (ret 뒤 `rax` = 핸들) · `+0xcc` (ret 뒤 `eax` = `GetFileType`) . 현재 `sample/` 은 `/MT` + 직접 `WriteFile` 이라 CRT 경로를 안 탄다.
5. **로캘 칸 `+0x138`** 이 `setlocale(LC_ALL, "")` 뒤 0 이 아니게 되는지, 그러면 `GetConsoleMode` 가 실제로 불리는지 (`write_requires_double_translation_nolock +0x89`).
6. **서버가 돌려주는 값**: `GetConsoleMode` 모드, `GetConsoleOutputCP` 코드 페이지, `NtWriteFile` 의 `Information` (진짜 콘솔에서 `Length` 와 같은지), `GetLangId` WORD — sast/80 의 멈춤점 (`kernelbase!ConsoleCallServer+0xde`, `GetConsoleMode+0xe2`, `GetConsoleOutputCP+0xe5`, `WriteConsoleInternal+0x114`) 와 `nt!NtWriteFile` 반환.
7. **`stdout` 이 파일일 때 버퍼 크기·비우는 시점** (`ucrtbase!__acrt_stdio_flush_nolock+0x56` ret, 종료 때 `common_flush_all`) — `__acrt_stdio_allocate_buffer_nolock` 안 읽음.
8. **`CONOUT$` 의 NT 이름·루트** (ntdll `RtlpIsDosDeviceName_Ustr` 이후): 정적 후속 후보.
9. **msvcp140 `std::cout`** 의 쓰기 경로 (`fwrite` → `_fwrite_nolock_internal`?) — 정적 후속 후보 (`msvcp140.dll` 은 아직 analyze 안 됨).
10. 입력 쪽 (`_read_nolock`, `ReadConsoleW`, `std::cin`) 은 이번 범위 밖.

## 판정

**분석성공.** 질문 1~5 모두 `[정적]` 답과 `.asm` 줄 근거가 있다. 못 정한 것은 `[공백]`/`[추측]` 으로 위에 표시 (로캘 `+0x138` 칸 이름, 비 tty `stdout` 버퍼 크기, `-1` 이름, `CONOUT$` NT 이름, `"server"` EA 를 condrv 가 읽는 자리, `std::cout` 경로, `Information` 이 서버 몫이라는 것의 실제 값). 스크립트 문제 없음. ucrtbase·condrv 모두 새로 analyze (PDB 붙음, 오류 없음).

### 짝 결과서와 어긋나는 곳 / 요청서 가정 정정

- 요청서 질문 2 의 "`FDEV` + `GetConsoleMode` 성공이면 `WriteConsoleW`" → **실제는 (로캘 ≠ C 또는 UTF 모드) ∧ FDEV ∧ 텍스트 ∧ `GetConsoleMode` 성공 이면 "이중 번역"이고, ANSI 텍스트는 이중 번역도 `WriteFile`, `WriteConsoleW` 는 UTF-8/UTF-16 텍스트 모드의 `_putwch_nolock` (`CONOUT$`) 뿐.**
- 요청서 "알고 싶은 것 1" 의 `_NO_CONSOLE_FILENO (-2)` → **맞다**, `FILE._file` 과 `osfhnd` 둘 다 -2.
- sast/80 과 겹치는 부분(IOCTL `0x500016`/`0x500023`/`0x500037`, 열기 이름, `FileFsDeviceInformation` `0x50`)은 **서로 맞는다**. 어긋난 곳 없음.
- 정정 제안 없음 (run 문서는 건드리지 않았다).

### 산출물 위치

- 결과서: `/home/mjbin/prj/bob/teamprj/windows-pe-internals/_workspace/sast/81_console-crt_result.md`
- ucrtbase: `ms/decomp/ucrtbase/` (82개 + `_index.tsv`), `ms/disassemble/ucrtbase/` (`_calls.tsv`, `_map/__acrt_initialize_lowio_*`, `_map/__stdio_common_vfprintf_*`)
- condrv: `ms/decomp/condrv/` (31개), `ms/disassemble/condrv/` (`_calls.tsv`)
- PDB: `ms/sym/ucrtbase.dll.pdb`, `ms/sym/condrv.sys.pdb` (+ `.info.txt`, `.aliases.tsv`)
- Ghidra 프로젝트: `ms/ghidra/ucrtbase.*`, `ms/ghidra/condrv.*`

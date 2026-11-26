# 2026-11-26 (작성일, 정적 분석 2026-11-26) run147 — 화면 DC 하나가 서는 길 (Q278a, 정적만): `GdiplusStartup` 차례 5 `CreateICA("DISPLAY",0,0,0)` 는 gdi32 썽크 ⇒ gdi32full `CreateICA` → `bCreateDCA` (이름이 "DISPLAY" 면 **화면 표시 `EBX = 1`**, 문자열 안 만듦) → `hdcCreateDCW` 에서 **syscall `NtGdiOpenDCW` 하나**만 부르고, 그 반환은 **0 인가 · 종류 비트가 `0x10000` 인가** 둘만 본다. 프린터 길 (`bLoadSpooler` · UMPD · `pldcCreate` · `IcmInitLocalDC` · `DocumentEventEx`) 은 화면 표시 때문에 syscall 앞에서, 프린터 핸들 0 때문에 syscall 뒤에서 건너뛴다. 9 째 출력 포인터는 화면 길에서 안 읽는다. **IC 도 DC 와 똑같이 종류 비트 `0x10000`** (4 째 인자 IC `2` / DC `0` 은 커널만 본다). `DC_ATTR` 은 `HmgAllocateObjectAttr` (0x18 B 칸) 가 아니라 **`win32kbase!HmgAllocateDcAttr` 의 `0x170` B 슬롯** (유저에 앞 `0x160` B 복사, 유저가 읽는 끝은 `+0x128` qword). `+0x0` 비트 1 = "주 화면 장치 위 DC" (커널 `GreCreateDisplayDC +0x243` 이 켬) — 켜져도 **`GetDeviceCaps(0x58 / 0x5a)` 는 늘 `NtGdiGetDeviceCaps` syscall**. gdiplus 차례 5 ~ 7 은 **`NtGdiOpenDCW` ≠ 0 · `NtGdiCreateCompatibleDC` ≠ 0 만으로 `+0x5e3` 까지** 간다. 에뮬레이터 `GetDC` 값 `0x21000` 은 종류 비트가 `2` 라 `GetDeviceCaps` 가 표를 보기도 전에 `LastError 6` + 0 — run144 6절의 `0x57` 은 종류 비트 `0x10000` 일 때의 길이다

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("화면 DC 하나가 서는 길 — `CreateICA("DISPLAY")` → `NtGdiOpenDCW` 의 HDC · DC 칸 · `DC_ATTR` 최소 꼴 · `GetDeviceCaps(0x58 / 0x5a)`", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/367_internals-gdi-display-dc.md`, 튜닝 `366_tune18.md` GP1 · MB — `GdiplusStartup` = 1 받은 129 · `GetDeviceCaps(0x58/0x5a)` = 0 받은 362 · `CreateDCA` = 0 5 · 메시지 상자 12 · `GetWindowDC` = 0 288) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 user32 · gdi32 · gdiplus 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run142 · run144 · run146 과 같은 줄), 사진 PEB `GdiSharedHandleTable` (null) `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:1779`, `_P2.txt:2489`, run86 · run144 와 같은 줄). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (gdi32 · gdi32full · gdiplus · win32kbase · win32kfull 줄 · 근거 줄) · 3장 gdi32 하위 절 한 줄 · run144 하위 절 두 줄 · run146 하위 절 뒤 새 하위 절 · 4장 (run144 정적 N1 표시 · run144 PE_emulator 한 마디 · run146 동적 번호 · Q278a 닫힘 줄 · run147 줄 셋) · 5장 (run147 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q278a** (Q278 의 N1 만 앞으로 — 사용자 결정 2026-11-26 "이것도 부탁해", 정적만). 결과서 판정 **분석성공** — 질문 1 (a)(b)(c)(d) · 2 · 3 (a)(b)(c) 모두 `[정적]` 줄 근거로 닫음. win32k 몫은 sast/142 결과서가 생긴 뒤 열어 일부 읽었고, 못 읽은 것은 "win32k 몫 남음" 1 ~ 6.
> - 이어 쓰는 앞 run: **run144** (1절 차례 4 ~ 7 · 3절 표 칸 0x18 B · 핸들 비트 · 가리기 식 · 6절 `GetDeviceCaps` · 다음 요청 후보 N1), **run142** (메시지 상자 `NtUserGetDCEx(0,0,3)` 의 DC — 이번엔 "같은 꼴인가" 한 줄만), **run146** (대화상자 길의 GDI 부름 `CreateDlgFont` 는 여기서도 안 닫힘), **run86** (`pGdiDevCaps` = PEB `+0xf8` 값 `+ 0x180000`, `GdiProcessSetup +0x456`).
> - 앞 run 고쳐 읽기 하나: run144 맨 위 한 줄 (아래 "앞 run 고쳐 읽기").
> - 남은 동적 → **Q277** (같은 VM GUI 세션 — 결과서 동적 1 ~ 7 을 출처로 보탤 꼴), 남은 정적 → **Q278b** (결과서 "win32k 몫 남음" 1 ~ 6 + "다음 요청 후보").

## 요약

- **1 (a) `NtGdiOpenDCW` 인자 9 칸** `[정적]` (`hdcCreateDCW +0x8a ~ +0xc6`): `(1 이름 UNICODE_STRING*, 2 DEVMODE, 3 포트 이름, 4 종류, 5 화면 표시, 6, 7, 8, 9 출력 포인터)`. `CreateICA("DISPLAY",0,0,0)` = `(0, 0, 0, 2, 1, 0, 0, 0, &지역)`, `CreateDCA("DISPLAY",…)` 는 **4 째만 `0`**. IC 표시는 4 째 (`+0x8c ~ +0x97 NEG · SBB · AND R9D,2`), 에뮬레이터 로그의 5 째 `1` 은 "화면" 표시. 9 째 `[RBP-0x78]` 은 프린터 길 `+0x413` 에서만 읽힌다. 커널은 1 째가 NULL 이면 `NtGdiOpenDCW +0x39 JZ → +0x1c1 UserGetDesktopDC(종류, 0, 1)` 만 부르고 2 ~ 9 째를 안 본다. (칸 이름은 `[추측]`, 자리 · 값은 `[정적]`.)
- **1 (b) 프린터 길 건너뛰기** `[정적]`: syscall **앞** = `bCreateDCA +0x88 _stricmp(driver, "DISPLAY")` 또는 `\\.\DISPLAY` 11 글자 (`+0x59` · `+0x1a5`) 면 `EBX = 1` → `hdcCreateDCW +0x76 TEST R9D · +0x79 JZ` 가 안 뛰어 `+0x12d ~` 프린터 문을 통째로 건너뜀. syscall **뒤** = 종류가 `0x10000` 이 아니어도 `+0x31c CMP [RSP+0x58],0 · +0x322 JZ → +0xf1` (프린터 핸들 0) 로 바로 돌려줌.
- **1 (c) syscall 뒤 보는 것** `[정적]`: **반환 0 인가** (`+0xd8 · +0xdb JZ → +0x13a`) 와 **종류 `0x10000` 인가** (`+0xdf AND 0x7f0000 · +0xe5 CMP`) 뿐. `+0x5cc IcmInitLocalDC` · `+0x5ed HANDLE_TO_INDEX` · `+0x614 GdiGetEntry` · `+0x693` · `+0x7a8 NtGdiDeleteObjectApp` 은 화면 길에서 안 닿는다. **화면 길의 NULL 은 `NtGdiOpenDCW` = 0 하나뿐.** 꼬리 `GdiTrackHCreate` 는 HDC 를 그대로 돌려준다 (전역 개수 `LOCK XADD`, 1000 배수일 때만 `GetGuiResources`).
- **1 (d) HDC 꼴** `[정적]`: IC 도 종류 비트 `0x10000` — 뒤의 `GetDeviceCaps +0x5c AND 0x7f0000 · +0x66 JNZ` 가 `0x10000` 이 아니면 표를 보기 전에 `pldcGet` 길로 간다. 스톡 비트 (`0x800000`) 꺼짐, 고유 번호 (24 ~ 31) 는 아무거나 (0 이 쉬움). run144 "핸들 비트" 규칙 그대로 `H = 번호 | 0x10000 | 고유 << 24`.
- **2 표 칸** `[정적]`: run144 3절 **그대로** — 이번에 읽은 DC 쪽 함수 (`GetDeviceCaps` · `CreateCompatibleDC` · `CreateCompatibleBitmap` · `GetDIBits` · `pldcGet`) 가 모두 같은 검사 (`+0xe == 1` · `+0xc == H>>16` · `(+0x8 & ~1) == gW32PID` · `+0x10` ≠ 0 · 풀어도 ≠ 0). 실패 때 가는 곳만 함수마다 다르다.
- **2 `DC_ATTR`** `[정적]`: 잡는 쪽 = **`win32kbase!HmgAllocateDcAttr`** (rva `0x2ad8c`, `GreCreateDisplayDC +0x205`) — **슬롯 `0x170` B, 한 쪽에 11 개** (`+0x18c MOV EDX,0xb` · `+0x1a9 ADD RDI,0x170`). `HmgAllocateObjectAttr` 은 0x18 B 칸 170 개/쪽 (`+0xee MOV EDX,0xaa` · `+0x10b ADD RSI,0x18`) — 영역 쪽. 유저에 복사되는 건 앞 `0x160` B (`RestoreAttributes +0x10` 부터), `+0x160` qword 는 0 (`HmgAllocateDcAttr +0x99`). PDB 이름 `?RestoreAttributes@DC@@QEAAXPEAU_DC_ATTR@@@Z` 에 `_DC_ATTR` 형 이름이 있다 (`.asm` ALIASES 줄).
- **2 `+0x0` 비트 1** `[정적]`: "이 DC 가 주 화면 장치 위에 있다 → 지표 몇 개를 공유 블록 `pGdiDevCaps` 에서 읽어도 된다" (뜻은 `[추측]`, 켜는 줄은 `[정적]` `GreCreateDisplayDC +0x231 CMP RDI,[RCX+0x30] · +0x243 OR [RCX],2`). `NtGdiOpenDCW(NULL…)` · `NtUserGetDC*` (`CreateCacheDC +0x83`) · `NtGdiCreateCompatibleDC` (`GrepCreateCompatibleDC +0x126 / +0x256`) 가 모두 `GreCreateDisplayDC` 를 부른다. 유저 쪽에 이 비트를 쓰는 줄은 없다. **0 이어도 된다** (그러면 지표 전부 syscall). 비트 0 = DIB 섹션이 골라짐 (`CreateCompatibleBitmap +0x12e` · `SelectObjectImpl +0x591`) — 화면 DC 는 0.
- **2 `+0x98` · `+0xa0` · `+0xa8` · `+0x128`** `[정적]`: 유저 쪽 어디도 값을 **검사하지 않는다** (`SelectObjectImpl` 이 "이전 값" 으로 돌려주고 새 값을 씀). `+0x98` 처음 값 `0x12001f` (`DCMEMOBJ +0x1b8`). 붓 · 펜 · 글꼴 처음 값은 커널 세션 기본 틀 `[W32GetSessionState()+0x58]+0x90` (0x1b0 B) 을 `DCMEMOBJ +0x74 ~ +0xec` 가 복사한 것 — 틀 내용은 `[공백]` (Q278b). **0 아닌 값이 *필요한* 칸은 없다.**
- **3 (a) `GetDeviceCaps(0x58 / 0x5a)`** `[정적]`: 비트 1 이 꺼졌으면 모든 지표가 `+0x1cb NtGdiGetDeviceCaps` (`0x12bf`), 켜졌어도 `0x58` · `0x5a` 는 점프 표 case 1 (`+0x1c6`) 로 **syscall** (같은 길 `4 · 6 · 8 · 0xa · 0x28 · 0x2a · 0x2c`). 커널은 `GDIINFO` (`PDEV+0x848`) `+0x28` / `+0x2c` 에서 읽고, 화면 장치면 **DPI 가상화** — 스레드 DPI 문맥 (`+0xc8 [스레드+0x190]`) · 프로세스 (`+0xec [W32PROCESS+0x10c]`) · `+0x11a [W32PROCESS+0x110]` word 를 따라 **두 지표가 같은 값** (`+0x132 MOV EBX,EBP`). 유저 쪽은 값을 안 검사한다 (gdiplus 는 `≤ 0` 이면 96.0). 그래서 "무엇과 같아야 하나" = **`0x58` = `0x5a`, 0 아님** 만.
- **3 (b) 지표 표** `[정적]`: 블록 칸 `0x2` → `+0x4` · `0xc` → `+0x20` · `0xe` → `+0x24` · `0x26` → `+0x34` · `0x68` → `+0x4c` · `0x76` → `+0x6c` · `0x75` → `+0x70`. `0x8` · `0xa` 는 늘 syscall. `NtGdiGetDeviceCapsAll` (`+0x2f3`, `0x12c0`) 은 종류 ≠ `0x10000` 인 DC (프린터 등) 의 LDC 캐시가 빌 때만.
- **3 (c) gdiplus 차례 5 ~ 7** `[정적]`: `DesktopIc` 로 하는 GDI 일 = `GetDeviceCaps` ×2 → `GpDevice::GpDevice(hdc)` 안 **`CreateCompatibleDC(hdc)`** (`+0xaa`) · `GetDeviceCaps(2)` · (1 이면) `0xc` · 늘 `8` · `0xa` → `InitializeForGdiScreen` → `InitializeForGdiBitmap` → **`ExtractPixelFormatFromHDC(hdc)`** (`CreateCompatibleBitmap(hdc,1,1)` · `GetDIBits` ×1 ~ 2 · `DeleteObject`), `+0x60b` 에서 한 번 더. NULL 가지 열 곳 (`+0x341 ~ +0x5e3`) 가운데 **HDC · GDI 답에 달린 건 `+0x341` (GpDevice 무효) 하나**, 아홉은 할당뿐. `+0x341` 은 `CreateCompatibleDC` = 0 이거나, `TECHNOLOGY == 1` 인데 `BITSPIXEL ≤ 8` 이고 팔레트 칸이 0 일 때 (flow-writer 대조 3).
- **통과 최소** `[정적]`: `NtGdiOpenDCW` ≠ 0 · `NtGdiCreateCompatibleDC` ≠ 0 이면 표 칸 · `DC_ATTR` 이 없어도 `+0x5e3` 까지 — `GetDeviceCaps` 는 0 (+ `0x57` 또는 `6`) 일 뿐이고 (DPI 0 → 96.0, `TECHNOLOGY` 0 이면 `BITSPIXEL` 을 안 물음), `ExtractPixelFormatFromHDC` 는 `CreateCompatibleBitmap` 이 0 이면 포맷 0 으로 조용히 끝난다. 다만 "진짜 꼴" 로 채워 두는 게 N2 뒤에 안전하다 (아래 PE_emulator 쪽).
- **그 뒤 벽** `[정적]`: `InternalGdiplusStartup +0x62b InitImagingLibrary` (→ 0 이면 `+0x632`) — **N2 = Q278b**.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백다섯째 바퀴. plan **Q278a**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 없음.** gdi32 · gdi32full · gdiplus · win32kbase · win32kfull 모두 `-readOnly` export · refs 만 (결과서). win32k 둘은 sast/142 결과서 (`2026-11-26 18:29:57`) 가 생긴 뒤에 열었다 (export 18:32 ~ 18:36, 끝난 뒤 Ghidra 0개 — 결과서).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| gdi32 | `92C6EC2ABE478836C99BC83C2F3AE3551` | 같음 (run86 · run144) |
| gdi32full | `A6245E4F5C78EB06438EA4820F1F6EB41` | 같음 (run86 · run144) |
| gdiplus (`GdiPlus.dll`) | `DDF19AA09F285660C86065C38EF751A41` | 같음 (run144) |
| win32kbase | `D22DFDA3D54B87E6BD0567A57877904C1` | 같음 (run136 · run142 · run144 · run146) |
| win32kfull | `5CD57181BCFDC5AE8F4819BEB1196F091` | 같음 (run136 · run142 · run146) |

- 버전 대조 (결과서 + flow-writer): 요청서 · run144 에 적힌 줄 (`hdcCreateDCW +0xc9 NtGdiOpenDCW` · `+0x1cd bLoadSpooler` · `+0x243 LoadUserModePrinterDriver` · `+0x5cc IcmInitLocalDC` · `+0x5dd` · `+0x5ed` · `+0x614` · `+0x693` · `+0x7a8`, `bCreateDCA +0xd4`, gdi32full `CreateICA` rva `0x42c90`, `GetDeviceCaps +0x163 ~ +0x1cb` · `+0x1c0`, gdiplus `InternalGdiplusStartup +0x280 · +0x296`) 은 `.asm` 과 **일치**. syscall 번호 (`_syscalls.tsv`) — `NtGdiOpenDCW` `0x10c7` · `NtGdiGetDeviceCaps` `0x12bf` · `NtGdiGetDeviceCapsAll` `0x12c0` · `NtGdiCreateCompatibleDC` `0x104f` · `NtGdiCreateCompatibleBitmap` `0x1046` · `NtGdiGetDIBitsInternal` `0x107d` · `NtGdiDeleteObjectApp` `0x1020` · `NtUserGetDC` `0x1008` · `NtUserGetDCEx` `0x108a` · `NtUserGetWindowDC` `0x105e` — 이름 일치.
- PDB 에 `_DC_ATTR` 형 이름은 꾸민 함수 이름 안에만 있다 (`RestoreAttributes` ALIASES). 칸 정의는 안 봤다 — 칸 이름은 `[추측]`, 오프셋 · 줄은 `[정적]`.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): `GdiplusStartup` = 1 은 아직 run144 차례 4 (영역 표 칸) 에서 멈춤 → 고치면 바로 차례 5 `CreateICA("DISPLAY")`. 표 칸 짓기를 영역 · DC 한 번에 하려고 N1 을 먼저 받으려 함. 같은 DC 칸이 `GetDeviceCaps(0x58/0x5a)` = 0 (362 샘플, HDC = `GetDC(0)` stub `0x21000`) 도 풂. 정책 D156 = 보이지 않는 가짜 창, win32k GDI 를 옮기지 않음 → 알고 싶은 것은 **gdi32 · gdi32full · gdiplus 가 그 HDC 로 무엇을 읽고 검사하나**. 값 (DPI `0x60` · 1920 × 1080) 은 에뮬레이터가 정함. 요청서 `_workspace/sast/145_gdi-display-dc.md`:
  1. `CreateICA("DISPLAY",0,0,0)` 이 HDC 를 돌려줄 때까지 — (a) `NtGdiOpenDCW` 인자 9 칸 · IC / DC 차이 · 9 째를 유저가 읽나 (b) 프린터 길 건너뛰기 (c) syscall 뒤 보는 것 · NULL 가지 전부 (d) HDC 꼴.
  2. DC 칸과 `DC_ATTR` 최소 꼴 — 표 칸 · `DC_ATTR` 크기 (`HmgAllocateObjectAttr` 가 맞나) · 칸 표 · `+0x0` 비트 1 · 처음 값 · 다른 길 HDC.
  3. 그 HDC 로 하는 일 — (a) `GetDeviceCaps(0x58/0x5a)` 갈래 · 값 출처 (b) 지표 표 (c) gdiplus `+0x296 ~ +0x5e3`.
- 쓰는 곳 (원문): 갈래 BD `ext/syscall/syscall_layer_cpp/src/gui/nt_gdi_init2.cpp` · `nt_gdi_create_rect_rgn.cpp` · 새 `NtGdiOpenDCW` · 새 `NtGdiGetDeviceCaps` · `nt_user_get_dc.cpp` · `nt_gdi_create_compatible_dc.cpp` · 새 `NtUserGetDCEx` · `NtUserGetWindowDC`.

## 방법

- 정적 sast/145 (결과서 `_workspace/sast/145_gdi-display-dc_result.md`, **분석성공**, 스크립트 오류 없음 — 주의 하나 "같은 이름 파일 덮기" → 도구 메모):
  - gdi32 새로: `CreateCompatibleDC` · `IsCreateCompatibleDCPresent` · `CreateICW` · `CreateDCA` · `CreateCompatibleBitmap` · `GetDIBits`. 있던 `CreateICA` · `GetDeviceCaps` · `pldcGet` · `GdiGetEntry` · `HANDLE_TO_INDEX` 읽기. refs `0x27600` (`CreateICA` 지연 가져오기 칸).
  - gdi32full 새로: `CreateCompatibleDC` · `CreateCompatibleDCAdvanced` · `IcmInitLocalDC` · `CreateCompatibleBitmap` · `GetDIBits`. 있던 `CreateICA` · `bCreateDCA` · `hdcCreateDCW` · `GdiTrackHCreate` · `SelectObjectImpl` 읽기.
  - gdiplus 새로: `EpScanGdiDci` · `EpScanEngine` · `ConvertBrushToGdi` (둘) · `ConvertAlphaToGdi` · `ExtractPixelFormatFromHDC` (+ depth 1). 있던 `InternalGdiplusStartup` · `GpDevice` · `DriverGdi` · `DpBitmap` · `InitializeForGdiScreen` · `InitializeForGdiBitmap` 읽기. `GpDevice::IsValid` · `DpDriver` 빈 가상함수 · `GpSemaphore::Initialize` 는 `objdump` 로만 (`[objdump]`).
  - win32kbase 새로: `NtGdiOpenDCW` · `NtGdiGetDeviceCaps` · `hdcOpenDCW` · `GreGetDeviceCapsInternal` · `UserGetDesktopDC` · `GreCreateDisplayDC` · `HmgAllocateDcAttr` · `HmgFreeDcAttr` · `SetupDCAttributes` · `DCMEMOBJ` (둘) · `RestoreAttributes` (둘) · `CreateCacheDC` · `GreCreateCompatibleDC` · `GrepCreateCompatibleDC` · `NtUserGetDC` · `_GetDCEx`. win32kfull 새로: `NtUserGetDCEx` · `NtUserGetWindowDC`.
  - 문자열 · 데이터 초깃값 (gdi32full `.rdata` `DISPLAY` rva `0xc55d8` · `\\.\DISPLAY` `0xc5928`, `.data` 한도 값) 과 user32 `GetDC` 는 `objdump`.
- 같은 시간에 다른 decompiler 가 sast/144 (combase · rpcss) · sast/146 (dialog-end) 를 돌았다. flow-writer 는 이 바퀴 `.asm` 을 읽기만 했다.
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - gdi32full `CreateICA +0x0 ~ +0x18` (전부); `bCreateDCA +0x46 ~ +0xd9 · +0xe1 ~ +0x16d · +0x195 ~ +0x1dd`; **`hdcCreateDCW +0x2d ~ +0x16b`** (전부) · `+0x1cd · +0x1ec · +0x243 · +0x2f4 · +0x31c · +0x322 · +0x413 · +0x5cc · +0x5dd · +0x5ed · +0x614 · +0x677 · +0x693 · +0x7a5 · +0x7a8`; `GdiTrackHCreate +0x9 · +0xc · +0x14 · +0x39 · +0x5b · +0x65 · +0x6e · +0x7f · +0x9c · +0xf2`; `CreateCompatibleDC +0xd · +0xa7 · +0xb7 · +0xc2 · +0xd9 · +0x101 · +0x103 · +0x110 · +0x115 · +0x122`; `CreateCompatibleBitmap +0xa7 · +0xbb · +0xcd · +0xe6 · +0xf6 · +0x118 · +0x120 · +0x128 · +0x12e · +0x147 · +0x160 · +0x1e7 · +0x1fa`; `GetDIBits +0xb5 · +0xba · +0xdc · +0x13a · +0x13e · +0x140 · +0x146 · +0x19b · +0x215`; `IcmInitLocalDC +0x178 · +0x1cc`; `SelectObjectImpl +0x235 · +0x260 · +0x26a · +0x277 · +0x3a3 · +0x3aa · +0x3b3 ~ +0x3c5 · +0x557 ~ +0x5c1 · +0x60a ~ +0x658 · +0x758`.
  - gdi32 **`GetDeviceCaps +0x2b · +0x41 · +0x47 · +0x59 ~ +0x6c · +0xb3 · +0xbc · +0xd6 · +0x163 · +0x175 · +0x187 · +0x19b · +0x1ba · +0x1c0 ~ +0x221 · +0x224 · +0x231 · +0x239 · +0x243 · +0x249 · +0x25d ~ +0x28f · +0x2af · +0x2d9 · +0x2f3 · +0x301 ~ +0x32f · +0x336 · +0x34a · +0x35e · +0x419 · +0x420 · +0x42b · +0x432 · +0x43c`**, 점프 표 `gdi32.dll` rva `0x1c80` (dword 28 개) · `0x1cf0` (byte `0x7a` 개) 를 `objdump -s` 로 다시 풀어 case 줄에 맞춤 (아래 대조 4); `pldcGet +0x6d · +0x78 · +0x86 · +0x93 · +0xb1 · +0xb3`.
  - gdiplus `InternalGdiplusStartup +0x280 ~ +0x62b` 의 call 전부 (39곳) · `+0x28c · +0x293 · +0x296 · +0x2e9 ~ +0x2f7 · +0x30d · +0x33f · +0x341 · +0x36c · +0x38b · +0x3b7 · +0x3f3 · +0x42d · +0x467 · +0x4a1 · +0x4db · +0x518 · +0x544 · +0x548 · +0x5e3 · +0x632 · +0x6d5`; **`GpDevice`** call 전부 (12곳) · `+0x6a · +0xa0 · +0xb6 · +0xce ~ +0xdb · +0x172 ~ +0x1ae · +0x1c5 · +0x1d9 ~ +0x1fe`; **`ExtractPixelFormatFromHDC +0x3f · +0x41 · +0x4f ~ +0x6c · +0x8e · +0x9a · +0xa8 · +0xbd · +0xd4 · +0x11b · +0x120 ~ +0x13b · +0x185 ~ +0x18f`**; `InitializeForGdiBitmap +0x28 · +0x3a · +0x46 · +0x4d`; `InitializeForGdiScreen +0x1e`; `DriverGdi +0x68`; `ConvertBrushToGdi +0x1f`; `ConvertAlphaToGdi +0x1b`.
  - win32kbase **`NtGdiOpenDCW +0x1c ~ +0x3f · +0x13a · +0x171 · +0x1b8 ~ +0x1c9`**; **`UserGetDesktopDC +0x0 ~ +0x117 · +0x129 · +0x157 · +0x196 · +0x1d2 ~ +0x1e8`**; **`GreCreateDisplayDC`** call 전부 (30곳) · `+0x57 · +0x231 · +0x243 · +0x2f8 · +0x40d`; `HmgAllocateDcAttr +0x99 · +0x18c · +0x1a9`; `HmgAllocateObjectAttr +0xee · +0x10b`; `RestoreAttributes +0x10 · +0x9e`; `DCMEMOBJ +0x42 · +0x47 · +0x74 · +0xec · +0x1b8 · +0x25b`; `SetupDCAttributes +0x28 · +0x6b · +0x82`; `NtGdiGetDeviceCaps +0x37 · +0x43 · +0x8e · +0x93 · +0xf2`; `GreGetDeviceCapsInternal +0x14 · +0x1a · +0x7d · +0x92 · +0x95 · +0xc8 · +0xec · +0xf6 · +0xff · +0x101 · +0x11a · +0x126 · +0x128 · +0x132 · +0x14c · +0x1a9 · +0x1e4 · +0x248 · +0x254`; `CreateCacheDC +0x83`; `GrepCreateCompatibleDC +0x126 · +0x256 · +0x2a4`; `NtUserGetDC` call 전부 (`+0x83 _GetDC` 등).
  - win32kfull `NtUserGetDCEx` · `NtUserGetWindowDC` call 전부 (`+0x13a` · `+0x46 _GetDCEx`).
  - `_syscalls.tsv` 번호 10개 (위 환경), `ms/sym/*.pdb.info.txt` 다섯.
  - `../PE_emulator-BD/ext/syscall/syscall_layer_cpp/src/gui/` 의 `nt_user_get_dc.cpp` · `nt_gdi_create_compatible_dc.cpp` · `nt_gdi_init2.cpp` 를 **줄 번호만 훑음** (읽기만, 고치지 않음).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **HDC 는 "도화지 대여증" 이다.** gdiplus 는 시작할 때 화면용 대여증을 한 장 받아 (`CreateICA`) 그걸로 "화면이 몇 DPI 냐" 를 묻고, 그 대여증을 본떠 연습장 (`CreateCompatibleDC`) 을 한 장 더 만든다.
- 창구 (gdi32) 는 대여증을 받으면 run144 의 **출입증 대장** 을 펴서 "도화지 (DC) 줄이 맞나" 를 본다. 대장 줄 맨 끝 칸에는 **사물함 열쇠 (`DC_ATTR`)** 가 있고, 사물함 첫 칸에 "본점 화면용" 스티커 (비트 1) 가 붙어 있으면 몇몇 질문은 게시판 (`pGdiDevCaps`) 을 보고 바로 답한다. DPI 질문은 스티커가 있어도 늘 본점 (커널) 에 전화한다.
- 에뮬레이터가 준 `0x21000` 은 대여증 종류 칸이 "도화지" 가 아니라 다른 숫자라, 창구가 대장을 펴 보지도 않고 "그런 대여증 없음 (6)" 이라고 돌려보낸 꼴이다 `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{gdi32,gdi32full,gdiplus,win32kbase,win32kfull}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `gdiplus!InternalGdiplusStartup` (`0xe1380`) | **`+0x280`** | **`+0x287`** | **gdi32 `CreateICA("DISPLAY",0,0,0)`** → `+0x28c DesktopIc` · `+0x293 TEST` · **`+0x296 JZ → +0x7d1` (1)** | [정적] |
| 〃 | `+0x2a4` · `+0x2cb` | `+0x2ab` · `+0x2d2` | `GetDeviceCaps(DesktopIc, 0x58 / 0x5a)` → `DesktopDpiX/Y` (≤ 0 이면 `+0x2f7` 96.0) | [정적] |
| 〃 | `+0x326` · `+0x33a` | `+0x32b` · `+0x33f` | **`GpDevice::GpDevice(hdc)`** (`0x933e4`) · `CheckValid<GpDevice>` → **`+0x341 JZ`** | [정적] |
| 〃 | `+0x3d4` · `+0x40e` | `+0x3d9` · `+0x413` | `DriverGdi` ×2 (`0xbf840`) — 안 `ConvertBrushToGdi +0x1f GetStockObject(0)` | [정적] |
| 〃 | `+0x4bc` | `+0x4c1` | `DriverMeta` — 안 `ConvertAlphaToGdi +0x1b GetStockObject(4)` | [정적] |
| 〃 | `+0x56e` | `+0x573` | `DpBitmap::InitializeForGdiScreen` (`0x932dc`) `+0x1e` → `InitializeForGdiBitmap` (`0x93344`) | [정적] |
| 〃 | `+0x60b` | `+0x610` | `InitializeForGdiBitmap(g_pRemoteSurface, …)` — 같은 GDI 일 한 번 더 | [정적] |
| 〃 | `+0x62b` | `+0x630` | `InitImagingLibrary` → `+0x632 JZ` — **그 뒤 벽 (N2, Q278b)** | [정적] |
| `gdi32!CreateICA` (썽크 `0xace0`) | `+0x0` `JMP` | — | `[0x180027600]` 지연 가져오기 ⇒ gdi32full `CreateICA` | [정적] |
| `gdi32full!CreateICA` (`0x42c90`) | `+0xc` | `+0x11` | `bCreateDCA` (`0x42ce0`) — `+0x4 [RSP+0x20] = 1` (IC) · `+0x18 JMP GdiTrackHCreate` (꼬리) | [정적] |
| `gdi32full!bCreateDCA` | `+0x88` | `+0x8f` | `_stricmp(driver, "DISPLAY")` → 0 이면 `+0x9c EBX = 1` | [정적] |
| 〃 | **`+0xd4`** | **`+0xd9`** | **`hdcCreateDCW(0, 0, 0, EBX=1, 1, 0)`** (`0x42ef0`) — 반환 저장만 | [정적] |
| `gdi32full!hdcCreateDCW` | **`+0xc9`** | **`+0xd0`** | **`NtGdiOpenDCW`** (win32u `0x10c7`, 가져오기 칸 `0x1800c2068`) → `+0xd8 TEST · +0xdb JZ → +0x13a` · `+0xe5 CMP 0x10000` | [정적] |
| 〃 | `+0x1cd` · `+0x243` · `+0x2f4` · `+0x5cc` · `+0x7a8` | `+0x1d2` · `+0x248` · `+0x2f9` · `+0x5d1` · `+0x7af` | `bLoadSpooler` · `LoadUserModePrinterDriver` · `DocumentEventEx` · `IcmInitLocalDC` · `NtGdiDeleteObjectApp` — **화면 길에서 안 닿음** | [정적] |
| `gdi32!GetDeviceCaps` (`0x17f0`) | **`+0x1cb`** | **`+0x1d2`** | **`NtGdiGetDeviceCaps`** (`0x12bf`) — `+0x1c0 TEST [DC_ATTR],2` 꺼짐 또는 case 1 | [정적] |
| 〃 | `+0x26b` · `+0x2f3` | `+0x270` · `+0x2fa` | `pldcGet` · `NtGdiGetDeviceCapsAll` (`0x12c0`) — 종류 ≠ `0x10000` 일 때 | [정적] |
| `gdiplus!GpDevice` (`0x933e4`) | **`+0xaa`** | **`+0xb1`** | **gdi32 `CreateCompatibleDC(hdc)`** → `+0xb6 [this+0x18]` | [정적] |
| 〃 | `+0xc2` · `+0xea` · `+0x104` · `+0x1cd` | `+0xc9` · `+0xf1` · `+0x10b` · `+0x1d4` | `GetDeviceCaps(2 · 8 · 0xa · 0xc)` | [정적] |
| 〃 | `+0x1e5` | `+0x1ea` | `PaletteChangeNotification` — `BITSPIXEL ≤ 8` 일 때만 | [정적] |
| `gdi32full!CreateCompatibleDC` (`0x39470`) | `+0xd` · `+0xa7` | `+0x14` · `+0xae` | `NtGdiCreateCompatibleDC` (`0x104f`) · `GdiGetEntry` (원본 칸) → `+0x103 [DC_ATTR+0x108]` | [정적] |
| `gdiplus!InitializeForGdiBitmap` | `+0x4d` | `+0x52` | `ExtractPixelFormatFromHDC([GpDevice+0x5e0])` (`0x17d60`) | [정적] |
| `gdiplus!ExtractPixelFormatFromHDC` | `+0x4f` · `+0x8e` · `+0xd4` · `+0x123` | `+0x56` · `+0x95` · `+0xdb` · `+0x12a` | `CreateCompatibleBitmap(hdc,1,1)` · `GetDIBits` ×2 · `DeleteObject` | [정적] |
| `win32kbase!NtGdiOpenDCW` (`0x3a060`) | **`+0x1c1`** | **`+0x1c6`** | **`UserGetDesktopDC(종류 = R9D, 0, 1)`** — `+0x36 TEST RCX · +0x39 JZ` (1 째 NULL) | [정적] |
| 〃 | `+0x171` | `+0x176` | `hdcOpenDCW` — 1 째 ≠ NULL (`\\.\DISPLAYn` · 프린터) | [정적] |
| `win32kbase!UserGetDesktopDC` | `+0x7b` | `+0x80` | `GreCreateDisplayDC([[세션+0xe338]+0x30], 종류, …)` | [정적] |
| 〃 | `+0x106` | `+0x10b` | `_GetDCEx(…, 0, 0x80800003)` — 종류 0 · 다른 프로세스 갈래 (대조 6) | [정적] |
| `win32kbase!GreCreateDisplayDC` | `+0xd2` · `+0x205` · `+0x29d` · `+0x2fb` | `+0xd7` · `+0x20a` · `+0x2a2` · `+0x300` | `DCMEMOBJ` · **`HmgAllocateDcAttr`** · `SetupDCAttributes` · `HmgFreeDcAttr` (실패) | [정적] |
| `win32kbase!NtGdiGetDeviceCaps` (`0x2e700`) | `+0x37` · `+0x8e` | `+0x3c` · `+0x93` | `APIDCOBJ(hdc)` (못 잠그면 `+0xf2` 6) · `GreGetDeviceCapsInternal` (`0x31ecc`) | [정적] |
| `win32kbase!CreateCacheDC` · `GrepCreateCompatibleDC` | `+0x83` · `+0x126` / `+0x256` | `+0x88` · `+0x12b` / `+0x25b` | `GreCreateDisplayDC` (같은 규칙) | [정적] |
| `win32kfull!NtUserGetDCEx` · `NtUserGetWindowDC` | `+0x13a` · `+0x46` | `+0x141` · `+0x4d` | win32kbase `_GetDCEx` | [정적] |

### 1. `CreateICA("DISPLAY",0,0,0)` 가 HDC 를 돌려줄 때까지 (질문 1) [정적]

| 차례 | 부르는 것 / syscall | 조건 | 반환을 유저 쪽이 보나 | 0 이면 |
|---|---|---|---|---|
| 0 | gdi32 `CreateICA` 썽크 → 지연 가져오기 칸 `0x180027600` | 첫 부름에 풀림 — 풀기 syscall (`NtProtectVirtualMemory` ×2) 은 run86 · run144 꼴 `[추측: 이번에 칸 안 풂]` | — | 예외 (NULL 아님) |
| 1 | gdi32full `CreateICA` | 5 째 인자 `1` (IC) | 안 봄 | — |
| 2 | `bCreateDCA` 이름 판정 | ① device ≠ NULL 이고 `\\.\DISPLAY` 로 시작 → `EBX=1` + 문자열 ② 아니고 driver == "DISPLAY" (`_stricmp`) → **`EBX=1`, 문자열 안 만듦** ③ driver 가 `\\.\DISPLAY` 로 시작 → `EBX=1` ④ 그 밖 `EBX=0` (프린터). 포트 · DEVMODE NULL 이면 변환 건너뜀 (`+0xa6` · `+0xaf`) | `RtlCreateUnicodeStringFromAsciiz` 의 AL — "DISPLAY" + 나머지 NULL 이면 안 부름 | — |
| 3 | `hdcCreateDCW(0, 0, 0, 1, 1, 0)` | | 반환 `RAX` 저장 (`+0xd9`) 뒤 그대로 | 그대로 0 |
| 4 | `hdcCreateDCW +0x76 TEST R9D · +0x79 JZ → +0x12d` | 화면 표시 1 → 안 뜀 → 프린터 문 (`+0x12d ~ +0x31c`) 통째로 건너뜀 | — | — |
| 5 | **`NtGdiOpenDCW`** (`0x10c7`) | 인자 표 (2절) | **봄** (`+0xd8 · +0xdb`) | NULL (3절) |
| 6 | 종류 검사 `+0xdd ~ +0xeb` | `0x10000` 이면 `+0xf1`, 아니어도 `+0x31c` 프린터 핸들 0 → `+0xf1` | 종류만 | 그대로 돌려줌 |
| 7 | `+0xf1 ~ +0x106` | `[RSP+0x78]` (0, `+0x4a`) · `RBX` (0, `+0x56`) 둘 다 0 → `+0x106 MOV RAX,RSI` | — | — |
| 8 | `GdiTrackHCreate(hdc)` (`0x40360`) | 0 이면 0 (`+0xc`). 아니면 `g_GDIHandleCount` `LOCK XADD` (`+0x14`), 1000 배수면 `+0x6e ~ +0x7f GetGuiResources`. 한도 (`.data` `0x2710` · `0x1f40`) 위 · 추적기 켜짐이면 추적 쪽 | HDC 그대로 (`+0x5b`) | — |
| 9 | gdiplus `+0x28c ~ +0x296` | | **봄** | **1** |

- 이 구간의 syscall 은 **`NtGdiOpenDCW` 하나** (+ 첫 부름의 지연 가져오기 풀기 `[추측]`).
- 다른 길 한 줄 (CreateDCA 5 샘플 참고): 종류 `0` 은 `UserGetDesktopDC` 에서 UI 한도 검사 (`+0x129 CanPerformUIOperation` · `+0x157 ValidateHwndEx` · `+0x196 HasUILimit`) 를 거치고 안 되면 `+0x1e5` 에서 0. 종류 `2` (IC) 는 `+0x110 MOV EBX,2 · +0x115 CMP · +0x117 JZ` 로 이 검사를 건너뛴다 (`[정적: 조건만]`). 그러니 r18 `CreateDCA` = 0 5 샘플은 지금은 stub 기본 대답 0 때문이다.

### 2. `NtGdiOpenDCW` 인자 9 칸 (질문 1 a) [정적, 이름 `[추측]`]

| 칸 | `CreateICA("DISPLAY")` / `CreateDCA("DISPLAY")` | 어디서 (`hdcCreateDCW`) | 유저가 syscall 뒤 읽나 |
|---|---|---|---|
| 1 `RCX` 이름 | **NULL** (`\\.\DISPLAYn` 이면 그 이름) | `+0xc6 MOV RCX,R13` ← 1 째 인자 | 안 읽음 |
| 2 `RDX` DEVMODE | 0 | `+0xc3 MOV RDX,RDI` ← 3 째 인자 | 프린터 길만 |
| 3 `R8` 포트 이름 | 0 | `+0xbb` · `+0xc0` ← 2 째 인자 (`[RSP+0x68]`) | — |
| 4 `R9D` **종류** | **IC `2`** / DC `0` | `+0x8c ~ +0x97` (5 째 인자 → `NEG · SBB · AND 2`) | — |
| 5 `[RSP+0x20]` **화면 표시** | **`1`** | `+0x2d MOV R12D,R9D` · `+0xb6` | — |
| 6 `[RSP+0x28]` | 0 | `+0x3b ~ +0x41` · `+0xae ~ +0xb2` ← 6 째 인자 (`bCreateDCA +0xb8` 가 0) | — |
| 7 `[RSP+0x30]` | 0 (프린터 길은 프린터 핸들) | `+0x8a XOR EDX` · `+0xa9` | — |
| 8 `[RSP+0x38]` | 0 (프린터 길은 UMPD 쪽) | `+0x7f XOR ECX` · `+0xa4` | — |
| 9 `[RSP+0x40]` **출력 포인터** | `&[RBP-0x78]` (처음 0 — `+0x6f`) | `+0x9b` · `+0x9f` | **프린터 길 `+0x413` 한 곳뿐**, 화면 길은 안 읽음 |

- 에뮬레이터 r18 `CreateDCA` 로그 `(0,0,0,0,1,0,0,0,포인터)` 는 위 DC 줄과 같다 (요청 쪽 로그, `[관찰]` 아님). `CreateICA` 는 4 째만 `2`.
- 커널: 1 째가 NULL 이면 `NtGdiOpenDCW +0x39 JZ → +0x1b8 ~ +0x1c1` 이 `UserGetDesktopDC(R9D, 0, 1)` 만 부르고 9 째에 안 쓴다. 1 째가 NULL 이 아니면 `+0x13a` 에서 9 째를 꺼내 `+0x171 hdcOpenDCW` 로 넘긴다 (이번 요청 밖).

### 3. NULL 로 나가는 가지 (질문 1 c) [정적]

| 가지 | 줄 | 조건 | 화면 길에서 |
|---|---|---|---|
| `NtGdiOpenDCW` = 0 | `+0xdb JZ → +0x13a` → `+0x140 [RSP+0x58]` (0) · `+0x14e TEST RSI` (0) · `+0x157` · `+0x160 TEST R14` (0) → `+0x169 XOR ESI` · `+0x16b JMP → +0xf1` → 0 | 커널이 0 | **닿음 (유일)** |
| `+0x7a8 NtGdiDeleteObjectApp(RSI)` | `+0x14e TEST RSI · +0x151 JNZ → +0x7a5` | HDC 를 받은 **뒤** 실패 (`pldcCreate` NULL · DEVMODE · 할당 · `DocumentEventEx = −1` · UMPD 거짓) — 모두 `+0x31c ~` 블록 (종류 ≠ `0x10000` 이고 프린터 핸들 ≠ 0) 안 | 안 닿음 |
| `bCreateDCA` NULL | `+0x142` · `+0x185` · `+0x1dd` | 문자열 · DEVMODE 변환 실패 | 안 닿음 |
| 커널 쪽 | `UserGetDesktopDC +0x1e5` (UI 한도, 종류 0) · `GreCreateDisplayDC +0x2fb HmgFreeDcAttr` (실패 정리) · `+0x57 JZ → +0x40d XOR AL` (세션 화면 장치 없음) | win32k 몫 | 종류 2 는 UI 한도 안 탐 |

### 4. HDC 꼴 · 표 칸 · `DC_ATTR` 칸 (질문 1 d · 2) [정적]

- **HDC** = `번호 (< 0x10000) | 0x10000 | 고유 << 24`, 스톡 비트 꺼짐. IC · DC 같은 꼴 (유저 쪽은 구분 안 함).
- **표 칸** (run144 3절 그대로): `PEB[+0xf8] + (H & 0xffff)*0x18` — `+0x8` (`& ~1`) == `gW32PID` (0 불가) · `+0xc` word == `H >> 16` · `+0xe` byte == **1** · `+0x10` = 가린 `DC_ATTR` 포인터 (풀어도 ≠ 0). `+0x0` · `+0xf` 안 읽음.

| 함수 | 검사 줄 | 실패하면 |
|---|---|---|
| `GetDeviceCaps` (gdi32 `0x17f0`) | 앞에 **`+0x59 ~ +0x66` 종류 비트 `0x10000`** · 칸 `+0x163` · `+0x175` · `+0x187` · `+0x19b` · `+0x1ba` | 칸 실패: `+0xbc TEB+0x68 = 0x57` + 0, syscall 없음. 종류 비트 다름: `0x660000` 이면 `+0x318` (`TECHNOLOGY` 만 5), 그 밖 `+0x26b pldcGet` = 0 → **`+0x281 TEB+0x68 = 6`** + 0 |
| `CreateCompatibleDC` (gdi32full `0x39470`) — 원본 칸 | `+0xa7 ~ +0x101` → `+0x103 CMP [DC_ATTR+0x108],0` | **실패해도 새 DC 를 그대로 돌려줌** (`+0x115` → `+0x122` `GdiTrackHCreate`). `+0x108` ≠ 0 이면 `+0x110 IcmEnableForCompatibleDC` 만 더 |
| `CreateCompatibleBitmap` (`0x531b0`) | `+0xa7 ~ +0x118` | `+0x1fa` NULL, syscall 없음. 통과 + `cx`/`cy` 0 → `+0x1e7` 스톡 비트맵 칸 |
| `GetDIBits` (`0x37b10`) | `+0x19b ~ +0x215` | `+0xb5 · +0xba GdiSetLastError(0x57)` + 0 |
| `pldcGet` (gdi32 `0x1d70`) | `+0x6d` · `+0x78` · `+0x86` · `+0x93` · `+0xb1` | 0. 통과면 `+0xb3 [DC_ATTR+0x90]` (LDC) |

**`DC_ATTR`** — 슬롯 `0x170` B (`HmgAllocateDcAttr`), 유저에 복사되는 앞 `0x160` B, 유저 쪽이 읽는 끝 `+0x128` qword (→ `0x130` B). 이름 `[추측]`:

| 오프셋 | 뜻 `[추측]` | 읽는 줄 | 쓰는 줄 | 0 이어도 되나 / 필요한 값 |
|---|---|---|---|---|
| `+0x00` 비트 1 | 주 화면 장치 위 DC | `GetDeviceCaps +0x1c0` | 커널 `GreCreateDisplayDC +0x243` | **0 이어도 됨** (전부 syscall). 켜면 `pGdiDevCaps` 블록이 읽힘 (5절) |
| `+0x00` 비트 0 | DIB 섹션 골라짐 | `CreateCompatibleBitmap +0x12e` · `SelectObjectImpl +0x235` · `+0x591` | 커널 | **0** (켜지면 `+0x147 NtGdiGetDCObject` · `+0x160 GetObjectA` 길) |
| `+0x90` qword | LDC 포인터 | `pldcGet +0xb3` | 커널 · 프린터 길 | **0** |
| `+0x98` dword | 바뀜 표시 | `SelectObjectImpl +0x3b3` · `+0x758` | `SelectObjectImpl +0x260` · `+0x3bf` · `+0x569` · `+0x5ad` · `+0x647` | 처음 값 `0x12001f` (`DCMEMOBJ +0x1b8`), 유저는 안 걸림 |
| `+0xa0` qword | 붓 | `SelectObjectImpl +0x5b7` | `+0x5c1` | 값 검사 없음 — "이전 붓" 으로 돌려줌 (0 이면 앱이 실패로 읽을 수 있음 `[추측]`) |
| `+0xa8` qword | 펜 | `+0x573` · `+0x651` | `+0x57d` · `+0x658` | 같음 |
| `+0xf0` dword | ICM 표시 | `SelectObjectImpl +0x26a (& 3)` · `+0x557 (& 0x11)` · `+0x59f` · `+0x60a` · `GetDIBits +0x140` (둘째 부름부터) | 프린터 길 `IcmInitLocalDC +0x178` · `+0x1cc` | **0** |
| `+0xf8` qword | ICM 색 공간 | `SelectObjectImpl +0x61a` · `+0x63a` | — | 0 |
| `+0x108` qword | ICM 정보 | `CreateCompatibleDC +0x103` · `SelectObjectImpl +0x277` · `hdcCreateDCW +0x677` (프린터 길) | 커널 | **0** |
| `+0x114` dword | 글 쪽 (언어팩) | `GetTextExtentPointW +0x124` (`gbLpk ≠ 0` 일 때) | — | N3 몫 `[공백]` |
| `+0x128` qword | 글꼴 | `SelectObjectImpl +0x3a3` | `+0x3c5` | 값 검사 없음 (같으면 `NtGdiSelectFont` 안 부름 `+0x3aa`) |
| `+0x160` qword | — | — | `HmgAllocateDcAttr +0x99` 가 0 | 0 |

- **처음 값 출처**: `DCMEMOBJ` (`0x69a6c`) `+0x42 · +0x47 HmgAlloc(0x850, …)` 로 DC 객체 → **`[W32GetSessionState()+0x58]+0x90` 에서 `0x1b0` B 를 DC `+0x220` 으로 복사** (`+0x74 ~ +0xec`) · DC `+0x3d0` 에 그 위치. 그 뒤 `GreCreateDisplayDC +0x205 HmgAllocateDcAttr` → `+0x29d SetupDCAttributes` → `+0x28 RestoreAttributes` (앞 `0x160` B 를 유저 슬롯에) · `+0x6b ~ +0x82` 표 칸 `+0x10` 에 가린 포인터 (`gCookie` = `[Win32Process+0x118]`, 식은 run144 와 같음).
- `DCMEMOBJ +0x25b OR …,0x210000` — 종류 `0x11` 객체가 프린터 ALTDC (`0x210000`) 로 바뀌는 갈래, 화면 길 아님.
- **다른 길 HDC** (질문 2 끝): 유저 쪽 검사는 전부 같다 — gdi32 는 HDC 가 어디서 왔는지 모른다. `NtUserGetDC +0x83 _GetDC` · `NtUserGetDCEx +0x13a` / `NtUserGetWindowDC +0x46` → `_GetDCEx`, `CreateCacheDC +0x83` · `GrepCreateCompatibleDC +0x126 / +0x256` · `UserGetDesktopDC +0x7b` → `GreCreateDisplayDC` (`_GetDCEx` → `CreateCacheDC` 줄은 안 이음 `[공백]`). 메모리 DC 는 **원본 DC 의 `+0x108` 이 0 이어야** ICM 부름을 안 한다.

### 5. `GetDeviceCaps` 두 갈래 · 지표 표 (질문 3 a · b) [정적]

차례: `+0x2b InitOnceExecuteOnce` (Edge GDI, 보통 `g_pEdgeGdi == 0` → `+0x41` 안 뜀) · `+0x47 CMP g_systemCallFilterId,5` · `+0x59 ~ +0x66` 종류 · 칸 검사 · **`+0x1c0 TEST byte [DC_ATTR],2`**.

| 갈래 | 줄 | 하는 일 |
|---|---|---|
| 비트 1 꺼짐 | `+0x1c6 MOV EDX,EBX · +0x1cb NtGdiGetDeviceCaps` | 지표 그대로 syscall, 반환 그대로 |
| 비트 1 켜짐 | `+0x1e8 MOV RDI,[pGdiDevCaps]` → `+0x1ef CMP EBX,2` (`+0x1f4 [RDI+4]`) · `+0x208 CMP EBX,0xc` (`+0x20d [RDI+0x20]`) · `+0x221 CMP EBX,0x79 · +0x224 JA → +0xb3` · `+0x231 · +0x239 · +0x243` 점프 표 | 지표 → 블록 칸. 표 밖은 `0x57` + 0 |
| 종류 ≠ `0x10000` · ≠ `0x660000` | `+0x26b pldcGet` → `+0x2d9 TEST [LDC+8],0x2000000` → `+0x2f3 NtGdiGetDeviceCapsAll` · `+0x307 OR` | 프린터 DC 등 |
| `g_systemCallFilterId == 5` | `+0x419 CMP R9,-0x2321b000 · +0x420 JZ → +0x1e8` | 특별 HDC `0xFFFFFFFFDCDE5000` (user32 `GetDC` 같은 값 `[objdump]`) — 보통은 0 이라 안 탐 |

`pGdiDevCaps` = PEB `+0xf8` 값 `+ 0x180000` (run86). **지표 → 블록 칸** (SDK 이름 짝은 `[추측]`, 칸 · 줄은 `[정적]`):

| 지표 | 이름 `[추측]` | 블록 칸 / 상수 (case 줄) |
|---|---|---|
| `0x0` | DRIVERVERSION | `+0x0` (`+0x42b`) |
| `0x2` | **TECHNOLOGY** | `+0x4` (`+0x1f4`) |
| `0x4 · 0x6 · 0x8 · 0xa` | HORZSIZE · VERTSIZE · **HORZRES · VERTRES** | **늘 syscall** (`+0x1c6`) |
| `0xc` | **BITSPIXEL** | `+0x20` (`+0x20d`) |
| `0xe` | **PLANES** | `+0x24` (`+0x249`) |
| `0x10` | NUMBRUSHES | 상수 −1 (`+0x432`) |
| `0x12` · `0x16` · `0x18` | NUMPENS · NUMFONTS · NUMCOLORS | `+0x28` (`+0x43c`) · `+0x2c` · `+0x30` |
| `0x1c` · `0x1e` · `0x20` · `0x24` | CURVECAPS · LINECAPS · POLYGONALCAPS · CLIPCAPS | 상수 `0x1ff` · `0xfe` · `0xff` · `1` |
| `0x22` | TEXTCAPS | `+0x64` |
| `0x26` | **RASTERCAPS** | `+0x34` (`+0x2af`) |
| `0x28 · 0x2a · 0x2c` | ASPECTX · Y · XY | **늘 syscall** |
| `0x58` · `0x5a` | **LOGPIXELSX · Y** | **늘 syscall** |
| `0x68` | **SIZEPALETTE** | `+0x4c` (`+0x34a`) |
| `0x6a` · `0x6c` | NUMRESERVED · COLORRES | 상수 `0x14` · `+0x50` |
| `0x6e` · `0x6f` · `0x70` · `0x71` | PHYSICAL* | `+0x54` · `+0x58` · `+0x5c` · `+0x60` |
| `0x74` · `0x75` · `0x76` · `0x77` | VREFRESH · DESKTOPVERTRES · **DESKTOPHORZRES** · BLTALIGNMENT | `+0x68` · `+0x70` (`+0x35e`) · `+0x6c` (`+0x336`) · `+0x74` |
| `0x78` · `0x79` | SHADEBLENDCAPS · COLORMGMTCAPS | `+0x88` · `+0x8c` |
| 그 밖 (`0x14` · `0x1a` · 홀수 · `0x2e ~ 0x57` · …) | — | 블록 길에서 `0x57` + 0 (`+0xb3`) |

블록은 `+0x0 ~ +0x90`, 바로 뒤 `+0x94` 는 언어팩 칸 (run86), `+0xb0` 부터 스톡 배열 — 같은 쪽 안이다.

**`0x58` / `0x5a` 값 출처** (`NtGdiGetDeviceCaps` → `GreGetDeviceCapsInternal`):

- `+0x37 APIDCOBJ(hdc)` — 못 잠그면 `+0x43 JZ → +0xf2` `EngSetLastError(6)` + 0. 잠그면 장치 (PDEV) → `+0x8e GreGetDeviceCapsInternal`.
- `GDIINFO` = `[PDEV]+0x848` (`+0x14 · +0x1a`). `0x58` → `+0x28` (`+0x7d`), `0x5a` → `+0x2c` (`+0x14c`), `0x8` → `+0x10`, `0xa` → `+0x14`, `0xc` → `+0x18` (15 면 16, `+0x248 ~ +0x254`).
- **화면 장치** (`+0x92 · +0x95 TEST CL,1`) 이면 DPI 가상화: 스레드 DPI 문맥 (`+0xc8 [스레드+0x190]`) → 없으면 프로세스 (`+0xec [W32PROCESS+0x10c]`) → `(값 >> 8) & 0x1ff` (`+0xf6 ~ +0xff`) 가 0 이 아니면 그 값, 0 이면 `GDIINFO+0x2c` (`+0x101`) 이다가 프로세스 `+0x110` word 가 0 이 아니면 그 값 (`+0x11a ~ +0x126`). **`0x58` · `0x5a` 는 둘 다 이 한 값** (`+0x128 ~ +0x132`).
- 그래서 "96 고정" 이 아니라 **프로세스 · 스레드 DPI 설정을 따라간다.** `0x8` · `0xa` 는 DPI 가 100% 가 아닌 가상화면 `EngMulDiv` 로 줄이는 갈래 (`+0x1a9 ~ +0x1e4`) 가 있다 — 100% 면 `GDIINFO+0x10` · `+0x14` 그대로 `[추측: 그 갈래 조건 안 읽음]`.

### 6. gdiplus 차례 5 ~ 7 — `DesktopIc` 로 하는 일 (질문 3 c) [정적]

| 차례 (`+0x…`) | 부르는 것 | GDI 호출 · 읽는 칸 | NULL 가지 · 조건 |
|---|---|---|---|
| `+0x28c ~ +0x2f7` | `DesktopIc` · `GetDeviceCaps(0x58 · 0x5a)` | syscall ×2 (또는 칸 실패 시 0). `float` 로 `DesktopDpiX/Y` | **`+0x296`** (HDC 0 → **1**). DPI `≤ 0` 은 가지 아님 (`+0x2e9 COMISS · +0x2f0 · +0x2f5` → `+0x2f7` 96.0) |
| `+0x30d ~ +0x341` | `GpMallocEx(0x660)` → **`GpDevice(hdc)`** → `CheckValid<GpDevice>` | 안: `+0x6a [this+0x5e0] = hdc` · **`+0xaa CreateCompatibleDC(hdc)` → `+0xb6 [this+0x18]`** · `+0xc2 GetDeviceCaps(2)` · `+0xd3 CMP 1 · +0xd5 JZ → +0x1c5` (1 이면 `+0x1cd GetDeviceCaps(0xc)` · `+0x1d9 CMP 8 · +0x1dc JG`) · `+0xea GetDeviceCaps(8)` · `+0x104 (0xa)` · `HeapAlloc` ×2 · `EpScanGdiDci` ×2 | **`+0x341`** ← 표시값 `[this+8] == 0x76654431`. `+0x181 ~ +0x1a3`: `[+0x5d8] ≠ 0 · [+0x5d0] ≠ 0 · [+0x18] ≠ 0` 이면 `0x4c494146 + 0x2a1c02eb`. → **HDC 에 달림**: ① `CreateCompatibleDC` = 0 ② `TECHNOLOGY == 1` · `BITSPIXEL ≤ 8` · 팔레트 칸 `[+0x628]` 0 (`+0x1f7`) ③ 할당 실패 |
| `+0x34c ~ +0x36c` | `DeviceList` | 할당뿐 | `+0x36c` 할당뿐 |
| `+0x377 ~ +0x3b7` | `EngineDriver` (`+0x38b` 표시값 `0x72446431`) | 할당뿐 | `+0x3b7` 할당뿐 |
| `+0x3c4 ~ +0x42d` | `DriverGdi` ×2 | `GetStockObject(0)` (반환 저장만, 표시값은 `DriverGdi +0x68` 에서 무조건) | `+0x3f3` · `+0x42d` 할당뿐 |
| `+0x438 ~ +0x4db` | `DriverD3D` · `DriverInfo` · `DriverMeta` | `DriverMeta` 안 `GetStockObject(0)` · `(4)` | `+0x467` · `+0x4a1` · `+0x4db` 할당뿐 |
| `+0x4ea ~ +0x518` | `DpBitmap(NULL)` | GDI 안 부름 | `+0x518` 할당뿐 |
| `+0x523 ~ +0x563` | `GetSystemMetrics` ×4 (user32) | HDC 와 무관 | 가지 아님 |
| `+0x56e` | `InitializeForGdiScreen` → `InitializeForGdiBitmap +0x4d` | **`ExtractPixelFormatFromHDC([GpDevice+0x5e0])`** | 가지 아님 |
| `+0x588` | `DesktopDriver->vtbl[+0x10]` (CFG 간접) | `DpDriver` 빈 가상함수 (`ret 0`) `[objdump]` | — |
| `+0x5b5 ~ +0x5e3` | `g_pRemoteSurface = DpBitmap(NULL)` | GDI 안 부름 | `+0x5e3` 할당뿐 |
| `+0x60b` | `InitializeForGdiBitmap` 한 번 더 | `ExtractPixelFormatFromHDC` 한 번 더 | 가지 아님 |

**`ExtractPixelFormatFromHDC(hdc)`** (`0x17d60`): `+0x3f XOR EBX` (포맷 0) · `+0x41 memset` → **`+0x4f CreateCompatibleBitmap(hdc,1,1)`** → 0 이면 **`+0x61 JZ → +0x12f`** (포맷 0 을 조용히 돌려줌 — 대조 1) → `+0x8e GetDIBits` (머리만, `+0x9a biBitCount` · `+0xa8 biCompression == 3`) → (`> 8` 이고 `BI_BITFIELDS` 면) `+0xd4 GetDIBits` 한 번 더 (색 마스크 — 이때 `GetDIBits +0x140` 이 `[DC_ATTR+0xf0]` 를 읽음) → `+0x11b` 등 포맷 상수 (`0x22009` …) → `+0x123 DeleteObject`. 포맷은 `DpBitmap` 칸에 저장될 뿐 startup 의 NULL 가지에 안 쓰인다. syscall 은 표 칸이 맞을 때만 나감 (`NtGdiCreateCompatibleBitmap` `0x1046` · `NtGdiGetDIBitsInternal` `0x107d` · `NtGdiDeleteObjectApp` `0x1020`).

### 호출 관계

```
gdiplus!InternalGdiplusStartup (0xe1380)                                                  [정적]
 ├─ (5) +0x287  gdi32!CreateICA (JMP 지연) ⇒ gdi32full!CreateICA (0x42c90)     NULL → +0x296 → 1
 │               ├─ +0x11  bCreateDCA           +0x8f _stricmp "DISPLAY" → EBX = 1 (문자열 안 만듦)
 │               │          └─ +0xd9  hdcCreateDCW(0,0,0,1,1,0)
 │               │                     +0x79 화면 표시 → 프린터 문 (+0x12d ~ +0x31c) 건너뜀
 │               │                     +0xd0 NtGdiOpenDCW(0,0,0, 2(IC)/0(DC), 1,0,0,0,&지역)  ── SYSCALL 0x10c7
 │               │                            ⇒ win32kbase +0x39 1 째 NULL → +0x1c6 UserGetDesktopDC
 │               │                                 └─ +0x80 GreCreateDisplayDC  +0xd7 DCMEMOBJ (세션 틀 0x1b0 B 복사)
 │               │                                     +0x20a HmgAllocateDcAttr (0x170 B) · +0x243 [DC_ATTR] |= 2 (주 화면)
 │               │                                     +0x2a2 SetupDCAttributes → RestoreAttributes (0x160 B) · 표 칸 +0x10
 │               │                     +0xdb 0 → NULL · +0xe5 종류 0x10000 · +0x322 프린터 핸들 0 → +0x106 반환
 │               └─ +0x18  GdiTrackHCreate (꼬리)  개수 +1, HDC 그대로
 ├─ (6) +0x2ab · +0x2d2  gdi32!GetDeviceCaps(0x58 · 0x5a)
 │               +0x66 종류 ≠ 0x10000 → pldcGet → 0 · TEB+0x68 = 6
 │               +0x163 ~ +0x1ba 표 칸 실패 → 0 · 0x57
 │               +0x1c0 [DC_ATTR] 비트 1 꺼짐 / case 1 → +0x1d2 NtGdiGetDeviceCaps  ── SYSCALL 0x12bf
 │                      ⇒ +0x93 GreGetDeviceCapsInternal  GDIINFO+0x28/+0x2c · DPI 문맥 (둘이 같은 값)
 ├─ (7) +0x32b  GpDevice(hdc)   +0xb1 CreateCompatibleDC(hdc) ⇒ NtGdiCreateCompatibleDC  ── SYSCALL 0x104f
 │               +0xc9 GetDeviceCaps(2) · (1 이면 +0x1d4 (0xc)) · +0xf1 (8) · +0x10b (0xa)
 │      +0x33f  CheckValid<GpDevice>  → +0x341 (CreateCompatibleDC 0 · TECHNOLOGY 1 + BITSPIXEL ≤ 8)
 ├─     +0x3d9 ~ +0x5e1  드라이버 여섯 · DpBitmap ×2 · GetSystemMetrics     (NULL 가지 아홉은 할당뿐)
 │      +0x573  InitializeForGdiScreen → InitializeForGdiBitmap +0x52 ExtractPixelFormatFromHDC
 │               +0x56 CreateCompatibleBitmap(hdc,1,1) (0 → +0x12f 포맷 0) · +0x95/+0xdb GetDIBits · +0x12a DeleteObject
 ├─     +0x610  InitializeForGdiBitmap (한 번 더)
 └─ (8) +0x630  InitImagingLibrary   → +0x632 → 1                    ← 그 뒤 벽 (N2, Q278b)
```

- (오프셋은 ret 자리, 검사 줄은 함수 안 줄. `⇒` 는 지연 가져오기 · syscall 너머. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (결과서 "지금 우리 맞나 · 틀리나")

결과서는 `../PE_emulator-BD/ext/syscall/syscall_layer_cpp/src/gui/` 의 머리 주석 · 반환 줄만 읽었고, flow-writer 는 `nt_user_get_dc.cpp` · `nt_gdi_create_compatible_dc.cpp` · `nt_gdi_init2.cpp` 를 줄 번호만 훑었다 (읽기만). 고칠지는 사용자 · PE_emulator 결정.

| 쓰는 곳 (원문 "답이 오면 쓰는 곳") | 판정 | 이유 |
|---|---|---|
| `nt_gdi_init2.cpp` (표 칸 · DevCaps 블록) | **모자람 (run144 와 같음), DC 몫 덧붙임** | 블록 (쪽 올림 `0x181000`) 은 `pGdiDevCaps` (`+0x180000 ~ +0x180090`) 를 품는다. `DC_ATTR+0x0` 비트 1 을 켤 거면 5절 칸을 채움, **안 켜면 블록 0 그대로 안전**. 표에 DC 칸 한 줄 (아래 9) 이 더 필요 |
| `nt_gdi_create_rect_rgn.cpp` (run144 꼴) | **run144 판정 그대로, DC 일과 무관** | 0x18 B 칸 (`HmgAllocateObjectAttr` 꼴) 은 영역뿐 — DC 의 `DC_ATTR` 은 `0x170` B 로 따로 |
| 새 `NtGdiOpenDCW` | **만들 것** | 1 째 NULL · 4 째 `0` / `2` → 화면 HDC (종류 `0x10000`). **0 아니면 됨**, 9 째에 안 써도 됨. 1 째 ≠ NULL (`\\.\DISPLAYn` · 프린터) 은 이번 요청 밖 |
| 새 `NtGdiGetDeviceCaps` | **만들 것** | 지표 값 (아래 13). 기본 대답 0 이어도 `GdiplusStartup` 은 통과 (DPI 0 → 96.0) — 다만 `TECHNOLOGY` · `BITSPIXEL` · 해상도는 N2 뒤 GDI+ 가 읽을 수 있음 |
| `nt_user_get_dc.cpp` (`:20` `0x21000`) | **틀림 (종류 비트)** | `0x21000 & 0x7f0000 = 0x20000` (종류 2) → `GetDeviceCaps +0x66` 에서 표 앞에서 갈라져 `pldcGet` 0 → **`LastError 6`** + 0 (362 샘플의 길). 같은 HDC 로 `SelectObject` · `GetTextExtentPointW` 도 실패. **`번호 \| 0x10000`** + 표 칸 + `DC_ATTR` 필요 |
| `nt_gdi_create_compatible_dc.cpp` (`:21` `0x20c00`) | **gdiplus 에는 지금도 통과, 꼴은 틀림** | `GpDevice +0xb6` 은 0 아님만 봄, `CreateCompatibleDC` 는 원본 칸 검사가 실패해도 그대로 돌려줌 (`+0x115`). 종류 비트 `2` 라 그 DC 로 하는 다음 일에서 같은 실패 → 같은 꼴로 |
| 새 `NtUserGetDCEx` · `NtUserGetWindowDC` | **만들 것 — `NtUserGetDC` 와 같은 꼴** | user32 `GetDC` 는 `NtUserGetDC` 로 바로 점프 (필터 값 5 가 아니면), `GetDCEx` · `GetWindowDC` 는 win32u 로 점프하는 썽크뿐 `[objdump]`. 반환 0 이면 그대로 0 (메시지 상자 12 · `GetWindowDC` 288) |

- **flow-writer 보탬 (줄 번호 훑기)**: `nt_user_get_dc.cpp:7` · `nt_gdi_create_compatible_dc.cpp:8` 의 "`0x400` 배수라 핸들 표가 안 주는 번호" 근거는 nt 객체 핸들 표 (`ExpInsertLowLevelTableIntoFreeList`) 규칙이다 — run144 가 `nt_gdi_create_rect_rgn.cpp:7` 에 대해 적은 것과 같은 말로, GDI 핸들은 "핸들 비트" 규칙 (종류 = 16 ~ 22 비트) 을 따라야 한다. `0x400` 배수를 유지하고 싶으면 번호 칸 (0 ~ 15 비트) 만 `0x400` 배수로 두면 된다 (`0x00010c00` 처럼) `[정적: 비트 규칙과 비교]`.

**stub 이 채울 최소 목록** (run144 목록 1 ~ 7 — PEB `+0xf8` 블록 · 스톡 배열 · `0x12` · `0x13` · 영역 표 칸 · PEB `+0x140` 0 — 은 그대로. 이어서):

8. **DC 핸들 `H_dc`** — 종류 `0x10000` · 스톡 비트 0 · 번호 `idx < 0x10000` · 고유 번호 0 (24 ~ 31 비트 0 이면 `HANDLE_TO_INDEX` 가 syscall 을 안 부름). 예 `0x00010c00` → 칸 번호 `0xc00`.
9. **표 칸 (DC)** `PEB[+0xf8] + idx*0x18`: `+0x08` = `PID & ~3` (`gW32PID`) · `+0x0c` = `H >> 16` (= `0x0001`) · `+0x0e` = **1** · `+0x10` = `ROR64(블록주소 ^ gCookie, gCookie & 0x3f)` (하위 6 비트 0 이면 `블록주소 ^ gCookie`). `+0x00` · `+0x0f` 는 0.
10. **`DC_ATTR` 블록** — 쓰기 가능 `0x170` B (읽는 쪽은 `0x130` B 까지), 0 으로 채우고: `+0x00` = `0x02` (비트 1 켜려면 — 안 켜려면 0) · `+0x98` = `0x12001f` · `+0xa0` · `+0xa8` · `+0x128` = 스톡 붓 · 펜 · 글꼴 핸들 (0 이어도 통과하지만 `SelectObject` 가 "이전 값" 0 을 돌려줌 `[추측]`). **`+0x90` · `+0xf0` · `+0xf8` · `+0x108` 은 반드시 0.** 어느 스톡 핸들이어야 하는지는 `[공백]` (세션 기본 틀 — Q278b).
11. **`NtGdiOpenDCW`** (`0x10c7`) → 8 ~ 10 의 HDC (**0 아님 필수**). IC (`2`) · DC (`0`) 같은 꼴. 9 째는 안 씀.
12. **`NtGdiCreateCompatibleDC(hdc)`** (`0x104f`) → **0 아님 필수** (0 이면 `GpDevice [+0x18]` 0 → `+0x341` → **1**). 가능하면 8 ~ 10 과 같은 DC 꼴. 원본 `DC_ATTR+0x108` 은 0.
13. **`NtGdiGetDeviceCaps(hdc, 지표)`** (`0x12bf`) — 최소: `2 → 1` · `0xc → 32` · `8 → 1920` · `0xa → 1080` · `0xe → 1` · `0x58` = `0x5a` → `0x60` · `0x68 → 0` · `0x26` (RASTERCAPS) · `0x76 → 1920` · 그 밖 0. **`2 → 1` 이면 `0xc` 는 반드시 `> 8`** (아니면 `+0x341` 쪽으로 갈 수 있음). 둘 다 0 은 안전 (`TECHNOLOGY` 0 이면 `BITSPIXEL` 을 안 물음). HDC 를 못 알아보면 진짜 커널도 6 + 0.
14. **(비트 1 을 켰다면) `pGdiDevCaps` 블록** `PEB[+0xf8] + 0x180000`: `+0x4 = 1` · `+0x20 = 32` · `+0x24 = 1` · `+0x34` · `+0x4c = 0` · `+0x6c` · `+0x70` (1920 · 1080) 등 5절 표. **채우지 않으려면 비트 1 을 0 으로.**
15. **안 만들어도 되는 것**: `NtGdiCreateCompatibleBitmap` · `NtGdiGetDIBitsInternal` · `NtGdiDeleteObjectApp` — 기본 대답 0 이어도 `ExtractPixelFormatFromHDC` 는 포맷 0 으로 조용히 통과. 비트맵 핸들을 줄 거면 종류 `0x50000` · 칸 `+0xe == 5` + `GetDIBits` 의 `biBitCount` · `biCompression` · 마스크를 채워야 함.
16. **조용한 부수 효과**: `g_systemCallFilterId` — `Gdi32DllInitialize` 의 `GetProcessMitigationPolicy` 가 실패하거나 0 이면 0. **`5` 를 주면 `GetDeviceCaps` 가 다른 길** (`+0x419`).

**그 뒤 벽 한 줄**: `InternalGdiplusStartup +0x62b InitImagingLibrary` (0 이면 `+0x632` → 1) — **N2 (Q278b)**, DC 일 아님. 이어서 `+0x6d5 SysColorNotify` · `+0x73d InitSystemFontsDirs` · `+0x775 GetGpInstalledFontCollection` · `+0x7b3 BackgroundThreadStartup`.

## 확인된 것

- `[정적]` 화면 길 `CreateICA("DISPLAY",0,0,0)` 는 syscall `NtGdiOpenDCW` 하나만 부르고, 반환이 0 인가 · 종류 비트가 `0x10000` 인가만 본다. 프린터 길은 syscall 앞 (`EBX = 1`) · 뒤 (프린터 핸들 0) 에서 건너뛴다. 화면 길의 NULL 은 `NtGdiOpenDCW` = 0 하나뿐.
- `[정적]` `NtGdiOpenDCW` 4 째 = IC `2` / DC `0`, 5 째 = 화면 표시 `1`, 9 째는 프린터 길만 읽는다. 커널은 1 째 NULL 이면 `UserGetDesktopDC` 로 곧장 가고 2 ~ 9 째를 안 본다.
- `[정적]` IC 도 DC 도 종류 비트 `0x10000`. `GetDeviceCaps` 는 종류가 다르면 표를 보기 전에 `pldcGet` 길 → `LastError 6` + 0.
- `[정적]` `DC_ATTR` = `HmgAllocateDcAttr` 의 `0x170` B 슬롯 (11 개/쪽). 유저 쪽이 읽는 끝 `+0x128`. 0 아닌 값이 필요한 칸은 없다.
- `[정적]` `DC_ATTR+0x0` 비트 1 은 커널 `GreCreateDisplayDC +0x243` 이 장치 == 주 화면 장치일 때 켠다. 0 이어도 되고, 켜져도 `0x58` · `0x5a` · `4 · 6 · 8 · 0xa · 0x28 · 0x2a · 0x2c` 는 syscall.
- `[정적]` 커널의 `0x58` · `0x5a` 는 DPI 문맥 (스레드 → 프로세스 → `GDIINFO+0x2c`) 을 따라가고 두 지표가 같은 값이다.
- `[정적]` gdiplus 차례 5 ~ 7 에서 HDC · GDI 답에 달린 NULL 가지는 `+0x296` 과 `+0x341` 둘. `NtGdiOpenDCW` · `NtGdiCreateCompatibleDC` 가 0 이 아니면 `+0x5e3` 까지 간다.
- `[관찰]` test.exe 초기화 끝 모듈 6개에 user32 · gdi32 · gdiplus 가 없고 PEB `GdiSharedHandleTable` 은 (null) (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, `_P1a.txt:1779` · `_P2.txt:2489`) — 이 길은 test.exe 에선 안 생긴다.

## flow-writer 대조

결과서 오프셋을 `.asm` 과 맞춰 봤다. **어긋난 곳 셋 (1 ~ 3)** (고친 값을 이 문서에 씀) + 보탬 넷 (4 ~ 7):

1. **`ExtractPixelFormatFromHDC +0x61 JZ → +0x18f`** (결과서 (4) 표) → 목적지는 **`+0x12f MOV EAX,EBX`** (`0x180017e8f`). `EBX` 는 `+0x3f XOR EBX,EBX` 로 0 이라 "포맷 0 을 조용히 돌려줌" 은 맞다. `+0x18f` 는 다른 조각 (`MOV ECX,EAX`).
2. **`GreCreateDisplayDC +0x2f8 → HmgFreeDcAttr`** (결과서 (1c)) → `+0x2f8` 은 `MOV RCX,R14` (인자), **call 은 `+0x2fb` (ret `+0x300`)**. 같은 자리.
3. **`+0x341` 의 조건 ②** (결과서 한 줄 답 12 · (4) 표 "`TECHNOLOGY == 1` 인데 `BITSPIXEL ≤ 8` 이면") → 하나 더 있다: `GpDevice +0x1d9 CMP EAX,8 · +0x1dc JG → +0xdb` (8 보다 크면 정상), 8 이하면 `+0x1e5 PaletteChangeNotification` → **`+0x1ea CMP [this+0x628],0 · +0x1f1 JNZ → +0xdb`** — 그 칸이 여전히 0 일 때만 `+0x1f7` 무효 표시. `[this+0x628]` 은 `+0xa0` 에서 0 으로 시작하고, `PaletteChangeNotification` 이 채우는지는 안 읽었다 (`[공백]`, 팔레트 칸 `[추측]`). 에뮬레이터 규칙 "`TECHNOLOGY` 1 이면 `BITSPIXEL` > 8" 은 그대로 안전.
4. **보탬 — 점프 표 case 줄**: `gdi32.dll` rva `0x1cf0` (byte) · `0x1c80` (dword) 을 `objdump -s` 로 다시 풀어 case 목적지 줄을 맞췄다 — `0xe` → `+0x249 [RDI+0x24]` · `0x26` → `+0x2af [+0x34]` · `0x68` → `+0x34a [+0x4c]` · `0x76` → `+0x336 [+0x6c]` · `0x75` → `+0x35e [+0x70]` · `0x0` → `+0x42b [RDI]` · `0x12` → `+0x43c [+0x28]` · `0x10` → `+0x432` (−1). `4 · 6 · 8 · 0xa · 0x28 · 0x2a · 0x2c · 0x58 · 0x5a` → case 1 = `+0x1c6`. 지표 `0x14` · `0x1a` 는 case 5 = `+0xb3` (case `0x1b` 와 같은 `0x57` 줄). 결과서 표와 **일치**.
5. **보탬 — `hdcCreateDCW` 성공 끝**: `+0xf1 CMP [RSP+0x78],0 · +0xf7 JNZ` · `+0xfd TEST RBX · +0x100 JNZ` 는 화면 길에서 둘 다 0 (`+0x4a AND [RSP+0x78],RSI(0)` · `+0x56 XOR EBX`) 이라 `+0x106 MOV RAX,RSI` 로 간다. NULL 길 `+0x13a ~ +0x16b` 도 `+0x140 [RSP+0x58]` (0, `+0x51`) · `+0x160 TEST R14` (0, `+0x47`) 를 지나 `+0x169` — 결과서 (1c) 와 같은 뜻.
6. **보탬 — `UserGetDesktopDC` 의 종류 0 길**: 종류 `2` (IC) 는 `+0x59 JNZ → +0x110 · +0x117 JZ → +0x5f` → `+0x68 TEST ESI` (≠ 0) → **`+0x7b GreCreateDisplayDC(R15 = [[W32GetUserSessionState()+0xe338]+0x30], 2, …)`** — 곧 주 화면 장치로 만들어 비트 1 이 켜진다 `[정적: 줄 이음]`. 종류 `0` (DC) 는 UI 한도 검사 뒤 `+0x6a JZ → +0xaa` 로 가서, 시스템 스레드가 아니고 (`+0xad PsIsSystemThread`) 세션 쪽 프로세스 (`+0xcc [W32GetUserGdiSessionState()+0x28]`) 와 다르면 (`+0xdc CMP · +0xdf JZ`) **`+0x106 _GetDCEx([[[pti+0x1f0]+0x8]+0x18], 0, 0x80800003)`** — DC 캐시 길로 받는다. 그러니 일반 프로세스의 `CreateDCA("DISPLAY")` 는 `_GetDCEx` 를 탄다 `[정적: 줄 · 칸 뜻은 추측]`. `+0x51 JZ` (기능 깃발 꺼짐) 갈래는 안 읽음. → Q278b 5 에 보탬.
7. **보탬 — 이름**: `RestoreAttributes` 의 PDB 꾸민 이름 `?RestoreAttributes@DC@@QEAAXPEAU_DC_ATTR@@@Z` (`.asm` ALIASES) — 형 이름 `_DC_ATTR` 은 PDB 에 있다 (칸 정의는 안 봄). `SelectObjectImpl +0x26a` 는 `[+0xf0] & 3`, `+0x55f` · `+0x610` 은 `& 0x11` (결과서 칸 표의 "`& 0x11`" 은 뒤 둘).
- 그 밖 결과서 오프셋 (요약 1 ~ 14 · 표 (1a)(1b)(1c)(2a)(2b)(3)(4) 의 `+0x…` 거의 전부 — 위 "다시 본 줄") 과 syscall 번호 10개 · PDB GUID 다섯은 **일치**.

## 앞 run 고쳐 읽기

틀린 줄은 없고, 종류 비트가 다를 때의 길 · `DC_ATTR` 크기가 좁혀진 것이라 "정정" 이 아니라 "고쳐 읽기" 로 run144 맨 위에 한 줄 넣었다.

- **run144**: 6절 "`GetDeviceCaps(DC)` — 표 칸이 0 이면 0, `TEB+0x68 = 0x57`" · PE_emulator 표 "DC 길 `0x57` / 0" 은 **핸들 종류 비트가 `0x10000` 일 때의 길**이다. 에뮬레이터 `0x21000` · `0x20c00` 처럼 종류 비트가 `2` 면 `GetDeviceCaps +0x66 JNZ → +0x25d` 로 표를 보기 전에 갈라져 `pldcGet` = 0 → **`TEB+0x68 = 6`** + 0 (`+0x281`). 3절 칸 표 "`+0x10` … DC → `DC_ATTR` (≥ 0x130 B)" 는 맞고, 잡는 쪽은 **`HmgAllocateDcAttr` (슬롯 `0x170` B)** — 2절 끝 `HmgAllocateObjectAttr` (0x18 B) 는 영역 쪽 이름이다. 다음 요청 후보 N1 은 닫힘 (Q278a), N2 ~ N5 는 Q278b.

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (gdi32 · gdiplus 안 올림) 이고 PEB `+0xf8` 은 test.exe 사진에서 (null) 이다.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 7 — 대기열 **Q277** (같은 VM GUI 세션 — Q162 · Q273 과 합친 줄) 에 출처로 얹을 꼴. test.exe 가 아니라 gdiplus 를 부르는 GUI 프로세스, 그 프로세스 조건):

1. `gdi32full!hdcCreateDCW+0xd0` (`rax` = HDC · `[rsp+0x58]` = 0?) · `+0xf1` · `bCreateDCA+0xd9` · `CreateICA+0x11` — HDC 비트 (종류 `0x10000`? 스톡? 고유?).
2. 그 HDC 의 표 칸 (`PEB+0xf8 + (H & 0xffff)*0x18`, 24 B) 과 풀린 `DC_ATTR` `0x170` B 덤프 (`+0x0 · +0x90 · +0x98 · +0xa0 · +0xa8 · +0xf0 · +0xf8 · +0x108 · +0x114 · +0x128 · +0x160`) — `gdi32!GetDeviceCaps+0x1c0` 의 `r8`, `[r8]` 비트 0 · 1.
3. `pGdiDevCaps` (`PEB+0xf8 + 0x180000`) `0x98` B 덤프 · `GetDeviceCaps` 끝 (`+0x1d2` · `+0x1f7` 등) 의 `eax` — 지표 `0x58` · `0x5a` · `8` · `0xa` 특히.
4. `gdi32full!CreateCompatibleDC+0x103` (`[r8+0x108]`) · `+0x115` (`rdi`) · `CreateCompatibleBitmap+0x12e` · `+0x1e0` · `GetDIBits+0x133`.
5. `gdiplus!InternalGdiplusStartup+0x293` (`rax`) · `+0x33f` (`eax`) · `GpDevice+0x1a3` (`[rbx+8]` = `0x76654431`?) · `+0x1ea` (`[rbx+0x628]`) · `ExtractPixelFormatFromHDC+0x12f` (`ebx` — `0x22009`? `[추측]`).
6. 커널: `win32kbase!NtGdiOpenDCW+0x1c1` (`r9d`) · `UserGetDesktopDC+0x7b` / `+0x106` (어느 갈래) · `GreCreateDisplayDC+0x231` · `+0x243` · `+0x20a` (`rax`) · `DCMEMOBJ+0x1b8` · `SetupDCAttributes+0x82` · `NtGdiGetDeviceCaps+0x93` (`eax`) · `GreGetDeviceCapsInternal+0xc8` · `+0xec` · `+0x11a` · **`[W32GetSessionState()+0x58]+0x90` `0x1b0` B** (스톡 핸들 값).
7. `gdi32full!GdiTrackHCreate+0x14` 의 `g_GDIHandleCount` 처음 값 (이번 길과는 무관).

정적 (대기열 **Q278b** 에 얹을 꼴 — 결과서 "win32k 몫 남음" 1 ~ 6 + "다음 요청 후보"):

1. 세션 기본 `DC_ATTR` 틀 `[W32GetSessionState()+0x58]+0x90` (0x1b0 B) 을 채우는 곳 — 스톡 붓 · 펜 · 글꼴 핸들과 `+0x0` · `+0xf0` · `+0x108` 처음 값.
2. `GrepCreateCompatibleDC` (`0x14950`) 본문 — `GreCreateDisplayDC` 인자 (`+0x126` · `+0x256`), 메모리 DC 의 `DC_ATTR+0x0` 비트, `+0x2a4 vSetDpiScaling`.
3. `GDIINFO` (`PDEV+0x848`) 를 채우는 곳 (`+0x10 · +0x14` · `+0x28 · +0x2c`) 과 `pGdiDevCaps` 블록 (`+0x180000 ~ +0x180090`) 을 채우는 곳 — `GdiProcessCallout` 쪽 (run86 · Q163 ① 과 겹침).
4. `[W32PROCESS+0x10c / +0x110]` · `[스레드+0x190 / +0x198]` (DPI 문맥) 이 써지는 곳 — `NtUserSetProcessDpiAwarenessContext` 길. `GreGetDeviceCapsInternal +0x1a9 ~ +0x1e4` (`EngMulDiv`) 와 `[PDEV+0x990]`.
5. `_GetDCEx` → `CreateCacheDC` 줄 (`DCE` 만들기) · `UserGetDesktopDC` UI 한도 (`CanPerformUIOperation` · `HasUILimit`) 본문 — 메시지 상자 12 (`NtUserGetDCEx(0,0,3)`) · `CreateDCA` 5. + flow-writer 대조 6 의 `UserGetDesktopDC +0x106 _GetDCEx(…, 0x80800003)` 갈래.
6. `GreCreateDisplayDC` 조건 `[session+0xe338]→+0x30` (주 화면 장치) 이 세워지는 곳, `hdcOpenDCW` (프린터 · `\\.\DISPLAYn`, `UserGetMonitorDC`).
7. (다음 요청 후보 그대로) **N2** `InitImagingLibrary` (`+0x62b`) · `SysColorNotify` · `InitSystemFontsDirs` · `GetGpInstalledFontCollection` — 그 뒤 벽. **N3** 글 폭 (`GdiGetCharDimensions` · `pcfLocateCFONT` · `bComputeTextExtent`, `DC_ATTR+0x114` · `+0x128`) — 메시지 상자 12 는 N1 만으로는 안 풀림. **N4** · **N5** 는 Q278b 그대로.
8. (새로) `GetDeviceCaps` 의 `g_systemCallFilterId == 5` 길 (`+0x419`) 과 user32 `GetDC +0x0` 의 같은 검사 — 에뮬레이터 `GetProcessMitigationPolicy` 가 0 일 때만 안전. 그리고 대조 3 의 `GpDevice [this+0x628]` 을 `PaletteChangeNotification` 이 채우는 조건 (낮음).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함). 이번에 읽은 줄에서 디버거 여부를 보는 줄은 없었다 `[정적: 읽은 줄 안에서]`.
- **스크립트 주의 (결과서 — 오류 아님, 제안만, plan 의 하네스 제안 묶음에 이미 보탬)**: `ExportFunctions.java` 는 한 번 실행 안에서만 같은 이름 둘째부터 `__<rva>` 를 붙여서, 정규식이 이미 `.asm` 이 있는 이름의 **다른 함수** 까지 맞히면 기존 파일을 덮을 수 있다 (예: gdiplus `Initialize.asm` = `OSInfo::Initialize` 인데 `^Initialize$` 면 `GpSemaphore::Initialize` 가 가져갈 수 있음 `[추측]`). 이번엔 그 함수를 뽑지 않고 `objdump` 로만 읽었다. flow-writer 는 `.claude/` 를 고치지 않는다.
- 지난 run144 도구 메모의 `hdcCreateDCW` 아래 `File name too long` 은 이번에 깊이 0 으로만 뽑아서 안 났다 (결과서). `IcmInitLocalDC.asm` 은 이번에 생겼다.
- 같은 때 explainer 가 run146 해설서를, decompiler 가 sast/144 · 146 을 썼다 — 이 문서는 그 파일을 안 건드렸다. run146 문서는 동적 대기열 번호 두 곳 (`Q273`) 만 채웠다.

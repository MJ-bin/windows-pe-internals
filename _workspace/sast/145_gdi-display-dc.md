# 145 gdi-display-dc — 정적 분석 요청

- 날짜: 2026-11-26
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q278a** (Q278 의 N1 만 앞으로 — Q278 을 그 자리에서 Q278a / Q278b (N2 ~ N5) 로 나눔). PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/367_internals-gdi-display-dc.md` — **원문을 먼저 끝까지 읽을 것** ("이미 아는 것" 에 호출 줄 훑은 것 · 로그 수). 사용자 결정 2026-11-26 "이것도 부탁해" (365 둘 다음). **정적만.**
- **먼저 읽는 것**: `_workspace/sast/141_gdi-startup_result.md` 전부 (run144 — 1절 차례 4 ~ 7 · 3절 표 칸 0x18 B · 핸들 비트 · 가리기 식 · 6절 `GetDeviceCaps` · "다음 요청 후보" N1), `runs/03_sprint/run144-gdi-startup.md` (flow-writer 대조로 고친 오프셋 있으면 그쪽이 맞음).
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `gdi32.dll` · `gdi32full.dll` · `gdiplus.dll` — **이미 analyze 됨** (run144). 있는 asm 읽기 + 없는 함수만 `^이름$` export (예: `IcmInitLocalDC` — run144 도구 메모에 긴 파일 이름 문제로 안 뽑혔을 수 있음. 그러면 rva 로 뽑고 결과서 "스크립트 문제" 에 적기).
  - `win32kfull.sys` · `win32kbase.sys` — **이미 analyze 됨**. 1 (a) 9 째 인자 · 2 `DC_ATTR` 잡는 쪽 · 3 (a) 값 출처에 필요한 함수만. **⚠ 지금 sast/142 가 이 두 프로젝트를 쓴다.** win32k export 가 필요해지면 먼저 `_workspace/sast/142_dialog-window_result.md` 가 있는지 본다 — **없으면 Ghidra 로 열지 말고** 있는 asm (`win32kbase/HmgAllocateObjectAttr.asm` 등) 만 읽고, 못 한 win32k 몫을 결과서 "win32k 몫 남음" 절에 목록으로 적은 뒤 그 상태로 끝내고 보고한다 (오케스트레이터가 142 가 끝난 뒤 이어 부름).
- 주의: 같은 시간에 다른 decompiler 가 **user32 · win32kfull · win32kbase** (sast/142) · **RpcEpMap · rpcrt4** (sast/143) · **combase · rpcss** (sast/144) 를 돈다 — 그쪽은 건드리지 않는다. 새 export 전에 `free -g`. scratchpad 임시 파일은 `s145/` 하위에.

## 왜 (요청한 쪽 말 요약)

- `GdiplusStartup` = 1 (129 샘플) 은 지금 run144 차례 4 (영역 표 칸) 에서 멈춤 → 고치면 바로 차례 5 `CreateICA("DISPLAY")`. 표 칸 짓기를 영역 · DC 한 번에 하려고 N1 을 먼저 받으려 함. 같은 DC 칸이 `GetDeviceCaps(0x58/0x5a)` = 0 (362 샘플) 도 풂.
- 에뮬레이터 정책 (D156): 보이지 않는 가짜 창 — 그리지 않음. win32k GDI 를 옮기지 않음 → 알고 싶은 것은 커널 본문이 아니라 **gdi32 · gdi32full · gdiplus 가 그 HDC 로 무엇을 읽고 검사하나**. 값 (DPI `0x60` · 1920 × 1080) 은 에뮬레이터가 정함 → 필요한 것은 **어느 칸에 들어가고 무엇과 같아야 하는지**.

## 알고 싶은 것 (원문 번호 그대로)

1. **`CreateICA("DISPLAY", 0, 0, 0)` 이 HDC 를 돌려줄 때까지** (`bCreateDCA` → `hdcCreateDCW`)
   - (a) `NtGdiOpenDCW` 인자 9 칸의 뜻과 `CreateICA` / `CreateDCA` 가 다르게 넣는 칸 (5 째 `1` = IC 표시?). 9 째 (출력 포인터) 에 커널이 쓰는 것을 유저 쪽이 읽나.
   - (b) `"DISPLAY"` 일 때 프린터 길 (`bLoadSpooler` · UMPD · `pldcCreate` · `DocumentEventEx`) 을 건너뛰는 조건 — syscall 앞 · 뒤.
   - (c) syscall 뒤 `hdcCreateDCW` 가 **보는 것**: 반환 0 처리 · `+0x5cc IcmInitLocalDC` · `+0x5ed ~ +0x614` 칸 읽기 · ICM 쪽 읽기 / 쓰기 · 더 나가는 syscall. **NULL 로 나가는 가지 전부** (`+0x7a8` 로 가는 조건).
   - (d) HDC 꼴: 종류 `0x10000` 인가 (IC 와 DC 가 같은가) · 스톡 비트 · 고유 수 — run144 "핸들 비트" 규칙으로 고르면 되나.
2. **DC 칸과 `DC_ATTR` 최소 꼴** (1 · 3 의 길에서 유저 쪽이 읽는 / 쓰는 칸만)
   - 표 칸: run144 3절 그대로인지 한 줄.
   - `DC_ATTR`: 크기 (잡는 쪽 — `HmgAllocateObjectAttr` 가 맞나) 와 **칸 표** — 오프셋 · 크기 · 읽는 줄 · 쓰는 줄 · 0 이어도 되나 / 특정 값이어야 하나. 특히 `+0x0` 비트 1 (`GetDeviceCaps +0x1c0` — 뜻 · 누가 켜나) · `+0x98` · `+0xa0` · `+0xa8` · `+0x128` 처음 값 (스톡 붓 · 펜 · 글꼴 핸들이어야 하나).
   - 같은 꼴이 `NtUserGetDC` · `NtUserGetDCEx` · `NtUserGetWindowDC` · `NtGdiCreateCompatibleDC` HDC 에도 그대로인가 (한 줄씩, 다르면 다른 칸만).
3. **그 HDC 로 하는 일**
   - (a) `GetDeviceCaps(hdc, 0x58 / 0x5a)`: `+0x1c0` 두 갈래 가운데 화면 DC 가 타는 쪽. syscall 쪽이면 `NtGdiGetDeviceCaps` 가 두 지표를 어디서 읽나 (DPI 가상화 — 96 고정인가) — "무엇과 같아야 하나" 만. 블록 쪽이면 `pGdiDevCaps` (`PEB+0xf8 값 + 0x180000`) 의 **지표 → 오프셋 표** 와 `+0x2f3 NtGdiGetDeviceCapsAll` 부르는 조건.
   - (b) 같은 표에 지표 `0x68` · `0xc` · `0x8` · `0xe` · `0xa` · `0x26` · `0x76` 도.
   - (c) gdiplus `InternalGdiplusStartup +0x296` 뒤 ~ `+0x5e3`: `DesktopIc` 로 부르는 GDI 함수 · syscall · 읽는 칸 (`GpDevice::GpDevice(hdc)` · `DriverGdi` · `DpBitmap::InitializeForGdiScreen` …). NULL 가지 가운데 **HDC · GDI 답에 달린 것** 과 조건 (할당뿐인 가지는 "할당뿐" 한 마디).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| gdi32full | `CreateICA` · `bCreateDCA` · `hdcCreateDCW` (있음) · `IcmInitLocalDC` · 길에 나오는 칸 읽기 함수 | 1 | 1 |
| gdi32 | `GetDeviceCaps` (있음) · `GdiGetEntry` · `HANDLE_TO_INDEX` (있음) · `pGdiDevCaps` 블록 읽는 줄 | 0~1 | 2 · 3 (a)(b) |
| gdiplus | `InternalGdiplusStartup` `+0x296 ~ +0x5e3` · `GpDevice` · `DriverGdi` · `DpBitmap::InitializeForGdiScreen` (있음) | 1 | 3 (c) |
| win32kbase / win32kfull | `NtGdiOpenDCW` 9 째 인자 쓰는 줄 · `DC_ATTR` 잡는 쪽 · `NtGdiGetDeviceCaps` 값 출처 — **위 ⚠ 조건** | 0 | 1 (a) · 2 · 3 (a) |

- 범위: 유저 쪽이 읽고 검사하는 것까지. 프린터 드라이버 본문 · ICM 색 프로필 본문 · 그리기 · N2 (`InitImagingLibrary` …) · N3 (글 폭) 는 이름만.
- 분량이 크면 **1 → 2 먼저, 3 (a)(b) 다음, 3 (c) 는 "다음 요청으로"** 라고 적어도 된다 (원문 "3 (c) 는 run 을 나눠도 됨").

## 받고 싶은 것

- 결과서 `_workspace/sast/145_gdi-display-dc_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (작은 물음마다 한 줄).
  3. 표: (1) run144 1절 꼴 **"최소 길 지도"** — `차례 · 부르는 것 / syscall (번호 — win32u _syscalls.tsv) · 조건 · 반환을 유저 쪽이 보나 · 0 이면 · 줄` · (2) **칸 표** (run144 3절 꼴) — 표 칸 · `DC_ATTR` · `pGdiDevCaps` 지표 → 오프셋 · (3) NULL 가지 표.
  4. PDB GUID 확인 줄 (export 한 모듈 — 기존 프로젝트와 같은지).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** — 원문 "답이 오면 쓰는 곳" (`nt_gdi_init2.cpp` · `nt_gdi_create_rect_rgn.cpp` · 새 `NtGdiOpenDCW` · `NtGdiGetDeviceCaps` · `nt_user_get_dc.cpp` · `nt_gdi_create_compatible_dc.cpp` · 새 `NtUserGetDCEx` · `NtUserGetWindowDC`) 마다 한 줄. **"stub 이 채울 최소 목록"** — run144 목록 1 ~ 7 에 이어 DC 핸들 꼴 · 표 칸 · `DC_ATTR` 블록 (크기 · 0 아닌 칸) · `NtGdiOpenDCW` · `NtGdiGetDeviceCaps` 답 — 과 **"그 뒤 벽" 한 줄** (N2 의 무엇).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (대기열 Q277 VM 에 얹을 꼴 — 멈춤점 `gdi32full!함수+0x…`) 과 **"다음 요청 후보"**.

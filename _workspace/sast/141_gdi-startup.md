# 141 gdi-startup — 정적 분석 요청

- 날짜: 2026-11-25
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 는 gdi 를 안 올림)
- 대기열: **Q270** (PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/343_internals-gdi-startup.md` — **원문을 먼저 읽을 것**. `GdiplusStartup` 안 syscall 차례 (에뮬레이터 로그) 와 AT03 실험 (스톡 붓 칸을 채우면 `RegisterClassEx` 가 실패) 이 거기 있다). 사용자 결정 2026-11-25 (PE_emulator D154) 의 넷째. **정적만.** 진짜 핸들 값·표 칸 내용은 GUI 동적 Q162 몫 — 이번에 안 함.
- **먼저 읽는 것** (이미 답한 칸은 가리키기만):
  - `runs/03_sprint/run86-gdi32-init.md` (PEB `+0xf8` `GdiSharedHandleTable` → GDI 공유 메모리 최소 `0x180150` B, `+0x180140` DC 붓 · `+0x180148` DC 펜 0 아님, `NtGdiInit2`)
  - `ms/disassemble/gdi32full/GetStockObject.asm` (스톡 객체 칸 `+0x1800b0 + 8 × i`)
  - `runs/03_sprint/run131-user32-regclass.md` (`RegisterClassExWOWW` 차례 — 배경 붓을 어디서 검사하나 이어서)
  - `_workspace/sast/139_user32-connect_result.md` (이 요청 직전 바퀴 — user32 겹침)
- 대상 빌드: Windows 11 24H2 26100.9457 x64.
  - gdi32full · gdi32 · user32: **analyze 됨** — `^이름$` export 만. `_index.tsv`: gdi32full `GdiValidateHandle` (0x70180, 133 B) · `GetStockObject` (0x25e90) · `GetObjectW` (0x107f0) · `SelectObject` (썽크 0x87680) · gdi32 `DeleteObject` (0x1070) · `SelectObject` (0x4150) · `GetObjectW` (0x4b80) · `GdiValidateHandle` (썽크 0xc5c0) · user32 `RegisterClassExW` (0x1c0d0) · `RegisterClassExWOWW` (0x1c99c).
  - **gdiplus**: `System32/GdiPlus.dll` **처음 analyze** (PDB 받기 포함). 에뮬레이터는 `C:\WINDOWS\System32\gdiplus.dll` 을 올린다 (원문). WinSxS 판은 보지 않는다.
- 주의: **sast/139 (user32) 가 끝난 뒤** 시작 (user32 프로젝트 겹침). gdiplus analyze 는 그 전에 걸어 둬도 된다 (다른 프로젝트면).

## 알고 싶은 것

1. **`GdiplusStartup` 이 1 (`GenericError`) 로 나가는 첫 가지** — 에뮬레이터 로그의 syscall 차례 (`NtQuerySystemInformation(0)`·`(1)` → `NtQueryInformationProcess(−1, 0x25)` (출력 안 씀) → `NtQueryLicenseValue` 둘 → `NtGdiCreateRectRgn(0,0,1,1)` (핸들 `0x20800`) → `NtGdiDeleteObjectApp(0x20800)` (기본 대답 0) → `NtProtectVirtualMemory` 둘 → `NtFreeVirtualMemory` → 1) 중 **무엇의 결과를 보고** 실패로 가나:
   - 후보: 영역 핸들 값 (종류 비트) · `NtGdiDeleteObjectApp` 반환 (0 이면 실패?) · `0x25` 출력 칸 · 라이선스 값 · 공유 핸들 표 칸 (`DeleteObject` 가 syscall 앞에서 표를 보나).
   - **실패 가지의 함수 이름 + 조건 줄**, 그리고 성공하려면 그 자리에 무엇이 있어야 하나.
2. **gdi32 가 핸들을 믿는 조건** — `GdiValidateHandle` (gdi32full) 이 공유 핸들 표에서 읽는 것:
   - 표 시작 (PEB `+0xf8` 그대로인가) · 칸 크기 · 핸들 값에서 칸 번호를 꺼내는 식 · 종류 칸 · 고유 번호 (핸들 위 16 비트와 대조?) · 주인 프로세스 칸 (PID 대조?) · 통과 조건 — 줄 근거.
   - `RegisterClassExWOWW` (또는 그 아래) 가 배경 붓에 `GdiValidateHandle` 을 부르는 자리 · 거절이면 어떤 오류 (`ERROR_INVALID_PARAMETER`?) — AT03 실험 (칸 채움 → 실패, 0 → 검사 건너뜀) 이 코드로 설명되나.
   - 스톡 객체 핸들 꼴 (스톡 비트 · 주인 0 등) — `GetStockObject` 가 `+0x1800b0 + 8 × i` 에서 꺼낸 값이 표의 어느 칸과 맞아야 통과하나. 그 칸을 채우는 쪽 (win32k 가 공유 메모리에 미리) 은 이름만.
3. (되면) **`DeleteObject` · `SelectObject` · `GetObjectW`** (gdi32 → gdi32full) 가 syscall 앞에서 같은 표를 보나 — 표가 비면 syscall 없이 실패하는 함수 목록.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| gdiplus | `GdiplusStartup` (+ 실패 가지까지의 callee — 첫 가지를 찾으면 멈춤) | 1~2 | 질문 1 |
| gdi32 · gdi32full | `DeleteObject` · `GdiValidateHandle` · (영역 만들기 `CreateRectRgn` 이 1 에서 나오면) | 1 | 질문 1·2 |
| user32 | `RegisterClassExWOWW` 배경 붓 검사 자리 (run131 asm 다시 읽기) | 0 | 질문 2 |
| gdi32full | `GetStockObject` (asm 있음) · `GetObjectW` · `SelectObject` 의 진짜 몸 | 1 | 질문 2·3 |

- 범위: 시작 실패 가지 · 핸들 표 검사 규칙. gdiplus 그리기 · win32k GDI 본문은 이름만.
- 크면 **1 → 2 → 3** 차례로 쓰고 나머지는 "다음 요청으로".

## 받고 싶은 것

- 결과서 `_workspace/sast/141_gdi-startup_result.md`.
  1. asm 을 남겨 줌. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1) `GdiplusStartup` 차례 표 (`차례 · 부르는 것 · 보는 결과 · 실패 조건 · 줄`) · (2) 핸들 표 칸 표 (`칸 오프셋 · 크기 · 뜻 · 검사 · 줄`) + 핸들 값 → 칸 번호 식 · (3) syscall 없이 실패하는 함수 목록.
  4. **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: 팀원 `ext/syscall/syscall_layer_cpp/src/gui/nt_gdi_init2.cpp` (공유 메모리 블록 — 지금 핸들 표 0) · `nt_gdi_create_rect_rgn.cpp` (핸들 값 꼴) · `NtGdiDeleteObjectApp` 새 stub · 보류 패치 `AT03_stock-objects_held.patch`. **"스톡 객체를 채우면서 `RegisterClassEx` 도 통과시키는 최소 표 칸"** 을 한 목록으로.
  5. PDB GUID 확인 줄 (gdiplus 새로 받은 것 포함).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (Q162 세션과 합치게) 과 **"다음 요청 후보"**.

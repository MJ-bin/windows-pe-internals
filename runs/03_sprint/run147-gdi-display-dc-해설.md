# run147 gdi-display-dc — 해설

> **고쳐 읽기 (run153)**: 2.8 표 `+0x98` "처음 값 `0x12001f`" 는 DC 를 만들 때 덮는 값이고 사무실 기본 양식 (세션 틀) 값은 `0x10` 이다 (유저가 보는 처음 값은 그대로 `0x12001f`). 2.7 "세션 기본 틀의 내용은 `[공백]`" · 2.8 "붓 · 펜 · 글꼴 = 스톡 핸들 (어느 것인지는 `[공백]`)" → 닫힘 `[정적]`: 붓 스톡 0 (WHITE_BRUSH) · 펜 스톡 7 (BLACK_PEN) · 글꼴 스톡 13 (SYSTEM_FONT, `[추측: 오프셋 일치]`), 채우는 곳은 `InitializeGre` → `InitializeDefaultDCAttributes`. 2.7 의 "남는 `+0x160` 은 0" 은 0 으로 시작해 gdi32full 이 글꼴 사본 목록 머리로 쓰는 칸이다. 4절 Q278b 의 `DC_ATTR+0x114` · 세션 틀 · `GrepCreateCompatibleDC` · 블록 채우는 곳 · DPI 칸 · 주 화면 판정 · 그 뒤 벽 (`InitImagingLibrary` — 벽 아님) 은 닫힘. 본문은 그대로 두었다 → [run153 문서](run153-gdi-font-dc.md)

> 이 글은 [run147 문서](run147-gdi-display-dc.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q278a** — PE_emulator 요청 "화면 DC 하나가 서는 길 — `CreateICA("DISPLAY")` → `NtGdiOpenDCW` 의 HDC · DC 칸 · `DC_ATTR` 최소 꼴 · `GetDeviceCaps(0x58 / 0x5a)`" (`_workspace/plan.md`, 이번에 **끝 (run147)**)
  - 원 요청 `../PE_emulator/_workspace/05_tuning_planner/367_internals-gdi-display-dc.md`, 튜닝 `366_tune18.md` GP1 · MB (요청 쪽 에뮬레이터 숫자):
    - `GdiplusStartup` = 1 을 받은 **129** 샘플
    - `GetDeviceCaps(0x58/0x5a)` = 0 을 받은 **362** 샘플
    - `CreateDCA` = 0 **5** 샘플 · 메시지 상자 **12** · `GetWindowDC` = 0 **288**
  - [run144](run144-gdi-startup-해설.md) 가 남긴 정적 대기열 **Q278** 에서 **N1 만 앞으로 당긴 것** 이다. 나머지 (N2 ~ N5 등) 는 **Q278b**.
  - 사용자 결정 2026-11-26 "이것도 부탁해". 정적만.
  - 결과서 판정 **분석성공** — 질문 1 (a)(b)(c)(d) · 2 · 3 (a)(b)(c) 모두 `[정적]` 줄 근거로 닫음.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q277** (같은 VM GUI 세션)
    - 정적 → **Q278b** (결과서 "win32k 몫 남음" 1 ~ 6 + "다음 요청 후보")
- 근거:
  - run 문서 [`run147-gdi-display-dc.md`](run147-gdi-display-dc.md) (가장 중요)
  - 요청서 [`_workspace/sast/145_gdi-display-dc.md`](../../_workspace/sast/145_gdi-display-dc.md) · 결과서 [`145_gdi-display-dc_result.md`](../../_workspace/sast/145_gdi-display-dc_result.md)
    - **처음 analyze 없음.** gdi32 · gdi32full · gdiplus · win32kbase · win32kfull 모두 이미 analyze 돼 있어서 `-readOnly` export · refs 만 했다.
    - win32k 둘은 sast/142 (대화상자) 가 같은 Ghidra 프로젝트를 쓰고 있어서, 142 결과서가 생긴 **뒤에** 열었다.
  - flow.md 변경: docs 커밋 `0d560df` (`git show 0d560df -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 user32 · gdi32 · gdiplus 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run142 · run144 · run146 과 같은 줄).
   - 사진의 PEB `GdiSharedHandleTable` 도 (null) 이다 `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:1779`, `_P2.txt:2489`).
   - 그래서 이번 이야기는 [run144](run144-gdi-startup-해설.md) 와 같은 자리 — **"`main` 뒤에 그림 라이브러리 (gdiplus) 를 켜는 프로그램"** 의 일이다.
2. **요청 쪽 숫자 · 로그는 에뮬레이터 것이다.**
   - "129 샘플", "`GetDC(0)` stub 값 `0x21000`", "`CreateDCA` 로그 `(0,0,0,0,1,0,0,0,포인터)`" 같은 것은 PE_emulator 로그다. 진짜 Windows 를 본 게 아니라서 `[관찰]` 이 아니다. 이 글에서는 "에뮬레이터 로그" 라고만 부른다.
3. **오프셋 읽는 법** (앞 해설들과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문 "`+0xc9 NtGdiOpenDCW`" · "`+0x1c0 TEST`" 처럼 명령 이름과 같이 적힌 숫자 = **함수 안 그 줄** (call 자리 포함).
   - "`+0xdb JZ → +0x13a`" 은 그 줄과 점프가 가는 곳.
   - 콜스택으로 본 적은 없다 (정적만).
4. **값은 하나도 안 봤다.**
   - 진짜 HDC 값 · 진짜 `DC_ATTR` 내용 · 진짜 DPI 값은 이번 범위 밖이다 (동적 Q277 몫).
   - 이번에 본 건 "**코드가 어느 칸을 읽고, 무엇과 비교하고, 0 이면 어디로 가나**" 뿐이다.
   - PE_emulator 쪽 목록의 "`0x00010c00`" · "1920 · 1080 · `0x60`" 같은 값은 **비트 규칙으로 만든 예 · 에뮬레이터가 정한 값** 이다. 진짜 값이 아니다.
5. **칸 이름은 짐작이다.**
   - PDB 에 `_DC_ATTR` 라는 **형 이름** 은 있다 — `RestoreAttributes` 의 꾸민 이름 `?RestoreAttributes@DC@@QEAAXPEAU_DC_ATTR@@@Z` 안에 들어 있다 `[정적]`. 하지만 **칸 정의는 안 봤다.**
   - 그래서 "`+0xa0` = 붓", "`+0x0` 비트 1 = 주 화면 위 DC" 같은 **이름 · 뜻은 `[추측]`**, **오프셋과 그 칸을 읽는 줄은 `[정적]`** 이다.
   - `GetDeviceCaps` 지표 번호의 SDK 이름 (`LOGPIXELSX` 등) 짝도 `[추측]` 이다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- TEB = 직원 **개인 안내판** → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.2
- **DPI** = 화면 확대 비율 (글자 · 그림을 몇 배로 그리나) → [run85 해설](run85-user32-init-해설.md) 2.5 (이번에 커널 쪽 값 출처를 2.11 에서 더 본다)
- **run86 에서 푼 것** → [run86 해설](run86-gdi32-init-해설.md)
  - gdi32 = 그림 도구 건물 **앞 동**, gdi32full = **본관**, 앞 동 6바이트 `JMP` = 본관으로 가는 **통로** (썽크) → 2.1
  - **지연 가져오기** (처음 부를 때 칸을 채움, `NtProtectVirtualMemory` 두 번) → 2.6
  - GDI 공유 메모리 (PEB `+0xf8`) = **그림 도구 진열장**, **DC** = 도화지 + 그 도화지 전용 도구 세트, **스톡 객체** = 처음부터 있는 공용 붓 · 펜 → 2.10
  - `pGdiDevCaps` = 진열장 값 `+ 0x180000` (`GdiProcessSetup +0x456`) → 2.10 표
- **run142 에서 푼 것** → [run142 해설](run142-user32-connect-해설.md) 2.8 — 메시지 상자가 `NtUserGetDCEx(0,0,3)` 로 받는 **DC** (그리기 판), "GDI 답이 0 이면 조용히 0"
- **run144 에서 푼 것** → [run144 해설](run144-gdi-startup-해설.md)
  - **GDI+ (gdiplus)** = 고급 화방, `GdiplusStartup` = 화방 개점 → 1절 "새 말 — GDI+"
  - 핸들 = **번호표**, 공유 핸들 표 = 진열장 앞쪽 **번호표 대장**, 칸 = 대장의 **한 줄** → 2.3
  - **`gW32PID`** (우리 가게 번호) · **가려진 포인터** (`gCookie` 로 잠근 보관함 열쇠 쪽지) → 2.5 "새 말 셋"
  - **핸들 비트** (0 ~ 15 칸 번호 · 16 ~ 22 종류 · 23 스톡 · 24 ~ 31 고유) · 칸 24 B → 2.6
  - **`DC_ATTR`** = 도화지 옆 **"지금 쥐고 있는 붓 · 펜 · 글꼴" 메모장** → 2.13 "새 말 — `DC_ATTR`"
- **run145 에서 푼 것** → [run145 해설](run145-epm-endpoint-해설.md) 2.2 — **CFG 간접 호출** = 교환원 앞의 경비

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, PEB = 가게 **안내판**, TEB = 직원 **개인 안내판**, 커널 = **관리사무소**, syscall = **신청 창구**.
- gdi32 = 그림 도구 건물 **앞 동**, gdi32full = **본관**, win32k = 관리사무소 **그래픽 부서**, gdiplus = **고급 화방**.
- GDI 공유 메모리 (PEB `+0xf8`) = **그림 도구 진열장**. 앞쪽 `+0x0 ~ +0x180000` 이 **번호표 대장** (run144).
- 핸들 = **번호표**. DC = **도화지** (+ 그 도화지 전용 도구 세트). `DC_ATTR` = 도화지 옆 **메모장**.
- 이번에 새로 붙는 그림:
  - **IC** = **"보기만 하는 도화지"**. 그림은 안 그리고 "이 화면 크기가 얼마지? DPI 는?" 같은 것만 물어볼 때 쓰는 도화지 번호표.
  - 메모장 맨 첫 칸의 비트 1 = **"본점 화면용" 스티커**. 이 스티커가 붙어 있으면 본관은 몇몇 질문을 진열장 안 **화면 정보 게시판** (`pGdiDevCaps`) 을 보고 바로 답한다.
  - 그래도 **DPI 질문은 스티커가 있어도 늘 그래픽 부서에 신청한다** (신청 창구 = `NtGdiGetDeviceCaps`).
  - `CreateCompatibleDC` 로 만드는 DC = 화면 도화지를 본뜬 **연습장**.
- run 문서 "결과" 비유는 이름이 조금 다르다 — HDC = "도화지 **대여증**", 공유 핸들 표 = "**출입증** 대장", `DC_ATTR` = "**사물함** 열쇠", 커널 = "**본점** 에 전화" (run 문서도 이 비유를 `[추측: 비유]` 로 둔다). 이 글은 앞 해설 그림 (번호표 · 번호표 대장 · 메모장 · 그래픽 부서) 에 맞춰 부른다. 그림만 다르고 사실은 같다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| 1 (a) `NtGdiOpenDCW` 인자 9 칸은 | `CreateICA("DISPLAY",0,0,0)` = `(0, 0, 0, 2, 1, 0, 0, 0, &지역)`. **IC 표시는 4 째 (`2`)**, 5 째 `1` 은 "화면" 표시. `CreateDCA` 는 4 째만 `0`. 9 째 출력 포인터는 화면 길에서 안 읽는다 `[정적]` (2.4) |
| 1 (b) 프린터 길은 어떻게 건너뛰나 | syscall **앞**: 이름이 "DISPLAY" 면 `EBX = 1` → `hdcCreateDCW +0x79` 가 프린터 문을 통째로 건너뜀. syscall **뒤**: 프린터 핸들 0 → 바로 돌려줌 `[정적]` (2.3) |
| 1 (c) syscall 뒤 무엇을 보나 | **반환이 0 인가 · 종류 비트가 `0x10000` 인가** 둘뿐. 화면 길의 NULL 은 **`NtGdiOpenDCW` = 0 하나뿐** `[정적]` (2.5) |
| 1 (d) HDC 꼴은 | **IC 도 DC 와 똑같이 종류 비트 `0x10000`**. 스톡 비트 꺼짐, 고유 번호 아무거나. run144 핸들 비트 규칙 그대로 `[정적]` (2.6) |
| 2 표 칸 · `DC_ATTR` 크기는 | 표 칸은 run144 3절 **그대로**. `DC_ATTR` 은 `HmgAllocateObjectAttr` (0x18 B) 가 **아니라** **`HmgAllocateDcAttr` 의 `0x170` B 슬롯** `[정적]` (2.7) |
| 2 `DC_ATTR` 에 꼭 필요한 값은 | **0 아닌 값이 필요한 칸은 없다.** `+0x0` 비트 1 ("주 화면 위 DC" `[추측]`) 은 커널이 켜지만 0 이어도 된다 `[정적]` (2.8) |
| 3 (a) `GetDeviceCaps(0x58 / 0x5a)` 는 | 비트 1 이 켜졌든 꺼졌든 **늘 syscall `NtGdiGetDeviceCaps`**. 커널은 DPI 문맥을 따라 **두 지표에 같은 값** 을 준다 `[정적]` (2.9 · 2.11) |
| 3 (b) 지표 → 게시판 칸 | `0x2 → +0x4` · `0xc → +0x20` · `0xe → +0x24` · `0x26 → +0x34` · `0x68 → +0x4c` · `0x76 → +0x6c` 등. `0x8` · `0xa` 는 늘 syscall `[정적]` (2.10) |
| 3 (c) gdiplus 차례 5 ~ 7 은 | **`NtGdiOpenDCW` ≠ 0 · `NtGdiCreateCompatibleDC` ≠ 0 만으로 `+0x5e3` 까지** 간다 `[정적]` (2.12 · 2.13) |
| 덤 — 362 샘플은 왜 0 | 에뮬레이터 `0x21000` 은 종류 비트가 `2` 라 `GetDeviceCaps` 가 **대장을 펴 보기도 전에** `LastError 6` + 0 `[정적]` (2.13) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**초기화가 끝난 뒤, 프로그램이 gdiplus 를 켤 때** 다. run144 가 멈춘 자리 바로 다음이다. test.exe 에는 없는 옆길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → LdrpInitializeProcess
 │    └─ (2.7) dll 전원 켜기
 │          └─ (user32 를 올리는 프로세스라면) user32 DllMain → GdiDllInitialize
 │                └─ NtGdiInit2 → 그래픽 부서가 진열장 (PEB +0xf8) 을 걺        ← run86
 └─ … → NtContinue (은퇴) → exe 진입점 → (CRT) → main
                                                   └─ (그림을 그리는 프로그램이라면)
                                                       gdiplus!GdiplusStartup → InternalGdiplusStartup
                                                        ├─ (4) CreateRectRgn → 표 칸 검사           ← run144 (첫 가지)
                                                        ├─ (5) CreateICA("DISPLAY")                 ← 이번 (run147)
                                                        ├─ (6) GetDeviceCaps(0x58 · 0x5a)            ← 이번
                                                        ├─ (7) GpDevice → CreateCompatibleDC …       ← 이번
                                                        └─ (8) InitImagingLibrary                    ← 그 뒤 벽 (Q278b)
```

- run144 는 화방 개점 체크리스트의 **4번 항목 (모양 자 하나 빌리기)** 까지 봤다. 이번은 **5 ~ 7번 항목** — 화면 도화지 받기 · DPI 묻기 · 연습장 만들기 — 이다.
- flow.md 에서도 3장 user32 절 아래, **대화상자 하위 절 (run146) 다음** 에 새 하위 절 "화면 DC 하나가 서는 길" 로 들어갔다.
- 프로그램 쪽에서 부르는 일이라 CLAUDE.md 범위로 치면 "`main` 안에서 OS 를 다시 부르는 화살표" 쪽이다. 그래서 그림엔 안 넣었다.

### 왜 이 질문이 지금 왔나

- PE_emulator 는 진짜 nt · ntdll 이름으로 짓는 에뮬레이터다. win32k (그래픽 부서) 는 없고, `NtGdi*` syscall 은 임시 stub 이나 기본 대답을 준다.
- 에뮬레이터 정책 (D156) 은 "보이지 않는 가짜 창 — 그리지 않음. win32k GDI 를 옮기지 않음" 이다.
  - 그래서 알고 싶은 것은 커널 본문이 아니라 **gdi32 · gdi32full · gdiplus 가 그 HDC 로 무엇을 읽고 검사하나** 다.
  - DPI `0x60` · 1920 × 1080 같은 **값은 에뮬레이터가 정한다.** 필요한 건 "**어느 칸에 들어가고, 무엇과 같아야 하나**" 뿐.
- 막힘 (에뮬레이터 로그, 원 요청):
  - `GdiplusStartup` = 1 (129 샘플) 은 아직 run144 차례 4 (영역 표 칸) 에서 멈춘다. 그걸 고치면 **바로 차례 5 `CreateICA("DISPLAY")`** 가 기다린다.
  - 에뮬레이터 쪽은 표 칸 짓기를 **영역 · DC 한 번에** 하고 싶어서 N1 을 먼저 받으려 했다.
  - 같은 DC 칸 이야기가 `GetDeviceCaps(0x58/0x5a)` = 0 (362 샘플, HDC = `GetDC(0)` stub `0x21000`) 도 풀 것 같았다.

### 무엇이 궁금했나

1. **`CreateICA("DISPLAY",0,0,0)` 이 HDC 를 돌려줄 때까지** — (a) `NtGdiOpenDCW` 인자 9 칸 · IC / DC 차이 · 9 째를 유저가 읽나 (b) 프린터 길 건너뛰기 (c) syscall 뒤 보는 것 · NULL 가지 전부 (d) HDC 꼴.
2. **DC 칸과 `DC_ATTR` 최소 꼴** — 표 칸 · `DC_ATTR` 크기 (`HmgAllocateObjectAttr` 가 맞나) · 칸 표 · `+0x0` 비트 1 · 처음 값 · 다른 길 HDC.
3. **그 HDC 로 하는 일** — (a) `GetDeviceCaps(0x58/0x5a)` 갈래 · 값 출처 (b) 지표 표 (c) gdiplus `+0x296 ~ +0x5e3`.

### 바퀴 전에 알던 것

- `[정적]` (run144) `InternalGdiplusStartup` 차례 — 4 `CreateRectRgn` (첫 가지) · **5 `CreateICA("DISPLAY")` → `+0x296 JZ → +0x7d1` → 1 (다음 벽)** · 6 `GetDeviceCaps(0x58 / 0x5a)` (≤ 0 이면 96.0) · 7 `GpDevice` …
- `[정적]` (run144) 공유 핸들 표: PEB `+0xf8` 그대로 시작, 칸 0x18 B, 칸 번호 = 핸들 `& 0xffff`. DC 쪽 함수는 칸 `+0xe == 1` · `+0xc == H>>16` · `+0x8 & ~1 == PID` (0 안 됨) · `+0x10` 풀어도 ≠ 0 을 본다.
- `[정적]` (run144) `GetDeviceCaps(DC)` — 표 칸이 0 이면 0, `TEB+0x68 = 0x57`, syscall 없음 (run144 해설 2.13 표). → **이번에 "종류 비트가 `0x10000` 일 때만" 이라고 좁혀졌다** (2.13).
- `[정적]` (run144) `DC_ATTR` 은 "≥ 0x130 B" — `+0x98` 바뀜 표시 · `+0xa0` 붓 · `+0xa8` 펜 · `+0x128` 글꼴. 잡는 쪽은 몰랐다.
- `[정적]` (run86) `pGdiDevCaps` = PEB `+0xf8` 값 `+ 0x180000` (`GdiProcessSetup +0x456`). 그 안이 무엇인지는 몰랐다.
- `[정적]` (run142) 메시지 상자는 `NtUserGetDCEx(0,0,3)` 로 DC 를 받는다.

---

## 2. 찾은 것

### 2.1 한눈에 — 화면 도화지 받기 · DPI 묻기 · 연습장 만들기 `[정적]`

run 문서 "호출 관계" 를 줄였다 (나무 숫자 = ret 자리, 검사 줄은 함수 안 줄).

```
gdiplus!InternalGdiplusStartup (0xe1380)
 ├─ (5) +0x287  gdi32!CreateICA ⇒ gdi32full!CreateICA (0x42c90)          NULL → +0x296 → 1
 │               ├─ +0x11  bCreateDCA     +0x8f _stricmp "DISPLAY" → EBX = 1 (화면)
 │               │          └─ +0xd9  hdcCreateDCW(0,0,0,1,1,0)
 │               │                     +0x79 화면 → 프린터 문 건너뜀
 │               │                     +0xd0 NtGdiOpenDCW(0,0,0, 2(IC)/0(DC), 1, 0,0,0, &지역)  ── SYSCALL 0x10c7
 │               │                            ⇒ win32kbase  1 째 NULL → UserGetDesktopDC → GreCreateDisplayDC
 │               │                                 DCMEMOBJ · HmgAllocateDcAttr (0x170 B) · [DC_ATTR] |= 2 · SetupDCAttributes
 │               │                     +0xdb 0 → NULL · +0xe5 종류 0x10000 · +0x322 프린터 핸들 0 → 그대로 반환
 │               └─ +0x18  GdiTrackHCreate (꼬리)   개수 +1, HDC 그대로
 ├─ (6) +0x2ab · +0x2d2  gdi32!GetDeviceCaps(0x58 · 0x5a)
 │               +0x66 종류 ≠ 0x10000 → pldcGet → 0 · TEB+0x68 = 6
 │               +0x163 ~ +0x1ba 표 칸 실패 → 0 · 0x57
 │               +0x1c0 [DC_ATTR] 비트 1 꺼짐 / case 1 → +0x1d2 NtGdiGetDeviceCaps  ── SYSCALL 0x12bf
 ├─ (7) +0x32b  GpDevice(hdc)   +0xb1 CreateCompatibleDC(hdc) ⇒ NtGdiCreateCompatibleDC  ── SYSCALL 0x104f
 │      +0x33f  CheckValid<GpDevice>  → +0x341 (CreateCompatibleDC 0 · TECHNOLOGY 1 + BITSPIXEL ≤ 8)
 ├─     +0x573  InitializeForGdiScreen → InitializeForGdiBitmap → ExtractPixelFormatFromHDC
 │               CreateCompatibleBitmap(hdc,1,1) (0 → 포맷 0, 조용히) · GetDIBits · DeleteObject
 └─ (8) +0x630  InitImagingLibrary   → +0x632 → 1                    ← 그 뒤 벽 (N2, Q278b)
```

- `⇒` 는 지연 가져오기 통로 · syscall 너머를 건너뛴다는 표시다.

비유: 고급 화방이 개점 체크리스트 5번 — "화면용 도화지 한 장" — 을 본관에 부탁한다. 본관은 이름을 보고 "아, 화면이구나" 하고 **프린터 창구는 들르지도 않고** 그래픽 부서에 바로 신청한다. 번호표를 받으면 "빈 번호표인가 · 도화지 번호표인가" 두 가지만 보고 화방에 건넨다. 화방은 그 도화지로 "DPI 가 얼마?" 를 두 번 묻고, 그 도화지를 본떠 연습장을 한 장 더 만든다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `+0xd0 NtGdiOpenDCW(0,0,0, 2(IC)/0(DC), 1, 0,0,0, &지역)  ── SYSCALL 0x10c7`

- **누가**: gdi32full 의 `hdcCreateDCW` (`0x42ef0`, 도화지 신청서 쓰는 직원).
- **어디서**: 함수 안 `+0xc9` 에서 call, 돌아오는 자리 (ret) 가 `+0xd0`. 인자는 바로 앞 `+0x8a ~ +0xc6` 이 만든다.
- **누구를**: `NtGdiOpenDCW` — win32u 를 거쳐 그래픽 부서로 가는 syscall (번호 `0x10c7`, 가져오기 칸 `0x1800c2068`).
- **무엇을 위해**: 화면 도화지 번호표 (HDC) 하나를 받는다. 받은 뒤 `+0xd8 TEST · +0xdb JZ → +0x13a` 로 "0 인가" 를 보고, `+0xe5 CMP` 로 "종류 비트가 `0x10000` 인가" 를 본다.
- 태그: `[정적]` (`ms/disassemble/gdi32full/` 줄).

#### 줄 2: `+0x1c0 [DC_ATTR] 비트 1 꺼짐 / case 1 → +0x1d2 NtGdiGetDeviceCaps  ── SYSCALL 0x12bf`

- **누가**: gdi32 의 `GetDeviceCaps` (`0x17f0`, 장치 능력 묻기 창구).
- **어디서**: 함수 안 `+0x1c0 TEST byte [DC_ATTR],2` — 메모장 첫 바이트의 비트 1 을 본다. 꺼져 있으면 (또는 켜져 있어도 점프 표에서 "case 1" 로 가면) `+0x1c6 MOV EDX,EBX` → `+0x1cb` 에서 call, ret 가 `+0x1d2`.
- **누구를**: `NtGdiGetDeviceCaps` (syscall `0x12bf`).
- **무엇을 위해**: 지표 하나 (여기선 `0x58` 또는 `0x5a`) 의 값을 그래픽 부서에 묻는다. 반환을 그대로 돌려준다.
- 태그: `[정적]`.

### 2.3 ★ (1 b) "DISPLAY" 를 알아보는 순간 — 프린터 길을 앞뒤에서 건너뛴다 `[정적]`

**새 말 — IC (정보 컨텍스트)**: DC 와 같은 꼴이지만 **그리기는 안 하고 장치 정보만 물어볼 때** 쓰는 것 `[추측: 일반적으로 알려진 설명]`. `CreateICA` 로 만든다. 비유: **"보기만 하는 도화지"** — 종이 크기 · DPI 같은 걸 재 보는 용도. 이번에 코드에서 IC 와 DC 가 다른 곳은 **`NtGdiOpenDCW` 4 째 인자 하나** 뿐이었다 (2.4 · 2.6).

`CreateICA` / `CreateDCA` 는 원래 **화면** 도 **프린터** 도 열 수 있는 함수다. 그래서 길 안에 프린터용 준비 (`bLoadSpooler` · UMPD · `pldcCreate` · `IcmInitLocalDC` · `DocumentEventEx`) 가 잔뜩 있다. 화면이면 이걸 다 건너뛰어야 한다. 그 건너뛰기가 **syscall 앞 한 번, 뒤 한 번** 있다.

#### 앞 — `bCreateDCA` 의 이름 판정 (run 문서 1절 차례 2)

1. 장치 이름 (device) 이 있고 `\\.\DISPLAY` 로 시작 → `EBX = 1` + 이름 문자열을 만든다.
2. 아니고 드라이버 이름 (driver) 이 "DISPLAY" (`+0x88 _stricmp`, 대소문자 무시) → **`EBX = 1`, 문자열은 안 만든다** (`+0x9c`).
3. 드라이버 이름이 `\\.\DISPLAY` 로 시작 → `EBX = 1`.
4. 그 밖 → `EBX = 0` (프린터).

- `\\.\DISPLAY` 로 시작하는지 보는 건 11 글자 비교다 (`+0x59` · `+0x1a5`, run 문서 요약).
- `CreateICA("DISPLAY",0,0,0)` 은 **2번** 을 탄다. 포트 · DEVMODE 도 NULL 이라 변환을 건너뛴다 (`+0xa6` · `+0xaf`).
- 그다음 `+0xd4 hdcCreateDCW(0, 0, 0, EBX=1, 1, 0)` — 4 째 = 화면 표시 `1`, 5 째 = IC 표시 `1`.
- `hdcCreateDCW` 안 `+0x76 TEST R9D · +0x79 JZ → +0x12d` — 화면 표시가 1 이라 **안 뛴다.** 그래서 `+0x12d ~ +0x31c` 의 **프린터 문을 통째로 건너뛴다.**

#### 뒤 — 프린터 핸들 0

- syscall 뒤 `+0xdd ~ +0xeb` 가 종류를 본다. `0x10000` 이면 `+0xf1` 로 간다.
- 종류가 `0x10000` 이 아니어도, `+0x31c CMP [RSP+0x58],0 · +0x322 JZ → +0xf1` — 프린터 핸들 칸이 **0** 이면 바로 돌려준다. 화면 길은 프린터를 안 열었으니 늘 0 이다.

비유: 본관 직원이 신청서 맨 위 "DISPLAY" 를 보는 순간, **프린터 창구 (스풀러 · 드라이버 · 색 관리) 는 들르지도 않는다.** 신청이 끝난 뒤에도 "프린터 영수증 (프린터 핸들)" 칸이 비어 있으니 뒷정리 없이 바로 건넨다.

### 2.4 ★ (1 a) `NtGdiOpenDCW` 인자 9 칸 — IC 표시는 4 째 `[정적, 칸 이름 추측]`

| 칸 | `CreateICA("DISPLAY")` / `CreateDCA("DISPLAY")` | 어디서 (`hdcCreateDCW`) | 유저가 syscall 뒤 읽나 |
|---|---|---|---|
| 1 이름 | **NULL** | `+0xc6 MOV RCX,R13` | 안 읽음 |
| 2 DEVMODE | 0 | `+0xc3` | 프린터 길만 |
| 3 포트 이름 | 0 | `+0xbb` · `+0xc0` | — |
| 4 **종류** | **IC `2`** / DC `0` | `+0x8c ~ +0x97` (5 째 인자 → `NEG · SBB · AND 2`) | — |
| 5 **화면 표시** | **`1`** | `+0x2d MOV R12D,R9D` · `+0xb6` | — |
| 6 · 7 · 8 | 0 | (프린터 길은 7 째에 프린터 핸들, 8 째에 UMPD 쪽) | — |
| 9 **출력 포인터** | `&[RBP-0x78]` (처음 0) | `+0x9b` · `+0x9f` | **프린터 길 `+0x413` 한 곳뿐**, 화면 길은 안 읽음 |

이 표에서 읽을 것 셋:

1. **자리가 한 칸 엇갈린다.**
   - `hdcCreateDCW` 가 받은 **4 째 (화면 표시)** 는 `+0x2d MOV R12D,R9D` 로 보관됐다가 `NtGdiOpenDCW` 의 **5 째** 로 나간다.
   - `hdcCreateDCW` 가 받은 **5 째 (IC 표시)** 는 `+0x8c ~ +0x97` 에서 `2` 또는 `0` 으로 바뀌어 `NtGdiOpenDCW` 의 **4 째** 로 나간다.
   - 그래서 에뮬레이터 로그의 "5 째 `1`" 은 IC 표시가 아니라 **"화면" 표시** 다. 에뮬레이터 `CreateDCA` 로그 `(0,0,0,0,1,0,0,0,포인터)` 는 위 DC 줄과 같다 (요청 쪽 로그).
2. **9 째 (출력 포인터) 는 화면 길에서 아무도 안 읽는다.** 그러니 에뮬레이터 stub 은 여기에 아무것도 안 써도 된다.
3. **커널도 1 째가 NULL 이면 2 ~ 9 째를 안 본다.** win32kbase `NtGdiOpenDCW` `+0x36 TEST RCX · +0x39 JZ` → `+0x1b8 ~ +0x1c1` 이 `UserGetDesktopDC(종류 = R9D, 0, 1)` 만 부른다. 1 째가 NULL 이 아니면 (`\\.\DISPLAYn` · 프린터) `+0x13a` 에서 9 째를 꺼내 `+0x171 hdcOpenDCW` 로 넘긴다 — 이번 요청 밖.

비유: 신청서 9 칸 가운데, 화면 도화지 신청에서 그래픽 부서가 실제로 보는 건 **"이름 칸이 비었나" · "보기만 할 건가 (4 째)"** 정도다. 맨 끝 "답장 받을 봉투 (9 째)" 는 프린터 신청 때만 열어 본다.

### 2.5 ★ (1 c) syscall 뒤 보는 것 둘 · NULL 은 하나뿐 `[정적]`

#### 차례 (run 문서 1절 표를 줄임)

| 차례 | 하는 것 | 0 이면 |
|---|---|---|
| 0 | gdi32 `CreateICA` 썽크 → 지연 가져오기 칸 `0x180027600` (첫 부름에 풀림 — `NtProtectVirtualMemory` ×2 는 run86 · run144 꼴 `[추측: 이번에 칸 안 풂]`) | — |
| 1 | gdi32full `CreateICA` — `+0x4 [RSP+0x20] = 1` (IC) | — |
| 2 | `bCreateDCA` 이름 판정 → `EBX = 1` (2.3) | — |
| 3 | `hdcCreateDCW(0, 0, 0, 1, 1, 0)` | 그대로 0 |
| 4 | `+0x79` 화면 → 프린터 문 건너뜀 | — |
| 5 | **`NtGdiOpenDCW`** (`0x10c7`) | **NULL** |
| 6 | 종류 검사 `+0xdd ~ +0xeb` (아니어도 프린터 핸들 0 → `+0xf1`) | 그대로 돌려줌 |
| 7 | `+0xf1 ~ +0x106` — 두 칸 다 0 → `+0x106 MOV RAX,RSI` | — |
| 8 | `GdiTrackHCreate(hdc)` (꼬리) | 0 이면 0 |
| 9 | gdiplus `+0x28c ~ +0x296` | **1** |

- **이 구간의 syscall 은 `NtGdiOpenDCW` 하나** (+ 첫 부름의 지연 가져오기 풀기 `[추측]`).
- 꼬리 `GdiTrackHCreate` (`0x40360`) 는 HDC 를 **그대로** 돌려준다 (`+0x5b`). 하는 일은 GDI 핸들 개수 세기 — `g_GDIHandleCount` 를 `LOCK XADD` 로 하나 올리고 (`+0x14`), 1000 의 배수일 때만 `+0x6e ~ +0x7f GetGuiResources` 를 부른다. 한도 (`.data` `0x2710` · `0x1f40`) 를 넘거나 추적기가 켜졌을 때만 추적 쪽으로 간다.

#### NULL 로 나가는 가지 (run 문서 3절)

| 가지 | 조건 | 화면 길에서 |
|---|---|---|
| `NtGdiOpenDCW` = 0 | `+0xdb JZ → +0x13a` → … → `+0x169 XOR ESI` → 0 | **닿음 (유일)** |
| `+0x7a8 NtGdiDeleteObjectApp` | HDC 를 받은 **뒤** 프린터 쪽 준비가 실패 (`pldcCreate` NULL · DEVMODE · 할당 · `DocumentEventEx = −1` · UMPD 거짓) — 모두 `+0x31c ~` 블록 안 | 안 닿음 |
| `bCreateDCA` NULL (`+0x142` · `+0x185` · `+0x1dd`) | 문자열 · DEVMODE 변환 실패 | 안 닿음 |
| 커널 쪽 (`UserGetDesktopDC +0x1e5` UI 한도 · `GreCreateDisplayDC` 실패 정리 · 세션 화면 장치 없음) | win32k 몫 | 종류 2 (IC) 는 UI 한도 안 탐 |

- 요청이 물었던 `+0x5cc IcmInitLocalDC` · `+0x5ed HANDLE_TO_INDEX` · `+0x614 GdiGetEntry` · `+0x693` · `+0x7a8` 은 **화면 길에서 안 닿는다.** 모두 "종류 ≠ `0x10000` 이고 프린터 핸들 ≠ 0" 인 블록 안이다.

비유: 본관 직원은 받은 번호표에 대해 **"빈 번호표인가?" · "도화지 번호표인가?"** 두 가지만 확인한다. 대장도 안 펴 보고, 색 관리 창구도 안 들른다. 그러니 "못 받았어요 (NULL)" 는 **그래픽 부서가 0 을 줬을 때 하나뿐** 이다.

#### 덤 — `CreateDCA` 와 커널 쪽 갈래 (종류 0 vs 2)

- 커널 `UserGetDesktopDC` 에서 **종류 `0` (DC)** 는 UI 한도 검사 (`+0x129 CanPerformUIOperation` · `+0x157 ValidateHwndEx` · `+0x196 HasUILimit`) 를 거치고, 안 되면 `+0x1e5` 에서 0 `[정적]`.
- **종류 `2` (IC)** 는 `+0x110 MOV EBX,2 · +0x115 CMP · +0x117 JZ` 로 이 검사를 건너뛴다 `[정적: 조건만]`.
- flow-writer 가 더 이었다 (대조 6): IC 는 곧 `+0x7b GreCreateDisplayDC(주 화면 장치, 2, …)` 로 가서 비트 1 이 켜진다 `[정적: 줄 이음]`. DC (종류 0) 는 일반 프로세스면 `+0x106 _GetDCEx(…, 0, 0x80800003)` — **DC 캐시 길** 로 받는다 `[정적: 줄 · 칸 뜻은 추측]`.
- 에뮬레이터 `CreateDCA` = 0 (5 샘플) 은 지금은 stub 기본 대답 0 때문이다 (run 문서).

### 2.6 ★ (1 d) HDC 꼴 — IC 도 DC 도 종류 비트 `0x10000` `[정적]`

run144 해설 2.6 의 핸들 비트를 다시 쓰면:

| 비트 | 뜻 | 화면 HDC |
|---|---|---|
| 0 ~ 15 | 칸 번호 | `< 0x10000` |
| 16 ~ 22 | 종류 | **`0x10000` (DC)** — IC 도 같다 |
| 23 | 스톡 (`0x800000`) | **꺼짐** |
| 24 ~ 31 | 고유 번호 | 아무거나 (0 이 쉬움) |

→ `H = 번호 | 0x10000 | 고유 << 24`.

**왜 IC 도 `0x10000` 이어야 하나 — 단계별로**

1. 유저 쪽 (gdi32 · gdi32full) 에서 IC 와 DC 가 갈리는 곳은 `NtGdiOpenDCW` 4 째 인자 하나다. 그건 **커널만 본다** (2.4).
2. 그 뒤 gdiplus 는 그 HDC 로 `GetDeviceCaps` 를 부른다.
3. `GetDeviceCaps` 는 `+0x59 ~ +0x66` 에서 핸들의 종류 비트 (`& 0x7f0000`) 가 `0x10000` 인지 보고, **아니면 `+0x66 JNZ` 로 대장을 펴 보기도 전에** `pldcGet` 길로 간다 (→ 0 + `TEB+0x68 = 6`, 2.9).
4. 그러니 IC 가 진짜 Windows 에서 제대로 답을 받으려면 **종류 비트가 `0x10000` 이어야 한다.** 다른 종류 번호를 쓸 자리가 유저 쪽엔 없다.

- 표 칸 (대장 한 줄) 은 run144 3절 **그대로** — 이번에 읽은 DC 쪽 함수 다섯 (`GetDeviceCaps` · `CreateCompatibleDC` · `CreateCompatibleBitmap` · `GetDIBits` · `pldcGet`) 이 모두 같은 검사를 한다:
  - `+0xe == 1` · `+0xc == H>>16` · `(+0x8 & ~1) == gW32PID` (0 안 됨) · `+0x10` ≠ 0 · 풀어도 ≠ 0.
  - 실패했을 때 가는 곳만 함수마다 다르다 (run 문서 4절 표).
- **다른 길 HDC** (`NtUserGetDC` · `NtUserGetDCEx` · `NtUserGetWindowDC` · `NtGdiCreateCompatibleDC`) 도 유저 쪽 검사는 **전부 같다** — gdi32 는 HDC 가 어디서 왔는지 모른다. 커널 쪽에선 이 길들이 모두 `GreCreateDisplayDC` 를 부른다 (`_GetDCEx` → `CreateCacheDC` 줄은 안 이음 `[공백]`).

비유: 보기만 하는 도화지든 그리는 도화지든, 번호표의 **종류 자리엔 똑같이 "도화지 (1)"** 가 찍혀야 한다. "보기만 할 거예요" 는 신청서 4 째 칸에만 적고, 번호표엔 안 남는다.

### 2.7 ★ (2) `DC_ATTR` 은 누가 얼마나 잡나 — `0x170` B 슬롯 `[정적]`

요청은 "`DC_ATTR` 을 잡는 쪽이 `HmgAllocateObjectAttr` 가 맞나?" 를 물었다. **아니다.**

| 잡는 함수 (win32kbase) | 슬롯 크기 | 한 쪽에 | 줄 | 쓰는 곳 |
|---|---|---|---|---|
| `HmgAllocateObjectAttr` | **0x18 B** | 170 개 | `+0xee MOV EDX,0xaa` · `+0x10b ADD RSI,0x18` | 영역 쪽 (run144) |
| **`HmgAllocateDcAttr`** (rva `0x2ad8c`) | **`0x170` B** | **11 개** | `+0x18c MOV EDX,0xb` · `+0x1a9 ADD RDI,0x170` | **DC** — `GreCreateDisplayDC +0x205` 가 부름 |

- 유저 쪽 메모장에 **복사되는 건 앞 `0x160` B** (`RestoreAttributes +0x10` 부터). 남는 `+0x160` qword 는 `HmgAllocateDcAttr +0x99` 가 0 으로 둔다.
- 유저 쪽 함수가 읽는 **가장 먼 칸은 `+0x128` qword** (→ `0x130` B 까지). run144 의 "≥ 0x130 B" 는 이 읽는 끝이었고, 실제 슬롯은 `0x170` B 다.

#### 처음 값은 어디서 오나 (커널 쪽 차례)

1. `DCMEMOBJ` (`0x69a6c`) `+0x42 · +0x47 HmgAlloc(0x850, …)` 로 커널 쪽 DC 객체를 만든다.
2. **세션 기본 틀** `[W32GetSessionState()+0x58]+0x90` 에서 `0x1b0` B 를 DC `+0x220` 으로 복사한다 (`+0x74 ~ +0xec`).
3. `GreCreateDisplayDC +0x205 HmgAllocateDcAttr` → 유저 쪽 슬롯 하나.
4. `+0x29d SetupDCAttributes` → `+0x28 RestoreAttributes` 가 앞 `0x160` B 를 유저 슬롯에 넣고, `+0x6b ~ +0x82` 가 대장 칸 `+0x10` 에 **가린 포인터** 를 적는다 (`gCookie` = `[Win32Process+0x118]`, 식은 run144 와 같음).

- 세션 기본 틀의 **내용** (스톡 붓 · 펜 · 글꼴 핸들인지) 은 `[공백]` 이다 → Q278b.

비유: 그래픽 부서는 도화지를 하나 만들 때 **사무실 기본 양식 (세션 기본 틀)** 을 베껴 원본 서류를 만들고, 진열장에 메모장 한 장 (`0x170` B 칸) 을 따로 떼어 앞부분을 옮겨 적는다. 그리고 번호표 대장 줄에 "메모장 위치" 를 **잠근 쪽지** 로 적어 둔다. 영역용 작은 메모지 (0x18 B) 와는 **다른 묶음** 에서 뗀다.

### 2.8 ★ (2) `DC_ATTR` 칸 — 꼭 필요한 값은 없다 `[정적, 이름 추측]`

run 문서 4절 칸 표에서 이번 길과 관계 깊은 줄만:

| 오프셋 | 뜻 `[추측]` | 누가 읽나 | 0 이어도 되나 |
|---|---|---|---|
| `+0x00` 비트 1 | 주 화면 장치 위 DC | `GetDeviceCaps +0x1c0` | **됨** (그러면 지표 전부 syscall). 켜면 게시판이 읽힘 (2.10) |
| `+0x00` 비트 0 | DIB 섹션 골라짐 | `CreateCompatibleBitmap +0x12e` · `SelectObjectImpl` | **0 이어야** (켜지면 다른 길) |
| `+0x90` qword | LDC 포인터 | `pldcGet +0xb3` | **0** |
| `+0x98` dword | 바뀜 표시 | `SelectObjectImpl` | 처음 값 `0x12001f` (`DCMEMOBJ +0x1b8`), 유저는 안 걸림 |
| `+0xa0` · `+0xa8` qword | 붓 · 펜 | `SelectObjectImpl` | 값 검사 없음 — "이전 붓 · 펜" 으로 돌려줌 |
| `+0xf0` · `+0xf8` · `+0x108` | ICM 표시 · 색 공간 · 정보 | `SelectObjectImpl` · `GetDIBits` · `CreateCompatibleDC +0x103` | **0** |
| `+0x128` qword | 글꼴 | `SelectObjectImpl +0x3a3` | 값 검사 없음 |

**새 말 — ICM (색 관리)**: 장치마다 색이 다르게 나오는 걸 맞춰 주는 기능 `[추측: 일반적으로 알려진 설명]`. 칸 `+0xf0` · `+0xf8` · `+0x108` 가 이쪽이고, 0 이 아니면 색 관리 함수를 더 부른다 (예: `CreateCompatibleDC +0x103` 이 원본 DC `+0x108` ≠ 0 이면 `+0x110 IcmEnableForCompatibleDC`). 비유: 도화지에 붙는 **"색 보정 안경"**. 화면 도화지 최소 꼴에선 안 쓴다 (0).

#### 비트 1 — "본점 화면용" 스티커

- **누가 켜나** `[정적]`: 커널 `GreCreateDisplayDC +0x231 CMP RDI,[RCX+0x30]` — DC 의 장치가 **주 화면 장치** 와 같으면 `+0x243 OR [RCX],2`.
- `NtGdiOpenDCW(NULL…)` · `NtUserGetDC*` (`CreateCacheDC +0x83`) · `NtGdiCreateCompatibleDC` (`GrepCreateCompatibleDC +0x126 / +0x256`) 가 모두 `GreCreateDisplayDC` 를 부르니 **같은 규칙** 이다.
- **뜻** `[추측]`: "이 DC 는 주 화면 위에 있으니, 지표 몇 개는 공유 게시판 (`pGdiDevCaps`) 에서 읽어도 된다."
- **유저 쪽에 이 비트를 쓰는 줄은 없다.** 읽기만 한다 (`GetDeviceCaps +0x1c0`).
- **0 이어도 된다** — 그러면 `GetDeviceCaps` 가 모든 지표를 syscall 로 묻는다.

#### 붓 · 펜 · 글꼴 칸 — 값을 검사하는 줄이 없다

- `SelectObjectImpl` 은 이 칸을 읽어 **"이전 값" 으로 돌려주고** 새 값을 쓸 뿐이다. 값이 맞는지 보는 줄은 없다.
- 0 이어도 통과하지만, 그러면 `SelectObject` 가 "이전 값" 으로 0 을 돌려준다 — 앱이 그걸 실패로 읽을 수 있다 `[추측]`.
- 그래서 결론: **0 아닌 값이 *필요한* 칸은 없다.** 다만 "진짜 꼴" 로 채우려면 `+0x98 = 0x12001f`, 붓 · 펜 · 글꼴 = 스톡 핸들 (어느 것인지는 `[공백]`).

비유: 메모장에서 본관이 **꼭 들여다보는** 칸은 많지 않다. "본점 화면용 스티커" 는 없어도 되고 (그러면 매번 그래픽 부서에 물어봄), "지금 쥔 붓 · 펜" 칸은 바꿀 때 "전에 뭐 쥐고 있었지?" 하고 읽어 줄 뿐 검사는 안 한다. **"색 보정 안경" 칸 · "프린터 기록" 칸만은 비어 있어야** 엉뚱한 창구로 안 간다.

### 2.9 ★ (3 a) `GetDeviceCaps` 의 갈림길 — DPI 는 늘 syscall `[정적]`

**새 말 — 장치 능력 (`GetDeviceCaps`)**: DC 하나를 주고 "이 장치의 ○○ 는 얼마?" 를 지표 번호로 묻는 함수 `[추측: 일반적으로 알려진 설명]`. 예: `0x58` · `0x5a` = 가로 · 세로 DPI (`LOGPIXELSX · Y`), `8` · `0xa` = 가로 · 세로 해상도 (`HORZRES · VERTRES`) — 이름 짝은 `[추측]`, 번호 · 줄은 `[정적]`. 비유: 도화지 번호표를 내밀고 **"이 종이 크기가 얼마예요?"** 하고 묻는 창구.

`GetDeviceCaps` (gdi32 `0x17f0`) 안을 차례대로 (run 문서 5절):

1. `+0x2b InitOnceExecuteOnce` — Edge GDI 쪽. 보통 `g_pEdgeGdi == 0` 이라 `+0x41` 에서 안 뛴다.
2. `+0x47 CMP g_systemCallFilterId,5` — 5 면 특별 길 (`+0x419`). 보통은 0 이라 안 탄다.
3. **`+0x59 ~ +0x66` 종류 비트 검사** — HDC 의 `& 0x7f0000` 이
   - `0x10000` 이면 → 다음 (4).
   - `0x660000` 이면 → `+0x318` (`TECHNOLOGY` 만 5).
   - 그 밖이면 → `+0x26b pldcGet` → 0 → **`+0x281 TEB+0x68 = 6`** + 0.
4. **대장 칸 검사** `+0x163` · `+0x175` · `+0x187` · `+0x19b` · `+0x1ba` — 실패하면 `+0xbc TEB+0x68 = 0x57` + 0, syscall 없음.
5. **`+0x1c0 TEST byte [DC_ATTR],2`** — 비트 1:
   - **꺼짐** → `+0x1cb NtGdiGetDeviceCaps` (syscall) — 모든 지표.
   - **켜짐** → `+0x1e8 MOV RDI,[pGdiDevCaps]` → 지표에 따라 게시판 칸을 읽는다 (2.10). 그런데 **`0x58` · `0x5a` 는 점프 표 case 1 (`+0x1c6`) 로 가서 결국 syscall** 이다. 같은 길: `4 · 6 · 8 · 0xa · 0x28 · 0x2a · 0x2c`.

**새 말 — 점프 표 (jump table)**: "지표가 n 이면 여기로" 를 번호 순서대로 적어 둔 표 `[추측: 일반적으로 알려진 설명]`. 코드는 지표 번호로 표를 찾아 그 줄로 뛴다. 여기선 `gdi32.dll` rva `0x1cf0` (byte) · `0x1c80` (dword) 두 장이다 (flow-writer 가 `objdump -s` 로 다시 풀어 맞춤, 대조 4). 비유: 질문 번호별로 **"이 질문은 게시판 몇 번 칸 / 이 질문은 그래픽 부서에 전화"** 라고 적힌 안내표.

- **`NtGdiGetDeviceCapsAll`** (`+0x2f3`, `0x12c0`) 은 종류 ≠ `0x10000` 인 DC (프린터 등) 에서 `pldcGet` 이 기록을 찾았는데 그 캐시가 빌 때만 (`+0x2d9 TEST [LDC+8],0x2000000`). 화면 DC 길에선 안 닿는다.

**새 말 — LDC**: 종류 비트가 `0x10000` 이 아닌 DC (프린터 등) 를 위해 유저 쪽이 따로 두는 기록. `pldcGet` 이 찾고, 찾으면 `DC_ATTR+0x90` 에 그 포인터가 있다 (`pldcGet +0xb3`) `[정적: 읽는 줄 · 이름 뜻은 추측]`. 비유: 프린터 도화지에만 붙는 **"프린터 작업 수첩"**. 화면 도화지엔 없다 (`+0x90` = 0).

비유로 정리: 창구는 번호표를 받으면 ① **"도화지 번호표인가"** (아니면 프린터 수첩부터 찾고, 없으면 "그런 번호표 없음 (6)") ② **"대장에 줄이 맞나"** (아니면 "잘못된 매개변수 (0x57)") ③ **"본점 화면용 스티커가 있나"** (있으면 몇몇은 게시판에서) 순으로 본다. 그런데 **DPI · 해상도 질문은 ③ 에서 스티커가 있어도 그래픽 부서에 전화한다.**

### 2.10 (3 b) 화면 정보 게시판 — 지표 → 칸 `[정적, 이름 추측]`

**새 말 — `pGdiDevCaps` 블록**: 진열장 (PEB `+0xf8` 값) `+0x180000 ~ +0x180090` 자리. 비트 1 이 켜진 DC 의 지표 몇 개를 `GetDeviceCaps` 가 syscall 없이 여기서 읽는다 `[정적]`. run144 해설 2.6 진열장 그림의 "장치 정보 (pGdiDevCaps)" 자리가 바로 이것이다. 비유: 진열장 안쪽에 붙은 **화면 정보 게시판**.

대표만 (전체는 run 문서 5절 표):

| 지표 | 이름 `[추측]` | 게시판 칸 / 상수 |
|---|---|---|
| `0x2` | TECHNOLOGY | `+0x4` |
| `0xc` | BITSPIXEL | `+0x20` |
| `0xe` | PLANES | `+0x24` |
| `0x26` | RASTERCAPS | `+0x34` |
| `0x68` | SIZEPALETTE | `+0x4c` |
| `0x75` · `0x76` | DESKTOPVERTRES · DESKTOPHORZRES | `+0x70` · `+0x6c` |
| `0x4 · 0x6 · 0x8 · 0xa` · `0x28 · 0x2a · 0x2c` · **`0x58` · `0x5a`** | 크기 · 해상도 · 종횡비 · **DPI** | **늘 syscall** |
| 그 밖 (`0x14` · `0x1a` · 홀수 · …) | — | 블록 길에서 `0x57` + 0 |

- 게시판은 `+0x0 ~ +0x90`, 바로 뒤 `+0x94` 는 언어팩 칸 (run86), `+0xb0` 부터 스톡 꽂이 — **같은 쪽 안** 이다.

그래서 run144 해설 2.6 의 진열장 그림에 한 줄이 채워진다:

```
PEB+0xf8 →  +0x0                번호표 대장 (칸 0x18 B)                 ← run144
            +0x180000 ~ +0x90   화면 정보 게시판 (pGdiDevCaps)           ← 이번 (run147)
            +0x180094           언어팩 켜짐 표시                         ← run86
            +0x1800b0           스톡 꽂이 (+ 8*i)                       ← run86 · run144
```

### 2.11 (3 a) DPI 값은 어디서 오나 — 커널 쪽 `[정적]`

에뮬레이터는 커널 본문을 옮기지 않는다. 그래서 여기서 알고 싶은 건 "**무엇과 같아야 하나**" 하나다.

1. `NtGdiGetDeviceCaps` (`0x2e700`) `+0x37 APIDCOBJ(hdc)` — HDC 를 못 잠그면 `+0xf2 EngSetLastError(6)` + 0.
2. 잠그면 장치 (PDEV) 를 들고 `+0x8e GreGetDeviceCapsInternal` (`0x31ecc`).
3. 장치 정보 `GDIINFO` = `[PDEV]+0x848`. `0x58` → `+0x28`, `0x5a` → `+0x2c`.
4. **화면 장치면** (`+0x92 · +0x95 TEST CL,1`) **DPI 가상화** 를 거친다:
   - 스레드 DPI 문맥 (`+0xc8 [스레드+0x190]`) → 없으면 프로세스 (`+0xec [W32PROCESS+0x10c]`) → `(값 >> 8) & 0x1ff` 가 0 이 아니면 그 값.
   - 0 이면 `GDIINFO+0x2c` 이다가, 프로세스 `+0x110` word 가 0 이 아니면 그 값.
   - **`0x58` · `0x5a` 는 둘 다 이 한 값** (`+0x128 ~ +0x132`, `+0x132 MOV EBX,EBP`).

**새 말 — DPI 가상화**: 같은 화면이라도 **묻는 프로그램 (스레드 · 프로세스) 의 DPI 설정에 따라 다른 DPI 를 답해 주는 것** `[추측: 일반적으로 알려진 설명]`. 위 3 · 4 의 줄이 그 모양이다 `[정적]`. 비유: 그래픽 부서가 "종이 촘촘함이 얼마예요?" 라는 질문에, **묻는 가게가 쓰는 돋보기 설정을 보고** 답을 맞춰 주는 것.

그래서:

- **"96 고정" 이 아니다.** 프로세스 · 스레드 DPI 설정을 따라간다.
- 유저 쪽은 값을 **검사하지 않는다.** gdiplus 는 `≤ 0` 이면 96.0 으로 바꿀 뿐이다 (`+0x2e9 COMISS · +0x2f0 · +0x2f5` → `+0x2f7`).
- 그러니 에뮬레이터가 지킬 것: **`0x58` = `0x5a`, 0 아님** 만.
- `0x8` · `0xa` (해상도) 는 DPI 가 100% 가 아닌 가상화면 `EngMulDiv` 로 줄이는 갈래 (`+0x1a9 ~ +0x1e4`) 가 있다 — 100% 면 `GDIINFO+0x10` · `+0x14` 그대로 `[추측: 그 갈래 조건 안 읽음]`.

### 2.12 ★ (3 c) gdiplus 차례 5 ~ 7 — 화방이 그 도화지로 하는 일 `[정적]`

run 문서 6절 표를 줄였다.

| 차례 (`+0x…`) | 하는 것 | NULL 가지 |
|---|---|---|
| `+0x28c ~ +0x2f7` | `DesktopIc` 에 HDC 저장 · `GetDeviceCaps(0x58 · 0x5a)` → `DesktopDpiX/Y` | **`+0x296`** (HDC 0 → **1**). DPI ≤ 0 은 가지 아님 (96.0) |
| `+0x30d ~ +0x341` | `GpMallocEx(0x660)` → **`GpDevice(hdc)`** → `CheckValid<GpDevice>` | **`+0x341`** (아래) |
| `+0x34c ~ +0x518` | `DeviceList` · `EngineDriver` · `DriverGdi` ×2 · `DriverD3D` · `DriverInfo` · `DriverMeta` · `DpBitmap(NULL)` | 아홉 곳 모두 **할당뿐** (`GetStockObject(0)` · `(4)` 는 반환 저장만) |
| `+0x523 ~ +0x563` | `GetSystemMetrics` ×4 (user32) | HDC 와 무관 |
| `+0x56e` | `InitializeForGdiScreen` → `InitializeForGdiBitmap` → **`ExtractPixelFormatFromHDC`** | 가지 아님 |
| `+0x588` | `DesktopDriver->vtbl[+0x10]` (CFG 간접) — `DpDriver` 빈 가상함수 (`ret 0`) `[objdump]` | — |
| `+0x5b5 ~ +0x5e3` | `g_pRemoteSurface = DpBitmap(NULL)` | 할당뿐 |
| `+0x60b` | `InitializeForGdiBitmap` 한 번 더 | 가지 아님 |

#### `GpDevice(hdc)` 안 — 연습장 만들기

1. `+0x6a [this+0x5e0] = hdc` — 받은 화면 HDC 를 기억.
2. **`+0xaa CreateCompatibleDC(hdc)` → `+0xb6 [this+0x18]`** — 화면 HDC 를 본뜬 연습장 DC 를 만든다 (syscall `NtGdiCreateCompatibleDC` `0x104f`).
3. `+0xc2 GetDeviceCaps(2)` (TECHNOLOGY) — 1 이면 `+0x1cd GetDeviceCaps(0xc)` (BITSPIXEL) 를 더 묻는다.
4. `+0xea GetDeviceCaps(8)` · `+0x104 (0xa)` — 해상도.
5. `HeapAlloc` ×2 · `EpScanGdiDci` ×2.
6. `+0x181 ~ +0x1a3`: `[+0x5d8] ≠ 0 · [+0x5d0] ≠ 0 · [+0x18] ≠ 0` 이면 표시값 `0x4c494146 + 0x2a1c02eb` 를 적는다. `CheckValid` 는 이 표시값이 `0x76654431` 인지 본다.

**새 말 — 호환 DC (`CreateCompatibleDC`)**: 어떤 DC 와 **같은 꼴로 만든 메모리 속 DC** `[추측: 일반적으로 알려진 설명]`. 화면에 바로 안 그리고 메모리에 먼저 그릴 때 쓴다. 비유: 화면 도화지를 본뜬 **연습장**.

#### `+0x341` 로 떨어지는 때 — HDC 에 달린 가지

- ① **`CreateCompatibleDC` = 0** (연습장 칸 `[+0x18]` 이 0 이라 표시값이 안 적힘).
- ② **`TECHNOLOGY == 1` 인데 `BITSPIXEL ≤ 8`** 이고, `+0x1e5 PaletteChangeNotification` 뒤에도 팔레트 칸 `[this+0x628]` 이 0 일 때 (`+0x1ea CMP · +0x1f1 JNZ` → `+0x1f7` 무효 표시). — 팔레트 칸 조건은 flow-writer 가 더 찾았다 (대조 3). `PaletteChangeNotification` 이 그 칸을 채우는지는 `[공백]`, "팔레트 칸" 이름은 `[추측]`.
- ③ 할당 실패.

→ 에뮬레이터 규칙 "**`TECHNOLOGY` 가 1 이면 `BITSPIXEL` 은 반드시 `> 8`**" 은 그대로 안전하다. 둘 다 0 도 안전하다 (`TECHNOLOGY` 0 이면 `BITSPIXEL` 을 안 묻는다).

#### `ExtractPixelFormatFromHDC` — 조용히 끝나는 길

**새 말 — 픽셀 포맷**: 화면 한 점을 **몇 비트로, 어떤 색 순서로** 적는지 `[추측: 일반적으로 알려진 설명]`. 비유: 연습장 **모눈 한 칸에 색을 적는 방식**.

1. `+0x3f XOR EBX` — 포맷 = 0 으로 시작.
2. **`+0x4f CreateCompatibleBitmap(hdc,1,1)`** — 1 × 1 그림 한 장.
3. **0 이면 `+0x61 JZ → +0x12f`** — 포맷 0 을 **조용히** 돌려준다 (flow-writer 대조 1 이 결과서의 `+0x18f` 를 `+0x12f` 로 고침).
4. 아니면 `+0x8e GetDIBits` (머리만 — `biBitCount` · `biCompression`) → (`> 8` 이고 `BI_BITFIELDS` 면) `+0xd4 GetDIBits` 한 번 더 (색 마스크).
5. `+0x123 DeleteObject`.

- 포맷은 `DpBitmap` 칸에 저장될 뿐 startup 의 NULL 가지에 안 쓰인다.
- syscall (`NtGdiCreateCompatibleBitmap` `0x1046` · `NtGdiGetDIBitsInternal` `0x107d` · `NtGdiDeleteObjectApp` `0x1020`) 은 **표 칸이 맞을 때만** 나간다.

비유: 화방은 연습장에 1 × 1 모눈 한 장을 붙여 보고 "한 칸에 색을 어떻게 적지?" 를 알아본다. 모눈을 못 받으면 **"모름 (0)" 이라고 적어 두고 그냥 넘어간다.** 개점을 접지 않는다.

### 2.13 ★ 통과 최소 — 그리고 362 샘플의 진짜 이유 `[정적]`

#### 통과 최소 (run 문서 요약 "통과 최소")

**`NtGdiOpenDCW` ≠ 0 · `NtGdiCreateCompatibleDC` ≠ 0** 이면 대장 칸 · `DC_ATTR` 이 **없어도** `+0x5e3` 까지 간다. 단계별로:

1. `CreateICA` — 반환 0 인가 · 종류만 본다 (2.5). 0 아니면 통과 (`+0x296`).
2. `GetDeviceCaps(0x58 · 0x5a)` — 칸이 없으면 0 (+ `0x57` 또는 `6`) 을 줄 뿐. gdiplus 는 0 을 96.0 으로 바꾼다.
3. `GpDevice` — `CreateCompatibleDC` 는 원본 칸 검사가 **실패해도 새 DC 를 그대로 돌려준다** (`+0x115` → `+0x122`). 그래서 `NtGdiCreateCompatibleDC` 가 0 아니면 연습장 칸 `[+0x18]` ≠ 0. `TECHNOLOGY` 가 0 이면 `BITSPIXEL` 을 안 묻는다 → `+0x341` 통과.
4. 할당 아홉 — 할당뿐.
5. `ExtractPixelFormatFromHDC` — `CreateCompatibleBitmap` 이 0 이면 포맷 0 으로 조용히 끝.

- 다만 run 문서는 "**진짜 꼴로 채워 두는 게 N2 뒤에 안전하다**" 고 적는다. `TECHNOLOGY` · `BITSPIXEL` · 해상도는 N2 (`InitImagingLibrary` 뒤) 에서 GDI+ 가 읽을 수 있어서다.

#### 362 샘플 — `GetDeviceCaps` 가 0 인 진짜 이유

1. 에뮬레이터 `GetDC(0)` stub 은 `0x21000` 을 준다 (`nt_user_get_dc.cpp:20`).
2. `0x21000 & 0x7f0000 = 0x20000` — 종류 비트가 **`2`** 다 (DC 는 `1` = `0x10000`).
3. `GetDeviceCaps +0x66 JNZ → +0x25d` — **대장을 펴 보기도 전에** 갈라진다.
4. `pldcGet` 이 0 → **`+0x281 TEB+0x68 = 6`** + 0.
5. → 362 샘플의 0 은 "대장 칸이 없어서 (`0x57`)" 가 **아니라** "도화지 번호표가 아니어서 (`6`)" 다.

- 같은 HDC 로 `SelectObject` · `GetTextExtentPointW` 도 실패한다 (run 문서 PE_emulator 표).
- `nt_gdi_create_compatible_dc.cpp` 의 `0x20c00` 도 같은 종류 비트 `2` 라 꼴이 틀렸다. 다만 gdiplus 는 `GpDevice +0xb6` 에서 "0 아님" 만 보니 지금도 통과한다.

비유: 에뮬레이터 그래픽 부서가 준 번호표엔 종류 자리에 "도화지 (1)" 가 아니라 "2" 가 찍혀 있었다. 그래서 창구 직원은 **대장도 안 펴 보고** "프린터 수첩부터 찾아볼게요… 없네요, 그런 번호표 없음 (6)" 하고 돌려보냈다. run144 에서 본 "대장 줄이 없음 (`0x57`)" 은 번호표 종류 자리가 맞았을 때의 이야기였다.

#### "`0x400` 배수" 를 지키고 싶다면 (flow-writer 보탬)

- 에뮬레이터 주석 (`nt_user_get_dc.cpp:7` · `nt_gdi_create_compatible_dc.cpp:8`) 의 "`0x400` 배수" 근거는 **nt 객체 핸들 표** 규칙이다 — run144 가 영역 핸들에 대해 적은 것과 같은 말 (run144 해설 2.10 "하나 더").
- GDI 핸들은 "핸들 비트" 규칙 (종류 = 16 ~ 22 비트) 을 따라야 한다. `0x400` 배수를 지키고 싶으면 **번호 칸 (0 ~ 15 비트) 만** `0x400` 배수로 — 예 `0x00010c00` `[정적: 비트 규칙과 비교]`.

### 2.14 PE_emulator 판정 — 맞음 · 틀림 (run 문서 "PE_emulator 쪽")

고칠지는 사용자 · PE_emulator 결정이다. flow-writer 는 `../PE_emulator-BD/…/src/gui/` 를 줄 번호만 훑었다 (읽기만).

| 쓰는 곳 | 판정 | 한 줄 이유 |
|---|---|---|
| `nt_gdi_init2.cpp` (표 칸 · DevCaps 블록) | **모자람 (run144 와 같음) + DC 칸 한 줄** | 블록은 게시판 자리를 품는다. 비트 1 을 켤 때만 게시판을 채우면 됨, **안 켜면 0 그대로 안전** |
| `nt_gdi_create_rect_rgn.cpp` | **run144 판정 그대로, DC 와 무관** | 0x18 B 칸은 영역뿐 — DC 는 `0x170` B 로 따로 |
| 새 `NtGdiOpenDCW` | **만들 것** | 1 째 NULL · 4 째 `0`/`2` → 종류 `0x10000` HDC. **0 아니면 됨**, 9 째에 안 써도 됨 |
| 새 `NtGdiGetDeviceCaps` | **만들 것** | 기본 대답 0 이어도 `GdiplusStartup` 은 통과. 다만 N2 뒤에 읽힐 수 있음 |
| `nt_user_get_dc.cpp` (`0x21000`) | **틀림 (종류 비트)** | 2.13 — `번호 \| 0x10000` + 표 칸 + `DC_ATTR` 필요 |
| `nt_gdi_create_compatible_dc.cpp` (`0x20c00`) | **gdiplus 엔 지금도 통과, 꼴은 틀림** | 종류 비트 `2` — 다음 일에서 같은 실패 |
| 새 `NtUserGetDCEx` · `NtUserGetWindowDC` | **만들 것 — `NtUserGetDC` 와 같은 꼴** | user32 쪽은 win32u 로 점프하는 썽크뿐 `[objdump]`. 반환 0 이면 그대로 0 (메시지 상자 12 · `GetWindowDC` 288) |

**stub 이 채울 최소 목록** — run144 목록 1 ~ 7 에 이어 8 ~ 16 (run 문서에 전부). 뼈대만:

- 8 · 9 · 10: **DC 번호표** (종류 `0x10000` · 스톡 0 · 고유 0) + **대장 줄** (`+0xe = 1`, 주인 PID 정확히, 가린 메모장 위치) + **메모장 `0x170` B** (0 으로 채우고, `+0x90` · `+0xf0` · `+0xf8` · `+0x108` 은 **반드시 0**).
- 11 · 12: `NtGdiOpenDCW` · `NtGdiCreateCompatibleDC` — **0 아님 필수**.
- 13: `NtGdiGetDeviceCaps` — `0x58` = `0x5a`, **`2 → 1` 이면 `0xc` 는 반드시 `> 8`**.
- 14: 비트 1 을 켰다면 게시판 칸. **안 채우려면 비트 1 을 0 으로.**
- 15: `NtGdiCreateCompatibleBitmap` · `NtGdiGetDIBitsInternal` · `NtGdiDeleteObjectApp` 는 안 만들어도 됨.
- 16: `g_systemCallFilterId` — `5` 를 주면 `GetDeviceCaps` 가 다른 길 (`+0x419`).

**그 뒤 벽 한 줄**: `InternalGdiplusStartup +0x62b InitImagingLibrary` (0 이면 `+0x632` → 1) — **N2 (Q278b)**, DC 일 아님.

### 2.15 앞 run 고쳐 읽기 — run144

틀린 줄은 없고, 종류 비트가 다를 때의 길 · `DC_ATTR` 크기가 좁혀진 것이라 run 문서는 "정정" 이 아니라 "**고쳐 읽기**" 로 run144 문서 맨 위에 한 줄 넣었다. 이 글도 같은 뜻으로 [run144 해설](run144-gdi-startup-해설.md) 맨 위에 한 줄 넣었다.

| run144 | 전에 | 이제 |
|---|---|---|
| 6절 · 해설 2.13 | `GetDeviceCaps(DC)` — 표 칸이 0 이면 0, `TEB+0x68 = 0x57` | **종류 비트가 `0x10000` 일 때의 길.** 종류 비트가 `2` (`0x21000` · `0x20c00`) 면 표 앞에서 갈라져 `TEB+0x68 = 6` + 0 |
| PE_emulator 표 · 해설 2.14 | "DC 길 `0x57` / 0" | 같음 (위) |
| 3절 칸 표 · 해설 2.6 · 2.13 | `+0x10` … DC → `DC_ATTR` (≥ 0x130 B) | 맞다. 잡는 쪽은 **`HmgAllocateDcAttr` (슬롯 `0x170` B)** — `HmgAllocateObjectAttr` (0x18 B) 은 영역 쪽 |
| 다음 요청 후보 · 해설 4절 Q278 | N1 (바로 다음 벽) | **N1 닫힘 (Q278a → run147)**, N2 ~ N5 는 Q278b |

### 2.16 flow-writer 대조 — 결과서와 다르게 적은 곳 (짧게)

run 문서 "flow-writer 대조" 는 **어긋난 곳 셋 + 보탬 넷** 이다. 이 글은 run 문서 값을 따랐다.

- **어긋남 1**: `ExtractPixelFormatFromHDC +0x61 JZ` 가 가는 곳은 결과서의 `+0x18f` 가 아니라 **`+0x12f MOV EAX,EBX`**. 뜻 ("포맷 0 을 조용히 돌려줌") 은 같다 (2.12).
- **어긋남 2**: `HmgFreeDcAttr` call 은 `GreCreateDisplayDC +0x2f8` 이 아니라 **`+0x2fb`** (ret `+0x300`). `+0x2f8` 은 인자 넣는 줄. 같은 자리.
- **어긋남 3**: `+0x341` 조건 ② 에 **팔레트 칸 `[this+0x628]` 이 여전히 0 일 때** 가 붙는다 (2.12).
- **보탬 4**: 점프 표 case 줄을 `objdump -s` 로 다시 풀어 맞춤 — 결과서 표와 일치 (2.9 · 2.10).
- **보탬 5**: `hdcCreateDCW` 성공 끝 `+0xf1 ~ +0x106` · NULL 길 `+0x13a ~ +0x16b` 의 칸들이 화면 길에서 0 인 이유 (2.5).
- **보탬 6**: `UserGetDesktopDC` 의 종류 0 길 → `_GetDCEx` (2.5 덤).
- **보탬 7**: `_DC_ATTR` 형 이름이 PDB 에 있다 · `SelectObjectImpl +0x26a` 는 `& 3`, `& 0x11` 은 `+0x55f` · `+0x610`.

### 2.17 정리 — 비유로 한 번 더

1. 고급 화방 (gdiplus) 은 개점 체크리스트 5번에서 본관에 **"화면용, 보기만 하는 도화지 한 장"** 을 부탁한다.
2. 본관은 신청서 맨 위 "DISPLAY" 를 보는 순간 **프린터 창구를 건너뛰고**, 그래픽 부서에 신청서 (`NtGdiOpenDCW`) 한 장만 낸다. "보기만 할 거예요" 는 4 째 칸에, "화면이에요" 는 5 째 칸에 적는다. 답장 봉투 (9 째) 는 안 연다.
3. 번호표를 받으면 **"빈 번호표인가 · 도화지 번호표인가"** 만 보고 화방에 건넨다. 실패는 "그래픽 부서가 0 을 줬다" 하나뿐.
4. 그래픽 부서는 번호표를 줄 때 진열장에 **메모장 한 장 (`0x170` B)** 을 떼어 기본 양식을 옮겨 적고, 주 화면이면 **"본점 화면용" 스티커** 를 붙인다. 대장 줄엔 메모장 위치를 잠근 쪽지로 적는다.
5. 화방은 그 도화지로 **DPI 를 두 번** 묻는다. 스티커가 있어도 **DPI 는 늘 그래픽 부서에 전화** 하고, 그래픽 부서는 묻는 가게의 돋보기 설정에 맞춰 **가로 · 세로 같은 값** 을 답한다.
6. 그다음 그 도화지를 본떠 **연습장** 을 만들고, 1 × 1 모눈으로 "색 적는 방식" 을 알아본다. 모눈을 못 받아도 그냥 넘어간다.
7. 그래서 에뮬레이터는 **"신청서 두 장 (도화지 · 연습장) 에 0 아닌 번호표"** 만 주면 이 구간을 넘는다. 다만 번호표 종류 자리엔 **"도화지 (1)"** 를 찍어야, 나중에 DPI 를 물을 때 "그런 번호표 없음 (6)" 을 안 듣는다.
8. 그 뒤 벽은 **그림 파일 도구 준비 (`InitImagingLibrary`)** — Q278b.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 0d560df -- runs/03_sprint/flow.md`)

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | gdi32 · gdi32full · gdiplus · win32kbase · win32kfull 줄에 run147 없음 | 다섯 줄에 run147 보탬 (다시 analyze 없음, `-readOnly` export) + 근거 줄 (run147) 한 줄 (test.exe 는 안 올림 · `GdiSharedHandleTable` (null) `[관찰]`) |
| 3장 gdi32 하위 절 (run86) | `pGdiDevCaps` 는 "`+0x180000`" 이라는 위치만 | 한 줄 보탬: **`+0x180000 ~ +0x180090` 이 `pGdiDevCaps` 지표 블록** — 비트 1 이 켜진 DC 일 때만 읽고, `8 · 0xa · 0x58 · 0x5a` 는 그래도 syscall `[정적]` |
| 3장 GDI 공유 핸들 표 하위 절 (run144) | 나무 `CreateICA("DISPLAY")` 줄 "← 다음 벽 (Q278)" · 남은 것 "정적 Q278" | "← 다음 벽 (**Q278a → run147**, 아래 하위 절)" + 한 줄 (다음 벽은 정적으로 닫힘 · `0x57` 은 종류 비트 `0x10000` 일 때) + 남은 것 "N1 은 Q278a → run147, 나머지는 **Q278b**" |
| 3장 대화상자 하위 절 (run146) | 남은 것 "동적 — Q273 세션에 얹을 꼴" | "동적 **Q273**" (번호 채움) |
| **3장 새 하위 절** | — | "**화면 DC 하나가 서는 길**" — 비유 (도화지 대여증) · 나무 · `[정적]` 여섯 (syscall 하나 · IC = DC 종류 비트 · `DC_ATTR` 0x170 B · DPI 늘 syscall · 통과 최소 · 에뮬레이터 종류 비트 `2`) |
| 4장 | run144 정적 줄 "Q278 (N1 바로 다음 벽)" · run144 PE_emulator 줄 "DC 길 `0x57`" · run146 동적 줄 "번호는 오케스트레이터가 붙임" | run144 정적 N1 **해결됨 → run147, Q278a** 표시 · run144 PE_emulator 줄에 "run147: 이건 종류 비트 `0x10000` 일 때" 한 마디 · run146 동적 → **Q273** · **Q278a 해결됨** 줄 하나 · **run147 새로** 줄 셋 (동적 Q277 · 정적 Q278b · PE_emulator 쪽) |
| 5장 | — | run147 두 줄 (근거 run · 요청서 번호 표) |

- **그림 (1장) 은 안 바뀌었다.** test.exe 초기화 밖 이야기라서다.
- 짐 지도 (`baggage.md`) 도 안 바뀌었다. **새 짐 칸 없음** — 이번 길은 test.exe 초기화 밖 (gdi32 · gdiplus 안 올림) 이고 PEB `+0xf8` 은 test.exe 사진에서 (null) 이다.
- 태그 변화로 보면: run144 까지 `[공백]` 이던 "`DC_ATTR` 을 잡는 쪽 · 크기" 와 "`pGdiDevCaps` 블록의 칸" 이 `[정적]` 이 됐다. 칸 **이름** 은 그대로 `[추측]`.

### 큰 그림에서 어떻게 이어지나

- **run86 → run144 → run147 이 진열장 하나로 이어졌다.**
  - run86: 그래픽 부서가 진열장을 **건다** (`NtGdiInit2` → PEB `+0xf8`). 게시판 위치 (`+0x180000`) 만 알았다.
  - run144: 진열장 앞쪽이 **번호표 대장** 이고, 대장이 비면 화방 개점 체크리스트 4번에서 넘어진다.
  - run147: 체크리스트 5 ~ 7번. 도화지 번호표의 **종류 자리** · 메모장 크기 · **게시판 칸** 이 나왔다.
- **PE_emulator 쪽으로는**: 362 샘플의 원인이 "대장 칸 없음" 이 아니라 **"번호표 종류 자리가 틀림"** 으로 좁혀졌고, 화면 DC 최소 꼴 (목록 8 ~ 16) 이 나왔다. run144 목록 1 ~ 7 (영역 · 스톡) 과 합치면 **영역 · DC 표 칸을 한 번에** 지을 수 있다 (에뮬레이터가 원했던 것).
- **아직 화방이 문을 연 건 아니다.** 5 ~ 7번을 넘기면 바로 `InitImagingLibrary` (N2) 가 기다린다 → Q278b.
- run142 의 메시지 상자 12 샘플은 **N1 만으로는 안 풀린다** — 글 폭 (N3) 이 남았다 → Q278b.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q277** (동적, 같은 VM GUI 세션 — 미룸)

test.exe 로는 못 본다 (gdi32 · gdiplus 를 안 올린다). gdiplus 를 부르는 GUI 프로세스가 필요하고, 고르기는 사용자 결정이다. 결과서 "동적으로 확인할 것" 1 ~ 7 이 Q277 에 출처로 붙었다.

1. `gdi32full!hdcCreateDCW+0xd0` 의 `rax` — 진짜 HDC 비트 (종류 `0x10000`? 스톡? 고유?), `[rsp+0x58]` = 0?
   - 왜 못 알았나: 정적은 "무엇과 비교하나" 만 보여 준다. 진짜 값은 실행 때만.
2. 그 HDC 의 대장 줄 (24 B) 과 풀린 `DC_ATTR` `0x170` B 덤프 — 특히 `+0x0` 비트 0 · 1, `+0x90` · `+0x98` · `+0xa0` · `+0xa8` · `+0xf0` · `+0xf8` · `+0x108` · `+0x114` · `+0x128` · `+0x160`.
3. 게시판 (`PEB+0xf8 + 0x180000`) `0x98` B 덤프, `GetDeviceCaps` 끝의 `eax` (지표 `0x58` · `0x5a` · `8` · `0xa`).
4. `CreateCompatibleDC +0x103` · `+0x115`, `CreateCompatibleBitmap +0x12e` · `+0x1e0`, `GetDIBits +0x133`.
5. gdiplus `+0x293` · `+0x33f`, `GpDevice +0x1a3` (표시값 `0x76654431`?) · `+0x1ea` (`[rbx+0x628]`), `ExtractPixelFormatFromHDC +0x12f` (`ebx` — `0x22009`? `[추측]`).
6. 커널: `NtGdiOpenDCW+0x1c1` (`r9d`) · `UserGetDesktopDC` 어느 갈래 · `GreCreateDisplayDC +0x243` 등 · **세션 기본 틀 `0x1b0` B** (스톡 핸들 값).
7. `GdiTrackHCreate+0x14` 의 `g_GDIHandleCount` 처음 값 (이번 길과는 무관).

### 정적으로 남은 것 → **Q278b**

1. **세션 기본 `DC_ATTR` 틀** `[W32GetSessionState()+0x58]+0x90` (0x1b0 B) 을 채우는 곳 — 붓 · 펜 · 글꼴이 어느 스톡 핸들인지, `+0x0` · `+0xf0` · `+0x108` 처음 값.
   - 왜 못 알았나: win32k 안쪽이고, 이번엔 "복사하는 줄" 까지만 읽었다.
2. `GrepCreateCompatibleDC` (`0x14950`) 본문 — 연습장 DC 의 `DC_ATTR+0x0` 비트, `+0x2a4 vSetDpiScaling`.
3. `GDIINFO` (`PDEV+0x848`) 와 게시판 (`+0x180000 ~ +0x180090`) 을 **채우는 곳** — `GdiProcessCallout` 쪽 (run86 · Q163 ① 과 겹침).
4. DPI 칸 `[W32PROCESS+0x10c / +0x110]` · `[스레드+0x190 / +0x198]` 이 써지는 곳 (`NtUserSetProcessDpiAwarenessContext` 길), `EngMulDiv` 갈래 조건.
5. `_GetDCEx` → `CreateCacheDC` 줄 · `UserGetDesktopDC` UI 한도 본문 — 메시지 상자 12 · `CreateDCA` 5, 대조 6 의 `_GetDCEx(…, 0x80800003)` 갈래.
6. 주 화면 판정 `[session+0xe338]→+0x30` 이 세워지는 곳, `hdcOpenDCW` (프린터 · `\\.\DISPLAYn`).
7. 다음 요청 후보: **N2** `InitImagingLibrary` · `SysColorNotify` · `InitSystemFontsDirs` · `GetGpInstalledFontCollection` (그 뒤 벽). **N3** 글 폭 (`DC_ATTR+0x114` · `+0x128`) — 메시지 상자 12 는 N1 만으론 안 풀림. N4 · N5 는 그대로.
8. (새로) `GetDeviceCaps` 의 `g_systemCallFilterId == 5` 길 (`+0x419`) 과 user32 `GetDC +0x0` 의 같은 검사 · `GpDevice [this+0x628]` 을 `PaletteChangeNotification` 이 채우는 조건 (낮음).

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- 칸 이름 (`DC_ATTR` 칸 · 게시판 칸 · `NtGdiOpenDCW` 인자 칸) 과 지표 SDK 이름 짝은 `[추측]` — PDB 에 `_DC_ATTR` 형 이름은 있지만 칸 정의는 안 봤다. 오프셋 · 줄만 `[정적]`.
- 비트 1 의 **뜻** ("주 화면 위 DC") 은 `[추측]`, 켜는 줄은 `[정적]`.
- 첫 부름의 지연 가져오기 풀기 (`NtProtectVirtualMemory` ×2) 는 `[추측: 이번에 칸 안 풂]`.
- 해상도 (`0x8` · `0xa`) 의 `EngMulDiv` 갈래 조건 `[추측: 안 읽음]`.
- 디버거 영향: 이번에 읽은 줄에서 디버거 여부를 보는 줄은 없었다 `[정적: 읽은 줄 안에서]`.
- 하네스 쪽: Ghidra export 스크립트가 같은 이름 다른 함수의 `.asm` 을 덮을 수 있다는 주의 (결과서 "스크립트 문제") 는 plan 의 하네스 제안 묶음에 보탰다 (오케스트레이터 · 사용자 몫).

### 짐 지도

- 새 짐 칸 없음, 2장 숫자 그대로일 것 (run 문서 "짐 지도 반영 거리"). `baggage.md` 는 안 고쳤다.

---

## 5. 더 읽기

- run 문서: [`run147-gdi-display-dc.md`](run147-gdi-display-dc.md)
  - 1절 (`CreateICA` 차례) · 2절 (인자 9 칸) · 3절 (NULL 가지) · 4절 (HDC · 표 칸 · `DC_ATTR` 칸 표) · 5절 (`GetDeviceCaps` 갈래 · 지표 표 · DPI 값 출처) · 6절 (gdiplus 차례 5 ~ 7) · "PE_emulator 쪽" (최소 목록 8 ~ 16) · "flow-writer 대조" · "앞 run 고쳐 읽기"
- 요청서: [`_workspace/sast/145_gdi-display-dc.md`](../../_workspace/sast/145_gdi-display-dc.md)
- 결과서: [`_workspace/sast/145_gdi-display-dc_result.md`](../../_workspace/sast/145_gdi-display-dc_result.md) — "먼저 읽는 한 줄 답" 1 ~ 14, 표 (1a)(1b)(1c)(2a)(2b)(3)(4), "PE_emulator 쪽", "win32k 몫 남음", "동적으로 확인할 것"
- 원 요청: `../PE_emulator/_workspace/05_tuning_planner/367_internals-gdi-display-dc.md`
- flow.md (`git show 0d560df -- runs/03_sprint/flow.md`):
  - 3장 "화면 DC 하나가 서는 길" 하위 절 (새로)
  - 3장 "GDI 공유 핸들 표 · `GdiplusStartup` 첫 가지 · 배경 붓 검사" 하위 절 (run144) 의 run147 줄
  - 3장 gdi32 · gdi32full 시작 함수 하위 절 (run86) 의 `pGdiDevCaps` 줄
  - 4장 Q278a 해결됨 · run147 새로 셋
- 앞 해설: [run86](run86-gdi32-init-해설.md) 2.6 · 2.10 · [run85](run85-user32-init-해설.md) 2.5 (DPI) · [run142](run142-user32-connect-해설.md) 2.8 · [run144](run144-gdi-startup-해설.md) 2.3 ~ 2.6 · 2.13 · [run146](run146-dialog-window-해설.md) 2.10 (대화상자 길의 GDI 부름 — `CreateDlgFont` 는 여기서도 안 닫힘)

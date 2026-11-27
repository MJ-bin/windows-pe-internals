# run153 gdi-font-dc — 해설

> 이 글은 [run153 문서](run153-gdi-font-dc.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q278b** — Q278 남은 몫 "메시지 상자 글 폭 · 세션 기본 `DC_ATTR` 틀 · `GDIINFO` / `pGdiDevCaps` · 필터 값 5 · gdiplus 시작 뒷몫" (`_workspace/plan.md`, 이번에 **끝 (run153)**)
  - PE_emulator 380 묶음 **정적 차례 ④ 앞 몫** (원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md`) · `367_internals-gdi-display-dc.md` 의 "다음" 몫.
  - 사용자 결정 2026-11-27 (PE_emulator D179). **정적만.**
  - 결과서 판정 **분석성공** — 물음 1 ~ 5 · 6 ② ~ ④ 를 줄 근거로 닫았다. 6 ① (N5 `NtQIP(0x25)` 를 누가 부르나) 만 반쯤 (`[공백]`).
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q277** (같은 VM GUI 세션)
    - 디버거 없이 떠 올 것 → 380 **2 단계 VM 수집 스크립트** 재료
    - 정적 → 결과서 "다음 요청 후보" (대기열 자리는 오케스트레이터가 정함)
- 근거:
  - run 문서 [`run153-gdi-font-dc.md`](run153-gdi-font-dc.md) (가장 중요)
  - 요청서 [`_workspace/sast/150_gdi-font-dc.md`](../../_workspace/sast/150_gdi-font-dc.md) · 결과서 [`150_gdi-font-dc_result.md`](../../_workspace/sast/150_gdi-font-dc_result.md)
    - **처음 analyze 없음.** gdiplus · gdi32 · gdi32full · user32 · win32kfull · win32kbase 여섯 프로젝트에 `-readOnly` export · refs 만 했다.
    - ntoskrnl 은 있던 `.asm` 을 읽기만, kernelbase · `apisetschema.dll` · win32kbase `.data` 값은 PE 파일을 직접 읽었다 (`[objdump]` 표시).
  - flow.md 변경: docs 커밋 `421c585` (`git show 421c585 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 user32 · gdi32 · gdiplus 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`).
   - 사진의 PEB `GdiSharedHandleTable` 도 (null) 이다 `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:1779`, `_P2.txt:2489`).
   - 그래서 이번 이야기도 [run144](run144-gdi-startup-해설.md) · [run147](run147-gdi-display-dc-해설.md) 과 같은 자리 — **"`main` 뒤에 메시지 상자를 띄우거나 gdiplus 를 켜는 프로그램"** 의 일이다.
2. **"12 샘플" 같은 숫자는 에뮬레이터 것이다.**
   - "메시지 상자 12 샘플이 `NtUserGetDCEx(0,0,3)` 기본 대답 0 ×2 로 끝남" 은 PE_emulator 로그다. 진짜 Windows 를 본 게 아니라 `[관찰]` 이 아니다.
   - 그리고 이번엔 결과서도 flow-writer 도 `../PE_emulator*` 를 **안 열었다.** PE_emulator 판정 (2.13) 은 run147 때 읽은 상태 기준이다.
3. **오프셋 읽는 법** (앞 해설과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문 "`+0x119 pcfLocateCFONT`" · "`+0x155 TEST`" 처럼 명령 이름과 같이 적힌 숫자 = **함수 안 그 줄** (call 자리 포함).
   - "`+0x153 JZ → +0x69f`" 는 그 줄과 점프가 가는 곳. 가는 곳도 **함수 시작 기준** 이다.
   - 콜스택으로 본 적은 없다 (정적만).
4. **값은 하나도 안 봤다.**
   - 이번에 본 건 "**코드가 어느 칸을 읽고, 무엇과 비교하고, 0 이면 어디로 가나**" 다.
   - 세션 기본 틀의 값 (`+0x88 = 1` 같은 것) 은 "**코드가 써 넣는 상수**" 를 읽은 것이다. 실행 중에 바뀌었는지는 모른다.
5. **칸 이름 · SDK 이름은 짐작이다.**
   - `tmHeight` · `tmAveCharWidth` · `tmPitchAndFamily` 같은 `TEXTMETRIC` 칸 이름, `WHITE_BRUSH` · `BLACK_PEN` · `SYSTEM_FONT`, `DCX_WINDOW | DCX_CACHE` 같은 이름 짝은 `[추측]` 인 곳이 많다.
   - **오프셋과 그 칸을 읽는 줄은 `[정적]`**, 이름 · 뜻은 run 문서 표시를 그대로 따른다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- **DPI** = 화면 확대 비율 → [run85 해설](run85-user32-init-해설.md) 2.5 · **`psi` / `gpsi`** (서버 정보판 화살표) → 같은 해설 2.5
- **run86 에서 푼 것** → [run86 해설](run86-gdi32-init-해설.md)
  - gdi32 = 그림 도구 건물 **앞 동**, gdi32full = **본관** → 2.1
  - GDI 공유 메모리 (PEB `+0xf8`) = **그림 도구 진열장**, **DC** = 도화지 + 전용 도구 세트, **스톡 객체** = 처음부터 있는 공용 붓 · 펜 → 2.10
- **run142 에서 푼 것** → [run142 해설](run142-user32-connect-해설.md) 2.8 — 메시지 상자가 `NtUserGetDCEx(0,0,3)` 로 받는 **DC**, "GDI 답이 0 이면 조용히 0"
- **run144 에서 푼 것** → [run144 해설](run144-gdi-startup-해설.md)
  - **GDI+ (gdiplus)** = 고급 화방, `GdiplusStartup` = 화방 개점 → 1절
  - **핸들** = 번호표, **공유 핸들 표** = 진열장 앞쪽 **번호표 대장**, 칸 = 대장의 한 줄 → 2.3 · 2.6
  - **핸들 비트** (23번 = 스톡) · 칸 24 B → 2.6, **스톡 객체 꽂이** (`+0x1800b0 + 8*i`) → 2.11
  - **`DC_ATTR`** = 도화지 옆 **메모장** → 2.13 "새 말 — `DC_ATTR`"
  - **"표가 비면 syscall 없이 실패"** 하는 함수 표 → 2.13
- **run147 에서 푼 것** → [run147 해설](run147-gdi-display-dc-해설.md)
  - **HDC 종류 비트 `0x10000`** → 2.6, **`DC_ATTR` `0x170` B 슬롯 · 사무실 기본 양식 (세션 기본 틀)** → 2.7
  - **`GetDeviceCaps`** (장치 능력 묻기) → 2.9, **`pGdiDevCaps` 블록** = 진열장 안 **화면 정보 게시판** → 2.10
  - **호환 DC** = 연습장 → 2.12, **ICM** = 색 보정 안경 → 2.8

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, PEB = 가게 **안내판**, 커널 = **관리사무소**, syscall = **신청 창구**.
- gdi32 = 그림 도구 건물 **앞 동**, gdi32full = **본관**, win32k = 관리사무소 **그래픽 부서**, gdiplus = **고급 화방**.
- GDI 공유 메모리 (PEB `+0xf8`) = **그림 도구 진열장**. 앞쪽이 **번호표 대장**, 뒤쪽 `+0x180000 ~` 이 **화면 정보 게시판**.
- 핸들 = **번호표**. DC = **도화지**. `DC_ATTR` = 도화지 옆 **메모장**. 세션 기본 틀 = 그래픽 부서의 **사무실 기본 양식**.
- 메시지 상자 = 창 담당 건물이 직원 앞에 세우는 **작은 알림판** (run142).
- 이번에 새로 붙는 그림:
  - **글 폭 재기** = 알림판 크기를 정하려고 **글씨 한 칸이 몇 눈금인지 자로 재는 일**.
  - **CFONT (글꼴 사본)** = 본관 책상 서랍에 넣어 둔 **"글꼴 자"**. 서랍에 맞는 자가 있으면 그래픽 부서에 안 묻고 바로 잰다.
  - **되돌이** = 서랍이 비었거나 잠겼을 때 **그래픽 부서에 전화해서 재 달라고 하는 길** (`NtGdiGetTextMetricsW` · `NtGdiGetTextExtent`).
  - **필터 값** = 가게 입구에 붙은 **"출입 제한 등급" 딱지**. 일반 가게는 0 (제한 없음).
- run 문서 "결과" 비유는 이름이 조금 다르다 — HDC = "도화지 **대여증**", 공유 핸들 표 = "**출입증** 대장", 커널 = "**본점** 에 전화" (run 문서는 이 비유를 `[추측: 비유]` 로 둔다). 이 글은 앞 해설 그림 (번호표 · 번호표 대장 · 메모장 · 그래픽 부서) 에 맞춰 부른다. "글꼴 자 서랍" 은 run 문서 비유를 그대로 가져왔다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| 1 메시지 상자는 글 폭을 어떻게 재나 | `UserGetCharDimensionsEx` → gdi32full **`GdiGetCharDimensions` → `pcfLocateCFONT` → `bGetTextMetricsWInternal`** `[정적]` (2.3) |
| 1 0 이 되는 곳은 | **셋뿐** — 표 검사 실패 (syscall 없음) · `NtGdiGetTextMetricsW` = 0 · (가변 폭일 때) `NtGdiGetTextExtent` = 0 `[정적]` (2.3) |
| 1 글꼴 사본 길이 막히면 | **조용히 NULL → syscall 되돌이로 지나간다.** 에뮬레이터는 사본을 안 만들어도 된다 `[정적]` (2.4) |
| 1 `NtUserGetDCEx(0,0,3)` 이 0 이 되는 조건 | 일반 프로세스 · 화면 있음이면 **없다.** 12 샘플의 0 은 stub 탓 `[정적]` (2.6) |
| 2 세션 기본 `DC_ATTR` 틀은 | `InitializeGre +0xb8 → InitializeDefaultDCAttributes`. `+0x88 = 1` · `+0x40 = 0x4b` · 붓 · 펜 · 글꼴 = 스톡 0 · 7 · 13 `[정적]` (글꼴은 `[추측: 오프셋 일치]`) (2.7) |
| 2 `GDIINFO` · 게시판은 | `GreUpdateSharedDevCaps → vGetDeviceCaps` 가 `GDIINFO` (`PDEV+0x848`) 에서 옮겨 적는다 `[정적]` (2.8) |
| 3 필터 값 5 는 어디서 | `EPROCESS+0x6e8` (부모가 준 Win32k 필터 속성) 하위 4 비트. **일반 프로세스는 0** `[정적]` + `[objdump]` (2.9) |
| 4 win32k 몫 2 · 4 · 6 | 연습장 DC · DPI 칸 · 주 화면 장치를 쓰는 곳이 나왔다 `[정적]` (2.10) |
| 5 gdiplus `+0x62b ~ +0x7b3` | **벽 아님.** 새로 나오는 건 `BackgroundThreadStartup` 의 **이벤트 둘 + 스레드 하나** `[정적]` (2.11) |
| 6 그 밖 | N5 출처 `[공백]` · 표 칸 `+0xf` 비트 `0x2` · `g_pEdgeGdi` 보통 0 `[추측]` · `[GpDevice+0x628]` 은 32 비트 화면이면 안 읽음 (2.12) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**초기화가 끝난 뒤, 프로그램이 메시지 상자를 띄우거나 gdiplus 를 켤 때** 다. test.exe 에는 없는 옆길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 만들기
 │    └─ PspAllocateProcess +0x1f11  EPROCESS+0x6e8 ← 부모가 준 Win32k 필터     ← 이번 (2.9, 필터 값 출처)
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → LdrpInitializeProcess
 │    └─ (user32 를 올리는 프로세스라면) user32 · gdi32 DllMain
 │          └─ Gdi32DllInitialize +0x4b GetProcessMitigationPolicy → g_systemCallFilterId  ← 이번 (2.9)
 └─ … → NtContinue (은퇴) → exe 진입점 → (CRT) → main
                                                   ├─ (메시지 상자를 띄우는 프로그램이라면)
                                                   │   MessageBoxW → … → SoftModalMessageBox
                                                   │    ├─ NtUserGetDCEx(0,0,3)            ← 이번 (2.6)
                                                   │    └─ 글 폭 재기                        ← 이번 (2.3 ~ 2.5)
                                                   └─ (gdiplus 를 켜는 프로그램이라면)
                                                       InternalGdiplusStartup
                                                        ├─ (5 ~ 7) CreateICA · GetDeviceCaps · GpDevice  ← run147
                                                        └─ +0x62b ~ +0x7b3 뒷몫                         ← 이번 (2.11)

(그 밑바닥 — 그래픽 부서가 부팅 때 미리 해 둔 일)
 win32kbase!InitializeGre → InitializeDefaultDCAttributes (사무실 기본 양식)   ← 이번 (2.7)
 win32kbase!InitVideo → GreUpdateSharedDevCaps → vGetDeviceCaps (화면 정보 게시판) ← 이번 (2.8)
```

- 메시지 상자 쪽은 [run142](run142-user32-connect-해설.md) 가 "GDI 답이 0 이면 조용히 0" 까지, [run144](run144-gdi-startup-해설.md) 가 "그 답은 대장 칸을 먼저 본다" 까지 봤다. 이번엔 **대장을 통과한 뒤 실제로 글 폭을 재는 길** 이다.
- gdiplus 쪽은 [run147](run147-gdi-display-dc-해설.md) 이 개점 체크리스트 5 ~ 7번까지 봤다. 이번엔 그 **뒤 칸 (`+0x62b ~`)** 이다.
- 프로그램 쪽에서 부르는 일이라 CLAUDE.md 범위로는 "`main` 안에서 OS 를 다시 부르는 화살표" 쪽이다. 그래서 그림엔 안 넣었다.

### 왜 이 질문이 지금 왔나

- PE_emulator 쪽 막힘 (에뮬레이터 숫자):
  - **메시지 상자 12 샘플** 이 `NtUserGetDCEx(0,0,3)` 기본 대답 0 ×2 → `MessageBoxW` 0 으로 끝난다 (run147 목적).
  - run147 이 HDC · 표 칸 · `DC_ATTR` 꼴을 닫았다. 그런데 그 HDC 로 **글 폭을 재는 길** (run144 N3) 이 남았다. HDC 만 고쳐서는 안 풀린다는 뜻이다.
- 같이 남은 것: win32k 쪽이 표를 어떻게 채우나 (N4 · run147 "win32k 몫" 1 ~ 6), 필터 값 5 갈래, gdiplus `+0x62b` 뒤 (N2), 그 밖 자잘한 것 (N5 · 표 칸 뜻 · `g_pEdgeGdi` · `[GpDevice+0x628]`).
- 380 묶음의 차례 ④ 로 사용자가 정했다.

### 무엇이 궁금했나

1. **N3 글 폭** — `GdiGetCharDimensions` · `pcfLocateCFONT` · `bComputeTextExtent` 가 읽는 칸 · 부르는 syscall · 실패하면 어디로. 그리고 `NtUserGetDCEx(0,0,3)` 이 0 이 되는 조건.
2. **세션 기본 `DC_ATTR` 틀** 의 처음 값 · 스톡 핸들 · `GDIINFO` · `pGdiDevCaps` 를 채우는 곳.
3. **필터 값 5** — 어디서 오고, 5 면 무엇이 달라지나.
4. `GrepCreateCompatibleDC` · DPI 칸 · 주 화면 판정 · `hdcOpenDCW`.
5. **N2** — gdiplus 시작 뒷몫이 부르는 syscall · 레지스트리 · 파일.
6. N5 · 표 칸 `+0x0` / `+0xf` · `g_pEdgeGdi` · `[GpDevice+0x628]`.

### 바퀴 전에 알던 것

- `[정적]` (run142) 메시지 상자는 `SoftModalMessageBox +0xfc` 에서 `NtUserGetDCEx(0,0,3)` 로 DC 를 받고, 폭이 0 이면 `+0x153` 에서 `MessageBoxW` 를 0 으로 끝낸다.
- `[정적]` (run144) `GdiGetCharDimensions` · `GetTextMetricsW` · `GetTextExtentPointW` 는 표 칸이 없으면 **syscall 없이** 0 (run144 해설 2.13 표).
- `[정적]` (run147) HDC 는 종류 비트 `0x10000`, `DC_ATTR` 은 `HmgAllocateDcAttr` 의 `0x170` B 슬롯, 처음 값은 세션 기본 틀 `[W32GetSessionState()+0x58]+0x90` (0x1b0 B) 에서 복사. **틀 내용은 `[공백]`.**
- `[정적]` (run147) `GetDeviceCaps` · user32 `GetDC` 에 `g_systemCallFilterId == 5` 갈래가 있다. 그 값이 어디서 오는지는 몰랐다.
- `[정적]` (run147) gdiplus 다음 벽은 `+0x62b InitImagingLibrary` 일 것 (0 이면 `+0x632` → 1).

---

## 2. 찾은 것

### 2.1 한눈에 — 대여 · 글꼴 고르기 · 글 폭 재기 `[정적]`

run 문서 "호출 관계" 의 메시지 상자 쪽을 줄였다.

```
user32!SoftModalMessageBox (0x21f20)                                                      [정적]
 ├─ +0xfc   NtUserGetDCEx(0,0,3)  ── SYSCALL 0x108a     0 → 한 번 더 → 0 이면 MessageBoxW 0
 ├─ +0x12d  SelectObject(hdc, 글꼴)        DC_ATTR+0x128 · +0x98
 ├─ +0x14e  UserGetCharDimensionsEx       +0x79 GetTextFaceW ⇒ NtGdiGetTextFaceW ── SYSCALL 0x1078
 │           +0x99 gdi32full!GdiGetCharDimensions (0x4db20)
 │                  +0x48 ~ +0xd7 표 검사 실패 → +0x19c 0 (syscall 없음)            ← 표 칸 없으면 여기서 끝
 │                  +0x11e pcfLocateCFONT   가지 4a ~ 4h               → NULL (조용히)
 │                  +0x133 bGetTextMetricsWInternal   사본 메트릭 있으면 +0x51 memcpy
 │                                                    없으면 +0x10b NtGdiGetTextMetricsW(hdc, 버퍼, 0x44) ── SYSCALL 0x106e
 │                         0 → 폭 0 → +0x153 JZ → MessageBoxW 0
 │                  +0x155 [TEXTMETRIC+0x37] 비트 0 꺼짐 → 폭 = tmAveCharWidth
 │                         켜짐 → +0x1c5 GetTextExtentPointWInternal(52 자) → 폭 = (cx/26+1)/2
 ├─ +0x174  MB_GetMaxButtonSize  +0x7b GetTextExtentPointW
 ├─ +0x250  GetTextExtentPointW(제목)
 │           (둘 다) +0x6e GetDCDpiScaleValue ≠ 1 / 사본 없음 → +0x269 NtGdiGetTextExtent ── SYSCALL 0x108c
 └─ +0x2e7 DeleteObject · +0x2f6 NtUserReleaseDC ── SYSCALL 0x151c
```

**새 말 — 글 폭 (글자 치수)**: 글꼴로 글자를 쓸 때 **한 글자가 가로 · 세로 몇 픽셀을 차지하나** `[추측: 일반적으로 알려진 설명]`. 메시지 상자는 이걸로 상자 · 단추 크기를 정한다 (그래서 폭 0 이면 상자를 못 만든다). 비유: 알림판을 짜기 전에 **"이 글씨 한 칸이 몇 눈금이지?"** 하고 자로 재는 일.

비유: 직원이 알림판을 세우려고 ① 그래픽 부서에서 **도화지를 빌리고** ② 거기 **글꼴을 고르고** ③ **글씨 한 칸 크기를 재고** ④ 제목 · 단추 글이 몇 눈금인지 재고 ⑤ 도화지를 돌려준다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `│                  +0x11e pcfLocateCFONT   가지 4a ~ 4h               → NULL (조용히)`

- **누가**: gdi32full `GdiGetCharDimensions` (rva `0x4db20`) 가
- **어디서**: 함수 안 `+0x119` 에서 부르고, 돌아오는 자리가 `+0x11e` (나무 숫자 = ret 자리).
- **누구를**: gdi32full `pcfLocateCFONT` (rva `0x1bbd0`) — 이 DC 에 맞는 **글꼴 사본** 을 찾는 함수.
- **무엇을 위해**: 사본이 있으면 그래픽 부서에 안 묻고 재려고. 돌아오면 `+0x128 MOV RDI,[RAX]` 로 사본 포인터를 꺼낸다 `[정적]`.
- "가지 4a ~ 4h" 는 사본을 **포기하는 길 여덟** (2.4), "NULL (조용히)" 는 포기해도 **`LastError` 를 안 세우고** 그냥 NULL 을 준다는 뜻 `[정적]`.

#### 줄 2: `│                                                    없으면 +0x10b NtGdiGetTextMetricsW(hdc, 버퍼, 0x44) ── SYSCALL 0x106e`

- **누가**: gdi32full `bGetTextMetricsWInternal` (rva `0x4dd30`) 가
- **어디서**: 함수 안 `+0x104` 에서 부르고 ret 은 `+0x10b`.
- **누구를**: win32u 를 거쳐 syscall `0x106e` (`NtGdiGetTextMetricsW`) — 그래픽 부서에 거는 전화.
- **무엇을 위해**: 사본에 메트릭이 없을 때 (`사본 = NULL` 이거나 `[사본+0x1c] & 8` 꺼짐) 그래픽 부서에 글꼴 치수를 물으려고. `0x44` B 버퍼로 받아 `+0x114 JZ` 로 0 인지 본다 `[정적]`.
- 이 줄이 이번 바퀴의 **핵심 되돌이** 다. 사본 길이 어디서 막혀도 결국 이 전화가 나간다.

### 2.3 ★ 글 폭 재는 차례 (물음 1) — 0 이 되는 곳은 셋 `[정적]`

run 문서 1절 표를 차례대로 풀었다.

**새 말 — `TEXTMETRIC`**: 글꼴 하나의 **치수 표** — 높이 · 평균 글자 폭 · 가변 폭인가 같은 것 `[추측: 일반적으로 알려진 설명 · SDK 이름]`. 여기선 `0x3c` B 짜리 지역 버퍼다 `[정적]`. 읽는 칸은 셋뿐: `+0x0` (높이, `tmHeight`) · `+0x14` (평균 글자 폭, `tmAveCharWidth`) · `+0x37` 비트 0 (가변 폭, `tmPitchAndFamily`) — 칸 이름은 `[추측: SDK 이름]`. 비유: 글꼴 상자에 붙은 **"치수 카드"**.

**새 말 — 가변 폭 글꼴**: 글자마다 폭이 다른 글꼴 (`i` 는 좁고 `W` 는 넓은) `[추측: 일반적으로 알려진 설명]`. 반대는 모든 글자 폭이 같은 고정 폭. 비유: 글씨마다 칸 크기가 다른 원고지 vs 칸이 다 같은 원고지.

#### 차례

| 차례 | 하는 일 | 0 / 실패면 |
|---|---|---|
| 0 | `SoftModalMessageBox +0xf5 NtUserGetDCEx(0,0,3)` — 도화지 빌리기 | 한 번 더, 그래도 0 이면 `MessageBoxW` 0 (run142) |
| 1 | `+0x126 SelectObject(hdc, 글꼴)` — 메모장 `+0x128` 을 읽고 쓰고, `+0x98` 에 `(& ~0x20) \| 0x10` | **반환은 저장만.** 글꼴 칸이 없어도 0 을 줄 뿐 안 막힌다 |
| 2 | `UserGetCharDimensionsEx +0x72 GetTextFaceW` → syscall `NtGdiGetTextFaceW` (`0x1078`) | 0 이어도 버퍼가 0 그대로 → `'@'` 아님 → 일반 길. (`+0x7e CMP 0x40` — `'@'` = 세로 글꼴 표시 `[추측: 뜻]`) |
| 3 | `+0x92 GdiGetCharDimensions(hdc, 0, &cy)` — 먼저 **표 검사** (`+0x48 ~ +0xd7`) | **하나라도 실패 → `+0x19c XOR EAX,EAX` 로 0, `*cy` 안 씀, syscall 없음** ← 0 ① |
| 4 | `+0x119 pcfLocateCFONT` — 사본 찾기 | NULL 이어도 조용히 다음 차례로 (2.4) |
| 5 | `+0x12e bGetTextMetricsWInternal` — 사본에 메트릭 있으면 `+0x4c memcpy` (syscall 없음), 없으면 **`+0x104 NtGdiGetTextMetricsW`** | syscall 이 0 이면 **폭 0 · `*cy` 안 씀** ← 0 ② |
| 6 | `+0x14c TEST R12` → `+0x1f8` `*cy = tmHeight` | — |
| 7 | `+0x155 TEST [TEXTMETRIC+0x37],1` | **꺼져 있으면 `+0x16f` 폭 = `tmAveCharWidth` (`+0x14`) 로 끝** |
| 8 | (가변 폭일 때) 사본에 계산된 폭이 있으면 (`[사본+0x1c] & 0x10`) `+0x167` 폭 = `[사본+0x254]`. 없으면 `+0x1c0 GetTextExtentPointWInternal("a…zA…Z" 52 자, 깃발 1)` | `+0x1c7 JZ` → **폭 0** ← 0 ③ |
| 9 · 10 | 제목 · 단추 글은 `GetTextExtentPointW` 가 잰다 (2.5) | 되돌이 `NtGdiGetTextExtent` |
| 11 | `+0x2e0 DeleteObject(글꼴)` · `+0x2ef NtUserReleaseDC` | — |

- 차례 8 의 폭 식은 `(cx / 26 + 1) / 2` (`+0x1c9 ~ +0x1e5`) `[정적]`. 52 글자 전체 폭을 반올림해서 52 로 나눈 **평균 한 글자 폭** 과 같은 꼴이다 `[추측: 식 읽기]`.

#### 0 이 되는 곳은 셋뿐

run 문서 "확인된 것" 그대로 `[정적]`:

1. **표 검사** (차례 3) — 대장 줄이 틀림. syscall 이 안 나간다.
2. **`NtGdiGetTextMetricsW` = 0** (차례 5).
3. **(가변 폭일 때) `NtGdiGetTextExtent` = 0** (차례 8 → 9 의 되돌이).

- **사본 길은 어디서 막혀도 0 을 안 만든다.** 이게 이번 바퀴 결론의 뿌리다.

#### 왜 "사본을 안 만들어도 된다" 고 하나 — 단계별로

1. 근거: `pcfLocateCFONT` 의 NULL 가지 여덟이 모두 `LastError` 를 안 세우고 그냥 NULL 이다 `[정적]` (2.4).
2. 근거: `bGetTextMetricsWInternal` 은 사본이 NULL 이면 `+0x104` 에서 syscall 을 낸다 `[정적]`.
3. 근거: `GetTextExtentPointW*` 도 사본 NULL (`+0x1a8`) · 계산 실패 (`+0x1d5`) 면 `+0x20b` 되돌이 → `NtGdiGetTextExtent` 로 간다 `[정적]`.
4. 그래서: 에뮬레이터가 사본을 하나도 안 만들면, 길은 **언제나 syscall 두 개 (`NtGdiGetTextMetricsW` · `NtGdiGetTextExtent`) 로 끝난다.** 이 둘만 "0 아님 + 값" 을 주면 폭이 나온다.
5. 아직 확인 아님: 진짜 Windows 가 이 DC 에서 사본 길을 타는지 · 되돌이를 타는지는 실행해 봐야 안다 (4절 동적 1).

비유: 본관 직원은 자로 재기 전에 **책상 서랍 (글꼴 자)** 을 연다. 서랍에 맞는 자가 있으면 바로 재고, 없거나 잠겼으면 **그래픽 부서에 전화** 해서 "이 글씨 몇 눈금이에요?" 하고 묻는다. 그러니 에뮬레이터는 서랍을 안 만들어도 된다. **전화만 받아서 "몇 눈금" 이라고 답해 주면** 된다. 다만 **번호표가 대장에 없으면** 직원은 서랍도 안 열고 전화도 안 건다 — 그냥 "0 눈금" 이다.

### 2.4 ★ 글꼴 사본 (`pcfLocateCFONT`) — 서랍을 포기하는 가지 여덟 `[정적]`

**새 말 — CFONT (글꼴 사본)**: gdi32full 이 **유저 쪽에 들고 있는 글꼴 치수 사본** — 메트릭 (`[사본+0x258 ~ +0x298]` 에 저장), 글자별 폭 표 (`[사본+0x54 + 2*글자]`, WORD), 계산된 평균 폭 (`[사본+0x254]`) 같은 것을 담는다 `[정적: 읽고 쓰는 칸]`. 새 사본은 메모장 `DC_ATTR+0x160` 목록 머리에 끼운다 (`pcfLocateCFONT +0x58d ~ +0x59c`, **유저가 씀**) `[정적]`. 비유: 본관 책상 서랍의 **"글꼴 자"**. 한 번 재 둔 눈금을 적어 두고 다음엔 전화 없이 쓴다.

run 문서 2절 표를 한 줄씩 풀었다. 전부 **조용히** NULL 이다.

| 가지 | 무엇을 보나 | 이러면 서랍 포기 |
|---|---|---|
| 4a DPI 비율 | 메모장 `+0x88` (`+0x112`). 표가 틀리면 `+0x355 NtGdiGetDCDpiScaleValue` | **`> 1`** (0 · 1 은 통과) |
| 4b 글꼴 연결 | 전역 하나가 `-1` 이면 `+0x142 NtGdiQueryFontAssocInfo(0)` 를 **한 번만** 부르고 저장 | 반환 **≠ 0** |
| 4c 포기 깃발 | 메모장 `+0x98` 비트 `0x20` (`+0x15c`) | 켜져 있으면. (글꼴을 다시 고르면 `SelectObjectImpl +0x3bc` 가 지움) |
| 4d 변환 계수 | `+0x154 & 0xe000` · `+0x40` 비트 0 (`+0x170 ~ +0x184`). `+0x40` 비트 0 이 0 이면 `+0x5ab NtGdiComputeXformCoefficients` | 그 syscall 이 0 → `+0x5cd` 가 `+0x98 \|= 0x20` |
| 4e 한자권 코드 페이지 | `guintDBCScp` (`+0x18a`) | `-1` 이 아니고, `GetCodePage(hdc)` 와 다르고 한자권이면 `+0x382` 가 `+0x98 \|= 0x20` |
| 4f 글꼴 칸 | 메모장 `+0x128` 핸들을 대장에서 다시 확인 (종류 `0xa` 등) | 하나라도 틀리면 `+0x3db` 가 `+0x98 \|= 0x20` |
| 4g 글꼴 attr | `[attr+8]` · `[attr] & 1` (`+0x286 ~ +0x296`) | `[attr+8] = 0` 이거나 `[attr] & 1` |
| 4h 목록 · 만들기 | 목록에서 같은 사본 찾기, 없으면 `+0x40b pcfCreateCFONT` (`gpwcANSICharSet ≠ 0` 필요) | 만들기 실패 |

- 몇 개 (4d · 4e · 4f) 는 그냥 NULL 이 아니라 **메모장 `+0x98` 에 `0x20` 을 켜서**, 글꼴을 다시 고를 때까지 서랍을 아예 잠근다 `[정적]`.
- 비유: 직원은 서랍을 열기 전에 "돋보기 배율이 1 넘나 (4a)", "글꼴 연결이 켜졌나 (4b)", "서랍에 '잠김' 쪽지가 붙었나 (4c)" … 를 차례로 본다. 하나라도 걸리면 **아무 말 없이 서랍을 닫고 전화기를 든다.** 어떤 건 닫으면서 **"잠김" 쪽지** 까지 붙인다 — 글꼴을 다시 고를 때까지 그 서랍은 안 연다.

#### 4e 하나 더 — 타깃 VM 이 한국어 코드 페이지라면

- **새 말 — 코드 페이지 (ACP)**: 유니코드가 아닌 글자를 어떤 표로 읽을지 정한 번호 `[추측: 일반적으로 알려진 설명]`. 949 (`0x3b5`) 가 한국어다 `[추측: 일반적으로 알려진 값]`.
- `guintDBCScp` 는 `GdiProcessSetup` 이 ACP 가 `0x3a4 · 0x3b5 · 0x3b6 · 0x3a8` 일 때만 그 값으로 (`+0x132`), 아니면 `-1` 로 둔다 (`+0x124`) `[정적]`.
- 그래서 **타깃 VM ACP 가 949 면 4e 길을 탄다** `[정적]` — 실제 ACP 는 VM 에서 `GetACP` 로 봐야 한다 (4절 수집 4).
- 어느 쪽이든 끝은 되돌이 syscall 이다. 길만 달라진다.

### 2.5 `GetTextExtentPointW` — 대장보다 DPI 비율을 먼저 본다 `[정적]`

제목 · 단추 글 폭은 `GetTextExtentPointW` (`0x12ce0`) · `GetTextExtentPointWInternal` (`0x11b90`) 이 잰다. 둘은 **같은 차례 · 같은 조건** 이다.

1. 글자 수 검사 (`+0x2b` / `+0x2e` — 0 이면 `SIZE = 0` + 1, syscall 없음).
2. 글자가 전부 ASCII 인가 — 아니면 곧장 되돌이 (`+0x64` / `+0x60 JNZ`).
3. **`GetDCDpiScaleValue(hdc) == 1` 인가** (`+0x6d` / `+0x69`) — 아니면 곧장 되돌이 (`+0x75` / `+0x70 JNZ`).
4. 그다음에야 **표 검사** (실패면 `GetTextExtentPointWInternal +0x8e` `0x57` + 0).
5. `gbLpk` 꺼짐이면 `pcfLocateCFONT` → `bComputeTextExtent` 로 서랍 길.
6. 사본 NULL · 계산 실패면 **되돌이 → `NtGdiGetTextExtent(hdc, 글, 개수, &SIZE, 1)` (`0x108c`)**.

여기서 run144 를 고쳐 읽는다:

- run144 해설 2.13 표는 "`GetTextExtentPointW` — 대장이 비면 0, `0x57`, syscall 없음" 이었다.
- 그런데 3번이 4번보다 **앞** 이다. 대장이 비면 `GetDCDpiScaleValue` 가 대장 대신 **`NtGdiGetDCDpiScaleValue` (`0x12be`) syscall** 을 내고 (`GetDCDpiScaleValue +0x107`), 그 답이 1 이 아니면 **곧장 `NtGdiGetTextExtent` syscall** 로 간다 — `LastError` 안 세움 `[정적]`.
- 그래서 "syscall 없이 실패" 하는 함수 목록에서 **`GetTextExtentPointW` 는 빠진다.** `GetTextMetricsW` · `GdiGetCharDimensions` 는 그대로다.

비유: 제목 글 폭을 재는 직원은 대장을 펴기 **전에** "이 도화지 돋보기 배율이 딱 1 인가?" 부터 본다. 1 이 아니면 서랍이고 대장이고 **안 보고 바로 전화** 한다.

### 2.6 ★ `NtUserGetDCEx(0,0,3)` 이 0 이 되는 길 — 일반 프로세스엔 없다 `[정적]`

인자는 `(hwnd 0, hrgn 0, flags 3)`. `3 = DCX_WINDOW | DCX_CACHE` 는 `[추측: 이름]`.

**새 말 — 캐시 DC (DCE)**: 그래픽 부서가 창마다 새로 안 만들고 **목록에 두고 돌려 쓰는 DC** `[추측: 일반적으로 알려진 설명]`. `_GetDCEx` 는 목록 `[세션+0xe338]+0x18` 을 훑고, 빈 칸이 없으면 `CreateCacheDC` 로 0x100 B 짜리 하나를 새로 만든다. HDC 는 그 안 `[DCE+0x10]` 이다 `[정적]`. 비유: 그래픽 부서 창고의 **"대여용 도화지 묶음"**.

**새 말 — UI 한도**: 프로세스에 걸 수 있는 "화면 · 창 만지기 제한" `[추측: 이름 job UILIMIT]`. 코드로는 `HasUILimit` · `CanPerformUIOperation` 이 커널 `KfGetProcessEffectiveUILimits(프로세스)` 결과의 비트 `0x1` 을 본다 (결과서). 비유: 가게 문에 붙은 **"화면 만지기 금지" 딱지**.

| 0 이 되는 길 | 줄 | 일반 프로세스는 |
|---|---|---|
| ① `hrgn` 이 세션의 특별 지역 핸들과 같음 | `+0x62 CMP RSI,[세션+0xace8]` → `0x57` + NULL (그 칸 뜻 `[공백]`) | run 문서는 안 걸린다고 본다. 다만 `hrgn = 0` 이라, 그 칸이 0 이 아니어야 안 걸린다 `[추측: 칸 값은 안 봄]` |
| ② 모르는 깃발 | `+0x6f TEST EBP,0xffdaf800` → `0x57` + NULL | `3` 은 0 이라 안 걸림 |
| ③ UI 한도 + 기능 꺼짐 + 바탕 창 무효 | `+0x15f HasUILimit` · `+0x17a ValidateHwnd` → `UserSetLastError(5)` + NULL | UI 한도가 없으면 안 걸림 |
| ④ 도화지 못 만듦 | `_GetDCEx +0xa1c CreateCacheDC` → `+0x83 GreCreateDisplayDC(hdev, 0, 2)` — hdev = **주 화면 장치 `[세션+0xe338]+0x30`** · `+0x2b9 GreSetDCOwnerEx` 실패 | 화면이 있으면 안 걸림 |

- `hwnd = 0` 이면 지금 스레드의 바탕 창을 쓴다 (`+0xa4 [pti+0x1f0]`). 책상이 없으면 널 검사 없이 읽어 접근 위반일 것 `[추측]`.
- **기능 상태** (`Feature_LearningModeExpansion`): 파일 속 처음 값으로는 **꺼짐** `[objdump]`. 켜져 있으면 ③ 자리에서 `ValidateHwnd` 가 0 이어도 원격 측정 한 줄 (`+0x111`) 만 하고 계속 간다 `[정적]`. 실제 값은 런타임에 바뀔 수 있어 동적.
- 어느 쪽이든 **UI 한도가 없는 일반 프로세스는 ③ 에서 안 막힌다.**

#### 그래서 12 샘플의 0 은 — 단계별로

1. 근거: 0 이 되는 네 길은 위 ① ~ ④ 뿐이다 `[정적]`.
2. 근거: `(0, 0, 3)` 은 ② 에 안 걸리고 (① 은 위 표 단서), 일반 프로세스는 ③ 에, 화면이 있으면 ④ 에 안 걸린다 `[정적]`.
3. 그래서: **진짜 길에선 일반 프로세스에 0 이 될 조건이 없다.** 메시지 상자 12 샘플 · `CreateDCA` 5 샘플의 0 은 **stub (기본 대답 0) 탓** 이다 (run 문서 판정).
4. 아직 확인 아님: 실제 GUI 프로세스에서 어느 갈래를 지나는지는 동적 (4절 동적 3).

비유: 직원이 "도화지 하나 주세요" 하면 그래픽 부서는 ① 이상한 영역을 달라고 했나 ② 모르는 주문 표시가 있나 ③ 이 가게에 "화면 만지기 금지" 딱지가 붙었나 ④ 창고에 화면 장치가 있나 만 본다. 평범한 가게라면 **거절할 이유가 없다.** 에뮬레이터 창구가 "없어요 (0)" 라고 답한 건 창구가 아직 비어 있어서다.

### 2.7 사무실 기본 양식 (세션 기본 `DC_ATTR` 틀) — 칸 값 (물음 2) `[정적]`

run147 해설 2.7 에서 "그래픽 부서가 도화지를 만들 때 **사무실 기본 양식** 을 베낀다" 까지 봤다. 이번엔 **그 양식에 처음 무엇이 적히나** 다.

- 양식을 채우는 곳: `InitializeGre` (win32kbase `0x2f93fc`) `+0xb8` → **`InitializeDefaultDCAttributes`** (`0x1914e8`) `[정적]`.
- 자리: `G = [W32GetSessionState()+0x58]` (`InitializeGre +0x9a`), 틀 = `G+0x90` (0x1b0 B). **틀 칸 = G 안 오프셋 − 0x90** 으로 맞췄다 (flow-writer 가 쓰는 줄 28 개를 이 식으로 다시 맞춤 — 결과서와 일치).
- 유저 메모장에는 이 틀의 앞 `0x160` B 가 들어간다 (run147). 그래서 **유저가 보는 `DC_ATTR` 처음 값 = 아래 값** (+ `+0x00` 비트 1, `+0x98`).

대표 칸만 (전체는 run 문서 4절):

| 칸 | 처음 값 | 누가 읽나 · 뜻 |
|---|---|---|
| `+0x20` · `+0x2c` (float) | `16.0f` | 사본 비교 (`pcfLocateCFONT +0x2bf ~ +0x2e3`). 배율 `[추측]` |
| `+0x40` | `0x4b` (비트 0 켜짐) | 4d 가지 (`pcfLocateCFONT +0x180`). 비트 0 = 변환 계수 유효 `[추측]` |
| **`+0x88`** | **`1`** | **DPI 비율** — 4a 가지 · `GetDCDpiScaleValue +0xe0`. 뒤에 바꾸는 곳은 `SetCachedDpiScaleValue` 뿐 |
| `+0x98` | 틀 `0x10` → DC 사본 **`0x12001f`** | run147 (DC 를 만들 때 `DCMEMOBJ +0x1b8` 가 덮음) |
| `+0xa0` | **스톡 0 (WHITE_BRUSH) 핸들** | 붓 (`bInitBrush +0xa0`) |
| `+0xa8` | **스톡 7 (BLACK_PEN) 핸들** | 펜 (`bInitBRUSHOBJ +0x286`) |
| `+0xf0` | `0` | ICM 꺼짐 (run147) |
| `+0x108 · +0x114 · +0x118 · +0x11c · +0x120` | `0` (안 씀) | ICM 정보 · 언어팩 칸 · 글 늘리기 |
| **`+0x128`** | **스톡 13 (SYSTEM_FONT) 핸들** `[추측: 오프셋 일치]` | 글꼴 (win32kfull 스톡 글꼴 초기화가 `[G'+0x1b8]` 에 씀, `+0x1b8 − 0x90 = +0x128`) |
| `+0x154` | `0x1802` (`& 0xe000 = 0`) | 4d 가지 (`+0x170`) |
| `+0x160` | 0 으로 시작 | **gdi32full 이 글꼴 사본 목록 머리로 씀** (2.4) |

- 스톡 13 이 `[추측]` 인 이유: win32kfull 쪽이 쓰는 자리 `[G'+0x1b8]` 이 틀 `+0x128` 과 **오프셋이 맞는다** 는 것뿐이고, 두 파일의 `G'` 가 같은 세션 전역인지는 포인터를 안 따라갔다.
- **스톡 핸들 꼴**: 객체 핸들에 **`0x800000` 을 켠 값** (`bSetStockObject +0x33 BTS 0x17`), 배열 `G+0xbc0` 의 `i` 번째 `[정적]`. run144 해설 2.6 의 "핸들 비트 23 = 스톡" 이 커널 쪽에서 이렇게 켜진다. 공유 메모리 꽂이 `+0x1800b0 + 8*i` (run144 해설 2.11) 와 같은 배열인지는 `[추측: 오프셋 · 번호 일치]`.

#### 에뮬레이터에겐 무슨 뜻인가

- **이 값을 그대로 안 써도 된다** — 유저 쪽이 값 검사를 안 한다 (run147) `[정적]`.
- 다만 두 칸은 **어느 길로 가나** 를 정한다:
  - `+0x88` — 1 이면 서랍 길을 시도 (→ NULL → 되돌이), 1 이 아니면 곧장 되돌이. **어느 쪽도 syscall 로 끝난다.**
  - `+0x40` — run 문서 판정은 "사본 길을 안 쓸 거면 **0 으로 두는 편이 단순**" (4d 에서 바로 되돌이).

비유: 사무실 기본 양식에는 "돋보기 배율 1", "붓 = 공용 흰 붓", "펜 = 공용 검은 펜", "글꼴 = 공용 시스템 글꼴 (아마)" 이 미리 적혀 있다. 그런데 본관 직원은 이 값이 **맞는지 따지지 않는다.** 배율 칸 하나만 "서랍을 열어 볼까, 바로 전화할까" 를 가른다.

### 2.8 `GDIINFO` · 화면 정보 게시판 · 언어팩 칸 (물음 2) `[정적]`

**새 말 — `GDIINFO`**: 화면 장치 하나의 **장치 정보 원본** — 해상도 · 비트 수 · DPI 같은 것. 커널 쪽 `PDEV+0x848` 에 있다 `[정적: 자리]`. `GDIINFO` · `PDEV` 이름 뜻은 `[추측: 일반적으로 알려진 설명 — PDEV 는 그래픽 부서가 화면 장치 하나를 나타내는 기록]`. 비유: **게시판에 옮겨 적기 전의 원본 서류**.

#### 게시판 ← 원본

- `GreUpdateSharedDevCaps(PDEV)` → `+0x25 vGetDeviceCaps` 가 원본 (`PDEV+0x848`) 칸을 **게시판 (`+0x180000 ~ +0x180090`)** 으로 옮긴다 `[정적]`.
- 부르는 곳: `InitVideo +0x1ba` · `SetDPIinSetup` · `DrvChangeDisplaySettingsInternal` (결과서).
- 예: 게시판 `+0x20` (BITSPIXEL) ← 원본 `+0x18` (15 면 16), `+0x6c` · `+0x70` ← 원본 `+0x10` · `+0x14` (해상도). 나머지는 run 문서 5절 표.

#### 원본은 누가 채우나

- **가짜 화면** (`StubDispEnablePDEV` → `+0x33 bInitGdiInfo`) 이면 **상수** — 1024 × 768 · 32 비트 · 60 Hz · LOGPIXELS = DEVMODE `dmLogPixels` `[정적]`. HORZSIZE · RASTERCAPS · 팔레트 크기 · PHYSICAL* 은 안 채움 (0). `StubDisp` 가 "연결 끊긴 세션용" 이란 뜻은 `[추측: 이름 · 힌트 함수 이름]`.
- **진짜 화면 드라이버** 는 `EnablePDEV` 가 드라이버를 **간접 호출** 한다 → 이 VM 에서 누가 값을 채우는지 `[공백]`.
- 그래서 VM 에서 `GetDeviceCaps` 표를 떠 오면 가늠할 수 있다 — `HORZSIZE` · `RASTERCAPS` · `PHYSICAL*` 이 0 이고 1024 × 768 이면 StubDisp 쪽 (4절 수집 1).

#### 언어팩 칸 `+0x180094` — 글 폭 길에서 `+0x114` 가 안 쓰이는 이유

**새 말 — 언어팩 (LPK)**: 아랍어 · 태국어처럼 **글자 배치가 복잡한 언어** 를 위한 추가 처리 `[추측: 일반적으로 알려진 설명]`. gdi32full 에선 전역 `gbLpk` 가 켜져 있을 때만 `LpkGetTextExtentExPoint` 쪽으로 간다 `[정적]`. 비유: 특별한 글씨를 위한 **"특수 자"**.

1. `InitializeGre +0x4ac` 가 게시판 `+0x180094` 를 0 으로 비운다.
2. `+0x4b8 RtlQueryRegistryValues` 로 `\Registry\Machine\System\CurrentControlSet\Control\LanguagePack` 아래 **값마다** 콜백 `LpkShapeQueryRoutine` → `REG_DWORD` 면 `OR [+0x180094], 1 << 값` (`+0x32`). 키 · DWORD 가 없으면 0.
3. `+0x4cb` 가 `+0x180098` 을 1 로.
4. 유저 쪽 `GdiProcessSetup +0x161` 이 `+0x180094` 가 0 이면 언어팩 초기화를 건너뛰어 **`gbLpk = 0`**.
5. 그래서 글 폭 길에서 메모장 `+0x114` · `LpkGetTextExtentExPoint` 를 **안 탄다** — run147 의 `+0x114` `[공백]` 이 이렇게 닫혔다 `[정적]`.

### 2.9 ★ 필터 값 5 — 어디서 오나, 5 면 무엇이 달라지나 (물음 3) `[정적]` + `[objdump]`

**새 말 — 필터 값 (`g_systemCallFilterId`, Win32k 필터)**: 프로세스를 만들 때 정해지는 **"이 프로세스의 win32k syscall 을 걸러라" 설정의 번호** `[추측: 일반적으로 알려진 설명 · 속성 이름]`. gdi32 · user32 가 각자 전역에 들고 있고, 5 면 몇몇 함수가 그래픽 부서 대신 다른 길로 간다 `[정적]`. 비유: 가게 입구의 **"출입 제한 등급" 딱지**. 일반 가게는 0 (딱지 없음).

**새 말 — 완화 정책 (`GetProcessMitigationPolicy`)**: 프로세스의 보안 설정 하나를 **번호로 물어보는** kernelbase 함수 `[추측: 일반적으로 알려진 설명]`. 여기선 정책 `0xb` 를 묻는다 `[정적]`.

#### 출처 — 위에서 아래로

```
gdi32!Gdi32DllInitialize +0x4b  GetProcessMitigationPolicy(자기, 0xb, &값, 4)   → +0x62 하위 4 비트 → g_systemCallFilterId   [정적]
 └─ kernelbase!GetProcessMitigationPolicy  NtQueryInformationProcess(자기, 0x34, {0xb, 값}, 8)                      [objdump]
     └─ nt!NtQueryInformationProcess +0x1d3f PsGetWin32KFilterSet = [EPROCESS+0x6e8]  → +0x1d4b AND 0xf              [정적]
         └─ 그 칸은 nt!PspAllocateProcess +0x1f0e · +0x1f11 이 프로세스 만들 때 [속성+4] 로 씀                      [정적]
```

- user32 도 같은 걸 묻는다 — `ClientThreadSetup +0x45` · `_UserClientDllInitialize +0x463` 이 값이 `7` (= 아직 안 알아봄) 일 때 한 번. 실패하면 `7` 그대로 (= 5 아님) `[정적]`.
- `[EPROCESS+0x6e8]` 은 **부모가 준 "Win32k 필터" 속성** `[추측: 이름]` 이다. **일반 프로세스는 0** (run 문서).
- 이 칸은 test.exe 도 가진 커널 칸이다. 하지만 이번에 사진 값을 읽지 않았다 — test.exe 값이 0 인지는 `[공백]` (4절 짐 지도).

#### 5 일 때 달라지는 곳

| 곳 | 5 일 때 |
|---|---|
| user32 `GetDC` (`0x4a8c0`) `+0x0 CMP · +0x7 JZ → +0x15` | 인자 무시, **가짜 HDC `0xFFFFFFFFDCDE5000`**, syscall 안 부름 |
| user32 `ReleaseDC` | 가짜 HDC 면 1, syscall 없음 |
| gdi32 `GetDeviceCaps` `+0x419 ~ +0x426` | **가짜 HDC 만** 표 검사 없이 게시판 길 (run147 2.9) |
| gdi32 `D3DKMTOpenAdapterFromGdiDisplayName` | `0xC000001C` |
| user32 그 밖 25 곳 (창 속성 · 메시지 · 훅 · 클래스 …) | 다른 길 — 본문은 sast/151 몫 `[공백]` |

- **값이 0 이거나 `NtQueryInformationProcess` 가 실패해도 이 갈래는 하나도 안 탄다** `[정적]`. 그래서 run 문서 판정은 "에뮬레이터가 5 를 줄 이유가 없다 — **5 금지**".

비유: 입구에 "등급 5" 딱지가 붙은 가게는 도화지를 빌리러 가도 그래픽 부서에 안 가고 **입구에서 "가짜 도화지 번호표"** 를 받는다. 딱지 번호는 가게를 차릴 때 **본사 (부모)** 가 정해서 관리사무소 장부 (`EPROCESS+0x6e8`) 에 적는다. 평범한 가게는 딱지가 없다 (0). 에뮬레이터가 괜히 5 를 주면 가게 전체가 엉뚱한 길로 간다.

### 2.10 win32k 몫 2 · 4 · 6 — 연습장 · DPI 칸 · 주 화면 장치 (물음 4) `[정적]`

run147 이 남긴 "win32k 몫" 중 나머지다. 에뮬레이터 쪽으로는 **값이 아니라 "누가 쓰나"** 가 나왔다. 짧게.

- **연습장 (`GrepCreateCompatibleDC`, `0x14950`)**: 원본 DC 가 있으면 `+0x126 GreCreateDisplayDC([원본+0x30] = PDEV, 1, 0)`, 없으면 (`CreateCompatibleDC(0)`) 주 화면 장치로 `+0x256`. 종류 1 (메모리 DC) 이면 기본 비트맵 1 × 1. 원본에 DPI 비율이 켜져 있으면 `+0x2a4 vSetDpiScaling`, 레이아웃 비트면 `+0x293 dwSetLayout`.
- **DPI 칸 — 누가 쓰나**:

| 칸 | 쓰는 곳 |
|---|---|
| `[W32PROCESS+0x10c]` (문맥) | `NtUserSetProcessDpiAwarenessContext +0xc4`. 프로세스 만들 때 처음 값 `[공백]` |
| `[W32PROCESS+0x110]` (시스템 DPI) | `InitUserScreen +0x396` (모든 프로세스) · `SetDPIinSetup +0x492` (지금 프로세스) — 값 `[psi+0x1b56]` |
| `[스레드+0x190]` (문맥) | `W32SetCurrentThreadDpiAwarenessContext +0x6d` |
| `DC_ATTR+0x88` (비율) | `SetCachedDpiScaleValue` 만 — "DPI 변환 없음" 이면 **1** (결과서) |

- **주 화면 장치 `[세션+0xe338]+0x30`**: `InitVideo +0x179 · +0x180` (`DrvSetDisplayConfig` 가 준 목록의 첫 장치) 가 쓰고 `SetDPIinSetup +0x3e5` 가 다시 쓴다. 2.6 ④ 에서 `CreateCacheDC` 가 읽는 칸이 이것이다.
- **`hdcOpenDCW`** (`0x3a260`): 이름으로 화면 장치를 찾으면 `UserGetMonitorDC` / `GreCreateDisplayDC`, 못 찾으면 프린터 쪽 (`PDEVOBJ` → `DrvCreateMDEV` → `GreCreateDisplayDC`). 하는 일 설명은 호출 이름에서 읽은 윤곽 `[추측]`, 줄은 `[정적]`. 프린터 길 뒷부분은 `[공백]`.

### 2.11 ★ gdiplus 시작 뒷몫 (N2) — 벽이 아니다, 새로 나오는 건 스레드 하나 `[정적]`

run147 은 "다음 벽은 `+0x62b InitImagingLibrary`" 라고 했다. 열어 보니 **벽이 아니었다.**

| 차례 | 하는 일 | 바깥 답이 필요한가 |
|---|---|---|
| `+0x62b` | `InitImagingLibrary` — 임계 구역 초기화 등 | **아니다.** `+0x2d MOV EAX,1` 늘 1, `+0x632 JZ` 못 탐 |
| `+0x6d5` | `SysColorNotify` = user32 `GetSysColor` ×4 = `[gpsi + 지표*4 + 0x11d8]` 읽기 | syscall 없음. 단 **`gpsi` 가 0 이면 접근 위반** |
| `+0x6e1 ~ +0x723` | `GetModuleHandleW("gdi32.dll")` · `GetProcAddress` ×2 | 이미 올라온 모듈 — 반환 안 봄 |
| `+0x72a` | `GetSystemDefaultLCID` | 반환 안 봄 (kernelbase 안은 `[공백]`) |
| `+0x73d` | `InitSystemFontsDirs` — 시스템 폴더 문자열 + `\fonts` | **경로 이름만.** 파일 · 레지스트리 안 건드림 |
| `+0x775` | `GetGpInstalledFontCollection` — 빈 표 할당 | 글꼴 열거는 **나중에 처음 쓸 때** |
| `+0x78b` | `GpMallocEx(0x10)` | 할당뿐 |
| **`+0x7b3`** | **`BackgroundThreadStartup`** | **여기가 새 syscall 꾸러미** (아래) |

#### `BackgroundThreadStartup` — 이벤트 둘, 스레드 하나

**새 말 — 이벤트 (`CreateEventW`)**: 스레드끼리 **"준비됐어" 신호를 주고받는 깃발** 같은 커널 객체 `[추측: 일반적으로 알려진 설명]`. 비유: 직원끼리 쓰는 **신호등**.

1. `+0x7ad CMP [입력+0x10],0` — `SuppressBackgroundThread` 가 0 일 때만 들어간다 `[추측: 칸 이름]`.
2. **`CreateEventW(0,1,0,0)` ×2** (`+0x16` · `+0x36`) = `NtCreateEvent` ×2.
3. `AddRefModule` — `GetModuleFileNameA` → `LoadLibraryExA(그 경로)` (자기를 한 번 더 올림 — 새 파일은 안 연다 `[추측]`).
4. `HeapAlloc(0x18)` → **`CreateThread`** (`CreateThreadAndAddRefDll +0x85`) = `NtCreateThreadEx`.
5. **하나라도 NULL 이면** → `+0x7ba JZ → +0x7d1` 로 **`GdiplusStartup` 실패** `[정적]`.

- 새 스레드가 하는 일 (`BackgroundThreadProc` → `InternalNotificationStartup`): 숨은 창 (`+0x211 CreateWindowExW`) · 팔레트 (`+0x291`) · **`GetDC(숨은 창)`** (`+0x2a7`) · `SelectPalette` · `ReleaseDC` · `DeleteObject` → 메시지 고리 (`MsgWaitForMultipleObjects` · `PeekMessageW`) 에서 **계속 기다림** `[정적]`.
- 그 안 반환은 **하나도 안 본다.** `BackgroundThreadStartup` 은 **스레드 핸들만** 본다 (`+0x83 ~ +0x8d`).
- **레지스트리 · 파일을 읽는 줄은 이 구간에 없다.** 글꼴 레지스트리 · 파일은 `GpInstalledFontCollection` 이 처음 쓰일 때의 일 (이번 범위 밖).

비유: 화방 개점 체크리스트 8번부터는 **서류 정리** 수준이다 — 그림 파일 도구 준비 (늘 OK), 진열장 색 견본 보기, "글꼴 폴더 주소" 메모하기, 빈 글꼴 목록 만들기. 마지막에 **뒷방 직원 한 명** 을 새로 뽑고 신호등 두 개를 단다. 이 직원은 숨은 창 하나 열어 두고 **연락이 올 때까지 계속 기다린다.** 직원 · 신호등을 못 구하면 개점 실패.

### 2.12 그 밖 (물음 6) `[정적]` · `[objdump]`

- **N5 `NtQueryInformationProcess(−1, 0x25)`**: kernelbase 안에서 이걸 부르는 줄은 **`GetProcessVersion` (`0x23dc0`) `+0x3b` · `+0xd9`** 뿐 `[objdump]`. 그런데 kernelbase 안에서 `GetProcessVersion` 을 부르는 `call` 은 0 건 `[objdump]`, `GetVersionExW` 는 `RtlGetVersion` + 선택 함수 표만 `[objdump]`. → **gdiplus 쪽 출처는 `[공백]`**. run144 의 "`GetVersionExA` 쪽일 것" `[추측]` 은 확인 안 된 채 남는다.
- **표 칸 `+0xf` · `+0x0`**: 유저 쪽이 `+0xf` 를 읽는 곳은 하나 — `SelectObjectImpl +0x398 TEST [항목+0xf],0x2` (켜져 있으면 빠른 길을 건너뛰고 되돌이) `[정적]`. `+0x0` 은 gdi32 · gdi32full 어디도 안 읽음 → 커널 객체 포인터 자리 `[추측]`.
- **`g_pEdgeGdi`**: `GetDeviceCaps +0x2b` 에서 API 세트 `ext-ms-win-gdi-edgegdi-l1-1-0` 가 있나 묻고, 있으면 GDI 호출이 Edge 쪽으로 (결과서). 이 빌드 `apisetschema.dll` 의 그 계약은 **호스트 이름이 빈 글** `[objdump]`, `edgegdi.dll` 은 System32 에 없음 → **보통 0** `[추측: 빈 호스트 = 없음으로 취급]`.
- **`[GpDevice+0x628]`**: `TECHNOLOGY == 1` 이고 `BITSPIXEL ≤ 8` 일 때만 본다 → **32 비트 화면이면 안 읽는다** `[정적]`.

### 2.13 ★ PE_emulator 판정 · 메시지 상자 12 샘플을 푸는 최소 (run 문서 "PE_emulator 쪽")

결과서도 flow-writer 도 `../PE_emulator*` 를 **이번엔 열지 않았다** — run147 때 읽은 상태 기준의 판정이다. 고칠지는 사용자 · PE_emulator 결정.

| 대상 | 판정 | 왜 |
|---|---|---|
| 메시지 상자 12 샘플 | **stub 탓** | 진짜 길엔 0 될 조건이 없다 (2.6). 고칠 것 = HDC 돌려주기 + 글 폭 대답 |
| run147 목록 13 `NtGdiGetDeviceCaps` 최소값 | **맞음** | StubDisp 상수와 같은 꼴. 유저 쪽은 값 검사 안 함 |
| run147 목록 10 `DC_ATTR` | **더하기** | 진짜 틀 값 (2.7) 이 있다. 사본 길을 안 쓸 거면 `+0x40` = 0 이 단순 |
| `g_systemCallFilterId` (run147 목록 16) | **5 를 줄 이유 없음** | 일반 프로세스는 0 (2.9) |
| gdiplus `+0x62b ~ +0x775` | **벽 아님** | 단 `gpsi` 는 있어야 함 (run85 csrss 답) |

#### 푸는 최소 — 순서대로

**전제**: run147 목록 8 ~ 10 (DC 번호표 · 대장 줄 · 메모장) 이 있어야 한다. 없으면 아래를 다 줘도 `GdiGetCharDimensions` 가 표 검사에서 syscall 없이 0 이다 (2.3 차례 3).

1. **`NtUserGetDCEx(0, 0, 3)`** → run147 목록 8 꼴 HDC (`번호 | 0x10000`) + 대장 줄. `NtUserReleaseDC` → 1.
2. **`NtGdiGetTextMetricsW(hdc, 버퍼, 0x44)` → 0 아님 + 버퍼 앞 `0x3c` B** — `+0x00` (높이) ≥ 1 · `+0x14` (평균 글자 폭) ≥ 1.
3. **`TEXTMETRIC +0x37` 비트 0 은 0** — 그러면 52 글자 계산을 건너뛰고 `+0x14` 가 그대로 폭이 된다.
4. **`NtGdiGetTextExtent(hdc, 글, 개수, SIZE*, 1)` → 0 아님 + `SIZE`** (개수 > 0 이면 `cx ≥ 1`). 제목 · 단추 글이 이걸로 잰다.
5. 나머지 (`NtGdiGetTextFaceW` · `NtGdiQueryFontAssocInfo` · `NtGdiComputeXformCoefficients` · `NtGdiGetDCDpiScaleValue`) 는 **기본 대답 0 이어도 된다** — 서랍 길만 막힐 뿐 되돌이로 지나간다.

#### stub 이 채울 최소 목록 — run147 의 1 ~ 16 에 이어 17 ~ 20 (뼈대만, 전체는 run 문서)

- 17: **글 폭 되돌이 syscall** — 위 1 ~ 5.
- 18: **사본 길 흉내 — 필요 없음, 권하지 않음.** 하나라도 틀리면 `+0x98 |= 0x20` 으로 막힐 뿐이라 안 만드는 쪽이 싸다.
- 19: 조용한 값 — 공유 `+0x180094 = 0` · `+0x180098 = 1`, `gpsi` 있음, 필터 질의는 "성공 + 0" (또는 실패) — **5 금지**, Edge 계약 없음.
- 20: **gdiplus 백그라운드 스레드** — `NtCreateEvent` ×2 · `NtCreateThreadEx` 가 0 아닌 핸들을 줘야 한다. 그 스레드는 메시지 고리에서 **영영 기다리는** 스레드라 에뮬레이터 스케줄러가 견뎌야 한다 `[추측: 에뮬레이터 쪽 영향]`.

**그 뒤 벽 한 줄**: gdiplus 시작은 20 까지면 끝까지 간다. 다음 벽 후보는 **글꼴 열거** (`GpInstalledFontCollection` 첫 사용 — 레지스트리 `…\CurrentVersion\Fonts` · 글꼴 파일). 이번에 안 읽음.

### 2.14 앞 run 고쳐 읽기 — run144 · run147

틀린 줄이 아니라 길이 좁혀지거나 `[공백]` 이 닫힌 것이라 run 문서는 "정정" 이 아니라 "**고쳐 읽기**" 로 앞 문서 · 해설 맨 위에 한 줄씩 넣었다 (본문은 그대로).

| 앞 run | 전에 | 이제 |
|---|---|---|
| run144 해설 2.13 | `GetTextExtentPointW` — 대장이 비면 0 + `0x57`, syscall 없음 | **빠진다.** 먼저 `GetDCDpiScaleValue == 1` 을 보고, 대장이 비면 `NtGdiGetDCDpiScaleValue` syscall → 1 아니면 `NtGdiGetTextExtent` syscall (2.5) |
| run144 해설 2.4 | `0x25` 는 kernelbase `GetVersionExA` 쪽일 것 `[추측]` | 확인 안 됨 — kernelbase 안 0x25 는 `GetProcessVersion` 뿐, 그 호출자 없음. 출처 `[공백]` (2.12) |
| run144 4절 Q278 | N2 ~ N5 남음 | N2 (벽 아님) · N3 (글 폭) · N4 (win32k 표 일부) 닫힘, N5 반만 |
| run147 해설 2.8 `+0x114` | N3 몫 `[공백]` | `gbLpk ≠ 0` 일 때만 읽음, `gbLpk` 는 레지스트리 `LanguagePack` 값이 있을 때만 (2.8) |
| run147 해설 2.7 `+0x160` | 0 | 0 으로 시작해 **gdi32full 이 사본 목록 머리로 씀** (2.4) |
| run147 해설 2.8 `+0x98` | 처음 값 `0x12001f` | 틀 값은 `0x10`, `0x12001f` 는 DC 만들 때 덮는 값 (유저가 보는 처음 값은 그대로) |
| run147 해설 2.7 · 2.8 | 틀 내용 · 어느 스톡인지 `[공백]` | 붓 스톡 0 · 펜 스톡 7 · 글꼴 스톡 13 `[추측: 오프셋 일치]` (2.7) |
| run147 4절 Q278b | "win32k 몫" 1 ~ 6 · 대조 6 의 기능 꺼짐 갈래 | 닫힘 (2.6 · 2.8 · 2.10) |

- 덤: run 문서는 run152 문서에도 "보탬 · 표기" 한 줄을 넣었다 (F2 실패 값 `0xc000000d` · PEB32 세 칸 줄 범위). 이 글의 주제 밖이라 run 문서 "run152 대조" 로 링크만.

### 2.15 flow-writer 대조 — 결과서와 다르게 적은 곳 (짧게)

run 문서 "flow-writer 대조" 는 **결론을 바꾸는 어긋남 없음**, 줄 표기만 고친 곳 일곱이다. 이 글은 run 문서 값을 따랐다.

1. `pcfLocateCFONT` 의 `NtGdiGetDCDpiScaleValue` call 은 `+0x352` 가 아니라 **`+0x355`** (ret `+0x35c`).
2. 사본 참조 수 돌려놓기 `+0x178 DEC` 는 `pcfLocateCFONT` 가 아니라 **`GdiGetCharDimensions +0x178`** (부른 쪽). `GetTextExtentPointW` 는 `+0x1be`.
3. `GetTextExtentPointW` 의 ASCII 검사는 `+0x40` 이 아니라 **`+0x4d · +0x52` 고리 → `+0x5e · +0x60 JNZ → +0x214`** (앞에 `+0x3b · +0x3e` 한 줄도).
4. `guintDBCScp` 에 코드 페이지 값을 쓰는 줄은 `+0x124` 가 아니라 **`+0x132`** (`+0x124` 는 `-1` 쓰는 줄). 뜻은 같다.
5. `NtUserGetDCEx` 의 `UserSetLastError` call 은 **`+0x198`** (ret `+0x19d`).
6. `+0x49 · +0x4e` 는 `GpFontTable` 이 아니라 **`GpInstalledFontCollection` 생성자** 의 줄.
7. `GreCreateDisplayDC +0x14c` 는 `CMP R14D,1` 이 아니라 `CMP R14D,ESI` (`ESI = 1`). `+0xb5` 는 `MOV`, OR 은 `+0xaf`. 뜻 같음.

- 이름이 둘로 갈리는 `alias:` 두 곳 (`NtUserGetDCEx +0x9f` — `W32GetCurrentThread` / `PtiCurrent`, `pcfLocateCFONT +0x11d` — `ReturnValueTracer<int>` / `<long>` 생성자) 은 후보 둘을 그대로 적었다.

### 2.16 정리 — 비유로 한 번 더

1. 직원이 알림판 (메시지 상자) 을 세우려고 그래픽 부서에서 **도화지를 빌린다.** 평범한 가게라면 그래픽 부서가 거절할 이유가 없다 — 에뮬레이터가 "없어요" 한 건 창구가 비어서다.
2. 도화지에 **글꼴을 고르고**, 본관 직원이 **글씨 한 칸 크기** 를 잰다.
3. 직원은 먼저 **번호표 대장** 을 본다. 줄이 틀리면 아무것도 안 하고 "0 눈금".
4. 대장이 맞으면 **책상 서랍 (글꼴 자, CFONT)** 을 연다. 배율 · 잠김 쪽지 · 코드 페이지 … 하나라도 걸리면 **말없이 서랍을 닫고**, 어떤 땐 "잠김" 쪽지도 붙인다.
5. 서랍이 안 되면 **그래픽 부서에 전화** (`NtGdiGetTextMetricsW` · `NtGdiGetTextExtent`). 전화로 "높이 ○ · 평균 폭 ○" 를 받으면 끝.
6. 그래서 에뮬레이터는 서랍을 안 만들고 **전화 두 통만 받아 주면** 된다 (대장 줄은 있어야 함).
7. 도화지 메모장의 처음 값은 그래픽 부서의 **사무실 기본 양식** 에서 온다 — 배율 1, 공용 흰 붓, 공용 검은 펜, 공용 시스템 글꼴 (아마). 본관은 이 값을 따지지 않는다.
8. 가게 입구 **"출입 제한 등급" 딱지** 는 본사가 가게를 차릴 때 정한다. 평범한 가게는 0 — 에뮬레이터가 5 를 붙이면 가게 전체가 다른 길로 간다.
9. 화방 개점 뒷몫은 서류 정리뿐이고, 마지막에 **뒷방 직원 한 명 + 신호등 둘** 을 구해야 문을 연다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 421c585 -- runs/03_sprint/flow.md`)

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | gdi32 · gdi32full · gdiplus · user32 · win32kbase · win32kfull 줄에 run153 없음 | 여섯 줄에 run153 보탬 (다시 analyze 없음) + 근거 줄 (run153) 한 줄 (test.exe 는 안 올림 · `GdiSharedHandleTable` (null) `[관찰]`) |
| 3장 gdi32 하위 절 (run86) | 게시판 위치만 | 한 줄 보탬: 블록은 `GreUpdateSharedDevCaps → vGetDeviceCaps` 가 `GDIINFO` 에서, `+0x180094` 는 `LanguagePack` 값, `+0x180098` 은 1 `[정적]` |
| 3장 메시지 상자 하위 절 (run142) | GDI 벽 (`+0xfc` · `+0x153`) 남음 | 한 줄: **"메시지 상자의 GDI 벽은 정적으로 닫힘"** `[정적]` |
| 3장 공유 핸들 표 하위 절 (run144) | "표를 syscall 앞에서 보는 함수" 에 `GetTextExtentPointW` 포함 | 한 줄: **`GetTextExtentPointW` 는 좁힘** (`GetDCDpiScaleValue == 1` 먼저), N2 ~ N4 닫힘 · N5 반만 `[정적]` |
| 3장 화면 DC 하위 절 (run147) | 틀 내용 · `+0x114` · 그 뒤 벽 N2 `[공백]` | 한 줄: **닫힘** (스톡 0 · 7 · 13, `gbLpk`, `InitImagingLibrary` 늘 1) `[정적]` |
| **3장 새 하위 절** | — | "**메시지 상자 글 폭 · 세션 기본 `DC_ATTR` 틀 · 필터 값 5 · gdiplus 시작 뒷몫**" — 비유 (글꼴 자 서랍) · 나무 · `[정적]` 일곱 + `[공백]` 하나 (N5) |
| 4장 | run144 정적 줄 · run147 동적 · run147 정적 남음 | run144 정적 줄 표시 넷 · run147 동적 한 마디 · run147 정적 **해결됨 → run153** · **Q278b 해결됨** 줄 · **run153 새로** 줄 둘 (남은 것 · PE_emulator 쪽) |
| 5장 | — | run153 두 줄 (근거 run · 요청서 번호 표) |

- **그림 (1장) 은 안 바뀌었다.** test.exe 초기화 밖 이야기라서다.
- 태그로 보면: run147 까지 `[공백]` 이던 "세션 기본 틀 내용 · 어느 스톡인지 · `+0x114` · gdiplus 그 뒤 벽 · `NtUserGetDCEx` 0 조건" 이 `[정적]` 이 됐다. 글꼴 스톡 13 만 `[추측: 오프셋 일치]`, 진짜 화면 드라이버 `GDIINFO` 와 N5 출처는 `[공백]` 그대로.

### 큰 그림에서 어떻게 이어지나

- **run142 → run144 → run147 → run153 이 메시지 상자 하나로 이어졌다.**
  - run142: 메시지 상자는 GDI 답이 0 이면 조용히 0.
  - run144: 그 GDI 답은 **번호표 대장** 을 먼저 본다.
  - run147: 도화지 번호표의 **종류 자리** · 메모장 크기가 나왔다.
  - run153: 대장을 지난 뒤 **글 폭을 재는 길** 과 **0 이 되는 곳 셋** 이 나왔다. → 메시지 상자의 GDI 벽은 정적으로 **다 닫혔다.**
- **gdiplus 쪽** 은 개점 체크리스트가 **끝까지** 보인다. 마지막 필요한 것은 이벤트 둘 · 스레드 하나. 다음 벽 후보는 글꼴 열거 (이번 범위 밖).
- **PE_emulator 쪽으로는**: 메시지 상자 12 샘플을 풀 최소가 "HDC + 대장 줄 + 메모장 위에 **syscall 두 개 (`NtGdiGetTextMetricsW` · `NtGdiGetTextExtent`) 에 0 아닌 값**" 으로 좁혀졌다. 서랍 (CFONT) 흉내는 필요 없다.
- **test.exe 쪽으로는**: 커널 칸 `EPROCESS+0x6e8` (필터 속성) 이 test.exe 도 가진 짐 칸 후보로 하나 떠올랐다. 값은 아직 안 봤다 (4절 짐 지도).

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 — 디버거 필요 → **Q277** (같은 VM GUI 세션)

test.exe 로는 못 본다. MessageBox · gdiplus 를 부르는 GUI 프로세스가 필요하다. run 문서 "아직 확인 필요" 동적 1 ~ 7.

1. **진짜 Windows 는 서랍 길을 타나, 되돌이를 타나** — `GdiGetCharDimensions +0xd7` · `+0x11e` · `+0x133` · `+0x1c5` · `+0x17b`, `pcfLocateCFONT` 가지 자리들.
   - 왜 못 알았나: 정적은 "어느 칸을 보고 어디로 가나" 만 보여 준다. 사본이 실제로 있는지는 실행 때 값.
2. 그 DC 의 `DC_ATTR` 0x170 B 가 2.7 표와 같은가, 스톡 핸들이 `GetStockObject(0 · 7 · 13)` 과 같은가 (글꼴 13 의 `[추측]` 을 닫을 곳).
3. win32k: `NtUserGetDCEx +0x62` (`[세션+0xace8]`) · `_GetDCEx +0xa21` · 세션 틀 0x1b0 B · 게시판 0x98 B · `GDIINFO` 0x140 B.
4. 언어팩: `InitializeGre +0x4bf` 직후 `+0x180094` 값.
5. 필터: `PspAllocateProcess +0x1f11` 의 `ecx` — test.exe · 일반 GUI exe 둘 다 0 인가.
6. N5: `kernelbase!GetProcessVersion +0x3b` 콜스택 — gdiplus 시작 길에서 닿는지.
7. 스레드: `CreateThreadAndAddRefDll +0x8c` · `InternalNotificationStartup +0x218` · `+0x2ae` · `+0x2c6`.

### 디버거 없이 떠 올 것 → 380 **2 단계 VM 수집 스크립트** 재료

run 문서 1 ~ 6. 에뮬레이터 대답 값을 정할 재료다.

1. `GetDeviceCaps(화면 HDC, 0 ~ 121)` 전부 — StubDisp 쪽인지 진짜 드라이버인지 가늠.
2. **메시지 상자 글꼴과 폭** — `lfMessageFont` → `GetTextMetricsW` · `GetTextFaceW` · `GdiGetCharDimensions` · `GetTextExtentPoint32W` · `GetDpiForSystem`. **에뮬레이터 `NtGdiGetTextMetricsW` · `NtGdiGetTextExtent` 대답 재료.**
3. 글꼴 레지스트리 · 파일 (`…\CurrentVersion\Fonts` · `FontSubstitutes` · `FontLink\SystemLink` · `WindowMetrics` · `C:\Windows\Fonts`).
4. 언어팩 · 로케일 — `LanguagePack` 값 · `GetSystemDefaultLCID` · **`GetACP`** (949 면 2.4 의 4e 길) · `GetSystemDirectoryW`.
5. DPI · 필터 — DPI 인식 셋 · `GetProcessMitigationPolicy(ProcessSystemCallFilterPolicy)` (test.exe · 일반 GUI exe 비교) · Edge 계약 있나.
6. 스톡 · 시스템 색 — `GetStockObject(0 · 7 · 13)` · `GetSysColor(1 · 15 · 16 · 20)` · `GetVersionExA`.

### 정적으로 남은 것 → 결과서 "다음 요청 후보" (대기열 자리는 오케스트레이터가 정함)

1. N5 마무리 — kernelbase `GetVersionExW` 의 선택 함수 셋 · `GetProcessVersion` 을 부르는 kernelbase 밖 줄.
   - 왜 못 알았나: 이번엔 kernelbase 를 Ghidra 로 안 열고 `[objdump]` 로 `call` 만 셌다.
2. 글꼴 사본의 "진짜 꼴" (`pcfCreateCFONT` 칸 · `NtGdiGetWidthTable` 인자 · win32kfull 쪽 `NtGdiGetTextMetricsW` 등) — **에뮬레이터가 서랍 길을 정말 필요로 할 때만.**
3. 글꼴 열거 (gdiplus `GpInstalledFontCollection` 첫 사용) — 다음 gdiplus 벽 후보.
4. `PspAllocateProcess` Win32k 필터 속성은 누가 넣나 · 필터 5 의 syscall 표.
5. `[세션+0xace8]` 뜻 · `UserGetMonitorDC` · `DrvGetHDEV` · DPI 100% 아닐 때의 `GreGetDeviceCapsInternal`.
6. sast/151 (user32 · win32k) 로: user32 필터 5 의 25 곳 본문 · `SoftModalMessageBox` DPI 쪽.

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- 진짜 화면 드라이버가 `GDIINFO` 를 어떻게 채우나 `[공백]` (간접 호출).
- 프로세스 DPI 문맥 `[W32PROCESS+0x10c]` 의 처음 값 `[공백]`, `hdcOpenDCW` 프린터 길 뒷부분 `[공백]`.
- 칸 이름 (`TEXTMETRIC` 칸 · `DC_ATTR` 칸 · 스톡 이름 · `DCX_*`) 은 `[추측]`. 오프셋 · 줄만 `[정적]`.
- `GetSystemDefaultLCID` 의 kernelbase 안 · gdiplus ETW 등록 (`+0x7bc`) `[공백]`.
- 하네스 쪽 제안 하나: Ghidra export 의 정규식 자리에 `rva:` 를 `|` 로 섞으면 `rva:` 쪽이 **조용히 무시된다** — `ghidra-headless-export` 스킬에 "`rva:` 는 단독으로만" 한 줄 추가 제안 (오케스트레이터 · 사용자 몫).

### 짐 지도

- **새 짐 칸 없음, 2장 숫자 그대로일 것** (run 문서 "짐 지도 반영 거리"). `baggage.md` 는 안 고쳤다.
- 다만 커널 칸 **`EPROCESS+0x6e8` (Win32k 필터)** 은 test.exe 도 가진 칸이다. 짐 목록에 이 칸이 있는지 · 값이 0 인지는 확인 안 함 `[공백]` → 짐 지도 바퀴 때 사진 (`ms/logs/04_baggage-read_P1a.txt` 류) 에서 한 번 보기 (run 문서 "아직 확인 필요" 정적 7, flow-writer 보탬).

---

## 5. 더 읽기

- run 문서: [`run153-gdi-font-dc.md`](run153-gdi-font-dc.md)
  - 1절 (글 폭 길 표) · 2절 (`pcfLocateCFONT` 가지 표) · 3절 (`NtUserGetDCEx` 0 조건) · 4절 (세션 틀 칸 표) · 5절 (`GDIINFO` · 게시판 · 언어팩) · 6절 (필터 값 5) · 7절 (gdiplus 뒷몫) · 8절 (win32k 몫) · 9절 (그 밖) · "PE_emulator 쪽" (최소 목록 17 ~ 20) · "flow-writer 대조" · "앞 run 고쳐 읽기" · "run152 대조"
- 요청서: [`_workspace/sast/150_gdi-font-dc.md`](../../_workspace/sast/150_gdi-font-dc.md)
- 결과서: [`_workspace/sast/150_gdi-font-dc_result.md`](../../_workspace/sast/150_gdi-font-dc_result.md) — "먼저 읽는 한 줄 답" 1 ~ 6, 표 (A) (4a ~ 4i) (B) (C) (D1) (D3) (E) (F) (F2) (G1 ~ G4) — (4i) 는 가지가 아니라 `bComputeTextExtent` 표, "PE_emulator 쪽", "동적으로 확인할 것", "다음 요청 후보"
- 원 요청: `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 차례 ④ · `367_internals-gdi-display-dc.md` "다음"
- flow.md (`git show 421c585 -- runs/03_sprint/flow.md`):
  - 3장 "메시지 상자 글 폭 · 세션 기본 `DC_ATTR` 틀 · 필터 값 5 · gdiplus 시작 뒷몫" 하위 절 (새로)
  - 3장 gdi32 (run86) · 메시지 상자 (run142) · 공유 핸들 표 (run144) · 화면 DC (run147) 하위 절의 run153 줄
  - 4장 Q278b 해결됨 · run153 새로 둘
- 앞 해설: [run142](run142-user32-connect-해설.md) 2.8 (메시지 상자 GDI 답) · [run144](run144-gdi-startup-해설.md) 2.6 · 2.11 · 2.13 (핸들 비트 · 스톡 · 표가 비면) · [run147](run147-gdi-display-dc-해설.md) 2.7 ~ 2.10 · 2.12 (메모장 · 사무실 기본 양식 · 게시판 · 연습장) · [run86](run86-gdi32-init-해설.md) 2.10 (진열장) · [run85](run85-user32-init-해설.md) 2.5 (`gpsi` · DPI)

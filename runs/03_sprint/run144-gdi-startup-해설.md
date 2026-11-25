# run144 gdi-startup — 해설

> 이 글은 [run144 문서](run144-gdi-startup.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q270** — PE_emulator 요청 "`GdiplusStartup` 이 무엇을 보고 1 로 나가나 · gdi32 가 GDI 핸들을 믿을 때 공유 핸들 표의 어느 칸을 보나 · 스톡 객체 핸들 꼴 · (되면) 표를 syscall 앞에서 보는 함수" (`_workspace/plan.md`, 이번에 **끝 (run144)**)
  - 원 요청 `../PE_emulator/_workspace/05_tuning_planner/343_internals-gdi-startup.md`, 튜닝 `343_tune16.md` GP1 · ST1 (요청 쪽 에뮬레이터 숫자):
    - GP1: `GdiplusStartup` 을 부른 **110** 샘플이 **110 / 110** 1 을 받음.
    - ST1: 스톡 객체 칸이 비어 `GetStockObject` 0 을 받은 **179** 샘플. 칸을 채우는 패치 (AT03) 는 **보류** — 채우면 `RegisterClassEx` 가 "매개 변수가 틀립니다" 로 실패해서.
  - 사용자 결정 2026-11-25 (PE_emulator D154) 요청 넷의 **넷째 · 마지막**. 정적만.
    - 이것으로 D154 요청 넷 (Q267 run141 · Q268 [run142](run142-user32-connect-해설.md) · Q269 [run143](run143-mountdev-name-해설.md) · Q270 run144) 이 **모두 끝났다**.
  - run142 가 넘긴 GDI 몫 (결과서 N4 — DC · 글꼴 · 글자 폭) 도 이 바퀴가 받았다.
  - 결과서 판정 **분석성공** — 질문 1 · 2 닫음, 질문 3 은 **읽은 함수 한정** 으로 닫음.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q277** (Q162 · Q273 과 같은 세션)
    - 정적 → **Q278** (N1 `CreateICA("DISPLAY")` 가 **바로 다음 벽**, 가장 급함)
- 근거:
  - run 문서 [`run144-gdi-startup.md`](run144-gdi-startup.md) (가장 중요)
  - 요청서 [`_workspace/sast/141_gdi-startup.md`](../../_workspace/sast/141_gdi-startup.md) · 결과서 [`141_gdi-startup_result.md`](../../_workspace/sast/141_gdi-startup_result.md)
    - **gdiplus 를 처음 analyze** 했다 (`System32\GdiPlus.dll`, PDB 받음, 50초 안쪽, 함수 5239 · `FUN_` 0).
    - gdi32 · gdi32full · user32 · win32kbase 는 이미 analyze 돼 있어 `^이름$` export · refs 만 했다.
  - flow.md 변경: docs 커밋 `8a1bf16` (`git show 8a1bf16 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 user32 · gdi32 · gdiplus 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run142 · run143 과 같은 줄).
   - 사진의 PEB `GdiSharedHandleTable` 도 (null) 이다 `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:1779`, `_P2.txt:2489`, run86 과 같은 줄). 이번에 다루는 표는 test.exe 에선 **생기지도 않는다.**
   - 그래서 이번 이야기는 **"`main` 뒤에 그림을 그리는 프로그램이 처음 그림 도구를 만들고 믿을 때"** 의 일이다.
2. **요청 쪽 숫자 · 로그는 에뮬레이터 것이다.**
   - "110 / 110", "핸들 `0x20800`", "`NtGdiCreateRectRgn` → `NtGdiDeleteObjectApp` → … → 1", "AT03 실험" 같은 것은 PE_emulator 로그 · 실험이다. 진짜 Windows 를 본 게 아니라서 `[관찰]` 이 아니다. 이 글에서는 "에뮬레이터 로그" 라고만 부른다.
3. **오프셋 읽는 법** (앞 해설들과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문 "`+0x1c3 CreateRectRgn`" · "`+0x19c` 종류 비교" 처럼 적힌 숫자 = **함수 안 그 줄** (call 자리 포함).
   - "`+0x1d9 JZ → +0x7d1`" 은 그 줄과 점프가 가는 곳.
   - 콜스택으로 본 적은 없다 (정적만).
4. **값은 하나도 안 봤다.**
   - 진짜 Windows 의 표 칸에 무슨 숫자가 들어 있는지, 진짜 핸들 값이 얼마인지는 이번 범위 밖이다 (요청대로 — 동적 Q277 몫).
   - 이번에 본 건 "**코드가 표의 어느 칸을 읽고, 무엇과 비교하고, 안 맞으면 어디로 가나**" 뿐이다.
   - 2.6 · 2.11 의 "스톡 핸들 예 `0x01900010`" 같은 값은 **비트 규칙으로 만든 예** 다. 진짜 값이 아니다 (run 문서도 그렇게 적었다).
5. **비유 이름이 run 문서와 조금 다르다.**
   - run 문서 "결과" 비유: 공유 핸들 표 = **출입증 대장**, 핸들 = **출입증**, 칸 = "번호 · 주인 · 종류 · 보관함 열쇠" 한 줄 (run 문서는 이 비유를 `[추측: 비유]` 로 둔다).
   - 이 글은 앞 해설들 (run86 · run142) 과 맞춰 핸들 = **번호표** (run86 2.10 "핸들 (번호표)") 로 부르고, 공유 핸들 표 = 진열장 앞쪽에 붙은 **번호표 대장** 으로 쓴다. 그림만 다르고 사실은 같다 (아래 "비유").

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- TEB = 직원 **개인 안내판** → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.2
- **run86 에서 푼 것** → [run86 해설](run86-gdi32-init-해설.md)
  - gdi32 = 그림 도구 건물 **앞 동**, gdi32full = **본관**, 앞 동의 6바이트 `JMP` = 본관으로 가는 **통로** → 맨 위 "비유" · 2.1
  - **지연 가져오기** (처음 부를 때 관리실에 물어 칸을 채움, `NtProtectVirtualMemory` 두 번) → 2.6
  - `NtGdiInit2` = 그래픽 부서에 거는 **첫 신청**, 반환값 = **접수 번호** (0 이면 개점 포기) → 2.9
  - GDI 공유 메모리 (PEB `+0xf8`) = **그림 도구 진열장**, **DC** = 도화지 + 그 도화지 전용 도구 세트, **스톡 객체** = 처음부터 있는 공용 붓 · 펜 → 2.10
- **run121 에서 본 것** → [run121 해설](run121-teb-other-dlls-해설.md) 2.6 — TEB `GdiTebBatch` (`+0x2f0`) 는 gdi32 에선 `InternalDeleteObject` · `DeleteRegion` 둘만 쓴다 (이번 줄과 같다).
- **run131 에서 푼 것** → [run131 해설](run131-user32-regclass-해설.md)
  - 창 클래스 = **창 설계도 이름** (설계도엔 창 함수 · **배경 붓** · 메뉴 이름이 적힌다) → 1절 "새 말 — 창 클래스"
  - `RegisterClassExWOWW` 가 커널 부르기 **전** 에 거르는 `0x57` 다섯 경우 (그중 하나가 "잘못된 브러시") → 2.4
- **run142 에서 푼 것** → [run142 해설](run142-user32-connect-해설.md)
  - TEB `+0x78` (그래픽 손님 등록 칸) · 문지기 · GDI 답 (메시지 상자의 "조용한 0") → 2.3 · 2.8

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, PEB = 가게 **안내판**, TEB = 직원 **개인 안내판**, 커널 = **관리사무소**, syscall = **신청 창구**.
- user32 = **창 담당 건물**, gdi32 = 그림 도구 건물 **앞 동**, gdi32full = **본관**, win32k = 관리사무소 **그래픽 부서**.
- GDI 공유 메모리 (PEB `+0xf8`) = 그래픽 부서가 가게 땅에 걸어 주는 **그림 도구 진열장**. 그 위치가 안내판 `+0xf8` 칸에 적힌다 (run86).
- 이번에 새로 붙는 그림:
  - **핸들 = 번호표.** 그래픽 부서는 그림 도구 (붓 · 영역 · 도화지 …) 를 하나 만들 때마다 직원에게 **번호표** 만 준다.
  - **공유 핸들 표 = 진열장 앞쪽에 붙은 "번호표 대장".** 그래픽 부서는 번호표를 줄 때마다 대장 한 줄에 "**주인 · 번호표 윗자리 · 종류 · 보관함 열쇠**" 를 적어 둔다 (run 문서 비유의 "출입증 대장").
  - 본관 (gdi32 · gdi32full) 은 번호표를 받으면 **대장을 펴서 그 줄이 맞는지 먼저 본다.** 대장 줄이 비어 있으면 "가짜 번호표" 로 보고 버린다.
  - 진열장의 **스톡 칸** (run86 의 "기본 붓 · 기본 펜" 자리) 은 "공용 도구의 번호표 꽂이" 다. 꽂이에 꽂힌 번호표도 **대장에 줄이 있어야** 진짜로 통한다.
  - 보관함 열쇠 (칸 `+0x10`) 는 **접수 번호 (`gCookie`) 로 잠가 둔** 쪽지다. 풀어야 보관함 위치가 나온다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| ① `GdiplusStartup` 이 1 로 나가는 첫 가지는 | `InternalGdiplusStartup +0x1c3 CreateRectRgn(0,0,1,1)` 이 **NULL** 을 받아 **`+0x1d9 JZ → +0x7d1`** → 1. 그 앞 시스템 정보 · 버전 묻기는 **void** 라 분기에 안 쓰인다 `[정적]` (2.4) |
| ② NULL 은 누가 만드나 | gdiplus 가 아니라 **gdi32 `CreateRectRgn` 안.** `NtGdiCreateRectRgn` 이 준 핸들로 **PEB `+0xf8` 표 칸을 다시 읽어** 종류 · 윗 16비트 · 주인 · 사용자 쪽 포인터를 보고, 하나라도 아니면 `DeleteRegion` (= 로그의 `NtGdiDeleteObjectApp`) 을 부르고 NULL `[정적]` (2.5) |
| ③ 표는 어떻게 생겼나 | **PEB `+0xf8` 그대로 시작, 칸 0x18 B, 칸 번호 = 핸들 `& 0xffff`.** 칸 `+0x8` 주인 · `+0xc` 핸들 윗 16비트 · `+0xe` 종류 · `+0x10` 가려진 사용자 쪽 포인터 (칸 이름은 `[추측]`) `[정적]` (2.6) |
| ④ `GdiValidateHandle` 은 무엇을 보나 | **칸 `+0xc` == 핸들 윗 16비트** · **칸 `+0x8` (& ~1) == PID 또는 0** 만. 종류 · 포인터는 **안 본다** `[정적]` (2.7) |
| ⑤ `RegisterClassEx` 는 언제 거절하나 | 배경 붓이 `0x1f` 보다 크면 `GdiValidateHandle` 을 부르고, 거절 + 앱 버전 > 3.0 이면 `0x57` + 0 `[정적]` (2.8) |
| ⑥ AT03 은 왜 실패했나 | **스톡 배열만 채우고 표 칸은 0** 이라 윗 16비트 비교에서 FALSE `[정적]` (2.9) |
| ⑦ 에뮬레이터 영역 핸들 `0x20800` 은 | 표 칸이 없을 뿐 아니라 **종류 비트도 틀렸다** (`2`, 영역은 `4`). 꼴은 `번호 \| 0x040000 \| (고유 << 24)` `[정적]` (2.10) |
| ⑧ 표를 syscall 앞에서 보는 함수 | `DeleteObject` · `SelectObject` · `GetObjectType` · DC 쪽 넷. 안 보는 것: `GetStockObject` · `GetObjectW` `[정적]` (2.13) |
| ⑨ 첫 가지를 넘기면 | **다음 벽** = `+0x280 CreateICA("DISPLAY")` → `+0x296 JZ → +0x7d1` (Q278 N1) `[정적]` (2.4) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**초기화가 끝난 뒤, 프로그램이 그림 라이브러리를 켜거나 창 설계도를 올릴 때** 다. test.exe 에는 없는 옆길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → LdrpInitializeProcess
 │    └─ (2.7) dll 전원 켜기
 │          └─ (user32 를 올리는 프로세스라면) user32 DllMain → GdiDllInitialize
 │                └─ NtGdiInit2 → 그래픽 부서가 진열장 (PEB +0xf8) 을 걺   ← run86
 └─ … → NtContinue (은퇴) → exe 진입점 → (CRT) → main
                                                   └─ (그림을 그리는 프로그램이라면)
                                                       ├─ gdiplus!GdiplusStartup         ← 이번 (run144)
                                                       │    └─ gdi32!CreateRectRgn → 표 칸 검사
                                                       ├─ user32!RegisterClassExW         ← run131 길 + 이번 (배경 붓 검사)
                                                       │    └─ GdiValidateHandle → 표 칸 검사
                                                       └─ MessageBoxW → … DC · 글자 폭     ← run142 길 + 이번 (DC 칸)
```

- run86 은 **진열장을 거는** 쪽이었다. 이번은 그 진열장의 **앞쪽 (번호표 대장)** 을 **읽는** 쪽이다.
- flow.md 에서도 3장 **창 API 하위 절 (run142) 바로 다음** 에 새 하위 절 "GDI 공유 핸들 표 · `GdiplusStartup` 첫 가지 · 배경 붓 검사" 로 들어갔다.
- 프로그램 쪽에서 부르는 일이라 CLAUDE.md 범위로 치면 "`main` 안에서 OS 를 다시 부르는 화살표" 쪽이다. 그래서 그림엔 안 넣었다.

**새 말 — GDI+ (gdiplus)**: GDI 위에 얹힌, 더 편하게 그림을 그리게 해 주는 라이브러리 `[추측: 일반적으로 알려진 설명]`. 쓰기 전에 `GdiplusStartup` 으로 한 번 켜야 한다. 비유: 그림 도구 건물 옆에 새로 들인 **고급 화방**. 화방은 문을 열 때 그림 도구 건물에서 도구 몇 개를 시험 삼아 빌려 본다.

### 왜 이 질문이 지금 왔나

- PE_emulator 는 진짜 nt · ntdll 이름으로 짓는 에뮬레이터다. win32k (그래픽 부서) 는 없고, `NtGdi*` syscall 은 임시 stub 이나 기본 대답을 준다.
- 막힘 둘 (에뮬레이터 로그 · 실험, 원 요청):
  - **GP1**: `GdiplusStartup` 이 **110 / 110** 실패 (1).
    - 로그의 syscall 차례: 메모리 몇 개 → `NtQuerySystemInformation(0)` · `(1)` → `NtQueryInformationProcess(−1, 0x25)` → `NtQueryLicenseValue` 둘 → `NtGdiCreateRectRgn(0,0,1,1)` (임시 stub — 핸들 `0x20800`) → `NtGdiDeleteObjectApp(0x20800)` (0) → `NtProtectVirtualMemory` 둘 → `NtFreeVirtualMemory` → **1**.
    - 후보가 여럿이었다: 핸들 값의 종류 비트? `NtGdiDeleteObjectApp` 의 0? `0x25` 출력 칸? 라이선스 값? 공유 핸들 표 칸?
  - **ST1 · AT03**: 스톡 칸이 비어 `GetStockObject` 가 0 (179 샘플). 그래서 스톡 붓 칸에 `0x21400 + i*0x400` 을 넣어 봤더니 (AT03), 이번엔 `RegisterClassEx` 가 "매개 변수가 틀립니다" 로 실패. 칸이 0 이면 오히려 성공.
- 둘 다 "**gdi32 가 GDI 핸들을 언제 진짜로 믿나**" 하나로 모인다.

### 무엇이 궁금했나

1. **첫 가지**: `GdiplusStartup` 이 1 로 나가는 첫 가지는 무엇의 결과를 보나? 성공하려면 무엇이 있어야 하나?
2. **핸들을 믿는 조건**: `GdiValidateHandle` 이 표에서 읽는 칸 · 칸 번호 식 · 통과 조건, `RegisterClassExWOWW` 의 배경 붓 검사, 스톡 핸들 꼴.
3. (되면) `DeleteObject` · `SelectObject` · `GetObjectW` 가 syscall 앞에서 표를 보나?

### 바퀴 전에 알던 것

- `[정적]` (run86) PEB `+0xf8` → GDI 공유 메모리. **최소 `0x180150` B** 읽기 가능, `+0x180140` (DC 붓) · `+0x180148` (DC 펜) 0 아님. 스톡 칸 `+0x1800b0 + 8*i` — 뜻은 "스톡 객체 핸들 표 `[뜻 추측]`".
  - `GdiProcessSetup` 이 gdi32 수첩 `pGdiSharedMemory` · `pGdiSharedHandleTable` · `pGdiDevCaps` 를 쓴다는 것도 알았다.
  - 하지만 **번호표 대장 (핸들 표 칸) 의 모양은 몰랐다** (run86 정적 ⑤ "핸들 표 항목 모양").
- `[정적]` (run86) `DllMain +0x2e NtGdiInit2` 반환 → gdi32 수첩 `gCookie`.
- `[정적]` (run131) `RegisterClassExWOWW` 는 커널 부르기 전 `0x57` 로 거르는 경우가 다섯 — 그중 하나가 "잘못된 브러시". 그 **조건 줄은 안 봤다**.
- `[정적]` (run142) 메시지 상자 길의 GDI 쪽 (DC · 글꼴 · 글자 폭) 은 "조용한 0" 을 낼 수 있다 — 무엇을 보는지는 N4 로 이 바퀴에 넘김.
- `[정적]` (run121) `GdiTebBatch` · `GdiBatchCount` 를 gdi32 에서 쓰는 건 `InternalDeleteObject` · `DeleteRegion` 둘뿐.
- `[정적]` (run113) PEB `GdiHandleBuffer` (`+0x140`) 를 쓰는 곳 = win32kbase `GdiProcessCallout +0xfe`.

---

## 2. 찾은 것

### 2.1 한눈에 — 화방 개점 · 번호표 확인 · 창 설계도 검사 `[정적]`

run 문서 "호출 관계" 를 줄였다 (나무 숫자 = ret 자리).

```
gdiplus!GdiplusStartup (0xe1170)
 └─ +0x77  InternalGdiplusStartup (0xe1380)              실패 가지 18곳 → 모두 1
            ├─ +0x2a   HeapCreate                       NULL → 1
            ├─ +0x190  OSInfo::Initialize · +0x1b9 InitVersionInfo     void — 결과 안 봄
            ├─ +0x1ca  gdi32!CreateRectRgn(0,0,1,1)      NULL → +0x1d9 → +0x7d1 → 1   ← 첫 가지
            │           ├─ +0x74   hGetPEBHandle(2,0)    PEB+0x140 캐시 (비었음 → 0)
            │           ├─ +0x174  NtGdiCreateRectRgn    ── SYSCALL → H
            │           │          표 칸 [H & 0xffff] 검사: 종류 · 윗 16비트 · 주인 · 포인터
            │           └─ +0x127  DeleteRegion(H)       ← +0x116 (검사 실패)
            │                       TEB+0x78 == 0 → NtGdiDeleteObjectApp ── SYSCALL
            ├─ +0x287  CreateICA("DISPLAY")              NULL → +0x296 → 1   ← 다음 벽 (Q278)
            └─ +0x7d6  InternalGdiplusShutdown (실패일 때)

user32!RegisterClassExWOWW (0x1c99c)     +0x12d hbrBackground > 0x1f 일 때만
 └─ +0x2df gdi32!GdiValidateHandle ⇒ gdi32full!GdiValidateHandle (0x70180)
            +0xc == H>>16 · (+0x8 & ~1) == PID 또는 0         (종류 · 포인터 안 봄)
           FALSE + 앱 버전 > 0x300 → RtlSetLastWin32Error(0x57) → 0
```

- `⇒` 는 지연 가져오기 통로 (run86 2.6) 를 건너뛴다는 표시다.

비유: 고급 화방이 문을 열면서 그림 도구 건물에 "1×1 크기 영역 하나 만들어 주세요" 한다. 본관은 그래픽 부서에 신청해서 번호표를 받고, **진열장 앞 번호표 대장을 펴서 그 줄을 확인** 한다. 대장 줄이 비어 있으면 번호표를 버리고 "못 만들었어요 (NULL)" 라고 화방에 말한다. 화방은 그 말 한마디에 개점을 접는다 (1).

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ +0x1ca  gdi32!CreateRectRgn(0,0,1,1)      NULL → +0x1d9 → +0x7d1 → 1   ← 첫 가지`

- **누가**: gdiplus 의 `InternalGdiplusStartup` (화방 개점 준비 담당).
- **어디서**: 함수 안 `+0x1c3` 에서 call, 돌아오는 자리 (ret) 가 `+0x1ca`. 인자는 바로 앞 `+0x1b9 ~ +0x1c1` 이 `(0,0,1,1)` 로 만든다.
- **누구를**: gdi32 의 `CreateRectRgn` (사각형 영역 만들기).
- **무엇을 위해**: 영역 하나를 만들어 `Globals::CachedGdiRegion` 에 둔다 (`+0x1cf`). 그다음 `+0x1d6 TEST RAX,RAX` · `+0x1d9 JZ → +0x7d1` — 반환이 NULL 이면 정리 (`InternalGdiplusShutdown`) 로 가서 **1** 을 돌려준다.
- 태그: `[정적]` (`ms/disassemble/gdiplus/` 줄).

**새 말 — 영역 (region)**: 화면 위 어떤 모양의 구역을 나타내는 GDI 그림 도구 `[추측: 일반적으로 알려진 설명]`. 여기선 `(0,0)` ~ `(1,1)` 크기 사각형 하나다. 비유: 진열장에서 빌리는 **모양 자** 하나.

#### 줄 2: `+0xc == H>>16 · (+0x8 & ~1) == PID 또는 0         (종류 · 포인터 안 봄)`

- **누가**: gdi32full 의 `GdiValidateHandle` (번호표 검사원).
- **어디서**: 함수 안 `+0x4a ~ +0x53` (윗 16비트 비교) 과 `+0x5c ~ +0x69` (주인 비교) — run 문서 칸 표 기준.
- **누구를**: 직접 부르는 건 아니고, 앞 줄의 `GdiGetEntry` 가 표에서 복사해 온 칸 0x18 B 를 읽는다.
- **무엇을 위해**: "이 번호표가 대장과 맞나" 를 본다. 맞으면 TRUE, 아니면 FALSE (`+0x82`).
- 태그: `[정적]`.

### 2.3 ★ 큰 그림 — 커널이 준 번호표를, gdi32 가 대장에서 다시 찾는다 `[정적]`

이번 바퀴의 뼈대다. 한 걸음씩.

1. 프로그램이 그림 도구 (영역 · 붓 · DC …) 를 만들면, gdi32 는 그래픽 부서에 신청한다 (`NtGdiCreateRectRgn` 같은 win32k syscall).
2. 그래픽 부서는 **핸들 `H`** (번호표) 하나를 돌려준다.
3. gdi32 는 그 번호표를 **그대로 믿지 않는다.** `H & 0xffff` 를 칸 번호로 삼아, **PEB `+0xf8` 이 가리키는 표** 에서 그 칸 (0x18 B) 을 읽는다.
4. 칸에서 넷을 본다 — **종류** (`+0xe`) · **윗 16비트** (`+0xc`) · **주인** (`+0x8`) · **사용자 쪽 포인터** (`+0x10`). 함수마다 넷 중 무엇을 보는지가 다르다 (2.7 표).
5. 하나라도 안 맞으면 그 핸들을 **가짜로 본다.**
   - `CreateRectRgn` 이면 → `DeleteRegion` 으로 치우고 NULL.
   - `GdiValidateHandle` 이면 → FALSE.
   - `DeleteObject` 면 → syscall 도 안 하고 FALSE.

**새 말 — 공유 핸들 표 (의 칸)**: PEB `+0xf8` (`GdiSharedHandleTable`, run86) 이 가리키는 메모리의 **앞쪽 `+0x0 ~ +0x180000`**. 칸 하나가 0x18 B 이고, 핸들마다 한 칸씩 있다 `[정적]`. 커널이 쓰고 유저 쪽 gdi32 가 읽는다. 비유: 진열장 앞쪽에 붙은 **번호표 대장**, 칸 = 대장의 **한 줄**.

- 진짜 Windows 에서 이 칸을 채우는 쪽은 win32kbase `NtGdiCreateRectRgn` (`0xfe1c0`) → `HmgAllocateObjectAttr` (`0xfe380`) · `HmgInsertObjectInternal` (`0x2b4a0`) 다 `[이름만]`. 그래서 **그래픽 부서가 번호표를 주면서 대장에도 적어 두는** 그림이다.
- 에뮬레이터엔 그래픽 부서가 없다. 번호표 (stub 의 `0x20800`) 는 주는데 **대장엔 아무것도 안 적는다** (표가 전부 0). 그래서 gdi32 는 매번 "가짜" 라고 본다.

왜 이렇게 다시 확인하게 짰는지는 코드가 말해 주지 않는다. 코드로 보이는 **효과** 는 "칸이 이상하면 syscall 없이 일찍 거른다" 이다 (`DeleteObject` 가 칸이 없으면 syscall 없이 FALSE, 2.13) `[정적]`. 설계 의도는 `[추측]` 으로 남긴다.

비유: 그래픽 부서가 번호표를 줄 때마다 진열장 앞 대장에 한 줄 적는다. 본관 직원은 번호표를 받으면 대장부터 펴 본다. 우리 에뮬레이터 그래픽 부서는 번호표만 찍어 주고 대장은 비워 둔다. 그러니 본관은 받은 번호표를 족족 "가짜" 로 버린다.

### 2.4 ★ `GdiplusStartup` 의 첫 실패 가지 `[정적]`

#### 차례 (run 문서 1절 표)

| 차례 | 부르는 것 | 보는 결과 | 실패 조건 · 반환 |
|---|---|---|---|
| 0 | `GdiplusStartup` 입구 검사 | 포인터 셋 · 입력 `+0x0` 버전 · `+0x10` | 포인터 NULL 등 → **2** (`+0xeb`). 버전이 1 ~ 3 아님 → **`0x11`** (`+0x48`) |
| 1 | `EnterCriticalSection` · `LibraryInitRefCount == 0` | 두 번째 부름부터는 안 들어감 | — |
| 2 | `InternalGdiplusStartup` → `HeapCreate` | NULL | → **1** (`+0x3c → +0x7cc`) |
| 3 | `OSInfo::Initialize` · `GetModuleHandleA(NULL)` · `InitVersionInfo` | **안 봄 (void)** | 1 을 못 만든다 |
| **4** | **`CreateRectRgn(0,0,1,1)`** | **NULL** | **→ 1 (`+0x1d9 → +0x7d1`) ← 첫 가지** |
| 5 | `CreateICA("DISPLAY")` | NULL | → 1 (`+0x296`) — **다음 벽** (Q278 N1) |
| 6 | `GetDeviceCaps(0x58 / 0x5a)` | ≤ 0 이면 96.0 | 실패 아님 |
| 7 | `GpDevice` · 드라이버 · 표면 · `InitImagingLibrary` · `InitSystemFontsDirs` · `GetGpInstalledFontCollection` · `BackgroundThreadStartup` | NULL / 0 | → 1 (Q278) |
| 8 | TraceLogging 등록 | — | **0 (성공)** |

#### 다른 후보는 왜 아닌가 — 단계별로

1. 요청은 후보 다섯을 냈다: 영역 핸들의 종류 비트 · `NtGdiDeleteObjectApp` 반환 · `0x25` 출력 칸 · 라이선스 값 · 공유 핸들 표 칸.
2. `0x25` 출력 · 라이선스 값: 차례 3 의 `OSInfo::Initialize` · `InitVersionInfo` 안쪽 (kernelbase `GetSystemInfo` · `GetVersionExA` 쪽) 일 것이다 `[추측: kernelbase 줄은 안 읽음]`. 그런데 **어느 쪽이든 그 두 함수는 반환이 void** 라 `InternalGdiplusStartup` 이 결과를 분기에 안 쓴다 `[정적]`. → 1 을 만들 수 없다.
3. `NtGdiDeleteObjectApp` 반환: 이건 **gdi32 `CreateRectRgn` 이 이미 실패를 정한 뒤** 치우는 호출이다 (2.5). 그리고 `DeleteRegion` 은 반환을 안 쓴다. → 원인이 아니라 결과다.
   - "gdiplus 의 정리 (`InternalGdiplusShutdown`) 가 부른 것 아닌가?" — 아니다. 정리 쪽 `+0x25d TEST RCX` · `+0x260 JZ` 는 `CachedGdiRegion` 이 NULL 이면 `DeleteObject` 를 **안 부른다** `[정적]`.
4. 영역 핸들의 종류 비트: `CreateRectRgn` 은 **핸들 값의 종류 비트를 직접 안 본다** — 칸 `+0xe` (종류) 를 4 와 비교할 뿐이다 (결과서). 다만 종류 비트는 나중에 `DeleteObject` 에서 문제가 된다 (2.10).
5. 남은 것 = **공유 핸들 표 칸**. 칸이 0 이라 `CreateRectRgn` 이 NULL → 첫 가지.
6. 에뮬레이터 로그와 맞물림 `[정적 + 요청 쪽 로그]`: `NtGdiCreateRectRgn` 바로 뒤 `NtGdiDeleteObjectApp` 이 나오고, `CreateICA` 안에서 나갈 `NtGdiOpenDCW` 가 **없다**. 차례 4 에서 멈춘 것과 맞는다.

#### 실패 가지 18곳 — 모두 1

- flow-writer 가 `.asm` 으로 셌다: `InternalGdiplusStartup` 에서 실패로 가는 `JZ` 는 **18곳**. `+0x3c` 하나는 `+0x7cc MOV ESI,1` 로, 나머지 17곳은 `+0x7d1` 로 간다.
- `ESI` 를 쓰는 줄은 `+0x55 LEA ESI,[R12+0x1]` · `+0x7cc` · (복원) `+0x7dd` 뿐 → **어느 가지로 실패하든 1** 이다 `[정적]`.
- 그래서 "1 이 나왔다" 는 것만으로는 **어느 벽에서 넘어졌는지 모른다.** 이번엔 로그 차례 (`NtGdiOpenDCW` 없음) 로 차례 4 라고 좁혔다.

#### 로그의 나머지 syscall 은 어디서

- 첫 `NtAllocateVirtualMemory · NtFreeVirtualMemory · NtAllocateVirtualMemory` = `HeapCreate` 안, 끝 `NtFreeVirtualMemory` = `InternalGdiplusShutdown +0x608 HeapDestroy` `[추측]`.
- `NtProtectVirtualMemory` ×2 = 지연 가져오기 풀기 꼴 (run86 A6). `CreateRectRgn` 꼬리의 `GdiTrackHCreate` (지연 가져오기 칸) 쪽이 후보 `[공백: 정적으로 못 정함 → Q277]`.

비유: 화방 개점 체크리스트엔 항목이 18개 있고, **어느 항목에서 걸려도 "개점 실패 (1)" 한 가지 말만** 한다. 앞쪽 "건물 정보 · 버전 묻기" 는 듣기만 하고 체크 표시는 안 한다. 처음으로 체크 표시를 하는 항목이 "모양 자 하나 빌리기" 였고, 거기서 걸렸다.

### 2.5 ★ 첫 가지 안쪽 — gdi32 `CreateRectRgn` 의 칸 검사 넷 `[정적]`

gdi32full 의 `CreateRectRgn` 은 6 B 썽크 (`0x87520`) 이고, 몸 (734 B) 은 gdi32 (rva `0x2950`) 에 있다.

| 차례 | 하는 것 | 줄 |
|---|---|---|
| a | 좌표가 범위 밖이면 `TEB+0x68 = 0x57` 후 0 | `+0xa ~ +0x54` |
| b | `hGetPEBHandle(2, 0)` — PEB `+0x140` 영역 캐시. 새 프로세스는 비어 0 | `+0x6f` · `+0x7a JZ → +0x160` |
| c | **`NtGdiCreateRectRgn(0,0,1,1)`** → `H` | `+0x16d` |
| d · e · f | 칸 번호 계산 · 번호 ≥ `gMaxGdiHandleCount` (`0x1000000`) 면 실패 · 칸 24 B 복사 | `+0x80` · `+0x9d` · `+0xbd` · `+0xc3 JNC → +0x116` · `+0xda` · `+0x181 ~ +0x196` |
| **g** | **종류**: 칸 `+0xe` byte == **4** | `+0x19c` · `+0x1a1 JNZ → +0x116` |
| **h** | **윗 16비트**: 칸 `+0xc` word == `H >> 16` | `+0x1a7 ~ +0x1ae` · `+0x1b3 JNZ → +0x116` |
| **i** | **주인**: (칸 `+0x8` dword & ~1) == `gW32PID` — **0 은 안 됨** | `+0x1b9 ~ +0x1c0` · `+0x1c6 JNZ → +0x116` |
| **j** | **사용자 쪽 포인터**: 칸 `+0x10` ≠ 0, 풀어도 ≠ 0 | `+0x1cc ~ +0x1d4` · `+0x1da ~ +0x1f3` |
| k | 풀린 포인터의 0x18 B 에 영역 모양을 **씀** — 그래서 쓰기 가능해야 함 | `+0x1f9 ~ +0x256` |
| **실패** | e · g · h · i · j 어디서든 **`+0x116`** → `H ≠ 0` 이면 `DeleteRegion(H)` → NULL | `+0x116` · `+0x122` · `+0x129` |

#### 새 말 셋

**새 말 — `gW32PID`**: gdi32 수첩에 적힌 "이 프로세스 번호" 값. `GdiProcessSetup +0x142 ~ +0x14f` 가 TEB `+0x40` (`ClientId`) 값 `& ~3` 을 넣는다 `[정적]`. 비유: 본관 수첩의 "**우리 가게 번호**". 대장 줄의 "주인" 칸과 비교하는 기준이다.

**새 말 — 가려진 포인터 (`gCookie` 로 잠금)**: 칸 `+0x10` 에는 보관함 주소가 **그대로 적혀 있지 않다.** gdi32 는 `ROR (0x40 − (gCookie & 0x3f))` 로 돌린 뒤 `XOR gCookie` 해서 풀어 쓴다 `[정적]`. 그러니 채우는 쪽은 `ROR64(ptr ^ gCookie, gCookie & 0x3f)` 로 잠가 둬야 한다 (flow-writer 대조 7). `gCookie` = `NtGdiInit2` 반환값 (run86). 비유: 보관함 열쇠 쪽지를 **접수 번호로 암호를 걸어** 적어 둔 것. 접수 번호를 아는 본관만 풀 수 있다.

- `gCookie` 의 아래 6비트가 0 이면 돌리는 양이 0 이라 `ptr ^ gCookie` 하나로 끝난다 (결과서 권고).

**새 말 — 영역 캐시 (PEB `+0x140` `GdiHandleBuffer`)**: 미리 만들어 둔 영역 핸들을 쌓아 두는 칸. `hGetPEBHandle` 이 여기서 꺼낼 게 있으면 syscall 없이 쓴다 `[정적]`. 새 프로세스는 개수 칸이 0 이라 꺼낼 게 없고 → `NtGdiCreateRectRgn` 으로 간다. 비유: 진열장 옆 **모양 자 재고 선반**. 비어 있으니 그래픽 부서에 새로 신청한다.

- 그 사이 `hGetPEBHandle +0x57` 이 PEB `+0x140` 첫 qword 에 TEB 주소를 **잠금 (`LOCK CMPXCHG`)** 으로 잠깐 쓰고, `+0x159` 에서 0 으로 되돌린다 (flow-writer 대조 6). → PEB `+0x140` 은 **0 그대로 + 쓰기 가능** 이어야 한다.

#### 실패 뒤 — `DeleteRegion` 은 왜 syscall 을 냈나

1. `DeleteRegion` (`0x1470`) 은 같은 칸 검사를 **한 번 더** 한다 (`+0x10b GdiGetEntry` · `+0x118` 종류 4 · `+0x12a` 윗 16비트 · `+0x13c` 주인).
2. **`TEB+0x78 ≠ 0` · 배치 자리 남음 · 칸 유효** 셋이 다 맞으면 syscall 없이 TEB `GdiTebBatch` (`+0x2f0`) 에 쌓는다. `GdiBatchLimit` 개 모이면 `NtGdiFlush`.
3. 에뮬레이터는 `TEB+0x78 = 0` (run142) 이고 칸도 0 → `+0x2e CMP [TEB+0x78],0` · `+0x33 JZ → +0xd7` → `+0xe9` 에서 **syscall `NtGdiDeleteObjectApp`**.
4. 로그의 `NtGdiDeleteObjectApp(0x20800)` 이 바로 이것이다. 로그와 맞는다.

**새 말 — TEB 배치 (`GdiTebBatch`)**: 작은 GDI 일을 syscall 로 하나씩 보내지 않고 직원 개인 안내판 (TEB) 의 칸에 **모아 뒀다가 한꺼번에** 보내는 것 `[정적: 쌓는 조건 줄]`. 비유: 반납할 도구를 바로 창구에 안 내고 **반납함** 에 모았다가, 차면 한 번에 들고 가는 것.

#### 성공하려면 칸에 무엇이 있어야 하나 (run 문서)

`H` = `NtGdiCreateRectRgn` 반환, `i = H & 0xffff < 0x10000`, 칸 = `PEB[+0xf8] + i*0x18`:

- `+0x8` = PID & ~3 (`gW32PID`)
- `+0xc` = `H >> 16`
- `+0xe` = 4
- `+0x10` = `ROR64(ptr ^ gCookie, gCookie & 0x3f)` (`ptr` = 쓰기 가능 ≥ 0x18 B)
- `H` 의 24 ~ 31비트 = 칸 `+0xd` (0 이 쉬움)
- PEB `+0x140` 은 0 그대로 + 쓰기 가능

비유: 본관 직원이 번호표를 받아 대장 한 줄을 펴고 네 가지를 맞춰 본다 — "**모양 자 맞나 (종류 4)**", "**번호표 윗자리 같나**", "**우리 가게 것 맞나 (주인)**", "**보관함 열쇠 쪽지가 있고 풀리나**". 넷 다 맞아야 보관함에 모양 자의 크기를 적어 넣고 화방에 건넨다. 하나라도 틀리면 번호표를 반납 창구로 보내고 "못 만들었어요" 한다.

### 2.6 대장 모양 — 표 시작 · 칸 크기 · 칸 번호 식 · 핸들 비트 `[정적]`

#### 표 시작과 칸 크기

- **표 시작 = PEB `+0xf8` 값 그대로** (더하기 없음). `gdi32full!GdiProcessSetup +0x431 ~ +0x449` 가 gdi32 수첩 `pGdiSharedMemory` (`0x25778`) 와 `pGdiSharedHandleTable` (`0x257a8`) 에 **같은 값** 을 쓴다. `pGdiDevCaps` 엔 `+0x180000` (`+0x456`, run86).
- **칸 = 0x18 B** — 읽는 쪽이 모두 `LEA RAX,[idx+idx*2]` 뒤 `*8` 꼴이다 (`GdiGetEntry +0x15 / +0x19`, `HANDLE_TO_INDEX +0x20 / +0x2c`).
- 칸 수는 `0x10000` 일 것 `[추측: 나눗셈으로만 — 코드의 상한 비교는 gMaxGdiHandleCount 0x1000000 뿐]`. `+0x180000` 에서 장치 정보 (DevCaps) 가 시작하니까 `0x180000 / 0x18` 로 셈한 것.

그래서 진열장 (GDI 공유 메모리) 을 다시 그리면:

```
PEB+0xf8 →  +0x0       ┐
                       │  번호표 대장 (칸 0x18 B × 0x10000 [추측: 개수])   ← 이번
            +0x180000  ┘  장치 정보 (pGdiDevCaps)
            +0x180094     언어팩 켜짐 표시                                ← run86
            +0x1800b0     스톡 꽂이 (+ 8*i)                              ← run86, 이번에 뜻 [정적]
            +0x180140 · +0x180148   DC 붓 · DC 펜 (i = 0x12 · 0x13)       ← run86
```

#### 칸 번호 식 (`HANDLE_TO_INDEX` — gdi32 `0x2e70` · gdi32full `0x13c60`, 같은 식)

1. `ext = ((H >> 8) & 0xff0000) | (H & 0xffff)` — `H` 의 24 ~ 31비트를 16 ~ 23비트 자리로 옮긴다.
2. `ext < 0x10000` 이면 (곧 `H` 의 24 ~ 31비트가 0 이면) → 칸 번호 = `H & 0xffff`.
3. 아니면, 칸 `[H & 0xffff]` 의 `+0xd` 바이트가 `ext >> 16` 과 같을 때만 `H & 0xffff`. 다르면 `ext` (표 밖) → `NtGdiGetEntry` syscall 로 커널에 묻는다.

- 결과서 팁: 에뮬레이터는 `idx < 0x10000` 이고 칸 `+0xd` 를 `H>>24` 와 맞춰 두면 (0 이 가장 쉬움) 표 읽기에 syscall 이 안 낀다.

#### 핸들 비트 — 번호표에 적힌 자리들

| 비트 | 뜻 |
|---|---|
| 0 ~ 15 | 칸 번호 |
| 16 ~ 22 | **종류** — `0x10000` DC · `0x40000` 영역 · `0x50000` 비트맵 · `0x80000` 팔레트 · `0xa0000` 글꼴 · `0x100000` 붓 · `0x300000` 펜 … |
| 23 | **스톡** (`0x800000`) |
| 24 ~ 31 | 고유 번호 |

- **핸들 윗 16비트 = 칸 `+0xc` word** — win32kbase `HmgpComputeHandleValue +0x4 ~ +0x15` 가 `(칸 +0xc word << 16) | 번호` 로 핸들을 만든다 `[정적]`. 그래서 "윗 16비트 비교" 는 "**이 번호표가 이 대장 줄에서 나온 게 맞나**" 를 보는 것이 된다.
- 스톡 비트는 win32kbase `bSetStockObject +0x33 BTS R10,0x17` 이 켠다 `[정적]`.

**새 말 — 스톡 비트**: 핸들의 23번 비트. 켜져 있으면 "공용 기본 도구" 라는 표시다 `[정적: 켜는 줄 · 쓰는 줄]`. 비유: 번호표에 찍힌 "**공용**" 도장. 이 도장이 있으면 `DeleteObject` 는 지우는 척만 하고 TRUE 를 돌려준다 (syscall 없음, 2.13).

#### 칸 24 B (이름은 `[추측]`, 오프셋 · 검사는 `[정적]`)

| 칸 | 뜻 | 대장 줄 비유 |
|---|---|---|
| `+0x00` qword | 커널 쪽 객체 주소 `[추측]` — 유저 쪽은 복사만, 읽는 줄 못 찾음 (0 이어도 됨) | 그래픽 부서 내부 메모 |
| `+0x08` dword | **주인 프로세스** (하위 1비트 무시) | 주인 |
| `+0x0c` word | **핸들 윗 16비트** (종류 · 스톡 · 고유). 윗 바이트 `+0xd` 는 `HANDLE_TO_INDEX` 가 따로 읽음 | 번호표 윗자리 |
| `+0x0e` byte | **종류 번호** (DC 1 · 영역 4 · 비트맵 5 · 팔레트 8 · 글꼴 `0xa` · 붓 · 펜 `0x10` …) | 도구 종류 |
| `+0x0f` byte | 비트 1 이 켜지면 `SelectObject(hdc, 글꼴)` 거절 `[뜻 공백]` | — |
| `+0x10` qword | **사용자 쪽 속성 블록 포인터** (가려짐). 영역 → 0x18 B, DC → `DC_ATTR` (≥ 0x130 B) | 보관함 열쇠 쪽지 |

### 2.7 ★ `GdiValidateHandle` — 보는 것 · 안 보는 것 `[정적]`

#### 보는 것 (gdi32full `0x70180`)

1. 핸들이 **0** 이면 → 바로 **TRUE** (`+0x12 → +0x71`).
2. 칸 번호 < `gMaxGdiHandleCount` (아니면 `+0x24 JNC` → FALSE).
3. `GdiGetEntry` 가 칸을 복사해 온다 (음수면 `+0x48 JS` → FALSE). — 2 · 3 의 실패 가지는 flow-writer 대조 4 가 보탰다.
4. **칸 `+0xc` word == `H >> 16`** (정확히).
5. **(칸 `+0x8` & ~1) == `gW32PID` 또는 0.**
6. 다 맞으면 TRUE, 하나라도 아니면 FALSE (`+0x82`).

#### 안 보는 것

- **칸 `+0xe` 종류** — 안 본다.
- **칸 `+0x10` 포인터** — 안 본다.
- 그러니 "붓 자리에 붓 아닌 핸들" 을 넣어도, 대장 줄의 윗자리 · 주인만 맞으면 통과한다 (2.9 의 AT03 이야기와 이어진다).

#### 누가 칸의 무엇을 보나 (run 문서 표를 줄임)

`✓` 검사 · `—` 안 봄 · "또는 0" = 주인 칸 0 도 통과.

| 함수 | `+0xe` 종류 | `+0xc` 윗 16비트 | `+0x8` 주인 | `+0x10` 포인터 |
|---|---|---|---|---|
| `GdiValidateHandle` | — | ✓ 정확 | ✓ PID **또는 0** | — |
| `CreateRectRgn` · `DeleteRegion` | ✓ == 4 | ✓ 정확 | ✓ PID (**0 안 됨**) | ✓ |
| `InternalDeleteObject` (`DeleteObject`) | ✓ == `(H>>16)&0x1f` | ✓ 스톡 비트 빼고 | ✓ PID **또는 0** | 붓 · 펜 배치 때만 |
| `GetObjectType` | — (핸들 비트) | ✓ 정확 | ✓ PID **또는 0** | — |
| `SelectObjectImpl` — 객체 | ✓ == `(h>>16)&0x1f` | ✓ 스톡 비트 빼고 | ✓ PID **또는 0** | — |
| `SelectObjectImpl` — DC · DC 쪽 넷 (`GetDeviceCaps` 등) | ✓ == 1 | ✓ 정확 | ✓ PID (**0 안 됨**) | ✓ |
| `GetStockObject` · `GetObjectW` | — | — | — | — |

이 표에서 읽을 것 둘:

1. **주인 0 을 받아 주는 함수가 따로 있다.** `GdiValidateHandle` · `DeleteObject` · `GetObjectType` · `SelectObject(객체)` 는 받고, `CreateRectRgn` · DC 쪽은 안 받는다. → 스톡 칸은 주인 0 으로 둬도 되지만, 영역 · DC 칸은 PID 를 정확히 넣어야 한다 (2.12).
2. **종류 칸을 `(H>>16)&0x1f` 와 비교하는 함수가 있다** (`InternalDeleteObject` · `SelectObjectImpl`). → 핸들의 종류 비트와 칸 `+0xe` 가 서로 맞아야 한다 (2.10 의 이유).

비유: 번호표 검사원은 대장 줄에서 "**번호표 윗자리**" 와 "**주인 (우리 가게거나, 주인 없는 공용)**" 두 가지만 본다. 도구 종류나 보관함 열쇠는 안 본다. 더 깐깐한 직원 (`CreateRectRgn` · DC 쪽) 은 넷 다 보고, 주인이 비어 있어도 안 받는다.

### 2.8 ★ `RegisterClassExWOWW` 의 배경 붓 검사 `[정적]`

run131 이 "커널 부르기 전 거르는 `0x57` 다섯 경우" 중 하나로 적은 "**잘못된 브러시**" 의 조건을 이번에 줄로 닫았다.

| 차례 | 줄 | 뜻 |
|---|---|---|
| 1 | `+0x114 RtlGetExpWinVer` → `+0x119 MOV EDI,EAX` | 앱이 기대하는 Windows 버전 |
| 2 | `+0x129 MOV RCX,[RBX+0x30]` · `+0x12d CMP RCX,0x1f` · **`+0x131 JA → +0x2d8`** | `hbrBackground`. **0 ~ `0x1f` 는 검사 안 함** |
| 3 | **`+0x2d8 CALL [0x1800b51b8]`** = gdi32 `GdiValidateHandle` | 썽크 `JMP [0x180027bf0]` → 지연 가져오기 → gdi32full `0x70180` |
| 4 | `+0x2e4 TEST EAX,EAX` · `+0x2e6 JNZ → +0x137` | 통과 → 등록 계속 |
| 5 | `+0x2ec CMP EDI,0x300` · **`+0x2f2 JA → +0x326`** | 버전 > 3.0 → `+0x32b RtlSetLastWin32Error(0x57)` · 0 반환 |
| 6 | `+0x2f4 MOV [RBX+0x30],R12` · `+0x2f8 JMP → +0x137` | 버전 ≤ 3.0 → 붓을 0 으로 바꾸고 계속 |

**새 말 — 배경 붓 (`hbrBackground`) 과 COLOR 번호**: 창 설계도 (run131) 에 적는 "창 바탕을 칠할 붓". 진짜 붓 핸들 대신 작은 숫자 (시스템 색 번호 + 1) 를 넣을 수도 있다 — run 문서 "0 ~ `0x1f` (COLOR 번호 + 1)". 비유: 설계도의 "바탕색" 칸에 **붓 번호표** 를 꽂거나, 그냥 **색 견본 번호** 를 적거나.

- `0x1f` 이하 → "색 견본 번호구나" 하고 검사 안 함.
- `0x1f` 초과 → "붓 번호표구나" 하고 번호표 검사원 (`GdiValidateHandle`) 에게 보냄.

**새 말 — 앱 버전 (`RtlGetExpWinVer`)**: user32 의 `RtlGetExpWinVer` (`0x1cd60`) 가 hInstance 의 PE 머리 `+0x48` (Major) `<< 8 | +0x4a` (Minor) 로 만든 값 `[정적]`. hInstance NULL · 머리 없음 · Major 1 이면 `0x30a`. 비유: 앱이 "**나는 Windows 몇 판 기준으로 만들어졌어요**" 하고 머리에 적어 둔 숫자.

- 3.0 이하 (아주 옛 앱) 면 너그럽게 붓을 0 으로 지우고 넘어간다.
- 결과서는 "지금 나오는 앱은 거의 다 서브시스템 4.0 이상이라 이쪽 (`0x57`)" 이라고 적었다.

**새 말 — `0x57` (`ERROR_INVALID_PARAMETER`)**: Win32 오류 번호, 뜻은 "매개 변수가 틀립니다". AT03 실험에서 본 그 문구다.

#### 같은 검사원을 부르는 자리

- user32 안에서 gdi32 `GdiValidateHandle` 을 부르는 곳은 **9곳** (`_refs/0xb51b8.tsv`): `DT_InitDrawTextInfo` · `RegisterClassExWOWW` · `RegisterClassExWOWA` · `ValidateMENUITEMINFO` ×3 · `SetClassLongPtrW` · `SetClassLongPtrA` · `ValidateMENUINFO`. 같은 규칙이 모두에 걸린다.
- run131 의 combase `OleMainThreadWndClass` 는 `hbrBackground = 2` 라 이 검사를 **안 탄다** → run131 에 고칠 것 없음.

비유: 창 담당 건물 등록 창구는 설계도의 "바탕색" 칸을 본다. 작은 숫자면 색 견본 번호로 알고 넘어가고, 큰 숫자면 붓 번호표로 보고 검사원에게 대장 확인을 맡긴다. 검사원이 "가짜" 라고 하면, 요즘 앱에겐 "매개 변수가 틀립니다" 로 돌려보낸다.

### 2.9 ★ AT03 실험은 왜 실패했나 — 단계별로 `[정적]`

AT03 = 에뮬레이터가 스톡 꽂이 (`+0x1800b0 + 8*i`) 에 `0x21400 + i*0x400` 을 넣은 보류 패치.

1. 프로그램이 `GetStockObject(i)` 로 붓을 달라고 한다.
   - `GetStockObject` 는 **꽂이 값만 읽어 그대로 돌려준다** — 대장은 안 본다 (2.11). → `0x21400 + i*0x400` 을 받는다. 여기까진 성공.
2. 프로그램이 그 붓을 배경 붓으로 넣고 `RegisterClassEx` 를 부른다.
3. `RegisterClassExWOWW +0x12d`: 값이 `0x1f` 보다 크다 → `+0x2d8` 로 가서 `GdiValidateHandle`.
4. `HANDLE_TO_INDEX`: 칸 번호 = `0x1400 + i*0x400` (< `0x10000`).
5. 그 칸 (`+0x1e000 + i*0x6000`) 은 에뮬레이터가 **안 채웠다** → 전부 0.
6. `GdiValidateHandle +0x4e`: 칸 `+0xc` word `0` ≠ `H>>16` = `0x0002` → **FALSE**.
7. 앱 버전 > 3.0 → `0x57` + 0 → "매개 변수가 틀립니다".

그럼 **꽂이가 0 일 때는 왜 성공했나**:

1. `GetStockObject` 가 0 을 돌려준다.
2. 배경 붓 0 → `CMP RCX,0x1f` 에서 "색 견본 번호" 쪽으로 빠진다 → 검사 자체를 안 한다.
3. → 등록 성공.

**원인 = "꽂이 (배열) 만 채우고 대장 (표 칸) 은 안 채움".**

비유: AT03 은 공용 붓 번호표를 **꽂이에만 잔뜩 꽂아 두고 대장엔 안 적은** 꼴이었다. 손님이 번호표를 뽑아 창구에 내자, 창구는 대장을 펴 보고 "그런 줄 없어요" 하고 돌려보냈다. 꽂이가 비어 있을 땐 손님이 번호표 없이 (0) 왔고, 창구는 "번호표 없으면 검사할 것도 없죠" 하고 받아 줬다.

### 2.10 ★ 에뮬레이터 영역 핸들 `0x20800` — 종류 비트가 틀렸다 `[정적]`

에뮬레이터 `nt_gdi_create_rect_rgn.cpp` 는 `NtGdiCreateRectRgn` 에 핸들 `0x20800` 을 돌려준다. 틀린 곳이 **둘** 이다 (run 문서 "PE_emulator 쪽").

#### 틀림 ① — 대장 칸을 안 채운다

- 2.5 의 넷 (`+0x8` PID · `+0xc` `H>>16` · `+0xe` 4 · `+0x10` 가린 포인터) 이 없다. → `CreateRectRgn` NULL → `GdiplusStartup` 1.

#### 틀림 ② — 번호표의 종류 자리가 영역이 아니다

한 걸음씩.

1. `0x20800` 의 16 ~ 22비트 (종류) = **`2`**. 영역은 **`4`** (`0x40000`) 다.
2. "칸만 채우면 되지 않나?" — 칸 `+0xe` 를 `CreateRectRgn` 요구대로 **4** 로 두면, `CreateRectRgn` 은 넘는다 (핸들 값의 종류 비트를 직접 안 보니까, 2.4 의 4).
3. 그런데 나중에 프로그램이 `DeleteObject(그 영역)` 를 부르면, `InternalDeleteObject +0x5c / +0x5e` 가 **칸 `+0xe` == `(H>>16) & 0x1f`** 를 본다.
4. `(0x20800 >> 16) & 0x1f` = `2` 인데 칸 `+0xe` = 4 → 안 맞음 → **FALSE** (syscall 도 없이).
5. 곧 **칸과 번호표가 서로 다른 종류를 말하면**, 깐깐한 함수 하나는 통과하고 다른 하나는 막힌다.

#### 고칠 꼴

- 핸들 = **`번호 | 0x040000 | (고유 << 24)`** (번호 < `0x10000`).
- 칸: `+0x08` = PID & ~3 · `+0x0c` = `H >> 16` · `+0x0e` = 4 · `+0x10` = `ROR64(ptr ^ gCookie, gCookie & 0x3f)` (`ptr` = 쓰기 가능 ≥ 0x18 B).

#### 하나 더 — "`0x400` 배수" 근거는 다른 표의 규칙이다

- `nt_gdi_create_rect_rgn.cpp:7` 은 "`0x400` 배수라 핸들 표가 안 주는 번호" 를 근거로 들었다. 그건 **nt 객체 핸들 표** (`ExpInsertLowLevelTableIntoFreeList`) 규칙이다. GDI 핸들은 2.6 의 비트 규칙을 따르니 그 근거와 무관하다 `[정적: 결과서 비트 규칙과 비교]`.

비유: 에뮬레이터가 찍어 준 모양 자 번호표엔 종류 자리에 "**2번 도구**" 라고 적혀 있다. 대장에 "4번 (모양 자)" 이라고 적어 주면 빌릴 때는 통과하지만, 반납할 때 반납 창구가 "번호표엔 2번, 대장엔 4번 — 안 맞네" 하고 반납을 거절한다. 번호표부터 "4번" 으로 찍어야 한다.

### 2.11 스톡 객체 — `GetStockObject` 가 읽는 꽂이 `[정적]`

run86 이 "스톡 객체 핸들 표 `[뜻 추측]`" 이라고 했던 `+0x1800b0 + 8*i` 의 뜻이 이번에 `[정적]` 이 됐다.

- `i > 0x15` → 0 (`+0x11` · `+0x14 JA`).
- 붓 · 펜 · 팔레트 등: **`[PEB+0xf8 값 + 0x1800b0 + 8*i]` 를 그대로 핸들로 돌려준다** (`+0x65 ~ +0x72`). **대장은 안 본다.**
- 글꼴 `i = 10 · 13 · 16`: 스레드 DPI 인식이 없으면 `+0x180160 + 8*i` 를 먼저 (`+0x7c ~ +0x87`), 0 이면 위 꽂이 (`+0x8a JZ`).
- GUI 글꼴 `i = 17`: `GetDpiDepStockObjectPlateauSlot` 로 `+0x180138` · `+0x1801e8` · `+0x180210 + 8*슬롯` 중 하나. 칸이 0 이면 `+0x124 NtGdiEnsureDpiDepDefaultGuiFontForPlateau`.
- 그래서 진열장 크기는 쓰는 범위에 따라: 붓 · 펜 · 팔레트만 `0x180160` · 글꼴까지 `0x1801e8` · GUI 글꼴까지 `0x180290` (2.12 의 1).

**스톡 핸들 꼴** `[정적: 비트 규칙으로 만든 예, 진짜 값 아님]`:

- `번호 | (종류 | 0x80) << 16 | 고유 << 24`
- 붓 `0x900000` · 펜 `0xb00000` · 글꼴 `0x8a0000` · 팔레트 `0x880000`
- 예: 번호 `0x10` · 고유 1 · 붓 → `0x01900010`. 칸 `+0xc` word = `H >> 16`.

진짜 Windows 에서 이 칸을 채우는 쪽 (이름만): win32kbase `GdiProcessCallout` (`0xe2010`, `+0x22b` PEB `+0xf8`, run86) · `bSetStockObject` · `GreGetStockObject` · win32kfull `bInitStockFontsInternal` · `bSetStockFont`. 커널 세션 배열 (`+0xbc0`) 과 공유 `+0x1800b0` 이 **같은 메모리인지는 `[공백]`** (Q278).

### 2.12 ★ 최소 표 칸 — 스톡 + `RegisterClassEx` 통과, 그리고 `GdiplusStartup` 첫 가지 `[정적]`

run 문서 "PE_emulator 쪽" 의 한 목록이다 (붓 `i = 0 ~ 5` · `0x12` 기준, 펜 · 글꼴 · 팔레트는 종류만 다름).

#### 스톡 객체를 채우면서 `RegisterClassEx` 도 통과시키려면

1. PEB `+0xf8` → 읽기 가능 블록, 크기 ≥ `0x180160` (붓 · 펜 · 팔레트) · `0x1801e8` (+ 글꼴) · `0x180290` (+ GUI 글꼴).
2. `+0x1800b0 + 8*i` ← 스톡 핸들 `H_i`. `0x12` · `0x13` 은 0 아님 (`DllMain`, run86).
3. `H_i` = 번호 `idx_i < 0x10000` (서로 다름) · 종류 `0x100000` · 스톡 `0x800000` · 고유 아무거나 (예 `0x01900010`).
4. 대장 칸 `PEB[+0xf8] + idx_i*0x18`:
   - `+0x08` = **0** (주인 0 은 `GdiValidateHandle` · `GetObjectType` · `DeleteObject` · `SelectObject(객체)` 가 받음)
   - `+0x0c` = `H_i >> 16`
   - `+0x0e` = `(H_i >> 16) & 0x1f` (붓 `0x10`)
   - `+0x00` · `+0x0f` · `+0x10` = 0
5. 이미 되어 있어야 하는 것: `gMaxGdiHandleCount` (`GdiDllInitialize` 가 `0x1000000`) · `gW32PID` (`GdiProcessSetup`) · gdi32 `GdiValidateHandle` 지연 가져오기 풀기.
6. → `GdiValidateHandle(H_i)` TRUE → `RegisterClassExWOWW +0x2e6 JNZ` 통과.

#### `GdiplusStartup` 첫 가지까지 넘기려면 (위 + 영역 하나)

7. 영역 핸들 `H_r = idx_r | 0x040000 | 고유 << 24`, 칸 `+0x08` = PID & ~3 · `+0x0c` = `H_r >> 16` · `+0x0e` = 4 · `+0x10` = `ROR64(ptr ^ gCookie, gCookie & 0x3f)` (`ptr` = 쓰기 가능 ≥ 0x18 B). PEB `+0x140` 은 0 그대로 + 쓰기 가능.
   - 그 뒤 **다음 벽** = `CreateICA("DISPLAY")` (Q278 N1).

#### 왜 AT03 의 지금 값보다 `0x900000` 꼴이 안전한가

- 지금 AT03 값 `0x21400 + i*0x400` 은 윗 16비트가 `0x0002` (붓 아님, 스톡 비트도 없음).
- `GdiValidateHandle` 은 종류를 안 보니 **표 칸만 맞추면 통과엔 지장 없다.**
- 하지만 `GetObjectType` 은 핸들 비트로 종류를 가르고, `DeleteObject` 는 스톡 비트가 없으면 `+0xcc` 에서 "공용이라 TRUE" 로 안 간다 → 다르게 움직인다.
- 그래서 결과서 권고 = **`0x900000 | 번호 | 고유 << 24`** 꼴.

#### flow-writer 가 에뮬레이터 줄 번호로 덧붙인 것 (읽기만, 고치지 않음)

- `nt_gdi_init2.cpp:33` 반환 `1` → `gCookie = 1` (아래 6비트 = 1). 그래서 7 의 잠금은 `ROR64(ptr ^ 1, 1)` 이 된다. 결과서 권고대로 아래 6비트를 0 으로 두면 `XOR` 만.
- `nt_gdi_init2.cpp:28` 지금 스톡 `0x12` · `0x13` 칸 값이 `0x12` · `0x13` (≤ `0x1f`). `DllMain` 의 "0 아님" 은 넘지만, 그 값이 배경 붓으로 쓰이면 `+0x12d` 에서 색 견본 번호로 읽혀 검사를 건너뛴다 `[정적: 비교만]`.
- `nt_gdi_init2.cpp:27` "게스트엔 읽기 전용" — 표 칸은 유저 쪽이 **읽기만** 하니 괜찮다 `[정적: 읽은 줄 안에서]`. 다만 칸 `+0x10` 이 가리키는 보관함 (영역 · `DC_ATTR`) 은 gdi32 가 **쓴다** (`CreateRectRgn +0x227 ~ +0x245`) → 다른 쓰기 가능 메모리여야 한다.
- 머리 주석 "읽는 끝 `+0x180150` → 쪽 올림 `0x181000`" 이면 `0x1801e8` · `0x180290` 은 이미 그 안이다 `[추측: 매핑 함수 몸은 안 읽음]`.

비유: 공용 붓을 쓰게 하려면 꽂이에 번호표를 꽂고, **대장에도 줄을 적어야** 한다. 공용 붓 줄은 주인을 비워 둬도 (0) 되고, 번호표엔 "붓 + 공용" 도장을 찍는 게 안전하다. 모양 자 (영역) 는 더 깐깐해서 주인 · 보관함 열쇠까지 다 적어야 한다.

### 2.13 표가 비면 syscall 없이 실패하는 함수 — 그리고 run142 의 메시지 상자 `[정적]`

| 함수 | 표 칸이 0 이면 | syscall |
|---|---|---|
| `GdiValidateHandle` | FALSE | 없음 |
| `DeleteObject` → `InternalDeleteObject` | FALSE (오류 안 세움) | 없음 |
| `DeleteObject(스톡)` (칸 정상) | TRUE | 없음 |
| `SelectObject(hdc, 객체)` — 객체 칸 없음 | 0 (오류 안 세움) | 없음 |
| `SelectObject(hdc, 객체)` — DC 칸 없음 | 0, `0x57` | 없음 |
| `SelectObject(hdc, 붓 · 펜)` — 칸 정상 | 이전 붓 · 펜 (DC 사본 `+0xa0` / `+0xa8`) | **없음** (`+0x98` 에 바뀜 표시) |
| `GetObjectType` | 0 | 없음 (DC 만 `NtGdiGetDCDword`) |
| `GetStockObject` | 표 안 봄 | 없음 (`i = 17` 칸 0 일 때만) |
| `GetObjectW` | 표 안 봄 | 경우에 따라 (`+0x64 NtGdiExtGetObjectW`) |
| `GetDeviceCaps(DC)` | 0, `TEB+0x68 = 0x57` | 없음 |
| `GetTextMetricsW` · `GetTextExtentPointW` · `GdiGetCharDimensions` (DC) | 0 (`GetTextExtentPointW` 는 `0x57`) · 아무것도 안 씀 | 없음 |
| `CreateRectRgn` | NULL | **있음** (`NtGdiCreateRectRgn` + `NtGdiDeleteObjectApp`) |

- 표의 나머지 줄 (글꼴 · 비트맵 고르기 · `DeleteRegion` 배치 등) 은 run 문서 6절.
- 에뮬레이터 입장에서 읽을 것: **"syscall 이 안 나간다" 는 건 "stub 을 고쳐도 소용없다"** 는 뜻이다. 이 함수들은 그래픽 부서에 묻기 **전에** 대장에서 걸러진다. 고칠 곳은 대장 칸이다.

#### run142 의 메시지 상자 GDI 답과 이어 보기

run142 해설 2.8 의 "GDI 답 — DC · 글꼴 · 글자 폭이 0 이면 상자는 **조용히 0**" 이 무엇을 보는지 이번에 닫혔다 (값은 안 보고 "표를 보는 자리" 만).

1. `NtUserGetDCEx(0,0,3)` 가 준 HDC 는 종류 `0x10000` 이고, 대장에 **DC 칸** (`+0xe == 1` · `+0xc == H>>16` · `+0x8 & ~1 == PID` 정확히 · `+0x10` 풀면 ≠ 0 인 `DC_ATTR`) 이 있어야 한다.
2. DC 칸이 없으면 `GdiGetCharDimensions` 가 **말없이 돌아간다** → 부른 쪽은 폭 0 으로 읽는다.
3. → `SoftModalMessageBox +0x151 · +0x153 JZ → +0x69f` 가 폭 0 으로 `MessageBoxW` 를 0 으로 끝낸다.
4. HFONT (`NtGdiHfontCreate`) 는 종류 `0xa0000` + **글꼴 칸** (`+0xe == 0xa` · `+0xc == H>>16` · 주인 PID 또는 0 · `+0xf` 비트 1 꺼짐) 이 있어야 `SelectObject(hdc, 글꼴)` 가 이전 글꼴을 돌려준다.
5. 남은 것 (DC 칸을 만드는 쪽 · `DC_ATTR` 전체 · 글꼴 사본) → Q278.

**새 말 — `DC_ATTR`**: DC 칸 `+0x10` 이 가리키는 **유저 쪽 DC 속성 블록** (≥ 0x130 B — `+0x98` 바뀜 표시 · `+0xa0` 붓 · `+0xa8` 펜 · `+0x128` 글꼴) `[정적: 읽는 칸만, 전체는 공백]`. 비유: 도화지 (DC) 옆에 붙은 **"지금 쥐고 있는 붓 · 펜 · 글꼴" 메모장**. `SelectObject` 가 붓 · 펜을 바꿀 땐 그래픽 부서에 안 묻고 이 메모장만 고친다.

### 2.14 PE_emulator 판정 — 맞음 · 틀림 (run 문서 "PE_emulator 쪽")

고칠지는 사용자 · PE_emulator 결정이다. flow-writer 는 `../PE_emulator` 를 줄 번호만 훑었다.

| 쓰는 곳 | 판정 | 한 줄 이유 |
|---|---|---|
| `nt_gdi_init2.cpp` (공유 메모리 — 표 0) | **모자람** | `0x180150` 은 `DllMain` 까지만. 표 칸이 0 이면 `CreateRectRgn` NULL · `GdiValidateHandle` FALSE · DC 길 `0x57` / 0. `gCookie` 는 0 아님 + 아래 6비트 0 권고 |
| `nt_gdi_create_rect_rgn.cpp` (`0x20800`) | **틀림 (둘)** | 표 칸 안 채움 + 종류 비트 `2` (영역 `4`) → 2.10 |
| `NtGdiDeleteObjectApp` 새 stub | **반환 1 이어야** | 유효한 객체에 0 이면 `DeleteObject` 가 FALSE (`InternalDeleteObject +0x129` 반환을 그대로 씀). `GdiplusStartup` 의 `DeleteRegion` 은 반환을 안 씀. 칸 지우기는 선택. `TEB+0x78 ≠ 0` 이 되면 영역 · 붓 · 펜 지우기는 TEB 배치로 가서 이 stub 이 안 불림 |
| 보류 패치 `AT03_stock-objects_held.patch` | **원인 확인, 고칠 자리 있음** | 꽂이만 채우고 대장 칸 안 채움 → 2.12 목록을 같이 넣으면 통과. `0x900000` 꼴이 안전 |

### 2.15 앞 run 고쳐 읽기 — 닫힘과 바뀐 곳

틀린 것은 없고 `[추측]` · `[공백]` 이 닫힌 것이라, run 문서는 "정정" 이 아니라 "**고쳐 읽기**" 로 각 문서 맨 위에 한 줄씩 넣었다. 이 글도 같은 뜻으로 [run86 해설](run86-gdi32-init-해설.md) · [run142 해설](run142-user32-connect-해설.md) 맨 위에 한 줄씩 넣었다.

| 앞 run | 전에 | 이제 |
|---|---|---|
| run86 | `+0x1800b0 + 8*i` = "스톡 객체 핸들 표 `[뜻 추측]`" | 뜻 `[정적]` — `GetStockObject` 가 **그대로 핸들로** 돌려준다 (대장 안 봄) |
| run86 | GDI 공유 메모리 "최소 `0x180150` B" | **`DllMain` 까지만** 맞는 말. 앞쪽 `+0x0 ~ +0x180000` 이 번호표 대장이고, 칸이 0 이면 `CreateRectRgn` · `GdiValidateHandle` · DC 길이 실패 |
| run86 | 정적 ⑤ "핸들 표 항목 모양" | **일부 닫힘** — 칸 모양 · 누가 무엇을 보나 · `DeleteRegion` 배치 조건 |
| run131 | "잘못된 브러시" 는 다섯 경우 중 하나 (조건 줄 안 봄) | 조건 = `hbrBackground > 0x1f` + `GdiValidateHandle` FALSE + 버전 > 3.0. run131 엔 고칠 것 없음 (`hbrBackground = 2`) |
| run142 | 메시지 상자 GDI 답 (차례 8 · N4) → Q270 | DC 칸 · 글꼴 칸을 syscall 앞에서 보는 자리 `[정적]`. DC 칸 만들기 · `DC_ATTR` 전체 · 글꼴 사본은 Q278 |

### 2.16 flow-writer 대조 — 결과서와 다르게 적은 곳 (뜻은 같음, 짧게)

run 문서 "flow-writer 대조" 는 "**어긋난 줄 없음**" 이다. 덧붙임만 있다. 이 글을 읽을 때 걸릴 만한 것만:

- **실패 가지 18곳 · 모두 1** — 결과서엔 없고 flow-writer 가 셌다 (2.4).
- `CreateRectRgn +0x25d` · `DeleteObject +0x4f` 가 부르는 gdi32 `0xd4c8` 은 **같은 주소 이름 2개** (`IsGdiTrackHDeletePresent` · `IsGdiTrackHCreatePresent`). 결과서가 쓴 `IsGdiTrackHCreatePresent` 는 그중 하나 — 하는 일은 "`GdiTrackH*` API 세트가 있나 묻기".
- `GdiValidateHandle` 의 실패 가지 둘 더: `+0x24 JNC` (번호 ≥ 상한) · `+0x48 JS` (`GdiGetEntry` 실패) (2.7).
- gdi32 `SelectObject` (`0x4150`) 는 `SelectObjectImpl` 앞에 `g_pEdgeGdi` 를 보고, 0 이 아니면 다른 길 (간접 호출) 로 간다. 보통 프로세스는 0 일 것 `[추측]`.
- 가리기 식 `ROR64(ptr ^ gCookie, gCookie & 0x3f)` 가 맞다는 것 — 푸는 줄 (`+0x1e1 ~ +0x1f0`) = `ROL(stored, gCookie & 0x3f) ^ gCookie` 이라서 (2.5).

### 2.17 정리 — 비유로 한 번 더

1. 그래픽 부서는 그림 도구를 만들 때마다 직원에게 **번호표** 를 주고, 진열장 앞 **번호표 대장** 에 한 줄 (주인 · 윗자리 · 종류 · 잠근 보관함 열쇠) 을 적는다.
2. 본관 (gdi32) 은 번호표를 받으면 **대장부터 펴 본다.** 무엇을 보는지는 직원마다 다르다 — 번호표 검사원 (`GdiValidateHandle`) 은 윗자리 · 주인만, 모양 자 담당 (`CreateRectRgn`) 은 넷 다.
3. 고급 화방 (gdiplus) 은 개점하면서 모양 자 하나를 빌려 보는데, 우리 에뮬레이터 그래픽 부서는 **대장을 비워 둬서** 본관이 번호표를 버렸다. 화방은 그 한마디에 "개점 실패 (1)".
4. AT03 은 공용 붓 번호표를 **꽂이에만 꽂고 대장엔 안 적어서**, 창 설계도 등록 창구가 대장을 펴 보자마자 거절했다.
5. 에뮬레이터 모양 자 번호표는 종류 자리부터 틀려 있어서 (`2`), 대장만 고쳐서는 반납 때 다시 걸린다. 번호표는 "4번" 으로 찍어야 한다.
6. 대장을 제대로 적어 주면 화방은 다음 체크 항목 — **화면 도화지 받기 (`CreateICA("DISPLAY")`)** — 로 넘어간다. 거기가 다음 벽이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 8a1bf16 -- runs/03_sprint/flow.md`)

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | gdiplus 줄 없음, gdi32 · gdi32full · user32 · win32kbase 줄에 run144 없음 | **새 gdiplus 줄** (처음 analyze, PDB `DDF19AA0…`, 함수 5239) + 네 줄에 run144 보탬 + 근거 줄 (run144) 한 줄 (test.exe 는 안 올림 · `GdiSharedHandleTable` (null) `[관찰]`) |
| 3장 gdi32 하위 절 (run86) | "최소 `0x180150` B" · 스톡 칸 `[뜻 추측]` | 한 줄 보탬: 앞쪽 `+0x0 ~ +0x180000` 이 **공유 핸들 표**, 스톡 칸은 `GetStockObject` 가 그대로 돌려주는 값 `[정적]`. `0x180150` 은 `DllMain` 까지만 |
| 3장 창 API 하위 절 (run142) | 메시지 상자 벽 나무 `+0x153 글자 폭 0 → 0 (GDI — Q270)` · 남은 것 "GDI 는 Q270" | `(GDI — Q270 → run144: DC 칸 없으면 폭 0)` · "Q270 → run144 (아래 하위 절, 남은 GDI 는 Q278)" 두 줄 |
| **3장 새 하위 절** | — | "**GDI 공유 핸들 표 · `GdiplusStartup` 첫 가지 · 배경 붓 검사**" — 비유 (출입증 대장) · 나무 · `[정적]` 여덟 (첫 가지 · NULL 은 gdi32 · 표 모양 · 핸들 비트 · `GdiValidateHandle` · AT03 · `0x20800` 종류 비트 · 표를 보는 함수) |
| 3장 창 클래스 등록 절 (run131) | "잘못된 브러시" 조건 줄 없음 | 한 줄 보탬: **배경 붓 검사** `+0x129 ~ +0x131 → +0x2d8` · 거절 + 버전 > `0x300` 이면 `0x57`, combase 의 `2` 는 안 탐 `[정적]` |
| 4장 | run86 정적 ⑤ "GDI 그리기 (핸들 표 항목 …)" · run142 정적 N4 "→ Q270" | ⑤ **일부 해결 → run144** · N4 **일부 해결 → run144** 표시, **Q270 해결됨** 줄 하나, **run144 새로** 줄 셋 (동적 Q277 · 정적 Q278 · PE_emulator 쪽) |
| 5장 | — | run144 두 줄 (근거 run · 요청서 번호 표) |

- **그림 (1장) 은 안 바뀌었다.** test.exe 초기화 밖 이야기라서다.
- 짐 지도 (`baggage.md`) 도 안 바뀌었다. 새 짐 칸은 없다. 보탤 거리 하나 (짐 지도 3.1 PEB `+0x140 GdiHandleBuffer` 를 쓰는 쪽에 gdi32 `hGetPEBHandle +0x57` · `+0x159` 도 있다 `[정적]`) 는 다음 짐 지도 바퀴 몫으로 Q264 에 보탰다 (plan.md).

### 큰 그림에서 어떻게 이어지나

- **run86 → run142 → run144 가 진열장 하나로 이어졌다.**
  - run86: 그래픽 부서가 진열장을 **건다** (`NtGdiInit2` → PEB `+0xf8`). 본관은 꽂이의 DC 붓 · DC 펜만 확인.
  - run142: 창 API 문지기를 지나면 메시지 상자가 **DC · 글자 폭** 에서 조용히 0 — "GDI 는 Q270".
  - run144: 진열장 앞쪽이 **번호표 대장** 이고, gdi32 가 번호표를 받을 때마다 거기서 확인한다. 대장이 비면 화방 (`GdiplusStartup`) · 설계도 등록 (`RegisterClassEx`) · 메시지 상자 (DC 길) 가 모두 같은 이유로 넘어진다.
- **PE_emulator 쪽으로는**: GP1 · ST1 · AT03 세 막힘이 **"대장 칸을 안 채움"** 하나로 모였고, 채울 칸의 최소 목록 (2.12) 과 핸들 꼴 (2.10 · 2.11) 이 나왔다.
- **아직 화방이 문을 연 건 아니다.** 첫 가지를 넘기면 바로 `CreateICA("DISPLAY")` 가 기다린다 — HDC 꼴 · DC 칸 · `DC_ATTR` 이 필요하다 → Q278 N1.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q277** (동적, Q162 · Q273 과 같은 세션)

test.exe 로는 못 본다 (gdi32 · gdiplus 를 안 올린다). `GdiplusStartup` 을 부르는 GUI 프로세스가 필요하고, 고르기는 사용자 결정이다.

1. PEB `+0xf8` 표 덤프 — 스톡 붓 칸 (`+0x8` 이 정말 0 인가 · `+0xc` = `H>>16` · `+0xe` `0x10` · `+0x10` 0), 영역 칸 (`+0x10` 가려진 값), DC 칸. `gCookie` (gdi32 rva `0x257a0`) 와 풀린 포인터.
   - 왜 못 알았나: 정적은 "무엇과 비교하나" 만 보여 준다. 진짜 커널이 넣는 값은 실행 때만.
2. `win32u!NtGdiCreateRectRgn` 반환 비트 (종류 4 · 스톡 0 · 고유), `CreateRectRgn +0x19c` 의 `[RSP+0x2e]` · `+0x1ae` 의 `[RSP+0x2c]`.
3. `GetStockObject(0..5, 0x12, 0x13)` 값이 `종류 | 0x800000` 꼴인가, 그 칸 `+0x8`.
   - 2.11 의 스톡 핸들 꼴은 **비트 규칙으로 만든 예** 라 진짜 값 확인이 필요하다.
4. `RegisterClassExWOWW +0x2d8` 직전 `RCX` · `+0x2e4` `EAX` · `+0x119` `EDI` (일반 앱 `RtlGetExpWinVer`).
5. `DeleteObject(영역)` 때 `TEB+0x78` 이 0 인가 (아니면 TEB `+0x2f0` 배치).
6. `NtQueryInformationProcess(−1, 0x25)` · `NtProtectVirtualMemory` ×2 를 부르는 줄.
   - 왜 못 알았나: kernelbase `GetVersionExA` 쪽 · `CreateRectRgn` 꼬리 · `InternalGdiplusShutdown` 중 어디인지 정적으로 못 정함 `[공백]`.
7. 멈춤점 후보: `gdi32!CreateRectRgn +0x19c` · `+0x116` · `gdi32full!GdiValidateHandle +0x4a` · `user32!RegisterClassExWOWW +0x2e4` · `gdiplus!InternalGdiplusStartup +0x1d6` · `+0x293`.

### 정적으로 남은 것 → **Q278**

1. **N1 (바로 다음 벽, 가장 급함)**: `InternalGdiplusStartup +0x296` — gdi32 `CreateICA` → gdi32full `CreateICA` → `bCreateDCA` → `hdcCreateDCW` (`+0xc9 NtGdiOpenDCW`) 가 돌려줄 HDC 꼴 · DC 칸 · `DC_ATTR` 크기와 칸, `GetDeviceCaps(0x58 / 0x5a)` · `GpDevice::GpDevice(hdc)` 가 읽는 칸.
   - 왜 못 알았나: 이번 요청 밖이었고, `hdcCreateDCW` 아래는 Ghidra export 스크립트 문제 (이름이 255 B 를 넘는 C++ 템플릿 함수 → "File name too long") 로 일부만 뽑혔다.
2. **N2**: `InitImagingLibrary` (`+0x632`) · `InitSystemFontsDirs` (`+0x744`) · `GetGpInstalledFontCollection` (`+0x784`) 가 0 을 내는 조건.
3. **N3**: DC 칸 · `DC_ATTR` 전체 (run142 N4 남은 몫) — `NtUserGetDCEx` · `NtGdiOpenDCW` 가 만드는 칸, `pcfLocateCFONT` · `CFONT`, `GetDCDpiScaleValue`, `bComputeTextExtent`.
4. **N4**: win32k 쪽 표 채우기 — `GdiProcessCallout` · `HmgInsertObjectInternal` · `bSetStockObject` 세션 배열 ↔ 공유 `+0x1800b0` (2.11 의 `[공백]`). "win32k 안쪽" 이라 사용자 결정.
5. **N5** (낮음): `NtQueryInformationProcess(0x25)` · `NtQueryLicenseValue` 를 부르는 kernelbase `GetVersionExA/W` 줄 (이번 분기엔 안 쓰인다).

### 짐 지도 → **Q264** (사용자 결정 필요)

- 새 짐 칸은 없다. 보탤 거리 하나: 짐 지도 3.1 PEB `+0x140 GdiHandleBuffer` 를 쓰는 쪽에 gdi32 `hGetPEBHandle +0x57` (잠금으로 TEB 주소) · `+0x159` (0) 도 있다 `[정적]`. 칸 수는 그대로.

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- 칸 이름 (`+0x8` 주인 · `+0x10` 사용자 쪽 포인터 등) 은 `[추측]` — PDB 에 이 구조체 타입이 없다. 오프셋 · 검사 · 줄만 `[정적]`.
- 칸 `+0x00` 의 뜻 (커널 쪽 객체 주소) `[추측]`, 칸 `+0x0f` 비트 1 의 뜻 `[공백]`.
- 칸 수 `0x10000` `[추측: 나눗셈으로만]`.
- `CreateFontIndirectW` 몸 · `DC_ATTR` 전체 · 글꼴 사본 쪽 `[공백]` (질문 3 을 "읽은 함수 한정" 으로 닫은 이유).
- gdi32 가 왜 커널이 준 핸들을 다시 확인하게 짰는지 — 코드가 말하지 않는다 `[추측]` (2.3).
- 하네스 쪽: Ghidra export 스크립트의 긴 파일 이름 문제는 하네스 제안으로 올라갔다 (오케스트레이터 · 사용자 몫, run 문서 "도구 메모").
- 디버거 영향: 이번에 읽은 줄 (gdiplus 시작 · gdi32 핸들 검사 · user32 붓 검사) 에서 디버거 여부를 보는 줄은 없었다 `[정적: 읽은 줄 안에서]`.

---

## 5. 더 읽기

- run 문서: [`run144-gdi-startup.md`](run144-gdi-startup.md)
  - 1절 (`GdiplusStartup` 차례) · 2절 (`CreateRectRgn`) · 3절 (칸 · 번호 식 · 핸들 비트, "누가 무엇을 보나" 전체 표) · 4절 (스톡) · 5절 (배경 붓 · AT03) · 6절 (syscall 없이 실패) · 7절 (run142 GDI 몫) · "PE_emulator 쪽" · "flow-writer 대조"
- 요청서: [`_workspace/sast/141_gdi-startup.md`](../../_workspace/sast/141_gdi-startup.md)
- 결과서: [`_workspace/sast/141_gdi-startup_result.md`](../../_workspace/sast/141_gdi-startup_result.md) — 표 (1) · (1b) · (2) · (2b) · (2c) · (2d) · (3), "지금 우리 (PE_emulator) 맞나·틀리나", "다음 요청 후보"
- 원 요청: `../PE_emulator/_workspace/05_tuning_planner/343_internals-gdi-startup.md`
- flow.md (`git show 8a1bf16 -- runs/03_sprint/flow.md`):
  - 3장 "GDI 공유 핸들 표 · `GdiplusStartup` 첫 가지 · 배경 붓 검사" 하위 절 (새로)
  - 3장 gdi32 · gdi32full 시작 함수 하위 절 (run86) 의 run144 줄
  - 3장 창 API 넷의 연결 가드 하위 절 (run142) 의 메시지 상자 벽 나무
  - 3장 창 클래스 등록 (COM STA) 절의 "배경 붓 검사" 줄
  - 4장 run86 정적 ⑤ · run142 정적 N4 · Q270 해결됨 · run144 새로 셋
- 앞 해설: [run86](run86-gdi32-init-해설.md) 2.6 · 2.9 · 2.10 · [run121](run121-teb-other-dlls-해설.md) 2.6 · [run131](run131-user32-regclass-해설.md) 2.4 · [run142](run142-user32-connect-해설.md) 2.3 · 2.8

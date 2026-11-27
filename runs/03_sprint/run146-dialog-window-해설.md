# run146 dialog-window — 해설

> **고쳐 읽기 (run148)**: 2.7 · 2.12 는 `NtUserDestroyWindow` 를 만들기 길 (컨트롤 실패, `+0xc01`) 에서만 적었다 → **모달 끝 `DialogBox2 +0x3c4` 에서도 늘 불린다** `[정적]` (결과 `pdlg+0x20` 은 그 앞 `+0x3bd` 에서 먼저 읽음). 반환 안 봄 · 기본 대답 0 이면 됨은 그대로. 2.12 최소 목록 7 "(sast/146 에서 보탬)" 과 4절 "정적으로 남은 것 → sast/146 (진행 중)" 은 run148 로 닫혔다 — `NtUserSetDialogPointer` 의 fnid `0x2a4` 는 **"필수"** 로 강화 (`DefDlgProcWorker +0x32 ~ +0x45` 가 0 · `0x2a4` 아니면 앱 프로시저를 안 부름), 장부 `+0x20` 의 "기본 단추 ID 겸 결과값" 은 `DM_GETDEFID` 가 같은 칸을 읽는 줄로 `[정적]`. 본문은 그대로 두었다 → [run148 해설](run148-dialog-end-해설.md) 2.5 · 2.6 · 2.8 · 2.12 · 2.13

> **고쳐 읽기 (run157)**: 2.5 표 `+0x38` (첫 자식) 과 4절 "정적으로 남은 것" 의 "자식 연결" — 컨트롤은 커널 (`xxxCreateWindowEx` → `LinkWindow`) 이 부모 자식 줄의 **맨 끝에** 붙인다 `[정적]`. 그래서 2.7 컨트롤 고리가 만든 차례 (= 템플릿 차례) 가 곧 `+0x38 → +0x48` 사슬 차례이고, 이전 형제 칸 `사본+0x50` 도 쓴다. 2.5 표 `+0x12` 비트 3 (ANSI) 은 A 판에서도 클래스가 ANSI 일 때만 켜지고, A 판의 `+0x78` 은 `DefDlgProcA`. 컨트롤 프로시저 본문 `[공백]` 은 반쯤 닫힘 (`BS_DEFPUSHBUTTON` → `WM_GETDLGCODE` `0x2010`, 클릭 → 부모에게 `WM_COMMAND`). 자세히는 [run157 문서](run157-dialog-win32k.md).

> 이 글은 [run146 문서](run146-dialog-window.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q274a** — PE_emulator 요청 "`DialogBoxParamW` 가 `InternalCreateDialog` 에서 hwnd 를 돌려줄 때까지 · 창 사본 · 핸들 항목 칸 표" (`_workspace/plan.md`, 지금 **진행 — 물음 1 · 2 끝 (run146), 물음 3 은 sast/146**)
  - 원 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` 의 **물음 1 · 2**. 튜닝 `357_tune17.md` GC1, 병목 `dialog_box_fail` 악성 60 (요청 쪽 에뮬레이터 숫자).
  - Q274 (run142 가 남긴 정적 N1 · N2 · N3 · N5) 에서 **N1 · N2 만 앞으로 당긴 것** 이다. 나머지 N3 · N5 는 Q274b.
  - 사용자 결정 2026-11-26 "다른 것보다 가장 먼저". 정적만.
  - 결과서 판정 **분석성공** — 물음 1 (0)(a)(b)(c)(d)(e)(f) · 2 모두 `[정적]` 줄 근거로 답함.
  - 원문 물음 3 (기본 단추 한 번으로 `EndDialog` 까지) 은 **sast/146 dialog-end** 로 따로 간다 (지금 도는 중).
- 근거:
  - run 문서 [`run146-dialog-window.md`](run146-dialog-window.md) (가장 중요)
  - 요청서 [`_workspace/sast/142_dialog-window.md`](../../_workspace/sast/142_dialog-window.md) · 결과서 [`142_dialog-window_result.md`](../../_workspace/sast/142_dialog-window_result.md)
    - **처음 analyze 없음.** user32 · win32kfull · win32kbase · ntoskrnl 모두 이미 analyze 돼 있어서 `-readOnly` export · refs 만 했다.
  - flow.md 변경: docs 커밋 `3aa3207` (`git show 3aa3207 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 user32 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run131 · run142 · run144 와 같은 줄).
   - 그래서 이번 이야기는 **"user32 를 쓰는 프로그램이 대화상자를 하나 띄울 때"** 의 일이다. 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다.
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - "악성 60", "−1 을 받았다" 같은 것은 PE_emulator 로그다. 진짜 Windows 를 본 게 아니라서 `[관찰]` 이 아니다. 이 글에서는 "에뮬레이터 로그" 라고만 부른다.
3. **오프셋 읽는 법** (앞 해설들과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문 "`+0x57 LEA …`" 처럼 명령 이름과 같이 적힌 숫자 = **함수 안 그 줄** (call 자리 포함).
   - "`+0x75 JC → +0x246`" 은 그 줄과 점프가 가는 곳.
   - `InternalCreateDialog` 는 **조각** 이 많다 — 같은 줄이 "조각 `FUN_18001fbc1 +0x17a`" 와 "`InternalCreateDialog +0x11ab`" 두 이름으로 불린다. run 문서는 두 기준을 같이 적는다. 결과서가 틀린 곳 일곱 가운데 셋이 이 두 기준을 섞은 데서 났다 (2.14).
   - 콜스택으로 본 적은 없다 (정적만).
4. **값은 하나도 안 봤다.**
   - 진짜 Windows 에서 사본 `+0xc8` 이 정말 `0x1e` 인지, 핸들 항목 크기가 얼마인지는 이번 범위 밖이다 (동적 Q273 몫).
   - 이번에 본 건 "**코드가 어느 칸을 읽고 쓰고, 0 이면 어디로 가나**" 뿐이다.
5. **칸 이름은 전부 짐작이다.**
   - PDB 에 `tagWND` · `tagDIALOG` · `tagHANDLEENTRY` 형식이 **없다** (`pdb_struct.py --list` 로 0개) `[정적]`.
   - 그래서 "`+0x78` = 창 프로시저", "`+0x2a` = fnid" 같은 **이름은 `[추측]`**, **오프셋과 그 칸을 읽는 줄은 `[정적]`** 이다. 이 글도 그렇게 읽어 주면 된다.
6. **비유 이름이 run 문서와 다르다.**
   - run 문서 "결과" 비유: **가게 개업 · 구청 · 업종 대장 · 사물함 · 장부 · 사장님께 전화** (run 문서도 이 비유를 `[추측: 비유]` 로 둔다).
   - 그런데 앞 해설들에선 이미 **가게 = 프로세스, 관리사무소 = 커널** 이다. 그래서 이 글은 앞 해설 그림에 맞춰 부른다 (아래 "비유"). 사물함 · 장부는 그대로 빌려 쓴다. 그림만 다르고 사실은 같다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- TEB = 직원 **개인 안내판** → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.2
- **run85 에서 푼 것** → [run85 해설](run85-user32-init-해설.md)
  - user32 = **창 담당 건물**, win32u = **단축번호 버튼판** → 2.1
  - `gSharedInfo` = **공용 안내 묶음**, `psi` = **서버 정보판** → 2.5
  - **커널 콜백** · `KernelCallbackTable` (**내선 번호표**) · `__ClientThreadSetup` = 84번째 칸 → 2.12
- **run131 에서 푼 것** → [run131 해설](run131-user32-regclass-해설.md)
  - 창 클래스 = **창 설계도 이름**, 창 함수 = 메시지가 오면 부를 함수 → 1절 "새 말 — 창 클래스"
  - ATOM (원자) = **번호표** → 1절 "새 말 — ATOM"
  - `ClassNameToVersion` · `NtUserGetAtomName` · `VerNtUserCreateWindowEx` 사슬 → 2.10
- **run142 에서 푼 것** → [run142 해설](run142-user32-connect-해설.md)
  - **문지기 (가드)** — TEB `+0x78` 이 0 이면 `NtUserGetThreadState(0xe)` → 2.3
  - **부수 효과** (돌려주는 값 말고 덤으로 바뀌는 것) → 2.6
  - TEB `+0x820` = **방 정보 카드** (`tagDESKTOPINFO`) → 2.6
  - **접근 위반** (빈 칸을 밟다 발이 빠짐) → 2.8
  - **DC** = 도화지 + 붓 세트 → 2.8
  - **모달 고리** · **창 메시지 (`WM_*`)** = 알림 카드 · 대화상자 자료 `+0x18` (끝 표시) · `+0x20` (결과) → 2.9
- **run144 에서 푼 것** → [run144 해설](run144-gdi-startup-해설.md)
  - GDI **공유 핸들 표의 칸** · `SelectObject` 가 칸이 없으면 `0x57` + 0 → 2.13
  - 앱 버전 (`RtlGetExpWinVer`) = "나는 몇 년식 앱" → "새 말 — 앱 버전"
- **run145 에서 푼 것** → [run145 해설](run145-epm-endpoint-해설.md)
  - **CFG 간접 호출 (`⇢`)** = 교환원 앞의 경비 → 2.2 줄 ①

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, TEB = 직원 **개인 안내판**, 커널 = **관리사무소**, syscall = **신청 창구**.
- user32 = **창 담당 건물**, win32u = **단축번호 버튼판**, win32k = 관리사무소 **그래픽 부서**.
- `gSharedInfo` = **공용 안내 묶음**, `psi` = **서버 정보판**.
- 커널 콜백 (되부름) = 관리사무소가 일하다가 가게 사람을 **거꾸로 불러내는** 것. 그때 보는 게 **내선 번호표**.
- 원자 = **번호표** (run131). 메시지 상자 = 직원 앞에 세우는 **작은 알림판** (run142).
- 이번에 새로 붙는 그림:
  - 대화상자 = 창 담당 건물이 세우는 **알림판** (메시지 상자보다 큰 것, 단추 · 글상자 같은 **부품** 이 붙는다).
  - 창 하나를 만들면 그래픽 부서가 **원본 서류** 를 갖고, 가게 쪽엔 **열람용 사본** 을 둔다 → **창 사본**.
  - 공용 안내 묶음 안 "창 번호부" 의 한 줄 → **핸들 항목**. 창 번호 (hwnd) 를 들고 와서 이 줄을 보면 사본이 어디 있는지 안다.
  - 창마다 딸린 **빈 사물함** → **여분 블록**. 창 담당 건물 **비품실** (`pUserHeap`) 에서 빌린다.
  - 알림판의 **장부** → **대화상자 자료 (`pdlg`)**. 사물함 **둘째 칸** (`+8`) 에 꽂힌다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| (0) `NtUserGetAtomName(0x8002)` 의 진짜 대답은 | **반환 6 · 버퍼 `#32770` + 끝 NUL.** `0xc000` 아래 원자는 원자 표를 **안 찾고** `L"#%u"` 로 지어 준다. 커널은 `Length` 칸을 안 쓴다 `[정적]` (2.3) |
| (b) 대화상자 창 만들기 인자는 | 클래스 **`0x8002` 그대로**, `hInstance` 는 준 값 그대로, 끝 인자에 `0x40000000` `[정적]` (2.4) |
| (b) 돌아온 hwnd 를 무엇으로 검사하나 | `ValidateHwnd` → `ValidateDialogPwnd`. **syscall 없이** 핸들 항목 · 창 사본 칸만 본다. 실패하면 창은 안 부수고 NULL `[정적]` (2.5) |
| (b) 대화상자 자료 (`pdlg`) 는 누가 어디에 | **user32 가 자기 힙에서 0 으로 잡고**, `NtUserSetDialogPointer` 가 커널 쪽에서 **사본 `+0x128` 여분 블록의 `+8`** 에 꽂는다 (반환은 안 봄) `[정적]` (2.6) |
| (b) 그 여분 블록은 누가 | **커널이 창 만들기 도중 되부름 `0x7b`** 로 user32 힙에서 받아 `xxxCreateWindowEx +0x1a46` 에서 사본에 넣는다 `[정적]` (2.6) |
| (c) 컨트롤 (부품) 은 | 하나씩 만든다. **하나라도 NULL 이면 전체 NULL** — `DS_NOFAILCREATE` 면 그것만 건너뜀 `[정적]` (2.7) |
| (d) 창 프로시저를 syscall 없이 부르는 조건 | 핸들 항목 `+0x8` == TEB `+0x78` 등 **조건 다섯** `[정적]` (2.8) |
| (e) 표시 · 끝 | `ShowWindow` · `UpdateWindow` · 초점 · 앞으로 내기 **반환은 안 본다**. NULL 로 나가는 가지 **15개** `[정적]` (2.9) |
| (f) 되부름이 꼭 필요한가 | **아니오.** `0x7b` 가 하던 "0 채운 여분 블록" 하나만 stub 이 대신하면 된다 `[정적]` (2.11) |
| 2 칸 표 | 핸들 항목 · TEB · 공유 정보 · 창 사본 칸 표. 사본은 **최소 `0x130` B**, user32 는 사본에 **안 쓴다** `[정적, 이름은 추측]` (2.5) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**초기화가 끝난 뒤, 프로그램이 대화상자를 처음 띄우는 순간** 이다. test.exe 에는 없는 옆길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → LdrpInitializeProcess
 │    └─ (2.7) dll 전원 켜기
 │          └─ (user32 를 올리는 프로세스라면) user32 DllMain → GdiDllInitialize   ← run85 · run86
 └─ … → NtContinue (은퇴) → exe 진입점 → (CRT) → main
                                                   └─ (user32 를 쓰는 프로그램이라면)
                                                       DialogBoxParamW
                                                        ├─ 문지기 (NtUserGetThreadState(0xe))   ← run142
                                                        ├─ 창 만들기 → hwnd 돌려받기           ← 이번 (run146)
                                                        └─ 모달 고리 → EndDialog                ← sast/146 (진행 중)
```

- flow.md 에서는 **3장 창 API 하위 절 (run142) · GDI 공유 핸들 표 하위 절 (run144) 다음** 에 새 하위 절 "대화상자 창 한 장이 hwnd 로 돌아올 때까지" 로 들어갔다.
- 프로그램 쪽에서 부르는 일이라 CLAUDE.md 범위로 치면 "`main` 안에서 OS 를 다시 부르는 화살표" 쪽이다. 그래서 그림엔 안 넣었다.

### 왜 이 질문이 지금 왔나

- run142 에서 에뮬레이터는 "문지기 대답을 0 에서 1 로" 고칠 자리를 받았다. 원문에 따르면 PE_emulator 갈래 BD01b 가 그걸 풀었다.
- 그랬더니 **다음 벽** 이 나왔다 (에뮬레이터 로그, 원 요청):
  - `DialogBoxParamW` · `DialogBoxIndirectParamA` 가 여전히 −1. 부모 hwnd 는 늘 0.
  - 이번엔 `NtUserGetAtomName(0x8002)` 에 기본 대답 **0** → `ClassNameToVersion` NULL → 커널 창 만들기를 **아예 안 부르고** −1.
  - 그 벽을 넘어도 `NtUserCreateWindowEx` 0 · `ValidateHwnd` · "사본 없음" 같은 벽이 줄줄이 있을 것.
- PE_emulator 정책 D156 은 **"보이지 않는 가짜 창"** 이다. 창은 만들어진 것으로 답하고, 창 프로시저는 진짜로 부르고, 기본 단추는 바로 누른 것으로 친다.
- 그러니 알고 싶은 건 커널 본문이 아니라 **"user32 가 무엇을 읽고 무엇을 검사하나"** 다.

### 무엇이 궁금했나

1. **창 만들기 길 (N1)**
   - (0) `NtUserGetAtomName(0x8002)` 의 진짜 대답은?
   - (a) 문지기 뒤 GDI · 지표 부름은 0 을 받으면 어떻게 되나?
   - (b) 대화상자 창 인자는? 돌아온 뒤 무엇을 검사하나? `NtUserSetDialogPointer` 는 무엇을 하나? 사본 `+0x128` 쪽은 누가 적나?
   - (c) 컨트롤 고리는?
   - (d) 창 프로시저를 syscall 없이 부르는 조건은?
   - (e) 표시 · 끝, 그리고 NULL 로 나가는 가지 전부는?
   - (f) 되부름 (커널이 유저 코드를 부르는 것) 이 꼭 필요한가?
2. **칸 표 (N2)** — 이 길에서 user32 가 읽고 쓰는 창 사본 · 핸들 항목 칸 전부.

### 바퀴 전에 알던 것

- `[정적]` (run131) `ClassNameToVersion +0x226` 이 클래스가 원자일 때 `NtUserGetAtomName` 을 부른다. 반환 = 글자 수, user32 가 그걸로 `Length` 를 직접 쓴다.
  - 다만 **"끝 NUL 은 세지 않는다"** 는 건 `[추측]` 이었다 (run131 4절 동적 3).
  - 0 이면 `VerNtUserCreateWindowEx` 가 커널 창 만들기를 **안 부르고** NULL (run131 해설 2.10).
- `[정적]` (run142) 대화상자의 틀 — `InternalCreateDialog` 문지기, 핸들 항목 `+0x0 ~ +0x1a`, TEB `+0x820` / `+0x828` / `+0x840`, "최소 조건" 목록 7 · 8.
  - `[공백]` 최소 길 표 19 ~ 27 — 어느 호출이 syscall 없이 풀리나, `NtUserShowWindow` · `NtUserSetForegroundWindow` 조건.
  - `[추측]` 대화상자 자료가 어느 힙에 있나 (run142 해설 4절).
- `[정적]` (run144) GDI 공유 핸들 표 — `SelectObject` 등은 DC 칸이 없으면 `0x57` + 0.

---

## 2. 찾은 것

### 2.1 한눈에 — 대화상자 한 장이 hwnd 로 돌아올 때까지 `[정적]`

run 문서 "호출 관계" 나무를 대표 줄만 남긴 것이다 (오프셋은 ret 자리, `⇒` 는 syscall 너머 커널, `⇢` 는 되부름 · CFG 간접).

```
user32!DialogBoxParamW → DialogBoxIndirectParamAorW → InternalDialogBox
 └─ +0x75 InternalCreateDialog                            (NULL → −1)
     ├─ +0xd40 NtUserGetThreadState(0xe)                   문지기 (run142)
     ├─ +0x33a CreateCompatibleDC(0)                       0 → NULL
     ├─ +0x644 VerNtUserCreateWindowEx(…, 0x8002, 0x8002, …)
     │    ├─ +0x17c ClassNameToVersion +0x22d NtUserGetAtomName ── SYSCALL 0x109c
     │    │      ⇒ win32kfull → win32kbase!UserGetAtomName → nt!RtlQueryAtomInAtomTable  → 6 "#32770"
     │    └─ +0x236 NtUserCreateWindowEx ── SYSCALL 0x106f
     │           ⇒ xxxCreateWindowEx
     │               ├─ +0x144b xxxClientAllocWindowClassExtraBytes +0x62 KeUserModeCallback(0x7b)
     │               │        ⇢ user32!__xxxClientAllocWindowClassExtraBytes  (0 채운 블록)
     │               └─ +0x1a46 [사본+0x128] ← 여분 블록
     ├─ +0x658 ValidateHwnd                                0 → NULL
     ├─ +0x66c ValidateDialogPwnd                          0 → NULL
     │    ├─ +0x44 AllocLookasideEntry                     pdlg 0x58 B (0)
     │    └─ +0x56 NtUserSetDialogPointer ── SYSCALL 0x1552  (반환 버림)
     │           ⇒ SetDialogPointer +0xbc  [[사본+0x128]+8] ← pdlg
     ├─      +0x69a ~ +0x72c  pdlg 칸 채우기
     ├─ +0x97a VerNtUserCreateWindowEx (컨트롤마다)
     ├─ +0xa69 SendMessageWorker(사본, WM_INITDIALOG)      syscall 없이
     │          ⇢ DefDlgProcW → DefDlgProcWorker → 앱 프로시저 ([pdlg+0])
     └─ +0xf81 NtUserShowWindow · +0xf8e UpdateWindow      반환 안 봄
```

네 덩어리로 보면 쉽다.

1. **이름 묻기** — 클래스 번호 `0x8002` 의 이름을 커널에 묻는다 (2.3).
2. **창 만들기** — 커널이 창을 만들고, 그 도중 user32 를 거꾸로 불러 **빈 사물함** 을 받아 온다 (2.4 · 2.6).
3. **user32 혼자 하는 일** — 사본 검사, 장부 (`pdlg`) 만들기, 부품 (컨트롤) 만들기, "준비됐어요" 알림 (2.5 ~ 2.8).
4. **보이기** — 결과를 안 보고 넘어간다 (2.9).

### 2.2 한 줄씩 읽는 법

#### 줄 1: `│ ├─ +0x17c ClassNameToVersion +0x22d NtUserGetAtomName ── SYSCALL 0x109c`

- **누가**: user32 `ClassNameToVersion`. 자기는 `VerNtUserCreateWindowEx` 가 `+0x177` 에서 부른다 (ret `+0x17c`).
- **어디서**: call 자리 `+0x226`, 돌아오는 자리 (ret) `+0x22d`.
- **누구를**: win32u `NtUserGetAtomName` (번호 `0x109c`) → 커널 win32kfull `NtUserGetAtomName`.
  - 인자는 둘 — 원자 (`0x8002`) 와 `UNICODE_STRING` (버퍼 0x200 B, `MaximumLength = 0x200`).
- **무엇을 위해**: 원자의 **이름 글** 을 받으려고. 받은 이름은 "창 클래스 바꿔치기" 를 찾는 **열쇠** 로 쓴다 (run131 해설 2.10).
  - 돌아온 뒤 `+0x232 ADD AX,AX` · `+0x235` 에서 user32 가 `Length = 반환 × 2` 를 **직접** 쓴다.
- 태그 `[정적]`.

#### 줄 2: `├─ +0x144b xxxClientAllocWindowClassExtraBytes +0x62 KeUserModeCallback(0x7b)`

- **누가**: 커널 쪽 win32kfull `xxxCreateWindowEx` (창 만들기 본체).
- **어디서**: `+0x1446` 에서 `xxxClientAllocWindowClassExtraBytes` 를 부르고 (ret `+0x144b`), 그 안 `+0x5b` 에서 `KeUserModeCallback` 을 부른다 (ret `+0x62`).
  - 번호 `0x7b` 는 `+0x4c MOV R8D,4` 뒤 `+0x57 LEA ECX,[R8+0x77]` 로 만든다.
- **누구를**: 커널이 **유저 쪽** user32 `__xxxClientAllocWindowClassExtraBytes` (rva `0x4f770`) 를 거꾸로 부른다.
- **무엇을 위해**: 창의 **여분 블록** 을 user32 힙에서 빌려 오려고. 받은 포인터는 `+0x1a46` 에서 사본 `+0x128` 에 들어간다.
- 태그 `[정적]`.

### 2.3 ★ (0) 원자 이름 — `0x8002` 는 "#32770" 이 된다 `[정적]`

#### 새 말 — 정수 원자 · 표 원자

- 원자 = 문자열 이름 대신 쓰는 **16비트 번호** (run131 해설 "새 말 — ATOM", 비유 **번호표**).
- 이번에 원자가 **두 종류** 로 갈린다는 게 코드로 보였다.
  - **정수 원자** = `0xc000` **아래** 번호 (0 제외). 이름 장부 (원자 표) 를 **안 찾는다**. 번호 자체가 이름이다 `[정적]`.
  - **표 원자** = `0xc000` **이상** 번호. 등록할 때 이름이 장부에 적힌 것 (run131 의 `OleMainThreadWndClass` 같은 것) `[정적: 갈림 줄]`. 진짜 이름 글은 이번에도 안 봤다 → 동적 Q242.
- 비유: 번호표 가운데 `0xc000` 아래 번호는 **이름 칸이 없는 번호표** 다. 이름을 물으면 장부를 펴지 않고 "#번호" 라고 그대로 읽어 준다.

#### 네 층을 따라 내려가기

| 층 | 한 일 | 줄 |
|---|---|---|
| user32 `ClassNameToVersion` | 원자일 때만 부른다. 버퍼 0x200 B (0 채움 아님), `MaximumLength = 0x200` | `+0x98` · `+0x9f` · `+0x74` · `+0x21b` → call `+0x226` |
| win32kfull `NtUserGetAtomName` | 글자 한도 = `MaximumLength >> 1`. **`Length` 를 쓰는 줄이 없다** | `+0x6e / +0x74` → call `+0x7d` |
| win32kbase `UserGetAtomName` | 세션 원자 표를 꺼내고, 바이트 한도 = 글자 × 2. **돌려줄 때 바이트 ÷ 2** | `+0x2d` · `+0x34` → call `+0x4a` · `+0x5c / +0x60` |
| ntoskrnl `RtlQueryAtomInAtomTable` | **`0xc000` 미만이면 표를 안 찾고** `L"#%u"` 로 짓는다. 끝 NUL 을 **쓰지만 세지 않는다** | `+0x6c ~ +0x75` · `+0x277 ~ +0x298` · `+0x2c5` · `+0x2d0` |

맨 아래 층을 한 걸음씩 보면:

1. `+0x6c MOV EAX,0xc000` · `+0x71 CMP R15W,AX` — 원자가 `0xc000` 보다 작은가?
2. `+0x75 JC → +0x246` — 작으면 표 찾는 길을 **건너뛴다**.
3. `+0x246 / +0x24a` — 원자가 0 이면 `+0x2d4` 에서 `0xc000000d` (실패). 그럼 위층에서 반환 0.
4. `+0x277 ~ +0x298` — `_snwprintf_s(지역 16자, 16, _TRUNCATE, L"#%u", 원자)`. 서식 글 `0x140afbf20` = 바이트 `23 00 25 00 75 00 00 00` = **`L"#%u"`** (flow-writer 가 `ms/bin` 의 `ntoskrnl.exe` 바이트로 다시 읽음).
   - `_snwprintf_s` = 서식대로 글을 짓는 함수. `#%u` 는 "`#` 다음에 부호 없는 십진수" 라는 뜻이다 `[추측: 일반적으로 알려진 설명]`.
5. `+0x29f ADD ESI,ESI` — 바이트 = 글자 × 2. 이만큼만 복사 (`+0x2bd`).
6. `+0x2c5` — 그 **뒤 칸에 NUL (0)** 을 쓴다. 바이트 수엔 안 넣는다.
7. `+0x2d0` — 바이트 수를 돌려준다. win32kbase 가 `+0x60` 에서 ÷ 2 해서 **글자 수** 로 바꾼다.

그래서 `0x8002` (= 32770) → **반환 6**, 버퍼 `#32770` + NUL (14 B) `[정적]`. user32 는 512 B 를 주니 잘릴 일이 없다.

#### 덤 둘

- **끝 NUL 은 쓰지만 세지 않는다.** 그래서 버퍼 한도는 "글자 수 + 1" 이상이어야 전체를 쓴다. 모자라면 `한도 − 1` 글자로 자른다 (`+0x2a7 JNC → +0x2ef` · `+0x2f4`) `[정적]`.
- **커널은 `UNICODE_STRING.Length` 를 안 쓴다.** user32 `ClassNameToVersion +0x232 / +0x235` 가 쓴다 `[정적]`. user32 는 끝 NUL 을 읽지도 않는다 — `Length` 만큼만 넘긴다.

비유: 신청서 업종 칸에 "32770 번" 이라는 숫자만 적혀 있으면, 그래픽 부서는 업종 장부를 펴지 않는다. 그냥 "#32770" 이라고 글자로 바꿔 읽어 주고, "여섯 글자요" 라고 답한다. 글자 수 칸 (`Length`) 은 창 담당 건물이 자기가 적는다.

### 2.4 ★ (b) 대화상자 창 만들기 — 인자 `[정적]`

`InternalCreateDialog +0x5de ~ +0x63f` 에서 인자를 모아 `+0x63f` 에서 `VerNtUserCreateWindowEx` 를 부른다. 대표만:

| 인자 | 값 | 줄 |
|---|---|---|
| 클래스 (둘째) | **`0x8002`** — 템플릿에 클래스 이름이 빈 글일 때 | `+0x575` · `+0x57f MOV R10D,0x8002` · `+0x63c MOV RDX,R10` |
| 버전 클래스 (셋째) | `ClassNameToVersion` 결과 — 바꿔치기 없으면 같은 `0x8002` | `VerNtUserCreateWindowEx +0x17c` · `+0x1c1` |
| 스타일 | 템플릿 스타일에서 `WS_VISIBLE` (`0x10000000`) 를 **뗀다** (보이기는 나중에 `NtUserShowWindow`) | `+0x415 BT EDI,0x1c` → 조각 `+0x1db ~ +0x1e7` |
| `hInstance` | **호출자가 준 값 그대로** | `+0x5f0` · `+0x60b` |
| 끝 인자 | `dwExpWinVer \| 0x40000000` — 이 비트가 `ClassNameToVersion` 길을 연다 | `+0x5ea` · `VerNtUserCreateWindowEx +0x108` |

- **새 말 — 템플릿 (대화상자 템플릿)**: 앱 안에 들어 있는 대화상자 **모양 설명서**. 크기 · 스타일 · 부품 (컨트롤) 목록이 적혀 있다 `[추측: 일반적으로 알려진 설명]`. `DialogBoxParamW` 는 이걸 `FindResourceExW` · `LoadResource` 로 꺼낸다 `[정적]`. 비유: 알림판 **조립 설명서**.
- `0x8002` 를 고르는 줄은 "템플릿의 클래스 칸이 비었나" (`+0x575 CMP word [R10],R13W`) 하나다. 비었으면 `0x8002`, 이름이 있으면 그 글을 넘긴다.
- 커널이 0 을 주면 `VerNtUserCreateWindowEx` 는 `GetLastError() == 0x57f` 일 때만 `RegisterDefaultClass` 로 한 번 더 해 본다 (`+0x263 ~ +0x28f`). **stub 이 "0 + 오류 없음" 이면 그대로 NULL** `[정적]`.

비유: 알림판 신청서에 "업종 = 32770 번" 을 그대로 적어 낸다. 이름으로 바꿔 적지 않는다.

### 2.5 ★ (b) 돌아온 hwnd 검사 — 핸들 항목과 창 사본 `[정적]`

커널이 hwnd 를 돌려주면, user32 는 그걸 **바로 믿지 않는다.** 세 번 본다.

1. `+0x647 TEST RAX` · `+0x64a JZ → +0xc90` — 0 인가?
2. `+0x653 ValidateHwnd` → `+0x65e JZ → +0xc90` — 그 번호로 사본을 찾을 수 있나?
3. `+0x667 ValidateDialogPwnd` → `+0x66e JZ → +0xc90` — 대화상자 자료를 붙일 수 있나?

셋 중 하나라도 0 이면 `+0xc90` (메뉴 파괴 · 글꼴 지움) 뒤 **NULL** 이다. **창은 안 부순다** `[정적]`.

2번을 읽으려면 새 말 둘이 필요하다.

#### 새 말 — 핸들 항목

- hwnd (창 번호) 하나에 대응하는, **공용 안내 묶음 안 배열의 한 칸** 이다. 배열은 `gSharedInfo+0x8`, 칸 크기는 `gSharedInfo+0x10` (dword) `[정적]`.
- 몇 번째 칸인지 = `hwnd & 0xffff` (아래 16비트). 윗부분 `(hwnd >> 16) & 0x7fff` 는 **고유 수** 로, 칸의 `+0x1a` 와 맞아야 한다 (`0x7fff` 는 만능) `[정적]`.
- 비유: 공용 안내 묶음 안 **창 번호부의 한 줄**. 창 번호를 들고 오면 그 줄에 "이 창 사본은 어디, 주인 직원은 누구, 종류는 창" 이 적혀 있다.

| 칸 | 크기 | 뜻 `[추측: 이름]` · 통과 조건 `[정적]` |
|---|---|---|
| `+0x00` | 8 | 사본 위치 — **사본 = TEB `+0x828` + 이 값** (더하기만, 범위 검사 없음) |
| `+0x08` | 8 | 소유 스레드 ID — 직접 호출 (2.8) 은 이게 TEB `+0x78` 과 같아야 |
| `+0x10` | 8 | `[[TEB+0x820]+0]` 과 같으면 통과 (다르면 `NtUserMapDesktopObject` 로 한 번 더 — 막지는 않음) |
| `+0x18` | 1 | 0 = 빈 칸 · **1 = 창** |
| `+0x19` | 1 | 비트 0 = 실패 · 비트 2 = 보안 검사 건너뜀 |
| `+0x1a` | 2 | hwnd 윗 15비트와 같아야 |

#### 새 말 — 창 사본

- 창의 진짜 몸 (커널 창 물체) 은 커널에 있다. user32 는 그걸 직접 못 만진다.
- 대신 커널이 **유저 쪽에서 읽을 수 있는 그림자** 를 둔다. `ValidateHwnd` 가 돌려주는 포인터 (`pwnd`) 가 이것이다. 커널 창 물체 `+0x28` 이 이 사본을 가리킨다 (`SetDialogPointer +0x19`) `[정적]`.
- **user32 는 사본에 쓰는 줄이 없다.** 쓰기는 전부 커널이다 `[정적]`. user32 는 읽기만 한다.
- 비유: 그래픽 부서 서류함의 **원본 서류** 를, 가게 쪽 창가에 **열람용 사본** 으로 붙여 둔 것. 가게 직원은 읽을 수만 있고, 고치려면 신청 창구 (syscall) 로 가야 한다.

이번 길에서 user32 가 읽는 칸이 `+0x128` (qword) 까지라서 **사본은 최소 `0x130` B** 여야 한다 `[정적]`. 대표 칸만 (전체 표는 run 문서 6절):

| 오프셋 | 이름 `[추측]` | 왜 중요한가 `[정적]` |
|---|---|---|
| `+0x00` | `HWND` | hwnd 와 같아야 |
| `+0x08` | 커널 쪽 자기 주소 | 사본 안 커널 포인터를 유저 주소로 바꿀 때 뺀다 |
| `+0x12` | 창 깃발 | 비트 0 = 대화상자 자료 붙음 · 비트 2 = 직접 호출 막기 · 비트 3 = ANSI |
| `+0x1c ~ +0x1f` | 스타일 | 만들 때 준 스타일 (`WS_VISIBLE` 뺌) |
| `+0x2a` | fnid | 0 시작 → `0x2a4` |
| `+0x38` | 첫 자식 | **0 이면 초점이 대화상자 자신** |
| `+0x78` | 창 프로시저 | **0 이면 안 됨** — 대화상자 = `DefDlgProcW` |
| `+0x80` | 클래스 | **0 이면 `[0+4]` 접근 위반** |
| `+0xc8` | 여분 바이트 크기 | 대화상자는 **`0x1e` 이상** |
| `+0x128` | 여분 블록 | **0 이면 `InternalCreateDialog +0x6ab` 접근 위반** (2.6) |

**새 말 — fnid**: 창이 어떤 기본 종류로 굴러가는지 적는 번호로 보이는 칸 `[추측: 이름]`. 이번 길에서는 0 으로 시작했다가 대화상자 자료가 붙을 때 `0x2a4` 가 된다 (`SetDialogPointer +0xf2`) `[정적]`. 비유: 창 서류에 찍히는 **종류 도장**.

#### `ValidateDialogPwnd` 가 0 이 되는 때 `[정적]`

| 조건 | 줄 |
|---|---|
| 사본 `+0x12` 비트 0 이 **이미** 켜져 있으면 아무것도 안 하고 1 (대화상자 자료를 안 만듦) | `+0x6` → `+0xf` |
| 사본 `+0xc8 < 0x1e` → 오류 `0x58c` · 0 | `+0x1b` · `+0x22` |
| 사본 `+0x2a & 0xc000` ≠ 0 → 0 | `+0x29` |
| 대화상자 자료 할당 0 → 0 | `+0x3f / +0x44` |

- 첫 줄이 재미있다. 비트 0 은 "**자료 붙음**" 표시다 (`SetDialogPointer +0x10a` 가 켬). 그래서 run 문서 최소 목록은 "대화상자 사본 `+0x12` 는 처음엔 **0**" 이라고 적는다 (비트 0 이 먼저 켜져 있으면 `pdlg` 를 안 만듦 `[정적]`).
  - 켜 둔 채로 가면 장부가 안 생기고, 장부 칸을 쓰는 줄 (2.6 ④) 이 빈 칸을 밟을 것이다 `[추측: 두 [정적] 줄을 이은 것]`.

### 2.6 ★ (b) 대화상자 자료 (`pdlg`) 와 여분 블록 — 누가 어디에 `[정적]`

이번 바퀴의 핵심 두 개 중 하나다. 등장인물이 셋이라 **시간 순서** 로 따라간다.

#### 새 말 — 여분 블록

- 창마다 딸린 **추가 칸 묶음**. 크기는 사본 `+0xc8` (창 여분 바이트 크기) 이고, 그 주소가 사본 `+0x128` 에 들어간다 `[정적]`.
- 대화상자에서는 모양이 이렇게 보인다 `[추측: DLGWINDOWEXTRA]`: `+0x0` (`DWLP_MSGRESULT`) · **`+0x8` = 대화상자 자료 (`pdlg`)** · `+0x10` (`DWLP_USER`). 모두 0 으로 시작.
- 비유: 창마다 하나씩 달린 **빈 사물함**. 대화상자는 그 사물함 **둘째 칸** 에 장부를 꽂아 둔다.

#### 새 말 — 되부름 (사용자 모드 되부름)

- 커널이 일하다가 **유저 쪽 함수를 거꾸로 부르는 것**. 커널 쪽에선 `KeUserModeCallback(번호, …)` 으로 부르고, 유저 쪽 함수는 일을 마치면 `NtCallbackReturn` 으로 돌아간다 `[정적]`.
- 번호는 `KernelCallbackTable` (내선 번호표) 의 칸 번호다 — run85 해설 2.12 의 "커널 콜백" 과 같은 것이다. run142 에선 84번 (`0x54`, `__ClientThreadSetup`) 을 봤다.
- 비유: 관리사무소가 창 담당 건물에 **내선 전화** 를 거는 것.

#### 시간 순서로

**① 커널 — 창 만들기 도중 사물함 빌려 오기** (`NtUserCreateWindowEx` 안)

1. `xxxCreateWindowEx +0x4b2` — 사본 `+0x128` 을 0 으로 둔다.
2. `+0x9c9 GetClientExtraBytesTotalSize` — 여분 크기 (= 사본 `+0xc8`, 어떤 기능이 켜지면 `+0x150` 더함) 가 0 이 아니면 `+0x9d0 JNZ → +0x1444`.
3. `+0x1446 xxxClientAllocWindowClassExtraBytes(크기)` → 안에서 **`KeUserModeCallback(0x7b, 입력 4 B)`** (`+0x5b`).
4. ⇢ user32 `__xxxClientAllocWindowClassExtraBytes` (rva `0x4f770`):
   - `+0x10` 입력에서 크기를 읽고
   - `+0x13 LEA EDX,[RAX+8]` — 깃발 8 = **0 으로 채움**
   - `+0x1d RtlAllocateHeap(pUserHeap, …)` — **user32 힙** 에서 빌림
   - `+0x3a NtCallbackReturn` — 블록 주소를 들고 커널로 돌아감
5. `+0x1a46 MOV [사본+0x128],RDI` — **받은 블록을 사본에 적는다.**

- `0x7b` 가 정말 이 함수 칸인지는 이렇게 확인했다 (결과서): `0x4f770` 을 가리키는 칸 `0xb4af8` 이 `__ClientThreadSetup` 칸 `0xb49c0` (번호 `0x54`, run142) 보다 `0x138 / 8 = 39` 칸 뒤다 → `0x7b` ✓.

**② user32 — 장부 꺼내기** (`ValidateDialogPwnd +0x3f`)

- `AllocLookasideEntry(&DialogLookaside)` 가 **0x58 B** 를 잡는다.
  - 자유 목록에 남은 게 있으면 거기서 (`+0x20 ~ +0x2c`), 없으면 `+0x72 RtlAllocateHeap(pUserHeap, 0, 0x58)`.
  - 두 길 다 `+0x51 memset(…, 0, …)` 을 지나서 **0 으로 시작** 한다.
- **새 말 — 자유 목록 (lookaside)**: 같은 크기 칸을 다 쓰고 돌려받으면 버리지 않고 **쌓아 두었다 다시 쓰는 선반** `[추측: 일반적으로 알려진 설명]`. 처음 한 번 `InitLookaside` 가 `0x58 B × 2` 로 만든다 `[정적]`.

**③ 커널 — 장부를 사물함에 꽂기** (`ValidateDialogPwnd +0x4f NtUserSetDialogPointer(hwnd, pdlg)`, 번호 `0x1552`)

- win32kfull `SetDialogPointer`:
  - `+0x25` 여분 크기 `≥ 0x1e` 인지 · `+0x32` 사본 `+0x12` 비트 2 면 아무것도 안 함 · `+0x5e / +0x62` 같은 프로세스인지
  - `+0xa3 unsafe_cast_fnid_zero_to_PDIALOG` — fnid 가 0 또는 `0x2a4` 면 **사본 `+0x128`** (여분 블록 주소) 을 꺼냄
  - **`+0xbc MOV [RAX+8],R14`** — 여분 블록 `+8` 에 `pdlg`
  - `+0xf2` fnid 가 0 이면 `0x2a4` · `+0x10a SetOrClrWF` 로 사본 `+0x12` 비트 0 켬
- **user32 는 이 syscall 의 반환을 안 본다** (`+0x56` → `+0x5b JMP → +0xf MOV EAX,1`) `[정적]`.
  - 보는 건 **부수 효과** 다. 2.6 바로 아래 ④ 가 여분 블록 `+8` 을 곧장 쓰면서 확인하는 꼴이다.

**④ user32 — 장부 칸 채우기** (`InternalCreateDialog +0x69a ~ +0x72c`)

- 칸마다 `[사본+0x128]` → `[+8]` 을 **다시 읽어서** 쓴다. 대표 칸:

| 칸 | 쓰는 줄 | 뜻 `[추측: 이름]` |
|---|---|---|
| `+0x00` | `+0x6dd` | **앱 프로시저** (`DLGPROC`, 앱이 `DialogBoxParamW` 에 준 함수) |
| `+0x18` | `+0x6af` · `+0x6eb` · `+0x72c` | 깃발 — 비트 0 = 끝남 (`EndDialog`) · 비트 2 = ANSI · 비트 3 = DPI |
| `+0x20` | `+0x6fa` (처음 1) | **기본 단추 ID 겸 결과값** (처음 1 = IDOK) |
| `+0x30` | 조각 `+0x2ce` | 글꼴 (`HFONT`) |

#### 어디서 넘어지나 — 둘로 나뉜다 (run 문서가 결과서를 고친 곳)

| 비어 있는 것 | 넘어지는 줄 |
|---|---|
| 사본 `+0x128` 이 0 (= 되부름 `0x7b` 몫이 없음) | **`+0x6ab MOV RCX,[RAX+0x8]`** |
| 여분 블록은 있는데 `+8` 이 0 (= `NtUserSetDialogPointer` 가 안 적음) | **`+0x6af AND [RCX+0x18],…`** |

- 결과서는 둘 다 "`+0x6af`" 로 적었다. run 문서가 `.asm` 으로 다시 보고 나눴다 (대조 7) — 이 글은 run 문서를 따른다.
- 그래서 `NtUserSetDialogPointer` 는 **반환은 아무 값이어도 되지만, 몸 (부수 효과) 은 꼭 있어야 한다.**

#### 정리 — 누가 무엇을

| 무엇 | 누가 잡나 | 누가 적나 |
|---|---|---|
| 여분 블록 (사물함) | **user32** 힙 (되부름 `0x7b` 안) | **커널** `xxxCreateWindowEx +0x1a46` → 사본 `+0x128` |
| 대화상자 자료 `pdlg` (장부) | **user32** `AllocLookasideEntry` (0 채움) | **커널** `SetDialogPointer +0xbc` → 여분 블록 `+8` |
| 장부 안 칸 | — | **user32** `InternalCreateDialog +0x69a ~ +0x72c` |

비유: 그래픽 부서는 알림판을 등록하면서 내선으로 창 담당 건물을 불러 "빈 사물함 하나 비품실에서 꺼내 와" 한다 (되부름 `0x7b`). 사물함을 받아 알림판 서류에 "사물함 위치" 를 적는다. 그다음 창 담당 건물이 비품실에서 **빈 장부** 를 꺼내 와 "이거 꽂아 주세요" 하면, 그래픽 부서가 사물함 **둘째 칸** 에 꽂고 서류에 "장부 있음" 도장을 찍는다. 장부 내용 (사장님 연락처, 기본 단추) 은 창 담당 건물이 직접 적는다.

### 2.7 (c) 컨트롤 고리 — 부품을 하나씩 `[정적]`

**새 말 — 컨트롤**: 대화상자 안의 단추 · 글상자 같은 **작은 자식 창** `[추측: 일반적으로 알려진 설명]`. 템플릿에 개수 (`cdit`) 와 하나씩의 설명이 있다 `[정적]`. 비유: 알림판에 붙는 **부품**.

고리 한 바퀴 (`InternalCreateDialog +0x975 ~ +0x9f4`):

1. 부품 설명을 읽는다. 클래스가 `0xffff` 로 시작하면 **서수** 다.
   - 서수 번호 `& 0xff7f` 가 `0x19` 보다 작으면 원자 = `psi+0x364 + 2 × 번호` (`+0x84b`). 아니면 실패 (`+0x83e`).
   - 비유: "3번 표준 부품" 처럼 번호로만 적힌 부품은, 서버 정보판의 **표준 부품 번호표 줄** 에서 원자를 꺼낸다.
2. `+0x975 VerNtUserCreateWindowEx` — 부품 창을 만든다. 원자면 `NtUserGetAtomName` + `NtUserCreateWindowEx`. **`hMenu` 인자 = 컨트롤 ID** (`+0x904` → `+0x957`).
3. 글꼴이 있으면 `+0x9a7 SendMessageW(ctl, 0x30)` (`WM_SETFONT`).
4. **늘** `+0x9ba SendMessageW(ctl, 0x87)` — 반환 `& 0x10` 이면 장부 `+0x20` = 그 ID (기본 단추).

**하나가 NULL 이면** (`+0x980 JZ → +0xbf4`):

- 대화상자 스타일에 `DS_NOFAILCREATE` (`0x10`) 가 **없으면** → `+0xc01 NtUserDestroyWindow(대화상자)` → **전체 NULL**.
- **있으면** → `+0xbf8 JNZ → +0x9c7` 로 그 부품만 건너뛴다.

부품 hwnd 가 엉터리여도 3 · 4 는 안 막힌다:

- `SendMessageW` → `SendMessageInternal` 은 사본을 못 구하면 **조용히 0** 을 준다 (`+0x11b XOR EAX,EAX`, 오류 없음) `[정적]`.
- 그러니 부품 hwnd 에 항목 · 사본이 없어도 고리는 지나간다.

고리가 끝나면 기본 초점을 고른다: `_NextControl` 이 첫 자식 (사본 `+0x38`) 부터 "보임 + 탭 정지 + 비활성 아님" 을 찾는다. **자식이 없으면 (`+0x38 == 0`) 대화상자 자신** 에게 초점 `[정적]`.

### 2.8 ★ (d) syscall 없이 창 프로시저 부르기 — `SendMessageWorker` `[정적]`

#### 새 말 — 창 프로시저 · 앱 프로시저

- **창 프로시저** = 창에 메시지가 오면 부를 함수 (run131 해설의 "창 함수"). 사본 `+0x78` 에 있다 `[추측: 이름]`. 대화상자 창은 user32 `DefDlgProcW` 여야 한다.
- **앱 프로시저** = 앱이 `DialogBoxParamW` 에 넘긴 함수. 장부 `+0x0` 에 있다. `DefDlgProcW` 가 이걸 꺼내 부른다.
- 비유: 창 프로시저 = 알림판 **담당 직원**, 앱 프로시저 = 알림판 주인인 **사장님**. 알림 카드는 담당 직원이 먼저 받고, 사장님께 전한다.

#### 다섯 조건 — 다 맞으면 신청 창구를 안 거친다

`InternalCreateDialog` 는 `WM_SETFONT` (조각 `+0x2f0`) 와 `WM_INITDIALOG` (`+0xa64`) 를 `SendMessageWorker` 로 보낸다. 이 함수는 차례로 본다.

| # | 조건 | 줄 |
|---|---|---|
| ① | 메시지가 `0x3e0 ~ 0x3e8` 아님 | `+0x41` |
| ② | **핸들 항목 `+0x8` == TEB `+0x78`** (= 이 창 주인이 지금 이 직원) | `+0xbc CMP R8,RAX` · `+0xbf JNZ` |
| ③ | 사본 `+0x12` 비트 2 = 0 | `+0xc5` · `+0xd0` |
| ④ | `[[TEB+0x820]+0x10] \| [TEB+0x838]` 의 `0x2020` = 0 — **TEB `+0x820` 널 검사 없음** | `+0xf9 ~ +0x110` |
| ⑤ | 사본 `+0x80` (클래스) ≠ 0 — 0 이면 `+0x3c5` 에서 **접근 위반** | `+0x2a4` · `+0x3c5` |

- 하나라도 안 맞으면 **syscall 길** — `NtUserMessageCall` (`0x1005`, `+0x9f4`) 이나 `gapfnScSendMessage` 표 (`+0xa21`).
- 사이에 글자 종류 (ANSI / 유니코드) 를 맞추는 단계가 있다. 보내는 쪽과 창이 같으면 건너뛴다. **A 판은 여기서 갈린다 (sast/146)**.

#### 다 맞으면 — 길 A 또는 길 B

- 사본 `+0x78` (창 프로시저) 이 서버 정보판의 표 칸 (`psi + 0x248 + 8k` 또는 `psi + 0x188 + 8k`, `k = fnid − 0x29a`) 과 **같으면 길 A**: 일꾼 (`psi + fnid×8 − 0x1200`) 을 부른다. 일꾼 칸이 0 이면 널 호출.
- **아니면 길 B**: `+0x600 UserCallWinProcCheckWow([사본+0xe0], [사본+0x78], hwnd, msg, w, l, 0, 1)` — **사본 `+0x78` 을 그대로 부른다.**
- 클래스 fnid 가 `0x29a` · `0x2a1 ~ 0x2aa` 가 아니면 표를 안 보고 곧장 길 B 다 (대조 10). 그래서 stub 은 클래스 블록 `+4` 를 0 으로 둬도 된다.

#### `WM_INITDIALOG` 가 사장님께 닿기까지

1. `+0xa64 SendMessageWorker(사본, 0x110, …)` — 다섯 조건을 통과하고, 길 B 라면 사본 `+0x78` 을 부른다. 그래서 사본 `+0x78` = **`DefDlgProcW`** 여야 앱 프로시저까지 간다.
2. ⇢ 사본 `+0x78` = `DefDlgProcW` → `+0x17 ValidateHwnd` → `+0x31 DefDlgProcWorker`.
3. `DefDlgProcWorker`:
   - fnid 가 0 이면 `NtUserSetWindowFNID(hwnd, 0x2a4)` (`+0x404`). 이번 길에선 이미 `0x2a4` 다 (2.6 ③).
   - `+0x4e ValidateDialogPwnd` — 이미 붙어 있으니 1.
   - **`+0x8c ~ +0x9c` — 장부 = `[[사본+0x128]+8]`, 앱 프로시저 = `[장부+0x0]`.**
   - `+0xbf UserCallDlgProcCheckWow` ⇢ 앱 프로시저 (CFG 간접 `+0x193` · `+0x1ed`). **syscall 없음.**
4. `WM_INITDIALOG` 는 앱 프로시저 뒤 기본 처리 없이 끝나고, 앱의 반환을 돌려준다 (`+0x2da` · `+0x2dd`).
5. `InternalCreateDialog` 는 그 반환이 0 이면 기본 초점 고르기를 건너뛴다 (`+0xa7d` · `+0xa7f`).

비유: 알림판이 다 조립되면 창 담당 건물이 사장님께 "준비됐어요" 알림 카드를 보낸다. 조건 다섯이 맞으면 — 이 알림판 주인이 바로 지금 이 직원이고, 막는 표시가 없고, 설계도도 있으면 — **신청 창구 (syscall) 를 안 거치고 건물 안 내선으로 바로** 담당 직원 (`DefDlgProcW`) 에게 건다. 담당 직원은 사물함 둘째 칸 장부에서 사장님 번호를 찾아 전한다.

### 2.9 (e) 표시 · 끝 — 반환은 안 본다, NULL 가지 15 `[정적]`

#### 반환을 안 보는 부름

- `+0xf7a NtUserShowWindow(hwnd, 1)` (`0x1052`)
- `+0xf89 UpdateWindow` — 사본 `+0x88` · `+0x11` 비트 `0x10` · `+0x38` 이 **다 0 이면 syscall 없이 1**. 하나라도 0 이 아닐 때만 `NtUserUpdateWindow` (`0x15c8`).
- `DlgSetFocus` → `NtUserSetFocus` (`0x104c`, 꼬리 `JMP`)
- `NtUserSetForegroundWindow` (`0x155b`) — `DS_SETFOREGROUND` 일 때. 반환 대신 그 뒤 `IsWindow` 만 본다.
- `NtUserSetDialogSystemMenu` · `NtUserSetWindowContextHelpId` 도 마찬가지.

그래서 이 syscall 들은 **기본 대답 0 으로 충분** 하다 (run 문서 PE_emulator 표).

#### NULL (→ −1) 로 나가는 가지 15 — 묶어 보기

| 묶음 | 가지 | 대표 조건 |
|---|---|---|
| 앞쪽 | N1 ~ N6 | 문지기 0 (N1, run142) · 새 앱 + 스타일 `0x8000` · `LoadMenuW` 0 · **`CreateCompatibleDC(0)` 0 (N4)** · 모니터 NULL (N5 · N6) |
| 창 만들기 | N7 ~ N9 | **`VerNtUserCreateWindowEx` NULL (N7 — 지금 에뮬레이터 벽)** · `ValidateHwnd` NULL · `ValidateDialogPwnd` 0 |
| 부품 | N10 · N11 | 부품 NULL · 서수 `≥ 0x19` (`DS_NOFAILCREATE` 없으면 대화상자 파괴) |
| 뒤쪽 | N12 ~ N15 | `WM_INITDIALOG` 뒤 hwnd 무효 · `IsWindow` 0 (새 앱) · `DS_SETFOREGROUND` 뒤 `IsWindow` 0 · 끝 검사 `HMValidateHandleNoSecure` 0 |

- 바깥에서 −1 을 만드는 줄은 넷: `FindResourceExW` 0 · `LoadResource` 0 · `GetSizeDialogTemplate` 0 · `InternalDialogBox +0x78` (N1 ~ N15 모두).
- 하나 더: **부모 ≠ 0 이고 `ValidateHwnd(부모)` 가 0 이면 −1 이 아니라 0** 이다. 우리 표본은 부모 0 이라 해당 없음.
- 전체 표는 run 문서 7절.

### 2.10 (a) 문지기 뒤 GDI · 지표 부름 — NULL 은 하나뿐 `[정적]`

문지기를 지나 `+0x63f` 창 만들기 앞까지, GDI · 지표 부름이 여럿 있다. 대부분은 **0 을 받아도 그냥 넘어간다.**

- `GetDpiForSystem` · `GetDpiServerInfoForCurrentThread` · `DeleteDC` · `_AdjustWindowRectExForDpi` · `RepositionRect` — 0 이어도 지나감 (값 0 이면 좌표 0, 크래시 아님).
- `GetDialogMonitor` — 모니터 0 이면 여기선 지나가지만 **나중에 N5** 로 NULL.
- 글꼴 (`CreateDlgFont` → 실패면 `GetStockObject(0xd)`) — 둘 다 0 이면 **글꼴 없음** 으로 지나감 (`WM_SETFONT` 안 보냄).
- **NULL 로 끝나는 GDI 부름은 `CreateCompatibleDC(0)` 하나** (N4).

run144 와 잇기: `SelectObject` 가 DC 칸이 없어 `0x57` + 0 을 줘도 (run144 7절) 여기선 대체 글꼴로 넘어가고, `GdiGetCharDimensions` 가 말없이 돌아가도 기본 글자 단위를 쓴다 `[정적]`. run144 의 메시지 상자 길 (`SoftModalMessageBox +0x153` — 폭 0 이면 `MessageBoxW` 0) 과 달리, **이 줄들에선 GDI 0 이 NULL 로 이어지지 않는다.**

### 2.11 ★ (f) 되부름이 꼭 필요한가 — 아니오, 하나만 대신하면 `[정적]`

창을 만들 때 커널은 보통 user32 를 여러 번 되부른다 — 창 프로시저에 `WM_NCCREATE` · `WM_CREATE` 같은 알림을 보내는 것도 되부름이다. 에뮬레이터가 이걸 다 해야 할까?

| 되부름 | 뒤 단계가 읽는 상태를 만드나 |
|---|---|
| **`0x7b` 여분 블록** | **예.** 사본 `+0x128` (0 채움, `+0xc8` 바이트). 없으면 `+0x6ab` 널 읽기 |
| `WM_NCCREATE` · `WM_CREATE` … (대화상자 창 프로시저) | **아니오.** `DefDlgProcWorker` 가 첫 메시지에서 하는 일 (`NtUserSetWindowFNID` · `ValidateDialogPwnd`) 은 `InternalCreateDialog +0x667` 이 어차피 하고 (fnid 는 `SetDialogPointer +0xf2`), 장부 칸은 `InternalCreateDialog` 가 쓴다 |
| 부품의 `WM_NCCREATE` / `WM_CREATE` | `[추측]` — 부품 프로시저 본문은 안 읽음. 부품 사본 `+0x12` 비트 2 를 켜 두면 syscall 길 (stub 0) 로 가서 부품 프로시저를 아예 안 부른다 |
| `WM_GETMINMAXINFO` · `WM_NCCALCSIZE` · `WM_SIZE` 등 | 아니오 — user32 가 이 길에서 읽는 칸 없음 |

단계별로:

1. 근거: 뒤 단계 (`InternalCreateDialog`) 가 읽는 것 가운데 되부름이 만드는 건 사본 `+0x128` 하나다 `[정적]`.
2. 근거: 그 블록은 "user32 힙에서 0 채운 `+0xc8` 바이트" 일 뿐이다 — 특별한 내용이 없다 `[정적]`.
3. 그래서: `NtUserCreateWindowEx` stub 이 **직접** 그런 블록을 만들어 사본 `+0x128` 에 넣으면, 되부름 바퀴를 세우지 않아도 (a) ~ (e) 가 지나간다 `[정적]`.

**새 말 — stub**: PE_emulator 에서 진짜 커널 대신 syscall 에 답하는 **작은 가짜 몸**. "몸 없음" 이면 기본 대답 0 을 준다 (run 문서 PE_emulator 표). 비유: 그래픽 부서가 없는 가게에서, 신청 창구에 앉아 대신 대답해 주는 **대리 직원**.

비유: 진짜 그래픽 부서는 알림판을 등록하면서 창 담당 건물에 내선을 여러 번 건다. 그런데 뒤에서 실제로 쓰이는 건 "빈 사물함 꺼내 와" 한 통뿐이다. 대리 직원은 그 사물함만 직접 꺼내 달아 주면 된다.

### 2.12 PE_emulator 판정 — 맞음 · 틀림 (run 문서 "PE_emulator 쪽")

flow-writer 는 `../PE_emulator` 의 세 파일을 **줄 번호만** 훑었다 (읽기만). 갈래 BD01b 코드는 안 봤다. 고칠지는 사용자 · PE_emulator 결정이다.

| 지금 에뮬레이터 | 판정 |
|---|---|
| `nt_user_get_atom_name.cpp` — `0xc000` 아니면 0, 이름 글 안 씀 | **`0x8002` 갈래 틀림 — 지금 벽.** 정수 원자는 `#<십진>` + NUL, 반환 = 글자 수, `Length` 안 씀 |
| `nt_user_create_window_ex.cpp` — `0x8002` 거절 | **틀림** — `0x8002` 를 받아야 함 (원자 이름을 풀어도 바로 다음 벽 N7) |
| 같은 파일 — 늘 같은 hwnd `0x10010` | **틀림** — 한 번에 `1 + 컨트롤 수` 번 불림. 같은 hwnd 면 부품에 보낸 `SendMessageW` 가 대화상자 자신에게 가서 장부 `+0x20` 을 덮어쓸 수 있음 |
| 창 물체 · 핸들 표 · 공유 메모리 안 함 | **모자람** — 대화상자 하나만큼은 항목 · 사본 · **여분 블록** 이 꼭 필요 |
| `csr_user_connect_stub.cpp` — `gSharedInfo+0x8` · `+0x10` · `psi+0x8` = 0 | **모자람** — 핸들 표 블록 (배열 · 항목 크기 · 개수) 이 있어야 `ValidateHwnd` 통과 |
| 새 stub `NtUserSetDialogPointer` (몸 없음 → 0) | **틀림** — 반환 0 은 괜찮지만 **여분 블록 `+8` 에 `pdlg` 를 안 적으면 `+0x6af` 접근 위반** |
| `NtUserShowWindow` · `UpdateWindow` · `SetFocus` 등 (몸 없음 → 0) | **맞음** — 반환을 안 본다 |

**stub 이 채워야 하는 최소 목록** (run142 "가드 뒤 첫 벽까지" 7 · 8 을 채운 판, 줄임):

1. `NtUserGetAtomName` — `0 < 원자 < 0xc000` 이면 `#<십진>` + NUL, 글자 수 반환 (`0x8002` → 6). `Length` 는 안 건드림.
2. 공유 블록 — 핸들 항목 배열 · 항목 크기 (`≥ 0x1c`) · 개수 · 주 모니터 (run142) · (서수 부품이면) `psi+0x364` 원자.
3. TEB — `+0x78` (0 이면 항목 `+0x8` 도 0) · `+0x820` 0 아님 (run142 의 0 채운 `0x48` B 면 됨) · 나머지는 0 가능.
4. `NtUserCreateWindowEx` — `0x8002` 를 받고, **hwnd 마다 다른 값**.
   - 대화상자 hwnd: 항목 + 사본 (≥ `0x130` B, `+0x78` = **진짜 `DefDlgProcW` 주소**, `+0x80` 읽을 수 있는 클래스 블록, `+0xc8 ≥ 0x1e`, `+0x12` 는 **0**) + **0 채운 여분 블록** → 사본 `+0x128`.
   - 부품 hwnd: 0 아닌 서로 다른 값이면 고리는 지나감.
5. `NtUserSetDialogPointer` — `[[사본+0x128]+8] = pdlg` · fnid 0 이면 `0x2a4` · 사본 `+0x12 |= 1`. 반환 아무 값.
6. 나머지 (`ShowWindow` 등) — 기본 대답 0.
7. (sast/146 에서 보탬) 모달 고리 입구 · `EndDialog` 뒤 syscall · A 판.

자세한 칸은 run 문서 "PE_emulator 쪽".

### 2.13 앞 run 고쳐 읽기 — 닫힘과 바뀐 곳

틀린 것은 없고 `[추측]` · `[공백]` 이 정적으로 닫힌 것이라, run 문서가 "정정" 이 아니라 **"고쳐 읽기"** 로 각 문서 맨 위에 한 줄씩 넣었다. 해설서에도 맨 위에 한 줄씩 넣었다.

| 앞 run | 그때 | 이번에 |
|---|---|---|
| run131 | `NtUserGetAtomName` "반환 = 글자 수 (끝 NUL 제외 `[추측]`)" | **정수 원자는 `[정적]`** — 끝 NUL 을 쓰되 세지 않음, 커널은 `Length` 안 씀, `0x8002` → 6 · `#32770`. 표 원자의 진짜 글은 그대로 동적 (Q242) |
| run142 | 최소 길 표 19 ~ 27 `[공백]` (어느 호출이 syscall 없이 풀리나 · 표시 조건) | **`[정적]` 닫힘** — 직접 호출 조건 다섯 (2.8), 표시 · 초점 · 앞으로 내기 반환은 안 봄 (2.9) |
| run142 | 대화상자 자료 "어느 힙" `[추측]` | **`pUserHeap`** — 장부는 `AllocLookasideEntry`, 그걸 담는 여분 블록은 되부름 `0x7b` (2.6) |
| run142 | 최소 조건 7 "`ValidateHwnd` 를 통과하는 hwnd" | run146 "stub 이 채워야 하는 최소 목록" 으로 채움. 다음 요청 후보 N1 · N2 는 닫힘. 메시지 상자 템플릿의 컨트롤 수는 그대로 `[공백]` |

### 2.14 flow-writer 대조 — 결과서와 다르게 적은 곳 (짧게)

run 문서는 결과서 오프셋을 `.asm` 과 하나하나 맞췄다. **어긋난 곳 일곱** 은 run 문서 값을 쓴다 (이 글도 그 값을 썼다).

1. `ClassNameToVersion` 의 `RtlAllocateHeap` call 은 `+0x6f` 가 아니라 **`+0x74`** (`+0x6f` 는 크기 넣는 줄).
2. `GetStockObject(0x10)` 은 조각 **`+0x17a`** (= `InternalCreateDialog +0x11ab`). 결과서의 조각 `+0x1a3` 쪽은 `DeleteObject`.
3. `NtUserGetSystemMenu` 는 **`InternalCreateDialog +0x8cba9`** (조각 `FUN_1800ab5c3 +0x176`).
4. N14 의 `MOV RDI,R13` 은 조각 **`+0x382`** (= `+0x13b3`).
5. TEB `+0x81c` 를 "`UpdateWindow +0xd5`" 가 읽는다 → `UpdateWindow` 는 `+0x4f` 에서 끝나 그런 줄이 없다. 읽는 건 그 안 `ValidateHwnd +0xde`.
6. `psi` 창 프로시저 표 식은 **`psi + 0x248 + 8k` · `psi + 0x188 + 8k`** (fnid `0x2a4` 일 때 `psi+0x298` · `psi+0x1d8`).
7. 여분 블록이 비었을 때 넘어지는 줄은 **둘로 나뉜다** — `+0x6ab` (블록 없음) / `+0x6af` (`pdlg` 없음) (2.6).

보탬 넷: 조각 `+0x359` 의 `SendMessageWorker(사본, 0x127, …)` (`psi` 비트 조건이 다 맞을 때만 — `psi` 0 이면 안 탐) · `AllocLookasideEntry` 두 길 다 0 채움 · 클래스 `+4` 가 0 이어도 길 B · `Ordinal_2635` = PDB 이름 `IsWindowGdiScaledX`.

### 2.15 정리 — 비유로 한 번 더

- 직원이 **알림판 (대화상자)** 을 세우려 한다. 문지기 (run142) 를 지나면, 창 담당 건물이 그래픽 부서 신청 창구에 알림판 신청서를 낸다. 업종 칸엔 "**32770 번**" 이라는 숫자만 있다.
- 그래픽 부서는 그 번호가 `0xc000` 아래라 **장부를 펴지 않고** "#32770, 여섯 글자" 라고 읽어 준다. 에뮬레이터 대리 직원은 지금 "모르겠어요 (0)" 라고 해서 신청서가 그 자리에서 반려됐다.
- 신청서가 통과하면 그래픽 부서는 알림판을 등록하면서 **내선 `0x7b`** 로 창 담당 건물을 불러 **빈 사물함** 을 받아 온다. 사물함 위치는 가게 쪽 **열람용 사본** `+0x128` 에 적힌다.
- 창 담당 건물은 창 번호부 줄 (핸들 항목) 로 사본을 찾아 확인하고, 비품실에서 **빈 장부** 를 꺼내 그래픽 부서에 "사물함 둘째 칸에 꽂아 주세요" 한다. 그래픽 부서의 대답은 안 듣는다 — 대신 꽂혔는지는 바로 다음 줄에서 장부를 쓰며 알게 된다.
- 장부에 사장님 연락처 · 기본 단추 (IDOK) 를 적고, 부품을 하나씩 붙이고, 사장님께 "준비됐어요" 를 **창구 없이 내선으로** 전한다. 간판 켜기 (`ShowWindow`) 결과는 확인하지 않는다.
- 대리 직원이 할 일은 결국: 번호를 글자로 읽어 주기, 창마다 다른 번호 주기, 번호부 줄 · 열람용 사본 · **빈 사물함** 하나 마련하기, 장부를 사물함 둘째 칸에 꽂기.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 3aa3207 -- runs/03_sprint/flow.md`)

**그림은 그대로다.** 이번 길은 test.exe 초기화 밖이기 때문이다. 바뀐 곳은 글뿐이다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (ntkrnlmp · user32 · win32kbase · win32kfull 줄 · 근거 줄) | run145 까지 | run146 이 읽은 함수 보탬. 근거 줄에 "(run146) 정적 바퀴, 사진 값 대조 없음 — test.exe 는 user32 를 안 올린다" |
| 3장 창 API 하위 절 (run142) | 가드 뒤 대화상자 창 만들기는 틀만 | 한 줄 보탬 — "(run146) **가드 뒤 대화상자 창 만들기** `[정적]` … 아래 하위 절" |
| 3장 새 하위 절 (GDI 공유 핸들 표 하위 절 다음) | 없음 | **"대화상자 창 한 장이 hwnd 로 돌아올 때까지"** — 나무 한 장 + `[정적]` 여섯 줄 (정수 원자 이름 · 클래스 `0x8002` · `pdlg` · 여분 블록 되부름 `0x7b` · 컨트롤 · 반환 안 봄) |
| 3장 창 클래스 등록 절 (run131) | `NtUserGetAtomName` 반환 "글자 수 (끝 NUL 제외)" 는 `[추측]` | 한 줄 보탬 — "(run146) **원자 이름 글** `[정적]` … `[정적]` 으로 닫힘" |
| 4장 run131 줄 | 동적 3 · 정적 1 · PE_emulator 열림 | 정수 원자 몫 닫힘 표시 (표 원자는 그대로 Q242) |
| 4장 run142 줄 | 정적 N1 · N2 · PE_emulator 7 열림 | "N1 · N2 는 Q274a 로 당김 → **일부 해결 → run146**" (컨트롤 수 · `EndDialog` 칸은 sast/146, N3 · N5 는 Q274b) |
| 4장 새 줄 | — | Q274a 일부 닫힘 줄 + run146 줄 셋 (동적 → Q273 세션에 얹을 꼴 · 정적 → sast/146 · PE_emulator) |
| 5장 | — | run146 두 줄 |

- 같은 커밋에서 4장 run145 줄의 "다음 요청 후보" 에 대기열 번호 **Q281** 이 붙었다 (run145 문서도 같이).
- 짐 지도 (`baggage.md`) 는 안 바뀐다 — **새 짐 칸 없음** (이번 길은 초기화 밖).

태그 변화만 뽑으면:

- 정수 원자 이름 · 반환 (끝 NUL 제외) — `[추측]` → **`[정적]`**
- 대화상자 자료 힙 — `[추측]` → **`[정적]` (`pUserHeap`)**
- 최소 길 19 ~ 27 (syscall 없이 풀리는 호출 · 표시 조건) — `[공백]` → **`[정적]`**
- 칸 이름 (`tagWND` 류) — 여전히 **`[추측]`** (PDB 에 형식 없음)

### 큰 그림에서 어떻게 이어지나

- 창 API 쪽 옆길을 시간 순서로 늘어놓으면 이렇게 이어진다:
  - run85 · run86 — user32 · gdi32 `DllMain` (프로그램이 user32 를 올릴 때)
  - run131 — 창 클래스 등록 (COM STA)
  - run142 — 창 API 넷의 **문지기**
  - run144 — GDI 공유 핸들 표
  - **run146 — 문지기 뒤 대화상자 창 만들기 → hwnd** ← 이번
  - sast/146 — 모달 고리 → 기본 단추 → `EndDialog` (진행 중)
- 이번에 **창 하나를 "user32 가 믿게" 만드는 데 필요한 칸이 다 나왔다** — 핸들 항목 · 창 사본 · 여분 블록 · 대화상자 자료. sast/146 은 이 칸들 위에서 메시지 고리와 끝을 본다.
- 그리고 "되부름 바퀴는 이 길에선 필요 없다" 는 답 덕분에, 에뮬레이터는 커널 → 유저 되부름을 통째로 짓지 않고도 대화상자 하나를 만들 수 있다 `[정적]`.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q273** (동적, VM GUI 세션 — 미룸)

run146 동적 1 ~ 10 은 같은 VM GUI 세션이라 새 줄 없이 **Q273 에 출처로 보탰다** (plan 결정 기록). test.exe 가 아니라 `DialogBoxParamW` 를 부르는 GUI 프로세스가 필요하다. 대표만:

| 무엇을 | 왜 이번에 못 알았나 |
|---|---|
| `ClassNameToVersion+0x22d` 에서 `ax` 가 정말 6, 버퍼가 `#32770` 인가, 그때 `Length` 는 아직 0 인가 | 정적으로 길만 봤다. 값은 실행해야 보인다 |
| `InternalCreateDialog+0x658` 뒤 진짜 사본 칸 (`+0x78` 이 `DefDlgProcW` 인가 · `+0xc8` 이 `0x1e` 인가 · `+0x128`) 과 핸들 항목 (`+0x8` == TEB `+0x78`?) | 칸을 **누가 읽나** 는 봤지만 진짜 Windows 가 **무슨 값** 을 넣는지는 못 봄 |
| `xxxCreateWindowEx+0x1446 / +0x1a46` 되부름 `0x7b` 입력 (`0x1e`?) 과 돌아온 포인터 | 커널 멈춤점 + 프로세스 조건 필요 |
| `SendMessageWorker` 가 진짜로 길 A 로 가나 길 B 로 가나 (`+0x3f3`) | `psi` 표 칸 값에 달림 |
| TEB `+0x78` 이 진짜 스레드 ID 인가 · `gSharedInfo+0x10` (항목 크기) 진짜 값 | run142 동적과 겹침 |

### 정적으로 남은 것 → **sast/146 dialog-end** (Q274a 물음 3, 진행 중)

- 원문 물음 3 — `DialogBox2` 고리 · `IsDialogMessageW` · `DispatchMessageWorker` · `EndDialog` 뒤 syscall (장부 `+0x18` 비트 0 · `+0x20` 결과값 겸 기본 단추 ID).
- `GetDlgItem` (사본 `+0x140` · 자식 연결), `_SetWindowLongPtr` / `_GetWindowLongPtr` (`DWLP_USER` = 여분 블록 `+0x10`).
- A 판 갈림 (2.8 의 글자 종류 단계) · 모달 아닌 판 (`CreateDialogParam*`).
- `DefWindowProcWorker` (`WM_GETDLGCODE` 가 앱에서 0 으로 올 때), (필요하면) 길 A 일꾼을 채우는 쪽.

### 다른 대기열로 간 것

- **Q242** (동적) — 표 원자 (`0xc000` 이상) 의 진짜 이름 글. 정수 원자 몫만 이번에 닫혔다.
- **Q278** — `CreateDlgFont` 안의 gdi 부름.
- **Q274b** (정적) — Q274 의 나머지 N3 (`xxxCreateThreadInfo` TEB `+0x820` / `+0x828` 채우기) · N5 (`GetSystemMetrics` 지표 칸 · `LoadIcoCur` 자원 길).

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- `[공백]` `_MonitorFromRect` 가 모니터 칸이 0 일 때 정말 NULL 인지 (N5, 본문 안 읽음).
- `[공백]` 부품 프로시저 (`ButtonWndProcWorker` 류) 본문 — 부품 사본 비트 2 / 항목 없음으로 건너뛸 수 있다.
- `[추측]` 사본 `+0x12` 비트 0 · 2 · 3, 장부 `+0x40` · `+0x50` 의 이름 (형식이 PDB 에 없음).
- `[공백]` `0x7b` 말고 다른 되부름이 부품 사본 `+0x128` 을 채우는지 (부품 클래스의 여분 크기).
- `[추측]` WinDbg 에선 `user32!InternalCreateDialog+0x8ca16` 처럼 떨어진 조각 오프셋 대신 주소 (`user32+0xab5a6`) 로 멈춤점을 걸어야 할 수 있다 (run 문서 "디버거 메모").

### 짐 지도

- **새 짐 칸 없음.** 이번 길은 test.exe 초기화 밖 (user32 안 올림) 이라 짐 지도 2장 숫자는 그대로다.

---

## 5. 더 읽기

- run 문서: [`run146-dialog-window.md`](run146-dialog-window.md)
  - "정적 — 호출 지점" 표 · 1절 (원자 층) · 2절 (인자 · 돌아온 뒤 검사 · `pdlg` 칸) · 3절 (컨트롤 고리) · 4절 (직접 호출 조건) · 5절 (되부름) · 6절 (칸 표) · 7절 (NULL 가지) · 8절 (GDI) · "PE_emulator 쪽" · "flow-writer 대조" · "앞 run 고쳐 읽기"
- 요청서: [`_workspace/sast/142_dialog-window.md`](../../_workspace/sast/142_dialog-window.md)
- 결과서: [`_workspace/sast/142_dialog-window_result.md`](../../_workspace/sast/142_dialog-window_result.md) — "먼저 읽는 한 줄 답" · 2절 (원자) · 3절 (인자 · 대화상자 자료) · 4절 (칸 표) · 6절 (메시지 보내기 · 되부름) · 9절 (지금 우리 맞나) · 10절 (동적)
- 원 요청: `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md`
- flow.md: [3장 — "대화상자 창 한 장이 hwnd 로 돌아올 때까지" 하위 절 · 창 API 하위 절 · 창 클래스 등록 절](flow.md) · 4장 run131 · run142 · run146 줄 · 5장 run146
- 앞 해설:
  - [run85 해설](run85-user32-init-해설.md) 2.5 (공용 안내 묶음 · 서버 정보판) · 2.12 (커널 콜백 · 내선 번호표)
  - [run131 해설](run131-user32-regclass-해설.md) 1절 (창 클래스 · ATOM) · 2.10 (`NtUserGetAtomName` · `NtUserCreateWindowEx` 사슬)
  - [run142 해설](run142-user32-connect-해설.md) 2.3 (문지기) · 2.6 (부수 효과 · 방 정보 카드) · 2.9 (모달 고리 · 대화상자 자료 `+0x18` · `+0x20`)
  - [run144 해설](run144-gdi-startup-해설.md) 2.13 (GDI 공유 핸들 표의 칸)
  - [run145 해설](run145-epm-endpoint-해설.md) 2.2 (CFG 간접 호출)

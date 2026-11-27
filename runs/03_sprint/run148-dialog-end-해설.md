# run148 dialog-end — 해설

> **고쳐 읽기 (run157)**: 이 글이 "커널 몫 (Q282)" 으로 남긴 것이 대부분 `[정적]` 으로 닫혔다. 2.11 · 4절의 비트표 (`user32+0xda568 ~`) 는 `NtUserInitializeClientPfnArrays` 가 아니라 **`NtUserProcessConnect` 가 돌려준 `gSharedInfo` 복사** 이고 한도 `0x33f` · `0x349` — `WM_GETDLGCODE` 0 길은 **syscall 0**. 4절 "자식 연결이 템플릿 차례인가" → 커널이 **맨 끝에 붙인다** (= 템플릿 차례, 이전 형제 `+0x50` 도). 2.10 `+0x12` 비트 3 은 확장 스타일만으론 안 켜지고 A 판 `+0x78` = `DefDlgProcA`. 2.6 IMM 훅 = `imm32!ImmTranslateMessage`. 2.13 의 `MessageBoxWorker` · `MessageBoxTimeoutW` 는 반환을 안 바꾼다. 2.12 "가장 짧게 누르기" 의 ID 는 `pdlg+0x20` 이 기본 단추로 덮였을 때만 맞다 — `MB_YESNO` 의 기본은 `IDYES (6)`. `xxxPeekMessage` 는 이 빌드에 없다 (몸 `xxxRealInternalGetMessage`, 첫 메시지 = `WM_PAINT`). 자세히는 [run157 문서](run157-dialog-win32k.md).

> 이 글은 [run148 문서](run148-dialog-end.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q274a 둘째 몫** — PE_emulator 요청 "모달 대화상자가 메시지 하나로 `EndDialog` 까지 가서 빠지는 길" (`_workspace/plan.md`, 이번에 **Q274a 끝 (run148)**)
  - 원 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` 의 **물음 3** "기본 단추 한 번으로 끝". 물음 1 · 2 는 [run146](run146-dialog-window-해설.md) 이 했다.
  - 튜닝 `357_tune17.md` GC1, 병목 `dialog_box_fail` 악성 60 (요청 쪽 에뮬레이터 숫자).
  - 사용자 결정 2026-11-26 "다른 것보다 가장 먼저". 원문 "3 은 run 을 나눠도 됨" 에 따라 run146 과 나눴다. 정적만.
  - 결과서 판정 **분석성공** — 물음 3 (a)(b)(c)(d) 와 sast/142 11절 (앱 프로시저가 거의 늘 부르는 것) 모두 `[정적]` 줄 근거로 답함.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q273** (같은 VM GUI 세션에 출처로 보탬)
    - 정적 → **Q282** (결과서 "win32k 몫 남음" 1 ~ 7 + "다음 요청 후보")
- 근거:
  - run 문서 [`run148-dialog-end.md`](run148-dialog-end.md) (가장 중요)
  - 요청서 [`_workspace/sast/146_dialog-end.md`](../../_workspace/sast/146_dialog-end.md) · 결과서 [`146_dialog-end_result.md`](../../_workspace/sast/146_dialog-end_result.md)
    - **처음 analyze 없음.** user32 만 `-readOnly` export (새 이름 36개) · refs 4개.
    - **win32kfull · win32kbase 는 안 열었다.** 작업하는 동안 sast/145 (GDI) 결과서가 아직 없어서, 요청서 ⚠ 대로 열지 않고 "win32k 몫 남음" 목록만 남겼다. user32 쪽에서 다 닫혀서 꼭 필요하지도 않았다.
  - flow.md 변경: docs 커밋 `e7f7278` (`git show e7f7278 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 user32 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run131 · run142 · run146 과 같은 줄).
   - 그래서 이번 이야기는 **"user32 를 쓰는 프로그램이 대화상자를 띄운 뒤, 그게 닫힐 때까지"** 의 일이다. 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다.
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - "악성 60", "부모 0 이 80 / 80", "A 판 15", "`CreateDialogParam*` 13" 같은 것은 PE_emulator 로그다. 진짜 Windows 를 본 게 아니라서 `[관찰]` 이 아니다.
   - run 문서가 "표본" 이라 부르는 것도 이 로그다. 이 글에서는 "에뮬레이터 로그 표본" 이라고 부른다.
3. **오프셋 읽는 법** (앞 해설들과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문 · 표의 "`+0x70`", "`+0x3bd RSI = …`" 처럼 명령과 같이 적힌 숫자 = **함수 안 그 줄** (call 자리 포함). run 문서 맨 위에도 "글의 오프셋은 함수 안 줄 · call 자리, ret 는 호출 지점 표" 라고 적혀 있다.
   - "`+0x272 JZ → +0x20a`" 는 그 줄과 점프가 가는 곳.
   - **`IsDialogMessageW` 는 조각과 "주인 없는 덩어리" 가 섞인다.**
     - 조각 = Ghidra 가 따로 떼어 이름 붙인 몸 일부 (`FUN_1800123b9` 같은 것). run 문서는 "조각 `+0x5` (= `+0x36e`)" 처럼 두 기준을 같이 적는다.
     - 덩어리 = 어느 함수에도 안 붙은 먼 곳 코드. 그래서 `+0x98b19` 처럼 **함수 시작에서 아주 먼** 오프셋이 나온다. 함수 시작에서 잰 거리라서 이상한 게 아니다.
   - 콜스택으로 본 적은 없다 (정적만).
4. **값은 하나도 안 봤다.**
   - 진짜 Windows 에서 처음 오는 메시지가 무엇인지, TEB `+0x860` 에 무엇이 있는지는 이번 범위 밖이다 (동적 Q273 몫).
   - 이번에 본 건 "**코드가 어느 칸을 읽고 쓰고, 어떤 반환을 보고, 0 이면 어디로 가나**" 뿐이다.
5. **칸 이름은 전부 짐작이다.**
   - PDB 에 `tagWND` · `tagDIALOG` · `tagMSG` 형식이 **없다** (run146 확인) `[정적]`.
   - 그래서 `DLGC_DEFPUSHBUTTON`, `WS_DISABLED`, `DWLP_USER` 같은 **이름은 `[추측]`**, **오프셋과 그 칸을 읽는 줄은 `[정적]`** 이다.
6. **비유 이름이 run 문서와 다르다.**
   - run 문서 "결과" 비유: **번호표 기다리는 창구 · 창구 직원 (user32) · 본사 (커널) · 사장님 · 장부 · 도장** (run 문서는 이 비유를 `[추측: 비유]` 로 둔다).
   - 그런데 앞 해설들에선 이미 **신청 창구 = syscall, 관리사무소 = 커널** 이다. "창구" 를 두 뜻으로 쓰면 헷갈리니, 이 글은 앞 해설 그림에 맞춰 부른다 (아래 "비유"). 사장님 · 장부 · 도장은 그대로 빌려 쓴다. 그림만 다르고 사실은 같다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- TEB = 직원 **개인 안내판** → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.2
- user32 = **창 담당 건물**, win32u = **단축번호 버튼판**, `psi` = **서버 정보판** → [run85 해설](run85-user32-init-해설.md) 2.1 · 2.5
- **run142 에서 푼 것** → [run142 해설](run142-user32-connect-해설.md)
  - **모달 고리** (알림판 앞에서 누가 단추를 누를 때까지 기다리는 직원) · **창 메시지 (`WM_*`)** = **알림 카드** → 2.9
  - **접근 위반** (빈 칸을 밟다 발이 빠짐) → 2.8
- **run145 에서 푼 것** → [run145 해설](run145-epm-endpoint-해설.md)
  - **CFG 간접 호출 (`⇢`)** → 2.2 줄 ①
- **run146 에서 푼 것** → [run146 해설](run146-dialog-window-해설.md)
  - **핸들 항목** (창 번호부의 한 줄) · **창 사본** (열람용 사본) · **fnid** (종류 도장) → 2.5
  - **여분 블록** (빈 사물함) · **대화상자 자료 `pdlg`** (장부, 사물함 둘째 칸) · **되부름** (내선 전화) → 2.6
  - **컨트롤** (알림판 부품) → 2.7
  - **창 프로시저** (담당 직원, `DefDlgProcW`) · **앱 프로시저** (사장님) · `SendMessageWorker` 가 syscall 없이 부르는 조건 → 2.8
  - **stub** (대리 직원) → 2.11

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, TEB = 직원 **개인 안내판**, 커널 = **관리사무소**, syscall = **신청 창구**.
- user32 = **창 담당 건물**, win32k = 관리사무소 **그래픽 부서**.
- 대화상자 = **알림판**, 컨트롤 = 알림판 **부품** (단추 등), 창 사본 = **열람용 사본**, 여분 블록 = **빈 사물함**, `pdlg` = 사물함 둘째 칸의 **장부**.
- 창 프로시저 = 알림판 **담당 직원** (`DefDlgProcW`), 앱 프로시저 = 알림판 주인 **사장님**.
- 모달 고리 = 알림판 앞에서 **기다리는 직원**, 창 메시지 = **알림 카드**, stub = 대리 직원.
- 이번에 새로 붙는 그림:
  - 메시지 큐 = 직원 앞 **카드함**. 알림 카드가 여기 줄 서 있다.
  - `NtUserPeekMessage` = 창구에 "카드함에 카드 있어요?" 하고 **들여다보기**. `NtUserWaitMessage` = 카드가 올 때까지 **졸기**.
  - `SendMessageW` = 받는 쪽에 **내선으로 바로 전하기** (run146 그대로). `PostMessage` = 카드함에 **넣어 두기**.
  - `IsDialogMessageW` = 카드가 오면 먼저 거치는 **알림판 전용 분류대**.
  - `EndDialog` = 사장님이 장부에 **"끝" 도장** 을 찍고 **결과** 를 적는 것.
  - 바쁜 고리 = 졸기가 0 초라서 **카드함 뚜껑만 쉼 없이 여닫는 직원**.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| (a) 진짜에서 Enter 는 어떤 카드로 오나 | **`WM_KEYDOWN` (`0x100`) · `VK_RETURN` (`0xd`)**. `IsDialogMessageW` 가 그걸 **`SendMessageW(hDlg, WM_COMMAND, ID, 컨트롤)`** 로 바꿔 보낸다 (카드함엔 안 넣음) `[정적]` (2.4) |
| (a) 기본 단추 ID 는 어디서 | `DM_GETDEFID` 의 답 = **장부 `+0x20` 아랫 16비트**. 이 칸이 **`EndDialog` 결과값 칸과 같다**. 처음 값 1 `[정적]` (2.5) |
| (b) `WM_COMMAND` 를 곧장 주면 | `IsDialogMessageW` → `TranslateMessage` → `DispatchMessageWorker` → `DefDlgProcW` → `DefDlgProcWorker` → 앱 프로시저까지 **win32k syscall 0번** `[정적]` (2.6) |
| (b) `EndDialog` 는 무엇을 | 장부 `+0x18 \|= 1` · `+0x20 = n` 을 쓰고, `NtUserSetWindowPos` · `NtUserPostMessage` 를 부르고, **반환은 안 본다** `[정적]` (2.7) |
| (c) 고리는 어떻게 빠지고 무엇을 돌려주나 | 고리 맨 위 (`+0x12a ~ +0x142`) 에서 "끝" 도장을 보고 빠진다. **장부 `+0x20` 을 먼저 읽고 (`+0x3bd`)** `NtUserDestroyWindow` (`+0x3c4`, 반환 안 봄) 뒤 그 값을 돌려준다 `[정적]` (2.8) |
| (c) 끝없는 고리는 | 빠지는 길은 **넷뿐**, 횟수 · 시간 한도 없음. `NtUserPeekMessage` 가 늘 0 이면 (= 지금 에뮬레이터) **끝없는 바쁜 고리** `[정적]` (2.9) |
| (d) A 판 · 모달 아닌 판 | A 판 끝 길은 W 판과 **같다**. `CreateDialogParam*` 은 고리 없이 `InternalCreateDialog` 에서 끝 `[정적]` (2.10) |
| (sast/142 11절) 앱이 거의 늘 부르는 것 | `GetDlgItem` · `GetWindowLongPtrW` 는 syscall 0. **`SetWindowLongPtrW` 는 (`g_systemCallFilterId ≠ 5` 면) syscall** — stub 이 같은 사물함 칸에 써야 한다 `[정적]` (2.11) |
| 에뮬레이터에 넣을 가장 짧은 곳 | **`NtUserPeekMessage` 의 출력 한 번** (`WM_COMMAND`, wParam = `LOWORD(pdlg+0x20)`). 그러면 syscall 은 넷 (2.12) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**초기화가 끝난 뒤, 프로그램이 띄운 대화상자가 닫혀서 `DialogBoxParamW` 가 돌아오는 순간** 이다. test.exe 에는 없는 옆길이다.

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
                                                        ├─ 창 만들기 → hwnd 돌려받기           ← run146
                                                        └─ 모달 고리 → EndDialog → 돌아옴      ← 이번 (run148)
```

- flow.md 에서는 **3장 "대화상자 창 한 장이 hwnd 로 돌아올 때까지" 하위 절 (run146) 바로 뒤** 에 새 하위 절 "모달 대화상자가 메시지 하나로 끝까지" 로 들어갔다.
- 프로그램 쪽에서 부르는 일이라 CLAUDE.md 범위로 치면 "`main` 안에서 OS 를 다시 부르는 화살표" 쪽이다. 그래서 그림엔 안 넣었다.

### 왜 이 질문이 지금 왔나

- run146 으로 "창이 서는 길" 은 닫혔다. 에뮬레이터 stub 이 그 목록대로 채워지면 `InternalCreateDialog` 가 hwnd 를 돌려준다.
- 그다음 `DialogBox2` 의 **모달 고리** 에 들어간다 ([run142 해설](run142-user32-connect-해설.md) 2.9).
- PE_emulator 정책 D156 "보이지 않는 가짜 창" 의 셋째 몫은 **"대화상자는 사람이 기본 단추를 바로 누른 것으로 친다"** 이다.
- 그러려면 알아야 한다:
  - 사람이 누른 것을 **어떤 카드로, 어디에** 넣어야 하나?
  - 그 카드가 사장님 (앱) 께 닿고, 사장님이 "끝" 을 외친 뒤, 고리가 **진짜로 빠져나오나**?
  - 그 길의 syscall 들 (`NtUserPeekMessage` · `NtUserDestroyWindow` …) 은 **무엇을 돌려줘야** 하나?

### 무엇이 궁금했나

1. **(a)** 진짜에서 Enter 는 고리에 어떤 메시지로 오나. 기본 단추 ID 는 어디서 읽나 (`DM_GETDEFID` · 장부 `+0x20` 겸용인가).
2. **(b)** `WM_COMMAND(ID, BN_CLICKED)` 를 대화상자 앞으로 곧장 주면 앱 프로시저까지 syscall 없이 가나. `EndDialog` 가 쓰는 칸과 부르는 syscall.
3. **(c)** `EndDialog` 뒤 고리가 빠질 때 syscall 반환을 보나. `DialogBoxParamW` 반환은 어디서 오나. `NtUserPeekMessage` 반환은 어떻게 쓰나. **끝없이 도는 꼴 (금지 꼴)** 은.
4. **(d)** A 판 (`DialogBoxIndirectParamA`) · 모달 아닌 판 (`CreateDialogParam*`) 이 갈라지는 자리만.
5. **(sast/142 11절)** 앱 프로시저가 거의 늘 부르는 것 (`GetDlgItem` · `SetWindowLongPtrW(DWLP_USER)` · `GetParentDialog` · `WM_GETDLGCODE` 가 0 일 때).

### 바퀴 전에 알던 것

- `[정적]` (run142) 모달 고리의 **틀**:
  - `DialogBox2` 가 `_PeekMessage` → `NtUserPeekMessage` 로 메시지를 묻고, 없으면 `NtUserWaitMessage` 로 잔다.
  - 있으면 `IsDialogMessageW` → `TranslateMessage` → `DispatchMessageWorker`.
  - 매 바퀴 장부 `+0x18` 의 "끝 표시" 를 보고, 켜져 있으면 `NtUserDestroyWindow` (`+0x3c4`) 뒤 결과를 돌려준다.
  - `EndDialog` 가 장부 `+0x18` 에 끝 표시, `+0x20` 에 결과를 쓴다.
  - **`WM_QUIT` 이면 `EndDialog` 없이 빠지고 반환은 0** — 이번에 **틀렸다고** 정정된다 (2.13).
- `[정적]` (run146) 창 · 사본 · 사물함 · 장부의 칸:
  - 장부 `+0x20` 은 `InternalCreateDialog +0x6fa` 에서 **처음 1**, 컨트롤 고리가 `WM_GETDLGCODE` 답 `& 0x10` 인 컨트롤 ID 로 바꿀 수 있다.
  - 그 칸을 "기본 단추 ID 겸 결과값" 이라고 불렀지만, 그 **이름 (뜻)** 은 `[추측]` 이었다.
  - 사본 `+0x78` = `DefDlgProcW` 여야 앱 프로시저까지 간다. fnid 는 `NtUserSetDialogPointer` 가 `0x2a4` 로 적는다.
- `[공백]` (run146) 남겨 둔 것 — 고리 · `IsDialogMessageW` · `DispatchMessageWorker` 의 갈래, `EndDialog` 뒤 syscall 반환, `GetDlgItem` · `SetWindowLongPtrW`, A 판 갈림. 이게 sast/146 으로 넘어왔다.

---

## 2. 찾은 것

### 2.1 한눈에 — 고리가 카드 한 장으로 끝날 때까지 `[정적]`

run 문서 "호출 관계" 나무를 대표 줄만 남긴 것이다 (오프셋은 ret 자리, `⇢` 는 CFG 간접 · 보내기).

```
user32!InternalDialogBox +0x8f DialogBox2(hwnd, 소유자 0, …)
 ├─ 고리 맨 위 +0x12a ~ +0x142  [[사본+0x128]+8] NULL / +0x18 비트0 → +0x389 빠짐
 │   ├─ +0x270 _PeekMessage +0x3f NtUserPeekMessage ── SYSCALL 0x1001        반환 0 / 0 아님만
 │   │   0 → 빈 틈: (보임·소유자 0) +0x246 NtUserWaitMessage ── SYSCALL 0x100a → 맨 위
 │   ├─ +0x283 WM_QUIT → +0x384 NtUserPostQuitMessage → 빠짐
 │   ├─ +0x2a0 IsDialogMessageW(hwnd, &msg)
 │   │   ├─ (WM_KEYDOWN VK_RETURN) +0x98b51 SendMessageW(DM_GETDEFID) ⇢ DefDlgProcWorker +0xa4b pdlg+0x20
 │   │   │   → +0x98b1e SendMessageW(hDlg, WM_COMMAND, ID, 컨트롤)
 │   │   └─ (WM_COMMAND 등 기본) +0x131 TranslateMessage · +0x13b DispatchMessageWorker
 │   │        ⇢ DefDlgProcW → DefDlgProcWorker (fnid 0/0x2a4) +0xc4 UserCallDlgProcCheckWow
 │   │          ⇢ 앱 프로시저 → EndDialog(hDlg, n)
 │   │             ├─ +0x70 pdlg+0x18 |= 1 · +0x7f pdlg+0x20 = n
 │   │             ├─ +0xb0 NtUserSetWindowPos(…, 0x97) ── SYSCALL 0x1021   반환 안 봄
 │   │             └─ +0xe0 NtUserPostMessage(hDlg, 0, 0, 0) ── SYSCALL 0x100d   반환 안 봄
 │   └─ +0x121 HMValidateHandleNoSecure (0 → 빠짐, 반환 0) → 맨 위
 └─ 빠짐 +0x389 → +0x3a4 HMValidateHandleNoSecure → +0x3bd RSI = pdlg+0x20
         → +0x3cb NtUserDestroyWindow ── SYSCALL 0x1095   반환 안 봄
         → +0x3ea RAX = RSI → DialogBoxParamW 반환
```

다섯 덩어리로 보면 쉽다.

1. **카드 꺼내기** — 카드함에 카드가 있나 묻는다. 없으면 존다 (2.3).
2. **분류대** — 키 카드면 "단추 눌렸어요" 카드로 바꿔 바로 전하고, 그 밖의 카드는 담당 직원에게 넘긴다 (2.4 · 2.6).
3. **사장님의 "끝"** — 사장님이 `EndDialog` 로 장부에 도장과 결과를 적는다 (2.7).
4. **빠지기** — 다음 바퀴 맨 위에서 도장을 보고 나온다. 결과를 먼저 베끼고 알림판을 철거한다 (2.8).
5. **돌려주기** — 베낀 결과가 `DialogBoxParamW` 의 반환이 된다 (2.8).

### 2.2 한 줄씩 읽는 법

#### 줄 1: `│   ├─ +0x270 _PeekMessage +0x3f NtUserPeekMessage ── SYSCALL 0x1001        반환 0 / 0 아님만`

- **누가**: user32 `DialogBox2` (rva `0x11154`). 고리 안에서 매 바퀴 한 번.
- **어디서**: `DialogBox2` 의 call 자리 `+0x26b`, 돌아오는 자리 (ret) `+0x270`. 부르는 건 `_PeekMessage(&msg, 0, 0, 0, 1, 0, 0)` 이다.
  - 그 안에서 다시 call `+0x38` · ret `+0x3f` 로 win32u `NtUserPeekMessage` (번호 `0x1001`) 를 부른다.
- **누구를**: 그래픽 부서 (win32k) 의 `NtUserPeekMessage`.
- **무엇을 위해**: 카드함에서 **카드 한 장 (MSG, 0x30 B)** 을 꺼내 오려고. 카드는 `DialogBox2` 스택 `[RSP+0x50]` 에 받는다.
- **돌아와서 무엇을 보나**:
  - `_PeekMessage` 는 `+0x44 MOV EBX,EAX` · `+0x57 MOV EAX,EBX` — syscall 반환을 **그대로** 돌려준다.
  - `DialogBox2` 는 `+0x270 TEST` · `+0x272 JZ → +0x20a` — **0 이냐, 0 아니냐만** 본다. 0 이면 "빈 틈" 으로 간다.
- 태그 `[정적]`.

#### 줄 2: `└─ 빠짐 +0x389 → +0x3a4 HMValidateHandleNoSecure → +0x3bd RSI = pdlg+0x20 → +0x3cb NtUserDestroyWindow`

이 줄엔 **두 종류 오프셋이 섞여 있다**. 읽는 연습으로 좋다.

- `+0x389` = 점프 목적지 (고리 맨 위에서 "끝" 도장을 보면 여기로 뛴다).
- `+0x3a4` = ret 자리 (call 은 `+0x39f`).
- `+0x3bd` = **함수 안 그 줄** — `RSI = [pdlg+0x20]`. call 이 아니라 그냥 읽기다.
- `+0x3cb` = ret 자리 (call 은 `+0x3c4`). 바로 그 자리 명령이 `NOP` 이다 → **반환을 안 본다**.
- **무엇을 위해**: 알림판 hwnd 가 아직 살아 있나 확인하고 (`HMValidateHandleNoSecure`, 0 이면 반환 0), **결과를 먼저 베낀 뒤**, 알림판을 철거한다 (`NtUserDestroyWindow`, 번호 `0x1095`).
- 태그 `[정적]`.

### 2.3 ★ 고리 한 바퀴 다시 — 카드함 들여다보기와 졸기 `[정적]`

run142 해설 2.9 에서 고리의 틀은 봤다. 이번엔 **반환을 어떻게 보는지** 까지 봤다.

**새 말 — 메시지 큐**: 창 메시지 (알림 카드) 가 **도착한 순서대로 줄 서 있는 곳**. 일반적으로는 그래픽 부서 (커널) 가 GUI 스레드마다 하나씩 둔다고 알려져 있다 `[추측: 일반적으로 알려진 설명]`. user32 는 이걸 직접 못 열고 `NtUserPeekMessage` 로 "하나 꺼내 줘" 하고 묻는다 `[정적]`. 비유: 직원 앞 **카드함**.

**새 말 — 빈 틈 (idle)**: run 문서가 쓰는 말. `NtUserPeekMessage` 가 0 (= 카드 없음) 을 돌려준 순간이다.

빈 틈에서 하는 일은 셋 중 하나다 (E11, `+0x20a ~ +0x24b` 등):

| 상태 | 하는 일 | 줄 |
|---|---|---|
| ① 알림판이 아직 안 보임 (사본 `+0x1f` 비트 4 꺼짐) | `NtUserShowWindow(hwnd, 1)` (`0x1052`) + `UpdateWindow` (+ 조건부 `NtUserNotifyWinEvent(0x10)`) | `+0x2da` · `+0x2e9` · `+0x312` |
| ② 보임 · 소유자 ≠ 0 · 이번 틈 처음 | 소유자에게 `WM_ENTERIDLE (0x121)` 한 번 | `+0x111` |
| ③ 그 밖 (에뮬레이터 로그 표본 — 부모 0) | `HMValidateHandleNoSecure` 로 hwnd 확인 → **`NtUserWaitMessage`** (`0x100a`) → 맨 위로 | `+0x222` · `+0x23f` · `+0x24b JMP +0x119` |

- **셋 다 반환을 안 본다** `[정적]`.
- `NtUserWaitMessage` 는 이름대로라면 "카드가 올 때까지 재워 줘" 다. 그런데 user32 는 **깨어난 이유도, 반환도 안 본다.** 돌아오면 그냥 맨 위로 가서 다시 묻는다.

**한 가지 더 — `NtUserGetMessage` 는 안 부른다** `[정적]`.

- 앱이 자기 고리 (`while (GetMessage(...))`) 를 돌 때 쓰는 건 `GetMessageW +0x1b NtUserGetMessage` (`0x1004`) 다. 반환을 그대로 준다 (`+0x43`).
- 하지만 **모달 고리 `DialogBox2` 는 이걸 안 부른다.** 들여다보기 (`Peek`) + 졸기 (`Wait`) 조합이다.
- 이게 에뮬레이터 쪽 판정 하나를 바꾼다 (2.12).

비유: 알림판 앞 직원은 창구에 "카드함에 카드 있어요?" 하고 묻는다. 대답은 "있다 / 없다" 만 듣는다. 없으면 알림판이 꺼져 있나 보고 켜 둔 뒤, 꺼져 있지 않으면 **졸기 신청** 을 낸다. 졸다 깨면 이유는 안 묻고 다시 "카드 있어요?" 한다.

### 2.4 ★ (a) Enter 는 어떤 카드로 오나 — 분류대가 바꿔 준다 `[정적]`

**새 말 — `WM_COMMAND` (`0x111`)**: 단추 같은 부품이 "**나 눌렸어요**" 하고 알림판 (부모 창) 에 알리는 카드. 칸은 이렇게 쓰인다 (run 문서 8절 MSG 표):

- wParam = 단추 **ID** (아랫 16비트) + 알림 종류 (윗 16비트, 눌림 `BN_CLICKED` = 0) — 그래서 그냥 ID 와 같다.
- lParam = 그 단추의 **hwnd** (없으면 0).
- 비유: "**1번 단추 눌렸어요**" 카드.

**새 말 — 기본 단추**: 사람이 **Enter 만 쳐도 눌린 것으로 치는 단추**. 보통 "확인" 이다 `[추측: 일반적으로 알려진 설명]`. 이번 코드에서는 "기본 단추 ID 를 고르는 차례" 가 줄로 보인다 `[정적]` (아래).

**새 말 — 보내기 (Send) 와 부치기 (Post)**:

- `SendMessageW` = 받는 창 프로시저를 **바로 부른다**. 카드함을 안 거친다 (run146 해설 2.8 의 "내선으로 바로").
- `PostMessage` = 카드를 **카드함에 넣어 둔다**. 나중에 고리가 `NtUserPeekMessage` 로 꺼내야 읽힌다.
- 이 차이가 이번 결과의 열쇠 중 하나다.

이제 Enter 를 따라가 보자.

1. 사람이 Enter 를 친다. 고리에 오는 카드는 **`WM_KEYDOWN` (`0x100`)**, wParam = **`VK_RETURN` (`0xd`)**, hwnd = 포커스 창 `[정적]`.
   - `WM_COMMAND` 가 처음부터 오는 게 **아니다**.
2. `DialogBox2 +0x29b` 가 카드를 `IsDialogMessageW(hwnd, &msg)` 에 먼저 보낸다.
   - **새 말 — `IsDialogMessageW`**: 대화상자 고리에서 카드를 받자마자 먼저 거치는 함수. Tab · Enter · Esc 같은 **대화상자용 키** 를 알아서 처리한다. 처리했으면 TRUE 를 돌려주고, 그러면 고리는 그 카드를 따로 번역 · 배달하지 않는다 (`+0x2a2 JNZ → +0x2ba`) `[정적]`. 비유: 알림판 전용 **분류대**.
3. 분류대 안: `+0xf2 ~ +0x103` 에서 "`message − 0x100 == 0`?" → 키 카드 → `+0x175`.
4. `+0x184 SendMessageW(msg.hwnd, WM_GETDLGCODE 0x87, …)` — 포커스 창에게 "**너 어떤 키를 직접 받고 싶니?**" 하고 묻는다.
   - 답에 `0x4` (`DLGC_WANTALLKEYS` `[추측: 이름]`) 가 있으면 → "다 내가 받을게" → 그냥 번역 · 배달 (`+0x18e JNZ → +0x129`).
5. 없으면 wParam 을 **빼기 사슬** 로 가른다 (`+0x190 ~ +0x1ac`): `3` · `0x1b` → 취소 길 (조각 `+0x369`, IDCANCEL), `9` (탭) → 덩어리 `+0x98b6e`, **`0xd` (Enter) → `+0x1ea`**.
6. Enter 갈래 — **기본 단추 ID 고르기**. 차례가 셋이다.
   - ① `GetFocus` (`+0x1ea`) 로 포커스 컨트롤을 찾고, 그 컨트롤이 `WM_GETDLGCODE` 에 `0x10` (`+0x211`, `DLGC_DEFPUSHBUTTON` `[추측: 이름]`) 을 답하면 → **그 컨트롤 ID** (`GetDlgCtrlID`, `+0x21d`).
   - ② 아니면 `SendMessageW(hDlg, DM_GETDEFID 0x400)` (`+0x98b4c`) — 답의 윗 16비트가 `0x534b` 면 → **아랫 16비트** (`+0x98b5c JZ → +0x98b66`).
   - ③ 그것도 아니면 → **`1`** (`+0x98b61`).
   - TEB `+0x860` 이 0 이면 `GetFocus` 가 0 이라 곧장 ② 로 간다.
7. 고른 ID 로 `_FindDlgItem` (조각 `+0x36e`) — 그 ID 의 부품을 찾는다.
8. 찾은 결과에 따라:
   - 찾음 + 멀쩡 → **`+0x98b19 SendMessageW(hDlg, 0x111, ID, 컨트롤 hwnd)`** → `+0x98b1f JMP +0x13b` → TRUE.
   - 못 찾음 → `+0x98c12 JZ → +0x98b0a` → 같은 `SendMessageW(hDlg, 0x111, ID, RBX)` → TRUE. (run 문서 요약은 lParam 을 "컨트롤 hwnd 또는 0" 이라 적는다.)
   - 찾음 + 부품이 **꺼져 있음** (사본 `+0x1f` 비트 3, `+0x98c18`) → `NtUserMessageBeep(0)` (`+0x98c24`, 삑) 만 하고 TRUE. **`WM_COMMAND` 를 안 보낸다.**

정리 `[정적]`:

- Enter 는 **`WM_KEYDOWN`** 으로 오고, 분류대 (`IsDialogMessageW`) 가 **`WM_COMMAND` 로 바꿔서, 카드함을 안 거치고 바로 보낸다** (`SendMessageW`).
- 원래 키 카드는 번역 · 배달하지 않는다.

덤 둘:

- **진짜 입력에서도 `WM_COMMAND` 가 카드함으로 오는 길이 있다** `[정적]`: 앱이 `WM_CLOSE (0x10)` 에 FALSE 를 돌려주면 `DefDlgProcWorker +0x448 ~ +0x46c` 가 `PostMessageW(hDlg, 0x111, 2, 그 컨트롤)` 로 **부친다**. 그러니 "`NtUserPeekMessage` 가 `message = 0x111` 을 주는 꼴" 은 진짜 흐름에도 있다.
- 마우스 클릭은 부품 프로시저가 `WM_COMMAND` 를 **보낸다** (`xxxButtonNotifyParent` — 이름만, 본문 안 읽음 `[공백]`).

비유: 손님이 Enter 를 누르면 카드함엔 "**Enter 키 눌림**" 카드가 들어온다. 분류대 직원은 그걸 보고 "Enter 면 기본 단추지" 하며 장부에서 기본 단추 번호를 찾아, "**1번 단추 눌렸어요**" 를 사장님께 **내선으로 바로** 전한다. 원래 Enter 카드는 거기서 버린다. 기본 단추가 꺼져 있으면 "삑" 소리만 낸다.

### 2.5 ★ 장부 `+0x20` — 기본 단추 번호 칸이 곧 결과 칸 `[정적]`

2.4 의 ② `DM_GETDEFID` 는 누가 어떻게 답할까?

**새 말 — `DM_GETDEFID` (`0x400`) · `DM_SETDEFID`**: 대화상자에게 "**기본 단추 번호가 뭐야?**" / "**이걸로 바꿔**" 하고 묻는 카드. 대화상자 담당 직원 (`DefDlgProcWorker`) 이 답한다 `[정적]`.

답하는 줄 (`DefDlgProcWorker +0xa24 ~ +0xa55`):

1. 장부 `+0x18` 비트 0 (끝 도장) 이 켜져 있거나, 장부 `+0x20 == 0` 이면 → **0**.
2. 아니면 `+0xa4b MOVZX word [pdlg+0x20]` — **장부 `+0x20` 의 아랫 16비트** 를 읽어 `| 0x534b0000` 해서 돌려준다.

`DM_SETDEFID` 는 `pdlg+0x20 = wParam` 으로 쓴다 (조각 `+0x9799c`).

여기서 중요한 사실이 하나 나온다.

- 기본 단추 번호는 **장부 `+0x20`** 에 있다.
- `EndDialog` 가 결과를 쓰는 곳도 **장부 `+0x20`** 이다 (`EndDialog +0x7f`, 2.7).
- `DialogBox2` 가 돌려주는 것도 **장부 `+0x20`** 이다 (`+0x3bd`, 2.8).
- → **한 칸이 두 일을 한다.** 끝나기 전엔 "기본 단추 번호", 끝나면 "결과" `[정적]`.

이 칸에 값이 들어가는 줄을 모아 보면:

| 언제 | 누가 | 줄 |
|---|---|---|
| 창 만들 때 처음 | `1` (IDOK) | `InternalCreateDialog +0x6fa` (run146) |
| 컨트롤 고리 | `WM_GETDLGCODE` 답 `& 0x10` 인 컨트롤 ID | run146 해설 2.7 |
| 앱이 바꿀 때 | `DM_SETDEFID` 의 wParam | 조각 `+0x9799c` |
| 끝낼 때 | `EndDialog(hDlg, n)` 의 `n` (64비트) | `EndDialog +0x7f` |

- run146 은 이 칸을 "기본 단추 ID 겸 결과값" 이라 불렀지만, 그 뜻은 `[추측: 이름]` 이었다 (sast/142 11절 "겸용"). 이번에 **같은 칸을 `DM_GETDEFID` 가 읽는 줄** 이 보여서 겸용이 `[정적]` 으로 확인됐다.

비유: 장부에 "**기본 단추: 1번**" 이라고 적힌 칸이 있다. 사장님이 "끝" 할 때 **바로 그 칸을 지우고** "결과: n" 이라고 덮어 적는다. 그래서 사장님이 끝을 안 외쳐도 그 칸엔 늘 숫자 (처음엔 1) 가 남아 있다.

### 2.6 ★ (b) `WM_COMMAND` 를 곧장 주면 — 창구를 한 번도 안 거친다 `[정적]`

에뮬레이터는 사람도 키보드도 없다. 그러니 Enter 카드 대신 **"1번 단추 눌렸어요" 카드를 처음부터 넣으면** 어떻게 될까? 길을 따라가 보자.

| 걸음 | 함수 | 하는 일 | syscall |
|---|---|---|---|
| 1 | `IsDialogMessageW` | `ValidateHwndNoRip(hDlg)` · **`MessageTable[0x111] = 0`** 이라 "카드함으로 오면 거부" 비트 (`0x200`) 에 안 걸림 · `CallMsgFilterW` (TEB 훅 칸 0 이면 syscall 없이 0) · 키 카드가 아니라 기본 갈래 (`+0x129`) | 0 |
| 2 | `TranslateMessage` (`+0x12c` → ret `+0x131`) | `+0x3b XOR EAX,EAX` — 키 카드 (`0x100 0x101 0x104 0x105`) 만 syscall. `0x111` 은 그냥 0 | 0 |
| 3 | `DispatchMessageWorker` (`+0x136` → ret `+0x13b`) | `ValidateHwnd` · 사본 `+0x12` 비트 2 (`+0xb6`) 가 0 이면 `NtUserDispatchMessage` 안 탐 · **`+0x1d8 UserCallWinProcCheckWow([사본+0xe0], [사본+0x78], …)`** | 0 |
| 4 | `DefDlgProcW` → `DefDlgProcWorker` | **fnid 검사 (`+0x32 ~ +0x45`)** — 사본 `+0x2a` 가 0 또는 `0x2a4` 여야 · 장부 `[pdlg+0x0]` (앱 프로시저) · `+0xbf UserCallDlgProcCheckWow` | 0 |
| 5 | **앱 프로시저** | `WM_COMMAND` 를 받는다 | — |

→ **win32k syscall 0번** `[정적]`.

**새 말 — `MessageTable`**: user32 안에 있는 **메시지 번호별 성질표** (rva `0xb91f0`). 번호마다 비트가 있어서 "이건 카드함으로 오면 거부 (`0x200`)", "이건 글자 변환이 필요 (`0x100`)" 같은 걸 적어 둔다 `[정적: 비트 쓰임]`. 비유: 알림 카드 종류별 **취급 주의 표**. `WM_INITDIALOG` (`0x110` → `0x600`) · `WM_GETDLGCODE` (`0x87` → `0x615`) 는 `0x200` 이 켜져 있어서 **카드함으로 주면 거부된다** — 이런 건 보내기 (Send) 로만 온다 (금지 꼴 F5).

**조건 — 이게 다 맞아야 창구를 안 거친다** (run 문서 3절 표 줄임):

| 칸 | 있어야 할 값 | 어긋나면 |
|---|---|---|
| MSG `+0x0` hwnd | 모달 대화상자 hwnd (또는 그 자손) | 0 → 아무 일 없이 고리만 돈다 (F2) |
| MSG `+0x8` message | `0x111` | `& 0xfffe0000` 이나 `0x200` 비트 → 오류 · 0 |
| 사본 `+0x12` 비트 2 | 0 | 켜지면 `NtUserDispatchMessage` (`0x1031`) |
| 사본 `+0x2a` (fnid) | **0 또는 `0x2a4`** | **다른 값이면 앱 프로시저를 안 부르고 0** |
| 사본 `+0x78` | `DefDlgProcW` | — |
| `[[TEB+0x820]+0x10] \| [TEB+0x838]` | 0 | `NtUserCallMsgFilter` (`0x1013`) |
| user32 훅 전역 (`gcCallUserApiHook` 등) | 0 | 훅 갈래 |
| wParam 아랫 16비트 | `0xe5` 아님 | `0xe5` 면 IMM 훅 (`[user32+0xd96f0]`) 을 부름 `[공백: 훅 몸 안 읽음]` |

**fnid 줄이 특히 중요하다.** run146 에서 `NtUserSetDialogPointer` 가 fnid 를 `0x2a4` 로 적는다고 봤다 (종류 도장). 이번에 그 도장이 **틀리면 사장님께 카드가 아예 안 간다** 는 줄 (`DefDlgProcWorker +0x45 JNZ → +0x211`) 이 보였다. 그래서 stub 판정이 "필수" 로 세졌다 (2.12).

앱의 대답은 어떻게 되나 `[정적]`:

- `UserCallDlgProcCheckWow` 의 반환 EAX 는 (훅 없으면) 늘 0 이고, 앱의 진짜 반환은 `[rbp-0x28]` 로 따로 받는다 (`DefDlgProcWorker +0xc4` · `+0xec`).
- 앱이 TRUE 면 `[여분+0x0]` (`DWLP_MSGRESULT`) 를 돌려주고, FALSE 면 `+0x185 DefWindowProcWorker` 로 간다.
- 그런데 **`DispatchMessageWorker` 도 `IsDialogMessageW` 도 이 반환을 안 본다** (E16). 고리 입장에선 "전했다" 로 끝이다.

비유: "1번 단추 눌렸어요" 카드를 카드함에 넣으면, 직원이 꺼내 분류대에 놓는다. 분류대는 "키 카드 아니네" 하고 담당 직원에게 넘긴다. 담당 직원은 **알림판 종류 도장** 을 확인하고 사장님께 건넨다. 이 동안 **신청 창구에 한 번도 안 간다** — 모두 창 담당 건물 안 내선이다. 단, 종류 도장이 엉뚱하면 담당 직원은 사장님께 안 건넨다.

### 2.7 ★ `EndDialog` — 도장 두 개, 창구 두 번, 대답은 안 듣는다 `[정적]`

사장님 (앱 프로시저) 이 `WM_COMMAND` 를 받고 `EndDialog(hDlg, n)` 를 부른다고 하자 (E14 — 이건 앱 몫이다).

`EndDialog` (rva `0x219e0`) 가 하는 일, 차례대로:

1. `ValidateHwnd` (`+0x1f`) · `ValidateDialogPwnd` (`+0x33`) — 둘 중 하나라도 0 이면 `+0x18f` 에서 반환 0.
2. `GetActiveWindow` (`+0x40`) — 지금 활성 창은?
3. **`+0x70` 장부 `+0x18 |= 1`** — "끝" 도장.
4. **`+0x7f` 장부 `+0x20 = n`** (64비트) — 결과. (2.5 의 그 칸이다.)
5. **`+0xa9 NtUserSetWindowPos(hDlg, 0, 0, 0, 0, 0, 0x97)`** (`0x1021`).
6. **`+0xd9 NtUserPostMessage(hDlg, 0, 0, 0)`** (`0x100d`).
7. `+0xe5` 반환 1.

- 활성 창 · 포커스 · 소유자에 따라 `NtUserSetFocus` · `NtUserSetActiveWindow` · `NtUserZapActiveAndFocus` · `NtUserEnableWindow` 가 **조건부로** 더 붙는다.
- **모두 반환을 안 본다** — 호출 바로 뒤가 `NOP` 이다.

**왜 "가장 짧은 꼴은 syscall 둘뿐" 인가** — 단계별로 (run 문서 대조 8):

1. `GetActiveWindow` 는 `[[TEB+0x860]+0x28]` 을, `GetFocus` 는 `[[TEB+0x860]+0x20]` 을 읽는다 `[정적]`.
   - **새 말 — TEB `+0x860`**: 직원 안내판의 한 칸. 어떤 블록을 가리키는데, 그 블록의 `+0x20` 이 포커스, `+0x28` 이 활성 창이다. 블록 이름은 `[추측: 스레드 입력 상태 블록]`. **0 이면 "활성 없음 · 포커스 없음"** 으로 읽힌다 `[정적]`. 비유: 안내판의 "**지금 내가 보고 있는 것**" 메모 칸.
2. TEB `+0x860` = 0 → `GetActiveWindow` 0.
3. `+0x45 XOR RAX,RDI` · `+0x48 MOVZX EBP,AX` — 활성 창과 hDlg 의 **아랫 16비트만** 견준다. 0 과 hwnd 는 다르니 `EBP ≠ 0`.
4. 그래서 "활성 창이 나면" 갈래 (`+0x86`) 를 안 탄다 → `+0xa9 SetWindowPos` → (`+0xb8 JNZ → +0x106` · `+0x111 JNZ → +0xae`) → `+0xd9 PostMessage`.
5. → **syscall 둘 (`SetWindowPos` · `PostMessage`)** 만 `[정적]`.

이 두 syscall 이 커널에서 정확히 무엇을 하나:

- run142 해설은 `SetWindowPos` 를 "숨기기", `PostMessage(hDlg, 0, 0, 0)` 를 "잠든 고리 깨우기" 로 읽었다.
- 그런데 커널 쪽 몸은 이번에도 안 봤다 (win32k 안 엶). 그러니 이 뜻은 `[추측]` 이다 — `0x97` 깃발의 부작용은 **Q282 4** 로 남았다.
- 확실한 건 하나: **user32 는 둘 다 대답을 안 듣는다** `[정적]`.

비유: 사장님이 장부에 **"끝" 도장** 을 찍고, **결과 칸** (원래 "기본 단추: 1번" 이 적혀 있던 그 칸) 에 결과를 덮어 적는다. 그리고 창구에 신청서 두 장 — "알림판 위치 좀 바꿔 주세요 (`0x97`)", "카드함에 빈 카드 한 장 넣어 주세요" — 을 낸다. 대답은 안 듣고 돌아선다.

### 2.8 ★ (c) 빠지기 · 반환 — 결과를 먼저 베끼고 철거한다 `[정적]`

`EndDialog` 는 도장만 찍고 돌아온다. **고리가 바로 끝나는 게 아니다.** 사장님 → 담당 직원 → 분류대 → 고리 순으로 돌아온 뒤:

1. **E17**: 고리로 돌아와 `HMValidateHandleNoSecure` (`+0x11c`) 로 hwnd 가 아직 있나 본다. 0 이면 빠짐 (반환 0).
2. **E5 고리 맨 위** (`+0x12a ~ +0x142`): 매 바퀴 `[[사본+0x128]+8]` (장부) 가 NULL 인가, 장부 `+0x18` 비트 0 (끝 도장) 이 켜졌나를 본다. → 켜졌으니 `+0x389` 로 빠진다.
3. **E18**: (`gpsi+0x764 & 0x8000` 이면 `NotifyWinEvent(0x11)`) → `+0x39f HMValidateHandleNoSecure` (0 이면 반환 0) → **`+0x3bd RSI = [pdlg+0x20]` — 결과를 먼저 읽는다.**
4. **E19**: **`+0x3c4 NtUserDestroyWindow(hwnd)`** (`0x1095`) — **반환 안 봄** (`+0x3cb NOP`).
5. **E20**: 소유자가 있으면 `GetThreadState(0xa)` → `QueryWindow` → `SetForegroundWindow` (표본은 소유자 0 이라 안 탐).
6. **E21**: `+0x3ea MOV RAX,RSI` → `InternalDialogBox +0x8f` → `DialogBoxParamW` 반환. **64비트 그대로, 좁히기 없음.**

**왜 "먼저 읽는다" 가 중요한가** — 단계별로:

1. 근거: 결과는 장부 `+0x20` 에 있다 `[정적]`.
2. 근거: `NtUserDestroyWindow` 는 알림판을 철거한다. 장부가 그 뒤에 풀릴 수도 있다.
3. 근거: `DialogBox2` 는 **철거 전** `+0x3bd` 에서 값을 RSI 에 베껴 둔다 `[정적]`.
4. 그래서: stub 이 `NtUserDestroyWindow` 에서 장부나 사물함을 풀거나 다시 써도 **반환값은 안전하다** (금지 꼴 F10 — "괜찮음").
   - `WM_NCDESTROY` 를 안 보내면 장부가 안 풀릴 뿐이다 (누수, 무해) `[정적]`.

**`DialogBoxParamW` 가 돌려주는 값 정리** `[정적]`:

| 상황 | 반환 |
|---|---|
| 앱이 `EndDialog(hDlg, n)` | **`n`** (64비트) |
| `EndDialog` 없이 `WM_QUIT` 으로 빠짐 | 장부 `+0x20` 그대로 = **처음 값 1** (또는 기본 단추 ID) — **0 아님** (2.13 정정) |
| 고리 중 hwnd 가 사라짐 (E17 · E18) | **0** — 0 이 되는 건 이것뿐 |
| 처음 `ValidateHwnd` 실패 (E2) | **−1** |

**run146 고쳐 읽기와 이어진다**: run146 은 `NtUserDestroyWindow` 를 "컨트롤 만들기 실패 때만 불림" 으로 적었다. 이번에 **모달 끝 `+0x3c4` 에서 늘** 불린다는 게 보였다. 반환을 안 보니 "기본 대답 0 이면 됨" 은 그대로다 (2.13).

비유: 직원이 졸다 깨서 (또는 다음 바퀴에) 장부를 펴 보니 "끝" 도장이 있다. 직원은 **결과 칸을 먼저 손바닥에 베껴 적고**, 그다음 창구에 "알림판 철거해 주세요" 를 낸다 (대답은 안 들음). 그리고 베낀 숫자를 들고 사장님 (`DialogBoxParamW` 를 부른 쪽) 께 돌아간다. 철거하면서 장부가 버려져도, 숫자는 이미 손바닥에 있다.

### 2.9 ★ 금지 꼴 — 끝없는 바쁜 고리 `[정적]`

이번 바퀴에서 에뮬레이터에 가장 큰 경고다.

**새 말 — 바쁜 고리 (busy loop)**: 할 일이 없는데도 **쉬지 않고 같은 확인만 되풀이하는 고리**. 진짜로 잠들지 않으니 CPU 만 계속 쓴다. 에뮬레이터라면 **명령 수 한도까지** 이 고리만 돈다 (run 문서 PE_emulator 표). 비유: 졸기 신청을 냈는데 **0 초 만에 깨워 주니**, 카드함 뚜껑만 쉼 없이 여닫는 직원.

**새 말 — 금지 꼴**: run 문서 5절이 쓰는 말. stub 이 이렇게 대답하면 **고리를 못 빠지거나 접근 위반이 나는 꼴** 이다 (F1 ~ F11).

**먼저 — 빠지는 길은 넷뿐이다** `[정적]`:

1. 장부 `+0x18` 비트 0 — 앱이 `EndDialog` 를 불렀을 때 (E5).
2. 장부 (`pdlg`) 가 NULL (E5).
3. hwnd 가 사라짐 — `HMValidateHandleNoSecure` 0 · 사본 `+0x2a & 0xc000` (E3 · E11 · E17).
4. `WM_QUIT` (`0x12`) — `+0x280` / `+0x283` (E8).

**그 밖에 횟수 · 시간 한도는 없다** `[정적]`. TEB `+0x860` ≠ 0 일 때 100 번마다 `NtYieldExecution` 을 부르는 것뿐이다 (`+0x1e9`) — 이건 끝내는 게 아니라 잠깐 양보하는 것이다.

**F1 — 지금 에뮬레이터 기본 대답** — 단계별로:

1. 근거: `NtUserPeekMessage` 몸이 없어서 **기본 대답 0** 이다 (run 문서 PE_emulator 쪽 — flow-writer 가 파일 목록만 훑음).
2. 근거: 고리는 0 이면 `+0x272 JZ → +0x20a` 빈 틈으로 간다 `[정적]`.
3. 근거: 보이고 소유자 0 이면 `+0x23f NtUserWaitMessage` — 이것도 몸이 없어 **즉시 0** 이다. 그리고 user32 는 반환을 안 본다 `[정적]`.
4. 근거: `+0x24b JMP +0x119` → 맨 위. 끝 도장도 없고, 장부도 있고, hwnd 도 살아 있고, `WM_QUIT` 도 안 왔다 → 빠지는 길 넷 다 아님.
5. 그래서: 1 번으로 돌아가 **영원히 반복** — 바쁜 고리 `[정적]`.
6. 그리고: run146 stub 이 채워져 `InternalCreateDialog` 가 hwnd 를 돌려주는 **바로 그 순간** 이 고리에 들어간다. 지금 벽 (창 만들기) 을 넘으면 바로 다음 벽이 이것이다.

**다른 금지 꼴 (대표만, 전체는 run 문서 5절)**:

| # | 꼴 | 무슨 일이 | 권하는 처리 (결과서) |
|---|---|---|---|
| F2 | 1 을 주는데 MSG 를 안 채움 (hwnd 0 · message 0) | 분류대 0 → 번역 0 → 배달 0 → 맨 위. 역시 고리만 | MSG 채우기 |
| F3 | hwnd 가 `ValidateHwnd` 에 0 | 배달 0. `HMValidateHandleNoSecure` 도 0 이면 **뜻밖에 빠짐 (반환 0)** | run146 칸 맞추기 |
| F5 | `WM_INITDIALOG` · `WM_GETDLGCODE` 같은 `0x200` 카드를 카드함으로 줌 | 거부 (`SetLastError(0x487)`) → F2 | 이런 건 보내기로만 |
| **F7** | 앱이 `EndDialog` 를 안 부름 (입력 검사 거절 · 다른 ID 만 처리) + stub 이 같은 카드를 계속 줌 | 매 바퀴 1 → 무시 → 맨 위 | 횟수 한도 + 카드 바꾸기 `[추측: 정책]`: 기본 ID → `IDCANCEL (2)` → `WM_CLOSE` → `WM_QUIT` |
| F8 | 기본 단추가 꺼진 부품인데 Enter 길로 줌 | 삑 (`NtUserMessageBeep`) 만 → F7 | `WM_COMMAND` 를 곧장 줌 |
| F9 | 장부 NULL | `DialogBox2 +0x83 / +0x87` **접근 위반** (고리 안 NULL 검사는 그 뒤에만) | run146 stub 5 (`NtUserSetDialogPointer`) |

비유: 직원이 "카드 있어요?" 하면 대리 직원이 늘 "없어요" 한다. 졸기 신청을 내면 대리 직원이 0 초 만에 깨운다. 직원은 장부를 펴 보고 (도장 없음), 알림판도 그대로고, "가게 문 닫아요" 카드도 없으니 다시 "카드 있어요?" 한다. **영원히.** run142 해설 2.9 의 "아무것도 안 넣으면 직원은 알림판 앞에서 계속 기다린다" 가 이번에 줄 근거로 확인됐고, 그 기다림이 **잠이 아니라 헛바퀴** 라는 것까지 보였다.

### 2.10 (d) A 판 · 모달 아닌 판 — 끝 길은 같다 `[정적]`

**새 말 — A 판 / W 판**: 같은 API 의 두 판. A 판은 글자를 **ANSI (한 바이트 계열)** 로, W 판은 **유니코드 (두 바이트)** 로 받는다 `[추측: 일반적으로 알려진 설명]`. 이번 코드에서 둘이 갈리는 건 "깃발 `2`" 하나다 `[정적]`.

| 자리 | `DialogBox*W` | `DialogBoxIndirectParamA` | `CreateDialogParam*` |
|---|---|---|---|
| 깃발 넘김 | `DialogBoxParamW +0x5d` → 0 | `+0x9 MOV [RSP+0x28],2` | W 0 · A 2 |
| 다음 | `InternalDialogBox` → `InternalCreateDialog` → **`DialogBox2` (고리)** | 같음 | **`CreateDialogIndirectParamAorW +0x52 InternalCreateDialog` 에서 끝 (고리 없음)** |
| 자원 없을 때 | −1 | −1 | **0** |
| `InternalCreateDialog` 안 | — | `+0x5da AND R14D,2` → 확장 스타일 `\| 0x80000000` · 장부 `+0x18 \|= 4` (`+0x72c`) | 같은 규칙 |

- **글자 변환은 대부분 안 일어난다**: `SendMessageWorker +0x14c ~ +0x16f` 에서 창과 보내는 쪽의 글자 종류가 달라도, `WM_SETFONT` · `WM_GETDLGCODE` · `WM_INITDIALOG` · `WM_COMMAND` · `DM_*` 는 `MessageTable & 0x100` 이 꺼져 있어 **변환 · syscall 없이 지나간다** (`+0x21d JZ → +0x29c`) `[정적]`.
- 변환이 도는 건 글자 메시지 (`0x102 0x103 …`) 와 `0xb0 0xb1` 뿐이다.
- 사본 `+0x12` 비트 3 (ANSI) 을 켜는 쪽은 **커널** 로 보이지만, win32kfull `xxxCreateWindowEx` 는 눈으로만 훑어서 `[추측]` (Q282 1).
- **결론**: A 판도 끝 길 (E9 ~ E21) 은 W 판과 같다. `CreateDialogParam*` 은 모달이 아니라서 고리 자체가 없다.

비유: 한글 알림판 (W) 과 영문 알림판 (A) 은 만들 때 서류 칸 몇 개만 다르다. "단추 눌렸어요" 카드는 글자가 없는 카드라서 번역할 게 없다. 그리고 `CreateDialog*` 는 알림판만 세워 놓고 **직원이 그 앞에 서서 기다리지 않는** 판이다.

### 2.11 앱 프로시저가 거의 늘 부르는 것 — 사물함 셋째 칸이 함정 `[정적]`

사장님 (앱 프로시저) 은 카드를 받으면 보통 부품을 찾고, 자기 메모를 꺼내 본다. 이 부름들이 창구를 거치는지 봤다.

| API | 하는 일 | syscall |
|---|---|---|
| `GetDlgItem(hDlg, id)` | 사본 `+0x38` (첫 자식) → `+0x48` (다음 형제) 사슬을 따라 `+0x140` (ID) 가 같은 걸 찾는다. 못 찾으면 `SetLastError(0x58d)` · 0 | **0** |
| `GetDlgCtrlID` | `[사본+0x140]` | 0 |
| `GetParentDialog` | 사본 `+0x12` 비트 0 인 창을 만날 때까지 `+0x30` (부모) 사슬을 올라감 | 0 |
| `GetWindowLongPtrW(hDlg, 0x10)` | **`[[사본+0x128]+0x10]`** 을 사용자 모드로 읽음 (항목 `+0x8` == TEB `+0x78` 일 때) | 0 |
| **`SetWindowLongPtrW(hDlg, 0x10, v)`** | `g_systemCallFilterId ≠ 5` 면 **syscall** (`_SetWindowLongPtr +0x6a / +0x71 JNZ → +0x261`, call `+0x26c`), 반환 = 옛 값 | **`NtUserSetWindowLongPtr` (`0x15da`)** |
| `SetWindowLongPtrW(hDlg, 8, p)` (`DWLP_DLGPROC`) | 사용자 모드로 장부 `+0x0 = p` | 0 |
| `WM_GETDLGCODE` 가 앱에서 0 | `DefWindowProcWorker` → `RealDefWindowProcWorker` — 커널이 채우는 비트표 (`user32+0xda568 ~ +0xda580`) 에 따라 | 0 또는 `NtUserMessageCall` 1번 |
| `DlgSetFocus` | `SendMessageW(0x87)` → … → `NtUserSetFocus` 꼬리호출 | 1번 |

**함정 하나 — 단계별로**:

1. 근거: run146 에서 사물함 (여분 블록) 칸은 `+0x0` (`DWLP_MSGRESULT`) · `+0x8` (장부) · `+0x10` (`DWLP_USER`) 로 보였다 (이름은 `[추측]`).
2. 근거: **읽기** (`GetWindowLongPtrW(hDlg, 0x10)`) 는 사물함 `+0x10` 을 **직접** 읽는다 `[정적]`.
3. 근거: **쓰기** (`SetWindowLongPtrW(hDlg, 0x10, v)`) 는 (`g_systemCallFilterId ≠ 5` 면) **창구 (syscall `0x15da`)** 로 간다 `[정적]`.
4. 그래서: stub 이 `NtUserSetWindowLongPtr` 에서 **같은 칸 `[[사본+0x128]+0x10]` 에 써 줘야** 나중에 읽을 때 그 값이 나온다.
5. 지금 stub 은 몸이 없어 0 만 준다 → 앱이 `WM_INITDIALOG` 에서 넣은 포인터를 `WM_COMMAND` 때 NULL 로 읽을 것이다 `[추측: 앱 쪽 일반 꼴]`.

- `g_systemCallFilterId` (user32 전역 `0xd9970`) 가 5 인 때가 언제인지, 누가 정하는지는 이번에 안 따라갔다 `[공백]`. 결과서는 "보통 5 가 아님" 이라 적었지만, 그 "보통" 에는 줄 근거가 없다 `[추측]` (Q282 9).

비유: 사물함 **셋째 칸** 은 사장님 개인 메모 칸이다. 꺼내 볼 때는 가게 쪽에서 직접 열어 보는데, **넣을 때는 창구에 맡겨야** 한다. 그러니 창구의 대리 직원이 맡은 메모를 **정말로 그 칸에 넣어 주지 않으면**, 사장님이 나중에 열어 볼 때 빈 칸이다.

### 2.12 ★ PE_emulator 판정 — 그리고 "가장 짧게 누르기" (run 문서 "PE_emulator 쪽")

flow-writer 는 `../PE_emulator-BD/ext/syscall/syscall_layer_cpp/src/gui/` 파일 목록 (19개) 과 `nt_user_get_message.cpp` (26줄) 를 **줄 번호만** 훑었다 (읽기만). `peek_message` · `wait_message` · `destroy_window` · `set_dialog_pointer` · `set_window_pos` · `post_message` · `show_window` · `set_window_long_ptr` 파일은 **없다 → 몸 없는 기본 대답 0**. 고칠지는 사용자 · PE_emulator 결정이다.

| 지금 에뮬레이터 | 판정 | 이 글 |
|---|---|---|
| `NtUserPeekMessage` · `NtUserWaitMessage` 몸 없음 → 0 | **틀림 — 금지 꼴 F1, 끝없는 바쁜 고리** | 2.9 |
| `nt_user_get_message.cpp:6 ~ :7` 주석 "모달 고리의 잠과 같다고 봄 — 근거 없음" | **주석 근거 고칠 거리** — `DialogBox2` 는 `NtUserGetMessage` 를 안 부른다. 이 파일은 앱 자기 고리용으로만 맞다 | 2.3 |
| 같은 파일 `:5` "기본 대답 0 이 가짜 `WM_QUIT` 로 창 고리를 끝냄" | **맞음** — `GetMessageW` 는 반환을 그대로 준다 | 2.3 |
| 같은 파일 `:22` `window != 0x10010` 이면 `stop` | **같이 바뀌어야 함** — run146 이 "hwnd 마다 다른 값" 을 권함 | — |
| `NtUserGetMessage` 가 −1 을 안 줌 | **맞음** — −1 은 앱이 `while (GetMessage())` 면 영원히 돈다. 앞으로도 주지 말 것 | — |
| `NtUserDestroyWindow` 몸 없음 → 0 | **기본 대답 0 은 맞음, "실패 때만 불림" 은 틀림** — 모달 끝에서 늘 불림 | 2.8 |
| `NtUserSetDialogPointer` 몸 없음 | **fnid `0x2a4` 를 적는 게 필수** (run146 판정 강화) | 2.6 |
| `NtUserSetWindowLongPtr` 몸 없음 → 0 | **틀림** — 같은 사물함 칸에 써야 함 | 2.11 |
| `SetWindowPos` · `PostMessage` · `ShowWindow` · `SetFocus` 등 몸 없음 → 0 | **맞음** — 반환 안 봄 | 2.7 |

**"기본 단추를 누른 것으로 치기" — 가장 짧은 곳** `[정적: 줄을 이은 권고]`:

**`NtUserPeekMessage` (`0x1001`) 의 출력 한 번.** 반환 `1`, 카드 (`*pmsg`, 0x30 B, 먼저 0 채움):

| MSG 칸 | 값 |
|---|---|
| `+0x00` hwnd | 그 모달 대화상자 hwnd |
| `+0x08` message | `0x111` (`WM_COMMAND`) |
| `+0x10` wParam | `LOWORD(pdlg+0x20)` — **0 이면 1** (2.4 ③ 과 같은 규칙) |
| `+0x18` lParam | 그 ID 의 컨트롤 hwnd (사본 `+0x38` → `+0x48` 사슬에서 `+0x140 == ID`), 없으면 0 |
| 나머지 | 0 |

- 왜 Enter 카드 (`WM_KEYDOWN`) 가 아니라 `WM_COMMAND` 인가: Enter 길은 `WM_GETDLGCODE` · `DM_GETDEFID` · `_FindDlgItem` 을 더 거치고, 기본 단추가 꺼져 있으면 삑만 하고 끝난다 (F8). `WM_COMMAND` 를 곧장 주면 분류대 기본 갈래로 바로 간다 (2.6).
- 그러면 그 대화상자 한 번의 syscall 은 **`NtUserPeekMessage` · `NtUserSetWindowPos` · `NtUserPostMessage` · `NtUserDestroyWindow` 넷** `[정적]`.
- 조건 둘: `NtUserSetDialogPointer` 가 fnid `0x2a4` 를 적어 둬야 하고 (또는 0 그대로), 앱이 `DWLP_USER` 를 쓰면 `NtUserSetWindowLongPtr` 가 같은 칸에 써야 한다.

**조심** (run 문서 — `[정적 두 줄을 이은 것]`):

- stub 이 부품 프로시저를 안 부르면 장부 `+0x20` 은 처음 값 `1` 그대로다.
- 메시지 상자 `MB_DlgProc` 는 ID 1 · 2 를 **그 단추가 있을 때만** 받는다 (run142). `MB_YESNO` 처럼 `IDOK` 단추가 없는 상자면 `WM_COMMAND(1)` 이 무시되어 **F7** 로 간다. 메시지 상자의 기본 단추 스타일 · 순서는 **Q282 7**.

**stub 이 채워야 하는 최소 목록 — run146 1 ~ 6 에 이어서 7 ~ 15** (줄임, 전체는 run 문서):

7. **`NtUserPeekMessage`** — 첫 부름에 위 카드. 다시 불리면 (F7) 정책대로 `IDCANCEL` → `WM_CLOSE` → `WM_QUIT` `[추측: 정책]`, 그래도 안 끝나면 진단 정지.
8. **hwnd 마다** — 부품 사본: `+0x1f` 비트 6 켬 · `+0x30` = 대화상자 사본 · `+0x48` = 다음 부품 · `+0x140` = ID. 대화상자 사본: `+0x38` = 첫 부품 · `+0x2a` = `0x2a4` · `+0x78` = `DefDlgProcW` 등.
9. **TEB `+0x860` = 0 허용 (권장)** — `EndDialog` 가 syscall 둘만 부르고, Enter 길은 `DM_GETDEFID` 로.
10. **`SetWindowPos` · `PostMessage` · `SetFocus` · …** — 반환 안 봄 → 0. 단 앱이 자기에게 `PostMessage` 하는 꼴을 살리려면 카드함이 있어야 한다.
11. **`NtUserDestroyWindow`** — 반환 안 봄. 장부는 이미 읽힘.
12. **`NtUserWaitMessage`** — 이 길에서 불리면 (= Peek 가 0) **진단 정지** 권장.
13. **`NtUserMessageCall`** — 0 으로 충분. `NtUserDispatchMessage` · `NtUserTranslateMessage` · `NtUserCallMsgFilter` 는 위 조건이면 **안 불림** (불리면 조건이 어긋난 것).
14. **`NtUserSetWindowLongPtr`** — `[[사본+0x128] + 인덱스] = 값`, 반환 = 옛 값.
15. **되부름 · 카드함** — 이 끝 길엔 되부름이 필요 없다. 카드함이 필요한 건 앱이 자기에게 `PostMessage` 하는 경우 (F7) 뿐.

**되부름이 꼭 필요한가 — 아니오** `[정적]`. 앱 프로시저까지 가는 길 (`DispatchMessageWorker` → `UserCallWinProcCheckWow` → `DefDlgProcW` → `UserCallDlgProcCheckWow`) 은 **전부 user32 안** (CFG 간접 호출) 이다. `NtUserDestroyWindow` 가 커널에서 `WM_DESTROY` 를 되부르는 쪽은 `[추측]` 이고, 모달을 빠지는 데는 필요 없다.

비유: 대리 직원이 할 일은 결국 이렇다. 직원이 처음 "카드 있어요?" 하면 **"1번 단추 눌렸어요" 카드 한 장** 을 내준다. 알림판 종류 도장을 제대로 찍어 두고, 사장님 메모는 맡으면 진짜로 사물함에 넣는다. 나머지 신청서는 "네" 하고 받기만 하면 된다. 그리고 **직원이 졸기 신청을 내면 — 그건 뭔가 잘못된 것이니 멈춰 세운다.**

### 2.13 앞 run 정정 · 고쳐 읽기

이번엔 **"정정" 하나 (틀린 것)** 와 **"고쳐 읽기" 하나 (빠진 것)** 가 있다. run 문서가 각 문서 맨 위에 한 줄씩 넣었고, 해설서에도 맨 위에 한 줄씩 넣었다.

| 앞 run | 그때 | 이번에 |
|---|---|---|
| **run142 (정정)** | 4절 "`WM_QUIT` 이면 `EndDialog` 없이 → 장부 `+0x20` 이 처음 값 **0** 이라 `MessageBoxW` 0" | **틀렸다** `[정적]` — 처음 값은 **1** (`InternalCreateDialog +0x6fa MOV qword [pdlg+0x20],1`, 기본 단추 컨트롤 ID 로 바뀔 수 있음). `DialogBox2` 는 `WM_QUIT` 에서도 `+0x3bd` 로 그 값을 돌려주고, `SoftModalMessageBox` 는 −1 만 0 으로 바꾼다 (`+0x5b6 ~ +0x5be`). → `DialogBox2` · `SoftModalMessageBox` 반환은 **0 이 아니다**. `MessageBoxWorker` · `MessageBoxTimeoutW` 가 그 뒤에 값을 바꾸는지는 `[공백]` |
| run142 (닫힘) | 4절 모달 고리 · 최소 길 26 ~ 28 · PE_emulator 8 ("`WM_COMMAND(IDOK)` / `WM_QUIT` 중 무엇을") | 줄 근거로 닫힘. 둘 다 반환은 장부 `+0x20`, `WM_COMMAND(LOWORD(pdlg+0x20))` 한 번이 가장 짧음, `WM_QUIT` 은 앱 메인 고리까지 끝냄 (`NtUserPostQuitMessage` 로 다시 걸어 둠). **정책은 그대로 사용자 몫** |
| **run146 (고쳐 읽기)** | PE_emulator 표 · 최소 목록 6 — `NtUserDestroyWindow` 는 만들기 길 (컨트롤 실패, `+0xc01`) 만 | **모달 끝 `DialogBox2 +0x3c4` 에서도 늘 불린다.** 반환 안 봄 · 기본 대답 0 이면 됨은 그대로 |
| run146 (닫힘) | 최소 목록 7 "(sast/146 에서 보탬)" 자리 · "아직 확인 필요" 정적 1 ~ 4 | 이번 7 ~ 15 로 채움. `NtUserSetDialogPointer` 의 fnid `0x2a4` 는 **"필수"** 로 강화 |

**run142 정정을 단계별로**:

1. run142 는 "`EndDialog` 가 안 불렸으니 결과 칸은 처음 값, 처음 값은 0" 이라고 봤다.
2. 그런데 run146 에서 `InternalCreateDialog +0x6fa` 가 그 칸에 **1** 을 넣는 줄이 이미 보였다 `[정적]`.
3. 이번에 `DialogBox2 +0x37d → +0x389 → +0x3bd → +0x3ea` 로, `WM_QUIT` 갈래도 **같은 `+0x3bd` 읽기를 지난다** 는 게 확인됐다 (run 문서 대조 7) `[정적]`.
4. 그래서 `WM_QUIT` 의 반환은 0 이 아니라 **1 (또는 기본 단추 ID)** 이다.
5. 다만 `MessageBoxW` 끝까지는 아직 아니다 — 그 사이 두 함수 (`MessageBoxWorker` · `MessageBoxTimeoutW`) 는 안 봤다 `[공백]` (Q282 9).

비유: run142 때는 "사장님이 끝을 안 외쳤으니 결과 칸은 비어 있겠지" 했다. 그런데 그 칸은 처음부터 "**기본 단추: 1번**" 이 적혀 있는 칸이었다 (2.5). 그러니 가게 문 닫기 (`WM_QUIT`) 로 끝나도 손바닥엔 1 이 베껴진다.

### 2.14 flow-writer 대조 — 결과서와 다르게 적은 곳 (짧게)

run 문서는 결과서 오프셋을 `.asm` 과 하나하나 맞췄다. **어긋난 곳 여섯** 은 run 문서 값을 쓴다 (이 글도 그 값을 썼다). 뜻은 모두 같고, 대부분 "**갈라지는 줄을 바로 앞 `TEST` / `CMP` 줄로 적은 것**" 이다.

1. `DialogBox2` 의 `ValidateHwnd` 뒤 점프는 `+0x56` (`TEST`) 이 아니라 **`+0x59 JZ`** (→ `+0x9963d`, −1). hwnd 가 NULL 일 때도 `+0x45 TEST · +0x48 JZ` 로 같은 곳에 간다.
2. F1 의 점프는 `+0x270` (`TEST`) 이 아니라 **`+0x272 JZ`** (→ `+0x20a`).
3. F2 의 점프는 `IsDialogMessageW +0x9f` (`CMP`) 가 아니라 **`+0xa2 JZ`** (→ `+0x142`).
4. `WM_COMMAND` 를 보낸 뒤 JMP 는 `+0x98c31` 이 아니라 **`+0x98b1f`** (→ `+0x13b`). `+0x98c31` 은 삑 갈래 끝의 JMP 다 — 덩어리와 조각의 끝 JMP 를 헷갈린 꼴.
5. `DefWindowProcWorker` call 은 `+0x173` (인자 채우기 시작) 이 아니라 **`+0x185`** (ret `+0x18a`).
6. `InternalDialogBox` 의 `AND ESI,8` 은 `+0x115` (`MOVZX`) 가 아니라 **`+0x119`**.

보탬 둘: (7) run142 `WM_QUIT` 반환 정정의 근거 (2.13), (8) `EndDialog` 가 활성 창을 아랫 16비트만 견주어 "syscall 둘만" 이 맞다는 것 (2.7).

### 2.15 정리 — 비유로 한 번 더

- 알림판 (대화상자) 이 서면, 직원은 그 앞에 서서 **카드함** 을 본다. "카드 있어요?" (`NtUserPeekMessage`) — 대답은 "있다 / 없다" 만 듣는다.
- 사람이 Enter 를 치면 카드함엔 "Enter 키" 카드가 온다. **분류대** (`IsDialogMessageW`) 가 장부의 "기본 단추: 1번" 칸을 보고, "1번 단추 눌렸어요" (`WM_COMMAND`) 를 사장님께 **내선으로 바로** 전한다.
- "1번 단추 눌렸어요" 카드를 처음부터 넣어도 된다. 그러면 분류대 → 담당 직원 → 사장님까지 **창구를 한 번도 안 간다.** 단, 알림판 **종류 도장** (fnid `0x2a4`) 이 맞아야 한다.
- 사장님은 장부에 **"끝" 도장** 을 찍고, **"기본 단추" 칸을 결과로 덮어 쓴다.** 창구에 신청서 두 장을 내고 대답은 안 듣는다.
- 직원은 다음 바퀴 맨 위에서 도장을 보고, **결과를 손바닥에 베낀 뒤** 알림판 철거를 신청한다 (대답 안 들음). 베낀 숫자가 `DialogBoxParamW` 의 반환이다.
- 지금 대리 직원은 "카드 있어요?" 에 늘 "없어요", 졸기 신청엔 0 초 만에 깨운다. 그래서 직원은 **영원히 헛바퀴** 를 돈다. 고치는 가장 짧은 길은 **첫 "카드 있어요?" 에 "1번 단추 눌렸어요" 카드 한 장** 을 주는 것이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show e7f7278 -- runs/03_sprint/flow.md`)

**그림은 그대로다.** 이번 길은 test.exe 초기화 밖이기 때문이다. 바뀐 곳은 글뿐이다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (user32 줄 · 근거 줄) | 앞 run 까지 | run148 이 읽은 함수 보탬. 근거 줄에 "(run148) 정적 바퀴 … 사진 값 대조 없음 — test.exe 는 user32 를 안 올린다 · 요청 쪽 값은 에뮬레이터 로그라 `[관찰]` 로 안 씀" |
| 3장 창 API 하위 절 (run142) 나무 | `WM_QUIT (+0x280) → EndDialog 없이 → 0` | `… → pdlg+0x20 (처음 1 — run148 정정, 0 아님)` |
| 3장 창 API 하위 절 (run142) | — | 한 줄 보탬 — "(run148) **모달 고리 끝** `[정적]` … **정정**: `WM_QUIT` 의 반환은 0 이 아니라 `pdlg+0x20`" |
| 3장 대화상자 창 하위 절 (run146) | "남은 것 → … 정적 sast/146 dialog-end" | 두 줄 — "(run148) **이 hwnd 로 모달 고리를 빠지는 길**: fnid `0x2a4` 필수 · `NtUserDestroyWindow` 는 모달 끝에서 늘" · "sast/146 dialog-end → run148" |
| 3장 새 하위 절 (run146 하위 절 다음) | 없음 | **"모달 대화상자가 메시지 하나로 끝까지"** — 비유 · 나무 한 장 + `[정적]` 다섯 줄 (Enter → `WM_COMMAND` · syscall 0 조건 · 빠지는 길 넷 · 반환 `pdlg+0x20` · A 판 / `SetWindowLongPtrW`) |
| 4장 run142 줄 | 정적 N1 · N2 "sast/146 dialog-end" · PE_emulator 8 열림 | "→ **run148**" · PE_emulator 8 에 "→ run148: 둘 다 반환 = `pdlg+0x20` … 정책은 그대로 사용자 몫" |
| 4장 Q274a 줄 | "일부 해결 — 물음 1 · 2 → run146" | **"해결됨 — 물음 1 · 2 → run146 · 물음 3 → run148"** |
| 4장 run146 줄 (정적 · PE_emulator) | 정적 1 ~ 4 "진행 중" | "**1 ~ 4 (해결됨) → run148**, 5 와 `[공백]` 은 그대로 · 일부 Q282". PE_emulator 줄에 `DestroyWindow` 고쳐 읽기 · fnid 필수 · "7 은 run148 의 7 ~ 15 로 채움" |
| 4장 run147 줄 | — | 대조 한 줄 보탬 (`GetDeviceCaps` `AND` 는 `+0x59` 가 아니라 `+0x5c` — run148 때 explainer 가 찾은 것) |
| 4장 새 줄 | — | run148 줄 넷 — 해결됨 · 동적 (Q273) · 정적 (Q282) · PE_emulator |
| 5장 | — | run148 두 줄 |

- 짐 지도 (`baggage.md`) 는 안 바뀐다 — **새 짐 칸 없음** (이번 길은 초기화 밖).

태그 변화만 뽑으면:

- 장부 `+0x20` = 기본 단추 ID **겸** 결과값 — 뜻 `[추측: 이름]` → **`[정적]`** (`DM_GETDEFID` 가 같은 칸을 읽는 줄)
- run142 "`WM_QUIT` → 반환 0" — **틀림 → 정정** (`pdlg+0x20`, 처음 1) `[정적]`. `MessageBoxW` 끝까지는 `[공백]`
- run142 · run146 의 고리 · `EndDialog` 뒤 syscall · `GetDlgItem` · A 판 `[공백]` → **`[정적]`**
- `NtUserDestroyWindow` 부르는 곳 — "컨트롤 실패 때만" → **"모달 끝에서도 늘"** `[정적]`
- 칸 이름 (`tagWND` · `tagDIALOG` · `tagMSG` 류) — 여전히 **`[추측]`** (PDB 에 형식 없음)

### 큰 그림에서 어떻게 이어지나

- 창 API 쪽 옆길을 시간 순서로 늘어놓으면 이렇게 이어진다:
  - run85 · run86 — user32 · gdi32 `DllMain` (프로그램이 user32 를 올릴 때)
  - run131 — 창 클래스 등록 (COM STA)
  - run142 — 창 API 넷의 **문지기**, 모달 고리의 틀
  - run144 · run147 — GDI 공유 핸들 표 · 화면 DC
  - run146 — 문지기 뒤 대화상자 창 만들기 → hwnd
  - **run148 — 모달 고리 → 기본 단추 → `EndDialog` → 반환** ← 이번
- 이것으로 **`DialogBoxParamW` 한 번이 처음부터 끝까지** 줄 근거로 이어졌다 — 문지기 → 창 만들기 → 고리 → 끝 → 반환. **Q274a 끝.**
- 에뮬레이터 쪽에서 보면 길이 둘로 정리된다:
  - **지금 벽**: 창 만들기 (run146 목록 1 ~ 6).
  - **바로 다음 벽**: 바쁜 고리 (이번 F1). 그 벽을 넘는 가장 짧은 열쇠는 `NtUserPeekMessage` 의 첫 출력 한 장이다.
- 남은 건 대부분 **진짜 값** (동적 Q273) 과 **커널 쪽 몸** (정적 Q282) 이다. user32 가 무엇을 읽고 검사하는지는 이번으로 거의 다 나왔다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q273** (동적, VM GUI 세션 — 미룸)

run148 동적 1 ~ 12 는 같은 VM GUI 세션이라 **Q273 에 출처로 보탰다**. test.exe 가 아니라 `DialogBoxParamW` 를 부르는 GUI 프로세스가 필요하다 (user32 유저 멈춤점). 대표만:

| 무엇을 | 왜 이번에 못 알았나 |
|---|---|
| `DialogBox2+0x270` 에서 진짜 첫 카드들의 차례 (`WM_PAINT` · `WM_NCACTIVATE` …?) · 몇 번째에 `0x111` / `0x100` 이 오나 | 카드를 만드는 건 커널이다. 정적으로는 고리가 카드를 **어떻게 쓰는지** 만 봤다 |
| Enter 한 번 — `IsDialogMessageW+0x189` (`WM_GETDLGCODE` 답) · `+0x98b51` (`DM_GETDEFID` 답이 `0x534b0001` 인가) · `+0x98b1e` | 값은 실행해야 보인다 |
| `EndDialog+0x70` 장부 · `+0xb0` · `+0xe0` · `DialogBox2+0x3bd` 의 `rsi` — 진짜 `EndDialog` syscall 이 둘뿐인가 | TEB `+0x860` 이 진짜로는 0 이 아닐 수 있다 |
| TEB `+0x860` 진짜 값 (`+0x20` 포커스 · `+0x28` 활성) | 블록 모양부터 `[추측]` |
| 자식 연결 (`+0x30 +0x38 +0x40 +0x48`) 이 템플릿 차례인가 | 연결하는 쪽은 커널 (Q282 1) |
| 비트표 `user32+0xda568 ~` 진짜 값 | 커널이 채운다 `[공백]` |

### 정적으로 남은 것 → **Q282**

결과서 6절 "win32k 몫 남음" 1 ~ 7 · 8절 "다음 요청 후보" + flow-writer 보탬. 이번엔 win32k 를 안 열어서 커널 쪽 몸이 다 여기로 갔다.

- **급함** 1 — win32kfull `xxxCreateWindowEx`: 자식 연결 순서 (`LinkWindow`) · 확장 스타일 `0x80000000` → 사본 `+0x12` 비트 3 (A 판).
- **급함** 2 — `NtUserPeekMessage` · `xxxPeekMessage`: 진짜 첫 카드들, stub 의 "맨 처음 한 번 `WM_COMMAND`" 가 진짜와 얼마나 다른지.
- 3 — `NtUserDestroyWindow` 가 `WM_DESTROY` / `WM_NCDESTROY` 를 되부르는지 · 항목 해제.
- 4 — `NtUserSetWindowPos(…, 0x97)` 의 부작용 (숨기기 · 활성 변경 — 2.7 의 `[추측]`).
- 5 · 6 — 비트표를 채우는 곳 · `NtUserMessageCall` · `NtUserSetWindowLongPtr` 의 커널 쪽.
- 7 — **`SoftModalMessageBox` · `MB_AddPushButtons`** — 메시지 상자의 기본 단추 (`MB_YESNO` 에서 장부 `+0x20` 이 맞는지), `MessageBox` 가 같은 고리를 타는지. `ButtonWndProcWorker` · `xxxButtonNotifyParent` 본문.
- 9 — `g_systemCallFilterId` 를 누가 언제 정하나, 그리고 `MessageBoxWorker` · `MessageBoxTimeoutW` 가 반환을 바꾸는지 (run142 정정의 남은 몫).

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- `[공백]` IMM 훅 (`[user32+0xd96f0]`) 몸 — wParam 이 `0xe5` 일 때만 탄다.
- `[공백]` `RealDefWindowProcWorker` 가 비트표 길로 커널에 보낼 때 (`gapfnScSendMessage[0]`) 의 일.
- `[추측]` 칸 이름 전부 (`DLGC_*` · `WS_*` · `DWLP_*` · 사본 칸). PDB 에 형식이 없다.
- `[추측: 정책]` F7 때 카드를 `IDCANCEL` → `WM_CLOSE` → `WM_QUIT` 로 바꾸는 차례 — 코드가 정하는 게 아니라 정책이다. **사용자 · PE_emulator 결정.**

### 짐 지도

- **새 짐 칸 없음.** 이번 길은 test.exe 초기화 밖 (user32 안 올림) 이라 짐 지도 2장 숫자는 그대로다.

---

## 5. 더 읽기

- run 문서: [`run148-dialog-end.md`](run148-dialog-end.md)
  - "정적 — 호출 지점" 표 · 1절 (끝 길 지도 E1 ~ E21) · 2절 (Enter 길) · 3절 (syscall 0 조건) · 4절 (반환을 보는 / 안 보는 syscall) · 5절 (금지 꼴 F1 ~ F11) · 6절 (A 판 · 모달 아닌 판) · 7절 (앱이 거의 늘 부르는 것) · 8절 (칸 표 보탬) · "PE_emulator 쪽" · "flow-writer 대조" · "정정" · "앞 run 고쳐 읽기"
- 요청서: [`_workspace/sast/146_dialog-end.md`](../../_workspace/sast/146_dialog-end.md)
- 결과서: [`_workspace/sast/146_dialog-end_result.md`](../../_workspace/sast/146_dialog-end_result.md) — "먼저 읽는 한 줄 답" · 1절 (끝 길 지도) · 5절 (지금 우리 맞나) · 6절 (win32k 몫 남음) · 7절 (동적) · 8절 (다음 요청 후보)
- 원 요청: `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` (물음 3)
- flow.md: [3장 — "모달 대화상자가 메시지 하나로 끝까지" 하위 절 · "대화상자 창 한 장이 hwnd 로 돌아올 때까지" 하위 절 · 창 API 하위 절](flow.md) · 4장 run142 · Q274a · run146 · run148 줄 · 5장 run148
- 앞 해설:
  - [run142 해설](run142-user32-connect-해설.md) 2.9 (모달 고리 · 알림 카드 · `EndDialog` 틀 — 맨 위 정정 한 줄 참고)
  - [run146 해설](run146-dialog-window-해설.md) 2.5 (핸들 항목 · 창 사본 · fnid) · 2.6 (사물함 · 장부) · 2.7 (컨트롤 고리) · 2.8 (창 프로시저 · 앱 프로시저 · 내선) · 2.11 (stub) · 2.12 (최소 목록 1 ~ 6 — 맨 위 고쳐 읽기 한 줄 참고)
  - [run145 해설](run145-epm-endpoint-해설.md) 2.2 (CFG 간접 호출)

# run157 dialog-win32k — 해설

> 이 글은 [run157 문서](run157-dialog-win32k.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q282** — run148 이 남긴 정적 몫 "대화상자 · 메시지 상자의 **커널 쪽 몸**" (`_workspace/plan.md`, 이번에 **끝 (run157)**)
  - PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` 의 뒷몫이다. 380 묶음 차례 **④** "Q278b · Q282" 의 뒷몫 (사용자 결정 2026-11-27, PE_emulator D179).
  - [run146](run146-dialog-window-해설.md) · [run148](run148-dialog-end-해설.md) 이 user32 쪽에서 대화상자 길을 닫으면서 "**이건 win32k (커널) 가 하는 일이라 못 봤다**" 고 넘긴 것들이다.
  - 결과서 판정 **분석성공** — 요청서 물음 1 ~ 7 모두 줄 근거로 답했다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q273** (VM GUI 세션에 얹을 꼴, 결과서 "디버거 필요" 1 ~ 9)
    - 정적 → **Q301** (결과서 "다음 요청 후보" — `plan.md` 작업본 기준)
- 근거:
  - run 문서 [`run157-dialog-win32k.md`](run157-dialog-win32k.md) (가장 중요)
  - 요청서 [`_workspace/sast/151_dialog-win32k.md`](../../_workspace/sast/151_dialog-win32k.md) · 결과서 [`151_dialog-win32k_result.md`](../../_workspace/sast/151_dialog-win32k_result.md)
    - **처음 analyze 없음.** user32 · win32kfull · kernelbase 를 `-readOnly` 로 export · refs 했다. win32kbase 는 있던 `.asm` 글만 읽었다.
    - 새로 뽑은 함수: win32kfull 48개 · user32 11개 · kernelbase 3개. 스크립트 문제 없음.
    - 작은 표 몇 개는 **바이너리 파일에서 직접 읽었다** (파이썬으로 `ms/bin` PE 파싱). 결과서 · run 문서는 이걸 `[바이너리]` 로 표시한다. 이 글도 그대로 옮긴다.
  - flow.md 변경: docs 커밋 `d26ea6e` (`git show d26ea6e -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 일곱

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 user32 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run142 · run146 · run148 과 같은 줄).
   - 그래서 이번 이야기는 "**user32 를 쓰는 프로그램이 대화상자 · 메시지 상자를 띄웠을 때, 그래픽 부서 (커널) 안에서 무슨 일이 생기나**" 다. 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다.
2. **요청 쪽 숫자 · 파일 줄은 에뮬레이터 것이다.**
   - `nt_user_peek_message.cpp :29` · `:50`, `nt_user_create_window_ex.cpp :45` · `:162` 같은 건 PE_emulator 코드 줄이다. flow-writer 는 **줄 번호만 훑었다** (읽기만, 안 고침).
   - 진짜 Windows 를 본 게 아니라서 `[관찰]` 이 아니다.
3. **오프셋 규칙은 앞 해설들과 같다. 다만 이번엔 두 기준이 섞이는 곳이 셋이다.**
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문 · 표에서 "`+0x1720 LinkWindow`" 처럼 함수 이름 **앞** 숫자 = **call 자리**. "`+0xbc4 CMP CL,0x40`" 처럼 명령과 같이 적힌 숫자 = 그 함수 안 줄.
   - 그래서 같은 `LinkWindow` 부르기가 나무에선 `+0x1725`, 본문에선 `+0x1720` 이다. 둘 다 맞다.
   - **조각 기준 오프셋** (조각 = Ghidra 가 떼어 이름 붙인 몸 일부, [run148 해설](run148-dialog-end-해설.md) 조심할 곳 3) 이 셋:
     - `InternalCreateDialog +0x1341 ~ +0x134f` = 조각 `FUN_18001fbc1 +0x310 ~ +0x31e`
     - `ButtonWndProcWorker +0x9b83b` = 조각 `FUN_1800aa27b +0x30` — 함수 시작에서 **아주 먼** 숫자지만 이상한 게 아니다
     - `MB_DlgProc +0x5eae4` 도 같은 꼴 (떨어진 범위)
   - 콜스택으로 본 적은 없다 (정적만).
4. **창 칸 번호가 두 벌이다.**
   - 창 하나에 **커널 쪽 원본** (`pwnd`) 과 **유저 쪽 열람용 사본** 이 따로 있다 ([run146 해설](run146-dialog-window-해설.md) 2.5 "새 말 — 창 사본").
   - 같은 "다음 형제" 라도 커널 원본에선 `+0x58`, 사본에선 `+0x48` 이다. 같은 "첫 자식" 도 원본 `+0x70`, 사본 `+0x38` 이다.
   - run 문서는 "사본 `+0x…`" 와 "`pwnd+0x…`" (또는 `[부모+0x70]` 처럼 커널 레지스터 기준) 를 나눠 쓴다. 이 글도 **사본** 이라고 적힌 것만 user32 가 읽는 칸이다.
5. **칸 이름은 전부 짐작이다.**
   - PDB 에 `tagWND` · `tagMSG` · `tagDIALOG` 형식이 **없다** (run146 확인) `[정적]`.
   - 그래서 `DefWindowMsgs` · `DLGC_*` · "만든이 ANSI" 같은 **칸 이름은 `[추측]`**, **오프셋과 그 칸을 읽고 쓰는 줄은 `[정적]`** 이다.
6. **비유 이름이 run 문서와 다르다.**
   - run 문서 "결과" 비유: **줄 서는 학급 · 출석부 · 우편함 · 편지 · 전화 · 초인종 · 명찰 대장** (run 문서는 이 비유를 `[추측: 비유]` 로 둔다).
   - 그런데 앞 해설들에서 이미 대화상자 = **알림판**, 컨트롤 = **부품**, 메시지 큐 = **카드함**, `NtUserPeekMessage` = **카드함 들여다보기** 다.
   - 그래서 이 글은 학급 줄 → **알림판 부품 줄**, 우편함 → **카드함**, 명찰 대장 → **서버 정보판의 단추 명찰 줄** 로 부른다. 그림만 다르고 사실은 같다.
7. **이름 하나가 고쳐졌다.**
   - 요청서와 run148 은 `xxxPeekMessage` 라는 이름을 썼다. **이 빌드엔 그런 이름이 없다** — `pdb_aliases.py --where win32kfull.sys xxxPeekMessage` → "PDB 공개 심볼에 없음" `[정적]`.
   - 진짜 몸은 `NtUserPeekMessage → xxxInternalGetMessage → xxxRealInternalGetMessage` 다 (2.5).

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- user32 = **창 담당 건물**, win32u = **단축번호 버튼판**, `gSharedInfo` = **공용 안내 묶음**, `psi` (`gpsi`) = **서버 정보판** → [run85 해설](run85-user32-init-해설.md) 2.1 · 2.5
- **run142 에서 푼 것** → [run142 해설](run142-user32-connect-해설.md)
  - **모달 고리** (알림판 앞에서 누가 단추를 누를 때까지 기다리는 직원) · **창 메시지 (`WM_*`)** = **알림 카드** → 2.9
  - 서버 정보판의 단추 글 칸 (`psi+0x3a4 + 40*k`) → 2.10
- **run146 에서 푼 것** → [run146 해설](run146-dialog-window-해설.md)
  - **템플릿** (알림판 **조립 설명서**) → 2.4
  - **핸들 항목** (창 번호부의 한 줄) · **창 사본** (열람용 사본) · **fnid** (종류 도장) → 2.5
  - **여분 블록** (빈 사물함) · **대화상자 자료 `pdlg`** (장부, 사물함 둘째 칸) · **되부름** (내선 전화) → 2.6
  - **컨트롤** (알림판 부품) · 컨트롤 고리 → 2.7
  - **창 프로시저** (담당 직원) · **앱 프로시저** (사장님) → 2.8
  - **stub** (대리 직원) → 2.11
- **run148 에서 푼 것** → [run148 해설](run148-dialog-end-해설.md)
  - **메시지 큐** (카드함) · `NtUserPeekMessage` (들여다보기) · `NtUserWaitMessage` (졸기) · **빈 틈** → 2.3 · 맨 위 "비유"
  - `SendMessageW` = **내선으로 바로 전하기**, `PostMessage` = **카드함에 넣어 두기** → 맨 위 "비유"
  - `IsDialogMessageW` = 알림판 전용 **분류대** → 2.4
  - **장부 `+0x20`** = 기본 단추 번호 칸 **겸** 결과 칸 · `DM_GETDEFID` → 2.5
  - `WM_COMMAND` 를 곧장 주는 길 · `MessageTable` (취급 주의 표) → 2.6
  - `EndDialog` = 장부에 **"끝" 도장** → 2.7
  - **바쁜 고리** · **금지 꼴** (F1 ~ F11) → 2.9
  - **A 판 / W 판** → 2.10
  - `g_systemCallFilterId` · 사물함 셋째 칸 (`SetWindowLongPtrW`) → 2.11

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, 커널 = **관리사무소**, win32k = 관리사무소 **그래픽 부서**, syscall = **신청 창구**.
- user32 = **창 담당 건물**, `gSharedInfo` = **공용 안내 묶음**, `psi` = **서버 정보판**.
- 대화상자 = **알림판**, 컨트롤 = 알림판 **부품** (단추 등), 창 사본 = **열람용 사본**, `pdlg` = **장부**.
- 창 프로시저 = **담당 직원**, 앱 프로시저 = **사장님**, stub = **대리 직원**, 되부름 = 그래픽 부서가 거는 **내선 전화**.
- 메시지 큐 = **카드함**, 창 메시지 = **알림 카드**. `SendMessageW` = 내선, `PostMessage` = 카드함에 넣기.
- 이번에 새로 붙는 그림:
  - 알림판의 부품들은 **한 줄로 선다.** 부품마다 "**내 뒤**" 쪽지와 "**내 앞**" 쪽지를 들고 있고, 알림판은 "**맨 앞 부품**" 쪽지를 든다 (2.3).
  - 그래픽 부서는 새 부품을 늘 **줄 맨 뒤에** 세운다 (2.3).
  - 카드함에서 카드를 꺼내는 **차례가 정해져 있다**: 내선은 그 자리에서 받고, 넣어 둔 카드 → 초인종 (입력) → "**다시 칠해 주세요**" 쪽지 → 알람 (2.5).
  - 서버 정보판에는 **단추 명찰 줄** 이 11칸 있다. 명찰마다 이름과 **번호** 가 적혀 있고, 그 번호는 그래픽 부서가 써 준다 (2.8).
  - 공용 안내 묶음 안에는 "**그래픽 부서로 보낼 카드 목록**" 체크표가 있다 (2.10).

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| (1) 부품은 줄 어디에 붙나 | **맨 끝.** `xxxCreateWindowEx` 가 자식 창이면 `HWND_BOTTOM (1)` 을 주고, `LinkWindow` 가 마지막 형제 뒤에 끼운다. **연결 차례 = 템플릿 차례.** "내 앞" 칸 `사본+0x50` 도 쓴다 `[정적]` (2.3) |
| (1') A 판이면 | 확장 스타일 `0x80000000` → **`사본+0x78` = `DefDlgProcA`** (A 표 칸). `사본+0x12` 비트 3 은 이것만으론 **안 켜지고** 클래스 쪽 조건일 때만 `[정적]` (2.4) |
| (2) `NtUserPeekMessage` 는 무엇부터 꺼내나 | **보낸 것 (안에서 처리) → 게시 → 입력 → `WM_PAINT` → `WM_TIMER` → 0.** 막 선 모달 대화상자의 첫 메시지는 **`WM_PAINT`** `[정적]` (2.5) |
| (3) `MessageBox` 는 같은 고리인가 · 반환을 바꾸나 | **같은 `DialogBox2` 고리**, 반환은 **안 바꾼다** `[정적]` (2.7) |
| (3) 기본 단추 번호는 | 종류 × `DEFBUTTON` 표에서 정해진다. **`MB_YESNO` = `IDYES (6)`** (`IDOK` 아님) `[정적]` + `[바이너리]` (2.8) |
| (4) 비트표 `user32+0xda568 ~` 는 누가 · 값은 | **`gSharedInfo` 복사** — 보통 프로세스는 **csrss USERSRV 연결 답 `+8`** 에서, `NtUserProcessConnect` 는 서버 프로세스만 (run 문서 맨 위 "정정"). 한도 `0x33f` · `0x349`. `WM_GETDLGCODE` · `WM_COMMAND` · `WM_INITDIALOG` 는 목록에 없어 syscall 없이 0 `[정적]` + `[바이너리]` (2.10) |
| (5) 단추는 무엇을 하나 | 기본 단추는 `WM_GETDLGCODE` 에 **`0x2010`** 으로 답하고, 눌리면 부모에게 **`WM_COMMAND` 를 내선으로** 보낸다 `[정적]` (2.11) |
| (6) 끝 길 syscall 은 안에서 무엇을 | `NtUserDestroyWindow` 는 **`WM_DESTROY` · `WM_NCDESTROY` 를 되부르며 보낸다.** `WM_CLOSE` 기본 처리 = 곧장 `xxxDestroyWindow`, `GWLP_USERDATA` = `사본+0xd8` `[정적]` (2.12) |
| (7) 그 밖 | IMM 훅 = `imm32!ImmTranslateMessage` · `GetUserDefaultLCID` = NLS 캐시 노드 / `NtQueryDefaultLocale` `[정적]` (2.13) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**초기화가 끝난 뒤, 프로그램이 대화상자 · 메시지 상자를 띄우고 닫는 동안 — 그 길의 그래픽 부서 쪽** 이다. test.exe 에는 없는 옆길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → LdrpInitializeProcess
 │    └─ (2.7) dll 전원 켜기
 │          └─ (user32 를 올리는 프로세스라면) user32 DllMain                  ← run85
 │                └─ csrss USERSRV 연결 답 → gSharedInfo 복사 — 그 안 +0x218 ~ 가 비트표   ← run85 · 이번 (2.10)
 └─ … → NtContinue (은퇴) → exe 진입점 → (CRT) → main
                                                   └─ (user32 를 쓰는 프로그램이라면)
                                                       DialogBoxParamW / MessageBoxW
                                                        ├─ 문지기                         ← run142
                                                        ├─ 창 만들기 → hwnd               ← run146
                                                        │    └─ (커널) 부품을 줄에 잇기 · A 판      ← 이번
                                                        ├─ 모달 고리 → EndDialog → 돌아옴 ← run148
                                                        │    └─ (커널) 카드 꺼내는 차례 · 철거 때 되부름 ← 이번
                                                        └─ (MessageBox) 기본 단추 번호 표            ← 이번
```

- flow.md 에서는 **3장 "모달 대화상자가 메시지 하나로 끝까지" 하위 절 (run148) 바로 뒤** 에 새 하위 절 "대화상자 · 메시지 상자의 커널 쪽 몸" 으로 들어갔다.
- 프로그램 쪽에서 부르는 일이라 그림엔 안 넣었다 (run142 · run146 · run148 과 같다).

### 왜 이 질문이 지금 왔나

- run146 · run148 로 **user32 가 무엇을 읽고, 무엇을 검사하고, 어디서 넘어지는지** 는 거의 다 나왔다.
- 그런데 user32 는 그래픽 부서가 **해 놓은 결과** 만 읽는다. 예를 들어:
  - 부품 줄 (`사본+0x38 → +0x48`) 이 템플릿 차례로 서 있다고 **믿고** 읽는다. 누가 그렇게 세웠는지는 못 봤다.
  - `NtUserPeekMessage` 가 돌려준 카드를 **어떻게 쓰는지** 만 봤다. 어떤 카드가 **먼저** 오는지는 못 봤다.
  - 비트표 (`user32+0xda568 ~`) 를 **읽는 줄** 만 봤다. 누가 채우는지, 값이 뭔지는 못 봤다.
- PE_emulator 의 대리 직원 (stub) 은 그래픽 부서 **흉내** 를 내야 한다. 흉내를 맞게 내려면 진짜 그래픽 부서가 하는 일을 알아야 한다.
- 특히 결과서가 미리 경고한 것: "**`MB_YESNO` 이면 기본 단추가 `IDOK` 가 아니다 — stub 이 틀린 답을 낼 위험**" (plan.md Q282 줄).

### 무엇이 궁금했나

요청서 물음 (급한 것부터):

1. **(급함)** 자식 연결 순서 — `LinkWindow` 가 새 부품을 맨 앞 / 맨 뒤 어디에 끼우나. A 판 비트 (`사본+0x12` 비트 3 · `DefDlgProcA`) 는 어느 줄에서.
2. **(급함)** `NtUserPeekMessage` 의 진짜 첫 메시지들 — `WM_PAINT` · `WM_NCACTIVATE` · `WM_TIMER` 중 무엇이 먼저. `wRemove` · 반환.
3. **(급함)** `MessageBox` 의 기본 단추 ID — `MB_YESNO` 등에서 `pdlg+0x20` 에 무엇이. 같은 고리를 타나, 반환을 바꾸나.
4. 비트표 `user32+0xda568 ~ +0xda580` 를 채우는 곳 · 값.
5. 단추 (`ButtonWndProcWorker` · `xxxButtonNotifyParent`) — 기본 단추 표시 · `WM_COMMAND`.
6. 끝 길 syscall 의 부작용 — `WM_CLOSE` / `SC_CLOSE` · `xxxDestroyWindow` 되부름 · `SetWindowPos(0x97)` · `NtUserMessageCall` · `GWLP_USERDATA`.
7. 그 밖 — `GetUserDefaultLCID` · `g_systemCallFilterId` · IMM 훅 몸.

### 바퀴 전에 알던 것

- `[정적]` (run146) 대화상자 사본 칸: `+0x30` 부모 · `+0x38` 첫 자식 · `+0x48` 다음 형제 · `+0x78` 창 프로시저 (W 판 = `DefDlgProcW`) · `+0x12` 비트 3 = ANSI (이름 `[추측]`).
  - run146 stub 10 은 "부품 줄은 **템플릿 차례**" 라고 적었지만, 줄을 세우는 쪽 (커널) 을 안 봐서 `[추측]` 이었다.
- `[정적]` (run148) 모달 고리: `DialogBox2` 는 `NtUserPeekMessage` 반환이 **0 이냐 아니냐만** 본다. 빈 틈이면 `NtUserWaitMessage` 로 존다.
  - 진짜 첫 카드가 무엇인지는 **동적 · 커널 몫** 으로 남겼다.
- `[정적]` (run148) 장부 `+0x20`: 처음 `1` (`InternalCreateDialog +0x6fa`), 부품의 `WM_GETDLGCODE` 답에 `0x10` 이 있으면 그 부품 ID 로 덮인다. `EndDialog` 결과도 같은 칸.
  - run148 의 "가장 짧게 누르기" = `NtUserPeekMessage` 첫 출력에 `WM_COMMAND(LOWORD(pdlg+0x20))`.
- `[추측]` (run148) 비트표는 `NtUserInitializeClientPfnArrays` (`ClientThreadSetup +0x228`) 가 채운다 — **이번에 틀렸다고 나온다** (2.10).
- `[정적]` (run85) 보통 프로세스의 `gSharedInfo` · `gpsi` 는 **csrss (USERSRV) 연결 답** 에서만 온다. `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 `ClientThreadSetup` 의 **서버 프로세스 갈래** (`gfServerProcess ≠ 0`) 에서만 불린다.
  - run157 문서는 처음에 이 조건을 빼고 적었다가, 이 해설을 쓰면서 어긋남을 찾아 `.asm` 으로 다시 보고 **run85 쪽으로 고쳤다** (run 문서 맨 위 "정정 (run157 해설 때 대조)", 2.10).
- `[공백]` (run142 · run148) `MessageBoxWorker` · `MessageBoxTimeoutW` 가 반환을 바꾸는지, IMM 훅 몸, `xxxButtonNotifyParent` 본문.

---

## 2. 찾은 것

### 2.1 한눈에 — 그래픽 부서 쪽 다섯 갈래 `[정적]`

run 문서 "호출 관계" 나무를 대표 줄만 남긴 것이다 (오프셋은 ret 자리, `⇒` 는 syscall 너머, `⇢` 는 보내기 · 되부름).

```
user32!_UserClientDllInitialize (보통 프로세스 — gfServerProcess == 0)
 └─ +0x2b1 CsrClientConnectToServer(…, 3, 0x248 B) → +0x30b ~ +0x37c 답 +8 의 0x240 B → gSharedInfo
           (+0x218 0x33f · +0x228 0x349 = 비트표)                                                  ①
user32!ClientThreadSetup (서버 프로세스 첫 스레드만 — +0xdd ~ +0xed)
 └─ +0x127 NtUserProcessConnect ⇒ +0x265 InitMapSharedSection   (← InitMessageTables 가 만든 비트 배열)
           +0x134 ~ +0x1ad 같은 0x240 B 를 gSharedInfo 로 복사

win32kfull!xxxCreateWindowEx (부품마다)
 ├─ +0x1b9 JS (0x80000000) → … → +0x8fd [사본+0x78] = A 표 칸                                    ②
 ├─ +0xbc7 WS_CHILD → +0x2049 hwndInsertAfter = 1 (HWND_BOTTOM)
 └─ +0x1725 LinkWindow(새, 1, 부모)  마지막 형제 뒤 — 사본 +0x48 · +0x50                          ③

user32!DialogBox2 → NtUserPeekMessage ⇒ +0x104 xxxInternalGetMessage +0x7d xxxRealInternalGetMessage
     ├─ +0x4d2 xxxReceiveMessages   P1 보낸 것 (반환 안 됨)
     ├─ +0x913 xxxReadPostMessage   P3 게시
     ├─ +0xa33 xxxScanSysQueue      P4 입력
     ├─ +0xbc0 xxxDoPaint           P5 WM_PAINT {hwnd, 0xf, 0, 0}   ← 막 선 대화상자의 첫 메시지      ④
     ├─ +0xcb8 DoTimer              P6 WM_TIMER
     └─ (없음) → 0

user32!MessageBoxW → MessageBoxTimeoutW +0x18f MessageBoxWorker
 │   +0x2c8 [gpsi+칸*0x28+0x3c4] = 단추 ID (커널 xxxLoadSomeStrings +0x4e 가 써 둠)                  ⑤
 └─ +0x34e SoftModalMessageBox +0x5b6 InternalDialogBox → DialogBox2 (같은 고리, 반환 그대로)

user32!DialogBox2 +0x3cb NtUserDestroyWindow ⇒ xxxDestroyWindow … ⇢ WM_DESTROY · ⇢ WM_NCDESTROY (되부름)
```

다섯 갈래를 시간 순서로 말하면:

1. **① 프로세스 시작** — "보낼 카드 목록" 체크표가 든 공용 안내 묶음을 받는다. 보통 가게는 **인사팀 창구 (csrss USERSRV 연결 답, run85 해설 비유)** 로 받고, 그래픽 부서에서 직접 받는 건 (`NtUserProcessConnect`) **서버 가게** 뿐이다 (2.10).
2. **② · ③ 알림판 세우기** — 부품을 하나씩 만들 때마다 줄 맨 뒤에 세우고, A 판이면 담당 직원을 영문판으로 바꾼다 (2.3 · 2.4).
3. **④ 고리** — 카드함에서 정해진 차례로 카드를 꺼낸다. 막 선 알림판의 첫 카드는 "다시 칠해 주세요" (2.5 · 2.6).
4. **⑤ 메시지 상자** — 같은 고리를 타고, 단추 번호는 서버 정보판의 명찰 줄에서 읽는다 (2.7 ~ 2.9).
5. **철거** — 그래픽 부서가 철거하면서 사장님께 "철거합니다" 내선을 두 번 건다 (2.12).

### 2.2 한 줄씩 읽는 법

#### 줄 1: `└─ +0x1725 LinkWindow(새, 1, 부모)  마지막 형제 뒤 — 사본 +0x48 · +0x50`

- **누가**: win32kfull `xxxCreateWindowEx`. 부품 (컨트롤) 하나를 만들 때마다 한 번.
- **어디서**: call 자리 `+0x1720`, 돌아오는 자리 (ret) `+0x1725`. 나무 숫자는 ret 이다.
- **누구를**: win32kfull `LinkWindow` (rva `0xe4c0`).
- **인자**: `RCX` = 새 창, **`RDX` = 1** (`HWND_BOTTOM` — 맨 뒤), `R8` = 부모.
  - 이 `1` 은 바로 앞 `+0x167b PWInsertAfter` 가 "0 · 1 · −1 · −2 면 그대로" 돌려준 값이다.
  - 그 앞은 `+0x2049 MOV [RSP+0xe8],R15` (R15 = 1 은 `+0x1f7`) 가 넣어 둔 값이다.
- **무엇을 위해**: 새 부품을 부모 (알림판) 의 부품 줄 **맨 뒤** 에 세우려고. 세우면서 사본 쪽 "앞 부품의 내 뒤" 칸 (`+0x48`) 과 "새 부품의 내 앞" 칸 (`+0x50`) 을 쓴다.
- 태그 `[정적]`.

#### 줄 2: `├─ +0xbc0 xxxDoPaint  P5 WM_PAINT {hwnd, 0xf, 0, 0}`

- **누가**: win32kfull `xxxRealInternalGetMessage` (rva `0x13da9c`). `NtUserPeekMessage` 의 진짜 몸이다.
- **어디서**: call `+0xbbb`, ret `+0xbc0`.
  - 그 바로 앞 갈래 줄: `+0xbb1 TEST AL,0x20` · `+0xbb3 JZ` — "칠할 것 있음" 비트 (`QS_PAINT`, `0x20`) 가 꺼져 있으면 건너뛴다.
- **누구를**: `xxxDoPaint`.
- **무엇을 위해**: 칠할 곳이 남은 창이 있으면 **`WM_PAINT` 카드 한 장** 을 만들려고. 카드 모양은 `{hwnd = 칠할 창, message = 0xf, wParam = 0, lParam = 0}` 이다 (`xxxDoPaint +0x21e MOV [RBX+8],0xf`).
- **차례로 보면**: 이 줄은 P5 — 게시 (P3) · 입력 (P4) 을 다 본 **다음** 에야 온다. 그래서 "게시도 입력도 없는 막 선 알림판" 에선 이게 첫 카드가 된다 (2.5).
- 태그 `[정적]`.

#### 줄 3: `+0x2c8 [gpsi+칸*0x28+0x3c4] = 단추 ID (커널 xxxLoadSomeStrings +0x4e 가 써 둠)`

이 줄은 **call 이 아니다.** 읽는 연습으로 좋다.

- **누가**: user32 `MessageBoxWorker`.
- **어디서**: `+0x2c8` = 함수 안 **그 줄** (`MOV EAX,[RSI+0x3c4]` 꼴, 결과서). call 이 아니라 그냥 읽기다.
- **무엇을**: 서버 정보판 (`gpsi`) 의 단추 명찰 줄에서, 고른 칸의 `+0x3c4` 를 읽는다. 그게 **단추 번호 (ID)** 다.
- **그 값은 누가 썼나**: user32 가 아니라 **커널** `xxxLoadSomeStrings +0x4e` 가 `칸 + 1` 로 써 뒀다. 한 줄에 **두 모듈** 이 나오는 이유다.
- 태그 `[정적]`.

### 2.3 ★ (1) 부품 줄 — 새 부품은 늘 맨 뒤에 선다 `[정적]`

#### 새 말 — 형제 · 형제 연결

- **형제** = 같은 부모를 가진 창들. 알림판 하나에 붙은 단추 · 글상자 들이 서로 형제다.
- **형제 연결** = 형제들을 **한 줄로 엮어 둔 것**. 창마다 "내 뒤 형제", "내 앞 형제" 칸이 있고, 부모는 "첫 자식" 칸을 든다 `[정적: 칸 쓰임]`.
- 비유: 알림판 부품들이 **한 줄로 선다**. 부품마다 "**내 뒤**" 쪽지와 "**내 앞**" 쪽지를 들고 있고, 알림판은 "**맨 앞 부품**" 쪽지를 든다.

사본 쪽 칸 (run 문서 1절 "결론"):

| 사본 칸 | 뜻 `[추측: 이름]` | 비유 |
|---|---|---|
| `+0x30` | 부모 사본 | "내 알림판" 쪽지 |
| `+0x38` | 첫 자식 | 알림판의 "맨 앞 부품" 쪽지 |
| `+0x48` | 다음 형제 | 부품의 "내 뒤" 쪽지 |
| **`+0x50`** | **이전 형제** (이번에 새로) | 부품의 "내 앞" 쪽지 |

- 커널 원본 쪽에선 같은 연결이 다른 번호다: 첫 자식 `[부모+0x70]`, 다음 형제 `[+0x58]` (조심할 곳 4).

#### 새 말 — `hwndInsertAfter` (`HWND_TOP` · `HWND_BOTTOM`)

- 새 창을 형제 줄의 **어디에 끼울지** 정하는 값이다. 결과서 · run 문서에서:
  - **`0` = `HWND_TOP`** — 줄 **맨 앞** 에 끼운다
  - **`1` = `HWND_BOTTOM`** — 줄 **맨 뒤** 에 선다
  - `−1` · `−2` 도 `PWInsertAfter` 가 그대로 돌려주는 특별한 값이다 (이번 길엔 안 나옴)
  - 그 밖의 값은 "**이 창 뒤에**" 라는 진짜 창이다 (`LinkWindow +0x3e JBE → +0x46c` 다른 길)
- 비유: 줄에 새 사람을 세울 때 "**맨 앞에 끼워**" / "**맨 뒤에 서**" / "**저 사람 뒤에 서**" 라고 말해 주는 것.

#### 걸음마다 따라가기 (run 문서 표 A)

1. **처음엔 맨 앞 (0)** — `xxxCreateWindowEx +0xb51` 가 `[RSP+0xe8]` 에 0 (`HWND_TOP`) 을 넣는다. CBT 훅이 켜져 있으면 훅이 준 값이 들어간다.
2. **자식 창이면 맨 뒤 (1) 로 덮는다** — `+0xbc4 CMP CL,0x40` · `+0xbc7 JZ → +0x2014`.
   - 조건은 사본 `+0x1f & 0xc0 == 0x40`, 곧 `WS_CHILD` 스타일.
   - 부모가 바탕 창이든 아니든 (`+0x203b _GetDesktopWindow` · `+0x2043 JNZ → +0x2bd9`) 결국 `+0x2049 MOV [RSP+0xe8],R15` 로 간다. R15 = 1 (`+0x1f7`).
3. **값 확인** — `+0x167b PWInsertAfter` 가 0 · 1 · −1 · −2 면 그대로 돌려준다 (`PWInsertAfter +0x1c ~ +0x3a JZ → +0xfb MOV RAX,RDI`).
4. **잇기 전 정리** — `ValidateNewParent` · `UnlinkWindow` (새 창은 아직 안 이어져서 사실상 빈 일) · 자식이면 `GetTopMostInsertAfter` 건너뜀.
5. **잇기** — **`+0x1720 LinkWindow(새 창, 1, 부모)`**.
6. **줄 끝 찾기** (`LinkWindow +0x213 ~ +0x253`):
   - 부모의 첫 자식 `[부모+0x70]` 부터 시작한다.
   - 다음 형제 `[+0x58]` 를 따라간다.
   - **다음이 없거나**, 다음 형제의 사본 `+0x14` 비트 5 가 켜져 있으면 멈춘다. (이 비트의 이름은 run 문서에 없다.)
7. **끼우기** (`+0x2e1 ~ +0x3b4`):
   - 새 창의 "내 앞" = 찾은 끝 형제, **사본 `+0x50`** = 그 형제 사본
   - 끝 형제의 "내 뒤" = 새 창, **사본 `+0x48`** = 새 창 사본
8. **첫 부품일 때** — 부모에게 자식이 아직 없으면 새 창이 첫 자식이 된다 (`부모+0x70` · 부모 사본 `+0x38`) (`+0xa9 ~ +0xe0`).

#### 그래서 — 연결 차례 = 템플릿 차례 (단계별로)

1. 근거: run146 컨트롤 고리는 템플릿에 적힌 차례대로 부품을 **하나씩** 만든다 (`InternalCreateDialog +0x975` 고리) `[정적]` (run146).
2. 근거: 부품은 `WS_CHILD` 라서 **매번 맨 뒤 (1)** 로 간다 `[정적]` (위 2 ~ 5).
3. 근거: `LinkWindow` 는 맨 뒤를 받으면 **지금 줄의 끝 형제 뒤** 에 끼운다 `[정적]` (위 6 ~ 7).
4. 그래서: 먼저 만든 부품이 앞, 나중에 만든 부품이 뒤 → **줄 차례 = 템플릿 차례** `[정적]`.
5. 예를 들어 (규칙을 적용한 예시) 부품 셋 A · B · C 를 차례로 만들면:
   - 알림판 사본 `+0x38` = A
   - A `+0x48` = B · B `+0x48` = C · **C `+0x48` = 0**
   - B `+0x50` = A · C `+0x50` = B
6. run146 stub 10 의 "템플릿 차례" 가 **맞았다**. 그리고 "내 앞" 칸 `+0x50` 도 써야 한다는 게 **새로** 나왔다.

왜 이게 중요한가:

- `GetDlgItem` 은 사본 `+0x38 → +0x48` 사슬을 따라가며 ID 를 찾는다 (run148 2.11). 줄이 엉키면 못 찾는다.
- **메시지 상자** 는 `MB_AddPushButtons` 가 템플릿에 **단추를 먼저** 쓴다 (`SoftModalMessageBox +0x511`). 그래서 단추가 줄 앞쪽이다.
  - `MB_DlgProc` 은 `GetWindow(GW_CHILD)` (`+0x184`) 로 맨 앞 부품을 잡고, **기본 단추 번호 (`[RDI+0x74]`) 만큼** `GW_HWNDNEXT` (`+0x310`) 로 뒤로 가서 `NtUserSetFocus` (`+0x197`) 한다.
  - 곧 "기본 단추 = 줄의 몇 번째 단추" 로 찾는다. 줄이 단추 차례대로 서 있어야 맞는 단추에 초점이 간다.
  - 아이콘 · 글 항목이 줄 어디에 서는지는 `[공백]`.
- 최상위 창 (자식 아님) 은 1 번 걸음의 `HWND_TOP` 그대로 바탕 창 자식의 **맨 앞** 에 간다. 띠 순서 · `GetTopMostInsertAfter` 갈래는 안 따라갔다 `[공백]`.

비유: 알림판 조립 설명서에 "단추 1, 단추 2, 글상자" 순으로 적혀 있으면, 그래픽 부서는 부품을 그 순서로 만들어 **늘 줄 맨 뒤에** 세운다. 그래서 설명서 순서와 줄 순서가 같다. 세울 때 새 부품에게 "네 앞은 누구" 쪽지도 쥐여 준다.

### 2.4 (1') A 판 — 담당 직원을 영문판으로 바꾸는 줄 `[정적]`

[run148 해설](run148-dialog-end-해설.md) 2.10 에서, A 판 (`DialogBoxIndirectParamA`) 은 `InternalCreateDialog` 가 확장 스타일에 `0x80000000` 을 얹는다고 봤다. 그 표시를 커널이 받아서 무엇을 하나가 이번 물음이다.

#### 새 말 — A 표 · W 표 (클라이언트 프로시저 표)

- user32 안 창 프로시저 (담당 직원) 주소를 **두 벌** 적어 둔 표. 하나는 A 판용, 하나는 W 판용이다.
- 서버 정보판에 있다: **`gpsi+0x188` (A 표)** · **`gpsi+0x248` (W 표)** `[정적]`. 채우는 쪽은 커널 `NtUserInitializeClientPfnArrays +0xb0 → InitializeClientPfnArrays` (`+0xfe` · `+0x1f2`) `[정적]`.
  - 그런데 user32 가 이 syscall 을 부르는 곳은 `ClientThreadSetup +0x228` 하나이고, **서버 프로세스 첫 스레드일 때만** 이다 `[정적]` (run85 · run 문서 "정정").
  - 그래서 보통 프로세스는 `gpsi` 가 가리키는 세션 공유 메모리에 **서버 프로세스가 이미 채워 둔 값** 을 읽는 것으로 보인다 `[추측: 세션당 한 번 · 공유]` (run 문서 2절).
- 어느 쪽이 A 고 어느 쪽이 W 인지 **이름** 은 인자 차례로 짐작한 것 `[추측]` 이고, `MapClientNeuterToClientPfn` 이 두 표를 짝지어 쓰는 줄이 그 짝을 정한다.
- 비유: 담당 직원 명단 **두 권** — 한글판 명단 (W) 과 영문판 명단 (A). 같은 자리에 같은 일을 하는 직원이 적혀 있다.

#### 걸음마다 (run 문서 표 A')

1. **표시 보기** — `+0x1b6 TEST R15D,R15D` · `+0x1b9 JS → +0x21e4`. 확장 스타일 부호 비트 (= `0x80000000`) 가 켜졌나.
2. **표시 지우기** — `+0x21e4 BTR R12D,0x1f`. **저장되는 스타일에서는 지운다.** 원래 값은 따로 남긴다.
3. **깃발 만들기** — `+0x1bf MOV EAX,0x320` · `+0x1c7 CMOVNS AX,SI`. 켜졌으면 `0x320`, 아니면 0.
4. **명단 바꾸기** — `+0x8f1 MapClientNeuterToClientPfn(클래스, 0, 깃발)`.
   - 클래스 fnid 가 `0x29a` 또는 `0x2a1 ~ 0x2aa` 일 때만 바꾼다.
   - 깃발이 있으면, 클래스 프로시저가 **W 표 칸과 같을 때 A 표 칸** 을 돌려준다.
5. **사본에 쓰기** — `+0x8fd MOV [RAX+0x78],RCX` → **`사본+0x78`**.
   - 대화상자 (fnid `0x2a4`) 면 A = `gpsi+0x1d8` = **`DefDlgProcA` 칸**, W = `gpsi+0x298`.
6. **표시 비트 하나 켜기** — `+0xa16 SetOrClrWF(…, 0x320, …)` → **`사본+0x13` 비트 5** `[추측: 이름 = 만든이 ANSI]`.
7. **`사본+0x12` 비트 3 은?** — **6 까지로는 안 켜진다.** 켜지는 건 둘 중 하나일 때만:
   - 클래스 자체가 ANSI 깃발을 가졌을 때 (`+0xa23 ~ +0xa3a`, `+0xa3a MOV R8D,0x208`)
   - ANSI 만든이 **이고** 클래스 원자가 `gpsi+0x364 ~ +0x388` 아홉 칸 중 하나일 때 (`+0x1cbb ~ +0x1de8`)
   - 대화상자 클래스가 그 아홉에 드는지는 **`[공백]`**.

`SetOrClrWF` 깃발 읽는 법 (run 문서 B6): 숫자 = `(바이트 << 8) | 마스크`. 그래서

- `0x204` → 사본 `+0x12` 비트 2
- `0x208` → 사본 `+0x12` 비트 3
- `0x320` → 사본 `+0x13` 비트 5

#### 무엇이 바뀌나 (앞 run 과 비교)

- run146 해설 2.5 표는 `+0x12` 비트 3 = ANSI 라고 적었고, run148 은 "켜는 쪽은 커널 `[추측]`" 이라 했다.
- 이번에: **A 판 대화상자라도 이 비트는 클래스 쪽 조건일 때만 켜진다** `[정적]`. A 판의 진짜 표시는 **`사본+0x78` 이 `DefDlgProcA`** 라는 것이다.
- 끝 길에선 `+0x13` 비트 5 · `+0x12` 비트 3 이 안 쓰인다 (run148 6절) → stub 이 이 두 비트를 안 맞춰도 끝 길은 안 틀린다.
- 대신 `사본+0x78` 은 **배달할 때 직접 부르는 주소** 다 (run148 2.6 표 걸음 3). 그래서 A 판에서 W 판 직원이 적혀 있으면 틀린다 (2.14).

비유: 영문 알림판을 주문받으면, 그래픽 부서는 주문서의 "영문" 도장을 지우고 (저장 스타일에서 지움), 담당 직원 칸에 **영문판 명단의 직원** 을 적는다. 알림판 서류 자체에 "영문" 표시를 다는 건 (`+0x12` 비트 3) 알림판 **종류** 가 영문 전용일 때뿐이다.

### 2.5 ★ (2) 카드함에서 꺼내는 차례 — `NtUserPeekMessage` 의 몸 `[정적]`

#### 이름부터 — `xxxPeekMessage` 는 없다

- 요청서는 `xxxPeekMessage` 를 보라고 했다. 이 빌드 PDB 엔 그 이름이 **없다** (`pdb_aliases.py` · `_index.tsv` 둘 다) `[정적]`.
- 진짜 길: `NtUserPeekMessage +0xff` → `xxxInternalGetMessage +0x78` → **`xxxRealInternalGetMessage`** (몸).
- 이 몸은 `GetMessage` 판과 **같다**. `wRemove` · 깃발로만 갈린다 `[정적]`.

#### 새 말 — 메시지 큐 차례 (깨움 비트)

- 카드함 ([run148 해설](run148-dialog-end-해설.md) 2.3) 은 사실 칸이 **여러 개** 다. 내선 (보낸 것), 넣어 둔 카드 (게시), 입력 (키 · 마우스), 칠하기, 타이머.
- 칸마다 "**뭔가 와 있음**" 비트가 있다. 결과서 · run 문서는 `QS_SENDMESSAGE (0x40)` · `QS_POSTMESSAGE (8)` · `QS_PAINT (0x20)` · `QS_TIMER (0x10)` 라고 부른다. 이 글은 **깨움 비트** 라고 부른다.
- `xxxRealInternalGetMessage` 는 이 비트들을 **정해진 차례로** 하나씩 보고, 처음으로 켜진 칸에서 카드를 꺼낸다 `[정적]`.
- 비유: 카드함 앞에 **칸별 램프** 가 있다. 직원은 램프를 늘 **같은 순서** 로 보고, 처음 켜진 칸에서 카드를 한 장 꺼낸다.

#### 차례표 (run 문서 3절 표, 줄임)

| 차례 | 무엇 | 깨움 비트 | 돌려주나 | 줄 |
|---|---|---|---|---|
| P0 | 깃발 검사 | — | `wRemove & 0xe300fffc ≠ 0` 이면 `LastError 0x3ec` · 0 | `NtUserPeekMessage +0xde · +0xe3 JNZ → +0x189` |
| P1 | **보낸 메시지 (내선)** | `0x40` | **안 돌려준다** — 안에서 처리 | `+0x4cd` · `+0xb30 xxxReceiveMessages` |
| P2 | (입력 큐 주인 스레드일 때만) 입력 한 번 | — | 입력 카드 | `+0x8a3 xxxScanSysQueue` |
| **P3** | **게시 (넣어 둔 카드)** | `8` | 카드 | `+0x8e0 TEST AL,8` → `+0x90e xxxReadPostMessage` |
| **P4** | **입력** | `wake & mask & 0x3c07` | 카드 | `+0xa2e xxxScanSysQueue` |
| **P5** | **`WM_PAINT`** | `0x20` | `{hwnd, 0xf, 0, 0}` | `+0xbb1 TEST AL,0x20` → `+0xbbb xxxDoPaint` |
| **P6** | **`WM_TIMER`** | `0x10` | 카드 | `+0xca8` → `+0xcb3 DoTimer` |
| P7 | 없음 | — | **0** | 빈 반환 길 |

- 돌려줄 게 있으면 카드 (MSG, 0x30 B) 를 사용자 버퍼로 베낀다 (`NtUserPeekMessage +0x120 ~ +0x159`).

`wRemove` 비트 읽는 법:

- 비트 0 = "꺼낸 카드를 **뺄까**" — `+0x22c ~ +0x237 R15D = wRemove & 1` 로 넘어가 `xxxReadPostMessage` 의 "지움" 인자가 된다.
- 비트 1 = `PM_NOYIELD` — **없으면** 빈 반환 길에서 `zzzWakeInputIdle` · `xxxReceiveMessages` 를 한 번 더 부른다 (`+0x163c TEST · +0x1644 JNZ`, call `+0x1649` · `+0x1651`).
- 모달 고리 `DialogBox2` 는 `(0, 0, 0, 1)` 만 준다 — 거르기 없음, "뺀다" (run 문서 PE_emulator 표, run148 2.2 줄 1).

#### 그래서 — 막 선 알림판의 첫 카드는 `WM_PAINT` (단계별로)

1. 근거: 차례는 P1 → P2 → P3 → P4 → P5 → P6 → P7 이다 `[정적]`.
2. 상황 (run 문서가 둔 전제): 모달 대화상자가 막 섰고, **입력도 없고 게시한 카드도 없다**.
3. P1 — 알림판이 설 때 오가는 `WM_NCACTIVATE` · `WM_ACTIVATE` 같은 건 **보내기 (내선)** 다. P1 에서 **그 자리에서 처리** 되고 **돌려주지 않는다**. 그래서 `PeekMessage` 결과로는 **안 나온다** `[정적]`.
4. P2 · P3 · P4 — 비었다.
5. P5 — 칠할 곳이 있는 창이 있으면 `WM_PAINT` 를 만들어 돌려준다 `[정적]`.
   - 막 보인 창에 칠할 곳이 생긴다는 것 자체는 일반적으로 알려진 설명이다 `[추측]`. 그게 생기는 줄은 run 문서에 없다.
   - 대화상자 → 부품 중 **어느 창부터** 칠하라고 하는지는 `xxxInternalDoPaint` 몫인데, 안 읽었다 `[공백]`.
6. 다 칠하고 나면 P7 = **0** → `DialogBox2` 빈 틈 → `NtUserWaitMessage` (졸기).
7. 그래서: **첫 부름 = `WM_PAINT`** (칠할 창마다), 그다음 0 `[정적]`. run148 이 "동적으로 볼 것 1" 로 남긴 물음에 **정적 기대값** 이 생겼다 (확인은 아직 — Q273).

`WM_QUIT` 은 `xxxReadPostMessage` 안에서 나올 것인데, 안 읽었다 `[공백]`.

비유: 알림판을 막 세운 직원이 카드함을 들여다본다. 내선은 울리는 대로 그 자리에서 받으니 카드로 안 남는다. 넣어 둔 카드도 없고 초인종도 안 울렸다. 그다음 램프 — "**다시 칠해 주세요**" 가 켜져 있다. 그래서 첫 카드는 그것이다. 다 칠하고 나면 램프가 다 꺼지고, 직원은 존다.

### 2.6 `WM_PAINT` 와 갱신 영역 — 칠하기가 끝나야 쪽지가 그친다

#### 새 말 — 갱신 영역

- 창에서 **다시 그려야 하는 부분**. 이게 남아 있는 동안 `WM_PAINT` 가 온다는 게 일반적으로 알려진 설명이다 `[추측: 일반적으로 알려진 설명]`.
- run 문서 · 결과서도 "진짜는 칠할 창이 남아 있는 동안 계속 줌", "갱신 영역을 비워야" 라고 적는다.
- 비유: 알림판에 붙은 "**여기 다시 칠해 주세요**" 스티커. 칠하고 **스티커를 떼야** 쪽지가 그친다.

#### 누가 스티커를 떼나 (단계별로)

1. 근거: `WM_PAINT` (`0xf`) 는 비트표의 첫 목록 (`DefWindowMsgs`) 에 **있다** → `RealDefWindowProcWorker` 가 `NtUserMessageCall` 로 **커널에 보낸다** `[정적]` (2.10).
2. 그래서: 기본 칠하기 처리는 **그래픽 부서 안** 에서 일어난다.
3. 그래서: stub 이 `WM_PAINT` 를 만들어 주면서 `NtUserMessageCall` 은 **0 만 돌려주고 아무 일도 안 하면**, 스티커가 안 떨어진다.
4. 그러면: 다음 `PeekMessage` 에서 **같은 `WM_PAINT`** 가 또 나올 것이다 → 끝없는 되풀이 `[추측: xxxDoPaint 의 반복 조건은 안 읽음]`.
5. 그래서 run 문서 최소 목록 24: **`NtUserMessageCall` 이 `msg = 0xf` 면 그 창의 갱신 영역을 비운다** (stub 이 `WM_PAINT` 를 줄 때만).

비유: 대리 직원이 "다시 칠해 주세요" 쪽지를 넣어 줬다. 그런데 칠하는 사람 (그래픽 부서) 역할도 대리 직원이 맡았는데, 칠은 안 하고 "네" 만 하면 스티커는 그대로다. 그러면 램프가 계속 켜져 있고, 쪽지가 또 온다.

### 2.7 ★ (3) `MessageBox` 도 같은 고리 — 반환은 안 바꾼다 `[정적]`

run142 · run148 에서 `MessageBoxW` 가 `DialogBox2` 고리를 탄다는 건 봤다. 남은 물음은 "**가는 길에 반환을 바꾸는 줄이 있나**" 였다 (run148 2.13 `[공백]`).

가는 길 (call 자리):

```
MessageBoxW → MessageBoxTimeoutW +0x18a → MessageBoxWorker +0x349 → SoftModalMessageBox +0x5b1
            → InternalDialogBox → DialogBox2   (run148 E1 ~ E21 과 같은 고리, 알림판 담당은 MB_DlgProc)
```

돌아오는 길 — 바꾸는 줄을 하나씩 봤다:

| 자리 | 하는 일 | 바꾸나 |
|---|---|---|
| `DialogBox2` | 장부 `+0x20` 을 돌려줌 (run148) | — |
| `SoftModalMessageBox +0x5b8 ~ +0x5be` | **−1 만 0 으로** | −1 일 때만 |
| `MessageBoxWorker` | 받은 값 그대로 | 아니오 |
| `MessageBoxTimeoutW +0x18f MOV EBX,EAX … +0x19e MOV EAX,EBX` | 그대로 | 아니오 |

- 결론: `MessageBoxW` 반환 = **누른 단추 번호** (`EndDialog(hwnd, 누른 ID)` → 장부 `+0x20`) 그대로다. 단, 고리가 −1 을 내면 0 으로 바뀐다 `[정적]`.

비유: 메시지 상자는 알림판의 **작은 판** 일 뿐이다. 같은 직원이 같은 자리에서 기다리고, 사장님이 적은 결과를 위층 사람들이 **손대지 않고** 그대로 올려 보낸다.

### 2.8 ★ (3) 메시지 상자 종류 → 기본 단추 번호 표 `[정적]` + `[바이너리]`

#### 새 말 — 메시지 상자 단추 종류 (`uType`)

- `MessageBox` 의 넷째 인자 `uType` 는 비트 묶음이다. 이번에 쓰는 부분은 셋 (run 문서 4절 규칙):
  - **아래 4 비트 (`uType & 0xf`) = 종류.** 단추 세트 번호다. `0` `MB_OK` · `1` `MB_OKCANCEL` · `2` `MB_ABORTRETRYIGNORE` · `3` `MB_YESNOCANCEL` · `4` `MB_YESNO` · `5` `MB_RETRYCANCEL` · `6` `MB_CANCELTRYCONTINUE`. 6 보다 크면 `LastError 0x59e` · 0 (`+0x84 ~ +0x8d`).
  - **8 ~ 11 비트 (`(uType >> 8) & 0xf`) = 기본 단추 번호.** `MB_DEFBUTTON2` = `0x100`, `3` = `0x200`, `4` = `0x300`. 0 부터 센다 (첫째 단추 = 0).
  - **14 비트 (`(uType >> 14) & 1`) = `MB_HELP`.** 켜면 "도움말" 단추가 하나 더 붙는다.
- 비유: 메시지 상자 주문서의 "**단추 세트 번호**" 칸 (0 ~ 6), "**몇 번째 단추를 기본으로**" 칸, "**도움말 단추 추가**" 칸.

#### 단추를 고르는 데 쓰는 작은 표 셋 (`[바이너리]`)

user32 안에 숫자만 든 표 셋이 있다. 결과서와 flow-writer 가 **둘 다** `ms/bin` 에서 읽었다:

| 표 | 값 | 뜻 | 비유 |
|---|---|---|---|
| `mpTypeCcmd` (8칸) | `[1,2,3,3,2,2,3,0]` | 종류마다 **단추 수** | 세트마다 단추 몇 개 |
| `mpTypeIich` (8칸) | `[0,2,5,12,9,16,19,0]` | 종류마다 아래 줄에서 **시작 자리** | 세트 목록이 몇 번 칸부터인가 |
| `SEBbuttons` (23칸) | `[0,8, 0,1,8, 2,3,4,8, 5,6,8, 5,6,1,8, 3,1,8, 1,9,10,8]` | 세트별 **명찰 칸 번호** 를 이어 붙인 줄 | 세트별 단추 목록을 한 줄로 이어 쓴 종이 |

명찰 칸 번호 → 단추 (run 문서): OK 0 · CANCEL 1 · ABORT 2 · RETRY 3 · IGNORE 4 · YES 5 · NO 6 · CLOSE 7 · HELP 8 · TRYAGAIN 9 · CONTINUE 10.

- 세트마다 목록 끝에 `8 (HELP)` 가 붙어 있다. 그래서 `MB_HELP` 는 "**한 칸 더 읽기**" 일 뿐이다.

#### 명찰 번호 (`uID`) 는 그래픽 부서가 써 둔다

- 서버 정보판의 단추 명찰 줄: `gpsi + 0x3a4 + 0x28*칸` (칸 0 ~ 10). 한 칸에:
  - `+0x00` 이름 (보이는 글 — run142 2.10 의 "단추 글")
  - **`+0x20` (= `gpsi + 0x3c4 + 0x28*칸`) 번호 `uID`**
  - `+0x24` 글 번호 `uStr` (`0x320 + 칸`)
- **`uID = 칸 + 1`** 을 쓰는 건 **커널 `xxxLoadSomeStrings +0x4e`** 다 (`+0x34` 가 `uStr`, 이름은 `xxxClientLoadStringW` 로) `[정적]`.
- 그래서 단추 번호 = 칸 + 1: OK 1 · CANCEL 2 · ABORT 3 · RETRY 4 · IGNORE 5 · YES 6 · NO 7 · CLOSE 8 · HELP 9 · TRYAGAIN 10 · CONTINUE 11.
- 비유: 서버 정보판에 **단추 명찰 11개** 가 걸려 있다. 명찰마다 "예" 같은 이름과 **번호** 가 있는데, 번호는 그래픽 부서가 적어 준다.

#### 고르는 규칙 다섯 걸음 (`MessageBoxWorker`)

1. 종류 = `uType & 0xf`.
2. 단추 수 = `mpTypeCcmd[종류]` + (`MB_HELP` 면 1) (`+0xb1 ~ +0xbd`).
3. **기본 번호 = `(uType >> 8) & 0xf`. 단추 수 이상이면 0** (`+0xc0 ~ +0xcd`).
4. 단추 `j` 의 명찰 칸 = `SEBbuttons[mpTypeIich[종류] + j]` (`+0x22b` · `+0x25b`).
5. 단추 번호 = 그 명찰 칸의 `uID` (`+0x2c8`, = 칸 + 1).

#### 읽는 법 — `MB_YESNO | MB_DEFBUTTON2` (`uType = 0x104`) 를 손으로 따라가기

위 규칙과 표 값을 그대로 적용해 보자 (run 문서 표 C 와 같은 답이 나와야 한다):

1. 종류 = `0x104 & 0xf` = **4** (`MB_YESNO`).
2. 단추 수 = `mpTypeCcmd[4]` = **2**. `MB_HELP` 없음.
3. 기본 번호 = `(0x104 >> 8) & 0xf` = **1**. 1 < 2 라 그대로 → **둘째 단추**.
4. 시작 자리 = `mpTypeIich[4]` = **9**.
   - 단추 0 = `SEBbuttons[9]` = **5** (YES)
   - 단추 1 = `SEBbuttons[10]` = **6** (NO)
5. 번호 = 칸 + 1 → 단추 0 = **6 (`IDYES`)**, 단추 1 = **7 (`IDNO`)**.
6. 기본 번호 1 → **`IDNO 7`**. 표 C 의 `MB_YESNO` · `DEFBUTTON2` 칸과 같다.

한 번 더, 짧게 — `MB_OK | MB_DEFBUTTON2`:

- 단추 수 1, 기본 번호 1 → **1 은 1 이상이라 0 으로** → 첫째 단추 = `IDOK 1`. 표 C 의 `MB_OK` 줄이 다 1 인 이유다.

#### 표 C (run 문서 4절 그대로)

| 종류 | 단추 (왼쪽부터) | `DEFBUTTON1` | `DEFBUTTON2` | `DEFBUTTON3` | `DEFBUTTON4` | `+ MB_HELP` |
|---|---|---|---|---|---|---|
| `MB_OK` (0) | `IDOK 1` | **1** | 1 | 1 | 1 | `[1, 9]`: 2 → 9 |
| `MB_OKCANCEL` (1) | `1` · `IDCANCEL 2` | **1** | **2** | 1 | 1 | `[1, 2, 9]`: 3 → 9 |
| `MB_ABORTRETRYIGNORE` (2) | `IDABORT 3` · `IDRETRY 4` · `IDIGNORE 5` | **3** | **4** | **5** | 3 | `[3, 4, 5, 9]`: 4 → 9 |
| `MB_YESNOCANCEL` (3) | `IDYES 6` · `IDNO 7` · `IDCANCEL 2` | **6** | **7** | **2** | 6 | `[6, 7, 2, 9]`: 4 → 9 |
| **`MB_YESNO` (4)** | `IDYES 6` · `IDNO 7` | **6** | **7** | 6 | 6 | `[6, 7, 9]`: 3 → 9 |
| `MB_RETRYCANCEL` (5) | `IDRETRY 4` · `IDCANCEL 2` | **4** | **2** | 4 | 4 | `[4, 2, 9]`: 3 → 9 |
| `MB_CANCELTRYCONTINUE` (6) | `IDCANCEL 2` · `IDTRYAGAIN 10` · `IDCONTINUE 11` | **2** | **10** | **11** | 2 | `[2, 10, 11, 9]`: 4 → 9 |

표 읽는 법:

- **굵은 칸** = 기본 번호가 단추 수 **안쪽** 이라 그대로 쓰인 칸. 굵지 않은 칸 = 단추 수를 넘어서 **0 (첫째 단추) 으로 떨어진** 칸.
- 마지막 열 `[6, 7, 9]: 3 → 9` 는 "`MB_HELP` 를 켜면 단추 줄이 `IDYES 6 · IDNO 7 · IDHELP 9` 로 늘고, **`DEFBUTTON3` 이면 기본이 9**" 라는 뜻이다. 다른 `DEFBUTTON` 칸은 도움말 없을 때와 같다 (단추 수가 하나 늘어 0 으로 떨어지는 칸만 달라질 수 있다).
- **가장 중요한 칸**: `MB_YESNO` 의 기본은 **`IDYES 6`** 이다. **`IDOK 1` 이 아니다.**

#### 누르면 · 닫으면 — `MB_DlgProc` (run 문서 4절)

| 들어온 번호 (`WM_COMMAND` 의 `LOWORD(wParam)`) | 하는 일 |
|---|---|
| `3 · 4 · 5 · 6 · 7 · 10 · 11` | 곧장 `EndDialog(hwnd, id)` |
| `1 · 2` | **`GetDlgItem(hwnd, id)` 가 있어야** `EndDialog`. 없으면 무시 |
| `9` | 도움말 |
| 그 밖 (`8` · `0` 포함) | 무시 |

- 닫기 (×) · Esc: `MessageBoxWorker +0x331 ~ +0x346` 이 "취소 번호" 칸 `[lpmb+0x78]` 을 정한다 — `MB_OK` 면 1, 아니면 `IDCANCEL` 단추가 있으면 2, 없으면 0.
  - 0 이면 `MB_DlgProc +0x1a3 CMP · +0x1a7 JZ → +0x31f` 가 **시스템 메뉴의 닫기를 지운다** (`NtUserGetSystemMenu` · `NtUserDeleteMenu`).
  - 곧 **`MB_YESNO` · `MB_ABORTRETRYIGNORE` 는 닫기 · Esc 로 못 끝낸다** `[정적]`.
- 덤 — 기본이 `9` (도움말) 인 칸: 표 C 의 `+ MB_HELP` 열에서 기본이 9 가 되는 경우, "기본 단추를 누른 것으로 치기" 를 하면 `MB_DlgProc` 은 **도움말로 가고 `EndDialog` 를 안 부를 것** 이다 `[추측: 표 C 와 위 "누르면" 표 두 [정적] 을 이은 것]`.

#### 상자 없이 곧장 돌려주는 길

- user32 전역 `gfDMREnable` (`0xda904`) 이 0 이 아니면, 상자를 안 띄우고 **같은 표의 기본 단추 번호를 곧장 돌려준다** (`MessageBoxWorker +0xe8 CMP · +0xef JZ` 안 탈 때 `+0xf1 ~ +0x10f` → `+0x116 JMP`) `[정적]`.
- 이 전역을 누가 켜는지는 `[공백]` (Q301).

### 2.9 ★ 장부 `+0x20` 은 언제 기본 단추 번호가 되나 — 대리 직원이 넘어지는 곳 `[정적]`

[run148 해설](run148-dialog-end-해설.md) 2.5 에서 장부 `+0x20` 은 "기본 단추 번호 칸 **겸** 결과 칸" 이었다. 이번엔 그 칸이 **2.8 표의 값** 이 되는 길을 끝까지 봤다.

#### 칸이 채워지는 길

1. 처음 값 **1** — `InternalCreateDialog +0x6fa` (run146).
2. 부품을 하나 만들 때마다 `+0x9ba SendMessageW(부품, WM_GETDLGCODE 0x87)` 를 내선으로 묻는다.
3. 답에 **`0x10`** 이 있으면 (`+0x9bf TEST AL,0x10` · `+0x9c1 JNZ`) **`InternalCreateDialog +0x1341 ~ +0x134f`** 가 그 부품 ID 로 `+0x20` 을 덮는다.
   - 기본 단추 부품이 여럿이면 **마지막 것** 이 남는다.
4. 그 `0x10` 을 답하는 건 단추 담당 직원 `ButtonWndProcWorker` 다 (2.11):
   - `BS_DEFPUSHBUTTON (1)` → **`0x2010`** (`0x10` 있음)
   - `BS_PUSHBUTTON (0)` → `0x2020` (`0x10` 없음)
5. 메시지 상자에선 기본 단추가 `0x10` 으로 답하므로 `+0x20` = 표 C 의 기본 번호가 된다 (`MB_YESNO` 면 6) `[정적]`.

#### 대리 직원이 넘어지는 곳 (단계별로)

1. 근거: run148 "가장 짧게 누르기" 는 `WM_COMMAND(LOWORD(pdlg+0x20), 0 이면 1)` 을 넣는다.
2. 근거: 대리 직원이 부품 담당 직원을 **안 타게** 만들면 (사본 `+0x12` 비트 2 로 우회 · 항목 없음), 3 ~ 4 의 `0x10` 답이 안 온다.
3. 그래서: `+0x20` 은 처음 값 **1** 그대로다.
4. 그래서: `MB_YESNO` 상자에 `WM_COMMAND(1)` 이 들어간다.
5. 근거: `MB_DlgProc` 은 `1` 이면 `GetDlgItem(hwnd, 1)` 이 **있어야** 끝낸다. `MB_YESNO` 엔 ID 1 단추가 없다 → **무시**.
6. 그러면: 고리가 안 끝난다. run148 금지 꼴 **F7** (같은 카드 · 무시 · 맨 위) 로 끝없이 돈다.

run 문서가 적은 **고칠 꼴 둘** (어느 쪽을 쓸지는 사용자 · PE_emulator 결정):

- ① 사본 `+0x1c & 0xf == 1` (= `BS_DEFPUSHBUTTON`) 인 부품의 ID 를 `pdlg+0x20` 에 **직접** 써 준다 (최소 목록 22).
- ② 표 C 를 stub 이 **직접** 갖고 (종류 · `MB_DEFBUTTON*` → 번호) 그 번호로 누른다.

#### 명찰 번호가 0 이면 — 더 앞에서 넘어진다

1. 근거: 단추 번호는 서버 정보판의 명찰 `+0x3c4` (`uID`) 에서 읽는다 (`MessageBoxWorker +0x2c8`) `[정적]`.
2. 근거: run142 때 에뮬레이터는 서버 정보판을 **0 으로 채웠다**. run142 판정 "비면 빈 글 단추 (상자는 뜸)" 는 **이름 칸** 만의 얘기였다.
3. 그래서: `uID` 도 0 이면 **단추 번호가 전부 0** 이다.
4. 그러면: 눌러도 `WM_COMMAND(0)` → `MB_DlgProc` 이 무시 → **상자를 끝낼 수 없다** `[정적]`.
5. 그래서 run 문서 최소 목록 21: 명찰 11칸의 `+0x20` 에 **`uID = i + 1`** 을 써 줄 것 (`MessageBox` 필수).

비유: 장부의 "기본 단추" 칸은 처음엔 "1번" 이라 적혀 있다. 진짜 그래픽 부서에선 부품을 붙일 때마다 "너 기본 단추니?" 하고 내선으로 물어서, "예" 단추가 "네" 하면 칸을 "6번" 으로 고친다. 대리 직원이 이 내선을 건너뛰면 칸은 "1번" 그대로고, "예 / 아니오" 판에 없는 "1번 눌렸어요" 카드를 넣게 된다. 사장님은 "그런 단추 없는데?" 하고 무시한다. 명찰 번호가 아예 비어 있으면, 어떤 단추를 눌러도 "0번" 이라 역시 무시된다.

### 2.10 ★ (4) 비트표 — "그래픽 부서로 보낼 카드 목록" `[정적]` + `[바이너리]`

#### 새 말 — 비트표 (기본 처리 목록)

- 메시지 번호마다 **비트 하나** 를 둔 표. 비트가 1 이면 "이 카드는 이 목록에 있다" 는 뜻이다.
- 목록이 둘이다 (이름 `[추측]`):
  - **`DefWindowMsgs`** — 기본 처리를 **커널로 보낼** 카드
  - **`DefWindowSpecMsgs`** — 기본 처리를 **유저 쪽 (user32 안)** 에서 할 카드
- user32 의 기본 처리 담당 `RealDefWindowProcWorker` 가 이 표를 본다 (run 문서 5절, run148):
  - `msg ≤ 한도1` ∧ 첫 목록 비트 1 → **`NtUserMessageCall (fnid 0x29e)`** — 신청 창구로
  - 아니면 `msg ≤ 한도2` ∧ 둘째 목록 비트 1 → 유저 쪽 점프표
  - **둘 다 아니면 0** (`+0x56`) — 창구도 안 가고 아무 일도 안 함
- 비유: 공용 안내 묶음에 꽂힌 **체크표 두 장**. 한 장은 "그래픽 부서로 보낼 카드", 한 장은 "건물 안에서 처리할 카드". 둘 다 체크가 없으면 "할 일 없음".

#### 누가 채우나 — `NtUserInitializeClientPfnArrays` 가 **아니다**

run148 은 `ClientThreadSetup +0x228 NtUserInitializeClientPfnArrays` 가 채운다고 짐작했다 `[추측]`. 틀렸다.

그리고 이번 바퀴 안에서도 한 번 고쳐졌다. run 문서는 처음에 "`ClientThreadSetup +0x120 NtUserProcessConnect` 가 받은 0x240 B 복사" 라고 **조건 없이** 적었다. 이 해설을 쓰면서 run85 와 어긋나는 걸 찾았고, flow-writer 가 `.asm` 으로 다시 봐서 **run85 가 맞다** 고 고쳤다 (run 문서 맨 위 "정정 (run157 해설 때 대조)", flow-writer 대조 8).

길 (단계별로):

1. **커널, 일찍** — win32kfull `InitMessageTables` (rva `0x407be0`) 가 `.rdata` 의 메시지 목록 둘 (rva `0x374cb0` 43개 · `0x374c00` 77개) 로 비트 배열을 만든다 (`SharedAlloc`, `+0xae` · `+0x116`). 결과는 세션 칸 `[+0x5468 ~ +0x5480]` 에 둔다 `[정적]`.
2. **커널, 연결할 때** — `NtUserProcessConnect +0x260 InitMapSharedSection` 이 그 세션 칸을 연결 구조 (`USERCONNECT`) 에 옮기고 (`+0x2fe ~ +0x38c`), `+0x297 memcpy` 로 사용자 버퍼에 베낀다 `[정적]`.
3. **user32, 보통 프로세스** (`gfServerProcess == 0`) — `_UserClientDllInitialize +0x2aa CsrClientConnectToServer(…, 3, 0x248 B, 0x248, &gfServerProcess)` 로 **csrss USERSRV 에 연결** 한다. 돌아와서 `+0x2be CMP [gfServerProcess],0` · `+0x2ca JNZ` 를 안 타면, (PEB `+0x3` 비트 `0x2` 가 꺼져 있을 때) **`+0x30b ~ +0x37c` 가 답 `+8` (`[RSP+0x68]`) 부터 0x240 B 를 `gSharedInfo` (`+0xda350`) 로 통째 복사** 한다 → `+0x380 ~ +0x387` `gpsi` `[정적]` (run85 그대로).
4. **user32, 서버 프로세스만** (`gfServerProcess ≠ 0`, csrss 같은 서버) — 첫 스레드일 때만 `ClientThreadSetup +0x120 NtUserProcessConnect` (번호 `0x10e2`) 로 받은 **같은 0x240 B** 를 `+0x134 ~ +0x1ad` 에서 `gSharedInfo` 로 복사한다 `[정적]`.
   - 조건 줄: `+0xdd MOV EAX,[gfServerProcess]` · `+0xe3 TEST` · `+0xe5 JZ → +0x258` (끝), 그리고 `+0xeb TEST EDI` · `+0xed JZ` (EDI = 옛 `gfFirstThread`).
5. 그래서: `user32+0xda568` − `0xda350` = **`0x218`** → 비트표는 **공용 안내 묶음의 `+0x218 ~ +0x230`** 이다. 보통 프로세스에선 답이 `+8` 부터 복사되니, **USERSRV 답 버퍼로는 `+0x220` · `+0x228` · `+0x230` · `+0x238`** 이다.
6. `NtUserInitializeClientPfnArrays` (`ClientThreadSetup +0x228`) 는 **2.4 의 A 표 · W 표** 쪽이다. 비트표와 무관하고, 같은 서버 조건 아래에 있다 `[정적]`.

아직 모르는 것:

- csrss 가 **보통 프로세스의 USERSRV 답** 을 무엇으로 채우는지 (같은 `NtUserProcessConnect` 를 부르는지) 는 안 읽었다 — csrss 쪽 `[공백]`.
- 그래서 값 (`0x33f` · `0x349` · 목록 43 · 77) 은 커널 세션 칸에서 오는 같은 값으로 **보고 쓴다** `[추측: 같은 세션 칸]`.
- PEB `+0x3` 비트 `0x2` 가 켜진 프로세스는 다른 길 (PEB `+0x58` 에 `gSharedInfo` 주소를 넣어 두고, 연결 뒤 복사 없이 건너뜀) 을 탄다. 누가 그 주소에 쓰는지는 `[추측: csrss 쪽]` (run 문서 5절).

비유: 공용 안내 묶음은 보통 가게라면 **인사팀 창구 (csrss)** 에 등록할 때 답장 봉투에 들어 온다 ([run85 해설](run85-user32-init-해설.md) 2.11 그림 그대로). 그래픽 부서에서 **직접** 받는 건 csrss 같은 **서버 가게** 뿐이다. 묶음 모양 (0x240 B) 은 둘 다 같다.

칸 자리 (`+0x218 ~ +0x230`) · 값 (`0x33f` · `0x349`) · 커널이 만드는 길 (`InitMessageTables` → `InitMapSharedSection`) 은 처음 적은 그대로다.

#### 값 (run 문서 5절 표)

| user32 칸 | `gSharedInfo` | 이름 `[추측]` | 값 |
|---|---|---|---|
| `+0xda568` | `+0x218` | `DefWindowMsgs` 한도 | **`0x33f`** |
| `+0xda570` | `+0x220` | 그 비트 배열 (포인터) | 43개 비트가 1 |
| `+0xda578` | `+0x228` | `DefWindowSpecMsgs` 한도 | **`0x349`** |
| `+0xda580` | `+0x230` | 그 비트 배열 (포인터) | 77개 |

- 비트 배열 크기 = `(한도 >> 3) + 1` 바이트 → `0x68` B · `0x6a` B. 이 바이트 수는 `InitMessageTables` 규칙으로 **flow-writer 가 셈** `[정적 규칙 + 계산]`.
- 목록 전체 번호는 run 문서 5절에 있다.

#### 대화상자 길에 쓰이는 번호만 (단계별로)

| 번호 | 이름 | 첫 목록 (커널로) | 둘째 목록 | 결과 |
|---|---|---|---|---|
| `0x87` | `WM_GETDLGCODE` | 없음 | 없음 | **syscall 없이 0** |
| `0x111` | `WM_COMMAND` | 없음 | 없음 | **syscall 없이 0** |
| `0x110` | `WM_INITDIALOG` | 없음 | 없음 | **syscall 없이 0** |
| `0xf` | `WM_PAINT` | **있음** | — | 커널로 (`NtUserMessageCall`) |
| `0x10` | `WM_CLOSE` | **있음** | — | 커널로 |
| `0x112` | `WM_SYSCOMMAND` | **있음** | — | 커널로 |
| `0x86` | `WM_NCACTIVATE` | **있음** | — | 커널로 |
| `0x6` | `WM_ACTIVATE` | — | **있음** | 유저 쪽 |

- run148 7절은 "`WM_GETDLGCODE` 가 앱에서 0 이면 → syscall 0 이거나 `NtUserMessageCall` 1번 — 정적으로 못 정함" 이었다. 이번에 **syscall 0** 으로 닫혔다.

#### 대리 직원이 비트표를 안 채우면

- 보통 프로세스라면 대리 직원이 채울 곳은 **csrss USERSRV 연결 답** (PE_emulator `csr_user_connect_stub.cpp`) 의 `+0x220` · `+0x228` · `+0x230` · `+0x238` 이다 (run 문서 최소 목록 20). 배열 포인터는 게스트가 읽을 수 있는 주소여야 한다.
- 안 채우면 한도가 0 → **모든 기본 처리가 syscall 없이 0**.
- 끝 길 (`WM_COMMAND` · `WM_GETDLGCODE` · `WM_INITDIALOG`) 엔 **영향 없다** — 어차피 목록에 없다.
- 대신 `WM_PAINT` · `WM_CLOSE` · `WM_SYSCOMMAND` · `WM_NCACTIVATE` 의 기본 처리가 **아무 일도 안 한다** (run 문서 PE_emulator 표). 2.6 의 "스티커를 안 뗌" 과 이어진다.

비유: 프로세스가 문을 열 때 그래픽 부서가 공용 안내 묶음에 **체크표 두 장** 을 넣어 보낸다. 창 담당 건물의 기본 처리 직원은 카드를 받으면 체크표를 본다. "단추 눌렸어요" · "기본 단추니?" · "준비됐어요" 카드는 **어느 장에도 체크가 없다** → 그냥 "할 일 없음". "칠해 주세요" · "닫아 주세요" 는 첫 장에 체크 → 창구로 보낸다.

### 2.11 (5) 단추 담당 직원 — 기본 단추 표시와 클릭 알림 `[정적]`

#### `WM_GETDLGCODE` 에 무엇이라 답하나

- 조건: 창 fnid `0x2a1` 이고 단추 자료 `[[사본+0x128]]` 가 있을 때. 단추 종류는 `[사본+0x1c] & 0xf`.
- **`1` (`BS_DEFPUSHBUTTON`) → `0x10 | 0x2000 = 0x2010`** (`+0xe8 SUB R14D,1` · `+0xec JZ → +0x32b MOV EBX,0x10` → `+0x12e BTS RBX,0xd`).
- `0` (`BS_PUSHBUTTON`) → `0x2020` (`+0xe6 JZ → +0x129 MOV EBX,0x20`).
- 2.9 에서 본 `0x10` 이 여기서 나온다.

#### 눌리면 — 부모에게 내선

- **`xxxButtonNotifyParent(단추, 코드)` = `SendMessageW(부모 hwnd, WM_COMMAND, word [사본+0x140] | 코드 << 16, 단추 hwnd)`** (`+0x32 MOV EDX,0x111` · `+0x3a JMP SendMessageW`).
- **카드함이 아니라 내선 (보내기)** 이다. 그래서 2.5 의 P1 처럼 `PeekMessage` 결과로는 안 나온다.
- ID 는 **아랫 16 비트만** (`word [사본+0x140]`).
- 부모 hwnd 는 부모 사본 `[사본+0x30]` 의 `[+0]`. 부모 칸이 0 이면 자기 사본 (`+0xa JZ → +0x3f`).
- `BN_CLICKED (0)` 을 주는 줄: 조각 `FUN_1800aa27b +0x24 XOR EDX,EDX → +0x30 CALL` (= `ButtonWndProcWorker +0x9b83b`).
  - 그 앞에 `+0x12 SendMessageWorker(…, 0xf1 …)` (`BM_SETCHECK` `[추측: 이름]`) · `+0x1f xxxBNReleaseCapture` 가 있다.
- **어떤 입력 (마우스 뗌 · 스페이스 뗌 · `BM_CLICK`) 이 이 조각으로 가는지는 `[공백]`** — 점프표를 다 안 읽었다 (Q301).

비유: 단추 부품은 "너 기본 단추니?" 하고 물으면 "네 (`0x10`)" 라고 답한다. 누가 단추를 누르면, 단추는 알림판 담당에게 **내선으로** "저 (번호 n) 눌렸어요" 하고 바로 전한다. 카드함을 안 거친다.

### 2.12 (6) 끝 길 syscall — 그래픽 부서 안에서 생기는 일 `[정적]`

run148 4절은 끝 길 syscall 들의 **반환을 user32 가 보나** 만 봤다. 이번엔 그래픽 부서 **안** 에서 무엇이 일어나는지 봤다.

#### 닫기 두 갈래 — `WM_CLOSE` · `SC_CLOSE`

1. `WM_CLOSE (0x10)` 는 첫 목록에 있다 (2.10) → `RealDefWindowProcWorker` → `NtUserMessageCall (0x29e)` → 커널 `xxxRealDefWindowProc +0x831` → **`xxxDestroyWindow(pwnd)` 곧장. 확인 없음** `[정적]`.
   - 단 대화상자는 그 전에 `DefDlgProcWorker` 가 가로챈다. 앱이 FALSE 면 `PostMessageW(hDlg, WM_COMMAND, IDCANCEL …)` 로 바꾼다 (run148 `+0x448 ~ +0x467`).
   - `MessageBox` 의 `MB_DlgProc` 은 `WM_CLOSE` 를 따로 안 다룬다 → 같은 길.
2. `WM_SYSCOMMAND (0x112)` 도 첫 목록 → `xxxRealDefWindowProc +0x14bc xxxSysCommand` → **`SC_CLOSE (0xf060)` 면 `WM_CLOSE` 를 보낸다** (`xxxSysCommand +0x1ae`, 카드함 아님) `[정적]`.

#### 철거 — `NtUserDestroyWindow` 는 사장님께 두 번 내선을 건다

1. `NtUserDestroyWindow` (`0x1095`) → `xxxDestroyWindow` 는 여섯 걸음을 부른다: `_Prologue` · `_Phase1` · `xxxDW_DestroyOwnedWindows` · `_Phase2` · `_Phase3` · `xxxFreeWindow`.
2. `_Phase2 +0x3a9 xxxDW_SendDestroyMessages` 안에서:
   - `+0xba` 메시지 `0x272` 를 보낸다 — 무슨 메시지인지는 `[공백]` (Q301)
   - **`+0xef` `WM_DESTROY (2)`** 를 보낸다
3. `xxxFreeWindow +0x31 _Phase2 +0x49` 에서 **`WM_NCDESTROY (0x82)`** 를 보낸다.
4. 둘 다 **커널이 클라이언트로 되부르며 보낸다** (되부름 = run146 "내선 전화") `[정적]`.
5. `WM_NCDESTROY` 에서 `DefDlgProcWorker +0x31a` 가 장부 (`pdlg`) 를 푼다 (run148).

대리 직원이 이 되부름을 안 하면 (단계별로):

1. 근거: user32 는 `NtUserDestroyWindow` 반환을 안 본다 (run148) → 반환 0 이어도 모달 끝 길은 닫힌다 (run148 F10).
2. 그러나: 앱이 `WM_DESTROY` 에서 `PostQuitMessage` 로 끝내는 꼴이면, 그 카드가 안 와서 **앱 자기 루프 (`while (GetMessage())`) 가 안 끝난다** `[추측: 앱 코드 안 봄]`.
3. 그래서 최소 목록 25: 보낼지는 **정책**.

#### 숨기기 — `NtUserSetWindowPos(…, 0x97)`

- `0x97` = `NOSIZE | NOMOVE | NOZORDER | NOACTIVATE | HIDEWINDOW` (run 문서 7절).
- 길: `NtUserSetWindowPos +0x14b xxxSetWindowPos` → `+0x66 xxxSetWindowPosAndBand` → `+0x8b xxxEndDeferWindowPosEx` (`+0x203 xxxSendChangedMsgs` · `+0x68c xxxSwpActivate` · `+0x6a0 xxxDoSyncPaint` 등).
- 보이기 비트 지움 + 위치 바뀜 보내기 (`WM_WINDOWPOSCHANGED 0x47`) 가 **생길 수 있다** — 정확한 차례 · 개수는 안 읽었다 `[추측]` (Q301).

#### 사물함 메모 칸 — `GWLP_USERDATA`

- `NtUserSetWindowLongPtr` (`0x15da`) 의 음수 번호는 `xxxSetWindowData` 가 처리한다 (`xxxSetWindowLongPtr +0x4a JS` · `+0x47c`).
- **`−21` (`GWLP_USERDATA`) = 사본 `+0xd8`**: `+0x3b CMP EBX,-0x15` · `+0x3e JZ → +0xd0` · `+0xd4` 에서 **옛값을 읽고** · `+0xdb` 에서 **새값을 쓴다**. 반환 = 옛값 `[정적]`.
  - run148 의 `[추측]` ("`−21` → `+0xd8`") 이 이번에 `[정적]` 이 됐다.
- 다른 음수 번호 (run 문서 7절 표): `−12` (자식) `+0x140` · `−6` `+0x20` · `−4` `+0x78` · `−16` / `−20` 스타일 · `−8` 부모 · `−40` `+0xe8` 비트 5.
- 필터 5 일 때 (`+0x86 PsGetWin32KFilterSet` 이 5) 는 `GWLP_USERDATA` 말고 다 `LastError 0x57` 이다 (2.13).

#### 나머지 둘

- `NtUserSetMsgBox` (`0x1573`, `MB_DlgProc +0xec`) — 사본 `+0x10` 비트 5 를 켠다 (`+0x8a SetOrClrWF(1,pwnd,0x20,0)`). **반환은 안 본다** → 아무 값이면 된다. 그 비트는 `DialogBox2 +0x289` 의 Ctrl+C 길용이다.
- `NtUserPostMessage` (`EndDialog +0xd9`, `(hDlg, 0, 0, 0)`) — 카드함에 `message = 0` 한 장을 넣는다. `DialogBox2` 는 그 전에 빠지므로, **다음 앱 루프의 `PeekMessage` / `GetMessage` 가 가장 먼저 받을 것** `[추측: 큐 코드 안 읽음]`.

비유: 철거반 (그래픽 부서) 은 알림판을 뜯기 전에 사장님께 **내선 두 번** — "곧 철거합니다 (`WM_DESTROY`)", "다 뜯었습니다 (`WM_NCDESTROY`)". 대리 직원이 이 전화를 안 걸어도 알림판은 치워진다. 하지만 "철거 전화가 오면 가게 문을 닫자" 고 정해 둔 사장님은 문 닫을 때를 모른다.

### 2.13 (7) 그 밖 — 필터 · IMM · `GetUserDefaultLCID` `[정적]`

- **`g_systemCallFilterId`** — run153 이 닫았다 (sast/150 표 E). 일반 프로세스 0. 이번에 본 새 쓰임 하나: `NtUserSetWindowLongPtr +0x86 PsGetWin32KFilterSet() == 5` 면 `GWLP_USERDATA` 말고 `LastError 0x57` (`+0x12c ~ +0x13b`).
- **IMM 훅 `[user32+0xd96f0]` = `imm32!ImmTranslateMessage`** — `_InitializeImmEntryTable +0x6e3` 에서 글 `"ImmTranslateMessage"` (`0xb9f58`, `[바이너리]`) 를 잡고, `+0x6f4 GetProcAddress` → `+0x710` 에 저장.
  - `TranslateMessage +0x12 CMP word [msg+0x10],0xe5` — **wParam 아랫 16 비트가 `0xe5` 일 때만** 부른다. 메시지 종류와 상관없다 (`WM_COMMAND` ID 229 도 해당).
  - 보통은 안 탄다. 몸은 imm32 (프로젝트 없음).
  - run148 2.6 표의 `[공백: 훅 몸]` 이 이름까지는 닫혔다.
- **`GetUserDefaultLCID`** (kernelbase rva `0x50ef0`):
  - `+0x4 GetCurrentNlsCache()` → `[+0x10]` 노드가 있으면 첫 dword. **`0x1000` 이면 `0xc00` 으로** 바꿔 돌려준다 (`+0x1e`).
  - 없으면 시스템 노드 → 비었으면 `+0x39 SetupSystemLocaleHashNode(0)` → `+0xf7 NtQueryDefaultLocale`.
  - 캐시 `+0x10` 이 어떻게 채워지는지는 `[공백]` (다른 프로젝트 몫 — csrss / basesrv 쪽).
  - 끝 길에선 `IsDialogMessageW +0x22` 가 부를 때만 쓰인다 (run148).

### 2.14 PE_emulator 판정 — 맞음 · 틀림 (run 문서 "PE_emulator 쪽")

flow-writer 는 `../PE_emulator/…/src/gui/` 파일 목록 (24개) 과 몇 줄만 **줄 번호로** 확인했다 (읽기만). 고칠지는 사용자 · PE_emulator 결정.

| 지금 에뮬레이터 | 진짜 (정적) | 판정 |
|---|---|---|
| `NtUserPeekMessage` = 게시 큐만, 없으면 멈춤 (`nt_user_peek_message.cpp :29` · `:50`) | 게시 → 입력 → `WM_PAINT` → `WM_TIMER` → 0 | **순서는 맞음 (P3 한 칸만 한 꼴).** 빠진 것 `WM_PAINT` · `WM_TIMER`. 빈 큐 = 멈춤은 진짜 (0 + 졸기) 와 달라 `[정책]` |
| `WM_PAINT` 를 만들어 줄 때 | `NtUserMessageCall` 이 칠하기를 끝내야 멈춤 | 갱신 영역을 비워야 — 아니면 끝없는 반복 `[추측]` (2.6) |
| `NtUserWaitMessage` = 잠 | 빈 틈 뒤 잠 | **맞음** |
| `NtUserCreateWindowEx` — 자식 연결 · ANSI 안 함 | 맨 끝 연결 + `사본+0x50` | run146 stub 10 확정 + `+0x50` 추가 (2.3) |
| `사본+0x78` = W 판 고정 (`nt_user_create_window_ex.cpp :45` · `:162`) | A 판이면 `DefDlgProcA` | **A 판 시료에서 틀림.** W 판은 맞음 (2.4) |
| "기본 단추 누름" = `WM_COMMAND(LOWORD(pdlg+0x20))` (run148) | 기본 번호는 표 C | **`pdlg+0x20` 이 표 C 값일 때만 맞음.** 아니면 `MB_YESNO` 끝없는 고리 (2.9) |
| 서버 정보판 0 채움 (run142) | 명찰 `+0x3c4` `uID` 를 단추 번호로 읽음 | **0 이면 단추 번호 전부 0** (2.9) |
| 닫기 · Esc 를 "누름" 으로 칠 때 | `MB_YESNO` · `MB_ABORTRETRYIGNORE` 는 못 끝냄 | 2.8 |
| csrss USERSRV 연결 답 (`csr_user_connect_stub.cpp`) 의 `gSharedInfo` 칸 (`NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 보통 프로세스가 안 부름) | 비트표는 **USERSRV 답 `+8` 의 0x240 B** 가 줌. A / W 표는 `gpsi` 공유 메모리에 이미 있는 값 `[추측]` | 답의 비트표 칸이 0 이면 끝 길엔 영향 없음. 대신 `WM_PAINT` 등 기본 처리가 빈 일. `gpsi+0x188` 이 0 이면 A 판 값 모름 (2.10 · 2.4) |
| `NtUserDestroyWindow` 몸 없음 → 0 | 되부름 둘 | **반환은 맞음.** 앱 루프엔 영향 `[추측]` (2.12) |
| `NtUserSetWindowLongPtr` 몸 없음 | `−21` = `+0xd8`, 옛값 반환 | run148 판정 (틀림) 그대로 + 자리 확정 |
| `NtUserSetMsgBox` 몸 없음 → 0 | 반환 안 봄 | **맞음** |

**대리 직원이 채워야 하는 최소 목록 18 ~ 25** (run148 의 7 ~ 15 에 이어서, 줄임 — 전체는 run 문서):

18. 자식 연결 — 마지막 자식 뒤에, `+0x38` / `+0x48` / **`+0x50`** / `+0x30` (2.3)
19. A 판 — `사본+0x78` = A 표 칸, `+0x13` 비트 5. 보통 프로세스 stub 은 `NtUserInitializeClientPfnArrays` 가 안 불리니 **`gpsi` (USERSRV 답이 주는 `psi`) 의 A / W 표 칸을 미리 채워 둬야** 값을 안다 `[추측: 세션 공유 값]` (2.4)
20. **csrss USERSRV 연결 답 (`csr_user_connect_stub.cpp`) 의 `gSharedInfo` 0x240 B (답 `+8` 부터)** — `gSharedInfo` `+0x218 = 0x33f` · `+0x228 = 0x349` + 배열 둘, **답 버퍼로는 `+0x220` · `+0x228` · `+0x230` · `+0x238`** (안 채워도 끝 길엔 무관) (2.10). 처음엔 "`NtUserProcessConnect` 가 돌려주는" 으로 적혔다가 고쳐졌다 — 그건 서버 프로세스만
21. **명찰 줄** — `uID = i + 1` (`MessageBox` 필수) (2.9)
22. **`pdlg+0x20` = 기본 단추 ID** — `BS_DEFPUSHBUTTON` 부품 ID 를 직접 (2.9)
23. `NtUserSetMsgBox` — 아무 값 (2.12)
24. `NtUserMessageCall` — `WM_PAINT` 를 줄 때만, 갱신 영역 비움 (2.6)
25. `NtUserDestroyWindow` — `WM_DESTROY` · `WM_NCDESTROY` 를 보낼지는 정책 (2.12)

### 2.15 앞 run 고쳐 읽기 · 정정

틀린 **확정** 은 없었다. `[추측]` · `[공백]` 이 정적으로 닫히거나 좁혀진 것이라, run 문서는 앞 문서 셋 (과 각 해설서) 맨 위에 "고쳐 읽기 (run157)" 한 줄씩만 넣었다. 본문은 그대로다.

| 앞 run | 전 | 후 |
|---|---|---|
| **run148** | 비트표는 `NtUserInitializeClientPfnArrays` 가 `[추측]` | **`gSharedInfo` 복사** — 보통 프로세스는 csrss USERSRV 연결 답 `+8` (`_UserClientDllInitialize +0x30b ~ +0x37c`), `NtUserProcessConnect` 는 서버 프로세스만. 값 `0x33f` · `0x349` |
| run148 | `WM_GETDLGCODE` 0 길 — syscall 0 이거나 1번, 못 정함 | **syscall 0** |
| run148 | `+0x38` 차례 `[추측: 템플릿 차례]` | `[정적]` + `+0x50` |
| run148 | `+0xd8` (`−21`) `[추측]` · `NtUserDestroyWindow` 되부름 `[추측]` | 둘 다 `[정적]` |
| run148 | `+0x12` 비트 3 을 켜는 쪽 커널 `[추측]` | 클래스 쪽 조건 (확장 스타일만으론 안 켜짐) |
| run148 | IMM 훅 · `xxxButtonNotifyParent` · `MessageBoxWorker` 반환 `[공백]` | `ImmTranslateMessage` · 보내기 `WM_COMMAND` · 안 바꿈 |
| run148 | 최소 목록 7 "ID = `LOWORD(pdlg+0x20)`, 0 이면 1" | `pdlg+0x20` 이 표 C 값일 때만 |
| run148 | 이름 `xxxPeekMessage` | **이 빌드에 없음** (정정, 몸 `xxxRealInternalGetMessage`) |
| **run146** | `+0x12` 비트 3 "= 0 (유니코드)" | W 판에선 맞음. A 판에서 켜는 조건은 클래스 쪽 |
| run146 | `psi+0x320` 을 채우는 쪽 | `InitializeClientPfnArrays +0x2b2` 가 `gpsi+0x308` 에 0x58 B — `+0x320` 은 그 안 `[정적 두 줄을 이은 것]` |
| run146 | 컨트롤 프로시저 본문 `[공백]` | `WM_GETDLGCODE` 답 · 클릭 알림 닫힘 (클릭 입구는 아직 `[공백]`) |
| **run142** | `psi+0x3a4 + 40*k` 가 0 이면 빈 글 단추 (상자는 뜸) | **`+0x3c4` (`uID`) 가 0 이면 단추 번호 전부 0** — 상자는 떠도 못 끝냄 |

### 2.16 flow-writer 대조 — 결과서와 다르게 적은 곳 (짧게)

flow-writer 가 `.asm` · `_calls.tsv` 와 맞춰 보고 **고친 값** 을 run 문서에 썼다. 1 ~ 6 은 결론을 안 바꾼다 — 대부분 "갈래 줄을 바로 앞 `TEST` / `MOV` 줄로 적은 것" 이다. 7 · 8 은 이 해설을 쓰면서 찾은 어긋남을 flow-writer 가 다시 봐서 보탠 것이고, **8 은 결론 하나 (비트표 출처) 를 바꾼다.**

1. 빈 반환 길 둘째 `zzzWakeInputIdle` call 은 `+0x1646` 이 아니라 **`+0x1649`**. 갈리는 줄은 `+0x1644 JNZ`.
2. `xxxDW_SendDestroyMessages` 의 `+0xa8` · `+0xeb` 는 메시지 번호를 넣는 줄, **call 은 `+0xba` · `+0xef`**.
3. 표 A' B1 — `TEST` 는 `+0x1b6`, `JS` 가 `+0x1b9`.
4. 표 A A2 — `_GetDesktopWindow` call 은 `+0x203b`, `JNZ` 는 `+0x2043` (그 앞 `IsDpiBoundaryBetweenWindows` 갈래가 하나 더 있다 — 뜻은 안 따라감).
5. `+0x1341 ~ +0x134f` 는 **함수 기준** (`InternalCreateDialog +0x1341`). 조각 기준은 `FUN_18001fbc1 +0x310 ~ +0x31e`.
6. `MB_DlgProc` — `GetWindow(5)` call `+0x184` · `GetWindow(2)` `+0x310` · `NtUserSetFocus` `+0x197` · `JZ` 는 `+0x1a7`.

7. (이 해설 때 대조) 표 E `NtUserPostMessage` "`EndDialog +0xb0`" (sast/146 에서 온 값) → `+0xb0` 은 `NtUserSetWindowPos` 의 ret (call `+0xa9`). **`NtUserPostMessage` 는 call `+0xd9` · ret `+0xe0`**. run 문서 7절 표는 처음부터 `+0xd9` 였다.
8. (이 해설 때 대조) **비트표 출처** — 결과서 표 D · 한 줄 답 4 를 따라 "`NtUserProcessConnect` 복사" 로 조건 없이 적었던 것. `.asm` 으로 보니 그 복사는 서버 프로세스 첫 스레드만이고, 보통 프로세스는 `_UserClientDllInitialize +0x30b ~ +0x37c` 의 csrss USERSRV 답 복사다 (2.10). **이것 하나는 결론을 바꾼다.** 결과서 줄 번호 자체 (`+0x120` · `+0x134` · `+0x228`) 는 맞다.

- 보탬: 결과서 VM 항목 4 의 "`MessageBoxWorker+0x34e` 직전" — `+0x34e` 는 `SoftModalMessageBox` call (`+0x349`) 의 **ret** 다. 부르기 **전** 값을 보려면 `+0x349` 에 건다.

### 2.17 정리 — 비유로 한 번 더

- 가게가 문을 열 때, 공용 안내 묶음에 **체크표 두 장** 이 들어 온다 (보통 가게는 인사팀 창구 답장 봉투로, 서버 가게만 그래픽 부서에서 직접). "단추 눌렸어요" 류 카드엔 체크가 없어서, 창 담당 건물 안에서 조용히 "할 일 없음" 으로 끝난다.
- 알림판을 세울 때, 그래픽 부서는 부품을 조립 설명서 순서대로 만들어 **늘 줄 맨 뒤에** 세운다. 부품마다 "내 앞 · 내 뒤" 쪽지를 쥐여 준다. 영문 알림판이면 담당 직원을 **영문판 명단** 에서 고른다.
- 알림판이 서면, 직원은 카드함 램프를 **정해진 순서** 로 본다. 내선은 그 자리에서 받고, 넣어 둔 카드 · 초인종이 없으니 첫 카드는 "**다시 칠해 주세요**". 칠하고 스티커를 떼야 그친다.
- 메시지 상자의 단추 번호는 서버 정보판 **명찰 줄** 에서 읽는다. 번호는 그래픽 부서가 써 준다. "예 / 아니오" 상자의 기본은 "**예 (6)**" 이지 "확인 (1)" 이 아니다. 그리고 "예 / 아니오" 상자는 ×로도 못 닫는다.
- 철거할 땐 그래픽 부서가 사장님께 **내선 두 번** 을 건다.
- 그래서 대리 직원은: 명찰 번호를 채우고, 장부의 기본 단추 칸을 진짜 기본 단추로 고치고, 부품 줄을 템플릿 순서로 세워야 한다. 칠하기 쪽지를 줄 거면 스티커도 떼 줘야 한다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show d26ea6e -- runs/03_sprint/flow.md`)

**그림은 그대로다.** 이번 길은 test.exe 초기화 밖이기 때문이다. 바뀐 곳은 글뿐이다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (kernelbase · user32 · win32kbase · win32kfull 줄) | 앞 run 까지 | run157 한 마디씩 — 다시 analyze 안 함, 새로 뽑은 함수 (win32kfull 48 · user32 11 · kernelbase 3) |
| 근거 줄 | — | "(run157) 정적 바퀴 … 사진 값 대조 없음 — test.exe 는 user32 를 안 올린다" |
| 3장 run142 절 (`MessageBoxW` 의 벽) | — | 한 줄 — "(run157) **메시지 상자의 단추 번호 · 반환** `[정적]`" (`uID` 가 0 이면 단추 번호 전부 0 · `MB_YESNO` = `IDYES 6`) |
| 3장 run146 절 (`InternalCreateDialog`) | — | 한 줄 — "(run157) **컨트롤 연결 · A 판** `[정적]`" |
| 3장 run148 절 (`DialogBox2`) | "남은 것 → … 정적 **Q282** …" | 한 줄 — "(run157) **고리의 커널 쪽** `[정적]`" + "정적 **Q282 → run157**" |
| 3장 새 하위 절 (run148 하위 절 다음) | 없음 | **"대화상자 · 메시지 상자의 커널 쪽 몸"** — 비유 · 나무 한 장 + `[정적]` 여섯 줄 (부품 줄 맨 끝 · A 판 · 카드 꺼내는 차례 · 기본 단추 표 · 비트표 · 끝 길) |
| 4장 run146 줄 (정적 · PE_emulator) | 정적 5 · 컨트롤 프로시저 `[공백]` 열림 | "→ run157" 표시 (`gpsi+0x308` · 반쯤 해결) · PE_emulator 줄에 "run157: 템플릿 차례 확정 + `+0x50` · A 판이면 `DefDlgProcA`" |
| 4장 run148 줄 (동적) | 1 ~ 12 기대값 없음 | "run157: 1 · 6 · 9 · 12 는 **정적 기대값이 생김**" (`WM_PAINT` · 템플릿 차례 + `+0x50` · `0x33f` / `0x349` · `DefDlgProcA`) |
| 4장 run148 줄 (정적) | 1 ~ 9 열림 | **"해결됨 — 1 ~ 9 대부분 → run157 (Q282)"**, `xxxPeekMessage` 에 "(run157: 이 빌드에 없음)" |
| 4장 run148 줄 (PE_emulator) | 최소 목록 7 ~ 15 | "(run157: 맞음 — `MB_YESNO` 기본 = `IDYES 6`, ID 는 `pdlg+0x20` 이 기본 단추로 덮였을 때만)" |
| 4장 새 줄 | — | run157 줄 셋 — 해결됨 · 남은 것 (동적 Q273 · 디버거 없이 시료 · 정적 후보) · PE_emulator |
| 5장 | — | run157 두 줄 |

- **이 해설 때 대조로 한 번 더 고친 곳** (flow.md · run 문서 · run148 문서, 아직 커밋 전 — `git diff`):
  - 새 하위 절 나무에 `_UserClientDllInitialize +0x2b1 CsrClientConnectToServer(…, 3, 0x248 B)` → `+0x30b ~ +0x37c` 복사 줄이 들어가고, `ClientThreadSetup` 줄은 "**서버 프로세스 첫 스레드만** (`+0xdd ~ +0xed`)" 이 됐다.
  - 비트표 `[정적]` 줄 · 3장 run148 절의 "(run157) 고리의 커널 쪽" 줄 · 4장 "(run157 새로) PE_emulator 쪽" 줄의 "`NtUserProcessConnect` 의 복사" 가 "**보통 프로세스는 csrss USERSRV 연결 답 `+8`**, `NtUserProcessConnect` 는 서버 프로세스만" 으로 바뀌었다. PE_emulator 쪽 대상은 `csr_user_connect_stub.cpp`.
  - A / W 표 줄엔 "user32 가 그걸 부르는 건 서버 프로세스뿐이라 보통 프로세스는 공유 `gpsi` 에 이미 있는 값 `[추측]`" 이 붙었다.
- 짐 지도 (`baggage.md`) 는 안 바뀐다 — **새 짐 칸 없음** (이번 길은 초기화 밖. 비트표 · 명찰 줄 · A/W 표는 GUI 프로세스의 user32 / 세션 짐이다).

태그 변화만 뽑으면:

- 부품 줄 차례 (run146 stub 10) — `[추측: 템플릿 차례]` → **`[정적]`** + 이전 형제 `+0x50` 새로
- 비트표 채우는 곳 — `[추측]` `NtUserInitializeClientPfnArrays` → **`[정적]` `gSharedInfo` 복사 — 보통 프로세스는 csrss USERSRV 답, `NtUserProcessConnect` 는 서버 프로세스만** (틀린 짐작이 바로잡힘. run 문서도 이 해설 때 대조로 한 번 더 고침), 값 `[공백]` → **`[정적]` + `[바이너리]`**
- 첫 메시지 — 동적 몫 `[공백]` → **`[정적]` 기대값 `WM_PAINT`** (진짜 값 확인은 아직 동적)
- `MessageBox` 기본 단추 · 반환 — `[공백]` → **`[정적]` + `[바이너리]`** (표 C)
- `−21` → `+0xd8` · `DestroyWindow` 되부름 — `[추측]` → **`[정적]`**
- 칸 이름 (`tagWND` 류 · `DefWindowMsgs` · `DLGC_*`) — 여전히 **`[추측]`**

### 큰 그림에서 어떻게 이어지나

- 창 API 옆길을 시간 순서로 늘어놓으면:
  - run85 · run86 — user32 · gdi32 `DllMain` (프로그램이 user32 를 올릴 때)
  - run142 — 창 API 넷의 문지기, 모달 고리의 틀
  - run144 · run147 · run153 — GDI 쪽
  - run146 — 대화상자 창 만들기 → hwnd (user32 쪽)
  - run148 — 모달 고리 → `EndDialog` → 반환 (user32 쪽)
  - **run157 — 같은 길의 그래픽 부서 쪽** ← 이번
- 이것으로 `DialogBoxParamW` · `MessageBoxW` 한 번이 **user32 쪽도, 커널 쪽도** 줄 근거로 이어졌다. Q282 끝.
- 에뮬레이터 쪽에서 보면 남은 결정이 또렷해졌다:
  - **`MessageBox` 를 "누른 것으로 치기"** 는 이제 무엇을 넣어야 하는지 표 (표 C) 가 있다. 명찰 번호 (`uID`) 와 장부 `+0x20` 두 칸만 맞으면 된다.
  - **`WM_PAINT` 를 흉내 낼지** 는 정책이다. 흉내 내면 `NtUserMessageCall` 도 같이 손봐야 한다.
- 남은 건 대부분 **진짜 값** (동적 Q273) 이고, 정적은 가지 끝 몇 개 (Q301) 다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q273** (동적, VM GUI 세션 — 미룸)

test.exe 가 아니라 `DialogBoxParamW` / `MessageBoxW` 를 부르는 GUI 프로세스가 필요하다 (user32 유저 멈춤점). run 문서 "아직 확인 필요" 동적 1 ~ 9, 대표만:

| 무엇을 | 왜 이번에 못 알았나 |
|---|---|
| `DialogBox2+0x270` 에서 부름마다 `message` — 첫 카드가 정말 `0xf` 인지, 몇 번 · 어느 hwnd, 언제 0 이 되나 | 정적으로는 **차례** 만 봤다. 칠할 창이 몇 개 · 어느 차례인지는 실행 때 상태 (`xxxInternalDoPaint` 도 안 읽음) |
| 부품 줄 진짜 값 — `+0x38` 부터 `+0x48` 로 따라가며 `+0x140` (ID) · `+0x50` | 규칙은 봤지만 값은 실행해야 |
| A 판 — `DialogBoxIndirectParamA` 시료의 `사본+0x78` 이 `DefDlgProcA` 인지 · `+0x12` | 같음 |
| MessageBox 종류별 — `MessageBoxWorker+0x349` (부르기 전) 단추 ID 배열 · 기본 번호 · 취소 번호, 그리고 `pdlg+0x20` 이 표 C 와 같은지 | 표는 바이너리 값으로 계산한 것 — 실행으로 맞춰 봐야 |
| 명찰 줄 `gpsi+0x3c4` 부터 0x28 간격 11칸 (`uID` 1 ~ 11?) | 커널이 실행 때 쓴다 |
| 비트표 `user32+0xda568` (`0x33f`?) · `+0xda578` (`0x349`?) · `0x87` 비트 0? | 같음 |
| `DestroyWindow` 동안 받는 메시지 차례 (`0x272` · `WM_DESTROY` · `WM_NCDESTROY`) · `EndDialog` 뒤 `message = 0` | 되부름 차례는 실행해야 |

### 디버거 없이 떠 올 것 (시료 프로그램 — 대기열 자리는 오케스트레이터가 정함)

- **MessageBox 반환값 표** — 일곱 종류 × `DEFBUTTON1 ~ 4` (+ `MB_HELP` 하나) 에 Enter 한 번 → 반환값을 파일로, 표 C 와 비교. 닫기 (×) · Esc 도 (`MB_YESNO` 는 안 닫혀야).
- **부품 줄 차례** — `WM_INITDIALOG` 에서 `GW_CHILD` → `GW_HWNDNEXT` (+ `GW_HWNDPREV`) 로 ID 찍기. 메시지 상자 (`#32770`) 도 단추가 먼저인지.
- **대화상자 프로시저가 받는 메시지 차례** — `WM_PAINT` 횟수 · `WM_ERASEBKGND` · `WM_NCACTIVATE` · `WM_SHOWWINDOW` · `WM_WINDOWPOSCHANGED` · 끝의 `WM_DESTROY` · `WM_NCDESTROY`.
- **A 판** — `IsWindowUnicode(hDlg)` · `GetWindowLongPtrA/W(GWLP_WNDPROC)`.
- **`GetUserDefaultLCID`** 등 로케일 값 (`0x409` 인지, `0x1000 → 0xc00` 갈래인지).
- **`GWLP_USERDATA`** 왕복 · `GWLP_ID` 쓰기 뒤 `GetDlgCtrlID`.

### 정적으로 남은 것 → **Q301** (`plan.md` 작업본, 대기)

결과서 "다음 요청 후보" 1 ~ 7 + 다른 프로젝트 몫:

| 무엇을 | 이번에 못 본 이유 |
|---|---|
| 대화상자 클래스 원자 (`#32770`) 가 `gpsi+0x364 ~ +0x388` 아홉 칸에 드는지 (표 A' B9) | 원자를 채우는 곳을 안 읽음 (이름부터 `_index.tsv` 확인 필요) |
| `xxxEndDeferWindowPosEx` 의 숨기기 · 활성 안 함 갈래 — `SetWindowPos(0x97)` 의 정확한 보내기 차례 | 길만 따라가고 갈래는 안 읽음 (2.12 `[추측]`) |
| `xxxInternalDoPaint` — `WM_PAINT` 창 차례 · 반복 조건 | 안 읽음 (2.5 `[공백]` · 2.6 `[추측]`) |
| `xxxReadPostMessage` (`WM_QUIT`) · `xxxScanSysQueue` (입력 없을 때) | 안 읽음 |
| 단추 클릭 입구 (마우스 뗌 · 스페이스 · `BM_CLICK`) · `xxxPressButton` | 점프표를 다 안 읽음 (2.11 `[공백]`) |
| `gfDMREnable` · `gfEMIEnable` (user32 `0xda904` · `0xda930`) 를 정하는 곳 | 안 따라감 (2.8) |
| `0x272` 가 무슨 메시지인지 | 번호만 봄 (2.12 `[공백]`) |
| csrss 가 보통 프로세스의 USERSRV 연결 답 (`gSharedInfo` 0x240 B) 을 무엇으로 채우는지 · PEB `+0x3` 비트 `0x2` 길에서 PEB `+0x58` 주소에 누가 쓰는지 | csrss 쪽은 안 읽음 (2.10 `[공백]` · `[추측]`). 대기열 자리는 run 문서에 없음 |
| (다른 프로젝트) kernelbase `GetCurrentNlsCache` 안쪽 · NLS 사용자 정보를 채우는 csrss / basesrv · imm32 `ImmTranslateMessage` | 이번 요청 밖 · imm32 는 프로젝트 없음 |

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- `[공백]` 메시지 상자에서 아이콘 · 글 항목이 부품 줄 어디에 서는지 (2.3).
- `[공백]` 최상위 창 띠 순서 · `GetTopMostInsertAfter` 갈래 (2.3).
- `[공백]` `사본+0x14` 비트 5 가 무슨 표시인지 — 줄 끝 찾기에서 멈추는 조건인데 이름이 없다 (2.3).
- `[추측]` 칸 이름 전부. PDB 에 형식이 없다.
- `[추측: 정책]` 빈 큐 = 멈춤 · `WM_PAINT` 흉내 · `DestroyWindow` 되부름 여부 — 코드가 정하는 게 아니다. **사용자 · PE_emulator 결정.**

### 짐 지도

- **새 짐 칸 없음.** 이번 길은 test.exe 초기화 밖 (user32 안 올림) 이라 짐 지도 2장 숫자는 그대로다.

---

## 5. 더 읽기

- run 문서: [`run157-dialog-win32k.md`](run157-dialog-win32k.md)
  - "정적 — 호출 지점" 표 · 1절 (자식 연결 A1 ~ A10) · 2절 (ANSI B1 ~ B9) · 3절 (첫 메시지 차례 P0 ~ P7) · 4절 (MessageBox 표 C) · 5절 (비트표 표 D) · 6절 (단추) · 7절 (끝 길 syscall 표 E) · 8절 (그 밖) · "호출 관계" · "PE_emulator 쪽" (최소 목록 18 ~ 25) · "정정" · "앞 run 고쳐 읽기" · "flow-writer 대조"
- 요청서: [`_workspace/sast/151_dialog-win32k.md`](../../_workspace/sast/151_dialog-win32k.md)
- 결과서: [`_workspace/sast/151_dialog-win32k_result.md`](../../_workspace/sast/151_dialog-win32k_result.md) — "먼저 읽는 한 줄 답" 1 ~ 7 · 표 A ~ E · "PE_emulator 쪽" · "VM 으로 확인할 것" · "다음 요청 후보"
- 원 요청: `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` (뒷몫), 380 묶음 차례 ④
- flow.md: [3장 — "대화상자 · 메시지 상자의 커널 쪽 몸" 하위 절 · "모달 대화상자가 메시지 하나로 끝까지" 하위 절 · "대화상자 창 한 장이 hwnd 로 돌아올 때까지" 하위 절 · 창 API 하위 절](flow.md) · 4장 run146 · run148 · run157 줄 · 5장 run157
- 앞 해설:
  - [run142 해설](run142-user32-connect-해설.md) 2.9 (모달 고리 · 알림 카드) · 2.10 (서버 정보판 단추 글 칸 — 맨 위 고쳐 읽기 한 줄 참고)
  - [run146 해설](run146-dialog-window-해설.md) 2.5 (창 사본 칸 표 · fnid) · 2.6 (장부 · 되부름) · 2.7 (컨트롤 고리 — 맨 위 고쳐 읽기 한 줄 참고)
  - [run148 해설](run148-dialog-end-해설.md) 2.3 (카드함 · 빈 틈) · 2.5 (장부 `+0x20`) · 2.6 (`WM_COMMAND` 곧장 · `MessageTable`) · 2.9 (금지 꼴) · 2.10 (A 판) · 2.11 (사물함 셋째 칸) — 맨 위 고쳐 읽기 한 줄 참고

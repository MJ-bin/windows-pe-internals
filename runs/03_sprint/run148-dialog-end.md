# 2026-11-26 (작성일, 정적 분석 2026-11-26) run148 — 모달 대화상자가 메시지 하나로 `EndDialog` 까지 가서 빠지는 길 (Q274a 둘째 몫 = 원문 물음 3, 정적만): 진짜에서 Enter 는 고리에 **`WM_KEYDOWN (0x100)` · `VK_RETURN (0xd)`** 로 오고 `IsDialogMessageW` 가 그것을 **`SendMessageW(hDlg, WM_COMMAND, ID, 컨트롤)`** 로 바꿔 보낸다 — 기본 단추 ID 는 `DM_GETDEFID` 답이고, 그 답은 **`pdlg+0x20` 아랫 16비트** (= `EndDialog` 결과값 칸과 같은 칸). `WM_COMMAND(ID, BN_CLICKED)` 를 대화상자 hwnd 앞으로 곧장 주면 `IsDialogMessageW` → `TranslateMessage` → `DispatchMessageWorker` → `DefDlgProcW` → `DefDlgProcWorker` → 앱 프로시저까지 **win32k syscall 0번** 간다. `EndDialog` 는 `pdlg+0x18 |= 1` · `pdlg+0x20 = n` 을 쓰고 `NtUserSetWindowPos` · `NtUserPostMessage` 를 부르며 **반환은 안 본다**. `DialogBox2` 는 고리 맨 위 (`+0x12a ~ +0x142`) 에서 그 비트를 보고 빠져, **`pdlg+0x20` 을 먼저 읽고 (`+0x3bd`) `NtUserDestroyWindow` (`+0x3c4`, 반환 안 봄)** 뒤 그 값을 돌려준다. `NtUserPeekMessage` 가 늘 0 이면 (지금 에뮬레이터 기본 대답) **끝없는 바쁜 고리** — 횟수 · 시간 한도가 코드에 없다

> **고쳐 읽기 (run157, sast/151 — Q282)**: 이 문서가 넘긴 정적 ("아직 확인 필요" 정적 1 ~ 9) 이 대부분 `[정적]` 으로 닫혔다 (원문은 그대로). ① 정적 5 · 8절 비트표 "`NtUserInitializeClientPfnArrays` · `ClientThreadSetup +0x228` 가 채움 `[추측]` · `[공백: 값]`" → **아니다** — `ClientThreadSetup +0x120 NtUserProcessConnect` 가 돌려준 `gSharedInfo` 0x240 B 복사 (`+0x218 ~ +0x230`), 한도 `0x33f` · `0x349`. 7절 `WM_GETDLGCODE` 0 길은 **syscall 0** (`0x87` · `0x111` · `0x110` 은 목록에 없음). ② 8절 `+0x38` "`[추측: 템플릿 차례]`" → `[정적]` (`xxxCreateWindowEx` 가 `HWND_BOTTOM` → `LinkWindow` 가 끝에, 이전 형제 `+0x50` 도). ③ 8절 `+0xd8` · 최소 목록 14 `GWLP_USERDATA` `[추측]` → `[정적]` (`xxxSetWindowData +0xd4 / +0xdb`, 옛값 반환). ④ `NtUserDestroyWindow` 의 `WM_DESTROY` · `WM_NCDESTROY` 되부름 `[추측]` → `[정적]` (커널이 보냄). ⑤ 6절 `+0x12` 비트 3 "커널 `[추측]`" → 확장 스타일 `0x80000000` 만으론 안 켜짐 (클래스 ANSI 조건), A 판 `사본+0x78` = `DefDlgProcA`. ⑥ 3절 IMM 훅 `[공백]` → `imm32!ImmTranslateMessage`, 2절 `xxxButtonNotifyParent` `[공백]` → 부모에게 `WM_COMMAND` 보내기, "정정" 의 `MessageBoxWorker` · `MessageBoxTimeoutW` 반환 `[공백]` → 안 바꿈. ⑦ PE_emulator 최소 목록 7 "ID = `LOWORD(pdlg+0x20)`, 0 이면 1" 은 `pdlg+0x20` 이 기본 단추 ID 로 덮였을 때만 맞다 — `MB_YESNO` 의 기본은 `IDYES (6)` (run157 4절 표). ⑧ 정적 2 의 `xxxPeekMessage` 는 **이 빌드에 없다** — 몸은 `NtUserPeekMessage +0xff xxxInternalGetMessage +0x78 xxxRealInternalGetMessage`, 꺼내는 차례 게시 → 입력 → `WM_PAINT` → `WM_TIMER` → 0 (막 선 대화상자의 첫 메시지 = `WM_PAINT`). 자세히는 `run157-dialog-win32k.md`.

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** (원 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` 의 **물음 3** "기본 단추 한 번으로 끝", 튜닝 `357_tune17.md` GC1 · 병목 `dialog_box_fail` 악성 60) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 user32 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run131 · run142 · run146 과 같은 줄). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (user32 줄 · 근거 줄) · 3장 run142 하위 절 (나무 한 줄 정정 · 한 줄 보탬) · run146 하위 절 두 줄 · run146 하위 절 뒤 새 하위 절 · 4장 (run142 정적 · PE_emulator 8 표시 · Q274a 일부 해결 줄 → 해결됨 · run146 정적 1 ~ 4 닫힘 · run146 PE_emulator 한 마디 · run147 대조 한 줄 · run148 줄 넷 — 해결됨 · 동적 · 정적 · PE_emulator) · 5장 (run148 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q274a 둘째 몫** (원문 물음 3 — 첫째 몫 물음 1 · 2 는 run146). 사용자 결정 2026-11-26 "다른 것보다 가장 먼저", 원문 "3 은 run 을 나눠도 됨" 에 따라 나눔. 결과서 판정 **분석성공** — 3 (a)(b)(c)(d) 와 sast/142 11절 (앱 프로시저가 거의 늘 부르는 것) 모두 `[정적]` 줄 근거로 답함. **이것으로 Q274a 끝.**
> - 이어 쓰는 앞 run: **run146** (대화상자 창이 hwnd 로 돌아올 때까지 · 칸 표 · stub 최소 목록 1 ~ 7), **run142** (모달 고리 틀 — `DialogBox2` · `EndDialog` · `MB_DlgProc`).
> - 앞 run 정정 하나 (run142 `WM_QUIT` 반환) · 고쳐 읽기 하나 (run146 `NtUserDestroyWindow` 부르는 곳) — 각 문서 맨 위 한 줄 (아래 "정정" · "앞 run 고쳐 읽기").
> - 남은 동적 → 결과서 7절 1 ~ 12 → 대기열 **Q273** (같은 VM GUI 세션, 출처로 보탤 꼴). 남은 정적 → 결과서 6절 win32k 1 ~ 7 · 8절 → 대기열 **Q282**.

## 요약

- **(a) Enter 의 모양** `[정적]`: 고리 (`DialogBox2 +0x26b _PeekMessage`) 가 받은 메시지는 먼저 `IsDialogMessageW` (`+0x29b`) 로 간다. `WM_KEYDOWN` 이면 `SendMessageW(msg.hwnd, WM_GETDLGCODE 0x87)` (`+0x184`) — 답에 `0x4` 가 없으면 wParam 을 뺄셈 사슬로 풀어 (`+0x190 ~ +0x1ac`) `VK_RETURN` → `+0x1ea`. 기본 단추 ID 는 ① 포커스 컨트롤이 `WM_GETDLGCODE` 에 `0x10` (`+0x211`) 을 답하면 그 컨트롤 ID ② 아니면 `DM_GETDEFID (0x400)` 답 (`+0x98b4c`) 의 윗 16비트가 `0x534b` 면 아랫 16비트 ③ 아니면 `1` (`+0x98b61`). 그 ID 로 `_FindDlgItem` (조각 `+0x36e`) → **`SendMessageW(hDlg, 0x111, ID, 컨트롤 hwnd 또는 0)`** (`+0x98b19`) → TRUE. 키 메시지 자체는 번역 · 배달 안 함. 컨트롤이 꺼져 있으면 (`+0x98c18 TEST [+0x1f],8`) `NtUserMessageBeep` 만.
- **`DM_GETDEFID` 의 답** `[정적]`: `DefDlgProcWorker +0xa24 ~ +0xa55` — `pdlg+0x18` 비트 0 이 켜졌거나 `pdlg+0x20 == 0` 이면 0, 아니면 `MOVZX word [pdlg+0x20]` (`+0xa4b`) `| 0x534b0000`. `DM_SETDEFID` 는 `pdlg+0x20 = wParam` (조각 `+0x9799c`). **기본 단추 ID 칸 = 결과값 칸 = `pdlg+0x20`** (run146 · sast/142 11절의 "겸용" 확인). 처음 값은 `InternalCreateDialog +0x6fa` 의 `1`.
- **(b) `WM_COMMAND` 를 곧장 주면** `[정적]`: `IsDialogMessageW` (`MessageTable[0x111] = 0` 이라 `0x200` 거부 안 걸림, `CallMsgFilterW` 는 TEB 훅 칸 0 이면 syscall 없이 0, 메시지 갈래에 안 걸려 `+0x129` 기본) → `TranslateMessage` (`+0x3b XOR EAX,EAX` — 키 메시지 `0x100 0x101 0x104 0x105` 만 syscall) → `DispatchMessageWorker` (사본 `+0x12` 비트 2 = 0 이면 `NtUserDispatchMessage` 안 탐, `+0x1d8 UserCallWinProcCheckWow`) → `DefDlgProcW` → `DefDlgProcWorker` (fnid 검사 `+0x32 ~ +0x45` · `+0xbf UserCallDlgProcCheckWow`) → **앱 프로시저. win32k syscall 0번.**
- **`EndDialog(hDlg, n)`** `[정적]`: `+0x70 pdlg+0x18 |= 1` · `+0x7f pdlg+0x20 = n` (64비트) → `+0xa9 NtUserSetWindowPos(hDlg,0,0,0,0,0,0x97)` (`0x1021`) → `+0xd9 NtUserPostMessage(hDlg,0,0,0)` (`0x100d`) → `+0xe5` 반환 1. 활성 · 포커스 (TEB `+0x860` 블록) · 소유자가 있으면 `NtUserSetFocus` · `NtUserSetActiveWindow` · `NtUserZapActiveAndFocus` · `NtUserEnableWindow` 가 조건부로 더 붙는다. **모두 반환 안 봄** (호출 뒤 `NOP`).
- **(c) 고리에서 빠지기 · 반환** `[정적]`: `DialogBox2 +0x12a ~ +0x142` 가 매 바퀴 `[[사본+0x128]+8]` (`pdlg`) 가 NULL 인지 · `+0x18` 비트 0 인지를 본다 → `+0x389`. 빠진 뒤 `+0x39f HMValidateHandleNoSecure` (0 → 반환 0) → **`+0x3bd RSI = [pdlg+0x20]`** → **`+0x3c4 NtUserDestroyWindow`** (`0x1095`, 반환 안 봄) → (소유자 0) `+0x3ea MOV RAX,RSI` → `InternalDialogBox +0x8f` → `DialogBoxParamW` 반환 (64비트 그대로).
- **`NtUserPeekMessage` 의 반환 · 빈 틈** `[정적]`: `_PeekMessage +0x44 / +0x57` 이 반환을 그대로 주고 `DialogBox2 +0x270 TEST · +0x272 JZ → +0x20a` 는 **0 / 0 아님만** 본다. 0 이면 빈 틈: 안 보이면 `NtUserShowWindow(hwnd,1)` (`+0x2da`) + `UpdateWindow`, 보이고 소유자 있으면 `WM_ENTERIDLE (0x121)` 한 번 (`+0x111`), 소유자 없으면 (표본 80/80) **`+0x23f NtUserWaitMessage`** (`0x100a`, 반환 안 봄) → `+0x24b JMP +0x119` → 맨 위.
- **금지 꼴 (끝없는 고리)** `[정적]`: 고리가 빠지는 길은 **넷뿐** — `pdlg+0x18` 비트 0 (앱이 `EndDialog`) · `pdlg` NULL · hwnd 사라짐 (`HMValidateHandleNoSecure` 0 · 사본 `+0x2a & 0xc000`) · `WM_QUIT` (`+0x280 / +0x283`). `DialogBox2` 안에 횟수 · 시간 한도는 없다 (TEB `+0x860` ≠ 0 일 때 100 번마다 `NtYieldExecution` 만). → `NtUserPeekMessage` 늘 0 + `NtUserWaitMessage` 즉시 0 = **바쁜 고리 (F1, 지금 에뮬레이터 기본 대답)**.
- **(d) A 판 · 모달 아닌 판** `[정적]`: `DialogBoxIndirectParamA +0x9` 깃발 `2` → `InternalCreateDialog +0x5da AND R14D,2` → 확장 스타일 `| 0x80000000` (`+0x5e5 ~ +0x5f4`) · `pdlg+0x18 |= 4` (`+0x72c`) 만 다르다. `SendMessageWorker +0x14c ~ +0x16f` 글자 종류가 달라도 `WM_SETFONT` · `WM_GETDLGCODE` · `WM_INITDIALOG` · `WM_COMMAND` · `DM_*` 는 변환 · syscall 없이 지나간다 (`MessageTable & 0x100` 꺼짐, `+0x21d JZ → +0x29c`). **끝 길은 W 판과 같다.** `CreateDialogParamW/A` 는 `CreateDialogIndirectParamAorW +0x52 InternalCreateDialog` 에서 끝 (고리 없음), 자원이 없으면 0 (`DialogBox*` 는 −1).
- **앱 프로시저가 거의 늘 부르는 것** `[정적]`: `GetDlgItem` = 사본 `+0x38` (첫 자식) → `+0x48` (다음 형제) 사슬을 `+0x140` (ID) 로 찾음, syscall 0. `GetWindowLongPtrW(hDlg, DWLP_USER 0x10)` = `[사본+0x128]+0x10` 을 사용자 모드로 읽음. **`SetWindowLongPtrW(hDlg, 0x10, v)` 는 `g_systemCallFilterId ≠ 5` 면 syscall `NtUserSetWindowLongPtr` (`0x15da`)** (`_SetWindowLongPtr +0x6a / +0x71 JNZ → +0x261`, call `+0x26c`) — stub 이 같은 칸에 써야 `Get` 이 읽는다. `GetParentDialog` = 사본 `+0x12` 비트 0 인 창까지 `+0x30` 부모 사슬. `WM_GETDLGCODE` 가 앱에서 0 이면 `DefWindowProcWorker` → `RealDefWindowProcWorker` — 커널이 채우는 비트표 (`user32+0xda568 ~ +0xda580`) 에 따라 syscall 0 이거나 `NtUserMessageCall` 한 번. `DlgSetFocus` 는 `NtUserSetFocus` 꼬리호출.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백여섯째 바퀴. plan **Q274a** (둘째 몫). **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 없음.** user32 만 `-readOnly` export · refs. win32kfull · win32kbase 는 **안 엶** — 작업하는 동안 `_workspace/sast/145_gdi-display-dc_result.md` 가 없어서 요청서 ⚠ 대로 안 열고 결과서 6절 "win32k 몫 남음" 에 목록만 남김 (user32 쪽에서 닫혀 꼭 필요하지 않았음). 있던 `ms/disassemble/win32kfull/xxxCreateWindowEx.asm` 은 눈으로만 훑음 (4 (d) ANSI 비트 줄 — `[추측]`).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| user32 | `CDB947D1B96B87977915DE04CF54279C1` | 같음 (run85 · run131 · run142 · run144 · run146) |

- 버전 대조 (결과서): user32 는 "알려진 사실" 표에 줄이 없어 sast/142 가 확인한 줄과 맞춤 — `ClassNameToVersion +0x226/+0x22d` · `InternalCreateDialog +0x63f/+0x644` · `+0x653/+0x658` · `+0x667/+0x66c` · `+0xa64/+0xa69` · `+0xf7a/+0xf81` · `ValidateDialogPwnd +0x4f/+0x56` 전부 `_calls.tsv` 와 **일치**. 새 줄 `InternalDialogBox +0x8a/+0x8f` · `DialogBox2 +0x26b/+0x270` · `+0x29b/+0x2a0` · `+0x3c4/+0x3cb` 도 `.asm` 과 맞음.
- PDB 에 `tagWND` · `tagDIALOG` · `tagMSG` 형식이 없다 (run146 확인) — 칸 이름은 `[추측: 이름]`, 오프셋 · 읽는 줄은 `[정적]`.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): 정책 D156 "보이지 않는 가짜 창" 의 셋째 몫 — **대화상자는 사람이 기본 단추를 바로 누른 것으로 친다**. run146 이 창이 서는 길을 닫았으니, 이번엔 **모달 고리가 메시지 하나로 `EndDialog` 까지 가서 빠지는 길** 과 그 길의 stub (`NtUserPeekMessage` · `NtUserDestroyWindow` …) 이 돌려줄 값. 요청서 `_workspace/sast/146_dialog-end.md`:
  1. (원문 3 (a)) 진짜에서 Enter 는 고리에 어떤 메시지로 오나 · 기본 단추 ID 를 어디서 읽나.
  2. (원문 3 (b)) `WM_COMMAND(ID, BN_CLICKED)` 를 곧장 주면 앱 프로시저까지 syscall 없이 가나 · `EndDialog` 가 쓰는 칸과 syscall.
  3. (원문 3 (c)) `EndDialog` 뒤 고리가 빠질 때 syscall 의 반환을 보나 · `DialogBoxParamW` 반환은 어디서 · `NtUserPeekMessage` 반환 · **끝없는 고리 꼴**.
  4. (원문 3 (d)) `DialogBoxIndirectParamA` · `CreateDialogParam*` 이 갈라지는 자리만.
  5. (sast/142 11절) 앱 프로시저가 거의 늘 부르는 것 (`GetDlgItem` · `SetWindowLongPtrW(DWLP_USER)` · `GetParentDialog` · `WM_GETDLGCODE` 0 길).
- 쓰는 곳 (원문): 새 stub (`NtUserPeekMessage` · `NtUserDestroyWindow` · `NtUserSetDialogPointer` 류), run146 "stub 이 채워야 하는 최소 목록" 에 이어 붙임.

## 방법

- 정적 sast/146 (결과서 `_workspace/sast/146_dialog-end_result.md`, **분석성공**, 스크립트 문제 없음 — `export` · `refs` 모두 `SCRIPT ERROR` 없음, 로그 `ms/ghidra/user32.export.s146{a..e}.stdout` · `user32.refs.s146_*.stdout`):
  - user32 (asm · c, depth 0) 새로 뽑은 이름 36개: `PeekMessageW` · `GetMessageW` · `TranslateMessage` · `TranslateMessageEx` · `_GetDlgItem` · `_FindDlgItem` · `_SetWindowLongPtr` · `SetWindowLongPtrW` · `GetWindowLongPtrW` · `_GetWindowLongPtr` · `DispatchMessageW` · `IsDialogMessage` (Ghidra 이름 — PDB 이름 `IsDialogMessageA`, rva `0x10fe0`, 별칭 무리 아님) · `DialogBoxIndirectParamA` · `DialogBoxIndirectParamW` · `CreateDialogParamW` · `CreateDialogParamA` · `CreateDialogIndirectParamAorW` · `CreateDialogIndirectParamA` · `CreateDialogIndirectParamW` · `CallMsgFilterW` · `ValidateHwndNoRip` · `RealDefWindowProcWorker` · `xxxSaveDlgFocus` · `xxxRestoreDlgFocus` · `_GetNextDlgTabItem` · `GetFocus` · `_IsChild` · `IsChild` · `TestWindowProcess` · `_GETPTI` · `GETCLIENTWNDINFO` · `IsWindow` · `GetWindowData` · `GetWindowLongPtrA` · `GetWindowLongA` · `GetDlgCtrlID` (`IsWindow` · `GetDlgCtrlID` 는 run146 때도 있던 이름 — 다시 뽑음). refs `0xda568` (+0x28 B) · `0xda570` · `0xda580` · `0xd96f0`.
  - user32 이미 있던 `.asm` 읽기만: `DialogBox2` · `EndDialog` · `IsDialogMessageW` · `DispatchMessageWorker` · `DefDlgProcW` · `DefDlgProcA` · `DefDlgProcWorker` · `_PeekMessage` · `SendMessageWorker` · `InternalDialogBox` · `InternalCreateDialog` · `DialogBoxParamW` · `DialogBoxIndirectParamAorW` · `GetDlgItem` · `_NextControl` · `_GetChildControl` · `GetParentDialog` · `DlgSetFocus` · `DefWindowProcWorker` · `UserCallDlgProcCheckWow` · `GetActiveWindow` · `GetCapture` · `PtiCurrent` · `MB_DlgProc`.
  - 표 값 (파일에서 직접, `ms/bin` user32.dll): `MessageTable` (rva `0xb91f0`) · `gapfnScSendMessage` (rva `0xb4230`) · `RealDefWindowProcWorker` · `SendMessageWorker` 점프표.
- 같은 시간에 decompiler 가 sast/144 (combase · rpcss) · sast/145 (gdi32 · gdi32full · gdiplus + win32k) 를 돌았다 — 이 바퀴는 그 프로젝트를 안 열었다. explainer 는 run147 해설서를 쓰는 중 (이 문서는 그 파일을 안 건드림).
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - user32 `DialogBox2` 본체 거의 전부 (`+0x0` ~ `+0x4ae` · 떨어진 범위 `+0x9963d ~ +0x9967c` · 조각 `FUN_1800aa778`); `EndDialog` 전부 (`+0x0` ~ `+0x191`); `_PeekMessage +0x0 ~ +0x145`; `GetMessageW +0x0 ~ +0x51`; `TranslateMessage +0x0 ~ +0x5a` · `+0x6a / +0x71`; `InternalDialogBox +0x51 ~ +0xad · +0x101 ~ +0x121`.
  - `IsDialogMessageW +0x13 ~ +0x14e · +0x16a ~ +0x22d · +0x264 ~ +0x2a6` · 조각 `FUN_1800123b9` (`+0x369 ~ +0x374`) · `FUN_1800aac5f` (`+0x98c0f ~ +0x98c31`) · 주인 없는 덩어리 `+0x98add ~ +0x98b69`.
  - `DispatchMessageWorker +0x22 · +0x2f · +0x35 · +0x3b · +0x46 · +0x4f · +0x54 · +0x5d · +0x71 · +0x7d · +0x87 · +0xae · +0xb6 · +0xee · +0x104 · +0x16b · +0x1ad · +0x1b5 · +0x1cb · +0x1d8 · +0x1f5 · +0x1fc · +0x29a · +0x341 · +0x587`.
  - `DefDlgProcWorker +0x32 ~ +0x6f · +0x8c · +0x9a · +0x9c · +0xbf · +0xc4 · +0xec · +0xf0 · +0x143 ~ +0x18c · +0x1f7 · +0x207 · +0x211 · +0x218 · +0x21e · +0x2e2 · +0x31a · +0x404 · +0x448 · +0x467 · +0x46c · +0x4e5 · +0x598 · +0x684 · +0x90b · +0x913 · +0x92c · +0x935 · +0x97e · +0x9b8 · +0x9e4 · +0x9fa · +0xa1f · +0xa24 · +0xa37 · +0xa4b · +0xa55` · 조각 `+0x9799c`.
  - `_SetWindowLongPtr +0x4a · +0x6a · +0x71 · +0x87 ~ +0x92 · +0xbb · +0xc6 · +0x12d · +0x144 · +0x14d · +0x154 · +0x162 ~ +0x16c · +0x174 · +0x1c2 · +0x1c9 · +0x234 · +0x248 · +0x261 ~ +0x273`; `_GetWindowLongPtr +0x33 · +0x16e · +0x184 · +0x1d6 · +0x1e6 · +0x1ef · +0x260`; `GetDlgItem +0x8 ~ +0x7f`; `DlgSetFocus` 전부; `RealDefWindowProcWorker +0x46 · +0x4e · +0x56 · +0x76 · +0x82 · +0xb6 · +0xc6 · +0xcb · +0xcd`; `CallMsgFilterW +0x26 · +0x66 · +0xb4`; `GetActiveWindow +0x9 · +0x17`; `GetFocus +0x9 · +0x15`; `GetCapture +0x7 ~ +0x14`; `UserCallDlgProcCheckWow +0x3a · +0x53 · +0xcf · +0xf3 · +0x11d · +0x14e · +0x193`; `PeekMessageW +0x43`; `GetParentDialog +0x8 ~ +0x39`; `GetDlgCtrlID +0x14 · +0x3f`; `GetWindowData +0x57 · +0xc3 · +0xe5`.
  - A 판 · 모달 아닌 판: `DialogBoxIndirectParamA +0x9`; `DialogBoxParamW +0x39 · +0x5d · +0x6d`; `DialogBoxIndirectParamAorW +0x4a · +0x50 · +0x6c`; `CreateDialogParamW +0x37 · +0x74`; `CreateDialogParamA +0x5b`; `CreateDialogIndirectParamAorW +0x33 · +0x39 · +0x52`; `InternalCreateDialog +0x5da · +0x5e5 · +0x5f4 · +0x5ff · +0x69a ~ +0x6fa · +0x713 · +0x72c · +0xba0`; `SendMessageWorker +0x11d · +0x14c · +0x16f · +0x175 · +0x17d · +0x1a2 · +0x1a8 · +0x1f2 · +0x1fa · +0x200 · +0x214 · +0x21d · +0x225 · +0x28f · +0x29c`.
  - 정정 근거로 `SoftModalMessageBox +0x5b1 ~ +0x5be · +0x5fc` (run142 때 뽑힌 asm).
  - win32u `_syscalls.tsv` — 결과서가 쓴 번호 25개 (`0x1000` · `0x1001` · `0x1002` · `0x1004` · `0x1005` · `0x100a` · `0x100b` · `0x100d` · `0x100e` · `0x1013` · `0x1021` · `0x1028` · `0x1031` · `0x104c` · `0x1052` · `0x108d` · `0x1095` · `0x10cd` · `0x1416` · `0x14dd` · `0x14ee` · `0x1552` · `0x155b` · `0x15d6` · `0x15da`) 전부 이름 일치.
  - `pdb_aliases.py --where user32.dll IsDialogMessage` · `--lookup user32.dll 0x10fe0`.
  - `../PE_emulator-BD/ext/syscall/syscall_layer_cpp/src/gui/` 파일 목록 (19개) 과 `nt_user_get_message.cpp` (26줄, `../PE_emulator` 판과 같음) 를 **줄 번호만 훑음** (읽기만, 고치지 않음).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **모달 대화상자는 "번호표 기다리는 창구" 다.** user32 (창구 직원) 는 "다음 손님!" (`NtUserPeekMessage`) 을 부르고, 손님이 없으면 잠깐 졸다가 (`NtUserWaitMessage`) 다시 부른다. 손님이 와야만 일이 진행된다.
- 손님이 "확인 단추 눌렀어요" (`WM_COMMAND`) 라는 쪽지를 내밀면, 직원은 본사 (커널) 에 묻지 않고 바로 사장님 (앱 프로시저) 께 쪽지를 건넨다. 사장님이 "끝!" (`EndDialog`) 이라고 장부 (`pdlg`) 에 도장 (`+0x18` 비트 0) 과 결과 (`+0x20`) 를 찍으면, 직원은 다음 번호를 부르기 전에 장부를 보고 창구를 닫는다.
- 에뮬레이터는 지금 "다음 손님!" 에 늘 "없어요" (0) 라고 답하고, 졸기도 0초만 존다 — 그래서 직원이 **영원히 "다음 손님!" 만 외친다** `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/user32/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `user32!DialogBoxParamW` | `+0x6d` | `+0x72` | `DialogBoxIndirectParamAorW` (깃발 `+0x5d AND [RSP+0x28],0`) | [정적] |
| `user32!InternalDialogBox` | `+0x70` | `+0x75` | `InternalCreateDialog` (run146) | [정적] |
| 〃 | **`+0x8a`** | **`+0x8f`** | **`DialogBox2(hwnd, 소유자, 소유자가이미꺼짐, 초점줄까)`** — 셋째 인자 = 소유자 사본 `+0x1f` 비트 3 (`+0x115` · `+0x119 AND ESI,8`) | [정적] |
| `user32!DialogBox2` (rva `0x11154`) | `+0x4e` | `+0x53` | `ValidateHwnd` — 0 이면 `+0x59 JZ → +0x9963d` (−1) | [정적] |
| 〃 | `+0x5f` · `+0x486` | `+0x64` · `+0x48b` | `GetCapture` · (≠0 이면) `SendMessageW(그 창, WM_CANCELMODE 0x1f)` | [정적] |
| 〃 | `+0xd6` · `+0x111` | `+0xdb` · `+0x116` | `IsWindow(소유자)` · `SendMessageW(소유자, WM_ENTERIDLE 0x121, 0, hwnd)` — 빈 틈 · 소유자 ≠ 0 | [정적] |
| 〃 | `+0x11c` · `+0x222` · `+0x39f` | `+0x121` · `+0x227` · `+0x3a4` | `HMValidateHandleNoSecure(hwnd)` — 0 이면 빠짐 (반환 0) | [정적] |
| 〃 | `+0x1e9` | `+0x1f0` | `NtYieldExecution` — TEB `+0x860` ≠ 0 · 카운터 100 | [정적] |
| 〃 | **`+0x23f`** | **`+0x246`** | **`NtUserWaitMessage`** (`0x100a`) — 빈 틈 · 소유자 0, 반환 안 봄 | [정적] |
| 〃 | **`+0x26b`** | **`+0x270`** | **`_PeekMessage(&msg, 0, 0, 0, 1, 0, 0)`** — 0 이면 `+0x272 JZ → +0x20a` | [정적] |
| 〃 | **`+0x29b`** · `+0x2a9` · `+0x2b5` | **`+0x2a0`** · `+0x2ae` · `+0x2ba` | **`IsDialogMessageW(hwnd, &msg)`** · (FALSE 일 때만) `TranslateMessage` · `DispatchMessageWorker` | [정적] |
| 〃 | `+0x2da` · `+0x2e9` · `+0x312` | `+0x2e1` · `+0x2ee` · `+0x319` | `NtUserShowWindow(hwnd,1)` (`0x1052`) · `UpdateWindow` · `NtUserNotifyWinEvent(0x10)` (`0x1028`, `gpsi+0x764 & 0x8000`) — 빈 틈 · 안 보일 때 | [정적] |
| 〃 | `+0x354` · 조각 `+0xe` (= `+0x99632`) | `+0x359` · 조각 `+0x13` | `GetKeyState(0x11)` · `SendMessageW(hwnd, WM_COPY 0x301)` — 사본 `+0x10` 비트 5 일 때만 | [정적] |
| 〃 | `+0x37d` | `+0x384` | `NtUserPostQuitMessage(wParam)` (`0x14ee`) — `WM_QUIT` | [정적] |
| 〃 | **`+0x3c4`** | **`+0x3cb`** | **`NtUserDestroyWindow(hwnd)`** (`0x1095`) — 빠진 뒤 늘, 반환 안 봄 | [정적] |
| 〃 | `+0x3da` · `+0x43a` · `+0x44e` | `+0x3e1` · `+0x441` · `+0x455` | `NtUserGetThreadState(0xa)` (`0x1000`) · `NtUserQueryWindow(소유자,7)` (`0x100e`) · `NtUserSetForegroundWindow(소유자)` (`0x155b`) — 소유자 ≠ 0 | [정적] |
| 〃 | `+0x41c` | `+0x423` | `NtUserNotifyWinEvent(0x11)` — `gpsi+0x764 & 0x8000` | [정적] |
| 〃 | `+0x99658` · `+0x9966c` | `+0x9965f` · `+0x99673` | `NtUserEnableWindow(소유자,1)` (`0x1416`) · `NtUserSetFocus` (`0x104c`) — hwnd 무효 → −1 갈래 | [정적] |
| `user32!_PeekMessage` (rva `0x11980`) | **`+0x38`** | **`+0x3f`** | **`NtUserPeekMessage`** (`0x1001`) — `+0x44 MOV EBX,EAX` · `+0x57 MOV EAX,EBX` 반환 그대로 | [정적] |
| `user32!GetMessageW` | `+0x1b` | `+0x22` | `NtUserGetMessage` (`0x1004`) — `+0x43 MOV EAX,EDI` 반환 그대로 (`DialogBox2` 는 안 부름) | [정적] |
| `user32!IsDialogMessageW` (rva `0x12050`) | `+0x22` · `+0x3d` | `+0x29` · `+0x42` | `GetUserDefaultLCID` (kernelbase — user32 밖) · `ValidateHwndNoRip(hDlg)` | [정적] |
| 〃 | `+0x92` | `+0x97` | `CallMsgFilterW(&msg, 0)` — ≠0 이면 `+0x99 JNZ → +0x13b` TRUE | [정적] |
| 〃 | `+0xab` | `+0xb0` | `ValidateHwnd(msg.hwnd)` — `msg.hwnd == 0` 이면 앞 `+0xa2 JZ → +0x142` (0) | [정적] |
| 〃 | **`+0x12c`** · **`+0x136`** | **`+0x131`** · **`+0x13b`** | **`TranslateMessage`** · **`DispatchMessageWorker(&msg, 0)`** — 기본 갈래 (`WM_COMMAND` 포함) | [정적] |
| 〃 | `+0x184` · `+0x1ea` · `+0x209` · `+0x21d` · `+0x227` | `+0x189` · `+0x1ef` · `+0x20e` · `+0x222` · `+0x22c` | `WM_KEYDOWN` 길: `SendMessageW(msg.hwnd, 0x87)` · `GetFocus` · `SendMessageW(포커스, 0x87)` · `GetDlgCtrlID` · `ValidateHwnd` | [정적] |
| 〃 | `+0x98b4c` | `+0x98b51` | `SendMessageW(hDlg, DM_GETDEFID 0x400, 0, 0)` | [정적] |
| 〃 | 조각 `FUN_1800123b9 +0x5` (= `+0x36e`) | 조각 `+0xa` | `_FindDlgItem(hDlg 사본, ID)` | [정적] |
| 〃 | **`+0x98b19`** | **`+0x98b1e`** | **`SendMessageW(hDlg, WM_COMMAND 0x111, ID, 컨트롤 hwnd 또는 0)`** → `+0x98b1f JMP +0x13b` (TRUE) | [정적] |
| 〃 | 조각 `FUN_1800aac5f +0x15` (= `+0x98c24`) | 조각 `+0x1c` | `NtUserMessageBeep(0)` (`0x14dd`) — 찾은 컨트롤이 꺼짐 (`+0x1f` 비트 3) | [정적] |
| `user32!CallMsgFilterW` | `+0xb4` | `+0xbb` | `NtUserCallMsgFilter` (`0x1013`) — TEB 훅 칸 ≠ 0 일 때만 | [정적] |
| `user32!TranslateMessage` | `+0x4e` | `+0x55` | `NtUserTranslateMessage` (`0x100b`) — `0x100 0x101 0x104 0x105` 만 | [정적] |
| 〃 | `+0x71` | `+0x76` | ⇢ IMM 훅 `[user32+0xd96f0]` (CFG) — `word [msg+0x10] == 0xe5` 일 때 | [정적] |
| `user32!DispatchMessageWorker` | `+0x3b` | `+0x40` | `ValidateHwnd(msg.hwnd)` — 0 이면 `+0x46 JZ → +0x16b` (0) | [정적] |
| 〃 | **`+0x1d8`** | **`+0x1dd`** | **`UserCallWinProcCheckWow([사본+0xe0], [사본+0x78], …)`** | [정적] |
| 〃 | `+0x341` | `+0x348` | `NtUserDispatchMessage` (`0x1031`) — 사본 `+0x12` 비트 2 (`+0xb6`) | [정적] |
| `user32!DefDlgProcWorker` | `+0x404` | `+0x40b` | `NtUserSetWindowFNID(hwnd, 0x2a4)` (`0x108d`) — fnid 0 일 때 (run146) | [정적] |
| 〃 | **`+0xbf`** | **`+0xc4`** | **`UserCallDlgProcCheckWow`** ⇢ 앱 프로시저 `[pdlg+0x0]` (run146) — 앱 반환은 `[rbp-0x28]` (`+0xec`) | [정적] |
| 〃 | `+0x185` | `+0x18a` | `DefWindowProcWorker(사본, msg, w, l, bAnsi)` — 앱이 FALSE | [정적] |
| 〃 | `+0x207` | `+0x20c` | `_SetWindowLongPtr(hDlg, 0, 0, 0)` — `[여분+0x0]` ≠ 0 일 때 (보통 syscall `0x15da`) | [정적] |
| 〃 | `+0x467` | `+0x46c` | `PostMessageW(hDlg, 0x111, 2, 컨트롤)` — `WM_CLOSE` 에 앱이 FALSE | [정적] |
| 〃 | `+0x913` | `+0x91a` | `NtUserMessageBeep` | [정적] |
| `user32!EndDialog` (rva `0x219e0`) | `+0x1f` · `+0x33` · `+0x40` | `+0x24` · `+0x38` · `+0x45` | `ValidateHwnd` · `ValidateDialogPwnd` (둘 다 0 → `+0x18f` 반환 0) · `GetActiveWindow` | [정적] |
| 〃 | **`+0xa9`** | **`+0xb0`** | **`NtUserSetWindowPos(hDlg, 0, 0, 0, 0, 0, 0x97)`** (`0x1021`) | [정적] |
| 〃 | `+0xc2` · `+0x113` | `+0xc9` · `+0x11a` | `NtUserSetActiveWindow(소유자)` (`0x10cd`) · `NtUserZapActiveAndFocus` (`0x15d6`) — 활성 창이 hDlg 일 때 | [정적] |
| 〃 | **`+0xd9`** | **`+0xe0`** | **`NtUserPostMessage(hDlg, 0, 0, 0)`** (`0x100d`) | [정적] |
| 〃 | `+0x12a` · `+0x135` · `+0x145` | `+0x12f` · `+0x13a` · `+0x14c` | `GetFocus` · `IsChild` · `NtUserSetFocus(hDlg)` (`0x104c`) — 활성 창 = hDlg 이고 포커스가 자식 | [정적] |
| 〃 | `+0x17e` | `+0x185` | `NtUserEnableWindow(소유자, 1)` (`0x1416`) — 소유자 ≠ 0 · `pdlg+0x18` 비트 1 = 0 | [정적] |
| `user32!_SetWindowLongPtr` | **`+0x26c`** | **`+0x273`** | **`NtUserSetWindowLongPtr(hwnd, 인덱스, 값, bAnsi)`** (`0x15da`) — `g_systemCallFilterId ≠ 5` (`+0x71` · `+0x1c9 JNZ → +0x261`) | [정적] |
| `user32!RealDefWindowProcWorker` | `+0xc6` | `+0xcb` | ⇢ `gapfnScSendMessage[MessageTable[msg] & 0xff]` (CFG, fnid `0x29e` `+0xb6`) — 비트표 켜졌을 때 | [정적] |
| `user32!DlgSetFocus` | `+0x14` · `+0x2e` · `+0x3b` | `+0x19` · `+0x33` · (꼬리) | `SendMessageW(0x87)` · (`DLGC_HASSETSEL`) `SendMessageW(EM_SETSEL 0xb1)` · `JMP [NtUserSetFocus]` | [정적] |
| `user32!CreateDialogIndirectParamAorW` | `+0x52` | `+0x57` | `InternalCreateDialog` — 여기서 끝 (고리 없음) | [정적] |

### 1. 끝 길 지도 — 고리가 `WM_COMMAND(IDOK, BN_CLICKED)` 하나를 받아 `DialogBoxParamW` 가 돌아올 때까지 [정적]

run146 의 끝 (`InternalCreateDialog` 가 hwnd 를 돌려줌) 에서 잇는다. **가장 짧은 꼴** = 소유자 0 (표본 80/80) · TEB `+0x860` = 0 · 훅 0 · `gpsi+0x764` 비트 0. 켜졌을 때는 "조건" 칸. 결과서 1절 E1 ~ E21 을 줄였다 (오프셋은 아래 "flow-writer 대조" 로 고친 값).

| 차례 | 부르는 것 / syscall | 조건 | 반환을 user32 가 보나 · 0 이면 | syscall 없이 읽는 / 쓰는 칸 | 줄 |
|---|---|---|---|---|---|
| E1 | `InternalDialogBox` → `DialogBox2` | 늘 | 반환 = `DialogBoxParamW` 반환 (64비트) | — | `InternalDialogBox +0x8a/+0x8f` |
| E2 | `ValidateHwnd(hwnd)` | 늘 | 0 → **−1** (소유자 있으면 `NtUserEnableWindow` · `NtUserSetFocus`) | 핸들 항목 · TEB (run146) | `DialogBox2 +0x4e/+0x53 · +0x59 JZ` |
| E3 | `GetCapture` | 늘 | ≠0 → `WM_CANCELMODE` | `gpsi+0x1b50` (0 이면 syscall 없이 0) | `+0x5f/+0x64 · +0x67` |
| E4 | `pdlg+0x18` 비트 1 쓰기 · 스타일 읽기 | 늘 | — | **쓰기** `pdlg+0x18` 비트 1 = 셋째 인자 · 읽기 사본 `+0x1c` 비트 8 (`DS_NOIDLEMSG` `[추측: 이름]`) · `+0x1f` 비트 4 (보임 → `R12D`) · `gpsi+0x88c` 비트 0 | `+0x6d ~ +0xce` |
| **E5** | **고리 맨 위**: `pdlg` NULL · `+0x18` 비트 0 검사 | 매 바퀴 | 맞으면 **빠짐** → E18 | 사본 `+0x128` → `[+8]` | **`+0x12a ~ +0x142`** |
| E6 | (빈 큐 빠른 길) TEB `+0x860` | ≠0 일 때만 | 0 이면 곧장 E7 | TEB `+0x860` → `+0x0 +0x4 +0x8 +0x14` · TEB `+0x808` · `+0x81c` | `+0x148 ~ +0x20a` (`+0x162 JZ → +0x250`) |
| **E7** | **`_PeekMessage` → `NtUserPeekMessage` (`0x1001`)** | 매 바퀴 | **봄 (0 / 0 아님만)**. 0 → E11 빈 틈 | MSG 를 스택 `[RSP+0x50]` 0x30 B (`+0x39 ~ +0x41` 0 채움) 에 받음 | `+0x250 ~ +0x272` · `_PeekMessage +0x38 ~ +0x63` |
| E8 | `message == WM_QUIT (0x12)` | ≠0 일 때 | 맞으면 `NtUserPostQuitMessage` → **빠짐** (반환 = `pdlg+0x20`) | MSG `+0x8` · `+0x10` | `+0x274 ~ +0x283 → +0x379` · `+0x37d` |
| E9 | `IsDialogMessageW(hwnd, &msg)` | 사본 `+0x10` 비트 5 = 0 | ≠0 → 번역 · 배달 건너뜀 | — | `+0x289 · +0x29b/+0x2a0 · +0x2a2 JNZ → +0x2ba` |
| E10 | `IsDialogMessageW` 안: `ValidateHwndNoRip` · `MessageTable & 0x200` 거부 · `CallMsgFilterW` · `msg.hwnd` 검사 (0 → 0, 자손이면 `+0x30` 사슬) · 메시지 갈래 | — | `WM_COMMAND` 는 기본 갈래 → `TranslateMessage` + `DispatchMessageWorker` → **TRUE** | `MessageTable[0x111] = 0` · `[0x110] = 0x600` · `[0x87] = 0x615` (`0x200` → 큐로 주면 거부) · TEB `+0x820`→`+0x10` · `+0x838` | `+0x22 ~ +0x13b` · `CallMsgFilterW +0x26 ~ +0x66` |
| E11 | **빈 틈** (E7 이 0): ① 안 보임 → `NtUserShowWindow(hwnd,1)` + `UpdateWindow` (+ `NotifyWinEvent(0x10)`) ② 보임 · 소유자 ≠ 0 · 이번 틈 처음 → `WM_ENTERIDLE` ③ 그 밖 (표본) → `HMValidateHandleNoSecure` · `+0x2a & 0xc000` · **`NtUserWaitMessage` (`0x100a`)** → 맨 위 | — | **셋 다 반환 안 봄** | 사본 `+0x1f` · `+0x2a` · `[RSP+0xb0]` (이번 틈 표시) | `+0x20a ~ +0x24b` · `+0x2d2 ~ +0x31e` · `+0xd3 ~ +0x119` |
| E12 | `DispatchMessageWorker(&msg, 0)` | 늘 | — | `& 0xfffe0000` 거부 · `ValidateHwnd` · **msg.hwnd 를 사본 `+0x0` 으로 되씀** (`+0x4f`) · `MessageTable & 0x200` 거부 · **사본 `+0x12` 비트 2 → `NtUserDispatchMessage`** · 비트 3 (ANSI) 다르면 변환 · 같으면 `UserCallWinProcCheckWow([사본+0xe0], [사본+0x78], …)` | `+0x22 ~ +0x1fc` |
| E13 | `DefDlgProcW` → `DefDlgProcWorker(사본, 0x111, ID, lParam, 0)` | 사본 `+0x2a` ∈ {0, `0x2a4`} | **`0x2a4` 도 0 도 아니면 앱 프로시저 안 부르고 0** (`+0x45 JNZ → +0x211`) | `[여분+0x0]` ≠ 0 이면 `_SetWindowLongPtr(h,0,0,0)` · `[pdlg+0x0]` (0 이면 앱 호출 건너뜀 `+0x9a`) · 사본 `+0xe0` | `DefDlgProcWorker +0x32 ~ +0xec` |
| E14 | **앱 프로시저**: `WM_COMMAND` → `EndDialog(hDlg, n)` → TRUE | 앱 몫 | — | — | — |
| **E15** | **`EndDialog`**: `ValidateHwnd` · `ValidateDialogPwnd` · `GetActiveWindow` · **`pdlg+0x18 \|= 1` · `pdlg+0x20 = n`** · `NtUserSetWindowPos(…,0x97)` (`0x1021`) · `NtUserPostMessage(hDlg,0,0,0)` (`0x100d`) · 반환 1 | 활성 · 포커스 · 소유자에 따라 `NtUserSetFocus` · `NtUserSetActiveWindow` · `NtUserZapActiveAndFocus` · `NtUserEnableWindow` 더함 | **전부 반환 안 봄** | `GetActiveWindow` = `[[TEB+0x860]+0x28]` · `GetFocus` = `[[TEB+0x860]+0x20]` (TEB `+0x860` = 0 이면 둘 다 0 → syscall 둘만) · 사본 `+0x1f` · `+0x30` · `+0x40` | `EndDialog +0x1f ~ +0xe5 · +0x106 ~ +0x18a` |
| E16 | `DefDlgProcWorker` 돌아옴 | — | 앱 반환 ≠0 → `[여분+0x0]` (`DWLP_MSGRESULT`) 반환 · 0 → `DefWindowProcWorker`. **`DispatchMessageWorker` · `IsDialogMessageW` 는 이 반환을 안 봄** | 사본 `+0x12` 비트 0 | `+0x143 ~ +0x1f7` |
| E17 | 고리로: 보임이면 곧장, 아니면 `0x113`/`0x118` → E11 ①, `0x104` → 빈 틈, 그 밖 곧장 → `HMValidateHandleNoSecure` → E5 | 매 바퀴 | 0 → **빠짐 (반환 0)** | — | `+0x2ba ~ +0x2cc · +0x45e ~ +0x474 · +0x119 ~ +0x124` |
| **E18** | **빠짐**: (`gpsi+0x764 & 0x8000` 이면 `NotifyWinEvent(0x11)`) · `HMValidateHandleNoSecure` (0 → 반환 0) · **`RSI = [pdlg+0x20]` 먼저 읽음** (`pdlg` NULL 이면 0) | — | NotifyWinEvent 반환 안 봄 | `gpsi+0x764` · 사본 `+0x128` · `pdlg+0x20` | `+0x389 ~ +0x3bd · +0x42d` |
| **E19** | **`NtUserDestroyWindow(hwnd)` (`0x1095`)** | 늘 | **안 봄** (`+0x3cb NOP`) | — | `+0x3c4/+0x3cb` |
| E20 | 소유자 있으면 `GetThreadState(0xa)` → `QueryWindow(소유자,7)` → `SetForegroundWindow` | 소유자 ≠ 0 (표본 0) | 앞 둘은 **봄** (갈래만) | — | `+0x3d0 ~ +0x3ea · +0x432 ~ +0x45a` |
| E21 | **반환 `RAX = RSI`** → `InternalDialogBox` → `DialogBoxParamW` | — | 좁히기 없음 | — | `+0x3ea ~ +0x40d` · `InternalDialogBox +0x8f ~ +0xad` |

**한 번의 syscall (가장 짧은 꼴)**: `NtUserPeekMessage` ×1 → (`IsDialogMessageW` ~ 앱 프로시저는 0번) → `NtUserSetWindowPos` · `NtUserPostMessage` (`EndDialog`) → `NtUserDestroyWindow` = **4번**. 첫 `NtUserPeekMessage` 가 0 이면 그 앞에 `NtUserShowWindow` (안 보일 때) 또는 `NtUserWaitMessage` 가 더.

### 2. (a) Enter 가 고리에 오는 모양 — `IsDialogMessageW` 의 `WM_KEYDOWN` 길 [정적]

- `IsDialogMessageW +0xf2 ~ +0x103`: `message − 0x100 == 0` (`WM_KEYDOWN`) → `+0x175`. `SendMessageW(msg.hwnd, 0x87, wParam, &msg)` (`+0x184`) 답에 `0x4` (`DLGC_WANTALLKEYS` `[추측: 이름]`) 가 있으면 그냥 번역 · 배달 (`+0x18e JNZ → +0x129`).
- 없으면 wParam 뺄셈 사슬 (`+0x190 ~ +0x1ac`): `3` → 조각 `+0x369` (IDCANCEL 길, `EBP = 2`) · `9` (탭) → 덩어리 `+0x98b6e` · **`0xd` → `+0x1ea`** · `0x1b` → 조각 `+0x369`.
- `VK_RETURN` 갈래: `GetFocus` (`+0x1ea`) — TEB `+0x860` = 0 이면 0 → ② 로. 포커스가 있고 `SendMessageW(포커스, 0x87)` 의 `0x10` (`+0x211`, `DLGC_DEFPUSHBUTTON` `[추측: 이름]`) 이 켜졌으면 ① `GetDlgCtrlID(포커스)` (`+0x21d`) → 조각 `+0x98c0f`. 아니면 ② `SendMessageW(hDlg, DM_GETDEFID 0x400)` (`+0x98b4c`) → 윗 16비트 `0x534b` 면 아랫 16비트 (`+0x98b5c JZ → +0x98b66`), 아니면 `1` (`+0x98b61`) → 조각 `+0x369 _FindDlgItem`.
- 조각 `+0x98c0f`: 못 찾음 → `+0x98c12 JZ → +0x98b0a` → `SendMessageW(hDlg, 0x111, ID, RBX)` · 찾음 + `+0x1f` 비트 3 → `NtUserMessageBeep(0)` (`+0x98c24`) 후 TRUE (`WM_COMMAND` 안 보냄) · 찾음 + 멀쩡 → `+0x98b07 RBX = [컨트롤 사본]` (hwnd) → **`+0x98b19 SendMessageW(hDlg, 0x111, ID, 컨트롤 hwnd)`** → `+0x98b1f JMP +0x13b` TRUE.
- **진짜 입력에서 `WM_COMMAND` 가 큐로 오는 길도 있다**: `DefDlgProcWorker +0x448 ~ +0x46c` — 앱이 `WM_CLOSE (0x10)` 에 FALSE 를 돌려주면 `PostMessageW(hDlg, 0x111, 2, 그 컨트롤)`. 그러니 "`NtUserPeekMessage` 가 `message = 0x111` 을 주는 꼴" 은 진짜 흐름에도 있다 (키 길과 달리 큐로).
- 마우스 클릭은 컨트롤 프로시저가 `WM_COMMAND` 를 **보냄** (`xxxButtonNotifyParent` — 이름만, 본문 안 읽음 `[공백]`).

### 3. (b) `WM_COMMAND` 를 곧장 줬을 때 — syscall 0 의 조건 [정적]

| 칸 | 있어야 할 값 | 어긋나면 | 줄 |
|---|---|---|---|
| MSG `+0x0` hwnd | 모달 대화상자 hwnd (또는 그 자손 — 컨트롤은 `+0x1f` 비트 6 · `+0x30` 사슬) | 0 → `IsDialogMessageW` 0 · `DispatchMessageWorker` 0 → 아무 일 없이 고리만 (F2) | `IsDialogMessageW +0x9f / +0xa2` · `+0xc3 ~ +0xf0` |
| MSG `+0x8` message | `0x111` | `& 0xfffe0000` → `SetLastError(0x57)` · `MessageTable & 0x200` → `SetLastError(0x487)` | `DispatchMessageWorker +0x22` · `IsDialogMessageW +0x6c / +0x70` |
| 사본 `+0x12` 비트 2 | 0 | 켜지면 `NtUserDispatchMessage` (`0x1031`) | `DispatchMessageWorker +0xb6` |
| 사본 `+0x2a` (fnid) | 0 또는 `0x2a4` | 다른 값 → 앱 프로시저 안 부름, 0 | `DefDlgProcWorker +0x32 ~ +0x45` |
| 사본 `+0x78` | `DefDlgProcW` (run146) | — | `DispatchMessageWorker +0x1d8` 인자 |
| `[[TEB+0x820]+0x10] \| [TEB+0x838]` | 0 | ≠0 → `NtUserCallMsgFilter` (`0x1013`, ≠0 반환이면 TRUE 로 끝) | `CallMsgFilterW +0x26 ~ +0x66` |
| `gcCallUserApiHook` · `ghmodUserApiHook` · `gfUserApiHook` · `guah+0x70 ~ +0x88` | 0 (`gcCallUserApiHook` 는 LOCK 증감이라 쓰기 가능 `.data`) | 훅 갈래 | `UserCallDlgProcCheckWow +0xcf ~ +0x14e` · `DefWindowProcWorker +0x0` |
| `word [msg+0x10]` | `0xe5` 아님 (ID ≠ 229) | `0xe5` 면 IMM 훅 `[user32+0xd96f0]` 부름 `[공백: 훅 몸 안 읽음]` | `TranslateMessage +0x12` · `+0x71` |

- `UserCallDlgProcCheckWow` 의 반환 EAX 는 (훅 없으면) 늘 0 이고, 앱의 DLGPROC 반환은 출력 포인터 `[rbp-0x28]` 로 받는다 (`DefDlgProcWorker +0xc4` · `+0xec`).
- `WM_COMMAND` 는 `DefDlgProcWorker` 의 앱 호출 뒤 개별 갈래가 없다 — 앱이 FALSE 면 `+0x185 DefWindowProcWorker` → `RealDefWindowProcWorker` 사용자 모드 점프표 `0x111` = `return 0` (`+0x56`), 단 비트표 (커널이 채움) 가 커널로 보내라 하면 `gapfnScSendMessage[0]` 길 `[공백]`.

### 4. (c) 끝 · 반환 — 반환을 보는 / 안 보는 syscall [정적]

| syscall | 부르는 곳 | 반환을 보나 | 0 · 아무 값이면 |
|---|---|---|---|
| `NtUserPeekMessage` `0x1001` | `_PeekMessage +0x38` | **봄 (0 / 0 아님)** | 0 = 빈 틈 (E11) |
| `NtUserWaitMessage` `0x100a` | `DialogBox2 +0x23f` | 안 봄 | 바쁜 고리 (F1) |
| `NtUserShowWindow` `0x1052` · `UpdateWindow` · `NtUserNotifyWinEvent` `0x1028` | `DialogBox2 +0x2da · +0x2e9 · +0x312 · +0x41c` | 안 봄 | 같음 |
| `NtUserPostQuitMessage` `0x14ee` | `DialogBox2 +0x37d` | 안 봄 | 같음 |
| `NtUserSetWindowPos` `0x1021` · `NtUserPostMessage` `0x100d` · `NtUserSetActiveWindow` `0x10cd` · `NtUserZapActiveAndFocus` `0x15d6` · `NtUserSetFocus` `0x104c` · `NtUserEnableWindow` `0x1416` | `EndDialog +0xa9 · +0xd9 · +0xc2 · +0x113 · +0x145 · +0x17e` | 안 봄 (호출 뒤 `NOP`) | 같음 |
| **`NtUserDestroyWindow` `0x1095`** | **`DialogBox2 +0x3c4`** (+ run146 `InternalCreateDialog +0xc01` 컨트롤 실패) | 안 봄 | 같음 |
| `NtUserGetThreadState(0xa)` `0x1000` · `NtUserQueryWindow` `0x100e` | `DialogBox2 +0x3da · +0x43a` | **봄** (소유자 있을 때만) | 표본은 소유자 0 이라 안 탐 |
| `NtUserSetForegroundWindow` `0x155b` | `DialogBox2 +0x44e` | 안 봄 | — |
| `NtUserMessageBeep` `0x14dd` | `IsDialogMessageW +0x9885e · +0x98c24` · `DefDlgProcWorker +0x913` | 안 봄 | — |
| `NtUserCallMsgFilter` `0x1013` | `CallMsgFilterW +0xb4` | **봄** (≠0 → `IsDialogMessageW` TRUE) | 훅 0 이면 안 탐 |
| `NtUserSetWindowLongPtr` `0x15da` | `_SetWindowLongPtr +0x26c` · (`DefDlgProcWorker +0x207` 경유) | **앱이 옛 값을 쓰면 봄** (user32 안 호출자는 안 봄) | 옛 값을 돌려줘야 맞음 |
| `NtUserMessageCall` `0x1005` | `RealDefWindowProcWorker +0xc6` (비트표) · `_GetWindowLongPtr +0x260` (항목 `+0x8` ≠ TEB `+0x78`) | 반환 그대로 줌 | 0 으로 충분 |
| `NtUserGetMessage` `0x1004` | **`DialogBox2` 는 안 부름.** 앱 자기 루프 · `GetMessageW +0x1b` | 그대로 돌려줌 (0 = `WM_QUIT` → 앱 루프 끝 · −1 = 오류 → `while (GetMessage())` 면 영원히) | **−1 은 주지 말 것** |

- `WM_ENTERIDLE (0x121)` 은 소유자 ≠ 0 일 때만 (E11 ②) — 표본 80/80 이 부모 0 이라 안 탄다.
- **`DialogBoxParamW` 반환 = `pdlg+0x20`** — `EndDialog` 가 쓴 `n` (64비트), 안 불렸으면 처음 값 (`InternalCreateDialog +0x6fa` 의 `1`, 컨트롤 고리가 `DLGC_DEFPUSHBUTTON` 컨트롤 ID 로 바꿀 수 있음, run146). 0 이 되는 건 hwnd 가 사라졌을 때 (E17 · E18) 뿐.

### 5. 금지 꼴 — 끝없는 고리 · 접근 위반 [정적]

빠지는 길은 E5 `pdlg+0x18` 비트 0 · `pdlg` NULL · hwnd 사라짐 (E3 / E11 / E17) · `WM_QUIT` (E8) **넷뿐**. 그 밖에 횟수 · 시간 한도는 없다.

| # | 꼴 | 왜 (줄) | 권하는 처리 (결과서) |
|---|---|---|---|
| **F1** | `NtUserPeekMessage` 늘 **0** + `NtUserWaitMessage` 즉시 0 (**지금 에뮬레이터 기본 대답**) | `+0x272 JZ → +0x20a` → (보이고 소유자 0) `+0x23f` → `+0x24b JMP +0x119` → 맨 위. 반환 둘 다 안 봄 → **바쁜 고리** | `NtUserWaitMessage` 가 이 길에서 불리면 진단 정지 |
| F2 | 1 을 주는데 MSG 를 안 채움 (hwnd 0 · message 0) | `IsDialogMessageW +0xa2 JZ → +0x142` 0 → `TranslateMessage` 0 → `DispatchMessageWorker +0x35 JZ → +0x29a` 0 → 맨 위 | 채우기 |
| F3 | hwnd 가 `ValidateHwnd` 0 (항목 · TEB `+0x820` / `+0x828` 어긋남) | `DispatchMessageWorker +0x46 JZ → +0x16b` 0 → F2. `HMValidateHandleNoSecure(hDlg)` 도 0 이면 **빠짐 (반환 0)** — 뜻밖에 빠질 수 있음 | run146 칸 맞추기 |
| F4 | `message & 0xfffe0000` | `+0x587 SetLastError(0x57)` 0 → F2 | 하지 말 것 |
| F5 | `MessageTable & 0x200` 메시지 (`WM_INITDIALOG 0x110` · `WM_GETDLGCODE 0x87` 등) 를 큐로 줌 | `IsDialogMessageW +0x70 JNZ → +0x272 → +0x27a SetLastError(0x487)` 0 · `DispatchMessageWorker +0x71 JNZ → +0x46b` / `+0x87 JNZ → +0x356` 0 → F2 | 이런 건 보내기로만 |
| F6 | msg.hwnd 가 대화상자도 자손도 아님 | `IsDialogMessageW +0xd7 JNZ → +0x142` 0 → 고리가 직접 번역 · 배달 → 컨트롤 프로시저 (사본 `+0x12` 비트 2 면 `NtUserDispatchMessage` 0) → F2 | msg.hwnd = 대화상자 |
| **F7** | 앱이 `EndDialog` 를 안 부름 (입력 검사 거절 · 다른 ID 만 처리) + stub 이 같은 메시지를 계속 줌 | 매 바퀴 1 → 무시 → 맨 위 | 횟수 한도 + 메시지 바꾸기 `[추측: 정책]`: 기본 ID → `IDCANCEL (2)` → `WM_CLOSE (0x10)` (앱이 FALSE 면 `DefDlgProcWorker +0x467 PostMessageW(…0x111, 2…)` — 큐가 있어야 다시 나옴) → `WM_QUIT` (E8, 반환 `pdlg+0x20`, 앱 메인 루프도 `NtUserPostQuitMessage` 로 끝나게 됨) |
| F8 | 기본 단추가 꺼진 컨트롤인데 `WM_KEYDOWN(VK_RETURN)` 길로 줌 | `IsDialogMessageW +0x98c18` → `NtUserMessageBeep` 후 TRUE, `WM_COMMAND` 안 보냄 → F7 | `WM_COMMAND` 를 곧장 줌 |
| F9 | `pdlg` NULL (사본 `+0x128` 비었거나 `[+8]` NULL) | `DialogBox2 +0x83 / +0x87` 접근 위반 (고리 안 `+0x138 JZ` 는 그 뒤에만) | run146 stub 5 (`NtUserSetDialogPointer`) |
| F10 | `NtUserDestroyWindow` 뒤 `pdlg` · 여분 블록을 풀거나 재사용 | `+0x3bd` 에서 **이미 읽은** 뒤라 괜찮음. `WM_NCDESTROY` 를 안 보내면 `pdlg` 가 안 풀릴 뿐 (`DefDlgProcWorker +0x31a ~ +0x3ae`, 누수 · 무해) | — |
| F11 | 항목 `+0x8` ≠ TEB `+0x78` | `GetWindowLongPtrW(hDlg, 0x10)` 이 `_GetWindowLongPtr +0x1e6 JNZ → +0x260` `NtUserMessageCall` 로 감 → stub 0 → 앱의 `DWLP_USER` 포인터 NULL → 앱 쪽 널 읽기 `[추측]` | run146 의 TEB `+0x78` 규칙 지키기 |

### 6. (d) A 판 · 모달 아닌 판이 갈라지는 줄 [정적]

| 자리 | `DialogBox*W` | `DialogBoxIndirectParamA` / `DialogBoxParamA` | `CreateDialogParam*` |
|---|---|---|---|
| 깃발 넘김 | `DialogBoxParamW +0x5d AND [RSP+0x28],0` | `DialogBoxIndirectParamA +0x9 MOV [RSP+0x28],2` | `CreateDialogParamW +0x74 MOV [RSP+0x28],EBX(0)` · `CreateDialogParamA +0x5b … ,2` |
| 깃발 가공 | `DialogBoxIndirectParamAorW +0x4a AND EAX,2 · +0x50 OR EAX,1` | 같음 → 3 | `CreateDialogIndirectParamAorW +0x33 · +0x39` → W 1 · A 3 |
| 다음 | `InternalDialogBox` → `InternalCreateDialog` → **`DialogBox2` (고리)** | 같음 | **`CreateDialogIndirectParamAorW +0x52 InternalCreateDialog` 에서 끝** (고리 · 소유자 끄기 없음) |
| 자원 없을 때 | −1 (`DialogBoxParamW +0x39 JZ`) | −1 | **0** (`CreateDialogParamW +0x37 JNZ` 안 타면 0) |
| `InternalCreateDialog` 안 | 깃발 비트 1 = 0 | `+0x5da MOV R14D,[RBP+0x8]; AND R14D,2` → **확장 스타일 `\| 0x80000000`** (`+0x5e5 ~ +0x5f4`, `+0x5ff OR ECX,ESI`) · **`+0x713 / +0x72c pdlg+0x18 \|= 4`** | 같은 규칙 |
| 보낼 때 | — | `SendMessageWorker +0x14c ~ +0x16f` (창 ANSI 비트 ↔ 보내는 쪽): 같으면 `+0x16f JZ → +0x28f`. 다르면 점프표 (`+0x175 ~ +0x1a2`) · `0x140` (`+0x17d`) · `0x286` (`+0x1f2`) · `≥ 0x400` (`+0x1fa JNC → +0x28f`) · 기본 `+0x200 → +0x214 TEST [MessageTable+msg*2], 0x100` 꺼짐 → `+0x21d JZ → +0x29c` (변환 · syscall 없이) | — |
| 변환이 도는 메시지 | — | 글자 갈래 `+0x225`: `0x2f 0xcc 0x102 0x103 0x106 0x107 0x10f 0x120` · `0x286`. `+0x1a8` 갈래: `0xb0 0xb1` | — |
| `DefDlgProcA` | — | `+0x24 MOV [RSP+0x20],1` → `DefDlgProcWorker(…, bAnsi=1)` — `bAnsi` 는 `DefWindowProcWorker` 인자로만 (앱 프로시저는 날 메시지) | — |
| 사본 `+0x12` 비트 3 (ANSI) 를 켜는 쪽 | — | 커널 `[추측]` (win32kfull `xxxCreateWindowEx +0xa3a` `0x208` 등 — 눈으로만 훑음, 다음 요청) | — |

- **결론**: 끝 길 (E9 ~ E21) 은 A 판도 같다. 달라지는 건 만들 때 확장 스타일 · `pdlg+0x18` 비트 2 · 사본 `+0x12` 비트 3 (커널) · 글자 메시지 변환뿐. `WM_INITDIALOG` · `WM_COMMAND` · `WM_SETFONT` · `WM_GETDLGCODE` · `DM_*` 만 쓰는 대화상자는 변환이 없다.

### 7. 앱 프로시저가 거의 늘 부르는 것 (sast/142 11절) [정적]

| API | 하는 일 | syscall | 줄 |
|---|---|---|---|
| `GetDlgItem(hDlg, id)` | `ValidateHwnd` → 사본 `+0x38` (첫 자식, `− [자기+8] + 자기` 변환) → `+0x140 == id` 면 `[사본+0]` hwnd, 아니면 `+0x48` (다음 형제). 못 찾으면 `SetLastError(0x58d)` · 0 | **0** | `GetDlgItem +0x8 · +0x1e · +0x27 ~ +0x2b · +0x38 · +0x40 · +0x67 · +0x7f` |
| `_FindDlgItem` | `_GetDlgItem` 먼저, 못 찾으면 `_NextControl` 로 `WS_EX_CONTROLPARENT` 안까지 | 0 | `_FindDlgItem +0x14 · +0x33 ~ +0x6c` (결과서) |
| `GetWindowLongPtrW(hDlg, 0x10)` | TEB `+0x840/+0x848` 캐시 · 항목 검증 → `_GetWindowLongPtr` : `_GETPTI == PtiCurrent` (항목 `+0x8` == TEB `+0x78`) 면 **`[[사본+0x128]+0x10]`** 사용자 모드 | 0 (어긋나면 `NtUserMessageCall` code `0x5a`) | `_GetWindowLongPtr +0x1d6 · +0x1e6 · +0x1ef` |
| `GetWindowLongPtrW(hDlg, 0)` | `[[사본+0x128]+0x0]` (`DWLP_MSGRESULT`) | 0 | `+0x16e · +0x184` |
| 음수 인덱스 | `GetWindowData`: `−21` = `[사본+0xd8]` · `−6` = `[+0x20]` · `−12` = 자식이면 `[+0x140]` · `−16` 스타일 · `−20` 확장 스타일 · `−8` 부모 / 소유자 | 0 | `GetWindowData +0x57 · +0xc3` (결과서 `+0x0 ~ +0x27c`) |
| **`SetWindowLongPtrW(hDlg, 0x10 / 0, v)`** | `_SetWindowLongPtr(h, i, v, 0)`. **`g_systemCallFilterId ≠ 5` 면 syscall** (`+0x6a CMP · +0x71 JNZ → +0x261` · 인덱스 0 은 `+0x4a → +0x14d · +0x154 → +0x174 · +0x1c2 / +0x1c9 JNZ → +0x261`), 반환 = 옛 값. `== 5` 면 사용자 모드 (`+0x87 ~ +0x92` · `+0x162 ~ +0x16c`) | **`NtUserSetWindowLongPtr` (`0x15da`)** | `+0x26c / +0x273` |
| `SetWindowLongPtrW(hDlg, DWLP_DLGPROC 8, p)` | 사용자 모드로 `pdlg+0x0 = p`, `pdlg+0x18` 비트 2 를 `bAnsi` 로 (다르면 `GetCPD` 변환 thunk) | 0 | `+0x9b ~ +0x12d · +0x144` |
| `GetDlgCtrlID` | 캐시 · 항목 검증 → `[사본+0x140]` | 0 | `GetDlgCtrlID +0x14 · +0x3f` |
| `GetParentDialog` | `+0x8 TEST [+0x12],1` 켜졌으면 (`+0x1a` 비트 0 · `+0x1d` 비트 2 보고) 그 창, 아니면 `+0x1f` 비트 6 이고 `+0x30` 있으면 한 단계 위로 | 0 | `GetParentDialog +0x8 ~ +0x39` |
| `WM_GETDLGCODE` 가 앱에서 0 | `DefDlgProcWorker +0x143 TEST R13` → … → `+0x185 DefWindowProcWorker` → `RealDefWindowProcWorker +0x46 CMP [0xda568]` → 비트 `[0xda570]` 켜지면 `MessageTable[0x87] & 0xff = 0x15` → **`gapfnScSendMessage[0x15]` = `NtUserMessageCall` 입구** (fnid `0x29e`), 아니면 점프표 `return 0` (`+0x56`) | 0 또는 **`NtUserMessageCall` (`0x1005`) 1번** — 비트표는 커널이 채움, 정적으로 못 정함 | `RealDefWindowProcWorker +0x46 ~ +0xcd` |
| `DlgSetFocus` | `SendMessageW(h, 0x87)` → `& 8` 이면 `SendMessageW(EM_SETSEL)` → `JMP [NtUserSetFocus]` | **`NtUserSetFocus` (`0x104c`) 1번** (꼬리, 반환 안 씀) | `DlgSetFocus +0x14 · +0x19 · +0x2e · +0x3b` |

- `g_systemCallFilterId` (user32 전역 `0xd9970`) 가 5 인 때가 어떤 때인지 · 누가 정하는지는 이번에 안 따라감 `[공백]` — 결과서는 "보통 5 가 아님" 이라 적었고, 그 "보통" 은 줄 근거가 없다 `[추측]`.

### 8. 칸 표 보탬 — run146 6절에 없는 칸만 [정적, 이름은 `[추측]`]

**MSG (0x30 B, `NtUserPeekMessage` 가 채움)**

| 칸 | 크기 | 읽는 줄 | 쓰는 줄 | 있어야 할 값 |
|---|---|---|---|---|
| `+0x00` hwnd | 8 | `IsDialogMessageW +0x9f / +0xa8` · `DispatchMessageWorker +0x2f` · `+0x1cb` | `DispatchMessageWorker +0x4f` (사본 `+0x0` 으로 되씀) | **0 아님** — 모달 대화상자 hwnd |
| `+0x08` message | 4 | `DialogBox2 +0x274 · +0x2c3` · `IsDialogMessageW +0x50 · +0xf2` · `DispatchMessageWorker +0x22 · +0x54` · `TranslateMessage +0x18` · `_PeekMessage +0x72` | — | `0x111` |
| `+0x10` wParam | 8 | `DialogBox2 +0x379` · `IsDialogMessageW +0x175 · +0x190` · `TranslateMessage +0x12` · 앱 | `_PeekMessage +0x80` (`0xcc` / `0x102` 면 `& 0xffff`) · `DispatchMessageWorker +0x1f5` | `MAKEWPARAM(ID, 0)` = ID (`0xe5` 아님) |
| `+0x18` lParam | 8 | `DispatchMessageWorker +0x1b5` · 앱 | — | 그 ID 의 컨트롤 hwnd (없으면 0) |
| `+0x20` ~ `+0x2f` | — | 이 길에선 안 읽음 | — | 0 가능 |

**창 사본 (`pwnd`)**

| 오프셋 | 이름 `[추측]` | 읽는 줄 | 값 |
|---|---|---|---|
| `+0x10` 비트 5 | 깃발 | `DialogBox2 +0x289` | 0 (켜지면 Ctrl+C 처리 `+0x32e ~ +0x362`) |
| `+0x1a` 비트 0 | `WS_EX_CONTROLPARENT` | `IsDialogMessageW +0xb0` · `GetParentDialog +0x28` | 0 |
| `+0x1f` 비트 3 | `WS_DISABLED` | `IsDialogMessageW +0x98c18` · `DefDlgProcWorker +0x90b` · `InternalDialogBox +0x119` (소유자) | 컨트롤: 0 |
| `+0x1f` 비트 4 | `WS_VISIBLE` | `DialogBox2 +0x96 / +0xa4` | 0 가능 (0 이면 첫 빈 틈에 `ShowWindow`) |
| `+0x1f` 비트 6 · 7 | `WS_CHILD` / `WS_POPUP` | `EndDialog +0x4b ~ +0x52` · `IsDialogMessageW +0xd0` · `GetParentDialog +0xe` | 대화상자 비트 6 꺼짐 · **컨트롤 비트 6 켜짐** |
| `+0x2a` (fnid, run146) | — | **`DefDlgProcWorker +0x32 · +0x41`** | **0 또는 `0x2a4`** — 아니면 앱 프로시저 안 부름 |
| `+0x30` | 부모 | `GetParentDialog +0x14` · `IsDialogMessageW +0xd9` · `EndDialog +0x58` | 컨트롤: 대화상자 사본. 대화상자: 0 |
| `+0x38` | 첫 자식 | `GetDlgItem +0x1e` · `_NextControl` (run146) | 첫 컨트롤 `[추측: 템플릿 차례 — 커널 연결 순서는 win32k 몫]` |
| `+0x40` | 소유자 | `EndDialog +0x121` | 0 (표본) |
| `+0x48` | 다음 형제 | `GetDlgItem +0x40` | 마지막 컨트롤 0 |
| `+0xd8` | `GWLP_USERDATA` | `GetWindowData +0xc3` | 쓰기는 syscall (커널이 씀 `[추측]`) |
| `+0x140` (run146) | 컨트롤 ID | `GetDlgItem +0x38` · `GetDlgCtrlID +0x3f` · `GetWindowData +0x57` | 컨트롤마다 ID |

**여분 블록 (`[사본+0x128]`) · `pdlg`**

| 칸 | 읽는 줄 | 쓰는 줄 | 뜻 |
|---|---|---|---|
| 여분 `+0x00` | `DefDlgProcWorker +0x62 · +0x1f7` · `_GetWindowLongPtr +0x184` | `DefDlgProcWorker +0x207` (→ 보통 syscall) · `_SetWindowLongPtr +0x16c` (`== 5` 일 때만) | `DWLP_MSGRESULT`, 0 으로 시작 |
| 여분 `+0x10` | `_GetWindowLongPtr +0x1ef` | **보통 syscall `0x15da` (커널)** · `_SetWindowLongPtr +0x92` (`== 5` 일 때만) | `DWLP_USER`, 0 으로 시작 |
| `pdlg+0x00` | `DefDlgProcWorker +0x9c` | `_SetWindowLongPtr +0x12d` (`DWLP_DLGPROC`) | 앱 프로시저 |
| `pdlg+0x18` | `DialogBox2 +0x87 · +0x13e` · `EndDialog +0x16c` · `DefDlgProcWorker +0x598 · +0x684 · +0x9fa · +0xa37` · `_SetWindowLongPtr +0xc6` | **`DialogBox2 +0x8f` (비트 1)** · **`EndDialog +0x70` (비트 0)** · `_SetWindowLongPtr +0x144` (비트 2) | 비트 0 끝남 · 비트 1 소유자가 이미 꺼져 있었음 (켜져 있으면 `EndDialog` 가 `NtUserEnableWindow` 안 함) · 비트 2 ANSI |
| `pdlg+0x20` | **`DialogBox2 +0x3bd` (반환값)** · `DefDlgProcWorker +0xa4b` (`DM_GETDEFID`) · `+0x4e5` | **`EndDialog +0x7f` (64비트 결과)** · 조각 `+0x9799c` (`DM_SETDEFID`) · `InternalCreateDialog +0x6fa` (처음 `1`) | **기본 단추 ID 겸 결과값** |

**TEB · `gpsi` · user32 전역**

| 칸 | 읽는 줄 | 뜻 |
|---|---|---|
| TEB `+0x860` | `GetActiveWindow +0x9` (`[+0x28]`) · `GetFocus +0x9` (`[+0x20]`) · `DialogBox2 +0x15b` · `PeekMessageW +0x43` · `UserCallDlgProcCheckWow +0x3a ~ +0x53` | **0 이면 전부 "없음"** — 활성 0 · 포커스 0 · 빠른 길 안 탐. 가리키는 곳 모양 `[추측: 스레드 입력 상태 블록]` |
| TEB `+0x898` · `+0x89c` · `+0x8a0 ~ +0x8cf` | `DispatchMessageWorker +0x2b5 · +0x428 · +0x4f6` · `_PeekMessage +0x8a` | DBCS 때만, 0 가능 |
| `gpsi+0x764` | `DialogBox2 +0x2f5 · +0x390` | `0x8000` 이면 `NotifyWinEvent` 둘. 0 가능 |
| `gpsi+0x88c` 비트 0 · `+0x1b50` · `+0x1b5c` | `DialogBox2 +0xaf` · `GetCapture +0x7` · `IsDialogMessageW +0x98b9b ~ +0x98bb6` | 0 이면 건너뜀 |
| `g_systemCallFilterId` (`0xd9970`) | `_PeekMessage +0x65` · `_SetWindowLongPtr +0x6a · +0x14d · +0x1c2` · `GetWindowData +0xe5` · `GetMessageW +0x27` | ≠5 → `SetWindowLongPtr` 는 syscall. =5 → 훅 갈래 · 사용자 모드 쓰기 `[공백: 값 정하는 쪽]` |
| `gcCallUserApiHook` (`0xda734`) · `ghmodUserApiHook` (`0xda7c0`) · `gfUserApiHook` (`0xda730`) | `UserCallDlgProcCheckWow +0xcf ~ +0x14e` · `DefWindowProcWorker +0x0` | 0 이어야 (첫 것은 LOCK 증감) |
| 비트표 `user32+0xda568` (한도) · `+0xda570` (포인터) · `+0xda578` · `+0xda580` | `RealDefWindowProcWorker +0x46 · +0x4e · +0x76 · +0xcd` | user32 는 안 씀 (refs WRITE 0), 커널이 채움 `[공백: 값]`. **두 한도가 0 이면 어떤 메시지든 syscall 없이 0** (`+0x56`) |

### 호출 관계

```
user32!InternalDialogBox +0x8f DialogBox2(hwnd, 소유자 0, 꺼짐 0, …)                                 [정적]
 ├─ +0x53 ValidateHwnd (0 → −1) · +0x64 GetCapture · +0x6d ~ +0xce pdlg+0x18 비트1 · 사본 +0x1c/+0x1f
 ├─ 고리 맨 위 +0x12a ~ +0x142  [[사본+0x128]+8] NULL / +0x18 비트0 → +0x389 빠짐
 │   ├─ +0x270 _PeekMessage +0x3f NtUserPeekMessage ── SYSCALL 0x1001        반환 0 / 0 아님만
 │   │   0 → 빈 틈: (안 보임) +0x2e1 NtUserShowWindow · +0x2ee UpdateWindow
 │   │             (보임·소유자 0) +0x227 HMValidateHandleNoSecure · +0x246 NtUserWaitMessage ── SYSCALL 0x100a → 맨 위
 │   ├─ +0x283 WM_QUIT → +0x384 NtUserPostQuitMessage → 빠짐
 │   ├─ +0x2a0 IsDialogMessageW(hwnd, &msg)
 │   │   ├─ +0x97 CallMsgFilterW (훅 0 → syscall 없이 0)
 │   │   ├─ (WM_KEYDOWN VK_RETURN) +0x1ef GetFocus · +0x98b51 SendMessageW(DM_GETDEFID) ⇢ DefDlgProcWorker +0xa4b pdlg+0x20
 │   │   │   → 조각 +0x373 _FindDlgItem → +0x98b1e SendMessageW(hDlg, WM_COMMAND, ID, 컨트롤)
 │   │   └─ (WM_COMMAND 등 기본) +0x131 TranslateMessage (0x111 → syscall 없이 0)
 │   │                           +0x13b DispatchMessageWorker +0x1dd UserCallWinProcCheckWow
 │   │                                   ⇢ DefDlgProcW → DefDlgProcWorker (fnid 0/0x2a4) +0xc4 UserCallDlgProcCheckWow
 │   │                                     ⇢ 앱 프로시저 → EndDialog(hDlg, n)
 │   │                                        ├─ +0x70 pdlg+0x18 |= 1 · +0x7f pdlg+0x20 = n
 │   │                                        ├─ +0xb0 NtUserSetWindowPos(…, 0x97) ── SYSCALL 0x1021   반환 안 봄
 │   │                                        └─ +0xe0 NtUserPostMessage(hDlg, 0, 0, 0) ── SYSCALL 0x100d   반환 안 봄
 │   └─ +0x121 HMValidateHandleNoSecure (0 → 빠짐, 반환 0) → 맨 위
 └─ 빠짐 +0x389 → +0x3a4 HMValidateHandleNoSecure → +0x3bd RSI = pdlg+0x20
         → +0x3cb NtUserDestroyWindow ── SYSCALL 0x1095   반환 안 봄
         → +0x3ea RAX = RSI → DialogBoxParamW 반환
```

- (나무 오프셋은 ret 자리, 칸 줄은 함수 안 줄. `⇢` 는 CFG 간접 · 보내기. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (결과서 5절 "지금 우리 맞나 · 틀리나")

flow-writer 는 `../PE_emulator-BD/ext/syscall/syscall_layer_cpp/src/gui/` 파일 목록 (19개 — `nt_user_*` · `nt_gdi_*`) 과 `nt_user_get_message.cpp` (26줄, `../PE_emulator` 판과 같은 내용) 를 줄 번호만 훑었다 (읽기만). **`peek_message` · `wait_message` · `destroy_window` · `set_dialog_pointer` · `set_window_pos` · `post_message` · `show_window` · `set_window_long_ptr` 파일은 없다 → 몸 없는 기본 대답 0.** 고칠지는 사용자 · PE_emulator 결정.

| 우리 지금 | 진짜 (정적) | 판정 |
|---|---|---|
| **`NtUserPeekMessage` · `NtUserWaitMessage` 몸 없음 → 기본 대답 0** | `DialogBox2 +0x270 / +0x272` 는 0 / 0 아님만 보고, 0 이면 빈 틈 → (보이고 소유자 0) `+0x23f NtUserWaitMessage` (반환 안 봄) → `+0x24b JMP +0x119` → 맨 위. 횟수 · 시간 한도 없음 | **틀림 — 금지 꼴 F1, 끝없는 바쁜 고리.** run146 stub 이 채워져 `InternalCreateDialog` 가 hwnd 를 돌려주는 순간 이 고리에 들어간다 (명령 수 한도까지 CPU 만 씀) |
| `nt_user_get_message.cpp:6 ~ :7` "빈 큐면 … `DialogBox2 +0x23f (NtUserWaitMessage)` · GetMessage 몸 asm 은 없어 같은 모달 고리의 잠과 같다고 봄 — 근거 없음" | 이제 asm 이 있다: `GetMessageW +0x1b NtUserGetMessage` · `+0x43` 반환 그대로. **`DialogBox2` 는 `NtUserGetMessage` 를 부르지 않는다** (모달 고리의 잠은 `NtUserWaitMessage`) | **주석 근거 고칠 거리** — 이 파일은 앱 자기 루프 (`while (GetMessage())`) 용으로만 맞다. 모달 대화상자는 이 stub 에 안 닿음 |
| `:5` "기본 대답 0 이 가짜 WM_QUIT 로 창 고리를 끝내므로 … 재운다" | `GetMessageW` 가 반환을 그대로 줌 → 0 이면 앱 루프 끝 | **맞음** |
| `:22` `window != 0 && window != 0x10010` 이면 `stop` | run146 이 "hwnd 마다 다른 값" 을 권함 (대화상자 + 컨트롤) | **같이 바뀌어야 함** — `0x10010` 고정 조건 |
| `NtUserGetMessage` 가 −1 을 안 줌 | `GetMessageW` 는 −1 도 그대로 → 앱이 `while (GetMessage())` 면 영원히 | **맞음** — 앞으로도 −1 은 주지 말 것 |
| `NtUserDestroyWindow` 몸 없음 → 0 (run146 "기본 대답 0 으로 충분", sast/142 9절 "컨트롤 실패 때만 불림") | **모달 끝 `DialogBox2 +0x3c4` 에서 늘 불림.** 반환 안 봄 · `pdlg+0x20` 은 `+0x3bd` 에서 먼저 읽힘 | **기본 대답 0 은 맞음, "실패 때만" 은 틀림** (run146 고쳐 읽기) |
| `NtUserSetDialogPointer` 몸 없음 (run146: 몸 필요) | 이번에 하나 더: `DefDlgProcWorker +0x32 ~ +0x45` — 사본 `+0x2a` 가 0 도 `0x2a4` 도 아니면 **앱 프로시저를 안 부르고 0** (0 이면 `NtUserSetWindowFNID(0x2a4)` `0x108d` 를 부르고 진행) | **`0x2a4` 를 적는 게 필수** (run146 판정 강화 — 안 적고 다른 값이 남아 있으면 `WM_COMMAND` 가 앱에 안 닿아 F7) |
| `NtUserSetWindowLongPtr` (`0x15da`) 몸 없음 → 0 | `g_systemCallFilterId ≠ 5` 면 앱의 `SetWindowLongPtrW(hDlg, DWLP_USER, p)` 는 이 syscall. **`GetWindowLongPtrW` 는 `[[사본+0x128]+0x10]` 를 사용자 모드로 읽음** | **틀림** — 0 만 주면 앱이 `WM_INITDIALOG` 에서 넣은 포인터를 뒤 메시지 (`WM_COMMAND`) 에서 NULL 로 읽음 `[추측: 앱 쪽 일반 꼴]` |
| `NtUserSetWindowPos` · `NtUserPostMessage` · `NtUserShowWindow` · `NtUserSetFocus` · `NtUserSetActiveWindow` · `NtUserZapActiveAndFocus` · `NtUserEnableWindow` · `NtUserNotifyWinEvent` · `NtUserPostQuitMessage` 몸 없음 → 0 | **반환 안 봄** (호출 뒤 `NOP`) | **맞음** — 기본 대답 0 으로 충분 |

**"기본 단추를 누른 것으로 치기" — 가장 짧은 곳**: **`NtUserPeekMessage` (`0x1001`) 의 출력 한 번.** 반환 `1`, `*pmsg` (0x30 B, 먼저 0 채움) =

| MSG 칸 | 값 |
|---|---|
| `+0x00` hwnd | 그 모달 대화상자 hwnd |
| `+0x08` message | `0x111` (`WM_COMMAND`) |
| `+0x10` wParam | `LOWORD(pdlg+0x20)` (읽는 곳 `[[사본+0x128]+8]+0x20`) — **0 이면 1** (`IsDialogMessageW +0x98b61` 과 같은 규칙) |
| `+0x18` lParam | 그 ID 의 컨트롤 hwnd (사본 `+0x38` → `+0x48` 사슬에서 `+0x140 == ID`), 없으면 0 |
| 나머지 | 0 |

그러면 그 대화상자 한 번의 syscall 은 **`NtUserPeekMessage` · `NtUserSetWindowPos` · `NtUserPostMessage` · `NtUserDestroyWindow` 넷**. 이 MSG 가 `IsDialogMessageW` → `DispatchMessageWorker` → `DefDlgProcW` → 앱 프로시저까지 가려면 **`NtUserSetDialogPointer` 가 사본 `+0x2a` 에 `0x2a4` 를 적어 둔 것** (또는 0 그대로) 이 필수이고, 앱이 `DWLP_USER` 를 쓰면 **`NtUserSetWindowLongPtr` 가 `[[사본+0x128]+0x10]` 에 써야** 한다.

- 조심 (결과서 8절 · run142 를 이음 `[정적 두 줄을 이은 것]`): stub 이 컨트롤 프로시저를 안 부르면 (run146 — 컨트롤 hwnd 에 항목이 없으면 `WM_GETDLGCODE` 가 조용히 0) `pdlg+0x20` 은 처음 값 `1` 그대로다. 메시지 상자 `MB_DlgProc` 는 ID 1 · 2 를 **그 단추가 있을 때만** 받는다 (run142 `+0x5eaf0 GetDlgItem`) — `MB_YESNO` 처럼 `IDOK` 단추가 없는 판이면 `WM_COMMAND(1)` 이 무시되어 F7 로 간다. 기본 단추 스타일 · 순서를 정하는 `SoftModalMessageBox` · `MB_AddPushButtons` 줄은 **Q282**.

**stub 이 채워야 하는 최소 목록 — run146 목록 1 ~ 6 에 이어 붙임** (run146 의 7 "sast/146 에서 보탬" 자리를 채움. 결과서 9 ~ 17 → 여기 7 ~ 15):

7. **`NtUserPeekMessage(pmsg, hwnd, min, max, wRemove, flags)`** (`0x1001`, 모달 고리 `DialogBox2 +0x26b` 에서): 첫 부름에 반환 `1` + 위 MSG. 다시 불리면 (앱이 안 끝냄 — F7) 정책대로 `IDCANCEL` → `WM_CLOSE` → `WM_QUIT` `[추측: 정책]`, 그래도 안 끝나면 진단 정지. hwnd 는 stub 이 `NtUserSetDialogPointer(hwnd, pdlg)` 로 알던 hwnd 가운데 아직 `NtUserDestroyWindow` 안 당한 맨 나중 것 `[추측]`.
8. **hwnd 마다 (run146 4번) 에 더**: 컨트롤 사본 — `+0x1f` 비트 6 켜짐 · `+0x30` = 대화상자 사본 · `+0x48` = 다음 컨트롤 사본 (마지막 0) · `+0x140` = ID · `+0x1f` 비트 3 꺼짐. 대화상자 사본 — `+0x38` = 첫 컨트롤 · `+0x30` = `+0x40` = 0 · `+0x1f` 비트 6 꺼짐 · `+0x2a` = `0x2a4` (5번이 적음) · `+0x12` 비트 0 켜짐 · 비트 2 꺼짐 · `+0x78` = `DefDlgProcW`. 주소 변환 `포인터 − [사본+0x8] + 사본` 이 맞게 (`[+8] == 자기 주소` 면 그대로).
9. **TEB `+0x860` = 0 허용 (권장)**: `GetActiveWindow` · `GetFocus` 가 0 → `EndDialog` 는 `SetWindowPos` · `PostMessage` 둘만, `IsDialogMessageW` 키 길은 `DM_GETDEFID` 로. 채우려면 `[+0x20]` 포커스 · `[+0x28]` 활성 · `[+0x0]` 비트 2 = 0 · `[+0x4] | [+0x8]` 가 `0x3dff` 와 안 겹치게.
10. **`NtUserSetWindowPos` (`0x1021`) · `NtUserPostMessage` (`0x100d`) · `NtUserSetFocus` (`0x104c`) · `NtUserSetActiveWindow` (`0x10cd`) · `NtUserZapActiveAndFocus` (`0x15d6`) · `NtUserEnableWindow` (`0x1416`) · `NtUserNotifyWinEvent` (`0x1028`) · `NtUserPostQuitMessage` (`0x14ee`)**: 반환 안 봄 → 0. 단 앱이 `PostMessage` 로 자기 대화상자에 보내는 꼴 (`WM_CLOSE` 기본 처리 포함) 을 살리려면 `NtUserPostMessage` 가 큐에 넣고 `NtUserPeekMessage` 가 그 큐를 먼저 비워야 한다.
11. **`NtUserDestroyWindow(hwnd)` (`0x1095`)**: 반환 안 봄. 항목을 풀어도 (뒤 `ValidateHwnd` 0 — 고리는 이미 나감) 두어도 됨. `pdlg+0x20` 은 이미 읽힘. `WM_DESTROY` / `WM_NCDESTROY` 를 앱에 보내는 건 선택 (안 보내면 `pdlg` 누수만).
12. **`NtUserWaitMessage` (`0x100a`)**: 이 길에서 불리면 (= `NtUserPeekMessage` 가 0 을 줌) **진단 정지** 권장 — 돌려줘도 같은 고리, 재우면 모두 자서 엔진이 끝냄 (`nt_user_get_message.cpp` 와 같은 꼴).
13. **`NtUserMessageCall` (`0x1005`)**: 컨트롤 · `WM_GETDLGCODE` 의 `DefWindowProc` 길용 — 0 으로 충분. `NtUserDispatchMessage` (`0x1031`) · `NtUserTranslateMessage` (`0x100b`) · `NtUserCallMsgFilter` (`0x1013`) 는 위 조건이면 **안 불림** (불리면 조건이 어긋난 것 — 진단 거리).
14. **`NtUserSetWindowLongPtr(hwnd, 인덱스, 값, bAnsi)` (`0x15da`)**: 대화상자 hwnd · 인덱스 `0` / `0x10` → **`[[사본+0x128] + 인덱스] = 값`, 반환 = 옛 값**. `−21` (`GWLP_USERDATA`) → 사본 `+0xd8` `[추측]` (읽기 `GetWindowData +0xc3`). 인덱스 `8` (`DWLP_DLGPROC`) 은 user32 가 사용자 모드로 써서 안 옴. `DefDlgProcWorker +0x207` 은 `[여분+0x0]` ≠ 0 일 때 `(hDlg, 0, 0)` 으로 부름.
15. **되부름 · 큐**: 이 끝 길엔 되부름이 필요 없다. 큐가 있어야 하는 건 앱이 `PostMessage` 로 자기 대화상자에 보내는 경우 (F7) 뿐.

**되부름이 꼭 필요한가 — 아니오.** 끝 길의 앱 프로시저 호출은 `DispatchMessageWorker` → `UserCallWinProcCheckWow` → `DefDlgProcW` → `UserCallDlgProcCheckWow` 전부 user32 안 (CFG 간접 호출) `[정적]`. `NtUserDestroyWindow` 가 커널에서 `WM_DESTROY` / `WM_NCDESTROY` 를 되부르는 쪽 (`[추측]`, win32k 몫) 은 모달을 빠지는 데 필요 없다.

## 확인된 것

- `[정적]` Enter 는 고리에 `WM_KEYDOWN (0x100)` · `VK_RETURN` 으로 오고 `IsDialogMessageW` 가 `SendMessageW(hDlg, WM_COMMAND, ID, 컨트롤)` 로 바꿔 보낸다 (큐에 안 넣음). 기본 ID 는 포커스 컨트롤의 `DLGC_DEFPUSHBUTTON` → `DM_GETDEFID` → `1` 차례.
- `[정적]` `DM_GETDEFID` 의 답은 `pdlg+0x20` 아랫 16비트 `| 0x534b0000` — 기본 단추 ID 칸과 `EndDialog` 결과값 칸이 같다. 처음 값은 `1` (`InternalCreateDialog +0x6fa`).
- `[정적]` `WM_COMMAND` 를 대화상자 hwnd 앞으로 곧장 주면 앱 프로시저까지 win32k syscall 이 없다 (조건: 사본 `+0x12` 비트 2 = 0 · `+0x2a` ∈ {0, `0x2a4`} · TEB 훅 칸 0 · user32 훅 전역 0).
- `[정적]` `EndDialog` 는 `pdlg+0x18 |= 1` · `pdlg+0x20 = n` 을 쓰고 `NtUserSetWindowPos` · `NtUserPostMessage` (조건부로 넷 더) 를 부르며 반환을 안 본다.
- `[정적]` `DialogBox2` 는 매 바퀴 고리 맨 위에서 `pdlg+0x18` 비트 0 을 보고 빠지며, `pdlg+0x20` 을 **읽은 뒤** `NtUserDestroyWindow` (반환 안 봄) 를 부르고 그 값을 64비트 그대로 돌려준다.
- `[정적]` `NtUserPeekMessage` 의 반환은 0 / 0 아님만 본다. 고리를 빠지는 길은 넷뿐이고 횟수 · 시간 한도가 없다 — 늘 0 이면 바쁜 고리.
- `[정적]` `DialogBox2` 는 `NtUserGetMessage` 를 부르지 않는다 (모달 고리의 잠은 `NtUserWaitMessage`).
- `[정적]` A 판 (`DialogBoxIndirectParamA`) 의 끝 길은 W 판과 같고, `CreateDialogParam*` 은 `InternalCreateDialog` 에서 끝난다 (자원 없으면 0).
- `[정적]` `GetDlgItem` · `GetWindowLongPtrW(DWLP_USER)` · `GetParentDialog` · `GetDlgCtrlID` 는 syscall 이 없고, `SetWindowLongPtrW` 는 `g_systemCallFilterId ≠ 5` 면 `NtUserSetWindowLongPtr` syscall 이다.
- `[관찰]` test.exe 초기화 끝 모듈 6개에 user32 가 없다 (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run146 과 같은 줄) — 이 길은 test.exe 에선 안 생긴다.

## flow-writer 대조

결과서 오프셋을 `.asm` 과 맞춰 봤다. **어긋난 곳 여섯 (1 ~ 6)** (이 문서엔 고친 값을 씀) + 보탬 둘 (7 · 8):

1. **E2 "`DialogBox2 +0x56 JZ → +0x9963d`"** → `+0x56` 은 `TEST RAX,RAX`, **JZ 는 `+0x59`** (→ `+0x9963d`). hwnd 가 NULL 일 때도 `+0x45 TEST · +0x48 JZ` 로 같은 곳 (−1) 에 간다 (결과서에 없던 줄).
2. **F1 "`DialogBox2 +0x270 JZ → +0x20a`"** → `+0x270` 은 `TEST EAX,EAX`, **JZ 는 `+0x272`**. 목적지 `+0x20a` 는 맞음.
3. **F2 "`IsDialogMessageW +0x9f JZ`"** → `+0x9f` 는 `CMP [msg],RBX(0)`, **JZ 는 `+0xa2`** (→ `+0x142` 0 반환).
4. **4-1 "찾음 + 멀쩡 → `SendMessageW(…)` (`+0x98b19`) → `+0x98c31 JMP +0x13b`"** → 보낸 뒤 JMP 는 **`+0x98b1f`** (→ `+0x13b`). `+0x98c31` 은 `NtUserMessageBeep` 갈래 (꺼진 컨트롤) 끝의 JMP 다. 갈래 뜻 (둘 다 TRUE) 은 같음.
5. **E13 · E16 · 4-5 "`+0x173 DefWindowProcWorker(…)` (ret `+0x18a`)"** → `+0x173` 은 인자 채우기 시작 (점프 목적지 `MOV EAX,[RBP+0x68]`), **call 은 `+0x185`** (ret `+0x18a`).
6. **E4 · 2-3 "`InternalDialogBox +0x115 AND ESI,8`"** → `+0x115` 는 `MOVZX ESI, byte [소유자 사본+0x1f]`, **`AND ESI,8` 은 `+0x119`**. 뜻 (소유자 `WS_DISABLED` 비트 → `DialogBox2` 셋째 인자 → `pdlg+0x18` 비트 1) 은 맞음.
7. **보탬 — run142 `WM_QUIT` 반환**: 결과서 F7 "반환 = `pdlg+0x20` = 처음 값 1" 은 `.asm` 과 맞음 (`InternalCreateDialog +0x69a ~ +0x6fa` 가 `[[사본+0x128]+8]+0x20 = 1`, `DialogBox2 +0x37d → +0x389 → +0x3bd → +0x3ea`). run142 4절 "자료 `+0x20` 이 처음 값 (0) 이라 `MessageBoxW` 0" 과 다르다 → 아래 "정정". `SoftModalMessageBox +0x5b6 ~ +0x5be` 는 `InternalDialogBox` 반환이 −1 일 때만 0 으로 바꾸고 나머지는 그대로 (`+0x5fc MOV EAX,R15D`).
8. **보탬 — `EndDialog` 활성 창 비교**: `+0x45 XOR RAX,RDI · +0x48 MOVZX EBP,AX` — 아랫 16비트만 견준다 (결과서와 같음). TEB `+0x860` = 0 이면 `GetActiveWindow` 0 → `EBP` = hwnd 아랫 16비트 (≠0) → `+0x86` 안 탐 → `+0xa9` SetWindowPos → `+0xb8 JNZ → +0x106` 다시 0 → `+0x111 JNZ → +0xae` → `+0xd9` PostMessage. 그래서 "syscall 둘만" 이 맞다.
- 그 밖 결과서 오프셋 (`DialogBox2` · `EndDialog` · `_PeekMessage` · `IsDialogMessageW` · `DispatchMessageWorker` · `DefDlgProcWorker` · `_SetWindowLongPtr` · `_GetWindowLongPtr` · `GetDlgItem` · `DlgSetFocus` · `RealDefWindowProcWorker` · `CallMsgFilterW` · `TranslateMessage` · `GetMessageW` · A 판 / `CreateDialog*` · `SendMessageWorker +0x14c ~ +0x29c` · `InternalCreateDialog +0x5da ~ +0x72c` · 점프 목적지 `+0x12a ~ +0x142 → +0x389` · `+0x162 → +0x250` · `+0x24b → +0x119` · `+0x283 → +0x379` · `+0x2a2 → +0x2ba` · `+0x45 → +0x211` · `+0x71 → +0x261` · `+0x1c9 → +0x261` · `+0x70 → +0x272` 등) 과 syscall 번호 25개는 **일치**.

## 정정

- **run142 4절 "비정상 끝 = `WM_QUIT`" 의 반환값** — run142 는 "`EndDialog` 가 안 불려 자료 `+0x20` 이 처음 값 (0) 이라 `MessageBoxW` 0" 이라 적었다. `.asm` 으로는 **처음 값이 1** 이다 (`InternalCreateDialog +0x6fa MOV qword [pdlg+0x20],1`, 컨트롤 고리에서 `DLGC_DEFPUSHBUTTON` 컨트롤 ID 로 바뀔 수 있음 — run146), `DialogBox2` 는 `WM_QUIT` 에서도 `+0x3bd` 로 그 값을 돌려준다, `SoftModalMessageBox` 는 −1 만 0 으로 바꾼다 (`+0x5b6 ~ +0x5be` — `+0x5b6 XOR ECX,ECX` · `+0x5b8 CMP EAX,-1` · `+0x5bb MOV R15D,ECX` · `+0x5be CMOVNZ R15D,EAX`). 그래서 `DialogBox2` · `SoftModalMessageBox` 의 반환은 **0 이 아니다** `[정적]`. `MessageBoxWorker` · `MessageBoxTimeoutW` 가 그 뒤에 값을 바꾸는지는 이번에 안 봤다 `[공백]`. run142 맨 위 · flow.md 3장 run142 나무 한 줄에 정정을 남겼다.

## 앞 run 고쳐 읽기

- **run146** (맨 위 한 줄): PE_emulator 표 "`NtUserDestroyWindow` … 반환을 안 본다 (… `+0xc08`)" · 최소 목록 6 은 만들기 길 (컨트롤 실패) 만 적었다 — sast/142 9절 "컨트롤 실패 때만 (N10) 불림" 을 그대로 받은 것. → **모달 끝 `DialogBox2 +0x3c4` 에서도 늘 불린다.** 반환 안 봄 · 기본 대답 0 이면 됨은 그대로. 그리고 최소 목록 7 "(sast/146 에서 보탬)" · "아직 확인 필요" 정적 1 ~ 4 는 이 문서로 닫힘 (`NtUserSetDialogPointer` 의 `0x2a4` 는 "필수" 로 강화).

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (user32 안 올림) 이다.

## 아직 확인 필요

동적 (결과서 7절 1 ~ 12 → 대기열 **Q273** — 같은 VM GUI 세션, 출처로 보탤 꼴. test.exe 가 아니라 `DialogBoxParamW` 를 부르는 GUI 프로세스. 멈춤점은 user32 유저 멈춤점):

1. `user32!DialogBox2+0x270` (`_PeekMessage` 반환 직후) — `r eax` · `dq @rsp+0x50 L6` (MSG — 처음 오는 메시지 차례: `WM_PAINT` · `WM_NCACTIVATE` …) · 몇 번째에 `0x111` / `0x100` 이 오나.
2. `user32!IsDialogMessageW+0x189` (`WM_GETDLGCODE` 답) · `+0x98b51` (`DM_GETDEFID` 답, `0x534b0001`?) · `+0x98b1e` (`WM_COMMAND` 보낸 뒤) — Enter 한 번.
3. `user32!DefDlgProcWorker+0xc4` (앱 반환 `[rbp-0x28]`) · `+0xbf` 앞 (앱 프로시저 주소 `[pdlg+0]` · 인자).
4. `user32!EndDialog+0x24` (`rbx` = 사본) · `+0x70` (`dq poi(poi(@rbx+0x128)+8) L6`) · `+0xb0` · `+0xe0` · `user32!DialogBox2+0x3bd` 의 `rsi` · `+0x3cb` 뒤 `rax` — 진짜 `EndDialog` syscall 이 둘뿐인지.
5. TEB `+0x860` 진짜 값 — `dq @$teb+0x860 L1` → `dq poi(@$teb+0x860) L8` (`+0x20` 포커스 · `+0x28` 활성 · `+0x4 +0x8` 깨움 비트).
6. 자식 연결 진짜 값 — 대화상자 사본 `+0x30 +0x38 +0x40 +0x48` · 컨트롤 사본 · `+0x140` — 연결 순서가 템플릿 차례인지.
7. `DialogBox2+0xaf` 의 `gpsi+0x88c` · `+0x2f5` 의 `gpsi+0x764` · `GetCapture` 의 `gpsi+0x1b50` 진짜 값.
8. `DispatchMessageWorker+0xb6` (`[사본+0x12]`) · `+0x1d8` (`rdx` = `DefDlgProcW`?) · `+0x341` 이 안 걸리는지.
9. `dq user32+0xda568 L4` · `0x87` · `0x111` 의 비트 (`db poi(user32+0xda570)+(0x87>>3) L1`).
10. `RealDefWindowProcWorker+0xc6` 이 안 걸리는지 (앱이 `WM_COMMAND` 에 FALSE 인 대화상자).
11. `WM_CLOSE` 시험 — 앱이 FALSE 면 `DefDlgProcWorker+0x467` 에서 `PostMessageW(…0x111, 2 …)` 가 오는지.
12. A 판 한 번 (`DialogBoxIndirectParamA` 표본 하나) — `InternalCreateDialog+0x5ff` 직후 `ecx` (`0x80000000`) · 사본 `+0x12` 비트 3 · `pdlg+0x18` 비트 2 · `SendMessageWorker+0x16f` 가 건너뛰는지.

정적 (결과서 6절 "win32k 몫 남음" 1 ~ 7 + 8절 "다음 요청 후보" → 대기열 **Q282**):

1. **급함**: win32kfull `xxxCreateWindowEx` — 자식 연결 `+0x30 / +0x38 / +0x48` 순서 (`LinkWindow` 가 맨 아래 / 위 어디에) · 확장 스타일 `0x80000000` → 사본 `+0x12` 비트 3 · 클라이언트 프로시저 (`DefDlgProcA`) 로 가는 줄.
2. **급함**: win32kfull `NtUserPeekMessage` · `xxxPeekMessage` — 진짜 첫 메시지들 · `wRemove` (stub 의 "맨 처음 한 번 `WM_COMMAND`" 가 진짜와 얼마나 다른지).
3. `NtUserDestroyWindow` · `xxxDestroyWindow` — `WM_DESTROY` / `WM_NCDESTROY` 되부름 · 항목 해제.
4. `NtUserSetWindowPos(…, 0x97)` — 숨기기 · 활성 변경 부작용 (TEB `+0x860` 블록).
5. `NtUserInitializeClientPfnArrays` (`0x14aa`) · `ClientThreadSetup +0x228` — 비트표 `user32+0xda568 ~ +0xda580` 을 채우는 곳 `[추측]`.
6. `NtUserMessageCall` fnid `0x29e` · `WM_GETDLGCODE` 답 · `NtUserSetWindowLongPtr` — `GWLP_USERDATA` 가 사본 `+0xd8` 로 가는 줄.
7. **`SoftModalMessageBox` · `MB_AddPushButtons`** — 메시지 상자의 기본 단추 스타일 · 순서 (`MB_YESNO` 에서 `pdlg+0x20` 이 맞는지), `MessageBox` 가 같은 `DialogBox2` 고리를 타는지 (run142 틀 확인 — 같으면 stub 7 이 `MessageBox` 에도 쓰임). `ButtonWndProcWorker` · `xxxButtonNotifyParent` 본문 (`DLGC_DEFPUSHBUTTON` 답 · 클릭 → `WM_COMMAND`).
8. `RealDefWindowProcWorker` 의 `WM_CLOSE` · `WM_SYSCOMMAND(SC_CLOSE)` 갈래. `GetUserDefaultLCID` (kernelbase, `IsDialogMessageW +0x22`) — 에뮬레이터가 이미 만난 길이면 불필요.
9. (flow-writer 보탬) `g_systemCallFilterId` 를 누가 언제 정하나 — `SetWindowLongPtr` 가 syscall 인지 사용자 모드인지를 가른다. 그리고 run142 정정 뒤 `MessageBoxWorker` · `MessageBoxTimeoutW` 가 `SoftModalMessageBox` 반환을 바꾸는지.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함). 디버거 여부를 보는 줄은 이번에 읽은 길에 없었다 `[정적: 읽은 줄 안에서]`.
- **스크립트 문제 없음** (결과서). 읽을 때 조심할 것: `IsDialogMessageW` 는 run146 의 `InternalCreateDialog` 처럼 조각 (`frag:FUN_…+0x…` 와 `(= IsDialogMessageW +0x…)`) · "주인 없는 덩어리" (`+0x98…` 큰 오프셋) 가 섞인다. 결과서의 어긋남 4 (`+0x98b1f` ↔ `+0x98c31`) 는 덩어리와 조각의 끝 JMP 를 헷갈린 꼴이다. 또 결과서는 "JZ 줄" 을 바로 앞 `TEST` / `CMP` 줄 오프셋으로 적은 곳이 셋 (대조 1 · 2 · 3) — WinDbg 멈춤점에는 문제없지만 "어느 줄에서 갈라지나" 를 적을 땐 조건 점프 줄로 적자 (기록만, 하네스 제안 아님).
- 같은 때 다른 decompiler (sast/144 · 145) 와 explainer (run147 해설서) 가 일했다 — 이 문서는 그 파일을 안 건드렸다.

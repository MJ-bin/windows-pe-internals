# 146 dialog-end — 정적 분석 결과

- 요청서: `_workspace/sast/146_dialog-end.md` (원문 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` "알고 싶은 것 3" · "받을 꼴" 과 `_workspace/sast/142_dialog-window_result.md` 11절 · 4절 · 6절을 먼저 읽음). 대기열 Q274a 둘째 몫. 정적만.
- 스크립트 문제: 없음 (`export` · `refs` 모두 `SCRIPT ERROR` 없이 끝남).
- PDB GUID (`ms/sym/user32.dll.pdb.info.txt`): user32 `CDB947D1B96B87977915DE04CF54279C1` (sast/142 와 같은 값). 커널 PDB 는 이번에 안 만짐 (ntoskrnl 안 엶).
- 다시 analyze 한 모듈: 없음. user32 만 `-readOnly` export · refs. **win32kfull · win32kbase · combase · rpcss · gdi32 · gdi32full · gdiplus 프로젝트는 열지 않음.** 작업하는 동안 `_workspace/sast/145_gdi-display-dc_result.md` 가 없어서 (마지막 확인 직전에 생긴 것을 봄 — 그때는 이미 user32 만으로 닫힌 뒤라 안 엶) 요청서 ⚠ 대로 win32k 는 안 열고 아래 "win32k 몫 남음" 절에 목록만 남김. **이제 145 결과서가 있으니 6절 목록은 다음 요청에서 열어도 됨** (이미 있던 `ms/disassemble/win32kfull/*.asm` 글 파일은 읽기만 함).
- 새로 뽑은 것 (depth 0, 로그 `ms/ghidra/user32.export.s146{a..e}.stdout`, `user32.refs.s146_*.stdout`):
  - user32 asm · c: `PeekMessageW` `GetMessageW` `TranslateMessage` `TranslateMessageEx` `_GetDlgItem` `_FindDlgItem` `_SetWindowLongPtr` `SetWindowLongPtrW` `GetWindowLongPtrW` `_GetWindowLongPtr` `DispatchMessageW` `IsDialogMessage` (= A 판 `IsDialogMessageA`) `DialogBoxIndirectParamA` `DialogBoxIndirectParamW` `CreateDialogParamW` `CreateDialogParamA` `CreateDialogIndirectParamAorW` `CreateDialogIndirectParamA` `CreateDialogIndirectParamW` · `CallMsgFilterW` `ValidateHwndNoRip` `RealDefWindowProcWorker` `xxxSaveDlgFocus` `xxxRestoreDlgFocus` `_GetNextDlgTabItem` `GetFocus` `_IsChild` `IsChild` · `TestWindowProcess` · `_GETPTI` `GETCLIENTWNDINFO` `IsWindow` · `GetWindowData` `GetWindowLongPtrA` `GetWindowLongA` `GetDlgCtrlID`.
  - `refs`: user32 `0xda568` (+0x28 B) · `0xda570` · `0xda580` · `0xd96f0`.
  - 이미 있던 asm (읽기만): `DialogBox2` `EndDialog` `IsDialogMessageW` `DispatchMessageWorker` `DefDlgProcW` `DefDlgProcA` `DefDlgProcWorker` `_PeekMessage` `SendMessageWorker` `InternalDialogBox` `InternalCreateDialog` `DialogBoxParamW` `DialogBoxIndirectParamAorW` `GetDlgItem` `_NextControl` `_GetChildControl` `GetParentDialog` `DlgSetFocus` `DefWindowProcWorker` `UserCallDlgProcCheckWow` `GetActiveWindow` `GetCapture` `PtiCurrent` `MB_DlgProc`. win32kfull 글 파일 `xxxCreateWindowEx.asm` 은 ANSI 비트 줄 찾느라 눈으로만 훑음 (아래 4 절 `[추측]`).
  - 표 값 읽기 (파일에서 직접, `ms/bin` user32.dll): `MessageTable` (rva `0xb91f0`) · `gapfnScSendMessage` (rva `0xb4230`) · `RealDefWindowProcWorker` / `SendMessageWorker` 의 점프표.
- 버전 대조: user32 는 "알려진 사실" 표에 줄이 없어 sast/142 가 확인한 줄과 맞춤 — `ClassNameToVersion +0x226/+0x22d` · `InternalCreateDialog +0x63f/+0x644` `+0x653/+0x658` `+0x667/+0x66c` `+0xa64/+0xa69` `+0xf7a/+0xf81` · `ValidateDialogPwnd +0x4f/+0x56` 전부 `_calls.tsv` 에서 **일치**. 새 줄 `InternalDialogBox +0x8a/+0x8f` (→ `DialogBox2`) · `DialogBox2` 호출표 (`+0x26b/+0x270 _PeekMessage` `+0x29b/+0x2a0 IsDialogMessageW` `+0x3c4/+0x3cb NtUserDestroyWindow`) 는 `.asm` 줄과 대조해 맞음.
- PDB 에 `tagWND` · `tagDIALOG` · `tagMSG` 같은 형식이 없음 (142 확인): 아래 칸 이름은 `[추측: 이름]`, 오프셋 · 읽는 줄은 `[정적]`.
- 오프셋 쓰는 법: 부르기는 `call/ret` (WinDbg `caller+0x..` 와 같음 = `_calls.tsv` 의 `ret_off`), 점프 목적지는 "목적지 − 함수 시작". 조각은 `(= 함수 +0x…)` 로 바꾼 값만 씀.

---

## 먼저 읽는 한 줄 답

- **1 (원문 3 (a) Enter)**: 진짜에서 Enter 는 고리에 **`WM_KEYDOWN (0x100)`, wParam = `VK_RETURN (0xd)`** 로 온다 (hwnd = 포커스 창). `IsDialogMessageW` 가 그걸 **`SendMessageW(hDlg, WM_COMMAND, ID, 컨트롤 hwnd 또는 0)`** 로 바꿔 **보낸다** (큐에 안 넣음, 키 메시지는 번역 · 배달 안 하고 TRUE). ID 고르는 차례: ① 포커스 컨트롤이 `WM_GETDLGCODE` 에 `0x10` 비트 (DLGC_DEFPUSHBUTTON) 를 답하면 그 컨트롤 ID (`GetDlgCtrlID`) ② 아니면 `DM_GETDEFID (0x400)` 답의 윗 16비트가 `0x534b` 면 아랫 16비트 ③ 아니면 `1` (IDOK). **`DM_GETDEFID` 는 `DefDlgProcWorker` 가 `pdlg+0x20` 아랫 16비트 | `0x534b0000` 으로 답한다 → 기본 단추 ID 칸 = 결과값 칸 = `pdlg+0x20` (142 11절의 "겸용" 확인).** (`IsDialogMessageW +0x175~+0x1ac · +0x98b3e~+0x98b66 · +0x98c0f~+0x98c31` · `DefDlgProcWorker +0xa24~+0xa55`)
- **2 (원문 3 (b) WM_COMMAND 곧장)**: `WM_COMMAND(ID, BN_CLICKED=0)` 를 hwnd = 대화상자 로 주면 `IsDialogMessageW` → `TranslateMessage` (WM_COMMAND 는 `+0x3b XOR EAX,EAX` 로 syscall 없이 0) → `DispatchMessageWorker` → `UserCallWinProcCheckWow` → `[사본+0x78]` (= `DefDlgProcW`) → `DefDlgProcWorker` → `UserCallDlgProcCheckWow` → **앱 프로시저**까지 **win32k syscall 0번** 간다 (조건: 사본 `+0x12` 비트 2 = 0 · `+0x2a` = `0x2a4` · TEB `+0x820`→`+0x10` 와 TEB `+0x838` = 0 · `gpsi+0x764` 등 아래 칸 표). `EndDialog(hDlg, n)` 은 `pdlg+0x18 |= 1`, `pdlg+0x20 = n` (64비트) 를 쓰고 syscall 은 `NtUserSetWindowPos(0x1021)` · `NtUserPostMessage(0x100d)` 둘 (TEB `+0x860` = 0 일 때; 더 켜지면 `NtUserSetFocus` `NtUserSetActiveWindow` `NtUserZapActiveAndFocus` `NtUserEnableWindow` 가 조건부). **반환은 둘 다 안 봄.**
- **3 (원문 3 (c) 끝 · 고리)**: `DialogBox2` 는 고리 맨 위 `+0x12a~+0x142` 에서 **`[[사본+0x128]+8]` 의 `+0x18` 비트 0** 을 본다 (켜지면 `+0x389` 로 빠짐). 빠진 뒤: `[gpsi+0x764] & 0x8000` 이면 `NtUserNotifyWinEvent(0x11)` → `HMValidateHandleNoSecure(hwnd)` (0 이면 **반환 0**) → **`pdlg+0x20` 을 먼저 읽고 (`+0x3bd`)** → `NtUserDestroyWindow(0x1095)` (**반환 안 봄**) → (소유자 0 이면) 읽어 둔 값을 `RAX` 로 돌려줌 = `DialogBoxParamW` 반환 (64비트 그대로). `NtUserPeekMessage` 반환은 **0 / 0 아님** 만 본다 (0 이면 "빈 틈": 안 보이면 `NtUserShowWindow(1)` + `UpdateWindow` 로 보이게 하고, 보이면 소유자 있을 때 한 번 `WM_ENTERIDLE (0x121)`, 소유자 없으면 `NtUserWaitMessage(0x100a)`, 반환 안 봄). **`NtUserPeekMessage` 가 늘 0 이거나 같은 헛 메시지만 주면 끝없이 돈다** — 나가는 길은 `pdlg+0x18` 비트 0 · `pdlg` NULL · hwnd 가 사라짐 (`HMValidateHandleNoSecure` 0 · 사본 `+0x2a & 0xc000`) · `WM_QUIT (0x12)` 네 가지뿐. 금지 꼴 표 아래.
- **4 (원문 3 (d) A 판 · 모달 아닌 판)**: `DialogBoxIndirectParamA` 는 `+0x9 MOV [RSP+0x28],2` 로 깃발 `2` 를 넘겨 `InternalCreateDialog +0x5da AND R14D,2` → 확장 스타일 `0x80000000` (`+0x5e5~+0x5f4`) · `pdlg+0x18 |= 4` (`+0x72c`) 만 달라진다. 끝 길 (`WM_COMMAND` → `EndDialog`) 은 **같다** — `SendMessageWorker +0x14c~+0x16f` 글자 종류 불일치 갈래에서도 `WM_SETFONT (0x30)` `WM_GETDLGCODE (0x87)` `WM_INITDIALOG (0x110)` `WM_COMMAND (0x111)` `DM_*` (≥ `0x400`) 는 **변환 · syscall 없이 지나간다** (점프표 기본 갈래 `+0x200` 이고 `MessageTable & 0x100` 이 꺼져 있음). 변환이 도는 건 글자 메시지 (`0x102 0x103 0x106 0x107 0x10f 0x120 0xcc 0x286`) 와 `0xb0 0xb1` 뿐. `CreateDialogParamW/A` 는 `CreateDialogIndirectParamAorW +0x52` → `InternalCreateDialog` 에서 **끝** (`InternalDialogBox` · `DialogBox2` 안 탐); 자원을 못 찾으면 **0** (`DialogBox*` 는 −1).
- **5 (142 11절 앱 프로시저가 거의 늘 부르는 것)**: `GetDlgItem` = `ValidateHwnd` 후 사본 `+0x38` (첫 자식) → `+0x48` (다음 형제) 사슬을 `+0x140` (컨트롤 ID) 으로 찾음 — **syscall 0**, 못 찾으면 `SetLastError(0x58d)` · 0. `GetWindowLongPtrW(hDlg, DWLP_USER=0x10)` = **여분 블록 `[사본+0x128]+0x10`** 을 사용자 모드로 읽음 (항목 `+0x8` == TEB `+0x78` 이면 syscall 0, 다르면 `NtUserMessageCall`). **`SetWindowLongPtrW(hDlg, 0x10, v)` 는 (`g_systemCallFilterId ≠ 5` = 보통) syscall `NtUserSetWindowLongPtr (0x15da)`** (`_SetWindowLongPtr +0x6a/+0x71 JNZ → +0x261`, 반환 = 옛 값) — **stub 이 같은 칸 `[사본+0x128]+0x10` 에 써야 `Get` 이 그 값을 읽음.** (`DWLP_DLGPROC (8)` 쓰기만 사용자 모드: `pdlg+0x0`.) `DWLP_MSGRESULT (0)` = 여분 `+0x0`, `DWLP_DLGPROC (8)` = `pdlg+0x0`. `GetParentDialog` = 사본 `+0x12` 비트 0 이 켜진 창을 만날 때까지 `+0x30` 부모 사슬을 올라감 (syscall 0). `WM_GETDLGCODE` 가 앱에서 0 으로 오면 `DefDlgProcWorker +0x173` → `DefWindowProcWorker` → `RealDefWindowProcWorker`: 커널이 채우는 비트표 값에 따라 **syscall 0 (`return 0`) 이거나 `NtUserMessageCall(0x1005)` 한 번** (`gapfnScSendMessage[0x15]` = `NtUserMessageCall`, fnid `0x29e`). `DlgSetFocus` = `SendMessageW(0x87)` → (`DLGC_HASSETSEL` 이면 `EM_SETSEL`) → **`NtUserSetFocus(0x104c)` 꼬리호출**.
- **stub 한 줄**: "기본 단추를 누른 것으로 치기" 를 가장 짧게 넣는 곳 = **`NtUserPeekMessage(0x1001)` 의 출력 한 번**: 반환 `1`, MSG = `{hwnd = 모달 대화상자 hwnd, message = 0x111, wParam = LOWORD(pdlg+0x20) (0 이면 1), lParam = 그 ID 의 컨트롤 hwnd 또는 0, 나머지 0}`. 그러면 syscall 은 PeekMessage · SetWindowPos · PostMessage · DestroyWindow 넷.

---

## 1. 끝 길 지도 — 고리가 `WM_COMMAND(IDOK, BN_CLICKED)` 한 개를 받아 `DialogBoxParamW` 가 돌아올 때까지

번호는 `ms/disassemble/win32u/_syscalls.tsv`. 앞 단계 (142 1절 차례 1~26 = `InternalCreateDialog` 가 hwnd 를 돌려줌) 는 끝났다고 놓고, 거기서 이어진다. "TEB `+0x860` = 0 · 훅 0 · `gpsi` 칸 0" 인 가장 짧은 꼴 기준 (켜졌을 때는 "조건" 칸). 줄은 `ms/disassemble/user32/<함수>.asm` `[정적]`.

| 차례 | 부르는 것 / syscall (번호) | 조건 | 반환을 user32 가 보나 · 0 이면 | syscall 없이 읽는 / 쓰는 칸 | 줄 |
|---|---|---|---|---|---|
| E1 | `InternalDialogBox` → `DialogBox2(hwnd, 소유자, 소유자가이미꺼짐, 초점줄까)` | 항상 (142 차례 26 뒤) | 반환 = `DialogBoxParamW` 반환 그대로 (64비트) | 소유자 = 부모 인자 (표본은 0) | `InternalDialogBox +0x7e~+0x8a/+0x8f` · `DialogBoxParamW +0x6d` |
| E2 | `ValidateHwnd(hwnd)` | 항상 | 0 → **−1** (소유자 있으면 `NtUserEnableWindow(소유자,1)` · `NtUserSetFocus` 한 번씩) | 항목 · TEB (142 4절) | `DialogBox2 +0x4e/+0x53 +0x56 JZ → +0x9963d~+0x9967c` |
| E3 | `GetCapture` | 항상 | 0 이 아니면 `SendMessageW(그 창, WM_CANCELMODE 0x1f)` | `gpsi+0x1b50` (0 이면 syscall 없이 0, ≠0 이면 `NtUserGetThreadState(2)` 꼬리호출) | `DialogBox2 +0x5f/+0x64 +0x67 JNZ → +0x479` · `GetCapture +0x7/+0xe/+0x14` |
| E4 | `pdlg+0x18` 비트 1 쓰기 · 스타일 읽기 | 항상 | — | **쓰기** `[[사본+0x128]+8]+0x18` (비트 1 = 소유자가 이미 꺼져 있었음, `InternalDialogBox +0x115 AND ESI,8`) · 읽기 사본 `+0x1c` (비트 8 = `DS_NOIDLEMSG` → `[RSP+0x40]`) · `+0x1f` 비트 4 (보임 → `R12D`) · `gpsi+0x88c` 비트 0 | `DialogBox2 +0x6d~+0x8f` · `+0x92~+0xa4` · `+0xa8~+0xce` |
| E5 | **고리 맨 위**: `pdlg` 읽기 · 끝남 검사 | 매 바퀴 | `pdlg` NULL 이거나 `+0x18` 비트 0 켜짐 → **빠짐** (`+0x389`) | 사본 `+0x128` → `[+8]` (**NULL 이면 접근 위반**) | `DialogBox2 +0x12a~+0x142` |
| E6 | (빈 큐 빠른 길) `[TEB+0x860]` | `[TEB+0x860] ≠ 0` 일 때만 | 0 이면 곧장 E7 | TEB `+0x860` → `+0x0 +0x4 +0x8 +0x14` · TEB `+0x808` (카운터) · `+0x81c` | `DialogBox2 +0x148~+0x20a` (`+0x162 JZ → +0x250`) |
| E7 | **`_PeekMessage(&msg, 0, 0, 0, PM_REMOVE=1, 0, 0)` → `NtUserPeekMessage` (0x1001)** | 매 바퀴 | **반환 0 이면 E11 (빈 틈)** · ≠0 이면 E8. `_PeekMessage` 는 반환을 그대로 줌 (`+0x44 MOV EBX,EAX` · `+0x57 MOV EAX,EBX`) | MSG 를 `DialogBox2` 스택 `[RSP+0x50]` (0x30 B, `+0x39~+0x41` 에서 0 채움) 에 받음. `message == 0xcc / 0x102` 면 wParam 을 `& 0xffff` (`_PeekMessage +0x7e~+0x80 · +0xb1~+0xb6`) | `DialogBox2 +0x250~+0x272` · `_PeekMessage +0x38/+0x3f +0x44~+0x63` · `g_systemCallFilterId ≠ 5` (`+0x65`) |
| E8 | `message == WM_QUIT (0x12)` 검사 | ≠0 일 때 | 맞으면 `NtUserPostQuitMessage(wParam)` (0x14ee, 반환 안 봄) → **빠짐** (반환 = `pdlg+0x20`) | MSG `+0x8`, `+0x10` | `DialogBox2 +0x274 +0x280 +0x283 JZ → +0x379` · `+0x37d/+0x384` |
| E9 | `IsDialogMessageW(hwnd, &msg)` | 사본 `+0x10` 비트 5 = 0 (켜지면 Ctrl+C / Ctrl+Ins 를 `WM_COPY` 로) | 반환 ≠0 이면 번역 · 배달 건너뜀 (`+0x2a2 JNZ → +0x2ba`) | 아래 E10 | `DialogBox2 +0x289 +0x28d` · `+0x293~+0x2a2` |
| E10 | `IsDialogMessageW` 안: `GetUserDefaultLCID` (kernelbase, **user32 밖 — 정적으로 안 닫음**) · `ValidateHwndNoRip(hDlg)` (0 → 0 반환) · `message < 0x400` 이고 `MessageTable[msg] & 0x200` 이면 `SetLastError(0x487)` 후 0 · **`CallMsgFilterW(&msg, 0)`** · `msg.hwnd == 0` 이면 0 · `ValidateHwnd(msg.hwnd)` · `msg.hwnd == hDlg` (아니면 `+0x30` 부모 사슬을 `hDlg` 까지 올라감, 자식 비트 `+0x1f & 0xc0 == 0x40` 필요) · 메시지 갈래 | WM_COMMAND (`0x111`) 는 갈래에 안 걸려 기본: `TranslateMessage` + `DispatchMessageWorker(&msg, 0)` → **TRUE** | `CallMsgFilterW` 는 `[[TEB+0x820]+0x10] \| [TEB+0x838] = 0` 이면 syscall 없이 0, ≠0 이면 `NtUserCallMsgFilter` (0x1013, 반환 ≠0 이면 TRUE 로 끝) · 나머지 syscall 0 | MessageTable (rva `0xb91f0`, 이미지 안): `0x100`=0 `0x111`=0 (**0x200 없음 → 통과**) · `0x110`=`0x600` `0x87`=`0x615` (**0x200 있음 → 큐로 주면 거부 · 0 반환**) | `IsDialogMessageW +0x22 +0x3d +0x50~+0x99 +0x9f~+0xc6 +0xf2~+0x13b` · `CallMsgFilterW +0x26~+0x66` |
| E11 | **빈 틈** (E7 반환 0): ① 보임 비트 R12D = 0 → `NtUserShowWindow(hwnd,1)` (0x1052) + `UpdateWindow` (사본 `+0x88 +0x11 +0x38` 0 이면 syscall 0) + (`gpsi+0x764 & 0x8000` 이면 `NtUserNotifyWinEvent(0x10)` 0x1028) → R12D=1 → 고리 위로 ② 보임 · 소유자 ≠ 0 · `DS_NOIDLEMSG` 아님 · 이번 틈에 아직 안 보냄 → `SendMessageW(소유자, WM_ENTERIDLE 0x121, 0, hwnd)` 한 번 ③ 그 밖 (**표본 = 소유자 0**) → `HMValidateHandleNoSecure(hwnd)` · 사본 `+0x2a & 0xc000` · **`NtUserWaitMessage` (0x100a)** → 고리 위로 | — | **셋 다 반환 안 봄.** ③ 에서 hwnd 무효 / `+0x2a & 0xc000` 이면 빠짐 | 사본 `+0x1f`, `+0x2a` · `[RSP+0xb0]` (이번 틈 ENTERIDLE 보냄 표시, 메시지를 받을 때마다 0 으로) | `DialogBox2 +0x20a~+0x24b` · `+0x2d2~+0x31e` · `+0xd3~+0x119` |
| E12 | `DispatchMessageWorker(&msg, 0)` (IsDialogMessageW `+0x136`) | 항상 | — | `message & 0xfffe0000` 이면 `SetLastError(0x57)` 후 0 · `ValidateHwnd(msg.hwnd)` 0 이면 0 · **msg.hwnd 를 사본 `+0x0` 으로 정규화해 되씀** · `MessageTable & 0x200` 이면 `SetLastError(0x487)` 후 0 · **사본 `+0x12` 비트 2 켜지면 `NtUserDispatchMessage` (0x1031)** (WM_PAINT 도) · 사본 `+0x12` 비트 3 (ANSI) 과 `fAnsi=0` 이 다르면 항목 `+0x8` == TEB `+0x78` 검사 + 글자 변환 · 같으면 **`UserCallWinProcCheckWow([사본+0xe0], [사본+0x78], msg.hwnd, message, wParam, lParam, 0, 1)`** (아래 2절 칸 표) | `DispatchMessageWorker +0x22 +0x3b~+0x4f +0x5d~+0x7d +0xae~+0x104 +0x1ad~+0x1fc` |
| E13 | `DefDlgProcW` → `ValidateHwnd` → `DefDlgProcWorker(사본, 0x111, ID, lParam, 0)`: **fnid 검사** (`+0x2a` = 0 이면 `NtUserSetWindowFNID(0x2a4)` (0x108d) 후 진행 · **`0x2a4` 아니면 앱 프로시저 안 부르고 0 반환**) · `ValidateDialogPwnd` · `DWLP_MSGRESULT` 비움 · 앱 프로시저 부름 | 사본 `+0x2a` ∈ {0, `0x2a4`} · `[[사본+0x128]]` 이 0 이 아니면 `_SetWindowLongPtr(hDlg, 0, 0, 0)` (보통 syscall `NtUserSetWindowLongPtr` 0x15da, 반환 안 봄) | `UserCallDlgProcCheckWow` 의 **반환 EAX 는 (훅 없을 땐) 늘 0**, 앱의 DLGPROC 반환은 출력 포인터 `[rbp-0x28]` 로 받음 (`+0xc4 TEST EAX,EAX` · `+0xec MOV R13,[RBP-0x28]`) | `[pdlg+0x0]` (앱 프로시저, 0 이면 앱 호출 건너뜀 `+0x9a JZ → +0xf0`) · 사본 `+0xe0` (활성화 컨텍스트) | `DefDlgProcW +0x17~+0x31` · `DefDlgProcWorker +0x32~+0x65 +0x8c~+0xec` · `UserCallDlgProcCheckWow +0x193/+0x198` (CFG 간접 호출) |
| E14 | **앱 프로시저**: `WM_COMMAND` 처리 → `EndDialog(hDlg, n)` → (앱은 `TRUE` 반환) | 앱 몫 | — | 아래 E15 | — |
| E15 | `EndDialog(hDlg, n)`: `ValidateHwnd` (0 → **0 반환**) · `ValidateDialogPwnd` (대화상자 표시 비트가 이미 켜져 있으면 곧 1) · `GetActiveWindow` · **`pdlg+0x18 \|= 1` · `pdlg+0x20 = n` (64비트)** · `NtUserSetWindowPos(hDlg,0,0,0,0,0, 0x97)` (0x1021) · `NtUserPostMessage(hDlg, 0, 0, 0)` (0x100d) · **반환 `1`** | `EndDialog` 가 활성 창 / 포커스 / 소유자 · 부모를 보고 아래 syscall 을 더함: `GetActiveWindow` 가 hDlg 와 아랫 16비트가 같으면 `GetFocus`+`IsChild` → 자식에 포커스면 `NtUserSetFocus(hDlg)` (0x104c) · 소유자 `NtUserSetActiveWindow` (0x10cd) · 소유자 없으면 `NtUserZapActiveAndFocus` (0x15d6) · 소유자가 있고 `pdlg+0x18` 비트 1 = 0 이면 `NtUserEnableWindow(소유자,1)` (0x1416) | **전부 반환 안 봄** (호출 뒤 `NOP` · 반환 레지스터 안 씀) | `GetActiveWindow` = `[[TEB+0x860]+0x28]` (TEB `+0x860` = 0 이면 0 → 위 둘만) · `GetFocus` = `[[TEB+0x860]+0x20]` · 사본 `+0x1f` (자식 비트) · `+0x30` (부모) · `+0x40` (소유자) · 사본 `+0x128` | `EndDialog +0x1f~+0xe5` (`+0x45 XOR RAX,RDI` `+0x48 MOVZX EBP,AX` `+0x58` `+0x65~+0x7f` `+0xa9/+0xb0` `+0xd9/+0xe0`) · `+0x106~+0x11f` · `+0x12a~+0x151` · `+0x156~+0x18a` · `GetActiveWindow +0x9~+0x17` · `GetFocus +0x9~+0x15` |
| E16 | `DefDlgProcWorker` 돌아옴: `HMValidateHandleNoSecure(hwnd)` · 사본 `+0x2a & 0xc000` → 앱 반환 `R13` 이 ≠0 이면 **`[사본+0x128]+0x0` (`DWLP_MSGRESULT`) 을 돌려줌**, 0 이면 `DefWindowProcWorker` (→ 커널 비트표에 따라 `return 0` 또는 `NtUserMessageCall`) | — | `DispatchMessageWorker` · `IsDialogMessageW` 둘 다 이 반환을 **안 봄** | 사본 `+0x12` 비트 0 | `DefDlgProcWorker +0xcc~+0xec +0x143~+0x1f7 +0x173~+0x18a` |
| E17 | 고리로 돌아옴: R12D ≠ 0 이면 곧장, 아니면 `message` 가 `0x113 / 0x118` (타이머 → 보이기 E11 ①) · `0x104` (→ 빈 틈) · 그 밖은 곧장 → **`HMValidateHandleNoSecure(hwnd)`** (0 → 빠짐) → E5 | 매 바퀴 | 0 → **빠짐 (반환 0)** | — | `DialogBox2 +0x2ba~+0x2cc +0x45e~+0x474 +0x119~+0x124` |
| E18 | **E5 에서 `pdlg+0x18` 비트 0 → 빠짐**: `[gpsi+0x764] & 0x8000` 이면 `NtUserNotifyWinEvent(0x11, hwnd, 0, 0)` (0x1028) · `HMValidateHandleNoSecure(hwnd)` (0 → **반환 0**, 값을 못 읽음) · **`RSI = [[사본+0x128]+8]+0x20` 를 먼저 읽음** (`pdlg` NULL 이면 0) | — | NotifyWinEvent 반환 안 봄 | `gpsi+0x764` · 사본 `+0x128` · `pdlg+0x20` | `DialogBox2 +0x389~+0x3bd +0x39f~+0x3a7 +0x42d` |
| E19 | **`NtUserDestroyWindow(hwnd)` (0x1095)** | 항상 (빠진 뒤) | **반환 안 봄** (`+0x3cb NOP` 뒤 `RAX` 안 씀) | — | `DialogBox2 +0x3c4/+0x3cb` |
| E20 | 소유자 있으면 `NtUserGetThreadState(0xa)` (0x1000) → ≠0 이면 `NtUserQueryWindow(소유자, 7)` (0x100e) → 0 이면 `NtUserSetForegroundWindow(소유자)` (0x155b) | 소유자 ≠ 0 (표본 0) | `GetThreadState` · `QueryWindow` 반환을 **봄** (아래 갈래만 정함), 마지막 것은 안 봄 | — | `DialogBox2 +0x3d0~+0x3ea +0x432~+0x45a` |
| E21 | **반환**: `RAX = pdlg+0x20` (E18 에서 읽어 둔 값) → `InternalDialogBox` → `DialogBoxIndirectParamAorW` → `DialogBoxParamW` | — | 좁히기 · 부호 바꾸기 없음 (64비트 그대로) | — | `DialogBox2 +0x3ea~+0x40d` · `InternalDialogBox +0x8f~+0xad` · `DialogBoxIndirectParamAorW +0x6c` |

**한 바퀴 syscall 수 (가장 짧은 꼴, 소유자 0 · TEB `+0x860` = 0 · 훅 0 · `gpsi+0x764` 비트 0)**: `NtUserPeekMessage` ×1 → (`IsDialogMessageW`~앱 프로시저 안은 0번) → `EndDialog` 의 `NtUserSetWindowPos` · `NtUserPostMessage` → `NtUserDestroyWindow` = **4번**. `NtUserPeekMessage` 가 처음에 0 을 주면 `NtUserShowWindow` 한 번이 더.

---

## 2. 칸 표 보탬 — 142 4절에 없는 칸만

### 2-1. MSG (`NtUserPeekMessage` 가 채우는 0x30 B)
| 칸 | 크기 | 읽는 줄 | 쓰는 줄 | 0 가능 / 있어야 할 값 `[추측: 이름]` |
|---|---|---|---|---|
| `+0x00` hwnd | 8 | `IsDialogMessageW +0x9f/+0xa8` · `DispatchMessageWorker +0x2f` · `UserCallWinProcCheckWow` 인자 (`+0x1cb`) | `DispatchMessageWorker +0x4f MOV [RBX],RCX` (사본 `+0x0` 으로 정규화) | **0 이면 안 됨** (`IsDialogMessageW` 0 반환 · `DispatchMessageWorker` 0 반환 → 아무 일 없이 고리만 돎). 모달 대화상자 hwnd (또는 그 자손) |
| `+0x08` message | 4 | `DialogBox2 +0x274/+0x2c3` · `IsDialogMessageW +0x50 +0xf2` · `DispatchMessageWorker +0x22 +0x54` · `TranslateMessage +0x18` · `_PeekMessage +0x72` | — | **`0x111` (WM_COMMAND)**. `& 0xfffe0000` 이면 `SetLastError(0x57)` · 거부. `MessageTable & 0x200` (`0x87` `0x110` 등) 이면 `SetLastError(0x487)` · 거부 |
| `+0x10` wParam | 8 | `DialogBox2 +0x379` (WM_QUIT) · `IsDialogMessageW +0x175 +0x190` (키) · 앱 프로시저 | `_PeekMessage +0x80` (`message` 가 `0xcc`/`0x102` 면 `& 0xffff`) · `DispatchMessageWorker +0x1f5` (되씀) | WM_COMMAND: `MAKEWPARAM(ID, BN_CLICKED=0)` = ID (`TranslateMessage +0x12 CMP word [RCX+0x10],0xe5` — **ID 가 0xE5 (229) 면 IMM 훅 `[user32+0xd96f0]` (`_InitializeImmEntryTable +0x710` 이 씀) 을 부름** `[공백: 그 훅 몸 안 읽음]`) |
| `+0x18` lParam | 8 | `DispatchMessageWorker +0x1b5` · 앱 프로시저 | — | WM_COMMAND: 그 ID 의 컨트롤 hwnd (못 찾으면 0 — 앱이 `GetDlgCtrlID((HWND)lParam)` 같은 걸 하면 달라짐) |
| `+0x20` time · `+0x24` pt · `+0x2c` | 4/8/4 | 이 길에서는 **안 읽음** | — | 0 가능 (`GetMessageTime` 류는 TEB `+0x8a0` 쪽 `[추측]`, 이 길 밖) |

### 2-2. 창 사본 (`pwnd`) 보탬
| 오프셋 | 크기 | 이름 `[추측]` | 읽는 줄 | 쓰는 줄 | 0 가능 / 있어야 할 값 |
|---|---|---|---|---|---|
| `+0x10` 비트 5 (`0x20`) | 1 | 깃발 | `DialogBox2 +0x289` | 커널 | 0 (켜지면 Ctrl+C / Ctrl+Ins 처리 `+0x32e~+0x362`: `SendMessageW(WM_COPY 0x301)` · `GetKeyState` → `NtUserGetKeyState` 0x1002) |
| `+0x1a` 비트 0 | 1 | ExStyle 비트 16 (`WS_EX_CONTROLPARENT` `[추측]`) | `IsDialogMessageW +0xb0` · `GetParentDialog +0x28` · `_NextControl +0x7b` (142) | 커널 | 0 (켜지면 중첩 대화상자 · 부모 대화상자 찾는 갈래) |
| `+0x1f` 비트 3 (`0x08`) | 1 | `WS_DISABLED` `[추측]` | `IsDialogMessageW +0x98c18 TEST [RAX+0x1f],8` · `DefDlgProcWorker +0x90b` | 커널 | 컨트롤: 0 (**켜져 있으면 키 길은 `NtUserMessageBeep` (0x14dd) 만 하고 `WM_COMMAND` 를 안 보냄** — 끝없는 고리 꼴 F8) |
| `+0x1f` 비트 4 (`0x10`) | 1 | `WS_VISIBLE` | `DialogBox2 +0x96/+0xa4` (→ `R12D`) · `InternalCreateDialog +0xf68` (142) | 커널 | 0 가능 (0 이면 첫 빈 틈에 `NtUserShowWindow(hwnd,1)`) |
| `+0x1f` 비트 6·7 | 1 | `WS_CHILD` / `WS_POPUP` | `EndDialog +0x4b~+0x52` · `IsDialogMessageW +0xd0` · `GetParentDialog +0xe` · `_GetChildControl +0xc` | 커널 | 대화상자 = 비트 6 꺼짐 · **컨트롤 = 비트 6 켜짐** (그래야 `+0x30` 부모 사슬을 따라 대화상자에 닿음). `+0x40` (소유자) 는 비-자식에서만 읽음 |
| `+0x2a` | 2 | fnid (142) | **`DefDlgProcWorker +0x32 +0x41`** | 커널 `SetDialogPointer +0xf2` | **보탬: `0` 또는 `0x2a4` 가 아니면 `DefDlgProcWorker` 가 앱 프로시저를 안 부르고 0 반환** (`+0x45 JNZ → 조각 +0x211 XOR EAX,EAX`). 그래서 `NtUserSetDialogPointer` 가 `0x2a4` 를 적는 게 필수 (142 6번) |
| `+0x30` | 8 | 부모 (커널 주소) | `GetParentDialog +0x14` · `IsDialogMessageW +0xd9` · `EndDialog +0x58` (자식일 때) · `_FindDlgItem` | 커널 | 컨트롤: 대화상자 사본 주소 (또는 `[+8]` 규칙으로 변환 가능한 값). 대화상자: 0 |
| `+0x38` | 8 | 첫 자식 | `GetDlgItem +0x1e` · `_GetDlgItem +0x5` · `_NextControl +0xdd` (142) | 커널 | **컨트롤을 만든 순서 (템플릿 차례) 의 첫째** `[추측: 탭 순서 = 템플릿 차례 — 연결 순서를 커널이 어떻게 만드는지는 win32k 몫]` |
| `+0x40` | 8 | 소유자 (비-자식) | `EndDialog +0x121` | 커널 | 0 (표본 소유자 0) |
| `+0x48` | 8 | **다음 형제** | `GetDlgItem +0x40` · `_GetDlgItem +0x13` · `_NextControl` | 커널 | 마지막 컨트롤: 0. 아니면 다음 컨트롤 (`포인터 − [자기+8] + 자기` 로 변환) |
| `+0xd8` | 8 | `GWLP_USERDATA` | `GetWindowData +0xc3` (`GetWindowLongPtrW(h, −21)` · `_GetWindowLongPtr +0x33 JS`) | **쓰기는 (보통) syscall** `NtUserSetWindowLongPtr (0x15da)` (`_SetWindowLongPtr +0x1c2 CMP g_systemCallFilterId,5; +0x1c9 JNZ → +0x261`) — 사본 `+0xd8` 은 커널이 씀 `[추측]` | 0 가능 |
| `+0x140` | 4 | 컨트롤 ID (142) | `GetDlgItem +0x38` · `_GetDlgItem +0xb` · `_FindDlgItem +0x49` · `GetDlgCtrlID +0x3f` · `GetWindowData +0x57` | 커널 | 컨트롤마다 `hMenu` 인자로 준 ID. **0 이면 `GetDlgItem(0)` 이 첫 ID 0 컨트롤에 맞음** |

### 2-3. 여분 블록 (`[사본+0x128]`) · `pdlg` 보탬
| 칸 | 크기 | 읽는 줄 | 쓰는 줄 | 뜻 · 0 가능 |
|---|---|---|---|---|
| 여분 `+0x00` | 8 | `DefDlgProcWorker +0x62` · `+0x1f7` (앱이 TRUE 반환 시 돌려줌) · `_GetWindowLongPtr +0x184` | `DefDlgProcWorker +0x207` (→ `_SetWindowLongPtr(h,0,0,0)` — 보통 **syscall `NtUserSetWindowLongPtr`**, `[여분+0x0]` 이 0 이 아닐 때만 부름) · 사용자 모드 쓰기는 `g_systemCallFilterId == 5` 일 때만 (`_SetWindowLongPtr +0x169~+0x16c`) | `DWLP_MSGRESULT`. 0 으로 시작 |
| 여분 `+0x10` | 8 | `_GetWindowLongPtr +0x1ef` (사용자 모드) | **보통 syscall `NtUserSetWindowLongPtr` (커널이 씀)** · 사용자 모드 쓰기는 `g_systemCallFilterId == 5` 일 때만 (`_SetWindowLongPtr +0x8e~+0x92`) | `DWLP_USER`. 0 으로 시작 (앱이 `WM_INITDIALOG` 에서 씀) |
| `pdlg+0x00` (142) | 8 | `DefDlgProcWorker +0x9c` | **`_SetWindowLongPtr +0xbb~+0x12d` (`DWLP_DLGPROC` 쓰기: `pdlg+0x0 = 새 값`, `pdlg+0x18` 비트 2 를 `bAnsi` 로 맞춤, 안 맞으면 `GetCPD` 로 변환 thunk)** | 앱 프로시저 |
| `pdlg+0x18` (142) | 4 | `DialogBox2 +0x13e` · `EndDialog +0x16c` · `DefDlgProcWorker +0x598 +0x684 +0x9fa +0xa37` · `_SetWindowLongPtr +0xc6` | **`DialogBox2 +0x8f` (비트 1)** · **`EndDialog +0x70` (비트 0)** · `_SetWindowLongPtr +0x144` (비트 2) | 비트 0 = 끝남 · **비트 1 = 소유자가 이미 꺼져 있었음 (`InternalDialogBox +0x115`), 켜져 있으면 `EndDialog` 가 `NtUserEnableWindow(소유자,1)` 를 안 함** · 비트 2 = ANSI · 비트 3 = DPI (142) |
| `pdlg+0x20` (142) | 8 | `DialogBox2 +0x3bd` (**반환값**) · `DefDlgProcWorker +0xa4b` (`DM_GETDEFID`) `+0x9e4~+0xa1f` (`DM_SETDEFID`) · `+0x4e5` | `EndDialog +0x7f` (**64비트 결과**) · `DM_SETDEFID` (조각 `+0x9799c`) · `InternalCreateDialog +0x6fa` (처음 `1`) · 조각 `+0x310~+0x31e` (`DLGC_DEFPUSHBUTTON` 컨트롤 ID) | **기본 단추 ID 겸 결과값**. `DM_GETDEFID` 는 `pdlg+0x18` 비트 0 이 켜졌거나 `pdlg+0x20 == 0` 이면 0, 아니면 `LOWORD \| 0x534b0000` |

### 2-4. TEB · `gpsi` · user32 전역 보탬
| 칸 | 읽는 줄 | 뜻 · 필요 |
|---|---|---|
| TEB `+0x860` (포인터) | `GetActiveWindow +0x9` (`[+0x28]`) · `GetFocus +0x9` (`[+0x20]`) · `DialogBox2 +0x15b` (`[+0x0 +0x4 +0x8 +0x14]`) · `PeekMessageW +0x43` · `UserCallDlgProcCheckWow +0x3a~+0x53` (`[+0x0]` 비트 2) | **0 이면 전부 "없음"**: `GetActiveWindow` = 0 · `GetFocus` = 0 · 빠른 길 안 탐 · 훅 비트 안 봄 (142 의 "일반 길" 이 이것). 가리키는 곳 모양은 `[추측: 스레드 입력 상태 블록 — 창 활성 `+0x28` · 포커스 `+0x20` · 깨움 비트 `+0x4 +0x8` · 마지막 확인 시각 `+0x14`]` |
| TEB `+0x898` (word) · `+0x89c/+0x89d` · `+0x8a0~+0x8cf` (MSG 크기 덩어리) | `DispatchMessageWorker +0x2b5 (+0x8b0) +0x428 (+0x89c) +0x4f6 (+0x898)` · `_PeekMessage +0x8a` | DBCS 이중 바이트 글자 (`gpsi` 비트 1) 때만. 0 가능 |
| `gpsi+0x764` (dword) | `DialogBox2 +0x2f5 +0x390` | 비트 `0x8000` 켜지면 `NtUserNotifyWinEvent` (0x1028) 를 `DialogBox2` 가 두 번 (시작 `0x10` · 끝 `0x11`) 부름. **0 이어도 됨** |
| `gpsi+0x88c` 비트 0 · `+0x1b50` · `+0x1b5c` | `DialogBox2 +0xaf` · `GetCapture +0x7` · `IsDialogMessageW` 조각 (`+0x98b9b~+0x98bb6`) | 0 이면 모두 건너뜀 (142 의 `+0x1b56 +0x1b66` 와 같은 블록 안) |
| user32 전역 `g_systemCallFilterId` (`0xd9970`) | `_PeekMessage +0x65` · `_SetWindowLongPtr +0x6a +0x14d +0x1c2` · `GetWindowData +0xe5` · `GetMessageW +0x27` | **보통 5 가 아님 (그 값이면 이 장의 길이 아래처럼 감)**: ≠5 → `PeekMessage` 훅 길 안 탐 · **`SetWindowLongPtr` 는 syscall `NtUserSetWindowLongPtr`**. =5 → `PeekMessage` / `GetMessage` 가 `IsLocalHookInstalled(3)` 갈래 · `SetWindowLongPtr` 가 사용자 모드 쓰기. 값을 정하는 쪽은 앞 sast (139 의 연결) 에서 본 것과 같음 `[공백: 이번엔 안 따라감]` |
| user32 전역 `gcCallUserApiHook` (`0xda734`) · `ghmodUserApiHook` (`0xda7c0`) · `gfUserApiHook` (`0xda730`) · `guah+0x70~+0x88` (`0xd90c0~0xd90d8`) | `UserCallDlgProcCheckWow +0xcf~+0xf3 +0x11d~+0x14e` · `DefWindowProcWorker +0x0` | `gcCallUserApiHook` 는 **LOCK 증감 (쓰기 가능한 `.data`)**, 나머지는 0 이어야 (0 이면 `UserCallDlgProcCheckWow` 가 곧장 앱 프로시저를 CFG 로 부르고 반환 0) |
| user32 비트표 `+0xda568 (한도) +0xda570 (포인터) +0xda578 (한도) +0xda580 (포인터)` | `RealDefWindowProcWorker +0x46 +0x4e +0x76 +0xcd` | user32 는 **안 씀 (WRITE 참조 0 — `refs`)**, 커널이 채움 `[공백: 값은 win32k 몫]`. **두 한도가 모두 0 이면 `RealDefWindowProcWorker` 는 어떤 메시지든 syscall 없이 0 반환 (`+0x56`)** |

---

## 3. 금지 꼴 — 끝없는 고리 · 접근 위반

고리가 **빠지지 못하는** 꼴 (코드 근거): 빠지는 길은 E5 `pdlg+0x18` 비트 0 (앱이 `EndDialog` 를 불러야) · `pdlg` NULL · hwnd 사라짐 (E3/E11/E17) · `WM_QUIT` (E8) **넷뿐**이다. 그 밖에 `DialogBox2` 안에 횟수 한도 · 시간 한도는 **없다** (TEB `+0x860` ≠ 0 일 때의 100 번마다 `NtYieldExecution` 만 있음).

| # | 꼴 | 왜 끝없나 · 죽나 (줄) | 권하는 처리 |
|---|---|---|---|
| F1 | `NtUserPeekMessage` 가 늘 **0** + `NtUserWaitMessage` 가 즉시 0 (**지금 우리 기본 대답 = 이 꼴**) | `DialogBox2 +0x270 JZ → +0x20a` → (보이면) `+0x23f NtUserWaitMessage` → `+0x24b JMP +0x119` → 맨 위. 반환 둘 다 안 봄 → **바쁜 고리** (CPU 만 씀, 명령 수 한도까지) | `NtUserWaitMessage` 는 "이 길에서 불리면 진단 정지" (아래 stub 14) |
| F2 | `NtUserPeekMessage` 가 1 을 주는데 **MSG 를 안 채움** (hwnd 0 · message 0) | `IsDialogMessageW +0x9f JZ` 0 → `TranslateMessage` 0 → `DispatchMessageWorker +0x35 JZ → +0x29a → +0xa8 JZ` 0 → `DialogBox2 +0x2ba` → 맨 위. 같은 효과 (바쁜 고리, `NtUserWaitMessage` 안 거침) | 채우기 |
| F3 | hwnd 가 `ValidateHwnd` 에서 0 (항목 · 고유 수 · TEB `+0x820`/`+0x828` 어긋남) | `DispatchMessageWorker +0x46 JZ → +0x16b` 0 — F2 와 같음. 반대로 `HMValidateHandleNoSecure(hDlg)` 도 0 이면 **빠짐 (반환 0)** — 의도치 않게 빠질 수 있음 | 142 4절 칸 맞추기 |
| F4 | `message & 0xfffe0000` | `IsDialogMessageW`/`DispatchMessageWorker +0x22 JNZ → +0x587` `SetLastError(0x57)` 0 → F2 | 하지 말 것 |
| F5 | `message` 가 `MessageTable & 0x200` 인 것 (**`WM_INITDIALOG 0x110` · `WM_GETDLGCODE 0x87`** 등) 을 큐로 줌 | `IsDialogMessageW +0x70 JNZ → +0x272` `SetLastError(0x487)` 0 + `DispatchMessageWorker +0x71 JNZ → +0x46b; +0x87 JNZ → +0x356` 0 → F2. 이런 메시지는 **보내기 (`SendMessage`) 로만** 가야 함 | 큐로 주지 말 것 |
| F6 | msg.hwnd 가 대화상자도 자손도 아닌 창 (또는 컨트롤인데 `+0x1f` 비트 6 · `+0x30` 부모 사슬이 끊김) | `IsDialogMessageW +0xd0~+0xf0 JNZ → +0x142` 0 → 고리가 직접 `Translate` + `Dispatch` → 컨트롤 프로시저 (stub 이 안 부르게 사본 `+0x12` 비트 2 를 켜 두면 `NtUserDispatchMessage` 0 반환) → F2 | msg.hwnd = 대화상자 |
| F7 | 앱이 `EndDialog` 를 안 부름 (입력 검사 거절 · `WM_COMMAND` 를 무시 · 다른 ID 만 처리) + stub 이 같은 메시지를 계속 줌 | 매 바퀴 `NtUserPeekMessage` 1 → 앱이 무시 → 맨 위 … 끝없음 | **횟수 한도 두고 다른 메시지로 바꾸기** `[추측: 정책]`: 1 번째 `WM_COMMAND(기본 ID)` → 2 번째 `WM_COMMAND(IDCANCEL=2)` → 3 번째 `WM_CLOSE (0x10)` (`DefDlgProcWorker +0x448~+0x467`: 앱이 FALSE 면 **`PostMessageW(hDlg, WM_COMMAND, IDCANCEL …)` 를 큐로 보냄** — `NtUserPostMessage` 가 큐에 넣어야 다시 나옴) → 4 번째 `WM_QUIT` (E8 로 빠짐, 반환 = `pdlg+0x20` = 처음 값 1, **앱 메인 루프도 `NtUserPostQuitMessage` 로 끝나게 됨**) |
| F8 | 기본 단추 컨트롤이 `WS_DISABLED` 인데 **`WM_KEYDOWN(VK_RETURN)` 길** 로 줌 | `IsDialogMessageW +0x98c18` → `NtUserMessageBeep` 후 TRUE, `WM_COMMAND` 안 보냄 → F7 | `WM_COMMAND` 를 곧장 줌 (이 길은 안 막음) |
| F9 | `pdlg` 가 NULL (사본 `+0x128` 이 비었거나 `[+8]` 이 NULL) | `DialogBox2 +0x83/+0x87` (`[RDX+0x18]` 읽음) 접근 위반 (142 와 같음). `+0x138 JZ → +0x389` 는 고리 안에서만 | 142 stub 6번 |
| F10 | `NtUserDestroyWindow` 뒤 `pdlg` · 여분 블록을 **해제 · 재사용** | `+0x3bd` 에서 **이미 읽은** 뒤라 괜찮음 (읽기가 먼저). 단 `DefDlgProcWorker +0x31a~+0x3ae` (`WM_NCDESTROY` 에서 `FreeLookasideEntry` · `NtUserSetDialogPointer(hwnd, 0)`) 를 stub 이 안 보내면 `pdlg` 가 안 풀릴 뿐 (누수, 무해) | — |
| F11 | 항목 `+0x8` ≠ TEB `+0x78` (TEB `+0x78 = 0` 인데 항목은 0 이 아니거나 그 반대) | `GetWindowLongPtrW(hDlg, 0x10)` 이 `_GETPTI == PtiCurrent` 에서 어긋나 `NtUserMessageCall` (code `0x5a`) 로 감 (`_GetWindowLongPtr +0x1d6~+0x1e6 JNZ → +0x260`) → stub 이 0 을 주면 앱이 `DWLP_USER` 로 읽은 포인터가 NULL → 앱 쪽 널 읽기 `[추측]` | 142 의 TEB `+0x78` 규칙 지키기 |

---

## 4. 근거 풀이

### 4-1. (a) Enter 가 고리에 오는 모양 — `IsDialogMessageW` 의 `WM_KEYDOWN` 길 `[정적]`
- `DialogBox2` 는 `_PeekMessage(&msg, 0, 0, 0, 1, 0, 0)` 로 받은 메시지를 **먼저 `IsDialogMessageW(hDlg, &msg)` 에 준다** (`+0x293~+0x29b`); TRUE 면 자기가 번역 · 배달하지 않는다 (`+0x2a2 JNZ → +0x2ba`).
- `IsDialogMessageW`: `+0xf2 MOV ECX,[RDI+8]` 에서 `message − 0x100 == 0` (`WM_KEYDOWN`) 이면 `+0x175`. `SendMessageW(msg.hwnd, WM_GETDLGCODE 0x87, wParam, &msg)` (`+0x184/+0x189`) → 답의 `0x4` (DLGC_WANTALLKEYS) 가 있으면 **그냥 번역 · 배달** (`+0x129`). 없으면 wParam 에서 `3` · `6` · `4` 를 차례로 빼며 비교 (`+0x190~+0x1ac`): `VK_CANCEL (3)` → 조각 `+0x369` (`_FindDlgItem(hDlg, EBP=2)` = IDCANCEL), `VK_TAB (9)` → 탭 이동, **`VK_RETURN (0xd)` → `+0x1ea`**, `VK_ESCAPE (0x1b)` → 조각 `+0x369` (IDCANCEL).
- `VK_RETURN` 갈래: `GetFocus()` (`+0x1ea`, TEB `+0x860` = 0 이면 0 → 아래 ② 로) → 포커스가 있으면 `SendMessageW(포커스, 0x87, 0, 0)` (`+0x209/+0x20e`) 의 `0x10` 비트 (`+0x211 TEST CL,0x10`) 가 켜졌으면 `GetDlgCtrlID(포커스)` (`+0x21d`) 와 `ValidateHwnd(포커스)` (`+0x227`) 로 ① **그 컨트롤의 ID** → 조각 `+0x98c0f`.
  ② (포커스 없음 · 기본 단추 아님) `SendMessageW(hDlg, DM_GETDEFID 0x400, 0, 0)` (`+0x98b4c/+0x98b51`), 답의 윗 16비트가 `0x534b` (`DC_HASDEFID`) 면 아랫 16비트 (`+0x98b5c`), 아니면 `EAX = 1` (`+0x98b61`) → `EBP = AX` (`+0x98b66`) → 조각 `+0x369`: `_FindDlgItem(hDlg 사본, EBP)` (`+0x36e`).
  조각 `+0x98c0f`: 못 찾음 (`TEST RAX; JZ → +0x98b0a`) → **`SendMessageW(hDlg, 0x111, ID, 0)`**. 찾음 + `+0x1f` 비트 3 (꺼짐 표시) 켜짐 → `NtUserMessageBeep(0)` 후 TRUE (**WM_COMMAND 안 보냄**). 찾음 + 멀쩡 → **`SendMessageW(hDlg, 0x111, ID, 컨트롤 hwnd)`** (`+0x98b19`) → `+0x98c31 JMP +0x13b` (`MOV EAX,1`, TRUE).
- **`DM_GETDEFID` 답** (`DefDlgProcWorker` 메시지 `0x400` 갈래 `+0xa24~+0xa55`): `GetParentDialog` → 그 `pdlg` 의 `+0x18` 비트 0 이 켜졌으면 0 · `pdlg+0x20 == 0` 이면 0 · 아니면 `MOVZX EAX, word [pdlg+0x20]` `OR 0x534b0000`. `DM_SETDEFID (0x401)` 는 `pdlg+0x20 = wParam` (조각 `+0x9799c`) + `NotifyWinEvent(0x8011)` → **`pdlg+0x20` 은 기본 ID 와 `EndDialog` 결과값을 같이 쓴다.** `EndDialog` 뒤에는 `+0x18` 비트 0 때문에 `DM_GETDEFID` 가 0.
- 진짜 입력으로 `WM_COMMAND` 가 **큐에서** 오는 유일한 길: `DefDlgProcWorker +0x448~+0x467` — 앱이 `WM_CLOSE (0x10)` 에 FALSE 를 돌려주면 `_GetDlgItem(hDlg, 2)` → 있고 안 꺼졌으면 `PostMessageW(hDlg, 0x111, 2, 그 컨트롤)` (`+0x46c`). 즉 "`NtUserPeekMessage` 가 `message = 0x111` 을 주는 모양" 은 진짜 흐름에도 있다 (키 길과 달리 큐로 옴).
- 마우스 클릭은 컨트롤 프로시저가 `WM_COMMAND` 를 **보냄** (`xxxButtonNotifyParent`, 이름만 — 본문 안 읽음).

### 4-2. (b) `WM_COMMAND` 를 곧장 줬을 때 — 반환 검사 · syscall · 읽는 칸
위 1절 E9~E16. 요약:
- `IsDialogMessageW`: `ValidateHwndNoRip(hDlg)` → 사본. `MessageTable[0x111] = 0` (0x200 없음). `CallMsgFilterW(&msg, 0)` = 0 (TEB `+0x820`→`+0x10` · TEB `+0x838` 0). `msg.hwnd == hDlg` 면 `+0xc6 JZ → +0xf2`. `0x111 − 0x100 = 0x11` 는 `2` 씩 세 번 빼도 · `0xfb` 도 안 맞아 `+0x129` 기본 갈래.
- `TranslateMessage(&msg)`: 먼저 `word [msg+0x10] == 0xe5` 를 봄 (ID 가 `0xe5` 면 IMM 훅 갈래, 위 2-1) → 메시지가 `0x100 0x101 0x104 0x105` 가 아니면 `TEST ECX,0xfffe0000` 만 보고 0 (**syscall 0**). `0x100~0x105` 키 메시지만 `NtUserTranslateMessage` (0x100b).
- `DispatchMessageWorker`: 위 E12. 대화상자는 사본 `+0x12` 비트 2 = 0 · `+0x78` = `DefDlgProcW` 라 `NtUserDispatchMessage` 를 **안 탄다**. `+0x1d8` 의 `UserCallWinProcCheckWow` 는 142 6절대로 syscall 0.
- `DefDlgProcWorker`: E13 · E16. 앱 프로시저 호출 직전 읽는 것 = `[사본+0xe0]` · `[[사본+0x128]+8]+0x0`. `WM_COMMAND` (`0x111`) 는 `DefDlgProcWorker` 의 앱 호출 뒤 **개별 갈래 없이** `+0x143~+0x160 → +0x97e~+0x9b8 → +0x173 DefWindowProcWorker` (앱이 FALSE 일 때) — `RealDefWindowProcWorker` 의 사용자 모드 점프표는 `0x111` 에서 `return 0` (`+0x56`) `[정적: 점프표 값 직접 읽음]`, 단 비트표 (커널이 채움) 가 `0x111` 을 커널로 보내라 하면 `gapfnScSendMessage[MessageTable[0x111] & 0xff = 0]` 길 `[공백]`.
- **앱의 반환**: `DefDlgProcWorker` 는 앱의 `DLGPROC` 반환 (`[rbp-0x28]`) 이 ≠0 이면 `[사본+0x128]+0x0` (`DWLP_MSGRESULT`) 을 돌려주고 (사본 `+0x12` 비트 0 = 켜져 있어서), 0 이면 기본 처리. `IsDialogMessageW` · `DispatchMessageWorker` 는 그 값을 안 봄.
- **`EndDialog` 가 쓰는 칸**: `pdlg+0x18 |= 1` (`+0x70`) · `pdlg+0x20 = n` (`+0x7f`). 읽는 칸: 사본 `+0x1f` (자식 판별) · `+0x30` / `+0x40` (부모 / 소유자) · `pdlg+0x18` 비트 1 (소유자 다시 켤지) · 활성 / 포커스 (TEB `+0x860`).

### 4-3. (c) 끝 · 반환 — 반환을 user32 가 보는 / 안 보는 syscall 표
| syscall | 부르는 곳 | 반환을 보나 | 0 · 아무 값이면 |
|---|---|---|---|
| `NtUserPeekMessage` 0x1001 | `_PeekMessage +0x38` | **본다** (0 / 0 아님) | 0 = 빈 틈 (E11) |
| `NtUserWaitMessage` 0x100a | `DialogBox2 +0x23f` | 안 봄 | 바쁜 고리 (F1) |
| `NtUserShowWindow` 0x1052 · `UpdateWindow` · `NtUserNotifyWinEvent` 0x1028 | `DialogBox2 +0x2da +0x2e9 +0x312 +0x41c` | 안 봄 | 같음 |
| `NtUserPostQuitMessage` 0x14ee | `DialogBox2 +0x37d` | 안 봄 | 같음 |
| `NtUserSetWindowPos` 0x1021 · `NtUserPostMessage` 0x100d · `NtUserSetActiveWindow` 0x10cd · `NtUserZapActiveAndFocus` 0x15d6 · `NtUserSetFocus` 0x104c · `NtUserEnableWindow` 0x1416 | `EndDialog +0xa9 +0xd9 +0xc2 +0x113 +0x145 +0x17e` | 안 봄 (호출 뒤 `NOP`) | 같음 (`EndDialog` 는 늘 1 반환) |
| `NtUserDestroyWindow` 0x1095 | `DialogBox2 +0x3c4` | 안 봄 | 같음 |
| `NtUserGetThreadState(0xa)` 0x1000 · `NtUserQueryWindow(소유자,7)` 0x100e | `DialogBox2 +0x3da +0x43a` | **본다** (소유자 있을 때만) | 소유자 0 이라 안 탐 |
| `NtUserSetForegroundWindow` 0x155b | `DialogBox2 +0x44e` | 안 봄 | — |
| `NtUserMessageBeep` 0x14dd | `IsDialogMessageW +0x9885e +0x98c24` · `DefDlgProcWorker +0x913` | 안 봄 | — |
| `NtUserCallMsgFilter` 0x1013 | `CallMsgFilterW +0xb4/+0xbb` | **본다** (≠0 이면 `IsDialogMessageW` 가 TRUE 로 끝) | 훅 0 이라 안 탐 |
| `NtUserSetWindowLongPtr` 0x15da | `_SetWindowLongPtr +0x261~+0x273` (`g_systemCallFilterId ≠ 5`) · `DefDlgProcWorker +0x207` (`[여분+0x0] ≠ 0` 일 때) | **앱이 옛 값을 쓰면 본다** (user32 안 호출자 `DefDlgProcWorker` 는 안 봄) | 이전 값을 돌려줘야 `x = SetWindowLongPtr(…)` 가 맞음 |
| `NtUserMessageCall` 0x1005 | `RealDefWindowProcWorker +0xc6` (비트표) · `GetWindowLongPtr` 항목 `+0x8` ≠ TEB `+0x78` 일 때 | 반환 그대로 줌 | 0 으로 충분 |
| `NtUserGetMessage` 0x1004 | **`DialogBox2` 는 안 부름** (`_calls.tsv` 에 없음). 앱 자기 루프 · `GetMessageW +0x1b` | **그대로 돌려줌** (0 = WM_QUIT 라 앱 루프 끝 · −1 = 오류인데 앱이 `while (GetMessage())` 면 영원히 돎 · 그 밖 = 메시지 있음) | **−1 은 주지 말 것** |

`WM_ENTERIDLE (0x121)` 은 소유자 ≠ 0 일 때만 (E11 ②). 표본 80/80 이 부모 0 이라 이 길을 안 탄다.

### 4-4. (d) A 판 · 모달 아닌 판이 갈라지는 줄
| 자리 | `DialogBox*W` | `DialogBoxIndirectParamA` / `DialogBoxParamA` | `CreateDialogParam*` |
|---|---|---|---|
| 깃발 넘김 | `DialogBoxParamW +0x5d AND [RSP+0x28],0` | `DialogBoxIndirectParamA +0x9 MOV [RSP+0x28],2` (`DialogBoxParamA` 도 `2`) | `CreateDialogParamW +0x74 MOV [RSP+0x28],EBX(0)` · `CreateDialogParamA +0x5b MOV [RSP+0x28],2` |
| 깃발 가공 | `DialogBoxIndirectParamAorW +0x4a AND EAX,2; +0x50 OR EAX,1` | 같음 → 3 | `CreateDialogIndirectParamAorW +0x33 AND EAX,2; +0x39 OR EAX,1` → W 1 · A 3 |
| 다음 | `InternalDialogBox +0x70 InternalCreateDialog` → **`+0x8a DialogBox2` (고리)** | 같음 | **`InternalCreateDialog` 에서 끝 (hwnd 돌려줌, `InternalDialogBox` · `DialogBox2` · 소유자 끄기 · 모래시계 커서 안 탐)** |
| 자원 없을 때 | −1 (`DialogBoxParamW +0x39 JZ → RBX=−1`) | −1 | **0** (`CreateDialogParamW +0x37 JNZ` 안 타면 `RBX=0` 반환) |
| `InternalCreateDialog` 안 | 깃발 비트 1 = 0 | `+0x5da MOV R14D,[RBP+0x8]; AND R14D,2` → **확장 스타일 `\| 0x80000000`** (`+0x5e5~+0x5f4`) · **`+0x713/+0x72c pdlg+0x18 \|= 4`** | 같은 깃발 규칙 |
| `+0xba0 TEST [RBP+0x8],4` (끝 검사 건너뜀) | 이 호출자들은 4 를 안 넣음 → 늘 끝 검사를 함 | 같음 | 같음 |
| 보낼 때 | — | **`SendMessageWorker +0x14c~+0x16f`** (`~[사본+0x12] >> 3 & 1` = 창이 유니코드 ↔ 보내는 쪽 `bAnsi == 0`): 같으면 건너뜀 (`+0x16f JZ → +0x28f`). 다르면 `+0x175~+0x1a2` 점프표 (메시지 − `0x2f`, `0x120` 까지) · `0x140` (`+0x17d JZ → +0x1a8`) · `0x286` (`+0x1f2 JZ → +0x225`) · `≥ 0x400` (`+0x1fa JNC → +0x28f` 건너뜀) · 기본 갈래 `+0x200 → +0x214 TEST word [MessageTable+msg*2],R9W (0x100, `+0x11d` 에서 넣음)` 가 꺼져 있으면 `+0x21d JZ → +0x29c` 변환 · syscall 없이 지나감 | — |
| 갈래 값 (표를 파일에서 읽음) | | 기본 갈래 (변환 · syscall 0): `0x30 0x31~ 0x87 0x110 0x111 0x112 0x100` 등 대부분. 글자 변환 갈래 `+0x225` (`EDX=0x80000000`): **`0x2f 0xcc 0x102 0x103 0x106 0x107 0x10f 0x120`** 와 `0x286`. `+0x1a8` 갈래: `0xb0 0xb1` | |
| 사본 `+0x12` 비트 3 (ANSI) 를 누가 켜나 | — | **커널 `[추측]`**: `xxxCreateWindowEx` 가 마지막 인자 · 클래스 깃발을 보고 `SetOrClrWF(…, 0x208, …)` 를 부름 (win32kfull `xxxCreateWindowEx +0xa3a MOV R8D,0x208` 이 클래스 `[+8]+6` 의 비트 1 에 걸림 · `+0x1bf MOV EAX,0x320` / `+0x8e6 MapClientNeuterToClientPfn(…, word [RSP+0xa4])` 가 ANSI 일 때 다른 클라이언트 프로시저를 고름). 확장 스타일 `0x80000000` 이 어느 비트로 가는지는 **이번엔 안 따라감** | |
| `DefDlgProcA` | | `DefDlgProcA +0x24 MOV [RSP+0x20],1` 로 **`DefDlgProcWorker(…, bAnsi=1)`** — `bAnsi` 는 `+0x173~+0x185` `DefWindowProcWorker` 호출 인자로만 감 (앱 프로시저는 **변환 없이 날 메시지** 로 불림, `+0x8c~+0xbf`) | |
| `DWLP_DLGPROC` (`SetWindowLongPtr(hDlg, 8, p)`) | | `_SetWindowLongPtr +0xc6~+0x12d`: `pdlg+0x18` 비트 2 와 `bAnsi` 가 다르면 `GetCPD` 변환 thunk 로 감싸 `pdlg+0x0` 에 씀 | |

**결론**: 끝 길 (E9~E21) 은 A 판도 같다. 달라지는 건 ① 만들 때 확장 스타일 · `pdlg+0x18` 비트 2 (스택 값만) ② 창 사본 `+0x12` 비트 3 (커널) ③ 글자 메시지 · 문자열 메시지 변환. `WM_INITDIALOG` · `WM_COMMAND` · `WM_SETFONT` · `WM_GETDLGCODE` · `DM_*` 만 쓰는 대화상자는 변환 없음.

### 4-5. 앱 프로시저가 거의 늘 부르는 것 (5) — 줄 근거
- `GetDlgItem(hDlg, id)` `GetDlgItem.asm`: `+0x8 ValidateHwnd` → `+0x1e MOV RAX,[RAX+0x38]` (첫 자식, 0 이 아니면 `+0x27~+0x2b` 로 `− [자기+8] + 자기`) → `+0x38 CMP [RAX+0x140],EBX` (ID) 같으면 `+0x7f MOV RBX,[RAX]` (hwnd) 반환, 다르면 `+0x40 MOV RCX,[RAX+0x48]` (다음 형제) → 반복. 못 찾으면 `+0x67 MOV ECX,0x58d; RtlSetLastWin32Error` · 0. **syscall 0.** `_GetDlgItem` 은 같은 일을 오류 · 검증 없이. `_FindDlgItem +0x14` 가 `_GetDlgItem` 을 먼저 부르고 못 찾으면 (대화상자가 `& 0xffff2fff == 0x29d` (데스크톱) 가 아닐 때) `_NextControl` 로 `WS_EX_CONTROLPARENT` 컨테이너 안까지 뒤짐 (`+0x33~+0x6c`).
- `SetWindowLongPtrW` = `_SetWindowLongPtr(h, 인덱스, 값, 0)` (`SetWindowLongPtrW +0x0/+0x3`). **보통 (`g_systemCallFilterId ≠ 5`) 은 거의 다 syscall `NtUserSetWindowLongPtr` (0x15da, 인자 `hwnd, 인덱스, 값, bAnsi`, 반환 = 옛 값)**: 대화상자 (사본 `+0x12` 비트 0) 에서 인덱스 `0x10` → `+0x6a CMP [g_systemCallFilterId],5; +0x71 JNZ → +0x261`, 인덱스 `0` → `+0x4a JZ → +0x14d; +0x154 JNZ → +0x174; +0x1c2 CMP …,5; +0x1c9 JNZ → +0x261`, 일반 창 · 그 밖 인덱스 (음수 `GWLP_USERDATA −21` 포함) → `+0x1c2/+0x1c9 JNZ → +0x261` 로 같은 syscall. **사용자 모드로 쓰는 건 `g_systemCallFilterId == 5` 일 때 (`+0x87~+0x92` `[여분+0x10]` · `+0x162~+0x16c` `[여분+0x0]` · `+0x234~+0x248` 양수 인덱스) 와 `DWLP_DLGPROC (8)` 쓰기 (`+0x9b~+0x12d`, `TestWindowProcess` 가 1 일 때: `pdlg+0x0` · `pdlg+0x18` 비트 2) 뿐.** `0x14~0x1d` (대화상자) 는 `SetLastError(0x587)` 후 0. → stub `NtUserSetWindowLongPtr` 는 대화상자 인덱스 `0` `0x10` 에서 `[[사본+0x128]+인덱스]` 에 쓰고 옛 값을 돌려줌; 음수 `−21` 이면 사본 `+0xd8` `[추측: 커널이 쓰는 칸 — 읽는 쪽 `GetWindowData +0xc3` 이 확정]`.
- `GetWindowLongPtrW` = TEB `+0x840/+0x848` 한 칸 캐시 (142) 맞으면 `_GetWindowLongPtr(사본, 인덱스, 0)`, 아니면 항목 검증 후 같은 함수. 대화상자 (`+0x12` 비트 0): 인덱스 `0` → `+0x16e _GETPTI == PtiCurrent` (항목 `+0x8` == TEB `+0x78`) 이면 `[여분+0x0]`, **`0x10` → `+0x1d6~+0x1ef [여분+0x10]`**, 다르면 `NtUserMessageCall` (code `0x5a`) 로 감. 음수 인덱스 → `GetWindowData +0x0~+0x27c`: **`−21` = `[사본+0xd8]` · `−6` = `[사본+0x20]` · `−12` (`GWLP_ID`) = 자식이면 `[사본+0x140]` · `−16` = 스타일 · `−20` = 확장 스타일 · `−8` = 부모 / 소유자**. 전부 syscall 0.
- `GetDlgCtrlID` `GetDlgCtrlID +0x14~+0x3f`: TEB 캐시 · 항목 검증 후 `MOV EAX,[사본+0x140]`. `GetParent` · `GetParentDialog +0x0~+0x39`: `+0x8 TEST [RCX+0x12],1` → 켜졌으면 (`+0x28`~) `+0x1a` 비트 0 · `+0x1d` 비트 2 를 보고 그 창을 돌려줌, 아니면 `+0x1f` 비트 6 (자식) 이고 `+0x30` 이 있으면 한 단계 올라가 반복.
- `WM_GETDLGCODE` 가 앱에서 0: `DefDlgProcWorker +0x143 TEST R13,R13` → 0 → (`ESI=0x87 > 0xa1`) `+0x14c~+0x153 JBE → +0x218` → `+0x21e CMP ESI,0x2e; JA → +0x2e2` → (`+0x2e2~+0x314` 에 안 걸림) `JNZ → +0x92c` → `+0x935 JMP` → `+0x173 DefWindowProcWorker(사본, 0x87, w, l, bAnsi)` (ret `+0x18a`) → `DefWindowProcWorker +0x0 CMP [ghmodUserApiHook],0; +0x8 JZ +0x1d JMP RealDefWindowProcWorker` → `RealDefWindowProcWorker +0x46 CMP EBX,[0x1800da568]` (한도 `[정적]`) → 비트 `[0xda570]` 켜지면 `+0x82~+0xcb`: `MessageTable[0x87] = 0x615` 의 낮은 바이트 `0x15` → **`gapfnScSendMessage[0x15]` = `NtUserMessageCall` 입구 (rva `0x73530`)** 를 CFG 로 부름, fnid `0x29e` (`+0xb6`) → **syscall `NtUserMessageCall` (0x1005) 한 번**. 아니면 (`[0xda580]` 표) 사용자 모드 점프표: `0x87` → `return 0` (`+0x56`). 비트표는 커널이 채우므로 어느 쪽인지 **정적으로는 못 정함**.
- `DlgSetFocus` `+0x6~+0x19`: `SendMessageW(hwnd, 0x87, 0, 0)` → `TEST AL,8` (`DLGC_HASSETSEL`) 이면 `SendMessageW(hwnd, EM_SETSEL 0xb1, 0, 0x7fffffff)` (`+0x1d~+0x2e`) → `+0x3b JMP [NtUserSetFocus]` (**꼬리호출, syscall 0x104c 한 번**, 반환은 호출자 `DlgSetFocus` 의 반환이 되나 호출자들이 안 씀).

---

## 5. 지금 우리 (PE_emulator) 맞나·틀리나

읽은 곳: `PE_emulator-BD/ext/syscall/syscall_layer_cpp/src/gui/` 파일 목록 (`nt_user_get_message.cpp` 등 `nt_user_*` 19개 — **`peek_message` · `destroy_window` · `set_dialog_pointer` · `wait_message` · `set_window_pos` · `post_message` · `show_window` 파일은 없음 → 몸 없는 기본 대답 0**) 과 `nt_user_get_message.cpp` 한 파일.
- **`NtUserPeekMessage` · `NtUserWaitMessage` 기본 대답 0 = 금지 꼴 F1 — 틀림.** `DialogBox2` 에 닿으면 (142 의 stub 이 채워져 `InternalCreateDialog` 가 hwnd 를 돌려주는 순간) 끝없이 도는 바쁜 고리가 된다. 맞는 값: 위 stub 9~14.
- **`nt_user_get_message.cpp` (임시 stub)**: 머리 주석 "같은 모달 고리의 잠과 같다고 봄" — 모달 고리의 잠은 `NtUserWaitMessage` 이고 **`DialogBox2` 는 `NtUserGetMessage` 를 아예 안 부른다** (`_calls.tsv` `DialogBox2` 줄에 없음) → 이 파일은 앱 자기 루프용으로만 맞다. `window != 0 && != 0x10010` 이면 정지 — 142 가 hwnd 를 "늘 같은 값" 이 아니게 바꾸면 이 조건도 바뀌어야 함. 기본 대답 0 이 "가짜 WM_QUIT" 라는 주석은 `GetMessageW +0x1b/+0x43` 이 반환을 그대로 주므로 맞음 (앱 `while (GetMessage())` 루프가 끝남).
- **`NtUserGetMessage` 의 −1**: 안 줘도 됨 (지금 stub 은 안 줌) — 다만 `GetMessageW` 가 −1 을 그대로 주므로 앞으로 "오류" 를 −1 로 구현하면 앱 루프가 영원히 돎.
- **`NtUserCreateWindowEx` / `NtUserSetDialogPointer` / 공유 블록**: 142 9절 그대로. 이번에 **더 필요한 칸**: 컨트롤 사본 `+0x30` (부모) · `+0x38`/`+0x48` (자식 · 형제 연결) · `+0x40` (소유자 0) · `+0x140` (ID) · `+0x1f` 비트 6 (자식) — 아래 stub 10.
- 142 9절 stub 줄 중 **고쳐 읽어야 할 것 하나**: 142 의 "`NtUserDestroyWindow` — 컨트롤 실패 때만 (N10) 불림" → **모달 끝에서도 불림 (`DialogBox2 +0x3c4`)**. 반환은 안 보니 0 이어도 되지만 "컨트롤 실패 때만" 이 아니다.

### stub 이 채워야 하는 최소 목록 — 142 9절 목록 (1~8) 에 이어 붙임
9. **`NtUserPeekMessage(pmsg, hwnd, min, max, wRemove, flags)`** (모달 고리 `DialogBox2 +0x26b` 에서): **첫 번째 부름**에 반환 `1`, `*pmsg` (0x30 B, 먼저 0 채움) = `{hwnd = 그 모달 대화상자, message = 0x111, wParam = ID, lParam = 그 ID 컨트롤 hwnd (사본 `+0x38` → `+0x48` 사슬에서 `+0x140 == ID` 인 것, 없으면 0)}`. **ID = `LOWORD(pdlg+0x20)` (읽는 곳 `[[사본+0x128]+8]+0x20`, 0 이면 1)** — `DM_SETDEFID` · 컨트롤 고리의 `DLGC_DEFPUSHBUTTON` 이 정한 값을 그대로 따르게. 안 끝나고 다시 부르면 (F7) 정책대로 (IDCANCEL → `WM_CLOSE` → `WM_QUIT`) `[추측: 정책]`, 그래도 안 끝나면 진단 정지. `hwnd` 는 stub 이 `NtUserSetDialogPointer(hwnd, pdlg)` (`ValidateDialogPwnd +0x4f` 가 대화상자마다 한 번 부름) 로 알던 hwnd 중 아직 `NtUserDestroyWindow` 안 당한 맨 나중 것 `[추측]`.
10. **hwnd 마다 (142 4번) 에 더**: 컨트롤 사본 — `+0x1f` 비트 6 켜짐 (`WS_CHILD`) · `+0x30` = 대화상자 사본 주소 · `+0x48` = 다음 컨트롤 사본 (템플릿 차례, 마지막 0) · `+0x140` = ID · `+0x1f` 비트 3 꺼짐 (`WS_DISABLED` 아님). 대화상자 사본 — `+0x38` = 첫 컨트롤 · `+0x30` = `+0x40` = 0 · `+0x1f` 비트 6 꺼짐 · `+0x2a` = `0x2a4` (6번이 적음) · `+0x12` 비트 0 켜짐 (6번) · 비트 2 꺼짐 · `+0x78` = `DefDlgProcW`. 주소 변환 규칙 `포인터 − [사본+0x8] + 사본` 이 맞도록 (`[+8] == 자기 주소` 면 그대로).
11. **TEB `+0x860` = 0 허용 (권장)**: `GetActiveWindow` · `GetFocus` 가 0 이 되어 `EndDialog` 는 `SetWindowPos` · `PostMessage` 둘만 부르고, `IsDialogMessageW` 키 길은 `DM_GETDEFID` 길로 감. 채우려면 `[+0x20]` = 포커스 hwnd · `[+0x28]` = 활성 hwnd · `[+0x0]` 비트 2 = 0 · `[+0x4] \| [+0x8]` 가 `0x3dff` 와 겹치지 않게 (겹치면 `DialogBox2` 가 빈 큐 빠른 길을 못 씀).
12. **`NtUserSetWindowPos` (0x1021) · `NtUserPostMessage` (0x100d) · `NtUserSetFocus` (0x104c) · `NtUserSetActiveWindow` (0x10cd) · `NtUserZapActiveAndFocus` (0x15d6) · `NtUserEnableWindow` (0x1416) · `NtUserNotifyWinEvent` (0x1028) · `NtUserPostQuitMessage` (0x14ee)**: **반환 안 봄 → 아무 값 (0).** 단 `NtUserPostMessage` 는 앱이 `PostMessage` 로 보낸 것을 받을 큐가 없으면 `WM_CLOSE` 의 기본 처리 (`PostMessageW(…WM_COMMAND, IDCANCEL)`) 가 사라져 F7 에서 못 빠짐 → 큐를 두려면 `NtUserPeekMessage` 가 먼저 그 큐를 비우게.
13. **`NtUserDestroyWindow(hwnd)` (0x1095)**: 반환 안 봄. 항목을 풀어도 되고 (이후 `ValidateHwnd` 0 — 고리는 이미 나감) 두어도 됨. `pdlg+0x20` 은 **이미 읽혔음**. `WM_DESTROY` / `WM_NCDESTROY` 를 앱 프로시저에 보내는 건 선택 (`DefDlgProcWorker +0x31a` 는 `WM_NCDESTROY` 에서 `pdlg` 를 풂 — 안 보내면 누수만).
14. **`NtUserWaitMessage` (0x100a)**: 이 길에서 불리면 (= `NtUserPeekMessage` 가 0 을 줌) **진단 정지** 권장 — 돌려줘도 같은 고리, 자면 모두 자게 되어 엔진이 끝냄 (`nt_user_get_message.cpp` 와 같은 꼴).
15. **`NtUserMessageCall` (0x1005)**: 컨트롤 (사본 `+0x12` 비트 2) · `WM_GETDLGCODE` 의 `DefWindowProc` 길용 — 0 으로 충분 (142 와 같음). `NtUserDispatchMessage` (0x1031) · `NtUserTranslateMessage` (0x100b) · `NtUserCallMsgFilter` (0x1013) 는 위 조건이면 **안 불림** (불리면 조건이 어긋난 것).
16. **`NtUserSetWindowLongPtr(hwnd, 인덱스, 값, bAnsi)` (0x15da)** (`g_systemCallFilterId ≠ 5` 라 앱의 `SetWindowLongPtr` 는 거의 다 여기로 옴): 대화상자 hwnd 이고 인덱스 `0` / `0x10` 이면 **`[[사본+0x128] + 인덱스] = 값`, 반환 = 옛 값** (`GetWindowLongPtrW` 가 사용자 모드로 같은 칸을 읽으므로 같은 칸이어야 함 — 앱이 `WM_INITDIALOG` 에서 `DWLP_USER` 에 자기 포인터를 넣고 뒤 메시지에서 꺼내 씀), 인덱스 `−21` (`GWLP_USERDATA`) 이면 사본 `+0xd8` `[추측]` (읽기 `GetWindowData +0xc3` 이 이 칸). 인덱스 `8` (`DWLP_DLGPROC`) 은 user32 가 사용자 모드로 `pdlg+0x0` 에 써서 syscall 이 안 옴. `DefDlgProcWorker +0x207` 은 `[여분+0x0] ≠ 0` 일 때 `(hDlg, 0, 0)` 으로 부름.
17. **되부름 · 큐**: 이 끝 길에도 **되부름 필요 없음** (`WM_COMMAND` 는 user32 가 앱 프로시저를 직접 부름). 큐가 있어야 하는 건 앱이 `PostMessage` 로 자기 대화상자에 보내는 경우 (F7) 뿐.

### 되부름이 꼭 필요한가 (끝 길)
**아니오.** 끝 길의 앱 프로시저 호출은 `DispatchMessageWorker` → `UserCallWinProcCheckWow` → `DefDlgProcW` 전부 user32 안 (CFG 간접 호출 `UserCallWinProcCheckWow` · `UserCallDlgProcCheckWow +0x193`). `NtUserDestroyWindow` 가 커널에서 `WM_DESTROY` / `WM_NCDESTROY` 를 되부르는 쪽 (`[추측]`, win32k 몫) 은 모달을 빠지는 데 필요 없다.

---

## 6. win32k 몫 남음 (열지 않음 — 작업하는 동안 `145_gdi-display-dc_result.md` 가 없었음. 지금은 생겼으니 다음 요청에서 열어도 됨)

user32 쪽에서 닫혀서 **꼭 필요하지는 않았다**. 필요해지면 이 목록으로:
1. win32kfull `NtUserPeekMessage` (0x1001) · `xxxPeekMessage` — 진짜 반환 · MSG 칸 · 첫 부름에 오는 메시지 (`WM_PAINT` `WM_NCACTIVATE` `WM_TIMER` …) · `wRemove` 처리. (user32 는 0 / 0 아님 · MSG `+0x0 +0x8 +0x10 +0x18` 만 읽음 — 닫힘)
2. `NtUserDestroyWindow` · `xxxDestroyWindow` — `WM_DESTROY` / `WM_NCDESTROY` 되부름 · 항목 해제 · `pdlg` 정리 (`DefDlgProcWorker +0x31a`).
3. `NtUserSetWindowPos(…, 0x97)` — 숨기기 · 활성 변경 부작용 (`TEB+0x860` 블록 갱신).
4. `NtUserInitializeClientPfnArrays` (0x14aa) · `ClientThreadSetup +0x228` — **`user32 +0xda568~+0xda580` 비트표 (`gDefWindowMsgs`) 를 채우는 곳** `[추측]` — `RealDefWindowProcWorker` 가 `WM_GETDLGCODE` 등을 커널로 보낼지 정함.
5. `xxxCreateWindowEx` — 확장 스타일 `0x80000000` (ANSI) 이 사본 `+0x12` 비트 3 · 클라이언트 프로시저 (`DefDlgProcA` 쪽) 로 가는 줄 (`+0x1b9 JS` · `+0x1bf` · `+0x8e6 MapClientNeuterToClientPfn` · `+0xa3a 0x208`), 자식 연결 `+0x30 +0x38 +0x48` 의 순서 (`LinkWindow` 가 맨 아래 / 맨 위 어디에 끼우나).
6. `NtUserMessageCall` 의 `fnid 0x29e` (`DefWindowProc`) · `WM_GETDLGCODE` 답.
7. `NtUserSetWindowLongPtr` (0x15da) — `GWLP_USERDATA` 가 사본 `+0xd8` 에 가는 줄.

---

## 7. 동적으로 확인할 것 (Q273 VM GUI 세션에 얹을 꼴)

멈춤점은 `user32!함수+0x…` (user32 만, 프로세스 조건 필요 없음 — 커널 멈춤점이 아님). 오프셋은 위 줄 (호출 직후 = `ret_off`).
1. **`user32!DialogBox2+0x270`** (`_PeekMessage` 반환 직후): `r eax` (진짜 반환) · `dq @rsp+0x50 L6` (MSG — 처음 오는 메시지 순서: `WM_PAINT` `WM_NCACTIVATE` …) · 여러 번 (`ba`/`bp` 조건: 몇 번째에 `0x111`/`0x100` 이 오나).
2. **`user32!IsDialogMessageW+0x189`** (`WM_GETDLGCODE` 답) · **`+0x98b51`** (`DM_GETDEFID` 답, `0x534b0001` 인가) · `+0x98b1e` (`WM_COMMAND` 를 보낸 뒤) — Enter 한 번.
3. **`user32!DefDlgProcWorker+0xc4`** (`UserCallDlgProcCheckWow` 뒤): `rdx`/`[rbp-0x28]` (앱 반환) · **`+0xbf` 앞**: 앱 프로시저 주소 `[pdlg+0]` · 호출 인자.
4. **`user32!EndDialog+0x24`** (`rbx` = 사본) · `+0x70` (`dq poi(poi(@rbx+0x128)+8) L6` → `pdlg` 전체) · `+0xb0` · `+0xe0` · **`user32!DialogBox2+0x3cb`** 뒤 `rax` · `+0x3bd` 에서 `rsi` (읽는 결과값). 진짜에서 `EndDialog` 가 부르는 syscall 이 `SetWindowPos` · `PostMessage` 둘뿐인지 (TEB `+0x860` ≠ 0 이면 더).
5. **TEB `+0x860` 진짜 값**: `dq @$teb+0x860 L1` → `dq poi(@$teb+0x860) L8` (`+0x20` 포커스 · `+0x28` 활성 · `+0x4 +0x8` 깨움 비트).
6. **자식 연결 진짜 값**: 대화상자 사본 `dq @rax+0x30 L5` (`+0x30 +0x38 +0x40 +0x48`) · 컨트롤 사본 `dq … L5` · `dd …+0x140 L1` — 컨트롤 연결 순서가 템플릿 차례인지.
7. **`user32!DialogBox2+0xaf`** 의 `gpsi+0x88c` · `+0x2f5` 의 `gpsi+0x764` · `+0x5f` 의 `gpsi+0x1b50` 진짜 값.
8. **`user32!DispatchMessageWorker+0xb6`** (`[사본+0x12]`) · `+0x1d8` (`rdx` = 창 프로시저 = `DefDlgProcW` 인가) · `+0x341` 이 **안 걸리는지** (`NtUserDispatchMessage`).
9. **`user32+0xda568` · `+0xda570` · `+0xda578` · `+0xda580`** 진짜 값 (`dq user32+0xda568 L4`) · `0x87` · `0x111` 의 비트 (`db poi(user32+0xda570)+(0x87>>3) L1`).
10. **`user32!RealDefWindowProcWorker+0xc6`** 이 안 걸리는지 (앱이 `WM_COMMAND` 에 FALSE 를 돌려준 대화상자 하나로).
11. **WM_CLOSE 시험**: 앱이 `WM_CLOSE` 에 FALSE → `user32!DefDlgProcWorker+0x46c` 에서 `PostMessageW(…0x111, 2 …)` 가 오는지.
12. **A 판 한 번** (`DialogBoxIndirectParamA` 시료 15 개 중 하나): `user32!InternalCreateDialog+0x5ff` 직후 `ecx` (`0x80000000`) · 만들어진 사본 `db @rax+0x12 L1` 비트 3 · `pdlg+0x18` 비트 2 · `SendMessageWorker+0x16f` 가 같다고 건너뛰는지.

---

## 8. 다음 요청 후보
- (win32k, sast/145 뒤) 위 6절 1~7. 가장 급한 것: **5 (자식 연결 순서 · ANSI 비트)** 와 **1 (`NtUserPeekMessage` 첫 메시지들)** — 진짜 첫 메시지가 뭔지 알면 stub 의 "맨 처음 한 번 `WM_COMMAND`" 가 진짜와 얼마나 다른지 앎.
- `ButtonWndProcWorker` · `xxxButtonNotifyParent` 본문 (컨트롤 프로시저) — 마우스 클릭이 `WM_COMMAND` 를 보내는 줄, `WM_GETDLGCODE` 에서 `DLGC_DEFPUSHBUTTON` 을 돌려주는 줄 (stub 이 컨트롤 사본 비트 2 우회로 건너뛰는 걸 대신하려면 `BS_DEFPUSHBUTTON` 스타일 → ID 를 `pdlg+0x20` 에 직접 적어야 함: **MessageBox (`MB_DlgProc`) 의 `MB_YESNO` 같은 판은 기본 ID 가 `IDOK (1)` 이 아니라서 이 칸이 맞지 않으면 틀린 답 (`IDOK`) 이 나옴** — `SoftModalMessageBox` · `MB_AddPushButtons` 에서 기본 단추 스타일 · 순서를 정하는 줄).
- `GetUserDefaultLCID` (kernelbase) — `IsDialogMessageW +0x22` 가 부름. 에뮬레이터가 이미 만난 길이면 불필요.
- `RealDefWindowProcWorker` 의 `WM_CLOSE` · `WM_SYSCOMMAND(SC_CLOSE)` 갈래 (앱이 `WM_CLOSE` 를 처리 안 하는 대화상자의 끝).
- `SoftModalMessageBox` · `MB_DlgProc` 로 `MessageBox` 가 같은 고리 (`DialogBox2`) 를 타는지 (139 4절 틀 확인) — 같으면 위 stub 9 가 `MessageBox` 에도 쓰임.

## 판정
**분석성공 (user32 안에서 닫힘).** 물음 3 (a) (b) (c) (d) 와 5 모두 줄 근거와 함께 답했고, 핵심 발견 다섯: ① Enter 는 `WM_KEYDOWN` 으로 오고 `IsDialogMessageW` 가 `SendMessageW(WM_COMMAND)` 로 바꾸며 **기본 단추 ID = `pdlg+0x20` 아랫 16비트 (= 결과값 칸)**; ② `WM_COMMAND` 곧장 → 앱 프로시저는 syscall 0 (조건: 사본 `+0x12` 비트 2 · `+0x2a` = `0x2a4` · TEB `+0x820`→`+0x10`/`+0x838` 0); ③ 끝 길의 syscall 은 `NtUserPeekMessage` (반환만 봄) · `SetWindowPos` · `PostMessage` · `DestroyWindow` (모두 반환 안 봄) 뿐, 나가는 길은 `pdlg+0x18` 비트 0 · `pdlg` NULL · hwnd 사라짐 · `WM_QUIT` 넷; ④ 지금 우리의 기본 대답 0 은 **끝없는 바쁜 고리 (F1)**; ⑤ `DefDlgProcWorker` 는 사본 `+0x2a` 가 `0` 도 `0x2a4` 도 아니면 앱 프로시저를 안 부름. 정적으로 못 닫은 것 (비트표 `user32+0xda568~` 값 · ANSI 비트의 커널 쪽 · 자식 연결 순서 · `GetUserDefaultLCID` · `NtUserDestroyWindow` 부작용 · MessageBox 기본 ID) 은 `[추측]`·`[공백]` 으로 표시하고 6 · 7 · 8절에 넘김. MS 코드는 레포에 붙이지 않았다 (`ms/disassemble/…` 경로와 `+0x…` 만).

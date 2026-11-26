# 2026-11-25 (작성일, 정적 분석 2026-11-25) run142 — user32 창 API 넷의 연결 가드 · `NtUserGetThreadState(0xe)` 의 뜻 · 메시지 상자 한 장의 최소 길 (Q268, 정적만): `MessageBoxW` · `DialogBoxParamW` · `LoadIconW` · `GetSystemMetrics` 는 **같은 도우미를 부르지 않는다.** 같은 모양 다섯 줄 — `TEB+0x78 == 0` 이면 `NtUserGetThreadState(0xe)` (win32u `0x1000`) 를 부르고 **반환이 0 이면 실패 (0 / −1)** — 을 각자 안에 품고 있다 (`MessageBoxWorker +0x58 ~ +0x75` · `InternalCreateDialog +0x13c ~ +0xd50` · `LoadIcoCur +0x52 ~ +0x258` · `RealGetSystemMetrics +0x19 ~ +0x131`). win32kfull `NtUserGetThreadState` 종류 `0xe` 는 **아무 칸도 안 읽고 상수 `1` 을 돌려준다** (`+0x11 CMP ECX,0xe` · `+0x14 JZ → +0x16c MOV EAX,1`). 목적은 반환값이 아니라 **win32k syscall 이라서 커널이 먼저 스레드를 GUI 로 바꾸는 것** (`KiSystemServiceStart +0x91f KiConvertToGuiThread`) — 그 변환 안 win32kbase `xxxCreateThreadInfo` 가 **TEB `+0x78` 을 쓰고** (`+0x756`, 값은 `PsGetThreadId` 반환) win32kfull `xxxClientThreadSetup +0x4c KeUserModeCallback(0x54)` 로 user32 `__ClientThreadSetup` 을 되부른다. 가드만 풀면 상자는 아직 안 뜬다 — 그 뒤 벽 셋 (TEB `+0x820` · `psi+0x1b56` · `gSharedInfo+0x18`) 과 GDI 답이 있고, 모달 고리는 `MB_DlgProc` 가 `WM_COMMAND` 를 받아 `EndDialog` 를 불러야 끝난다

> **고쳐 읽기 (run144, sast/141 — Q270)**: 최소 길 표 차례 8 · PE_emulator 6 · 다음 요청 후보 N4 로 넘긴 "GDI (PEB `+0xf8` GDI 공유 표 — Q270)" — `GdiGetCharDimensions` · `GetTextExtentPointW` · `GetTextMetricsW` · `SelectObject` · `GetDeviceCaps` 는 syscall 앞에서 공유 핸들 표의 **DC 칸** (`+0xe == 1` · `+0xc == H>>16` · `+0x8 & ~1 ==` 이 프로세스 PID 정확히 · `+0x10` 풀면 0 아닌 `DC_ATTR` 포인터) 을 본다 `[정적]`. 칸이 없으면 `GdiGetCharDimensions` 가 아무것도 안 쓰고 돌아가 `SoftModalMessageBox +0x153` 이 폭 0 으로 `MessageBoxW` 0, `SelectObject` · `GetTextExtentPointW` 는 `0x57` + 0. HFONT 는 종류 `0xa0000` + 글꼴 칸이 있어야 한다. DC 칸을 만드는 쪽 · `DC_ATTR` 전체 · 글꼴 사본은 Q278. 자세히는 `runs/03_sprint/run144-gdi-startup.md` 7절.

> **고쳐 읽기 (run146, sast/142 — Q274a)**: 최소 길 표 차례 19 ~ 27 의 `[공백]` (어느 호출이 syscall 없이 풀리나 · `NtUserShowWindow` · `NtUserSetForegroundWindow` 조건) 과 다음 요청 후보 N1 · N2 가 `[정적]` 으로 닫혔다 — `WM_SETFONT` · `WM_INITDIALOG` 는 핸들 항목 `+0x8` == TEB `+0x78` · 사본 `+0x12` 비트 2 = 0 · `[[TEB+0x820]+0x10] | [TEB+0x838]` 의 `0x2020` = 0 · 사본 `+0x80` ≠ 0 이면 `SendMessageWorker` 가 syscall 없이 창 프로시저를 부르고, 표시 · 초점 · 앞으로 내기 반환은 안 본다. 5절 "대화상자 자료 … `[추측: 어느 힙]`" → `pUserHeap` (`pdlg` 0x58 B 는 `AllocLookasideEntry`, 그것을 담는 사본 `+0x128` 여분 블록은 커널 되부름 `0x7b`). PE_emulator "최소 조건" 7 의 "`ValidateHwnd` 를 통과하는 hwnd" 는 run146 "stub 이 채워야 하는 최소 목록" 으로 채움. 메시지 상자 템플릿의 컨트롤 수는 그대로 `[공백]`. 자세히는 `runs/03_sprint/run146-dialog-window.md`.

> **정정 · 고쳐 읽기 (run148, sast/146 — Q274a 둘째 몫)**: 4절 "비정상 끝 = `WM_QUIT` … 자료 `+0x20` 이 처음 값 (0) 이라 **`MessageBoxW` 0**" 은 틀렸다 `[정적]` — 처음 값은 **1** (`InternalCreateDialog +0x6fa MOV qword [pdlg+0x20],1`, 컨트롤 고리에서 `DLGC_DEFPUSHBUTTON` 컨트롤 ID 로 바뀔 수 있음 — run146) 이고, `DialogBox2` 는 `WM_QUIT` 에서도 `+0x3bd` 로 그 값을 돌려주며, `SoftModalMessageBox +0x5b6 ~ +0x5be` 는 −1 만 0 으로 바꾼다 → `DialogBox2` · `SoftModalMessageBox` 반환은 0 이 아니다 (`MessageBoxWorker` · `MessageBoxTimeoutW` 가 그 뒤 바꾸는지는 `[공백]`). 고쳐 읽기: 4절 모달 고리 · 최소 길 차례 26 ~ 28 · PE_emulator 8 ("`WM_COMMAND(IDOK)` / `WM_QUIT` 중 무엇을") 이 줄 근거로 닫힘 — 고리는 `NtUserPeekMessage` 반환 0 / 0 아님만 보고, 0 이면 (소유자 0) `NtUserWaitMessage` 뒤 다시 돈다 (횟수 한도 없음), Enter 는 `WM_KEYDOWN` → `IsDialogMessageW` 가 `SendMessageW(WM_COMMAND, LOWORD(pdlg+0x20))`, `EndDialog` 뒤 `NtUserDestroyWindow` 까지 반환은 안 본다. 자세히는 `runs/03_sprint/run148-dialog-end.md`.

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("user32 창 API 넷이 `NtUserGetThreadState(0xe)` 뒤 무엇을 보고 0 / −1 로 나가나, 그리고 메시지 상자 한 장이 뜨기까지의 가장 짧은 길", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/335_internals-user32-connect.md`, 튜닝 `335_tune15.md` GC, 병목 샘플 72) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 user32 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run131 · run136 과 같은 줄). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (ntkrnlmp · user32 · win32u · win32kbase · win32kfull 줄 · 근거 줄) · 3장 user32 시작 함수 절 한 줄 · gdi32 하위 절 한 줄 · 그 뒤 새 하위 절 · 4장 (run85 · run86 · run121 · run131 줄 표시 · Q268 닫힘 줄 · run142 줄 셋) · 5장 (run142 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q268** (PE_emulator 요청 335, 사용자 결정 2026-11-25 — PE_emulator D154 의 둘째, 정적만). 우리 대기열 **Q161 ③ · Q163 ④ 를 당긴 것.** 결과서 판정 **분석성공** (질문 1 · 3 닫음, 질문 2 는 틀 + 확인된 벽까지).
> - 이어 쓰는 앞 run: **run85** (`gSharedInfo` user32 rva `0xda350` = csrss USERSRV 연결 답 `+8` 부터, 첫 8 B = `gpsi`; `DllMain` 이 읽는 `psi` 칸 `+0x0` · `+0x8bc` · `+0x1b66`; GUI 변환 = 첫 win32k syscall, `__ClientThreadSetup` = `apfnDispatch` 84번째 칸), **run86** (A12 `ClientThreadSetup +0x93 GdiProcessSetup` · A13 `GetAppCompatFlags2 +0x12` TEB `+0x78` → `+0x45 NtUserGetThreadState(0xe)`), **run131** (`RegisterClassExWOWW` 는 `gpsi` 안 읽음 · `VerNtUserCreateWindowEx +0x236 NtUserCreateWindowEx`), **run136** (win32kfull · win32kbase 첫 analyze), **run121** (짐 지도 3.3 — `Win32ThreadInfo` 놓는 줄 못 찾음 `[공백]`).
> - 남은 동적 → **Q273 (동적: 결과서 동적 — TEB +0x78 값·+0x800~+0x8f8·gSharedInfo/psi 덤프·__ClientThreadSetup 진입·메시지 상자 syscall 수·LoadIconW seq 2766 TEB 값 — Q162 와 합침)**, 남은 정적 → **Q274 (정적: 결과서 다음 요청 후보 N1~N5 — InternalCreateDialog 창 만들기·tagWND 칸·xxxCreateThreadInfo TEB+0x820/0x828 채우기·GetSystemMetrics 지표별 칸·LoadIcoCur 자원 길)**. GDI 쪽 DC · 글꼴 · 글자 폭 (결과서 N4) 은 **Q270 (sast/141 `gdi-startup`) 이 받았다.**

## 요약

- **가드는 넷 다 같은 꼴, 함수는 따로** `[정적]`: `GS:[0x30]` → `CMP qword [TEB+0x78],0` → `JZ` → `MOV ECX,0xe` → `CALL [user32 IAT 0xb65f8]` (= win32u `NtUserGetThreadState`, `0x1000`) → `TEST RAX,RAX` → 0 아니면 계속, 0 이면 실패. **syscall 뒤에 TEB `+0x78` 을 다시 읽지 않는다.** 그래서 에뮬레이터의 기본 대답 0 이 넷을 정확히 그 실패 가지로 보낸다.
  - 반환: `MessageBoxW` 0 · `DialogBoxParamW` −1 (`InternalCreateDialog` NULL → `InternalDialogBox` −1) · `LoadIconW` 0 · `GetSystemMetrics` 0.
  - 같은 꼴이 user32 전체 **85곳** 에서 그 가져오기 칸을 부른다 (직접 `CALL` 79 + `JMP` 껍데기 6, `_refs/0xb65f8.tsv`). 함수 하나로 남은 것은 `PtiCurrent` (`0x19d10`, syscall 뒤 **다시** TEB `+0x78` 을 읽음) · `IsGUIThread` (`0x52210`).
- **종류 `0xe` = 상수 1** `[정적]`: win32kfull `NtUserGetThreadState` (rva `0x1ec5b0`) `+0x11 CMP ECX,0xe` · `+0x14 JZ → +0x16c MOV EAX,1` · `+0x171 JMP → +0x97` (끝). `+0x21 EnterSharedCrit` 보다 앞이라 락도 안 잡는다. **정상 반환은 0 이 아니라 1.**
- **부수 효과가 목적** `[정적, 커널 가운데는 이름만]`: win32k syscall (표 인덱스 `0x20`) 은 `KiSystemServiceStart +0x907 CMP EDI,0x20` → `+0x91f KiConvertToGuiThread` → `+0x4 PsConvertToGuiThread` (이름만) → … → win32kfull `W32pThreadCallout +0x61 UserThreadCallout` → win32kbase `+0x36c xxxCreateThreadInfo`.
  - `xxxCreateThreadInfo +0x745 PsGetThreadId` → **`+0x756 MOV [TEB+0x78], 그 반환`** (TEB 가 있을 때만 `+0x73d / +0x740`). 값은 스레드 ID 로 보인다 `[추측: 가져오기 이름에서]` — 포인터가 아님.
  - `+0x76c / +0x772` `pti+0x208 = TEB+0x800` (`pClientInfo`) · `+0x780 ~ +0x794` TEB `+0x8e8` ← `pti+0x190`, `+0x8ec` ← `ESI`.
  - `+0x136f ~ +0x137b` ApiSet 표 `[+0x4e8]` 간접 = win32kfull `xxxClientThreadSetup` (rva `0x1c4f60`) → `+0x4c KeUserModeCallback(0x54, …)` → user32 `__ClientThreadSetup` → `ClientThreadSetup`. 반환 음수면 변환 실패 (`+0x1389 JS`).
- **가드 뒤 벽 셋** `[정적]` (메시지 상자 길):
  - TEB `+0x820` 이 0 → `SoftModalMessageBox +0x372 / +0x385 MOV EBX,[RCX+0x30]` 접근 위반 (널 검사 없음). user32 `ClientThreadSetup +0x2d5 ~ +0x324` 는 이 칸이 0 이면 `pUserHeap` 에서 0 채운 `0x48` B 를 빌려 (`pdiLocal`) 넣는다.
  - `psi+0x1b56` (word) 이 0 → `GetMessageBoxFontForDpi +0x8f / +0xa6 IDIV R8D` 0 나눔.
  - `gSharedInfo+0x18` (`pDispInfo`, user32 rva `0xda368`) 이 0 → `GetPrimaryMonitor +0x7 MOV RCX,[RAX+8]` 접근 위반.
  - 그 뒤 GDI 답 (DC · 글꼴 · 글자 폭) 이 0 이면 `MessageBoxW` 는 조용히 0 (`SoftModalMessageBox +0x107` · `+0x153`).
- **모달 고리 끝** `[정적]`: `DialogBox2` (rva `0x11154`) 고리는 `NtUserPeekMessage` → (없으면) `NtUserWaitMessage` 에서 잠. `EndDialog` (`+0x70 OR [자료+0x18],1` · `+0x7f [자료+0x20] = 결과`) 가 불려야 끝나고, 그것을 부르는 것은 `MB_DlgProc` (rva `0x50ab0`) 가 `WM_COMMAND` (단추 번호) 나 `WM_TIMER` 를 받을 때. `WM_QUIT` (`0x12`) 가 나오면 `EndDialog` 없이 빠져나와 `MessageBoxW` 0.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백째 바퀴. plan **Q268**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 없음.** user32 · win32kfull · win32kbase 는 이미 analyze 됨 — `^이름$` export · refs 만 (결과서). ntoskrnl `.asm` 은 sast/138 이 쓰는 중이라 이미 있던 파일만 읽음.

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| user32 | `CDB947D1B96B87977915DE04CF54279C1` | 같음 |
| win32u | `6295787D0B7E537E98F77F31F4426D1C1` | 같음 |
| win32kfull | `5CD57181BCFDC5AE8F4819BEB1196F091` | 같음 |
| win32kbase | `D22DFDA3D54B87E6BD0567A57877904C1` | 같음 |
| ntoskrnl (ntkrnlmp) | `C29EBFB06B78B3C020DCA66D99713F9E1` | 같음 |

- 버전 대조 (결과서): run85 · run86 에 적힌 줄 (`GetAppCompatFlags2 +0x12 / +0x17 / +0x45` · `__ClientThreadSetup +0x4` · `ClientThreadSetup +0x93 / +0x272 / +0x29c / +0x2c0 / +0x356` · win32u `NtUserGetThreadState` `0x1000` rva `0x12b0` · `KiSystemServiceStart +0x91f` · `KiConvertToGuiThread +0x4`) 과 요청서 rva · 크기 전부 **일치**.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): 창 API 넷이 늘 실패 — `MessageBox*` 0 (107/107) · `DialogBox*` −1 (50/50) · `LoadIcon*` 0 (78/78) · `GetSystemMetrics` 0 (484/484). 로그엔 `MessageBox*` 안 syscall 이 `NtUserGetThreadState(0xe)` (기본 대답 0) 하나뿐, `LoadIconW` 는 syscall 0 개. 요청서 `_workspace/sast/139_user32-connect.md`:
  1. **가드** — 넷이 실패로 나가는 첫 가지와 읽는 칸 (TEB `+0x78` · TEB `+0x800` 언저리 · `gpsi` · `NtUserGetThreadState(0xe)` 반환 · `gSharedInfo`). 같은 도우미가 있나.
  2. **가장 짧은 성공 길** — `MessageBoxW(NULL, 글, 제목, MB_OK)` 한 번의 win32k syscall 차례, syscall 없이 읽는 칸, 모달 고리가 끝나는 조건.
  3. **win32kfull `NtUserGetThreadState` 종류 `0xe`** — 뜻 · 정상 반환 · 부수 효과가 목적인지.
- 쓰는 곳 (원문): 팀원 `ext/syscall/syscall_layer_cpp/src/alpc/csr_user_connect_stub.cpp` (`psi` 블록) · `src/gui/` 새 stub · (꼭 필요하면) `KeUserModeCallback` (지금 없음). 메시지 상자에 "누가 답하나" 는 사용자 정책 (`335_tune15.md` Q5).

## 방법

- 정적 sast/139 (결과서 `_workspace/sast/139_user32-connect_result.md`, **분석성공**, 스크립트 문제 없음):
  - 요청 전에 이미 있던 export 그대로 읽음: user32 28개 (요청서 대상 + 도우미) · win32kfull `NtUserGetThreadState`.
  - 이번에 더 뽑음: user32 14개 (`_PeekMessage` · `HMValidateHandleNoSecure` · `DispatchMessageWorker` · `SendMessageW` · `SendMessageWorker` · `IsDialogMessageW` · `GetCapture` · `IsWindow` · `CreateDlgFont` · `GetMonitorRect` · `EndTaskModalDialog` · `StartTaskModalDialog` · `PtiCurrent` · `IsGUIThread`) + `GetDpiCacheIndex` · `_MonitorFromWindowInternal`, win32kbase 3 (`xxxCreateThreadInfo` · `UserThreadCallout` · `W32pThreadCallout`), win32kfull 2 (`W32pThreadCallout` · `xxxClientThreadSetup`).
  - refs: user32 `0xb65f8` (`NtUserGetThreadState` 가져오기 칸) · `0xda9ec` (`gfSystemInitialized`), win32kfull `xxxClientThreadSetup`, win32kbase `xxxCreateThreadInfo`.
  - 읽기만: win32u `_syscalls.tsv`, ntoskrnl `KiSystemServiceStart` · `KiConvertToGuiThread` `.asm`.
- 같은 시간에 decompiler 가 sast/140 (kernelbase · mountmgr) · sast/141 (gdi-startup — user32 가 겹침) 을 돈다. flow-writer 는 이번 바퀴 `.asm` 을 읽기만 했고 sast/140 · 141 결과서는 안 읽었다.
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - user32 `MessageBoxW +0x35 · +0x3a · +0x40`; `MessageBoxTimeoutW +0x3c · +0xaa · +0xb0 ~ +0x18a` (전부); `MessageBoxA +0x40`; `MessageBoxTimeoutA +0xfb`; `MessageBoxWorker +0x48 · +0x58 ~ +0x75 · +0x7a / +0x80 · +0x1bf · +0x266 · +0x27a · +0x283 · +0x2be · +0x2c8 · +0x304 ~ +0x308 · +0x349`; `SoftModalMessageBox +0x4f / +0x53 · +0xb1 ~ +0xbd · +0xdd · +0xf5 · +0x104 / +0x107 · +0x10d · +0x114 · +0x126 · +0x149 ~ +0x153 · +0x16f · +0x1b6 ~ +0x1e3 · +0x1fa · +0x20a · +0x212 / +0x21a · +0x249 · +0x255 / +0x261 · +0x2e0 · +0x2ef · +0x365 ~ +0x385 · +0x3ae / +0x3ba · +0x40e · +0x422 · +0x438 · +0x4b9 · +0x4da / +0x4e6 · +0x511 · +0x550 · +0x558 · +0x564 ~ +0x581 · +0x5b1 ~ +0x5be · +0x5cd / +0x5d0 · +0x5fc · +0x628 · +0x63f ~ +0x657 · +0x65e ~ +0x673`; `DialogBoxParamW +0x1d · +0x2a · +0x39 · +0x41 · +0x53 · +0x6d`; `DialogBoxIndirectParamAorW +0x20 ~ +0x29 · +0x67`; `InternalDialogBox +0x2d ~ +0x45 · +0x70 ~ +0x8f · +0xaf / +0xb4 · +0xe0 · +0xe8 · +0x151 ~ +0x15c`; `InternalCreateDialog +0x133 ~ +0x140 · +0x63f · +0x653 · +0x667 · +0x975 · +0x9a7 · +0x9ba · +0xa64 · +0xb4a / +0xb4f · +0xc50 · +0xcf2 · +0xd37 ~ +0xd50 · +0xf7a · +0xf89`; `GetDesktopWindow +0x15 ~ +0x2f · +0x80 ~ +0x99`; `LoadIconW +0x18`; `LoadIcoCur +0x2d · +0x52 / +0x56 · +0x1e5 · +0x1f4 / +0x1f7 · +0x22d / +0x231 · +0x239 ~ +0x258 · +0x290 ~ +0x2a9`; `GetSystemMetrics +0x21 ~ +0xd3`; `RealGetSystemMetrics +0xd ~ +0x33 · +0x75 · +0xa9 · +0x117 ~ +0x131 · +0x1b7 · +0x233`; `PtiCurrent` 전부; `IsGUIThread +0x2d ~ +0x43`; `GetAppCompatFlags2 +0x12 · +0x17 · +0x45`; `ClientThreadSetup +0x93 · +0xba ~ +0xd3 · +0x29c ~ +0x2b0 · +0x2cc ~ +0x340 · +0x402 · +0x414 / +0x41b`; `GetMessageBoxFontForDpi +0x23 · +0x37 · +0x8f · +0xa6 · +0xb2`; `GetPrimaryMonitor` 전부; `GetDialogMonitor +0x18 / +0x1b · +0x2a · +0x60 · +0x79`; `GetMonitorWorkRect +0x6c · +0xa6 · +0xab`; `GetDpiForSystem` 전부; `IsThreadDesktopComposited` 전부; `GetDpiForCurrentProcess +0x13 · +0x28`; `GetDpiDependentMetric +0x41 ~ +0x70`; `GetDpiCacheIndex +0xe · +0x4f`; `GetDpiServerInfoForCurrentThread` 의 call 넷 · `+0x41 · +0x59 · +0x5e`; `ValidateHwnd +0x17 · +0x20 · +0x3c · +0x75 · +0x86 · +0x9f · +0xa4 · +0xae · +0xb4 · +0xc6`; `HMValidateHandleNoSecure +0xf · +0x38 · +0x45 · +0x72 · +0x85 ~ +0x8e`; `_PeekMessage +0x38`; `DialogBox2 +0x87 ~ +0x96 · +0xaf · +0x12a · +0x142 · +0x230 ~ +0x239 · +0x23f · +0x26b · +0x280 / +0x283 · +0x29b · +0x2a9 · +0x2b5 · +0x2f5 · +0x37d · +0x3bd · +0x3c4 · +0x3ea`; `EndDialog +0x4b · +0x65 · +0x70 · +0x7f · +0xa9 · +0xc2 · +0xd9 · +0x113`; `MB_DlgProc +0x22 ~ +0x6b` (메시지 풀이) · `+0xcc ~ +0x1ec` · `+0x228 ~ +0x2a5` · 떨어진 범위 `+0x5eac8 ~ +0x5eb10`.
  - win32kfull `NtUserGetThreadState +0xf ~ +0x21 · +0x97 · +0x16c / +0x171`; `xxxClientThreadSetup +0x26 · +0x49 / +0x4c · +0x58`; `W32pThreadCallout +0x61`.
  - win32kbase `xxxCreateThreadInfo +0x4e / +0x57 · +0x731 ~ +0x7a1 · +0x1350 ~ +0x1389`; `UserThreadCallout +0x36c`.
  - ntoskrnl `KiSystemServiceStart +0x907 · +0x91f / +0x924`; `KiConvertToGuiThread +0x4`.
  - win32u `_syscalls.tsv` — 결과서가 쓴 번호 40개 전부.
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **창 API 넷은 은행 창구 넷이다.** 창구마다 손님에게 "번호표 (TEB `+0x78`) 있으세요?" 를 묻는다. 없으면 본점 (커널) 에 "이 손님 등록해 주세요" (`NtUserGetThreadState(0xe)`) 를 부탁한다.
- 본점은 대답으로 **"1 (네)"** 만 하지만, 그 전화를 받는 순간 손님 카드를 만들어 번호표를 손님 주머니에 넣어 주고 (TEB `+0x78`), 지점에 "새 손님 왔어요" 라고 되전화한다 (`__ClientThreadSetup`).
- 에뮬레이터 본점은 "0 (아니요)" 라고 대답해서, 창구 넷이 모두 문을 닫았다 `[추측: 비유]`.
- 대답만 1 로 고치면 창구는 열린다. 하지만 상자를 진짜 그리려면 책상 (TEB `+0x820`) · 기준 글자 크기 (`psi+0x1b56`) · 모니터 안내판 (`gSharedInfo+0x18`) · 펜과 종이 (GDI) 가 있어야 한다.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{user32,win32kfull,win32kbase,ntoskrnl}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `user32!MessageBoxW` (rva `0x5ed90`) | `+0x40` | `+0x45` | `MessageBoxTimeoutW` (`0x5edf0`) — 타임아웃 −1 (`+0x35`), 언어 0 (`+0x3a`) | [정적] |
| `user32!MessageBoxA` (`0x96e40`) | `+0x40` | `+0x45` | `MessageBoxTimeoutA` (`0x96f90`) → `+0xfb / +0x100 MessageBoxTimeoutW` | [정적] |
| `user32!MessageBoxTimeoutW` | `+0x18a` | `+0x18f` | `MessageBoxWorker` (`0x5f1d4`) | [정적] |
| 〃 | `+0xd2` · `+0x12c` | `+0xd9` · `+0x133` | `NtUserGetThreadState` — **죽은 가지** (`+0xcb ADD RAX,0x800` 뒤 `+0xce JNZ` — TEB 주소 + 0x800 은 0 이 안 됨) | [정적] |
| `user32!MessageBoxWorker` | **`+0x62`** | **`+0x69`** | **`NtUserGetThreadState(0xe)`** (IAT `0xb65f8`, win32u `0x1000`) — 가드 | [정적] |
| 〃 | `+0x308` | `+0x30f` | `NtUserModifyUserStartupInfoFlags(1, 0)` (`0x10f9`) | [정적] |
| 〃 | `+0x2be` | `+0x2c3` | `TextAlloc` (단추 글) | [정적] |
| 〃 | `+0x349` | `+0x34e` | `SoftModalMessageBox` (`0x21f20`) | [정적] |
| `user32!SoftModalMessageBox` | `+0x65e` | `+0x665` | `NtUserGetThreadState(0xe)` — 겹친 가드 (TEB `+0x78` 0 이면 또) | [정적] |
| 〃 | `+0xdd` | `+0xe2` | `LoadIcoCur` (아이콘 유형일 때만 — `MB_OK` 는 `+0xbd JNZ → +0x61c`) | [정적] |
| 〃 | `+0xf5` · `+0x63f` | `+0xfc` · `+0x646` | `NtUserGetDCEx` (`0x108a`) — 0 이면 한 번 더, 또 0 이면 `+0x657 → +0x5ea` | [정적] |
| 〃 | `+0x10d` | `+0x112` | `GetDpiForSystem` (`0x6de0`) | [정적] |
| 〃 | `+0x114` | `+0x119` | `GetMessageBoxFontForDpi` (`0x21900`) → `+0xb2 CreateFontIndirectW` | [정적] |
| 〃 | `+0x126` · `+0x149` | `+0x12d` · `+0x14e` | `SelectObject` · `UserGetCharDimensionsEx` — `+0x151 TEST EDX` · `+0x153 JZ → +0x69f` (글자 폭 0 이면 0) | [정적] |
| 〃 | `+0x1c2` · `+0x1e3` · `+0x261` · `+0x3ba` · `+0x4e6` | `+0x1c7` · `+0x1e8` · `+0x266` · `+0x3bf` · `+0x4eb` | `GetDpiDependentMetric(지표 5 · 6 · 0xc · 0xd · 2, dpi)` — 바로 앞마다 `GetDpiForSystem` (`+0x1b6` · `+0x1d7` · `+0x255` · `+0x3ae` · `+0x4da`) | [정적] |
| 〃 | `+0x1fa` | `+0x1ff` | `GetDialogMonitor(NULL, 1)` → `+0x60 NtUserGetForegroundWindow` (`0x1037`) · `+0x79 GetPrimaryMonitor` | [정적] |
| 〃 | `+0x20a` | `+0x20f` | `GetMonitorWorkRect` | [정적] |
| 〃 | `+0x212` | `+0x217` | `GetDpiServerInfoForCurrentThread` (`0xa777c`) → `+0x66 CallWin32uFunction<…NtUserEnsureDpiServerInfoForDpi…>` (조건부) | [정적] |
| 〃 | `+0x2ef` | `+0x2f6` | `NtUserReleaseDC` (`0x151c`) | [정적] |
| 〃 | `+0x40e` · `+0x422` · `+0x4b9` · `+0x511` | `+0x413` · `+0x429` · `+0x4be` · `+0x516` | `MB_FindDlgTemplateSize` · `RtlAllocateHeap` · `MB_UpdateDlgHdr` · `MB_AddPushButtons` (템플릿 만들기) | [정적] |
| 〃 | `+0x550` · `+0x558` | `+0x555` · `+0x55f` | `LoadIcoCur` (화살표 커서) · `NtUserSetCursor` (`0x1018`) | [정적] |
| 〃 | `+0x581` | `+0x588` | `NtUserMessageBeep` (`0x14dd`) — `+0x564 TEST [RSI+0x28],0x80` · `+0x57f JZ` (아이콘 유형일 때만) | [정적] |
| 〃 | **`+0x5b1`** | **`+0x5b6`** | **`InternalDialogBox` (`0x20ae8`)** — `MB_DlgProc` | [정적] |
| 〃 | `+0x628` | `+0x62f` | `NtUserSetCursor` (옛 커서로) | [정적] |
| `user32!DialogBoxParamW` (`0x66260`) | `+0x2a` · `+0x41` | `+0x31` · `+0x48` | `FindResourceExW` · `LoadResource` (0 이면 −1) | [정적] |
| 〃 | `+0x6d` | `+0x72` | `DialogBoxIndirectParamAorW` (`0x621b0`) → `+0x20 GetSizeDialogTemplate` · `+0x67 InternalDialogBox` | [정적] |
| `user32!InternalDialogBox` | `+0xaf` | `+0xb4` | `GetDesktopWindow` (`0xf890`) — 부모 hwnd ≠ 0 일 때 (`+0x2d / +0x30`) · 같은 가드 | [정적] |
| 〃 | `+0xe0` · `+0xe8` | `+0xe5` · `+0xef` | `LoadIcoCur` (`IDC_WAIT`) · `NtUserSetCursor` — `psi+0x7b4 ≠ 0` 일 때만 (`+0x3e / +0x45`) | [정적] |
| 〃 | **`+0x70`** | **`+0x75`** | **`InternalCreateDialog` (`0x1eb90`)** — NULL 이면 `+0x78 JZ → +0x151` → −1 | [정적] |
| 〃 | **`+0x8a`** | **`+0x8f`** | **`DialogBox2` (`0x11154`)** — 모달 고리 | [정적] |
| `user32!InternalCreateDialog` | **`+0xd39`** | **`+0xd40`** | **`NtUserGetThreadState(0xe)`** — 가드 | [정적] |
| 〃 | `+0x63f` · `+0x975` | `+0x644` · `+0x97a` | `VerNtUserCreateWindowEx` → `NtUserCreateWindowEx` (`0x106f`) — 대화상자 · 컨트롤 | [정적] |
| 〃 | `+0x653` · `+0x667` | `+0x658` · `+0x66c` | `ValidateHwnd` · `ValidateDialogPwnd` (→ `NtUserSetDialogPointer` `0x1552`) | [정적] |
| 〃 | `+0x9a7` · `+0x9ba` · `+0xa64` | `+0x9ac` · `+0x9bf` · `+0xa69` | `SendMessageW` ×2 · `SendMessageWorker` | [정적] |
| 〃 | `+0xf7a` · `+0xf89` | `+0xf81` · `+0xf8e` | `NtUserShowWindow` (`0x1052`) · `UpdateWindow` | [정적] |
| `user32!DialogBox2` | `+0x26b` | `+0x270` | `_PeekMessage` → `+0x38 NtUserPeekMessage` (`0x1001`) | [정적] |
| 〃 | `+0x23f` | `+0x246` | `NtUserWaitMessage` (`0x100a`) | [정적] |
| 〃 | `+0x29b` · `+0x2a9` · `+0x2b5` | `+0x2a0` · `+0x2ae` · `+0x2ba` | `IsDialogMessageW` · `TranslateMessage` · `DispatchMessageWorker` | [정적] |
| 〃 | `+0x37d` | `+0x384` | `NtUserPostQuitMessage` (`0x14ee`) — `WM_QUIT` 받았을 때 다시 걸기 | [정적] |
| 〃 | `+0x3c4` | `+0x3cb` | `NtUserDestroyWindow` (`0x1095`) | [정적] |
| `user32!MB_DlgProc` (`0x50ab0`) | `+0xec` · `+0x197` · `+0x1d4` | `+0xf3` · `+0x19e` · `+0x1db` | `NtUserSetMsgBox` (`0x1573`) · `NtUserSetFocus` (`0x104c`) · `NtUserSetTimer` (`0x1016`, 타임아웃 ≠ −1 일 때만) — `WM_INITDIALOG` | [정적] |
| 〃 | `+0x5ead9` · **`+0x5eae4`** | `+0x5eade` · **`+0x5eae9`** | `EndTaskModalDialog` · **`EndDialog`** — `WM_COMMAND` (떨어진 범위) / `WM_TIMER` (`+0x266` 뒤 `0x7d00`) | [정적] |
| `user32!EndDialog` (`0x219e0`) | `+0xa9` · `+0xc2` · `+0xd9` · `+0x113` | `+0xb0` · `+0xc9` · `+0xe0` · `+0x11a` | `NtUserSetWindowPos` (`0x1021`) · `NtUserSetActiveWindow` (`0x10cd`) · `NtUserPostMessage` (`0x100d`) · `NtUserZapActiveAndFocus` (`0x15d6`) | [정적] |
| `user32!LoadIconW` (`0x559e0`) | `+0x18` | `+0x1d` | `LoadIcoCur` (`0x618c`) | [정적] |
| `user32!LoadIcoCur` | **`+0x23e`** | **`+0x245`** | **`NtUserGetThreadState(0xe)`** — 가드 | [정적] |
| 〃 | `+0x1e5` | `+0x1ec` | `NtUserFindExistingCursorIcon` (`0x1039`) — 0 아니면 `+0x1f7 JNZ → +0x135` (자원 안 읽음) | [정적] |
| `user32!GetSystemMetrics` (`0x19080`) | `+0x75` · `+0xd3` | `+0x7a` · `+0xd8` | (CFG 간접 `[guah+0x90]`) · `RealGetSystemMetrics` (`0x8700`) | [정적] |
| 〃 | `+0xba` | `+0xc1` | `NtUserLoadUserApiHook` (`0x14cc`) — `psi+0x0` 비트 `0x10` 일 때 | [정적] |
| `user32!RealGetSystemMetrics` | **`+0x11c`** | **`+0x123`** | **`NtUserGetThreadState(0xe)`** — 가드 | [정적] |
| 〃 | `+0x233` | `+0x23a` | `WTSGetServiceSessionId` — 지표 `0x1000` 일 때 (`+0x2d / +0x33`) | [정적] |
| `user32!PtiCurrent` (`0x19d10`) | `+0x2c` | `+0x33` | `NtUserGetThreadState(0xe)` — 0 아니면 `+0x3b JNZ → +0x14` (TEB `+0x78` 다시 읽음) | [정적] |
| `user32!ClientThreadSetup` (`0x4f810`) | `+0x29c` | `+0x2a3` | `NtUserGetThreadDesktop` (`0x107b`) → `+0x2b0 gfSystemInitialized` | [정적] |
| 〃 | `+0x2f8` | `+0x2ff` | `RtlAllocateHeap(pUserHeap, 8, 0x48)` → `+0x304 pdiLocal` · `+0x324 [TEB+0x820]` — TEB `+0x820` 0 일 때 (`+0x2de / +0x2e6`) | [정적] |
| `ntoskrnl!KiSystemServiceStart` | `+0x91f` | `+0x924` | `KiConvertToGuiThread` — `+0x907 CMP EDI,0x20` (win32k 표) | [정적] |
| `ntoskrnl!KiConvertToGuiThread` | `+0x4` | `+0x9` | `PsConvertToGuiThread` (이름만) | [정적] |
| `win32kfull!W32pThreadCallout` (rva `0x13bdb0`) | `+0x61` | `+0x68` | `UserThreadCallout` (win32kbase `0x1977a0`) | [정적] |
| `win32kbase!UserThreadCallout` | `+0x36c` | `+0x371` | `xxxCreateThreadInfo` (`0x194d84`) | [정적] |
| `win32kbase!xxxCreateThreadInfo` | `+0x745` | `+0x74c` | `PsGetThreadId([pti+0])` → **`+0x756 [TEB+0x78]`** | [정적] |
| 〃 | **`+0x137b`** | **`+0x1380`** | ApiSet 표 `[+0x4e8]` 간접 (CFG) = **win32kfull `xxxClientThreadSetup`** (`0x1c4f60`) — 음수면 `+0x1389 JS` | [정적] |
| `win32kfull!xxxClientThreadSetup` | **`+0x4c`** | **`+0x53`** | **`KeUserModeCallback(0x54, …)`** → user32 `__ClientThreadSetup` (콜백 표 84번째, run85) | [정적] |
| `win32kfull!NtUserGetThreadState` (`0x1ec5b0`) | `+0x21` | `+0x28` | `EnterSharedCrit` — **종류 `0xe` 는 `+0x14 JZ` 로 여기 안 옴** | [정적] |

### 1. 가드 — 실패로 나가는 첫 가지 (질문 1) [정적]

공통 꼴 (넷 다): `GS:[0x30]` → `CMP qword [TEB+0x78],0` → `JZ` → `MOV ECX,0xe` → `CALL [0xb65f8]` → `TEST RAX,RAX` → `JNZ 계속` / 0 이면 아래 반환.

| API | 가드가 든 함수 | 실패 조건 | 반환 | 줄 |
|---|---|---|---|---|
| `MessageBoxW` · `MessageBoxA` | `MessageBoxWorker` (A 는 `MessageBoxTimeoutA` 에서 글자를 W 로 바꾼 뒤 같은 길) | TEB `+0x78` == 0 **그리고** syscall 반환 0 | **0** | `+0x48 GS:[0x30]` · `+0x58 CMP [RAX+0x78],R13` · `+0x5c JNZ → +0x7a` · `+0x5e LEA ECX,[R13+0xe]` · `+0x62 CALL` · `+0x6e TEST` · `+0x71 JNZ → +0x7a` · `+0x73 XOR EAX,EAX` · `+0x75 JMP → +0x388` |
| (겹친 가드) | `SoftModalMessageBox` | 같음 | 0 | `+0x4f CMP [RAX+0x78],RCX` · `+0x53 JZ → +0x659` · `+0x65e CALL` · `+0x66d JNZ → +0x59` · `+0x673 JMP → +0x5ff`. TEB `+0x78` 이 그대로 0 이면 **같은 syscall 을 한 번 더** |
| (가드 아님) | `MessageBoxTimeoutW +0xc2 ~ +0xe1` · `+0x11c ~ +0x13b` | TEB 주소 + `0x800` 이 0 인가 → 늘 아님 | — | 죽은 가지. 로그의 "`MessageBox*` 안 syscall 하나" 와 맞음 |
| `DialogBoxParamW` | `InternalCreateDialog` → `InternalDialogBox` | 같음 → `InternalCreateDialog` NULL | **−1** | `InternalCreateDialog +0x133 GS:[0x30]` · `+0x13c CMP [RAX+0x78],RDI` · `+0x140 JZ → +0xd37` · `+0xd39 CALL` · `+0xd45 TEST` · `+0xd48 JNZ → +0x146` · `+0xd4e XOR EAX,EAX` · `+0xd50 JMP → +0xbb9`; `InternalDialogBox +0x78 JZ → +0x151` · `+0x153 JNZ` / `+0x15c JZ` → 조각 `0x1800ab7c2` (`OR RAX,-1`) |
| (가드 앞 −1, 가드 아님) | `DialogBoxParamW` · `DialogBoxIndirectParamAorW` | 자원 못 찾음 · 템플릿 크기 0 | −1 | `DialogBoxParamW +0x1d OR RBX,-1` · `+0x39 JZ` · `+0x53 JZ`; `DialogBoxIndirectParamAorW +0x27 JNZ` 안 타면 `+0x29 OR RAX,-1` |
| (부모 있을 때 앞 가드) | `GetDesktopWindow` (`InternalDialogBox +0xaf`) | 부모 hwnd ≠ 0 · TEB `+0x78` 0 · 반환 0 | 데스크톱 NULL (그 뒤 `InternalCreateDialog` 가 또 가드) | `+0x15 CMP [RAX+0x78],RBX` · `+0x19 JZ → +0x80` · `+0x85 CALL` · `+0x94 JNZ → +0x1b` · `+0x96 MOV RDI,RBX` · `+0x99 JMP → +0x36`. 로그 "3643 · 3644 (둘) → 3645 (−1)" = 이 둘로 보임 `[추측: 부모 인자 확인 필요]` |
| `LoadIconW` | `LoadIcoCur` | 같음 | **0** | `+0x2d GS:[0x30]` · `+0x52 CMP [RAX+0x78],R8` · `+0x56 JZ → +0x239` · `+0x23e CALL` · `+0x24d TEST` · `+0x250 JNZ → +0x5c` · `+0x256 XOR EAX,EAX` · `+0x258 JMP → +0x138` |
| (syscall 없는 0) | `LoadIcoCur +0x290 ~ +0x2a9` | 자원 번호가 user32 안 표에 있고 (`+0x22d CMP [RCX],R14W` · `+0x231 JZ → +0x290`) 그 칸 비트 0 이 켜졌고 TEB `+0x810` ≥ `0x400` | 0 | `+0x290 TEST byte [RCX+4],1` · `+0x294 JZ` · `+0x29f CMP [RAX+0x810],0x400` · `+0x2a9 JNC → +0x256`. 로그의 "`LoadIconW` syscall 0 개" 는 **이 길이거나 그때 TEB `+0x78` 이 이미 ≠ 0** 이었을 것 `[추측]` |
| `GetSystemMetrics` | `RealGetSystemMetrics` | 같음 | **0** | `+0xd GS:[0x30]` · `+0x19 CMP [RAX+0x78],0` · `+0x1e JZ → +0x117` · `+0x11c CALL` · `+0x128 TEST` · `+0x12b JNZ → +0x24` · `+0x131 XOR EAX,EAX`. 지표 `0x1000` 비교 (`+0x2d / +0x33`) 는 가드 **뒤** |
| (가드 앞 읽기) | `GetSystemMetrics` | — (가드 아님) | — | `+0x9e MOV RAX,[gpsi]` · `+0xa5 TEST byte [RAX],0x10` — `psi` 가 NULL 이면 접근 위반. 비트가 켜지면 `+0xba NtUserLoadUserApiHook` |

- **같은 도우미는 없다.** 컴파일러가 각 함수 안에 풀어 넣은 꼴이다. 함수로 남은 같은 꼴: `PtiCurrent` (`0x19d10`, 62 B — syscall 이 0 아니면 `+0x3b JNZ → +0x14` 로 **TEB `+0x78` 을 다시 읽음**, 0 이면 `+0x3d → +0x31` 로 0) · `IsGUIThread` (`0x52210`, 인자가 참일 때만 syscall, 0 이면 `+0x43 RtlSetLastWin32Error`). 가져오기 칸 `0xb65f8` 을 쓰는 곳 85 (CALL 79 · JMP 6 — `GetCapture` · `GetInputState` · `InSendMessageEx` · `GetMessageExtraInfo` · `GetMessageTime` · 서수 2599) — `ms/disassemble/user32/_refs/0xb65f8.tsv`.
- **그러니 고칠 자리는 syscall 대답 하나** — `0x1000` 의 인자 `0xe` 에 1.

### 2. 종류 `0xe` 와 GUI 변환 (질문 3) [정적]

- **뜻**: 칸을 돌려주지 않는 **상수 1**. `NtUserGetThreadState +0xf MOV EBX,ECX` · `+0x11 CMP ECX,0xe` · `+0x14 JZ → +0x16c MOV EAX,1` · `+0x171 JMP → +0x97` (끝). `+0x21 EnterSharedCrit` 앞이다. 다른 종류 (0 ~ `0xd`) 는 이번 범위 밖.
- **정상 반환 = 1.** 0 은 user32 가 실패로 읽는다 (넷의 가드 · `PtiCurrent` · `IsGUIThread`).
- **목적 = 부수 효과.** 나무 (오프셋은 ret 자리):

```
(유저) user32 가드 → win32u NtUserGetThreadState (0x1000)  ── SYSCALL
nt!KiSystemServiceStart                         +0x907 CMP EDI,0x20 (win32k 표)                    [정적]
 └─ +0x924  KiConvertToGuiThread
             └─ +0x9  PsConvertToGuiThread      (이름만 — 안에서 win32k 스레드 호출로 가는 줄은 안 읽음 [추측])
win32kfull!W32pThreadCallout
 └─ +0x68  UserThreadCallout (win32kbase)
            └─ +0x371  xxxCreateThreadInfo
                        ├─ +0x57   [RSP+0x78] ← 커널 GS:[0x30]  (= 이 스레드 TEB [추측: KPCR +0x30])
                        ├─ +0x74c  PsGetThreadId([pti+0])  → +0x756 [TEB+0x78] ← 반환   (TEB ≠ 0 일 때 +0x740)
                        ├─         +0x772 [pti+0x208] ← TEB+0x800 · +0x787 [TEB+0x8e8] ← [pti+0x190] · +0x794 [TEB+0x8ec] ← ESI
                        └─ +0x1380 ⇢ [ApiSet 표+0x4e8] = win32kfull!xxxClientThreadSetup      음수 → +0x1389 JS 실패
                                     └─ +0x53  KeUserModeCallback(0x54, …)
                                                ⇢ user32!__ClientThreadSetup → ClientThreadSetup   (run85)
                                                   ├─ +0x9a   GdiProcessSetup                (첫 GUI 스레드, run86 A12)
                                                   ├─ +0xca   [TEB+0x800] 비트 3 "이미 함" · +0xd3 [TEB+0x910] ← 2
                                                   ├─ +0x2a3  NtUserGetThreadDesktop → +0x2b0 gfSystemInitialized
                                                   └─ +0x2ff  RtlAllocateHeap(pUserHeap, 8, 0x48) → +0x324 [TEB+0x820]   (TEB+0x820 == 0 일 때)
win32kfull!NtUserGetThreadState  +0x14 JZ → +0x16c  EAX = 1
```

- (`⇢` 는 간접 호출 · 되부름. `W32pThreadCallout` 을 누가 부르는지는 커널 `PsConvertToGuiThread` 안이라 안 읽음 — 결과서도 "변환이 win32k 스레드 호출을 부름" 까지.)
- **TEB `+0x78` 의 값**: `PsGetThreadId` 반환이다 `[정적: 가져오기 칸 rva 0x2d0268 이름]`. 즉 스레드 ID 로 보인다 `[추측 — 포인터 (`pti`) 가 아님]`. user32 는 이 칸을 **0 인지만** 본다.
- **TEB `+0x820` 의 정체**: user32 전역 `pdiLocal` 의 PDB 꾸민 이름이 `?pdiLocal@@3PEAUtagDESKTOPINFO@@EA` 라 `tagDESKTOPINFO *` 를 담는 칸이다 `[정적: PDB 공개 이름]` (결과서는 이름 `[추측]`). user32 가 만든 것은 0 채운 `0x48` B.

### 3. `MessageBoxW(NULL, 글, 제목, MB_OK)` 한 번의 최소 길 (질문 2) [정적]

전제: 가드를 지났다 (반환 1). 차례는 `.asm` 위치 순서 (조건은 표시). gdi32 안은 이름만 (Q270). 결과서 표 (2) 의 0 ~ 32 를 묶어 줄였다.

| 차례 | syscall (번호) | 조건 · 인자 | syscall 없이 읽는 칸 |
|---|---|---|---|
| 0 | — | `MessageBoxW` → `MessageBoxTimeoutW` (`MSGBOXDATA` `memset(0xa0)`) | `gfServerProcess` · TEB `+0x8e8` (0 이면 `gDefaultDpiContext` 파일 값 `0x6010`) |
| 1 | `NtUserGetThreadState` (`0x1000`, `0xe`) → 1 | 가드 (`MessageBoxWorker +0x62`) | TEB `+0x78` |
| 2 | — | 단추 글 만들기 (`TextAlloc`), 제목 NULL 이면 "오류" 문구 | **`psi + 40*k + 0x3a4`** (글 `0x20` B) · `+0x3c4` · `+0x3c8` · TEB `+0x810` |
| 3 | `NtUserModifyUserStartupInfoFlags` (`0x10f9`) | `(1, 0)` | — |
| 4 | (`NtUserGetThreadState` 한 번 더) | `SoftModalMessageBox` 가드 — TEB `+0x78` 0 일 때 | TEB `+0x78` |
| 5 | `NtUserGetDCEx` (`0x108a`) | `(0, 0, 3)` = 화면 DC. **0 → 한 번 더 → 0 이면 `MessageBoxW` 0** | — |
| 6 | `NtUserGetDpiForCurrentProcess` (`0x1440`) — 처음 한 번 | `GetDpiForSystem` → `GetDpiForCurrentProcess` (`IsThreadDesktopComposited` 안에 가드 또 하나) | **`psi+0x1b66`** · TEB `+0x820` → `+0x40` 비트 0 (TEB `+0x820` 0 이면 0 으로 봄, 널 검사 있음) |
| 7 | (gdi32) `CreateFontIndirectW` → `NtGdiHfontCreate` (`0x1057`) | 글꼴 | `psi+0x138c ~ +0x13e4` (LOGFONT) · **`psi+0x1b56` — 0 이면 0 나눔** |
| 8 | (gdi32) `SelectObject` · `NtGdiGetTextMetricsW` (`0x106e`) · `NtGdiGetTextExtentExW` (`0x12e4`) · `NtGdiDeleteObjectApp` (`0x1020`) | **글자 폭 0 이면 `MessageBoxW` 0** (`+0x153`) | (PEB `+0xf8` GDI 공유 표 — Q270) |
| 9 | `NtUserEnsureDpiDepSysMetCacheForPlateau` (`0x141c`) — 칸이 −1 일 때만 | `GetDpiDependentMetric` 다섯 번 (지표 5 · 6 · `0xc` · `0xd` · 2) | **`psi + 4*(캐시번호*0x1e + 지표) + 0x8ec`** (0 이면 크기 0) |
| 10 | `NtUserGetForegroundWindow` (`0x1037`) | `GetDialogMonitor(NULL, 1)` — 0 이면 주 모니터 | **`gSharedInfo+0x18` → `[+8]` + `gSharedInfo+0x20`** · 모니터 `+0x1c` · `+0x2c` · `+0x3c` · `+0x3e` |
| 11 | `NtUserEnsureDpiServerInfoForDpi` (`0x141e`) — 조건부 | `GetDpiServerInfoForCurrentThread` | `psi + (번호+0x31)*0x68` (`+0x0` · `+0x8` HFONT) |
| 12 | `NtUserReleaseDC` (`0x151c`) | 5 의 DC | — |
| 13 | — | 상자 위치 | **TEB `+0x820` → `+0x30` (널 검사 없음 — 0 이면 접근 위반)** |
| 14 | — | 템플릿 (`RtlAllocateHeap(pUserHeap)`, `DS_SETFONT`) | `pUserHeap` |
| 15 | `NtUserFindExistingCursorIcon` (`0x1039`) · `NtUserSetCursor` (`0x1018`) | 화살표 커서. `FindExisting` 이 0 아니면 자원 안 읽음 | (`LoadIcoCur` 가드 또 하나) |
| 16 | `NtUserMessageBeep` (`0x14dd`) | 아이콘 유형일 때만 — `MB_OK` 는 없음 | — |
| 17 | (`NtUserSetCursor` 대기 커서) | `psi+0x7b4 ≠ 0` 일 때만 | **`psi+0x7b4`** |
| 18 | (`NtUserGetThreadState` 한 번 더) | `InternalCreateDialog` 가드 | TEB `+0x78` |
| 19 | `NtUserCreateWindowEx` (`0x106f`) | 대화상자 창 | — |
| 20 | `NtUserValidateHandleSecure` (`0x15ce`) · `NtUserMapDesktopObject` (`0x14d9`) — 칸이 안 맞을 때만 | `ValidateHwnd` — 공유 핸들 표를 syscall 없이 읽음 | 아래 5절 |
| 21 | `NtUserSetDialogPointer` (`0x1552`) | `ValidateDialogPwnd` — 대화상자 자료를 창에 붙임 | — |
| 22 | `NtUserCreateWindowEx` ×(컨트롤 수) | `MB_OK`: 글 + 단추 = 최소 2 `[공백: 정확한 수]` | — |
| 23 | `NtUserMessageCall` (`0x1005`) — 어느 쪽인지 `[공백]` | `WM_SETFONT` 류 · `WM_INITDIALOG` (같은 스레드 창이면 user32 안에서 바로 부를 수 있음) | 창 물체 칸 |
| 24 | `NtUserSetMsgBox` (`0x1573`) · `NtUserSetFocus` (`0x104c`) | `MB_DlgProc` 가 `WM_INITDIALOG` 받을 때 (타이머는 타임아웃 −1 이라 없음) | `MSGBOXDATA +0x7c` · `+0x38` |
| 25 | `NtUserShowWindow` (`0x1052`) · `NtUserSetForegroundWindow` (`0x155b`) 따위 | 조건 `[공백]` | — |
| 26 | `NtUserPeekMessage` (`0x1001`) · `NtUserWaitMessage` (`0x100a`) (고리) | 4절 | TEB `+0x800` 쪽 칸 · `psi+0x88c` · `psi+0x764` 비트 `0x8000` |
| 27 | `NtUserDispatchMessage` (`0x1031`) 등 — 조건부 | `IsDialogMessageW` → `TranslateMessage` → `DispatchMessageWorker` | 창 물체의 프로시저 칸 |
| 28 | `EndDialog` 안: `NtUserSetWindowPos` (`0x1021`) · `NtUserSetActiveWindow` (`0x10cd`) / `NtUserZapActiveAndFocus` (`0x15d6`) · `NtUserPostMessage` (`0x100d`); 고리 뒤 `NtUserDestroyWindow` (`0x1095`) | 4절 | 대화상자 자료 `+0x18` · `+0x20` |
| 29 | `NtUserSetCursor` (`0x1018`) | 옛 커서로 (`+0x5cd / +0x5d0`) | — |
| 30 | 반환 | `InternalDialogBox` 결과 (−1 이면 0, `+0x5b8 / +0x5be CMOVNZ`) → `MessageBoxW` | — |

- `[공백]` 으로 남은 것: 19 ~ 27 중 어느 호출이 같은 스레드 안에서 syscall 없이 풀리는지, `NtUserCreateWindowEx` 가 hwnd 를 돌려줄 때 커널이 핸들 표 · 창 물체를 만드는 법 (범위 밖), 컨트롤 수 · `NtUserMessageCall` 횟수. `InternalCreateDialog` (4144 B) 는 틀만 → **Q274 N1**.

### 4. 모달 고리가 끝나는 조건 [정적]

- 고리 = `DialogBox2` (`InternalDialogBox +0x8a`). 한 바퀴: `+0x26b _PeekMessage` (→ `NtUserPeekMessage`) → 없으면 `+0x23f NtUserWaitMessage` 에서 잠 → 있으면 `+0x29b IsDialogMessageW` → `+0x2a9 TranslateMessage` → `+0x2b5 DispatchMessageWorker`.
- **정상 끝 = `EndDialog(hwnd, 번호)`**:
  - `EndDialog +0x65 MOV RAX,[창+0x128]` → `[+8]` (대화상자 자료) · `+0x70 OR [자료+0x18],1` (끝 표시) · `+0x7f MOV [자료+0x20],R14` (결과) → `+0xa9 NtUserSetWindowPos` (숨기기) → `+0xd9 NtUserPostMessage` (고리 깨우기).
  - `DialogBox2` 는 매 바퀴 자료를 본다 — `+0x12a ~ +0x142` (자료 NULL 이거나 끝 표시 → `+0x389`) · `+0x230 ~ +0x239` (창 `+0x2a` 의 `0xc000` → 지워짐). 빠지면 `+0x3c4 NtUserDestroyWindow` 뒤 `+0x3bd [자료+0x20]` 을 반환 (`+0x3ea`).
- **`EndDialog` 를 부르는 것 = `MB_DlgProc`** (메시지 풀이 `+0x22 ~ +0x6b`, 뺄셈 사슬):

| 메시지 | 가는 곳 | 하는 일 |
|---|---|---|
| `0x0f` | `+0x3dc` | `DrawCommandRectangle` |
| `0x53` | `+0x3a6` | 도움말 (`SendHelpMessage`) |
| `0x82` | `+0x8f` | 창 정리 (`DefWindowProcWorker`) · 타이머 지우기 |
| **`0x110` (`WM_INITDIALOG`)** | `+0xdc` | `NtUserSetMsgBox` · 첫 단추로 `NtUserSetFocus` · (타임아웃 있으면) `NtUserSetTimer` |
| **`0x111` (`WM_COMMAND`)** | `+0x275` | `LOWORD(wParam)` 3 ~ 6 · 7 → 곧장 `EndTaskModalDialog` + **`EndDialog(hwnd, 번호)`** (`+0x5ead6 ~ +0x5eae4`). 1 · 2 → 그 번호 단추가 있을 때만 (`+0x5eaf0 GetDlgItem` ≠ 0). 9 → 도움말. 그 밖 0 반환 |
| **`0x113` (`WM_TIMER`)** | `+0x266` | `EndTaskModalDialog` + `EndDialog(hwnd, 0x7d00)` (타임아웃) — `MessageBoxW` 는 타이머를 안 걺 |
| `0x136` | `+0xcc` | `psi+0x1280` (브러시) 반환 |
| `0x138` | `+0x228` | `SetTextColor` · `SetBkMode` · `GetSysColorBrush(5)` |
| `0x301` | `+0x21d` | `MB_CopyToClipboard` |

- **그 `WM_COMMAND` 는 누가 만드나**: 사용자 입력 (단추 · Enter · Esc) 이 커널에서 메시지가 되어 `NtUserPeekMessage` 로 들어온다. 에뮬레이터에선 **`NtUserPeekMessage` 의 대답이 유일한 입구** (정책 `335_tune15.md` Q5).
- **비정상 끝 = `WM_QUIT` (`0x12`)**: `DialogBox2 +0x280 CMP EAX,0x12` · `+0x283 JZ → +0x379` · `+0x37d NtUserPostQuitMessage` 로 다시 걸고 빠져나옴. `EndDialog` 가 안 불려 자료 `+0x20` 이 처음 값 (0) 이라 **`MessageBoxW` 0**.

### 5. user32 가 syscall 없이 읽는 공유 칸 (지금까지) [정적]

`psi` = `gpsi` (user32 rva `0xda590`), `gSharedInfo` = rva `0xda350` (run85). 칸 뜻은 `[추측]`, 오프셋 · 읽는 줄은 `[정적]`.

| 칸 | 읽는 줄 | 쓰임 `[추측]` | 비어 있으면 |
|---|---|---|---|
| `gSharedInfo+0x0` (= `psi`) | 거의 모든 곳 | — | 접근 위반 (run85) |
| `gSharedInfo+0x8` (핸들 항목 배열) · `+0x10` (항목 크기) · `psi+0x8` (항목 개수) | `ValidateHwnd +0x3c ~ +0x75` · `HMValidateHandleNoSecure +0xf ~ +0x38` | hwnd 아래 16비트 = 항목 번호 | 검사 실패 → 잘못된 창 핸들 |
| 핸들 항목 `+0x0` · `+0x10` · `+0x18` · `+0x19` · `+0x1a` | `ValidateHwnd +0x86 ~ +0xc6` · `HMValidateHandleNoSecure +0x45 ~ +0x7e` | `+0x18` 종류 (1 = 창) · `+0x19` 지워짐 비트 · `+0x1a` hwnd 윗부분 · `+0x10` 소유 데스크톱 표지 · `+0x0` 창 물체 (커널 쪽 주소) | — |
| TEB `+0x820` → `+0x0` · `+0x8` · `+0x30` · `+0x40` | `HMValidateHandleNoSecure +0x72` · `GetDesktopWindow +0x1b ~ +0x2b` · `SoftModalMessageBox +0x372 / +0x385` · `IsThreadDesktopComposited +0x20 / +0x2c` | `tagDESKTOPINFO` (2절) | **`GetDesktopWindow` · `SoftModalMessageBox` 접근 위반** (`IsThreadDesktopComposited` · 핸들 검사는 견딤) |
| TEB `+0x828` | `HMValidateHandleNoSecure +0x87 / +0x8e` · `GetDesktopWindow +0x2f` | 커널 주소 → user32 주소 차이 | 0 이면 커널 주소 그대로 |
| TEB `+0x840` · `+0x848` | `ValidateHwnd +0x17 · +0x20` | 직전 hwnd 캐시 | 0 이면 느린 길 |
| TEB `+0x810` · `+0x818` | `GetAppCompatFlags2` (run86) · `LoadIcoCur +0x29f` · `MessageBoxWorker +0x1bf` · `RealGetSystemMetrics +0x75` | 예상 Windows 버전 · 호환 | 0 이면 건너뜀 |
| TEB `+0x8e8` | `MessageBoxTimeoutW +0xec` · `GetDpiForSystem +0x29` | 스레드 DPI 문맥 | 0 이면 기본값 |
| `psi+0x0` 비트 `0x10` · `0x80` | `GetSystemMetrics +0xa5` · `InternalCreateDialog +0xb4a` | API 후크 · 기능 | 0 = 안 켜짐 |
| `psi+0x3a4 + 40*k` · `+0x3c4` · `+0x3c8` | `MessageBoxWorker +0x266 ~ +0x2c8` | 단추 글 표 | 빈 글 단추 (상자는 뜸) |
| `psi+0x764` · `+0x7b4` · `+0x88c` · `+0x1280` · `+0x1b5c` | `DialogBox2 +0xaf · +0x2f5` · `InternalDialogBox +0x3e` · `InternalCreateDialog +0xb4f · +0xc50 · +0xcf2` · `MB_DlgProc +0xd3 · +0x149` | 접근성 알림 · 대기 커서 · 입력 상태 · 브러시 | 0 = 안 켜짐 |
| `psi+0x768 + 4*지표` | `RealGetSystemMetrics +0xa9` 뒤 | `GetSystemMetrics` 값 표 | 지표 전부 0 |
| `psi+0x8ec + 4*(번호*0x1e + 지표)` | `GetDpiDependentMetric +0x41 · +0x64` | DPI 의존 지표 (−1 이면 syscall) | 크기 0 |
| `psi+0x8bc` | `RealGetSystemMetrics +0x1b7` · run85 | 비트 열 | — |
| `psi+0x138c ~ +0x13e4` · **`+0x1b56`** · `+0x1b66` | `GetMessageBoxFontForDpi +0x23 ~ +0x37` · `GetDpiCacheIndex +0xe` · `GetDpiForCurrentProcess +0x13` | 메시지 글꼴 · 기준 DPI · 프로세스 DPI | **`+0x1b56 == 0` → 0 나눔** |
| `psi + (번호+0x31)*0x68` | `GetDpiServerInfoForCurrentThread +0x41 ~ +0x5e` | DPI 서버 정보 (번호 ≤ `0x11`, `GetDpiCacheIndex +0x4f CMP EDX,0x12`) | `SelectObject(hdc, 0)` |
| `gSharedInfo+0x18` (rva `0xda368`) → `+0x8` · `gSharedInfo+0x20` (rva `0xda370`) · 모니터 `+0x1c` · `+0x2c` · `+0x3c` · `+0x3e` | `GetPrimaryMonitor +0x0 ~ +0x19` · `GetMonitorWorkRect +0x6c · +0xa6 · +0xab` | 주 모니터 · 작업 영역 | **`+0x18` 이 0 → 접근 위반** (모니터 포인터 0 은 `GetPrimaryMonitor` 가 0 으로 돌려줌 `+0xd / +0x10`) |
| 창 물체 사본 `+0x1c` · `+0x1f` · `+0x2a` · `+0x128 → [+8]` (자료 `+0x18` · `+0x20`) | `DialogBox2 +0x87 ~ +0x96 · +0x12a · +0x235` · `EndDialog +0x4b ~ +0x7f` · `GetDialogMonitor +0x2a` | 스타일 · 상태 · 대화상자 자료 | 자료 칸은 `EndDialog` 가 **쓰므로** 쓰기 가능 메모리여야 함 `[추측: 어느 힙]` |

- `psi` 는 **`0x1b68` B 이상이면 지금까지 본 칸이 다 들어간다** (가장 뒤 `+0x1b66` word, DPI 서버 정보 블록 `+0x13e8 ~ +0x1ab8`). run85 의 "0 채운 `0x1b68` B" 는 크기는 맞고, **값이 0 이면 안 되는 칸** 이 창 API 길에 생긴다 (`+0x1b56` 등).

### 호출 관계

```
MessageBoxW(NULL, 글, 제목, MB_OK)                                                           [정적]
user32!MessageBoxW +0x45 MessageBoxTimeoutW
 └─ +0x18f  MessageBoxWorker
             ├─ +0x69   NtUserGetThreadState(0xe)   가드: 0 → +0x73 0 반환          ── SYSCALL 0x1000
             ├─ +0x2c3  TextAlloc                    psi+0x3a4 단추 글
             ├─ +0x30f  NtUserModifyUserStartupInfoFlags(1, 0)                         ── SYSCALL 0x10f9
             └─ +0x34e  SoftModalMessageBox
                         ├─ +0x665  NtUserGetThreadState(0xe)   (TEB+0x78 0 이면 또)  ── SYSCALL
                         ├─ +0xfc   NtUserGetDCEx(0, 0, 3)       0 → +0x646 또 → 0 반환 ── SYSCALL 0x108a
                         ├─ +0x112  GetDpiForSystem → … NtUserGetDpiForCurrentProcess (처음)   psi+0x1b66
                         ├─ +0x119  GetMessageBoxFontForDpi   psi+0x1b56 IDIV → CreateFontIndirectW (gdi32)
                         ├─ +0x14e  UserGetCharDimensionsEx    폭 0 → +0x153 → 0 반환            (gdi32)
                         ├─ +0x1c7 …+0x4eb  GetDpiDependentMetric ×5   psi+0x8ec (−1 → 0x141c)
                         ├─ +0x1ff  GetDialogMonitor(NULL, 1) → NtUserGetForegroundWindow · GetPrimaryMonitor   gSharedInfo+0x18
                         ├─ +0x2f6  NtUserReleaseDC                                               ── SYSCALL 0x151c
                         │          +0x372 / +0x385  [[TEB+0x820]+0x30]   (0 이면 접근 위반)
                         ├─ +0x555  LoadIcoCur(IDC_ARROW) → NtUserFindExistingCursorIcon          ── SYSCALL 0x1039
                         ├─ +0x55f  NtUserSetCursor                                               ── SYSCALL 0x1018
                         ├─ +0x5b6  InternalDialogBox(…, MB_DlgProc, …)
                         │           ├─ +0x75  InternalCreateDialog
                         │           │          ├─ +0xd40  NtUserGetThreadState(0xe)  가드: 0 → NULL → −1
                         │           │          ├─ +0x644  VerNtUserCreateWindowEx → NtUserCreateWindowEx   ── SYSCALL 0x106f
                         │           │          ├─ +0x658  ValidateHwnd        gSharedInfo+0x8 · TEB+0x820/0x828/0x840
                         │           │          ├─ +0x66c  ValidateDialogPwnd → NtUserSetDialogPointer      ── SYSCALL 0x1552
                         │           │          ├─ +0x97a  VerNtUserCreateWindowEx (컨트롤마다)
                         │           │          ├─ +0x9ac · +0x9bf · +0xa69  SendMessageW · SendMessageWorker   (WM_INITDIALOG ⇢ MB_DlgProc)
                         │           │          └─ +0xf81  NtUserShowWindow                                  ── SYSCALL 0x1052
                         │           └─ +0x8f  DialogBox2
                         │                      ├─ +0x270  _PeekMessage → NtUserPeekMessage             ── SYSCALL 0x1001
                         │                      ├─ +0x246  NtUserWaitMessage                            ── SYSCALL 0x100a
                         │                      ├─ +0x2a0 · +0x2ae · +0x2ba  IsDialogMessageW · TranslateMessage · DispatchMessageWorker
                         │                      │           ⇢ MB_DlgProc  WM_COMMAND → +0x5eae9 EndDialog
                         │                      │                          (+0xb0 SetWindowPos · +0xe0 PostMessage)
                         │                      ├─ +0x384  NtUserPostQuitMessage   (WM_QUIT 일 때)
                         │                      └─ +0x3cb  NtUserDestroyWindow                          ── SYSCALL 0x1095
                         └─ +0x62f  NtUserSetCursor (옛 커서)
```

- (나무 오프셋은 ret 자리. `⇢` 는 메시지로 가는 창 프로시저 호출. 콜스택으로 본 적 없음.)

## 확인된 것

- `[정적]` 창 API 넷의 가드는 같은 꼴 다섯 줄이 각 함수 안에 있다 (`MessageBoxWorker +0x58 ~ +0x75` · `InternalCreateDialog +0x13c ~ +0xd50` · `LoadIcoCur +0x52 ~ +0x258` · `RealGetSystemMetrics +0x19 ~ +0x131`). 공통 도우미 없음. syscall 반환 0 이면 실패, 0 아니면 TEB `+0x78` 을 다시 안 읽고 계속.
- `[정적]` win32kfull `NtUserGetThreadState` 종류 `0xe` = 상수 1 (`+0x14 JZ → +0x16c`), 락 앞.
- `[정적]` win32k syscall → `KiSystemServiceStart +0x91f KiConvertToGuiThread` → `PsConvertToGuiThread` (이름만) … `W32pThreadCallout +0x61 UserThreadCallout +0x36c xxxCreateThreadInfo`. 그 안 `+0x756` 이 TEB `+0x78` 을, `+0x772` 가 `pti+0x208` (= TEB `+0x800`) 을, `+0x787 / +0x794` 가 TEB `+0x8e8` · `+0x8ec` 를 쓴다.
- `[정적]` `xxxCreateThreadInfo +0x137b` → win32kfull `xxxClientThreadSetup +0x4c KeUserModeCallback(0x54)` = user32 `__ClientThreadSetup` 되부름.
- `[정적]` user32 `ClientThreadSetup +0x2d5 ~ +0x324` 가 TEB `+0x820` 이 0 이면 0 채운 `0x48` B (`pdiLocal`, `tagDESKTOPINFO *`) 를 넣는다.
- `[정적]` 메시지 상자 길의 벽: TEB `+0x820` (`SoftModalMessageBox +0x385`, 널 검사 없음) · `psi+0x1b56` (`GetMessageBoxFontForDpi +0xa6`) · `gSharedInfo+0x18` (`GetPrimaryMonitor +0x7`) · GDI (`+0x107` · `+0x153`).
- `[정적]` 모달 고리는 `EndDialog` 의 끝 표시 (`[자료+0x18] |= 1`) 로 끝나고, `EndDialog` 는 `MB_DlgProc` 의 `WM_COMMAND` / `WM_TIMER` 갈래에서만. `WM_QUIT` 이면 `EndDialog` 없이 0.

## 정정 (요청서 · 원문의 짐작이 틀린 곳)

1. **"넷이 같은 도우미를 쓰면 그 함수 하나로"** → 같은 도우미는 없다. 같은 꼴이 각자 안에 있다 (85곳). 고칠 자리는 도우미가 아니라 **syscall 대답 하나**.
2. **"`NtUserGetThreadState(0xe)` 기본 대답 0"** → 정상은 **1**. 0 이 넷을 실패로 보낸다.
3. **"TEB `+0x78` `Win32ThreadInfo` (포인터)"** → 쓰는 줄의 값은 `PsGetThreadId` 반환이다 (스레드 ID 로 보임 `[추측]`). user32 는 0 인지만 본다.
4. **"`LoadIconW` 는 syscall 0 개로 0 = 가드"** → 가드라면 syscall 이 하나 나와야 한다. syscall 없는 0 은 `LoadIcoCur +0x290 ~ +0x2a9` (TEB `+0x810` ≥ `0x400` · 표 비트) 길이거나, 그때 TEB `+0x78` 이 이미 0 이 아니었다 `[추측 → Q273]`.

### 고쳐 읽기 (앞 run)

이번에 닫히거나 좁혀진 앞 run 의 `[추측]` · `[공백]`. 각 문서 맨 위에 "고쳐 읽기 (run142)" 한 줄을 넣었다.

- **run85** — "user32 코드엔 TEB `+0x78` 쓰기 없음 → 쓰는 쪽은 win32k `[추측]`" → **`[정적]`**: win32kbase `xxxCreateThreadInfo +0x756` (값 = `PsGetThreadId` 반환 — 포인터가 아닌 스레드 ID 로 보임 `[추측]`). "변환 뒤 `__ClientThreadSetup` 콜백" → 자리 `[정적]`: `xxxCreateThreadInfo +0x137b` ⇢ win32kfull `xxxClientThreadSetup +0x4c KeUserModeCallback(0x54)`. "`psi` 0 채운 `0x1b68` B 면 길 끝까지" 는 `DllMain` 까지 그대로 맞고, 창 API 길엔 값이 0 이면 안 되는 칸이 생긴다 (`+0x1b56` 등, 5절).
- **run86** — 표 A13 의 답 "아무 값 (0 이면 `GetAppCompatFlags2` 가 0)" → 정상 답은 **상수 1** (`win32kfull!NtUserGetThreadState +0x16c`). 0 이면 창 API 넷이 실패한다. 데이터 칸 표 TEB `+0x78` "포인터 — `NtGdiInit2` 가 GUI 로 바꾼 뒤 0 아님 `[추측]`" → 쓰는 곳 `[정적]` 위와 같음, 값은 포인터가 아닌 스레드 ID 로 보임 `[추측]`.
- **run121** — 다음 요청 후보 ① "`Win32ThreadInfo` 를 유저 TEB 에 놓는 줄 — win32kbase `xxxCreateThreadInfo` 후보 `[공백]`" → **닫힘** `[정적]` `+0x745 / +0x756`. 짐 지도 3.3 `+0x78` 줄 "놓는 줄 못 찾음" 은 짐 지도 바퀴에서 고칠 것 (이번엔 안 고침 — 보고에 제안).

## PE_emulator 쪽 (결과서 "지금 우리 맞나 · 틀리나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 지금 에뮬레이터 | 판단 | 근거 · 할 것 |
|---|---|---|
| `NtUserGetThreadState(0xe)` 기본 대답 0 | **틀림** — 넷이 실패하는 직접 원인 | 정상은 1 (2절). `MessageBoxWorker +0x73` (107/107) · `InternalCreateDialog +0xd4e` → −1 (50/50) · `LoadIcoCur +0x256` · `RealGetSystemMetrics +0x131` (484/484) |
| TEB `+0x78` 0 그대로 | **가드엔 괜찮음** (반환 1 이면 다시 안 읽음) | 진짜는 0 아님 (`xxxCreateThreadInfo +0x756`). 0 으로 두면 85곳에서 syscall 이 계속 나간다 (`GetDpiForSystem` 아래 `IsThreadDesktopComposited` 처럼 메시지 상자 한 번에 여러 번). 0 아닌 값 (스레드 ID 꼴 `[추측]`) 을 두면 조용해진다 |
| 되부름 (`__ClientThreadSetup`) 안 함 | **가드 통과엔 필요 없음** | 다만 되부름이 해 주던 **TEB `+0x820` ≠ 0** 은 메시지 상자 · `GetDesktopWindow` 에 꼭 필요. `KeUserModeCallback` 을 안 만들면 user32 와 같은 0 채운 `0x48` B 를 직접 TEB `+0x820` 에 |
| `psi` 블록 (`csr_user_connect_stub.cpp`) 0 채운 `0x1b68` B | **`DllMain` 까지 맞음, 창 API 길엔 모자람** | `+0x1b56` · `+0x1b66` (word, 0 아님 — 96 = `0x60` 이 자연스러움 `[추측]`) · 단추 글 (`+0x3a4 ~`) · 지표 표 (`+0x768` · `+0x8ec`) · `gSharedInfo+0x18` (모니터 안내판) |
| `GetSystemMetrics(0x1000)` 0 | **가드 뒤에도 맞을 가능성 높음** `[추측]` | 지표 `0x1000` 은 서비스 세션 (`PEB+0x90`) 과 PEB `+0x2c0` 세션이 같으면 0 (`RealGetSystemMetrics +0x233 ~ +0x271`) |

**가드를 통과시키는 최소 조건 (한 목록)**:

1. **win32u `NtUserGetThreadState` (`0x1000`) 인자 `ECX == 0xe` 일 때 `RAX = 1`.** 이것 하나로 넷 모두 가드를 지난다.
2. (같이 하면 좋음) TEB `+0x78` 을 0 아닌 값으로 — syscall 이 덜 나감.

**가드 뒤 첫 벽까지 (메시지 상자가 "뜨려면")**:

3. TEB `+0x820` ≠ 0, 가리키는 곳 `0x48` B 이상 (`+0x0` · `+0x8` · `+0x30` · `+0x40`). TEB `+0x828` = 0 도 됨.
4. `psi+0x1b56` (word) ≠ 0, `psi+0x1b66` 같은 값.
5. `gSharedInfo+0x18` ≠ 0 (읽을 수 있음), 그 `+0x8` 이 주 모니터 물체 (`+0x1c` · `+0x2c` 16 B 씩, `+0x3c` · `+0x3e` word).
6. GDI: `NtUserGetDCEx` 0 아닌 HDC · `NtGdiHfontCreate` 0 아닌 HFONT · 글자 폭 0 아닌 답 — **Q270 (sast/141) 답과 맞출 것.**
7. `NtUserFindExistingCursorIcon` 0 아닌 핸들 (자원 읽기 건너뛰기). `NtUserCreateWindowEx` 가 `ValidateHwnd` 를 통과하는 hwnd (핸들 표 항목 + 창 물체 + 대화상자 자료 — 5절).
8. 고리 끝 입구: `NtUserPeekMessage` 가 `WM_COMMAND(IDOK)` 를 줄지 (→ 1), `WM_QUIT` 을 줄지 (→ 0) — **정책은 사용자 몫.**

- `LoadIconW` 만이면 1 + (`NtUserFindExistingCursorIcon` 0 아님). `GetSystemMetrics` 만이면 1 + `psi` 비트 `0x10` 꺼짐 + 지표 표. `DialogBoxParamW` 는 1 + 3 · 5 · 6 · 7.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 7 — 대기열 **Q273 (동적: 결과서 동적 — TEB +0x78 값·+0x800~+0x8f8·gSharedInfo/psi 덤프·__ClientThreadSetup 진입·메시지 상자 syscall 수·LoadIconW seq 2766 TEB 값 — Q162 와 합침)**. test.exe 는 user32 를 안 올림 — user32 를 올리는 프로세스 (notepad 류 · 새 GUI 샘플, 고르기는 사용자 결정), 그 프로세스 조건):

1. 진짜 GUI 스레드의 TEB `+0x78` (스레드 ID 와 같은가) · `+0x800 ~ +0x8f8` 덤프 — 특히 `+0x810` · `+0x818` · `+0x820` · `+0x828` · `+0x840` · `+0x848` · `+0x8e8`.
2. `gSharedInfo` (user32 rva `0xda350`, `0x240` B) · `psi` 앞 `0x1c00` B 덤프 — 5절 칸 값 (`+0x1b56` · `+0x1b66` · `+0x3a4` · `+0x768` · `+0x8ec` · `+0x138c`).
3. 첫 `NtUserGetThreadState(0xe)` 앞뒤 (`win32u!NtUserGetThreadState`, ret 자리): TEB `+0x78` 이 0 → ≠ 0 으로 바뀌나, 반환 1 인가, 그 사이 `user32!__ClientThreadSetup` 이 서나, 그때 콜스택 (`KeUserModeCallback` ← `win32kfull!xxxClientThreadSetup` ← `win32kbase!xxxCreateThreadInfo+0x1380`).
4. 메시지 상자 한 번의 syscall 순서 (3절 표 대조): `NtUserCreateWindowEx` 몇 번, `NtUserMessageCall` 이 나오나, `NtUserGetDpiForCurrentProcess` 가 처음 한 번만인가.
5. `LoadIcoCur +0x290 ~ +0x2a9` 가 "syscall 없는 0" 인가 — TEB `+0x810` 값. 에뮬레이터 로그 seq 2766 ~ 2768 과 같이.
6. 부모 hwnd 있는 `DialogBoxParamW` 에서 `NtUserGetThreadState(0xe)` 가 둘인가 (`InternalDialogBox +0xaf GetDesktopWindow` + `InternalCreateDialog +0xd39`).
7. 추천 멈춤점: `win32u!NtUserGetThreadState` · `user32!MessageBoxWorker+0x5c` · `user32!SoftModalMessageBox+0x372` · `user32!GetMessageBoxFontForDpi+0xa6` (`r8d`) · `user32!DialogBox2+0x26b` · `user32!EndDialog+0x70`.

정적 (결과서 "다음 요청 후보" N1 ~ N5 — 대기열 **Q274 (정적: 결과서 다음 요청 후보 N1~N5 — InternalCreateDialog 창 만들기·tagWND 칸·xxxCreateThreadInfo TEB+0x820/0x828 채우기·GetSystemMetrics 지표별 칸·LoadIcoCur 자원 길)**, N4 는 Q270 이 받음):

1. **N1** `InternalCreateDialog` 메시지 상자 갈래 전체 (템플릿 `DS_SETFONT` · `CreateDlgFont` · 컨트롤 고리 · `NtUserShowWindow` · `SetForegroundWindow` 조건) + `VerNtUserCreateWindowEx` 클래스 처리 — 3절 19 ~ 27 의 `[공백]`.
2. **N2** 창 물체 사본 (`tagWND`) 칸 표 — `DispatchMessageWorker` · `DefDlgProcW` · `ValidateDialogPwnd` · `EndDialog` · `SendMessageWorker` 가 읽는 오프셋 전부, 핸들 표 항목 모양.
3. **N3** win32kbase `xxxCreateThreadInfo` 본문 (5365 B) — TEB `+0x820` · `+0x828` · ClientInfo 칸을 언제 누가 채우나 (`InitClientInfo` · `xxxResolveDesktop` · `zzzSetDesktop`), `UserThreadCallout` 조건, 데스크톱 없는 프로세스에서의 모양.
4. **N4** GDI DC · 글꼴 · 글자 폭 — `NtUserGetDCEx` HDC 를 gdi32 가 읽는 법 (PEB `+0xf8`), `GdiGetCharDimensions` → **Q270 (sast/141 `gdi-startup`)** 이 받았다.
5. **N5** `GetSystemMetrics` 지표 번호별 읽는 칸 표 (484 샘플 상위 번호와 맞춰), `LoadIcoCur` 자원 길 (`ObjectFromDIBResource` · 커서 만들기 syscall).

- 앞 대기열과의 관계: **Q161 ③ 일부 닫힘** (창 API 넷 · 메시지 상자 길이 읽는 `psi` 칸 — 5절. `_refs/0xda590.tsv` 368줄 전부는 아님), **Q161 ④ 일부** (`ClientThreadSetup` 의 TEB `+0x820` · `+0x910` · `gfSystemInitialized` 몫), **Q163 ② 일부** (GUI 변환 순서 · `__ClientThreadSetup` 되부름 자리. `NtGdiInit2` 반환값은 그대로), **Q163 ④ 일부** (메시지 상자 · 대화상자 · 모달 고리 틀. 창 만들기 안쪽은 Q274 N1 · N2), **Q243 ① 일부** (`ValidateHwnd` 가 읽는 `gSharedInfo` · TEB 칸).
- 그 밖 `[공백]` / `[추측]`: TEB `+0x78` 값이 스레드 ID 인지, `PsConvertToGuiThread` → `W32pThreadCallout` 줄 (커널 안 읽음), 컨트롤 수 · `NtUserMessageCall` 횟수, 대화상자 자료 힙, `psi+0x1b56` 의 자연값 (96 `[추측]`).

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- 결과서 표 (2) 10 의 "`GetDpiDependentMetric` 이 `+0x1b6 · +0x1d7 · +0x255 · +0x3ae · +0x4da` 에서" — 그 자리는 **`GetDpiForSystem` call** 이다. `GetDpiDependentMetric` call 은 바로 뒤 **`+0x1c2 · +0x1e3 · +0x261 · +0x3ba · +0x4e6`** (ret `+0x1c7 · +0x1e8 · +0x266 · +0x3bf · +0x4eb`), 지표 `ECX` = 5 · 6 · `0xc` · `0xd` · 2 는 맞음. 짝마다 `GetDpiForSystem` 이 먼저 불려 그 안 `IsThreadDesktopComposited` 가드도 그만큼 탄다.
- `GetDpiDependentMetric +0x4f / +0x58 CALL` — call 은 **`+0x51`** (ret `+0x58`), `+0x4f` 는 `MOV ECX,EDI`. 칸이 −1 일 때만 (`+0x49 CMP R8D,-1` · `+0x4d JNZ`), syscall 뒤 `+0x64` 에서 다시 읽는다.
- `NtUserEnsureDpiServerInfoForDpi` 는 IAT 직접 호출이 아니라 **`CallWin32uFunction<…NtUserEnsureDpiServerInfoForDpi…>`** (rva `0x7a830`) 껍데기를 거친다 (`GetDpiServerInfoForCurrentThread +0x66`). 이름으로 보아 win32u 내보내기를 이름으로 찾는 것 `[추측: 안 읽음]` — 그 함수가 없으면 syscall 이 안 나갈 수 있다.
- `MB_DlgProc` 메시지 `0x136` 은 헤더 값으로 `WM_CTLCOLORDLG` 다 (`WM_CTLCOLORMSGBOX` 는 `0x132`) `[추측: 일반 지식 — 코드엔 숫자만]`. 그 갈래 (`+0xcc`) 는 `psi+0x1280` 을 돌려줄 뿐이라 뜻은 그대로. `WM_COMMAND` 의 7 (`IDNO`) 도 3 ~ 6 과 같이 곧장 `EndDialog` 로 간다 (`+0x5eb02 SUB ECX,7` · `+0x5eb05 JZ → +0x5ead6`).
- `xxxCreateThreadInfo +0x756` 쓰기는 지역 TEB 값이 0 이 아닐 때만 (`+0x738 ~ +0x740`). `+0x4e GS:[0x30]` 을 "이 스레드의 TEB" 로 읽은 것은 커널 `KPCR +0x30` (`Used_Self`) 일반 지식 `[추측: PDB 타입 대조 안 함]`. TEB `+0x8e8` · `+0x8ec` 쓰기는 `[pti+0x208]` (= TEB `+0x800`) 을 거친 `+0xe8` · `+0xec` (`+0x780 ~ +0x794`).
- TEB `+0x820` 이름 — 결과서 `[추측: 이름]` 을 `pdiLocal` 의 PDB 꾸민 이름 (`tagDESKTOPINFO *`) 으로 받쳤다 (2절).
- `GetDesktopWindow` (rva `0xf890`) 는 내보내기 이름이고, **PDB 공개 이름은 `GetCurrentThreadDesktopHwnd`** 다 (`pdb_aliases.py --where user32.dll` — 같은 주소 이름 2개). 문서는 내보내기 이름을 쓴다.
- `IsThreadDesktopComposited` 는 TEB `+0x820` 이 0 이면 0 을 돌려준다 (`+0x27 TEST / +0x2a JZ`) — 결과서 표 (5) 의 "0 이면 접근 위반" 목록에 안 들어간 것과 맞음. 가드 syscall 이 0 이면 **1** 을 돌려준다 (`+0x51`).
- `GetPrimaryMonitor` 는 모니터 포인터 (`[pDispInfo+8]`) 가 0 이면 0 을 돌려준다 (`+0xd TEST / +0x10 JZ`) — 접근 위반은 그다음 `GetMonitorWorkRect` 쪽 `[추측: 그 함수의 널 검사 안 봄]`.
- 그 밖 결과서 오프셋 · 점프 목적지 · syscall 번호 40개는 `.asm` · `_syscalls.tsv` 와 **일치** (위 "방법" 의 줄 전부).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- user32 `MB_DlgProc` 처럼 Ghidra 가 떨어진 범위 (`0x1800af578 ~`) 를 본체에 붙인 함수는 오프셋이 `+0x5eac8` 처럼 커진다. WinDbg 에선 `user32!MB_DlgProc+0x5eae4` 대신 주소 (`user32+0xaf594`) 로 걸어야 할 수 있다 `[추측]` — 기록만, 하네스 제안 아님.

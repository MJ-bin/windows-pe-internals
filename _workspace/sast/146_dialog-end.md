# 146 dialog-end — 정적 분석 요청

- 날짜: 2026-11-26
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q274a** 의 둘째 몫 (sast/142 가 원문 물음 1 · 2, 이번이 **물음 3**). PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` — **원문 "알고 싶은 것 3" 과 "받을 꼴" 을 먼저 읽을 것**. 사용자 결정 2026-11-26 "다른 것보다 가장 먼저" (원문 "3 은 run 을 나눠도 됨" 에 따라 나눔). **정적만.**
- **먼저 읽는 것**: `_workspace/sast/142_dialog-window_result.md` 전부 — 특히 **11절 "다음 요청 후보 — sast/146 에 넘길 것"** (이번 요청의 읽을 칸 · 함수 목록을 거기서 그대로 받음), 4절 칸 표 (핸들 항목 · TEB · `psi` / `gSharedInfo` · 사본 · `pdlg`), 6절 `SendMessageWorker` 길 A / B · 바로 부르기 조건. 그리고 `_workspace/sast/139_user32-connect_result.md` 4절 (모달 고리 `DialogBox2` · `EndDialog` · `MB_DlgProc` 틀).
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `user32.dll` — **이미 analyze 됨**. 있는 asm (`DialogBox2` · `EndDialog` · `IsDialogMessageW` · `DispatchMessageWorker` · `DefDlgProcW` · `DefDlgProcWorker` · `SendMessageWorker` · `DialogBoxIndirectParamAorW` …) 읽기, 없는 함수만 `^이름$` export (`-readOnly`).
  - `win32kfull.sys` · `win32kbase.sys` — 이번엔 **꼭 필요할 때만** (3 (c) 의 syscall 이 반환으로 무엇을 주는지 user32 쪽에서 안 닫힐 때). **⚠ 지금 sast/145 가 이 두 프로젝트를 쓸 수 있다** — 열기 전에 `_workspace/sast/145_gdi-display-dc_result.md` 가 있는지 보고, 없으면 열지 말고 결과서 "win32k 몫 남음" 절에 목록으로 남긴다.
- 주의: 같은 시간에 다른 decompiler 가 **combase · rpcss** (sast/144) · **gdi32 · gdi32full · gdiplus (+ win32k)** (sast/145) 를 돈다 — 그쪽은 건드리지 않는다. 새 export 전에 `free -g`. scratchpad 임시 파일은 `s146/` 하위에.

## 왜 (요청한 쪽 말 요약)

- 에뮬레이터 정책 (D156) = 보이지 않는 가짜 창: 창은 만들어진 것으로 답하고, 창 프로시저는 진짜로 부르고, **대화상자는 사람이 기본 단추를 바로 누른 것으로 친다**. sast/142 가 창이 서는 길을 닫았으니, 이번엔 **모달 고리가 메시지 하나로 `EndDialog` 까지 가서 빠지는 길** 이 필요함 — stub (`NtUserPeekMessage` · `NtUserDestroyWindow` …) 이 무엇을 돌려줘야 하나.

## 알고 싶은 것 (원문 물음 3 + sast/142 11절)

1. **(원문 3 (a)) 진짜에서 Enter 는 고리에 어떤 메시지로 오나** — 키 메시지 (`WM_KEYDOWN VK_RETURN`) 를 `IsDialogMessageW` 가 `WM_COMMAND` 로 바꾸나, 처음부터 `WM_COMMAND` 인가. 기본 단추 ID 를 어디서 읽나 (`DM_GETDEFID` · `pdlg+0x20` 겸용인가 — 142 11절).
2. **(원문 3 (b)) `WM_COMMAND(ID, BN_CLICKED)` 를 대화상자 hwnd 앞으로 곧장 주면** `IsDialogMessageW` → `TranslateMessage` → `DispatchMessageWorker` → `DefDlgProcW` → 앱 프로시저까지 syscall 없이 가나. 읽는 칸 (`DispatchMessageWorker +0xb6` 사본 `+0x12` 비트 2 · `+0xee` · `[[사본+0x128]+8]`) · 중간 syscall (`NtUserDispatchMessage` · `NtUserTranslateMessage` …) 과 반환 검사. 앱 프로시저가 `EndDialog(hDlg, ID)` 를 부를 때 `EndDialog` 가 쓰는 칸 (`pdlg+0x18` 비트 0 · `+0x20` 결과값) 과 부르는 syscall.
3. **(원문 3 (c)) `EndDialog` 뒤 고리가 빠질 때** — `DialogBox2` 가 `pdlg+0x18` 비트 0 을 보는 자리 · 그 뒤 부르는 syscall (`NtUserSetWindowPos` · `NtUserPostMessage` · `NtUserDestroyWindow` · `NtUserShowWindow` …) 의 반환을 보는지 · 0 이면 어떻게 되나 · `DialogBoxParamW` 가 돌려주는 값이 어디서 오나 (`pdlg+0x20`). `NtUserPeekMessage` / `NtUserGetMessage` 의 반환 · 출력 `MSG` 칸을 고리가 어떻게 쓰나 (0 이면 `WM_ENTERIDLE` 류로 가나 · 끝없는 고리 조건). **고리에서 빠지지 못하고 끝없이 도는 꼴 (금지 꼴)** 을 줄로.
4. **(원문 3 (d)) 갈라지는 자리만** — `DialogBoxIndirectParamA` (ANSI, 15 샘플): `SendMessageWorker +0x14c ~ +0x225` (창 ANSI 비트 `+0x12` 비트 3 vs 보내는 쪽) · `MessageTable[msg] & 0x100` · 확장 스타일 `0x80000000` (`InternalCreateDialog +0x5de`) · `pdlg+0x18` 비트 2. `CreateDialogParam*` (모달 아님, 13 샘플 — 0 반환): 고리 없이 hwnd 를 돌려주는 길이 `DialogBox*` 와 갈리는 줄.
5. **(142 11절) 앱 대화상자 프로시저가 거의 늘 부르는 것** — `GetDlgItem` / `_GetDlgItem` / `_FindDlgItem` (컨트롤 ID = 사본 `+0x140` · 자식 연결 `+0x30` · `+0x38` · `+0x40?` — 확인), `SetWindowLongPtrW(DWLP_USER)` / `GetWindowLongPtrW` (`_SetWindowLongPtr` — 여분 블록 `+0x10` · syscall 인가 사본만인가), `GetParentDialog +0x8` (사본 `+0x12` 비트 0). `WM_GETDLGCODE` 가 앱 프로시저에서 0 으로 돌아오면 가는 `DefWindowProcWorker` 길 (`DlgSetFocus`) — syscall 이 나가나.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| user32 | `DialogBox2` · `EndDialog` · `IsDialogMessageW` · `TranslateMessage` · `DispatchMessageWorker` · `DefDlgProcW` / `DefDlgProcWorker` · `PeekMessageW` / `GetMessageW` 쪽 (있으면) | 1 | 1 ~ 3 |
| user32 | `SendMessageWorker +0x14c ~ +0x225` · `InternalCreateDialog +0x5de` 둘레 · `CreateDialogParamW` / `A` · `DialogBoxIndirectParamAorW` | 0~1 | 4 |
| user32 | `GetDlgItem` · `_GetDlgItem` · `_FindDlgItem` · `SetWindowLongPtrW` · `_SetWindowLongPtr` · `GetWindowLongPtrW` · `_GetWindowLong` · `GetParentDialog` · `DefWindowProcWorker` (`WM_GETDLGCODE` 갈래만) · `DlgSetFocus` | 0~1 | 5 |
| win32kfull | (꼭 필요할 때만 — ⚠) | 0 | 3 |

- 범위: user32 가 읽고 검사하는 것까지. 컨트롤 프로시저 본문 (`ButtonWndProcWorker` …) · 그리기 · 키보드 입력 본문은 이름만.
- 분량이 크면 **2 → 3 → 1 → 5 → 4** 로 결과서에 먼저 쓰고 나머지는 "다음 요청으로" 라고 적어도 된다 (2 · 3 이 stub 을 연다).

## 받고 싶은 것

- 결과서 `_workspace/sast/146_dialog-end_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (1 ~ 5 마다 한 줄).
  3. 표: (1) **"끝 길 지도"** (142 1절 꼴) — `차례 · 부르는 것 / syscall (번호 — win32u _syscalls.tsv) · 조건 · 반환을 user32 가 보나 · 0 이면 · syscall 없이 읽는 / 쓰는 칸 · 줄` — 고리가 메시지 하나 (`WM_COMMAND(IDOK, BN_CLICKED)`) 를 받아 `DialogBoxParamW` 가 돌아올 때까지 · (2) **칸 표 보탬** — 142 4절에 없는 칸만 (`오프셋 · 크기 · 읽는 줄 · 쓰는 줄 · 0 가능 여부`) · (3) 금지 꼴 표 (끝없는 고리 · 접근 위반).
  4. PDB GUID 확인 줄.
- **"지금 우리 (PE_emulator) 맞나·틀리나"** — 원문 "답이 오면 쓰는 곳" 의 새 stub (`NtUserPeekMessage` · `NtUserDestroyWindow` · `NtUserSetDialogPointer` 류) 마다 돌려줄 값 · 쓸 `MSG` 칸 한 줄씩, 그리고 142 9절 **"stub 이 채워야 하는 최소 목록"** 에 이어 붙일 줄들. "기본 단추를 누른 것으로 치기" 를 **어느 syscall 의 출력으로 넣는 게 가장 짧은가** 한 줄.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (대기열 Q273 VM GUI 세션에 얹을 꼴 — 멈춤점 `user32!함수+0x…`) 과 **"다음 요청 후보"**.

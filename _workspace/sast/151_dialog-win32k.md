# 151 dialog-win32k — 정적 분석 요청

- 날짜: 2026-11-27
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 쪽 남은 일)
- 대기열: **Q282** (대기열에 있던 항목). PE_emulator 380 묶음 차례 ④ (`../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 머리 "차례 (우리 D179)" — "④ Q278b · Q282"). 사용자 결정 2026-11-27. **정적만.**
- 먼저 읽는 것: `_workspace/plan.md` 의 **Q282 줄** · `_workspace/sast/146_dialog-end_result.md` 전부 — 특히 **6절 "win32k 몫 남음" 1 ~ 7** · **8절 "다음 요청 후보"** · 5절 "PE_emulator 쪽" · "stub 이 채워야 하는 최소 목록" · `_workspace/sast/142_dialog-window_result.md` (4절 칸 표 · 9절 최소 목록) · `runs/03_sprint/run146-dialog-window.md` · `run148-dialog-end.md`. 그리고 **`_workspace/sast/150_gdi-font-dc_result.md`** (방금 끝난 win32k 쪽 읽기 — 겹치는 함수는 다시 뽑지 말고 그 asm 을 씀).
- 대상 빌드: 26100.9457 x64 `System32/`: `user32.dll` · `win32kfull.sys` · `win32kbase.sys` (모두 프로젝트 있음) · (되면) `kernelbase.dll` `GetUserDefaultLCID` 만 — **⚠ kernelbase 는 sast/152 가 쓰는 중일 수 있음: `_workspace/sast/152_appinfo-runas_result.md` 가 없으면 열지 말고 "다른 프로젝트 몫" 에 남김.**
- 같은 시간에 도는 decompiler: sast/152 (windows.storage · shell32 · appinfo · kernelbase) · sast/153 (ntoskrnl · ntdll) · sast/154 (rpcrt4 · winnsi). **이 요청은 user32 · win32kfull · win32kbase 만.** 새 export 전에 `free -g`. scratchpad 임시 파일은 `s151/` 하위.

## 알고 싶은 것 (Q282 줄 — 급한 것부터)

1. **(급함) 자식 연결 순서 · ANSI 비트** (146 6절 5): `xxxCreateWindowEx` — `LinkWindow` 가 새 자식을 맨 위 / 맨 아래 어디에 끼우나 (사본 `+0x30` · `+0x38` · `+0x48` 순서), 확장 스타일 `0x80000000` 이 사본 `+0x12` 비트 3 · 클라이언트 프로시저 (`DefDlgProcA` 쪽) 로 가는 줄 (`+0x1b9` · `+0x1bf` · `+0x8e6 MapClientNeuterToClientPfn` · `+0xa3a`).
2. **(급함) `NtUserPeekMessage` 진짜 첫 메시지들** (6절 1): `xxxPeekMessage` — 대화상자가 막 선 뒤 첫 부름들에 오는 메시지 차례 (`WM_PAINT` · `WM_NCACTIVATE` · `WM_TIMER` …) 를 정하는 줄 · `wRemove` 처리 · 반환.
3. **(급함) `SoftModalMessageBox` · `MB_AddPushButtons` 기본 단추 ID** — `MB_YESNO` 등에서 기본 단추 스타일 · 순서 · `pdlg+0x20` 에 들어가는 값 (stub 이 늘 `IDOK` 를 내면 틀리는 판 표). `MessageBox` 가 같은 고리 (`DialogBox2`) 를 타나, `MessageBoxWorker` 가 반환을 바꾸나.
4. 비트표 `user32+0xda568 ~ +0xda580` (`gDefWindowMsgs`?) 를 채우는 곳 (6절 4 — `NtUserInitializeClientPfnArrays` · `ClientThreadSetup +0x228`) · 값.
5. `ButtonWndProcWorker` · `xxxButtonNotifyParent` — `WM_GETDLGCODE` 가 `DLGC_DEFPUSHBUTTON` 을 돌려주는 줄 · 클릭이 `WM_COMMAND` 를 보내는 줄.
6. `RealDefWindowProcWorker` `WM_CLOSE` / `SC_CLOSE` 갈래 · `NtUserDestroyWindow` / `xxxDestroyWindow` 되부름 (`WM_DESTROY` · `WM_NCDESTROY`) (6절 2) · `NtUserSetWindowPos(…, 0x97)` 부작용 (6절 3) · `NtUserMessageCall fnid 0x29e` `WM_GETDLGCODE` (6절 6) · `NtUserSetWindowLongPtr` `GWLP_USERDATA` (6절 7).
7. 그 밖 Q282 줄: `GetUserDefaultLCID` (⚠ 위) · `g_systemCallFilterId` 를 정하는 쪽 (150 이 닫았으면 가리키기만) · IMM 훅 `[user32+0xd96f0]` 몸.

- 분량이 크면 **1 → 2 → 3 → 4 → 5 → 6 → 7** 차례로 쓰고 나머지는 "다음 요청으로".

## 받고 싶은 것

- 결과서 `_workspace/sast/151_dialog-win32k_result.md`. 근거 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋`.
  1. 맨 위 **"먼저 읽는 한 줄 답"** (1 ~ 7).
  2. 표: 자식 연결 순서 표 · ANSI 비트 줄 · 첫 메시지 차례 표 · **MessageBox 종류 → 기본 단추 ID 표** · 비트표 값 · 끝 길 syscall 부작용 표.
  3. PDB GUID 확인 줄.
- **"PE_emulator 쪽 — 지금 맞나·틀리나"** (146 5절에 이어): stub 마다 바꿀 값 · "stub 이 채워야 하는 최소 목록" 에 이어 붙일 줄 · "기본 단추를 누른 것으로 치기" 가 MessageBox 종류마다 어떤 ID 여야 하나.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에:
  - **"VM 으로 확인할 것 — 디버거 필요"** (Q273 VM GUI 세션에 얹을 꼴)
  - **"VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것"** (없으면 "없음")
  - **"다른 프로젝트 몫"** · **"다음 요청 후보"**

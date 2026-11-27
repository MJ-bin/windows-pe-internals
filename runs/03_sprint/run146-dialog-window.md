# 2026-11-26 (작성일, 정적 분석 2026-11-26) run146 — 대화상자 창 한 장이 hwnd 로 돌아올 때까지 (Q274a 물음 1 · 2, 정적만): `NtUserGetAtomName(0x8002)` 의 진짜 답은 **반환 6 · 버퍼 `L"#32770"` + 끝 NUL** — 정수 원자 (`0xc000` 아래) 는 원자 표를 안 찾고 `ntoskrnl!RtlQueryAtomInAtomTable +0x298 _snwprintf_s(…, L"#%u", 원자)` 로 짓는다, 커널은 `UNICODE_STRING.Length` 를 안 쓰고 user32 가 `반환 × 2` 로 쓴다. 대화상자 창 `NtUserCreateWindowEx` 는 클래스 `0x8002` 를 그대로 받고, 돌아온 hwnd 는 `ValidateHwnd` → `ValidateDialogPwnd` 가 검사한다. 대화상자 자료 `pdlg` (0x58 B) 는 **user32 가 자기 힙에서 0 으로** 잡고, `NtUserSetDialogPointer` (반환 안 봄) 가 커널 쪽에서 **창 여분 블록 (사본 `+0x128`) 의 `+8`** 에 적는다. 그 여분 블록은 커널이 창 만들기 도중 **사용자 모드 되부름 `0x7b` (`__xxxClientAllocWindowClassExtraBytes`)** 로 user32 힙에서 받아 `xxxCreateWindowEx +0x1a46` 에서 넣는다 — 되부름 가운데 stub 이 대신해야 하는 건 이것 하나. `WM_SETFONT` · `WM_INITDIALOG` 는 핸들 항목 `+0x8` == TEB `+0x78` 등 다섯 조건이 맞으면 syscall 없이 창 프로시저를 바로 부르고, `DefDlgProcWorker` 가 `pdlg+0x0` 의 앱 프로시저를 부른다. `ShowWindow` · `UpdateWindow` · 초점 · 앞으로 내기의 반환은 안 본다

> **고쳐 읽기 (run148, sast/146 — Q274a 둘째 몫)**: PE_emulator 표 "`NtUserDestroyWindow` … 반환을 안 본다 (… `+0xc08`)" 와 최소 목록 6 은 만들기 길 (컨트롤 실패, sast/142 9절 "컨트롤 실패 때만 (N10) 불림") 만 적었다 → **모달 끝 `DialogBox2 +0x3c4` 에서도 늘 불린다** `[정적]` (`pdlg+0x20` 은 그 앞 `+0x3bd` 에서 먼저 읽음). 반환 안 봄 · 기본 대답 0 이면 됨은 그대로. 최소 목록 7 "(sast/146 에서 보탬)" 자리와 "아직 확인 필요" 정적 1 ~ 4 는 run148 로 닫힘 — `NtUserPeekMessage` 출력 한 번 (`WM_COMMAND`, wParam = `LOWORD(pdlg+0x20)`) · `NtUserSetWindowLongPtr` 같은 칸 쓰기 · `NtUserSetDialogPointer` 의 fnid `0x2a4` 는 "필수" 로 강화 (`DefDlgProcWorker +0x32 ~ +0x45` 가 0 · `0x2a4` 아니면 앱 프로시저를 안 부름). 자세히는 `runs/03_sprint/run148-dialog-end.md`.

> **고쳐 읽기 (run157, sast/151 — Q282)**: ① PE_emulator 표 · stub 10 의 자식 연결 "템플릿 차례" 는 `[정적]` 으로 맞다 (`xxxCreateWindowEx` 가 `WS_CHILD` 에 `HWND_BOTTOM` → `LinkWindow` 가 마지막 형제 뒤에) — 이전 형제 칸 `사본+0x50` 도 써야 한다. ② 6절 사본 `+0x12` 비트 3 "= 0 (유니코드)" 은 W 판에서 맞고, A 판 (`0x80000000`) 에서도 클래스가 ANSI 일 때만 켜진다; A 판의 `사본+0x78` 은 `DefDlgProcW` 가 아니라 `DefDlgProcA` (A 표 `gpsi+0x1d8`). ③ "아직 확인 필요" 정적 5 (길 A 일꾼 `psi+0x320` 을 채우는 쪽) → `InitializeClientPfnArrays +0x2b2` 가 셋째 배열 (0x58 B) 을 `gpsi+0x308` 에 쓴다 — `+0x320` 은 그 범위 안 `[정적 두 줄을 이은 것]`. ④ `[공백]` "컨트롤 프로시저 본문" → `BS_DEFPUSHBUTTON` 의 `WM_GETDLGCODE` = `0x2010`, 클릭은 부모에게 `WM_COMMAND` 보내기 (클릭 입구는 아직 `[공백]`). 자세히는 `run157-dialog-win32k.md` (원문은 그대로).

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("`DialogBoxParamW` 가 `InternalCreateDialog` 에서 hwnd 를 돌려줄 때까지 · 창 사본 · 핸들 항목 칸 표", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` 의 **물음 1 · 2**, 튜닝 `357_tune17.md` GC1 · 병목 `dialog_box_fail` 악성 60) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 user32 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run131 · run142 · run144 와 같은 줄). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (ntkrnlmp · user32 · win32kbase · win32kfull 줄 · 근거 줄) · 3장 run142 하위 절 한 줄 · run144 하위 절 뒤 새 하위 절 · 창 클래스 등록 절 (run131) 한 줄 · 4장 (run131 동적 3 · 정적 1 · PE_emulator 줄 표시 · run142 정적 N1 · N2 · PE_emulator 7 표시 · Q274a 일부 닫힘 줄 · run146 줄 셋) · 5장 (run146 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q274a** (Q274 의 N1 · N2 를 앞으로 당긴 것, 사용자 결정 2026-11-26 "다른 것보다 가장 먼저", 정적만). 원문 물음 3 (기본 단추 한 번으로 `EndDialog` 까지) 은 **sast/146 dialog-end** 로 따로 간다 (지금 도는 중) — 그래서 Q274a 는 **물음 1 · 2 몫만 닫힘**. 결과서 판정 **분석성공** — 1 (0)(a)(b)(c)(d)(e)(f) · 2 모두 `[정적]` 줄 근거로 답함.
> - 이어 쓰는 앞 run: **run142** (대화상자 틀 · `InternalCreateDialog` 가드 · 핸들 항목 `+0x0 ~ +0x1a` · TEB `+0x820` / `+0x828` / `+0x840` · "최소 조건" 목록 7 · 8), **run131** (`VerNtUserCreateWindowEx` → `ClassNameToVersion +0x226 NtUserGetAtomName` · 반환 = 글자 수 · 끝 NUL 제외 `[추측]`), **run144** (GDI 공유 핸들 표 — 이번 길의 GDI 부름이 0 을 받아도 되는지만 이어 봄).
> - 앞 run 고쳐 읽기 둘: run142 · run131 맨 위 한 줄 (아래 "앞 run 고쳐 읽기").
> - 남은 동적 → 결과서 "동적으로 확인할 것" 1 ~ 10 (~~대기열 번호는 오케스트레이터가 붙임 — Q273 VM GUI 세션에 얹을 꼴~~ → 대기열 **Q273** — 동적 1 ~ 10 을 출처로 보탬, run147 때 적음), 남은 정적 → sast/146 dialog-end (물음 3) + 결과서 `[공백]` ① ~ ⑥.

## 요약

- **1 (0) 원자 이름** `[정적]`: win32kfull `NtUserGetAtomName` (`+0x6e / +0x74` 글자 한도 = `MaximumLength / 2`, `Length` 칸은 안 씀) → win32kbase `UserGetAtomName` (`+0x2d` 세션 원자 표 · `+0x4a` 부름 · `+0x60 SHR EBX,1` 반환 = 바이트 ÷ 2) → ntoskrnl `RtlQueryAtomInAtomTable` (`+0x6c ~ +0x75` **`0xc000` 미만이면 표를 안 찾고** `+0x246` · `+0x277 ~ +0x298 _snwprintf_s(버퍼 16자, _TRUNCATE, L"#%u", 원자)` · `+0x2c5` **끝 NUL 을 씀 (세지는 않음)** · `+0x2d0` 바이트 수). 서식 글 `0x140afbf20` = `#%u` (`ms/bin` 바이트). 그래서 `0x8002` → **6** · `#32770`. 원자 0 이면 `0xc000000d` → 반환 0.
- **1 (b) 대화상자 창** `[정적]`: `InternalCreateDialog +0x57f MOV R10D,0x8002` · `+0x63c` · `+0x63f VerNtUserCreateWindowEx` — 템플릿에 클래스가 없으면 둘째 인자 `0x8002`, 셋째 = `ClassNameToVersion` 결과 (바꿔치기 없으면 같은 `0x8002`). `hInstance` = 준 값 그대로 (`+0x5f0` → `+0x60b`), 끝 인자 = `dwExpWinVer | 0x40000000` (`+0x5ea`). 돌아온 뒤 `+0x647 / +0x64a` (0) · `+0x653 ValidateHwnd` · `+0x667 ValidateDialogPwnd` — **셋 중 하나라도 0 이면 NULL** (창은 안 부숨).
- **`pdlg` 와 여분 블록 — 누가 어디에** `[정적]`:
  - `pdlg` 0x58 B = **user32** `ValidateDialogPwnd +0x3f AllocLookasideEntry` (자유 목록 또는 `+0x72 RtlAllocateHeap(pUserHeap, 0, 0x58)`, 두 길 다 `+0x51 memset` 0).
  - 붙이기 = **커널** `NtUserSetDialogPointer(hwnd, pdlg)` (`ValidateDialogPwnd +0x4f`, **반환은 버림** `+0x56 → +0x5b JMP → +0xf MOV EAX,1`) → win32kfull `SetDialogPointer +0xa3 unsafe_cast_fnid_zero_to_PDIALOG` (= `[사본+0x128]`) · **`+0xbc MOV [RAX+8],R14`** · `+0xf2` fnid `0x2a4` (0 일 때만) · `+0x10a SetOrClrWF` (사본 `+0x12` 비트 0).
  - 사본 `+0x128` (여분 블록) = **커널** `xxxCreateWindowEx +0x4b2` (0) → `+0x9c9 GetClientExtraBytesTotalSize` ≠ 0 → `+0x1446 xxxClientAllocWindowClassExtraBytes` (`+0x57 LEA ECX,[R8+0x77]` = **`0x7b`** · `+0x5b KeUserModeCallback`) ⇢ user32 `__xxxClientAllocWindowClassExtraBytes` (rva `0x4f770`, `+0x13` 깃발 8 = 0 채움 · `+0x1d RtlAllocateHeap(pUserHeap)` · `+0x3a NtCallbackReturn`) → **`+0x1a46 MOV [사본+0x128],RDI`**.
- **1 (c) 컨트롤 고리** `[정적]`: 컨트롤마다 `+0x975 VerNtUserCreateWindowEx` (원자면 `NtUserGetAtomName` + `NtUserCreateWindowEx`) · (글꼴 있으면) `+0x9a7 SendMessageW(0x30)` · `+0x9ba SendMessageW(0x87)` (반환 `& 0x10` 이면 `pdlg+0x20` = 그 ID). **하나가 NULL 이면 전체 NULL** (`+0x980 → +0xbf4` → `+0xc01 NtUserDestroyWindow` → `+0xd4e`) — 대화상자 스타일 `DS_NOFAILCREATE` (`0x10`) 면 그 컨트롤만 건너뜀 (`+0xbf8 JNZ → +0x9c7`). 서수 클래스는 `psi+0x364 + 2×(번호 & 0xff7f)` 원자 (`+0x84b`), 번호 `≥ 0x19` 면 실패 (`+0x83e`).
- **1 (d) syscall 없이 창 프로시저 부르기** `[정적]` (`SendMessageWorker`): ① 메시지 `0x3e0 ~ 0x3e8` 아님 (`+0x41`) ② **핸들 항목 `+0x8` == TEB `+0x78`** (`+0xbc / +0xbf`) ③ 사본 `+0x12` 비트 2 = 0 (`+0xc5`) ④ `[[TEB+0x820]+0x10] | [TEB+0x838]` 의 `0x2020` = 0 (`+0xf9 ~ +0x110`, **TEB `+0x820` 널 검사 없음**) ⑤ 사본 `+0x80` (클래스) ≠ 0 (`+0x2a4` → 0 이면 `+0x3c5` 접근 위반). 그다음 사본 `+0x78` 이 `psi` 표 칸과 같으면 길 A (일꾼 `psi + fnid×8 − 0x1200`), 아니면 **길 B `+0x600 UserCallWinProcCheckWow([사본+0xe0], [사본+0x78], …)`**. 안 맞으면 `+0x9f4 NtUserMessageCall` (`0x1005`) 또는 `gapfnScSendMessage` 표 (`+0xa21`). `DefDlgProcW +0x31` → `DefDlgProcWorker +0x8c ~ +0x9c` 가 **`[[사본+0x128]+8]+0x0`** 에서 앱 프로시저를 꺼내 `+0xbf UserCallDlgProcCheckWow` 로 부른다.
- **1 (e) 표시 · 끝** `[정적]`: `NtUserShowWindow` (`+0xf7a`) · `UpdateWindow` (`+0xf89`, 사본 `+0x88` · `+0x11` 비트 `0x10` · `+0x38` 이 다 0 이면 syscall 없이 1) · `NtUserSetForegroundWindow` · `DlgSetFocus` (→ `NtUserSetFocus`) **반환 모두 안 봄**. NULL 로 나가는 가지는 15개 (아래 7절), 바깥에서 −1 을 만드는 줄 넷.
- **1 (f) 되부름** `[정적]`: 되부름 없이 hwnd 만 돌려줘도 (a) ~ (e) 는 지나간다 — **단 `0x7b` 이 하던 "사본 `+0x128` 에 0 채운 여분 블록" 은 stub 이 대신** 해야 한다 (없으면 `InternalCreateDialog +0x6ab` 에서 널 읽기). `WM_NCCREATE` · `WM_CREATE` 같은 창 프로시저 되부름이 만드는 상태를 뒤 단계가 읽는 줄은 없다 (대화상자 창).
- **2 칸 표** `[정적]`: 핸들 항목 크기 = `gSharedInfo+0x10`, 번호 = `hwnd & 0xffff`, 고유 수 = `(hwnd >> 16) & 0x7fff` (`0x7fff` 만능). TEB `+0x828` 은 **더하기만** (범위 검사 없음), TEB `+0x820` 은 **첫 qword 와 항목 `+0x10` 이 같은지만**. 사본은 `+0x128` 까지 읽으니 **최소 `0x130` B**. 구조체 형식은 PDB 에 없다 (`tagWND` · `tagDIALOG` 0개) — 칸 이름은 전부 `[추측]`.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백넷째 바퀴. plan **Q274a**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 없음.** user32 · win32kfull · win32kbase · ntoskrnl 모두 `-readOnly` export · refs 만 (결과서).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| user32 | `CDB947D1B96B87977915DE04CF54279C1` | 같음 (run85 · run131 · run142 · run144) |
| win32kfull | `5CD57181BCFDC5AE8F4819BEB1196F091` | 같음 (run136 · run142) |
| win32kbase | `D22DFDA3D54B87E6BD0567A57877904C1` | 같음 (run136 · run142 · run144) |
| ntoskrnl (ntkrnlmp) | `C29EBFB06B78B3C020DCA66D99713F9E1` | 같음 |

- 버전 대조 (결과서): user32 는 "알려진 사실" 표에 줄이 없어 sast/139 · 128 에 적힌 줄과 맞춤 — 전부 일치 (`InternalCreateDialog +0x13c` · `+0x63f/+0x644` · `+0x653` · `+0x667` · `+0x9a7` · `+0x9ba` · `+0xa64` · `+0xf7a` · `+0xf89`, `ValidateDialogPwnd +0x4f`, `ClassNameToVersion +0x226`, `ValidateHwnd` 항목 칸 줄, win32u 번호).
- PDB 에 `tagWND` · `tagDIALOG` · `tagHANDLEENTRY` 형식 없음 (`pdb_struct.py --list` 로 user32 · win32kbase · win32kfull 0개, ntoskrnl 은 `_TEB` 만 — `+0x78 Win32ThreadInfo` · `+0x800 Win32ClientInfo`). 칸 이름은 `[추측: 이름]`, 오프셋 · 읽는 줄은 `[정적]`.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): 악성 60 이 `DialogBoxParamW` · `DialogBoxIndirectParamA` 에서 −1, 부모 hwnd 는 늘 0. 문지기 (run142) 를 푼 갈래 BD01b 에서 다음 벽은 `NtUserGetAtomName(0x8002)` 기본 대답 0 → `ClassNameToVersion` NULL → 커널 창 만들기를 안 부르고 −1. 그 뒤 벽 (`NtUserCreateWindowEx` 0 · `ValidateHwnd` · 사본 없음) 도 이어서 열어야 함. 정책 D156 = **보이지 않는 가짜 창** — 창은 만들어진 것으로 답하고 창 프로시저는 진짜로 부르고 기본 단추를 바로 누른 것으로 친다. 알고 싶은 것은 커널 본문이 아니라 **user32 가 무엇을 읽고 무엇을 검사하나**. 요청서 `_workspace/sast/142_dialog-window.md`:
  1. 창 만들기 길 (N1) — (0) `NtUserGetAtomName(0x8002)` 진짜 값 · (a) 가드 뒤 GDI · 지표 부름과 0 일 때 · (b) 대화상자 창 인자 · 돌아온 뒤 검사 · `NtUserSetDialogPointer` · 누가 `+0x128` 에 적나 · (c) 컨트롤 고리 · (d) syscall 없이 창 프로시저를 부르는 조건 · (e) 표시 · 끝 · NULL 가지 전부 · (f) 되부름이 꼭 필요한가.
  2. 창 사본 · 핸들 항목 칸 표 (N2) — 이 길에서 user32 가 읽는 / 쓰는 칸 전부.
- 쓰는 곳 (원문): `ext/syscall/syscall_layer_cpp/src/gui/nt_user_get_atom_name.cpp` · `nt_user_create_window_ex.cpp` · `src/alpc/csr_user_connect_stub.cpp` · 새 stub (`NtUserSetDialogPointer` · `NtUserShowWindow` 류).

## 방법

- 정적 sast/142 (결과서 `_workspace/sast/142_dialog-window_result.md`, **분석성공**, 스크립트 문제 없음 — `export` · `refs` 모두 `SCRIPT ERROR` 없음):
  - user32 (asm · c, depth 0) 새로 33개: `SkipSz` · `_GetChildControl` · `_NextControl` · `_IsDescendant` · `DlgSetFocus` · `xxxCheckDefPushButton` · `UpdateWindow` · `GetActiveWindow` · `GetNonChildAncestor` · `DefDlgProcWorker` · `DefDlgProcA` · `GetDlgItem` · `GetDlgCtrlID` · `GetParent` · `GetParentDialog` · `GetWindowLongW` · `_GetWindowLong` · `RepositionRect` · `IsWindowGdiScaledX` (Ghidra 이름 `Ordinal_2635`) · `UserCallWinProc` · `UserCallWinProcCheckWow` · `DefWindowProcWorker` · `DefWindowProcW` · `__xxxClientAllocWindowClassExtraBytes` · `SendMessageInternal` · `AllocLookasideEntry` · `InitLookaside` · `FreeLookasideEntry` · `UserCallDlgProcCheckWow` · `GetPrimaryMonitor` · `SetThreadDpiAwarenessContextFromWindow` · `GetDialogMonitor`. refs `0x4f770` (콜백 표 칸 확인).
  - user32 이미 있던 `.asm` 읽기만: `InternalCreateDialog` · `VerNtUserCreateWindowEx` · `ClassNameToVersion` · `ValidateHwnd` · `HMValidateHandleNoSecure` · `ValidateDialogPwnd` · `SendMessageWorker` · `SendMessageW` · `DefDlgProcW` · `DialogBoxParamW` · `DialogBoxIndirectParamAorW` · `InternalDialogBox` · `CreateDlgFont` · `IsWindow`.
  - win32kfull: `NtUserGetAtomName` · `NtUserSetDialogPointer` · `NtUserSetDialogSystemMenu` (+ depth 1 `SetDialogPointer` · `xxxSetDialogSystemMenu` · `ValidateReceivingHwnd` …) · `unsafe_cast_fnid_zero_to_PDIALOG` · `NtUserCreateWindowEx` · `xxxCreateWindowEx` · `SetOrClrWF` · `xxxClientAllocWindowClassExtraBytes` · `GetClientExtraBytesTotalSize`.
  - win32kbase: `UserGetAtomName` · `UserGetAtomNameFromAtomTable` (+ depth 1) · `HMAllocObject` · `HMAllocObjectEx`.
  - ntoskrnl: `RtlQueryAtomInAtomTable` (+ depth 1, 13개 — `RtlpLockAtomTable` 포함). 서식 글 (`0x140afbf20`) 은 `ms/bin` 의 `ntoskrnl.exe` 바이트.
- 같은 시간에 decompiler 가 sast/143 (rpcrt4 · RpcEpMap) · sast/144 (com-rpcss) · sast/145 (gdi-display-dc) · sast/146 (dialog-end) 를 돈다. flow-writer 는 이 바퀴 `.asm` 을 읽기만 했다.
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - ntoskrnl `RtlQueryAtomInAtomTable +0x56 · +0x5b · +0x6c ~ +0x75 · +0x246 ~ +0x2fb` (전부) · `+0x35c`, 서식 글 `0x140afbf20` (`ms/bin` 바이트 `23 00 25 00 75 00 00 00`).
  - win32kbase `UserGetAtomName +0x2d · +0x34 · +0x4a · +0x5a · +0x5c · +0x60 · +0x75`; `HMAllocObjectEx +0x244 · +0x42a · +0x43e`.
  - win32kfull `NtUserGetAtomName +0x0 ~ +0xa3` (전부); `NtUserSetDialogPointer +0x5a`; `SetDialogPointer +0x19 · +0x25 · +0x32 · +0x5e · +0x62 · +0xa3 · +0xbc · +0xe0 · +0xf2 · +0x10a`; `unsafe_cast_fnid_zero_to_PDIALOG +0x19 · +0x77`; `NtUserCreateWindowEx +0x678`; `xxxCreateWindowEx +0x4b2 · +0x510 · +0x577 · +0x59e · +0x9c9 ~ +0x9d0 · +0x1444 ~ +0x144b · +0x147c · +0x148e · +0x1a46`; `xxxClientAllocWindowClassExtraBytes +0x4c · +0x57 · +0x5b`; `GetClientExtraBytesTotalSize +0x11 · +0x2d`.
  - user32 `ClassNameToVersion +0x5a ~ +0x9f · +0x21b ~ +0x244`; `VerNtUserCreateWindowEx +0x108 · +0x177 · +0x17c · +0x1c1 · +0x1e1 · +0x22f · +0x263 · +0x28f`; `InternalCreateDialog` 본체 약 140줄 (`+0x7f` ~ `+0x10cf`, 조각 `FUN_18001fbc1 +0x159 ~ +0x385` 전부, `FUN_1800ab5c3 +0x9c · +0x176`, 떨어진 범위 `+0x8ca16 ~ +0x8ca28`); `ValidateDialogPwnd +0x6 ~ +0x83`; `AllocLookasideEntry +0x0 ~ +0x86` (전부); `__xxxClientAllocWindowClassExtraBytes +0x0 ~ +0x3a`; `SendMessageWorker +0x1c ~ +0x110 · +0x29c ~ +0x40f · +0x546 ~ +0x605 · +0x731 ~ +0xa3d`; `DefDlgProcW +0x17 · +0x31`; `DefDlgProcWorker +0x32 · +0x36 · +0x41 · +0x4e · +0x5b · +0x65 · +0x8c · +0x9c · +0xa3 · +0xbf · +0xcf · +0xdd · +0xf0 · +0x13d · +0x18c ~ +0x193 · +0x1ea · +0x207 · +0x2da · +0x2dd · +0x3e2 · +0x3f0 · +0x404 · +0x986 · +0x98b`; `ValidateHwnd +0x17 ~ +0x146` (22줄); `HMValidateHandleNoSecure +0x72 ~ +0x8e`; `UpdateWindow` (전부); `DlgSetFocus +0x14 · +0x3b`; `_NextControl +0x53 · +0x57 · +0x7b · +0xdd · +0xe6`; `_GetChildControl +0x7 ~ +0x31`; `_IsDescendant +0x7 · +0x17 · +0x24`; `xxxCheckDefPushButton +0xf3`; `IsWindow +0x26 · +0xfe`; `UserCallDlgProcCheckWow +0x3a · +0x53 · +0x193 · +0x1ed`; `GetPrimaryMonitor` (전부); `GetDialogMonitor +0x60 ~ +0x7e`; `SendMessageW +0x17 ~ +0x143`; `SendMessageInternal +0x2e · +0x37 · +0xf9 · +0x11b`; `GetParentDialog +0x8`; `_GetWindowLong +0x50`; `DialogBoxParamW +0x1d ~ +0x53`; `DialogBoxIndirectParamAorW +0x20 ~ +0x56`; `InternalDialogBox +0x2d ~ +0x167`.
  - win32u `_syscalls.tsv` — 결과서가 쓴 번호 20개 (`0x1005` · `0x1037` · `0x104c` · `0x1052` · `0x105b` · `0x106f` · `0x108d` · `0x1095` · `0x109c` · `0x10ca` · `0x1416` · `0x1437` · `0x14d9` · `0x1552` · `0x1553` · `0x155b` · `0x1595` · `0x15c8` · `0x15ce` · `0x1000`) 전부 이름 일치.
  - `pdb_aliases.py --lookup user32.dll 0x20910 · 0x4f770 · 0x13428`, `--lookup win32kfull.sys 0x2897d0` (모두 "별칭 무리 아님"), `--where user32.dll IsWindowGdiScaledX` (rva `0x20910`, `_index.tsv` 이름 `Ordinal_2635`).
  - `../PE_emulator` 의 `nt_user_get_atom_name.cpp` (21줄) · `nt_user_create_window_ex.cpp` (26줄) · `csr_user_connect_stub.cpp` (99줄) 를 **줄 번호만 훑음** (읽기만, 고치지 않음).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **대화상자는 "가게 개업" 이다.** user32 는 개업 신청서 (템플릿) 를 들고 구청 (커널) 에 간다. 가게 종류 칸에 "32770 번 업종" (`0x8002`) 이라고만 적혀 있으면, 구청은 업종 대장을 펴지 않고 **"#32770" 이라고 글자로 바꿔 읽어 준다.**
- 구청은 가게 (창) 를 등록하면서 **빈 사물함 (여분 블록)** 하나를 user32 창고에서 빌려 와 가게에 달아 준다 (되부름 `0x7b`). user32 는 자기 창고에서 **장부 (pdlg)** 를 꺼내고, 구청이 그 장부를 사물함 둘째 칸에 꽂아 준다 (`NtUserSetDialogPointer`).
- 그다음은 user32 혼자 한다 — 장부에 사장님 전화번호 (앱 프로시저) 를 적고, 직원 (컨트롤) 을 한 명씩 등록하고, 사장님께 "개업 준비됐어요" (`WM_INITDIALOG`) 를 **직접 전화** (syscall 없이) 한다. 간판 켜기 (`ShowWindow`) 결과는 확인하지 않는다 `[추측: 비유]`.
- 에뮬레이터는 지금 "32770 번 업종은 모르겠어요" (0) 라고 답해서 신청서가 그 자리에서 반려됐다.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{user32,win32kfull,win32kbase,ntoskrnl}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `user32!DialogBoxParamW` | `+0x2a` · `+0x41` | `+0x31` · `+0x48` | `FindResourceExW(hInst, 5, 이름, 0)` · `LoadResource` — 0 이면 −1 (`+0x39` · `+0x53 JZ`, `RBX = −1` 은 `+0x1d`) | [정적] |
| `user32!DialogBoxIndirectParamAorW` | `+0x20` | `+0x25` | `GetSizeDialogTemplate` — 0 이면 `+0x29 OR RAX,-1` | [정적] |
| `user32!InternalDialogBox` | `+0x70` | `+0x75` | **`InternalCreateDialog`** (`0x1eb90`) — NULL 이면 `+0x78 JZ → +0x151` → (부모 0) 조각 `OR RAX,-1` | [정적] |
| `user32!InternalCreateDialog` | `+0x7f` | `+0x84` | `GetDpiForSystem` | [정적] |
| 〃 | `+0xd39` | `+0xd40` | **`NtUserGetThreadState(0xe)`** (`0x1000`) — 가드 (TEB `+0x78` 0 일 때, run142) | [정적] |
| 〃 | `+0x19e` · `+0x1005` | `+0x1a3` · `+0x100c` | `RtlGetExpWinVer` · (`hInst == 0` 이면) `GetModuleHandleW(0)` | [정적] |
| 〃 | `+0x8ca16` | `+0x8ca1b` | `LoadMenuW` — 템플릿에 메뉴가 있을 때, 0 이면 `+0x8ca28 JZ → +0xd4e` | [정적] |
| 〃 | `+0x316` | `+0x31b` | `GetDialogMonitor(부모, 1)` → (부모 0) `+0x60 NtUserGetForegroundWindow` (`0x1037`) → `GetPrimaryMonitor` | [정적] |
| 〃 | `+0x333` | `+0x33a` | **`CreateCompatibleDC(0)`** — 0 이면 `+0x345 JZ → +0xc90` (NULL) | [정적] |
| 〃 | `+0x34e` | `+0x353` | `IsWindowGdiScaledX` (부모 GDI 확대 — 참이면 조각 `FUN_1800ab5c3` 의 `+0x9c NtUserGetDC` 길) | [정적] |
| 〃 | `+0x386` | `+0x38b` | `CreateDlgFont` — `DS_SETFONT` (`+0x35d TEST DIL,0x40`) | [정적] |
| 〃 | 조각 `FUN_18001fbc1 +0x17a` (= `+0x11ab`) | 조각 `+0x181` | `GetStockObject(0x10)` — `DS_FIXEDSYS` · 새 앱 | [정적] |
| 〃 | `+0x3e9` | `+0x3f0` | `GetStockObject(0xd)` — 글꼴 0 일 때 대체 | [정적] |
| 〃 | `+0x3b0` · `+0x3bb` | `+0x3b5` · `+0x3c0` | `GetDpiServerInfoForCurrentThread` (`+0x20` · `+0x24` 기본 글자 단위) | [정적] |
| 〃 | `+0x409` · `+0x467` · `+0x533` | `+0x410` · `+0x46c` · `+0x538` | `DeleteDC` · `_AdjustWindowRectExForDpi` · `RepositionRect` — 반환 안 봄 | [정적] |
| 〃 | 조각 `+0x220` (= `+0x1251`) | 조각 `+0x225` | `_MonitorFromRect` — 0 이면 조각 `+0x22b JZ → +0xc90` | [정적] |
| 〃 | **`+0x63f`** | **`+0x644`** | **`VerNtUserCreateWindowEx`** (대화상자, `+0x63c MOV RDX,R10` = `0x8002`) | [정적] |
| 〃 | **`+0x653`** | **`+0x658`** | **`ValidateHwnd`** — 0 이면 `+0x65e JZ → +0xc90` | [정적] |
| 〃 | **`+0x667`** | **`+0x66c`** | **`ValidateDialogPwnd`** — 0 이면 `+0x66e JZ → +0xc90` | [정적] |
| 〃 | 조각 `+0x292` (= `+0x12c3`) | 조각 `+0x299` | `NtUserSetWindowContextHelpId` (`0x1595`) — EX 템플릿 도움말 번호 ≠ 0 | [정적] |
| 〃 | 조각 `+0x2ae` (= `+0x12df`) · 조각 `FUN_1800ab5c3 +0x176` (= `+0x8cba9`) | 조각 `+0x2b5` · `+0x17d` | `NtUserSetDialogSystemMenu` (`0x1553`) / `NtUserGetSystemMenu` (`0x105b`) — `WS_SYSMENU` (사본 `+0x1e` 비트 3) | [정적] |
| 〃 | 조각 `+0x2f0` (= `+0x1321`) | 조각 `+0x2f5` | `SendMessageWorker(사본, WM_SETFONT 0x30, hfont, 0, 0)` — 글꼴 있고 앱 프로시저 있을 때 | [정적] |
| 〃 | **`+0x975`** | **`+0x97a`** | **`VerNtUserCreateWindowEx`** (컨트롤마다) — 0 이면 `+0x980 JZ → +0xbf4` | [정적] |
| 〃 | `+0x9a7` · `+0x9ba` | `+0x9ac` · `+0x9bf` | `SendMessageW(ctl, 0x30)` (글꼴 있을 때) · `SendMessageW(ctl, 0x87)` (늘) | [정적] |
| 〃 | 조각 `+0x2ff` (= `+0x1330`) | 조각 `+0x306` | `NtUserSetWindowContextHelpId` (컨트롤 도움말 번호 ≠ 0) | [정적] |
| 〃 | `+0xc01` | `+0xc08` | `NtUserDestroyWindow` (`0x1095`) — 컨트롤 실패 · `DS_NOFAILCREATE` 없음 | [정적] |
| 〃 | **`+0xa64`** | **`+0xa69`** | **`SendMessageWorker(사본, WM_INITDIALOG 0x110, hwndFocus, lParam, 0)`** — 앱 프로시저 ≠ 0 (`+0xa40`) | [정적] |
| 〃 | `+0xa6f` · `+0xba8` | `+0xa74` · `+0xbad` | `HMValidateHandleNoSecure(hwnd)` — 0 이면 NULL | [정적] |
| 〃 | `+0xaaf` · `+0xac9` · `+0xb21` · `+0xb2e` | `+0xab4` · `+0xace` · `+0xb26` · `+0xb33` | `_GetChildControl` · `_NextControl` · `DlgSetFocus` · `xxxCheckDefPushButton` — 기본 초점 (반환 안 봄) | [정적] |
| 〃 | `+0xb36` | `+0xb3b` | `IsWindow(hwnd)` — 0 이고 TEB `+0x810 ≥ 0x400` 이면 NULL | [정적] |
| 〃 | 조각 `+0x359` (= `+0x138a`) | 조각 `+0x35e` | `SendMessageWorker(사본, 0x127, …)` — `psi` 비트 조건이 다 맞을 때만 (`+0xb43 ~ +0xb7d`, 아래 "flow-writer 대조" 8) | [정적] |
| 〃 | 조각 `+0x366` · `+0x375` (= `+0x1397` · `+0x13a6`) | 조각 `+0x36d` · `+0x37a` | `NtUserSetForegroundWindow` (`0x155b`) · `IsWindow` — `DS_SETFOREGROUND` | [정적] |
| 〃 | **`+0xf7a`** · **`+0xf89`** | `+0xf81` · `+0xf8e` | **`NtUserShowWindow(hwnd, 1)`** (`0x1052`) · **`UpdateWindow`** — 반환 안 봄 | [정적] |
| `user32!VerNtUserCreateWindowEx` | `+0x177` | `+0x17c` | `ClassNameToVersion` — 끝 인자에 `0x40000000` (`+0x108 AND R12D,0x40000000`) | [정적] |
| 〃 | **`+0x22f`** | **`+0x236`** | **`NtUserCreateWindowEx`** (`0x106f`) — 둘째 `+0x1e1 MOV RDX,RDI`, 셋째 `+0x1c1 MOV R8,R14` | [정적] |
| 〃 | `+0x263` | `+0x26a` | `GetLastError` — `0x57f` 일 때만 `RegisterDefaultClass` 로 한 번 더 (`+0x28f`) | [정적] |
| `user32!ClassNameToVersion` | `+0x74` | `+0x7b` | `RtlAllocateHeap(pUserHeap, 0, 0x200)` — 이름 버퍼 | [정적] |
| 〃 | **`+0x226`** | **`+0x22d`** | **`NtUserGetAtomName(원자, USTR)`** (`0x109c`) — `+0x232 ADD AX,AX` · `+0x235` `Length` 를 user32 가 씀 | [정적] |
| win32kfull `NtUserGetAtomName` (rva `0x1d55b0`) | `+0x5c` | `+0x63` | `ProbeForWrite(Buffer, MaximumLength, 2)` | [정적] |
| 〃 | **`+0x7d`** | **`+0x84`** | **win32kbase `UserGetAtomName(원자, Buffer, MaximumLength/2)`** | [정적] |
| win32kbase `UserGetAtomName` | **`+0x4a`** | **`+0x51`** | **ntoskrnl `RtlQueryAtomInAtomTable(세션 표, 원자, 0, 0, Buffer, &바이트)`** | [정적] |
| ntoskrnl `RtlQueryAtomInAtomTable` (rva `0x9b2b70`) | `+0x56` | `+0x5b` | `RtlpLockAtomTable` — 실패면 `+0x35c` `0xc000000d` | [정적] |
| 〃 | **`+0x298`** | **`+0x29d`** | **`_snwprintf_s(지역 16자, 16, _TRUNCATE, L"#%u", 원자)`** — 원자 `< 0xc000` 갈래 | [정적] |
| 〃 | `+0x2bd` | `+0x2c2` | 바이트 복사 (같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`) → `+0x2c5` NUL | [정적] |
| `user32!ValidateDialogPwnd` (rva `0x13428`) | `+0x3f` | `+0x44` | `AllocLookasideEntry(&DialogLookaside)` — 0 이면 0 | [정적] |
| 〃 | **`+0x4f`** | **`+0x56`** | **`NtUserSetDialogPointer(hwnd, pdlg)`** (`0x1552`) — 반환 버림 | [정적] |
| 〃 | `+0x6d` | `+0x72` | `InitLookaside` — 처음 한 번 (`0x58 B × 2`) | [정적] |
| `user32!AllocLookasideEntry` | `+0x72` · `+0x51` | `+0x79` · `+0x56` | `RtlAllocateHeap(pUserHeap, 0, 0x58)` (자유 목록이 비었을 때) · `memset(…, 0, 0x58)` (두 길 다) | [정적] |
| win32kfull `NtUserSetDialogPointer` | `+0x5a` | `+0x5f` | `SetDialogPointer` | [정적] |
| win32kfull `SetDialogPointer` | `+0xa3` | `+0xa8` | `unsafe_cast_fnid_zero_to_PDIALOG` — fnid 0 / `0x2a4` 면 `[사본+0x128]` (`+0x77`) | [정적] |
| 〃 | `+0x10a` | `+0x10f` | `SetOrClrWF(1, 창, 0x201, 1)` — 사본 `+0x12` 비트 0 | [정적] |
| win32kfull `NtUserCreateWindowEx` | `+0x678` | `+0x67d` | `xxxCreateWindowEx` | [정적] |
| win32kfull `xxxCreateWindowEx` | `+0x9c9` | `+0x9ce` | `GetClientExtraBytesTotalSize` (= 사본 `+0xc8`, 기능 켜지면 `+0x150` 더함) — 0 아니면 `+0x9d0 JNZ → +0x1444` | [정적] |
| 〃 | **`+0x1446`** | **`+0x144b`** | **`xxxClientAllocWindowClassExtraBytes(크기)`** → `+0x1a46 MOV [사본+0x128],RDI` | [정적] |
| win32kfull `xxxClientAllocWindowClassExtraBytes` | **`+0x5b`** | **`+0x62`** | **`KeUserModeCallback(0x7b, 입력 4 B)`** ⇢ user32 `__xxxClientAllocWindowClassExtraBytes` | [정적] |
| `user32!__xxxClientAllocWindowClassExtraBytes` (rva `0x4f770`) | `+0x1d` · `+0x3a` | `+0x24` · `+0x41` | `RtlAllocateHeap(pUserHeap, 8 (0 채움), [입력])` · `NtCallbackReturn` | [정적] |
| `user32!SendMessageWorker` | `+0x600` | `+0x605` | **`UserCallWinProcCheckWow([사본+0xe0], 프로시저, hwnd, msg, w, l, 0, 1)`** — 길 B | [정적] |
| 〃 | `+0x806` · `+0x956` | `+0x80b` · `+0x95b` | ⇢ 일꾼 (CFG 간접, `psi + fnid×8 − 0x1200`) — 길 A | [정적] |
| 〃 | `+0x9f4` · `+0xa21` | `+0x9fb` · `+0xa26` | `NtUserMessageCall` (`0x1005`, `msg ≥ 0x400`) · ⇢ `gapfnScSendMessage[MessageTable[msg] & 0xff]` — syscall 길 | [정적] |
| `user32!DefDlgProcW` | `+0x17` · `+0x31` | `+0x1c` · `+0x36` | `ValidateHwnd` · `DefDlgProcWorker(사본, msg, w, l, 0)` | [정적] |
| `user32!DefDlgProcWorker` | `+0x404` | `+0x40b` | `NtUserSetWindowFNID(hwnd, 0x2a4)` (`0x108d`) — fnid 0 일 때 (`+0x32 / +0x36`) | [정적] |
| 〃 | `+0x4e` | `+0x53` | `ValidateDialogPwnd` | [정적] |
| 〃 | **`+0xbf`** | **`+0xc4`** | **`UserCallDlgProcCheckWow([사본+0xe0], [pdlg+0x0], hDlg, msg, w, l, 0, &결과)`** | [정적] |
| `user32!UserCallDlgProcCheckWow` | `+0x193` · `+0x1ed` | `+0x198` · `+0x1f2` | ⇢ 앱 프로시저 (CFG 간접) — syscall 없음 | [정적] |
| `user32!DlgSetFocus` | `+0x14` · `+0x3b` | `+0x19` · (꼬리) | `SendMessageW(0x87)` · `JMP [NtUserSetFocus]` (`0x104c`) | [정적] |
| `user32!UpdateWindow` | `+0x9` · `+0x3e` | `+0xe` · (꼬리) | `ValidateHwnd` · `JMP [NtUserUpdateWindow]` (`0x15c8`) — 사본 `+0x88` · `+0x11` 비트 `0x10` · `+0x38` 중 하나라도 0 아닐 때만 | [정적] |

### 1. (0) 원자 이름 — `NtUserGetAtomName(0x8002, USTR)` [정적]

| 층 | 한 일 | 줄 |
|---|---|---|
| user32 `ClassNameToVersion` | 원자일 때만 (`+0x98 TEST RBX,-0x10000` · `+0x9f JZ`). 버퍼 0x200 B (`+0x6a` · `+0x74 RtlAllocateHeap(pUserHeap, 0, 0x200)` — 0 채움 아님), `MaximumLength = 0x200` (`+0x21b`), `Buffer` (`+0x222`) | `+0x226 / +0x22d` |
| win32kfull `NtUserGetAtomName` | 포인터를 `MmUserProbeAddress` 로 자름 (`+0x28 ~ +0x35`) · `+0x3a` 첫 dword (`Length \| MaximumLength << 16`) · `+0x44` `Buffer` · `+0x5c ProbeForWrite` · **`+0x6e / +0x74` 글자 한도 = `MaximumLength >> 1`** · `+0x89` 반환 그대로. **`Length` 를 쓰는 줄이 없다** | `+0x7d / +0x84` |
| win32kbase `UserGetAtomName` | `+0x2d` 세션 원자 표 (`[세션 상태 + 0xa658]`) · `+0x34` 바이트 한도 = 글자 × 2 · `+0x5a JS` (실패 → `+0x75 SetLastNtError`, 0) · **`+0x5c / +0x60` 반환 = 바이트 ÷ 2** | `+0x4a / +0x51` |
| ntoskrnl `RtlQueryAtomInAtomTable` | `+0x6c MOV EAX,0xc000` · `+0x71 CMP R15W,AX` · **`+0x75 JC → +0x246` (정수 원자 — 표 안 찾음)** · `+0x246 / +0x24a` 원자 0 → `+0x2d4` `0xc000000d` · `+0x250 / +0x253` 성공 · `+0x25d` · `+0x268` (참조 수 · 고정 수 칸이 있으면 1) · `+0x277 ~ +0x298` `#%u` (지역 16자) · `+0x29d / +0x29f` 바이트 = 글자 × 2 · `+0x2a5 CMP ESI,EDI` · `+0x2a7 JNC → +0x2ef` (한도 부족: `+0x2ef CMP EDI,2` · `+0x2f2 JC → +0x2e5` `0xc0000023`, 아니면 `+0x2f4` 바이트 = 한도 − 2 로 잘라 성공) · `+0x2bd` 복사 · **`+0x2c2 / +0x2c5` 끝 NUL 씀** · `+0x2d0` 바이트 수 돌려줌 | `+0x298 / +0x29d` |

- 서식 글 `0x140afbf20` = `23 00 25 00 75 00 00 00` = **`L"#%u"`** (flow-writer 가 `ms/bin/…/ntoskrnl.exe` 구역표로 다시 읽음).
- 그래서 `0x8002` (= 32770) → **반환 6**, 버퍼 `23 00 33 00 32 00 37 00 37 00 30 00 00 00` (`#32770` + NUL, 14 B). user32 는 512 B 를 주므로 자르기 없음.
- 끝 NUL 은 **쓰지만 세지 않는다**: 바이트 = 글자 × 2 (`+0x29f ADD ESI,ESI`) 를 복사하고 그 뒤 칸에 0 (`+0x2c5`). 그래서 한도는 "글자 수 + 1" 이상이어야 전체를 쓴다.
- user32 는 끝 NUL 을 안 읽는다 — `Length = 반환 × 2` 로 `RtlFindActivationContextSectionString` 에 넘김 (run131: 매니페스트 바꿔치기 없으면 NOT_FOUND → 원자 그대로).
- 글 원자 (`0xc000` 이상) 의 진짜 이름은 그대로 동적 (Q242, run131 동적 3).

### 2. (b) 대화상자 창 — 인자 · 돌아온 뒤 검사 · `pdlg` [정적]

**인자** (`InternalCreateDialog +0x5de ~ +0x63f`, `VerNtUserCreateWindowEx +0x1c1 ~ +0x22f`):

| 인자 | 값 | 줄 |
|---|---|---|
| 확장 스타일 | `(param_7 & 2 ? 0x80000000 : 0) \| 템플릿 확장 스타일` | `+0x5de AND R14D,0x2` · `+0x5ff OR ECX,ESI` |
| 클래스 (둘째) | **`0x8002`** — 템플릿 클래스가 빈 글일 때 (`+0x575 CMP word [R10],R13W`). 이름이 있으면 `LARGE_STRING` (조각 `+0x249 ~ +0x25d RtlInitLargeUnicodeString`) | `+0x57f MOV R10D,0x8002` · `+0x63c MOV RDX,R10` |
| 버전 클래스 (셋째) | `ClassNameToVersion` 결과 — 바꿔치기 없으면 같은 `0x8002` | `VerNtUserCreateWindowEx +0x17c` · `+0x1c1` |
| 창 이름 | `LARGE_STRING` (글자 수 × 2) | `+0x5a9 ~ +0x5b0` |
| 스타일 | 템플릿 스타일에서 `WS_VISIBLE` (`0x10000000`) 를 뗌 (보이기는 나중에 `NtUserShowWindow`), 새 앱이면 `DS_3DLOOK (4)` 더함 | `+0x415 BT EDI,0x1c` → 조각 `+0x1db ~ +0x1e7` (`OR R12D,1` · `BTR EDI,0x1c`) · `+0x1e1 OR EDI,ECX` |
| `x y cx cy` | 템플릿 단위 × 글자 단위 → 픽셀 (`0x8000` = 기본 위치) | 결과서 3-1 |
| 부모 · 메뉴 | `[RBP-0x10]` (우리 표본 0) · `[RBP+0x20]` | 결과서 3-1 |
| `hInstance` | **호출자가 준 값 그대로** (0 이면 `GetModuleHandleW(0)` 는 `RtlGetExpWinVer` 에만 쓰고 넘기는 값은 0) | `+0x5f0 MOV RAX,[RBP+0x30]` · `+0x60b MOV [RSP+0x50],RAX` |
| `lpParam` | 0 | `+0x606 MOV [RSP+0x58],R13` |
| 끝 인자 | `dwExpWinVer \| 0x40000000` — 이 비트가 `ClassNameToVersion` 길을 연다 | `+0x5ea` · `VerNtUserCreateWindowEx +0x108` |

- `VerNtUserCreateWindowEx` 는 커널 0 이면 `GetLastError() == 0x57f` 일 때만 `RegisterDefaultClass` 로 한 번 더 (`+0x263 ~ +0x28f`). **stub 이 0 + 오류 없음이면 그대로 NULL.**

**돌아온 뒤 검사**: `+0x647 TEST RAX` · `+0x64a JZ → +0xc90` → `+0x653 ValidateHwnd` → `+0x65e JZ → +0xc90` → `+0x667 ValidateDialogPwnd` → `+0x66e JZ → +0xc90`. `+0xc90` 은 메뉴 파괴 · 글꼴 지움 뒤 NULL — **창은 안 부순다**.

`ValidateDialogPwnd` (rva `0x13428`) 가 0 이 되는 때:

| 조건 | 줄 |
|---|---|
| 사본 `+0x12` 비트 0 이 켜져 있으면 **아무것도 안 하고 1** (`pdlg` 를 안 만듦) | `+0x6 TEST [RCX+0x12],1` → `+0xf MOV EAX,1` |
| 사본 `+0xc8` (여분 바이트 크기) `< 0x1e` → `SetLastError(0x58c)` · 0 | `+0x1b CMP [RCX+0xc8],0x1e` · `+0x22 JL → +0x83` |
| 사본 `+0x2a` (fnid) `& 0xc000` ≠ 0 → 0 | `+0x29 TEST word [RCX+0x2a],AX` |
| `AllocLookasideEntry` 0 → 0 | `+0x3f / +0x44` |
| `InitLookaside` 실패 (처음 한 번) | `+0x6d ~ +0x74` |

**`pdlg` (0x58 B) — 누가 잡고 · 누가 적나**:

- 잡기 = **user32** `AllocLookasideEntry` — 자유 목록 (`+0x20 ~ +0x2c`) 이나 `+0x72 RtlAllocateHeap(pUserHeap, 0, 0x58)` → 두 길 다 `+0x51 memset(…, 0, [목록+0x28])` → **0 으로 시작**.
- 붙이기 = **커널** `NtUserSetDialogPointer(hwnd, pdlg)` → `SetDialogPointer`: `+0x19 LEA RSI,[RCX+0x28]` (커널 창 물체 `+0x28` = 사용자 쪽 사본 포인터) · `+0x25` `+0xc8 ≥ 0x1e` · `+0x32 TEST [사본+0x12],4` (켜져 있으면 아무것도 안 함) · `+0x5e / +0x62` 같은 프로세스 · `+0xa3 unsafe_cast_fnid_zero_to_PDIALOG` (fnid 0 또는 `0x2a4` 면 `[사본+0x128]`, `+0x77`) · **`+0xbc MOV [RAX+8],R14` = 여분 블록 `+8` 에 `pdlg`** · `+0xf2 MOV word [사본+0x2a],0x2a4` (fnid 0 일 때만) · `+0x10a SetOrClrWF(…, 0x201, 1)` = 사본 `+0x12` 비트 0. (`pdlg` 가 0 이면 `+0xe0 OR word [사본+0x2a],0x4000`.)
- **user32 는 `NtUserSetDialogPointer` 의 반환을 안 본다** (`+0x56 NOP` → `+0x5b JMP → +0xf MOV EAX,1`). 보는 건 **부수 효과** — 여분 블록 `+8` 에 `pdlg` 가 들어갔는지를 `InternalCreateDialog +0x6ab ~ +0x6af` 가 바로 쓰면서 확인하는 꼴이다 (아래 대조 7).
- 사본 `+0x128` (여분 블록) = **커널**: `xxxCreateWindowEx +0x4b2` 가 0 으로 둔 뒤 `+0x9c9 GetClientExtraBytesTotalSize` (`+0x11` = 사본 `+0xc8`, 기능 켜지면 `+0x2d` 에서 `+0x150` 더함) 가 0 이 아니면 `+0x1446 xxxClientAllocWindowClassExtraBytes` → **`KeUserModeCallback(0x7b, 4 B 입력)`** (`+0x4c MOV R8D,4` · `+0x57 LEA ECX,[R8+0x77]` · `+0x5b`) → `+0x1a46 MOV [사본+0x128],RDI`. 이미 0 이 아니면 `+0x147c ~ +0x148e` `UserSetLastError` 로 실패.
- user32 받는 쪽 = `__xxxClientAllocWindowClassExtraBytes` (rva `0x4f770`): `+0x10 MOV R8D,[RCX]` (크기) · `+0x13 LEA EDX,[RAX+8]` (`RAX` = 0 → 깃발 8 = 0 채움) · `+0x1d RtlAllocateHeap(pUserHeap)` · `+0x3a NtCallbackReturn`. 콜백 표 번호 확인 (결과서): `_refs/0x4f770.tsv` 의 칸 `0xb4af8` 이 `__ClientThreadSetup` 칸 `0xb49c0` (번호 `0x54`, run142) 보다 `0x138 / 8 = 39` 칸 뒤 → `0x7b` ✓.
- 여분 블록 모양 `[추측: DLGWINDOWEXTRA]`: `+0x0` qword (`DWLP_MSGRESULT` — `DefDlgProcWorker +0x5b ~ +0x65` 가 0 이 아니면 `+0x207 _SetWindowLongPtr(…,0,0)`) · **`+0x8` = `pdlg`** · `+0x10` (`DWLP_USER`). 모두 0 으로 시작.

**`pdlg` 칸** (`InternalCreateDialog +0x69a ~ +0x72c` 가 칸마다 `[사본+0x128]` → `[+8]` 을 다시 읽어 씀):

| 칸 | 크기 | 쓰는 줄 | 읽는 줄 | 뜻 `[추측: 이름]` |
|---|---|---|---|---|
| `+0x00` | 8 | `+0x6dd MOV [RCX],R13` (`R13` = `+0x6a4 [RBP+0x100]` 앱 프로시저) | `+0xa40` · `DefDlgProcWorker +0x9c` · `SendMessageWorker +0x78b` | 앱 `DLGPROC` |
| `+0x08` | 4 | `+0x6be` | (이 길에선 안 읽음) | 글자 단위 가로 |
| `+0x0c` | 4 | `+0x6cf` | 〃 | 글자 단위 세로 |
| `+0x18` | 4 | `+0x6af AND …,~2` · `+0x6eb AND …,~1` · `+0x72c OR …,4` (ANSI) · 조각 `+0x276 OR …,8` (DPI) | `+0xa93` · `+0xf5e` · `DefDlgProcWorker` · `DialogBox2` | 깃발 — 비트 0 = 끝남 (`EndDialog`) · 비트 2 = ANSI · 비트 3 = DPI |
| `+0x20` | 8 | `+0x6fa MOV qword [RCX+0x20],1` · 조각 `+0x31e` (`WM_GETDLGCODE` 가 `DLGC_DEFPUSHBUTTON` 이면 그 ID) | `DefDlgProcWorker` · `EndDialog` · `DialogBox2` | **기본 단추 ID 겸 결과값** (처음 1 = IDOK) — `EndDialog` 쪽은 sast/146 |
| `+0x30` | 8 | 조각 `+0x2ce` (= `+0x12ff`) | `DefDlgProcWorker +0x31a ~` (`DeleteObject`) | `HFONT` |
| `+0x3c` | 4 | 조각 `+0x287` (= `+0x12b8`) | — | DPI (켜졌을 때만) |
| `+0x40` | 8 | (안 씀 — 0) | `DefDlgProcWorker` (0 아니면 `DeleteObject`) | 또 하나의 GDI 개체 `[추측]` |
| `+0x50` | 4 | `+0x710` (`[RBP-0x34]`, `CreateDlgFont` 출력) | — | `[추측: 글꼴 크기 (pt)]` |

### 3. (c) 컨트롤 고리 (`InternalCreateDialog +0x2de ~ +0xa23`, 몸통 `+0x975 ~ +0x9f4`) [정적]

- 항목 꼴: 일반 `DLGITEMTEMPLATE` (스타일 `+0` · 확장 `+4` · `x y cx cy` `+8 ~ +0xe` · ID `+0x10` word · 클래스 `+0x12`) / EX (도움말 `+0` · 확장 `+4` · 스타일 `+8` · 위치 `+0xc ~ +0x12` · ID `+0x14` dword · 클래스 `+0x18`) (`+0x764` · `+0x76f ~ +0x7b9` · `+0xa03 ~ +0xa23`). 반복 = `cdit` (`+0x74e ~ +0x75e`).
- 클래스: `0xffff` 로 시작하면 서수 → `(서수 & 0xff7f) < 0x19` 면 **원자 = `psi+0x364 + 2×번호`** (`+0x824 ~ +0x84b`), 아니면 실패 (`+0x83e JNC → +0xbf4`). 글이면 `SkipSz` + `RtlInitLargeUnicodeString`. 콤보 원자 (`psi+0x36e`, `+0x871`) 이거나 스타일 비트 23 이 켜졌고 대화상자가 `DS_3DLOOK` (`+0x860`) 이면 `WS_EX_CLIENTEDGE (0x200)` 더하고 비트 23 뗌 (`+0x87c` · `+0xc12`). 모든 컨트롤 확장 스타일에 `0x4` (`+0x857 OR R8D,R14D`). ID ≠ 0 이면 스타일 `(& 0x3fffffff) | 0x40000000` (`+0x8e1` · `+0x8e8 BTS R9D,0x1e`).
- 창 이름: 서수면 4 B 이진, 아니면 글 (`+0x8b6 ~ +0x8cb`). 생성 데이터 바이트 ≠ 0 이면 그 칸 주소를 `lpParam` 으로 (`+0x8ce ~ +0x8d7` → `+0x949`). **`hMenu` 인자 = 컨트롤 ID** (`+0x904 MOVSXD R14,R13D` → `+0x957`).
- 컨트롤 하나당 syscall: (원자일 때) `NtUserGetAtomName` → `NtUserCreateWindowEx`. 반환 0 → `+0x980 JZ → +0xbf4 TEST [사본+0x1c],0x10` (`DS_NOFAILCREATE`) → 없으면 `+0xc01 NtUserDestroyWindow(대화상자)` → `+0xc0d JMP → +0xd4e` → **NULL**; 있으면 `+0xbf8 JNZ → +0x9c7` (다음 컨트롤).
- 이어서: 도움말 번호 ≠ 0 → 조각 `+0x2ff NtUserSetWindowContextHelpId` · 글꼴 ≠ 0 → `+0x9a7 SendMessageW(ctl, 0x30, hfont, 0)` · **늘 `+0x9ba SendMessageW(ctl, 0x87)`** → 반환 `& 0x10` (`+0x9bf TEST AL,0x10`) 이면 조각 `+0x310 ~ +0x31e` `pdlg+0x20 = ID`.
- `SendMessageW` → `SendMessageInternal` → 사본이 안 구해지면 **조용히 0** (`SendMessageInternal +0x11b XOR EAX,EAX`, 오류 없음). 컨트롤 hwnd 에 항목 · 사본이 없으면 이 두 호출은 0 을 돌려주고 고리는 안 막힌다.
- 고리 끝 기본 초점: `+0xaaf _GetChildControl(대화상자, NULL)` 은 늘 NULL (`_GetChildControl +0x7 TEST RDX`) → `+0xac9 _NextControl(대화상자, NULL, 3)` 이 첫 자식 (사본 `+0x38`, `_NextControl +0xdd`) 부터 "보임 + 탭 정지 + 비활성 아님" (`+0xae0 ~ +0xaf3` `& 0x18010000 == 0x10010000`) 을 찾음. **자식이 없으면 (`+0x38 == 0`) 대화상자 자신** → `+0xb21 DlgSetFocus` · `+0xb2e xxxCheckDefPushButton`.

### 4. (d) `SendMessageWorker` — syscall 없이 창 프로시저를 부르는 조건 [정적]

| 차례 | 조건 | 안 맞으면 | 줄 |
|---|---|---|---|
| 1 | 메시지가 `0x3e0 ~ 0x3e8` 아님 | syscall 길 | `+0x38 LEA EAX,[RDI-0x3e0]` · `+0x3e` · `+0x41 JBE → +0x9bb` |
| 2 | **핸들 항목 `+0x8` == TEB `+0x78`** (TEB `+0x78` 이 0 이면 `NtUserGetThreadState(0xe)` 를 부르고 결과와 상관없이 다시 TEB `+0x78`) | syscall 길 | `+0x58 ~ +0x83` · `+0x87 ~ +0xb8` (항목 찾기, 없으면 `+0xba` 0) · **`+0xbc CMP R8,RAX` · `+0xbf JNZ → +0x9b6`** |
| 3 | 사본 `+0x12` 비트 2 = 0 | syscall 길 | `+0xc5 TEST [R12+0x12],4` · `+0xd0 JNZ → +0x9b6` |
| 4 | `[[TEB+0x820]+0x10] \| [TEB+0x838]` 의 `0x2020` = 0 — **`[TEB+0x820]` 널 검사 없음** | syscall 길 | `+0xf9` · `+0x100` · `+0x103` · `+0x10a` · `+0x110 JNZ → +0x9b6` |
| 5 | 글자 종류: 보내는 쪽 (`bAnsi`) 과 창 (`+0x12` 비트 3) 이 같으면 변환 건너뜀. 다르면 메시지별 변환, `MessageTable[msg] & 0x100` 인 것은 syscall 길 — **A 판은 여기서 갈림 (sast/146)** | 변환 / syscall | `+0x14c ~ +0x16f` · `+0x175 ~ +0x225` · `+0x40f TEST R14B` |
| 6 | 사본 `+0x80` (클래스, 커널 주소) ≠ 0 → `+0x3a8` 에서 `− [사본+8] + 사본` 으로 바꿈 | **0 이면 `+0x3c5 MOVZX R9D,word [RSI+4]` 에서 `[0+4]` 접근 위반** (훅 없을 때) | `+0x29c` · `+0x2a4 TEST RSI` · `+0x2a7 JNZ → +0x3a8` / `+0x2ad JMP → +0x3b0` |
| 7 | 사용자 API 훅 (`ghmodUserApiHook` ≠ 0 · `gfUserApiHook` ≠ 0) 이면 길 B | — | `+0x3b0 ~ +0x3c3` |
| 8 | 클래스 `+4` (fnid) 가 `0x29a` 또는 `0x2a1 ~ 0x2aa` 이고 **사본 `+0x78` (창 프로시저) 이 `[psi + 0x248 + 8k]` 또는 `[psi + 0x188 + 8k]` (`k = fnid − 0x29a`) 와 같으면 길 A**, 아니면 길 B | — | `+0x3c5 ~ +0x3e5` · `+0x3ec LEA RAX,[R9-0x29a]` · `+0x3f3` · `+0x401` · `+0x409 JZ → +0x731` |

- **길 A** (`+0x731 ~ +0x8ed`): `gSharedInfo+0x30+0x10k` (메시지 비트표 포인터) 가 0 이거나 (`+0x740 / +0x743 JZ → +0x8f7`) 메시지 비트가 켜졌으면 일꾼 `[psi + fnid×8 − 0x1200]` (fnid `0x2a4` → `psi+0x320`; `0x29a` · `0x2a0` 은 `ABI_Get_apfnClientWorkerNtDll` `+0x58` · `+0x60`) 을 `(사본, msg, w, l, bAnsi)` 로 부름 (`+0x914` · `+0x956`). 표에 없으면 fnid `0x2a4` 이고 `[[사본+0x128]+8]` · 그 `+0x0` 이 0 이 아닐 때만 같은 일꾼 (`+0x767 ~ +0x806`), 아니면 `DefWindowProcA/W` · `RealDefWindowProcWorker` (`+0x870 ~ +0x8a0`). **일꾼 칸이 0 이면 널 호출.**
- **길 B** (`+0x40f ~ +0x605`): TEB `+0x800` 비트를 켜고 (`+0x585 ~ +0x599`) **`UserCallWinProcCheckWow([사본+0xe0], [사본+0x78], hwnd, msg, w, l, 0, 1)`** (`+0x600`) 로 창 프로시저를 직접 부른 뒤 되돌림 (`+0x617 ~ +0x64c`). 메시지 `0xe9` 일 때만 `[TEB+0x918]+8` 을 셈 (`+0x546 ~ +0x572`) — 이 길의 `0x30` · `0x110` · `0x87` 과는 무관.
- **syscall 길** (`+0x9c0 ~ +0xa21`): `msg ≥ 0x400` 이면 `NtUserMessageCall(hwnd, msg, w, l, 0, 0x2b1, bAnsi)` (`+0x9f4`), 아니면 `gapfnScSendMessage[MessageTable[msg] & 0xff]` (`+0xa05 ~ +0xa21`) 에서 다시 `NtUserMessageCall`.
- `SendMessageW` 는 `hwnd + 1` 이 `0` · `0x10000` (= `−1` · `0xffff` 방송) 이 아닐 때 `SendMessageInternal` 로 (`+0x28 ~ +0x34 JNZ → +0xe4`). 메시지 `& 0xfffe0000` 이면 `0x57` + 0 (`+0x17 / +0x1d → +0x143`).

**`WM_INITDIALOG`** (`+0xa64 SendMessageWorker(사본, 0x110, hwndFocus, lParam, 0)`): 길 B 면 사본 `+0x78` = **`DefDlgProcW`** 여야 앱 프로시저까지 간다. `DefDlgProcW +0x17 ValidateHwnd` → `+0x31 DefDlgProcWorker`:

- `+0x32 / +0x36` fnid 0 → `+0x3e2 ~ +0x404 NtUserSetWindowFNID(hwnd, 0x2a4)` (사본 `+0xc8 ≥ psi+0x15c` 일 때, `+0x3f0`) / `+0x41` fnid ≠ `0x2a4` 면 `DefWindowProc` 길.
- `+0x4e ValidateDialogPwnd` (이미 1) → `+0x5b ~ +0x65` 여분 블록 `+0x0` 이 0 이 아니면 지움 → **`+0x8c ~ +0x9c` `pdlg = [[사본+0x128]+8]`, 앱 프로시저 = `[pdlg+0x0]`** → `+0xa3 ~ +0xbf UserCallDlgProcCheckWow` (CFG 간접 `+0x193` · `+0x1ed`, syscall 없음 — 앞뒤로 DPI 맞춤 · 활성화 컨텍스트 · 훅만).
- `+0xcf HMValidateHandleNoSecure` (0 이면 끝) · `+0xdd` fnid `& 0xc000` · `WM_INITDIALOG` 는 앱 프로시저 뒤 기본 처리 없이 끝 — `+0x2da MOV EAX,R13D` · `+0x2dd JMP → +0x193` (돌려주는 값 = 앱 반환 `[RBP-0x28]`, `+0x18c / +0x190`). 앱이 0 을 돌려준 갈래도 `+0x986 SUB EAX,0x110` · `+0x98b JZ → +0x190` 같은 끝.
- `InternalCreateDialog` 는 그 반환을 본다: 0 이면 기본 초점 건너뜀 (`+0xa7d TEST ESI,ESI` · `+0xa7f JZ → +0xb33`).

### 5. (f) 되부름이 만드는 상태 [정적]

| 되부름 | 뒤 단계가 읽는 상태를 만드나 | 근거 |
|---|---|---|
| **`0x7b` `__xxxClientAllocWindowClassExtraBytes`** | **예** — 사본 `+0x128` 의 여분 블록 (0 채움, `+0xc8` 바이트). 없으면 `InternalCreateDialog +0x6ab` 널 읽기 | `xxxCreateWindowEx +0x1446 / +0x1a46` · `InternalCreateDialog +0x69a ~ +0x6ab` |
| `WM_NCCREATE` · `WM_CREATE` … (커널 → 대화상자 창 프로시저) | **아니오** — `DefDlgProcWorker` 가 첫 메시지에서 하는 일 (`NtUserSetWindowFNID`, `ValidateDialogPwnd`) 은 `InternalCreateDialog +0x667` 이 어차피 하고 (fnid 는 `SetDialogPointer +0xf2`), `pdlg` 칸은 `InternalCreateDialog` 가 쓴다 | `DefDlgProcWorker +0x32 ~ +0x55` · `InternalCreateDialog +0x667 ~ +0x72c` |
| 컨트롤의 `WM_NCCREATE` / `WM_CREATE` | `[추측]` — 컨트롤 프로시저 본문은 안 읽음. 이 길에서 컨트롤에 닿는 건 `SendMessageW(ctl, 0x30 / 0x87)` 와 `DlgSetFocus` 뿐 — 컨트롤 사본 `+0x12` 비트 2 를 켜 두면 syscall 길 (`NtUserMessageCall`, stub 0) 로 가서 컨트롤 프로시저를 아예 안 부름 (`SendMessageWorker +0xc5 / +0xd0`), 항목이 없으면 `SendMessageInternal` 이 조용히 0 | 3절 · 4절 |
| `WM_GETMINMAXINFO` · `WM_NCCALCSIZE` · `WM_SIZE` 등 | 아니오 — user32 가 이 길에서 읽는 칸 없음 | 결과서 4절 |

### 6. 칸 표 (물음 2) [정적, 이름은 `[추측]`]

**핸들 항목** (`gSharedInfo+0x8` 배열의 한 칸) — 크기 = **`gSharedInfo+0x10` (dword)** (`ValidateHwnd +0x65 IMUL`), 읽는 칸이 `+0x1a` 까지라 `0x1c` B 이상. 번호 = `hwnd & 0xffff` (`+0x51`), 개수 검사 `번호 < [psi+8]` (`+0x5b`) · `< [[gSharedInfo+0]+8]` (`+0x7c`). 고유 수 = `(hwnd >> 16) & 0x7fff` (`+0x90 ~ +0x9c`).

| 칸 | 크기 | 읽는 줄 | 통과 조건 · 0 가능 |
|---|---|---|---|
| `+0x00` | 8 | `ValidateHwnd +0xd0` · `HMValidateHandleNoSecure +0x8e` | 커널 쪽 창 주소. **사본 = TEB `+0x828` + 이 값** (더하기만). 합이 0 이면 NULL (`+0xe7`) |
| `+0x08` | 8 | `SendMessageWorker +0xb4` | **소유 스레드 ID** (`HMAllocObjectEx +0x42a ~ +0x43e`). 직접 호출은 == TEB `+0x78` |
| `+0x10` | 8 | `ValidateHwnd +0xc6` · `HMValidateHandleNoSecure +0x81` | == `[[TEB+0x820]+0]` 이면 통과, 다르면 `NtUserMapDesktopObject` (`0x14d9`, `+0x135`) 로 한 번 더 (막지는 않음) |
| `+0x18` | 1 | `ValidateHwnd +0x86` · `+0xb4` | 0 = 빈 칸 · **1 = 창** (`HMAllocObjectEx +0x244` 가 씀) |
| `+0x19` | 1 | `ValidateHwnd +0xae` · `+0xd3` | 비트 0 = 실패 (파괴됨 `[추측]`) · 비트 2 = 보안 검사 (`NtUserValidateHandleSecure` `0x15ce`) 건너뜀 |
| `+0x1a` | 2 | `ValidateHwnd +0xa4` | == hwnd 윗 15비트 (`0x7fff` 만능, `+0x146`) |

**TEB 칸**:

| 칸 | 읽는 줄 | 뜻 · 조건 |
|---|---|---|
| `+0x78` | `InternalCreateDialog +0x13c` · `SendMessageWorker +0x58 / +0x83` · `SendMessageW +0x48` | 스레드 ID (run142). **0 으로 두면 항목 `+0x8` 도 0 이어야** 직접 길 |
| `+0x800` | `SendMessageWorker +0xdf` | 길 B 가 비트를 켰다 되돌림 — 0 시작 가능 |
| `+0x810` | `InternalCreateDialog +0x1d4` · 조각 `+0x335` | `< 0x400` 이면 옛 앱 (`DS_3DLOOK` 강제 안 함 · `IsWindow` 0 이어도 hwnd 반환). 0 가능 |
| `+0x81c` | `ValidateHwnd +0xde` · `SendMessageInternal +0xf9` | 비트 `0x20000000` 켜지면 `NtUserValidateHandleSecure` 를 매번 — 0 이어야 syscall 없음 |
| **`+0x820`** | `HMValidateHandleNoSecure +0x72` · `SendMessageWorker +0xf9 ~ +0x103` | `+0x0` 데스크톱 표지 · `+0x8` 데스크톱 창 · `+0x10` 훅 비트. **`SendMessageWorker` 는 널 검사 없이 `[+0x10]`** |
| `+0x828` | `ValidateHwnd +0xcc` · `HMValidateHandleNoSecure +0x87` | 사본 = 이 값 + 항목 `+0x0`. **범위 검사 없음.** 0 이면 항목 값이 곧 사본 주소 |
| `+0x838` | `SendMessageWorker +0x103` | `0x2020` 비트 0 |
| `+0x840` / `+0x848` | `ValidateHwnd +0x17 / +0x20` · `SendMessageInternal +0x2e / +0x37` · `IsWindow +0x26` | 한 칸 캐시 (직전 hwnd · 사본). 0 이면 느린 길. user32 는 안 씀 |
| `+0x860` · `+0x8e8 / +0x8ec` | `UserCallDlgProcCheckWow +0x3a ~ +0x53` · `UserCallWinProcCheckWow` · `SetThreadDpiAwarenessContextFromWindow` | 0 이면 일반 길 · DPI 문맥은 TEB 만 바꿈 (syscall 없음) |

**공유 정보** (`gSharedInfo` = user32 rva `0xda350`, `psi` = `gpsi`):

| 칸 | 읽는 줄 | 필요 |
|---|---|---|
| `gSharedInfo+0x0` (= `psi`) · `psi+0x8` (항목 개수) | `ValidateHwnd +0x3c / +0x5b / +0x7c` | 번호 < 개수 |
| `gSharedInfo+0x8` (항목 배열) · `+0x10` (항목 크기) | `ValidateHwnd +0x65 ~ +0x75` | 위 표 |
| `gSharedInfo+0x18` → `+0x8` (주 모니터) | `GetPrimaryMonitor +0x0 ~ +0x10` | **`+0x18` 이 0 이면 `+0x7` 접근 위반** (run142). 모니터 0 이면 `GetPrimaryMonitor` 0 → `_MonitorFromRect` → NULL 가지 N5 |
| `gSharedInfo+0x28 + 0x10k` (최대 번호) · `+0x30 + 0x10k` (메시지 비트표) | `SendMessageWorker +0x734 ~ +0x761` | 0 이면 "모든 메시지를 일꾼으로" (길 A 안) |
| `psi + 0x188 + 8k` · `psi + 0x248 + 8k` (`k = fnid − 0x29a`; fnid `0x2a4` → `psi+0x1d8` · `psi+0x298`) | `SendMessageWorker +0x401` · `+0x3f3` | 사본 `+0x78` 과 같으면 길 A, 다르면 (0 포함) 길 B |
| `psi + fnid×8 − 0x1200` (fnid `0x2a4` → `psi+0x320`) | `SendMessageWorker +0x7c4` · `+0x914` | 길 A 일꾼 — 0 이면 널 호출 |
| `psi+0x364 + 2i` (i = 0 ~ 0x18) | `InternalCreateDialog +0x84b` · `+0x871` (i = 5 콤보) | 컨트롤 클래스 원자 (서수 컨트롤) |
| `psi+0x15c` | `DefDlgProcWorker +0x3e2 ~ +0x3f6` | fnid 0 인 창에서만 |
| `psi+0x0` 비트 `0x2` `0x4` `0x80` · `psi+0x7b4` · `psi+0x1b5c` | `SendMessageWorker +0x1af` · `InternalCreateDialog +0xb4a ~ +0xb61` · `+0xc50` | 0 이면 전부 건너뜀 |

**창 사본** (`pwnd`, `ValidateHwnd` 반환) — 읽는 칸이 `+0x128` (qword) 까지라 **최소 `0x130` B**. user32 는 사본에 **쓰는 줄이 없다** (쓰기는 전부 커널).

| 오프셋 | 크기 | 이름 `[추측]` | 읽는 줄 | 쓰는 쪽 (커널) | 0 가능 / 있어야 할 값 |
|---|---|---|---|---|---|
| `+0x00` | 8 | `HWND` | `InternalCreateDialog` (`ValidateDialogPwnd +0x49` → `NtUserSetDialogPointer`) · `SendMessageWorker +0x1c` · `+0x87` (낮은 16비트로 항목 다시 찾음) | — | **hwnd 와 같아야** |
| `+0x08` | 8 | 커널 쪽 자기 주소 | `SendMessageWorker +0x3a8` · `_GetChildControl +0x2a` · `_IsDescendant +0x24` · `_NextControl +0xe6` | — | 사본 안 커널 포인터 (`+0x30` · `+0x38` · `+0x80`) 를 `포인터 − [+8] + pwnd` 로 바꿈. 커널 = 사용자 주소면 `[+8] == pwnd` |
| `+0x11` | 1 | 깃발 | `UpdateWindow +0x1d` | — | 0 가능 (켜지면 `NtUserUpdateWindow`) |
| **`+0x12`** | 1 | 창 깃발 | 비트 0: `ValidateDialogPwnd +0x6` · `GetParentDialog +0x8` · `_GetWindowLong +0x50` · `DefDlgProcWorker +0x1ea` / 비트 2: `SendMessageWorker +0xc5` / 비트 3: `+0x14c` | 비트 0: `SetDialogPointer +0x10a` | **대화상자: 처음엔 0** (비트 0 이 먼저 켜져 있으면 `pdlg` 를 안 만듦) · 비트 2 = 0 (직접 길) · 비트 3 = 0 (유니코드) |
| `+0x1a` | 1 | 깃발 | `_GetChildControl +0x31` · `_NextControl +0x7b` | — | 0 |
| `+0x1c ~ +0x1f` | 4 | 스타일 | `InternalCreateDialog +0x860` · `+0xbf4` (`& 0x10`) · `+0xae0` · `+0x68e` (`+0x1e`) · `+0xa90` (`+0x1d`) · `+0xf68` · `+0xb76` (`+0x1f`) · `_GetChildControl +0xc` | `xxxCreateWindowEx +0x577` | 만들 때 준 스타일 (`WS_VISIBLE` 뺌) |
| `+0x2a` | 2 | fnid | `ValidateDialogPwnd +0x29` · `IsWindow +0xfe` · `DefDlgProcWorker +0x32 / +0x41` · `_IsDescendant +0x7` | `SetDialogPointer +0xf2` (0 이면 `0x2a4`) | 0 시작 → `0x2a4`. `& 0xc000` 이면 `ValidateDialogPwnd` 0, 부호 비트면 `IsWindow` 0 |
| `+0x30` | 8 | 부모 (커널 주소) | `_GetChildControl +0x21` · `_NextControl +0x53 / +0x57` · `_IsDescendant +0x17` | — | 0 가능 (최상위) |
| `+0x38` | 8 | 첫 자식 (커널 주소) | `_NextControl +0xdd` · `UpdateWindow +0x23` | — | **0 이면 초점이 대화상자 자신** · `UpdateWindow` 는 syscall 없이 1 |
| `+0x78` | 8 | 창 프로시저 (사용자 주소) | `SendMessageWorker +0x47` · `+0x3e7` | — | **0 이면 안 됨** — 길 B 면 이 주소를 부름 (대화상자 = `DefDlgProcW`) |
| `+0x80` | 8 | 클래스 (커널 주소) | `SendMessageWorker +0x29c ~ +0x3c5` | — | **0 이면 `[0+4]` 접근 위반**. 가리키는 곳 `+4` word = 클래스 fnid |
| `+0x88` | 8 | (갱신 영역 `[추측]`) | `UpdateWindow +0x13` | — | 0 가능 |
| `+0xc8` | 4 | 창 여분 바이트 크기 | `ValidateDialogPwnd +0x1b` · `DefDlgProcWorker +0x3f0` · 커널 `GetClientExtraBytesTotalSize +0x11` | `xxxCreateWindowEx +0x59e` | 대화상자 **`0x1e` 이상** — 되부름 `0x7b` 의 요청 크기 |
| `+0xe0` | 8 | 활성화 컨텍스트 | `DefDlgProcWorker +0xa3` · `SendMessageWorker +0x5e6` | — | 0 가능 |
| `+0x120` | 4 | 창 DPI 문맥 | `UserCallWinProcCheckWow +0x102` · `SetThreadDpiAwarenessContextFromWindow +0x12c` | `xxxCreateWindowEx +0x510` | 0 가능 |
| **`+0x128`** | 8 | 창 여분 블록 (사용자 주소) | `InternalCreateDialog +0x69a` · `+0x6b3` · … (`pdlg` 칸을 쓸 때마다) · `DefDlgProcWorker +0x5b` · `+0x8c` · `SendMessageWorker +0x776` | `xxxCreateWindowEx +0x1a46` (되부름 `0x7b` 결과) | **0 이면 `+0x6ab` 접근 위반.** 쓰기 가능 · 0 채움 · `+0xc8` 바이트 이상 |
| `+0x140` | 4 | 컨트롤 ID `[추측]` | `xxxCheckDefPushButton +0xf3` | — | 컨트롤마다 `hMenu` 로 준 ID (`+0x957`) |

### 7. (e) NULL (→ −1) 로 나가는 가지 전부 — `InternalCreateDialog` [정적]

공통 끝: `+0xd4e XOR EAX,EAX` → `+0xd50 JMP → +0xbb9` (또는 `+0xc90` 정리 뒤 `+0xd4e`). 바깥 `InternalDialogBox +0x78 JZ → +0x151` → 부모 0 이면 `+0x159 / +0x15c JZ` → 조각 `OR RAX,-1` = **−1**.

| # | 줄 | 조건 | 뒷정리 |
|---|---|---|---|
| N1 | `+0x13c / +0x140 JZ → +0xd37` · `+0xd45 / +0xd48` · `+0xd4e` | TEB `+0x78 == 0` 이고 `NtUserGetThreadState(0xe)` 0 (run142) | 없음 |
| N2 | `+0x216 CMP R14D,EAX` · `+0x219 JNZ → +0x10cd` · `+0x10cf JNZ → +0xd4e` | 새 앱 (`RtlGetExpWinVer ≥ 0x400`) 이고 템플릿 스타일 `0x8000` 비트 | 없음 |
| N3 | `+0x8ca16 / +0x8ca1b LoadMenuW` · `+0x8ca28 JZ → +0xd4e` | 템플릿 메뉴가 있는데 `LoadMenuW` 0 | 없음 |
| N4 | `+0x333 / +0x33a` · `+0x345 JZ → +0xc90` | **`CreateCompatibleDC(0)` = 0** | `+0xc90` |
| N5 | 조각 `+0x220 / +0x225 _MonitorFromRect` · `+0x22b JZ → +0xc90` | 모니터 NULL (`gSharedInfo+0x18` 의 `+8` 이 0) · `_MonitorFromRect` 도 NULL `[추측: 본문 안 읽음]` | 〃 |
| N6 | `+0xc7d / +0xc82 _MonitorFromPoint` · `+0xc8a` | `DS_CENTERMOUSE` + `psi+0x7b4 ≠ 0` + 모니터 NULL (`psi+0x7b4` 0 이면 안 탐) | 〃 |
| N7 | `+0x644 / +0x647` · `+0x64a JZ → +0xc90` | **`VerNtUserCreateWindowEx` NULL** (커널 0 · `NtUserGetAtomName` 0) | 〃 |
| N8 | `+0x658` · `+0x65e JZ → +0xc90` | `ValidateHwnd` NULL | 〃 (창 안 부숨) |
| N9 | `+0x66c` · `+0x66e JZ → +0xc90` | `ValidateDialogPwnd` 0 | 〃 |
| N10 | `+0x97a ~ +0x980 JZ → +0xbf4` | 컨트롤 `VerNtUserCreateWindowEx` NULL | `DS_NOFAILCREATE` 없으면 `+0xc01 NtUserDestroyWindow` → `+0xc0d JMP → +0xd4e` |
| N11 | `+0x83e JNC → +0xbf4` | 컨트롤 서수 `≥ 0x19` | N10 과 같음 |
| N12 | `+0xa6f / +0xa74` · `+0xa77 JZ → +0xb9b` → `+0xba5` | `WM_INITDIALOG` 뒤 hwnd 무효 (앱이 `DestroyWindow`) | 없음 |
| N13 | `+0xb36 / +0xb3b` · `+0xb3d JZ → +0x1358` (조각 `+0x327`) · 조각 `+0x335 CMP [TEB+0x810],0x400` · `+0x33b JNC → +0xd4e` | `IsWindow` 0 이고 새 앱 (옛 앱이면 조각 `+0x341 JMP → +0xbb6` hwnd 그대로) | 없음 |
| N14 | 조각 `+0x366 ~ +0x37c` (= `+0x1397 ~ +0x13ad`) → 조각 `+0x382 MOV RDI,R13` (= `+0x13b3`) → `+0x385 JMP → +0xb9b` | `DS_SETFOREGROUND` 이고 그 뒤 `IsWindow` 0 | 없음 |
| N15 | `+0xba3 JNZ → +0xbb6` 안 탐 → `+0xba5 / +0xba8` · `+0xbad NEG` · `SBB` · `+0xbb3 AND RDI,RCX` | 끝 검사 `HMValidateHandleNoSecure` 0 (`[RBP+0x8] & 4` 가 0 일 때) | 없음 |

- 바깥에서 −1: `DialogBoxParamW +0x39 JZ` (`FindResourceExW` 0) · `+0x53 JZ` (`LoadResource` 0) · `DialogBoxIndirectParamAorW +0x27 JNZ` 안 탐 → `+0x29 OR RAX,-1` (`GetSizeDialogTemplate` 0) · `InternalDialogBox +0x78 JZ → +0x151 → 조각 OR RAX,-1` (N1 ~ N15 모두).
- **부모 ≠ 0 이고 `ValidateHwnd(부모)` 0 이면 −1 이 아니라 0** (`InternalDialogBox +0x101 ~ +0x107 JZ → +0x8f`). 부모 ≠ 0 · `ESI == 0` 이면 `+0x167 NtUserEnableWindow(부모, EBX)` 뒤 같은 −1. 우리 표본 부모는 0 이라 해당 없음.

### 8. (a) 가드 뒤 ~ `+0x63f` 앞 — GDI · 지표 부름 [정적]

| 템플릿 / 상태 | 줄 | 부르는 것 | 0 을 받으면 |
|---|---|---|---|
| 늘 | `+0x7f` | `GetDpiForSystem` | 지나감 |
| 늘 | `+0x19e` (`hInst == 0` 이면 `+0x1005 GetModuleHandleW(0)`) | `RtlGetExpWinVer` | `< 0x400` 이면 옛 앱 갈래 (`+0x1be JC → +0x10c2`) |
| 늘 | `+0x316` | `GetDialogMonitor(부모, 1)` | 모니터 0 → 나중에 N5 |
| 늘 | `+0x333` | `CreateCompatibleDC(0)` | **0 → N4 (NULL)** |
| 부모 GDI 확대 | `+0x34e IsWindowGdiScaledX` → 조각 `FUN_1800ab5c3` | `NtUserGetDC` (`+0x9c`) → … `NtUserReleaseDC` | 부모 0 이면 안 탐 `[추측: IsWindowGdiScaledX(0) = 0]` |
| `DS_SETFONT` (`0x40`) | `+0x35d` · `+0x386` | `CreateDlgFont` (gdi32 — Q278) | 0 → `+0x3de / +0x3e9 GetStockObject(0xd)`. 둘 다 0 → 글꼴 없음 (`WM_SETFONT` 안 보냄) |
| `DS_FIXEDSYS` (`0x8`) · 새 앱 | `+0xdaf ~` → 조각 `+0x175 / +0x17a` | `GetStockObject(0x10)` | 0 → 글꼴 없음 |
| 글꼴 있을 때 | `+0xe24 ~ +0xe85` · 조각 `+0x19f ~ +0x1d6` | `SelectObject` → `GdiGetCharDimensions` → `SelectObject` | `SelectObject` 0 → `DeleteObject` + `GetStockObject(0xd)`. `GdiGetCharDimensions` 0 → 기본 단위 그대로 |
| 늘 | `+0x3b0` · `+0x3bb` | `GetDpiServerInfoForCurrentThread` → `[+0x20]` · `[+0x24]` | 값 0 이면 좌표 0 (크래시 아님) |
| 늘 | `+0x409` · `+0x467` · `+0x533` | `DeleteDC` · `_AdjustWindowRectExForDpi` · `RepositionRect` | 안 봄 |
| 부모 ≠ 0 | `+0xdf2` · `+0xe00` | `ClientToScreen` · `GetWindowLongW(부모, −0x14)` | 해당 없음 |

- run144 와 잇기: 이 길의 GDI 부름 가운데 **NULL 로 끝나는 건 `CreateCompatibleDC(0)` 하나**. `SelectObject` 가 DC 칸이 없어 `0x57` + 0 을 줘도 (run144 7절) 여기선 대체 글꼴로 넘어가고, `GdiGetCharDimensions` 가 말없이 돌아가도 (run144) 기본 글자 단위를 쓴다 `[정적]`.

### 호출 관계

```
user32!DialogBoxParamW (hInst, 이름, 부모 0, proc, lParam)                                 [정적]
 ├─ +0x31 FindResourceExW · +0x48 LoadResource            0 → −1
 └─ DialogBoxIndirectParamAorW +0x25 GetSizeDialogTemplate (0 → −1) → InternalDialogBox
     └─ +0x75  InternalCreateDialog  (NULL → InternalDialogBox +0x151 → −1)
                ├─ +0xd40 NtUserGetThreadState(0xe)            가드 (TEB+0x78 == 0 일 때, run142)
                ├─ +0x31b GetDialogMonitor · +0x33a CreateCompatibleDC(0) (0 → NULL) · +0x38b CreateDlgFont …
                ├─ +0x644 VerNtUserCreateWindowEx(…, 0x8002, 0x8002, …, ver|0x40000000)
                │         ├─ +0x17c ClassNameToVersion +0x22d NtUserGetAtomName ── SYSCALL 0x109c
                │         │          ⇒ win32kfull!NtUserGetAtomName +0x84 win32kbase!UserGetAtomName
                │         │            +0x51 nt!RtlQueryAtomInAtomTable  (< 0xc000 → +0x29d _snwprintf_s "#%u")  → 6 "#32770"
                │         └─ +0x236 NtUserCreateWindowEx ── SYSCALL 0x106f
                │                    ⇒ win32kfull!NtUserCreateWindowEx +0x67d xxxCreateWindowEx
                │                      ├─ +0x9ce GetClientExtraBytesTotalSize (= 사본+0xc8 ≥ 0x1e)
                │                      ├─ +0x144b xxxClientAllocWindowClassExtraBytes +0x62 KeUserModeCallback(0x7b)
                │                      │          ⇢ user32!__xxxClientAllocWindowClassExtraBytes +0x24 RtlAllocateHeap(0 채움) · +0x41 NtCallbackReturn
                │                      └─ +0x1a46 [사본+0x128] ← 여분 블록
                ├─ +0x658 ValidateHwnd                          0 → NULL
                ├─ +0x66c ValidateDialogPwnd                    0 → NULL
                │         ├─ +0x44 AllocLookasideEntry          pdlg 0x58 B (pUserHeap, memset 0)
                │         └─ +0x56 NtUserSetDialogPointer(hwnd, pdlg) ── SYSCALL 0x1552   반환 버림
                │                  ⇒ win32kfull!SetDialogPointer +0xbc [[사본+0x128]+8] ← pdlg · +0xf2 fnid 0x2a4 · +0x10f 사본+0x12 |= 1
                ├─        +0x69a ~ +0x72c  pdlg 칸 채우기 (+0x0 앱 프로시저 · +0x20 = 1 …)
                ├─ +0x97a VerNtUserCreateWindowEx (컨트롤마다) · +0x9ac SendMessageW(0x30) · +0x9bf SendMessageW(0x87)
                ├─ +0xa69 SendMessageWorker(사본, 0x110)          직접 길 B → +0x605 UserCallWinProcCheckWow
                │          ⇢ DefDlgProcW +0x36 DefDlgProcWorker +0xc4 UserCallDlgProcCheckWow ⇢ 앱 프로시저 ([pdlg+0])
                ├─ +0xab4 _GetChildControl · +0xace _NextControl · +0xb26 DlgSetFocus · +0xb33 xxxCheckDefPushButton
                ├─ +0xb3b IsWindow                              0 + 새 앱 → NULL
                └─ +0xf81 NtUserShowWindow(hwnd, 1) · +0xf8e UpdateWindow    반환 안 봄
```

- (나무 오프셋은 ret 자리. `⇒` 는 syscall 너머 커널, `⇢` 는 되부름 · CFG 간접. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (결과서 "지금 우리 맞나 · 틀리나")

flow-writer 는 `../PE_emulator/ext/syscall/syscall_layer_cpp/src/gui/nt_user_get_atom_name.cpp` (21줄) · `nt_user_create_window_ex.cpp` (26줄) · `src/alpc/csr_user_connect_stub.cpp` (99줄) 를 줄 번호만 훑었다 (읽기만). 갈래 BD01b (`PE_emulator-BD`) 코드는 안 봤다 — 원문 요약만 씀. 고칠지는 사용자 · PE_emulator 결정.

| 우리 지금 | 진짜 (정적) | 판정 |
|---|---|---|
| `nt_user_get_atom_name.cpp:15` 원자 `0xc000` 아니면 `stop` (기본 대답 0), `:16 ~ :17` `0xc000` 이면 1, `:9` 이름 글 안 씀 | 정수 원자 `0 < a < 0xc000` 은 **표 없이 `L"#<십진>"` + NUL 을 `[USTR+8]` 에 쓰고 글자 수 (NUL 제외) 반환** — `0x8002` → 6 · `#32770`. 한도 = `MaximumLength / 2` 글자, 모자라면 `한도 − 1` 글자로 자르고 그 수 (한도 < 2 글자면 0). `Length` 는 **안 씀** (user32 가 씀). 원자 0 → 0 | **`0x8002` 갈래 틀림 — 지금 벽.** `:10 TODO(VM)` 중 정수 원자 몫은 **정적으로 닫힘** (`RtlQueryAtomInAtomTable +0x75` · `+0x298` · `+0x2c5` · `UserGetAtomName +0x60`). `0xc000` 이상 (표 원자) 의 진짜 이름은 그대로 동적 (Q242). "0 아닌 값이면 지나감" (run131) 은 표 원자 · 정수 원자 모두 맞음 |
| `nt_user_create_window_ex.cpp:20` 클래스가 `0xc000` 또는 `≥ 0x10000` 아니면 `stop` (→ 0) | 대화상자 창은 둘째 · 셋째 인자 모두 **`0x8002`**, 컨트롤은 `psi+0x364` 표 원자 (서수) 또는 이름 글 | **틀림** — `0x8002` 를 받아야 함 (원자 이름을 풀어도 바로 다음 벽 N7) |
| `:22` 늘 같은 hwnd `0x10010` · `:13` "몇 번 불러도 같은 값" | 대화상자 한 번에 **`1 + 컨트롤 수`** 번 불림. 대화상자 hwnd 는 항목 · 사본 · 여분 블록이 있어야 `ValidateHwnd` · `ValidateDialogPwnd` · `IsWindow` · `HMValidateHandleNoSecure` 가 지나감 | **틀림** — 같은 hwnd 면 컨트롤 고리의 `SendMessageW(ctl, …)` 가 대화상자 자신에게 가서 앱 프로시저가 `WM_SETFONT` · `WM_GETDLGCODE` 를 받고 `pdlg+0x20` 을 덮어쓸 수 있음 (결과서 9절) |
| `:14` 안 하는 것: 창 객체 · 핸들 표 · 공유 메모리 · 창 함수로 메시지 보내기 · `NtUserDestroyWindow` | 대화상자 hwnd 에는 항목 (`+0x0` · `+0x8` · `+0x10` · `+0x18` · `+0x1a`) · 사본 (6절 표) · **여분 블록 (되부름 `0x7b` 몫)** 이 꼭 필요. 컨트롤 hwnd 는 항목이 없어도 `SendMessageInternal +0x11b` 가 조용히 0 — 고리는 지나감 | **모자람** — 대화상자 하나만큼은 채워야 함 |
| `csr_user_connect_stub.cpp:87 ~ :90` `psi` 0x2000 B · 캡처 `+0x8` (= `gSharedInfo+0x0`) 만 씀 → `gSharedInfo+0x8` · `+0x10` · `psi+0x8` = 0 | 핸들 표 블록이 있어야 `ValidateHwnd` 가 지나감 — 배열 · 항목 크기 · 개수 · (서수 컨트롤이면) `psi+0x364` 원자 표. `gSharedInfo+0x28 ~` 비트표 · `psi+0x188` / `+0x248` 표 · `psi+0x320` 은 0 이어도 됨 (길 B) | **모자람** — run142 최소 조건 3 ~ 5 (원문에 따르면 BD01b 가 넣음) 위에 얹는 것 |
| 새 stub `NtUserSetDialogPointer` (지금 몸 없음 → 기본 대답 0) | user32 는 **반환을 안 보지만 부수 효과가 필요**: `*(qword*)([사본+0x128] + 8) = pdlg` · 사본 `+0x2a` 가 0 이면 `0x2a4` · 사본 `+0x12 \|= 1` | **틀림 (반환 0 은 괜찮음, 안 적으면 `+0x6af` 접근 위반)** |
| 새 stub `NtUserShowWindow` · `NtUserUpdateWindow` · `NtUserSetFocus` · `NtUserSetForegroundWindow` · `NtUserSetDialogSystemMenu` · `NtUserGetSystemMenu` · `NtUserSetWindowContextHelpId` · `NtUserDestroyMenu` · `NtUserDestroyWindow` · `NtUserMessageCall` (몸 없음 → 0) | **반환을 안 본다** (`+0xf81` · `UpdateWindow +0x3e` 꼬리 · `DlgSetFocus +0x3b` 꼬리 · 조각 `+0x36d` 뒤 `IsWindow` 만 봄 · 조각 `+0x2b5` · `+0x299` · `+0xc08`). `NtUserMessageCall` 은 직접 길이 안 되는 메시지 · 컨트롤용 | **맞음 — 기본 대답 0 으로 충분** |

**stub 이 채워야 하는 최소 목록** (run142 "가드 뒤 첫 벽까지" 7 · 8 을 채운 판. 1 ~ 6 은 run142 그대로 둔다):

1. **`NtUserGetAtomName(원자, USTR*)`** (`0x109c`): `0 < 원자 < 0xc000` 이면 `L"#<십진>"` + NUL 을 `Buffer` 에 쓰고 글자 수 반환 (`0x8002` → 6). 표 원자 (`≥ 0xc000`) 는 0 아닌 값. `Length` 는 건드리지 않는다.
2. **공유 블록** (`csr_user_connect_stub.cpp`): `gSharedInfo+0x0 == psi` · `gSharedInfo+0x8` 항목 배열 (쓰는 hwnd 번호까지) · `gSharedInfo+0x10` 항목 크기 (`≥ 0x1c`) · `psi+0x8` 개수 · `gSharedInfo+0x18` → `+0x8` 주 모니터 (run142) · (서수 컨트롤이면) `psi+0x364 + 2i` 원자 (0 아님).
3. **TEB**: `+0x78` (0 이면 항목 `+0x8` 도 0) · `+0x820` (0 아님, `+0x0` 표지 · `+0x10` dword 0, run142 의 0 채운 `0x48` B 면 됨) · `+0x828` 0 가능 · `+0x838` 0 · `+0x81c` 비트 `0x20000000` 0 · `+0x840` / `+0x848` 0 · `+0x810` (0 이면 옛 앱 — 그래도 지나감).
4. **`NtUserCreateWindowEx`** (`0x106f`): 둘째 · 셋째 인자 원자 `0x8002` (와 서수 원자 · 이름 글) 를 받는다. **hwnd 마다 다른 값.**
   - **대화상자 hwnd** — (a) 항목: `+0x0` = 사본 주소 − TEB `+0x828` · `+0x8` = TEB `+0x78` · `+0x10` = `[[TEB+0x820]]` · `+0x18` = 1 · `+0x19` = 0 · `+0x1a` = `(hwnd >> 16) & 0x7fff` (또는 `0x7fff`). (b) 사본 (≥ `0x130` B, 0 채움 뒤): `+0x0` hwnd · `+0x8` 자기 주소 · `+0x1c` 스타일 (`WS_VISIBLE` 뺌) · `+0x12` **0** · `+0x2a` 0 · `+0x78` = **진짜 `user32!DefDlgProcW` 주소** · `+0x80` 읽을 수 있는 클래스 블록 (`+4` word — 0 이어도 길 B, `0x2a4` 면 `psi+0x1d8` / `+0x298` 칸이 `+0x78` 과 달라야 길 B) · `+0xc8` ≥ `0x1e` · `+0x38` 0 (자식 연결 안 함 → 초점이 대화상자 자신). (c) **여분 블록**: user32 힙에서 `+0xc8` 바이트 0 채움 → 사본 `+0x128` (되부름 `0x7b` 대신).
   - **컨트롤 hwnd** — 0 아닌 서로 다른 값이면 고리는 지나감 (항목이 없으면 `SendMessageW` 가 조용히 0, `WM_GETDLGCODE` 0 → `pdlg+0x20` 은 처음 값 1 그대로). 컨트롤 프로시저를 부르게 하려면 컨트롤마다 위 (a) (b) 와 `+0x140` ID · 대화상자 `+0x38` 자식 연결 — **sast/146 몫**.
5. **`NtUserSetDialogPointer(hwnd, pdlg)`** (`0x1552`): `[[사본+0x128] + 8] = pdlg` · `사본+0x2a` 0 이면 `0x2a4` · `사본+0x12 |= 1`. 반환 아무 값.
6. 나머지 (`ShowWindow` · `UpdateWindow` · `SetFocus` · `SetForegroundWindow` · `SetDialogSystemMenu` · `GetSystemMenu` · `SetWindowContextHelpId` · `DestroyMenu` · `DestroyWindow` · `MessageCall`) 는 반환 무관 — 기본 대답 0.
7. (sast/146 에서 보탬) 모달 고리 `DialogBox2` 입구 (`NtUserPeekMessage` 가 줄 메시지) · `EndDialog` 뒤 syscall · A 판 갈림.

**되부름이 꼭 필요한가 — 아니오.** 되부름 `0x7b` 가 하던 "사본 `+0x128` 에 0 채운 여분 블록" 하나만 `NtUserCreateWindowEx` stub 이 직접 만들면, `WM_NCCREATE` 류 창 프로시저 되부름 없이도 (a) ~ (e) 가 지나간다 `[정적]`. 사용자 모드 되부름 바퀴를 세울 필요는 이 길에선 없다.

**정적으로 못 닫은 것** (결과서 그대로):

- `[공백]` `_MonitorFromRect` 가 `pDispInfo+8 == 0` 일 때 정말 NULL 인지 (N5, 본문 안 읽음).
- `[공백]` 컨트롤 프로시저 (`ButtonWndProcWorker` 류) 본문 — 컨트롤 사본 비트 2 / 항목 없음으로 건너뛸 수 있음.
- `[공백]` `CreateDlgFont` 안 gdi 부름 (Q278).
- `[추측]` 사본 `+0x12` 비트 0 · 2 · 3, `pdlg+0x40` · `+0x50` 의 이름 (형식이 PDB 에 없음).
- `[공백]` `0x7b` 말고 다른 되부름이 컨트롤 사본 `+0x128` 을 채우는지 (컨트롤 클래스 `cbWndExtra`).

## 확인된 것

- `[정적]` 정수 원자 (`0 < a < 0xc000`) 이름은 원자 표를 안 찾고 `RtlQueryAtomInAtomTable` 이 `L"#%u"` 로 짓는다. 끝 NUL 을 쓰고 세지 않으며, 반환 = 글자 수 (win32kbase 가 바이트 ÷ 2). 커널은 `UNICODE_STRING.Length` 를 안 쓴다 — user32 `ClassNameToVersion +0x232 / +0x235` 가 쓴다. `0x8002` → 6 · `#32770`.
- `[정적]` 대화상자 창 `NtUserCreateWindowEx` 클래스 인자는 `0x8002` 그대로 (템플릿 클래스 빈 글), `hInstance` 는 준 값 그대로, 끝 인자 `| 0x40000000`.
- `[정적]` 돌아온 hwnd 는 `ValidateHwnd` · `ValidateDialogPwnd` 가 syscall 없이 검사 (핸들 항목 · TEB `+0x820` / `+0x828` · 사본 `+0x12` · `+0xc8` · `+0x2a`). 실패해도 창을 부수지 않고 NULL.
- `[정적]` `pdlg` 는 user32 가 0 으로 잡고, `NtUserSetDialogPointer` (반환 버림) 가 커널 쪽에서 여분 블록 `+8` 에 적고 fnid `0x2a4` · 사본 `+0x12` 비트 0 을 켠다.
- `[정적]` 사본 `+0x128` 여분 블록은 `xxxCreateWindowEx` 가 되부름 `0x7b` (`__xxxClientAllocWindowClassExtraBytes`, user32 힙 0 채움) 로 받아 `+0x1a46` 에 넣는다.
- `[정적]` 컨트롤 하나가 NULL 이면 대화상자를 부수고 NULL — `DS_NOFAILCREATE` 면 그 컨트롤만 건너뜀.
- `[정적]` `SendMessageWorker` 의 직접 길 조건 (항목 `+0x8` == TEB `+0x78` · 사본 `+0x12` 비트 2 · TEB `+0x820` 훅 비트 · 클래스 ≠ 0), 길 B 는 `UserCallWinProcCheckWow` 로 사본 `+0x78` 을 부르고, `DefDlgProcWorker` 는 `[pdlg+0]` 앱 프로시저를 `UserCallDlgProcCheckWow` 로 부른다.
- `[정적]` `ShowWindow` · `UpdateWindow` · `SetFocus` · `SetForegroundWindow` · `SetDialogSystemMenu` · `SetWindowContextHelpId` 반환은 안 본다. NULL 가지 15개 + 바깥 −1 줄 넷.
- `[관찰]` test.exe 초기화 끝 모듈 6개에 user32 가 없다 (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 과 같은 줄) — 이 길은 test.exe 에선 안 생긴다.

## flow-writer 대조

결과서 오프셋을 `.asm` 과 맞춰 봤다. **어긋난 곳 일곱 (1 ~ 7)** (고친 값을 이 문서에 씀) + 보탬 넷 (8 ~ 11) + 뒤에 보탠 어긋남 하나 (12 — run147 때, explainer 가 찾음):

1. **`ClassNameToVersion +0x6f RtlAllocateHeap`** (결과서 2절) → `+0x6f` 는 `MOV R8D,EDI` (크기 `0x200`). **call 은 `+0x74` (ret `+0x7b`)**.
2. **`GetStockObject(0x10)` "조각 `+0x1a6/+0x1ab`"** (결과서 1절 표 차례 10) → 조각 `FUN_18001fbc1` 의 **`+0x175 MOV ECX,0x10` · `+0x17a` call (ret `+0x181`) = `InternalCreateDialog +0x11ab`**. 조각 `+0x1a3` 은 `DeleteObject` 다 (`SelectObject` 실패 갈래). 결과서 8절 "조각 `+0x175`" 와는 맞음.
3. **`NtUserGetSystemMenu` "`+0x8cba7`"** (결과서 1절 차례 17) → 조각 `FUN_1800ab5c3 +0x176` (ret `+0x17d`) **= `InternalCreateDialog +0x8cba9`**.
4. **N14 "`+0x1382 MOV RDI,R13 (0)` → `+0xba5`"** (결과서 7절) → `+0x1382` 는 조각 `+0x351 MOV EDX,0x127`. **`MOV RDI,R13` 은 조각 `+0x382` (= `+0x13b3`)**, 그 뒤 `+0x385 JMP → +0xb9b` (거기서 `+0xba5` 로). 결과서 1절 차례 24 의 `+0x13b3` 과 맞음.
5. **TEB `+0x81c` 읽는 줄 "`UpdateWindow +0xd5`"** (결과서 4-2) → `UpdateWindow` 는 `+0x4f` 에서 끝나는 함수라 `+0xd5` 가 없다. `UpdateWindow` 는 `+0x9 ValidateHwnd` 안 (`ValidateHwnd +0xde`) 에서만 그 칸을 읽는다.
6. **`psi` 창 프로시저 표 식** (결과서 4-3) "`psi+0x1d8+8×(fnid−0x29a)` (A) · `psi+0x298+8×(…)` (W)" → `.asm` 은 `+0x3ec LEA RAX,[R9-0x29a]` · `+0x3f3 CMP RCX,[RDX+RAX*8+0x248]` · `+0x401 CMP RCX,[RDX+RAX*8+0x188]`. 식은 **`psi + 0x248 + 8k` · `psi + 0x188 + 8k`** (`k = fnid − 0x29a`) 이고, fnid `0x2a4` 일 때 칸이 `psi+0x298` · `psi+0x1d8` 이다 (결과서 6-1 의 식과 같음). W / A 이름은 `[추측]`.
7. **여분 블록이 비었을 때 넘어지는 줄** (결과서 1절 차례 16 · 4-4 "`+0x6af` 에서 접근 위반") → 둘로 나뉜다: 사본 `+0x128` 이 0 이면 **`+0x6ab MOV RCX,[RAX+0x8]`** 에서, 여분 블록은 있는데 `+8` (`pdlg`) 이 0 이면 (= `NtUserSetDialogPointer` stub 이 안 적음) **`+0x6af AND [RCX+0x18],…`** 에서 넘어진다. 그래서 `NtUserSetDialogPointer` 는 반환은 무관해도 몸이 꼭 있어야 한다 (PE_emulator 표에 반영).
8. **보탬 — 조각 `+0x359 SendMessageWorker(사본, 0x127, R14, 0, 0)`** (= `+0x138a`, 결과서 표에 없는 부름): `+0xb43 ~ +0xb7d` 에서 `psi+0x0` 비트 `0x80` 꺼짐 · `psi+0x1b5c` 비트 31 켜짐 · 비트 `0x20` 꺼짐 · `GetAppCompatFlags2(0x400)` 비트 1 꺼짐 · 사본 `+0x1f & 0xc0 ≠ 0x40` (자식 아님) 일 때만 (`+0xb7d JNZ →` 조각 `+0x346`). 메시지 이름은 `WM_CHANGEUISTATE` `[추측: 헤더 일반 지식 — 코드엔 숫자만]`. `psi` 를 0 으로 두면 `+0xb61 JZ` 에서 안 탄다 — 결과서 4-3 "0 이면 전부 건너뜀" 과 맞음.
9. **보탬 — `AllocLookasideEntry`**: `RtlAllocateHeap` call 은 `+0x72` (ret `+0x79`, 결과서 "`+0x20 ~ +0x56`" 범위 밖). 자유 목록 길 (`+0x20 ~ +0x2c`) 과 힙 길 (`+0x84 JNZ → +0x48`) 둘 다 `+0x51 memset(…, 0, [목록+0x28])` 를 지난다 — "0 으로 시작" 은 두 길 모두 맞음.
10. **보탬 — `SendMessageWorker` 클래스 `+4`**: 클래스 fnid 가 `0x29a` · `0x2a1 ~ 0x2aa` 가 아니면 (`+0x3db CMP AX,9` · `+0x3e1 CMP R9W,0x29a` · `+0x3e5 JNZ → +0x40f`) `psi` 표를 안 보고 곧장 길 B — stub 은 클래스 블록 `+4` 를 0 으로 둬도 된다 (최소 목록 4 에 반영). 사본 `+0x80` 이 0 이면 `+0x2ad JMP → +0x3b0` 뒤 `+0x3c5` 에서 `[0+4]` (결과서와 같음).
11. **이름**: Ghidra `Ordinal_2635` 는 PDB 이름 `IsWindowGdiScaledX` (rva `0x20910`, `pdb_aliases.py --where`) — 별칭 무리 아님. 문서는 PDB 이름을 씀. `RtlQueryAtomInAtomTable +0x2bd` 의 바이트 복사 대상은 같은 주소 이름 4개 (`.asm` `ALIASES(3)`: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`) — 하는 일로 적음.
12. **길 A 메시지 비트표 칸** (결과서 6-1 의 6 "길 A": "`gSharedInfo+0x28+0x10k` 의 비트표 포인터") → **결과서 6-1 식 틀림, 이 문서 4절 · 6절 표가 맞다.** `SendMessageWorker +0x731 ADD RAX,RAX` (k 를 둘로) · `+0x734 LEA RCX,[gSharedInfo+0x28]` · **`+0x73b MOV R10,[RCX+RAX*8+0x8]`** (= `gSharedInfo+0x30+0x10k`, 비트표 포인터) → `+0x740 TEST R10 · +0x743 JZ` · **`+0x749 CMP EDI,[RCX+RAX*8]`** (= `gSharedInfo+0x28+0x10k`, 최대 메시지 번호 dword) · `+0x74c JA` · `+0x74e ~ +0x761` 비트 검사. 결과서 4-3 표 (`+0x28+0x10×…` 최대 번호 · `+0x30+0x10×…` 비트표) 와 이 문서는 같다 — 6-1 한 줄만 `+0x28` 로 적었다. PE_emulator 쪽 결론 (0 으로 두면 길 A 가 "모든 메시지를 일꾼으로") 은 그대로.
- 그 밖 결과서 오프셋 · 점프 목적지 (`+0x140 → +0xd37` · `+0x219 → +0x10cd` · `+0x345 → +0xc90` · `+0x688 → +0x12c0` · `+0x694 → +0x12d4` · `+0x83e / +0x980 → +0xbf4` · `+0xa77 → +0xb9b` · `+0xb3d → +0x1358` · `+0xbf8 → +0x9c7` · `+0xc0d → +0xd4e` · `+0xd48 → +0x146` · `+0xd50 → +0xbb9` · `+0x10cf → +0xd4e` 등) 과 syscall 번호 20개는 `.asm` · `_syscalls.tsv` 와 **일치**.

## 앞 run 고쳐 읽기

틀린 것은 없고 `[추측]` · `[공백]` 이 정적으로 닫힌 것이라 "정정" 이 아니라 "고쳐 읽기" 로 각 문서 맨 위에 한 줄씩 넣었다.

- **run142**: 최소 길 표 차례 19 ~ 27 의 `[공백]` (어느 호출이 syscall 없이 풀리나 · `NtUserShowWindow` · `NtUserSetForegroundWindow` 조건) → `[정적]` 닫힘 — `WM_SETFONT` · `WM_INITDIALOG` 는 `SendMessageWorker` 직접 길 (항목 `+0x8` == TEB `+0x78` 등 4절), 표시 · 초점 · 앞으로 내기 반환은 안 봄. 5절 "대화상자 자료 … 쓰기 가능 메모리여야 함 `[추측: 어느 힙]`" → **`pUserHeap`** (`pdlg` 는 `AllocLookasideEntry`, 여분 블록은 되부름 `0x7b`). PE_emulator "최소 조건" 7 의 "`NtUserCreateWindowEx` 가 `ValidateHwnd` 를 통과하는 hwnd (핸들 표 항목 + 창 물체 + 대화상자 자료)" → 이 문서 "stub 이 채워야 하는 최소 목록" 1 ~ 6 으로 채움. 다음 요청 후보 N1 · N2 는 이 문서로 닫힘 (컨트롤 수 — 메시지 상자 템플릿 — 는 그대로 `[공백]`).
- **run131**: `NtUserGetAtomName` "반환 = 글자 수 (끝 NUL 제외 `[추측]`)" → **정수 원자는 `[정적]`** — 끝 NUL 을 쓰고 세지 않음 (`RtlQueryAtomInAtomTable +0x29f` · `+0x2c5`, `UserGetAtomName +0x60`), 커널은 `Length` 를 안 씀. 정수 원자 (`0xc000` 아래) 이름은 `#<십진>`. 표 원자 (`0xc000` 이상, `OleMainThreadWndClass` 같은) 의 진짜 글은 그대로 동적 3 (Q242). 4절 "`NtUserCreateWindowEx` 사슬만" 의 대화상자 몫 (인자 · 돌아온 뒤 검사) 은 이 문서 2절.

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (user32 안 올림) 이다.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 10 — ~~대기열 번호는 오케스트레이터가 붙임, Q273 VM GUI 세션에 얹을 꼴~~ → 대기열 **Q273** (1 ~ 10 을 출처로 보탬, run147 때 적음). test.exe 가 아니라 `DialogBoxParamW` 를 부르는 GUI 프로세스, 그 프로세스 조건, 유저 멈춤점):

1. `user32!ClassNameToVersion+0x22d` — `ax` (= 6?) · `du poi(@rbp-0x59) L7` (= `#32770`?) · `+0x235` 앞에서 `UNICODE_STRING.Length` 가 아직 0 인지.
2. `user32!InternalCreateDialog+0x63f` 직전 — `rdx` (= `0x8002`?) · `r8` 의 `LARGE_STRING` · 스택 17 인자 (스타일 · 부모 · `hInstance` · 끝 인자 `| 0x40000000`).
3. `InternalCreateDialog+0x658` 직후 `rax` = 사본 — `dq @rax L0x30` · `+0x12` · `+0x2a` · `+0x78` (= `DefDlgProcW`?) · `+0x80` · `+0xc8` (`0x1e`?) · `+0x128`, 항목 `dq poi(user32!gSharedInfo+8)+(hwnd&0xffff)*dwo(user32!gSharedInfo+10) L4` (`+0x8` == TEB `+0x78`? · `+0x10` == `poi(poi(@$teb+0x820))`?).
4. `ValidateDialogPwnd+0x4f` / `+0x56` — `rdx` = `pdlg` (0x58 B 0?) · 여분 블록 `+8` 이 뒤에 `pdlg` 로 · 사본 `+0x2a` (`0x2a4`?) · `+0x12` 비트 0.
5. `win32kfull!xxxCreateWindowEx+0x1446` / `+0x1a46` (커널 멈춤점, 프로세스 조건) — 되부름 `0x7b` 입력 4 B (`0x1e`?) 와 돌아온 포인터. `user32!__xxxClientAllocWindowClassExtraBytes+0x3a` 의 `rax`.
6. `SendMessageWorker+0xbc` (`r8` · `rax`) · `+0xc5` · `+0x100` · `+0x3f3` (길 A / B) · `+0x806` / `+0x956` (일꾼) · `+0x600` (길 B).
7. `DefDlgProcWorker+0xbf` — `rdx` = 앱 프로시저 = `[pdlg+0]`.
8. 컨트롤 고리 `InternalCreateDialog+0x975` 의 `rdx` (컨트롤 원자) · `VerNtUserCreateWindowEx+0x17c` 의 `rax` (comctl32 v6 매니페스트면 바꿔치기 글).
9. `+0xf7a` — 사본 `+0x1f` 비트 `0x10` · `pdlg+0x18` 비트 0, `UpdateWindow` 가 syscall 을 부르는지 (`+0x3e`).
10. TEB `+0x78` (스레드 ID?) · `+0x810` · `+0x840` / `+0x848` · `gSharedInfo+0x10` (항목 크기) 진짜 값 (run142 동적 1 · 2 와 겹침).

정적 (지금 도는 **sast/146 dialog-end** — 원문 물음 3 + 이번에 확인된 칸):

1. `DialogBox2` 고리 · `IsDialogMessageW` · `DispatchMessageWorker` (`+0xb6 TEST [사본+0x12],4` · `+0xee`) · `EndDialog` 뒤 syscall (`pdlg+0x18` 비트 0 · `+0x20` 결과값 겸 기본 단추 ID) · `_GetDlgItem` / `GetDlgItem` (사본 `+0x140` · 자식 연결 `+0x30` · `+0x38` · `+0x40?`) · `GetParentDialog +0x8`.
2. `_SetWindowLongPtr` / `_GetWindowLongPtr` (`DWLP_USER` = 여분 블록 `+0x10`) · `_GetWindowLong +0x50`.
3. A 판 갈림 — `SendMessageWorker +0x14c ~ +0x225` · `MessageTable[msg] & 0x100` · 확장 스타일 `0x80000000` (`InternalCreateDialog +0x5de`) · `pdlg+0x18` 비트 2. 모달 아닌 판 (`CreateDialogParam*`).
4. `DefWindowProcWorker` (`WM_GETDLGCODE` 가 앱에서 0 으로 올 때).
5. (필요하면) 길 A 일꾼 (`psi+0x320`) 을 채우는 쪽 — win32kfull `NtUserInitializeClientPfnArrays`.
6. 이번 `[공백]` ① ~ ⑥ (위 "정적으로 못 닫은 것").

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함). 디버거 여부를 보는 줄은 이번에 읽은 길에 없었다 `[정적: 읽은 줄 안에서]`.
- **스크립트 문제 없음** (결과서). 읽을 때 조심할 것: `InternalCreateDialog` 는 조각 (`FUN_18001fbc1` · `FUN_1800ab5c3` · `FUN_1800ab74b`) 과 "주인 없는 덩어리" 가 많아 `.asm` 에 `frag:FUN_…+0x…` 와 `(= InternalCreateDialog +0x…)` 두 기준이 섞인다 — 결과서의 어긋남 셋 (대조 2 · 3 · 4) 이 모두 이 두 기준을 섞은 데서 났다. 또 떨어진 범위 (`0x1800ab5a6` 등) 는 `+0x8ca16` 처럼 큰 오프셋으로 붙는다. WinDbg 에선 `user32!InternalCreateDialog+0x8ca16` 대신 주소 (`user32+0xab5a6`) 로 걸어야 할 수 있다 `[추측]` (run142 `MB_DlgProc` 메모와 같은 꼴) — 기록만, 하네스 제안 아님.
- 같은 때 다른 decompiler (sast/143 · 144 · 145 · 146) 가 `ms/` · `_workspace/sast/` 에서 일했다 — 이 문서는 그 파일을 안 건드렸다.

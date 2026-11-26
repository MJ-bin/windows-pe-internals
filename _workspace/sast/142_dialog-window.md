# 142 dialog-window — 정적 분석 요청

- 날짜: 2026-11-26
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q274a** (Q274 의 N1 · N2 를 앞으로 당긴 것 — Q274 를 그 자리에서 Q274a / Q274b 로 나눔). PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` — **원문을 먼저 끝까지 읽을 것** ("이미 아는 것" 절에 다시 안 해도 되는 줄과 asm 목록이 있음). 사용자 결정 2026-11-26 "다른 것보다 가장 먼저". **정적만.**
- **이번 요청은 원문의 물음 1 · 2 만.** 물음 3 (기본 단추 한 번으로 `EndDialog` 까지 — `DialogBox2` 고리 · `IsDialogMessageW` · `DispatchMessageWorker` · `EndDialog` 뒤 syscall · A 판 / 모달 아닌 판 갈림) 은 이 결과서가 나온 뒤 **sast/146 dialog-end** 로 따로 간다 (원문 "3 은 run 을 나눠도 됨"). 물음 2 의 칸 표는 물음 1 의 길 (`DefDlgProcW` · `SendMessageWorker` 포함) 에서 읽고 쓰는 칸까지만 — 3 의 길 칸은 144 가 보탠다.
- **먼저 읽는 것**: `_workspace/sast/139_user32-connect_result.md` (run142 — 대화상자 틀 · 핸들 항목 · TEB `+0x820/+0x828/+0x840` · 창 사본 칸 · "최소 조건" 목록 7 · 8), `_workspace/sast/128_user32-regclass_result.md` (run131 — `VerNtUserCreateWindowEx` · `ClassNameToVersion` · `NtUserGetAtomName` 반환 = 글자 수), `_workspace/sast/141_gdi-startup_result.md` (run144 — GDI 공유 핸들 표; DC · 글꼴 본문은 Q278 이라 이번 범위 밖).
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `user32.dll` — **이미 analyze 됨**. 다시 analyze 하지 말고 asm 은 있는 것 읽기, 없는 함수만 `^이름$` export.
  - `win32kfull.sys` · `win32kbase.sys` — **이미 analyze 됨** (run136 · run142 · run144 때). `NtUserGetAtomName` 과 그 아래 원자 조회 (1 (0)), `NtUserCreateWindowEx` 가 사본 · 대화상자 칸에 쓰는 줄 (1 (b) 의 "누가 `+0x128` 쪽에 적나"), `NtUserSetDialogPointer` (1 (b)), 창 만들기 중 되부름 (`xxxSendMessage` 류 — 1 (f), 이름 · 차례만) 에 필요한 함수만 `^이름$` export. 이름은 `pdb_aliases.py --where win32kfull.sys <이름>` 으로 먼저.
- 주의: 같은 시간에 다른 decompiler 가 **RpcEpMap · rpcrt4** (sast/143) 를 돈다 — 그쪽은 건드리지 않는다. 새 export 전에 `free -g`. scratchpad 임시 파일은 `s142/` 하위에.

## 왜 (요청한 쪽 말 요약)

- 악성 60 샘플이 `DialogBoxParamW` · `DialogBoxIndirectParamA` 에서 −1. 부모 hwnd 는 늘 0. 에뮬레이터 정책 (D156) = **보이지 않는 가짜 창**: 창은 만들어진 것으로 답하고, 창 프로시저는 진짜로 부르고, 기본 단추를 바로 누른 것으로 침. win32k 창 관리자는 안 옮김 → 알고 싶은 것은 커널 본문이 아니라 **user32 가 무엇을 읽고 무엇을 검사하나**.
- 지금 벽: `NtUserGetAtomName(0x8002)` 0 → `ClassNameToVersion` NULL → 커널 창 만들기를 안 부르고 −1. 그 뒤 벽 (`NtUserCreateWindowEx` 0 · `ValidateHwnd` · 사본 없음) 도 이어서 열어야 함.

## 알고 싶은 것 (원문 번호 그대로)

1. **창 만들기 길 (N1)** — `DialogBoxParamW(hInst, 템플릿, 부모 0, proc, lParam)` 이 `InternalCreateDialog` 에서 hwnd 를 돌려줄 때까지 차례대로.
   - (0) `NtUserGetAtomName(0x8002, USTR)` 진짜 값 — 반환 글자 수 · 버퍼 글 · 끝 NUL 을 쓰나 / 세나. 정수 원자 (`0xc000` 아래) 는 표를 안 찾고 `#십진수` (`#32770`, 6) 로 짓나. win32kfull `NtUserGetAtomName` + 그 아래 원자 조회 (ntoskrnl 쪽 `RtlQueryAtomInAtomTable` 류면 이름 · 줄만 — ntoskrnl 은 이미 analyze 됨, 필요하면 export).
   - (a) 가드 뒤 ~ `+0x63f` 앞: 템플릿 깃발마다 타는 GDI · 지표 부름과 **0 을 받으면 어디로 가나** (원문: 급하지 않음 — 표 한 장이면 됨).
   - (b) `+0x63f` 대화상자 창 `NtUserCreateWindowEx` 인자 (클래스 = `0x8002` 그대로인가 · 스타일 · `hInstance` · 깃발), 돌아온 뒤 user32 검사 — `ValidateHwnd` (`+0x653`) · `ValidateDialogPwnd` (`+0x667`). `NtUserSetDialogPointer` 인자 · 반환 검사. 대화상자 자료 블록을 누가 어디에 잡고, **누가 창 사본 `+0x128` 쪽에 그 포인터를 적나** (커널이면 stub 이 대신 적어야 함 — 커널 쪽 쓰는 줄).
   - (c) 컨트롤 고리 (`+0x975` 둘레): 항목 → 클래스 인자 꼴 · 컨트롤마다 syscall · 반환 검사 · 하나가 0 이면 전체 실패인가 (조건 깃발).
   - (d) `WM_SETFONT` · `WM_INITDIALOG` (`+0x9a7` · `+0x9ba` · `+0xa64`): `SendMessageWorker` 가 **syscall 없이 창 프로시저를 바로 부르는 조건** (읽는 사본 칸 · TEB `+0x78` 과 견주는 칸), 아니면 가는 syscall. `DefDlgProcW` 가 앱 프로시저를 어디서 꺼내 부르나.
   - (e) `+0xf7a` · `+0xf89` 둘레: `NtUserShowWindow` · `UpdateWindow` · 앞으로 내기 · 초점 · 기본 단추 표시 — 부르는 조건 · 반환을 보나. **NULL (→ −1) 로 나가는 가지 전부** (`InternalCreateDialog` 안 + 바깥 `DialogBoxIndirectParamAorW` 가 −1 을 만드는 줄).
   - (f) 창 만들기 도중 커널 되부름 (`WM_NCCREATE` · `WM_CREATE` …) 이 **뒤 단계에 꼭 필요한 상태를 만드나** — 되부름 없이 hwnd 만 돌려줘도 (a) ~ (e) 가 지나가나. 예/아니오 + 줄 (user32 쪽에서 되부름이 쓰는 칸을 나중에 읽는지로 판단 — 커널 되부름 본문은 이름만).
2. **창 사본 · 핸들 항목 칸 표 (N2)** — 1 의 길에서 user32 가 **읽는 / 쓰는** 칸 전부.
   - 핸들 항목: 크기 · 칸마다 통과 조건 · hwnd 에서 번호 · 고유 수를 꺼내는 식.
   - 커널 주소 → 사용자 주소: TEB `+0x828` · TEB `+0x820 → +0x0 / +0x8` (범위 검사인가).
   - 창 사본 (`tagWND`): 오프셋 · 크기 · 읽는 줄 · **0 이어도 되나 / 특정 값이어야 하나** — 스레드 정보 포인터 · 창 프로시저 · 스타일 · 부모 · 자식 · 다음 형제 · 컨트롤 ID · 클래스 · 대화상자 자료.
   - 대화상자 자료 블록: `+0x18` · `+0x20` 밖의 칸 (앱 프로시저 · 기본 단추 ID · 초점 창).
   - 볼 함수: `ValidateHwnd` · `HMValidateHandleNoSecure` · `ValidateDialogPwnd` · `SendMessageWorker` · `DefDlgProcW` (길에 나오면 `_GetChildControl` · `_NextControl`). 칸 이름은 PDB 타입 (`tagWND` · `tagDLG` 류 — user32 / win32kbase PDB 에 있으면 이름 · 오프셋, 없으면 "PDB 에 없음" + `[추측: 이름]`).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| user32 | `DialogBoxIndirectParamAorW` · `InternalCreateDialog` (있음 — 가드 뒤 전부) · `VerNtUserCreateWindowEx` · `ClassNameToVersion` (있으면) | 1 | 1 (a)(b)(c)(e) |
| user32 | `ValidateHwnd` · `HMValidateHandleNoSecure` · `ValidateDialogPwnd` · `SendMessageWorker` · `DefDlgProcW` · `CreateDlgFont` (있음) · 길에 나오는 `_GetChildControl` · `_NextControl` · `DlgSetFocus` · `xxxCheckDefPushButton` | 1 | 1 (b)(d)(e) · 2 |
| win32kfull | `NtUserGetAtomName` (+ 원자 조회 한 단) · `NtUserSetDialogPointer` · `NtUserCreateWindowEx` → 사본 · 대화상자 칸 쓰는 줄 (이름 · 줄만) | 0~1 | 1 (0)(b)(f) |
| win32kbase | 핸들 항목 · 사본 주소 바꾸기에 필요한 것만 | 0 | 2 |

- 범위: user32 가 읽고 검사하는 것까지. win32k 창 관리자 본문 · 그리기 · GDI · 글꼴 (Q278) · TEB `+0x820` 채우기 (Q274b N3) · 지표 (Q274b N5) 는 이름만.
- 분량이 크면 원문 차례 **1 (0) → 1 (b) → 1 (e) → 1 (f) → 1 (c) → 1 (d) → 2 → 1 (a)** 로 결과서에 먼저 쓰고 나머지는 "다음 요청으로" 라고 적어도 된다.

## 받고 싶은 것

- 결과서 `_workspace/sast/142_dialog-window_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (1 (0) ~ (f) · 2 마다 한 줄).
  3. 표: (1) run142 3절 꼴 **"최소 길 지도"** — `차례 · 부르는 것 / syscall (번호 — win32u _syscalls.tsv) · 조건 · 반환을 user32 가 보나 · 0 이면 · syscall 없이 읽는 칸 · 줄` · (2) **칸 표** — `구조체 · 오프셋 · 크기 · 이름 (PDB / [추측]) · 읽는 줄 · 쓰는 줄 · 0 가능 여부 / 있어야 할 값` · (3) **NULL (→ −1) 가지 표**.
  4. PDB GUID 확인 줄 (export 한 모듈 — 이미 쓰는 프로젝트와 같은지).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** — 원문 "답이 오면 쓰는 곳" 마다 한 줄: `nt_user_get_atom_name.cpp` (`0x8002` 가지 — 반환 · 쓸 글) · `nt_user_create_window_ex.cpp` (`0x8002` 받기 · hwnd 마다 항목 · 사본 — 채울 칸) · `csr_user_connect_stub.cpp` (핸들 표 블록) · 새 stub (`NtUserSetDialogPointer` · `NtUserShowWindow` 류 — 반환 값). 그리고 **"stub 이 채워야 하는 최소 목록"** (run142 목록 7 · 8 을 채운 판) 과 **"되부름이 꼭 필요한가" 한 줄**.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (대기열 Q273 VM GUI 세션에 얹을 꼴 — 멈춤점 `user32!함수+0x…`) 과 **"다음 요청 후보"** (sast/146 dialog-end 에 넘길 것 — 3 의 길에서 볼 칸 · 함수).

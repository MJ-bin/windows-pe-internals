# 142 dialog-window — 정적 분석 결과

- 요청서: `_workspace/sast/142_dialog-window.md` (원문 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` 를 먼저 끝까지 읽음)
- 대기열: Q274a. 정적만. 물음 1 · 2 만 (물음 3 은 **sast/146 dialog-end** 로 — 코디네이터가 번호를 144 → 146 으로 고쳐 줌).
- 스크립트 문제: 없음 (`export` · `refs` 모두 `SCRIPT ERROR` 없이 끝남).
- PDB GUID (`ms/sym/*.pdb.info.txt`, 전부 이미 쓰던 프로젝트와 같음 — sast/139 머리와 같은 값):
  - ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준과 같음 (빌드 26100.9457 맞음)
  - user32 `CDB947D1B96B87977915DE04CF54279C1` · win32kfull `5CD57181BCFDC5AE8F4819BEB1196F091` · win32kbase `D22DFDA3D54B87E6BD0567A57877904C1`
- 다시 analyze 한 모듈: 없음. 전부 `-readOnly` export · refs (user32 · win32kfull · win32kbase · ntoskrnl). rpcrt4 (sast/143) 는 안 건드림. **win32kfull · win32kbase 는 이 결과서 쓰기 전에 다 닫음 (sast/145 가 열어도 됨).**
- 새로 뽑은 것 (depth 0, 로그 `ms/ghidra/<모듈>.export.s142?.stdout`):
  - user32 (asm · c): `SkipSz` `_GetChildControl` `_NextControl` `_IsDescendant` `DlgSetFocus` `xxxCheckDefPushButton` `UpdateWindow` `GetActiveWindow` `GetNonChildAncestor` `DefDlgProcWorker` `DefDlgProcA` `GetDlgItem` `GetDlgCtrlID` `GetParent` `GetParentDialog` `GetWindowLongW` `_GetWindowLong` `RepositionRect` `Ordinal_2635`(= `IsWindowGdiScaledX`) `UserCallWinProc` `UserCallWinProcCheckWow` `DefWindowProcWorker` `DefWindowProcW` · `__xxxClientAllocWindowClassExtraBytes` · `SendMessageInternal` · `AllocLookasideEntry` `InitLookaside` `FreeLookasideEntry` `UserCallDlgProcCheckWow` `GetPrimaryMonitor` · `SetThreadDpiAwarenessContextFromWindow` `GetDialogMonitor`
  - win32kfull: `NtUserGetAtomName` `NtUserSetDialogPointer` `NtUserSetDialogSystemMenu` (+ depth 1: `SetDialogPointer` `xxxSetDialogSystemMenu` `ValidateReceivingHwnd` …) · `unsafe_cast_fnid_zero_to_PDIALOG` `NtUserCreateWindowEx` `xxxCreateWindowEx` `SetOrClrWF` · `xxxClientAllocWindowClassExtraBytes` `GetClientExtraBytesTotalSize`
  - win32kbase: `UserGetAtomName` `UserGetAtomNameFromAtomTable` (+ depth 1) · `HMAllocObject` `HMAllocObjectEx`
  - ntoskrnl: `RtlQueryAtomInAtomTable` (+ depth 1, 13개 — `RtlpLockAtomTable` 포함)
  - `refs`: user32 `0x4f770` (콜백 표 칸 확인용)
  - 이미 있던 asm (읽기만): user32 `InternalCreateDialog` `VerNtUserCreateWindowEx` `ClassNameToVersion` `ValidateHwnd` `HMValidateHandleNoSecure` `ValidateDialogPwnd` `SendMessageWorker` `SendMessageW` `DefDlgProcW` `DialogBoxParamW` `DialogBoxIndirectParamAorW` `InternalDialogBox` `CreateDlgFont` `IsWindow`.
- 버전 대조: user32 는 "알려진 사실" 표에 줄이 없어 sast/139 · 128 에 적힌 줄과 맞춤 — 전부 일치:
  `InternalCreateDialog +0x13c CMP [TEB+0x78]` · `+0x63f/+0x644 VerNtUserCreateWindowEx` · `+0x653/+0x658 ValidateHwnd` · `+0x667/+0x66c ValidateDialogPwnd` · `ValidateDialogPwnd +0x4f/+0x56 NtUserSetDialogPointer` · `+0x9a7` `+0x9ba SendMessageW` · `+0xa64/+0xa69 SendMessageWorker` · `+0xf7a/+0xf81 NtUserShowWindow` · `+0xf89 UpdateWindow` · `ClassNameToVersion +0x226 NtUserGetAtomName` · `ValidateHwnd` 의 항목 칸 줄 (`+0x86 +0xa4 +0xae +0xb4 +0xc6`) · win32u 번호 (`_syscalls.tsv`) 도 139 와 같음.
- PDB 에 `tagWND` · `tagDIALOG` · `tagHANDLEENTRY` 같은 **형식(type) 이 없음**: `pdb_struct.py --list` 로 user32 · win32kbase · win32kfull 모두 0개, ntoskrnl 은 `_TEB` 만 (`+0x78 Win32ThreadInfo void*`, `+0x800 Win32ClientInfo ?[496B]`). 아래 칸 이름은 전부 `[추측: 이름]` — 오프셋 · 읽는 줄은 `[정적]`.
- 오프셋 쓰는 법: 부르기는 `call/ret`, 점프 목적지는 "목적지 − 함수 시작". `InternalCreateDialog` 는 조각 (`FUN_18001fbc1` 등) 이 많아 조각 줄은 `(= InternalCreateDialog +0x…)` 로 바꾼 값만 씀.

---

## 먼저 읽는 한 줄 답

- **1 (0) `NtUserGetAtomName(0x8002, USTR)`**: 진짜 값은 **반환 6**, 버퍼에 **`L"#32770"` + 끝 NUL** (14 B) 을 쓴다. 정수 원자 (`0xc000` 아래, 0 제외) 는 **원자 표를 안 찾고** `L"#%u"` 로 지어 준다 (`ntoskrnl!RtlQueryAtomInAtomTable +0x277~+0x298`, 서식 글자는 파일에서 읽음). 반환 = 글자 수 (끝 NUL 제외, `바이트 >> 1`). 커널은 `UNICODE_STRING.Length` 를 **안 쓴다** — user32 가 `반환 × 2` 로 직접 쓴다 (`ClassNameToVersion +0x232/+0x235`). 원자 0 이면 0 (STATUS_INVALID_PARAMETER).
- **1 (a)** 가드 뒤 ~ `+0x63f` 앞: GDI · 지표 부름 대부분은 **0 을 받아도 NULL 로 안 나간다** (글꼴 없음 · 글자 단위 기본값으로 넘어감). NULL 로 나가는 앞쪽 가지는 **`CreateCompatibleDC(0)` = 0 · 템플릿에 메뉴가 있는데 `LoadMenuW` = 0 · 새 앱 + 스타일 `0x8000` 비트 · 모니터가 NULL** 뿐. 크래시 칸: `gSharedInfo+0x18` (pDispInfo) NULL · 글자 단위 포인터.
- **1 (b)** `+0x63f`: 클래스 = **`0x8002` 그대로** (템플릿에 클래스 이름이 없을 때; 둘째 인자 = 준 클래스, 셋째 = `ClassNameToVersion` 결과 — 바꿔치기 없으면 같은 `0x8002`). 돌아온 뒤 **`ValidateHwnd` → `ValidateDialogPwnd` 둘 다 0 이면 NULL**. `ValidateDialogPwnd` 가 대화상자 자료 블록 (`pdlg`, 0x58 B, user32 가 자기 힙에서 0 으로 채워 잡음) 을 만들고 `NtUserSetDialogPointer(hwnd, pdlg)` 를 부른다 — **반환은 안 봄**. 사본 `+0x128` (= 창 여분 바이트 포인터) 은 **커널이 적는다**: 창 만들기 중 **사용자 모드 되부름 `0x7b` (`xxxClientAllocWindowClassExtraBytes`)** 이 user32 힙에서 0 채운 블록을 받아 오고 `xxxCreateWindowEx +0x1a46` 이 `+0x128` 에 넣는다. `pdlg` 포인터 자체는 `NtUserSetDialogPointer` 가 그 블록의 `+8` 에 적는다.
- **1 (c)** 컨트롤마다 `VerNtUserCreateWindowEx` 1번 (= `NtUserCreateWindowEx` 1번 + 클래스가 원자일 때 `NtUserGetAtomName` 1번) + `SendMessageW(WM_SETFONT)` (글꼴 있을 때만) + `SendMessageW(WM_GETDLGCODE)`. 하나가 NULL 이면 **전체 NULL** (대화상자 `NtUserDestroyWindow`) — 단 **`DS_NOFAILCREATE` (스타일 `0x10`) 가 있으면 그 컨트롤만 건너뜀**. 서수 클래스 (`0xffff, 번호`) 는 `psi+0x364+2×(번호&0xff7f)` 의 원자로 바뀌고 번호가 `0x19` 이상이면 실패 취급.
- **1 (d)** `SendMessageWorker` 가 syscall 없이 창 프로시저를 바로 부르는 조건 = ① `msg` 가 `0x3e0~0x3e8` 아님 ② **핸들 항목 `+0x8` (소유 스레드 ID) == TEB `+0x78`** ③ 사본 `+0x12` 비트 `0x4` = 0 ④ `[[TEB+0x820]+0x10] | [TEB+0x838]` 의 `0x2020` 비트 = 0 ⑤ 사본 `+0x80` (클래스 포인터) ≠ 0 ⑥ (글자 종류가 보내는 쪽과 창이 같을 때) — 안 맞으면 `NtUserMessageCall` (`0x1005`) 이나 `gapfnScSendMessage` 길. `DefDlgProcW` → `DefDlgProcWorker` 가 **`[[사본+0x128]+8]` (= `pdlg`) 의 `+0x0`** 에서 앱 프로시저를 꺼내 `UserCallDlgProcCheckWow` 로 부른다.
- **1 (e)** `ShowWindow` · `UpdateWindow` · `SetForegroundWindow` · `SetFocus` **반환은 전부 안 본다**. NULL (→ −1) 로 나가는 가지 전부는 7절 "NULL 가지 표" (N1~N15 + 바깥 줄).
- **1 (f)** 되부름 없이 hwnd 만 돌려줘도 (a)~(e) 가 **지나간다 — 단 하나만 stub 이 대신해야 함: 되부름 `0x7b` 이 하던 일 (사본 `+0x128` 에 0 채운 여분 블록).** 안 하면 `InternalCreateDialog +0x69a~+0x6ab` 에서 널 읽기. `WM_NCCREATE`/`WM_CREATE` 같은 **창 프로시저 되부름은 뒤 단계가 읽는 칸을 만들지 않는다** (`DefDlgProcWorker` 의 첫 일 `NtUserSetWindowFNID` + `ValidateDialogPwnd` 는 `InternalCreateDialog` 가 어차피 직접 한다). 컨트롤 쪽은 `[추측]` (아래 (f) 절).
- **2 칸 표**: 핸들 항목 = `gSharedInfo+0x10` 바이트 (읽는 칸 `+0x0 +0x8 +0x10 +0x18 +0x19 +0x1a`), 번호 = `hwnd & 0xffff`, 고유 수 = `(hwnd >> 16) & 0x7fff` (`0x7fff` 는 만능). TEB `+0x828` 은 **더하기만** (범위 검사 없음), TEB `+0x820` 은 **첫 qword 와 항목 `+0x10` 이 같은지만** 봄 (다르면 `NtUserMapDesktopObject` 로 빠짐). 사본은 `+0x128` 까지 (최소 `0x130` B). 자세한 칸은 4절.

---

## 1. 최소 길 지도 — `DialogBoxParamW(hInst, 이름 글, 0, proc, lParam)` → `InternalCreateDialog` 가 hwnd 를 돌려줄 때까지

번호는 `ms/disassemble/win32u/_syscalls.tsv`. 오프셋은 `.asm` 위치 순서 (실행 순서와 거의 같음 — 갈림길은 "조건" 칸). "반환을 user32 가 보나" 는 반환값 쓰임 (예 = 0 / 비교에 씀).

| 차례 | 부르는 것 / syscall | 조건 | 반환을 user32 가 보나 · 0 이면 | syscall 없이 읽는 칸 | 줄 `[정적]` |
|---|---|---|---|---|---|
| 1 | `DialogBoxParamW` → `FindResourceExW(hInst, 5 (RT_DIALOG), 이름, 0)` → `LoadResource` | 템플릿이 자원일 때 | 0 → 둘 다 **−1** | — | `DialogBoxParamW +0x2a/+0x31` · `+0x39 JZ` · `+0x41/+0x48` · `+0x53 JZ` (RBX=−1 은 `+0x1d`) |
| 2 | `DialogBoxIndirectParamAorW` → `GetSizeDialogTemplate` | 항상 | 0 → **−1** | 템플릿 | `+0x20/+0x25` · `+0x27 JNZ` · `+0x29 OR RAX,-1`. 깃발 `(flags & 2) \| 1` 를 `InternalDialogBox` 에 (`+0x4a~+0x56`) |
| 3 | `InternalDialogBox` | 부모 0 이면 `+0x30 JNZ` 안 탐 | — | `gpsi+0x7b4` (0 이면 대기 커서 건너뜀, `+0x3e/+0x45`) | `InternalDialogBox +0x2d~+0x45` · `+0x68/+0x6b/+0x70 → InternalCreateDialog` |
| 4 | `InternalCreateDialog` 입구: `GetDpiForSystem` · `gfServerProcess` · TEB `+0x8e8` 읽기 | 항상 | DPI 문맥 (`[RBP-0x7c]` 깃발) — 0/기본이면 켜지지 않음 | TEB `+0x8e8` (0 이면 `gDefaultDpiContext`) | `+0x7f/+0x84` · `+0x8a~+0x12a` |
| 5 | **가드** `NtUserGetThreadState(0xe)` (`0x1000`) | TEB `+0x78` == 0 일 때만 | 0 → **NULL** (`+0xd4e`), ≠0 → 계속 (뒤에 TEB+0x78 다시 안 읽음) | TEB `+0x78` | `+0x133/+0x13c/+0x140 JZ → +0xd37` · `+0xd39/+0xd40` · `+0xd45` · `+0xd48 JNZ → +0x146` · `+0xd4e XOR EAX,EAX` |
| 6 | 템플릿 읽기 (복사본) · `RtlGetExpWinVer(hInst)` | `hInst==0` 이면 `GetModuleHandleW(0)` (`+0x1005`) 도 | 새 앱 (`≥0x400`) / 옛 앱 갈림, NULL 아님 | 템플릿 · exe 머리 · TEB `+0x810` | `+0x146~+0x219` (표 (a) 참고) |
| 7 | (메뉴가 있을 때) `LoadMenuW` | 템플릿 메뉴 이름 ≠ 빈 글 | 0 → **NULL** | — | `+0x8ca16/+0x8ca1b` · `+0x8ca28 JZ → +0xd4e` |
| 8 | `GetDialogMonitor(부모, 1)` → (부모 0) `NtUserGetForegroundWindow` (`0x1037`) → `GetPrimaryMonitor` | 항상 (`DS_CENTERMOUSE` + `psi+0x7b4≠0` 이면 `NtUserGetCursorPos` `0x1437` + `_MonitorFromPoint`) | 0 이면 나중에 `_MonitorFromRect` 로 한 번 더 — 그것도 0 이면 **NULL** | **`gSharedInfo+0x18` → `[+8]`** (NULL 이면 접근 위반) · `gSharedInfo+0x20` | `+0x316/+0x31b` · `GetDialogMonitor +0x60/+0x67` · `+0x74~+0x7e` · `GetPrimaryMonitor +0x0~+0x1c` |
| 9 | `CreateCompatibleDC(0)` (gdi32 → `NtGdiCreateCompatibleDC`) | 항상 | 0 → **NULL** | — | `+0x333/+0x33a` · `+0x345 JZ → +0xc90` |
| 10 | (`DS_SETFONT`) `CreateDlgFont` · (`DS_FIXEDSYS` + 새 앱) `GetStockObject(0x10)` · `GetStockObject(0xd)` 대체 · `SelectObject` · `GdiGetCharDimensions` · `DeleteDC` | 스타일 비트 | 전부 0 이어도 **지나감** (글꼴 없음, 글자 단위는 DPI 서버 정보 `+0x20/+0x24` 기본값) | `GetDpiServerInfoForCurrentThread` 포인터의 `+0x20` `+0x24` | `+0x386/+0x38b` · `+0x3b0/+0x3b5` `+0x3bb/+0x3c0` · `+0x3e9/+0x3f0` · `+0x409/+0x410` · 조각 `+0x1a6/+0x1ab` (`GetStockObject(0x10)`) |
| 11 | `_AdjustWindowRectExForDpi` · `RepositionRect(모니터, 사각형, 스타일, 확장스타일)` (`DS_CENTER` 면 `GetMonitorWorkRect`) | 항상 | 반환 안 봄 | 모니터 물체 사각형 (139 와 같음) | `+0x467/+0x46c` · `+0x533/+0x538` |
| 12 | **`VerNtUserCreateWindowEx` (대화상자)** → `ClassNameToVersion` → **`NtUserGetAtomName(0x8002)`** (`0x109c`) → **`NtUserCreateWindowEx`** (`0x106f`) | 클래스 인자가 원자 `0x8002` (템플릿 클래스 빈 글) | 0 → **NULL** (아래 NULL 표). 대답 6 이면 `RtlFindActivationContextSectionString` NOT_FOUND → 원자 그대로 (run131) | `ClassNameToVersion` 이 `pUserHeap` 에서 0x200 B 잡음 | `+0x575 CMP [R10],0` · `+0x57f MOV R10D,0x8002` · `+0x63c MOV RDX,R10` · `+0x63f/+0x644` · `VerNtUserCreateWindowEx +0x177` · `ClassNameToVersion +0x226` · `VerNtUserCreateWindowEx +0x1e1 MOV RDX,RDI` `+0x1c1 MOV R8,R14` `+0x22f/+0x236` |
| 13 | (커널 안, user32 가 안 봄) **사용자 모드 되부름 `0x7b`** → user32 `__xxxClientAllocWindowClassExtraBytes` | 클래스 여분 바이트 크기 ≠ 0 (대화상자 클래스는 `DLGWINDOWEXTRA` 30 → 사본 `+0xc8`) | 사본 `+0x128` 에 0 채운 블록 포인터가 들어감 | — | win32kfull `xxxCreateWindowEx +0x9c9/+0x9ce` · `+0x9d0 JNZ → +0x1444` · `+0x1446/+0x144b` · `+0x147c CMP [사본+0x128]` · `+0x1a46 MOV [사본+0x128],RDI`; user32 `__xxxClientAllocWindowClassExtraBytes +0x10 MOV R8D,[RCX]` · `+0x13 LEA EDX,[RAX+8]` (0 채움 깃발) · `+0x1d RtlAllocateHeap(pUserHeap)` · `+0x3a NtCallbackReturn` |
| 14 | `ValidateHwnd(hwnd)` | 항상 | 0 → **NULL** | 핸들 항목 · TEB `+0x820/+0x828/+0x840/+0x848` · `psi+0x8` | `+0x650~+0x65e` · `ValidateHwnd +0x0~+0x14f` |
| 15 | `ValidateDialogPwnd(사본)` → `AllocLookasideEntry(&DialogLookaside)` → **`NtUserSetDialogPointer(hwnd, pdlg)`** (`0x1552`) | `사본+0x12` 비트 0 이 0 일 때 (켜져 있으면 곧장 1) | `ValidateDialogPwnd` 0 → **NULL**. **`NtUserSetDialogPointer` 반환은 버림** (`+0x56 NOP → +0x5b JMP → +0xf MOV EAX,1`) | 사본 `+0x12` · `+0xc8` (≥ `0x1e`) · `+0x2a` | `+0x664~+0x66e` · `ValidateDialogPwnd +0x6 TEST [RCX+0x12],1` · `+0x1b CMP [RCX+0xc8],0x1e` · `+0x29 TEST [RCX+0x2a],0xc000` · `+0x38/+0x3f` · `+0x44 TEST RAX` · `+0x49 MOV RCX,[RBX]` · `+0x4f/+0x56` |
| 16 | `pdlg` 칸 채우기 (사본 `+0x128` → `[+8]`) | 항상 | — | 사본 `+0x128` (널이면 `+0x6af` 에서 접근 위반) | `+0x69a~+0x72c` · 조각 `+0x26b~+0x287` (DPI 켜졌을 때 `+0x18\|=8`, `+0x3c`=DPI) |
| 17 | (스타일 `WS_SYSMENU` = 사본 `+0x1e` 비트 3) `NtUserSetDialogSystemMenu` (`0x1553`) 또는 `NtUserGetSystemMenu` (`0x105b`) | `+0x1e & 7 == 0` 이면 앞의 것 | 반환 안 봄 | 사본 `+0x1e` | `+0x68e~+0x694 JNZ → +0x12d4` · 조각 `+0x2a3~+0x2ba` (`+0x12d4~+0x12eb`) · `+0x8cba7` `NtUserGetSystemMenu` |
| 18 | (EX 템플릿 도움말 번호 ≠ 0) `NtUserSetWindowContextHelpId` (`0x1595`) | `[RBP-0x5c]` ≠ 0 | 반환 안 봄 | — | `+0x683~+0x688 JNZ → +0x12c0` · 조각 `+0x292` |
| 19 | (글꼴 있고 앱 프로시저 있을 때) `SendMessageWorker(사본, WM_SETFONT 0x30, hfont, 0, 0)` | `R12 & 4` · `dlgproc ≠ 0` | 반환 안 봄 | 사본 `+0x128` (`[+8]+0x30` 에 글꼴 저장) | `+0x72f TEST AL,R12B → +0x12f0` · 조각 `+0x2bf~+0x2f5` (`+0x12f0~+0x1326`) |
| 20 | **컨트롤 고리** (`cdit` 번) — 4·5절의 (c) 참고: `VerNtUserCreateWindowEx` (→ `NtUserGetAtomName` + `NtUserCreateWindowEx`) · `SendMessageW(ctl, 0x30)` · `SendMessageW(ctl, 0x87)` | 컨트롤 수만큼 | 0 → 아래 NULL 표 | `psi+0x364+2i` (원자 표) | `+0x975/+0x97a` · `+0x9a7/+0x9ac` · `+0x9ba/+0x9bf` · `+0x980 JZ → +0xbf4` |
| 21 | `WM_INITDIALOG` (0x110): `SendMessageWorker(사본, 0x110, hwndFocus, lParam, 0)` → 직접 길이면 `DefDlgProcW` → 앱 프로시저 | 앱 프로시저 ≠ 0 (`+0xa40 CMP [RBP+0x100],R13`) | **반환을 봄**: 0 이면 기본 초점 건너뜀 (`+0xa7d TEST ESI,ESI; JZ → +0xb33`) | 아래 (d) 의 칸 | `+0xa57~+0xa64/+0xa69` · `+0xa6f/+0xa74 HMValidateHandleNoSecure` (0 → 뒤쪽 NULL) |
| 22 | 기본 초점: `_GetChildControl` → `_NextControl` → `DlgSetFocus` (= `SendMessageW(0x87)` + `NtUserSetFocus` `0x104c`) → `xxxCheckDefPushButton` | 반환 ≠ 0 · 대화상자 자료 `+0x18` 비트 0 = 0 · 스타일 `DS_CONTROL` (사본 `+0x1d` 비트 2) 아님 | 반환 안 봄 | 사본 `+0x30` (부모) · `+0x38` (첫 자식) | `+0xa7d~+0xb33` · `DlgSetFocus +0x14 SendMessageW` `+0x3b JMP [NtUserSetFocus]` |
| 23 | `IsWindow(hwnd)` | 항상 | 0 이면 TEB `+0x810 ≥ 0x400` 일 때 **NULL**, 아니면 hwnd 그대로 | 핸들 항목 · 사본 `+0x2a` 부호 | `+0xb36/+0xb3b` · `+0xb3d JZ → +0x1358` (조각 `+0x327`) |
| 24 | (`DS_SETFOREGROUND` = 스타일 `0x200`) `NtUserSetForegroundWindow` (`0x155b`) → `IsWindow` | `local_118 & 8` | 반환은 안 봄, **`IsWindow` 0 이면 NULL** | — | 조각 `+0x366~+0x37a` (`+0x1397~+0x13ab`) · `+0x13b3 MOV RDI,R13` |
| 25 | (스타일 `WS_VISIBLE` 이었고, 아직 안 끝났고, 사본 `+0x1f` 비트 `0x10` 꺼짐) `NtUserShowWindow(hwnd, 1)` (`0x1052`) → `UpdateWindow` (사본 `+0x88` · `+0x11` · `+0x38` 이 다 0 이면 **syscall 없이 1**, 아니면 `NtUserUpdateWindow` `0x15c8` 꼬리호출) | 조건 셋 모두 | **둘 다 반환 안 봄** | `[[사본+0x128]+8]+0x18` 비트 0 · 사본 `+0x1f` | `+0xf53~+0xf8e` · `+0xf7a/+0xf81` · `+0xf89/+0xf8e` · `UpdateWindow +0x13~+0x3e` |
| 26 | 끝 검사 `HMValidateHandleNoSecure(hwnd)` → 반환 = hwnd 또는 0 | `param_7 & 4` 가 0 일 때 | 0 → **NULL** | 핸들 항목 · TEB `+0x820` | `+0xba5/+0xbad` · `+0xbb0~+0xbb6` |

---

## 2. (0) `NtUserGetAtomName` — 줄 근거 `[정적]`

- win32kfull `NtUserGetAtomName` (`ms/disassemble/win32kfull/NtUserGetAtomName.asm`): `+0x22` 공유 임계 구역 · `+0x28~+0x35` 포인터를 `MmUserProbeAddress` 로 자름 · `+0x3a MOV EAX,[RBX]` (`Length | MaximumLength<<16`) · `+0x44 MOV RBX,[RBX+8]` (Buffer) · `+0x5c ProbeForWrite(Buffer, MaximumLength, 2)` · `+0x6e MOVZX R8D,word [RSP+0x72]; SHR R8D,1` (= **MaximumLength/2 글자**) · `+0x7d/+0x84 UserGetAtomName(atom, Buffer, 글자수)` · `+0x89 MOV EBX,EAX` → 그대로 반환. **`Length` 를 쓰는 줄이 없다.**
- win32kbase `UserGetAtomName` (`ms/disassemble/win32kbase/UserGetAtomName.asm`): `+0x2d MOV RCX,[세션 상태+0xa658]` (세션 원자 표) · `+0x34 LEA EAX,[RBX+RBX]` (바이트 한도 = 글자수×2) · `+0x4a/+0x51 RtlQueryAtomInAtomTable(표, atom, 0, 0, Buffer, &한도)` · `+0x5a JS` (실패면 `+0x75/+0x77 SetLastNtError`, 반환 0) · `+0x5c MOV EBX,[RSP+0x50]; +0x60 SHR EBX,1` → **반환 = 바이트 ÷ 2**.
- ntoskrnl `RtlQueryAtomInAtomTable` (rva `0x9b2b70`, `ms/disassemble/ntoskrnl/RtlQueryAtomInAtomTable.asm`): `+0x56/+0x5b RtlpLockAtomTable` (실패 시 `+0x35c` STATUS_INVALID_PARAMETER) · `+0x6c MOV EAX,0xc000; +0x71 CMP R15W,AX; +0x75 JC → +0x246` (**`0xc000` 미만 = 정수 원자, 표 안 찾음**) · `+0x246 TEST R15W; JZ → +0x2d4` (원자 0 → `0xc000000d`) · `+0x250` 성공 상태 · `+0x25d/+0x268` (참조 수 · 고정 수 포인터가 있으면 1 로 채움) · `+0x277 MOV [RSP+0x20],R15D; +0x27c LEA R9,[0x140afbf20]; +0x290 LEA RCX,[RSP+0x88]; +0x298 _snwprintf_s(버퍼, 16, -1, 서식, 원자)` — 서식 글자 `0x140afbf20` 를 `ms/bin` 에서 읽으니 **`#%u`**. `+0x29d~+0x29f` 바이트 = 글자수×2 · `+0x2a5 CMP ESI,EDI; JNC → +0x2ef` (한도가 모자라면 `한도−2` 로 자르고 성공 상태 그대로, 한도 < 2 면 `+0x2e5` BUFFER_TOO_SMALL) · `+0x2bd` 복사 · **`+0x2c5 MOV word [R13+RDI*2],R12W` = 끝 NUL 씀 (세지는 않음)** · `+0x2d0 MOV [RAX],ESI` 바이트 수 돌려줌.
- user32 쪽: `ClassNameToVersion +0x6a EDI=0x200` · `+0x6f RtlAllocateHeap(pUserHeap, 0, 0x200)` (Buffer) · `+0x21b MOV [RBP-0x5f],DI` (MaximumLength = `0x200`) · `+0x222 MOV [RBP-0x59],RSI` (Buffer) · `+0x226/+0x22d NtUserGetAtomName` · `+0x232 ADD AX,AX; +0x235 MOV [RBP-0x61],AX` (**Length = 반환×2 를 user32 가 씀**) · `+0x239 JNZ` (0 이면 `+0x23f XOR ECX,ECX` → NULL). user32 는 **끝 NUL 을 안 읽는다** (Length 로 `RtlInitUnicodeString` 없이 `RtlFindActivationContextSectionString` 에 넘김).
- 그래서 `0x8002` → **6**, 버퍼 `23 00 33 00 32 00 37 00 37 00 30 00 00 00` (`#32770` + NUL). 한도 14 B 이상이면 그대로, user32 는 512 B 를 줌.
- 요청서의 "ntoskrnl 쪽 이름 · 줄만" → 위로 닫음. 글 원자 (`0xc000` 이상) 의 진짜 이름은 Q242 (동적) 그대로.

## 3. (b) `NtUserCreateWindowEx` 인자 · 검사 · 대화상자 자료

**3-1. 인자 (`InternalCreateDialog +0x5de~+0x63f`, `VerNtUserCreateWindowEx +0x1c1~+0x22f`)** `[정적]`
- 확장 스타일 = `(param_7 & 2 ? 0x80000000 : 0) | 템플릿 확장 스타일 (ESI)` (`+0x5de~+0x5ff`); 스타일 = 템플릿 스타일에서 `WS_VISIBLE` (`0x10000000`) 를 뗀 값 (`+0x415 BT EDI,0x1c` → 조각 `+0x1db~+0x1e7` 이 `BTR EDI,0x1c` 와 `R12|=1` — 보이기는 나중에 `NtUserShowWindow`) 에 새 앱이면 `DS_3DLOOK (4)` 를 더함 (`+0x1e1 OR EDI,ECX`).
- 클래스 (둘째 인자) = **`0x8002`** (`+0x57f MOV R10D,0x8002; +0x63c MOV RDX,R10`) — 템플릿 클래스가 빈 글일 때. 이름이 있으면 `RtlInitLargeUnicodeString` 으로 `LARGE_STRING` (조각 `+0x25d`). 셋째 (버전 클래스) = `ClassNameToVersion` 결과 = 바꿔치기 없으면 같은 `0x8002`. 창 이름 = `LARGE_STRING` (`+0x5a9 ~ +0x5b0`, 글자 수 ×2).
- 스택 인자: `x y cx cy` (템플릿 단위 × 글자 단위 → 픽셀, `0x8000` = 기본 위치), 부모 = `[RBP-0x10]` (우리 표본은 0), 메뉴 = `[RBP+0x20]`, **`hInstance` = 호출자가 준 값 그대로** (`+0x5f0 MOV RAX,[RBP+0x30]` → `+0x60b MOV [RSP+0x50],RAX` — 0 이면 `GetModuleHandleW(0)` 로 `RtlGetExpWinVer` 만 하고 넘기는 값은 원래 0), `lpParam` = 0 (`+0x606 MOV [RSP+0x58],R13`), 끝 인자 = `dwExpWinVer \| 0x40000000` (`+0x5ea MOV [RSP+0x70],EAX`). `0x40000000` 이 있어서 `VerNtUserCreateWindowEx +0x108 AND R12D,0x40000000` 이 켜져서 `ClassNameToVersion` 을 부르는 길이 열린다 (`VerNtUserCreateWindowEx +0x177`).
- `VerNtUserCreateWindowEx` 는 커널이 0 을 주면 `GetLastError() == 0x57f` (클래스 못 찾음) 일 때만 `RegisterDefaultClass` 로 한 번 더 (`+0x263~+0x28f`). stub 이 **0 + 오류 없음** 이면 그대로 NULL.

**3-2. 돌아온 뒤 user32 검사**
- `+0x647 TEST RAX; +0x64a JZ → +0xc90` (NULL) → `ValidateHwnd` (`+0x653`) → 0 이면 NULL → `ValidateDialogPwnd` (`+0x667`) → 0 이면 NULL. 이 셋 뒤에 **창을 파괴하지 않고** `+0xc90` (메뉴 파괴 · 글꼴 지움 · NULL) 로 간다.
- `ValidateDialogPwnd` 0 이 되는 때: 사본 `+0xc8` < `0x1e` (`SetLastError(0x58c)`, `+0x1b/+0x22 JL → +0x83`), 사본 `+0x2a` & `0xc000` ≠ 0 (`+0x29 TEST word [RCX+0x2a],AX`), `AllocLookasideEntry` 0 (`+0x44`), `InitLookaside` 실패 (`+0x6d~+0x74`, 처음 한 번 `0x58 B × 2`). 사본 `+0x12` 비트 0 이 켜져 있으면 **아무것도 안 하고 1** (`+0x6`).

**3-3. 대화상자 자료 `pdlg` (0x58 B) — 누가 어디에 잡고 · 누가 어디에 적나**
- 잡는 쪽 = **user32** `AllocLookasideEntry` (`+0x20~+0x56`: 자유 목록에서 꺼내거나 `RtlAllocateHeap(pUserHeap, 0, 0x58)`) + **`memset` 0 채움** (`+0x51`). 즉 0 으로 시작.
- 붙이는 쪽 = 커널 `NtUserSetDialogPointer(hwnd, pdlg)` → `SetDialogPointer` (win32kfull `SetDialogPointer.asm`): `+0x19 LEA RSI,[RCX+0x28]` (커널 창 물체 `+0x28` = 사용자에게 보이는 사본 포인터) · `+0x25 CMP [사본+0xc8],0x1e; JL` · `+0x32 TEST [사본+0x12],4; JNZ` (비트 2 켜져 있으면 아무것도 안 함) · `+0x5e~+0x62 CMP R8,[pti+0x1d0]` (같은 프로세스) · `+0xa3 unsafe_cast_fnid_zero_to_PDIALOG` (`+0x19~+0x77`: fnid(`사본+0x2a`) 가 0 이거나 `0x2a4` 이면 **`[사본+0x128]`** 을 돌려줌) · **`+0xbc MOV [RAX+8],R14` = 여분 블록 `+8` 에 pdlg** · `+0xf2 MOV word [사본+0x2a],0x2a4` (fnid 가 0 일 때만) · `+0x10a SetOrClrWF(1, 창, 0x201, 1)` = 사본 `+0x12` 비트 0 켬. (pdlg 가 0 이면 `+0xe0 OR word [사본+0x2a],0x4000`.)
- 사본 `+0x128` 쪽 = **커널**: win32kfull `xxxCreateWindowEx +0x4b2` 가 먼저 0 으로 두고, `+0x9c9/+0x9ce GetClientExtraBytesTotalSize` (= 사본 `+0xc8`, 기능 켜졌을 때만 `+0x150` 더함 — `GetClientExtraBytesTotalSize +0x11~+0x2d`) 가 0 이 아니면 `+0x1446/+0x144b xxxClientAllocWindowClassExtraBytes(크기)` (`KeUserModeCallback(0x7b = LEA ECX,[R8+0x77], 입력 4 B)`, `xxxClientAllocWindowClassExtraBytes +0x4c/+0x57/+0x5b`) → 반환 포인터를 `+0x1a46 MOV [사본+0x128],RDI` (이미 0 이 아니면 `+0x147c~+0x148e` `SetLastError(0x57)` 로 실패). **user32 쪽 받는 함수** = `__xxxClientAllocWindowClassExtraBytes` (user32 rva `0x4f770`) — 콜백 표 칸 확인: `_refs/0x4f770.tsv` 의 칸 `0xb4af8` 이 `__ClientThreadSetup` 칸 `0xb49c0` (표 번호 `0x54`, sast/139) 보다 `0x138/8 = 39` 칸 뒤 → 번호 `0x7b` ✓.
- 여분 블록 모양 `[추측: DLGWINDOWEXTRA]`: `+0x0` qword (앱의 `DWLP_MSGRESULT`; `DefDlgProcWorker +0x5b~+0x65` 가 0 이 아니면 `_SetWindowLongPtr(…,0,0)` 으로 지움) · **`+0x8` qword = pdlg** · `+0x10` qword (앱의 `DWLP_USER`) · 모두 0 으로 시작해야 함.
- **`pdlg` 칸 (`InternalCreateDialog` 가 쓰는 줄 · 다른 함수가 읽는 줄)**:

| 칸 | 크기 | 쓰는 줄 | 읽는 줄 | 뜻 `[추측: 이름]` |
|---|---|---|---|---|
| `+0x00` | 8 | `InternalCreateDialog +0x6dd MOV [RCX],R13` (R13 = 앱 프로시저) | `InternalCreateDialog +0xa40` (프로시저 ≠ 0) · `DefDlgProcWorker +0x8c~+0x9c` (`MOV RDX,[RCX]`) · `SendMessageWorker +0x776~+0x78f` (`CMP [RCX],0`) | 앱 `DLGPROC` |
| `+0x08` | 4 | `+0x6be MOV [RCX+8],ESI` | (이 길에서는 안 읽음 — 대화상자 단위 변환 쪽이 읽을 것 `[추측]`) | 글자 단위 가로 (`GetDpiServerInfo…+0x20` 또는 `GdiGetCharDimensions` 반환) |
| `+0x0c` | 4 | `+0x6cf MOV [RCX+0xc],EAX` | 〃 | 글자 단위 세로 (`…+0x24` 또는 `GdiGetCharDimensions` 출력) |
| `+0x18` | 4 | `+0x6af AND …,0xfffffffd` · `+0x6eb AND …,0xfffffffe` · `+0x72c OR …,4` (ANSI 만든 이) · 조각 `+0x276 OR …,ESI(8)` (DPI 켜짐) | `+0xa93 TEST [RCX+0x18],1` · `+0xf5e` · `DefDlgProcWorker` · `DialogBox2` | 깃발: 비트 0 = 끝남 (`EndDialog`) · 비트 2 = ANSI · 비트 3 = DPI 크기 조절 |
| `+0x20` | 8 | `+0x6fa MOV qword [RCX+0x20],1` · (`INITDIALOG` 에서 기본 단추를 찾으면 조각 `+0x310~+0x31e` `[…+0x20]=컨트롤 ID`) | `DefDlgProcWorker` (`DM_GETDEFID`/`SETDEFID`) · `EndDialog` · `DialogBox2` | **기본 단추 ID 겸 결과값** (처음 `1` = IDOK; `EndDialog` 가 같은 칸에 결과를 씀 — 146 에서 확인) |
| `+0x30` | 8 | 조각 `+0x2ce MOV [RCX+0x30],RAX` (= `+0x12ff`) | `DefDlgProcWorker +0x31a~` (`DeleteObject`) | `HFONT` |
| `+0x3c` | 4 | 조각 `+0x287 MOV [RCX+0x3c],EAX` (`+0x12b8`) | — | DPI (켜졌을 때만) |
| `+0x40` | 8 | (안 씀 — 0) | `DefDlgProcWorker` (0 아니면 `DeleteObject`) | 또 하나의 GDI 개체 `[추측]` |
| `+0x50` | 4 | `+0x710 MOV [RCX+0x50],EAX` (= `[RBP-0x34]`, `CreateDlgFont` 의 5번째 출력 인자) | — | `[추측: 글꼴 크기(pt)]` |

## 4. 칸 표 — user32 가 이 길에서 읽는 / 쓰는 칸 (2)

### 4-1. 핸들 항목 (`gSharedInfo+0x8` 가 가리키는 배열의 한 칸)
- 크기 = **`gSharedInfo+0x10` (dword)** (`ValidateHwnd +0x65 IMUL ECX,[gSharedInfo+0x10]`). 읽는 칸이 `+0x1a` (word) 까지라 **`0x1c` 바이트 이상** 필요. 번호 = **`hwnd & 0xffff`** (`+0x51 MOVZX ECX,BX`), 고유 수 = **`(hwnd >> 16) & 0x7fff`** (`+0x90~+0x9c SHR RAX,0x10; AND AX,0x7fff`), 항목 `+0x1a` (word) 와 같거나 `0x7fff` (만능, `+0x146 CMP AX,CX; JZ → +0xae`).
- 개수 검사: `번호 < [gpsi+8]` (`+0x5b`) **그리고** `번호 < [[gSharedInfo+0]+8]` (`+0x7c`) — 둘 다 같은 `psi` 면 하나.

| 칸 | 크기 | 읽는 줄 | 통과 조건 · 0 가능 여부 `[추측: 이름]` |
|---|---|---|---|
| `+0x00` | 8 | `ValidateHwnd +0xd0` · `HMValidateHandleNoSecure +0x8e` | 창의 커널 쪽 주소. 사본 주소 = **TEB `+0x828` + 이 값** (더하기만 — 범위 검사 없음). 0 이면 `ValidateHwnd` 가 NULL (`+0xe7 TEST RSI`) |
| `+0x08` | 8 | `SendMessageWorker +0xb4 MOV RAX,[RDX+8]` → `+0xbc CMP R8,RAX` | **소유 스레드 ID** (`HMAllocObjectEx +0x42a~+0x43e`: `[항목+8] = PsGetThreadId([pti])`). 직접 호출 조건: **== TEB `+0x78`** |
| `+0x10` | 8 | `ValidateHwnd +0xc6 CMP [RDI+0x10],RAX` · `HMValidateHandleNoSecure +0x81` | == `[[TEB+0x820]+0]` 이면 통과. 다르면 `NtUserMapDesktopObject` (`0x14d9`) 로 syscall 한 번 더 (`+0x132/+0x135`, 결과를 `RSI` 에) — 막지는 않음 `[추측: 소속 데스크톱 표지]` |
| `+0x18` | 1 | `ValidateHwnd +0x86 CMP [RDI+0x18],SIL` · `+0xb4 CMP …,1` | 0 = 빈 칸 (실패) · **1 = 창** (아니면 실패). 쓰는 줄 `HMAllocObjectEx +0x244` `[추측: 종류]` |
| `+0x19` | 1 | `ValidateHwnd +0xae TEST [RDI+0x19],1` · `+0xd3 TEST …,4` | 비트 0 켜지면 **실패** (파괴됨 `[추측]`). 비트 2 켜지면 "보안 검사" (`NtUserValidateHandleSecure` `0x15ce`) 건너뜀 |
| `+0x1a` | 2 | `ValidateHwnd +0xa4 CMP AX,[RDI+0x1a]` | == hwnd 윗 15비트 (`0x7fff` 는 만능) `[추측: 고유 수]` |

### 4-2. TEB 칸 (`GS:[0x30]`)
| 칸 | 읽는 줄 | 뜻 · 조건 |
|---|---|---|
| `+0x78` (qword) | `InternalCreateDialog +0x13c` (가드) · `SendMessageWorker +0x58/+0x83` · `SendMessageW +0x48` | **스레드 ID** (`xxxCreateThreadInfo +0x745~+0x756`, sast/139). 직접 호출은 항목 `+0x8` 과 같아야. **0 으로 두면 항목 `+0x8` 도 0 이어야** (그때 `NtUserGetThreadState(0xe)` 를 한 번 더 부르고 결과는 안 씀: `+0x5f~+0x75`) |
| `+0x800` (CI_flags) | `SendMessageWorker +0xdf` | 읽고, 프로시저 부르는 동안 `+0x800 \|= 0x40000000/0x80000000` 로 바꿨다가 되돌림 (`+0x585~+0x643`) — 0 시작 가능 |
| `+0x810` (`dwExpWinVer`) | `InternalCreateDialog +0x1d4` · `+0xb3d` 뒤 조각 `+0x335` | `< 0x400` 이면 스타일 `DS_3DLOOK` 강제 안 함, `IsWindow` 0 일 때 NULL 안 만듦. 0 가능 (옛 앱 취급) |
| `+0x81c` (dword) | `ValidateHwnd +0xde` · `SendMessageInternal +0xf9` · `UpdateWindow +0xd5` | 비트 `0x20000000` 켜지면 `NtUserValidateHandleSecure` 를 매번 — **0 이어야 syscall 없음** |
| **`+0x820`** (포인터) | `HMValidateHandleNoSecure +0x72` · `SendMessageWorker +0xf9~+0x103` | 가리키는 곳 `+0x0` qword = 데스크톱 표지, `+0x8` qword = 데스크톱 창 (커널 주소, `SendMessageW +0x62`), `+0x10` dword = 데스크톱 훅 비트 (**`SendMessageWorker` 는 NULL 검사 없이 `[+0x10]` 읽음**) — 0 이면 `ValidateHwnd` 는 `MapDesktopObject` 로 빠지지만 `SendMessageWorker` 는 접근 위반 |
| `+0x828` | `HMValidateHandleNoSecure +0x87` · `ValidateHwnd +0xcc` | 사본 주소 = 이 값 + 항목 `+0x0`. 0 이면 항목 값이 곧 사본 주소 (에뮬레이터가 커널 = 사용자 주소로 두면 0) |
| `+0x838` (훅 비트) | `SendMessageWorker +0x103` | `\| [TEB+0x820 → +0x10]` 의 `0x2020` 비트가 0 이어야 직접 호출 |
| `+0x840` / `+0x848` | `ValidateHwnd +0x17/+0x20` · `SendMessageInternal +0x2e/+0x37` · `IsWindow +0x26` | 직전 hwnd 와 그 사본 (**한 칸 캐시** — 맞으면 항목 안 봄). 0 이면 느린 길. **user32 는 이 칸을 안 쓴다** (커널이 되부름 도중 채움 `[추측]`) |
| `+0x860` | `UserCallDlgProcCheckWow +0x3a~+0x53` · `UserCallWinProcCheckWow +0x4b~+0x64` | 0 이면 일반 길 |
| `+0x8e8` / `+0x8ec` | `UserCallWinProcCheckWow +0xe1~+0x116` · `SetThreadDpiAwarenessContextFromWindow +0x108~` | DPI 문맥. 사본 `+0x120` 과 다르면 **TEB 만 바꿈** (`+0x6fd~+0x723`) — syscall 없음 |

### 4-3. 공유 정보 (`gSharedInfo` = user32 rva `0xda350`, `psi` = `gpsi`)
| 칸 | 읽는 줄 | 뜻 · 필요 |
|---|---|---|
| `gSharedInfo+0x0` (= `psi`), `psi+0x8` (qword 항목 개수) | `ValidateHwnd +0x3c/+0x5b/+0x7c` | 번호 < 개수 |
| `gSharedInfo+0x8` (항목 배열), `+0x10` (항목 크기 dword) | `ValidateHwnd +0x65~+0x75` | 4-1 |
| `gSharedInfo+0x18` (`pDispInfo`) → `+0x8` (주 모니터) · `gSharedInfo+0x20` | `GetPrimaryMonitor +0x0~+0x1c` | 139 와 같음. NULL 이면 접근 위반 |
| `gSharedInfo+0x28+0x10×(fnid−0x29a)` (최대 번호 dword) · `+0x30+0x10×…` (메시지 비트표 포인터) | `SendMessageWorker +0x731~+0x761` | 0 이면 "모든 메시지를 내장 프로시저로" (직접 호출 길 A) |
| `psi+0x1d8+8×(fnid−0x29a)` (A) · `psi+0x298+8×(…)` (W) | `SendMessageWorker +0x3f3` `+0x401` | 창 프로시저 (`사본+0x78`) 가 이 중 하나와 같으면 내장 길 A, 아니면 일반 길 B |
| `psi+fnid×8−0x1200` (fnid `0x2a4` → `psi+0x320`) | `SendMessageWorker +0x7b0~+0x7c4` (`+0x8f7~+0x91c`) | 길 A 에서 부르는 일꾼 포인터 (진짜는 `DefDlgProcWorker` — 인자 모양이 같음 `[추측]`). **0 이면 널 호출** |
| `psi+0x364+2×i` (원자 표, `i = 서수 & 0xff7f`, 0..0x18) | `InternalCreateDialog +0x84b MOVZX R10D,[R11+RAX*2+0x364]` | 컨트롤 클래스 원자. `psi+0x36e` (i=5) 는 콤보 상자 (`+0x871`) |
| `psi+0x15c` (word) | `DefDlgProcWorker +0x3e2~+0x3f6` | fnid 가 0 인 창에서만 `사본+0xc8` 과 견줌 |
| `psi+0x0` 비트 `0x2` `0x4` `0x80` · `psi+0x7b4` · `psi+0x1b5c` | `SendMessageWorker +0x1af` · `InternalCreateDialog +0xb4a~+0xb61` · `+0xc50` | 0 이면 전부 건너뜀 (139 와 같음) |

### 4-4. 창 사본 (`pwnd`, `ValidateHwnd` 가 돌려주는 포인터) — **PDB 에 `tagWND` 없음, 이름은 `[추측]`**
`pwnd = [TEB+0x828] + 항목+0x0`. 크기: 읽는 칸이 `+0x128` (qword) 까지 → **최소 `0x130` B**.

| 오프셋 | 크기 | 이름 `[추측]` | 읽는 줄 | 쓰는 줄 | 0 가능 / 있어야 할 값 |
|---|---|---|---|---|---|
| `+0x00` | 8 | `HWND` (머리 `h`) | `InternalCreateDialog +0x667 MOV RCX,[RBX]` (→ `NtUserSetDialogPointer`) · `SendMessageWorker +0x1c MOV RSI,[RCX]` · `+0x87 MOVZX ECX,word [R12]` | 커널 | **hwnd 와 같아야** (`SendMessageWorker` 가 이 값의 낮은 16 비트로 항목을 다시 찾음) |
| `+0x08` | 8 | 커널 쪽 자기 주소 | `SendMessageWorker +0x3a8 SUB RSI,[R12+8]` · `_GetChildControl +0x2a` · `_IsDescendant +0x24` · `_NextControl +0xe6` | 커널 | 사본 안의 커널 포인터 (`+0x30 +0x38 +0x80`) 를 사용자 주소로 바꿀 때 `포인터 − [+8] + pwnd`. **커널 = 사용자 주소면 `[+8] == pwnd`** |
| `+0x11` | 1 | 깃발 바이트 | `UpdateWindow +0x1d TEST [RAX+0x11],0x10` | 커널 | 0 가능 (켜지면 `NtUserUpdateWindow` 부름) |
| **`+0x12`** | 1 | **창 깃발 바이트** | 비트 0: `ValidateDialogPwnd +0x6` · `GetParentDialog +0x8` · `_GetWindowLong +0x50` · `DefDlgProcWorker +0x1ea` / 비트 2: `SendMessageWorker +0xc5` (켜지면 **syscall 길**) · `DispatchMessageWorker +0xb6` / 비트 3: `SendMessageWorker +0x14c` (창이 ANSI) | 비트 0: 커널 `SetDialogPointer +0x10a` (`SetOrClrWF 0x201`) | 비트 0 = **대화상자 표시** (켜면 `ValidateDialogPwnd` 가 `pdlg` 를 안 만들고 바로 1 — 그때 여분 블록 `+8` 에 `pdlg` 가 이미 있어야). 비트 2 = 0 (직접 호출) · 비트 3 = 0 (유니코드) |
| `+0x1a` | 1 | 깃발 | `_GetChildControl +0x31` · `_NextControl +0x7b` (비트 0 켜지면 거기서 멈춤) · `DefDlgProcWorker` | 커널 | 0 |
| `+0x1c` | 4 | **스타일** | `InternalCreateDialog +0x860` (`& 4` `DS_3DLOOK`) · `+0xbf4` (`& 0x10` `DS_NOFAILCREATE`) · `+0xae0` (`& 0x18010000`: 탭 정지 · 보임 · 비활성) · `_NextControl` | 커널 (`xxxCreateWindowEx +0x577`, `WS_VISIBLE` 뗀 값) | 만들 때 준 스타일 (`WS_VISIBLE` 뺌) |
| `+0x1d` | 1 | 스타일 윗바이트 | `InternalCreateDialog +0xa90 MOV AL,[RBX+0x1d]` (비트 2 = `DS_CONTROL 0x400`) · `+0x68e` | 〃 | 〃 |
| `+0x1e` | 1 | 스타일 바이트 2 | `+0x68e MOV AL,[RBX+0x1e]` (비트 3 = `WS_SYSMENU 0x80000`; `&7` = 그 아래 비트) | 〃 | 〃 |
| `+0x1f` | 1 | 스타일 윗바이트 | `_GetChildControl +0xc` (`& 0xc0 == 0x40` = 자식) · `+0xf68 TEST [RBX+0x1f],0x10` (`WS_VISIBLE`) · `+0xb76` · `InternalDialogBox +0x10c/+0x119` | 〃 | 〃 |
| `+0x2a` | 2 | **fnid** (함수 종류) | `ValidateDialogPwnd +0x29` (`& 0xc000`) · `IsWindow +0xfe` (부호) · `DefDlgProcWorker +0x32/+0x41` · `_IsDescendant +0x7` (`& 0x2fff == 0x29d` = 데스크톱) | 커널 `SetDialogPointer +0xf2` (0 이면 `0x2a4`) | 0 시작 가능 → `NtUserSetDialogPointer` 가 `0x2a4` 로. `& 0xc000` 이면 `ValidateDialogPwnd` 0. 부호 비트 (`0x8000`) 면 `IsWindow` 0 |
| `+0x30` | 8 | 부모 (커널 주소) | `_GetChildControl +0x21` · `_NextControl +0x53/+0x57` · `_IsDescendant +0x17` | 커널 | 0 가능 (최상위) — 아니면 `+8` 규칙으로 변환 |
| `+0x38` | 8 | 첫 자식 (커널 주소) | `_NextControl +0xdd MOV R9,[RCX+0x38]` · `UpdateWindow +0x23` | 커널 | **0 이면 `_NextControl(대화상자, NULL, 3)` 이 대화상자 자신을 돌려줌** → 초점은 대화상자 hwnd 에 (`+0xb0d~+0xb21`). `_GetChildControl(…, NULL)` 은 늘 NULL (`+0x7 TEST RDX; JZ`) |
| `+0x78` | 8 | **창 프로시저** (사용자 모드 주소) | `SendMessageWorker +0x47` · `+0x3e7` | 커널 | **0 이면 안 됨** — `psi` 의 `+0x1d8/+0x298` 표와 같으면 길 A (일꾼 포인터 `psi+0x320` 필요), 다르면 길 B (이 주소를 `UserCallWinProcCheckWow` 로 부름) |
| `+0x80` | 8 | **클래스 포인터** (커널 주소) | `SendMessageWorker +0x29c MOV RSI,[R12+0x80]` (`+0x2a4 TEST; +0x3a8` 변환) · `+0x3c5 MOVZX R9D,word [RSI+4]` | 커널 | **0 이면 `[0+4]` 접근 위반** (내장 길 검사 앞). 가리키는 곳 `+4` word = 클래스 fnid (`0x2a4` 대화상자) |
| `+0x88` | 8 | (갱신 영역 `[추측]`) | `UpdateWindow +0x13` | 커널 | 0 가능 |
| `+0xc8` | 4 | **창 여분 바이트 크기** | `ValidateDialogPwnd +0x1b` (≥ `0x1e`) · `DefDlgProcWorker +0x3f0` · 커널 `GetClientExtraBytesTotalSize +0x11` | 커널 `xxxCreateWindowEx +0x59e` (= 클래스 `[+8]+0x50`) | **`0x1e` 이상** (대화상자). 이 크기가 되부름 `0x7b` 의 요청 크기 |
| `+0xe0` | 8 | 활성화 컨텍스트 | `DefDlgProcWorker +0xa3` · `SendMessageWorker +0x5e6` → `UserCallWinProc…` | 커널 | 0 가능 (`RtlActivateActivationContextUnsafeFast` 가 0 을 받음) |
| `+0x120` | 4 | 창 DPI 문맥 | `UserCallWinProcCheckWow +0x102` · `SetThreadDpiAwarenessContextFromWindow +0x12c` | 커널 `xxxCreateWindowEx +0x510` | 0 가능 (TEB `+0x8e8` 만 바뀜) |
| **`+0x128`** | 8 | **창 여분 바이트 포인터** (사용자 주소) | `InternalCreateDialog +0x69a` `+0x6b3` `+0x6c1` … (`pdlg` 칸을 쓸 때마다 다시 읽음) · `DefDlgProcWorker +0x5b` `+0x8c` · `SendMessageWorker +0x776` | **커널** `xxxCreateWindowEx +0x1a46` (되부름 `0x7b` 결과) | **0 이면 접근 위반**. 가리키는 곳은 쓰기 가능 · 0 채움 · `+0xc8` 바이트 이상 |
| `+0x140` | 4 | **컨트롤 ID** `[추측]` | `xxxCheckDefPushButton +0xf3 CMP EDX,[RSI+0x140]` | 커널 | 컨트롤마다 `hMenu` 인자로 준 ID (`+0x957`) |

### 4-5. user32 가 **쓰는** 칸 (이 길 전체)
- 사본 (`pwnd`) 에는 **쓰는 줄이 없다** (user32 는 사본을 읽기만; 쓰기는 전부 커널 syscall).
- `pdlg` (3-3 표) · 그리고 `pdlg` 가 들어 있는 여분 블록 `+0x0` (`DefDlgProcWorker +0x5b~+0x65` → `_SetWindowLongPtr(…,0,0)`, 0 이 아닐 때만).
- TEB: `+0x800` · `+0x8e8/+0x8ec` · (`g_systemCallFilterId == 5` 일 때만) `+0x8e0 \|= 0x100` (`SendMessageWorker +0x140`).
- `psi+…`: 없음. 핸들 항목: 없음.

## 5. (c) 컨트롤 고리 (`InternalCreateDialog +0x2de~+0xa23`, 반복 몸통 `+0x975~+0x9f4`)
- 항목 꼴: 일반 `DLGITEMTEMPLATE` = 스타일 `+0` · 확장 `+4` · `x y cx cy` `+8~+0xe` · ID `+0x10` (word) · 클래스 `+0x12`; EX = 도움말 `+0` · 확장 `+4` · 스타일 `+8` · `x y cx cy` `+0xc~+0x12` · ID `+0x14` (dword) · 클래스 `+0x18` (`+0x764`, `+0x76f~+0x7b9`, `+0xa03~+0xa23`). 반복 횟수는 `cdit` (`+0x74e~+0x75e`).
- 클래스: `0xffff` 로 시작하면 서수 → `(서수 & 0xff7f) < 0x19` 면 **원자 = `psi+0x364+2×번호`** (`+0x824~+0x84b`), 아니면 실패 (`+0x83e JNC → +0xbf4`). 글이면 `SkipSz` + `RtlInitLargeUnicodeString` (조각 `+0x25d`). 원자가 콤보 칸 (`psi+0x36e`) 이거나 스타일 비트 23 이 켜졌고 대화상자가 `DS_3DLOOK` 이면 컨트롤 확장 스타일에 `0x200` (`WS_EX_CLIENTEDGE`) 을 더하고 비트 23 을 뗌 (`+0x860~+0x87c`, `+0xc12`). 모든 컨트롤 확장 스타일에 `0x4` (`WS_EX_NOPARENTNOTIFY`) 를 더함 (`+0x857 OR R8D,R14D` = 4). ID ≠ 0 이면 스타일을 `(& 0x3fffffff) \| 0x40000000` (자식, `+0x8e1~+0x8e8`).
- 창 이름: `0xffff` 서수면 4 바이트 이진 (`local_e0 = 0x400000004`), 아니면 글 (`길이×2`, `+0x8b6~+0x8cb`). 생성 데이터: 바이트 수 ≠ 0 이면 그 크기 칸 주소를 `lpParam` 으로 (`+0x8ce~+0x8d7 MOVZX/NEG/SBB/AND` → `+0x949 MOV [RSP+0x58],R11`). `hMenu` 인자 = 컨트롤 ID (`+0x904 MOVSXD R14,R13D` → `+0x957 MOV [RSP+0x48],R14`).
- 컨트롤 하나당 syscall: **`NtUserGetAtomName` (원자일 때, `ClassNameToVersion +0x226`) → `NtUserCreateWindowEx`** (`+0x975/+0x97a`). 반환 0 이면 `+0x980 JZ → +0xbf4` → `사본+0x1c & 0x10` (`DS_NOFAILCREATE`) 가 **없으면** `NtUserDestroyWindow` (`+0xc01/+0xc08`) → `+0xc0d JMP → +0xd4e` → **NULL**; 있으면 `+0xbf8 JNZ → +0x9c7` (고리의 다음 컨트롤).
- 이어서 (있으면): `도움말 ID ≠ 0` → `NtUserSetWindowContextHelpId` (조각 `+0x2ff`) · **글꼴 ≠ 0 → `SendMessageW(ctl, 0x30, hfont, 0)`** (`+0x99a~+0x9ac`) · **항상 `SendMessageW(ctl, 0x87)`** (`+0x9ba/+0x9bf`): 반환 `& 0x10` (`DLGC_DEFPUSHBUTTON`) 이면 `pdlg+0x20 = 컨트롤 ID` (조각 `+0x310~+0x31e` = `+0x1341~+0x134f`).
- `SendMessageW` → `SendMessageInternal` (사본이 안 구해지면 **조용히 0**, 오류 없음: `+0x11b XOR EAX,EAX`) → `SendMessageWorker`. 컨트롤 hwnd 가 항목 · 사본을 안 가지면 이 두 호출이 0 을 돌려주고 지나간다 (고리는 안 막힘).
- 고리 끝: `_GetChildControl(대화상자, NULL)` 은 늘 NULL → `_NextControl(대화상자, NULL, 3)` 이 첫 자식 (`사본+0x38`) 에서 시작해 "보임 + 탭 정지 + 비활성 아님" (`& 0x18010000 == 0x10010000`) 을 찾음 (`+0xaaf~+0xb19`, `+0xea5~+0xf14`). 못 찾으면 대화상자 자신 (자식 없음 → `+0x38 == 0`). `WM_INITDIALOG` 의 `wParam` = 찾은 컨트롤 hwnd (또는 0).

## 6. (d)(f) 메시지 보내기와 되부름

**6-1. `SendMessageWorker` 직접 호출 길** — 5 조건을 순서대로 (`ms/disassemble/user32/SendMessageWorker.asm`):
1. `+0x38 LEA EAX,[RDI-0x3e0]; +0x3e CMP EAX,8; +0x41 JBE → +0x9bb` (메시지 `0x3e0~0x3e8` 은 항상 syscall 길).
2. `+0x58~+0x7a` TEB `+0x78` 읽기 (0 이면 `NtUserGetThreadState(0xe)` 를 부르고 **결과와 상관없이** `R8 = TEB+0x78`), `+0x87~+0xb8` 항목 찾기 (`번호 < psi+8`, `항목+0x18 ≠ 0`), `+0xb4 MOV RAX,[RDX+8]` (없으면 `+0xba XOR EAX,EAX`), **`+0xbc CMP R8,RAX; +0xbf JNZ → +0x9b6` (syscall 길)**.
3. `+0xc5 TEST [사본+0x12],4; +0xd0 JNZ → +0x9b6`.
4. `+0xf9 MOV RAX,[TEB+0x820]; +0x100 MOV ECX,[RAX+0x10]; +0x103 OR ECX,[TEB+0x838]; +0x10a TEST ECX,0x2020; +0x110 JNZ → +0x9b6`.
5. `+0x14c~+0x16f` 글자 종류: `(bAnsi == 0) == (~사본+0x12 >> 3 & 1)` 이면 변환 건너뜀 (`JZ → +0x28f`). 다르면 메시지별 변환 (`+0x175~+0x225`) 하고 `MessageTable[msg] & 0x100` 이 켜진 메시지는 syscall 길 (`R14B`, `+0x40f JNZ → +0x9c0`) — **A 판은 여기서 갈림 (146)**.
6. `+0x29c RSI = [사본+0x80]` (0 이면 `+0x3c5` 에서 `[0+4]` 접근 위반), `+0x3b0~+0x3c3` 사용자 API 훅 (`ghmodUserApiHook`/`gfUserApiHook`) 있으면 일반 길 B, `+0x3c5 R9W = [클래스+4]` (fnid) · `+0x3d3~+0x3e5` fnid 가 `0x29a` 또는 `0x2a1~0x2aa` 이어야 길 A 후보 · `+0x3e7~+0x409` **`[사본+0x78]` 이 `psi` 의 `+0x248+8k` (W) 또는 `+0x188+8k` (A) 와 같으면 길 A (`+0x731`)**, 아니면 길 B (`+0x40f`).
   - **길 A** (`+0x731~+0x8ed`): `gSharedInfo+0x28+0x10k` 의 비트표 포인터가 0 이거나 (`+0x740/+0x743`) 메시지가 표 안에 켜져 있으면 일꾼 `psi+fnid×8−0x1200` 을 `(사본, msg, wParam, lParam, bAnsi)` 로 직접 부름 (`+0x8f7~+0x956`); 표에 없으면 fnid `0x2a4` 이고 `[[사본+0x128]+8]` 과 그 `+0x0` 이 0 이 아닐 때만 같은 일꾼 (`+0x767~+0x7ce`), 아니면 `RealDefWindowProcWorker` / `DefWindowProcW/A` (`+0x870~+0x8a0`).
   - **길 B** (`+0x40f~+0x605`): 메시지 `0xe9` · `0x111` 등 몇 개를 빼면 TEB `+0x800` 비트를 켜고 **`UserCallWinProcCheckWow([사본+0xe0], [사본+0x78], hwnd, msg, wParam, lParam, 0, 1)`** (`+0x600/+0x605`) 로 창 프로시저를 직접 부른 뒤 되돌림. `UserCallWinProcCheckWow` 자체에는 win32k syscall 을 부르는 코드가 없다 (활성화 컨텍스트 · DPI 맞춤 · 훅 검사만; `+0x4b~+0x116`).
7. syscall 길 (`+0x9c0~+0xa3d`): `msg ≥ 0x400` 이면 **`NtUserMessageCall` (`0x1005`)** `(hwnd, msg, wParam, lParam, 0, 0x2b1, bAnsi)` (`+0x9f4`), 아니면 `gapfnScSendMessage[MessageTable[msg] & 0xff]` (`+0xa02~+0xa21`) — 거기서 다시 `NtUserMessageCall`. `SendMessageW` 안에도 같은 표 길이 있는데 (`SendMessageW +0xdd`/`+0x10a`), 그건 hwnd 가 `0xffff` (HWND_BROADCAST) 또는 −1 일 때뿐 (`+0x28 LEA RAX,[RCX+1]; +0x2e TEST RAX,-0x10001; +0x34 JNZ → +0xe4 SendMessageInternal`). 보통 hwnd 와 hwnd = 0 은 `SendMessageInternal` 로 간다. 메시지 번호 `& 0xfffe0000` 이면 `SetLastError(0x57)` 후 0 (`+0x17/+0x1d → +0x143`).

**6-2. 대화상자 창의 `WM_INITDIALOG`** (`SendMessageWorker(사본, 0x110, hwndFocus, lParam, 0)`):
- 길 B 로 가면 `[사본+0x78]` = **`DefDlgProcW`** 여야 한다 (대화상자 클래스의 클라이언트 프로시저). `DefDlgProcW +0x17/+0x1c ValidateHwnd` → `+0x31 DefDlgProcWorker(사본, msg, w, l, 0)`.
- `DefDlgProcWorker`: `+0x32/+0x36` fnid 0 → `+0x3e2~+0x404` **`NtUserSetWindowFNID(hwnd, 0x2a4)`** (`0x108d`) (`사본+0xc8 ≥ psi+0x15c` 일 때) / fnid ≠ `0x2a4` 면 `DefWindowProc` 길 · `+0x4e ValidateDialogPwnd` (이미 1) · **`+0x8c~+0x9c`: `pdlg = [[사본+0x128]+8]`, 앱 프로시저 = `[pdlg+0x0]` (0 이면 `+0xf0` 이후 기본 처리)** · `+0xa3~+0xbf UserCallDlgProcCheckWow([사본+0xe0], 앱 프로시저, hDlg, msg, wParam, lParam, 0, &결과)` · `+0xcf HMValidateHandleNoSecure` (0 이면 끝) · `+0xdd TEST [사본+0x2a],0xc000` · `WM_INITDIALOG` (`0x110`) 는 앱 프로시저 뒤 **기본 처리 없이 끝**: `+0xf0~+0x13d` 에서 `msg − 0x39 == 0xd7` (= `0x110`) → `+0x2da MOV EAX,R13D; +0x2dd JMP → +0x193` (돌려주는 값 = 앱 프로시저의 반환 `[RBP-0x28]`; 끝 `+0x18c/+0x190`). 앱이 0 을 돌려준 갈래도 `+0x986 SUB EAX,0x110; +0x98b JZ → +0x190` 으로 같은 끝.
- `UserCallDlgProcCheckWow` 는 syscall 없이 앱 프로시저를 CFG 로 직접 부름 (`+0x193/+0x1ed indirect:cfg:stack[+0x10]`; 앞뒤로 `SetThreadDpiAwarenessContextFromWindow` · `RtlActivateActivationContextUnsafeFast` · `gcCallUserApiHook` 만).
- `InternalCreateDialog` 는 반환을 `(int)_Var19` 로 본다: 0 이면 기본 초점 건너뜀 (`+0xa7d TEST ESI,ESI`).

**6-3. (f) 되부름이 만드는 상태 — 예/아니오 + 줄**
| 되부름 | 뒤 단계가 읽는 상태를 만드나 | 근거 |
|---|---|---|
| **`0x7b` `ClientAllocWindowClassExtraBytes`** | **예** — 사본 `+0x128` 의 여분 블록. 없으면 `+0x6af` 접근 위반 | 3-3 · `xxxCreateWindowEx +0x1446/+0x1a46` · `InternalCreateDialog +0x69a~+0x6ab` |
| `WM_NCCREATE` · `WM_CREATE` … (커널 → 창 프로시저) | **아니오** (대화상자 창) — `DefDlgProcWorker` 가 첫 메시지에서 하는 일 (`NtUserSetWindowFNID`, `ValidateDialogPwnd`) 은 `InternalCreateDialog +0x667` 이 이미 하고, `pdlg` 칸은 `InternalCreateDialog` 가 쓴다 | `DefDlgProcWorker +0x32~+0x55` · `InternalCreateDialog +0x667~+0x72c` |
| 컨트롤의 `WM_NCCREATE`/`WM_CREATE` (버튼 · 정적 글 · 편집 …) | `[추측]` — 컨트롤 프로시저가 자기 자료를 사본 여분 바이트에 만들고, 길에서 읽는 건 `SendMessageW(ctl, 0x30/0x87)` · `xxxCheckDefPushButton` 의 `SendMessageW`. **컨트롤 사본 `+0x12` 비트 2 를 켜 두면 이 호출들이 `NtUserMessageCall` 로 가서 (stub 이 0 답) 컨트롤 프로시저를 아예 안 부름 → 안전** `[정적: SendMessageWorker +0xc5/+0xd0]`. 컨트롤 프로시저 본문은 안 읽음 | `SendMessageWorker +0xc5~+0xd0` · 5절 |
| `WM_GETMINMAXINFO` · `WM_NCCALCSIZE` · `WM_SIZE` 등 | 아니오 (user32 가 이 길에서 읽는 칸 없음) | 4절에 해당 칸 없음 |

**한 줄**: 되부름을 구현하지 않아도 (a)~(e) 는 지나간다 — **`0x7b` 하나를 stub 이 대신 (여분 블록 할당)** 하고, 대화상자 창 프로시저는 `SendMessageWorker` 직접 길로 진짜로 부르면 된다.

## 7. (e) NULL (→ −1) 로 나가는 가지 전부 — `InternalCreateDialog` (`ms/disassemble/user32/InternalCreateDialog.asm`)

공통 끝: NULL 은 `+0xd4e XOR EAX,EAX → +0xd50 JMP +0xbb9` (또는 `+0xc90` 정리 → `+0xd4e`). `InternalDialogBox +0x75/+0x78 JZ → +0x151` → 부모 0 이면 `+0x159 TEST RDI; +0x15c JZ` → 조각 `FUN_1800ab7c2 +0x0 OR RAX,-1; +0x4 JMP → InternalDialogBox +0x8f` = **−1**. (부모 ≠ 0 이고 `ESI == 0` 이면 `+0x167 NtUserEnableWindow(부모, EBX)` 하고 같은 −1.)

| # | 줄 | 조건 | 뒷정리 |
|---|---|---|---|
| N1 | `+0x13c/+0x140 JZ → +0xd37` · `+0xd45/+0xd48 JNZ → +0x146` · `+0xd4e` | TEB `+0x78 == 0` 이고 `NtUserGetThreadState(0xe)` 반환 0 (139 와 같음) | 없음 |
| N2 | `+0x1ba JC` 안 탐 → `+0x216 CMP R14D,EAX; +0x219 JNZ → +0x10cd; +0x10cf JNZ → +0xd4e` | **새 앱 (`RtlGetExpWinVer ≥ 0x400`) 이고 템플릿 스타일 낮은 16비트의 `0x8000` 비트가 켜짐** (`BTR EDI,0xf` 로 뗀 값과 달라짐) | 없음 |
| N3 | `+0x8ca16/+0x8ca1b LoadMenuW; +0x8ca28 JZ → +0xd4e` | 템플릿에 메뉴 이름이 있는데 `LoadMenuW` 0 | 없음 |
| N4 | `+0x333/+0x33a CreateCompatibleDC; +0x345 JZ → +0xc90` | **`CreateCompatibleDC(0)` = 0** | `+0xc90` (메뉴 파괴 · 글꼴 `DeleteObject` 후 NULL) |
| N5 | 조각 `+0x220/+0x225 _MonitorFromRect; +0x22b JZ → +0xc90` (= `+0x1251~+0x125c`) | 부모 없음 · `GetDialogMonitor` 가 NULL (`gSharedInfo+0x18` 의 `+8` 이 0) · `_MonitorFromRect` 도 NULL `[추측: 같은 표를 쓰면 NULL]` | 〃 |
| N6 | `+0xc7d/+0xc82 _MonitorFromPoint; +0xc8a JNZ` 안 탐 → `+0xc90` | `DS_CENTERMOUSE` (`0x1000`) + `psi+0x7b4 ≠ 0` + 모니터 NULL (우리 표본은 `psi+0x7b4 = 0` 이라 안 탐) | 〃 |
| N7 | `+0x644/+0x647 TEST RAX; +0x64a JZ → +0xc90` | **`VerNtUserCreateWindowEx` NULL** (커널 0 · `ClassNameToVersion` NULL = `NtUserGetAtomName` 0) | 〃 |
| N8 | `+0x658/+0x65b; +0x65e JZ → +0xc90` | `ValidateHwnd` NULL | 〃 (창은 안 부숨) |
| N9 | `+0x66c/+0x66e JZ → +0xc90` | `ValidateDialogPwnd` 0 (3-2) | 〃 |
| N10 | `+0x97a/+0x97d/+0x980 JZ → +0xbf4` | 컨트롤 `VerNtUserCreateWindowEx` NULL | `사본+0x1c & 0x10` 없음 → `+0xc01/+0xc08 NtUserDestroyWindow(대화상자)` → `+0xc0d JMP → +0xd4e` |
| N11 | `+0x83e JNC → +0xbf4` | 컨트롤 서수 클래스 `≥ 0x19` | N10 과 같음 |
| N12 | `+0xa6f/+0xa74 HMValidateHandleNoSecure; +0xa77 JZ → +0xb9b` → `+0xba5/+0xbad` | `WM_INITDIALOG` 처리 뒤 hwnd 가 더는 유효하지 않음 (앱이 `DestroyWindow` 함) → 끝 검사가 0 | 없음 |
| N13 | `+0xb36/+0xb3b IsWindow; +0xb3d JZ → +0x1358` (조각 `+0x327`) `+0x335 CMP [TEB+0x810],0x400; +0x33b JNC → +0xd4e` | 초기화 뒤 `IsWindow` 0 이고 새 앱 (옛 앱이면 hwnd 그대로 반환 `+0x341 JMP → +0xbb6`) | 없음 |
| N14 | 조각 `+0x366~+0x37a` (= `+0x1397~+0x13ab`) `NtUserSetForegroundWindow` 뒤 `IsWindow` 0 → `+0x1382 MOV RDI,R13 (0)` → `+0xba5` | `DS_SETFOREGROUND` 이고 그 뒤 창이 사라짐 | 없음 |
| N15 | `+0xba3 JNZ → +0xbb6` 안 탐 → `+0xba5/+0xba8 HMValidateHandleNoSecure; +0xbad NEG RAX; SBB; AND RDI,RCX` | 끝 검사가 0 (위 항목 N12 와 같은 자리, `param_7 & 4` 가 0 일 때) | 없음 |

바깥 (−1 을 만드는 줄): `DialogBoxParamW +0x39 JZ` (`FindResourceExW` 0) · `+0x53 JZ` (`LoadResource` 0) · `DialogBoxIndirectParamAorW +0x27 JNZ` 안 탐 → `+0x29 OR RAX,-1` (`GetSizeDialogTemplate` 0) · `InternalDialogBox +0x78 JZ → +0x151 → 조각 OR RAX,-1` (N1~N15 모두). **부모 hwnd ≠ 0 이고 `ValidateHwnd(부모)` 가 0 이면 −1 이 아니라 0** (`InternalDialogBox +0x101~+0x107 JZ → +0x8f`, 반환 RAX=0) — 우리 표본 부모는 0 이라 해당 없음.

## 8. (a) 가드 뒤 ~ `+0x63f` 앞 — 템플릿 깃발마다 타는 부름 표

| 템플릿 / 상태 | 줄 | 부르는 것 | 0 을 받으면 |
|---|---|---|---|
| 항상 | `+0x7f/+0x84` | `GetDpiForSystem` (→ `IsThreadDesktopComposited` → TEB `+0x820`) | 0 이어도 지나감 |
| 항상 | `+0x19e/+0x1a3` | `RtlGetExpWinVer(hInst)` (user32 가 exe 머리를 읽음). `hInst==0` 이면 `+0x1005 GetModuleHandleW(0)` | 반환 `< 0x400` 이면 옛 앱 갈래 (`+0x1be JC → +0x10c2`) |
| 항상 | `+0x316/+0x31b` | `GetDialogMonitor(부모, 1)` → (부모 0) `NtUserGetForegroundWindow` → `GetPrimaryMonitor` | 모니터 0 → N5 |
| 항상 | `+0x333/+0x33a` | `CreateCompatibleDC(0)` | **0 → N4 (NULL)** |
| 부모의 GDI 확대 (`Ordinal_2635`=`IsWindowGdiScaledX`) | `+0x34e/+0x353` · `+0x355 JNZ →` 조각 `+0x8cacc~+0x8cb43` | `NtUserGetDC` → `GetDCDpiScaleValue` → `CreateCompatibleBitmap` · `SelectObject` · `NtUserReleaseDC` | 0 이면 건너뜀 (stub 은 부모 0 이라 `IsWindowGdiScaledX(0)` 가 0 이면 안 탐) |
| `DS_SETFONT` (`0x40`) | `+0x35d TEST DIL,0x40; +0x386/+0x38b` | `CreateDlgFont` (gdi32 부름 — Q278) | 0 → `+0x3de TEST R12B,4; +0x3e4/+0x3e9 GetStockObject(0xd)`. 둘 다 0 → 글꼴 없음 (`WM_SETFONT` 안 보냄) |
| `DS_SETFONT` 아님 · `DS_FIXEDSYS` (`0x8`) · 새 앱 | `+0xdaf~+0xdc2 JNZ → 조각 +0x175` | `GetStockObject(0x10)` | 0 → 글꼴 없음 |
| 글꼴이 있을 때 | `+0xe24~+0xe85` | `SelectObject(hdc, 글꼴)` → `GdiGetCharDimensions(hdc, 0, &세로)` → `SelectObject(…, 옛것)` | `SelectObject` 0 → `DeleteObject` + `GetStockObject(0xd)`. `GdiGetCharDimensions` 0 → 기본 단위 그대로 |
| 항상 | `+0x3b0/+0x3b5` `+0x3bb/+0x3c0` | `GetDpiServerInfoForCurrentThread()` → `[+0x20]` `[+0x24]` (기본 글자 단위) | 값이 0 이면 단위 0 → 좌표 0 (크래시 아님). 포인터는 `psi` 안 (139) |
| 항상 | `+0x409/+0x410` | `DeleteDC` | 안 봄 |
| 항상 | `+0x467/+0x46c` | `_AdjustWindowRectExForDpi(사각형, 스타일, 0, 확장스타일, dpi)` | 안 봄 |
| 항상 | `+0x533/+0x538` | `RepositionRect(모니터, 사각형, 스타일, 확장)` (`DS_CENTER 0x800` 이면 앞서 `GetMonitorWorkRect`) | 안 봄 (모니터 사각형 읽음) |
| 부모 ≠ 0 | `+0xdf2` `+0xe00` | `ClientToScreen` · `GetWindowLongW(부모, −0x14)` | 해당 없음 (우리 표본 부모 0) |
| 템플릿 스타일 | `+0x10da~+0x1111` 조각들 | 스타일 보정 (`0x80`→`0x101`, `0x2000`→`0x400`, `0x400`→ `& 0xff37fffd`, `0x4000`) | NULL 아님 |

## 9. 지금 우리 (PE_emulator) 맞나·틀리나

- **`nt_user_get_atom_name.cpp` (`0x8002` 가지)**: 지금 `0xc000` 만 1, 그 밖은 0, 글 안 씀 → **`0x8002` 는 틀림.** 맞는 규칙 한 줄: `0 < 원자 < 0xc000` 이면 `L"#<십진수>"` 를 버퍼 (`[USTR+8]`) 에 쓰고 NUL 까지 쓰고 **글자 수 (NUL 제외)** 를 반환 (`0x8002` → 6, 글 `#32770`). 버퍼 한도 (`MaximumLength/2` 글자) 가 글자 수 + 1 이상이어야 전체를 씀 — 모자라면 `한도−1` 글자로 자르고 그 수 반환 (한도 < 2 글자면 0). `Length` 는 **쓰지 않는다** (user32 가 씀). `0xc000` 이상은 그대로 "0 아닌 값" (run131) 이고 컨트롤 원자 (`psi+0x364` 표의 값) 에도 같이 쓰임.
- **`nt_user_create_window_ex.cpp`**: 지금 "클래스 인자가 이름 글 포인터거나 `0xc000` 이면 늘 같은 hwnd `0x10010`, 그 밖 (`0x8002` 도) 0" → **틀림 둘**: (1) `0x8002` 를 받아야 함 (둘째 · 셋째 인자 둘 다 `0x8002`), 컨트롤 원자도. (2) **늘 같은 hwnd 는 안 됨** — 대화상자 한 번에 `1 + 컨트롤 수` 번 불리고, hwnd 마다 4-1 · 4-4 의 항목 · 사본 · 여분 블록이 따로 있어야 `ValidateHwnd` · `SendMessageW` 가 지나감 (같은 hwnd 를 돌려주면 컨트롤 고리가 자기 자신에게 `SendMessageW` 하고 `+0x20` 을 덮어씀).
- **`csr_user_connect_stub.cpp` (핸들 표 블록)**: 지금 `gSharedInfo+0x8` 0 · `psi+0x8` 0 → 4-3 의 칸이 모두 필요 (`+0x8` 배열 · `+0x10` 항목 크기 · `psi+0x8` 개수 · `psi+0x364` 원자 표 · `gSharedInfo+0x28~` 비트표는 0 으로 둘 수 있음). 139 의 "최소 조건 3~5" (TEB `+0x820` · `psi+0x1b56` · `gSharedInfo+0x18`) 위에 얹는 것.
- **새 stub**: `NtUserSetDialogPointer` — 위 3-3 대로 (여분 블록 `+8` 에 쓰고, `fnid` 0 이면 `0x2a4`, 사본 `+0x12 \|= 1`, 반환 1; **반환은 user32 가 안 봄**). `NtUserShowWindow` · `NtUserUpdateWindow` · `NtUserSetFocus` · `NtUserSetForegroundWindow` · `NtUserSetDialogSystemMenu` · `NtUserGetSystemMenu` · `NtUserSetWindowContextHelpId` · `NtUserDestroyMenu` — **반환 안 보니 아무 값 (0) 이면 됨** (몸 없는 기본 대답 0 으로 충분). `NtUserDestroyWindow` — 컨트롤 실패 때만 (N10) 불림; 반환 안 봄. `NtUserMessageCall` — 직접 길이 안 되는 메시지 · 컨트롤용 (6-3), 0 으로 충분.

### stub 이 채워야 하는 최소 목록 (run142 목록 7 · 8 을 채운 판)
1. **`NtUserGetAtomName(원자, USTR*)`**: 위 규칙 (`#32770` · 반환 6). 표 원자 (`0xc000` 이상) 는 0 아닌 값.
2. **공유 블록**: `gSharedInfo+0x0 == psi` · `gSharedInfo+0x8` 항목 배열 (hwnd 최대 번호까지) · `gSharedInfo+0x10` 항목 크기 (≥ `0x1c`, 보통 `0x20`) · `psi+0x8` 개수 · `gSharedInfo+0x18` pDispInfo (+`+8` 모니터, 139) · **`psi+0x364+2i` (i = 0..5) 시스템 클래스 원자 (0 아님)**.
3. **TEB**: `+0x78` = 스레드 ID (0 이면 항목 `+0x8` 도 0) · `+0x820` (≥ `0x48` B, `+0x0` 표지, `+0x8` 데스크톱 창, `+0x10` dword 0, 139) · `+0x828` (0 가능) · `+0x838` 0 · `+0x81c` 비트 `0x20000000` 0 · `+0x840/+0x848` 0 · `+0x810` (0 이면 옛 앱 취급 — 그래도 지나감).
4. **hwnd 한 개마다** (대화상자 + 컨트롤 + 만들어진 순서대로): (a) 항목: `+0x0` 사본 주소 − TEB`+0x828` · `+0x8` = TEB`+0x78` (직접 길 쓰려면) · `+0x10` = `[[TEB+0x820]]` · `+0x18` = 1 · `+0x19` = 0 · `+0x1a` = `(hwnd>>16)&0x7fff`; (b) 사본 (≥ `0x130` B, 0 채움 후): `+0x0` hwnd · `+0x8` 자기 주소 · `+0x1c` 스타일 · `+0x2a` 0 · `+0x12` (대화상자: 0 / 컨트롤: 비트 2 켬) · `+0x78` 창 프로시저 (**대화상자 = 진짜 `DefDlgProcW` 주소**) · `+0x80` 클래스 포인터 (대화상자만 필요; `+4` word = `0x2a4`) · `+0xc8` (대화상자 `0x1e` 이상) · `+0x128` 여분 블록 (`+0xc8` 바이트, 0 채움, user32 힙) · `+0x140` 컨트롤 ID; (c) 길 B 로 하려면 `psi+0x1d8/+0x298` 의 `fnid 0x2a4` 칸이 `사본+0x78` 과 다르게 (0 도 됨). 길 A 로 하려면 `psi+0x298`(W) = `사본+0x78`, `psi+0x320` = `DefDlgProcWorker` 주소.
5. **`NtUserCreateWindowEx`**: 둘째 · 셋째 인자 원자 `0x8002` (또는 서수에서 온 원자) 를 받아 hwnd + 항목 + 사본 + 여분 블록. 대화상자 클래스는 `+0xc8 = 0x1e`.
6. **`NtUserSetDialogPointer(hwnd, pdlg)`**: `*(qword*)([사본+0x128] + 8) = pdlg`, `사본+0x2a` 가 0 이면 `0x2a4`, `사본+0x12 \|= 1`.
7. 나머지 syscall (`ShowWindow` 등) 은 반환 무관.
8. (146 에서 보탬) 자식 연결 (`사본+0x30/+0x38/…`) · `WM_COMMAND` 입구.

### "되부름이 꼭 필요한가"
**아니오** — 단, 되부름 `0x7b` 가 하던 "사본 `+0x128` 에 0 채운 여분 블록" 은 stub 이 직접 만들어야 하고, 그 밖의 창 프로시저 되부름 (`WM_NCCREATE` 류) 은 안 해도 (a)~(e) 가 지나간다. 레 바퀴 (`Src/nt` 사용자 모드 되부름) 를 세울 필요는 이 길에서는 없음 `[정적]`.

## 10. 동적으로 확인할 것 (Q273 VM GUI 세션에 얹을 꼴)
1. **`NtUserGetAtomName(0x8002)` 직후** (`user32!ClassNameToVersion+0x22d` 에서 멈춤): `ax` (= 6) 와 `du poi(@rbp-0x59) L7` (= `#32770`), 그리고 `UNICODE_STRING.Length` 가 이 시점에 0 인지 (+0x235 에서 user32 가 쓰기 전).
2. **`user32!InternalCreateDialog+0x63f` 직전**: `rdx` (= `0x8002`), `r8` 가 가리키는 `LARGE_STRING`, 스택 `[rsp+0x20..0x70]` 17 인자 (스타일 · 부모 · hInstance · 끝 인자 `…|0x40000000`).
3. **`InternalCreateDialog+0x658` 직후** `rax` = 사본: `dq @rax L0x30` · `db @rax+0x12 L1` · `dw @rax+0x2a L1` · `dq @rax+0x78 L1` (창 프로시저 = `DefDlgProcW`?) · `dq @rax+0x80 L1` (클래스) · `dd @rax+0xc8 L1` (0x1e?) · `dq @rax+0x128 L1` · 항목 `dq poi(user32!gSharedInfo+8)+(hwnd&0xffff)*dwo(user32!gSharedInfo+10) L4` (`+0x8` 이 TEB `+0x78` 과 같은지, `+0x10` 이 `poi(poi(@$teb+0x820))` 와 같은지).
4. **`ValidateDialogPwnd+0x4f` · `+0x56`** (`NtUserSetDialogPointer` 앞뒤): `rdx` = `pdlg` (0x58 B 0 채움?) · `+0x128` 쪽 `[+8]` 가 뒤에 `pdlg` 로 바뀌었는지 · `@rax+0x2a` (`0x2a4`?) · `@rax+0x12` 비트 0.
5. **`win32kfull!xxxCreateWindowEx+0x1446` / `+0x1a46`** (커널 멈춤점, 프로세스 조건): 되부름 `0x7b` 의 입력 4 B (= `0x1e`?) 와 돌아온 포인터 → 사본 `+0x128`. `user32!__xxxClientAllocWindowClassExtraBytes+0x3a` (`rax` = 블록 주소).
6. **`SendMessageWorker+0xbc`** (`cmp r8,rax`): `r8` = TEB `+0x78`, `rax` = 항목 `+0x8` — 둘이 같은지, `+0xc5` 에서 사본 `+0x12` 비트 2, `+0x100` 에서 `[[teb+0x820]+0x10]`, `+0x3f3` 에서 `psi+0x298+…` 와 `사본+0x78` 비교 결과 (길 A / B 어느 쪽인지), `+0x8f7` 에서 일꾼 주소 (`DefDlgProcWorker` 인지), `+0x600` (길 B).
7. **`DefDlgProcWorker+0xbf`** (`UserCallDlgProcCheckWow` 앞): `rdx` = 앱 프로시저 = `[pdlg+0]`.
8. **컨트롤 고리**: `InternalCreateDialog+0x975` 에서 `rdx` (컨트롤 클래스 원자 값들 = `psi+0x364` 표) 와 `VerNtUserCreateWindowEx+0x177` 직후 `ClassNameToVersion` 반환 (바꿔치기 있나 — 앱 매니페스트가 comctl32 v6 이면 `rax` 가 원자가 아닌 글).
9. **`NtUserShowWindow` 호출 여부**: `+0xf7a` 에 멈춰 사본 `+0x1f` 비트 `0x10` · `pdlg+0x18` 비트 0; `UpdateWindow` 가 syscall 을 부르는지 (`+0xf89` → `UpdateWindow+0x3e`).
10. TEB `+0x78` 이 진짜 스레드 ID 인지 (139 와 겹침) · `+0x810` · `+0x840/+0x848` 의 값 · `gSharedInfo+0x10` 의 진짜 값 (항목 크기).

## 11. 다음 요청 후보 — **sast/146 dialog-end** 에 넘길 것
- 물음 3 그대로 (`DialogBox2` 고리 · `IsDialogMessageW` · `DispatchMessageWorker` · `EndDialog` 뒤 syscall · A 판 갈림 · 모달 아닌 판) + 이번에 확인된 **146 에서 읽을 칸**:
  - `DispatchMessageWorker +0xb6 TEST [사본+0x12],4` (같은 비트 2) · `+0xee MOVZX ECX,[RDI+0x12]` · `[[사본+0x128]+8]` → `pdlg+0x18` 비트 0 (끝남) · `+0x20` (**결과값 = 기본 단추 ID 겸용** — `EndDialog` 가 같은 칸에 씀) · `_GetDlgItem` / `_FindDlgItem` / `GetDlgItem` (컨트롤 ID = 사본 `+0x140`, 자식 연결 `+0x30 +0x38 +0x40?`) · `GetParentDialog +0x8` (사본 `+0x12` 비트 0).
  - `_SetWindowLongPtr`/`_GetWindowLongPtr` (`DWLP_USER` = 여분 블록 `+0x10` — 앱 대화상자 프로시저가 거의 늘 부름) · `_GetWindowLong +0x50` (`[+0x12]&1`).
  - A 판: `SendMessageWorker +0x14c~+0x225` (창 ANSI 비트 `+0x12` 비트 3 vs 보내는 쪽) · `MessageTable[msg] & 0x100` · 확장 스타일 `0x80000000` (`InternalCreateDialog +0x5de`) · `pdlg+0x18` 비트 2.
  - `DefWindowProcWorker` (`WM_GETDLGCODE` 0x87 가 앱 프로시저에서 0 으로 돌아오면 가는 길 — `DlgSetFocus` 가 대화상자 hwnd 자신에게 보냄).
  - 길 A (`psi+0x320`) 의 진짜 일꾼 (`NtUserInitializeClientPfnArrays` 쪽, win32kfull) — 146 에서 필요하면.
- 이번에 `[공백]` 으로 남긴 것: ① `_MonitorFromRect` 가 `pDispInfo+8 == 0` 일 때 정말 NULL 인지 (본문 안 읽음) ② 컨트롤 프로시저 (`ButtonWndProcWorker` 등) 본문 — 컨트롤 사본 비트 2 우회로 건너뛸 수 있음 ③ `CreateDlgFont` 의 gdi 호출 (Q278) ④ 사본 `+0x12` 비트 2 · 3 · 0 의 진짜 이름 (구조체 형식이 PDB 에 없음) ⑤ `pdlg+0x40` · `+0x50` 뜻 ⑥ `0x7b` 이외의 되부름이 컨트롤 사본 `+0x128` 을 채우는지 (컨트롤 클래스의 `cbWndExtra` — 기본 `8` 일 것 `[추측]`).

## 판정

**분석성공.** 물음 1 (0)(b)(c)(d)(e)(f) · 2 모두 줄 근거와 함께 답했고 (a) 는 표 한 장으로 닫음. 핵심 발견 둘: ① 정수 원자는 표를 안 찾고 `#%u` 로 지음 — `0x8002` → 반환 6 · `#32770`; ② 사본 `+0x128` (여분 블록) 은 **되부름 `0x7b`** 이 만든다 — 이것만 stub 이 대신하면 창 프로시저 되부름은 필요 없음. 정적으로 못 닫은 것 (컨트롤 프로시저 본문 · 사본 깃발 이름 · 모니터 NULL 끝 · 길 A/B 어느 쪽인지) 은 `[추측]`·`[공백]` 으로 표시하고 10절 동적 목록에 넘김. MS 코드는 레포에 붙이지 않았다 (`ms/disassemble/…`, `ms/decomp/…` 경로와 `+0x…` 만).

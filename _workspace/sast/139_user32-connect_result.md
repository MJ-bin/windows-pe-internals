# 139 user32-connect — 정적 분석 결과

- 요청서: `_workspace/sast/139_user32-connect.md` (원문 `../PE_emulator/_workspace/05_tuning_planner/335_internals-user32-connect.md` 먼저 읽음)
- 대기열: Q268. 정적만. 값 (`psi` 칸 내용·핸들 값) 은 하지 않음 (Q162 몫).
- 모듈 / 바이너리: user32 · win32kfull · win32kbase (이미 analyze 됨, 다시 안 함. `-readOnly` export·refs 만) + 읽기만 한 것: win32u `_syscalls.tsv`, ntoskrnl `.asm` (sast/138 이 쓰는 중이라 건드리지 않고 이미 있는 파일만 읽음)
- PDB GUID (`ms/sym/*.pdb.info.txt`):
  - ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준과 같음 (빌드 26100.9457 맞음)
  - user32 `CDB947D1B96B87977915DE04CF54279C1` · win32kfull `5CD57181BCFDC5AE8F4819BEB1196F091` · win32kbase `D22DFDA3D54B87E6BD0567A57877904C1` · win32u 는 `ms/sym/win32u.dll.pdb.info.txt`
- 뽑은 것 (모두 depth 0, 끝내는 데 user32 프로젝트 한 번에 4~5초씩, 오래 안 잡음):
  - 이 요청이 시작되기 전에 이미 있던 것 (17:30~17:34 export): user32 28개 (요청서 대상 + 도우미) · win32kfull `NtUserGetThreadState`. 그대로 읽음.
  - 이번에 더 뽑음: user32 14개 (`_PeekMessage` `HMValidateHandleNoSecure` `DispatchMessageWorker` `SendMessageW` `SendMessageWorker` `IsDialogMessageW` `GetCapture` `IsWindow` `CreateDlgFont` `GetMonitorRect` `EndTaskModalDialog` `StartTaskModalDialog` `PtiCurrent` `IsGUIThread`) + `GetDpiCacheIndex` `_MonitorFromWindowInternal`, win32kbase 3개 (`xxxCreateThreadInfo` `UserThreadCallout` `W32pThreadCallout`), win32kfull 2개 (`W32pThreadCallout` `xxxClientThreadSetup`).
  - `refs`: user32 `0xb65f8` (`NtUserGetThreadState` 가져오기 칸 — 이 함수 부르는 곳 전부) · `0xda9ec` (`gfSystemInitialized`), win32kfull `xxxClientThreadSetup`, win32kbase `xxxCreateThreadInfo`.
  - 이름이 안 맞은 것 없음. 스크립트 문제 없음 (맨 위 "스크립트 문제" 절 필요 없음).
- 버전 대조 (user32 는 "알려진 사실" 표에 없어서 run85·run86 에 적힌 줄과 맞춤): 전부 일치
  - `GetAppCompatFlags2 +0x12 CMP [TEB+0x78]` · `+0x17 JZ` · `+0x45/+0x4c NtUserGetThreadState` = run86 A13 과 같음
  - `__ClientThreadSetup +0x4/+0x9 → ClientThreadSetup`, `ClientThreadSetup +0x93/+0x9a GdiProcessSetup` · `+0x272/+0x279 NtUserRemoteConnectState` · `+0x29c/+0x2a3 NtUserGetThreadDesktop` · `+0x2c0/+0x2c7 NtUserRegisterLPK` · `+0x356/+0x35d NtUserGetProcessUIContextInformation` = run85·86 과 같음
  - win32u `NtUserGetThreadState` = syscall `0x1000` (win32u rva `0x12b0`) = run85 과 같음
  - ntoskrnl `KiSystemServiceStart +0x91f/+0x924 → KiConvertToGuiThread`, `KiConvertToGuiThread +0x4/+0x9 → PsConvertToGuiThread` = run85 과 같음
  - 요청서의 rva · 크기 (`MessageBoxWorker` 0x5f1d4 949B, `SoftModalMessageBox` 0x21f20 2174B, `InternalDialogBox` 0x20ae8 376B, `LoadIcoCur` 0x618c 772B, `NtUserGetThreadState` win32kfull 0x1ec5b0 705B 등) 전부 `.asm` 헤더·`_index.tsv` 와 맞음

## 먼저 읽는 한 줄 답

1. **가드**: 넷 다 **같은 함수를 안 부른다.** 같은 모양 5줄을 각자 안에 박아 두었다 — `TEB+0x78 == 0` 이면 `NtUserGetThreadState(0xe)` 를 부르고, **반환이 0 이면 실패(0 / −1)**, 0 아니면 계속 (syscall 뒤에 TEB+0x78 을 다시 읽지 않는다). 그래서 에뮬레이터의 기본 대답 0 이 넷 모두를 정확히 그 가지로 보낸다. (자리: `MessageBoxWorker +0x58~+0x75` · `InternalCreateDialog +0x13c~+0xd50` · `LoadIcoCur +0x52~+0x258` · `RealGetSystemMetrics +0x19~+0x131`.) 함수 밖에 있는 같은 모양 = `PtiCurrent` (0x19d10).
2. **가장 짧은 성공 길**: 가드만 풀어서는 **안 된다.** 가드(반환 1) 바로 뒤에서 `TEB+0x820` 이 0 이면 접근 위반 (`SoftModalMessageBox +0x372/+0x385`), `psi+0x1b56` 이 0 이면 0 나눔 (`GetMessageBoxFontForDpi +0xa6`), `gSharedInfo+0x18` (pDispInfo) 이 0 이면 접근 위반 (`GetPrimaryMonitor`). 그 뒤 GDI 답 (DC · 글꼴 · 글자 폭) 이 0 이면 `MessageBoxW` 는 조용히 0 으로 돌아온다 (`SoftModalMessageBox +0x153`). 성공 길의 win32k syscall 차례와 모달 고리 끝나는 조건은 아래 표 (2)·(4). **고리는 `EndDialog` 가 불려야 끝나고, 그 `EndDialog` 는 `MB_DlgProc` 가 `WM_COMMAND` (단추 번호) 를 받을 때 불린다 — 입력 메시지는 `NtUserPeekMessage` 로만 들어온다.**
3. **`NtUserGetThreadState` 종류 `0xe`**: 아무 칸도 안 읽고 **상수 `1` 을 돌려주는** 갈래다 (`win32kfull +0x11 CMP ECX,0xe` · `+0x14 JZ → +0x16c MOV EAX,1`). 락도 안 잡는다 (`EnterSharedCrit` 보다 앞). 정상 반환은 **0 이 아니라 1**. 목적은 반환값이 아니라 **부수 효과** — win32k syscall 이므로 **커널이 그 전에** 스레드를 GUI 로 바꾼다 (`KiSystemServiceStart +0x91f KiConvertToGuiThread`). 그 변환이 TEB 칸 (`+0x78` · `+0x800` 이후) 을 채우고 `__ClientThreadSetup` 을 되부른다.

## (1) 가드 표 — 실패로 나가는 첫 가지

공통 모양 (넷 다): `GS:[0x30]` → `CMP qword [TEB+0x78],0` → `JZ` → `MOV ECX,0xe` → `CALL [user32 IAT 0xb65f8]` (= win32u `NtUserGetThreadState`, syscall `0x1000`) → `TEST RAX,RAX` → `JNZ 계속` / 0 이면 아래 반환.

| API | 가드가 든 함수 | 읽는 칸 | 실패 조건 | 반환 | 줄 `[정적]` |
|---|---|---|---|---|---|
| `MessageBoxW` (0x5ed90) | `MessageBoxWorker` (0x5f1d4). 길: `MessageBoxW +0x40/+0x45` → `MessageBoxTimeoutW` (0x5edf0) `+0x18a/+0x18f` → `MessageBoxWorker` | TEB `+0x78` (qword) · syscall 반환 | TEB+0x78 == 0 **그리고** `NtUserGetThreadState(0xe)` 반환 == 0 | **0** (`FALSE` 아님, 그냥 0) | `MessageBoxWorker +0x48 GS:[0x30]` · `+0x58 CMP [RAX+0x78],R13` · `+0x5c JNZ → +0x7a` · `+0x5e LEA ECX,[R13+0xe]` · `+0x62/+0x69 CALL` · `+0x6e TEST` · `+0x71 JNZ → +0x7a` · `+0x73 XOR EAX,EAX` · `+0x75 JMP → +0x388` |
| `MessageBoxA` (0x96e40) | 같음. 길: `+0x40/+0x45` → `MessageBoxTimeoutA` (0x96f90) → `MBToWCSEx` ×2 → `+0xfb/+0x100 MessageBoxTimeoutW` → `MessageBoxWorker` | 같음 | 같음 | 0 | 위와 같음 (A→W 변환 쪽엔 가드 없음) |
| `MessageBoxW` 안 겹친 가드 | `SoftModalMessageBox` (0x21f20) | TEB+0x78 · syscall 반환 | 같음 | 0 | `+0x4f CMP [RAX+0x78],RCX` · `+0x53 JZ → +0x659` · `+0x65e/+0x665 CALL` · `+0x66d JNZ → +0x59` · `+0x673 JMP → +0x5ff` (EAX = R15D = 0). Worker 가 이미 지나갔으면 TEB+0x78 이 그대로 0 인 에뮬레이터에선 **같은 syscall 을 한 번 더** 부른다 |
| `MessageBoxTimeoutW` 안 두 호출 | (`+0xd2/+0xd9` · `+0x12c/+0x133`) | `GS:[0x30] + 0x800` 이 0 인가 | `+0xcb ADD RAX,RBX(0x800)` · `+0xce JNZ` — **TEB 주소 + 0x800 은 0 이 될 수 없어** 이 두 호출은 사실상 죽은 가지 | — | 로그의 "`MessageBox*` 안 syscall 하나뿐" 과 맞음 (Worker +0x62 하나) |
| `DialogBoxParamW` (0x66260) | `InternalCreateDialog` (0x1eb90). 길: `DialogBoxParamW +0x6d/+0x72` → `DialogBoxIndirectParamAorW` (0x621b0) `+0x67/+0x6c` → `InternalDialogBox` (0x20ae8) `+0x70/+0x75` → `InternalCreateDialog` | TEB `+0x78` · syscall 반환 | 같음 → `InternalCreateDialog` 가 NULL → `InternalDialogBox +0x78 JZ` | **−1** (`InternalDialogBox` 가 −1, 그대로 위로) | `InternalCreateDialog +0x133 GS:[0x30]` · `+0x13c CMP [RAX+0x78],RDI` · `+0x140 JZ → +0xd37` · `+0xd37 MOV ECX,ESI(0xe)` · `+0xd39/+0xd40 CALL` · `+0xd45 TEST` · `+0xd48 JNZ → +0x146` · `+0xd4e XOR EAX,EAX` · `+0xd50 JMP → +0xbb9`. 그다음 `InternalDialogBox +0x75 call/ret` · `+0x78 JZ → +0x151` · `+0x153 JNZ`/`+0x15c JZ` → 조각 `FUN_1800ab7c2 +0x0 OR RAX,-1` · `+0x4 JMP → InternalDialogBox +0x8f` |
| `DialogBoxParamW` 의 **앞쪽** 같은 −1 (가드 아님) | `DialogBoxParamW +0x2a/+0x31 FindResourceExW` · `+0x41/+0x48 LoadResource` · `DialogBoxIndirectParamAorW +0x20/+0x25 GetSizeDialogTemplate` | 대화상자 자원 / 템플릿 | 자원 못 찾음 · 템플릿 크기 0 | −1 | `DialogBoxParamW +0x39 JZ` · `+0x53 JZ` (RBX = −1, `+0x1d OR RBX,-1`) · `DialogBoxIndirectParamAorW +0x27 JNZ` 안 타면 `+0x29 OR RAX,-1` |
| 그 앞의 "두 번째" syscall | `InternalDialogBox +0xaf/+0xb4` → `GetDesktopWindow` (0xf890) | TEB+0x78 · syscall 반환 | 부모 hwnd (`R8`) ≠ 0 일 때만 (`+0x2d TEST R8,R8` · `+0x30 JNZ → +0xaf`) | 데스크톱 NULL (그다음 `InternalCreateDialog` 가 또 한 번) | `GetDesktopWindow +0x15 CMP [RAX+0x78],RBX` · `+0x19 JZ → +0x80` · `+0x85/+0x8c CALL` · `+0x94 JNZ → +0x1b` · `+0x96 MOV RDI,RBX` · `+0x99 JMP → +0x36`. 로그의 "3643 · 3644 (`NtUserGetThreadState(0xe)` 둘) → 3645 (−1)" = 이 둘 `[추측: 부모 hwnd 가 0 이 아니었다면. 인자 확인 필요]` |
| `LoadIconW` (0x559e0) | `LoadIcoCur` (0x618c). 길: `LoadIconW +0x18/+0x1d` (type 3 = 아이콘, 플래그 `0x8040`) | TEB `+0x78` · syscall 반환 | 같음 | **0** | `LoadIcoCur +0x2d GS:[0x30]` · `+0x52 CMP [RAX+0x78],R8` · `+0x56 JZ → +0x239` · `+0x239 MOV ECX,0xe` · `+0x23e/+0x245 CALL` · `+0x24d TEST` · `+0x250 JNZ → +0x5c` · `+0x256 XOR EAX,EAX` · `+0x258 JMP → +0x138` |
| `LoadIconW` 의 **syscall 없는** 0 | `LoadIcoCur +0x290~+0x2a9` | TEB `+0x810` (dword) · 표 칸 `[RCX+4]` 비트 0 | 자원 번호가 user32 안 표에 있고 (`+0x22d CMP [RCX],R14W` · `+0x231 JZ → +0x290`) 그 칸 비트 0 이 켜졌고 `TEB+0x810 >= 0x400` | 0 | `+0x290 TEST byte [RCX+4],1` · `+0x294 JZ` · `+0x29f CMP [RAX+0x810],0x400` · `+0x2a9 JNC → +0x256` (가드와 같은 반환 자리). 로그의 "`LoadIconW` syscall 0 개" 는 가드 길이 아니라 **이 길이거나** 그때 TEB+0x78 이 이미 ≠ 0 이었을 것 `[추측 — 로그 seq 2766~2768 에서 TEB+0x78 · 0x810 확인 필요]` |
| `GetSystemMetrics` (0x19080) | `RealGetSystemMetrics` (0x8700). 길: `GetSystemMetrics +0x75/+0x7a` (`CALL guah+0x90`, 파일 처음 값 `0x180008700` 으로 확인) 또는 `+0xd3` (지표 `0x5a` · `0x2000` 직행) | TEB `+0x78` · syscall 반환 | 같음 | **0** | `RealGetSystemMetrics +0xd GS:[0x30]` · `+0x19 CMP [RAX+0x78],0` · `+0x1e JZ → +0x117` · `+0x117 MOV ECX,0xe` · `+0x11c/+0x123 CALL` · `+0x128 TEST` · `+0x12b JNZ → +0x24` · `+0x131 XOR EAX,EAX` · `+0x133~+0x140 RET`. 로그 "`GetSystemMetrics(0x1000)` 0": `+0x2d CMP EBX,0x1000` · `+0x33 JZ → +0x233` 은 가드 뒤라 가드가 먼저 0 을 냄 |
| `GetSystemMetrics` 가드 **앞** 읽기 | `GetSystemMetrics +0x9e/+0xa5` | `gpsi+0x0` 비트 `0x10` | 가드 아님. 지표 `0x5a`·`0x2000` 말고는 늘 `psi` 를 읽는다 — `psi` NULL 이면 접근 위반. 비트가 켜지면 `NtUserLoadUserApiHook` (0x14cc) | — | `+0x21 CMP gfUserApiHook` · `+0x28 JZ → +0x93` · `+0x44 TEST EAX` · `+0x46 JZ → +0x9e` · `+0x9e MOV RAX,[gpsi]` · `+0xa5 TEST byte [RAX],0x10` · `+0xba/+0xc1 NtUserLoadUserApiHook` |

**같은 도우미가 있나**: 넷은 안 쓴다. 이 모양은 컴파일러가 각 함수 안에 풀어 넣은 것이다. 같은 모양의 함수 하나로 남은 것: `PtiCurrent` (0x19d10, 62B — `+0xd CMP [TEB+0x78]` · `+0x12 JZ → +0x27` · `+0x2c/+0x33 CALL` · `+0x3b JNZ → +0x14` 로 **되돌아가 TEB+0x78 을 다시 읽음** · 아니면 `+0x3d JMP → +0x31` 로 0 반환) · `IsGUIThread` (0x52210, 인자 `bConvert` 가 참일 때만 syscall, 0 이면 `SetLastError(0xe = ERROR_OUTOFMEMORY)`). 이 `NtUserGetThreadState` 가져오기 칸 `0xb65f8` 을 부르는 곳은 user32 전체 **85곳** (직접 `CALL` 79 + `GetCapture` 같은 `JMP` 껍데기 6) — `_refs/0xb65f8.tsv`. 그러니 **고칠 자리는 syscall 대답 하나** (`0x1000` 의 인자 `0xe`).

## (2) 최소 길 표 — `MessageBoxW(NULL, 글, 제목, MB_OK)` 한 번

전제: 가드를 지났다 (TEB+0x78 == 0 이면 `NtUserGetThreadState(0xe)` → 1). 번호는 `ms/disassemble/win32u/_syscalls.tsv`. **오프셋 차례는 `.asm` 위치 순서다 (실행 순서와 거의 같지만 조건은 `[공백]` 로 표시한 곳이 있음).** gdi32 안으로 들어가는 것은 이름만 (sast/141 몫).

| 차례 | syscall (번호) | 인자 요점 | 읽는 칸 (syscall 없이) | 줄 `[정적]` |
|---|---|---|---|---|
| 0 | 없음 | `MessageBoxW` 가 `dwMilliseconds = -1`, `wLanguageId = 0` 으로 `MessageBoxTimeoutW` 호출. `memset(0xa0)` 으로 `MSGBOXDATA` (cbSize `0x50`) 채움 | `gfEMIEnable` (=0 이면 건너뜀) · `gfServerProcess` · TEB `+0x8e8` (0 이면 `gDefaultDpiContext`, 파일 처음 값 `0x6010` → 하위 4비트 0 ≠ 2 라 `SetThreadDpiAwarenessContext(-2)` 안 탐) | `MessageBoxW +0x35 OR [RSP+0x28],-1` · `+0x3a MOV [RSP+0x20],R11W` · `MessageBoxTimeoutW +0x3c memset` · `+0xaa gfServerProcess` · `+0xe3~+0x110` (TEB+0x8e8 읽기 · `AND AL,0xf` · `CMP AL,2` · `JNZ → +0x16c`) · `+0x172 TEST EAX` · `+0x174 JZ → +0x185` · `+0x18a/+0x18f MessageBoxWorker` |
| 1 | `NtUserGetThreadState` (`0x1000`) 종류 `0xe` → **1** | 가드 (위 표). 이후 `SoftModalMessageBox +0x65e` 에서 TEB+0x78 이 그대로 0 이면 한 번 더 | TEB `+0x78` | `MessageBoxWorker +0x62/+0x69` |
| 2 | 없음 | 검사: `uType & 0xf > 6` 이면 `SetLastError(0x59e)` 후 0. `gfDMREnable` ≠ 0 이면 `psi` 표 값을 그대로 돌려줌 (정상은 0). 제목 NULL 이면 `LoadStringBaseExW` 로 "오류" 문구. 단추마다 글자 만들기: `TextAlloc` (= `pUserHeap` 에 `RtlAllocateHeap`) | **`psi` 단추 글 표** `psi + 40*SEBbuttons[k] + 0x3a4` (글자 16개 = 0x20B) · `+0x3c4` (dword, 명령 번호) · `+0x3c8` (dword, 문자열 id). `MB_OK` 단추 하나 | `MessageBoxWorker +0x84~+0x8d` · `+0xcf CMP gfEMIEnable` · `+0xe8 CMP gfDMREnable` · `+0xf1~+0x10f` · `+0x11b~+0x1a7` · `+0x266 MOV RAX,[gpsi]` · `+0x283 MOV EDX,[RSI+0x3c8]` · `+0x2c8 MOV EAX,[RSI+0x3c4]` · `+0x2be/+0x2c3 TextAlloc` |
| 3 | `NtUserModifyUserStartupInfoFlags` (`0x10f9`) | `(1, 0)` — 이 스레드의 시작 정보 플래그 중 `STARTF_USESHOWWINDOW` 비트를 지움 `[추측: 뜻]` | — | `MessageBoxWorker +0x304 XOR EDX,EDX` · `+0x306 MOV ECX,ESI` · `+0x308/+0x30f CALL` |
| 4 | — | `SoftModalMessageBox` 로 넘어감 (`MSGBOXDATA +0x60/+0x68/+0x70/+0x74/+0x78` 에 단추 글·개수 적어서) | — | `MessageBoxWorker +0x349/+0x34e` |
| 5 | (아이콘 유형일 때만) `NtUserFindExistingCursorIcon` (`0x1039`) 이하 `LoadIcoCur` 길 | `MB_OK` 는 아이콘 없음이라 건너뜀 (`+0xb7 CMP EBX,0x80` · `+0xbd JNZ → +0x61c`) | — | `SoftModalMessageBox +0xb1 AND EBX,0xf0` · `+0xdd/+0xe2 LoadIcoCur` |
| 6 | `NtUserGetDCEx` (`0x108a`) | `(hwnd=0, hrgn=0, flags=3)` = 화면 DC. **0 이면 한 번 더 (`hwnd = MSGBOXDATA+8`) 하고 그래도 0 이면 `MessageBoxW` 가 0** | — | `+0xf5/+0xfc CALL` · `+0x104 TEST` · `+0x107 JZ → +0x636` · `+0x63f/+0x646` · `+0x651 JNZ` · `+0x657 JMP → +0x5ea` |
| 7 | `NtUserGetDpiForCurrentProcess` (`0x1440`) — 처음 한 번만 | `GetDpiForSystem` (0x6de0) → `GetDpiForCurrentProcess` (0x6e90). 돌려준 값이 `0x60` 보다 작으면 `0x60` 으로 올려 캐시 | **`psi+0x1b66`** (word) vs 캐시 `gPackedProcessDpiInfo` 윗 16비트. `GetDpiForSystem` 은 `+0x78 IsThreadDesktopComposited` 에서 **TEB+0x78 가드 한 번 더** · `[TEB+0x820]+0x40` 비트 0 | `SoftModalMessageBox +0x10d/+0x112` · `GetDpiForCurrentProcess +0x13 · +0x28/+0x2f` · `GetDpiForSystem +0x78/+0x7d` · `IsThreadDesktopComposited +0x11 · +0x20 · +0x2c` |
| 8 | (gdi32 이름만) `CreateFontIndirectW` → `NtGdiHfontCreate` (`0x1057`) | 메시지 상자 글꼴. **`psi+0x1b56` 이 0 이면 0 나눔** (`IDIV R8D`) | `psi+0x138c · +0x139c · +0x13ac · +0x13bc · +0x13cc · +0x13dc · +0x13e4` (LOGFONT 모양, 약 0x5c B) · **`psi+0x1b56` (word, 0 아님)** | `SoftModalMessageBox +0x114/+0x119` · `GetMessageBoxFontForDpi +0x37 MOVZX R8D,[RDX+0x1b56]` · `+0x8f/+0xa6 IDIV` · `+0xb2/+0xb9 CreateFontIndirectW` |
| 9 | (gdi32 이름만) `SelectObject` · `UserGetCharDimensionsEx` (`GetTextMetricsW` → `NtGdiGetTextMetricsW` `0x106e`, `GdiGetCharDimensions`) · `GetTextExtentPointW` (→ `NtGdiGetTextExtentExW` `0x12e4`) · `DeleteObject` (→ `NtGdiDeleteObjectApp` `0x1020`) | **글자 폭 0 이면 `MessageBoxW` 가 0 으로 돌아온다**. HDC 가 GDI 공유 표에 있어야 gdi32 가 읽음 (PEB `+0xf8`, sast/141) | — | `SoftModalMessageBox +0x126 · +0x149/+0x14e · +0x151 TEST EDX` · `+0x153 JZ → +0x69f` · `+0x16f/+0x174 MB_GetMaxButtonSize` · `+0x249/+0x250 GetTextExtentPointW` · `+0x2e0/+0x2e7 DeleteObject` |
| 10 | `NtUserEnsureDpiDepSysMetCacheForPlateau` (`0x141c`) — 칸이 −1 일 때만 | `GetDpiDependentMetric` (0x6498) 이 여러 번 (+0x1b6 · +0x1d7 · +0x255 · +0x3ae · +0x4da 에서 지표 5 · 6 · 0xc · 0xd · 2) | **`psi + 4*(캐시번호*0x1e + 지표) + 0x8ec`**. 캐시번호는 `GetDpiCacheIndex` (`psi+0x1b56` 과 같으면 0, 아니면 `(dpi-0x48)/6/…`). 0 으로 채우면 syscall 없이 지표 0 이 나옴 | `GetDpiDependentMetric +0x41 MOV R8D,[RAX+RSI*4+0x8ec]` · `+0x4f/+0x58 CALL` · `GetDpiCacheIndex +0xe` |
| 11 | `NtUserGetForegroundWindow` (`0x1037`) | `GetDialogMonitor(owner=NULL, 1)` — 주인 창이 없으니 앞 창을 물어봄. 0 이면 주 모니터 | **`gSharedInfo+0x18` (= `pDispInfo`, user32 rva `0xda368`) → `[+8]` (주 모니터, 커널 쪽 주소) + `gSharedInfo+0x20` (rva `0xda370`, 공유 차이)**. 모니터 물체 `+0x1c`·`+0x2c` (RECT 16B) · `+0x3c`·`+0x3e` (word). **`pDispInfo` 가 0 이면 `[0+8]` 접근 위반** | `SoftModalMessageBox +0x1fa/+0x1ff` · `GetDialogMonitor +0x18 TEST RCX` · `+0x1b JZ → +0x60` · `+0x60/+0x67 CALL` · `+0x79/+0x7e GetPrimaryMonitor` · `GetPrimaryMonitor +0x0 · +0x7 · +0x12` · `SoftModalMessageBox +0x20a/+0x20f GetMonitorWorkRect` · `GetMonitorWorkRect +0x6c MOVUPS [R15+0x2c]` |
| 12 | `NtUserEnsureDpiServerInfoForDpi` (`0x141e`) — 조건부 | `GetDpiServerInfoForCurrentThread` (0xa777c): 결과 `+0x8` 이 HFONT 로 `SelectObject` 에 들어감 | `psi + (캐시번호+0x31)*0x68` (+0x0 dword, +0x8 qword). 기능 켜짐 · 번호 > 1 · +0x0 == 0 일 때만 syscall | `SoftModalMessageBox +0x212/+0x217 · +0x21a MOV RDX,[RAX+8]` · `GetDpiServerInfoForCurrentThread +0x41 · +0x59 · +0x5e · +0x66/+0x6b` |
| 13 | `NtUserReleaseDC` (`0x151c`) | 6 의 DC 놓기 | — | `SoftModalMessageBox +0x2ef/+0x2f6` |
| 14 | 없음 | **`[TEB+0x820]` 이 가리키는 곳의 `+0x30` (dword) 읽기** → 상자 위치 계산. **TEB+0x820 이 0 이면 접근 위반** | TEB `+0x820` (`ClientInfo` `+0x20` — `pDeskInfo` 로 읽힘 `[추측: 이름]`) 와 그 `+0x30` | `SoftModalMessageBox +0x365 GS:[0x30]` · `+0x372 MOV RCX,[RAX+0x820]` · `+0x385 MOV EBX,[RCX+0x30]` |
| 15 | 없음 | 템플릿 만들기: `MB_FindDlgTemplateSize` → `RtlAllocateHeap(pUserHeap)` → `MB_UpdateDlgHdr` (스타일 `0x80c801c5` 또는 `0x80c80347`, `DS_SETFONT` 켜짐) → `MB_AddPushButtons` → `MB_UpdateDlgItem` | `pUserHeap` (= PEB `+0x30` 힙, run85) · `psi` 단추 글 | `SoftModalMessageBox +0x40e/+0x413 · +0x422/+0x429 · +0x438 JZ → +0x5ea` · `+0x4b9/+0x4be · +0x511/+0x516` |
| 16 | `NtUserFindExistingCursorIcon` (`0x1039`) · `NtUserSetCursor` (`0x1018`) | 화살표 커서 (`IDC_ARROW 0x7f00`, 플래그 `0x8040` = LR_SHARED·DEFAULTSIZE). `LoadIcoCur` 는 **`FindExisting` 이 0 아닌 값을 돌려주면 자원 읽기 없이 그 핸들로 끝낸다** (`+0x1f4 TEST RAX` · `+0x1f7 JNZ → +0x135`). 0 이면 `ObjectFromDIBResource` (user32 의 `.rsrc` 읽기 + 커서 만들기 syscall `[공백]`) | `LoadIcoCur` 도 같은 TEB+0x78 가드 | `SoftModalMessageBox +0x550/+0x555 LoadIcoCur` · `+0x558/+0x55f NtUserSetCursor` · `LoadIcoCur +0x1e5/+0x1ec NtUserFindExistingCursorIcon` |
| 17 | `NtUserMessageBeep` (`0x14dd`) | 아이콘 유형 (`uType & 0xf0 ≠ 0`, `MB_USERICON` 아님) 일 때만. `MB_OK` 는 안 부름 | — | `SoftModalMessageBox +0x564 TEST [RSI+0x28],0x80` · `+0x57f JZ → +0x58d` · `+0x581/+0x588` |
| 18 | `InternalDialogBox` 호출 | `(hmodUser, 템플릿, 주인=NULL, MB_DlgProc, 데이터, 0)` | — | `SoftModalMessageBox +0x5b1/+0x5b6` |
| 19 | (psi+0x7b4 ≠ 0 일 때만) `NtUserSetCursor` | 대기 커서 `IDC_WAIT 0x7f02`. 0 이면 건너뜀 | **`psi+0x7b4`** (dword) | `InternalDialogBox +0x32 MOV RAX,[gpsi]` · `+0x3e CMP [RAX+0x7b4],0` · `+0x45 JNZ → +0xc6` · `+0xe0/+0xe5 · +0xe8/+0xef` |
| 20 | 가드 한 번 더 (TEB+0x78 == 0 이면) | `InternalCreateDialog` 입구 | 위 표 (1) | `InternalCreateDialog +0x133 ~ +0x146` |
| 21 | `NtUserCreateWindowEx` (`0x106f`) | **대화상자 창 한 개**. 클래스 = 대화상자 기본 클래스 (템플릿 클래스 0) | — | `InternalCreateDialog +0x63f/+0x644 VerNtUserCreateWindowEx` → `VerNtUserCreateWindowEx +0x22f/+0x236 NtUserCreateWindowEx` |
| 22 | `NtUserValidateHandleSecure` (`0x15ce`) · `NtUserMapDesktopObject` (`0x14d9`) — 칸이 안 맞을 때만 | 새 hwnd 를 user32 가 검사: **공유 핸들 표를 syscall 없이 읽음** (아래 표 (5)) | 공유 핸들 표 · TEB+0x820/0x828 | `InternalCreateDialog +0x653/+0x658 ValidateHwnd` · `ValidateHwnd +0x3c ~ +0xe7` |
| 23 | `NtUserSetDialogPointer` (`0x1552`) | 대화상자 자료 (user32 의 옆 목록에서 받은 블록) 의 포인터를 창에 붙임. 그 포인터가 나중에 `EndDialog`·`DialogBox2` 가 읽고 쓰는 칸 | `AllocLookasideEntry` / `InitLookaside` | `InternalCreateDialog +0x667/+0x66c ValidateDialogPwnd` → `ValidateDialogPwnd +0x4f/+0x56` |
| 24 | `NtUserCreateWindowEx` (`0x106f`) ×(컨트롤 수) | `MB_OK`: 글 (static) + 단추 (button) = 최소 2개. 아이콘 유형이면 아이콘 static 하나 더. 클래스는 번호 (`0x0080` button · `0x0082` static) | — | `InternalCreateDialog +0x975/+0x97a VerNtUserCreateWindowEx` (컨트롤 고리) |
| 25 | `NtUserMessageCall` (`0x1005`) — 같은 스레드 창이면 user32 안에서 바로 창 프로시저 호출 `[공백: 어느 쪽인지]` | 글꼴 알림 · `WM_INITDIALOG` | 창 물체의 클래스·프로시저 칸 | `InternalCreateDialog +0x9a7 · +0x9ba SendMessageW` · `+0xa64/+0xa69 SendMessageWorker` · `SendMessageW +0xdd indirect:cfg:[표+0xb4230]` · `+0x10a/+0x111 NtUserMessageCall` |
| 26 | `NtUserSetMsgBox` (`0x1573`) · `NtUserSetFocus` (`0x104c`) · (타임아웃 있을 때만) `NtUserSetTimer` (`0x1016`) | `MB_DlgProc` 가 `WM_INITDIALOG` (`0x110`) 를 받을 때. `MessageBoxW` 는 타임아웃 −1 이라 타이머 없음 | `MSGBOXDATA +0x7c` (타임아웃) · `+0x38` (도움말 id) | `MB_DlgProc +0x22 SUB EDX,0xf` 부터 `+0x65 CMP EDX,0x1c9` 까지 (메시지 번호 풀이) · `+0xec/+0xf3` · `+0x197/+0x19e` · `+0x1d4/+0x1db` |
| 27 | `NtUserShowWindow` (`0x1052`) · `NtUserSetForegroundWindow` (`0x155b`) · `NtUserSetWindowContextHelpId` (`0x1595`) · `NtUserSetDialogSystemMenu` (`0x1553`) · `NtUserIsTopLevelWindow` (`0x14c0`) 따위 | 창 보이기·앞으로·메뉴. **조건은 `[공백]`** (스타일 비트·`DS_*`·DPI 방송 기능) | — | `InternalCreateDialog +0xf7a/+0xf81 NtUserShowWindow` · `+0xf89 UpdateWindow` · 조각 `FUN_18001fbc1 +0x292 · +0x2ae · +0x366 · +0x142 · +0x159` |
| 28 | `NtUserPeekMessage` (`0x1001`) · `NtUserWaitMessage` (`0x100a`) (고리) | `DialogBox2` 의 모달 고리. 메시지 없으면 **`NtUserWaitMessage` 에서 잠** | TEB `+0x800+0x60` · `+0x8` · `+0x1c` (ClientInfo 쪽 칸) · KUSER `+0x320`·`+0x4` (틱) · **`psi+0x88c` (byte)** · **`psi+0x764` 비트 `0x8000`** | `InternalDialogBox +0x8a/+0x8f DialogBox2` · `DialogBox2 +0x26b/+0x270 _PeekMessage` → `_PeekMessage +0x38/+0x3f NtUserPeekMessage` · `+0x23f/+0x246 NtUserWaitMessage` · `+0xa8 ~ +0xb6` · `+0x148 ~ +0x1ab` |
| 29 | `NtUserDispatchMessage` (`0x1031`) · `NtUserValidateTimerCallback` (`0x1014`) — 조건부 | `IsDialogMessageW` → `TranslateMessage` → `DispatchMessageWorker` (같은 프로세스 창이면 user32 안에서 창 프로시저를 바로 부름) | 창 물체의 프로시저 칸 | `DialogBox2 +0x29b · +0x2a9 · +0x2b5/+0x2ba` · `DispatchMessageWorker +0x1d8 · +0x27f · +0x309 UserCallWinProc…` · `+0x341/+0x348 · +0x40e/+0x415` |
| 30 | 끝: `EndDialog` 안 `NtUserSetWindowPos` (`0x1021`, 플래그 `0x97` = 숨기기) · `NtUserSetActiveWindow` (`0x10cd`) 또는 `NtUserZapActiveAndFocus` (`0x15d6`) · `NtUserPostMessage` (`0x100d`, `WM_NULL`) · 고리 끝난 뒤 `NtUserDestroyWindow` (`0x1095`) | 아래 (4) | 대화상자 자료 `+0x18` 비트 0 (끝남 표시) · `+0x20` (결과 값) | `EndDialog +0xa9/+0xb0 · +0xc2/+0xc9 · +0xd9/+0xe0 · +0x113/+0x11a` · `DialogBox2 +0x3c4/+0x3cb` |
| 31 | `NtUserSetCursor` (`0x1018`) | 처음 커서로 되돌림 (16 이 돌려준 옛 커서가 0 아닐 때) | — | `SoftModalMessageBox +0x5cd TEST RBX` · `+0x5d0 JNZ → +0x625` · `+0x628/+0x62f` |
| 32 | 반환 | `MessageBoxW` 반환 = `EndDialog` 가 넣은 결과 (단추 번호, `MB_OK` 는 `IDOK=1`). `InternalDialogBox` 가 −1 이면 0 | — | `SoftModalMessageBox +0x5b8 CMP EAX,-1` · `+0x5be CMOVNZ R15D,EAX` · `+0x5fc MOV EAX,R15D` · `DialogBox2 +0x3bd MOV RSI,[RSI+0x20]` · `+0x3ea MOV RAX,RSI` |

**이 표에서 `[공백]` 으로 남긴 것**: ① 22~29 사이 어느 호출이 같은 스레드 안에서 syscall 없이 풀리는지 ② `NtUserCreateWindowEx` 가 hwnd 를 돌려줄 때 커널이 핸들 표·창 물체를 어떻게 만드는지 (커널 쪽 — 이 요청 범위 밖) ③ 단추 하나 + 글 하나가 정확히 몇 번의 `NtUserCreateWindowEx`·`NtUserMessageCall` 인지 (동적 Q162 로 세는 쪽이 빠름). `InternalCreateDialog` 는 4144B 라 갈래가 많고 이번엔 틀만 잡았다.

## (3) 종류 `0xe` — 한 줄 + 부수 효과

- **뜻**: 어느 칸도 돌려주지 않는다. **고정 반환 `1`**. `win32kfull!NtUserGetThreadState` (rva `0x1ec5b0`) `+0xf MOV EBX,ECX` · `+0x11 CMP ECX,0xe` · `+0x14 JZ → +0x16c` · `+0x16c MOV EAX,1` · `+0x171 JMP → +0x97` (끝). `EnterSharedCrit` (`+0x21`) 앞이라 사용자 임계구역도 안 잡는다. 나머지 갈래 (종류 0~0xd) 는 `W32GetUserSessionState`·현재 스레드 칸을 읽는다 (예: 종류 2 = `GetCapture`) — 이번 요청 밖.
- **정상 반환이 0 인가**: **아니다. 1.** 0 은 user32 가 실패로 읽는다 (`PtiCurrent` · `IsGUIThread` 가 0 이면 `SetLastError(0xe = 14)` — "메모리 부족" 으로 읽음).
- **부수 효과가 목적인가**: **그렇다.** win32k syscall (번호 ≥ `0x1000`) 은 부르는 순간 커널이 먼저 스레드를 GUI 로 바꾼다. 순서 `[정적]`:
  1. `ntoskrnl!KiSystemServiceStart +0x91f/+0x924 → KiConvertToGuiThread` (표 인덱스 `EDI == 0x20` 일 때, `+0x907 CMP EDI,0x20`) → `+0x4/+0x9 PsConvertToGuiThread` (이름만. ntoskrnl 은 sast/138 이 맡음).
  2. 변환이 win32k 스레드 호출을 부름: `win32kfull!W32pThreadCallout +0x61/+0x68 → UserThreadCallout` (win32kbase, 이름 `+0x36c/+0x371 xxxCreateThreadInfo`).
  3. **`xxxCreateThreadInfo` 가 채우는 TEB 칸** `[정적]`: `+0x4e/+0x57` 커널 `GS:[0x30]` (= 이 스레드의 TEB) 를 지역에 저장 · `+0x745/+0x74c` `PsGetThreadId(pti→스레드)` (가져오기 칸 rva `0x2d0268` 이 `PsGetThreadId` 임을 `objdump -p` 로 확인) → **`+0x751/+0x756 MOV [TEB+0x78], 그 반환`** — 즉 TEB+0x78 에는 **스레드 ID 가 들어간다 `[추측: 이름 PsGetThreadId 가 맞다면. 포인터가 아님]`** · `+0x76c/+0x772` `pti+0x208 = TEB+0x800` (`pClientInfo`) · `+0x779~+0x794` TEB `+0x8e8` ← `pti+0x190`, `+0x8ec` ← 0 (+`+0x8f0` ← `pti+0x194`, `+0x8f4` ← 0).
  4. 그 안에서 `xxxCreateThreadInfo +0x136f~+0x1380` 이 ApiSet 표 `[+0x4e8]` 을 간접 호출 = **win32kfull `xxxClientThreadSetup`** (rva `0x1c4f60`, 163B; `win32kfull` 쪽 refs 가 `ext_ms_win_moderncore_win32k_base_ntuser_l1_table+0x4e8` 에서 그 주소를 확인). 반환이 음수면 변환 실패 (`+0x1389 JS`). 그 함수가 `KeUserModeCallback(0x54 = 84, NULL, 0, …)` 을 부른다 (`xxxClientThreadSetup +0x4c/+0x53`) → user32 `__ClientThreadSetup` (콜백 표 84번째 칸 `+0x2a0`, run85) → `ClientThreadSetup`.
  5. **`TEB+0x820` (`pDeskInfo` 칸) 은 user32 가 자기 손으로도 채운다**: `ClientThreadSetup +0x2d5~+0x2e6` 이 칸이 0 이면 `+0x2f8/+0x2ff RtlAllocateHeap(pUserHeap, 0x8=0채움, 0x48B)` 로 `pdiLocal` (rva `0xdaaf8`) 을 만들어 `+0x314~+0x324 MOV [TEB+0x820],RAX`. 즉 **0 으로 채운 0x48B 덩어리면 user32 가 만든 것과 같다** (`+0x30`·`+0x40` 읽어도 0). (서버 프로세스인데 첫 스레드가 아닐 때 `pdiLocal` 을 그대로 쓰는 갈래는 `+0x402~+0x41b`.)
  6. 같은 `ClientThreadSetup` 이 `TEB+0x800` 비트 3 (`+0xca TEST [RBX],8`) 로 "이미 함" 표시, `[TEB+0x910] = 2` (`+0xd3`), `gfSystemInitialized` ← `NtUserGetThreadDesktop(tid) ≠ 0` (`+0x29c~+0x2b0`; 읽는 곳은 `CreateScreenBitmap` 뿐 — `refs 0xda9ec`), 첫 스레드면 `GdiProcessSetup` (`+0x93/+0x9a`, run86 A12).

## (4) 모달 고리가 끝나는 조건

고리는 `DialogBox2` (0x11154, `InternalDialogBox +0x8a/+0x8f` 에서 부름). 한 바퀴: `_PeekMessage` (→ `NtUserPeekMessage` `0x1001`) → 메시지 없으면 `NtUserWaitMessage` (`0x100a`) 에서 잠 → 있으면 `IsDialogMessageW` (키 입력은 여기서 대화상자 단추로 바뀜) → 아니면 `TranslateMessage` → `DispatchMessageWorker`.

끝나는 길 둘 `[정적]`:

1. **정상**: 어디선가 `EndDialog(hwnd, 번호)` 가 불림.
   - `EndDialog` (0x219e0) 가 대화상자 자료 (`[창+0x128] → [+8]`) 의 `+0x18` 에 비트 1 (끝) 을 켜고 `+0x20` 에 결과를 쓴다 (`+0x65 MOV RAX,[RBX+0x128]` · `+0x70 OR [RCX+0x18],1` · `+0x7f MOV [RCX+0x20],R14`), 창을 숨기고 (`NtUserSetWindowPos`), `NtUserPostMessage(hwnd, WM_NULL)` 로 고리를 깨운다 (`+0xd9/+0xe0`).
   - `DialogBox2` 는 매 바퀴 그 칸을 본다: `+0x12a~+0x142` (자료 NULL 이거나 비트 1 켜짐 → `+0x389`) · `+0x230~+0x239` (창 `+0x2a` word & `0xc000` → 지워짐 표시) → 빠져나가 `NtUserDestroyWindow` (`+0x3c4/+0x3cb`) 후 **`[자료+0x20]` 을 반환** (`+0x3bd`).
   - **누가 `EndDialog` 를 부르나 = `MB_DlgProc`** (0x50ab0). 메시지 번호 풀이 `[정적]`: `0x110` `WM_INITDIALOG` (+0xdc) · **`0x111` `WM_COMMAND` (+0x275)** · `0x113` `WM_TIMER` (+0x266) · `0x82` `WM_NCDESTROY` · `0x53` `WM_HELP` · `0x0f` `WM_PAINT` · `0x136`·`0x138` `WM_CTLCOLORMSGBOX`·`STATIC` · `0x301` `WM_COPY`.
     - `WM_COMMAND`: `LOWORD(wParam)` 이 3~6 이면 곧장 `EndTaskModalDialog` + `EndDialog(hwnd, 그 번호)` (`+0x5ead6~+0x5eae4`). 1·2 는 그 번호 단추가 있을 때만 (`+0x5eaeb GetDlgItem` ≠ 0). 7 (`IDNO`) · 9 (도움말) 는 갈래 따로 (`+0x5eb00~+0x5eb10`). `MB_OK` 는 `IDOK=1` 단추 하나.
     - `WM_TIMER`: `EndDialog(hwnd, 0x7d00 = 32000 = IDTIMEOUT)` — `MessageBoxW` 는 타임아웃 −1 이라 타이머를 안 걸어서 안 온다 (`MessageBoxWorker +0x7a~+0x80`, `MB_DlgProc +0x1d4 NtUserSetTimer` 는 타임아웃 있을 때만).
   - **그 `WM_COMMAND` 는 누가 만드나**: 사용자 입력 (단추 클릭 · Enter · Esc · 낱글자) 이 win32k 입력 스레드에서 메시지로 큐에 들어가고 `NtUserPeekMessage` 가 돌려준다. 우리 에뮬레이터에선 **`NtUserPeekMessage` 의 대답이 유일한 입구** (정책 `335_tune15.md` Q5).
2. **비정상 빠져나옴**: 큐에서 `WM_QUIT` (`0x12`) 가 나오면 `NtUserPostQuitMessage` 를 다시 걸고 고리를 끝낸다 (`DialogBox2 +0x280 CMP EAX,0x12` · `+0x283 JZ → +0x379` · `+0x37d/+0x384`). `EndDialog` 가 안 불렸으니 자료 `+0x20` 은 처음 값 (0) 이라 **`MessageBoxW` 가 0 으로 돌아온다** (성공 `IDOK` 가 아님).

## (5) user32 가 syscall 없이 읽는 공유 칸 (지금까지 확인한 것)

`psi` = `gpsi` (user32 rva `0xda590`). `gSharedInfo` = rva `0xda350` (run85). 이름과 '쓰임' 칸의 뜻은 `[추측]`, **오프셋 · 읽는 줄은 `[정적]`**.

| 칸 | 크기 | 읽는 줄 | 쓰임 | 비어 있으면 |
|---|---|---|---|---|
| `gSharedInfo+0x0` (= `psi`) | 8 | 거의 모든 곳 | — | 접근 위반 (run85) |
| `gSharedInfo+0x8` (핸들 항목 배열 `aheList`) · `+0x10` (항목 크기, dword) · `psi+0x8` (qword, 항목 개수) | 8 · 4 · 8 | `ValidateHwnd +0x3c~+0x75` · `HMValidateHandleNoSecure +0xf~+0x38` | hwnd → 항목 번호 = `hwnd & 0xffff`, 번호 ≥ 개수면 실패 | 창이 하나도 못 믿음 (검사 실패 → `SetLastError(0x578 = 잘못된 창 핸들)`) |
| 핸들 항목 `+0x0` (8) · `+0x10` (8) · `+0x18` (byte) · `+0x19` (byte) · `+0x1a` (word) | | `ValidateHwnd +0x86 · +0x9f · +0xa4 · +0xae · +0xb4 · +0xc6` · `HMValidateHandleNoSecure +0x45 · +0x60 · +0x66 · +0x70 · +0x7e` | `+0x18` 종류 (0 = 빈 칸, 1 = 창), `+0x19` 비트 0 = 지워짐 · 비트 2 = 파괴 중, `+0x1a` = hwnd 윗 15비트와 같아야 함, `+0x10` = 소유 데스크톱 표지 (TEB `[+0x820]` 가 가리키는 곳 첫 qword 와 같아야), `+0x0` = 창 물체 (커널 쪽 주소) | |
| TEB `+0x820` (`pDeskInfo`) 가 가리키는 곳 `+0x0` (qword) · `+0x8` (qword = 데스크톱 창 물체) · `+0x30` (dword) · `+0x40` (비트 0 = 합성 중) | | `HMValidateHandleNoSecure +0x72~+0x85` · `GetDesktopWindow +0x1b~+0x2b` · `SoftModalMessageBox +0x372/+0x385` · `IsThreadDesktopComposited +0x20/+0x2c` | `+0x0` 이 항목 `+0x10` 과 달라도 틀린 것이 아니라 `NtUserMapDesktopObject` (`0x14d9`) 로 가서 syscall 한 번 더 | **0 이면 `GetDesktopWindow` · `SoftModalMessageBox` 접근 위반**. 핸들 검사는 0 이어도 syscall 로 빠짐 |
| TEB `+0x828` (`ulClientDelta`) | 8 | `HMValidateHandleNoSecure +0x87~+0x8e` · `GetDesktopWindow +0x2f` | 커널 쪽 주소 + 이 값 = user32 가 읽는 주소 | 0 이면 커널 쪽 주소를 그대로 읽음 |
| TEB `+0x840` · `+0x848` | 8 · 8 | `ValidateHwnd +0x17 · +0x20` | 직전에 검사한 hwnd 와 그 결과 (한 칸 캐시) | 0 이면 캐시 실패 → 위 느린 길 |
| TEB `+0x810` · `+0x818` (`ClientInfo+0x10` · `+0x18`) | 4 · 4 | `GetAppCompatFlags2 +0x22 · +0x33` (run86) · `LoadIcoCur +0x29f` · `MessageBoxWorker +0x1bf` · `RealGetSystemMetrics +0x75` | "예상 Windows 버전" 비교 · 호환 플래그 | 0 이면 모두 건너뜀 (최소로 안전) |
| TEB `+0x8e8` | 4 | `MessageBoxTimeoutW +0xec` · `GetDpiForSystem +0x29` · 기타 | 스레드 DPI 문맥. 0 이면 `gDefaultDpiContext` (처음 `0x6010`) | 0 이면 기본값 |
| `psi+0x0` 비트 `0x10` · `0x80` | 1 | `GetSystemMetrics +0xa5` · `InternalCreateDialog +0xb4a` | API 후크 · 기능 표시 | 0 = 안 켜짐 (최소) |
| `psi+0x3a4 + 40*k` (글 `0x20B`) · `+0x3c4` · `+0x3c8` | | `MessageBoxWorker +0x27a · +0x283 · +0x2c8 · +0x266` | 단추 글 표 | 0 이면 **글이 빈 단추** (상자는 뜸) |
| `psi+0x764` 비트 `0x8000` · `+0x7b4` · `+0x88c` (byte) · `+0x1280` (qword) · `+0x1b50` (dword) · `+0x1b5c` (dword) | | `DialogBox2 +0x2f5 · +0xaf` · `InternalDialogBox +0x3e` · `InternalCreateDialog +0xb4f · +0xc50 · +0xcf2` · `MB_DlgProc +0xd3` · `GetCapture +0x7` | 접근성 알림 · 대기 커서 · 입력 상태 · 글자색 브러시 · 캡처 표시 | 0 = 안 켜짐 |
| `psi+0x768 + 4*지표번호` | 4 | `RealGetSystemMetrics +0xa9/+0xb0` | **`GetSystemMetrics` 값 표 (DPI 무관 지표)** | 0 이면 지표가 전부 0 |
| `psi+0x8ec + 4*(번호*0x1e + 지표)` | 4 | `GetDpiDependentMetric +0x41` | DPI 의존 지표 캐시. −1 이면 `NtUserEnsureDpiDepSysMetCacheForPlateau` | 0 이면 지표 0 (상자 크기 0) |
| `psi+0x8bc` (dword, 비트 열) | 4 | `RealGetSystemMetrics +0x1b7` (지표 `0x2000~0x2006`) · run85 | | |
| `psi+0x138c ~ +0x13e8` (LOGFONT) · **`psi+0x1b56` (word)** · `psi+0x1b66` (word) | | `GetMessageBoxFontForDpi +0x23 ~ +0x86` · `GetDpiCacheIndex +0xe` · `GetDpiForCurrentProcess +0x13` | 메시지 글꼴 · 기준 DPI · 프로세스 DPI | **`+0x1b56 == 0` 이면 0 나눔** |
| `psi + (번호+0x31)*0x68` (+0x0 dword · +0x8 qword) | | `GetDpiServerInfoForCurrentThread +0x41~+0x5e` | DPI 서버 정보 (HFONT 칸) | 0 이면 `SelectObject(hdc, 0)` |
| `gSharedInfo+0x18` (`pDispInfo`, rva `0xda368`) → `+0x8` · `gSharedInfo+0x20` (rva `0xda370`) · 모니터 물체 `+0x1c` · `+0x2c` · `+0x3c` · `+0x3e` | | `GetPrimaryMonitor +0x0 ~ +0x1c` · `GetMonitorWorkRect +0x6c · +0xa6 · +0xab` | 주 모니터 · 작업 영역 | **`pDispInfo` 0 이면 접근 위반 (`[0+8]`)**, 모니터 포인터 0 이면 `[0+0x2c]` 접근 위반 |
| 창 물체 사본 (`tagWND` 모양, 핸들 항목 `+0x0` 로 얻음) `+0x1c` (dword, 스타일) · `+0x1f` (byte, `0x10`=보임 · `0xc0`) · `+0x2a` (word, `0xc000` 지워짐) · `+0x128` (qword) → `[+8]` (대화상자 자료) `+0x18` · `+0x20` | | `DialogBox2 +0x87 · +0x92 · +0x96 · +0x12a · +0x131 · +0x13e · +0x235` · `EndDialog +0x4b · +0x65 · +0x70 · +0x7f` · `GetDialogMonitor +0x2a` | 위 (4) | 창 하나마다 이 모양이 있어야 `EndDialog` 가 끝낼 수 있음. **자료 칸은 `EndDialog` 가 쓰는 것이라 쓰기 가능 메모리여야 함 `[추측: 어느 힙인지]`** |

## 지금 우리 (PE_emulator) 맞나·틀리나

- **`NtUserGetThreadState(0xe)` 기본 대답 0 → 틀림.** 정상은 `1`. 이 때문에 `MessageBoxWorker +0x73` (107/107) · `InternalCreateDialog +0xd4e` → −1 (50/50) · `LoadIcoCur +0x256` (78/78 이면) · `RealGetSystemMetrics +0x131` (484/484) 이 정확히 0/−1 로 나감. 로그의 "`NtUserGetThreadState(0xe)` 하나뿐 · 반환 0" 과 코드가 일치한다.
- **TEB `+0x78` 0 그대로 → 가드엔 맞아도 되는 값** (반환이 1 이면 syscall 뒤에 다시 안 읽음). 그러나 진짜는 0 아님 (`xxxCreateThreadInfo +0x756`, 스레드 ID 로 보임 `[추측]`) — `PtiCurrent` · `GetDpiForSystem` 아래 `IsThreadDesktopComposited` 처럼 매번 가드를 다시 타는 곳이 많다 (user32 전체 85곳). 값을 0 아닌 걸로 두면 syscall 도 안 나가서 로그가 조용해진다.
- **되부름 안 함 → 가드 통과에는 필요 없음.** 다만 되부름이 해 주던 일 중 **`TEB+0x820` ≠ 0** 은 메시지 상자 길 (`SoftModalMessageBox +0x372`) 과 `GetDesktopWindow` (`+0x2b`) 에서 꼭 필요하다. `KeUserModeCallback` 을 안 만들 거면 에뮬레이터가 직접 user32 와 같은 0x48B 0 덩어리를 만들어 TEB `+0x820` 에 넣으면 된다 (위 (3) 5).
- **`psi` 블록 (csr_user_connect_stub.cpp)**: run85 의 "0 으로 채운 `0x1b68`B" 는 `DllMain` 까지는 맞지만 **창 API 길에는 모자란다** — `psi+0x1b56` 이 0 이면 `GetMessageBoxFontForDpi` 0 나눔, `gSharedInfo+0x18` 이 0 이면 접근 위반, 단추 글 표 (`+0x3a4`~) 가 비면 빈 단추, 지표 표 (`+0x768`, `+0x8ec`) 가 0 이면 모든 크기 0. **크기는 `0x1b68`B 이상이면 지금까지 본 칸이 다 안에 든다** (가장 뒤 = `+0x1b66` word. DPI 서버 정보 블록 `psi+(번호+0x31)*0x68` 은 번호 0~17 → `+0x13e8 ~ +0x1ab8`, `GetDpiCacheIndex +0x4f CMP EDX,0x12` 로 번호 17 이 최대). 채워야 하는 값 칸은 위 표 (5).
- **`GetSystemMetrics(0x1000)`** 는 가드를 지나면 `WTSGetServiceSessionId` (kernelbase → `PEB+0x90` 첫 dword) 와 PEB `+0x2c0` (`SessionId`) 를 비교하고, 다르면 `RtlGetActiveConsoleId` 와 또 비교해서 둘 다 다르면 1. 같은 세션이면 0 (`RealGetSystemMetrics +0x233~+0x271`). 에뮬레이터의 "0" 은 가드 통과 후에도 맞는 값일 가능성이 높다 `[추측]`.

### 가드를 통과시키는 최소 조건 (한 목록)

1. **win32u `NtUserGetThreadState` (`0x1000`) 인자 `ECX == 0xe` 일 때 `RAX = 1`** (그 밖 종류 대답은 이번 요청에서 안 바꿈). 이것 하나로 **넷 모두** 가드를 지난다.
2. (TEB+0x78 을 0 아닌 값으로 둘 수 있으면 같이: 스레드 ID 같은 0 아닌 값 `[추측]`. 안 두면 85곳에서 syscall 이 계속 나감 — 반환 1 이므로 길은 안 막힘.)

**가드 뒤 첫 벽까지 (MessageBox 기준, 이것까지 만족해야 상자가 "뜬다")**

3. **TEB `+0x820` ≠ 0**, 가리키는 곳 0x48B 이상 읽을 수 있음 (`+0x0` qword · `+0x8` qword · `+0x30` dword · `+0x40` dword). TEB `+0x828` = 0 도 됨 (0 이면 커널 쪽 주소를 그대로 씀).
4. **`psi+0x1b56` (word) ≠ 0** (96 = `0x60` 이 자연스러움 `[추측]`), `psi+0x1b66` (word) 같은 값.
5. **`gSharedInfo+0x18` ≠ 0** (읽을 수 있는 메모리), 그 `+0x8` 이 주 모니터 물체 (읽을 수 있고 `+0x1c`·`+0x2c` 16B 씩, `+0x3c`·`+0x3e` word).
6. GDI: `NtUserGetDCEx` 가 0 아닌 HDC, `NtGdiHfontCreate` 가 0 아닌 HFONT, 글자 폭이 0 이 아닌 답 (`SoftModalMessageBox +0x153`) — sast/141 의 답과 맞춰야 함.
7. `NtUserFindExistingCursorIcon` 이 0 아닌 핸들 (자원 읽기 건너뛰려면). `NtUserCreateWindowEx` 가 `ValidateHwnd` 를 통과하는 hwnd (핸들 표 항목 + 창 물체 + 대화상자 자료 — 위 표 (5)).
8. 고리를 끝낼 입구: `NtUserPeekMessage` 가 `WM_COMMAND(IDOK)` 를 줄지 (→ 반환 1), `WM_QUIT` 을 줄지 (→ 반환 0) — **정책은 사용자 몫**.

`LoadIconW` 만 풀려면 1 + (`LoadIcoCur` 가 `NtUserFindExistingCursorIcon` 을 부르는 길 + 그 대답이 0 아님) 이면 된다 (`+0x1e5/+0x1ec`, `+0x1f4 TEST` · `+0x1f7 JNZ → +0x135`). `GetSystemMetrics` 만 풀려면 1 + `psi` 비트 `0x10` 꺼짐 + 지표 표 (`+0x768`). `DialogBoxParamW` 는 1 + 위 3·5·6·7 쪽 모두 (`MessageBox` 와 같은 `InternalCreateDialog`).

## 동적으로 확인할 것 (Q162 세션과 합치게)

1. 진짜 GUI 스레드에서 TEB `+0x78` (스레드 ID 와 같은가, 포인터인가) · `+0x800~+0x8f8` 전부 덤프 — 특히 `+0x810 · +0x818 · +0x820 · +0x828 · +0x840 · +0x848 · +0x8e8`.
2. `gSharedInfo` (user32 rva `0xda350`, 0x240B) · `psi` 앞 `0x1c00B` 덤프 — 위 표 (5) 의 오프셋 칸 값 (`+0x1b56` · `+0x1b66` · `+0x3a4` 단추 글 · `+0x768` · `+0x8ec` · `+0x138c`).
3. `NtUserGetThreadState(0xe)` 의 첫 호출 직전·직후 (win32u `+0x12b0` 에서 멈춤): TEB+0x78 이 0 → ≠ 0 으로 바뀌는지, 반환 RAX 가 1 인지, 호출 중 `user32!__ClientThreadSetup` 이 들어오는지 (`bp user32!__ClientThreadSetup`) 와 그때의 콜스택 (`KeUserModeCallback` ← `xxxClientThreadSetup` ← `xxxCreateThreadInfo +0x137b/+0x1380` 이 보여야 함).
4. 메시지 상자 한 번의 syscall 순서 (표 (2) 를 맞춰 봄): `NtUserCreateWindowEx` 몇 번, `NtUserMessageCall` 이 나오는지 (같은 스레드 창이면 안 나올 수 있음), `NtUserGetDpiForCurrentProcess` 가 첫 호출 한 번만 나오는지.
5. `LoadIcoCur +0x290~+0x2a9` (`TEB+0x810 >= 0x400` 이고 표 비트 0) 가 `LoadIconW` 의 "syscall 없는 0" 인지 — 그때 TEB+0x810 값. 에뮬레이터 로그 seq 2766~2768 도 같이.
6. 부모 hwnd 가 있는 `DialogBoxParamW` 호출에서 `NtUserGetThreadState(0xe)` 가 둘인지 (`InternalDialogBox +0xaf GetDesktopWindow` + `InternalCreateDialog +0xd39`).
7. 추천 멈춤점: `win32u!NtUserGetThreadState` (프로세스 조건) · `user32!MessageBoxWorker +0x5c` (TEB+0x78 과 `JNZ`) · `user32!SoftModalMessageBox +0x372` (TEB+0x820) · `user32!GetMessageBoxFontForDpi +0xa6` (`R8D` = `psi+0x1b56`) · `user32!DialogBox2 +0x26b` (고리 한 바퀴) · `user32!EndDialog +0x70`.

## 다음 요청 후보

- **N1**: `InternalCreateDialog` 메시지 상자 갈래 전체 (템플릿 `DS_SETFONT` · `CreateDlgFont` · 컨트롤 고리 · `NtUserShowWindow`·`SetForegroundWindow` 조건) + `VerNtUserCreateWindowEx` 클래스 처리 — 표 (2) 21~27 의 `[공백]` 닫기.
- **N2**: 창 물체 사본 칸 표 — `DispatchMessageWorker` · `DefDlgProcW` · `ValidateDialogPwnd` · `EndDialog` · `SendMessageWorker` 가 읽는 `tagWND` 오프셋 전부 (표 (5) 마지막 줄 넓히기). 핸들 표 항목 (0x20B 모양 `[추측]`) 만들기를 에뮬레이터가 하려면 필요.
- **N3**: win32kbase `xxxCreateThreadInfo` 본문 (5365B) — TEB+0x820·0x828·ClientInfo 칸을 **언제** 누가 채우나 (`InitClientInfo` · `xxxResolveDesktop` · `zzzSetDesktop`), `UserThreadCallout` 조건, 데스크톱 없는 프로세스 (test.exe 같은) 에서의 모양. sast/141 의 gdi 쪽과 겹치지 않게.
- **N4**: GDI 쪽 DC·글꼴·글자 폭 (sast/141 `gdi-startup` 와 합칠 수 있음): `NtUserGetDCEx` 가 돌려주는 HDC 를 gdi32 가 어떻게 읽나 (PEB `+0xf8` GDI 공유 표), `GdiGetCharDimensions`.
- **N5**: `GetSystemMetrics` 지표 번호별 읽는 칸 전체 표 (484 샘플이 쓴 번호 상위 20개와 맞춰서), `LoadIcoCur` 의 자원 길 (`ObjectFromDIBResource` · 커서 만들기 syscall).

## 판정

**분석성공.** 질문 1 (가드) 은 `[정적]` 으로 닫음 (4개 함수 줄별 확인, 같은 도우미 없음 · 반환 1 이 열쇠). 질문 3 (종류 `0xe`) 도 닫음 (상수 1, 부수 효과 쪽이 목적, 커널 변환 순서까지 줄로 이음). 질문 2 (최소 성공 길) 는 **틀 + 확인된 벽 (TEB+0x820 · `psi+0x1b56` · `pDispInfo` · GDI 답)** 까지. 창 만들기 syscall 개수 · 창 물체 칸은 `[공백]` 으로 남기고 "다음 요청 후보" 로 넘김. 값 (`psi` 칸 내용) 은 요청대로 안 함.

(참고: 이번에 읽은 MS 코드는 레포에 붙이지 않았다. `ms/disassemble/{user32,win32kfull,win32kbase}/` 의 `.asm` 줄 번호는 `+0x…` 로만 적음.)

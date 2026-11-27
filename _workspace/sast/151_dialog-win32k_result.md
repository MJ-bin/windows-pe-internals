# 151 dialog-win32k — 정적 분석 결과

- 요청서: `_workspace/sast/151_dialog-win32k.md` (대기열 Q282). 먼저 읽은 것: `plan.md` Q282 줄 · `146_dialog-end_result.md` 전부 (6절 · 8절 · 5절 · "stub 이 채워야 하는 최소 목록" 9 ~ 17) · `150_gdi-font-dc_result.md` 머리 · 표 (E). **정적만.**
- 스크립트 문제: 없음 (`export` · `refs` 모두 `SCRIPT ERROR` 없이 끝남, 한 번에 4 초 안쪽). `rva:` 는 쓸 일이 없어서 안 씀 (메모 확인만).
- PDB GUID (`ms/sym/*.pdb.info.txt`, 146 · 150 과 같음): user32 `CDB947D1B96B87977915DE04CF54279C1` · win32kfull `5CD57181BCFDC5AE8F4819BEB1196F091` · win32kbase `D22DFDA3D54B87E6BD0567A57877904C1`. (커널 ntoskrnl 은 안 만짐 — GUID 확인 해당 없음.)
- 다시 analyze 한 모듈: 없음. 연 프로젝트는 **user32 · win32kfull · kernelbase** (`-readOnly` export · refs). win32kbase 는 `.asm` 글 파일만 읽음 (`CreateCacheDC` 따위 — 이번 질문엔 안 필요해서 새로 안 뽑음). **kernelbase 는 일 끝 무렵에 열었다**: 요청서 ⚠ 조건 (`_workspace/sast/152_appinfo-runas_result.md` 가 있어야) 을 처음 확인했을 땐 파일이 없었고 (그래서 `[objdump]` 로 먼저 훑음), 작업 중 14:57 에 생겨서 (ps 에 Ghidra 프로세스 없음 · `free -g` available 25 GB 확인 뒤) `GetUserDefaultLCID` 와 부르는 둘만 뽑음 (`rva:` 단독 사용). 다른 decompiler 와 겹치지 않게 `free -g` 를 export · refs 마다 먼저 봄 (available 25 ~ 28 GB).
- 새로 뽑은 것 (depth 0, 로그 `ms/ghidra/win32kfull.export.s151{a..j}.stdout` · `user32.export.s151a.stdout` · `kernelbase.export.s151a.stdout` · `win32kfull.refs.s151_xxxLoadSomeStrings.stdout`):
  - win32kfull: `LinkWindow` `UnlinkWindow` `PWInsertAfter` `MapClientNeuterToClientPfn` `NtUserPeekMessage` `xxxInternalGetMessage` `xxxRealInternalGetMessage` `xxxReadPostMessage` `xxxDoPaint` `xxxInternalDoPaint` `DoTimer` `xxxSendPostedMessage` `xxxReceiveMessages` `xxxScanSysQueue` `NtUserInitializeClientPfnArrays` `InitializeClientPfnArrays` `NtUserProcessConnect` `InitMapSharedSection` `InitMessageTables` `InitFunctionTables` `xxxLoadSomeStrings` `NtUserSetMsgBox` `NtUserDestroyWindow` `xxxDestroyWindow` `_Prologue` `_Phase1` `_Phase2` `_Phase3` `xxxDW_DestroyOwnedWindows` `xxxDW_SendDestroyMessages` `xxxFreeWindow` `xxxFreeWindow_Phase1` `xxxFreeWindow_Phase2` `NtUserSetWindowPos` `xxxSetWindowPos` `xxxSetWindowPosAndBand` `xxxEndDeferWindowPosEx` `xxxSendChangedMsgs` `NtUserMessageCall` `xxxDefWindowProc` `xxxRealDefWindowProc` `xxxWrapRealDefWindowProc` `xxxSysCommand` `xxxClientCallDefWindowProc` `NtUserSetWindowLongPtr` `xxxSetWindowLongPtr` `xxxSetWindowData` `xxxSendMessageEx`.
  - user32: `ButtonWndProcWorker` `ButtonWndProcW` `ButtonWndProcA` `xxxButtonNotifyParent` `IsPushButton` `xxxPressButton` `xxxRemoveDefaultButton` `_InitializeImmEntryTable` `User32InitializeImmEntryTable` `xxxBNPaint` `xxxButtonDrawNewState` (`RealDefWindowProcWorker` · `DefDlgProcWorker` 는 같은 이름으로 다시 씀 — 내용 같음).
  - kernelbase: `GetUserDefaultLCID` `GetCurrentNlsCache` `SetupSystemLocaleHashNode`.
  - 이미 있던 asm (읽기만): user32 `MessageBoxW` `MessageBoxTimeoutW` `MessageBoxWorker` `SoftModalMessageBox` `MB_AddPushButtons` `MB_DlgProc` `MB_GetMaxButtonSize` `ClientThreadSetup` `InternalCreateDialog` `TranslateMessage` `xxxCheckDefPushButton` · win32kfull `xxxCreateWindowEx` `SetOrClrWF`.
  - **표 값을 파일에서 직접 읽음** (`[바이너리]` 표시, 파이썬으로 `ms/bin` PE 파싱, 스크래치 `s151/`): win32kfull 메시지 목록 (rva `0x374cb0` · `0x374c00`) · user32 `SEBbuttons` (rva `0xbba90`) · `mpTypeIich` (`0xbbaec`) · `mpTypeCcmd` (`0xbbaf4`) · imm32 이름 글 (`0xb9f58`). kernelbase `GetUserDefaultLCID` 는 처음 `objdump` 로 훑고 (`[objdump]`), 뒤에 Ghidra 로 확인.
- 버전 대조: **일치.** 146 이 적은 줄을 `.asm` 에서 다시 찾음 — `xxxCreateWindowEx +0x1b9 JS` · `+0x1bf MOV EAX,0x320` · `+0x8e6 → +0x8f1 MapClientNeuterToClientPfn` · `+0xa3a MOV R8D,0x208` 모두 같음 · `InternalCreateDialog +0x6fa MOV [RCX+0x20],1` · `DefDlgProcWorker` 조각 `+0x9799c` (`[RCX+0x20]` 쓰기) · `RealDefWindowProcWorker +0x46 / +0x4e / +0xcd` · `ClientThreadSetup +0x228 NtUserInitializeClientPfnArrays` 같음. `_calls.tsv` 의 `ret_off` 도 `.asm` 줄과 맞음 (예 `NtUserPeekMessage +0xff → +0x104 xxxInternalGetMessage` · `xxxCreateWindowEx +0x1720 → +0x1725 LinkWindow` · `xxxRealDefWindowProc +0x831 → +0x836 xxxDestroyWindow`).
- 오프셋 쓰는 법: 아래 `+0x…` 는 **`.asm` 줄의 명령 위치** (call 이면 `_calls.tsv` 2 열; WinDbg `caller+0x..` 는 3 열 `ret_off` = 직접 call 은 `+5`, `call [메모리]` 는 `+7`). 점프 목적지는 "목적지 − 함수 시작". 커널 창 구조의 칸 이름은 PDB 에 형식이 없어서 `[추측: 이름]`, 오프셋 · 읽는 줄은 `[정적]`.
- 창이 두 벌 : 커널 `pwnd` (`[pwnd+0x28]` = 공유 사본 커널 주소, `[pwnd+0x30]` = 같은 사본의 유저 눈 주소) 와 user32 가 읽는 **사본**. 이 결과서의 "사본 +0x…" 는 user32 쪽 (142 · 146 과 같은 칸). 커널 쪽 칸은 `pwnd+0x…` 로 따로 적음.

---

## 먼저 읽는 한 줄 답

1. **(자식 연결 순서 · ANSI 비트)**
   - **새 자식은 맨 아래 (끝) 에 붙는다.** `xxxCreateWindowEx` 는 `WS_CHILD` 창에 `hwndInsertAfter = HWND_BOTTOM (1)` 을 주고 (`+0xbc7 JZ → +0x2014 … +0x2049 MOV [RSP+0xe8],R15` · R15 = 1 은 `+0x1f7`) `PWInsertAfter` (`+0x167b`, 0 · 1 · −1 · −2 는 그대로 돌려줌 `PWInsertAfter +0x22~+0x3a · +0xfb`) → `LinkWindow(새 창, 1, 부모)` (`+0x1720`). `LinkWindow` 는 부모의 첫 자식 `[부모+0x70]` 에서 다음 형제 `[+0x58]` 를 따라 **마지막 형제 뒤** (사본 `+0x14` 비트 5 가 켜진 형제 앞에서 멈춤) 에 끼운다. 컨트롤을 템플릿 차례로 만들면 **연결 차례 = 템플릿 차례**: 사본 `+0x38` (부모) = 첫째 → 첫째 `+0x48` = 둘째 → … 마지막 `+0x48` = 0. **146 stub 10 의 "템플릿 차례" 는 맞다.** 더 필요한 칸: 이전 형제 `사본 +0x50`. 한 번에 끼우는 줄 `LinkWindow +0x30e ~ +0x3b4` (표 A).
   - **ANSI**: 확장 스타일 `0x80000000` 은 ① 저장되는 확장 스타일에서 지워지고 (`+0x1b9 JS → +0x21e4 BTR R12D,0x1f`) ② 안쪽 깃발 `[RSP+0xa4] = 0x320` 이 되어 (`+0x1bf~+0x1cb`) ③ `MapClientNeuterToClientPfn(클래스, 0, 0x320)` (`+0x8e6/+0x8f1`) 로 **클라이언트 프로시저 `사본 +0x78` 를 A 판 표 칸으로 바꾸고** (`+0x8fd MOV [RAX+0x78],RCX`; 대화상자 = `DefDlgProcA`) ④ `사본 +0x13` 비트 5 를 켠다 (`+0xa04~+0xa16` `SetOrClrWF(…, 0x320, …)`). **`사본 +0x12` 비트 3 (`0x208`) 은 이 확장 스타일 하나로 켜지지 않는다**: 클래스 깃발 `[[클래스+8]+6]` 비트 1 (ANSI 클래스) 이거나 (`+0xa23~+0xa3a`), ANSI 만든이 + 클래스 원자가 `gpsi+0x364 … +0x388` 아홉 칸 중 하나일 때 (`+0x1cbb~+0x1de8`). 대화상자 클래스가 그 아홉에 드는지는 `[공백]` (순서상 안 든다고 `[추측]`).
2. **(`NtUserPeekMessage` 진짜 첫 메시지들)** 부름 차례 (`xxxRealInternalGetMessage`): ① 보낸 메시지는 안에서 처리 (`xxxReceiveMessages`, 반환 안 됨) → ② (이 스레드가 입력 큐 주인일 때만) 입력 한 번 → ③ **게시 메시지** (`QS_POSTMESSAGE`, `xxxReadPostMessage`) → ④ **입력** (키 · 마우스, `xxxScanSysQueue`) → ⑤ **`WM_PAINT`** (`QS_PAINT`, `xxxDoPaint`: `message = 0xf` · wParam = lParam = 0 · hwnd = 칠할 게 있는 창) → ⑥ **`WM_TIMER`** (`QS_TIMER`, `DoTimer`) → ⑦ 없으면 반환 0 (`PM_NOYIELD` 가 없으면 그 앞에 `zzzWakeInputIdle`). `WM_NCACTIVATE` · `WM_ACTIVATE` 같은 건 **보낸 (SendMessage) 메시지라 PeekMessage 로 안 나온다**. 그래서 막 선 보이는 모달 대화상자 + 입력 장치 없음 + 게시 메시지 없음 → 첫 부름 = `WM_PAINT` (칠할 창마다 한 번씩), 그 뒤 0 (→ `DialogBox2` 빈 틈 → `WaitMessage`). `wRemove`: `& 0xe300fffc` 가 있으면 `LastError 0x3ec` 로 0 (`NtUserPeekMessage +0xd7~+0xe3 → +0x189`); 비트 0 (`PM_REMOVE`) 은 `ReadPostMessage` · `ScanSysQueue` 에 "뺄까" 로 넘어감 (`xxxRealInternalGetMessage +0x225~+0x237` 의 `R15D = wRemove & 1`); 비트 1 (`PM_NOYIELD`) 은 없을 때 반환 길에서 `WakeInputIdle` + `ReceiveMessages` 를 건너뜀 (`+0x163c`). 반환 = `xxxInternalGetMessage` 반환 (≠0 이면 MSG 0x30 B 를 사용자 버퍼에 복사, `+0xff · +0x120~+0x159`).
3. **(MessageBox 기본 단추 ID)** `MessageBoxW → MessageBoxTimeoutW → MessageBoxWorker → SoftModalMessageBox → InternalDialogBox → DialogBox2` **같은 고리 (146 E1 ~ E21) 를 탄다** (`SoftModalMessageBox +0x5b1 CALL InternalDialogBox`, 대화상자 프로시저 = `MB_DlgProc`). 반환은 그대로: `MB_DlgProc` 가 `EndDialog(hwnd, 누른 ID)` → `DialogBox2` 반환 `pdlg+0x20` → `SoftModalMessageBox +0x5be` (−1 이면 0) → `MessageBoxWorker +0x34e/+0x386` → `MessageBoxTimeoutW +0x19e MOV EAX,EBX` → `MessageBoxW`. **바꾸는 줄 없음.** 종류 → 단추 → ID 는 `MessageBoxWorker` 가 `SEBbuttons[mpTypeIich[종류] + j]` 로 `gpsi` 의 `MBStrings` 칸을 골라 `uID` (= 칸 번호 + 1, **커널이 `xxxLoadSomeStrings` 로 씀**) 를 읽는다. **`MB_YESNO` 의 기본 단추 = `IDYES (6)`, `MB_DEFBUTTON2` 면 `IDNO (7)`** — `IDOK (1)` 가 아니다. 전체 표는 표 C. `pdlg+0x20` 에 그 ID 가 들어가는 줄 = `InternalCreateDialog +0x9ba~+0x9c1 → 조각 +0x1341~+0x134f` (단추가 `WM_GETDLGCODE` 에 `0x10` 비트로 답할 때 그 단추 ID 를 씀; 처음값 `1` 은 `+0x6fa`). 단추 쪽: `ButtonWndProcWorker` 가 `BS_DEFPUSHBUTTON` 이면 `0x2010` 을 돌려줌 (한 줄 답 5). **`MB_RETURN` 같은 바꿈 없음. `gfDMREnable ≠ 0` 이면 상자를 안 띄우고 같은 표의 기본 단추 ID 를 곧장 돌려주는 길이 따로 있다** (`MessageBoxWorker +0xe8~+0x116`).
4. **(비트표 `user32+0xda568 ~ +0xda580`)** `NtUserInitializeClientPfnArrays` 가 아니다 (그건 클라이언트 프로시저 배열 둘 + 일꾼 배열 + `hmodUser` 만 `gpsi` 에 복사). **`ClientThreadSetup +0x120 NtUserProcessConnect` 가 돌려준 0x240 B 를 user32 `gSharedInfo` (`+0xda350`) 로 통째 복사** (`+0x134~+0x1ad`) 한 것 — 그 안 `+0x218 / +0x220` = `DefWindowMsgs` {한도, 비트 포인터}, `+0x228 / +0x230` = `DefWindowSpecMsgs` {한도, 포인터} (`0xda568 − 0xda350 = 0x218`). 커널은 `InitMapSharedSection` (`+0x2fe~+0x38c`) 이 세션 칸 `[세션+0x5468 · 5470 · 5478 · 5480]` 을 사용자 연결 구조에 옮김; 그 칸은 **`InitMessageTables` (win32kfull rva `0x407be0`) 가 `.rdata` 메시지 목록 (`0x374cb0` · `0x374c00`) 으로 만든 비트 배열**. **값: `DefWindowMsgs` 한도 `0x33f` · `DefWindowSpecMsgs` 한도 `0x349`; `WM_GETDLGCODE (0x87)` 와 `WM_COMMAND (0x111)` 는 두 목록 어디에도 없음 → `RealDefWindowProcWorker` 가 syscall 없이 0** (146 `[공백]` 닫힘). 값 표는 표 D.
5. **(`ButtonWndProcWorker` · `xxxButtonNotifyParent`)** `WM_GETDLGCODE (0x87)`: 창 fnid 가 `0x2a1` 이고 단추 자료 `[[사본+0x128]]` 가 있을 때 → 단추 종류 `[사본+0x1c] & 0xf` — **`BS_DEFPUSHBUTTON (1)` → `0x10 | 0x2000 = 0x2010` (`ButtonWndProcWorker +0xe3~+0xec JZ → +0x32b MOV EBX,0x10 → +0x12e BTS RBX,0xd`)**, `BS_PUSHBUTTON (0)` → `0x20 | 0x2000 = 0x2020` (`+0x129 MOV EBX,0x20`). 클릭: **`xxxButtonNotifyParent(단추, 코드)`** 가 부모에게 **`SendMessageW(부모 hwnd, WM_COMMAND, MAKEWPARAM(word [단추 사본+0x140], 코드), 단추 hwnd)`** 를 보낸다 (큐 아님; `xxxButtonNotifyParent +0x0~+0x32 MOV EDX,0x111 · +0x3a JMP SendMessageW`, ID 는 **아랫 16 비트만**). `BN_CLICKED (0)` 을 정하는 줄 = 조각 `FUN_1800aa27b` (`= ButtonWndProcWorker +0x9b80b`) `+0x24 XOR EDX,EDX → +0x30 CALL xxxButtonNotifyParent`; 어떤 입력 메시지가 그 조각으로 가는지 (마우스 뗌 · 스페이스 뗌 · `BM_CLICK`) 는 점프표를 다 안 읽음 `[공백]`.
6. **(끝 길 syscall 부작용)** ① `WM_CLOSE (0x10)` 는 `DefWindowMsgs` 에 있음 → `RealDefWindowProcWorker` 가 `NtUserMessageCall (fnid 0x29e)` → 커널 `xxxRealDefWindowProc +0x74a~+0x831` → **`xxxDestroyWindow(pwnd)` 곧장 (확인 없음)**. 단 **대화상자는 `DefDlgProcWorker` 가 먼저 가로채** 앱이 FALSE 면 `PostMessageW(hDlg, WM_COMMAND, IDCANCEL …)` (146 `+0x448~+0x467`), `MessageBox` 는 `MB_DlgProc` 가 `WM_CLOSE` 를 안 다뤄 같은 길. ② `WM_SYSCOMMAND (0x112)` 도 `DefWindowMsgs` → `xxxRealDefWindowProc +0x14bc xxxSysCommand` → **`SC_CLOSE (0xf060)` = 보내기 (큐 아님) `xxxSendTransformableMessageTimeout(pwnd, WM_CLOSE …)`** (`xxxSysCommand +0xd9~+0x114` 연쇄 → `+0x186~+0x1ae`). ③ `NtUserDestroyWindow` → `xxxDestroyWindow`: `_Prologue` (`+0x3f`) → `_Phase1` (`+0x90`) → `xxxDW_DestroyOwnedWindows` (`+0xa2`) → `_Phase2` (`+0xb1`: **소유자 `xxxEnableWindow` · `xxxActivateWindowWithOptions` · 포커스 · `xxxWindowEvent` 와 `xxxDW_SendDestroyMessages` (`+0x3a9`) = 보내기로 메시지 `0x272` 뒤 `WM_DESTROY (2)`**) → `_Phase3` (`+0xc0`) → `xxxFreeWindow` (`+0xcf` → `xxxFW_DestroyAllChildren` → `_Phase2` 안 **`WM_NCDESTROY (0x82)` 보내기** `+0x3c MOV EDX,0x82 · +0x49`). 즉 **`WM_DESTROY` · `WM_NCDESTROY` 는 커널이 클라이언트로 되부르며 보낸다** (146 `[추측]` 확인). ④ `NtUserSetWindowPos(…, 0x97)` = `NOSIZE | NOMOVE | NOZORDER | NOACTIVATE | HIDEWINDOW`: 깃발 검사 통과 (`+0x8f TEST R15D,0xfffd9800`) → `xxxSetWindowPos +0x66 → xxxSetWindowPosAndBand +0x39/+0x76/+0x8b → xxxEndDeferWindowPosEx` (`+0x15b xxxCalcValidRects` · `+0x203 xxxSendChangedMsgs` → `WM_WINDOWPOSCHANGED (0x47)` 보내기 · `PostShellHookMessagesEx` · `xxxCallHook` · `+0x68c xxxSwpActivate` · `+0x6a0 xxxDoSyncPaint`) — **보이기 비트를 지우고 위치 바뀜 메시지를 보냄**; 숨기는 줄 · 활성 바뀜 여부는 안 읽음 `[공백]`. ⑤ `NtUserMessageCall (0x1005)`: `msg < 0x400` 이면 `MessageTable[msg] & 0xff` 로 `gapfnMessageCall` (win32kfull rva `0x367420`) 을 고르고 `fnid` (`0x29e` = `DefWindowProc`) 는 인자로 넘김 (`NtUserMessageCall +0x1c0~+0x20b`); **`WM_GETDLGCODE` · `WM_COMMAND` 는 여기 오지 않는다 (한 줄 답 4).** ⑥ `NtUserSetWindowLongPtr (0x15da)`, `GWLP_USERDATA (−21)`: `xxxSetWindowLongPtr +0x4a JS` (음수) → `xxxSetWindowData +0x3b CMP EBX,−0x15 → +0xd0~+0xdb`: **`옛값 = [사본+0xd8]; [사본+0xd8] = 새값; 반환 = 옛값`** (146 `[추측]` 확인, 읽는 쪽 `GetWindowData +0xc3` 과 같은 칸).
7. **(그 밖)** ① `g_systemCallFilterId` 를 정하는 쪽 = **150 이 닫음**: 150 결과서 "먼저 읽는 한 줄 답" 3 · 표 (E) (user32 필터 5 갈래 25 곳). 한 줄: `GetProcessMitigationPolicy` 하위 4 비트 ← 커널 `NtQueryInformationProcess(0x34)` ← `[EPROCESS+0x6e8]` (부모가 준 Win32k 필터), 일반 프로세스 0. 이 요청에서 본 새 쓰임: `NtUserSetWindowLongPtr +0x86~+0x92` 가 `PsGetWin32KFilterSet() == 5` 면 `GWLP_USERDATA (−21)` 말고는 `LastError 0x57` (`+0x12c~+0x13b`). ② **IMM 훅 `[user32+0xd96f0]` = `imm32!ImmTranslateMessage`** (`_InitializeImmEntryTable +0x6e3 LEA RDX,[0xb9f58]` ("ImmTranslateMessage", `[바이너리]`) → `+0x6f4 GetProcAddress` → `+0x710 MOV [0xd96f0],RAX`); `TranslateMessage +0x12 CMP word [msg+0x10],0xe5 (VK_PROCESSKEY) → +0x6a~+0x71` 에서 `RCX=hwnd · EDX=message · R8=wParam · R9=lParam` 으로 CFG 호출, 반환 ≠ 0 이면 TRUE. 몸은 imm32 (프로젝트 없음) = 다른 프로젝트 몫. **메시지 종류와 상관없이 `wParam` 아랫 16 비트가 `0xE5` 일 때만 탄다 (`WM_COMMAND` 의 ID 가 229 인 경우 포함) → 보통 안 탐.** ③ **`GetUserDefaultLCID` (kernelbase rva `0x50ef0`, 73 B)** `[정적]`: `GetCurrentNlsCache()` (`+0x4`, rva `0x52350`) 를 불러 반환 `[+0x10]` (사용자 기본 로케일 해시 노드) 가 NULL 이 아니면 그 노드의 첫 dword = LCID, **`0x1000` (`LOCALE_CUSTOM_UNSPECIFIED`) 이면 `0xc00` (`LOCALE_CUSTOM_DEFAULT`) 으로 바꿔** 돌려줌 (`+0x9~+0x25`). NULL 이면 전역 `gpSysLocHashN` (`0x1803aad10`, 시스템 로케일 노드) 를 보고 (`+0x27~+0x31`), 비었으면 `SetupSystemLocaleHashNode(0)` (`+0x39`, rva `0x68410`) 가 만들어 `[노드]` (첫 dword) 를 돌려줌 (이 갈래는 조각 `FUN_18019f4d2 +0x0 MOV EAX,[RAX]` 뒤 `GetUserDefaultLCID +0x21` 로 곧장 돌아가서 `+0x1e CMOVZ` 를 건너뜀 → **`0x1000 → 0xc00` 바꿈은 사용자 노드 갈래에만 있음**). **`SetupSystemLocaleHashNode` 는 `NtQueryDefaultLocale(FALSE, &gSystemLocale)` (ntdll syscall, `+0xf7`) 로 시스템 로케일을 받아** (`gSystemLocale` `0x1803aace8` 이 아직 안 채워졌을 때) `MakeLocHashNode` (`+0x87`) / `MakeNamedLocaleHashNode` (`+0xa1`) 로 노드를 만든다 (`gTblPtrsLock` 아래). 첫 줄 `GetCurrentNlsCache` (4643 B) 는 `TEB+0x179c` 가 0 이고 `gInteractiveLogonUserProcess` / `gpServerNlsUserInfo` (csrss 가 나눠 주는 NLS 사용자 정보) 를 보며, 캐시가 낡으면 `CsrClientCallServer` (`+0x34e`) · `NtClose` · `RtlOpenCurrentUser` (`+0x6a0`, 사용자 레지스트리) 를 부를 수 있음 → **LCID 값은 에뮬레이터의 NLS 사용자 정보 · 기본 로케일 stub 이 정함 (`NtQueryDefaultLocale` 값 + 사용자 캐시). 캐시 안쪽 (`+0x10` 가 어떻게 채워지는지) 은 `[공백]`**. 요청 1 ~ 6 의 끝 길에서는 `IsDialogMessageW +0x22` 가 부를 때만 필요 (키 `WM_KEYDOWN` 길 · 146 E10).

---

## 표 A. 자식 연결 — `xxxCreateWindowEx` → `LinkWindow` (win32kfull)

| 단계 | 하는 일 | 줄 |
|---|---|---|
| A1 | 만들기 전 `hwndInsertAfter` = `[RSP+0xe8]`: 처음엔 부모 (`+0x6f/+0x77`), 보통 길에서 **0 (`HWND_TOP`)** 으로 덮음 (`+0xb51 MOV [RSP+0xe8],RSI`), CBT 훅이 켜졌으면 훅이 준 값 (`+0x20a9~+0x2102`) | `xxxCreateWindowEx +0x6f +0x77 +0xb51 +0x2102` |
| A2 | **`WS_CHILD` (사본 `+0x1f & 0xc0 == 0x40`)** 이면 훅 값도 덮어 **`1` (`HWND_BOTTOM`)** — 부모가 바탕 창이든 아니든 둘 다 `+0x2049` 로 (`+0x2038 _GetDesktopWindow … JNZ → +0x2bd9 … JMP +0x2049`) | `+0xbc7 JZ → +0x2014 · +0x2049 · +0x1f7 MOV R15D,1` |
| A3 | `PWInsertAfter(hwndInsertAfter)` → 0 · 1 · −1 · −2 이면 **그 값 그대로** (`MOV RAX,RDI`) | `xxxCreateWindowEx +0x167b` · `PWInsertAfter +0x1c · +0x26 · +0x30 · +0x3a → +0xfb` |
| A4 | `ValidateNewParent` · `UnlinkWindow(새 창, 옛 부모)` (새 창은 아직 안 이어졌으니 사실상 빈 일) → 자식이면 위쪽 맨 위 보정 (`GetTopMostInsertAfter`) 을 건너뜀 → `CMP RBX,2` (<2 면 곧장) | `+0x16af · +0x16c3 · +0x16cc~+0x16d3 JZ → +0x170d · +0x1709~+0x1711` |
| A5 | **`LinkWindow(RCX=새 창, RDX=1, R8=부모)`** | `+0x1720` (ret `+0x1725`) |
| A6 | `LinkWindow`: 부모 첫 자식 `[R8+0x70]` 이 이미 새 창이면 끝. `RDX` 가 0 · 1 · −1 · −2 가 아니면 (진짜 창) 다른 길 `+0x46c`. 부모가 **바탕 창이면** `GetBandOrdinal` 로 띠 번호를 받아 (`+0x154`) 띠 순서도 비교, **아니면 `R9D = 0`** (컨트롤 · 대화상자는 이쪽) | `LinkWindow +0x2c · +0x36~+0x3e · +0x86~+0x9d · +0x154` |
| A7 | **`RDX == 1` (BOTTOM)**: `RBX = [부모+0x70]` (첫 자식). 없으면 "맨 위" 길과 같음 (A9). 있으면 (첫 자식 사본 `+0x14` 비트 5 가 켜져 있으면 A9) 다음 형제 `R10 = [RBX+0x58]` 를 따라가며, 다음 형제 사본 `+0x14` 비트 5 가 켜졌거나 다음이 없으면 멈춤 → **`RBX` = 마지막 (보통 진짜 마지막) 형제** | `LinkWindow +0x213~+0x216 · +0x218~+0x24e · +0x250~+0x253` |
| A8 | `RBX` 뒤에 끼움: (뒤에 형제가 있으면 그쪽 이전 `+0x60` · 사본 `+0x50` 을 새 창으로) · **새 창 `+0x60` (이전) = RBX · 사본 `+0x50` = RBX 사본** · **RBX `+0x58` (다음) = 새 창 · 사본 `+0x48` = 새 창 사본** · 새 창 `+0x58` (다음) = 원래 RBX 의 다음 (없으면 0) | `LinkWindow +0x2e1~+0x3b4` (`+0x37a~+0x3a4`) |
| A9 | `RDX == 0 (TOP)` 이거나 A7 에서 첫 자식이 비었을 때: 부모에 자식이 **없으면** 새 창이 첫 자식 (`부모 +0x70` · 부모 사본 `+0x38` = 새 창 사본), **있으면 첫 자식 앞에** 끼움 (`첫 자식 +0x60` · 사본 `+0x50` = 새 창, `새 창 +0x58` · 사본 `+0x48` = 첫 자식) | `LinkWindow +0xa9~+0xe0 · +0x3b9~+0x41b` |
| A10 | 끝: `BTS [새 창+0x17c],0x18` · 부모 사슬에 새 창이 이미 있는지 보고 (`+0xeb~+0x106`) `0x2000000` 비트 정리 · (`IsWindowDesktopComposed` 이면 `DwmAsyncChildLink` 로 DWM 에 알림 `+0x1e8`) | `LinkWindow +0xe0~+0x12f · +0x16c~+0x1f4` |

**결론 (사본 칸)**: `사본+0x30` = 부모 사본 (부모 칸은 `xxxCreateWindowEx +0xce2` 가 `LinkWindow` 앞에 씀) · `사본+0x38` = 첫 자식 · `사본+0x48` = 다음 형제 · **`사본+0x50` = 이전 형제**. 템플릿 차례로 만든 컨트롤 ID · `사본 +0x140` 이 `GetDlgItem` 의 사슬 (`+0x38 → +0x48`) 과 `GetWindow(GW_CHILD / GW_HWNDNEXT)` 차례와 같다. **`MessageBox` 는 `MB_AddPushButtons` 가 템플릿에 단추를 먼저 쓰므로 (`SoftModalMessageBox +0x511`) 단추 차례가 앞**이고 `MB_DlgProc +0x189~+0x1a3` 이 `GetWindow(GW_CHILD)` 에서 `기본 단추 번호` 번 다음 형제로 가서 `NtUserSetFocus` 를 부르는 것과 맞는다 (아이콘 · 글 항목은 그 뒤 조각 안, 안 읽음 `[공백]`). 최상위 창 (`HWND_TOP`) 은 바탕 창 자식 맨 위에 (띠 순서 · `GetTopMostInsertAfter` 갈래 안 따라감 `[공백]`).

### 표 A'. ANSI · 클라이언트 프로시저 줄 — `xxxCreateWindowEx` · `MapClientNeuterToClientPfn`

| 단계 | 하는 일 | 줄 |
|---|---|---|
| B1 | `dwExStyle` (인자 1 = `ECX`) 의 부호 비트 (= `0x80000000`) 가 켜졌는지 | `xxxCreateWindowEx +0x1b9 TEST R15D,R15D · JS → +0x21e4` |
| B2 | 켜졌으면 **저장되는 확장 스타일 사본 `[RSP+0x50]` · `[RSP+0x90]` 에서 비트 31 지움** (`BTR R12D,0x1f`); 원래 값은 `[RSP+0x8c]` 에 남음 (뒤에서 `EBX`) | `+0x21e4~+0x21ee` · `+0x3c` |
| B3 | **`AX = 0x320` (켜졌을 때) / `0` (아니면)** → `[RSP+0xa4]` · `[RSP+0x60]` | `+0x1bf MOV EAX,0x320 · +0x1c4 TEST · +0x1c7 CMOVNS AX,SI · +0x1cb · +0x1d2` |
| B4 | **`MapClientNeuterToClientPfn(클래스, 0, B3 값)`**. 클래스 fnid `word [[클래스+8]+4]` 가 `0x29a` 또는 `0x2a1 ~ 0x2aa` 일 때만 바꿈: B3 ≠ 0 (A 만든이) 이고 클래스 프로시저가 **W 표** (`gpsi + fnid*8 − 0x1288`) 의 칸과 같으면 **A 표** (`− 0x1348`) 의 칸을 돌려줌; B3 = 0 이면 반대 | `+0x8e6 MOVZX R8D,[RSP+0xa4] · +0x8f1 CALL` · `MapClientNeuterToClientPfn +0x20~+0x97 · +0xa5~+0xdf` |
| B5 | 반환을 **`사본 +0x78`** 에 씀 (대화상자 fnid `0x2a4` → A 표 10 번째 칸 = `gpsi+0x1d8`, W 표 = `gpsi+0x298`) | `+0x8f6 · +0x8fd MOV [RAX+0x78],RCX` |
| B6 | 클래스가 서버 쪽 프로시저 (`[[클래스+8]+6]` 비트 0) 면 `사본 +0x12` 비트 2 (`0x204`) 켬 · (사본 `+0x10 + 2`, `SetOrClrWF` 의 깃발 상수 = `(바이트번호 << 8) | 마스크`: `0x204` → `+0x12` 비트 2, `0x208` → `+0x12` 비트 3, `0x320` → `+0x13` 비트 5) | `+0x91c~+0x92b · SetOrClrWF +0x14~+0x52` |
| B7 | `SetOrClrWF(1, 새 창, B3 값, 1)` → B3 = `0x320` 이면 **`사본 +0x13` 비트 5 켬** `[추측: 이름 = 만든이가 ANSI]`; 0 이면 마스크 0 이라 아무 일 없음 | `+0xa04~+0xa16` |
| B8 | **클래스 ANSI 깃발 `[[클래스+8]+6] & 2` 이면 `사본 +0x12` 비트 3 켬** (`0x208`) | `+0xa23~+0xa34 · +0xa3a MOV R8D,0x208 · +0xa49` |
| B9 | 클래스가 ANSI 아니면: 만든이가 A (`EBX` 부호) 일 때 클래스 원자 `word [RSP+0x5c]` 를 `gpsi+0x364 · +0x366 · +0x368 · +0x36a · +0x36e · +0x370 · +0x372 · +0x382 · +0x388` 와 비교, **하나라도 같으면 B8 로 (비트 3 켬)**, 아니면 안 켬 | `+0x1cbb~+0x1de8 → +0xa3a` |

배열 쪽: `NtUserInitializeClientPfnArrays → InitializeClientPfnArrays` 가 첫째 배열 (0xc0 B) 을 `gpsi+0x188` (**A 표**), 둘째를 `gpsi+0x248` (**W 표**), 셋째를 `gpsi+0x308` (0x58 B) · `gpsi+0x1d50`, `hmodUser` 를 `[세션+0x5490]` 에 씀 (`InitializeClientPfnArrays +0xdf~+0x3a8`; 세션 칸 `+0x11a40` 이 0 일 때 한 번만 `+0x36 · +0x3b4`). 배열 이름 A / W 는 인자 차례로 `[추측]` (`MapClientNeuter` 의 쓰임이 확정).

---

## 표 B. `NtUserPeekMessage` — 첫 메시지 차례 (win32kfull)

| 차례 | 무엇 | 조건 (깨움 비트 `fsWakeBits` ∧ 마스크 `R12D` = `CalcWakeMask(최소, 최대, wRemove >> 16)`) | 반환 · MSG | 줄 |
|---|---|---|---|---|
| P0 | 깃발 검사 · 부르기 | `wRemove & 0xe300fffc ≠ 0` → `LastError 0x3ec` · 0. 여섯째 인자 비트 0 이면 `KeWaitForSingleObject` 갈래 (`user32 _PeekMessage` 는 0 을 줌 → 안 탐) | — | `NtUserPeekMessage +0x2d~+0x37 · +0xd7~+0xe3 · +0xff` |
| P1 | 보낸 메시지 | `QS_SENDMESSAGE (0x40)` | **반환 안 됨** (보낸 쪽 프로시저가 안에서 돎) | `xxxRealInternalGetMessage +0x4be~+0x4cd` · `+0xb28~+0xb30` |
| P2 | (입력 큐 주인 스레드일 때만) 입력 한 번 | `[큐+0x58] == 이 스레드` 이고 `[…+0x214] & 0x400` · `wake & mask & 0x3c07` | 입력 메시지 | `+0x831~+0x8a3` |
| P3 | **게시 메시지** | `QS_POSTMESSAGE (0x8)` (`+0x8e0 TEST AL,8`) | `xxxReadPostMessage(…, 지움 = wRemove & 1)` → 맞는 첫 것 | `+0x8e8~+0x90e` |
| P4 | **입력** | `wake & mask & 0x3c07` (키 `0x1` · 마우스 이동 `0x2` · 단추 `0x4` · 원입력 `0x400` …) | `xxxScanSysQueue` | `+0x9eb~+0xa2e` |
| P5 | **`WM_PAINT`** | `QS_PAINT (0x20)` (`+0xbb1 TEST AL,0x20`) | `xxxDoPaint(창 거르기, MSG*)`: **`MSG = {hwnd = 칠할 창, message = 0xf, wParam = 0, lParam = 0, time = 틱, pt = 커서}`** (`xxxDoPaint +0x1dd · +0x1ea · +0x1ee · +0x21e MOV [RBX+8],0xf`) | `xxxRealInternalGetMessage +0xbbb` · `xxxDoPaint +0x1b4~+0x22e` |
| P6 | **`WM_TIMER`** | `QS_TIMER (0x10)` (`+0xca8 TEST AL,0x10`) | `DoTimer(창 거르기)` | `+0xcb3` |
| P7 | 없음 | — | 0. `PM_NOYIELD` 없으면 `zzzWakeInputIdle` + `xxxReceiveMessages` 를 한 번 (`GetMessage` 판은 대신 `xxxSleepThread*` 로 잠 `+0xaee · +0xdbb · +0xe25`) | `+0xc41 · +0x1646` |

- **막 선 모달 대화상자 한 개 · 입력 없음 · 게시 없음**: P1 ~ P4 가 비고 **P5 `WM_PAINT` 가 먼저** (대화상자 → 컨트롤 …, `xxxInternalDoPaint` 가 차례를 정함, 안 읽음). 다 칠하고 나면 P7 = 0. 사용자가 단추를 누르면 P3 (게시) 가 아니라 **P4 입력** → `IsDialogMessageW` 가 `WM_COMMAND` 로 바꿔 보냄 (146 4-1).
- **WM_PAINT 가 반복되지 않으려면** 앱 · `DefWindowProc` 가 칠하기를 끝내야 (`WM_PAINT (0xf)` 는 `DefWindowMsgs` 에 있음 → `NtUserMessageCall` → 커널 `xxxDWP_Paint`). **stub 이 `WM_PAINT` 를 줬는데 `NtUserMessageCall` 이 0 만 주면 같은 `WM_PAINT` 가 끝없이 반복** (진짜는 갱신 영역이 안 비어 있는 동안 계속) `[추측: xxxDoPaint 의 반복 조건은 안 읽음]`.
- **`WM_QUIT`**: `xxxReadPostMessage` 안에서 (게시 큐가 빈 뒤 `QUITPOSTED` 로) 나올 것 — 이 요청에서 안 읽음 `[공백]`.

---

## 표 C. MessageBox 종류 → 단추 → 기본 단추 ID (`[바이너리]` 표 + 줄 근거)

규칙 (`MessageBoxWorker`, user32): ① 종류 `uType & 0xf` (> 6 이면 `LastError 0x59e` · 0, `+0x84~+0x8d`) ② 단추 수 `mpTypeCcmd[종류] + ((uType >> 14) & 1)` (`MB_HELP` 이면 하나 더, `+0xb4~+0xbd`) ③ **기본 단추 번호 (0 부터) `(uType >> 8) & 0xf` — 단추 수보다 작지 않으면 0** (`+0xc0~+0xcf`) ④ 단추 `j` 의 `MBStrings` 칸 = `SEBbuttons[mpTypeIich[종류] + j]` (`+0x22b · +0x25b`) ⑤ 그 칸의 **`uID` = `gpsi + 칸*0x28 + 0x3c4`** (`+0x2c8 MOV EAX,[RSI+0x3c4]`) = ID. **`uID` 는 커널 `xxxLoadSomeStrings` 가 `칸번호 + 1` 로 씀** (`xxxLoadSomeStrings +0x4e~+0x55`, 칸 0 ~ 10; `uStr = 0x320 + 칸` `+0x34`; 이름은 `xxxClientLoadStringW` 로 `gpsi+0x3a4 + 칸*0x28` 에, 15 글자 `+0x7b`). 부르는 곳: `xxxInitWindowStation +0x8e` · `xxxUpdatePerUserSystemParameters +0xb9d` (`refs`). 칸 번호: OK 0 · CANCEL 1 · ABORT 2 · RETRY 3 · IGNORE 4 · YES 5 · NO 6 · CLOSE 7 · HELP 8 · TRYAGAIN 9 · CONTINUE 10 → ID = 칸 + 1.

| 종류 (`uType & 0xf`) | 단추 (왼쪽부터) | `MB_DEFBUTTON1` (0) | `MB_DEFBUTTON2` (`0x100`) | `MB_DEFBUTTON3` (`0x200`) | `MB_DEFBUTTON4` (`0x300`) | `+ MB_HELP (0x4000)` 이면 끝에 `IDHELP (9)` 를 더, `DEFBUTTON4` 가 그 단추 |
|---|---|---|---|---|---|---|
| `MB_OK` (0) | `IDOK 1` | **1** | 1 (수 1 보다 큼 → 0 번) | 1 | 1 | `[1, 9]`, DEFBUTTON2 = 9 |
| `MB_OKCANCEL` (1) | `IDOK 1` · `IDCANCEL 2` | **1** | **2** | 1 | 1 | `[1, 2, 9]`: 3 → 9, 4 → 1 |
| `MB_ABORTRETRYIGNORE` (2) | `IDABORT 3` · `IDRETRY 4` · `IDIGNORE 5` | **3** | **4** | **5** | 3 | `[3, 4, 5, 9]`: 4 → 9 |
| `MB_YESNOCANCEL` (3) | `IDYES 6` · `IDNO 7` · `IDCANCEL 2` | **6** | **7** | **2** | 6 | `[6, 7, 2, 9]`: 4 → 9 |
| `MB_YESNO` (4) | `IDYES 6` · `IDNO 7` | **6** | **7** | 6 | 6 | `[6, 7, 9]`: 3 → 9, 4 → 6 |
| `MB_RETRYCANCEL` (5) | `IDRETRY 4` · `IDCANCEL 2` | **4** | **2** | 4 | 4 | `[4, 2, 9]`: 3 → 9 |
| `MB_CANCELTRYCONTINUE` (6) | `IDCANCEL 2` · `IDTRYAGAIN 10` · `IDCONTINUE 11` | **2** | **10** | **11** | 2 | `[2, 10, 11, 9]`: 4 → 9 |

(`SEBbuttons` = `[0,8, 0,1,8, 2,3,4,8, 5,6,8, 5,6,1,8, 3,1,8, 1,9,10,8, …]` · `mpTypeIich` = `[0,2,5,12,9,16,19,0]` · `mpTypeCcmd` = `[1,2,3,3,2,2,3,0]` — 각 종류 뒤에 `8 (HELP)` 가 붙어 있어서 `MB_HELP` 는 단순히 한 칸 더 읽음. 표를 위 규칙으로 계산해 맞춤.)

**끝 길 (누르면)** — `MB_DlgProc` `WM_COMMAND` (`+0x275~+0x2a5` · 떨어진 범위 `+0x5eac8~+0x5eb21`): `id = LOWORD(wParam)`. `3 · 4 · 5 · 6 · 7 · 10 · 11` → 곧장 `EndTaskModalDialog` + **`EndDialog(hwnd, id)`**; `1 (IDOK)` · `2 (IDCANCEL)` → **`GetDlgItem(hwnd, id)` 가 있어야** `EndDialog` (없으면 무시 → 고리 안 끝남); `9 (IDHELP)` → 도움말; `8 (IDCLOSE)` 와 그 밖 → 무시 (0). **`WM_CLOSE` / Esc**: `MessageBoxWorker +0x331~+0x346` 이 `[lpmb+0x78]` = (종류 `MB_OK` 면 1, 아니면 단추 중 `IDCANCEL (2)` 가 있으면 2, 없으면 0) 로 적고, `MB_DlgProc +0x1a3 CMP [RDI+0x78],0 → JZ +0x31f` 가 **0 이면 시스템 메뉴의 닫기 (`0xf060`) 를 지움** (`NtUserGetSystemMenu` · `NtUserDeleteMenu`). 곧 **`MB_YESNO` 는 닫기 · Esc 로는 못 끝낸다** (눌러야 `IDYES` / `IDNO`).

**stub 이 "기본 단추를 누른 것으로 치기" 를 하려면**: `NtUserPeekMessage` 첫 출력 `WM_COMMAND (wParam = 위 표의 기본 ID)` — 146 stub 9 의 "ID = `LOWORD(pdlg+0x20)`, 0 이면 1" 은 **`pdlg+0x20` 이 위 ID 일 때만 맞다**. 진짜는 `InternalCreateDialog +0x9ba~+0x9c1` (`SendMessageW(자식, 0x87)` 의 `0x10` 비트) → 조각 `+0x1341~+0x134f` 가 그 단추 ID 로 `pdlg+0x20` 을 덮는다 (한 대화상자에 `DEFPUSHBUTTON` 이 여럿이면 **마지막 것**). 컨트롤 프로시저를 안 타게 (사본 `+0x12` 비트 2 우회) 만든 stub 은 이 줄이 안 타서 `pdlg+0x20 = 1` 로 남음 → **`MB_YESNO` 가 `IDOK (1)` 를 `WM_COMMAND` 로 받아 `MB_DlgProc` 가 `GetDlgItem(hwnd, 1)` 에서 못 찾고 무시 → 끝없는 고리 (F7)**. 고치는 법 두 가지: ① 단추 사본 `+0x1c & 0xf == 1` 인 컨트롤 ID 를 `pdlg+0x20` 에 직접 씀 ② 위 표를 stub 이 직접 (종류 · `MB_DEFBUTTON*` → ID).

---

## 표 D. 비트표 값 (`user32+0xda568 ~ +0xda580` = `gSharedInfo+0x218 ~ +0x230`)

| user32 칸 | gSharedInfo | 이름 `[추측]` | 값 | 채우는 줄 |
|---|---|---|---|---|
| `+0xda568` (dword) | `+0x218` | `DefWindowMsgs` 한도 | **`0x33f`** (목록의 가장 큰 번호) | `InitMessageTables +0x7c · +0x8d` → `[세션+0x5468]` → `InitMapSharedSection +0x2fe/+0x304` (`[R15+0x220]`) |
| `+0xda570` (포인터) | `+0x220` | `DefWindowMsgs` 비트 배열 (`(한도 >> 3) + 1` 바이트, `SharedAlloc`) | 아래 목록의 번호 비트가 1 | `InitMessageTables +0xba` (`[세션+0x5470]`) · 비트 켜기 `+0x626~+0x646` · `InitMapSharedSection +0x338` |
| `+0xda578` (dword) | `+0x228` | `DefWindowSpecMsgs` 한도 | **`0x349`** | `InitMessageTables +0xe4 · +0xf5` → `[세션+0x5478]` → `InitMapSharedSection +0x33f/+0x345` |
| `+0xda580` (포인터) | `+0x230` | `DefWindowSpecMsgs` 비트 배열 | 아래 목록 | `InitMessageTables +0x122` (`[세션+0x5480]`) · `+0x660~+0x680` · `InitMapSharedSection +0x38c` |

`RealDefWindowProcWorker` 의 쓰임: `msg ≤ 한도1` 이고 `DefWindowMsgs` 비트가 1 → **`NtUserMessageCall (fnid 0x29e)`** (`+0x46 · +0x76~+0x82 · +0xb6 · +0xc6`); 아니면 `msg ≤ 한도2` 이고 `DefWindowSpecMsgs` 비트 1 → 유저 쪽 점프표 (`+0x4e · +0xcd~+0xd7 · +0xdd~`); 둘 다 아니면 **0 반환 (`+0x56`)**.

- `DefWindowMsgs` (43 개, `[바이너리]` rva `0x374cb0`): `0x33 0x32 0xb 0xc 0xf(WM_PAINT) 0x10(WM_CLOSE) 0x14(WM_ERASEBKGND) 0x1f 0x20 0x26 0x27 0x2b 0x4d 0x35 0x81 0x80 0x83 0x85 0x86(WM_NCACTIVATE) 0xa0 0xa5 0xa4 0xa1 0xa2 0xa3 0x101 0x105 0x106 0x112(WM_SYSCOMMAND) 0x22b 0x3b 0x88 0x317 0x7f 0x7b 0x313 0x50 0x51 0x128 0xae 0xaf 0x31b 0x33f`.
- `DefWindowSpecMsgs` (77 개, rva `0x374c00`): `0x6(WM_ACTIVATE) 0xd 0xe 0x205 0x11 0x13 0x18 0x21 0x53 0x2e 0x2f 0x100 0x104 0x109 0x22a 0x46 0x47 0x8b 0x19 0x132~0x137 0x84(WM_NCHITTEST) 0xac 0x138 0x55 0x219 0x218 0x20a 0x20e 0x20c 0x290 0x291 0x286 0x10f 0x10d 0x10e 0x284 0x281 0x283 0x282 0x285 0x287 0x8c 0x37 0x127 0x129 0x319 0x240 0x11a 0x119 0x241~0x243 0x245~0x247 0x249~0x24c 0x252 0x251 0x253 0x24e 0x24f 0x24d 0x2cd 0x272 0x349 0x5a 0x2e6 0x23b 0x23c`.
- 대화상자 길에 쓰이는 번호: **`0x87` (`WM_GETDLGCODE`) 없음 · `0x111` (`WM_COMMAND`) 없음 · `0x110` (`WM_INITDIALOG`) 없음 → 셋 다 syscall 없이 0.** `0xf` · `0x10` · `0x112` · `0x86` 는 커널로 간다 (stub 이 `NtUserMessageCall` 을 안 주면 위 한 줄 답 6 의 길이 안 열림).

---

## 표 E. 끝 길 syscall 부작용 (146 4-3 표 보탬 — 커널 쪽)

| syscall | 커널 쪽 몸 (정적) | user32 가 반환을 보나 | 줄 |
|---|---|---|---|
| `NtUserPeekMessage` 0x1001 | 표 B. 못 받으면 0, 받으면 MSG 0x30 B (`+0x5c` · `+0x7c` 는 0 으로 비움) | 0 / ≠0 만 | `NtUserPeekMessage +0xff · +0x120~+0x159` |
| `NtUserSetWindowPos` 0x1021 (`0x97`) | `HIDEWINDOW` 포함: `xxxEndDeferWindowPosEx` 길 (한 줄 답 6 ④). **`WM_WINDOWPOSCHANGING` / `CHANGED` · 숨김 · 포커스 · 활성 바뀜 보내기가 일어날 수 있음** (대화상자 프로시저가 `WM_SHOWWINDOW` · `WM_WINDOWPOSCHANGED` 를 받음 `[추측: 정확한 차례 · 개수는 안 읽음]`) | 안 봄 (`EndDialog` 는 늘 1) | `NtUserSetWindowPos +0x8f · +0x14b` → `xxxSetWindowPos +0x66` → `xxxSetWindowPosAndBand +0x39 · +0x76 · +0x8b` → `xxxEndDeferWindowPosEx +0x15b · +0x203 · +0x68c · +0x6a0` · `xxxSendChangedMsgs +0xe0~+0xe8` |
| `NtUserPostMessage` 0x100d (`EndDialog` 가 `(hDlg, 0, 0, 0)`) | 큐에 `message = 0` 한 개 (아래 `[추측]`) | 안 봄 | `EndDialog +0xb0` (146) |
| `NtUserDestroyWindow` 0x1095 | 한 줄 답 6 ③: `WM_DESTROY` · `WM_NCDESTROY` **보내기 (클라이언트 되부름)** → 앱 프로시저가 둘 다 받음; `NCDESTROY` 에서 `DefDlgProcWorker +0x31a` 가 `pdlg` 해제 | 안 봄 | `xxxDestroyWindow +0x3f~+0xcf` · `Phase2 +0x3a9` → `xxxDW_SendDestroyMessages +0xa8 · +0xeb` · `xxxFreeWindow +0x1e · +0x26 · +0x31 · +0x3f` → `xxxFreeWindow_Phase2 +0x3c · +0x49` |
| `NtUserMessageCall` 0x1005 | `msg < 0x400`: `MessageTable[msg] & 0xff` 로 `gapfnMessageCall[…]` · `fnid` 인자 (`0x29e` → `xxxDefWindowProc` 쪽); `msg ≥ 0x400`: 세션 표 `[… + (fnid+6 & 0x1f)*8 + 0x11a48]` | 반환 그대로 | `NtUserMessageCall +0x172~+0x20b` |
| `NtUserSetWindowLongPtr` 0x15da | 인덱스 `≥ 0` 대화상자 (fnid `0x2a4`): `0` (`DWLP_MSGRESULT`) · `0x10` (`DWLP_USER`) → 여분 블록, `8` 은 유저 모드. **`< 0`**: `xxxSetWindowData` — `−21` 사본 `+0xd8` · `−12` (`GWLP_ID`, 자식이면) 사본 `+0x140` · `−6` 사본 `+0x20` · `−4` (`GWL_WNDPROC`) 사본 `+0x78` (+ `0x208` · `0x204` 비트 갱신) · `−16` / `−20` 스타일 · `−8` 부모 · `−40` 사본 `+0xe8` 비트 5 | **앱이 옛 값을 쓰면 봄** | `xxxSetWindowLongPtr +0x4a · +0x170~+0x17f · +0x467~+0x47c` · `xxxSetWindowData +0x3b~+0x79 · +0xd0 · +0x1e6 · +0x486 · +0x6b9` |
| `NtUserSetMsgBox` 0x1573 (`MB_DlgProc +0xec`) | **사본 `+0x10` 비트 5 (`SetOrClrWF(1, pwnd, 0x20, 0)`) 를 켜고** `[[pwnd+0x18]+8]` 이 가리키는 곳 `+0x30` 을 1 올림 (소유 스레드/데스크톱 칸 `[추측]`) → `DialogBox2 +0x289` 의 Ctrl+C / Ctrl+Ins 길을 켬 | 안 봄 (`+0xf3` 뒤 안 씀) | `NtUserSetMsgBox +0x2d~+0x8a` · `MB_DlgProc +0xec` |
| `NtUserProcessConnect` 0x10e2 (프로세스 시작 한 번, 이 길 앞) | `InitMapSharedSection` 이 `USERCONNECT` 를 채워 사용자 버퍼로 복사 (`+0x297 · +0x5a3`) — `gpsi` 주소 (`[R15+0x8]`) · 핸들 표 · 표 D | 반환이 음수면 `ClientThreadSetup` 이 0 으로 돌아감 | `NtUserProcessConnect +0x260 · +0x297` · `InitMapSharedSection +0x244~+0x3c7` |

- `EndDialog` 가 올린 `message = 0` 은 `DialogBox2` 가 `pdlg+0x18` 비트 0 을 맨 위에서 보고 빠지므로 **그 고리에서는 안 꺼내진다** → 같은 스레드의 다음 `PeekMessage` / `GetMessage` (앱 자기 루프) 가 가장 먼저 받는다 `[추측: 큐 코드는 안 읽음; 146 E5 · E17 에서 파생]`.
- `WM_DESTROY` · `WM_NCDESTROY` 를 stub 이 안 보내면: 146 F10 그대로 (누수만, 반환 무관). 앱이 `WM_DESTROY` 에서 `PostQuitMessage` 를 부르는 구조 (`while (GetMessage())` 로 끝나는 앱) 면 **안 보내는 stub 은 앱 자기 루프가 영원히 돎** (이 요청에서 앱 코드 안 봄 `[추측]`).

---

## PE_emulator 쪽 — 지금 맞나 · 틀리나 (146 5절에 이어)

읽은 곳: `PE_emulator/ext/syscall/syscall_layer_cpp/src/gui/` 의 `nt_user_peek_message.cpp` (53 줄) · `nt_user_wait_message.cpp` (20 줄) · `nt_user_create_window_ex.cpp` 머리 주석 · 파일 목록 + `syscall_layer_cpp/src` 전체에서 이름 grep (`NtUserDestroyWindow` · `NtUserSetWindowPos` · `NtUserSetMsgBox` · `NtUserMessageCall` · `NtUserSetWindowLongPtr` · `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 `gui/` 에 몸 파일이 **없고** 이름이 나오는 곳은 `nt_user_create_window_ex.cpp` 머리 주석 한 줄뿐 — **몸 없는 기본 대답** 으로 보임, 다른 모듈 (프로세스 시작 쪽) 이 따로 받는지는 안 따라감 `[공백]`).

- **`NtUserPeekMessage` (게시 큐만 · 없으면 `gui_peek_message_none` 멈춤)**: 순서 면에서는 **진짜의 P3 (게시) 한 칸만 한 꼴** — 맞음. 빠진 것: P2 · P4 (입력, 없어도 됨) · **P5 `WM_PAINT` · P6 `WM_TIMER`** (진짜는 칠할 창이 있는 한 `WM_PAINT` 를 계속 줌, 한 줄 답 2). 큐가 비면 "멈춤" 은 진짜의 "0 + `WaitMessage` 잠" 과 달라서 `[정책]` — 단 stub 이 `WM_PAINT` 를 만들어 주려면 **`NtUserMessageCall(WM_PAINT)` 가 갱신 영역을 비워야** 안 끝없이 돎 (표 B 끝 `[추측]`). 거르기 인자 (`min`/`max` ≠ 0) 를 멈춤으로 두는 것은 진짜 (`CalcWakeMask` 로 범위를 받음) 보다 좁음 — 모달 고리 `DialogBox2` 는 (0, 0, 0) 만 줘서 무해. `wRemove` 윗 16 비트 (`PM_QS_*`) 를 "안 하는 것" 으로 멈추는 것도 같은 이유로 무해 (진짜는 허용, 마스크를 좁힐 뿐).
- **`NtUserWaitMessage` (잠)**: 맞음 — 진짜도 빈 틈 (P7 = 0) 뒤에 잠. 깨움 조건은 큐 · 입력 · 보낸 메시지 · 타이머.
- **`NtUserCreateWindowEx` (hwnd · 사본만 만들고 자식 연결 · ANSI 안 함)**: 146 stub 10 은 이번 줄로 **고쳐 확정**: 연결 순서 = 템플릿 차례 (끝에 붙임, 표 A) 이고 **`사본 +0x50` (이전 형제) 도** 써야 함 (`GetWindow(GW_HWNDPREV)` · `GetNextDlgTabItem(이전)` 이 읽음 `[추측]`). **A 판** (`DialogBoxIndirectParamA`) 이면 `사본 +0x78` 이 `DefDlgProcW` 가 아니라 **`DefDlgProcA` (A 표 10 번째)**, 컨트롤은 A 판 프로시저 (표 A' B4 · B5) — 지금은 W 판만 쓴다고 알고 있음 (`+0x78 = user32 + kDefDlgProcWRva`). A 판 시료에서 틀림. `사본 +0x13` 비트 5 · `+0x12` 비트 3 은 끝 길에 안 쓰여서 안 틀림 (146 4-4 결론 그대로).
- **MessageBox**: 위 표 C 로 **틀릴 수 있는 곳 둘** — ① `MBStrings` (`gpsi+0x3a4 + 0x28*i`) 를 안 채우면 `MessageBoxWorker +0x2c8` 이 `uID = 0` 을 읽어 단추가 전부 ID 0 → 반환 0 ② `pdlg+0x20` 이 `1` 로 남으면 `MB_YESNO` 등이 끝없는 고리 (표 C 끝). 기본 단추를 누른 것으로 칠 때 ID 는 **표 C 의 `(종류, DEFBUTTON)` 칸**; 닫기 / Esc 는 `MB_OK` 는 `IDOK`, 취소 단추가 있으면 `IDCANCEL`, 없으면 **무시** (`MB_YESNO` 는 못 끝냄).

### stub 이 채워야 하는 최소 목록 — 146 의 9 ~ 17 에 이어 붙임

18. **자식 연결 (146 10 번 보탬)**: 컨트롤 사본을 만들 때마다 부모의 **마지막 자식 뒤에** 붙임 — 첫 자식이면 `[부모사본+0x38] = 새`, 아니면 `[마지막+0x48] = 새` · `[새+0x50] = 마지막`; `[새+0x30] = 부모사본` · `[새+0x48] = 0` · `[부모사본+0x38]` 은 첫째 그대로. (맨 위 `HWND_TOP` 은 반대: 첫 자식 앞.)
19. **A 판 (146 4-4)**: 확장 스타일 `0x80000000` 이 오면 저장 스타일에서 지우고, `사본 +0x78` 을 **A 표 칸** (대화상자 = `DefDlgProcA`, 단추 = `ButtonWndProcA` …) 으로, `사본 +0x13` 비트 5 켬; `사본 +0x12` 비트 3 은 클래스가 ANSI 이거나 (ANSI 만든이 ∧ 내장 클래스 아홉) 일 때만. 이 표는 `NtUserInitializeClientPfnArrays` (win32u `0x14aa`) 가 `gpsi+0x188 (A) / +0x248 (W)` 에 받아 둠 — **그 syscall 이 몸이 없으면 `사본 +0x78` 에 쓸 값을 모름**.
20. **`NtUserProcessConnect` 가 돌려주는 `gSharedInfo` (0x240 B)**: 비트표 둘이 필요하면 `+0x218` = `0x33f` · `+0x220` = 표 D 목록 비트 배열, `+0x228` = `0x349` · `+0x230` = 비트 배열. **안 채우면 (한도 0) 모든 `DefWindowProc` 가 syscall 없이 0 → 끝 길엔 영향 없음** (`WM_COMMAND` · `WM_GETDLGCODE` 는 어차피 0), 대신 `WM_PAINT` · `WM_CLOSE` · `WM_SYSCOMMAND` · `WM_NCACTIVATE` 의 기본 처리가 아무 일도 안 함.
21. **`MBStrings` (`MessageBox` 필수)**: `gpsi+0x3a4 + 0x28*i` (i = 0 ~ 10): `+0x00` wchar[15] 이름 (`"OK"` · `"Cancel"` · `"&Abort"` · `"&Retry"` · `"&Ignore"` · `"&Yes"` · `"&No"` · `"Close"` · `"Help"` · `"&Try Again"` · `"&Continue"` 꼴, 이름은 보이는 것만 영향) · **`+0x20` (= `gpsi+0x3c4+0x28*i`) `uID = i + 1`** · `+0x24` `uStr = 0x320 + i`.
22. **`pdlg+0x20` 을 기본 단추 ID 로**: 컨트롤 프로시저를 안 탄다면 단추 사본 `+0x1c & 0xf == 1` (`BS_DEFPUSHBUTTON`) 인 컨트롤의 ID 를 `[[사본+0x128]+8]+0x20` 에 써 줄 것 (표 C).
23. **`NtUserSetMsgBox (0x1573)`**: `MB_DlgProc` `WM_INITDIALOG` 가 부름 (반환 안 봄) → 아무 값 (사본 `+0x10` 비트 5 는 Ctrl+C 길용이라 끝 길엔 무관).
24. **`NtUserMessageCall (0x1005)` 몸 (`WM_PAINT` 를 주는 경우만)**: `msg = 0xf` 이면 대화상자 · 컨트롤의 갱신 영역을 비운다 (아니면 같은 `WM_PAINT` 반복).
25. **`NtUserDestroyWindow` (146 13 번 보탬)**: `WM_DESTROY (2)` · `WM_NCDESTROY (0x82)` 를 **앱 프로시저에 보낼지** 는 정책 — 안 보내도 모달 끝 길은 닫힘 (146 F10); 앱이 `WM_DESTROY` 에서 `PostQuitMessage` 로 끝내는 구조면 보내야 앱 루프가 끝남.

---

## VM 으로 확인할 것 — 디버거 필요 (Q273 VM GUI 세션에 얹을 꼴)

멈춤점은 user32 · 사용자 모드 (프로세스 조건 불필요). 오프셋은 위 줄 (call 뒤 = `ret_off`).
1. **`user32!DialogBox2+0x270`** (`_PeekMessage` 반환 직후) 에서 `r eax` · `dq @rsp+0x50 L6` — **첫 부름 · 둘째 부름 … 의 `message`** (표 B: `0xf` WM_PAINT 가 먼저 오는지, 몇 번 오는지, 어느 hwnd 인지; 0 이 되는 때 `+0x23f` `NtUserWaitMessage` 가 부름).
2. **자식 연결 진짜 값**: 대화상자 사본 `dq @rax+0x30 L6` (`+0x30 +0x38 +0x40 +0x48 +0x50`) → 첫 자식 `dq poi(@rax+0x38) L6` … `+0x48` 으로 따라가며 각 컨트롤의 `dd …+0x140 L1` (ID) · `+0x50` (이전). 템플릿 차례와 같은지, 마지막 `+0x48` = 0 인지.
3. **A 판**: `DialogBoxIndirectParamA` 시료 하나로 `user32!InternalCreateDialog+0x658` 직후 사본 `dq @rax+0x78 L1` (= `DefDlgProcA` 인지 — `x user32!DefDlgProcA` 와 비교), `db @rax+0x12 L2` (비트 3 · `+0x13` 비트 5), `dq poi(user32+0xda590)+0x1d8 L1` · `+0x298 L1` (A / W 표 10 번째).
4. **MessageBox 종류별** (`MB_YESNO` · `MB_YESNOCANCEL | MB_DEFBUTTON2` · `MB_CANCELTRYCONTINUE` · `MB_OK | MB_HELP`): `user32!MessageBoxWorker+0x34e` 직전 `[rbx+0x60]` (단추 ID 배열) · `[rbx+0x74]` (기본 번호) · `[rbx+0x78]` (취소 ID) · `user32!InternalCreateDialog` 조각 `+0x134f` 직후 `[[사본+0x128]+8]+0x20` (`dq`) — 표 C 와 같은지.
5. **`gpsi` `MBStrings`**: `dd poi(user32+0xda590)+0x3c4 L1` · `+0x3ec` · `+0x414` … (0x28 간격 11 칸, `uID` = 1 ~ 11 인지) · `dd …+0x3c8` (`uStr` = `0x320 ~ 0x32a`).
6. **비트표**: `dd user32+0xda568 L1` (= `0x33f`) · `dd user32+0xda578 L1` (= `0x349`) · `db poi(user32+0xda570)+(0x87>>3) L1` 의 비트 7 (0 이어야) · `0xf` · `0x10` · `0x112` 의 비트.
7. **`xxxButtonNotifyParent`**: 단추 한 번 눌러 `user32!xxxButtonNotifyParent` 에서 `edx` (코드) · `[rcx+0x140]` · 부모로 가는 `SendMessageW` 의 `r8` (= ID | 코드 << 16).
8. **끝 길의 보냄들**: `DestroyWindow` 하는 동안 대화상자 프로시저가 받는 메시지 차례 (`WM_DESTROY` · `WM_NCDESTROY` 가 오는지 — `user32!DefDlgProcWorker` 입구에 조건부 로그) · `EndDialog` 뒤 큐에 `message = 0` 이 남는지 (`user32!PeekMessageW` 다음 부름).
9. **SC_CLOSE**: 대화상자에 `PostMessage(WM_SYSCOMMAND, SC_CLOSE)` → 앱 프로시저가 `WM_CLOSE` 를 받고 (`DefDlgProcWorker +0x448~+0x467` 로) `WM_COMMAND(IDCANCEL)` 이 큐에 오는지.

## VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것

- **MessageBox 반환값 표** (시료 프로그램): `MB_OK` · `MB_OKCANCEL` · `MB_ABORTRETRYIGNORE` · `MB_YESNOCANCEL` · `MB_YESNO` · `MB_RETRYCANCEL` · `MB_CANCELTRYCONTINUE` 각 `DEFBUTTON1 ~ 4` (+ `MB_HELP` 하나) 를 띄워 **Enter 한 번 (또는 마우스로 기본 단추)** 쳐서 `MessageBox` 반환값을 파일에 적게 함 → 표 C 와 비교. 닫기 (×) · Esc 도 한 번씩 (`MB_YESNO` 는 안 닫혀야).
- **자식 순서** (시료 프로그램): `DialogBoxParam` 의 `WM_INITDIALOG` 에서 `GetWindow(hDlg, GW_CHILD)` 부터 `GW_HWNDNEXT` 로 따라 가며 ID 를 찍음 (+ `GW_HWNDPREV` 로 거꾸로) → 템플릿 차례와 같은지. `MessageBox` 상자 (`FindWindow(L"#32770")` 후 `EnumChildWindows` / `GetWindow`) 도: 단추가 먼저 오는지.
- **대화상자 프로시저가 받는 메시지 차례** (시료 프로그램): `DLGPROC` 맨 앞에서 모든 `uMsg` 를 시각과 함께 로그 (`WM_INITDIALOG` 뒤 `WM_PAINT` 가 몇 번, `WM_ERASEBKGND` · `WM_NCACTIVATE` · `WM_SHOWWINDOW` · `WM_WINDOWPOSCHANGED` 차례, 끝낼 때 `WM_DESTROY` · `WM_NCDESTROY`) — 보내는 메시지 (SendMessage) 도 대화상자 프로시저엔 다 보임. 앱 자기 루프의 `GetMessage` 로도 `message = 0` 이 오는지 (모달 뒤 첫 `GetMessage`).
- **A 판**: 같은 대화상자를 `DialogBoxIndirectParamA` 로 띄워 `IsWindowUnicode(hDlg)` · `GetWindowLongPtrA/W(GWLP_WNDPROC)` 차례 결과 (A 판이면 `IsWindowUnicode` 의 의미 확인 — 클래스 ANSI 비트 3 이 꺼져 있으면 TRUE 로 나올 것 `[추측]`).
- **`GetUserDefaultLCID`** 반환값 (콘솔 출력) + `GetSystemDefaultLCID` · `GetThreadLocale` · `GetUserDefaultLocaleName` — 에뮬레이터 stub 이 이미 만난 값과 비교 (`0x409` 인지 `0x1000 → 0xc00` 바꿈 갈래인지).
- **`SetWindowLongPtr(h, GWLP_USERDATA, v)` / `GetWindowLongPtr`** 왕복 (표 E) 과 `GWLP_ID` 쓰기 후 `GetDlgCtrlID`.

## 다른 프로젝트 몫

- **kernelbase `GetCurrentNlsCache` 안쪽** (`0x52350`, 4643 B) — `GetUserDefaultLCID` 가 의존. 이번엔 호출 이름만 읽음 (`CsrClientCallServer` · `RtlOpenCurrentUser` · `NlsUpdateCacheInfo` · `GetNamedLocaleHashNode` 갈래). NLS 사용자 정보 (`gpServerNlsUserInfo`) 를 누가 채우는지는 csrss / basesrv 쪽 (`다음 요청 후보`).
- **imm32 `ImmTranslateMessage`** — 프로젝트 없음. `TranslateMessage` 가 `wParam == 0xE5` 일 때만 부름 (보통 안 탐).
- **gdi32 / gdi32full** — `MB_DlgProc` 의 `SetTextColor` · `SetBkMode` · `GetSysColorBrush` 와 `SoftModalMessageBox` 의 `GetTextExtentPointW` 는 150 이 닫은 쪽.

## 다음 요청 후보

- 대화상자 클래스 원자 (#32770) 가 `gpsi+0x364 ~ +0x388` 아홉 칸에 드는지 — 원자를 채우는 곳 (`xxxRegisterSystemClasses`? 이름은 `_index.tsv` 에서 확인 필요) 을 읽으면 표 A' B9 의 `[공백]` 이 닫힘 (`사본 +0x12` 비트 3 이 A 대화상자에서 켜지는지).
- `xxxEndDeferWindowPosEx` 안 `SWP_HIDEWINDOW (0x80)` · `SWP_NOACTIVATE (0x10)` 갈래 (`xxxCalcValidRects` · `xxxSwpActivate` · `xxxShowWindowEx` 로 가는지, `WM_SHOWWINDOW (0)` · 활성 바뀜 보내기가 있는지) — 표 E 의 `NtUserSetWindowPos` 칸의 `[추측]` 을 닫음.
- `xxxInternalDoPaint` (`WM_PAINT` 가 나오는 창 차례 · `WM_ERASEBKGND` / `WM_NCPAINT` 보내기 · 반복 조건) — 표 B 의 `[추측]`.
- `xxxReadPostMessage` (`WM_QUIT` 만들기 · 지움 처리 · 거르기) 와 `xxxScanSysQueue` (입력 없을 때 반환) — 입력이 전혀 없는 에뮬레이터에선 P4 가 항상 빈 줄이라는 확인.
- `ButtonWndProcWorker` 의 클릭 입구 (마우스 뗌 · 스페이스 · `BM_CLICK`) 와 `xxxPressButton` (한 줄 답 5 `[공백]`).
- `gfDMREnable` · `gfEMIEnable` (user32 `0xda904` · `0xda930`) 를 정하는 곳 (`MessageBoxWorker +0xcf · +0xe8`) — 호환 시스템이 "기본 단추 ID 곧장 반환" 을 켜는 조건.
- `xxxDW_SendDestroyMessages` 가 `WM_DESTROY` 앞에 보내는 `0x272` 가 무슨 메시지인지.

## 판정

**분석성공.** 요청서 1 ~ 7 모두 줄 근거와 함께 답했다 (`GetUserDefaultLCID` 는 처음 152 결과서가 없어 `[objdump]` 로만 보다가, 작업 중 생겨서 kernelbase 를 열어 확인 — 몸은 닫혔고 값이 나오는 NLS 캐시 안쪽만 `[공백]`). 핵심 발견 여섯: ① **새 자식은 부모 자식 목록의 맨 끝에 붙는다 (`LinkWindow(…, HWND_BOTTOM, …)`) = 템플릿 차례** (146 stub 10 확정, 이전 형제 `사본 +0x50` 추가); ② **확장 스타일 `0x80000000` 은 `사본 +0x78` 을 A 표 칸으로 · `+0x13` 비트 5 를 켜지만 `+0x12` 비트 3 은 클래스가 ANSI 일 때 (또는 만든이 ANSI ∧ 내장 클래스 아홉)**; ③ **`PeekMessage` 차례 = 게시 → 입력 → `WM_PAINT` → `WM_TIMER` → 0, 보낸 메시지는 안 나옴** (막 선 모달 대화상자의 첫 부름은 `WM_PAINT`); ④ **MessageBox 는 같은 `DialogBox2` 고리, 반환은 눌린 ID 그대로, 기본 단추는 `MB_YESNO` = `IDYES (6)` 등 표 C — `IDOK` 로 고정하면 틀림; `pdlg+0x20` 이 단추 `WM_GETDLGCODE` 답으로 정해지고 stub 이 컨트롤을 우회하면 `1` 로 남아 끝없는 고리**; ⑤ **비트표는 `NtUserProcessConnect` 의 `gSharedInfo` 복사 (`InitMessageTables` 가 만든 배열), `WM_GETDLGCODE` · `WM_COMMAND` · `WM_INITDIALOG` 는 목록에 없어 syscall 0**; ⑥ **`WM_CLOSE` → `xxxDestroyWindow` 곧장 (대화상자는 `DefDlgProcWorker` 가 먼저 `WM_COMMAND(IDCANCEL)` 로 바꿈), `SC_CLOSE` = `WM_CLOSE` 보내기, `DestroyWindow` 는 `WM_DESTROY` · `WM_NCDESTROY` 를 클라이언트에 되부르며 보냄, `GWLP_USERDATA` = 사본 `+0xd8`.** 정적으로 못 닫은 것: 대화상자 클래스 원자가 아홉 칸에 드는지 · `SetWindowPos(0x97)` 의 정확한 보내기 차례 · `WM_PAINT` 반복 조건 · 단추 클릭 입구 · `GetCurrentNlsCache` 안쪽 — 표시했고 위 목록에 넘김. MS 코드는 레포에 붙이지 않았다 (`ms/disassemble/…` 경로와 `+0x…` · 우리 말 요약만).

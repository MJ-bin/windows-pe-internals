# 2026-11-27 (작성일, 정적 분석 2026-11-27) run157 — 대화상자 · 메시지 상자의 커널 쪽 몸 (Q282, 정적만): 새 컨트롤은 부모 자식 목록의 **맨 끝** 에 붙는다 (`xxxCreateWindowEx` 가 `WS_CHILD` 면 `HWND_BOTTOM (1)` → `LinkWindow` 가 마지막 형제 뒤에) — **연결 차례 = 템플릿 차례**, 이전 형제 칸 `사본+0x50` 도 쓴다. `NtUserPeekMessage` 의 꺼내는 차례는 **보낸 것 (안에서 처리) → 게시 → 입력 → `WM_PAINT` → `WM_TIMER` → 0** 이라, 막 선 모달 대화상자 (입력 · 게시 없음) 의 첫 부름은 **`WM_PAINT`** 다 (`WM_NCACTIVATE` 류는 보내기라 안 나옴). `MessageBox` 는 같은 `DialogBox2` 고리를 타고 반환을 바꾸지 않으며, 기본 단추 ID 는 `MessageBoxWorker` 표에서 정해진다 — **`MB_YESNO` = `IDYES (6)`** (`IDOK` 아님). user32 비트표 (`+0xda568 ~ +0xda580`) 는 `NtUserInitializeClientPfnArrays` 가 아니라 **`gSharedInfo` 복사** (보통 프로세스는 csrss USERSRV 연결 답, `NtUserProcessConnect` 는 서버 프로세스만 — run157 해설 때 정정) 이고 값은 한도 `0x33f` · `0x349`, `WM_GETDLGCODE` · `WM_COMMAND` · `WM_INITDIALOG` 는 목록에 없어 syscall 없이 0. `WM_DESTROY` · `WM_NCDESTROY` 는 커널이 되부르며 보낸다, `GWLP_USERDATA` = `사본+0xd8`

> **정정 (run157 해설 때 대조 — explainer 가 찾은 근거 어긋남, `.asm` 으로 가림)**: 비트표 (`user32+0xda568 ~ +0xda580` = `gSharedInfo+0x218 ~ +0x230`) 를 "`ClientThreadSetup +0x120 NtUserProcessConnect` 가 받은 0x240 B 복사" 라고 **조건 없이** 적은 것은 틀렸다 — **run85 가 맞다**. `ClientThreadSetup +0xdd MOV EAX,[gfServerProcess]` · `+0xe3 TEST` · `+0xe5 JZ → +0x258` (끝) 과 `+0xeb TEST EDI` · `+0xed JZ` (EDI = 옛 `gfFirstThread`) 때문에 `+0x120 NtUserProcessConnect` · `+0x134 ~ +0x1ad` 복사 (와 `+0x228 NtUserInitializeClientPfnArrays`) 는 **`gfServerProcess ≠ 0` (csrss 같은 서버 프로세스) 의 첫 스레드에서만** 탄다 `[정적]`. **보통 프로세스**는 `_UserClientDllInitialize +0x2aa CsrClientConnectToServer(…, 3, [RSP+0x60] 0x248 B, 0x248, &gfServerProcess)` → `+0x2be CMP [gfServerProcess],0` · `+0x2ca JNZ` 안 탐 → (PEB `+0x3` 비트 `0x2` 꺼짐이면) **`+0x30b ~ +0x37c` 가 답 `[RSP+0x68]` (= 답 `+8`) 의 0x240 B 를 `gSharedInfo` 로 복사** → `+0x387` `gpsi` `[정적]`. 그래서 비트표 칸은 USERSRV 답 버퍼로 **`+0x220` · `+0x228` · `+0x230` · `+0x238`** 이고, PE_emulator 쪽 대상은 **`csr_user_connect_stub.cpp` (USERSRV 연결 답)** 이다 (목록 20 고침). 값 (`0x33f` · `0x349` · 목록 43 · 77) · 커널 쪽 `InitMessageTables` → 세션 칸 → `InitMapSharedSection` 줄은 그대로; csrss 가 보통 프로세스 답을 무엇으로 채우나는 안 읽음 (`[공백]`, 같은 `NtUserProcessConnect` 를 클라이언트 핸들로 부를 것 `[추측]`). A / W 표 (`gpsi+0x188` · `+0x248`) 도 보통 프로세스에선 `NtUserInitializeClientPfnArrays` 를 안 부르므로 `gpsi` 공유 메모리에 이미 있는 값을 읽는 것 `[추측]` (목록 19 고침). 고친 곳: 머리 · 요약 (4) · 호출 지점 표 (`ClientThreadSetup` 두 줄 + `_UserClientDllInitialize` 한 줄 새로) · 2절 배열 쪽 · 5절 user32 쪽 길 · 7절 표 `NtUserProcessConnect` 줄 · 나무 · PE_emulator 쪽 표 · 목록 19 · 20 · 확인된 것 · 앞 run 고쳐 읽기 run148 ① · flow-writer 대조 7 · 8 (7 = `EndDialog` `NtUserPostMessage` call `+0xd9`). run148 문서 맨 위 ① 과 flow.md 3장 · 4장 같은 말도 맞췄다. 지도 범위 4장 목록에 run146 PE_emulator 줄을 보탰다.

(글의 오프셋은 **함수 시작 기준** `+0x…` — 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표 셋째 칸. 조각 (`frag:FUN_…`) 기준 오프셋은 늘 "조각 `FUN_… +0x…`" 로 따로 적고 함수 기준 값을 괄호로 붙였다. 결과서 값과 call 줄이 다르게 읽히는 곳은 아래 "flow-writer 대조". 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** 의 남은 정적 몫 (원 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-dialog-window.md` 의 뒷몫, 380 묶음 차례 ④ "Q278b · Q282" 의 뒷몫 — `380_internals-r19-lanes.md` 머리 "차례 (우리 D179)") 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 user32 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run142 · run146 · run148 과 같은 줄). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (user32 · win32kbase · win32kfull · kernelbase 줄 한 마디 · 근거 줄) · 3장 run142 절 한 줄 · run146 절 한 줄 · run148 절 한 줄 + 새 하위 절 "커널 쪽 몸" · 4장 (run146 정적 5 · run146 PE_emulator 줄 (run157 해설 때 대조로 보탬) · run148 동적 · 정적 · PE_emulator 줄 표시 + run157 줄 셋) · 5장 (run157 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q282** (run148 남은 정적 — sast/146 6절 "win32k 몫 남음" 1 ~ 7 · 8절 "다음 요청 후보", 사용자 결정 2026-11-27 PE_emulator D179 380 차례 ④). 결과서 판정 **분석성공** — 요청서 1 ~ 7 모두 줄 근거로 답함. 못 닫은 것: 대화상자 클래스 원자가 `gpsi+0x364 ~ +0x388` 아홉 칸에 드는지 · `SetWindowPos(0x97)` 의 정확한 보내기 차례 · `WM_PAINT` 반복 조건 · 단추 클릭 입구 · `GetCurrentNlsCache` 안쪽.
> - 이어 쓰는 앞 run: **run148** (sast/146 — 모달 고리 끝 길 · stub 최소 목록 7 ~ 15 · 남은 정적 1 ~ 9), **run146** (sast/142 — 대화상자 창 · 사본 칸 표 · 최소 목록 1 ~ 7), **run142** (sast/139 — 창 API 가드 · 메시지 상자 최소 길 · `psi` 단추 글 칸), **run153** (sast/150 — `g_systemCallFilterId` · 필터 5 표 E, 이번엔 가리키기만).
> - 앞 run 고쳐 읽기 셋: run142 · run146 · run148 (과 각 해설서) 맨 위 한 줄 (아래 "앞 run 고쳐 읽기"). 원문은 그대로 두었다.
> - 남은 것 → 동적은 Q273 VM GUI 세션에 얹을 꼴 (결과서 "VM 으로 확인할 것 — 디버거 필요" 1 ~ 9), 디버거 없이 떠 올 것 (시료 프로그램 여섯 가지), 정적은 결과서 "다음 요청 후보" 7 — 대기열 자리는 오케스트레이터가 정함.

## 요약

- **(1) 자식 연결 순서** `[정적]`: `xxxCreateWindowEx` 는 처음 `hwndInsertAfter` 를 `HWND_TOP (0)` 으로 두다가 (`+0xb51`), **`WS_CHILD` 창이면 `HWND_BOTTOM (1)` 로 덮는다** (`+0xbc4 CMP CL,0x40 · +0xbc7 JZ → +0x2014 … +0x2049 MOV [RSP+0xe8],R15`, R15 = 1 은 `+0x1f7`). `PWInsertAfter` (`+0x167b`) 는 0 · 1 · −1 · −2 를 그대로 돌려주고, `LinkWindow(새 창, 1, 부모)` (`+0x1720`) 는 부모 첫 자식 `[부모+0x70]` 에서 다음 형제 `[+0x58]` 를 따라 **마지막 형제 뒤** 에 끼운다 (`LinkWindow +0x213 ~ +0x253`, 사본 `+0x14` 비트 5 가 켜진 형제 앞에서 멈춤). 그래서 템플릿 차례로 만든 컨트롤은 **사본 `+0x38` (첫 자식) → `+0x48` (다음) … 마지막 `+0x48` = 0** 이고, 끼울 때 **이전 형제 `사본+0x50`** 도 쓴다 (`+0x37a ~ +0x3b0`). run146 stub 10 의 "템플릿 차례" 가 맞다.
- **(1') ANSI** `[정적]`: 확장 스타일 `0x80000000` 은 ① 저장 스타일에서 지워지고 (`+0x1b9 JS → +0x21e4 BTR R12D,0x1f`) ② 깃발 `0x320` 이 되어 (`+0x1bf ~ +0x1cb`) ③ `MapClientNeuterToClientPfn(클래스, 0, 0x320)` (`+0x8f1`) 이 **`사본+0x78` 를 A 표 칸으로** 바꾸고 (`+0x8fd`; 대화상자 = `gpsi+0x1d8` = `DefDlgProcA` 칸) ④ `사본+0x13` 비트 5 를 켠다 (`+0xa16 SetOrClrWF(…,0x320,…)`). **`사본+0x12` 비트 3 (`0x208`) 은 이것만으로 안 켜진다** — 클래스 ANSI 깃발 (`+0xa23 ~ +0xa3a`) 이거나, ANSI 만든이 ∧ 클래스 원자가 `gpsi+0x364 ~ +0x388` 아홉 칸 중 하나일 때 (`+0x1cbb ~ +0x1de8`). 대화상자 클래스가 그 아홉에 드는지는 `[공백]`.
- **(2) `NtUserPeekMessage` 첫 메시지** `[정적]`: 몸은 `NtUserPeekMessage +0xff xxxInternalGetMessage +0x78 xxxRealInternalGetMessage` (요청서의 `xxxPeekMessage` 는 이 빌드에 없다 — 아래 "정정"). 꺼내는 차례: P1 보낸 메시지 (`xxxReceiveMessages`, 안에서 처리 · 반환 안 됨) → P2 (입력 큐 주인 스레드일 때만) 입력 한 번 → **P3 게시** (`QS_POSTMESSAGE`, `+0x8e0 TEST AL,8` → `+0x90e xxxReadPostMessage`) → **P4 입력** (`+0xa2e xxxScanSysQueue`) → **P5 `WM_PAINT`** (`QS_PAINT`, `+0xbb1 TEST AL,0x20` → `+0xbbb xxxDoPaint`: `message 0xf` · wParam = lParam = 0 · hwnd = 칠할 창) → **P6 `WM_TIMER`** (`QS_TIMER`, `+0xca8` → `+0xcb3 DoTimer`) → P7 0. `wRemove & 0xe300fffc` 면 `LastError 0x3ec` · 0 (`NtUserPeekMessage +0xde TEST · +0xe3 JNZ → +0x189`), 비트 0 은 "뺄까" 로 넘어가고 (`+0x22c ~ +0x237 R15D = wRemove & 1`), 비트 1 (`PM_NOYIELD`) 이 없으면 빈 반환 길에서 `zzzWakeInputIdle` · `xxxReceiveMessages` 를 부른다 (`+0x163c TEST · +0x1644 JNZ`, call `+0x1649` · `+0x1651`). **막 선 모달 대화상자 (입력 · 게시 없음) 의 첫 부름 = `WM_PAINT`**, 다 칠하면 0 → `DialogBox2` 빈 틈 → `WaitMessage`.
- **(3) MessageBox 기본 단추 ID** `[정적]` + `[바이너리]`: `MessageBoxW → MessageBoxTimeoutW +0x18a → MessageBoxWorker +0x349 → SoftModalMessageBox +0x5b1 → InternalDialogBox → DialogBox2` — **run148 E1 ~ E21 과 같은 고리**, 반환은 그대로 (`SoftModalMessageBox +0x5b8 ~ +0x5be` 가 −1 만 0 으로, `MessageBoxTimeoutW +0x19e MOV EAX,EBX`). 단추 ID = `gpsi + 칸*0x28 + 0x3c4` (`MBStrings[칸].uID`, 칸 = `SEBbuttons[mpTypeIich[종류] + j]`, `MessageBoxWorker +0x22b · +0x25b · +0x2c8`), `uID = 칸 + 1` 은 **커널 `xxxLoadSomeStrings +0x4e`** 가 쓴다. 기본 단추 번호 = `(uType >> 8) & 0xf` (단추 수 이상이면 0, `+0xc0 ~ +0xcd`). **`MB_YESNO` → `IDYES 6` (DEFBUTTON2 면 `IDNO 7`)** — 표 C. `pdlg+0x20` 은 그 단추가 `WM_GETDLGCODE` 에 `0x10` 으로 답할 때 덮인다 (`InternalCreateDialog +0x9ba ~ +0x9c1` → `+0x1341 ~ +0x134f`). `gfDMREnable ≠ 0` 이면 상자 없이 같은 표의 기본 ID 를 곧장 돌려주는 길이 따로 있다 (`MessageBoxWorker +0xe8 ~ +0x116`).
- **(4) 비트표** `[정적]` + `[바이너리]`: `user32+0xda568 ~ +0xda580` = `gSharedInfo (+0xda350) + 0x218 ~ +0x230`. **보통 프로세스에선 csrss USERSRV 연결 답** (`_UserClientDllInitialize +0x2aa CsrClientConnectToServer(…, 3, 0x248 B, …)`) 의 `+8` 부터 0x240 B 를 `gSharedInfo` 로 통째 복사한 것 (`+0x30b ~ +0x37c`) 이고 — `ClientThreadSetup +0x120 NtUserProcessConnect` 뒤 복사 (`+0x134 ~ +0x1ad`) 는 `gfServerProcess ≠ 0` (csrss 같은 서버 프로세스) 의 첫 스레드에서만 (run85, 맨 위 "정정 (run157 해설 때 대조)") — 커널은 `InitMessageTables` 가 `.rdata` 목록 둘 (rva `0x374cb0` 43개 · `0x374c00` 77개) 로 만든 비트 배열을 세션 칸 `[+0x5468 ~ +0x5480]` 에 두었다가 `InitMapSharedSection` 이 옮긴다. 값: **`DefWindowMsgs` 한도 `0x33f` · `DefWindowSpecMsgs` 한도 `0x349`**, `0x87` · `0x111` · `0x110` 은 두 목록 어디에도 없음 → `RealDefWindowProcWorker` 가 syscall 없이 0. `0xf` · `0x10` · `0x112` · `0x86` 은 커널로 간다 (`NtUserMessageCall`).
- **(5) 단추** `[정적]`: `ButtonWndProcWorker` 의 `WM_GETDLGCODE` — `BS_DEFPUSHBUTTON (1)` → **`0x2010`** (`+0xe3 ~ +0xec JZ → +0x32b MOV EBX,0x10 → +0x12e BTS RBX,0xd`), `BS_PUSHBUTTON (0)` → `0x2020` (`+0x129`). 클릭은 `xxxButtonNotifyParent` 가 **`SendMessageW(부모, WM_COMMAND, MAKEWPARAM(word [단추 사본+0x140], 코드), 단추 hwnd)`** (보내기, `+0x32 MOV EDX,0x111 · +0x3a JMP SendMessageW`). `BN_CLICKED (0)` 을 주는 줄 = 조각 `FUN_1800aa27b +0x24 XOR EDX,EDX → +0x30 CALL` (= `ButtonWndProcWorker +0x9b82f` · call `+0x9b83b`). 어떤 입력이 그 조각으로 가는지는 `[공백]`.
- **(6) 끝 길 syscall 부작용** `[정적]`: `WM_CLOSE` 는 `DefWindowMsgs` 에 있어 커널 `xxxRealDefWindowProc +0x831 xxxDestroyWindow` 로 곧장 (대화상자는 `DefDlgProcWorker` 가 먼저 `PostMessageW(…IDCANCEL…)` 로 바꿈 — run148). `SC_CLOSE` = `xxxSysCommand +0x1ae` 가 `WM_CLOSE` 를 **보낸다**. `NtUserDestroyWindow` → `xxxDestroyWindow` → `_Phase2 +0x3a9 xxxDW_SendDestroyMessages` (`+0xba` 메시지 `0x272` · `+0xef` **`WM_DESTROY`** 보내기) → `xxxFreeWindow +0x31 _Phase2 +0x49` **`WM_NCDESTROY`** 보내기 — 둘 다 커널이 클라이언트로 되부른다. `NtUserSetWindowPos(…, 0x97)` 는 `xxxSetWindowPos +0x66 → xxxSetWindowPosAndBand +0x8b → xxxEndDeferWindowPosEx` (`+0x203 xxxSendChangedMsgs` 등) — 숨김 · 위치 바뀜 보내기가 생길 수 있음 (정확한 차례 `[추측]`). `NtUserSetWindowLongPtr(GWLP_USERDATA)` = `xxxSetWindowData +0x3b CMP EBX,-0x15 → +0xd4 / +0xdb`: **옛값 `[사본+0xd8]` 읽고 새값 씀, 반환 옛값**.
- **(7) 그 밖** `[정적]`: `g_systemCallFilterId` 는 run153 (sast/150 표 E) 이 닫음 — 이번에 본 새 쓰임 하나 (`NtUserSetWindowLongPtr +0x86 PsGetWin32KFilterSet` 이 5 면 `GWLP_USERDATA` 말고는 `LastError 0x57`). **IMM 훅 `[user32+0xd96f0]` = `imm32!ImmTranslateMessage`** (`_InitializeImmEntryTable +0x6e3 · +0x6f4 · +0x710`), `TranslateMessage` 는 `word [msg+0x10] == 0xe5` 일 때만 부름 (몸은 imm32 — 다른 프로젝트 몫). **`GetUserDefaultLCID`** (kernelbase rva `0x50ef0`) = `GetCurrentNlsCache()` 의 `[+0x10]` 노드 첫 dword (`0x1000` 이면 `0xc00` 으로), 없으면 시스템 노드 (`SetupSystemLocaleHashNode +0xf7 NtQueryDefaultLocale`) — 값은 NLS 캐시 · 기본 로케일 stub 이 정함, 캐시 안쪽 `[공백]`.
- **PE_emulator 쪽 한 줄씩** → 끝 "PE_emulator 쪽" 절.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백열다섯째 바퀴. plan **Q282**. **짐 지도 바퀴 아님.**
- 정적: **다시 analyze 한 모듈 없음.** user32 · win32kfull · kernelbase 를 `-readOnly` 로 export · refs, win32kbase 는 있던 `.asm` 글만 읽음. kernelbase 는 요청서 ⚠ ("sast/152 결과서가 생긴 뒤에만") 대로 처음엔 `objdump` 로만 보다가, `_workspace/sast/152_appinfo-runas_result.md` 가 생긴 뒤 (Ghidra 프로세스 없음 · `free -g` 확인) `GetUserDefaultLCID` 와 부르는 둘만 뽑았다 (결과서 머리).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| user32 | `CDB947D1B96B87977915DE04CF54279C1` | 같음 (run85 · run142 · run146 · run148 · run153) |
| win32kfull | `5CD57181BCFDC5AE8F4819BEB1196F091` | 같음 (run136 · run142 · run146 · run147 · run153) |
| win32kbase | `D22DFDA3D54B87E6BD0567A57877904C1` | 같음 (run86 · run146 · run147 · run153) |
| KernelBase | `72CCE506944DE0267C172AD64287F9BD1` | 같음 (run14 … run156) |

- 버전 대조 (결과서): run148 · sast/146 이 적은 줄 — `xxxCreateWindowEx +0x1b9 JS` · `+0x1bf MOV EAX,0x320` · `+0x8e6 → +0x8f1 MapClientNeuterToClientPfn` · `+0xa3a MOV R8D,0x208` · `InternalCreateDialog +0x6fa` · `DefDlgProcWorker` 조각 `+0x9799c` · `RealDefWindowProcWorker +0x46 / +0x4e / +0xcd` · `ClientThreadSetup +0x228` — **일치**. flow-writer 도 `_calls.tsv` 와 `.asm` 으로 같은 줄을 다시 봤다 (아래 "방법").
- PDB 에 `tagWND` · `tagMSG` · `tagDIALOG` 형식이 없다 (run146 확인) — 칸 이름은 `[추측: 이름]`, 오프셋 · 읽는 줄은 `[정적]`. 창은 두 벌: 커널 `pwnd` (`[pwnd+0x28]` = 사본 커널 주소, `[pwnd+0x30]` = 같은 사본의 유저 눈 주소) 와 user32 가 읽는 **사본**. 이 문서의 "사본 `+0x…`" 는 run142 · run146 · run148 과 같은 칸, 커널 쪽은 `pwnd+0x…`.

## 목적

- PE_emulator 쪽 남은 정적 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): run146 · run148 이 user32 쪽 끝 길을 닫으며 넘긴 **win32k 몫**. 요청서 `_workspace/sast/151_dialog-win32k.md`:
  1. (급함) 자식 연결 순서 · ANSI 비트 (`LinkWindow` · `사본+0x12` 비트 3 · `DefDlgProcA`).
  2. (급함) `NtUserPeekMessage` 진짜 첫 메시지들 · `wRemove` · 반환.
  3. (급함) `SoftModalMessageBox` · `MB_AddPushButtons` 기본 단추 ID (`MB_YESNO` 에서 `pdlg+0x20`), `MessageBox` 가 같은 고리를 타나 · 반환을 바꾸나.
  4. 비트표 `user32+0xda568 ~ +0xda580` 을 채우는 곳 · 값.
  5. `ButtonWndProcWorker` · `xxxButtonNotifyParent` (`DLGC_DEFPUSHBUTTON` · `WM_COMMAND`).
  6. `WM_CLOSE` / `SC_CLOSE` · `xxxDestroyWindow` 되부름 · `SetWindowPos(0x97)` · `NtUserMessageCall` · `GWLP_USERDATA`.
  7. `GetUserDefaultLCID` · `g_systemCallFilterId` · IMM 훅 몸.
- 쓰는 곳: PE_emulator stub (`NtUserPeekMessage` · `NtUserCreateWindowEx` · `NtUserProcessConnect` (→ 보통 프로세스는 csrss USERSRV 답 쪽 — 맨 위 "정정") · `MessageBox` "기본 단추 누름"), run148 "stub 이 채워야 하는 최소 목록" 에 이어 붙임.

## 방법

- 정적 sast/151 (결과서 `_workspace/sast/151_dialog-win32k_result.md`, **분석성공**, 스크립트 문제 없음 — `export` · `refs` 모두 `SCRIPT ERROR` 없음, 로그 `ms/ghidra/win32kfull.export.s151{a..j}.stdout` · `user32.export.s151a.stdout` · `kernelbase.export.s151a.stdout` · `win32kfull.refs.s151_xxxLoadSomeStrings.stdout`):
  - win32kfull (depth 0) 새로 48개: `LinkWindow` · `UnlinkWindow` · `PWInsertAfter` · `MapClientNeuterToClientPfn` · `NtUserPeekMessage` · `xxxInternalGetMessage` · `xxxRealInternalGetMessage` · `xxxReadPostMessage` · `xxxDoPaint` · `xxxInternalDoPaint` · `DoTimer` · `xxxSendPostedMessage` · `xxxReceiveMessages` · `xxxScanSysQueue` · `NtUserInitializeClientPfnArrays` · `InitializeClientPfnArrays` · `NtUserProcessConnect` · `InitMapSharedSection` · `InitMessageTables` · `InitFunctionTables` · `xxxLoadSomeStrings` · `NtUserSetMsgBox` · `NtUserDestroyWindow` · `xxxDestroyWindow` 과 `_Prologue` · `_Phase1 ~ 3` · `xxxDW_DestroyOwnedWindows` · `xxxDW_SendDestroyMessages` · `xxxFreeWindow` 과 `_Phase1 · 2` · `NtUserSetWindowPos` · `xxxSetWindowPos` · `xxxSetWindowPosAndBand` · `xxxEndDeferWindowPosEx` · `xxxSendChangedMsgs` · `NtUserMessageCall` · `xxxDefWindowProc` · `xxxRealDefWindowProc` · `xxxWrapRealDefWindowProc` · `xxxSysCommand` · `xxxClientCallDefWindowProc` · `NtUserSetWindowLongPtr` · `xxxSetWindowLongPtr` · `xxxSetWindowData` · `xxxSendMessageEx`.
  - user32 새로 11개: `ButtonWndProcWorker` · `ButtonWndProcW` · `ButtonWndProcA` · `xxxButtonNotifyParent` · `IsPushButton` · `xxxPressButton` · `xxxRemoveDefaultButton` · `_InitializeImmEntryTable` · `User32InitializeImmEntryTable` · `xxxBNPaint` · `xxxButtonDrawNewState` (`RealDefWindowProcWorker` · `DefDlgProcWorker` 는 같은 이름으로 다시 씀 — 내용 같음). 있던 `.asm` 읽기만: `MessageBoxW` · `MessageBoxTimeoutW` · `MessageBoxWorker` · `SoftModalMessageBox` · `MB_AddPushButtons` · `MB_DlgProc` · `MB_GetMaxButtonSize` · `ClientThreadSetup` · `InternalCreateDialog` · `TranslateMessage` · `xxxCheckDefPushButton`.
  - kernelbase 새로 3개: `GetUserDefaultLCID` · `GetCurrentNlsCache` · `SetupSystemLocaleHashNode`.
  - 표 값 (`[바이너리]`, 결과서가 파이썬으로 `ms/bin` PE 파싱): win32kfull 메시지 목록 (rva `0x374cb0` · `0x374c00`) · user32 `SEBbuttons` (`0xbba90`) · `mpTypeIich` (`0xbbaec`) · `mpTypeCcmd` (`0xbbaf4`) · 이름 글 `"ImmTranslateMessage"` (`0xb9f58`).
- 같은 시간에 decompiler 가 sast/152 (windows.storage · shell32 · appinfo · kernelbase) · sast/153 (ntoskrnl · ntdll) · sast/154 (rpcrt4 · winnsi) 를 돌았다. explainer 는 run156 해설서를 쓰는 중 — 이 문서는 그 파일을 안 건드림.
- flow-writer 가 다시 본 것:
  - `_calls.tsv` (win32kfull · user32 · kernelbase) 에서 결과서의 call 줄 — 아래 호출 지점 표 전부. `.asm` 으로 갈래 줄: `xxxRealInternalGetMessage +0x225 ~ +0x237 · +0x4be ~ +0x4cd · +0x8e0 ~ +0x8e8 · +0xb28 ~ +0xb30 · +0xbb1 ~ +0xbbb · +0xc33 ~ +0xc41 · +0xca8 ~ +0xcb3 · +0x162e ~ +0x1651`; `NtUserPeekMessage +0x2d ~ +0x39 · +0xd7 ~ +0x104 · +0x120 ~ +0x159 · +0x189 ~ +0x18e`; `xxxCreateWindowEx +0x1b6 ~ +0x1d2 · +0x1f7 · +0x8e6 ~ +0x8fd · +0xa04 ~ +0xa49 · +0xb51 · +0xbc4 ~ +0xbcd · +0xce2 · +0x1709 ~ +0x1720 · +0x2014 ~ +0x2051 · +0x21e4 ~ +0x21ee`; `LinkWindow +0x0 ~ +0x97 · +0x1f4 ~ +0x3b4 · +0x46c`; `PWInsertAfter +0x1c ~ +0x3a · +0xfb`; `MapClientNeuterToClientPfn` 전부 (`+0x0 ~ +0xdf`); `InitializeClientPfnArrays +0x36 · +0xfe · +0x1f2 · +0x2b2 · +0x353 · +0x3ad · +0x3b4`; `InitMessageTables +0x7c · +0x8d · +0xba · +0xe4 · +0xf5 · +0x122 · +0x626 ~ +0x646 · +0x660 ~ +0x680`; `xxxLoadSomeStrings +0x34 ~ +0x55 · +0x7b`; `xxxDW_SendDestroyMessages +0xa2 ~ +0xbf · +0xe1 ~ +0xef`; `xxxSetWindowData +0x3b · +0x3e · +0xd0 ~ +0xdb`; `xxxSetWindowLongPtr +0x41 ~ +0x4a`; `NtUserSetWindowPos +0x8f · +0x96`.
  - user32 `.asm`: `MessageBoxWorker +0x7a ~ +0xcf · +0xe4 ~ +0x116 · +0x22b · +0x25b · +0x2c8 · +0x331 ~ +0x346`; `MessageBoxTimeoutW +0x18f ~ +0x19e`; `SoftModalMessageBox +0x5b6 ~ +0x5be`; `MB_DlgProc +0x171 ~ +0x1ad · +0x275 ~ +0x2a5 · +0x303 ~ +0x324`; `InternalCreateDialog +0x9b2 ~ +0x9cb` · 조각 `FUN_18001fbc1 +0x306 ~ +0x327` (= `+0x1337 ~ +0x1358`); `ButtonWndProcWorker +0xe3 ~ +0xec · +0x129 · +0x12e · +0x32b` · 조각 `FUN_1800aa27b +0x0 ~ +0x36`; `xxxButtonNotifyParent +0x0 · +0x32 · +0x3a`; `_InitializeImmEntryTable +0x6e3 · +0x6f4 · +0x710`; `TranslateMessage +0x12 · +0x6a · +0x71`; `ClientThreadSetup +0x120 · +0x134 · +0x1ad`. kernelbase `GetUserDefaultLCID +0x0 ~ +0x39`.
  - **표 값 다시 읽음** (스크래치 파이썬 `pefile`, `ms/bin` 의 user32.dll · win32kfull.sys): `SEBbuttons` 23 칸 · `mpTypeIich` 8 · `mpTypeCcmd` 8 · `0xb9f58` 글 `"ImmTranslateMessage"` — 결과서와 같음. 메시지 목록 둘: 43 개 (최대 `0x33f`, 뒤 0) · 77 개 (최대 `0x349`, 뒤 0), 둘 다 `0x87` · `0x111` · `0x110` 없음, `0xf` · `0x10` · `0x112` · `0x86` 은 첫째에, `0x6` 은 둘째에 — 결과서와 같음.
  - `pdb_aliases.py --where win32kfull.sys xxxPeekMessage` → "PDB 공개 심볼에 없음"; `_index.tsv` 에도 `PeekMessage` 가 든 이름은 `NtUserPeekMessage` (+ `$filt$0` · `$filt$1`) 뿐.
  - `../PE_emulator/ext/syscall/syscall_layer_cpp/src/gui/` 파일 목록 (24개) · `nt_user_peek_message.cpp` (53줄) 의 멈춤 줄 (`:29` · `:50 gui_peek_message_none`) · `nt_user_create_window_ex.cpp :45 kDefDlgProcWRva` · `:162` 를 줄 번호만 훑음 (읽기만, 고치지 않음).

## 결과

비유:

- **대화상자는 "줄 서는 학급" 이다.** 새로 온 학생 (컨트롤) 은 늘 **줄 맨 뒤** 에 선다 (`HWND_BOTTOM`). 그래서 출석부 순서 (템플릿) 와 줄 순서가 같다. 학생마다 "내 앞 사람" (`+0x50`) 과 "내 뒤 사람" (`+0x48`) 을 기억한다.
- **우편함 (`NtUserPeekMessage`) 에는 꺼내는 순서가 정해져 있다.** 전화 (보낸 메시지) 는 받자마자 그 자리에서 처리하고, 그다음 편지 (게시) → 초인종 (입력) → "칠해 주세요" 쪽지 (`WM_PAINT`) → 알람 (`WM_TIMER`) 순. 막 문을 연 교실엔 편지도 초인종도 없으니 첫 쪽지는 "칠해 주세요" 다.
- **메시지 상자의 단추 번호는 명찰 대장 (`MBStrings`) 에서 읽는다.** 대장은 커널이 써 준다. "예 / 아니오" 상자의 기본 단추는 "예 (6)" 이지 "확인 (1)" 이 아니다 `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/{win32kfull,user32,kernelbase}/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `win32kfull!NtUserPeekMessage` (rva `0x13d680`) | **`+0xff`** | **`+0x104`** | **`xxxInternalGetMessage`** — 앞 `+0xde TEST EAX,0xe300fffc · +0xe3 JNZ → +0x189` | [정적] |
| 〃 | `+0x18e` | `+0x193` | `UserSetLastError(0x3ec)` — 깃발 거절 | [정적] |
| 〃 | `+0x77` | `+0x7e` | `KeWaitForSingleObject` — 여섯째 인자 비트 0 (`+0x34 TEST BL,1 · +0x37 JZ`) | [정적] |
| `win32kfull!xxxInternalGetMessage` | `+0x78` | `+0x7d` | **`xxxRealInternalGetMessage`** (rva `0x13da9c`) — 몸 | [정적] |
| `win32kfull!xxxRealInternalGetMessage` | `+0x17a` | `+0x17f` | `CalcWakeMask(최소, 최대, wRemove >> 16)` | [정적] |
| 〃 | `+0x4cd` · `+0xb30` | `+0x4d2` · `+0xb35` | `xxxReceiveMessages` — P1 보낸 메시지 (`QS_SENDMESSAGE 0x40`, `+0xb28 TEST CL,0x40`) | [정적] |
| 〃 | `+0x8a3` | `+0x8a8` | `xxxScanSysQueue` — P2 (입력 큐 주인 스레드일 때) | [정적] |
| 〃 | **`+0x90e`** | **`+0x913`** | **`xxxReadPostMessage`** — P3 게시 (`+0x8e0 TEST AL,8 · +0x8e2 JZ`) | [정적] |
| 〃 | **`+0xa2e`** | **`+0xa33`** | **`xxxScanSysQueue`** — P4 입력 | [정적] |
| 〃 | **`+0xbbb`** | **`+0xbc0`** | **`xxxDoPaint`** — P5 `WM_PAINT` (`+0xbb1 TEST AL,0x20 · +0xbb3 JZ`) | [정적] |
| 〃 | **`+0xcb3`** | **`+0xcb8`** | **`DoTimer`** — P6 `WM_TIMER` (`+0xca8 TEST AL,0x10 · +0xcaa JZ`) | [정적] |
| 〃 | `+0xc41` · `+0x1649` | `+0xc46` · `+0x164e` | `zzzWakeInputIdle` — 빈 반환 길 (`+0x1649` 앞 `+0x163c TEST [RSP+0x2b0],2 · +0x1644 JNZ` = `PM_NOYIELD` 면 건너뜀) | [정적] |
| 〃 | `+0xc49` · `+0x1651` | `+0xc4e` · `+0x1656` | `xxxReceiveMessages` — 빈 반환 길 | [정적] |
| 〃 | `+0xaee` · `+0xdbb` · `+0xe25` | `+0xaf3` · `+0xdc0` · `+0xe2a` | `xxxSleepThread2` · `xxxSleepThreadWithPwnd` · `xxxSleepThread2` — `GetMessage` 판의 잠 | [정적] |
| `win32kfull!xxxCreateWindowEx` | `+0x8f1` | `+0x8f6` | `MapClientNeuterToClientPfn(클래스, 0, word [RSP+0xa4])` → `+0x8fd [사본+0x78]` | [정적] |
| 〃 | `+0xa16` · `+0xa49` | `+0xa1b` · `+0xa4e` | `SetOrClrWF(…, 0x320 또는 0, …)` · `SetOrClrWF(…, 0x208, …)` | [정적] |
| 〃 | `+0x203b` | `+0x2040` | `_GetDesktopWindow` — `WS_CHILD` 길 (`+0x2043 JNZ → +0x2bd9`, 둘 다 `+0x2049` 로) | [정적] |
| 〃 | `+0x167b` | `+0x1680` | `PWInsertAfter(hwndInsertAfter)` | [정적] |
| 〃 | `+0x16af` · `+0x16c3` · `+0x1701` | `+0x16b4` · `+0x16c8` · `+0x1706` | `ValidateNewParent` · `UnlinkWindow` · `GetTopMostInsertAfter` (자식이면 건너뜀) | [정적] |
| 〃 | **`+0x1720`** | **`+0x1725`** | **`LinkWindow(새 창, 1, 부모)`** | [정적] |
| `win32kfull!LinkWindow` (rva `0xe4c0`) | `+0x89` | `+0x8e` | `_GetDesktopWindow` — 부모가 바탕 창인지 (아니면 띠 비교 없음) | [정적] |
| 〃 | `+0x263` · `+0x27c` · `+0x296` · `+0x2b6` · `+0x2d4` | `+0x268` · … | `GetBandOrdinal` — 바탕 창 자식일 때만 | [정적] |
| 〃 | `+0x338` · `+0x36e` · `+0x398` | `+0x33f` · `+0x375` · `+0x39f` | `HMAssignmentLock` — 이전 · 다음 칸 쓰기 (사본 `+0x50` · `+0x48` 는 그 사이 `+0x330` · `+0x35c` · `+0x390` · `+0x3b0`) | [정적] |
| `win32kfull!NtUserInitializeClientPfnArrays` | `+0xb0` | `+0xb5` | `InitializeClientPfnArrays` — `gpsi+0x188` · `+0x248` · `+0x308` · `+0x1d50` · `[세션+0x5490]` | [정적] |
| `win32kfull!NtUserProcessConnect` | `+0x260` | `+0x265` | `InitMapSharedSection` — `USERCONNECT` 채움 (`+0x244 ~ +0x3c7`, 세션 칸 `[+0x5468 ~ +0x5480]` 옮김 `+0x2fe ~ +0x38c` — 결과서) | [정적] |
| 〃 | `+0x297` · `+0x5a3` | `+0x29c` · `+0x5a8` | `memcpy` — `USERCONNECT` 를 사용자 버퍼로 | [정적] |
| `win32kfull!InitMessageTables` (rva `0x407be0`) | `+0xae` · `+0x116` | `+0xb5` · `+0x11d` | `SharedAlloc` — 비트 배열 둘 (`[세션+0x5470]` · `[+0x5480]`) | [정적] |
| `win32kfull!xxxLoadSomeStrings` | `+0x81` (첫) | `+0x86` | `xxxClientLoadStringW` — 단추 이름 (`+0x34 [gpsi+칸*0x28+0x3c8] = uStr` · **`+0x4e [+0x3c4] = uID`**) | [정적] |
| `win32kfull!xxxRealDefWindowProc` | **`+0x831`** | **`+0x836`** | **`xxxDestroyWindow`** — `WM_CLOSE` | [정적] |
| 〃 | `+0x14bc` | `+0x14c1` | `xxxSysCommand` — `WM_SYSCOMMAND` | [정적] |
| `win32kfull!xxxSysCommand` | `+0x1ae` | `+0x1b3` | `xxxSendTransformableMessageTimeout(pwnd, WM_CLOSE …)` — `SC_CLOSE` | [정적] |
| `win32kfull!xxxDestroyWindow` | `+0x3f` · `+0x90` · `+0xa2` · `+0xb1` · `+0xc0` · `+0xcf` | `+0x44` · `+0x95` · `+0xa7` · `+0xb6` · `+0xc5` · `+0xd4` | `_Prologue` · `_Phase1` · `xxxDW_DestroyOwnedWindows` · `_Phase2` · `_Phase3` · `xxxFreeWindow` | [정적] |
| `win32kfull!xxxDestroyWindow_Phase2` | `+0x1b3` · `+0x1d0` · `+0x2cc` | `+0x1b8` · `+0x1d5` · `+0x2d1` | `xxxEnableWindow` (소유자) · `xxxActivateWindowWithOptions` · `xxxWindowEvent` | [정적] |
| 〃 | **`+0x3a9`** | **`+0x3ae`** | **`xxxDW_SendDestroyMessages`** | [정적] |
| `win32kfull!xxxDW_SendDestroyMessages` | `+0xba` · **`+0xef`** | `+0xbf` · **`+0xf4`** | `xxxSendTransformableMessageTimeout` — 메시지 `0x272` (`+0xa8 MOV EDX,0x272`) · **`WM_DESTROY (2)`** (`+0xeb LEA EDX,[R9+2]`) | [정적] |
| `win32kfull!xxxFreeWindow` | `+0x1e` · `+0x26` · `+0x31` · `+0x3f` | `+0x23` · `+0x2b` · `+0x36` · `+0x44` | `_Phase1` · `xxxFW_DestroyAllChildren` · `_Phase2` · `FreeWindow_Phase3` | [정적] |
| `win32kfull!xxxFreeWindow_Phase2` | **`+0x49`** | **`+0x4e`** | **`xxxSendTransformableMessageTimeout`** — **`WM_NCDESTROY (0x82)`** (`+0x3c MOV EDX,0x82`) | [정적] |
| `win32kfull!NtUserSetWindowPos` | `+0x14b` | `+0x150` | `xxxSetWindowPos` — 앞 `+0x8f TEST R15D,0xfffd9800 · +0x96 JNZ` | [정적] |
| `win32kfull!xxxSetWindowPos` | `+0x66` | `+0x6b` | `xxxSetWindowPosAndBand` | [정적] |
| `win32kfull!xxxSetWindowPosAndBand` | `+0x39` · `+0x76` · `+0x8b` | `+0x3e` · `+0x7b` · `+0x90` | `InternalBeginDeferWindowPos` · `_DeferWindowPos` · `xxxEndDeferWindowPosEx` | [정적] |
| `win32kfull!xxxEndDeferWindowPosEx` | `+0x15b` · `+0x203` · `+0x5a7` · `+0x5c1` · `+0x68c` · `+0x6a0` | `+0x160` · `+0x208` · … · `+0x691` · `+0x6a5` | `xxxCalcValidRects` · `xxxSendChangedMsgs` · `PostShellHookMessagesEx` · `xxxCallHook` · `xxxSwpActivate` · `xxxDoSyncPaint` | [정적] |
| `win32kfull!NtUserSetWindowLongPtr` | `+0x86` | `+0x8d` | `PsGetWin32KFilterSet` (5 면 `GWLP_USERDATA` 말고 `+0x136 UserSetLastError(0x57)`) | [정적] |
| 〃 | `+0xae` | `+0xb3` | `xxxSetWindowLongPtr` | [정적] |
| `win32kfull!xxxSetWindowLongPtr` | `+0x47c` | `+0x481` | `xxxSetWindowData` — 음수 인덱스 (`+0x4a JS`) | [정적] |
| `win32kfull!NtUserSetMsgBox` | `+0x8a` | `+0x8f` | `SetOrClrWF(1, pwnd, 0x20, 0)` — 사본 `+0x10` 비트 5 | [정적] |
| `user32!ClientThreadSetup` | **`+0x120`** | **`+0x127`** | **`NtUserProcessConnect`** — 뒤 `+0x134 LEA RAX,[gSharedInfo]` ~ `+0x1ad` 0x240 B 복사. **`gfServerProcess ≠ 0` (`+0xdd MOV EAX,[gfServerProcess]` · `+0xe3 TEST` · `+0xe5 JZ → +0x258` 끝) + 첫 스레드 (`+0xeb TEST EDI` · `+0xed JZ`, EDI = 옛 `gfFirstThread`) 일 때만** (run157 해설 때 대조) | [정적] |
| 〃 | `+0x228` | `+0x22f` | `NtUserInitializeClientPfnArrays` — 비트표와 무관. 같은 조건 (`+0x1b9 ~ +0x1c1` `gfServerProcess` · `+0x1c7 ~ +0x1c9` 첫 스레드) | [정적] |
| `user32!_UserClientDllInitialize` | `+0x2aa` | `+0x2b1` | `CsrClientConnectToServer(…, 3, [RSP+0x60] 0x248 B, 0x248, &gfServerProcess)` → `+0x2be CMP [gfServerProcess],0` · `+0x2ca JNZ` 안 탐 → (PEB `+0x3` 비트 `0x2` 꺼짐이면) **`+0x30b ~ +0x37c` 답 `[RSP+0x68]` (= 답 `+8`) 의 0x240 B → `gSharedInfo`** · `+0x380 ~ +0x387` `gpsi` — **보통 프로세스의 비트표 출처** (run85 · run157 해설 때 대조) | [정적] |
| `user32!MessageBoxTimeoutW` | `+0x18a` | `+0x18f` | `MessageBoxWorker` — `+0x18f MOV EBX,EAX … +0x19e MOV EAX,EBX` 그대로 | [정적] |
| `user32!MessageBoxWorker` | `+0x349` | `+0x34e` | `SoftModalMessageBox` | [정적] |
| `user32!SoftModalMessageBox` | `+0x511` · **`+0x5b1`** | `+0x516` · **`+0x5b6`** | `MB_AddPushButtons` · **`InternalDialogBox`** → `DialogBox2` (run148 고리) | [정적] |
| `user32!InternalCreateDialog` | `+0x9ba` | `+0x9bf` | `SendMessageW(컨트롤, WM_GETDLGCODE 0x87)` — `+0x9bf TEST AL,0x10 · +0x9c1 JNZ → +0x1341` (`pdlg+0x20` 덮기) | [정적] |
| `user32!MB_DlgProc` | `+0xec` | `+0xf3` | `NtUserSetMsgBox` (`0x1573`, 반환 안 봄) | [정적] |
| 〃 | `+0x184` · `+0x310` · `+0x197` | `+0x189` · `+0x315` · `+0x19e` | `GetWindow(5 = GW_CHILD)` · `GetWindow(2 = GW_HWNDNEXT)` ×`[RDI+0x74]` · `NtUserSetFocus` — 기본 단추에 포커스 | [정적] |
| 〃 | `+0x324` · `+0x344` | `+0x32b` · `+0x34b` | `NtUserGetSystemMenu` · `NtUserDeleteMenu` — `[RDI+0x78] == 0` (`+0x1a3 CMP · +0x1a7 JZ → +0x31f`) | [정적] |
| 〃 | `+0x5eae4` · `+0x5eaf0` | `+0x5eae9` · `+0x5eaf5` | `EndDialog` · `GetDlgItem` — `WM_COMMAND` (run142) | [정적] |
| `user32!ButtonWndProcWorker` | 조각 `FUN_1800aa27b +0x30` (= `+0x9b83b`) | 조각 `+0x35` (= `+0x9b840`) | `xxxButtonNotifyParent(단추, 0 = BN_CLICKED)` (`+0x24 XOR EDX,EDX`) | [정적] |
| `user32!xxxButtonNotifyParent` | `+0x3a` | (꼬리) | `JMP SendMessageW(부모, 0x111, ID \| 코드 << 16, 단추 hwnd)` | [정적] |
| `user32!_InitializeImmEntryTable` | `+0x6f4` | `+0x6fb` | `GetProcAddress(imm32, "ImmTranslateMessage")` → `+0x710 [0xd96f0]` | [정적] |
| `user32!TranslateMessage` | `+0x71` | `+0x76` | ⇢ `[user32+0xd96f0]` (CFG) — `+0x12 CMP word [msg+0x10],0xe5` | [정적] |
| `kernelbase!GetUserDefaultLCID` (rva `0x50ef0`) | `+0x4` · `+0x39` | `+0x9` · `+0x3e` | `GetCurrentNlsCache` · `SetupSystemLocaleHashNode(0)` | [정적] |
| `kernelbase!SetupSystemLocaleHashNode` | `+0x87` · `+0xa1` · `+0xf7` | `+0x8c` · `+0xa6` · `+0xfe` | `MakeLocHashNode` · `MakeNamedLocaleHashNode` · **`NtQueryDefaultLocale`** | [정적] |
| `kernelbase!GetCurrentNlsCache` | `+0x34e` · `+0x6a0` | `+0x355` · `+0x6a7` | `CsrClientCallServer` · `RtlOpenCurrentUser` (캐시가 낡았을 때, 첫 자리만) | [정적] |

- `_calls.tsv` 의 `alias:<이름>+1` (예 `xxxReceiveMessages` · `xxxScanSysQueue` · `MB_AddPushButtons`) 은 PDB 꾸민 이름 하나가 같은 주소에 더 있다는 표시다 — `pdb_aliases.py --lookup win32kfull.sys 0x40af4` · `0x2624c4` 는 "별칭 무리 아님". 이름은 그대로 썼다.

### 1. 자식 연결 — `xxxCreateWindowEx` → `PWInsertAfter` → `LinkWindow` (결과서 표 A) [정적]

| 단계 | 하는 일 | 줄 |
|---|---|---|
| A1 | `hwndInsertAfter` 칸 `[RSP+0xe8]`: 처음 부모 → 보통 길에서 **0 (`HWND_TOP`)** (`+0xb51`), CBT 훅이 켜졌으면 훅 값 | `xxxCreateWindowEx +0xb51 · +0x20be xxxCallHook` |
| A2 | **`WS_CHILD` (사본 `+0x1f & 0xc0 == 0x40`) 이면 `1` (`HWND_BOTTOM`)** — 부모가 바탕 창이든 아니든 `+0x2049` 로 | `+0xbc4 CMP CL,0x40 · +0xbc7 JZ → +0x2014 · +0x203b _GetDesktopWindow · +0x2043 JNZ → +0x2bd9 · +0x2049 MOV [RSP+0xe8],R15` (R15 = 1, `+0x1f7`) |
| A3 | `PWInsertAfter` — 0 · 1 · −1 · −2 면 그대로 | `+0x167b` · `PWInsertAfter +0x1c · +0x26 · +0x30 · +0x3a JZ → +0xfb MOV RAX,RDI` |
| A4 | `ValidateNewParent` · `UnlinkWindow` (새 창은 아직 안 이어져 사실상 빈 일) · 자식이면 `GetTopMostInsertAfter` 건너뜀 · `CMP RBX,2` | `+0x16af · +0x16c3 · +0x1709 ~ +0x1711` |
| A5 | **`LinkWindow(RCX = 새 창, RDX = 1, R8 = 부모)`** | `+0x1720` |
| A6 | 부모 첫 자식 `[R8+0x70]` 이 이미 새 창이면 끝 · `RDX` 가 진짜 창이면 다른 길 (`+0x3a CMP RAX,-5 · +0x3e JBE → +0x46c`) · 부모가 바탕 창이면 띠 번호 비교, 아니면 `R9D = 0` | `LinkWindow +0x2c ~ +0x3e · +0x86 ~ +0x97 · +0x9d` |
| A7 | **`RDX == 1`**: `RBX = [부모+0x70]` (첫 자식, 없거나 첫 자식 사본 `+0x14` 비트 5 면 A9) → 다음 형제 `[RBX+0x58]` 를 따라 **다음이 없거나 다음의 사본 `+0x14` 비트 5 가 켜질 때까지** | `+0x213 CMP RBX,R8(1) · +0x218 ~ +0x22d · +0x233 ~ +0x24e` |
| A8 | `RBX` 뒤에 끼움: (뒤 형제가 있으면 그 이전 칸을 새 창으로) · **새 창 이전 = RBX · 사본 `+0x50` = RBX 사본** · **RBX 다음 = 새 창 · 사본 `+0x48` = 새 창 사본** | `+0x2e1 ~ +0x3b4` (`+0x330` · `+0x35c` · `+0x390` · `+0x3b0`) |
| A9 | `RDX == 0` 또는 첫 자식 없음: 첫 자식이 없으면 새 창이 첫 자식 (`부모+0x70` · 부모 사본 `+0x38`), 있으면 그 앞에 | `+0xa9 ~ +0xe0 · +0x3b9 ~ +0x41b` |
| A10 | 끝: `BTS [새 창+0x17c],0x18` · 부모 사슬 확인 · (DWM 합성이면 `DwmAsyncChildLink`) | `+0xe0 ~ +0x12f · +0x16c ~ +0x1f4` |

- **결론 (사본 칸)**: `+0x30` 부모 사본 (`xxxCreateWindowEx +0xce2` 가 `LinkWindow` 앞에 읽는 칸) · `+0x38` 첫 자식 · `+0x48` 다음 형제 · **`+0x50` 이전 형제**. 템플릿 차례로 만든 컨트롤은 `GetDlgItem` 사슬 (`+0x38 → +0x48`, run148) 과 `GetWindow(GW_CHILD / GW_HWNDNEXT)` 차례가 템플릿 차례와 같다.
- **`MessageBox`** 는 `MB_AddPushButtons` 가 템플릿에 단추를 먼저 쓰므로 (`SoftModalMessageBox +0x511`) 단추가 앞이다 — `MB_DlgProc` 이 `GetWindow(GW_CHILD)` (`+0x184`) 에서 `[RDI+0x74]` (기본 단추 번호) 번 `GW_HWNDNEXT` (`+0x310`) 로 가 `NtUserSetFocus` (`+0x197`) 하는 것과 맞는다. 아이콘 · 글 항목의 자리는 `[공백]`.
- 최상위 창 (`HWND_TOP`) 은 바탕 창 자식 맨 위에 — 띠 순서 · `GetTopMostInsertAfter` 갈래는 안 따라감 `[공백]`.

### 2. ANSI · 클라이언트 프로시저 — `xxxCreateWindowEx` · `MapClientNeuterToClientPfn` (결과서 표 A') [정적]

| 단계 | 하는 일 | 줄 |
|---|---|---|
| B1 | `dwExStyle` 부호 비트 (= `0x80000000`) | `+0x1b6 TEST R15D,R15D · +0x1b9 JS → +0x21e4` |
| B2 | 켜졌으면 저장 스타일 사본 `[RSP+0x50]` · `[RSP+0x90]` 에서 비트 31 지움 (원래 값은 `[RSP+0x8c]`) | `+0x21e4 BTR R12D,0x1f · +0x21e9 · +0x21ee` |
| B3 | 깃발 **`0x320` (켜짐) / 0** → `[RSP+0xa4]` · `[RSP+0x60]` | `+0x1bf MOV EAX,0x320 · +0x1c4 · +0x1c7 CMOVNS AX,SI · +0x1cb · +0x1d2` |
| B4 | `MapClientNeuterToClientPfn(클래스, 0, B3)` — 클래스 fnid `word [[클래스+8]+4]` 가 `0x29a` 또는 `0x2a1 ~ 0x2aa` 일 때만. B3 ≠ 0 이면 클래스 프로시저가 **W 표** (`gpsi + fnid*8 − 0x1288`) 칸과 같을 때 **A 표** (`− 0x1348`) 칸을, B3 = 0 이면 반대 | `+0x8e6 · +0x8f1` · `MapClientNeuterToClientPfn +0x20 ~ +0x3b · +0x4c ~ +0x97 · +0xa5 ~ +0xdf` |
| B5 | 반환을 **`사본+0x78`** 에 (대화상자 fnid `0x2a4` → A = `gpsi+0x1d8`, W = `gpsi+0x298`) | `+0x8f6 · +0x8fd MOV [RAX+0x78],RCX` |
| B6 | 서버 쪽 프로시저 클래스면 `사본+0x12` 비트 2 (`0x204`). `SetOrClrWF` 깃발 = `(바이트 << 8) \| 마스크` (`0x204` → `+0x12` 비트 2 · `0x208` → 비트 3 · `0x320` → `+0x13` 비트 5) | `+0x91c ~ +0x92b` |
| B7 | `SetOrClrWF(1, 새 창, B3, 1)` — B3 = `0x320` 이면 **`사본+0x13` 비트 5** `[추측: 이름 = 만든이 ANSI]` | `+0xa04 ~ +0xa16` |
| B8 | **클래스 ANSI 깃발 `[[클래스+8]+6] & 2` 면 `사본+0x12` 비트 3** | `+0xa23 ~ +0xa34 JZ · +0xa3a MOV R8D,0x208 · +0xa49` |
| B9 | 클래스가 ANSI 아니면: 만든이 A 이고 원자 `word [RSP+0x5c]` 가 `gpsi+0x364 · +0x366 · +0x368 · +0x36a · +0x36e · +0x370 · +0x372 · +0x382 · +0x388` 중 하나면 B8 로 | `+0x1cbb ~ +0x1de8` (결과서 — flow-writer 는 이 범위를 안 다시 봄) |

- 배열 쪽 `[정적]`: `NtUserInitializeClientPfnArrays +0xb0 → InitializeClientPfnArrays` 가 첫 배열 (0xc0 B) 을 **`gpsi+0x188` (A 표)** (`+0xfe ADD RCX,0x188`), 둘째를 **`gpsi+0x248` (W 표)** (`+0x1f2`), 셋째 (0x58 B) 를 `gpsi+0x308` (`+0x2b2`) · `gpsi+0x1d50` (`+0x353`), `hmodUser` 를 `[세션+0x5490]` (`+0x3ad`) — 세션 칸 `+0x11a40` 이 0 일 때 한 번만 (`+0x36` · `+0x3b4`). user32 가 이 syscall 을 부르는 곳은 `ClientThreadSetup +0x228` 하나이고 **`gfServerProcess ≠ 0` 첫 스레드일 때만** (run85 · run157 해설 때 대조) — 보통 프로세스는 `gpsi` 가 가리키는 세션 공유 메모리에 서버 프로세스가 이미 채운 값을 읽는 것으로 보인다 `[추측: 세션당 한 번 · 공유]`. A / W 이름은 인자 차례로 `[추측]` 이고, `MapClientNeuterToClientPfn` 의 쓰임 (`−0x1348 = +0x188` 기준 · `−0x1288 = +0x248` 기준, fnid `0x29a` 가 0 번 칸) 이 그 짝을 정한다.

### 3. `NtUserPeekMessage` — 첫 메시지 차례 (결과서 표 B) [정적]

| 차례 | 무엇 | 조건 (깨움 비트 ∧ `CalcWakeMask` 마스크) | 반환 · MSG | 줄 |
|---|---|---|---|---|
| P0 | 깃발 · 부르기 | `wRemove & 0xe300fffc ≠ 0` → `LastError 0x3ec` · 0. 여섯째 인자 비트 0 이면 `KeWaitForSingleObject` 갈래 (user32 `_PeekMessage` 는 0 을 줌) | — | `NtUserPeekMessage +0x2d ~ +0x37 · +0xd7 ~ +0xe3 · +0xff` |
| P1 | 보낸 메시지 | `QS_SENDMESSAGE (0x40)` | **반환 안 됨** (안에서 처리) | `xxxRealInternalGetMessage +0x4be ~ +0x4cd · +0xb28 ~ +0xb30` |
| P2 | (입력 큐 주인 스레드일 때만) 입력 한 번 | `[큐+0x58] == 이 스레드` 등 | 입력 메시지 | `+0x831 ~ +0x8a3` |
| **P3** | **게시** | `QS_POSTMESSAGE (8)` | `xxxReadPostMessage(…, 지움 = wRemove & 1)` | `+0x8e0 ~ +0x90e` |
| **P4** | **입력** | `wake & mask & 0x3c07` | `xxxScanSysQueue` | `+0x9eb ~ +0xa2e` |
| **P5** | **`WM_PAINT`** | `QS_PAINT (0x20)` | `xxxDoPaint`: **`{hwnd = 칠할 창, 0xf, 0, 0, 시각, 커서}`** | `+0xbb1 ~ +0xbbb` · `xxxDoPaint +0x1b4 ~ +0x22e` (`+0x21e MOV [RBX+8],0xf`) |
| **P6** | **`WM_TIMER`** | `QS_TIMER (0x10)` | `DoTimer` | `+0xca8 ~ +0xcb3` |
| P7 | 없음 | — | 0. `PM_NOYIELD` 없으면 `zzzWakeInputIdle` · `xxxReceiveMessages` (`GetMessage` 판은 대신 잠) | `+0xc41 · +0x163c ~ +0x1651` |

- 반환 = `xxxInternalGetMessage` 반환. ≠ 0 이면 MSG 0x30 B 를 사용자 버퍼로 (`NtUserPeekMessage +0x120 ~ +0x159`; `+0x5c` · `+0x7c` 칸은 0 으로).
- **막 선 모달 대화상자 · 입력 없음 · 게시 없음** → P1 ~ P4 비고 **P5 `WM_PAINT` 먼저** (칠할 창마다; 창 차례는 `xxxInternalDoPaint` 몫 — 안 읽음 `[공백]`), 다 칠하면 P7 = 0. `WM_NCACTIVATE` · `WM_ACTIVATE` 는 **보낸 메시지라 PeekMessage 로 안 나온다** (P1).
- `WM_PAINT` 가 끝없이 되풀이되지 않으려면 칠하기가 끝나야 한다 — `0xf` 는 `DefWindowMsgs` 에 있어 `NtUserMessageCall` → 커널로 간다. stub 이 `WM_PAINT` 를 줬는데 `NtUserMessageCall` 이 0 만 주면 같은 `WM_PAINT` 가 반복될 것 `[추측: xxxDoPaint 반복 조건은 안 읽음]`.
- `WM_QUIT` 는 `xxxReadPostMessage` 안에서 나올 것 — 안 읽음 `[공백]`.

### 4. MessageBox 종류 → 기본 단추 ID (결과서 표 C) [정적] + [바이너리]

규칙 (`MessageBoxWorker`): ① 종류 `uType & 0xf` (> 6 이면 `LastError 0x59e` · 0, `+0x84 ~ +0x8d`) ② 단추 수 `mpTypeCcmd[종류] + ((uType >> 14) & 1)` (`MB_HELP` 면 하나 더, `+0xb1 ~ +0xbd`) ③ **기본 번호 `(uType >> 8) & 0xf`, 단추 수 이상이면 0** (`+0xc0 ~ +0xcd SBB/AND`) ④ 단추 `j` 의 칸 = `SEBbuttons[mpTypeIich[종류] + j]` (`+0x22b · +0x25b`) ⑤ **ID = `gpsi + 칸*0x28 + 0x3c4`** (`+0x2c8`) — 커널 `xxxLoadSomeStrings +0x4e` 가 `칸 + 1` 로 씀 (칸 0 ~ 10, `uStr = 0x320 + 칸` `+0x34`, 이름은 `xxxClientLoadStringW` 로 `+0x3a4 + 칸*0x28`). 칸: OK 0 · CANCEL 1 · ABORT 2 · RETRY 3 · IGNORE 4 · YES 5 · NO 6 · CLOSE 7 · HELP 8 · TRYAGAIN 9 · CONTINUE 10.

| 종류 | 단추 (왼쪽부터) | `DEFBUTTON1` | `DEFBUTTON2` (`0x100`) | `DEFBUTTON3` (`0x200`) | `DEFBUTTON4` (`0x300`) | `+ MB_HELP` |
|---|---|---|---|---|---|---|
| `MB_OK` (0) | `IDOK 1` | **1** | 1 | 1 | 1 | `[1, 9]`: 2 → 9 |
| `MB_OKCANCEL` (1) | `1` · `IDCANCEL 2` | **1** | **2** | 1 | 1 | `[1, 2, 9]`: 3 → 9 |
| `MB_ABORTRETRYIGNORE` (2) | `IDABORT 3` · `IDRETRY 4` · `IDIGNORE 5` | **3** | **4** | **5** | 3 | `[3, 4, 5, 9]`: 4 → 9 |
| `MB_YESNOCANCEL` (3) | `IDYES 6` · `IDNO 7` · `IDCANCEL 2` | **6** | **7** | **2** | 6 | `[6, 7, 2, 9]`: 4 → 9 |
| **`MB_YESNO` (4)** | `IDYES 6` · `IDNO 7` | **6** | **7** | 6 | 6 | `[6, 7, 9]`: 3 → 9 |
| `MB_RETRYCANCEL` (5) | `IDRETRY 4` · `IDCANCEL 2` | **4** | **2** | 4 | 4 | `[4, 2, 9]`: 3 → 9 |
| `MB_CANCELTRYCONTINUE` (6) | `IDCANCEL 2` · `IDTRYAGAIN 10` · `IDCONTINUE 11` | **2** | **10** | **11** | 2 | `[2, 10, 11, 9]`: 4 → 9 |

(표 값: `SEBbuttons = [0,8, 0,1,8, 2,3,4,8, 5,6,8, 5,6,1,8, 3,1,8, 1,9,10,8]` · `mpTypeIich = [0,2,5,12,9,16,19,0]` · `mpTypeCcmd = [1,2,3,3,2,2,3,0]` — 결과서 · flow-writer 둘 다 `ms/bin` 에서 읽음, 위 규칙으로 계산한 표. 종류마다 뒤에 `8 (HELP)` 가 붙어 있어 `MB_HELP` 는 한 칸 더 읽을 뿐.)

- **누르면** (`MB_DlgProc` `WM_COMMAND`, `+0x275 ~ +0x2a5` · 떨어진 범위 `+0x5eac8 ~ +0x5eb21`): `id = LOWORD(wParam)`. `3 · 4 · 5 · 6 · 7 · 10 · 11` → 곧장 `EndDialog(hwnd, id)`; `1 · 2` → **`GetDlgItem(hwnd, id)` 가 있어야** `EndDialog` (없으면 무시); `9` → 도움말; 그 밖 (`8` · `0` 포함) → 무시.
- **닫기 · Esc**: `MessageBoxWorker +0x331 ~ +0x346` 이 `[lpmb+0x78]` = (`MB_OK` 면 1, 아니면 `IDCANCEL` 단추가 있으면 2, 없으면 0), `MB_DlgProc +0x1a3 CMP · +0x1a7 JZ → +0x31f` 가 **0 이면 시스템 메뉴의 닫기를 지움** (`NtUserGetSystemMenu` · `NtUserDeleteMenu`). 곧 **`MB_YESNO` 는 닫기 · Esc 로 못 끝낸다.**
- **`pdlg+0x20` (기본 단추 ID = 결과값 칸, run148)**: 처음 `1` (`InternalCreateDialog +0x6fa`) → 컨트롤마다 `SendMessageW(컨트롤, 0x87)` (`+0x9ba`) 의 답에 `0x10` 이 있으면 (`+0x9bf TEST · +0x9c1 JNZ`) **`InternalCreateDialog +0x1341 ~ +0x134f`** (조각 `FUN_18001fbc1 +0x310 ~ +0x31e`) 가 그 컨트롤 ID 로 덮는다 — `DEFPUSHBUTTON` 이 여럿이면 **마지막 것**.
- **`gfDMREnable ≠ 0`** (user32 `0xda904`) 이면 상자를 안 띄우고 같은 표의 기본 단추 ID 를 곧장 돌려준다 (`MessageBoxWorker +0xe8 CMP · +0xef JZ` 안 탈 때 `+0xf1 ~ +0x10f` → `+0x116 JMP`). 이 전역을 켜는 곳은 `[공백]` (다음 요청 후보).

### 5. 비트표 값 — `user32+0xda568 ~ +0xda580` (결과서 표 D) [정적] + [바이너리]

| user32 칸 | `gSharedInfo` | 이름 `[추측]` | 값 | 채우는 줄 |
|---|---|---|---|---|
| `+0xda568` (dword) | `+0x218` | `DefWindowMsgs` 한도 | **`0x33f`** | `InitMessageTables +0x7c · +0x8d` → `[세션+0x5468]` → `InitMapSharedSection +0x2fe / +0x304` |
| `+0xda570` (포인터) | `+0x220` | `DefWindowMsgs` 비트 배열 (`(한도 >> 3) + 1` B, `SharedAlloc`) | 아래 43개의 비트가 1 | `InitMessageTables +0xae · +0xba` · 비트 켜기 `+0x626 ~ +0x646` · `InitMapSharedSection +0x338` |
| `+0xda578` (dword) | `+0x228` | `DefWindowSpecMsgs` 한도 | **`0x349`** | `InitMessageTables +0xe4 · +0xf5` → `[세션+0x5478]` → `InitMapSharedSection +0x33f / +0x345` |
| `+0xda580` (포인터) | `+0x230` | `DefWindowSpecMsgs` 비트 배열 | 아래 77개 | `InitMessageTables +0x116 · +0x122` · `+0x660 ~ +0x680` · `InitMapSharedSection +0x38c` |

- user32 쪽 길 (run157 해설 때 대조로 고침):
  - **보통 프로세스** (`gfServerProcess == 0`): `_UserClientDllInitialize +0x2aa CsrClientConnectToServer(…, 3, [RSP+0x60] 0x248 B, 0x248, &gfServerProcess)` → `+0x2be CMP [gfServerProcess],0` · `+0x2ca JNZ` 안 탐 → (PEB `+0x3` 비트 `0x2` 꺼짐이면 `+0x2e0 JZ → +0x30b`) **`+0x30b ~ +0x37c` 가 답 `+8` (`[RSP+0x68]`) 의 0x240 B 를 `gSharedInfo` (`+0xda350`) 로 복사** → `+0x380 ~ +0x387` `gpsi` (run85 그대로). `0xda568 − 0xda350 = 0x218` → 비트표 칸은 **답 버퍼로 `+0x220` · `+0x228` · `+0x230` · `+0x238`**.
  - PEB `+0x3` 비트 `0x2` 가 켜지고 `0x40` 이 꺼진 프로세스는 연결 전에 PEB `+0x58` 에 `{0x240, &gSharedInfo} | 1` 을 넣어 두고 (`+0x210 ~ +0x244`), 연결 뒤 `[PEB+0x58] == 0` 이어야 (`+0x2fa ~ +0x2fe`) 복사 없이 `+0x309 JMP +0x380` — 누가 그 주소에 쓰나는 `[추측: csrss 쪽]`.
  - **서버 프로세스** (`gfServerProcess ≠ 0`) 첫 스레드만: `ClientThreadSetup +0x120 NtUserProcessConnect` → `+0x134 ~ +0x1ad` 같은 0x240 B 복사.
  - 커널 쪽: `NtUserProcessConnect +0x260 InitMapSharedSection` 이 `USERCONNECT` 를 채우고 `+0x297 memcpy` 로 사용자 버퍼에. csrss 가 보통 프로세스의 USERSRV 답을 무엇으로 채우나 (같은 `NtUserProcessConnect` 를 클라이언트 핸들로 부르나) 는 안 읽음 `[추측]` · csrss 쪽 `[공백]`. 값 (`0x33f` · `0x349` · 목록 둘) 은 세션 칸에서 오므로 그대로 쓴다 `[추측: 같은 세션 칸]`.
- 쓰임 (`RealDefWindowProcWorker`, run148): `msg ≤ 한도1` ∧ 비트1 → `NtUserMessageCall (fnid 0x29e)` · 아니면 `msg ≤ 한도2` ∧ 비트2 → 유저 쪽 점프표 · 둘 다 아니면 **0** (`+0x56`).
- `DefWindowMsgs` (43, rva `0x374cb0`): `0x33 0x32 0xb 0xc 0xf(WM_PAINT) 0x10(WM_CLOSE) 0x14 0x1f 0x20 0x26 0x27 0x2b 0x4d 0x35 0x81 0x80 0x83 0x85 0x86(WM_NCACTIVATE) 0xa0 0xa5 0xa4 0xa1 0xa2 0xa3 0x101 0x105 0x106 0x112(WM_SYSCOMMAND) 0x22b 0x3b 0x88 0x317 0x7f 0x7b 0x313 0x50 0x51 0x128 0xae 0xaf 0x31b 0x33f`.
- `DefWindowSpecMsgs` (77, rva `0x374c00`): `0x6(WM_ACTIVATE) 0xd 0xe 0x205 0x11 0x13 0x18 0x21 0x53 0x2e 0x2f 0x100 0x104 0x109 0x22a 0x46 0x47 0x8b 0x19 0x132~0x137 0x84 0xac 0x138 0x55 0x219 0x218 0x20a 0x20e 0x20c 0x290 0x291 0x286 0x10f 0x10d 0x10e 0x284 0x281 0x283 0x282 0x285 0x287 0x8c 0x37 0x127 0x129 0x319 0x240 0x11a 0x119 0x241~0x243 0x245~0x247 0x249~0x24c 0x252 0x251 0x253 0x24e 0x24f 0x24d 0x2cd 0x272 0x349 0x5a 0x2e6 0x23b 0x23c` (결과서 목록, flow-writer 는 개수 · 최대값 · 아래 다섯 번호의 있음 / 없음만 다시 셈).
- **대화상자 길**: `0x87` (`WM_GETDLGCODE`) · `0x111` (`WM_COMMAND`) · `0x110` (`WM_INITDIALOG`) **없음 → syscall 없이 0**. `0xf` · `0x10` · `0x112` · `0x86` 은 커널로.

### 6. 단추 — `ButtonWndProcWorker` · `xxxButtonNotifyParent` [정적]

- `WM_GETDLGCODE (0x87)`: 창 fnid `0x2a1` 이고 단추 자료 `[[사본+0x128]]` 가 있을 때 종류 `[사본+0x1c] & 0xf` — **`1` (`BS_DEFPUSHBUTTON`) → `0x10 | 0x2000 = 0x2010`** (`+0xe8 SUB R14D,1 · +0xec JZ → +0x32b MOV EBX,0x10 → +0x12e BTS RBX,0xd`), `0` (`BS_PUSHBUTTON`) → `0x2020` (`+0xe6 JZ → +0x129 MOV EBX,0x20`).
- 클릭 알림: **`xxxButtonNotifyParent(단추, 코드)` = `SendMessageW(부모 hwnd, WM_COMMAND, word [사본+0x140] | 코드 << 16, 단추 hwnd)`** — 큐가 아니라 보내기, ID 는 아랫 16 비트만. 부모 hwnd = 부모 사본 `[사본+0x30]` (주소 변환 `− [사본+8] + 사본`) 의 `[+0]`, 부모 칸이 0 이면 자기 사본 (`+0xa JZ → +0x3f`) (`+0x0 ~ +0x2f · +0x32 MOV EDX,0x111 · +0x3a JMP SendMessageW` — flow-writer 가 `.asm` 으로 다시 봄).
- `BN_CLICKED (0)` 을 주는 줄: 조각 `FUN_1800aa27b` (= `ButtonWndProcWorker +0x9b80b`) `+0x12 SendMessageWorker(…, 0xf1 …)` (`BM_SETCHECK` `[추측: 이름]`) → `+0x1f xxxBNReleaseCapture` → **`+0x24 XOR EDX,EDX → +0x30 CALL xxxButtonNotifyParent`**. 어떤 입력 (마우스 뗌 · 스페이스 뗌 · `BM_CLICK`) 이 이 조각으로 가는지는 점프표를 다 안 읽어 `[공백]`.

### 7. 끝 길 syscall 부작용 — 커널 쪽 (결과서 표 E, run148 4절 보탬) [정적]

| syscall | 커널 쪽 몸 | user32 가 반환을 보나 | 줄 |
|---|---|---|---|
| `NtUserPeekMessage` `0x1001` | 위 3절. 못 받으면 0, 받으면 MSG 0x30 B | 0 / ≠0 만 | `NtUserPeekMessage +0xff · +0x120 ~ +0x159` |
| `NtUserSetWindowPos` `0x1021` (`0x97` = `NOSIZE \| NOMOVE \| NOZORDER \| NOACTIVATE \| HIDEWINDOW`) | 깃발 통과 (`+0x8f TEST R15D,0xfffd9800`) → `xxxSetWindowPos +0x66` → `xxxSetWindowPosAndBand +0x39 · +0x76 · +0x8b` → `xxxEndDeferWindowPosEx` (`+0x15b xxxCalcValidRects` · `+0x203 xxxSendChangedMsgs` (`WM_WINDOWPOSCHANGED 0x47` 보내기) · `PostShellHookMessagesEx` · `xxxCallHook` · `+0x68c xxxSwpActivate` · `+0x6a0 xxxDoSyncPaint`) — **보이기 비트 지움 + 위치 바뀜 보내기가 생길 수 있음** `[추측: 정확한 차례 · 개수는 안 읽음]` | 안 봄 (run148) | `NtUserSetWindowPos +0x8f · +0x14b` · `xxxSendChangedMsgs +0xe0 ~ +0xe8` (결과서) |
| `NtUserPostMessage` `0x100d` (`EndDialog` 의 `(hDlg, 0, 0, 0)`) | 큐에 `message = 0` 하나 — `DialogBox2` 는 그 전에 빠지므로 **다음 `PeekMessage` / `GetMessage` (앱 루프) 가 가장 먼저 받음** `[추측: 큐 코드 안 읽음, run148 E5 · E17 에서]` | 안 봄 | `EndDialog +0xd9` (run148) |
| `NtUserDestroyWindow` `0x1095` | `xxxDestroyWindow` → `_Phase2 +0x3a9 xxxDW_SendDestroyMessages` (`+0xba` `0x272` · **`+0xef WM_DESTROY`**) → `xxxFreeWindow +0x26 xxxFW_DestroyAllChildren · +0x31 _Phase2 +0x49` **`WM_NCDESTROY`** — **커널이 클라이언트로 되부르며 보냄**, `NCDESTROY` 에서 `DefDlgProcWorker +0x31a` 가 `pdlg` 해제 (run148) | 안 봄 | 위 호출 지점 표 |
| `NtUserMessageCall` `0x1005` | `msg < 0x400`: `MessageTable[msg] & 0xff` 로 `gapfnMessageCall` (rva `0x367420`) · `fnid` 인자 (`0x29e` → `xxxDefWindowProc` 쪽); `≥ 0x400`: 세션 표 | 반환 그대로 | `NtUserMessageCall +0x172 ~ +0x20b` (결과서) |
| `NtUserSetWindowLongPtr` `0x15da` | `≥ 0` 대화상자: `0` · `0x10` → 여분 블록 (run148). **`< 0`**: `xxxSetWindowData` — **`−21` 사본 `+0xd8`** · `−12` (자식) `+0x140` · `−6` `+0x20` · `−4` `+0x78` (+ `0x208` · `0x204` 갱신) · `−16` / `−20` 스타일 · `−8` 부모 · `−40` `+0xe8` 비트 5 | **앱이 옛 값을 쓰면 봄** | `xxxSetWindowLongPtr +0x4a JS · +0x47c` · `xxxSetWindowData +0x3b CMP EBX,-0x15 · +0x3e JZ → +0xd0 · +0xd4 MOV RSI,[RAX+0xd8] · +0xdb MOV [RAX+0xd8],R15` |
| `NtUserSetMsgBox` `0x1573` (`MB_DlgProc +0xec`) | 사본 `+0x10` 비트 5 켬 (`+0x8a SetOrClrWF(1,pwnd,0x20,0)`) · `[[pwnd+0x18]+8]+0x30` 을 1 올림 `[추측: 칸 이름]` → `DialogBox2 +0x289` 의 Ctrl+C 길 | 안 봄 | `NtUserSetMsgBox +0x2d ~ +0x8a` |
| `NtUserProcessConnect` `0x10e2` (프로세스 시작 한 번) | `InitMapSharedSection` 이 `USERCONNECT` 를 채워 복사 — `gpsi` 주소 · 핸들 표 · 위 5절 | 음수면 `ClientThreadSetup` 0. **user32 가 부르는 건 서버 프로세스 (`gfServerProcess ≠ 0`) 첫 스레드뿐** — 보통 프로세스는 csrss USERSRV 답 (run85 · run157 해설 때 대조) | `+0x260 · +0x297` |

- **`WM_CLOSE (0x10)`** 은 `DefWindowMsgs` 에 있어 `RealDefWindowProcWorker` → `NtUserMessageCall (0x29e)` → 커널 `xxxRealDefWindowProc +0x74a ~ +0x831` → **`xxxDestroyWindow(pwnd)` 곧장 (확인 없음)**. 대화상자는 그 전에 `DefDlgProcWorker` 가 가로채 앱이 FALSE 면 `PostMessageW(hDlg, WM_COMMAND, IDCANCEL …)` (run148 `+0x448 ~ +0x467`), `MessageBox` 의 `MB_DlgProc` 는 `WM_CLOSE` 를 따로 안 다뤄 같은 길.
- **`WM_SYSCOMMAND (0x112)`** 도 `DefWindowMsgs` → `xxxRealDefWindowProc +0x14bc xxxSysCommand` → **`SC_CLOSE (0xf060)` = `WM_CLOSE` 보내기** (`xxxSysCommand +0xd9 ~ +0x114` 연쇄 → `+0x186 ~ +0x1ae`).
- `WM_DESTROY` 앞에 보내는 `0x272` 가 무슨 메시지인지는 `[공백]` (다음 요청 후보). 앱이 `WM_DESTROY` 에서 `PostQuitMessage` 로 끝내는 구조면, 이 되부름을 안 하는 stub 에선 앱 루프가 안 끝난다 `[추측: 앱 코드 안 봄]`.

### 8. 그 밖 — 필터 · IMM · `GetUserDefaultLCID` [정적]

- **`g_systemCallFilterId`** = run153 이 닫음 (sast/150 한 줄 답 3 · 표 E): `GetProcessMitigationPolicy` 하위 4 비트 ← `NtQueryInformationProcess(0x34)` ← `[EPROCESS+0x6e8]`, 일반 프로세스 0. 이번 새 쓰임: `NtUserSetWindowLongPtr +0x86 PsGetWin32KFilterSet() == 5` 면 `GWLP_USERDATA` 말고는 `LastError 0x57` (`+0x12c ~ +0x13b`).
- **IMM 훅 `[user32+0xd96f0]` = `imm32!ImmTranslateMessage`**: `_InitializeImmEntryTable +0x6e3 LEA RDX,[0xb9f58]` (글 `"ImmTranslateMessage"`, `[바이너리]`) → `+0x6f4 GetProcAddress` → `+0x710 MOV [0xd96f0],RAX`. `TranslateMessage +0x12 CMP word [msg+0x10],AX(0xe5 = VK_PROCESSKEY)` → `+0x6a ~ +0x71` 에서 `(hwnd, message, wParam, lParam)` 로 부르고 반환 ≠ 0 이면 TRUE. **메시지 종류와 상관없이 wParam 아랫 16 비트가 `0xe5` 일 때만** (`WM_COMMAND` ID 229 포함) → 보통 안 탄다. 몸은 imm32 (프로젝트 없음).
- **`GetUserDefaultLCID`** (rva `0x50ef0`, 73 B): `+0x4 GetCurrentNlsCache()` → `+0x9 [RAX+0x10]` (사용자 기본 로케일 노드) 가 있으면 첫 dword, **`0x1000` 이면 `0xc00`** (`+0x1e CMOVZ`) 로 바꿔 반환. 없으면 `gpSysLocHashN` (`+0x27`) → 비었으면 `+0x39 SetupSystemLocaleHashNode(0)` 이 **`NtQueryDefaultLocale(FALSE, &gSystemLocale)` (`+0xf7`)** 로 받아 노드를 만든다 (`+0x87 MakeLocHashNode` / `+0xa1 MakeNamedLocaleHashNode`, `gTblPtrsLock` 아래). 이 갈래는 조각 `FUN_18019f4d2` 뒤 `+0x21` 로 돌아가 `0x1000 → 0xc00` 바꿈을 건너뛴다 (결과서). `GetCurrentNlsCache` (4643 B) 는 `TEB+0x179c` · csrss 가 나눠 주는 NLS 사용자 정보를 보고, 낡으면 `CsrClientCallServer` (`+0x34e`) · `RtlOpenCurrentUser` (`+0x6a0`) — **캐시 `+0x10` 이 어떻게 채워지는지는 `[공백]`**. 끝 길에선 `IsDialogMessageW +0x22` 가 부를 때만 (run148).

### 호출 관계

```
user32!_UserClientDllInitialize (보통 프로세스 — gfServerProcess == 0, run85 · run157 해설 때 대조)     [정적]
 └─ +0x2b1 ntdll!CsrClientConnectToServer(…, 3, 0x248 B)  → +0x30b ~ +0x37c 답 +8 의 0x240 B → gSharedInfo   (+0x218 0x33f · +0x220 비트 · +0x228 0x349 · +0x230 비트)

user32!ClientThreadSetup (서버 프로세스만 — gfServerProcess ≠ 0 · 첫 스레드, +0xdd ~ +0xed)         [정적]
 ├─ +0x127 NtUserProcessConnect ── SYSCALL 0x10e2 ⇒ +0x265 InitMapSharedSection   (세션 칸 [+0x5468 ~ +0x5480] ← InitMessageTables)
 │         +0x29c memcpy USERCONNECT → +0x134 ~ +0x1ad gSharedInfo 0x240 B 복사   (+0x218 0x33f · +0x220 비트 · +0x228 0x349 · +0x230 비트)
 └─ +0x22f NtUserInitializeClientPfnArrays ⇒ +0xb5 InitializeClientPfnArrays     (gpsi+0x188 A · +0x248 W · +0x308 · +0x1d50)

win32kfull!xxxCreateWindowEx (컨트롤마다 — run146 InternalCreateDialog +0x97a 길)
 ├─ +0x1b9 JS (0x80000000) → +0x21e4 BTR · +0x1bf 0x320 → +0x8f6 MapClientNeuterToClientPfn → +0x8fd [사본+0x78] (A 표)
 ├─ +0xa1b SetOrClrWF(0x320) 사본+0x13 비트5 · +0xa4e SetOrClrWF(0x208) 사본+0x12 비트3 (클래스 ANSI 일 때만)
 ├─ +0xbc7 WS_CHILD → +0x2049 hwndInsertAfter = 1 (HWND_BOTTOM)
 ├─ +0x1680 PWInsertAfter (0·1·−1·−2 그대로)
 └─ +0x1725 LinkWindow(새, 1, 부모)  마지막 형제 뒤 — 사본 +0x48 (앞 형제의 다음) · +0x50 (새 창의 이전)

user32!DialogBox2 +0x270 _PeekMessage +0x3f NtUserPeekMessage ── SYSCALL 0x1001          (run148 고리)
 ⇒ win32kfull!NtUserPeekMessage +0xe3 (깃발 0xe300fffc → 0x3ec) · +0x104 xxxInternalGetMessage +0x7d xxxRealInternalGetMessage
     ├─ +0x4d2 xxxReceiveMessages        P1 보낸 것 (반환 안 됨)
     ├─ +0x913 xxxReadPostMessage        P3 게시
     ├─ +0xa33 xxxScanSysQueue           P4 입력
     ├─ +0xbc0 xxxDoPaint                P5 WM_PAINT  {hwnd, 0xf, 0, 0}   ← 막 선 대화상자의 첫 메시지
     ├─ +0xcb8 DoTimer                   P6 WM_TIMER
     └─ (없음) +0x164e zzzWakeInputIdle · +0x1656 xxxReceiveMessages → 0   (PM_NOYIELD 면 건너뜀)

user32!MessageBoxW → MessageBoxTimeoutW +0x18f MessageBoxWorker
 │   +0xc0 ~ +0xcd 기본 번호 · +0x22b / +0x25b SEBbuttons[mpTypeIich[종류]+j] · +0x2c8 [gpsi+칸*0x28+0x3c4] = ID (커널 xxxLoadSomeStrings +0x4e)
 └─ +0x34e SoftModalMessageBox +0x516 MB_AddPushButtons · +0x5b6 InternalDialogBox → DialogBox2 (같은 고리)
                                      InternalCreateDialog +0x9bf WM_GETDLGCODE & 0x10 → +0x1341 ~ +0x134f pdlg+0x20 = 기본 ID
                                      ⇢ ButtonWndProcWorker  BS_DEFPUSHBUTTON → 0x2010
                                      ⇢ MB_DlgProc WM_COMMAND → EndDialog(id) (1·2 는 GetDlgItem 있어야)

user32!DialogBox2 +0x3cb NtUserDestroyWindow ── SYSCALL 0x1095
 ⇒ win32kfull!xxxDestroyWindow +0xb6 _Phase2 +0x3ae xxxDW_SendDestroyMessages +0xf4 ⇢ WM_DESTROY (되부름)
                              +0xd4 xxxFreeWindow +0x36 _Phase2 +0x4e ⇢ WM_NCDESTROY (되부름)
```

- (나무 오프셋은 ret 자리, 갈래 줄은 함수 안 줄. `⇒` 는 syscall 너머, `⇢` 는 보내기 · 되부름 · CFG 간접. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (결과서 "PE_emulator 쪽 — 지금 맞나 · 틀리나" — run148 표에 이어)

flow-writer 는 `../PE_emulator/ext/syscall/syscall_layer_cpp/src/gui/` 파일 목록 (24개) 과 `nt_user_peek_message.cpp` (53줄) 의 멈춤 줄 · `nt_user_create_window_ex.cpp` 의 `+0x78` 줄 (`:45` · `:162`) 만 줄 번호로 확인했다 (읽기만). 결과서는 그 밖에 `nt_user_wait_message.cpp` (20줄) · `nt_user_create_window_ex.cpp` 머리 주석 · `src` 전체 이름 grep 을 읽었다 — `NtUserDestroyWindow` · `NtUserSetWindowPos` · `NtUserSetMsgBox` · `NtUserMessageCall` · `NtUserSetWindowLongPtr` · `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 `gui/` 에 몸 파일이 없다 (다른 모듈이 받는지는 `[공백]`). 고칠지는 사용자 · PE_emulator 결정.

| 우리 지금 | 진짜 (정적) | 판정 |
|---|---|---|
| **`NtUserPeekMessage` = 게시 큐만, 없으면 `gui_peek_message_none` 멈춤** (`nt_user_peek_message.cpp :29` · `:50`) | 차례 = **게시 → 입력 → `WM_PAINT` → `WM_TIMER` → 0** (P3 ~ P7), 보낸 메시지는 안 나옴 | **순서는 맞음 (P3 한 칸만 한 꼴)**. 빠진 것: P5 `WM_PAINT` · P6 `WM_TIMER` (입력 P2 · P4 는 없어도 됨). 큐가 비면 멈춤은 진짜의 "0 + `WaitMessage` 잠" 과 달라 `[정책]`. 거르기 (`min` / `max` ≠ 0 · `wRemove` 윗 16 비트) 를 멈춤으로 두는 건 진짜보다 좁지만 모달 고리 `DialogBox2` 는 (0, 0, 0, 1) 만 줘서 무해 |
| `WM_PAINT` 를 만들어 줄 때 | 진짜는 칠할 창이 남아 있는 동안 계속 줌. `0xf` 는 `DefWindowMsgs` → `NtUserMessageCall` 이 칠하기를 끝내야 멈춤 | **`WM_PAINT` 를 주려면 `NtUserMessageCall(WM_PAINT)` 가 그 창의 갱신 영역을 비워야** — 아니면 같은 `WM_PAINT` 끝없이 `[추측]` (최소 목록 24) |
| `NtUserWaitMessage` = 잠 | 빈 틈 (P7 = 0) 뒤 잠, 깨움은 큐 · 입력 · 보냄 · 타이머 | **맞음** |
| **`NtUserCreateWindowEx` — hwnd · 사본만, 자식 연결 · ANSI 안 함** | 연결 = **맨 끝 (템플릿 차례)**, 이전 형제 `사본+0x50` 도 씀 | **run146 stub 10 "템플릿 차례" 확정 + `사본+0x50` 추가** (`GetWindow(GW_HWNDPREV)` · `GetNextDlgTabItem(이전)` 이 읽을 것 `[추측]`) |
| `사본+0x78 = user32 + kDefDlgProcWRva` (`nt_user_create_window_ex.cpp :45` · `:162`, W 판 고정) | 확장 스타일 `0x80000000` (A 판 `DialogBoxIndirectParamA`, run148) 이면 **`사본+0x78` = A 표 칸 = `DefDlgProcA`** (대화상자 `gpsi+0x1d8`), 컨트롤도 A 판 프로시저 | **A 판 시료에서 틀림.** W 판은 맞음. `사본+0x13` 비트 5 · `+0x12` 비트 3 은 끝 길에 안 쓰여 안 틀림 (run148 6절) |
| `MessageBox` "기본 단추 누름" = `WM_COMMAND(LOWORD(pdlg+0x20), 0 이면 1)` (run148 최소 목록 7) | 기본 ID 는 표 C (`MB_YESNO` = `IDYES 6`). `pdlg+0x20` 은 단추 컨트롤의 `WM_GETDLGCODE` 답 (`0x10`) 으로만 그 ID 가 됨 | **`pdlg+0x20` 이 표 C 값일 때만 맞음.** stub 이 컨트롤 프로시저를 안 타게 (사본 `+0x12` 비트 2 우회 · 항목 없음) 하면 `pdlg+0x20 = 1` 그대로 → **`MB_YESNO` 가 `WM_COMMAND(1)` 을 받아 `GetDlgItem(hwnd, 1)` 실패 → 무시 → 끝없는 고리 (run148 F7)**. 고칠 꼴 둘: ① 사본 `+0x1c & 0xf == 1` 인 컨트롤 ID 를 `pdlg+0x20` 에 직접 ② 표 C 를 stub 이 직접 (종류 · `MB_DEFBUTTON*` → ID) |
| `MBStrings` (`gpsi+0x3a4 + 0x28*i`) — `psi` 0 채움 (run142) | `+0x3c4 + 0x28*i` 의 **`uID` 를 `MessageBoxWorker +0x2c8` 이 단추 ID 로 읽음** | **0 이면 단추 ID 가 전부 0** → 누른 `WM_COMMAND(0)` 은 `MB_DlgProc` 가 무시 · 반환 0. 이름 (`+0x3a4`) 만 비면 빈 글 단추 (run142 판정은 이름 쪽만 맞음) |
| 닫기 (×) · Esc 를 "누름" 으로 칠 때 | `MB_OK` → `IDOK`, `IDCANCEL` 단추가 있으면 `IDCANCEL`, 없으면 닫기 메뉴 지움 · 무시 | **`MB_YESNO` · `MB_ABORTRETRYIGNORE` 는 닫기 · Esc 로 못 끝냄** |
| csrss USERSRV 연결 답 (`csr_user_connect_stub.cpp`) 의 `gSharedInfo` 칸 (· `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 몸 없음 — 보통 프로세스는 안 부름) | **비트표 둘은 USERSRV 답 `+8` 의 0x240 B 가 줌** (`NtUserProcessConnect` 는 서버 프로세스만 — run157 해설 때 대조). A / W 표는 `gpsi` 공유 메모리에 이미 있는 값 `[추측]` | 답의 비트표 칸이 0 이면 (한도 0) → **모든 `DefWindowProc` 가 syscall 없이 0** — 끝 길 (`WM_COMMAND` · `WM_GETDLGCODE` · `WM_INITDIALOG`) 엔 영향 없음, 대신 `WM_PAINT` · `WM_CLOSE` · `WM_SYSCOMMAND` · `WM_NCACTIVATE` 기본 처리가 아무 일도 안 함. `gpsi+0x188` (A 표) 가 0 이면 → `사본+0x78` 에 쓸 A 판 값을 모름 |
| `NtUserDestroyWindow` 몸 없음 → 0 (run148: 기본 0 맞음) | `WM_DESTROY` · `WM_NCDESTROY` 를 커널이 되부르며 보냄 | **반환은 그대로 맞음.** 되부름을 안 하면 앱의 `WM_DESTROY` 처리 (보통 `PostQuitMessage`) 가 안 돎 — 모달 끝 길엔 무관, 앱 자기 루프 (`while (GetMessage())`) 엔 영향 `[추측]` |
| `NtUserSetWindowLongPtr` 몸 없음 (run148: 틀림) | `GWLP_USERDATA (−21)` = **`사본+0xd8` 쓰고 옛값 반환** (run148 `[추측]` 확인) | run148 판정 그대로 + `−21` 자리 확정 |
| `NtUserSetMsgBox` 몸 없음 → 0 | 반환 안 봄 (`MB_DlgProc +0xf3` 뒤 안 씀), 사본 `+0x10` 비트 5 는 Ctrl+C 길용 | **맞음** |

**`NtUserPeekMessage` 진짜 첫 메시지 (stub 이 진짜를 흉내 낼 때의 차례)**: 부를 때마다 ① 게시 큐 (stub 큐 — `EndDialog` 의 `message = 0` · 앱 `PostMessage` 포함) 맨 앞 → ② (입력 없음) → ③ 칠할 창이 남았으면 `{그 hwnd, WM_PAINT 0xf, 0, 0}` → ④ 걸린 타이머가 됐으면 `WM_TIMER` → ⑤ 0. 막 선 대화상자에선 첫 부름 = `WM_PAINT` (대화상자 → 컨트롤 차례는 `xxxInternalDoPaint` 몫 `[공백]`), "기본 단추 누름" 정책 (run148) 은 그 뒤 게시 칸 (①) 에 `WM_COMMAND` 를 넣는 꼴이면 진짜 차례와 어긋나지 않는다 `[추측: 정책 맞춤]`.

**stub 이 채워야 하는 최소 목록 — 결과서 번호 18 ~ 25** (sast/146 결과서 9 ~ 17 = run148 문서 7 ~ 15 에 이어 붙임. 1 ~ 8 은 sast/142 = run146 문서 1 ~ 6):

18. **자식 연결 (run146 stub 10 보탬)**: 컨트롤 사본을 만들 때마다 부모의 **마지막 자식 뒤에** — 첫 자식이면 `[부모사본+0x38] = 새`, 아니면 `[마지막+0x48] = 새` · `[새+0x50] = 마지막`; `[새+0x30] = 부모사본` · `[새+0x48] = 0`. (`HWND_TOP` 이면 반대로 첫 자식 앞.)
19. **A 판 (run148 6절)**: 확장 스타일 `0x80000000` 이면 저장 스타일에서 지우고 **`사본+0x78` = A 표 칸** (대화상자 = `DefDlgProcA`, 단추 = `ButtonWndProcA` …), `사본+0x13` 비트 5 켬; `사본+0x12` 비트 3 은 클래스가 ANSI 이거나 (ANSI 만든이 ∧ 내장 클래스 아홉) 일 때만. A / W 표는 `gpsi+0x188` / `+0x248` 에 있다 — 커널 `NtUserInitializeClientPfnArrays` (win32u `0x14aa`) 가 채우지만 user32 는 **서버 프로세스에서만** 부르므로 (run157 해설 때 대조), 보통 프로세스 stub 은 **`gpsi` (USERSRV 답이 주는 `psi`) 의 그 칸을 미리 채워 둬야** `사본+0x78` 에 쓸 값을 안다 `[추측: 세션 공유 값]`.
20. **csrss USERSRV 연결 답 (서버 3 · 0x248 B — `csr_user_connect_stub.cpp`) 의 `gSharedInfo` 0x240 B (답 `+8` 부터)** (run157 해설 때 대조 — 처음엔 "`NtUserProcessConnect` 가 돌려주는" 으로 적었으나 그건 서버 프로세스만): 비트표가 필요하면 `gSharedInfo` `+0x218 = 0x33f` · `+0x220` = 5절 43개 비트 배열 · `+0x228 = 0x349` · `+0x230` = 77개 비트 배열 (**답 버퍼로는 `+0x220` · `+0x228` · `+0x230` · `+0x238`**, 배열 포인터는 게스트가 읽을 수 있는 주소). 안 채워도 (한도 0) 끝 길엔 영향 없음.
21. **`MBStrings` (`MessageBox` 필수)**: `gpsi + 0x3a4 + 0x28*i` (i = 0 ~ 10) — `+0x00` wchar[15] 이름 (보이는 것만 영향) · **`+0x20` (= `gpsi+0x3c4+0x28*i`) `uID = i + 1`** · `+0x24` `uStr = 0x320 + i`.
22. **`pdlg+0x20` = 기본 단추 ID**: 컨트롤 프로시저를 안 타는 stub 은 `BS_DEFPUSHBUTTON` (사본 `+0x1c & 0xf == 1`) 컨트롤의 ID 를 `[[사본+0x128]+8]+0x20` 에 써 줄 것 (표 C 와 같아짐).
23. **`NtUserSetMsgBox (0x1573)`**: 반환 안 봄 → 아무 값.
24. **`NtUserMessageCall (0x1005)` 몸 (`WM_PAINT` 를 줄 때만)**: `msg = 0xf` 면 그 창의 갱신 영역을 비움 (아니면 같은 `WM_PAINT` 반복 `[추측]`).
25. **`NtUserDestroyWindow` (run148 11 보탬)**: `WM_DESTROY (2)` · `WM_NCDESTROY (0x82)` 를 앱 프로시저에 **보낼지는 정책** — 안 보내도 모달 끝 길은 닫힘 (run148 F10), 앱이 `WM_DESTROY` 에서 `PostQuitMessage` 로 끝내는 구조면 보내야 앱 루프가 끝남.

**비트표 `gSharedInfo` 값 (한 줄)**: `+0x218 = 0x33f`, `+0x220 → (0x33f >> 3) + 1 = 0x68` B 비트 배열 (5절 43개), `+0x228 = 0x349`, `+0x230 → 0x6a` B 비트 배열 (77개) — 바이트 수는 `InitMessageTables` 의 `(한도 >> 3) + 1` 규칙으로 flow-writer 가 셈 `[정적 규칙 + 계산]`.

## 확인된 것

- `[정적]` 새 자식 창은 `xxxCreateWindowEx` 가 `HWND_BOTTOM (1)` 을 주고 `LinkWindow` 가 마지막 형제 뒤에 끼운다 — 연결 차례 = 만든 차례 (대화상자 = 템플릿 차례). 이전 형제 칸은 사본 `+0x50`.
- `[정적]` 확장 스타일 `0x80000000` 은 `사본+0x78` 을 A 표 칸으로 · `사본+0x13` 비트 5 를 켜고 저장 스타일에선 지워지며, `사본+0x12` 비트 3 은 클래스 쪽 조건으로만 켜진다.
- `[정적]` `NtUserPeekMessage` 의 꺼내는 차례는 보낸 것 (안에서) → 게시 → 입력 → `WM_PAINT` → `WM_TIMER` → 0 이고, 보낸 메시지는 반환으로 안 나온다. 요청서의 `xxxPeekMessage` 는 이 빌드 PDB 에 없다.
- `[정적]` `MessageBox` 는 `DialogBox2` 고리를 그대로 타며 `MessageBoxWorker` · `MessageBoxTimeoutW` 는 반환을 바꾸지 않는다. 단추 ID 는 `gpsi` `MBStrings` `uID` (커널이 `칸+1` 로 씀) 이고, `MB_YESNO` 의 기본은 `IDYES (6)` 이다 (표 값 `[바이너리]`, flow-writer 도 다시 읽음).
- `[정적]` user32 비트표는 `gSharedInfo` 복사이고 (`NtUserInitializeClientPfnArrays` 아님) — **보통 프로세스는 csrss USERSRV 연결 답 `+8` 의 복사** (`_UserClientDllInitialize +0x30b ~ +0x37c`), `NtUserProcessConnect` 복사 (`ClientThreadSetup +0x134 ~ +0x1ad`) 는 `gfServerProcess ≠ 0` 첫 스레드만 (run157 해설 때 대조), 한도는 `0x33f` · `0x349`, `WM_GETDLGCODE` · `WM_COMMAND` · `WM_INITDIALOG` 는 목록에 없다 (`[바이너리]`, flow-writer 다시 셈).
- `[정적]` `BS_DEFPUSHBUTTON` 의 `WM_GETDLGCODE` 답은 `0x2010`, 클릭 알림은 부모에게 보내는 `WM_COMMAND(ID | 코드 << 16, 단추)`.
- `[정적]` `WM_DESTROY` · `WM_NCDESTROY` 는 커널 `xxxDestroyWindow` 길이 보내기로 클라이언트에 되부른다. `WM_CLOSE` 기본 처리는 확인 없이 `xxxDestroyWindow`, `SC_CLOSE` 는 `WM_CLOSE` 보내기.
- `[정적]` `GWLP_USERDATA` 쓰기 = 커널 `xxxSetWindowData` 가 `사본+0xd8` 에 쓰고 옛값을 돌려준다.
- `[정적]` IMM 훅 칸 `[user32+0xd96f0]` 은 `imm32!ImmTranslateMessage` 주소이고 wParam 아랫 16 비트 `0xe5` 일 때만 불린다.
- `[관찰]` test.exe 초기화 끝 모듈 6개에 user32 가 없다 (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run148 과 같은 줄) — 이 길은 test.exe 에선 안 생긴다.

## 정정

- **이름 — 요청서 · run148 "아직 확인 필요" 정적 2 의 `xxxPeekMessage`**: 이 빌드엔 없다 — `pdb_aliases.py --where win32kfull.sys xxxPeekMessage` → "PDB 공개 심볼에 없음", `_index.tsv` 에 `PeekMessage` 가 든 이름은 `NtUserPeekMessage` (+ SEH 거르개 둘) 뿐. **몸 = `NtUserPeekMessage +0xff xxxInternalGetMessage +0x78 xxxRealInternalGetMessage`** (`GetMessage` 판과 같은 몸, `wRemove` · 깃발로 갈림). run148 문서는 원문을 두고 맨 위 "고쳐 읽기 (run157)" 줄에 적었다. flow.md 4장 run148 정적 줄의 같은 이름엔 "(run157: 이 빌드에 없음 — 몸 `xxxRealInternalGetMessage`)" 를 붙였다.

## 앞 run 고쳐 읽기

틀린 확정은 없고, `[추측]` · `[공백]` 이 정적으로 닫히거나 좁혀진 것이라 "고쳐 읽기" 로 각 문서 (와 해설서) 맨 위에 한 줄씩 넣었다 (원문은 그대로).

- **run148** (sast/146): ① 정적 5 "`NtUserInitializeClientPfnArrays` · `ClientThreadSetup +0x228` — 비트표 채우는 곳 `[추측]`" → **아님**: `gSharedInfo` 복사 (`+0x218 ~ +0x230`) — 보통 프로세스는 csrss USERSRV 연결 답 `+8` (`_UserClientDllInitialize +0x30b ~ +0x37c`), 서버 프로세스만 `ClientThreadSetup +0x120 NtUserProcessConnect` (run157 해설 때 대조로 고침 — 처음엔 `NtUserProcessConnect` 로만 적었음), 값 `0x33f` · `0x349` (8절 칸 표 "비트표 … `[공백: 값]`" 닫힘) ② 7절 `WM_GETDLGCODE` 0 길 "syscall 0 이거나 `NtUserMessageCall` 1번 — 정적으로 못 정함" → **syscall 0** (`0x87` 목록에 없음) ③ 8절 `+0x38` "`[추측: 템플릿 차례]`" → `[정적]` 확정 (+ `+0x50` 이전 형제) ④ 8절 `+0xd8` "커널이 씀 `[추측]`" · PE_emulator 14 "`−21` → `+0xd8` `[추측]`" → `[정적]` (`xxxSetWindowData +0xd4 / +0xdb`) ⑤ "되부름이 꼭 필요한가" 의 `NtUserDestroyWindow` 되부름 `[추측]` → `[정적]` ⑥ 6절 "`+0x12` 비트 3 을 켜는 쪽 커널 `[추측]`" → 클래스 쪽 조건 (확장 스타일만으론 안 켜짐) ⑦ 3절 IMM 훅 `[공백: 훅 몸]` → `imm32!ImmTranslateMessage` ⑧ 2절 `xxxButtonNotifyParent` `[공백]` → 보내기 `WM_COMMAND` ⑨ "정정" 의 `MessageBoxWorker` · `MessageBoxTimeoutW` 반환 `[공백]` → 안 바꿈 ⑩ PE_emulator 최소 목록 7 "ID = `LOWORD(pdlg+0x20)`, 0 이면 1" → `pdlg+0x20` 이 표 C 값일 때만 ⑪ 정적 2 `xxxPeekMessage` 이름 (위 "정정").
- **run146** (sast/142): ① "아직 확인 필요" 정적 5 "(필요하면) 길 A 일꾼 `psi+0x320` 을 채우는 쪽 — `NtUserInitializeClientPfnArrays`" → `InitializeClientPfnArrays` 가 셋째 배열 (0x58 B) 을 `gpsi+0x308` 에 쓴다 (`+0x2b2`) — `+0x320` 은 그 범위 안 `[정적 두 줄을 이은 것]` ② 6절 사본 `+0x12` 비트 3 "= 0 (유니코드)" 는 W 판에선 맞고, A 판에서 켜는 조건은 클래스 ANSI / 내장 아홉 (표 A' B8 · B9) ③ `[공백]` "컨트롤 프로시저 (`ButtonWndProcWorker` 류) 본문" → `WM_GETDLGCODE` 답 · 클릭 알림 닫힘 (클릭 입구는 아직 `[공백]`).
- **run142** (sast/139): 5절 칸 표 "`psi+0x3a4 + 40*k` · `+0x3c4` · `+0x3c8` — 0 이면 빈 글 단추 (상자는 뜸)" → **`+0x3c4` (`uID`) 가 0 이면 단추 ID 가 전부 0** — 상자는 떠도 단추를 눌러 끝낼 수 없다 (`MB_DlgProc` 가 ID 0 무시). `+0x3c4` 를 쓰는 쪽은 커널 `xxxLoadSomeStrings +0x4e` (`칸 + 1`).

## flow-writer 대조 (결과서와 다르게 적은 곳 — 결론 바꾸는 어긋남 없음)

결과서 오프셋을 `.asm` · `_calls.tsv` 와 맞춰 봤다. 함수 기준 · 주소 · 조각 기준이 섞인 곳과 "갈래 줄 ↔ call 줄" 이 어긋난 곳 여섯 — 이 문서엔 고친 값을 썼다. 7 · 8 은 run157 해설 때 대조로 보탠 것이고, **8 은 결론 하나 (비트표 출처) 를 바꾼다** (맨 위 "정정").

1. **표 B P7 · 한 줄 답 2 "`zzzWakeInputIdle` `+0xc41 · +0x1646`"** → 둘째 call 은 **`+0x1649`** (ret `+0x164e`). `+0x1646` 은 `MOV RCX,RSI`. `PM_NOYIELD` 검사는 `+0x163c TEST [RSP+0x2b0],2` 이고 갈리는 줄은 **`+0x1644 JNZ`**.
2. **표 E `xxxDW_SendDestroyMessages +0xa8 · +0xeb`** → 이 둘은 메시지 번호를 넣는 줄 (`MOV EDX,0x272` · `LEA EDX,[R9+0x2]`), **call 은 `+0xba` · `+0xef`** (ret `+0xbf` · `+0xf4`).
3. **표 A' B1 "`+0x1b9 TEST R15D,R15D · JS`"** → `TEST` 는 `+0x1b6`, `JS` 가 `+0x1b9`. 목적지 `+0x21e4` 는 맞음.
4. **표 A A2 "`+0x2038 _GetDesktopWindow … JNZ → +0x2bd9`"** → `+0x2038` 은 `MOV RCX,R14`, **call 은 `+0x203b`**, `JNZ` 는 `+0x2043`. (그 앞 `+0x202b IsDpiBoundaryBetweenWindows` · `+0x2032 JNZ → +0x2b8a` 갈래가 하나 더 있다 — 결과서에 없던 줄, 뜻은 안 따라감.)
5. **한 줄 답 3 · 표 C 끝 "`InternalCreateDialog +0x9ba ~ +0x9c1` → 조각 `+0x1341 ~ +0x134f`"** → `+0x1341 ~ +0x134f` 는 **함수 기준** (`= InternalCreateDialog +0x1341`), 조각 기준으로는 `FUN_18001fbc1 +0x310 ~ +0x31e`. 앞쪽은 call `+0x9ba` (ret `+0x9bf`) · `+0x9bf TEST AL,0x10` · `+0x9c1 JNZ`.
6. **표 C 끝 · 표 A 결론 `MB_DlgProc` "`+0x189 ~ +0x1a3` `GetWindow(GW_CHILD)` … `+0x1a3 CMP [RDI+0x78],0 → JZ +0x31f`"** → `GetWindow(5)` call 은 `+0x184` (ret `+0x189`), 되풀이 `GetWindow(2)` 는 `+0x310`, `NtUserSetFocus` 는 `+0x197`. `JZ` 는 **`+0x1a7`** (`+0x1a3` 은 `CMP`).
7. **(run157 해설 때 대조) 표 E `NtUserPostMessage` "`EndDialog +0xb0`" (sast/146 에서 온 값)** → `+0xb0` 은 `NtUserSetWindowPos` 의 ret (call `+0xa9`). **`NtUserPostMessage` 는 call `+0xd9` · ret `+0xe0`** (`ms/disassemble/user32/_calls.tsv` `EndDialog` 줄). 이 문서 7절 표는 처음부터 `+0xd9` 로 적었다.
8. **(run157 해설 때 대조) 비트표 출처** — 결과서 표 D · 한 줄 답 4 의 "`ClientThreadSetup +0x120 NtUserProcessConnect` 가 받은 0x240 B 복사" 를 이 문서도 조건 없이 따라 적었다. `.asm` 은 그 복사가 `gfServerProcess ≠ 0` 첫 스레드만이고, 보통 프로세스는 `_UserClientDllInitialize +0x30b ~ +0x37c` 의 csrss USERSRV 답 복사다 (맨 위 "정정"). 결과서 줄 번호 자체 (`+0x120` · `+0x134` · `+0x228`) 는 맞다.
- 보탬: VM 항목 4 "`MessageBoxWorker+0x34e` 직전 `[rbx+0x60]` …" — `+0x34e` 는 `SoftModalMessageBox` call (`+0x349`) 의 ret 자리다. 부르기 **전** 값을 보려면 `+0x349` 에 걸고, `+0x34e` 는 부른 **뒤** (같은 구조체라 칸 값은 같을 것 `[추측]`).
- 그 밖 결과서 줄 (`NtUserPeekMessage +0x2d ~ +0x189` · `xxxRealInternalGetMessage` P1 ~ P6 · `xxxCreateWindowEx +0x8e6 ~ +0x8fd · +0xa04 ~ +0xa49 · +0xb51 · +0xbc7 · +0x167b ~ +0x1725 · +0x21e4` · `LinkWindow` A6 ~ A9 · `PWInsertAfter` · `MapClientNeuterToClientPfn` · `InitializeClientPfnArrays` · `InitMessageTables` · `xxxLoadSomeStrings +0x34 · +0x4e` · `NtUserProcessConnect +0x260 · +0x297` · `xxxRealDefWindowProc +0x831 · +0x14bc` · `xxxSysCommand +0x1ae` · `xxxDestroyWindow` 여섯 call · `_Phase2 +0x3a9` · `xxxFreeWindow_Phase2 +0x3c · +0x49` · `NtUserSetWindowPos +0x8f · +0x14b` · `xxxSetWindowPos(AndBand)` · `xxxEndDeferWindowPosEx` 여섯 call · `NtUserSetWindowLongPtr +0x86 · +0x136` · `xxxSetWindowData +0x3b ~ +0xdb` · `NtUserSetMsgBox +0x8a` · user32 `ClientThreadSetup +0x120 · +0x134 · +0x228` · `MessageBoxWorker` ①~⑤ 줄 · `+0x331 ~ +0x346` · `MessageBoxTimeoutW +0x18a · +0x19e` · `SoftModalMessageBox +0x511 · +0x5b1 · +0x5b6 ~ +0x5be` · `MB_DlgProc +0xec · +0x275 ~ +0x2a5` · `ButtonWndProcWorker +0xe3 ~ +0xec · +0x129 · +0x12e · +0x32b` · 조각 `FUN_1800aa27b +0x24 · +0x30` · `xxxButtonNotifyParent +0x32 · +0x3a` · `_InitializeImmEntryTable +0x6e3 · +0x6f4 · +0x710` · `TranslateMessage +0x12 · +0x6a · +0x71` · kernelbase `GetUserDefaultLCID +0x4 · +0x9 · +0x1e · +0x27 · +0x39` · `SetupSystemLocaleHashNode +0x87 · +0xa1 · +0xf7` · `GetCurrentNlsCache +0x34e · +0x6a0`) 와 표 값 (`[바이너리]`) 은 **일치**.

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (user32 안 올림) 이다. `gSharedInfo` 비트표 · `MBStrings` · A / W 표는 GUI 프로세스의 user32 / 세션 짐이라 test.exe 짐 지도엔 안 든다.

## 아직 확인 필요

동적 (결과서 "VM 으로 확인할 것 — 디버거 필요" 1 ~ 9, 대기열 **Q273** VM GUI 세션에 얹을 꼴 — test.exe 가 아니라 `DialogBoxParamW` / `MessageBoxW` 를 부르는 GUI 프로세스, user32 유저 멈춤점):

1. `user32!DialogBox2+0x270` — `r eax` · `dq @rsp+0x50 L6` 을 부름마다: 첫 `message` 가 `0xf` 인지, 몇 번 · 어느 hwnd, 언제 0 이 되나 (run148 동적 1 과 같은 자리 — 이번에 기대값이 생김).
2. 자식 연결 진짜 값 — 대화상자 사본 `+0x30 ~ +0x50` → `poi(@rax+0x38)` 부터 `+0x48` 로 따라가며 `+0x140` (ID) · `+0x50` (이전), 템플릿 차례 · 마지막 `+0x48 = 0` 인지 (run148 동적 6).
3. A 판 — `DialogBoxIndirectParamA` 시료로 `InternalCreateDialog+0x658` 뒤 `dq @rax+0x78 L1` (= `x user32!DefDlgProcA`?) · `db @rax+0x12 L2` · `dq poi(user32+0xda590)+0x1d8 L1` · `+0x298 L1` (run148 동적 12).
4. MessageBox 종류별 (`MB_YESNO` · `MB_YESNOCANCEL | MB_DEFBUTTON2` · `MB_CANCELTRYCONTINUE` · `MB_OK | MB_HELP`) — `user32!MessageBoxWorker+0x349` (부르기 전) `[rbx+0x60]` (단추 ID 배열) · `[rbx+0x74]` · `[rbx+0x78]`, `InternalCreateDialog+0x134f` 뒤 `pdlg+0x20` — 표 C 와 같은지.
5. `gpsi` `MBStrings` — `dd poi(user32+0xda590)+0x3c4 L1` 부터 0x28 간격 11칸 (`uID` 1 ~ 11?) · `+0x3c8` (`uStr` `0x320 ~ 0x32a`?).
6. 비트표 — `dd user32+0xda568 L1` (`0x33f`?) · `dd user32+0xda578 L1` (`0x349`?) · `db poi(user32+0xda570)+0x10 L1` 비트 7 (`0x87`, 0 이어야) · `0xf` · `0x10` · `0x112` 비트 (run148 동적 9).
7. `user32!xxxButtonNotifyParent` — 단추 한 번 눌러 `edx` (코드) · `[rcx+0x140]` · `SendMessageW` 의 `r8`.
8. 끝 길의 보냄 — `DestroyWindow` 동안 대화상자 프로시저가 받는 메시지 차례 (`0x272` · `WM_DESTROY` · `WM_NCDESTROY`, `DefDlgProcWorker` 입구 조건부 로그) · `EndDialog` 뒤 `message = 0` 이 다음 `PeekMessageW` 에 오는지.
9. `SC_CLOSE` — `PostMessage(WM_SYSCOMMAND, SC_CLOSE)` → 앱 프로시저가 `WM_CLOSE` 를 받고 `WM_COMMAND(IDCANCEL)` 이 큐에 오는지 (run148 동적 11).

디버거 없이 떠 올 것 (결과서 "디버거 없이" — 시료 프로그램):

- **MessageBox 반환값 표**: 일곱 종류 × `DEFBUTTON1 ~ 4` (+ `MB_HELP` 하나) 에 Enter 한 번 → 반환값 파일 기록, 표 C 와 비교. 닫기 (×) · Esc 도 (`MB_YESNO` 는 안 닫혀야).
- **자식 순서**: `WM_INITDIALOG` 에서 `GW_CHILD` → `GW_HWNDNEXT` (+ `GW_HWNDPREV` 거꾸로) ID 찍기, `#32770` 메시지 상자도 (단추가 먼저인지).
- **대화상자 프로시저 메시지 차례**: `DLGPROC` 맨 앞에서 모든 `uMsg` 를 시각과 함께 — `WM_PAINT` 횟수 · `WM_ERASEBKGND` · `WM_NCACTIVATE` · `WM_SHOWWINDOW` · `WM_WINDOWPOSCHANGED` · 끝의 `WM_DESTROY` · `WM_NCDESTROY`, 모달 뒤 첫 `GetMessage` 의 `message = 0`.
- **A 판**: `DialogBoxIndirectParamA` 로 `IsWindowUnicode(hDlg)` · `GetWindowLongPtrA/W(GWLP_WNDPROC)`.
- **`GetUserDefaultLCID`** · `GetSystemDefaultLCID` · `GetThreadLocale` · `GetUserDefaultLocaleName` 콘솔 출력 (`0x409` 인지, `0x1000 → 0xc00` 갈래인지).
- **`SetWindowLongPtr(h, GWLP_USERDATA, v)` / `GetWindowLongPtr`** 왕복 · `GWLP_ID` 쓰기 뒤 `GetDlgCtrlID`.

정적 (결과서 "다음 요청 후보" — 대기열 자리는 오케스트레이터가 정함):

1. 대화상자 클래스 원자 (`#32770`) 가 `gpsi+0x364 ~ +0x388` 아홉 칸에 드는지 — 원자를 채우는 곳 (`xxxRegisterSystemClasses` 인지 `_index.tsv` 에서 이름 확인 필요). 표 A' B9 `[공백]`.
2. `xxxEndDeferWindowPosEx` 의 `SWP_HIDEWINDOW` · `SWP_NOACTIVATE` 갈래 (`WM_SHOWWINDOW` · 활성 바뀜 보내기) — 7절 `NtUserSetWindowPos` 칸 `[추측]`.
3. `xxxInternalDoPaint` — `WM_PAINT` 창 차례 · `WM_ERASEBKGND` / `WM_NCPAINT` 보내기 · 반복 조건 (3절 `[추측]`).
4. `xxxReadPostMessage` (`WM_QUIT` · 지움 · 거르기) · `xxxScanSysQueue` (입력 없을 때).
5. `ButtonWndProcWorker` 클릭 입구 (마우스 뗌 · 스페이스 · `BM_CLICK`) · `xxxPressButton`.
6. `gfDMREnable` · `gfEMIEnable` (user32 `0xda904` · `0xda930`) 를 정하는 곳.
7. `xxxDW_SendDestroyMessages` 의 `0x272` 가 무엇인지.

다른 프로젝트 몫: kernelbase `GetCurrentNlsCache` 안쪽 · NLS 사용자 정보 (`gpServerNlsUserInfo`) 를 채우는 csrss / basesrv 쪽 · imm32 `ImmTranslateMessage` (프로젝트 없음) · gdi32 쪽 (`MB_DlgProc` 색 · `SoftModalMessageBox` 글 폭 — run153 이 닫은 쪽).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함). 디버거 여부를 보는 줄은 이번에 읽은 길에 없었다 `[정적: 읽은 줄 안에서]`.
- **스크립트 문제 없음** (결과서). 읽을 때 조심할 것 (기록만, 하네스 제안 아님):
  - 결과서가 "갈래 줄" 을 바로 앞 `TEST` / `CMP` · `MOV` 줄로 적은 곳이 넷 (대조 1 · 3 · 4 · 6) — run148 과 같은 꼴. WinDbg 멈춤점엔 문제없지만 "어디서 갈라지나 · 어디서 부르나" 는 조건 점프 줄 · `CALL` 줄로 적자.
  - `InternalCreateDialog` 는 run146 · run148 때처럼 조각 (`frag:FUN_18001fbc1 +0x…`) 과 함수 기준 (`= InternalCreateDialog +0x…`) 이 한 줄에 같이 찍힌다. 결과서의 "조각 `+0x1341`" 은 함수 기준 값을 "조각" 이라 부른 것 (대조 5).
  - `win32kfull` `_calls.tsv` 의 `alias:<이름>+1` 은 PDB 꾸민 이름이 같은 주소에 하나 더 있다는 뜻일 뿐 별칭 무리가 아니었다 (`--lookup` 둘).
- 같은 때 다른 decompiler (sast/152 · 153 · 154) 와 explainer (run156 해설서) 가 일했다 — 이 문서는 그 파일을 안 건드렸다.

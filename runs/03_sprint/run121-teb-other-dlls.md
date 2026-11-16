# 2026-11-16 (작성일) run121 — `_TEB` "범위 밖 길 — 다른 DLL" 39칸 확인 (Q102, 정적만): **쓰는 줄 있음 23 · 못 찾음 16**, 짐작 틀림 둘 (`WinSockData` → **mswsock.dll**, `WOW32Reserved` → **KERNELBASE**), test.exe 초기화에서 타는 칸 **0** — KERNELBASE 5칸 중 `NlsCache` 는 DllMain 사슬로 `GetCurrentNlsCache` 까지 닿지만 쓰는 줄이 **`IsImpersonating ≠ 0` 갈래**에만 있어 안 탐 (사진 P2 0 과 맞음)

(글의 오프셋은 쓰는 줄 · 읽는 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 test.exe 가 **안 올리는 DLL** (gdi32full · user32 · opengl32 · mswsock · combase …) 과 KERNELBASE 의 초기화 밖 함수가 대부분이다. 초기화 길에 걸리는 것은 이미 그림에 있는 상자 (`KERNELBASE!_KernelBaseBaseDllInitialize` → `BaseNlsDllInitialize`) 안쪽 한 갈래뿐이다. 대표 지점 규칙으로 노드를 늘리지 않았다 — **그림은 안 바꿨다.** 글 (flow.md 2.5 · 4장 · 5장) 과 짐 지도 (3.3 · 새 3.3.1 · 7장 · 8장) 에만 넣었다.

## 요약

- **39칸을 칸마다 확인했다** `[정적]` — System32 의 x64 PE 3,881개를 `objdump` 로 풀어 "TEB 주소 (`GS:[0x30]`) 에서 그 칸으로 쓰는 줄" 을 모두 찾았다 (결과서 방법).
  - **쓰는 줄 있음 23**: 짐작 DLL 이 맞음 19 · 짐작 DLL 도 쓰지만 주인은 다른 DLL 2 (`GdiTebBatch` · `GdiBatchCount` — 주로 **gdi32full**) · **짐작이 틀림 2** (`WinSockData` ws2_32 → **mswsock.dll**, `WOW32Reserved` wow64cpu → **KERNELBASE**).
  - **쓰는 줄 못 찾음 16**: `ActiveRpcHandle` · `User32Reserved` · `UserReserved` · `GdiCachedProcessHandle` · `GdiClientPID` · `GdiClientTID` · `GdiThreadLocalInfo` · `glSection` · `glTable` · `EtwTraceData` · `TxnScopeEnterCallback` · `TxnScopeExitCallback` · `TxnScopeContext` · `ReservedForPerf` · `ReservedForCodeCoverage` · `pShimData`. 그중 13칸은 읽는 줄도 없다. "못 찾음" 이지 "없음 증명" 은 아니다 (아래 "검색 한계").
- **test.exe 초기화에서 타는 칸 0** `[정적 + 관찰]`:
  - 사진 P2 (`NtContinue` 입구) 모듈은 6개뿐 — test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2 (`ms/logs/04_baggage-read_P2.txt:3397 ~ 3923`) `[관찰]`. 그러니 쓰는 줄 있는 23칸 중 18칸 — 안 올리는 DLL 몫 16 · 다른 프로세스 (csrss) · 커널 몫 2 — 은 처음부터 안 탄다.
  - 올라오는 DLL 안에서 쓰는 칸은 **KERNELBASE 5칸** (`StaticUnicodeBuffer` · `GuaranteedStackBytes` · `SavedPriorityState` · `WOW32Reserved` · `NlsCache`). 넷은 초기화 길에서 직접 호출로 닿지 않는다.
  - `NlsCache` 만 닿는다: `_KernelBaseBaseDllInitialize +0x4e1 BaseNlsDllInitialize` → … → `GetCPHashNode +0x4c6 GetCurrentNlsCache` / `+0x13e CheckSpecialLocales`. 그런데 쓰는 줄 (`GetCurrentNlsCache +0x1021` 등) 은 TEB `IsImpersonating` (`+0x179c`) 이 **0 이 아닐 때** 가는 갈래 안이다 (`+0x3d` 읽기 → `+0x45 JNZ`). 초기화 땐 그 값이 0 (사진 P2 0) → 안 탄다.
  - ntdll · kernel32 는 39칸 중 쓰는 칸이 하나도 없다 → "다른 DLL" 이라는 이름이 맞다.
- **"초기화 동안 0" 은 그대로** (run44 사진 K2 ~ P2 일곱 장).
- 짐 지도: **2장 숫자 그대로.** 3.3 "다른 DLL 39" 줄을 "쓰는 줄 23 · 못 찾음 16" 으로 고치고, 칸마다 표 **3.3.1** (39줄, 줄 수에 안 셈) 을 새로 뒀다. 같이 고친 줄: `IsImpersonating` (KERNELBASE NLS 10 함수가 씀 — 빠져 있었음), `StaticUnicodeString` (`FatalAppExitA +0x49` 은 이 칸 — 정정).
- **PE_emulator** (결과서가 본 줄, flow-writer 는 코드 안 읽음): **맞음** — 에뮬레이터는 이 39칸을 안 건드려 0, 사진과 같다. KERNELBASE NLS 코드를 진짜로 돌릴 때 `IsImpersonating == 0` 이 유지되는지만 **확인 필요**.

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: Ghidra analyze · 새 export **안 함**. `objdump -d -M intel` 로 System32 의 `*.dll *.exe *.ocx *.cpl *.ax *.drv` 3,916개를 풀었다 (x64 3,881개, .NET 35개는 건너뜀). 커널 쪽은 `win32kbase.sys` · `win32kfull.sys` · `win32k.sys` 셋만 따로 (결과서).
- 이름: 이미 `_index.tsv` · PDB 가 있던 KERNELBASE · kernel32 · ntdll · user32 · gdi32 · gdi32full · rpcrt4 · csrsrv · basesrv · ucrtbase 는 그대로. **공개 이름이 꼭 필요한 12개만 PDB 를 새로 받음** (`fetch_pdb.py`, analyze 안 함): advapi32 · combase · mswsock · opengl32 · imm32 · ws2_32 · sechost · wow64cpu · WUDFHost.exe · regsvc · CoreMessaging · ttdrecordcpu (결과서).
- 버전 대조 (결과서): 훑기가 낸 "함수 시작 + 오프셋" 이 `_index.tsv` 함수 시작과 모두 같고 (`+0x0`), `.asm` 이 있는 함수 (`GetCurrentNlsCache` · `CheckSpecialLocales` · `CsrpHandleInlineUserConnect` · `CsrApiRequestThread` · `ClientThreadSetup` · `_KernelBaseBaseDllInitialize`) 의 줄과 맞다. sast/48 의 KERNELBASE 줄도 같은 오프셋으로 다시 나옴.
- flow-writer 가 `.asm` 으로 다시 본 줄: `GetCurrentNlsCache +0x3d` (`MOV ECX,[RAX+0x179c]`) · `+0x45 JNZ` · `+0x64e SUB ECX,1` · `+0xa60` (`[RAX+0x17a0]` 읽기) · `+0xbee` (`MOV [RCX+0x179c],R12D`) · `+0xfcc` · `+0x1021` · `+0x1096` (`MOV [RAX+0x17a0],RDI`), `GetCPHashNode +0x13e` · `+0x4c6` CALL, `_KernelBaseBaseDllInitialize +0x4e` (`CMP EDX,2`) · `+0x80` · `+0x9e` (`[+0x1768]` 읽기) · `+0xdf` · `+0x4e1` CALL — 결과서와 같다.
- 다른 decompiler 가 같은 때 ntdll (sast/116) · ntoskrnl (sast/118) 을 돌렸다 — 이번엔 둘 다 읽기만.
- 임시 도구는 결과서가 따로 둔 **도구 폴더** (`_workspace/sast/117_teb-other-dlls_tools/`) 에 있다. 커밋할지는 사용자 결정 대기 — 이 문서의 사실은 결과서만 근거로 한다.

## 목적

- run54 (sast/48) 이 칸 이름만 보고 "쓰는 DLL" 을 짐작한 `_TEB` 39칸 (`[추측]`) 을 줄로 확인한다 (Q102):
  1. 칸마다 짐작한 DLL 에 쓰는 줄이 정말 있나, 틀렸으면 실제 DLL 은 어디인가.
  2. 그 함수가 test.exe 초기화 동안 불리나 (올라오는 DLL 은 KERNEL32 · KERNELBASE 뿐).
  3. 짐 지도 3.3 "다른 DLL 39" 를 줄 단위 (칸 · 쓰는 DLL · 함수 · test.exe 에서 타나) 로 바꿀 수 있게.

## 방법

- 정적: `_workspace/sast/117_teb-other-dlls.md` — System32 전체 `objdump` 훑기 (요청서 "가벼운 쪽 먼저").
  - `MOV REG,GS:[0x30]` (TEB) 에서 시작해 `MOV` · `LEA` · `ADD` 로 따라가고, 호출 뒤엔 휘발 레지스터를 버리고, 앞쪽 분기는 상태를 합쳤다. `GS:[칸]` 직접 접근도 같이. 39칸 오프셋 범위 (`pdb_struct.py ntdll.dll _TEB`) 에 드는 접근만 **쓰기 / 읽기 / 주소 꺼내기 (`LEA`)** 로 나눴다.
  - 함수 이름: `.pdata` 함수 시작 → `_index.tsv` 이름, 없으면 PDB 공개 이름.
  - 커널 (`Win32ThreadInfo` · `Gdi*` 의 커널 쓰기): win32k 셋만, `GS:[0x188]` (현재 스레드) → `+0xf0` (TEB) 로 따라감.
  - "test.exe 에서 타나": ① 사진 P2 모듈 목록 `[관찰]` ② KERNELBASE · kernel32 직접 호출 그래프에서 DllMain 입구 · `_KernelBaseBaseDllInitialize` 의 **PROCESS_ATTACH 갈래만** (`+0x195` 부터) · kernel32 입구 · `BaseThreadInitThunk` 에서 닿는지 (결과서).
- 결과서: `_workspace/sast/117_teb-other-dlls_result.md` (**분석성공** — 스크립트 문제 없음).
- 사진 값: `ms/logs/04_baggage-read_P2.txt` `WOW32Reserved` 2697 · `StaticUnicodeBuffer` 2738 · `GuaranteedStackBytes` 2761 · `SavedPriorityState` 2767 · `IsImpersonating` 2774 · `NlsCache` 2775 (flow-writer 가 로그로 다시 맞춤 — 아래 "도구 메모") · 모듈 목록 `:3397 ~ 3923`.
- 앞 바퀴 짝: run54 (sast/48 — 짐작을 만든 바퀴), run85 (sast/82 — user32 `ClientThreadSetup` 의 `Win32ClientInfo` 줄), run86 (sast/83 — gdi32 · gdi32full `DllMain` 에 `Gdi*` 쓰기 없음), run68 · run71 (KERNELBASE NLS 초기화 사슬).

## 결과

비유 (TEB = 스레드마다 하나씩 받는 **사물함**):

- 사물함엔 칸이 137개 있다. 그중 39칸에는 "그래픽 팀 칸", "네트워크 팀 칸" 처럼 **다른 팀 이름표**가 붙어 있다.
- run54 는 이름표만 보고 "이 칸은 그래픽 팀이 쓰겠지" 라고 짐작했다.
- 이번엔 회사 전체 (System32) 의 일지를 다 뒤져 **누가 실제로 그 칸에 물건을 넣는지** 찾았다. 23칸은 넣는 사람을 찾았고, 16칸은 아무도 넣는 걸 못 봤다.
- test.exe 는 그래픽 · 네트워크 팀을 아예 안 부른다. 그래서 그 칸들은 처음 받은 빈 상태 (0) 그대로다.

### 1. 39칸 — 칸 · 쓰는 DLL · 함수 · test.exe 에서 타나 (결과서 1장)

O = 쓰는 줄 있음, X = 못 찾음. 함수 뒤 괄호는 함수 시작 rva. "안 올림" = P2 모듈 6개에 없음 `[관찰]`.

| # | 칸 | 짐작 (sast/48) | 실제 | 쓰는 자리 (대표) | test.exe |
|---|---|---|---|---|---|
| 1 | `+0x50` `ActiveRpcHandle` | rpcrt4 | **X** (읽는 줄도 없음 — rpcrt4 는 `ReservedForNtRpc` 를 씀) | — | 해당 없음 |
| 2 | `+0x70` `CsrClientThread` | CSR | O csrsrv · basesrv (csrss.exe 안) | csrsrv `CsrConnectToUser +0x9b` (0x1470) · `CsrpHandleInlineUserConnect +0x6c · +0x93` · `CsrProcessLazyRegister +0xc6` · `CsrApiRequestThread +0x98` (`LEA`); basesrv `BaseSrvRegisterThread +0x10e` | 안 탐 — csrss 자기 스레드 TEB |
| 3 | `+0x78` `Win32ThreadInfo` | win32kbase | O **커널만** | win32kbase.sys `xxxDestroyThreadInfo +0x104` (0xb2c70, 정리 쪽). **만드는 줄 못 찾음 `[공백]`**. 유저 DLL 은 읽기만 (user32 53 · imm32 13 · gdi32 2 — 결과서 숫자) | 안 탐 — GUI 초기화 없음 |
| 4 | `+0x80` `User32Reserved` | user32 | **X** (wow64.dll 의 `[+0xc0]` · `[+0xc4]` 쓰기는 `WowTebOffset` 을 더한 **TEB32** 칸이라 뺌) | — | 해당 없음 |
| 5 | `+0xe8` `UserReserved` | user32 | **X** | — | 해당 없음 |
| 6 | `+0x100` `WOW32Reserved` | wow64cpu | O **KERNELBASE** (짐작 틀림) | `Wow64SetThreadDefaultGuestMachine +0x33` (0xc50f0, `MOV [RCX+0x100],RBX`) | 안 탐 — 직접 호출 사슬 없음, P2 null |
| 7 | `+0x110` `ReservedForDebuggerInstrumentation` | 이름만 | O ttdrecordcpu.dll (TTD 기록기) | `TTD::ThreadInfo::GetOrCreateForCurrentThread +0x108 · +0x10e` (0xa044, 값 `'TTD1'` + 포인터) | 안 올림 |
| 8 | `+0x2f0` `GdiTebBatch` | gdi32 | O **gdi32full 주** · gdi32 일부 · 커널 win32kfull | gdi32full 8 함수 (`PatBlt +0x28a` (0x22db0) · `ExtTextOutWImpl +0x382` · `SelectObjectImpl +0x43d` …), gdi32 `InternalDeleteObject +0x268` · `DeleteRegion +0xaf`, win32kfull `GdiThreadCalloutFlushUserBatch +0x4c` | 안 올림 |
| 9 ~ 12 | `+0x7e8` `GdiCachedProcessHandle` · `+0x7f0` `GdiClientPID` · `+0x7f4` `GdiClientTID` · `+0x7f8` `GdiThreadLocalInfo` | gdi32 | **X** 넷 | — | 해당 없음 |
| 13 | `+0x800` `Win32ClientInfo` (496B) | user32 | O **user32 주** · gdi32full · imm32 · CoreMessaging · (csrsrv) | user32 36 함수 (`ClientThreadSetup +0xd3` — run85 · `PeekMessageW +0x96` · `DispatchMessageWorker +0x2d7` …), gdi32full 42 함수 (`+0x808` 칸 — `BitBlt +0x94` …), imm32 8 함수 (`CtfImmTIMActivate +0xf8` …), CoreMessaging `EnsureSharedTebInitialized<…> +0x48` (`+0x9e8`), csrsrv `CsrApiRequestThread +0x65` | 안 올림 |
| 14 | `+0x9f0` `glDispatchTable` | opengl32 | O opengl32 (표 주소를 복사 호출 목적지로 넘김) | `SetCltProcTable +0xe7` · `MakeAnyCurrent +0x283` · `bMakeNoCurrent +0x121` · `vInitTebCache +0x28` | 안 올림 |
| 15 | `+0x1138` `glReserved1` | opengl32 | O opengl32 | 69 함수 (대부분 `glclt*`, `GLInitializeThread +0x75` · `MakeAnyCurrent +0x3d3` …) | 안 올림 |
| 16 | `+0x1220` `glReserved2` | opengl32 | O opengl32 | `GLInitializeThread +0x15d` · `FreePolyMaterial +0x85` · `PAMatAlloc +0xb0` | 안 올림 |
| 17 | `+0x1228` `glSectionInfo` | opengl32 | O opengl32 | `glsrvLoseCurrent +0xbe` · `glsrvMakeCurrent +0x193 · +0x568` · `__glDestroyContext +0x39 · +0x2d6` | 안 올림 |
| 18 · 19 | `+0x1230` `glSection` · `+0x1238` `glTable` | opengl32 | **X** 둘 | — | 해당 없음 |
| 20 | `+0x1240` `glCurrentRC` | opengl32 | O opengl32 | `MakeAnyCurrent +0x1da` · `bMakeNoCurrent +0x58` · `GLInitializeThread +0x14c` · `GLUnInitializeThread +0x126` · `MetaRcBegin` · `MetaRcEnd` | 안 올림 |
| 21 | `+0x1248` `glContext` | opengl32 | O opengl32 | `GLInitializeThread +0x16e` · `wglCbSetCurrentValue +0x9` (pdata 없는 자리, PDB 가장 가까운 이름) | 안 올림 |
| 22 | `+0x1268` `StaticUnicodeBuffer` (522B) | KERNELBASE | O KERNELBASE | `BaseCopyStream` (0xd4644, `+0x39e LEA` · `+0x6c87 ~` 쓰기) · 조각 `BaseCopyStream$fin$0 +0x414 ~ +0x46d`. 복사 중 임시 칸으로 쓰고 되돌림 `[추측]` | 안 탐 — 직접 호출 사슬 없음, P2 `""` |
| 23 | `+0x1698` `ReservedForNtRpc` | rpcrt4 | O rpcrt4 | `ThreadSelf +0x2dc` (0x52930) · `_InitializeDLL +0x30` (0x91f50, DllMain 때 0) | 안 올림 |
| 24 | `+0x1728` `PerflibData` | advapi32 / sechost | O **advapi32** (+ regsvc, sechost 는 X) | advapi32 `PerflibciQueryV2Provider +0x623` (값 `'Perf'`) · regsvc 같은 이름 `+0x1fa` | 안 올림 |
| 25 | `+0x1730` `EtwTraceData` | advapi32 / sechost | **X** | — | 해당 없음 |
| 26 | `+0x1738` `WinSockData` | ws2_32 | O **mswsock.dll** (짐작 틀림 — ws2_32 는 줄 없음) | `SockWaitForSingleObject +0x72 · +0x18e · +0x290 · +0x2bf` (0x93c0) · `AllocateWinsockThreadData +0x52` (0xcd20) | 안 올림 |
| 27 | `+0x1740` `GdiBatchCount` | gdi32 | O **gdi32full 주** · gdi32 · 커널 win32kfull | 8번과 같은 함수들 (`PatBlt +0x2c5` …), gdi32 `InternalDeleteObject +0x24f` · `DeleteRegion +0x96`, win32kfull `GdiThreadCalloutFlushUserBatch +0x3a` (= 0) | 안 올림 |
| 28 | `+0x1748` `GuaranteedStackBytes` | KERNELBASE | O KERNELBASE | `SetThreadStackGuarantee +0x120` (0xf22d0) · `SwitchToFiberContext +0x112` (0x196f70) | 안 탐 — 직접 호출 사슬 없음, P2 0 |
| 29 | `+0x1750` `ReservedForPerf` | 이름만 | **X** | — | 해당 없음 |
| 30 | `+0x1758` `ReservedForOle` | ole32 / combase | O **combase** (ole32 · coml2 는 읽기만) | 10 함수 — `GetCurrentComApartment +0xc7` · `COleTls::TLSAllocData +0xe2` · `InternalTlsAllocData +0x30` … | 안 올림 |
| 31 | `+0x1768` `SavedPriorityState` | KERNELBASE | O KERNELBASE | `SetThreadPriority +0x1b7 · +0x271` (0xc6660). 읽기 `_KernelBaseBaseDllInitialize +0x80 · +0x9e` (THREAD_DETACH 갈래) | 안 탐 — 직접 호출 사슬 없음, P2 null |
| 32 | `+0x1770` `ReservedForCodeCoverage` | 이름만 | **X** | — | 해당 없음 |
| 33 | `+0x17a0` `NlsCache` | KERNELBASE | O KERNELBASE (10 함수, 쓰기 15줄) | `GetCurrentNlsCache +0x1021 · +0x1096` (0x52350) · `CheckSpecialLocales +0x1283 · +0x12f8` (0x14730) · `InternalLcidToName` · `CompareStringW +0xcc7` · `GetSortNode` · `CompareStringA` · `GetStringTypeExW` · `NlsValidateLocale` · `GetLocaleInfoHelper` — 모두 `RtlAllocateHeap(0x698)` → 채움 → TEB 에 놓기 | **조건부, 초기화 땐 안 탐** (아래 2절) |
| 34 | `+0x17a8` `pShimData` | apphelp | **X** (apphelp 도 안 읽음) | — | 해당 없음 |
| 35 ~ 37 | `+0x17f0` `TxnScopeEnterCallback` · `+0x17f8` `TxnScopeExitCallback` · `+0x1800` `TxnScopeContext` | ole32 / combase | **X** 셋 (읽기만: ntdll `TppWorkerThread +0x8bd · +0x9cf` · `TppCallbackCheckThreadAfterCallback +0x1c7` · verifier.dll) | — | 해당 없음 |
| 38 | `+0x1818` `ReservedForWdf` | 이름만 | O WUDFHost.exe (UMDF 호스트) | `CWudfIoDispatcher::MarkUmdfDriverInitiatedRequest +0x8a` · `CWudfDefaultDispatcher::DoRead +0x261 · +0x2b8` · `UnmarkUmdfDriverInitiatedRequest` (TEB 를 인자로 받음) | 안 올림 |
| 39 | `+0x1820` `ReservedForCrt` | 이름만 | O ucrtbase.dll | `__crt_state_management::enter_os_call +0x22` (= 1) · `leave_os_call +0x22` (= 0) · `enter_os_call_inline +0x11` | 안 올림 (sample 은 `/MT` 정적 CRT `[소스]`) |

- 칸 수 대조: 39줄 (9 ~ 12 · 18 · 19 · 35 ~ 37 묶음 포함) = O 23 + X 16 (결과서).
- 함수 rva · 모든 `+0x` 는 결과서 1장. 짐 지도 3.3.1 에 같은 표를 칸마다 옮겼다.

### 2. KERNELBASE 5칸 — test.exe 초기화에서 타나 (결과서 3장)

사진 P2 값 (`ms/logs/04_baggage-read_P2.txt`) `[관찰]`: `WOW32Reserved` null (2697) · `StaticUnicodeBuffer` `""` (2738) · `GuaranteedStackBytes` 0 (2761) · `SavedPriorityState` null (2767) · `IsImpersonating` 0 (2774) · `NlsCache` null (2775). P2 는 모든 `DllMain` 뒤다.

| 쓰는 함수 (칸) | 초기화 길에서 직접 호출로 닿나 `[정적]` | 결론 |
|---|---|---|
| `BaseCopyStream` · `$fin$0` (`StaticUnicodeBuffer`) | 안 닿음 | 안 탐 |
| `SetThreadStackGuarantee` · `SwitchToFiberContext` (`GuaranteedStackBytes`) | 안 닿음 | 안 탐 |
| `SetThreadPriority` (`SavedPriorityState`) | 안 닿음 (`CtrlRoutine` 로 가는 주소 꺼냄 줄만) | 안 탐 |
| `Wow64SetThreadDefaultGuestMachine` (`WOW32Reserved`) | 안 닿음 | 안 탐 |
| `InternalLcidToName` · `CompareStringW` · `CompareStringA` · `GetSortNode` · `GetStringTypeExW` · `GetLocaleInfoHelper` (`NlsCache`) | 안 닿음 | 안 탐 |
| `NlsValidateLocale` (`NlsCache`) | 닿음 — 단 `TipInitialize` (DllMain 입구에서 부름) 아래 JSON 해석 → `WideCharToMultiByte` → `NlsValidateLocale` 사슬. TIP 시험용 갈래, 켜지는 조건 `[공백]` | 사진 0 이라 안 탐 `[관찰 간접]` |
| **`GetCurrentNlsCache` · `CheckSpecialLocales`** (`NlsCache`) | **닿음** — 아래 호출 관계 | 쓰는 줄이 **`IsImpersonating ≠ 0` 갈래**에만 있다 → 초기화 땐 안 탐 `[정적 + 관찰]` |

### 정적 — 호출 지점 (`ms/disassemble/kernelbase/*.asm` 의 `CALL ret=` 줄)

| caller | call | ret | callee | 뜻 | 표시 |
|---|---|---|---|---|---|
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x4e1` | `+0x4e6` | `KERNELBASE!BaseNlsDllInitialize` | PROCESS_ATTACH 갈래 NLS 준비 (run68) | [정적] |
| `KERNELBASE!GetCPHashNode` | `+0x4c6` | `+0x4cb` | `KERNELBASE!GetCurrentNlsCache` | `SetupAnsiOemCodeHashNodes` 아래 코드 페이지 노드 찾기 안 | [정적] |
| `KERNELBASE!GetCPHashNode` | `+0x13e` | `+0x143` | `KERNELBASE!CheckSpecialLocales` | 같은 함수 안 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0xdf` | `+0xe4` | `KERNELBASE!SetThreadLocale` | **THREAD_ATTACH 갈래** (`+0x4e CMP EDX,2`) 의 콘솔 앱 전용 — 첫 스레드는 PROCESS_ATTACH 라 안 탐 | [정적] |

- `BaseNlsDllInitialize` → `NlsProcessInitialize` → `SetupAnsiOemCodeHashNodes` → `GetCPHashNode` 사이 call 자리는 결과서에 이름 사슬로만 있다 (flow.md 2.5 의 `+0x1c NlsProcessInitialize` · run117 의 `GetCPHashNode +0x2d3` 와 같은 줄기).
- `GetCPHashNode +0x4c6` 가 초기화에서 **실제로** 불리는 갈래인지 (`IsUnspecifiedLcid` 쪽) 는 아직 안 봤다 — 정적 후보 ③.

### 호출 관계

```
[② test.exe 첫 스레드 — KERNEL32 준비 안, P1a ~ P1b]
KERNELBASE!_KernelBaseBaseDllInitialize        (+0x4e CMP EDX,2 — PROCESS_ATTACH 갈래만, +0x195 부터)
 └─ +0x4e1  BaseNlsDllInitialize
             └─ NlsProcessInitialize → SetupAnsiOemCodeHashNodes → GetCPHashNode
                  ├─ +0x13e  CheckSpecialLocales      쓰기 +0x1283 · +0x12f8 [TEB+0x17a0]   ← 갈래 밖
                  └─ +0x4c6  GetCurrentNlsCache
                               ├─ +0x3d   ECX ← [TEB+0x179c] IsImpersonating
                               ├─ +0x45   JNZ +0x64e        ← 0 이 아닐 때만 (초기화 땐 0 → 안 탐)
                               │            +0x64e SUB ECX,1 · CMP ECX,1 …
                               │            +0xa60 [TEB+0x17a0] 읽기 → 0 이면 +0xfcc 할당
                               │            +0x1021 · +0x1096  [TEB+0x17a0] ← 새 블록 (NlsCache)
                               └─ (0 이면) 다음 줄로 — 이 갈래엔 TEB NlsCache 쓰기 없음 (결과서)

(안 타는 갈래)
 _KernelBaseBaseDllInitialize +0xdf SetThreadLocale        THREAD_ATTACH · 콘솔 앱
 _KernelBaseBaseDllInitialize +0x80 · +0x9e [TEB+0x1768]    THREAD_DETACH (RtlCleanUpTEBLangLists 뒤) — 읽기
```


### 3. 곁에서 알게 된 것 (결과서 4장)

- **`IsImpersonating` (`+0x179c`) 을 쓰는 곳에 KERNELBASE NLS 10 함수가 빠져 있었다** `[정적]` — `InternalLcidToName +0x985` · `CheckSpecialLocales +0xdf6` · `CompareStringW +0xa2b` · `GetSortNode +0x1367` · `CompareStringA +0xd14` · `GetUserOverrideString +0x749` · `GetStringTypeExW +0xad4` · `GetCurrentNlsCache +0xbee` · `NlsValidateLocale +0x940` · `GetLocaleInfoHelper +0xc00`. 값 2 (`MOV R12D,2` → `[TEB+0x179c]`) 또는 되돌림. 뜻은 "스레드별 NLS 캐시를 쓰는 중" 표시로 보인다 `[추측]`. sast/48 은 nt · ntdll 쓰기만 적었다. `GetCurrentNlsCache` · `CheckSpecialLocales` 는 초기화 사슬에 있지만 갈래를 안 탄다 (사진 0).
- **`NlsCache` 를 읽고 쓰는 함수** (`GetCurrentNlsCache`) 는 초기화 사슬에 있다 (`GetCPHashNode +0x4c6` 아래) — **함수 단위** 이야기다 (결과서 4.5 "읽는 쪽도 DllMain 사슬에 있다"). **줄 단위**로는 `NlsCache` 를 읽는 `+0xa60` 도 쓰는 줄과 같이 `+0x45 JNZ +0x64e` 뒤 `IsImpersonating ≠ 0` 갈래에 있다 (결과서 3장 사슬, 위 호출 관계 트리). 그 갈래 앞에서 TEB 를 읽는 줄은 `+0x3d` (`IsImpersonating`) 다. 0 갈래도 `+0x64e` 뒤쪽 주소로 뛰어드는 줄이 있어 (`+0x58 JZ` 등), `+0xa60` 이 0 갈래에서 닿는지는 줄 단위로 따져 보지 않았다 `[공백]`.
- **`FatalAppExitA +0x49` 은 `StaticUnicodeString` (`+0x1258`) 줄이다** — `LEA RCX,[RBX+0x1258]` (칸 주소를 넘김). sast/48 · 짐 지도는 `StaticUnicodeBuffer` (`+0x1268`) 의 쓰는 자리로 적었다 → 정정.
- ntdll 에서 훑기가 `glDispatchTable` 범위로 잡은 5줄 (`RtlpCheckRelativeDrive +0x248 · +0x257` `[+0xf28]` 등) 은 `WowTebOffset` 을 더한 TEB32 칸 (`0xf28` = 32비트 `HardErrorMode` 자리) 으로 보여 뺐다 `[추측]` (결과서).

## 확인된 것

- `[정적]` 39칸 중 쓰는 줄이 있는 칸 23, 3,881개 x64 PE 에서 쓰는 줄을 못 찾은 칸 16 (결과서 1장).
- `[정적]` `WinSockData` 의 쓰는 DLL 은 **mswsock.dll** (`SockWaitForSingleObject` · `AllocateWinsockThreadData`), ws2_32 는 이 칸을 읽지도 쓰지도 않는다.
- `[정적]` `WOW32Reserved` (x64 TEB) 의 쓰는 곳은 **KERNELBASE `Wow64SetThreadDefaultGuestMachine +0x33`** 하나. wow64cpu 는 x64 칸을 안 만진다.
- `[정적]` `GdiTebBatch` · `GdiBatchCount` 는 **gdi32full** 이 주로 쓰고, gdi32 는 `InternalDeleteObject` · `DeleteRegion` 둘만. 커널 win32kfull `GdiThreadCalloutFlushUserBatch` 가 비운다. 나머지 `Gdi*` 넷은 쓰는 줄 없음.
- `[정적]` `Win32ThreadInfo` 는 유저 DLL 이 쓰지 않는다 (user32 · imm32 · gdi32 는 읽기만). 커널 쪽은 정리 줄 (`xxxDestroyThreadInfo +0x104`) 만 찾음 — run85 "user32 는 안 쓴다" 와 맞다.
- `[정적]` ntdll · kernel32 는 39칸 중 쓰는 칸이 없다.
- `[관찰]` test.exe 는 P2 까지 모듈 6개만 올린다 (`_P2.txt:3397 ~ 3923`) → 다른 DLL 몫은 처음부터 안 탄다.
- `[정적 + 관찰]` KERNELBASE 5칸은 초기화 동안 안 쓰인다 — 넷은 호출 사슬이 없고, `NlsCache` 는 사슬은 닿지만 쓰는 줄이 `IsImpersonating ≠ 0` 갈래 안. 사진 P2 다섯 칸 모두 0 / null / `""`.

## 정정

- **run54** (`runs/03_sprint/run54-teb-fill.md` "범위 밖 길에서 씀 59" 표의 "다른 DLL (39)" 칸 — sast/48 짐작을 옮긴 것):
  - `ws2_32 1` → **mswsock.dll** (`WinSockData`).
  - `WOW64 1 (WOW32Reserved)` → **KERNELBASE** (`Wow64SetThreadDefaultGuestMachine +0x33`).
  - `GDI 6` → `GdiTebBatch` · `GdiBatchCount` 둘만 쓰는 줄 있음 (gdi32full 주), 넷은 못 찾음.
  - 16칸 (`ActiveRpcHandle` · `User32Reserved` · `UserReserved` · `Gdi*` 넷 · `glSection` · `glTable` · `EtwTraceData` · `TxnScope*` 셋 · `ReservedForPerf` · `ReservedForCodeCoverage` · `pShimData`) 은 짐작 DLL 에도, System32 어디에도 쓰는 줄이 없다.
  - run54 문서 맨 위에 한 줄 넣었다.
- **run86** (`runs/03_sprint/run86-gdi32-init.md` ⑧ "짐 지도 3.3 의 `gdi32 6 (Gdi*)` 짐작은 그대로 `[추측]`"): 그 짐작은 **둘만 맞고 (주인은 gdi32full), 넷은 쓰는 줄 없음**. run86 이 본 "`DllMain` 줄기에는 `Gdi*` 쓰기가 없다" 는 그대로 맞다. run86 문서 맨 위에 한 줄 넣었다.
- **짐 지도 3.3** (run54 가 옮긴 sast/48 줄): `StaticUnicodeBuffer` 의 쓰는 자리 `FatalAppExitA +0x49` → `StaticUnicodeString` (`+0x1258`) 줄. `IsImpersonating` "범위 밖에서도 씀" 에 KERNELBASE NLS 10 함수 보탬. 짐 지도 줄에서 고쳤다 (sast/48 결과서는 그대로 둠).

## 아직 확인 필요

### 셈 결정 (오케스트레이터 · 사용자)

- **X 16칸을 어디에 셀까.** 지금은 "범위 밖 길에서만 씀 — 다른 DLL 39" 안에 "쓰는 줄 못 찾음 16" 으로 두었다 (2장 숫자 그대로). 성격은 "아무도 안 씀" 과 같다 (결과서 4.2 — 옛 칸 · 계측 칸 · 이름만 있는 칸). 옮기면 3.3 "아무도 안 씀 28 → 44 · 범위 밖 59 → 43 (다른 DLL 39 → 23)", 2장 위 "아무도 안 씀 (표에 열 없음)" 유저 · "범위 밖" 유저 합도 같이 바뀐다 ("0 그대로 (사진)" 112 와 2장 표 칸은 그대로).

### 동적 (모두 낮음 — 사진 P2 가 이미 0 으로 답함. dast 요청 필요 없음, 결과서 5장)

- **D1** 초기화 중 `GetCurrentNlsCache` 가 불리고 어느 갈래로 도나 — `bp KERNELBASE!GetCurrentNlsCache` (test.exe 조건) → `dd @$teb+0x179c L1` · `dq @$teb+0x17a0 L1` · `k` (`GetCPHashNode+0x4cb` 에서 왔나). 사진으로는 호출 여부를 못 봄.
- **D2** TEB `+0x179c` · `+0x17a0` 에 초기화 중 쓰기가 한 번도 없나 (쓰고 되돌림 포함) — `ba w4 @$teb+0x179c` · `ba w8 @$teb+0x17a0` 를 `LdrpInitializeProcess` 입구부터 `NtContinue` 까지, hit 수. 쓰고 되돌림은 사진에 안 남는다.

### 정적

- ① (낮음) `Win32ThreadInfo` 를 유저 TEB 에 **놓는** 줄 — 후보 이름 win32kbase `xxxCreateThreadInfo` (rva `0x194d84`, 안 풂) `[공백]`. `objdump` 훑기는 TEB 를 인자로 넘기는 꼴을 못 잡는다. 필요하면 win32kbase Ghidra analyze 한 번. test.exe 는 GUI 를 안 올려 급하지 않음.
- ② (낮음) X 16칸 다시 보기 — TEB 포인터를 인자 · 스택 · 구조체로 넘긴 뒤 쓰는 꼴 (예: `UnmarkUmdfDriverInitiatedRequest`) 은 이 훑기가 놓칠 수 있다. 그리고 SysWOW64 · .NET 35개 · win32k 셋 밖 드라이버는 안 봤다.
- ③ (낮음) `GetCPHashNode +0x4c6` 가 초기화에서 실제로 불리는 갈래인지 (`IsUnspecifiedLcid` 쪽) — 새 칸은 없을 것 (짐 지도 7.6 NLS 칸에 이미 줄).
- ④ (낮음) `NlsValidateLocale` 로 가는 `TipInitialize` 갈래가 켜지는 조건 `[공백]`.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- **맞음**: 에뮬레이터 `Src/nt/Process.cpp` 의 `_FillTeb` (`MmCreateTeb` 13줄) · `_WriteSameTebFlags` 만 TEB 를 채우고 이 39칸은 0 — 사진 P2 와 같다 (결과서).
- **확인 필요**: 에뮬레이터가 KERNELBASE 를 진짜 코드로 돌리나. 돌리면 `GetCurrentNlsCache` 갈래가 `IsImpersonating == 0` 으로 가는지만 보면 된다 (D1 과 같은 질문).

## 디버거 메모 / 도구 메모

- 디버거가 붙어서 달라지는 점: 없음 (결과서).
- **결과서 3장의 사진 줄 번호 짝이 어긋나 있다** — 값 (0 / null / `""`) 은 맞는데, 칸 이름과 줄 번호를 서로 다른 순서로 적었다 (예: 결과서 "`NlsCache` null (2697)" → 로그 2697 은 `+0x100 WOW32Reserved`, `NlsCache` 는 2775). flow-writer 가 로그를 열어 이 문서 · 짐 지도 · flow.md 에 바른 짝으로 적었다. 결론은 안 바뀐다.
- 도구 (결과서 "도구 평가", 하네스는 flow-writer 가 안 고침):
  - 결과서가 임시 도구를 도구 폴더에 두었다 — System32 전체 TEB 칸 접근 훑기 · 함수 이름 붙이기 · KERNELBASE · kernel32 직접 호출 그래프 닿기. 커밋 여부는 사용자 결정 대기.
  - 처음판 (선형 추적) 은 `SetThreadPriority` 의 `SavedPriorityState` 쓰기를 놓쳤다 → 앞쪽 분기 상태 합치기 + 연결된 `.pdata` 조각은 초기화하지 않기를 넣고서야 잡혔다. `rex.W call` 같은 접두 줄을 접두로 안 읽으면 호출 뒤 레지스터 버리기가 빠져 틀린 줄이 생겼다.
  - 결과서 제안: `scripts/field_writers.py <바이너리|전체> --struct _TEB --fields <이름…>` (모듈 · 함수 · `+0x` · 읽기/쓰기/`LEA`) — sast/43 ~ 48 의 같은 제안을 이번 도구가 시제품으로 보여 줌. 남은 한계: 인자로 받은 TEB 따라가기, TEB32 구분.

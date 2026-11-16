# 117 teb-other-dlls — 정적 분석 결과

- 요청서: `_workspace/sast/117_teb-other-dlls.md` (Q102, `_TEB` "범위 밖 길 — 다른 DLL" 39칸)
- 방법: **objdump 만** (Ghidra analyze 안 함, 새 export 도 안 함). PDB 는 **이름이 꼭 필요한 12개만** 새로 받음 (`fetch_pdb.py`): advapi32 · combase · mswsock · opengl32 · imm32 · ws2_32 · sechost · wow64cpu · WUDFHost.exe · regsvc · CoreMessaging · ttdrecordcpu. (이미 PDB·`_index.tsv` 가 있던 KERNELBASE · kernel32 · ntdll · user32 · gdi32 · gdi32full · rpcrt4 · csrsrv · basesrv · ucrtbase 는 그대로 씀.) ntdll(sast/116)·ntoskrnl(sast/118) 은 건드리지 않음 (읽기만).
- 버전 대조: 훑기가 낸 "함수 시작 + 오프셋" 이 Ghidra `_index.tsv` 함수 시작 주소와 전부 같고 (`+0x0`), `.asm` 이 있는 함수 (`GetCurrentNlsCache` `+0x1021·+0x1096`, `CheckSpecialLocales` `+0x1283·+0x12f8`, `CsrpHandleInlineUserConnect` `+0x6c·+0x93`, `CsrApiRequestThread` `+0x65·+0x98`, `ClientThreadSetup` `+0xd3`, `_KernelBaseBaseDllInitialize` `+0x80·+0x9e·+0xdf`) 의 줄과 일치. 표의 핵심 쓰기 줄 30여 개는 objdump 원문 줄로 다시 대조함 (전부 일치). sast/48 의 KERNELBASE 줄 (`SetThreadStackGuarantee +0x120`, `SwitchToFiberContext +0x112`, `SetThreadPriority +0x1b7·+0x271`, `Wow64SetThreadDefaultGuestMachine +0x33`, `GetCurrentNlsCache +0x1021·+0x1096`, `CompareStringW +0xcc7`) 도 같은 오프셋으로 다시 나옴 → 스크립트와 바이너리 둘 다 믿을 수 있음.
- 스크립트 문제: 없음 (`.claude/` 스크립트는 안 썼고 안 고침). 임시 도구는 `_workspace/sast/117_teb-other-dlls_tools/` 에 따로 둠 (아래 "도구 평가").

## 한 줄 답

**39칸 중 쓰는 줄이 있는 칸 23 · 못 찾은 칸 16.** 그리고 test.exe 가 올리는 DLL (KERNEL32 · KERNELBASE) 안에서 쓰는 칸은 **KERNELBASE 5칸** 뿐이고, 그 5칸도 초기화 동안엔 안 탄다 (사진 P2 값 0 과 맞음). 나머지 18칸은 DLL 을 안 올리거나 (16) 다른 프로세스·커널 (2) 이라 test.exe 에서 안 탄다. → **"초기화 동안 0" 은 그대로.**

- 쓰는 줄 있음 23 = 짐작 DLL 이 맞음 19 · 짐작 DLL 도 쓰지만 주 DLL 이 다름 2 (`GdiTebBatch`·`GdiBatchCount`: gdi32 도 쓰지만 대부분 gdi32full) · **짐작이 틀림 2** (`WinSockData` ws2_32 → **mswsock.dll**, `WOW32Reserved` wow64cpu → **KERNELBASE**).
- 쓰는 줄 못 찾음 16: `ActiveRpcHandle` · `User32Reserved` · `UserReserved` · `GdiCachedProcessHandle` · `GdiClientPID` · `GdiClientTID` · `GdiThreadLocalInfo` · `glSection` · `glTable` · `EtwTraceData` · `TxnScopeEnterCallback` · `TxnScopeExitCallback` · `TxnScopeContext` · `ReservedForPerf` · `ReservedForCodeCoverage` · `pShimData`. (읽는 줄도 없는 칸이 13, `TxnScope*` 3칸은 ntdll `TppWorkerThread`·`TppCallbackCheckThreadAfterCallback` 과 verifier.dll 이 읽기만.)
- ntdll · kernel32 에는 39칸 중 쓰는 칸이 하나도 없다 (kernel32 `NtVdm64CreateProcessInternalW +0x95` 는 `StaticUnicodeBuffer` 주소만 꺼냄; 훑기가 ntdll 에서 `glDispatchTable` 범위로 잡은 5줄 — `RtlpCheckRelativeDrive +0x248 · +0x257 [+0xf28]` 등 — 은 `WowTebOffset` 을 더한 TEB32 칸 (0xf28 = `HardErrorMode` 자리) 으로 보여 뺌 `[추측]`). → 39칸을 ntdll 쪽으로 옮길 칸 없음.

## 방법 (objdump 전수 훑기)

1. `ms/bin/.../System32/` 의 `*.dll *.exe *.ocx *.cpl *.ax *.drv` **3,916개** 를 `objdump -d -M intel` 로 풀었다 (x64 3,881개, 나머지 35개는 .NET 이라 건너뜀; 28개 병렬, 약 35초).
2. 줄마다 `mov REG,gs:0x30` (= TEB) 로 시작해 `mov`·`lea`·`add` 로 따라가고, 호출 뒤엔 휘발 레지스터를 버리고, 함수 시작 (`.pdata`, 연결 조각은 부모에 붙임) 마다 초기화, 앞쪽 분기는 상태를 합쳤다. `gs:0x...` 직접 접근도 같이 봄. 39칸 오프셋 범위 (`pdb_struct.py ntdll.dll _TEB` 기준) 안으로 들어가는 접근만 모아 **쓰기 / 읽기 / 주소 꺼내기 (lea)** 로 가름.
3. 함수 이름 = `.pdata` 함수 시작 → Ghidra `_index.tsv` 이름 (있으면), 없으면 PDB 공개 이름. `rva` 는 함수 시작 RVA.
4. 커널 쪽 (`Win32ThreadInfo`·`Gdi*` 의 커널 쓰기) 은 `win32kbase.sys` · `win32kfull.sys` · `win32k.sys` 만 따로 훑음 (`gs:0x188` 현재 스레드 → `+0xf0` TEB 로 따라감).
5. "test.exe 에서 타나": ① 사진 P2 (`NtContinue` 입구) 모듈 6개 = test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2 (`ms/logs/04_baggage-read_P2.txt:3397 ~ 3923`) — 다른 DLL 은 **안 올라옴** `[관찰]`. test.exe·dll1·dll2 는 import 가 KERNEL32 (와 서로) 뿐이고, 39칸 쓰는 줄 0 (정적 CRT 안에도 없음). ② KERNELBASE 5칸은 직접 호출 그래프 (KERNELBASE · kernel32 `.pdata` + `call/jmp` 직접 목적지 + kernel32 → KERNELBASE import 이름 연결) 에서 DllMain 입구 · `_KernelBaseBaseDllInitialize` 의 **PROCESS_ATTACH 갈래만** (`+0x195` 부터 갈래 따라가기) · kernel32 입구 · `BaseThreadInitThunk` 에서 닿는지 봄.

## 1. 39칸 표 (baggage 3.3 "다른 DLL 39" → 줄 단위)

방법 약어: **OD** = objdump 전수 훑기 · **PDB** = PDB 공개 이름 · **Gh** = Ghidra `_index.tsv` 이름 · **asm** = `.asm` 줄 대조. "안 올림" = P2 모듈 6개에 없음 `[관찰]`.

| # | 칸 (오프셋) | 짐작 (sast/48) | 쓰는 줄 · 실제 DLL | 쓰는 자리 (함수 `+0x`, 함수 rva) | 방법 | test.exe 에서 타나 |
|---|---|---|---|---|---|---|
| 1 | `ActiveRpcHandle` (+0x50) | rpcrt4 | **X** — rpcrt4 는 `ReservedForNtRpc` 를 씀. 이 칸은 읽는 줄도 없음 | — | OD | 해당 없음 (아무도 안 씀) |
| 2 | `CsrClientThread` (+0x70) | CSR | **O csrsrv.dll · basesrv.dll** (csrss.exe 안, csrss 자기 스레드 TEB) | csrsrv `CsrConnectToUser +0x9b` (0x1470) · `CsrpHandleInlineUserConnect +0x6c · +0x93` (0x17c0, asm) · `CsrProcessLazyRegister +0xc6` (0x2180) · `CsrApiRequestThread +0x98` (lea, 0x9340, asm); basesrv `BaseSrvRegisterThread +0x10e` (0x5da0). ntdll 에는 없음 | OD·PDB·Gh·asm | **안 탐** — 쓰는 코드가 csrss 프로세스 안 (test.exe TEB 아님) |
| 3 | `Win32ThreadInfo` (+0x78) | win32kbase (커널) | **O 커널만** | win32kbase.sys `xxxDestroyThreadInfo +0x104` (0xb2c70, `MOV [R14+0x78]`, R14 = `KTHREAD+0xf0` 에서 읽은 TEB). **만드는 줄은 못 찾음 `[공백]`.** 유저 DLL 은 읽기만 (user32 53 · imm32 13 · gdi32 2) | OD (win32k 따로)·PDB | **안 탐** — GUI 초기화 없음 (user32·gdi32 안 올림) |
| 4 | `User32Reserved` (+0x80, 104B) | user32 | **X** — user32 도 안 읽고 안 씀. (wow64.dll rva 0x12af0 `+0x32` · 0x12848 `+0x66` 의 `[+0xc0]`·`[+0xc4]` 쓰기는 `WowTebOffset` (`[TEB+0x180c]`) 을 더한 **TEB32** 칸 (`_TEB32.WOW32Reserved` +0xc0) 이라 뺌 `[정적]`) | — | OD | 해당 없음 |
| 5 | `UserReserved` (+0xe8, 20B) | user32 | **X** | — | OD | 해당 없음 |
| 6 | `WOW32Reserved` (+0x100) | wow64cpu | **O 그런데 KERNELBASE** (wow64cpu 는 x64 칸을 안 만짐) | KERNELBASE `Wow64SetThreadDefaultGuestMachine +0x33` (0xc50f0, `MOV [RCX+0x100],RBX`). 읽기 `+0x29`, `Wow64DetectMachineTypeInternal +0x2b` (0xc5150). wow64.dll 의 `+0xc0·+0xc4` 쓰기는 TEB32 칸 (4번 줄 설명) | OD·Gh | **안 탐** — 초기화 직접 호출 사슬 없음, P2 null |
| 7 | `ReservedForDebuggerInstrumentation` (+0x110, 128B) | 이름만 | **O ttdrecordcpu.dll** (TTD 기록기) | `TTD::ThreadInfo::GetOrCreateForCurrentThread +0x108 · +0x10e` (0xa044, 값 `0x31445454` 'TTD1' + 포인터) | OD·PDB | 안 올림 |
| 8 | `GdiTebBatch` (+0x2f0, 0x4e8B) | gdi32 | **O gdi32full (대부분) · gdi32 (일부)** | gdi32full 8 함수: `PolyPatBlt +0x332 · +0x355 · +0x38b` (0x12100) · `ExtTextOutWImpl +0x382 · +0x397 · +0x3bc` (0x1da30) · `bBatchTextOut +0x368 · +0x37e · +0x3b2` (0x1f6a0) · `ExtSelectClipRgnImpl +0x375 · +0x3cb` (0x20cf0) · `SetBrushOrgEx +0x187 · +0x1e3` (0x22660) · `PatBlt +0x28a · +0x2ac · +0x2e1` (0x22db0) · `SelectObjectImpl +0x43d · +0x49a` (0x26210) · `ExtTextOutInternalA` (0x3c83c). gdi32: `InternalDeleteObject +0x268` (0x10f0) · `DeleteRegion +0xaf` (0x1470). 커널 win32kfull `GdiThreadCalloutFlushUserBatch +0x4c` (0x1d6760) | OD·Gh·PDB | 안 올림 |
| 9 | `GdiCachedProcessHandle` (+0x7e8) | gdi32 | **X** | — | OD | 해당 없음 |
| 10 | `GdiClientPID` (+0x7f0) | gdi32 | **X** | — | OD | 해당 없음 |
| 11 | `GdiClientTID` (+0x7f4) | gdi32 | **X** | — | OD | 해당 없음 |
| 12 | `GdiThreadLocalInfo` (+0x7f8) | gdi32 | **X** | — | OD | 해당 없음 |
| 13 | `Win32ClientInfo` (+0x800, 496B) | user32 | **O user32 (대부분)** · gdi32full · imm32 · CoreMessaging · (csrss 쪽 csrsrv) | user32 36 함수: `ClientThreadSetup +0xd3` (0x4f810, `MOV [TEB+0x910],2`, asm) · `SetThreadDpiAwarenessContext +0x5c` (0x7ae0) · `PeekMessageW +0x96` (0x117e0) · `DispatchMessageWorker +0x2d7` (0x15bc0) · `SendMessageWorker +0x140` · `SetPropW +0x1ad` … / gdi32full 42 함수가 `+0x808` 칸을 비우고 씀 (`BitBlt +0x94` 0x22a20 · `PatBlt +0x5b` 0x22db0 · `PolyPatBlt +0xe3` …) / imm32 8 함수 (`CtfImmTIMActivate +0xf8` 0x5e20 · `CtfImmCoInitialize +0x3c` 0x6a10 · `ISPY_PreInitialize +0x81` 0x9490 …, 플래그 `bts/or [TEB+0x800]`) / CoreMessaging `EnsureSharedTebInitialized<…> +0x48` (0x2dcec, `[TEB+0x9e8]`) / csrsrv `CsrApiRequestThread +0x65` (`[TEB+0x800]`, asm) | OD·Gh·PDB·asm | 안 올림 (user32·gdi32 안 올림) |
| 14 | `glDispatchTable` (+0x9f0, 1864B) | opengl32 | **O opengl32** (표 주소를 복사 호출의 목적지로 넘김, 직접 `MOV` 쓰기가 아님) | `SetCltProcTable +0xe7` (0x43e0) · `MakeAnyCurrent +0x283` (0x54e0) · `bMakeNoCurrent +0x121` (0x5c60) · `vInitTebCache +0x28` (0xc4c0) — `ADD R14,0x9f0` 뒤 `LEA RCX,[R14+RAX*8]` → `CALL 0x18005c1f6` (복사) | OD·PDB | 안 올림 |
| 15 | `glReserved1` (+0x1138, 232B) | opengl32 | **O opengl32** | 69 함수 — 대부분 `glclt*` (`glcltVertex3fv +0x3f · +0x63` 0x2a8f0 · `glcltTexCoord2f +0x24` 0x36980 …) + `GLInitializeThread +0x75` (0xc290) · `MakeAnyCurrent +0x3d3` (0x54e0, `[+0x1138]=0`) · `glsbAttention +0x5e` (0x8dec) | OD·PDB | 안 올림 |
| 16 | `glReserved2` (+0x1220) | opengl32 | **O opengl32** | `GLInitializeThread +0x15d` (0xc290, =0) · `FreePolyMaterial +0x85` (0x5aa94, =0) · `PAMatAlloc +0xb0` (0x62124) | OD·PDB | 안 올림 |
| 17 | `glSectionInfo` (+0x1228) | opengl32 | **O opengl32** | `glsrvLoseCurrent +0xbe` (0x488c) · `glsrvMakeCurrent +0x193 · +0x568` (0xafa0) · `__glDestroyContext +0x39 · +0x2d6` (0xb3ffc) | OD·PDB | 안 올림 |
| 18 | `glSection` (+0x1230) | opengl32 | **X** (opengl32 도 안 읽음) | — | OD | 해당 없음 |
| 19 | `glTable` (+0x1238) | opengl32 | **X** | — | OD | 해당 없음 |
| 20 | `glCurrentRC` (+0x1240) | opengl32 | **O opengl32** | `MakeAnyCurrent +0x1da` (0x54e0) · `bMakeNoCurrent +0x58` (0x5c60) · `GLInitializeThread +0x14c` (=0) · `GLUnInitializeThread +0x126` (0xc650, =0) · `MetaRcBegin +0x26 · +0x7c · +0x95` (0x6c984) · `MetaRcEnd +0x26 · +0x46` (0x6ca34) | OD·PDB | 안 올림 |
| 21 | `glContext` (+0x1248) | opengl32 | **O opengl32** | `GLInitializeThread +0x16e` (=0) · `wglCbSetCurrentValue +0x9` (rva 0x6dd79 — pdata 없는 곳, PDB 가장 가까운 이름) | OD·PDB | 안 올림 |
| 22 | `StaticUnicodeBuffer` (+0x1268, 522B) | KERNELBASE | **O KERNELBASE** | `BaseCopyStream +0x39e` (lea) · `+0x3b6 ~ +0x3ef` (읽기, `MOVUPS` 8번) · `+0x6c87 ~` (쓰기, `MOVUPS` 여러 번) (0xd4644) · 조각 `BaseCopyStream$fin$0 +0x414 ~ +0x46d` (쓰기, 0x19a288). 복사 작업 중 이 칸을 임시 칸으로 쓰고 되돌림 `[추측]`. kernel32 `NtVdm64CreateProcessInternalW +0x95` 는 주소만 | OD·Gh | **안 탐** — `BaseCopyStream` 으로 가는 초기화 직접 호출 사슬 없음, P2 `""` |
| 23 | `ReservedForNtRpc` (+0x1698) | rpcrt4 | **O rpcrt4** | `THREAD *ThreadSelf() +0x2dc` (0x52930) · `_InitializeDLL +0x30` (0x91f50, `AND [TEB+0x1698],0` — DllMain 때 비움) | OD·PDB·Gh | 안 올림 |
| 24 | `PerflibData` (+0x1728) | advapi32 / sechost | **O advapi32** (+ regsvc.dll; sechost 는 X) | advapi32 `PerflibciQueryV2Provider +0x623` (0x3650, 상수 `0x66726550` = 'Perf' 를 넣음) · regsvc `PerflibciQueryV2Provider +0x1fa` (0x1ea94) | OD·PDB | 안 올림 |
| 25 | `EtwTraceData` (+0x1730) | advapi32 / sechost | **X** (sechost·advapi32 모두 줄 없음) | — | OD | 해당 없음 |
| 26 | `WinSockData` (+0x1738) | ws2_32 | **O 그런데 mswsock.dll** (ws2_32 는 줄 없음) | `SockWaitForSingleObject +0x72 · +0x18e · +0x290 · +0x2bf` (0x93c0) · `AllocateWinsockThreadData +0x52` (0xcd20, `AND [..],0`) | OD·PDB | 안 올림 |
| 27 | `GdiBatchCount` (+0x1740) | gdi32 | **O gdi32full (대부분) · gdi32** | gdi32full `PolyPatBlt +0x36c` · `ExtTextOutWImpl +0x3c2` · `bBatchTextOut +0x393` · `ExtSelectClipRgnImpl +0x3ac` · `SetBrushOrgEx +0x1c4` · `PatBlt +0x2c5` · `SelectObjectImpl +0x47b` (+ `ExtTextOutInternalA`; 함수 rva 는 8번 줄과 같음) / gdi32 `InternalDeleteObject +0x24f` · `DeleteRegion +0x96` / 커널 win32kfull `GdiThreadCalloutFlushUserBatch +0x3a` (=0) | OD·Gh·PDB | 안 올림 |
| 28 | `GuaranteedStackBytes` (+0x1748) | KERNELBASE | **O KERNELBASE** | `SetThreadStackGuarantee +0x120` (0xf22d0) · `SwitchToFiberContext +0x112` (0x196f70). 같은 코드가 smss.exe · autofstx.exe · fstx.dll · tprtdll.dll · vertdll.dll 에도 정적으로 들어 있음 | OD·Gh | **안 탐** — 초기화 직접 호출 사슬 없음, P2 0 |
| 29 | `ReservedForPerf` (+0x1750) | 이름만 | **X** | — | OD | 해당 없음 |
| 30 | `ReservedForOle` (+0x1758) | ole32 / combase | **O combase** (ole32·coml2 는 읽기만) | 10 함수: `GetCurrentComApartment +0xc7` (0x117a0) · `GetCurrentApartmentId +0x5a` (0x1b3b0) · `COleStaticMutexSem::Request +0xa0` (0x20df0) · `InitChannelIfNecessary +0xef` (0x22450) · `COleTls::TLSAllocData +0xe2` (0x23f20) · `IsThreadInNTA +0x49` (0x54b48) · `IsApartmentInitialized +0x65` (0x8d3a0) · `CStdIdentity::IsCallableFromCurrentApartment +0x9a` (0xce7b0) · `UninitMTAWorker +0x197` (0xcf100) · `InternalTlsAllocData +0x30` (0x1040f0) | OD·PDB | 안 올림 |
| 31 | `SavedPriorityState` (+0x1768) | KERNELBASE | **O KERNELBASE** | `SetThreadPriority +0x1b7 · +0x271` (0xc6660; 값 = 할당 블록 / 0). 읽기: `_KernelBaseBaseDllInitialize +0x80 · +0x9e` (스레드 떠나는 갈래), ntdll `TppWorkerThread` | OD·Gh | **안 탐** — 초기화 직접 호출 사슬 없음, P2 null |
| 32 | `ReservedForCodeCoverage` (+0x1770) | 이름만 | **X** | — | OD | 해당 없음 |
| 33 | `NlsCache` (+0x17a0) | KERNELBASE | **O KERNELBASE** (10 함수, 쓰기 15줄) | `GetCurrentNlsCache +0x1021 · +0x1096` (0x52350, asm) · `CheckSpecialLocales +0x1283 · +0x12f8` (0x14730, asm) · `InternalLcidToName +0xc28 · +0xc86` (0x13950) · `CompareStringW +0xcc7` (0x16bc0) · `GetSortNode +0x150f · +0x15c8` (0x17aa0) · `CompareStringA +0xfd9` (0x1c510) · `GetStringTypeExW +0xcee · +0xd50` (0x51440) · `NlsValidateLocale +0xbd3 · +0xcc7` (0x53d70, 조각 포함) · `GetLocaleInfoHelper +0x19f2` (0x71250). 모두 같은 꼴: `RtlAllocateHeap(0x698)` → 새 블록 채움 → `MOV [TEB+0x17a0],블록` | OD·Gh·asm | **조건부, 초기화 땐 안 탐** — 아래 3장 |
| 34 | `pShimData` (+0x17a8) | apphelp | **X** (apphelp 도 안 읽음) | — | OD | 해당 없음 |
| 35 | `TxnScopeEnterCallback` (+0x17f0) | ole32 / combase | **X** (쓰는 줄 없음. 읽기만: ntdll `TppWorkerThread +0x8bd · +0x9cf`, `TppCallbackCheckThreadAfterCallback +0x1c7`, verifier.dll) | — | OD | 해당 없음 |
| 36 | `TxnScopeExitCallback` (+0x17f8) | ole32 / combase | **X** (같음) | — | OD | 해당 없음 |
| 37 | `TxnScopeContext` (+0x1800) | ole32 / combase | **X** (같음) | — | OD | 해당 없음 |
| 38 | `ReservedForWdf` (+0x1818) | 이름만 | **O WUDFHost.exe** (UMDF 호스트) | `CWudfIoDispatcher::MarkUmdfDriverInitiatedRequest +0x8a` (0x6c00, =1) · `CWudfDefaultDispatcher::DoRead +0x261` (0xfaf0, =1) · `+0x2b8` (=0) · `UnmarkUmdfDriverInitiatedRequest` (0xdec0, =0; TEB 를 인자로 받음) | OD·PDB | 안 올림 |
| 39 | `ReservedForCrt` (+0x1820) | 이름만 | **O ucrtbase.dll** | `__crt_state_management::enter_os_call +0x22` (0xad30, =1) · `leave_os_call +0x22` (0x4af20, =0) · `enter_os_call_inline +0x11` (0x5e8d0, =1) · `leave_os_call` 조각 rva 0xad1c (=0). OS 호출 앞뒤로 1 / 0 을 씀 | OD·PDB·Gh | 안 올림 (test.exe 는 정적 CRT, ucrtbase 안 올림) |

칸 수 대조: 위 표 39줄 (35·36·37 이 세 줄) = 요청서 39칸. O 23 + X 16 = 39.

## 2. 묶음별 한 줄 (baggage 3.3 줄 바꾸기용)

| 묶음 | 칸 | 실제 DLL | test.exe |
|---|---|---|---|
| `Gdi*` (6) | `GdiTebBatch` · `GdiBatchCount` O (gdi32full 주, gdi32 일부) · `GdiCachedProcessHandle` · `GdiClientPID` · `GdiClientTID` · `GdiThreadLocalInfo` X | gdi32full · gdi32 (+ 커널 win32kfull 이 0 으로 비움) | 안 올림 |
| user32 계열 (3) + `Win32ThreadInfo` (1) | `Win32ClientInfo` O (user32 · gdi32full · imm32 · CoreMessaging) · `User32Reserved` · `UserReserved` X · `Win32ThreadInfo` 커널만 | user32 외 | 안 올림 |
| `gl*` (8) | 6 O (opengl32) · `glSection` · `glTable` X | opengl32 | 안 올림 |
| `WinSockData` (1) | O — **mswsock.dll** (짐작 ws2_32 틀림) | mswsock | 안 올림 |
| rpcrt4 (2) | `ReservedForNtRpc` O · `ActiveRpcHandle` X | rpcrt4 | 안 올림 |
| ole (4) | `ReservedForOle` O (combase) · `TxnScope*` 3 X | combase | 안 올림 |
| advapi32/sechost (2) | `PerflibData` O (advapi32 · regsvc) · `EtwTraceData` X | advapi32 | 안 올림 |
| CSR (1) | `CsrClientThread` O (csrsrv · basesrv, csrss.exe 안) | csrss 쪽 | test.exe TEB 아님 |
| WOW64 (1) | `WOW32Reserved` O — **KERNELBASE** (짐작 wow64cpu 틀림) | KERNELBASE | 안 탐 |
| KERNELBASE (4) | `StaticUnicodeBuffer` · `NlsCache` · `GuaranteedStackBytes` · `SavedPriorityState` 모두 O | KERNELBASE | 안 탐 (3장) |
| 이름만 (6) | `ReservedForWdf` (WUDFHost) · `ReservedForCrt` (ucrtbase) · `ReservedForDebuggerInstrumentation` (ttdrecordcpu) O · `ReservedForPerf` · `ReservedForCodeCoverage` · `pShimData` X | 각각 | 안 올림 |

- 지금 PE_emulator 맞나·틀리나: **맞다.** 에뮬레이터 `Src/nt/Process.cpp` 의 `_FillTeb` (`MmCreateTeb` 13줄) 와 `_WriteSameTebFlags` 만 TEB 를 채우고 이 39칸은 안 건드려 0 — 사진 P2 와 같다. 틀어질 곳: 에뮬레이터가 KERNELBASE 실제 코드를 돌릴 때 (`NlsCache` 갈래 — 3장) → 같은 갈래면 0 유지. 에뮬레이터가 KERNELBASE 를 실제 코드로 도는지 `[확인 필요]`.

## 3. test.exe 초기화에서 타나 — KERNELBASE 5칸

사진 P2 값: `NlsCache` null · `IsImpersonating` 0 · `StaticUnicodeBuffer` `""` · `GuaranteedStackBytes` 0 · `SavedPriorityState` null · `WOW32Reserved` null (`ms/logs/04_baggage-read_P2.txt:2697 · 2738 · 2761 · 2767 · 2774 · 2775`) `[관찰]`. P2 = `NtContinue` 입구 = 모든 `DllMain` 뒤 (KERNEL32·KERNELBASE 시작 함수는 `P1a~P1b`).

| 쓰는 함수 | 초기화 길에서 직접 호출로 닿나 `[정적]` | 결론 |
|---|---|---|
| `BaseCopyStream` · `$fin$0` (`StaticUnicodeBuffer`) | 안 닿음 | 안 탐 |
| `SetThreadStackGuarantee` · `SwitchToFiberContext` (`GuaranteedStackBytes`) | 안 닿음 | 안 탐 |
| `SetThreadPriority` (`SavedPriorityState`) | 안 닿음 (`CtrlRoutine` 로 가는 주소 꺼냄 줄만) | 안 탐 |
| `Wow64SetThreadDefaultGuestMachine` (`WOW32Reserved`) | 안 닿음 | 안 탐 |
| `InternalLcidToName` · `CompareStringW` · `CompareStringA` · `GetSortNode` · `GetStringTypeExW` · `GetLocaleInfoHelper` (`NlsCache`) | 안 닿음 | 안 탐 |
| `NlsValidateLocale` (`NlsCache`) | 닿음, 단 `TipInitialize` (DllMain 입구 호출) 아래 JSON 해석 → `WideCharToMultiByte` → `NlsValidateLocale` 사슬 — TIP 시험용 갈래, 켜지는 조건 `[공백]` | 사진 0 이라 안 탐 `[관찰 간접]` |
| **`GetCurrentNlsCache` · `CheckSpecialLocales`** (`NlsCache`) | **닿음**: DllMain `_KernelBaseBaseDllInitialize +0x4e1` → `BaseNlsDllInitialize` → `NlsProcessInitialize` → `SetupAnsiOemCodeHashNodes` → `GetCPHashNode` → (`+0x4c6` / `+0x13e`) 두 함수 | **쓰는 줄이 `IsImpersonating` (TEB `+0x179c`) ≠ 0 갈래에만 있다**: `GetCurrentNlsCache +0x3d` 가 읽고 `+0x45 JNZ → +0x64e` (`SUB ECX,1`, `CMP ECX,1`) → `+0xa60` 에서 `TEB[+0x17a0]` 이 0 이면 `+0xfcc` 할당 → `+0x1021` 쓰기. 초기화 길은 `IsImpersonating = 0` (P2 0) 이라 이 갈래를 안 탄다 `[정적 + 관찰]` |

- 참고: `SetThreadLocale` 로 가는 호출 (`_KernelBaseBaseDllInitialize +0xdf`) 은 DllMain 의 **THREAD_ATTACH 갈래 (`+0x4e CMP EDX,2`) 의 콘솔 앱 전용** 줄이다. 첫 스레드는 PROCESS_ATTACH 라 안 탐 (둘째 스레드는 이 범위 밖).
- `SavedPriorityState` 읽는 줄 `+0x80 · +0x9e` 는 THREAD_DETACH 갈래 (`RtlCleanUpTEBLangLists` 바로 뒤).
- 지금 PE_emulator 맞나·틀리나: **맞다** (에뮬레이터가 이 5칸을 0 으로 두면 사진과 같음). KERNELBASE NLS 코드를 실제로 돌릴 때만 `IsImpersonating == 0` 이 유지되는지 보면 된다 `[확인 필요]`.

## 4. 정정 · 새로 알게 된 것 (flow-writer 용)

1. **짐작 둘이 틀렸다**: `WinSockData` 는 ws2_32 가 아니라 **mswsock.dll** (`SockWaitForSingleObject`·`AllocateWinsockThreadData`; ws2_32 는 읽지도 쓰지도 않음). `WOW32Reserved` 는 wow64cpu 가 아니라 **KERNELBASE `Wow64SetThreadDefaultGuestMachine`** (x64 TEB 의 이 칸은 "스레드 기본 게스트 머신" 16비트 값 자리처럼 쓰임 `[추측]`; sast/48 이 이미 같이 적었던 줄). `GdiTebBatch`·`GdiBatchCount` 는 gdi32 보다 **gdi32full** 이 주 (gdi32 도 `InternalDeleteObject`·`DeleteRegion` 이 씀).
2. **쓰는 줄 못 찾은 16칸이 짐작표엔 "DLL 있음" 으로 적혀 있었다.** baggage 3.3 "다른 DLL 39" 는 "쓰는 DLL 짐작 `[추측]`" 에서 "O 23 (줄 확인) · X 16 (3,881개 x64 PE 에서 쓰는 줄 못 찾음 `[공백]` — 아무도 안 씀 후보)" 로 바꿀 수 있다. X 16 은 "아무도 안 씀 28" 과 같은 종류 (옛 칸·계측 칸·이름만 있는 칸).
3. sast/48 결과서 175줄 `StaticUnicodeBuffer … FatalAppExitA +0x49` 는 **`StaticUnicodeString` (+0x1258, Buffer 포인터 +0x1260) 줄**이다 (`FatalAppExitA +0x49 LEA RCX,[RBX+0x1258]`). `StaticUnicodeBuffer` (+0x1268) 자체의 쓰는 자리는 `BaseCopyStream` 쪽 (표 22번).
4. **`IsImpersonating` 쓰는 곳에 KERNELBASE NLS 10 함수가 빠져 있다** (sast/48 3.2 줄은 nt 와 ntdll 만): `InternalLcidToName +0x985` · `CheckSpecialLocales +0xdf6` · `CompareStringW +0xa2b` · `GetSortNode +0x1367` · `CompareStringA +0xd14` · `GetUserOverrideString +0x749` · `GetStringTypeExW +0xad4` · `GetCurrentNlsCache +0xbee` · `NlsValidateLocale +0x940` · `GetLocaleInfoHelper +0xc00` — 값 2 (`MOV R12D,0x2` → `[TEB+0x179c]`) 또는 복원. 의미: 스레드별 NLS 캐시를 쓰는 중이라는 표시 `[추측]`. 그중 `CheckSpecialLocales`·`GetCurrentNlsCache` 는 초기화 직접 호출 사슬에 있지만 갈래 안 탐 (사진 0, 3장).
5. 위 4 의 반대로, **`NlsCache` 를 읽는 쪽**도 DllMain 사슬에 있다 (`GetCPHashNode +0x4c6`) — 초기화 중 TEB `NlsCache` 를 읽는 줄은 있고 쓰는 줄은 갈래 밖.
6. ntdll·kernel32 는 39칸을 하나도 안 쓴다 → "다른 DLL" 이라는 이름이 맞다 (ntdll 로 옮길 칸 없음).
7. `Win32ThreadInfo` 의 만드는 줄 (유저 TEB 에 `pti` 를 놓는 줄) 은 이 훑기로 못 찾음 — 커널 `xxxDestroyThreadInfo +0x104` 는 정리 쪽. 이름만: win32kbase `xxxCreateThreadInfo` (rva 0x194d84) 가 후보이나 안 풂 `[공백]`.

## 5. 동적으로 확인할 것

dast 요청 필요: **없음.** 모두 낮음 (사진 P2 가 이미 0 으로 답함).

| # | 질문 | 추천 멈춤점 | 사진으로? |
|---|---|---|---|
| D1 | 초기화 중 `GetCurrentNlsCache` 가 불리고 어느 갈래로 도나 (`IsImpersonating` 읽은 값) | `bp KERNELBASE!GetCurrentNlsCache` (조건 test.exe) → `dd @$teb+0x179c L1` , `dq @$teb+0x17a0 L1`, `k` (`GetCPHashNode+0x4cb` 에서 왔나) | 불가 (호출 여부). 낮음 |
| D2 | TEB `+0x179c` · `+0x17a0` 에 초기화 중 쓰기가 한 번도 없나 (쓰고 되돌림 포함) | `ba w4 @$teb+0x179c` · `ba w8 @$teb+0x17a0` 을 `LdrpInitializeProcess` 입구부터 걸고 `NtContinue` 까지 hit 수 | 불가 (쓰고 되돌림은 사진에 안 남음). 낮음 |

- 디버거가 붙어서 달라지는 점: 없음.

## 6. 다음 요청 후보

1. (정적, 문서) sast/48 3.2 `IsImpersonating` 줄에 KERNELBASE NLS 10 함수 보태기 + baggage 3.3 "다른 DLL 39" → "O 23 · X 16" 줄 바꾸기 (flow-writer, 이 결과서 1~2장 근거).
2. (정적, 낮음) `Win32ThreadInfo` 를 유저 TEB 에 놓는 줄 — win32kbase `xxxCreateThreadInfo` 와 win32kfull 호출 사슬 (objdump 로는 함수 인자로 TEB 를 넘겨 못 잡음; 필요하면 win32kbase Ghidra analyze 한 번). test.exe 는 GUI 를 안 올려 우선순위 낮음.
3. (정적, 낮음) `GetCPHashNode +0x4c6` 가 초기화에서 부르는지 (`IsUnspecifiedLcid` 갈래) — 7.6.8 NLS 칸 (`gNlsProcessLocalCache`·`gpACPHashN` …) 에 이미 줄이 있어 새 칸은 없을 것.
4. (하네스) 임시 도구를 `scripts/` 정식 도구로: 제안 아래.

## 7. 검색 한계 (한 줄)

3,881개 x64 PE 의 `gs:0x30` 추적 훑기라서, **TEB 포인터를 함수 인자·스택·구조체에 담아 넘긴 뒤 쓰는 줄** (예: `UnmarkUmdfDriverInitiatedRequest`) 은 놓칠 수 있고 (그래서 "X" 16칸은 "찾지 못함" 이지 "없음 증명" 이 아님), 합치기가 느슨해 틀린 줄이 섞여 (TEB32 를 쓰는 wow64·ntdll 줄 등) 표에는 눈으로·`.asm` 으로 확인한 줄만 넣었으며, SysWOW64·.NET (35개)·드라이버 (.sys 는 win32k 3개만) 는 안 봤다.

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **고칠 버그는 없다.** `fetch_pdb.py` 12개 · `pdb_struct.py ntdll.dll _TEB` · `pdb_aliases.py --where` 모두 문제없이 돌았다.
- 이번에 임시 도구를 짰다 (`_workspace/sast/117_teb-other-dlls_tools/`, 커밋 여부는 오케스트레이터가): `tebscan_other.py` (System32 전체 TEB 칸 접근 훑기, 35초) · `post.py` (함수 소유 + 이름 붙이기) · `reach.py` · `reach2.py` · `reach3.py` (KERNELBASE·kernel32 직접 호출 그래프 + DllMain PROCESS_ATTACH 갈래만 따라 닿는지). sast/43·44·46·47·48 의 `field_writers.py` 제안과 같은 것이다 — 이번에 **정식 도구로 묶으면 좋다는 근거**:
  - 처음판 (선형 추적) 은 `SetThreadPriority` 의 `SavedPriorityState` 쓰기를 놓쳤다 → **앞쪽 분기 상태 합치기 + 연결된 `.pdata` 조각은 초기화하지 않기** 를 넣으니 잡혔다 (sast/48 첫판 `tebscan.py` 와 같은 증상).
  - `rex.W call` 같은 접두 줄을 접두로 안 읽으면 호출 뒤 레지스터 버리기가 빠져 틀린 줄이 생긴다 (이번 처음판 오류).
  - 남은 한계: 인자로 받은 TEB 따라가기 (제안 sast/48 (b)), TEB32 구분.
- 제안: `scripts/field_writers.py <바이너리|전체> --struct _TEB --fields <이름…>` (결과 = 모듈·함수·`+0x`·읽기/쓰기/lea) — 위 `tebscan_other.py` + `post.py` 가 시제품.

## 판정

**분석성공.** 요청서 질문 1~3 모두 답함: 39칸 모두 O/X 와 쓰는 자리 (O 23 · X 16), 짐작이 틀린 칸 2 (`WinSockData` → mswsock, `WOW32Reserved` → KERNELBASE), test.exe 에서 타는 칸 0 (KERNELBASE 5칸은 사슬이 닿아도 갈래 밖, 사진 0 과 맞음). 정적으로 못 닫은 것: `Win32ThreadInfo` 만드는 줄 `[공백]`, 인자로 TEB 를 넘기는 꼴의 누락 가능성, NLS 갈래 "실제로 안 탔다" 는 사진 간접 (D1·D2, 낮음). 버전 대조: `.asm` 줄·sast/48 KERNELBASE 줄과 일치.

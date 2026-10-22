# 분석 진행표

- 마지막 갱신: 2026-10-22 (run18 뒤)
- 다음 항목: Q9b (단계 2)
- 근거: `runs/01_sprint/flow.md` 그림 1·2, 2장, 4장·4.1 표

## 대기열 (위에서부터 처리, 새 항목은 맨 아래에)

| # | 항목 | 단계 | 정적/동적 | 들어온 곳 | 상태 |
|---|---|---|---|---|---|
| Q1 | 2.1 `nt!NtCreateUserProcess` callee (depth 1) | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 끝 (run9) |
| Q2 | 2.1 `nt!PspAllocateProcess`, `nt!MmCreatePeb` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 끝 (run10) |
| Q3 | 2.1 `nt!PspAllocateThread`, `nt!PspInsertProcess` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 끝 (run11) |
| Q4 | 2.1 `nt!PspInsertThread`, `nt!PspCallProcessNotifyRoutines` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 1); sast/06 결과 (`PspAllocateThread` 가 잡은 스레드 락을 풀지 않음 → `PspInsertThread` 에서 푸는지) | 끝 (run12) |
| Q5a | 2.2 첫 스레드를 풀어 주는 nt 쪽: `NtResumeThread` → `PsResumeThread` → `KeResumeThread`, 그리고 멈춰 두는 쪽 `KeSuspendThread`·`KiSuspendThread` 가 하는 일 (멈춘 스레드가 실제로 어디서 서나) | 1 | 정적 (ntoskrnl) | 처음 (4장 2.2); Q5 를 그 자리에서 나눔 (2026-10-21) | 끝 (run13) |
| Q5b | 2.2 부모 쪽에서 첫 스레드를 풀어 주는 곳: `KERNELBASE!CreateProcessInternalW` 가 `NtCreateUserProcess` 뒤에 `NtResumeThread` 를 부르나 (이름 확인 필요) | 1 | 정적 (kernelbase) | 처음 (4장 2.2); Q5 를 그 자리에서 나눔 (2026-10-21) | 끝 (run14) |
| Q6 | 2.3 `nt!KiStartUserThread`, `nt!PspUserThreadStartup` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 2); ntoskrnl analyze (2026-10-19): `_index.tsv` 에 `KiStartUserThread` 없음, `KxStartUserThread` 있음 (PDB엔 `KiStartUserThread`·`KiStartUserThreadReturn` 이름 있음) → 이름 대응 확인 | 끝 (run15) |
| Q7 | 2.3 유저 첫 위치가 `ntdll!LdrInitializeThunk`로 정해지는 곳, `RtlUserThreadStart`와의 관계 | 1 | 정적 (ntoskrnl) | 처음 (4장 2.3); sast/03 결과 (`PspCreateUserContext` 91바이트 — `NtCreateUserProcess +0xd22` 에서 부름, 첫 스레드 유저 시작 문맥 후보); sast/06 결과 (`PspAllocateThread` 는 CONTEXT.Rip→StartAddress, Rcx→Win32StartAddress 로 옮겨 적기만 함, `PspUserThreadStartup` 주소는 `+0x681` 에서 `KeInitThread` 로 → CONTEXT 를 누가 채우나); sast/10·run15 (유저 첫 RIP = `_KTRAP_FRAME.Rip`(+0x168), 트랩 프레임 = InitialStack-0x190, 처음 `KxContextToKframes` 가 채우고 `PspUserThreadStartup +0xb3 PspInitializeThunkContext` 가 다시 정함 → 이 함수 본문) | 끝 (run16) |
| Q8 | 2.4 `ntdll!LdrInitializeThunk`, `LdrpInitializeInternal`, `LdrpInitialize` callee | 2 | 정적 (ntdll) | 처음 (그림 2); sast/02 결과 (`+0x16c` 의 caller 이름이 `_LdrpInitialize`, `LdrpInitialize` 는 꼬리호출(JMP)로 `LdrpInitializeInternal` 에 넘김 → flow.md 2.4 이름 정정 필요) | 끝 (run17) |
| Q9a | 2.4 `ntdll!LdrpInitializeProcess` 앞쪽 `+0x0 ~ +0x12dd` callee (호출 94곳 — 프로세스 전체 준비: 힙·NLS·실행 옵션·환경·경로·ntdll 모듈 항목), 입구에서 RCX·RDX 쓰는 곳 | 2 | 정적 (ntdll) | 처음 (그림 2); Q9 를 그 자리에서 나눔 (2026-10-22) | 끝 (run18) |
| Q9b | 2.4 `ntdll!LdrpInitializeProcess` 뒤쪽 `+0x12de ~ 끝` callee (호출 75곳 — 모듈 목록·디버거·WOW64·kernel32·import·그래프·TLS) | 2 | 정적 (ntdll) | 처음 (그림 2); Q9 를 그 자리에서 나눔 (2026-10-22); sast/13·run18 (넘길 연결점: `+0x1302 LdrpDrainWorkQueue`·`+0x1313 LdrpInitializeDllPath`, `+0x1359 LdrpAllocateModuleEntry`(exe 항목 → `LdrpImageEntry`), RCX 처음 쓰는 곳 `+0x17f4`, `BeingDebugged` `+0x17d2`·`+0x1b35`, KUSER `+0x330` `+0x188c`·`+0x1cb0`, `LdrpNtdllHotPatchContext` `+0x18ef`, `NtGlobalFlag & 0x100` `+0x16b7`) | 대기 |
| Q10 | 2.5 `LdrpInitializeKernel32Functions`, `LdrLoadDll`, `LdrpLoadDll` callee | 2 | 정적 (ntdll) | 처음 (그림 2); run18 (앞쪽이 준비한 전역 `LdrpKnownDllDirectoryHandle`·`LdrpKnownDllPath`·`LdrpSystem32`·`RtlpSystem32Dirs`·`LdrpHashTable`·`LdrpHeap`·`LdrpSystemDllBase`·`LdrpAppHeaders` 를 DLL 로드 코드가 쓰는지 `refs` 로) | 대기 |
| Q11 | 2.5 `LdrpLoadDllInternal` (`+0x284` 앞 = 올리기 쪽), `LdrpPrepareModuleForExecution` callee | 2 | 정적 (ntdll) | 처음 (그림 2, 4장 2.5) | 대기 |
| Q12 | 2.5·2.6 올리면서 의존 관계 그래프를 만드는 함수 (Q11 결과에서 이어감) | 2 | 정적 (ntdll) | 처음 (4장 2.5·2.6) | 대기 |
| Q13 | 2.5 KERNELBASE·KERNEL32 시작 함수 이름 (PE 진입점 → PDB 이름) | 2 | 정적 (kernelbase, kernel32) | 처음 (4장 2.5) | 대기 |
| Q14 | 2.6 dll1·dll2를 올리는 쪽 (`LdrpInitializeProcess+0x1bb6`(call 자리, ret `+0x1bbb`) 앞 호출 중 import 처리) | 2 | 정적 (ntdll) | 처음 (4장 2.6) | 대기 |
| Q15 | 2.7 `LdrpInitializeGraphRecurse`, `LdrpInitializeNode` callee | 2 | 정적 (ntdll) | 처음 (그림 2) | 대기 |
| Q16 | 2.7 `LdrpCallTlsInitializers`, `LdrpCallInitRoutine`, `ImageTlsCallbackCaller` callee | 2 | 정적 (ntdll) | 처음 (그림 2, 4장 2.7); run18 (`LdrpInitializeProcess +0x774 ~ +0x8a3` 이 PEB TLS 비트맵 칸 PEB+0x78·+0x80·+0x238·+0x240 을 걸어 둠 — `LdrpInitializeTls` 바탕) | 대기 |
| Q17 | 2.10 로더 끝 → 진입점: `LdrInitializeThunk` 뒤 `NtContinue`, `RtlUserThreadStart` callee | 2 | 정적 (ntdll, ntoskrnl) | 처음 (그림 2 PH4); run16 (커널 쪽 `NtContinue` 가 저장본 CONTEXT 의 Rip(=RtlUserThreadStart) 로 보내는 길 — `KiContinueEx`/`KiContinuePreviousModeUser` 등 ntoskrnl, 이름은 --where 로) | 대기 |
| Q18 | 2.10 `KERNEL32!BaseThreadInitThunk` callee | 2 | 정적 (kernel32) | 처음 (그림 2) | 대기 |
| Q19 | 2.2 첫 스레드가 실제로 풀리는 시점 (부모 스레드와의 순서) | 3 | 동적 | 처음 (4장 2.2); sast/03 동적 1·2 (`NtCreateUserProcess` 입구 ThreadFlags bit0, `PspInsertThread` 의 `KeSuspendThread` `+0x2e5`·`KeReadyThread` `+0x5a6` 탔는지); run12 (멈춘 첫 스레드도 `KeReadyThread` 를 거침 → 실제로 서는 시점·풀리는 시점, `KeSuspendThread` 안쪽); sast/08·run13 (`nt!KiSchedulerApc` 에서 `k` 로 서는 곳 확인 — 기대 `KeWaitForSingleObject ← KiSchedulerApc+0xf7 ← KiDeliverApc+0x220`, `PspUserThreadStartup+0x22` 와 `NtResumeThread` 의 순서, `KiSuspendThread` 의 어느 갈래인지, SuspendCount `+0x284`·SuspendEvent `+0x2e4` 값 바뀌는 순간); sast/09·run14 (cmd.exe 의 실제 `dwCreationFlags` 에 4 가 있나, `nt!NtCreateUserProcess`·`nt!NtResumeThread` 입구(cmd.exe `/p`)에서 긴 `k` — 유저 쪽 `KERNELBASE!CreateProcessInternalW+0x30ee`/`+0x5bd3` 보이나, `NtCreateUserProcess` 몇 번, 8번째 인자 1 인가, `CsrClientCallServer +0x4672` 순서, `NtAlertResumeThread` 엔 안 서나, 가운데 ①검사 ②CSR 알림 ③후처리 중 test.exe 때 실제로 도는 것(import 검사 12곳)) | 대기 |
| Q20 | 2.5 kernel32·kernelbase가 메모리에 올라오는 순간 (`NtMapViewOfSection` 계열 + `k`) | 3 | 동적 | 처음 (4장 2.5) | 대기 |
| Q21 | 2.5 `LdrpCallInitRoutine` → KERNELBASE·KERNEL32 시작 함수 실제 진입 + 콜스택 | 3 | 동적 | 처음 (4.1 (1)) | 대기 |
| Q22 | 2.5 `LdrpInitializeNode` [KERNELBASE]/[KERNEL32] 대응 (긴 `k`) | 3 | 동적 | 처음 (4.1 (1)) | 대기 |
| Q23 | 2.6 dll1·dll2가 메모리에 올라오는 순간 | 3 | 동적 | 처음 (4장 2.6) | 대기 |
| Q24 | 2.7 `LdrpInitializeGraphRecurse` 재귀 단계 ↔ 모듈 대응 (`k 8` 이상) | 3 | 동적 | 처음 (4.1 (2)) | 대기 |
| Q25 | 2.7 `LdrpCallInitRoutine` → `ImageTlsCallbackCaller` (`k`), 그 안쪽 | 3 | 동적 | 처음 (4.1 (2)(3), 4장 2.7); sast/05 지도 (`ImageTlsCallbackCaller` 는 호출 참조 0, 데이터 참조뿐 — `LdrpCallTlsInitializers+0x18f` 에서 주소만 꺼냄 → 정적 호출 지도로는 못 닿음) | 대기 |
| Q26 | 2.8 `ImageTlsCallbackCaller` → `Dll2Tls`/`Dll1Tls` (`k`) | 3 | 동적 | 처음 (4.1 (2), 4장 2.8) | 대기 |
| Q27 | 2.8 `LdrpCallInitRoutine` → `_DllMainCRTStartup` → `DllMain` (`k`) | 3 | 동적 | 처음 (4.1 (2)) | 대기 |
| Q28 | 2.9 `ImageTlsCallbackCaller` → `test!ExeTls` (`k`) | 3 | 동적 | 처음 (4.1 (3)) | 대기 |
| Q29 | 2.10 `LdrInitializeThunk` → `NtContinue` → `RtlUserThreadStart` (`k`) | 3 | 동적 | 처음 (4.1 로더 끝); sast/05 지도 (`RtlUserThreadStart` 는 CALL/JMP 참조 0 — `NtContinue` 의 SYSCALL 에서 정적 길이 끊김); sast/11 (`LdrInitializeThunk +0x13`(ret +0x18) → `NtContinue(저장본, TRUE)` 는 [정적]. 커널 `NtContinue` 가 저장본 Rip=`RtlUserThreadStart` 로 돌아가는 부분은 아직) | 대기 |
| Q30 | 2.10 `RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup` (`k`) | 3 | 동적 | 처음 (4.1 로더 끝) | 대기 |
| Q31 | 2.10 `mainCRTStartup` → `LoadLibraryExW` ×3 → `main` (`k`) | 3 | 동적 | 처음 (4.1 로더 끝, 4장 2.10) | 대기 |
| Q32 | 2.2 `nt!PspMapThreadCreationFlags` 본문 (ThreadFlags → 최종 플래그 bit0 가 그대로 가나) | 1 | 정적 (ntoskrnl) | sast/03 결과 (정적 후보) | 대기 |
| Q33 | 2.1 `nt!MmCreateSpecialImageSection` callee (exe 이미지 섹션, `MiCreateImageFileMap` 과의 관계) | 1 | 정적 (ntoskrnl) | sast/03 결과 (정적 후보, points.md [확인 필요]) | 대기 |
| Q34 | 2.1 `NtCreateUserProcess` 실행 길: 간접 호출 2곳(`+0x5b2`, `+0x11bb`) 대상, `PspCreateUserContext` 길 vs `PspGetContextThreadInternal` 길, 이미지 섹션 재시도, 반환값 | 3 | 동적 | sast/03 동적 3~6, run9 flow.md 4.1 | 대기 |
| Q35 | 2.1 `PspAllocateProcess` 실행 길: `MmInitializeProcessAddressSpace` `+0x2095`(보통) vs `+0x2113`(복제) 중 탄 쪽, 간접 호출 2곳(`+0x121a` @0xf04d28, `+0x17c4` @0xf04d20) 대상, `PspWow64PickBestNtdll` `+0xffb` 타는지, `+0x2314`·`+0x239c` 에서 새 PEB `+0x10`·`+0x20` 값 | 3 | 동적 | sast/04 동적 확인 | 대기 |
| Q36 | 2.1 `nt!PspCopyAndFixupParameters`(PEB `+0x20` 프로세스 매개변수를 누가 쓰나), `nt!PspPrepareSystemDllInitBlock`(ntdll 로 넘기는 시작 정보) | 1 | 정적 (ntoskrnl) | sast/04 결과 (다음 정적 후보) | 대기 |
| Q37 | 지도 크기 재기 (실험): 정적 호출 관계를 뿌리에서 끝까지 따라가 깊이별 함수 수를 재고, 끊기는 노드에 끊긴 이유를 적는다. 뿌리 = 커널 `nt!NtCreateUserProcess`, 유저 `ntdll!LdrInitializeThunk` | - | 정적 (ntoskrnl, ntdll) | 사용자 제안 (2026-10-21) | 끝 (run 없음, 도구 실험 — 결과서 sast/05) — 사용자 결정으로 Q3 앞에서 처리 |
| Q38 | 2.1 `PspAllocateThread`·`PspInsertProcess` 실행 길: 입구 11번째 인자(`poi(@rsp+0x58)`) 가 0인지(보통 길), `+0x6f4` 에서 `KeInitThread` 시작 정보(`dqs @rdx`), 먼 덩어리(`nt+0xb0d315`) 를 WinDbg 가 무슨 이름으로 보이나(`ln`), `PspInsertProcess+0x115` 에서 test.exe 가 목록에 보이나, `CONTEXT.Rip/Rcx` ↔ `_ETHREAD.StartAddress/Win32StartAddress` 비교, `PspInsertProcess+0x1e3` 앞뒤 핸들 표, `PspSetupUserStack`·`MmCreateTeb` 반환값과 TEB 주소 | 3 | 동적 | sast/06 동적 확인, run11 4.1; 묶음 M 시험 (`PspSetupUserShadowStack` `+0xb65` 를 타는지 = 이 VM 에서 CET 섀도 스택이 켜졌나) | 대기 |
| Q39 | 2.1 `PspInsertThread`·`PspCallProcessNotifyRoutines` 실행 길: 입구 flags(`poi(@r9)`) bit0·`0x20`, `+0x2e0` 멈춤 여부, `+0x5dd`/`+0x5a1` 전후 스레드 상태, 프로세스 락이 호출자에서 들어오나, 알림 간접 호출 4곳(`+0x14d`/`+0x31c`/`+0xbe`/`+0x1cf`) 의 `ln @rax` 와 `PspNotifyEnableMask`·목록·BAM/DAM 값, `PsTerminateProcess`·잡 길을 안 타는지, 둘째 스레드 때 `+0x802` 가 안 걸리는지, 알림 문턱 값(`프로세스+0x5fc` bit0, `+0x640`), `+0x499` 에서 `poi(@rax)` | 3 | 동적 | sast/07 동적 확인, flow.md 4.1 (run12) (Q38 과 연달아 있어 한 run 으로 묶을 수 있음) | 대기 |
| Q40 | 2.2·2.3 작은 정적 확인 둘: `EPROCESS.Flags3.ProcessFirstResume`(`+0x5fc` bit15, `PsMultiResumeThread` 가 켬)를 읽는 곳, 조각 `FUN_1406b2db3` 의 정체(`KiApcInterrupt` 추정) | 1 | 정적 (ntoskrnl) | sast/08·run13 4장 | 대기 |
| Q41 | 2.2 부모 유저 쪽 남은 정적 확인 (작음): `kernel32!CreateProcessW` → `KERNELBASE!CreateProcessW` 연결, ApiNumber `0x1001d` 정체 (`CsrClientCallServer` 는 ntdll — import 표로 확인 2026-10-22), ntdll `NtCreateUserProcess`·`NtResumeThread` 스텁이 바로 시스템 콜을 하나, KERNELBASE 가 켜는 ProcessFlags bit9(`0x200`)를 커널이 어떻게 쓰나 | 1 | 정적 (kernel32, ntdll, ntoskrnl) | sast/09·run14 4장 | 대기 |
| Q42 | 2.3 커널 → 유저 전환 실제 길: `KiKvaShadow`·`KiIrqlFlags` 값(유저로 가는 `IRETQ` 가 `KxStartUserThread` 조각 쪽인지 `KiKernelExit +0xa5` 인지), 멈춤 APC 가 `PspUserThreadStartup+0x26` 에서 들어오나, `+0xae`/`+0xb3` 앞뒤 트랩 프레임 `Rip`/`Rsp`, `KiStartUserThread+0x28` 에서 `dq @rbp+0xe8 L5`(유저 진입 직전 Rip·CS·EFlags·Rsp·SS), `KiInitiateUserApc` 타나, ThreadInserted/Terminated/Minimal 값, 트랩 프레임 위치 `RBP-0x80 = InitialStack-0x190` 확인, `KyStartUserThread` 템플릿이 정말 안 도나 | 3 | 동적 | sast/10 동적 확인 (Q7 결과와 같이 보면 좋음); sast/11 (`PspUserThreadStartup +0xae` 앞뒤 트랩 프레임 Rip/Rcx/Rdx/Rsp, `PspInitializeThunkContext +0x16d`/`+0x2d6`/`+0x2db`/`+0x2f5`(KiCheckForKernelApcDelivery 불리나), `ntdll!LdrInitializeThunk` 입구 RCX·RDX·`dt _CONTEXT @rcx`, `+0x13` 에서 @rbx, `dq nt!PsNtdllExports L3`, `poi(PspSystemDlls)+0x20` vs `lm m ntdll`, InstrumentationCallback·CET 비트) | 대기 |
| Q43 | 2.4 로더 입구 실행 값 (주소로 멈춤 — 이름 겹침): `ln ntdll+0x8c0f8`·`x ntdll!*LdrpInitialize` 로 WinDbg 가 `_LdrpInitialize` 를 무슨 이름으로 보이나, `ntdll+0xe1448`/`+0xe1484` 에서 RCX·RDX 같은가와 `SecurityCookieInitialized`(`+0x1d23ce`)·`SpecialMachineFramesInitialized`(`+0x1d23d0`), `LdrpInitializeInternal` 입구 `TEB+0x17ee`·`LdrpHotPatchInitialized`(`+0x1d2928`), `LdrpInitializeHotPatching +0x45` eax 와 `LdrpIsHotPatchingEnabled`(`+0x1d26f0`) — `LdrpLoadPatchedNtdll` 타나, `_LdrpInitialize +0x43`(`ntdll+0x8bfcf`) eax (첫 스레드 0 / Worker 2), `ntdll+0xe142e` 도착이 `LdrpInitializeProcess` 끝난 뒤인가 | 3 | 동적 | sast/12·run17 | 대기 |
| Q44 | 2.4 `LdrpInitializeProcess` 앞쪽 실행 값 (주소로 멈춤, `ntdll+0x8c404+오프셋`, test.exe 만): 입구 `r rcx rdx`(RDX = `lm m ntdll` 시작인가), `+0x5f2` `UseWOW64` 0 인가·`+0x587` 타나, `+0x58e` 에서 `NtGlobalFlag`(PEB+0xbc, 커널 디버거만 붙었을 때), `LdrpInitializeProcessHeap` 반환값, `\KnownDlls` 열기(`+0xdb8`) 성공·`KnownDllPath`·`LdrpSystem32`, `+0x1233` 의 `[ctx]`·`[ctx+8]`(둘째 `+0x126a` 타나), `+0x12de` 에서 `PebLdr`(rva 0x1d2880) 목록 3개에 ntdll 만 있나, 에필로그 `+0xa7b` 에 안 서나 | 3 | 동적 | sast/13·run18 "아직 확인 필요" 동적 1~8 (Q43 과 같은 기준점 — 연달아 있어 한 run 으로 묶을 수 있음) | 대기 |
| Q45 | 2.4 `ntdll!LdrpInitializeNtdllDataTableEntry` 본문 (633바이트): 적재·메모리 순서 목록에 넣는 곳(`LdrpInsertDataTableEntry` 를 부르나), 색인, 둘째 호출(핫패치 ntdll)의 정체 | 2 | 정적 (ntdll) | sast/13·run18 (새 정적 후보) | 대기 |

상태: `대기` / `진행 중` / `끝 (runN)` / `합침 → Qn` / `보류 (사용자 결정, 날짜)`

- 단계 3 항목은 그때까지 정적으로 채워지면 `합침 → Qn` 또는 끝으로 바꾼다 (이미 정적으로 답이 나온 것은 동적으로 다시 보지 않는다).
- `nt!KiSystemServiceCopyEnd`(시스템 콜 입구)는 callee가 간접 호출뿐이라 단계 1 항목에서 뺐다.
- test.exe 바이너리는 서버에 없어서 `mainCRTStartup` 안쪽은 정적 항목이 없다 (Q31 동적).

## 결정 기록
- 2026-10-19: 처음 대기열 Q1~Q31 사용자 확인 ("확인")
- 2026-10-19: Q1 전에 스크립트 시험을 먼저 한다 (사용자 요청). sast/01_script-check = 대기열 항목 아님. ntoskrnl analyze는 뒤에서, ntdll로 알려진 오프셋 대조. 스크립트 문제는 고치지 않고 보고만 (`.claude/`는 사용자가 고침)
- 2026-10-19: sast/01 실패 (JDK 25 + Ghidra 11.3.2 OSGi 로더). 사용자가 JDK 21 설치·`launch.properties` 지정. 사용자 허락으로 ntoskrnl 작업 종료, PDB 없는 Ghidra 프로젝트 2개 삭제 → sast/02_script-check 로 재시도
- 2026-10-19: sast/02 실패 (JDK 25 때 만든 Ghidra 스크립트 캐시 `~/.config/ghidra/ghidra_11.3.2_PUBLIC/osgi/compiled-bundles/d5ff6acd` 재사용). 사용자 허락으로 ntoskrnl 작업 종료·PDB 없는 프로젝트 삭제·캐시 삭제 → 같은 요청서 sast/02로 한 번 더 (사용자 결정)
- 2026-10-19: sast/02 재실행 성공. ntdll 알려진 오프셋 8/8 일치 (전부 ret_off). ntoskrnl analyze 뒤에서 도는 중 (PID 847212, PDB 붙음)
- 2026-10-19: 사용자 결정 — 하네스는 오케스트레이터가 사용자 검토 뒤 고치고, 묶음마다 하네스 전용 커밋으로 관리 (되돌리기 = `git revert`)
- 2026-10-19: 하네스 묶음 A (수정 규칙) 커밋 `e3ac6ba`, 묶음 C (ghidra-headless-export 알려진 사실 표·reference 예시·JDK 캐시 항목 정정) 커밋 `933fd83`. 사용자 검토 완료
- 2026-10-19: 하네스 묶음 B·D-1·D-2·E 사용자 OK. D-2 (analyze 건너뛰기 조건 강화, 실패면 원인 보고 후 사용자 결정) 커밋 `40df1ae`, E (check_commit.sh 한글 첫 줄 검사) 커밋 `b1fd76f`
- 2026-10-19: B (ExportFunctions 꼬리호출 `tail`·`INT` 제외), D-1 (run_ghidra.sh SCRIPT ERROR면 실패 종료)는 ntoskrnl analyze 끝난 뒤 고침 (도는 중인 스크립트). Q1은 B 뒤에 시작 (사용자 결정 "(나)")
- 2026-10-19: ntoskrnl analyze 끝 (01:02 ~ 01:54, 함수 34982개, `FUN_` 4625개, SetPdb 성공, SCRIPT ERROR 0)
- 2026-10-19: 하네스 묶음 B (ExportFunctions 꼬리호출 `tail`·`INT` → `TRAP`) 커밋 `ef63cc2`, D-1 (run_ghidra.sh SCRIPT ERROR → exit 4) 커밋 `11f2a2d`. ntdll 재export 시험 통과. Q1 시작 (sast/03)
- 2026-10-19: Q1 끝 (run9, sast/03). 새 할 일 — 사용자: "제안대로" → Q32·Q33·Q34 맨 아래 추가, `PspCreateUserContext` 는 Q7 에, ThreadFlags·KeSuspendThread 동적 확인은 Q19 에 출처만 추가
- 2026-10-19: 사용자 "F 먼저" → 하네스 묶음 F (sast/03 스크립트 문제 1~3) 사용자 검토 후 커밋: F-1 CFG `indirect:cfg:` `a7a7c92`, F-2 ICF `alias:` (Ordinal_/$thunk$/Nt·Zw 제외) `41bdb35`, F-3 조각 `frag:` `41b7428`. ntoskrnl·ntdll 재export 시험 통과. 그다음 Q2 시작 (sast/04)
- 2026-10-19: Q2 끝 (run10, sast/04). 새 할 일 Q35(동적)·Q36(정적) 맨 아래 추가 (기존 항목과 겹침 없음). 그림 1 정적 상자 8개 (요청 5~7 보다 1개 많음, MmMapViewOfSectionEx 두 번 = 상자 둘이라 그대로 둠)
- 2026-10-19: 사용자 "너무 빠르다" → 속도 규칙·해설 담당·별칭 표 제안. 사용자: 중복 이름은 추천대로, 해설에서 "스스로 확인 질문"은 뺌, 나머지 동의
- 2026-10-21: 하네스 묶음 H (바퀴 사이 멈춤, 주제 하나씩) `e568e3d`, I (explainer·run-explainer, runN-키워드-해설.md) `a09703d`, J (pdb_aliases.py 별칭 표, P19) `bdedf48`. ms/sym/*.aliases.tsv 생성 (커밋 안 함). 다음: run9·run10 해설서 채워 넣기, Q3 는 사용자 "다음" 뒤
- 2026-10-21: 사용자 "바탕 개념 절은 너무 기초적" → 하네스 묶음 K (해설서 양식에서 바탕 개념 절 제거, 채팅 설명 순서도) `a0f4164`. run9·run10 해설은 그대로
- 2026-10-21: 사용자 질문 "정적 전체 지도를 먼저 뽑는 게 빠를까?" → 비교 설명 후 사용자 결정: Q3 전에 지도 크기 재기 실험 (Q37). 뿌리 nt!NtCreateUserProcess / ntdll!LdrInitializeThunk, 끊기는 노드엔 이유. 도구는 하네스 밖 실험용 스크립트 (쓸 만하면 그때 하네스 검토)
- 2026-10-21: Q37 끝 (sast/05). 결론 — 정적 전체 지도는 커널 끝까지 11688개라 읽을 목록으론 못 씀, 흐름도 함수는 커널 깊이 5·ntdll 깊이 7 안쪽, 공용 도구(caller≥20)로 잘린 흐름도 함수 없음. 사용자: "지도는 색인, 분석은 지금처럼" → 하네스 묶음 L (CallGraphMap.java + run_ghidra.sh map) 검토 OK, 시험 통과
- 하네스 후보 (나중에 검토): CallGraphMap/ExportFunctions 의 `cfgSource()` 가 외부 주소 참조를 `@0xfffffffe…` 로 찍음 (커널 2곳, ntdll 7곳)
- 2026-10-21: 하네스 묶음 L 커밋 `6159550` (CallGraphMap.java, run_ghidra.sh map, 지도 = 색인)
- 2026-10-21: Q3 끝 (run11, sast/06). Q38(동적) 맨 아래 추가, Q4·Q7 에 출처 추가. sast/06 이 찾은 스크립트 구멍(함수에 안 속한 코드 덩어리 안 호출이 _calls.tsv·지도에서 빠짐) 은 바퀴 끝난 뒤 사용자에게 따로 제안
- 2026-10-21: 사용자 승인 → 하네스 묶음 M (주인 없는 코드 덩어리를 본래 함수 몫으로 읽기, ExportFunctions·CallGraphMap). 재추출 결과 이전 바퀴 숫자 변화 없음 (NtCreateUserProcess 83, PspAllocateProcess 144 등 그대로), PspAllocateThread 만 37→49줄 (run11 에 이미 표로 있음) → run9·10·11 정정 필요 없음. 지도 커널 기준20 5694→5757, 흐름도 함수 41개 변화 없음. .pdata 대조 720/720
- 2026-10-21: 하네스 묶음 M 커밋 `cb41881`
- 2026-10-21: Q4 끝 (run12, sast/07). 그림 1 의 nt 함수(Q1~Q4) 끝. 단계 1 남은 처음 항목: Q5·Q6·Q7. Q39(동적) 맨 아래 추가. flow.md 2.2 "+0x598 에서 다른 길" 정정 (실패 길도 +0x59e 로 돌아와 KeReadyThread)
- 2026-10-21: Q5 가 두 모듈(ntoskrnl, kernelbase)에 걸쳐 커서 규칙 4 대로 그 자리에서 Q5a(nt)·Q5b(kernelbase) 로 나눔. 원래 Q5 줄: | Q5 | 2.2 첫 스레드를 풀어 주는 함수 (nt 쪽 이름, `_index.tsv`로 확인) + 부모 쪽에서 그걸 부르는 곳 (`KERNELBASE!CreateProcessInternalW` 등, 이름 확인 필요) | 1 | 정적 (ntoskrnl, kernelbase) | 처음 (4장 2.2) | 대기 |
- 하네스 후보 (나중에 검토): (1) `_index.tsv` 는 Ghidra 가 고른 이름 하나만 담는다 — PDB 이름 `KeWaitForSingleObject` 가 내보내기 이름 `KeWaitForMutexObject` 로 잡혀 `_index.tsv` 로 못 찾음 (sast/08). (2) ExportFunctions 가 Ghidra 함수의 떨어진 두 번째 범위를 머리 표시 없이 큰 `+0x..` 로 찍음 (`KiSchedulerApc +0x4106d8`, sast/08)
- 2026-10-21: Q5a 끝 (run13, sast/08). Q19 에 출처 추가, Q40(정적) 맨 아래 추가. KernelBase analyze 끝 (함수 6448, SetPdb OK) — Q5b 준비 됨
- 하네스 후보 (나중에 검토): (3) windbg-kd-runbook pitfalls — `nt!KiSchedulerApc`·`nt!KeResumeThread` 는 아주 자주 불리니 test.exe 조건 필수 (flow-writer 제안, run13)
- 2026-10-22: 사용자 "Q5b 전에 하네스 후보 먼저, N2 → N1 → N3 순서". 하네스 묶음 N2 (pdb_aliases.py --where, 이름 확인 규칙) 커밋 `f528f34`
- 2026-10-22: 하네스 묶음 N1 (장부 숫자 순 정렬 sort_calls.py, 조각 JMP frag, 떨어진 범위 머리, CFG 출처 stack[…]/import:) 커밋 `18687ae`. ④ 는 처음 '외부 주소'로 잘못 진단 → 시험에서 스택 칸으로 밝혀져 사용자 승인 후 고침
- 2026-10-22: 하네스 묶음 N3 (reference.md `.c` 읽을 때 주의, pitfalls P4 예) 사용자 OK — 커밋 해시는 아래 줄에. 이로써 쌓였던 하네스 후보 7개(G1~G4, sast/08 (1)(2), run13 (3), Q37 외부 주소) 정리 끝
- 2026-10-22: 하네스 묶음 N3 커밋 `3e1575b`
- 2026-10-22: Q5b 끝 (run14, sast/09). 2.2 정적으로 완성: KERNELBASE 가 ThreadFlags 항상 1 → 커널 KeSuspendThread → CSR 알림 → CREATE_SUSPENDED 없으면 NtResumeThread. Q19 출처 추가, Q41(정적, 작음) 추가
- 하네스 후보 (나중에 검토): (4) SEH 정리 함수(`$fin$…`, `$filt$…`)는 호출로 닿지 않아 지도·.asm 에 안 들어옴 — reference.md "지도 > 읽을 때 주의" 에 한 줄 (sast/09 제안)
- 하네스 후보 (나중에 검토, sast/10): (5) .asm 에 함수 범위 안 읽지 않은 틈 표시 없음 (`PspUserThreadStartup +0x80~+0x90`, `KxStartUserThread +0x22e~+0x345`) (6) `KxStartUserThread +0x229 → FUN_1406afeac` 같은 가짜 호출이 일반 호출로 적힘 (7) 조각 오프셋의 함수 시작 기준 환산값이 .asm 에 없음 (8) `--where` 가 PDB 함수를 덮는 Ghidra `FUN_` 을 안 알려 줌 (9) reference ".c 읽을 때 주의" 에 MOV CR8·SWAPGS·IRETQ 가 .c 에 안 나온다는 줄
- 2026-10-22: Q6 끝 (run15, sast/10). 2.3 커널 → 유저 전환 = IRETQ [정적]. run13 정정 2건(이름표, CL 인자). Q7 출처 추가
- 하네스 후보 (나중에 검토, run15 flow-writer): (10) pitfalls — IRETQ 줄 자체(Kx +0x3e1, KiKernelExit +0xa5)에 멈춤점 금지, 유저 진입 값은 KiStartUserThread+0x28 에서 `dq @rbp+0xe8 L5` (11) KxStartUserThread 안 주소는 WinDbg 에 KiStartUserThread+(x-0x60) 로 보임 [추측]
- 2026-10-22: 사용자 결정 — Q7 먼저, 단계 1 끝나는 시점에 하네스 후보 8개((4)~(11), 묶음 O1·O2·O3)를 한 번에 처리
- 하네스 후보 (단계 1 끝 묶음에 추가, sast/11): (12) .asm 전역 변수 참조에 PDB 공개 이름 주석 (`; PsNtdllExports+0x8`) (13) "이 전역을 누가 읽고 쓰나" 찾는 도구 (`run_ghidra.sh refs`) — `PsNtdllExports` 쓰기는 INIT 표 포인터를 거쳐 코드 참조로 안 나옴 (14) `indirect:cfg:?` 를 레지스터+오프셋 표기로. (sast/11 의 "읽지 않은 틈" 은 (5) 와 같음)
- 2026-10-22: Q7 끝 (run16, sast/11). **단계 1(nt 지도) 처음 항목 Q1~Q7 모두 끝** (run9~run16). 남은 단계 1 쪽 정적 후보: Q32·Q33·Q36·Q40·Q41 (맨 아래에 붙은 것). 사용자 결정대로 다음은 하네스 후보 묶음
- 하네스 후보 (단계 1 끝 묶음에 추가, run16 flow-writer): (15) pitfalls P4 예에 `PspInitializeThunkContext` (모든 프로세스의 모든 유저 스레드가 거침)
- 2026-10-22: 하네스 후보 12개를 O3 → O2 → O1 순서로 (사용자 OK). 묶음 O3 (.c 에 안 나오는 명령, P4 예, P20, Kx 이름 환산) 커밋 `a9dd0b0`
- 2026-10-22: 다음 flow-writer 때 정정 (O2 refs 시험에서 발견): flow.md 2곳·run16 해설 1곳의 `NtCreateThreadEx +0x1c1`, `PspWow64InitThread +0x73` → 명령 시작은 `+0x1bf`, `+0x71` (objdump 확인)
- 2026-10-22: 하네스 묶음 O2 (예외 정리 함수 seh:, refs 명령 RefsTo.java, --where 품은 함수, sort_calls seh) 커밋 `b3a16c7`
- 2026-10-22: 묶음 O1 시험 실패 (⑥ 가짜 호출 규칙이 진짜 호출을 지움 — LdrInitializeThunk +0x9 등, ntdll 지도 붕괴; ⑫ 커널 GS 상대 접근에 TEB 이름; ⑤ 거짓 틈 9개). 커밋 전이라 하네스·장부·지도를 되돌림. 사용자 결정 (가): 통과한 ⑦⑭ 만 O1a 로 먼저, ⑤⑥⑫ 는 기준을 새로 짜서 O1b 로 다시 검토
- 2026-10-22: 하네스 묶음 O1a (⑦ 조각 오프셋 `(= 함수 +0x..)`, ⑭ cfg 출처 `[레지스터+오프셋]` — `MOV RAX,[메모리]` 꼴만, JMP·RET 앞에서 멈춤) 커밋 `db4607b`. 1차 시험의 거짓 라벨(⑭ JMP 넘음·XOR·레지스터 꼴)을 고쳐 재시험 통과 (사용자 OK). 알려진 한계 `MiValidateSectionCreate +0x1f050f` (Jcc 로만 들어오는 갈래)는 사용자 결정으로 reference.md 에 적고 그대로 둠
- 2026-10-22: 하네스 묶음 O1b 커밋 `c5bd70c` (사용자 OK). ⑥ FAKECALL = 목적지 첫 명령 ADD RSP,8 (RSB 채우기 사다리, 장부에서 KxStartUserThread +0x229·SwapContext +0x213 빠짐), ⑤ .asm 끝 읽지 않은 틈 목록 (29개 원본 바이트 일치, 사다리 N칸 표시), ⑫ 전역 변수 PDB 이름 주석 (메모리 피연산자·IMPORTED 이름·0x100 안, GS:/FS:·??_C@ 제외). 시험 3번 (1차: ⑤ 틈 2개 놓침·⑫ 엉뚱한 이름 → 고침, 2차 통과, 3차 ??_C@ 제외 확인). 이로써 단계 1 끝 하네스 후보(O1·O2·O3) 모두 처리
- 2026-10-22: Q8 끝 (run17, sast/12 정적만). 동적 확인거리는 Q43 으로 맨 뒤에
- 하네스 후보 (나중에 검토, sast/12): (16) 평범한 `CALL RAX` 도 바로 앞 `MOV RAX,[mem]` 에서 이름을 찾아 `indirect:<이름>` 으로 (지금은 `indirect:?`, 예: `_LdrpInitialize +0x9a` = `g_LdrpWow64LdrpInitialize`)
- 하네스 후보 (나중에 검토, run17 flow-writer): (17) windbg-kd-runbook pitfalls — `LdrpInitialize` 이름이 0xe1448 과 0x8bf8c(`_LdrpInitialize`, 콜스택 표시 이름) 에 걸치니 `bp ntdll!LdrpInitialize` 대신 `ntdll+rva` 로 건다
- 2026-10-22: Q9 가 커서 (`LdrpInitializeProcess` 7750바이트, 호출 169곳·함수 113개 — 지금까지 최대 `PspAllocateProcess` 144곳보다 큼) 규칙 4 대로 그 자리에서 Q9a(`+0x0~+0x12dd`, 95곳)·Q9b(`+0x12de~끝`, 74곳) 로 나눔. 경계 = `+0x12de LdrpInitParallelLoadingSupport` (모듈 목록 쪽 시작). 원래 Q9 줄: | Q9 | 2.4 `ntdll!LdrpInitializeProcess` callee (크면 그 자리에서 Q9a/Q9b로 나눔) | 2 | 정적 (ntdll) | 처음 (그림 2) | 대기 |. Q9a 시작 (sast/13)
- 2026-10-22: Q9a 끝 (run18, sast/13 정적만). 호출 수 정정 — 앞쪽 94 / 뒤쪽 75 (경계 `+0x12de` 는 뒤쪽). Q44(동적)·Q45(정적) 맨 아래 추가, Q9b·Q10·Q16 에 출처 추가. flow.md 그림 2 `+0x1bb6` → ret `+0x1bbb` 정정 (run18)
- 하네스 후보 (나중에 검토, run18 flow-writer): (18) windbg-kd-runbook pitfalls — 라벨·멈춤점 오프셋이 call 자리인지 ret 자리인지 섞지 않는다 (`+0x1bb6` call / `+0x1bbb` ret, 확인은 run8 처럼 `ub +0x1bbb`)

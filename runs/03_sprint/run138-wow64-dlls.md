# 2026-11-23 (작성일, 정적 분석 2026-11-23) run138 — WoW64 (x86) 초기화의 64비트 쪽 DLL 넷 (Q253b, 정적만): 64비트 ntdll `LdrpLoadWow64` 는 **`…\System32\wow64.dll` 하나만** `LdrpLoadDll(…, 0x800, …)` 로 올리고 (wow64base · wow64win · wow64con 은 그 **정적 import** 로 따라온다), 이름 **6개** 를 `LdrGetProcedureAddress` 로 찾아 ntdll 전역 6칸에 쓴다. `g_LdrpWow64LdrpInitialize` = **`wow64!Wow64LdrpInitialize`** — 처음 한 번 `ProcessInit` (CPU dll 을 레지스트리 `…\Wow64\x86` 로 고르고, 키가 없으면 `wow64cpu.dll`) 뒤 32비트 문맥 `Eip` 를 32비트 ntdll 의 `LoaderInitRoutine` (64비트 ntdll `LdrSystemDllInitBlock +0x18`) 으로 놓고 `BTCpuSimulate` 에 맡겨 **돌아오지 않는다** (예외: 시작 주소가 64비트 PE 모듈 안). wow64cpu 는 **`IRETQ` (첫 진입) · `JMPF m16:32` (CS `0x23`)** 로 32비트에 들어가고 **`JMPF` (CS `0x33`)** 로 돌아와 `Wow64SystemServiceEx` (번호 비트 12~13 = 표 4개, `whNt*`) 로 64비트 `Nt*` 를 잇는다

> **고쳐 읽기 (run139, sast/135 — Q253a)**: 아래 "CPU 영역 … 이 칸에 쓰는 줄은 wow64 · wow64cpu 에 없다 → 커널이 지음 `[추측]`" (요약 A2 · 2절 표 `+0x0` · `+0x2` 줄 · TEB64 `+0x1488` 줄) 은 맞다 `[정적]` — 자리는 `PspSetupUserStack +0x153 PspWow64SetupCpuArea` (64비트 스택 맨 위를 잘라 냄), **TEB64 `+0x1488` ← 영역** 은 `PspWow64InitThread +0xae`, **`+0x0` ← `0x014c0000`** (플래그 워드 처음 값 0 · 기계 `0x14c`) 은 `+0xbf`, `+0x4` x86 `CONTEXT` 는 `+0xdc RtlInitializeExtendedContext2` · `PspWow64InitThreadGuestx86` (`Eip` = 32비트 `RtlUserThreadStart`, CS `0x23` · SS `0x2b` · FS `0x53`). 그리고 `Ntdll32LoaderInitRoutine` (64비트 블록 `+0x18`) = `PsWowX86SharedInformation +0x00` 사본 = 32비트 `LdrInitializeThunk` `[정적: 표 B-0 이어 읽기]`. → `runs/03_sprint/run139-wow64-kernel.md`

> **고쳐 읽기 (run140, sast/137 — Q253c)**: 아래 `Wow64SetupInitialCall` 의 "`Eip` ← 블록 `+0x18` 값 · `[Esp+4]` = `CONTEXT32` 사본 · `[Esp+8]` = 32비트 ntdll 기준" 은 **받는 쪽 코드로도 맞다** `[정적]` — 32비트 ntdll (`SysWOW64\ntdll.dll`, PDB `wntdll.pdb`) `LdrInitializeThunk` (export rva `0x34230`) `+0x8 ECX = [EBP+8]` (문맥) · `+0x5 EDX = [EBP+0xc]` (기준). "32비트 `Nt*` 스텁이 어느 쪽을 부르는지는 Q253c" → 스텁 508개는 `_Wow64SystemServiceCall` = `JMP [Wow64Transition]`, `fs:[0xc0]` 직접 호출은 `NtQueryInformationProcess` 하나 (기준 윗바이트 `0x4b` 일 때만). 그리고 sast/136 결과서 "다음 요청 후보 1 ④" 의 칸 번호 표는 **블록 `+0x38` = `RtlUserThreadStart` (확정) · `+0x40` = `RtlpQueryProcessDebugInformationRemote` · `+0x48` = 이름 없는 32비트 ntdll 기준 주소 · `+0x50` = `LdrSystemDllInitBlock` · `+0x58` = `RtlpFreezeTimeBias`** 로 바로잡는다 ("`[7]` (+0x50, 미사용)" 아님). 이 문서가 쓴 wow64 읽기 칸 (`+0x18 ~ +0x38` · `+0x48`) 은 그대로 맞다. → `runs/03_sprint/run140-wow64-ntdll32.md`

> **고쳐 읽기 (run152, sast/147 — Q287, 좁힘)**: 아래 3-3 표 "부르기" 줄 (원래 `:272`, 지금 `:274`) 의 "`whNt*` 는 … **64비트 ntdll `Nt*` 를 import 해서 부른다**" 와 나무 (원래 `:320`, 지금 `:322`) 의 "`whNt*` → `ntdll!Nt*` ⇒ syscall" 은 **대부분의 썽크에만** 맞다 `[정적]` — **`whNtWow64CsrClientConnectToServer` (nt 표 칸 `0x1e9`) 는 `+0x1c` 에서 64비트 ntdll 의 `CsrClientConnectToServer` (Nt 가 아닌 함수, import 칸 `0x3c5f8`) 를 부르고**, 그 안 `CsrpConnectToServer` 가 syscall 여섯 (`NtOpenSection` · `NtCreateSection` · `NtConnectPort` · `NtClose` · `NtMapViewOfSection` · `NtClose`) 을 낸다. `whNtContinue` → `Wow64NtContinue` (run140) 처럼 `Nt*` 를 바로 안 부르는 썽크도 있다. 표 · 범위 검사 · 번호 비트 줄은 그대로 맞다 — run152.

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator-AR 요청** (갈래 AR "x86 exe 를 진짜 WoW64 로", 원 요청 `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` 의 **A 절**) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 x64 라 이 갈래를 안 탄다 — `LdrpInitializeProcess +0x1766` 의 `UseWOW64` 가 run44 사진 0 `[관찰]` (flow.md 2.4 run44 "갈림 값", run106), EPROCESS `WoW64Process : (null)` (`ms/logs/04_baggage-read_P1a.txt:160` · `_P2.txt:160`, run106 이 맞춘 줄). 그림 2 에는 `UseWOW64` 갈래 노드가 없어 **그림은 안 바꿨다.** flow.md 는 기준 (ntdll 줄 · 새 PDB 줄 둘 · 근거 줄) · 2.4 `_LdrpInitialize` · `LdrpInitializeProcess` 의 WoW64 줄 몇 개 · 3장 끝 새 절 · 4장 · 5장만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q253b** (AR01 A 절. 사용자 결정 2026-11-23 "다 끝나면 이것도 바로 이어서" — 요청 차례 C → A → B 의 둘째. C = sast/135 ntoskrnl, B = sast/137 SysWOW64 ntdll 은 같은 시간에 도는 다른 바퀴). 결과서 판정 **분석성공** (A 1 ~ 5 전부, 못 닫은 몇 줄은 `[추측]` · `[공백]`).
> - 이어 쓰는 앞 run: **run17** (`_LdrpInitialize +0x9a` 간접 호출 · `g_LdrpWow64LdrpInitialize` 를 채우는 곳 `LdrpLoadWow64 +0xe8`), **run18** (`+0x5f2 UseWOW64 = WowTebOffset > 0`), **run19** (`+0x17c1/+0x17c6 LdrpLoadWow64` · `+0x17fc/+0x17fe CALL g_LdrpWow64LdrpInitialize(CONTEXT)` · `+0x1801 JMP +0x1d1b`), **run106** (`LdrpLoadWow64 +0x98` = 문맥 비트 `0x800` 의 뿌리), **run82** (`KiUserExceptionDispatcher +0x1` `Wow64PrepareForException` rva `0x1e70e0`, 표 칸 `0x173848`), **run121** (wow64.dll 의 `[+0xc0]` · `[+0xc4]` 쓰기는 TEB32 칸), **run48** (`PsWowX86SharedInformation +0x30`). 모두 이번 줄과 **맞는다** (아래 "환경" 버전 대조). run17 · run19 의 `[추측]` 둘이 닫혀 그 문서 맨 위에 "고쳐 읽기 (run138)" 한 줄씩 넣었다.
> - 남은 동적 → **Q260 (동적: 결과서 동적 13개 — Q253d VM 과 합침)**, 남은 정적 → **Q261 (정적: 결과서 다음 요청 후보 2~5 — wow64 `whNt*` 본문·`Wow64pCpuInitializeStartupContext`·`Wow64KiUserCallbackDispatcher` · 터보 썽크 · wow64cpu 4GB 아래 올리기 · XState CPU 영역)**. 후보 1 (32비트 ntdll 쪽) 은 **Q253c** (sast/137) 로 이미 갔다.

## 요약

- **A1 `LdrpLoadWow64`** `[정적]` — 글 `…\System32\` + `wow64.dll` (`.rdata` `0x17bf78`) 을 `LdrpLoadDll(&글, &경로, 0x800, &항목)` (`+0xa8`) 로 올린다. 표 `0x173830` (16 B × **6칸** `{이름 ANSI_STRING*, 쓸 ntdll 전역 주소}`) 을 돌며 `LdrGetProcedureAddress` (`+0xf1`) 로 `Wow64LdrpInitialize` · `Wow64PrepareForException` · `Wow64ApcRoutine` · `Wow64PrepareForDebuggerAttach` · `Wow64SuspendLocalThread` · `Wow64SuspendLocalProcess` 를 ntdll `.mrdata` 6칸 (`0x1e70c8` · `0x1e70e0` · `0x1e70d0` · `0x1e70f8` · `0x1e7118` · `0x1e7110`) 에 **직접** 쓴다 (`LdrProtectMrdata(0)` · `(1)` 사이). 끝에 모듈 참조를 한 번 내려놓는다 (성공 때도). 하나라도 실패하면 그 상태 → `LdrpInitializeProcess +0x17cc JS` → `_LdrpInitialize` → `LdrpInitializationFailure` → **`NtTerminateProcess`**.
- **A3 `Wow64LdrpInitialize`** `[정적]` (wow64 rva `0x11ed0`) — `Wow64Info` (= **PEB32 + `0x490`**) 를 TEB64 `TlsSlots[10]` (`+0x14d0`) 에 놓고 → CPU 영역 풀이 (`RtlWow64GetCpuAreaInfo(TEB64 +0x1488)`) → (처음이면) **`ProcessInit`** → `LdrProcessInitializationComplete` → exe · 32비트 ntdll 이미지 알림 (`CpuNotifyMapViewOfSection` — 여기서 32비트 ntdll 의 `Wow64Transition` 이 써짐) → `Run64IfContextIs64` → `ThreadInit` → 32비트 문맥 고치기 (`Eip ← Ntdll32LoaderInitRoutine`) → **`RunCpuSimulation` = `BtFuncs+0x20` (`BTCpuSimulate`)**, 뒤에 `INT3`.
  - **돌아오나 → 아니오 (보통 길).** 돌아오는 길은 `Run64IfContextIs64` 가 참 (시작 주소가 든 모듈이 64비트 PE · CLR 헤더 없음) 일 때 `+0x297 RET` 하나. 그래서 `LdrpInitializeProcess +0x17fe` · `+0x1801 JMP +0x1d1b` 와 `_LdrpInitialize +0x9c` 는 그 예외 갈래에서만 탄다. x86 exe 에서 그 값이 거짓인지는 실행 값이라 `[추측]` (Q260).
  - **CPU dll 고르기** — `NtOpenKey(\Registry\Machine\Software\Microsoft\Wow64\x86)` → 값 이름 = exe 파일 이름 → 없으면 기본값 → **키가 없으면 (`0xc0000034`) `wow64cpu.dll`** → `LdrLoadDll` → 함수 표 34칸 (필수 3: `BTCpuProcessInit` · `BTCpuSimulate` · `BTCpuGetBopCode`).
  - **32비트 `LdrInitializeThunk` 는 부르지 않는다.** 32비트 문맥 `Eip` 를 `LdrSystemDllInitBlock +0x18` 값 (wow64 이름 `Ntdll32LoaderInitRoutine`) 으로 놓고, 32비트 스택에 `[Esp+4] = CONTEXT32 사본`, `[Esp+8] = 32비트 ntdll 기준 주소` 를 만들어 CPU 에 넘긴다 (`Wow64SetupInitialCall`).
- **A2 wow64cpu** `[정적]` — 64 → 32: **`IRETQ`** (틀 `{Eip, 0x23, EFlags, Esp, 0x2b}`, FS `0x53` 또는 `WRFSBASE`) 와 **`JMPF m16:32 [R14]`** (`{Eip, 0x23}`, 앞에 `MOV SS,0x2b`). CPU 영역 `+0x0` 비트 0 이 켜져 있으면 `IRETQ` — **첫 진입은 `IRETQ`** (`Wow64LdrpInitialize +0x11a` 가 비트 0 을 켬). 32 → 64: 구역 `W64SVC` (`0x7000`, `EA` `JMPF ptr16:32`) 또는 `WOW64SVC` (`0x3010`, `JMPF m16:32 [esp]`), 둘 다 **CS `0x33`** → `CpupReturnFromSimulatedCode` (`0x1a38`) → 터보 표 칸 0 `ServiceNoTurbo` → `Wow64SystemServiceEx`. **`RETF` 없음.** CPU 영역 = TEB64 `+0x1488` (`TlsSlots[1]`) 이 가리킴: `{+0x0 플래그 WORD, +0x2 Machine 0x14c}` + `+0x4` x86 `CONTEXT` (`0x2cc`) + `+0x2f8` CPU 전용 포인터 + 끝 `+0x300` (XState 꺼짐 기준). **이 칸에 쓰는 줄은 wow64 · wow64cpu 에 없다** → 커널이 지음 `[추측]` (sast/135 몫).
  - `Wow64Transition` 은 **wow64.dll `CpupResolveReverseImports +0xb2`** 가 32비트 ntdll 의 그 내보낸 칸에 4바이트로 쓴다. 값 = `BTCpuGetBopCode()` = `KiFastSystemCall` (`0x7000`) 또는 `KiFastSystemCall2` (`0x3010`). 같은 값을 스레드마다 TEB32 `+0xc0` (`WOW32Reserved`) 에도 (`CpuThreadInit +0x32`).
- **A4 syscall 다리** `[정적]` — `Wow64SystemServiceEx(ECX = 번호, RDX = 32비트 인자 포인터)`. 번호 **비트 12~13 = 표** (0 nt `sdwhnt32` · 1 win32k `sdwhwin32` · 2 console `sdwhcon` · 3 base `sdwhbase`), **비트 0~11 = 칸**, 비트 16 이상 = wow64cpu 터보 번호. 표 4 × `0x30` B (`ServiceTables` `0x52030`), 칸 수 nt 509 · win32k 1497 · console 4 · base 16, 이름 꼴 `whNt*` (console 은 `wh*`). 범위 검사 (`0xc000001c`) 는 한계가 모두 `0x1000` 이라 늘 통과. wow64win 은 같은 꼴 (`sdwhwin32`).
- **A5** `[정적]` — wow64base · wow64win · wow64con 은 **wow64.dll 의 정적 import** (각 변수 하나 `sdwh*`) 라 `LdrpLoadWow64` 의 `LdrpLoadDll(wow64.dll)` 한 번에 같이 올라온다. `LoadLibrary` 글 · 레지스트리 없음. CPU dll (wow64cpu) 만 따로 `LdrLoadDll`.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔여섯째 바퀴. plan **Q253b**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 둘** (wow64cpu 6초 · wow64 12초, `SetPdb:` 줄 있음 — `ms/ghidra/wow64cpu.analyze.log` · `wow64.analyze.log`). PDB 를 **받기만** 한 것 셋 (wow64win · wow64base · wow64con — import · PE 데이터 · 이름표로 닫힘). 64비트 ntdll 은 다시 analyze 안 함.

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 이번 일 |
|---|---|---|
| wow64cpu.dll (51,128 B) | `5523FB635B93EB60C1DC4AA13C02C7991` | **처음 analyze** (함수 29 — `_index.tsv` 머리 줄 빼고). PDB 는 전에 받아 둔 것 (GUID 같음) |
| wow64.dll (356,936 B) | `AED569E2D483C3CDC0C97DEC3691CD551` | **처음 analyze** (함수 1142), PDB 처음 받음 |
| wow64win.dll (575,888 B) | `28A0615EA1DB50E56B09381B397C580E1` | PDB 받기만 (analyze 안 함) |
| wow64base.dll (59,392 B) | `DD039D5CACB5A3D52830CD0313ED18D51` | PDB 받기만 |
| wow64con.dll (113,120 B) | `EE59C3B807B7AA61ABDF5B4D4BD272621` | PDB 받기만 |
| ntdll.dll | `C3093720177DA6851519DCFFE8DF53FE1` | 기준 줄과 같음. 다시 analyze 안 함 — `^이름$` export depth 0 한 번 (`s136a`, 7개) + refs 4장 |

- 버전 대조 (결과서 "버전 확인" — 기존 run 근거와 전부 일치): `LdrpInitializeProcess +0x17c1/+0x17c6` · `+0x17fc/+0x17fe` · `+0x1801 → +0x1d1b` (run17 · 18 · 19 · 106), `+0x1bb6/+0x1bbb` · `_LdrpInitialize +0x167/+0x16c` · `LdrInitializeThunk +0x9/+0xe` (스킬 "알려진 사실"), 표 칸 `0x173848` (run82), `KiUserExceptionDispatcher +0x1` → `0x1e70e0` (run82), TEB32 `+0xc0` · `+0xc4` (run121), `LdrSystemDllInitBlock +0x48` = 32비트 ntdll 기준 (run48 `PsWowX86SharedInformation +0x30` 과 같은 자리 — 블록 `+0x18` 부터 시작이면).
- flow-writer 가 다시 본 것 (`.asm` · objdump · PE): ntdll `LdrpLoadWow64` (`+0x69 ~ +0x1b3` 전부), `LdrpInitializeProcess +0xd36 · +0xd4a · +0x17b9 ~ +0x1801 · +0xa7b`, `_LdrpInitialize +0x90 ~ +0xa9 · +0x172 · +0x215 ~ +0x252 · +0x2f7 ~ +0x31b`, `RtlWow64GetCpuAreaInfo +0x24 · +0x35 · +0x77 · +0x9d · +0x159 · +0x172 · +0x17d · +0x1a3`, `RtlWow64GetCurrentCpuArea +0x39`, `_refs/0x1e70d0 · 0x1e70f8 · 0x1e7110 · 0x1e7118 · Wow64PrepareForException`; wow64 `Wow64LdrpInitialize` (줄 전부), `ProcessInit` (표 줄 전부), `RunCpuSimulation +0x4 · +0xb · +0x12 · +0x17`, `ThreadInit +0x50 ~ +0xd0 · +0xa1`, `CpuThreadInit +0x13 · +0x32`, `CpuGetBinaryTranslatorPath` · `CpuLoadBinaryTranslator` · `CpupResolveReverseImports` · `CpuNotifyMapViewOfSection` · `Wow64SystemServiceEx` · `Wow64SetupInitialCall` · `ThunkStartupContext64TO32` · `Run64IfContextIs64` · `Wow64PrepareForException` (결과서 줄 전부); wow64cpu `RunSimulatedCode +0x0 ~ +0x6e · +0x9f · +0xb4 · +0xbd · +0x116`, `BTCpuSimulate` 전부, `BTCpuGetBopCode` 전부, `BTCpuProcessInit +0x24 · +0x43 · +0x4a · +0x7b · +0xd8`, `BTCpuThreadInit +0x3a · +0x54`, `CpupSyscallStub` 전부, `WaitForMultipleObjects +0x0 · +0x1f`, `BTCpuResetToConsistentState +0x91`; `_objdump_text_0x1000.s` `19dd` · `1a32 ~ 1a7d` · `1f83` · `1f86` + 구간 `0x1920 ~ 0x1f6f` 의 `ret` 0개 · `iretq` 5 · `jmp FWORD PTR [r14]` 10 · `retf/lret` 0 (전체), `_ndisasm_wow64svc.txt` 전부. PE (`pefile`, 일회용): 다섯 DLL 의 import 표 · `ImageBase` (모두 `0x180000000`), wow64cpu 구역표 (`WOW64SVC` `0x3000` 실행 · `W64SVC` `0x7000` 읽기 전용) · 재배치 (`0x7001` 타입 10 하나) · `0x7000` 바이트 · 내보내기, wow64 `.rdata` `0x3e480` · `0x3e530`, ntdll `.rdata` `0x17bf78`. PDB 공개 이름 (`llvm-pdbutil -publics`, wow64cpu): `CpupReturnFromSimulatedCode` (`0x1a38`) · `ServiceNoTurbo` 와 `TurboDispatchJumpAddressEnd` (둘 다 `0x1a6e`) · `KiFastSystemCall` (`0x7000`) · `KiFastSystemCall2` (`0x3010`).

## 목적

- PE_emulator 갈래 AR: x86 exe 를 **진짜** WoW64 코드로 돌린다. 길 = 32비트 ntdll → `Wow64Transition` → wow64cpu → wow64 (· wow64win) → 64비트 ntdll `syscall` → 에뮬레이터 커널. 에뮬레이터는 64비트 ntdll 초기화를 흉내 내다가 **`LdrpInitializeProcess +0x17fe` 부터 진짜 코드** 를 돌릴 계획. 요청서 `_workspace/sast/136_wow64-dlls.md` (원문 A 1 ~ 5):
  1. `LdrpLoadWow64` — DLL 이름 글 · 올리는 법 · 찾는 이름 표 (몇 칸 · 쓰는 전역) · 실패 때.
  2. wow64cpu — 64 ↔ 32 전환 명령 · 셀렉터, CPU 영역 자리와 칸, 32비트로 처음 들어가는 함수, `Wow64Transition` 을 누가 쓰나.
  3. wow64 — `g_LdrpWow64LdrpInitialize` 대상 · 차례 · CPU dll 고르기 · 32비트 `LdrInitializeThunk` 로 가는 법 · **돌아오나** · 읽는 칸 · syscall · 레지스트리.
  4. syscall 다리 — 입구 · 표 꼴 · 번호 → 표 고르기 · 실패 상태 · wow64win 꼴.
  5. wow64base · wow64con — 올라오나 · 누가.
- 쓰는 곳 (원문 "답이 오면 쓰는 곳"): AR04 (TEB32 · 스택 · CPU 영역) · AR05 (GDT · FS · 모드 바꾸기) · AR06 (ntdll 엔진 WoW64 갈래 · 은퇴 자리) · AR07 (wow64 초기화 벽 · 메모리 상한 · 예외).

## 방법

- 정적 sast/136 (결과서 `_workspace/sast/136_wow64-dlls_result.md`, **분석성공**, 스크립트 문제 없음):
  - wow64cpu `^(BTCpu.*|RunSimulatedCode|CpupSyscallStub|TurboDispatchJumpAddress.*|ThunkNone|WaitForMultipleObjects)$` depth 0 (15개, `ms/ghidra/wow64cpu.export.s136a.stdout`).
  - wow64 `^(Wow64LdrpInitialize|ProcessInit|ThreadInit)$` depth 1 (`s136a`), `CpuGetBinaryTranslatorPath` · `CpupResolveReverseImports` · `Wow64SystemServiceEx` · `Wow64pInitializeCrossProcessMechanism` · `Wow64InitializeEmulatedSystemInformation` · `GetImageName` · `Wow64pInitializeFilePathRedirection` depth 1 (`s136b`), 내보낸 6 + `Wow64KiUserCallbackDispatcher` · `Wow64pCpuInitializeStartupContext` depth 0 (`s136c`), `Wow64HandleSystemServiceError` depth 0 (`s136d`). 지도 `ms/disassemble/wow64/_map/Wow64LdrpInitialize_*` (깊이 4).
  - ntdll `^(RtlWow64GetCpuAreaInfo|RtlWow64GetCurrentCpuArea|RtlpWow64CtxFromAmd64|RtlWow64GetCpuAreaEnabledFeatures|RtlWow64GetCurrentMachine|RtlWow64GetProcessMachines|RtlWow64GetSharedInfoProcess)$` depth 0 (`s136a`), refs `0x1e70d0` · `0x1e70f8` · `0x1e7110` · `0x1e7118`. 있던 `LdrpLoadWow64.asm` · `LdrpInitializeProcess.asm` · `_LdrpInitialize.asm` 읽기.
  - **Ghidra 가 함수로 못 읽은 곳** (wow64cpu 32비트 쪽 · `RunSimulatedCode` 뒤 틈 RVA `0x1a38 ~ 0x1f6f`): `ms/disassemble/wow64cpu/_objdump_text_0x1000.s` (`.text` 전체 objdump) · `_ndisasm_wow64svc.txt` (구역 `WOW64SVC` · `W64SVC` 를 32/64비트로).
  - PE 데이터 (표 · import · 재배치 · `.rdata` 글) · PDB 이름표 (`ms/sym/*.aliases.tsv`) 는 결과서 쪽 일회용 스크립트.
- 같은 시간에 decompiler 둘이 ntoskrnl (sast/135) · SysWOW64 ntdll (sast/137) 을 돈다 (`ms/` 만) — flow-writer 는 `.asm` 을 읽기만 했고 그 결과는 이번에 쓰지 않았다.

## 결과

비유:

- **WoW64 = 큰 건물 (64비트) 안에 지은 작은 옛날식 사무실 (32비트).** 옛날식 사무실 사람은 64비트 경비실 (커널) 말을 못 알아듣는다.
- **wow64.dll = 통역사, wow64cpu.dll = 사무실 문.** 64비트 ntdll 은 통역사 한 명만 부르고 (`LdrpLoadWow64`), 통역사는 자기 조수 셋 (wow64base · wow64win · wow64con) 을 데려온다 (import).
- 통역사는 사무실을 꾸민 뒤 (`ProcessInit`) 직원을 문 안으로 들여보내고 (`BTCpuSimulate`) **자기는 문 앞을 떠나지 않는다** — 다시 돌아와 64비트 로더에게 "끝났어요" 하지 않는다 (돌아오지 않음).
- 직원이 경비실에 부탁할 일이 생기면 문 (`JMPF` CS `0x33`) 으로 나와 통역사 창구 (`Wow64SystemServiceEx`) 에 서류를 내고, 통역사가 64비트 서류 (`Nt*`) 로 바꿔 경비실에 낸다 `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄 · objdump, `ms/disassemble/{ntdll,wow64,wow64cpu}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x17c1` | `+0x17c6` | `LdrpLoadWow64(RCX = [RSP+0xc0] 시스템 디렉터리 글)` — `UseWOW64 ≠ 0` (`+0x1766` · `+0x176d JZ +0x1806`) | [정적] (run19 그대로) |
| 〃 | `+0x17fc` (`CALL RAX`) | `+0x17fe` | 간접 `g_LdrpWow64LdrpInitialize` (`+0x17ed`) = **`wow64!Wow64LdrpInitialize`** (`RCX = [RSP+0xe0]` 문맥, `+0x17f4`) — **보통 안 돌아옴** | [정적] |
| `ntdll!_LdrpInitialize` | `+0x9a` (`CALL RAX`) | `+0x9c` | 같은 간접 (나중 스레드, `RCX = R12`) — 돌아오면 `+0xa2 LdrpInitializeThread` | [정적] |
| 〃 | `+0x2f9` · `+0x304` | `+0x2fe` · `+0x309` | `LdrpInitializationFailure` · `NtTerminateProcess(-1, 상태)` (`LdrpLoadWow64` 실패가 올라온 길) | [정적] |
| `ntdll!LdrpLoadWow64` (rva `0x89194`) | `+0x69` · `+0x7a` | `+0x6e` · `+0x7f` | `RtlAppendUnicodeStringToString(buf, 인자)` · `RtlAppendUnicodeToString(buf, "wow64.dll")` | [정적] |
| 〃 | `+0x8e` | `+0x93` | `LdrpInitializeDllPath(글, 0x4001, &경로)` | [정적] |
| 〃 | **`+0xa8`** | **`+0xad`** | **`LdrpLoadDll(&글, &경로, 0x800, &항목)`** (`+0x98 MOV R8D,0x800`) | [정적] |
| 〃 | `+0xb4` | `+0xb9` | `LdrpReleaseDllPath` | [정적] |
| 〃 | `+0xc3` · `+0x143` | `+0xc8` · `+0x148` | `LdrProtectMrdata(0)` · `(1)` | [정적] |
| 〃 | **`+0xf1`** (×6) | **`+0xf6`** | **`LdrGetProcedureAddress(DllBase, 이름, 0, 출력 칸)`** — 표 `0x173830` (`+0xcd LEA R15`), `+0xd7 CMP EDI,6` | [정적] |
| 〃 | `+0x14b` | `+0x150` | `LdrpDereferenceModule(항목)` | [정적] |
| 〃 | `+0x139` · `+0x1ae` | `+0x13e` · `+0x1b3` | `LdrpLogInternal` (이름 실패 줄 `0x1109` · 올리기 실패 줄 `0x10f2`) | [정적] |
| `wow64!Wow64LdrpInitialize` (rva `0x11ed0`) | `+0xa6` | `+0xad` | `ntdll!RtlWow64GetCpuAreaInfo(TEB64 +0x1488, 0, &정보)` | [정적] |
| 〃 | `+0x123` | `+0x128` | `wil_InitializeFeatureStaging` (처음만) | [정적] |
| 〃 | **`+0x13d`** | **`+0x142`** | **`ProcessInit`** (처음만) | [정적] |
| 〃 | `+0x144` | `+0x14b` | `ntdll!LdrProcessInitializationComplete` (처음만) | [정적] |
| 〃 | `+0x15d` | `+0x164` | `NtTerminateProcess(-1, ProcessInit 상태)` (음수일 때만) | [정적] |
| 〃 | `+0x193` · `+0x1c8` | `+0x19a` · `+0x1cd` | `RtlImageNtHeader(exe)` · `CpuNotifyMapViewOfSection(exe 경로, 기준, 0, 크기, 0, 4)` | [정적] |
| 〃 | `+0x1d5` · `+0x1dd` · `+0x1e5` · `+0x20d` | `+0x1da` · `+0x1e2` · `+0x1ec` · `+0x212` | `Wow64DetectMachineTypeInternal` · `Wow64SelectSystem32PathInternal` · `RtlGetNtSystemRoot` · `RtlStringCbPrintfW("%s%s\ntdll.dll")` | [정적] |
| 〃 | `+0x220` · **`+0x258`** | `+0x227` · **`+0x25d`** | `RtlImageNtHeader(NtDll32Base)` · **`CpuNotifyMapViewOfSection(32비트 ntdll)`** | [정적] |
| 〃 | `+0x261` | `+0x266` | `Wow64pGetLegacyLpcPortName` → `+0x266 JMP +0xc3` | [정적] |
| 〃 | `+0x2ad` · `+0x2c3` · `+0x2d9` | `+0x2b4` · `+0x2ca` · `+0x2e0` | `NtTerminateProcess` (이미지 알림 · 경로 글 · 32비트 ntdll 알림 실패) | [정적] |
| 〃 | **`+0xc6`** | **`+0xcb`** | **`Run64IfContextIs64(문맥)`** — `+0xcd JNZ +0x26b` → `+0x297 RET` (돌아오는 유일한 길) | [정적] |
| 〃 | `+0xf4` | `+0xf9` | `ThreadInit(CPU 전용 포인터)` (실패면 `+0x31f RtlRaiseStatus`) | [정적] |
| 〃 | `+0x108` · `+0x111` | `+0x10d` · `+0x116` | `ThunkStartupContext64TO32` · `Wow64SetupInitialCall(&정보)` | [정적] |
| 〃 | **`+0x11d`** | **`+0x122`** (`INT3`) | **`RunCpuSimulation`** → `+0xb` (ret `+0xd`) 간접 `BtFuncs+0x20` = `wow64cpu!BTCpuSimulate`, 돌아오면 `+0x17 INT 0x29` (코드 7) | [정적] |
| `wow64!ProcessInit` (rva `0x12e3c`) | `+0x1a` · `+0x32` · `+0x48` | `+0x21` · `+0x39` · `+0x4d` | `RtlInitializeSRWLock` · `RtlCreateHashTable` · `Wow64ProtectMrdata(0)` | [정적] |
| 〃 | `+0x5a` · `+0x5f` | `+0x5f` · `+0x64` | `InitializeContextMapper` · `Wow64InitializeEmulatedSystemInformation` (`+0x33 NtQuerySystemInformation(0x3e)`) | [정적] |
| 〃 | `+0xa0` · `+0xa8` | `+0xa5` · `+0xad` | `GetImageName` · `Wow64pInitializeFilePathRedirection` (→ `InitializeWindirFullObjectName +0x80 NtOpenFile` · `+0xa7 NtQueryObject`, `+0x141 NtOpenDirectoryObject`) | [정적] |
| 〃 | **`+0x1ab`** | **`+0x1b0`** | **`CpuLoadBinaryTranslator(이미지 이름)`** | [정적] |
| 〃 | `+0x1c7` (`CALL RAX`) | `+0x1c9` | 간접 `BtFuncs[0]` = `wow64cpu!BTCpuProcessInit(이미지 이름, &CpuThreadSize)` | [정적] |
| 〃 | `+0x1e6` · `+0x1f3` | `+0x1eb` · `+0x1f8` | `Wow64pInitializeCrossProcessMechanism` (`+0x8e NtCreateSection` · `+0xdf NtMapViewOfSection`) · `Wow64ProtectMrdata(1)` | [정적] |
| `wow64!CpuLoadBinaryTranslator` | `+0x51` | `+0x56` | `CpuGetBinaryTranslatorPath` | [정적] |
| 〃 | **`+0x90`** | **`+0x97`** | **`LdrLoadDll(0, 0, &경로, &핸들)`** | [정적] |
| 〃 | `+0xcf` · `+0xf5` | `+0xd6` · `+0xfc` | `RtlInitAnsiStringEx` · `LdrGetProcedureAddress` (표 `0x3a220` 34칸 → `BtFuncs` `0x520f0`) | [정적] |
| `wow64!CpuGetBinaryTranslatorPath` | `+0xad` | `+0xb4` | `NtOpenKey(0x20019, \Registry\Machine\Software\Microsoft\Wow64\x86)` — `0xc0000034` 면 `+0xbe JZ +0x253` | [정적] |
| 〃 | `+0x122` · `+0x18d` | `+0x129` · `+0x194` | `NtQueryValueKey(값 이름 = exe 파일 이름)` · `(값 이름 "")` | [정적] |
| 〃 | `+0x20e` · `+0x27b` | `+0x213` · `+0x280` | `RtlUnicodeStringPrintf("%ws\system32\%ws")` · `RtlStringCbPrintfW("%s", "wow64cpu.dll")` | [정적] |
| `wow64!CpuNotifyMapViewOfSection` | `+0x54` · `+0xb5` | `+0x5b` · `+0xba` | `NtQueryVirtualMemory(클래스 6)` · `CpupResolveReverseImports` | [정적] |
| `wow64!CpupResolveReverseImports` | `+0x5b` | `+0x62` | `RtlFindExportedRoutineByName(이미지, "Wow64Transition")` (앞 `+0x2a` `BtFuncs+0x78` = `BTCpuGetBopCode`) | [정적] |
| 〃 | `+0x9a` · `+0xc9` | `+0xa1` · `+0xd0` | `NtProtectVirtualMemory(4 B, PAGE_READWRITE)` · (원래 보호로) — 사이 **`+0xb2 MOV [RSI],EAX`** | [정적] |
| `wow64!ThreadInit` | `+0xa1` | `+0xa6` | `CpuThreadInit` → `+0x2d` (ret `+0x2f`) 간접 `BTCpuGetBopCode` · `+0x32 MOV [TEB32+0xc0],EAX` · `+0x57` (ret `+0x59`) 간접 `BTCpuThreadInit` | [정적] |
| `wow64!Wow64SetupInitialCall` | `+0x37` | `+0x3c` | `CpuGetContext` (사본 문맥, 플래그 `0x10017`) | [정적] |
| `wow64cpu!BTCpuSimulate` (rva `0x1780`) | `+0x4` | `+0x9` | `RunSimulatedCode` → `+0x9 JMP +0xb` → `+0xb JMP +0x4` (끝없는 고리) | [정적] |
| `wow64cpu!RunSimulatedCode` (rva `0x1920`) | `+0x6b` `JMPF [R14]` · `+0x116` `IRETQ` | — | 32비트 코드 (CS `0x23`) | [정적] |
| wow64cpu RVA `0x1a6e` (`ServiceNoTurbo`) | RVA `0x1a73` | RVA `0x1a79` | `[0x4350]` = `wow64!Wow64SystemServiceEx(ECX = EAX, RDX = R11)` → `0x1a7d JMP 0x194f` (`RunSimulatedCode +0x2f`) | [정적] (objdump) |
| `wow64cpu!BTCpuProcessInit` | `+0x7b` · `+0xc8` | `+0x82` · `+0xcf` | `NtQueryInformationProcess(0x34)` · `NtProtectVirtualMemory(W64SVC, 0x20)` → `+0xd8 CpupSystemCallFast = 1` | [정적] |
| `wow64cpu!WaitForMultipleObjects` (터보) | `+0x1f` | `+0x24` | `CpupSyscallStub` (`+0x0 MOV RAX,0x1a`) → `+0x11 SYSCALL` (KUSD `+0x308` 비트 0 이면 `INT 0x2e`) | [정적] |
| `wow64!Wow64SystemServiceEx` (rva `0x8c30`) | `+0x169` · `+0x174` · `+0x18b` | `+0x16e` · `+0x179` · `+0x190` | `whNtQueryInformationProcess` · `whNtCallbackReturn` · `whNtReadVirtualMemory` (주소 비교로 바로) | [정적] |
| 〃 | **`+0x199`** (`CALL RAX`) | **`+0x19b`** | 나머지 `whNt*(RCX = 인자 포인터)` | [정적] |
| 〃 | `+0x1ad` (`CALL RAX`) | `+0x1af` | 로그 훅 `pfnWow64LogSystemService` (있을 때만) | [정적] |
| `wow64!Wow64PrepareForException` | `+0x5e` | `+0x63` | `CpuResetToConsistentState` (`+0x19 CMP word [문맥+0x38],0x23`) | [정적] |

### 1. `LdrpLoadWow64` — 무엇을 올리고 무엇을 찾나 (A1) [정적]

#### 1-1. 이름 표 (ntdll `0x173830`, 16 B × 6칸)

칸 모양 `{+0x0 ANSI_STRING*, +0x8 값을 쓸 ntdll 전역 주소}`. 루프: `+0xcd LEA R15,[0x173830]` · `+0xe8 MOV R9,[R15+RSI*8+8]` (출력 칸) · `+0xed MOV RDX,[R15+RSI*8]` (이름) · `+0xf1 CALL LdrGetProcedureAddress` · `+0xd7 CMP EDI,6` / `+0xda JNC`. Ghidra 가 `+0xcd` 옆에 단 `RtlpTpWorkTaskCallbacks+0x10` 은 앞쪽 가까운 이름일 뿐이다 (결과서 "알아 둘 것").

| 칸 | 표 주소 (출력 칸 포인터) | 이름 글 | 쓰는 전역 (ntdll rva · PDB 이름) | 그 전역을 읽는 곳 (`_refs`) |
|---|---|---|---|---|
| 0 | `0x173830` (`0x173838`) | `Wow64LdrpInitialize` | `0x1e70c8` `g_LdrpWow64LdrpInitialize` | `_LdrpInitialize +0x90` · `LdrpInitializeProcess +0x17ed` |
| 1 | `0x173840` (`0x173848`) | `Wow64PrepareForException` | `0x1e70e0` `Wow64PrepareForException` | `KiUserExceptionDispatcher +0x1` (run82) |
| 2 | `0x173850` (`0x173858`) | `Wow64ApcRoutine` | `0x1e70d0` `Wow64ApcRoutine` | `KiUserApcDispatcher +0x6e` |
| 3 | `0x173860` (`0x173868`) | `Wow64PrepareForDebuggerAttach` | `0x1e70f8` `g_LdrpWow64PrepareForDebuggerAttach` | `DbgUiRemoteBreakin +0x38` |
| 4 | `0x173870` (`0x173878`) | `Wow64SuspendLocalThread` | `0x1e7118` `g_LdrpWow64SuspendLocalThread` | `RtlpWow64SuspendThreadWorker +0x0` |
| 5 | `0x173880` (`0x173888`) | `Wow64SuspendLocalProcess` | `0x1e7110` `g_LdrpWow64SuspendLocalProcess` | `RtlpWow64SuspendLocalProcess +0x0` |

- 여섯 이름 모두 wow64.dll 내보내기에 있다 (결과서: 번호 16 · 21 · 7 · 20 · 29 · 28).
- 이 6칸의 **파일 처음 값은 0** 이고 쓰는 코드 줄은 없다 — `LdrGetProcedureAddress` 가 출력 포인터로 쓴다. run82 의 "64비트에선 `Wow64PrepareForException` 0" 과 맞는다 (test.exe 는 `LdrpLoadWow64` 를 안 탐).

#### 1-2. 올리는 법

1. 글 만들기: `+0x69 RtlAppendUnicodeStringToString(buf, 인자)` → `+0x6e LEA RDX,[0x18017bf78]` (UTF-16 `wow64.dll`, flow-writer `pefile` 로 다시 읽음) → `+0x7a RtlAppendUnicodeToString` = `…\System32\wow64.dll`. 인자는 `LdrpInitializeProcess +0xd36 ~ +0xd4a` (`RtlAppendUnicodeStringToString` 두 번) 가 `[RSP+0xc0]` 에 만든 `UNICODE_STRING` (끝 `SlashSystem32SlashString`), `+0x17b9 LEA RCX,[RSP+0xc0]` 로 넘김. 앞부분이 무엇인지는 안 따라감 `[공백]`.
2. `+0x8e LdrpInitializeDllPath(글, 0x4001, &경로)` → **`+0xa8 LdrpLoadDll(&글, &경로, 0x800, &항목)`** → `+0xb4 LdrpReleaseDllPath`. `0x800` 은 run106 의 문맥 비트 `0x800` 뿌리와 같은 줄 (`+0x98`).
3. `+0xc3 LdrProtectMrdata(0)` → 칸마다 `LdrGetProcedureAddress([항목+0x30] = DllBase, 이름, 0, 출력 칸)` → `+0x143 LdrProtectMrdata(1)` → `+0x14b LdrpDereferenceModule(항목)`. **성공 때도 참조를 한 번 내려놓는다.** 그 뒤에도 wow64.dll 이 남는지는 `LdrpLoadDll` 쪽 참조 규칙이라 `[공백]`.

#### 1-3. 실패 때

| 실패 | 이 함수 | 부른 쪽 |
|---|---|---|
| `LdrpLoadDll` 음수 | `+0xbb JS` → `+0x17e ~ +0x1ae LdrpLogInternal` (줄 `0x10f2`) → `+0x1b3 JMP +0x150` (참조 내리기 건너뜀) → 그 상태 | `LdrpInitializeProcess +0x17c6 MOV [RSP+0x50],EAX` · `+0x17cc JS` → `+0xa7b` (함수 끝, 그 상태 반환) |
| `LdrGetProcedureAddress` 음수 | `+0xfa JS` → `+0x100 ~ +0x139 LdrpLogInternal` (줄 `0x1109`, 실패한 이름 포함) → 루프 끝 → `+0x13e` 이후 (`LdrProtectMrdata(1)` · 참조 내리기) → **마지막 실패 상태 `EBX`**. 이미 쓴 앞 칸은 되돌리지 않음 | 〃 |
| — | — | `_LdrpInitialize` 에서 음수 → `+0x2f7` → **`+0x2f9 LdrpInitializationFailure` → `+0x304 NtTerminateProcess(-1, 상태)`** |

- 성공이면 `+0x17d2 ~ +0x17dd` (`BeingDebugged` 면 `LdrpDoDebuggerBreak`) → `+0x17dd LdrInitState = 3` → `+0x17e7 BTR.LOCK [PEB+0x50],1` (`ProcessInitializing` 끔) → **`+0x17fc CALL g_LdrpWow64LdrpInitialize`** → `+0x17fe NOP` → `+0x1801 JMP +0x1d1b`.

### 2. `Wow64LdrpInitialize` 차례 (A3) [정적]

입구 `RCX` = 커널이 맡긴 64비트 `CONTEXT*` (R14 에 보관). `Wow64Info` = **PEB32 + `0x490`** (`sizeof(_PEB32)` 가 정확히 `0x490` — 결과서 `pdb_struct.py`).

| 차례 | 줄 | 하는 일 | 읽는 칸 · syscall | 실패 때 |
|---|---|---|---|---|
| 1 | `+0x4c ~ +0x8f` | TEB32 = TEB64 + `WowTebOffset` (`+0x55`), PEB32 = `[TEB32+0x30]` (`+0x69`), **`Wow64InfoPtr` ← PEB32 + `0x490`** (`+0x79` · `+0x7f`), **TEB64 `+0x14d0` (`TlsSlots[10]`) ← 그 주소** (`+0x8f`) | TEB64 `+0x180c` · TEB32 `+0x30` | `WowTebOffset == 0` → `+0x5e JZ +0x299` (아래 "flow-writer 대조") |
| 2 | `+0x9f ~ +0xb4` | `RtlWow64GetCpuAreaInfo(TEB64 +0x1488, 0, &정보)` (3절 3-2) | TEB64 `+0x1488` | 음수 → `+0x2a1` `INT 0x29` (코드 `0x4b`) |
| 3 | `+0xba` · `+0xc1` | 바이트 `0x4c736` (Ghidra 표시 `CachedMachine+0x2`, 별개 칸) 이 0 이면 **프로세스 갈래** (4 ~ 10), 아니면 11 로 | — | — |
| 4 | `+0x123 ~ +0x139` | `wil_InitializeFeatureStaging`; `Wow64Info +0x20 ← 0x8664`, `+0x22 ← 정보.Machine` (`[RBP+0x24]`) | | |
| 5 | **`+0x13d`** | **`ProcessInit()`** (2-1) | | 음수 → `+0x156 JNS` 안 탐 → `+0x15d NtTerminateProcess(-1, 상태)` |
| 6 | `+0x144` | **`LdrProcessInitializationComplete()`** — ntdll 25 B 썽크 (`_index.tsv` 크기 25), 결과서: `LdrpInitializationComplete(&LdrpProcessInitialized, &LdrpInitCompleteEvent, 0x1484)` 로 넘어감. **5 의 상태를 보기 전에** 부른다 | | |
| 7 | `+0x179` | 바이트 `0x4c736` ← 1 | | |
| 8 | `+0x180 ~ +0x1c8` | exe 알림: 글 = PEB64 `ProcessParameters.ImagePathName` (`+0x20` → `+0x60/+0x68`, 비면 `.rdata` `0x3f038`), `RtlImageNtHeader(PEB64 +0x10)` → `SizeOfImage` 올림 → `CpuNotifyMapViewOfSection(글, 기준, 0, 크기, 0, 4)` | PEB64 `+0x10` · `+0x20` | 음수 → `+0x1cf JS` → `+0x2ad NtTerminateProcess` |
| 9 | `+0x1d5 ~ +0x212` | `Wow64DetectMachineTypeInternal` (→ `0x14c`) → `Wow64SelectSystem32PathInternal` (→ `\SysWOW64`) → `RtlGetNtSystemRoot` → `RtlStringCbPrintfW` = `<시스템루트>\SysWOW64\ntdll.dll` | | 음수 → `+0x214 JS` → `+0x2c3 NtTerminateProcess` |
| 10 | `+0x21a ~ +0x266` | **32비트 ntdll 알림** `CpuNotifyMapViewOfSection(글, NtDll32Base, 0, 크기, 0, 4)` — 여기서 `Wow64Transition` 이 써짐 (3-3) → `Wow64pGetLegacyLpcPortName` (환경변수 `_CLUSTER_NETWORK_NAME_` · `COMPUTERNAME`, 결과서) → `+0x266 JMP +0xc3` | `NtDll32Base` 전역 (`0x52018`) | 음수 → `+0x25f JS` → `+0x2d9 NtTerminateProcess` |
| 11 | **`+0xc6`** | **`Run64IfContextIs64(문맥)`** — 참이면 `+0xcd JNZ +0x26b` → **`+0x297 RET` (돌아옴)** | 문맥 `+0xc0` (`R9`) 이 표시 `0xF0E0D0C0A0908070` 이면 `+0xf8` (`Rip`), 아니면 `+0x80` (`Rcx`) 를 시작 주소로 → PEB64 `Ldr` (`+0x18` → `+0x10` 목록) 에서 그 주소가 든 모듈이 **64비트 PE · CLR 헤더 없음** 이면 참 (결과서) | — |
| 12 | `+0xd3 · +0xd7` | CPU 영역 `+0x0` 에 비트 1 (`OR [RAX],2`) | `정보 +0x18` | |
| 13 | `+0xda ~ +0xf1` | `CpuThreadSize` (`0x4c968`, wow64cpu 는 0) 가 0 이 아니면 스택에 잡고 0 으로 (`+0x2ea ~ +0x318`); `[정보+8]` (= CPU 영역 `+0x2f8`) ← 그 주소 (wow64cpu 는 0) | | |
| 14 | `+0xf4` | **`ThreadInit(CPU 전용 포인터)`** → `+0xa1 CpuThreadInit` → `BTCpuGetBopCode` 값을 **TEB32 `+0xc0`** 에 (`+0x32`) → `BTCpuThreadInit`. 그 밖 TEB64 `+0x108` → TEB32 `+0xc4` (`ThreadInit +0x60 · +0x66`), TEB64 `+0x1747` → TEB32 `+0xf77` (`+0x6c · +0x72`) | 소프트웨어 CPU 일 때만 `NtCreateEvent` · `TpAllocWait` (wow64cpu 는 안 탐, 결과서) | 음수 → `+0xfb JS` → `+0x31f RtlRaiseStatus` |
| 15 | `+0x101 ~ +0x116` | `ThunkStartupContext64TO32(32비트 문맥, 64비트 문맥)` → **`Wow64SetupInitialCall(&정보)`**: 32비트 `Esp` 아래 `CONTEXT32` 사본 자리 + 호출 틀 (`[Esp+4]` = 사본 주소, `[Esp+8]` = `NtDll32Base`), **`Eip ← Ntdll32LoaderInitRoutine`** (`Wow64Info +0x8` 이 0 이 아니면 `Eip ← [Wow64Info+8]`, `Ecx ← Ntdll32LoaderInitRoutine` — `+0x58 CMP`) | `Wow64SetupInitialCall +0x15 ~ +0x76` (`+0x76 MOV [RAX+0xb8],ECX` = `CONTEXT32.Eip`) | |
| 16 | `+0x116 ~ +0x122` | CPU 영역 `+0x0` 에 **비트 0** (`+0x11a OR [RAX],1`) → **`RunCpuSimulation()`** (`+0x4 MOV RAX,[BtFuncs+0x20]` · `+0xb CALL`) → 돌아오면 `+0x12 ~ +0x17 INT 0x29` (코드 7). 부른 쪽 `+0x122 INT3` | | |

#### 2-1. `ProcessInit` 안 (wow64 rva `0x12e3c`)

| 줄 | 하는 일 | syscall · 칸 | 실패 때 |
|---|---|---|---|
| `+0x13 ~ +0x3e` | 락 `AlertByThreadIdEventLock` · 해시 표 (`+0x1a RtlInitializeSRWLock` · `+0x32 RtlCreateHashTable`) | | 실패 → `+0x20d` `0xc000009a` |
| `+0x46 ~ +0x54` | `Wow64ProtectMrdata(0)`, **`Wow64Info +0x0 ← 0x1000`** | (`NtProtectVirtualMemory`) | |
| `+0x5a` | `InitializeContextMapper` — `\KnownDlls\kernel32.dll` (+ `\KnownDlls32` 의 것) 구역을 잠깐 사상 (결과서) | `LdrGetKnownDllSectionHandle` · `NtMapViewOfSection` · `NtUnmapViewOfSection` · `NtClose` | 실패해도 계속 (결과서) |
| `+0x5f` | `Wow64InitializeEmulatedSystemInformation` — `+0x33 NtQuerySystemInformation(0x3e, 0x40 B)` → 성공이면 `Wow64pMaxAddress` ← 결과 `+0x28` | | 실패해도 계속 |
| `+0x6c ~ +0x9a` | **64비트 ntdll `LdrSystemDllInitBlock` 에서 32비트 ntdll 정보 복사** (`+0x6c` import `__imp_LdrSystemDllInitBlock`): `+0x48` → `NtDll32Base` (`+0x73`), `+0x18` → `Ntdll32LoaderInitRoutine`, `+0x20` → `…KiUserExceptionDispatcher`, `+0x28` → `…KiUserApcDispatcher`, `+0x30` → `…KiUserCallbackDispatcher` (`+0x9a`). `+0x38` 은 `ThunkStartupContext64TO32 +0x74` 가 32비트 `Eip` 와 비교 | 커널이 채운 블록 (run32) | |
| `+0xa0` | `GetImageName` — `ImagePathName` 마지막 `\` 뒤 (없으면 `Unknown Image`) | PEB64 `+0x20` | |
| `+0xa8` | `Wow64pInitializeFilePathRedirection` — `InitializeWindirFullObjectName` (`+0x80 NtOpenFile` · `+0xa7 NtQueryObject(1)` · `+0xba NtClose`) · `+0x141 NtOpenDirectoryObject` (`\KnownDlls32`, 머신 `0x14c` 칸) | | `NtOpenFile` · `NtQueryObject` 실패 → **프로세스 죽음** (결과서). `\KnownDlls32` 실패는 통과 |
| `+0xb5 ~ +0x14d` | 서비스 표 `ServiceTables` (`0x52030`, 4 × `0x30`) ← `sdwhnt32` · `sdwhwin32` · `sdwhcon` · `sdwhbase` (import 변수) | | |
| `+0x154 ~ +0x1a2` | **PEB32 에 64비트 PEB 칸 복사** (하위 32비트): `+0x1f8 ← PEB64 +0x2f8` (`+0x174 · +0x188`), `+0x200 ← +0x308` (`+0x18e · +0x194`), `+0x1e8` (0 일 때) `← +0x2d8` (`+0x19c · +0x1a2`) | | |
| **`+0x1ab`** | **`CpuLoadBinaryTranslator(이미지 이름)`** (2-2) | | 음수 → `+0x1b4 JS` → 그 상태 |
| `+0x1b6 ~ +0x1c9` | `BtFuncs[0]` = **`BTCpuProcessInit(이미지 이름, &CpuThreadSize)`** | wow64cpu (3절) | |
| `+0x1e6` | `Wow64pInitializeCrossProcessMechanism` — 이름 없는 구역 (`+0x8e NtCreateSection`, 접근 `0xf001f`) + `+0xdf NtMapViewOfSection` | | 음수 → `+0x1ef JS` → 실패 반환 |
| `+0x1f3` | `Wow64ProtectMrdata(1)` | | |

#### 2-2. CPU dll 고르기 · 올리기

- **레지스트리** (`CpuGetBinaryTranslatorPath`): `+0xad NtOpenKey(0x20019, \Registry\Machine\Software\Microsoft\Wow64\x86)` (글 `.rdata` `0x3e480`, flow-writer `pefile` 로 다시 읽음) → `+0x122 NtQueryValueKey(값 이름 = exe 파일 이름, 클래스 2)` → 없거나 REG_SZ · REG_EXPAND_SZ 가 아니면 `+0x18d` 기본값 (`""`) 으로 다시. 글에 `\` 가 없으면 (`+0x1d8 wcsrchr`) `+0x20e "%ws\system32\%ws"`.
  - **키가 없으면** (`+0xb9 CMP EAX,0xc0000034` · `+0xbe JZ +0x253`) → `+0x27b RtlStringCbPrintfW("%s", "wow64cpu.dll")` (글 `0x3e530`) → 같은 꼴 `…\system32\wow64cpu.dll`. 기본값도 못 읽으면 (`+0x19f JS`) 실패.
- **올리기** (`CpuLoadBinaryTranslator`): `+0x51 CpuGetBinaryTranslatorPath` → `+0x6d memset(BtFuncs, 0, 0x110)` → `+0x7d Wow64ProtectMrdata(0)` → **`+0x90 LdrLoadDll(0, 0, &경로, &핸들)`** → 표 `0x3a220` (34 × `0x18` B `{이름, 출력 칸, 필수 바이트}`) 로 `+0xcf RtlInitAnsiStringEx` · `+0xf5 LdrGetProcedureAddress` → `BtFuncs` (`0x520f0`, 34 × 8 B).
  - 필수 3: `BTCpuProcessInit` (`+0x0`) · `BTCpuSimulate` (`+0x20`) · `BTCpuGetBopCode` (`+0x78`). 나머지 31 은 `0xc0000139` · `0xc000007a` 면 건너뜀 (`+0x105 ~ +0x11e`). wow64cpu 는 그중 `BTCpu*` 8개만 내보낸다 (flow-writer `pefile`: `GetBopCode` · `GetContext` · `ProcessInit` · `ResetToConsistentState` · `SetContext` · `Simulate` · `ThreadInit` · `TurboThunkControl` + `TurboDispatchJumpAddressStart` · `…End`).
  - wow64cpu 자신도 `wow64.dll` 을 import 한다 (2개 — flow-writer `pefile`). 실패하면 `$fin$0` 가 `BtFuncs` 를 지우고 `LdrUnloadDll` (결과서).

#### 2-3. 돌아오나 → **아니오** (보통 길)

- ① `BTCpuSimulate` = `+0x4 CALL RunSimulatedCode` · `+0x9 JMP +0xb` · `+0xb JMP +0x4` — 끝없는 고리.
- ② `RunSimulatedCode` 머리부터 `CpupSyscallStub` 앞까지 (RVA `0x1920 ~ 0x1f6f`) **`ret` 0개** (flow-writer 가 objdump 로 다시 셈). 32 → 64 로 돌아오는 길은 모두 `CpupReturnFromSimulatedCode` (`0x1a38`) → 터보 표 → `0x1a7d JMP 0x194f` (= `RunSimulatedCode +0x2f`) 로 다시 32비트로.
- ③ `Wow64LdrpInitialize +0x122 INT3` · `RunCpuSimulation +0x12 ~ +0x17 INT 0x29` — "여기 오면 안 됨" 표시.
- **돌아오는 유일한 길 = 11 단계 `Run64IfContextIs64` 참** (`+0xcd JNZ +0x26b` → `+0x297 RET`). 그러면 `LdrpInitializeProcess +0x17fe` · `+0x1801 JMP +0x1d1b`, 또는 나중 스레드 `_LdrpInitialize +0x9c` → `+0xa2 LdrpInitializeThread` 를 탄다. x86 exe 첫 스레드에서 이것이 거짓인지는 실행 값이라 `[추측]` (Q260 2).

### 3. wow64cpu — 문 (A2) [정적]

#### 3-1. 전환 꼴

| 방향 | 자리 | 명령 | 모양 |
|---|---|---|---|
| 64 → 32 빠른 길 | `RunSimulatedCode +0x6b` (RVA `0x198b`), 터보 썽크 끝들 (모두 10곳) | **`JMPF m16:32 [R14]`** | `[R14]` = `{Eip (+0x63), 0x0023 (+0x4e)}`, 앞에 `+0x56 MOV R8D,0x2b` · `+0x5c MOV SS,R8W` · `+0x67 MOV ESP,[R13+0x48]`. `Edi Esi Ebx Ebp Eax` 복원 (`+0x37 ~ +0x47`) |
| 64 → 32 다시 싣는 길 | `RunSimulatedCode +0x116` (RVA `0x1a36`; 같은 꼴 모두 5곳) | **`IRETQ`** | 틀 `{Eip, 0x23, EFlags, Esp, 0x2b}`. 앞에 `+0x6e FXRSTOR [R13+0x50]` · (XState 면 `+0x88 RtlXRestore`) · DS/ES/GS `0x2b` · FS: KUSD `+0x28a` (`+0x9f`) 가 0 이면 `MOV FS,0x53`, 아니면 `+0xb4 MOV R8,GS:[0]` → `WRFSBASE R8` (RVA `0x19dd`) · `ECX`/`EDX` 도 |
| 어느 길로 | `+0x2f BTR [R13-0x80],0` (= CPU 영역 `+0x0` 비트 0) · `+0x35 JC +0x6e` | — | 켜져 있었으면 `IRETQ`. **첫 진입은 `IRETQ`** (`Wow64LdrpInitialize +0x11a`) |
| 32 → 64 빠른 | `KiFastSystemCall` = 구역 `W64SVC` RVA `0x7000` | **`JMPF ptr16:32` (`EA`)** → `0x7009` 64비트 `JMP [R15+0xf8]` (터보 칸 31 = `CpupReturnFromSimulatedCode`) | 파일 바이트 `ea 09 70 00 80 34 00` 에 **`DIR64` 재배치 (`0x7001`, 타입 10)** 가 걸려, 이미지가 4GB 아래면 셀렉터가 `0x34 − 1 = 0x33` (결과서 풀이 — 바이트 · 재배치는 flow-writer `pefile` 로 다시 봄) |
| 32 → 64 느린 | `KiFastSystemCall2` = 구역 `WOW64SVC` RVA `0x3010` | `push 0x33; push eax; call $+5; pop eax; add eax,0xc; xchg eax,[esp]; jmp far [esp]` = **`JMPF m16:32`** → `0x3024` (CS `0x33`) `add rsp,8` → `jmp 0x1a38` | `_ndisasm_wow64svc.txt` |
| 고르기 | `BTCpuGetBopCode` `+0x0 CMP [CpupSystemCallFast],0` · `+0x7` / `+0xe` · `+0x15 CMOVNZ` | — | 1 이면 `0x7000`, 아니면 `0x3010`. 1 로 되는 곳은 `BTCpuProcessInit +0xd8` 하나 — 조건: KUSD `+0x308` ≠ 1 (`+0x4a`) · `+0x7b NtQueryInformationProcess(0x34)` 의 "동적 코드 금지" 꺼짐 · `+0xc8 NtProtectVirtualMemory(W64SVC, 0x20)` 성공 |
| 32 → 64 뒤 | `CpupReturnFromSimulatedCode` RVA `0x1a38` | `XCHG R14,RSP` · 32비트 `Eip` (`[R13+0x3c]`) · `Esp` (`+0x48`) · `Edi Esi Ebx Ebp` · `EFlags` (`+0x44`) 저장 · `R11` = 인자 포인터 · `ECX = EAX >> 16` · `JMP [R15+RCX*8]` | 칸 0 = RVA `0x1a6e` (PDB 이름 둘: `ServiceNoTurbo` · 내보낸 `TurboDispatchJumpAddressEnd`): `ECX = EAX` · `RDX = R11` · `CALL [Wow64SystemServiceEx]` · 결과 → `[R13+0x34]` (`Eax`) · `JMP 0x194f` |
| 64비트 `syscall` (터보) | `CpupSyscallStub` RVA `0x1f70` | KUSD `+0x308 & 1` 이면 `INT 0x2e`, 아니면 **`SYSCALL`** (`+0x11`) | 터보 썽크 (예 `WaitForMultipleObjects`: `+0x0 MOV RAX,0x1a` · `+0x1f CALL`) 는 **wow64 를 안 거치고 wow64cpu 안에서 커널로 간다** |

- `RETF` 는 없다 (`retf` · `lret` 0).
- `R12` (= TEB64, `+0x10`) · `R13` (= CPU 영역 + `0x80`, `+0x20 · +0x28`) · `R14` (64비트 스택) · `R15` (터보 표 `0x41b0`, `+0x19`) 는 32비트 구간 내내 64비트 레지스터에 그대로 있어야 한다.
- **wow64cpu 는 4GB 아래에 올라와야 맞는다** — `CpuThreadInit +0x32` 가 주소 하위 32비트만 TEB32 `+0xc0` 에 쓰고, 32비트 코드가 `W64SVC` · `WOW64SVC` 를 직접 실행하며, 위 재배치 꼴이 맞으려면. 선호 기준 주소는 `0x180000000` (PE). **누가 낮게 올리나는 안 봄** `[공백]` (Q261).

#### 3-2. CPU 영역 (TEB64 `+0x1488` = `TlsSlots[1]` 이 가리킴)

64비트 ntdll `RtlWow64GetCpuAreaInfo` (머신 0 → 영역 `+0x2` 에서 읽음 — `+0x24 MOVZX EDI,[RCX+2]`, `+0x35 SUB EAX,0x14c`) 가 계산한다. KUSD `XState` 가 꺼진 경우 (`+0x77` `[0x7ffe03d8]` · `+0x9d`):

| 오프셋 | 칸 (우리 이름) | 크기 | 누가 |
|---|---|---|---|
| `+0x0` | 플래그 WORD (비트 0 = 다시 싣기, 비트 1 = ?) | 2 (`BTR` 은 DWORD) | 켬: `Wow64LdrpInitialize +0xd7` (`|= 2`) · `+0x11a` (`|= 1`) · `BTCpuResetToConsistentState +0x91` (`|= 1`). 끔: `RunSimulatedCode +0x2f`. 처음 값 = 커널 `[추측]` |
| `+0x2` | **Machine** WORD `0x14c` | 2 | 읽기만 (`RtlWow64GetCpuAreaInfo +0x24` · `RtlWow64GetCurrentCpuArea +0x39`). 쓰는 줄 wow64 · wow64cpu 에 없음 → 커널 `[추측]` |
| `+0x4` | **x86 `CONTEXT`** (`0x2cc`) — `정보 +0x0` (`+0x159 ~ +0x172`) | `0x2cc` | 아래 |
| `+0xa0 Edi` · `+0xa4 Esi` · `+0xa8 Ebx` · `+0xac Edx` · `+0xb0 Ecx` · `+0xb4 Eax` · `+0xb8 Ebp` · `+0xbc Eip` · `+0xc0 SegCs` · `+0xc4 EFlags` · `+0xc8 Esp` · `+0xcc SegSs` | 문맥 안 칸 (wow64cpu 는 `R13 = 영역 + 0x80` 기준 `+0x20 ~ +0x4c`) | 각 4 | 32 → 64 때 저장 (objdump `1a42 ~ 1a61`, `1a79`), 64 → 32 때 읽기 (`RunSimulatedCode`), `Wow64SetupInitialCall` 이 `Eip` · `Esp` · `Ecx` 고침 |
| `+0xd0` | `ExtendedRegisters` (FXSAVE 512 B) | `0x200` | `FXRSTOR [R13+0x50]` |
| `+0x2d0` | `CONTEXT_EX` (`0x20`) — XState 일 때만 | `0x20` | `BTCpuThreadInit +0x3a` → TEB64 `+0x14d8` (`+0x54`). `0xcc` 더하는 뜻은 `[공백]` |
| `+0x2f8` | CPU 전용 포인터 칸 (`정보 +0x8`) | 8 | `Wow64LdrpInitialize +0xf1` (wow64cpu 는 0) |
| `+0x300` | 끝 (`정보 +0x28`, `+0x17d`) | — | |

- **TEB64 `+0x1488` 에 쓰는 줄은 wow64 · wow64cpu 어디에도 없다** (결과서 전수, 읽는 곳 `Wow64LdrpInitialize +0x9f` · `RunSimulatedCode +0x20` 등) → 커널 (`PspWow64SetupCpuArea` 쪽) 이 만든다 `[추측]` — 이름 · 줄은 sast/135 결과에서 맞춘다.
- XState 가 켜지면 크기 · 칸이 달라진다 (결과서). 에뮬레이터가 KUSD `+0x3d8` 을 어떻게 두든 진짜 `RtlWow64GetCpuAreaInfo` 가 계산한다.

#### 3-3. `Wow64Transition` — 누가 쓰나

- 쓰는 쪽은 **wow64.dll**: `CpuNotifyMapViewOfSection +0xb5` → `CpupResolveReverseImports` `+0x2a` (`BtFuncs+0x78` = `BTCpuGetBopCode`) → `+0x5b RtlFindExportedRoutineByName(이미지, "Wow64Transition")` → `+0x9a NtProtectVirtualMemory(4 B, 4)` → **`+0xb2 MOV [RSI],EAX`** (RVA `0xbe5a`) → `+0xc9` 원래 보호로.
- `CpuNotifyMapViewOfSection` 은 이미지 `MajorSubsystemVersion` · `MinorSubsystemVersion` 이 KUSD `NtMajorVersion` · `NtMinorVersion` 과 같을 때만 부른다 (`+0x93 ~ +0xb0`, 결과서), `+0x54 NtQueryVirtualMemory(6)` 가 음수면 건너뜀 (`+0x62 JS`). 초기화 때 부르는 자리는 둘 (exe `+0x1c8`, 32비트 ntdll `+0x258`).
- 값 = `KiFastSystemCall` (`wow64cpu + 0x7000`) 또는 `KiFastSystemCall2` (`+0x3010`). 같은 값이 스레드마다 TEB32 `+0xc0` (`WOW32Reserved`) 에도. 32비트 `Nt*` 스텁이 어느 쪽을 부르는지는 **32비트 ntdll 본문** — Q253c (sast/137).

### 4. syscall 다리 (A4) [정적]

| 항목 | 값 · 모양 | 줄 |
|---|---|---|
| 입구 | `Wow64SystemServiceEx(ECX = 번호 (EAX 그대로), RDX = 32비트 스택 인자 포인터)` (rva `0x8c30`, 내보내기) | `+0x28 MOV R15,RDX` · `+0x2b MOV EDI,ECX`; 부르는 곳 wow64cpu RVA `0x1a73` |
| 번호 → 표 | `EDX = (번호 >> 12) & 3` = **표**, `EDI = 번호 & 0xfff` = **칸**. 비트 16 이상은 wow64cpu 터보 번호 (`0x1a65 ~ 0x1a67`) | `+0x84 ~ +0x8c` |
| 표 | `ServiceTables` (`0x52030`) 4 × `0x30` B: 0 nt `sdwhnt32` (wow64 자기 것) · 1 win32k `sdwhwin32` (wow64win) · 2 console `sdwhcon` (wow64con) · 3 base `sdwhbase` (wow64base). 칸 `{+0x0 함수 배열, +0x8 0, +0x10 한계, +0x18 인자 바이트 표, +0x20/+0x24/+0x28 오류 표}` | `ProcessInit +0xb5 ~ +0x14d`, PE 데이터 (결과서) |
| 칸 수 · 이름 | nt **509** (`whNtAccessCheck` 0 … `whNtWow64GetCurrentProcessorNumber` 508, 끝 20개 `whNtWow64*` 는 32비트 전용), win32k **1497**, console **4** (`whUserConnectToServer` · `whCallUserpExitWindowsEx` · `whEndTask` · `whLogon`), base **16**. 꼴 `whNt*` (console 은 `wh*`) | PDB 이름표 + PE 표 (결과서) |
| 부르기 | `+0x12e` · `+0x132` 표 → 함수 포인터. 셋은 주소 비교로 바로 (`+0x169` · `+0x174` · `+0x18b`), 나머지 `+0x199 CALL RAX`. `whNt*` 는 `RCX[0], [4], …` 로 32비트 인자를 읽고 **64비트 ntdll `Nt*` 를 import 해서 부른다** (`whNtReadVirtualMemory +0x4f`, 결과서) | `.asm` |
| 범위 검사 | `+0xa0 CMP EDI,[표+0x10]` · `+0xa5 JA` → `+0x26e` **`0xc000001c`**. 한계가 넷 다 `0x1000` 이라 늘 통과 (console 은 4칸뿐인데도) | `.asm` + PE 데이터 |
| 변환 중 예외 | `$filt$0`: `0x80000003` · `0xc0000008` · `0xc0000235` 면 `Wow64PassExceptionToGuest`. 처리기 (`+0x1c1 ~ +0x1df`, Ghidra 틈) 가 `Wow64HandleSystemServiceError` → 표 `+0x20` 오류 종류 (nt 는 0 → 상태 그대로) | 결과서 |
| win32k | wow64win `sdwhwin32` (RVA `0x59398`) 같은 꼴. 다른 내보내기 `NtWow64UserConnectHook` 하나 | PE 데이터 (analyze 안 함) |

### 5. wow64base · wow64con (A5) [정적: PE import]

flow-writer 가 `pefile` 로 다시 읽은 import 표:

| DLL | import |
|---|---|
| wow64.dll | `ntdll.dll` 598 · `wow64base.dll` 1 (`sdwhbase`) · `wow64win.dll` 1 (`sdwhwin32`) · `wow64con.dll` 1 (`sdwhcon`) |
| wow64con.dll | `ntdll.dll` 11 · `wow64win.dll` 1 (`NtWow64UserConnectHook`) |
| wow64base.dll | `ntdll.dll` 33 · `wow64.dll` 3 |
| wow64win.dll | `ntdll.dll` 15 · `wow64.dll` 9 |
| wow64cpu.dll | `ntdll.dll` 17 · `wow64.dll` 2 |

- 그래서 **`LdrpLoadWow64 +0xa8 LdrpLoadDll(wow64.dll)` 의 import 훑기 한 번에 wow64base · wow64win · wow64con 이 같이 올라온다.** 고르는 레지스트리 · `LoadLibrary` 글 없음 (결과서: 세 이름 글은 import 이름으로만). wow64cpu 만 나중에 `CpuLoadBinaryTranslator +0x90 LdrLoadDll` 로.
- 범위 밖 이름만: wow64win 안 `wow64lg2.dll` 글 (로깅, `[추측]`), wow64 안 `Wow6432Node` 레지스트리 글 (32비트 `NtOpenKey` 때, 초기화 아님).

### 호출 관계

```
ntdll!LdrpInitializeProcess   (UseWOW64 ≠ 0 일 때만 — test.exe 는 +0x176d JZ 로 건너뜀, run44 사진 0)
 ├─ +0x17c6  LdrpLoadWow64(…\System32\)
 │            ├─ +0xad  LdrpLoadDll(…\System32\wow64.dll, 0x800)   ── import 로 wow64base · wow64win · wow64con 같이
 │            ├─ +0xf6  LdrGetProcedureAddress ×6  → .mrdata 0x1e70c8 · e0 · d0 · f8 · 7118 · 7110
 │            └─ +0x150 LdrpDereferenceModule
 └─ +0x17fe  ⇢ wow64!Wow64LdrpInitialize(CONTEXT)          (돌아오지 않음 — 아래 +0x297 RET 갈래만 예외)
              ├─ +0xad  ntdll!RtlWow64GetCpuAreaInfo(TEB64+0x1488)
              ├─ +0x142 ProcessInit                               (처음만)
              │          ├─ +0x64  Wow64InitializeEmulatedSystemInformation → NtQuerySystemInformation(0x3e)
              │          ├─ +0xad  Wow64pInitializeFilePathRedirection → NtOpenFile · NtQueryObject · NtOpenDirectoryObject
              │          ├─ +0x1b0 CpuLoadBinaryTranslator
              │          │          ├─ +0x56 CpuGetBinaryTranslatorPath → NtOpenKey(…\Wow64\x86) · NtQueryValueKey  (없으면 wow64cpu.dll)
              │          │          └─ +0x97 LdrLoadDll(wow64cpu.dll) · +0xfc LdrGetProcedureAddress ×34
              │          ├─ +0x1c9 ⇢ wow64cpu!BTCpuProcessInit   (+0xd8 CpupSystemCallFast)
              │          └─ +0x1eb Wow64pInitializeCrossProcessMechanism → NtCreateSection · NtMapViewOfSection
              ├─ +0x14b ntdll!LdrProcessInitializationComplete      (처음만)
              ├─ +0x1cd CpuNotifyMapViewOfSection(exe)
              ├─ +0x25d CpuNotifyMapViewOfSection(SysWOW64\ntdll.dll) +0xba CpupResolveReverseImports +0xb2 [Wow64Transition] ← BopCode
              ├─ +0xcb  Run64IfContextIs64 ── 참 → +0x297 RET (돌아옴)
              ├─ +0xf9  ThreadInit +0xa6 CpuThreadInit  → TEB32+0xc0 ← BopCode · ⇢ BTCpuThreadInit
              ├─ +0x116 Wow64SetupInitialCall   Eip ← LdrSystemDllInitBlock+0x18 값, [Esp+4] 사본 문맥, [Esp+8] 32비트 ntdll 기준
              └─ +0x122 RunCpuSimulation ⇢ wow64cpu!BTCpuSimulate +0x9 RunSimulatedCode   (고리)
                          ├─ IRETQ (CS 0x23 · SS 0x2b, 첫 진입) / JMPF [R14] (CS 0x23)  ──▶ 32비트 ntdll (Q253c)
                          └─ ◀── JMPF (CS 0x33) KiFastSystemCall(2) → CpupReturnFromSimulatedCode
                                   └─ ServiceNoTurbo 0x1a79 ⇢ wow64!Wow64SystemServiceEx
                                             └─ +0x19b ⇢ whNt* → ntdll!Nt* ⇒ syscall
```

- (나무 오프셋은 ret 자리. `⇢` 는 간접 호출 · import 호출, `⇒` 는 syscall → 커널. 콜스택으로 본 적 없음.)

## 확인된 것

- `[정적]` `LdrpLoadWow64` 는 wow64.dll 하나를 `LdrpLoadDll(…, 0x800, …)` 로 올리고 표 6칸으로 ntdll 전역 6칸을 채운다. 실패하면 프로세스가 끝난다 (`NtTerminateProcess`).
- `[정적]` `g_LdrpWow64LdrpInitialize` 의 대상 = `wow64!Wow64LdrpInitialize` (이름 글 `Wow64LdrpInitialize` 를 이 칸에 찾아 씀, PDB 이름 같음, rva `0x11ed0`). **run17 · run19 의 `[추측: wow64 쪽 시작 함수]` 를 닫음.**
- `[정적]` `Wow64LdrpInitialize` 는 보통 돌아오지 않는다 (`BTCpuSimulate` 고리, `ret` 없음, `INT3` · `INT 0x29` 표시). 돌아오는 길은 `Run64IfContextIs64` 참 하나. **run19 "탄 뒤엔 `+0x1801 JMP +0x1d1b`" 는 그 예외 갈래에서만.**
- `[정적]` 32비트 ntdll 로는 **부르지 않고 문맥을 넘긴다** — `Eip` = 64비트 ntdll `LdrSystemDllInitBlock +0x18` 값, `[Esp+4]` 사본 문맥, `[Esp+8]` 32비트 ntdll 기준.
- `[정적]` CPU dll 은 레지스트리로 고르고 없으면 `wow64cpu.dll`. 필수 함수 3.
- `[정적]` 64 ↔ 32 전환 꼴 (`IRETQ` · `JMPF m16:32` CS `0x23` / `JMPF` CS `0x33`, `RETF` 없음), CPU 영역 칸, `Wow64Transition` 쓰는 줄 (`CpupResolveReverseImports +0xb2`).
- `[정적]` syscall 다리 꼴 (번호 비트 12~13 = 표, 표 4개 × `0x30`, 범위 검사 사실상 없음, 터보 썽크는 wow64cpu 안 `SYSCALL`).
- `[정적: PE import]` wow64base · wow64win · wow64con 은 wow64.dll 의 정적 import 로 같이 올라온다.

## 정정 (고쳐 읽기 — run17 · run19)

틀린 걸 단정한 줄은 없다. `[추측]` 으로 둔 두 곳이 닫혀 각 문서 맨 위에 "고쳐 읽기 (run138)" 한 줄을 넣었다 (02_sprint 문서는 그 한 줄만).

- **run17** "이번에 안 정한 것" 의 `g_LdrpWow64LdrpInitialize` 가 가리키는 함수 → **`wow64!Wow64LdrpInitialize`** `[정적]`. `+0x9a` 나중 스레드 길도 같은 함수이고, 보통 돌아오지 않아 `+0xa2 LdrpInitializeThread` 는 `Run64IfContextIs64` 참일 때만.
- **run19** 간접 호출 표 `+0x17fc / +0x17fe` 의 "대상 이름 `[추측: wow64 쪽 시작 함수]`" → **`wow64!Wow64LdrpInitialize`** `[정적]`, 그리고 "탄 뒤엔 `+0x1801 JMP +0x1d1b`" 는 **돌아올 때만** — 보통 길은 그 안에서 32비트 코드로 넘어가 돌아오지 않는다.
- 맞는 줄 (안 고침, 이번에 근거만 보탬): run82 (`Wow64PrepareForException` 은 `LdrpLoadWow64` 의 `LdrGetProcedureAddress` 출력으로만 채워짐 — 64비트에선 0), run121 (wow64 의 `+0xc0` · `+0xc4` 는 TEB32 칸 — `CpuThreadInit +0x32` · `ThreadInit +0x66`), run48 (`PsWowX86SharedInformation +0x30` ↔ 64비트 ntdll 블록 `+0x48` 을 wow64 가 `NtDll32Base` 로 읽음 — run48 의 "`PspPrepareSystemDllInitBlock` 이 복사하는 것으로 읽힘 `[추측]`" 은 커널 쪽이라 그대로, sast/135 몫), run106 (`+0x98` `0x800`).

## PE_emulator-AR 쪽 (결과서 "지금 우리 맞나·틀리나" — `../PE_emulator-AR` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 곳 (원문 "답이 오면 쓰는 곳") | 에뮬레이터가 해야 할 것 / 진짜 코드가 읽으니 지어 둘 것 |
|---|---|
| **AR04** TEB32 · 32비트 스택 · CPU 영역 | ① TEB64 `+0x180c` > 0, TEB32 `+0x30` = PEB32, **PEB32 바로 뒤 (`+0x490`) 에 `Wow64Info` 자리** (≥ `0x24` B, 0 으로 — wow64 가 `+0x0` · `+0x20` · `+0x22` 를 쓰고 `+0x4` · `+0x8` 을 읽음). ② **TEB64 `+0x1488` = CPU 영역** (`{플래그, 0x14c}` + `CONTEXT32` 첫 값, 끝 `+0x300`, XState 끔 기준) — **wow64 는 이 칸에 안 쓰므로 커널 쪽이 지어야 함**. ③ 초기 `CONTEXT32.Esp` 아래 `0x2cc + 0x10` + 호출틀 12 B 가 쓸 수 있게 (`Wow64SetupInitialCall +0x15 ~ +0x4c`). ④ 64비트 ntdll `LdrSystemDllInitBlock +0x18 ~ +0x48`. `TlsSlots[10]` (`+0x14d0`) 은 wow64 가 자기가 씀 |
| **AR05** GDT · FS `0x53` · 모드 바꾸기 | 명령: 64비트 모드 `JMPF m16:32 [mem]` (`FF /5`, CS `0x23`, 앞 `MOV SS`), `IRETQ` (compat 5 칸 틀), 호환 모드 `JMPF ptr16:32` (`EA`) · `JMPF m16:32 [esp]` (CS `0x33`), `FXRSTOR`, `MOV` 세그먼트 (`0x2b` · `0x53`), 조건부 `WRFSBASE` (KUSD `+0x28a`; 끄면 `MOV FS,0x53` → GDT 칸 10 기준), **wow64cpu 안에서도 `SYSCALL` / `INT 0x2e`** (터보), 32비트 구간 동안 R8~R15 · 상위 32비트 보존. GDT 최소 `0x23` · `0x2b` · `0x33` · `0x53`. **wow64cpu 는 4GB 아래에** (재배치 꼴). KUSD `+0x308` (`SystemCall`) 값을 정해야 함 (1 이면 `INT 0x2e` · 빠른 길 꺼짐) |
| **AR06** ntdll 엔진 WoW64 갈래 · 은퇴 자리 | ① `+0x17c6` 의 `LdrpLoadWow64` 는 **진짜를 돌려도 됨** — 필요: `…\System32\` 글, 64비트 로더가 wow64 · wow64base · wow64win · wow64con 을 import 연결해 올릴 수 있을 것, 결과 6칸. ② **`+0x17fe` 는 돌아오지 않는다** — 엔진은 `+0x1801` 을 "돌아옴" 으로 두지 말고 진짜 코드에 넘기고 끝 (은퇴 `NtContinue` 는 32비트 ntdll 쪽 — Q253c). ③ `LdrpInitializationComplete` 는 wow64 (`+0x144`) 가 부른다 — 엔진이 따로 신호를 또 보내지 않게 (진짜 `_LdrpInitialize` 도 `+0x31b` 에서 이미 2 면 건너뜀 — 아래 대조). ④ 나중 스레드: `_LdrpInitialize +0x9a` 같은 전역 |
| **AR06** 지어 둘 것 | PEB64 `ProcessParameters.ImagePathName` · `ImageBaseAddress` · `Ldr` 목록 (+ `+0x2d8` · `+0x2f8` · `+0x308`), 64비트 ntdll `LdrSystemDllInitBlock`, `\KnownDlls\kernel32.dll` · `\KnownDlls32\kernel32.dll` (없어도 통과), **`\??\<시스템루트>` 열기 + `NtQueryObject(1)` (없으면 프로세스 죽음)**, `NtQuerySystemInformation(0x3e)` (없어도 통과), 레지스트리 `…\Wow64\x86` (없어도 통과 → wow64cpu.dll) |
| **AR07 이후** 벽 · 상한 · 예외 | 벽 (실패 → `NtTerminateProcess`): `RtlCreateHashTable`, 윈도 폴더 열기 · 이름, `CpuLoadBinaryTranslator`, 교차 프로세스 구역, 이미지 알림 둘. 상한: `Wow64pMaxAddress` ← `NtQuerySystemInformation(0x3e) +0x28` (커널 `Wow64VaSpace4Gb` 와 맞출 것 — sast/135). 예외: 64비트 `KiUserExceptionDispatcher +0x1` → `Wow64PrepareForException` (문맥 `SegCs == 0x23` 이면 `CpuResetToConsistentState` → `RtlpWow64CtxFromAmd64`) |

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 13 — 대기열 **Q260 (동적: 결과서 동적 13개 — Q253d VM 과 합침)**. x86 시험 exe 가 있어야 함 — 사용자 결정. 멈춤점은 그 프로세스 조건, ret 자리):
1. 6칸이 채워졌나 — `ntdll!LdrpLoadWow64+0x148` 에서 `dq ntdll+0x1e70c8 L2` · `+0x1e70d0` · `+0x1e70e0` · `+0x1e70f8` · `+0x1e7110 L2`.
2. **`Wow64LdrpInitialize` 가 돌아오나** — `wow64!Wow64LdrpInitialize+0xcb` `al` (0 이어야), `+0x122` · `ntdll!LdrpInitializeProcess+0x17fe` (걸리면 안 됨).
3. 프로세스 차례 — `wow64!Wow64LdrpInitialize+0x142` (`ProcessInit` `eax`) · `+0x14b` · `+0x1cd` · `+0x25d` · `+0x116`.
4. CPU dll 고르기 — `wow64!CpuGetBinaryTranslatorPath+0xb4` · `+0x129` · `+0x194`, `wow64!CpuLoadBinaryTranslator+0x97`, `lm m wow64cpu` (**4GB 아래인지**).
5. `Wow64Transition` 쓰기 — `wow64!CpupResolveReverseImports+0xb2` (call 아님, 쓰기 줄) `rsi` · `eax`.
6. 빠른 길 쓰나 — `wow64cpu!BTCpuProcessInit+0xd8`, `db wow64cpu+0x5598 L1`, `dd 7ffe0308 L1`.
7. CPU 영역 · 첫 진입 — `wow64cpu+0x1a36` (`IRETQ`) · `+0x198b` (`JMPF`): `dq @$teb+0x1488 L1`, `dc <영역> L0x40`, `dq rsp L5`, 세그먼트 전/후.
8. 32 → 64 입구 — `wow64cpu+0x1a38` · `+0x1a73` (ret `0x1a79`): `eax` · `r11` · `cs` (`0x33`).
9. 서비스 표 — `wow64!Wow64SystemServiceEx+0x199` (ret `+0x19b`): `dq wow64+0x52030 L18`, `rax` 이름.
10. `PEB32 +0x490` · `TlsSlots[10]` — `wow64!Wow64LdrpInitialize+0xad`: `dq @$teb+0x14d0 L1`.
11. `GS:[0]` · KUSD — `wow64cpu+0x19d4`: `dq gs:[0] L1` (= TEB32 인지), `db 7ffe028a L1`, `dd 7ffe0308 L1`.
12. `\KnownDlls32` 열렸나 — `wow64!Wow64pInitializeFilePathRedirection+0x148`.
13. 터보 썽크 쓰나 — `wow64cpu+0x1f70` (`CpupSyscallStub`) 에서 부른 쪽 · `syscall` 의 RIP.

정적 (결과서 "다음 요청 후보" 2 ~ 5 — 대기열 **Q261 (정적: 결과서 다음 요청 후보 2~5 — wow64 `whNt*` 본문·`Wow64pCpuInitializeStartupContext`·`Wow64KiUserCallbackDispatcher` · 터보 썽크 · wow64cpu 4GB 아래 올리기 · XState CPU 영역)**):
2. wow64 더 — `Wow64pCpuInitializeStartupContext` · `ThunkStartupContext64TO32` 분기, `whNtContinue` · `whNtTestAlert`, `Wow64KiUserCallbackDispatcher`, `Wow64ApcRoutine`, `Wow64SetupForInstrumentationReturn` (`Wow64Info +8`), 자주 쓰는 `whNt*` 본문.
3. wow64cpu 터보 썽크 본문 (`Thunk*Arg*` · `ReadWriteFile` 등 — Ghidra 가 함수로 안 잡은 구간).
4. wow64cpu 가 4GB 아래에 올라오는 까닭 (로더 / 커널 `MiSelectImageBase` 쪽, sast/135 C 8 과 같이).
5. KUSD `XState` 가 켜진 때 CPU 영역 크기 · 칸 (`CONTEXT_EX` `0xcc` 뜻).

- 후보 1 (32비트 ntdll — `LdrInitializeThunk(PCONTEXT, NtDllBase)` 입구 확인 · `Wow64Transition` 대 `fs:[0xc0]` · `NtContinue` 은퇴 길 · `NtdllExportInformation[1]` 8칸 이름) 은 **Q253c** (sast/137) 로 갔다.
- 그 밖 `[공백]`: `LdrpInitializeProcess +0xd36 ~ +0xd4a` 글 앞부분, `LdrpLoadWow64` 뒤 wow64.dll 참조 수, CPU 영역 `+0x0` 비트 1 뜻 · 처음 값, `Wow64Info +0x4` (CpuFlags 로 보임 `[추측]`) · `+0x8` 쓰는 곳, `CONTEXT_EX` `0xcc`, `LdrSystemDllInitBlock +0xc8` · `+0xd0` 을 wow64 가 읽는 두 곳 (rva `0x779f` · `0x1d1e1`, CFG 쪽 `[추측]`).

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- **`_LdrpInitialize +0x22d` (`UseWOW64`) 는 "건너뜀" 이 아니라 "이미 끝났으면 건너뜀"** (보탬): `+0x233 JNZ +0x31b` → `+0x31b CMP [LdrpProcessInitialized],R15D` (`R15D = 1`, `+0x2e`) → 1 이 아니면 `+0x322 JNZ` 로 건너뛰고, **아직 1 이면 `+0x328 JMP +0x239` 로 같은 `LdrpInitializationComplete` 를 부른다.** wow64 가 `+0x144` 에서 먼저 2 로 올려 두므로, 돌아오는 갈래 (`Run64IfContextIs64` 참) 에서도 신호가 두 번 가지 않는다. 결과서 AR06 ③ "엔진이 또 보내지 않게" 와 같은 뜻 — 진짜 `_LdrpInitialize` 를 돌리면 저절로 지켜진다.
- `Wow64LdrpInitialize` 의 `NtTerminateProcess` 자리: 결과서 표 8 · 9 · 10 의 `+0x2a8` · `+0x2be` · `+0x2d4` 는 **분기 목적지**이고 call 은 `+0x2ad` · `+0x2c3` · `+0x2d9` (ret `+0x2b4` · `+0x2ca` · `+0x2e0`). 각 call 뒤에 `JMP` 로 원래 길에 다시 붙는다 (`+0x2b9 JMP +0x1d5` 등 — `NtTerminateProcess(-1)` 이 돌아오지 않으면 무의미).
- `Wow64LdrpInitialize` 돌아오는 `RET` 는 `+0x297` (결과서 "+0x26b 로 가서 `RET`" — `+0x26b` 는 분기 목적지, 쿠키 검사 `+0x275` 뒤 `+0x297 RET`).
- `WowTebOffset == 0` 갈래 (`+0x5e JZ +0x299`): `+0x299 MOV RCX,R15` (`R15 = 0`) → `+0x29c JMP +0x69` → `MOV EAX,[RCX+0x30]` = **주소 `0x30` 읽기**. 결과서 "TEB32 쪽을 0 으로 읽음" 은 이 뜻이고, 실제로는 접근 위반이 날 자리로 보인다 `[추측]`. 음수면 (`+0x64 JS`) 더하지 않고 TEB64 자체 `+0x30` 을 읽는다. WoW64 프로세스에선 안 타는 갈래.
- `ProcessInit` 실패 때 `LdrProcessInitializationComplete` (`+0x144`) 는 **상태를 보기 전에** 불린다 (`+0x154 TEST EBX` 는 그 뒤). 결과서 차례 5 · 6 과 같은 순서, 보탬.
- 함수 수: 결과서 "wow64cpu 30 · wow64 1143" 은 `_index.tsv` 줄 수 (머리 줄 포함) — 함수는 29 · 1142 (`ExportIndex:` 로그 줄도 같음).
- RVA `0x1a6e` 는 PDB 공개 이름이 둘 (`ServiceNoTurbo` · `TurboDispatchJumpAddressEnd`) — 결과서 본문은 앞 이름, `_calls.tsv` 는 뒤 이름 (`TurboDispatchJumpAddressEnd +0x5/+0xb Wow64SystemServiceEx` = RVA `0x1a73/0x1a79`). 같은 줄.
- 결과서 "wow64cpu 는 7개와 `TurboThunkControl` 만 내보냄" — 내보내기는 `BTCpu*` 8 + `TurboDispatchJumpAddressStart` · `…End` 2 (34칸 표에 안 드는 이름). 뜻은 같음.
- 그 밖 결과서 오프셋 · 바이트 · 글 · import 는 `.asm` · objdump · `pefile` 과 **일치** (위 "환경" 의 줄 전부).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- Ghidra 가 함수로 못 읽은 구간 (wow64cpu `0x1a38 ~ 0x1f6f` · `WOW64SVC` · `W64SVC`) 은 결과서처럼 objdump · ndisasm 파일로만 근거를 잡았다. `RunSimulatedCode +0xbd` 를 Ghidra 는 `WRFSBASE RAX` 로 보이지만 바이트는 `wrfsbase r8` (objdump `19dd`, 결과서 "알아 둘 것").
- Ghidra 전역 주석 `RtlpTpWorkTaskCallbacks+0x10` (표 `0x173830`) · `CachedMachine+0x2` (바이트 `0x4c736`) 는 앞쪽 가까운 이름 — 별개 칸.
- `pefile` 은 시스템 `python3 -I` 에선 안 보인다 (사용자 site-packages 에 있음). flow-writer 는 스크래치 폴더에서 `-I` 없이 돌렸다 (기록만).

# 136 wow64-dlls — 정적 분석 결과

- 요청서: `_workspace/sast/136_wow64-dlls.md` (원문 `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` A 절)
- 판정: **분석성공** (A 1 ~ 5 전부 닫음. 못 닫은 몇 줄은 `[추측]`·`[공백]` 로 표시, 맨 아래 "동적으로 확인할 것")
- 스크립트 문제: **없음** (아래 "알아 둘 것" 에 Ghidra 표시 몇 개만 적음)
- 다른 decompiler (sast/135 ntoskrnl) 는 건드리지 않음. Ghidra 는 한 번에 하나만 돌림 (매번 `free -g`: 전체 46G, 쓸 수 있는 메모리 25 ~ 27G, 8G 기본값으로 충분).

## PDB GUID 줄 (새로 받은 것 · 새로 analyze 한 것)

| 모듈 | PDB guid+age (바이너리에서 읽음) | analyze | 함수 수 |
|---|---|---|---|
| wow64cpu.dll | `5523FB635B93EB60C1DC4AA13C02C7991` | 함 (`SetPdb:` 줄 있음, PDB 적용 끝남) | 30 |
| wow64.dll | `AED569E2D483C3CDC0C97DEC3691CD551` | 함 (`SetPdb:` 줄 있음) | 1143 |
| wow64win.dll | `28A0615EA1DB50E56B09381B397C580E1` | **안 함** (요청서 약속: import·표 모양만) | — |
| wow64base.dll | `DD039D5CACB5A3D52830CD0313ED18D51` | **안 함** (import 로 닫힘) | — |
| wow64con.dll | `EE59C3B807B7AA61ABDF5B4D4BD272621` | **안 함** (import 로 닫힘) | — |

- 64비트 `ntdll.dll` 은 다시 analyze 하지 않고 `^이름$` export · `refs` 만 함.
- 커널 GUID 확인은 해당 없음 (ntoskrnl 을 안 건드림).

## 버전 확인 (기존 run 근거와 대조) — 전부 일치

| 이번에 본 것 | 기존 근거 | 결과 |
|---|---|---|
| `LdrpInitializeProcess` +0x17c1 call / ret +0x17c6 = `LdrpLoadWow64`, +0x17fc call / ret +0x17fe = `g_LdrpWow64LdrpInitialize`, +0x1801 `JMP` 목적지 0x18008e11f = +0x1d1b | run17·18·19·106 | 일치 (`ms/disassemble/ntdll/_calls.tsv` 와 `LdrpInitializeProcess.asm`) |
| `LdrpInitializeProcess` +0x1bb6/+0x1bbb `LdrpInitializeGraphRecurse`, `_LdrpInitialize` +0x167/+0x16c `LdrpInitializeProcess`, `LdrInitializeThunk` +0x9/+0xe | 스킬 "알려진 사실" 표 | 일치 (스크립트 오프셋 믿을 수 있음) |
| 표 둘째 칸의 출력 칸 포인터 = ntdll RVA `0x173848` | run82 (`Wow64PrepareForException` 표 `0x173848`) | 일치 |
| `KiUserExceptionDispatcher +0x1` 가 `0x1e70e0` 읽음 | run82 | 일치 (`_refs/Wow64PrepareForException.tsv`) |
| wow64.dll 이 TEB32 `+0xc0` · `+0xc4` 에 씀 | run121 | 일치 (`CpuThreadInit.asm +0x32`, `ThreadInit.asm +0x66`) |
| `LdrSystemDllInitBlock +0x48` = 32비트 ntdll 기본 주소 | run48 (`PsWowX86SharedInformation +0x30`) | 일치 (+0x18 시작이면 +0x18 + 0x30 = +0x48. `ProcessInit.asm +0x73`) |

## 만든 파일

- `ms/sym/`: `wow64.dll.pdb` · `wow64win.dll.pdb` · `wow64base.dll.pdb` · `wow64con.dll.pdb` (+ `.info.txt`, `.aliases.tsv`), `wow64cpu.dll.aliases.tsv`
- `ms/ghidra/`: `wow64cpu.*` · `wow64.*` (프로젝트, analyze·export·map 로그)
- `ms/decomp/{wow64cpu,wow64}/` · `ms/disassemble/{wow64cpu,wow64}/` (`_calls.tsv`, `wow64/_map/Wow64LdrpInitialize_*` 깊이 4 지도)
  - wow64cpu: `BTCpu*` 전부 · `RunSimulatedCode` · `CpupSyscallStub` · `TurboDispatch*` · `ThunkNone` · `WaitForMultipleObjects` (깊이 0)
  - wow64: `Wow64LdrpInitialize` · `ProcessInit` · `ThreadInit` (깊이 1, 26개) + `CpuGetBinaryTranslatorPath` · `CpupResolveReverseImports` · `Wow64SystemServiceEx` · `Wow64pInitializeCrossProcessMechanism` · `Wow64InitializeEmulatedSystemInformation` · `GetImageName` · `Wow64pInitializeFilePathRedirection` (깊이 1) + `Wow64PrepareForException` · `Wow64ApcRoutine` 류 · `Wow64HandleSystemServiceError` (깊이 0)
- ntdll (깊이 0, 읽기만): `RtlWow64GetCpuAreaInfo` · `RtlWow64GetCurrentCpuArea` · `RtlpWow64CtxFromAmd64` · `RtlWow64GetCpuAreaEnabledFeatures` · `RtlWow64GetCurrentMachine` · `RtlWow64GetProcessMachines` · `RtlWow64GetSharedInfoProcess` + `refs` 넷 (`ms/disassemble/ntdll/_refs/0x1e70d0.tsv` · `0x1e70f8` · `0x1e7110` · `0x1e7118`)
- **Ghidra 가 안 읽은 자리용** (wow64cpu 의 32비트 쪽에서 들어오는 코드): `ms/disassemble/wow64cpu/_objdump_text_0x1000.s` (`.text` 전체를 objdump 로 푼 것), `ms/disassemble/wow64cpu/_ndisasm_wow64svc.txt` (구역 `WOW64SVC` · `W64SVC` 를 32/64비트로 푼 것). Ghidra `.asm` 의 "읽지 않은 틈" (RVA 0x1a38 ~ 0x1f6f) 이 여기 들어 있음. 아래 줄 근거 중 `RVA 0x....` 는 이 두 파일.
- 임시 스크립트 (PE 표 읽기·PDB 이름표): `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/s136/` (세션 임시. 근거는 아래 표·줄로 남김)

---

## 먼저 읽는 한 줄 답

1. **A1 `LdrpLoadWow64`**: `<System32>\wow64.dll` (글 `0x17bf78` = `wow64.dll`) 을 `LdrpLoadDll(..., 플래그 0x800, ...)` 로 올리고, 이름 **6개** (`Wow64LdrpInitialize` · `Wow64PrepareForException` · `Wow64ApcRoutine` · `Wow64PrepareForDebuggerAttach` · `Wow64SuspendLocalThread` · `Wow64SuspendLocalProcess`) 를 `LdrGetProcedureAddress` 로 찾아 ntdll 전역 6칸 (`0x1e70c8` · `0x1e70e0` · `0x1e70d0` · `0x1e70f8` · `0x1e7118` · `0x1e7110`) 에 직접 쓴다. 하나라도 실패하면 그 상태를 돌려주고 `_LdrpInitialize` 가 `LdrpInitializationFailure` → `NtTerminateProcess` 로 간다.
2. **A2 wow64cpu**: 64 → 32 는 **`JMPF m16:32` (CS `0x23`, 빠른 길) 와 `IRETQ` (CS `0x23`, SS `0x2b`, 상태 다시 싣는 길)** 둘 다. 32 → 64 는 **`JMPF` (CS `0x33`)** 두 꼴 (`KiFastSystemCall` 구역 `W64SVC` · `KiFastSystemCall2` 구역 `WOW64SVC`). `RETF` 는 없음. CPU 영역 = TEB64 `+0x1488` (`TlsSlots[1]`) 이 가리킴 (앞 4바이트 = {플래그 WORD, Machine WORD `0x14c`}, `+0x4` 부터 32비트 CONTEXT `0x2cc` 바이트, `+0x2f8` 에 CPU 전용 포인터 칸, 끝 `+0x300`). 32비트로 처음 들어가는 길은 `BTCpuSimulate` → `RunSimulatedCode` (+0x116 `IRETQ`). `Wow64Transition` 값은 wow64.dll 이 `CpupResolveReverseImports +0xb2` 에서 32비트 ntdll 에 씀 (값 = `BTCpuGetBopCode()` 가 준 `KiFastSystemCall` 또는 `KiFastSystemCall2` 주소).
3. **A3 wow64.dll**: `g_LdrpWow64LdrpInitialize` = `Wow64LdrpInitialize` (PDB 이름 맞음, rva `0x11ed0`). 차례: TEB32/PEB32 에서 `Wow64Info` 자리 잡기 → CPU 영역 보기 → (처음이면) `ProcessInit` · `LdrProcessInitializationComplete` · 이미지 2개 `CpuNotifyMapViewOfSection` → `Run64IfContextIs64` → `ThreadInit` → 32비트 문맥 고치기 → `RunCpuSimulation`. CPU dll 은 **레지스트리로 고른다** (키 `\Registry\Machine\Software\Microsoft\Wow64\x86`, 값 이름 = exe 파일 이름 → 없으면 기본값 → 키가 없으면 `wow64cpu.dll`). 32비트 `LdrInitializeThunk` 는 **부르지 않고** 32비트 문맥의 `Eip` 를 `Ntdll32LoaderInitRoutine` (= 64비트 ntdll `LdrSystemDllInitBlock +0x18`) 으로 놓고 `BTCpuSimulate` 에 맡긴다. **이 함수는 돌아오지 않는다** (x86 exe 첫 스레드·이후 스레드 모두 `RunCpuSimulation` 이 끝). 돌아오는 건 `Run64IfContextIs64` 가 참일 때 (시작 주소가 64비트 PE 모듈 안) 뿐 → `LdrpInitializeProcess +0x1801` 은 그 예외 갈래에서만 탄다.
4. **A4 다리**: 입구 `Wow64SystemServiceEx(ECX = 번호, RDX = 32비트 스택 인자 포인터)`. 번호 비트 12~13 = 표 (0 nt / 1 win32k / 2 console / 3 base), 비트 0~11 = 표 칸, 비트 16 이상 = wow64cpu 의 "터보 썽크" 번호. 표는 `ServiceTables` 4개 × 0x30 바이트 (칸 수 nt 509 · win32k 1497 · console 4 · base 16, 이름 꼴 `whNt*` 확인). 범위 검사는 한계 `0x1000` 이라 12비트 번호가 항상 통과 (사실상 없음). 예외가 나면 `Wow64HandleSystemServiceError` 로 상태를 정함. wow64win.dll 은 `sdwhwin32` 로 같은 꼴.
5. **A5 wow64base · wow64con**: **wow64.dll 의 정적 import 셋 (`wow64base.dll`·`wow64win.dll`·`wow64con.dll`) + `wow64con` 은 `wow64win` 도 import**. 그래서 `LdrpLoadWow64` 의 `LdrpLoadDll(wow64.dll)` 한 번에 같이 올라온다. 따로 `LoadLibrary` 글·레지스트리는 없음.

---

## (1) A1 — `LdrpLoadWow64` 심볼 표 (ntdll `0x173830`, 16바이트 × 6칸)

칸 모양: `{+0x0 ANSI_STRING 를 가리키는 포인터 (이름), +0x8 값을 쓸 ntdll 전역의 주소}`. ANSI_STRING 의 글자는 `.rdata` (RVA `0x19b6a0` 둘레) 에 있음. 근거: `ms/disassemble/ntdll/LdrpLoadWow64.asm` +0xcd (표 주소 `LEA R15`), +0xe8 (`[R15+RSI*8+8]` → R9 = 출력 칸), +0xed (`[R15+RSI*8]` → RDX = 이름), +0xf1/+0xf6 (`LdrGetProcedureAddress`). Ghidra 가 +0xcd 옆에 적은 `RtlpTpWorkTaskCallbacks+0x10` 은 가장 가까운 앞쪽 이름일 뿐 (표는 별개).

| 칸 | 표 주소 | 이름 글 | 쓰는 전역 (ntdll RVA · PDB 이름) | 그 전역을 읽는 곳 ([정적] `_refs`) |
|---|---|---|---|---|
| 0 | `0x173830` (+0x8 = `0x173838`) | `Wow64LdrpInitialize` | `0x1e70c8` `g_LdrpWow64LdrpInitialize` | `_LdrpInitialize +0x90` · `LdrpInitializeProcess +0x17ed` |
| 1 | `0x173840` (`0x173848`) | `Wow64PrepareForException` | `0x1e70e0` `Wow64PrepareForException` | `KiUserExceptionDispatcher +0x1` |
| 2 | `0x173850` (`0x173858`) | `Wow64ApcRoutine` | `0x1e70d0` `Wow64ApcRoutine` | `KiUserApcDispatcher +0x6e` |
| 3 | `0x173860` (`0x173868`) | `Wow64PrepareForDebuggerAttach` | `0x1e70f8` `g_LdrpWow64PrepareForDebuggerAttach` | `DbgUiRemoteBreakin +0x38` |
| 4 | `0x173870` (`0x173878`) | `Wow64SuspendLocalThread` | `0x1e7118` `g_LdrpWow64SuspendLocalThread` | `RtlpWow64SuspendThreadWorker +0x0` |
| 5 | `0x173880` (`0x173888`) | `Wow64SuspendLocalProcess` | `0x1e7110` `g_LdrpWow64SuspendLocalProcess` | `RtlpWow64SuspendLocalProcess +0x0` |

- 칸 수: **6** (`+0xd7 CMP EDI,0x6` / `+0xda JNC` 로 루프 끝). 여섯 이름 모두 wow64.dll 내보내기 표에 있음 (번호 16 · 21 · 7 · 20 · 29 · 28; `PE 내보내기 표`).
- **DLL 이름 글**: `+0x6e` 의 `0x18017bf78` = UTF-16 `wow64.dll` (9글자 + 널). 만드는 법: 먼저 +0x69 `RtlAppendUnicodeStringToString(buf, 인자 = 시스템 디렉터리 글)`, +0x7a `RtlAppendUnicodeToString(buf, "wow64.dll")` → `…\System32\wow64.dll`. 인자 글은 `LdrpInitializeProcess +0xd36 ~ +0xd4a` 가 `[RSP+0xc0]` 에 만든 `UNICODE_STRING` (끝 `SlashSystem32SlashString`) 이고 +0x17b9 `LEA RCX,[RSP+0xc0]` 로 넘김 (`[RSP+0x120]` 쪽 앞부분이 무엇인지는 이번에 안 따라감).
- **올리는 법**: +0x8e `LdrpInitializeDllPath(글 버퍼, 0x4001, &경로객체)` → +0xa8 `LdrpLoadDll(&글, &경로객체, R8D = 0x800, R9 = &LDR_DATA_TABLE_ENTRY 출력)` (ret +0xad) → +0xb4 `LdrpReleaseDllPath`. (`0x800` 은 run19 의 "문맥 비트 0x800" 과 같은 값.)
- 칸 채우기: +0xc3 `LdrProtectMrdata(0)` (ntdll `.mrdata` 쓰기 허용), 칸마다 `LdrGetProcedureAddress(DllBase = [엔트리+0x30], 이름, 순번 0, 출력 칸)`, 끝에 +0x143 `LdrProtectMrdata(1)` (ret +0x148) → +0x14b `LdrpDereferenceModule(엔트리)` (ret +0x150). 즉 **모듈 참조를 한 번 내려놓는다** (성공 때도). 그 뒤에도 wow64.dll 이 남아 있는지는 `LdrpLoadDll` 쪽 참조 규칙이라 `[공백]`.
- **실패 때**:
  - `LdrpLoadDll` 가 음수 → +0xbb `JS` → +0x17e ~ +0x1ae `LdrpLogInternal` (줄 번호 `0x10f2`) → +0x1b3 `JMP +0x150` (참조 내리기를 건너뜀) → 그 상태를 돌려줌.
  - `LdrGetProcedureAddress` 가 음수 → +0xfa `JS` → +0x100 ~ +0x139 `LdrpLogInternal` (줄 번호 `0x1109`, 실패한 이름 글 포함) → 루프를 끝내고 +0x13e 이후로 (`LdrProtectMrdata(1)` · 참조 내리기) → **마지막 실패 상태** `EBX` 를 돌려줌 (이미 쓴 앞 칸은 되돌리지 않음).
  - 부른 쪽: `LdrpInitializeProcess +0x17c6` `MOV [RSP+0x50],EAX` → +0x17cc `JS` → +0xa7b (`18008ce7f`, 함수 끝) 로 가서 그 상태를 반환. → `_LdrpInitialize +0x16c` 뒤 +0x172 `JS` (+0x1a7 로그) … +0xa7 `TEST EDI` `JS` → +0x2f7 → **+0x2f9 `LdrpInitializationFailure(상태)` → +0x304 `NtTerminateProcess(-1, 상태)`** (`_LdrpInitialize.asm` +0xa7/+0xa9, +0x2f9, +0x304). 성공이면 `LdrInitState = 3`, `BTR.LOCK [PEB+0x50],1` (+0x17dd/+0x17e7), **+0x17fc `CALL g_LdrpWow64LdrpInitialize(RCX = [RSP+0xe0] = 문맥)`**, +0x17fe 뒤 `NOP`, +0x1801 `JMP +0x1d1b`.

## (3) A3 — `Wow64LdrpInitialize` 차례 (wow64.dll rva `0x11ed0`, 811 바이트)

인자: `RCX` = 초기 64비트 `CONTEXT*` (R14 에 보관). `ms/disassemble/wow64/Wow64LdrpInitialize.asm` 기준. `Wow64Info` = PEB32 바로 뒤 (`PEB32 + 0x490`; `sizeof(_PEB32)` 가 정확히 `0x490` — `pdb_struct.py ntdll.dll _PEB32`).

| 차례 | 함수 · 오프셋 | 하는 일 | 읽는 칸 / 부르는 syscall / 레지스트리 | 실패 때 | 줄 |
|---|---|---|---|---|---|
| 1 | `Wow64LdrpInitialize` +0x4c ~ +0x8f | `Wow64InfoPtr` 전역 ← `PEB32 + 0x490` (TEB32 = TEB64 + `WowTebOffset`, `PEB32 = [TEB32+0x30]`), `TEB64.TlsSlots[10]` (`+0x14d0`) ← 그 주소 | TEB64 `+0x30`(자기) · `+0x180c` · TEB32 `+0x30` | `WowTebOffset == 0` 이면 TEB32 쪽을 0 으로 읽음 (`+0x5e JZ`) | +0x4c, +0x55, +0x69, +0x7f, +0x8f |
| 2 | +0x9f ~ +0xb4 | `RtlWow64GetCpuAreaInfo(TlsSlots[1], 0, &정보)` — CPU 영역 풀이 (아래 (2b)) | TEB64 `+0x1488` | 음수면 +0x2a1 `INT 0x29` (빠른 실패 코드 `0x4b`) | +0xa6 (ret +0xad), +0xb4 JS |
| 3 | +0xba | "프로세스 초기화 끝 표시" (바이트, 전역 `0x4c736`) 가 0 이면 **프로세스 갈래** (4 ~ 10), 아니면 건너뜀 (11 로) | — | — | +0xba, +0xc1 |
| 4 | +0x123 ~ +0x13d | `wil_InitializeFeatureStaging`; `Wow64Info[+0x20] = 0x8664`, `Wow64Info[+0x22] = 정보.Machine` (`[RBP+0x24]`, 0x14c) | `Wow64Info +0x20/+0x22` 쓰기 | — | +0x123, +0x12f, +0x13d |
| 5 | +0x13d | **`ProcessInit()`** (아래 표) | | 음수면 +0x156 `JNS` 지나 +0x15d **`NtTerminateProcess(-1, 상태)`** | +0x13d (ret +0x142), +0x15d |
| 6 | +0x144 | **`LdrProcessInitializationComplete()`** (ntdll, 25바이트 = `LdrpInitializationComplete(&LdrpProcessInitialized, &LdrpInitCompleteEvent, 0x1484)` 로 넘어가는 얇은 껍질 — `_LdrpInitialize +0x239 ~ +0x24d` 가 부르는 것과 같은 인자) | 64비트 ntdll 이 `UseWOW64` 일 때 건너뛴 것 (`_LdrpInitialize +0x22d CMP UseWOW64` (+0x233 `JNZ`)) 을 wow64 가 대신 부름 | — | +0x144 (ret +0x14b) |
| 7 | +0x179 | "프로세스 초기화 끝 표시" ← 1 | | | +0x179 |
| 8 | +0x180 ~ +0x1cd | exe 이미지 알림: 글 = `PEB64.ProcessParameters.ImagePathName` (없으면 `image`), `RtlImageNtHeader(PEB64.ImageBaseAddress)` → `SizeOfImage` (`NT+0x50`) 올림 → **`CpuNotifyMapViewOfSection(글, 기준주소, 0, 크기, 0, 4)`** | PEB64 `+0x10` · `+0x20` (`ProcessParameters`) · 그 안 `+0x60/+0x68` | 음수면 +0x1cf `JS` → +0x2a8 `NtTerminateProcess` | +0x19f ~ +0x1c8 |
| 9 | +0x1d5 ~ +0x212 | `Wow64DetectMachineTypeInternal()` (→ 0x14c) → `Wow64SelectSystem32PathInternal(머신)` (→ `\SysWOW64` 표 `x86Dir`) → `RtlGetNtSystemRoot()` → `RtlStringCbPrintfW(…, "%s%s\ntdll.dll")` = `<시스템루트>\SysWOW64\ntdll.dll` | `NtDll32Base` 전역 (아래) | 음수면 +0x214 `JS` → +0x2be `NtTerminateProcess` | +0x1d5, +0x1dd, +0x1e5, +0x20d |
| 10 | +0x220 ~ +0x266 | **32비트 ntdll 알림**: `CpuNotifyMapViewOfSection(글, NtDll32Base, 0, 크기, 0, 4)` (여기서 `Wow64Transition` 이 써짐 — (2c) 참고) → `Wow64pGetLegacyLpcPortName()` (환경변수 `_CLUSTER_NETWORK_NAME_` · `COMPUTERNAME` 읽고 `\BaseNamedObjects\Global\%s` 글 만듦) → +0x266 `JMP +0xc3` | | 음수면 +0x25f `JS` → +0x2d4 `NtTerminateProcess` | +0x258 (ret +0x25d), +0x261 (ret +0x266) |
| 11 | +0xc6 | **`Run64IfContextIs64(문맥)`** — 참이면 +0x26b 로 가서 **`RET`** (돌아옴) | 문맥 `+0xc0` (`R9`) 이 마커 `0xF0E0D0C0A0908070` 이면 `+0xf8` (`Rip`), 아니면 `+0x80` (`Rcx`) 를 시작 주소로 봄 → `PEB64.Ldr.InLoadOrderModuleList` 를 훑어 그 주소가 든 모듈이 **64비트 PE (AMD64, CLR 헤더 없음)** 이면 참 | — | +0xc6 (ret +0xcb), +0xcd JNZ |
| 12 | +0xd3 | CPU 영역 앞 DWORD 의 비트 1 (`0x2`) 켬 | CPU 영역 `+0x0` | | +0xd7 |
| 13 | +0xda ~ +0xf1 | `CpuThreadSize` (전역 `0x4c968`, wow64cpu 는 0) 가 0 이 아니면 스택에 잡고 0 으로 채움; 정보 `+0x8` 이 가리키는 칸 (`CPU영역+0x2f8`) ← 그 포인터 (0 이면 0) | | | +0xda, +0xf1 |
| 14 | +0xf4 | **`ThreadInit(CPU전용포인터)`** → `CpuThreadInit` → `BtFuncs+0x78` (`BTCpuGetBopCode`) 결과를 **TEB32 `+0xc0` 에 씀**, 이어 `BtFuncs+0x10` (`BTCpuThreadInit`) | TEB64 `+0x108`→TEB32 `+0xc4`, TEB64 `+0x1747`→TEB32 `+0xf77` (`IdealProcessor`), TEB64 `+0x14d0`→`Wow64Info +0x4` 의 비트 1 (소프트웨어 CPU 일 때만 `NtCreateEvent` + `TpAllocWait`; wow64cpu 는 `CpuFlags` 비트 0 만 켜서 안 탐) | 음수면 +0x31d `RtlRaiseStatus` | +0xf4 (ret +0xf9), `ThreadInit.asm +0x66,+0x72,+0x9c` |
| 15 | +0x101 ~ +0x116 | **`ThunkStartupContext64TO32(32비트문맥, 64비트문맥)`** 로 시작 주소·인자 몇 칸 고침 → **`Wow64SetupInitialCall(&정보)`**: 32비트 `Esp` 아래에 `CONTEXT32` 사본 자리 (`Esp − 0x2cc`, 4 정렬) 와 호출틀 (`[Esp+4] = 사본주소`, `[Esp+8] = NtDll32Base`) 을 만들고, **`Eip ← Ntdll32LoaderInitRoutine`** (`Wow64Info +0x8` 이 0 이 아니면 `Eip ← Wow64Info +0x8`, `Ecx ← Ntdll32LoaderInitRoutine`) | `Wow64SetupInitialCall.asm +0x15 ~ +0x76`; 사본 문맥플래그 `0x10017` 로 `CpuGetContext` | | +0x108 (ret +0x10d), +0x111 (ret +0x116) |
| 16 | +0x11a ~ +0x122 | CPU 영역 앞 DWORD 의 비트 0 (`0x1`) 켬 → **`RunCpuSimulation()`** = `BtFuncs+0x20` (`BTCpuSimulate`) 를 부름. 뒤에 **`INT3`** (+0x122). `RunCpuSimulation` 자신도 돌아오면 `INT 0x29` (코드 7) | `RunCpuSimulation.asm +0x4 ~ +0x17` | — | +0x11d (ret +0x122) |

**이 함수가 돌아오나** → **아니오** (일반 경로). 근거: ① wow64cpu 의 `BTCpuSimulate` 는 `RunSimulatedCode` 를 부르고 `JMP` 루프 (`BTCpuSimulate.asm +0x4/+0x9/+0xb`), `RunSimulatedCode` 에서 `CpupSyscallStub` 앞까지 (RVA `0x1920 ~ 0x1f6f`) 에는 `RET` 가 없음 (`_objdump_text_0x1000.s` 의 이 구간에 `ret` 0개; 바로 뒤 `CpupSyscallStub` `0x1f83/0x1f86` 의 `ret` 은 `CALL` 로 들어온 짧은 함수의 것); ② 32비트 → 64비트로 돌아오는 길은 모두 `CpupReturnFromSimulatedCode` (RVA `0x1a38`) → 터보 표 → 다시 `RunSimulatedCode` 머리 (`jmp 0x194f`) 로 가지 `ret` 로 안 감; ③ `Wow64LdrpInitialize +0x122 INT3` · `RunCpuSimulation +0x12 ~ +0x17` 이 "여기 오면 안 됨" 표시. **돌아오는 유일한 길 = 11 단계 `Run64IfContextIs64` 가 참 (+0x26b `RET`)**. → `LdrpInitializeProcess +0x17fe/+0x1801` 과 `_LdrpInitialize +0x9a/+0x9c` (이후 스레드; 돌아오면 `LdrpInitializeThread` 로 감, `_LdrpInitialize.asm +0x9f ~ +0xa2`) 는 그 갈래에서만 탄다. 시작 문맥이 64비트 모듈 안인지는 실행 때 값이라 x86 exe 에서 거짓이 되는지는 `[추측]` (동적 확인).

### `ProcessInit` 안 (wow64.dll rva `0x12e3c`, 531 바이트) — 차례

| 오프셋 | 하는 일 | 읽는 칸 / syscall / 레지스트리 | 실패 때 |
|---|---|---|---|
| +0x13 ~ +0x3e | `AlertByThreadIdEventLock` (SRW) 와 해시 표 `AlertByThreadIdEventHashTable` 만듦 | `RtlInitializeSRWLock` · `RtlCreateHashTable` | `0xc000009a` (+0x20d) |
| +0x46 ~ +0x54 | `Wow64ProtectMrdata(0)`; `Wow64Info[+0x0] = 0x1000` (기본 쪽 페이지 크기) | `NtProtectVirtualMemory` (`Wow64ProtectMrdata` 안) | |
| +0x5a | `InitializeContextMapper` — 64비트 `\KnownDlls` 의 `kernel32.dll` (+ 32비트 `\KnownDlls32` 의 `kernel32.dll`) 구역을 읽기로 사상해 `DebugBreak` 위치를 적어 둠 (문맥 주소 64↔32 변환용; `DllsToRemap` 에 kernel32 둘 + user32) | `LdrGetKnownDllSectionHandle` · `NtMapViewOfSection` · `NtUnmapViewOfSection` · `NtClose` | **실패해도 다음 항목으로 계속** (`_local_unwind` → 루프 `+0x229`) |
| +0x5f | `Wow64InitializeEmulatedSystemInformation` | **`NtQuerySystemInformation(0x3e = SystemEmulationBasicInformation, 0x40바이트)`**; 성공이면 `Wow64pMaxAddress` ← 결과 `+0x28` (최대 사용자 주소) | 실패하면 값을 안 바꿈 (오류 안 냄) |
| +0x6c ~ +0x9a | **`LdrSystemDllInitBlock` (64비트 ntdll 의 변수) 에서 32비트 ntdll 정보 복사**: `+0x48` → `NtDll32Base`, `+0x18` → `Ntdll32LoaderInitRoutine`, `+0x20` → `Ntdll32KiUserExceptionDispatcher`, `+0x28` → `Ntdll32KiUserApcDispatcher`, `+0x30` → `Ntdll32KiUserCallbackDispatcher` (32비트 값, `.mrdata` 전역: `NtDll32Base` `0x52018` · `Ntdll32LoaderInitRoutine` `0x52028` · `Ntdll32KiUserExceptionDispatcher` `0x5201c` · `Ntdll32KiUserApcDispatcher` `0x5200c` · `Ntdll32KiUserCallbackDispatcher` `0x52008`). `+0x38` 은 `ThunkStartupContext64TO32 +0x74` 가 32비트 `Eip` 와 비교 | 64비트 ntdll `LdrSystemDllInitBlock` 읽기 (커널이 채움) | |
| +0xa0 | `GetImageName` — `LdrLockLoaderLock` 로 `PEB64.ProcessParameters.ImagePathName` 의 **마지막 `\` 뒤 파일 이름** 복사 (없으면 `Unknown Image`) | PEB64 `+0x20` → `+0x60/+0x68` | |
| +0xa8 | `Wow64pInitializeFilePathRedirection` — `\??\<NtSystemRoot>` 글 · `\system32` 붙임 · `InitializeWindirFullObjectName` (**`NtOpenFile`(`\??\C:\WINDOWS` 류, 접근 `0x100080`, 옵션 `0x4020`) + `NtQueryObject(1)` + `NtClose`** 로 `\Device\Harddisk…\Windows` 전체 이름을 얻음) · `Wow64pKnownDllDirectories` 표 (`\KnownDlls` 0x8664 / **`\KnownDlls32` 0x14c** / `\KnownDllsArm32` 0x1c4) 중 `Wow64Info[+0x22]` 와 머신이 같은 칸의 디렉터리를 **`NtOpenDirectoryObject(접근 3)`** | `RtlGetNtSystemRoot` · `NtOpenFile` · `NtQueryObject` · `NtOpenDirectoryObject` | `NtOpenFile`/`NtQueryObject` 실패 → ProcessInit 이 실패 상태 반환 → **프로세스 죽음**. `\KnownDlls32` 열기 실패는 **통과** (그 칸 핸들 0 으로 남고 return 0) |
| +0xb5 ~ +0x14d | 서비스 표 `ServiceTables` (`0x52030`, 4 × 0x30바이트) 채움: `+0x0` ← `sdwhnt32` (wow64.dll 자기 것), `+0x30` ← `sdwhwin32` (wow64win), `+0x60` ← `sdwhcon` (wow64con), `+0x90` ← `sdwhbase` (wow64base) | import 한 `sdwh*` 변수 복사 | |
| +0x154 ~ +0x1a2 | **PEB32 에 64비트 PEB 칸 복사**: `Peb32` 전역 ← TEB32 `+0x30`; `PEB32 +0x1f8 (ActivationContextData)` ← `PEB64 +0x2f8`, `PEB32 +0x200 (SystemDefaultActivationContextData)` ← `PEB64 +0x308`, `PEB32 +0x1e8 (pShimData)` 가 0 이면 ← `PEB64 +0x2d8` (아래 읽는 칸 표) | | |
| +0x1ab | **`CpuLoadBinaryTranslator(이미지 이름)`** (아래) | | 음수면 그 상태로 끝 (+0x1b4 `JS`) |
| +0x1b6 ~ +0x1c7 | `BtFuncs[0]` = **`BTCpuProcessInit(이미지 이름, &CpuThreadSize)`** 부름 (ret +0x1c9) | wow64cpu 안 (2절) | |
| +0x1e6 | `Wow64pInitializeCrossProcessMechanism` — 이름 없는 구역 **`NtCreateSection(접근 0xf001f)`** + `NtMapViewOfSection` 으로 교차 프로세스 작업 칸을 만들고 `RtlWow64PushCrossProcessWorkOntoFreeList` 로 채움, `Wow64ProcessPendingCrossProcessItemsLock` 초기화 | `NtCreateSection` · `NtMapViewOfSection` · `NtUnmapViewOfSection` · `NtClose` | 음수 → +0x1ef `JS` → ProcessInit 이 실패 반환 |
| +0x1f3 | `Wow64ProtectMrdata(1)` | | |

### `CpuLoadBinaryTranslator` / `CpuGetBinaryTranslatorPath` (CPU dll 고르기·올리기)

- **레지스트리** (`CpuGetBinaryTranslatorPath.asm`): +0xad `NtOpenKey(접근 0x20019, 키 = \Registry\Machine\Software\Microsoft\Wow64\x86)` (키 글 = wow64.dll `.rdata` `0x3e480`, 46글자; ret +0xb4) → +0x122/+0x129 `NtQueryValueKey(키, **값 이름 = 이미지 파일 이름**, 정보 클래스 2)` → 값이 없거나 타입이 REG_SZ(1)/REG_EXPAND_SZ(2) 가 아니면 +0x13d 이후 **값 이름 `""` (기본값)** 으로 다시 (+0x18d `NtQueryValueKey`, ret +0x194). 읽은 글이 `\` 를 안 담으면 (`wcsrchr`, +0x1d8) `%ws\system32\%ws` (시스템루트 + 글) 로 만듦 (+0x20e `RtlUnicodeStringPrintf`, ret +0x213).
  - **키가 없으면** (`NtOpenKey` 가 `0xc0000034`, +0xb9 `CMP` / +0xbe `JZ`) → +0x253 (분기 목적지) ~ +0x27b `RtlStringCbPrintfW("%s", "wow64cpu.dll")` (ret +0x280) (글 `0x3e530`, 포맷 `0x3e54c`) → 똑같이 `…\system32\wow64cpu.dll`. 기본값도 못 읽으면 (+0x19f `JS`) 실패 상태로 끝남.
  - 즉 **레지스트리로 고르고, 시험 VM 에 키가 없으면 `wow64cpu.dll`**. 키·값은 `NtOpenKey`/`NtQueryValueKey` 로 읽는 것이라 에뮬레이터 레지스트리 시드에 키가 없어도 (`0xc0000034`) 길이 열려 있음.
- **올리기** (`CpuLoadBinaryTranslator.asm`): +0x51 `CpuGetBinaryTranslatorPath` → +0x6d `memset(BtFuncs, 0, 0x110)` → +0x7d `Wow64ProtectMrdata(0)` → +0x8e/+0x90 **`LdrLoadDll(0, 0, &경로, &핸들)`** (ret +0x97) → 표 `0x3a220` (34칸 × 0x18바이트 `{이름 포인터, 출력 칸, 필수 바이트}`) 로 `RtlInitAnsiStringEx` (+0xcf, ret +0xd6) + `LdrGetProcedureAddress` (+0xf5, ret +0xfc) → `BtFuncs` (`0x520f0`, 34 × 8바이트, `.mrdata`) 채움.
  - **필수 (바이트 1)**: `BTCpuProcessInit` (+0x0) · `BTCpuSimulate` (+0x20) · `BTCpuGetBopCode` (+0x78). 나머지 31칸은 **없어도 통과** (상태가 `0xc0000139`/`0xc000007a` 이고 바이트 0 이면 건너뜀; `+0x105 ~ +0x11e` 분기). 칸 이름은 표 순서대로 `BTCpuProcessInit · ProcessTerm · ThreadInit · ThreadTerm · Simulate · ResetFloatingPoint · ResetToConsistentState · NotifyDllLoad · NotifyDllUnload · PrepareForDebuggerAttach · NotifyBeforeFork · NotifyAfterFork · NotifyAffinityChange · SuspendLocalThread · IsProcessorFeaturePresent · GetBopCode · GetContext · SetContext · TurboThunkControl · NotifyMemoryAlloc · NotifyMemoryFree · NotifyMemoryProtect · FlushInstructionCache2 · NotifyMapViewOfSection · NotifyUnmapViewOfSection · UpdateProcessorInformation · NotifyReadFile · CfgDispatchControl · UseChpeFile · OptimizeChpeImportThunks · NotifyProcessExecuteFlagsChange · ProcessDebugEvent · FlushInstructionCacheHeavy · NotifyMemoryDirty`. wow64cpu.dll 은 이 중 7개 (`ProcessInit` · `ThreadInit` · `Simulate` · `ResetToConsistentState` · `GetBopCode` · `GetContext` · `SetContext`) 와 `TurboThunkControl` 만 내보냄 (`PE 내보내기 표`).
  - 실패하면 `$fin$0` (`CpuLoadBinaryTranslator$fin$0`) 이 `BtFuncs` 를 0 으로 지우고 `LdrUnloadDll`.

### 읽는 / 쓰는 칸 모음 (A3 "그 동안 읽는 PEB32 · TEB32 · 64비트 PEB/TEB 칸")

| 대상 | 칸 | 읽기/쓰기 | 누가 · 줄 |
|---|---|---|---|
| TEB64 | `+0x180c WowTebOffset` | 읽기 | 거의 모든 함수 (`Wow64LdrpInitialize +0x55`, `ProcessInit +0x15d`, `ThreadInit +0x50`, `BTCpuProcessInit +0x24`, `CpuThreadInit +0x13`) |
| TEB64 | `+0x1488 TlsSlots[1]` (CPU 영역 포인터) | 읽기만 (wow64.dll 7곳 · wow64cpu 읽기) — 쓰는 줄 없음 → **커널이 지어 줌** | `Wow64LdrpInitialize +0x9f`, `RunSimulatedCode +0x20` |
| TEB64 | `+0x14d0 TlsSlots[10]` | **쓰기** (`Wow64Info` 주소) | `Wow64LdrpInitialize +0x8f` (wow64.dll 안 쓰는 곳은 이 한 줄) |
| TEB64 | `+0x14d8 TlsSlots[11]` | 쓰기 (XState 위치, CPU 가 XState 쓸 때만) | `BTCpuThreadInit +0x54` |
| TEB64 | `+0x1498 TlsSlots[3]` · `+0x14e0 TlsSlots[12]` | 읽기/쓰기 (임시 할당 목록·호출 틀 연결 목록) | `Wow64SystemServiceEx +0x7d`, `+0x12b`, +0x227 |
| TEB64 | `+0x108 CurrentLocale`, `+0x1747 IdealProcessor` | 읽기 (→ TEB32 로 복사) | `ThreadInit +0x60`, `+0x6c` |
| TEB64 | `+0x0` (`NtTib.ExceptionList`) | 읽기 (`GS:[0]` → **`WRFSBASE`** 로 FS 기준에 넣음, 조건 있음) | `RunSimulatedCode +0xb4 ~ +0xbd` (RVA `0x19d4`) — 값 = TEB32 주소여야 함 `[추측]`, 쓰는 쪽은 커널 (sast/135 C 6) |
| TEB32 | `+0x30 ProcessEnvironmentBlock` | 읽기 | `Wow64LdrpInitialize +0x69`, `ProcessInit +0x171` |
| TEB32 | `+0xc0 WOW32Reserved` | **쓰기** (`BTCpuGetBopCode` 의 하위 32비트) | `CpuThreadInit +0x32` |
| TEB32 | `+0xc4 CurrentLocale` · `+0xf77 IdealProcessor` | 쓰기 | `ThreadInit +0x66`, `+0x72` |
| TEB32 | `+0xe38 TlsSlots[10]` | 읽기 (TEB32 포인터로 불렸을 때 `Wow64Info`) | `ThreadInit +0xd0` (정상 경로는 TEB64 `+0x14d0` 을 읽음 — TEB64 는 `+0x18` 이 `SubSystemTib` 라 `CMP ECX,[RCX+0x18]` 가 다름) |
| PEB64 | `+0x10 ImageBaseAddress` · `+0x20 ProcessParameters` (`+0x60/+0x68 ImagePathName`) · `+0x18 Ldr` | 읽기 | `Wow64LdrpInitialize +0x180~+0x1b6`, `GetImageName +0x20 ~ +0x4d`, `Run64IfContextIs64 +0x35 ~ +0x46` (`Ldr+0x10` 목록) |
| PEB64 | `+0x2d8 pShimData` · `+0x2f8 ActivationContextData` · `+0x308 SystemDefaultActivationContextData` | 읽기 (하위 32비트만) | `ProcessInit +0x174, +0x18e, +0x19c` |
| PEB32 | `+0x1e8` · `+0x1f8` · `+0x200` | 쓰기 (위 칸들) | `ProcessInit +0x188, +0x194, +0x1a2` |
| PEB32 뒤 (`Wow64Info`, `+0x490`) | `+0x0` ← `0x1000`; `+0x4` (CpuFlags 로 보임 `[추측]`) 읽기 + wow64cpu 가 비트 0 켬; `+0x8` 읽기 (0 이 아니면 `Eip` 바꿈 · 호출 뒤 `Wow64SetupForInstrumentationReturn`; 쓰는 곳은 이번 범위에서 못 봄 `[공백]`); `+0x20 ← 0x8664`; `+0x22 ← Machine` | 읽기/쓰기 | `ProcessInit +0x54`, `ThreadInit +0x98`, `BTCpuProcessInit +0x43`, `Wow64SetupInitialCall +0x58`, `Wow64LdrpInitialize +0x12f/+0x13d`, `Wow64SystemServiceEx +0x235` |
| 64비트 `LdrSystemDllInitBlock` | `+0x18 +0x20 +0x28 +0x30 +0x38 +0x48` | 읽기 | `ProcessInit +0x6c ~ +0x9a`, `ThunkStartupContext64TO32 +0x66/+0x74`. (그 밖 `+0xc8`/`+0xd0` 을 읽는 곳이 wow64.dll 안 둘 있음 — rva `0x779f`, `0x1d1e1`, CFG 쪽으로 보임 `[추측]`, 이번 범위 밖) |
| KUSER_SHARED_DATA | (wow64cpu) `+0x308 SystemCall` · `+0x28a` (= `ProcessorFeatures[22]`, `RDWRFSGSBASE` 로 보임 `[추측]`) · (ntdll 쪽) `+0x3d8 XState` | 읽기 | `BTCpuProcessInit +0x4a`, `CpupSyscallStub +0x7`, `RunSimulatedCode +0x9f` |

### 부르는 syscall (이름) — 초기화 동안

`NtQuerySystemInformation(0x3e)` · `NtOpenFile` · `NtQueryObject` · `NtClose` · `NtOpenDirectoryObject` · `NtOpenKey` · `NtQueryValueKey` · (`LdrLoadDll` → 64비트 로더 안 syscall 들) · `LdrGetKnownDllSectionHandle`/`NtMapViewOfSection`/`NtUnmapViewOfSection` (KnownDlls kernel32·user32) · `NtProtectVirtualMemory` (wow64.dll `.mrdata` 켜고 끄기 · `Wow64Transition` 쓰기 · wow64cpu `W64SVC` 쪽 · 터보 표) · `NtQueryVirtualMemory(클래스 6 = MemoryImageInformation)` (`CpuNotifyMapViewOfSection +0x5b`) · `NtQueryInformationProcess(0x34 = ProcessMitigationPolicy)` (`BTCpuProcessInit +0x7b`, `KUSD.SystemCall != 1` 일 때만) · `NtCreateSection` · `NtMapViewOfSection` · `NtUnmapViewOfSection` (교차 프로세스 칸) · `NtTerminateProcess` (실패 때). 스레드 초기화는 소프트웨어 CPU 가 아니면 syscall 없음. wow64.dll 의 이 호출들은 모두 **64비트 ntdll 의 `Nt*` 를 import 해서 부르는 것** (wow64.dll → ntdll `Nt*` → `syscall`).

---

## (2) A2 — wow64cpu.dll

### (2a) 전환 꼴 표

| 방향 | 어디 (RVA · PDB 이름) | 명령 | 셀렉터 · 모양 | 근거 |
|---|---|---|---|---|
| 64 → 32 (빠른 길) | `RunSimulatedCode` +0x6b (RVA `0x198b`), 터보 썽크들 끝 (`0x1ab6`, `0x1b15`, `0x1bfa`, … 모두 `jmp FWORD [r14]` 10곳) | **`JMPF m16:32 [R14]`** | 메모리 `[R14]` = `{Eip(32비트) , 0x0023}` (+0x4e `MOV [RSP+4],0x23` 뒤 +0x63 `MOV [RSP],R9D(Eip)`). 앞에 `MOV SS,0x2b` (+0x5c), `MOV ESP,[R13+0x48]` (+0x67). 복원하는 레지스터: `Edi Esi Ebx Ebp Eax` | `RunSimulatedCode.asm` +0x4e ~ +0x6b |
| 64 → 32 (상태 다시 싣는 길) | `RunSimulatedCode` +0x116 (RVA `0x1a36`; 같은 꼴 4곳 더: `0x1b4b` `0x1dce` `0x1e52` `0x1ece`) | **`IRETQ`** | 틀: `[RSP]=Eip`, `+8 = 0x23` (CS), `+0x10 = EFlags` (TF 포함 값을 올리고 저장본에선 TF 끔 `& 0xfffffeff`), `+0x18 = Esp`, `+0x20 = 0x2b` (SS). 앞에 `FXRSTOR [R13+0x50]`, (XState 있으면 `RtlXRestore`), `DS/ES/GS = 0x2b`, FS = `0x53` (`ProcessorFeatures[22]` 가 0 이면 `MOV FS,0x53`; 아니면 `WRFSBASE` 에 `GS:[0]`) , `ECX`/`EDX` 도 복원 | `RunSimulatedCode.asm` +0x6e ~ +0x116 |
| 어느 길로 가나 | `RunSimulatedCode` +0x2f `BTR DWORD [CPU영역+0], 0` — 비트 0 (이전 켜짐) 이면 **느린 길 (IRETQ)**, 아니면 빠른 길. 처음 들어갈 땐 `Wow64LdrpInitialize +0x11a` 가 비트 0 을 켜 둠 → **첫 진입은 `IRETQ`** (CS `0x23`, SS `0x2b`) | | `RunSimulatedCode.asm` +0x2f/+0x35 |
| 32 → 64 (빠른) | `KiFastSystemCall` = 구역 **`W64SVC`** RVA `0x7000` (16바이트, 읽기 전용 데이터 구역이었다가 `BTCpuProcessInit` 가 실행 가능으로 바꿈) | **`JMPF ptr16:32` (`EA`)** → 이어서 RVA `0x7009` 의 **64비트** `JMP [R15+0xf8]` → 터보 표 칸 31 = `CpupReturnFromSimulatedCode` | 파일에 박힌 8바이트 `09 70 00 80 34 00 00 00` 는 **`DIR64` 재배치 (RVA 0x7001)** 가 걸려 있어서, 이미지가 **4GB 아래** 에 올라오면 위 dword 가 `0x34 − 1 = 0x33` → **CS `0x33`**, 아래 dword = 실제 주소 하위 32비트가 됨. (`wow64cpu` 재배치표: 페이지 `0x7000` 에 항목 하나 = 오프셋 `0x7001`, 타입 10) | `_ndisasm_wow64svc.txt`, PE 재배치표 |
| 32 → 64 (느린) | `KiFastSystemCall2` = 구역 **`WOW64SVC`** RVA `0x3010` (구역 `0x3000`, 0x2d바이트, 실행) | `push 0x33; push eax; call $+5; pop eax; add eax,0xc; xchg eax,[esp]; jmp far [esp]` = **`JMPF m16:32`**, 목적지 `0x3024` (CS `0x33`) → `add rsp,8` → `jmp 0x1a38` (`CpupReturnFromSimulatedCode`) | `_ndisasm_wow64svc.txt` |
| 어느 걸 고르나 | **`BTCpuGetBopCode`** (`+0x0 CMP [CpupSystemCallFast],0`, `+0x7/+0xe` 두 주소, `+0x15 CMOVNZ`) = `CpupSystemCallFast` 가 1 이면 `KiFastSystemCall` (0x7000), 아니면 `KiFastSystemCall2` (0x3010). `CpupSystemCallFast` 는 `BTCpuProcessInit +0xd8` 에서만 1 로 됨: **조건** `KUSD+0x308 (SystemCall) != 1` · `NtQueryInformationProcess(ProcessMitigationPolicy)` 의 "동적 코드 금지" 비트 0 이 꺼져 있음 · `NtProtectVirtualMemory(W64SVC 페이지, 0x20 = PAGE_EXECUTE_READ)` 성공 | | `BTCpuProcessInit.asm` +0x4a ~ +0xd8, `BTCpuGetBopCode.asm` +0x0 ~ +0x19 |
| 32 → 64 후 처리 | `CpupReturnFromSimulatedCode` (RVA `0x1a38`) | `XCHG R14,RSP` (스택을 64비트로 되돌림) · 32비트 `Eip`/`Esp`/`EFlags`/`Edi`/`Esi`/`Ebx`/`Ebp` 를 CPU 영역에 저장 · `R11` = 32비트 스택 인자 포인터 · **`ECX = EAX >> 16` 로 `JMP [R15 + RCX*8]`** (터보 표 `TurboThunkDispatch`, `0x41b0`, 32칸). 칸 0 = `ServiceNoTurbo` (RVA `0x1a6e`): **`ECX = EAX`, `RDX = R11`, `CALL [Wow64SystemServiceEx]`** (ret RVA `0x1a79`), 결과 `EAX` 를 `[R13+0x34]` 에 저장, `JMP 0x194f` (다시 32비트로) | `_objdump_text_0x1000.s` `1a38:` ~ `1a7d:`; `_calls.tsv` (`TurboDispatchJumpAddressEnd +0x5/+0xb Wow64SystemServiceEx`) |
| 64비트 `syscall` | `CpupSyscallStub` (RVA `0x1f70`) | `KUSD+0x308 & 1` 이면 `INT 0x2e`, 아니면 **`SYSCALL`** (+0x11) | **터보 썽크** (`ReadWriteFile` · `DeviceIoctlFile` · `RemoveIoCompletion` · `WaitForMultipleObjects` · 여러 `Thunk*Arg*`) 가 wow64.dll 을 안 거치고 **wow64cpu.dll 안에서 직접** 커널로 감 (`RAX` = 64비트 서비스 번호, 예: `WaitForMultipleObjects` = `0x1a`). → 에뮬레이터 커널은 **`syscall` 명령 위치가 64비트 ntdll 밖 (wow64cpu.dll)** 일 수 있음에 대비 | `CpupSyscallStub.asm`, `WaitForMultipleObjects.asm +0x0/+0x1f` |

- `RETF` 는 **없음** (`_objdump_text_0x1000.s` 전체에서 `retf`/`lret` 0, `iretq` 5, `jmp FWORD PTR [r14]` 10).
- `R12`(= TEB64) · `R13`(= CPU 영역 + 0x80) · `R14`(64비트 스택 보관) · `R15`(터보 표) 는 **32비트 구간 내내 64비트 레지스터에 그대로 둬야 함** (`RunSimulatedCode +0x10 ~ +0x4b`, `CpupReturnFromSimulatedCode +0x0 XCHG R14,RSP`). 32비트 코드는 R8 ~ R15 · 각 상위 32비트를 못 건드림.
- **wow64cpu.dll 은 4GB 아래에 올라와야 한다**: `BTCpuGetBopCode` 가 주는 주소를 `CpuThreadInit +0x32 MOV [TEB32+0xc0],EAX` 가 **하위 32비트만** 저장하고, 32비트 코드가 그 주소의 `WOW64SVC`/`W64SVC` 를 직접 실행하며, 위 `DIR64` 재배치 꼼수가 맞으려면 기준이 4GB 아래여야 함. 선호 기준 주소는 `0x180000000` (PE). **누가 낮게 올리나는 이번에 안 봄** `[공백]` (로더/커널 `MiSelectImageBase`, 동적 `lm` 확인).

### (2b) CPU 영역 칸 표 (TEB64 `+0x1488` 이 가리킴; 이 문서에서 "CPU 영역" = `CpuReserved`)

64비트 ntdll 의 `RtlWow64GetCpuAreaInfo(CpuReserved, 머신 0 → 영역의 `+0x2` 에서 읽음, 출력 정보 0x30바이트)` 가 계산한다 (`RtlWow64GetCpuAreaInfo.asm`, 0x14c 갈래 +0x35 ~ +0x1a3). KUSD `XState.EnabledFeatures` 가 켜져 있지 않을 때의 값 (`0x7ffe03d8 & 0x40000000000009fc == 0` 인 경우, +0x77 ~ +0x9d 에서 갈림):

| CPU 영역 오프셋 | 이름 (우리가 붙임) | 크기 | 누가 쓰나 · 읽나 | 근거 |
|---|---|---|---|---|
| `+0x0` | 플래그 (WORD, 하위 비트만 씀). 비트 0 = "상태 다시 싣기" , 비트 1 = ? | 2 (DWORD 로 `BTR`) | 쓰기: `Wow64LdrpInitialize +0xd7` (`\|= 2`) · `+0x11a` (`\|= 1`), `BTCpuResetToConsistentState +0x91` (`\|= 1`), 지우기 `RunSimulatedCode +0x2f BTR`. 초기값은 커널 `[추측]` | 위 줄들 |
| `+0x2` | **Machine** (WORD, `0x14c`) | 2 | 읽기: `RtlWow64GetCpuAreaInfo +0x24` (`MOVZX EDI,[RCX+2]`), `RtlWow64GetCurrentCpuArea +0x39`. 쓰는 줄은 wow64.dll·wow64cpu 에 없음 → 커널 `[추측]` | `RtlWow64GetCurrentCpuArea.asm +0x39` |
| `+0x4` | **`WOW64_CONTEXT` (= x86 `CONTEXT`)** `[0x2cc]` — `정보 +0x0` 이 이 주소 (`(CpuReserved + 7) & ~3`) | `0x2cc` | 아래 레지스터 칸 | `RtlWow64GetCpuAreaInfo +0x159 ~ +0x172` |
| `+0xa0 Edi` · `+0xa4 Esi` · `+0xa8 Ebx` · `+0xac Edx` · `+0xb0 Ecx` · `+0xb4 Eax` · `+0xb8 Ebp` · `+0xbc Eip` · `+0xc0 SegCs` · `+0xc4 EFlags` · `+0xc8 Esp` · `+0xcc SegSs` | 문맥 안 칸 (문맥 오프셋 `+0x9c ~ +0xc8` 에 4 더함. wow64cpu 는 `R13 = 영역+0x80` 기준 `+0x20 ~ +0x4c` 로 접근) | 각 4 | 쓰기 (32 → 64 때 저장): `Edi Esi Ebx Ebp Eip Esp EFlags` (`CpupReturnFromSimulatedCode +0x3 ~ +0x29`), `Eax` (`ServiceNoTurbo +0xb`). 읽기 (64 → 32): 전부 (`RunSimulatedCode +0x6 ~ +0x116`). `Eip`/`Esp`/`Ecx` 를 `Wow64SetupInitialCall` 이 고침 | `RunSimulatedCode.asm`, `Wow64SetupInitialCall.asm +0x15 ~ +0x76` |
| `+0xd0` | `ExtendedRegisters` (FXSAVE 영역 512바이트) | 0x200 | 읽기 `FXRSTOR [R13+0x50]` (`RunSimulatedCode +0x6e`) | |
| `+0x2d0` | `CONTEXT_EX` (4칸 × 8 = 0x20; `XState.Offset` = `+0x2e0`) — `ContextFlags & 0x40` (XState) 가 켜졌을 때만 쓰임 | 0x20 | 읽기: `BTCpuThreadInit +0x3a` (`[문맥+0x2dc]`) → TEB64 `+0x14d8` ← `문맥 + 0xcc + 그 값` (**`0xcc` 의 뜻은 못 정함** `[공백]`) | `BTCpuThreadInit.asm +0x3a ~ +0x54` |
| `+0x2f8` | CPU 전용 포인터 칸 (8, `정보 +0x8` 이 이 주소) | 8 | 쓰기 `Wow64LdrpInitialize +0xf1` (`[정보+8] ← CpuThreadSize 만큼 잡은 칸의 주소`, wow64cpu 는 0) | `Wow64LdrpInitialize.asm +0xea ~ +0xf1` |
| `+0x300` | (끝. `정보 +0x28` = `(정보+8 + 0xb) & ~3`) | — | | `RtlWow64GetCpuAreaInfo +0x17d` |

- 정보 구조 (0x30): `+0x0` 문맥 · `+0x8` CPU 전용 칸 주소 · `+0x10` 문맥 (같은 값) · `+0x18` CPU 영역 시작 · `+0x20` 문맥 플래그 (`0x10000`, XState 켜지면 `0x10040`) · `+0x24` Machine · `+0x28` 끝. (`RtlWow64GetCpuAreaInfo +0x172 ~ +0x1a3`; wow64.dll 이 `[RBP+0x18]` `[RBP+0x24]` 를 읽음.)
- **누가 TEB64 `+0x1488` 에 값을 쓰나**: wow64.dll · wow64cpu.dll 전체를 풀어 본 결과 읽는 줄만 있음 (`wow64.dll` 일곱 곳, wow64cpu `RunSimulatedCode +0x20` 등) → **커널 (sast/135 C 7 `PspWow64SetupCpuArea` 쪽)** 이 만든다 `[추측]` (이름은 sast/135 결과서에서 대조).
- XState 가 켜진 CPU 에서는 크기·칸이 달라짐 (문맥 `0x4d0` 대, `RtlpGetEntireXStateAreaLength2` 사용). 에뮬레이터가 KUSD `+0x3d8` 을 어떻게 두는지에 따라 `RtlWow64GetCpuAreaInfo` (진짜 ntdll 코드) 가 알아서 계산함.

### (2c) `Wow64Transition` 누가 쓰나 · 어디를 가리키나

- 32비트 ntdll 의 `Wow64Transition` (내보낸 4바이트 변수) 에는 **wow64.dll** 이 씀: `CpuNotifyMapViewOfSection` (+0xb5 `CALL CpupResolveReverseImports`, ret +0xba) → `CpupResolveReverseImports` : +0x2a `BtFuncs+0x78` (`BTCpuGetBopCode`) 호출 → +0x5b `RtlFindExportedRoutineByName(이미지, "Wow64Transition")` (글 `0x3e068`) → +0x9a `NtProtectVirtualMemory(4바이트, PAGE_READWRITE=4)` → **+0xb2 `MOV [RSI],EAX`** (RVA `0xbe5a`) → +0xc9 `NtProtectVirtualMemory(원래 보호 복원)`.
- 이 길은 **Wow64Transition 이름이 내보내진 이미지만** 통과: `CpuNotifyMapViewOfSection +0x93 ~ +0xb0` 이 이미지 `MajorSubsystemVersion/MinorSubsystemVersion` (`NT+0x48/+0x4a`) 이 `KUSD.NtMajorVersion/NtMinorVersion` (`0x7ffe026c/0x270`) 과 같을 때만 부름. 초기화 때 wow64 가 부르는 곳은 두 군데 (exe `+0x1c8`, 32비트 ntdll `+0x258`).
- 값 = `BTCpuGetBopCode()` = **`KiFastSystemCall` (wow64cpu RVA 0x7000 → 이미지 기준 + 0x7000) 또는 `KiFastSystemCall2` (RVA 0x3010)**. 같은 값이 스레드마다 TEB32 `+0xc0` (`WOW32Reserved`) 에도 들어감 (`CpuThreadInit +0x32`). → 32비트 `Nt*` 스텁이 `Wow64Transition` 쪽을 부르는지 `fs:[0xc0]` 쪽을 부르는지는 **32비트 ntdll 본문 (Q253c)** 에서.

---

## (4) A4 — wow64.dll syscall 다리

| 항목 | 값 · 모양 | 근거 |
|---|---|---|
| 입구 | **`Wow64SystemServiceEx(ECX = 번호(EAX 그대로), RDX = 32비트 스택의 인자 포인터)`** (rva `0x8c30`, 712바이트, 내보내기 #30) | `Wow64SystemServiceEx.asm +0x28 (R15=RDX), +0x2b (EDI=ECX)`; 부르는 곳 `ServiceNoTurbo` (wow64cpu RVA `0x1a6e`) |
| 번호 고르기 | `EDX = (번호 >> 12) & 3` = **표 번호**, `EDI = 번호 & 0xfff` = **칸 번호**. 번호의 **비트 16 이상은 wow64cpu 가 터보 번호로 씀** (`ECX = EAX >> 16`) 이고 이 함수는 무시 | `+0x84 ~ +0x8c`, `CpupReturnFromSimulatedCode` (RVA `0x1a65`) |
| 서비스 표 | 전역 `ServiceTables` (`0x52030`) = **4개 × 0x30바이트**: 0 = nt (`sdwhnt32`, wow64.dll 자기 것), 1 = win32k (`sdwhwin32`, wow64win), 2 = console (`sdwhcon`, wow64con), 3 = base (`sdwhbase`, wow64base). 칸 모양 `{+0x0 함수 포인터 배열, +0x8 0, +0x10 한계, +0x18 인자 바이트 표 (서비스당 1바이트: 예 `NtAccessCheck` 0x20), +0x20/+0x24/+0x28 오류 표}`. 네 표 모두 한계 `+0x10` = `0x1000`, nt 와 base 의 오류 칸은 0 (`ProcessInit +0xb5 ~ +0x14d` 복사) | `ProcessInit.asm`, PE 데이터 `sdwhnt32` (`0x3a1f0`), wow64win/con/base PE 데이터 |
| 표 칸 수 | **nt 509** (`whNtAccessCheck` 0 번 … `whNtWow64GetCurrentProcessorNumber` 508 번; 칸 번호 = 커널 서비스 번호와 같은 순서 — 0 `NtAccessCheck`, 1 `NtWorkerFactoryWorkerReady`, 2 `NtAcceptConnectPort` … ), **win32k 1497** (`whNtUserGetThreadState` … `whNtVisualCaptureBits`), **console 4** (`whUserConnectToServer` · `whCallUserpExitWindowsEx` · `whEndTask` · `whLogon`), **base 16** (`whNtWow64CsrBasepRefreshIniFileMapping` · `…CreateProcess` · `…CreateProcess2` · `…NlsGetUserInfo` · `whNtWow64ConsoleLaunchServerProcess` 등). PDB 공개 심볼 이름으로 칸마다 확인 (nt 표 끝 `whNtWow64*` 20개 (칸 489 ~ 508, 32비트 전용 확장 번호)). | PDB `ms/sym/*.pdb` + PE 표 |
| 이름 꼴 | nt · win32k · base: **`whNt…`** (예: `whNtReadVirtualMemory`, `whNtQueryInformationProcess`, `whNtCallbackReturn`); console: `wh…` (`NtWow64` 안 붙음) | |
| 호출 | `+0x12e`/`+0x132` 표 → 함수 포인터, 3개만 주소 비교해 바로 부르고 (`Wow64SystemServiceEx` +0x174 → `whNtCallbackReturn`, +0x169 → `whNtQueryInformationProcess`, +0x18b → `whNtReadVirtualMemory`) 나머지는 `+0x199 CALL RAX` (`whNt*(RCX = 인자 포인터)`, ret +0x19b; 로그 훅 `pfnWow64LogSystemService` 가 있으면 +0x1ad). `whNt*` 는 `RCX[0], [4], [8] …` 로 32비트 인자를 읽어 **64비트 ntdll 의 `Nt*` 를 import 해서 부르고** (`whNtReadVirtualMemory +0x4f`) 결과를 32비트 칸에 돌려줌 → ntdll `syscall` → 커널 | `Wow64SystemServiceEx.asm`, `whNtReadVirtualMemory.asm +0x1f ~ +0x72` |
| 범위 검사 · 실패 상태 | **+0xa0 `CMP EDI,[표+0x10]`, +0xa5 `JA` → `0xc000001c` (`STATUS_INVALID_SYSTEM_SERVICE`)** (+0x26e). 그런데 네 표 모두 한계가 `0x1000` 이고 칸 번호는 `& 0xfff` (≤ `0xfff`) 라 **이 검사는 항상 통과** (표 끝 넘는 번호는 이 함수가 막지 않음; 예: console 은 4칸뿐). | `+0xa0 ~ +0xa5`, `+0x26e`; 한계 값은 PE 데이터 |
| 변환 중 예외 | `Wow64SystemServiceEx$filt$0`: 예외 `0x80000003` (BREAKPOINT) · `0xc0000008` (INVALID_HANDLE) · `0xc0000235` (HANDLE_NOT_CLOSABLE) 면 `Wow64PassExceptionToGuest` 로 32비트 쪽에 넘김, 필터는 항상 1. 처리기 (`+0x1c1 ~ +0x1df`, 31바이트, Ghidra 가 못 읽은 틈) 가 **`Wow64HandleSystemServiceError(예외코드, 틀)`** 호출 → 표 `+0x20` 오류 종류 (1 = `LastError` 를 `RtlNtStatusToDosError` 로 TEB64 `+0x68` 에, 2 = 값만 돌려줌, 3 = 둘 다, 그 밖 `0xc000000d`) 로 정함. **nt 표는 `+0x20 = 0` 이라 상태 그대로**. | `Wow64SystemServiceEx.asm` 끝 틈 안내, `Wow64HandleSystemServiceError.asm +0x2c ~ +0xa7`, 처리기 바이트는 wow64.dll `.text` rva `0x8df1` |
| win32k (wow64win.dll) | **같은 꼴**: 내보낸 변수 `sdwhwin32` (wow64win RVA `0x59398`) = `{함수 배열, 0, 한계 0x1000, 인자 바이트 표, 0, …}`. 다른 내보내기는 `NtWow64UserConnectHook` 하나. wow64win 은 `ntdll` 15 + `wow64.dll` 9 (`Wow64LogPrint` · `Wow64AllocThreadHeap` · `Wow64KiUserCallbackDispatcher` …) 만 import | wow64win.dll PE 데이터 (analyze 안 함) · `ms/sym/wow64win.dll.pdb` 이름표 |

## (5) A5 — wow64base · wow64con

| 질문 | 답 | 근거 (이름만) |
|---|---|---|
| 초기화에서 올라오나 | **예**, wow64.dll 이 올라올 때 같이 | wow64.dll 의 **정적 import 표** = `ntdll.dll` (598개), `wow64base.dll` (`sdwhbase`), `wow64win.dll` (`sdwhwin32`), `wow64con.dll` (`sdwhcon`). 한 import 마다 변수 하나 (표). wow64con.dll 은 `wow64win.dll` (`NtWow64UserConnectHook`) 과 `ntdll` (`CsrClientConnectToServer` · `CsrClientCallServer` …) 를 import. wow64base.dll 은 `wow64.dll` (`RedirectDosPath` 류 셋) 과 `ntdll` (`Csr*` · `DbgPrintEx` …) import. wow64win.dll 은 `wow64.dll` 아홉과 `ntdll` 15 |
| 누가 올리나 | **64비트 로더가 `LdrpLoadDll(wow64.dll)` 의 import 훑기 때** (= `LdrpLoadWow64 +0xa8`, 5.1 절). wow64.dll 쪽에 `LoadLibrary`·`LdrLoadDll` 글은 없음 (`LdrLoadDll` 은 CPU dll 올리기 한 곳 `CpuLoadBinaryTranslator +0x90` 뿐) | wow64.dll `.rdata` 에서 `wow64base.dll`·`wow64win.dll`·`wow64con.dll` 글은 import 이름으로만 나옴 (찾아본 결과) |
| 레지스트리로 고르나 | 아니오 (이 셋은 고정) | 위 |

- 참고 (이번 범위 밖, 이름만): wow64win.dll 안에 `wow64lg2.dll` 글이 있음 (로깅 DLL, 켜졌을 때 `pfnWow64LogSystemService` 로 연결되는 것으로 보임 `[추측]`). wow64.dll 안에 `Wow6432Node` 레지스트리 리다이렉트 글 (`\REGISTRY\MACHINE\SOFTWARE\Wow6432Node` · `…\Classes\Wow6432Node`) 이 있음 → 32비트 `NtOpenKey` 계열이 들어올 때 쓰임 (초기화는 아님, AR07 이후).

---

## 지금 우리 (PE_emulator-AR) 맞나·틀리나 — 원문 "답이 오면 쓰는 곳"

- **AR04 (TEB32 · 32비트 스택 · CPU 영역)**
  - 에뮬레이터가 해야 할 것: 진짜 코드가 읽으니 ① TEB64 `+0x180c` > 0 · TEB32 `+0x30` = PEB32 · **PEB32 바로 뒤 (`+0x490`) 에 `Wow64Info` 자리 (≥ 0x24 바이트, 0 으로)** (wow64.dll 이 `+0x0` `+0x20` `+0x22` 를 쓰고 `+0x4` `+0x8` 을 읽음) — PEB32 칸 크기를 `0x490` 보다 크게; ② **TEB64 `+0x1488` = CPU 영역** (앞 `{플래그 WORD, Machine WORD = 0x14c}`, `+0x4` 에 `CONTEXT32` 가 커널이 짠 첫 값 — `Eip`·`Esp` 는 wow64 가 덮어씀, 끝 `+0x300` 까지 읽고 `+0x2f8` 을 씀, XState 끄면 이 크기); wow64 는 이 칸에 **쓰지 않으므로 커널 쪽이 지어야 함**; ③ 초기 `CONTEXT32.Esp` 아래로 **`0x2cc + 0x10` + 호출틀 12바이트 (`Esp − 0x2dc` 부터)** 가 쓸 수 있게 이미 커밋돼 있어야 함 (`Wow64SetupInitialCall +0x15 ~ +0x4c`) ; ④ 64비트 ntdll 의 `LdrSystemDllInitBlock` `+0x18 ~ +0x48` (32비트 ntdll 의 `LoaderInitRoutine` · `KiUserExceptionDispatcher` · `KiUserApcDispatcher` · `KiUserCallbackDispatcher` · (`+0x38`) · 기준 주소) 를 지어야 함 (wow64 가 읽음).
  - 틀릴 수 있는 것: 지금 엔진이 `Wow64Info` 를 `PEB32` 크기에 안 넣었다면 `NtQuerySystemInformation`/매핑 모자람 — `Wow64Info +0x0` 쓰기에서 터짐. `TlsSlots[10]` (TEB64 `+0x14d0`) 은 wow64 가 **자기가 씀**이라 미리 안 채워도 됨.
- **AR05 (GDT · FS `0x53` · 모드 바꾸기)**
  - 에뮬레이터가 해야 할 것: 명령 지원 목록 — 64비트 모드의 **`JMPF m16:32 [mem]`** (`FF /5`, CS = `0x23`, 앞에 `MOV SS,0x2b`), **`IRETQ`** (compat CS `0x23` · SS `0x2b`, 5개 항목 틀), **호환 모드의 `JMPF ptr16:32` (`EA`)** (CS `0x33`), **호환 모드의 `JMPF m16:32 [esp]`** (CS `0x33`), `FXRSTOR` · `MOV DS/ES/GS/FS/SS, r` (값 `0x2b`/`0x53`), 조건부 **`WRFSBASE`** (KUSD `ProcessorFeatures[22]` 이 켜졌을 때; 꺼 두면 `MOV FS,0x53` 쪽으로 가서 GDT 칸 10 의 기준을 읽음), **`SYSCALL`/`INT 0x2e` 가 wow64cpu.dll 안에서도** 나옴 (터보 썽크), `PUSHF`/`POP`, **32비트 모드 동안 R8~R15 · 각 상위 32비트 보존**. GDT 는 `0x23` (compat 코드 DPL3) · `0x2b` (데이터) · `0x33` (64비트 코드) · `0x53` (FS, 기준 = TEB32) 가 이 코드의 최소 요구. `jmp far` 의 `DIR64` 재배치 꼼수 때문에 **wow64cpu.dll 은 4GB 아래에 올려야** (그 아래서 `0x34 → 0x33`).
  - 틀릴 수 있는 것: 엔진이 compat 모드 진입을 `SS`/`CS` 값만 바꿔 흉내 내면 `JMPF [r14]` 의 메모리 피연산자 (`{Eip, 0x23}` 6바이트) 와 `IRETQ` 틀이 안 맞음.
  - 그리고 `KUSD+0x308` (`SystemCall`): 1 이면 `CpupSyscallStub` 이 `INT 0x2e`, 빠른 길 (`W64SVC`) 이 꺼짐 — 값을 정해야 함 (기존 KUSD 시드 값 확인 필요).
- **AR06 (ntdll 엔진 WoW64 갈래 · 은퇴 자리)**
  - 에뮬레이터가 해야 할 것: ① `LdrpInitializeProcess +0x17c6` 의 `LdrpLoadWow64` 자리는 **진짜 `LdrpLoadWow64` 를 돌려도 됨** — 필요한 것은 (a) 시스템 디렉터리 글 `…\System32\`, (b) 64비트 로더가 wow64.dll · wow64base · wow64win · wow64con (+ wow64cpu 는 나중에) 을 import 연결해서 올릴 수 있을 것, (c) 결과 6칸 (`0x1e70c8 · 0x1e70d0 · 0x1e70e0 · 0x1e70f8 · 0x1e7110 · 0x1e7118`) 이 채워짐; ② **`+0x17fe` 는 돌아오지 않는다** (예외: `Run64IfContextIs64` 참) — 엔진은 `+0x17fc CALL` 이후 `+0x1801 JMP +0x1d1b` 자리를 "돌아옴" 으로 두지 말고 **진짜 코드에 넘기고 끝** (이후 NtContinue 은퇴는 **32비트 ntdll 쪽** — Q253c, 64비트 ntdll 의 `LdrInitializeThunk +0x13 NtContinue` 은 안 탐); ③ `UseWOW64` 일 때 `_LdrpInitialize +0x22d` 가 **`LdrpInitializationComplete` 를 안 부르고** `wow64!Wow64LdrpInitialize +0x144 LdrProcessInitializationComplete()` 가 대신 부르므로, 엔진이 따로 `LdrpProcessInitialized`/`LdrpInitCompleteEvent` 신호를 또 보내지 않게; ④ 이후 스레드: `_LdrpInitialize +0x9a` 에서 같은 전역 호출 (`RCX = R12 = 문맥`).
  - 지어 둘 것 (진짜 코드가 읽음): 64비트 PEB `ProcessParameters.ImagePathName` · `ImageBaseAddress` · `Ldr.InLoadOrderModuleList` (+ `+0x2d8 · +0x2f8 · +0x308`), 64비트 ntdll `LdrSystemDllInitBlock`, `\KnownDlls\kernel32.dll` · `\KnownDlls32\kernel32.dll` (없어도 통과), `\??\<시스템루트>` 파일 열기 + `NtQueryObject(1)` (**없으면 프로세스 죽음**), `NtQuerySystemInformation(0x3e)` (없어도 통과), 레지스트리 `\Registry\Machine\Software\Microsoft\Wow64\x86` (**없어도 통과 → wow64cpu.dll**).
- **AR07 이후 (진짜 wow64 초기화 벽 · 메모리 상한 · 예외)**
  - 벽 (실패하면 `NtTerminateProcess`): `RtlCreateHashTable`; **`\??\<윈도 폴더>` 열기·이름 얻기**; `CpuLoadBinaryTranslator` (LdrLoadDll 성공 + 필수 3개); `Wow64pInitializeCrossProcessMechanism` (이름 없는 구역 `0xf001f` 만들기·사상); 이미지 알림 둘 (`NtQueryVirtualMemory` 클래스 6 = 이미지 정보 가 **음수면** 건너뜀 `+0x62 JS`, 정상이면 `CpupResolveReverseImports`).
  - 메모리 상한: `Wow64pMaxAddress` ← `NtQuerySystemInformation(0x3e)` 의 최대 사용자 주소 (`+0x28`). 이 값이 32비트 프로세스의 상한 — 커널 `Wow64VaSpace4Gb` (sast/135 C 8) 와 맞춰야 함.
  - 예외: 64비트 `KiUserExceptionDispatcher +0x1` 이 `Wow64PrepareForException(예외기록, 문맥)` 을 부름 — 문맥 `SegCs == 0x23` 이면 스택을 맞춰 복사한 뒤 `CpuResetToConsistentState` (→ `BTCpuResetToConsistentState`: `RtlpWow64CtxFromAmd64` 로 64 → 32 문맥 변환) (`Wow64PrepareForException.asm +0x19 ~ +0x5e`). 32비트 구간에서 난 예외는 `BTCpuSimulate$filt$0` 이 `Wow64PassExceptionToGuest` 로 넘김 (RVA `0x201c`). wow64.dll 의 `Wow64SystemServiceEx$filt$0` 도 같은 함수.

## 동적으로 확인할 것 (원문 D 와 합치면)

| # | 무엇 | 멈춤점 (`caller+0x…` = `_calls.tsv` ret_off) | 볼 것 |
|---|---|---|---|
| 1 | 6칸이 채워졌나 | `ntdll!LdrpLoadWow64+0x148` (`LdrProtectMrdata(1)` ret) | `dq ntdll+0x1e70c8 L2 ; dq ntdll+0x1e70d0 L1 ; dq ntdll+0x1e70e0 L1 ; dq ntdll+0x1e70f8 L1 ; dq ntdll+0x1e7110 L2` |
| 2 | **`Wow64LdrpInitialize` 가 돌아오나** | `wow64!Wow64LdrpInitialize+0x122` (`RunCpuSimulation` ret, 걸리면 안 됨), `ntdll!LdrpInitializeProcess+0x17fe` (걸리면 안 됨), `wow64!Wow64LdrpInitialize+0xcb` (`Run64IfContextIs64` ret — `al`) | x86 exe 에서 `al == 0` 인지 |
| 3 | 프로세스 초기화 차례 | `+0x142` (`ProcessInit` ret, `eax`), `+0x14b`, `+0x1cd`, `+0x25d`, `+0x116` | `eax` = 0 인지 |
| 4 | CPU dll 고르기 | `wow64!CpuGetBinaryTranslatorPath+0xb4` (`NtOpenKey` ret, `eax`), `+0x129`, `+0x194` (`NtQueryValueKey`), `wow64!CpuLoadBinaryTranslator+0x97` (`LdrLoadDll` ret) | 실제 키·값, 올라온 경로 (`lm m wow64cpu`) — **4GB 아래인지** |
| 5 | `Wow64Transition` 쓰기 | `wow64!CpupResolveReverseImports+0xb2` (RVA `0xbe5a`) | `rsi` (= 32비트 ntdll 안 `Wow64Transition` 주소), `eax` (= `KiFastSystemCall(2)` 주소) |
| 6 | **빠른 길 쓰나** | `wow64cpu!BTCpuProcessInit+0xd8` (`CpupSystemCallFast=1` 자리), 또는 `dq wow64cpu+0x5598 L1` (바이트) | 값 0/1, `[0x7ffe0308]` |
| 7 | CPU 영역 · 첫 진입 | `wow64cpu+0x1a36` (`IRETQ`, 첫 진입), `wow64cpu+0x198b` (`JMPF`, 빠른) | `dq @$teb+0x1488 L1` (영역), `dc <영역> L0x40`, `r rsp` 에서 `dq rsp L5` (IRETQ 틀 `{Eip, 0x23, EFlags, Esp, 0x2b}`), `r cs, ss, ds, es, fs, gs` 를 **IRETQ 전/후** |
| 8 | 32 → 64 입구 | `wow64cpu+0x1a38` (`CpupReturnFromSimulatedCode`), `wow64cpu+0x1a73` (`call Wow64SystemServiceEx`, ret `0x1a79`) | `eax` (번호), `r11` (인자 포인터), `r cs` (0x33 인지) |
| 9 | 서비스 표 | `wow64!Wow64SystemServiceEx+0x199` (`CALL RAX`, ret +0x19b) 또는 `+0x1ad` | `dq wow64+0x52030 L18` (4 × 0x30), `rax` = `whNt*` 이름 |
| 10 | `PEB32 +0x490` · `TlsSlots[10]` | `wow64!Wow64LdrpInitialize+0xad` | `dq @$teb+0x14d0 L1`, `dd poi(@$teb+0x14d0) L12` |
| 11 | `GS:[0]` (WRFSBASE 길) · KUSD | `wow64cpu+0x19d4` | `dq gs:[0] L1` = TEB32 인지, `db 7ffe0000+0x28a L1`, `dd 7ffe0308 L1` |
| 12 | 레지스트리 · KnownDlls32 | (CPU dll 고르기와 같음) + `wow64!Wow64pInitializeFilePathRedirection+0x148` (`NtOpenDirectoryObject` ret) | `\KnownDlls32` 열렸나 |
| 13 | 터보 썽크 쓰나 | `wow64cpu+0x1f70` (`CpupSyscallStub`), `r rip` | 호출한 쪽이 터보 `Thunk*` 인지 · `syscall` 의 RIP 가 wow64cpu 안 |

## 다음 요청 후보

1. **Q253c (SysWOW64 ntdll, B 절)**: ① 32비트 `LdrInitializeThunk` 는 **`(PCONTEXT, PVOID NtDllBase)`** 로 들어옴 (`[Esp+4] = 문맥 사본`, `[Esp+8] = 32비트 ntdll 기준`, `[Esp]` = 안 채워진 반환 칸; `Wow64SetupInitialCall`), `Eip = 64비트 ntdll LdrSystemDllInitBlock+0x18` 의 값 (`Ntdll32LoaderInitRoutine`) 이 정말 `LdrInitializeThunk` 인지 확인; ② `Wow64Transition` · `fs:[0xc0]` 둘 중 어느 쪽을 스텁이 부르는지, `Nt*` 스텁의 `EAX` (비트 16 이상 터보 번호), 인자 위치; ③ `NtContinue` (32비트) 가 `whNtContinue` → 64비트 `NtContinueEx` 로 가는 길 (엔진의 은퇴 자리); ④ **`NtdllExportInformation[1]` WOW 8칸 이름 8개** = `LdrSystemDllInitBlock +0x18 ~ +0x50`: `[0]` `LoaderInitRoutine`(+0x18) · `[1]` `KiUserExceptionDispatcher`(+0x20) · `[2]` `KiUserApcDispatcher`(+0x28) · `[3]` `KiUserCallbackDispatcher`(+0x30) · `[4]`(+0x38, `ThunkStartupContext64TO32` 가 32비트 `Eip` 와 비교 — 32비트 `RtlUserThreadStart` 로 보임 `[추측]`) · `[5]`(+0x40, wow64 미사용) · `[6]` 기준 주소 (+0x48) · `[7]`(+0x50, 미사용). 이름 확인은 B 4 / 커널 `0xc7ff90` `[1]` 에서.
2. wow64.dll 더: `Wow64pCpuInitializeStartupContext` · `ThunkStartupContext64TO32` 분기 정밀, `whNtContinue`/`whNtTestAlert`, `Wow64KiUserCallbackDispatcher`, `Wow64ApcRoutine`, `Wow64SetupForInstrumentationReturn` (`Wow64Info+8`), 자주 쓰는 `whNt*` 본문 (`NtAllocateVirtualMemory` · `NtMapViewOfSection` · `NtCreateFile` · `NtOpenKey` …) — 에뮬레이터 syscall 표면이 x86 갈래로 넓어질 때 (AR07).
3. wow64cpu **터보 썽크** 본문 정밀 (`Thunk*Arg*`, `QuerySystemTime`, `ReadWriteFile` 등 — `_objdump_text_0x1000.s` 가 Ghidra 에 안 들어가 있어 Ghidra 쪽 함수 정의 보충).
4. wow64cpu 가 4GB 아래에 올라오는 까닭 (로더 `MiSelectImageBase` / `LdrpMapDll` 쪽, sast/135 C 8 과 같이).
5. KUSD `XState` 가 켜진 때 `RtlWow64GetCpuAreaInfo` 크기와 CPU 영역 칸 (`CONTEXT_EX` `0xcc` 의 뜻).

## 알아 둘 것 (스크립트 문제 아님)

- Ghidra 목록에서 `RunSimulatedCode` 안 `WRFSBASE` 가 `WRFSBASE RAX` 로 보이지만 실제 바이트 (`f3 49 0f ae d0`) 는 **`wrfsbase r8`** (`_objdump_text_0x1000.s` `19dd:`).
- Ghidra 전역 주석 `RtlpTpWorkTaskCallbacks+0x10` (표 `0x173830`), `CachedMachine+0x2` (바이트 `0x4c736`), `sdwhnt32+0x30` (`BtFuncs` 이름 표 `0x3a220`) 는 "가장 가까운 앞쪽 이름" 이고 실제로는 별개 칸 (스킬 "전역 주석 주의").
- wow64cpu 의 32비트 쪽 코드 (`WOW64SVC` · `W64SVC`) 와 `RunSimulatedCode` 뒤 큰 틈 (RVA `0x1a38 ~ 0x1f6f`) 은 Ghidra 가 함수로 못 읽음 → `.asm` 이 아니라 `_objdump_text_0x1000.s` · `_ndisasm_wow64svc.txt` 로 읽음 (같은 바이트).

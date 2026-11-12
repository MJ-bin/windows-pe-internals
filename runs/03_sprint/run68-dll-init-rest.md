# 2026-11-06 run68 — kernelbase·kernel32 시작 함수 안 나머지 (Q129, PE_emulator 요청 16 의 나머지): **csrss 연결은 syscall 6번으로 ntdll `Csr*` 전역 9개와 PEB 세 칸을 채우고**, `SysInfo` 는 클래스 0 한 번 — **PE_emulator 의 `+0x18`·`+0x28`·`+0x38` 로는 모자라 `+0x8 = 0x1000`·`+0x20 = 0x10000` 도 필요**, 콘솔은 "부모 콘솔에 붙기" 갈래

> **정정 (run71, sast/66)**: 아래 "`gpServerNlsUserInfo ← BaseStaticServerData + 0x178`" 은 그 칸을 **읽은 값이 아니라 주소**다 (`BaseNlsDllInitialize +0x9 LEA RAX,[R8+0x178]` → `+0x10 MOV [gpServerNlsUserInfo],RAX`). `BaseNlsDllInitialize`·`NlsProcessInitialize` 는 그 영역 안을 읽지 않고, csrss 도 초기화 때 그 영역을 안 채운다 (0). `MakeLocHashNode(0x7f)` 아래 (이 문서 `[공백]`) 가 `GetCurrentNlsCache` 까지 가면 그때 읽는다 — 그 길은 그대로 공백. 자세한 것은 `runs/03_sprint/run71-named-obj-dir.md` "정정".

> **정정 (run83, sast/80)**: 아래 콘솔 "부모 콘솔에 붙기" 갈래의 **syscall 은 3번이 아니라 4번**이다. `ConsoleCommitState +0xfa/+0xff SetTEBLangID` → `+0x12/+0x17 GetConsoleLangId` → **`+0xe1/+0xe8 NtDeviceIoControlFile(params.ConsoleHandle = 새 핸들, 0x500016, API 0x01000008)`** (콘솔 언어 ID, 성공이면 `SetThreadLocale` 이 TEB `CurrentLocale` 을 씀) 가 하나 더 있다 — 이 문서는 `SetTEBLangID` 아래를 안 열었다. 그래서 kernelbase 시작 함수 syscall 합도 15 ~ 16 → **16 ~ 17번** (+ NLS 모름). 아래 syscall 표에 `14′` 줄을 붙였다. 자세한 것은 `runs/03_sprint/run83-console-kernelbase.md` "정정".

## PE_emulator 쪽에서 쓸 것

### 한 줄 결론

1. **`_SYSTEM_BASIC_INFORMATION` 칸이 모자란다.** 지금 nt 대답 (`FillSystemBasicInformation`) 은 `+0x18`·`+0x28`·`+0x38` 만 채운다. kernelbase 는 `+0x8` (`HeapCreate`·`CreateFiberEx`·`GlobalMemoryStatusEx`·`VirtualAlloc` 쪽 등 20여 함수) 과 `+0x20` (`GlobalMemoryStatusEx`) 도 읽는다. **최소 `+0x8 = 0x1000`, `+0x20 = 0x10000` 을 더 채워야** 한다 (둘 다 커널이 상수로 쓰는 칸이라 쉽다). 아래 칸 표.
2. **csrss 연결 (`CsrClientConnectToServer`)** — test.exe 는 "클라이언트" 길. 인자 = `\Sessions\1\Windows` · 서버 번호 1 (BASESRV) · 연결 정보 8B (= `kernelbase!CtrlRoutine` 주소) · 크기 8. 안에서 `CsrpConnectToServer` 가 **syscall 6번** (`NtOpenSection` · `NtCreateSection` · `NtConnectPort` · `NtClose` · `NtMapViewOfSection` · `NtClose`) 을 부르고 **ntdll `Csr*` 전역 9개 + PEB `+0x88`·`+0x98`·`+0x380`** 을 채운다. 값 (핸들 숫자·뷰 주소·csrss PID) 은 csrss 가 주므로 동적 (Q84).
3. **`BaseStaticServerData`** (kernelbase) = `[PEB+0x98]+8` 의 값 − `PEB+0x380` + `PEB+0x88` — csrss 쪽 포인터를 내 뷰 주소로 옮긴 것. kernel32 도 같은 식으로 자기 칸에 한 번 더 (`_BaseDllInitialize +0x189`, `_Kernel32LegacyLib_BaseDllInitialize +0x28 ~ +0x5c`).
4. **콘솔 (`ConsoleInitialize`)** — test.exe (콘솔, `ConsoleHandle = 4`, `ConsoleFlags = 0`) 는 **"부모 콘솔에 붙기"** 갈래: `NtCreateFile(<핸들 4> 기준 "\Connect")` → 새 핸들 → `ConsoleCommitState` 가 `ProcessParameters.ConsoleHandle` 을 `4 → 새 핸들` 로 (run51). syscall 3번.
5. **NLS (`BaseNlsDllInitialize`)** — `gpServerNlsUserInfo ← BaseStaticServerData + 0x178`, 그 뒤 `NlsProcessInitialize`. syscall 은 `NtGetNlsSectionPtr` (종류 `0xe` 1번, `0xb` 1~2번) + `NtInitializeNlsFiles` 1번 — run57 과 같은 섹션을 kernelbase 가 **자기 뷰로 또** 연다. 단 `MakeLocHashNode(0x7f)` 아래는 **못 닫음**.
6. **락·예외 필터·ETW·쿠키** — 모두 syscall 없음 (ETW 만 `NtTraceControl` 2번):
   - `+0x508` 의 락 = `KernelBaseGlobalData+0xb0` (컴퓨터 이름 캐시 락), `DebugInfo = -1` (디버그 정보 **안 만듦**), `SpinCount = 0x20007d0`.
   - 예외 필터 = `RtlpUnhandledExceptionFilter ← RtlEncodePointer(kernelbase!UnhandledExceptionFilter)`, `BasepCurrentTopLevelFilter ← RtlEncodePointer(0)`.
   - ETW 공급자 `Microsoft.Windows.Kernel.Base` (이 프로세스의 5번째 등록 — 처음 한 번만 하는 4개 syscall 은 없음).
   - `_DllMainCRTStartupForGS2` 쿠키는 그 모듈 **자기** `/GS` 쿠키 (`RDTSC`) — run59 의 프로세스 쿠키 (`CookieValue`) 와 **별개**.
7. **test.exe 에서 안 타는 셋** (까닭은 아래 표): `DisableRegCachingForConstrainedImpersonation` 본문 · `RtlSetProtectedPolicy` · `GetRegistryExtensionFlags`.
8. **kernel32 쪽**: `LdrDisableThreadCalloutsForDll` 이 **kernel32 모듈 항목 `Flags` 비트 18 `DontCallForThreads`** 를 켠다 (kernel32 는 TLS 가 없어 `TlsIndex == 0`). `RtlSetThreadPoolStartFunc` 는 **0 을 돌려주는 빈 함수**다.

### `_SYSTEM_BASIC_INFORMATION` 칸 표 — PE_emulator 의 nt 대답에 줄 것

- 커널 `nt!ExpGetSystemBasicInformation` (rva `0x457484`, 264B) 이 사용자 버퍼에 바로 쓴다. 길이가 **정확히 `0x40`** 이 아니면 `STATUS_INFO_LENGTH_MISMATCH` (`0xC0000004`, `ExpQuerySystemInformation +0x72b CMP R14D,ECX` · `+0x72e JNZ`). 성공이면 반환 길이 `0x40` (`+0x740`).
- 칸 이름은 PDB 에 이 타입이 없어 공개 문서 이름 `[추측-이름]`. 오프셋·쓰는 줄·값 출처는 `.asm` `[정적]`.

| 오프셋 | 크기 | 뜻 `[추측-이름]` | 쓰는 줄 | 값 | kernelbase·kernel32 가 읽나 | PE_emulator 지금 |
|---|---|---|---|---|---|---|
| `+0x00` | 4 | Reserved | `+0x32` | **상수 0** | 아니오 | 0 (맞음) |
| `+0x04` | 4 | TimerResolution | `+0x35`·`+0x3b` | 커널 전역 `KeMaximumIncrement` — 부팅 때 정해짐 (동적). `[추측]` 156250 (`0x2625a`) | 아니오 | 0 |
| `+0x08` | 4 | PageSize | `+0x3f` | **상수 `0x1000`** | **예** (kernelbase 20여 곳 · kernel32 `IsBadReadPtr`·`IsBadWritePtr`·`GlobalMemoryStatus`) | **0 — 고쳐야 함** |
| `+0x0c` | 4 | NumberOfPhysicalPages | `+0xb4` | 파티션 `Vp.NumberOfPhysicalPages` (32비트 넘으면 `0xffffffff`) — 동적. `[추측]` `0x1ffe86` (KUSD 같은 이름 칸 `[관찰]` run44) | 아니오 | 0 |
| `+0x10` | 4 | LowestPhysicalPageNumber | `+0xd8` | 파티션 `Vp.LowestPhysicalPage` — 동적 | 아니오 | 0 |
| `+0x14` | 4 | HighestPhysicalPageNumber | `+0xfa` | 파티션 `Vp.HighestPhysicalPage` — 동적 | 아니오 | 0 |
| `+0x18` | 4 | AllocationGranularity | `+0x4c` | **상수 `0x10000`** | 예 (`VirtualAlloc`·`VirtualAllocEx(Numa)`·`CreateFiberEx` 등) | 채움 |
| `+0x1c` | 4 | (정렬 틈) | — | 안 씀 (호출한 쪽 값 그대로 — 전역이라 0) | — | 0 |
| `+0x20` | 8 | MinimumUserModeAddress | `+0x50` | **상수 `0x10000`** | **예** (`GlobalMemoryStatusEx +0x155`) | **0 — 고쳐야 함** |
| `+0x28` | 8 | MaximumUserModeAddress | `+0x5e` | **상수 `0x7ffffffeffff`** | 예 (`GlobalMemoryStatusEx +0x149`) | 채움 |
| `+0x30` | 8 | ActiveProcessorsAffinityMask | `+0x7d` | `KeActiveProcessors` 비트맵의 이 스레드 그룹 칸 — 동적. CPU 2개라 `3` 일 것 `[추측]` | 아니오 | 0 |
| `+0x38` | 1 | NumberOfProcessors | `+0x8a` | 위 마스크의 `POPCNT` — 동적. 2 일 것 `[추측]` | 아니오 | 채움 |
| `+0x39 ~ +0x3f` | 7 | (안 씀) | — | — | — | 0 |

- **읽는 쪽 (kernelbase `SysInfo`, `_refs` 29줄)**: `+0x8` — `WriteProcessMemory`·`DiscardVirtualMemory`·`GlobalMemoryStatusEx`·`QueryAppMemoryInformation`·**`HeapCreate +0x6`**·`CreateFiberEx`·`BaseInitializeFiberContext`·`SetThreadStackGuarantee`·`GetSystemFileCacheSize`·`OfferVirtualMemoryInternal`·`ReclaimVirtualMemory` / `+0x18` — `DiscardVirtualMemory`·**`VirtualAlloc +0x13`**·`VirtualAllocExNuma`·`VirtualAllocEx`·`CreateFiberEx`·`OfferVirtualMemoryInternal` / `+0x20`·`+0x28` — `GlobalMemoryStatusEx`. kernel32 `SysInfo` 는 `+0x8` 만.
- `+0xc`·`+0x10`·`+0x14`·`+0x30` 은 두 DLL 이 안 읽지만, 진짜와 같게 채워 두는 게 정직하다 (결과서 의견). `+0x38` 도 두 DLL 은 안 읽는다.
- `SysInfo` 를 채우는 곳은 두 벌, 각각 한 줄뿐:
  - kernelbase `SysInfo` (rva `0x3aee60`, `0x40`B) ← `_KernelBaseBaseDllInitialize +0x23a/+0x241 NtQuerySystemInformation(0, &SysInfo, 0x40, NULL)` (`+0x22d LEA RDX` · `+0x234 XOR ECX,ECX` · `+0x236 LEA R8D,[R9+0x40]`). 이 주소를 넘기는 줄은 `+0x22d` 하나 (`_refs`).
  - kernel32 `SysInfo` (rva `0xc2260`, `0x40`B) ← `_Kernel32LegacyLib_BaseDllInitialize +0x9c/+0xa3` (`+0x8f LEA RDX`).
  - 둘 다 실패하면 DllMain FALSE (`+0x248 JS` · `+0xaa JS`).

### 작은 표 (결과서 표를 줄임, rva 는 모듈 기준)

| 함수 | 인자 | 쓰는 칸 | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|
| ntdll `CsrClientConnectToServer` (`0xdb7a0`) | `\Sessions\1\Windows`, 1, &{`CtrlRoutine`}, 8, &byte | `CsrInitOnceDone`·`CsrHeap`·`CsrClientProcess` (← 1) + 아래 `CsrpConnectToServer` 몫, `*CalledFromServer ← 0` | `RtlImageNtHeaderEx`, `CsrpConnectToServer` | (아래) | 0 / 음수 |
| ntdll `CsrpConnectToServer` (`0xdbfe4`) | 위 넷 + &응답 상태 | `CsrPortName` (Length·MaximumLength·Buffer — 힙 `0x42`B) · `CsrPortHandle` (syscall 이 씀) · PEB `+0x380`·`+0x88`·`+0x98` · `CsrProcessId` · `CsrPortMemoryRemoteDelta` · `CsrPortHeap` · `CsrPortBaseTag` (← 0) | `RtlAllocateHeap`·`memcpy`·`memset`·`RtlpCreateHeap` (공용 부품) | **6** | 0 |
| ntdll `RtlSetUnhandledExceptionFilter` (`0xe95f0`, 21B) | 필터 | `RtlpUnhandledExceptionFilter` ← 인코딩 값 (`+0x9`) | `RtlEncodePointer` | 없음 | (무시) |
| kernelbase `BaseNlsDllInitialize` (`0xea9b0`) | reason, `BaseStaticServerData` | `gpServerNlsUserInfo` ← `+0x178` (`+0x10`), 정렬 해시 셋 0 | `NlsProcessInitialize` | 2~3 (+ 모름) | AL |
| ntdll `RtlInitializeCriticalSection` (`0xd65c0`) | cs | `+0x8` −1 · `+0xc` 0 · `+0x10` 0 · `+0x18` 0 · `+0x20` `0x20007d0` (CPU ≤ 1 이면 0) · `+0x0` −1 | (디버그 정보는 켜졌을 때만) | 없음 | 0 |
| kernelbase `ConsoleInitialize` (`0xbeb60`) | 없음 | 콘솔 전역들 · 매개변수 `ConsoleHandle` (`ProcessGroupId` 는 0 일 때만 — test.exe 는 `0x2f0` 이라 안 씀) | `ConsoleCreateConnectionObject`·`ConsoleCommitState` | 3 | AL 1 |
| kernelbase `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` (`0xf70ac`) | 없음 | 공급자 `0x3a9020` (`+0x20` RegHandle, `+0x28/+0x30` ← 0) | `EtwEventRegister`·`EtwEventSetInformation` | 2 (`NtTraceControl` `0xf`·`0x1e`) | 등록 상태 |
| ntdll `LdrDisableThreadCalloutsForDll` (`0xed1f0`, 81B) | hModule | kernel32 항목 `+0x6a` `OR 4` (= `Flags` 비트 18) | `LdrpFindLoadedDllByHandle`·`LdrpDereferenceModule` | 없음 | 상태 |
| kernel32 `_Kernel32LegacyLib_BaseDllInitialize` (`0x2ca24`) | reason | `KernelBaseGlobalData+0x28` ← 0 · kernel32 `BaseStaticServerData`·`BaseDllIniFileMapping`·`SysInfo` | `KernelBaseGetGlobalData`·메모리 관리 사본 함수 | 1 | AL |
| kernelbase `_DllMainCRTStartupForGS2` (`0x1290e8`, 24B) | reason | (reason 1) `__security_cookie`·`__security_cookie_complement` | `__security_init_cookie` | 없음 | 1 |

### test.exe 에서 안 타는 것과 까닭

| 함수 (부르는 자리 call/ret) | 안 타는 까닭 | 근거 줄 | 탔다면 |
|---|---|---|---|
| kernelbase `DisableRegCachingForConstrainedImpersonation` **본문** (`_KernelBaseBaseDllInitialize +0x184/+0x189` — 함수 자체는 불림) | 머리가 `RtlIsMultiSessionSku()` 를 부르고 **0 일 때만** 본문으로 점프. 이 VM 은 멀티세션 SKU — KUSD `SharedDataFlags` (`0x7ffe02f0`) = `0x10e`, 비트 8 켜짐 `[관찰]` run44 (`04_baggage-read_P1a.txt:2938`) → AL = 1 → 바로 반환 | `DisableRegCaching… +0x32/+0x39` · `+0x3e TEST AL,AL` · `+0x40 JZ` (본문) / 떨어지면 `+0x46` 반환. `RtlIsMultiSessionSku +0x26 MOV EAX,[0x7ffe02f0]` · `+0x2d SHR EAX,8` · `+0x30 AND AL,1` | 토큰을 본다 (`NtQueryInformationToken(-4, 0x1d)` + `RtlCheckTokenMembershipEx` 안 `NtAccessCheck`), 결과 1바이트를 `g_DisableCachingWhileImpersonated` 에 (`+0x150e1e`). **이름과 달리 레지스트리는 안 읽는다** |
| ntdll `RtlSetProtectedPolicy` (kernel32 `_BaseDllInitialize +0xe9/+0xf0`) | exe 헤더 `MajorSubsystemVersion` 이 **6 이상**이면 건너뜀. test.exe = 6 (결과서 `objdump -p`) | `+0xc7 MOVZX EAX,[NT+0x5c]` · `+0xcf SUB AX,SI` · `+0xd2 CMP AX,R9W` · `+0xd6 JBE` (→ `+0x28d`) · `+0x28d CMP word [NT+0x48],6` · `+0x292 JNC` (→ `+0xfb`, 건너뜀) | 정책 표 `RtlpProtectedPolicies` 에 GUID `BasepAllowResourceConversion` = 1 을 넣음 (mrdata 힙·`qsort`) |
| kernelbase `GetRegistryExtensionFlags` (kernel32 `_BaseDllInitialize +0x380/+0x387`) | 앞 `InitializeRegTermsrvFpns` 가 **곧바로 0** — test.exe 가 "터미널 서버 인식" exe (`DllCharacteristics = 0x8160`, 비트 15 켜짐) 라서. 0 이면 `+0x36b JZ` 로 건너뜀 | `_BaseDllInitialize +0x1f9/+0x200 RtlGetSuiteMask` · `+0x205 TEST AL,0x10` (`SuiteMask 0x110` `[관찰]` `_P1a.txt:2922`) · `+0x207 JNZ` (→ `+0x350`) · `+0x364/+0x369 InitializeRegTermsrvFpns` · `+0x369 TEST AL,AL` · `+0x36b JZ` (→ `+0x20d`) | 레지스트리 `NtOpenKey`·`NtQueryValueKey("RegistryExtensionFlags")`·`NtClose` 3번 → kernelbase `g_RegKrnGlobalState` |

- (덤) **`CsrpClientConnectToServer` 도 정상일 땐 안 탄다**: csrss 응답 상태 (`[RSP+0x70]`) 가 음수일 때만인데, basesrv `BaseClientConnectRoutine` 은 크기 8 이면 0 을 돌려준다 (`+0x13 XOR EAX,EAX`). csrss 쪽 상수에 기대는 결론이라 동적 확인 (Q84).
- (덤) `ConsoleAllocate` (새 콘솔 — conhost 띄우기) 도 안 탄다: `ConsoleHandle = 4` 는 0·−1·−2·−3 이 아니다 (`ConsoleInitialize +0x170 ~ +0x185`).
- (덤) 서버 길 (`CsrClientConnectToServer +0x137` 이하 — `CsrServerApiRoutine` 채우기·`RtlCreateTagHeap`) 은 csrss 자신만. test.exe 는 `Subsystem = 3` 이라 `CsrClientProcess = 1` (`+0x8f`) → 클라이언트 길.

오프셋 읽는 법 (run60 ~ run67 과 같음):

- `call +0x..` = CALL 명령 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 값.
- `DisableRegCaching…` 의 `+0x150xxx` 는 Ghidra 가 함수 본체를 두 덩이로 잡아서 생긴 큰 오프셋이다 (같은 함수의 떨어진 범위).

## 환경

- 26100.9457, 정적만 (VM 안 씀)
- 정적: KernelBase pdb GUID `72CCE506944DE0267C172AD64287F9BD1`, ntdll `C3093720177DA6851519DCFFE8DF53FE1`, kernel32 `AE9E9D8F7AC1467A645320FDC23235D01`, ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` (넷 다 기준 GUID 와 같음). basesrv 는 run34 의 PDB (`4B0951C4FAD4CCBDDFDAA284D1ED2C2E1`) 로 함수 하나 (`BaseClientConnectRoutine`) 를 더 뽑음. 새로 analyze 안 함.
- 값 대조: run44 사진의 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`·`_P1b.txt`·`_P2.txt`) 를 다시 읽었다 (결과서 + flow-writer). 새 동적 실행은 없다.
  - **kernelbase·kernel32 `.data` 는 여전히 사진에 없다** (run44 는 ntdll `.data` 만 읽음). `SysInfo` 두 벌·`BaseStaticServerData`·콘솔 전역 등의 값은 못 봤다.

## 목적

- PE_emulator 는 kernelbase·kernel32 시작 함수를 진짜로 돌리지 않고 흉내 낸다. run67 이 급한 셋을 닫았고, 이번은 요청 16 의 나머지 (원문 2·3·6 정적·8·9) 다.
- 알고 싶은 것 (요청서 `_workspace/sast/63_dll-init-rest.md` 1 ~ 12): 함수마다 **인자 · 쓰는 칸 · 값 출처 · 하위 호출 · syscall · 반환**, test.exe 기본 길에서 타는 갈래, `_SYSTEM_BASIC_INFORMATION` 칸 모양과 두 DLL 이 실제로 읽는 칸.

## 방법

- 정적: `_workspace/sast/63_dll-init-rest.md` — kernelbase `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation`·`DisableRegCachingForConstrainedImpersonation`·`BaseNlsDllInitialize`·`ConsoleInitialize`·`KernelBaseGetGlobalData`·`GetRegistryExtensionFlags`·`_DllMainCRTStartupForGS2`, ntdll `CsrClientConnectToServer`·`RtlSetUnhandledExceptionFilter`·`RtlInitializeCriticalSection`·`RtlInitializeCriticalSectionAndSpinCount`·`RtlImageNtHeader`·`CsrIdentifyAlertableThread` (= 0x10bc80)·`RtlSetProtectedPolicy`·`LdrDisableThreadCalloutsForDll`, kernel32 `InitializeRegTermsrvFpns`, ntoskrnl `ExpGetSystemBasicInformation` (모두 depth 1).
  - 결과서가 범위를 넓힌 것 (이유 있음): NLS 줄기 (`AllocTables`·`SetupMainNlsFiles`·`SetupAnsiOemCodeHashNodes`·`MakeLocHashNode` + 호출 목록만 본 8개, ntdll `RtlGetLocaleFileMappingAddress`), 콘솔 `ConsoleInitializeServerData`, 갈래 판정용 ntdll `RtlIsMultiSessionSku`·`RtlCheckTokenMembershipEx`·`RtlDeriveCapabilitySidsFromName`, csrss 쪽 basesrv `BaseClientConnectRoutine`, `__security_init_cookie_ex`.
  - 새로 뽑은 수: kernelbase 68 · ntdll 38 · kernel32 5 · ntoskrnl 1 · basesrv 1. 새 `_refs`: kernelbase `SysInfo`·`0x3a8fa8`·`g_DisableCachingWhileImpersonated`·`g_Win64Registry`, kernel32 `SysInfo`, ntdll `RtlpForceCSDebugInfoCreation`·`RtlCriticalSectionList`.
- 결과서: `_workspace/sast/63_dll-init-rest_result.md` (**분석성공**). 결과서는 `+0x..` 약 480줄을 `.asm` 과 다시 대조했다 (어긋난 5개는 쓰기 전에 고침).
- 오케스트레이터가 `.asm` 으로 한 번 더 본 줄: `_KernelBaseBaseDllInitialize +0x22d LEA RDX,SysInfo` · `+0x234 XOR ECX,ECX` · `+0x236 LEA R8D,[R9+0x40]` · `+0x23a/+0x241 NtQuerySystemInformation` · `+0x2d2/+0x2d9 CsrClientConnectToServer` · `+0x501/+0x508 RtlInitializeCriticalSection`, ntdll rva `0x10bc80` `+0x0 XOR EAX,EAX` · `+0x2 RET`.
- flow-writer 가 `.asm` 으로 한 번 더 본 줄:
  - `_KernelBaseBaseDllInitialize` `+0x160/+0x165` · `+0x16a` · `+0x171` · `+0x17a` · `+0x184/+0x189` · `+0x1e7/+0x1ee` · `+0x225/+0x22a` · `+0x246` · `+0x248` · `+0x253/+0x25a` · `+0x271/+0x278` · `+0x286`·`+0x28b` · `+0x292` · `+0x2a7` · `+0x2b4` · `+0x2bf` · `+0x2c4` · `+0x2c9` · `+0x2ce` · `+0x2e0` · `+0x2e6`·`+0x2eb` · `+0x2fb` · `+0x30b` · `+0x30f` · `+0x31f` · `+0x326` · `+0x32b` · `+0x332` · `+0x337` · `+0x342` · `+0x4a4` · `+0x4ab/+0x4b2` · `+0x4b7` · `+0x4b9/+0x4c0` · `+0x4c5` · `+0x4ce` · `+0x4d5`·`+0x4da` · `+0x4e1/+0x4e6` · `+0x4e8` · `+0x4ee`·`+0x4f3`·`+0x4fa` · `+0x50f` · `+0x515`·`+0x51a` · `+0x521/+0x526` · `+0x528` · `+0x52e`.
  - kernelbase `KernelBaseBaseDllInitialize +0x17` · `+0x1a` · `+0x28/+0x2d` · `+0x44` · `+0x49` · `+0x4e` · `+0x55/+0x5c` · `+0x68` · `+0x6f/+0x76`, `_DllMainCRTStartupForGS2` 전체 (24B), `KernelBaseGetGlobalData` 전체 (8B), `TraceLogging… +0x15` · `+0x2e` · `+0x55/+0x5c` · `+0x65` · `+0x7c/+0x83`, `DisableRegCaching… +0x32/+0x39` · `+0x3e` · `+0x40` · `+0x46` · `+0x150e1e`, `BaseNlsDllInitialize +0x4` · `+0x9` · `+0x10` · `+0x17/+0x1c` · `+0x62`, `ConsoleInitialize +0x42/+0x49` · `+0x63/+0x6a` · `+0x8f/+0x94` · `+0x94/+0x99` · `+0x99/+0x9e` · `+0x9e` · `+0xb8/+0xbd` · `+0xbf` · `+0x19a/+0x19f` · `+0x1ca/+0x1cf`, `ConsoleCreateConnectionObject +0x1c1/+0x1c8`.
  - ntdll `CsrClientConnectToServer` 의 전역 줄 전부 (`+0x3d` · `+0x50` · `+0x77/+0x7c` · `+0x8f` · `+0xa4` · `+0xb5/+0xba` · `+0xe7` · `+0xf2` · `+0x113/+0x118` · `+0x125` · `+0x150/+0x155` · `+0x162` · `+0x182/+0x187` · `+0x1ac` · `+0x1b3/+0x1b8` · `+0x1b8`), `CsrpConnectToServer` 의 전역·PEB·syscall 줄 전부 (`+0x111` · `+0x119/+0x11e` · `+0x11e` · `+0x194` · `+0x1b6/+0x1bb` · `+0x212` · `+0x23d/+0x242` · `+0x2d3` · `+0x2df/+0x2e4` · `+0x2eb/+0x2f0` · `+0x338/+0x33d` · `+0x344/+0x349` · `+0x36d` · `+0x379` · `+0x38d` · `+0x39c` · `+0x3ab` · `+0x3f3/+0x3f8` · `+0x3f8` · `+0x404` · `+0x43d`), `RtlSetUnhandledExceptionFilter` 전체 (21B), `LdrDisableThreadCalloutsForDll` 전체 (81B), `RtlInitializeCriticalSection +0x20` · `+0x27` · `+0x2d` · `+0x31` · `+0x35` · `+0x43` · `+0x4a` · `+0x4d` · `+0x51` · `+0x58` · `+0x5f`, `RtlGetLocaleFileMappingAddress` 전체, `RtlIsMultiSessionSku +0x26` · `+0x2d` · `+0x30`, `LdrpInitializeProcess +0x97e ~ +0x9b5` · `+0x19f4 ~ +0x1a10` (`RtlpForceCSDebugInfoCreation` 쓰기 셋, `_refs` 와 같음).
  - kernel32 `entry +0x14` · `+0x16` · `+0x1d/+0x22` · `+0x31/+0x36`, `_BaseDllInitialize +0x6c/+0x73` · `+0x92/+0x99` · `+0xc7` · `+0xcf` · `+0xd2` · `+0xd6` · `+0xe9/+0xf0` · `+0xfb` · `+0x189` · `+0x194` · `+0x19e` · `+0x1a4` · `+0x1f9/+0x200` · `+0x205` · `+0x207` · `+0x20d` · `+0x210/+0x217` · `+0x21f/+0x224` · `+0x261` · `+0x268` · `+0x26e` · `+0x275/+0x27c` · `+0x288` · `+0x28d` · `+0x292` · `+0x298` · `+0x364/+0x369` · `+0x369` · `+0x36b` · `+0x380/+0x387`, `_Kernel32LegacyLib_BaseDllInitialize +0x19/+0x20` · `+0x25` · `+0x8f` · `+0x9c/+0xa3` · `+0xaa` · `+0xba` · `+0xbd` · `+0xed/+0xf2`.
  - ntoskrnl `ExpGetSystemBasicInformation` 칸 쓰기 줄 11개 (`+0x32` ~ `+0xfa`) · `+0x103`, `ExpQuerySystemInformation +0x217` · `+0x72b` · `+0x72e` · `+0x737/+0x73c` · `+0x740`.
- 로그: 없음 (정적만). 값 대조만 run44 덤프 읽기 로그.

## 결과

### 정적 — 호출 지점 (`_calls.tsv`, 길 순서)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `KERNELBASE!KernelBaseBaseDllInitialize` | `+0x55` | `+0x5c` | `ntdll!RtlInitializeCriticalSectionAndSpinCount(&_CrtLock_Exit, 0xfa0)` | attach (`+0x17 CMP EDX,1` · `+0x1a JZ` → `+0x49`) | [정적] |
| `KERNELBASE!KernelBaseBaseDllInitialize` | `+0x28` | `+0x2d` | `KERNELBASE!_DllMainCRTStartupForGS2` | 모든 reason (안에서 reason 1 만 일함) | [정적] |
| `KERNELBASE!KernelBaseBaseDllInitialize` | `+0x44` (`JMP`) | — | `KERNELBASE!_KernelBaseBaseDllInitialize` | 꼬리 점프 | [정적] |
| `KERNELBASE!_DllMainCRTStartupForGS2` | `+0x9` | `+0xe` | `KERNELBASE!__security_init_cookie` | reason 1 (`+0x4` · `+0x7 JNZ`) | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x1e7` | `+0x1ee` | `ntdll!RtlGetCurrentServiceSessionId` | 이유 1 갈래 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x225` | `+0x22a` | `KERNELBASE!StringCbPrintfW` (`%ws\%ld%ws` → `\Sessions\1\Windows`) | 서비스 세션이 아닐 때 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x23a` | `+0x241` | `ntdll!NtQuerySystemInformation(0, &SysInfo, 0x40, NULL)` | 늘 | [정적] (오케스트레이터 확인) |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x253` · `+0x271` | `+0x25a` · `+0x278` | `ntdll!RtlRandomEx` ×2 (본문 run61) | 늘 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x2d2` | `+0x2d9` | `ntdll!CsrClientConnectToServer` | `[PEB+0x20].Flags` 양수 (secure process 아님 — `+0x2a7`) | [정적] (오케스트레이터 확인) |
| `ntdll!CsrClientConnectToServer` | `+0x77` | `+0x7c` | `ntdll!RtlImageNtHeaderEx(3, ImageBase, 0, &out)` | 늘 | [정적] |
| `ntdll!CsrClientConnectToServer` | `+0x113` | `+0x118` | `ntdll!CsrpConnectToServer` | 연결 정보 있음 · `CsrPortHandle == 0` (`+0xa4` · `+0xab JZ` → `+0xfe`) | [정적] |
| `ntdll!CsrpConnectToServer` | `+0x119` | `+0x11e` | `ntdll!RtlAllocateHeap(CsrHeap, NtdllBaseTag, 디렉터리+0x1c)` | 늘 | [정적] |
| `ntdll!CsrpConnectToServer` | `+0x1b6` | `+0x1bb` | `ntdll!NtOpenSection(…, 4, \Sessions\1\Windows\SharedSection)` | 늘 | [정적] |
| `ntdll!CsrpConnectToServer` | `+0x23d` | `+0x242` | `ntdll!NtCreateSection(…, 0xf001f, NULL, 0x10000, 4, SEC_COMMIT)` | 늘 | [정적] |
| `ntdll!CsrpConnectToServer` | `+0x2df` | `+0x2e4` | `ntdll!NtConnectPort(&CsrPortHandle, \Sessions\1\Windows\ApiPort, …)` | 늘 | [정적] |
| `ntdll!CsrpConnectToServer` | `+0x2eb` · `+0x344` | `+0x2f0` · `+0x349` | `ntdll!NtClose` ×2 (만든 섹션 · `SharedSection`) | 성공 길 | [정적] |
| `ntdll!CsrpConnectToServer` | `+0x338` | `+0x33d` | `ntdll!NtMapViewOfSection(SharedSection, -1, …, PAGE_READONLY)` | 성공 길 | [정적] |
| `ntdll!CsrpConnectToServer` | `+0x3f3` | `+0x3f8` | `ntdll!RtlpCreateHeap(0x8000, 포트 뷰, 0x10000, …)` — 포트 메모리 안 개인 힙 | 성공 길 | [정적] |
| `ntdll!CsrClientConnectToServer` | `+0xb5` | `+0xba` | `ntdll!CsrpClientConnectToServer` | csrss 응답 상태 < 0 일 때만 — **정상은 안 탐** | [정적] (csrss 쪽 상수에 기댐) |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x4ab` | `+0x4b2` | `ntdll!RtlSetUnhandledExceptionFilter(kernelbase!UnhandledExceptionFilter)` | 늘 | [정적] |
| `ntdll!RtlSetUnhandledExceptionFilter` | `+0x4` | `+0x9` | `ntdll!RtlEncodePointer` (run59) | 늘 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x4b9` | `+0x4c0` | `ntdll!RtlEncodePointer(0)` → `+0x4ce BasepCurrentTopLevelFilter` | 늘 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x4e1` | `+0x4e6` | `KERNELBASE!BaseNlsDllInitialize(…, 1, BaseStaticServerData)` | 늘 | [정적] |
| `KERNELBASE!BaseNlsDllInitialize` | `+0x17` | `+0x1c` | `KERNELBASE!NlsProcessInitialize` | attach | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x501` | `+0x508` | `ntdll!RtlInitializeCriticalSection(&KernelBaseGlobalData+0xb0)` | 늘 | [정적] (오케스트레이터 확인) |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x521` | `+0x526` | `KERNELBASE!ConsoleInitialize` | 늘 | [정적] |
| `KERNELBASE!ConsoleInitialize` | `+0x19a` | `+0x19f` | `KERNELBASE!ConsoleCreateConnectionObject` → `+0x1c1/+0x1c8 NtCreateFile` | `ConsoleHandle` 가 실제 핸들 (0·−1·−2·−3 아님) | [정적] |
| `KERNELBASE!ConsoleInitialize` | `+0x1ca` | `+0x1cf` | `KERNELBASE!ConsoleCommitState` (run51) | 위 성공 뒤 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x160` | `+0x165` | `KERNELBASE!TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` | `ConsoleInitialize` 성공 뒤 `+0x52e JMP` 로 옴 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x184` | `+0x189` | `KERNELBASE!DisableRegCachingForConstrainedImpersonation` | `g_Initialized == 0` (`+0x171`) — 함수는 불리지만 **본문은 안 탐** | [정적] |
| `KERNEL32!BaseDllInitialize` (Ghidra `entry`) | `+0x31` | `+0x36` | `KERNEL32!_DllMainCRTStartupForGS2` | reason 1 (`+0x14` · `+0x16 JZ`) | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x6c` | `+0x73` | `ntdll!RtlImageNtHeader(PEB.ImageBaseAddress)` | attach. NULL 이면 DllMain FALSE | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x92` | `+0x99` | 0 을 돌려주는 빈 함수 (ntdll rva `0x10bc80`, kernel32 import 이름 `RtlSetThreadPoolStartFunc`) | attach | [정적] (오케스트레이터 확인) |
| `KERNEL32!_BaseDllInitialize` | `+0xe9` | `+0xf0` | `ntdll!RtlSetProtectedPolicy` | `MajorSubsystemVersion < 6` 등 — **test.exe 안 탐** | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x1f9` | `+0x200` | `ntdll!RtlGetSuiteMask` | attach | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x210` | `+0x217` | `ntdll!LdrDisableThreadCalloutsForDll(kernel32 DllBase)` | attach | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x21f` | `+0x224` | `KERNEL32!_Kernel32LegacyLib_BaseDllInitialize` | 모든 reason | [정적] |
| `KERNEL32!_Kernel32LegacyLib_BaseDllInitialize` | `+0x9c` | `+0xa3` | `ntdll!NtQuerySystemInformation(0, &kernel32 SysInfo, 0x40, NULL)` | attach | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x275` | `+0x27c` | `ntdll!RtlInitUnicodeString(&PEB.CSDVersion, BaseCSDVersion)` | `PEB.CSDVersion.Buffer == 0` (`+0x261`) | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x364` | `+0x369` | `KERNEL32!InitializeRegTermsrvFpns` → 곧바로 0 | `SuiteMask & 0x10` (`+0x207 JNZ`) | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x380` | `+0x387` | `KERNELBASE!GetRegistryExtensionFlags` | 위가 0 아닐 때 — **test.exe 안 탐** | [정적] |
| `nt!ExpQuerySystemInformation` | `+0x737` | `+0x73c` | `nt!ExpGetSystemBasicInformation` | 클래스 0 (또는 `0x72`) · 길이 `0x40` | [정적] |

### `_KernelBaseBaseDllInitialize` attach 순서 (test.exe, 한눈에)

`BasepInitializationStage` (kernelbase `0x3aeea0`, word) 가 진행 표시다. 아래 "stage" 칸은 **그 단계가 끝난 뒤 쓰는 값**이고 옆에 쓰는 줄을 붙였다 (flow-writer `.asm` 확인).

| 순서 | 줄 (call / ret) | 일 | 그 뒤 stage (쓰는 줄) |
|---|---|---|---|
| 래퍼 | `KernelBaseBaseDllInitialize +0x55/+0x5c`, `+0x28/+0x2d` | `_CrtLock_Exit` 락 (스핀 `0xfa0`) → 쿠키 → `+0x44` 꼬리 점프 | |
| 1 | `+0x1ad/+0x1b2` | 메모리 관리 (run67) | |
| 2 | `+0x1cf/+0x1d6` | `RtlCreateTagHeap` → 0 (run67) | |
| 3 | `+0x1e7/+0x1ee`, `+0x225/+0x22a` | 객체 디렉터리 `\Sessions\1\Windows` 만들기 | |
| 4 | `+0x23a/+0x241` | `NtQuerySystemInformation(0)` → `SysInfo` | |
| 5 | `+0x253/+0x25a`, `+0x271/+0x278` | `RtlRandomEx` ×2 → `BasepFiberCookie` (`0x3aee48`) | `0x64` (`+0x28b`) |
| 6 | `+0x2d2/+0x2d9` | csrss 연결 | `0xc8` (`+0x2eb`) |
| 7 | `+0x2fb ~ +0x469` | `BaseStaticServerData` · `KernelBaseGlobalData+0x4 ~ +0x27` · `BaseRCNumber` | |
| 8 | `+0x490/+0x497` | 명령줄 ANSI 사본 (run67) | |
| 9 | `+0x4ab/+0x4b2`, `+0x4b9/+0x4c0` | 예외 필터 · `BasepCurrentTopLevelFilter` | `0x190` (`+0x4da`) |
| 10 | `+0x4e1/+0x4e6` | `BaseNlsDllInitialize` | `0x1f4` (`+0x4fa`) |
| 11 | `+0x501/+0x508` | 락 `KernelBaseGlobalData+0xb0` | `0x258` (`+0x51a`) |
| 12 | `+0x521/+0x526` | `ConsoleInitialize` → 성공이면 `+0x52e JMP` (→ `+0x160`) | |
| 13 | `+0x160/+0x165` | ETW 공급자 등록 | `0x2bc` (`+0x16a`) |
| 14 | `+0x17a`, `+0x184/+0x189`, `+0x189` | `g_Win64Registry ← 1` → `DisableRegCaching…` (본문 안 탐) → `g_Initialized` +1 (`INC.LOCK`) | |
| 반환 | | TRUE | |

- 실패 길: 4·6·10·11 이 음수/0 이면 `+0x5ab` 쪽에서 AL = 0, 12 가 0 이면 `+0x528 JZ` (→ `+0xf6`) — 모두 DllMain FALSE.

### 호출 관계

```
KERNELBASE!KernelBaseDllInitialize (entry, rva 0x5060)
 └─ +0xd   KernelBaseBaseDllInitialize (0x51d0)
            ├─ (1) +0x5c   RtlInitializeCriticalSectionAndSpinCount(&_CrtLock_Exit, 0xfa0)     attach 만 (+0x1a JZ → +0x49)
            ├─ (2) +0x2d   _DllMainCRTStartupForGS2 → +0xe __security_init_cookie        모듈 자기 /GS 쿠키 (RDTSC)
            └─ +0x44 꼬리 JMP ⇒ _KernelBaseBaseDllInitialize (0x4e6c0)      이유 1 갈래 (라벨 ret):
                 ├─ (1) +0x1b2  KernelBaseDllInitializeMemoryManager                 (run67)
                 ├─ (2) +0x241  ntdll!NtQuerySystemInformation(0, &SysInfo, 0x40)    ── SYSCALL ── nt!ExpQuerySystemInformation +0x73c → ExpGetSystemBasicInformation
                 ├─ (3) +0x25a · +0x278  ntdll!RtlRandomEx ×2 → BasepFiberCookie   (run61)
                 ├─ (4) +0x2d9  ntdll!CsrClientConnectToServer("\Sessions\1\Windows", 1, &{CtrlRoutine}, 8, &b)
                 │       ├─ +0x7c   RtlImageNtHeaderEx → +0x8f CsrClientProcess ← 1
                 │       └─ +0x118  CsrpConnectToServer
                 │                   ├─ +0x11e  RtlAllocateHeap(CsrHeap, …, 0x42) → CsrPortName.Buffer
                 │                   ├─ +0x1bb  NtOpenSection(…\SharedSection)       ── SYSCALL
                 │                   ├─ +0x242  NtCreateSection(0x10000, RW)          ── SYSCALL
                 │                   ├─ +0x2e4  NtConnectPort(&CsrPortHandle, …\ApiPort)  ── SYSCALL (→ csrss)
                 │                   ├─ +0x2f0  NtClose(만든 섹션)                     ── SYSCALL
                 │                   ├─ +0x33d  NtMapViewOfSection(SharedSection, 읽기 전용)  ── SYSCALL
                 │                   ├─ +0x349  NtClose(SharedSection)                ── SYSCALL
                 │                   │   +0x36d · +0x379 · +0x38d  PEB +0x380 · +0x88 · +0x98
                 │                   │   +0x39c · +0x3ab  CsrProcessId · CsrPortMemoryRemoteDelta
                 │                   └─ +0x3f8  RtlpCreateHeap(포트 뷰) → +0x3f8 CsrPortHeap · +0x404 CsrPortBaseTag ← 0
                 │      +0x32b BaseStaticServerData · +0x337 KernelBaseGlobalData+0x4 · +0x342 BaseRCNumber · … +0x469
                 ├─ (5) +0x497  RtlUnicodeStringToAnsiString                           (run67)
                 ├─ (6) +0x4b2  ntdll!RtlSetUnhandledExceptionFilter(UnhandledExceptionFilter)
                 │                └─ +0x9  RtlEncodePointer → +0x9 RtlpUnhandledExceptionFilter
                 ├─ (7) +0x4c0  ntdll!RtlEncodePointer(0) → +0x4ce BasepCurrentTopLevelFilter
                 ├─ (8) +0x4e6  BaseNlsDllInitialize(…, 1, BaseStaticServerData)
                 │                ├─ +0x10  gpServerNlsUserInfo ← BSSD+0x178
                 │                └─ +0x1c  NlsProcessInitialize
                 │                           ├─ +0x1f AllocTables · +0x35 SetupMainNlsFiles · +0x40 SetupAnsiOemCodeHashNodes
                 │                           │   (NtGetNlsSectionPtr 0xe ×1 · NtInitializeNlsFiles ×1 · NtGetNlsSectionPtr 0xb ×1~2)
                 │                           └─ +0x64 MakeLocHashNode(0x7f)       ← 아래 못 닫음
                 ├─ (9) +0x508  ntdll!RtlInitializeCriticalSection(&KernelBaseGlobalData+0xb0)
                 ├─ (10) +0x526 ConsoleInitialize
                 │                ├─ +0x49  RtlInitializeCriticalSection(&ConsoleStateLock)
                 │                ├─ +0x6a  RtlImageNtHeader → ConsoleIsConsoleApplication ← 1
                 │                ├─ +0x94 · +0x99 · +0x9e  ConsoleRationalize…(아무것도 안 씀) · InitializeCtrlHandling · InitExeName
                 │                ├─ +0xbd  ConsoleShouldAllocateConsole → TRUE (매니페스트 없음)
                 │                ├─ +0x19f ConsoleCreateConnectionObject → +0x1c8 NtCreateFile(<4>\Connect)   ── SYSCALL
                 │                └─ +0x1cf ConsoleCommitState (run51: ConsoleHandle ← 새 핸들, NtDeviceIoControlFile, NtSetInformationProcess(0x31))
                 ├─ (11) +0x165 TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation   (+0x52e JMP 로 옴)
                 │                ├─ +0x5c  EtwEventRegister   → NtTraceControl(0xf)
                 │                └─ +0x83  EtwEventSetInformation → NtTraceControl(0x1e)
                 └─ (12) +0x189 DisableRegCachingForConstrainedImpersonation
                                  └─ +0x39  RtlIsMultiSessionSku → 1 → 본문 안 탐

KERNEL32!BaseDllInitialize (entry, rva 0x2c5d0)
 ├─ (1) +0x36  _DllMainCRTStartupForGS2 (0x56898)          reason 1
 └─ (2) +0x22  _BaseDllInitialize (0x2c610)                  attach 갈래:
            ├─ (1) +0x73   ntdll!RtlImageNtHeader(exe)
            ├─ (2) +0x99   0 을 돌려주는 빈 함수 (ntdll 0x10bc80)
            ├─ (3) +0xba   ntdll!LdrSetDllManifestProber        (run67)
            ├─ (4) +0xc7   메모리 관리 사본 함수                 (run67)
            │      +0xf0   ntdll!RtlSetProtectedPolicy          ← 안 탐 (+0x292 JNC 로 건너뜀)
            │      +0x189 BaseStaticServerData · +0x194 BaseCSDVersion → +0x19e JNZ (→ +0x261)
            ├─ (5) +0x27c  ntdll!RtlInitUnicodeString(&PEB.CSDVersion, BaseCSDVersion)   CSDVersion.Buffer == 0 일 때 (+0x261), 끝나면 +0x288 JMP 로 되돌아감
            ├─ (6) +0x200  ntdll!RtlGetSuiteMask → 0x10 켜짐 → +0x350 쪽
            │      +0x369  InitializeRegTermsrvFpns → 0 (TS 인식 exe) → +0x387 GetRegistryExtensionFlags 안 탐
            ├─ (7) +0x217  ntdll!LdrDisableThreadCalloutsForDll(kernel32) → +0x3b 항목 Flags 비트 18
            └─ (8) +0x224  _Kernel32LegacyLib_BaseDllInitialize
                             ├─ +0x20  KernelBaseGetGlobalData → +0x25 KernelBaseGlobalData+0x28 ← 0
                             └─ +0xa3  ntdll!NtQuerySystemInformation(0, &kernel32 SysInfo, 0x40)   ── SYSCALL
```

- 라벨은 ret 자리. `(n)` 은 그린 것끼리의 코드 흐름 순서 (사이에 안 그린 호출이 많다). kernelbase (11) ETW 는 코드 자리 (`+0x160`) 는 앞이지만 `+0x52e JMP` 로 콘솔 **뒤에** 돈다.
- kernel32 (5) `+0x27c` 는 코드 자리 (`+0x261 ~ +0x288`) 는 뒤쪽이지만 `+0x19e JNZ` 로 들어갔다가 `+0x288 JMP` 로 `+0x1a4` 에 돌아오는 **떨어진 조각**이다 (flow-writer `.asm` 확인). 그래서 실행 순서는 `BaseCSDVersion` 바로 뒤, `RtlGetSuiteMask` 앞.

### syscall 표 (test.exe 기본 길, 이번 범위)

| # | 이름 · 클래스/코드 | 부르는 자리 (call/ret) | 조건 | 횟수 |
|---|---|---|---|---|
| 1 | `NtQuerySystemInformation` 클래스 0 (kernelbase `SysInfo`) | `_KernelBaseBaseDllInitialize +0x23a/+0x241` | 늘 | 1 |
| 2 ~ 7 | `NtOpenSection` · `NtCreateSection` · `NtConnectPort` · `NtClose` · `NtMapViewOfSection` · `NtClose` | `CsrpConnectToServer +0x1b6/+0x1bb` · `+0x23d/+0x242` · `+0x2df/+0x2e4` · `+0x2eb/+0x2f0` · `+0x338/+0x33d` · `+0x344/+0x349` ← `CsrClientConnectToServer +0x113/+0x118` ← `+0x2d2/+0x2d9` | `CsrPortHandle == 0` (처음) | 6 |
| 8 | `NtGetNlsSectionPtr` 종류 `0xe` | `SetupMainNlsFiles +0x4f/+0x56` | `PEB.UseCaseMapping ≠ 0` (`0x6a5` `[관찰]` `_P1a.txt:1817`) | 1 |
| 9 | `NtInitializeNlsFiles` | `RtlGetLocaleFileMappingAddress +0x29/+0x2e` ← `SetupMainNlsFiles +0x9d/+0xa4` | ntdll `gBaseAddress == 0` (`+0x1a` · `+0x24 JNZ`) | 1 (처음) |
| 10 | `NtGetNlsSectionPtr` 종류 `0xb` (949) | `GetCPHashNode +0x2d3/+0x2da` | 노드 캐시에 없을 때 | 1~2 |
| 11 | (`MakeLocHashNode(0x7f)` 아래 레지스트리·csrss 후보) | `NlsProcessInitialize +0x5f/+0x64` | **모름** | 0~? |
| 12 | `NtCreateFile` `<ConsoleHandle>\Connect` | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` | 실제 `ConsoleHandle` | 1 |
| 13 · 14 | `NtDeviceIoControlFile(0x500023)` · `NtSetInformationProcess(0x31)` | `ConsoleCommitState +0x8a/+0x91` · `+0xe9/+0xf0` (run51) | | 1 · 1 |
| 14′ | (run83 정정으로 더함) `NtDeviceIoControlFile(0x500016, API 0x01000008)` — 콘솔 언어 ID | `GetConsoleLangId +0xe1/+0xe8` ← `SetTEBLangID +0x12/+0x17` ← `ConsoleCommitState +0xfa/+0xff` | `params.ConsoleHandle > 0` (`GetConsoleLangId +0x5f` · `+0x62 JLE`) | 1 |
| 15 · 16 | `NtTraceControl` `0xf` · `0x1e` | `EtwEventRegister` · `EtwEventSetInformation` ← `TraceLogging… +0x55/+0x5c` · `+0x7c/+0x83` | 늘 (첫 등록이 아니라 Once 4개 없음) | 1 · 1 |
| 17 | `NtQuerySystemInformation` 클래스 0 (kernel32 `SysInfo`) | `_Kernel32LegacyLib_BaseDllInitialize +0x9c/+0xa3` | 늘 | 1 |

- 합: kernelbase DllMain **15 ~ 16번** + 11번 (모름) (오케스트레이터 고침 2026-11-06: 위 표 1·2~7·8·9·10·12·13~14·15~16 줄을 더하면 1+6+1+1+(1~2)+1+2+2 = 15~16 — 결과서 316줄의 "16~18" 은 셈 실수, 결과서는 안 고침) · kernel32 DllMain 1번. run67 의 `NtQuerySystemInformation(0x32)` 1번은 따로.
- 안 타는 갈래의 syscall (0번): `NtQueryInformationToken(0x1d)`·`NtAccessCheck` (`DisableRegCaching…` 본문), `NtOpenKey`·`NtQueryValueKey`·`NtClose` (`GetRegistryExtensionFlags`·`IsRegTSAppCompatEnabled`), `NtAlpcSendWaitReceivePort` (`CsrpClientConnectToServer` 아래).

### 질문별 요약 (근거 줄은 결과서 해당 절)

| # | 질문 | 답 | 닫힘 |
|---|---|---|---|
| 1 | ETW 공급자 | `Microsoft.Windows.Kernel.Base`, GUID `{3c74afb9-8d82-44e3-b52c-365dbf48382a}`, 칸 kernelbase `0x3a9020` (`0x40`B, PDB 이름 없음). run62 꼴과 같음. 5번째 등록 → RegHandle 계산 `0x5_00000041` | 정적으로 닫힘 (RegHandle 은 계산값) |
| 2 | `DisableRegCaching…` | 레지스트리 안 읽음. 멀티세션 SKU 면 본문 안 탐 → 이 VM 은 syscall 0, 쓰는 칸 없음 | 정적으로 닫힘 |
| 3 | `+0x241` | 클래스 0, `SysInfo` (`0x3aee60`), `0x40`. `SysInfo` 를 채우는 유일한 자리 | 정적으로 닫힘 |
| 4 | csrss 연결 | 꼴·쓰는 자리 위 표. `CsrpClientConnectToServer` 는 정상일 때 안 탐 | 꼴 닫힘 / 값 동적 (Q84) |
| 5 | 예외 필터 | `RtlpUnhandledExceptionFilter ← Encode(kernelbase!UnhandledExceptionFilter)` | 식 닫힘 / 값 동적 |
| 6 | NLS | 앞 세 줄기 닫힘, `MakeLocHashNode(0x7f)` 아래 못 닫음 | 부분 |
| 7 | 락 | `KernelBaseGlobalData+0xb0` (컴퓨터 이름 캐시 락), `DebugInfo = -1`, 스핀 `0x20007d0`, 목록에 안 끼움. `…AndSpinCount` 는 `+0x508` 이 아니라 래퍼 `+0x55/+0x5c` | 정적으로 닫힘 |
| 8 | 콘솔 | "부모 콘솔에 붙기" 갈래, syscall 3 | 갈래 닫힘 / 값 동적 |
| 9 | `SysInfo` 칸 | 칸 11 = 상수 5 (`+0x0`·`+0x8`·`+0x18`·`+0x20`·`+0x28`) + 부팅 때 정해짐 6 | 정적으로 닫힘 (값 6칸은 동적) |
| 10 | kernel32 `_BaseDllInitialize` | 위 표. `RtlSetProtectedPolicy`·`GetRegistryExtensionFlags` 안 탐, 빈 함수 하나, `DontCallForThreads` 켬 | 정적으로 닫힘 |
| 11 | `_Kernel32LegacyLib_BaseDllInitialize` | `KernelBaseGetGlobalData` = `&KernelBaseGlobalData` (`LEA` + `RET`). `BaseDllReadWriteIniFile` 은 detach 에서만 | 정적으로 닫힘 |
| 12 | `_DllMainCRTStartupForGS2` | 모듈 자기 `/GS` 쿠키. run59 쿠키 사슬과 별개 | 정적으로 닫힘 |

## 확인된 것

- `[정적]` **csrss 연결이 ntdll 전역 9개 + PEB 세 칸을 쓴다** (`CsrClientConnectToServer` · `CsrpConnectToServer`, 위 트리):
  - `CsrInitOnceDone` ← 1 (`CsrClientConnectToServer +0xe7`) · `CsrHeap` ← `PEB.ProcessHeap` (`+0xf2`) · `CsrClientProcess` ← 1 (`+0x8f`, `Subsystem ≠ 1`).
  - `CsrPortName` — `MaximumLength` ← `0x42` (`CsrpConnectToServer +0x111`), `Buffer` ← 힙 새 블록 (`+0x11e`), `Length` ← 디렉터리 + `\SharedSection` (`+0x194`) → 디렉터리 + `\ApiPort` (`+0x212`). 마지막 글 `\Sessions\1\Windows\ApiPort` (54B).
  - `CsrPortHandle` ← `NtConnectPort` 의 출력 (`+0x2d3 LEA RCX,[CsrPortHandle]` → `+0x2df/+0x2e4`) — 칸을 커널이 쓴다. 힙 실패 길만 ntdll 이 닫고 0 (`+0x43d`).
  - PEB `+0x380` `CsrServerReadOnlySharedMemoryBase` (`+0x36d`) · `+0x88` `ReadOnlySharedMemoryBase` (`+0x379`) · `+0x98` `ReadOnlyStaticServerData` (`+0x38d`).
  - `CsrProcessId` (`+0x39c`, 응답 블록 `[RBP+8]`) · `CsrPortMemoryRemoteDelta` (`+0x3ab`) · `CsrPortHeap` (`+0x3f8`, `RtlpCreateHeap` 결과) · `CsrPortBaseTag` ← 0 (`+0x404`).
  - `CsrServerApiRoutine` 은 이 길에서 **안 쓴다** — 서버 길 `+0x162 LEA R9` → `+0x182/+0x187 LdrGetProcedureAddressForCaller` 만 (csrss 자신). 같은 서버 길이 `CsrPortHeap` (`+0x1ac`) · `CsrPortBaseTag` (`+0x1b8`, `RtlCreateTagHeap` 반환) 도 쓴다.
- `[정적]` **`NtConnectPort` 는 ALPC 포트로 가는 옛 이름 API** (`NtSecureConnectPort`·`NtAlpcConnectPort` 가 아님). 클라이언트 뷰 = 이름 없는 `0x10000`B 섹션 (`NtCreateSection`, `SEC_COMMIT`), 그 뷰 안에 **개인 힙** `CsrPortHeap` 을 만든다 (`CommitRoutine` = 0 을 돌려주는 빈 함수 `0x10bc80`).
- `[정적]` **kernelbase 가 연결 결과를 쓰는 칸**: `BaseStaticServerData` (`0x3aedb8`) ← `[PEB+0x98]+8` − `PEB+0x380` + `PEB+0x88` (`+0x2fb ~ +0x32b`) · `KernelBaseGlobalData+0x4` ← `CalledFromServer ≠ 0` = 0 (`+0x326 CMP` · `+0x337 SETNZ`) · `BaseRCNumber` (`0x3aedc0`) ← `[BSSD+0x38]` (`+0x332` · `+0x342`) · `KernelBaseGlobalData+0x8`·`+0x18` (16B씩, BSSD `+0`·`+0x10` 의 문자열 머리를 옮기고 `Buffer` 를 내 뷰로 고침, `+0x33e ~ +0x469`).
- `[정적]` **kernel32 도 같은 식**: `_BaseDllInitialize +0x189` kernel32 `BaseStaticServerData` (`0xc2410`) · `+0x194` `BaseCSDVersion` (`0xc2440`) ← BSSD + `0x3a` · `+0x275/+0x27c` `RtlInitUnicodeString(&PEB.CSDVersion, BaseCSDVersion)` (`+0x261` `CSDVersion.Buffer` 가 0 일 때). `_Kernel32LegacyLib_BaseDllInitialize` 가 `BaseStaticServerData` 를 한 번 더, `BaseDllIniFileMapping` (`0xc2350`) ← BSSD `+0x170` 을 옮긴 값 (0 이면 0).
- `[정적]` **ntdll NLS 캐시 둘은 kernelbase NLS 준비가 처음 쓴다**: `RtlGetLocaleFileMappingAddress` (rva `0xfa250`) — `gBaseAddress` (`0x1ce7b0`) 가 0 이면 `+0x29/+0x2e NtInitializeNlsFiles` → `+0x34` **`gSystemLocale` (`0x1d3d94`)** ← 출력 로케일 → `+0x3f CMPXCHG.LOCK` **`gBaseAddress`** ← 뷰 주소 (다른 쪽이 먼저 넣었으면 `+0x7a NtUnmapViewOfSection` 하고 그 값을 씀). 이미 있으면 syscall 없이 돌려줌 (`+0x58 ~ +0x63`). 지도 (`NtInitializeNlsFiles` caller = `RtlpLoadNlsData` 만) 와 결과서 기준으로, ntdll 초기화 줄기에는 이 함수 호출이 없어 **여기가 처음**이다 `[정적, 지도 기준]`.
- `[정적]` **`RtlInitializeCriticalSection` 은 디버그 정보를 안 만든다** (`+0x51 [cs] ← -1`). 만드는 건 `RtlpForceCSDebugInfoCreation` (`0x1d4008`) ≠ 0 일 때뿐 (`+0x58 CMP` · `+0x5f JZ`). 그 바이트를 1 로 쓰는 곳은 `LdrpInitializeProcess +0x999` (`NtGlobalFlag & 0x1000` 또는 `LdrpShouldCreateStackTraceDb`, `+0x989 ~ +0x997`) · `+0x9b5` · `+0x1a10` (`PEB.AppCompatFlags` 비트 22, `+0x9ae`·`+0x1a09 BT …,0x16`) 셋뿐 (`_refs`). `[관찰]` run44 `NtGlobalFlag = 0`·`AppCompatFlags = 0` → 0 그대로 → `RtlCriticalSectionList` 에 안 끼움.
- `[정적]` **`LdrDisableThreadCalloutsForDll`** (81B): `PebLdr+0x48` (`ShutdownInProgress`) 가 0 이면 `+0x25/+0x2a LdrpFindLoadedDllByHandle` → 항목 `+0x6e` (`TlsIndex`) 가 0 일 때만 `+0x3b OR byte [항목+0x6a],4` = **`Flags` 비트 18 `DontCallForThreads`** (PDB `+0x68 Flags`). kernel32 는 TLS 디렉터리가 없어 `TlsIndex = 0` (run28 PE 파일, run44 사진 `TlsIndex` kernel32 0) → 켜진다.
- `[정적]` **빈 함수 하나** (ntdll rva `0x10bc80`): `+0x0 XOR EAX,EAX` · `+0x2 RET`. 같은 주소 이름 22개, 후보: `RtlSetThreadPoolStartFunc`, `CsrIdentifyAlertableThread`, `CsrpHeapCommitRoutine`, `RtlpFcNoopCallback`, `WinSqmIsOptedIn` … (`pdb_aliases.py --lookup ntdll.dll 0x10bc80`). kernel32 가 넘기는 두 함수 (`kernel32+0x69a90`·`+0x69b70`) 는 어디에도 저장되지 않는다.
- `[정적]` **`KernelBaseGetGlobalData`** (8B) = `+0x0 LEA RAX,[KernelBaseGlobalData]` · `+0x7 RET`. kernel32 는 그 주소 `+0x28` 에 0 을 쓴다 (`_Kernel32LegacyLib_BaseDllInitialize +0x25`). 이 칸은 detach (`+0xba CMP` · `+0xbd JNZ`) 에서 `BaseDllReadWriteIniFile` (`+0xed/+0xf2`) 을 부를지 가르는 데만 읽힌다.
- `[정적]` **`_DllMainCRTStartupForGS2`** (24B): reason 1 이면 `+0x9/+0xe __security_init_cookie` — `__security_cookie` (`0x3a90d0`) 가 0 이거나 기본값 `0x2b992ddfa232` 이면 `RDTSC` 로 새로 만들고 `__security_cookie_complement` (`0x3a90d8`) ← `~cookie`. 항상 1 반환. kernelbase 는 래퍼 `+0x28/+0x2d` 에서 (그래서 `_KernelBaseBaseDllInitialize` 머리 `+0x17` 이 이미 채운 쿠키를 읽음), kernel32 는 진입점 `+0x31/+0x36` 에서.
- `[정적]` **`ExpGetSystemBasicInformation` 칸 쓰기 11줄** — 위 칸 표. 상수 칸 `+0x8` = `0x1000` (`+0x3f`) · `+0x18`·`+0x20`·`+0x28` (`+0x4c`·`+0x50`·`+0x5e`) · `+0x0` = 0 (`+0x32`), `+0x4` = `KeMaximumIncrement` (`+0x35`·`+0x3b`), 반환 `+0x103 MOV EAX,R11D` (0).

## 사진과 맞춰 본 것 (run44 `[관찰]`, 새 실행 없음)

- **PEB CSR 칸 셋**: P1a `ReadOnlySharedMemoryBase = (null)`·`CsrServerReadOnlySharedMemoryBase = 0` (`_P1a.txt:1763`·`:1829`) → P1b `0x7ff4ecf70000`·`ReadOnlyStaticServerData = 0x7ff4ecf70750`·`0x7ff5575f0000` (`_P1b.txt:2461`·`:2463`·`:2527`), P2 같음 (`_P2.txt:2473`·`:2539`). `0x750` = 뷰 안 오프셋이라 `+0x98 = 내 뷰 + (csrss 포인터 − csrss 뷰)` 식과 맞다.
- **PEB `CSDVersion`**: P1a·P1b·P2 모두 `_UNICODE_STRING ""` 로 찍힌다 (`_P1a.txt:1804`, `_P1b.txt:2502`, `_P2.txt:2514`). 짐 지도 7.2 는 이 칸을 `P1a~P1b` 로 셌다 — `Buffer` 가 NULL 에서 빈 글 주소로 바뀌어도 이렇게 찍힐 것 `[추측]` (`dt` 출력에 `Buffer` 값이 없음).
- **세션** `PEB.SessionId = 1` (`_P1a.txt:1798`, `_P2.txt:2508`) → 객체 디렉터리 `\Sessions\1\Windows`.
- **갈래 값**: KUSD `SharedDataFlags = 0x10e` (`_P1a.txt:2938`, 비트 8 켜짐 → `DisableRegCaching…` 본문 안 탐), `SuiteMask = 0x110` (`_P1a.txt:2922`, 비트 `0x10` → `+0x350` 쪽), `PEB.UseCaseMapping = 0x6a5` (`_P1a.txt:1817`), `NumberOfProcessors = 2` (`_P1a.txt:1769` → 스핀 `0x20007d0`), 매개변수 `ConsoleHandle = 4`·`ConsoleFlags = 0` (`_P1a.txt:2513`·`:2514`).
- **ntdll `Csr*` 전역 8개**·`gBaseAddress`·`gSystemLocale` 은 짐 지도 7.3 이 `P1a~P1b` 로 분류한 것 (run44 전역 비교) — 정적 결론 "kernel32 준비 안 KERNELBASE 시작 함수가 처음 쓴다" 와 구간이 맞다. 단 이 구간엔 dll1·dll2 올리기도 섞여 있어 사진만으론 못 가른다 (Q84).

## 결과서와 같음 / 다르게 적은 곳 · 보탠 것

- 같음: 질문 1 ~ 12 의 결론, syscall 표, 작은 표, `SysInfo` 칸 표 — flow-writer 가 다시 본 `.asm` 줄이 모두 결과서와 같다.
- **`.c` 기준 (`.asm` 대조 안 함) 인 곳** — 결과서 표시 그대로 옮김:
  - csrss 쪽 `CsrpHandleInlineUserConnect` 의 `msg+0x48`·`+0x4c` (서버 DLL connect 루틴을 부르고 반환값을 응답에 쓰는 자리) — "`CsrpClientConnectToServer` 안 탄다" 결론의 반쪽이 이것이다.
  - `RtlpCreateHeap` 의 "CommitRoutine 있음" 가지에 syscall 없음.
  - `RtlCheckTokenMembershipEx` 안 `NtAccessCheck` 횟수 (안 타는 갈래).
  - ntdll `RtlpCreateHeap` 이 처음 한 번 클래스 0 을 불러 `+0x28` 을 `0x1ce6e8` 에 캐시 (`DAT_1801ce6e8 == 0` 일 때).
- **다르게 적은 곳**:
  - kernel32 `CSDVersion` 쓰기를 결과서는 "`+0x26e / +0x275 RtlInitUnicodeString`" 으로 적었다. `.asm` 은 `+0x26e LEA RCX,[RBX+0x2e8]` · **call `+0x275` / ret `+0x27c`** 다. 이 문서는 call/ret 로 적었다.
  - `RtlSetThreadPoolStartFunc` 는 같은 주소 이름이 22개라 (요청서는 21개) 하는 일 ("0 을 돌려주는 빈 함수") 로 적고 후보를 붙였다 (flow-doc 규칙). kernel32 쪽은 import 이름이 `RtlSetThreadPoolStartFunc` 라 그 이름을 같이 적었다.
  - **매개변수 `ProcessGroupId` (`+0x408`) 는 안 쓴다**: 결과서 반영거리는 "`+0x408` `ProcessGroupId` (0 → PID)" 라고 적었지만, `ConsoleInitialize +0x9e CMP [params+0x408],EDI` 는 0 일 때만 `+0xb2` 에서 PID 를 쓴다. `[관찰]` run44: K1 부터 P2 까지 `0x2f0` (`04_baggage-read_K1.txt:2366`, `_P1a.txt:2540`, `_P1b.txt:3238`, `_P2.txt:3250`) 이라 test.exe 에선 **조건부 안 탐**. (`0x2f0` 은 test.exe PID `0x1dd0` 이 아니다 — 부모 쪽에서 온 값 `[추측]`.) 짐 지도 3.2 에 "조건부 안 탐" 줄로.
  - 매개변수 `WindowFlags` (`+0xa4`) 의 비트 `0x200`·`0x400` 을 `ConsoleCommitState +0x2c ~ +0x159` 가 지울 수 있다고 결과서가 적었지만 정확한 줄이 없어 짐 지도 3장엔 안 옮겼다 (7.6.8 메모). test.exe `WindowFlags = 0` 이라 어느 쪽이든 그대로.
  - attach 순서 표의 stage 값 칸은 "그 단계가 **끝난 뒤** 쓰는 값" 이라고 풀고 쓰는 줄 (`+0x28b`·`+0x2eb`·`+0x4da`·`+0x4fa`·`+0x51a`·`+0x16a`) 을 붙였다. 결과서 표에는 쓰는 줄이 없었다.
- **보탠 것**:
  - ntdll `RtlGetLocaleFileMappingAddress` 의 전역 쓰기 줄 (`+0x34 gSystemLocale` · `+0x3f gBaseAddress`) — 결과서는 "`gBaseAddress`/`gSystemLocale` (`NtInitializeNlsFiles` 결과)" 로만 적음. flow-writer 가 `.asm` 전체를 읽어 짐 지도 3.8 로 옮김.
  - `CsrClientConnectToServer` 서버 길의 `CsrPortHeap` (`+0x1ac`) · `CsrPortBaseTag` (`+0x1b8`) 쓰기 줄 — 결과서는 서버 길을 "안 탐" 으로만. 짐 지도에 "조건부 안 탐" 줄로.
  - `RtlpForceCSDebugInfoCreation` 쓰기 셋의 조건 줄 (`LdrpInitializeProcess +0x989 ~ +0x997` · `+0x9ae` · `+0x1a09`) — 결과서는 쓰기 오프셋만.
  - kernelbase 래퍼 detach 의 `RtlDeleteCriticalSection` 은 call `+0x6f` / ret `+0x76` (결과서 "`+0x68 ~ +0x6f`").

## 정정

- 이전 run 의 정정은 없다.
  - flow.md 2.5 (run25) 의 "`GetRegistryExtensionFlags` — 터미널 서비스 갈래라 test.exe 에선 안 탈 수 있다 `[추측]`" 은 **맞았다** (안 탄다). `[추측]` → `[정적]` 으로 올림 (까닭: `InitializeRegTermsrvFpns` 가 0).
  - 짐 지도 7.8 의 "`P1a~P1b` `Csr*` 8개·PEB CSR 칸 4개 ↔ CSR 연결 (… `CsrClientConnectToServer` `[추측: 이름]`)" 도 맞았다. 쓰는 줄이 나와 3장으로 옮김.
- 요청서 (`_workspace/sast/63_dll-init-rest.md`) 의 짐작 둘은 틀렸다 (요청서는 고치지 않는다):
  - 4번 "부르는 곳 `+0x2d4/+0x2d9` 쯤" → call **`+0x2d2`** / ret `+0x2d9`.
  - 7번 "`+0x508` … `RtlInitializeCriticalSectionAndSpinCount`" 쪽 짐작 → `+0x508` 은 `RtlInitializeCriticalSection` 이고, `…AndSpinCount` 는 래퍼 `KernelBaseBaseDllInitialize +0x55/+0x5c` (`_CrtLock_Exit`, `0xfa0`) 가 부른다.
  - 4번 "`NtSecureConnectPort`/`NtAlpcConnectPort` 등" → **`NtConnectPort`**.

## 아직 확인 필요

동적 몫은 둘로 갈린다. ntdll·kernelbase 멈춤점은 test.exe 일 때만 걸리게 조건을 붙인다.

**Q84 — csrss 연결 결과 (값은 csrss 가 줌)**

1. `ntdll!CsrpConnectToServer+0x2e4` (`NtConnectPort` ret): `r rax` (0?) · `dq ntdll+0x1d4e00 L2` (`CsrPortName`) · `du poi(ntdll+0x1d4e08)` (`\Sessions\1\Windows\ApiPort`) · `dq ntdll+0x1d4e20 L1` (`CsrPortHandle`, 사진 `0x74`) · 응답 블록 `dq @rbp-8 L6` 과 `dd @rbp+0x1c L1` (상태 = 0 기대 → `CsrpClientConnectToServer` 안 탐). 안 맞으면 `ntdll!CsrpClientConnectToServer` 입구에 멈춤점.
2. `+0x38d` 직후: `dq @$peb+0x88 L1` · `dq @$peb+0x98 L1` · `dq @$peb+0x380 L1`. `+0x3f8` (ret) 에서 `dq ntdll+0x1d4e28 L1` (`CsrPortHeap`).
3. `KERNELBASE!_KernelBaseBaseDllInitialize+0x32b` 직후: `dq kernelbase+0x3aedb8 L1` (`BaseStaticServerData`) · `dq kernelbase+0x3aeec0 L0x20` (`KernelBaseGlobalData` 앞쪽).
4. 이 셋으로 짐 지도 Q84 의 "P1a ~ P1b 쪼개기" 중 CSR 몫이 닫힌다 (콘솔 몫은 아래 Q104 5).

**Q104 VM 단계 — 사진·멈춤점으로 값 보기**

1. **`SysInfo` 두 벌**: `KERNELBASE!_KernelBaseBaseDllInitialize+0x241` (ret) 에서 `dq kernelbase+0x3aee60 L8`, `KERNEL32!_Kernel32LegacyLib_BaseDllInitialize+0xa3` (ret) 에서 `dq kernel32+0xc2260 L8` — 동적 6칸 (`+0x4`·`+0xc`·`+0x10`·`+0x14`·`+0x30`·`+0x38`) 값.
2. **`BasepInitializationStage`**: `_KernelBaseBaseDllInitialize` 의 `+0x241`·`+0x2d9`·`+0x32b`·`+0x4e6`·`+0x508`·`+0x526`·`+0x165`·`+0x189` (ret) 마다 `dw kernelbase+0x3aeea0 L1` — 기대 `0x64` (`+0x2d9` 앞) → `0xc8` → `0x190` → `0x1f4` → `0x258` → `0x2bc`.
3. **KUSD `0x7ffe02f0`**: `dd 7ffe02f0 L1` (`0x10e`), `+0x189` 뒤 `dd kernelbase+0x3ab290 L1` (0) · `dd kernelbase+0x3ad9ac L1` (1) — `DisableRegCaching…` 가 정말 안 쓰는지.
4. **NLS syscall 수**: `kernelbase!BaseNlsDllInitialize` 입구 ~ `+0x1c` (ret) 사이에 `bp ntdll!NtGetNlsSectionPtr` (`r ecx` 종류)·`bp ntdll!NtInitializeNlsFiles`·레지스트리 (`NtOpenKey`·`NtQueryValueKey`) 가 서는지 + `k`. `dq kernelbase+0x3afc88 L1` (`gpServerNlsUserInfo`), `dq ntdll+0x1ce7b0 L1` · `dd ntdll+0x1d3d94 L1`.
5. **콘솔 새 핸들**: `KERNELBASE!ConsoleInitialize+0xbd` (ret) `r al` (1?), `+0x19f` (ret) `r eax` · `dq @rbp+0x28 L1` (새 핸들, 사진 `0x60`), 끝에 매개변수 `+0x10` 값.
6. (덤) ETW: `+0x165` (ret) 에서 `dq kernelbase+0x3a9040 L1` (RegHandle, 계산 `0x5_00000041`). 락: `+0x508` 뒤 `dq kernelbase+0x3aef70 L5` (`DebugInfo -1`·스핀 `0x20007d0`). kernel32 항목 `Flags`: `_BaseDllInitialize+0x217` (ret) 에서 kernel32 항목 `+0x68` 의 비트 18.

**정적으로 남은 것** (대기열 번호는 오케스트레이터가 붙임)

1. `NlsProcessInitialize +0x5f/+0x64 MakeLocHashNode(0x7f)` 아래 — `GetWindowsLocaleData → MakeLocHashNodeFromSystemLocale` 가 `GetNamedLocaleHashNode → GetCurrentNlsCache` (레지스트리 `NtOpenKey`·`NtQueryValueKey`·`NtCreateKey`·`NtNotifyChangeKey`, `CsrClientCallServer`) 까지 가는지. 동적 4 로도 닫힘.
2. `ConsoleShouldAllocateConsole` 안 `RtlQueryActivationContextApplicationSettings` — 매니페스트 없는 exe 에서 실패를 돌려준다는 결론의 본문 (syscall 없을 것 `[추측]`).
3. `AllocTables`·`GetCPHashNode`·`MakeLocHashNode` 의 힙 블록 크기·개수 (짐 지도 7.6.8 "미확인").
4. csrss 쪽 `CsrpHandleInlineUserConnect` 를 `.asm` 으로 다시 대조 (지금 `.c` 기준).

## 도구 메모

- `pdb_aliases.py` 의 kernelbase 모듈 이름은 이번에도 `KernelBase.dll` (대소문자 정확히) 로 썼다 (run67 메모 그대로).
- `pdb_struct.py` 는 `_SYSTEM_BASIC_INFORMATION` 을 못 찾는다 — PDB 에 그 타입이 없다 (결과서). 칸 이름은 공개 문서 이름 `[추측-이름]`.
- 새 함정 (디버거) 없음.
- 디버거가 붙어서 달라지는 점: 정적 바퀴라 본 것은 없다. 갈래를 가르는 값 중 `NtGlobalFlag` (`RtlpForceCSDebugInfoCreation` 조건) 은 run44 (커널 디버거 붙은 실행) 에서 0 `[관찰]` — 다른 실행 방식에서 바뀌는지는 모른다.

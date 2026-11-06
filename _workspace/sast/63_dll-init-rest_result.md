# 63 dll-init-rest — 정적 분석 결과

- 요청서: `_workspace/sast/63_dll-init-rest.md` (Q129, run68 예정)
- 모듈 / 바이너리: kernelbase · ntdll · kernel32 · ntoskrnl (`ms/bin/.../System32/`, **새로 analyze 안 함**) + basesrv 1개 (`BaseClientConnectRoutine`, 4번 답 확인용)
  - pdb guid+age: KernelBase `72CCE506944DE0267C172AD64287F9BD1` · ntdll `C3093720177DA6851519DCFFE8DF53FE1` · kernel32 `AE9E9D8F7AC1467A645320FDC23235D01` · ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID 와 같음)
- 뽑은 함수 (새로 `.asm`·`.c`): kernelbase 68 · ntdll 38 · kernel32 5 · ntoskrnl 1 · basesrv 1. 로그 `ms/ghidra/*sast63*`.
  - 요청서 대상 전부: kernelbase `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` · `DisableRegCachingForConstrainedImpersonation` · `BaseNlsDllInitialize` · `ConsoleInitialize` · `KernelBaseGetGlobalData` · `GetRegistryExtensionFlags` · `_DllMainCRTStartupForGS2` (depth 1) / ntdll `CsrClientConnectToServer` · `RtlSetUnhandledExceptionFilter` · `RtlInitializeCriticalSection` · `RtlInitializeCriticalSectionAndSpinCount` · `RtlImageNtHeader` · `CsrIdentifyAlertableThread` (= `RtlSetThreadPoolStartFunc`) · `RtlSetProtectedPolicy` · `LdrDisableThreadCalloutsForDll` (depth 1) / kernel32 `InitializeRegTermsrvFpns` (depth 1) / ntoskrnl `ExpGetSystemBasicInformation` (depth 1).
  - **범위를 넓힌 것 (이유 있음)**: NLS 쪽 (`BaseNlsDllInitialize` 하위 줄기가 syscall 까지 닿는지 보려고) kernelbase `AllocTables`·`SetupMainNlsFiles`·`SetupAnsiOemCodeHashNodes`·`MakeLocHashNode` (depth 1) 과 `GetWindowsLocaleData`·`MakeNamedLocaleHashNode`·`MakeLocHashNodeFromSystemLocale`·`FindLocaleHashNode`·`GetLocaleFileInfo`·`GetUnicodeFileInfo`·`CheckSpecialLocales`·`GetCurrentNlsCache` (depth 1, **열어서 호출 목록만** 봄), ntdll `RtlGetLocaleFileMappingAddress` (depth 1). 콘솔 쪽 kernelbase `ConsoleInitializeServerData` (depth 1). `DisableRegCaching…` 의 갈래 판정에 ntdll `RtlIsMultiSessionSku`·`RtlCheckTokenMembershipEx`·`RtlDeriveCapabilitySidsFromName`. 4번 답에 csrss 쪽 상대편 `basesrv!BaseClientConnectRoutine`. `__security_init_cookie_ex` (12번).
  - `_refs` 새로: kernelbase `SysInfo`(0x40바이트)·`0x3a8fa8`(0xb8바이트, TraceLogging 공급자 칸)·`g_DisableCachingWhileImpersonated`·`g_Win64Registry`, kernel32 `SysInfo`, ntdll `RtlpForceCSDebugInfoCreation`·`RtlCriticalSectionList`.
  - PDB 공개 이름 임시 표 (`llvm-pdbutil dump -publics`)와 PE `.rdata` 바이트 읽기 스크립트는 scratchpad 에만 만들었다 (레포 밖, 커밋 안 함).
- 버전 확인: `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` 일치, `LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal` 일치 (`_calls.tsv`). 커널 PDB GUID 기준값과 같음. → **일치**.
- 스크립트 문제: **없음.** 알아둘 것 둘 (스크립트 버그 아님).
  - `pdb_struct.py` 는 `_SYSTEM_BASIC_INFORMATION` 을 못 찾는다 (PDB 에 그 타입이 없음). 9번 칸 이름은 공개 이름 `[추측-이름]`.
  - kernelbase 는 `KernelBase.dll` (대소문자 그대로) — 이번에도 그렇게 썼다.
- 오프셋 대조: 아래 `+0x..` 는 **전부 `.asm` 줄을 스크립트로 다시 읽어 대조**했다 (검사 약 480줄, 어긋난 5개를 이 문서 쓰기 전에 고침: 예 `RtlpCreateHeap` 부르기는 `+0x3f3/+0x3f8` 이지 `+0x3d7` 이 아님, `_BaseDllInitialize` 의 `TEST AL,AL` 은 `+0x369`). 부르기는 call/ret 둘 다, 점프 목적지는 "목적지 − 함수 시작".
  - 예외: 4번의 csrss 쪽 (`CsrpHandleInlineUserConnect` 의 `msg+0x48/+0x4c`) 은 `.c` 기준이라 `.asm` 대조 안 함. 표시해 둠.

말투 주의: 이 문서의 "탄다 / 안 탄다" 는 `.asm` 의 조건 + run44 사진 값 (`[관찰]`) 으로 **계산한 정적 결론**이다. 실제로 그 길로 갔는지는 동적 몫이다.

## 한 줄 결론 (PE_emulator 용)

1. **ETW (묶음 A-1)**: kernelbase 가 등록하는 공급자는 **`Microsoft.Windows.Kernel.Base`**, GUID `{3c74afb9-8d82-44e3-b52c-365dbf48382a}`, 칸 `kernelbase+0x3a9020` (0x40바이트, PDB 공개 이름 없음). 모양은 run62 (ntdll 쪽 `TraceLoggingRegisterEx…`) 와 같다. 이 프로세스의 **5번째 등록**이라 Once 4번은 없고 syscall 은 `NtTraceControl` 2번 (`0xf`, `0x1e`).
2. **`DisableRegCachingForConstrainedImpersonation`**: 이름과 달리 레지스트리를 안 읽는다. 본문이 돌 때는 **토큰**을 보고 (`NtQueryInformationToken`) 결과 1바이트를 `g_DisableCachingWhileImpersonated` (kernelbase `0x3ab290`) 에 쓴다. 이 VM 은 멀티세션 SKU (`SharedDataFlags` `0x10e` 비트 8 `[관찰]`) 라 **함수 본문 갈래를 안 탄다** (멀티세션 SKU 가 아닐 때만 본문이 돈다 → syscall 0번, 쓰는 칸 없음).
3. **`+0x241 NtQuerySystemInformation`** = **클래스 0 (`SystemBasicInformation`), 버퍼 kernelbase `SysInfo` (`0x3aee60`), 길이 `0x40`**. 맞다, 이것이 `SysInfo` 를 채우는 유일한 자리다.
4. **`CsrClientConnectToServer`**: test.exe 는 "클라이언트" 길. 객체 디렉터리 `\Sessions\1\Windows` (세션 1 `[관찰]`), 서버 번호 1 (BASESRV), 연결 정보 = 8바이트 (`kernelbase!CtrlRoutine` 주소) 하나, `CalledFromServer` 출력. 안에서 syscall **6번** (`NtOpenSection`·`NtCreateSection`·`NtConnectPort`·`NtClose`·`NtMapViewOfSection`·`NtClose`). PEB 칸 셋 (`+0x88`·`+0x98`·`+0x380`) 과 `Csr*` 전역을 쓴다. 값은 csrss 가 주므로 동적 (Q84).
5. **`RtlSetUnhandledExceptionFilter(kernelbase!UnhandledExceptionFilter)`**: 인코딩해서 `RtlpUnhandledExceptionFilter` (ntdll `0x1d28e8`) 에 넣고 끝. syscall 없음.
6. **`BaseNlsDllInitialize`**: `gpServerNlsUserInfo` ← `BaseStaticServerData+0x178` 를 쓰고 `NlsProcessInitialize` 로 간다. run57 과 **이어진다** (같은 `NtGetNlsSectionPtr` 종류 `0xe`·`0xb`, kernelbase 가 자기 뷰를 또 만든다). syscall 후보: `NtGetNlsSectionPtr` 2~3번 + `NtInitializeNlsFiles` 1번. `MakeLocHashNode(0x7f)` 아래는 못 닫음.
7. **락**: `+0x508` 이 초기화하는 락은 kernelbase `KernelBaseGlobalData+0xb0` (rva `0x3aef70`, `_RTL_CRITICAL_SECTION` 0x28바이트) = **컴퓨터 이름 캐시 락** (`GetComputerNameExW` 가 씀). `RtlInitializeCriticalSection` 은 `DebugInfo = -1` (디버그 정보 블록 **안 만듦**), `SpinCount = 0x20007d0` (CPU 2개 이상일 때), `RtlCriticalSectionList` 에 안 끼움. syscall 없음. (`RtlInitializeCriticalSectionAndSpinCount` 는 **`+0x508` 이 아니라** DllMain 래퍼 `KernelBaseBaseDllInitialize +0x55/+0x5c` 가 `_CrtLock_Exit` 에 `0xfa0` 으로 부른다.)
8. **`ConsoleInitialize`**: test.exe (콘솔, 부모 cmd, `ConsoleHandle=4`·`ConsoleFlags=0` `[관찰]`) 는 "부모 콘솔에 붙기" 갈래. `NtCreateFile(<ConsoleHandle=4> 기준 "\Connect")` 로 새 연결 핸들을 만들어 `ProcessParameters.ConsoleHandle` 을 `4 → 0x60` 으로 바꾼다 (`ConsoleCommitState`, run51). 값(핸들 숫자·서버 PID)은 condrv·부모에서 오므로 동적.
9. **`_SYSTEM_BASIC_INFORMATION`**: 칸 11개 중 **상수 5칸** (`+0x8`=0x1000, `+0x18`=0x10000, `+0x20`=0x10000, `+0x28`=0x7ffffffeffff, `+0x0`=0), **부팅 때 정해지는 칸 6칸** (아래 표). kernelbase 는 `+0x8`·`+0x18`·`+0x20`·`+0x28` 을 읽고 (20여 함수), kernel32 는 `+0x8` 만 읽는다. **PE_emulator 가 채우는 `+0x18`·`+0x28`·`+0x38` 로는 모자란다**: `+0x8` (`HeapCreate`·`CreateFiberEx`·`GlobalMemoryStatusEx`·`IsBadReadPtr` …) 과 `+0x20` (`GlobalMemoryStatusEx`) 도 읽는다.
10. **kernel32 `_BaseDllInitialize`**: test.exe 에서 `RtlSetProtectedPolicy` (**안 탐**, 서브시스템 버전 6 ≥ 6), `GetRegistryExtensionFlags` (**안 탐**, `InitializeRegTermsrvFpns` 가 곧바로 0 — test.exe 가 TERMINAL_SERVICE_AWARE). `RtlSetThreadPoolStartFunc` = **아무것도 안 하고 0 반환** (같은 주소 이름 22개). `LdrDisableThreadCalloutsForDll` 는 kernel32 항목 `Flags` 의 **비트 18 `DontCallForThreads`** 를 켠다 (`TlsIndex == 0` 이라서).
11. **`_Kernel32LegacyLib_BaseDllInitialize`** (attach): `KernelBaseGlobalData+0x28 ← 0`, kernel32 `BaseStaticServerData`·`BaseDllIniFileMapping` 채움, `BaseDllInitializeMemoryManager2` 한 번 더, `NtQuerySystemInformation(0, kernel32 SysInfo, 0x40)`. `KernelBaseGetGlobalData` = `&KernelBaseGlobalData` 를 돌려줌 (맞다). `BaseDllReadWriteIniFile` 은 **detach 에서만**.
12. **`_DllMainCRTStartupForGS2`**: reason == 1 일 때만 `__security_init_cookie` → 그 모듈 **자기** `/GS` 쿠키 (`__security_cookie`) 를 `RDTSC` 로 만든다. run59 의 프로세스 쿠키 (ntdll `CookieValue`, `RtlEncodePointer` 용) 와는 **별개 사슬**. kernelbase 는 `KernelBaseBaseDllInitialize +0x28/+0x2d` (모든 reason) 에서, kernel32 는 `entry +0x31/+0x36` (reason 1) 에서 부른다.

## 질문별 답 [정적]

### 1. `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` (kernelbase rva `0xf70ac`, 182B)

- 부르는 곳: `_KernelBaseBaseDllInitialize +0x160 / +0x165` 하나. DLL_PROCESS_ATTACH 의 **맨 마지막 단계**다: `ConsoleInitialize` 가 성공 (`+0x521/+0x526`) 해야 `+0x52e JMP 0x18004e820` (= `+0x160`) 로 온다 (실패면 `+0x528 JZ 0x18004e7b6` = `+0xf6` 에서 FALSE). 반환값은 안 본다 (`+0x165` 가 곧바로 `MOV EAX,0x2bc`, `BasepInitializationStage` 에 저장).
- **공급자 = kernelbase `0x3a9020`** (0x40바이트, `~0x3a9060`). PDB 공개 이름이 없다. 앞 PDB 이름 `…FeatureTuningTelemetryCore…__hInner+0x78` 은 "가장 가까운 앞 이름 + 거리" 일 뿐이고 진짜 `__hInner` 는 `0x3a8fa8 ~ 0x3a8fe0` (0x38바이트) 로 **별개 공급자**다 (`_refs/0x3a8fa8.tsv`). 칸 (ntdll 쌍둥이 sast/57 표와 같은 모양):

| 칸 | 값 | 쓰는/읽는 줄 |
|---|---|---|
| `+0x00` dword (`0x3a9020`) | 켜짐 수준+1 (처음 0) | `_tlgEnableCallback` 이 씀 `[추측: ntdll 쌍둥이와 같다]`, `CreateProcessInternalW` 가 `CMP …,5` 로 읽음, detach `_KernelBaseBaseDllInitialize +0x5bb` 가 0 |
| `+0x08` (`0x3a9028`) | 메타데이터 포인터 = `kernelbase+0x368a85` | `+0x1d`·`+0x67` 읽음 |
| `+0x10`/`+0x18` | 키워드 any/all (처음 0) | `CreateProcessInternalW` 읽음 |
| `+0x20` (`0x3a9040`) | **RegHandle 8B** (처음 0) | `EtwEventRegister` 가 씀. detach `+0x5c1` 이 0. `+0x15 CMP …,0 / +0x2e JNZ 0x1800f7157` (= `+0xab`) 이면 `INT 0x29` (코드 5, 빠른 실패) |
| `+0x28`/`+0x30` | 16B, 처음 0 | 이 함수 `+0x4d` 가 0 으로 씀 |

- **정체**: 메타데이터 앞 16바이트 (`0x368a75`) = GUID **`{3c74afb9-8d82-44e3-b52c-365dbf48382a}`** (이 함수가 `+0x24` 에서 `[메타−0x10]` 을 스택으로 복사). 메타 첫 word `0x33` = 길이, 이름 문자열 **`Microsoft.Windows.Kernel.Base`**, 뒤에 "그룹 GUID" 특성 `{4f50731a-89cf-4782-b3e0-dce8c90476ba}` (ntdll 의 `Microsoft.Windows.Heap` 과 같은 그룹). PE 파일 `.rdata` 바이트를 직접 읽음 (`0x368a75 ~ 0x368ab9`, 임시 스크립트).
- 하위 호출 (call / ret): `+0x55/+0x5c` `EtwEventRegister` (`RCX` = 스택 GUID 사본 `+0x48`, `RDX` = `0x180105aa0` = **`_tlgEnableCallback`** kernelbase `0x105aa0` 117B `+0x41`, `R8` = 공급자 자신 `0x3a9020` `+0x3a`, `R9` = `0x3a9040` `+0x33`) · `+0x7c/+0x83` `EtwEventSetInformation` (`RCX` = 핸들 `+0x71`, `EDX` = 2 `+0x6e`, `R8` = 메타 `+0x67`, `R9D` = 메타 첫 word `0x33` `+0x78`). `EtwEventRegister` 가 0 아닌 값을 주면 `+0x65 JNZ 0x1800f714a` (= `+0x9e`) → 반환만 하고 SetInformation 건너뜀. 반환은 `EtwEventRegister` 의 상태.
- **syscall**: 직접 없음. 아래로 `NtTraceControl(0xf)` 1 (`EtwpRegisterProvider`) + `NtTraceControl(0x1e)` 1 (`EtwpSetProviderTraits`) = **2번**. "Once" 4번 (`NtCreateEvent`·`NtCreateWaitCompletionPacket`·`NtAssociateWaitCompletionPacket`·`NtTraceControl(0x1b)`) 은 **첫** `EtwpRegisterProvider` 에서만 도는데 (sast/57), 이 프로세스의 첫 등록은 `LdrpInitializeProcess +0xbee/+0xbf3` 라서 여기서는 안 탄다. → **run62 꼴과 같음, 가리키기만.**
- **RegHandle 계산값** `[정적 계산]`: 앞의 4번 등록 (A 0x1_00000000 · B 0x2_00000011 · C 0x3_00000021 · D 0x4_00000031) 이 풀리지 않았다고 보면 이 등록이 **5번째** → 순번 5, 칸 4 → **`0x5_00000041`**. kernelbase 가 `LdrpInitializeProcess +0x19a2` (`LdrpInitializeKernel32Functions`) 안에서 돌고, 다른 `EtwEventRegister` 호출 (`SbObtainTraceHandle` = `+0x19c8`) 은 그 뒤라서 사이에 끼는 등록은 없다고 본 것. 사진으로 확인 필요 (동적 D-ETW).
- 참고: DllMain 이 끝나고 돌아온 `entry` 가 `+0xf1/+0xf6` 에서 또 다른 등록 (`TraceLoggingRegisterEx_EventRegister_EventSetInformation(&0x3a8c08)`) 을 한다 — 이건 6번째 (이번 질문 밖, 순서 계산에만 필요).

### 2. `DisableRegCachingForConstrainedImpersonation` (kernelbase rva `0x4e64c`, 418B)

- 부르는 곳: `_KernelBaseBaseDllInitialize +0x184 / +0x189`. 조건: `+0x171 CMP [g_Initialized (0x3ab318)],0` → 0 일 때만 (첫 attach). 부르기 직전 `+0x17a` 에서 **`g_Win64Registry` (kernelbase `0x3ad9ac`) ← 1** 을 쓰고, 부른 뒤 `+0x189 INC.LOCK g_Initialized`. 인자 없음. `void` (반환 안 봄). (`g_Win64Registry` 는 `BaseRegOpenClassKeyFromLocation +0xd3` 이 읽음.)
- **읽는 것 → 쓰는 것**: 레지스트리는 **안 읽는다.** 읽는 건 ① `RtlIsMultiSessionSku()` ② 프로세스 토큰 (`NtQueryInformationToken`) ③ "capability" 이름 `constrainedImpersonation` (UTF-16, kernelbase `0x2dbe00`). 쓰는 건 **`g_DisableCachingWhileImpersonated` (kernelbase `0x3ab290`, dword) 하나** (`+0x150e1e`, 이 함수의 "떨어진 범위" 오프셋). 읽는 곳 = `MapPredefinedHandleInternal +0x154` (`CMP …,1`) 하나 (`_refs`).
- **갈래 (test.exe)**: 함수 머리가 `RtlIsMultiSessionSku()` 를 부르고 결과가 **0 일 때만** 본문을 탄다 (아래 표). `.asm` 의 큰 오프셋 `+0x150xxx` 는 Ghidra 가 함수 본체를 두 덩이로 잡아서 생긴 값이다 (`.asm` 에 "같은 함수의 떨어진 범위" 머리).

| 줄 | 하는 일 |
|---|---|
| `+0x32 / +0x39` | `RtlIsMultiSessionSku()` — ntdll rva `0x6be60`. `RtlGetCurrentServiceSessionId()` (`+0x4`, `PEB.SharedData` = `PEB+0x90` 이 NULL 이면 0) 가 0 이면 **`SharedUserData+0x2f0` (`SharedDataFlags`) 비트 8 `DbgMultiSessionSku`** 를 돌려준다 (`+0x26 MOV EAX,[0x7ffe02f0]`, `+0x2d SHR EAX,8`, `+0x30 AND AL,1`). `[관찰]` run44: `SharedDataFlags = 0x10e` (비트 8 켜짐), `PEB.SharedData = NULL` → **AL = 1** |
| `+0x3e TEST AL,AL` / `+0x40 JZ 0x18019f336` | **AL = 0 이면 본문 (`+0x150cea`) 으로 점프**, **AL ≠ 0 이면 점프 안 하고 `+0x46` 으로 떨어져 `__security_check_cookie` 후 반환** (`+0x4d / +0x52`, `+0x62 RET`). 이름 그대로 "멀티세션 SKU 가 **아닐 때만** 캐시를 끈다" |
| `+0x150cea ~ +0x150d12 / +0x150d19` | (AL = 0 일 때) **`NtQueryInformationToken(RCX = -4, EDX = 0x1d, R8 = &[RSP+0x34], R9D = 4, [RSP+0x20] = &[RSP+0x38])`** — `-4` = `NtCurrentProcessToken()`, 클래스 `0x1d` = `TokenIsAppContainer` |
| `+0x150d20 JS 0x18004e692` | 실패면 쓰기 없이 반환 (`+0x46`) |
| `+0x150d32 / +0x150d39` | `RtlInitUnicodeString(&us, "constrainedImpersonation")` |
| `+0x150d3e CMP [RSP+0x34],0` / `+0x150d43 JZ 0x18019f3ae` (= `+0x150d62`) | AppContainer 면 `+0x150d51 / +0x150d58` `RtlCapabilityCheck(NULL, &us, &결과)` 후 `+0x150d5d JMP` (= `+0x150e19`). 아니면 아래 |
| `+0x150d70 / +0x150d77` | `RtlDeriveCapabilitySidsFromName(&us, …)` — syscall 없음 (SHA-256 은 SymCrypt, ntdll `_calls`). `+0x150d7e JS 0x18019f3e8` (= `+0x150d9c`) 실패면 건너뜀 |
| `+0x150d90 / +0x150d97` | `RtlCheckTokenMembershipEx(-4, 능력 SID, 0, &결과 [RSP+0x30])` |
| `+0x150d9c CMP [RSP+0x30],0` / `+0x150da1 JNZ 0x18019f465` (= `+0x150e19`) | 이미 멤버면 결과를 쓰러 감 |
| `+0x150dad`, `+0x150db0`, `+0x150dbc / +0x150dc3` | `RtlInitializeSid(…, 권한 {0,0,0,0,0,5}, 하위 권한 2개)` |
| `+0x150dcf / +0x150dd6`, `+0x150de5` | `RtlSubAuthoritySid(sid, 0)` 칸에 **`0x20`** |
| `+0x150deb / +0x150df2`, `+0x150e07` | `RtlSubAuthoritySid(sid, 1)` 칸에 **`0x245`** (= S-1-5-32-581. 이름 "System Managed Accounts Group" 은 `[추측]`) |
| `+0x150e0d / +0x150e14` | `RtlCheckTokenMembershipEx(-4, sid, 0, &결과)` |
| `+0x150e19 ~ +0x150e1e` | `MOVZX EAX,[RSP+0x30]` → **`g_DisableCachingWhileImpersonated ← 결과`** (0 또는 1) |
| `+0x150e24 JMP 0x18004e692` | `+0x46` 으로 가서 반환 |

- **test.exe, 이 VM (AL = 1)**: 본문을 **안 탄다.** → syscall **0번**, 이 함수가 쓰는 칸 **없음** (`g_DisableCachingWhileImpersonated` 는 0 그대로; `g_Win64Registry ← 1` 은 호출한 쪽 `+0x17a` 가 씀).
- 본문 갈래 (멀티세션 아닌 SKU, 이 VM 은 안 탐)의 syscall: `NtQueryInformationToken(0x1d)` 1 + `RtlCheckTokenMembershipEx` 안 `NtAccessCheck` (호출마다 1번, 토큰 핸들이 NULL 이 아니라 `NtOpenThreadTokenEx`/`NtOpenProcessTokenEx`/`NtDuplicateToken` 은 안 탐, `ms/decomp/ntdll/RtlCheckTokenMembershipEx.c` 기준 `[정적, .c]`) 1~2번. 결과 1바이트는 토큰 안에 그 능력 SID / S-1-5-32-581 이 있는지에 달려 동적.
- 동적 확인: `dd 7ffe02f0 L1` 이 `0x10e` 인지 (비트 8), 반환 뒤 `dd kernelbase+0x3ab290 L1` 이 0 인지.

### 3. `+0x241 NtQuerySystemInformation` — 클래스·버퍼

- 부르기 `_KernelBaseBaseDllInitialize +0x23a / +0x241`. 인자: `ECX = 0` (`+0x234 XOR ECX,ECX`, 클래스 0 = `SystemBasicInformation`), `RDX = &SysInfo` (`+0x22d LEA RDX,[0x1803aee60]`, kernelbase rva **`0x3aee60`**), `R8D = 0x40` (`+0x236 LEA R8D,[R9+0x40]`, R9D = 0), `R9D = 0` (ReturnLength 없음). `SysInfo` 는 `0x3aee60 ~ 0x3aeea0` 이고 바로 뒤가 `BasepInitializationStage` (`0x3aeea0`).
- 실패 (`+0x246 TEST EAX,EAX` / `+0x248 JS 0x18004ec6b` = `+0x5ab`) 면 `XOR AL,AL` → **DllMain FALSE** (프로세스 초기화 실패).
- **`SysInfo` 를 채우는 곳은 이 한 줄뿐** (`_refs/SysInfo.tsv` 의 `DATA` 참조가 `+0x22d` 하나). 이 호출의 앞에는 `+0x1e7/+0x1ee RtlGetCurrentServiceSessionId`, `+0x225/+0x22a StringCbPrintfW` (객체 디렉터리 문자열 만들기, 4번에서 씀) 가 있다.
- 읽는 곳과 칸은 9번 표.

### 4. `CsrClientConnectToServer` (ntdll rva `0xdb7a0`, 489B)

- **부르는 자리**: `_KernelBaseBaseDllInitialize +0x2d2 / +0x2d9` (요청서의 "`+0x2d4`" 가 아니라 call `+0x2d2`, ret `+0x2d9`). 바로 앞 `+0x2a7 / +0x2ab` 가 `[PEB+0x20].Flags (+8)` 부호를 보고 (`JL 0x18004ed52` = `+0x692` → `CsrpLocalSetupForSecureProcess`, **secure process 만**; test.exe `Flags 0x4001` 은 양수라 안 탐).
- **인자** (kernelbase 가 만들어 줌):

| 인자 | 값 | 줄 |
|---|---|---|
| `RCX` 객체 디렉터리 (UTF-16 문자열 포인터) | `\Sessions\<PEB.SessionId>\Windows` 를 만든 스택 버퍼 `[RSP+0x50]` → 이 VM `\Sessions\1\Windows` (SessionId = 1 `[관찰]` run44 P1a `PEB+0x2c0`). 서비스 세션 (`SessionId == RtlGetCurrentServiceSessionId`) 이면 `\Windows` | `+0x1e1`·`+0x1e7`·`+0x201 CMP EBP,EAX`/`+0x203 JZ 0x18004ecfd` (= `+0x63d`), 만들기 `+0x225/+0x22a StringCbPrintfW` (서식 `%ws\%ld%ws` = kernelbase `0x2a2cc0`, `\Sessions` `0x2a3b68`, `\Windows` `0x2a2ca8`), 넘기기 `+0x2c9` |
| `EDX` 서버 DLL 번호 | **1** (BASESRV) | `+0x2ce LEA EDX,[R9+-0x7]` (R9D = 8) |
| `R8` 연결 정보 버퍼 | `&[RSP+0x48]` = 8바이트 지역변수, 안에 **`0x1800c6400` = kernelbase `CtrlRoutine`** (`+0x292 LEA RAX,[0x1800c6400]`, `+0x299`) | `+0x2bf` |
| `R9D` 연결 정보 크기 | **8** | `+0x2b4` |
| 5번째 `[RSP+0x20]` | `PBOOLEAN CalledFromServer` = `&[RSP+0x30]` (지역, `+0x29` 에서 0) | `+0x2c4` |

- **본문 갈래** (`ms/disassemble/ntdll/CsrClientConnectToServer.asm`, test.exe):
  - `+0x2f ~ +0x37`: 연결 정보가 NULL 이 아닌데 크기가 0 이면 `0xC000000D`. (8 이라 통과)
  - `+0x3d / +0x4a JZ 0x1800db87e` (= `+0xde`): `CsrInitOnceDone` (ntdll `0x1d4df9`) 0 → `+0xe7` 1 로, `+0xf2` **`CsrHeap` (`0x1d4e38`) ← `PEB.ProcessHeap`** (`+0xee`).
  - `+0x50 CMP CsrServerApiRoutine (0x1d4df0),0`: 0 (이 프로세스는 csrss 가 아님) → 아래로. (0 이 아니고 `CsrClientProcess` 도 0 이면 `+0x125` 에서 서버 길 `+0x1be`.)
  - `+0x77 / +0x7c` `RtlImageNtHeaderEx(3, PEB.ImageBaseAddress, 0, &out)` → `+0x81 CMP word [NT+0x5c],1`, `+0x86 JZ` : `Subsystem == 1 (NATIVE)` 이면 `CsrClientProcess` 그대로, 아니면 `+0x8f` **`CsrClientProcess` (`0x1d4df8`) ← 1**. test.exe `Subsystem = 3` → 1.
  - `+0x97 TEST AL,AL` 이 참 → "클라이언트" 길 (`+0x9d`). 거짓이면 `+0x137` 서버 길: `LdrGetDllHandleEx` → `LdrGetProcedureAddressForCaller` 로 `CsrServerApiRoutine` 채움 + `RtlCreateTagHeap` (csrss 만; **안 탐**).
  - `+0x9f TEST RBX,RBX`: 연결 정보가 있고 `+0xa4 CMP CsrPortHandle (0x1d4e20),0` 이 0 (처음 연결) → `+0xab JZ 0x1800db89e` (= `+0xfe`): **`+0x113 / +0x118 CsrpConnectToServer(ObjDir, ServerId, ConnInfo, Size, &상태 [RSP+0x70])`**. `[RSP+0x70]` 는 `+0x23` 에서 `0xC0000001` 로 미리 채움.
  - 돌아와서 `+0x11a JS` (음수면 그 상태로 반환), `+0x11c CMP [RSP+0x70],0 / +0x121 JL 0x1800db84d` (= `+0xad`): 상태가 음수이면 **`+0xb5 / +0xba CsrpClientConnectToServer(서버번호, 연결정보, 크기)`**; 아니면 `+0x123 JMP +0xba` 로 끝. 끝에서 `CalledFromServer` 가 NULL 이 아니면 `+0xc7 *p = 0`.
  - 반환: 성공 `EAX = 0` (`CsrpConnectToServer` 의 반환 그대로).
- **`CsrpConnectToServer`** (ntdll rva `0xdbfe4`) 안, 처음 연결 (순서, `ms/disassemble/ntdll/CsrpConnectToServer.asm`):

| 줄 (call / ret 또는 줄) | 하는 일 |
|---|---|
| `+0xad TEST RSI,RSI / JZ` | 객체 디렉터리 NULL → `0xC000000D` (`+0x486`) |
| `+0xb6 ~ +0xcb` | 글자 수 세기 → 바이트 `+0xc3`, `+0xc7 LEA R8,[RDI+0x1c]`, `+0xcb CMP R8,0xffff` / `+0xd4` `0xC0000106` (이름 너무 김) |
| `+0x104 ~ +0x11e` | `RtlAllocateHeap(CsrHeap, Flags = NtdllBaseTag, 크기 = 바이트+0x1c)` (call `+0x119`, ret `+0x11e`) → **`CsrPortName.MaximumLength` (`0x1d4e02`) ← 크기**, **`CsrPortName.Buffer` (`0x1d4e08`) ← 새 블록**. `\Sessions\1\Windows` 이면 38+0x1c = **0x42 (66)바이트**. NULL → `0xC0000017` |
| `+0x13a / +0x13f`, `+0x13f ~ +0x17b` | `memcpy(Buffer, 객체 디렉터리)`, `\` 한 글자, 이어서 `SharedSection\0` (ntdll `0x17ff90 ~ 0x17ffaa`, 13글자) |
| `+0x194` | `CsrPortName.Length` ← 디렉터리+2+26 |
| `+0x1b6 / +0x1bb` | **`NtOpenSection(&[RSP+0x50], 4 (SECTION_MAP_READ), OBJECT_ATTRIBUTES{0x30, Root 0, Name=&CsrPortName, 0x40})`** — 이름 `\Sessions\1\Windows\SharedSection`. `+0x1bf JS` 면 버퍼 해제 후 그 상태 반환 |
| `+0x1c5 ~ +0x1fa`, `+0x212` | 이름 끝을 `ApiPort` (ntdll `0x17ffb0`, 7글자) 로 덮고 `CsrPortName.Length` ← 54 |
| `+0x23d / +0x242` | **`NtCreateSection(&[RSP+0x68], 0xf001f (SECTION_ALL_ACCESS), NULL, &크기 0x10000, PAGE_READWRITE 4, SEC_COMMIT 0x8000000, 파일 NULL)`** — 이름 없는 0x10000바이트 섹션 (csrss 와 공유할 "포트 메모리") |
| `+0x282 CMP R12D,8` / `+0x28c CMP R14D,EDI` | 크기 8 이고 서버 번호 1 이면 `+0x295 [RBP+0x20] = *(QWORD*)ConnInfo` (= `CtrlRoutine`), `+0x29c [RBP+0x18] = 1` (서버 번호). 아니면 `+0x411` 에서 `[RBP+0x1c] = 0xC000000D` 로 미리 채우고 그대로 연결 |
| `+0x2df / +0x2e4` | **`NtConnectPort(&CsrPortHandle (0x1d4e20), &CsrPortName = \Sessions\1\Windows\ApiPort, SecurityQos{SecurityImpersonation 2, ctx 1, effective 1}, &ClientView{Length 0x30, 섹션 핸들, ViewSize 0x10000}, &ServerView{Length 0x18}, &최대메시지길이, ConnInfo = 0x30바이트 블록 [RBP-8], &길이 0x30)`**. 이것은 **ALPC 포트로 가는 NtConnectPort** (`NtSecureConnectPort`·`NtAlpcConnectPort` 가 아니다). 응답으로 연결 블록 채워짐 |
| `+0x2eb / +0x2f0` | `NtClose(NtCreateSection 핸들 [RSP+0x68])` |
| `+0x2f8 / +0x308` | `*OutStatus ← 연결 블록 [RBP+0x1c]` (csrss 의 응답 상태) |
| `+0x338 / +0x33d` | **`NtMapViewOfSection(SharedSection 핸들 [RSP+0x50], -1, &[RSP+0x70], 0, 0, NULL, &[RBP-0x40], ViewUnmap 2, MEM_TOP_DOWN\|SEC_NO_CHANGE 0x500000, PAGE_READONLY 2)`** — 읽기 전용 공유 섹션을 내 주소공간에 붙임 |
| `+0x344 / +0x349` | `NtClose(SharedSection 핸들)` |
| `+0x36d`, `+0x379`, `+0x38d` | **PEB 쓰기 셋** (`[GS:60]` = PEB): `PEB+0x380` (`CsrServerReadOnlySharedMemoryBase`) ← 응답 블록 `[RBP-8]` (csrss 쪽 뷰 주소) · `PEB+0x88` (`ReadOnlySharedMemoryBase`) ← 지역 `[RSP+0x70]` (내 뷰 주소) · `PEB+0x98` (`ReadOnlyStaticServerData`) ← `[RBP] − [RBP-8] + 내 뷰` (= csrss 쪽 포인터를 내 뷰로 옮긴 값) |
| `+0x39c`, `+0x3ab` | **`CsrProcessId` (`0x1d4e10`) ← 응답 블록 `[RBP+8]`**, **`CsrPortMemoryRemoteDelta` (`0x1d4e18`) ← `ClientView.ViewRemoteBase − ViewBase`** (`[RBP-0x60] − [RBP-0x68]`) |
| `+0x3b2 / +0x3b7`, `+0x3bb ~ +0x3f3 / +0x3f8` | `RTL_HEAP_PARAMETERS` 0x60바이트 (`InitialCommit 0x1000`, `InitialReserve = ViewSize`, **`CommitRoutine = 0x18010bc80` = `CsrpHeapCommitRoutine`** — `xor eax,eax; ret`, 같은 주소 PDB 이름 22개 무리) → **`RtlpCreateHeap(0x8000, ViewBase, ViewSize, 1, NULL, &params, 0)`** (call `+0x3f3`, ret `+0x3f8`) = 포트 메모리 안에 만드는 **개인 힙**. 이 갈래는 `RtlpCreateHeap` 안의 "CommitRoutine 있음" 가지 (`memset(HeapBase, 0, 0x1000)` 만, 이 가지엔 syscall 없음 `[정적, .c 기준]`) |
| `+0x3f8`, `+0x404` | **`CsrPortHeap` (`0x1d4e28`) ← 힙**, **`CsrPortBaseTag` (`0x1d4e30`) ← 0** |
| `+0x40a` | 반환 0 |

  - 실패 갈래: `NtConnectPort` 가 음수면 `+0x2f2 JS +0x490` 에서 섹션 핸들을 닫고 `+0x44e` 에서 이름 버퍼 해제 후 그 상태 반환. 힙 실패는 `+0x427 → 0xC0000017` 으로 `CsrPortHandle` 닫고 버퍼 해제. → kernelbase `+0x2e0 JS 0x18004ec6b` = **DllMain FALSE**.
- **`CsrpClientConnectToServer`** (ntdll `0xdb990`) 는 **정상일 때 안 탄다**. 탈 조건은 응답 상태 `[RSP+0x70] < 0`. 그 상태는 csrss 의 `CsrpHandleInlineUserConnect` (`msg+0x48 == 1` 이면 서버 DLL 의 connect 루틴을 8바이트 연결 정보로 불러 반환값을 `msg+0x4c` 에 씀, `.c` 기준) 가 정하고, 그 루틴은 basesrv `BaseClientConnectRoutine` (rva `0x5d70`): `[R8] == 8` 이면 `+0x6 ~ +0x10` 연결 정보를 `[세션객체+0x90]` 의 첫 칸에 저장하고 `+0x13 XOR EAX,EAX` **0 반환**, 크기가 8 이 아니면 `+0x1e` `0xC000000D`. kernelbase 가 크기 8 을 주므로 **0 → `CsrpClientConnectToServer` 안 탐** `[정적, csrss 쪽 상수 의존 → 동적 확인 Q84]`. (탔다면: `CsrAllocateCaptureBuffer` → `CsrAllocateMessagePointer` → `CsrClientCallServer` = `NtAlpcSendWaitReceivePort` → `CsrFreeCaptureBuffer`.)
- **쓰는 전역 (ntdll 모듈 rva)**: `CsrInitOnceDone` `0x1d4df9` · `CsrClientProcess` `0x1d4df8` · `CsrHeap` `0x1d4e38` · `CsrPortName` `0x1d4e00` (Length·MaximumLength·Buffer) · `CsrPortHandle` `0x1d4e20` · `CsrProcessId` `0x1d4e10` · `CsrPortMemoryRemoteDelta` `0x1d4e18` · `CsrPortHeap` `0x1d4e28` · `CsrPortBaseTag` `0x1d4e30`. **`CsrServerApiRoutine` (`0x1d4df0`) 는 이 길에서 안 쓴다** (서버 길에서만). 이것이 run44 사진의 "`Csr*` 8개" (`CsrPortHandle` `0x74` = ALPC 포트 등) 와 이어진다: `[관찰]` P1a 전부 0 → P1b 값.
- **돌아온 정보를 kernelbase 가 쓰는 곳** (`_KernelBaseBaseDllInitialize`): 응답이 올 때 `BasepInitializationStage = 0xc8` (`+0x2e6 ~ +0x2eb`) 뒤,
  - **`BaseStaticServerData` (kernelbase `0x3aedb8`) ← `ReadOnlyStaticServerData[1]` (`[PEB+0x98]+8`, `+0x2fb ~ +0x30b`) `− PEB+0x380 (+0x30f) + PEB+0x88 (+0x31f)`**, 저장 `+0x32b` → **예, `BaseStaticServerData`** 이다.
  - `KernelBaseGlobalData+0x4` (1바이트) ← `CalledFromServer != 0` (`+0x326 CMP [RSP+0x30],DIL`, `+0x337 SETNZ`) = 0.
  - `BaseRCNumber` (`0x3aedc0`, word) ← `[BaseStaticServerData+0x38]` (`+0x332`, `+0x342`).
  - `KernelBaseGlobalData+0x8` (16B) ← `[BaseStaticServerData+0]` (Windows 디렉터리 문자열 `+0x33e ~ +0x349`), 그 Buffer 가 NULL 이 아니면 `+0x10` 에 **csrss 쪽 포인터를 내 뷰로 옮겨** 더함 (`+0x363 ~ +0x3d3`); `+0x18` (16B) ← `[BaseStaticServerData+0x10]`, `+0x20` 에도 같은 옮김 (`+0x3da ~ +0x469`). (`+0x686`/`+0x6a4` 는 Buffer 가 NULL 일 때 0 으로 씀.) 이 칸들은 run67 의 이웃 칸 표와 같다.
- **동적 (Q84)**: 응답 블록 `[+0]` (csrss 뷰 주소)·`[+8]`·`[+0x10]` (csrss 의 `CsrProcessId`) 의 실제 값, `CsrPortHandle` 숫자, `NtConnectPort` 결과. P1b 사진 값 (`ReadOnlySharedMemoryBase = 0x7ff4ecf70000`, `ReadOnlyStaticServerData = 0x7ff4ecf70750`, `CsrServerReadOnlySharedMemoryBase = 0x7ff5575f0000`) 은 이 계산과 맞는다 (`0x...f70750 − 0x...f70000 = 0x750`).

### 5. `RtlSetUnhandledExceptionFilter` (ntdll rva `0xe95f0`, 21B)

- 부르는 곳 `_KernelBaseBaseDllInitialize +0x4ab / +0x4b2`. 넘긴 값: `RCX = 0x18010ed90` (`+0x4a4 LEA RCX,[0x18010ed90]`) = **kernelbase `UnhandledExceptionFilter`** (rva `0x10ed90`, 1402B, PDB 이름).
- 본문: `+0x4/+0x9` `RtlEncodePointer(RCX)` → `+0x9` **`RtlpUnhandledExceptionFilter` (ntdll `0x1d28e8`) ← 인코딩된 값**. 반환: 코드가 `RAX` (인코딩된 값) 를 그대로 둔 채 `RET` — 호출한 쪽은 안 본다.
- **인코딩 [정적]**: `RtlEncodePointer` (ntdll `0xe9610`) = `ROR(포인터 ^ CookieValue, CookieValue & 0x3f)` (`CookieValue` = ntdll `0x1d2408`, 32비트). `CookieValue` 가 0 이면 `NtQueryInformationProcess(-1, 0x24, …)` (`+0x43`) 인데 **run59: `LdrpInitializeProcess +0x12f` 가 이미 채움** → 여기선 syscall 없음.
- 이어서 `+0x4b7 XOR ECX,ECX` / `+0x4b9 / +0x4c0` `RtlEncodePointer(0)` → `+0x4ce` **`BasepCurrentTopLevelFilter` (kernelbase `0x3aeda8`) ← 인코딩된 NULL**. 둘 다 값은 프로세스 쿠키·kernelbase 베이스 주소에 달려 동적 (식만 정적).

### 6. `BaseNlsDllInitialize` (kernelbase rva `0xea9b0`, 122B)

- 부르는 곳 `_KernelBaseBaseDllInitialize +0x4e1 / +0x4e6`: `EDX = EBX` (reason), `R8 = BaseStaticServerData` (`+0x4c5`), 앞 `+0x4d5 ~ +0x4da` 에서 `BasepInitializationStage = 0x190`. 실패 (`AL == 0`, `+0x4e8 JZ 0x18004ec6b`) = DllMain FALSE.
- attach (`+0x4 CMP EDX,1`): **`gpServerNlsUserInfo` (kernelbase `0x3afc88`) ← `R8 + 0x178`** (`+0x9`, `+0x10`; csrss 가 준 NLS 사용자 정보 칸의 내 뷰 주소). `+0x17 / +0x1c NlsProcessInitialize()` 가 0 이 아니면 `+0x62` 로 AL=0 반환. 성공이면 `g_pSortHash` (`0x3af420`, 0x400B)·`g_pSortGuidHash` (`0x3af820`, 0x400B)·`g_pSortVersionDlls` (`0x3afc20`, 0x50B) 를 `memset` 0 (`+0x2f/+0x34`, `+0x43/+0x48`, `+0x55/+0x5a`), `AL = 1`.
- **`NlsProcessInitialize`** (kernelbase `0xeaa34`, `IsNlsProcessInitialized` `0x3aacec` 가 0 일 때만): `+0x1a/+0x1f AllocTables` (힙 블록 2개 `RtlAllocateHeap` + `memset`) → `+0x30/+0x35 SetupMainNlsFiles` → `+0x3b/+0x40 SetupAnsiOemCodeHashNodes` → `BasepIsSecureProcess` (`0x3aee50`) 가 0 이면 `gTblPtrsLock` 잡고 `+0x5f/+0x64` **`gpInvLocHashN ← MakeLocHashNode(0x7f)`** (불변 로케일), 풀고 → `gNlsProcessLocalCache` (`0x3afca0`/`0x3afca8`/`0x3afcb0`) 0, word `[0x3b0334]` 0·`[0x3b0336]` 1, `IsNlsProcessInitialized ← 1` (`+0xab`).
- **syscall 줄기 (test.exe)** — run57 과 **이어진다**:

| 줄 | 호출 | syscall | 조건 |
|---|---|---|---|
| `SetupMainNlsFiles +0x36 CMP PEB.UseCaseMapping (PEB+0x350),0` / `+0x4a ECX=0xe`, `+0x4f / +0x56` | `NtGetNlsSectionPtr(종류 0xe, 0, 0, &뷰)` | **1번** | `UseCaseMapping ≠ 0` — `[관찰]` run44 P1a `0x6a5` → 탄다. 결과 뷰 → `TblPtrs+0xc0`/`+0xc8` (`+0x68`, `+0x7b`). run57 `RtlpInitUppercaseTables` 와 **같은 호출, 같은 섹션, 새 뷰** |
| `SetupMainNlsFiles +0x9d / +0xa4` | ntdll `RtlGetLocaleFileMappingAddress(&뷰, &gSystemLocale (0x3aace8), 0)` → `+0x29 / +0x2e` `NtInitializeNlsFiles` | **1번 (처음 불리면)** | ntdll 캐시 `gBaseAddress` (`0x1ce7b0`) 가 0 일 때. ntdll 초기화 줄기 지도엔 이 함수 호출이 없다 (지도 `NtInitializeNlsFiles` 의 caller 는 `RtlpLoadNlsData` 만, 다른 길) → 여기서 **처음** `[정적, 지도 기준]`. 캐시가 이미 있으면 syscall 없이 `gSystemLocale` 만 돌려줌 (`+0x58 ~ +0x63`) |
| `SetupAnsiOemCodeHashNodes +0x3e / +0x43`, `+0x61 / +0x66` | `GetCPHashNode(PEB.ActiveCodePage 0x34c, &gpACPHashN)`, `GetCPHashNode(PEB.OemCodePage 0x34e, &gpOEMCPHashN)` → 캐시 못 찾으면 `GetCPHashNode +0x2d3 / +0x2da` `NtGetNlsSectionPtr(종류 0xb, 코드페이지)` | **1~2번** | ACP = OEM = `0x3b5` (949) `[관찰]`. 둘째는 첫째가 넣은 노드를 찾을 것이다 `[추측]`. UTF-8 (`0xfde9`) 이면 둘 다 건너뜀 (`+0x31`, `+0x54`) |
| `NlsProcessInitialize +0x5f / +0x64` `MakeLocHashNode(0x7f)` | `GetWindowsLocaleData` → (`CMP DI,0x7f`, `+0x28`) `MakeLocHashNodeFromSystemLocale` | **모름** | 그 아래에 `GetNamedLocaleHashNode` → `GetCurrentNlsCache` (레지스트리 `NtOpenKey`·`NtQueryValueKey`·`NtCreateKey`·`NtNotifyChangeKey`, csrss 호출 `CsrClientCallServer`) 까지 이어지는 갈래가 있다. 0x7f 가 거기를 타는지는 안 따라감 → 동적 |

  - 쓰는 NLS 전역 (일부): `gAnsiCodePage` (`0x3a92ac`) ← 949, `gOemCodePage` (`0x3a92a8`) ← 949, `gMacCodePage` ← 0 (`SetupAnsiOemCodeHashNodes +0x47`, `+0x6a`, `+0x70`), `KernelBaseGlobalData+0x60 ~ +0x90` (`SetupMainNlsFiles +0x111 ~ +0x164`, run67 "NLS 파일 정보" 칸), `TblPtrs+0x50`/`+0x58`.
- 비밀 프로세스 (`BasepIsSecureProcess ≠ 0`) 면 `SetupMainNlsFiles +0x174` 에서 `gSystemLocale = 0x409` 로 끝, 나머지 건너뜀.

### 7. `RtlInitializeCriticalSection` (ntdll `0xd65c0`, 335B) · `RtlInitializeCriticalSectionAndSpinCount` (`0xd4900`, 378B)

- **`+0x508` 이 초기화하는 락**: `_KernelBaseBaseDllInitialize +0x4f3 LEA RCX,[0x1803aef70]` / `+0x501 / +0x508 RtlInitializeCriticalSection` → **kernelbase `KernelBaseGlobalData+0xb0` (rva `0x3aef70`)**, 크기 `0x28` (= 바로 뒤 `BaseDllHandle` `0x3aef98` 까지). 쓰는 곳은 이 줄 하나, 쓰는 쪽 (잡는 곳) = `GetComputerNameExW` 2곳, 지우는 곳 = `BasepUnInitComputerNameCache +0x3d` (`_refs/KernelBaseGlobalData.tsv` target `+0xb0`) → **컴퓨터 이름 캐시 락**. 실패 (`+0x50f JS`) = DllMain FALSE (실제론 안 실패).
- **`RtlInitializeCriticalSection(RCX = cs)`** 가 칸에 쓰는 값 (`_RTL_CRITICAL_SECTION` 0x28바이트, PDB 타입):

| 칸 | 값 | 줄 |
|---|---|---|
| `+0x08` `LockCount` | `-1` | `+0x20` |
| `+0x0c` `RecursionCount` | 0 | `+0x27` |
| `+0x10` `OwningThread` | 0 | `+0x2d` |
| `+0x18` `LockSemaphore` | 0 | `+0x31` |
| `+0x20` `SpinCount` | **`0x20007d0`** (= 2000 \| `0x2000000`) — 단, `PEB.NumberOfProcessors (+0xb8) ≤ 1` 이면 0 (`+0x43 CMP …,1`, `+0x4a CMOVBE`). 이 VM `NumberOfProcessors = 2` `[관찰]` → `0x20007d0` | `+0x35`, `+0x4d` |
| `+0x00` `DebugInfo` | **`-1`** (디버그 정보 블록 안 만든 표시) | `+0x51` |

  - **디버그 정보는 만들지 않는다**: `+0x58 CMP RtlpForceCSDebugInfoCreation (0x1d4008),0` 이 0 이면 `+0x5f JZ 0x1800d6637` 로 건너뜀. 이 바이트는 `LdrpInitializeProcess +0x999`/`+0x9b5`/`+0x1a10` 만 1 로 쓰는데, 조건이 `NtGlobalFlag & 0x1000` (스택 추적 DB) 이나 `LdrpShouldCreateStackTraceDb` 이거나 `PEB.AppCompatFlags (+0x2c8)` 비트 22. `[관찰]` `NtGlobalFlag = 0`, `AppCompatFlags = 0` → 0. 만들게 되면 (`+0x64 / +0x69 RtlpAddDebugInfoToCriticalSection`): 먼저 **정적 SList `RtlCriticalSectionDebugSList` (`0x1cde90`) 에서 pop** (`+0xb`/`+0x12`), 비었으면 **프로세스 힙 `RtlAllocateHeap(PEB.ProcessHeap, 0, 0x30)`** (`+0x3b / +0x40`), `CMPXCHG.LOCK [cs],DebugInfo` 로 `DebugInfo` 에 넣고 `RtlCriticalSectionList` (`0x1cc8d0`) 의 이중 연결 리스트에 `RtlCriticalSectionLock` 아래 끼운다. 못 만들면 `SpinCount |= 0x1000000` (`+0x6f`) 로 "디버그 정보 없음" 표시. → **test.exe 기본 길에서 `RtlCriticalSectionList` 에 끼지 않는다.**
  - ETW 추적: `+0x80 ~ +0x94`: `SharedUserData+0x382` (`UserModeGlobalLogger` 칸) 이 0 이 아니고 `PEB.TracingFlags (+0x378)` 비트 1 (`CritSecTracingEnabled`) 이 켜졌을 때만 `+0x146 NtTraceEvent`. `[관찰]` `UserModeGlobalLogger` 0 (sast/57) → **안 탐**. 반환 `EAX = 0`.
- **`RtlInitializeCriticalSectionAndSpinCount(cs, 스핀)`**: 같은 일 + `EDX & 0xffffff` (`+0x2f`), `NumberOfProcessors ≤ 1` 이면 스핀 0, `EDX == 0` 이면 기본 `0x20007d0` (`+0xde`), 아니면 `EDX` 값을 `[cs+0x20]` 에 (`+0x69`). `DebugInfo = -1` (`+0x78`), 디버그 정보·ETW 갈래도 같음 (`+0x84`, `+0x171`).
  - **부르는 곳**: kernelbase DllMain **래퍼** `KernelBaseBaseDllInitialize +0x49 ~ +0x5c`: attach (`+0x17 CMP EDX,1`, `+0x1a JZ 0x180005219`) 에서 `RCX = &_CrtLock_Exit (0x3af3e0)`, `EDX = 0xfa0` (`+0x49`, `+0x4e`) → `[cs+0x20] = 0xfa0` (스핀 4000, CPU 2개). 이 호출은 `_KernelBaseBaseDllInitialize` 보다 **먼저** 돈다 (`+0x44 JMP 0x18004e6c0` 가 마지막 꼬리 점프). detach 에서는 `+0x68 ~ +0x6f` `RtlDeleteCriticalSection`.
- syscall: **두 함수 모두 직접 없음**.

### 8. `ConsoleInitialize` (kernelbase rva `0xbeb60`, 585B)

- 부르는 곳 `_KernelBaseBaseDllInitialize +0x521 / +0x526` (`BasepInitializationStage = 0x258`). 인자 없음. 반환 AL: 0 이면 DllMain FALSE (`+0x528 JZ 0x18004e7b6` = `+0xf6`).
- **test.exe (Subsystem 3, `ConsoleHandle = 4`, `ConsoleFlags = 0`, `StandardInput/Output/Error = 0x68/0x6c/0x70` — `[관찰]` run44 P1a)** 가 타는 갈래:

| 줄 (call / ret 또는 줄) | 하는 일 | 쓰는 칸 |
|---|---|---|
| `+0x42 / +0x49` | `RtlInitializeCriticalSection(&ConsoleStateLock)` (kernelbase `0x3af160`) → 위 7번 | `ConsoleStateLock` 0x28B |
| `+0x63 / +0x6a`, `+0x7b CMP [NT+0x5c],3`, `+0x89` | `RtlImageNtHeader(PEB.ImageBase)`; `Subsystem == 3` 이면 `AL=1`, 아니면 0 | **`ConsoleIsConsoleApplication` (`0x3af188`)** |
| `+0x8f / +0x94` | `ConsoleRationalizeAllocationOptionsWithPeb`: `ConsoleHandle` 가 0·-2·-3·-1 이면 전역 `0x3aa9f8` 에 1·2·`\|-1` 을 쓰고, `WindowFlags & 1` (STARTF_USESHOWWINDOW) 이면 `0x3aa9fc`·`0x3aaa00` (word) ← `ShowWindowFlags` | test.exe: `ConsoleHandle = 4`, `WindowFlags = 0` → **셋 다 안 씀** `[정적 + 관찰]` |
| `+0x94 / +0x99` | `InitializeCtrlHandling`: `HandlerListLength`·`AllocatedHandlerListLength` ← 1 (`+0x12`, `+0x1e` 근처), `HandlerList` (`0x3ab720`) 가 NULL 이면 `&SingleHandler`, **`SingleHandler` (`0x3af0f0`) ← `RtlEncodePointer(kernelbase+0x1235c0)`** (`+0x38 / +0x3f`; 기본 콘솔 제어 핸들러) | 위 전역 |
| `+0x99 / +0x9e` | `InitExeName`: `PEB.Ldr` 첫 모듈 (exe) 의 `BaseDllName` 을 복사 (`memcpy` `+0x5c`, 최대 0x200B) | `ExeNameBuffer` (`0x3af1a0`), `ExeNameLength` (`0x3af3a0`, word), `ExeNameInitialized` (`0x3ab714`) |
| `+0x9e CMP [params+0x408],0` / `+0xaf`, `+0xb2` | `ProcessParameters.ProcessGroupId` 가 0 이면 `TEB.ClientId.UniqueProcess` (`[GS:30]+0x40`) 로 채움 | 매개변수 칸 (`+0x408`) |
| `+0xb8 / +0xbd` | **`ConsoleShouldAllocateConsole()`**: 콘솔 앱이고 `RtlQueryActivationContextApplicationSettings(NULL, NULL, L"http://schemas.microsoft.com/SMI/2024/WindowsSettings", L"consoleAllocationPolicy", …)` 가 **실패 (음수)** 하면 TRUE (`+0x54 JS` → `+0x92 MOV AL,1`). test.exe 는 매니페스트 없음 (sast/62: 자원 디렉터리 0) → 실패 `[정적 + 관찰: 아래]`. 값이 `d`/`D` (detached) 인 경우만 `ConsoleHandle` 를 봐서 달라짐 | 없음 |
| `+0xbf JNZ 0x1800becc6` (= `+0x166`) | TRUE 면 아래. FALSE 면 `+0xc5` 갈래 (`ConsoleHandle ← 0`, 표준 핸들을 `ConsoleCloseIfConsoleHandle` 로 닫고 `ConsoleConnectionState` 0) — **안 탐** | |
| `+0x166 TEST [params+0x18],4` | `ConsoleFlags & 4` 이면 표준 핸들 셋을 `ConsoleCloseIfConsoleHandle` (`+0x20f ~ +0x225`). `ConsoleFlags = 0` → 안 탐 | |
| `+0x170 MOV RDX,[params+0x10]` … `+0x185` | `ConsoleHandle` 가 0·-2·-3 이면 `+0x142` **`ConsoleAllocate`** (새 콘솔 만들기, conhost 띄움 — **안 탐**), -1 이면 `+0x1c6` 로 바로 commit. **그 밖 (실제 핸들, 여기선 4)** 이면 아래 | |
| `+0x19a / +0x19f` | **`ConsoleCreateConnectionObject(&새핸들 [RBP+0x28], RDX = ConsoleHandle(4), R8 = NULL, R9 = 0, word 0)`** — 안에서 `ConsoleInitializeServerData` 가 **0x53c바이트** 연결 데이터를 채우고 (읽는 것: 매개변수 `WindowFlags +0xa4`·`ShowWindowFlags +0xa8`·`StartingX/Y +0x88/+0x8c`·`CountX/Y +0x90/+0x94`·`CountCharsX/Y +0x98/+0x9c`·`FillAttribute +0xa0`·`WindowTitle +0xb0`·`DesktopInfo +0xc0`·`CurrentDirectory +0x38`·`ProcessGroupId +0x408`, 위 `ConsoleIsConsoleApplication`, 전역 `0x3aa9f8`/`0x3aa9fc`, exe 이름), 힙 `RtlAllocateHeap(PEB.ProcessHeap, 0, 0x54b)` 에 확장 속성 (EA) 으로 싸서 **`NtCreateFile` (`+0x1c1 / +0x1c8`)**: 접근 `0x12019f`, 기준 디렉터리 = `ConsoleHandle`(4), 이름 **`\Connect`** (kernelbase `0x2a6250`; 기준 핸들이 NULL 이면 `\Device\ConDrv\Connect` `0x2a62a8`), 공유 7, 열기 방식 2 (FILE_CREATE), 옵션 `0x20` (동기), EA 이름 `server`. 끝나면 힙 블록 해제 (`+0x1e1`) | 새 핸들 → `[RBP+0x28]` |
| `+0x1d4 CMP EAX,0xc0000022` … | `NtCreateFile` 이 ACCESS_DENIED 이면 `ConsoleIsCallerInLowbox` 로 로우박스 확인 후 `NtClose(ConsoleHandle)` 하고 `ConsoleAllocate` — **안 탐** | |
| `+0x1a3 ~ +0x1c0`, `+0x1b9 TEST [params+0x18],2` | 상태 구조체 `[RBP-0x40]` 에 새 핸들 (`+8`) 과 옛 핸들 (`+0x10` = 4) 을 담음. `ConsoleFlags & 2` 이면 `+0x22f ConsoleSanitizeStandardIoObjects` — 0 이라 **안 탐** | |
| `+0x1ca / +0x1cf` | **`ConsoleCommitState(상태)`** (run51 이미 분석: `+0x3c ~ +0x40` **`params.ConsoleHandle (+0x10)` ← 새 핸들**, `+0x8a / +0x91` `NtDeviceIoControlFile(새핸들, IOCTL 0x500023, 출력 8B = conhost PID)`, `+0xe9 / +0xf0` `NtSetInformationProcess(-1, 0x31, &(PID\|1), 8)` (`OwnerProcessId` 둘째 쓰기), `+0xf5 / +0xfa` `InitializeCtrlHandling` 한 번 더, `+0xfa / +0xff` `SetTEBLangID`). 상태를 `ConsoleConnectionState` (`0x3af120`, 0x38B) 에 복사 (`+0x96 ~ +0xbb`) | `ConsoleConnectionState`, params `ConsoleHandle` |
| `+0x126` | `AL = 1` (ESI) 반환 | |

- **syscall (ConsoleInitialize 전체, test.exe)**: `NtCreateFile` 1 · `NtDeviceIoControlFile(0x500023)` 1 · `NtSetInformationProcess(0x31)` 1 = **3번** (run51 의 둘은 그대로, `NtCreateFile` 은 이번에 닿음). `RtlQueryActivationContextApplicationSettings` 안은 안 읽음 (syscall 없을 것 `[추측]`).
- `[관찰]` 일치: run44 P1b 에서 `ConsoleHandle` `4 → 0x60` — 이 갈래에서만 나오는 변화. 표준 핸들 셋은 안 바뀐다 (`ConsoleFlags & 2` 가 0 이라서) `[정적]`.
- 동적 필요: 새 핸들 숫자 (`0x60`), conhost PID, `NtCreateFile` 결과, `ConsoleShouldAllocateConsole` 가 정말 TRUE 인지 (위 일치로 간접 확인).

### 9. `_SYSTEM_BASIC_INFORMATION` (클래스 0) 칸 모양 — ntoskrnl `ExpGetSystemBasicInformation` (rva `0x457484`, 264B)

- 부르는 곳: `ExpQuerySystemInformation +0x737 / +0x73c` (`+0x503 TEST EBX,EBX / +0x507 JZ 0x140a07523` = `+0x72b`; 클래스 `0x72` 도 `+0x2ea CMP EBX,0x72` / `+0x2f3 JZ` 로 같은 곳). **크기 검사**: `+0x72b CMP R14D,ECX` (`ECX = 0x40`, `+0x217 MOV ECX,0x40`) / `+0x72e JNZ 0x140a07762` → 길이가 `0x40` 이 아니면 `STATUS_INFO_LENGTH_MISMATCH` **`0xC0000004`** (`+0x33b`; ReturnLength 포인터가 있으면 `*RL = 0x40` 먼저, 조각 `FUN_140a08af2`). 성공이면 반환 길이 `0x40` (`+0x740`), 반환 상태 `STATUS_SUCCESS` (`ExpGetSystemBasicInformation +0x103 MOV EAX,R11D`, R11D = 0).
- 사용자 버퍼에 직접 쓴다 (`$filt$0` = `ExSystemExceptionFilter`, 예외 때만). 안 쓰는 칸 (`+0x1c`, `+0x39 ~ +0x3f`) 은 **호출한 쪽 값 그대로** (kernelbase/kernel32 의 `SysInfo` 는 전역이라 0).
- **칸 표** (이름은 공개 문서·NDK 이름 `[추측-이름]`; 오프셋·크기·값 출처는 `.asm`):

| 오프셋 | 크기 | 뜻 `[추측-이름]` | 쓰는 줄 | 값 출처 | 정적? |
|---|---|---|---|---|---|
| `+0x00` | 4 | Reserved | `+0x32` | 0 (`R11D`) | **상수 0** |
| `+0x04` | 4 | TimerResolution (100ns 단위 시계 간격) | `+0x35` / `+0x3b` | nt 전역 **`KeMaximumIncrement`** (`0xfc6ae0`) | **동적 (부팅)**. `[관찰]` `SharedUserData.TickCountMultiplier = 0xfa00000` 에서 거꾸로 계산하면 156250 (`0x2625a`) `[추측]` |
| `+0x08` | 4 | PageSize | `+0x3f` | 상수 `0x1000` | **상수** |
| `+0x0c` | 4 | NumberOfPhysicalPages | `+0xb4` | `MiState.Partitions.Partition[프로세스의 PartitionId]` → `_MI_PARTITION.Vp.NumberOfPhysicalPages` (`+0x4850`), 32비트를 넘으면 `0xffffffff` | **동적 (부팅)**. `[관찰]` `SharedUserData.NumberOfPhysicalPages = 0x1ffe86` (같은 값일 것 `[추측]`) |
| `+0x10` | 4 | LowestPhysicalPageNumber | `+0xd8` | 같은 파티션 `Vp.LowestPhysicalPage` (`+0x4840`), 32비트 한도 | **동적 (부팅)** |
| `+0x14` | 4 | HighestPhysicalPageNumber | `+0xfa` | 같은 파티션 `Vp.HighestPhysicalPage` (`+0x4848`), 32비트 한도 | **동적 (부팅)** |
| `+0x18` | 4 | AllocationGranularity | `+0x4c` | 상수 `0x10000` | **상수** |
| `+0x1c` | 4 | (정렬 틈) | — | 안 씀 | — |
| `+0x20` | 8 | MinimumUserModeAddress | `+0x50` | 상수 `0x10000` | **상수** |
| `+0x28` | 8 | MaximumUserModeAddress | `+0x5e` | 상수 `0x7ffffffeffff` | **상수** |
| `+0x30` | 8 | ActiveProcessorsAffinityMask | `+0x7d` | `KeActiveProcessors` (`0xfc7160`) 의 비트맵 `[현재 스레드의 AffinityPrimaryGroup (KTHREAD+0x248)]` (`+0x6d` `KeActiveProcessors+0x8`). 그룹 번호 ≥ `Count` 이면 0 | **동적 (부팅)**. 이 VM CPU 2개 `[관찰: ActiveProcessorCount 2]` → `3` 일 것 `[추측]` |
| `+0x38` | 1 | NumberOfProcessors | `+0x8a` | `POPCNT(위 마스크)` (이 **그룹**의 CPU 수) | **동적 (부팅)**. 2 일 것 `[추측]` |
| `+0x39 ~ +0x3f` | 7 | (안 씀) | — | — | — |

  - "현재 프로세스의 파티션" 은 `GS:[0x188]` (`KPCR.CurrentThread`) → `[+0xb8]` (`ApcState.Process`) → `EPROCESS.Vm.Instance.PartitionId` (`+0x4ae`, word). 파티션 표 = `MiState` (`0xe2cb80`) `+0x3500 Partitions` `+0x48 Partition` (`_MI_PARTITION**`) = `0xe300c8` (PDB 이름 없음, `+0x8e MOV RAX,[0x140e300c8]`).
- **kernelbase 가 `SysInfo` 에서 읽는 칸** (`_refs/SysInfo.tsv` 29줄, 직접 참조만 — 이 전역의 주소를 레지스터로 넘기는 줄은 `+0x22d` 한 곳뿐이라 빠진 읽기는 없다):

| 칸 | 읽는 함수 (kernelbase) |
|---|---|
| `+0x8` PageSize | `WriteProcessMemory +0x1de` · `DiscardVirtualMemory +0x58/+0x10c/+0x1d0` · `GlobalMemoryStatusEx +0x8c/+0x1ca/+0x1ef` (×100 곱 `+0x8c`) · `QueryAppMemoryInformation +0xf8` · **`HeapCreate +0x6`** · `CreateFiberEx +0xb4/+0x1e0` · `BaseInitializeFiberContext +0x49` · `SetThreadStackGuarantee +0x47` · `GetSystemFileCacheSize +0x50/+0x68` · `OfferVirtualMemoryInternal +0x69/+0x116/+0x1e6` · `ReclaimVirtualMemory +0x38/+0x11a` |
| `+0x18` AllocationGranularity | `DiscardVirtualMemory +0x21d` · **`VirtualAlloc +0x13`** · `VirtualAllocExNuma +0x13` · `VirtualAllocEx +0x13` · `CreateFiberEx +0xaa` · `OfferVirtualMemoryInternal +0x204` |
| `+0x20` MinimumUserModeAddress | `GlobalMemoryStatusEx +0x155` (`SUB RCX,[SysInfo+0x20]`) |
| `+0x28` MaximumUserModeAddress | `GlobalMemoryStatusEx +0x149` (`MOV RCX,[SysInfo+0x28]`) |

  (`GlobalMemoryStatusEx` 는 `+0x149` 에서 `+0x28`, `+0x155` 에서 `+0x20` 을 읽어 가상 메모리 총량 = 최대−최소 를 만든다 `[추측: 쓰임]`.) kernel32 `SysInfo` (rva `0xc2260`) 는 `+0x8` 만: `IsBadReadPtr +0x4` · `GlobalMemoryStatus +0xe1` · `IsBadWritePtr +0x7`. ntdll 은 이것과 따로 `RtlpCreateHeap` 안에서 **처음 한 번** 클래스 0 을 불러 `+0x28` 을 `0x1ce6e8` 에 캐시한다 (`DAT_1801ce6e8 == 0` 일 때만, `.c` 기준; `+0x10000` 은 `0x1ce6f0` 상수).
- **PE_emulator 에 할 말**: 지금 `+0x18`·`+0x28`·`+0x38` 만 채우면, kernelbase 의 `HeapCreate` (`+0x8` 읽음) 와 `GlobalMemoryStatusEx` (`+0x8`·`+0x20`·`+0x28`) 등이 0 을 읽는다. 최소 **`+0x8 = 0x1000`, `+0x20 = 0x10000`** 도 필요하다 (상수라 쉽다). `+0xc`·`+0x10`·`+0x14`·`+0x30`·`+0x38` 은 kernelbase/kernel32 가 안 읽는다 (그래도 채워 두는 게 정직하다).

### 10. kernel32 `_BaseDllInitialize` (rva `0x2c610`) 안

공통: attach (`+0x5f CMP EAX,1`, `+0x62 JNZ 0x18002c82c`) 의 앞부분. `RBX = PEB`, `R14 = PEB.ProcessParameters`.

| 줄 | 함수 | 인자 · 쓰는 칸 · syscall | test.exe |
|---|---|---|---|
| `+0x68`, `+0x6c / +0x73` | ntdll **`RtlImageNtHeader`** (rva `0xddaf0`, 43B) | `RCX = PEB.ImageBaseAddress` → `RtlImageNtHeaderEx(Flags 1, 기준, 0, &out)` (`+0x18 LEA ECX,[R8+1]`, `+0x1c`): `'MZ'` (0x5a4d, `RtlImageNtHeaderEx +0x51`) · `e_lfanew < 0x10000000` · `'PE\0\0'` (0x4550, `+0x83`) 확인, 틀리면 `0xC000007B` → NULL 반환 (`+0x8c`). 쓰는 칸·syscall **없음**. NULL 이면 `+0x7e JZ 0x18002c9a1` (= `+0x391`) **DllMain FALSE** | 탄다 (NT 헤더 포인터를 RDI 에 보관) |
| `+0x84`, `+0x8b`, `+0x92 / +0x99` | ntdll **`RtlSetThreadPoolStartFunc`** (PDB rva `0x10bc80`, 요청서가 말한 `_index.tsv` 이름 `CsrIdentifyAlertableThread`) | `RCX = kernel32+0x69a90` (`BaseCreateThreadPoolThread`), `RDX = kernel32+0x69b70` (`BaseExitThreadPoolThread`). 본문 **`XOR EAX,EAX; RET` (`+0x0`, `+0x2`) = 아무것도 안 하고 0 반환.** 같은 주소 PDB 이름 **22개** (`CsrpHeapCommitRoutine`·`RtlpFcNoopCallback`·`WinSqm*`·`RtlAddRefMemoryStream` …, ICF) — 정말 "아무것도 안 함" 맞다. 쓰는 칸·syscall 없음 | 탄다 (효과 없음) |
| `+0xe9 / +0xf0` | ntdll **`RtlSetProtectedPolicy`** (rva `0xf9d80`, 740B) | `RCX = &GUID_BasepAllowResourceConversion (kernel32 0x91450)` (`+0xdf`), `RDX = 1` (`+0xe6 MOV RDX,RSI`), `R8 = 0`. 본문: `LdrEnsureMrdataHeapExists`, `RtlpProtectedPoliciesSRWLock` (`0x1ce848`), 정렬된 표 `RtlpProtectedPolicies` (`0x1e7080`, 24바이트 항목 {GUID 16B, 값 8B}, `bsearch` `+0xb9` / 없으면 mrdata 힙에 16개씩 `RtlAllocateHeap` `+0x11b`, `LdrProtectMrdata` 로 쓰기 풀기, `qsort` `+0x1b5`), 이미 있으면 값만 교체 | **안 탄다**: `+0xc7 MOVZX EAX,[NT+0x5c]` (Subsystem 3) → `+0xcf SUB AX,SI` → `+0xd2 CMP AX,R9W`, `+0xd6 JBE 0x18002c89d` (= `+0x28d`) → `+0x28d CMP word [NT+0x48],6` (MajorSubsystemVersion; test.exe **6** `objdump -p`) / `+0x292 JNC 0x18002c70b` (= `+0xfb`) 로 **건너뜀**. (Subsystem 이 1~3 밖이거나 버전 < 6 이면 `+0x298 JMP +0xdc` 로 호출) |
| `+0x1f9 / +0x200`, `+0x205 TEST AL,0x10`, `+0x207 JNZ 0x18002c960` (= `+0x350`) | ntdll `RtlGetSuiteMask` | `SharedUserData+0x2d0` (`SuiteMask`) — `[관찰]` `0x110` (비트 `0x10` 켜짐) | `+0x350` 으로 감 |
| `+0x364 / +0x369`, `+0x36b JZ 0x18002c81d` (= `+0x20d`) | kernel32 **`InitializeRegTermsrvFpns`** (rva `0x2cbc0`, 415B) | `RCX = &바이트 [RSP+0x30]`, `RDX = &dword [RSP+0x38]` (둘 다 0). 본문: `+0x17 / +0x1c` `IsTerminalServerCompatible()` → 아래 | **곧바로 FALSE**: `IsTerminalServerCompatible` 은 `RtlImageNtHeader` 로 NT 헤더를 얻고 `DllCharacteristics` (`[NT+0x5e]`, `+0x27 CMP`, `+0x2b SETL`) 의 **최상위 비트 (`TERMINAL_SERVER_AWARE` 0x8000)** 를 돌려준다. test.exe `DllCharacteristics = 0x8160` (비트 15 켜짐) → 1 → `InitializeRegTermsrvFpns +0x1e JZ 0x18002cbf3` 안 탐 → `+0x20 XOR AL,AL` 반환 0. 쓰는 칸·syscall **없음**. (`pfnTermsrvConvertSysRootToUserDir` 계열 함수 포인터 9개는 NULL 그대로) |
| (위가 TS 모르는 exe 일 때만) | `IsRegTSAppCompatEnabled` → `NtOpenKey("\Registry\Machine\System\CurrentControlSet\Control\Terminal Server")` `+0xb9`, `NtQueryValueKey("TSAppCompat")`·`("TSUserEnabled")` `+0x101`·`+0x14b`, `NtClose` 와 `GetCompatFlags`·`IsSystemLUID`, `LoadLibraryExW(0x180092b28)`+`GetProcAddress` 9개 (`+0x78`…) | 레지스트리 syscall | **안 탐** |
| `+0x380 / +0x387` | kernelbase **`GetRegistryExtensionFlags`** (rva `0x13a540`, 366B) | `ECX = [RSP+0x38]`(호환 플래그). `NtOpenKey("\Registry\Machine\Software\Microsoft\Windows NT\CurrentVersion\Terminal Server")` `+0x78 / +0x7f` → `RtlAllocateHeap(0x14)` → `NtQueryValueKey("RegistryExtensionFlags")` `+0xfc / +0x103` → **`g_RegKrnGlobalState` (kernelbase `0x3ab2a8`) ← 값** (`ECX` 비트 16·17 이 켜져 있으면 값의 비트 0·1 을 끔, `+0x145`), `RtlFreeHeap`, `NtClose`. syscall 3번 (+ 되풀이 없음) | **안 탄다** (`+0x36b JZ` 로 건너뜀, 위 `InitializeRegTermsrvFpns` 가 0) |
| `+0x20d`, `+0x210 / +0x217` | ntdll **`LdrDisableThreadCalloutsForDll`** (rva `0xed1f0`, 81B) | `RCX = R12` (kernel32 의 `hModule` = DllBase). `+0xe CMP PebLdr+0x48 (ShutdownInProgress, 0x1d28c8),0` 이 0 이면 `+0x25 / +0x2a` `LdrpFindLoadedDllByHandle(hModule, &항목, …)` → 성공이면 `+0x35 CMP [항목+0x6e] (TlsIndex),0` / `+0x39 JNZ` : 0 이면 **`+0x3b OR byte [항목+0x6a],4` = `LDR_DATA_TABLE_ENTRY.Flags` 비트 18 `DontCallForThreads`** (PDB 타입 `+0x68 Flags`, 비트 18) → `+0x3f / +0x44 LdrpDereferenceModule`. syscall 없음 | 탄다. kernel32 는 정적 TLS 가 없다 (`TlsIndex == 0`) → **kernel32 항목 `DontCallForThreads` 켜짐** → 이후 스레드 attach/detach 때 kernel32 시작 함수를 안 부른다 |

- 같은 함수의 나머지 (이번 질문은 아니지만 짐 지도에 필요): `+0x189` kernel32 `BaseStaticServerData` (`0xc2410`) ← `[PEB+0x98][1] − PEB+0x380 + PEB+0x88` (4번과 같은 식), `+0x194` **`BaseCSDVersion` (`0xc2440`) ← `BaseStaticServerData + 0x3a`**, `+0x261 CMP [PEB+0x2f0],0` (CSDVersion.Buffer) 가 0 이면 **`+0x26e / +0x275` `RtlInitUnicodeString(&PEB.CSDVersion (PEB+0x2e8), BaseCSDVersion)`** — run44 의 PEB `CSDVersion` 변화가 이것 `[정적]`. `BaseWindowsSys32x86Directory` (`0xc2430`) ← `[BaseStaticServerData+0x7f8]`. `+0x1cc TEST [params+8],0x4000`: `Flags = 0x4001` `[관찰]` → `LdrQueryImageFileExecutionOptions`/`SetSearchPathMode` 건너뜀. `+0x1da CMP [params+0xc],0` (`DebugFlags` 0 `[관찰]`) 아니면 `INT3`. `+0x2ee StringCbPrintfW` 는 `\Sessions\1\Windows` 를 스택에 만들지만 이 함수에서 다시 읽는 줄이 없다 (버려지는 값, `.asm` 에서 `[RSP+0x50]` 참조는 `+0x101`·`+0x115` 뿐).

### 11. kernel32 `_Kernel32LegacyLib_BaseDllInitialize` (rva `0x2ca24`)

- 부르는 곳: `_BaseDllInitialize +0x21f / +0x224` (모든 reason). 인자 `EDX = reason`. attach (`+0x10 CMP EDX,1`) 만 아래.
- 순서 (attach):
  - `+0x19 / +0x20` **`KernelBaseGetGlobalData()`** = kernelbase `0xe25e0` 8B, **`LEA RAX,[KernelBaseGlobalData]; RET`** → `&KernelBaseGlobalData` (kernelbase `0x3aeec0`). 예, 맞다.
  - `+0x25` **`KernelBaseGlobalData+0x28` (4B) ← 0** (`MOV [RAX+0x28],EBX`; 이 칸은 kernelbase 안에서 이름으로 참조하는 줄이 없다 — detach 때 `+0xba CMP [RAX+0x28],EBX` 로만 읽힘).
  - `+0x28 ~ +0x5c` kernel32 **`BaseStaticServerData` (`0xc2410`) ← 같은 식** (10번과 같은 값을 한 번 더).
  - `+0x63 / +0x68` `BaseDllInitializeMemoryManager2` (run67) 한 번 더.
  - `+0x6f ~ +0x88` **`BaseDllIniFileMapping` (kernel32 `0xc2350`) ← `[BaseStaticServerData+0x170]` 가 0 이 아니면 `− [BSSD+0x9e8] + BSSD`** (csrss 쪽 포인터를 내 뷰로 옮김), 0 이면 0.
  - `+0x8f ~ +0x9c / +0xa3` **`NtQuerySystemInformation(0, &SysInfo (kernel32 0xc2260), 0x40, NULL)`** — 클래스 0, 버퍼는 **kernel32 자기 `SysInfo`** (kernelbase 것과 다른 전역). 실패 (`+0xaa JS 0x18002cae5`) 면 AL=0 (DllMain FALSE).
  - `+0xac JMP` → `+0xbf MOV BL,1` → 반환 TRUE.
- detach (`+0xa JZ 0x18002cad2`): `+0xae / +0xb5` `KernelBaseGetGlobalData` → `+0xba CMP [RAX+0x28],0` / `+0xbd JNZ 0x18002caf3` → 0 이 아니면 `+0xed / +0xf2` **`BaseDllReadWriteIniFile(…)`** (ini 파일 내용 되쓰기) → attach 에서는 **안 불린다**. 요청서가 물은 "`+0xed/+0xf2` 조건" = `reason == 0` (process detach) **그리고** `KernelBaseGlobalData+0x28 ≠ 0` (kernel32 의 ini 함수들이 켜 둔 때 `[추측]`).
- 쓰는 kernel32 전역: `BaseStaticServerData` `0xc2410` · `BaseDllIniFileMapping` `0xc2350` · `SysInfo` `0xc2260` (0x40B) · (`BaseHeap` `0xc2240`·`SystemRangeStart` `0xc2238` 는 `BaseDllInitializeMemoryManager2`, run67).

### 12. kernelbase `_DllMainCRTStartupForGS2` (rva `0x1290e8`, 24B)

- 본문: `+0x4 CMP EDX,1` / `+0x7 JNZ 0x1801290f6` (= `+0xe`) → reason 이 1 일 때만 `+0x9 / +0xe` **`__security_init_cookie()`** (rva `0x126b04`). 그다음 `+0xe MOV EAX,1; RET` (항상 1 반환, 부르는 쪽은 안 봄).
- `__security_init_cookie`: `+0x4 CMP [__security_cookie (0x3a90d0)],0` / `+0xe` 기본값 `0x2b992ddfa232` 와 비교, 0 이거나 기본값이면 `+0x28 / +0x2d` **`__security_init_cookie_ex(&cookie)`**: `RDTSC` (`+0x5`) `>> 4 ^ &cookie`, 하위 48비트 (`& 0xffffffffffff`) → 0 이거나 기본값이면 `0x2b992ddfa233`. 끝에 `+0x37` **`__security_cookie_complement (0x3a90d8) ← ~cookie`**. syscall 없음, 값은 TSC 에 달려 동적.
- **run59 쿠키 사슬과 이어지나**: **아니다.** run59 는 ntdll `CookieValue` (`NtQueryInformationProcess(ProcessCookie)` → `RtlEncodePointer`) 였다. 이것은 kernelbase 이미지 **자기** 스택 보호 쿠키 (`__security_cookie`, 함수 머리에서 `XOR RSP` 로 쓰고 `__security_check_cookie` 로 검사). kernel32 도 자기 것 (`entry +0x31` → `_DllMainCRTStartupForGS2` rva `0x56898`).
- 누가 부르나: kernelbase = `KernelBaseBaseDllInitialize +0x28 / +0x2d` (**모든 reason** — 안에서 reason 1 만 일함), 이어서 `+0x44 JMP _KernelBaseBaseDllInitialize` (꼬리 점프). 그래서 **`_KernelBaseBaseDllInitialize` 의 함수 머리 `+0x17 MOV RAX,[__security_cookie]` 가 이미 채워진 쿠키를 읽는다.** kernel32 = `entry +0x14 CMP EDI,EDX(1)` / `+0x16 JZ 0x18002c601` (= `+0x31`) / `+0x31 / +0x36`, 그 뒤 `_BaseDllInitialize`.

## syscall 표 (test.exe 기본 길, 이번 요청 범위; 부르는 자리 = 함수 + call/ret)

| # | 이름 · 클래스/코드 | 부르는 자리 | 조건 | 횟수 |
|---|---|---|---|---|
| 1 | `NtQuerySystemInformation` · 클래스 **0** (`SysInfo`, 0x40) | `_KernelBaseBaseDllInitialize +0x23a/+0x241` | 항상 | 1 |
| 2 | `NtOpenSection` · `\Sessions\1\Windows\SharedSection`, 접근 4 | `CsrpConnectToServer +0x1b6/+0x1bb` ← `CsrClientConnectToServer +0x113/+0x118` ← `_KernelBaseBaseDllInitialize +0x2d2/+0x2d9` | `CsrPortHandle == 0` | 1 |
| 3 | `NtCreateSection` · 0x10000B 페이지파일 섹션 | `CsrpConnectToServer +0x23d/+0x242` | 같음 | 1 |
| 4 | `NtConnectPort` · `\Sessions\1\Windows\ApiPort` | `CsrpConnectToServer +0x2df/+0x2e4` | 같음 | 1 |
| 5 | `NtClose` (2번에서 만든 섹션) | `+0x2eb/+0x2f0` | 같음 | 1 |
| 6 | `NtMapViewOfSection` · 읽기 전용 | `+0x338/+0x33d` | 같음 | 1 |
| 7 | `NtClose` (SharedSection 핸들) | `+0x344/+0x349` | 같음 | 1 |
| 8 | `NtGetNlsSectionPtr` · 종류 `0xe` | `SetupMainNlsFiles +0x4f/+0x56` ← `NlsProcessInitialize +0x30/+0x35` ← `BaseNlsDllInitialize +0x17/+0x1c` | `PEB.UseCaseMapping ≠ 0` (`0x6a5` `[관찰]`) | 1 |
| 9 | `NtInitializeNlsFiles` | `RtlGetLocaleFileMappingAddress +0x29/+0x2e` ← `SetupMainNlsFiles +0x9d/+0xa4` | ntdll `gBaseAddress == 0` | 1 (처음 불릴 때) |
| 10 | `NtGetNlsSectionPtr` · 종류 `0xb` (코드페이지 949) | `GetCPHashNode +0x2d3/+0x2da` ← `SetupAnsiOemCodeHashNodes +0x3e/+0x43`, `+0x61/+0x66` | 노드 캐시에 없을 때 | 1~2 |
| 11 | (`MakeLocHashNode(0x7f)` 아래: `NtOpenKey`·`NtQueryValueKey`·`NtCreateKey`·`NtNotifyChangeKey`·`NtCreateEvent`·`NtQueryInformationToken` 후보) | `NlsProcessInitialize +0x5f/+0x64` | **모름** | 0~? |
| 12 | `NtCreateFile` · `<ConsoleHandle>\Connect` | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleInitialize +0x19a/+0x19f` ← `_KernelBaseBaseDllInitialize +0x521/+0x526` | 실제 `ConsoleHandle` (≠0,-1,-2,-3) | 1 |
| 13 | `NtDeviceIoControlFile` · `0x500023` (서버 PID) | `ConsoleCommitState +0x8a/+0x91` ← `ConsoleInitialize +0x1ca/+0x1cf` (run51) | 새 핸들 ≠ 0 | 1 |
| 14 | `NtSetInformationProcess` · 클래스 `0x31` | `ConsoleCommitState +0xe9/+0xf0` (run51) | 항상 | 1 |
| 15 | `NtTraceControl` · `0xf` | `…EtwEventRegister…` ← `_KernelBaseBaseDllInitialize +0x160/+0x165` | 항상 | 1 |
| 16 | `NtTraceControl` · `0x1e` | `EtwEventSetInformation` ← 같은 호출 | 항상 | 1 |
| 17 | `NtQuerySystemInformation` · 클래스 **0** (kernel32 `SysInfo`) | `_Kernel32LegacyLib_BaseDllInitialize +0x9c/+0xa3` ← `_BaseDllInitialize +0x21f/+0x224` | 항상 | 1 |
| — | (본문 갈래, **이 VM 은 안 탐**) `NtQueryInformationToken(0x1d)`·`NtAccessCheck` | `DisableRegCaching… +0x150d12/+0x150d19`, `RtlCheckTokenMembershipEx` | 멀티세션 아닌 SKU | 0 |
| — | (**안 탐**) `NtOpenKey`·`NtQueryValueKey`·`NtClose` | `GetRegistryExtensionFlags`, `IsRegTSAppCompatEnabled` | TS 모르는 exe | 0 |
| — | (**안 탐**) `NtAlpcSendWaitReceivePort` | `CsrClientCallServer` ← `CsrpClientConnectToServer` | csrss 응답 상태 < 0 | 0 |

- 합 (이번 요청 범위, 알려진 것): kernelbase DllMain **16~18번 + 11번 아래** (1 + 6 + 3~4 + 3 + 2 + …) + kernel32 DllMain 1번. run67 의 `NtQuerySystemInformation(0x32)` 1번은 별도.
- RtlpCreateHeap (CSR 개인 힙) 의 "CommitRoutine 있음" 가지에는 syscall 없음 `[정적, .c 기준]`.

## PE_emulator 가 바로 쓸 작은 표 (함수 × 인자 · 쓰는 칸 · 값 출처 · 하위 호출 · syscall · 반환)

| 함수 | 인자 | 쓰는 칸 (모듈 rva) | 값 출처 | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|---|
| kernelbase `…TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` (`0xf70ac`) | 없음 | 공급자 `0x3a9020`: `+0x20` RegHandle (`0x3a9040`), `+0x28/+0x30` ← 0 | GUID `{3c74afb9-…-365dbf48382a}` (`0x368a75`), 콜백 `0x105aa0`, 메타 `0x368a85` (길이 `0x33`) | `EtwEventRegister`, `EtwEventSetInformation(h,2,메타,0x33)` | `NtTraceControl` 0xf, 0x1e | `EtwEventRegister` 상태 |
| kernelbase `DisableRegCachingForConstrainedImpersonation` (`0x4e64c`) | 없음 | `g_DisableCachingWhileImpersonated` (`0x3ab290`) — **이 VM 은 안 씀 (0)** | 토큰 능력 `constrainedImpersonation` 멤버 여부 | `RtlIsMultiSessionSku` | 0 (본문 갈래만 `NtQueryInformationToken`·`NtAccessCheck`) | 없음 |
| `_KernelBaseBaseDllInitialize +0x23a` | `NtQuerySystemInformation(0, &SysInfo, 0x40, NULL)` | `SysInfo` (`0x3aee60`, 0x40B) | nt 9번 표 | — | 클래스 0 | 음수면 DllMain FALSE |
| ntdll `CsrClientConnectToServer` (`0xdb7a0`) | `RCX`=`\Sessions\1\Windows`, `EDX`=1, `R8`=&{`CtrlRoutine`}, `R9D`=8, `[RSP+0x20]`=&byte | `CsrInitOnceDone`·`CsrClientProcess`(←1)·`CsrHeap`·`CsrPortName`·`CsrPortHandle`·`CsrProcessId`·`CsrPortMemoryRemoteDelta`·`CsrPortHeap`·`CsrPortBaseTag`(←0), PEB `+0x88`·`+0x98`·`+0x380`, `*byte ← 0` | csrss 응답 블록 (동적) | `RtlImageNtHeaderEx`, `CsrpConnectToServer` (`RtlAllocateHeap`, `memcpy`, `RtlpCreateHeap`) | 6 (표 2~7) | 0 / 음수 |
| ntdll `RtlSetUnhandledExceptionFilter` (`0xe95f0`) | `RCX` = 필터 | `RtlpUnhandledExceptionFilter` (`0x1d28e8`) | `RtlEncodePointer(RCX)` | `RtlEncodePointer` | 없음 | (인코딩된 값, 무시) |
| kernelbase `BaseNlsDllInitialize` (`0xea9b0`) | `EDX`=reason, `R8`=`BaseStaticServerData` | `gpServerNlsUserInfo` (`0x3afc88`) ← `R8+0x178`, `g_pSortHash`/`g_pSortGuidHash`/`g_pSortVersionDlls` 0 으로 | 6번 | `NlsProcessInitialize` | 8~10 (+11?) | AL 1 / 0 |
| ntdll `RtlInitializeCriticalSection` (`0xd65c0`) | `RCX` = cs | cs `+0x8`=-1, `+0xc`=0, `+0x10`=0, `+0x18`=0, `+0x20`=`0x20007d0` (CPU ≤1 이면 0), `+0x0`=-1 | 상수 | (디버그 정보 켜질 때만 `RtlpAddDebugInfoToCriticalSection`) | 없음 | 0 |
| ntdll `RtlInitializeCriticalSectionAndSpinCount` (`0xd4900`) | `RCX` = cs, `EDX` = 스핀 | 위와 같되 `+0x20` = `EDX&0xffffff` (0 이면 `0x20007d0`, CPU≤1 이면 0) | 호출한 쪽 (`_CrtLock_Exit` ← `0xfa0`) | 같음 | 없음 | 0 |
| kernelbase `ConsoleInitialize` (`0xbeb60`) | 없음 | `ConsoleStateLock`·`ConsoleIsConsoleApplication`(←1)·`SingleHandler`·`HandlerList*`·`ExeName*`·`ConsoleConnectionState`, params `+0x10` (`ConsoleHandle` ← 새 핸들), `+0x408` | params `ConsoleHandle`·`ConsoleFlags`·`WindowFlags`…, NtCreateFile 결과 | 7번 락, `RtlImageNtHeader`, `ConsoleCreateConnectionObject`, `ConsoleCommitState` | 12, 13, 14 | AL (1) |
| kernel32 `_BaseDllInitialize` (`0x2c610`) | `RCX`=hModule, `EDX`=reason | 10번 | 10번 | `RtlImageNtHeader`·`RtlSetThreadPoolStartFunc`(무)·`LdrSetDllManifestProber`·`BaseDllInitializeMemoryManager2`·`RtlGetSuiteMask`·`InitializeRegTermsrvFpns`(→0)·`LdrDisableThreadCalloutsForDll`·`_Kernel32LegacyLib_BaseDllInitialize` | 17 | AL (1) |
| ntdll `LdrDisableThreadCalloutsForDll` (`0xed1f0`) | `RCX` = hModule | 항목 `Flags` 비트 18 | `TlsIndex == 0` | `LdrpFindLoadedDllByHandle`, `LdrpDereferenceModule` | 없음 | 상태 |
| kernel32 `_Kernel32LegacyLib_BaseDllInitialize` (`0x2ca24`) | `EDX`=reason | `KernelBaseGlobalData+0x28`(←0), `BaseStaticServerData`, `BaseDllIniFileMapping`, `SysInfo` | PEB·`BaseStaticServerData` | `KernelBaseGetGlobalData`, `BaseDllInitializeMemoryManager2` | 17 | AL |
| kernelbase `_DllMainCRTStartupForGS2` (`0x1290e8`) | `EDX`=reason | (reason 1) `__security_cookie` (`0x3a90d0`), `__security_cookie_complement` (`0x3a90d8`) | `RDTSC` | `__security_init_cookie` → `__security_init_cookie_ex` | 없음 | 1 |
| nt `ExpGetSystemBasicInformation` (`0x457484`) | `RCX` = 사용자 버퍼 | 9번 표 | 9번 표 | — | — | 0 (길이 ≠ 0x40 이면 호출 전에 `0xC0000004`) |

## `_KernelBaseBaseDllInitialize` attach 순서 (test.exe, 한눈에)

`BasepInitializationStage` (kernelbase `0x3aeea0`, word) 값이 진행 표시다.

| 순서 | 줄 (call / ret) | 일 | stage |
|---|---|---|---|
| 래퍼 | `KernelBaseBaseDllInitialize +0x55/+0x5c`, `+0x28/+0x2d` | `_CrtLock_Exit` 락 (스핀 `0xfa0`) → 쿠키 → 꼬리 점프 | |
| 1 | `+0x1ad/+0x1b2` | MemoryManager (run67) | |
| 2 | `+0x1cf/+0x1d6` | `RtlCreateTagHeap` → 0 (run67) | |
| 3 | `+0x1e7/+0x1ee`, `+0x225/+0x22a` | 객체 디렉터리 `\Sessions\1\Windows` 만들기 | |
| 4 | `+0x23a/+0x241` | `NtQuerySystemInformation(0)` → `SysInfo` | |
| 5 | `+0x253/+0x25a`, `+0x271/+0x278` | `RtlRandomEx` ×2 (시드 = `TEB.ClientId.UniqueProcess`, `+0x3c`/`+0x45`) → **`BasepFiberCookie` (`0x3aee48`) = (첫 값 << 32) \| 둘째 값**; ntdll 쪽 쓰는 전역 `RtlpRandomExConstantVector` 한 칸·`RtlpRandomExAuxVarY` (run61) | `0x64` |
| 6 | `+0x2d2/+0x2d9` | CSR 연결 (4번) | `0xc8` |
| 7 | `+0x2f2 ~ +0x469` | `BaseStaticServerData` 등 | |
| 8 | `+0x490/+0x497` | `RtlUnicodeStringToAnsiString` (run67) | |
| 9 | `+0x4ab/+0x4b2`, `+0x4b9/+0x4c0` | 예외 필터 (5번), `BasepCurrentTopLevelFilter` | `0x190` |
| 10 | `+0x4e1/+0x4e6` | `BaseNlsDllInitialize` (6번) | `0x1f4` |
| 11 | `+0x501/+0x508` | 락 `KernelBaseGlobalData+0xb0` (7번) | `0x258` |
| 12 | `+0x521/+0x526` | `ConsoleInitialize` (8번) | |
| 13 | `+0x160/+0x165` | ETW 등록 (1번) | `0x2bc` |
| 14 | `+0x17a`, `+0x184/+0x189`, `+0x189` | `g_Win64Registry ← 1`, `DisableRegCaching…` (2번), `g_Initialized++` | |
| 반환 | `+0xf4 MOV AL,1` | TRUE | |

(실패 지점: 4번·6번·10번·11번·12번 이 음수/0 이면 `+0x5ab XOR AL,AL` / `+0xf6` 으로 FALSE.)

## 닫힘 / 동적 필요

| 질문 | 상태 | 동적 몫 (Q104 VM 단계 / Q84 에 얹을 것) |
|---|---|---|
| 1 ETW | **정적으로 닫힘** (GUID·이름·syscall 2번). RegHandle `0x5_00000041` 은 계산값 | `kernelbase!_KernelBaseBaseDllInitialize +0x165` (ret) 에서 `dq kernelbase+0x3a9020 L8`, `dq kernelbase+0x3a9040 L1` (RegHandle). `!ntdll` 쪽 5번 등록 순서 확인 |
| 2 DisableReg | **정적으로 닫힘** (이 VM 은 AL=1 → 안 씀) | `dd 7ffe02f0 L1` (0x10e), 반환 뒤 `dd kernelbase+0x3ab290 L1` (0), `dd kernelbase+0x3ad9ac L1` (1) |
| 3 SysInfo | **정적으로 닫힘** (클래스 0, `SysInfo` 0x40B) | `+0x241` (ret) 에서 `dq kernelbase+0x3aee60 L8` — 값은 9번 표 |
| 4 CSR | **꼴·쓰는 자리 닫힘.** 값은 **동적 (Q84)** | 멈춤점 `ntdll!CsrpConnectToServer +0x2e4` (ret 의 `NtConnectPort`): `r rax`, `dq ntdll+0x1d4e00 L2`(`CsrPortName`), `du poi(ntdll+0x1d4e08)` (`\Sessions\1\Windows\ApiPort`), `dq ntdll+0x1d4e20 L1`; `+0x36d ~ +0x38d` 직후 `dq @$peb+0x88 L1`·`+0x98`·`+0x380`; `+0x3f8` (ret) `dq ntdll+0x1d4e28 L1`; `kernelbase!_KernelBaseBaseDllInitialize +0x32b` 직후 `dq kernelbase+0x3aedb8 L1`, `dq kernelbase+0x3aeec0 L0x20`. 응답 블록 `[RBP-8]` 0x30B (`+0x2e4` 에서) |
| 5 예외 필터 | **식은 정적으로 닫힘**, 값은 동적 | `ntdll!RtlSetUnhandledExceptionFilter +0x9` 직후 `dq ntdll+0x1d28e8 L1`, `kernelbase+0x3aeda8` |
| 6 NLS | **부분**: 앞 3줄기 닫힘, `MakeLocHashNode(0x7f)` 아래 **못 닫음** | `kernelbase!BaseNlsDllInitialize +0x1c` (ret) 에서 syscall 센기 (`bp ntdll!NtGetNlsSectionPtr`, `NtInitializeNlsFiles` 에 `r rcx` 로 종류·`kd> k`), 레지스트리 syscall 이 이 구간에 있는지. `dq kernelbase+0x3afc88 L1` |
| 7 락 | **정적으로 닫힘** | `dq kernelbase+0x3aef70 L5` (스핀 `0x20007d0`, DebugInfo -1) |
| 8 콘솔 | **갈래 닫힘**, 값 동적 | `kernelbase!ConsoleInitialize +0xbd` (ret) `r al` (`ConsoleShouldAllocateConsole`), `+0x19f` (ret) `r eax`·`dq @rbp+0x28 L1` (새 핸들), `dq @$peb → ProcessParameters+0x10 L1` |
| 9 SysInfo 칸 | **닫힘** (정적 5칸 · 동적 6칸) | 동적 6칸 값은 Q104: `dq kernelbase+0x3aee60 L8` 와 `dq kernel32+0xc2260 L8` |
| 10 kernel32 | **정적으로 닫힘** | `kernel32!_BaseDllInitialize +0x217` (ret) `dd` 로 kernel32 항목 `Flags` (`+0x68`) 비트 18, `+0x369` (ret) `r al` (0) |
| 11 LegacyLib | **정적으로 닫힘** | `kernel32!_Kernel32LegacyLib_BaseDllInitialize +0xa3` (ret) `dq kernel32+0xc2260 L8` |
| 12 GS2 | **정적으로 닫힘** | (선택) `dq kernelbase+0x3a90d0 L2` |

## 짐 지도 반영거리

- **kernelbase 전역** (모두 처음 0 — 파일 `.data` 꼬리): `SysInfo` `0x3aee60` (0x40B) · `BasepFiberCookie` `0x3aee48` · `BaseStaticServerData` `0x3aedb8` · `BaseRCNumber` `0x3aedc0` · `KernelBaseGlobalData` `+0x4`·`+0x8`·`+0x10`·`+0x18`·`+0x20` (4번) · `BasepCurrentTopLevelFilter` `0x3aeda8` · `gpServerNlsUserInfo` `0x3afc88` · NLS 전역 (`g_pSortHash` `0x3af420`·`g_pSortGuidHash` `0x3af820`·`g_pSortVersionDlls` `0x3afc20`·`gpInvLocHashN` `0x3aad28`·`gpACPHashN` `0x3aad00`·`gpOEMCPHashN` `0x3aad68`·`gAnsiCodePage` `0x3a92ac`·`gOemCodePage` `0x3a92a8`·`gSystemLocale` `0x3aace8`·`TblPtrs+0xc0/+0xc8/+0x50/+0x58`·`KernelBaseGlobalData+0x60 ~ +0x90`·`IsNlsProcessInitialized` `0x3aacec`) · 락 `KernelBaseGlobalData+0xb0` (`0x3aef70`, 0x28B) · `_CrtLock_Exit` `0x3af3e0` · 콘솔 (`ConsoleStateLock` `0x3af160`·`ConsoleIsConsoleApplication` `0x3af188`·`ConsoleConnectionState` `0x3af120`(0x38B)·`SingleHandler` `0x3af0f0`·`HandlerList` `0x3ab720`·`HandlerListLength`/`AllocatedHandlerListLength` `0x3af0f8/0x3af0fc`·`ExeNameBuffer` `0x3af1a0`·`ExeNameLength` `0x3af3a0`·`ExeNameInitialized` `0x3ab714`) · ETW 공급자 `0x3a9020` (0x40B) · `g_Win64Registry` `0x3ad9ac` · `g_Initialized` `0x3ab318` · `g_DisableCachingWhileImpersonated` `0x3ab290` (이 VM 0) · `__security_cookie` `0x3a90d0`/`0x3a90d8` · `BasepInitializationStage` `0x3aeea0` (진행 표시 word).
- **kernel32 전역**: `BaseStaticServerData` `0xc2410` · `BaseCSDVersion` `0xc2440` · `BaseDllIniFileMapping` `0xc2350` · `BaseWindowsSys32x86Directory` `0xc2430` · `SysInfo` `0xc2260` (0x40B) · `BasepAppCertDllsList` `0xc2420` (자기를 가리키는 빈 리스트) · `__security_cookie` (kernel32 자기 것). (`pfnTermsrvConvertSysRootToUserDir` 계열 9칸은 **NULL 그대로** — test.exe 는 TS 인식 exe.)
- **ntdll 전역**: `CsrInitOnceDone`·`CsrClientProcess`·`CsrHeap`·`CsrPortName`(Length·Maximum·Buffer)·`CsrPortHandle`·`CsrProcessId`·`CsrPortMemoryRemoteDelta`·`CsrPortHeap`·`CsrPortBaseTag` (4번 rva) · `RtlpUnhandledExceptionFilter` `0x1d28e8` · `gBaseAddress` `0x1ce7b0`/`gSystemLocale` `0x1d3d94` (`NtInitializeNlsFiles` 결과) · `RtlpRandomExConstantVector`·`RtlpRandomExAuxVarY` (매 호출, run61).
- **PEB 칸 (유저, 에뮬레이터 가상 주소 공간)**: `+0x88` `ReadOnlySharedMemoryBase`·`+0x98` `ReadOnlyStaticServerData`·`+0x380` `CsrServerReadOnlySharedMemoryBase` (**ntdll `CsrpConnectToServer`**), `+0x2e8` `CSDVersion` (**kernel32 `_BaseDllInitialize +0x26e/+0x275`**). run44 사진의 "PEB CSR 칸 4개" = 이 넷 `[정적]` (`PatchLoaderData` 는 다른 주인).
- **프로세스 매개변수 칸**: `+0x10` `ConsoleHandle` (`ConsoleCommitState`, `4 → 새 핸들`), `+0x408` `ProcessGroupId` (0 → PID), `+0xa4` `WindowFlags` (비트 0x200·0x400 지움 가능, `ConsoleCommitState +0x2c ~ +0x159`; test.exe `WindowFlags = 0` 이라 안 바뀜).
- **프로세스 힙 블록**: `CsrPortName.Buffer` **0x42바이트** (`\Sessions\1\Windows\ApiPort`, `CsrHeap` = 프로세스 힙) · `AllocTables` 2블록 (크기 미확인) · 이름 캐시 노드들 (`GetCPHashNode`·`MakeLocHashNode` 의 `RtlAllocateHeap`, 개수 미확인) · 임시 0x54b 블록 (`ConsoleCreateConnectionObject`, 곧 해제 `+0x1e1`) · (run67: `BaseAnsiCommandLine` 9B).
- **핸들 / 뷰 / 섹션**: `CsrPortHandle` (ALPC 포트, `[관찰]` `0x74`) · `ConsoleHandle` `4 → 0x60` (`4` 는 부모 콘솔 참조 핸들로 닫히지 않고 `ConsoleConnectionState+0x10` 에 남음 `[정적]`) · 읽기 전용 공유 뷰 (`PEB.ReadOnlySharedMemoryBase`, `[관찰]` `0x7ff4ecf70000`) · CSR 포트 뷰 0x10000B (RW, csrss 와 공유; 안에 `CsrPortHeap`) · kernelbase 자기 NLS 뷰 (종류 `0xe` ×1, `0xb` ×1~2) · locale 파일 뷰 (`NtInitializeNlsFiles`).
- **커널 칸 (에뮬레이터 바깥)**: `EPROCESS.OwnerProcessId` 둘째 쓰기 (`ConsoleCommitState +0xe9`, run51), 새 `_FILE_OBJECT` (condrv `\Connect`), ALPC 포트 객체 (`NtConnectPort`), 섹션 객체 2 + 뷰 2개 (VAD), 등록 개체 (ETW `NtTraceControl`).
- **줄기 길이**: 4번 (CSR) `CsrClientConnectToServer → CsrpConnectToServer → 6 syscall + RtlpCreateHeap(공용 부품)` 끝까지. 8번 `ConsoleInitialize → ConsoleCreateConnectionObject → NtCreateFile` 끝까지. 7번 `RtlInitializeCriticalSection` 끝까지. **끝까지 못 닿은 곳**: `MakeLocHashNode(0x7f)` 아래 (NLS 이름 캐시), `RtlQueryActivationContextApplicationSettings` 안, 본문 갈래 (`DisableReg…` 의 멀티세션 아닌 SKU 용).

## 동적분석으로 확인해야 할 것

dast 요청 필요: **Q84 (csrss 연결 결과)** 와 **Q104 (VM 사진: kernelbase·kernel32 `.data`)**. 위 "닫힘 / 동적 필요" 표의 멈춤점이 그대로 요청서 후보다. 핵심만:

1. `kernelbase!_KernelBaseBaseDllInitialize` 의 `+0x241`·`+0x2d9`·`+0x32b`·`+0x4e6`·`+0x508`·`+0x526`·`+0x165`·`+0x189` (ret) 를 차례로 멈춰 `BasepInitializationStage` (`0x3aeea0`) 를 같이 찍기 — DllMain 이 어디까지 갔는지 한눈에 보인다.
2. `ntdll!CsrpConnectToServer +0x2e4` 에서 응답 블록 0x30B (`[RBP-8]`) 와 `[RBP+0x1c]` (상태 = 0 기대) — 4번의 "`CsrpClientConnectToServer` 안 탄다" 확인. 안 맞으면 `ntdll!CsrpClientConnectToServer` 입구에 멈춤점.
3. NLS: `BaseNlsDllInitialize +0x1c` 까지의 syscall 수 (`NtGetNlsSectionPtr` 2~3, `NtInitializeNlsFiles` 1, 그리고 **레지스트리 syscall 이 섞이는지**).
4. 9번 칸의 실제 값 (`SysInfo` 두 벌).
5. `dd 7ffe02f0` (`0x10e`) / `g_DisableCachingWhileImpersonated` = 0 — 2번이 정말 안 쓰는지.

추천 멈춤점: `kernelbase!_KernelBaseBaseDllInitialize+0x241` (SysInfo 직후), `ntdll!CsrpConnectToServer+0x2e4` (연결 직후), `kernelbase!ConsoleInitialize+0x19f` (콘솔 새 핸들), `kernel32!_BaseDllInitialize+0x369` (TS 갈래 확인).

## 판정

**분석성공.** 요청서 질문 1 ~ 12 모두 `.asm` 줄(call/ret)과 함께 답이 있고, 대부분 정적으로 닫혔다. 남은 것은 ① csrss 응답 값 (4번, Q84), ② NLS `MakeLocHashNode(0x7f)` 아래 (6번), ③ 사진으로 보는 `SysInfo` 값 (9번, Q104) 셋이다. 스크립트 문제 없음.

# 57 tp-etw-init — 정적 분석 결과

- 요청서: `_workspace/sast/57_tp-etw-init.md` (Q118, PE_emulator 요청 10)
- 모듈 / 바이너리: ntdll (`ms/bin/…/System32/ntdll.dll`), PDB GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`, sast/56 과 같음)
- 분석: 이미 되어 있음 (`.gpr` + `_index.tsv` + `SetPdb:` 줄). **다시 analyze 안 했다.** export 만 했다.
- 뽑은 것 (모두 `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`; 로그 `ms/ghidra/ntdll.export.sast57a ~ m.stdout`)
  - 요청 대상 13개 (depth 0): `TpInitializePackage` `TppQueryMaximumGroupCount` `TpSetDefaultPoolCpuSets` `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` `EtwEventSetInformation` `EtwEventRegister` `EtwpRegisterProvider` `RtlInitializeHeapLogging` `RtlInitializeExceptionLog` `Feature_…DisallowSharedLockImplicitUpgrade…NoReportingNoInline`·`…Fallback` `Feature_…LoaderSnapsBuffer…DeviceUsageNoInline`·`…Fallback`
  - 줄기를 끝까지 가려고 보탠 것 (depth 0): `TpAllocWait` `TpSetWaitEx` `TpReleaseWait` `TppWorkInitialize` `TppPoolpReferenceGlobalPool` `TppCleanupGroupAddMember` `TppAdjustRunningThreadGoalWithLock` `EtwpSetProviderTraits` `EtwpUpdateEnableInfoAndCallback` `EtwpInsertRegistration` `EtwpCheckForPrivatePreEnable` `ProviderHandleInsert` `TppCleanupGroupMemberInitialize` `TpAllocPoolInternal` `EtwpTrackProviderBinary` `UserDiagnosticProviderCallback` `_tlgEnableCallback` `RtlpHpStackTraceEtwCallback` `EtwpNotificationThread` `wil_details_IsEnabledFallback` 와 그 아래 `wil_details_*` 9개 `RtlNotifyFeatureUsage` `RtlpSubscribeWnfStateChangeNotificationInternal` `RtlpCreateWnfUserSubscription` `RtlpCreateWnfNameSubscription` `RtlpAddWnfUserSubToNameSub` `RtlpCreateSerializationGroup` `RtlRunOnceBeginInitialize` `TpAllocTimer` `TppInitializeTimer*` `TpSetPoolMaxThreads` `TpSetPoolMaxThreadsSoftLimit` `TpSetPoolStackInformation` `TppPoolUpdateNodeRelation` `TppGetCurrentThreadNumaNode` `TppETWPoolCreate` `RtlGetCurrentServiceSessionId` `RtlpSynchronizeHeapLoggingStateCallback` `RtlpFcMapSingleBuffer` `RtlpFcBufferManagerUpdateBuffers` `RtlpFcSectionTypeToBufferType` `RtlpFcSendFeatureUsageNotifications` `RtlpFcNotifyFeatureUsageTarget` 그리고 depth 1 로 `RtlQueryFeatureConfiguration` `RtlpFcReferenceFeatureConfigurationBuffers` `RtlpFcQueryFeatureConfigurationFromKernel` `RtlQueryInternalFeatureConfiguration` (→ `RtlpFcUpdateLocalConfiguration` 등)
  - 넓힌 이유: 요청서 6·4 가 "줄기 끝까지" 이고, 줄기 끝 (`EtwpRegisterTpNotificationOnce` 의 `TpAllocWait`·`TpSetWaitEx`, `LdrpEnableUMGLTracingStateSync` 의 `RtlpInitializeWnf`→`TpAllocTimer`) 에서 **기본 스레드 풀이 처음 만들어져** syscall 이 늘기 때문이다. 8(a)(b) 는 "기능 상태" 가 첫 검사에서 설정 구역을 읽어 오는 syscall 길이 있어서 `RtlpFc*` 까지 갔다.
  - `_refs` 새로 (`ms/disassemble/ntdll/_refs/`): `RtlpSrwLockAllowImplicitUpgrade` `TppPoolpGlobalPoolMaxThreadsOverride` `g_hPrivLoggerNotificationProvider` `g_hUserDiagnosticProvider` `0x1cc6f0` `0x1cc670` `EtwpRegisterTpInitOnce` `PrivateLoggerNotificationEntry` `0x1d28c8` `EtwpFreeRegistrationList` `EtwEventRegister` `EtwNotificationRegister` `EtwpRegisterProvider` `EtwpRegisterTpNotificationOnce` `TppPoolpDefaultPoolCpuSets` `TppPoolpDefaultPoolCpuSetCount` `TppNumberNodes` `TppMaximumGroups` `TppHeapTag` `TppPoolpGlobalPool` `TpInitializePackage` `TpSetDefaultPoolCpuSets` `RtlInitializeExceptionLog` `RtlpExceptionLog2` `RtlInitializeHeapLogging` `PrivateLoggerNotificationGuid` `0x1d23e0` `0x1ce1f8` `0x1ce200` `0x1d2740` `RtlpFc*` `RtlQueryFeatureConfiguration` `wil_details_IsEnabledFallback` `0x1d4f20` `0x1d4f40` `0x1d3c40`
- 버전 확인: **일치** — `_calls.tsv` 에서 `LdrInitializeThunk +0x9/+0xe → LdrpInitialize`, `LdrpInitializeInternal +0x55/+0x5a → _LdrpInitialize`, `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess`, `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal`, `LdrpInitializeNode +0x166/+0x16b → LdrpCallTlsInitializers`. syscall 몸통 (`NtClose` 0xf, `NtMapViewOfSection` 0x28, `NtCreateEvent` 0x48) 도 알려진 번호와 같다.
- 스크립트 문제: 없음
- 오프셋 약속: `call +0x..` = CALL 자리, `ret +0x..` = 다음 줄 (콜스택 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리. 점프 목적지는 (목적지 절대 주소 − 함수 시작) 으로 셌다 (스크립트로 한 번 더 대조).
- 이름 조심: `+0x61 NtQuerySystemInformation` (Ghidra 별칭 `RtlGetNativeSystemInformation`) 은 PDB 별칭표로는 `Zw/NtQuerySystemInformation` 한 쌍뿐 → `NtQuerySystemInformation` (syscall 0x36). `0x1801cc6f0` 의 Ghidra 주석 `NormalizationListHead+0x48` 은 믿지 않는다 (아래 5번).

## 한 줄 결론 (PE_emulator 에 줄 것)

- **test.exe 길에서 `+0xb83 ~ +0xc2d` 구간 syscall = 30번** (정적 셈. 전제: ETW 세션이 이 공급자들을 켜 두지 않았고, 버퍼 모자람 되풀이가 없고, 힙이 커지느라 부르는 `NtAllocateVirtualMemory` 는 안 센 수): `TpInitializePackage` 3 + `LdrpEnableUMGLTracingStateSync` 18 + ETW 등록 9. 순서·이름은 아래 "syscall 표".
- **`EtwEventRegister` 는 이 구간에서 4번 불린다** (직접 2 + `TraceLoggingRegisterEx…` 안 1 + `RtlInitializeHeapLogging` 안 1). `RtlInitializeHeapLogging +0x3e` 의 둘째 `TraceLoggingRegisterEx…` 는 `RtlpHpHeapFeatures` 비트 3 이 0 이라 **안 탄다** `[관찰]`.
- **"Once" 의 정확한 뜻**: 4번 **전부** `NtTraceControl(함수 코드 0xf)` 를 1번씩 부른다 (`EtwpRegisterProvider +0x14d/+0x152`). "Once" 는 그 위에 얹히는 **TP 알림 등록 한 덩어리** (`NtCreateEvent`·`NtCreateWaitCompletionPacket`·`NtAssociateWaitCompletionPacket`·`NtTraceControl(0x1b)`, 합 4번) 이고 **첫 번째 `EtwpRegisterProvider` 호출에서만** 돈다. 그 첫 번째는 **`LdrpInitializeProcess +0xbee/+0xbf3` → `TraceLoggingRegisterEx… +0x55/+0x5a` → `EtwEventRegister`** 이다 (**`+0xc06`·`+0xc23` 이 아니다** — flow.md·run51 표 정정 필요). 이 구간보다 앞에서는 test.exe 에서 불리지 않는다.
- 이 TraceLogging 등록은 syscall 이 하나 더 있다: `EtwEventSetInformation → EtwpSetProviderTraits +0xdd/+0xe2 NtTraceControl(0x1e)` (1번). 그래서 첫 등록 하나가 syscall **6번**, 나머지 셋은 **각 1번**: 6+1+1+1 = 9.
- **`+0xbe2` `LdrpEnableUMGLTracingStateSync` 가 기본 스레드 풀을 처음 만든다** (IO 완료 포트 1 · 일꾼 공장 1 · 타이머 큐 둘 = `NtCreateTimer2` 2 …, 풀 만들기만 12번 + 첫 TP 개체 1번). 그래서 ETW 의 Once 쪽 `TpAllocWait` 은 **풀을 새로 안 만든다** (이미 있음) — 이 순서가 Once 쪽 횟수 4번을 정한다.
- 54 공사 중 셋 (`+0xaad ~ +0xb05`):
  - (a) `Feature_…DisallowSharedLockImplicitUpgrade`: **"켜짐" 고정이 맞다** `[관찰]` (`RtlpSrwLockAllowImplicitUpgrade` = 0 이 쓰는 줄 `+0xaba` 뒤 사진 P1a~P2 에서 0 → 함수가 0 이 아닌 값을 돌려줬다). 다만 **이 검사가 이 프로세스에서 첫 기능 상태 검사라서 설정 버퍼를 읽어 오는 syscall 이 붙는다**: `NtQuerySystemInformationEx(0xd3)` 1 + `NtMapViewOfSection` k + `NtClose` k (k = 0~4, **동적**).
  - (b) `LoaderSnapsBuffer`: **test.exe 에서 참이다 (스냅 버퍼가 실제로 만들어진다)** `[관찰]` — `LdrpSnapsUnicodeString`·`LdrpSnapsUnicodeString2` 가 사진 `≤P1a` 에서 값이 있다. PE_emulator 가 건너뛰면 syscall 은 같지만 (이 함수는 `RtlAllocateHeap` 하나) **힙 블록 0x1000 바이트 하나와 전역 4칸이 빠지고, 이후 `LdrpLogInternal` 이 그 버퍼에 글을 쌓는 길도 빠진다.**
  - (c) `RtlInitializeExceptionLog`: **안 탄다** `[관찰]` (`PEB.NtGlobalFlag` = 0 → 비트 `0x800000` 없음, `RtlpExceptionLog2` 끝까지 0).
- 정적으로 닫힘: 1·2·3·4(줄기)·5·6(인자·횟수)·7·8(c)·9. **동적 필요**: 8(a) 의 k, 그리고 "ETW 세션이 이 4개 공급자를 켜 놨는가" (사진은 켜짐 흔적이 없음을 말하지만 syscall 되풀이는 동적으로만 확정).

## 질문별 답 [정적]

각 함수에 (1) 무엇을 쓰나 (2) syscall (3) 부르는 하위 함수 (4) test.exe 갈래 순서로 쓴다. 근거는 `ms/disassemble/ntdll/<함수>.asm` 줄.

### 1. `TpInitializePackage` (rva `0xdc484`, 193B, 호출 `LdrpInitializeProcess +0xb83/+0xb88`)

- (1) 쓰는 것
  - 전역: `TppHeapTag` (`0x1d22bc`, 4B) ← `RtlCreateTagHeap` 결과 (`+0x51`). `TppNumberNodes` (`0x1d2274`, 4B) ← 버퍼 첫 dword + 1 (`+0x74 ~ +0x7a`). `TppMaximumGroups` (`0x1d2270`, 4B) ← `TppQueryMaximumGroupCount` 의 `AX` (`+0x85 ~ +0x88`).
  - PEB: `PEB+0x388` `TppWorkerpListLock` (8B) = 0 (`+0x9b`), `PEB+0x390` `TppWorkerpList` (`_LIST_ENTRY` 16B) 을 자기 자신을 가리키게 (`+0xa3`, `+0xa9`, `+0xad`). PDB `_PEB` 로 칸 이름 확인.
  - 지역: 스택 `0x408` 바이트 버퍼 (`+0x14` memset).
- (2) syscall: **3번** (항상)
  - `+0x61/+0x66` `NtQuerySystemInformation` (클래스 `0x37`, 버퍼 `0x408`) 1번.
  - `+0x80/+0x85` `TppQueryMaximumGroupCount` 안 `+0x44/+0x49` `NtQuerySystemInformationEx` (클래스 `0x6b`, 입력 4바이트 = 값 4) **2번**: 1번째는 버퍼 NULL 이라 `0xc0000004` (`+0x4d CMP`) → `+0x7a/+0x7f` `RtlAllocateHeap` → `+0x8b JMP 0x1800dc810` (= `+0x1c`) 로 되돌아 2번째는 성공 (`+0x4b JNS 0x1800dc881` = `+0x8d`, `MOVZX EDI, word [RBX+8]`) → `+0xb2/+0xb7` `RtlFreeHeap`. 2번째가 또 `0xc0000004` 면 `+0x57 JNZ 0x1800dc8bf` (= `+0xcb`) 로 풀고 다시 (되풀이는 동적).
  - 클래스 이름은 `[추측]` (`0x37` = NUMA 지도류, `0x6b` = 논리 프로세서 정보 Ex · 입력 4 = 그룹 관계).
- (3) 하위 (call / ret): `+0x14/+0x19` memset · `+0x36/+0x3b` `RtlCreateTagHeap` (인자: 프로세스 힙 `[PEB+0x30]`, 0, `0x18017fff0`, `0x180180010`) · `+0x61/+0x66` `NtQuerySystemInformation` · `+0x80/+0x85` `TppQueryMaximumGroupCount`. `TppQueryMaximumGroupCount` 안: `+0x44/+0x49` `NtQuerySystemInformationEx` · `+0x7a/+0x7f` `RtlAllocateHeap` · `+0xb2/+0xb7`, `+0xe7/+0xec` `RtlFreeHeap`.
- (4) 갈래
  - **실패 갈래 (`+0xb8a JS 0x18008ce7f` = `+0xa7b`)**: `TpInitializePackage` 가 음수를 돌려주면 `LdrpInitializeProcess` 는 공통 끝 `+0xa7b ~ +0xaa7 RET` 으로 바로 간다 (`+0xa76 MOV EAX,0xc0000017` 은 건너뜀) → **`LdrpInitializeProcess` 의 반환값 = `TpInitializePackage` 의 오류값 그대로**. 오류가 되는 곳 셋: `+0x68 JS 0x1800dc536` (= `+0xb2`, `NtQuerySystemInformation` 의 상태 그대로) · `+0x72 JC 0x1800dc53f` (= `+0xbb`, 돌아온 길이 < 4) · `+0x90 JZ 0x1800dc53f` (`TppMaximumGroups` 가 0) — 뒤 둘은 `0xc00000e5` (`+0xbb MOV EAX`, `+0xc0 JMP` → `+0xb2`).
  - test.exe: **성공 갈래** `[관찰]` — `PEB+0x390` 이 P1 `[0 - 0]` → P1a 에서 자기 주소 둘 (`ms/logs/04_baggage-read_P1.txt:1664`, `_P1a.txt:1831`), `TppNumberNodes`·`TppMaximumGroups` 사진 `≤P1a` (값 있음), `TppHeapTag` "끝까지 0" (`RtlCreateTagHeap` 가 0 — sast/40 `NtGlobalFlag & 0x800` 없음). 이 VM: `ActiveProcessorCount` = 2, `ActiveGroupCount` = 1 `[관찰]` (KUSD `+0x3c0`·`+0x3c4`).

### 2. `TpSetDefaultPoolCpuSets` (rva `0x121c38`, 77B, 호출 `+0xba2/+0xba7`)

- (1) 쓰는 것: `TppPoolpDefaultPoolCpuSetLock` (`0x1d22b0`, SRW 락) 잡고 풂, `TppPoolpDefaultPoolCpuSetCount` (`0x1d22b8`, 4B) ← EDX (`+0x1e`), `TppPoolpDefaultPoolCpuSets` (`0x1d32c0`) ← `RCX` 가 가리키는 `EDX*8` 바이트 복사 (`+0x24 SHL R8,3`, `+0x32` `RtlCopyMemory`). **길이 검사 없음** `[정적]`. 이름 있는 칸이 `0x1d32c0` ~ 다음 이름 `g_hPrivLoggerNotificationProvider` (`0x1d33c0`) = 최대 `0x100` 바이트 = 32개 `[추측]` (PDB 이름엔 크기가 없음).
- (2) syscall: **없음** (SRW 락 + memcpy).
- (3) 하위: `+0x16/+0x1b` `RtlAcquireSRWLockExclusive` · `+0x32/+0x37` `RtlCopyMemory` (별칭 `RtlMoveMemory`·`memcpy`·`memmove`) · `+0x48` 꼬리 `JMP` `RtlReleaseSRWLockExclusive` (돌아올 자리 없음).
- (4) 조건과 갈래: `+0xb90 MOV EDX,[R13+0x438]` → `+0xb97 TEST EDX,EDX` → `+0xb99 JZ 0x18008cfab` (= `+0xba7`) 이면 건너뜀. **R13 = 매개변수 블록** 맞다: `+0x6bc/+0x6c0 RtlNormalizeProcessParams([PEB+0x20])` 의 반환을 `+0x6c5 MOV R13,RAX` 로 저장하고, `+0x6c5` ~ `+0xb90` 사이에 `R13` 을 쓰는 줄이 없다. **test.exe: `+0x438` = 0** `[관찰]` — 사진 8곳 (K1 K2 K3 P0 P1 P1a P1b P2) 모두 `DefaultThreadpoolCpuSetMaskCount : 0`, `DefaultThreadpoolCpuSetMasks : (null)` (`ms/logs/04_baggage-read_<지점>.txt`, 예 `P1.txt:2377~2379`). `TppPoolpDefaultPoolCpuSetCount` 도 끝까지 0. **안 탄다.**
  - (타면) `TpAllocPoolInternal +0x3ff ~ +0x415` 가 새 풀마다 `TpSetPoolThreadCpuSets` (`NtSetInformationWorkerFactory` 클래스 15) 를 더 부르지만, test.exe 에선 아님.

### 3. `+0xba7 ~ +0xbd0` 블록

- (1) `+0xba7 MOV EBX,[R13+0x43c]` (`DefaultThreadpoolThreadMaximum`, 4B) → `+0xbae TEST EBX,EBX` → `+0xbb0 JZ 0x18008cfd4` (= `+0xbd0`). 0 이 아니면 `+0xbb2 LEA RCX,[0x1801d22a0]` (`TppPoolpGlobalPoolLock`) → `+0xbb9/+0xbbe` `RtlAcquireSRWLockExclusive` → **`+0xbbe MOV [0x1801d22a8],EBX`** (`TppPoolpGlobalPoolMaxThreadsOverride`, 4B) → `+0xbc4 LEA` → `+0xbcb/+0xbd0` `RtlReleaseSRWLockExclusive`.
- (2) syscall 없음. (3) 하위: 위 SRW 둘.
- (4) **test.exe: 안 탄다** `[관찰]` — `+0x43c` = 0 (사진 8곳, 위 2번과 같은 줄), `TppPoolpGlobalPoolMaxThreadsOverride` 사진 "끝까지 0" (baggage 7.3). 이 전역을 읽는 곳: `TppPoolpReferenceGlobalPool +0x107`·`+0x145`, `TpSetDefaultPoolMaxThreads +0x20`·`+0x3c` (`_refs/TppPoolpGlobalPoolMaxThreadsOverride.tsv`, 쓰는 곳은 `+0xbbe` 하나뿐). 값이 0 이면 기본 풀의 최대 스레드를 `ActiveProcessorCount*8` (최소 `0x300`) 으로 정한다 → 이 VM (2코어) 은 **`0x300`**, 소프트 한도는 `ActiveProcessorCount*4` (최소 `0x180`) → **`0x180`** (`TppPoolpReferenceGlobalPool +0x111 ~ +0x13a`, `+0x166 ~ +0x17b`). 값이 0 이 아니어도 풀 만들 때 `NtSetInformationWorkerFactory` 는 똑같이 2번 (값만 다름).

### 4. `LdrpEnableUMGLTracingStateSync` (rva `0x95794`, 234B, 호출 `+0xbe2/+0xbe7`)

- 부르는 조건: `+0xbd0 CMP [UseWOW64],R15D` → `+0xbd7 JNZ 0x18008cfeb` (= `+0xbe7`) · `+0xbd9 CMP [LdrpIsSecureProcess],R15B` → `+0xbe0 JNZ 0x18008cfeb`. **R15 = 0** 맞다 (`+0xa50 XOR R15D,R15D` 뒤 `+0xbd0` 까지 쓰는 줄 없음). `UseWOW64` 는 `+0x5f2` 가 `TEB+0x180c > 0` 일 때 1 (64비트 test.exe 는 0), `LdrpIsSecureProcess` 는 `LdrpInitialize +0x5a` 가 `ProcessParameters.Flags` 맨 윗 비트 때만 1 (sast/54). → test.exe: **탄다** `[정적]`.
- (1) 쓰는 것
  - 이름 없는 RTL_RUN_ONCE 칸 `0x1d23e0` (8B). **Ghidra 가 `g_pShimmedModuleList+0x8` 로 적었지만 틀렸다**: 앞 이름 `g_pShimmedModuleList` (`0x1d23d8`) 은 8B, 다음 이름 `Feature_…LoaderSnapsBuffer__private_reporting` (`0x1d23e8`). 이 칸을 쓰는 곳은 이 함수뿐 (`_refs/0x1d23e0.tsv`: `+0x6` 읽기 · `+0x35` CMPXCHG · `+0x7f`·`+0xd7` 주소 취함). 끝 값은 `RtlRunOnceComplete(…, 0, Context=[RSP+0x60])` 가 `+0x93/+0x98` 에서 정해 (구독 핸들 | 2).
  - 아래 줄기에서: WNF 한 번만 표 `0x1ce1f8` (RTL_RUN_ONCE) · WNF 문맥 포인터 `0x1ce200` ← `RtlpInitializeWnf +0xa1` · 힙 블록 `0x58`B (`RtlpInitializeWnf +0x1d`) · **기본 스레드 풀 전체** (`TppPoolpGlobalPool` `0x1d2298` 등, 아래).
- (2) syscall: **18번** (표는 아래 "syscall 표"). 줄기 (call / ret 둘 다):
  - `+0x7a/+0x7f` `RtlpSubscribeWnfStateChangeNotificationInternal` (인자: `RDX` = WNF 이름 `WNF_ETW_UMGL_TRACING_CHANGE` = `0x41911a28a3bc10b5` `[정적]` (`0x17c9e0` 의 8바이트), `R9` = 콜백 `0x18015e120` = `LdrpUMGLTracingStateChangeNotification`)
    - `+0x4b ~ +0x5c` `RtlRunOnceBeginInitialize(0x1ce1f8)` → 처음엔 `0x103` (`+0x66 CMP EAX,0x103`, `+0x6b JZ 0x180097019` = `+0x17d`) → `+0x180 ~ +0x18e` **`RtlpInitializeWnf`** (아래) → `+0x19e/+0x1a3` `RtlRunOnceComplete`.
    - 그다음 `+0xa7/+0xac` `RtlpCreateWnfUserSubscription` · `+0xc4/+0xc9` `RtlpCreateWnfNameSubscription` · `+0xde/+0xe3` `RtlpAddWnfUserSubToNameSub` (사용자 구독 · 이름 구독 · 이름에 붙이기) — 앞 둘은 힙·락뿐. 셋째 `RtlpAddWnfUserSubToNameSub +0xd5/+0xda` **`NtSubscribeWnfStateChange`** 1번 (`+0x7a ~ +0xc4` 의 두 갈래가 다 `+0xc6` 으로 와서 항상 부름).
    - `+0xe9/+0xee` `RtlGetCurrentServiceSessionId` 뒤 `+0xf2 MOV EDX,0x7ffe038e; CMP byte [RDX],0` — KUSD `UserModeGlobalLogger` 안 한 바이트. 0 이면 끝, 아니면 `RtlpWnfETWEventSubscribe` (`+0x198/+0x19d`, ETW 이벤트 쓰기). **test.exe: 0** `[관찰]` (`UserModeGlobalLogger` 32B 전부 0, P1a·P2 `dq 0x7ffe0380`).
  - **`RtlpInitializeWnf` (rva `0xf7230`)**: `+0x1d/+0x22` `RtlAllocateHeap(0x58)` · `+0x38` memset · **`+0x54/+0x59` `TpAllocTimer`** (인자: `RCX=&ctx+0x48`, `RDX=0x180114890` 콜백) · `+0x98/+0x9d` **`RtlpWnfRegisterTpNotification`** · 성공이면 `+0xa1 MOV [0x1801ce200],RBX`. 앞 두 `TpAllocTimer` / `RtlpWnfRegisterTpNotification` 이 syscall 의 덩어리다.
    - **`TpAllocTimer` (rva `0x84690`)**: `+0x72/+0x77` `RtlAllocateHeap(0x168)` → `+0xbd/+0xc2` `TppWorkInitialize` → `TppCleanupGroupMemberInitialize +0x15a/+0x15f` → **`TppPoolpReferenceGlobalPool`** (풀이 아직 없을 때만 `+0xc2/+0xc7` `TpAllocPoolInternal(&풀, 0)` 로 만든다. `+0x4d CMP [RBX],0`) → 돌아와 `+0xcd ~ +0xdf` `XADD.LOCK [풀+0x1b0],1` → **옛 값이 0 이면** `+0x16d/+0x172` `NtSetInformationWorkerFactory` (클래스 3, 값 1, 4바이트).
    - **풀 만들기 (`TpAllocPoolInternal` rva `0xe5a00`)**: `RtlImageNtHeaderEx` (exe 의 스택 예약·확정 크기 `[NT+0x60]`·`[NT+0x68]` 를 읽음, `+0x51/+0x56`) · 힙 4번 · `+0x240/+0x245` `TppPoolUpdateNodeRelation` (`+0x4b/+0x50` `NtQuerySystemInformationEx` 클래스 `0x6b` 입력 6 **2번**, `TppQueryMaximumGroupCount` 와 같은 "NULL 버퍼 → 길이 → 다시") · `+0x292/+0x297` `NtCreateIoCompletion` 1 · `+0x303/+0x308` `NtCreateWorkerFactory` 1 (시작 루틴 `0x18001a2e0` = 일꾼 스레드, 최대 `ActiveProcessorCount*4` 최소 `0x200` = `0x200`, 스택 크기는 exe 헤더 값) · `+0x373/+0x378` `TppGetCurrentThreadNumaNode` (`[풀+0x1a8]` 이 KUSD 와 같아 `TppAdjustRunningThreadGoalWithLock` 건너뜀) · `+0x38e/+0x393` `TppInitializeTimerQueue` → `TppInitializeTimerSubQueue` **2번**, 한 번마다 `NtCreateTimer2` · `NtCreateWaitCompletionPacket` · `NtAssociateWaitCompletionPacket` 각 1 (**6번**) · `+0x3fa ~ +0x415` 기본 CPU 집합은 `Count` = 0 이라 건너뜀 · `+0x4c1 ~ +0x4df` `TppETWPoolCreate` 는 KUSD `+0x386` = 0 이라 건너뜀 `[관찰]`.
    - 풀 만든 뒤 `TppPoolpReferenceGlobalPool +0x140/+0x145` `TpSetPoolMaxThreads` (`NtSetInformationWorkerFactory` 클래스 5, `0x300`) + `+0x181/+0x186` `TpSetPoolMaxThreadsSoftLimit` (클래스 14, `0x180`) **2번** (`TppPoolpGlobalPoolMaxThreads` `0x1d22ac` = 0 이라 `+0x1e3` 갈래 안 탐 — 끝까지 0 `[관찰]`). `TppPoolpGlobalPoolStackSize` (`0x1d2280`) 도 0 → `TpSetPoolStackInformation` 건너뜀.
    - → `TpAllocTimer` 안에서만 **13번**.
  - **`RtlpWnfRegisterTpNotification` (rva `0xf7314`)**: `+0x28/+0x2d` `NtCreateEvent` (접근 `0x1f0003`, 종류 1) 1 · `+0x47/+0x4c` `TpAllocWait` (`NtCreateWaitCompletionPacket` 1. 풀 개체 수 옛 값이 이미 ≥ 1 이라 `NtSetInformationWorkerFactory` 는 안 부름 — `TpAllocWait +0x115 ~ +0x122`) · `+0x77/+0x7c` **`NtSetWnfProcessNotificationEvent`** 1 · `+0x92/+0x97` `TpSetWaitEx` (`NtAssociateWaitCompletionPacket` 1 — `TpSetWaitEx +0x168/+0x16d`. 타임아웃이 NULL 이라 타이머는 안 검: `+0x180 JNZ` 안 탐) → **4번**.
  - `+0xa5/+0xaa` `RtlpEnumProcessHeaps(0x18009dae0 = RtlpSynchronizeHeapLoggingStateCallback, 0, 4)`: 프로세스 힙 목록을 훑으며 NT 힙마다 `RtlpHpHeapLoggingStateSync` (`RtlGetCurrentServiceSessionId` 뿐) — **syscall 없음** (`RtlEnterCriticalSection` 경합 때만).
  - 오류: `+0x8a JS 0x18009584b` (= `+0xb7`) 이면 `RtlRunOnceComplete(…, 4, 0)` 로 "실패" 표시 후 상태를 돌려줌. 이미 끝났으면 (`+0x1b JZ 0x180095847` = `+0xb3`) 0 돌려줌. 다른 스레드가 하는 중이면 (`+0xc8 JZ 0x18009586b` = `+0xd7`) `RtlpRunOnceWaitForInit` 후 `+0x26` 으로 되돌아감.
- (3) 하위 (call / ret): `+0x7a/+0x7f` `RtlpSubscribeWnfStateChangeNotificationInternal` · `+0x93/+0x98`, `+0xbe/+0xc3` `RtlRunOnceComplete` · `+0xa5/+0xaa` `RtlpEnumProcessHeaps` · `+0xe1/+0xe6` `RtlpRunOnceWaitForInit`.
- (4) 갈래: test.exe 에서 **줄기 전체를 탄다** `[정적]` + 사진 뒷받침 `[관찰]`: `TppPoolpGlobalPool` · `TppNumberNodes` 가 `≤P1a` (풀이 P1a 까지 만들어짐), `TppPoolpDefaultPoolCpuSetCount` 0 (집합 갈래 안 탐). run51 의 "`RtlpWnfRegisterTpNotification +0x7c NtSetWnfProcessNotificationEvent`" 줄기가 **맞다**; 다만 그 위 `RtlpInitializeWnf → TpAllocTimer` 에서 풀 만들기가 먼저 있다는 것이 run51·baggage 에 빠져 있었다.

### 5. `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` (rva `0x171708`, 155B, 호출 `+0xbee/+0xbf3`)

- **`0x1801cc6f0` 의 정체: TraceLogging 공급자 핸들 구조체 `0x38`바이트 (`0x1cc6f0 ~ 0x1cc727`). PDB 공개 이름 없음.** 바로 뒤 PDB 이름 `TppPoolpList` (`0x1cc728`) 가 이 구조체의 끝이고, 앞 PDB 이름 `NormalizationListHead` (`0x1cc6a8`) 는 따로 `0x48` 칸 (`NormalizationListHead+0x48` 은 "가장 가까운 앞 이름 + 거리" 일 뿐). 칸:
  | 칸 | 값 | 뜻 |
  |---|---|---|
  | `+0x00` dword | 0 | 켜짐 수준+1 (`_tlgEnableCallback +0x2f` 가 씀, `EtwSendNotification +0xfd` 등이 읽음) |
  | `+0x08` | `0x1801a3190` | 메타데이터 포인터. 이 앞 16바이트 (`0x1a3180`) = **공급자 GUID `{daf2c08d-5ac9-5066-ff0b-2d3d95275779}`**, 메타데이터 첫 word `0x20` = 길이, 이름 문자열 **`EtwUserModeDiagnosticProvider`** |
  | `+0x10` / `+0x18` | 0 | 키워드 any / all (`_tlgEnableCallback +0x31`·`+0x35`) |
  | `+0x20` | 0 | **RegHandle 8B** (`EtwEventRegister` 가 씀) |
  | `+0x28` / `+0x30` | 0 | 사용자 콜백 포인터·문맥 (이 함수가 `+0x39`·`+0x45` 에서 0 으로 다시 씀) |
  GUID 는 이름에서 만든 (3번째 묶음이 `5xxx`) TraceLogging GUID 다. 같은 모양의 쌍둥이가 `0x1cc670` (`RtlInitializeHeapLogging +0x37` 가 씀): 메타데이터 `0x1a3108`, 이름 **`Microsoft.Windows.Heap`**, GUID `{f0cd5b06-08c0-5ce4-09c2-17ba421985a0}`, 길이 `0x2c` (이름 뒤에 "그룹 GUID" 특성 `{4f50731a-89cf-4782-b3e0-dce8c90476ba}` 가 붙음).
- (1) 쓰는 것: 공급자 `+0x28`·`+0x30` = 0, `+0x20` ← RegHandle (`EtwEventRegister` 를 통해). 스택에 GUID 16B 복사 (`+0x2d ~ +0x31`).
- (2) syscall: 직접 없음. 아래로 간다 — `EtwEventRegister` (6번 표: `NtTraceControl(0xf)` 1 + **첫 호출이라 Once 4번**) + `EtwEventSetInformation → EtwpSetProviderTraits +0xdd/+0xe2` **`NtTraceControl(0x1e)`** 1번 (`LEA ECX,[R8+6]`, R8D=0x18). 한 호출의 합 = **6번**.
- (3) 하위 (call / ret): `+0x55/+0x5a` `EtwEventRegister` (`RCX` = 스택의 GUID 사본, `RDX` = `0x180171800` = `_tlgEnableCallback`, `R8` = 공급자 자신 (문맥), `R9` = 공급자 `+0x20`) · `+0x6e/+0x73` `EtwEventSetInformation` (`RCX` = 핸들, `EDX` = 2, `R8` = 메타데이터, `R9` = 길이 word) → `EtwpSetProviderTraits` 꼬리 `JMP` (`EtwEventSetInformation +0x2e`; 길이 검사 `+0x1c ~ +0x25`: 3 ~ 0xffff) · `+0x7d/+0x82` `__security_check_cookie`.
- (4) 갈래: **반환값은 `EtwEventRegister` 의 상태 (`+0x5a MOV EDI,EAX`)**; `EtwEventSetInformation` 의 오류는 버려진다 (`+0x73 MOV EAX,EDI`). `+0x5e JNZ 0x18017177b` (= `+0x73`) 이면 SetInformation 건너뜀. RegHandle 이 이미 0 이 아니면 `+0x37 JNZ 0x18017179b` (= `+0x93`) → `INT 0x29` (코드 5, 빠른 실패) — 처음 등록이라 안 탐. test.exe: 탄다 (이 구간의 **첫 ETW 등록**).

### 6. `EtwEventRegister` (rva `0x71450`, 72B) — 직접 호출 2번, 호출 횟수, syscall

- 시그니처 (공개 API `EventRegister` 와 같음): `RCX` = GUID 포인터, `RDX` = 콜백, `R8` = 콜백 문맥, `R9` = RegHandle 출력 포인터. `EtwNotificationRegister(RCX, EDX=3, R8=콜백, R9=문맥, [RSP+0x20]=RegHandle 포인터)` 로 넘김 (`+0x1e/+0x23`), 실패면 `+0x2b/+0x30` `RtlRestoreLastWin32Error` (별칭 `RtlSetLastWin32Error`). `+0x4 TEST RDX,RDX; +0x7 JZ 0x18007148d` (= `+0x3d`): 콜백이 NULL 이어도 문맥이 NULL 이면 통과 (`+0x40 JZ 0x180071459` = `+0x9`), 문맥이 있으면 `0x57` (잘못된 인자).
- **직접 호출 2번의 인자**
  | 호출 (call / ret) | `RCX` GUID (16B 그대로, `ntdll` 파일에서 직접 읽음) | `RDX` 콜백 | `R8` 문맥 | `R9` 핸들 출력 |
  |---|---|---|---|---|
  | `+0xc06/+0xc0b` | `PrivateLoggerNotificationGuid` `0x18017b4d0` = `5c ab 95 35 2a 04 8e 4c b9 42 2d 05 9b fe b1 b1` = **`{3595ab5c-042a-4c8e-b942-2d059bfeb1b1}`** | 0 | 0 | `g_hPrivLoggerNotificationProvider` `0x1d33c0` (8B) |
  | `+0xc23/+0xc28` | `UserDiagnosticGuid` `0x180190830` = `7b c8 5f 30 2a 00 26 5e d2 97 60 22 30 12 ca 9c` = **`{305fc87b-002a-5e26-d297-60223012ca9c}`** | `0x180147e40` = **`UserDiagnosticProviderCallback`** (23B) | 0 | `g_hUserDiagnosticProvider` `0x1d3d98` (8B) |
  - 콜백 `UserDiagnosticProviderCallback`: 제어 코드 (`EDX`) 가 1 이면 `g_isErrorOriginProviderEnabled` (`0x1ce75c`) = 1, 0 이면 0, 그 외 아무것도 안 함 (`+0x0 ~ +0x17`). **등록 때 곧바로 불리는 건 "그 공급자가 켜져 있을 때" 뿐**이다 (아래 `EtwpUpdateEnableInfoAndCallback`). 그 칸은 사진 "끝까지 0" `[관찰]`.
  - 두 반환값을 `LdrpInitializeProcess` 가 **검사하지 않는다** (`+0xc0b`·`+0xc28` 바로 다음이 `LEA`/`CALL`). 등록 실패는 프로세스 초기화를 막지 않는다.
- **핸들 출력 자리에 쓰는 것 (RegHandle, 8B)** — `EtwNotificationRegister +0x184 ~ +0x193`: `(registration의 순번 word [+0x54] << 32) | (핸들 표 칸 번호 dword)`. 실패하면 `+0x60` 에서 먼저 0 으로 쓴 그대로 둔다 (성공 경로만 `+0x193 MOV [R15],RCX`).
  - 순번 = `EtwpRegistrationSequence` (`0x1d33e4`, word) 에 `XADD +1` 한 값 +1 (`+0x10f ~ +0x11c`, 0 이 되면 한 번 더) → 첫 등록 1, 둘째 2, …
  - 칸 번호 = `ProviderHandleInsert` (rva `0x716c0`) 가 정한다: 자유 목록 머리 `[0x1ce2e0]` 가 0 이면 첫 덩어리 (`.rdata` `0x19aee8` 표: 첫 덩어리 `0x1e`=30칸) 를 `RtlAllocateHeap` 으로 만들어 칸 0 을 줌 (번호 **0**), 다음 칸부터 `(칸<<4)|1` (`0x11`, `0x21`, `0x31`, …).
  - → 정적 계산 `[정적]`: 4번 등록의 RegHandle 은 순서대로 **`0x1_00000000`** (A, TraceLogging) · **`0x2_00000011`** (B, `g_hPrivLoggerNotificationProvider`) · **`0x3_00000021`** (C, `g_hUserDiagnosticProvider`) · **`0x4_00000031`** (D, `RtlpHpStackTraceProviderHandle` `0x1ce888`). 숫자는 계산값이라 사진으로 확인 필요 (아래 동적 목록).
- **등록 한 번이 하는 일 (`EtwNotificationRegister`, rva `0x714a0`)**: 인자 검사 (`+0x29 ~ +0x3a`: GUID NULL · RegHandle 포인터 NULL → `0x57`) · GUID 가 `PrivateLoggerNotificationGuid` 면 `+0x5a JZ 0x18007169a` (= `+0x1fa`) 에서 `PrivateLoggerNotificationEntry` (`0x1d33c8`) 가 0 일 때만 계속 (0 이 아니면 `+0x207 JMP` → `0x57`; test.exe: `LdrpInitializeProcess +0xb32` 가 0 으로 만들어 둠) · `EtwpRegistrationCount` (`0x1d33e0`) +1 (`0x800` 이상이면 `0xe`) · 자유 목록 `EtwpFreeRegistrationList` (`0x1cdea0`, `+0xb19 ~ +0xb2b` 가 0 으로 만듦) 에서 꺼내려다 비어 있어 **`RtlAllocateHeap(프로세스 힙, 0x8 = 0으로 채움, 0x100)`** (`+0xa7 ~ +0xb4`; 등록 개체 `0x100`B 가 4개 생김) · 개체에 GUID `+0x20` · 콜백 `+0x30` · 문맥 `+0x38` · 순번 `+0x54` · 종류 `+0x56` (아래 12비트에 종류 3) · 만든 스레드 ID `+0x50` · `GS:[0x1720]` 의 아랫 dword `+0xf8` · SRW 락 `+0x40` 잡고 `ProviderHandleInsert` (`+0x13f/+0x144`) · **`EtwpRegisterProvider` (`+0x15c/+0x161`)** · `EtwpInsertRegistration` (`+0x16a/+0x16f`: `EtwpProvLock` 잡고 `EtwpRegistrationTable` `0x1d2238` 빨강검정 나무에 GUID 순으로 끼움 — 그 안 `+0xae/+0xb3` `RtlRbInsertNodeEx`) · `EtwpCheckForPrivatePreEnable` (`+0x172/+0x177`: `EtwpFindGuidEntry` 가 NULL 이면 곧장 끝 — `EtwpGuidEntryTable` 이 끝까지 0 `[관찰]` 이라 항상 곧장 끝) · 락 풂.
- **`EtwpRegisterProvider` (rva `0x72a70`) — syscall 이 어디 있나**
  - `+0x4f CMP byte [0x1d28c8],R14B` → `+0x61 JNZ 0x180072b84` (= `+0x114`): **`0x1d28c8` = `PebLdr.ShutdownInProgress`** (PDB `_PEB_LDR_DATA +0x48`, `PebLdr` = `0x1d2880`; `LdrpInitializeProcess +0x1ac` 가 0, `LdrShutdownProcess +0xae` 가 1). 0 이면 TP 알림 "Once" 를 거친다.
  - `+0x67 MOV RAX,[0x1d2268]` (`EtwpRegisterTpInitOnce`, RTL_RUN_ONCE 8B. 파일 값 0): 아래 두 비트 == 2 (끝남) 이면 `+0x84 JZ 0x180072b47` (= `+0xd7`) 로 건너뜀. 0 (처음) 이면 `+0x95 CMPXCHG.LOCK` 0→1 에 이긴 쪽이 `+0xa3 ~ +0xaf` **`EtwpRegisterTpNotificationOnce` 를 부르고** (call `+0xaf` / ret `+0xb4`) `+0xb7 ~ +0xc8` `RtlRunOnceComplete(&Once, 0, 0)` 로 끝 표시 (`+0xc0 JZ 0x180072cc1` = `+0x251` 은 실패 → `RtlRunOnceComplete(…, 4, 0)` 후 `EBP=0xc0000001` 로 등록 실패). 1 이면 (`+0xde CMP RCX,RSI` → `+0xe1 JZ 0x180072d09` = `+0x299`) `RtlpRunOnceWaitForInit`, 3 이면 `RtlReportCriticalFailure`.
  - 그다음 (Once 와 상관없이 **매 호출**) `+0x117 ~ +0x14d` **`NtTraceControl(ECX = 0xf, 입력 = 스택 0xa0B 블록, 출력 같은 블록)`**: 블록에 GUID `+0x0` · 종류 `+0x10` (=3) · 순번 `+0x14` · 콜백 `+0x20` 을 담아 보내고, 커널이 `+0x18` 에 커널 쪽 핸들을 돌려줌 (`+0x165 ~ +0x169` 가 개체 `+0x58` 에 저장). `+0x152 CMP EAX,0xc0000023` (버퍼 모자람) 이면 `+0x157 JZ 0x180072c59` (= `+0x1e9`) 에서 `RtlAllocateHeap` 으로 큰 버퍼를 받아 `+0x22f JNZ 0x180072b87` (= `+0x117`) 로 **되풀이** → 이 경우 한 등록의 `NtTraceControl(0xf)` 가 2번 이상. 보통 `0xa0` 으로 충분 `[추측]` (켜진 세션이 없을 때).
  - 성공하면 종류가 2 나 3 이라 (`+0x16d LEA ECX,[R15-2]; +0x171 CMP ECX,1; +0x174 JA`) `+0x17d/+0x182` `EtwpUpdateEnableInfoAndCallback`: 커널이 돌려준 "켜짐 정보" 의 `[+0x48]` (켜짐 여부) 가 0 이면 콜백 없이 끝 (`+0x98 JZ 0x180073871` = `+0xd1`, `+0xd4 JZ 0x180073853` = `+0xb3`). **켜져 있으면** `EtwpEventApiCallback` (`+0xae/+0xb3`) 으로 등록 콜백 (`UserDiagnosticProviderCallback`·`_tlgEnableCallback`·`RtlpHpStackTraceEtwCallback`) 을 부르고, 새 GUID 항목을 만들면서 (`EtwpAllocateUmGuidEntry`) `NtQueryVirtualMemory` 쪽 길 (`EtwpGetUmProcessImageInfo`) 까지 갈 수 있다 — **test.exe 는 안 간다** `[관찰]`: `EtwpGuidEntryTable` 이 끝까지 0 (항목이 한 번도 안 만들어짐), `UserModeGlobalLogger` 0.
  - GUID 가 `PrivateLoggerNotificationGuid` 면 `+0x186 ~ +0x19f` 에서 `PrivateLoggerNotificationEntry` ← 이 개체 (B 만).
- **`EtwpRegisterTpNotificationOnce` (rva `0x748f0`, 194B, 이름 그대로 "처음 한 번" 몫)**: `+0x32/+0x37` **`NtCreateEvent`** (접근 `0x1f0003`, 종류 1 = 동기화 이벤트, 처음 신호 안 줌) · `+0x4d/+0x52` **`TpAllocWait`** (`RCX` = 대기 개체 출력, `RDX` = `0x180072d30` = **`EtwpNotificationThread`** (대기 콜백), `R8` = 이벤트 핸들, `R9` = 0 환경 없음) → 안에서 **`NtCreateWaitCompletionPacket`** 1 (`TpAllocWait +0xba/+0xbf`) · `+0x64/+0x69` **`TpSetWaitEx`** (타임아웃 NULL) → **`NtAssociateWaitCompletionPacket`** 1 (`TpSetWaitEx +0x168/+0x16d`) · `+0x8c/+0x91` **`NtTraceControl(ECX = 0x1b, 입력 4B = 이벤트 핸들, 길이 4)`** 1 → 성공이면 `EAX = 1` 돌려줌 (`+0x95`). 실패 갈래 (`+0x39 JS`, `+0x54 JS`, `+0x93 JS` → `+0xa1`·`+0xb1`) 는 `TpReleaseWait`·`NtClose` 후 0 을 돌려줌. 그 이벤트가 신호되면 대기 콜백이 불린다 (신호는 커널이 `EtwDataSource` 알림으로 보냄 — run51/baggage 3.11).
  - 이 안의 **TP 개체 둘은 기본 스레드 풀이 이미 있을 때** 풀을 새로 만들지 않는다: `TpAllocWait +0x115 ~ +0x122` 의 `XADD [풀+0x1b0]` 옛 값이 ≥ 1 (4번 줄기 때문에 2) 이라 `+0x300` 의 `NtSetInformationWorkerFactory` 를 건너뛰고, `TppPoolpReferenceGlobalPool +0x4d CMP [RBX],0` 도 "이미 있음" 으로 빠진다. → **Once 갈래 = syscall 4번** (`NtCreateEvent`, `NtCreateWaitCompletionPacket`, `NtAssociateWaitCompletionPacket`, `NtTraceControl(0x1b)`).
- **`EtwEventRegister` 이 구간 호출 횟수와 syscall**
  | # | 부른 자리 (call / ret) | 공급자 | 이 호출의 syscall (순서대로) | 합 |
  |---|---|---|---|---|
  | A | `LdrpInitializeProcess +0xbee/+0xbf3` → `TraceLoggingRegisterEx… +0x55/+0x5a` | `EtwUserModeDiagnosticProvider` (TraceLogging, `0x1cc6f0`) | **Once 4번** (`NtCreateEvent`, `NtCreateWaitCompletionPacket`, `NtAssociateWaitCompletionPacket`, `NtTraceControl(0x1b)`) → `NtTraceControl(0xf)` → (돌아가 `EtwEventSetInformation`) `NtTraceControl(0x1e)` | **6** |
  | B | `LdrpInitializeProcess +0xc06/+0xc0b` | `PrivateLoggerNotificationGuid` | `NtTraceControl(0xf)` | 1 |
  | C | `LdrpInitializeProcess +0xc23/+0xc28` | `UserDiagnosticGuid` | `NtTraceControl(0xf)` | 1 |
  | D | `RtlInitializeHeapLogging +0x1e/+0x23` (호출 `LdrpInitializeProcess +0xc28/+0xc2d`) | `WindowsHeapSnapshotProvider` (`{901d2afa-4ff6-46d7-8d0e-53645e1a47f5}`, 콜백 `RtlpHpStackTraceEtwCallback`, 핸들 `RtlpHpStackTraceProviderHandle`) | `NtTraceControl(0xf)` | 1 |
  | (E) | `RtlInitializeHeapLogging +0x3e/+0x43` → `TraceLoggingRegisterEx… +0x55` (공급자 `Microsoft.Windows.Heap`, `0x1cc670`) | — | **안 탄다** `[관찰]` (`RtlpHpHeapFeatures` 비트 3 = 0). 탔다면 `NtTraceControl(0xf)` + `NtTraceControl(0x1e)` = 2 (Once 는 이미 끝남) | 0 |
  - **합: `EtwEventRegister` 4번 · syscall 9번** (`NtTraceControl` 6 = `0xf`×4 + `0x1b`×1 + `0x1e`×1, 그리고 `NtCreateEvent` 1 · `NtCreateWaitCompletionPacket` 1 · `NtAssociateWaitCompletionPacket` 1). 4번 **모두** syscall 까지 간다.
  - 되풀이가 붙는 곳: `NtTraceControl(0xf)` 와 `(0x1e)` 는 `0xc0000023` 이면 다시 (0x1e 는 `EtwpSetProviderTraits +0x1db CMP R15D,0x10` 까지 최대 16번). 이 VM 에서 켜진 세션이 있으면 늘 수 있다 — 동적.
  - 근거 `[관찰]` 으로 사진: `RtlpHpStackTraceProviderHandle`·`g_hPrivLoggerNotificationProvider`·`g_hUserDiagnosticProvider`·`EtwpRegisterTpInitOnce`·`PrivateLoggerNotificationEntry`·`EtwpRegistrationCount`·`EtwpRegistrationSequence`·`EtwpRegistrationTable` 모두 `≤P1a` (값 있음) → 네 등록이 P1a 전에 성공. `EtwpGuidEntryTable`·`EtwpFreeRegistrationList`·`g_isErrorOriginProviderEnabled` 끝까지 0 → 켜짐 정보로 콜백이 불린 흔적 없음, 등록 해제 없음.
  - **이 구간보다 앞에서는 안 불린다** (test.exe): `EtwNotificationRegister` 부르는 곳 6 중 init 길에 있는 것은 `EtwEventRegister` · `SbSelectProcedure +0x241/+0x27d` (`RtlInitializeHeapManager +0x56/+0x5b` 안 — `RtlpHpOptIntoSegmentHeap` 가 0 이라 **안 탐**, sast/40, `g_RegInfo+8`=0 `[관찰]`) · `SbObtainTraceHandle +0x83` (`LdrpInitializeProcess +0x19c8` — 이 구간 **뒤**). `EtwEventRegister` 부르는 곳 8줄 중 init 길: 위 A~D 와 `LdrpLogDeprecatedDllEtwEvent +0xdb` (`LdrpProcessWork` 안, 뒤) · 나머지 셋 (`LdrpAppxEtw*`, `LdrpLogFatalUserCallbackException`) 은 init 지도 밖.

### 7. `RtlInitializeHeapLogging` (rva `0x70420`, 68B, 호출 `+0xc28/+0xc2d`)

- (1) 쓰는 것: `RtlpHpStackTraceProviderHandle` (`0x1ce888`, 8B) ← RegHandle (`EtwEventRegister` 를 통해). 이 함수 자체는 전역을 직접 쓰지 않는다.
- (2) syscall: `+0x1e/+0x23` `EtwEventRegister` 안 `NtTraceControl(0xf)` **1번**. (E) 갈래는 안 탐.
- (3) 하위: `+0x1e/+0x23` `EtwEventRegister` (인자 `RCX`=`WindowsHeapSnapshotProvider` `0x18017b530`, `RDX`=`0x18014d680` `RtlpHpStackTraceEtwCallback`, `R8`=0, `R9`=`0x1ce888`) · `+0x3e/+0x43` `TraceLoggingRegisterEx…` (`RCX`=`0x1cc670`).
- (4) 갈래: `+0x23 TEST byte [RtlpHpHeapFeatures],8` → `+0x2c JNZ 0x180070457` (= `+0x37`) 이면 `+0x3e` 호출, 아니면 `+0x2e` 로 가서 반환 (`+0x2a MOV EBX,EAX` — 반환은 항상 첫 `EtwEventRegister` 의 상태). test.exe: **안 탄다** `[관찰]` — `RtlpHpHeapFeatures` 끝까지 0 (baggage 7.3, 비트 3 을 쓰는 곳 `RtlpHpSetLowFragHeapGlobalFlags +0x7a`·`+0x92` 는 LFH 전역 플래그 설정 때만). 반환값은 `LdrpInitializeProcess` 가 안 본다.

### 8. 54 바퀴 공사 중 셋 (`+0xaad ~ +0xb05`)

**(a) `Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_IsEnabledNoReportingNoInline`** (rva `0x11f8e4`, 36B, 호출 `+0xaad/+0xab2`)
- 무엇을 읽어 참/거짓을 정하나: 전역 상태 칸 `…__private_featureState` (`0x1d4f20`, dword) 를 읽어 (`+0x6`) **비트 1 (`0x2`) 이 켜져 있으면 비트 0 이 결과** (`+0x10 TEST AL,2`, `+0x12 JZ 0x18011f8fd` = `+0x19` 가 안 뛰면 `+0x14 AND EAX,1`, `+0x17 RET`). 꺼져 있으면 (처음) `+0x19 ~ +0x1e` 뒤 꼬리 `JMP` (`+0x20`) 로 `…IsEnabledFallback` (rva `0x11fc1c`, `R8` ← 기술자 `0x1b7f78`) → `wil_details_IsEnabledFallback` (rva `0xd4c0c`) 가 `wil_details_FeatureStateCache_ReevaluateCachedFeatureEnabledState` (`+0x32/+0x37`) → `wil_details_GetCurrentFeatureEnabledState` → `wil_RtlStagingConfig_QueryFeatureState` → `RtlQueryFeatureConfiguration(기능번호 0x02efc829, …)`. 결과는 `CMPXCHG.LOCK` 으로 상태 칸에 캐시 (비트 1 = "알아냄", 비트 0 = 켜짐; `Reevaluate +0xa0`). 기술자: `+0x00` → 상태 칸, `+0x18` 기능번호 `0x02efc829`, `+0x1c` 종류 3, `+0x1f` = 1 (**컴파일 때 기본값 = 켜짐**, 설정이 없을 때 쓰임), `+0x20` → 의존 목록 `0x180174ac8` (있음 — `wil_details_AreDependenciesEnabled`, 그 안은 안 따라감 `[공백]`).
- KUSD 칸: 직접은 안 읽지만 설정 버퍼의 신선도를 **`KUSER_SHARED_DATA +0x720` `FeatureConfigurationChangeStamp`** 와 견준다 (`RtlpFcReferenceFeatureConfigurationBuffers +0x6e MOV EBX,0x7ffe0720`; 버퍼 도장 `RtlpFcProcessManager +0xe0` < KUSD 도장이면 다시 받음). 이 VM: KUSD `+0x720` = 1 `[관찰]` (P1a·P2 `dq 0x7ffe0000`), `RtlpFcProcessManager` 는 `LdrpInitializeProcess +0x21c ~ +0x286` 가 0 으로 만든 직후라 도장 0 → **받아 온다** (`0 < 1`).
- syscall (이 프로세스의 **첫 기능 상태 검사** 라서 여기서 붙는다 — init 지도에서 `Feature_…IsEnabled` 부르는 곳은 이 둘이 처음이고 `LdrpInitializePolicy +0xfc5` 부터는 뒤): `RtlpFcReferenceFeatureConfigurationBuffers +0xcf/+0xd4` → **`RtlpFcUpdateLocalConfiguration`**: `+0xad/+0xb2` **`NtQuerySystemInformationEx` (클래스 `0xd3`, 입력 `0x20`B)** 1번 → 구역 4종 (`+0xea CMP EDI,R14D(4)`) 에 대해 `RtlpFcMapSingleBuffer +0x57/+0x5c` **`NtMapViewOfSection`** (커널이 돌려준 구역 핸들이 0 이 아닐 때만, 최대 4) → `RtlpFcBufferManagerUpdateBuffers` (`+0xff/+0x104`) → 남은 구역 핸들마다 `+0x137/+0x13c` **`NtClose`** (같은 개수). `NtUnmapViewOfSection` (`+0x175/+0x17a`) 는 처음엔 안 탄다. **k = 핸들이 0 이 아닌 구역 수 (0 ~ 4) 는 동적.** 합 = `1 + k + k` (≤ 9).
  - 그 뒤 `RtlpFcQueryFeatureConfigurationFromBufferSet` (`RtlQueryFeatureConfiguration +0x87/+0x8c`) 에서 값을 찾는다. 버퍼를 못 받았을 때만 (`+0x75 TEST EAX,EAX; +0x77 JS 0x1800d56fc` = `+0x10c`) `RtlpFcQueryFeatureConfigurationFromKernel` (`+0x115/+0x11a`) 안 `+0x51/+0x56` `NtQuerySystemInformationEx` 로 직접 물어본다 — 위에서 버퍼를 받았으니 test.exe 에선 안 탐 `[추측]`.
  - `g_wil_details_ensureSubscribedToFeatureConfigurationChanges` (`0x1ce3f0`)·`g_wil_details_subscribeFeatureStateCacheToConfigurationChanges` (`0x1ce3a8`) 는 "끝까지 0" `[관찰]` → 변경 구독 간접 호출 안 탐.
- (1) 쓰는 것: 상태 칸 `0x1d4f20` (사진 `≤P1a` 값 있음), `RtlpFcProcessManager` (`0x1d3c40`, `0x118`B, 설정 버퍼 두 벌 + 도장 — **P1a 에 채워짐**), 호출한 쪽이 `RtlpSrwLockAllowImplicitUpgrade` (`0x1cd090`, dword) ← `(EAX == 0) ? 1 : 0` (`+0xab2 MOV ECX,R15D; +0xab5 TEST EAX,EAX; +0xab7 SETZ CL; +0xaba MOV [..],ECX`). 읽는 곳: `RtlTryAcquireSRWLockShared +0x40`, `RtlpAcquireSRWLockSharedContended +0x4a` (`_refs/RtlpSrwLockAllowImplicitUpgrade.tsv`; 쓰는 곳은 `+0xaba` 하나).
- **"켜짐" 고정이 맞나: 맞다 `[관찰]`.** `+0xaba` 가 0 을 쓰려면 `EAX ≠ 0` (기능 켜짐), `RtlpSrwLockAllowImplicitUpgrade` 는 P1a(`+0x199d`, 이 쓰는 줄 **뒤**) ~ P2 모두 0 → 함수가 0 이 아닌 값을 돌려줬다. (PE_emulator 의 "켜짐" = `IsEnabled ≠ 0` → 전역 0.) 단 이 값은 **이 VM 의 기능 설정** (기술자 기본값은 켜짐) 에서 나온 것이다.

**(b) `Feature_Servicing_LoaderSnapsBuffer__private_IsEnabledDeviceUsageNoInline`** (rva `0x109dac`, 39B, 호출 `+0xac0/+0xac5`) → `LdrpInitializeSnapsBuffer` (rva `0xfb520`, 86B, `+0xac9/+0xace`)
- 판정 함수: 상태 칸 `…__private_featureState` (`0x1d4f40`) 를 읽어 **비트 4 (`0x10`) 가 켜져 있으면 비트 0** (`+0x10 TEST AL,0x10; +0x12 JNZ 0x180109dcf` = `+0x23`), 아니면 꼬리 `JMP` (`+0x1e`) 로 `…IsEnabledFallback` (rva `0x10e7b8`, 기술자 `0x1b8058`, **EDX = 3 = "기기 사용량 보고" 종류**) → `wil_details_IsEnabledFallback` → (a) 와 같은 `Reevaluate` (이미 받아 둔 버퍼를 씀: 이번엔 **추가 syscall 없음** 이 정상) → `+0x72/+0x77` `wil_details_FeatureReporting_ReportUsageToService` (사용량 장부 `…__private_reporting` `0x1d23e8` 에 `RecordUsageInCache` — **사진 `≤P1a` 값 있음 `[관찰]`**) → `+0x87/+0x8c` `wil_details_FeatureStateCache_TryEnableDeviceUsageFastPath` (상태 칸에 비트 4 를 `CMPXCHG` 로 켜서 다음부터 빠른 길). 기술자: 기능번호 `0x02967777`, 종류 3, 기본값 켜짐 (`+0x1f` = 1), 의존 목록 없음.
- **이 안에서 syscall 이 더 붙을 수 있는 곳**: `ReportUsageToServiceDirect +0x4a BT EBX,0xa` / `+0x4e JC 0x1800d5085` (= `+0x71`, 상태 비트 10) 이고 종류 `≠ 0xfe` (`+0x71 CMP ESI,0xfe`) 이면 `+0x9b/+0xa0` `RtlNotifyFeatureUsage` (rva `0xd53e0`) → `+0x46/+0x4b` `RtlpFcSendFeatureUsageNotifications` (rva `0xd5e20`) → `+0x8e/+0x93` `RtlpFcNotifyFeatureUsageTarget` (rva `0xd5ec0`) 안 **`NtQueryWnfStateData` (`+0x85/+0x8a`)·`NtUpdateWnfStateData` (`+0x107/+0x10c`)**. 이 VM 의 설정에서 보고가 켜져 있는지는 정적으로 못 정함 → 동적 (아래).
- **test.exe 에서 참인가: 참이다 `[관찰]`** — `LdrpSnapsUnicodeString` (`0x1d2750`)·`LdrpSnapsUnicodeString2` (`0x1d2740`) 가 사진 `≤P1a` 에 값이 있다 (둘 다 `LdrpInitializeSnapsBuffer` 가 씀: `+0x35`·`+0x3f` 길이/최대길이 dword `0x10000000` = 길이 0 · 최대 `0x1000`, `+0x25`·`+0x2c` 버퍼 포인터). 같은 4 KiB 블록을 두 문자열이 함께 가리킨다.
- `LdrpInitializeSnapsBuffer` 가 하는 일: (1) `+0x6 ~ +0x1b` **`RtlAllocateHeap(프로세스 힙 [PEB+0x30], 0, 0x1000)`** (call `+0x1b` / ret `+0x20`) 1번, (2) 성공이면 `LdrpSnapsUnicodeString+0x8` (`0x1d2758`) · `LdrpSnapsUnicodeString2+0x8` (`0x1d2748`) 에 그 포인터 (`+0x25`·`+0x2c`), 길이 dword 둘 (`+0x35`·`+0x3f`), 반환 0; 실패면 `+0x23 JZ 0x1800fb570` (= `+0x50`) 에서 `0xc0000017`. 호출한 쪽이 반환을 **검사하지 않는다** (`+0xace` 바로 `XOR R8D,R8D`). syscall 없음 (힙 확장 때만).
- **PE_emulator 가 건너뛰어도 되나**: 동작 면에선 거의 영향 없지만 **실제와 다르다**. 읽는 곳 `LdrpLogInternal +0x85 CMP [0x1d2758],R13 / +0x8c JZ 0x180011ef4` — NULL 이면 로그 서식 만들기(`RtlStringCchPrintfExW`·`_vsnprintf`·`LdrpAddUnicodeStringToSnapsBuffer`)를 통째로 건너뛴다. 실제 test.exe 는 버퍼가 있어서 로더 로그가 (수준 2~4 가 아니면, `+0x7c ~ +0x7f`) **스냅 켜짐 여부와 상관없이 그 버퍼에 쌓인다**. 건너뛰면: ① 프로세스 힙에서 `0x1000`B 블록 하나(+머리)가 빠져 이후 힙 주소·사용량이 달라지고 ② 위 전역 4칸이 0 으로 남고 ③ 이후 `LdrpLogInternal` 호출이 서식 만들기를 안 한다. syscall 횟수는 같다.

**(c) `RtlInitializeExceptionLog`** (rva `0x122248`, 73B, 호출 `+0xb00/+0xb05`)
- 조건: `+0xaf4 TEST dword [RDI+0xbc],0x800000` → `+0xafe JZ 0x18008cf09` (= `+0xb05`) 이면 건너뜀. **RDI = PEB** (`+0x14c MOV RDI,[R13+0x60]`, `R13` = `GS:[0x30]` 이 TEB, 그 `+0x60` = PEB), `PEB+0xbc` = **`NtGlobalFlag`** (PDB `_PEB`), 비트 `0x800000` = 예외 기록 켜기 플래그 (`FLG_ENABLE_EXCEPTION_LOGGING` `[추측]` 이름).
- (1) 타면 쓰는 것: `+0x19/+0x1e` `RtlAllocateHeap(프로세스 힙, 0, 0x11620)` → `RtlpExceptionLog2` (`0x1ce838`, 8B) ← 포인터 (`+0x1e`), NULL 이면 `+0x28 JZ 0x18012228c` (= `+0x44`) 로 끝, 아니면 `+0x35/+0x3a` memset(0, `0x11620`) → `RtlpExceptionLogSize` (`0x1d3d60`, dword) ← `0x32` (`+0x3a`).
- (2) syscall 없음 (힙 확장 때만). (3) 하위: `+0x19/+0x1e` `RtlAllocateHeap` · `+0x35/+0x3a` memset.
- (4) test.exe: **안 탄다** `[관찰]` — `PEB.NtGlobalFlag` 0 (P1·P1a `NtGlobalFlag : 0`), `RtlpExceptionLog2`·`RtlpExceptionLogSize` 사진 "끝까지 0". 반환값은 호출한 쪽이 안 본다.

### 9. 매개변수 블록 `+0x430 ~ +0x43c` — asm 과 PDB 맞춤

- PDB (`pdb_struct.py ntdll.dll _RTL_USER_PROCESS_PARAMETERS`, 크기 `0x450`) 와 asm 이 **그대로 맞는다**: `+0xb90 MOV EDX,[R13+0x438]` (4B) = `DefaultThreadpoolCpuSetMaskCount` · `+0xb9b MOV RCX,[R13+0x430]` (8B 포인터) = `DefaultThreadpoolCpuSetMasks` · `+0xba7 MOV EBX,[R13+0x43c]` (4B) = `DefaultThreadpoolThreadMaximum`. 다음 칸은 `+0x440 HeapMemoryTypeMask` (**4B**, 요청서의 8B 표기가 아님), 그 뒤 `+0x448 AttributeList` (포인터). R13 은 `+0x6c5` 의 `RtlNormalizeProcessParams` 반환 (같은 블록 주소를 그대로 돌려줌).
- 값: 위 사진 8곳 모두 0 (`+0x430` null, `+0x438`·`+0x43c`·`+0x440` 0).

## syscall 표 (test.exe 길, 정적 셈)

호출은 `call +0x../ret +0x..`. "탄다" 는 위 갈래 근거 기준 (`[관찰]` 표시한 것은 사진 뒷받침).

### `+0xb83 ~ +0xc2d` 구간 (요청서 본문) — 합계 **30번**

| # | 함수 / 사슬 | NtXxx | 횟수 | 조건 | test.exe |
|---|---|---|---|---|---|
| 1 | `TpInitializePackage +0x61/+0x66` | `NtQuerySystemInformation` (클래스 `0x37`) | 1 | 항상 | 탄다 |
| 2 | `TpInitializePackage +0x80/+0x85` → `TppQueryMaximumGroupCount +0x44/+0x49` | `NtQuerySystemInformationEx` (클래스 `0x6b`, 입력 4) | 2 | 첫 번째는 길이만 받음 (`0xc0000004`), 둘째가 본 호출. 길이가 그 사이 커지면 더 | 탄다 |
| — | `TpSetDefaultPoolCpuSets` · `+0xba7` 블록 | (없음) | 0 | `+0x438`/`+0x43c` ≠ 0 일 때만 실행되는 쪽 (실행돼도 syscall 없음) | **안 탄다** `[관찰]` |
| 3 | `LdrpEnableUMGLTracingStateSync +0x7a/+0x7f` → `RtlpSubscribeWnf… +0x189/+0x18e` → `RtlpInitializeWnf +0x54/+0x59` → `TpAllocTimer` → … → `TpAllocPoolInternal` | `NtQuerySystemInformationEx` (`0x6b`, 입력 6) | 2 | 풀 만들 때 (`TppPoolUpdateNodeRelation +0x4b/+0x50`) | 탄다 |
| 4 | 〃 `TpAllocPoolInternal +0x292/+0x297` | `NtCreateIoCompletion` | 1 | 〃 | 탄다 |
| 5 | 〃 `TpAllocPoolInternal +0x303/+0x308` | `NtCreateWorkerFactory` | 1 | 〃 | 탄다 |
| 6 | 〃 `TpAllocPoolInternal +0x38e/+0x393` → `TppInitializeTimerQueue +0x2c/+0x31`·`+0x44/+0x49` → `TppInitializeTimerSubQueue +0x4d/+0x52`·`+0x65/+0x6a`·`+0xa0/+0xa5` | `NtCreateTimer2` · `NtCreateWaitCompletionPacket` · `NtAssociateWaitCompletionPacket` 각 | 각 2 (=6) | 〃 (큐 둘) | 탄다 |
| 7 | 〃 `TppPoolpReferenceGlobalPool +0x140/+0x145`, `+0x181/+0x186` | `NtSetInformationWorkerFactory` (클래스 5 = `0x300`, 클래스 14 = `0x180`) | 2 | 새 전역 풀 (`TppPoolpGlobalPoolMaxThreads` = 0) | 탄다 |
| 8 | 〃 `TpAllocTimer +0x16d/+0x172` | `NtSetInformationWorkerFactory` (클래스 3, 값 1) | 1 | 풀의 첫 TP 개체 (옛 개수 0) | 탄다 |
| 9 | `RtlpInitializeWnf +0x98/+0x9d` → `RtlpWnfRegisterTpNotification +0x28/+0x2d` | `NtCreateEvent` | 1 | 항상 | 탄다 |
| 10 | 〃 `+0x47/+0x4c` → `TpAllocWait +0xba/+0xbf` | `NtCreateWaitCompletionPacket` | 1 | 〃 (`NtSetInformationWorkerFactory` 는 옛 개수 ≥ 1 이라 안 부름) | 탄다 |
| 11 | 〃 `+0x77/+0x7c` | `NtSetWnfProcessNotificationEvent` | 1 | 항상 | 탄다 |
| 12 | 〃 `+0x92/+0x97` → `TpSetWaitEx +0x168/+0x16d` | `NtAssociateWaitCompletionPacket` | 1 | 항상 | 탄다 |
| 13 | `RtlpSubscribeWnf… ` → `RtlpAddWnfUserSubToNameSub +0xd5/+0xda` | `NtSubscribeWnfStateChange` | 1 | 항상 | 탄다 |
| — | 〃 `+0xa5/+0xaa` `RtlpEnumProcessHeaps` → 콜백 | (없음) | 0 | — | — |
| 14 | A: `+0xbee/+0xbf3` → `TraceLoggingRegisterEx… +0x55/+0x5a` → `EtwEventRegister +0x1e/+0x23` → `EtwNotificationRegister +0x15c/+0x161` → `EtwpRegisterProvider +0xaf/+0xb4` → `EtwpRegisterTpNotificationOnce +0x32/+0x37` | `NtCreateEvent` | 1 | **첫** `EtwpRegisterProvider` 만 (Once 칸 처음 값 0, 종료 중 아님) | 탄다 |
| 15 | 〃 `… +0x4d/+0x52` → `TpAllocWait +0xba/+0xbf` | `NtCreateWaitCompletionPacket` | 1 | 〃 | 탄다 |
| 16 | 〃 `… +0x64/+0x69` → `TpSetWaitEx +0x168/+0x16d` | `NtAssociateWaitCompletionPacket` | 1 | 〃 | 탄다 |
| 17 | 〃 `… +0x8c/+0x91` | `NtTraceControl` (코드 `0x1b`, 입력 4B) | 1 | 〃 | 탄다 |
| 18 | A·B·C·D 각각의 `EtwpRegisterProvider +0x14d/+0x152` | `NtTraceControl` (코드 `0xf`) | 4 (A·B·C·D 각 1) | 매 등록. `0xc0000023` 이면 되풀이 | 탄다 |
| 19 | A 의 `TraceLoggingRegisterEx… +0x6e/+0x73` → `EtwEventSetInformation +0x2e` → `EtwpSetProviderTraits +0xdd/+0xe2` | `NtTraceControl` (코드 `0x1e`) | 1 | TraceLogging 등록만 | 탄다 |
| — | E: `RtlInitializeHeapLogging +0x3e/+0x43` | (`NtTraceControl` 0xf + 0x1e) | 0 (탔다면 2) | `RtlpHpHeapFeatures & 8` | **안 탄다** `[관찰]` |

- 합: 1 (표 1) + 2 (표 2) + [2 + 1 + 1 + 6 + 2 + 1 + 1 + 1 + 1 + 1 + 1 = 18] (표 3 ~ 13) + [1 + 1 + 1 + 1 + 4 + 1 = 9] (표 14 ~ 19) = **30**. 줄기별로는 `TpInitializePackage` 3 · `LdrpEnableUMGLTracingStateSync` 18 (그중 풀 만들기 12 + 첫 개체 1) · ETW 9.
- 시간 순서 (A 의 안): Once 네 개 → `NtTraceControl(0xf)` → `NtTraceControl(0x1e)`. 그다음 B, C, D 가 각 `NtTraceControl(0xf)`.
- 핸들을 만드는 것 (`HandleCount` 와 맞추기): 이벤트 2 (WNF 몫 1 + ETW 몫 1) · 대기 패킷 4 (WNF 1 + ETW 1 + 타이머 큐 2) · IO 완료 포트 1 · 일꾼 공장 1 · 타이머 2 = **핸들 10개** 가 이 구간에서 남는다 (P1 4개 → P1a 26개 증가분 22 중 일부 `[추측]`).

### 54 공사 중 셋 (`+0xaad ~ +0xb05`) — 합계 **`1 + 2k` (k = 0 ~ 4)**

| # | 함수 / 사슬 | NtXxx | 횟수 | 조건 | test.exe |
|---|---|---|---|---|---|
| a1 | (a) 첫 검사 → `…IsEnabledFallback` → `wil_details_IsEnabledFallback +0x32/+0x37` → `Reevaluate +0x3c/+0x41` → `GetCurrentFeatureEnabledState +0x40/+0x45` → `wil_RtlStagingConfig_QueryFeatureState +0x42/+0x47` → `RtlQueryFeatureConfiguration +0x6c/+0x71` → `RtlpFcReferenceFeatureConfigurationBuffers +0xcf/+0xd4` → `RtlpFcUpdateLocalConfiguration +0xad/+0xb2` | `NtQuerySystemInformationEx` (클래스 `0xd3`) | 1 | 설정 버퍼 도장 < KUSD 도장 (0 < 1) | 탄다 |
| a2 | 〃 `+0xdd/+0xe2` → `RtlpFcMapSingleBuffer +0x57/+0x5c` | `NtMapViewOfSection` | k (0 ~ 4) | 커널이 준 구역 핸들 ≠ 0 마다 | **동적** |
| a3 | 〃 `+0x137/+0x13c` | `NtClose` | k | a2 와 같은 개수 | **동적** |
| b | (b) `…DeviceUsageNoInline` → … `ReportUsageToServiceDirect +0x9b/+0xa0` → `RtlNotifyFeatureUsage` → `RtlpFcNotifyFeatureUsageTarget +0x85/+0x8a`·`+0x107/+0x10c` | `NtQueryWnfStateData` · `NtUpdateWnfStateData` | 0 또는 각 1 이상 `[추측]` | 사용량 보고가 켜진 설정일 때만 | **동적** (이 VM 설정) |
| b2 | (b) `LdrpInitializeSnapsBuffer +0x1b/+0x20` | (없음, `RtlAllocateHeap`) | 0 | — | 탄다 `[관찰]` |
| c | (c) `RtlInitializeExceptionLog` | (없음) | 0 | `NtGlobalFlag & 0x800000` | **안 탄다** `[관찰]` |

## 동적분석으로 확인해야 할 것 (Q104 VM 단계에 얹을 수 있게)

타깃 VM 에서 test.exe 가 `LdrpInitializeProcess` 를 도는 동안만. 주소는 `ntdll` 이미지 기준 시작 + rva. (조건은 test.exe 프로세스만: `/p <test EPROCESS>`)

| # | 멈춤점 (rva) | 찍을 것 | 확인하는 정적 결론 |
|---|---|---|---|
| D1 | `ntdll+0x8ceb1` (`+0xaad`), `+0x8cf87` (`+0xb83`), `+0x8cf8c` (`+0xb88`), `+0x8d031` (`+0xc2d`) 사이 | 이 사이 syscall 개수와 종류. 가장 간단한 길: `ntdll!NtTraceControl` (`+0x164860`)·`NtCreateEvent` (`+0x1616b0`)·`NtCreateWaitCompletionPacket` (`+0x1627e0`)·`NtAssociateWaitCompletionPacket` (`+0x161fe0`)·`NtSetInformationWorkerFactory` (`+0x1643c0`)·`NtQuerySystemInformationEx` (`+0x163b60`)·`NtMapViewOfSection` (`+0x1612b0`) 입구에 시간 순 기록 (`ecx` 가 `NtTraceControl` 코드) | 표 합계 30 (구간), `1 + 2k` (54 공사 중) |
| D2 | `NtTraceControl` 입구 `ecx`; 반환 | 코드 `0xf` ×4, `0x1b` ×1, `0x1e` ×1 이고 **`0xc0000023` 로 되풀이한 적이 있나** (`ntdll+0x72bc2` = `EtwpRegisterProvider +0x152` 에서 `eax`, `ntdll+0x71242` = `EtwpSetProviderTraits +0xe2` 에서 `eax`) | 6번 (되풀이 0) |
| D3 | `ntdll+0x72abf` (`EtwpRegisterProvider +0x4f`) | `PebLdr+0x48` (`ntdll+0x1d28c8`) = 0, 그리고 `ntdll+0x1d2268` (Once 칸) 이 처음엔 0, 첫 호출 뒤 2 | Once 가 첫 호출 한 번뿐 |
| D4 | `ntdll+0x72bed` (`EtwpRegisterProvider +0x17d` 호출 자리), `ntdll+0x7384e` (`EtwpUpdateEnableInfoAndCallback +0xae`), `ntdll+0x73a60` (`EtwpEventApiCallback` 입구) | 콜백까지 가는지. 안 걸려야 정상 (A ~ D 모두) | 켜진 세션 없음 |
| D5 | 등록 직후 (`EtwEventRegister` 반환) 에서 `[ntdll+0x1cc710]` (A 의 RegHandle), 구간 끝에서 `dq ntdll+0x1d33c0 L1`·`ntdll+0x1d3d98 L1`·`ntdll+0x1ce888 L1` | `0x100000000`·`0x200000011`·`0x300000021`·`0x400000031` 인가 | RegHandle 계산값 |
| D6 | `ntdll+0x70443` (`RtlInitializeHeapLogging +0x23`) | `[ntdll+0x1ce6d0] & 8` = 0 (E 안 탐) | 둘째 TraceLogging 안 탐 |
| D7 | `ntdll+0xd5962` (`RtlpFcUpdateLocalConfiguration +0xb2`, `NtQuerySystemInformationEx(0xd3)` 가 돌아온 자리) | 출력 버퍼 (호출 때 `R9` = 그 함수의 `rsp+0x60`, 0x68B) 안 구역 핸들 4칸 (버퍼 시작 기준 `+0x10`·`+0x28`·`+0x40`·`+0x58`, 칸 간격 `0x18`) 중 0 이 아닌 수 = k. 그리고 `NtMapViewOfSection` (`ntdll+0xd5b67` = `RtlpFcMapSingleBuffer +0x57` 호출 자리)·`NtClose` (`ntdll+0xd59e7` = `+0x137`) 실제 횟수 | k |
| D8 | `ntdll+0xdc4e5` (`TpInitializePackage +0x61`) 반환 (`ntdll+0xdc4ea`) 뒤 `[rsp+0x440]`·`[rsp+0x20]`; `ntdll+0xdc838` (`TppQueryMaximumGroupCount +0x44`) 가 2번 | 각 호출 반환 상태: 1번째 `0xc0000004`, 2번째 `0`. `TppNumberNodes` (`ntdll+0x1d2274`)·`TppMaximumGroups` (`ntdll+0x1d2270`) 값 | 표 1·2 |
| D9 | `ntdll+0xe5a00` (`TpAllocPoolInternal` 입구), `ntdll+0xe5c92` (`+0x292` `NtCreateIoCompletion`), `ntdll+0xe5d03` (`+0x303` `NtCreateWorkerFactory`), `ntdll+0x857f0` (`TppPoolpReferenceGlobalPool +0x140` `TpSetPoolMaxThreads`) | 입구가 **1번만** 걸리고 그게 `LdrpEnableUMGLTracingStateSync` 안 (`ntdll+0xf7284` `RtlpInitializeWnf +0x54` 뒤) 인지, ETW Once 때 (`ntdll+0x7493d` 뒤) 는 안 도는지 | 풀 만들기 1번 |
| D10 | `ntdll+0xf7284` (`RtlpInitializeWnf +0x54`), `ntdll+0xf738b` (`RtlpWnfRegisterTpNotification +0x77`), `ntdll+0x97a35` (`RtlpAddWnfUserSubToNameSub +0xd5`) | `NtSetWnfProcessNotificationEvent`·`NtSubscribeWnfStateChange` 각 1번. `NtSubscribeWnfStateChange` 가 바로 신호를 줘서 **일꾼 스레드가 만들어지는지** (스레드 수·`TppWorkerThread`) | 풀 만들기 뒤 스레드 |
| D11 | `ntdll+0x8cebe` (`+0xaba`) 에서 `ecx`, `ntdll+0x8cef8` (`+0xaf4`) 에서 `dword [rdi+0xbc]` | `ecx` = 0 이어야 (기능 켜짐), `[rdi+0xbc] & 0x800000` = 0 (예외 기록 안 탐) | 8(a) 켜짐, 8(c) 안 탐 |
| D12 | `ntdll+0x8cec9` (`+0xac5` ret) 에서 `eax` ≠ 0, 그 뒤 `LdrpInitializeSnapsBuffer` (`ntdll+0xfb520`) 가 `RtlAllocateHeap` 하나 | `eax` ≠ 0 (참), 만들어진 블록 주소 = `[ntdll+0x1d2758]` | 8(b) 참 |
| D13 | `ntdll+0xd5ec0` (`RtlpFcNotifyFeatureUsageTarget` 입구) | 걸리는지 (걸리면 (b) 에 `NtQueryWnfStateData`·`NtUpdateWnfStateData` 가 붙음) | 8(b) 보고 갈래 |

(주소 = 함수 rva + 오프셋, 요청서 `이미 아는 것` 의 `LdrpInitializeProcess` rva `0x8c404` 와 같은 방식. 예: `+0xb83` = `0x8c404 + 0xb83` = `0x8cf87`.)

정적으로 못 정한 것 (동적 필요):
1. 8(a) 의 k (구역 핸들이 0 이 아닌 수) — 반드시 동적.
2. "ETW 세션이 이 4개 공급자 GUID 를 켜 놨는가" — 사진은 흔적 없음 (`EtwpGuidEntryTable` 0 등), 하지만 `0xc0000023` 되풀이는 동적으로만.
3. (b) 의 사용량 보고 갈래의 `NtQueryWnfStateData`/`NtUpdateWnfStateData`.
4. `NtSubscribeWnfStateChange`·`NtSetWnfProcessNotificationEvent` 직후 커널이 일꾼 스레드를 만들어 주는지 (스레드 수).
5. `RtlQueryInformationActivationContext` (`TppCleanupGroupMemberInitialize +0xf3/+0xf8`, 클래스 1) 안은 호출 목록에 syscall 이 안 보여 "없음" 으로 셌다 `[정적, 정밀 추적은 안 함]`. 일반 힙 (`RtlAllocateHeap`) 이 힙을 늘리는 `NtAllocateVirtualMemory` 도 센 합계에 안 들어 있다 (공용 부품).

## 새로 알게 된 것 / 기존 문서와 다른 점 (flow-writer·baggage 에 넘길 것)

1. **flow.md `+0xc0b / +0xc28 EtwEventRegister → … → EtwpRegisterTpNotificationOnce`** 와 run51 표 "`LdrpInitializeProcess +0xc06/+0xc23 → EtwEventRegister`" 는 Once 를 타는 쪽이 틀렸다. Once 는 **첫 번째 `EtwpRegisterProvider`** = **`+0xbee → TraceLoggingRegisterEx… +0x55`** 한 번뿐이다.
2. **기본 스레드 풀은 `+0xbe2 LdrpEnableUMGLTracingStateSync` 가 처음 만든다** (`RtlpInitializeWnf +0x54 TpAllocTimer`). 풀 만들기 12 syscall + 커널 개체 (IO 완료 포트·일꾼 공장·타이머 2·대기 패킷 2) 와 전역 (`TppPoolpGlobalPool` 등), 일꾼 공장 최대 `0x300` / 소프트 `0x180` / 생성 최대 `0x200` — baggage 3.8 (ntdll 전역)·3.11 (커널 개체) 에 없다.
3. `0x1801cc6f0` = 이름 없는 TraceLogging 공급자 `EtwUserModeDiagnosticProvider` (`0x38`B). `0x1cc670` = `Microsoft.Windows.Heap` (안 탐). 이 전역 칸들은 baggage 7.3 에 이름 없는 `.data` 로 남았을 수 있다.
4. `0x1d23e0` = 이름 없는 RTL_RUN_ONCE (Ghidra `g_pShimmedModuleList+0x8` 는 틀림).
5. `RtlpFcProcessManager` (`0x1d3c40`) 는 run59 에서 "전부 0" 으로 적었으나 **`+0xaad` 의 첫 기능 상태 검사에서 채워진다** (`RtlpFcBufferManagerUpdateBuffers`, 버퍼 두 벌). 구역 syscall `NtQuerySystemInformationEx(0xd3)` + `NtMapViewOfSection`×k + `NtClose`×k 도 짐 지도에 없다.
6. (b) `LdrpSnapsUnicodeString{,2}` 와 `…LoaderSnapsBuffer…featureState` 는 사진 `≤P1a` 에서 값이 있다 → PE_emulator 의 "스냅 버퍼 건너뜀" 은 실제와 다르다 (위 8(b)).
7. TpInitializePackage 의 실패는 곧 `LdrpInitializeProcess` 의 반환값이다 (`+0xa7b` 공통 끝). 이 뒤 `_LdrpInitialize` 가 어떻게 하는지는 안 봤다.

## 판정

**분석성공.** 요청서 1 ~ 9 의 질문에 답과 `+0x` 근거가 있다. `EtwEventRegister` 횟수 (4번) · syscall 이름·횟수 (9번, 구간 30번) · Once 갈래 (첫 호출 한 번, 4번) 는 정적으로 닫혔고 test.exe 의 갈래는 사진 `[관찰]` 로 뒷받침했다. 동적이 꼭 필요한 것은 8(a) 의 `k`(구역 핸들 수), `0xc0000023` 되풀이 여부, (b) 보고 갈래, 일꾼 스레드 생성 여부 (위 목록, D1 ~ D13). dast 요청 필요: 위 D1·D2·D5·D7·D10·D12 를 Q104 VM 단계에 얹기.

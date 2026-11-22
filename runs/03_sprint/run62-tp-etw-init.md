# 2026-11-03 run62 — 스레드 풀·ETW 등록 구간 (Q118, PE_emulator 요청 10): `LdrpInitializeProcess +0xb83 ~ +0xc2d` 의 syscall 은 **30번** (정적). 기본 스레드 풀은 `+0xbe2 LdrpEnableUMGLTracingStateSync` 가 **처음 만든다** (풀 만들기 12 + 첫 개체 1). ETW "Once" (`NtTraceControl(0x1b)` 등 4번) 는 **`+0xbee` TraceLogging 첫 등록 한 번뿐** — run51 의 `+0xc06`·`+0xc23` 아래가 아니다 (정정). 앞 `+0xaad` 의 첫 기능 상태 검사가 `RtlpFcProcessManager` 를 채우고 설정 구역 syscall `1 + 2k` 를 부른다

> 정정 (run69, sast/64): 아래 "결과서와 다르게" 의 `[공백]` — `TpAllocPoolInternal +0x316` 의 `[RSP+0xb8]` 출처 (test.exe 가 `+0x443/+0x448` 클래스 `0xd` 갈래를 타나, 타면 구간 31) — 는 **닫혔다: 기본 풀 길은 안 탄다.** `[RSP+0xb8]` = 둘째 인자 `& 1` (`+0x2f AND EDX,R12D` · `+0x32`) 이고, 기본 풀은 `TppPoolpReferenceGlobalPool +0xbb XOR EDX,EDX` 로 0 을 넘긴다. 이 갈래는 로더 풀 (`LdrpEnableParallelLoading +0x38 MOV EDX,1`) 만 탄다. 그래서 `+0xb83 ~ +0xc2d` syscall 은 **30 그대로**. D9 의 `ntdll+0xe5e43` 덧붙임은 필요 없다. 자세히는 `runs/03_sprint/run69-loader-pool.md`.

> 보탬 (run125, sast/122): 아래 동적 **D10** ("WNF 두 syscall 직후 일꾼 스레드가 생기나") 의 정적 기대 = **안 생김** `[추측: 그때 포트가 비었다면]` — 커널 `NtCreateWorkerFactory` 는 일꾼을 안 만들고, 뒤 클래스 5 (옛 최대 `0x200` ≠ 0) · 클래스 3 (`ExpTryEnterWorkerFactoryAwayMode` 는 포트가 비면 거짓) 은 일이 없으면 만들기로 안 간다. 아래 `ActiveThreads` P1a 1 `[관찰]` 과 맞는다. `runs/03_sprint/run125-worker-factory.md` 8절.

## PE_emulator 쪽에서 쓸 것

**결론 셋.**

- **`+0xb83 ~ +0xc2d` syscall 약속 = 30번** (test.exe 길, 정적 셈). 줄기별로 `TpInitializePackage` 3 · `LdrpEnableUMGLTracingStateSync` 18 · ETW 등록 9. 표는 아래 1절.
  - 전제 (결과서): ETW 세션이 이 공급자들을 켜 두지 않았다, 버퍼 모자람 (`0xc0000023`) 되풀이가 없다, 힙이 커지느라 부르는 `NtAllocateVirtualMemory` 는 안 센다.
- **앞 구간 `+0xaad ~ +0xb05` (54 바퀴 "공사 중" 셋) = `1 + 2k` 번** (k = 0 ~ 4, **동적**). 첫 기능 상태 검사라 설정 구역을 받아 온다.
  - (a) `Feature_…DisallowSharedLockImplicitUpgrade` "켜짐" 고정은 **맞다** (`RtlpSrwLockAllowImplicitUpgrade` = 0, 사진 P1a ~ P2).
  - (b) 스냅 버퍼는 **test.exe 에서 실제로 만들어진다** (`LdrpSnapsUnicodeString{,2}` 사진 `≤P1a` 에 값). 건너뛰면 syscall 수는 같지만 힙 블록 `0x1000` 하나·전역 4칸·이후 `LdrpLogInternal` 서식 만들기가 빠진다.
  - (c) `RtlInitializeExceptionLog` 는 **안 탄다** (`NtGlobalFlag` 0).
- **`EtwEventRegister` 는 이 구간에서 4번** (직접 2 + `TraceLoggingRegisterEx…` 안 1 + `RtlInitializeHeapLogging` 안 1). 4번 **모두** `NtTraceControl(0xf)` 를 1번씩 부르고, **첫 번째 (A, `+0xbee`) 만** 그 앞에 Once 4번, 뒤에 `NtTraceControl(0x1e)` 1번이 붙는다 → 6 + 1 + 1 + 1 = 9.

오프셋 읽는 법 (run60·run61 과 같음):

- `call +0x..` = CALL 명령 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 값.

### 1. syscall 표 — `+0xb83 ~ +0xc2d` `[정적]` (결과서 "syscall 표" 그대로, 사슬은 call/ret)

| # | 함수 / 사슬 | NtXxx | 횟수 | 조건 | test.exe |
|---|---|---|---|---|---|
| 1 | `TpInitializePackage +0x61/+0x66` | `NtQuerySystemInformation` (클래스 `0x37`) | 1 | 항상 | 탄다 |
| 2 | `TpInitializePackage +0x80/+0x85` → `TppQueryMaximumGroupCount +0x44/+0x49` | `NtQuerySystemInformationEx` (클래스 `0x6b`, 입력 4) | 2 | 1번째는 길이만 (`0xc0000004`), 2번째가 본 호출 | 탄다 |
| — | `TpSetDefaultPoolCpuSets` · `+0xba7` 블록 | 없음 | 0 | 매개변수 `+0x438`/`+0x43c` ≠ 0 일 때만 실행 | **안 탄다** `[관찰]` |
| 3 | `LdrpEnableUMGLTracingStateSync +0x7a/+0x7f` → `RtlpSubscribeWnfStateChangeNotificationInternal +0x189/+0x18e` → `RtlpInitializeWnf +0x54/+0x59` → `TpAllocTimer` → … → `TpAllocPoolInternal +0x240/+0x245` → `TppPoolUpdateNodeRelation +0x4b/+0x50` | `NtQuerySystemInformationEx` (`0x6b`, 입력 6) | 2 | 풀 만들 때 | 탄다 |
| 4 | 〃 `TpAllocPoolInternal +0x292/+0x297` | `NtCreateIoCompletion` | 1 | 〃 | 탄다 |
| 5 | 〃 `TpAllocPoolInternal +0x303/+0x308` | `NtCreateWorkerFactory` | 1 | 〃 | 탄다 |
| 6 | 〃 `TpAllocPoolInternal +0x38e/+0x393` → `TppInitializeTimerQueue +0x2c/+0x31`·`+0x44/+0x49` → `TppInitializeTimerSubQueue +0x4d/+0x52`·`+0x65/+0x6a`·`+0xa0/+0xa5` | `NtCreateTimer2` · `NtCreateWaitCompletionPacket` · `NtAssociateWaitCompletionPacket` | 각 2 (= 6) | 〃 (큐 둘) | 탄다 |
| 7 | 〃 `TppPoolpReferenceGlobalPool +0x140/+0x145`, `+0x181/+0x186` | `NtSetInformationWorkerFactory` (클래스 5 = `0x300`, 클래스 14 = `0x180`) | 2 | 새 전역 풀 | 탄다 |
| 8 | 〃 `TpAllocTimer +0x16d/+0x172` | `NtSetInformationWorkerFactory` (클래스 3, 값 1) | 1 | 풀의 첫 TP 개체 (옛 개수 0) | 탄다 |
| 9 | `RtlpInitializeWnf +0x98/+0x9d` → `RtlpWnfRegisterTpNotification +0x28/+0x2d` | `NtCreateEvent` | 1 | 항상 | 탄다 |
| 10 | 〃 `+0x47/+0x4c` → `TpAllocWait +0xba/+0xbf` | `NtCreateWaitCompletionPacket` | 1 | 항상 | 탄다 |
| 11 | 〃 `+0x77/+0x7c` | `NtSetWnfProcessNotificationEvent` | 1 | 항상 | 탄다 |
| 12 | 〃 `+0x92/+0x97` → `TpSetWaitEx +0x168/+0x16d` | `NtAssociateWaitCompletionPacket` | 1 | 항상 | 탄다 |
| 13 | `RtlpSubscribeWnf… +0xde/+0xe3` → `RtlpAddWnfUserSubToNameSub +0xd5/+0xda` | `NtSubscribeWnfStateChange` | 1 | 항상 | 탄다 |
| 14 | A: `+0xbee/+0xbf3` → `TraceLoggingRegisterEx… +0x55/+0x5a` → `EtwEventRegister +0x1e/+0x23` → `EtwNotificationRegister +0x15c/+0x161` → `EtwpRegisterProvider +0xaf/+0xb4` → `EtwpRegisterTpNotificationOnce +0x32/+0x37` | `NtCreateEvent` | 1 | **첫** `EtwpRegisterProvider` 만 | 탄다 |
| 15 | 〃 `+0x4d/+0x52` → `TpAllocWait +0xba/+0xbf` | `NtCreateWaitCompletionPacket` | 1 | 〃 | 탄다 |
| 16 | 〃 `+0x64/+0x69` → `TpSetWaitEx +0x168/+0x16d` | `NtAssociateWaitCompletionPacket` | 1 | 〃 | 탄다 |
| 17 | 〃 `+0x8c/+0x91` | `NtTraceControl` (코드 `0x1b`, 입력 4B) | 1 | 〃 | 탄다 |
| 18 | A·B·C·D 각 `EtwpRegisterProvider +0x14d/+0x152` | `NtTraceControl` (코드 `0xf`) | 4 | 매 등록. `0xc0000023` 이면 되풀이 | 탄다 |
| 19 | A 의 `TraceLoggingRegisterEx… +0x6e/+0x73` → `EtwEventSetInformation +0x2e` (꼬리 JMP) → `EtwpSetProviderTraits +0xdd/+0xe2` | `NtTraceControl` (코드 `0x1e`) | 1 | TraceLogging 등록만 | 탄다 |
| — | E: `RtlInitializeHeapLogging +0x3e/+0x43` | (탔다면 `0xf` + `0x1e` = 2) | 0 | `RtlpHpHeapFeatures & 8` | **안 탄다** `[관찰]` |

- 합: 1 + 2 + 18 (3 ~ 13) + 9 (14 ~ 19) = **30**.
- 시간 순서: `TpInitializePackage` 3 → 풀 만들기 (3 ~ 7) → 첫 개체 (8) → WNF 알림 (9 ~ 12) → WNF 구독 (13) → A 의 Once 넷 → A `0xf` → A `0x1e` → B `0xf` → C `0xf` → D `0xf`.
- 남는 핸들 (결과서): 이벤트 2 · 대기 패킷 4 · IO 완료 포트 1 · 일꾼 공장 1 · 타이머 2 = **10개**. run44 핸들 수 P1 4 → P1a 26 의 증가분 22 중 일부 `[추측]`.

### 2. 54 공사 중 셋 `+0xaad ~ +0xb05` `[정적]` — 합 `1 + 2k`

| # | 함수 / 사슬 | NtXxx | 횟수 | 조건 | test.exe |
|---|---|---|---|---|---|
| a1 | `+0xaad/+0xab2` (a) → 꼬리 `…IsEnabledFallback` → `wil_details_IsEnabledFallback +0x32/+0x37` → `…Reevaluate… +0x3c/+0x41` → `…GetCurrentFeatureEnabledState +0x40/+0x45` → `wil_RtlStagingConfig_QueryFeatureState +0x42/+0x47` → `RtlQueryFeatureConfiguration +0x6c/+0x71` → `RtlpFcReferenceFeatureConfigurationBuffers +0xcf/+0xd4` → `RtlpFcUpdateLocalConfiguration +0xad/+0xb2` | `NtQuerySystemInformationEx` (클래스 `0xd3`) | 1 | 버퍼 도장 (`RtlpFcProcessManager +0xe0` = 0) < KUSD `+0x720` (= 1 `[관찰]`) | 탄다 |
| a2 | 〃 `+0xdd/+0xe2` → `RtlpFcMapSingleBuffer +0x57/+0x5c` | `NtMapViewOfSection` | k (0 ~ 4) | 커널이 준 구역 핸들 ≠ 0 마다 | **동적** |
| a3 | 〃 `+0x137/+0x13c` | `NtClose` | k | a2 와 같은 개수 | **동적** |
| b | (b) `+0xac0/+0xac5` → … `ReportUsageToServiceDirect +0x9b/+0xa0` → `RtlNotifyFeatureUsage +0x46/+0x4b` → `RtlpFcSendFeatureUsageNotifications +0x8e/+0x93` → `RtlpFcNotifyFeatureUsageTarget +0x85/+0x8a`·`+0x107/+0x10c` | `NtQueryWnfStateData` · `NtUpdateWnfStateData` | 0 또는 각 1 이상 `[추측]` | 사용량 보고가 켜진 설정일 때만 | **동적** |
| b2 | (b) `+0xac9/+0xace` `LdrpInitializeSnapsBuffer +0x1b/+0x20` | 없음 (`RtlAllocateHeap`) | 0 | (b) 참 | 탄다 `[관찰]` |
| c | (c) `+0xb00/+0xb05` `RtlInitializeExceptionLog` | 없음 | 0 | `PEB.NtGlobalFlag & 0x800000` | **안 탄다** `[관찰]` |

### 3. 함수마다 "정적으로 닫힘 / 동적 필요" (결과서 한 줄 결론)

| 함수 | 닫힘 | 남은 것 |
|---|---|---|
| `TpInitializePackage` (`+0xb83`) | 정적 닫힘 (실패면 `+0xb8a JS → +0xa7b` 공통 끝, 반환값 = 그 오류) | 2번째 `0x6b` 가 또 모자라 되풀이하는지만 (D8) |
| `TpSetDefaultPoolCpuSets` · `+0xba7` 블록 | 정적 닫힘, test.exe 안 탐 `[관찰]` | — |
| `LdrpEnableUMGLTracingStateSync` (`+0xbe2`) | 줄기 정적 닫힘 (18번) | 일꾼 스레드가 바로 생기나 (D10), `TpAllocPoolInternal +0x443` 갈래 (아래 "결과서와 다르게") |
| `TraceLoggingRegisterEx…` (`+0xbee`) · `EtwEventRegister` ×2 (`+0xc06`·`+0xc23`) · `RtlInitializeHeapLogging` (`+0xc28`) | 인자·횟수 정적 닫힘 | 켜진 세션·`0xc0000023` 되풀이 (D2·D4), RegHandle 값 (D5) |
| (a) `Feature_…DisallowSharedLockImplicitUpgrade` | "켜짐" 고정 맞음 `[관찰]` | 구역 수 k (D7) |
| (b) `Feature_…LoaderSnapsBuffer` + `LdrpInitializeSnapsBuffer` | 참 `[관찰]` — 건너뛰면 실제와 다름 | 보고 갈래 (D13) |
| (c) `RtlInitializeExceptionLog` | 안 탐 `[관찰]` | — |

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 값 대조는 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_<K1 ~ P2>.txt`) 를 결과서가 다시 읽은 것. 날짜는 결과서 날짜.
- 03_sprint 스무째 바퀴. plan **Q118** — 사용자 결정 2026-11-03 (PE_emulator 요청 10, Q104 VM 단계 앞, 급함 보통 — PE_emulator 54 바퀴가 도는 동안 끝나면 55 바퀴가 씀). **짐 지도 바퀴** — `baggage.md` 에 새 쓴 자리 (아래 "짐 지도 반영").
- 정적: ntdll (`ntdll.pdb`) `C3093720177DA6851519DCFFE8DF53FE1` — flow.md 기준과 같다 (결과서, sast/56 과 같음).
- 버전 대조 (결과서, `_calls.tsv`): `LdrInitializeThunk +0x9/+0xe → LdrpInitialize`, `LdrpInitializeInternal +0x55/+0x5a → _LdrpInitialize`, `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess`, `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal`, `LdrpInitializeNode +0x166/+0x16b → LdrpCallTlsInitializers` — 모두 일치. syscall 몸통 번호 (`NtClose` 0xf, `NtMapViewOfSection` 0x28, `NtCreateEvent` 0x48) 도 같다.

## 목적

- PE_emulator 54 바퀴는 `LdrpInitializeProcess +0xaa9 ~ +0xb7d` 를, 55 바퀴는 `+0xb83 ~ +0xc2d` 를 C++ 로 짓는다. 이 구간 함수 대부분이 `.asm` 본문이 없어 "공사 중" 빈 함수로 남을 처지였다.
- PE_emulator 의 "호출 약속" 덫 (syscall 이름·횟수를 미리 약속하고 실행에서 맞춤) 에 쓸 **syscall 이름·횟수·조건**, 그리고 함수마다 쓰는 칸 (요청서 1 ~ 9).
- 짐 지도: baggage 3.8 ntdll `.data` 전역 (`Tpp*`·`Etwp*`·`g_h*`·기능 상태 칸 등 `≤P1a`·`끝까지 0` 목록), 3.1 PEB `TppWorkerpList*`, 3.11 `EtwDataSource`·`WnfContext` 사슬.
- 범위 밖 (요청서): `+0xc2d RtlpInitEnvironmentBlock` 부터 뒤, 커널 쪽 `NtTraceControl`·`NtSetWnfProcessNotificationEvent` 본문 (run51 몫), 공용 부품 본문, ETW 이벤트 쓰기.

## 방법

- 정적: `_workspace/sast/57_tp-etw-init.md` — ntdll 대상 13개 depth 0 (`TpInitializePackage`·`TppQueryMaximumGroupCount`·`TpSetDefaultPoolCpuSets`·`TraceLoggingRegisterEx…`·`EtwEventSetInformation`·`EtwEventRegister`·`EtwpRegisterProvider`·`RtlInitializeHeapLogging`·`RtlInitializeExceptionLog`·기능 상태 함수 넷) + 줄기를 끝까지 가려고 보탠 약 50개 (`TpAllocWait`·`TpSetWaitEx`·`TppPoolpReferenceGlobalPool`·`TpAllocPoolInternal`·`TppInitializeTimer*`·`EtwpSetProviderTraits`·`EtwpInsertRegistration`·`ProviderHandleInsert`·`wil_details_*` 9개·`RtlpFc*` 등) + depth 1 넷 (`RtlQueryFeatureConfiguration` 등).
  - 이미 analyze 된 프로젝트 — **다시 analyze 안 함**, export 만 (로그 `ms/ghidra/ntdll.export.sast57a ~ m.stdout`).
  - `_refs` 새로 약 40개 (`RtlpSrwLockAllowImplicitUpgrade`·`TppPoolpGlobalPoolMaxThreadsOverride`·`g_hPrivLoggerNotificationProvider`·`g_hUserDiagnosticProvider`·`0x1cc6f0`·`0x1cc670`·`EtwpRegisterTpInitOnce`·`0x1d23e0`·`RtlpFc*` 등).
  - 넓힌 이유 (결과서): 줄기 끝 (`EtwpRegisterTpNotificationOnce` 의 `TpAllocWait`, `RtlpInitializeWnf → TpAllocTimer`) 에서 **기본 스레드 풀이 처음 만들어져** syscall 이 늘고, 8(a)(b) 는 첫 검사가 설정 구역을 받아 오는 길이 있어서.
- 결과서: `_workspace/sast/57_tp-etw-init_result.md` (**분석성공**, PDB 일치, 1 ~ 9 모두 답).
- 이 문서를 쓰며 다시 맞춘 것 (flow-writer, `.asm`): `LdrpInitializeProcess +0xaed ~ +0xc41` 전부, CALL 줄 — `TpInitializePackage`·`TppQueryMaximumGroupCount`·`TpSetDefaultPoolCpuSets`·`LdrpEnableUMGLTracingStateSync`·`RtlpSubscribeWnfStateChangeNotificationInternal`·`RtlpInitializeWnf`·`TpAllocTimer`·`RtlpWnfRegisterTpNotification`·`RtlpAddWnfUserSubToNameSub`·`TraceLoggingRegisterEx…`·`EtwEventRegister`·`EtwNotificationRegister`·`EtwpRegisterProvider`·`EtwpRegisterTpNotificationOnce`·`EtwpSetProviderTraits`·`EtwEventSetInformation`·`RtlInitializeHeapLogging`·`LdrpInitializeSnapsBuffer`·`RtlInitializeExceptionLog`·`TppPoolpReferenceGlobalPool`·`TppCleanupGroupMemberInitialize`·`TppWorkInitialize`·`TpAllocWait`·`TpSetWaitEx`·`TpAllocPoolInternal`·`TppPoolUpdateNodeRelation`·`TppInitializeTimerQueue`·`TppInitializeTimerSubQueue`·`TpSetPoolMaxThreads`·`TpSetPoolMaxThreadsSoftLimit`·`RtlpFcUpdateLocalConfiguration`·`RtlpFcReferenceFeatureConfigurationBuffers`·`RtlpFcMapSingleBuffer`·`RtlQueryFeatureConfiguration`, 전역 쓰는 줄 — `EtwNotificationRegister +0x7a`·`+0x10f`·`+0x1be`, `EtwpInsertRegistration +0x1b ~ +0x22`·`+0xb3`, `EtwpRegisterProvider +0x95`·`+0x19f`, `UserDiagnosticProviderCallback +0x5`·`+0x11`, `ProviderHandleInsert +0x6e`·`+0x228`·`+0x230`, `RtlpInitializeWnf +0xa1`, `LdrpEnableUMGLTracingStateSync +0x35`, `…Reevaluate… +0xa0`·`…TryEnableDeviceUsageFastPath +0x37` (`CMPXCHG.LOCK`). 결과서와 다른 곳은 아래 "결과서와 다르게 적은 곳".
- 문서: 이 run 문서, `run51-eprocess-later.md` · `run59-init-leftovers.md` (맨 위 정정 한 줄), `flow.md` (기준, 그림 2 `j1` 상자 글, 2.4 새 절·run59 묶음 2 줄·병렬 로딩 줄·run51 절, 3장 일꾼, 4장, 5장), `baggage.md` (아래).

## 결과

비유 (가게 문 열기):

- 가게 (프로세스) 가 문을 열기 전에 **일꾼 사무실 (기본 스레드 풀)** 을 하나 차린다. 사무실을 차리는 건 처음 "알림 받기 (WNF)" 를 신청할 때다 — 알림이 오면 처리할 일꾼이 필요하니까.
- 그다음 **방송국 (ETW) 에 채널 넷을 등록**한다. 방송국은 "등록 창구" 를 처음 쓸 때만 **알림 벨 (이벤트) + 벨 지키는 사람 (대기 개체)** 을 한 번 놓아 준다. 그게 "Once" 다. 벨 지키는 사람은 이미 차린 사무실 일꾼을 빌려 쓰니 사무실을 또 차리지 않는다.
- run51 은 "벨을 놓는 건 둘째·셋째 등록 때" 라고 적었는데, 실제로는 **첫 등록 (TraceLogging 채널)** 때다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/*.asm` `CALL … ret=`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0xaad` | `+0xab2` | `Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_IsEnabledNoReportingNoInline` (→ `+0xaba` `RtlpSrwLockAllowImplicitUpgrade` = `EAX == 0`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xac0` | `+0xac5` | `Feature_Servicing_LoaderSnapsBuffer__private_IsEnabledDeviceUsageNoInline` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xac9` | `+0xace` | `ntdll!LdrpInitializeSnapsBuffer` (위가 참일 때, 반환 검사 안 함) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xb00` | `+0xb05` | `ntdll!RtlInitializeExceptionLog` (`+0xaf4 TEST [RDI+0xbc],0x800000` → `+0xafe JZ → +0xb05`) — 안 탐 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xb83` | `+0xb88` | `ntdll!TpInitializePackage` (`+0xb8a JS → +0xa7b`) | [정적] |
| `ntdll!TpInitializePackage` | `+0x61` · `+0x80` | `+0x66` · `+0x85` | `NtQuerySystemInformation` (같은 주소 이름 2개: `NtQuerySystemInformation`·`RtlGetNativeSystemInformation` — PDB 별칭표는 `Zw/Nt` 한 쌍뿐) · `TppQueryMaximumGroupCount` | [정적] |
| `ntdll!TppQueryMaximumGroupCount` | `+0x44` | `+0x49` | `NtQuerySystemInformationEx` (2번 — `+0x8b JMP → +0x1c` 로 되돌아) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xba2` | `+0xba7` | `ntdll!TpSetDefaultPoolCpuSets` (`+0xb99 JZ → +0xba7`, `+0x438` = 0) — 안 탐 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xbb9` · `+0xbcb` | `+0xbbe` · `+0xbd0` | `RtlAcquireSRWLockExclusive` · `RtlReleaseSRWLockExclusive` (`TppPoolpGlobalPoolLock`, `+0xbb0 JZ → +0xbd0` — `+0x43c` = 0) — 안 탐 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xbe2` | `+0xbe7` | `ntdll!LdrpEnableUMGLTracingStateSync` (`UseWOW64 == 0` 이고 `LdrpIsSecureProcess == 0`) | [정적] |
| `ntdll!LdrpEnableUMGLTracingStateSync` | `+0x7a` | `+0x7f` | `ntdll!RtlpSubscribeWnfStateChangeNotificationInternal` (`R9` 콜백 = `LdrpUMGLTracingStateChangeNotification`) | [정적] |
| `ntdll!RtlpSubscribeWnfStateChangeNotificationInternal` | `+0x57` · `+0x189` · `+0x19e` | `+0x5c` · `+0x18e` · `+0x1a3` | `RtlRunOnceBeginInitialize(0x1ce1f8)` · **`RtlpInitializeWnf`** · `RtlRunOnceComplete` | [정적] |
| `ntdll!RtlpInitializeWnf` | `+0x54` · `+0x98` | `+0x59` · `+0x9d` | **`TpAllocTimer`** · `RtlpWnfRegisterTpNotification` | [정적] |
| `ntdll!TpAllocTimer` | `+0xbd` · `+0x16d` | `+0xc2` · `+0x172` | `TppWorkInitialize` · `NtSetInformationWorkerFactory` | [정적] |
| `ntdll!TppWorkInitialize` → `TppCleanupGroupMemberInitialize` | `+0x2c` → `+0x15a` | `+0x31` → `+0x15f` | `TppPoolpReferenceGlobalPool` | [정적] |
| `ntdll!TppPoolpReferenceGlobalPool` | `+0xc2` · `+0x140` · `+0x181` | `+0xc7` · `+0x145` · `+0x186` | **`TpAllocPoolInternal`** (풀이 없을 때만) · `TpSetPoolMaxThreads` · `TpSetPoolMaxThreadsSoftLimit` | [정적] |
| `ntdll!TpAllocPoolInternal` | `+0x240` · `+0x292` · `+0x303` · `+0x38e` | `+0x245` · `+0x297` · `+0x308` · `+0x393` | `TppPoolUpdateNodeRelation` · `NtCreateIoCompletion` · `NtCreateWorkerFactory` · `TppInitializeTimerQueue` | [정적] |
| `ntdll!RtlpWnfRegisterTpNotification` | `+0x28` · `+0x47` · `+0x77` · `+0x92` | `+0x2d` · `+0x4c` · `+0x7c` · `+0x97` | `NtCreateEvent` · `TpAllocWait` · `NtSetWnfProcessNotificationEvent` · `TpSetWaitEx` | [정적] |
| `ntdll!RtlpSubscribeWnfStateChangeNotificationInternal` → `RtlpAddWnfUserSubToNameSub` | `+0xde` → `+0xd5` | `+0xe3` → `+0xda` | `NtSubscribeWnfStateChange` | [정적] |
| `ntdll!LdrpEnableUMGLTracingStateSync` | `+0x93` · `+0xa5` | `+0x98` · `+0xaa` | `RtlRunOnceComplete` (칸 `0x1d23e0`) · `RtlpEnumProcessHeaps` (syscall 없음) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xbee` | `+0xbf3` | `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` (`RCX` = `0x1801cc6f0`, Ghidra 주석 `NormalizationListHead+0x48` 은 틀림) | [정적] |
| `ntdll!TraceLoggingRegisterEx…` | `+0x55` · `+0x6e` | `+0x5a` · `+0x73` | `EtwEventRegister` · `EtwEventSetInformation` (→ `+0x2e` 꼬리 JMP `EtwpSetProviderTraits`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xc06` · `+0xc23` | `+0xc0b` · `+0xc28` | `EtwEventRegister` (B·C, 반환 검사 안 함) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xc28` | `+0xc2d` | `ntdll!RtlInitializeHeapLogging` → `+0x1e/+0x23` `EtwEventRegister` (D) · `+0x3e/+0x43` `TraceLoggingRegisterEx…` (E, 안 탐) | [정적] |
| `ntdll!EtwEventRegister` | `+0x1e` | `+0x23` | `EtwNotificationRegister` | [정적] |
| `ntdll!EtwNotificationRegister` | `+0xb4` · `+0x13f` · `+0x15c` · `+0x16a` · `+0x172` | `+0xb9` · `+0x144` · `+0x161` · `+0x16f` · `+0x177` | `RtlAllocateHeap(0x100)` · `ProviderHandleInsert` · **`EtwpRegisterProvider`** · `EtwpInsertRegistration` · `EtwpCheckForPrivatePreEnable` | [정적] |
| `ntdll!EtwpRegisterProvider` | `+0xaf` · `+0xc8` · `+0x14d` · `+0x17d` | `+0xb4` · `+0xcd` · `+0x152` · `+0x182` | `EtwpRegisterTpNotificationOnce` (첫 호출만) · `RtlRunOnceComplete` · `NtTraceControl(0xf)` (매번) · `EtwpUpdateEnableInfoAndCallback` | [정적] |
| `ntdll!EtwpRegisterTpNotificationOnce` | `+0x32` · `+0x4d` · `+0x64` · `+0x8c` | `+0x37` · `+0x52` · `+0x69` · `+0x91` | `NtCreateEvent` · `TpAllocWait` (콜백 `EtwpNotificationThread`) · `TpSetWaitEx` · `NtTraceControl(0x1b)` | [정적] |
| `ntdll!EtwpSetProviderTraits` | `+0xdd` | `+0xe2` | `NtTraceControl(0x1e)` | [정적] |
| `ntdll!LdrpInitializeSnapsBuffer` | `+0x1b` | `+0x20` | `RtlAllocateHeap(프로세스 힙, 0, 0x1000)` | [정적] |
| `ntdll!RtlpFcReferenceFeatureConfigurationBuffers` | `+0xcf` | `+0xd4` | `RtlpFcUpdateLocalConfiguration` → `+0xad/+0xb2` `NtQuerySystemInformationEx(0xd3)` · `+0xdd/+0xe2` `RtlpFcMapSingleBuffer` · `+0xff/+0x104` `RtlpFcBufferManagerUpdateBuffers` · `+0x137/+0x13c` `NtClose` | [정적] |

### 질문별 답 (결과서 요약)

1. **`TpInitializePackage`** — 쓰는 것: `TppHeapTag` (`0x1d22bc`, `+0x51` ← `RtlCreateTagHeap` 결과) · `TppNumberNodes` (`0x1d2274`, `+0x74 ~ +0x7a` ← 버퍼 첫 dword + 1) · `TppMaximumGroups` (`0x1d2270`, `+0x85 ~ +0x88` ← `AX`) · **PEB `+0x388` `TppWorkerpListLock` = 0 (`+0x9b`) · `+0x390` `TppWorkerpList` 빈 목록 (`+0xa3`·`+0xa9`·`+0xad`)**. syscall 3 (위 1절). 실패는 `+0xb8a JS → +0xa7b` 공통 끝 — `LdrpInitializeProcess` 반환값 = 그 오류. test.exe 성공 갈래 `[관찰]` (PEB `+0x390` P1 `[0 - 0]` → P1a 자기 주소 둘, `04_baggage-read_P1.txt:1664`·`_P1a.txt:1831`). 이 VM `ActiveProcessorCount` 2 · `ActiveGroupCount` 1 `[관찰]`. `[정적]`
2. **`TpSetDefaultPoolCpuSets`** — `TppPoolpDefaultPoolCpuSetLock` 안에서 `TppPoolpDefaultPoolCpuSetCount` ← `EDX` (`+0x1e`), `TppPoolpDefaultPoolCpuSets` ← `EDX*8` 바이트 복사 (`+0x32`, **길이 검사 없음**). syscall 없음. R13 = 매개변수 블록 맞음 (`+0x6c5 MOV R13,RAX` ← `RtlNormalizeProcessParams`). test.exe `+0x438` = 0 → **안 탐** `[관찰]` (사진 8곳). `[정적]`
3. **`+0xba7 ~ +0xbd0` 블록** — `+0x43c` ≠ 0 이면 `TppPoolpGlobalPoolLock` 안에서 **`+0xbbe` `TppPoolpGlobalPoolMaxThreadsOverride`** ← `EBX` (쓰는 곳 이 하나). test.exe **안 탐** `[관찰]`. 0 이면 기본 풀 최대 = `ActiveProcessorCount*8` (최소 `0x300`) → 이 VM **`0x300`**, 소프트 한도 `*4` (최소 `0x180`) → **`0x180`** (`TppPoolpReferenceGlobalPool +0x111 ~ +0x13a`, `+0x166 ~ +0x17b`). `[정적]`
4. **`LdrpEnableUMGLTracingStateSync`** — 조건 `UseWOW64 == 0` · `LdrpIsSecureProcess == 0` (R15 = 0, `+0xa50 XOR R15D,R15D`) → test.exe **탄다**. 이름 없는 run-once 칸 `0x1d23e0` (아래 정정) 을 `+0x35 CMPXCHG` 로 잡고, WNF 이름 `WNF_ETW_UMGL_TRACING_CHANGE` (`0x41911a28a3bc10b5`) 구독. 그 안 **`RtlpInitializeWnf` (WNF 첫 한 번, 칸 `0x1ce1f8`) → `TpAllocTimer` → 기본 스레드 풀 처음 만들기** (IO 완료 포트 · 일꾼 공장 (시작 루틴 `0x18001a2e0` = `TppWorkerThread` — flow-writer `_index.tsv` 확인, 최대 `0x200`, 스택 = exe 헤더 값) · 타이머 큐 둘) → WNF 알림 이벤트 (`NtSetWnfProcessNotificationEvent` — 커널 `EPROCESS.WnfContext`, run51) → `0x1ce200` ← WNF 문맥 (`+0xa1`) → `NtSubscribeWnfStateChange`. KUSD `UserModeGlobalLogger` (`0x7ffe038e`) = 0 `[관찰]` 이라 `RtlpWnfETWEventSubscribe` 안 탐. `RtlpEnumProcessHeaps` 는 syscall 없음. 18번. `[정적]`
5. **`TraceLoggingRegisterEx…`** — **`0x1801cc6f0` = TraceLogging 공급자 핸들 구조체 `0x38`B (PDB 이름 없음)** — 메타데이터 `0x1801a3190` (이름 `EtwUserModeDiagnosticProvider`, GUID `{daf2c08d-5ac9-5066-ff0b-2d3d95275779}`), `+0x20` RegHandle, `+0x28`·`+0x30` = 0 (`+0x39`·`+0x45`). 쌍둥이 `0x1cc670` = `Microsoft.Windows.Heap` (E, 안 탐). 반환 = `EtwEventRegister` 상태, `EtwEventSetInformation` 오류는 버림. RegHandle 이 이미 있으면 `+0x37 JNZ → +0x93 INT 0x29`. `[정적]`
6. **`EtwEventRegister`** — 직접 두 호출 인자 (결과서 표): B `+0xc06` = `PrivateLoggerNotificationGuid` `{3595ab5c-042a-4c8e-b942-2d059bfeb1b1}`, 콜백 0, 핸들 `g_hPrivLoggerNotificationProvider` · C `+0xc23` = `UserDiagnosticGuid` `{305fc87b-002a-5e26-d297-60223012ca9c}`, 콜백 **`UserDiagnosticProviderCallback`** (`0x180147e40`), 핸들 `g_hUserDiagnosticProvider`. 둘 다 **반환 검사 안 함**. RegHandle = `(순번 << 32) | 칸 번호` (`EtwNotificationRegister +0x184 ~ +0x193`), 계산값 A `0x1_00000000` · B `0x2_00000011` · C `0x3_00000021` · D `0x4_00000031` (확인은 D5). 등록 개체 `0x100`B 4개 (`EtwNotificationRegister +0xb4`), 나무 `EtwpRegistrationTable` 에 GUID 순 (`EtwpInsertRegistration +0xae RtlRbInsertNodeEx`). `[정적]`
   - **"Once" 의 뜻**: `EtwpRegisterProvider +0x4f` `PebLdr.ShutdownInProgress` (`0x1d28c8`) = 0 이면 `EtwpRegisterTpInitOnce` (`0x1d2268`) 를 본다 → 처음 (0) 이면 `+0x95 CMPXCHG` 0 → 1 이긴 쪽만 `+0xaf EtwpRegisterTpNotificationOnce` → `+0xc8 RtlRunOnceComplete` 2. 그 안 TP 개체 둘은 **풀이 이미 있어서** 풀을 새로 안 만들고 `NtSetInformationWorkerFactory` 도 안 부른다 (`TpAllocWait +0x115 XADD` 옛 값 ≥ 1). 이 구간보다 앞에서 test.exe 가 `EtwNotificationRegister` 에 닿는 길은 없다 (`SbSelectProcedure` 는 `RtlpHpOptIntoSegmentHeap` 0 이라 안 탐, `SbObtainTraceHandle` 은 뒤 `+0x19c8`).
7. **`RtlInitializeHeapLogging`** — D `+0x1e` (`WindowsHeapSnapshotProvider` `{901d2afa-4ff6-46d7-8d0e-53645e1a47f5}`, 콜백 `RtlpHpStackTraceEtwCallback`, 핸들 `RtlpHpStackTraceProviderHandle`) 1번. `+0x23 TEST [RtlpHpHeapFeatures],8` → E 안 탐 `[관찰]` (0). 반환은 안 봄. `[정적]`
8. **54 공사 중 셋** — 위 2절. (a) 상태 칸 `…__private_featureState` (`0x1d4f20`) 비트 1 이 켜졌으면 비트 0 이 결과, 처음엔 꼬리 `Fallback` → … → `RtlQueryFeatureConfiguration(0x02efc829)` → `CMPXCHG` 캐시 (`Reevaluate +0xa0`). 기술자 기본값 켜짐 (`+0x1f` = 1). 이 프로세스 **첫 기능 상태 검사** 라 `RtlpFcProcessManager` (버퍼 두 벌 + 도장) 를 받아 온다. (b) 상태 칸 `0x1d4f40` 비트 4 빠른 길, 처음엔 보고 장부 `…LoaderSnapsBuffer__private_reporting` (`0x1d23e8`) 에 기록 → `TryEnableDeviceUsageFastPath +0x37 CMPXCHG` 비트 4. 버퍼는 이미 받아 둬서 추가 syscall 없음이 정상. (c) `RtlpExceptionLog2` ← `+0x1e`, `RtlpExceptionLogSize` ← `0x32` (`+0x3a`) — 안 탐. `[정적]` + 갈래 `[관찰]`
9. **매개변수 `+0x430 ~ +0x43c`** — PDB 와 asm 이 그대로 맞음. `+0x440 HeapMemoryTypeMask` 는 **4B** (요청서 8B 아님). 사진 8곳 모두 0. `[정적]` + `[관찰]`

### 호출 관계

```
LdrpInitializeProcess
 │  … (+0xa50 프로세스 힙, run46)
 ├─ +0xab2  Feature_…DisallowSharedLockImplicitUpgrade…NoInline   ⇒ …Fallback ⇒ wil_details_* → RtlQueryFeatureConfiguration
 │          │                                                        → RtlpFcReferenceFeatureConfigurationBuffers +0xd4
 │          │                                                        → RtlpFcUpdateLocalConfiguration: +0xb2 NtQuerySystemInformationEx(0xd3)
 │          │                                                          · +0xe2 RtlpFcMapSingleBuffer → +0x5c NtMapViewOfSection ×k
 │          │                                                          · +0x104 RtlpFcBufferManagerUpdateBuffers (RtlpFcProcessManager)
 │          │                                                          · +0x13c NtClose ×k
 │  +0xaba  RtlpSrwLockAllowImplicitUpgrade = (EAX == 0)  → 0
 ├─ +0xac5  Feature_…LoaderSnapsBuffer…DeviceUsageNoInline       (참)
 ├─ +0xace  LdrpInitializeSnapsBuffer                          RtlAllocateHeap 0x1000 → LdrpSnapsUnicodeString{,2}
 │  (+0xb05 RtlInitializeExceptionLog — 안 탐)
 │  +0xb2b EtwpFreeRegistrationList = 0 · +0xb32 PrivateLoggerNotificationEntry = 0 · +0xb7d NtdllBaseTag
 ├─ +0xb88  TpInitializePackage                                syscall 3 · PEB TppWorkerpList
 │  (+0xba7 TpSetDefaultPoolCpuSets · +0xbbe MaxThreadsOverride — 안 탐)
 ├─ +0xbe7  LdrpEnableUMGLTracingStateSync                     run-once 0x1d23e0
 │        └─ +0x7f  RtlpSubscribeWnfStateChangeNotificationInternal
 │                 ├─ +0x18e RtlpInitializeWnf                 (WNF 첫 한 번 0x1ce1f8)
 │                 │        ├─ +0x59 TpAllocTimer
 │                 │        │        └─ … TppPoolpReferenceGlobalPool → +0xc7 TpAllocPoolInternal   ← 기본 풀 처음 (12)
 │                 │        │           +0x172 NtSetInformationWorkerFactory(3, 1)                    ← 첫 개체 (1)
 │                 │        └─ +0x9d RtlpWnfRegisterTpNotification   NtCreateEvent · TpAllocWait · NtSetWnfProcessNotificationEvent · TpSetWaitEx (4)
 │                 └─ +0xe3 RtlpAddWnfUserSubToNameSub → +0xda NtSubscribeWnfStateChange (1)
 ├─ +0xbf3  TraceLoggingRegisterEx… (0x1cc6f0, A)
 │        ├─ +0x5a EtwEventRegister → +0x23 EtwNotificationRegister → +0x161 EtwpRegisterProvider
 │        │                                                    ├─ +0xb4  EtwpRegisterTpNotificationOnce   ← Once: NtCreateEvent · TpAllocWait · TpSetWaitEx · +0x91 NtTraceControl(0x1b) (4)
 │        │                                                    └─ +0x152 NtTraceControl(0xf)
 │        └─ +0x73 EtwEventSetInformation ⇒ EtwpSetProviderTraits → +0xe2 NtTraceControl(0x1e)
 ├─ +0xc0b  EtwEventRegister (B)  → … → EtwpRegisterProvider +0x152 NtTraceControl(0xf)
 ├─ +0xc28  EtwEventRegister (C)  → … → EtwpRegisterProvider +0x152 NtTraceControl(0xf)
 ├─ +0xc2d  RtlInitializeHeapLogging → +0x23 EtwEventRegister (D) → … → NtTraceControl(0xf)
 └─ +0xc32  RtlpInitEnvironmentBlock (run45, 이번 범위 밖)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). `⇒` 는 꼬리 `JMP` (콜스택에 안 보임). 호출 기호 없는 `+0x..` 줄은 그 함수 안 줄.
- 콜스택 기대 (Once 의 `NtTraceControl(0x1b)` 에서 `k`): `NtTraceControl` ← `EtwpRegisterTpNotificationOnce+0x91` ← `EtwpRegisterProvider+0xb4` ← `EtwNotificationRegister+0x161` ← `EtwEventRegister+0x23` ← `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation+0x5a` ← `LdrpInitializeProcess+0xbf3`. `[정적]`

### 사진 대조 (결과서가 run44 덤프 읽기 로그로 뒷받침한 것)

| 칸 | 사진 | 뜻 |
|---|---|---|
| PEB `TppWorkerpList` (`+0x390`) | P1 `[0 - 0]` → P1a 자기 주소 둘 (`04_baggage-read_P1.txt:1664`, `_P1a.txt:1831`) | `TpInitializePackage` 성공 갈래 |
| 매개변수 `+0x430`·`+0x438`·`+0x43c`·`+0x440` | 8곳 모두 0 / null (`P1.txt:2377 ~ 2379` 등) | 2·3 안 탐 |
| `TppNumberNodes`·`TppMaximumGroups`·`TppPoolpGlobalPool` | `≤P1a` 값 있음 | 풀이 P1a 전에 생김 |
| `TppHeapTag`·`TppPoolpDefaultPoolCpuSetCount`·`TppPoolpGlobalPoolMaxThreadsOverride`·`TppPoolpGlobalPoolMaxThreads` | 끝까지 0 | `RtlCreateTagHeap` 0 반환 · 집합·덮어쓰기 갈래 안 탐 |
| `EtwpRegisterTpInitOnce`·`PrivateLoggerNotificationEntry`·`EtwpRegistrationCount`·`EtwpRegistrationSequence`·`EtwpRegistrationTable`·`g_hPrivLoggerNotificationProvider`·`g_hUserDiagnosticProvider`·`RtlpHpStackTraceProviderHandle` | `≤P1a` 값 있음 | 네 등록이 P1a 전에 성공 |
| `EtwpGuidEntryTable`·`EtwpFreeRegistrationList`·`g_isErrorOriginProviderEnabled` | 끝까지 0 | 켜짐 정보로 콜백이 불린 흔적 없음, 등록 해제 없음 |
| `RtlpSrwLockAllowImplicitUpgrade` | P1a (`+0x199d`, 쓰는 줄 `+0xaba` 뒤) ~ P2 0 | (a) 켜짐 |
| `…DisallowSharedLockImplicitUpgrade__private_featureState`·`…LoaderSnapsBuffer__private_featureState`·`…LoaderSnapsBuffer__private_reporting`·`LdrpSnapsUnicodeString{,2}` | `≤P1a` 값 있음 | (a)(b) 첫 검사 · 스냅 버퍼 만들어짐 |
| `RtlpExceptionLog2`·`RtlpExceptionLogSize`, PEB `NtGlobalFlag` | 끝까지 0 / 0 | (c) 안 탐 |
| `RtlpHpHeapFeatures` | 끝까지 0 | E 안 탐 |
| KUSD `+0x720` `FeatureConfigurationChangeStamp` · `+0x386` · `UserModeGlobalLogger` (`0x7ffe0380` 32B) | 1 · 0 · 전부 0 (P1a·P2) | 설정 버퍼를 받아 옴 · `TppETWPoolCreate`·`RtlpWnfETWEventSubscribe` 안 탐 |

## 확인된 것

- **`+0xb83 ~ +0xc2d` 의 test.exe syscall = 30번** (`TpInitializePackage` 3 · `LdrpEnableUMGLTracingStateSync` 18 · ETW 9). `[정적]` (결과서 syscall 표) + 갈래 `[관찰]` run44 사진.
- **기본 스레드 풀은 `+0xbe2 LdrpEnableUMGLTracingStateSync → RtlpSubscribeWnf… +0x189 → RtlpInitializeWnf +0x54 → TpAllocTimer` 안에서 처음 만들어진다** (`TppPoolpReferenceGlobalPool +0xc2 TpAllocPoolInternal`, 풀이 없을 때만). `[정적]`. 사진 `TppPoolpGlobalPool` `≤P1a` 와 맞음 `[관찰]` run44.
  - run44 사진의 `ActiveThreads` 는 P1a 에서 아직 **1** 이다 (flow.md 3장, K3 ~ P1a 1 → P1b 3) `[관찰]`. 그러니 일꾼 공장을 만든 `+0xbe2` 뒤에도 P1a (`+0x199d`) 까지는 살아 있는 일꾼 스레드가 없다 — 만들어졌다 끝났을 수도 있지만 그 흔적은 사진에 없다 `[추측]` (D10).
- **ETW Once (`NtCreateEvent`·`NtCreateWaitCompletionPacket`·`NtAssociateWaitCompletionPacket`·`NtTraceControl(0x1b)`) 는 첫 `EtwpRegisterProvider` = `+0xbee → TraceLoggingRegisterEx… +0x55` 한 번뿐**이다. `+0xc06`·`+0xc23`·`RtlInitializeHeapLogging` 은 `NtTraceControl(0xf)` 1번씩. `[정적]`
- **`EtwEventRegister` 는 이 구간에서 4번, 모두 syscall 까지 간다.** `[정적]` + 네 핸들 `≤P1a` `[관찰]`.
- **`+0xaad` 가 이 프로세스의 첫 기능 상태 검사이고, 여기서 `RtlpFcProcessManager` 가 채워진다** (`RtlpFcUpdateLocalConfiguration +0xff RtlpFcBufferManagerUpdateBuffers`). 설정 구역 syscall `NtQuerySystemInformationEx(0xd3)` 1 + `NtMapViewOfSection`·`NtClose` 각 k. `[정적]` (k 는 동적)
- **(a) "켜짐" 고정이 맞고, (b) 스냅 버퍼는 실제로 만들어지며, (c) 예외 기록은 안 탄다.** `[정적]` + `[관찰]` run44.
- **`0x1d23e0` 은 이름 없는 RTL_RUN_ONCE 칸** (쓰는 곳 `LdrpEnableUMGLTracingStateSync` 뿐 — `+0x6` 읽기 · `+0x35 CMPXCHG.LOCK` · `+0x7f`·`+0xd7` 주소 넘김), **`0x1801cc6f0` 은 TraceLogging 공급자 구조체** (`0x38`B, `TppPoolpList` `0x1cc728` 바로 앞까지). Ghidra 주석 (`g_pShimmedModuleList+0x8`, `NormalizationListHead+0x48`) 은 "가장 가까운 앞 이름 + 거리" 일 뿐이다. `[정적]`

## 결과서와 다르게 적은 곳

- **`TpAllocWait` 의 `NtSetInformationWorkerFactory`** — 결과서는 "`+0x300` 의 `NtSetInformationWorkerFactory`" 라 적었다. `+0x300` 은 그 갈래의 첫 줄 (`MOV RCX,[RCX+0x38]`) 이고 CALL 은 **`+0x31b` / ret `+0x320`** 이다 (`.asm`). 뜻은 같다 (옛 개수 ≥ 1 이면 안 탐).
- **`TpAllocPoolInternal +0x443/+0x448 NtSetInformationWorkerFactory` (클래스 `0xd`)** — 결과서의 "풀 만들기 12번" 에 이 줄이 없다. flow-writer 가 `.asm` 을 보니 `NtCreateWorkerFactory` (`+0x303`) 뒤 `+0x316 CMP [RSP+0xb8],R15D` → `+0x31e JNZ → +0x42e` 일 때만 타는 갈래다 (돌아와 `+0x460 JMP → +0x324`). `[RSP+0xb8]` 에 무엇이 들어가는지는 안 봤다 → test.exe 가 타는지 `[공백]`. 타면 구간 합계가 31 이 된다. 결과서 숫자 (30) 는 고치지 않았다 — D9 멈춤점에 `ntdll+0xe5e43` 을 같이 걸면 닫힌다.
- 결과서 4번 "`+0x4b ~ +0x5c` `RtlRunOnceBeginInitialize(0x1ce1f8)`" 의 CALL 은 `+0x57` / ret `+0x5c` 다 (`+0x4e` 가 칸 주소 `LEA`). 이 문서 표는 call/ret 로 적었다.

## 정정

- **run51 — ETW "Once" 가 걸리는 자리.** run51 표 (`LdrpInitializeProcess +0xc06` / `+0xc23` → `EtwEventRegister`) 와 사슬 (`EtwEventRegister +0x1e → EtwNotificationRegister +0x15c → EtwpRegisterProvider +0xaf → EtwpRegisterTpNotificationOnce`) 은 **Once 를 타는 쪽이 틀렸다**. Once (`NtTraceControl(0x1b)` → 커널 `EPROCESS.EtwDataSource`) 는 **첫 `EtwpRegisterProvider` 한 번뿐**이고, 그 첫 번째는 `+0xbee` (ret `+0xbf3`) `TraceLoggingRegisterEx… +0x55` (ret `+0x5a`) → `EtwEventRegister` 다. `+0xc06`·`+0xc23` 은 `NtTraceControl(0xf)` 만 부른다. 사슬의 아래쪽 (`EtwEventRegister +0x1e → … → +0xaf`) 은 그대로 맞다.
  - run51 문서 맨 위에 한 줄. flow.md 2.4 run51 절 트리 줄, 짐 지도 3.11 `EtwDataSource` 줄·5장 ② 줄을 고침.
- **run59 — `RtlpFcProcessManager` 는 "전부 0" 으로 끝나지 않는다.** run59 의 "전부 0, `+0x108` 목록 머리 자기 주소" 는 `+0x21c ~ +0x286` 직후 값으로는 맞다. 그런데 같은 함수 뒤 **`+0xaad` 의 첫 기능 상태 검사가 설정 버퍼 두 벌과 도장 (`+0xe0`) 을 채운다** (`RtlpFcUpdateLocalConfiguration +0xff RtlpFcBufferManagerUpdateBuffers`). 사진 P1a 의 값 (7.3 `RtlpFcProcessManager*` 의 `*`) 은 이쪽에서 온 것이다. 그리고 그 길의 syscall (`NtQuerySystemInformationEx(0xd3)` + `NtMapViewOfSection` k + `NtClose` k) 이 짐 지도에 없었다.
  - run59 문서 맨 위에 한 줄. flow.md 2.4 run59 줄 (`Fc` 뜻), 짐 지도 3.8 둘째 줄.
- **run19 / flow.md 3장 — "스레드 풀을 만드는 첫 자리 = `+0x1a42 LdrpEnableParallelLoading`"** 은 **로더 전용 풀 (`LdrpThreadPool`)** 로는 맞지만 이 프로세스 **첫 스레드 풀**은 아니다. 기본 (전역) 풀 + 일꾼 공장 (`NtCreateWorkerFactory`) 은 그보다 앞 `+0xbe2` 에서 생긴다. 이 스프린트 문서 (flow.md) 만 고침 — 02_sprint run19 문서는 안 고친다.
- **Ghidra 주석 둘** (`0x1d23e0` = `g_pShimmedModuleList+0x8`, `0x1801cc6f0` = `NormalizationListHead+0x48`) — 둘 다 이름 없는 다른 변수다 (위 "확인된 것"). 짐 지도 7.3 의 `g_pShimmedModuleList`·`NormalizationListHead` 가 `≤P1a` 로 보인 까닭이 이 이름 없는 자리일 수 있다 `[추측]` (7.5).

## 짐 지도 반영 (`baggage.md`)

- 3.1 PEB 에 **2줄** (근거 칸 `sast57`, 줄 끝 **"Q118 로 닫힘"**): `TppWorkerpListLock` (`+0x388`, 0 을 씀) · `TppWorkerpList` (`+0x390`, 빈 목록). PEB 모르는 칸 75 → **73**, ntdll 15 → 17.
- 3.8 `.data` 에 이름 있는 전역 **28개** 를 옮김 (`≤P1a` 16 · `끝까지 0` 12) + `RtlpFcProcessManager` 둘째 줄 + **목록 밖 자리 5줄** (`0x1d23e0` · `0x1ce1f8` · `0x1ce200` · `0x1ce2a0 ~ 0x1ce2e8` · `0x1cc6f0`). `.data` 아는 것 94 → **122**, 모름 344 → **316**. 숫자는 baggage 2장.
- 3.11 `EtwDataSource` 줄 ntdll 사슬 고침 (정정), `WnfContext` 줄에 풀 만들기 한 마디.
- 4장 짝 3줄 (설정 구역 ↔ `RtlpFcProcessManager`, 기본 풀 ↔ 커널 개체, RegHandle ↔ 커널 등록), 5장 ② 4 의 `+0xaad ~ +0xc2d` 순서, 6장 부품 4줄, 7.2·7.3 목록, 7.5 실마리 (`TppPoolpList`·이름 없는 자리), 7.6 옆 짐 (기본 풀 구조·커널 개체 10 핸들·설정 구역 뷰), 새 **7.22** (동적 — Q104 VM 단계).

## 아직 확인 필요

- 동적 (결과서 "동적분석으로 확인해야 할 것" 번호 그대로 — 대기열 **Q104 VM 단계** 에 얹을 예정, 오케스트레이터가 plan 에 적음). 주소는 `ntdll` 이미지 시작 + rva, test.exe 프로세스 조건 (`/p <test EPROCESS>`):
  - **D1** `ntdll+0x8ceb1` (`+0xaad`) · `+0x8cf87` (`+0xb83`) · `+0x8cf8c` (`+0xb88`) · `+0x8d031` (`+0xc2d`) 사이 syscall 개수·종류. 가장 쉬운 길: `NtTraceControl` (`+0x164860`) · `NtCreateEvent` (`+0x1616b0`) · `NtCreateWaitCompletionPacket` (`+0x1627e0`) · `NtAssociateWaitCompletionPacket` (`+0x161fe0`) · `NtSetInformationWorkerFactory` (`+0x1643c0`) · `NtQuerySystemInformationEx` (`+0x163b60`) · `NtMapViewOfSection` (`+0x1612b0`) 입구에 시간 순 기록 (`ecx` = `NtTraceControl` 코드). 기대 30 (구간) · `1 + 2k` (앞).
  - **D2** `NtTraceControl` 입구 `ecx` 와 반환 — `0xf` ×4 · `0x1b` ×1 · `0x1e` ×1, `0xc0000023` 되풀이가 있었나 (`ntdll+0x72bc2` = `EtwpRegisterProvider +0x152`, `ntdll+0x71242` = `EtwpSetProviderTraits +0xe2` 에서 `eax`).
  - **D3** `ntdll+0x72abf` (`EtwpRegisterProvider +0x4f`) 에서 `PebLdr+0x48` (`ntdll+0x1d28c8`) = 0, `ntdll+0x1d2268` (Once 칸) 이 처음 0 → 첫 호출 뒤 2.
  - **D4** `ntdll+0x72bed` (`EtwpRegisterProvider +0x17d`) · `ntdll+0x7384e` (`EtwpUpdateEnableInfoAndCallback +0xae`) · `ntdll+0x73a60` (`EtwpEventApiCallback` 입구) — 콜백까지 가나 (안 걸려야 정상).
  - **D5** RegHandle 값: A `[ntdll+0x1cc710]`, 구간 끝 `dq ntdll+0x1d33c0 L1`·`ntdll+0x1d3d98 L1`·`ntdll+0x1ce888 L1` — `0x100000000`·`0x200000011`·`0x300000021`·`0x400000031` 인가.
  - **D6** `ntdll+0x70443` (`RtlInitializeHeapLogging +0x23`) 에서 `[ntdll+0x1ce6d0] & 8` = 0.
  - **D7** `ntdll+0xd5962` (`RtlpFcUpdateLocalConfiguration +0xb2`) 출력 버퍼 (그 함수 `rsp+0x60`, `0x68`B) 안 구역 핸들 4칸 (`+0x10`·`+0x28`·`+0x40`·`+0x58`) 중 0 이 아닌 수 = **k**, `NtMapViewOfSection` (`ntdll+0xd5b67`)·`NtClose` (`ntdll+0xd59e7`) 실제 횟수.
  - **D8** `ntdll+0xdc4e5` (`TpInitializePackage +0x61`) 반환 뒤 버퍼, `ntdll+0xdc838` (`TppQueryMaximumGroupCount +0x44`) 2번 — 1번째 `0xc0000004` · 2번째 0, `TppNumberNodes` (`ntdll+0x1d2274`)·`TppMaximumGroups` (`ntdll+0x1d2270`).
  - **D9** `ntdll+0xe5a00` (`TpAllocPoolInternal` 입구) · `ntdll+0xe5c92` (`+0x292`) · `ntdll+0xe5d03` (`+0x303`) · `ntdll+0x857f0` (`TppPoolpReferenceGlobalPool +0x140`) — 입구가 **1번만**, `LdrpEnableUMGLTracingStateSync` 안 (`ntdll+0xf7284` 뒤) 이고 ETW Once 때 (`ntdll+0x7493d` 뒤) 는 안 도는지. (flow-writer 덧붙임) `ntdll+0xe5e43` (`TpAllocPoolInternal +0x443`, 클래스 `0xd`) 이 서나 — 위 "결과서와 다르게".
  - **D10** `ntdll+0xf7284` (`RtlpInitializeWnf +0x54`) · `ntdll+0xf738b` (`RtlpWnfRegisterTpNotification +0x77`) · `ntdll+0x97a35` (`RtlpAddWnfUserSubToNameSub +0xd5`) — `NtSetWnfProcessNotificationEvent`·`NtSubscribeWnfStateChange` 각 1번, 그 직후 **일꾼 스레드가 생기나** (스레드 수·`TppWorkerThread`). 사진 P1a `ActiveThreads` 1 은 "P1a 에 살아 있는 일꾼 없음" 까지만 말한다.
  - **D11** `ntdll+0x8cebe` (`+0xaba`) `ecx` = 0 (기능 켜짐), `ntdll+0x8cef8` (`+0xaf4`) `dword [rdi+0xbc] & 0x800000` = 0.
  - **D12** `ntdll+0x8cec9` (`+0xac5`) `eax` ≠ 0, `LdrpInitializeSnapsBuffer` (`ntdll+0xfb520`) 의 블록 = `[ntdll+0x1d2758]`.
  - **D13** `ntdll+0xd5ec0` (`RtlpFcNotifyFeatureUsageTarget` 입구) 가 걸리나 — 걸리면 (b) 에 `NtQueryWnfStateData`·`NtUpdateWnfStateData`.
- 정적 (결과서 "정적으로 못 정한 것" 5 + flow-writer):
  - `TpAllocPoolInternal +0x316` 의 `[RSP+0xb8]` 출처 — `+0x443` 갈래를 타나 (위). 작다, 다음 정적 바퀴에 끼우거나 D9 로 닫음.
  - `RtlQueryInformationActivationContext` (`TppCleanupGroupMemberInitialize +0xf3/+0xf8`, 클래스 1) 안은 syscall "없음" 으로 셈 (정밀 추적 안 함).
  - (a) 기술자 의존 목록 `0x180174ac8` (`wil_details_AreDependenciesEnabled`) 안 `[공백]`.
  - `TpInitializePackage` 가 실패해 `LdrpInitializeProcess` 가 그 오류로 돌아간 뒤 `_LdrpInitialize` 가 어떻게 하는지 (결과서 7) — test.exe 는 안 탐.

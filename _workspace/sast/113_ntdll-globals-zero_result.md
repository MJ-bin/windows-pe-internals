# 113 ntdll-globals-zero — 정적 분석 결과 (Q78g2: ntdll 전역 `끝까지 0` 215 · `.mrdata` `끝까지 0` 22 · `.00cfg` `못 읽음` 4)

- 요청서: `_workspace/sast/113_ntdll-globals-zero.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), PDB guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt` — sast/111 과 같음). 커널 `ntkrnlmp.pdb` `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) = 기준값과 같음. **ntoskrnl Ghidra 프로젝트는 열지 않았다** (sast/112 가 도는 중).
- 분석: 이미 돼 있어서 다시 안 걸었다 (ntdll `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄). 시작할 때 도는 Ghidra 프로세스 없음 (`pgrep -fa 'ghidra.app.util.headless'` — 내 셸 줄만). 내 `export` 한 번 (ntdll 프로젝트, 4초, exit 0) 말고 안 돌렸다.
- **새로 뽑은 함수 15개, 전부 depth 0** (`run_ghidra.sh export ntdll.dll '^(…)$' 0`, 한 번 — `.asm` 이 없던 것만 `^이름$`, 이미 있던 `.asm`·`.c` 는 안 건드림): `ExecuteHotpatchTestRuntimeFunction` · `LdrpPayloadRestrictionMitigationsEnabled` · `LdrpInitializeCriticalSectionExceptionGlobalMitigation` · `LdrpInitializeSmtDelayedSleepQueryRegistry` · `RtlpLoadNlsData` · `LdrpGetShimEngineInterface` · `LdrpBuildImportRedirection` · `RtlpMoveActCtxToFreeList` · `IsFeatureEnabled` · `ApiSetpResolveHost` · `ApiSetpIsFeatureEnabled` · `RtlpAreLegacyDosDeviceNamesEnabled` · `RtlpInitializeStackTraceDatabase` · `EtwpInsertGuidEntry` · `LdrpGetFileSizeFromLoadAsDataTable`. (PE_emulator 가 "본문 `.asm` 이 없다" 로 빈 덫을 둔 두 함수 `LdrpInitializeCriticalSectionExceptionGlobalMitigation` · `LdrpPayloadRestrictionMitigationsEnabled` 가 이제 `.asm` 으로 있다.) `_calls.tsv` 4,328 → 4,408줄, 백업 대비 줄어든 줄 0 (`comm -23`). 로그 `ms/ghidra/ntdll.export.log`, 표준 출력 `ms/ghidra/ntdll.export.sast113a.stdout`.
- 이미 있어서 **읽기만 한 것** (다시 안 뽑음): `LdrpInitializeProcess` · `_LdrpInitialize` (objdump 풀이) · `LdrpInitializeExecutionOptions` · `LdrpInitializePolicy` · `LdrpCfgProcessLoadConfig` · `LdrpCfgCheckRoutineCallback` · `LdrpCfgDispatchRoutineCallback` · `LdrAddLoadAsDataTable` · `LdrpHandleTlsData`(풀이) · `LdrpLoadWow64` · `RtlSetHeapInformation` · `LdrpMapDllNtFileName` · `wil_details_AreDependenciesEnabled` · `wil_details_IsEnabledFallback` · `LdrpInitializeSmtDelayedSleep` · `RtlReleaseActivationContext`(풀이) 등 (`.asm`), `_map/LdrInitializeThunk_{nodes,edges}.tsv`, `_baggage/globals.tsv`, `_refs/0x1e6000.tsv`.
- **훑기 방법 + 한계 (한 줄씩)**
  - 칸 주소로 가는 모든 명령: 스크래치 objdump 풀이 `ntdll.dis` (421,449줄) 의 `# 0x…` 주석 (RIP 상대 목적지) 으로 이름 칸 `[주소, 주소+다음 이름까지 거리)` 을 훑어 쓰기 (`WRITE`·`RW`)·`lea`·읽기로 가르고, 쓰는 함수의 지도 깊이 (`m0~m10` / `out`) 를 붙였다 (241칸 한꺼번에). 한계: **레지스터에 담은 포인터로 쓰는 줄 (`[RAX]` 꼴) 은 안 잡힌다** — 그런 갈래 (`WOW64` 표 구동 · `WIL` 기술자 · `EtwpInsertGuidEntry` 의 `R15` · `LdrpHandleTlsData` 등) 는 그 함수 `.asm` 을 읽어 보탰다.
  - **`out` = 지도 밖** 에는 두 가지가 섞인다: 공개 API·오류 길 (진짜 밖) 과 **깊이 11 이상** (지도가 깊이 10 에서 끊김). 그래서 `out` 으로 쓰는 함수 148개 중 **지도 안 (`m≤10`) 호출자가 있는 17개** (`RtlpMoveActCtxToFreeList` · `RtlpPossibleDeadlock` · `RtlpInitMuiCriticalSection` · `RtlpGetCustomCultureRegKey` · `RtlpHeapTrkHash` · `RtlpSignalSystemDirsModification` · `RtlpComputeDllPath` …) 를 3단까지 거슬러 올라가 하나씩 다시 봤다 — 호출자가 조건 뒤에 있으면 "조건부 안 탐" 으로 옮겼다 (아래 표).
  - **분기 방향은 점프 목적지를 (목적지 − 함수 시작) 으로 직접 셈해 확인했다** (눈으로 "ret 로 뛸 것이다" 하고 읽은 첫 판을 `ExecuteHotpatchTestRuntimeFunction` 에서 고쳤다 — 아래 `RuntimeGlobal`).
  - 파일 값: `ntdll.dll` 구역 원본. `.data` 는 원본이 `0x2000` 바이트뿐이라 그 너머 (rva `0x1ce000` ~) 는 파일 0. 이름 칸 첫 `0x40` 바이트는 241칸 모두 파일 0 (`.00cfg` 앞 셋만 0 아님). 사진 값: `ms/logs/04_baggage-read_{P1a,P1b,P2}.txt` — **이름 칸 (첫 `0x20` 바이트) 은 `.data` · `.mrdata` 237칸 모두 P1a · P1b · P2 에서 0 (읽기 가능)** 임을 다시 확인했다 (7.3 `끝까지 0` 라벨이 맞다 — 정정 없음). `.00cfg` 는 8장 모두 읽기 불가.
- **버전 확인: 일치.** 알려진 사실 `_calls.tsv` 대조 (읽은 줄 그대로): `LdrInitializeThunk +0x9 / +0xe → LdrpInitialize` · `LdrpInitialize +0x3c tail → LdrpInitializeInternal` · `LdrpInitializeInternal +0x55 / +0x5a → _LdrpInitialize` · `_LdrpInitialize +0x167 / +0x16c → LdrpInitializeProcess` · `LdrpInitializeProcess +0x1bb6 / +0x1bbb → LdrpInitializeGraphRecurse` (옆에 `+0x16e6` · `+0x193c` 둘 더) · `LdrpInitializeNode +0x166 / +0x16b → LdrpCallTlsInitializers` · `+0x197 / +0x19c → LdrpCallInitRoutine` · `LdrpLoadDll +0xfb / +0x100 → LdrpLoadDllInternal`. 커널 줄은 ntoskrnl 쪽이라 안 열었다 (PDB GUID 만 확인).
- **오프셋 대조**: 이 결과서의 `함수 +0x…` 는 objdump 풀이 `ntdll.dis` 의 실제 명령 시작인지 스크립트로 모두 확인했다 — 맨 끝 "오프셋 대조".
- 약속: `call +0x..` = CALL 자리, `ret +0x..` = 다음 줄 (콜스택 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리. 점프 목적지는 (목적지 절대 주소 − 함수 시작) 으로 셌다. `m5` 등 = 지도 깊이 (`_map/LdrInitializeThunk_nodes.tsv`), `out` = 지도 밖.

## 스크립트 문제

없다. `run_ghidra.sh export` 한 번 문제없이 돌았다. 임시 스크립트는 전부 스크래치 (`…/scratchpad/s113/`, 레포 밖). 도구 제안은 맨 아래.

## 판단 기준 (한 줄)

sast/57·59·62·65·109·111 과 같다 (네 가름 + sast/111 이 닫은 8). **길** = ntdll 지도 (`_map/LdrInitializeThunk_nodes.tsv`, 1,248개). 칸 값이 끝까지 0 이라 "아는 칸 (값을 씀)" 은 **0 을 쓰는 경우뿐**. 셈 기준: 명시 0 저장 · 같은 칸 되쓰기만 "0 을 씀".
- **0 을 씀** = 길 안 줄이 늘 (조건 없이 또는 test.exe 가 채우는 조건에서) 이 칸에 0 을 쓴다.
- **조건부 안 탐** = 쓰는 줄이 지도 안 (또는 깊이 11+ 에서 지도 안 호출자가 부르는) 함수에 있고, test.exe 가 조건을 안 채운다. 조건은 점프 줄까지 적는다. 사진 값 0 이 "안 탐" 과 맞는다.
- **아무도 안 씀 (초기화 길 안)** = 코드가 이 칸으로 쓰는 명령이 아예 없다 (읽기 · 주소 돌려주기만). **잠금 칸** 은 따로 센다 — SRW 락 칸을 길 안 함수가 잡고 풀어 값이 0 으로 돌아오는 것 (sast/111 의 "쓰는 줄 없음 (잠금 칸)" 과 같은 취급).
- **범위 밖 길에서 씀 `[추측]`** = 쓰는 함수가 지도 밖이고 지도 안 호출자도 없다 (공개 API · 예외 · 종료 · 디버그 · 텔레메트리). 쓰는 함수 이름 하나 적는다.
- **sast/111 이 닫음** = 이 바퀴가 새로 안 보고 111 표를 가리킨다 (8개: `RtlpGlobalTagHeap` · `RtlpHpHeapFeatures` · `RtlpHpAppCompatFlags` · `g_RegInfo` · `LdrpModuleDatatableLock` · `LdrpEnforceIntegrityContinuity` · `LdrpAuditIntegrityContinuity` · `g_pShimEngineModule`).

## 한눈에

- **241칸 가름 (`.data` 215 · `.mrdata` 22 · `.00cfg` 4)**: **0 을 씀 3** (`.data`) · **조건부 안 탐 99** (`.data` 77 · `.mrdata` 18 · `.00cfg` 4) · **아무도 안 씀 51** (쓰는 코드 없음 44 + 잠금 칸 7) · **범위 밖 길에서 씀 80** (`.data` 77 · `.mrdata` 3) · sast/111 이 닫음 8. 합 3 + 99 + 51 + 80 + 8 = 241. 모름 0.
- **0 을 쓰는 곳 셋** (전부 `.data`, 값이 안 바뀌어 사진 `끝까지 0`): `RuntimeGlobal` (`ExecuteHotpatchTestRuntimeFunction +0x17`, `LdrGetProcedureAddressForCaller` 가 맨 앞에서 늘 부름) · `RtlFailedCriticalDebugAllocations` (`LdrpInitializeProcess +0x773`) · `RtlpRaiseExceptionOnPossibleDeadlock` (`LdrpInitializeCriticalSectionExceptionGlobalMitigation +0x44`, 레지스트리 값을 읽은 뒤 1 로 켤 수 있음). 111 이 "0 을 씀 후보" 로 넘긴 5개 (`RtlpForceCSToUseEvents` · `LdrpForkActiveLock` · `LdrpForkConditionVariable` · `LdrpRedirectionModule` · `RtlFailedCriticalDebugAllocations`) 중 **`RtlFailedCriticalDebugAllocations` 만 확정, 나머지 넷은 "0 을 씀" 이 아니라 조건부 안 탐** (아래 정정).
- **111 "init 줄기 직접 줄이라 0 을 씀 후보" 정정 4개**: `RtlpForceCSToUseEvents` = `PEB.AppCompatFlags` 비트 22 꺼져 안 탐 (`+0x9b3 JAE`) · `LdrpForkActiveLock`·`LdrpForkConditionVariable` = `PEB.InheritedAddressSpace ≠ 0` (복제 자식) 에서만 (`_LdrpInitialize +0x64 JNE → +0x284`) · `LdrpRedirectionModule` = `RedirectionDllName` 이 비면 `LdrpInitializeImportRedirection +0x56 JNE` 로 일 안 하고 반환. `RtlFailedCriticalDebugAllocations` 는 확정 (0 을 씀).
- **첫 판이 틀렸다가 고친 것 하나**: `RuntimeGlobal` — 처음에 `+0xb JNZ` 가 `ret` 로 뛴다고 읽어 "죽은 저장" 으로 봤으나, 목적지는 `+0x17` (`MOV [칸],0`) 이다. 비교 `CMP RAX,0x20` 는 칸 주소가 0x20 이 아니라 늘 거짓이므로 **`+0x17` 이 늘 실행**되어 칸에 0 을 쓴다 (`+0xd` 의 `0x14` 쪽이 죽은 줄). 핫패치 시험이 본문을 바꿔 비교를 참으로 만들 때만 `0x14` 가 쓰인다.
- **WIL 기능 상태 칸 (`Feature_*__private_featureState` 16)** 은 "의존 기능은 평가하되 상수 기능 (기술자 `+0x26`=1) 은 평가 안 함" 규칙 (`wil_details_AreDependenciesEnabled +0x20 ~ +0x2a`) 으로 가렸다 — 조건부 9 (ApiSet 기능 갈래 5 · 패키지 앱 갈래 3 · DOS 장치 이름 1) · 안 씀 5 (상수 기능) · 범위 밖 2 (`RtlCrc32` · MUI 언어 목록). 기능 번호 세 개 (`0x3a132a5` · `0x3a52a68` · `0x3b76c1d`) 가 ApiSet 항목에 붙었을 때만 `ApiSetpResolveHost +0x204` 가 기능을 평가한다.
- **`LdrAddLoadAsDataTable` 이 test.exe 에서 안 탔다 (정적 + 사진)** — 표 3칸 (`LoadAsDataTable*`) 이 0 인 것이 증거. sast/111 이 "정적 최단 길" 로 적은 `…LdrResSearchResource +0x727 → LdrAddLoadAsDataTable +0x37 → LdrpInitMuiCrits` 는 **P1a~P1b 에 `LoadAsDataCrits` 를 만든 길이 아니다**. 첫 부름은 나머지 셋 중 하나 (아래 5). → 3.8 의 `LoadAsDataCrits` · `LdrpInitOnceLoadAsDataCrits` 줄 `경로` 칸 정정 필요, 111 동적 2 후보 줄어듦.
- **`.00cfg` 4칸**: 모두 PE 로드 구성 `+0x118 ~ +0x130` 이 가리키는 칸. 파일 값 (재배치 뒤): `ntdll+0xfac60` (`_guard_check_icall_nop`, `ret 0`) · `ntdll+0x1664b0` 둘 (`_guard_xfg_dispatch_icall_nop`, 칸 `0x1e6000` 으로 뜀) · 0. 쓰는 줄 = 콜백 셋 (`LdrpCfgCheckRoutineCallback` · `LdrpCfgDispatchRoutineCallback` · `LdrpCgRoutineCallback`) 뿐, `LdrpCfgProcessLoadConfig +0x3a9 TEST R12D · +0x3ac JZ → +0x454` 로 CFG 강제 아닐 때 반환 → 안 탐. 사진은 8장 모두 읽기 불가 (`????`) 라 값은 `[정적 + 추정]`.
- **7.3 라벨 정정**: 없음 (이름 칸 값은 241칸 모두 `끝까지 0` / `.00cfg` `못 읽음` 라벨 그대로). 다만 `__@@_PchSym_…` 7칸은 변수가 아니라 **컴파일러 PCH 심볼 표시** 일 가능성 (코드 참조 0) — 전역 수에서 빼도 되는지는 사용자 결정 (7.24).
- **PE_emulator 쪽 새로 확인된 것**: 빈 덫 `LdrpInitializeCriticalSectionExceptionGlobalMitigation` (`Src/ntdll/LdrInit.cpp:1445`) 의 진짜 본문이 이제 `.asm` 으로 있다 — `RtlpRaiseExceptionOnPossibleDeadlock` 0 쓰기 + `NtOpenKey("…\\Session Manager")` → `NtQueryValueKey("RaiseExceptionOnPossibleDeadlock")` → `NtClose` (`+0x4b ~ +0x89`).

## 요약 표

| 구역 | 칸 | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (쓰는 코드 없음) | 아무도 안 씀 (잠금 칸) | 범위 밖 `[추측]` | sast/111 이 닫음 |
|---|---|---|---|---|---|---|---|
| `.data` `끝까지 0` | 215 | 3 | 77 | 43 | 7 | 77 | 8 |
| `.mrdata` `끝까지 0` | 22 | 0 | 18 | 1 | 0 | 3 | 0 |
| `.00cfg` `못 읽음` | 4 | 0 | 4 | 0 | 0 | 0 | 0 |
| **합** | **241** | **3** | **99** | **44** | **7** | **80** | **8** |

- 합 확인: `.data` 3 + 77 + 43 + 7 + 77 + 8 = 215 · `.mrdata` 18 + 1 + 3 = 22 · `.00cfg` 4.
- 7.3 `.data 끝까지 0` 215 중 이 표가 센 215 는 이름 칸 이름 215 와 일치한다 (7.3 줄의 `~~LdrpCorExeMainRoutine~~` · `~~UseCOR~~` 두 줄 친 이름은 217 → 215 에서 이미 뺌).

### 묶음 요약 (같은 조건으로 가른 것)

| 묶음 | 칸 | 가름 | 왜 test.exe 가 안 타나 / 왜 밖인가 (한 줄) | 대표 쓰는 줄 |
|---|---|---|---|---|
| App Verifier (`AVrf*` · `Avrfp*` · `RtlpCriticalSectionVerifier` · `RtlGuardAllowSuppressedCalls` · `LdrpShouldCreateStackTraceDb`) | 18 | 조건부 16 · 범위 밖 2 | `AvrfAppVerifierMode` 0 (`+0x1568 JE → +0x1753`) · `NtGlobalFlag & 0x2000100`·`0x100` 0 (`LdrpInitializeExecutionOptions +0x696 ~ +0x6a0` · `LdrpSendPostSnapNotifications +0x88 ~ +0x97`) · KUSD `ImageFileExecutionOptions` 비트 0 꺼짐 | `AVrfInitializeVerifier +0x1d3 ~ +0x1ea · +0x303 · +0x340 · +0x353 · +0x6d` |
| shim 엔진 콜아웃 (`g_pfnSE_*` · `g_pfnApphelpQueryModSettingsAllocProc`, 모두 `.mrdata`) | 9 | 조건부 9 | `PEB.pShimData` 0 → `LdrpInitializeProcess +0x19da JE` / `+0x19e3 JE → +0x19f4` 로 `+0x19ef LdrpInitShimEngine` 안 부름 (sast/14·61) | `LdrpGetShimEngineInterface +0x4a9 ~ +0x559` |
| WOW64 (`Wow64*` · `g_LdrpWow64*`, 모두 `.mrdata`) | 5 | 조건부 5 | `UseWOW64` 0 → `+0x1766 JE → +0x1806` 이 `+0x17c1 LdrpLoadWow64` 건너뜀. 칸은 `.rdata` `0x173830` 표 (6항목 {이름, 칸 주소}) 로 채워 코드에 칸 주소가 안 보임 | `LdrpLoadWow64 +0xe8 ~ +0xf1` |
| WIL 기능 (`Feature_*__private_reporting` 18 · `…featureState` 16 · `g_wil_*` 3) | 37 | 조건부 13 · 안 씀 20 · 범위 밖 4 | 조건부 = ApiSet 기능 번호 갈래 (`ApiSetpResolveHost +0x204`) · 패키지 앱 갈래 (`LdrpInitializePolicy +0x48`) · DOS 장치 이름 갈래. 안 씀 = 이 기능을 직접 묻는 함수가 ntdll 에 없고 의존으로 불려도 상수 기능이라 평가 안 함 / 보고 종류 0. 범위 밖 = `RtlCrc32` · MUI 언어 목록 | `…Reevaluate +0xa0` (`CMPXCHG`) · `wil_details_FeatureReporting_*` |
| SxS 활성화 문맥 추적 (`g_Sxs*` · `RtlpCapture…ActivationStacks`) | 6 | 조건부 3 · 안 씀 3 | 보존 스위치 (`g_SxsKeepActivationContextsAlive`) 가 켜질 때만 `RtlReleaseActivationContext +0xaa` 가 `RtlpMoveActCtxToFreeList` 로 꼬리 점프. 스위치 칸은 쓰는 코드 없음 | `RtlpMoveActCtxToFreeList +0x71 ~ +0xbd` |
| 힙 누수·추적·디버그 힙 (`RtlpLD*` · `RtlpLeak*` · `TrkContext` · `RtlpHp*Stack*` · `RtlpHeap*` · `RtlpHpTag*` 등) | 28 | 조건부 6 · 안 씀 6 · 범위 밖 16 | `RtlpCreateHeap +0x34e TEST EDI,0x61000000` 디버그 힙 플래그 · `RtlSetHeapInformation` 클래스 4 · `RtlpHpStackTraceEnable` 인자 비트 · `NtGlobalFlag & 0x1000` — init 호출은 클래스 1·7, `NtGlobalFlag` 0 | `RtlpBreakPointHeap +0xf` · `RtlpHpStackTraceEnable +0x32 ~ +0x82` · `RtlInitializeLeakDetection` |
| 범위 밖 공개 API·텔레메트리·예외 (`Ucp*` · `Etwp*` 로거 · `Wer*` · `Tpp*`/`RtlpTpIo*` · `RtlpPropStore*` · `RtlpLockedMemory*` · `__ft_*` · `RtlpProtectedPolicies*` 등, 임계구역 잠금 `RtlCriticalSectionLock` 은 잠금 칸으로 따로) | 57 | 조건부 4 · 범위 밖 53 | 쓰는 함수가 공개 API 이거나 오류·종료·텔레메트리 길 — 지도 밖이고 지도 안 호출자 없음 (있는 것은 조건부로 옮김) | 표의 `쓰는 줄` 칸 |
| MUI · 언어 · NLS · LoadAsData · TLS 지연 | 17 | 조건부 14 · 안 씀 1 · 잠금 1 · 범위 밖 1 | 매니페스트 `activeCodePage` 없음 (sast/52) · `.mui` 대체 모듈 안 올림 · 파일 경로 자원 갈래 안 탐 (`LdrAddLoadAsDataTable` 안 들어감) · TLS 정리 갈래 | `RtlpLoadNlsData +0x6e ~ +0xd1` · `LdrpSetAlternateResourceModuleHandle +0x2ac ~ +0x365` |
| 경로·DLL 디렉터리 (`LdrpPathLock` · `RtlpCachedPathLock` · `LdrpDllDirectoryLock` · `RtlpSearchPath` …) | 8 | 조건부 4 · 잠금 3 · 범위 밖 1 | `DllPath ""` 이라 `LdrSetDllDirectory` 안 부름 (`LdrpInitializePolicy +0x4e ~ +0x51 JE → +0x58`) · `TermsrvGetWindowsDirectoryW` 길이 0 이라 `LdrAddDllDirectory`·`RtlpSignalSystemDirsModification` 안 탐 (111) · 잠금은 늦은 경로 계산이 잡고 풂 | `LdrSetDllDirectory +0x21d` · `RtlpSignalSystemDirsModification +0x27 ~ +0x36` |
| 그 밖 갈래 (import 재지정 4 · 언로드 기록 4 · 오류 보고 3 · 복제 자식 2 · 핫패치 3 · SMT 레지스트리 1 · IFEO 3 · CFG `.00cfg` 4 + 비정렬 비트맵 1 · 임계구역 경합·디버그 4 · 잠금 칸 (`SbpContextLock` · `RtlpSecMemLock`) 2 · 예외 풀기 1 · 디버그 스위치 등 7 · PCH 표시 7 · 0 을 쓰는 둘) | 48 | 0 을 씀 3 · 조건부 25 · 안 씀 14 · 잠금 3 · 범위 밖 3 | 칸마다 표 (`조건 · 길` 칸) | 표 |
| sast/111 이 닫음 | 8 | 111 | 111 §1.4·§1.5 | — |

## 질문별 답 [정적]

### 1. 전역마다 가름 (표 + 풀이)

**표 읽는 법**: `쓰는 줄` = 함수 +명령 오프셋 (`m5` = 지도 깊이; `lea` = 주소만 넘김 — 칸 안쪽 쓰기는 부른 쪽 몫). `조건 · 길` 이 `조건 ⓐ` 처럼 글자면 아래 "공통 조건" 줄. 사진 값은 **241칸 모두 P1a · P1b · P2 에서 0** (이름 칸 첫 `0x20` 바이트 `[관찰]`) 이고 파일 값도 0 (`.00cfg` 앞 셋 제외) 이라 따로 열을 안 두었다. 칸 크기는 "다음 이름까지 거리" 짐작이라 이웃 칸이 섞일 수 있어, 쓰는 줄은 **이름 칸 `+0x0` 에 닿는 것만** 적고 이웃 칸 줄은 설명에 따로 적었다.

#### 공통 조건 (표 `조건 · 길` 칸의 글자)

- **ⓐ** (11칸): `AvrfAppVerifierMode` 0 → `LdrpInitializeProcess +0x1568 CMP · +0x156f JE → +0x1753` 이 AppVerifier 묶음 (`+0x1575 ~ +0x1750`) 을 건너뜀
- **ⓑ** (9칸): shim 엔진 올릴 때만: `LdrpInitializeProcess +0x19d7 TEST R13,R13 · +0x19da JE → +0x19f4` / `+0x19dc CMP [R13+0x210],EAX · +0x19e3 JE → +0x19f4` (`PEB.pShimData` 0 이라 `+0x19ef LdrpInitShimEngine` 안 부름, sast/14·61) → `LdrpInitShimEngine +0x109 → LdrpGetShimEngineInterface`. 다른 길 `LdrpPrepareModuleForExecution +0x88 → LdrpDynamicShimModule +0x9d → LdrpGetProcApphelpCheckModule +0x285` 도 `g_pShimmedModuleList` 0 이라 `LdrpDynamicShimModule +0x44 JE → +0x6f` 로 끝 (sast/61)
- **ⓒ** (5칸): `LdrpInitializeProcess +0x1766 CMP [UseWOW64],R15D · +0x176d JE → +0x1806` — WOW64 (32비트 프로세스) 아니면 `+0x17c1 LdrpLoadWow64` 건너뜀 (test.exe `UseWOW64` 0)
- **ⓓ** (4칸): `LdrpInitializeImportRedirection +0x53 CMP [RedirectionDllName.Length],SI · +0x56 JNE → +0x80` — `ProcessParameters +0x410 RedirectionDllName` 이 비면 바로 반환 (test.exe `""` `[관찰]` P2:3252)
- **ⓔ** (3칸): `ApiSetpResolveHost +0x1ed TEST [항목+6],4 · +0x1f1 JE → +0x211` / `+0x1fd MOV ECX,[항목+8] · +0x202 JE → +0x211` 뒤 `+0x204 ApiSetpIsFeatureEnabled → IsFeatureEnabled` — ApiSet 항목에 기능 번호 (`0x3a132a5` · `0x3a52a68` · `0x3b76c1d`) 가 붙었을 때만 (`LdrpSnapKernelBaseExtensions +0x1c5`)
- **ⓕ** (3칸): DLL 을 실제로 내릴 때만 (`LdrUnloadDll` · `LdrpDecrementModuleLoadCountEx +0xaf` → `LdrpUnloadNode +0x39 → LdrpProcessDetachNode +0x58 → LdrpRecordUnloadEvent`). init 길의 부르는 곳 (`LdrpInitializeProcess +0x1cf9` shim 실패 · `LdrpGetShimEngineInterface +0x130` · `LdrpCodeAuthzInitialize +0x554` · `RtlLogMessageInEventLogger +0x110`) 은 전부 오류·옵션 갈래 — 사진 0 = 내려간 DLL 없음
- **ⓖ** (3칸): `LdrResSearchResource +0x727 LdrAddLoadAsDataTable` — 파일 경로로 자원을 찾는 갈래 (`+0x6e3 ~ +0x727`). `LdrAddLoadAsDataTable +0x2e TEST RCX,RCX · +0x31 JE → +0x22f` 를 통과하고 `+0x37` 로 들어오면 표를 반드시 만들어 이 칸들이 써지므로, 사진 0 = 이 함수가 `+0x37` 을 안 탔다는 뜻 (아래 질문 3)
- **ⓗ** (3칸): `LdrResSearchResource +0x30b → LdrLoadAlternateResourceModuleEx +0xaea → LdrpSetAlternateResourceModuleHandle` — MUI 대체 모듈 (`.mui`) 을 처음 올릴 때만 (`+0x2d8 ~ +0x2ea` 자원 종류 검사 뒤). 내릴 때 `LdrUnloadAlternateResourceModuleEx` (m7)
- **ⓘ** (3칸): 공개 API `RtlSetProtectedPolicy` — 지도 밖 (읽기 `RtlQueryProtectedPolicy` · `RtlUnwindEx`)
- **ⓙ** (3칸): `LdrpCfgProcessLoadConfig +0x3a9 TEST R12D,R12D · +0x3ac JZ → +0x454` — `R12D` = `LdrControlFlowGuardEnforced` (`+0x187`, test.exe `LdrSystemDllInitBlock +0xb8` 0 → 0) 이면 반환. 칸을 쓰는 것은 그 뒤 `+0x70c` 간접 호출 (콜백) 뿐

#### 표 (241칸, 구역 순서 = `globals.tsv` 7.3 순서 · `.data` → `.mrdata` → `.00cfg`)

| # | 구역 | 이름 (rva) | 가름 | 쓰는 줄 | 조건 · 길 | 묶음 |
|---|---|---|---|---|---|---|
| 1 | .data | `RuntimeGlobal` (`0x1cddb8`) | 0 을 씀 | `ExecuteHotpatchTestRuntimeFunction +0x17` (`MOV [칸],0`). `+0xd` (0x14) 는 죽은 줄 | `+0x0 LEA RAX,[칸] · +0x7 CMP RAX,0x20 · +0xb JNZ → +0x17` — 주소 ≠ 0x20 이라 늘 `+0x17` 로 뛰어 0 을 씀. 부르는 곳 `LdrGetProcedureAddressForCaller +0x53` (m7) 이 맨 앞에서 늘 부름 (`LdrpInitializeKernel32Functions +0xfa · +0x12b`) | HOTPATCH |
| 2 | .data | `__@@_PchSym_…yq@ntdll` (`0x1cddbc`) | 아무도 안 씀 | 없음 (코드 참조 0) | 컴파일러 PCH 심볼 이름 `[추정]` — 변수 아님 | PCH |
| 3 | .data | `AVrfpHandleTraces` (`0x1cddc0`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x36e` (lea, 출력 칸) | 조건 ⓐ | AVRF |
| 4 | .data | `AVrfpDebug` (`0x1cddc4`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x391` (lea, 출력 칸) · 읽기 18곳 | 조건 ⓐ | AVRF |
| 5 | .data | `g_disableEscalation` (`0x1cddc8`) | 범위 밖 길에서 씀 `[추측]` | `WerEscalationLazyInit +0x332` | 부르는 곳 `WerReportSQMEvent +0xf` · `ShipAssert +0xd6` (둘 다 지도 밖) — 오류 보고 길 | WER |
| 6 | .data | `g_werEscalationData` (`0x1cddd0`) | 범위 밖 길에서 씀 `[추측]` | `WerEscalationLazyInit +0x2c5` (`LOCK CMPXCHG`) | 위와 같음 | WER |
| 7 | .data | `g_SxsCurrentDeadActivationContexts` (`0x1cde00`) | 조건부 안 탐 | `RtlpMoveActCtxToFreeList +0x8d · +0xbd` | `RtlReleaseActivationContext +0x92 CMP g_SxsKeepActivationContextsAlive · +0x9c JNE → +0xa5 ~ +0xaa (꼬리 JMP RtlpMoveActCtxToFreeList)` — 보존 스위치가 켜질 때만. 스위치 칸을 쓰는 코드 없음 (디버그용) | SXS |
| 8 | .data | `g_SxsTrackReleaseStacks` (`0x1cde04`) | 아무도 안 씀 | 없음 (읽기 `RtlReleaseActivationContext +0x3f`) | 디버그 스위치 — 쓰는 코드 없음 | SXS |
| 9 | .data | `g_SxsKeepActivationContextsAlive` (`0x1cde05`) | 아무도 안 씀 | 없음 (읽기 `RtlReleaseActivationContext +0x92` · `RtlCreateActivationContext +0x11d`) | 디버그 스위치 — 쓰는 코드 없음 | SXS |
| 10 | .data | `g_SxsFreeActivationContexts` (`0x1cde08`) | 조건부 안 탐 | `RtlpMoveActCtxToFreeList +0x71 · +0xb4` · `RtlpEnsureLiveDeadListsInitialized +0x41 · +0x3a` | 위 `g_SxsCurrentDeadActivationContexts` 와 같은 조건 (보존 스위치) | SXS |
| 11 | .data | `g_SxsLiveActivationContexts` (`0x1cde18`) | 조건부 안 탐 | `RtlpPlaceActivationContextOnLiveList +0x3e` · `RtlpEnsureLiveDeadListsInitialized +0x2c · +0x25` | 두 길: (a) `RtlReleaseActivationContext +0xaa → RtlpMoveActCtxToFreeList +0x12 → RtlpEnsureLiveDeadListsInitialized` — 보존 스위치 (`g_SxsKeepActivationContextsAlive`) 가 켜질 때만 (위 `g_SxsCurrentDeadActivationContexts`), (b) `RtlCreateActivationContext +0x129` (공개 API, 지도 밖) | SXS |
| 12 | .data | `RtlpCaptureActivationContextActivationStacks` (`0x1ce39c`) | 아무도 안 씀 | 없음 (읽기 `RtlActivateActivationContext +0x80` · `…Ex +0x9d`) | 디버그 스위치 — 쓰는 코드 없음 | SXS |
| 13 | .data | `Feature_ProjectTessera_CreateProcessInSandbox__private_reporting` (`0x1ce3a0`) | 조건부 안 탐 | `wil_details_FeatureReporting_*` 가 기술자 `+0x10` 으로 씀. 부르는 쪽 `Feature_ProjectTessera_CreateProcessInSandbox__private_IsEnabledDeviceUsageNoInline +0x37 → wil_details_IsEnabledFallback` | 조건 ⓔ | WIL-API |
| 14 | .data | `g_wil_details_subscribeFeatureStateCacheToConfigurationChanges` (`0x1ce3a8`) | 아무도 안 씀 | 없음 (읽기 `wil_details_FeatureStateCache_ReevaluateCachedFeatureEnabledState +0xad`) | 함수 포인터 칸 — 채우는 코드 없음 (구독 안 함) | WIL-hook |
| 15 | .data | `Feature_UxAccOptimization__private_reporting` (`0x1ce3b0`) | 아무도 안 씀 | 없음 | 이 기능을 직접 묻는 `Feature_…_IsEnabled…` 함수가 ntdll 에 없음 | WIL |
| 16 | .data | `Feature_Standalone_26_08_NonSec__private_reporting` (`0x1ce3b8`) | 아무도 안 씀 | 없음 | 이 기능을 직접 묻는 `Feature_…_IsEnabled…` 함수가 ntdll 에 없음 | WIL |
| 17 | .data | `Feature_Standalone_26_06_NonSec__private_reporting` (`0x1ce3c0`) | 아무도 안 씀 | 없음 | 이 기능을 직접 묻는 `Feature_…_IsEnabled…` 함수가 ntdll 에 없음 | WIL |
| 18 | .data | `Feature_DriverSetup_ReofferFingerprinting_v2__private_reporting` (`0x1ce3c8`) | 조건부 안 탐 | `wil_details_FeatureReporting_*` 가 기술자 `+0x10` 으로 씀. 부르는 쪽 `Feature_DriverSetup_ReofferFingerprinting_v2__private_IsEnabledDeviceUsageNoInline +0x37 → wil_details_IsEnabledFallback` | 조건 ⓔ | WIL-API |
| 19 | .data | `Feature_LearningModeTraceService__private_reporting` (`0x1ce3d0`) | 조건부 안 탐 | `wil_details_FeatureReporting_*` 가 기술자 `+0x10` 으로 씀. 부르는 쪽 `Feature_LearningModeTraceService__private_IsEnabledDeviceUsageNoInline +0x37 → wil_details_IsEnabledFallback` | 조건 ⓔ | WIL-API |
| 20 | .data | `Feature_SettingsDel__private_reporting` (`0x1ce3d8`) | 아무도 안 씀 | 없음 | 이 기능을 직접 묻는 `Feature_…_IsEnabled…` 함수가 ntdll 에 없음 | WIL |
| 21 | .data | `Feature_Tessera_202605Retail__private_reporting` (`0x1ce3e0`) | 아무도 안 씀 | 없음 | 이 기능을 직접 묻는 `Feature_…_IsEnabled…` 함수가 ntdll 에 없음 | WIL |
| 22 | .data | `Feature_Standalone_Future__private_reporting` (`0x1ce3e8`) | 아무도 안 씀 | 없음 | 이 기능을 직접 묻는 `Feature_…_IsEnabled…` 함수가 ntdll 에 없음 | WIL |
| 23 | .data | `g_wil_details_ensureSubscribedToFeatureConfigurationChanges` (`0x1ce3f0`) | 아무도 안 씀 | 없음 (읽기 `…Reevaluate… +0x10`) | 함수 포인터 칸 — 채우는 코드 없음 | WIL-hook |
| 24 | .data | `Feature_TestLoc02__private_reporting` (`0x1ce3f8`) | 아무도 안 씀 | 없음 | 이 기능을 직접 묻는 `Feature_…_IsEnabled…` 함수가 ntdll 에 없음 | WIL |
| 25 | .data | `Feature_ImplVal__private_reporting` (`0x1ce400`) | 아무도 안 씀 | 없음 | 이 기능을 직접 묻는 `Feature_…_IsEnabled…` 함수가 ntdll 에 없음 | WIL |
| 26 | .data | `g_wil_details_pfnFeatureLoggingHook` (`0x1ce408`) | 아무도 안 씀 | 없음 (읽기 `wil_details_FeatureReporting_ReportUsageToService +0x3e`) | 로깅 훅 포인터 — 채우는 코드 없음 | WIL-hook |
| 27 | .data | `Feature_TestLoc03__private_reporting` (`0x1ce410`) | 아무도 안 씀 | 없음 | 이 기능을 직접 묻는 `Feature_…_IsEnabled…` 함수가 ntdll 에 없음 | WIL |
| 28 | .data | `MuiCacheSWRLock` (`0x1ce420`) | 아무도 안 씀 · 잠금 칸 | `LdrpGetFromMUIMemCache +0xce` (`LOCK CMPXCHG` 잡기, m10) · lea `LdrpSetAlternateResourceModuleHandle +0x58 · +0x45b` · `LdrUnloadAlternateResourceModuleEx +0x2c · +0x1d1` | MUI 대체 자원 모듈 캐시 SRW 락 (`LdrResSearchResource +0x30b → LdrLoadAlternateResourceModuleEx +0x119`). 잡고 풀면 0 으로 돌아옴 — 사진 0 과 안 어긋남 | MUI |
| 29 | .data | `NlsEmptyLeadByteInfoTable` (`0x1ce440`) | 아무도 안 씀 | 없음 (`RtlpInitCodePageTables +0xcc` 가 lea 로 주소만 씀) | 빈 선두 바이트 표 (0x200B, 0 이 설계) — 값을 쓰는 코드 없음 (sast/52 와 같음) | NLS |
| 30 | .data | `Feature_KVS_BugFixes_55830752__private_reporting` (`0x1ce648`) | 아무도 안 씀 | 없음 | KVS 를 직접 묻는 함수 없음 — 상태 칸만 `DisallowSharedLock` 의 필요 기능으로 평가 (sast/111) | WIL |
| 31 | .data | `Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_reporting` (`0x1ce650`) | 아무도 안 씀 | 없음 | `LdrpInitializeProcess +0xaad` 의 `…IsEnabledNoReportingNoInline` 이 보고 종류 0 → `wil_details_IsEnabledFallback +0x43 TEST ESI,ESI · +0x45 JNZ` 안 탐 | WIL |
| 32 | .data | `RtlpCsVerifyDoNotBreak` (`0x1ce658`) | 아무도 안 씀 | 없음 (읽기 `RtlCheckHeldCriticalSections +0x7b`) | 디버그 스위치 | CS |
| 33 | .data | `PossibleDeadlockReportCounter` (`0x1ce65c`) | 조건부 안 탐 | `RtlpPossibleDeadlock +0x84` (`LOCK XADD`) | `RtlEnterCriticalSection +0xed (m6) → RtlpEnterCriticalSectionContended` 경합 대기 → `RtlpWaitOnCriticalSection +0x5f4 → RtlpPossibleDeadlock` — 임계구역을 오래 기다릴 때만 (`RtlpTimeout` −30일 이라 사실상 안 감, `RtlpTimeoutDisable` 1) `[추정]` | CS |
| 34 | .data | `RtlpCriticalSectionVerifier` (`0x1ce660`) | 조건부 안 탐 | `AVrfpEnableVerifierOptions +0x8b` | `AVrfInitializeVerifier +0x400` 아래 — AVRF 묶음 (`+0x1568`) | AVRF |
| 35 | .data | `RtlpLockedMemoryBlockLookasideCount` (`0x1ce664`) | 범위 밖 길에서 씀 `[추측]` | `RtlpRegisterLockedMemoryBlockLookaside +0x29` · `Unregister… +0x16` | 잠긴 메모리 룩어사이드 등록 API (지도 밖) | LOCKMEM |
| 36 | .data | `RtlpMemoryBlockLookasideLock` (`0x1ce668`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlpRegister/UnregisterLockedMemoryBlockLookaside` (잠금) | 위와 같음 — 잠금 칸 | LOCKMEM |
| 37 | .data | `RtlpMemoryZoneLock` (`0x1ce670`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlpRegister/UnregisterLockedMemoryZone` (잠금) | 잠긴 메모리 구역 등록 API (지도 밖) — 잠금 칸 | LOCKMEM |
| 38 | .data | `RtlpLockedMemoryZoneCount` (`0x1ce678`) | 범위 밖 길에서 씀 `[추측]` | `RtlpRegisterLockedMemoryZone +0x24` · `Unregister… +0x10` | 위와 같음 (옆 칸 `+0x4` 는 `RtlpGetTimeZoneInfoHandle` — 다른 변수) | LOCKMEM |
| 39 | .data | `Feature_GetPreferredThreadLanguage_Watson_AccessViolation_BugFix__private_reporting` (`0x1ce680`) | 범위 밖 길에서 씀 `[추측]` | `Feature_GetPreferredThreadLanguage…IsEnabledDeviceUsageNoInline` (지도 밖) | 부르는 곳 `RtlpMuiRegTryToAppendLangId +0x15d` · `RtlpLangNameInMultiSzString_Size +0x4b` — 지도 밖 (MUI 언어 목록) | WIL-out |
| 40 | .data | `InitRegistryInfoCritSect` (`0x1ce688`) | 조건부 안 탐 | `RtlpInitMuiCriticalSection +0x1a · +0x50` | `LdrpGetRcConfig +0xbe` · `LdrIsResItemExist +0x97` (m10) → `RtlpInitMuiCriticalSection` / `LdrpSetThreadPreferredLangList` — MUI 자원 구성·언어 대체 목록 갈래 (`LdrResFallbackLangList +0x12f` 도). 사진 0 = 안 탐 | MUI |
| 41 | .data | `g_RegInfo` (`0x1ce690`) | sast/111 이 닫음 | 조건부 안 탐 (`RtlInitializeHeapManager +0x5b`) | 111 §1.4 | 111 |
| 42 | .data | `RtlpGlobalTagHeap` (`0x1ce6a8`) | sast/111 이 닫음 | 조건부 안 탐 (`RtlCreateTagHeap +0x97`) | 111 §1.4 | 111 |
| 43 | .data | `RtlpPropStoreLock` (`0x1ce6b0`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlCompareExchangePropertyStore` · `RtlQueryPropertyStore` · `RtlRemovePropertyStore` (잠금) | 프로퍼티 저장소 공개 API (지도 밖) | PROPSTORE |
| 44 | .data | `RtlpPtrTreeLock` (`0x1ce6b8`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlCompareExchangePointerMapping` · `RtlQueryPointerMapping` · `RtlRemovePointerMapping` (잠금) | 포인터 매핑 공개 API (지도 밖) | PROPSTORE |
| 45 | .data | `RtlpFreezeTimeBias` (`0x1ce6c0`) | 아무도 안 씀 | 없음 (읽기 22곳) | 시간 편차 얼림 디버그 스위치 — 쓰는 코드 없음 | MISC |
| 46 | .data | `Feature_FasterCrc32__private_reporting` (`0x1ce6c8`) | 범위 밖 길에서 씀 `[추측]` | `Feature_FasterCrc32…IsEnabledDeviceUsageNoInline` (지도 밖) | 부르는 곳 `RtlCrc32 +0x18` — 지도 밖 | WIL-out |
| 47 | .data | `RtlpHpHeapFeatures` (`0x1ce6d0`) | sast/111 이 닫음 | 조건부 안 탐 | 111 §1.4 | 111 |
| 48 | .data | `RtlpSecMemLock` (`0x1ce6f8`) | 아무도 안 씀 · 잠금 칸 | lea `RtlpCallSecureMemoryCallbacks +0x2a · +0x69 · +0x8f · +0xf9` (m8, 잡고 풂) · `RtlRegister/DeregisterSecureMemoryCacheCallback` (공개 API) | 보안 메모리 해제 콜백 길이 잡고 풂 — 값은 0 으로 돌아옴 | SECMEM |
| 49 | .data | `RtlpLDNumBlocks` (`0x1ce700`) | 범위 밖 길에서 씀 `[추측]` | `RtlpReadProcessHeaps +0x77` · `RtlpLeakCallbackRoutine +0x70 · +0xba · +0x164` | 힙 누수 검출 (`RtlDetectHeapLeaks` 류) — 지도 밖 | LEAK |
| 50 | .data | `RtlpTempBlocks` (`0x1ce708`) | 범위 밖 길에서 씀 `[추측]` | `RtlpInitializeLeakDetection +0x8d` | 힙 누수 검출 (`RtlDetectHeapLeaks` 류) — 지도 밖 | LEAK |
| 51 | .data | `RtlpLeakHeapAddress` (`0x1ce710`) | 범위 밖 길에서 씀 `[추측]` | `RtlDetectHeapLeaks +0xd1` | 힙 누수 검출 (`RtlDetectHeapLeaks` 류) — 지도 밖 | LEAK |
| 52 | .data | `RtlpLeaksCount` (`0x1ce718`) | 범위 밖 길에서 씀 `[추측]` | `RtlpScanHeapAllocBlocks +0xa9` · `RtlDetectHeapLeaks +0x64` | 힙 누수 검출 (`RtlDetectHeapLeaks` 류) — 지도 밖 | LEAK |
| 53 | .data | `RtlpCrtHeapAddress` (`0x1ce720`) | 범위 밖 길에서 씀 `[추측]` | `RtlpLeakCallbackRoutine +0x16b` | 힙 누수 검출 (`RtlDetectHeapLeaks` 류) — 지도 밖 | LEAK |
| 54 | .data | `RtlpPreviousStartAddress` (`0x1ce728`) | 범위 밖 길에서 씀 `[추측]` | `RtlpPushPageDescriptor +0x125` | 힙 누수 검출 (`RtlDetectHeapLeaks` 류) — 지도 밖 | LEAK |
| 55 | .data | `RtlpShutdownProcessFlags` (`0x1ce730`) | 조건부 안 탐 | lea `LdrpInitializeExecutionOptions +0x1a2` (IFEO `ShutdownFlags` 읽기 출력 칸) | IFEO 부분 통째 건너뜀: `+0xa6 TEST [PP+8],0x4000 · +0xad JNZ → +0x604` (test.exe `Flags 0x4001`) | IFEO |
| 56 | .data | `RtlpLDPreviousPage` (`0x1ce738`) | 범위 밖 길에서 씀 `[추측]` | `RtlpReadProcessHeaps +0x6f` · `RtlpLeakCallbackRoutine +0xe4 · +0x10a · +0x158` | 힙 누수 검출 (`RtlDetectHeapLeaks` 류) — 지도 밖 | LEAK |
| 57 | .data | `RtlpLDCrtPage` (`0x1ce740`) | 범위 밖 길에서 씀 `[추측]` | `RtlpLeakCallbackRoutine +0x4c` | 힙 누수 검출 (`RtlDetectHeapLeaks` 류) — 지도 밖 | LEAK |
| 58 | .data | `RtlpValidateHeapHdrsEnable` (`0x1ce748`) | 아무도 안 씀 | 없음 (읽기 `RtlpValidateHeapHeaders +0xf`, m7) | 디버그 스위치 — 쓰는 코드 없음 | HEAPDBG |
| 59 | .data | `__ft_g_isTestReadyMedia` (`0x1ce74c`) | 범위 밖 길에서 씀 `[추측]` | `RtlpFtInitialize +0x55` | 기능 토글 초기화 (`RtlpFtInitOnceCallback` 아래) — 지도 밖 | FT |
| 60 | .data | `__ft_g_init_once` (`0x1ce750`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlpFtQueryConfiguration +0x2d` (RunOnce) | 위와 같음 | FT |
| 61 | .data | `g_dwLastErrorToBreakOn` (`0x1ce758`) | 아무도 안 씀 | 없음 (읽기 `RtlRestoreLastWin32Error +0xd` (m6) 외 2곳) | 디버그 스위치 — 쓰는 코드 없음 | MISC |
| 62 | .data | `gCustomCultureRegKey` (`0x1ce760`) | 조건부 안 탐 | `RtlpGetCustomCultureRegKey +0x42` (`LOCK CMPXCHG`) | `LdrResFallbackLangList +0x4a2` (m10) · `RtlCultureNameToLCID +0x1bc` → `RtlpIsCustomLocale +0x39` — MUI 언어 대체 갈래. 사진 0 = 안 탐 | LOCALE |
| 63 | .data | `gLocaleDataRegKey` (`0x1ce768`) | 조건부 안 탐 | `RtlpGetLocaleDataKey +0x7c` (`LOCK CMPXCHG`, 키 핸들 캐시) | `RtlpGetUserLocaleName +0x34` (m10) ← `RtlLcidToLocaleName +0x207` ← `LdrResSearchResource +0x3de` — 언어 이름 갈래 (사진 0 = 안 탐) | LOCALE |
| 64 | .data | `gLocaleTables` (`0x1ce770`) | 조건부 안 탐 | `RtlpLoadNlsData +0x6e · +0x79 · +0x84 · +0x8f · +0x9c · +0xa9 · +0xb6 · +0xc3` (칸마다) | `LdrpInitializeProcess +0x1c8 → LdrpInitializeNlsInfo +0x2a → LdrpFusionManifestCodePages +0xbc RtlpGetProcessCodepagesForLocale → +0x28 RtlpLoadNlsData` — 매니페스트 `activeCodePage` 가 있을 때만 (sast/52: `LdrpFusionManifestCodePages +0x4e` 의 `RtlQueryActivationContextApplicationSettings` 가 음수 → `+0x57 ~ +0x5a` 로 0 반환) | NLS |
| 65 | .data | `pTblPtrs` (`0x1ce7a8`) | 조건부 안 탐 | `RtlpLoadNlsData +0xd1` | `LdrpInitializeProcess +0x1c8 → LdrpInitializeNlsInfo +0x2a → LdrpFusionManifestCodePages +0xbc RtlpGetProcessCodepagesForLocale → +0x28 RtlpLoadNlsData` — 매니페스트 `activeCodePage` 가 있을 때만 (sast/52: `LdrpFusionManifestCodePages +0x4e` 의 `RtlQueryActivationContextApplicationSettings` 가 음수 → `+0x57 ~ +0x5a` 로 0 반환) | NLS |
| 66 | .data | `TrkContext` (`0x1ce800`) | 조건부 안 탐 | `RtlpHeapTrkSyncWithDiagnoser +0x5a` · `RtlpHeapTrkLeakCallback +0x16a` · lea `RtlHeapTrkInitialize +0x145 · +0x189 · +0x245` · lea `RtlpHeapTrkHash +0x1e` (m10) | 힙 추적 (`RtlHeapTrkInitialize`, `RtlpHeapTrkTrackAdd +0x37` → `RtlpHeapTrkHash`) 이 켜진 힙에서만 `[추정]` — 사진 0 | LEAK |
| 67 | .data | `RtlpLockedSectionListLock` (`0x1ce840`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlLockModuleSection` · `RtlUnlockModuleSection` (잠금) | 공개 API — 지도 밖 | LOCKMOD |
| 68 | .data | `RtlpProtectedPoliciesSRWLock` (`0x1ce848`) | 조건부 안 탐 | lea `RtlUnwindEx +0xaed · +0xb25` (m9) · `RtlRestoreContext +0x238 · +0x270` (m10) · 쓰는 줄은 지도 밖 `RtlCloneUserProcess +0x23f` · `RtlCompleteProcessCloning +0x80` | 예외 풀기 (`_local_unwind +0x1e ← LdrAddDllDirectory +0x1a5`) 가 실제로 돌 때만 — 정상 길엔 안 탐 `[추정: 예외 때만]` | UNWIND |
| 69 | .data | `RtlpPerformanceCounterFrequency` (`0x1ce850`) | 범위 밖 길에서 씀 `[추측]` | `RtlCapabilityCheck +0x5fe` | 공개 API — 지도 밖 | CAPCHK |
| 70 | .data | `RtlpCapChkTelemetryRunOnceCtx` (`0x1ce858`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlCapabilityCheck +0x3ba · +0x5f4` (RunOnce) | 위와 같음 | CAPCHK |
| 71 | .data | `RtlpHpNullGUID` (`0x1ce860`) | 아무도 안 씀 | 없음 (읽기 `RtlpHpTagAllocateHeap +0xd8 · +0xe9`) | 0 GUID 상수 — 쓰는 코드 없음 | HEAPTAG |
| 72 | .data | `RtlpHpTagInitVar` (`0x1ce870`) | 조건부 안 탐 | lea `RtlSetHeapInformation +0xa4` (RunOnce 인자) | `RtlSetHeapInformation +0x86 CMP EDX,4` 갈래 (클래스 4) 만. init 의 두 호출은 클래스 1 (`LdrpInitializeProcess +0xa34`, `EDX=EBX=1`) · 7 (`LdrpInitializeTlsHeap +0x9b`, `EDX=R9D−1`) | HEAPTAG |
| 73 | .data | `RtlpHeapStackTraceLog` (`0x1ce890`) | 범위 밖 길에서 씀 `[추측]` | `RtlpInitializeStackTraceLog +0x79` (`LOCK CMPXCHG`) | 지도 밖 (힙 스택 추적 로그) | HEAPDBG |
| 74 | .data | `RtlpHpStackTraceLogInstance` (`0x1ce898`) | 범위 밖 길에서 씀 `[추측]` | `RtlpHpStackTraceEtwCallback +0x52` (`LOCK XADD`) | ETW 콜백 — 지도 밖 | HEAPDBG |
| 75 | .data | `RtlpHpStackTrackingContext` (`0x1ce8a0`) | 조건부 안 탐 | `RtlpHpStackTraceEnable +0x32 · +0x43 · +0x4a ~ +0x82` (m7) · `RtlpHpStackTraceDisable +0x19` · `RtlpHpStackTraceAddStack +0x62` (m9) · `RtlpHpStackTraceRemoveStack +0x60` | `RtlpHpStackTraceEnable` 는 `RtlpHpSetLowFragHeapGlobalFlags +0x63 TEST BL,0x10 · +0x66 JE` (인자 ESI 에 `0x10` 없음; `RtlSetGlobalHeapFeatures +0x1c TEST RBX,RBX · +0x1f JE`) / `RtlpHpStackTraceConfig +0x4b TEST DIL,1` ← `RtlSetHeapInformation +0x1f1` (init 호출 클래스 아님) | HEAPDBG |
| 76 | .data | `RtlpHpAppCompatFlags` (`0x1ce8f8`) | sast/111 이 닫음 | 조건부 안 탐 | 111 §1.4 | 111 |
| 77 | .data | `g_isUcpListInitialized` (`0x1d21ec`) | 범위 밖 길에서 씀 `[추측]` | `UcpInitOnUnexpectedCodePathOnceCallBack +0xb` · `UcpUnInitializeListHead +0x5d` | 미예상 코드 길 텔레메트리 (`UcOnUnexpectedCodePath` 류) — 지도 밖 | UCP |
| 78 | .data | `UcpExclusiveLock` (`0x1d21f0`) | 범위 밖 길에서 씀 `[추측]` | lea `UcOnUnexpectedCodePath +0xa9 · +0x12a · +0x28a` (잠금) | 미예상 코드 길 텔레메트리 (`UcOnUnexpectedCodePath` 류) — 지도 밖 | UCP |
| 79 | .data | `UcpInitOnUnexpectedCodePathOnce` (`0x1d21f8`) | 범위 밖 길에서 씀 `[추측]` | lea `UcOnUnexpectedCodePath +0x62` (RunOnce) | 미예상 코드 길 텔레메트리 (`UcOnUnexpectedCodePath` 류) — 지도 밖 | UCP |
| 80 | .data | `UcpMaxTriggeredNodeExcceded` (`0x1d2200`) | 범위 밖 길에서 씀 `[추측]` | `UcpFindOrCreateTelemetryRecord +0xec` | 미예상 코드 길 텔레메트리 (`UcOnUnexpectedCodePath` 류) — 지도 밖 | UCP |
| 81 | .data | `UcpLockOwningThreadHandle` (`0x1d2208`) | 범위 밖 길에서 씀 `[추측]` | `UcOnUnexpectedCodePath +0xba · +0x135 · +0x291` | 미예상 코드 길 텔레메트리 (`UcOnUnexpectedCodePath` 류) — 지도 밖 | UCP |
| 82 | .data | `UcIsTraceLoggingInitialized` (`0x1d2210`) | 범위 밖 길에서 씀 `[추측]` | `UcpInitOnUnexpectedCodePathOnceCallBack +0x31` (이름 칸 `+0x0`) | 지도 밖. 옆 칸 `+0x4` 는 `RtlpHpHeapCreate +0x330` (m7, 세그먼트 힙 만들 때 — test.exe 는 NT 힙, sast/40) | UCP |
| 83 | .data | `RtlpHpEnvThreadSuspendOwnershipLock` (`0x1d2228`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlpHpLfhPrivateSlotListCompact +0x98 · +0x19e` (잠금) | 세그먼트 힙 LFH 정리 — 지도 밖 | HEAPDBG |
| 84 | .data | `EtwpGuidEntryTable` (`0x1d2248`) | 조건부 안 탐 | `EtwpInsertGuidEntry` (m10) — `lea R15,[칸]` (`+0x2e`) 뒤 레드-블랙 트리 넣기 (레지스터 간접). 읽기 `EtwpFindGuidEntry +0x141` (m8) | `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation +0x6e → EtwEventSetInformation +0x2e → EtwpSetProviderTraits +0x110 → EtwpUpdateEnableInfoAndCallback +0x282 → EtwpAllocateUmGuidEntry +0x54 → EtwpInsertGuidEntry` — 공급자 특성 (traits) 을 넣는 TraceLogging 등록 때만 `[추정: 갈래 조건 안 봄]`. 사진 0 = 안 탐 | ETW |
| 85 | .data | `EtwpPrivSessionLock` (`0x1d2260`) | 범위 밖 길에서 씀 `[추측]` | lea `EtwProcessPrivateLoggerRequest +0x58 · +0x9e · +0x156` (잠금) | 지도 밖 | ETW |
| 86 | .data | `TppPoolpSerializedPoolLock` (`0x1d2278`) | 조건부 안 탐 | lea `TppCleanupGroupMemberInitialize +0x28f` (m7) · `TpReleasePool +0x208` (m9) · `…Destroy +0x1ee` (잠금) | `TppCleanupGroupMemberInitialize +0x146 JNE → +0x28f` — 호출 환경 (`TP_CALLBACK_ENVIRON`) 의 풀이 직렬화 풀 표시일 때만 `[추정: 환경 갈래]` (init 의 로더 풀 일감은 일반 풀) | TP |
| 87 | .data | `TppPoolpGlobalPoolStackSize` (`0x1d2280`) | 범위 밖 길에서 씀 `[추측]` | `TpSetDefaultPoolStackInformation +0x7c` | 공개 API — 지도 밖 | TP |
| 88 | .data | `TppPoolpSerializedPool` (`0x1d2290`) | 조건부 안 탐 | lea `TppCleanupGroupMemberInitialize +0x296` · `TppPoolpReferenceGlobalPool +0x19e` (`+0xf3 JNE`) · `TpReleasePool +0x20f` · `…Destroy +0x1f5` | `TppCleanupGroupMemberInitialize +0x146 JNE → +0x28f` 직렬화 풀 갈래 (바로 위 `TppPoolpSerializedPoolLock` 줄과 같음). 칸 주소는 풀 포인터 자리에 들어가는 표시 주소 (`TppPoolpReferenceGlobalPool +0x1a5 CMP RBX,RAX`) | TP |
| 89 | .data | `TppPoolpGlobalPoolMaxThreads` (`0x1d22ac`) | 범위 밖 길에서 씀 `[추측]` | `TpSetDefaultPoolMaxThreads +0xfc` | 공개 API — 지도 밖 | TP |
| 90 | .data | `TppCleanupGroupMemberpNoPoolListLock` (`0x1d22c0`) | 조건부 안 탐 | lea `TppCleanupGroupMemberInitialize +0x249` (m7) · `…Destroy +0x21d · +0x25a` (잠금) | 111 `TppCleanupGroupMemberpNoPoolList` 와 같은 갈래: 멤버 `[+0x90]` 이 `+0x12b ~ +0x173` 뒤에도 0 일 때만 (`+0x17d JE → +0x249`) | TP |
| 91 | .data | `RtlpTpIoRegistered` (`0x1d22c8`) | 범위 밖 길에서 씀 `[추측]` | `RtlSetIoCompletionCallback +0xff` | `RtlSetIoCompletionCallback` 공개 API 길 — 지도 밖 | TPIO |
| 92 | .data | `RtlpTpIoTreeLock` (`0x1d22d0`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlpTpIoLookup +0x22 · +0x121` · `RtlpTpIoDllNotification +0x32 · +0x95` · `RtlpTpIoDllUnloaded +0xe · +0x5c` (잠금) | `RtlSetIoCompletionCallback` 공개 API 길 — 지도 밖 | TPIO |
| 93 | .data | `RtlpTpIoRegisteredLock` (`0x1d22d8`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlSetIoCompletionCallback +0xbf · +0x106` (잠금) | `RtlSetIoCompletionCallback` 공개 API 길 — 지도 밖 | TPIO |
| 94 | .data | `RtlpTpIoTree` (`0x1d22e0`) | 범위 밖 길에서 씀 `[추측]` | `RtlpTpIoLookup +0x11a` · `RtlpTpIoDllNotification +0x75` | `RtlSetIoCompletionCallback` 공개 API 길 — 지도 밖 | TPIO |
| 95 | .data | `RtlpTpIoDllNotificationCookie` (`0x1d22e8`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlSetIoCompletionCallback +0xdd` (DLL 알림 등록 출력 칸) | `RtlSetIoCompletionCallback` 공개 API 길 — 지도 밖 | TPIO |
| 96 | .data | `RtlpDiskSpeedInit` (`0x1d22f0`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlpQueryDiskSpeedPolicy +0x29` (RunOnce) | 111 `RtlpDiskSpeedPolicy` 와 같은 길 (직접 부르는 곳이 `_calls.tsv` 에 없음) — 지도 밖. `+0x8`·`+0xc` 는 `GetGlobalizationUserModelType`·`OpenGlobalizationUserSettingsKey…` 의 다른 변수 | DISKSPEED |
| 97 | .data | `g_MicrosoftTelemetryAssertLock` (`0x1d2300`) | 범위 밖 길에서 씀 `[추측]` | lea `MicrosoftTelemetryAssertTriggeredWorker +0x4d · +0x1d6` (잠금) | 단언 텔레메트리 — 지도 밖 | TELEM |
| 98 | .data | `g_MicrosoftTelemetryAssertsTriggeredList` (`0x1d2308`) | 범위 밖 길에서 씀 `[추측]` | `MicrosoftTelemetryAssertTriggeredWorker +0xcf` | 위와 같음 | TELEM |
| 99 | .data | `LdrpPathLock` (`0x1d2318`) | 아무도 안 씀 · 잠금 칸 | lea `LdrpComputeLazyDllPath +0x19 · +0xbf` (m8, 잡고 풂) | 늦은 DLL 경로 계산 (`LdrpSearchPath +0x36d`) 이 잡고 풂 — 값 0 으로 돌아옴 | PATH |
| 100 | .data | `RtlpCachedPathLock` (`0x1d2320`) | 아무도 안 씀 · 잠금 칸 | lea `LdrpReleaseDllPath +0x2f` (m5, 뒤 `LOCK BTS [RDI]`) · `LdrpResolveProcedureAddress +0x283 · +0x298` (m7) · `RtlpGetCachedPath` (m10) · `RtlReleasePath +0x12 · +0x28` (m7) · `RtlpSignalSystemDirsModification +0xf` | 캐시된 경로 SRW 락 — 길 안 여러 곳이 잡고 풂 (정확한 첫 잡는 줄은 동적). `LdrSetDllDirectory +0x236…` 의 `LOCK CMPXCHG` 는 `LdrpInitializePolicy +0x4e CMP [DllPath.Length],BX · +0x51 JE` 로 안 탐 (test.exe `DllPath ""`) | PATH |
| 101 | .data | `LdrpDllDirectoryLock` (`0x1d2388`) | 아무도 안 씀 · 잠금 칸 | lea `RtlpComputeDllPath +0x52 · +0x7f · +0xaf · +0xce` · `RtlpComputeDllPathWithOptions +0x24 · +0xc9` (m11, `RtlpGetCachedPath +0xbe · +0xe9` 가 부름) · `LdrSetDllDirectory +0xd6…` (`LOCK CMPXCHG`) · `LdrAddDllDirectory +0x14c · +0x1c9` | DLL 디렉터리 SRW 락 — 늦은 경로 계산 (`LdrpComputeLazyDllPath → LdrpGetDllPath +0xa8 → RtlpGetCachedPath`) 이 잡고 풂. `LdrSetDllDirectory` (`DllPath` 길이 0) · `LdrAddDllDirectory` (`TermsrvGetWindowsDirectoryW` 길이 0, 111) 는 안 탐 | PATH |
| 102 | .data | `LdrpAppPackagesPathVersion` (`0x1d2390`) | 범위 밖 길에서 씀 `[추측]` | `LdrUpdatePackageSearchPath +0xa2` · `LdrpResetAppPackagesPath +0x28` (INC) | 패키지 앱 경로 — 지도 밖 | PATH |
| 103 | .data | `RtlpUnloadEventTraceEx` (`0x1d23b0`) | 조건부 안 탐 | `LdrpRecordUnloadEvent +0x1f2` | 조건 ⓕ | UNLOAD |
| 104 | .data | `LdrpUnloadIndex` (`0x1d23b8`) | 조건부 안 탐 | `LdrpRecordUnloadEvent +0x47` | 조건 ⓕ | UNLOAD |
| 105 | .data | `LdrpUnloadIndexEx` (`0x1d23bc`) | 조건부 안 탐 | `LdrpRecordUnloadEvent +0x119` | 조건 ⓕ | UNLOAD |
| 106 | .data | `LdrpShouldCreateStackTraceDb` (`0x1d23cd`) | 조건부 안 탐 | `LdrpInitializeApplicationVerifierPackage +0xd8` (`SETNE`) · `+0x143` | `LdrpInitializeExecutionOptions +0x696 TEST [PEB+0xbc],0x2000100 · +0x6a0 JNZ → +0x73d`, `+0x6a6 LdrpPayloadRestrictionMitigationsEnabled · +0x6ad JNZ → +0x73d` — test.exe `NtGlobalFlag` 0, 완화 옵션 `LdrSystemDllInitBlock +0xa8 · +0xe0` 0 | AVRF |
| 107 | .data | `Feature_TreatDosDevicePathsAsNormalFiles__private_reporting` (`0x1d2400`) | 조건부 안 탐 | `RtlpAreLegacyDosDeviceNamesEnabled +0x12d` (`ReportUsageToService`, m9) | `RtlpIsDosDeviceName_Ustr +0x536 AND BL,6 · +0x539 CMP BL,4 · +0x53c JE` 뒤 `+0x542` — 옛 DOS 장치 이름 비교 갈래 `[추정]` | WIL-DOS |
| 108 | .data | `LdrpForkActiveLock` (`0x1d2410`) | 조건부 안 탐 | `_LdrpInitialize +0x336` (`MOV [칸],RBX`, RBX=0) | `+0x62 CMP [PEB+0],BL · +0x64 JNE → +0x284` (`PEB.InheritedAddressSpace ≠ 0` — `RtlCloneUserProcess` 자식) 안, `+0x29b MOV AL,[RSI] · +0x29f JNE → +0x32d`. test.exe `InheritedAddressSpace` 0 `[관찰]` | CLONE |
| 109 | .data | `LdrpForkConditionVariable` (`0x1d2418`) | 조건부 안 탐 | `_LdrpInitialize +0x33d` | 위와 같음 | CLONE |
| 110 | .data | `LdrpVchLock` (`0x1d2440`) | 범위 밖 길에서 씀 `[추측]` | 코드 참조 0 — `.mrdata` `LdrpVectorHandlerList +0x18` 이 가리키는 잠금 (`0x1ea590`) | 벡터 예외 처리기 등록 API (`RtlpAddVectoredHandler` · `RtlpRemoveVectoredHandler`, 지도 밖) | VEH |
| 111 | .data | `LdrpVehLock` (`0x1d2448`) | 범위 밖 길에서 씀 `[추측]` | 코드 참조 0 — `.mrdata` `LdrpVectorHandlerList +0x0` 이 가리키는 잠금 (`0x1ea578`) | 위와 같음 | VEH |
| 112 | .data | `RtlCriticalSectionLock` (`0x1d2450`) | 아무도 안 씀 · 잠금 칸 | lea `RtlpAddDebugInfoToCriticalSection +0xbe` (m6) · `RtlDeleteCriticalSection +0x84 · +0xba` (m7). 복제 뒤 0 을 쓰는 줄은 지도 밖 `RtlCloneUserProcess +0x1d6` · `RtlCompleteProcessCloning +0x1a` | 임계구역 디버그 정보 목록 락 — 111 의 `RtlCriticalSectionList` 쓰는 줄 (`+0x349 ~ +0x35d`) 과 같은 길이 잡고 풂 | CS |
| 113 | .data | `LdrpModuleDatatableLock` (`0x1d2458`) | sast/111 이 닫음 | 잠금 칸 | 111 §1.5 | 111 |
| 114 | .data | `LdrpDelayedTlsReclaimTable` (`0x1d2470`) | 조건부 안 탐 | lea `LdrpQueueDeferredTlsData +0x1a` (m9) | `LdrpHandleTlsData +0x428 JNE → +0x558 ~ +0x560 call LdrpQueueDeferredTlsData` — TLS 데이터 지연 반환 갈래 `[추정]` | TLS |
| 115 | .data | `LdrpCODScenarioTriggered` (`0x1d2570`) | 조건부 안 탐 | `LdrpCheckComponentOnDemandEtwEvent +0x11f` (m9) | `LdrpMapDllNtFileName +0x4b5 ~ +0x4d7` — DLL 열기가 `0xc0000022` (접근 거부) 로 실패하고 `R13D == 0` 일 때만 `+0x4d7` 에서 부름 | ERR |
| 116 | .data | `VsmEnclaveTelemetryInitRunOnce` (`0x1d2578`) | 범위 밖 길에서 씀 `[추측]` | lea `LdrpLogVsmEnclaveLdr…Telemetry` (지도 밖 넷) | 엔클레이브 텔레메트리 — 지도 밖 | TELEM |
| 117 | .data | `LibLoaderTelemetryInitRunOnce` (`0x1d2580`) | 조건부 안 탐 | lea `LdrpLogEtwHotPatchStatus +0x65` (m5, 함수 맨 앞 RunOnce) · `LdrpLogImportRedirectionTelemetry +0x44` · `LdrpLogIntegrityContinuityTelemetry +0x58` | `LdrpInitializeProcess +0x18ef MOV R9,[LdrpNtdllHotPatchContext] · +0x18f6 CMP [R9+0xc],DL · +0x18fa JE → +0x191d` — ntdll 핫패치 상태 바이트가 0 이면 `+0x1916` 호출 건너뜀. 다른 부름 (`LdrpMapAndSnapDependency +0x5ff` 등) 은 핫패치·재지정 길 | HOTPATCH |
| 118 | .data | `CastGuardTelemetryInitRunOnce` (`0x1d2588`) | 범위 밖 길에서 씀 `[추측]` | lea `LdrpCgLogFailure +0xb2` (`+0x0`) · `LdrpLogVerifyAlternateResourceModuleWithServiceChecksumFailure +0x3c` · `LdrpLogMapAndVerifyResourceFileFailure +0x46` (`+0x8`) | 캐스트가드·MUI 검증 실패 로그 — 지도 밖 (뒤 둘은 `LdrLoadAlternateResourceModuleEx` 아래 오류 갈래) | TELEM |
| 119 | .data | `LdrpCODScenarioLock` (`0x1d2598`) | 조건부 안 탐 | lea `LdrpCheckComponentOnDemandEtwEvent +0xaa · +0x160` (m9, 잠금) | 위 `LdrpCODScenarioTriggered` 와 같은 갈래 | ERR |
| 120 | .data | `CastGuardProvidersRegistered` (`0x1d25a0`) | 범위 밖 길에서 씀 `[추측]` | `LdrShutdownProcess +0x387` · `CastGuardTelemetryInitOnce +0x15` | 종료·캐스트가드 텔레메트리 — 지도 밖 | TELEM |
| 121 | .data | `CentennialIssueTracker_InitializedState` (`0x1d25a4`) | 조건부 안 탐 | `LdrpLogRelativePathWithAlteredSearchError +0x49` (`LOCK CMPXCHG`) · `+0x64` (`XCHG`) (m10) | `LdrpGetDllPath +0x1e5 JE · +0x1f0 JE` 뒤 `+0x1f9` — 상대 경로 + altered search 오류 보고 갈래 `[추정: +0x1c7 ~ +0x1f0 조건 안 풂]` | PATH |
| 122 | .data | `VSMEnclaveProvidersRegistered` (`0x1d25a8`) | 범위 밖 길에서 씀 `[추측]` | `LdrShutdownProcess +0x251` · `VsmEnclaveTelemetryInitOnce +0x15` | 종료·엔클레이브 텔레메트리 — 지도 밖 | TELEM |
| 123 | .data | `Feature_PackagedComElevationSupport_v2__private_reporting` (`0x1d25c8`) | 아무도 안 씀 | 없음 | `Feature_PackagedComElevationSupport_v2…IsEnabledNoReportingNoInline` (`LdrpInitializePolicy +0xf4 …`) 이 보고 종류 0 — 보고 안 함 | WIL |
| 124 | .data | `LdrpRedirectionTree` (`0x1d25d0`) | 조건부 안 탐 | lea `LdrpAddRedirectedFunction +0x148 · +0x249` (m7) · `LdrpCheckRedirection +0x1e8` (m7, 읽기 쪽) | 조건 ⓓ | REDIR |
| 125 | .data | `SbpContextLock` (`0x1d25e0`) | 아무도 안 씀 · 잠금 칸 | lea `SbUpdateSwitchContextBasedOnDll +0x1ea · +0x247 · +0x2a4` (m7) · `SbpUpdateCache +0x62 · +0x75` | 111 의 `SbDynamicContextDllCount` 줄 (`+0xfb`) 과 같은 함수가 잡고 풂 — 값 0 으로 돌아옴 | SB |
| 126 | .data | `__@@_PchSym_…cachemin` (`0x1d25e8`) | 아무도 안 씀 | 없음 (코드 참조 0) | 컴파일러 PCH 심볼 이름 `[추정]` — 변수 아님 | PCH |
| 127 | .data | `__@@_PchSym_…xtLoader` (`0x1d25ec`) | 아무도 안 씀 | 없음 (코드 참조 0) | 컴파일러 PCH 심볼 이름 `[추정]` — 변수 아님 | PCH |
| 128 | .data | `LdrpRedirectionByFunctionCalloutFunc` (`0x1d25f8`) | 조건부 안 탐 | lea `LdrpBuildImportRedirection +0x57` (출력 칸) | 조건 ⓓ | REDIR |
| 129 | .data | `LdrpRedirectionModule` (`0x1d2600`) | 조건부 안 탐 | `LdrpInitializeImportRedirection +0x18f` (`MOV [칸],RDI`) | 조건 ⓓ | REDIR |
| 130 | .data | `LdrpRedirectionCalloutFunc` (`0x1d2608`) | 조건부 안 탐 | lea `LdrpBuildImportRedirection +0x3d` (출력 칸) | 조건 ⓓ | REDIR |
| 131 | .data | `LdrpPotentialTlsLeaks` (`0x1d26d0`) | 조건부 안 탐 | `LdrpHandleTlsData +0x5a9` (`INC`, m8) | `+0x46a TEST AL,1 · +0x46c JNE → +0x5a9` — TLS 슬롯 정리 루프의 "이미 쓰이는 슬롯" 갈래 `[추정]`; 사진 0 = 안 탐 | TLS |
| 132 | .data | `RtlFailedCriticalDebugAllocations` (`0x1d272c`) | 0 을 씀 | `LdrpInitializeProcess +0x773` (`MOV [칸],R12D`, R12D=0 — `+0x693 XOR R12D,R12D`) | 무조건 (`+0x76b JGE → +0x773` · 안 뛰어도 `+0x76d` 뒤 같은 줄). `+0x693 ~ +0x773` 로 끼어드는 점프 없음. `RtlpAddDebugInfoToCriticalSection +0x2bd LOCK INC` (디버그 정보 할당 실패 때) 는 조건부 | LdrpInitializeProcess |
| 133 | .data | `LdrpFailureData` (`0x1d27a0`) | 아무도 안 씀 | 없음 (`LdrGetFailureData +0x0` 이 주소만 돌려줌) | 실패 정보 구조체 — init 길에 쓰는 줄 없음 | MISC |
| 134 | .data | `LdrpFatalHardErrorCount` (`0x1d2830`) | 조건부 안 탐 | `LdrpMapDllNtFileName +0x213` · `LdrpReportError +0x26a` (m7) · `LdrpProcessMachineMismatch +0xa7` (m10) (`INC`) | `LdrpMapDllNtFileName +0x199 JNS` (매핑 실패 상태 음수) · `+0x1ae ~ +0x1d0` (`0xc0000017` · `0xc000009a` · `0xc000012d` 등은 건너뜀 → +0x219) 뒤 `+0x201 NtRaiseHardError` → `+0x20a CMP [LdrInitState],3 · +0x211 JE → +0x219` — 하드 에러를 init 중 (`LdrInitState` ≠ 3) 에 낼 때만 `+0x213 INC`. `LdrpReportError` 는 `LdrpProcessWork +0x114 TEST [노드+0x68],0x20 · +0x118 JE` 뒤 `+0x122` (DLL 못 찾음 `0xc0000135` 오류 갈래 `[추정]`), `LdrpProcessMachineMismatch` 는 기계 불일치 이미지에서만 | ERR |
| 135 | .data | `LdrpEnforceIntegrityContinuity` (`0x1d2900`) | sast/111 이 닫음 | 0 을 씀 (입력값) | 111 §1.5 | 111 |
| 136 | .data | `LdrpAuditIntegrityContinuity` (`0x1d2918`) | sast/111 이 닫음 | 0 을 씀 (입력값) | 111 §1.5 | 111 |
| 137 | .data | `g_LdrBreakOnLdrpInitializeProcessFailure` (`0x1d2958`) | 아무도 안 씀 | 없음 (읽기 1곳, 실패 길) | 디버그 스위치 — 쓰는 코드 없음 | MISC |
| 138 | .data | `g_pShimEngineModule` (`0x1d2980`) | sast/111 이 닫음 | 조건부 안 탐 | 111 §1.5 | 111 |
| 139 | .data | `LdrpUseImpersonatedDeviceMap` (`0x1d29a0`) | 조건부 안 탐 | `LdrpInitializeExecutionOptions +0x279` | IFEO 건너뜀 (`+0xa6 · +0xad`) | IFEO |
| 140 | .data | `RtlpUnloadEventTrace` (`0x1d29c0`) | 조건부 안 탐 | lea `LdrpRecordUnloadEvent +0x22 · +0xb6` (m8) | 언로드 기록 — `LdrUnloadDll` 아래 (위 `RtlpUnloadEventTraceEx` 와 같은 조건) | UNLOAD |
| 141 | .data | `LdrpLoaderLockAcquisitionCount` (`0x1d3040`) | 범위 밖 길에서 씀 `[추측]` | `LdrLockLoaderLock +0x82 · +0x10c · +0x172` (`LOCK XADD`) | 공개 API (`LdrLockLoaderLock`) — 지도 밖 | LOADERLOCK |
| 142 | .data | `RtlpSearchPath` (`0x1d3058`) | 조건부 안 탐 | 지도 안: lea `RtlpSignalSystemDirsModification +0x36` (m6, 안에서 `0x18010d1fc` 가 칸을 0 으로 바꿈) · 공개 API `RtlSetSearchPathMode +0x86` | `LdrpInitializePerUserWindowsDirectory +0x16c` (111: `TermsrvGetWindowsDirectoryW` 길이 0 이면 `+0x4c ~ +0x50` 에서 끝 → 안 탐) | PATH |
| 143 | .data | `RtlpDllSearchPathWithOptions` (`0x1d3060`) | 조건부 안 탐 | `LdrSetDllDirectory +0x21d` · `LdrAddDllDirectory +0x1e8` (m6) · 지도 밖 `LdrRemoveDllDirectory +0x8e` | `LdrSetDllDirectory`: `LdrpInitializePolicy +0x4e CMP [DllPath.Length],BX · +0x51 JE → +0x58` (test.exe `DllPath ""`); `LdrAddDllDirectory`: `LdrpInitializePerUserWindowsDirectory +0x4c` 길이 0 (111) | PATH |
| 144 | .data | `RtlpExeSearchPath` (`0x1d3068`) | 조건부 안 탐 | lea `RtlpSignalSystemDirsModification +0x27` (m6, 안에서 `0x18010d1fc` 가 칸을 0 으로 바꿈) | 같음 — 111 의 `LdrpUserDllDirectories` 와 같은 갈래 | PATH |
| 145 | .data | `__@@_PchSym_…pssntlib` (`0x1d32a8`) | 아무도 안 씀 | 없음 (코드 참조 0) | 컴파일러 PCH 심볼 이름 `[추정]` — 변수 아님 | PCH |
| 146 | .data | `__@@_PchSym_…tpcompat` (`0x1d32ac`) | 아무도 안 씀 | 없음 (코드 참조 0) | 컴파일러 PCH 심볼 이름 `[추정]` — 변수 아님 | PCH |
| 147 | .data | `__@@_PchSym_…kOlyq@tp` (`0x1d32b0`) | 아무도 안 씀 | 없음 (코드 참조 0) | 컴파일러 PCH 심볼 이름 `[추정]` — 변수 아님 | PCH |
| 148 | .data | `__@@_PchSym_…etwntdll` (`0x1d33d0`) | 아무도 안 씀 | 없음 (코드 참조 0) | 컴파일러 PCH 심볼 이름 `[추정]` — 변수 아님 | PCH |
| 149 | .data | `EtwpGlobalSequence` (`0x1d33d4`) | 범위 밖 길에서 씀 `[추측]` | lea `EtwpInitLoggerContext +0x1bc` | ETW 로거 — 지도 밖 | ETW |
| 150 | .data | `EtwpLoggerArray` (`0x1d33d8`) | 범위 밖 길에서 씀 `[추측]` | `EtwpGetNextAvailableLoggerId +0x5c` (`LOCK CMPXCHG`) | ETW 로거 — 지도 밖 | ETW |
| 151 | .data | `EtwpMaxLoggers` (`0x1d33e8`) | 범위 밖 길에서 씀 `[추측]` | `EtwpGetMaxLogger +0x57` | ETW 로거 — 지도 밖 | ETW |
| 152 | .data | `RtlpHpEnvEnableSimulatedLargePageCommit` (`0x1d3400`) | 아무도 안 씀 | 없음 (읽기 1곳) | 디버그 스위치 — 쓰는 코드 없음 | HEAPDBG |
| 153 | .data | `UcpTriggeredList` (`0x1d3760`) | 범위 밖 길에서 씀 `[추측]` | `UcpFindOrCreateTelemetryRecord +0x134` · `UcpInitOnUnexpectedCodePathOnceCallBack +0x20 · +0x19` · `UcpUnInitializeListHead +0x31` | 미예상 코드 길 텔레메트리 — 지도 밖 | UCP |
| 154 | .data | `UcpTriggeredNodeCount` (`0x1d3770`) | 범위 밖 길에서 씀 `[추측]` | `UcpCreateTelemetryRecord +0x4c` · `UcpFindOrCreateTelemetryRecord +0xf6` | 위와 같음 | UCP |
| 155 | .data | `NormalizationListLock` (`0x1d3778`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlpGetNormalization +0x1c · +0x37 · +0x10d` (잠금) | 유니코드 정규화 API — 지도 밖 | NORMALIZE |
| 156 | .data | `RtlpGetCurrentProcessorNumberHow` (`0x1d3780`) | 범위 밖 길에서 씀 `[추측]` | `RtlpGetCurrentProcessorNumberInitialize +0x1d` | `RtlGetCurrentProcessorNumber` 첫 부름 — 지도 밖 | MISC |
| 157 | .data | `RtlpHpTagContext` (`0x1d3be0`) | 조건부 안 탐 | lea `RtlSetHeapInformation +0x96` (m5) · `RtlpHpTagReAllocateHeap +0xf6 · +0x10a` (m8). 쓰는 줄 `RtlpHpTagContextAllocateTag +0x16a · +0x171` ← `RtlpHpTagAllocateHeap` ← `RtlAllocateHeap +0x46 · +0xad5` (m5) | 힙 태그 기능이 켜졌을 때만 — `RtlpHpHeapFeatures` 비트 1 (111: `RtlSetHeapInformation +0xb4` 가 켬; test.exe 0). `RtlSetHeapInformation +0x8b TEST [RtlpHpHeapFeatures],AL` · 클래스 4 | HEAPTAG |
| 158 | .data | `RtlpDphInitFailureFlags` (`0x1d3c08`) | 아무도 안 씀 | 없음 (코드 참조 0) | 페이지 힙 초기화 실패 표시 — 쓰는 코드 없음 | HEAPDBG |
| 159 | .data | `RtlpRcuState` (`0x1d3c20`) | 범위 밖 길에서 씀 `[추측]` | `RtlRcuAllocate +0x7c · +0x75 · +0xa9` | RCU 공개 API — 지도 밖 | MISC |
| 160 | .data | `RtlpCtPublishInProgress` (`0x1d3c38`) | 범위 밖 길에서 씀 `[추측]` | `RtlRaiseCustomSystemEventTrigger +0x87 · +0x208` · `RtlpRtlpCtWaitForWnfQuiescentWorker +0x21` (`XCHG`) | 공개 API — 지도 밖 | MISC |
| 161 | .data | `RtlpStackTraceDatabase` (`0x1d3d58`) | 조건부 안 탐 | `RtlpInitializeStackTraceDatabase +0x2d` (`LOCK CMPXCHG`, m6) | `LdrpInitializeProcess +0x8a3 TEST [PEB+0xbc],0x1000 · +0x8ad JNE` / `+0x8af LdrpShouldCreateStackTraceDb · +0x8b7 JE → +0x989` — 사용자 스택 추적 DB (`NtGlobalFlag 0x1000`) 일 때만 `+0x979 RtlControlStackTraceDataBase → +0x25` (sast/13) | HEAPDBG |
| 162 | .data | `RtlpExceptionLogCount` (`0x1d3d64`) | 범위 밖 길에서 씀 `[추측]` | `RtlDispatchException +0xc2 · +0x493 · +0x819 · +0x849` · `RtlpLogExceptionHandler +0x41 · +0x1bc` (`LOCK CMPXCHG`) | 예외 처리 길 — 지도 밖 출발 (`RtlRaiseException +0x188`, m5) | EXC |
| 163 | .data | `__ft_g_api_info` (`0x1d3da0`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlGetFeatureToggleConfiguration +0x64 …` · `__ft_record_impression +0x26` | 기능 토글 — 지도 밖 | FT |
| 164 | .data | `__ft_g_feature_store_change_registration_handle` (`0x1d3dd8`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlpFtInitialize +0x4e` | 위와 같음 | FT |
| 165 | .data | `RtlpValidateHeapTagsEnable` (`0x1d3de0`) | 아무도 안 씀 | 없음 (읽기 1곳) | 디버그 스위치 — 쓰는 코드 없음 | HEAPDBG |
| 166 | .data | `RtlpHeapStopOn` (`0x1d3e00`) | 아무도 안 씀 | 없음 (읽기 18곳) | 디버그 스위치 — 쓰는 코드 없음 | HEAPDBG |
| 167 | .data | `RtlpHeapInvalidBreakPoint` (`0x1d3e30`) | 조건부 안 탐 | `RtlpBreakPointHeap +0xf` (1) · `+0x17` (0) (m8) · `RtlpCoalesceFreeBlocks +0x258 · +0x260` (m10) | `RtlpCreateHeap +0x34e TEST EDI,0x61000000 · +0x354 SETNE CL · +0x35b BT EDI,0x1c · +0x35e TEST AL,CL · +0x360 JNE → +0xe39 → +0xe60 RtlDebugCreateHeap` — 힙 디버그 플래그일 때만 (test.exe `NtGlobalFlag` 0) | HEAPDBG |
| 168 | .data | `RtlpLeakHeap` (`0x1d3e38`) | 범위 밖 길에서 씀 `[추측]` | `RtlDetectHeapLeaks +0x70 · +0xee` | 힙 누수 검출·보안 메모리 API — 지도 밖 | LEAK |
| 169 | .data | `RtlpProcessMemoryMap` (`0x1d3e40`) | 범위 밖 길에서 씀 `[추측]` | `RtlpInitializeLeakDetection +0x1a` | 힙 누수 검출·보안 메모리 API — 지도 밖 | LEAK |
| 170 | .data | `RtlpBusyList` (`0x1d3e50`) | 범위 밖 길에서 씀 `[추측]` | `RtlpInitializeLeakDetection +0x41 · +0x3a` · `RtlpScanHeapAllocBlocks +0x1a7` · `RtlpScanProcessVirtualMemory +0x13b` | 힙 누수 검출·보안 메모리 API — 지도 밖 | LEAK |
| 171 | .data | `RtlpLeakList` (`0x1d3e60`) | 범위 밖 길에서 씀 `[추측]` | `RtlpInitializeLeakDetection +0x56 · +0x4f` · `RtlpPushPageDescriptor +0x116` | 힙 누수 검출·보안 메모리 API — 지도 밖 | LEAK |
| 172 | .data | `RtlSecureMemorySystemRangeStart` (`0x1d3e70`) | 범위 밖 길에서 씀 `[추측]` | lea `RtlRegisterSecureMemoryCacheCallback +0xd` | 힙 누수 검출·보안 메모리 API — 지도 밖 | LEAK |
| 173 | .data | `SmtDelayedConfiguration` (`0x1d3e80`) | 조건부 안 탐 | lea `LdrpInitializeSmtDelayedSleep +0x3e · +0x56 · +0x6e · +0x86 · +0x9e` (m5) → 쓰는 줄 `LdrpInitializeSmtDelayedSleepQueryRegistry +0x75` (`MOV [RBX],ECX`) | `LdrpInitializeProcess +0x1c25` 가 늘 부름. `Session Manager` 키의 `SmtDelay…` DWORD 5개 (`+0x63 CMP [..],4 · +0x6a CMP [..],4`) 가 있을 때만 씀 — 이 VM 은 값 없음 (사진 0). PE_emulator 시험 `Test/LdrInitTest.cpp:9686` | SMT |
| 174 | .data | `RtlpPropStoreEntriesTotalCount` (`0x1d3ea0`) | 범위 밖 길에서 씀 `[추측]` | `RtlCompareExchangePropertyStore +0x150` | 프로퍼티 저장소 공개 API — 지도 밖 | PROPSTORE |
| 175 | .data | `RtlpPropStoreEntries` (`0x1d3ea8`) | 범위 밖 길에서 씀 `[추측]` | `RtlCompareExchangePropertyStore +0x15b` | 프로퍼티 저장소 공개 API — 지도 밖 | PROPSTORE |
| 176 | .data | `RtlpPropStoreEntriesActiveCount` (`0x1d3eb0`) | 범위 밖 길에서 씀 `[추측]` | `RtlCompareExchangePropertyStore +0x16d` · `RtlRemovePropertyStore +0x9e` | 프로퍼티 저장소 공개 API — 지도 밖 | PROPSTORE |
| 177 | .data | `RegistryInfoCritSect` (`0x1d3fe0`) | 조건부 안 탐 | lea `RtlSetProcessPreferredUILanguages +0x5d …` · `LdrpSetThreadPreferredLangList +0x52 · +0x77` · `RtlpInitMuiCriticalSection +0x3d` (임계구역 인자) | `LdrpGetRcConfig +0xbe` · `LdrIsResItemExist +0x97` (m10) → `RtlpInitMuiCriticalSection` / `LdrpSetThreadPreferredLangList` — MUI 자원 구성·언어 대체 목록 갈래 (`LdrResFallbackLangList +0x12f` 도). 사진 0 = 안 탐 | MUI |
| 178 | .data | `RtlpRaiseExceptionOnPossibleDeadlock` (`0x1d4009`) | 0 을 씀 | `LdrpInitializeCriticalSectionExceptionGlobalMitigation +0x44` (`MOV BYTE [칸],0`) · 1 로 켜는 줄 `+0xac` | `LdrpInitializeExecutionOptions +0x9d` 가 늘 부름 (`+0xa6` IFEO 건너뛰기 앞). `+0x1b CMP [LdrpIsSecureProcess],0 · +0x22 JNZ → +0xac` (보안 프로세스면 1) 아니면 `+0x44` 0 → `NtOpenKey` (`Session Manager`) → `NtQueryValueKey("RaiseExceptionOnPossibleDeadlock")` DWORD > 0 이면 `+0xac` 1. test.exe 는 보안 프로세스 아님 (`0x1d28e0` 0) · 값 없음 → 0. IFEO 쪽 `LdrpInitializeExecutionOptions +0x2fe SETNE` 는 건너뜀 | LdrpInitializeExecutionOptions |
| 179 | .data | `RtlpForceCSToUseEvents` (`0x1d41a0`) | 조건부 안 탐 | `LdrpInitializeProcess +0x9bb` (`MOV [칸],BL`, BL=1) | `+0x9b3 JAE → +0x9c1` — `PEB+0x2c8 AppCompatFlags` 비트 22 (`+0x9ae BT RCX,0x16`) 가 꺼지면 `+0x9b5 · +0x9bb` 둘 다 건너뜀 (test.exe `AppCompatFlags` 0 `[관찰]`). 0 을 쓰는 줄은 없음 (111 의 "0 을 씀 후보" 정정) | CS |
| 180 | .data | `RtlpModernAppKey` (`0x1d41a8`) | 조건부 안 탐 | `RtlOpenModernAppOptionsKey +0x77` (`LOCK CMPXCHG`, m6) | `LdrpInitializeExecutionOptions +0x10a` — IFEO 건너뜀 + `[PEB+3]` 비트 4 (패키지 앱) 아님 | IFEO |
| 181 | .data | `LoadAsDataTableBlockCount` (`0x1d41b0`) | 조건부 안 탐 | `LdrAddLoadAsDataTable +0x179` (0x20) · `+0x1c8` (+0x20) · `LdrRemoveLoadAsDataTable +0x27d` | 조건 ⓖ | LOADASDATA |
| 182 | .data | `LoadAsDataTable` (`0x1d41e8`) | 조건부 안 탐 | `LdrAddLoadAsDataTable +0x172` · `+0x1c1` · `LdrRemoveLoadAsDataTable +0x276` | 조건 ⓖ | LOADASDATA |
| 183 | .data | `LoadAsDataTableCount` (`0x1d41f0`) | 조건부 안 탐 | `LdrAddLoadAsDataTable +0x142` (`INC`) · `LdrRemoveLoadAsDataTable +0x119` | 조건 ⓖ | LOADASDATA |
| 184 | .data | `AlternateResourceModules` (`0x1d41f8`) | 조건부 안 탐 | `LdrpSetAlternateResourceModuleHandle +0x310 · +0x35e` · `LdrUnloadAlternateResourceModuleEx +0x12d · +0x176` | 조건 ⓗ | MUI |
| 185 | .data | `AlternateResourceModuleCount` (`0x1d4200`) | 조건부 안 탐 | `LdrpSetAlternateResourceModuleHandle +0x2ac` (`INC`) · `LdrUnloadAlternateResourceModuleEx +0xee` | 조건 ⓗ | MUI |
| 186 | .data | `AltResMemBlockCount` (`0x1d4204`) | 조건부 안 탐 | `LdrpSetAlternateResourceModuleHandle +0x317` (0x20) · `+0x365` · `LdrUnloadAlternateResourceModuleEx +0x138 · +0x17d` | 조건 ⓗ | MUI |
| 187 | .data | `gMUICacheType` (`0x1d4208`) | 범위 밖 길에서 씀 `[추측]` | `LdrSetMUICacheType +0x35` (읽기 8곳) | 공개 API — 지도 밖 | MUI |
| 188 | .data | `TrustedAppsRemoteBreakinRoutine` (`0x1d4268`) | 범위 밖 길에서 씀 `[추측]` | `DbgUiIssueRemoteBreakin +0x12b` | 디버거 원격 중단 — 지도 밖 | DBG |
| 189 | .data | `AVrfpVerifierDllsString` (`0x1d4280`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x30f` · lea `AVrfpParseVerifierDllsString +0x9a` | 조건 ⓐ | AVRF |
| 190 | .data | `AVrfpPageHeapPreviousStopData` (`0x1d4480`) | 범위 밖 길에서 씀 `[추측]` | `RtlApplicationVerifierStop +0x10e · +0x144` | AppVerifier 정지 보고 — 지도 밖 | AVRF |
| 191 | .data | `AVrfpVerifierLock` (`0x1d44c0`) | 조건부 안 탐 | lea `AVrfDllLoadNotification +0x44 · +0x5f` (m6) · `AVrfInitializeVerifier +0x66` (m5) · `AVrfDllUnloadNotification +0x34 · +0xa0` (m7) | `LdrpSendPostSnapNotifications +0x40 MOV EBP,[PEB+0xbc] · +0x88 AND EBP,0x100 · +0x95 TEST EBP,EBP · +0x97 JNE → +0x12d` (`FLG_APPLICATION_VERIFIER`) 일 때만 `+0x130 AVrfDllLoadNotification`. test.exe `NtGlobalFlag` 0 | AVRF |
| 192 | .data | `AVrfpEnabledSystemWide` (`0x1d44e8`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x353` | 조건 ⓐ | AVRF |
| 193 | .data | `AVrfpVerifierProvidersList` (`0x1d44f0`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x6d` (`MOV [칸],RSI` — 목록 머리 자기 가리킴) · lea 열 곳 | 조건 ⓐ | AVRF |
| 194 | .data | `AVrfpVerifierFlags` (`0x1d4500`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x303 · +0x340` | 조건 ⓐ | AVRF |
| 195 | .data | `AvrfpLoaderEntry` (`0x1d4520`) | 조건부 안 탐 | lea `AVrfInitializeVerifier +0x3d9 · +0x28e` | 조건 ⓐ | AVRF |
| 196 | .data | `AVrfpCurrentUserKeyPath` (`0x1d48c0`) | 조건부 안 탐 | lea `AVrfpFormatCurrentUserKeyPath +0x9` (m7) | `AVrfOpenCurrentUserImageFileOptionsKey +0x32` ← `LdrpInitializeExecutionOptions +0x649`; 그 앞 `+0x635 TEST [KUSD+0x3a0],1 · +0x63d JE → +0x696` — KUSD `ImageFileExecutionOptions` 비트 0 이 꺼져 있으면 (K3 사진 0) 건너뜀 | AVRF |
| 197 | .data | `AVrfpVerifierStopMessageFunction` (`0x1d4da8`) | 조건부 안 탐 | `AVrfpVerifierStopInitialize +0x93` (m6) | 조건 ⓐ | AVRF |
| 198 | .data | `AVrfpPageHeapStopData` (`0x1d4dc0`) | 범위 밖 길에서 씀 `[추측]` | `RtlApplicationVerifierStop +0x122 ~ +0x15a` | AppVerifier 정지 보고 — 지도 밖 | AVRF |
| 199 | .data | `HotPatchSpareGlobals` (`0x1d4e40`) | 아무도 안 씀 | 없음 (코드 참조 0) | 핫패치 예비 칸 — 쓰는 코드 없음 | HOTPATCH |
| 200 | .data | `Feature_UxAccOptimization__private_featureState` (`0x1d4ec0`) | 조건부 안 탐 | `…Reevaluate +0xa0` — 의존 기능으로 평가됨 | `PackagedComElevationSupport_v2` (`LdrpInitializePolicy +0xf4 …`, 패키지 앱 갈래만) 와 `GetPreferredThreadLanguage…BugFix` (지도 밖) 의 필요 기능. 기술자 `+0x25 · +0x26` 이 0 이라 평가 대상 | WIL-state |
| 201 | .data | `Feature_Standalone_26_06_NonSec__private_featureState` (`0x1d4ec8`) | 아무도 안 씀 | 없음 | `TestLoc03` 의 필요 기능이지만 기술자 `+0x26`=1 (상수 기능) — `wil_details_AreDependenciesEnabled +0x20 CMP [RAX+0x1e],0 · +0x24 JNZ → +0x52` 가 평가 없이 `[RAX+0x1f]` 만 봄. 직접 묻는 함수 없음 | WIL-state |
| 202 | .data | `Feature_TestLoc03__private_featureState` (`0x1d4ed0`) | 조건부 안 탐 | `…Reevaluate +0xa0` — 의존 기능으로 평가됨 | `DriverSetup_ReofferFingerprinting_v2` 의 필요 기능 — ApiSet 기능 갈래 (위 `Feature_*__private_reporting` 의 `ApiSetpResolveHost +0x204`) 일 때만 | WIL-state |
| 203 | .data | `Feature_DriverSetup_ReofferFingerprinting_v2__private_featureState` (`0x1d4ed8`) | 조건부 안 탐 | `…Reevaluate +0xa0` (`wil_details_IsEnabledFallback +0x32`) | `Feature_DriverSetup…IsEnabledDeviceUsageNoInline +0x37` ← `IsFeatureEnabled +0x2e` (기능 번호 `0x3a132a5`) — ApiSet 기능 갈래 | WIL-state |
| 204 | .data | `Feature_ProjectTessera_CreateProcessInSandbox__private_featureState` (`0x1d4ee0`) | 조건부 안 탐 | `…Reevaluate +0xa0` | `…IsEnabledDeviceUsageNoInline +0x37` ← `IsFeatureEnabled +0x27` (기능 번호 `0x3a52a68`) — ApiSet 기능 갈래 | WIL-state |
| 205 | .data | `Feature_LearningModeTraceService__private_featureState` (`0x1d4ee8`) | 조건부 안 탐 | `…Reevaluate +0xa0` | `…IsEnabledDeviceUsageNoInline +0x37` ← `IsFeatureEnabled +0x20` (기능 번호 `0x3b76c1d`) — ApiSet 기능 갈래 | WIL-state |
| 206 | .data | `Feature_Standalone_Future__private_featureState` (`0x1d4ef0`) | 조건부 안 탐 | `…Reevaluate +0xa0` — 의존 기능으로 평가됨 | `UxAccOptimization` 의 필요 기능 (기술자 `+0x26` 0) — 위 `UxAccOptimization` 과 같은 조건 | WIL-state |
| 207 | .data | `Feature_TestLoc02__private_featureState` (`0x1d4ef8`) | 아무도 안 씀 | 없음 | `TestLoc03` 의 필요 기능이지만 기술자 `+0x26`=1 (상수 기능) — 평가 안 함. 직접 묻는 함수 없음 | WIL-state |
| 208 | .data | `Feature_Standalone_26_08_NonSec__private_featureState` (`0x1d4f00`) | 아무도 안 씀 | 없음 | `Tessera_202605Retail` 의 필요 기능이지만 기술자 `+0x26`=1 — 평가 안 함. 직접 묻는 함수 없음 | WIL-state |
| 209 | .data | `Feature_SettingsDel__private_featureState` (`0x1d4f08`) | 아무도 안 씀 | 없음 | `FasterCrc32` 의 필요 기능이지만 기술자 `+0x26`=1 — 평가 안 함. 직접 묻는 함수 없음 | WIL-state |
| 210 | .data | `Feature_Tessera_202605Retail__private_featureState` (`0x1d4f10`) | 조건부 안 탐 | `…Reevaluate +0xa0` — 의존 기능으로 평가됨 | `ProjectTessera` · `LearningModeTraceService` 의 필요 기능 — ApiSet 기능 갈래 | WIL-state |
| 211 | .data | `Feature_ImplVal__private_featureState` (`0x1d4f18`) | 아무도 안 씀 | 없음 | `KVS_BugFixes` 의 필요 기능이지만 기술자 `+0x26`=1 — 평가 안 함 (sast/111: KVS 만 써짐, 사진 ImplVal 0 과 맞음) | WIL-state |
| 212 | .data | `Feature_GetPreferredThreadLanguage_Watson_AccessViolation_BugFix__private_featureState` (`0x1d4f30`) | 범위 밖 길에서 씀 `[추측]` | `…Reevaluate +0xa0` (`Feature_GetPreferredThreadLanguage…IsEnabledFallback`) | `…IsEnabledDeviceUsageNoInline` ← `RtlpMuiRegTryToAppendLangId +0x15d` · `RtlpLangNameInMultiSzString_Size +0x4b` (지도 밖, MUI 언어 목록) | WIL-out |
| 213 | .data | `Feature_FasterCrc32__private_featureState` (`0x1d4f38`) | 범위 밖 길에서 씀 `[추측]` | `…Reevaluate +0xa0` (`Feature_FasterCrc32…IsEnabledFallback`) | `…IsEnabledDeviceUsageNoInline` ← `RtlCrc32 +0x18` (지도 밖) | WIL-out |
| 214 | .data | `Feature_TreatDosDevicePathsAsNormalFiles__private_featureState` (`0x1d4f48`) | 조건부 안 탐 | `…Reevaluate +0xa0` (`RtlpAreLegacyDosDeviceNamesEnabled +0x11e · +0x136` 이 기술자를 넘김) | `RtlpIsDosDeviceName_Ustr +0x542` ← `RtlGetFullPathName_Ustr +0x12d` (m8→m9) — 옛 DOS 장치 이름 비교 갈래 `[추정]` | WIL-DOS |
| 215 | .data | `Feature_PackagedComElevationSupport_v2__private_featureState` (`0x1d4f50`) | 조건부 안 탐 | `…Reevaluate +0xa0` (`LdrpInitializePolicy +0xf4 · +0x17b · +0x1ea · +0x259 · +0x2d1`) | `LdrpInitializePolicy +0x42 MOV AL,[PEB+3] · +0x46 TEST AL,0x10 · +0x48 JNZ → +0x72` — 패키지 앱 (`IsPackagedProcess`) 일 때만. test.exe `BitField 0x4` `[관찰]` | PACKAGED |
| 216 | .mrdata | `AvrfpAPILookupCallbacksEnabled` (`0x1e7040`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x1da` (`MOV [칸],DIL`) | 조건 ⓐ | AVRF |
| 217 | .mrdata | `AVrfpEnabled` (`0x1e7041`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x1e3` | 조건 ⓐ | AVRF |
| 218 | .mrdata | `RtlGuardAllowSuppressedCalls` (`0x1e7042`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x1ea` · `LdrpInitializeExecutionOptions +0x37d` (`MOV BYTE [칸],1`) | AVRF 묶음 (`+0x1568`) · IFEO 건너뜀 (`+0xa6 · +0xad`) | AVRF |
| 219 | .mrdata | `AvrfpAPILookupCallbackRoutine` (`0x1e7048`) | 조건부 안 탐 | `AVrfInitializeVerifier +0x1d3` (`MOV [칸],RDX`) | 조건 ⓐ | AVRF |
| 220 | .mrdata | `RtlpProtectedPolicies` (`0x1e7080`) | 범위 밖 길에서 씀 `[추측]` | `RtlSetProtectedPolicy +0x143` | 조건 ⓘ | PROTPOLICY |
| 221 | .mrdata | `RtlpProtectedPoliciesActiveCount` (`0x1e7088`) | 범위 밖 길에서 씀 `[추측]` | `RtlSetProtectedPolicy +0x165` | 조건 ⓘ | PROTPOLICY |
| 222 | .mrdata | `RtlpProtectedPoliciesTotalCount` (`0x1e708c`) | 범위 밖 길에서 씀 `[추측]` | `RtlSetProtectedPolicy +0x14a` | 조건 ⓘ | PROTPOLICY |
| 223 | .mrdata | `g_pfnSE_GetProcAddressForCaller` (`0x1e7090`) | 조건부 안 탐 | `LdrpGetShimEngineInterface +0x529` | 조건 ⓑ | SHIM |
| 224 | .mrdata | `g_pfnApphelpQueryModSettingsAllocProc` (`0x1e7098`) | 조건부 안 탐 | `LdrpGetShimEngineInterface +0x559` | 조건 ⓑ | SHIM |
| 225 | .mrdata | `g_pfnSE_DllUnloaded` (`0x1e70a0`) | 조건부 안 탐 | `LdrpGetShimEngineInterface +0x4f9` | 조건 ⓑ | SHIM |
| 226 | .mrdata | `g_pfnSE_ProcessDying` (`0x1e70a8`) | 조건부 안 탐 | `LdrpGetShimEngineInterface +0x519` | 조건 ⓑ | SHIM |
| 227 | .mrdata | `g_pfnSE_ShimDllLoaded` (`0x1e70b0`) | 조건부 안 탐 | `LdrpGetShimEngineInterface +0x4b9` | 조건 ⓑ | SHIM |
| 228 | .mrdata | `Wow64ApcRoutine` (`0x1e70d0`) | 조건부 안 탐 | `LdrpLoadWow64 +0xe8 ~ +0xf1` (`.rdata` `0x173830` 표의 6항목 {이름, 칸 주소} 를 `LdrGetProcedureAddress` 로 채움 — 칸 주소가 표에 있어 직접 쓰는 줄은 안 잡힘) | 조건 ⓒ | WOW64 |
| 229 | .mrdata | `g_pfnSE_InitializeEngine` (`0x1e70d8`) | 조건부 안 탐 | `LdrpGetShimEngineInterface +0x4a9` | 조건 ⓑ | SHIM |
| 230 | .mrdata | `Wow64PrepareForException` (`0x1e70e0`) | 조건부 안 탐 | `LdrpLoadWow64 +0xe8 ~ +0xf1` (`.rdata` `0x173830` 표의 6항목 {이름, 칸 주소} 를 `LdrGetProcedureAddress` 로 채움 — 칸 주소가 표에 있어 직접 쓰는 줄은 안 잡힘) | 조건 ⓒ | WOW64 |
| 231 | .mrdata | `g_pfnSE_LdrResolveDllName` (`0x1e70e8`) | 조건부 안 탐 | `LdrpGetShimEngineInterface +0x539` | 조건 ⓑ | SHIM |
| 232 | .mrdata | `g_pfnSE_InstallBeforeInit` (`0x1e70f0`) | 조건부 안 탐 | `LdrpGetShimEngineInterface +0x4c9` | 조건 ⓑ | SHIM |
| 233 | .mrdata | `g_LdrpWow64PrepareForDebuggerAttach` (`0x1e70f8`) | 조건부 안 탐 | `LdrpLoadWow64 +0xe8 ~ +0xf1` (`.rdata` `0x173830` 표의 6항목 {이름, 칸 주소} 를 `LdrGetProcedureAddress` 로 채움 — 칸 주소가 표에 있어 직접 쓰는 줄은 안 잡힘) | 조건 ⓒ | WOW64 |
| 234 | .mrdata | `g_pfnSE_LdrEntryRemoved` (`0x1e7100`) | 조건부 안 탐 | `LdrpGetShimEngineInterface +0x509` | 조건 ⓑ | SHIM |
| 235 | .mrdata | `g_LdrpWow64SuspendLocalProcess` (`0x1e7110`) | 조건부 안 탐 | `LdrpLoadWow64 +0xe8 ~ +0xf1` (`.rdata` `0x173830` 표의 6항목 {이름, 칸 주소} 를 `LdrGetProcedureAddress` 로 채움 — 칸 주소가 표에 있어 직접 쓰는 줄은 안 잡힘) | 조건 ⓒ | WOW64 |
| 236 | .mrdata | `g_LdrpWow64SuspendLocalThread` (`0x1e7118`) | 조건부 안 탐 | `LdrpLoadWow64 +0xe8 ~ +0xf1` (`.rdata` `0x173830` 표의 6항목 {이름, 칸 주소} 를 `LdrGetProcedureAddress` 로 채움 — 칸 주소가 표에 있어 직접 쓰는 줄은 안 잡힘) | 조건 ⓒ | WOW64 |
| 237 | .mrdata | `LdrpUnalignedUserCallTargetBitMap` (`0x1e7348`) | 아무도 안 씀 | 없음 (코드 참조 0) | CFG 비정렬 대상 비트맵 — 쓰는 코드 없음 (입력값; 커널 쪽 CFG 길 `[추측]`) | CFG |
| 238 | .00cfg | `__guard_xfg_check_icall_fptr` (`0x1e6008`) | 조건부 안 탐 | `LdrpCfgCheckRoutineCallback +0x25 · +0x49` (`MOV [R8],RCX`) — 로드 구성 `+0x118` 이 가리키는 칸 | 조건 ⓙ | CFG |
| 239 | .00cfg | `__guard_xfg_dispatch_icall_fptr` (`0x1e6010`) | 조건부 안 탐 | `LdrpCfgDispatchRoutineCallback +0x25 · +0x49` — 로드 구성 `+0x120` | 조건 ⓙ | CFG |
| 240 | .00cfg | `__guard_xfg_table_dispatch_icall_fptr` (`0x1e6018`) | 조건부 안 탐 | `LdrpCfgDispatchRoutineCallback +0x25 · +0x49` — 로드 구성 `+0x128` | 조건 ⓙ | CFG |
| 241 | .00cfg | `__castguard_check_failure_os_handled_fptr` (`0x1e6020`) | 조건부 안 탐 | `LdrpCgRoutineCallback` (로드 구성 `+0x130`, 칸에 `0x18015ed50` 또는 0) | `LdrpCfgProcessLoadConfig +0x17c BT R15D,0x18 · +0x181 JC → +0x6a9` — `GuardFlags` 비트 24 (CastGuard) 꺼져 (ntdll `0x10417500`) 칸 수집조차 안 함; 켜져도 위 CFG 강제 게이트 | CFG |

#### 1.1 "0 을 씀" 셋 풀이

- **`RuntimeGlobal`** (`0x1cddb8`, 4B): `ExecuteHotpatchTestRuntimeFunction` (rva `0x134628`, 34B) 전체 — `+0x0 LEA RAX,[칸]` · `+0x7 CMP RAX,0x20` · `+0xb JNZ → +0x17` · (`+0xd MOV [칸],0x14` — 죽은 줄) · **`+0x17 MOV [칸],0`** · `+0x21 RET`. 부르는 곳 `LdrGetProcedureAddressForCaller +0x53 (ret +0x58)` 이 맨 앞에서 늘 부르고 곧바로 `+0x58 GetHotpatchTestRuntimeFunctionState` (칸을 읽어 `EAX` — `+0x5d TEST EAX,EAX · +0x5f JNZ`). 칸 값이 0 이면 그 JNZ 가 안 뛴다. 불리는 길: `LdrpInitializeProcess +0x199d (ret +0x19a2) LdrpInitializeKernel32Functions → +0xfa (ret +0xff) · +0x12b LdrGetProcedureAddressForCaller` (m6). 사진 `끝까지 0`. `[정적]`
- **`RtlFailedCriticalDebugAllocations`** (`0x1d272c`, 4B): `LdrpInitializeProcess +0x773 MOV [칸],R12D`, `R12D` = 0 (`+0x693 XOR R12D,R12D`; `+0x693` 뒤 `+0x773` 앞으로 끼어드는 점프 없음 — 점프 목적지 전부 셈). 무조건. 뒤 `+0x77a` 부터 `RtlCriticalSectionDebugSList` · `RtlpStaticDebugInfo` (111 §1a). 값을 키우는 줄 (`RtlpAddDebugInfoToCriticalSection +0x2bd LOCK INC`) 은 디버그 정보 할당이 실패할 때만. `[정적]`
- **`RtlpRaiseExceptionOnPossibleDeadlock`** (`0x1d4009`, 1B): `LdrpInitializeCriticalSectionExceptionGlobalMitigation` (rva `0xfaa6c`, 203B, **이번에 `.asm` 뽑음**). `+0x1b CMP [LdrpIsSecureProcess],0 · +0x22 JNZ → +0xac` (보안 프로세스면 곧장 `+0xac MOV [칸],1`). 아니면 `+0x44 MOV BYTE [칸],0` → `+0x4b NtOpenKey("\\Registry\\MACHINE\\System\\CurrentControlSet\\Control\\Session Manager", 접근 1)` → 실패하면 끝 (`+0x52 JS`) → `+0x7d NtQueryValueKey(키, "RaiseExceptionOnPossibleDeadlock", 클래스 2, 0x50B)` → `+0x89 NtClose` → 성공이고 종류 4 (`+0xb5 CMP [RSP+0x44],4`) · 길이 4 (`+0xbc`) · 값 > 0 (`+0xc3 CMP [RSP+0x4c],0 · +0xc8 JBE`) 이면 `+0xca JMP +0xac` 로 1. 부르는 곳 `LdrpInitializeExecutionOptions +0x9d (ret +0xa2)` 가 IFEO 건너뛰기 (`+0xa6 · +0xad`) 앞에서 **늘** 부른다. test.exe: `LdrpIsSecureProcess` 0 (`0x1d28e0` P1a·P1b·P2 `[관찰]`), 값 없음 (사진 0). IFEO 쪽 같은 칸 줄 (`LdrpInitializeExecutionOptions +0x2fe SETNE`) 은 IFEO 건너뜀으로 안 탐. `[정적]`
- **이 셋 말고 "0 을 씀" 후보로 보았다가 뺀 것**: 위 "111 정정 4개". 그리고 `RtlpHpStackTraceDisable +0x19 AND [..],0` · `RtlpBreakPointHeap +0x17` · `LdrUnloadAlternateResourceModuleEx +0x12d` 등 0 을 쓰는 다른 줄은 모두 조건부 안 탐 갈래 안 (표).

#### 1.2 WIL 기능 상태 칸을 가른 규칙 (`Feature_*__private_featureState` 16 · `__private_reporting` 18)

- 기술자 (`.rdata` `0x1b7cd0 ~ 0x1b80f0`, 항목 `0x38`B 19개): `+0x8` 상태 칸 주소 · `+0x10` 보고 칸 주소 · `+0x20` 8B = {기능 번호 4B, `+0x24` ?, **`+0x25` · `+0x26` · `+0x27`** } · `+0x28` 필요 기능 목록 포인터 (원소 = 필요 기능 기술자의 `+0x8` 주소).
- **`wil_details_AreDependenciesEnabled` (`+0x20 CMP [RAX+0x1e],0 · +0x24 JNZ → +0x52` · `+0x26 CMP [RAX+0x1d],0 · +0x2a JNZ`)**: 필요 기능의 `+0x25` 또는 `+0x26` 이 0 이 아니면 **평가 없이 `+0x27` 만 읽는다** (상수 기능). 둘 다 0 이면 `+0x32 → …GetCachedFeatureEnabledState → …Reevaluate +0xa0` 로 **상태 칸을 쓴다** (그리고 그 기능의 필요 기능도 같은 방식으로 재귀).
- 기술자 바이트 (`+0x25` · `+0x26`) 를 읽은 결과: **평가되는 기능** (둘 다 0) = `Tessera_202605Retail` · `ProjectTessera…` · `TestLoc03` · `Standalone_Future` · `LearningModeTraceService` · `UxAccOptimization` · `DriverSetup…` · `DisallowSharedLock…` · `KVS_BugFixes…` · `GetPreferredThreadLanguage…` · `FasterCrc32` · `LoaderSnapsBuffer` · `PackagedComElevationSupport_v2`; **상수 기능** (`+0x26 = 1`) = `SettingsDel` · `Standalone_26_08_NonSec` · `Standalone_26_06_NonSec` · `ImplVal` · `TestLoc02` · `TreatDosDevicePathsAsNormalFiles` (마지막은 `RtlpAreLegacyDosDeviceNamesEnabled` 가 기술자를 직접 넘겨 평가하므로 상수 취급은 "의존으로 불릴 때" 만). 사진 `ImplVal` 0 · `KVS` `0x40147` (111) 이 이 규칙과 맞는다.
- **필요 기능 사슬** (기술자 `+0x28` 목록 읽음): `DriverSetup → TestLoc03 → {TestLoc02 (상수), Standalone_26_06 (상수)}` · `ProjectTessera → Tessera_202605Retail → Standalone_26_08 (상수)` · `LearningModeTraceService → ProjectTessera → …` · `PackagedComElevation → UxAccOptimization → Standalone_Future` · `GetPreferredThreadLanguage → UxAccOptimization` · `FasterCrc32 → SettingsDel (상수)` · `DisallowSharedLock → KVS → ImplVal (상수)`.
- 직접 묻는 `Feature_…_IsEnabled…` 함수 (ntdll 안 전부): `DisallowSharedLockImplicitUpgrade` · `LoaderSnapsBuffer` (3.8 이미 — `LdrpInitializeProcess +0xaad · +0xac0`) · `PackagedComElevationSupport_v2` (`LdrpInitializePolicy +0xf4 · +0x17b · +0x1ea · +0x259 · +0x2d1`, 패키지 앱 갈래) · `DriverSetup_ReofferFingerprinting_v2` · `ProjectTessera…` · `LearningModeTraceService` (`IsFeatureEnabled +0x2e · +0x27 · +0x20`, 기능 번호 비교 `+0x4 CMP ECX,0x3a132a5` · `+0xc 0x3a52a68` · `+0x14 0x3b76c1d`, 부르는 곳 `ApiSetpIsFeatureEnabled +0x4` ← `ApiSetpResolveHost +0x204` ← `LdrpSnapKernelBaseExtensions +0x1c5`) · `TreatDosDevicePathsAsNormalFiles` (`RtlpAreLegacyDosDeviceNamesEnabled +0x11e · +0x136`) · `GetPreferredThreadLanguage…BugFix` (MUI 언어 목록, 지도 밖) · `FasterCrc32` (`RtlCrc32 +0x18`, 지도 밖). **이 밖 기능은 ntdll 에서 아무도 직접 안 묻는다.**
- 보고 칸 (`__private_reporting`): `wil_details_IsEnabledFallback +0x43 TEST ESI,ESI · +0x45 JNZ` 로 **보고 종류가 0 이 아닐 때만** `+0x72 ReportUsageToService` 가 쓴다. `…IsEnabledNoReportingNoInline` (보고 0) 은 안 씀 — `DisallowSharedLock` · `PackagedComElevation` 보고 칸이 "안 씀" 인 이유. 보고 종류가 있는 `…IsEnabledDeviceUsageNoInline` 세 개는 위 ApiSet 갈래.
- ApiSet 갈래 조건 (`ApiSetpResolveHost`, rva `0x11b40c`): `+0x1ed TEST BYTE [항목+6],4 · +0x1f1 JE → +0x211` (항목에 기능 표시 없으면 건너뜀) · `+0x1f3 TEST [항목+6],2 · +0x1f7 JNE → +0x3c7` · `+0x1fd MOV ECX,[항목+8] · +0x202 JE → +0x211` (기능 번호 0 이면 건너뜀) · `+0x204 ApiSetpIsFeatureEnabled` (안에서 `+0x4 IsFeatureEnabled` 를 불러 반환을 `+0x9 CMP EAX,2` 와 비교; `IsFeatureEnabled` 는 모르는 번호면 0, 아는 번호면 1 (꺼짐) 또는 2 (켜짐)) · `+0x20b JE → +0x3c7` (꺼져 있으면 이 항목 건너뜀). 사진 세 기능 칸 0 = 이 갈래에서 기능 번호 비교가 맞은 적 없음. `[정적 + 사진]`

#### 1.3 SxS · shim · WOW64 · 경로 등 큰 묶음 한 줄씩 (표 `쓰는 줄`·`조건` 에 있음)

- **SxS**: `RtlReleaseActivationContext +0x1d ~ +0x3d` 가 참조 개수 −1 (`LOCK CMPXCHG`) → 0 이 되면 `+0x82 JE → +0x92`: `+0x92 CMP [g_SxsKeepActivationContextsAlive],0 · +0x9c JNE → +0xa5` (켜졌으면 `+0xaa JMP RtlpMoveActCtxToFreeList`) 아니면 `+0x9e` 로 그냥 해제. 보존 스위치를 쓰는 코드가 ntdll 에 없어서 (읽기 2곳) 실제로는 디버거 · 설정으로만 켜지는 칸 `[추정]`.
- **WOW64**: `LdrpLoadWow64 +0xcd LEA R15,[…]` = `.rdata` `0x173830` 표, 항목 `0x10`B 6개 `{이름 ANSI_STRING 포인터, 칸 주소}` — `Wow64LdrpInitialize` → `0x1e70c8` · `Wow64PrepareForException` → `0x1e70e0` · `Wow64ApcRoutine` → `0x1e70d0` · `Wow64PrepareForDebuggerAttach` → `0x1e70f8` · `Wow64SuspendLocalThread` → `0x1e7118` · `Wow64SuspendLocalProcess` → `0x1e7110` (wow64.dll 내보내기). `+0xd7 CMP EDI,6` 루프가 `+0xf1 CALL LdrGetProcedureAddress` (`R9` = 칸 주소) 로 채운다. 칸 6개 모두 코드에 칸 주소가 안 보이고 표에만 있다 (ptr 스캔 `0x173838 · 0x173848 · 0x173858 · 0x173868 · 0x173878 · 0x173888`). 3.9 의 `g_LdrpWow64LdrpInitialize` 줄 `[추측]` 이 이로써 `[정적]` (표 구동).
- **경로 락 셋**: `RtlpCachedPathLock` 은 `LdrpReleaseDllPath +0x2f LEA RDI,[칸] … +0x55 LOCK BTS [RDI],0` (SRW 잡기, m5, `LdrpMapDllSearchPath +0x2d2` 등이 부름), `LdrpDllDirectoryLock` 은 `RtlpComputeDllPath +0x52 ..` (깊이 11, `RtlpGetCachedPath +0xbe · +0xe9` 가 부름) — 늦은 DLL 경로 계산이 잡고 풂.

- **지금 우리 (PE_emulator) — 질문 1**: **대부분 맞다, 한 곳 틀림.** `LdrpInitializeProcess` 몸통은 진짜 코드를 Unicorn 에서 돌려 직접 쓰는 줄이 같다 (`RtlFailedCriticalDebugAllocations` 기대 0 = `Test/LdrInitTest.cpp:1646`). 안 타는 조건의 **입력도 진짜 값과 같게 채워져 있다**: `NtGlobalFlag` 0 (`Src/nt/Process.cpp:236`) · `InheritedAddressSpace` 는 관찰 템플릿 (`:304`) · `UseWOW64` 0 (`LdrInitTest.cpp:1642`) · `g_ShimsEnabled` 0 (`:9975`) · `SmtDelayedConfiguration` 0 (`:9686`) · 시스템 DLL 블록 `+0xb8` 0 (`LdrInit.cpp:1460` 주석). **틀린 곳**: 덫 `LdrpInitializeCriticalSectionExceptionGlobalMitigation` 이 빈 함수 (`Src/ntdll/LdrInit.cpp:1445`) — 진짜는 `RtlpRaiseExceptionOnPossibleDeadlock` 에 0 을 쓰고 `NtOpenKey` · `NtQueryValueKey` · `NtClose` 를 부른다 (칸이 0 으로 시작하면 값은 같고, `0xcc` 무늬로 채워 시험하면 다르다). **확인 필요**: PEB `AppCompatFlags` (`+0x2c8`) · `PEB.pShimData` · KUSD `+0x3a0` · `RedirectionDllName` 을 우리가 모두 0·빈 값으로 두는지 (진짜 사진은 0·`""`), 활성화 문맥 (`activeCodePage` 없음) 응답.

### 2. `.00cfg` 4칸

**답: 넷 모두 "조건부 안 탐" — CFG 강제가 아니라 콜백이 안 불려 파일 처음 값이 P2 까지 그대로라고 본다 (사진은 8장 모두 읽기 불가라 `[정적 + 추정]`).**

| 칸 (rva) | 로드 구성 필드 | 파일 값 → 재배치 뒤 | `.reloc` | 쓰는 줄 (콜백) | 값 (P2 까지, 추정) |
|---|---|---|---|---|---|
| `__guard_xfg_check_icall_fptr` (`0x1e6008`) | `+0x118` | `0x1800fac60` → `ntdll+0xfac60` (`_guard_check_icall_nop`, `C2 00 00` = `ret 0`) | 있음 | `LdrpCfgCheckRoutineCallback +0x25 · +0x49` (`MOV [R8],RCX`; CFG 강제 + `LdrSystemDllInitBlock +0xf0` / `+0xf8` 또는 `LdrpValidateUserCallTarget` (`0x1244e0`) / `…ES` (`0x124530`)) | 파일 값 그대로 |
| `__guard_xfg_dispatch_icall_fptr` (`0x1e6010`) | `+0x120` | `0x1801664b0` → `ntdll+0x1664b0` (`_guard_xfg_dispatch_icall_nop`, `JMP [0x1e6000]`) | 있음 | `LdrpCfgDispatchRoutineCallback +0x25 · +0x49` (`LdrSystemDllInitBlock +0x100` / `+0x108` 또는 `LdrpDispatchUserCallTarget` `0x124580` / `…ES` `0x1245e0`) | 파일 값 그대로 |
| `__guard_xfg_table_dispatch_icall_fptr` (`0x1e6018`) | `+0x128` | 위와 같음 (`ntdll+0x1664b0`) | 있음 | 위와 같음 (`+0x382` 에서 `R9` = `LdrpCfgDispatchRoutineCallback`) | 파일 값 그대로 |
| `__castguard_check_failure_os_handled_fptr` (`0x1e6020`) | `+0x130` | 0 | **없음** | `LdrpCgRoutineCallback` (rva `0x114e30`, 25B) — 칸에 `0x18015ed50` 또는 0 | 0 그대로 |

- **쓰는 줄이 어디서 불리나**: 로드 구성 포인터 칸 다섯 (`+0x70` · `+0x78` · `+0x118` · `+0x120` · `+0x128`, CastGuard 면 `+0x130`) 을 `LdrpCfgProcessLoadConfig +0x26e ~ +0x387` 가 {칸 주소, 콜백} 쌍으로 모아 (`+0x264/+0x269`, `+0x2cd/+0x2d2`, `+0x32d/+0x332`, `+0x37d/+0x382`) `qsort` (`+0x39b`) → **`+0x3a0 CMP RBX,R14` · `+0x3a9 TEST R12D,R12D` · `+0x3ac JZ → +0x454` (반환 0)**. `R12D` = `LdrControlFlowGuardEnforced` 결과 (`+0x187`, `+0x18e JZ → +0x584` 가 0 으로). test.exe 는 `LdrSystemDllInitBlock +0xb8` (CFG 비트맵 주소, `0x1ea508`) 가 P1a·P2 에서 0 이라 `LdrControlFlowGuardEnforced` = 0 → 반환. 쓰는 것은 그 뒤 `+0x4be NtProtectVirtualMemory` → `+0x4e5 CMP RAX,R15` (`LdrpCfgCheckRoutineCallback`) · `+0x571 CMP RAX,R13` (`LdrpCfgDispatchRoutineCallback`) → 아니면 `+0x70c` 간접 호출 (`LdrpCgRoutineCallback`) 한 군데 — 안 탄다.
- CastGuard 칸 (`+0x130`) 은 `+0x17c BT R15D,0x18 · +0x181 JC → +0x6a9` — `GuardFlags` 비트 24 가 켜질 때만 수집 (ntdll `GuardFlags 0x10417500`, 비트 24 꺼짐).
- **읽는 줄**: 이 네 칸을 읽는 ntdll 코드는 **없다** (코드 참조 0; 로드 구성 `+0x118 ~ +0x130` 필드와 `.reloc` 만 가리킴). 옆 `0x1e6000` (`__guard_dispatch_icall_fptr`, 3.10 첫 줄) 만 CFG 간접 호출 (`0x172020 → 0x166360 → JMP [0x1e6000]`) 때마다 읽힌다.
- **사진이 못 읽은 이유**: 정적으로 못 정함 `[공백]` — `0x1e6000` 은 CFG 간접 호출마다 읽히니 쪽이 올라와 있어야 하는데 `????` 이다 (디버거 쪽 사정으로 `[추측]`). **동적 확인 필요** (동적 3).
- 지금 우리 (PE_emulator): **확인 필요** — `LdrControlFlowGuardEnforced` 덫이 0 갈래 (`Src/ntdll/LdrInit.cpp:1455 ~ 1463`, 블록 `+0xb8` 0) 라 콜백 안 부르는 것은 같다. 칸 값 = 파일 값을 이미지 기준 주소로 재배치한 값이어야 한다 (`.reloc` 가 `.00cfg` 쪽에도 적용되는지는 코드 안 봄). `0x1e6000` 은 간접 호출마다 읽혀 틀리면 바로 죽으니 돌고 있다면 맞다. 나머지 셋은 읽는 코드가 없어 틀려도 안 드러난다.

### 3. 간접 호출

**답: 쓰는 줄이 간접 호출 대상 안에 있는 곳은 `.00cfg` 4칸 하나 (`LdrpCfgProcessLoadConfig +0x70c (ret +0x711)`), 나머지는 없음.**

| 자리 | 대상 (좁힌 결과) | 쓰는 칸 | 안 타는 이유 |
|---|---|---|---|
| `LdrpCfgProcessLoadConfig +0x70c (ret +0x711)` `indirect:cfg` | 콜백 셋: `LdrpCfgCheckRoutineCallback` (rva `0xa5190`, `+0x4e5 CMP RAX,R15` 가 먼저 가름) · `LdrpCfgDispatchRoutineCallback` (`0xa6060`, `+0x571 CMP RAX,R13`) · 나머지 `LdrpCgRoutineCallback` (`0x114e30`, `+0x6d1 LEA RAX`, sast/58) | `.00cfg` 네 칸 + `.mrdata` 쪽 `__guard_check_icall_fptr` · `__guard_dispatch_icall_fptr` 칸 (로드 구성 `+0x70` · `+0x78`) | `+0x3ac JZ → +0x454` (CFG 강제 아님) |
| `LdrpLoadWow64 +0xf1` | 직접 호출 (`LdrGetProcedureAddress`) — 간접 아님 | `.mrdata` WOW64 5 | `UseWOW64` 0 |
| `LdrpInitializeProcess +0x1cdf` · `+0x1d16` · `+0x1b59` · `_LdrpInitialize +0x9a` | 이 바퀴의 칸을 쓰지 않음 (sast/109·111 이 이미 닫음; `+0x9a` 는 `g_LdrpWow64LdrpInitialize` 호출 = WOW64 만) | — | — |
| `…Reevaluate… +0xe0 · +0x106` | `g_wil_details_ensureSubscribed…` · `…subscribe…` 포인터 (111) | 기능 상태 칸 | 두 포인터 0 (이 바퀴 표 14·23번 근처 `N`) |

- 지금 우리: **확인 필요** (`LdrpCfgProcessLoadConfig` 몸통이 진짜인지 · 덫인지 코드 안 봄 — 안 타는 건 같다).

### 4. 줄기와 경로

- **줄기** (칸 값을 정하는 함수까지; 아래 "0 을 씀" 셋과 조건부 줄만 3장 양식, 아무도 안 씀·범위 밖은 표 `쓰는 줄` 칸에 이름만):
  - 0 을 씀: `LdrpInitializeProcess +0x773` (직접) · `LdrpInitializeProcess +0x199d (ret +0x19a2) LdrpInitializeKernel32Functions → +0xfa (ret +0xff) · +0x12b LdrGetProcedureAddressForCaller → +0x53 (ret +0x58) ExecuteHotpatchTestRuntimeFunction +0x17` · `LdrpInitializeProcess +0x531 (ret +0x536) LdrpInitializeExecutionOptions → +0x9d (ret +0xa2) LdrpInitializeCriticalSectionExceptionGlobalMitigation +0x44`, 이어 `NtOpenKey` (`+0x4b`) · `NtQueryValueKey` (`+0x7d`) · `NtClose` (`+0x89`).
  - 조건부: 표 `조건 · 길` (함수 사슬은 `chain` 으로 지도 BFS — 대표만): `LdrpInitializeProcess +0x1713 → AVrfInitializeVerifier` (m5) · `+0x19ef → LdrpInitShimEngine +0x109 → LdrpGetShimEngineInterface` · `+0x17c1 → LdrpLoadWow64 +0xe8` · `+0x195e → LdrpInitializeImportRedirection +0xf1 → LdrpBuildImportRedirection +0x93 → LdrpAddRedirectedFunction` · `+0x1c25 → LdrpInitializeSmtDelayedSleep +0x4c ~ +0xb1 → …QueryRegistry +0x75` · `+0x1c8 → LdrpInitializeNlsInfo +0x2a → LdrpFusionManifestCodePages +0xbc → RtlpGetProcessCodepagesForLocale +0x28 → RtlpLoadNlsData` · `+0x531 → LdrpInitializeExecutionOptions (+0x279 · +0x37d · +0x2fe · +0x1a2 · +0x10a IFEO 쪽)` · `_LdrpInitialize +0x64 JNE → +0x284 … +0x336 · +0x33d` (복제 자식) · `LdrpInitializeProcess +0x1aa4 (ret +0x1aa9) LdrpMapAndSnapDependency → +0x5e LdrpFindDllActivationContext → +0x158 LdrResFindResourceDirectory → +0x70 LdrResSearchResource → +0x30b LdrLoadAlternateResourceModuleEx · +0x727 LdrAddLoadAsDataTable · +0x3de RtlLcidToLocaleName` · `…LdrpSnapKernelBaseExtensions +0x1c5 → ApiSetpResolveHost +0x204 → ApiSetpIsFeatureEnabled → IsFeatureEnabled`.
  - **공용 부품 (한 번만)**: `RtlInitializeCriticalSectionEx` · `RtlRunOnceExecuteOnce` · `RtlAllocateHeap` · `NtOpenKey`/`NtQueryValueKey`/`NtClose` (6장).
- **줄기 밖 (이름만)**: 범위 밖 80칸의 쓰는 함수 전부 (표 `쓰는 줄`) · 조건부 줄 안쪽의 더 깊은 함수 (`AVrfpEnableVerifierOptions` 안 등).
- 지금 우리: **확인 필요** — 직접 줄은 `LdrpInitializeProcess` 몸통이 진짜라 자동으로 같다. 빈 덫 셋 (`LdrpInitializeCriticalSectionExceptionGlobalMitigation` · `LdrpPayloadRestrictionMitigationsEnabled` · `LdrControlFlowGuardEnforced`) 의 본문이 이제 `.asm` 으로 있다.

### 5. 111 / 3.8 에 고칠 것 · 새로 알게 된 것

1. **`LdrpInitMuiCrits` 첫 부름 좁힘 (111 정정)**: `LdrAddLoadAsDataTable` 은 `+0x2e TEST RCX,RCX · +0x31 JE → +0x22f` 를 통과해 `+0x37 LdrpInitMuiCrits` 로 들어오면, 표가 비었을 때 `+0x14d ~ +0x187` 에서 만들어 **`LoadAsDataTable` (`+0x172`) · `LoadAsDataTableBlockCount` (`+0x179`) 를 쓰고 `+0x142` 에서 `LoadAsDataTableCount` 를 올린다** (할당 실패 `0xc0000017` 만 예외). 사진 P1a · P1b · P2 에서 이 세 칸은 0 → **test.exe 는 init 동안 `LdrAddLoadAsDataTable +0x37` 에 안 들어갔다** `[정적 + 사진]`. `LoadAsDataCrits` · `LdrpInitOnceLoadAsDataCrits` 를 P1a~P1b 에 만든 `LdrpInitMuiCrits` 의 첫 부름은 나머지 셋 중 하나: **`LdrpGetFileSizeFromLoadAsDataTable +0x14`** (읽기만; 부르는 곳 `LdrpResGetMappingSize +0x1fc` ← `LdrResSearchResource +0x20c · +0x32d` · `LdrpResSearchResourceMappedFile +0x66a` · `LdrResGetRCConfig +0x1ce`) · `LdrRemoveLoadAsDataTable +0x34` (`LdrpResMapFile +0x100` 등) · `LdrpGetLoadAsEntry +0x2c` (`LdrLoadAlternateResourceModuleEx +0x32b`). **가장 그럴듯한 최단 길** = `LdrpInitializeProcess +0x1aa4 → LdrpMapAndSnapDependency +0x5e → LdrpFindDllActivationContext +0x158 → LdrResFindResourceDirectory +0x70 → LdrResSearchResource +0x20c (또는 +0x32d) → LdrpResGetMappingSize +0x1fc → LdrpGetFileSizeFromLoadAsDataTable +0x14 → LdrpInitMuiCrits`. 정확한 줄은 111 동적 2 로 (후보 셋으로 줄었다). **flow-writer: 3.8 `LdrpInitOnceLoadAsDataCrits` · `LoadAsDataCrits` 줄 `경로` 칸의 `LdrResSearchResource +0x727 → LdrAddLoadAsDataTable +0x37` 을 위 후보로 고칠 것.**
2. **`SmtDelayedConfiguration`**: 쓰는 줄은 `LdrpInitializeSmtDelayedSleep` 가 아니라 그 안 헬퍼 `LdrpInitializeSmtDelayedSleepQueryRegistry +0x75 MOV [RBX],ECX` (`NtQueryValueKey` 를 직접 부름 — `+0x40 CALL`). 키 `\\Registry\\MACHINE\\System\\CurrentControlSet\\Control\\Session Manager` 의 DWORD 5개 `SmtDelaySleepLoopWindowSize` · `SmtDelaySpinCountThreshold` · `SmtDelayBaseYield` · `SmtFactorYield` · `SmtDelayMaxYield` 가 있을 때만. 이 VM 은 없음 (사진 0). 진짜 윈도에서 이 키는 있을 테니 열릴 가능성이 높고, 그러면 syscall 은 `NtOpenKey` 1 · `NtQueryValueKey` 5 · `NtClose` 1 이 난다 (`[추정]`; PE_emulator 는 `NtOpenKey` 를 "없음" 으로 두어 `+0x2c` 에서 일찍 끝 — 칸 결과는 같지만 syscall 수가 다르다, 동적 2).
3. **`RtlpHpTagContext` · `RtlpHpTagInitVar`**: 힙 태그는 `RtlSetHeapInformation` 클래스 4 (`+0x86 CMP EDX,4`) 에서만 켜짐. init 호출 클래스는 1 (`LdrpInitializeProcess +0xa34`) 과 7 (`LdrpInitializeTlsHeap +0x9b`).
4. **`.mrdata` `AvrfAppVerifierMode`** (3.9, `0x1e7044`) 과 이 바퀴의 `.mrdata` AVRF 4칸은 같은 갈래 안 (같은 게이트 `+0x1568`).

## baggage.md 에 넣을 줄 (3장 양식)

단계 `②` 는 `LdrpInitializeProcess` 이하 (첫 스레드). 쓴 주인 모두 ntdll. 근거 칸은 `sast113` · 사진 구간.

### 3.8 ntdll `.data` 전역 — 아는 칸 +3 (0 을 씀 3)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `RuntimeGlobal` (`0x1cddb8`, 4B) | ② | 0 (핫패치 시험 함수가 불릴 때마다 0 으로 되씀. 죽은 줄 `+0xd` 의 `0x14` 는 `CMP RAX,0x20` 이 늘 거짓이라 안 씀) | 직접 (0 을 씀) | ntdll | `②… → LdrpInitializeKernel32Functions (call +0x199d) → LdrGetProcedureAddressForCaller (call +0xfa · +0x12b) → ExecuteHotpatchTestRuntimeFunction (call +0x53) +0x17` | `[정적]` sast113 · 정확 · 사진 `끝까지 0` |
| `RtlFailedCriticalDebugAllocations` (`0x1d272c`, 4B) | ② | 0 (`R12D`) — 임계구역 디버그 정보 할당 실패 횟수의 시작값. 실패 때만 `RtlpAddDebugInfoToCriticalSection +0x2bd` 가 +1 | 직접 (0 을 씀) | ntdll | `②… (LdrpInitializeProcess +0x773)` | `[정적]` sast113 · 정확 · 사진 `끝까지 0` |
| `RtlpRaiseExceptionOnPossibleDeadlock` (`0x1d4009`, 1B) | ② | 0 (그 뒤 보안 프로세스이거나 레지스트리 `Session Manager\\RaiseExceptionOnPossibleDeadlock` DWORD > 0 이면 1 — test.exe 는 0) | 직접 (0 을 씀) + syscall 읽기 | ntdll | `②… → LdrpInitializeExecutionOptions (call +0x531) → LdrpInitializeCriticalSectionExceptionGlobalMitigation (call +0x9d) +0x44 (1: +0xac)`; syscall (`+0x4b` NtOpenKey · `+0x7d` NtQueryValueKey · `+0x89` NtClose) | `[정적]` sast113 · 정확 · 사진 `끝까지 0` |

- **조건부 안 탐 77개 (7.24 ① 와 같은 선택: 3.8 에 "파일 처음 값 그대로 (조건)" 줄로 옮기면 "아는 칸" 으로 센다)** — 이름 · 쓰는 줄 · 조건은 §1 표 그대로. 묶음 줄 (옮긴다면 이 단위, `.data` 77): AppVerifier 12 · WIL 기능 (ApiSet 갈래 reporting 3 + featureState 7 + DOS 2 + 패키지 1) 13 · MUI·언어 5 · 언로드 4 · 경로 4 · 재지정 4 · SxS 3 · IFEO 3 · 힙 디버그·추적 3 + 힙 태그 2 + `TrkContext` 1 · `Tpp*` 3 · 오류 보고 3 · `LoadAsDataTable*` 3 · NLS 2 · 언어 키 2 · 복제 자식 2 · TLS 2 · 임계구역 (`PossibleDeadlockReportCounter` · `RtlpForceCSToUseEvents`) 2 · 각 1 (`RtlpProtectedPoliciesSRWLock` · `EtwpGuidEntryTable` · `LibLoaderTelemetryInitRunOnce` · `SmtDelayedConfiguration`).
- **아무도 안 씀 · 범위 밖 131개**: 3.8 에 새 줄 없이 7.3 에서 한 줄 묶음 ("처음 값 = 파일 = 0, sast113 §1 표") 로. 잠금 칸 7 (`MuiCacheSWRLock` · `RtlpSecMemLock` · `LdrpPathLock` · `RtlpCachedPathLock` · `LdrpDllDirectoryLock` · `RtlCriticalSectionLock` · `SbpContextLock`) 은 "잡고 풂 — 값 0 으로 돌아옴" 한 줄.
- **고칠 줄**: 3.8 `LdrpInitOnceLoadAsDataCrits` · `LoadAsDataCrits` 의 경로 (위 5.1) · 7.5 `RtlpForceCSToUseEvents` 등 "0 을 씀 후보" 메모 4개 (위 "111 정정 4개").

### 3.9 ntdll `.mrdata` 전역

- `.mrdata` `끝까지 0` 22: **조건부 안 탐 18** (AppVerifier 4 `AvrfpAPILookupCallbacksEnabled` · `AVrfpEnabled` · `RtlGuardAllowSuppressedCalls` · `AvrfpAPILookupCallbackRoutine` — `AVrfInitializeVerifier +0x1d3 ~ +0x1ea` · shim 9 — `LdrpGetShimEngineInterface +0x4a9 ~ +0x559` · WOW64 5 — `LdrpLoadWow64` 표 구동) · **범위 밖 3** (`RtlpProtectedPolicies` · `…ActiveCount` · `…TotalCount` — `RtlSetProtectedPolicy +0x143 · +0x165 · +0x14a`) · **쓰는 코드 없음 1** (`LdrpUnalignedUserCallTargetBitMap`). 3.9 에 묶음 한 줄 (조건부 안 탐 18 — `AvrfAppVerifierMode` · `PEB.pShimData` · `UseWOW64` 게이트) 로 옮기거나 7.3 `.mrdata` `끝까지 0` 에서 뺌: `.mrdata` 모름 26 → 4 (111 이 4 닫으면 0).
- 3.9 `g_LdrpWow64LdrpInitialize` 줄의 `[추측]` → `[정적]` (`LdrpLoadWow64 +0xe8 ~ +0xf1` 표 구동, 위 1.3).

### 3.10 ntdll `.00cfg` 칸

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `__guard_xfg_check_icall_fptr` (`0x1e6008`) · `__guard_xfg_dispatch_icall_fptr` (`0x1e6010`) · `__guard_xfg_table_dispatch_icall_fptr` (`0x1e6018`) · `__castguard_check_failure_os_handled_fptr` (`0x1e6020`) | ② (CFG 강제일 때만 — test.exe 안 탐) | 파일 처음 값: `ntdll+0xfac60` (`_guard_check_icall_nop`) · `ntdll+0x1664b0` 둘 (`_guard_xfg_dispatch_icall_nop`) · 0. 로드 구성 `+0x118 · +0x120 · +0x128 · +0x130` 이 가리키는 칸. CFG 강제면 콜백이 `LdrSystemDllInitBlock +0xf0 · +0xf8 · +0x100 · +0x108` (또는 `LdrpValidateUserCallTarget` · `LdrpDispatchUserCallTarget`) 를 씀 | 직접 (콜백, 간접 호출) | ntdll | `②… → LdrpInitializeNtdllDataTableEntry (call +0x11a2) → LdrpProcessMappedModule (call +0x124 · +0x145/+0x14a) → LdrpCfgProcessLoadConfig → +0x70c (간접) → LdrpCfgCheckRoutineCallback · LdrpCfgDispatchRoutineCallback · LdrpCgRoutineCallback` (`+0x3ac JZ → +0x454` 로 test.exe 는 반환) | `[정적 + 추정]` sast113 · 짐작 (사진 8장 읽기 불가) |

- 3.10 숫자: 전역 5 · 쓴 자리 아는 것 1 → (줄 하나로 묶으면) 아는 것 5 (모두 짐작: 안 탐), 모름 4 → 0. `못 읽음` 라벨은 그대로 두고 근거 칸에 "`????`" 유지.

### 7.3 남은 수 · 2장 "아직 모름"

| 안 | 7.3 모름 (현재 305 = `.data` 275 + `.mrdata` 26 + `.00cfg` 4) | 설명 |
|---|---|---|
| A. 값을 쓴 칸만 "아는 칸" (이 바퀴 0 을 씀 3) | **302** (`.data` 272) | 111 안 합치기 전 숫자 |
| B. 조건부 안 탐 줄도 3.8 · 3.9 · 3.10 줄로 (이 바퀴 3 + 99) | **203** (`.data` 195 · `.mrdata` 8 · `.00cfg` 0) | 203 = 111 몫 64 (`.data` 60 · `.mrdata` 4) + 이 바퀴 "아무도 안 씀 44 · 잠금 7 · 범위 밖 80" + sast/111 이 닫은 8 (111 안을 같이 반영하면 이 8 도 빠짐) |
| C. 위 + 이 바퀴 "아무도 안 씀 · 범위 밖" 도 7.3 에서 한 줄 묶음으로 뺌 | **64** | 111 몫 64 만 남음 |
| D. 111 · 113 모두 C 안 | **0** | 7.3 이름 305 가 전부 가름 표에 있음 (남는 것은 동적 확인 후보뿐) |

- **2장 한 줄 (Q78 짐 지도 C 정적 조각이 다 끝나면)**: 구조체 7.2 모름 **301칸 (이 바퀴와 무관, 그대로)** · 전역 7.3 모름 **305 → 0 (D 안, 가름 표로 닫힘) 또는 → 203 (B 안, 쓰는 줄 있는 것만 아는 칸으로)**. `[공백]` 표시 남는 것: `.00cfg` 4칸 P2 값 (읽기 불가) · `[추정]` 11칸 (조건을 점프 줄까지 못 푼 것 — 아래 "다음 요청 후보").

## 동적으로 확인할 것

정적으로 닫힌 것: 241칸 가름 전부. 아래는 "정적으로 정하지 못한 것" 과 "읽기만 하는 확인" (급하지 않음 — Q104 VM 단계에 얹을 것).

1. **`.00cfg` 쪽 읽기** (`[공백]`): `.process /p <test.exe>` 뒤 `.pagein ntdll+0x1e6000` 하고 `dq ntdll+0x1e6000 L5` — 기대 `ntdll+0x166480 · 0xfac60 · 0x1664b0 · 0x1664b0 · 0`. 안 되면 `!pte ntdll+0x1e6000`. 사진이 8장 모두 `????` 인 이유 (정적으로 못 정함).
2. **`LdrpInitializeSmtDelayedSleep` · `LdrpInitializeCriticalSectionExceptionGlobalMitigation` 의 `NtOpenKey` 반환** (읽기만): `ntdll!LdrpInitializeSmtDelayedSleep+0x1b` 직후 (ret `+0x20`) `r eax` · `ntdll!LdrpInitializeCriticalSectionExceptionGlobalMitigation+0x4b` 직후 (ret `+0x50`) `r eax` — 0 (열림) 이면 syscall 이 더 난다 (SMT: `NtQueryValueKey` 5 + `NtClose` 1 / 교착: `NtQueryValueKey` 1 + `NtClose` 1). PE_emulator 의 syscall 시간표 (`_workspace/orchestrator/130_syscall-timeline.md` 38번 줄) 와 맞추는 용. test.exe 일 때만.
3. **`LdrpInitMuiCrits` 첫 도착 콜스택 (111 동적 2 이어서, 후보 셋)**: `ntdll!LdrpInitMuiCrits` 입구 (rva `0x479f0`) 를 test.exe 일 때만 멈추고 `k` — `LdrpGetFileSizeFromLoadAsDataTable +0x14` / `LdrRemoveLoadAsDataTable +0x34` / `LdrpGetLoadAsEntry +0x2c` 중 어느 것.
4. (읽기만, 선택) **ApiSet 기능 갈래**: `ntdll!ApiSetpIsFeatureEnabled` 입구 (rva `0x1382c8`) 가 init 동안 몇 번 불리는지 · `ntdll!IsFeatureEnabled` 의 `ecx` 값들 — 0 번이면 위 §1.2 "기능 번호가 맞은 적 없음" 이 증명. 사진 `Feature_DriverSetup…` · `…ProjectTessera…` · `…LearningModeTraceService…` 0 과 일관.
5. (읽기만, 선택) **`LdrpNtdllHotPatchContext`** (`0x1d2908`): P1a·P1b 에 포인터 `0x9b2fcff9d0` 가 있고 P2 는 0. `LdrpInitializeProcess +0x18f6 CMP [R9+0xc],DL` 의 바이트 `db poi(ntdll+0x1d2908)+0xc L1` (P1b 시점) 가 0 인지 — `LibLoaderTelemetryInitRunOnce` 가 0 인 정적 설명의 한 고리.
- test.exe 일 때만 멈추게: sast/57 의 기존 조건 그대로.

## 다음 요청 후보

- **Q80 (파일 값 대조)**: 111 안 + 이 바퀴로 7.3 의 이름 305 가 모두 "파일 처음 값 그대로 (쓰는 줄 · 조건 · 없음 · 범위 밖) / 이 길이 값을 씀" 으로 닫혔다 — Q80 은 `.00cfg` 4 (재배치 뒤 값) 말고 새 일 없음.
- **정적 한 건 (작음, `[추정]` 11칸을 `[정적]` 으로)**: `LdrpGetDllPath +0x1c7 ~ +0x1f0` (`CentennialIssueTracker_InitializedState`) · `RtlpIsDosDeviceName_Ustr +0x4f2 ~ +0x542` (`Feature_TreatDosDevicePathsAsNormalFiles` 둘) · `LdrpHandleTlsData +0x425 ~ +0x46c` (`LdrpPotentialTlsLeaks` · `LdrpDelayedTlsReclaimTable`) · `TppCleanupGroupMemberInitialize +0x12b ~ +0x146` (`TppPoolpSerializedPool*`) · `EtwpUpdateEnableInfoAndCallback +0x282` 앞 조건 (`EtwpGuidEntryTable`) · `RtlpHpStackTraceConfig +0x30 ~ +0x61` (`RtlpHpStackTrackingContext`). 값은 이미 닫혀 있어 급하지 않다.
- **flow-writer**: 3.8 · 3.9 · 3.10 줄 + 7.3 숫자 (위 4안 중 사용자 선택) + 5.1 경로 고침 + 111 정정 4개 반영.
- **PE_emulator**: `LdrpInitializeCriticalSectionExceptionGlobalMitigation` 덫을 채울지 (결과는 같은 0 · syscall 3 이 빠져 있음 — 동적 2 로 실제 반환값 확인 뒤).

## 오프셋 대조

- **자동 대조**: 본문의 `함수이름 +0x…` 쌍 **508개** (같은 쌍이 여러 번 나오면 번번이 셈, 표 포함) 가 ① 함수 이름이 `ms/decomp/ntdll/_index.tsv` 에 있고 ② `함수 시작 + 오프셋` 이 objdump 풀이 `ntdll.dis` 의 **실제 명령 시작** 인지 스크립트로 확인 → **모두 맞음 (불일치 0)**. 이름이 같은 함수가 둘 이상 (`_guard_*`, ICF 가 합친 것) 이면 어느 시작 하나라도 맞으면 통과로 셌다. call 자리와 ret 자리 (call 다음 줄) 둘 다 명령 시작이라 같은 방법으로 확인된다. 점프 목적지 (`JNZ → +0x17` 따위) 는 (목적지 절대 주소 − 함수 시작) 으로 셈해 같은 풀이에서 다시 확인했다.
- **대상이 아닌 `+0x…`** (자동 대조에서 뺌): 구조체 칸 (`PEB+0xbc` · `[RAX+0x1e]` · `[R9+0xc]` · `[항목+6]` 등) · 로드 구성 필드 (`+0x118` ~ `+0x130`) · `ntdll+0x…` (이미지 rva) · 데이터 이름 `+0x…` (`UcIsTraceLoggingInitialized+0x4`) · 3장 양식의 `(call +0x…)` (앞 함수 안 call 자리 — 예 `LdrpInitializeKernel32Functions (call +0x199d)` 의 `+0x199d` 는 `LdrpInitializeProcess` 안) · 같은 backtick 범위에서 앞 함수 이름을 이어받는 짧은 `+0x…` 776개 중 위 세 갈래 6개.
- **눈으로 읽고 고친 것 둘**: `ExecuteHotpatchTestRuntimeFunction +0xb` 의 점프 목적지 (`ret` 가 아니라 `+0x17`, 위 한눈에) · `_LdrpInitialize +0x32d` 블록의 진입점 (`+0x29f JNE` 만, 복제 자식).
- **알려진 사실 대조**: 위 "버전 확인" (`_calls.tsv` 읽은 줄 그대로) 일치.

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **제안 1**: `globals.tsv` 의 "크기" 가 다음 이름까지 거리라 이웃 칸·이름 없는 칸 (`UcIsTraceLoggingInitialized+0x4` · `RtlpLockedMemoryZoneCount+0x4` · `RtlpDiskSpeedInit+0x8 · +0xc`) 이 이름 줄에 섞였다. 이번엔 쓰는 줄을 `+0x0` 에 닿는 것만 적었다. 111 제안 2 (코드 접근 폭 열) 를 다시 제안한다.
- **제안 2**: `run_ghidra.sh refs` 가 이름 하나씩이라 241칸에 못 썼다. `ntdll.dis` 풀이 + `refs.py` 가 대신했는데 **레지스터 간접 쓰기 (`[R15]` · 표 구동)** 는 못 본다 — 이번에 `WOW64` 표 · `EtwpGuidEntryTable` · WIL 기술자에서 눈으로 보탰다. `.rdata` 포인터 표가 칸 주소를 들고 있는 경우 (`ptr` 줄) 를 `refs` 가 "표 구동 쓰기 후보" 로 보여 주면 좋겠다.
- **제안 3**: `ExportFunctions` 는 지도 깊이 10 을 넘은 함수 (`RtlpMoveActCtxToFreeList` 류) 는 `out` 으로 보이게 한다. "out" 이 공개 API 인지 깊이 한계인지 `_nodes.tsv` 에 `depth` 상태가 있는 것 (`depth=218`) 과 "없는 것" 을 구분하는 열이 있으면 좋겠다.

## 판정

**분석성공.** 241칸 모두 가름 (0 을 씀 3 · 조건부 안 탐 99 · 아무도 안 씀 51 (잠금 7 포함) · 범위 밖 80 · sast/111 이 닫음 8). 버전 대조 일치, PDB 일치 (ntdll `C3093720177DA6851519DCFFE8DF53FE1`, 커널 정보 파일 `C29EBFB06B78B3C020DCA66D99713F9E1`). **고친 것**: 111 후보 4 ("0 을 씀" 아님) · `RuntimeGlobal` 첫 판 (죽은 줄 → 늘 0 을 씀) · 3.8 `LoadAsDataCrits` 경로 (`LdrAddLoadAsDataTable` 아님). **정적으로 못 닫은 것**: `.00cfg` 4칸 P2 값 (사진 읽기 불가 → 파일 값 + 콜백 안 탐으로 추정) · `[추정]` 11칸의 정확한 점프 조건 (값은 닫힘) · `LdrpInitMuiCrits` 첫 부름 줄 (후보 3). dast 요청 필요: 위 "동적으로 확인할 것" 1 ~ 3 (읽기·멈춤점, 급하지 않음).

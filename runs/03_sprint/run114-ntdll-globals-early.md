# 2026-11-16 (작성일) run114 — ntdll 전역 앞쪽 (Q78e2 + Q78f + Q80, 정적만): 7.3 `≤P1a` 61 · `P1a~P1b` 3 · 그 밖 8 = 72개 가름. **값을 쓰는 줄 18** (② 16 · syscall 2) · **0 을 씀 2** · **조건부 안 탐 16** → 3.8 로 옮김 (모름 304 → **268**), 아무도 안 씀 22 · 범위 밖 14 는 7.3 에 표시만. `EnvironmentVersion` 나머지 +2 = `RtlpWow64ThunkEnvironmentTo64` 안 `RtlSetEnvironmentVar` 두 번 (해시 표 아님 — run45 정정), 프로세스 힙 LFH 는 사진 P1a 앞에 이미 켜짐 (run46 정정), Q80 (파일 값 대조) 닫힘

(글의 오프셋은 call 자리 · 쓰는 줄 자리. "호출 관계" 나무의 callee 앞 오프셋은 ret 자리 (콜스택 표시) — 둘 다 아래 "정적 — 호출 지점" 표.)

> **지도 범위**: 그림은 안 고쳤다 (run112 · run113 과 같은 까닭). 새 단계 · 새 caller 가 없고, 이미 있는 함수 **안쪽** 줄과 그 아래 한두 걸음만 늘었다. 글은 flow.md 기준 · 2.4 · 4장 · 5장, 짐 지도는 3.2 · 3.8 · 3.9 · 7.3 과 2장 · 7.1 · 7.5 · 7.24 · 7.44 를 고쳤다.

## 요약 표 (72개 → ?)

셈 기준은 오케스트레이터가 정했다 (run60 `LdrpMrdataHeap*` · run66 `g_pShimmedModuleList` 선례):

- 값을 쓰는 줄이 있는 전역 (② · syscall) 과 **0 을 씀** → 3.8 아는 줄.
- **조건부 안 탐** 도 3.8 로 옮긴다 (쓰는 줄은 길 안에 있고, test.exe 가 조건을 안 채움).
- **아무도 안 씀** (결과서 말로 "쓰는 줄 없음") · **범위 밖 길** 은 이번엔 7.3 에 두고 "(run114 가름: …)" 표시만. 전역에 그 가름을 셈 열로 새로 둘지는 **사용자 결정 대기** (sast/111 · sast/113 결과서의 A/B/C 안).

| 묶음 | 칸 | ② 쓰는 줄 | ② syscall | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (잠금 칸 포함) | 범위 밖 길 | → 3.8 | 7.3 에 남음 |
|---|---|---|---|---|---|---|---|---|---|
| `.data ≤P1a` | 57 | 14 | 2 | 0 | 11 | 19 (잠금 2) | 11 | 27 | 30 |
| `.mrdata ≤P1a` 2 · `K1 이전` 2 | 4 | 0 | 0 | 0 | 0 | 2 | 2 | 0 | 4 |
| `.data P1a~P1b` (Q78f) | 3 | 2 | 0 | 0 | 0 | 0 | 1 | 2 | 1 |
| 그 밖 · 조건부 힙 새 4 (`끝까지 0`) | 4 | 0 | 0 | 0 | 4 | 0 | 0 | 4 | 0 |
| 그 밖 · sast/109 넘김 4 (`끝까지 0`) | 4 | 0 | 0 | 2 | 1 | 1 (잠금) | 0 | 3 | 1 |
| **합** | **72** | **16** | **2** | **2** | **16** | **22** | **14** | **36** | **36** |

- 결과서 맨 앞 셈 (64 = 18 + 46) 과 같은 숫자다. 결과서 "파일 처음 값 그대로 46" = 아무도 안 씀 21 + 조건부 안 탐 11 + 범위 밖 14.
- `LdrpHeap` 는 이미 3.8 (run46) — 결과서도 한 줄 확인만.
- 그래서 짐 지도 숫자 (2장):
  - ntdll `.data` 쓴 자리 아는 것 164 → **200** (정확 160 → **197** · 짐작 4 → **3**) · 모름 274 → **238** · nt 5 → **6** · ntdll 162 → **197**.
    - nt +1 은 `LdrpHotPatchInitCompleteEvent` 하나 — `NtCreateEvent` 가 **칸에 직접** 핸들을 쓴다 (`LdrpInitCompleteEvent` 와 같은 규칙, nt 만). `RtlpHypervisorSharedUserVa` 는 커널이 지역 버퍼 (`[RSP+0x168]`) 에 쓰고 ntdll `+0xb5e` 가 칸에 옮기므로 **ntdll 로 셌다** (run59 규칙 — 캐시 전역).
    - 정확이 37 늘고 짐작이 1 준 까닭: 새 36 (모두 정확) + **`LdrpDebugFlags`** (`ShowSnaps` 와 같은 주소 `0x1ccf60` 의 다른 이름) 가 짐작 → 정확. `LdrpInitializeProcess +0x59c` · `+0x754` `OR [LdrpDebugFlags],…` 가 `.asm` 에 있다 (조건부, 3장 규칙 "정확 줄이 하나라도 있으면 정확" — run49).
  - ntdll 전역 3구역 합: 아는 것 189 → **225** · 모름 304 → **268**. `.mrdata` 는 그대로 (모름 26).
  - 7.3 사진 구간별 모름: `≤P1a` 59 → **32** · `P1a~P1b` 3 → **1** · `끝까지 0` 236 → **229** (`.data` 214 → 207) · `K1 이전` 2 · 못 읽음 4.
  - 아는 전역 중 끝까지 0 (7.1): 60 → **67** (0 을 씀 2 + 조건부 5).
  - "0 그대로 (사진)" 열 (`.data` 265*) 은 **그대로** — 사진 분류는 짐작 크기 기준이라 안 고친다 (run66 선례). 이름 칸 폭으로 보면 `ShowSnaps` · `RtlpDisableHeapLookaside` · `RtlpSearchPathMode` 셋이 `끝까지 0` 이다 (아래 "구간 라벨 정정").

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: **ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`** (사진 로그 `lm` 줄과 같음 — 결과서), ntoskrnl pdb 정보 파일 `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음). **ntoskrnl Ghidra 프로젝트는 안 열었다** (그때 sast/110 이 도는 중).
- 버전 대조 (결과서): `LdrInitializeThunk +0xe → LdrpInitialize` · `LdrpInitializeInternal +0x5a → _LdrpInitialize` · `_LdrpInitialize +0x16c → LdrpInitializeProcess` · `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse` · `LdrpInitializeNode +0x16b → LdrpCallTlsInitializers` · `+0x19c → LdrpCallInitRoutine` · `LdrpLoadDll +0x100 → LdrpLoadDllInternal` 가 `_calls.tsv` 와 같다. 본문 `함수 +0x…` 308쌍을 결과서 스크립트가 objdump 풀이 명령 시작과 맞춤 (불일치 0).
- 사진 값 대조: 결과서가 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_{P1a,P1b,P2}.txt` 의 `.data` `dq 7ffafb82c000 L0x11eb` · `.mrdata` `dq 7ffafb847000 L0x6b7`) 와 ntdll 파일 구역 바이트 (재배치 뒤) 를 칸마다 견줬다. `.data` · `.mrdata` 앞 3페이지는 K1 ~ P1 읽기 불가 (7.3). 새 동적 실행은 없다.
- **sast/111 결과만 넣었다.** sast/110 · 112 ~ 120 은 안 넣었다. 예외 둘 (오케스트레이터 지시): sast/113 의 "`LdrAddLoadAsDataTable +0x37` 안 탐" 한 가지 (3.8 `LoadAsDataCrits` 경로 칸) 와 sast/120 (a) 의 `LdrpPolicyBits` 쓰는 줄 다시 셈 (5개 같음).

## 목적

- **Q78e2** (Q78e 의 전역 몫): 짐 지도 7.3 `.data ≤P1a` 57 · `.mrdata ≤P1a` 2 · `K1 이전` 2 의 쓴 자리. 묶음 7 힙 뒤 부분 (`LdrpInitializeProcess +0xa50 ~ +0xb7d`), run46 이 넘긴 조건부 힙 전역 8, run113 (sast/109) 이 넘긴 전역 4, 매개변수 `EnvironmentVersion` 나머지 +2.
- **Q78f**: `.data P1a~P1b` 남은 셋 (`RtlpSearchPathMode` · `LdrpStatistics` · `LoadAsDataCrits`).
- **Q80** (같이 닫기): 같은 `≤P1a` 전역을 "ntdll 파일 처음 값 vs P1a 사진 값" 으로 가름.

## 방법

- 정적: `_workspace/sast/111_ntdll-globals-early.md` — ntdll, 모두 depth 0 `^이름$`.
  - 새로 뽑음 17개 (두 번): `RtlpWow64ThunkEnvironmentTo64` · `RtlSetEnvironmentVar` · `RtlpSetupEnvironmentHashTable` · `RtlpScanEnvironment` · `RtlpInitializeLfhRandomDataArray` · `RtlpActivateLowFragmentationHeap` / `LdrpInitMuiCrits` · `LdrAddLoadAsDataTable` · `LdrpResMapFile` · `RtlpPerformHeapMaintenance` · `RtlpClearEnvironmentHashTable` · `RtlpAllocateEnvironmentHashTable` · `LdrpInitializePerUserWindowsDirectory` · `LdrpUpdateStatistics` · `AvrfMiniLoadDll` · `RtlpDiskSpeedInitialize` · `RtlSetSearchPathMode`. `_calls.tsv` 4,192 → 4,328 줄. (결과서가 이미 있던 `RtlSetHeapInformation` 을 한 번 더 썼다고 스스로 적음 — 같은 함수 · 같은 오프셋.)
  - 읽기만: `LdrpInitializeProcess` · `LdrpInitializeInternal` · `RtlInitializeHeapManager` · `RtlpCreateLowFragHeap` · `RtlpCreateHeap` · `RtlpInitEnvironmentBlock` · `LdrpInitializePolicy` · `LdrpCaptureCriticalThunks` · `RtlpAddDebugInfoToCriticalSection` · `RtlpLookupCurDirSetting` · `TppCleanupGroupMemberInitialize` · `LdrAddDllDirectory` · `LdrpInitializeExecutionOptions` · `SbUpdateSwitchContextBasedOnDll` · `LdrResSearchResource` · `LdrpFindDllActivationContext` · `LdrpInitializeKernel32Functions` · `wil_details_*` 등.
  - 훑기 (결과서 스크래치, 커밋 안 함): ntdll 전체 objdump 풀이의 RIP 상대 목적지로 이름 칸 `[주소, 주소 + 폭)` 에 가는 명령을 쓰기 · 읽기 · `lea` 로 가름 + 지도 깊이 (`m0 ~ m10` / `out`). **한계: 레지스터에 담은 포인터로 쓰는 줄은 안 잡힘** — 그런 갈래는 함수 `.asm` 을 읽어 보탬 (`RtlpAddDebugInfoToCriticalSection +0x353` 등).
  - 이름 칸 폭: PDB 에 크기가 없어 7.3 크기 ("다음 이름까지 거리") 대신 **코드가 접근하는 폭** 으로 봤다 → 7.3 라벨 일부가 옆 칸 때문이었다 (아래).
- 결과서: `_workspace/sast/111_ntdll-globals-early_result.md` (**분석성공** — 스크립트 문제 없음, 제안 셋).
- flow-writer 가 다시 본 것:
  - ntdll `.asm` 줄: `LdrpInitializeProcess +0x481` · `+0x4db` · `+0x4fe` · `+0x59c` · `+0x6b5` · `+0x73a` · `+0x754` · `+0x76d` · `+0x773` · `+0x78d` · `+0x7c4` · `+0x82e` · `+0x86e` · `+0xaad` · `+0xad3` · `+0xae6` · `+0xaed` · `+0xb4d` · `+0xb5e` · `+0xc2d` · `+0xcca` · `+0x1568` · `+0x1ca7` · `+0x1cae` · `+0x1cfe`, `RtlpInitEnvironmentBlock +0x86` · `+0x9b` · `+0xa4`, `RtlpWow64ThunkEnvironmentTo64 +0xa5` · `+0x126`, `LdrpInitializeInternal +0x3b` · `+0x8e`, `RtlpCreateLowFragHeap +0x256` · `+0x25c`, `RtlpInitializeLfhRandomDataArray +0x4b`, `RtlpPerformHeapMaintenance +0x22`, `RtlpActivateLowFragmentationHeap +0xde`, `LdrpInitMuiCrits +0x35` · `+0x4f` · `+0x60`, `LdrpUpdateStatistics +0xe` · `+0x2b` · `+0x33`, `LdrpCaptureCriticalThunks +0x67` · `+0x95` · `+0xa1`, `RtlpAddDebugInfoToCriticalSection +0x12` · `+0x353` · `+0x356`, `LdrpInitializePolicy +0x7e` · `+0x26b`, `RtlpSetupEnvironmentHashTable +0x1e` · `+0x52`, `RtlpLookupCurDirSetting +0x5f` · `+0x6e`, `…ReevaluateCachedFeatureEnabledState +0xa0`, `AvrfMiniLoadDll +0xa4f`, `LdrpInitializeExecutionOptions +0xa6` · `+0xad` · `+0x127` · `+0x1fe` · `+0x49c` · `+0x5fe`, `RtlInitializeHeapManager +0x2f` · `+0x31` · `+0x5b` · `+0x17f` · `+0x186`, `RtlCreateTagHeap +0x45` · `+0x97`, `LdrpInitShimEngine +0xf5`, `TppCleanupGroupMemberInitialize +0x17d` · `+0x284`, `RtlpLogHeapFailure +0x21`, `LdrpInitializePerUserWindowsDirectory +0x47` · `+0x4e` · `+0x50`, `SbUpdateSwitchContextBasedOnDll +0xfb` · `+0x2ba`.
  - `_calls.tsv`: 아래 표의 call · ret 줄. `RtlpAllocateHeap` 은 `.asm` 이 없다 — `+0xe5` 는 결과서 objdump 풀이 `[정적: objdump]`.
  - `_baggage/globals.tsv`: `LdrpDebugFlags` · `ShowSnaps` 둘 다 `0x1ccf60` (같은 주소 두 이름, 전역 수 438 에 둘 다 셈) · `RtlFailedCriticalDebugAllocations` `0x1d272c` (이름 있는 전역).

## 결과

비유 (새 가게 = test.exe 프로세스):

- **ntdll `.data`** 는 가게 뒤 창고 선반이다. 선반 칸마다 이름표가 붙어 있고, 공장 (ntdll 파일) 에서 올 때 이미 물건이 들어 있는 칸도 있다.
- 이번에 남은 칸 72개를 다 열어 보니, 절반 (36) 은 **공장에서 온 그대로** 였고 이 가게 문 여는 길에선 아무도 안 만진다. 나머지 절반 중 18칸은 문 열 때 직원이 직접 채웠고 (그중 2칸은 관청 (nt) 이 번호를 써 줌), 2칸은 "0" 을 적었고, 16칸은 "특별한 가게일 때만" 채우는 칸이었다 (패키지 앱 · 앱 검사기 · shim · IFEO 가 있을 때).
- 이름표가 칸 크기를 잘못 짐작해 **옆 칸 물건을 자기 것처럼 보이게** 한 곳이 넷 있었다 (아래 "구간 라벨 정정").

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeInternal` | `+0x8e` | `+0x93` | `NtCreateEvent` | `LdrpHotPatchInitCompleteEvent` 칸 주소를 넘김 — `+0x3b CMPXCHG.LOCK [LdrpHotPatchInitialized]` 가 이긴 첫 스레드만 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x481` | `+0x486` | `LdrpCaptureCriticalThunks` | → `+0x67 NtQueryVirtualMemory` (클래스 4) → `+0x95` `LdrpThunkSignature` 16B × 5 | [정적] (run18 그대로) |
| `ntdll!LdrpInitializeProcess` | `+0x7c4` | `+0x7c9` | SList 에 한꺼번에 넣는 함수 (`InterlockedPushListSList`, 같은 주소 이름 2개, 후보 `RtlInterlockedPushListSList`) | `RtlCriticalSectionDebugSList` ← `RtlpStaticDebugInfo` 8항목 | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x12a` | `+0x12f` | `RtlInitializeCriticalSectionEx` | `RtlpProcessHeapsLock` (깃발 `0x10000000`) → `+0x1c4` `RtlpAddDebugInfoToCriticalSection` → `+0x353 · +0x356` `RtlCriticalSectionList` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xaad` | `+0xab2` | `Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_IsEnabledNoReportingNoInline` | → … `wil_details_AreDependenciesEnabled +0x32` → … `Reevaluate +0xa0` — 필요 기능 `Feature_KVS_BugFixes_55830752` 상태 칸 | [정적] (run62 길 + 이번 칸) |
| `ntdll!LdrpInitializeProcess` | `+0xada` | `+0xadf` | `RtlInitializeCriticalSectionEx` | `LdrpEnclaveListLock` (`+0xad3 LEA`, `Flags 0`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xb4d` | `+0xb52` | `NtQuerySystemInformation` (같은 주소 이름 2개, 후보 `RtlGetNativeSystemInformation`) | 클래스 `0xc5` → `+0xb5e` `RtlpHypervisorSharedUserVa` (`+0xb54 JS` 면 안 씀) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xc2d` | `+0xc32` | `RtlpInitEnvironmentBlock` | `+0x86` `EnvironmentVersion` +1 · `+0x9b` `RtlpSetupEnvironmentHashTable` · **`+0xa4` `RtlpWow64ThunkEnvironmentTo64`** | [정적] (run45 + 이번) |
| `ntdll!RtlpWow64ThunkEnvironmentTo64` | `+0x126` | `+0x12b` | `RtlSetEnvironmentVar` | 표 `Wow64EnvironmentThunkTable` (`.rdata` `0x173b30`) 줄마다 — 앞 `+0xa5 RtlQueryEnvironmentVariable(W6432 이름)` 가 성공할 때만 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xfc5` | `+0xfca` | `LdrpInitializePolicy` | `+0x7e · +0x26b · +0x305 · +0x31d · +0x329` `LdrpPolicyBits` — 패키지 앱 + `DllPath` 비어 있지 않음일 때만 | [정적] (run65 그대로) |
| `ntdll!RtlpPerformHeapMaintenance` | `+0x22` | `+0x27` | `RtlpActivateLowFragmentationHeap` | 힙 깃발 비트 29 · `RtlpDisableHeapLookaside` 비트 0 꺼짐일 때 | [정적] |
| `ntdll!RtlpActivateLowFragmentationHeap` | `+0xde` | `+0xe3` | `RtlpCreateLowFragHeap` | → `+0x256` `RtlpLowFragHeapGlobalFlags` 비트 0 · `+0x25c` `RtlpInitializeLfhRandomDataArray` (`+0x4b` 32칸) | [정적] |
| `ntdll!RtlpAllocateHeap` | `+0xe2 ~ +0xe5` | — | `RtlpPerformHeapMaintenance` | 힙 `+0x78 & 0x30000000` 일 때 (`+0xd9` · `+0xe0`) | [정적: objdump] (`.asm` 없음) |
| `ntdll!LdrpDrainWorkQueue` | `+0x15b` | `+0x160` | `LdrpUpdateStatistics` | 주 스레드 큐 비우기 → `LdrpStatistics` `+0xe` · `+0x2b` | [정적] |
| `ntdll!LdrpWorkCallback` | `+0x64` | `+0x69` | `LdrpUpdateStatistics` | 로더 일꾼 스레드 → `+0x33` (`LdrpStatistics+0x4`) | [정적] |
| `ntdll!LdrAddLoadAsDataTable` | `+0x37` | `+0x3c` | `LdrpInitMuiCrits` | (sast/113) **test.exe 는 이 줄로 안 들어감** — 표 칸 셋이 사진 0. 첫 부름은 다른 셋 중 하나 (아래 1h) | [정적] |
| `ntdll!LdrpInitializePerUserWindowsDirectory` | `+0x47` | `+0x4c` | 간접 (`indirect:cfg`) — `kernel32!TermsrvGetWindowsDirectoryW` | 반환 길이 0 이면 `+0x4e JNZ` 안 뛰고 `+0x50` 끝 → `+0x17b LdrAddDllDirectory` 안 탐 | [정적] (대상 이름은 `LdrpInitializeKernel32Functions +0x12b` 이 찾는 글자) |
| `ntdll!LdrpSendPostSnapNotifications` | `+0xa3` | `+0xa8` | `SbUpdateSwitchContextBasedOnDll` | `+0xfb INC.LOCK` `SbDynamicContextDllCount+0x4` (개수 칸, 이름 칸 아님) | [정적] (run66 그대로) |

### 호출 관계

```
[test.exe 첫 스레드 — ② ntdll]
LdrpInitializeInternal  +0x3b LdrpHotPatchInitialized 0→1 (이긴 스레드만)
 └─ +0x93  NtCreateEvent(&LdrpHotPatchInitCompleteEvent)           ← nt 가 핸들 (8) 을 씀
_LdrpInitialize ─ +0x16c LdrpInitializeProcess
   +0x4db / +0x4fe  LdrpEnforce/AuditIntegrityContinuity ← 시작 정보 +0xa8 비트 4~5 (test.exe 0)
   ├─ +0x486  LdrpCaptureCriticalThunks  +0x6c NtQueryVirtualMemory · +0x95 LdrpThunkSignature
   +0x59c / +0x754  LdrpDebugFlags(= ShowSnaps) |= 1   (NtGlobalFlag & 2 일 때만 — 안 탐)
   +0x6b5 RtlpTimeout ← PEB+0xc0 · +0x76d RtlpTimeoutDisable = 1 · +0x773 RtlFailedCriticalDebugAllocations = 0
   +0x78d ~ +0x7b4  SList 머리 0 · RtlpStaticDebugInfo 8항목 잇기
   ├─ +0x7c9  (SList 한꺼번에 넣기)  RtlCriticalSectionDebugSList 깊이 8
   +0x82e ~ +0x86e  RtlpFlsContext 0 · 목록 머리 자기
   ├─ +0x9f2  RtlInitializeHeapManager
   │          └─ +0x12f RtlInitializeCriticalSectionEx(&RtlpProcessHeapsLock) ─ +0x1c9 RtlpAddDebugInfoToCriticalSection
   │                                                                          +0x17 SList 에서 꺼냄 · +0x353/+0x356 RtlCriticalSectionList 꼬리
   ├─ +0xa50  LdrpInitializeProcessHeap   (run46)
   │          … 그 뒤 첫 힙 할당 어딘가 ─ RtlpAllocateHeap +0xe5 ─ RtlpPerformHeapMaintenance
   │            └─ +0x27 RtlpActivateLowFragmentationHeap ─ +0xe3 RtlpCreateLowFragHeap
   │                 +0x256 RtlpLowFragHeapGlobalFlags |= 1 · +0x261 RtlpInitializeLfhRandomDataArray (+0x4b 32칸)
   ├─ +0xab2  Feature_…DisallowSharedLockImplicitUpgrade… ─ … AreDependenciesEnabled ─ … Reevaluate +0xa0 (KVS 상태 칸)
   ├─ +0xadf  RtlInitializeCriticalSectionEx(&LdrpEnclaveListLock)   +0xae6/+0xaed LdrpEnclaveList 머리
   ├─ +0xb52  NtQuerySystemInformation(0xc5)   +0xb5e RtlpHypervisorSharedUserVa = 0x7ffe3000
   ├─ +0xc32  RtlpInitEnvironmentBlock   +0x86 EnvironmentVersion 0→1
   │          ├─ +0xa0 RtlpSetupEnvironmentHashTable  (RtlpEnvironmentHashTable 머리)
   │          └─ +0xa9 RtlpWow64ThunkEnvironmentTo64
   │               ├─ +0xaa RtlQueryEnvironmentVariable(ProgramW6432)  (표가 비면 RtlpScanEnvironment 로 채움)
   │               └─ +0x12b RtlSetEnvironmentVar(ProgramFiles)       1→2   (표 둘째 줄 CommonProgramFiles 2→3)
   ├─ +0xfca  LdrpInitializePolicy   (LdrpPolicyBits — 패키지 앱만, 안 탐)
   ├─ +0x1aa9 LdrpMapAndSnapDependency … LdrpInitMuiCrits  +0x35 Once 0→1 · +0x54 LoadAsDataCrits · +0x65 Once→2   (P1a~P1b)
   ├─ +0x1ab1 / +0x1b91 LdrpDrainWorkQueue ─ +0x160 LdrpUpdateStatistics   (+ 일꾼 LdrpWorkCallback +0x69)
   └─ +0x1b77 LdrpInitializePerUserWindowsDirectory ─ +0x4c (간접) TermsrvGetWindowsDirectoryW → 길이 0 → 끝
```

### 1. 값을 쓰는 줄이 있는 전역 18 (② 16 · syscall 2) — 결과서 1.1 · 1.3 · 1a ~ 1i

| 이름 (rva) | 구간 | 쓰는 줄 | 값 (사진) |
|---|---|---|---|
| `RtlCriticalSectionList` (`0x1cc8d0`) | `≤P1a*` | `RtlpAddDebugInfoToCriticalSection +0x353` · `+0x356` (꼬리 잇기) | P1a 항목 0 (`RtlpProcessHeapsLock`) · 1 (프로세스 힙 락) → P1b 항목 2 |
| `RtlpFlsContext` (`0x1cde30`) | `≤P1a*` | `LdrpInitializeProcess +0x82e ~ +0x86e` | 0 · 목록 머리 자기 (`+0x48`). P2 에 바뀜 (`RtlFlsSetValue` 등 — 이름만) |
| `RtlCriticalSectionDebugSList` (`0x1cde90`) | `≤P1a*` | `+0x78d` (0) · `+0x7c4` 한꺼번에 넣기 | 깊이 8 → P1a 6 → P1b 5 |
| `RtlpStaticDebugInfo` (`0x1d4020`) | `≤P1a*` | `+0x79e ~ +0x7b4` (8항목 잇기) | 항목 0 `+0x8` = `RtlpProcessHeapsLock`, 항목 1 `+0x38` = 프로세스 힙 락 |
| `LdrpThunkSignature` (`0x1cdfa0`, 이름 칸 `0x50`B) | `≤P1a*` | `LdrpCaptureCriticalThunks +0x95` | 스텁 5개 앞 16B — 사진 80B 와 바이트 단위로 같음 |
| `RtlpTimeout` (`0x1d2730`) | `≤P1a` | `LdrpInitializeProcess +0x6b5` (PEB `+0xc0` 복사) · `+0x73a` (LoadConfig 값 ≠ 0 일 때만 덮음 — 안 탐) | `0xffffe86d079b8000` = −30일 (값 주인 nt, PEB 와 같음) |
| `RtlpTimeoutDisable` (`0x1d2728`) | `≤P1a` | `+0x76d` (`RtlpTimeout` < −1시간이면 1) | 1 |
| `RtlpLowFragHeapGlobalFlags` (`0x1d2218`) | `≤P1a` | `RtlpCreateLowFragHeap +0x256` (비트 0) | 1 |
| `RtlpLowFragHeapRandomData` (`0x1d3420`, `0x100`B) | `≤P1a*` | `RtlpInitializeLfhRandomDataArray +0x4b` (32칸 xorshift64\*) | 난수 (씨앗 `RtlpHpHeapGlobals +0x40`) |
| `RtlpEnvironmentHashTable` (`0x1d2840`, `0x20`B) | `≤P1a` | `RtlpSetupEnvironmentHashTable +0x10 ~ +0x52` · `RtlpScanEnvironment +0x29d` · `+0x3b3 ~ +0x3cc` | `{개수 0x2f, 크기 0x400, 표, 스캔됨 1, 환경}` |
| `LdrpEnclaveList` (`0x1d4220`) | `≤P1a` | `LdrpInitializeProcess +0xae6` · `+0xaed` | 자기 가리킴 |
| `LdrpEnclaveListLock` (`0x1d4240`) | `≤P1a` | `+0xad3` 주소 → `+0xada RtlInitializeCriticalSectionEx(칸, 0, 0)` | `DebugInfo −1` · `LockCount −1` · `SpinCount 0x20007d0` |
| `Feature_KVS_BugFixes_55830752__private_featureState` (`0x1d4f28`) | `≤P1a` | `…ReevaluateCachedFeatureEnabledState +0xa0` (칸 주소는 기술자 `0x1b7fb0 +0x0`) | `0x40147` |
| `LdrpInitOnceLoadAsDataCrits` (`0x1ce428`) | ~~`≤P1a`~~ → **`P1a~P1b`** | `LdrpInitMuiCrits +0x35` (0 → 1) · `+0x60 RtlRunOnceComplete` (→ 2) | P1a 0 → P1b 2 |
| `LoadAsDataCrits` (`0x1d41c0`) | `P1a~P1b` | `LdrpInitMuiCrits +0x4f RtlInitializeCriticalSectionEx(칸, 0, 0)` | `DebugInfo −1` · `LockCount −1` |
| `LdrpStatistics` (`0x1d2650`, 12B) | `P1a~P1b` | `LdrpUpdateStatistics +0xe` (일감 최대) · `+0x33` (일꾼) · `+0x2b` (그 밖) | `{3, 3, 3}` |
| `LdrpHotPatchInitCompleteEvent` (`0x1d2910`) | `≤P1a` | **syscall** — `LdrpInitializeInternal +0x8e NtCreateEvent(&칸, 0x1f0003, 0, 0, 0)` | 핸들 8 (nt 가 씀) |
| `RtlpHypervisorSharedUserVa` (`0x1d3e98`) | `≤P1a` | **syscall** — `LdrpInitializeProcess +0xb4d NtQuerySystemInformation(0xc5, …, 8)` → `+0xb5e` | `0x7ffe3000` (KUSD 뒤 3쪽, nt 가 올린 쪽) |

- `RtlCriticalSectionList` 의 첫 두 항목이 `RtlpProcessHeapsLock` · 프로세스 힙 락이라는 것은 값 증거 (`RtlpStaticDebugInfo +0x8 · +0x38`) 로 읽었다. 깃발 `0x10000000` 이 "디버그 정보 강제" 라는 뜻은 `[추정]` (결과서 — `RtlInitializeCriticalSectionEx` 본문 안 읽음). 다른 임계구역 (`LdrpEnclaveListLock` · `LoadAsDataCrits`) 은 `Flags 0` 이라 `DebugInfo = −1`.
- `RtlpTimeout` 의 값 주인은 nt (짐 지도 3.1 `+0xc0 CriticalSectionTimeout`, 단계 ①) — ntdll 은 복사만.
- `LdrpStatistics` `{3, 3, 3}` 은 주 스레드 갈래와 일꾼 갈래 (`LdrpThreadPool`, run69) 가 둘 다 탔다는 뜻 `[관찰 간접]`.

### 2. 0 을 씀 2 · 조건부 안 탐 16 — test.exe 가 왜 안 타나

- **0 을 씀** (`끝까지 0`): `LdrpEnforceIntegrityContinuity` (`+0x4db SETZ`) · `LdrpAuditIntegrityContinuity` (`+0x4fe MOV`) — `LdrSystemDllInitBlock +0xa8` (커널이 채운 `MitigationOptionsMap` 둘째 qword) `>> 4 & 3` 에서 계산, 무조건 실행. test.exe 입력 비트 0 → 둘 다 0. 읽는 쪽 `LdrpMapDllNtFileName +0x141 · +0x416` · `LdrpValidateIntegrityContinuity +0x90`.
- **조건부 안 탐** 16 (값은 모두 파일 처음 값 = 사진):

| 이름 (rva) | 사진 구간 (7.3) | 쓰는 줄 | 조건 — test.exe |
|---|---|---|---|
| `LdrpPolicyBits` (`0x1cd7fc`) | `≤P1a` (파일 `0x16`) | `LdrpInitializePolicy +0x7e` (`0x29`) · `+0x26b` · `+0x305` · `+0x31d` · `+0x329` | PEB `IsPackagedProcess` (`+0x46 TEST AL,0x10`) **그리고** `DllPath.Length ≠ 0` (`+0x72`) — test.exe `BitField 0x4` |
| `ShowSnaps` = `LdrpDebugFlags` (`0x1ccf60`) | `≤P1a` → 이름 칸은 **`끝까지 0`** | `LdrpInitializeProcess +0x59c` · `+0x754` (`OR … ,1`) | `PEB.NtGlobalFlag & 2` (FLG_SHOW_LDR_SNAPS) — test.exe 0 |
| `g_SxsMaxDeadActivationContexts` (`0x1ccf80`) | `≤P1a` (파일 `0xffffffff`) | `LdrpInitializeExecutionOptions +0x49c` | IFEO 부분 통째 건너뜀 (`+0xa6 TEST [PP+8],0x4000` · `+0xad JNZ`, `Flags 0x4001`) |
| `RtlpUnloadEventTraceExNumber` (`0x1cd7c4`) | `≤P1a` (파일 `0x40`) | `LdrpInitializeExecutionOptions +0x1fe` · `LdrpRecordUnloadEvent +0x1cd` | 같은 IFEO 건너뜀 / 모듈 언로드 때 |
| `LdrpQueryProcessModuleInformationLoopDetectorCount` (`0x1cd7c8`) | `≤P1a` (파일 `0x19000`) | `LdrpInitializeExecutionOptions +0x5fe` | 같은 IFEO 건너뜀 |
| `RtlpDisableHeapLookaside` (`0x1ce6e0`) | `≤P1a` → 이름 칸은 **`끝까지 0`** | `LdrpInitializeExecutionOptions +0x127` (칸 주소를 IFEO 읽기에 넘김) | 같은 IFEO 건너뜀 |
| `RtlpLargestLfhBlock` (`0x1cd0a8`) | `≤P1a` (파일 `0x4000`) | `RtlInitializeHeapManager +0x186` (`0x400`) | 스위트 마스크 비트 16 (`+0x17d JNC`) — test.exe `0x110` (sast/40) |
| `RtlpHeapFailureInfo` (`0x1cd0e0`) | `≤P1a` | `RtlpLogHeapFailure +0x21 ~` · `RtlpLocateRelatedBlocks` · `RtlpPrintErrorInformation` | 힙 오류를 보고할 때만 |
| `RtlpDebugPageHeapTable` (`0x1cc570`) | `≤P1a` | `AvrfMiniLoadDll +0xa4f` | `LdrpInitializeProcess +0x1568` `AvrfAppVerifierMode == 0` 이면 AppVerifier 묶음을 건너뜀 |
| `TppCleanupGroupMemberpNoPoolList` (`0x1cc738`) | `≤P1a` | `TppCleanupGroupMemberInitialize +0x284` (꼬리) | 멤버 풀 칸 `[+0x90]` 이 0 일 때만 (`+0x17d JZ`) — 풀이 붙는 멤버라 안 탐 `[정적]` |
| `LdrpUserDllDirectories` (`0x1cc7f8`) | `≤P1a` | `LdrAddDllDirectory +0x1b5` (← `LdrpInitializePerUserWindowsDirectory +0x17b`) | 위 간접 호출이 길이 0 → `+0x50` 끝 (사진 변화 없음으로 `[관찰 간접]`) |
| `RtlpHpHeapFeatures` (`0x1ce6d0`) | `끝까지 0` | `RtlInitializeHeapManager +0x31` · `RtlSetHeapInformation +0xb4` · `RtlpHpSetLowFragHeapGlobalFlags +0x7a · +0x92` | `RtlpHpOptIntoSegmentHeap` 반환 ≠ 0 (`+0x2f JZ`) — test.exe 는 NT 힙 |
| `g_RegInfo` (`0x1ce690`) | `끝까지 0` | `RtlInitializeHeapManager +0x5b` (`+0x8`) | 같은 갈래 |
| `RtlpHpAppCompatFlags` (`0x1ce8f8`) | `끝까지 0` | `RtlpHpSetLowFragHeapGlobalFlags +0x85` · `RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit +0xf` | 전역 힙 설정 길의 조건부 갈래 |
| `RtlpGlobalTagHeap` (`0x1ce6a8`) | `끝까지 0` | `RtlCreateTagHeap +0x97` (← `LdrpInitializeProcess +0xb78`) | `NtGlobalFlag & 0x800` (`+0x45 JZ`) |
| `g_pShimEngineModule` (`0x1d2980`) | `끝까지 0` | `LdrpInitShimEngine +0xf5` · `LdrpGetProcApphelpCheckModule +0x27e` · `LdrpGetShimEngineInterface +0x135` · `LdrpInitializeProcess +0x1cfe` (0) · `LdrInitShimEngineDynamic +0xdd` | shim 엔진을 올릴 때만 — `g_ShimsEnabled` 0 이라 `+0x1ca7 CMP · +0x1cae JZ` 로 `+0x1cfe` 도 건너뜀 |

- 나머지 넷 (`RtlpLowFragHeapGlobalFlags` · `RtlpLargestLfhBlock` · `RtlpDisableHeapLookaside` · `RtlpLowFragHeapRandomData`) 은 run46 의 "조건부 힙 전역 8" 중 위 표 · 1절에 이미 있다 → **run46 넘김 8개 모두 닫힘**.

### 3. 7.3 에 남는 36 (run114 가름 표시만)

- **아무도 안 씀 22** (결과서 "쓰는 줄 없음" — 이름 칸에 쓰는 명령이 ntdll 에 없음, 값은 파일 처음 값):
  - `.data ≤P1a` 19: `_pctype` · `_pwctype` · `__initiallocinfo` · `__lconv_c` · 스위치 표 항목 둘 (`g_SbTableEntry_VerifyVersionInfoSwitch_RTL` · `…HeapPaddingAndLFHSubsegmentCommitSwitch_RTL`) · `g_SbCachedModuleTable_RTL` · `LdrpSnapsLock_DEBUG` · `LdrpLoaderLock_DEBUG` · `LdrpDllNotificationLock_DEBUG` · `_matherr_flag` · `__mb_cur_max` · `g_sqmSessionHandle` · `Utf8TableInfo` · `RtlpUnloadEventTraceExSize` · `ModuleNamePlaceholderBuffer` · `SbDynamicContextDllCount*` (이름 칸 — `*` 는 옆 칸 `+0x4`) + **잠금 칸 2** `LdrpLoaderLock*` · `LdrpDllNotificationLock` (처음 값 = 파일, 공용 `RtlEnterCriticalSection` · `RtlLeaveCriticalSection` 이 `+0x8 ~ +0x10` 만 바꿈 — `LdrpLoaderLock` P1b 는 잡힌 상태).
  - `.mrdata` 2: `__guard_check_icall_fptr` · `__security_cookie_complement` (파일 값 `~0x2b992ddfa232` — `InitSecurityCookie` 는 `__security_cookie` 만 새로 써서 P1a 의 쿠키와 이 칸은 안 맞는다, 실패 보고 때만 쓰임).
  - `끝까지 0` 1 (잠금 칸): `LdrpModuleDatatableLock` (SRW 락 빠른 길 `lock cmpxchg` · `lock and` 수십 곳 `[추정]`, 사진 3장 풀린 상태 0).
- **범위 밖 길 14** `[추측]` (쓰는 함수가 지도 밖 — 공개 API · 오류 · 종료 길):
  - `.data ≤P1a` 11: `RtlpSecMemListHead` · `RtlpLockedSectionList` · `NormalizationListHead` · `LdrpSnapsLock` · `LdrpDllNotificationList` · `_control87` 의 `dazSupported` · `PossibleDeadlockReportThreshold` · `RtlpPtrTree` · `__ft_g_token` · `TelemetryEventThrottle` · `RtlpDiskSpeedPolicy` (`RtlpDiskSpeedInitialize` 는 직접 부르는 곳이 `_calls.tsv` 에 없음).
  - `.mrdata` 2: `NtUserPfn` (user32 `InitializeNtdllUserPfn` 만 — run85 실마리 그대로) · `LdrpVectorHandlerList` (VEH 공개 API — run82).
  - `.data P1a~P1b` 1: `RtlpSearchPathMode` (이름 칸은 `RtlSetSearchPathMode +0xe6` 뿐 — 사진 3장 0. **P1b 에 바뀐 건 옆 칸** `0x1d2358`).
- `.mrdata` 끝 페이지 둘 (`LdrpVectorHandlerList` · `__security_cookie_complement`) 의 `K1 이전` 라벨은 "커널이 썼다" 가 아니라 **파일에 원래 있던 값** 이라는 뜻이었다 (K1 = 파일 = P1a = P2).

### 4. 구간 라벨 정정 (이름 칸 vs 옆 칸) — 결과서 "구간 라벨 정정"

7.3 크기는 "다음 이름까지 거리" 짐작이라 이웃 이름 없는 칸이 이름에 묻힌다 (run46 이 먼저 본 꼴). 이름 칸 (코드 접근 폭) 으로 다시 보면:

| 이름 | 7.3 라벨 | 이름 칸 사진 | 라벨의 원인 (옆 칸) | 고친 것 |
|---|---|---|---|---|
| `ShowSnaps` | `≤P1a` | 끝까지 0 | `+0x10` (`0x1ccf70`) 파일 값 `0xffffffff` (`ShipAssert +0xa6 lock xadd` 카운터) | 3.8 줄 사진 칸에 `끝까지 0 (이름 칸)` |
| `RtlpDisableHeapLookaside` | `≤P1a` | 끝까지 0 | `+0x8` · `+0x10` (3.8 목록 밖 자리 — run46) | 같음 |
| `LdrpInitOnceLoadAsDataCrits` | `≤P1a` | P1a 0 → P1b 2 | `+0x8` (`0x1ce430`, IFEO 바탕 키 핸들 — run46) | 3.8 줄 구간 `P1a~P1b` |
| `RtlpSearchPathMode` | `P1a~P1b` | 끝까지 0 | `+0x8` (`0x1d2358`) P1b `0xb0` — 레지스트리 키 핸들 캐시 | 이름은 7.3 에 남기고 표시, 옆 칸은 3.8 목록 밖 자리 새 줄 |
| `SbDynamicContextDllCount` | `≤P1a` | 이름 칸 (`+0x0`) 파일 값 그대로 | `+0x4` 개수 칸 P1b 3 (5칸 × 8B 표) | 이름은 7.3 에 남김 (아무도 안 씀), 개수 칸은 3.8 목록 밖 자리 새 줄 |

- 7.3 · 2.1 의 **사진 구간 숫자는 안 고쳤다** (run66 `g_pShimmedModuleList` 선례 — 사진 분류는 짐작 크기 기준).

### 5. 목록 밖 자리 (옆 칸) 새 줄 셋 — 전역 수에 안 셈

- `0x1cd804` (`SbDynamicContextDllCount +0x4`, 칸마다 개수) ← `SbUpdateSwitchContextBasedOnDll +0xfb INC.LOCK` · `+0x2ba XADD.LOCK` (`②N…`, P1a~P1b 칸 0 개수 0 → 3).
- `0x1d2358` (`RtlpSearchPathMode +0x8`, `Session Manager` 키 핸들 캐시) ← `RtlpLookupCurDirSetting +0x5f NtOpenKey` → `+0x6e CMPXCHG.LOCK` (값은 nt 가 정한 핸들 번호, 사진 P1b `0xb0`). 사슬: `②… → LdrpDrainWorkQueue (+0x182) → LdrpProcessWork (+0x65) → LdrpMapDllSearchPath (+0x12c) → LdrpSearchPath (+0x36d) → LdrpComputeLazyDllPath (+0x62) → LdrpGetDllPath (+0xa8) → RtlpGetCachedPath (+0xbe) → RtlpComputeDllPath (+0x4b) → RtlpLookupCurDirSetting` (결과서 — `RtlpComputeDllPath` 부터 지도 밖).
- `0x1cc578` (`RtlpDebugPageHeapTable +0x8`) ← `LdrpInitializeProcess +0xcca MOV …,R15D` (앞 `+0xcc0 CMP · +0xcc8 JZ` 로 건너뛸 수 있음 — 조건 뜻은 안 봄) · AppVerifier `LdrpInitializeApplicationVerifierPackage +0x117 · +0x1d8`. 읽는 쪽 `RtlpCreateHeap +0x116`.
- 결과서가 같은 목록에 넣은 `0x1d272c` 는 **이름 있는 전역** `RtlFailedCriticalDebugAllocations` (`globals.tsv` — 7.3 `끝까지 0`) 라 목록 밖 자리로 안 셌다. `LdrpInitializeProcess +0x773 MOV dword [0x1d272c],R12D` (`R12` = 0) 는 0 을 쓰는 줄이다 `[정적]` — 오케스트레이터 셈 숫자 (0 을 씀 2) 에 없어서 **7.3 에 두고 7.5 실마리로** 적었다 (아래 "결과서와 다르게 적은 곳").

### 6. `EnvironmentVersion` 나머지 +2 (결과서 질문 3)

**답: ntdll `RtlpWow64ThunkEnvironmentTo64` 안 `RtlSetEnvironmentVar` 두 번** `[정적]`. 같은 `RtlpInitEnvironmentBlock` 호출 (`LdrpInitializeProcess +0xc2d`) 안, 사진 구간 `P1~P1a`.

| 순서 | 줄 | 하는 일 | `EnvironmentVersion` (옛 블록 `+0x3f8`) |
|---|---|---|---|
| ① | `RtlpInitEnvironmentBlock +0x86 INC` | 환경을 힙 블록으로 복사한 뒤 | 0 → 1 |
| ② | `RtlpInitEnvironmentBlock +0xa4` → `RtlpWow64ThunkEnvironmentTo64 +0x126 RtlSetEnvironmentVar` | 표 첫 줄: `ProgramFiles` ← `ProgramW6432` 값 (`+0xa5 RtlQueryEnvironmentVariable` 가 성공해야) | 1 → 2 |
| ③ | 같은 `+0x126` 둘째 바퀴 | `CommonProgramFiles` ← `CommonProgramW6432` | 2 → 3 |
| — | 셋째 바퀴 | `PROCESSOR_ARCHITECTURE` ← `PROCESSOR_ARCHITEW6432` — 그 변수는 WOW64 프로세스 환경에만 있어 test.exe 는 `+0xc4 JS` 로 건너뜀 | 그대로 |

- 표 `Wow64EnvironmentThunkTable` (`.rdata` `0x173b30`, 줄 `0x28`B × 3) 내용은 결과서가 파일에서 읽음.
- 지도 안에서 이 칸 (`[reg+0x3f8]`) 을 쓰는 줄은 `RtlpInitEnvironmentBlock +0x86` 과 `RtlSetEnvironmentVar` 둘뿐 (나머지 `RtlSetCurrentEnvironment` · `RtlSetEnvironmentStrings` 는 지도 밖 공개 API) → 사진 `0 → 3` 과 줄 수 (1 + 2) 가 맞는다 `[정적 + 사진 간접]`. `RtlSetEnvironmentVar` 안 올리는 줄은 갈래마다 한 번 `[정적, 갈래 전체는 안 따라감]`.
- 해시 표: `RtlSetEnvironmentVar` 가 `RtlpClearEnvironmentHashTable` 로 표를 비우고, 다음 `RtlQueryEnvironmentVariable` 이 `RtlpScanEnvironment` 로 다시 채운다 — 사진 P1a `{개수 47, 스캔됨 1}` 과 맞음 (마지막 조회 줄은 안 짚음).

### 7. LFH 켜기 (결과서 1e)

- `RtlpLowFragHeapGlobalFlags` 비트 0 과 `RtlpLowFragHeapRandomData` 32칸이 **사진 P1a 에 이미 있다** `[관찰]` run44. 쓰는 줄 사슬 `RtlpAllocateHeap +0xe5` (`[정적: objdump]`) → `RtlpPerformHeapMaintenance +0x22` → `RtlpActivateLowFragmentationHeap +0xde` → `RtlpCreateLowFragHeap +0x256` · `+0x25c` `[정적]`.
- 조건: 힙 깃발 비트 29 (`RtlpCreateHeap +0x1a3 BTS EDX,0x1d`, 생성 깃발 `& 0x80` 일 때) · `RtlpDisableHeapLookaside` 비트 0 꺼짐. 그러니 프로세스 힙 LFH 는 **`LdrpInitializeProcess` 중간 (P1~P1a) 의 어느 프로세스 힙 할당 안에서** 켜진다. 어느 할당인지는 `RtlAllocateHeap` 이 공용 부품이라 지도에서 끊겨 **동적** (아래 D1).

### 8. 간접 호출 (결과서 질문 4)

- 칸을 쓰는 줄이 간접 호출 대상 안인 곳 셋 — 모두 안 탐: `LdrpInitializePerUserWindowsDirectory +0x47` (`kernel32!TermsrvGetWindowsDirectoryW`, 길이 0 — 왜 0 인지는 kernel32 안, D3) · `RtlInitializeHeapManager +0x67` (`g_RegInfo+0x8` 함수, `+0x2f JZ` 로 건너뜀) · `Reevaluate +0xe0 · +0x106` (구독 포인터 둘 0 `[관찰]`).
- 그 밖 W 줄이 든 함수의 간접 호출은 이 칸들을 안 쓴다 (sast/109 목록 그대로).

## Q80 닫힘 (파일 값 대조 — 결과서 "Q80 에 넘길 판단")

| 결과 | `.data ≤P1a` 57 | `.mrdata` 4 | `.data P1a~P1b` 3 | 합 64 |
|---|---|---|---|---|
| P1a = P1b = P2 = 파일 값 | 39 | 4 | 1 | 44 |
| P1a = 파일 값, 뒤에 바뀜 | 3 (`LdrpLoaderLock` 잠금 · `SbDynamicContextDllCount` 개수 칸 · `LdrpInitOnceLoadAsDataCrits` 0 → 2) | 0 | 2 (`LdrpStatistics` · `LoadAsDataCrits` — 파일 0 = P1a 0) | 5 |
| P1a ≠ 파일 값 (코드가 씀) | 15 (② 13 · syscall 2) | 0 | 0 | 15 |

- **Q80 이 보는 `≤P1a` 61 (`.data` 57 + `.mrdata` 4) 중 46 = P1a 값 = 파일 값** (39 + 3 + 4) — 46 모두 코드 근거 (쓰는 줄 없음 · 조건부 · 범위 밖 · 잠금 · 옆 칸) 를 나란히 적었다. `LdrpInitOnceLoadAsDataCrits` 를 `P1a~P1b` 로 고치면 `≤P1a` 기준으론 45.
- **15 = 파일 ≠ P1a** → 쓰는 줄로 닫힘 (위 1절, 모두 줄을 찾음).
- Q80 원래 범위 (run44 때 `≤P1a` 전역 156 + `PebLdr` 5칸) 중 나머지는 앞 run 들 (run45 ~ run113) 이 쓰는 줄로 이미 닫았다 (`PebLdr` 는 run113). → **오케스트레이터 판단: Q80 닫힘 (run114)**. flow.md 4장 · 짐 지도 7.3 머리에 적었다.
- 하나 남는 것: 짐 지도 7.5 의 CFG 도우미 `LdrpGuardDispatchIcallNoESFptr` (`.mrdata` `≤P1a`, 이미 3.9 아는 칸) 는 "Q80 으로 가려진다" 고 적혀 있었는데 sast/111 대조 밖이다 (아래 "아직 확인 필요").

## 확인된 것

- `[정적]` 위 1절 18칸의 쓰는 줄 (결과서 + flow-writer `.asm`). 그중 값이 syscall 에서 오는 것 둘 — `NtCreateEvent` 는 칸에 직접 (nt), `NtQuerySystemInformation 0xc5` 는 지역 버퍼 → ntdll 이 옮김.
- `[정적]` `LdrpInitializeProcess +0x4db` · `+0x4fe` 는 무조건 바이트 쓰기 — `LdrpEnforce/AuditIntegrityContinuity` "0 을 씀".
- `[정적]` 조건부 안 탐 16칸의 쓰는 줄과 조건 (2절 표). `LdrpPolicyBits` 쓰는 줄 5개 (sast/120 (a) 가 다시 세어 같음).
- `[정적]` `EnvironmentVersion` 나머지 +2 = `RtlpInitEnvironmentBlock +0xa4 → RtlpWow64ThunkEnvironmentTo64 +0x126 RtlSetEnvironmentVar` ×2.
- `[정적]` LFH 켜는 줄 사슬 (`RtlpPerformHeapMaintenance +0x22 → RtlpActivateLowFragmentationHeap +0xde → RtlpCreateLowFragHeap +0x256 · +0x25c`). `[관찰]` run44 사진 P1a 에 이미 켜진 값.
- `[정적]` `LdrpDebugFlags` = `ShowSnaps` (같은 주소) — `+0x59c` · `+0x754` `OR`. 짐 지도의 `LdrpDebugFlags` 짐작 줄이 정확 줄을 얻었다.
- `[정적 + 사진]` 7.3 라벨 넷이 옆 칸 때문 (4절).

## 결과서와 다르게 적은 곳 · 보탠 것

- **셈 기준** (오케스트레이터): 결과서 "합치기 제안" 둘 중 큰 쪽 (조건부 안 탐도 3.8 로) 을 따랐다 — `.data` 모름 274 → 238. 결과서 숫자 "275 → 239" 는 run105 (`AvrfpLoaderEntry`) 전 기준이라 하나씩 다르다.
- **`LdrpDebugFlags` 짐작 → 정확** (flow-writer): 결과서는 `ShowSnaps` 만 셌다. 같은 주소의 다른 이름 `LdrpDebugFlags` 는 이미 3.8 짐작 줄이 있어 새 칸 수는 그대로, 정확 +1 · 짐작 −1.
- **`RtlFailedCriticalDebugAllocations`** (`0x1d272c`): 결과서는 "옆 칸 줄 (3.8 목록 밖 자리로)" 에 넣었지만 `globals.tsv` 에 이름이 있는 전역이다 (`RtlpTimeoutDisable` 짐작 크기 4 — 묻힌 칸이 아님). 쓰는 줄 `+0x773` 은 0 을 쓴다 `[정적]`. 오케스트레이터 셈 (0 을 씀 2) 에 없어서 옮기지 않고 7.5 실마리로 — **옮기면 0 을 씀 3 · 모름 267** (다음 요청 후보 · 결정).
- **경로 칸** (`LdrpInitOnceLoadAsDataCrits` · `LoadAsDataCrits`): 결과서 "정적 최단 길" `… LdrResSearchResource +0x727 → LdrAddLoadAsDataTable +0x37 → LdrpInitMuiCrits` 는 **아니다** (sast/113 — 그 길로 들어오면 반드시 써지는 `LoadAsDataTable*` 세 칸이 사진 0). 짐 지도엔 후보 셋 (`LdrpGetFileSizeFromLoadAsDataTable +0x14` · `LdrRemoveLoadAsDataTable +0x34` · `LdrpGetLoadAsEntry +0x2c`) 과 sast/113 이 고른 그럴듯한 최단 길 (`… LdrResSearchResource +0x20c → LdrpResGetMappingSize +0x1fc → LdrpGetFileSizeFromLoadAsDataTable +0x14 → LdrpInitMuiCrits` `[추정]`) 로 적었다.
- **syscall 둘의 쓴 주인**: 결과서는 `LdrpHotPatchInitCompleteEvent` · `RtlpHypervisorSharedUserVa` 둘 다 "② syscall 이 씀 — 쓴 주인 nt" 로 적었다. 짐 지도 규칙 (run59 — "syscall 결과를 지역 버퍼로 받아 옮기는 캐시 전역은 ntdll") 대로 `RtlpHypervisorSharedUserVa` 는 **ntdll** 로 셌다 (`+0xb56 MOV RAX,[RSP+0x168]` · `+0xb5e MOV [칸],RAX`). 칸 주소를 syscall 에 바로 넘기는 `LdrpHotPatchInitCompleteEvent` 만 nt. 가름 (값을 쓰는 줄 있음) 은 같다.
- **잠금 칸 셋** (`LdrpLoaderLock` · `LdrpDllNotificationLock` · `LdrpModuleDatatableLock`): 결과서는 "쓰는 줄 없음 (잠금 칸)" 으로 셌다. 셈 기준 ("아무도 안 씀 → 7.3 표시만") 대로 7.3 에 두었다. run60 · run69 는 잡고 푸는 락 (`LdrpInvertedFunctionTableSRWLock` · `TppPoolpListLock`) 을 3.8 로 옮겼는데, 그건 결과서가 "쓰는 줄" (잡고 풂) 로 셌기 때문이다 — 같은 꼴인데 다르게 셈 (아래 "아직 확인 필요").

## 정적과 동적이 다른 곳

- 없다. 72칸 모두 파일 값 · 쓰는 줄 · 사진 값이 맞는다.
- **디버거가 붙어서 달라지는 점**: 없음 — `ShowSnaps` 는 `NtGlobalFlag & 2` 로만 켜지고 KDNET 은 `NtGlobalFlag` 를 안 바꾼다 `[추측]`. 사용자 모드 디버거로 띄우면 힙 깃발 (`NtGlobalFlag` 힙 디버그 비트) 이 바뀌어 LFH 길이 달라질 수 있다 `[추측]`.

## 정정

- **run45** (`runs/03_sprint/run45-work-list.md`, 짐 지도 7.5 · 3.2 의 run45 줄): `EnvironmentVersion` 나머지 +2 를 "`RtlpInitEnvironmentBlock +0xa0 RtlpSetupEnvironmentHashTable` 쪽 `[추측]`" 으로 적었는데 **틀렸다**. 해시 표 함수가 아니라 `+0xa4 RtlpWow64ThunkEnvironmentTo64` 안 `RtlSetEnvironmentVar` 두 번 (`ProgramFiles` · `CommonProgramFiles`) 이다. → run45 문서 맨 위 한 줄. 짐 지도 7.5 · 3.2 · 7.8 은 `(해결됨 → run114)`.
- **run46** (`runs/03_sprint/run46-process-heap.md`, 짐 지도 7.5 run46 줄 · flow.md 4장 run46 줄): `RtlpLowFragHeapRandomData` (LFH 켜질 때) 를 "범위 밖", `RtlpLowFragHeapGlobalFlags` 비트 0 을 "블록 나눠 주기 쪽 — 7.6.1 범위 밖" 으로 적었는데 **틀렸다**. 두 칸 다 사진 P1a 에 이미 값이 있다 — LFH 는 `LdrpInitializeProcess` 가 끝나기 전 첫 프로세스 힙 할당들 중 하나 안에서 켜진다 (위 7절). → run46 문서 맨 위 한 줄.
- **짐 지도 7.3 라벨** 넷 (4절 표) — 3.8 줄 · 7.3 표시로 고침, 사진 숫자는 그대로.

## 이전 run 에서 닫은 것

- run46 "보고도 닫지 않은 것" 11 (짐 지도 7.3 run46 줄 · 7.5) → 모두 가름: **3.8 로 10** (`RtlpLowFragHeapGlobalFlags` · `RtlpLowFragHeapRandomData` — 쓰는 줄 / `RtlpDisableHeapLookaside` · `RtlpLargestLfhBlock` · `RtlpHeapFailureInfo` · `RtlpDebugPageHeapTable` · `RtlpHpHeapFeatures` · `RtlpHpAppCompatFlags` · `RtlpGlobalTagHeap` · `g_RegInfo` — 조건부 안 탐) · **7.3 에 1** (`RtlpSecMemListHead` — 범위 밖).
- run59 (짐 지도 7.5) `LdrpEnforceIntegrityContinuity` · `LdrpAuditIntegrityContinuity` "저장 줄 오프셋 · 칸 셈은 Q78e2 몫" → `+0x4db` · `+0x4fe`, 0 을 씀.
- run65 정적 ① (짐 지도 7.24 · flow.md 4장) `LdrpPolicyBits` 를 3.8 로 옮길지 → 옮김 (조건부 안 탐).
- run66 실마리 (짐 지도 7.5) `LdrpQueryProcessModuleInformationLoopDetectorCount` 이름 칸 `0x19000` 이 자기 값일 수 → 맞다 (파일 값, 쓰는 줄 `+0x5fe` 조건부).
- run85 실마리 `NtUserPfn` → 파일 처음 값 그대로 · 범위 밖 (7.3 표시).
- run113 정적 ③ "Q78e2 에 얹을 전역" 넷 → 0 을 씀 2 · 잠금 1 · 조건부 1.
- run45 (짐 지도 2.1 · 7.8) "`EnvironmentVersion` 나머지 +2" → 위 6절.
- Q80 → 닫힘 (위 절).

## 아직 확인 필요

### 동적 (결과서 "동적으로 확인할 것" 1 ~ 6 — 급하지 않음, Q104 VM 단계에 얹을 것. 대기열 번호는 오케스트레이터가 붙임, 멈춤점은 test.exe 조건)

- **D1** LFH 가 켜지는 첫 `RtlAllocateHeap`: `ntdll!RtlpPerformHeapMaintenance` 입구 (rva `0x5cc30`) 첫 도착 `k` — `RtlpAllocateHeap` 위가 `LdrpInitializeProcess` 의 어느 `+0x…` 인지. 이어 `RtlpCreateLowFragHeap +0x256` 뒤 `dd ntdll!RtlpLowFragHeapGlobalFlags L1` = 1.
- **D2** `ntdll!LdrpInitMuiCrits` 입구 (rva `0x479f0`) 첫 도착 `k` — 후보 셋 (`LdrpGetFileSizeFromLoadAsDataTable +0x14` / `LdrRemoveLoadAsDataTable +0x34` / `LdrpGetLoadAsEntry +0x2c`, sast/113 이 `LdrAddLoadAsDataTable +0x37` 을 뺌) 중 어느 것.
- **D3** `bp ntdll!LdrpInitializePerUserWindowsDirectory+0x47` 에서 `ln @rax` (= `kernel32!TermsrvGetWindowsDirectoryW`) · `+0x4c` 에서 `r eax` = 0. 왜 0 인지 (터미널 서버 아님 `[추측]`) 는 kernel32 안.
- **D4** `EnvironmentVersion` 단계: `ntdll!RtlpInitEnvironmentBlock+0x8d` 에서 `dq @rbx+0x3f8 L1` = 1, `ntdll!RtlpWow64ThunkEnvironmentTo64+0x12b` 를 지날 때마다 2 → 3, `+0xaa` 에서 `r eax` ≥ 0 (`ProgramW6432` 있음).
- D5 (선택) `…ReevaluateCachedFeatureEnabledState+0xa0` 에서 `rcx`/`r15` = `ntdll+0x1d4f28` 인 호출 (DisallowSharedLock 평가 1 + KVS 평가 1).
- D6 (선택, 읽기만) `ntdll!LdrpPolicyBits` `0x16` 그대로 — 새 멈춤점 없음.

### 정적 (결과서 "다음 요청 후보" + flow-writer)

- ① **Q78g2** (이어서): `끝까지 0` 전역 중 이름 칸에 쓰는 줄이 지도 안 (`m3 ~ m10`) 에 있는 것 — `.data` 39 · `.mrdata` 14 (shim 엔진 인터페이스 포인터 9 · AppVerifier 4 + 1). "조건부 안 탐 / 0 을 씀" 후보. 그중 init 줄기 직접 줄: `RtlpForceCSToUseEvents` (`LdrpInitializeProcess +0x9bb`) · `LdrpForkActiveLock` · `LdrpForkConditionVariable` (`_LdrpInitialize +0x336 · +0x33d`) · `LdrpRedirectionModule` (`LdrpInitializeImportRedirection +0x18f`) · **`RtlFailedCriticalDebugAllocations`** (`+0x773`, 0 을 씀 — 이번에 옮길지 결정 필요).
- ② **셈 결정 (사용자)**: 전역에 "아무도 안 씀 · 범위 밖" 가름 열을 둘지 (sast/111 · 113 의 A/B/C 안). 지금은 7.3 에 표시만 — 그래서 7.3 모름 268 에 "가름 끝났지만 모름으로 남은" 36 이 들어 있다.
- ③ **셈 일관성 (오케스트레이터)**: 잠금 칸 (`LdrpLoaderLock` · `LdrpDllNotificationLock` · `LdrpModuleDatatableLock`) 을 run60 · run69 처럼 3.8 "잡고 풂" 줄로 셀지, 이번처럼 "아무도 안 씀 (잠금)" 으로 둘지.
- ④ (작음) `LdrResSearchResource +0x6e3 ~ +0x727` 조건 (파일 경로로 찾는 갈래) — D2 를 못 하면 정적으로 첫 줄을 좁힐 수 있음. 값은 이미 닫힘.
- ⑤ (작음) 짐 지도 7.5 CFG 도우미 `LdrpGuardDispatchIcallNoESFptr` (`.mrdata` `≤P1a`) 의 P1a 값 (`_guard_dispatch_icall` = ntdll+`0x166480`) 이 파일 값인지 — sast/111 대조 밖. Q80 을 닫으며 남는 마지막 "파일 값?" 한 칸.
- ⑥ (작음) `LdrpInitializeProcess +0xcc0 · +0xcc8` 조건 (`0x1cc578` ← 0 을 건너뛰는지).

### PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- `EnvironmentVersion`: **틀림 (알고 있는 차이) — 우리 1, 진짜 3.** 우리 환경 표에 `ProgramW6432` · `CommonProgramW6432` 가 없어 진짜 `RtlpWow64ThunkEnvironmentTo64` 몸통이 `RtlQueryEnvironmentVariable` 실패로 건너뛴다. 두 변수를 넣으면 `+0x126` 이 두 번 불려 3 이 될 것 `[정적 추정]` (PE_emulator 일 55 "다시 볼 것" 과 같은 얘기).
- `RtlpLowFragHeapGlobalFlags`: 우리 시험 기대값 0 은 LFH 켜기 **앞** 단계라 맞지만, 사진 P1a `1` 은 켜기 뒤 값. 프로세스 힙 할당 길 (LFH 켜기) 을 넣으면 `RtlpLowFragHeapRandomData` 32칸도 나온다 (난수라 사진과 같을 필요 없음).
- `LdrpThunkSignature` · `RtlpTimeout` · `RtlpTimeoutDisable` · `LdrpEnforce/AuditIntegrityContinuity`: **맞음** (결과서 — `LdrpInitializeProcess` 몸통이 진짜 코드).
- `LdrpPolicyBits`: **맞음** (빈 함수 + 파일 값 `0x16`).
- `RtlpEnvironmentHashTable` · `LdrpEnclave*` · `Feature_KVS…` · `RtlpHypervisorSharedUserVa` (우리 `NtQuerySystemInformation 0xc5` 반환에 달림 — 실패면 `+0xb54 JS` 로 안 씀) · `LdrpHotPatchInitCompleteEvent` · `LdrpStatistics` · `LoadAsDataCrits` · `TermsrvGetWindowsDirectoryW` 흉내: **확인 필요**.
- "파일 처음 값 그대로" 46칸: ntdll `.data` 를 구역 원본 `0x2000` 바이트 + 0 채움으로 올리고 재배치만 맞으면 코드 없이 같다 — **확인 필요** (rva `≥ 0x1ce000` 은 파일 값 0).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntkrnlmp · ntdll pdb 줄에 run114, 근거 로그 run114 줄), 2.4 머리 run 목록 · `LdrpInitializeInternal` 절 (`LdrpHotPatchInitCompleteEvent`) · `LdrpInitializeProcess` 묶음 8 줄 (`EnvironmentVersion` +2) · 프로세스 힙 절 두 줄 (LFH 켜기 — run46 정정 · 임계구역 디버그 정보 첫 둘) · 기능 상태 절 한 줄 (묶음 7 힙 뒤 넷), 4장 run45 `EnvironmentVersion` 줄 · run46 조건부 힙 줄 · run65 정적 ① · run113 정적 ③ `(해결됨 → run114)` + run45 CFG 줄 메모 + 새 줄 넷 (해결됨 · 동적 D1 ~ D6 · 정적 ① ~ ⑥ · PE_emulator), 5장 두 표. 그림은 안 고침.
- 짐 지도 (`baggage.md`): 기준 run114 줄 · 판 줄 · 1장 (`.data` 줄 + run114 메모) · 2장 표 두 줄 (`.data` · 전역 합) · 2장 아래 전역 줄 · 2.1 아래 (Q80 닫힘 · 전역 모름 나눔 · 구간 정해진 모름 3 → 1 · run45 남은 것) · 3장 머리 run114 줄 · **3.2 새 줄 1 + 고친 줄 1** · **3.8 머리 run114 줄 · 새 줄 36 (전역 33줄 — 36칸 · 목록 밖 자리 3) + 고친 줄 1 (`LdrpDebugFlags`)** · 3.9 머리 메모 (`.mrdata` 넷 가름) · 4장 짝 고친 줄 1 (이벤트 핸들) · 새 줄 1 (`RtlpHypervisorSharedUserVa`) · 5장 ② · 7장 머리 · 7.1 (전역 끝까지 0 모름 237 → 229 — run105 숫자 안 고쳐진 것 바로잡음 · 아는 끝까지 0 60 → 67) · 7.3 (머리 Q80 · 가름 표시 · 라벨 정정, `.data` 목록 · `.mrdata` 목록 표시, 해결됨 줄, run46 줄) · 7.5 일곱 줄 `(해결됨 → run114)` + 새 실마리 1 · 7.8 · 7.24 ① · 7.44 ③ · 새 7.45 · 8장.
- 이전 run 문서 맨 위 정정 한 줄: run45 · run46.

## 디버거 메모 / 도구 메모

- 결과서 제안 (하네스, 직접 안 고침): ① `scripts/refs_many.sh` — 이름 목록 파일을 받아 이름 칸 `[주소, 주소 + 폭)` 의 쓰기 · 읽기 · `lea` 줄과 지도 깊이를 TSV 한 장으로 (레지스터 간접 쓰기는 못 봄 표시) ② `globals.tsv` 에 **코드 접근 폭** 열 (첫 접근의 `BYTE/WORD/DWORD/QWORD PTR`) — 이번 라벨 정정 넷 같은 오분류를 줄임 ③ (사소) `.asm` 전역 주석 "이웃 이름 + 칸" 이 다른 변수일 수 있음 (`NormalizationListHead+0x68` = `0x1cc710` 이름 없는 칸).
- 짐 지도 메모 (flow-writer): 같은 주소에 PDB 이름이 둘인 전역 (`LdrpDebugFlags` · `ShowSnaps`) 은 `globals.tsv` 에 두 줄로 들어가 전역 수 438 에 둘 다 센다. 한쪽만 3장에 있으면 다른 쪽이 7.3 에 "모름" 으로 남는다 — 다른 짝이 더 있는지 한 번 훑을 만하다 (다음 요청 후보 아님, 도구 메모).

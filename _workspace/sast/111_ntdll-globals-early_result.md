# 111 ntdll-globals-early — 정적 분석 결과 (Q78e2 + Q78f: ntdll 전역 `≤P1a` · `.mrdata` · `P1a~P1b` · 조건부 힙 전역 · `EnvironmentVersion` +2)

- 요청서: `_workspace/sast/111_ntdll-globals-early.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), PDB guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt` — 사진 로그 `lm` 줄 (`ntdll.pdb\C3093720177DA6851519DCFFE8D…`) 과 같음). 커널 `ntkrnlmp.pdb` `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) = 기준값과 같음. **ntoskrnl Ghidra 프로젝트는 열지 않았다** (sast/110 이 도는 중).
- 분석: 이미 돼 있어서 다시 안 걸었다 (ntdll `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄). 내가 시작할 때 도는 Ghidra 프로세스 없음 (`pgrep -fa 'ghidra.app.util.headless'` 0줄). 내 `export` 두 번 (ntdll 프로젝트, 각 4초, exit 0) 말고 안 돌렸다.
- **새로 뽑은 함수 17개, 전부 depth 0** (`run_ghidra.sh export ntdll.dll '^(…)$' 0`, 두 번):
  - 첫 번 (환경·힙): `RtlpWow64ThunkEnvironmentTo64` · `RtlSetEnvironmentVar` · `RtlpSetupEnvironmentHashTable` · `RtlpScanEnvironment` · `RtlpInitializeLfhRandomDataArray` · `RtlpActivateLowFragmentationHeap`.
  - 둘째 번 (P1a~P1b·조건 갈래): `LdrpInitMuiCrits` · `LdrAddLoadAsDataTable` · `LdrpResMapFile` · `RtlpPerformHeapMaintenance` · `RtlpClearEnvironmentHashTable` · `RtlpAllocateEnvironmentHashTable` · `LdrpInitializePerUserWindowsDirectory` · `LdrpUpdateStatistics` · `AvrfMiniLoadDll` · `RtlpDiskSpeedInitialize` · `RtlSetSearchPathMode`.
  - **내 실수 하나**: 첫 번 정규식에 이미 `.asm` 이 있던 `RtlSetHeapInformation` 이 끼어 같은 함수를 한 번 더 썼다 (내용은 sast/40 이 뽑은 것과 같은 함수·같은 오프셋 — 다른 파일은 안 건드림). 이유 없이 다시 뽑지 말라는 요청서 규칙을 어긴 것이라 적어 둔다.
- 이미 있어서 **읽기만 한 것** (다시 안 뽑음): `LdrpInitializeProcess` · `LdrpInitializeInternal` · `RtlInitializeHeapManager` · `RtlpCreateLowFragHeap` · `RtlpCreateHeap` · `RtlpInitEnvironmentBlock` · `LdrpInitializePolicy` · `LdrpCaptureCriticalThunks` · `RtlpAddDebugInfoToCriticalSection` · `RtlpLookupCurDirSetting` · `TppCleanupGroupMemberInitialize` · `LdrAddDllDirectory` · `LdrpInitializeExecutionOptions` · `SbUpdateSwitchContextBasedOnDll` · `LdrResSearchResource` · `LdrpFindDllActivationContext` · `LdrpInitializeKernel32Functions` · `wil_details_AreDependenciesEnabled` · `wil_details_FeatureStateCache_ReevaluateCachedFeatureEnabledState` 등 (`.asm`), 그리고 `_map/LdrInitializeThunk_{nodes,edges}.tsv`.
- **훑기 방법 + 한계 (한 줄씩)**
  - 칸 주소로 가는 모든 명령: 스크래치 objdump 풀이 `ntdll.dis` (421,449줄) 의 `# 0x…` 주석 (RIP 상대 목적지) 으로 이름 칸 `[주소, 주소+폭)` 을 훑어 쓰기·읽기·`lea` 로 가르고 (쓰는 함수 + 함수 안 오프셋, 지도 깊이 `m0~m10` / `out` 표시), 쓰기 모양 (`mov [칸],…` · `or/and/add/inc/xadd/cmpxchg [칸]`) 은 그 자리 명령으로 판단. 한계: **레지스터에 담은 포인터로 쓰는 줄 (`[RAX]` 꼴) 은 안 잡힌다** — 그런 갈래는 그 함수 `.asm` 을 읽어 보탰다 (`RtlpAddDebugInfoToCriticalSection +0x353` 등). `run_ghidra.sh refs` 는 이름 하나씩이라 이름 70여 개엔 느려서 안 썼다 (결과는 같은 `.asm` 줄로 대조).
  - 칸 크기: PDB 에 크기가 없다. 7.3 의 크기는 "다음 이름까지 거리" 짐작이라 **이웃 이름 없는 칸이 섞인다.** 그래서 이 결과서는 **이름 칸 = 코드가 그 주소로 접근하는 폭 (바이트 · 워드 · 더블워드 · 쿼드워드 · 구조체 머리 `_LIST_ENTRY` · 임계구역 `0x28`B)** 으로 잡고, 7.3 의 구간 라벨이 옆 칸 때문에 붙은 곳은 따로 적었다 (표 앞 "구간 라벨 정정").
  - 파일 값: `ntdll.dll` 구역 원본 바이트. `.data` 는 원본이 `0x2000` 바이트뿐이라 그 너머 (rva `0x1ce000` ~) 는 파일 값 0 (구역 가상 크기가 더 큼). 포인터는 로더가 `.reloc` 로 고치므로 파일 `0x180xxxxxx` 를 `0x7ffafb66…` 로 바꿔 사진과 견줬다 (표에는 `ntdll+0x…`). 사진 값: `ms/logs/04_baggage-read_{P1a,P1b,P2}.txt` 의 `.data` 덤프 (`dq 7ffafb82c000 L0x11eb`) · `.mrdata` 덤프 (`dq 7ffafb847000 L0x6b7`). **`.data` 와 `.mrdata` 앞 3페이지는 K1~P1 읽기 불가** (7.3) → `.data` 는 P1a 부터만 값이 있다. ntdll 시작 `0x7ffafb660000` 은 사진 8장 모두 같다.
- **버전 확인: 일치.** 알려진 사실 `_calls.tsv` 대조 (읽은 줄 그대로): `LdrInitializeThunk +0x9 / +0xe → LdrpInitialize` · `LdrpInitializeInternal +0x55 / +0x5a → _LdrpInitialize` · `_LdrpInitialize +0x167 / +0x16c → LdrpInitializeProcess` · `LdrpInitializeProcess +0x1bb6 / +0x1bbb → LdrpInitializeGraphRecurse` (옆에 `+0x16e6` · `+0x193c` 둘 더) · `LdrpInitializeNode +0x166 / +0x16b → LdrpCallTlsInitializers` · `+0x197 / +0x19c → LdrpCallInitRoutine` · `LdrpLoadDll +0xfb / +0x100 → LdrpLoadDllInternal`. 커널 줄 (`NtCreateUserProcess`·`PspInsertThread`) 은 ntoskrnl 쪽이라 안 열었다 (커널 PDB GUID 만 확인).
- **오프셋 대조**: 아래 `+0x` 는 `.asm` 줄 (또는 같은 풀이의 같은 함수 시작 기준 오프셋) 에서 다시 찾아 맞춰 봤다 — 맨 끝 "오프셋 대조" 절.
- 약속: `call +0x..` = CALL 자리, `ret +0x..` = 다음 줄 (콜스택 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리. 점프 목적지는 (목적지 절대 주소 − 함수 시작) 으로 셌다.

## 스크립트 문제

없다. `run_ghidra.sh export` 두 번 문제없이 돌았다 (`_calls.tsv` 4,192 → 4,328줄, sast/109 앞 백업 대비 줄어든 줄 `comm -23` 0). 임시 스크립트는 전부 스크래치 (`…/scratchpad/s111/`, 레포 밖). 제안은 맨 아래.

## 판단 기준 (한 줄)

sast/46·47·48·109 와 같다. **길** = ntdll 지도 (`_map/LdrInitializeThunk_nodes.tsv`, 1,248개 — 이 결과서 표의 `m0~m10` 이 그 깊이, `out` 은 지도 밖. 지도는 공용 도구 (caller 20곳 이상) 를 안 펼친다). 이름 칸에 쓰는 줄이 있고 값이 파일과 다르면 "② 쓰는 줄 있음", 값을 syscall 이 써 주면 "② syscall 이 씀", 쓰는 줄이 길 안 함수에 있는데 test.exe 가 조건을 안 채우면 "조건부 안 탐 + 조건", 쓰는 줄이 지도 밖 함수 (공개 API · 오류·종료 길) 에만 있으면 "범위 밖 길에서 씀 `[추측]`", 코드가 이름 칸에 쓰는 명령이 아예 없으면 "쓰는 줄 없음". 앞의 셋 (조건부 · 범위 밖 · 쓰는 줄 없음) 은 사진 값 = 파일 값일 때 **"파일 처음 값 그대로"** 로 가른다 (요청서 1번). 락 칸 (임계구역 · SRW) 은 처음 값이 파일 그대로고 공용 락 함수가 `+0x8 ~ +0x10` 만 바꾸므로 "쓰는 줄 없음 (잠금 칸)" 으로 센다.

## 한눈에

- **64개 가름 (7.3 `.data ≤P1a` 57 + `.mrdata` 4 + `.data P1a~P1b` 3)**: **이 길이 값을 씀 18** (② 쓰는 줄 16 · ② syscall 이 씀 2) · **파일 처음 값 그대로 46** (쓰는 줄 없음 21 (잠금 칸 2 포함) · 조건부 안 탐 11 · 범위 밖 길 14) · 0 을 씀 0 · 모름 0. 합 18 + 46 = 64.
- 그 밖 (요청서 "그 밖") 8개 (+ `LdrpHeap` 확인 1): 조건부 힙 전역 8 중 새로 닫은 4 (`RtlpHpHeapFeatures` · `RtlpHpAppCompatFlags` · `RtlpGlobalTagHeap` · `g_RegInfo` — 모두 조건부 안 탐, 나머지 넷은 위 64 안) · sast/109 가 넘긴 4 (`LdrpAuditIntegrityContinuity` · `LdrpEnforceIntegrityContinuity` = **0 을 씀 (입력값)**, `LdrpModuleDatatableLock` = 잠금 칸, `g_pShimEngineModule` = 조건부 안 탐) + `LdrpHeap` (이미 3.8 에 있음 — 한 줄 확인만). 묶음 7 힙 뒤 부분 (`+0xa50 ~ +0xb7d`) 은 아래 1g. `EnvironmentVersion` +2 는 질문 3.
- **7.3 `≤P1a` 라벨 중 일부는 이름 칸이 아니라 이웃 칸 때문이었다**: 이름 칸이 사진 P1a 에서 0 인 것 `ShowSnaps` · `RtlpDisableHeapLookaside` (끝까지 0) · `LdrpInitOnceLoadAsDataCrits` (P1a 0 → P1b 2, 라벨은 `≤P1a` 가 아니라 `P1a~P1b` 가 맞다), `P1a~P1b` 로 적힌 `RtlpSearchPathMode` 는 이름 칸이 끝까지 0 (옆 칸 `0x1d2358` 이 P1b 에 `0xb0`). 자세히는 "구간 라벨 정정".
- **값의 주인이 정해진 것 (새로 알게 된 것)**:
  - `LdrpThunkSignature` = 시스템 호출 스텁 5개 (`ZwOpenFile` `0x33` · `ZwCreateSection` `0x4a` · `ZwQueryAttributesFile` `0x3d` · `ZwOpenSection` `0x37` · `ZwMapViewOfSection` `0x28`) 의 **앞 16바이트 복사본** — 사진 P1a 80바이트와 바이트 단위로 같다 (`LdrpCaptureCriticalThunks +0x95`).
  - `RtlpTimeout` = PEB `+0xc0 CriticalSectionTimeout` 복사 (사진 `0xffffe86d079b8000` = −30일, PEB 와 같은 값) · `RtlpTimeoutDisable` = 그 값이 −1시간보다 작으면 1.
  - **LFH 켜기 (`RtlpLowFragHeapGlobalFlags` 비트 0 · `RtlpLowFragHeapRandomData` 32칸) 는 사진 P1a 에 이미 일어나 있다.** sast/40 이 "LFH 는 나중에 켜짐 — 범위 밖" 이라 넘긴 줄은 `RtlpAllocateHeap +0xe5` → `RtlpPerformHeapMaintenance +0x22` → `RtlpActivateLowFragmentationHeap +0xde` → `RtlpCreateLowFragHeap` 로 정적 길이 이어졌다 (어느 `RtlAllocateHeap` 호출이 처음 타는지는 동적).
  - **`EnvironmentVersion` +2 = `RtlpInitEnvironmentBlock +0xa4` 가 부르는 `RtlpWow64ThunkEnvironmentTo64` 안 `RtlSetEnvironmentVar` 두 번** (`ProgramFiles` ← `ProgramW6432` · `CommonProgramFiles` ← `CommonProgramW6432`, 환경에 그 둘이 있을 때) — 질문 3.
  - **`LdrpPolicyBits`: 3.8 로 옮기자** (쓰는 줄 5개 모두 패키지 앱 갈래, 조건부 안 탐, 파일 `0x16` 그대로) — 질문 2.
  - `Feature_KVS_BugFixes_55830752__private_featureState` 는 **`DisallowSharedLockImplicitUpgrade` 의 "필요 기능"** 으로 같이 평가되며 써진다 (기술자 `+0x20 requiresFeatures`) — 이 기능에는 `IsEnabled…` 함수가 ntdll 에 아예 없다.
  - `LdrpHotPatchInitCompleteEvent` (핸들 8) · `RtlpHypervisorSharedUserVa` (`0x7ffe3000`) 는 값을 **nt 가 syscall 로 써 준다**.
  - `SbDynamicContextDllCount` 는 5칸 `{문맥 번호, 개수}` 표 (칸당 8B): 이름 칸 `+0x0` 은 파일 값, 개수 칸 `+0x4` 가 P1a~P1b 에 `INC.LOCK` 으로 0 → 3.
- **Q80 (파일 값 대조) 에 넘길 수**: Q80 이 보는 `≤P1a` 61개 (`.data` 57 + `.mrdata` 4) 중 **46개는 P1a 값 = 파일 값** (재배치 뒤; `.data` 42 + `.mrdata` 4), 나머지 15 (② 13 · syscall 2) 는 파일 ≠ P1a 라 쓰는 줄로 닫혔다. (`P1a~P1b` 3 중 1은 끝까지 파일 0, 2는 P1b 에 처음 생김.) 아래 "Q80 에 넘길 판단".
- **Q78g2 로 넘길 것**: `끝까지 0` 전역 215 + 22 중 **이름 칸에 쓰는 줄이 지도 안 (깊이 `m3~m10`) 에 있는 것 53개** (`.data` 39 + `.mrdata` 14, 이미 닫은 8개 빼고) — "다음 요청 후보" 표. 사진은 모두 0 이라 "조건부 안 탐 / 0 을 씀" 으로 갈릴 후보다.
- 정적으로 못 닫은 것: LFH 켜지는 정확한 `RtlAllocateHeap` 호출 (동적 1), `LdrpInitMuiCrits` 첫 입구 줄 (동적 2), `TermsrvGetWindowsDirectoryW` 가 길이 0 을 돌려주는 이유 (동적 3, kernel32).

## 요약 표

| 묶음 | 칸 | ② 쓰는 줄 | ② syscall | 파일값 그대로 · 쓰는 줄 없음 (잠금 포함) | 조건부 안 탐 | 범위 밖 길 | 0 을 씀 |
|---|---|---|---|---|---|---|---|
| `.data ≤P1a` | 57 | 14 | 2 | 19 (잠금 2) | 11 | 11 | 0 |
| `.mrdata ≤P1a` 2 + `K1 이전` 2 | 4 | 0 | 0 | 2 | 0 | 2 | 0 |
| `.data P1a~P1b` (Q78f) | 3 | 2 | 0 | 0 | 0 | 1 | 0 |
| **합** | **64** | **16** | **2** | **21** | **11** | **14** | **0** |
| 그 밖 · 조건부 힙 새 4 | 4 | 0 | 0 | 0 | 4 | 0 | 0 |
| 그 밖 · sast/109 넘김 4 | 4 | 0 | 0 | 1 (잠금) | 1 | 0 | 2 |

- 합 확인: 14 + 2 + 19 + 11 + 11 = 57 · 0 + 0 + 2 + 0 + 2 = 4 · 2 + 0 + 0 + 0 + 1 = 3 → 64.
- 조건부 힙 전역 8개 (run46 넘김) 이름: `RtlpHpHeapFeatures` · `RtlpHpAppCompatFlags` · `RtlpGlobalTagHeap` · `g_RegInfo` (`+0x8`) 는 새로 닫은 4 (아래 표 "조건부 힙"), `RtlpLowFragHeapGlobalFlags` · `RtlpLargestLfhBlock` · `RtlpDisableHeapLookaside` · `RtlpLowFragHeapRandomData` 는 위 57 안 (sast/40 결과서가 7.5 에 적은 목록 그대로).
- 합치기 제안 (3.8 · 2장 숫자는 flow-writer 몫): `.data` 모름 275 → **257** (아는 칸 +18: ② 16 · syscall 2) 로 줄이거나, 조건부 안 탐 11 + 4 + 1 과 0 을 씀 2 도 3.8 줄로 옮기면 **모름 275 → 239**. `.mrdata` 모름 26 → 22 (4개 모두 쓰는 줄 없음 / 범위 밖 — 3.9 에 한 줄 묶음으로 옮기면). 옮길지는 오케스트레이터 결정 (7.24 ① 이 같은 질문).

## 질문별 답 [정적]

### 1. 전역마다 가름 (표 + 풀이)

**표 읽는 법**: 이름 `(0x…)` = rva, `N B 봄` = 이 표가 보여 주는 이름 칸 폭. `가름` 은 "판단 기준" 의 말. `쓰는 줄` = 함수 +명령 오프셋 (여러 줄이면 다 적음; "쓴 값"). `파일 값` = 구역 원본 (재배치 뒤 `ntdll+0x…`). `사진 값` = P1a·P1b·P2 (`= 파일` 이면 세 장 모두 파일 값과 같음). `경로 · 조건` 의 `②…` = `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess` (baggage 3장 줄임), `②N…` = `②… → LdrpInitializeGraphRecurse → LdrpInitializeNode`.

#### 구간 라벨 정정 (이름 칸 vs 옆 칸)

| 이름 | 7.3 라벨 | 이름 칸 (사진) | 라벨의 원인 | 정정 |
|---|---|---|---|---|
| `ShowSnaps` | `≤P1a` | P1a·P1b·P2 모두 0, 파일 0 | 짐작 크기 24B 안 `+0x10` (`0x1ccf70`) = `0xffffffff` (파일 값; `ShipAssert +0xa6 lock xadd` 가 만지는 카운터) | `끝까지 0` (쓰는 줄은 조건부 안 탐 — `NtGlobalFlag & 2`) |
| `RtlpDisableHeapLookaside` | `≤P1a` | P1a·P1b·P2 모두 0, 파일 0 | `+0x8` (`0x1ce6e8`) = `0x7ffffffeffff` · `+0x10` = `0x10000` (sast/40 이 이미 찾은 목록 밖 자리 둘, `RtlpCreateHeap +0xe1c · +0xdeb`) | `끝까지 0` |
| `LdrpInitOnceLoadAsDataCrits` | `≤P1a` | P1a 0 → P1b·P2 2 (`P1a~P1b`) | `+0x8` (`0x1ce430`) = `0x10` (IFEO 바탕 키 핸들, sast/40) | `P1a~P1b` |
| `RtlpSearchPathMode` | `P1a~P1b` | 끝까지 0 (`+0x0` 4B) | `+0x8` (`0x1d2358`) 이 P1b 에 `0xb0` (레지스트리 키 핸들 캐시) | 이름 칸은 `끝까지 0`, 옆 칸 줄을 3.8 "목록 밖 자리" 로 |
| `SbDynamicContextDllCount` | `≤P1a` | 이름 칸 (`+0x0` 문맥 번호) 은 파일 값 그대로, `+0x4` 개수 칸이 P1b 에 3 | 파일 값 (`+0x8 = 1`, `+0x10 = 2` …) | `≤P1a` 가 맞음 (표 첫 칸 파일 값). 크기는 5칸 × 8B = `0x28`B |
| `LdrpStatistics` | `P1a~P1b` | 맞음 | — | 그대로 (3 dword) |

- 이름 칸 7.3 라벨 정정은 위 넷 (`ShowSnaps` · `RtlpDisableHeapLookaside` · `LdrpInitOnceLoadAsDataCrits` · `RtlpSearchPathMode`). 나머지는 이름 칸 자체의 값이다.

#### 1.1 `.data ≤P1a` 57

| 구역 | 구간 (7.3) | 이름 (칸 크기) | 가름 | 쓰는 줄 | 파일 값 | 사진 값 (P1a · P1b · P2) | 경로 · 조건 |
|---|---|---|---|---|---|---|---|
| .data | ≤P1a | `_pctype` (`0x1cc000`, 8B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 13곳: `isleadbyte +0x0` · `__pctype_func +0x0` · `_input_s` · `_winput_s`) | ntdll+0x18fd70 | = 파일 | — (CRT 문자 분류 읽기만) |
| .data | ≤P1a | `_pwctype` (`0x1cc008`, 8B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 2곳: `iswctype +0xa` · `_iswctype_l +0xa`) | ntdll+0x18ff72 | = 파일 | — |
| .data | ≤P1a | `__initiallocinfo` (`0x1cc010`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 11곳) | 0x1 0 | = 파일 | — |
| .data | ≤P1a | `__lconv_c` (`0x1cc170`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (코드 참조 0, `.data` `0x1cc138` 포인터가 가리킴) | ntdll+0x1905b0 ntdll+0x1905b2 | = 파일 | — |
| .data | ≤P1a | `g_SbTableEntry_VerifyVersionInfoSwitch_RTL` (`0x1cc220`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (코드 참조 0, `.rdata` `0x174b80` 이 가리킴) | ntdll+0x198190 ntdll+0x1981c0 | = 파일 | — (스위치 표 항목, `SbSelectProcedure` 가 읽음 `[추정]`) |
| .data | ≤P1a | `g_SbTableEntry_HeapPaddingAndLFHSubsegmentCommitSwitch_RTL` (`0x1cc3f0`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (코드 참조 0, `.rdata` `0x174b88` 이 가리킴) | ntdll+0x198570 ntdll+0x1983e0 | = 파일 | — (`RtlInitializeHeapManager +0x56` `SbSelectProcedure` 가 읽는 표 `[정적]` sast/40) |
| .data | ≤P1a | `RtlpSecMemListHead` (`0x1cc538`, 16B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `RtlRegisterSecureMemoryCacheCallback +0x8a` (꼬리 `+0x8`) | ntdll+0x1cc538 ntdll+0x1cc538 | = 파일 | 길 안 읽기·`lea` 만 (`RtlpSecMemFreeVirtualMemory +0x40`); 쓰는 쪽은 공개 API |
| .data | ≤P1a | `RtlpLockedSectionList` (`0x1cc548`, 16B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `RtlLockModuleSection +0xfc` (꼬리 `+0x8`) | ntdll+0x1cc548 ntdll+0x1cc548 | = 파일 | 공개 API `RtlLockModuleSection` — test.exe 길 밖 |
| .data | ≤P1a | `RtlpDebugPageHeapTable` (`0x1cc570`, 8B 봄) | 파일값 그대로 · 조건부 안 탐 | `AvrfMiniLoadDll +0xa4f` | ntdll+0x1d3c08 | = 파일 | `AVrfInitializeVerifier +0x3f2` → 이 함수. `LdrpInitializeProcess +0x1568` 이 `AvrfAppVerifierMode == 0` 이면 AppVerifier 묶음을 건너뜀 (3.9) — test.exe 는 0 |
| .data | ≤P1a | `NormalizationListHead` (`0x1cc6a8`, 16B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `RtlpGetNormalization +0x141` (꼬리 `+0x8`) | ntdll+0x1cc6a8 ntdll+0x1cc6a8 | = 파일 | `RtlpGetNormalization` = 유니코드 정규화 API 길 (`NormalizationList__Lookup`), test.exe 길 밖 |
| .data | ≤P1a | `TppCleanupGroupMemberpNoPoolList` (`0x1cc738`, 16B 봄) | 파일값 그대로 · 조건부 안 탐 | `TppCleanupGroupMemberInitialize +0x284` (꼬리 `+0x8`) | ntdll+0x1cc738 ntdll+0x1cc738 | = 파일 | 멤버의 풀 칸 `[멤버+0x90]` 이 `+0x12b ~ +0x173` 뒤에도 0 일 때만 (`+0x17d JZ → +0x249`, 락 `TppCleanupGroupMemberpNoPoolListLock`). 풀이 붙는 멤버는 `TppPoolpReferenceGlobalPool` (`+0x153`) 등이 `+0x90` 을 채워 안 탐 `[정적]`. 사진: 머리 칸 안 바뀜 |
| .data | ≤P1a | `LdrpUserDllDirectories` (`0x1cc7f8`, 16B 봄) | 파일값 그대로 · 조건부 안 탐 | `LdrAddDllDirectory +0x1b5` (머리 `+0x0`) | ntdll+0x1cc7f8 ntdll+0x1cc7f8 | = 파일 | `LdrpInitializeProcess +0x1b72` → `LdrpInitializePerUserWindowsDirectory +0x17b` → 이 함수. `+0x47` 간접 호출 (`TermsrvGetWindowsDirectoryW`) 이 길이 0 을 돌려주면 `+0x4c ~ +0x50` 에서 끝 (사진 변화 없음으로 확인) |
| .data | ≤P1a | `LdrpSnapsLock` (`0x1cc810`, 16B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `LdrpAddUnicodeStringToSnapsBuffer +0x116 · +0x1e0` (`lock cmpxchg`, 잠금 칸 `+0x8`) | ntdll+0x1cc838 0xffffffff | = 파일 | 스냅 글 쓰기 길 (`ShowSnaps` 켜졌을 때 `LdrpLogInternal` 류). 정적 임계구역 — 사진 3장 모두 안 잡힌 상태 |
| .data | ≤P1a | `LdrpSnapsLock_DEBUG` (`0x1cc838`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (코드 참조 0) | 0 ntdll+0x1cc810 | = 파일 | — |
| .data | ≤P1a | `LdrpLoaderLock_DEBUG` (`0x1cc868`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (코드 참조 0) | 0 ntdll+0x1cc898 | = 파일 | — |
| .data | ≤P1a | `LdrpLoaderLock` (`0x1cc898`, 16B 봄) | 파일값 그대로 · 잠금 칸 (공용 부품이 잡고 풂) | 처음 값은 파일 그대로. 잠금 `+0x8 ~ +0x10` 은 공용 `RtlEnterCriticalSection` · `RtlLeaveCriticalSection` 이 바꿈 (직접 쓰는 줄은 `LdrpCompleteProcessCloning +0x48 ~ +0x5e` 뿐 — 길 밖) | ntdll+0x1cc868 0xffffffff | P1a ntdll+0x1cc868 0xffffffff → P1b ntdll+0x1cc868 0x1fffffffe → P2 ntdll+0x1cc868 0xffffffff | `_LdrpInitialize +0xf3 LEA` 가 주소를 넘김 (②). P1b 사진은 잡힌 상태 (`LockCount −2` · `RecursionCount 1` · `OwningThread 0x26ec`), P2 는 풀림 |
| .data | ≤P1a | `LdrpDllNotificationList` (`0x1cc8c0`, 16B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `LdrRegisterDllNotification +0xa9` (꼬리 `+0x8`) | ntdll+0x1cc8c0 ntdll+0x1cc8c0 | = 파일 | 공개 API — 길 안 읽기 (`LdrpSendDllNotifications +0x52 LEA`) |
| .data | ≤P1a | `RtlCriticalSectionList` (`0x1cc8d0`, 16B 봄) | ② 쓰는 줄 있음 | `RtlpAddDebugInfoToCriticalSection +0x353` (옛 꼬리 `[RAX]` ← 새 노드; 빈 목록이면 머리 `+0x0`) · `+0x356` (머리 `+0x8` ← 새 노드) | ntdll+0x1cc8d0 ntdll+0x1cc8d0 | P1a ntdll+0x1d4030 ntdll+0x1d4060 → P1b·P2 ntdll+0x1d4030 ntdll+0x1d4090 | `RtlInitializeCriticalSectionEx` (디버그 정보를 만드는 임계구역마다) → `RtlpAddDebugInfoToCriticalSection`. 첫째 `RtlInitializeHeapManager +0x12a` `RtlpProcessHeapsLock` (정적 항목 0), 둘째 프로세스 힙 락 (항목 1) — 값 증거 `RtlpStaticDebugInfo +0x8 · +0x38` |
| .data | ≤P1a | `LdrpDllNotificationLock_DEBUG` (`0x1cc8e0`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (코드 참조 0) | 0 ntdll+0x1cc910 | = 파일 | — |
| .data | ≤P1a | `LdrpDllNotificationLock` (`0x1cc910`, 16B 봄) | 파일값 그대로 · 잠금 칸 (공용 부품이 잡고 풂) | 처음 값은 파일 그대로 (`lea` 14곳 = 잠금 인자, 직접 쓰는 줄 0) | ntdll+0x1cc8e0 0xffffffff | = 파일 | `LdrpSendPostSnapNotifications +0x30 · +0xeb` 등이 잡고 풂 (사진 3장 모두 풀린 상태) |
| .data | ≤P1a | `g_SbCachedModuleTable_RTL` (`0x1cca80`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 11곳) | 0 0x200000000 | = 파일 | — |
| .data | ≤P1a | `_control87 의 dazSupported` (`0x1ccaf0`, 1B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `_control87 +0x1ae` (`MOV byte,0` — 0 을 씀) | 0x1 | = 파일 | CRT 부동소수 제어 API — test.exe 길 밖 |
| .data | ≤P1a | `_matherr_flag` (`0x1ccf40`, 4B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 3곳) | 0x2694 | = 파일 | — |
| .data | ≤P1a | `__mb_cur_max` (`0x1ccf50`, 4B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 8곳) | 0x1 | = 파일 | — |
| .data | ≤P1a | `ShowSnaps` (`0x1ccf60`, 4B 봄) | 파일값 그대로 · 조건부 안 탐 | `LdrpInitializeProcess +0x59c` · `+0x754` (`OR [LdrpDebugFlags], 1`) | 0 | = 파일 | `PEB.NtGlobalFlag & 2` (FLG_SHOW_LDR_SNAPS) 일 때만 (`+0x58e TEST [RDI+0xbc],CL` · `+0x741 ~ +0x752`). test.exe `NtGlobalFlag` = 0. 이름 `ShowSnaps` = `LdrpDebugFlags` (같은 주소) |
| .data | ≤P1a | `g_sqmSessionHandle` (`0x1ccf78`, 8B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 2곳) | 0xffffffffffffffff | = 파일 | — |
| .data | ≤P1a | `g_SxsMaxDeadActivationContexts` (`0x1ccf80`, 4B 봄) | 파일값 그대로 · 조건부 안 탐 | `LdrpInitializeExecutionOptions +0x49c` | 0xffffffff | = 파일 | IFEO 값 `+0x48b RtlQueryImageFileKeyOption` ≠ 0 일 때. IFEO 부분 통째 건너뜀: `+0xa6 TEST [PP+8],0x4000 · +0xad JNZ → +0x604` (test.exe `Flags 0x4001`) |
| .data | ≤P1a | `Utf8TableInfo` (`0x1cd050`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 29곳 · `lea` 9곳) | 0xfffd003f0004fde9 0x3f003f | = 파일 | — |
| .data | ≤P1a | `PossibleDeadlockReportThreshold` (`0x1cd094`, 4B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `RtlpPossibleDeadlock +0xa6` | 0x1 | = 파일 | 교착 의심 보고 길 (`RtlpWaitOnCriticalSection` 류) — 길 밖 |
| .data | ≤P1a | `RtlpPtrTree` (`0x1cd098`, 16B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | 없음 (읽기·`lea` 만; 트리 바꾸기는 `RtlCompareExchangePointerMapping` · `RtlRemovePointerMapping` 이 주소를 넘김) | 0 0x1 | = 파일 | 공개 API (포인터 매핑) — 길 밖 |
| .data | ≤P1a | `RtlpLargestLfhBlock` (`0x1cd0a8`, 8B 봄) | 파일값 그대로 · 조건부 안 탐 | `RtlInitializeHeapManager +0x186` (`0x400` 을 씀) | 0x4000 | = 파일 | `+0x174 (ret +0x179) RtlGetSuiteMask → BT EAX,0x10` 이 켜졌을 때만 (`+0x17d JNC`). test.exe 스위트 마스크 `0x110` → 비트 16 꺼짐 (sast/40) |
| .data | ≤P1a | `__ft_g_token` (`0x1cd0c4`, 12B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `__FT_token_increment +0x18` (`cmpxchg`) · `RtlpHeapTrkLeakCallback +0x160 · +0x1f5` · `RtlpHeapTrkReportResult +0xb0` | 0x1 0x1 0x58 | = 파일 | 기능 텔레메트리·힙 누수 추적 길 — 길 밖 |
| .data | ≤P1a | `TelemetryEventThrottle` (`0x1cd0d0`, 4B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `RtlCapabilityCheck +0x404 · +0x562` | 0x1 | = 파일 | `RtlCapabilityCheck` 공개 API 길 — 길 밖 |
| .data | ≤P1a | `RtlpHeapFailureInfo` (`0x1cd0e0`, 16B 봄) | 파일값 그대로 · 조건부 안 탐 | `RtlpLogHeapFailure +0x21 ~ +0x35` · `RtlpLocateRelatedBlocks +0xf6 ~ +0x25c` · `RtlpPrintErrorInformation` | 0x6d000000002 0 | = 파일 | 힙 오류를 보고할 때만 (`RtlpReportHeapFailure +0x1c LEA`). 정상 길엔 주소만 `RtlpHpHeapGlobals +0x10` 에 (sast/40) |
| .data | ≤P1a | `RtlpDiskSpeedPolicy` (`0x1cd7b4`, 4B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `RtlpDiskSpeedInitialize +0x5a` | 0xa | = 파일 | `RtlpDiskSpeedInitialize` 는 직접 부르는 곳이 `_calls.tsv` 에 없음 (한 번 실행 콜백 `[추측]`) — 길 밖 |
| .data | ≤P1a | `RtlpUnloadEventTraceExSize` (`0x1cd7c0`, 4B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (`RtlGetUnloadEventTraceEx +0x0` 가 주소만 돌려줌) | 0x68 | = 파일 | — |
| .data | ≤P1a | `RtlpUnloadEventTraceExNumber` (`0x1cd7c4`, 4B 봄) | 파일값 그대로 · 조건부 안 탐 | `LdrpInitializeExecutionOptions +0x1fe` · `LdrpRecordUnloadEvent +0x1cd` | 0x40 | = 파일 | IFEO `RtlQueryApplicationKeyOption` 값 ≠ 0 일 때 (IFEO 건너뜀) / 모듈 언로드 때 |
| .data | ≤P1a | `LdrpQueryProcessModuleInformationLoopDetectorCount` (`0x1cd7c8`, 4B 봄) | 파일값 그대로 · 조건부 안 탐 | `LdrpInitializeExecutionOptions +0x5fe` | 0x19000 | = 파일 | IFEO 값이 있을 때 (IFEO 건너뜀) |
| .data | ≤P1a | `ModuleNamePlaceholderBuffer` (`0x1cd7d0`, 44B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (`LdrpLoadDependentModuleInternal +0x19be` · `LdrpFindOrPrepareLoadingModule +0x65b` 가 `lea` 로 주소를 씀 — 값은 글자 `PatchImagePlaceholder`) | `PatchImagePlaceholder` (UTF-16) | = 파일 | — |
| .data | ≤P1a | `LdrpPolicyBits` (`0x1cd7fc`, 4B 봄) | 파일값 그대로 · 조건부 안 탐 | `LdrpInitializePolicy +0x7e · +0x26b · +0x305 · +0x31d · +0x329` | 0x16 | = 파일 | `LdrpInitializeProcess +0xfc5 (ret +0xfca)` → `LdrpInitializePolicy`. `PEB.BitField & 0x10` (IsPackagedProcess, `+0x42 ~ +0x48`) **그리고** `DllPath.Length ≠ 0` (`+0x72 ~ +0x75`) 일 때만. test.exe `BitField = 0x4` |
| .data | ≤P1a | `SbDynamicContextDllCount` (`0x1cd800`, 16B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 이름 칸 (`+0x0` 문맥 번호 표 8B 칸) 은 쓰는 줄 없음. **옆 칸 `+0x4` (개수)** 는 `SbUpdateSwitchContextBasedOnDll +0xfb` `INC.LOCK` · `+0x2ba` `XADD.LOCK` | 0 0x1 | P1a 0 0x1 → P1b·P2 0x300000000 0x1 | `②N… LdrpSendPostSnapNotifications (call +0xa3) → SbUpdateSwitchContextBasedOnDll +0xfb` (P1a~P1b 에 칸 0 개수 0 → 3) |
| .data | ≤P1a | `RtlpFlsContext` (`0x1cde30`, 16B 봄) | ② 쓰는 줄 있음 | `LdrpInitializeProcess +0x82e` (memset `+0x58`) · `+0x83a` (`+0x0` ← 0) · `+0x844 ~ +0x859` (`+0x8 ~ +0x47` ← 0) · `+0x86e · +0x867` (`+0x48 · +0x50` 목록 머리 ← 자기 주소) | 0 0 | P1a 0 0 → P1b 0 0 → P2 0 0x25996f3bfd0 | ②… (`LdrpInitializeProcess` 직접). 뒤에 바꾸는 쪽 `RtlFlsSetValue +0x13b` 등 (P1b~P2, 이름만) |
| .data | ≤P1a | `RtlCriticalSectionDebugSList` (`0x1cde90`, 16B 봄) | ② 쓰는 줄 있음 | `LdrpInitializeProcess +0x78d` (`MOVAPS` 로 0) · `+0x7c4 (ret +0x7c9)` `InterlockedPushListSList` (정적 항목 8개를 한꺼번에 넣음) | 0 0 | P1a 0x20006 ntdll+0x1d4080 → P1b·P2 0x20005 ntdll+0x1d40b0 | ②… (`LdrpInitializeProcess` 직접). 뒤에 꺼내는 쪽 `RtlpAddDebugInfoToCriticalSection +0x12` (P1a → P1b 깊이 6 → 5) |
| .data | ≤P1a | `LdrpThunkSignature` (`0x1cdfa0`, 16B 봄) | ② 쓰는 줄 있음 | `LdrpCaptureCriticalThunks +0x95` (`MOVDQU [RCX],XMM0`, 칸마다 16B ×5) | 0 0 | 0x33b8d18b4c 0x17ffe03082504f6 | `LdrpInitializeProcess +0x481 (ret +0x486)` → `LdrpCaptureCriticalThunks` |
| .data | ≤P1a | `LdrpInitOnceLoadAsDataCrits` (`0x1ce428`, 8B 봄) | ② 쓰는 줄 있음 | `LdrpInitMuiCrits +0x35` (`CMPXCHG.LOCK` 0 → 1) · `+0x60 (ret +0x65)` `RtlRunOnceComplete` (→ 2) | 0 | P1a 0 → P1b·P2 0x2 | `LdrpInitializeProcess +0x1aa4 → LdrpMapAndSnapDependency +0x5e → LdrpFindDllActivationContext +0x158 → LdrResFindResourceDirectory +0x70 → LdrResSearchResource +0x727 → LdrAddLoadAsDataTable +0x37 → LdrpInitMuiCrits` (각 `+off` = 앞 함수 안 call 자리; P1a~P1b, 정적 최단 길 — 어느 줄을 타는지는 동적) |
| .data | ≤P1a | `RtlpDisableHeapLookaside` (`0x1ce6e0`, 4B 봄) | 파일값 그대로 · 조건부 안 탐 | `LdrpInitializeExecutionOptions +0x127` (칸 주소를 IFEO 읽기에 넘김) | 0 | = 파일 | IFEO `DisableHeapLookaside` — IFEO 건너뜀 (`+0xa6 · +0xad`). 읽는 쪽: `RtlpPerformHeapMaintenance +0x19`, `RtlInitializeHeapManager +0xd7` |
| .data | ≤P1a | `RtlpLowFragHeapGlobalFlags` (`0x1d2218`, 4B 봄) | ② 쓰는 줄 있음 | `RtlpCreateLowFragHeap +0x256` (비트 0 켬, `& 3 == 0` 일 때 `+0x252 JNZ`) | 0 | 0x1 | LFH 켤 때: `RtlpAllocateHeap +0xe5` → `RtlpPerformHeapMaintenance +0x22` → `RtlpActivateLowFragmentationHeap +0xde` → `RtlpCreateLowFragHeap`. 조건부 다른 줄: `RtlInitializeHeapManager +0x17f` (스위트 비트 16) · `RtlpHpApplyGlobalSegmentHeapConfigurations` · `RtlSetGlobalHeapFeatures` · `RtlpHpSetLowFragHeapGlobalFlags` |
| .data | ≤P1a | `RtlpTimeoutDisable` (`0x1d2728`, 1B 봄) | ② 쓰는 줄 있음 | `LdrpInitializeProcess +0x76d` (`MOV byte,CL` — `CL = 1`) | 0 | 0x1 | `+0x764 CMP [RtlpTimeout], −0x861c46800 · +0x76b JGE` 가 안 뛸 때 (타임아웃 < −1시간). −30일 이라 씀 |
| .data | ≤P1a | `RtlpTimeout` (`0x1d2730`, 8B 봄) | ② 쓰는 줄 있음 | `LdrpInitializeProcess +0x6b5` (`PEB+0xc0` 복사) · `+0x73a` (이미지 LoadConfig `+0x14` 가 0 이 아닐 때만 덮음) | 0 | 0xffffe86d079b8000 | ②… (`LdrpInitializeProcess` 직접). 값의 주인은 nt (`PEB.CriticalSectionTimeout` = `−25,920,000,000,000` 100ns = −30일) |
| .data | ≤P1a | `RtlpEnvironmentHashTable` (`0x1d2840`, 32B 봄) | ② 쓰는 줄 있음 | `RtlpSetupEnvironmentHashTable +0x10 · +0x17 · +0x32 · +0x47 · +0x52` (비우고 `+0x4` ← `0x400`, `+0x8` ← 표 포인터) · `RtlpScanEnvironment +0x29d` (항목 채움) · `+0x3b3 · +0x3c5 · +0x3cc` (`+0x18` · `+0x10`) | 0 0 0 0 | 0x4000000002f 0x25996f33460 0x1 0x25996f33452 | `LdrpInitializeProcess +0xc2d (ret +0xc32) RtlpInitEnvironmentBlock → (call +0x9b/ret +0xa0) RtlpSetupEnvironmentHashTable`; 채우기는 첫 `RtlQueryEnvironmentVariable` (`RtlpWow64ThunkEnvironmentTo64 +0xa5`) 안의 `RtlpScanEnvironment` |
| .data | ≤P1a | `LdrpHotPatchInitCompleteEvent` (`0x1d2910`, 8B 봄) | ② syscall 이 씀 | `LdrpInitializeInternal +0x8e (ret +0x93)` `NtCreateEvent(&칸, 0x1f0003, 0, 0, 0)` — 핸들 번호는 nt 가 써 줌 | 0 | 0x8 | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal` — `+0x3b CMPXCHG.LOCK [LdrpHotPatchInitialized]` 0 → 1 이 성공한 첫 스레드만 (`+0x45 JZ → +0x77`) |
| .data | ≤P1a | `RtlpLowFragHeapRandomData` (`0x1d3420`, 16B 봄) | ② 쓰는 줄 있음 | `RtlpInitializeLfhRandomDataArray +0x4b` (32칸 × 8B 루프, `+0x56 JNZ`) | 0 0 | 0x1c375003da390ef8 0x823b64db15d9c832 | `RtlpCreateLowFragHeap +0x25c (ret +0x261)` → 이 함수 (위 `RtlpLowFragHeapGlobalFlags` 와 같은 LFH 켜기 길). 값 = `RtlpHpHeapGlobals +0x40` 의 xorshift64* 난수 |
| .data | ≤P1a | `RtlpHypervisorSharedUserVa` (`0x1d3e98`, 8B 봄) | ② syscall 이 씀 | `LdrpInitializeProcess +0xb4d (ret +0xb52)` `NtQuerySystemInformation(0xc5, buf, 8)` → `+0xb5e MOV [칸],RAX` | 0 | 0x7ffe3000 | ②… 묶음 7 힙 뒤 (`+0xb54 JS` 이면 안 씀). 값 `0x7ffe3000` = nt 가 유저 공간에 올린 하이퍼바이저 공유 쪽 |
| .data | ≤P1a | `RtlpStaticDebugInfo` (`0x1d4020`, 16B 봄) | ② 쓰는 줄 있음 | `LdrpInitializeProcess +0x7a2` (항목 7개 `[i] ← 다음 항목`) · `+0x7b4` (마지막 ← 0) · `+0x7c4` `InterlockedPushListSList` | 0 0 | 0x7ffa00000000 ntdll+0x1d3f60 | ②… (`LdrpInitializeProcess` 직접). 쓰는 쪽 뒤: `RtlpAddDebugInfoToCriticalSection` 이 꺼내 항목 칸 (CriticalSection · ProcessLocksList) 을 채움 |
| .data | ≤P1a | `LdrpEnclaveList` (`0x1d4220`, 16B 봄) | ② 쓰는 줄 있음 | `LdrpInitializeProcess +0xae6 · +0xaed` (`+0x8` · `+0x0` ← 자기 주소) | 0 0 | ntdll+0x1d4220 ntdll+0x1d4220 | ②… 묶음 7 힙 뒤 (`+0xadf` 바로 뒤) |
| .data | ≤P1a | `LdrpEnclaveListLock` (`0x1d4240`, 16B 봄) | ② 쓰는 줄 있음 | `LdrpInitializeProcess +0xad3` 가 주소를 넘기고 `+0xada (ret +0xadf) RtlInitializeCriticalSectionEx(칸, 0, 0)` 이 칸을 채움 (`DebugInfo −1` · `LockCount −1` · `SpinCount 0x20007d0`) | 0 0 | 0xffffffffffffffff 0xffffffff | ②… 묶음 7 힙 뒤 |
| .data | ≤P1a | `Feature_KVS_BugFixes_55830752__private_featureState` (`0x1d4f28`, 4B 봄) | ② 쓰는 줄 있음 | `wil_details_FeatureStateCache_ReevaluateCachedFeatureEnabledState +0xa0` (`CMPXCHG`) — 칸 주소는 기술자 `0x1b7fb0 +0x0` | 0 | 0x40147 | `LdrpInitializeProcess +0xaad (ret +0xab2) Feature_Servicing_DisallowSharedLockImplicitUpgrade… → …IsEnabledFallback → wil_details_IsEnabledFallback +0x32 → Reevaluate +0x3c → GetCurrentFeatureEnabledState +0xb1 → wil_details_AreDependenciesEnabled +0x32 → GetCachedFeatureEnabledState → Reevaluate` (KVS 는 DisallowSharedLock… 이 **필요로 하는 기능**, 기술자 `+0x20`) |

#### 1.2 `.mrdata` 4 (`≤P1a` 2 · `K1 이전` 2)

| 구역 | 구간 (7.3) | 이름 (칸 크기) | 가름 | 쓰는 줄 | 파일 값 | 사진 값 (P1a · P1b · P2) | 경로 · 조건 |
|---|---|---|---|---|---|---|---|
| .mrdata | ≤P1a | `__guard_check_icall_fptr` (`0x1e7000`, 8B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 5곳: `_guard_check_icall +0x4` · `__except_validate_*` · `RtlInitializeNtUserPfn`) | ntdll+0xfac60 | = 파일 | — (K1~P1 앞 페이지 읽기 불가 — 파일 값과 P1a 가 같다) |
| .mrdata | ≤P1a | `NtUserPfn` (`0x1e7130`, 16B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `RtlInitializeNtUserPfn +0x97 · +0xa9 · +0xbf` (표 복사) · `RtlResetNtUserPfn +0x47 ~` (되돌림) | ntdll+0x137040 ntdll+0x137040 | = 파일 | user32 가 `InitializeNtdllUserPfn +0x3f` 에서 부를 때만 — test.exe 는 user32 를 안 올림 (7.5, 파일 처음 값 `UninitUser32Proc` 그대로) |
| .mrdata | K1 이전 | `LdrpVectorHandlerList` (`0x1ea578`, 16B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | `RtlpAddVectoredHandler` · `RtlpRemoveVectoredHandler` (표 안 `lea` 만 보임) | ntdll+0x1d2448 ntdll+0x1ea580 | = 파일 | 공개 API (`RtlAddVectoredExceptionHandler` 류) — 길 밖 (run82). K1 값 = 파일 값 = P1a·P2 |
| .mrdata | K1 이전 | `__security_cookie_complement` (`0x1ea5b0`, 8B 봄) | 파일값 그대로 · 쓰는 줄 없음 | 없음 (읽기 `__report_gsfailure +0x144` 뿐) | 0xffffd466d2205dcd | = 파일 | — (K1 값 = 파일 값. `__security_cookie` 만 `InitSecurityCookie` 가 새로 씀) |

- `.mrdata` 앞 3페이지 (`0x1e7000 ~ 0x1e9fff`) 는 K1~P1 읽기 불가 → `__guard_check_icall_fptr` · `NtUserPfn` 의 `≤P1a` 는 "P1a 에 처음 보임 = 파일 값" 이다. 끝 페이지 (`0x1ea000 ~`) 의 `LdrpVectorHandlerList` · `__security_cookie_complement` 는 K1 부터 읽혀 **K1 값 = 파일 값 = P1a = P2** (사진 8장 모두 같다) — `K1 이전` 라벨은 커널이 쓴 값이 아니라 **파일에 원래 들어 있던 값**이라는 뜻으로 읽힌다.
- `__security_cookie_complement` 는 읽기 한 곳 (`__report_gsfailure +0x144`, 보안 검사 실패 길) 뿐 — 쓰는 줄이 없다. 파일 값 `0xffffd466d2205dcd` = `~0x2b992ddfa232` (파일의 `__security_cookie` 값) 이고, `InitSecurityCookie` 는 `__security_cookie` (3.9) 만 새로 쓰고 이쪽은 안 고친다 → P1a 의 쿠키와 이 칸은 서로 안 맞는다 (실패 보고 때만 쓰임).

#### 1.3 `.data P1a~P1b` 3 (Q78f)

| 구역 | 구간 (7.3) | 이름 (칸 크기) | 가름 | 쓰는 줄 | 파일 값 | 사진 값 (P1a · P1b · P2) | 경로 · 조건 |
|---|---|---|---|---|---|---|---|
| .data | P1a~P1b | `RtlpSearchPathMode` (`0x1d2350`, 4B 봄) | 파일값 그대로 · 범위 밖 길에서 씀 `[추측]` | 이름 칸: `RtlSetSearchPathMode +0xe6` 뿐 (공개 API). **옆 칸 `+0x8` (`0x1d2358`)** 은 `RtlpLookupCurDirSetting +0x6e` (`lock cmpxchg`) | 0 | = 파일 | 옆 칸: `②… → LdrpDrainWorkQueue (+0x182) → LdrpProcessWork (+0x65) → LdrpMapDllSearchPath (+0x12c) → LdrpSearchPath (+0x36d) → … → RtlpLookupCurDirSetting +0x5f NtOpenKey` — `Session Manager` 키 핸들 캐시 (P1b `0xb0`, 풀이 1h) |
| .data | P1a~P1b | `LdrpStatistics` (`0x1d2650`, 12B 봄) | ② 쓰는 줄 있음 | `LdrpUpdateStatistics +0xe` (`+0x0` ← 일감 수 최대) · `+0x33` (`+0x4` `INC`, 로더 일꾼 스레드) · `+0x2b` (`+0x8` `INC`, 그 밖) | 0 0 0 | P1a 0 0 0 → P1b·P2 0x3 0x3 0x3 | `LdrpDrainWorkQueue +0x15b` (`②… LdrpInitializeProcess +0x1aac/+0x1b8c`) · `LdrpWorkCallback +0x64` (스레드 풀 일꾼, 길 밖 출발) |
| .data | P1a~P1b | `LoadAsDataCrits` (`0x1d41c0`, 16B 봄) | ② 쓰는 줄 있음 | `LdrpInitMuiCrits +0x4f (ret +0x54) RtlInitializeCriticalSectionEx(칸, 0, 0)` | 0 0 | P1a 0 0 → P1b·P2 0xffffffffffffffff 0xffffffff | 위 `LdrpInitOnceLoadAsDataCrits` 와 같은 길 (`LdrpInitMuiCrits`) |

#### 1.4 조건부 힙 전역 8 중 새로 닫은 4

| 구역 | 구간 (7.3) | 이름 (칸 크기) | 가름 | 쓰는 줄 | 파일 값 | 사진 값 (P1a · P1b · P2) | 경로 · 조건 |
|---|---|---|---|---|---|---|---|
| .data | 끝까지 0 (조건부 힙 8 중 새 4) | `RtlpHpHeapFeatures` (`0x1ce6d0`, 1B 봄) | 파일값 그대로 · 조건부 안 탐 | `RtlInitializeHeapManager +0x31` (`\|= 1`) · `RtlSetHeapInformation +0xb4` (`\|= 2`) · `RtlpHpSetLowFragHeapGlobalFlags +0x7a · +0x92` | 0 | = 파일 | `RtlInitializeHeapManager +0x28 (ret +0x2d) RtlpHpOptIntoSegmentHeap` 반환 ≠ 0 일 때만 (`+0x2f JZ → +0x6c`). test.exe 는 NT 힙 (sast/40) |
| .data | 끝까지 0 (조건부 힙 8 중 새 4) | `RtlpHpAppCompatFlags` (`0x1ce8f8`, 4B 봄) | 파일값 그대로 · 조건부 안 탐 | `RtlpHpSetLowFragHeapGlobalFlags +0x85` (`\|= 1`) · `RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit +0xf` (`\|= 2`, 길 밖) | 0 | = 파일 | 전역 힙 설정 길 (`RtlpHpApplyGlobalSegmentHeapConfigurations` ← `RtlSetGlobalHeapFeatures`) 의 조건부 갈래. 값 0 |
| .data | 끝까지 0 (조건부 힙 8 중 새 4) | `RtlpGlobalTagHeap` (`0x1ce6a8`, 8B 봄) | 파일값 그대로 · 조건부 안 탐 | `RtlCreateTagHeap +0x97` | 0 | = 파일 | `LdrpInitializeProcess +0xb78 (ret +0xb7d) RtlCreateTagHeap` 가 `NtGlobalFlag & 0x800` 일 때만 (`+0x45 JZ → +0x67` 로 0 반환). 값 0 |
| .data | 끝까지 0 (조건부 힙 8 중 새 4) | `g_RegInfo` (`0x1ce690`, 16B 봄) | 파일값 그대로 · 조건부 안 탐 | `RtlInitializeHeapManager +0x5b` (`+0x8` ← `SbSelectProcedure` 반환) | 0 0 | = 파일 | 위 `RtlpHpHeapFeatures` 와 같은 갈래 (`RtlInitializeHeapManager +0x2f JZ → +0x6c` 안 탐). 이름 칸 `+0x0` 은 `RtlUpdateProcessRegistryInfo` 등 길 밖 |

- 나머지 넷 (`RtlpLowFragHeapGlobalFlags` · `RtlpLargestLfhBlock` · `RtlpDisableHeapLookaside` · `RtlpLowFragHeapRandomData`) 은 1.1 표에 있다. 8개 모두 쓰는 줄 위치 (sast/40 이 적은 것) 가 `.asm` 줄과 맞는다: `RtlInitializeHeapManager +0x31 · +0x5b · +0x17f · +0x186` · `RtlSetHeapInformation +0xb4` · `RtlpHpSetLowFragHeapGlobalFlags +0x7a · +0x85 · +0x92` · `RtlCreateTagHeap +0x97` · `RtlpCreateLowFragHeap +0x256` · `RtlpInitializeLfhRandomDataArray +0x4b` · `LdrpInitializeExecutionOptions +0x127`.
- **정정 하나 (sast/40 의 "LFH 는 나중에 켜짐 — 범위 밖")**: `RtlpLowFragHeapGlobalFlags` 비트 0 과 `RtlpLowFragHeapRandomData` 가 사진 P1a 에 이미 있다 (`1` · 32칸 xorshift 난수). 그러니 프로세스 힙의 LFH 는 **`LdrpInitializeProcess` 가 끝나기 전, 첫 힙 할당들 중 하나 안에서** 켜진다 (아래 1e).

#### 1.5 sast/109 가 넘긴 전역 (`LdrpHeap` 는 이미 3.8)

| 구역 | 구간 (7.3) | 이름 (칸 크기) | 가름 | 쓰는 줄 | 파일 값 | 사진 값 (P1a · P1b · P2) | 경로 · 조건 |
|---|---|---|---|---|---|---|---|
| .data | sast/109 넘김 | `LdrpAuditIntegrityContinuity` (`0x1d2918`, 1B 봄) | 0 을 씀 (입력값) | `LdrpInitializeProcess +0x4fe` (`MOV byte [칸],AL`) | 0 | = 파일 | `+0x4a4 MOVAPS XMM1,[LdrSystemDllInitBlock+0xa0]` → 둘째 qword `>> 4 & 3` (`+0x4d2 ~ +0x4d6`) 이 1 또는 3 이면 1. 값 0 (입력 비트 0). 읽는 쪽 `LdrpMapDllNtFileName +0x141` |
| .data | sast/109 넘김 | `LdrpEnforceIntegrityContinuity` (`0x1d2900`, 1B 봄) | 0 을 씀 (입력값) | `LdrpInitializeProcess +0x4db` (`SETZ byte [칸]`) | 0 | = 파일 | 같은 줄기: `(>>4 & 3) == 1` 이면 1. 값 0. 읽는 쪽 `LdrpMapDllNtFileName +0x416` · `LdrpValidateIntegrityContinuity +0x90` |
| .data | sast/109 넘김 | `LdrpModuleDatatableLock` (`0x1d2458`, 8B 봄) | 파일값 그대로 · 잠금 칸 (공용 부품이 잡고 풂) | 처음 값 0 (파일 그대로). 잡고 풀기: `LdrpFindLoadedDllByName +0x17a` 외 `lock cmpxchg` · `lock and ~4` 수십 곳 (SRW 락 빠른 길 `[추정]`) | 0 | = 파일 | `LdrpFindExistingModule +0x21 LEA` 등 — 사진 3장 모두 풀린 상태 (0) |
| .data | sast/109 넘김 | `g_pShimEngineModule` (`0x1d2980`, 8B 봄) | 파일값 그대로 · 조건부 안 탐 | `LdrpInitShimEngine +0xf5` · `LdrpGetProcApphelpCheckModule +0x27e` · `LdrpGetShimEngineInterface +0x135` · `LdrpInitializeProcess +0x1cfe` (0 을 씀) · `LdrInitShimEngineDynamic +0xdd` | 0 | = 파일 | shim 엔진을 올릴 때만 (`g_ShimsEnabled` = 0, `LdrpInitializeProcess +0x1ca7 CMP · +0x1cae JZ → +0x1d07` 로 `+0x1cfe` 도 건너뜀) |

- `LdrpHeap` (`0x1d2860`): 이미 3.8 에 있다 (`LdrpInitializeProcess +0xb12` 에서 `R12` = `PEB.ProcessHeap` 을 씀, 사진 `0x25996f30000`, run46). 바뀐 것 없음.

#### 1a. 임계구역 디버그 정보 묶음 — `RtlCriticalSectionDebugSList` · `RtlpStaticDebugInfo` · `RtlCriticalSectionList`

- `LdrpInitializeProcess +0x77a ~ +0x7c9` (call `+0x7c4`):
  - `+0x78d` `MOVAPS` 로 SList 머리 `RtlCriticalSectionDebugSList` (16B) 를 0 으로.
  - `+0x794 ~ +0x7b2` 루프로 정적 디버그 정보 배열 `RtlpStaticDebugInfo` (`0x1d4020`, 항목 `0x30`B × 8 = `0x180`B) 의 항목 `i` 첫 8B 를 항목 `i+1` 주소로 이음 (`+0x79e LEA RAX,[RBX+0x30]` · `+0x7a2 MOV [RBX],RAX` · `+0x7a8 ~ +0x7b2` 끝 검사 `0x1d4140`), `+0x7b4` 마지막 항목 ← 0.
  - `+0x7c4 (ret +0x7c9) InterlockedPushListSList(머리, 첫 항목 `0x1d4020`, 끝 `0x1d4170`, 8)` → 깊이 8.
- 뒤에 임계구역이 디버그 정보를 요구할 때 `RtlpAddDebugInfoToCriticalSection +0x12 (ret +0x17) RtlInterlockedPopEntrySList` 가 이 SList 에서 꺼내 쓰고 (비면 `+0x3b RtlAllocateHeap`), 임계구역에 달고 `RtlCriticalSectionList` 꼬리에 잇는다 (`+0x349` 새 항목 `[+0]` ← 머리 · `+0x34f` `[+8]` ← 옛 꼬리 · **`+0x353 [옛 꼬리 +0] ← 새 항목`** · `+0x356` 머리 `+0x8` ← 새 항목, 락 `+0x35d` 풂). 빈 목록이면 옛 꼬리 = 머리 자신이라 `+0x353` 이 머리 `Flink` 를 쓴다.
- 사진 `[관찰]`: SList P1a `Depth 6 · Sequence 2` (정적 항목 2개 꺼내 감) → P1b·P2 `Depth 5`; `RtlpStaticDebugInfo` 항목 0 `+0x8 CriticalSection` = `ntdll+0x1d3f60` = `RtlpProcessHeapsLock`, 항목 1 `+0x38` = 프로세스 힙 `+0x2c8` (힙 락) → **첫 디버그 정보 임계구역 = `RtlInitializeHeapManager +0x12a (ret +0x12f) RtlInitializeCriticalSectionEx(&RtlpProcessHeapsLock, 0, 0x10000000)`**, 둘째 = 프로세스 힙 락. `RtlCriticalSectionList` P1a `Flink` = 항목 0 의 `ProcessLocksList` (`ntdll+0x1d4030`), `Blink` = 항목 1 (`+0x1d4060`) → P1b 에 항목 2 (`+0x1d4090`) 가 더 붙음.
- 깃발 `0x10000000` 이 "디버그 정보 강제" 라는 뜻은 값 증거 (정적 항목 0 이 `RtlpProcessHeapsLock` 에 달림) 로만 읽었다 `[추정]` — `RtlInitializeCriticalSectionEx` 본문은 안 읽음. 다른 임계구역 (`LdrpEnclaveListLock` · `LoadAsDataCrits`) 은 `Flags 0` 으로 만들어져 `DebugInfo = −1` (디버그 정보 없음) 이다.

#### 1b. `RtlpFlsContext` (`0x1cde30`)

- `LdrpInitializeProcess +0x818 ~ +0x86e`: (앞 `+0x7ce ~ +0x811` 은 TLS 비트맵 이웃 칸 `0x1ce220`·`0x1ce210` 이라 이름 밖) `+0x82e LEA RCX,[RtlpFlsContext+0x8]` → `+0x835 memset(…, 0, 0x58)` → `+0x83a` `+0x0` ← 0 (`R12` = 0) → `+0x841 ~ +0x859` `+0x8 ~ +0x47` 을 `MOVUPS` 네 번으로 0 → `+0x860 ~ +0x86e` `+0x48`·`+0x50` (목록 머리 `Flink`·`Blink`) ← `&RtlpFlsContext+0x48`. 사진 P1a `+0x48 = +0x50 = ntdll+0x1cde78` (= `RtlpFlsContext+0x48`, 자기 가리킴) 와 맞음.
- P1b~P2 에 `+0x8` (`0x25996f3bfd0`), `+0x48 · +0x50` (힙 블록), `+0x58` (`4`) 이 바뀜 — `RtlFlsSetValue +0x13b` 등 `RtlFlsAlloc*` 길 (지도 밖, 이름만; P1b~P2 사이에 FLS 슬롯을 할당·쓰는 호출이 있었다는 뜻 — 부르는 모듈은 이번 요청 밖).

#### 1c. `LdrpThunkSignature` (`0x1cdfa0`, 이름 칸 `0x50`B)

- `LdrpInitializeProcess +0x481 (ret +0x486) LdrpCaptureCriticalThunks`:
  - `+0x1f ~ +0x46` `LdrpCriticalLoaderFunctions` (`.rdata` `0x174758`, 포인터 5개) 를 스택에 복사 → `+0x67 (ret +0x6c) NtQueryVirtualMemory(-1, 함수 주소들, 클래스 4 = 작업집합 확장 정보, 크기 `0x50`)` → 칸마다 `[+0x8]` 비트 30 (`+0x87 BT RAX,0x1e`) 이 켜져 있으면 (`+0x8c JNC`) `+0x8e ~ +0x95` 그 함수 앞 16바이트를 `LdrpThunkSignature` 의 칸 `i` (`+0x10 × i`) 에 복사 (`MOVDQU`). 하나라도 꺼져 있으면 `+0xa1 LdrpDetourExist = 1`.
  - 5개 = `ZwOpenFile` (`0x161410`) · `ZwCreateSection` (`0x1616f0`) · `ZwQueryAttributesFile` (`0x161550`) · `ZwOpenSection` (`0x161490`) · `ZwMapViewOfSection` (`0x1612b0`). 복사된 바이트 = 스텁 시작 `4c 8b d1 b8 NN 00 00 00 f6 04 25 08 03 fe 7f 01` (`NN` = `0x33` · `0x4a` · `0x3d` · `0x37` · `0x28`) — 사진 P1a 의 이름 칸 80바이트와 **바이트 단위로 같다** `[관찰]`.
- 이름 밖 옆 칸 (`+0x258` 이후 `0x2` · `0x25996f318b0` · `0x400` · `0x9b2fa66240` …) 은 WNF·힙 추적 등 다른 변수다 (`LdrpInitializeProcess +0x7eb ~ +0x80a` 등). 이름만.

#### 1d. 임계구역 타임아웃 `RtlpTimeout` · `RtlpTimeoutDisable`

- `LdrpInitializeProcess +0x6ae MOV RAX,[RDI+0xc0]` (`RDI` = PEB, `+0xc0 CriticalSectionTimeout`) → `+0x6b5 MOV [RtlpTimeout],RAX`. 사진 `RtlpTimeout` = PEB `+0xc0` = `0xffffe86d079b8000` = `−25,920,000,000,000` × 100ns = **−30일** (`2,592,000 s × 10^7`; 사진 PEB 덤프 `9b2fa660c0` K1·P0·P1·P1a·P2 모두 같은 값 `[관찰]`) — **값의 주인은 nt (3.1 `+0xc0 CriticalSectionTimeout` 줄, 단계 ①)**, ntdll 은 복사만.
- 덮는 줄: 이미지 LoadConfig 가 있고 (`+0x6ef LdrImageDirectoryEntryToLoadConfigEx`, `UseWOW64`/`UseCOR` 가 아닐 때, `+0x6d5 ~ +0x6eb`) 크기 `≥ 0x18` (`+0x723 CMP [R15],0x18 · +0x727 JC`) 이고 `[R15+0x14]` (`CriticalSectionDefaultTimeout`) ≠ 0 이면 `+0x72f ~ +0x73a` 가 `× −10000` 해서 `RtlpTimeout` 을 덮는다. 사진 값이 PEB 값 그대로라 test.exe 는 이 줄을 안 탄다 (LoadConfig 의 그 칸 0 또는 크기 미만; PE_emulator 02 가 "test.exe LoadConfig 칸 0 직접 확인" 이라 적음).
- `RtlpTimeoutDisable`: `+0x75a MOV RAX,-0x861c46800` (= −1시간) · `+0x764 CMP [RtlpTimeout],RAX` · `+0x76b JGE → +0x773` 안 뛰면 (`RtlpTimeout` < −1시간) `+0x76d MOV byte [RtlpTimeoutDisable],CL` (`CL` = 1, `+0x747 LEA ECX,[R8-1]`, `R8D = 2`). −30일 < −1시간 → 1. 사진 1 `[관찰]`.

#### 1e. LFH 켜기 — `RtlpLowFragHeapGlobalFlags` · `RtlpLowFragHeapRandomData`

- 쓰는 줄: `RtlpCreateLowFragHeap +0x24a ~ +0x256` (`MOV EAX,[플래그]` · `TEST AL,3` · `+0x252 JNZ → +0x261` 로 이미 비트 0·1 이 있으면 건너뜀 · `+0x254 OR EAX,EDI` · `+0x256 MOV [플래그],EAX`, `EDI` = 1 (`+0x5b MOV EDI,0x1`, 다른 갈래는 `+0x86 LEA EDI,[RCX+0x1]` 에 `RCX` = 0 — 어느 쪽이든 1)) → 바로 이어 `+0x25c (ret +0x261) RtlpInitializeLfhRandomDataArray` (`+0x0 LEA R8,[RtlpLowFragHeapRandomData]` · `+0x7 MOV R9D,0x20` · `+0xd ~ +0x3b` `RtlpHpHeapGlobals +0x40` 상태에 xorshift64\* (`>>12` · `<<25` · `>>27`, 곱 `0x2545f4914f6cdd1d`) `CMPXCHG.LOCK` · `+0x4b MOV [R8],RDX` · `+0x4e ADD R8,8` · `+0x56 JNZ` → **32칸 × 8B = `0x100`B**).
- 사슬 (정적, `_calls.tsv` + 풀이로 만든 호출자 찾기): `RtlpAllocateHeap +0xd9 TEST [heap+0x78],0x30000000 · +0xe0 JE` 통과 시 `+0xe2 ~ +0xe5 CALL RtlpPerformHeapMaintenance` → `+0xc ~ +0x10` 비트 29 (`0x20000000`) 가 켜져 있고 `+0x19 TEST [RtlpDisableHeapLookaside],1` 이 0 이면 `+0x22 (ret +0x27) CALL RtlpActivateLowFragmentationHeap` → `+0xde (ret +0xe3) RtlpCreateLowFragHeap`. 비트 29 는 `RtlpCreateHeap +0x1a3 BTS EDX,0x1d` 가 (생성 플래그 `& 0x80` 일 때, `+0x1a7 TEST R15B,0x80 · +0x1ab CMOVZ`) 세운다. 다른 부르는 곳: `RtlpSetHeapDebuggingInformation +0xaf` (← `RtlSetHeapDebuggingInformation +0x40` ← `RtlSetHeapInformation +0x22a`, 힙 정보 클래스를 바꾸는 호출 — `LdrpInitializeProcess +0xa34` 는 sast/40 이 "안 탐" 으로 닫음).
- 이 사슬은 지도에서 `RtlpAllocateHeap` (공용 도구, 호출자 많음) 에서 끊겨 **지도 안 칸이 아니라 밖 출발**이다. 사진 P1a 에 이미 있으므로 LFH 켜기는 `LdrpInitializeProcess` 중간의 어느 프로세스 힙 할당에서 일어난다 → **정확한 호출 줄은 동적** (아래 동적 1).
- `RtlInitializeHeapManager +0x17f ~ +0x186` 는 스위트 비트 16 (`RtlGetSuiteMask`, test.exe `0x110` → 꺼짐) 일 때만 같은 전역에 `|= 4` 와 `RtlpLargestLfhBlock = 0x400` 을 쓴다 (안 탐, 사진 파일 그대로).

#### 1f. 환경 해시 표 `RtlpEnvironmentHashTable` (`0x1d2840`, 이름 칸 `0x20`B)

- `RtlpInitEnvironmentBlock +0x9b (ret +0xa0) RtlpSetupEnvironmentHashTable`: `+0x10 · +0x17` 이름 칸 32B 를 0 → `+0x1e (ret +0x23) RtlpAllocateEnvironmentHashTable(0x100, 0)` → `+0x32` `+0x0` (개수) ← 0 → `+0x47` `+0x8` ← 표 포인터 → `+0x52` `+0x4` ← `0x400` → `+0x6a STOSQ.REP` 로 표 칸을 `&머리 | 1` (빈 표시) 로 채움.
- 채우기: `RtlQueryEnvironmentVariable` 이 표가 비었으면 (`+0x10` = 0) `RtlpScanEnvironment` (`RtlQueryEnvironmentVariable +0x6cf · +0x8c2`) 를 불러 환경 문자열마다 `RtlpInsertEnvironmentHashTableEntry` (`RtlpScanEnvironment +0x29d`) → `+0x3b3 · +0x3c5` `+0x18` ← 환경 포인터 · `+0x3cc` `+0x10` ← 1. 환경을 바꾸는 `RtlSetEnvironmentVar` 는 `RtlpClearEnvironmentHashTable` (`+0x1d` `+0x18` ← 0 · `+0x27` `+0x10` ← 0 + 항목 해제) 를 불러 표를 비운다. 사진 P1a `{개수 0x2f (47), 크기 0x400, 표 0x25996f33460, 스캔됨 1, 환경 0x25996f33452}`.
- 첫 `RtlQueryEnvironmentVariable` 은 바로 `RtlpInitEnvironmentBlock +0xa4` 가 부르는 `RtlpWow64ThunkEnvironmentTo64 +0xa5` (질문 3) — 그 안 `RtlSetEnvironmentVar` 두 번이 표를 비우고, 마지막 조회가 다시 채운다 `[정적: 사진 값과 맞음, 정확한 마지막 조회 줄은 안 짚음]`.

#### 1g. 묶음 7 힙 뒤 부분 (`LdrpInitializeProcess +0xa50 ~ +0xb7d`) — 칸 가름

| 줄 | 하는 일 | 쓰는 칸 | 이번 요청 몫 · 이미 닫힘 |
|---|---|---|---|
| `+0xa4b (ret +0xa50) LdrpInitializeProcessHeap` ~ `+0xaa9` | 힙 만들기 · 실패 길 | PEB 힙 4칸 · 힙 전역 | 이미 닫힘 (run46) |
| `+0xaad (ret +0xab2)` `Feature_Servicing_DisallowSharedLockImplicitUpgrade…IsEnabledNoReportingNoInline` · `+0xaba` | 기능 검사 → `RtlpSrwLockAllowImplicitUpgrade` ← `ECX` | `RtlpSrwLockAllowImplicitUpgrade` · **`Feature_KVS_BugFixes…featureState`** (필요 기능 평가) · 자기 `featureState` | 앞 둘 이미 3.8 (run62); **KVS 칸 새로 닫음** |
| `+0xac0 (ret +0xac5)` `Feature_Servicing_LoaderSnapsBuffer…IsEnabledDeviceUsageNoInline` · `+0xac9 (ret +0xace) LdrpInitializeSnapsBuffer` | 기능 검사 · 스냅 버퍼 | `LoaderSnapsBuffer featureState` · `LdrpSnapsUnicodeString*` | 이미 닫힘 (run62) |
| `+0xad3 ~ +0xadf` | `RtlInitializeCriticalSectionEx(&LdrpEnclaveListLock, 0, 0)` | **`LdrpEnclaveListLock`** | **새로 닫음** (1.1 표) |
| `+0xae6 · +0xaed` | `LdrpEnclaveList` 머리 ← 자기 주소 | **`LdrpEnclaveList`** | **새로 닫음** |
| `+0xaf4 ~ +0xb05` | `PEB.NtGlobalFlag & 0x800000` 이면 `RtlInitializeExceptionLog` (안 탐) | `RtlpExceptionLog2` | 이미 닫힘 (sast/57 8c) |
| `+0xb0d (ret +0xb12) RtlpInitializeThreadActivationContextStack` | 스레드 활성화 문맥 스택 만들기 | TEB 활성화 문맥 스택 쪽 칸 | 이번 요청 밖 (TEB 칸 — 3.3) |
| `+0xb12` | `LdrpHeap` ← `R12` | `LdrpHeap` | 이미 3.8 (run46) |
| `+0xb19 ~ +0xb32` | `EtwpFreeRegistrationList` 16B 0 · `PrivateLoggerNotificationEntry` ← 0 | 둘 | 이미 닫힘 (run62) |
| `+0xb4d (ret +0xb52) NtQuerySystemInformation(0xc5, buf, 8)` · `+0xb5e` | 하이퍼바이저 공유 쪽 주소 | **`RtlpHypervisorSharedUserVa`** | **새로 닫음** (syscall 이 씀) |
| `+0xb78 (ret +0xb7d) RtlCreateTagHeap` · `+0xb7d` | 태그 힙 → `NtdllBaseTag` | `NtdllBaseTag` · (`RtlpGlobalTagHeap` 조건부) | 앞 이미 3.8; `RtlpGlobalTagHeap` **새로 닫음** (조건부) |

- 이 구간에서 요청서가 "묶음 7 힙 뒤 부분" 으로 모르던 것은 **네 칸** (`LdrpEnclaveList` · `LdrpEnclaveListLock` · `RtlpHypervisorSharedUserVa` · `Feature_KVS…featureState`) 이었고 모두 위 표에서 줄을 찾았다. 뒤 `+0xb83 TpInitializePackage` 부터는 sast/57 몫.

#### 1h. P1a~P1b 쪽 (Q78f) 줄기

- **`LdrpInitOnceLoadAsDataCrits` · `LoadAsDataCrits`**: `LdrpInitMuiCrits` (지도 `m10`) 가 "한 번만" 초기화한다: `+0x9 MOV RAX,[Once]` · `+0x18 ~ +0x1e` 상태 2 면 끝 · `+0x35 CMPXCHG.LOCK [Once]` 0 → 1 (처음 도착) · **`+0x46 LEA RCX,[LoadAsDataCrits]` → `+0x4f (ret +0x54) RtlInitializeCriticalSectionEx(칸, 0, 0)`** · **`+0x60 (ret +0x65) RtlRunOnceComplete(&Once, 0, 0)`** (상태 2). 사진: P1a 둘 다 0 → P1b `Once` = 2, `LoadAsDataCrits` = `{DebugInfo −1, LockCount −1, RecursionCount 0, SpinCount 0x20007d0}`.
  - 정적 최단 호출 사슬 (지도 BFS): `LdrpInitializeProcess +0x1aa4 (ret +0x1aa9) LdrpMapAndSnapDependency → +0x5e (ret +0x63) LdrpFindDllActivationContext → +0x158 (ret +0x15d) LdrResFindResourceDirectory → +0x70 (ret +0x75) LdrResSearchResource → +0x727 (ret +0x72c) LdrAddLoadAsDataTable → +0x37 (ret +0x3c) LdrpInitMuiCrits`. `LdrResSearchResource` 에서 이 길은 "파일 경로로 리소스를 찾는" 갈래로 보인다 `[추정]` (`+0x6f7 LdrpResMapFile` 로 파일을 데이터로 열고 `+0x727` 이 표에 올림). 같은 함수를 부르는 다른 길 (`LdrRemoveLoadAsDataTable +0x34` · `LdrpGetLoadAsEntry +0x2c` · `LdrpGetFileSizeFromLoadAsDataTable +0x14`) 도 있어서 **어느 줄이 처음인지는 정적으로 못 정함** (동적 2).
- **`LdrpStatistics`** (`0x1d2650`, dword 3개): `LdrpUpdateStatistics` (지도 `m6`) — `+0x0 MOV EAX,[LdrpWorkInProgress]` · `+0x6 CMP [LdrpStatistics],EAX · +0xc JNC` · **`+0xe MOV [LdrpStatistics],EAX`** (진행 중 일감 수 최대) · `+0x14 ~ +0x29` TEB `SameTebFlags & 0x2000` (로더 일꾼 스레드) 이면 **`+0x33 INC [LdrpStatistics+0x4]`**, 아니면 **`+0x2b INC [LdrpStatistics+0x8]`**. 부르는 곳 `LdrpDrainWorkQueue +0x15b` (`②… LdrpInitializeProcess +0x1302 · +0x19c1 · +0x1aac · +0x1b8c` 의 큐 비우기, 주 스레드) · `LdrpWorkCallback +0x64` (스레드 풀 일꾼, 지도 밖 출발). 사진 P1b·P2 `{3, 3, 3}` — 일꾼 스레드가 있어 (`LdrpThreadPool`, run69) 두 갈래가 모두 3 `[관찰]`.
- **`RtlpSearchPathMode`**: 이름 칸 (`0x1d2350`) 에 쓰는 줄은 `RtlSetSearchPathMode +0xe6` (공개 API `SetSearchPathMode` 길, 지도 밖) 뿐이고 test.exe 길엔 호출이 없다 → 사진 3장 모두 0. **P1b 에 바뀐 것은 옆 칸 `0x1d2358`** (`0xb0`): `RtlpLookupCurDirSetting +0x5f (ret +0x64) NtOpenKey(&지역, 1, OBJECT_ATTRIBUTES `.rdata` 0x173be8 = "\\Registry\\MACHINE\\System\\CurrentControlSet\\Control\\Session Manager")` → `+0x6e CMPXCHG.LOCK [0x1d2358],RCX` (열린 키 핸들 캐시 — 값은 nt 가 정한 핸들 번호; 이어 `+0xa1 NtQueryValueKey`). 사슬: `②… → LdrpDrainWorkQueue (+0x182) → LdrpProcessWork (+0x65) → LdrpMapDllSearchPath (+0x12c) → LdrpSearchPath (+0x36d) → LdrpComputeLazyDllPath (+0x62) → LdrpGetDllPath (+0xa8) → RtlpGetCachedPath (+0xbe) → RtlpComputeDllPath (+0x4b) → RtlpLookupCurDirSetting` (지도 `m7 ~ m10`, `RtlpComputeDllPath` 부터는 `out`; 의존 DLL 을 찾는 길이라 P1a~P1b). 요청서 Q78f 의 "RtlpSearchPathMode" 줄은 **옆 칸 줄을 3.8 "목록 밖 자리" 로** 더하고 이름 칸은 `끝까지 0` 으로 정정.

#### 1i. syscall 이 쓰는 둘

- **`LdrpHotPatchInitCompleteEvent`** (`0x1d2910`, HANDLE 8B): `LdrpInitializeInternal +0x82 LEA RCX,[칸]` · `+0x89 MOV EDX,0x1f0003` (`EVENT_ALL_ACCESS`) · `+0x8e (ret +0x93) NtCreateEvent(&칸, 0x1f0003, 0, 0 (NotificationEvent), 0 (FALSE))` — 첫 스레드만 (`+0x3b CMPXCHG.LOCK [LdrpHotPatchInitialized]` 0 → 1 이 성공해야 `+0x45 JZ → +0x77`). 칸 값은 nt 가 정한 핸들 번호 (사진 8). 뒤 `+0xcc ~ +0xda LdrpInitializationComplete` 가 신호를 주고 둘째 스레드는 `+0xe4 ~ +0xf2 LdrpWaitForInitializationComplete` 로 기다림 (읽는 쪽). 쓴 주인 **nt (syscall)**, 부르는 쪽 ntdll `LdrpInitializeInternal`.
- **`RtlpHypervisorSharedUserVa`** (`0x1d3e98`, 8B): `LdrpInitializeProcess +0xb3c ~ +0xb4d` `NtQuerySystemInformation(클래스 0xc5, 버퍼, 8)` → `+0xb52 TEST EAX,EAX · +0xb54 JS → +0xb65` (실패면 안 씀) → `+0xb56 MOV RAX,[RSP+0x168]` · `+0xb5e MOV [칸],RAX`. 사진 `0x7ffe3000` (KUSD `0x7ffe0000` 뒤 3쪽 — nt 가 하이퍼바이저 공유 쪽을 유저 공간에 올리고 주소를 돌려줌 `[정적: 클래스 이름은 sast/40 이 PDB 로 `SystemHypervisorSharedPageInformation` 이라 적음]`). 쓴 주인 **nt (syscall)**.

#### 1j. 0 을 씀 둘 — `LdrpEnforceIntegrityContinuity` · `LdrpAuditIntegrityContinuity`

- `LdrpInitializeProcess +0x4a4 MOVAPS XMM1,[LdrSystemDllInitBlock+0xa0]` (커널이 K1 앞에 채우는 `MitigationOptionsMap` 24B, 3.x) → `+0x4c4 ~ +0x4d6` 둘째 qword (`+0xa8`) `>> 4 & 3` → `+0x4d9 CMP AL,BL` (`BL` = 1, `+0x475 MOV EBX,1`) · **`+0x4db SETZ [LdrpEnforceIntegrityContinuity]`** (= 1 이면 1) · `+0x4f5 CMP AL,3` · `+0x4f7 MOV AL,R14B` (`R14B` = 0) · `+0x4fc MOV AL,BL` · **`+0x4fe MOV [LdrpAuditIntegrityContinuity],AL`** (값 1 또는 3 이면 1). 두 줄 모두 무조건 실행 (바이트 쓰기). test.exe 입력 비트 0 → 둘 다 0 (사진 `끝까지 0`) — **0 을 씀 (입력값에서 계산)**.
- 읽는 쪽: `LdrpMapDllNtFileName +0x141 · +0x416` · `LdrpValidateIntegrityContinuity +0x90`. 둘째 검사가 켜지면 `LdrpMapDllNtFileName +0x407 → LdrpValidateIntegrityContinuity (+0x69) → LdrpSetModuleSigningLevel` 로 `NtGetCachedSigningLevel` 를 부른다 (sast/109 3.2 `SigningLevel` 줄).

#### 1k. 지금 우리 (PE_emulator)

- `LdrpInitializeProcess` 몸통은 진짜 코드를 Unicorn 에서 돌리므로 **직접 쓰는 줄은 이미 같은 값이 나온다**: `LdrpThunkSignature` 80B (`Test/LdrInitTest.cpp` `_ExpectCriticalThunks` — 스텁 5개 앞 16B 와 `0x1cdfa0 + 0x10 × i` 를 비교, 시작 전에 `0xcc` 로 덮어 둔 칸이 진짜 코드로 채워지는지 봄), `RtlpTimeout`·`RtlpTimeoutDisable` (`:1645 · :1647` 기대값 `0xffffe86d079b8000` · 1 = 사진 값), `LdrpEnforceIntegrityContinuity`·`LdrpAuditIntegrityContinuity` (`:1544 ~ :1545` 기대 0 · 0, 입력 비트를 켠 시험은 `:1578 ~ :1579` Enforce 0 · Audit 1) 등 맞다.
- **사진과 다른 곳 (알려진 차이)**: `RtlpLowFragHeapGlobalFlags` — 우리 시험 기대값 `0` (`:1546`) 은 LFH 켜기 앞 단계라 맞지만, 사진 P1a 의 `1` 은 위 1e 의 LFH 켜기 뒤 값이다. 프로세스 힙 할당 길을 넣을 때 `RtlpLowFragHeapRandomData` 32칸 (`RtlpHpHeapGlobals +0x40` 씨앗 → xorshift64\*) 도 같이 나온다 — 씨앗이 우리 쪽에서 다르면 값은 달라진다 (난수라 사진과 같게 맞출 필요 없음).
- 나머지 W 줄 (`RtlpEnvironmentHashTable` · `LdrpEnclave*` · `Feature_KVS…` · `RtlpHypervisorSharedUserVa` · `LdrpHotPatchInitCompleteEvent` · `LdrpStatistics` · `LoadAsDataCrits`) 은 **확인 필요** (코드 안 봄). `RtlpHypervisorSharedUserVa` 는 우리 `NtQuerySystemInformation` 이 클래스 `0xc5` 에 무엇을 돌려주는지 (사진 `0x7ffe3000`, 실패해도 `+0xb54 JS` 로 안 씀) 에 달렸다.

### 2. `LdrpPolicyBits` — 3.8 로 옮길까 (7.24 ①)

**답: 옮기자.** 근거 (`LdrpInitializePolicy.asm`, 호출 `LdrpInitializeProcess +0xfc5 (ret +0xfca)`):

| 쓰는 줄 | 쓰는 것 | 조건 (test.exe 는 둘 다 거짓) |
|---|---|---|
| `+0x7e MOV [LdrpPolicyBits],0x29` | 값 통째 (패키지 정책 처음 값) | `+0x42 MOV AL,[PEB+3]` · `+0x46 TEST AL,0x10` (**`IsPackagedProcess`**) · `+0x48 JNZ → +0x72` **그리고** `+0x72 CMP word [DllPath.Length],0 · +0x75 JZ → +0x4a` (경로가 비면 안 들어옴) |
| `+0x26b OR [..],0x4` · `+0x305 OR [..],0x40` · `+0x31d OR [..],0x2` · `+0x329 AND [..],0xfffffffe` | 앱 모델 정책 (`RtlQueryPackageClaims` 다섯 번 `+0xc3 · +0x14a · +0x1b9 · +0x228 · +0x2a2`) 에 따라 비트 켬·끔 | 위 갈래 안쪽 |

- 사진: 파일 `0x16` = P1a = P1b = P2 `0x16` (이름 칸 4B `0x1cd7fc`; 같은 qword 의 아래 4B `0x1cd7f8` = `0x72` 는 이름 밖 앞 칸). test.exe `PEB.BitField` = `0x4` (비트 4 꺼짐) `[관찰]` → `+0x48 JNZ` 안 뜀 → `+0x4e ~ +0x58` 에서 끝.
- 읽는 쪽 16곳 (`LdrpMapDllSearchPath +0xb5` · `LdrLoadDll +0x106` · `LdrpGetDllPath +0x81 · +0x1fe` · `LdrSetDllDirectory +0x6` 등) — 값 `0x16` 은 길 곳곳에서 읽힌다 (7.24 ① "쓰는 줄은 패키지 갈래뿐" 확인).
- **권고**: 요청서가 쓴 기준 ("쓰는 줄이 있으면 아는 칸, 조건부 안 탐도 가름") 이면 3.8 에 **조건부 안 탐 줄** 로 옮기고 7.3 `≤P1a` 에서 뺀다. 일관성: 같은 바퀴의 조건부 안 탐 줄 16개 (이 표의 11 + 조건부 힙 새 4 + `g_pShimEngineModule`; `LdrpPolicyBits` 포함) 를 같이 옮긴다 (요약 표 합치기 제안). 안 옮기고 "파일 값 그대로" 한 줄 묶음으로 두는 선택도 가능 — 오케스트레이터 결정.
- **지금 우리 (PE_emulator)**: **맞다.** `LdrpInitializePolicy` 를 공사 중 빈 함수로 두고 `LdrpPolicyBits` 는 ntdll 파일 값 `0x16` 그대로 쓴다 (`PE_emulator/_workspace/orchestrator/61_current-dir.md` R1 이 같은 근거) — test.exe 는 패키지 앱이 아니라서 진짜도 안 쓴다.

### 3. `EnvironmentVersion` 나머지 +2 — 누가, 언제

**답: ntdll `RtlpWow64ThunkEnvironmentTo64` (`RtlpInitEnvironmentBlock +0xa4 (ret +0xa9)` 가 부름) 안의 `RtlSetEnvironmentVar` 두 번.** 같은 `RtlpInitEnvironmentBlock` 호출 (`LdrpInitializeProcess +0xc2d (ret +0xc32)`) 안, 사진 구간 `P1~P1a`.

| 순서 | 줄 | 하는 일 | `EnvironmentVersion` (`ProcessParameters +0x3f8`, 옛 블록) |
|---|---|---|---|
| ① | `RtlpInitEnvironmentBlock +0x86 INC qword [RBX+0x3f8]` | 환경을 힙 블록으로 복사한 뒤 (`+0x45 RtlpAllocateEnvBlock` · `+0x62` 복사, 환경이 없으면 `+0x78 RtlCreateEnvironmentEx`) 한 번 올림 | 0 → 1 |
| ② | **`RtlpWow64ThunkEnvironmentTo64 +0x126 (ret +0x12b) RtlSetEnvironmentVar`** | 표 `Wow64EnvironmentThunkTable` (`.rdata` `0x173b30`, 줄 `0x28`B × 3) 첫 줄: **`ProgramFiles` ← `ProgramW6432` 값** (`+0xa5 RtlQueryEnvironmentVariable(ProgramW6432)` 이 성공해야; `+0xaf ~ +0xb8` 크기 검사) | 1 → 2 |
| ③ | 같은 `+0x126` 둘째 바퀴 | 표 둘째 줄: **`CommonProgramFiles` ← `CommonProgramW6432`** | 2 → 3 |
| — | 셋째 바퀴 (`PROCESSOR_ARCHITECTURE` ← `PROCESSOR_ARCHITEW6432`) | `PROCESSOR_ARCHITEW6432` 는 **WOW64 프로세스 환경에만** 있어 test.exe 에선 `RtlQueryEnvironmentVariable` 이 실패 → `+0xc4 JS → +0x12b` 로 건너뜀. (있으면 flag 0 이라 `+0xc6 CMP byte [RBX],R15B` 가 안 건너뛰고 `+0xdb RtlSetEnvironmentVar` 가 그 변수를 지운 뒤 `+0x126` 이 `PROCESSOR_ARCHITECTURE` 를 설정 → 두 번 더) | (그대로) |

- 표 내용은 `.rdata` 원본에서 읽음: 줄 0 `ProgramFiles`/`ProgramW6432`/flag 1 · 줄 1 `CommonProgramFiles`/`CommonProgramW6432`/flag 1 · 줄 2 `PROCESSOR_ARCHITECTURE`/`PROCESSOR_ARCHITEW6432`/flag 0 (`+0xc6 CMP byte [RBX],R15B` 가 flag 1 이면 지우기를 건너뜀).
- `RtlSetEnvironmentVar` 는 한 번 불릴 때 `EnvironmentVersion` 을 올리는 줄이 여럿 (`+0x6f8 ~ +0x6fb` · `+0x78f` · `+0x847` · `+0x871` · `+0x88f INC` · `$fin$0 +0x23`) 이지만 갈래마다 한 번이다 `[정적, 갈래 전체는 안 따라감]`. 지도 안 함수 중 이 칸 (`[reg+0x3f8]`) 을 쓰는 줄은 `RtlpInitEnvironmentBlock +0x86` 과 `RtlSetEnvironmentVar` (지도 `m7`) 둘뿐이다 (ntdll 전체 풀이: 나머지는 `RtlSetCurrentEnvironment +0x53` · `RtlSetEnvironmentStrings +0x76 · +0x108` 로 지도 밖 — 공개 API). 그래서 사진 `0 → 3` (P1~P1a, 새 블록에서 3) 과 줄 수 (1 + 2) 가 맞는다 `[정적 + 사진 간접]`.
- 환경 블록 `+0x80 Environment` 가 새로 할당돼 옮겨질 수도 있지만 (`RtlSetEnvironmentVar +0x6e3` 갈래) 그건 `RtlpInitParameterBlock` (`LdrpInitializeProcess +0xc3a (ret +0xc3f)`) 통째 복사 전, 옛 블록 안에서 일어난다 — 3.2 줄 설명 그대로.
- **지금 우리 (PE_emulator)**: **틀림 (알고 있는 차이) — 우리 1, 진짜 3.** 우리 환경 표에 `ProgramW6432` · `CommonProgramW6432` 가 없어서 진짜 `RtlpWow64ThunkEnvironmentTo64` 몸통이 `RtlQueryEnvironmentVariable` 실패로 건너뛴다 (PE_emulator `58_env-param-heap.md` Q1 '(가) 그대로' · 일 55 "다시 볼 것" 과 같은 얘기). 두 변수를 사진의 같은 값 (`ProgramFiles` = `ProgramW6432`) 으로 환경 표에 넣으면 진짜 몸통이 `+0x126` 을 두 번 불러 3 이 된다 `[정적 추정]` — 환경 블록 크기·`RtlpAllocateEnvBlock` 힙 늘이기 횟수 (`NtAllocateVirtualMemory`) 가 입력에 달리는 건 D31 로 이미 처리됨.

### 4. 간접 호출

**답: 이 칸들을 쓰는 줄이 간접 호출 대상 안에 있는 곳 둘, 나머지는 없음.**

| 자리 | 대상 | 쓰는 줄이 달린 칸 | 좁힌 결과 |
|---|---|---|---|
| `LdrpInitializePerUserWindowsDirectory +0x47 (ret +0x4c)` `indirect:cfg` (인자 `RCX` 가 함수 포인터) | `kernel32!TermsrvGetWindowsDirectoryW` — 호출자 `LdrpInitializeProcess +0x1995 ~ +0x199d (ret +0x19a2) LdrpInitializeKernel32Functions` 가 `+0x12b (ret +0x130) LdrGetProcedureAddressForCaller(kernel32, "TermsrvGetWindowsDirectoryW" (`.rdata` `0x173bc8`), …, 출력 = 인자 칸 `[RSP+0x188]`)` 로 담음 (같은 함수의 `+0xfa` 는 `BaseThreadInitThunk` → `Kernel32ThreadInitThunkFunction`, `+0x196` 은 `KernelbasePostInit` → 곧바로 `+0x1aa` 간접 호출) | `LdrpUserDllDirectories` (`LdrAddDllDirectory +0x17b → +0x1b5`) | 반환 길이가 0 이면 `+0x4c ADD EAX,EAX · +0x4e JNZ` 안 뛰어 `+0x50 XOR EAX,EAX` 에서 끝. 사진: 목록 머리 안 바뀜 → 길이 0 이었다 `[관찰 간접]`. 왜 0 인지 (터미널 서버 아님) 는 kernel32 안 → **동적 3** |
| `RtlInitializeHeapManager +0x67 (ret +0x6c)` `indirect:cfg` | `g_RegInfo+0x8` 가 가리키는 함수 (`SbSelectProcedure(0xabababab…)` 반환) | `RtlpHpAppCompatFlags` · `RtlpHpHeapFeatures` 칸 (후보 `RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit`) | 위 `+0x2f JZ → +0x6c` 에서 건너뜀 → 안 탐 (sast/40 D11 과 같음) |
| `Reevaluate +0xe0 · +0x106` `indirect:cfg` | `g_wil_details_ensureSubscribedToFeatureConfigurationChanges` · `…subscribeFeatureStateCacheToConfigurationChanges` | KVS·기능 상태 칸 (변경 구독) | 두 포인터 모두 0 (`끝까지 0` `[관찰]`) → 안 탐 |

- 그 밖 W·S 줄이 든 함수의 간접 호출 (`LdrpInitializeProcess +0x1b59 · +0x1cdf · +0x1d16` · `_LdrpInitialize +0x9a` 등) 은 이 칸들을 쓰지 않는다 (sast/109 가 이미 목록으로 닫음).
- **지금 우리**: 확인 필요 (`TermsrvGetWindowsDirectoryW` 를 우리가 어떻게 흉내 내는지 코드 안 봄 — 0 길이를 돌려주면 진짜와 같다).

### 5. 줄기와 경로

- **줄기** (칸 값을 정하는 함수까지 — 끝은 "더 부르는 게 없는 함수" 또는 syscall):
  - 직접: `LdrpInitializeProcess` 안 줄 (`+0x4db · +0x4fe` · `+0x6b5 · +0x73a · +0x76d` · `+0x77a ~ +0x7c9` · `+0x82e ~ +0x86e` · `+0xad3 ~ +0xaed` · `+0xb4d ~ +0xb5e`) · `RtlpCreateLowFragHeap +0x256` → `RtlpInitializeLfhRandomDataArray` · `LdrpCaptureCriticalThunks +0x95` · `RtlpInitEnvironmentBlock → RtlpSetupEnvironmentHashTable` · `RtlpWow64ThunkEnvironmentTo64 → RtlSetEnvironmentVar → RtlpClearEnvironmentHashTable` · `RtlpScanEnvironment → RtlpInsertEnvironmentHashTableEntry` · `RtlpAddDebugInfoToCriticalSection +0x12 ~ +0x356` · `LdrpInitMuiCrits → RtlInitializeCriticalSectionEx · RtlRunOnceComplete` · `LdrpUpdateStatistics` · `RtlpLookupCurDirSetting → NtOpenKey` · `SbUpdateSwitchContextBasedOnDll +0xfb`.
  - syscall: `NtCreateEvent` (`LdrpInitializeInternal +0x8e`) · `NtQuerySystemInformation 0xc5` (`LdrpInitializeProcess +0xb4d`) · `NtQueryVirtualMemory` (클래스 4, `LdrpCaptureCriticalThunks +0x67`) · `NtOpenKey` (`RtlpLookupCurDirSetting +0x5f`).
  - 기능 상태: `Feature_Servicing_DisallowSharedLockImplicitUpgrade…IsEnabled…` → `wil_details_IsEnabledFallback +0x32` → `…ReevaluateCachedFeatureEnabledState +0x3c` → `wil_details_GetCurrentFeatureEnabledState +0xb1` → `wil_details_AreDependenciesEnabled +0x32` → `…GetCachedFeatureEnabledState` (꼬리 `+0x18`) → `…Reevaluate +0xa0` (`CMPXCHG`) — 3.x 공용 부품 (sast/57 이미 닫은 평가 길).
- **줄기 밖 (이름만)**: `RtlpAllocateHeap +0xe5` → `RtlpPerformHeapMaintenance` → `RtlpActivateLowFragmentationHeap` (공용 `RtlAllocateHeap` 길 — 지도 `m8` 은 `RtlpSetHeapDebuggingInformation` 쪽 호출자만 보임) · `LdrpWorkCallback` (스레드 풀) · `RtlpComputeDllPath` 이상 · `RtlFlsSetValue` · `LdrAddLoadAsDataTable` 의 다른 호출자 · 조건부 안 탐 줄의 쓰는 함수 전부 (`LdrpInitializePolicy` · `AvrfMiniLoadDll` · `LdrpInitShimEngine` · `RtlCreateTagHeap` …) · 범위 밖 길 쓰는 함수 전부 (표 `쓰는 줄` 칸).
- **공용 부품 (한 번만)**: `RtlInitializeCriticalSectionEx` (7.x 6장) · `RtlRunOnceComplete` · `InterlockedPushListSList` / `RtlInterlockedPopEntrySList` · `RtlAllocateHeap` · `RtlQueryEnvironmentVariable`.
- **지금 우리**: 확인 필요 — 위 줄기의 직접 줄은 `LdrpInitializeProcess` 몸통이 진짜라 자동으로 같다. 지도 밖 출발 (LFH 켜기 · `LdrpWorkCallback` 일꾼) 은 우리 힙·스레드 풀 몸통에 달렸다.

## Q80 에 넘길 판단 (파일 값 대조)

방법: 이름 칸 바이트를 `ntdll.dll` 파일 구역 바이트 (포인터는 재배치 뒤) 와 사진 P1a·P1b·P2 로 견줬다.

| 결과 | `.data ≤P1a` 57 | `.mrdata` 4 | `.data P1a~P1b` 3 | 합 64 |
|---|---|---|---|---|
| **P1a = P1b = P2 = 파일 값** | 39 | 4 | 1 (`RtlpSearchPathMode`, 0 = 0) | **44** |
| P1a = 파일 값, 뒤에 바뀜 | 3 (`LdrpLoaderLock` 잠금 · `SbDynamicContextDllCount` 개수 칸 · `LdrpInitOnceLoadAsDataCrits` 0 → 2) | 0 | 2 (`LdrpStatistics` · `LoadAsDataCrits`, 파일 0 = P1a 0 → P1b 에 처음 생김) | **5** |
| P1a ≠ 파일 값 (코드가 씀) | 15 (② 13 · syscall 2) | 0 | 0 | **15** |

- **Q80 이 보는 `≤P1a` 61개 (`.data` 57 + `.mrdata` 4) 중 46개는 "P1a 값 = 파일 값"** (39 + 3 + 4). 그중 `LdrpInitOnceLoadAsDataCrits` 1개는 이 결과서가 `P1a~P1b` 로 정정하므로 (구간 라벨 정정) **정정 뒤 `≤P1a` 기준으론 45**. 이 모두 이 결과서가 **코드 근거 (쓰는 줄 없음 · 조건부 · 범위 밖 · 잠금 · 옆 칸)** 도 나란히 적었다 (표 `가름` = `파일값 그대로`). 그러니 Q80 은 "파일 처음 값 그대로" 쪽은 이 결과서로 이미 닫힌 셈이다 — 닫을지는 Q80 차례에 오케스트레이터가 판단.
- 나머지 15 는 파일 값 ≠ P1a 라 Q80 으로 안 닫히고 **쓰는 줄 (이 결과서)** 로 닫혔다. 그 15 모두 줄을 찾았다 — Q80 후보에서 빠진다.
- `.mrdata` 4: 전부 파일 값 (`__guard_check_icall_fptr` · `NtUserPfn` 은 K1~P1 읽기 불가라 P1a 부터 파일 값, `LdrpVectorHandlerList` · `__security_cookie_complement` 는 K1 부터 파일 값). 7.5 `NtUserPfn` 실마리가 맞다.
- 조건부 힙 전역 새 4 · sast/109 넘김 4 (`끝까지 0`): 파일 값 0 = 사진 0 (이름 칸).
- **지금 우리**: 맞을 가능성 큼 — ntdll 파일 `.data` 를 그대로 올리는 한 "파일 처음 값 그대로" 칸은 코드 없이 같다 (재배치만 맞으면). 확인 필요: 우리가 `.data` 를 구역 원본 `0x2000` 바이트 + 0 채움으로 올리는지 (표의 rva `≥ 0x1ce000` 은 파일 값 0).

## baggage.md 에 넣을 줄 (3장 양식)

단계 `②` 는 `LdrpInitializeProcess` 이하 (첫 스레드). 쓴 주인 모두 ntdll (표시한 곳만 nt). 근거 칸은 `sast111` · 사진 구간.

### 3.8 ntdll `.data` 전역 — 아는 칸 +20 (② 쓰는 줄 16 · syscall 2 · 0 을 씀 2)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `RtlCriticalSectionList` (`0x1cc8d0`, 16B) | ② | 디버그 정보를 단 임계구역들의 고리 머리 `Flink`·`Blink`. 파일 값은 자기 가리킴. P1a 에 항목 0 (`RtlpProcessHeapsLock`) · 1 (프로세스 힙 락) → P1b 항목 2 | 직접 (꼬리 잇기) | ntdll | `②… → RtlInitializeHeapManager (call +0x12a) → RtlInitializeCriticalSectionEx → RtlpAddDebugInfoToCriticalSection +0x353 · +0x356` | `[정적]` sast111 · 정확 · 사진 `≤P1a*` (뒤에 바뀜: 이름만) |
| `RtlCriticalSectionDebugSList` (`0x1cde90`, 16B) | ② | `SLIST_HEADER` — 정적 디버그 정보 8개를 한꺼번에 넣음 (깊이 8). P1a 깊이 6 → P1b 5 | 직접 | ntdll | `②… (LdrpInitializeProcess +0x78d · +0x7c4)`; 꺼내는 쪽 `RtlpAddDebugInfoToCriticalSection +0x12` | `[정적]` sast111 · 정확 · 사진 `≤P1a*` |
| `RtlpStaticDebugInfo` (`0x1d4020`, `0x180`B) | ② | 항목 8개 (`0x30`B) 를 한 줄로 이음 (`[i]` ← `i+1`, 끝 ← 0) | 직접 | ntdll | `②… (LdrpInitializeProcess +0x79e ~ +0x7b4)` | `[정적]` sast111 · 정확 · 사진 `≤P1a*` |
| `RtlpFlsContext` (`0x1cde30`, `0x58`B) | ② | `+0x0 ~ +0x47` 0, `+0x48`·`+0x50` 목록 머리 ← 자기 주소 (`+0x48`). P2 에 `+0x8`·`+0x48`·`+0x50`·`+0x58` 바뀜 | 직접 | ntdll | `②… (LdrpInitializeProcess +0x82e ~ +0x86e)`; 뒤 `RtlFlsSetValue +0x13b` | `[정적]` sast111 · 정확 · 사진 `≤P1a*` |
| `LdrpThunkSignature` (`0x1cdfa0`, `0x50`B) | ② | 스텁 5개 (`ZwOpenFile` · `ZwCreateSection` · `ZwQueryAttributesFile` · `ZwOpenSection` · `ZwMapViewOfSection`) 앞 16B 복사 | 직접 (복사) | ntdll | `②… → LdrpCaptureCriticalThunks (call +0x481) +0x95` (`NtQueryVirtualMemory` 클래스 4 `+0x67`) | `[정적]` sast111 · 정확 · 사진 `≤P1a*` (80B 일치) |
| `RtlpTimeout` (`0x1d2730`) · `RtlpTimeoutDisable` (`0x1d2728`) | ② | PEB `+0xc0` (−30일) 복사 / 그 값 < −1시간이면 1 | 직접 | ntdll (값 주인 nt — 3.1 `+0xc0` 줄) | `②… (LdrpInitializeProcess +0x6b5 · +0x73a · +0x76d)` | `[정적]` sast111 · 정확 · 사진 `≤P1a` |
| `RtlpLowFragHeapGlobalFlags` (`0x1d2218`) | ② | 첫 LFH 만들 때 비트 0 켬 (`1`) | 직접 | ntdll | `프로세스 힙 할당 → RtlpAllocateHeap +0xe5 → RtlpPerformHeapMaintenance +0x22 → RtlpActivateLowFragmentationHeap +0xde → RtlpCreateLowFragHeap +0x256` | `[정적]` sast111 · 정확 · 사진 `≤P1a` (줄은 지도 밖 출발) |
| `RtlpLowFragHeapRandomData` (`0x1d3420`, `0x100`B) | ② | 32칸 × 8B xorshift64\* (`RtlpHpHeapGlobals +0x40` 씨앗) | 직접 | ntdll | 위 `RtlpCreateLowFragHeap +0x25c → RtlpInitializeLfhRandomDataArray +0x4b` | `[정적]` sast111 · 정확 · 사진 `≤P1a*` |
| `RtlpEnvironmentHashTable` (`0x1d2840`, `0x20`B) | ② | 환경 변수 해시 표 머리 `{개수, 크기 0x400, 표 포인터, 스캔됨, 환경 포인터}` (P1a 개수 47) | 직접 | ntdll | `②… → RtlpInitEnvironmentBlock (call +0xc2d) → RtlpSetupEnvironmentHashTable (+0x9b); RtlpScanEnvironment +0x3b3 · +0x3cc` | `[정적]` sast111 · 정확 · 사진 `≤P1a` |
| `LdrpEnclaveList` (`0x1d4220`) · `LdrpEnclaveListLock` (`0x1d4240`, `0x28`B) | ② | 목록 머리 자기 가리킴 / 임계구역 (`DebugInfo −1` · `LockCount −1` · `SpinCount 0x20007d0`) | 직접 / 함수 (`RtlInitializeCriticalSectionEx`) | ntdll | `②… (LdrpInitializeProcess +0xad3 ~ +0xaed)` | `[정적]` sast111 · 정확 · 사진 `≤P1a` |
| `Feature_KVS_BugFixes_55830752__private_featureState` (`0x1d4f28`) | ② | 기능 상태 캐시 `0x40147` — `DisallowSharedLock…` 의 필요 기능으로 평가됨 | 직접 (`CMPXCHG`, 칸 주소는 기술자 `+0x0`) | ntdll | `②… (LdrpInitializeProcess +0xaad) … → wil_details_AreDependenciesEnabled +0x32 → …GetCachedFeatureEnabledState → …Reevaluate +0xa0` | `[정적]` sast111 · 정확 · 사진 `≤P1a` |
| `LdrpStatistics` (`0x1d2650`, 12B) | ② (P1a~P1b) | `{일감 수 최대, 일꾼 스레드 처리 수, 그 밖 처리 수}` = `{3, 3, 3}` | 직접 | ntdll | `②… LdrpDrainWorkQueue +0x15b · LdrpWorkCallback +0x64 → LdrpUpdateStatistics +0xe · +0x33 · +0x2b` | `[정적]` sast111 · 정확 · 사진 `P1a~P1b` |
| `LdrpInitOnceLoadAsDataCrits` (`0x1ce428`) · `LoadAsDataCrits` (`0x1d41c0`, `0x28`B) | ② (P1a~P1b) | 한 번만 초기화 표시 (2) / 임계구역 (`DebugInfo −1`) | 직접 (`CMPXCHG` · `RtlRunOnceComplete` / `RtlInitializeCriticalSectionEx`) | ntdll | `LdrpInitializeProcess +0x1aa4 → LdrpMapAndSnapDependency +0x5e → LdrpFindDllActivationContext +0x158 → LdrResFindResourceDirectory +0x70 → LdrResSearchResource +0x727 → LdrAddLoadAsDataTable +0x37 → LdrpInitMuiCrits +0x35 · +0x4f · +0x60` (최단 길, 줄은 동적) | `[정적]` sast111 · 정확 · 사진 `P1a~P1b` |
| `LdrpHotPatchInitCompleteEvent` (`0x1d2910`) | ② | 이벤트 핸들 (8) | syscall (`NtCreateEvent`, 칸 주소를 넘김) | **nt** | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal (+0x8e)` — 첫 스레드만 | `[정적]` sast111 · 정확 · 사진 `≤P1a` |
| `RtlpHypervisorSharedUserVa` (`0x1d3e98`) | ② | 하이퍼바이저 공유 쪽 유저 주소 (`0x7ffe3000`) | syscall (`NtQuerySystemInformation 0xc5`) | **nt** | `②… (LdrpInitializeProcess +0xb4d → +0xb5e)` | `[정적]` sast111 · 정확 · 사진 `≤P1a` |
| `LdrpEnforceIntegrityContinuity` (`0x1d2900`) · `LdrpAuditIntegrityContinuity` (`0x1d2918`) | ② | 시작 정보 `+0xa8` 비트 4~5 에서 계산한 바이트 (test.exe 0) | 직접 (0 을 씀 — 입력값) | ntdll | `②… (LdrpInitializeProcess +0x4db · +0x4fe)` | `[정적]` sast111 · 정확 · 사진 `끝까지 0` (7.5 줄 해결) |

- 이어서 **조건부 안 탐** 줄 (3.8 에 넣으면 "아는 칸" 으로 세는 선택, 7.24 ①): 아래 16개. 각 줄 `무엇을` = "파일 처음 값 그대로 (…)" 로, 쓰는 줄·조건은 §1 표 그대로.
  - `LdrpPolicyBits` (`LdrpInitializePolicy +0x7e · +0x26b · +0x305 · +0x31d · +0x329`, 패키지 앱) · `RtlpLargestLfhBlock` (`RtlInitializeHeapManager +0x186`) · `RtlpHpHeapFeatures` · `g_RegInfo` (`+0x8`) · `RtlpHpAppCompatFlags` · `RtlpGlobalTagHeap` · `RtlpDisableHeapLookaside` (IFEO) · `g_SxsMaxDeadActivationContexts` · `RtlpUnloadEventTraceExNumber` · `LdrpQueryProcessModuleInformationLoopDetectorCount` (IFEO 셋) · `ShowSnaps` (`NtGlobalFlag & 2`) · `RtlpDebugPageHeapTable` (AppVerifier) · `TppCleanupGroupMemberpNoPoolList` · `LdrpUserDllDirectories` (`TermsrvGetWindowsDirectoryW` 길이 0) · `RtlpHeapFailureInfo` · `g_pShimEngineModule`.
- **옆 칸 줄 (3.8 "목록 밖 자리" 로)**: `0x1cd804`~ (`SbDynamicContextDllCount` 5칸 표의 개수 칸, `SbUpdateSwitchContextBasedOnDll +0xfb · +0x2ba`) · `0x1d2358` (`RtlpSearchPathMode+0x8`, 레지스트리 키 핸들 캐시, `RtlpLookupCurDirSetting +0x6e`) · `0x1cc578` (`RtlpDebugPageHeapTable+0x8`, 읽기 `RtlpCreateHeap +0x116`, 쓰기 `LdrpInitializeProcess +0xcca` ← 0 · AppVerifier `LdrpInitializeApplicationVerifierPackage +0x117 · +0x1d8`) · `0x1d272c` (`RtlFailedCriticalDebugAllocations` ← 0, `LdrpInitializeProcess +0x773`).
- **파일 처음 값 그대로 (쓰는 줄 없음 / 범위 밖) 묶음 줄**: 21 + 14 = 35개는 3.8 에 새 줄 없이 7.3 `≤P1a` 에서 빼고 한 줄로 ("처음 값 = 파일, sast111 §1 표").
- **정정**: 3.8 `LdrpInitOnceLoadAsDataCrits` 구간 `P1a~P1b` · `RtlpDisableHeapLookaside`·`ShowSnaps`·`RtlpSearchPathMode` `끝까지 0` (구간 라벨 정정 표). `RtlpLowFragHeapGlobalFlags` 7.5 "LFH 켜기 범위 밖" 줄 닫음 (위 1e).

### 3.9 ntdll `.mrdata` 전역

- `__guard_check_icall_fptr` · `NtUserPfn` · `LdrpVectorHandlerList` · `__security_cookie_complement`: 4개 모두 **파일 처음 값 그대로** (쓰는 줄 없음 2 · 범위 밖 2). 3.9 에 한 줄 묶음 (7.5 `NtUserPfn` 실마리와 함께) 또는 7.3 `.mrdata` 에서 뺌 — `.mrdata` 모름 26 → 22.

### 3.2 `_RTL_USER_PROCESS_PARAMETERS` — `EnvironmentVersion` 줄 고침

- `| +0x3f8 EnvironmentVersion | ② | 환경을 별도 힙 블록으로 복사한 뒤 +1 (INC, RtlpInitEnvironmentBlock +0x86), 이어 RtlpWow64ThunkEnvironmentTo64 (call +0xa4) 가 RtlSetEnvironmentVar 를 두 번 (ProgramFiles ← ProgramW6432 · CommonProgramFiles ← CommonProgramW6432, 환경에 그 둘이 있을 때) → 합 +3 (사진 0 → 3). WOW64 부모 환경이면 PROCESSOR_ARCHITEW6432 줄이 더해져 +2 더 | 직접 (옛 블록, 이어서 통째 복사) | ntdll | ②… → RtlpInitEnvironmentBlock (call +0xc2d) → +0x86 · RtlpWow64ThunkEnvironmentTo64 (+0xa4) → RtlSetEnvironmentVar (+0x126) | [정적] sast111 · 정확 · 사진 P1~P1a |`
- 7.5 의 `EnvironmentVersion` 나머지 +2 줄 ("모름 — `RtlpSetupEnvironmentHashTable` 쪽 `[추측]`") 은 **닫힘 + 추측 정정**: 해시 표 함수가 아니라 WOW64 썽크.

## 동적으로 확인할 것

정적으로 닫힌 것: 표 64 + 그 밖 8 의 가름 전부 (`LdrpHeap` 는 이미 3.8), `EnvironmentVersion` +2, `LdrpPolicyBits`. 아래는 "정적으로 정하지 못한 것" 과 "읽기만 하는 확인".

1. **LFH 가 켜지는 첫 `RtlAllocateHeap` 호출** (정적 못 정함): `ntdll!RtlpPerformHeapMaintenance` 입구 (`rva 0x5cc30`) 를 test.exe 일 때만 멈추고 `k` — 첫 도착 콜스택 (`RtlpAllocateHeap +0xea` 위가 `LdrpInitializeProcess` 의 어느 `+0x…` 인지). 이어서 `RtlpCreateLowFragHeap +0x256` 직후 `dd ntdll!RtlpLowFragHeapGlobalFlags L1` = 1. (사진 P1a 에는 이미 1.)
2. **`LdrpInitMuiCrits` 첫 도착 콜스택** (P1a~P1b): `ntdll!LdrpInitMuiCrits` 입구 (`rva 0x479f0`) 를 test.exe 일 때만 멈추고 `k` — `LdrAddLoadAsDataTable +0x37` 인지 `LdrpGetFileSizeFromLoadAsDataTable +0x14` 인지.
3. **`LdrpInitializePerUserWindowsDirectory +0x4c` 에서 `r eax` = 0** (읽기만) 과 `ln @rax(+0x47 호출 대상)` = `kernel32!TermsrvGetWindowsDirectoryW` 확인. 멈춤점 `ntdll!LdrpInitializePerUserWindowsDirectory+0x47` (call 자리), test.exe 일 때만.
4. **`EnvironmentVersion` 단계**: `ntdll!RtlpInitEnvironmentBlock+0x8d` (INC 뒤) 에서 `dq @rbx+0x3f8 L1` = 1, `ntdll!RtlpWow64ThunkEnvironmentTo64+0x12b` 를 두 번 지날 때마다 2 → 3 (`+0x126` 가 call 자리, `+0x12b` 가 ret 자리). 그리고 `RtlQueryEnvironmentVariable(ProgramW6432)` 이 성공하는지 (`r eax` ≥ 0 at `+0xaa`).
5. (읽기만, 선택) `Feature_KVS_BugFixes_55830752__private_featureState` 쓰는 순간: `ntdll!wil_details_FeatureStateCache_ReevaluateCachedFeatureEnabledState+0xa0` 에서 `rcx` = `ntdll+0x1d4f28` 인 호출이 있는지 (DisallowSharedLock 평가 1번 + KVS 평가 1번).
6. (읽기만) `ntdll!LdrpPolicyBits` `0x16` 그대로 (sast/59 와 같은 항목 — 새 멈춤점 없음).
- test.exe 일 때만 멈추게: `bp /w "@$peb != 0 && ..."` 같은 프로세스 이름 조건 (`.process` 조건) — 새로 만들지 않고 sast/57 의 기존 조건을 그대로.

## 다음 요청 후보

- **Q78g2 (이어서)**: `끝까지 0` 전역 중 **이름 칸에 쓰는 줄이 지도 안 `m3~m10` 에 있는 것 — `.data` 39개 (아래 표) + `.mrdata` 14개** — "조건부 안 탐 / 0 을 씀" 으로 가를 후보 (이 바퀴가 닫은 8개 (`RtlpGlobalTagHeap` · `RtlpHpHeapFeatures` · `RtlpHpAppCompatFlags` · `g_RegInfo` · `LdrpModuleDatatableLock` · `LdrpEnforceIntegrityContinuity` · `LdrpAuditIntegrityContinuity` · `g_pShimEngineModule`) 는 뺐다). 첫 쓰는 줄만:

| 이름 | 첫 쓰는 줄 | 지도 깊이 | 지도 안 쓰는 줄 수 |
|---|---|---|---|
| `RuntimeGlobal` | `ExecuteHotpatchTestRuntimeFunction +0xd` | m7 | 2 |
| `MuiCacheSWRLock` | `LdrpGetFromMUIMemCache +0xce` | m10 | 1 |
| `RtlpCriticalSectionVerifier` | `AVrfpEnableVerifierOptions +0x8b` | m6 | 1 |
| `gLocaleTables` | `RtlpLoadNlsData +0x6e` | m8 | 3 |
| `pTblPtrs` | `RtlpLoadNlsData +0xd1` | m8 | 1 |
| `RtlpHpStackTrackingContext` | `RtlpHpStackTraceAddStack +0x62` | m9 | 2 |
| `UcIsTraceLoggingInitialized` | `RtlpHpHeapCreate +0x330` | m7 | 1 |
| `RtlpCachedPathLock` | `LdrSetDllDirectory +0x236` | m6 | 5 |
| `LdrpDllDirectoryLock` | `LdrSetDllDirectory +0xd6` | m6 | 5 |
| `RtlpUnloadEventTraceEx` | `LdrpRecordUnloadEvent +0x1f2` | m8 | 1 |
| `LdrpUnloadIndex` | `LdrpRecordUnloadEvent +0x47` | m8 | 1 |
| `LdrpUnloadIndexEx` | `LdrpRecordUnloadEvent +0x119` | m8 | 1 |
| `LdrpShouldCreateStackTraceDb` | `LdrpInitializeApplicationVerifierPackage +0xd8` | m6 | 2 |
| `LdrpForkActiveLock` | `_LdrpInitialize +0x336` | m3 | 1 |
| `LdrpForkConditionVariable` | `_LdrpInitialize +0x33d` | m3 | 1 |
| `LdrpCODScenarioTriggered` | `LdrpCheckComponentOnDemandEtwEvent +0x11f` | m9 | 1 |
| `CentennialIssueTracker_InitializedState` | `LdrpLogRelativePathWithAlteredSearchError +0x49` | m10 | 2 |
| `LdrpRedirectionModule` | `LdrpInitializeImportRedirection +0x18f` | m5 | 1 |
| `LdrpPotentialTlsLeaks` | `LdrpHandleTlsData +0x5a9` | m8 | 1 |
| `RtlFailedCriticalDebugAllocations` | `RtlpAddDebugInfoToCriticalSection +0x2bd` | m6 | 2 |
| `LdrpFatalHardErrorCount` | `LdrpMapDllNtFileName +0x213` | m8 | 3 |
| `LdrpUseImpersonatedDeviceMap` | `LdrpInitializeExecutionOptions +0x279` | m5 | 1 |
| `RtlpDllSearchPathWithOptions` | `LdrSetDllDirectory +0x21d` | m6 | 2 |
| `RtlpStackTraceDatabase` | `RtlpInitializeStackTraceDatabase +0x2d` | m6 | 1 |
| `RtlpHeapInvalidBreakPoint` | `RtlpBreakPointHeap +0xf` | m8 | 4 |
| `RtlpRaiseExceptionOnPossibleDeadlock` | `LdrpInitializeCriticalSectionExceptionGlobalMitigation +0x44` | m6 | 3 |
| `RtlpForceCSToUseEvents` | `LdrpInitializeProcess +0x9bb` | m4 | 1 |
| `RtlpModernAppKey` | `RtlOpenModernAppOptionsKey +0x77` | m6 | 1 |
| `LoadAsDataTableBlockCount` | `LdrAddLoadAsDataTable +0x179` | m9 | 3 |
| `LoadAsDataTable` | `LdrAddLoadAsDataTable +0x172` | m9 | 3 |
| `LoadAsDataTableCount` | `LdrAddLoadAsDataTable +0x142` | m9 | 2 |
| `AlternateResourceModules` | `LdrpSetAlternateResourceModuleHandle +0x310` | m10 | 4 |
| `AlternateResourceModuleCount` | `LdrpSetAlternateResourceModuleHandle +0x2ac` | m10 | 2 |
| `AltResMemBlockCount` | `LdrpSetAlternateResourceModuleHandle +0x317` | m10 | 4 |
| `AVrfpVerifierDllsString` | `AVrfInitializeVerifier +0x30f` | m5 | 1 |
| `AVrfpEnabledSystemWide` | `AVrfInitializeVerifier +0x353` | m5 | 1 |
| `AVrfpVerifierProvidersList` | `AVrfInitializeVerifier +0x6d` | m5 | 1 |
| `AVrfpVerifierFlags` | `AVrfInitializeVerifier +0x303` | m5 | 2 |
| `AVrfpVerifierStopMessageFunction` | `AVrfpVerifierStopInitialize +0x93` | m6 | 1 |

  - `.mrdata` `끝까지 0` 22 중 지도 안에 쓰는 줄이 있는 14: **shim 엔진 인터페이스 포인터 열** (`g_pfnSE_InitializeEngine` · `_InstallBeforeInit` · `_LdrResolveDllName` · `_GetProcAddressForCaller` · `_ProcessDying` · `_DllUnloaded` · `_LdrEntryRemoved` · `_ShimDllLoaded` · `g_pfnApphelpQueryModSettingsAllocProc` — 모두 `LdrpGetShimEngineInterface +0x4a9 ~ +0x559`, shim 엔진을 올릴 때만; run66 sast/61 이 같은 길의 둘 (`g_pfnApphelpCheckModuleProc` · `g_pfnSE_DllLoaded`) 을 닫음) + **AppVerifier 넷** (`AVrfpEnabled` · `AvrfpAPILookupCallbacksEnabled` · `AvrfpAPILookupCallbackRoutine` · `RtlGuardAllowSuppressedCalls`, `AVrfInitializeVerifier +0x1d3 ~ +0x1ea`) — 둘 다 조건부 안 탐 후보 (`g_ShimsEnabled` = 0 · `AvrfAppVerifierMode` = 0). 나머지 8 (`RtlpProtectedPolicies*` 셋 · `Wow64*` 다섯 · `LdrpUnalignedUserCallTargetBitMap`) 은 지도 안 쓰는 줄 없음.
  - 지도 밖에서만 쓰는 `.data` 이름 (`끝까지 0` 나머지 약 170) 은 "범위 밖 길 `[추측]`" 또는 "아무도 안 씀" 후보. `RtlFailedCriticalDebugAllocations` 는 이 바퀴가 `LdrpInitializeProcess +0x773` 도 0 을 쓰는 줄로 봤다 (표 위 "옆 칸 줄"). `RtlpForceCSToUseEvents` (`LdrpInitializeProcess +0x9bb`) · `LdrpForkActiveLock`·`LdrpForkConditionVariable` (`_LdrpInitialize +0x336 · +0x33d`) · `LdrpRedirectionModule` (`LdrpInitializeImportRedirection +0x18f`) 는 init 줄기 직접 줄이라 값이 0 이면 "0 을 씀" 후보.
- **Q80**: 위 판단 그대로면 `≤P1a` 61 중 46 (정정 뒤 45) 이 "파일 처음 값 그대로" 로, 15 는 쓰는 줄로 이미 닫혀 Q80 은 새 일 없이 닫을 수 있다 — 오케스트레이터가 Q80 차례에 결정.
- **PE_emulator 요청 후보**: (a) 환경 표에 `ProgramW6432` · `CommonProgramW6432` 추가 (`EnvironmentVersion` 3, 질문 3) — 일 55 "다시 볼 것"; (b) 프로세스 힙 할당 길 (LFH 켜기, 1e) — `RtlpLowFragHeapGlobalFlags` 1 과 난수 32칸.
- **정적 한 건 (작음)**: `LdrpInitMuiCrits` 의 호출 사슬을 동적 2 로 못 정하면 `LdrResSearchResource` 의 `+0x6e3 ~ +0x727` 조건 (경로로 찾는 갈래) 을 정적으로 읽어 첫 줄을 좁힐 수 있다 — 값은 이미 닫혀 있어 급하지 않음.

## 오프셋 대조

- **자동 대조**: 본문의 `함수이름 +0x…` 쌍 **308개** (같은 쌍이 여러 번 나오면 번번이 셈) 가 ① 함수 이름이 `ms/decomp/ntdll/_index.tsv` 에 있고 ② `함수 시작 + 오프셋` 이 objdump 풀이 `ntdll.dis` 의 **실제 명령 시작** 인지 스크립트로 확인 → **308개 모두 맞음 (불일치 0)**. call 자리와 ret 자리 (call 다음 줄) 둘 다 명령 시작이라 같은 방법으로 확인된다. 대상이 아닌 `+0x`: 구조체 칸 (`PEB+0xc0` · `[RBX+0x3f8]`) · `ntdll+0x…` (이미지 rva) · 데이터 이름 `+0x…` (`RtlpFlsContext+0x48` 등) · 같은 문단 앞 함수 이름을 이어받는 짧은 `+0x…` (예: "`LdrpInitMuiCrits` … `+0x35`").
- **수동 확인 (함수 이름이 길어 정규식이 못 잡은 것)**: `wil_details_FeatureStateCache_ReevaluateCachedFeatureEnabledState +0x3c (ret +0x41)` · `+0xa0 CMPXCHG.LOCK [R15],ESI` · `wil_details_GetCurrentFeatureEnabledState +0xb1 (ret +0xb6) → wil_details_AreDependenciesEnabled` · `wil_details_AreDependenciesEnabled +0x32 (ret +0x37) → …GetCachedFeatureEnabledState` · `…GetCachedFeatureEnabledState +0x18 JMP → Reevaluate` 를 `_calls.tsv` · `.asm` 줄로 다시 봤다.
- **알려진 사실 대조 (위 버전 확인)**: `_calls.tsv` 읽은 줄 그대로 일치.

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **제안 1**: `scripts/refs_many.sh` — `refs` 가 이름 한 개씩이라 이름 70여 개를 한꺼번에 훑으려면 objdump 풀이로 해야 했다. 이름 목록 파일 (한 줄에 이름 하나) 을 받아 이름 칸 `[주소, 주소 + 폭)` 의 `WRITE`/`RW`/`lea`/`READ` 줄과 지도 깊이 (`m0~m10`/`out`) 를 TSV 한 장으로 뽑으면 이런 가름 바퀴가 몇 분 안에 끝난다 (이번 `ntdll.dis` 풀이 `refs.py` 가 그 역할 — 레지스터 간접 쓰기는 못 봄이라 한계 표시 필요).
- **제안 2**: `globals.tsv` 의 "크기" 열이 "다음 이름까지 거리" 라 이웃 이름 없는 칸이 이름에 묻혀 7.3 구간 라벨이 틀어졌다 (`ShowSnaps` · `RtlpDisableHeapLookaside` · `LdrpInitOnceLoadAsDataCrits` · `RtlpSearchPathMode`). **코드 접근 폭 (첫 접근의 `BYTE/WORD/DWORD/QWORD PTR`) 을 열 하나로 더하면** 이런 오분류를 줄인다.
- **제안 3 (사소)**: `.asm` 전역 주석 (`RtlpDebugPageHeapTable+0x8` 처럼) 은 이웃 이름 + 칸 이라 다른 변수일 수 있다는 sast/40 메모를 SKILL.md 에 이미 적었다 — 이번에도 같은 사례 (`NormalizationListHead+0x68` = `0x1cc710` 이름 없는 칸, 값 `1 1`, 직접 참조 0).

## 판정

**분석성공.** 64개 가름 (이 길이 씀 18 · 파일 처음 값 그대로 46), 그 밖 8개 (+ `LdrpHeap` 확인), `LdrpPolicyBits` (3.8 로 옮기자), `EnvironmentVersion` +2 (`RtlpWow64ThunkEnvironmentTo64` → `RtlSetEnvironmentVar` ×2) 모두 줄로 닫음. 버전 대조 일치, PDB 일치 (ntdll `C3093720177DA6851519DCFFE8DF53FE1`, 커널 정보 파일 `C29EBFB06B78B3C020DCA66D99713F9E1`). dast 요청 필요: 위 "동적으로 확인할 것" 1 ~ 4 (읽기·멈춤점, 급하지 않음 — Q104 VM 단계에 얹을 것). 정적으로 못 닫은 것: LFH 켜지는 정확한 `RtlAllocateHeap` 호출 · `LdrpInitMuiCrits` 첫 도착 줄 (동적). 요청서 가설과 달랐던 것: ① 7.3 라벨 일부가 이웃 칸 때문이었고 (구간 라벨 정정 표), ② `EnvironmentVersion` +2 는 `RtlpSetupEnvironmentHashTable` 쪽이 아니라 WOW64 썽크 (7.5 `[추측]` 정정), ③ LFH 켜기는 범위 밖이 아니라 P1a 앞 (sast/40 "나중에 켜짐" 정정).

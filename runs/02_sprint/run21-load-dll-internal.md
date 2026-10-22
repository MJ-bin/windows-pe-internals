# 2026-10-23 run21 — `LdrpLoadDllInternal`: 빠른 끝 → 찾거나 준비 → 올리기 → 실행 준비 → 고정, 그리고 `LdrpPrepareModuleForExecution` 이 그래프 켜기를 건너뛰는 조건 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntdll (`ntdll.dll`, 26100.9457), pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (run17 ~ run20 과 같은 파일)
  - analyze 는 다시 안 했다. 이미 분석된 프로젝트에서 두 함수를 depth 1 로 뽑았다 (29개).
  - 버전 대조: **일치.** `_calls.tsv` 의 `LdrpLoadDll +0xfb → ret +0x100 LdrpLoadDllInternal`, `LdrpLoadDllInternal +0x27f → ret +0x284 LdrpPrepareModuleForExecution`, `LdrpPrepareModuleForExecution +0xea → ret +0xef LdrpInitializeGraphRecurse`, `LdrpInitializeProcess +0x1bb6 → ret +0x1bbb LdrpInitializeGraphRecurse` 가 run7·run8 콜스택 값과 모두 같다.
- 근거로 읽은 것
  - 본문: `LdrpLoadDllInternal` (rva `0x1fc30`, 1304바이트, 직접 호출 33줄 = 본체 29 + 예외 조각 4), `LdrpPrepareModuleForExecution` (rva `0x86e4c`, 307바이트, 호출 8줄).
  - 확인용: `LdrpLoadDll.asm` `+0xbd ~ +0xfb` (인자 다시 읽기), `LdrpInitializeProcess.asm` `+0x1351`, `+0x1a8e ~ +0x1aed`.
  - 지도 `_map/LdrInitializeThunk_edges.tsv` 는 **이름과 호출 자리만** 따라갔다 (callee 본문은 안 읽음).
  - 전역 참조 `_refs/LdrpMainThreadToken.tsv` (새로 만듦), `_refs/LdrInitState.tsv` (기존).
  - 구조체 칸 이름은 PDB 타입(`llvm-pdbutil`)으로: `_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`, `_LDR_DDAG_STATE`, `_LDR_DLL_LOAD_REASON`, TEB `SameTebFlags`.

## 목적

- 대기열 **Q11**. flow.md 2.5 `[공백]` "kernel32·kernelbase 를 메모리에 올리는 순간" 이 `LdrpLoadDllInternal` 안 **어느 호출 아래**인지 코드로 좁힌다.
- 덤으로: 둘째 `LdrLoadDll("KERNELBASE.dll")` 이 이 함수 안 어디서 끝나는지, 같은 `LdrpPrepareModuleForExecution` 이 kernel32 때는 그래프를 켜고(run7) exe 때는 안 켜는(run8) 까닭.

## 방법

- 정적: `_workspace/sast/16_load-dll-internal.md` — ntdll, `^(LdrpLoadDllInternal|LdrpPrepareModuleForExecution)$` depth 1 (+ 지도 이름 사슬, 전역 refs)
- 결과서: `_workspace/sast/16_load-dll-internal_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntdll/{LdrpLoadDllInternal,LdrpPrepareModuleForExecution,LdrpLoadDll,LdrpInitializeProcess}.asm`
  - `ms/disassemble/ntdll/_calls.tsv`, `ms/disassemble/ntdll/_map/LdrInitializeThunk_edges.tsv`, `ms/disassemble/ntdll/_refs/LdrpMainThreadToken.tsv`
- 오프셋 읽는 법 (run9 ~ 20 과 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**.
- 요청서는 `LdrpLoadDllInternal` 의 caller 를 "3" 이라 했지만, 지도 edges 로 확인된 caller 는 **둘**이다: `LdrpLoadDll +0x100`, `LdrpLoadForwardedDll +0x2e6` (결과서대로).

## 결과

한 줄로: **`LdrpLoadDllInternal` 은 "이미 있으면 바로 돌려주고, 없으면 자리를 만들어 실제로 올리고, 다 올리면 켜기 준비를 부르고, 프로세스 초기화 초반이면 못 내리게 고정하는" 함수다.** 실제 올리기(파일 열기·섹션 만들기·매핑)는 이 함수의 **`+0x284` 앞** 두 호출 아래에 있다.

- 비유: 택배 접수 창구. "이미 받은 물건인가?"(빠른 끝) → 아니면 송장 만들기(찾거나 준비) → 실제 배송(올리기) → 받는 집에 "도착했으니 전원 켜세요" 연락(실행 준비).

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpLoadDllInternal` | `+0xb3` | `+0xb8` | `LdrpFastpthReloadedDll` (이미 올라온 DLL 빠른 길 `[추측: 이름]`) | [정적] |
| 〃 | `+0x185` | `+0x18a` | `LdrpDrainWorkQueue(0)` (`LoadOwner` 가 아닐 때만) | [정적] |
| 〃 | `+0x19b` | `+0x1a0` | `LdrpThreadTokenSetMainThreadToken` | [정적] |
| 〃 | `+0x1bb` | `+0x1c0` | `LdrpDetectDetour` | [정적] |
| 〃 | `+0x1ed` | `+0x1f2` | `LdrpFindOrPrepareLoadingModule` (7 인자) | [정적] |
| 〃 | `+0x20b` | `+0x210` | `LdrpProcessWork(항목->LoadContext, 1)` (앞 결과가 `0xc0000135` 일 때만) | [정적] |
| 〃 | `+0x215` | `+0x21a` | `LdrpDrainWorkQueue(1)` (늘) | [정적] |
| 〃 | `+0x312` | `+0x317` | `LdrpThreadTokenUnsetMainThreadToken` (전역 토큰 ≠ 0 일 때) | [정적] |
| 〃 | `+0x23b` | `+0x240` | `LdrpHandleReplacedModule` (항목이 있으면 늘) | [정적] |
| 〃 | `+0x33c` | `+0x341` | `LdrpFreeReplacedModule` (항목이 바뀌었을 때) | [정적] |
| 〃 | `+0x25d` | `+0x262` | `LdrpCondenseGraph(항목->DdagNode)` (`LoadContext ≠ 0` 일 때) | [정적] |
| 〃 | `+0x27f` | `+0x284` | `LdrpPrepareModuleForExecution(항목, 상태 칸)` (상태 ≥ 0 일 때) | [정적] + [26100.9457 확인] run7 |
| 〃 | `+0x290` | `+0x295` | `LdrpBuildForwarderLink(부모 항목, 항목)` | [정적] |
| 〃 | `+0x332` | `+0x337` | `LdrpPinModule(항목)` (**`LdrInitState == 0` 일 때만**) | [정적] |
| 〃 | `+0x2c3` | `+0x2c8` | `LdrpFreeLoadContextOfNode(DdagNode, 상태 칸)` | [정적] |
| 〃 | `+0x2e6` / `+0x2ee` | `+0x2eb` / `+0x2f3` | `LdrpDecrementModuleLoadCountEx` / `LdrpDereferenceModule` (실패일 때) | [정적] |
| 〃 | `+0x416` | `+0x41b` | `LdrpDropLastInProgressCount` (`BL == 0` 일 때) | [정적] |
| 〃 | `+0x441`·`+0x4b1`·`+0x3ac`·`+0x50f` | `+0x446`·`+0x4b6`·`+0x3b1`·`+0x514` | `LdrpFindLoadedDllByHandle`·`LdrpQueryCurrentPatch`·`LdrpApplyPatchImage`·`LdrpDereferenceModule` (이유 9 = 패치 전용) | [정적] |
| `ntdll!LdrpPrepareModuleForExecution` | `+0x12a` | `+0x12f` | `LdrpCondenseGraph` (노드 `State` 5 일 때) | [정적] |
| 〃 | `+0x6a` | `+0x6f` | `LdrpAddNodeServiceTag` (조건부) | [정적] |
| 〃 | `+0x76` | `+0x7b` | `LdrpNotifyLoadOfGraph` | [정적] |
| 〃 | `+0x88` | `+0x8d` | `LdrpDynamicShimModule` | [정적] |
| 〃 | `+0xd1` | `+0xd6` | `LdrpAcquireLoaderLock` (아래 조건일 때만) | [정적] |
| 〃 | `+0xea` | `+0xef` | `LdrpInitializeGraphRecurse(노드, 상태 칸, &0)` | [정적] + [26100.9457 확인] run7 |
| 〃 | `+0xf9` | `+0xfe` | `LdrpReleaseLoaderLock` | [정적] |

- 잡일: `LdrpLoadDllInternal` 에 `LdrpLogInternal` 6곳 + `LdrpFreeUnicodeString` 1곳(이유 9 만), 예외 조각 `$fin$0` 에 `LdrpLogError` 1곳. `LdrpPrepareModuleForExecution` 에 `LdrpLogInternal` 1곳(`+0xbd`, shim 실패).
- 예외 조각 `LdrpLoadDllInternal$fin$0` (rva `0x1683b0`): 예외일 때 상태 `0xc0000005`, `LdrpLogError` → `LdrpDrainWorkQueue(1)` → (토큰 있으면) `LdrpThreadTokenUnsetMainThreadToken` → (`BL == 0` 이면) `LdrpDropLastInProgressCount`. 본체 정리와 같은 순서의 마무리 세트. [정적]

### 정적 — `LdrpLoadDllInternal` 순서와 갈림 (이유 = 4, `LdrLoadDll` 길)

| 순서 | ret | 하는 일 | 갈림 | 표시 |
|---|---|---|---|---|
| 1 | `+0x55`, `+0x84` | 이름 로그 2개. 결과 항목 칸 `*param_7 = 0`, 지역 항목 칸 = 0 | — | [정적] |
| 2 | (`+0x9e`) | 이유 == 9 (`PatchImage`) 면 3 을 건너뜀 | 이유 4 는 안 뜀 | [정적] |
| 3 | **`+0xb8`** | `LdrpFastpthReloadedDll(이름, 플래그, 부모 항목, 결과 항목 칸)` | **`eax ≥ 0` 또는 `0xc00004c0` 이면 빠른 끝**: 상태 칸에 쓰고 마지막 로그 2개 뒤 `RET`. 아래 전부(`+0x284` 포함)를 건너뜀. 그 밖의 음수면 느린 길 `+0x15f` | [정적]. "이미 올라온 DLL 을 빨리 돌려줌" 은 `[추측: 이름·callee 이름]` |
| 4 | `+0x18a` | TEB `SameTebFlags & 0x1000` (PDB `LoadOwner`) 가 꺼져 있으면 `LdrpDrainWorkQueue(0)`. 켜져 있으면 `BL = 1` 로 두고 건너뜀 | — | [정적] |
| 5 | `+0x1a0` | `LdrpThreadTokenSetMainThreadToken()` (인자 없음) | 이유 9 면 패치 갈래로 | [정적] |
| 6 | (`+0x1a1 ~ +0x1b5`) | 부모 항목이 있고 `BL == 0` 이고 부모 `DdagNode->LoadCount == 0` 이면 상태 `0xc0000135` 로 7·8 건너뜀 | `LdrLoadDll` 길은 부모 항목 0 이라 안 걸림 | [정적] |
| 7 | `+0x1c0` | `LdrpDetectDetour()` | — | [정적] |
| 8 | **`+0x1f2`** | `LdrpFindOrPrepareLoadingModule(이름, 경로 구조체, 플래그, 이유, param_5, &지역 항목, 상태 칸)` | `0xc0000135` → 9. `0xc000022d` 또는 0 이상 → 10. 그 밖의 음수 → 상태 칸에 쓰고 10 | [정적]. 역할 "목록에서 찾고, 없으면 새 항목을 만들어 '이제 올려라' 를 알림" 은 `[추측: callee 이름]` |
| 9 | **`+0x210`** | `LdrpProcessWork(지역 항목->LoadContext, 1)` | 8 이 `0xc0000135` 일 때만 | [정적]. 실제 올리기 `[추측: 지도 이름 사슬]` (아래) |
| 10 | `+0x21a` | `LdrpDrainWorkQueue(1)` | **늘** (9 를 안 탄 길도) | [정적] |
| 11 | `+0x317` | 전역 `LdrpMainThreadToken ≠ 0` 이면 `LdrpThreadTokenUnsetMainThreadToken()` | — | [정적] |
| 12 | (`+0x22f`) | 지역 항목이 NULL 이면 상태 `0xc0000017` 후 정리로 | — | [정적] |
| 13 | `+0x240` | `LdrpHandleReplacedModule(항목)` → 결과 항목 칸 | 바뀌었으면 `+0x341 LdrpFreeReplacedModule` 후 교체 | [정적] |
| 14 | `+0x262` | `LdrpCondenseGraph(항목->DdagNode)`, 뒤에 `LoadContext +0xc8 = 0` | `항목->LoadContext(+0xb0) ≠ 0` 일 때만 | [정적] |
| 15 | **`+0x284`** | `LdrpPrepareModuleForExecution(항목, 상태 칸)` → 결과를 상태 칸에 | **상태 칸 ≥ 0 일 때만** (`+0x274`) | [정적] + [26100.9457 확인] run7 |
| 16 | `+0x295` | `LdrpBuildForwarderLink(부모 항목, 항목)` → 상태 칸 | 15 가 0 이상일 때 | [정적] |
| 17 | **`+0x337`** | `LdrpPinModule(항목)` | **`+0x29b` `LdrInitState == 0` 일 때만**. 이 함수에서 `LdrInitState` 를 읽는 곳은 여기 하나 | [정적] |
| 18 | `+0x2c8` | `LdrpFreeLoadContextOfNode(DdagNode, 상태 칸)` | — | [정적] |
| 19 | `+0x2eb`, `+0x2f3` | 실패면 결과 항목 칸 = 0, `LdrpDecrementModuleLoadCountEx`, `LdrpDereferenceModule` | 상태 < 0 일 때 | [정적] |
| 끝 | `+0x41b` → `+0x113`·`+0x143` | `BL == 0` 이면 `LdrpDropLastInProgressCount` → 마지막 로그(`Status: 0x%08lx`) → `RET` | — | [정적] |

- 반환형은 void. **결과는 칸 두 개로 나간다**: 결과 항목 칸(`param_7`), 상태 칸(`param_8`). 상태 칸에 쓰는 자리: `+0xd0`(빠른 끝), `+0x30b`, `+0x31c`, `+0x35f`, `+0x372`, `+0x284` 결과, `+0x295` 결과. → run20 의 "`LdrpLoadDll` 은 상태 칸 값을 돌려준다 (칸에 실제로 쓰는지는 Q11)" 에서 **쓰는 쪽이 확인됐다.** [정적]
- 넷째 인자(`R9D = 4`)는 `9` 와 비교되는 곳이 6곳(`+0x9e`, `+0xda`, `+0x18a`, `+0x2a8`, `+0x2d1`, `+0x356`)이고, 새 항목의 `LoadReason(+0x10c) == 9` 와도 대조된다. PDB `_LDR_DLL_LOAD_REASON` 의 9 = `PatchImage`. 그래서 이 자리가 **적재 이유**로 쓰인다는 것이 코드 모양으로 뒷받침된다 [정적]. 4 의 이름 `DynamicLoad` 는 값이 같다는 것뿐 `[추측]`.

### 정적 — 인자 9개가 가는 곳 (`LdrpLoadDll +0xfb` → 이 함수)

| 인자 | `LdrLoadDll` 길 값 | 넘기는 곳 |
|---|---|---|
| `RCX` 이름 | 전처리된 이름 | 로그, `+0xb3 Fastpth` 1번째, `+0x1ed FindOrPrepare` 1번째 |
| `RDX` 경로 구조체 | `LdrLoadDll` 의 `[RSP+0x40]` | `+0x1ed FindOrPrepare` 2번째만 (`Fastpth` 에는 안 감) |
| `R8D` 플래그 | 플래그 | `Fastpth` 2번째, `FindOrPrepare` 3번째 |
| `R9D` 이유 | 4 | `== 9` 비교 6곳, `FindOrPrepare` 4번째 |
| 5번째 | 0 (패치용 기반 항목) | `FindOrPrepare` 5번째, 이유 9 일 때 `+0x514 LdrpDereferenceModule` |
| 6번째 | 0 (부모 항목) | `Fastpth` 3번째, 부모 `LoadCount` 검사, `+0x295 LdrpBuildForwarderLink` 1번째 |
| 7번째 | 결과 항목 칸 | `Fastpth` 4번째, `+0x240` 에서 씀, 실패 때 0 |
| 8번째 | 상태 칸 (`LdrpLoadDll [RSP+0x50]`, `LdrpPreprocessDllName` 결과가 이미 들어 있음) | `FindOrPrepare` 7번째, `+0x284 Prepare` 2번째, `+0x2c8 FreeLoadContextOfNode` 2번째 |
| 9번째 | 0 | 이유 9 일 때만 `+0x446 LdrpFindLoadedDllByHandle` |

- 경로 구조체(`RDX`)가 `FindOrPrepare` 로만 가므로, run20 이 남긴 `LdrLoadDll` "경로 해제" 바이트(`[RSP+0xbc]`)를 켜는 곳은 이 함수 안이 아니라 그 아래일 것 `[추측]` (이 함수에 그 칸 쓰기가 없음 — 결과서가 따로 답하지 않아 남김).

### 정적 — 지도 이름 사슬: 올리기는 어디서 (본문 안 읽음, 이름·호출 자리만)

`_map/LdrInitializeThunk_edges.tsv` 의 caller → callee 줄, 라벨은 ret 자리.

- **(가) KnownDll 길 — `\KnownDlls` 섹션 열기 + 매핑** (깊이 5)
  `LdrpLoadDllInternal +0x1f2` → `LdrpFindOrPrepareLoadingModule +0x5e5` → `LdrpLoadKnownDll +0xf1` → `LdrpMapDllWithSectionHandle +0x4e` → `LdrpMinimalMapModule +0x179` → `NtMapViewOfSection`
  - 옆 가지: `LdrpLoadKnownDll +0x63` → `LdrpFindKnownDll +0xd9` → `NtOpenSection`, `LdrpLoadKnownDll +0xfd` → `NtClose`. 이 길엔 `NtOpenFile`·`NtCreateSection` 이 없다.
- **(나) 파일 경로 길 — 파일 열기 + 섹션 만들기 + 매핑** (깊이 4 ~ 6)
  `LdrpLoadDllInternal +0x210` → `LdrpProcessWork +0x6a` → `LdrpMapDllSearchPath +0x25c` → `LdrpMapDllNtFileName` → `+0x137 NtOpenFile` / `+0x194 NtCreateSection` / `+0x28c LdrpMapDllWithSectionHandle +0x4e` → `LdrpMinimalMapModule +0x179` → `NtMapViewOfSection`
  - `LdrpProcessWork` 의 다른 갈래 이름: `+0x144 LdrpMapDllFullPath`, `+0x14e LdrpMapDllRetry`, `+0x158 LdrpMapDllPatchImage`, `+0x131 LdrpSnapModule`.
- 두 길 모두 **`LdrpMapDllWithSectionHandle` 에서 만난다** (지도상 caller 셋: `LdrpLoadKnownDll`, `LdrpMapDllNtFileName`, `LdrpMapDllPatchImage`). run20 이 본 `LdrpInsertDataTableEntry` 호출자 중 하나인 `LdrpMapDllWithSectionHandle +0x141` 이 이것이다 → **매핑한 뒤 모듈 목록에 끼우는 자리**도 이 함수 안 `[정적: 지도]`.
- **KnownDll 쪽(`LdrpLoadKnownDll`/`LdrpFindKnownDll`)은 이 길 위에 있다**: 예. 어떤 DLL 이 KnownDll 로 가는지는 정적으로 못 정함.
- **(다) 의존 DLL 을 처리하는 함수까지** (지도에 있는 이름: `LdrpLoadDependentModuleInternal`, `LdrpLoadDependentModuleW`, `LdrpMapAndSnapDependency`, `LdrpSnapModule`. `LdrpLoadDependentModule` 단독 이름은 없음)
  - 길 1: `… LdrpLoadKnownDll +0xf1` → `LdrpMapDllWithSectionHandle +0x1c8` → `LdrpMapAndSnapDependency` → `+0x34e LdrpLoadDependentModuleInternal` / `+0x4c2 LdrpSnapModule` / `+0x5d8 LdrpLoadDependentModuleW`(→ `+0xae LdrpLoadDependentModuleInternal`)
  - 길 2 (깊이 3): `LdrpLoadDllInternal +0x210` → `LdrpProcessWork +0x131` → `LdrpSnapModule +0x5df` → `LdrpLoadDependentModuleInternal`
  - `LdrpLoadDependentModuleInternal` 안에 다시 `+0x1792 LdrpLoadKnownDll`, `+0x1cae LdrpProcessWork`, `+0x182c LdrpQueueWork`, `+0x115c LdrpAllocateModuleEntry`, `+0x16ee LdrpFindLoadedDllByName` → **의존 DLL 처리가 같은 올리기 함수로 되돌아오는 재귀 모양**.
  - `LdrpMapAndSnapDependency` 는 run19 가 본 `LdrpInitializeProcess +0x1aa4`(exe import) 와 **같은 함수**다.

### 정적 — `LdrpPrepareModuleForExecution` 순서와 갈림

| 순서 | 자리 | 하는 일 | 갈림 | 표시 |
|---|---|---|---|---|
| 1 | `+0x20 ~ +0x2b` | 현재 스레드 ID 가 전역 `LdrpDllNotificationLock +0x10` (임계 구역 `OwningThread` 자리) 과 같으면 | **바로 `return 0`** | [정적]. "DLL 알림 콜백 안에서 다시 들어옴 막기" `[추측]` |
| 2 | `+0x31 ~ +0x40` | 노드 `DdagNode->State(+0x38)` 로 갈림 | 5 `Snapped` → `+0x12f LdrpCondenseGraph` 뒤 3 / 6 `Condensed` → 3 / 7 `ReadyToInit` → 6 / **8 `Initializing`·9 `ReadyToRun` → 바로 `return 0`** / 그 밖 → `0xc00000e5` | [정적] |
| 3 | `+0x6f` | `LdrpAddNodeServiceTag(노드)` | 항목 `Flags` 비트 5 (`ProcessStaticImport`) 꺼짐 + TEB `SubProcessTag ≠ 0` | [정적] |
| 4 | `+0x7b` | `LdrpNotifyLoadOfGraph(노드)` | 음수면 그 값으로 끝 | [정적] |
| 5 | `+0x8d` | `LdrpDynamicShimModule(노드)` | 음수면 `+0xbd` 로그 후 끝 | [정적] |
| 6 | **`+0xbf ~ +0xcf`** | **`항목->LoadContext(+0xb0) ≠ 0` 이고 `LoadContext +0x20` 비트 0 이 0** 이어야 7 로 | 아니면 지금 값으로 끝 (`+0xcf JNZ +0xfe`) | [정적] |
| 7 | `+0xd6` → **`+0xef`** → `+0xfe` | `LdrpAcquireLoaderLock` → **`LdrpInitializeGraphRecurse(노드, 상태 칸, &바이트 0)`** → `LdrpReleaseLoaderLock(…, EDX=2, …)` | — | [정적] + `+0xef` 는 [26100.9457 확인] run7 |

- **`+0xea` 를 안 타는 조건** (코드에 쓰인 것 전부): (a) 알림 락 주인 스레드면, (b) 노드 상태 8·9 이거나 5 ~ 9 밖, (c) 알림·shim 이 음수, (d) `LoadContext == 0`, (e) `LoadContext +0x20` 비트 0 이 1.
- **두 caller 가 다르게 도는 이유 = (e)** [정적]:
  - `LdrpInitializeProcess` 는 exe 항목을 만들 때 넘긴 로드 문맥(지역 `[RSP+0x2b0]`, `+0x1351 LEA` → `LdrpAllocateModuleEntry`, run19) 의 `+0x20` 칸 = `[RSP+0x2d0]` 에 **`+0x1a94 OR dword ptr [RSP+0x2d0], R12D`** (`+0x1a8e R12D = 1`) 로 비트 0 을 켠다.
  - 그 **뒤**에 `+0x1aa4 LdrpMapAndSnapDependency` 와 `+0x1aed LdrpPrepareModuleForExecution(exe 항목, …)` 을 부른다. 그래서 exe 로 부른 `Prepare` 는 `+0xcf` 에서 그래프 켜기를 건너뛰고, 켜기는 `LdrpInitializeProcess` 가 직접 `+0x1bbb LdrpInitializeGraphRecurse` 로 한다. → run8 이 dll1·dll2 켜기를 `+0x1bbb` 아래에서 본 것 `[관찰]` 과 맞는다.
  - `LdrpLoadDllInternal` 쪽 로드 문맥은 `LdrpFindOrPrepareLoadingModule` 이 만든 것이고, 두 함수 안에서 그 비트를 켜는 줄은 없다 `[정적, 이 두 함수 안]`. 비트가 0 이면 켠다 → run7 이 `+0xef` 에서 켜기를 본 것 `[관찰]` 과 어긋나지 않는다. 비트 값 자체는 callee 가 정하니 동적.
  - 비트 0 의 이름은 PDB 에 없다 (`_LDRP_LOAD_CONTEXT` 는 칸 정보 없는 앞선 선언). 뜻 "켜기는 호출자가 따로 한다" 는 `[추측]`.
  - 로드 락 해제의 `EDX` 가 여기선 2, `LdrpInitializeProcess +0x1bc7`(call) 에선 9 `[정적]`. 호출 자리 표시 값 `[추측]`.

### 정적 — 둘째 `LdrLoadDll("KERNELBASE.dll")` 은 어디서 끝나나

정적으로는 **두 길이 다 가능**하고 고를 수 없다.

- (가) **`+0xb8` 빠른 끝.** `LdrpFastpthReloadedDll` 이 0 이상을 주면 바로 `RET`. `+0x284` 까지 안 간다. callee 이름(`LdrpFindLoadedDllByName`, `LdrpIncrementModuleLoadCount`)으로는 이쪽이 어울린다 `[추측]`.
- (나) **느린 길에서 `FindOrPrepare` 가 이미 있는 항목을 찾음** (0 이상, `0xc0000135` 아님) → `ProcessWork` 안 탐 → `+0x284` 까지 간다. 하지만 `Prepare` 안에서 노드 `State` 가 9 면 `+0x40` 에서 바로 `return 0` → `+0xef` 도 `LdrpCallInitRoutine` 도 안 불린다.
- 둘 다 run7 의 "둘째 호출에서는 시작 함수가 안 불렸다" `[관찰]` 과 맞는다. 가르는 건 `+0xb8` 의 `eax` → 동적 Q49.
- (나)라면 `+0x29b` 에서 `LdrInitState == 0` 이라 `+0x337 LdrpPinModule` 이 다시 불릴 수 있다 `[정적]`.

### 정적 — `LdrpDrainWorkQueue`·메인 스레드 토큰은 kernel32 준비 때도 타나

- 이 함수 코드에 `LdrInitState` 조건은 `+0x29b` 하나뿐이다 (`_refs/LdrInitState.tsv`). Drain(`+0x18a`, `+0x21a`) 과 토큰 Set(`+0x1a0`) 은 `LoadOwner` 비트와 이유 9 에만 갈린다. → `LdrLoadDll("KERNEL32.DLL")` 도 이 호출 자리를 **지나가는 코드 모양**이다 (`LdrpEnableParallelLoading +0x1a42` 앞이라도). [정적]
- Unset(`+0x317`) 은 전역 `LdrpMainThreadToken ≠ 0` 일 때만. 이 전역은 `LdrpThreadTokenSetMainThreadToken +0x29` 에서만 쓰이고 `…UnsetMainThreadToken +0x1f` 에서 0 으로 되돌아간다. 읽는 곳: 이 함수 `+0x21a`·`$fin$0 +0x3d`, `LdrpInitializeProcess +0x1ab4`, `LdrpWorkCallback +0xa1`/`+0xc4`, `TppWorkpExecuteCallback +0x2c5`/`+0x43c` (`_refs/LdrpMainThreadToken.tsv`). Set 이 이 전역을 **조건 없이 쓰는지**는 callee 본문이라 모른다 → 동적 Q49.
- `LdrpInitializeProcess` 도 같은 짝을 쓴다: `+0x1a8e Set` … `+0x1ab1 Drain(1)` … `+0x1ab4` 읽기 → Unset (run19 의 `+0x1a8e`·`+0x1ab1`).

### 호출 관계

```
LdrpLoadDll +0x100   (caller 둘: LdrpLoadDll +0x100, LdrpLoadForwardedDll +0x2e6 — 지도)
 └─ LdrpLoadDllInternal (0x1fc30)     이유 = 4 / 결과: 결과 항목 칸, 상태 칸
      ├─ (1) +0xb8   LdrpFastpthReloadedDll   ── eax ≥ 0 또는 0xc00004c0 → 로그 뒤 RET (아래 전부 건너뜀)
      ├─ (2) +0x18a  LdrpDrainWorkQueue(0)          LoadOwner 가 아닐 때만
      ├─ (3) +0x1a0  LdrpThreadTokenSetMainThreadToken
      ├─ (4) +0x1c0  LdrpDetectDetour
      ├─ (5) +0x1f2  LdrpFindOrPrepareLoadingModule
      │      └─ (지도) +0x5e5 LdrpLoadKnownDll → +0xf1 LdrpMapDllWithSectionHandle → +0x4e LdrpMinimalMapModule → +0x179 NtMapViewOfSection
      ├─ (6) +0x210  LdrpProcessWork(LoadContext, 1)  (5) 가 0xc0000135 일 때만
      │      └─ (지도) +0x6a LdrpMapDllSearchPath → +0x25c LdrpMapDllNtFileName → NtOpenFile / NtCreateSection / LdrpMapDllWithSectionHandle
      │         (지도) +0x131 LdrpSnapModule → +0x5df LdrpLoadDependentModuleInternal (의존 DLL)
      ├─ (7) +0x21a  LdrpDrainWorkQueue(1)          늘
      ├─ (8) +0x317  LdrpThreadTokenUnsetMainThreadToken   토큰 ≠ 0
      ├─ (9) +0x240  LdrpHandleReplacedModule       (바뀌었으면 +0x341 LdrpFreeReplacedModule)
      ├─ (10) +0x262 LdrpCondenseGraph               LoadContext ≠ 0
      ├─ (11) +0x284 LdrpPrepareModuleForExecution    상태 ≥ 0                          [관찰] run7
      │     ├─ (State 5 일 때) +0x12f LdrpCondenseGraph       State 8·9 → return 0
      │     ├─ +0x6f  LdrpAddNodeServiceTag   (조건부)
      │     ├─ +0x7b  LdrpNotifyLoadOfGraph
      │     ├─ +0x8d  LdrpDynamicShimModule
      │     └─ LoadContext ≠ 0 && LoadContext+0x20 비트 0 == 0 일 때만
      │          ├─ +0xd6  LdrpAcquireLoaderLock
      │          ├─ +0xef  LdrpInitializeGraphRecurse                                  [관찰] run7
      │          └─ +0xfe  LdrpReleaseLoaderLock
      ├─ (12) +0x295 LdrpBuildForwarderLink
      ├─ (13) +0x337 LdrpPinModule                    LdrInitState == 0 일 때만
      └─ (14) +0x2c8 LdrpFreeLoadContextOfNode       → (실패면 +0x2eb·+0x2f3) → +0x41b → 로그 → RET
```

- 번호는 **코드 흐름 순서**다. 실행 때 탄 건 `(11)` 과 그 안 `+0xef` 뿐이다 (run7 `[관찰]`).

## 확인된 것

- **`LdrpLoadDllInternal` 순서**: 빠른 끝 확인(`+0xb8`) → 일감 비우기·토큰 → 찾거나 준비(`+0x1f2`) → (새로 올릴 때만) `LdrpProcessWork`(`+0x210`) → 일감 비우기 → 교체 처리 → 그래프 줄이기 → **실행 준비(`+0x284`)** → 전달 링크 → (`LdrInitState == 0` 이면) 고정(`+0x337`) → 로드 문맥 치우기. [정적] `.asm`
- **`+0x284` 를 타려면** 빠른 끝을 안 타고, 상태 칸이 0 이상이어야 한다. [정적] `+0xc2`·`+0x154`·`+0x274`
- **메모리에 올리는 시스템 콜까지의 이름 사슬은 `+0x284` 앞의 두 호출 아래**에 있다: KnownDll 길(`+0x1f2` 아래, `NtOpenSection` → `NtMapViewOfSection`), 파일 길(`+0x210` 아래, `NtOpenFile` → `NtCreateSection` → `NtMapViewOfSection`). 둘 다 `LdrpMapDllWithSectionHandle` → `LdrpMinimalMapModule +0x179` 에서 매핑. [정적: 지도, 본문 안 읽음]
  - 그래서 flow.md 2.5 `[추측]` "`+0x284` 앞부분에서 올린다" 는 **코드 순서로 지지된다.** 실제로 어느 길을 탔는지는 동적 (Q20).
- **"kernelbase 는 같은 `LdrLoadDll` 안에서 따라 올라온다" `[추측]` 을 받칠 길이 지도에 있다: 예.** 올리기 함수 아래에 `LdrpMapAndSnapDependency`/`LdrpSnapModule` → `LdrpLoadDependentModuleInternal` → 다시 `LdrpLoadKnownDll`·`LdrpProcessWork` 가 있어서, `LdrLoadDll` 을 다시 부르지 않고 import 를 올릴 수 있는 모양이다. [정적: 지도] 실제로 그렇게 됐는지는 동적 (Q20).
- **`LdrpPrepareModuleForExecution` 은 노드 상태와 로드 문맥 비트로 그래프 켜기를 가른다.** exe 길(`LdrpInitializeProcess +0x1aed`)은 `+0x1a94` 에서 로드 문맥 `+0x20` 비트 0 을 먼저 켜 두기 때문에 켜기를 건너뛴다. 켜기는 `LdrpInitializeProcess +0x1bbb` 가 직접 한다. [정적] `LdrpPrepareModuleForExecution +0xcb ~ +0xcf`, `LdrpInitializeProcess +0x1a8e ~ +0x1a94`. run7·run8 관찰 둘 다와 맞다.
- **이미 끝난 노드(`State` 9) 는 `Prepare` 가 바로 돌아온다** (`+0x40`). 둘째 `LdrLoadDll("KERNELBASE.dll")` 이 `+0x284` 까지 가더라도 시작 함수가 다시 안 불리는 이유가 코드에 있다. [정적]
- **kernel32 준비 때(`LdrInitState` 0)만 타는 자리 = `+0x337 LdrpPinModule`** (이 함수에서 `LdrInitState` 를 읽는 유일한 곳). [정적] `refs`
- **Drain·토큰 Set 은 `LdrInitState` 와 상관없이** 코드상 지나간다 (`LoadOwner`·이유 9 로만 갈림). [정적]
- **`LdrpLoadDll` 이 돌려주는 상태 칸은 이 함수가 쓴다** (빠른 끝·실패·`Prepare`·`BuildForwarderLink` 결과 자리 7곳). run20 의 남은 질문 하나가 풀렸다. [정적]
- **caller 는 둘**: `LdrpLoadDll +0x100`, `LdrpLoadForwardedDll +0x2e6`. [정적: 지도]

## 정정

- 없음. 이전 run 과 어긋나는 것은 없다. (요청서의 "caller 3" 은 run 문서에 쓴 적 없는 요청서 숫자라 정정 대상이 아니다. 지도상 둘.)

## 아직 확인 필요

- 동적 → **Q20** 올리기가 실제로 일어나는 순간: `LdrpMinimalMapModule` ret `+0x179` (`ntdll+0xa399` = `_index.tsv` rva `0xa220` + `0x179`) 에서 `k` — 콜스택에 (가) `LdrpFindOrPrepareLoadingModule +0x5e5` 길인지 (나) `LdrpProcessWork +0x6a` 길인지, KERNELBASE 가 KERNEL32 와 **같은 `LdrLoadDll`(`+0xc0`) 아래**인지. 그리고 `LdrpLoadDllInternal` ret `+0x1f2` (`ntdll+0x1fe22`) `r eax` (`0xc0000135` 인가), `du poi(poi(@rsp+0x40)+0x60)` (항목 이름).
- 동적 → **Q24** `LdrpPrepareModuleForExecution` 입구 (`ntdll+0x86e4c`): `dd poi(@rcx+0x98)+0x38 L1` (State), `dq @rcx+0xb0 L1` (LoadContext), `db poi(@rcx+0xb0)+0x20 L1` (비트 0) — kernel32 때 0, exe 때 1 인지. `+0xef` (`ntdll+0x86f3b`) 이 어느 모듈 호출에서 오는지 (입구에서 `du poi(@rcx+0x60)` 저장).
- 동적 → **Q49** 두 `LdrLoadDll` 안 실행 값: ret `+0xb8` (`ntdll+0x1fce8`) `r eax` (Fastpth 결과, 첫째·둘째 비교), ret `+0x1a0` (`ntdll+0x1fdd0`) 뒤 `dq ntdll+0x1d2618 L1` (`LdrpMainThreadToken`), ret `+0x284` (`ntdll+0x1feb4`) `r eax` (둘째 호출에서 이 줄이 오는지), `+0x29b` (`ntdll+0x1fecb`) 에서 `dd ntdll+0x1d23c8 L1` (`LdrInitState`) 와 `LdrpPinModule` 이 불리나.
- 정적 → **Q12** 그래프를 만드는 함수: 후보 `LdrpAllocateModuleEntry` (caller `LdrpFindOrPrepareLoadingModule +0x47a`, `LdrpLoadDependentModuleInternal +0x115c`, `LdrpInitializeProcess +0x135e`/`+0x157c`), `LdrpMapAndSnapDependency`/`LdrpSnapModule`/`LdrpLoadDependentModuleInternal`, `LdrpCondenseGraph` → `LdrpCondenseGraphRecurse` → `LdrpMergeNodes`. (`LdrpRecordModuleDependency` 는 지도상 `LdrpInitializeNtdllDataTableEntry +0x243` 만 부름, `LdrpDependencyExist` 는 `LdrpLoadContextReplaceModule +0x93`.)
- 정적 → **Q15** `LdrpInitializeGraphRecurse` 본문: `Prepare +0xea` 와 `LdrpInitializeProcess +0x1bb6` 은 같은 3 인자(노드, 상태 칸, `&바이트 0`). 지도상 caller 는 `LdrpInitializeProcess` 3곳, `LdrpPrepareModuleForExecution +0xef`, `LdrGetProcedureAddressForCaller +0x94a`, `AVrfInitializeVerifier`, `LdrpInitializeImportRedirection +0x150`, `LdrpInitializeShimDllDependencies +0xd2`.
- 남은 `[추측]`: `LdrpFastpthReloadedDll`·`LdrpFindOrPrepareLoadingModule`·`LdrpProcessWork` 의 역할 (본문 안 읽음), `LoadContext +0x20` 비트 0 과 `+0xc8` 칸의 이름, 노드 상태 7 일 때 알림·shim 을 건너뛰는 이유, `LdrpQueueWork` 로 넣은 일감이 병렬 로딩 전엔 어디서 도는지 (Q47·Q20), `LdrLoadDll` 경로 해제 바이트 `[RSP+0xbc]` 를 켜는 곳 (이 함수 안에는 없음).

## 디버거 메모 / 도구 메모

- 도구 문제 없음 (결과서 "스크립트 문제: 없음").
- 구조체 칸 이름은 스크립트가 아니라 `llvm-pdbutil-14 dump -types ms/sym/ntdll.dll.pdb` 로 PDB 타입을 직접 읽었다. `_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없어 오프셋만 쓴다.
- 이번 바퀴 `alias:` 이름은 없다.

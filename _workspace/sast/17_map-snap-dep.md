# 17 map-snap-dep — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run22)
- 대기열: Q12a (`_workspace/plan.md`) — Q12 를 둘로 나눈 앞쪽. 안쪽 `LdrpLoadDependentModuleInternal` 은 Q12b
- 관련: flow.md 2.5 `[공백]` 올리는 순간 절 (run21 이름 사슬), 2.6 `[공백]` import DLL 이 메모리에 올라오는 순간, 그림 2 `j2` 상자 / 이전 요청 sast/16 (run21), sast/14 (run19, `LdrpInitializeProcess +0x1aa4`)

## 범위

- 대상 두 함수, depth 1: `LdrpMapAndSnapDependency`, `LdrpAllocateModuleEntry`.
- callee 본문은 읽지 않는다. 특히 `LdrpLoadDependentModuleInternal`(Q12b), `LdrpSnapModule`, `LdrpQueueWork`, `LdrpLoadDependentModuleW` 안쪽은 이름·인자·결과만.
- 잡일(`memset`, `strlen`, `RtlCopyMemory`, 문자열 변환, 힙 할당·해제, 로그, 쿠키)은 개수만. 단, `RtlAllocateHeap` 이 **그래프 자료(노드·간선)** 를 만드는 것으로 보이면 그 자리는 따로 적는다.
- 구조체 칸 이름은 PDB 타입으로 (`_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`, `_LDRP_LOAD_CONTEXT` 같은 것 — 정확한 이름은 PDB 에서 확인, 없으면 "PDB 에 없음").

## 알고 싶은 것

1. **`LdrpMapAndSnapDependency` 의 순서**: 호출 23곳을 시간 순서로. 특히:
   - 입구 인자 (sast/14: 첫 인자 = 로드 문맥, exe 길에서는 `LdrpInitializeProcess [RSP+0x2b0]`) 에서 무엇을 꺼내나 (모듈 항목, import 표).
   - `+0x9e LdrpPrepareImportAddressTableForSnap`, `+0xe5 LdrpGetImportDescriptorForSnap`: import 표를 어떻게 얻나.
   - **import 하나마다 도는 고리**가 있나. 있으면 고리 범위(`+0x..~+0x..`)와, 고리 한 바퀴에 부르는 것 (`+0x34e LdrpLoadDependentModuleInternal`, `+0x4c2 LdrpSnapModule`, `+0x4f7 LdrpQueueWork`, `+0x5d8 LdrpLoadDependentModuleW` 중 무엇이 어떤 조건으로).
   - `+0xd2 LdrpShouldModuleImportBeRedirected`, `+0x10e LdrpQueryCurrentPatch` 의 조건 (test.exe 길에선 안 탈 것인지).
   - `+0x461`/`+0x47d` SRW 락 짝 안에서 하는 일.
2. **"누가 누구에 기대나" 를 적는 자리**: 이 함수 본문에 그래프 간선(의존 관계)을 만들거나 목록에 끼우는 줄이 있나 (예/아니오). 있으면 자리와 칸 이름. 없으면 "`LdrpLoadDependentModuleInternal` 에 넘기는 인자 중 어느 것이 부모(기대는 쪽) 정보인가" 를 적는다 — Q12b 에서 이어 볼 단서.
3. **같이 올리기 vs 일감으로 넘기기**: `LdrpLoadDependentModuleInternal` 을 직접 부르는 길과 `LdrpQueueWork` 로 넘기는 길이 갈리는 조건. run19 는 병렬 로딩을 켜는 자리가 `LdrpInitializeProcess +0x1a42` 이고 exe import 는 그 **뒤** `+0x1aa4`, kernel32 는 그 **앞** `+0x199d` 라고 했다. kernel32 준비 때(`LdrpMapDllWithSectionHandle +0x1c8` 에서 이 함수로 옴, run21)와 exe import 때 이 갈림이 다르게 가나 (정적 조건까지).
4. **`LdrpSnapModule` 을 부르는 때**: import 를 다 올린 뒤 한 번인가, 하나마다인가. 조건.
5. **`LdrpAllocateModuleEntry`** (호출 5곳, caller 6곳): 무엇을 할당하나 — 모듈 항목(`_LDR_DATA_TABLE_ENTRY`) 과 그래프 노드(`DdagNode`, 항목 `+0x98`, run19) 를 **같이** 만드나 (`+0x2e`, `+0x60` 의 `RtlAllocateHeap` 두 번). 크기, 처음 값 (특히 노드 `State`, 목록 머리), 활성화 컨텍스트(`+0xbb`, `+0x16b`) 는 언제.
   - caller 중 `LdrpLoadDependentModuleInternal +0x1157` 과 `LdrpFindOrPrepareLoadingModule +0x475` 가 있다 → "새 DLL 을 처음 만나면 항목+노드를 만든다" 가 맞나 (`[추측]` 수준이면 그렇게).
6. **2.6 관련 (Q14 와 같은 함수)**: test.exe → dll1 → dll2 의 경우, 이 함수가 exe 에 대해 한 번 불리고 dll1 은 그 고리에서 나오나. dll2 는 dll1 을 올린 뒤 **같은 함수가 dll1 에 대해 다시 불려서** 나오는가 (누가 다시 부르나 — `LdrpMapDllWithSectionHandle +0x1c3`, 지도). 정적으로 말할 수 있는 만큼.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpMapAndSnapDependency$` | 1 | 질문 1 ~ 4, 6 |
| ntdll | `^LdrpAllocateModuleEntry$` | 1 | 질문 5 |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpMapAndSnapDependency` rva `0x210a0`, 1544바이트, 깊이 5, caller 2: `LdrpInitializeProcess +0x1aa4`(exe import, run19), `LdrpMapDllWithSectionHandle +0x1c3`(올린 DLL 마다, run21). 직접 호출 23줄:
  - `+0x63 LdrpFindDllActivationContext`, `+0x9e LdrpPrepareImportAddressTableForSnap`, `+0xd2 LdrpShouldModuleImportBeRedirected`, `+0xe5 LdrpGetImportDescriptorForSnap`, `+0x10e LdrpQueryCurrentPatch`
  - `+0x17b RtlAllocateHeap`, `+0x213 strlen`, `+0x258 memset`, `+0x315 RtlAnsiStringToUnicodeString`, **`+0x34e LdrpLoadDependentModuleInternal`**, `+0x36e RtlDeleteBoundaryDescriptor`(별칭 많은 주소, sast/15), `+0x3ce RtlUTF8ToUnicodeN`, `+0x451 RtlFreeHeap`
  - `+0x461 RtlAcquireSRWLockExclusive` / `+0x47d RtlReleaseSRWLockExclusive`, **`+0x4c2 LdrpSnapModule`**, **`+0x4f7 LdrpQueueWork`**, `+0x524 NtdllpReallocateStringRoutine`, `+0x541 RtlpCtAllocateMemory`, `+0x567 RtlCopyMemory`, **`+0x5d8 LdrpLoadDependentModuleW`**, `+0x604 LdrpLogEtwHotPatchStatus`, `+0x7e __security_check_cookie`
  - (위 오프셋은 지도의 ret 자리)
- `LdrpAllocateModuleEntry` rva `0x6fab0`, 412바이트, 깊이 5, caller 6: `LdrpInitializeProcess +0x1359`·`+0x1577`, `LdrpInitializeNtdllDataTableEntry +0x67`, `LdrpLoadDependentModuleInternal +0x1157`, `LdrpFindOrPrepareLoadingModule +0x475`, `LdrpAllocatePlaceHolder +0xd0`. 직접 호출 5줄: `+0x2e`·`+0x60 RtlAllocateHeap`, `+0xbb RtlAddRefActivationContext`, `+0x15b RtlFreeHeap`, `+0x16b RtlGetActiveActivationContext`.
- `_index.tsv` 에 "의존 관계를 더한다" 는 이름의 함수(예: `AddDependency`)는 없다. `LdrpRecordModuleDependency`(0xd8e40) 는 caller 가 `LdrpInitializeNtdllDataTableEntry +0x23e` 하나뿐, `LdrpDependencyExist` 는 `LdrpLoadContextReplaceModule +0x8e` 하나뿐. → 간선은 어느 함수 본문 안에서 직접 만들 것 `[추측]`.

## 이미 아는 것

- `[관찰]` (run8) `LdrpInitializeProcess +0x1bb6`(call) 에서 멈췄을 때 dll1·dll2 가 이미 올라와 있었다. dll1·dll2 는 `LdrpLoadDll` 을 타지 않았다. 켜는 순서는 dll2 → dll1.
- `[소스]` test.exe 는 dll1 만 import, dll2 는 dll1 을 거쳐 간접으로 붙는다.
- `[정적]` (sast/14·run19) exe import: `LdrpInitializeProcess +0x1a42 LdrpEnableParallelLoading` → `+0x1a89` 메인 스레드 토큰 → **`+0x1aa4 LdrpMapAndSnapDependency(로드 문맥 [RSP+0x2b0])`** → `+0x1aac LdrpDrainWorkQueue(1)` → `+0x1aed LdrpPrepareModuleForExecution(LdrpImageEntry)`. `+0x1a94` 가 로드 문맥 `+0x20` 비트 0 을 세워 켜기를 미룬다 (run21).
- `[정적]` (sast/16·run21) 지도 이름 사슬: `LdrpMapDllWithSectionHandle +0x1c8` → `LdrpMapAndSnapDependency` → `+0x34e LdrpLoadDependentModuleInternal` / `+0x4c2 LdrpSnapModule` / `+0x5d8 LdrpLoadDependentModuleW`. `LdrpLoadDependentModuleInternal` 안에 `LdrpLoadKnownDll`·`LdrpProcessWork`·`LdrpQueueWork` → 다시 올리기 함수로 돌아오는 **재귀 모양**. 노드 상태 번호: 5~9 (run21 해설 표), 9 = 켜짐 `[추측: ReadyToRun]`.
- `[추측]` (flow.md 2.5·2.6) 올리면서 import 를 읽어 의존 관계 그래프를 만들고, 다 올라온 뒤 그 순서대로 켠다.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄). 태그 `[정적]` / `[추측]`. 라벨은 ret 자리 (call 자리는 "(call)").
- `LdrpMapAndSnapDependency` 순서를 **짧은 트리 하나** (고리가 있으면 고리 안을 들여 써서).
- test.exe → dll1 → dll2 를 이 함수 기준으로 그린 **짧은 이야기 그림** (정적으로 말할 수 있는 만큼, 모르는 칸은 `?`).
- 그림 2 `(정적)` 상자 후보 2 ~ 3개 (이유 한 줄씩).
- 동적으로 확인할 것 + 멈춤점 (`ntdll+rva`, 짧게). 이미 대기 중인 Q20·Q23·Q24·Q47 과 겹치면 그 번호를.
- Q12b (`LdrpLoadDependentModuleInternal`) 에 넘길 질문 3 ~ 5개 (이번에 본 인자·결과 칸 기준).
- Q14 ("dll1·dll2 를 올리는 쪽", `+0x1bb6` 앞 import 처리) 가 이번 결과로 **답이 났는지** 한 줄 판단 (답남 / 일부 / 아님).
- 스크립트 문제는 "스크립트 문제" 절에.

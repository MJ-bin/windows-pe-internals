# 18 load-dependent — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run23)
- 대기열: Q12b (`_workspace/plan.md`) — Q12 를 둘로 나눈 안쪽. 바깥 고리 `LdrpMapAndSnapDependency` 는 Q12a (sast/17·run22) 에서 끝
- 관련: flow.md 2.5 (kernelbase 가 kernel32 를 따라 올라오는 길), 2.6 `[공백]` import DLL 이 올라오는 순간·test.exe → dll1 → dll2 이야기 그림의 `?` 칸, 4장 2.5·2.6 그래프 줄 / 이전 요청 sast/17 (질문 다섯 개를 넘겨받음), sast/16

## 범위

- 대상 `ntdll!LdrpLoadDependentModuleInternal` 하나 (rva `0x21ab0`, 9250바이트), depth 1. 예외 정리 조각 `$fin$0` 포함.
- callee 본문은 읽지 않는다 (`LdrpLoadKnownDll`, `LdrpProcessWork`, `LdrpQueueWork`, `LdrpFindLoadedDllByName`, `ApiSetp*`, `LdrpAllocateModuleEntry`(sast/17 에서 봄) 등). 이름·인자·결과만.
- 잡일(`RtlCopyMemory`, `RtlpCtAllocateMemory`, `NtdllpReallocateStringRoutine`, `wcslen`, `memset`, 로그·ETW·`RtlGetCurrentServiceSessionId`, 쿠키)은 묶음 안 개수만.
- 구조체 칸: `_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`, `_LDRP_CSLIST` 등은 PDB 타입으로. `_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없다 (sast/17) — 이름을 지어내지 말고 오프셋+역할로.

## 알고 싶은 것

1. **큰 순서**: 호출 75곳을 하는 일별 **묶음 5 ~ 10개**로 시간 순서대로 (sast/13·14 와 같은 표: 묶음 | 범위 | 요약 | 대표 호출 call/ret | 호출 수(잡일)). 함수가 크니 **갈림길(어느 길로 끝나나)** 을 먼저 그려 주면 좋다.
2. **간선(의존 관계)을 적는 줄** (sast/17 넘긴 질문 ①): 인자 2·3 (부모 로드 문맥·부모 항목, sast/17) 으로 자식 노드 `IncomingDependencies`(+0x30) 나 부모 노드 `Dependencies`(+0x28) 에 쓰는 줄이 있나 (예/아니오). 있으면 자리와 칸 이름 (PDB `_LDRP_CSLIST` 등), 간선 한 칸을 무엇으로 만드나 (`RtlAllocateHeap` 자리 `+0x929`·`+0x10d6`·`+0x17bd` 중 어느 것, 크기). 없으면 "간선을 쓰는 곳은 이 함수에 없다" + 남은 후보.
3. **세 갈림** (sast/17 ③): 이 DLL 이 ① 이미 올라와 있음 (`+0x16e9 LdrpFindLoadedDllByName` 등), ② KnownDll (`+0x178d LdrpLoadKnownDll`), ③ 새로 올리기 (`+0x10d6 ~ +0x1157` 새 로드 문맥 + `LdrpAllocateModuleEntry`, 그 뒤 `+0x1827 LdrpQueueWork` 또는 `+0x1ca9 LdrpProcessWork`) 로 갈리는 조건과 순서. 갈림마다 인자 5 (`&ctx.58[번호]`, sast/17) 에 무엇을 저장하나. ①일 때도 간선은 생기나 (이미 있는 DLL 에 기대는 경우).
4. **일감 vs 바로** (sast/17 ④): `+0x1827 LdrpQueueWork` 와 `+0x1ca9 LdrpProcessWork` 를 부르는 조건. 병렬 로딩 켜짐 여부(전역이나 TEB 비트)를 읽나. kernel32 준비 때(병렬 로딩 켜기 전, run19 `LdrpInitializeProcess +0x1a42` 앞)와 exe import 때 다르게 가나.
5. **남은 자식 수와 기다림** (sast/17 ②): `+0x12df~+0x12f7` 에서 부모 문맥 `+0x6c` 를 줄이는 조건, `+0x1606~+0x1634` 의 "`+0x6c == 0` 이면 부모 노드 `State = 3`, 반환 `0x103`" 의 뜻 (자식이 아직 안 끝나서 부모가 기다린다는 표시인가). `0x103` 을 caller(`LdrpMapAndSnapDependency`) 가 어떻게 받나 (sast/17 결과서와 맞춰서).
6. **ApiSet**: `+0x2fa ApiSetpResolveHost`, `+0x22b0 ApiSetpSearchForApiSet`, `+0x22db ApiSetpSearchForApiSetHost`. import 이름이 `api-ms-win-…` 같은 가상 이름일 때 진짜 DLL 이름(예: KERNELBASE.dll)으로 바꾸는 자리인가 `[추측]`. 바뀐 이름으로 ①②③ 갈림을 다시 타나. (run20·run21: kernel32 를 들이면 kernelbase 가 따라 들어온다는 `[추측]` 과 이어짐 — kernel32 의 import 는 이번 범위 밖이라 "이 함수가 가상 이름을 바꿔 준다" 까지만)
7. **인자와 결과** (sast/17 ⑤): 인자 4 (`R9D` = LoadReason) 가 쓰이는 자리(`+0x1179`) 와 값, 6번째 인자 `[RSP+0x48]` 임시 포인터가 돌려주는 것. 반환값 종류 (0, `0x103`, 실패 코드).
8. **실패 정리** (`+0x1f03 ~ +0x1f70`): `LdrpUnmapModule`, `LdrpDestroyNode`, `LdrpReleaseTlsEntry`, `RtlReleaseActivationContext` — 어떤 실패 때 타나. `+0x23f0 RtlRaiseException` 의 조건. test.exe 길에서 안 탈 것인지.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpLoadDependentModuleInternal$` | 1 | 질문 1 ~ 8 (`.asm` 이미 있으면 그대로) |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- rva `0x21ab0`, 9250바이트, 깊이 6, caller 3: `LdrpMapAndSnapDependency +0x34e`(ret, import 고리), `LdrpSnapModule +0x5df`, `LdrpLoadDependentModuleW +0xae`. 직접 호출 75줄 (예외 조각 1 포함).
- 잡일 아닌 것 (ret 자리): `+0x2ff ApiSetpResolveHost`, `+0x92e`·`+0x10db`·`+0x17c2 RtlAllocateHeap`, `+0xb32`/`+0xd1f` SRW 공유 락 짝, `+0xdff LdrpDereferenceModule`, `+0x1145 RtlCopyMemory` → **`+0x115c LdrpAllocateModuleEntry`**, `+0x132f RtlReleaseSRWLockExclusive`, `+0x1341 LdrpFreeReplacedModule`, `+0x142b RtlDosApplyFileIsolationRedirection_Ustr`, `+0x1493 RtlGetFullPathName_Ustr`, `+0x1513 LdrpIncrementModuleLoadCount`, `+0x1681 RtlDetermineDosPathNameType_Ustr`, `+0x169a LdrpGetFullPath`, **`+0x16ee LdrpFindLoadedDllByName`**, `+0x1772 RtlpAcquireSRWLockExclusiveContended`, **`+0x1792 LdrpLoadKnownDll`**, `+0x1805 RtlFreeHeap`, **`+0x182c LdrpQueueWork`**, `+0x1882 RtlAcquireSRWLockExclusive`, `+0x1be7 LdrpFindOrPrepareEnclaveModule`, **`+0x1cae LdrpProcessWork`**, 실패 정리 `+0x1f08 ~ +0x1f70` (`LdrpUnmapModule`, `LdrpFreeUnicodeString`, `RtlFreeHeap`, `LdrpDestroyNode`, `LdrpReleaseTlsEntry`, `RtlReleaseActivationContext`), `+0x21a8 LdrpLogDllState`, `+0x22b5 ApiSetpSearchForApiSet`, `+0x22e0 ApiSetpSearchForApiSetHost`, `+0x2357 RtlDeactivateActivationContextUnsafeFast`, `+0x23f5 RtlRaiseException`, 예외 조각 `RtlDeactivateActivationContextUnsafeFast`.
- 잡일: `RtlCopyMemory` 8, `RtlGetCurrentServiceSessionId` 6, `RtlpCtAllocateMemory` 5, `NtdllpReallocateStringRoutine` 4, `LdrpLogEtwEvent` 4, `RtlDeleteBoundaryDescriptor`(별칭 9개 주소 = 힙 버퍼 놓기, sast/15·run22) 3, `LdrpLogInternal` 3, `wcslen` 2, `LdrpAppendUnicodeStringToFilenameBuffer` 2, `memset` 1, 쿠키 1.

## 이미 아는 것

- `[정적]` (sast/17·run22) 바깥 고리 `LdrpMapAndSnapDependency +0x1d0~+0x3a3` 이 import 마다 **조건 없이** 이 함수를 부른다. 인자: 1 = ?, 2 = 부모 로드 문맥(`ctx`), 3 = 부모 항목(`R13`), 4 = LoadReason, 5 = `&ctx.58[번호]`, 6 = `[RSP+0x48]` (자세한 표는 sast/17 결과서). 로드 문맥 `+0x30` = 부모 항목 (exe·kernel32 는 0). 바깥 함수에는 간선을 쓰는 줄이 없다.
- `[정적]` (sast/17) `LdrpAllocateModuleEntry` 는 항목(0x138) + 노드(0x50) 한 쌍을 만들고, 노드 `State`·`Dependencies`·`IncomingDependencies` 는 쓰지 않아 0 `[추측: 힙 flag 8]`. 부모가 있으면 부모의 활성화 컨텍스트를 물려받는다.
- `[정적]` (sast/16·run21) 지도 사슬: 이 함수 안 `LdrpLoadKnownDll`·`LdrpProcessWork`·`LdrpQueueWork` → 다시 올리기 함수(`LdrpMapDllWithSectionHandle`) → `+0x1c8` 에서 `LdrpMapAndSnapDependency` 다시 → 재귀 모양.
- `[관찰]` (run8) dll1·dll2 는 `LdrpInitializeProcess +0x1bb6`(call) 전에 이미 올라와 있었고, 켜는 순서는 dll2 → dll1.
- `[소스]` test.exe 는 dll1 만, dll1 은 dll2 를 import 한다. (sample/ 의 import 는 일반 DLL 이름이고 api-set 이름은 아님 — 확인은 sample 소스로)
- 노드 상태 번호 (run21 해설 표): 3 = ?, 4 = snap 직전, 5 = IAT 없음, 6~7, 8·9 = 켜짐 근처 `[추측]`.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄). 태그 `[정적]` / `[추측]`. **오프셋은 call·ret 둘 다** 적어 주세요 (예: `+0x1827 (call) / +0x182c (ret)`). 라벨로 쓸 땐 ret.
- 질문 1 표 하나 + **갈림길 트리 하나** (①②③ 와 끝나는 자리).
- run22 이야기 그림(test.exe → dll1 → dll2)의 `?` 칸 중 이번에 채워지는 것.
- 그림 2 `(정적)` 상자 후보 2 ~ 4개 (이유 한 줄씩).
- 동적으로 확인할 것 + 멈춤점 (`ntdll+rva`, 짧게). 이미 대기 중인 Q20·Q23·Q24·Q47 과 겹치면 그 번호를.
- 남은 그래프 일 (`LdrpCondenseGraph`→`LdrpCondenseGraphRecurse`→`LdrpMergeNodes`, `LdrpSnapModule`) 을 따로 한 바퀴(Q12c)로 볼 가치가 있는지 한 줄 판단.
- Q14 ("dll1·dll2 를 올리는 쪽") 가 이번 결과로 답이 났는지 한 줄 판단 (답남 / 일부 / 아님).
- 스크립트 문제는 "스크립트 문제" 절에.

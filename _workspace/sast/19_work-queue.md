# 19 work-queue — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run24)
- 대기열: Q12c (`_workspace/plan.md`) — Q12 의 마지막 조각 (일감 흐름 + 그래프 정리)
- 관련: flow.md 2.5·2.6 (run22 이야기 그림 `?` 칸: "일감을 누가 꺼내 올리나", "자식이 다 올라온 뒤 부모 카운터를 줄이고 snap 으로 잇는 코드"), 3장 ntdll 일꾼, 4장 / 이전 요청 sast/18 (run23), sast/17 (run22), sast/14 (run19, `LdrpEnableParallelLoading`)

## 범위

- 대상 (depth 1):
  - 일감 흐름: `LdrpQueueWork`, `LdrpWorkCallback`, `LdrpProcessWork`, `LdrpDrainWorkQueue`, `LdrpSignalModuleMapped`
  - 그래프 정리: `LdrpCondenseGraph`, `LdrpCondenseGraphRecurse`, `LdrpMergeNodes`
- callee 본문은 읽지 않는다 (`LdrpMapDllSearchPath`·`LdrpMapDllFullPath`·`LdrpSnapModule`(Q50)·`TpPostWork`·`TpAllocWork` 등). 이름·인자·결과만.
- 잡일(로그·ETW·에러 보고·`RtlFreeHeap`)은 개수만.
- 구조체 칸은 PDB 타입으로. `_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없다 — 이름을 지어내지 말고 오프셋+역할로.
- **오프셋은 call·ret 둘 다** 적는다. 점프 목적지를 함수 오프셋으로 바꿀 땐 "절대 주소 − 함수 시작" 으로 계산했는지 한 번 더 확인한다 (sast/16·18 에서 한 줄씩 어긋난 적이 있음).

## 알고 싶은 것

1. **일감 큐 (`LdrpQueueWork`, 3곳)**: 일감(로드 문맥)을 어느 목록(전역 이름, `refs`)에 넣나. 잠금(`RtlEnterCriticalSection` 의 대상 전역 이름)은 무엇인가. `+0x7d TpPostWork` 는 언제 부르나 — 일꾼을 깨우는 조건 (병렬 로딩이 꺼져 있으면 안 부르나? 무엇으로 판단하나).
2. **일꾼 (`LdrpWorkCallback`)**: 지도에 없다 (호출 참조 0, `LdrpEnableParallelLoading +0xe2 TpAllocWork` 에 주소로 넘겨지는 것으로 보임 — `refs LdrpWorkCallback` 으로 확인). 본문 순서: 큐에서 꺼내 `LdrpProcessWork` 를 부르나, 몇 개씩, 언제 끝나나.
3. **`LdrpDrainWorkQueue` (8곳, caller 16)**: 인자(0/1, sast/16·19 에서 `Drain(1)` 이 나옴)에 따라 무엇이 다른가. `+0x182 LdrpProcessWork` 로 **이 스레드가 직접** 일감을 꺼내 처리하나, `+0x194 NtWaitForSingleObject` 로 **일꾼이 끝내길 기다리나** — 둘이 갈리는 조건. 무엇을 기다리나 (이벤트 전역 이름). → run19 의 `LdrpInitializeProcess` Drain 4곳, run21 의 `LdrpLoadDllInternal` Drain 2곳이 kernel32 준비 때(병렬 로딩 켜기 전)와 exe import 때(켠 뒤) 다르게 도는지.
4. **`LdrpProcessWork` (17곳)**: 입구 인자 (로드 문맥, 두 번째 인자 1, run21). 순서: `+0x65 LdrpMapDllSearchPath` / `+0x13f LdrpMapDllFullPath` / `+0x149 LdrpMapDllRetry` / `+0x153 LdrpMapDllPatchImage` 중 무엇을 어떤 조건으로 (올리기 길), `+0x12c LdrpSnapModule` 은 언제 (올린 뒤 바로 snap?), 끝에 `NtSetEvent`(tail) 로 누구에게 신호하나. 실패 처리(`LdrpReportError` 등)는 짧게.
5. **`LdrpSignalModuleMapped`** (caller `LdrpProcessMappedModule +0x1e2`): 모듈이 올라온 뒤 무엇을 큐에 넣나 (`+0x59 LdrpQueueWork`). → run22·23 의 "자식이 다 올라온 뒤 부모 카운터(`ctx+0x6c`)를 줄이고 snap 으로 잇는 코드" 가 여기인가, 아니면 어디인가 (이 다섯 함수 안에서 `+0x6c` 를 줄이는 줄을 찾는다).
6. **그래프 정리 (`LdrpCondenseGraph` → `LdrpCondenseGraphRecurse` → `LdrpMergeNodes`)**: 무엇을 "정리" 하나 — 서로 기대는 노드(고리)를 하나로 합치는가 `[추측]`. 합치는 조건 (노드 칸 이름, PDB `_LDR_DDAG_NODE`), 합친 노드의 간선은 어떻게 되나, `LdrpDestroyNode` 는 언제. test.exe → dll1 → dll2 (고리 없음) 에서는 아무것도 안 합칠 것인가.
7. **이야기 그림 잇기**: run22·23 이야기 그림 (test.exe → dll1 → dll2, kernel32 → kernelbase) 에서 이번에 채울 수 있는 `?` 칸. 특히 "일감 넣기 → (누가) 꺼내서 올리기 → 올린 뒤 그 모듈의 import 고리 → … → 자식 끝 → 부모 snap" 의 한 바퀴가 이 다섯 함수로 닫히나.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpQueueWork$` | 1 | 질문 1 |
| ntdll | `^LdrpWorkCallback$` | 1 | 질문 2 (지도에 없음 — 새로 뽑기) |
| ntdll | `^LdrpDrainWorkQueue$` | 1 | 질문 3 |
| ntdll | `^LdrpProcessWork$` | 1 | 질문 4 |
| ntdll | `^LdrpSignalModuleMapped$` | 1 | 질문 5 |
| ntdll | `^LdrpCondenseGraph$`, `^LdrpCondenseGraphRecurse$`, `^LdrpMergeNodes$` | 1 | 질문 6 |
| ntdll | 전역: 큐 목록·잠금·이벤트 (질문 1·3 에서 나온 이름), `LdrpWorkCallback` | - | `refs` |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpQueueWork` rva `0xe244c`, 136바이트, 깊이 6, caller 4: `LdrpMapAndSnapDependency +0x4f7`, `LdrpSignalModuleMapped +0x5e`, `LdrpLoadDependentModuleInternal +0x182c`, `LdrpLoadContextReplaceModule +0x10f` (모두 ret). 호출 3: `+0x19/+0x1e RtlEnterCriticalSection`, `+0x54/+0x59 RtlLeaveCriticalSection`, `+0x7d/+0x82 TpPostWork`.
- `LdrpWorkCallback` rva `0xe9ef0` — `_index.tsv` 에 있음, 지도에 없음.
- `LdrpEnableParallelLoading` (run19): `+0x49 TpAllocPoolInternal`, `+0x66 TpSetPoolWorkerThreadIdleTimeout`, `+0x7f TpSetPoolMaxThreads`, `+0xe2 TpAllocWork`.
- `LdrpDrainWorkQueue` rva `0xad400`, 456바이트, 깊이 5, caller 16 (지도에 보이는 것 14: `LdrpInitializeProcess` 4, `LdrpLoadDllInternal` 3, `LdrpInitializeThread`, `LdrpInitializeImportRedirection`, `LdrUnloadDll`, `LdrGetProcedureAddressForCaller`, `LdrpFastpthReloadedDll`, `RtlQueryInformationActivationContext`, `LdrpFindLoadedDll`). 호출 8: 임계 구역 들어가기/나오기 ×(2/3), `+0x15b LdrpUpdateStatistics`, `+0x182/+0x187 LdrpProcessWork`, `+0x194/+0x199 NtWaitForSingleObject`.
- `LdrpProcessWork` rva `0x17140`, 465바이트, 깊이 6, caller 3 (지도): `LdrpDrainWorkQueue +0x187`, `LdrpLoadDependentModuleInternal +0x1cae`, `LdrpLoadDllInternal +0x210`. 호출 17 (본체 13 + 예외 조각 4): `+0x65 LdrpMapDllSearchPath`, `+0x12c LdrpSnapModule`, `+0x13f LdrpMapDllFullPath`, `+0x149 LdrpMapDllRetry`, `+0x153 LdrpMapDllPatchImage`, 임계 구역 짝, `+0x1c3 NtSetEvent` (꼬리 호출), 로그·에러 5.
- `LdrpSignalModuleMapped` rva `0xe23e0`, 100바이트, caller 1 (`LdrpProcessMappedModule +0x1e2`). 호출 1: `+0x59/+0x5e LdrpQueueWork`.
- `LdrpCondenseGraph` (43바이트, caller `LdrpPrepareModuleForExecution +0x12f`, `LdrpLoadDllInternal +0x262`) → `+0x21 LdrpCondenseGraphRecurse` (344바이트: SRW 독점 짝 안에서 `+0xd3 LdrpMergeNodes`, `+0xfd LdrpDestroyNode`, `+0x138` 자기 재귀) → `LdrpMergeNodes` (876바이트: 로그 2, `RtlFreeHeap` 3).

## 이미 아는 것

- `[정적]` (sast/18·run23) `LdrpLoadDependentModuleInternal +0x182c LdrpQueueWork`: 새 DLL 이고 경로 직접 지정(`0x400`)이 아닐 때, `LdrpModuleDatatableLock` 독점을 잡은 채로 부른다. 병렬 로딩 켜짐 여부를 읽는 줄은 없다. `+0x1cae LdrpProcessWork` 는 `0x400` 이 켜졌을 때 (락 잡기 전). 간선은 `+0xf69`/`+0xf8b`. 부모 `+0x6c` 는 `*arg6 ≠ 0` 또는 자식 `State ≥ 2` 일 때만 줄인다. `+0x6c == 0` 이고 자식 `State < 2` 면 부모 `State = 3`, 반환 `0x103`.
- `[정적]` (sast/17·run22) `LdrpMapAndSnapDependency` 끝맺음: 부모 없음(`ctx+0x30 == 0`, exe·kernel32) → `+0x4c2 LdrpSnapModule`, 부모 있음 → `+0x4f7 LdrpQueueWork` (자기 snap 을 일감으로). 락 짝 안에서 `ctx+0x6c` 를 1 줄인다. 올라온 DLL 마다 `LdrpMapDllWithSectionHandle +0x1c8` 에서 이 함수가 다시 불린다.
- `[정적]` (sast/16·run21) `LdrpLoadDllInternal`: `+0x1f2 LdrpFindOrPrepareLoadingModule` 이 `0xc0000135` 면 `+0x210 LdrpProcessWork(ctx, 1)` → `+0x21a LdrpDrainWorkQueue(1)`.
- `[정적]` (sast/14·run19) `LdrpInitializeProcess +0x1a42 LdrpEnableParallelLoading(ProcessParameters.LoaderThreads)` 가 스레드 풀·일감을 만든다. Drain 4곳: `+0x1307`, `+0x19c6`, `+0x1ab1`(인자 1), `+0x1b91` (ret). kernel32 준비(`+0x199d`)는 `+0x1a42` 앞.
- `[관찰]` (run7·run8) Worker 스레드 쪽은 `LdrpInitialize +0xa7` 에서 `LdrpInitializeThread` 를 불렀다 (= 일꾼 스레드가 실제로 있었다). dll1·dll2 는 `+0x1bb6` 전에 이미 올라와 있었다.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄, call·ret 둘 다). 태그 `[정적]` / `[추측]`. 라벨은 ret.
- 일감 흐름 **한 바퀴 그림 하나** (큐에 넣기 → 누가 꺼내나(일꾼 / Drain 하는 스레드) → `LdrpProcessWork` → 올리기 → `LdrpMapAndSnapDependency` 재귀 → 자식 끝 → 신호 → 부모 snap). 모르는 칸은 `?`.
- 그래프 정리 짧은 트리 하나.
- run22·23 이야기 그림에서 이번에 채운 `?` 칸 목록.
- 그림 2 `(정적)` 상자 후보 2 ~ 4개 (이유 한 줄씩).
- 동적으로 확인할 것 + 멈춤점 (`ntdll+rva`, 짧게). 이미 대기 중인 Q20·Q23·Q47 과 겹치면 그 번호를.
- Q14 ("dll1·dll2 를 올리는 쪽") 가 이번 결과로 답이 났는지 한 줄 판단 (답남 / 일부 / 아님).
- 스크립트 문제는 "스크립트 문제" 절에 (특히 `LdrpWorkCallback` 처럼 주소로만 넘겨지는 함수를 지도가 못 잇는 것 — 이미 알려진 한계인지 한 줄).

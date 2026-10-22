# 16 load-dll-internal — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run21)
- 대기열: Q11 (`_workspace/plan.md`)
- 관련: flow.md 2.5 (`LdrLoadDll` 안쪽, `[공백]` kernel32·kernelbase 를 메모리에 올리는 순간), 2.6, 2.7, 4장 2.5 공백 / 이전 요청 sast/15 (`LdrpLoadDll +0xfb` → 이 함수, 인자 9개), sast/14 (`LdrpInitializeProcess +0x1aed` → `LdrpPrepareModuleForExecution`)

## 범위

- 대상 두 함수, depth 1: `LdrpLoadDllInternal`, `LdrpPrepareModuleForExecution`.
- callee 본문은 읽지 않는다. 의존 관계 그래프를 **만드는** 함수의 본문은 Q12, 그래프를 **도는** `LdrpInitializeGraphRecurse` 본문은 Q15 몫이다.
- 단, 질문 2 는 **지도(`_map/*_edges.tsv`)의 이름만으로** 한 단계 ~ 몇 단계 아래 길을 따라가도 된다 (본문 읽기 아님, 함수 이름과 호출 자리만).
- 잡일(`LdrpLogInternal`, `LdrpLogError`, `LdrpFreeUnicodeString` 등)은 개수만.

## 알고 싶은 것

1. **`LdrpLoadDllInternal` 의 앞쪽(`+0x0 ~ +0x27f`, `+0x284` 앞) 순서**: 호출을 시간 순서로 정리하고 갈림 조건을 적는다. 특히:
   - `+0xb3 LdrpFastpthReloadedDll` (ret `+0xb8`): 이미 올라온 DLL 을 빨리 돌려주는 길인가? 이 길을 타면 어디로 뛰나 (`+0x284` 를 건너뛰나).
   - `+0x1ed LdrpFindOrPrepareLoadingModule` (ret `+0x1f2`) → `+0x20b LdrpProcessWork` (ret `+0x210`) → `+0x215 LdrpDrainWorkQueue` (ret `+0x21a`): 각각 무슨 역할인지 이름·인자로 (`[추측]`), 갈림 조건, 결과 칸.
   - `+0x23b LdrpHandleReplacedModule`, `+0x25d LdrpCondenseGraph` 의 조건.
   - `+0x185`/`+0x215` `LdrpDrainWorkQueue`, `+0x19b`/`+0x312` 메인 스레드 토큰: kernel32 준비 때(`LdrInitState`=0, 병렬 로딩을 켜는 `LdrpInitializeProcess +0x1a42` **전**)에도 타는가.
   - `LdrInitState` 를 읽는 곳 (sast/15: `+0x29b` 근처) 과 그 값으로 갈리는 내용.
2. **올리기가 실제로 일어나는 곳 (지도로 이름만)**: `LdrpLoadDllInternal` 에서 출발해 메모리에 올리는 시스템 콜(`NtOpenFile`·`NtCreateSection`·`NtMapViewOfSection` 계열, 정확한 이름은 `_index.tsv`/`--where`)까지 닿는 **호출 길(이름 사슬)** 을 지도에서 찾는다. 가장 짧은 길 1 ~ 2개, 깊이 표시. 같은 방법으로 **의존 DLL 을 처리하는 함수**(예: `LdrpLoadDependentModule*`, `LdrpMapAndSnapDependency`, `LdrpSnapModule` — 이름은 지도로 확인)까지 닿는 길도. KnownDll 쪽(`LdrpFindKnownDll`/`LdrpLoadKnownDll`, sast/15 `refs`)이 이 길 위에 있나.
   - 이걸로 답하고 싶은 것: flow.md 2.5 `[추측]` "kernel32 를 올리면 kernelbase 가 **같은 `LdrLoadDll` 안에서** 따라 올라온다" 를 지도상 길로 뒷받침할 수 있나 (예/아니오 + 사슬). 실제로 타는지는 동적 (Q20).
3. **둘째 `LdrLoadDll("KERNELBASE.dll")`** (sast/15, `LdrpInitializeKernel32Functions` ret `+0x156`): 이미 올라와 있다면 이 함수 안 어느 길로 끝나나 (1번 `LdrpFastpthReloadedDll` 인가, `LdrpFindOrPrepareLoadingModule` 이 찾은 경우인가). `+0x284 LdrpPrepareModuleForExecution` 까지 가나? (run7: 둘째 호출에서는 `LdrpCallInitRoutine` 이 안 불렸다 `[관찰]`)
4. **`LdrpPrepareModuleForExecution` 의 순서** (호출 8곳): `+0x6a LdrpAddNodeServiceTag`, `+0x76 LdrpNotifyLoadOfGraph`, `+0x88 LdrpDynamicShimModule`, `+0xd1 LdrpAcquireLoaderLock`, **`+0xea LdrpInitializeGraphRecurse`** (ret `+0xef`, run7 관찰), `+0xf9 LdrpReleaseLoaderLock`, `+0x12a LdrpCondenseGraph`. 각 갈림 조건, 특히 `+0xea` 를 타지 않는 조건 (예: 프로세스 초기화 중이면 건너뛰고 나중에 `LdrpInitializeProcess +0x1bb6` 이 한꺼번에?). caller 가 둘(`LdrpLoadDllInternal +0x27f`, `LdrpInitializeProcess +0x1aed`)인데 두 길에서 다르게 도나.
   - 이걸로 답하고 싶은 것: run7 은 kernel32 준비 때 `+0xef` 에서 그래프 켜기를 **관찰**했고, run8 은 dll1·dll2 켜기를 `LdrpInitializeProcess +0x1bbb` 에서 관찰했다. 같은 함수가 어떤 조건으로 한쪽에서는 켜고 다른 쪽에서는 미루나.
5. **`LdrpLoadDllInternal` 뒤쪽 (`+0x284 ~ 끝`)** 짧게: `+0x290 LdrpBuildForwarderLink`, `+0x2c3 LdrpFreeLoadContextOfNode`, 참조 수 내리기, `LdrpPinModule`, 패치 쪽(`LdrpApplyPatchImage`, `LdrpQueryCurrentPatch`), 예외 정리 조각 `$fin$0` (Drain·토큰 해제·진행 중 수 내리기). 결과(항목·상태)를 caller 에 돌려주는 자리.
6. **인자 대조**: sast/15 가 적은 `LdrpLoadDll +0xfb` 의 인자 9개(RCX 이름 버퍼, RDX 경로 구조체, R8D 플래그, R9D=4, `[RSP+0x30]` 항목 칸, `[RSP+0x38]` 상태 칸 …)가 이 함수 안에서 어디로 가나 (어느 callee 에 넘기나) 한 줄씩.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpLoadDllInternal$` | 1 | 질문 1·3·5·6 |
| ntdll | `^LdrpPrepareModuleForExecution$` | 1 | 질문 4 |
| ntdll | 지도 `_map/LdrInitializeThunk_edges.tsv` (이름만) | - | 질문 2 — 시스템 콜·의존 DLL 함수까지의 길 |
| ntdll | 전역 `LdrInitState` | - | 질문 1 (`refs`, 있으면 기존 것) |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpLoadDllInternal` rva `0x1fc30`, 1304바이트, 깊이 7, caller 3. 직접 호출 33줄 (본체 29 + 예외 조각 4):
  - 앞쪽: `+0xb3 LdrpFastpthReloadedDll`, `+0x185 LdrpDrainWorkQueue`, `+0x19b LdrpThreadTokenSetMainThreadToken`, `+0x1bb LdrpDetectDetour`, `+0x1ed LdrpFindOrPrepareLoadingModule`, `+0x20b LdrpProcessWork`, `+0x215 LdrpDrainWorkQueue`, `+0x23b LdrpHandleReplacedModule`, `+0x25d LdrpCondenseGraph`, **`+0x27f LdrpPrepareModuleForExecution`** (ret `+0x284`, run7 관찰), 로그 4
  - 뒤쪽: `+0x290 LdrpBuildForwarderLink`, `+0x2c3 LdrpFreeLoadContextOfNode`, `+0x2e6 LdrpDecrementModuleLoadCountEx`, `+0x2ee`/`+0x50f LdrpDereferenceModule`, `+0x312 LdrpThreadTokenUnsetMainThreadToken`, `+0x332 LdrpPinModule`, `+0x33c LdrpFreeReplacedModule`, `+0x3ac LdrpApplyPatchImage`, `+0x416 LdrpDropLastInProgressCount`, `+0x441 LdrpFindLoadedDllByHandle`, `+0x479 LdrpFreeUnicodeString`, `+0x4b1 LdrpQueryCurrentPatch`, 로그 2
  - 예외 조각 `$fin$0`: `LdrpLogError`, `LdrpDrainWorkQueue`, `LdrpThreadTokenUnsetMainThreadToken`, `LdrpDropLastInProgressCount`
- `LdrpPrepareModuleForExecution` rva `0x86e4c`, 307바이트, 깊이 5, caller 2. 직접 호출 8줄 (위 질문 4).

## 이미 아는 것

- `[관찰]` (run7) `LdrLoadDll +0x170` → `LdrpLoadDll +0x100` → `LdrpLoadDllInternal +0x284` → `LdrpPrepareModuleForExecution +0xef` → `LdrpInitializeGraphRecurse` (재귀 `+0x96`) → `LdrpInitializeNode +0x6a` → `LdrpCallInitRoutine` [KERNELBASE 추정] → 그다음 [KERNEL32 추정]. 모두 첫째 `LdrLoadDll`(`+0xc0`) 안. 둘째 `LdrLoadDll`(`+0x156`)에서는 시작 함수가 안 불렸다.
- `[관찰]` (run7·run8) import 로 붙는 dll1·dll2 는 `LdrpLoadDll` 을 타지 않았다. dll1·dll2 켜기는 `LdrpInitializeProcess +0x1bbb` 에서.
- `[정적]` (sast/15·run20) 첫째 `LdrLoadDll` 이름 = `KERNEL32.DLL`, 둘째 = `KERNELBASE.dll`. `LdrpLoadDll +0xfb` 인자 9개, R9D=4(`LoadReasonDynamicLoad` 값). `LdrpFindOrPrepareLoadingModule` 이 `LdrpHashTable`·`LdrpHeap` 을, `LdrpFastpthReloadedDll` 이 `LdrpHeap` 을 읽는다 (`_refs/`). KnownDll 쪽 읽는 함수 `LdrpFindKnownDll`/`LdrpCheckKnownDllFullPath`/`LdrpLoadKnownDll`, `LdrpAppHeaders` 읽는 함수 `LdrpMinimalMapModule`/`LdrpProcessMachineMismatch`.
- `[정적]` (sast/14·run19) `LdrpInitializeProcess +0x1a42 LdrpEnableParallelLoading` 이 일꾼·일감을 준비하고, 그 **뒤** `+0x1aa4 LdrpMapAndSnapDependency`(안에 `LdrpQueueWork`) 가 exe import 를 올린다. kernel32 준비(`+0x199d`)는 그보다 **앞**. `LdrpPrepareModuleForExecution(LdrpImageEntry, &상태)` 를 `+0x1aed` 에서 부르고 그 callee 에 `LdrpInitializeGraphRecurse +0xea` 가 있다. `LdrInitState` 는 `+0x1a4c`=1, `+0x1b1e`=2.
- `[추측]` (flow.md 2.5) kernel32 를 올리면서 kernelbase 도 따라 올리고 의존 관계 그래프를 만든다. 켜기의 `LdrpInitializeGraphRecurse` 는 그 그래프를 따라간다.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄, 질문 2 는 지도의 줄). 태그 `[정적]` / `[추측]`. 라벨은 ret 자리 (call 자리는 "(call)").
- `LdrpLoadDllInternal` 과 `LdrpPrepareModuleForExecution` 순서를 **짧은 트리 하나씩** (flow.md 2.5 `LdrLoadDll` 안쪽 트리에 붙일 수 있게).
- 질문 2 의 이름 사슬 (`A +0x.. → B +0x.. → … → Nt…`, 깊이).
- 그림 2 `(정적)` 상자 후보 2 ~ 4개 (이유 한 줄씩).
- 동적으로 확인할 것 + 멈춤점 (`ntdll+rva`, 짧게). 이미 대기 중인 Q20·Q21·Q22·Q24·Q49 와 겹치면 그 번호를.
- Q12·Q15 에 넘길 연결점 한 줄씩 (특히 그래프를 만드는 함수 후보 이름).
- 스크립트 문제는 "스크립트 문제" 절에.

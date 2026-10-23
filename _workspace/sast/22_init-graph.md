# 22 init-graph — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run27)
- 대기열: Q15 (`_workspace/plan.md`) — 2.7 `LdrpInitializeGraphRecurse`, `LdrpInitializeNode` callee
- 관련: flow.md 그림 2 (`g1`~`g3`, `n1`·`n2`, `k6`~`k11` 의 `[… 추정]` 상자), 2.7 `ntdll!LdrpInitializeGraphRecurse` 절, 2.4 "`LdrpInitializeGraphRecurse` 세 자리" / 이전 요청 sast/14 (run19, `+0x1bb6` 인자), sast/16 (run21, `LdrpPrepareModuleForExecution +0xea`), sast/19 (run24, `LdrpCondenseGraph` — 노드 State 5 → 6)

## 범위

- 대상 두 함수, depth 1: `LdrpInitializeGraphRecurse` (209바이트, 호출 2), `LdrpInitializeNode` (732바이트, 호출 11).
- callee 본문은 읽지 않는다. 특히 **`LdrpCallTlsInitializers`, `LdrpCallInitRoutine`, `ImageTlsCallbackCaller` 본문은 Q16 몫** — 이번엔 부르는 조건·넘기는 인자·결과 처리만.
- `LdrpApplyPatchImage` 는 이름·조건만. 로그(`LdrpLogInternal`·`LdrpLogDllState`)는 개수만.
- 오프셋은 call·ret 둘 다. 점프 목적지는 `절대 주소 − 함수 시작`. 다 쓴 뒤 `.asm` 과 다시 대조.
- 구조체 칸은 PDB 타입으로 (`_LDR_DDAG_NODE`, `_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_STATE`, `_LDRP_CSLIST`, `_LDR_SERVICE_TAG_RECORD` 등). PDB 에 없는 칸은 오프셋+역할로.

## 알고 싶은 것

1. **`LdrpInitializeGraphRecurse` 도는 순서**
   - 노드의 어느 목록을 따라가나 (`Dependencies` / `IncomingDependencies` / 다른 칸 — PDB 이름으로).
   - **자식을 먼저 재귀(`+0x91`, ret `+0x96`)하고 그다음 자기 노드를 켜나(`+0x65`, ret `+0x6a`)** — 예/아니오. 코드 순서가 `+0x65` 가 `+0x91` 보다 앞인데, 실제 실행 순서(점프로 돌아오는지)를 정확히.
   - 건너뛰는 조건: 노드 `State` 값별로 (예: 이미 `Initializing`(8)·`ReadyToRun`(9) 이면 건너뜀?). 같은 노드를 두 번 켜지 않게 막는 장치가 있나 (마름모 모양 의존일 때).
   - 셋째 인자 `R8` (`&바이트 0`, sast/14·16) 과 둘째 인자 `RDX` (상태 칸) 는 각각 무엇에 쓰나 (읽기/쓰기, 재귀에 그대로 넘기나).
   - 실패하면 (자식이 실패 / `LdrpInitializeNode` 가 음수) 어떻게 되나 — 바로 돌아오나, 다른 자식은 계속하나.
2. **`LdrpInitializeNode` 순서 (호출 11곳)**
   - 노드 `State` 를 쓰는 곳 전부 (값과 오프셋. 예: 7 → 8 → 9?).
   - 노드 안의 **모듈 목록(`Modules`)을 도나** — 합쳐진 노드(sast/19 `LdrpMergeNodes`)면 모듈 여럿을 차례로 켜는가. 돈다면 순서.
   - `+0x151 RtlActivateActivationContextUnsafeFast` ↔ `+0x1a9 RtlDeactivate…` 짝: 무엇의 활성화 문맥인가 (항목 `EntryPointActivationContext`?).
   - **`+0x166 LdrpCallTlsInitializers` (ret `+0x16b`) 부르는 조건** (항목 `TlsIndex`? 플래그?)과 넘기는 인자 (특히 이유 값 `DLL_PROCESS_ATTACH`=1 인지).
   - **`+0x197 LdrpCallInitRoutine` (ret `+0x19c`) 부르는 조건** (항목 `EntryPoint ≠ 0`? `Flags` 의 `ImageDll`·`ProcessAttachCalled`·`DontCallForThreads` 등 PDB 비트 이름) 과 넘기는 네 인자 (`rcx` = 진입점, `rdx` = DllBase?, `r8` = 1?, `r9` = ?).
   - **`+0x17a` 에서 `LdrpProcessInitContextRecord` 를 읽어 어디에 쓰나** (sast/14: 읽는 곳은 여기 하나). `LdrpCallInitRoutine` 의 넷째 인자(= DllMain 의 `lpReserved`) 로 가는가 — 예/아니오.
   - `LdrpCallInitRoutine` 결과 처리: FALSE(0) 를 돌려받으면 어떻게 되나 (상태 코드 값 그대로, 예 `0xc0000142`).
   - TLS 와 진입점 중 **어느 쪽을 먼저 부르나** (코드 순서 `+0x166` → `+0x197` 이 실행 순서와 같은가).
   - `+0x2fd LdrpApplyPatchImage` 조건 (핫패치일 때만?).
3. **exe 노드는?** `+0x1bb6` 의 시작 노드는 exe 항목의 노드다 (sast/14). 그런데 exe TLS 는 `LdrpInitializeProcess +0x1c31` 에서 따로 부른다 (sast/14). 그러면 `LdrpInitializeNode` 가 **exe 노드에서 TLS·진입점을 건너뛰는 조건**이 코드에 있나 (`ImageDll` 비트? 항목이 `LdrpImageEntry` 인지 비교?) — 예/아니오와 오프셋.
4. **관찰과 맞추기** (정적 조건까지):
   - run7 kernel32 길 (`LdrpPrepareModuleForExecution +0xef` 아래): KERNELBASE 는 `+0x96` 재귀 안의 `+0x6a`, KERNEL32 는 재귀 없이 `+0x6a` `[관찰]`. "자식 먼저" 로 설명되나.
   - run7·8 exe 길 (`+0x1bbb` 아래): dll1 은 2단계, dll2 는 3단계 이상 `[관찰]`. **"재귀 한 단계 = 모듈 하나"** (flow.md `[추측]`) 가 코드 모양으로 맞나. 맞지 않으면 (예: ntdll·kernel32 같은 이미 켜진 노드도 한 단계를 차지하나) 그 이유.
   - 실행 순서 dll2 → dll1 → exe `[관찰]` 이 코드로 나오나.
5. **동적 Q24 를 위한 좁히기**: 재귀 단계마다 "지금 어느 모듈인가" 를 찍으려면 입구에서 무엇을 읽으면 되나 (예: `rcx`(노드) → `Modules` → 항목 → `BaseDllName`, 오프셋 사슬). `LdrpInitializeNode` 입구도 같이.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpInitializeGraphRecurse$` | 1 | 질문 1·4·5 |
| ntdll | `^LdrpInitializeNode$` | 1 | 질문 2·3·5 |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`, 오프셋은 call / ret)

- `LdrpInitializeGraphRecurse` rva `0x87d60`, 209바이트, 깊이 5, caller 7곳: `LdrpInitializeProcess +0x16e6`·`+0x193c`·**`+0x1bb6`**, `AVrfInitializeVerifier +0x221`·`+0x295`, `LdrpInitializeImportRedirection +0x14b`, **`LdrpPrepareModuleForExecution +0xea`**, `LdrGetProcedureAddressForCaller +0x945`, `LdrpInitializeShimDllDependencies +0xcd`, 그리고 자기 재귀.
  - 호출 2: `+0x65`/`+0x6a LdrpInitializeNode`, `+0x91`/`+0x96 LdrpInitializeGraphRecurse` (재귀).
- `LdrpInitializeNode` rva `0xe5d0`, 732바이트, 깊이 6, caller 2곳: `LdrpInitializeGraphRecurse +0x65`, `LdrpLoadShimEngine +0x1f0`.
  - 호출 11: `+0xf9 LdrpLogInternal`, `+0x151 RtlActivateActivationContextUnsafeFast`, **`+0x166 LdrpCallTlsInitializers`**, **`+0x197 LdrpCallInitRoutine`**, `+0x1a9 RtlDeactivateActivationContextUnsafeFast`, `+0x24b LdrpLogDllState`, `+0x293 LdrpLogInternal`, `+0x2fd LdrpApplyPatchImage`, `+0x343 LdrpLogInternal`, SEH: `$fin$0+0xd RtlDeactivateActivationContextUnsafeFast`, `$filt$1+0x2e RtlReportException`.
  - 참고 (이번엔 이름만, Q16): `LdrpCallTlsInitializers` rva `0x10e50` 963바이트, `LdrpCallInitRoutine` rva `0x10ba0` 667바이트. `LdrpApplyPatchImage` rva `0x118460`.

## 이미 아는 것

- `[관찰]` (run7) kernel32 길: `LdrLoadDll+0x170` → `LdrpLoadDll+0x100` → `LdrpLoadDllInternal+0x284` → `LdrpPrepareModuleForExecution+0xef` → `LdrpInitializeGraphRecurse` → (KERNELBASE) `+0x96` 재귀 → `+0x6a LdrpInitializeNode` / (KERNEL32) 바로 `+0x6a`.
- `[관찰]` (run7·run8) exe 길: dll1 의 `LdrpInitializeNode` 위로 `LdrpInitializeGraphRecurse` 2단계 (`+0x6a` ← `+0x96` ← `LdrpInitializeProcess+0x1bbb`), dll2 는 3단계 이상 (`k 5` 라 잘림). 실행 순서 dll2 → dll1 → exe. dll1·dll2 진입 때 `LdrpCallInitRoutine` ret `+0x19c`.
- `[정적]` (sast/14·run19) `LdrpInitializeProcess +0x1bb6`: `RCX` = exe 항목 `DdagNode`(`[LdrpImageEntry+0x98]`), `RDX` = 상태 칸, `R8` = 바이트 0 칸, 로더 락 안. 앞 `+0x1b83` 전역 `LdrpProcessInitContextRecord` = CONTEXT, 뒤 `+0x1bec` = 0. 읽는 곳은 `LdrpInitializeNode +0x17a` 하나. exe TLS 는 따로 `+0x1c31` (exe `TlsIndex ≠ 0` 일 때).
- `[정적]` (sast/16·run21) `LdrpPrepareModuleForExecution +0xea` 도 같은 3 인자. 노드 State 7 `ReadyToInit` 이면 바로 켜기 쪽, 8·9 면 바로 `return 0`. exe 길은 로드 문맥 `+0x20` 비트 0 때문에 Prepare 안 켜기를 건너뜀.
- `[정적]` (sast/19·run24) `LdrpCondenseGraph` 가 켜기 전에 노드 State 5 → 6. test.exe·dll1·dll2 는 고리가 없어서 합치지 않음.
- `[추측]` (flow.md 2.7) 재귀 한 단계 = 모듈 하나, 맨 바깥 test.exe → dll1 → dll2. 남한테 기대는 모듈일수록 나중에 켠다.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄, call·ret 둘 다). 태그 `[정적]` / `[추측]`. 라벨은 ret.
- 두 함수를 **이어진 트리 하나** (`LdrpInitializeProcess +0x1bb6` 에서 출발해 `LdrpCallInitRoutine` 까지, State 쓰기·갈림 짧게).
- flow.md 그림 2 의 `[… 추정]` 상자 중 이번 결과로 근거가 바뀌는 것 (맞음 / 틀림 / 여전히 동적).
- 그림 2 `(정적)` 상자 후보 2 ~ 4개 (이유 한 줄씩).
- 동적으로 확인할 것 + 멈춤점 (`ntdll+rva`, 짧게). 이미 대기 중인 Q24·Q25·Q27 과 겹치면 그 번호를.
- Q16 (`LdrpCallTlsInitializers`·`LdrpCallInitRoutine`·`ImageTlsCallbackCaller`) 요청서에 미리 넘길 것 (인자 뜻 등).
- 스크립트 문제는 "스크립트 문제" 절에.

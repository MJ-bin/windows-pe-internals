# 2026-10-23 run27 — import DLL 켜기의 틀: `LdrpInitializeGraphRecurse` 는 **자식 먼저(후위 순서)**, `LdrpInitializeNode` 는 **State 8 → TLS → 진입점 → 9**, exe 노드는 **통째로 건너뜀**, `lpReserved` = CONTEXT 포인터 (정적)

> **정정 (run28)**: "TLS 콜백이든 DllMain 이든 예외를 던지면 Node 의 `__except` 가 잡는다" 에서 **TLS 콜백 쪽은 틀렸다.** TLS 콜백 예외는 안쪽 `LdrpCallTlsInitializers` 의 `__try` (`+0x12b ~ +0x1a2`, 필터 늘 1) 가 먼저 잡아 그 모듈의 남은 콜백만 건너뛰고 아무것도 위로 안 올린다. Node 의 `__except` 로 가는 건 DllMain(진입점) 예외다.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntdll 26100.9457, pdb GUID `C3093720177DA6851519DCFFE8DF53FE1` (flow.md 기준과 같음). 이미 analyze 된 프로젝트를 다시 썼다 (analyze 안 돌림).
- 버전 대조 (결과서): `_calls.tsv` 의 `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x166/+0x16b → LdrpCallTlsInitializers`, `+0x197/+0x19c → LdrpCallInitRoutine` 이 run7·run8 콜스택과 같다. 다시 뽑은 `.asm` 은 앞 것과 글자까지 같다.
- 이름: 세 함수 모두 `_index.tsv` 에 PDB 이름 그대로 있다.

| 함수 | rva | 크기 | 직접 호출 |
|---|---|---|---|
| `ntdll!LdrpInitializeGraphRecurse` (이하 GR) | `0x87d60` | 209바이트 | 2곳 |
| `ntdll!LdrpInitializeNode` (이하 Node) | `0xe5d0` | 732바이트 | **12곳** (`_calls.tsv` 는 11곳, 아래 "도구 메모") + 예외 조각 2곳 |
| `ntdll!LdrpRecordModuleDependency` (곁, depth 0) | `0xd8e40` | 326바이트 | 간선 칸 비트 0 이 켜지는 곳만 봄 |

## 목적

- Q15: flow.md 2.7 의 그래프 켜기 두 함수 본문.
  - GR 이 어느 목록을 어떤 순서로 도나, 자식 먼저인가, 무엇을 건너뛰나, 실패하면 어떻게 되나.
  - Node 가 노드 `State` 를 어떻게 바꾸나, TLS·진입점을 부르는 조건과 인자, `LdrpProcessInitContextRecord` 를 어디에 쓰나.
  - exe 노드는 어떻게 되나 (exe TLS 는 `LdrpInitializeProcess +0x1c31` 에서 따로 부르는데).
  - run7·run8 관찰 (재귀 깊이, dll2 → dll1 순서) 이 코드 모양으로 설명되나.

## 방법

- 정적: `_workspace/sast/22_init-graph.md` — ntdll, `^(LdrpInitializeGraphRecurse|LdrpInitializeNode)$` depth 1 (9개 함수 뽑음). callee 본문은 안 읽음 (`LdrpCallTlsInitializers`·`LdrpCallInitRoutine` 본문은 Q16 몫).
  - 요청서 밖으로 하나 더: `LdrpRecordModuleDependency` depth 0 (간선 칸 `+0x18` 비트 0 이 어디서 켜지나만).
- 결과서: `_workspace/sast/22_init-graph_result.md` (**분석성공**)
- 근거 파일 (레포 밖, 커밋 안 함): `ms/disassemble/ntdll/{LdrpInitializeGraphRecurse,LdrpInitializeNode,LdrpRecordModuleDependency,LdrpNotifyLoadOfGraph}.asm`, `ms/disassemble/ntdll/_calls.tsv`, `ms/disassemble/ntdll/_refs/LdrpProcessInitContextRecord.tsv`.
- 칸 이름은 PDB 타입(`llvm-pdbutil`)으로 맞췄다. Node 의 예외 착지점(`+0x1b0 ~ +0x21f`)은 Ghidra 가 안 읽은 틈이라 결과서가 원본 바이트(`objdump`)와 unwind 범위 표(`pefile`)로 읽었다.

## 결과

### 정적 — 먼저 알아 둘 칸 (PDB)

- `_LDR_DDAG_NODE` (노드): `+0x00 Modules`, `+0x28 Dependencies`, `+0x30 IncomingDependencies`, `+0x38 State`.
- `_LDR_DDAG_STATE`: -5 `LdrModulesMerged`, **-4 `LdrModulesInitError`**, -3 `LdrModulesSnapError`, 5 `Snapped`, 6 `Condensed`, 7 `ReadyToInit`, **8 `Initializing`**, **9 `ReadyToRun`**.
- 간선 칸 (PDB 이름 없음, run23 과 같은 0x20바이트): `+0x00` 다음 칸, `+0x08` 자식 노드, `+0x10` 자식 쪽 고리, `+0x18` 부모 노드 (맨 아래 비트 0 = 표시 비트).
- `_LDR_DATA_TABLE_ENTRY` (항목) 에서 쓰는 칸: `+0x20 InInitializationOrderLinks`, `+0x30 DllBase`, `+0x38 EntryPoint`, `+0x68 Flags`, `+0x6e TlsIndex`, `+0x88 EntryPointActivationContext`, `+0xa0 NodeModuleLink`, `+0xb0 LoadContext`, `+0x10c LoadReason`.
- `Flags` 비트: 5 `ProcessStaticImport` (0x20, 읽음), 19 `ProcessAttachCalled` (0x80000, 씀), 20 `ProcessAttachFailed` (0x100000, 씀). `ImageDll`·`DontCallForThreads` 는 두 함수에서 안 읽는다.

### 정적 — 호출 지점 (`_calls.tsv` + 착지점 1곳)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 (요약) | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeGraphRecurse` | `+0x91` | `+0x96` | `LdrpInitializeGraphRecurse` (재귀) | `Dependencies` 의 자식 노드 `State == 7` 일 때, 자식마다 | [정적] + [관찰] run7 |
| `ntdll!LdrpInitializeGraphRecurse` | `+0x65` | `+0x6a` | `LdrpInitializeNode` | 자식 한 바퀴가 **끝난 뒤** (지연 갈래에 안 걸리면) | [정적] + [관찰] run7 |
| `ntdll!LdrpInitializeNode` | `+0x2fd` | `+0x302` | `LdrpApplyPatchImage` | 항목 `LoadReason == 9` (패치 이미지) 만, 모듈 처리 맨 앞 | [정적] |
| `ntdll!LdrpInitializeNode` | `+0xf9` | `+0xfe` | `LdrpLogInternal` | 모듈마다 | [정적] |
| `ntdll!LdrpInitializeNode` | `+0x151` | `+0x156` | `RtlActivateActivationContextUnsafeFast` | 모듈마다 늘 (항목 `+0x88`) | [정적] |
| `ntdll!LdrpInitializeNode` | `+0x166` | `+0x16b` | `LdrpCallTlsInitializers` | 항목 `TlsIndex ≠ 0` | [정적] + [관찰] run7·run8 |
| `ntdll!LdrpInitializeNode` | `+0x197` | `+0x19c` | `LdrpCallInitRoutine` | 항목 `EntryPoint ≠ 0` | [정적] + [관찰] run7·run8 |
| `ntdll!LdrpInitializeNode` | `+0x1a9` | `+0x1ae` | `RtlDeactivateActivationContextUnsafeFast` | 정상 길 | [정적] |
| `ntdll!LdrpInitializeNode` | `+0x1fe` | `+0x203` | `LdrpLogInternal` | **예외 착지점** 안 (`_calls.tsv` 에 없음) | [정적: 원본 바이트] |
| `ntdll!LdrpInitializeNode` | `+0x24b` | `+0x250` | `LdrpLogDllState` | 성공한 모듈 | [정적] |
| `ntdll!LdrpInitializeNode` | `+0x293` | `+0x298` | `LdrpLogInternal` | 진입점이 FALSE 또는 예외 | [정적] |
| `ntdll!LdrpInitializeNode` | `+0x343` | `+0x348` | `LdrpLogInternal` | `LdrpApplyPatchImage` 실패 | [정적] |
| `LdrpInitializeNode$fin$0` | `+0xd` | `+0x12` | `RtlDeactivateActivationContextUnsafeFast` | 예외로 빠져나갈 때 (운영체제가 부름) | [정적] |
| `LdrpInitializeNode$filt$1` | `+0x2e` | `+0x33` | `RtlReportException` | 예외 필터 (늘 1 반환) | [정적] |

### 정적 — `LdrpInitializeGraphRecurse(노드, RDX, R8)` 도는 순서

1. **입구**: 노드 `State == -4` 면 바로 `0xc0000142` 를 돌려준다 (`+0x9` 비교, `+0x16` → `+0xa3`). 그 밖 값은 안 본다 (7 인지도 안 봄).
2. **자식 한 바퀴 = `Dependencies` (`+0x28`) 목록.** `IncomingDependencies` 는 안 건드린다.
   - 목록은 꼬리(`Tail`) 다음이 맨 앞인 고리다. `Tail->다음` 부터 `Tail` 까지 한 바퀴 (`+0x37` ~ `+0x59`).
   - 칸은 꼬리에 붙여 넣으니 (`LdrpRecordModuleDependency +0x89 ~ +0x92`, run23 `+0xf69`) **먼저 들어온 간선(import 앞쪽)부터** 본다.
   - 간선 칸 `+0x18` 비트 0 이 켜진 칸은 건너뛴다 (`+0x3a`/`+0x3e`). 이 비트를 켜는 곳: `LdrpRecordModuleDependency +0x11a` (칸을 새로 할당했을 때), run23 `LdrpLoadDependentModuleInternal +0x17d9`. 지우는 곳: run23 `+0xede`. 뜻("대비용 칸" 쯤)은 `[추측]`.
3. **자식 노드 `State` 로 갈림**:

| 자식 State | 하는 일 | 근거 |
|---|---|---|
| 7 `ReadyToInit` | **재귀** `+0x91` (ret `+0x96`). 돌아와서 음수가 아니면 고리 계속 | `+0x47`/`+0x4a` → `+0x89` |
| 8 `Initializing` | 재귀 안 함. 지역 깃발 `[RSP+0x40] = 1` 만 | `+0x51`/`+0x54` → `+0x9c` |
| -4 `InitError` | `0xc0000142`, 자기 `State = -4` 쓰고 return | `+0x4c`/`+0x4f` → `+0xb2` → `+0x6e` |
| 그 밖 (9 등) | 무시 (프레임을 안 만든다) | 고리 다음 칸 |

4. **자기 노드 켜기는 고리가 다 끝난 뒤**: `+0x59` 고리 끝 → (깃발 검사 `+0x5b`) → **`+0x65 LdrpInitializeNode` (ret `+0x6a`)**. 자식이 없으면 `+0x32` 에서 바로 `+0x62` 로.
   - 재귀 덩어리(`+0x89 ~ +0x9a`)는 주소로는 `RET`(`+0x87`) 뒤에 따로 있고, 고리 안에서 점프로 갔다가 `+0x98 JNS +0x56` 으로 돌아온다. 그래서 **코드 주소 순서(`+0x65` 가 앞)와 실행 순서(재귀가 앞)가 반대**다.
   - 결론: **자식 가지를 다 끝낸 뒤 자기 노드를 켠다 (후위 순서).** 형제끼리는 `Dependencies` 목록 순서.
5. **같은 노드를 두 번 안 켜는 장치 = `State` 하나.** Node 가 들어가자마자 8, 끝나면 9 로 바꾸니, 마름모 모양 의존이어도 두 번째로 만난 쪽에선 "그 밖" 이라 무시된다. 따로 "방문함" 표시는 없다.
   - 자식이 7 이 되는 곳 (GR 밖, 줄만 봄): `LdrpNotifyLoadOfGraph +0x19` 가 `State = 7`, 실패 시 `+0x29` 에서 6 으로 되돌림, 자식 재귀 `+0x61`. 이 함수는 run21 `LdrpPrepareModuleForExecution +0x7b` 이 부른다 `[정적: 줄 일부만]`.
6. **지연 갈래** (자식 중 8 이 있었을 때): `+0xb9` 로 가서
   - `+0xbc` 셋째 인자가 가리키는 바이트에 1 을 쓴다.
   - 첫 모듈 항목의 `LoadContext`(`항목+0xb0`) 가 0 이거나, 둘째 인자 `RDX` 가 `[LoadContext+0x28]` 과 같으면 → 그래도 켠다 (`+0x65`).
   - 둘 다 아니면 `+0xcf` → **켜지 않고 0 (성공) 으로 return.** `State` 는 7 그대로.
   - 뜻 `[추측]`: 다른 로드 작업이 지금 이 노드의 의존을 켜는 중이면(DllMain 안 `LoadLibrary` 같은 중첩 로드) 물러나는 장치. test.exe 시동에서 타는지는 동적.
7. **둘째·셋째 인자**:
   - `RDX` = 읽기 전용. `+0xc9` 비교 한 곳뿐, 재귀에 그대로 (`+0x8e`). run22 에서 `LoadContext+0x28` 은 "결과 상태를 쓸 곳의 포인터" 였으니, 비교 뜻은 "같은 로드 작업인가" `[정적: 비교 줄] + [추측: 뜻]`.
   - `R8` = 쓰기 전용. `+0xbc` 한 곳에서 1 을 쓴다. 재귀 때는 자기 지역 깃발 주소(`+0x89 LEA R8,[RSP+0x40]`)를 넘기니 **신호가 한 층씩 위로 올라간다.** 맨 위 호출자(`LdrpInitializeProcess +0x1bb6`, `Prepare +0xea`)는 돌아와서 그 바이트를 안 읽는다.
8. **실패**: 자식 GR 이 음수(`+0x9a`)거나 Node 가 음수(`+0x6c` 안 탐) → `+0x6e` 에서 자기 `State = -4` 쓰고 같은 값 return. **남은 형제는 안 본다** (7 그대로). 맨 위까지 조상이 모두 -4 가 된다. 이미 9 가 된 노드를 되돌리는 줄은 없다.

### 정적 — `LdrpInitializeNode(노드)` 순서

1. `+0x1f` **`State = 8`** (무조건). 노드 주소는 `+0x1a` 에서 따로 챙겨 둔다.
2. **고리 A** (`+0x44 ~ +0x2ef`): `Modules` 목록을 **꼬리에서 머리 쪽으로** 돌며, exe 항목(`LdrpImageEntry`)만 빼고(`+0x4b`/`+0x4e`) 항목의 `InInitializationOrderLinks` 를 **PEB.Ldr 초기화 순서 목록(`PebLdr+0x30`) 꼬리에 끼운다** (`+0x2d4 ~ +0x2e1`). 끼우기 전 목록 검사가 틀리면 `+0x66 INT 0x29` (fail-fast).
   - 그래서 그 목록 순서 = 켜는 순서 `[정적 추론]`. ntdll 은 run18 `LdrpInitializeProcess +0x1283 ~ +0x12d7` 에서 직접 끼워 둔다.
3. **고리 B** (`+0x7e ~ +0x262`): 같은 목록을 다시 꼬리 → 머리로, **모듈마다**:
   - 항목 == `LdrpImageEntry` → 통째로 건너뜀 (`+0x9b`/`+0x9e` → `+0x25e`).
   - `LoadReason == 9` → 먼저 `+0x2fd LdrpApplyPatchImage`. 음수면 끝 (`+0x348` → `+0x2a5`, `State = -4`).
   - `+0xbd` 전역 `LdrpCurrentDllInitializer = 항목` → `+0xf9` 로그.
   - `+0x151` 활성화 문맥 켜기 (항목 `EntryPointActivationContext`, 스택 틀 `_RTL_CALLER_ALLOCATED_ACTIVATION_CONTEXT_STACK_FRAME_EXTENDED` 크기 0x48·형식 1).
   - **TLS**: `TlsIndex ≠ 0` 이면 `+0x166 LdrpCallTlsInitializers(ECX = 1, RDX = 항목)` (ret `+0x16b`). 반환값은 버린다.
   - **진입점**: `EntryPoint ≠ 0` 이면 `+0x197 LdrpCallInitRoutine` (ret `+0x19c`), 인자 4개:
     - `RCX` = `EntryPoint` (`+0x194`), `RDX` = `DllBase` (`+0x190`), `R8D` = 1 (`+0x18a`),
     - **`R9` = `Flags.ProcessStaticImport` 가 켜져 있으면 `LdrpProcessInitContextRecord` 의 값 (첫 스레드 CONTEXT 포인터), 아니면 0** (`+0x170`·`+0x174`·`+0x17a`).
     - 반환은 `AL` 한 바이트만 성공 깃발로 쓴다 (`+0x19c`·`+0x1a0`).
   - `+0x1a9` 활성화 문맥 끄기 → `+0x220` `LdrpCurrentDllInitializer` 되돌림 → (예외가 없었으면) `+0x231 Flags |= ProcessAttachCalled`.
   - 성공이면 `+0x24b LdrpLogDllState` → 다음 모듈. **실패**(`AL == 0` 또는 예외)면 `+0x293` 로그 → `+0x298` 상태 = **`0xc0000142` (상수)** → `+0x29e Flags |= ProcessAttachFailed` → 고리 끝 (남은 모듈 안 켬).
4. 끝 `+0x2ba` **`State = 9`** (상태 0) 또는 **-4** (실패), 상태 코드 반환.
- 순서 한 줄: **8 → (초기화 순서 목록에 끼움) → 모듈마다 [활성화 → TLS → 진입점 → 끄기 → `ProcessAttachCalled`] → 9.** TLS 가 진입점보다 먼저이고, 둘 사이에 뒤로 가는 점프가 없다. run8 `[관찰]` "TLS(`+0x16b`) → 시작 함수(`+0x19c`)" 와 같다.

### 정적 — 예외 처리 (Node)

- unwind 범위 표 (결과서가 `pefile` 로 읽음): `try/except` `[+0x10e, +0x1b0)` 필터 `$filt$1`, 착지점 `+0x1b0`; `try/finally` `[+0x157, +0x1a4)` 마무리 `$fin$0`.
- 필터는 `RtlReportException` 으로 보고한 뒤 늘 1 (잡아서 실행) 을 돌려준다 (`$filt$1 +0x33`).
- 착지점 `+0x1b0`: 예외 표시 = 1, 성공 깃발 = 0, `+0x1fe` 로그 → `+0x220` 으로 합류.
- 그래서 **TLS 콜백이든 DllMain 이든 예외를 던지면 여기서 잡혀 `0xc0000142` 실패**가 된다. 이때는 `ProcessAttachCalled` 가 안 켜진다.

### 정적 — exe 노드

- Node 는 항목 포인터를 전역 `LdrpImageEntry` 와 비교해서 **exe 항목을 고리 A·B 둘 다에서 건너뛴다** (`+0x4b`, `+0x9b`). `ImageDll` 비트가 아니라 포인터 비교다.
- 그래서 exe 노드에 대해 Node 는 **`State` 8 → 9 만 쓰고 0 을 돌려준다.** 로그·활성화·TLS·진입점이 하나도 없다.
- 이 Node(exe 노드) 는 맨 바깥 GR 프레임의 `+0x65` (ret `+0x6a`) 에서 **dll1·dll2 를 다 켠 뒤** 불린다.
- exe TLS 가 `LdrpInitializeProcess +0x1c94` (ret `+0x1c99`) 에서 따로 도는 이유가 이것이다 (run19).

### 호출 관계

```
LdrpInitializeProcess  +0x1bbb (call +0x1bb6)     RCX = exe 노드, RDX = 상태 칸, R8 = &바이트 0. 로더 락 안.
│                                                 앞 +0x1b83 에서 전역 LdrpProcessInitContextRecord = CONTEXT
└─ GR(exe 노드)                                  [정적] 입구 State == -4 면 0xc0000142
   ├─ (1) +0x96  GR(재귀)  [dll1 추정]            Dependencies 에서 State 7 인 자식마다
   │     ├─ (1) +0x96  GR(재귀)  [dll2 추정]
   │     │     └─ +0x6a  LdrpInitializeNode [dll2]     (dll2 의 의존은 다 9 라 재귀 없음 — 추측)
   │     └─ (2) +0x6a  LdrpInitializeNode [dll1]
   └─ (2) +0x6a  LdrpInitializeNode [test.exe]   [정적] exe 항목이라 건너뜀, State 8 → 9 만

LdrpInitializeNode(노드) — 모듈 하나 (dll1·dll2)
 ├─ +0x1f   State = 8
 ├─ 고리 A: 항목을 PebLdr+0x30 (초기화 순서 목록) 꼬리에 끼움 (exe 빼고)
 ├─ 고리 B, 모듈마다:
 │   ├─ (LoadReason == 9 만) +0x302 LdrpApplyPatchImage
 │   ├─ +0xfe   LdrpLogInternal
 │   ├─ +0x156  RtlActivateActivationContextUnsafeFast(틀, 항목+0x88)     ┐ try/except
 │   ├─ (TlsIndex ≠ 0)   +0x16b LdrpCallTlsInitializers(1, 항목)           │ [+0x10e, +0x1b0)
 │   ├─ (EntryPoint ≠ 0) +0x19c LdrpCallInitRoutine(EntryPoint, DllBase, 1,│
 │   │                          ProcessStaticImport ? CONTEXT 포인터 : 0)  │ 결과 AL
 │   ├─ +0x1ae  RtlDeactivateActivationContextUnsafeFast                    ┘
 │   ├─ (예외 없으면) Flags |= ProcessAttachCalled
 │   └─ AL ≠ 0 → +0x250 LdrpLogDllState, 다음 모듈
 │      AL == 0 / 예외 → +0x298 로그, 상태 0xc0000142, Flags |= ProcessAttachFailed, 끝
 └─ +0x2ba  State = 9 (성공) / -4 (실패)
```

## 확인된 것

- (`[정적]`) **GR = 자식 먼저, 자기 나중 (후위 순서).** 자식은 노드 `Dependencies` 목록에서 `State == 7` 인 것만, 목록 순서대로 재귀한다. 자기 노드 Node 는 고리가 끝난 뒤 `+0x65` 에서 한 번.
- (`[정적]` + `[관찰]` run7·run8) 실행 순서 **dll2 → dll1 → exe** 가 코드에서 나온다: GR(exe) → GR(dll1) → GR(dll2) → Node(dll2) → Node(dll1) → Node(exe 노드). 관찰과 같다.
- (`[정적]` + `[관찰]` run7) kernel32 길 "KERNELBASE = `+0x96` 재귀 안의 `+0x6a`, KERNEL32 = 재귀 없이 `+0x6a`" 도 같은 모양이다 (KERNEL32 노드의 자식 KERNELBASE 가 7 이라 재귀).
- (`[정적]`) **"재귀 한 단계 = 모듈 하나" 는 코드 모양으로 맞다. 정확히는 "한 단계 = 아직 `State 7` 인 노드 하나".** 이미 9 (켜짐) 이거나 8 (켜는 중) 인 노드는 프레임을 차지하지 않는다. 그래서 ntdll·kernel32 처럼 이미 켜진 모듈은 깊이에 안 들어간다 (run7·run8 "dll1 2단계, dll2 3단계" 와 맞음). 어느 프레임이 어느 모듈인지는 실행 값이라 여전히 동적 (Q24).
- (`[정적]`) **같은 노드를 두 번 안 켜는 장치는 노드 `State` (7 → 8 → 9) 하나뿐.**
- (`[정적]`) Node 의 `State` 쓰기: 입구 `+0x1f` = 8, 끝 `+0x2ba` = 9 또는 -4. GR 은 실패 때 `+0x6e` = -4.
- (`[정적]`) Node 는 노드 안 `Modules` 목록을 **두 번** 돈다 (꼬리 → 머리): 고리 A 는 PEB.Ldr 초기화 순서 목록에 끼우기, 고리 B 는 모듈마다 켜기. 합쳐진 노드(run24 `LdrpMergeNodes`)면 모듈 여럿을 차례로 켜고, 하나라도 실패하면 나머지는 안 켠다.
- (`[정적]`) TLS 는 `TlsIndex ≠ 0`, 진입점은 `EntryPoint ≠ 0` 일 때만. 둘 다 이유 값 1 (`DLL_PROCESS_ATTACH`). TLS 쪽 둘째 인자는 `DllBase` 가 아니라 **항목 포인터**다.
- (`[정적]`) **`LdrpProcessInitContextRecord` 를 읽는 유일한 곳(`+0x17a`) = `LdrpCallInitRoutine` 의 넷째 인자 `R9`.** 정적 import 모듈(`Flags.ProcessStaticImport`) 이면 첫 스레드 CONTEXT 포인터, 아니면 0. 이게 DllMain 의 `lpReserved` 로 가는지는 `LdrpCallInitRoutine` 본문 몫 `[추측]` (Q16·Q27).
- (`[정적]`) DllMain 이 FALSE 면 상태는 늘 `0xc0000142` (상수), `ProcessAttachCalled`·`ProcessAttachFailed` 둘 다 켜진다. 예외면 `0xc0000142` + `ProcessAttachFailed` 만.
- (`[정적]`) **exe 노드는 Node 가 통째로 건너뛴다** (항목 == `LdrpImageEntry`). Node(exe 노드) 는 `State` 8 → 9 만. exe TLS 가 따로 도는 이유.
- (`[정적]` + `[관찰]` run8 표) run8 의 멈춤점은 `LdrpCallTlsInitializers`·`LdrpCallInitRoutine` 이었다 (부른 곳 Node `+0x16b`·`+0x19c`). exe 노드는 거기까지 안 가니 표에 없는 게 코드와 맞다.
- (`[정적]` 조건 + `[관찰]` run8) run8 의 kernel32 길에서 `LdrpCallTlsInitializers` 는 **한 번만** 보였다 (3번 줄, 콜백 호출 없음). 부르는 조건이 `TlsIndex ≠ 0` 뿐이니, 그 모듈(KERNELBASE 추정)은 `TlsIndex ≠ 0`, 다른 쪽은 0 이었다는 뜻이다. 콜백을 안 부른 이유는 `LdrpCallTlsInitializers` 본문 (Q16).

## 정정

- 이전 run 문서·flow.md 에서 노드 `State -4` 를 `SnapError` 로 읽은 곳은 **없다** (`grep` 으로 확인). run24·flow.md 2.7 의 `-3 SnapError` 는 PDB 와 맞다. -4 = `LdrModulesInitError` 는 이번에 처음 적는다.
- 요청서(sast/22) "Node 호출 11" 은 실제 **12곳** (예외 착지점 `+0x1fe` 빠짐, 아래 도구 메모). 요청서는 run 이 아니라 정정 한 줄을 안 단다.
- flow.md 2.7 의 `[추측]` "재귀 한 단계 = 모듈 하나" 는 틀리지 않았다. 다만 정확한 말은 "아직 켜지지 않은(State 7) 노드 하나" 다 (flow.md 에 그렇게 고쳐 적음).

## 아직 확인 필요

- **동적 Q24 (재귀 단계 ↔ 모듈)** — 이번에 좁혀짐. 오프셋은 `ntdll + rva`, 이번 부팅 값 아님:
  - GR 입구 `ntdll+0x87d60`, Node 입구 `ntdll+0xe5d0`. 둘 다 `rcx` = 노드.
    - 이름: `dt ntdll!_LDR_DATA_TABLE_ENTRY (poi(@rcx)-0xa0) BaseDllName` (`Modules.Flink` 가 항목 `+0xa0 NodeModuleLink` 를 가리킴. 합쳐진 노드면 첫 모듈만).
    - 노드 값: `dd @rcx+0x38 L1` (State), 같은 줄에 `k` 로 GR 프레임 수.
  - 예측: GR 입구 exe → dll1 → dll2 (프레임 1·2·3), Node 입구 dll2 (3겹) → dll1 (2겹) → **exe 노드 (1겹)**. kernel32 길은 KERNELBASE (2겹) → KERNEL32 (1겹).
  - 지연 갈래 안 타나: `ntdll+0x87e2f` (켜지 않고 0 반환) 에 **안 설 것** `[추측]`. 같이 `ntdll+0x87e12` (자식 -4), `ntdll+0x87dce` (`State = -4` 쓰기) 도 안 설 것.
  - (선택) `State` 쓰기: `ntdll+0xe5ef` (8), `ntdll+0xe88a` (`ecx` = 9 예측).
  - (선택) `main` 에서 `PEB.Ldr` 초기화 순서 목록을 따라가면 ntdll, KERNELBASE, KERNEL32, dll2, dll1 (exe 없음) 일 것 `[정적 추론]`.
- **동적 Q27 (`DllMain` 의 `lpReserved`)**: `ntdll+0xe767` (`LdrpCallInitRoutine` call) 에서 `r rcx, rdx, r8, r9` 와 `dq ntdll!LdrpProcessInitContextRecord L1` 이 같은지, dll1·dll2 `DllMain` 입구 `r8` 이 같은 값인지. 반환 `ntdll+0xe76c` 에서 `al`.
- **동적 Q25 (TLS 쪽)**: `ntdll+0xe736` (`LdrpCallTlsInitializers` call) 에서 `ecx = 1`, `dt ntdll!_LDR_DATA_TABLE_ENTRY @rdx BaseDllName TlsIndex`.
- **정적 Q16 에 넘길 것**: `LdrpCallTlsInitializers` 인자 = (1, 항목 포인터), 반환값은 버려짐 → 실패는 예외로만. `LdrpCallInitRoutine` 인자 = (진입점, `DllBase`, 1, `R9`), 반환은 `AL`. `r8 → DllMain rdx` 처럼 한 칸씩 미는지, `R9` 가 `lpReserved` 로 가는지. TLS 쪽 호출(`LdrpCallTlsInitializers +0x19b`)에서 네 칸에 무엇을 넣는지. `TlsIndex` 를 누가 쓰나.
- **정적 Q54** (뜻을 못 정한 것 중 둘): 항목 `Flags.ProcessStaticImport`(0x20) 를 누가 켜나 (dll1·dll2 에 켜져 있을 것 `[추측]` — 이번 두 함수는 읽기만), 간선 칸 `+0x18` 비트 0 의 뜻 (`LdrpRecordModuleDependency +0x11a` 가 켬, "대비용 칸" `[추측]`).
- 그 밖에 뜻을 못 정한 것 (`[추측]` 그대로): `RDX` 비교 = "같은 로드 작업" 인가, 이 시점에 `LoadContext` 가 살아 있나, ntdll 노드 `State` (9 로 짐작 — `LdrpInitializeNtdllDataTableEntry` 본문 안 읽음).

## 디버거 메모 / 도구 메모

- **`_calls.tsv` 가 Node 의 호출 1곳을 빠뜨렸다**: `+0x1fe` (ret `+0x203`) `LdrpLogInternal`. Ghidra 가 점프로 안 닿는 **예외 착지점**(`+0x1b0 ~ +0x21f`, `.asm` 의 "읽지 않은 틈")을 코드로 안 따라가서다. 결과서가 원본 바이트와 unwind 범위 표로 찾았다. 이 문서는 사실(12곳)만 적었고, 스크립트 고치기 제안은 결과서 "스크립트 문제" 절에 있다.
- 결과서 2-10 의 "`+0x348 JMP +0x875`" 는 절대 주소 끝자리(`0x18000e875`)를 그대로 적은 것이다. 함수 시작 기준으로는 **`+0x2a5`** (`.asm` 확인). 이 문서는 `+0x2a5` 로 적었다.

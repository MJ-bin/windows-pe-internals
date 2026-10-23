# 22 init-graph — 정적 분석 결과

- 요청서: `_workspace/sast/22_init-graph.md`
- 모듈 / 바이너리: ntdll (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/ntdll.dll`), pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`). analyze 는 다시 안 돌림 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음).
- 뽑은 함수: 9개 (정규식 `^(LdrpInitializeGraphRecurse|LdrpInitializeNode)$`, depth 1) → `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`. 본문을 읽은 건 두 함수뿐. callee (`LdrpCallTlsInitializers`, `LdrpCallInitRoutine`, `LdrpApplyPatchImage`, 로그 둘, Rtl 활성화 둘) 는 뽑기만 했고 읽지 않았다.
  - 요청서 밖으로 **하나** 더 뽑음: `LdrpRecordModuleDependency` (depth 0, 326바이트). 이유: `LdrpInitializeGraphRecurse` 가 "간선 칸 `+0x18` 비트 0" 을 보고 건너뛰는데, 그 비트가 어디서 켜지는지 보려고. 이 함수 안만 보고 더 안 넓혔다.
- PDB 타입 확인: `llvm-pdbutil-14 dump -types` 를 스크래치 폴더에 떠서 봄 (커밋 안 함). `_LDR_DDAG_NODE`, `_LDR_DDAG_STATE`, `_LDR_DATA_TABLE_ENTRY` (+Flags 비트), `_LDR_DLL_LOAD_REASON`, `_PEB_LDR_DATA`, `_RTL_CALLER_ALLOCATED_ACTIVATION_CONTEXT_STACK_FRAME_EXTENDED`.
- 버전 확인: **일치.** `_calls.tsv` 에서 `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x166/+0x16b → LdrpCallTlsInitializers`, `+0x197/+0x19c → LdrpCallInitRoutine` (알려진 사실 표 그대로). 이번에 쓴 오프셋 약 90개 (`+0x…` 줄과 점프 목적지)를 `.asm` 과 스크립트로 다시 맞춰 봤고 다 맞았다.
- 다시 뽑은 `.asm` 은 어제 것과 **글자까지 같다** (`diff` 없음).

## 스크립트 문제 (고치지 않음, 제안만)

- **`LdrpInitializeNode` 호출 한 곳이 `_calls.tsv`·지도에서 빠져 있다.**
  - 증상: 요청서는 "호출 11" 이지만 실제는 12곳. 빠진 것 = `+0x1fe` (ret `+0x203`) `LdrpLogInternal` (로그 줄 번호 `0x606`).
  - 어디에 있나: `.asm` 맨 끝 "읽지 않은 틈 `+0x1b0~+0x21f`" 안. 점프로 안 닿는 **예외 착지점** (`__except` 안쪽)이다. 이미 문서에 있는 "읽지 않은 틈" 한계 그대로이고, 이번엔 그 안에 진짜 호출이 있었다.
  - 어떻게 알았나: `objdump -d` 로 `0x18000e780~0x18000e7f0` 을 읽음 (`call 0x180011c80` 가 `0x18000e7ce`). 그리고 `pefile` 로 이 함수의 unwind 정보를 읽어 `__C_specific_handler` 범위 표를 봤다: `[+0x10e, +0x1b0)` 필터 `$filt$1` 착지점 `+0x1b0`, `[+0x157, +0x1a4)` 마무리 `$fin$0`.
  - 이번 결과에 주는 영향: 호출 개수만 달라짐 (로그 4곳으로 셈). 아래 답은 직접 읽은 걸로 보정했다.
  - 고칠 방법 제안: `ExportFunctions.java` 가 `.pdata`/unwind 의 C 범위 표(`ScopeRecord.JumpTarget`)를 "닿는 코드 시작점" 으로 같이 따라가게. 그러면 착지점이 `.asm` 본문과 `_calls.tsv` 에 들어온다.

## 질문별 답 [정적]

근거 파일: `ms/disassemble/ntdll/LdrpInitializeGraphRecurse.asm` (이하 **GR**), `ms/disassemble/ntdll/LdrpInitializeNode.asm` (이하 **Node**). 디컴파일 `.c` 는 구조 확인용으로만 봤고, 흐름은 전부 `.asm` 으로 셌다. 오프셋은 함수 시작 기준, 호출은 `call → ret` 둘 다.

### 먼저 구조체 (PDB 이름, 모르는 칸은 오프셋+역할)

- `_LDR_DDAG_NODE` (0x50바이트, 이하 "노드"): `+0x00 Modules`(목록 머리), `+0x28 Dependencies`, `+0x30 IncomingDependencies`, `+0x38 State`. (둘 다 `_LDRP_CSLIST`, 칸은 `Tail` 하나)
- `_LDR_DDAG_STATE` (PDB 이름): -5 `LdrModulesMerged`, **-4 `LdrModulesInitError`**, -3 `SnapError`, 5 `Snapped`, 6 `Condensed`, 7 `ReadyToInit`, **8 `Initializing`**, **9 `ReadyToRun`**.
  - 주의: 이전 문서 일부가 -4 를 "SnapError" 로 읽을 수 있는데, PDB 는 -4 = `InitError`, -3 = `SnapError`.
- 간선 칸 (이름 없음, 0x20바이트, sast/18 과 같음): `+0x00` 다음 칸 (목록 고리), `+0x08` **자식 노드**, `+0x10` 자식의 `IncomingDependencies` 고리, `+0x18` 부모 노드 (맨 아래 비트 0 = 표시 비트).
- `_LDR_DATA_TABLE_ENTRY` 쓰는 칸: `+0x20 InInitializationOrderLinks`, `+0x30 DllBase`, `+0x38 EntryPoint`, `+0x48 FullDllName`, `+0x58 BaseDllName`, `+0x68 Flags`, `+0x6e TlsIndex`, `+0x88 EntryPointActivationContext`, `+0x98 DdagNode`, `+0xa0 NodeModuleLink`, `+0xb0 LoadContext`, `+0x10c LoadReason`.
- `Flags` 비트 (PDB): 비트 5 = `ProcessStaticImport` (0x20), 비트 19 = `ProcessAttachCalled` (0x80000), 비트 20 = `ProcessAttachFailed` (0x100000). 이 두 함수가 `Flags` 에서 읽는 건 **0x20 하나**, 쓰는 건 **0x80000·0x100000 둘**. `ImageDll`·`DontCallForThreads` 는 이 두 함수에서 안 읽는다.

### 질문 1. `LdrpInitializeGraphRecurse` 도는 순서

**1-1. 어느 목록을 따라가나 → `Dependencies` (`+0x28`).** `IncomingDependencies` 는 안 건드린다.
- 근거 GR `+0x26` (`RSI = [노드+0x28]` = `Dependencies.Tail`), `+0x37` (`RBX = [RBX]` 다음 칸), `+0x40` (`RCX = [RBX+0x8]` = 자식 노드).
- 목록 모양: 꼬리(`Tail`)의 "다음"이 맨 앞 칸인 고리. `+0x37` 이 `Tail->다음` 부터 시작해서 `Tail` 칸 자신까지 한 바퀴 (`+0x56 CMP RBX,RSI`, `+0x59 JNZ +0x37`). 칸을 넣을 때 꼬리에 붙이니 (`LdrpRecordModuleDependency +0x89~+0x92`, sast/18 `LdrpLoadDependentModuleInternal +0xf69`) **먼저 들어온 간선(= import 앞쪽)부터** 본다.
- 건너뛰는 칸: `+0x3a TEST [RBX+0x18],1; +0x3e JNZ +0x56`. **간선 칸 `+0x18` 비트 0 이 켜진 칸은 아예 안 본다.**
  - 이 비트가 켜지는 곳 (지금까지 본 곳): `LdrpRecordModuleDependency +0x11a` (자기가 칸을 새로 할당했을 때 `OR [칸+0x18],1`), sast/18 `LdrpLoadDependentModuleInternal +0x17d9` ("대비용으로 할당한 칸"). 반대로 보통 칸은 `+0xede` 에서 비트가 지워진다 (sast/18). 그래서 뜻은 "대비용 칸 표시" 쯤 `[추측]`. **GR 이 이런 칸을 무시한다는 사실은 `[정적]`.**

**1-2. 자식 먼저 재귀 → 자기 나중. → 예.** (코드 순서는 `+0x65` 가 `+0x91` 보다 앞이지만 실행은 반대)
- 모양: 재귀 호출 덩어리(`+0x89~+0x9a`)는 함수 끝쪽 `RET`(`+0x87`) **뒤에** 따로 놓여 있고, 고리 안에서 점프로 갔다가 돌아온다.
  - 가기: `+0x47 CMP EAX,7; +0x4a JZ +0x89` → `+0x89 LEA R8,[RSP+0x40]` (3번째 인자 만들기) → `+0x8e MOV RDX,RBP` (2번째 인자 그대로) → **`+0x91 CALL GR` (ret `+0x96`)** (`RCX` 는 `+0x40` 에서 이미 자식 노드).
  - 돌아오기: `+0x96 TEST EAX; +0x98 JNS +0x56` → 고리 검사로 돌아감. 음수면 `+0x9a JMP +0x6e` (실패 쪽).
  - 자기 노드 켜기는 고리가 **다 끝난 뒤**에만 닿는다: `+0x59` 고리 끝 → `+0x5b CMP [RSP+0x40],0; +0x60 JNZ +0xb9`(지연 검사, 1-3) → `+0x62 MOV RCX,RDI` → **`+0x65 CALL LdrpInitializeNode` (ret `+0x6a`)**. 자식이 없으면(`+0x32 JZ +0x62`) 바로 여기.
- 그래서 **자식 쪽 가지를 다 끝낸 뒤(후위 순서) 자기 노드를 켠다.** 같은 형제끼리는 `Dependencies` 목록 순서.

**1-3. 건너뛰는 조건 (노드 `State` 값별) 과 같은 노드를 두 번 안 켜는 장치**

| State | 자기 노드 (입구) | 자식 노드 (고리 안) |
|---|---|---|
| -4 `InitError` | `+0x9 CMP [RCX+0x38],-4; +0x16 JZ +0xa3` → `+0xa3 EAX=0xc0000142` 로 바로 return. **State 쓰기 없음** | `+0x4c CMP EAX,-4; +0x4f JZ +0xb2` → `0xc0000142`, 자기 State = -4 로 쓰고 return (실패) |
| 7 `ReadyToInit` | 검사 안 함 (아래 참고) | `+0x47 CMP 7; +0x4a JZ +0x89` → **재귀** |
| 8 `Initializing` | 검사 안 함 | `+0x51 CMP 8; +0x54 JZ +0x9c` → 재귀 안 함, 지역 깃발 `[RSP+0x40] = 1` 만 켬 |
| 그 밖 (9, 0~6, -1~-3, -5) | 검사 안 함 | 무시 (재귀도 실패도 아님) |

- **같은 노드를 두 번 켜지 않는 장치 = `State` 하나.** 별도 "방문함" 표시는 없다. 자식은 `State == 7` 일 때만 내려가고, `LdrpInitializeNode` 가 들어가자마자 8 (`Node +0x1f`), 끝나면 9 (`Node +0x2ba`) 로 바꾼다. 마름모 의존 (A→B, A→C, B→D, C→D) 이면 D 는 B 쪽에서 켜져 9 가 되고, C 쪽에서는 "그 밖" 이라 무시된다.
- **자기 노드가 7 인지는 GR 안에서 안 본다** (-4 만 봄). 막는 건 부르는 쪽이다: `LdrpPrepareModuleForExecution` 의 State 갈림 (8·9 면 바로 return, sast/16) 과 부모 GR 의 `== 7` 검사.
- 자식이 7 이 되어 있는 이유 (GR 밖, 줄만 봄): `Prepare +0x76/+0x7b LdrpNotifyLoadOfGraph` 가 자식(State 6)을 먼저 재귀하며 7 로 올린다 (`LdrpNotifyLoadOfGraph.asm +0x19` 에서 `State = 7`, 실패 시 `+0x29` 에서 6 으로 되돌림, 재귀 `+0x61`). 본문은 이번 범위 밖 `[정적: 줄 일부만]`.
- **"지금 초기화 중" 자식(8)을 만났을 때 (지연 갈래)**: 고리가 끝났는데 깃발이 켜져 있으면 (`+0x5b~+0x60`) → `+0xb9`:
  1. `+0xbc` 3번째 인자가 가리키는 바이트에 1 을 쓴다 (부모에게 신호).
  2. `+0xb9 RAX=[노드]` (`Modules.Flink`), `+0xc0 RCX=[RAX+0x10]` = 첫 모듈 항목의 **`LoadContext`(`+0xb0`)**.
  3. `LoadContext == 0` (`+0xc7 JZ +0x62`) 이거나 `RDX == [LoadContext+0x28]` (`+0xc9 CMP RBP,[RCX+0x28]; +0xcd JZ +0x62`) 이면 → 그래도 켠다 (`+0x65`).
  4. 둘 다 아니면 `+0xcf XOR EAX,EAX; JMP +0x75` → **켜지 않고 0 (성공) 으로 return.** 노드 State 는 7 그대로 (쓰기 없음).
  - 뜻 `[추측]`: 다른 로드 작업이 이 노드가 기대는 모듈을 지금 초기화하고 있으면(보통 DllMain 안에서 `LoadLibrary` 한 중첩 로드), 이 로드 작업이 먼저 켜지 않고 물러난다. 단 같은 로드 작업(`LoadContext+0x28` 이 같음)이면 안 물러난다. **`ProcessAttach` 순서를 지키려는 장치로 보임.** test.exe 시동에서 이 갈래를 타는지는 코드로 못 정한다 → 동적.

**1-4. 셋째 인자 `R8`, 둘째 인자 `RDX`**
- **`RDX` (GR 안에서 `RBP`)**: **읽기 전용, 비교에만.** `+0xc9` 한 곳에서 `[첫 모듈 LoadContext+0x28]` 과 같은지만 본다. 재귀에는 그대로 넘긴다 (`+0x8e MOV RDX,RBP`). 쓰는 줄 없음.
  - `LoadContext+0x28` 은 sast/17 에서 "결과 NTSTATUS 를 쓸 곳을 가리키는 포인터". 그러니 `RDX` = 이 로드 작업의 상태 칸 주소, 비교 = "이 노드가 **같은 로드 작업** 소속인가" `[정적: 비교 줄] + [추측: 이름]`. 호출한 쪽: `LdrpInitializeProcess +0x1bb6` 은 `[RSP+0x2d8]`, `Prepare +0xea` 은 Prepare 자기 2번째 인자 (`+0xe2 MOV RDX,RSI`).
- **`R8` (GR 안에서 `R14`)**: **쓰기 전용.** 쓰는 곳은 `+0xbc` 하나 (`*R14 = 1`). 이 함수 안에서 읽지 않는다. 재귀할 때 자기 지역 변수의 주소(`+0x89 LEA R8,[RSP+0x40]`)를 넘기니 **신호가 한 층씩 위로 올라간다** (자식이 "내 의존 중 초기화 중인 게 있었다" → 부모의 깃발 = 1 → 부모도 같은 지연 검사).
  - 맨 위 호출자는 돌아와서 안 읽는다: `LdrpInitializeProcess` `[RSP+0x56]` 은 `+0x1b9d` 에서 0 으로 쓰고 `+0x1ba2` 에서 주소만 줌, 다른 읽는 줄 없음. `Prepare` `[RSP+0x50]` 도 `+0xe5` 쓰기·`+0xdd` 주소 주기뿐.

**1-5. 실패하면**
- 자식 GR 이 음수 (`+0x98 JNS` 안 탐) → `+0x9a JMP +0x6e` → **`+0x6e` 자기 State = `-4` 로 쓰고** 같은 값 return. **형제 자식은 더 안 본다.** 아직 안 켠 형제는 State 7 그대로.
- `LdrpInitializeNode` 가 음수 (`+0x6a TEST EAX; +0x6c JNS +0x75` 안 탐) → 같은 `+0x6e` (-4 쓰고 return). (Node 도 끝에서 -4 를 쓰니 이중으로 쓴다.)
- 자식이 이미 -4 → `0xc0000142` + `+0x6e`.
- 맨 위까지 이 사슬로 올라간다 → 조상 노드는 전부 -4. **이미 9 가 된 노드를 되돌리는 줄은 이 두 함수에 없다** (내리기·언로드 없음). 맨 위(`LdrpInitializeProcess +0x1bbb`)는 EAX 를 `[RSP+0x50]` 에 받아 `+0x1c0e` 쪽 실패로 간다 (run19).

### 질문 2. `LdrpInitializeNode` 순서

**2-1. `State` 쓰기 전부**
| 오프셋 | 쓰는 값 | 조건 |
|---|---|---|
| Node `+0x1f` | **8** (`Initializing`) | 함수 들어오자마자, 무조건 (`[RCX+0x38]`) |
| Node `+0x2ba` | **9** (`ReadyToRun`) 또는 **-4** (`InitError`) | `+0x2a5 ECX=9; +0x2aa EAX=-4; +0x2af TEST R14D; +0x2b2 CMOVNZ` → `R14D`(상태 코드) 가 0 이면 9, 아니면 -4. 저장할 노드 주소는 `+0x1a` 에서 `[RSP+0x68]` 에 따로 챙겨 둔 처음 `RCX` |
| GR `+0x6e` | **-4** | 자식 또는 Node 가 음수 (1-5) |
- 즉 **7 → 8 → 9** 맞다 (7 은 부르기 전에 다른 곳이 쓴 것). 실패는 8 → -4.

**2-2. 노드 안 모듈 목록(`Modules`)을 도나 → 예, 두 번 돈다.** 둘 다 목록 **꼬리(`Blink`, `[노드+8]`)에서 머리 쪽으로 거꾸로** 간다 (`+0x26 RAX=[RCX+8]`, `+0x73 RSI=[RBX+8]`, 다음은 `[RSI+8]`). 항목 주소 = 목록 칸 − 0xa0 (`NodeModuleLink`, `+0x44`, `+0x8f`). 목록이 비면 둘 다 건너뜀 (`+0x34 JZ +0x68`, `+0x86 CMP RSI,RBX; +0x89 JZ +0x2a5`).
- **고리 A (`+0x44 ~ +0x2ef`) — 아직 아무것도 안 켠 상태에서 먼저.** exe 항목(`LdrpImageEntry`)만 빼고(`+0x4b CMP RDX,RCX; +0x4e JZ +0x2e8`) 모든 모듈 항목의 `InInitializationOrderLinks`(`항목+0x20`) 를 **PEB 로더 데이터의 초기화 순서 목록 `InInitializationOrderModuleList`(`PebLdr+0x30` 머리, `+0x38` 꼬리) 꼬리에 끼운다** (`+0x2d4~+0x2e1`). 끼우기 전에 `꼬리.Flink == 머리` 검사 (`+0x58 CMP [R8],R9`), 틀리면 `+0x61 MOV ECX,3; +0x66 INT 0x29` (fail-fast, 목록 깨짐).
  - 그래서 이 목록의 순서 = 켜는 순서 `[정적 추론]`. ntdll 은 `LdrpInitializeProcess +0x1283~+0x12d7` 에서 직접 끼워 둠 (flow.md 2.4), exe 는 여기서 빠짐.
- **고리 B (`+0x7e ~ +0x262`) — 모듈마다 하나씩 켠다.** 합쳐진 노드(sast/19 `LdrpMergeNodes`)면 모듈이 여럿이고 이 고리가 차례로 켠다. **한 모듈이라도 실패하면 (`+0x23f JZ +0x267`) 고리를 끝내고 (`+0x2a5` 로) 나머지 모듈은 안 켠다.** test.exe·dll1·dll2 는 노드당 모듈 하나 (고리 없음, sast/19).

**2-3. 활성화 문맥 `+0x151 RtlActivateActivationContextUnsafeFast` ↔ `+0x1a9 RtlDeactivate…` 짝**
- 무엇의 문맥: **그 모듈 항목의 `EntryPointActivationContext` (`항목+0x88`)** (`+0x145 MOV RDX,[RDI+0x88]`). 값은 `LdrpFindDllActivationContext` 가 넣는다 (sast/17).
- 틀: 스택 `RSP+0x70` 에 크기 `0x48` (`+0x10e`), 형식 `1` (`+0x117`), 나머지 0 으로 채운 구조체 = PDB `_RTL_CALLER_ALLOCATED_ACTIVATION_CONTEXT_STACK_FRAME_EXTENDED` (Size 0x48, Format 1) 를 `RCX` 로 넘김 (`+0x14c`).
- **모듈마다 무조건** 켜고 (TLS·진입점 없어도) 끈다. 끄기는 정상 길에서 `+0x1a9`, 예외로 빠져나갈 때는 `$fin$0 +0xd` (같은 함수, 틀 `RBP+0x70`).

**2-4. `+0x166 LdrpCallTlsInitializers` (ret `+0x16b`)**
- **조건: 항목 `TlsIndex`(`+0x6e`, 16비트) ≠ 0.** `+0x157 CMP word [RDI+0x6e],0; +0x15c JZ +0x16b`. `Flags` 는 안 본다.
- 인자: `ECX = 1` (`+0x161`, **`DLL_PROCESS_ATTACH`**), `RDX = 항목 포인터` (`+0x15e MOV RDX,RDI`; DllBase 가 아니라 **항목 자체**).
- **반환값은 안 쓴다** (`+0x16b` 바로 `TEST R15,R15`). 실패 신호는 예외뿐 (2-9).

**2-5. `+0x197 LdrpCallInitRoutine` (ret `+0x19c`)**
- **조건: 항목 `EntryPoint`(`+0x38`) ≠ 0** (`+0xc4 R15=[RDI+0x38]`, `+0x16b TEST R15,R15; +0x16e JZ +0x1a4`). `ImageDll`·`ProcessAttachCalled`·`DontCallForThreads` 같은 비트는 **안 본다.** 이미 불렸는지(`ProcessAttachCalled`) 도 안 본다 (중복 방지는 State 가 한다).
- 인자 4개 (`Node +0x170 ~ +0x197`):
  - `RCX` = 진입점 `EntryPoint` (`+0x194 MOV RCX,R15`)
  - `RDX` = `DllBase` (`+0x190 MOV RDX,[RDI+0x30]`)
  - `R8D` = 1 (`+0x18a`, `DLL_PROCESS_ATTACH`)
  - `R9` = `lpReserved` 자리 (2-6)
- 반환: `+0x19c MOVZX R12D,AL; +0x1a0 MOV [RSP+0x40],AL` → **`AL`(BOOLEAN) 한 바이트**를 성공 깃발로 쓴다 (DllMain 이 FALSE 면 0).

**2-6. `+0x17a` 의 `LdrpProcessInitContextRecord` → 어디에 쓰나 → 예, `LdrpCallInitRoutine` 의 넷째 인자 `R9` 로 간다.**
- `+0x170 TEST [RDI+0x68],0x20` (**`Flags.ProcessStaticImport`**) → `+0x174 MOV R9D,0` → `+0x17a CMOVNZ R9,[LdrpProcessInitContextRecord]` (8바이트 **값**을 읽음, 주소가 아니라). 그러니 `R9` = (정적 import 모듈이면 **첫 스레드 CONTEXT 레코드 포인터**, 아니면 0). `+0x182 MOV [RSP+0xc0],R9` 로 지역 변수에도 복사하지만 이 함수 안에서 다시 읽는 줄은 없다. `R9` 는 `+0x197` 까지 안 바뀐다 (`+0x18a`, `+0x190`, `+0x194` 는 `R8`·`RDX`·`RCX` 만 씀).
- 그 전역은 8바이트 포인터이고 `LdrpInitializeProcess +0x1b83` 에서 CONTEXT 포인터를 넣고 `+0x1bec` 에서 0 으로 지운다 (`_refs/LdrpProcessInitContextRecord.tsv`). 읽는 곳은 여기 하나.
- 뜻: DllMain 의 세 번째 인자 `lpReserved`. **정적 import 로 붙은 DLL 은 CONTEXT 포인터를, 나중에 `LoadLibrary` 로 붙은 DLL 은 NULL 을 받는다** `[정적: R9 까지] + [추측: LdrpCallInitRoutine 이 한 칸씩 밀어 DllMain 으로 넘김. 관찰 "LdrpCallInitRoutine r8=1 이 DllMain rdx=1" 과 같은 모양, 확인은 Q16·Q27]`.

**2-7. 결과 처리: FALSE(0) 면**
- `+0x23c TEST R12B,R12B; +0x23f JZ +0x267` → `+0x293 LdrpLogInternal` (줄 `0x614`), **`+0x298 R14D = 0xc0000142`** (`STATUS_DLL_INIT_FAILED`, 상수 — DllMain 이 돌려준 값이 아니라 늘 이 값), `+0x29e Flags |= 0x100000` (**`ProcessAttachFailed`**) → `+0x2a5` 로 가서 State = -4, return `0xc0000142`.
- 참고: `Flags |= 0x80000` (**`ProcessAttachCalled`**) 는 그 앞 `+0x231` 에서 이미 켜졌다 (`+0x227 CMP [RSP+0x118],0; +0x22f JNZ +0x238` — **예외가 안 났을 때만** 켬). 그래서 DllMain 이 FALSE 면 `ProcessAttachCalled` 와 `ProcessAttachFailed` 둘 다 켜진다. 진입점이 0 이어도(= 안 불렀어도) `ProcessAttachCalled` 는 켜진다.

**2-8. TLS 와 진입점 중 어느 쪽 먼저 → TLS 먼저, 코드 순서와 같다.**
- `+0x166` TLS → `+0x197` 진입점, 둘 사이에 뒤로 가는 점프 없음. 같은 활성화 문맥 틀 안 (`+0x151` 켜기 → … → `+0x1a9` 끄기). 한 모듈의 순서: 로그(`+0xf9`) → `LdrpCurrentDllInitializer = 이 항목` (`+0xbd`) → 활성화 → TLS(`TlsIndex≠0`) → 진입점(`EntryPoint≠0`) → 비활성화 → `LdrpCurrentDllInitializer` 되돌림 (`+0x220`) → `ProcessAttachCalled` → 로그 `LdrpLogDllState`(`+0x24b`) → 다음 모듈.

**2-9. 예외 처리 (요청서에 없던 것)**
- 이 함수의 unwind 범위 표 (`pefile` 로 읽음): `try/except` `[+0x10e, +0x1b0)` 필터 `$filt$1`, 착지점 `+0x1b0`; `try/finally` `[+0x157, +0x1a4)` 마무리 `$fin$0`.
- 필터 `$filt$1` (`LdrpInitializeNode$filt$1 +0x2e`) 은 `RtlReportException` 으로 보고한 뒤 **늘 1 (실행하라)** 을 돌려준다 (`+0x33 MOV EAX,1`).
- 착지점 `+0x1b0` (`.asm` 의 "읽지 않은 틈", 스크립트 문제 절): `[RSP+0x118] = 1`, `R12B = 0`, `LdrpLogInternal`(`+0x1fe`, 줄 `0x606`) 후 `+0x220` 으로 합류. **TLS 콜백이든 DllMain 이든 예외를 던지면 여기서 잡혀 "초기화 실패" (`0xc0000142`) 가 된다.** `ProcessAttachCalled` 는 안 켜진다.
- 이 try 범위에는 `RtlActivate…` 호출(`+0x151`)도 들어 있다.

**2-10. `+0x2fd LdrpApplyPatchImage` 조건 → `LoadReason`(`+0x10c`) == 9 (`LoadReasonPatchImage`) 일 때만.**
- `+0xa4 CMP [RDI+0x10c],9; +0xab JZ +0x2fa`. 모듈 하나를 켜기 **시작할 때 맨 먼저** 부른다 (로그·활성화·TLS 보다 앞). 양수면 `+0x30e JNS +0xb1` 로 정상 길을 이어간다 (그 모듈도 TLS·진입점을 부른다). 음수면 `LdrpLogInternal`(`+0x343`, 줄 `0x5cb`) 하고 `+0x348 JMP +0x875` 로 끝 — 상태 코드는 Apply 가 돌려준 값 그대로, State = -4, `ProcessAttachFailed` 는 **안 켠다.**
- 이름·조건만 봤다 (본문은 범위 밖).

**로그 호출 개수** (요청서 "개수만"): `LdrpLogInternal` 4곳 (`+0xf9`, `+0x1fe`(틈), `+0x293`, `+0x343`), `LdrpLogDllState` 1곳 (`+0x24b`). 켜기 길(정상) 에서는 `+0xf9` 와 `+0x24b` 만 지난다.

### 질문 3. exe 노드는? → **건너뛴다. 코드에 있다.**

- `Node +0x9b CMP RDI,RCX` (`RDI` = 지금 항목, `RCX` = 전역 **`LdrpImageEntry`** 값, `+0x2d` 에서 읽음) → `+0x9e JZ +0x25e` → 항목을 통째로 건너뛰고 (`+0x25e MOV RSI,[RSI+8]` → `+0x262 JMP +0x7e`) 다음 모듈. 고리 A 에서도 같은 비교로 건너뜀 (`+0x4b`).
  - 비교는 **항목 포인터 == `LdrpImageEntry`** 이다. `ImageDll` 비트가 아니다.
- 건너뛰면 exe 에는 로그·활성화·TLS·진입점·`LdrpCurrentDllInitializer`·`ProcessAttachCalled`·`LdrpLogDllState` 가 다 없다. 그래서 exe 노드의 `Node` 는 **State 8 → (아무 일 없이) → 9** 를 쓰고 0 을 돌려줄 뿐이다.
- 그 결과 exe TLS 는 따로 `LdrpInitializeProcess +0x1c31` 쪽 (sast/14, `TlsIndex ≠ 0` 일 때), exe 진입점은 이 로더가 안 부르고 뒤에서 `NtContinue` 로 넘어간다 (flow.md 2.9~2.10).
- 이 `Node(exe 노드)` 는 맨 바깥 GR 프레임의 `+0x65 → ret +0x6a` 에서 불린다 (지도 `LdrpInitializeProcess +0x1bbb` 바로 아래, GR 프레임 1개). flow.md 그림 2 에는 이 상자가 없다 (아래 판정표).

### 질문 4. 관찰과 맞추기

**4-1. run7 kernel32 길 → "자식 먼저" 로 설명된다.**
- `Prepare +0xea → GR(KERNEL32 노드)`: 의존 목록에서 KERNELBASE 가 State 7 이라 `+0x91` 재귀 → GR(KERNELBASE 노드) 안에서 `+0x65 → ret +0x6a Node(KERNELBASE)`; 돌아와 고리를 끝내고 **맨 바깥 프레임의 `+0x65 → ret +0x6a Node(KERNEL32)`**. 관찰 "KERNELBASE = `+0x96` 재귀 안의 `+0x6a`, KERNEL32 = 재귀 없이 `+0x6a`" 와 같은 모양이다.
- ntdll 은 그 고리에서 프레임을 안 만든다 (관찰에 ntdll 의 Node 가 없음). ntdll 노드 State 가 7 이 아니어서(9 로 추정) 무시됐다는 뜻 `[추측]` (`LdrpInitializeNtdllDataTableEntry` 본문은 안 읽음).
- 어느 노드가 KERNELBASE·KERNEL32 인지는 코드로 못 정한다 → 동적 (Q24).

**4-2. run7·8 exe 길 → "재귀 한 단계 = 모듈 하나" 는 코드 모양으로 맞다. 정확히는 "한 단계 = 아직 State 7 인 노드 하나".**
- 프레임 하나가 만드는 일: 부모의 `Dependencies` 에서 `State == 7` 인 자식 하나에 대해 `+0x91` 이 부른 GR. 노드 하나는 보통 모듈 하나 (고리 합침이 없으면, sast/19).
- **이미 켜진 노드 (9) 나 지금 켜는 중 (8) 인 노드는 프레임을 차지하지 않는다.** `+0x47` 갈림에서 걸러진다. ntdll·kernel32 처럼 이미 9 인 모듈이 test.exe 의 의존 목록에 있어도 프레임은 늘지 않는다 (exe 길 관찰의 단계 수가 dll1 = 2, dll2 = 3 인 것과 맞음).
- 그림 `exe → dll1 → dll2` 는 `[소스]` (dll1 이 `Dll2Hello` import) 와 관찰 깊이 (GR 3겹 → dll2, 2겹 → dll1) 가 이 모양과 같다. 코드만으로 "프레임 2 = dll1" 을 못 박지는 못한다 (그건 런타임 의존 목록 값) → Q24.

**4-3. 실행 순서 dll2 → dll1 → exe 는 코드로 나온다.**
- 후위 순서 (1-2): GR(exe) → `+0x91` GR(dll1) → `+0x91` GR(dll2) → `+0x65` **Node(dll2)** → 돌아와 GR(dll1) 의 `+0x65` **Node(dll1)** → 돌아와 GR(exe) 의 `+0x65` **Node(exe 노드)** (아무 일 없음, 질문 3). 관찰 "dll2 → dll1 → (exe TLS 는 별도)" 와 같다. run7·8 표에 exe 노드의 `Node` 가 안 나오는 것은, 멈춤점이 `Node +0x16b`·`+0x19c` 쪽이었다면 맞는 모양이다 (exe 를 건너뛰니 거기까지 안 간다. 그때 멈춤점이 정확히 어디였는지는 이번에 안 봤다).
- 한 가지 예측: **`LdrpInitializeNode` 입구에 서면 exe 시동 중 3번** (GR 프레임 3겹 → dll2, 2겹 → dll1, 1겹 → exe 노드) 설 것이고, `+0x16b`·`+0x19c` 는 2번이다. 다만 DLL 들이 안에서 부르는 `LoadLibraryExW` (run7 "3번") 로 중첩 로드가 생기면 그 안에서 GR·Node 가 더 불릴 수 있다 (스택에 `LdrpCallInitRoutine` 이 있으면 중첩).

### 질문 5. 동적 Q24 를 위한 좁히기

GR 입구·Node 입구 둘 다 **`rcx` = 노드** (`_LDR_DDAG_NODE*`). 모듈 이름까지 가는 사슬:
- 첫 모듈 항목 = `poi(@rcx) - 0xa0` (`Modules.Flink` 는 `NodeModuleLink`(`항목+0xa0`)를 가리킴)
- 이름: `dt ntdll!_LDR_DATA_TABLE_ENTRY (poi(@rcx)-0xa0) BaseDllName DllBase` 또는 `du poi(poi(@rcx)-0xa0+0x60)` (`BaseDllName` 은 `항목+0x58`, 글자 주소 `+0x60`)
- 노드 값: `dd @rcx+0x38 L1` (State), `dq @rcx+0x28 L1` (`Dependencies.Tail`)
- 합쳐진 노드면 `Modules` 가 여러 항목이다. 이 사슬은 첫 항목만 보여 준다 (test.exe·dll1·dll2 는 하나라 충분).
- 지금 켜는 모듈을 바로 읽는 길: 전역 `ntdll!LdrpCurrentDllInitializer` (Node 가 `+0xbd` 에서 항목 포인터를 넣고 `+0x220` 에서 되돌림) — `LdrpCallInitRoutine` 안에서 멈췄을 때 `dt ntdll!_LDR_DATA_TABLE_ENTRY poi(ntdll!LdrpCurrentDllInitializer) BaseDllName`.
- 재귀 단계 수는 `k` 로 GR 프레임을 센다.

## 이어진 트리 (`LdrpInitializeProcess +0x1bb6` → `LdrpCallInitRoutine`)

`[정적]`. 괄호 안은 쓰는 State, `{}` 는 갈림. 노드 하나가 모듈 하나인 보통 경우.

```
LdrpInitializeProcess  +0x1bb6 (ret +0x1bbb)       RCX=exe 노드, RDX=상태 칸, R8=&바이트 0, 로더 락 안, +0x1b83 에서 CONTEXT 포인터를 전역에 넣어 둠
└ GR(exe 노드)                                      {노드 State == -4 면 0xc0000142 return}
   ├ Dependencies 한 바퀴 (먼저 들어온 간선부터, 간선 +0x18 비트 0 켜진 칸은 건너뜀)
   │    자식 State {7: 재귀 / 8: 깃발 켬 / -4: 실패 return / 그 밖: 무시}
   │    └ +0x91 (ret +0x96) GR(dll1 노드)  ← 자식 = State 7        [dll1 추정 — 값은 동적]
   │         ├ Dependencies 한 바퀴
   │         │    └ +0x91 (ret +0x96) GR(dll2 노드)                 [dll2 추정]
   │         │         ├ (의존 모두 9 라 재귀 없음)
   │         │         └ +0x65 (ret +0x6a) Node(dll2 노드)
   │         └ +0x65 (ret +0x6a) Node(dll1 노드)
   ├ {깃발 켜졌고 LoadContext 있고 RDX ≠ [LoadContext+0x28] → +0xcf 켜지 않고 return 0 [test.exe 에선 안 탐 추측]}
   └ +0x65 (ret +0x6a) Node(exe 노드)       ← 끝에서 한 번 더, 이쪽은 모듈이 exe 뿐이라 아무 일 없음
       GR 가 음수를 받으면 +0x6e State=-4 후 return (형제 안 봄)

Node(노드)
 ├ +0x1f  State = 8
 ├ 고리 A: 목록 꼬리→머리, exe 빼고 InInitializationOrderLinks 를 PebLdr+0x30 목록 꼬리에 끼움
 ├ 고리 B: 목록 꼬리→머리, 모듈마다
 │   ├ {항목 == LdrpImageEntry → 통째로 건너뜀 (+0x9b/+0x9e)}
 │   ├ {LoadReason == 9 PatchImage → +0x2fd LdrpApplyPatchImage (음수면 State -4 로 끝, 정상이면 계속)}
 │   ├ +0xbd LdrpCurrentDllInitializer = 항목, +0xf9 로그
 │   ├ +0x151 RtlActivateActivationContextUnsafeFast(틀, 항목+0x88)        ┐ try/except [+0x10e,+0x1b0)
 │   ├ {TlsIndex ≠ 0 → +0x166 (ret +0x16b) LdrpCallTlsInitializers(1, 항목)} │ 예외 → 착지 +0x1b0 → 실패
 │   ├ {EntryPoint ≠ 0 → +0x197 (ret +0x19c) LdrpCallInitRoutine(          │
 │   │       EntryPoint, DllBase, 1, {ProcessStaticImport ? CONTEXT 포인터 : 0})  결과 AL
 │   ├ +0x1a9 RtlDeactivate…                                                ┘
 │   ├ +0x220 LdrpCurrentDllInitializer 되돌림, {예외 없으면 Flags |= ProcessAttachCalled}
 │   └ {AL == 0 또는 예외 → 로그, R14=0xc0000142, Flags |= ProcessAttachFailed, 고리 끝} / 아니면 +0x24b LdrpLogDllState, 다음 모듈
 └ +0x2ba  State = {R14==0 ? 9 : -4},  return R14
```

## 동적분석으로 확인해야 할 것

대기 중인 번호에 묶을 수 있는 것은 새 번호를 안 만든다 (오케스트레이터가 정함).

1. **Q24 (재귀 단계 ↔ 모듈)** — 이번에 좁혀짐.
   - 멈춤점 (`ntdll+rva`): **GR 입구 `ntdll+0x87d60`**, **Node 입구 `ntdll+0xe5d0`**. 두 곳 다 `rcx` = 노드 → 위 질문 5 의 사슬로 이름을 찍는다. 같은 줄에 `k` 로 GR 프레임 수.
   - 예측 (코드 모양 + 소스): GR 입구 순서 exe → dll1 → dll2 (프레임 1, 2, 3), Node 입구 순서 dll2(GR 3겹) → dll1(2겹) → **exe 노드(1겹)**. 마지막 exe 노드 Node 는 run7·8 표에 없다 (멈춤점이 `+0x16b`·`+0x19c` 쪽이었다면 안 걸리는 게 정상). 맞는지가 확인거리.
   - kernel32 길 (`Prepare +0xef` 아래) 은 같은 두 멈춤점으로 KERNELBASE(2겹) → KERNEL32(1겹) 예측.
2. **Q24 (갈림 확인) — 지연 갈래 안 타나**: `ntdll+0x87e2f` (`XOR EAX,EAX`, 켜지 않고 return 0). test.exe 시동에서는 **안 설 것** `[추측]`. 서면 DllMain 안 중첩 로드 때문이다. 같이 `ntdll+0x87e12` (자식 -4), `ntdll+0x87dce` (`State = -4` 쓰기) 도 안 설 것.
3. **Q27 (`DllMain` 의 `lpReserved`)**: `ntdll+0xe767` (`LdrpCallInitRoutine` 부르기 직전) 에서 `r9` = `poi(ntdll!LdrpProcessInitContextRecord)` 인지, 그리고 dll1·dll2 의 `DllMain` 입구 `r8` 이 같은 값인지. exe 의 CONTEXT 이므로 `.cxr`/`dt ntdll!_CONTEXT` 로 읽히는 값인지도 한 번.
4. **Q25·Q27 (`LdrpCallInitRoutine` 인자)**: TLS 쪽 `ntdll+0xe736` 에서 `ecx=1`, `rdx` = 항목 (`dt ntdll!_LDR_DATA_TABLE_ENTRY @rdx BaseDllName`). 진입점 쪽 `ntdll+0xe767` 에서 `rcx` = 진입점, `rdx` = `DllBase`, `r8` = 1. 반환 `ntdll+0xe76c` 에서 `al`.
5. **State 쓰기 확인 (선택)**: `ntdll+0xe5ef` (`State = 8` 직전), `ntdll+0xe88a` (끝 쓰기, `ecx` = 9 또는 -4 — test.exe 에선 9 예측).
6. **초기화 순서 목록 (선택)**: `main` 에 닿은 뒤 `dt ntdll!_PEB_LDR_DATA` 의 `InInitializationOrderModuleList` 를 따라가면 ntdll, KERNELBASE, KERNEL32, dll2, dll1 순서일 것 (exe 없음) `[정적 추론]`. 계속 이어가는 일이라 새 번호보다 Q24 에 붙이는 걸 추천.
7. **정적으로 못 정한 것**: 간선 칸 `+0x18` 비트 0 의 정확한 뜻 (어느 간선이 대비용으로 할당되나), `LoadContext+0x28` 이 같은 로드 작업을 뜻하는지의 실제 값, `LoadContext` 가 이 시점에 살아 있는지. 코드로는 `[추측]`.

dast 요청 필요: 위 1·2·3 (Q24·Q27 에 흡수 가능), 4 (Q25·Q27).

## flow.md 그림 2 의 `[… 추정]` 상자 — 이번 결과로 바뀌는 것

| 상자 | 지금 글 | 이번 근거 | 판정 |
|---|---|---|---|
| `g1` GR `[test.exe 추정]` | 첫 인자 = exe 항목 `DdagNode` | 맨 바깥 프레임 = 이 노드. 자식 끝낸 뒤 `+0x65/+0x6a` 로 **Node(exe 노드)** 를 부름 (그림에 간선 없음). 노드 상자 이름 자체는 PDB 와 `+0x1baf` 로 확정 | **맞음 (정적).** `추정` 은 빼도 됨. 값은 Q46 |
| `g2` `[dll1 추정]`, `g3` `[dll2 추정]` | 관찰 깊이 + 소스 | 프레임 하나 = 부모 `Dependencies` 의 State 7 자식 노드 하나 (`+0x91`). 그래서 "한 단계 = 노드 하나" 는 코드로 맞음 | **구조는 맞음.** "어느 노드가 dll1/dll2" 는 값을 봐야 하니 **여전히 동적 (Q24)** |
| `n1`·`n2` `[dll1]`·`[dll2]` | 관찰 | 후위 순서로 dll2 → dll1 이 코드에서 나옴 | **맞음 (순서)** |
| `k6`·`k7`·`k8`·`k11` (`[KERNELBASE 추정]`, `[KERNEL32 추정]`) | 관찰 | "자식 먼저" 로 KERNELBASE → KERNEL32 설명됨, 프레임 모양도 같음 | **구조는 맞음.** 이름은 **여전히 동적 (Q24)** |
| `k9` `(TLS 콜백 호출 없음)` | 관찰 | 조건이 `TlsIndex ≠ 0` 뿐 → "부르지 않음" 과 "부르되 콜백이 없음" 이 코드에서 갈리는 지점은 `LdrpCallTlsInitializers` 본문 | **여전히 동적** (Q16 본문을 읽고 판단) |
| `n2d`·`n1d`, `k10`·`k12` `LdrpCallInitRoutine` | 관찰 `rcx`=`_DllMainCRTStartup` | 조건 `EntryPoint ≠ 0`, 인자 (진입점, DllBase, 1, CONTEXT 또는 0) | **인자까지 정적으로 정해짐** (다음 호출 대상은 여전히 `?`) |
| 그림에 **없는** 것 | | Node(exe 노드), 고리 A (초기화 순서 목록 끼우기), State 8·9 쓰기, 예외로 잡는 try/except | 새 상자 후보 (아래) |

## 그림 2 `(정적)` 상자 후보 (2 ~ 4개)

1. **`GR 의 자식 한 바퀴`** (`g1` 아래): "Dependencies 목록에서 State 7 인 자식만 재귀(`+0x91`), 8 이면 깃발, -4 면 실패, 9 등은 무시. 자식 다 끝난 뒤 `+0x65` 로 자기 노드." — "재귀 한 단계 = 노드 하나" 와 "남한테 기대는 모듈일수록 나중에" `[추측]` 문장의 코드 근거.
2. **`ntdll!LdrpInitializeNode [exe 노드]` (정적: 항목이 `LdrpImageEntry` 면 통째로 건너뜀, State 8 → 9 만)** (`g1` 의 `+0x6a`): exe 만 TLS·진입점이 `Node` 가 아니라 `LdrpInitializeProcess +0x1c31` 쪽에서 도는 까닭.
3. **`LdrpInitializeNode` 안 한 모듈 순서 상자**: "초기화 순서 목록에 끼움(고리 A) → 활성화 문맥 켜기 → TLS(`TlsIndex ≠ 0`) → 진입점(`EntryPoint ≠ 0`, `r9` = CONTEXT 포인터 또는 0) → 끄기 → `ProcessAttachCalled`. try/except 로 감싸서 예외나 FALSE 면 `0xc0000142`, State -4." — `n1`·`n2`·`k8` 의 조건 근거. 한 상자로 묶어도 됨.
4. **`GR 지연 갈래`** (선택, 작게): "지금 초기화 중(8) 인 의존이 있고 로드 문맥이 다르면 켜지 않고 return 0 (`+0xcf`)." test.exe 시동에서는 안 타는 쪽이라 점선 상자로.

## Q16 요청서에 미리 넘길 것 (`LdrpCallTlsInitializers`·`LdrpCallInitRoutine`·`ImageTlsCallbackCaller`)

- `LdrpCallTlsInitializers` 로 들어오는 인자: `ECX` = 1 (`DLL_PROCESS_ATTACH`), `RDX` = **`_LDR_DATA_TABLE_ENTRY*`** (DllBase 가 아니다). 반환값은 `LdrpInitializeNode` 가 **버린다.** 그래서 이 함수가 실패를 알리는 길은 예외뿐이다.
- 부르는 조건은 항목 `TlsIndex`(`+0x6e`) ≠ 0 뿐. 그러니 `TlsIndex` 를 누가 쓰는지 (`LdrpHandleTlsData`/`LdrpInitializeTls` 쪽 `[추측]`) 와 "TLS 디렉터리가 있는 DLL 의 `TlsIndex` 는 0 이 아닌가" 를 같이 봐야 한다.
- `LdrpCallInitRoutine` 인자 (진입점 쪽 호출): `RCX` = 부를 함수, `RDX` = `DllBase`, `R8D` = 이유(1), `R9` = `lpReserved` 자리. **TLS 쪽 호출**(`LdrpCallTlsInitializers +0x19b`) 은 `RCX` = `ImageTlsCallbackCaller` 라는 관찰이 있으니, 같은 네 칸에 무엇을 넣는지 (특히 `RDX`·`R9`) 를 `LdrpCallTlsInitializers` 본문에서 읽을 것.
- 반환은 `AL` (BOOLEAN) 만 쓴다 (`MOVZX R12D,AL`). `LdrpCallInitRoutine` 이 DllMain 의 BOOL 을 어떻게 `AL` 로 돌려주는지, 그리고 `r8` → DllMain 의 `rdx` 로 한 칸씩 미는지 확인.
- 두 호출 다 **`RtlActivateActivationContextUnsafeFast` 틀 안 + `__except`(필터 늘 1) 안 + `LdrpCurrentDllInitializer` = 그 항목** 인 채로 불린다. 안쪽에서 예외가 나면 `LdrpInitializeNode` 가 잡아 `0xc0000142` 로 만든다.
- `ImageTlsCallbackCaller` 는 지도에 호출 참조가 없다 (sast/05). `LdrpCallTlsInitializers +0x18f` 에서 주소만 꺼낸다는 기존 메모 (plan Q16) 가 그대로 출발점.

## 판정

**분석성공.** 요청서 질문 1~5 모두 `.asm` 의 `+0x..` 줄 (call·ret 둘 다) 로 답했고, 쓴 오프셋 약 90개를 `.asm` 과 다시 맞췄다. 버전 확인 일치. `[추측]` 으로 남긴 것: 간선 칸 비트 0 의 이름, `RDX` = "같은 로드 작업 식별" 이라는 뜻, 지연 갈래의 목적, ntdll 노드 State. test.exe 시동에서 지연 갈래·`Node(exe 노드)` 가 실제로 어떻게 되는지, 프레임이 어느 모듈인지는 동적으로 넘겼다. 도구 쪽 한계 하나 (`_calls.tsv` 에서 예외 착지점 호출 1곳 빠짐) 는 위 "스크립트 문제" 절.

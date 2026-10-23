# 19 work-queue — 정적 분석 결과

- 요청서: `_workspace/sast/19_work-queue.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), PDB `ms/sym/ntdll.dll.pdb` (이미 분석됨: `.gpr` + `_index.tsv` + `SetPdb:` 줄 다 있음. analyze 안 다시 함)
- 뽑은 함수: 요청서 8개 (`^(LdrpQueueWork|LdrpWorkCallback|LdrpDrainWorkQueue|LdrpProcessWork|LdrpSignalModuleMapped|LdrpCondenseGraph|LdrpCondenseGraphRecurse|LdrpMergeNodes)$`, depth 1 → 29개) → `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`
  - 범위를 **조금 넓힌 것 (이유 있음, 전부 depth 0, 한 함수씩)**: `LdrpEnableParallelLoading`·`LdrpInitParallelLoadingSupport` (일꾼을 깨우는 조건의 반대쪽 = 일감 개체를 만드는 곳), `LdrpProcessMappedModule` (`LdrpSignalModuleMapped` 의 caller, 부르는 조건), `TppWorkpExecuteCallback` (`refs LdrpWorkCallback` 이 가리킨 곳), `LdrpCreateLoaderEvents` (이벤트 종류). `LdrpDetectDetour`·`LdrpDropLastInProgressCount` 는 이미 파일이 있었음.
  - `refs` 11개: `LdrpWorkQueue`(16), `LdrpWorkQueueLock`(40), `LdrpWorkInProgress`, `LdrpMapAndSnapWork`, `LdrpWorkCompleteEvent`, `LdrpLoadCompleteEvent`, `LdrpRetryQueue`(16), `LdrpDetourExist`, `LdrpWorkCallback`, `LdrpMainThreadToken`, `LdrpRetryingModuleIndex` → `ms/disassemble/ntdll/_refs/`
  - 구조체 칸은 PDB 로 확인: `llvm-pdbutil-14 dump -types ms/sym/ntdll.dll.pdb` (스크래치 파일, 커밋 안 함).
- 버전 확인: `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal`, `LdrpInitializeNode +0x166/+0x16b → LdrpCallTlsInitializers`, `+0x197/+0x19c → LdrpCallInitRoutine` 일치. (지금 `_calls.tsv` 가 `_LdrpInitialize +0x167/+0x16c` 로 나옴 — 스킬 표엔 `+0x16c` 만 있고 call 자리 `+0x167` 도 같은 줄이라 일치.) 불일치 없음.
- **오프셋 확인 (요청서가 따로 부탁함)**: 이 8개 함수 `.asm` 전부에 대해 스크립트로 직접 계산했다 (`-I` 모드 파이썬).
  - 모든 줄 `+0x…` = "절대 주소 − 함수 시작" 과 같다 (assert 통과).
  - 점프(`Jcc`/`JMP`) 128개의 목적지를 "절대 주소 − 함수 시작" 으로 바꿔서 **같은 함수 안 명령 시작 위치와 맞는지** 봤다: 127개 맞고, 안 맞는 1개는 `LdrpProcessWork +0x1c3` 의 `JMP NtSetEvent` (꼬리호출, 밖으로 나가는 것이라 당연).
  - 호출 36개의 `ret` 는 전부 **바로 다음 명령의 오프셋** 과 같다.
  - 아래 글의 점프 목적지는 이 표에서 옮겼다. 라벨은 call 과 ret 둘 다 적고, 콜스택과 비교할 땐 ret.

## 스크립트 문제

- 없음. `LdrpWorkCallback` 이 지도에 없는 건 **이미 알려진 한계** 다 (reference.md "읽을 때 주의": 함수 주소를 값으로 넘겨 나중에 부르는 길은 지도에 없다). 대신 `refs LdrpWorkCallback` 이 4곳을 찾아 줬다 (질문 2).
- 참고: `_calls.tsv` 의 `LdrpDrainWorkQueue` caller 줄은 16곳 중 지금 뽑아 둔 함수(LdrGetProcedureAddressForCaller, LdrpFastpthReloadedDll, LdrpInitializeProcess ×4, LdrpLoadDllInternal ×3)뿐이다. 나머지 caller 5곳(`LdrpFindLoadedDll`, `LdrpInitializeImportRedirection`, `LdrpInitializeThread`, `LdrUnloadDll`, `RtlQueryInformationActivationContext`)은 `.asm` 이 없어서 Drain 인자를 못 봤다.

## 한눈에 (먼저)

- 일감 = **로드 문맥(`ctx`)**. 목록 칸은 `ctx+0x40` (LIST_ENTRY). 큐 머리 = `LdrpWorkQueue` (`ntdll+0x1d26b0`), 잠금 = `LdrpWorkQueueLock` (`+0x1d2680`, 임계 구역).
- **일감 한 개 = 두 종류**: `ProcessWork` 가 `ctx→항목→DdagNode.State` 를 보고 `0` (PlaceHolder) 이면 **올리기**, `0` 이 아니면 **`LdrpSnapModule`**. (그래서 `LdrpSignalModuleMapped`/`MapAndSnap` 가 부모 `State=4` 로 바꾸고 부모 문맥을 다시 큐에 넣으면 그게 "부모 snap 일감" 이 된다.)
- 꺼내는 사람은 둘: (a) **지금 로드를 책임지는 스레드(`LoadOwner`)** 가 `LdrpDrainWorkQueue` 안에서 직접, (b) **풀 일꾼** (`TpPostWork` 로 깨움). (b) 는 `LdrpMapAndSnapWork` 가 0 이 아닐 때만 생긴다.
- `LdrpWorkCallback` 은 **일꾼용 반복문** 이다. 그런데 일꾼 쪽 `TppWorkpExecuteCallback` 이 "콜백 == `LdrpWorkCallback`" 이면 **같은 코드를 자기 안에 복사해 둔 길** 로 가서 `LdrpProcessWork` 를 직접 부른다. 그래서 `LdrpWorkCallback` 함수 자체는 실행 때 안 불릴 수도 있다 `[추측]`.
- 부모 카운터(`ctx+0x6c`)를 줄이고 snap 을 잇는 코드는 **`LdrpSignalModuleMapped +0x40~+0x59`** 가 맞다 (자식이 Mapped 가 된 순간 부모들에게).

## 질문별 답 [정적]

### 1. `LdrpQueueWork` — 어디에 넣나, 잠금, `TpPostWork` 조건

- 근거: `ms/disassemble/ntdll/LdrpQueueWork.asm` (`rva 0xe244c`, 136바이트). `.c` 대신 `.asm` 로 읽음.
- 답:

| call / ret | 줄 | 하는 일 |
|---|---|---|
| (call 없음) `+0x6~+0x10` | `RAX=[ctx+0x28]; CMP [RAX],0; JL +0x82` (+0x82 = 함수 끝 `ADD RSP`) | **상태가 이미 음수면 아무것도 안 하고 끝.** (실패한 문맥은 큐에 안 넣음) |
| `+0x19` / `+0x1e` | `RtlEnterCriticalSection(&LdrpWorkQueueLock)` (`ntdll+0x1d2680`) | 잠금 |
| (인라인) `+0x1e~+0x4d` | `ctx+0x40` 을 `LdrpWorkQueue`(`+0x1d26b0`) 목록 **꼬리(Blink, `+0x1d26b8`)** 에 끼움 | 끝에 붙임 (순서대로 처리). 꼬리 `Flink` 가 머리가 아니면 `INT 0x29` (`+0x3a`, 빠른 실패) |
| `+0x54` / `+0x59` | `RtlLeaveCriticalSection` | 잠금 풂 |
| `+0x59~+0x76` | 아래 세 조건 | 깨울지 판단 |
| `+0x7d` / `+0x82` | `TpPostWork(LdrpMapAndSnapWork)` | 일꾼 깨우기 |

- **목록 전역 이름**: `LdrpWorkQueue` (`ntdll+0x1d26b0`, 머리. Flink `+0`, Blink `+8`). 기본 상태 "빈 목록(자기를 가리킴)" 은 `LdrpInitParallelLoadingSupport +0x0~+0x33` 이 만든다 (`+0x3a` 에 잠금 `RtlInitializeCriticalSectionEx`, 끝 `+0x43` 에서 `LdrpCreateLoaderEvents` 로 꼬리호출). 이 함수의 caller = `LdrpInitializeProcess +0x12de` (ret `+0x12e3`), `Drain(0)` 첫 번째(`+0x1302`) 바로 앞.
- **잠금 이름**: `LdrpWorkQueueLock` (`ntdll+0x1d2680`, 크기 0x28 = `RTL_CRITICAL_SECTION`).
- **`TpPostWork` 를 부르는 조건 (세 개 다 참일 때)** `[정적]`:
  1. `LdrpMapAndSnapWork` (`ntdll+0x1d2640`, 일꾼 개체 포인터) **≠ 0** (`+0x59`, 아니면 `JZ +0x82`).
  2. `PEB→Ldr(+0x18)→ShutdownInProgress(+0x48)` 바이트가 0 (`+0x63~+0x70`, `GS:[0x60]` = PEB; PDB `_PEB_LDR_DATA +0x48 ShutdownInProgress` 확인).
  - "병렬 로딩이 켜졌나" 를 읽는 **별도 플래그는 없다.** `LdrpMapAndSnapWork` 가 0 이냐 아니냐가 곧 그 판단이다.
- **`LdrpMapAndSnapWork` 는 누가 정하나** (`refs`):
  - 만든다: `LdrpEnableParallelLoading +0xa9` 가 이 주소를 `TpAllocWork` 의 출력 자리로 넘긴다 (call `+0xdd` / ret `+0xe2`). 콜백 인자 = `+0x9d LEA RDX,[0x1800e9ef0]` = **`LdrpWorkCallback`** (`refs` 로 확인).
  - 그 앞 조건 (`LdrpEnableParallelLoading`): 인자(`ProcessParameters.LoaderThreads`, run19) `ECX` → `EBX` = 스레드 수 (`+0x23~+0x35`: 인자가 0 이면 `인자+4`, 스위트 마스크 `RtlGetSuiteMask` 비트 16 이 켜져 있으면 인자 그대로(=0), 인자가 0 이 아니면 `min(인자, 0x10)`, `+0xf9~+0x102`). 풀은 항상 만든다(`+0x44 TpAllocPoolInternal`, 최대 스레드 `EBX-1` 또는 1, `+0x7a TpSetPoolMaxThreads`). **`EBX ≤ 1` (`+0x7f JBE +0xe4`) 이거나 `LdrpDetourExist ≠ 0` (`+0x84`) 이면 `TpAllocWork` 를 안 부른다 → `LdrpMapAndSnapWork` 는 0 으로 남는다.**
  - 지운다: `LdrpDetectDetour +0xb3~+0xd5` (`LdrpDetourExist` 가 켜지면 `TpWaitForWork`+`TpReleaseWork` 후 0 으로), `LdrpCompleteProcessCloning +0x78`.
- **답 요약**: 병렬 로딩 켜기 **전** (kernel32 준비 때) 에는 `LdrpMapAndSnapWork == 0` 이므로 `TpPostWork` 를 안 부른다. 켠 **뒤**에도 `LoaderThreads`/스위트 값이 `EBX ≤ 1` 로 만들면 0 이다. 실제 값은 **동적** (아래 Q47 곁).

### 2. `LdrpWorkCallback` — 일꾼

- 근거: `ms/disassemble/ntdll/LdrpWorkCallback.asm` (`rva 0xe9ef0`, 218바이트), `_refs/LdrpWorkCallback.tsv`.
- **`refs` 결과** (4곳): `LdrpEnableParallelLoading +0x9d` (주소를 `TpAllocWork` 인자로 넘김), `TppWorkpExecuteCallback +0x19d` (주소를 비교용으로 읽음), 함수 밖 2곳 (`rva 0x1def24`, `GuardCFFunctionTable+0xd2a` = CFG 허용 목록으로 보임 `[추측]`). **→ 주소로 넘겨지는 것이 맞다.**
- 본문 순서 (call / ret, 반복문 한 바퀴):

| 위치 | 하는 일 |
|---|---|
| `+0x23` | `LdrpDetourExist ≠ 0` 이면 `JNZ +0x7a` (끝으로 감: 아무것도 안 함) |
| `+0x33` / `+0x38` | 잠금 (`LdrpWorkQueueLock`) |
| `+0x38~+0x55` | `LdrpWorkQueue` **머리 쪽 한 개**를 뺀다 (`RBX=[LdrpWorkQueue]`, 이웃 검사 실패면 `+0xbd` `INT 0x29`) |
| `+0x59~+0x69` | 뺀 게 머리 자신이 아니면(= 일감이 있었으면) `LdrpWorkInProgress += 1` (`+0x5e`) 하고 `+0x64` / `+0x69` `LdrpUpdateStatistics` |
| `+0x70` / `+0x75` | 잠금 풂 |
| `+0x75~+0x78` | 일감 없으면 (`RSI==RBX`) `+0x7a` 로: 가장했던 토큰이 있으면(`DIL≠0`) `+0x81` / `+0x86` `LdrpThreadTokenSetImpersonationToken(0)` 로 되돌리고 **반환**(`+0x9a`) |
| `+0x9c~+0xd9` | 일감이 있고 아직 토큰을 안 바꿨으면 (`DIL==0`) `LdrpMainThreadToken`(`+0x1d2618`) 이 0 이 아닐 때 `+0xcb` / `+0xd0` `LdrpThreadTokenSetImpersonationToken(메인 토큰)` 으로 **메인 스레드 토큰을 가장**하고, 성공하면(`CMOVNS`) `DIL=1` |
| `+0xb3` / `+0xb8` | **`LdrpProcessWork(일감 - 0x40, 0)`** — 인자 2 = 0 (끝에서 `WorkInProgress` 를 줄이고 신호할 것) |
| `+0xb8` JMP → `+0x23` | 다시 맨 앞(`LdrpDetourExist` 검사)으로 |

- **몇 개씩**: 한 번에 하나씩 꺼내 처리하고 맨 앞으로 돌아가는 반복이다. **큐가 빌 때까지** 돈다 (고정 개수 없음).
- **언제 끝나나**: (a) 큐가 비었을 때, (b) `LdrpDetourExist` 가 켜졌을 때. 이 함수는 기다림(`NtWait*`)이 없다.
- 이 함수의 `LdrpProcessWork` 호출은 `Drain` 의 것과 달리 인자 2 가 항상 `0` 이다.
- **실제 일꾼은 이 함수를 안 거칠 수 있다** `[정적: 코드가 그렇게 쓰여 있다]`: `TppWorkpExecuteCallback +0x1b4 CMP RAX,RCX` (`RAX` = 일감 개체의 콜백 칸 `[개체+0x50]`, `RCX = LdrpWorkCallback` 주소) 이 같으면 `+0x1bd~+0x2db` 로 가서 **같은 반복문의 복사본** (`LdrpDetourExist` 확인, 잠금, 목록에서 빼기, `LdrpWorkInProgress` 증가, `LdrpUpdateStatistics`, 토큰 가장)을 돌고 **`+0x2db` / `+0x2e0` 에서 `LdrpProcessWork` 를 직접 부른다.** 달라야 간접 호출(`+0x4cb`, `indirect:cfg`)로 간다. → 풀 일꾼의 콜스택에는 `LdrpWorkCallback` 이 안 보이고 `TppWorkpExecuteCallback+0x2e0` 가 보일 것이다 `[추측: 확인은 동적]`.

### 3. `LdrpDrainWorkQueue` — 인자 0/1, 직접 처리 vs 기다림

- 근거: `ms/disassemble/ntdll/LdrpDrainWorkQueue.asm` (`rva 0xad400`, 456바이트, 호출 8개 = 임계 구역 들어가기 ×2 / 나오기 ×3 / `LdrpUpdateStatistics` / `LdrpProcessWork` / `NtWaitForSingleObject`), `_refs/LdrpWorkInProgress.tsv`, `LdrpDropLastInProgressCount.asm`.
- 인자 `ECX` (`ESI`) 한 가지만 다르다 (`+0x2f TEST ECX,ECX`, `+0x3a CMOVZ R14, LdrpLoadCompleteEvent`):

| | `Drain(0)` | `Drain(1)` |
|---|---|---|
| 기다리는 이벤트 | `LdrpLoadCompleteEvent` (`ntdll+0x1d2620`) | `LdrpWorkCompleteEvent` (`ntdll+0x1d2660`) |
| 끝나는 조건 (큐가 빈 상태에서) | `LdrpWorkInProgress == 0` | `LdrpWorkInProgress == 1` **그리고** `LdrpRetryQueue` 가 비어 있음 |
| 끝나기 직전 | `WorkInProgress = 1` 로 올리고 (`+0xa3`) | 이미 1 (`+0xa3` 같은 줄이 다시 1 로 씀) |
| 끝낼 때 | **TEB `SameTebFlags(+0x17ee) |= 0x1000` (`LoadOwner`, PDB 확인)** (`+0xb6~+0xd3`) 후 반환 | 같은 줄 (`+0xeb` 에서 재시도 큐가 비었으면 `JZ +0xb6`) |
| 뜻 | "로드 책임자가 된다" | "책임자(나)가 큐를 다 비우고 일꾼도 다 끝날 때까지 돕는다" |

  - 호출 `NtWaitForSingleObject(R14, FALSE, NULL)` (`+0x191~+0x194`, ret `+0x199`): 시간제한 없이 이벤트가 켜질 때까지.
  - **이벤트는 둘 다 자동 리셋** (`LdrpCreateLoaderEvents +0x4/+0x12~+0x1e`, `NtCreateEvent(.., 0x1f0003, NULL, EventType=1(Synchronization), 초기 0)`).
  - 누가 이벤트를 켜나: `LdrpWorkCompleteEvent` ← `LdrpProcessWork` 끝 (`+0x1c3` 꼬리호출 `NtSetEvent`, 또는 예외 정리 `$fin$0 +0x83`); `LdrpLoadCompleteEvent` ← `LdrpDropLastInProgressCount +0x45` (꼬리호출 `NtSetEvent`). 이 함수는 `LoadOwner` 비트(0xefff 로 지움, `+0xd`)·`WorkInProgress=0` (`+0x25`) 도 한다 = 책임자 내려놓기.
- 반복문 (점프 목적지 환산 확인됨):
  - `+0x42` 맨 앞(잠금 `+0x49`/`+0x4e`). `+0x4e` 에서 `LdrpDetourExist` 를 읽음 (`DIL`).
  - 평소(`DIL==0`): `+0x5e~+0x83` 머리 한 개 빼기. 일감이 있으면 (`+0x8a JNZ +0x150`): `DIL==0` 이므로 `+0x155` `WorkInProgress++`, `+0x15b`/`+0x160` `LdrpUpdateStatistics`, `+0x160~+0x167` 잠금 풂 (call `+0x167` / ret `+0x16c`), `+0x17a` 에서 **`LdrpProcessWork(일감-0x40, DIL)`** (call `+0x182` / ret `+0x187`) 하고 `+0x187 JMP +0x42`. → **이 스레드가 직접 일감을 처리한다.**
  - 일감이 없으면 (`R15==RBX`): `+0x90 CMP WorkInProgress, ESI`. 같지 않으면(`JNZ +0x160`) 잠금 풀고 `+0x18c` 에서 `NtWaitForSingleObject` (call `+0x194` / ret `+0x199`), 이어서 `+0x42` 로. **→ 일꾼이 끝내길 기다린다.** 같으면 위 표의 "끝나는 조건".
  - `Drain(1)` 끝 직전 `+0xeb`: `LdrpRetryQueue` 가 비어 있지 않으면 `+0xf4` 잠금(call `+0xfb` / ret `+0x100`), `LdrpRetryQueue` 전체를 `LdrpWorkQueue` 로 옮기고 (`+0x100~+0x134`), `LdrpRetryingModuleIndex` 지움 (`+0x13b`), 잠금 풂 (call `+0x143` / ret `+0x148`), `+0x14b JMP +0x42`.
  - `LdrpDetourExist ≠ 0` (`+0x58 JNZ +0x19e`): `Drain(1)` 이면 평소 길(`+0x5e`)로 가되 `DIL=1` 이라 `WorkInProgress` 를 안 올리고 `ProcessWork` 의 인자 2 가 1 이 된다. `Drain(0)` 이면 **큐를 건드리지 않고** `WorkInProgress==0` 일 때만 1 로 만들어 `LoadOwner` 를 가지고 끝내고(`+0x1af~+0x1b9 BPL=1`→`+0xb2`), 아니면 이벤트를 기다린다.
- 갈리는 조건 (`+0x182` 직접 처리 vs `+0x194` 기다림) = **"큐에 일감이 있나" (`+0x87`/`+0x175`)**. 있으면 직접, 없고 `WorkInProgress != ESI` 이면 기다림.
- **잠금 이름**: `LdrpWorkQueueLock` (질문 1 과 같음).
- **run19/run21 caller 에서의 인자** (`.asm` 에서 `ECX` 를 직접 봄):

| caller | call / ret | 인자 | 비고 |
|---|---|---|---|
| `LdrpInitializeProcess` | `+0x1302` / `+0x1307` | 0 (`XOR ECX,ECX`) | `LdrpInitParallelLoadingSupport +0x12de` (ret `+0x12e3`) 바로 뒤. 책임자 되기 |
| 〃 | `+0x19c1` / `+0x19c6` | 0 | kernel32 준비 `+0x199d` 뒤, 병렬 로딩 켜기 `+0x1a42` 앞 |
| 〃 | `+0x1aac` / `+0x1ab1` | **1** (`MOV ECX,R12D`, `R12D=1`은 sast/16) | `+0x1aa4 LdrpMapAndSnapDependency` (exe import) 바로 뒤, 켜기 `+0x1a42` 뒤 |
| 〃 | `+0x1b8c` / `+0x1b91` | 0 | `LdrpProcessInitContextRecord` 저장 뒤 |
| `LdrpLoadDllInternal` | `+0x185` / `+0x18a` | 0 | **`+0x16d~+0x174` 에서 `LoadOwner` 비트가 이미 켜져 있으면 이 줄을 건너뜀** (`JNZ` → `+0x420` 쪽) = 안쪽 로드는 책임자 다시 안 됨 |
| 〃 | `+0x215` / `+0x21a` | **1** | `+0x20b` / `+0x210 LdrpProcessWork(ctx, 1)` 바로 뒤 |
| 〃 | `$fin$0 +0x38` / `+0x3d` | 1 | 예외 정리 (`$fin$0`) |
| `LdrpFastpthReloadedDll` | `+0x11a` / `+0x11f` | 0 (`LoadOwner` 가 이미 있으면 건너뜀 `+0x10a`/`+0x112`) | |
| `LdrGetProcedureAddressForCaller` | `+0x8d4` / `+0x8d9` | 0 | 바로 뒤 `+0x8de LdrpDropLastInProgressCount` |

  - 같은 짝: 책임자를 내려놓는 쪽 `LdrpDropLastInProgressCount` 가 `InitializeProcess` 에 `+0x1753`, `+0x1b40`, `+0x1be7` 로 3번, `LdrpLoadDllInternal +0x416`, `LdrpInitializeThread +0x3f2` 등에서 불린다 (지도 `_edges`). 즉 `InitializeProcess` 안에서 "책임자 기간" 이 **세 번 열리고 닫힌다**: `[+0x1302 ~ +0x1753]`, `[+0x19c1 ~ +0x1b40]` (여기에 켜기 `+0x1a42` 와 exe import `+0x1aa4`, `Drain(1)` `+0x1aac` 가 들어 있음), `[+0x1b8c ~ +0x1be7]` `[정적: 위치만. 사이에서 뭘 하는지는 안 읽음]`.
  - 못 읽은 caller 5곳 (`LdrpFindLoadedDll +0x327`, `LdrpInitializeImportRedirection +0x12e`, `LdrpInitializeThread +0xea`, `LdrUnloadDll +0x1e1`, `RtlQueryInformationActivationContext +0x4e3`) 의 인자는 확인 전.
- **kernel32 준비 때와 exe import 때가 다르게 도나**: 코드(`Drain`/`QueueWork`/`ProcessWork`)는 **똑같다**. 다른 건 `LdrpMapAndSnapWork` 가 0 이냐 아니냐 뿐이다 (질문 1).
  - 켜기 앞(`+0x19c1` 기간): `TpPostWork` 를 안 부르므로 큐에 쌓인 일감은 **Drain 하는 스레드(메인) 혼자** 꺼내서 처리한다 (`+0x182`).
  - 켠 뒤(`+0x1aac` 기간, `LoaderThreads` 조건이 맞으면): `QueueWork +0x7d` 가 일꾼을 깨운다. 메인은 `Drain(1)` 에서 (a) 큐에서 직접 꺼내거나 (b) 큐가 비었는데 `WorkInProgress != 1` (일꾼이 아직 일하는 중) 이면 `LdrpWorkCompleteEvent` 를 기다린다. 마지막 일꾼이 `ProcessWork` 끝에서 `WorkInProgress` 를 1 로 만들고 큐가 비었으면 `NtSetEvent` 한다 (질문 4).

### 4. `LdrpProcessWork` — 올리기 길, snap, 신호

- 근거: `ms/disassemble/ntdll/LdrpProcessWork.asm` (`rva 0x17140`, 465바이트).
- 입구 인자: `RCX = ctx`, `DL` = 두 번째 인자 → `EDI` (`+0x16`) 에 복사. 이 인자는 "**이 일감을 `WorkInProgress` 에 센 적 없다**" 로 읽힌다: `1` 이면 끝에서 `WorkInProgress` 를 줄이거나 신호하지 않고 그냥 반환(`+0x15d TEST DIL; JZ +0x171`), `0` 이면 줄이고 신호함 `[정적: 줄. 이름은 추측]`.
  - `LdrpLoadDllInternal +0x20b` 는 1 (자기 스레드가 책임자라서 센 적 없음), `LdrpWorkCallback +0xb3` 은 0, `Drain +0x182` 는 `DIL` (= `LdrpDetourExist`, 평소 0), `TppWorkpExecuteCallback +0x2db` 는 0.
- 순서 (call / ret, 점프 환산 확인):

| 위치 | 하는 일 |
|---|---|
| `+0x1c~+0x23` | `*(ctx+0x28)` (상태 포인터)가 **음수면** 아무것도 안 하고 `+0x15d` 로 (이미 실패한 문맥) |
| `+0x29~+0x38` | `E = ctx+0x38` 의 `DdagNode(+0x98)->State(+0x38)` 가 **0 이 아니면** `JNZ +0x129` → **`+0x12c` / `+0x131` `LdrpSnapModule(ctx)`** (올리지 않고 snap 만). 결과 `ESI`, 음수면 `*(ctx+0x28)=ESI` (`+0x133~+0x13b`) |
| `+0x3e~+0x48` | State == 0 이면: 항목 `LoadReason(+0x10c) == 9` (`LoadReasonPatchImage`, PDB) → `JZ +0x153` → **`+0x153` / `+0x158` `LdrpMapDllPatchImage`** |
| `+0x4e~+0x55` | `ctx+0x20` 비트 20 (0x100000) → `JC +0x149` → **`+0x149` / `+0x14e` `LdrpMapDllRetry`** |
| `+0x5b~+0x5f` | `ctx+0x20` 비트 9 (0x200) → `JC +0x13f` → **`+0x13f` / `+0x144` `LdrpMapDllFullPath`** |
| `+0x65` / `+0x6a` | 아니면 **`LdrpMapDllSearchPath(ctx)`** |
| `+0x6a~+0x7f` | 결과 `ESI` 가 0 이상이면 끝(`JNS +0x15d`), `0xc000022d` (`STATUS_RETRY`) 이면 끝 (재시도 큐 쪽) |
| `+0xc1` / `+0xc6` | 그 밖의 실패: `LdrpLogInternal` (로그) |
| `+0xce~+0x127` | 상태 `0xc0000135` (`STATUS_DLL_NOT_FOUND`) 일 때만: `+0xda`/`+0xdf LdrpLogError`, `+0xe2`/`+0xe7 LdrpLogDeprecatedDllEtwEvent`, `+0x10b`/`+0x110 LdrpLogLoadFailureEtwEvent`, 그리고 항목 `Flags(+0x68) & 0x20` (`ProcessStaticImport`, PDB) 이면 **`+0x122` / `+0x127 LdrpReportError(ctx,0,상태)`** |
| `+0x133~+0x13b` | 음수면 `*(ctx+0x28) = ESI` 로 상태를 넘김 |
| `+0x15d~+0x16f` | `DIL ≠ 0` 이면 반환 |
| `+0x171` / `+0x178` / `+0x17d` | `DIL == 0`: 잠금 (`LdrpWorkQueueLock`), `LdrpWorkInProgress--` (`+0x17d~+0x185`) |
| `+0x18b~+0x1d0` | **큐가 비었고 (`+0x192 CMP [LdrpWorkQueue], &LdrpWorkQueue; JZ +0x1c8`) 줄인 값이 1 이면** `BL=1` |
| `+0x19d` / `+0x1a9` | 잠금 풂 (call `+0x1a4` / ret `+0x1a9`) |
| `+0x1ad~+0x1c3` | `BL ≠ 0` 이면 `NtSetEvent(LdrpWorkCompleteEvent, 0)` — **꼬리호출** (`JMP`, call 아님, ret 없음) |

- **올리기 길 고르기**: 순서대로 (1) 항목이 이미 State≠0 → 올리지 않고 snap, (2) 패치 이미지, (3) 재시도 비트, (4) 전체 경로 비트, (5) 기본은 검색 경로. `LdrpSnapModule` 은 **올리는 길과 같은 호출에서 불리지 않는다.** 한 일감은 올리기 **또는** snap 이다 (State 가 0 이냐로 갈림). 올린 뒤 snap 은 다른 일감(질문 5, `LdrpMapAndSnapDependency` 끝맺음)으로 이어진다.
- **신호**: `NtSetEvent` 는 `LdrpWorkCompleteEvent` 하나 (`+0x1af`). 대상 = `Drain(1)` 에서 `NtWaitForSingleObject` 로 기다리는 스레드 (책임자). 같은 일이 예외 때 `LdrpProcessWork$fin$0 +0x83` (예외 정리 함수, 호출로 안 닿음) 에서도 일어난다. 거기서는 `ctx` 의 상태를 `0xc0000005` 로 쓰고 `LdrpLogError` 한다 (`+0x16`~`+0x29`).
- 실패 처리: 위 표 (`LdrpLogError`/`LogLoadFailureEtwEvent`/`ReportError`) 가 다. 짧게.

### 5. `LdrpSignalModuleMapped` — 부모 카운터 줄이기

- 근거: `ms/disassemble/ntdll/LdrpSignalModuleMapped.asm` (`rva 0xe23e0`, 100바이트), `LdrpProcessMappedModule.asm`.
- 인자 `RCX` = **방금 올라온 모듈의 항목** (`LdrpProcessMappedModule +0x1dd` 에서 `MOV RCX,RBX`). 순서:
  1. `+0xa` `RAX = 항목+0x98 (DdagNode)`; `+0x11` `RDI = 노드+0x30 (IncomingDependencies.Tail)`. 0 이면 (들어오는 간선 없음 = exe) 바로 끝 (`+0x18 JNZ +0x26`, `+0x24 RET`).
  2. 고리 (`+0x26~+0x63`): `IncomingDependencies` 목록을 한 바퀴 돈다 (끝 `+0x5e CMP RBX,RDI; JZ +0x1a`, 안 끝나면 `+0x63 JMP +0x29`). 칸 한 개 = 간선 칸의 `+0x10` 고리 (sast/18).
  3. 칸의 `+8` (= 간선 칸 `+0x18`, 부모 노드, 맨 아래 비트 지움 `AND -8`, `+0x30`) = **부모 노드** `RDX`. `RAX=[RDX]` (노드의 `Modules` 첫 모듈 링크 = `NodeModuleLink(+0xa0)`), `RCX = [RAX+0x10]` = 부모 항목의 **`LoadContext(+0xb0)`** (`+0x37`). 0 이면 이 부모는 건너뜀 (`+0x3e JZ +0x5e`).
  4. **`+0x40 MOV EAX,[RCX+0x6c]`, `+0x43 DEC EAX`, `+0x45 MOV [RCX+0x6c],EAX`** — **부모 문맥의 `+0x6c` 를 1 줄임.**
  5. 부모 노드 `State(+0x38) == 5` (Snapped) 이면 건너뜀 (`+0x48 CMP; +0x4c JZ +0x5e`). 줄인 값 ≠ 0 이어도 건너뜀 (`+0x4e TEST; +0x50 JNZ`).
  6. 0 이 됐고 아직 Snapped 가 아니면 **`+0x52 MOV [RDX+0x38],4` (부모 `State = Snapping`)**, **`+0x59` / `+0x5e` `LdrpQueueWork(부모 문맥)`**.
- **답**: 요청서의 "자식이 다 올라온 뒤 부모 카운터를 줄이고 snap 으로 잇는 코드" = **이 함수** 다 (`+0x40~+0x59`). `LdrpQueueWork` 로 넘긴 부모 문맥은 질문 4 에서 `State ≠ 0` 이라 `LdrpSnapModule` 로 가는 일감이 된다.
- 이 함수 안에서 `+0x6c` 를 줄이는 줄은 이 하나뿐이다 (이 다섯 함수 중). 다른 두 곳은 이미 알려진 것: `LdrpMapAndSnapDependency +0x461~+0x47d` (자기 몫 1, sast/17), `LdrpLoadDependentModuleInternal +0x12df~+0x12f7` (자식이 이미 `State ≥ 2` 일 때, sast/18). 셋이 합쳐서 "자식 수 + 1" 을 0 으로 만든다: **마지막으로 줄이는 쪽이 부모 snap 을 시작한다** `[정적]`.
- **언제 불리나** (`LdrpProcessMappedModule`): 락 `LdrpModuleDatatableLock` 독점 안 (`+0x1b6` / `+0x1bb` 잡기 ~ `+0x1e9` / `+0x1ee` 풀기)에서 `+0x1c2 DdagNode.State = 2` (Mapped) 를 쓴 **바로 뒤**, 두 조건이 참일 때: (a) 인자 2 의 비트 23 (0x800000) 이 꺼짐 (`+0x1c9~+0x1ce`, `LdrpMapAndSnapDependency` 가 `ctx+0x20 & 0x800000` 을 본 그 비트와 같은 자리로 보임 `[추측]`), (b) 항목 `LoadContext(+0xb0) ≠ 0` (`+0x1d0~+0x1d8`). 이름이 `Signal` 인 이유 = "Mapped 가 됐다고 부모들에게 알림".
  - 즉 "자식이 **Mapped** 가 되면" 이지 "자식이 snap 까지 끝나면" 이 아니다. 자식 import 의 자식(손자)은 자식의 `LdrpMapAndSnapDependency` 가 따로 센다.

### 6. 그래프 정리 (`LdrpCondenseGraph` → `Recurse` → `LdrpMergeNodes`)

- 근거: `LdrpCondenseGraph.asm` (43바이트), `LdrpCondenseGraphRecurse.asm` (344바이트), `LdrpMergeNodes.asm` (876바이트). PDB `_LDR_DDAG_NODE` (크기 0x50): `+0x00 Modules`, `+0x18 LoadCount`, `+0x20 LowestLink`, `+0x28 Dependencies`, `+0x30 IncomingDependencies`, `+0x38 State`, `+0x40 CondenseLink`(`_SINGLE_LIST_ENTRY`), `+0x48 PreorderNumber`. `_LDR_DDAG_STATE`: `-5 Merged`, `-3 SnapError`, `5 Snapped`, `6 Condensed`.
- **무엇을 "정리" 하나 = 서로 기대는 고리(강하게 연결된 묶음)를 노드 하나로 합친다.** 코드가 **Tarjan 알고리즘** 모양이다 (preorder 번호 + lowest link + 스택) `[정적: 코드 모양, 알고리즘 이름은 추측]`:
  - `LdrpCondenseGraph(노드)`: **입구 인자는 `DdagNode`** (`LdrpLoadDllInternal +0x256 MOV RCX,[RSI+0x98]`, 〃 `+0x25d` call / `+0x262` ret). `State < 6` 일 때만 (`+0xb JGE +0x26`) 지역 변수 두 개(번호 세는 값 0, 스택 머리 1)를 만들어 `Recurse` 를 부른다 (call `+0x21` / ret `+0x26`).
  - `Recurse(노드, &번호, &스택)`:
    - `+0x13~+0x24`: 번호 +1 → `PreorderNumber(+0x48)` 와 `LowestLink(+0x20)` 에 씀. `+0x2a~+0x39`: `CondenseLink(+0x40)` 를 스택에 올림.
    - `+0x2d~+0x3f`: `Dependencies.Tail(+0x28)` 이 0 이 아니면 `+0x110` 고리: 간선 칸 `+8` = **자식 노드**. 자식 `State > 5` (이미 Condensed 이상, `+0x117 CMP; +0x11b JLE`) 이면 건너뜀, 아니면:
      - 자식 `PreorderNumber == 0` (아직 안 봄, `+0x128~+0x12d`) → **자기 재귀** `+0x138` / `+0x13d` → 돌아와서 `LowestLink = min(내 것, 자식 것)` (`+0x13d~+0x145`).
      - 이미 본 자식 (`+0x157`): 자식 `CondenseLink(+0x40) ≠ 0` (= 아직 스택에 있음)이면 `LowestLink = min(내 것, 자식 `PreorderNumber`)` (`+0x15e JMP +0x140`).
      - 자식 `State == -3` (`LdrModulesSnapError`) 이면 내 `State = -3` 도 (`+0x148~+0x14e`, 에러 전파).
    - 고리가 끝나면 `+0x45~+0x50`: `PreorderNumber == LowestLink` 이면 **묶음의 뿌리**. 스택에서 내 `CondenseLink` 까지 꺼낸다 (`+0x52~+0x8a`).
      - 나만 있으면 (`+0x65 CMP R15,RCX; JZ +0x96`): `CondenseLink = 0`, `State == 5` 이면 `State = 6` (`+0x96~+0xa3`). **합치지 않는다.**
      - 나 말고 꺼낸 게 있으면: `+0xbf` / `+0xc6` **`RtlAcquireSRWLockExclusive(LdrpModuleDatatableLock)`** (`ntdll+0x1d2458`), **`+0xd3` / `+0xd8` `LdrpMergeNodes(뿌리, &꺼낸 목록)`**, `+0xdf` / `+0xe4` `RtlReleaseSRWLockExclusive`. 그다음 목록에 남은 노드를 하나씩 **`+0xfd` / `+0x102` `LdrpDestroyNode`** (목록은 `+0x107 JMP +0x96` 으로 끝나 뿌리 `State 5→6`).
  - `LdrpMergeNodes(뿌리 RCX, 목록 RDX)` (잠금 안에서, 본문 요약 `[정적]`):
    - 시작/각 노드마다 `LdrpLogInternal` 2번 (`+0x51` / `+0x56`, `+0xa1` / `+0xa6`). 로그 이름 = 항목 `BaseDllName` (`항목+0x58`).
    - 합쳐지는 노드마다: 그 노드의 `Modules` **첫 모듈**(`+0x59 RSI=[노드+0]`)을 자기 목록에서 빼서 **뿌리의 `Modules` 꼬리에 끼우고** (`+0x5d~+0xc4`), 항목의 `DdagNode(+0x98)` 를 뿌리로 바꿈 (`+0xa6 MOV [RSI-8],RBX`). 노드가 모듈을 하나씩만 가졌다고 가정한 모양이다 `[추측]`.
    - `LoadCount` 를 뿌리에 더하고 자기는 0 (`+0xc8~+0xd2`). 합쳐진 노드 `State = 0xfffffffb` (-5, `Merged`, `+0xd9`).
    - 합쳐진 노드의 **모든 간선 칸 끝을 뿌리로 바꿈**: `Dependencies` 목록 칸의 `+0x18`(부모 노드) = 뿌리 (`+0xe0~+0xea`), `IncomingDependencies` 목록 칸의 `+0x08`(자식 노드) = 뿌리 (`+0xf3~+0xfd`).
    - 다 끝나고 (`+0x10b~`) 뿌리의 `Dependencies`/`IncomingDependencies` 에서 **뿌리→뿌리로 가는 칸**(합치면서 생긴 자기 고리)과 같은 칸 중복을 목록에서 빼고 `RtlFreeHeap(LdrpHeap, 0, 칸)` (call `+0x1e4`/`+0x237`/`+0x30d`, ret `+0x1e9`/`+0x23c`/`+0x312`) 하고 `LoadCount -= 1` (`R12D = -1`).
- **합친 노드의 간선**: 끝점을 뿌리로 옮기고 자기 고리·중복은 지운다 (위).
- **`LdrpDestroyNode` 는 언제**: `Recurse` 안 `+0xfd` 하나뿐 (이 세 함수 중). 합치고 난 뒤 **목록에 남은 노드(=합쳐지고 `Modules` 가 빈 노드)** 마다. 본문은 안 읽음 (`LdrpDestroyNode` 는 `LdrpLoadDependentModuleInternal +0x1f55` 등 다른 caller 도 있음, 지도).
- **test.exe → dll1 → dll2 (고리 없음)**: 각 노드가 `PreorderNumber == LowestLink` 이고 스택 맨 위가 자기이므로 전부 "나만 있으면" 길(`+0x96`)이다. **`LdrpMergeNodes` 도 `LdrpDestroyNode` 도 안 부른다** `[정적: 코드 모양으로 그렇다]`. 하는 일은 노드마다 `State 5 → 6` 뿐. 시스템 DLL(kernel32·kernelbase·ntdll 등) 끼리 고리가 있는지는 모름 → 동적.
- 이 함수가 불리는 때: `LdrpPrepareModuleForExecution +0x12a`/`+0x12f`, `LdrpLoadDllInternal +0x25d`/`+0x262` (후자는 `항목.LoadContext(+0xb0) ≠ 0` 일 때, `+0x24c~+0x254`).

### 7. 이야기 그림 잇기

#### 일감 흐름 한 바퀴 그림 [정적]

```
[부모 P 가 import 서술자마다 자식 C 를 처리]  LdrpMapAndSnapDependency(Pctx)   (sast/17, 이미 앎)
   └ LdrpLoadDependentModuleInternal(Pctx, C)                                (sast/18)
        새 DLL 이면: Cctx 만들기(+0x30=부모 항목), 항목·노드(State 0)
        간선 쓰기 (+0xf69/+0xf8b) → LdrpQueueWork(Cctx)   (+0x182c, 락 안)
             │
             ▼  LdrpQueueWork
        LdrpWorkQueue 꼬리에 Cctx+0x40 끼움   (락 LdrpWorkQueueLock)
        LdrpMapAndSnapWork≠0 이고 !ShutdownInProgress 면 TpPostWork   (+0x7d/+0x82)
             │
   ┌─────────┴───────────────────────────────┐
   ▼                                          ▼
 (a) 책임자(LoadOwner) 스레드                  (b) 풀 일꾼 (LoaderThreads>1, !Detour 일 때만 존재)
   LdrpDrainWorkQueue(1)                         TppWorkpExecuteCallback  ← 콜백==LdrpWorkCallback 이면 복사본 반복
   일감 있으면 직접 꺼냄 (+0x182)                 (원본 LdrpWorkCallback: 같은 반복, 비면 끝)
   없고 WorkInProgress≠1 → NtWait(WorkComplete)   일감 있으면 꺼냄, 메인 토큰 가장
   (+0x194)                                      
   └──────────────┬────────────────────────────┘
                  ▼  LdrpProcessWork(Cctx, 0)   (Drain +0x182 / WorkCallback +0xb3 / Tpp +0x2db)
          Cctx→항목→State:
          ├ 0 (PlaceHolder)  → 올리기: PatchImage / Retry / FullPath / SearchPath   (+0x153,+0x149,+0x13f,+0x65)
          │     └ ... LdrpMapDllWithSectionHandle (sast/17: +0x26c LdrpProcessMappedModule, +0x1c3 MapAndSnapDependency)
          │          ├ LdrpProcessMappedModule: 락 안에서 C.State=2, LdrpSignalModuleMapped(C항목)  ◄── ① 부모들에게 알림
          │          │      각 부모 P (IncomingDependencies): P.ctx+0x6c--, ==0 이면 P.State=4, LdrpQueueWork(Pctx)
          │          └ LdrpMapAndSnapDependency(Cctx) (C 의 import 로 재귀: 손자 → 위 맨 위로)
          │                 끝맺음: Cctx+0x6c-- (자기 몫), ==0 이면 State=4, +0x30==0 면 직접 Snap / 아니면 QueueWork(Cctx)
          └ ≠0 (예: 4 Snapping) → LdrpSnapModule(ctx)   (+0x12c, 주소 연결. 본문은 Q50)
                  │
                  ▼ 끝: DL==0 이면 lock, WorkInProgress--, 큐 비었고 값==1 이면 NtSetEvent(LdrpWorkCompleteEvent)  (+0x1c3)
                  │       → (a)의 Drain(1) 이 깨어남 ... 큐 비고 WorkInProgress==1(+재시도 큐 비) 이면 끝, TEB LoadOwner 켜고 반환
                  ▼
   [나중] LdrpDropLastInProgressCount: LoadOwner 끄고 WorkInProgress=0, NtSetEvent(LdrpLoadCompleteEvent)
   [snap 후] LdrpCondenseGraph(노드): 고리 없으면 State 5→6 만
```

- 한 바퀴가 이 다섯 함수로 **거의 닫힌다.** 모르는 칸 (`?`): ① `LdrpSnapModule` 안에서 import 주소를 쓰는 곳과 `State` 를 `5` 로 쓰는 곳 (Q50). ② 올리기 길 `LdrpMapDllSearchPath` 등 안 (파일 맵·섹션) — 요청서 범위 밖. ③ `LdrpCondenseGraph` 를 부르는 `LdrpPrepareModuleForExecution +0x12a` 가 State 5 일 때 부르는지 (앞 단계).

#### 그래프 정리 짧은 트리

```
LdrpCondenseGraph(노드)                  State<6 일 때만
└ LdrpCondenseGraphRecurse(노드,&번호,&스택)     (+0x21/+0x26)
    ├ 번호 매기기 (+0x48,+0x20), 스택에 올림 (+0x40)
    ├ Dependencies 간선마다 자식:  (State>5 면 건너뜀)
    │    └ 안 본 자식이면 자기 재귀 (+0x138/+0x13d), LowestLink 로 min
    ├ 뿌리(번호==LowestLink)면 스택에서 묶음 꺼냄
    │    ├ 나 혼자: State 5→6                              ← test.exe/dll1/dll2
    │    └ 여럿:  락(LdrpModuleDatatableLock 독점)
    │              LdrpMergeNodes(뿌리, 목록)  (+0xd3/+0xd8)
    │                 모듈을 뿌리로, 간선 끝점 뿌리로, 자기 고리·중복 칸 해제, 합쳐진 노드 State=-5
    │            락 풀고, 남은 노드마다 LdrpDestroyNode (+0xfd/+0x102), 뿌리 State 5→6
```

#### run22·23 이야기 그림에서 채운 `?` 칸

1. "**일감을 누가 꺼내 올리나**": 책임자 스레드가 `Drain` 안에서 직접(큐 비면 기다림), 또는 `TpPostWork` 로 깬 풀 일꾼 (`TppWorkpExecuteCallback` 복사본 또는 `LdrpWorkCallback`). → 채움 `[정적]`. 단 어느 스레드가 실제로 했는지는 동적 (`?` 남음).
2. "**자식이 다 올라온 뒤 부모 카운터(`+0x6c`)를 줄이고 snap 으로 잇는 코드**": `LdrpSignalModuleMapped +0x40~+0x59` (자식 Mapped 직후, 락 안), 그리고 이미 알던 두 곳. → 채움.
3. "일감 넣기 → 꺼내 올리기 → 올린 뒤 import 고리 → 자식 끝 → 부모 snap" 의 연결: 위 그림. `LdrpProcessWork` 가 State 로 올리기/snap 을 가른다. → 채움.
4. kernel32·kernelbase 가 병렬 로딩 켜기 **앞**에서 어떻게 큐를 쓰나: `LdrpMapAndSnapWork == 0` 이라 `TpPostWork` 없이 Drain 하는 스레드(`LoadDllInternal +0x185/+0x215`, `InitializeProcess +0x19c1`)가 큐를 비움 `[정적: 코드상]`. kernel32 의 `ctx+0x30 == 0` (sast/17) 이라 자기 끝맺음에서 `LdrpQueueWork` 를 안 부르고 직접 `LdrpSnapModule` 한다. → 채움 (코드상).
5. 그래프 정리: dll1→dll2 에서는 합칠 게 없다. `State 5→6` 만. → 채움.
- 남은 `?`: `LdrpSnapModule` 본문(Q50), 실제 일꾼 스레드 수/누가 처리했는지(동적), `LdrpDestroyNode` 본문.

#### 그림 2 `(정적)` 상자 후보 (2 ~ 4개)

1. `ntdll!LdrpQueueWork` — "일감을 큐 꼬리에 넣고, 일꾼 개체가 있으면 TpPostWork" (이유: 병렬 로딩 켜짐의 유일한 판단 자리).
2. `ntdll!LdrpProcessWork` — "State 0 이면 올리기, 아니면 snap" (이유: 한 일감이 두 역할인 것이 흐름 이해의 열쇠).
3. `ntdll!LdrpSignalModuleMapped` — "자식 Mapped → 부모 카운터 줄임 → 0 이면 부모 snap 일감" (이유: run22 `?` 를 닫는 코드).
4. `ntdll!LdrpDrainWorkQueue` — "(0) 책임자 되기 / (1) 큐 비우고 일꾼 기다리기" (이유: 책임자 비트 `LoadOwner` 가 여기서 켜짐).

## 동적분석으로 확인해야 할 것

- **정적으로 못 정한 것** (실행 값):
  1. `ProcessParameters.LoaderThreads` 값과 `LdrpMapAndSnapWork` (`ntdll+0x1d2640`) 가 켠 뒤에 0 이 아닌가 → 일꾼이 실제로 있는가. (Q47 와 같은 자리)
  2. 일감 하나(`dll1`, `dll2`)를 **어느 스레드가** 처리했나 (메인 `Drain(1)` vs 풀 일꾼). 후자면 콜스택이 `TppWorkpExecuteCallback+0x2e0` 인지 `LdrpWorkCallback+0xb8` 인지.
  3. `Drain(1)` 이 `NtWait` (`+0x199`) 까지 가나, 아니면 메인이 다 꺼내서 안 기다리나.
  4. 시스템 DLL 에 고리가 있어 `LdrpMergeNodes` 가 불리나 (아마 안 불림).
- 추천 멈춤점 (`ntdll` 기준 rva, test.exe 만 멈추게 조건 붙임. 이미 대기 중인 **Q47**(일꾼 스레드), **Q23**(dll1·dll2 올라오는 순간), **Q20** 곁에 붙임. 새 번호는 오케스트레이터가 정함):
  - `ntdll+0xe5809` (`LdrpEnableParallelLoading +0xdd` `TpAllocWork` call 직전), `ntdll+0xe580e` (ret 자리): 직후 `dq ntdll+0x1d2640 L1` 로 `LdrpMapAndSnapWork`. 입구 `ntdll+0xe572c` `ecx` = `LoaderThreads`. (Q47)
  - `ntdll+0xe24c9` (`LdrpQueueWork +0x7d`, `TpPostWork` call 직전): 걸리면 일꾼을 깨우는 것. 안 걸리고 `ntdll+0xe244c` (입구) 만 걸리면 `LdrpMapAndSnapWork == 0` 거나 `ShutdownInProgress`. (Q47)
  - `ntdll+0x17140` (`LdrpProcessWork` 입구) 에서 `~` 와 `k` (어느 스레드, 호출한 곳이 `LdrpDrainWorkQueue+0x187` 인지 `TppWorkpExecuteCallback+0x2e0` 인지 `LdrpWorkCallback+0xb8` 인지), `rdx` = 인자 2. (Q23)
  - `ntdll+0xad400` (`LdrpDrainWorkQueue` 입구) `ecx` = 0/1. 기다림 확인은 `ntdll+0xad594` (`+0x194` `NtWaitForSingleObject` call 직전).
  - `ntdll+0xe2420` (`LdrpSignalModuleMapped +0x40`, 부모 카운터 줄이기 직전) `dd @rcx+0x6c L1`, `ntdll+0xe2439` (`+0x59` `LdrpQueueWork` call 직전: 걸리면 부모 snap 일감이 만들어지는 것). (Q23)
  - `ntdll+0x1726c` (`LdrpProcessWork +0x12c` `LdrpSnapModule` call 직전) vs `ntdll+0x171a5` (`+0x65` `LdrpMapDllSearchPath` call 직전): 일감마다 어느 쪽인지.
  - `ntdll+0xe9ef0` (`LdrpWorkCallback` 입구): **안 걸려도 이상하지 않다** (`TppWorkpExecuteCallback` 복사본). 대신 `ntdll+0x19050` (`TppWorkpExecuteCallback +0x2e0` ret 자리).
  - `ntdll+0xed9f4` (`LdrpMergeNodes` 입구): 안 걸리는 게 정상일 것.

## 판정

**분석성공.** 요청서 7개 질문 모두 `.asm` 줄과 call·ret 로 답했다. 점프 목적지 환산은 스크립트로 전부 확인 (128개 중 127개 함수 안 명령과 일치, 나머지 1개는 `NtSetEvent` 꼬리호출). 버전 대조 일치.

- **Q14 ("dll1·dll2 를 올리는 쪽") 판단: 일부.** 코드 쪽 길은 완결됐다 (`LdrpMapAndSnapDependency +0x34e` → `LdrpLoadDependentModuleInternal` → `LdrpQueueWork` → `LdrpProcessWork` → `LdrpMapDllSearchPath`…). 하지만 **어느 스레드가 어느 줄에서 올렸는지** 는 `LoaderThreads` 와 `LdrpMapAndSnapWork` 값에 달려 있어 정적으로 못 정함 → 동적 (Q23·Q47).
- 동적 요청 필요: 위 "동적분석으로 확인해야 할 것" 1~3번 (Q47·Q23 곁에 붙이는 방식 제안).

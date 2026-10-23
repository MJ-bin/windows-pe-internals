# 2026-10-23 run24 — 일감 흐름: `LdrpQueueWork` 가 큐 꼬리에 넣고 → 로드 책임자(`LdrpDrainWorkQueue`) 나 풀 일꾼이 꺼내 → `LdrpProcessWork` 가 노드 `State` 로 "올리기 / snap" 을 고르고 → 자식이 Mapped 가 되면 `LdrpSignalModuleMapped` 가 부모 카운터를 줄여 부모 snap 일감을 넣는다. 그래프 정리는 고리 합치기 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntdll (`ntdll.dll`, 26100.9457), pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (run17 ~ run23 과 같은 파일)
  - analyze 는 다시 안 했다. 이번에 새로 뽑은 함수: 요청서 8개 depth 1 (29개) + depth 0 다섯 (`LdrpEnableParallelLoading`, `LdrpInitParallelLoadingSupport`, `LdrpProcessMappedModule`, `TppWorkpExecuteCallback`, `LdrpCreateLoaderEvents`). `refs` 전역 11개 (`ms/disassemble/ntdll/_refs/`).
  - 버전 대조: **일치.** `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal`, `LdrpInitializeNode +0x166/+0x16b`·`+0x197/+0x19c` 가 run7·run8 관찰 값과 같다 (결과서).
- 근거로 읽은 것
  - 본문: `LdrpQueueWork` (rva `0xe244c`, 136바이트), `LdrpWorkCallback` (`0xe9ef0`, 218바이트), `LdrpDrainWorkQueue` (`0xad400`, 456바이트), `LdrpProcessWork` (`0x17140`, 465바이트), `LdrpSignalModuleMapped` (`0xe23e0`, 100바이트), `LdrpCondenseGraph` (`0xe9164`, 43바이트), `LdrpCondenseGraphRecurse` (`0xe91a0`, 344바이트), `LdrpMergeNodes` (`0xed9f4`, 876바이트).
  - 곁에서 본 것 (부르는 조건만): `LdrpEnableParallelLoading` (`0xe572c`), `LdrpProcessMappedModule` (`0xa974`, `+0x1b6 ~ +0x1ee`), `TppWorkpExecuteCallback` (`0x18d70`, `+0x19d ~ +0x2e0`), `LdrpInitParallelLoadingSupport`, `LdrpCreateLoaderEvents`, `LdrpDropLastInProgressCount`.
  - callee 본문은 안 읽었다: `LdrpMapDllSearchPath`·`LdrpMapDllFullPath`·`LdrpMapDllRetry`·`LdrpMapDllPatchImage`·`LdrpSnapModule`(Q50)·`LdrpDestroyNode`·`TpPostWork`·`TpAllocWork` 등은 이름·인자·결과만.
  - 구조체 칸 이름은 PDB 타입(`llvm-pdbutil`): `_LDR_DDAG_NODE` (`+0x00 Modules`, `+0x18 LoadCount`, `+0x20 LowestLink`, `+0x28 Dependencies`, `+0x30 IncomingDependencies`, `+0x38 State`, `+0x40 CondenseLink`, `+0x48 PreorderNumber`), `_LDR_DDAG_STATE` (`-5 Merged`, `-3 SnapError`, 0 PlaceHolder, 2 Mapped, 4 Snapping, 5 Snapped, 6 Condensed), `_PEB_LDR_DATA +0x48 ShutdownInProgress`, TEB `SameTebFlags(+0x17ee)` 의 `LoadOwner` = `0x1000`.
  - 로드 문맥(`_LDRP_LOAD_CONTEXT`)은 run22·23 처럼 칸 정보가 없어 **오프셋 + 역할**만 쓴다. 이번에 새로 나온 칸: `+0x40` = 일감 큐에 거는 목록 고리.

## 목적

- 대기열 **Q12c** (Q12 의 마지막 조각). run22·23 이 남긴 `?` 셋을 일감 함수 본문으로 푼다.
  - 일감을 **어디에 넣고, 누가, 언제 꺼내 올리나** (`LdrpQueueWork`·`LdrpWorkCallback`·`LdrpDrainWorkQueue`·`LdrpProcessWork`).
  - 자식이 다 올라온 뒤 **부모 카운터(`ctx+0x6c`)를 줄이고 snap 으로 잇는 코드** 가 어디인가 (`LdrpSignalModuleMapped`).
  - 그래프 **정리**(`LdrpCondenseGraph` → `Recurse` → `LdrpMergeNodes`)가 무엇을 하고, test.exe → dll1 → dll2 에서 무엇을 하나.

## 방법

- 정적: `_workspace/sast/19_work-queue.md` — ntdll, `^(LdrpQueueWork|LdrpWorkCallback|LdrpDrainWorkQueue|LdrpProcessWork|LdrpSignalModuleMapped|LdrpCondenseGraph|LdrpCondenseGraphRecurse|LdrpMergeNodes)$` depth 1
- 결과서: `_workspace/sast/19_work-queue_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntdll/{LdrpQueueWork,LdrpWorkCallback,LdrpDrainWorkQueue,LdrpProcessWork,LdrpSignalModuleMapped,LdrpCondenseGraph,LdrpCondenseGraphRecurse,LdrpMergeNodes,LdrpEnableParallelLoading,TppWorkpExecuteCallback,LdrpProcessMappedModule}.asm`
  - `ms/disassemble/ntdll/_calls.tsv`, `ms/disassemble/ntdll/_refs/` (전역 11개)
- 오프셋 읽는 법 (run9 ~ 23 과 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**. 표에는 둘 다, 글·그림 라벨에는 ret. 메모리에 쓰는 줄·점프는 그 명령 자리 하나.
- 결과서는 8개 함수의 점프 128개 목적지를 "절대 주소 − 함수 시작" 으로 스크립트로 대조했다 (127개 함수 안 명령과 일치, 나머지 1개는 `LdrpProcessWork +0x1c3` 의 `NtSetEvent` 꼬리 점프). 이 문서는 그 표에서 옮겼고, 주요 줄은 `.asm` 에서 한 번 더 봤다 (아래 도구 메모에 한 곳 고쳐 읽음).

## 결과

한 줄로: **일감 = 로드 문맥 하나. `LdrpQueueWork` 는 큐 꼬리에 넣기만 한다 (일꾼이 있으면 깨운다). 꺼내는 쪽은 둘 — 로드 책임자 스레드가 `LdrpDrainWorkQueue` 안에서 직접, 또는 풀 일꾼. 꺼낸 일감은 `LdrpProcessWork` 가 노드 `State` 를 보고 0 이면 올리고, 아니면 `LdrpSnapModule` 한다. 자식이 Mapped 가 되는 순간 `LdrpSignalModuleMapped` 가 부모들의 카운터를 1 줄이고, 0 이 된 부모를 "snap 일감" 으로 다시 큐에 넣는다.**

- 비유: 이삿짐 센터의 **일감 상자**.
  - 쪽지(로드 문맥)를 상자 맨 뒤에 넣는다. 아르바이트생(풀 일꾼)이 등록돼 있으면 "일 왔어요" 하고 부른다.
  - 현장 반장(로드 책임자)은 상자에 쪽지가 있으면 **직접** 꺼내 처리하고, 상자가 비었는데 아르바이트생이 아직 일하는 중이면 **끝났다는 벨**이 울릴 때까지 기다린다.
  - 쪽지 한 장은 두 종류다: "짐 가져오기"(올리기) 또는 "전기·수도 잇기"(snap). 짐표 상태가 아직 0 이면 가져오기, 아니면 잇기.
  - 짐 하나가 집 앞에 도착하면(Mapped), 그 짐을 기다리던 집들의 "남은 짐 수" 를 하나씩 뺀다. 0 이 된 집은 "이제 전기·수도 이어 주세요" 쪽지를 상자에 넣는다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpQueueWork` | `+0x19` | `+0x1e` | `RtlEnterCriticalSection(&LdrpWorkQueueLock)` — 문맥 상태가 음수가 아닐 때만 (`+0x10 JL` → 끝) | [정적] |
| 〃 | `+0x54` | `+0x59` | `RtlLeaveCriticalSection` — 그 사이 `ctx+0x40` 을 `LdrpWorkQueue` **꼬리**에 끼움 (`+0x1e ~ +0x4d`) | [정적] |
| 〃 | `+0x7d` | `+0x82` | **`TpPostWork(LdrpMapAndSnapWork)`** — `LdrpMapAndSnapWork ≠ 0` (`+0x59`) 이고 `PEB.Ldr.ShutdownInProgress == 0` (`+0x63 ~ +0x70`) 일 때만 | [정적] |
| `ntdll!LdrpEnableParallelLoading` | `+0x44` | `+0x49` | `TpAllocPoolInternal(&LdrpThreadPool)` — 늘 | [정적] |
| 〃 | `+0x7a` | `+0x7f` | `TpSetPoolMaxThreads(풀, 스레드 수 − 1 또는 1)` | [정적] |
| 〃 | `+0xdd` | `+0xe2` | **`TpAllocWork(&LdrpMapAndSnapWork, LdrpWorkCallback, ..)`** — 스레드 수 > 1 (`+0x7f`·`+0x82 JBE`) 이고 `LdrpDetourExist == 0` (`+0x84`) 일 때만 | [정적] |
| `ntdll!LdrpWorkCallback` | `+0x33` / `+0x70` | `+0x38` / `+0x75` | 임계 구역 들어가기 / 나오기 — 그 사이 큐 **머리**에서 하나 빼고 `LdrpWorkInProgress += 1` | [정적] |
| 〃 | `+0x64` | `+0x69` | `LdrpUpdateStatistics` — 일감이 있을 때 | [정적] |
| 〃 | `+0xcb` | `+0xd0` | `LdrpThreadTokenSetImpersonationToken(LdrpMainThreadToken)` — 메인 스레드 토큰 가장, 처음 한 번 | [정적] |
| 〃 | `+0xb3` | `+0xb8` | **`LdrpProcessWork(일감, 0)`** → `JMP +0x23` (맨 앞으로) | [정적] |
| 〃 | `+0x81` | `+0x86` | `LdrpThreadTokenSetImpersonationToken(0)` — 큐가 비어 끝낼 때 되돌리기 | [정적] |
| `ntdll!TppWorkpExecuteCallback` | `+0x2db` | `+0x2e0` | **`LdrpProcessWork(일감, 0)`** — 일감 개체의 콜백 칸 == `LdrpWorkCallback` 주소일 때 (`+0x1b4 CMP`), 같은 반복문을 이 함수 안에 복사해 둔 길 | [정적] |
| 〃 | `+0x4cb` | `+0x4d0` | 간접 호출 (CFG) — 콜백이 `LdrpWorkCallback` 이 아닐 때 | [정적] |
| `ntdll!LdrpDrainWorkQueue` | `+0x49` | `+0x4e` | `RtlEnterCriticalSection(&LdrpWorkQueueLock)` — 반복문 맨 앞 `+0x42` | [정적] |
| 〃 | `+0x15b` | `+0x160` | `LdrpUpdateStatistics` — 일감을 꺼냈을 때 (`WorkInProgress++` 뒤) | [정적] |
| 〃 | `+0x167` | `+0x16c` | `RtlLeaveCriticalSection` | [정적] |
| 〃 | `+0x182` | `+0x187` | **`LdrpProcessWork(일감, DIL)`** — 큐에 일감이 있을 때. **이 스레드가 직접 처리**, 끝나면 `JMP +0x42` | [정적] |
| 〃 | `+0x194` | `+0x199` | **`NtWaitForSingleObject(이벤트, FALSE, NULL)`** — 큐가 비었고 `LdrpWorkInProgress ≠ 인자` 일 때 (일꾼이 아직 일하는 중) | [정적] |
| 〃 | `+0xfb` / `+0x143` | `+0x100` / `+0x148` | 임계 구역 짝 — `Drain(1)` 끝 직전 `LdrpRetryQueue` 를 `LdrpWorkQueue` 로 옮길 때 | [정적] |
| `ntdll!LdrpProcessWork` | `+0x12c` | `+0x131` | **`LdrpSnapModule(ctx)`** — 노드 `State ≠ 0` 일 때 (`+0x34 ~ +0x38`) | [정적] |
| 〃 | `+0x153` | `+0x158` | `LdrpMapDllPatchImage` — `State == 0` 이고 항목 `LoadReason == 9` | [정적] |
| 〃 | `+0x149` | `+0x14e` | `LdrpMapDllRetry` — `ctx+0x20` 비트 20 (`0x100000`) | [정적] |
| 〃 | `+0x13f` | `+0x144` | `LdrpMapDllFullPath` — `ctx+0x20` 비트 9 (`0x200`) | [정적] |
| 〃 | `+0x65` | `+0x6a` | **`LdrpMapDllSearchPath(ctx)`** — 위 셋 다 아닐 때 (기본) | [정적] |
| 〃 | `+0x122` | `+0x127` | `LdrpReportError(ctx, 0, 상태)` — 상태 `0xc0000135` 이고 항목 `Flags & 0x20` (`ProcessStaticImport`) | [정적] |
| 〃 | `+0x178` / `+0x1a4` | `+0x17d` / `+0x1a9` | 임계 구역 짝 — 인자 2 == 0 일 때만. 그 사이 `LdrpWorkInProgress--` | [정적] |
| 〃 | `+0x1c3` | (꼬리 `JMP`, ret 없음) | `NtSetEvent(LdrpWorkCompleteEvent, 0)` — 큐가 비었고 줄인 값이 1 일 때 | [정적] |
| `ntdll!LdrpProcessMappedModule` | `+0x1b6` / `+0x1e9` | `+0x1bb` / `+0x1ee` | `RtlAcquire/ReleaseSRWLockExclusive(LdrpModuleDatatableLock)` — 그 사이 `+0x1c2` 노드 `State = 2` (Mapped) | [정적] |
| 〃 | `+0x1dd` | `+0x1e2` | **`LdrpSignalModuleMapped(항목)`** — 인자 2 비트 23 꺼짐 (`+0x1c9`) + 항목 `LoadContext ≠ 0` (`+0x1d0`) | [정적] |
| `ntdll!LdrpSignalModuleMapped` | `+0x59` | `+0x5e` | **`LdrpQueueWork(부모 문맥)`** — 부모 `ctx+0x6c` 를 줄인 값이 0 이고 부모 노드 `State ≠ 5` 일 때. 바로 앞 `+0x52` 부모 `State = 4` | [정적] |
| `ntdll!LdrpCondenseGraph` | `+0x21` | `+0x26` | `LdrpCondenseGraphRecurse(노드, &번호, &스택)` — 노드 `State < 6` 일 때만 (`+0x7 ~ +0xb`) | [정적] |
| `ntdll!LdrpCondenseGraphRecurse` | `+0x138` | `+0x13d` | `LdrpCondenseGraphRecurse` (자기 재귀) — 아직 번호가 없는 자식마다 | [정적] |
| 〃 | `+0xc6` / `+0xdf` | `+0xcb` / `+0xe4` | `RtlAcquire/ReleaseSRWLockExclusive(LdrpModuleDatatableLock)` — 묶음에 노드가 둘 이상일 때만 | [정적] |
| 〃 | `+0xd3` | `+0xd8` | **`LdrpMergeNodes(뿌리, &목록)`** — 위 락 안 | [정적] |
| 〃 | `+0xfd` | `+0x102` | `LdrpDestroyNode` — 합친 뒤 남은 노드마다 | [정적] |
| `ntdll!LdrpMergeNodes` | `+0x1e4` / `+0x237` / `+0x30d` | `+0x1e9` / `+0x23c` / `+0x312` | `RtlFreeHeap(LdrpHeap, 0, 칸)` — 자기 고리·중복 간선 칸 놓기 | [정적] |

- 잡일: `LdrpProcessWork` 로그·ETW 4곳 (`+0xc6 LdrpLogInternal`, `+0xdf LdrpLogError`, `+0xe7 LdrpLogDeprecatedDllEtwEvent`, `+0x110 LdrpLogLoadFailureEtwEvent`), `LdrpMergeNodes` 로그 2곳 (`+0x56`, `+0xa6`).
- 예외 조각 `LdrpProcessWork$fin$0` (호출로 안 닿음): 문맥 상태를 `0xc0000005` 로 쓰고 `+0x2e LdrpLogError`, 그다음 본문 끝과 같은 일(`WorkInProgress--`, 조건 맞으면 `NtSetEvent`)을 한다.
- 이번 8개 함수 + 곁 함수 호출 줄에 `alias:` 로 나온 이름은 없다.

### 정적 — 일감 큐 (`LdrpQueueWork`)

- **일감 = 로드 문맥**. 문맥 `+0x40` 이 목록 고리다. 큐 머리 = 전역 **`LdrpWorkQueue`** (`ntdll+0x1d26b0`, `Flink +0`, `Blink +8`), 잠금 = **`LdrpWorkQueueLock`** (`ntdll+0x1d2680`, 임계 구역 0x28바이트) `[정적]`.
- **꼬리에 붙인다** (`+0x1e ~ +0x4d`, `Blink` 쪽). 꺼내는 쪽(`LdrpWorkCallback`·`Drain`)은 머리에서 뺀다 → **먼저 넣은 것부터** 처리된다 `[정적]`. 꼬리 이웃 검사가 깨졌으면 `+0x3a INT 0x29` (빠른 실패).
- 문맥 상태(`*(ctx+0x28)`)가 이미 음수면 아무것도 안 한다 (`+0x10 JL +0x82`).
- **일꾼을 깨우는 조건** (`+0x59 ~ +0x7d`, 둘 다 참일 때 `TpPostWork`):
  1. 전역 **`LdrpMapAndSnapWork`** (`ntdll+0x1d2640`, 일감 개체 포인터) `≠ 0`
  2. `PEB→Ldr→ShutdownInProgress`(`+0x48`) 바이트 `== 0`
  - **"병렬 로딩이 켜졌나" 를 읽는 따로 된 깃발은 없다.** `LdrpMapAndSnapWork` 가 0 이냐가 곧 그 판단이다 `[정적]`.
- **이 함수는 넣기만 한다.** 안에서 `LdrpProcessWork` 를 부르지 않는다 (호출 3개뿐). 그러니 일감이 이 자리에서 바로 처리되는 일은 없다 `[정적]`. (`TpPostWork` 가 그 자리에서 콜백을 돌리지 않는다는 건 `[추측]` — 본문 안 봄.)
- `LdrpMapAndSnapWork` 를 만지는 곳 (`_refs/LdrpMapAndSnapWork.tsv`, 8줄):
  - 만든다: `LdrpEnableParallelLoading +0xa9` (`TpAllocWork` 의 출력 자리). 콜백 인자 `+0x9d` = **`LdrpWorkCallback`** 주소.
  - 지운다: `LdrpDetectDetour +0xd5` (`LdrpDetourExist` 가 켜지면 일감 개체를 기다렸다 놓고 0), `LdrpCompleteProcessCloning +0x78`.
  - 읽는다: `LdrpQueueWork +0x59`·`+0x76`, `LdrpMinimalMapModule +0x296` (뒤의 것은 이번에 안 봄).
- **`LdrpEnableParallelLoading` 이 일감 개체를 안 만드는 경우** (`+0x23 ~ +0x8b`, `+0xf9 ~ +0x105`):
  - 인자(`ProcessParameters.LoaderThreads`, run19) = 0 이면 스레드 수 = **4**, 다만 `RtlGetSuiteMask` 비트 16 이 켜졌으면 0. 인자 ≠ 0 이면 `min(인자, 16)`.
  - 풀은 늘 만든다 (`+0x49`). 최대 스레드 = 스레드 수 − 1 (1 이하면 1).
  - **스레드 수 ≤ 1 이거나 `LdrpDetourExist ≠ 0` 이면 `TpAllocWork` 를 안 부른다** → `LdrpMapAndSnapWork` 는 0 그대로 → 그 뒤 `LdrpQueueWork` 도 일꾼을 안 깨운다 `[정적]`.
- 그래서 **병렬 로딩 켜기(`LdrpInitializeProcess +0x1a42`) 앞**(kernel32 준비 때)에는 `LdrpMapAndSnapWork == 0` 이라 일꾼을 안 깨운다 `[정적]`. 켠 뒤 test.exe 에서 실제로 0 이 아닌지는 `LoaderThreads` 값·스위트 비트·`LdrpDetourExist` 에 달렸다 → 동적 Q47.
  - `[추측]` IFEO 를 따로 안 줬으면 `LoaderThreads` 는 0 일 것이고, 그러면 스레드 수 4 → 최대 3, 일감 개체가 만들어진다. run5 `[관찰]` 의 `TppWorkerThread` 2개와 어긋나지 않는다.
- 큐·잠금·이벤트를 처음 만드는 곳: `LdrpInitParallelLoadingSupport` (caller `LdrpInitializeProcess +0x12e3`, ret) — 빈 큐(자기를 가리킴), `+0x3f RtlInitializeCriticalSectionEx`(잠금), `+0x43` 꼬리 점프 `LdrpCreateLoaderEvents` → `NtCreateEvent` 2번 (`+0x23`, `+0x46`). 이벤트 둘 다 자동 리셋(Synchronization), 처음 꺼짐 (결과서).

### 정적 — 꺼내는 쪽 ①: 로드 책임자 (`LdrpDrainWorkQueue`)

- 인자 하나(`ECX`, 0/1)로 두 가지만 다르다 (`+0x2f TEST`, `+0x3a CMOVZ`):

| | `Drain(0)` | `Drain(1)` |
|---|---|---|
| 기다리는 이벤트 | `LdrpLoadCompleteEvent` (`ntdll+0x1d2620`) | `LdrpWorkCompleteEvent` (`ntdll+0x1d2660`) |
| 끝나는 조건 (큐가 빈 상태에서, `+0x90`) | `LdrpWorkInProgress == 0` | `LdrpWorkInProgress == 1` 그리고 `LdrpRetryQueue` 가 빔 (`+0xeb`) |
| 끝낼 때 | `WorkInProgress = 1` (`+0xa3`), TEB `SameTebFlags |= 0x1000` (**`LoadOwner`**, `+0xd3`) | 같은 줄 |
| 뜻 `[추측]` | "다른 로드가 다 끝날 때까지 기다렸다가 내가 **책임자**가 된다" | "책임자인 내가 큐를 다 비우고, 일꾼도 다 끝날 때까지 돕는다" |

- **반복문** (`+0x42` 맨 앞): 잠금 → 큐 머리에서 하나 빼기
  - 일감이 있으면 → `WorkInProgress++` → 잠금 풂 → **`+0x187 LdrpProcessWork(일감, DIL)`** → 맨 앞으로. **이 스레드가 직접 처리한다** `[정적]`.
  - 일감이 없고 `WorkInProgress ≠ 인자` → 잠금 풂 → **`+0x199 NtWaitForSingleObject(이벤트)`** (시간 제한 없음) → 맨 앞으로. **일꾼이 끝내길 기다린다** `[정적]`.
  - 일감이 없고 `WorkInProgress == 인자` → 위 표의 "끝낼 때".
  - 갈리는 조건 = **"큐에 일감이 있나"** 하나.
- `Drain(1)` 끝 직전 재시도 큐가 비어 있지 않으면 `LdrpRetryQueue` 전체를 `LdrpWorkQueue` 로 옮기고 (`+0x100 ~ +0x134`) 다시 돈다. 재시도 큐에 넣는 쪽은 이번에 안 봤다.
- `LdrpDetourExist ≠ 0` 이면 (`+0x58`): `Drain(1)` 은 일감을 세지 않고(`DIL = 1`) 처리, `Drain(0)` 은 큐를 안 건드리고 책임자만 된다.
- **이벤트를 켜는 쪽**: `LdrpWorkCompleteEvent` ← `LdrpProcessWork +0x1c3` (꼬리 점프 `NtSetEvent`). `LdrpLoadCompleteEvent` ← `LdrpDropLastInProgressCount +0x45` (꼬리 점프 `NtSetEvent`). `LdrpDropLastInProgressCount` 는 `LoadOwner` 비트를 지우고 `WorkInProgress = 0` 으로 둔다 = **책임자 내려놓기** (결과서).
- **run19·run21 caller 의 인자** (`.asm` 의 `ECX` 를 봄, 결과서 표):

| caller | call / ret | 인자 | 비고 |
|---|---|---|---|
| `LdrpInitializeProcess` | `+0x1302` / `+0x1307` | 0 | `+0x12e3 LdrpInitParallelLoadingSupport` 바로 뒤 |
| 〃 | `+0x19c1` / `+0x19c6` | 0 | kernel32 준비 `+0x19a2` 뒤, 병렬 켜기 `+0x1a47` 앞 |
| 〃 | `+0x1aac` / `+0x1ab1` | **1** | exe import `+0x1aa9` 바로 뒤 |
| 〃 | `+0x1b8c` / `+0x1b91` | 0 | `LdrpProcessInitContextRecord` 저장 뒤 |
| `LdrpLoadDllInternal` | `+0x185` / `+0x18a` | 0 | `LoadOwner` 가 이미 켜져 있으면 건너뜀 (`+0x16d ~ +0x174`) |
| 〃 | `+0x215` / `+0x21a` | **1** | `+0x210 LdrpProcessWork(ctx, 1)` 바로 뒤 |
| 〃 | `$fin$0 +0x38` / `+0x3d` | 1 | 예외 정리 |
| `LdrpFastpthReloadedDll` | `+0x11a` / `+0x11f` | 0 | `LoadOwner` 가 있으면 건너뜀 |
| `LdrGetProcedureAddressForCaller` | `+0x8d4` / `+0x8d9` | 0 | 바로 뒤 `LdrpDropLastInProgressCount` |

- 나머지 caller 5곳 (`LdrpFindLoadedDll`, `LdrpInitializeImportRedirection`, `LdrpInitializeThread`, `LdrUnloadDll`, `RtlQueryInformationActivationContext`) 은 `.asm` 이 없어 인자를 못 봤다.
- `LdrpInitializeProcess` 안에서 "책임자 기간" 은 **세 번** 열리고 닫힌다 `[정적: 위치만]`: `[+0x1307 ~ +0x1753]`, `[+0x19c6 ~ +0x1b40]`, `[+0x1b91 ~ +0x1be7]` (닫는 쪽 = `LdrpDropLastInProgressCount`, 지도). 둘째 기간 안에 병렬 켜기 `+0x1a47`, exe import `+0x1aa9`, `Drain(1)` `+0x1ab1` 이 다 들어 있다.

### 정적 — 꺼내는 쪽 ②: 풀 일꾼 (`LdrpWorkCallback`, `TppWorkpExecuteCallback`)

- `LdrpWorkCallback` 은 **지도에 없다** (호출 참조 0). `refs` 4곳: `LdrpEnableParallelLoading +0x9d` (주소를 `TpAllocWork` 에 넘김), `TppWorkpExecuteCallback +0x19d` (비교용으로 주소를 읽음), 함수 밖 2곳 (`GuardCFFunctionTable` 등 — CFG 허용 목록으로 보임 `[추측]`). → **주소로 넘겨지는 함수가 맞다** `[정적]`. 지도가 이런 길을 못 잇는 건 이미 알려진 한계다 (결과서 "스크립트 문제").
- 본문 = 반복문 `[정적]`:
  1. `+0x23` `LdrpDetourExist ≠ 0` 이면 끝.
  2. 잠금 → 큐 **머리** 하나 빼기 → 있으면 `WorkInProgress += 1`, `+0x69 LdrpUpdateStatistics` → 잠금 풂.
  3. 없으면 가장했던 토큰을 되돌리고(`+0x86`) **반환**.
  4. 있으면 처음 한 번 메인 스레드 토큰을 가장하고(`+0xd0`, `LdrpMainThreadToken ≠ 0` 일 때) **`+0xb8 LdrpProcessWork(일감, 0)`** → 1 로.
  - 한 번에 하나씩, **큐가 빌 때까지** 돈다. 기다림(`NtWait*`)은 없다.
- **실제 일꾼은 이 함수를 안 거칠 수 있다** `[정적: 코드 줄]`: `TppWorkpExecuteCallback +0x1b4` 가 일감 개체의 콜백 칸과 `LdrpWorkCallback` 주소를 비교해서 같으면, 같은 반복문의 **복사본**(`+0x1bd ~ +0x2db`: `LdrpDetourExist` 확인, 잠금, 빼기, `WorkInProgress` 증가, `+0x212 LdrpUpdateStatistics`, `+0x233` 토큰 가장)을 돌고 **`+0x2e0 LdrpProcessWork`** 를 직접 부른다. 다르면 `+0x4d0` 간접 호출.
  - 그래서 `LdrpWorkCallback` 함수 자체는 실행 때 **안 불릴 수도 있다** `[추측]`. 풀 일꾼의 콜스택엔 `TppWorkpExecuteCallback+0x2e0` 이 보일 것 `[추측: 확인은 동적]`.

### 정적 — 일감 처리 (`LdrpProcessWork`)

- 입구: `RCX = ctx`, `DL` = 인자 2 → `EDI`. 인자 2 = 1 이면 끝에서 `WorkInProgress` 를 안 줄이고 신호도 안 한다 (`+0x15d`). 뜻은 "이 일감을 `WorkInProgress` 에 센 적 없다" `[추측: 이름 없음]`.
  - 인자 2 값: `LdrpLoadDllInternal +0x210` → 1, `LdrpWorkCallback +0xb8` → 0, `TppWorkpExecuteCallback +0x2e0` → 0, `Drain +0x187` → `DIL` (= `LdrpDetourExist`, 평소 0), `LdrpLoadDependentModuleInternal +0x1cae` → 1 (run23).
- **한 일감 = 올리기 또는 snap** (`+0x29 ~ +0x38`): `ctx+0x38`(항목) → `DdagNode(+0x98)` → **`State(+0x38)`** 가
  - **0 (PlaceHolder) 이면 올리기**, 길은 위에서부터: `LoadReason == 9` → `+0x158 LdrpMapDllPatchImage` / `ctx+0x20` 비트 20 → `+0x14e LdrpMapDllRetry` / 비트 9 → `+0x144 LdrpMapDllFullPath` / 기본 → **`+0x6a LdrpMapDllSearchPath`**.
  - **0 이 아니면 `+0x131 LdrpSnapModule(ctx)`** 만. 올리지 않는다.
  - 한 호출 안에서 올리기와 snap 이 같이 일어나지 않는다 `[정적]`. 올린 뒤의 snap 은 **다른 일감**으로 온다 (아래 Signal, run22 `MapAndSnap +0x4f7`).
- 결과: 0 이상 또는 `0xc000022d`(이름 `STATUS_RETRY` 로 알려진 값 `[추측]`) 면 그냥 끝. 그 밖의 음수면 `*(ctx+0x28) = 상태` (`+0x13b`). `0xc0000135` 일 때만 로그·ETW 3개, 항목이 정적 import(`ProcessStaticImport`)면 `+0x127 LdrpReportError`.
- **끝** (인자 2 == 0 일 때): 잠금 → `WorkInProgress--` → **큐가 비었고 줄인 값이 1 이면** (`+0x192`, `+0x1c8`) 잠금 풂 → `NtSetEvent(LdrpWorkCompleteEvent)` (꼬리 점프 `+0x1c3`). 남은 1 = 책임자 몫이다 → `Drain(1)` 의 기다림(`+0x199`)을 깨운다 `[정적]`.

### 정적 — 부모 카운터를 줄이고 snap 으로 잇는 곳 (`LdrpSignalModuleMapped`)

- **답: 이 함수다** (`+0x40 ~ +0x59`) `[정적]`. run22·23 이 "두 함수 어디에도 없다" 고 남긴 칸.
- 언제: `LdrpProcessMappedModule` 이 `LdrpModuleDatatableLock` 독점 안에서 **`+0x1c2` 노드 `State = 2` (Mapped) 를 쓴 바로 뒤** `+0x1e2` 로 부른다. 조건 둘: 인자 2 비트 23(`0x800000`) 꺼짐, 항목 `LoadContext(+0xb0) ≠ 0`. 비트 23 이 `LdrpMapAndSnapDependency` 가 보는 `ctx+0x20 & 0x800000` 과 같은 것인지는 `[추측]`.
  - `LdrpProcessMappedModule` 의 caller 는 `LdrpMapDllWithSectionHandle +0x26c`, `LdrpInitializeProcess +0x14ee`·`+0x167e` (`_calls.tsv`).
- 하는 일 (인자 = 방금 Mapped 가 된 **자식 항목**):
  1. 자식 노드 `IncomingDependencies(+0x30)` 가 비었으면 (나를 기다리는 부모 없음) 바로 끝 (`+0x18`, `+0x24 RET`).
  2. 목록 한 바퀴 (`+0x26 ~ +0x63`). 칸마다 `+0x18`(run23 간선 칸의 부모 노드, 아래 비트 지움) = **부모 노드** → 부모 노드의 첫 모듈 → 그 항목의 **`LoadContext`** (`+0x37`). 0 이면 이 부모는 건너뜀.
  3. **`+0x40 ~ +0x45` 부모 문맥 `+0x6c` 를 1 줄인다.**
  4. 부모 노드가 이미 `State 5`(Snapped) 면 건너뜀. 줄인 값이 0 이 아니어도 건너뜀.
  5. 0 이 됐으면 **`+0x52` 부모 `State = 4` (Snapping)**, **`+0x5e LdrpQueueWork(부모 문맥)`**.
- 넣은 부모 문맥은 `State = 4` 라서 `LdrpProcessWork` 에서 **snap 일감**이 된다 (위).
- 부모 `+0x6c` 를 줄이는 곳은 이제 셋이다 `[정적]`. 셋이 합쳐서 "자식 수 + 1" 을 0 으로 만들고, **마지막으로 줄이는 쪽이 부모 snap 을 시작한다**:
  - `LdrpMapAndSnapDependency +0x461 ~ +0x47d` — 자기 몫 1 (run22). 0 이면 부모 없음 → `+0x4c2 LdrpSnapModule` 직접, 부모 있음 → `+0x4f7 LdrpQueueWork`.
  - `LdrpLoadDependentModuleInternal +0x12f7` — 새 간선이 안 생겼거나 자식이 이미 `State ≥ 2` 일 때 (run23).
  - **`LdrpSignalModuleMapped +0x45`** — 새로 이은 자식이 나중에 Mapped 가 될 때 (run24).
- 주의: "자식이 **Mapped**(메모리에 올라옴) 가 되면" 이다. "자식이 snap 까지 끝나면" 이 아니다. 손자는 자식의 `LdrpMapAndSnapDependency` 가 따로 센다.
- 이 함수는 잠금을 따로 안 잡는다. caller 가 `LdrpModuleDatatableLock` 독점을 잡은 채라 그 안이다. 그 안에서 `LdrpQueueWork` 가 `LdrpWorkQueueLock` 을 잡는다 (락 두 개가 겹침, 순서 = Datatable → WorkQueue) `[정적]`.

### 정적 — 그래프 정리 (`LdrpCondenseGraph` → `Recurse` → `LdrpMergeNodes`)

- **정리 = 서로 기대는 고리(돌고 도는 묶음)를 노드 하나로 합치기** `[정적: 코드 모양]`. 번호 매기기(`PreorderNumber`) + 가장 낮은 번호(`LowestLink`) + 스택(`CondenseLink`) 을 쓰는 모양이 Tarjan 알고리즘과 같다 (알고리즘 이름은 `[추측]`).
  - 비유: 가계도를 그리다가 "A 는 B 에 기대고, B 는 다시 A 에 기대는" 동그라미가 보이면, 둘을 한 칸으로 묶어 "한 집" 으로 친다. 그래야 "누구부터 켤까" 순서를 정할 수 있다.
- `LdrpCondenseGraph(노드)`: 노드 `State < 6` 일 때만 (`+0xb`) 번호 0, 스택 머리 1 을 만들어 `+0x26 Recurse`.
- `LdrpCondenseGraphRecurse(노드, &번호, &스택)`:
  - 번호 +1 → `PreorderNumber`·`LowestLink` 에 쓰고, 스택에 올림.
  - `Dependencies` 간선마다 자식 (칸 `+8`): 자식 `State > 5` 면 건너뜀. 번호가 없으면 **자기 재귀 `+0x13d`** 후 `LowestLink = min`. 이미 스택에 있으면 `LowestLink = min(내 것, 자식 번호)`. 자식 `State == -3`(SnapError) 면 나도 `-3`.
  - 끝나고 `PreorderNumber == LowestLink` 면 묶음의 뿌리 → 스택에서 내 것까지 꺼냄:
    - **나 혼자면** (`+0x65`): 합치지 않는다. `State == 5` 면 `State = 6` (Condensed, `+0x96 ~ +0xa3`).
    - **여럿이면**: `LdrpModuleDatatableLock` 독점 (`+0xcb`) → **`+0xd8 LdrpMergeNodes(뿌리, &목록)`** → 락 풂 (`+0xe4`) → 남은 노드마다 **`+0x102 LdrpDestroyNode`** → 뿌리 `State 5 → 6`.
- `LdrpMergeNodes` (본문 요약 `[정적]`): 합쳐지는 노드의 첫 모듈을 뿌리 `Modules` 꼬리로 옮기고 항목 `DdagNode` 를 뿌리로, `LoadCount` 를 뿌리에 더함, 그 노드 `State = -5`(Merged). 간선 칸의 끝점을 전부 뿌리로 바꾼 뒤, 뿌리 → 뿌리 칸(자기 고리)과 중복 칸을 빼서 `RtlFreeHeap`. 노드마다 모듈이 하나라고 가정한 모양 `[추측]`.
- `LdrpDestroyNode` 본문은 **안 봤다**.
- 불리는 곳: `LdrpPrepareModuleForExecution +0x12f` (노드 `State 5` 일 때, run21), `LdrpLoadDllInternal +0x262` (항목 `LoadContext ≠ 0` 일 때, run21).
- **test.exe → dll1 → dll2 (고리 없음)** `[정적: 코드 모양]`: 노드마다 자기가 뿌리이고 스택 맨 위가 자기라 전부 "나 혼자" 길. **`LdrpMergeNodes` 도 `LdrpDestroyNode` 도 안 부르고**, 노드마다 `State 5 → 6` 만 한다. exe 쪽은 `LdrpInitializeProcess +0x1af2 LdrpPrepareModuleForExecution(exe)` 의 `+0x12f` 가 그 자리일 것 (exe 노드가 그때 `State 5` 라면, `[추측]`). 시스템 DLL 끼리 고리가 있는지는 모름 → 동적.

### 정적 — 일감 한 바퀴 그림

```
[부모 P 의 import 고리]  LdrpMapAndSnapDependency(P문맥)  +0x34e →
  LdrpLoadDependentModuleInternal(C 이름, P문맥, ..)                           (run23)
    새 C문맥·항목·노드 (State 0) → KnownDll 아님 → 락(Datatable) 안에서
    간선 P → C (+0xf69/+0xf8b) → +0x182c LdrpQueueWork(C문맥)
      └ LdrpWorkQueue 꼬리에 C문맥+0x40 (락 WorkQueue)
        LdrpMapAndSnapWork ≠ 0 · !ShutdownInProgress 면 +0x82 TpPostWork    ── (b) 일꾼 깨움
    P문맥 +0x6c : C 는 아직 State < 2 → 안 줄임 (락 순서상 C 가 이 사이에 Mapped 가 될 수 없음)
  고리 끝 → P 자기 몫 −1 (+0x474, 락 Datatable 안)

         ┌──────────────────── 누가 꺼내나 ────────────────────┐
 (a) 로드 책임자 스레드                         (b) 풀 일꾼 (LdrpMapAndSnapWork ≠ 0 일 때만 있음)
     LdrpDrainWorkQueue(1)                          TppWorkpExecuteCallback (복사본 반복)
       일감 있음 → +0x187 LdrpProcessWork            또는 LdrpWorkCallback 반복
       없음·WorkInProgress≠1 → +0x199 NtWait         일감 있음 → +0x2e0 / +0xb8 LdrpProcessWork(.., 0)
         └────────────────────────┬──────────────────────────┘
                                  ▼
  LdrpProcessWork(C문맥)   C.State == 0 → 올리기
    +0x6a LdrpMapDllSearchPath → (지도) … → LdrpMapDllWithSectionHandle
        +0x26c LdrpProcessMappedModule: 락(Datatable) 안 C.State = 2
               +0x1e2 LdrpSignalModuleMapped(C 항목)
                  부모 P 마다: P문맥 +0x6c −1 → 0 이면 P.State = 4, +0x5e LdrpQueueWork(P문맥)   ◀ P snap 일감
        +0x1c8 LdrpMapAndSnapDependency(C문맥)   ← C 의 import 로 같은 바퀴 (손자)
               끝맺음: C 자기 몫 −1 → 0 이면 C.State = 4, C 는 부모가 있으니 +0x4f7 LdrpQueueWork(C문맥)
    끝: WorkInProgress −1, 큐 비고 값 1 이면 NtSetEvent(LdrpWorkCompleteEvent)  → (a) 깨어남

  나중에 꺼낸 P문맥:  LdrpProcessWork(P문맥)   P.State == 4 ≠ 0 → +0x131 LdrpSnapModule(P문맥)   (본문 Q50)

  (a) Drain(1): 큐 비고 WorkInProgress == 1 (+ 재시도 큐 빔) → LoadOwner 켠 채 반환
  [나중] LdrpDropLastInProgressCount: LoadOwner 끔, WorkInProgress = 0, NtSetEvent(LdrpLoadCompleteEvent)
  [snap 뒤] LdrpCondenseGraph(노드): 고리 없으면 State 5 → 6 만
```

- 모든 줄이 `[정적]` (코드 길). 실제로 어느 스레드가 어느 일감을 처리했는지는 모른다 (동적 Q47·Q23).
- `?` 로 남는 것: `LdrpMapDllSearchPath` → `LdrpMapDllWithSectionHandle` 사이 (지도 이름 사슬 `+0x25c LdrpMapDllNtFileName` → `+0x28c`, run21, 본문 안 봄), `LdrpSnapModule` 안에서 IAT 를 쓰고 `State = 5` 를 쓰는 곳 (Q50), `LdrpDestroyNode` 본문.

### 정적 — test.exe → dll1 → dll2 이야기 (run22·23 그림의 `?` 칸 채우기)

일꾼이 **없다고 칠 때** (메인 스레드 혼자, 큐는 먼저 넣은 것부터) 코드대로 따라간 순서다. 일꾼이 있으면 (b) 쪽이 중간 일감을 가져가 순서가 섞일 수 있다. 모듈 대응(`dll1`·`dll2`)은 소스 의존 관계에서 짐작 `[소스]`+`[추측]`.

```
LdrpInitializeProcess
 +0x1aa9 LdrpMapAndSnapDependency(exe문맥)          exe문맥 +0x6c = 1 + 1
   +0x34e Internal("dll1.dll") → 간선 exe → dll1 → +0x182c LdrpQueueWork(dll1문맥)   큐: [dll1 올리기]
          dll1 State < 2 → exe +0x6c 그대로 (2)                                     ← run24 (run23 "≥ 2 면 −1" 은 이 길엔 안 나옴)
   자기 몫 −1 → 1 → snap 안 함 (+0x4c2 안 탐)                                          ← run24 [추측: 일꾼 없을 때]
 +0x1ab1 LdrpDrainWorkQueue(1)
   (1) +0x187 ProcessWork(dll1문맥)  State 0 → +0x6a MapDllSearchPath → … MapDllWithSectionHandle
         +0x26c ProcessMappedModule: dll1 State = 2
           +0x1e2 Signal(dll1) → exe +0x6c 1 → 0 → exe State = 4 → +0x5e QueueWork(exe문맥)   큐: [exe snap]
         +0x1c8 MapAndSnap(dll1문맥)  +0x6c = 1 + 1
           +0x34e Internal("dll2.dll") → 간선 dll1 → dll2 → +0x182c QueueWork(dll2문맥)    큐: [exe snap, dll2 올리기]
           자기 몫 −1 → 1 → 아무것도 안 함
   (2) +0x187 ProcessWork(exe문맥)   State 4 → +0x131 LdrpSnapModule(exe)
   (3) +0x187 ProcessWork(dll2문맥)  State 0 → 올리기 → … MapDllWithSectionHandle
         +0x26c ProcessMappedModule: dll2 State = 2
           +0x1e2 Signal(dll2) → dll1 +0x6c 1 → 0 → dll1 State = 4 → QueueWork(dll1문맥)  큐: [dll1 snap]
         +0x1c8 MapAndSnap(dll2문맥)  (dll2 의 import = ? 이미 올라온 것뿐이라면) → 0 → State 4
           +0x4f7 QueueWork(dll2문맥)                                                     큐: [dll1 snap, dll2 snap]
   (4) +0x187 ProcessWork(dll1문맥)  State 4 → +0x131 LdrpSnapModule(dll1)
   (5) +0x187 ProcessWork(dll2문맥)  State 4 → +0x131 LdrpSnapModule(dll2)
   큐 빔, WorkInProgress == 1 → 반환
 +0x1af2 LdrpPrepareModuleForExecution(exe) → (exe State 5 라면) +0x12f LdrpCondenseGraph → 셋 다 5 → 6, 합치기 없음
 +0x1bbb LdrpInitializeGraphRecurse … (2.7, run7·run8 [관찰])
```

- 채운 `?` (run22·23 그림):
  1. **일감을 누가 꺼내 올리나**: 책임자 스레드가 `Drain` 안에서 직접(`+0x187`), 또는 `TpPostWork` 로 깬 풀 일꾼 (`TppWorkpExecuteCallback +0x2e0` / `LdrpWorkCallback +0xb8`). `[정적]` — 실제 어느 쪽인지는 동적.
  2. **자식이 올라온 뒤 부모 카운터를 줄이고 snap 으로 잇는 코드**: `LdrpSignalModuleMapped +0x40 ~ +0x5e`. `[정적]`
  3. **`LdrpQueueWork` 가 돌아왔을 때 dll1 `State`**: 넣기만 하니 아직 0 (`< 2`). 그래서 exe 카운터는 그 자리에서 안 줄어든다. `[정적: 락 순서]`
  4. **올리기와 snap 의 연결**: 한 일감 = 둘 중 하나, `State` 로 갈림. `[정적]`
  5. **kernel32·kernelbase (병렬 켜기 앞)**: `LdrpMapAndSnapWork == 0` 이라 일꾼을 안 깨운다. 큐에 쌓인 일감(예: kernelbase snap, run22)은 `LdrpLoadDllInternal +0x21a Drain(1)` 이 혼자 비운다. kernel32 문맥은 `+0x30 == 0` 이라 자기 끝맺음에서 `LdrpSnapModule` 직접 (run22). `[정적: 코드상]`
  6. **그래프 정리**: 고리가 없으니 합치지 않는다. `[정적: 코드 모양]`
- 이 순서로 보면 **exe 의 snap 도 `+0x4c2` 직접이 아니라 일감(2)** 으로 갈 수 있다 `[추측: 일꾼 없을 때]`. 일꾼이 있으면 dll1 이 exe 고리 끝의 자기 몫 줄이기(`+0x474`) 전에 Mapped 가 될 수도 있어 `+0x4c2` 가 탈 수도 있다. 어느 쪽이든 snap 은 **딱 한 번** 이다 (마지막으로 줄이는 쪽이 시작).
- run8 `[관찰]` ("`+0x1bb6` 에서 dll1·dll2 가 이미 올라와 있었다") 과 어긋나지 않는다: 올리기는 전부 `+0x1ab1 Drain(1)` 이 돌아오기 전에 끝난다 (Drain(1) 은 큐가 비고 일꾼도 다 끝나야 돌아옴).

### 호출 관계

```
LdrpInitializeProcess
 ├─ (8)  +0x1a47 LdrpEnableParallelLoading                        [정적] run19
 │        └─ +0xe2  TpAllocWork(&LdrpMapAndSnapWork, LdrpWorkCallback)   스레드 수 > 1 · Detour 없음일 때   [정적]
 ├─ (9)  +0x1aa9 LdrpMapAndSnapDependency [test.exe]               [정적] run22
 │        └─ +0x34e LdrpLoadDependentModuleInternal [dll1 추정]    [정적] run23
 │             └─ +0x182c LdrpQueueWork [dll1 추정]                [정적]
 │                  └─ +0x82  TpPostWork                            일감 개체 ≠ 0 · 종료 중 아님   [정적]
 ├─ (10) +0x1ab1 LdrpDrainWorkQueue(1)                              [정적]
 │        ├─ +0x187 LdrpProcessWork [dll1 추정, 올리기]             큐에 일감이 있을 때마다   [정적]
 │        │    └─ +0x6a  LdrpMapDllSearchPath
 │        │         └─ ? (지도 이름 사슬) LdrpMapDllWithSectionHandle [dll1 추정]
 │        │              ├─ (1) +0x26c LdrpProcessMappedModule      [정적] run22
 │        │              │     └─ +0x1e2 LdrpSignalModuleMapped     [정적]
 │        │              │          └─ +0x5e LdrpQueueWork [test.exe snap 추정]   [정적]
 │        │              └─ (2) +0x1c8 LdrpMapAndSnapDependency [dll1 추정]   [정적] run22
 │        ├─ +0x187 LdrpProcessWork [test.exe 추정, snap]
 │        │    └─ +0x131 LdrpSnapModule                              [정적]
 │        └─ +0x199 NtWaitForSingleObject(LdrpWorkCompleteEvent)    큐가 비었고 일꾼이 일하는 중일 때   [정적]
 └─ (11) +0x1af2 LdrpPrepareModuleForExecution [test.exe]           [정적] run21
          └─ +0x12f LdrpCondenseGraph                               노드 State 5 일 때   [정적] run21
               └─ +0x26  LdrpCondenseGraphRecurse
                    └─ +0x13d LdrpCondenseGraphRecurse (재귀)  …  고리 없으면 +0xd8 LdrpMergeNodes 안 탐

(다른 스레드, 있다면) TppWorkpExecuteCallback
 └─ +0x2e0 LdrpProcessWork(일감, 0)      콜백이 LdrpWorkCallback 일 때 (아니면 +0x4d0 간접 → LdrpWorkCallback +0xb8)   [정적]
```

## 확인된 것

- (`[정적]`) 일감 = 로드 문맥, 큐 = `LdrpWorkQueue` (`ntdll+0x1d26b0`, 문맥 `+0x40` 고리), 잠금 = `LdrpWorkQueueLock` (`ntdll+0x1d2680`). `LdrpQueueWork` 는 **꼬리**에 넣고, 꺼내는 쪽은 **머리**에서 뺀다 (먼저 넣은 것부터).
- (`[정적]`) `LdrpQueueWork` 는 넣기만 한다. 일꾼을 깨우는 `+0x82 TpPostWork` 는 `LdrpMapAndSnapWork ≠ 0` + `ShutdownInProgress == 0` 일 때만. 병렬 로딩 여부는 `LdrpMapAndSnapWork` 가 0 이냐로 판단한다 (따로 된 깃발 없음).
- (`[정적]`) `LdrpMapAndSnapWork` 는 `LdrpEnableParallelLoading +0xe2 TpAllocWork` 가 채운다 (콜백 = `LdrpWorkCallback`). 스레드 수 ≤ 1 이거나 `LdrpDetourExist ≠ 0` 이면 안 채운다. → 병렬 켜기 앞(kernel32 준비)에는 일꾼을 안 깨운다.
- (`[정적]`) 꺼내는 쪽은 둘: 로드 책임자 스레드(`LdrpDrainWorkQueue` 안 `+0x187`, 직접 처리)와 풀 일꾼 (`TppWorkpExecuteCallback +0x2e0` 또는 `LdrpWorkCallback +0xb8`).
- (`[정적]`) `Drain(0)` = 다른 로드가 다 끝나길 기다려 책임자(`LoadOwner` `0x1000`)가 됨, `Drain(1)` = 책임자가 큐를 다 비우고 일꾼이 끝날 때까지 `LdrpWorkCompleteEvent` 를 기다림. 직접 처리 vs 기다림은 "큐에 일감이 있나" 하나로 갈린다.
- (`[정적]`) `LdrpProcessWork` 는 노드 `State` 로 한 일감을 "올리기(`State 0`, 기본 `+0x6a LdrpMapDllSearchPath`)" 또는 "snap(`State ≠ 0`, `+0x131 LdrpSnapModule`)" 중 하나로 처리한다. 끝에서 `WorkInProgress` 를 줄이고, 큐가 비었고 남은 값이 1 이면 `LdrpWorkCompleteEvent` 를 켠다.
- (`[정적]`) **부모 카운터를 줄이고 snap 으로 잇는 코드 = `LdrpSignalModuleMapped`** (`+0x40 ~ +0x5e`). 자식이 Mapped (`LdrpProcessMappedModule +0x1c2`) 가 된 직후, 락 안에서 부모마다 `ctx+0x6c` 를 1 줄이고, 0 이면 부모 `State = 4` + `LdrpQueueWork(부모 문맥)`.
- (`[정적]`) `+0x6c` 를 줄이는 곳은 셋 (`MapAndSnap +0x474`, `Internal +0x12f7`, `Signal +0x45`), 마지막으로 0 을 만드는 쪽이 부모 snap 을 시작한다.
- (`[정적]`) 그래프 정리 = 고리 합치기 (`LdrpCondenseGraphRecurse` 가 번호·`LowestLink`·스택으로 묶음을 찾고, 여럿이면 락 안에서 `LdrpMergeNodes`, 남은 노드는 `LdrpDestroyNode`). 고리가 없는 노드는 `State 5 → 6` 만.
- (`[정적: 코드 모양]`) test.exe → dll1 → dll2 는 고리가 없어 합치기가 안 일어난다.

## 정정

- run23 의 `[추측]` "**`LdrpQueueWork` 가 돌아온 뒤 자식 `State` 를 `+0x12df` 에서 읽으니, 그 자리에서 다 올렸으면 `State ≥ 2`, 미뤘으면 `< 2` → 병렬 켜기 앞뒤 차이가 여기 나타날 수 있다**" 는 **틀렸다.**
  - `LdrpQueueWork` 는 큐에 넣고 (조건 맞으면) 일꾼을 깨우기만 한다. 그 자리에서 올리지 않는다 `[정적]`.
  - 자식 `State = 2` 는 `LdrpProcessMappedModule` 이 `LdrpModuleDatatableLock` 독점 안에서 쓴다. `LdrpLoadDependentModuleInternal` 은 같은 락을 `+0x182c` 부터 `+0x12df ~ +0x12f7` 까지 쥐고 있으니, 그 사이에 다른 스레드가 자식을 Mapped 로 만들 수 없다 `[정적: 락 순서]`.
  - 그래서 새로 일감에 넣은 자식은 `+0x12df` 에서 늘 `State < 2` 이고, 부모 카운터는 그 자리에서 안 줄어든다. 병렬 켜기 앞뒤로 다르지 않다. 그 몫은 나중에 `LdrpSignalModuleMapped` 가 줄인다.
  - run23 문서 맨 위에 정정 안내 한 줄을 넣었다. flow.md 3장의 같은 `[추측]` 에도 (틀림 → run24) 를 붙였다.

## 아직 확인 필요

- **일꾼이 실제로 있나, 누가 `LdrpProcessWork` 를 도나, `Drain(1)` 이 기다리나** → 동적 **Q47**. 멈춤 (`ntdll+rva`, test.exe 만):
  - `ntdll+0xe572c` (`LdrpEnableParallelLoading` 입구) `r ecx` = `LoaderThreads`. `ntdll+0xe580e` (`+0xe2`, `TpAllocWork` ret) 뒤 `dq ntdll+0x1d2640 L1` = `LdrpMapAndSnapWork`. `TpAllocWork` 를 안 타면 `ntdll+0xe5809` 가 안 선다.
  - `ntdll+0xe24c9` (`LdrpQueueWork +0x7d`, `TpPostWork` call): 서면 일꾼을 깨우는 것. 입구 `ntdll+0xe244c` 만 서면 `LdrpMapAndSnapWork == 0` 이거나 종료 중.
  - `ntdll+0x17140` (`LdrpProcessWork` 입구): `~`, `k` (부른 곳이 `LdrpDrainWorkQueue+0x187` / `TppWorkpExecuteCallback+0x2e0` / `LdrpWorkCallback+0xb8` 중 무엇인지), `r dl`.
  - `ntdll+0xad400` (`LdrpDrainWorkQueue` 입구) `r ecx`, `ntdll+0xad594` (`+0x194`, `NtWaitForSingleObject` call): 서면 메인이 일꾼을 기다린 것.
  - `ntdll+0xe9ef0` (`LdrpWorkCallback` 입구): **안 서도 이상하지 않다**. 대신 `ntdll+0x19050` (`TppWorkpExecuteCallback +0x2e0`).
- **dll1·dll2 일감마다 어느 갈래인가, 부모 카운터** → 동적 **Q23**:
  - `ntdll+0x171a5` (`ProcessWork +0x65`, `LdrpMapDllSearchPath` call) vs `ntdll+0x1726c` (`+0x12c`, `LdrpSnapModule` call): 일감마다 `dS poi(@rbx+0x38)+0x58` (항목 `BaseDllName`) 와 함께.
  - `ntdll+0xe2420` (`LdrpSignalModuleMapped +0x40`) `dd @rcx+0x6c L1`, `ntdll+0xe2439` (`+0x59` `LdrpQueueWork` call): 서면 부모 snap 일감이 만들어지는 것.
  - exe 의 snap 이 `MapAndSnap +0x4c2`(`ntdll+0x2155d`, call `+0x4bd`) 로 가나 일감으로 가나.
  - `ntdll+0xed9f4` (`LdrpMergeNodes` 입구): 안 서는 게 정상일 것 (시스템 DLL 고리 포함).
- **주소 연결 `LdrpSnapModule`** (IAT 를 쓰는 곳, `State = 5` 를 쓰는 곳, `+0x5df` 갈래) → 정적 **Q50**.
- 정적으로 남은 것 (새 번호 없음): `LdrpDestroyNode` 본문, `LdrpMapDllSearchPath` → `LdrpMapDllWithSectionHandle` 사이 (run21 지도 이름 사슬만), `LdrpRetryQueue` 에 넣는 쪽, `Drain` caller 5곳 인자, `LdrpMinimalMapModule +0x296` 이 `LdrpMapAndSnapWork` 를 읽는 이유, `TpPostWork`·`TppWorkpExecuteCallback` 이 콜백을 어느 스레드에서 돌리나.
- 이름·뜻 못 정한 것: `LdrpProcessWork` 인자 2 의 뜻 (`[추측]` "센 적 없음"), `ctx+0x20` 비트 9·20 이름, `LdrpProcessMappedModule` 인자 2 비트 23, `LdrpDetourExist` 가 켜지는 조건 (`LdrpDetectDetour` 본문), 스위트 마스크 비트 16 의 뜻.
- Q14 ("dll1·dll2 를 올리는 쪽") 는 **일부**: 코드 길은 끝까지 이어졌다 (`MapAndSnap +0x34e` → `Internal +0x182c LdrpQueueWork` → `Drain +0x187`/일꾼 → `LdrpProcessWork +0x6a LdrpMapDllSearchPath` → … → `LdrpMapDllWithSectionHandle`). 어느 스레드가 어느 줄에서 올렸는지는 실행 값(`LoaderThreads`, `LdrpMapAndSnapWork`)에 달려 동적 (Q23·Q47).

## 디버거 메모 / 도구 메모

- 디버거는 안 썼다.
- 결과서 한 곳 고쳐 읽음: `LdrpCondenseGraphRecurse` 의 락 잡기를 결과서는 "`+0xbf` / `+0xc6`" 로 적었는데, `+0xbf` 는 `LEA RCX,LdrpModuleDatatableLock` 이고 call 은 `+0xc6`, ret 은 `+0xcb` 다 (`_calls.tsv` 137줄). 이 문서·flow.md 는 `+0xc6` / `+0xcb` 로 적었다.
- 결과서 질문 1 의 "`TpPostWork` 를 부르는 조건 (세 개 다 참일 때)" 는 목록에 둘만 있다. 세 번째는 `+0x10` 의 "문맥 상태가 음수가 아님" (아니면 큐에도 안 넣고 끝) 으로 읽었다.
- 주소로만 넘겨지는 함수(`LdrpWorkCallback`)는 지도에 안 나온다 — 이미 알려진 한계 (결과서). `refs` 로 찾았다.

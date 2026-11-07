# 67 pool-work-post — 정적 분석 결과

- 요청서: `_workspace/sast/67_pool-work-post.md` (Q137, run72 예정, PE_emulator 요청 23)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`, **새로 analyze 안 함**). pdb guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`). 커널 쪽은 이미 있는 `MiMapViewOfImageSection` `.asm`/`.c` 를 읽기만 함 (새 analyze 없음).
- Ghidra 잠금: 시작 전·export 전 `pgrep -fa 'ghidra.app.util.headless'` 두 번 확인 — 돌고 있는 작업 없음 (sast/66 은 안 돌고 있었음). 그대로 진행.
- 뽑은 함수 (새로): `^TppWorkPost$` depth 1 → 14개 (`TppWorkPost` + 부르는 13개: `NtAlertThreadByThreadId` · `NtReleaseWorkerFactoryWorker` · `NtTraceEvent` · `RtlAcquireSRWLockExclusive` · `RtlReleaseSRWLockExclusive` · `RtlpAcquireSRWLockExclusiveContended` · `RtlFreeHeap` · `TppAdjustRunningThreadGoalWithLock` · `TppCleanupGroupMemberDestroy` · `TppFreeWait` · `TppAlpcpFree` · `__security_check_cookie` · CFG 검사 함수). 로그 `ms/ghidra/ntdll.export.sast67a.stdout`.
- **요청서 밖으로 넓힌 것 (이유 있음)**
  - `LdrpAllocatePlaceHolder` · `LdrpFreeLoadContext` (depth 0, `ms/ghidra/ntdll.export.sast67b.stdout`) — 2번 "`ctx+0xb0 == 0` 이 정해지나" 를 보려고 지시서(ctx) 를 만드는 함수가 필요해서.
  - ntdll 전체 `objdump` 훑기 (`[reg+0xb0]` 쓰기 찾기) · 샘플 PE 헤더 3개 읽기 (임시 스크립트, 스크래치) · SSN (시스템 호출 번호) 은 ntdll 파일 스텁 바이트 (`MOV EAX,imm`) 에서 읽음.
- 있던 것 그대로 읽음 (다시 안 뽑음): `TpPostWork` · `LdrpQueueWork` · `LdrpCheckForRetryLoading` · `LdrpDrainWorkQueue` · `LdrpProcessWork` · `LdrpMapDllRetry` · `LdrpMinimalMapModule` · `LdrpWorkCallback` · `LdrpMapDllNtFileName` · `LdrpGetNtPathFromDosPath` · `TpAllocPoolInternal` · `LdrpDetectDetour` · 커널 `MiMapViewOfImageSection` · `MiSelectImageBase`.
- 버전 확인: 이번에 새로 만든 `_calls.tsv` 줄이 아니라 기존 줄로 대조 — `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` · `LdrInitializeThunk +0x9/+0xe → LdrpInitialize` · `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess` · `LdrpInitializeProcess +0x1a42/+0x1a47 → LdrpEnableParallelLoading` 모두 일치. `TppWorkPost` rva `0x39430`, 크기 1346B 요청서와 같음. → **일치.**
- 스크립트 문제: **없음.**
- 오프셋 대조: 이 결과서의 `+0x..` (call/ret, 점프 목적지 = 목적지 − 함수 시작) 약 250개를 `.asm` 줄과 스크립트로 다시 대조했다. 대조 중 **내 초안 오류 둘을 고쳤다**: `LdrpMapDllRetry +0xa3 JS` 목적지는 `+0xb4` (초안 `+0xb0`), `LdrpMapDllNtFileName +0xe8 JZ` 목적지는 `+0x10e` (초안 `+0xfe`).

## 한 줄 결론

- **`TppWorkPost` 는 "넣기만 하고 돌아온다."** 안에 `NtWait*`·`NtRemoveIoCompletion`·`NtWaitForWorkViaWorkerFactory` 가 **없다** `[정적]`. 기다림이 생길 수 있는 곳은 SRW 락이 **이미 잠겨 있을 때** 들어가는 `RtlpAcquireSRWLockExclusiveContended` (그 안 `NtWaitForAlertByThreadId`) 하나뿐이고, 첫 스레드 하나만 있는 에뮬레이터에선 안 탄다.
- **기본 대답 갈래 (풀 핸들 0 · 일꾼 0) 와 진짜 처음 넣기 (핸들 ≠ 0 · 일꾼 0) 는 같은 길을 탄다.** `TppWorkPost` 는 풀 핸들(`+0x38`) 을 **검사하지 않고** 그대로 `NtReleaseWorkerFactoryWorker` 에 넘기기만 한다. 반환값도 안 본다. 풀의 일꾼 수 칸 (`AvailableWorkerCount +0x1a0`) 은 아예 안 읽는다.
- **`NtReleaseWorkerFactoryWorker` 는 "처음 넣기" 에서 나온다 — 정확히 1번.** 풀을 만들 때 `QueueState` 의 `PendingReleaseCount` 가 0 으로 시작하고, 이 값이 0 인 첫 넣기가 1 로 올리면서 syscall 을 부른다. 둘째 넣기부터는 (일꾼이 안 가져가는 동안) 아예 큐에도 안 넣고 syscall 도 없다.
- **재시도 길은 `NtMapViewOfSection` 이 `0` 을 주면 안 탄다.** `0x40000003` 또는 `0x40000036` 일 때만 열린다. test.exe 에서 dll1·dll2 가 어느 값을 받는지는 **정적으로 안 닫힘 → 동적** (코드 조건은 닫힘: 기준 주소에 못 붙었을 때만).
- `0xc000022d` 는 `LdrpCheckForRetryLoading` 이 돌려주는 값이 **아니다.** 이 함수는 참/거짓(`AL`) 만 주고, `0xc000022d` 는 **부른 쪽** (`LdrpMinimalMapModule +0x2b6`, `LdrpMapDllNtFileName +0x22e`) 이 만든다.

## 질문별 답 [정적]

### 1. `TppWorkPost` (rva `0x39430`, 1346B)

근거 파일: `ms/disassemble/ntdll/TppWorkPost.asm` (+ `.c`), `TpPostWork.asm`, `LdrpQueueWork.asm`, `TpAllocPoolInternal.asm`, `TppAdjustRunningThreadGoalWithLock.asm`. 칸 이름은 PDB (`pdb_struct.py ntdll.dll _TP_WORK _TPP_CLEANUP_GROUP_MEMBER _TPP_WORK_STATE _TP_POOL _TPP_POOL_QUEUE_STATE _TPP_QUEUE _TP_TASK _TPP_BARRIER`).

**1-1. 인자 · 앞단**
- 호출 사슬: `LdrpQueueWork +0x76 MOV RCX,[LdrpMapAndSnapWork]` → `+0x7d/+0x82 TpPostWork(RCX=일감)` → `TpPostWork +0x3c JMP` (꼬리) → `TppWorkPost`. `TppWorkPost` 의 부르는 곳은 이 둘 말고 `TpSimpleTryPost` · `TppSingleTimerExpiration` (`.c` 맨 위 callers).
- 인자 = `RCX` = `_TP_WORK*` (`RSI` 에 보관). `LdrpQueueWork` 쪽 조건: `+0x6 ~ +0x10` 지시서 상태 `[ctx+0x28]` 이 음수가 아님 · `+0x59 CMP [LdrpMapAndSnapWork],0` ≠ 0 · `+0x70 CMP byte [PEB.Ldr+0x48],0` (`ShutdownInProgress`) = 0.
- `TpPostWork` 가 `TppRaiseInvalidParameter` (`+0x37` 꼬리) 로 보내는 조건: 일감 0 (`+0x0 / +0x3`) · `Flags +0xa8` 비트 16 `Released` (`+0xb` BT) 또는 비트 17 `CleanupGroupReleased` (`+0x11` BT) 켜짐 · `VFuncs +0x8` ≠ `TppWorkpCleanupGroupMemberVFuncs` (`+0x1e CMP`) · `ShutdownInProgress` ≠ 0 (`+0x31 CMP byte [RDX+0x48],0` → `+0x35 JZ +0x3c`). 로더 일감은 이 넷 다 통과 `[정적: 값은 sast/64 TpAllocWork]`.

**1-2. 본문 갈래 (call/ret 둘 다, 점프 목적지는 "목적지 − 함수 시작")**

| 구간 | 하는 일 | 근거 줄 |
|---|---|---|
| `+0x3f ~ +0xf6` | **배리어 세기**: 일감 `+0x38` (`CallbackBarrier.Ptr`, 하위 60비트 = 세기 · 상위 4비트 = 깃발) 을 `CMPXCHG` 로 +1. 깃발 최상위 비트 (63) 가 켜져 있고 세기가 0 으로 돌아오는 경우에만 배리어 락 (`+0x40`) 을 잡는 느린 길 (`+0x9d JZ` → `+0xea` 가 빠른 길). 빠른 길은 `+0xed CMPXCHG.LOCK [RSI+0x38]` 한 번 | `+0x3f`, `+0x43`, `+0x9d`, `+0xed` |
| `+0xfc ~ +0x132` | 느린 길에서 잡은 락이면: 기다리는 목록 (`+0x48 WaitList`) 을 비우고 `+0x10d RtlReleaseSRWLockExclusive` 하고 목록 한 칸마다 **`+0x127/+0x12c NtAlertThreadByThreadId(스레드 번호)`**. 빠른 길이면 `+0xff JZ` → `+0x134` 로 건너뜀 | `+0xff`, `+0x101`, `+0x127` |
| `+0x134 ~ +0x158` | **일감 상태**: `WorkState` (`+0xe8`) 를 `(옛값 + 2) & ~1` 로 `CMPXCHG` (`+0x14c`) = `Insertable` 비트 0 을 끄고 `PendingCallbackCount` +1. **옛 `Insertable` 이 0 이었으면 (`+0x158 JZ` → `+0x32a`) 여기서 끝** = 큐에 안 넣고 돌아감 (아래 C 열) | `+0x14c`, `+0x158` |
| `+0x15e ~ +0x17d` | 넣기로 정함: 일감 `Refcount` (`+0x0`) `ADD.LOCK 2`, `WorkOnBehalfTicket` (`+0x80`) ← `TEB+0x2b8` | `+0x15e`, `+0x176`, `+0x17d` |
| `+0x184 ~ +0x229` | **ETW 이벤트 갈래**: `KUSD +0x386` 바이트 (단 `PEB.SharedData +0x90` 이 0 이 아니고 그 가리키는 첫 값도 0 이 아니면 `SharedData+0x22c` 바이트를 대신 봄: `+0x199 ~ +0x1a2`, `+0x51e ~ +0x53e`) 가 0 이면 `+0x1a8 JZ` → `+0x229` 로 건너뜀. 0 이 아니면 **`+0x224/+0x229 NtTraceEvent(…, 0x20402, 0x28, 버퍼)`**. KUSD `+0x386` = 0 `[관찰, sast/57]` → 안 탐 | `+0x1a5`, `+0x1a8`, `+0x224` |
| `+0x229 ~ +0x24d` | 풀 고르기: `Pool` (`+0x90`) 이 0 이면 `TppPoolpGlobalPool` (`+0x239 JZ` → `+0x3e6`), `TppPoolpSerializedPool` 이면 우선순위를 1 로 (`+0x23f`). 우선순위 = `CallbackPriority` (`+0xc0`) → `ECX`, 노드 = `Task.NumaNode` (`+0xd0`) → `R8D` | `+0x229`, `+0x230`, `+0x24d` |
| `+0x254 ~ +0x279` · `+0x363 ~ +0x37d` · `+0x3e1` | 풀 `NodeStatus` (`+0x1ac`) 맞추기: **`-2` 이면 일감의 노드 번호로 채움** (`+0x26a JZ` → `+0x3e1` → `+0x36f CMPXCHG`), 같은 번호면 그대로, `-1` 이면 그대로, 다른 번호면 `-1` 로 | `+0x267`, `+0x26a`, `+0x363`, `+0x36f` |
| `+0x279 ~ +0x2e8` | **큐 고르고 락 잡기**: 큐 = `풀.TaskQueue[우선순위] + 노드*0x18` (`+0x287 MOV RAX,[RDI+RDX*8+0x10]`, `+0x28c LEA RBX,[RAX+RCX*8]`; RCX=노드*3), 락 = 큐 `+0x10` (`+0x299 LEA RBP,[RBX+0x10]`). `TEB+0x1850` 이 가리키는 8칸 표가 있으면 빈 칸에 락 주소를 적어 두고 (`+0x29d ~ +0x2c2`, 표가 0 이면 `+0x2a7 JZ` → `+0x2c5` 로 건너뜀), `+0x2c5 BTS.LOCK [RBP],0` — 이미 잠겨 있으면 (`+0x2cc JC` → `+0x3d4`) **`+0x3d7/+0x3dc RtlpAcquireSRWLockExclusiveContended`** | `+0x287`, `+0x28c`, `+0x2c5`, `+0x2cc`, `+0x3d7` |
| `+0x2d2 ~ +0x2e4` | 이중 연결 리스트 이웃 검사 (`Blink->Flink == 머리`). 틀리면 `+0x2e4 INT 0x29` (`ECX=3`, 즉시 중단) — **에뮬레이터 주의**: 큐 머리는 자기를 가리키게 만들어져 있어야 함 | `+0x2d2`, `+0x2d6`, `+0x2d9`, `+0x2e4` |
| `+0x3f2 ~ +0x403` | **꼬리에 붙이기**: 일감 `Task.ListEntry` (`+0xd8`, `R14`) 를 큐 꼬리로 (`+0x3f2/+0x3f8/+0x3fc/+0x3ff` 네 줄), 락 풀기 **`+0x403/+0x408 RtlReleaseSRWLockExclusive`** | `+0x3f2`, `+0x403` |
| `+0x408 ~ +0x43b` | 풀의 `LastProcCount` (`+0x1a8`) 가 현재 CPU 수 (`+0x1b8 SelectedCpuSetCount`, 0 이면 `KUSD +0x3c0 ActiveProcessorCount`) 와 **같으면 (`+0x424 JZ` → `+0x440`) 건너뜀**. 다르면 풀 락 (`+0x48`) 잡고 `+0x432 TppAdjustRunningThreadGoalWithLock` (그 안 `NtSetInformationWorkerFactory` 클래스 8 + `TppPoolUpdateNodeRelation`) | `+0x40d`, `+0x417`, `+0x41e`, `+0x424`, `+0x42a`, `+0x432`, `+0x43b` |
| `+0x440 ~ +0x47a` | 지금 스레드가 **이 풀의 일꾼이고 상태 3 이면** (`TEB.ThreadPoolData +0x1778` → `+0x461 CMP [..+0x80],3`) 4 로 바꾸고 `R8D=1` (= 풀 큐 상태에 "깨우기 필요" 안 올림) | `+0x44c`, `+0x461`, `+0x46a` |
| `+0x47a ~ +0x4b3` | **풀 `QueueState` (`+0x8`) 갱신** (`CMPXCHG` 되풀이, 되풀이 점프 `+0x4b3 JNZ` → `+0x487`): `QueueLength` (상위 4바이트) +1 하고, 하위 4바이트의 **상위 16비트 `PendingReleaseCount` 가 0 이고 (`+0x49e JZ` → `+0x4cd`) `R8D` 도 0 이면 (`+0x4cd/+0x4d0`) `PendingReleaseCount` 를 1 로 올리고 (`+0x4dd ADD EDX,0x10000`) "깨우기" 표시 (`+0x4e5 LEA ECX,[R8+1]`)**. 이미 ≠ 0 이면 표시 없음 | `+0x47e`, `+0x49e`, `+0x4a8`, `+0x4cd`, `+0x4dd`, `+0x4e5` |
| `+0x4b5 ~ +0x4f8` | 표시 있으면 (`+0x4b7 JNZ` → `+0x4ef`) **`+0x4ef MOV RCX,[RDI+0x38]` (풀 `WorkerFactory`) → `+0x4f3/+0x4f8 NtReleaseWorkerFactoryWorker`**, 돌아와 `+0x4b9` 로 | `+0x4b7`, `+0x4ef`, `+0x4f3` |
| `+0x4b9 ~ +0x4c8` | 일감 `Refcount` `XADD -1`. **옛 값이 1 이 아니면 (`+0x4c2 JNZ` → `+0x32a`) 그냥 끝.** 1 이면 (`+0x4c8 JMP` → `+0x2ea`) 해제 갈래 | `+0x4b9`, `+0x4be`, `+0x4c2`, `+0x4c8` |
| `+0x2ea ~ +0x3cf` · `+0x507 ~ +0x519` | 해제 갈래: `[일감+0x8]` (`VFuncs`) 의 `Free` 가 `TppSimplepFree` 면 `+0x304 TppCleanupGroupMemberDestroy` + `+0x325 RtlFreeHeap`, `TppFreeWait` 면 `+0x50a`, `TppAlpcpFree` 면 `+0x514`, 아니면 `+0x3ca` 간접(CFG) 호출 | `+0x2f5`, `+0x2fb`, `+0x3b1`, `+0x3c4`, `+0x3ca` |
| `+0x32a ~ +0x35a` | 끝: `+0x335 __security_check_cookie`, `+0x35a RET` | |

**1-3. 풀·일감의 "일꾼 0 · 핸들 0" 은 어디서 갈리나**
- **안 갈린다.** 위 표 어디에도 `풀+0x38` (핸들) 의 값 검사·`풀+0x1a0 AvailableWorkerCount`·`풀+0x1a4` 읽기가 없다. 핸들은 `+0x4ef` 에서 `RCX` 로 꺼내 `+0x4f3` 에 넘길 뿐이다.
- 일꾼이 있는지 없는지는 `QueueState.PendingReleaseCount` 가 대신 말해 준다 (일꾼이 일감을 가져가면 줄이는 값). 일꾼이 없는 에뮬레이터에선 이 값이 한 번 1 이 되면 그대로다.

**1-4. syscall 표** (SSN = 이 빌드 ntdll 스텁의 `MOV EAX,imm`)

| # | 자리 (call/ret) | syscall (SSN) | A. 기본 대답 갈래 (핸들 0 · 일꾼 0) 첫 넣기 | B. 진짜 처음 넣기 (핸들 ≠ 0 · 일꾼 0) | C. 둘째 이후 넣기 |
|---|---|---|---|---|---|
| 1 | `+0x4f3/+0x4f8` | **`NtReleaseWorkerFactoryWorker`** (`0x17e`), 인자 `RCX` = 풀 `+0x38` 하나 | **1번**, 핸들 0 으로 | **1번**, 진짜 핸들로 | **0번** (일감이 큐에 이미 있음: `Insertable` 0 → `+0x158` 에서 끝) |
| — | 조건부 ① `+0x224/+0x229` | `NtTraceEvent` (`0x5e`) | 안 탐 (KUSD `+0x386` = 0 `[관찰]`) | 같음 | 안 탐 |
| — | 조건부 ② `+0x127/+0x12c` | `NtAlertThreadByThreadId` (`0x71`) | 안 탐 (배리어 느린 길 = 누가 일감을 기다리는 중일 때만) | 같음 | 안 탐 |
| — | 조건부 ③ `+0x432` → `TppAdjustRunningThreadGoalWithLock +0x9b/+0xa0`, `+0xa3/+0xa8` | `NtSetInformationWorkerFactory` (`0x1b1`, 클래스 8) + `TppPoolUpdateNodeRelation` 안 `NtQuerySystemInformationEx` (`0x16e`) | 안 탐 (`LastProcCount` = CPU 수: `TpAllocPoolInternal +0x237` 이 만들 때 채움 → `+0x424 JZ`) | 같음 | 안 탐 |
| — | 조건부 ④ 락이 이미 잠겨 있을 때 `+0x3d7` / `+0x4fd` → `RtlpAcquireSRWLockExclusiveContended +0x146/+0x14b` | `NtWaitForAlertByThreadId` (`0x1e3`) | 안 탐 (다른 스레드가 락을 쥐고 있지 않음) | 같음 | 같음 |
| — | 조건부 ⑤ `Refcount` 가 0 이 될 때 (`+0x4c8`) `+0x304`/`+0x50a`/`+0x514`/`+0x3ca` | `NtClose` 등 (해제 쪽, 이름만) | 안 탐 (`Refcount` 1 → 3 → 2) | 같음 | 같음 |

- **합계: 첫 넣기 = syscall 1개 (`NtReleaseWorkerFactoryWorker`), 이후 넣기 = 0개.** 위 조건부 다섯은 test.exe 초기화에선 안 탄다 `[정적 조건 + 관찰 KUSD]`.
- 어느 syscall 도 반환값을 안 본다: `+0x4f8 JMP +0x4b9`, `+0x12c` 다음 `MOV RBX,RDI`, `+0x229` 다음 `MOV RDI,[RSI+0x90]`.

**1-5. 기다림이 이 함수 안에 있나**
- **없다.** `NtWaitForSingleObject`·`NtWaitForMultipleObjects`·`NtRemoveIoCompletion`·`NtWaitForWorkViaWorkerFactory` 호출이 `TppWorkPost` `_calls.tsv` 17줄에도, 깊이 1 에도 없다.
- 유일한 막힘 가능성: 락이 이미 잠겨 있을 때의 `RtlpAcquireSRWLockExclusiveContended` (`_calls.tsv`: `+0x146/+0x14b NtWaitForAlertByThreadId`). 그래서 **에뮬레이터는 큐 락 (`큐+0x10`)·풀 락 (`풀+0x48`)·배리어 락 (`일감+0x40`) 칸을 0 으로 둬야** 한다 (풀을 만들 때 `TpAllocPoolInternal +0x17d MOV [큐+0x10],0` 으로 0).

**1-6. `+0x3ca` 간접(CFG) 호출**
- 대상 = `[[일감+0x8]]` = `VFuncs->Free` (`+0x2ea MOV RAX,[RSI+0x8]` · `+0x2f5 MOV RAX,[RAX]`). 일감 개체 `VFuncs` = `TppWorkpCleanupGroupMemberVFuncs` (rva `0x173d30`) 이고 그 첫 칸 값은 PE 파일 바이트에서 **`0x18007fb80` = `TppWorkpFree`** (rva `0x7fb80`, 57B) `[정적: 파일 바이트]`.
- `+0x2fb JNZ` (`TppSimplepFree` 아님) → `+0x3a7` 에서 `TppFreeWait` · `TppAlpcpFree` 와도 안 같음 → `+0x3ca` 로 옴. 즉 **일감 종류에서 `+0x3ca` 대상은 `TppWorkpFree`.** 단 이 갈래는 `+0x4b9 XADD` 옛 값이 1 일 때만이라 (일감 `Refcount` 가 넣는 동안 1 → 3 → 2) 로더 일감에선 안 탄다.

**1-7. 쓰는 칸 (첫 넣기 한 번)**

| 개체 | 칸 | 변화 | 줄 |
|---|---|---|---|
| 일감 `_TP_WORK` | `+0x38 CallbackBarrier.Ptr` | 세기 +1 (모든 넣기마다) | `+0xed` |
| | `+0xe8 WorkState` | `Insertable` 1 → 0 · `PendingCallbackCount` +1 (모든 넣기마다 +1, `Insertable` 끄는 건 첫 번째만) | `+0x14c` |
| | `+0x0 Refcount` | +2 하고 −1 → 순증 +1 (큐에 넣을 때만) | `+0x15e`, `+0x4b9` |
| | `+0x80 WorkOnBehalfTicket` | ← `TEB+0x2b8` | `+0x17d` |
| | `+0xd8 Task.ListEntry` | 큐 꼬리에 연결 (`Flink`·`Blink`) | `+0x3f2 ~ +0x3ff` |
| 풀 `_TP_POOL` | `+0x1ac NodeStatus` | `0xfffffffe` → 일감의 노드 번호 (이 VM 은 0 `[추측: 노드 1개]`) | `+0x36f` |
| | `+0x8 QueueState` | `QueueLength` +1, `PendingReleaseCount` 0 → 1 (이 VM: 만들 때 하위 16비트 `RunningThreadGoal` = `ActiveProcessorCount` = 2 → 첫 넣기 뒤 `0x0000000100010002`) | `+0x4a8` |
| 큐 `_TPP_QUEUE` (`TaskQueue[1][노드 0]`) | `+0x0 Queue.Flink` · `+0x8 Queue.Blink` | 머리 ↔ 일감 `+0xd8` | `+0x3f2 ~ +0x3ff` |
| | `+0x10 Lock` | 잡았다 풂 (비트 0) | `+0x2c5`, `+0x403` |
| 스레드 `TEB` | `+0x1850 SchedulerSharedDataSlot` 가 가리키는 8칸 표 | 표가 있을 때만 빈 칸에 락 주소 (SRW 락 인라인) `[추측: 용도]` | `+0x29d ~ +0x2c2` |

- 큐는 `TpAllocPoolInternal +0x16d ~ +0x190` 에서 `Flink = Blink = 자기`, 락 = 0 으로 만들어진다 (`+0x17d`, `+0x181`, `+0x185`). 풀의 `QueueState` 는 `+0x263 ~ +0x26f` (하위 16비트만, 나머지 0), `NodeStatus` 는 `+0x253` (`0xfffffffe`), `LastProcCount` 는 `+0x237`.

닫힘 여부: **정적으로 닫힘.** 동적으로 한 번 읽으면 좋은 값 (닫힘에 필요하진 않음): KUSD `+0x386` · `TEB+0x1850` · 첫 넣기 뒤 `QueueState`.

### 2. 일감 ≠ 0 일 때 재시도 길

근거 파일: `LdrpCheckForRetryLoading.asm`, `LdrpMinimalMapModule.asm`, `LdrpMapDllNtFileName.asm`, `LdrpDrainWorkQueue.asm`, `LdrpProcessWork.asm`, `LdrpMapDllRetry.asm`, `LdrpGetNtPathFromDosPath.asm`, `LdrpAllocatePlaceHolder.asm`, `_map/LdrInitializeThunk_edges.tsv`.

**2-1. `LdrpCheckForRetryLoading` (rva `0xdf18`, 494B) — 이름이 헷갈린다: "재시도 길에 넣어야 하나" 를 정하고 넣는다**
- 인자: `RCX` = 지시서 (`LDRP_LOAD_CONTEXT`, 이하 ctx), `DL` = "없으면 새로 등록" 깃발. 돌려주는 값: `AL` (`BL`) 참/거짓. **상태 코드가 아니다.**
- 순서 (오프셋 `+0x..`):
  1. `+0x24 CMP [ctx+0xb0],0` ≠ 0 이면 거짓으로 끝 (`+0x2b JNZ` → `+0x195`).
  2. `+0x31 TEST [ctx+0x20],0x4100000` — 비트 20 (`0x100000` = "이미 재시도 줄에 넣음") 또는 비트 26 (`0x4000000`) 이 켜져 있으면 거짓으로 끝 (`+0x38 JNZ` → `+0x195`). 비트 26 을 켜는 곳은 `LdrpMapDllPatchImage +0x70` 하나 (objdump 훑기) = 핫패치 이미지, test.exe 길 아님.
  3. `+0x3e R15 = [ctx+0x38]` (항목 `LDR_DATA_TABLE_ENTRY`), `+0x49/+0x4e RtlEnterCriticalSection(LdrpWorkQueueLock)`.
  4. 트리 **`LdrpRetryingModuleIndex`** (rva `0x1d25b0`, 루트 8B + `+0x8` 에 `Encoded` 비트) 에서 항목의 `BaseNameHashValue` (`+0x108`, `+0x7e SUB`) 로 찾고 같으면 `FullDllName` (`+0x48/+0x50`) 을 대소문자 무시로 비교 (`+0xa9/+0xae RtlCompareUnicodeStrings`, 마지막 인자 `[RSP+0x20]=1`). 이 트리의 노드는 항목의 `+0xe0` (`MappingInfoIndexNode`, PDB).
  5. **같은 이름이 트리에 있으면** → `+0x153 BTS [ctx+0x20],0x14` (비트 20 켬) · ctx `+0x40` 을 `LdrpRetryQueue` (rva `0x1d2630`) 꼬리에 붙임 (`+0x158 ~ +0x182`, 이웃 검사 실패면 `+0x176 INT 0x29`) · `BL = 1` (`+0x16a`) → 참.
  6. **없으면**: `DL == 0` (`+0xd9 TEST R12B,R12B` · `+0xdc JZ` → `+0x189`) 이면 락 풀고 거짓. `DL != 0` 이면 `+0xfb LdrpCompareModuleName` 로 자리를 찾아 **`+0x14e/+0x153 RtlRbInsertNodeEx(&LdrpRetryingModuleIndex, 부모, 방향, 항목+0xe0)`** 로 트리에 넣고 **5번과 같은 길(비트 20, 재시도 줄에 붙임)로 합쳐서 참.**
  7. `+0x190/+0x195 RtlLeaveCriticalSection` 후 반환.
- syscall: **없음** (임계 구역만. 막히면 안쪽 대기가 있을 수 있는 공용 부품).

**2-2. `0xc000022d` 는 누가 만드나 / 이름 후보는**
- `LdrpMinimalMapModule +0x2a9/+0x2ae` 에서 `LdrpCheckForRetryLoading(ctx, DL=1)` → `AL ≠ 0` 이면 **`+0x2b6 MOV EBX,0xc000022d`**, `+0x2bb JMP +0x1b0`. 참이 아니면 (`+0x2b0 JZ` → `+0x384`) `R14B` 면 `0xc0000018`, 아니면 받은 값 그대로 (run38 과 같음).
- `LdrpMapDllNtFileName +0x46/+0x4b` 에서 `LdrpCheckForRetryLoading(ctx, DL=0)` (`+0x2b XOR EDX,EDX`) → `AL ≠ 0` 이면 **파일을 열기도 전에 `+0x22e MOV EAX,0xc000022d`** 로 돌아간다 (`+0x4d JNZ` → `+0x22e`). 이 호출은 **모든 DLL 이름 열기 앞에서 불린다** — 트리가 비어 있으면 임계 구역만 들어갔다 나오고 거짓 (syscall 0).
- 이름: `0xc000022d` = SDK 기억으로 `STATUS_RETRY` `[추측: 이름]`. 이 숫자 자체는 `.asm` 의 `MOV` 즉시값이다.
- 호출 쪽이 이 값을 어떻게 다루나: `LdrpProcessWork +0x74 CMP EAX,0xc000022d` · `+0x79 JZ` → `+0x15d` (오류 기록 없이 끝, 지시서 상태 `[ctx+0x28]` 에도 안 씀: `+0x137/+0x13b` 는 건너뜀).

**2-3. 재시도를 일으키는 첫 시도 — syscall 표** (`LdrpProcessWork → LdrpMapDllSearchPath → … → LdrpMapDllNtFileName`; 맵 결과가 `0x40000003` 또는 `0x40000036` 일 때)

| # | 자리 (call/ret) | syscall (SSN) | 비고 |
|---|---|---|---|
| 1 | `LdrpMapDllNtFileName +0x132/+0x137` | `NtOpenFile` (`0x33`) | 접근 `0x100021`, 공유 5, 옵션 `0x60` (`+0x129`, `+0x121`, `+0x111`). 실패 시 `0xc0000034` → `0xc0000135` 등 (`+0x2cc ~`) |
| — | `+0x109/+0x10e` | `NtSystemDebugControl` (`0x1d0`) | **조건부**: `PEB.NtGlobalFlag` (`+0xbc`) & `0x40000` 일 때만 (`+0xde TEST`, `+0xe8 JZ` → `+0x10e`) |
| 2 | `+0x18f/+0x194` | `NtCreateSection` (`0x4a`) | 이미지 섹션 (`+0x14e MOV R13D,0x1000000`; `UseWOW64 ≠ 0` 이면 `+0x4a0 ~ +0x4ac` 에서 ctx 플래그 `0x800` 이 꺼진 경우 `0x1100000` 로 바뀜) |
| 3 | `LdrpMapDllWithSectionHandle +0x49/+0x4e` → `LdrpMinimalMapModule +0x174/+0x179` | `NtMapViewOfSection` (`0x28`) | 결과 `0x40000003`/`0x40000036` 이 이 길의 문 |
| — | `LdrpCheckForRetryLoading` (2-1) | (없음) | `AL = 1` → `EBX = 0xc000022d` |
| 4 | `LdrpMinimalMapModule +0x2d4/+0x2d9` | `NtUnmapViewOfSection` (`0x2a`) | `+0x1b7 JNZ` → `+0x2c0` (`DllBase ≠ 0`), `+0x2c0 TEST EBX` · `+0x2c2 JS` (음수) → `+0x2d0 OR RCX,-1` (자기 프로세스) → 부른 뒤 `+0x2d9 [항목+0x30] = 0` |
| 5·6 | `LdrpMapDllNtFileName +0x29b` , `+0x2a4` | `NtClose` (`0xf`) ×2 | 섹션 핸들, 파일 핸들. `+0x28e/+0x291 TEST R15,R15` 에서 `R15 = [ctx+0xb0] = 0` 이라 닫는 쪽 (`≠ 0` 이면 `+0x434/+0x43f` 에서 ctx `+0xb8`·`+0x18` 에 보관) |

- **첫 시도 합계 = 6개** (조건부 `NtSystemDebugControl` 제외). 돌아오는 값 = `0xc000022d`.
- 이 값을 받은 `LdrpProcessWork` 는 오류 처리 없이 끝에 가서 (DL 이 0 이면 `+0x171 ~ +0x1c3`) `LdrpWorkInProgress--` 하고, 큐가 비었고 센 값이 1 이면 **`NtSetEvent(LdrpWorkCompleteEvent)`** (`+0x1c3` 꼬리, SSN `0xe`) 를 한 번 부른다 (재시도와 상관없이 일감 하나 끝날 때마다 같음).

**2-4. `LdrpDrainWorkQueue +0xeb ~ +0x134` — 재시도 줄을 일감 큐로 되돌린다** (`LdrpDrainWorkQueue(1)` 일 때만)
- 문 조건: 일감 큐가 비었고 (`+0x8a`) `LdrpWorkInProgress == ESI(1)` (`+0x90/+0x96`) → 잠금 풀고 `+0xb4 JNZ` → `+0xeb` (`ESI ≠ 0`).
- `+0xeb CMP [LdrpRetryQueue],R12` 같으면(= 재시도 줄이 비었음) `+0xf2 JZ` → `+0xb6` 로 그냥 끝 (책임자 비트 `TEB+0x17ee |= 0x1000`, `+0xd3`).
- 안 비었으면: 잠금 (`+0xf4/+0xfb`) → **줄 전체를 `LdrpWorkQueue` 머리 뒤로 옮김** (`+0x100 ~ +0x126`: 첫 노드 `Blink` ← 일감 큐 머리, `LdrpWorkQueue` ← 첫 노드, 끝 노드 `Flink` ← 일감 큐 머리, `LdrpWorkQueue+0x8` ← 끝 노드) → `LdrpRetryQueue` 를 빈 줄로 되돌림 (`+0x12d`, `+0x134`) → **`LdrpRetryingModuleIndex` 16바이트를 0 으로 지움** (`+0x13b MOVDQU`) → 잠금 풂 (`+0x143/+0x148`) → `+0x14b JMP +0x42` (처음으로).
- 그 다음 바퀴에서 옮겨진 ctx 가 하나씩 빠져 `LdrpProcessWork(ctx, DIL)` (`+0x182/+0x187`) 로 간다. syscall: **없음** (임계 구역만).

**2-5. `LdrpProcessWork +0x149/+0x14e → LdrpMapDllRetry` — 둘째 시도 syscall 표**
- 가르는 조건 (`LdrpProcessWork`): 지시서 상태가 음수 아님 (`+0x20/+0x23`) · DDAG 상태 (`항목+0x98 → +0x38 State`) = 0 (`+0x34/+0x38 JNZ` → `+0x129 LdrpSnapModule`; 0 이면 맵 갈래 `[추측: 0 = 자리표시자]`) · `LoadReason` (`항목+0x10c`) ≠ 9 (`+0x3e`, 9 면 `+0x153 LdrpMapDllPatchImage`) · **ctx `+0x20` 비트 20 켜짐** (`+0x51 BT`, `+0x55 JC` → `+0x149`) 이 `LdrpMapDllRetry`. (그다음 비트 9 → `+0x13f LdrpMapDllFullPath`, 아니면 `+0x65 LdrpMapDllSearchPath`).
- `LdrpMapDllRetry` (rva `0xebefc`, 248B) 순서:

| 자리 (call/ret) | 하는 일 | syscall |
|---|---|---|
| `+0x40/+0x45 memset` | 지역 구조체 `0x110`B 를 0 으로 (`+0x3a MOV R8D,0x110`) | 없음 |
| `+0x81/+0x86 LdrpFindExistingModule` | 항목의 `BaseDllName` (`+0x58`)·`FullDllName` (`+0x48`)·해시 (`+0x108`)·ctx 플래그로 **이미 올라온 같은 모듈을 찾음** (SRW 락 + `LdrpFindLoadedDllByNameLockHeld` 두 번, `_map` 확인) | 없음 |
| `+0x8d TEST RDX,RDX` · `+0x90 JNZ` → `+0xef` | 찾았으면 → `+0xf2/+0xf7 LdrpLoadContextReplaceModule(ctx)` (새로 안 맵; 안에서 `+0x10a/+0x10f LdrpQueueWork` 를 다시 부름 = **또 `TpPostWork`**) | 없음 (`LdrpQueueWork` 의 `TppWorkPost` 는 1번 표) |
| `+0x9a/+0x9f LdrpGetNtPathFromDosPath` | `FullDllName` 을 NT 경로로 (`+0x6c RtlpDosPathNameToRelativeNtPathName`) 바꾸고 **`+0xee/+0xf3 NtQueryAttributesFile`** 로 파일 확인. 이 함수의 반환값 = 이 syscall 의 상태 (`+0x73 JS` → `+0xf3`) | **`NtQueryAttributesFile` (`0x3d`) ×1** |
| `+0xa3 JS` → `+0xb4` | 음수면 `LdrpMapDllNtFileName` 건너뜀 | |
| `+0xad/+0xb2 LdrpMapDllNtFileName(ctx, NT 경로)` | 첫 시도와 같은 함수. 이번엔 ctx 비트 20 이 켜져 있어 `LdrpCheckForRetryLoading` 이 **`+0x31` 에서 바로 거짓** → 정상 길 | `NtOpenFile` · `NtCreateSection` · `NtMapViewOfSection` · `NtClose` ×2 (아래) |
| `+0xc3/+0xc8` | 지역 NT 경로 문자열 버퍼 해제 (힙 쪽) | 없음 |

- 둘째 시도 syscall (맵 결과가 또 `0x40000003` 이라고 할 때): `NtQueryAttributesFile` 1 + `NtOpenFile` 1 + `NtCreateSection` 1 + `NtMapViewOfSection` 1 + (`LdrpMinimalMapModule +0x2a9` 재시도 검사는 비트 20 때문에 거짓 → `+0x2b0 JZ` → `+0x384` 로 → 그대로 진행; `LdrpCompleteMapModule +0x31b LdrpRelocateImage` 가 **`0x40000003` 일 때만** 재배치 = `NtQueryVirtualMemory` · `LdrpSetProtection`(`NtProtectVirtualMemory`) — 횟수는 run38/Q126(5) 몫, 이름만) + `NtClose` ×2.
- **재시도는 경로 찾기 (`LdrpMapDllSearchPath`: 검색·KnownDlls 조회 등) 를 다시 하지 않는다.** 이미 정해진 `FullDllName` 으로 `NtQueryAttributesFile` 한 번 하고 바로 연다 `[정적]`.

**2-6. test.exe 에서 이 길을 타나 — 조건별로 정해지는 것 / 실행 때 정해지는 것**

| 조건 | 어디서 | 정해지는 때 |
|---|---|---|
| `LdrpMapAndSnapWork ≠ 0` | `LdrpMinimalMapModule +0x296` | **정해짐 `[정적]`**: 병렬 켜기 (`LdrpInitializeProcess +0x1a42/+0x1a47`) 이후 · `TpAllocWork` 성공 · 스레드 수 > 1 (`LoaderThreads=0` 이면 `EBX=4`, sast/64) · `LdrpDetourExist` = 0. kernel32 올라올 때는 0 (run24) |
| `ctx+0xb0 == 0` | `LdrpMinimalMapModule +0x288`, `LdrpCheckForRetryLoading +0x24` | **정해짐 `[정적]`: 항상 0.** ctx 는 `LdrpAllocatePlaceHolder +0x55 RtlAllocateHeap` 로 `NtdllBaseTag+0x40000 \| 8` (`8` = 0 채움) 힙 블록 (크기 이름 길이 `+0xd2`, `+0x4e`) 에서 만들어지고 `+0xb0` 은 안 쓴다. ntdll 전체에서 `[레지스터+0xb0]` 에 쓰는 명령을 `objdump` 로 훑은 결과 ctx 의 `+0xb0` 에 쓰는 줄은 **없다** (나온 것은 전부 `LDR_DATA_TABLE_ENTRY.LoadContext` (PDB `+0xb0`)·일감 `AllocCaller`·힙·`CONTEXT` 등 다른 구조체). 읽는 곳만 5곳: `LdrpCheckForRetryLoading +0x24`, `LdrpMinimalMapModule +0x288`, `LdrpMapDllNtFileName +0x1b`, `LdrpMapDllSearchPath +0x158`, `LdrpMapDllFullPath +0x89`. (한계: `lea` 뒤 쓰기·`rep stos` 류는 못 잡음 `[정적, 훑기 한계]`) |
| ctx `+0x20` 비트 20·26 꺼짐 (첫 시도) | `LdrpCheckForRetryLoading +0x31` | 첫 시도는 비트 20 이 꺼진 새 ctx (0 채움 + `LdrpAllocatePlaceHolder +0x7c BTS R15D,0xf` 로 비트 15만 켬) → **통과.** 비트 26 은 test.exe 길에서 안 켜짐 |
| `NtMapViewOfSection` 결과가 `0x40000003` 또는 `0x40000036` | `LdrpMinimalMapModule +0x192 ~ +0x1aa` | **실행 때** (3번) |

- 정리: **재시도 길로 들어가는 문은 오로지 "dll1·dll2 맵 결과가 0 이 아닌 경고 코드" 하나**다. 나머지 조건은 test.exe 에서 이미 참으로 정해져 있다.
- 첫 시도에서 이 문이 안 열리면 (맵 결과 0): `LdrpCheckForRetryLoading` 은 `LdrpMapDllNtFileName +0x46` 에서 DL=0 으로 한 번 불려 **빈 트리라서 거짓**, 재시도 줄은 비어 있어 `LdrpDrainWorkQueue +0xeb` 에서 `JZ`.
닫힘 여부: **코드 쪽은 정적으로 닫힘 / test.exe 가 타는지는 동적 필요** (3번과 같은 값).

### 3. dll1·dll2 이미지 맵 결과 — `0` 인가 `0x40000003` 인가

**3-1. 샘플 PE 헤더** (`sample/bin/` 과 `ms/bin/` 파일이 `md5sum` 으로 같음. 임시 스크립트 `struct` 로 읽음 `[정적+샘플]`)

| 파일 | `Characteristics` | `ImageBase` | `SizeOfImage` | `DllCharacteristics` | 재배치 디렉터리 | 진입점 rva |
|---|---|---|---|---|---|---|
| `test.exe` | `0x22` (LAA · `RELOCS_STRIPPED` 꺼짐) | `0x140000000` | `0x8c000` | `0x8160` (`DYNAMIC_BASE` · `HIGH_ENTROPY_VA` · `NX_COMPAT` · `TERMINAL_SERVER_AWARE`) | rva `0x8b000` · 크기 `0x810` | `0x1d40` |
| `dll1.dll` | `0x2022` (DLL · LAA · `RELOCS_STRIPPED` 꺼짐) | **`0x180000000`** | `0x8c000` | `0x160` (`DYNAMIC_BASE` · `HIGH_ENTROPY_VA` · `NX_COMPAT`) | rva `0x8b000` · 크기 `0x800` | `0x1d60` |
| `dll2.dll` | `0x2022` | **`0x180000000`** | `0x8c000` | `0x160` | rva `0x8b000` · 크기 `0x804` | `0x1d50` |

- 셋 다 `GUARD_CF` (`0x4000`) 는 꺼져 있고 로드 구성 디렉터리는 있다 (`0x140`B).
- **dll1 과 dll2 의 헤더 `ImageBase` 가 같다 (`0x180000000`).** 한 프로세스에서 둘이 모두 헤더 기준 주소에 붙을 수는 없다. 그러나 둘 다 `DYNAMIC_BASE` + 재배치 있음이라 커널이 이미지 파일마다 **자기 기준 주소** 를 따로 골라 준다 (아래).

**3-2. 커널이 `0x40000003` 을 만드는 조건** (`MiMapViewOfImageSection`, rva `0x9794ec`, `.asm` 읽기만)
- 상태는 **`+0xc0a MOV [RSP+0x60],0x40000003`** 한 자리에서 정해진다. 들어오는 점프는 `+0x42e JNZ` 하나 (`+0x412 ~ +0x42a`: **고른 시작 위치 − 섹션 오프셋 (`SUB RCX,RAX`) 을 `[RBP-0x60]` 와 `CMP`**). `[RBP-0x60]` = `+0xa8 MOV RDI,[R11+0x20]` → `+0xb0 MOV [RBP-0x60],RDI` = PDB `_SEGMENT.BasedAddress` (`+0x20`).
- 즉 **`0x40000003` ⇔ 실제로 붙은 시작 주소 ≠ 섹션의 `BasedAddress`.** 같으면 상태는 `0` 그대로.
- 시작 위치를 고르는 길:
  - 호출자가 기준 주소를 **NULL 로 준 보통 경우** (`LdrpMinimalMapModule` 이 주는 `&항목.DllBase` 가 처음 0): `+0x3d2 MOV EAX,[R12+0x3c]` · `+0x3d7 TEST AL,1` · `+0x3d9 JZ` → `+0x691`. 거기서 섹션 기준 주소부터 `+0x6c8/+0x6cd MiIsVaRangeAvailable` → **비어 있으면 (`+0x6d2 JNZ` → `+0x412`) 기준 주소 그대로 붙어 상태 `0`.** 비어 있지 않으면 (`+0x6d8 JMP` → `+0x932`) 다른 자리를 고르는 갈래 (`MiSelectUserAddress`, 디컴파일 기준) → 위 비교가 달라져 `0x40000003`.
  - 호출자가 주소를 정해 준 경우 (`+0x3d7` 에서 비트 켜짐): `+0x405/+0x40a MiIsVaRangeAvailable`, 비어 있지 않으면 `+0x40c JZ` → `+0xe72 MOV ESI,0xc0000018`.
- `0x40000003` 이 되려면 호출자가 `AllocationType` 에 `0x800000` 을 줘야 한다 (`+0x68 SHR EDX,0x16` · `+0x6f AND EDX,2` → 비트 1, `+0xc12 TEST BL,2`; 없으면 `+0xc15 JZ` → `+0x434` 쪽 `else`). `LdrpMinimalMapModule` 은 기본 `0x800000` 을 준다 (run38).
- `0x40000036` 은 `+0xc1b TEST BL,8` · `+0xc1e JNZ` → `+0xc79` 를 거쳐 `+0xc82 MOV [RSP+0x60],0x40000036` 로 간다 (Q126(6) 몫, 이름만).
- **섹션 기준 주소는 섹션을 만들 때** 정해진다 (`MiCreateNewSection → MiRelocateImage +0x2e6/+0x2eb → MiSelectImageBase`, rva `0x972de0`):
  - `MiSelectImageBase +0xd6 TEST DL,R10B` · `+0xd9 JNZ` → `+0x2b6`: 넷째 인자 비트 0 이 켜지면 **헤더 `ImageBase` 그대로** (`+0xcb MOV R13,[R14+0x20]` 값을 `+0x2ba MOV RDI,R13` 로 `*출력` 에). 이 비트는 이미지 헤더가 아니라 `MiRelocateImage` 의 여섯째 인자 `param_6 & 1` 에서 온다 (`MiRelocateImage.c`: `uVar12 = (param_6 & 1)*0x100 + 2` → `0xff < uVar12`). 호출자 `MiCreateNewSection` 가 무엇을 주는지는 이번에 안 읽음 `[공백]`.
  - 그 비트가 꺼져 있고 이미지가 **DLL** (`ImageCharacteristics & 0x2000`, `+0xfc ~ +0x104 BT AX,CX`) 이면 `+0x114 JNZ` 로 건너뛰지 않고 전역 비트맵 갈래 (`RtlFindClearBitsEx` · `MiObtainRelocationBits`, 64비트 구역은 `0x7fffffff0000` 에서 아래로 `64KB` 칸) 에서 **이미지 파일마다 다른 칸**을 받는다. EXE 는 `ExGenRandom` 쪽.
- 프로세스 플래그 `EPROCESS.Flags3` 비트 13 (`AllImagesAtBasePristineBase`, PDB 이름) 이 `+0xba TEST [R13+0x5fc],0x2000` 에서 흐름을 바꾼다. 뜻은 이름 외에 안 읽음 `[공백]`. `_SEGMENT.BasedAddress` 에 최종 기준 주소가 들어가는 줄도 이번에 안 따라감 `[공백]`.

**3-3. 결론**
- `[정적]` 코드 쪽: dll1·dll2 는 둘 다 DLL (`Characteristics 0x2022`) 이고 `DYNAMIC_BASE` + 재배치 있음. `param_6 & 1` 이 0 이라고 보면 (`[공백]`) **이미지 파일마다 커널이 비트맵에서 따로 고른 기준 주소**에 붙는다 → **헤더 `ImageBase` 가 같다는 사실 (`0x180000000`) 은 이 길의 결과와 무관**하고, 그 주소 범위가 test.exe 프로세스에서 비어 있으면 상태 `0` (`+0x6d2 JNZ`), **자리 충돌이면 `0x40000003`** (그 뒤 `LdrpCompleteMapModule +0x31b LdrpRelocateImage`).
  - 반대로 `param_6 & 1` 이 1 이면 둘 다 헤더 `0x180000000` 을 받아, 한 프로세스에서 **둘째로 붙는 DLL 은 반드시 충돌 → `0x40000003` → 재시도 길** 이 된다. 어느 쪽인지 가르는 `MiCreateNewSection` 은 안 읽었다 → 이 갈래가 3번이 닫히지 않는 가장 큰 이유.
- 값 자체는 **정적으로 안 닫힘** — 기준 주소 칸을 뽑는 비트맵·`ExGenRandom` 그리고 그 순간 프로세스 주소 공간의 점유 (힙·스택·다른 이미지) 가 실행 때 정해진다. 기대는 `0` `[추측]`.
- → **동적 필요 (Q104 — Q20·Q23 과 같이)**: `ntdll!LdrpMinimalMapModule +0x179` (rva `0xa399`, `NtMapViewOfSection` 돌아온 직후) 에서 `eax` 와 항목 이름. dll1·dll2·kernel32·kernelbase 모두 같은 자리에서 한 번에. 같이 `dq` 로 `poi(@rdi+0x30)` (붙은 주소) 를 읽어 dll1·dll2 가 서로 다른 주소인지 본다.
- (정적으로 더 닫으려면) 요청서 4번째 칸 후보: 커널 `MiCreateNewSection` 에서 `MiRelocateImage` 여섯째 인자 만드는 줄. 이번 요청서 범위 밖이라 안 함.

### 4. (선택) `LdrpWorkCallback` (rva `0xe9ef0`, 218B)

`sast/19` 에 이미 정리됨 (같은 `.asm`). 이번에 줄 오프셋을 다시 대조해 확인한 요약:

- 인자 (`PTP_CALLBACK_INSTANCE`, 문맥, `PTP_WORK`) 는 **하나도 안 쓴다.** 일꾼이 일감을 받으면 `LdrpWorkQueue` 를 **큐가 빌 때까지** 비운다.
- 한 바퀴: `+0x23 LdrpDetourExist` 가 켜졌으면 `+0x2a JNZ` → `+0x7a` (아무것도 안 하고 끝) → `+0x33/+0x38` 잠금 → 머리 한 개 빼기 (`+0x38 ~ +0x55`, 이웃 검사 실패 `+0x43/+0x4c JNZ` → `+0xbd INT 0x29`) → 일감이 있으면 `+0x5e LdrpWorkInProgress += 1`, `+0x64/+0x69 LdrpUpdateStatistics` → 잠금 풂 (`+0x70/+0x75`) → 일감 없으면 (`+0x78 JNZ` 안 뜀) `+0x7a` 로 끝 / 있으면 `+0x9c`.
- 첫 일감 때 `LdrpMainThreadToken` (rva `0x1d2618`) ≠ 0 이면 (`+0xa1/+0xab JNZ` → `+0xc4`) **`+0xcb/+0xd0 LdrpThreadTokenSetImpersonationToken(메인 토큰)`** (안에서 `+0x20/+0x25 NtSetInformationThread`, SSN `0xd`) 로 메인 스레드 토큰을 가장하고, 성공하면 `DIL=1`. 그 다음 **`+0xb3/+0xb8 LdrpProcessWork(일감−0x40, 0)`** 후 `+0xb8 JMP +0x23`. 끝에서 가장했으면 `+0x81/+0x86 LdrpThreadTokenSetImpersonationToken(0)` 로 되돌림.
- 이 함수 자체의 syscall: `NtSetInformationThread` (토큰 설정·해제, `LdrpMainThreadToken ≠ 0` 일 때만). 기다림 없음. `LdrpProcessWork` 안은 2-3 / 2-5 표와 같음 + 일감 하나 끝날 때 `NtSetEvent` 조건 (`LdrpProcessWork +0x1c3`).
- 실제 풀 일꾼은 이 함수 대신 `TppWorkpExecuteCallback` 안의 복사본을 돌릴 수 있음 (sast/19, `[추측: 동적 확인]`).
닫힘 여부: **정적으로 닫힘.**

## PE_emulator 한 줄 (요청서 "받고 싶은 것")

- **덫을 켜면 `LdrpQueueWork` 가 `TpPostWork` → `TppWorkPost` 를 부르는 코드가 열린다. `TppWorkPost` 에는 "넣기만" 약속하면 된다 — 기다림이 없다.** 약속할 것은 셋: (1) 일감 `WorkState` (`+0xe8`) `Insertable` = 1 (`TpAllocWork` 가 이미 `TppWorkInitialize +0x12b` 로 1 로 둠, sast/64) (2) 풀 `TaskQueue[우선순위 1][노드 0]` 큐 머리가 **자기를 가리키고 락 0** (`TpAllocPoolInternal +0x16d ~ +0x190` 가 만드는 모양) (3) **`NtReleaseWorkerFactoryWorker` 는 1번만 부르니 아무 상태 (권고 `STATUS_SUCCESS`) 를 줘도 되고 핸들 0 이어도 된다** (반환값·핸들 모두 안 봄).
- 첫 넣기 뒤 일감은 풀 큐에 영원히 남고 (`Insertable` 0), 이후 `LdrpQueueWork` 는 카운트만 올린다 — 에뮬레이터가 `LdrpDrainWorkQueue(1)` 로 `LdrpWorkQueue` 를 직접 비우는 길은 이것과 따로 돌아가므로 영향 없음 (run24).
- 조심: 에뮬레이터가 `NtQueryInformationThread(-2, 0x2a)` 에서 `LdrpDetectDetour` 에 `1` 을 주면 `LdrpDetectDetour +0xc4 TpWaitForWork(일감, 1)` 이 불릴 수 있다 (`+0xb3` 일감 ≠ 0). 두 번째 인자 1 은 "대기 중인 콜백 취소" 쪽이라 일꾼 없이 막히진 않을 것으로 보이지만 `TpWaitForWork` 본문은 이번에 안 읽음 `[공백]`. 기본 0 이면 안 탐.
- **재시도 길은 `NtMapViewOfSection` 이 `0` 이면 안 탄다.** 에뮬레이터가 dll1·dll2 에 `0` 을 주면 `LdrpCheckForRetryLoading` 은 `LdrpMapDllNtFileName +0x46` 에서 "빈 트리 → 거짓" 으로 지나갈 뿐이다. `0x40000003`/`0x40000036` 을 주기 시작하면 2-3 / 2-5 표의 syscall 이 늘어난다 (첫 시도 6 + 정리 + 둘째 시도 6 이상) 그리고 `LdrpDrainWorkQueue` 가 재시도 줄을 되돌린다.

## 짐 지도 반영거리 `[정적]`

- **유저 전역 (ntdll `.data`)**: `LdrpWorkQueue` (rva `0x1d26b0`, 머리 16B) · `LdrpWorkQueueLock` (`0x1d2680`) · `LdrpRetryQueue` (`0x1d2630`, 16B) · **`LdrpRetryingModuleIndex`** (`0x1d25b0`, 16B, 재시도 중인 이름 트리 — 초기 0) · `LdrpWorkInProgress` (`0x1d2668`) · `LdrpWorkCompleteEvent` (`0x1d2660`) · `LdrpLoadCompleteEvent` (`0x1d2620`) · `LdrpMainThreadToken` (`0x1d2618`) · `LdrpDetourExist` (`0x1d2610`) · `LdrpMapAndSnapWork` (`0x1d2640`).
- **첫 넣기 뒤 달라지는 칸** (1-7 표): 일감 `+0x0` Refcount 1→2 · `+0x38` 배리어 세기 0→1 · `+0x80` 증인표 ← `TEB+0x2b8` · `+0xd8` 리스트 연결 · `+0xe8` WorkState 1→2 · 풀 `+0x8` QueueState `0x…0002`→`0x0000000100010002` · 풀 `+0x1ac` NodeStatus `0xfffffffe`→노드 번호 · 큐(`TaskQueue[1][노드]`) 머리·락.
- **커널 개체 (에뮬레이터 바깥)**: `NtReleaseWorkerFactoryWorker` 1번이 일꾼 공장 개체의 `ReleaseCount` 같은 계수를 올릴 것 (본문은 Q134 (b) 몫 `[공백]`) — 짐 지도에 "일꾼 공장 1개 + 첫 넣기 후 상태" 로 표시.
- **ctx 구조 (`LDRP_LOAD_CONTEXT`, PDB 열거 없음 → asm 로만)**: 힙 `LdrpHeap` 에 `이름 길이 + 0xd2` 바이트, 0 채움. `+0x8` 이름 버퍼 포인터 (= ctx+0xd0) · `+0x10` 인자 · `+0x20` 플래그 (비트 15 켬; 비트 20 = 재시도 줄에 들어감) · `+0x28` 상태 칸 포인터 · `+0x30` 인자 · `+0x38` 항목 · `+0x40` 큐 연결 고리 · `+0xa8` ViewSize (run38) · `+0xb0` 안 쓰임 (항상 0) · `+0xb8` ← −1 (`LdrpAllocatePlaceHolder +0x71`; 파일 핸들 칸으로 쓰임) · `+0xd0 ~` 이름.

## 동적으로 확인해야 할 것 (Q104 VM 단계에 얹을 몫)

질문별 상태: **1번 정적으로 닫힘 · 2번 코드는 닫힘 / test.exe 가 타는지는 동적 · 3번 동적 · 4번 정적으로 닫힘 (sast/19 와 같음)**.

dast 요청 필요 (멈춤점은 `process test.exe` 조건):
1. **맵 결과 값**: `ntdll!LdrpMinimalMapModule +0x179` (rva `0xa399`, `NtMapViewOfSection` 돌아온 직후) — `eax` 와 `du poi(@rdi+0x50)` (항목 `FullDllName.Buffer`). kernel32·kernelbase·dll1·dll2 모두. 기대: dll1·dll2 `0` `[추측]`. `0x40000003`/`0x40000036` 이 나오면 바로 아래 둘을 켠다.
2. **재시도 길 열렸는지**: `ntdll!LdrpCheckForRetryLoading` 입구 (rva `0xdf18`) 에서 `edx` (DL) 와 돌아온 `al` · `ntdll!LdrpMapDllRetry` 입구 (rva `0xebefc`) 가 불리는지 · `ntdll!LdrpDrainWorkQueue +0xeb` (rva `0xad4eb`) 에서 `poi(ntdll!LdrpRetryQueue)` 가 머리와 다른지.
3. **첫 넣기 syscall 1번 + 칸**: `ntdll!TppWorkPost +0x4f3` (rva `0x39923`, `NtReleaseWorkerFactoryWorker` 부르기 직전) 에서 `rcx` = `poi(poi(ntdll!LdrpThreadPool)+0x38)` 인지, 이 멈춤점이 init 전체에서 **정확히 1번** 걸리는지. `ntdll!TppWorkPost` 입구 (rva `0x39430`) 는 넣기 횟수 (이상 1).
   - 첫 넣기 뒤 읽을 칸: `dq poi(ntdll!LdrpThreadPool)+8 L1` (기대 `0x0000000100010002`), `dd poi(ntdll!LdrpThreadPool)+0x1ac L1` (노드 번호 `0`), `dd poi(ntdll!LdrpMapAndSnapWork)+0xe8 L1` (`2`), `dq poi(ntdll!LdrpMapAndSnapWork)+0x38 L1` (`1`), `dd poi(ntdll!LdrpMapAndSnapWork) L1` (`2`).
4. **조건 입력 값**: `db 0x7ffe0386 L1` (`0`) · `dq @$teb+0x1850 L1` (`TEB.SchedulerSharedDataSlot`, 0 인지) · `dq @$teb+0x1778 L1` (`ThreadPoolData`, 메인 스레드는 0 이어야 함) · `dd 0x7ffe03c0 L1`.
5. (선택) `ntdll!NtSetEvent` 입구 — `LdrpWorkCompleteEvent` 로 가는 호출 개수.

추천 멈춤점 요약: `ntdll!LdrpMinimalMapModule +0x179` (맵 결과) · `ntdll!TppWorkPost +0x4f3` (유일한 syscall) · `ntdll!LdrpCheckForRetryLoading` 입구.

## 판정

**분석성공.** 핵심 1번 (`TppWorkPost` 기본 대답 갈래·처음 넣기 갈래·기다림 유무) 은 `.asm` 줄 근거로 닫힘: 갈래 하나, 첫 넣기 syscall 1개, 기다림 없음. 2번은 코드 조건·syscall 표까지 닫고 test.exe 가 타는지는 3번 (dll1·dll2 `NtMapViewOfSection` 결과) 에 달려 동적. 남은 `[공백]`: 클래스·이름이 아닌 것 — 커널 `ExpWorkerFactory…` 본문, `TpWaitForWork` 본문, `EPROCESS.Flags3` 비트 13 의 뜻, `_SEGMENT.BasedAddress` 를 채우는 줄.

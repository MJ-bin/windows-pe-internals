# 2026-11-22 (작성일, 정적 분석 2026-11-16) run125 — 일꾼 공장 (Q218, 정적만): `NtReleaseWorkerFactoryWorker` 는 넣은 일감 수를 올리고 "0 패킷" 하나를 완료 포트에 넣은 뒤 **같은 syscall 안에서** `ExpWorkerFactoryCheckCreate` 를 부른다. 일꾼이 0 이고 조건이 맞으면 그 자리에서 `ExpWorkerFactoryCreateThread` → `RtlpCreateUserThreadEx` → `ZwCreateThreadEx` (= `NtCreateThreadEx` 같은 길) 로 일꾼을 **멈춘 채 만들고 바로 푼다**. 시작 = 공장 `+0x18`, 인자 `+0x20`, 스택 `+0x38` / `+0x40`, 만들기 플래그 `0x1` 또는 `0x11` (`LoaderWorker`)

> **닫힘 (run130 — Q228)**: 아래 "결정할 것" 둘 — (가) 그림 2 커널 일꾼 만들기 노드는 **사용자 결정 2026-11-22: 지금은 안 넣음** (Q226 동적 뒤 다시), (나) 짐 지도 7.6.12 커널 줄 · "3장 (run72) 줄" (= 짐 지도 **4장** 두 공간 짝의 `(run72)` 줄) 은 **run130 에서 이 run 칸으로 채움**.


(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("일꾼 공장", 튜닝 `278_tune06.md P2`) 이다. 요청서는 "test.exe 초기화 범위 밖" 으로 잡았지만, **첫 넣기 자체는 초기화 안에서 한 번 일어난다** — 로더 풀 첫 `TppWorkPost +0x4f3 NtReleaseWorkerFactoryWorker` (run72 `[정적]`), 일꾼 2개가 P1a ~ P1b 에 생김 (run44 `[관찰]`). 그래서 flow.md 는 3장 끝 새 절 하나 · 4장 · 5장에 더해, 앞서 `[추측]` / `[공백]` 으로 남았던 줄 넷 (2.4 bit 11 뜻 · 2.6 "계기일 수 있다" · 3장 일꾼 `CreateFlags` · 3장 run72 줄) 에 **run125 화살표만** 붙였다. **그림은 안 바꿨다** (커널 쪽 일꾼 만들기 노드를 그림 2 에 넣을지는 결정할 일 — 아래 "아직 확인 필요"). 짐 지도 (`baggage.md`) 는 건드리지 않았다 (7.6.12 의 커널 칸 `[공백]` 을 이번 결과로 채울 수 있음 — 다음 짐 지도 바퀴 후보).

> **대기열**: **Q218** = **Q139 (d) + Q134 (b)** 를 당겨 온 것 + 덧붙임. 이번 run 으로 닫는 몫:
> - **Q139 (d)** 커널 `NtReleaseWorkerFactoryWorker` 본문 — **닫힘** (아래 1 · 2).
> - **Q134 (b)** 커널 `NtSetInformationWorkerFactory` 클래스 2 · 3 · 5 · `0xd` 갈래 (점프표), 공장 `+0x198` bit 11 의 뜻, 클래스 이름 — **닫힘**. 점프표 셋을 바이트로 풀어 클래스마다 몸 자리 · 칸을 정했고 (아래 5), bit 11 = "일꾼을 `LoaderWorker` 로 만들라" (`CreateFlags 0x10`). 클래스 **이름**은 PDB 에 열거가 없어서 (`llvm-pdbutil` types 에 `WorkerFactory` 0건) 공개 헤더와 맞춘 `[추측: 이름]` 으로 남는다 — 정적으로 더 닫을 길 없음.
> - Q139 · Q134 의 다른 몫 ((a) (b) (c) (e) 등) 은 이번에 안 했다.

## 요약

- **넣기 (`NtReleaseWorkerFactoryWorker`)** `[정적]`:
  - 마감 바이트 `P+0x21` 이 켜졌으면 `0x80`. 넣은 일감 수 `P+0x18` 을 1 올림 (`-1` 이면 `0xc0000001`).
  - 패킷이 이미 포트에 있으면 (`P+0x20`) **거기서 끝** (상태 0, 일꾼 만들기 검사도 안 함).
  - 아니면 `P+0x20 = 1`, **키 · APC 문맥 · 상태 · 정보가 모두 0 인 "풀어주기 패킷"** 을 완료 포트에 넣고 (기다리는 일꾼이 있으면 바로 넘김), **`+0x201 ExpWorkerFactoryCheckCreate(공장, 0, 0)`**.
- **일꾼을 만드는 조건** `[정적]` (`CheckCreate +0x8b ~ +0x16c`): 만드는 중 0 · 움직이는 일꾼 < 최대 · 기다리는 일꾼 0 · 포트에 일 있음 · 포트 `CurrentCount` < CPU 수 · 기록판 걸림 없음 · 부드러운 최대 안 넘음.
  - **새 공장의 첫 넣기는 전부 참 → 그 자리에서 만든다.** run72 `[추측]` "첫 넣기가 일꾼을 만드는 계기" → **맞다 `[정적]`**.
  - CPU 수 · 기록판 · 부드러운 최대에 걸리면 **30 ms → 120 ms 미룸** (시스템 관리 스레드 `ExpWorkerFactoryManagerThread` 가 처리). 만들기 실패면 길게 (`ThreadCreationTimeout` 초, 파일 값 10초).
- **만드는 길** `[정적]`: `CheckCreate +0x19b ExpWorkerFactoryCreateThread` → `+0xe5 RtlpCreateUserThreadEx` → `+0x14c ZwCreateThreadEx` → **`NtCreateThreadEx` (사용자 `CreateThread` 와 같은 길)** → `PspCreateThread` … (run74). 공장을 만든 프로세스에서만 돈다.
  - 시작 = `f+0x18`, 인자 = `f+0x20`, 스택 예약 = `f+0x38` (기본 `0x10000`), 커밋 = `f+0x40` (기본 `0x1000`). ntdll 은 시작 = `TppWorkerThread`, 인자 = 풀, 스택 = exe 헤더 값을 넘긴다.
  - 만들기 플래그 = `0x1` (멈춘 채로), 공장 bit 11 이 켜졌으면 `0x11` → `LoaderWorker`. 만든 뒤 `+0x153 ZwResumeThread` 로 **같은 호출 안에서 푼다**.
- **일감 받기 (`NtWaitForWorkViaWorkerFactory`)** `[정적]`: `+0x6ba IoRemoveIoCompletion` 으로 포트에서 꺼냄, 출력 한 칸 `0x20` × `Count` (1 ~ `0x7ffffff`), **시간 제한 없음**. 마감 = `0x80`, 줄이기 = `0x102`.
- **설정 클래스 4 · `0xb`** `[정적]`: 둘 다 입력 4바이트. 4 = 최소 스레드 수 (`f+0x178`) — 올리면 **그 자리에서 모자란 만큼 일꾼을 만든다** (조건 검사 없음). `0xb` = 일꾼 기본 우선순위 (`f+0x19c`) — 앞으로 만들 일꾼에만.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든셋째 바퀴. plan **Q218** (PE_emulator 요청 "일꾼 공장"). 사용자 결정 2026-11-16 "지금 즉시 먼저" (분석 멈춤 중 예외), 2026-11-22 "문서로 만들어서 커밋". **짐 지도 바퀴 아님.**
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`, 기준값과 같음). 다시 analyze 안 함 — 있던 `.gpr` 에서 읽기 전용 `export` 4번 (모두 `^이름$` depth 0).
- ntdll 은 있던 `TpAllocPoolInternal.asm` 한 장만 읽음 (`NtCreateWorkerFactory` 인자 값).
- 버전 대조 (결과서): `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` call `+0x802` / ret `+0x807` → `PspCallProcessNotifyRoutines` — **일치**. 스크립트 문제 없음.
- 같은 때 다른 decompiler 가 mswsock · afd · nsi (sast/121) 를 돌렸다 — 이번엔 안 건드림.
- **PDB 에 일꾼 공장 구조체도 정보 클래스 열거도 없다.** 칸 위치 · 동작은 `[정적]`, 칸 · 클래스 **이름**은 공개 헤더 (`WORKER_FACTORY_BASIC_INFORMATION` · `WORKERFACTORYINFOCLASS`) 와 맞춘 `[추측: 이름]`.

## 목적

- PE_emulator 는 지금 `NtReleaseWorkerFactoryWorker` 에서 핸들 · 접근 검사 뒤 멈추고, 일꾼 스레드를 안 만들어서 **모두 잠든다**. 진짜 커널은 무엇을 하나 (요청서 `_workspace/sast/122_worker-factory.md`):
  1. 넣기가 공장의 어떤 칸을 바꾸나, 어떤 조건에서 일꾼을 만드나, 바로인가 미루나. 첫 넣기가 계기인가 (run72 `[추측]` 닫기).
  2. 일꾼 스레드를 만드는 길 — 시작 주소 · 인자 · 스택 크기의 출처, `NtCreateThreadEx` 와 같은 길인가, 누구 프로세스에서 도나.
  3. `NtWaitForWorkViaWorkerFactory` — 꺼내는 함수 · 출력 모양, 기다림, 마감 · 줄이기 때 끝나는 길.
  4. 설정 클래스 4 (= Q134 (b) 몫) · `0xb` 의 뜻.
- 요청한 쪽이 본 에뮬레이터 로그 (진짜 관찰 아님): 공장 만들기 뒤 설정 클래스 5 → **4** → 3 차례, 클래스 4 와 `0xb` 가 "지원 안 함" 대답.

## 방법

- 정적: `_workspace/sast/122_worker-factory.md` — ntoskrnl, 모두 `^이름$` depth 0.
  - 요청서 대상 14: `NtReleaseWorkerFactoryWorker` · `NtWorkerFactoryWorkerReady` · `NtWaitForWorkViaWorkerFactory` · `NtCreateWorkerFactory` · `ExpWorkerFactoryCheckCreate` · `ExpWorkerFactoryWantsToCreate` · `ExpWorkerFactoryCreateThread` · `ExpWorkerFactoryDeferredThreadCreation` · `ExpSetWorkerFactoryDeferredCreateTimer` · `ExpWorkerFactoryManagerThread` · `ExpWorkerFactoryCompletionPacketRoutine` · `ExpTryEnterWorkerFactoryAwayMode` · `ExpLeaveWorkerFactoryAwayMode` · `ExpWorkerFactoryFinishDeferredWork`.
  - 넓힌 것 14 (결과서에 이유): `RtlpCreateUserThreadEx` (스레드 만들기 호출 그 자체) · `NtQueryInformationWorkerFactory` (칸 뜻 맞추기) · `ExpCheckThreadHistory` · `ExpInitializeThreadHistory` · `ExpRemoveCurrentThreadFromThreadHistory` · `ExpWorkerFactoryInitialization` · `ExpShutdownWorkerFactory` · `ExpDeleteWorkerFactory` · `ExpCloseWorkerFactory` · `NtShutdownWorkerFactory` · `IopFreeMiniCompletionPacket` · `IoFreeMiniCompletionPacket` · `IoAllocateMiniCompletionPacket` · `IopAllocateMiniCompletionPacket`.
  - 있던 것 읽기만: `NtSetInformationWorkerFactory.asm` · `NtCreateThreadEx.asm` · `IoRemoveIoCompletion.asm`, ntdll `TpAllocPoolInternal.asm`.
  - 전역 `refs` 2장 (`ExpWorkerFactoryThreadIdleTimeoutInSeconds` · `ExpWorkerFactoryThreadCreationTimeoutInSeconds`). 점프 표 셋 · 전역 초기값은 파일 바이트 (결과서 스크래치, 커밋 안 함).
- 결과서: `_workspace/sast/122_worker-factory_result.md` (**분석성공**, 스크립트 문제 없음).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` 로 call · ret 짝 맞춤 — `NtReleaseWorkerFactoryWorker` · `ExpWorkerFactoryCheckCreate` · `ExpWorkerFactoryCreateThread` · `RtlpCreateUserThreadEx` · `NtWaitForWorkViaWorkerFactory` · `NtWorkerFactoryWorkerReady` · `NtSetInformationWorkerFactory` · `NtCreateWorkerFactory` · `ExpWorkerFactoryManagerThread` · `ExpWorkerFactoryDeferredThreadCreation` · `IoRemoveIoCompletion` · `IopFreeMiniCompletionPacket` · `ExpShutdownWorkerFactory` · `NtShutdownWorkerFactory` · `ExpCloseWorkerFactory`.
  - `.asm` 줄: `NtReleaseWorkerFactoryWorker +0x27` · `+0xe0 ~ +0x10c` · `+0x132 ~ +0x153` · `+0x190 ~ +0x19f` · `+0x1f9 ~ +0x201`, `CheckCreate +0x8b ~ +0xac` · `+0x10c ~ +0x19b` · `+0x24d ~ +0x25a`, `CreateThread +0x1f` · `+0x36` · `+0x62` · `+0x91 ~ +0x12f`, `RtlpCreateUserThreadEx +0x73` · `+0xfc` · `+0x122 ~ +0x147`, `NtSetInformationWorkerFactory +0x67 ~ +0x85` · `+0x2b4 ~ +0x2d8` · `+0x6df ~ +0x768` · `+0x785 ~ +0x797`, `NtWorkerFactoryWorkerReady +0x66 ~ +0x90`, `NtWaitForWorkViaWorkerFactory +0x284` · `+0x2d5 ~ +0x30e` · `+0x684 ~ +0x6bf` · `+0x7a9 ~ +0x7bf` · `+0x979 ~ +0x980` · `+0xb15 ~ +0xb23`, `ExpWorkerFactoryCompletionPacketRoutine +0x71 ~ +0x7c`, `ExpShutdownWorkerFactory +0xad ~ +0xbc`, `NtCreateWorkerFactory +0x130 ~ +0x137` · `+0x1b4 ~ +0x28d`, ntdll `TpAllocPoolInternal +0x316 ~ +0x459`.
  - 같은 주소 이름은 `pdb_aliases.py --lookup ntoskrnl.exe` 로 (아래 표).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".
- 앞 바퀴 짝: run62 (기본 풀 + 공장 만들기), run69 (로더 풀, 클래스 2 · 3 · 5 · `0xd` 를 부르는 쪽 · 길이), run72 (`TppWorkPost` 첫 넣기), run74 (`NtCreateThreadEx` → `PspCreateThread` 길, `CreateFlags` 대응표), run93 (완료 포트 · `IoRemoveIoCompletion`).

## 결과

비유 (공장과 우체통):

- 일꾼 공장 = 일꾼을 불러 모으는 **사무소**. 사무소엔 우체통 (완료 포트) 이 하나 붙어 있다.
- 일감을 넣는 쪽 (`TppWorkPost`) 은 일감 자체를 넣지 않는다. **"일 있어요" 쪽지 한 장** (0 패킷) 만 우체통에 넣고 "넣은 수" 를 하나 올린다. 쪽지가 이미 들어 있으면 수만 올린다.
- 쪽지를 넣은 그 자리에서 사무소가 둘러본다: "지금 우체통 앞에서 기다리는 일꾼이 없고, 정원이 남았고, 뽑는 중인 사람도 없네" → **바로 한 명 뽑아서 일터로 보낸다**. 사람이 너무 많이 일하고 있으면 30 ms 뒤에 다시 본다.
- 뽑힌 일꾼은 먼저 "왔어요" (`WorkerReady`) 를 하고, 우체통에서 쪽지를 꺼낸다. 쪽지엔 아무것도 안 적혀 있다 — 진짜 일감은 ntdll 풀 쪽 목록에서 찾는다 `[추측: TppWorkerThread 안은 안 읽음]`.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtReleaseWorkerFactoryWorker` | `+0x46` | `+0x4b` | `ObReferenceObjectByHandle` (접근 1) | [정적] |
| `nt!NtReleaseWorkerFactoryWorker` | `+0x11f` | `+0x124` | `ExpLeaveWorkerFactoryAwayMode` (공장 bit 9 일 때) | [정적] |
| `nt!NtReleaseWorkerFactoryWorker` | `+0x18b` | `+0x190` | `KiAcquireKobjectLockSafe` (포트) | [정적] |
| `nt!NtReleaseWorkerFactoryWorker` | `+0x1be` | `+0x1c3` | `KiWakeQueueWaiter` (기다리는 일꾼에게 직접) | [정적] |
| `nt!NtReleaseWorkerFactoryWorker` | `+0x44a` | `+0x44f` | `KiWakeOtherQueueWaiters` (줄에 붙인 뒤) | [정적] |
| `nt!NtReleaseWorkerFactoryWorker` | `+0x2c4` | `+0x2c9` | `IopAllocateMiniCompletionPacket` (패킷이 없을 때만) | [정적] |
| `nt!NtReleaseWorkerFactoryWorker` | `+0x201` | `+0x206` | **`ExpWorkerFactoryCheckCreate(f, 0, 0)`** | [정적] |
| `nt!ExpWorkerFactoryCheckCreate` | `+0x148` | `+0x14d` | `ExpCheckThreadHistory` (걸림 ②) | [정적] |
| `nt!ExpWorkerFactoryCheckCreate` | `+0x19b` | `+0x1a0` | **`ExpWorkerFactoryCreateThread`** | [정적] |
| `nt!ExpWorkerFactoryCheckCreate` | `+0x27f` | `+0x284` | 미룬 목록에 넣기 (같은 주소 이름 2개, 후보: `ExpInterlockedPushEntrySList` · `RtlpInterlockedPushEntrySList`) | [정적] |
| `nt!ExpWorkerFactoryCheckCreate` | `+0x286` | `+0x28b` | `ExpSetWorkerFactoryDeferredCreateTimer` | [정적] |
| `nt!ExpWorkerFactoryCreateThread` | `+0x65` | `+0x6a` | `ExAcquireRundownProtection` (`f+0x68`) | [정적] |
| `nt!ExpWorkerFactoryCreateThread` | `+0xe5` | `+0xea` | **`RtlpCreateUserThreadEx`** | [정적] |
| `nt!ExpWorkerFactoryCreateThread` | `+0x13d` | `+0x142` | `ZwSetInformationThread` (클래스 3, `f+0x19c ≠ 0` 일 때만) | [정적] |
| `nt!ExpWorkerFactoryCreateThread` | `+0x1c8` | `+0x1cd` | `ZwSetInformationThread` (클래스 `0x27`, 공장 bit 14 일 때만) | [정적] |
| `nt!ExpWorkerFactoryCreateThread` | `+0x153` | `+0x158` | **`ZwResumeThread`** | [정적] |
| `nt!ExpWorkerFactoryCreateThread` | `+0x15f` | `+0x164` | `ObCloseHandle` | [정적] |
| `nt!RtlpCreateUserThreadEx` | `+0x14c` | `+0x151` | **`ZwCreateThreadEx`** | [정적] |
| `nt!NtWorkerFactoryWorkerReady` | `+0x48` | `+0x4d` | `ObReferenceObjectByHandle` (접근 `0x10`) | [정적] |
| `nt!NtWaitForWorkViaWorkerFactory` | `+0x20d` | `+0x212` | `ObReferenceObjectByHandle` (접근 2) | [정적] |
| `nt!NtWaitForWorkViaWorkerFactory` | `+0x6ba` | `+0x6bf` | **`IoRemoveIoCompletion`** (시간 제한 NULL) | [정적] |
| `nt!NtWaitForWorkViaWorkerFactory` | `+0x6ca` | `+0x6cf` | `ExpWorkerFactoryFinishDeferredWork` | [정적] |
| `nt!NtWaitForWorkViaWorkerFactory` | `+0xa5d` | `+0xa62` | `ExpWorkerFactoryCheckCreate` (일 집은 뒤 연쇄) | [정적] |
| `nt!NtWaitForWorkViaWorkerFactory` | `+0xaad` | `+0xab2` | `ExpWorkerFactoryWantsToCreate` (놀기 시간 초과 뒤) | [정적] |
| `nt!NtWaitForWorkViaWorkerFactory` | `+0xb1e` | `+0xb23` | `ExpRemoveCurrentThreadFromThreadHistory` (줄이기) | [정적] |
| `nt!IoRemoveIoCompletion` | `+0x8d` | `+0x92` | `KeRemoveQueueEx` | [정적] |
| `nt!IoRemoveIoCompletion` | `+0x1ec` | `+0x1f1` | `IopFreeMiniCompletionPacket` (종류 4 패킷) | [정적] |
| `nt!IopFreeMiniCompletionPacket` | `+0xac` | `+0xb1` | `ExpWorkerFactoryCompletionPacketRoutine` (콜백이 그것일 때) | [정적] |
| `nt!NtSetInformationWorkerFactory` | `+0x513` | `+0x518` | `ExpWorkerFactoryCreateThread` (클래스 5) | [정적] |
| `nt!NtSetInformationWorkerFactory` | `+0x582` | `+0x587` | `ExpTryEnterWorkerFactoryAwayMode` (클래스 3) | [정적] |
| `nt!NtSetInformationWorkerFactory` | `+0x59a` | `+0x59f` | `ExpWorkerFactoryCheckCreate` (클래스 3) | [정적] |
| `nt!NtSetInformationWorkerFactory` | `+0x63b` | `+0x640` | `KeTimeOutQueueWaiters` (클래스 `0xc`) | [정적] |
| `nt!NtSetInformationWorkerFactory` | `+0x6d5` | `+0x6da` | `KeSetTimer2` (클래스 2) | [정적] |
| `nt!NtSetInformationWorkerFactory` | `+0x74f` | `+0x754` | `ExpWorkerFactoryCreateThread` (클래스 4 루프) | [정적] |
| `nt!NtCreateWorkerFactory` | `+0xc3` | `+0xc8` | `ObReferenceObjectByHandle` (완료 포트, 접근 2) | [정적] |
| `nt!NtCreateWorkerFactory` | `+0x169` | `+0x16e` | `ObOpenObjectByPointer` (프로세스 → 커널 핸들 `f+0x28`) | [정적] |
| `nt!NtCreateWorkerFactory` | `+0x182` | `+0x187` | `IoAllocateMiniCompletionPacket` (풀어주기 패킷, 콜백 = `ExpWorkerFactoryCompletionPacketRoutine`) | [정적] |
| `nt!NtCreateWorkerFactory` | `+0x1c5` | `+0x1ca` | `ObCreateObjectEx` (몸 `0x2a0`) | [정적] |
| `nt!NtCreateWorkerFactory` | `+0x30f` | `+0x314` | `KeSetTimer2` (놀기 타이머) | [정적] |
| `nt!ExpWorkerFactoryManagerThread` | `+0x51` | `+0x56` | `KeRemoveQueueEx` (관리 큐) | [정적] |
| `nt!ExpWorkerFactoryManagerThread` | `+0x60` | `+0x65` | `ExpWorkerFactoryDeferredThreadCreation` | [정적] |
| `nt!ExpWorkerFactoryManagerThread` | `+0xc3` | `+0xc8` | `ExpWorkerFactoryCheckCreate` ("멀리 있기" 알림) | [정적] |
| `nt!ExpWorkerFactoryManagerThread` | `+0x115` | `+0x11a` | `KeTimeOutQueueWaiters` (놀기 제한) | [정적] |
| `nt!ExpWorkerFactoryDeferredThreadCreation` | `+0x59` | `+0x5e` | `ExpWorkerFactoryCheckCreate(f, 락, 1)` | [정적] |
| `nt!ExpWorkerFactoryInitialization` | `+0x1b9` | `+0x1be` | `PsCreateSystemThread` (관리 스레드, 부팅 때) | [정적] |
| `nt!NtShutdownWorkerFactory` | `+0x99` | `+0x9e` | `ExpShutdownWorkerFactory` | [정적] |
| `nt!ExpCloseWorkerFactory` | `+0xd` | `+0x12` | `ExpShutdownWorkerFactory` (마지막 핸들) | [정적] |
| `nt!ExpShutdownWorkerFactory` | `+0x13b` | `+0x140` | `IoSetIoCompletionEx3` (마감 패킷) | [정적] |

- `NtCreateWorkerFactory` 는 `ExpWorkerFactoryCreateThread` · `CheckCreate` 를 **안 부른다** (`_calls.tsv` 에 없음) → 공장을 만드는 것만으로는 일꾼이 안 생긴다 `[정적]`.

### 1. 넣기 — `NtReleaseWorkerFactoryWorker` (rva `0x260220`, 1145B) [정적]

차례 (`.asm`):

1. `+0x46` 핸들 → 공장 (`+0x27 LEA EDX,[R13+1]` = 접근 1).
2. IRQL 2 · `P` 락 (`P` = 공장 `+0x10` 이 가리키는 0x28 바이트 덩어리, 아래 6).
3. `+0xe0` `P+0x21` (마감) ≠ 0 → 락 풀고 **`0x80`**. `CheckCreate` 안 부름.
4. `+0xea ~ +0xf0` `P+0x18` 이 `-1` 이면 **`0xc0000001`** (넘침 막기).
5. `+0xfb` **`P+0x18` ++** (넣은 일감 수).
6. `+0x102` `P+0x20` (패킷이 이미 포트에 있음) ≠ 0 → 락 풀고 **상태 0 으로 끝** (`+0x106 JNZ`). 새 패킷 · `CheckCreate` 둘 다 없음.
7. `+0x10c` **`P+0x20 = 1`**. 공장 bit 9 ("멀리 있기") 면 `+0x11f ExpLeaveWorkerFactoryAwayMode`. 락 풀기.
8. 패킷 = `P+0x10` (`+0x132`). 없으면 `+0x2c4` 새로 할당, 그것도 실패하면 패킷 없이 `CheckCreate` 로.
9. `+0x143 ~ +0x153` 패킷 `+0x18` · `+0x20` · `+0x28` · `+0x30` (키 문맥 · APC 문맥 · 상태 · 정보) 을 **0 으로**.
10. 포트 락 (`+0x18b`). **포트에 기다리는 스레드가 있고** (`+0x190`) **`CurrentCount < MaximumCount`** (`+0x199 ~ +0x19f`) 면 `+0x1be KiWakeQueueWaiter` 로 직접 넘김. 아니면 `SignalState` ++ · 줄 꼬리에 붙임 (`+0x2e0 ~ +0x42f`), 옛 값 0 이고 기다리는 스레드가 있으면 `+0x44a KiWakeOtherQueueWaiters`.
11. **`+0x201 ExpWorkerFactoryCheckCreate(f, 0, 0)`** — 둘째 0 = 락 스스로 잡음, 셋째 0 = 미룬 일 다시 하기 아님.
12. 상태 0 반환.

- **바뀌는 칸**: `P+0x18` ++, `P+0x20` = 1, 포트 `SignalState` ++ (또는 직접 넘김), 공장 bit 9 끔. 일꾼 수 칸 (`f+0x180` · `+0x184` · `+0x188` · `+0x190`) 은 직접 안 건드린다.
- **"Release 를 N 번 불렀는데 패킷은 하나"**: 일꾼이 패킷을 꺼낼 때마다 콜백 `ExpWorkerFactoryCompletionPacketRoutine` 이 `P+0x18` 을 내리고 (`+0x71`), 0 이 아니면 **같은 패킷을 다시 포트에 넣는다** (`+0x75 JNZ`), 0 이 되면 `P+0x20 = 0` (`+0x7c`). → N 번 넣으면 일꾼이 N 번 꺼낸다.

### 2. 일꾼을 만드는 조건 — `ExpWorkerFactoryCheckCreate` (rva `0x260cf0`) [정적]

보통 모양 (셋째 인자 0 — Release · Wait · 설정이 부름). 모두 참일 때만 만든다:

| # | 조건 | 줄 | 뜻 |
|---|---|---|---|
| 1 | `f+0x190 == 0` | `+0x8b` | 이미 만드는 중이 아님 |
| 2 | `f+0x180 < f+0x17c` | `+0x94 ~ +0x9a` | 움직이는 일꾼 < 최대 |
| 3 | `P+0x1c == 0` | `+0xa6` | 기다리는 일꾼 없음 |
| 4 | 포트 `SignalState ≠ 0` 또는 `P+0x18 ≠ 0` | `+0x10c` · `+0x112` | 일이 있음 |
| 5 | 포트 `CurrentCount < KeNumberProcessors` | `+0x128 ~ +0x133` | CPU 수만큼 이미 일하는 중이 아님 — **걸림 ①** |
| 6 | `ExpCheckThreadHistory(f)` 거짓 | `+0x148 ~ +0x14f` | 최근 일꾼 4명 기록판 — **걸림 ②** (뜻은 `[공백]`) |
| 7 | `f+0x18c == 0` 또는 `f+0x180 <= f+0x18c` | `+0x15e ~ +0x16c` | 부드러운 최대 — **걸림 ③** |

- 1 ~ 4 중 하나라도 아니면 `+0xac` 에서 상태 비트만 지우고 **그냥 돌아감** (안 만듦).
- 다 맞으면: bit 9 끔 (`+0x17b`) → **`f+0x190` ++ (`+0x180`)** → 걸림 비트 지움 (`+0x189`, 마스크 `0xffffce0f`) → 락 풀기 → **`+0x19b ExpWorkerFactoryCreateThread(f)`**.
- **새 공장 첫 넣기** (일꾼 0, `P+0x18 = 1`, 포트에 패킷 1, `CurrentCount 0`, 기록판 비어 거짓, 부드러운 최대 0) → **전부 참 → 그 자리에서 만든다.**
- **걸림 ① ② ③ (미룸)**: 처음 걸리면 단계 비트 (`0x40` / `0x10` / `0x1000`) 를 켜고 타이머 종류 3 (30 ms), 다시 하기 때 또 걸리면 다음 단계 (`0x80` / `0x20` / `0x2000`) · 종류 2 (120 ms), 그다음 다시 하기 때는 **그 검사를 건너뛰고 만든다**.
  - 미루기 공통 (`+0x24d ~ +0x28b`): bit 3 켜기 (`+0x25a`) → 공장 참조 → 미룬 목록에 넣기 (`+0x27f`) → `+0x286 ExpSetWorkerFactoryDeferredCreateTimer(종류)`.
  - 타이머 (결과서, `ExpWorkerFactoryInitialization +0x84 ~ +0x96`): 3 = 30 ms, 2 = 120 ms, 1 = `ExpWorkerFactoryThreadCreationTimeoutInSeconds` 초 (파일 초기값 10). 한 번에 하나, 더 급한 종류가 이김.
  - 미룬 일은 **시스템 스레드 `ExpWorkerFactoryManagerThread`** (부팅 때 `ExpWorkerFactoryInitialization +0x1b9 PsCreateSystemThread`) 가 `+0x60 ExpWorkerFactoryDeferredThreadCreation` → `+0x59 CheckCreate(f, 락, 1)` 로 처리.
- **만들기 실패** (`CreateThread` 가 음수): `f+0x190` -- (`+0x1b6`). 상태가 `0xc000000a` · `0xc000010a` · `0xc00000f2` · `0xc0000001` 이면 조용히 포기, 그 밖이면 bit 8 켜고 종류 1 (길게) 로 미룸.
- **"멀리 있기"** (`ExpTryEnterWorkerFactoryAwayMode`, 클래스 3 · 관리 스레드가 부름): 묶음 수 `f+0x194 ≠ 0` 이고 포트가 비었으면 bit 9 · 10 을 켜고 포트에 알림을 걸어, 일이 들어오면 관리 스레드가 `CheckCreate` (`Manager +0xc3`). Release 는 먼저 이걸 끈다 (`+0x11f`).

**일꾼을 만드는 계기 전부** (`CheckCreate` · `CreateThread` 를 부르는 곳):

| 계기 | 줄 | 조건 |
|---|---|---|
| 넣기 | `NtRelease +0x201` → `CheckCreate` | 위 표 |
| 일꾼이 일을 집은 뒤 (연쇄) | `NtWait +0xa5d` → `CheckCreate` | `+0x7a9 ~ +0x7bf`: 움직이는 수 < 최대 · 기다리는 일꾼 0, 묶음 0 이면 곧바로 (`+0x979 ~ +0x980`), 묶음 있으면 포트에 일이 남았을 때만 |
| 설정 클래스 3 (묶음 0 → ≠ 0) | `Set +0x59a` → `CheckCreate` | `+0x582 TryEnterAwayMode` 가 참 = 포트에 이미 일이 있을 때뿐 |
| 설정 클래스 4 (최소 올림) | `Set +0x74f` → `CreateThread` 직접 | 아래 5 |
| 설정 클래스 5 (최대) | `Set +0x513` → `CreateThread` 직접 | 옛 최대 0 · `P+0x18 ≠ 0` 또는 포트에 일 (`+0x4bf ~ +0x4c7`), 실패는 삼킴 |
| 관리 스레드 | `Manager +0xc3`, `DeferredThreadCreation +0x59` | 미룸 · "멀리 있기" |

### 3. 일꾼을 만드는 길 · 새 스레드 첫 값 [정적]

```
nt!NtReleaseWorkerFactoryWorker
 └─ +0x206  ExpWorkerFactoryCheckCreate(f, 0, 0)
     └─ +0x1a0  ExpWorkerFactoryCreateThread(f)
         ├─ +0x6a   ExAcquireRundownProtection(f+0x68)      (실패 → 0x80)
         │  +0x87   P+0x21 (마감) 이면 안 만듦 · +0x91 f+0x188 ++
         ├─ +0xea   RtlpCreateUserThreadEx                   (+0xea f+0x1a0 ← 상태)
         │   └─ +0x151  ZwCreateThreadEx  ⇒ nt!NtCreateThreadEx → +0x2d2 PspCreateThread
         │                                    → +0x283 PspAllocateThread · +0x2ea PspInsertThread   (run74)
         ├─ +0x142  ZwSetInformationThread(3, &f+0x19c, 4)     (f+0x19c ≠ 0 일 때만)
         ├─ +0x1cd  ZwSetInformationThread(0x27, &f+0x78, …)   (공장 bit 14 일 때만)
         ├─ +0x158  ZwResumeThread                            ← 멈춰 만든 스레드를 푼다
         └─ +0x164  ObCloseHandle · +0x16d ExReleaseRundownProtection
```

- (나무 오프셋은 ret 자리. `NtCreateThreadEx` 아래는 run74 의 ret.)
- **`NtCreateThreadEx` 와 같은 길이다.** 다른 점은 호출자가 커널이라 `PreviousMode = Kernel` 인 것뿐 `[추측: Zw 스텁 30B 는 안 읽음]` — `NtCreateThreadEx +0x86 ~ +0x8c` (핸들 쓸 곳 검사 건너뜀), `+0xcb ~ +0x12c` (속성 · 프로세스 핸들에 `PreviousMode` 넘김 → 커널 핸들 허용).
- 앞뒤로 `KeEnterCriticalRegion` · `KeLeaveCriticalRegion` (`+0x5a` · call `+0x16d`). 실패면 `f+0x188` -- (`+0x19f`) 하고 상태를 돌려줌.

| 새 스레드 값 | 출처 | 근거 |
|---|---|---|
| **프로세스** | `NtCreateWorkerFactory` 5번째 인자 → 커널 핸들 `f+0x28` | Create `+0x169`, `CreateThread +0xa3` |
| **프로세스 제한** | 공장을 만드는 스레드의 프로세스여야 함 — 아니면 `0xc000000d` | Create `+0x130 ~ +0x137` (`[KTHREAD+0xb8]` 비교) |
| **시작 주소** (`Rcx`; `Rip` = `RtlUserThreadStart`) | 6번째 인자 → `f+0x18` | Create `+0x266`, `CreateThread +0xca`, `Rtlp +0x147` |
| **시작 인자** (`Rdx`) | 7번째 인자 → `f+0x20` | Create `+0x272`, `CreateThread +0xc1`, `Rtlp +0x142` |
| **스택 예약** (`MaximumStackSize`) | 9번째 인자 → `f+0x38` (0 이면 `0x10000`) | Create `+0x214 ~ +0x228`, `CreateThread +0xdc`, `Rtlp +0x122` |
| **스택 커밋** (`StackSize`) | 10번째 인자 → `f+0x40` (0 이면 `0x1000`) | Create `+0x244 ~ +0x25a`, `CreateThread +0xd3`, `Rtlp +0x12b` |
| `ZeroBits` | 0 | `CreateThread +0xb0` |
| **만들기 플래그** (`CreateFlags`) | `1` 에 `(f+0x198 & 0x800) >> 7` 을 OR 한 값 = **`0x1`** 또는 **`0x11`** | `CreateThread +0x1f` · `+0x36` · `+0x62`, `Rtlp +0x73` (`& 0x77` 만 통과) |
| 속성 | `PsAttributeClientId` 하나 (만든 스레드 `CLIENT_ID` 받기) | `Rtlp +0x9e ~ +0xb2` |
| 접근 · 개체 속성 | `0x1fffff` · `OBJ_KERNEL_HANDLE` (`0x200`) | `Rtlp +0xfc` · `+0x83 ~ +0x8e` |

- **ntdll 이 넘기는 값** (`TpAllocPoolInternal.asm`, 결과서): 프로세스 `-1` (`+0x2ee`), **시작 = `TppWorkerThread`** (`+0x2e2`, rva `0x1a2e0`), **인자 = 풀** (`+0x2dd`), 최대 = 인자 (0 이면 KUSD `+0x3c0` × 4 — 이름 `[추측: ActiveProcessorCount]`) 이고 512 미만이면 512 (`+0x2a5 ~ +0x2c0`), **스택 = exe 헤더 `SizeOfStackReserve` / `SizeOfStackCommit`** (`+0x76` · `+0x7a`). → 새 일꾼은 **`RtlUserThreadStart(TppWorkerThread, 풀)`** 로 시작한다 (첫 문맥은 run74 `NtCreateThreadEx +0x1bf ~ +0x236` 과 같음).
- **`CreateFlags` → TEB 비트** (run74 표와 잇기): `0x10` → 만들기 `0x80` → `SameTebFlags.LoaderWorker` (`0x2000`). `0x2` (`SkipThreadAttach`) · `0x20` (`SkipLoaderInit`) 은 **이 길에서 안 켜진다**.
- **어느 공장이 bit 11 을 켜나** — flow-writer 잇기 `[정적 사슬]`: 공장 bit 11 ← 설정 클래스 `0xd` 값 bit 0 (`Set +0x2c6 AND ECX,1` · `+0x2c9 SHL ECX,0xb` · `+0x2d8`). ntdll 은 **로더 풀에만** 클래스 `0xd` 를 값 `[RSP+0xb8]` 로 부르고 (`TpAllocPoolInternal +0x434 LEA R8,[RSP+0xb8]` · `+0x443`), 그 칸은 둘째 인자 `& 1` = 1 이다 (run69 `[정적]`). → **로더 풀 일꾼 = `CreateFlags 0x11` → `LoaderWorker`, 기본 풀 일꾼 = `0x1`** (기본 풀은 `0xd` 를 안 부름, run69). 결과서는 이 값을 "동적" 으로 남겼는데 run69 가 이미 정적으로 정했다 (아래 "flow-writer 대조").

### 4. 일감 받기 — `NtWaitForWorkViaWorkerFactory` (rva `0x32ae60`, 2887B) [정적]

인자: `(핸들, 패킷 배열, Count, &꺼낸 수, DeferredWork)`.

| 단계 | 줄 | 내용 |
|---|---|---|
| 검사 | `+0x10d ~ +0x116` | `Count` 1 ~ `0x7ffffff`, 아니면 `0xc000000d` |
| 사용자 모드 | `+0x125 ~ +0x18c` | 배열 `Count × 0x20` 쓰기 검사, 꺼낸 수 4바이트, **`DeferredWork` 8 정렬 아니면 `ExRaiseDatatypeMisalignment`** (`+0x173`), 0x18 바이트 복사 |
| 핸들 | `+0x20d` | 접근 2 |
| 임시 배열 | `+0x227 ~ +0x22b`, `+0xbd6 ~ +0xc16` | `Count > 0x10` 이면 풀 할당 (실패면 16 으로 줄여 스택 배열) |
| 마감 | `+0x284` | `P+0x21 ≠ 0` → **`0x80`** |
| 멀리 있기 끄기 | `+0x28e ~ +0x2cb` | bit 9 · 10 이면 포트 알림 떼기 (`+0x2ab`, `KiDeregisterObjectWaitBlock` — PDB C++ 이름 하나) |
| **기다림 표시** | `+0x2d8` | **`P+0x1c` ++** |
| 루프 머리 | `+0x2ff ~ +0x30e` | 최대 < 움직이는 수, 또는 마감 → **바로 줄이기 (`0x102`)** |
| **꺼내기** | `+0x684 ~ +0x6ba` | **`IoRemoveIoCompletion(포트 [P+8], 배열, 임시, Count, &꺼낸 수, PreviousMode, 시간 제한 NULL, 깨울 수 있음 1)`** |
| 뒤처리 | `+0x6ca` | `ExpWorkerFactoryFinishDeferredWork` |
| 성공 | `+0x8bf` / `+0x8fa` | 꺼낸 수를 씀. 상태 ≠ 0 이면 안 씀 (`+0x8b3 ~ +0x8b6`) |

- **출력 한 칸 = `0x20` 바이트**: `{ KeyContext, ApcContext, IoStatus 상태, IoStatus 정보 }` (`IoRemoveIoCompletion +0x204 ~ +0x20d`). 한 번에 `Count` 개까지.
- **풀어주기 패킷이 일꾼 눈에**: 패킷 종류 4 → `IoRemoveIoCompletion +0x1d1 ~ +0x1ec` 에서 네 칸 복사 뒤 `IopFreeMiniCompletionPacket` → 콜백 `ExpWorkerFactoryCompletionPacketRoutine` (`+0xac`). → **일꾼은 "네 칸이 모두 0 인 항목 1개" 를 받는다.**
- **일감이 없으면 시간 제한 없이 잠든다** (`P+0x1c` 올린 채). 놀기 제한은 이 호출 인자가 아니다 — 관리 스레드가 따로 깨운다.
- **끝내는 신호** (끝내는 건 ntdll 쪽 일, `[추측]`):

| 상태 | 언제 | 줄 |
|---|---|---|
| `0x80` | 들어올 때 이미 마감 | `+0x284` → `+0xbc6` |
| `0x102` ① | 루프 머리: 최대 < 움직이는 수 (클래스 5 로 낮춤) 또는 마감 | `+0x2ff ~ +0x30e` → `+0x932` |
| `0x102` ② | 놀기 제한 지남 (관리 스레드 `KeTimeOutQueueWaiters`) **이고** `WantsToCreate(f, 1)` 거짓 · 움직이는 수 > 최소 · 이 스레드 `IrpList` 비었음 — 하나라도 아니면 다시 기다림 | `+0x726` → `+0xaa5 ~ +0xaf6` |
| 줄이기 뒤처리 | `P+0x1c` -- · `f+0x180` -- · `f+0x184` -- · 기록판에서 뺌 | `+0x730`, `+0xb0f` · `+0xb15`, `+0xb1e` |

- **일을 집은 뒤 연쇄** (`+0x72c ~ +0x7bf`): `P+0x1c` -- → 기록판에 넣음 → 움직이는 수 < 최대 · 기다리는 일꾼 0 이면 `CheckCreate` (`+0xa5d`) → **일이 쌓여 있으면 일꾼이 하나씩 늘어난다.**
- 놀기 제한 = `ExpWorkerFactoryThreadIdleTimeoutInSeconds` (파일 초기값 67초, `1 ~ 600` 으로 자름). 관리 스레드가 공장 타이머 알림을 받으면 `f+0x190 == 0` · `f+0x180 > f+0x178` 일 때 `KeTimeOutQueueWaiters(포트, -f+0x70, f+0x180 - f+0x178)` (`Manager +0x115`). 레지스트리로 덮는 표를 읽는 함수는 `[공백]`.
- **마감 길**: `NtShutdownWorkerFactory +0x99` · `ExpCloseWorkerFactory +0xd` (마지막 핸들) → `ExpShutdownWorkerFactory`: `P+0x21 = 1` (`+0xad`), 최대 · 최소 0 (`+0xb5` · `+0xbc`), 기다리는 일꾼이 있으면 패킷을 넣어 (`+0x13b`) 한 명씩 연쇄로 깨움 — 깨어난 일꾼은 0 패킷을 받고 성공으로 돌아가고, **다음 `Wait` 가 `0x80`**.

### 5. 설정 클래스 4 · `0xb` — 그리고 Q134 (b) 의 클래스 표 [정적]

점프 표 셋 (결과서가 `ntoskrnl.exe` 바이트에서 풀음): A (클래스 → 길이 검사, rva `0x3a77ac`, 16칸) · B (읽기, `0x3a773c`, 14칸, 클래스 − 2) · C (락 안 몸, `0x3a7774`, 14칸, 클래스 − 2).

- **클래스 4** — A[4] `+0x77` (4바이트) → B[2] `+0xeb` → C[2] **`+0x547`**.
  1. `P+0x21` 이면 `0x80` (`+0x54c ~ +0x55a`).
  2. 새 값 > 옛 `f+0x178` 이면 "올렸다" 표시, **`f+0x178 = 새 값`** (`+0x6df ~ +0x6f6`, 값 검사 없음).
  3. 최대 `f+0x17c` 가 새 최소보다 작으면 최대를 끌어올림 (`+0x6fc ~ +0x702` → `+0x86d`).
  4. 안 올렸으면 끝 (`+0x708`) — **같거나 줄인 값은 칸만 바꾸고 상태 0**.
  5. 올렸으면 bit 9 끄고, **`f+0x188 + f+0x180 < f+0x178` 인 동안** `f+0x190` ++ → `+0x74f ExpWorkerFactoryCreateThread` (루프 `+0x724 ~ +0x768`). 실패면 `f+0x190` -- 하고 그 상태 반환.
  - → **최소를 N 으로 올리면 그 호출 안에서 모자란 만큼 한꺼번에 만든다.** 일감 · CPU 수 · 기록판 검사 없음, 상한 없음.
  - 이름 `[추측: 공개 헤더 WorkerFactoryThreadMinimum]`.
- **클래스 `0xb`** — A 를 안 거친다: `+0x67 CMP EBX,0xb` · `+0x6a JNZ` 로 `0xb` 만 `+0x70` 으로 (A[0xb] 칸은 잘못된 클래스 `0xc0000003`). 4바이트 검사 → B[9] → C[9] **`+0x780`**.
  - **`f+0x19c = 입력`** (`+0x785 ~ +0x791`, 값 검사 없음), 상태 = 마감이면 `0x80` 아니면 0 (`+0x797 ~`). **마감이어도 칸은 이미 씀.**
  - 스레드 일 없음. `CreateThread +0x124 ~ +0x13d` 가 `f+0x19c ≠ 0` 이면 **앞으로 만드는 일꾼에만** `ZwSetInformationThread(3, …, 4)`.
  - 이름 `[추측: WorkerFactoryThreadBasePriority]`.
- 둘 다 길이 4 아니면 `0xc0000004` (`+0x82 ~ +0x85`), 정렬 검사 없음, 핸들 접근 4 (`+0x20e`).

**Q134 (b) — 클래스별 몸** (C 표 · 직접 갈래, 이름은 모두 `[추측]`):

| 클래스 | 이름 `[추측]` | 몸 | 한 줄 `[정적]` |
|---|---|---|---|
| 2 | 놀기 제한 | C[0] `+0x676` | `f+0x70` ← 값, `KeSetTimer2` (`+0x6d5`) — run69 "유휴 시간" 맞음 |
| 3 | 묶음 수 | C[1] `+0x3ff` | `f+0x194` ± 값, 0 → ≠ 0 이면 `TryEnterAwayMode` (`+0x582`) → `CheckCreate` (`+0x59a`) |
| 4 | 최소 스레드 | C[2] `+0x547` | 위 |
| 5 | 최대 스레드 | C[3] `+0x494` | `f+0x17c` ← 값 (`+0x4cd`), 위 표의 조건이면 일꾼 1 직접 (`+0x513`) — run69 "최대 스레드" 맞음 |
| 6 | | A[6] `+0x3f5` | `0xc0000002` (구현 안 됨) |
| 8 | 목표 조정 | `+0x21b → +0x836` | 락 없이 포트 `MaximumCount` ← 값 (0 이면 CPU 수). `TppAdjustRunningThreadGoalWithLock` 이 부름 (run72) |
| 9 | 콜백 종류 | `+0x27b ~ +0x2de` | `f+0x180` ± 1 — 몸은 안 읽음 |
| `0xa` | 스택 정보 | C[8] `+0x7cf` | `f+0x38` · `f+0x40` (16바이트) |
| `0xb` | 기본 우선순위 | C[9] `+0x780` | 위 |
| `0xc` | 놀기 일꾼 바로 줄이기 | C[10] `+0x60b` | `KeTimeOutQueueWaiters` 시간 0 (`+0x63b`) |
| `0xd` | 플래그 | `+0x2b4` | **`f+0x198` bit 11 ← 값 bit 0** (`+0x2c6 ~ +0x2d8`) = 일꾼을 `LoaderWorker` 로 |
| `0xe` | 부드러운 최대 | C[12] `+0x5b3` | `f+0x18c` (`+0x5c6`) — 걸림 ③ 에 씀 |
| `0xf` | CPU 집합 | C[13] `+0x8b2` | bit 15 (일꾼을 한 번이라도 만듦) 면 `0xc000a003`, 아니면 `f+0x78` 복사 · bit 14 |
| 0 · 1 · 7 | | A | `0xc0000003` |

- 길이는 run69 표 (2 = 8 · `0xa` = `0x10` · `0xf` 가변 · 나머지 4) 와 같다.

### 6. 공장 칸 (요약) [정적, 이름은 `[추측]`]

몸 `0x2a0` (`NtCreateWorkerFactory +0x1b4` → `+0x1c5 ObCreateObjectEx`) + 작은 덩어리 `P` (0x28 바이트, 꼬리표 `TpWc`, 0 으로 채운 할당). 칸 하나하나 (쓰는 곳 · 읽는 곳 줄) · 상태 비트 16개는 결과서 "공장 객체 칸 표".

| 칸 | 뜻 |
|---|---|
| `f+0x10` | `P` 포인터 |
| `f+0x18` · `+0x20` | 시작 주소 · 시작 인자 |
| `f+0x28` · `+0x30` | 프로세스 커널 핸들 · 프로세스 객체 |
| `f+0x38` · `+0x40` | 스택 예약 · 커밋 |
| `f+0x48 ~ +0x67` | 최근 일꾼 4명 기록판 |
| `f+0x70` | 놀기 제한 (음수 100ns, 기본 −67초) |
| `f+0x178` · `+0x17c` | 최소 · 최대 스레드 |
| `f+0x180` · `+0x184` | 움직이는 일꾼 수 (최대와 견줌) · 전체 일꾼 수 (Query 가 보고) |
| `f+0x188` · `+0x190` | 만들었지만 `Ready` 전 수 · 만드는 중 수 |
| `f+0x18c` · `+0x194` | 부드러운 최대 · 묶음 수 |
| `f+0x198` | 상태 비트 (bit 3 미룸 · 4 ~ 8 · 12 ~ 13 걸림 · 9 · 10 멀리 있기 · **11 `LoaderWorker`** · 14 CPU 집합 · 15 한 번이라도 만듦) |
| `f+0x19c` · `+0x1a0` | 일꾼 기본 우선순위 · 마지막 만들기 상태 |
| `P+0x00` · `+0x08` · `+0x10` | 락 · 완료 포트 (`_KQUEUE`) · 풀어주기 패킷 |
| `P+0x18` · `+0x1c` | **넣은 일감 수** · **기다리는 일꾼 수** |
| `P+0x20` · `+0x21` · `+0x22` | 패킷이 포트에 있음 · **마감** · 지우는 중 |

- **일꾼 수 칸의 한살이**: `CheckCreate` 가 `+0x190` ++ → `CreateThread` 가 `+0x188` ++ → 새 일꾼이 **`NtWorkerFactoryWorkerReady`** (접근 `0x10`) 를 부르면 `+0x190` --, `+0x188` --, `+0x180` ++, `+0x184` ++ (`+0x66 ~ +0x90`; `+0x190 == 0` 이면 `0xc0000001`, `+0x188 == 0` 이면 `0xc000010a`). 줄일 때 `Wait` 가 `+0x180` · `+0x184` 를 내림.
- PE_emulator 의 `TODO(VM)` "`[[공장+0x10]+0x21]` 바이트 뜻" → **마감 (`Shutdown`) 바이트**, 새 공장은 0 (결과서).

### 7. 일감을 넣었는데 일꾼이 0 이면 — 한 장 [정적]

새 풀의 첫 `TppWorkPost` (`NtReleaseWorkerFactoryWorker(풀+0x38)` 한 번) 기준:

1. **같은 syscall 안에서** `P+0x18 = 1`, `P+0x20 = 1`, 포트에 0 패킷 1개 (`SignalState = 1`). 기다리는 일꾼이 없어 줄에 붙임.
2. `CheckCreate(f, 0, 0)` — 위 조건 일곱 모두 참.
3. `f+0x190 = 1` → `ExpWorkerFactoryCreateThread`: 같은 프로세스에 **멈춘 채 만들고 바로 풂**. `f+0x188 = 1`. 넣은 스레드는 그 뒤 syscall 에서 돌아온다.
4. 새 일꾼 (ntdll 쪽): `NtWorkerFactoryWorkerReady` → `f+0x190 = 0` · `f+0x188 = 0` · `f+0x180 = f+0x184 = 1` → `NtWaitForWorkViaWorkerFactory` 가 0 패킷을 꺼냄 (콜백이 `P+0x18 = 0` · `P+0x20 = 0`) → 풀의 일감 목록을 돎 `[추측: TppWorkerThread 안은 안 읽음]`.
5. `Ready` 전에 둘째 넣기가 와도 일꾼을 더 안 만든다 (`f+0x190 ≠ 0`, 그리고 ntdll 은 둘째 넣기부터 syscall 이 없음 — run72). 일꾼이 일을 집은 뒤 일이 남고 기다리는 일꾼이 없으면 **하나씩** 더 만든다. CPU 수만큼 이미 일하면 30 ms → 120 ms 미룬 뒤.

### 8. test.exe 와 맞물리는 곳

- `[정적]` (run72 + run125) **로더 풀 첫 넣기가 일꾼을 만드는 계기다.** `TppWorkPost +0x4f3` → 커널 `NtReleaseWorkerFactoryWorker +0x201` → `CheckCreate +0x19b` → `CreateThread`. run72 의 `[추측]` 이 닫힌다.
- `[정적 사슬]` (run69 + run125 + run74) 로더 풀 일꾼은 `CreateFlags 0x11` → `LoaderWorker` (`0x2000`), 기본 풀 일꾼은 `0x1` (TEB 비트 없음). 둘 다 `SkipThreadAttach` · `SkipLoaderInit` 은 이 길에서 안 켜진다.
  - run8 `[관찰]` "일꾼에서 `LdrInitializeThunk` 만, DllMain · TLS 콜백 안 불림" 과 run17 `[추측]` "`LoaderWorker` 면 `_LdrpInitialize` 가 첫 스레드를 안 기다림" 이 로더 풀 일꾼 쪽이면 맞물린다 `[추측: 일꾼 2개가 어느 풀 것인지는 동적 그대로]`.
- `[추측]` run44 `[관찰]` 일꾼 2개 (P1a ~ P1b, `ActiveThreads` 1 → 3) 와 어긋나지 않는다: 첫 넣기 1명 + 일을 집은 뒤 연쇄 (`Wait +0xa5d`) 1명이 한 모양 (로더 풀 최대 3, run69). 다른 모양 (둘째 공장 · 미룸) 도 지울 수 없다 → 동적.
- `[추측]` (run62 + run125) 기본 풀을 만드는 `+0xbe2` 구간에서는 일꾼이 안 생길 것: `NtCreateWorkerFactory` 는 일꾼을 안 만들고, 그 뒤 클래스 5 (옛 최대 `0x200` ≠ 0) · 클래스 3 (`TryEnterAwayMode` 는 포트가 비면 거짓) 은 포트에 일이 없으면 만들기로 안 간다. run44 P1a `ActiveThreads` 1 `[관찰]` 과 맞는다. "그때 포트가 비었나" 는 안 봤다.

## 확인된 것

- `[정적]` `NtReleaseWorkerFactoryWorker` 는 기다림 없이 칸 갱신 · 0 패킷 넣기 · `+0x201 ExpWorkerFactoryCheckCreate` 를 한다. 패킷이 이미 포트에 있으면 `+0x106` 에서 끝 (일꾼 검사 없음). (`.asm`, `_calls.tsv`)
- `[정적]` 일꾼 만들기 조건 일곱 (`CheckCreate +0x8b ~ +0x16c`), 새 공장의 첫 넣기는 바로 만든다. 미룸은 CPU 수 · 기록판 · 부드러운 최대 · 만들기 실패일 때만.
- `[정적]` 일꾼은 `ExpWorkerFactoryCreateThread +0xe5 RtlpCreateUserThreadEx` → `+0x14c ZwCreateThreadEx` 로, 사용자 `CreateThread` 와 같은 `NtCreateThreadEx` 길로 만들어진다. 시작 · 인자 · 스택은 공장 `+0x18` · `+0x20` · `+0x38` · `+0x40`. 만들기 플래그 `0x1` / `0x11`, 같은 호출 안에서 `+0x153 ZwResumeThread`.
- `[정적]` `NtCreateWorkerFactory` 는 일꾼을 안 만든다. 공장을 만드는 스레드의 프로세스 밖으론 못 만든다 (`0xc000000d`).
- `[정적]` `NtWaitForWorkViaWorkerFactory` 는 `IoRemoveIoCompletion` 을 시간 제한 없이 부른다. 출력 `0x20` × `Count`. 마감 `0x80`, 줄이기 `0x102`.
- `[정적]` 설정 클래스 4 = `f+0x178`, 올리면 그 자리에서 일꾼을 만든다. 클래스 `0xb` = `f+0x19c`, 앞으로 만들 일꾼에만. 둘 다 입력 4바이트.
- `[정적]` Q134 (b): 클래스 2 · 3 · 5 · `0xd` (+ 6 · 8 · 9 · `0xa` · `0xc` · `0xe` · `0xf`) 의 몸 자리 · 칸 (위 5). 공장 bit 11 = `LoaderWorker` 요청. 이름은 PDB 에 없어 `[추측]`.

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- **`ExpRemoveCurrentThreadFromThreadHistory` call 자리**: 결과서 3-3 표 "call `+0xb1b` (ret `+0xb23`)" → `+0xb1b` 는 `MOV RCX,RSI`, CALL 은 **`+0xb1e`** (ret `+0xb23`, `_calls.tsv` 같음).
- **"`+0x284 JNZ +0xbbc`"** (결과서 Q3 여러 곳): `+0x284` 는 `CMP byte [RCX+0x21],0`, 점프는 그 다음 줄. 뜻 같음.
- **결과서 "동적으로 확인할 것" 3** (ntdll 이 클래스 `0xd` 에 넣는 값 — "동적 값 필요"): 값 칸 `[RSP+0xb8]` (`TpAllocPoolInternal +0x434 LEA R8`) 은 run69 `[정적]` 이 "둘째 인자 `& 1`" 로 이미 정했다 (로더 풀 1 · 기본 풀은 갈래를 안 탐). → 로더 풀 bit 11 = 1 은 **정적으로 닫힘**. 동적은 확인용으로만 남김.

## 정정

- **run54** (`runs/03_sprint/run54-teb-fill.md`) 의 "`SkipThreadAttach` · `LoaderWorker` · `SkipLoaderInit` … 로더 일꾼 (`NtCreateThreadEx` 길, Q79) 에서만 켜짐" → 일꾼 공장 길에서 켜질 수 있는 건 **`LoaderWorker` 하나** (`CreateFlags` `0x1` / `0x11`). `SkipThreadAttach` (`0x2`) · `SkipLoaderInit` (`0x20`) 은 그 플래그를 직접 넘기는 `NtCreateThreadEx` 호출자에서만. 문서 맨 위에 한 줄.
- **run69** (`runs/03_sprint/run69-loader-pool.md`) — 정정 아님, **닫힘**: "커널 클래스 2 · 3 · 5 · `0xd` 갈래 (Ghidra 가 점프표를 못 풂) 와 일꾼 공장 `+0x198` bit 11 의 뜻" (= Q134 (b)) 과 "커널 일꾼 공장 값 — 읽을 타입 이름이 PDB 에 있는지부터 `[공백]`" (동적 5 → PDB 에 없음, `dq` 로 위 칸 표 오프셋). 클래스 2 = 유휴 시간 · 5 = 최대 스레드 `[추측]` 은 칸 동작으로 `[정적]` 받침 (이름은 그대로 `[추측]`). 맨 위에 한 줄.
- **run72** (`runs/03_sprint/run72-pool-work-post.md`) — **닫힘**: "커널 `NtReleaseWorkerFactoryWorker` 본문 — 첫 넣기가 일꾼을 새로 만드는 계기인지 `[추측]`" (= Q139 (d)) → **맞다 `[정적]`**. 맨 위에 한 줄.
- **run62** (`runs/03_sprint/run62-tp-etw-init.md`) — 정정 아님, **보탬**: D10 ("WNF 두 syscall 직후 일꾼 스레드가 생기나") 의 정적 기대 = 안 생김 `[추측: 그때 포트가 비었다면]` (위 8 마지막 줄). 맨 위에 한 줄.

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 쓰는 곳 (결과서가 본 줄) | 맞나 | 모자란 것 / 고칠 곳 |
|---|---|---|
| `ext/syscall/syscall_layer_cpp/src/threadpool/nt_release_worker_factory_worker.cpp:15` | 핸들 · 접근 1 **맞음**, 그 뒤 멈춤 → **틀림 (모자람)** | 마감 → `0x80`, `P+0x18 == -1` → `0xc0000001`, `P+0x18` ++, `P+0x20` 이 0 이면 0 패킷을 포트에 넣기, **일꾼 0 · 조건 맞으면 일꾼 1 만들기** (아래 다리). 코드 주석 "일꾼을 깨우거나 만드는 계기일 수 있다 [추측]" 은 **맞다** |
| `nt_worker_factory_worker_ready.cpp` | 접근 `0x10` · `+0x190 == 0 → 0xc0000001` **맞음** | 일꾼을 만들게 되면 `+0x188 == 0 → 0xc000010a` 갈래와 칸 셋 갱신 |
| `nt_create_io_completion.cpp:145` 근처 (`NtCreateWorkerFactory`) | 프로세스 ≠ 현재 → `0xc000000d` · 스택 기본 · 최대 저장 **맞음** | 놀기 제한 (`f+0x70`), `P` 덩어리 (포트 · 패킷 · 마감 바이트) — Release 를 흉내 내려면 필요 |
| `Src/nt/TeamSyscall.cpp:1062` (`PspCreateThread` 다리) | 모양 **맞음** | 공장 쪽에서 `start = f+0x18`, `arg = f+0x20`, `stack_size = f+0x40` (커밋), `maximum_stack_size = f+0x38` (예약), `CreateFlags 0x1` (bit 11 이면 `0x11`) 로 부르고 같은 호출 안에서 풀기. `:1058` `TODO(VM): CreateFlags → PspCreateThread 플래그` 는 run74 표 (`0x10 → 0x80`) 가 답 |
| `nt_wait_for_work_via_worker_factory.cpp` | `Count` 범위 · 배열 · 정렬 · 마감 `0x80` · 접근 2 **맞음** | ① `DeferredWork` 정렬 아님 → `ExRaiseDatatypeMisalignment` (상태 `0x80000002` 로 돌려줌 `[추측]`) — 지금 `stop` ② **큐가 비면 `stop` 은 틀림** — 진짜는 시간 제한 없이 잠듦 (`P+0x1c` ++) ③ 0 패킷이 같은 큐에 들어와야 깨어남 ④ `0x102` 줄이기 길 없음 |
| `worker_factory_set_length(cls)` | 클래스 4 · `0xb` 빠짐 (0 = 멈춤) | **`4 → 4`, `0xb → 4`** 더하기. `0xb` 는 지금 바로 모델 가능 (`f+0x19c` 저장, 마감이면 `0x80`). 4 는 "옛 최소보다 안 크면 칸만 · 상태 0" 까지 가능, 올리면 일꾼 만들기가 필요 |

- 요약: 진짜 커널은 **"일감을 넣으면 일꾼이 0 일 때 같은 호출 안에서 일꾼 하나를 만들어 깨운다"**. 지금 "아무것도 안 해서 모두 잠드는" 건 진짜와 다르다. 최소로 필요한 셋: (a) 칸 갱신 (b) 포트에 0 패킷 (c) 조건 맞으면 `PspCreateThread` 다리 한 번. 미룸 (30 / 120 ms) · 기록판 · 부드러운 최대 · 놀기 제한은 나중.

## 아직 확인 필요

동적 (결과서 "동적분석으로 확인할 것" 1 ~ 9 — 정적으로는 이 길에 간접 호출이 없다. 대기열 번호는 오케스트레이터가 붙임. 모두 test.exe 조건):

1. 첫 `NtReleaseWorkerFactoryWorker` 가 그 자리에서 만드나 — `nt!ExpWorkerFactoryCreateThread` 입구 `k` 에 `ExpWorkerFactoryCheckCreate+0x1a0` · `NtReleaseWorkerFactoryWorker+0x206` 이 보이나. 대안 `nt!ZwCreateThreadEx` 입구.
2. `nt!NtCreateThreadEx` 입구의 `CreateFlags` (7번째, `[rsp+0x38]`) · `StackSize` (9번째) · `MaximumStackSize` (10번째) — 로더 풀이면 `0x11` (flow-writer 기대, 위 3), exe 헤더 크기와 같은지.
3. (확인용) 로더 풀 공장 bit 11 — run69 정적으로 1 (위 "flow-writer 대조"). `TpAllocPoolInternal +0x443` 에서 `dd @r8 L1`.
4. `dq nt!ExpWorkerFactoryThreadIdleTimeoutInSeconds L1` · `…CreationTimeoutInSeconds` — 파일 값 67 · 10, 레지스트리로 덮일 수 있음.
5. 공장 칸 움직임 — `f+0x178 ~ +0x1a0` · `P` 를 Release 직후 / `Ready` 직후 / 첫 `Wait` 직후. `f+0x180` 과 `+0x184` 가 늘 같은지 (클래스 9 때문에 갈릴 수 있음 `[추측]`).
6. CPU 수 걸림 (①) 이 이 VM 에서 걸리나 (`KeNumberProcessors` · 포트 `CurrentCount`). 걸리면 30 ms 타이머 · 관리 스레드.
7. `ExpCheckThreadHistory` 반환값 — `CheckCreate +0x14d` 에서 `al`. 뜻은 `[공백]`.
8. 첫 `Wait` 가 돌려주는 항목이 네 칸 0 인지 (`IoRemoveIoCompletion +0x204 ~ +0x212`).
9. 일꾼이 `0x80` · `0x102` 를 받으면 ntdll 이 스레드를 끝내는지.
- (flow-writer 보탬) run44 일꾼 2개가 어느 풀 · 어느 계기 (첫 넣기 / 연쇄 / 미룸) 로 생겼나 — 1 의 멈춤점을 hit 수와 함께. run62 D10 · Q47 과 같이 볼 수 있음.

정적 (결과서 "다음 요청 후보" + 보탬):

1. ntdll `TppWorkerThread` (3824B) + `TpSetPoolMinThreads` (156B) · `TpSetPoolThreadBasePriority` (77B) — 0 패킷을 어떻게 알아보나, `Ready` · `Wait` 차례, `0x80` · `0x102` 에 끝내는 길, 클래스 4 · `0xb` 를 누가 어떤 값으로 부르나 (에뮬레이터 로그의 "5 → 4 → 3" 출처).
2. `KiWakeQueueWaiter` · `KeRemoveQueueEx` · `KeTimeOutQueueWaiters` · `KiInsertQueue` — 포트 `CurrentCount` · `SignalState` 가 언제 오르내리나 (걸림 ① 의 입력).
3. `NtSetInformationWorkerFactory` 클래스 9 몸 (`+0x27b ~ +0x2de`, `+0x8e0 ~ +0x910`, `+0x459 ~ +0x4e6`).
4. (낮음) `ExpCheckThreadHistory` 뜻 (걸림 ②) · `Wait +0x740 ~ +0x7a3` 기록판 채우기 규칙.
5. (작음) `NtQueryInformationWorkerFactory` 출력 `0x78` 칸 매핑 — 결과서 "다음 요청 후보" 5 에 이미 다 있음, 에뮬레이터가 바로 맞출 수 있음.
6. (flow-writer 보탬, 낮음) 레지스트리 `Session Manager\Executive` `WorkerFactoryThreadIdleTimeout` · `WorkerFactoryThreadCreationTimeout` 표 (rva `0xc81f50 ~ 0xc81f80`) 를 읽는 함수.

결정할 것 (flow-writer 제안):

- **그림 2 에 커널 쪽 일꾼 만들기 (`nt!NtReleaseWorkerFactoryWorker` → `ExpWorkerFactoryCheckCreate` → `ExpWorkerFactoryCreateThread`) 를 넣을지** — 첫 넣기가 test.exe 초기화 안이라 대표 지점 후보. 이번엔 안 넣음 (오케스트레이터 지시: 3장 · 4장 · 5장만).
- **짐 지도 7.6.12 커널 줄** (`(커널) 로더 풀 일꾼 공장 — "풀어 줄 수"`, 지금 `[추측]` · 커널 칸 `[공백]` (Q134 (b))) 과 3장 `(run72) 로더 풀 +0x38 WorkerFactory` 줄을 run125 칸 (`P+0x18` · `P+0x20` · 포트 `SignalState` · 공장 `+0x188` · `+0x190`) 으로 채울지 — 다음 짐 지도 바퀴 후보.

## 디버거 메모 / 도구 메모

- 이번엔 디버거를 안 썼다.
- **디버거가 붙어서 달라지는 점 (예상)**: 일꾼 만들기 미룸은 시간 (30 / 120 ms 타이머) 과 포트 `CurrentCount` 에 달려 있다. 커널 멈춤점에서 오래 서 있으면 다른 일꾼이 `CurrentCount` 를 붙들고 있거나 타이머가 먼저 터져, **"바로 만듦" 이 "미뤄 만듦" 으로 바뀔 수 있다** `[추측]`. 동적 1 · 6 은 조건부 멈춤점 + 바로 `g`.
- 일꾼 공장 구조체 타입이 PDB 에 없어서 `dt nt!_…` 로는 못 읽는다 — `dq` / `dd` 로 위 오프셋을 직접.

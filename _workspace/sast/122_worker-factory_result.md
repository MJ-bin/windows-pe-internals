# 122 worker-factory — 정적 분석 결과 (일꾼 공장, 질문 4개)

- 요청서: `_workspace/sast/122_worker-factory.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). `NtCreateWorkerFactory` 에 넘기는 인자 값을 보려고 ntdll `TpAllocPoolInternal.asm` (이미 있던 것) 한 곳만 읽음.
- PDB GUID: `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) — **기준값과 같음**.
- 분석: **이미 돼 있음** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄). **다시 analyze 안 함.** sast/121 (mswsock·afd·nsi) 이 같은 시간에 돌아서 ntoskrnl 은 **읽기 전용 `export` 만, 모두 `^이름$` depth 0** 로 4번 돌림 (프로젝트 잠금 충돌 없음, 4번 모두 rc=0, 서버 메모리 여유 22GB).
- 뽑은 함수: **새로 28개** (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`)
  - 요청서 대상 14: `NtReleaseWorkerFactoryWorker` · `NtWorkerFactoryWorkerReady` · `NtWaitForWorkViaWorkerFactory` · `NtCreateWorkerFactory` · `ExpWorkerFactoryCheckCreate` · `ExpWorkerFactoryWantsToCreate` · `ExpWorkerFactoryCreateThread` · `ExpWorkerFactoryDeferredThreadCreation` · `ExpSetWorkerFactoryDeferredCreateTimer` · `ExpWorkerFactoryManagerThread` · `ExpWorkerFactoryCompletionPacketRoutine` · `ExpTryEnterWorkerFactoryAwayMode` · `ExpLeaveWorkerFactoryAwayMode` · `ExpWorkerFactoryFinishDeferredWork`
  - **넓힌 것 14 (이유 있음, 모두 작은 함수 depth 0):**
    - `RtlpCreateUserThreadEx` (423B) — `ExpWorkerFactoryCreateThread` 가 부르는 "스레드 만들기 호출" 그 자체 (질문 2).
    - `NtQueryInformationWorkerFactory` (634B) — 공장 칸 뜻을 **공개 헤더 구조 (0x78 바이트) 와 칸 하나씩 맞춰 보려고** (칸 표의 근거).
    - `ExpCheckThreadHistory` · `ExpInitializeThreadHistory` · `ExpRemoveCurrentThreadFromThreadHistory` — `CheckCreate` 조건 식 안에 나오는 도우미 (조건을 끝까지 쓰라고 하셨음).
    - `ExpWorkerFactoryInitialization` — 미루기 타이머 값·관리 스레드가 언제 생기나.
    - `ExpShutdownWorkerFactory` · `ExpDeleteWorkerFactory` · `ExpCloseWorkerFactory` · `NtShutdownWorkerFactory` — 질문 3 "마감 때 일꾼이 스스로 끝나는 길" (`P+0x21`·`P+0x22` 를 누가 쓰나).
    - `IopFreeMiniCompletionPacket` · `IoFreeMiniCompletionPacket` · `IoAllocateMiniCompletionPacket` · `IopAllocateMiniCompletionPacket` — "풀어주기 패킷"이 일꾼 눈에 어떻게 보이나 (콜백 연결). 완료 포트 내부는 이 정도만.
  - 있던 것 읽기만: `NtSetInformationWorkerFactory.asm` · `NtCreateThreadEx.asm` · `IoRemoveIoCompletion.asm` · ntdll `TpAllocPoolInternal.asm`.
  - 전역 읽고 쓰기 `refs` 2장: `ExpWorkerFactoryThreadIdleTimeoutInSeconds` · `ExpWorkerFactoryThreadCreationTimeoutInSeconds` (`_refs/`). 점프 표·초기값은 작은 스크래치 파이썬 (`.../scratchpad/rva.py`, PE 구역 → 바이트 읽기, 커밋 안 함) 로 읽음.
- 버전 확인 (`_calls.tsv` `ret_off`, 스킬 "알려진 사실"): **일치** — `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` call `+0x802` / ret `+0x807` → `PspCallProcessNotifyRoutines`.
- 스크립트 문제: 없음.
- 오프셋 약속: `call +0x..` = CALL 자리, `ret +0x..` = 다음 줄, 그냥 `+0x..` = 그 명령 자리. 점프 목적지는 "목적지 주소 − 함수 시작" 으로 셈.
- 표시: `[정적]` 코드에 있는 것 · `[추측]` · `[공백]`. **PDB 에는 일꾼 공장 구조체도 정보 클래스 열거도 없다** (`llvm-pdbutil` types 에 `WorkerFactory` 0건). 그래서 칸·클래스 **이름**은 공개 헤더 (phnt `WORKER_FACTORY_BASIC_INFORMATION` · `WORKERFACTORYINFOCLASS`) 와 맞춘 것 = `[추측: 이름]`, **칸 위치·동작은 `[정적]`**.

---

## 한눈에 — 질문 4개 + 특별 질문

| # | 질문 | 한 줄 답 | 지금 우리 (PE_emulator) |
|---|---|---|---|
| 1 | Release 가 바꾸는 칸 · 일꾼 만드는 조건 | **`P+0x18` (넣은 횟수) 를 1 올리고, 0 으로 채운 "풀어주기 패킷" 하나를 완료 포트에 넣고 (이미 들어 있으면 안 넣음), 곧바로 `ExpWorkerFactoryCheckCreate` 를 부른다.** 조건이 맞으면 **그 호출 안에서 바로** 스레드를 만든다 (미룸은 제한에 걸릴 때만). **첫 넣기가 계기 = 맞다.** | **틀림 (모자람).** 핸들·접근 검사까지만 맞고 그 뒤를 멈춤 |
| 2 | 일꾼 만드는 길 · 시작 주소·인자·스택 | `CheckCreate +0x19b` → `ExpWorkerFactoryCreateThread` (`+0xe5` → `RtlpCreateUserThreadEx`, 그 `+0x14c` → `ZwCreateThreadEx`) → **`NtCreateThreadEx` (같은 길)** → `PspCreateThread` … 시작 = 공장 `+0x18`, 인자 = `+0x20`, 스택 예약 = `+0x38` (기본 0x10000), 커밋 = `+0x40` (기본 0x1000). `CreateFlags = 0x1` (멈춘 채로), 공장 `+0x198` 비트 11 이 켜졌으면 `0x11`, 만든 뒤 바로 `ZwResumeThread`. **공장 만든 프로세스에서만 돈다.** | 길은 **맞게 열려 있음** (우리 `PspCreateThread` 다리), 안 부르는 게 모자람 |
| 3 | 일꾼이 일감 받기 | `IoRemoveIoCompletion` (`+0x6ba`) 으로 **포트에서 꺼냄**. 출력 = **0x20 바이트 × `Count` 개** (`Count` 1 ~ 0x7ffffff), 꺼낸 수는 4바이트 칸에. **시간 제한 없음 (타임아웃 NULL, 깨울 수 있는 대기).** 마감 = 상태 `0x80`, 줄이기 = 상태 `0x102` | 앞 검사는 **맞음**, 빈 큐에서 멈추는 건 **틀림 (진짜는 잠들어 기다림)** |
| 4 | 설정 클래스 4 · `0xb` | **둘 다 입력 4바이트 고정.** 클래스 4 = 최소 스레드 수 (`+0x178`) — 늘리면 **그 자리에서 스레드 만듦**. `0xb` = 일꾼 기본 우선순위 (`+0x19c`) — 값 검사 없음, **앞으로 만들 일꾼에만** | 길이 표에 둘 다 빠짐 (`0` = 멈춤) |
| ★ | **일감이 들어왔는데 일꾼이 0 이면?** | **같은 `NtReleaseWorkerFactoryWorker` 호출 안에서 일꾼 1개를 만들어 깨운다** (조건 한 줄은 아래). 이후 일꾼이 일을 집을 때마다 "아직 일이 남았고 기다리는 일꾼이 없으면" 다음 일꾼을 하나씩 만든다. | 지금은 아무 일도 안 해서 모두 잠듦 → **틀림** |

---

## 공장 객체 칸 표 `[정적]` (이름은 `[추측: 공개 헤더]`)

공장 객체 몸 크기 **0x2a0** (`NtCreateWorkerFactory +0x1b4 MOV [RSP+0x28],0x2a0` → `ObCreateObjectEx` call `+0x1c5`; `ExpWorkerFactoryInitialization` 형식 정의에도 `0x2a0`). 뒤에 **작은 덩어리 `P`** (넌페이지드 0x28 바이트, 꼬리표 `TpWc`) 가 붙는다. 락·포트·패킷·기다리는 수는 **모두 `P` 안**에 있고 공장은 `+0x10` 으로 가리킨다.

### 공장 몸 (이하 `f`)

| 오프셋 | 크기 | 뜻 | 쓰는 곳 | 읽는 곳 |
|---|---|---|---|---|
| `+0x00` | 8 | 미룬 목록 (`ExpWorkerFactoryThreadCreationList`) 에 끼는 고리 `[추측]` | `CheckCreate +0x27f` (push, ret `+0x284`) | `DeferredThreadCreation +0x3b` (`MOV RDI,[RDI]`) |
| `+0x08` | 8 | 이번에 읽은 함수 안에 쓰고 읽는 곳 없음 `[공백]` | | |
| `+0x10` | 8 | **`P` 포인터** | `NtCreateWorkerFactory +0x1dc` | 거의 전부 (`NtRelease +0x6d`, `+0xd7` …) |
| `+0x18` | 8 | **시작 주소** (`StartRoutine`) | Create `+0x266` | `CreateThread +0xca` → 만들기 8번째 인자, `Query +0x1ab` |
| `+0x20` | 8 | **시작 인자** (`StartParameter`) | Create `+0x272` | `CreateThread +0xc1` → 9번째, `Query +0x1b7` |
| `+0x28` | 8 | **커널 핸들** (공장을 만든 프로세스, 접근 `0x2a`) | Create `+0x289` (`ObOpenObjectByPointer` ret `+0x16e`) | `CreateThread +0xa3` → 만들기 1번째 인자 |
| `+0x30` | 8 | 프로세스 객체 포인터 | Create `+0x28d` | `Query +0x1c3` (→ `+0x1d0` 프로세스 ID), `ExpDeleteWorkerFactory +0x4c` |
| `+0x38` | 8 | **스택 예약** (0 이면 `0x10000`) | Create `+0x214 ~ +0x228`; 설정 클래스 `0xa` `+0x807` | `CreateThread +0xdc` → 만들기 5번째, `Query +0x1d6` |
| `+0x40` | 8 | **스택 커밋** (0 이면 `0x1000`) | Create `+0x244 ~ +0x25a`; 클래스 `0xa` `+0x7fe` | `CreateThread +0xd3` → 6번째, `Query +0x1e2` |
| `+0x48 ~ +0x67` | 4×8 | **최근 일꾼 4명 기록판** (스레드 포인터) | `ExpInitializeThreadHistory +0xa/+0xe` 비움, `Wait +0x7a3` 근처 채움, `ExpShutdownWorkerFactory +0x4b` 비움 | `ExpCheckThreadHistory +0x54` |
| `+0x68` | 8 | 런다운 보호 (`ExAcquireRundownProtection`) | Create `+0x2ca` = 0 | `CreateThread +0x65` 잡고 `+0x168` 놓음 |
| `+0x70` | 8 | **놀기 제한 시간** (음수 100ns, 기본 `-67초`) | Create `+0x1ee`; 클래스 2 `+0x6b6` | `ManagerThread +0x107`, `Query +0x11f` |
| `+0x78 ~ +0x177` | 0x100 | CPU 집합 마스크 (클래스 `0xf`) | Create `+0x238` memset; 클래스 `0xf` (조각 `+0x33f0b7 ~ +0x33f0cc` memset·복사) | `CreateThread +0x1b8` (만든 스레드에 `ZwSetInformationThread` 클래스 `0x27`) |
| `+0x178` | 4 | **최소 스레드 수** (클래스 4) | Create `+0x1f2` = 0; 클래스 4 `+0x6f6`; 클래스 5 `+0x898`; `ExpShutdown +0xbc` = 0 | `Wait +0xac7`, `Manager +0xfc`, `Query +0x16f` |
| `+0x17c` | 4 | **최대 스레드 수** (클래스 5; 만들 때 8번째 인자) | Create `+0x200`; 클래스 5 `+0x4cd`; 클래스 4 `+0x86d` (최소가 더 크면 최대를 끌어올림); `ExpShutdown +0xb5` = 0 | `CheckCreate +0x94`, `WantsToCreate +0x12`, `Wait +0x2ff` 등 |
| `+0x180` | 4 | **움직이는 일꾼 수** (최대와 견주는 쪽) | `Ready +0x82` ++; `Wait +0xb0f` --; 클래스 9 ±1 (`+0x2a3`/`+0x36c`) | `CheckCreate +0x9a`, `WantsToCreate +0x1f`, `Set +0x724` |
| `+0x184` | 4 | **전체 일꾼 수** (`Query` 가 보고하는 쪽, `TotalWorkerCount`) | `Ready +0x8a` ++; `Wait +0xb15` -- | `Query +0x191` |
| `+0x188` | 4 | **만들어졌지만 아직 `Ready` 안 부른 수** (`PendingWorkerCount`) | `CreateThread +0x91` ++ (실패하면 `+0x19f` --); `Ready +0x90` -- | `Query +0x183`; `Set +0x724` (최소 채우기 합산) |
| `+0x18c` | 4 | **부드러운 최대** (클래스 `0xe`) | `Set +0x5c6` | `CheckCreate +0x15e` |
| `+0x190` | 4 | **"만드는 중" 수** (`CreateInProgress` > 0) | `CheckCreate +0x180` ++ (실패 `+0x1b6` --); `Set +0x4fe`·`+0x73c` ++; `Ready +0x72` -- | `CheckCreate +0x8b`, `WantsToCreate +0x3`, `Manager +0xec`, `Query +0x14b` |
| `+0x194` | 4 | **묶음 수** (`Binding`, 클래스 3) | `Set +0x570`·`+0x422`·`+0x862` | `ExpTryEnterAwayMode +0x33`, `Wait +0x979`, `Query +0x165` |
| `+0x198` | 4 | **상태 비트** (아래 표) | 여러 곳 | 여러 곳 |
| `+0x19c` | 4 | **일꾼 기본 우선순위** (클래스 `0xb`) | `Set +0x791` | `CreateThread +0x124 ~ +0x12f` (0 이 아니면 만든 스레드에 `ZwSetInformationThread` 클래스 3, 4바이트; call `+0x13d`, ret `+0x142`) |
| `+0x1a0` | 4 | **마지막 스레드 만들기 상태** (`LastThreadCreationStatus`) | `CreateThread +0xea` | `Query +0x1ee` |
| `+0x1a8` | ? | 놀기 타이머 (`KTIMER2`) | Create `+0x2bd` (`KeInitializeTimer2`), `+0x30f` (`KeSetTimer2`); 클래스 2 `+0x6d5` | `Manager +0x123`, `ExpShutdown +0x83` |
| `+0x230` | 0x30 `[추측]` | 놀기 타이머 알림 칸 (wait block) | Create `+0x2e4` (`KeRegisterObjectNotification` ret `+0x2e9`) | `Manager` (꺼낸 항목 − `0x230` = 공장, `+0xd9`) |
| `+0x260` | 4 | `1` = "이 알림은 타이머 쪽" 표식 | Create `+0x29f` | `Manager +0x7d` (`CMP [RDI+0x30],0`) |
| `+0x268` | 0x30 `[추측]` | **"멀리 있기" 포트 알림 칸** | `ExpTryEnterAwayMode +0x97`, `Wait +0x9d7` | `Manager` (꺼낸 항목 − `0x268` = 공장, `+0x88`) |
| `+0x298` | 4 | `0` = "이 알림은 포트 쪽" 표식 | Create `+0x298` | `Manager +0x7d` |

### 상태 비트 `f+0x198` `[정적]` (이름은 내 말)

| 비트 | 값 | 뜻 | 켜는 곳 | 끄는 곳 |
|---|---|---|---|---|
| 0~2 | `0x7` | 기록판 쓸 칸 번호 (**4 = 끔**) | `ExpShutdown +0x40` (4 로) | `ExpInitializeThreadHistory +0x0` |
| 3 | `0x8` | 미룬 목록에 올라 있음 | `CheckCreate +0x25a` | `DeferredThreadCreation +0x47` |
| 4~5 | `0x30` | **기록판 걸림** 단계 (`0x10` → `0x20`) | `CheckCreate +0x319`·`+0x365` | 만들기 성공 (`+0x189`, 마스크 `0xffffce0f`) |
| 6~7 | `0xc0` | **CPU 수 걸림** 단계 (`0x40` → `0x80`) | `CheckCreate +0x21e`·`+0x237` | 같음 |
| 8 | `0x100` | 만들기 실패 → 다시 시도할 것 | `CheckCreate +0x1ec` | 같음 |
| 9 | `0x200` | "멀리 있기" (일꾼 없을 때 관리 스레드가 포트를 지켜봄) | `ExpTryEnterAwayMode +0x52`, `Wait +0x98c` | `ExpLeaveAwayMode +0xcc` |
| 10 | `0x400` | 포트 알림이 걸려 있음 | `ExpTryEnterAwayMode +0x6c`, `Wait +0x9ae` | `ExpLeaveAwayMode +0xc4`, `Wait +0x2c1` |
| 11 | `0x800` | 일꾼을 **`LoaderWorker`** 로 요청 (클래스 `0xd` 값 비트 0) | `Set +0x2c6 ~ +0x2d8` | `CreateThread +0x36` |
| 12~13 | `0x3000` | **부드러운 최대 걸림** 단계 (`0x1000` → `0x2000`) | `CheckCreate +0x2c9`·`+0x38b` | 만들기 성공 |
| 14 | `0x4000` | CPU 집합이 설정됨 | 클래스 `0xf` (조각 `+0x33f0d1`) | |
| 15 | `0x8000` | 일꾼이 **한 번이라도** 만들어짐 | `CreateThread +0x111` | 클래스 `0xf` 가 이 비트가 켜졌으면 거절 `0xc000a003` (`+0x8c5`, `+0x8d5`) |

### `P` (공장 `+0x10` 이 가리키는 0x28 바이트)

| 오프셋 | 크기 | 뜻 | 쓰는 곳 | 읽는 곳 |
|---|---|---|---|---|
| `+0x00` | 8 | **락** (대기열 스핀락, IRQL 2) | Create `+0x8d` = 0 | 거의 전부 |
| `+0x08` | 8 | **완료 포트** (`_KQUEUE`) 포인터 (IoCompletion 핸들을 `+0xc3` 에서 `ObReferenceObjectByHandle`, 접근 `2`) | Create `+0xcf` | `Wait +0x6b6`, `CheckCreate +0x108`, `Release +0x136` |
| `+0x10` | 8 | **풀어주기 패킷** (`_IO_MINI_COMPLETION_PACKET_USER` 0x50B, **종류 4**, 콜백 = `ExpWorkerFactoryCompletionPacketRoutine`, 문맥 = `P`) | Create `+0x187` (`IoAllocateMiniCompletionPacket(…, P)`) | `Release +0x132`, `Routine +0xee`, `ExpShutdown +0x123` |
| `+0x18` | 4 | **넣은 일감 수** (`ReleaseCount` — Release 로 올리고 일꾼이 패킷을 꺼낼 때 내림) | `Release +0xfb` ++ ; `Routine +0x71` -- | `CheckCreate +0x112`, `WantsToCreate +0x43`, `Query +0x19b` |
| `+0x1c` | 4 | **기다리는 일꾼 수** (`WaitingWorkerCount` — `Wait` 안에서 막힌 스레드) | `Wait +0x2d8` ++ ; `Wait +0x730` -- | `CheckCreate +0xa6`, `WantsToCreate +0x2b`, `ExpTryEnterAwayMode +0x1f`, `Wait +0x7bb`, `Query +0x141` |
| `+0x20` | 1 | **패킷이 포트에 들어가 있음** (`InsertedIntoQueue`) | `Release +0x10c` = 1 ; `Routine +0x7c` = 0 ; `ExpShutdown +0xcf` = 1 | `Release +0x102`, `Query +0x157` |
| `+0x21` | 1 | **마감** (`Shutdown`) | `ExpShutdownWorkerFactory +0xad` = 1 | `Release +0xe0`, `Wait +0x284`·`+0x30a`, 클래스 4·5·2·`0xb` 몸 첫 줄, `Query +0x15e` |
| `+0x22` | 1 | **지우는 중** (객체가 없어짐 → `P` 를 마지막에 자유롭게 풀어 줌) | `ExpDeleteWorkerFactory +0x37` = 1 | `Routine +0x65` |

→ **에뮬레이터 `TODO(VM)` 하나 닫힘:** `[[공장+0x10]+0x21]` 은 **`Shutdown` 바이트**다. 새 공장은 0 (`P` 는 0 으로 채운 풀 `ExAllocatePool2` 플래그 `0x41`). 켜는 곳은 `ExpShutdownWorkerFactory +0xad` 하나 — `NtShutdownWorkerFactory +0x99` 와 `ExpCloseWorkerFactory +0xd` (마지막 핸들이 닫힐 때, `R9 == 1`) 가 부름.

### 포트 쪽 칸 (PDB `_KQUEUE` 로 확인)

| 포트 칸 | 뜻 | 이 길에서 |
|---|---|---|
| `+0x04` (`Header.SignalState`) | **큐에 들어 있는 패킷 수** | Release `+0x2e0` ++ ; `CheckCreate +0x10c`, `WantsToCreate +0x39`, `TryEnterAwayMode +0x60`, `Wait +0x99a` 가 "일이 있나" 로 읽음 |
| `+0x08` (`Header.WaitListHead`) | 기다리는 스레드 목록 | Release `+0x190` 이 비었는지 봄 |
| `+0x18` (`EntryListHead`) | 패킷 줄 | Release `+0x2e3 ~ +0x42f` 가 꼬리에 붙임 |
| `+0x28` (`CurrentCount`) | **지금 일하는 (큐에서 꺼내 가서 다시 안 기다리는) 스레드 수** | `CheckCreate +0x128` 이 CPU 수와 견줌 |
| `+0x2c` (`MaximumCount`) | 동시에 일해도 되는 수 | Release `+0x19c` |

---

## Q1. `NtReleaseWorkerFactoryWorker` (+ `NtWorkerFactoryWorkerReady`)

### 1-1. Release 가 하는 일 (차례) `[정적]` — `NtReleaseWorkerFactoryWorker.asm`

1. `+0x46` (ret `+0x4b`) `ObReferenceObjectByHandle(핸들, 접근 1, ExpWorkerFactoryObjectType)` — 접근 `1` = `EDX` (`+0x27 LEA EDX,[R13+1]`).
2. `+0x7c ~ +0xb4` IRQL 2 로 올리고 `P` 락 (`XCHG [RBX],RDX` `+0xae`).
3. `+0xe0 CMP byte [P+0x21]` ≠ 0 → 락 풀고 **상태 `0x80`** 돌려줌 (`+0xe4 JNZ +0x454` → `+0x459 ~ +0x463` → `+0x206`). **`CheckCreate` 안 부름.**
4. `+0xea ~ +0xf0` `P+0x18` 이 `-1` 이면 **`0xc0000001`** (`+0x475`) — 넘침 방지.
5. `+0xfb` **`P+0x18` 을 1 올림.** (`+0xf8 MOV EBP,R13D` → 기본 상태 0)
6. `+0x102 CMP byte [P+0x20]` ≠ 0 (이미 패킷이 포트에 있음) → 락 풀고 **상태 0 으로 끝** (`+0x106 JNZ +0x459`). **새 패킷 안 넣고 `CheckCreate` 도 안 부름.**
7. `+0x10c` **`P+0x20 = 1`.** `+0x110 ~ +0x124` 공장 `+0x198` 비트 9 가 켜져 있으면 `ExpLeaveWorkerFactoryAwayMode` (call `+0x11f`, ret `+0x124`). 락 풀기 `+0x129` (ret `+0x12e`).
8. 패킷 = `P+0x10` (`+0x132`). 없으면 `IopAllocateMiniCompletionPacket(1, 0)` (call `+0x2c4`, ret `+0x2c9`); 실패하면 패킷 없이 `+0x2d5 JMP +0x1f9` 로 **그냥 `CheckCreate` 로** (`+0x1f9 ~ +0x201`).
9. `+0x143 ~ +0x153` 패킷 `+0x18` `+0x20` `+0x28` `+0x30` 을 **0 으로** — 키 문맥·APC 문맥·상태·정보 (PDB `_IO_MINI_COMPLETION_PACKET_USER` 칸과 같음). **일꾼 눈에는 "전부 0 인 항목 하나".**
10. IRQL 2 · `KiAcquireKobjectLockSafe(포트)` (`+0x18b`, ret `+0x190`). 포트에 **기다리는 스레드가 있고** (`+0x190`) **`CurrentCount < MaximumCount`** (`+0x199 ~ +0x19f`) 이면 (현재 스레드의 `KTHREAD.Queue` (`+0xe8`) 가 이 포트이고 `WaitReason` (`+0x283`) 이 `0xf` = `WrQueue` 인 경우만 건너뜀, `+0x1a5 ~ +0x1af`, `+0x382 ~ +0x390`) → **`KiWakeQueueWaiter` 로 기다리는 일꾼에게 직접 넘김** (call `+0x1be`, ret `+0x1c3`). 아니면 **`SignalState` ++ (`+0x2e0`) 하고 줄 꼬리에 붙임** (`+0x2e3 ~ +0x42f`), 예전 값이 0 이고 기다리는 스레드가 있으면 `KiWakeOtherQueueWaiters` (call `+0x44a`). 이어서 `KiExitDispatcher` (call `+0x1e6`, ret `+0x1eb`).
11. **`+0x201` (ret `+0x206`) `ExpWorkerFactoryCheckCreate(공장, 0, 0)`** — 락 안 잡은 채로 (둘째 인자 0 = 스스로 락 잡음), 셋째 `0` = "미룬 일 다시 하기 아님".
12. 객체 참조 내리고 상태 (`EBP` = 0) 돌려줌 (`+0x206 ~ +0x26d`).

**바뀌는 칸 정리:** `P+0x18` ++ (넣은 일감 수), `P+0x20` = 1, 포트 `SignalState` ++ 또는 직접 넘김, 공장 `+0x198` 비트 9 끔. **`+0x180/+0x184/+0x188/+0x190` (일꾼 수 칸) 은 Release 가 직접 안 건드림** — `CheckCreate`·`CreateThread` 가 건드림.

### 1-2. `NtWorkerFactoryWorkerReady` — 새 일꾼이 "나 왔다" 하는 호출 `[정적]`

- 접근 `0x10` (`+0x16 MOV EDX,0x10`), call `+0x48` (ret `+0x4d`).
- `P` 락 (`KeAcquireInStackQueuedSpinLock` call `+0x61`, ret `+0x66`) → **`[f+0x190] == 0` 이면 `0xc0000001`** (`+0x66 ~ +0x6e JZ → +0xbb`) → 아니면 `[+0x190]--` (`+0x72`).
- **`[f+0x188] == 0` 이면 `0xc000010a`** (`+0x78 ~ +0x80 JZ → +0xc2`) — 위 `--` 는 되돌리지 않음.
- 아니면 `[+0x180]++` (`+0x82`), `[+0x184]++` (`+0x8a`), `[+0x188]--` (`+0x90`).
- → 일꾼 수 칸의 한살이: `CheckCreate` 가 `+0x190` ++ → `CreateThread` 가 `+0x188` ++ → **새 일꾼이 `Ready`** 를 부르면 `+0x190` · `+0x188` 내리고 `+0x180` · `+0x184` 올림. (줄이기 때 `Wait` 가 `+0x180`·`+0x184` 를 내림.)

### 1-3. 새 일꾼을 만드는 조건 `[정적]` — `ExpWorkerFactoryCheckCreate` (call `+0x201`/ret `+0x206` 에서 옴)

`CheckCreate(공장 f, 락 핸들 or 0, 다시하기 플래그 R8B)`. 락은 `P` 락 (둘째 인자가 0 이면 `+0x39 ~ +0x77` 에서 스스로 잡음). **다시하기 플래그가 0 일 때 (`Release`·`Wait`·`Set` 이 부르는 보통 모양):**

```
만든다  ⇔   f+0x190 == 0                         (+0x8b)  이미 만드는 중이 아님
        &&  f+0x180 <  f+0x17c                    (+0x94)  움직이는 일꾼 < 최대
        &&  P+0x1c  == 0                          (+0xa6)  기다리는 일꾼 없음
        &&  ( 포트.SignalState != 0  ||  P+0x18 != 0 )   (+0x10c, +0x112)  일이 있음
        &&  포트.CurrentCount <  KeNumberProcessors       (+0x128 ~ +0x133)  이미 CPU 수만큼 일하는 중이 아님   [걸림 ①]
        &&  !ExpCheckThreadHistory(f)             (+0x148 ~ +0x14f)                                              [걸림 ②]
        &&  ( f+0x18c == 0  ||  f+0x180 <= f+0x18c )      (+0x15e ~ +0x16c, +0x297)  부드러운 최대                [걸림 ③]
```
- 맞으면 `+0x16c` → 공장 비트 9 끄기 (`ExpLeaveAwayMode`, call `+0x17b`) → **`f+0x190` ++ (`+0x180`)** → 상태 비트 4~8·12~13 지우기 (`+0x189`, 마스크 `0xffffce0f`) → 락 풀기 (call `+0x193`) → **`ExpWorkerFactoryCreateThread(f)` (call `+0x19b`, ret `+0x1a0`)**.
- 앞 네 줄 중 하나라도 안 맞으면 `+0xac` 에서 **상태 비트만 지우고 그냥 돌아감** (일꾼을 안 만듦).
- **새 공장 첫 넣기** (일꾼 0, `P+0x18 = 1`, 포트에 패킷 1개, `CurrentCount = 0`, 기록판 비어 있음 → `ExpCheckThreadHistory` 는 **false**: 4칸이 다 비면 `+0xbd ~ +0xcd` 로 `AL = 0`, 부드러운 최대 0) → **전부 참 → 그 자리에서 만든다.** `[정적]`
- **걸림 ① ② ③ 에 걸리면 (미룸):** 상태 비트 단계를 올리고 `+0x24d` 미루기 공통 길로 간다.
  - 각 걸림은 **3 단계**다 (예: 걸림 ① `+0x20c ~ +0x247`): 처음 걸림 → 비트 `0x40` 켜고 타이머 종류 `3` (짧게) → 미룬 일을 다시 할 때 (플래그 1) 또 걸리면 `0x80` 으로 올리고 종류 `2` (중간) → 그 뒤 다시 할 때는 **그 검사를 건너뛰고 만든다** (`+0x2f8 ~ +0x305` 걸림 ① 은 `0x80` 이면 건너뜀; ② `+0x2df ~ +0x2e6`; ③ `+0x373 ~ +0x380`). 걸림 ②(기록판)·③(부드러운 최대)도 같은 모양 (`0x10 → 0x20`, `0x1000 → 0x2000`).
  - 미루기 공통 (`+0x24d ~ +0x28b`): 비트 3 안 켜져 있으면 켜고 → 락 풀기 (call `+0x263`, ret `+0x268`) → 공장 참조 올림 (`ObfReferenceObjectWithTag`, call `+0x270`) → **`ExpWorkerFactoryThreadCreationList` 에 `ExpInterlockedPushEntrySList`** (call `+0x27f`, ret `+0x284`) → **`ExpSetWorkerFactoryDeferredCreateTimer(종류)`** (call `+0x286`, ret `+0x28b`).
  - 타이머 종류 `[정적]` (`ExpSetWorkerFactoryDeferredCreateTimer`, `ExpWorkerFactoryInitialization +0x84 ~ +0x96`): `3` = 짧게 **30 ms** (`-0x493e0` ×100ns, 허용 지연 `0x1e` ms), `2` = 중간 **120 ms** (`-0x124f80`, `0x78`), `1` = 길게 **`ExpWorkerFactoryThreadCreationTimeoutInSeconds` 초** (파일 초기값 **10초**, 허용 `0x1f4` = 500 ms). 한 번에 타이머는 하나뿐이고 (`ExpWorkerFactoryThreadCreationState` `cmpxchg` `+0x3e`), **더 급한 (큰 번호) 종류가 이긴다** (`+0x30 CMP ECX,R8D; JG`).
- **만들기 실패 때** (`CreateThread` 가 음수 돌려줌, `+0x1a4 JNS` 아님): 락 다시 잡고 `f+0x190` -- (`+0x1b6`) → 상태가 `0xc000000a`·`0xc000010a`·`0xc00000f2`·`0xc0000001` 중 하나면 **조용히 포기** (`+0x1bc ~ +0x1e6`), 그 밖이면 **비트 8 (`0x100`) 켜고 종류 `1` (길게) 로 미룸** (`+0x1ec ~ +0x1fb`).

### 1-4. 바로 만드나, 미뤄 만드나 `[정적]`

- **바로:** 위 조건이 다 맞으면 `CheckCreate` 안에서 곧장 `CreateThread` (호출한 스레드 안, 같은 syscall 안). 새 스레드는 멈춘 채 만들어지고 `ZwResumeThread` (`CreateThread +0x153`, ret `+0x158`) 로 **같은 호출 안에서 풀림.**
- **미룸:** CPU 수 · 기록판 · 부드러운 최대 · 만들기 실패 중 하나에 걸릴 때만. 미룬 일은 **시스템 스레드 하나 `ExpWorkerFactoryManagerThread`** (부팅 때 `ExpWorkerFactoryInitialization +0x1b9` `PsCreateSystemThread`, 시작 `+0x194`) 가 처리: `KeRemoveQueueEx` (call `+0x51`, ret `+0x56`) 로 `ExpWorkerFactoryManagerQueue` 를 기다리다가, 타이머 알림 (`ExpWorkerFactoryThreadCreationBlock`) 이 오면 `ExpWorkerFactoryDeferredThreadCreation` (call `+0x60`, ret `+0x65`) → 목록을 `ExpInterlockedFlushSList` 로 비우고 (`+0x26`) 공장마다 `비트 3` 끄고 (`+0x47`) **`CheckCreate(공장, 락, 1)`** (call `+0x59`, ret `+0x5e`).
- **"멀리 있기" 길 (일꾼이 없는 동안 관리 스레드가 포트를 대신 지켜봄):** `ExpTryEnterWorkerFactoryAwayMode` (`Set` 클래스 3 의 `+0x582`, `Manager +0xaf`) 가 `f+0x194` (묶음 수) ≠ 0 이고 포트가 비었으면 비트 9·10 을 켜고 **포트에 `KeRegisterObjectNotification(포트, ManagerQueue, f+0x268)`** (`+0x97`) 을 걸어 둠 → 포트에 일이 들어오면 관리 스레드가 `CheckCreate` (`Manager +0xc3`). Release 는 먼저 이 알림을 끈다 (`+0x11f`).

### 1-5. 새 일꾼 말고 일꾼을 만드는 다른 계기 `[정적]` (`ExpWorkerFactoryCreateThread` / `CheckCreate` 부르는 곳 전부, `_calls.tsv`)

| 계기 | 부르는 줄 | 조건 |
|---|---|---|
| **Release** | `NtRelease +0x201` (→ `CheckCreate`) | 위 1-3 |
| **일꾼이 일을 집고 나서** (연쇄) | `NtWaitForWork +0xa5d` (→ `CheckCreate`, 락 든 채) | `Wait +0x7a9 ~ +0x7bf`: `f+0x180 < f+0x17c` && `P+0x1c == 0` 이고 `f+0x194 == 0` (`+0x979 ~ +0x980`) 이면 곧바로, 묶음이 있으면 포트에 일이 남은 경우만 (`+0x996 ~ +0x99e`) |
| 설정 클래스 **3** (묶음 0→≠0) | `Set +0x59a` (→ `CheckCreate`) | `ExpTryEnterWorkerFactoryAwayMode` 가 참 = 포트에 이미 일이 있을 때뿐 |
| 설정 클래스 **4** (최소 올림) | `Set +0x74f` (→ `CreateThread` 직접) | 아래 4-1 |
| 설정 클래스 **5** (최대 설정) / 클래스 9 | `Set +0x513` (→ `CreateThread` 직접) | 포트에 일이 이미 있을 때 등 (아래 4-3) |
| 관리 스레드 | `Manager +0xc3`, `DeferredThreadCreation +0x59` | 위 1-4 |

→ **`NtCreateWorkerFactory` 자체는 일꾼을 만들지 않는다** (`CreateThread`·`CheckCreate` 를 안 부름 — `_calls.tsv` 에 없음). 첫 일꾼은 **첫 Release** (또는 클래스 3·4·5) 에서 생긴다.

### 지금 우리 (PE_emulator)
- `nt_release_worker_factory_worker.cpp:15` (핸들 → 접근 `0x1` → `stop`): **앞 두 검사는 맞다** (`+0x46`, 접근 1). **모자란 것:** `P+0x21` → `0x80`, `P+0x18 == -1` → `0xc0000001`, `P+0x18` ++, `P+0x20` 가 0 이면 **0 으로 채운 패킷을 완료 포트에 넣음**, 그리고 **`CheckCreate`**. 지금 코드의 "일꾼을 깨우거나 만드는 계기일 수 있다 [추측]" 는 **맞다 `[정적]`**.
- `nt_worker_factory_worker_ready.cpp`: 접근 `0x10` · `[+0x190]==0 → 0xc0000001` 은 **맞다** (`+0x66 ~ +0x6e`). 일꾼을 만들게 되면 `+0x188 == 0 → 0xc000010a` 갈래와 세 칸 갱신이 필요.
- `nt_create_io_completion.cpp:145` 근처 (`NtCreateWorkerFactory`): `process != current → 0xc000000d` **맞음** (`+0x130 ~ +0x137` → `+0x3bd`), 스택 기본 `0x10000` / `0x1000` **맞음**, 최대 수 저장 **맞음** (`+0x200`). **모자란 것:** 놀기 제한 시간 (`+0x70` = 기본 `67초 × -1e7`), `P` 덩어리 (포트·패킷·마감 바이트), 풀어주기 패킷 (`IoAllocateMiniCompletionPacket` `+0x182`). 이 칸들이 있어야 Release 를 흉내낼 수 있다.

---

## Q2. 일꾼 스레드를 만드는 길

### 2-1. 사슬 `[정적]`

```
NtReleaseWorkerFactoryWorker +0x201 (ret +0x206)
 └ ExpWorkerFactoryCheckCreate +0x19b (ret +0x1a0)
    └ ExpWorkerFactoryCreateThread
       ├ +0x65 (ret +0x6a)  ExAcquireRundownProtection(f+0x68)          실패 → 상태 0x80 (+0x6c JZ +0x16d)
       ├ +0x7a (ret +0x7f)  P 락 ... +0x87 CMP [P+0x21],0 → 마감이면 만들지 않음 (+0x8b JNZ +0x1a9)
       ├ +0x91              f+0x188 ++ (만들어졌지만 Ready 전 수)
       ├ +0xe5 (ret +0xea)  RtlpCreateUserThreadEx
       │   └ +0x14c (ret +0x151)  ZwCreateThreadEx
       │        └ NtCreateThreadEx → +0x2cd (ret +0x2d2) PspCreateThread → +0x27e PspAllocateThread → +0x2e5 PspInsertThread  (run74)
       ├ +0xea              f+0x1a0 ← 상태
       ├ +0x111             처음 성공이면 f+0x198 |= 0x8000
       ├ +0x13d (ret +0x142) ZwSetInformationThread(스레드, 3 = 기본 우선순위, &f+0x19c, 4)   ← f+0x19c ≠ 0 일 때만
       ├ +0x1c8 (ret +0x1cd) ZwSetInformationThread(스레드, 0x27 = CPU 집합, &f+0x78, KiActiveGroups×8)   ← 비트 14 일 때만
       ├ +0x153 (ret +0x158) ZwResumeThread          ← 멈춰 만든 스레드를 푼다
       └ +0x15f (ret +0x164) ObCloseHandle, +0x168 (ret +0x16d) ExReleaseRundownProtection
```
- `ExpWorkerFactoryCreateThread` 는 앞뒤로 `KeEnterCriticalRegion` (`+0x5a DEC [KTHREAD+0x1e4]`) · `KeLeaveCriticalRegion` (call `+0x16d`, ret `+0x172`) 로 감쌈. 실패하면 (`+0xf4 JS +0x192`) `f+0x188` -- (`+0x19f`) 하고 상태를 그대로 돌려줌.
- **`NtCreateThreadEx` 와 같은 길인가: 같다.** `ZwCreateThreadEx` 를 거쳐 **`NtCreateThreadEx` 로 들어가고 그 아래는 사용자 `CreateThread` 와 똑같이** `PspCreateThread` → `PspAllocateThread` → `PspInsertThread` (run74·sast/69). 갈리는 점은 **호출자가 커널 모드라 `PreviousMode = Kernel`** 인 것뿐 `[추측: Zw 스텁 30B 는 안 읽음]`: `NtCreateThreadEx +0x86 CMP [KTHREAD+0x232],DL → +0x8c JZ +0xac` (핸들 쓸 곳 검사 건너뜀), `+0xcb ~ +0xde` (속성 목록을 `PspBuildCreateProcessContext` 에 `PreviousMode` 와 함께 넘김), `+0xf4 ~ +0x12c` (프로세스 핸들 `ObpReferenceObjectByHandleWithTag` 에 `PreviousMode` 넘김 → 커널 핸들 허용·접근 검사 없음).

### 2-2. 시작 주소 · 인자 · 스택 크기가 어디서 오나 `[정적]`

| 새 스레드 값 | 출처 (칸) | 만들기 호출 칸 | 근거 |
|---|---|---|---|
| **프로세스** | `NtCreateWorkerFactory` 5번째 인자 (프로세스 핸들) → 커널 핸들 `f+0x28` | `RtlpCreateUserThreadEx` 1번째 (`RCX`) → `ZwCreateThreadEx` 4번째 | Create `+0x169` (call, ret `+0x16e`), `CreateThread +0xa3` |
| **프로세스 제한** | **공장 만든 프로세스 = 부르는 스레드의 프로세스여야 함** | | Create `+0x122 ~ +0x137`: `[KTHREAD+0xb8]` (`ApcState.Process`) ≠ 프로세스 객체면 **`0xc000000d`** (`+0x3bd`) |
| **시작 주소** (`Rip` = `RtlUserThreadStart`, `Rcx` = 이 값) | 6번째 인자 → `f+0x18` | `RtlpCreateUserThreadEx` 8번째 → `ZwCreateThreadEx` 5번째 | Create `+0x266`; `CreateThread +0xca`; `Rtlp +0x147` |
| **시작 인자** (`Rdx`) | 7번째 인자 → `f+0x20` | 9번째 → 6번째 | Create `+0x272`; `CreateThread +0xc1`; `Rtlp +0x142` |
| **스택 예약** | 9번째 인자 → `f+0x38` (0 이면 `0x10000` = 64KB) | `Rtlp` 5번째 → `ZwCreateThreadEx` **10번째 (`MaximumStackSize`)** | Create `+0x214 ~ +0x228`; `CreateThread +0xdc`; `Rtlp +0x122 ~ +0x126` |
| **스택 커밋** | 10번째 인자 → `f+0x40` (0 이면 `0x1000`) | `Rtlp` 6번째 → `ZwCreateThreadEx` **9번째 (`StackSize`)** | Create `+0x244 ~ +0x25a`; `CreateThread +0xd3`; `Rtlp +0x12b ~ +0x12f` (`[RSP+0x40]`) |
| **`ZeroBits`** | 0 | | `CreateThread +0xb0 XOR R9D,R9D` |
| **만들기 플래그** | `1 또는 ((f+0x198 & 0x800) >> 7)` = **`0x1`** (멈춘 채로) 또는 **`0x11`** (+ `LoaderWorker`) | `Rtlp` 3번째 `R8D`, **`& 0x77` 만 통과** (`+0x73 TEST R8D,0xffffff88`), 비트 그대로 `ZwCreateThreadEx` `CreateFlags` 로 (`+0x8b ~ +0x110`) | `CreateThread +0x1f ~ +0x62` (`ESI`), `Rtlp +0x14c` |
| **속성** | `PS_ATTRIBUTE` 하나 = `PsAttributeClientId` (`0x10003`, 크기 0x10), `Rtlp +0x9e`·`+0xaa` | 만든 스레드 `CLIENT_ID` 돌려받음 | `Rtlp +0xb2 MOV [RBP-0x50],0x28` (목록 길이) |
| **접근** | `0x1fffff` (`THREAD_ALL_ACCESS`) | | `Rtlp +0xfc` |
| **개체 속성** | 길이 `0x30`, `Attributes = 0x200` (`OBJ_KERNEL_HANDLE`), 나머지 0 | | `Rtlp +0x83`, `+0x8e` |

- **ntdll 쪽에서 이 칸들을 채우는 값** (이미 있던 `TpAllocPoolInternal.asm`, 읽기만) `[정적]`: 프로세스 핸들 `-1` (현재 프로세스, `+0x2ee OR [RSP+0x20],-1`), **시작 주소 = `TppWorkerThread`** (`+0x2e2 LEA RAX,[0x18001a2e0]` → `_index.tsv` rva `0x1a2e0`), **시작 인자 = 풀 포인터** (`+0x2dd [RSP+0x30]`), **최대 스레드 수** = (인자, 0 이면 KUSD `+0x3c0` × 4) 이고 **512 미만이면 512** (`+0x2a5 ~ +0x2c0`; KUSD `+0x3c0` 이름은 `[추측: ActiveProcessorCount]`), **스택 예약·커밋 = 실행 파일 PE 헤더 `SizeOfStackReserve` / `SizeOfStackCommit`** (`+0x76 MOV R13,[RAX+0x60]`, `+0x7a MOV R14,[RAX+0x68]` → `+0x2d4`·`+0x2cf`). 풀 호출은 `+0x303` (ret `+0x308`).
- **이 스레드가 CPU 에서 처음 하는 일은 사용자 `CreateThread` 와 같다:** `Rip = ntdll!RtlUserThreadStart` (`PsNtdllExports+0x10`), `Rcx` = 시작 주소, `Rdx` = 인자 (run74 `NtCreateThreadEx +0x1bf ~ +0x236`). 즉 새 일꾼은 **`RtlUserThreadStart(TppWorkerThread, 풀)`** 로 시작한다.
- 이 일로 **flow.md 의 `[공백]` 하나가 닫힌다:** "일꾼이 실제로 어떤 `CreateFlags` 로 이 길로 만들어지는지는 그대로 모름 — 일꾼 공장 쪽 길은 안 봤다" (run74·run54 줄) → **`CreateFlags = 0x1` 또는 `0x11`.** `0x10` → 만들기 플래그 `0x80` → `LoaderWorker` (`0x2000`) 로 이어짐 (run74 표). `0x2`·`0x20` 은 이 길에서 **안 켜짐**.

### 지금 우리 (PE_emulator)
- `Src/nt/TeamSyscall.cpp:1062` 가 `PspCreateThread(프로세스, 스레드, start_routine, argument, stack_size, maximum_stack_size, 0)` 로 팀원 `NtCreateThreadEx` 를 받는 길을 이미 갖고 있다. **맞는 모양이다.** 공장 쪽에서 쓰려면 **`start_routine = f+0x18`, `argument = f+0x20`, `stack_size = f+0x40` (커밋), `maximum_stack_size = f+0x38` (예약), `CreateFlags = 0x1 (| 0x10)`** 으로 부르고 **같은 호출 안에서 풀면 된다.** `TODO(VM): CreateFlags → PspCreateThread 플래그 바꾸기` 줄 (`:1058`) 은 위 표 (`0x10 → 0x80`) 가 답.
- 만든 스레드의 `기본 우선순위 (+0x19c)` · `CPU 집합` 은 `f+0x19c` 가 0 이고 비트 14 가 꺼져 있으면 건너뜀 → **test.exe 기본 풀 (설정 안 함) 에선 안 일어남** `[추측: ntdll 가 클래스 0xb·0xf 를 안 부르면]` (동적 확인 후보).

---

## Q3. `NtWaitForWorkViaWorkerFactory`

`NtWaitForWorkViaWorkerFactory(핸들, 패킷 배열, Count, 꺼낸 수 칸, DeferredWork)` — 다섯 번째 인자는 `[RSP+0x220]` (`+0x65 MOV RBX,[RSP+0x220]`).

### 3-1. 일감 받기 `[정적]`

| 단계 | 줄 | 내용 |
|---|---|---|
| 검사 | `+0x10d ~ +0x116` | `Count - 1 > 0x7ffffff` → **`0xc000000d`** (`+0xba4`). 즉 `Count` 는 **1 ~ 0x7ffffff**. |
| 사용자 모드일 때 | `+0x125 ~ +0x188` | `ProbeForWrite(배열, Count×0x20, 정렬 8)` (call `+0x133`, ret `+0x138`), 꺼낸 수 칸 4바이트 쓰기 검사 (`+0x145 ~ +0x158`), **`DeferredWork` 가 8 정렬이 아니면 `ExRaiseDatatypeMisalignment`** (`+0x15a TEST BL,7` · `+0x15d JNZ +0x173`), 0x18 바이트를 복사 (`+0x178 ~ +0x18c`). |
| 핸들 | `+0x20d` (ret `+0x212`) | `ObReferenceObjectByHandle(핸들, 접근 2, ExpWorkerFactoryObjectType)` |
| 임시 배열 | `+0x227 ~ +0x22b`, `+0xbd6 ~ +0xc16` | **`Count > 0x10` 이면** `ExAllocatePool2(0x40, Count×8, 꼬리표 'None')` (call `+0xbe8`, ret `+0xbed`), 실패하면 `Count = 0x10` 으로 줄여 **스택 배열 (`[RSP+0x130]`, 0x80B) 로 대신**. `0x10` 이하는 처음부터 스택 배열. |
| 락 · 마감 | `+0x231 ~ +0x284` | `P` 락. **`P+0x21` ≠ 0 이면 `0x80`** 으로 끝 (`+0x284 JNZ +0xbbc` → `+0xbc6 MOV R14D,0x80`). |
| 멀리 있기 끄기 | `+0x28e ~ +0x2cb` | 공장 비트 9·10 이 켜져 있으면 포트 알림을 떼고 (`KiDeregisterObjectWaitBlock` call `+0x2ab`) 참조 내림 (call `+0x2bc`) |
| **기다림 표시** | `+0x2d5 ~ +0x2d8` | **`P+0x1c` ++** |
| 루프 머리 | `+0x2fd ~ +0x30e` | `f+0x17c < f+0x180` (최대 < 움직이는 수) 이거나 `P+0x21` → **바로 줄이기 (상태 `0x102`)**, `+0x932`. |
| **꺼내기** | `+0x684 ~ +0x6ba` | **`IoRemoveIoCompletion(포트 = [P+8], 출력 배열, 임시 배열, Count, &꺼낸 수, PreviousMode, 시간 제한 = NULL, 깨울 수 있음 = 1)`** (call `+0x6ba`, ret `+0x6bf`). 시간 제한 칸이 `R13` (= 0) → **기다림에 시간 제한 없음.** |
| 뒤처리 | `+0x6ca` (ret `+0x6cf`) | `ExpWorkerFactoryFinishDeferredWork` (`DeferredWork` 쪽 칸 정리) |
| 성공 | `+0x8bf ~ +0x8cb` | `*꺼낸 수 = 지역 칸` (사용자 모드면 `+0x8bf`, 커널 모드면 `+0x8fa`). **상태가 0 이 아니면 꺼낸 수 칸을 안 씀** (`+0x8b3 TEST R14D,R14D` · `+0x8b6 JNZ`). |

- **출력 한 칸 = 0x20 바이트** (`FILE_IO_COMPLETION_INFORMATION` 모양): `IoRemoveIoCompletion` 이 꺼낸 항목마다 **`{ KeyContext(8), ApcContext(8), IoStatus 상태(4+자리), IoStatusInformation(8) }`** 를 `+0x204 · +0x208 · +0x20d` 에서 씀 (`IoRemoveIoCompletion.asm`). **한 번에 `Count` 개까지.**
- **꺼낸 것을 돌려주는 함수** = `IoRemoveIoCompletion` (call `+0x6ba`). 안쪽은 `KeRemoveQueueEx` (call `IoRemoveIoCompletion +0x8d`). 이번엔 이름만.
- **풀어주기 패킷이 일꾼에게 어떻게 보이나:** 항목 종류가 `[패킷+0x10]` 이 `0` 도 `2` 도 아니라서 (`IoRemoveIoCompletion +0x10f ~ +0x1c9`, 종류 4) **`+0x1d1 ~ +0x1ec`** 에서 `{ +0x18, +0x20, +0x28, +0x30 }` 을 복사한 뒤 `IopFreeMiniCompletionPacket(패킷)` (call `+0x1ec`, ret `+0x1f1`) 을 부른다. 이 함수가 **종류 4 에 콜백이 있으면 그 콜백을 부름** (`IopFreeMiniCompletionPacket +0x9d ~ +0xac`: 콜백 == `ExpWorkerFactoryCompletionPacketRoutine` 이면 call `+0xac`, ret `+0xb1`). → **일꾼은 "키·APC 문맥·상태·정보가 전부 0 인 항목 1개" 를 받는다** (Release 가 0 으로 채웠으므로, Q1-1 9번).
- **콜백 `ExpWorkerFactoryCompletionPacketRoutine(패킷, P)`** `[정적]` (일꾼이 패킷을 꺼낼 때 그 스레드 안에서 돎):
  - `P` 락 (`+0x56 ~ +0x61`). `P+0x22` (지우는 중) 이면 `+0xc8` → `+0x77` 로 가서 **`P+0x20 = 0` 하고 풀어주기 패킷과 `P` 를 자유롭게 풂** (`+0x121 ~ +0x12f`: `IoFreeMiniCompletionPacket`, `ExFreePoolWithTag`).
  - 마감 (`P+0x21`) 이면: 기다리는 일꾼이 남아 있으면 (`P+0x1c ≠ 0`, `+0xd8 ~ +0xdb`) **같은 패킷을 다시 포트에 넣어서** (`IoSetIoCompletionEx3`, call `+0x117`) 다음 일꾼도 깨움 — **연쇄로 전부 깨운다**; 없으면 `P+0x20 = 0`.
  - 보통 때: **`P+0x18` -- (`+0x71`)** → **0 이 아니면 같은 패킷을 다시 포트에 넣음** (`+0x75 JNZ +0xdd` → `+0xe7 JMP +0xee`), 0 이 되면 `P+0x20 = 0` (`+0x7c`) 로 끝. → **Release 를 N 번 불렀으면 일꾼이 패킷을 N 번 꺼내게 된다** (패킷은 하나를 돌려씀).

### 3-2. 일감이 없을 때 기다림 `[정적]`
- 시간 제한 칸이 NULL 이라 **일꾼은 포트에 일이 들어올 때까지 안 깨어나는 기다림** (깨울 수 있는 대기 = `+0x684 MOV byte [RSP+0x38],1`). 일꾼 스레드가 `P+0x1c` ++ 한 채로 잠든다. **놀기 제한은 이 호출의 인자가 아니다** — 아래 3-3 의 관리 스레드가 따로 깨운다.

### 3-3. 마감 · 줄이기 때 일꾼이 스스로 끝나는 길 `[정적]`

일꾼이 끝내야 할 때 이 호출은 **상태로 알린다** (끝내는 건 호출한 ntdll 쪽 일, 아래 `[추측]`).

| 상태 | 언제 | 줄 |
|---|---|---|
| **`0x80`** | **들어올 때** `P+0x21` (마감) 이 이미 켜져 있음 | `+0x284 JNZ +0xbbc` → `+0xbc6 MOV R14D,0x80` |
| **`0x102` (`STATUS_TIMEOUT`)** ① | **루프 머리**: 최대 `<` 움직이는 수 (클래스 5 로 최대를 낮췄을 때) 또는 `P+0x21` 마감 | `+0x2ff ~ +0x30e` → `+0x932 MOV R14D,0x102` → `+0x938 JMP +0x72c` |
| **`0x102`** ② | **놀기 제한 지남**: 관리 스레드가 `KeTimeOutQueueWaiters` 로 일꾼을 시간 초과로 깨움 → `IoRemoveIoCompletion` 이 `0x102` 돌려줌 → **세 가지를 다 만족할 때만 "나감"**: `WantsToCreate(f, 1)` **거짓**, `f+0x180 > f+0x178` (움직이는 수 > 최소), **현재 스레드의 `IrpList` (`ETHREAD+0x540`) 가 비어 있음** (입출력 요청이 걸려 있으면 안 나감). 하나라도 안 맞으면 `+0x2fd` 로 돌아가 **다시 기다림.** | `+0x726 JZ +0xaa5` → `+0xaad` (call, ret `+0xab2`) · `+0xac7 ~ +0xadf` · `+0xae5 ~ +0xaf6 JZ +0x72c` (PDB `_ETHREAD.IrpList`) |
| 줄이기 뒤처리 (위 ①②) | `+0x72c` 로 모여 **`P+0x1c` --** → **`f+0x180` --, `f+0x184` --** → 기록판에서 현재 스레드 뺌 | `+0x730`, `+0x733 JZ +0xb0f`, `+0xb0f`·`+0xb15`, `ExpRemoveCurrentThreadFromThreadHistory` call `+0xb1b` (ret `+0xb23`) |
| 그 밖 | `IoRemoveIoCompletion` 이 돌려주는 값 그대로 (알림 대기 깨어남 등) | `+0x6bf MOV R14D,EAX` |

- `ExpWorkerFactoryWantsToCreate(f, 1)` (`WantsToCreate`, 이 호출에서만 불림) 한 줄: `f+0x190 == 0 && f+0x180 < f+0x17c + 1 && P+0x1c <= 1 && (포트.SignalState != 0 || P+0x18 != 0)` (`+0x3 ~ +0x49`) — "이 놀던 일꾼이 안 나가도 되는 일이 아직 있나".
- **줄이는 쪽 (누가 시간 초과를 일으키나)**: `ExpWorkerFactoryManagerThread` 가 공장 타이머 `f+0x1a8` 알림을 받으면 (`+0x7d ~ +0xd9`, 알림 칸 `f+0x230`) **`f+0x190 == 0` 이고 `f+0x180 > f+0x178` 일 때** `KeTimeOutQueueWaiters(포트, 시간 = -f+0x70 의 부호 뒤집음, 개수 = f+0x180 - f+0x178)` (call `+0x115`, ret `+0x11a`) 를 부르고, 마감이 아니면 타이머를 다시 건다 (call `+0x134`). **놀기 제한 값 = `ExpWorkerFactoryThreadIdleTimeoutInSeconds`** (파일 초기값 **67초** = `0x43`, `ExpWorkerFactoryInitialization` 이 `1 ~ 600` 으로 자름 `+0x4a ~ +0x6e`). `refs` 상 쓰는 곳은 이 함수뿐 + 레지스트리 표 한 줄 (`Session Manager\Executive` 의 `WorkerFactoryThreadIdleTimeout` · `WorkerFactoryThreadCreationTimeout` — 표 rva `0xc81f50 ~ 0xc81f80`, 표를 읽는 함수는 안 찾음 `[공백]`). 설정 클래스 `0xc` 가 같은 `KeTimeOutQueueWaiters` 를 **시간 0 으로 즉시** 부름 (`Set +0x63b`, 개수 = `f+0x180 - f+0x178`, `f+0x190 == 0` 일 때만; 줄인 수를 사용자 칸에 돌려줌 `+0x64f`).
- **마감 쪽:** `NtShutdownWorkerFactory +0x99` · `ExpCloseWorkerFactory +0xd` (마지막 핸들) → `ExpShutdownWorkerFactory`: 기록판 비움 (`+0x48 ~ +0x66`), 비트 9 끔, `P+0x21 = 1` (`+0xad`), **`f+0x17c = 0`, `f+0x178 = 0`** (`+0xb5`·`+0xbc`), 기다리는 일꾼이 있고 패킷이 안 들어 있으면 `P+0x20 = 1` 하고 **풀어주기 패킷을 포트에 넣음** (`IoSetIoCompletionEx3`, call `+0x13b`) → 일꾼이 하나 깨어나 → 콜백이 다음을 깨움 (위 연쇄). 깨어난 일꾼은 **0 패킷을 받고 성공으로 돌아가고**, 그 일꾼의 **다음 `Wait` 호출이 `+0x284` 에서 `0x80` 으로 끝난다** `[정적]`. 이후 `ExpDeleteWorkerFactory` 가 `P+0x22` 를 켜고 (`+0x37`) 프로세스 객체·핸들·포트 참조를 내림 (`+0x4c ~ +0x6d`), 패킷이 포트에 남아 있지 않으면 (`+0x43 ~ +0x75`) 풀어주기 패킷과 `P` 를 풂.
- **일꾼이 `0x80` / `0x102` 를 받고 스스로 `ExitThread` 하는지** 는 ntdll `TppWorkerThread` 의 일 (이번에 안 읽음) `[추측]`.

### 3-4. 일을 집은 일꾼의 뒤처리 (다음 일꾼 연쇄) `[정적]`
- `+0x72c ~ +0x730` `P+0x1c` -- → `+0x740 ~ +0x7a3` 기록판에 현재 스레드를 넣음 → **`+0x7a9 ~ +0x7bf`: `f+0x180 < f+0x17c` 이고 `P+0x1c == 0` 이면** (일꾼이 하나도 안 기다리고 여유가 있으면) `+0x979` 이하 (묶음이 없으면 곧바로 `CheckCreate` `+0xa52 ~ +0xa5d`, 있으면 포트에 일이 남은 경우만 `CheckCreate`, 없으면 "멀리 있기" 알림을 건다 `+0x9ae ~ +0x9d7`). → **일이 쌓여 있으면 일꾼이 하나씩 늘어난다.**

### 지금 우리 (PE_emulator)
- `nt_wait_for_work_via_worker_factory.cpp`: `Count == 0 || Count >= 0x08000000 → invalid` **맞음** (`+0x10d ~ +0x116`), 배열 `Count×0x20` · 8 정렬 · 꺼낸 수 칸 4바이트 검사 **맞음**, 마감 → `0x80` **맞음** (`+0x284`), 핸들 접근 `2` **맞음** (`+0x201 ... EDX = 2`). **고칠 곳:** (1) `DeferredWork` 8 정렬 아님 → **`ExRaiseDatatypeMisalignment` (`+0x15d JNZ +0x173`, 상태 `0x80000002` 로 돌려줌 `[추측: __try 로 상태 바뀜, $filt$0 = ExSystemExceptionFilter]`)** — 지금은 `stop("unverified_alignment_behavior")`. (2) **큐가 비었을 때 `stop` 하는 건 진짜와 다르다 — 진짜는 시간 제한 없이 잠들어 기다린다 (`P+0x1c` ++ 한 채로).** 모델은 "이 스레드를 포트에서 기다리게 재운다" 로 가야 한다. (3) **풀어주기 패킷 (0 이 4개) 이 같은 큐에 들어와야** 일꾼이 깨어난다 — 지금은 큐에 안 넣는다. (4) **상태 `0x102`** (줄이기) 길이 없음 — 놀기 제한이 안 걸림.

---

## Q4. `NtSetInformationWorkerFactory` 클래스 4 · `0xb`

`NtSetInformationWorkerFactory.asm` 점프 표 세 개를 `ntoskrnl.exe` 바이트에서 직접 풀었다 (표 이름은 내 말):

| 표 | 이미지 rva | 길이 | 인덱스 | 쓰는 줄 |
|---|---|---|---|---|
| **A** (1차: 클래스 → 길이 검사) | `0x3a77ac` | 16칸 | 클래스 `0 ~ 0xf` | `+0x3e5 MOV EAX,[RDX+RBX*4+0x3a77ac]`, `+0x3ef JMP RAX` |
| **B** (읽어 들이기) | `0x3a773c` | 14칸 | 클래스 − 2 | `+0xd4 MOV ECX,[RDX+RAX*4+0x3a773c]`, `+0xde JMP RCX` |
| **C** (락 안 몸) | `0x3a7774` | 14칸 | 클래스 − 2 | `+0x3a5 MOV ECX,[RDX+RAX*4+0x3a7774]`, `+0x3af JMP RCX` |

- **클래스 4:** A[4] = `+0x77` (4바이트 길이 검사) → B[2] = `+0xeb` (4바이트 읽기) → C[2] = **`+0x547`** (몸).
- **클래스 `0xb`:** A 를 **거치지 않는다.** 맨 앞에서 `+0x67 CMP EBX,0xb` · `+0x6a JNZ +0x3d5` 로 클래스 `0xb` 만 `+0x70` 으로 직행 (A[0xb] 칸은 `+0x82c` = 잘못된 클래스라서 A 로 가면 `0xc0000003` 이 됨). → 4바이트 길이 검사 (`+0x77 ~ +0x85`) → B[9] = `+0xeb` → C[9] = **`+0x780`** (몸).
- 둘 다 **길이 4 아니면 `0xc0000004`** (`+0x82 CMP R14D,R8D` · `+0x85 JNZ +0x3bc`), 값 읽기 `+0xeb ~ +0x104` (사용자 모드면 `0x7fffffff0000` 으로 눌러 읽기 검사, 정렬 검사 **없음**), 핸들 `+0x20e` (ret `+0x213`, 접근 `4` = `+0x207 LEA EDX,[R15+4]`), 락 `P` (`+0x23e ~ +0x275`).

### 4-1. 클래스 4 몸 (`+0x547`) `[정적]` — **이름: 최소 스레드 수 `[추측: 공개 헤더 WorkerFactoryThreadMinimum]`**

1. `+0x54c ~ +0x554` `P+0x21` ≠ 0 → **상태 `0x80`** (`+0x55a`), 아무것도 안 바꿈.
2. `+0x6df ~ +0x6f6` **새 값 > 옛 `f+0x178` 이면 "올렸다" 표시** (`CMOVA R12D`) 하고 **`f+0x178 = 새 값`** (**값 검사 없음**).
3. `+0x6fc ~ +0x702` `f+0x17c` (최대) 가 새 최소보다 작으면 **최대를 최소로 끌어올림** (`+0x86d`).
4. 올리지 않았으면 (`+0x708 JZ +0x7c7`) 끝 — **같거나 줄인 값은 아무 일도 없음.**
5. 올렸으면 `+0x714` 비트 9 끄기 (`ExpLeaveAwayMode`, call `+0x87b`) → **루프 `+0x724 ~ +0x768`: `f+0x188 + f+0x180 < f+0x178` 인 동안 `f+0x190` ++ → 락 풀고 → `ExpWorkerFactoryCreateThread` (call `+0x74f`, ret `+0x754`) → 락 다시 잡기** (call `+0x760`, ret `+0x765`). 실패 (음수) 면 `f+0x190` -- (`+0x76a`) 하고 그 상태를 돌려줌 (`+0x770 JMP +0x2de`).
   - → **최소를 N 으로 올리면 그 호출 안에서 일꾼 N 개 (이미 있는 수 뺀 만큼) 가 한꺼번에 만들어진다.** 일감·CPU 수·기록판 검사 **없음** (`CheckCreate` 안 거침). 합산에 쓰는 칸은 `+0x188` (아직 Ready 전) + `+0x180` (움직이는 수).
   - 값이 매우 크면 실패할 때까지 계속 만든다 (상한 검사 없음) `[정적]`.

### 4-2. 클래스 `0xb` 몸 (`+0x780`) `[정적]` — **이름: 일꾼 기본 우선순위 `[추측: WorkerFactoryThreadBasePriority]`**

- `+0x785 ~ +0x791` **`f+0x19c = 입력 4바이트`** (값 검사 **없음**).
- `+0x797 ~ +0x79c` 상태 = `P+0x21` 이 켜져 있으면 **`0x80`**, 아니면 `0` (`NEG CL` · `SBB` · `AND 0x80`). **`P+0x21` 이 켜져 있어도 칸은 이미 썼음.**
- 스레드를 만들거나 깨우는 일 **없음.** 쓰는 곳은 `CreateThread +0x124 ~ +0x13d` 하나: `f+0x19c ≠ 0` 이면 **그 뒤에 만드는 일꾼에만** `ZwSetInformationThread(스레드, 3, &f+0x19c, 4)` (call `+0x13d`, ret `+0x142`). 이미 있는 일꾼은 안 바꿈. 읽는 `Query` 칸 없음 (0x78 구조에 없음).

### 4-3. 같은 표에서 얻은 덤 (확인용, 앞 run 과 대조) `[정적]`

| 클래스 | 이름 `[추측]` | 몸 | 한 줄 |
|---|---|---|---|
| 2 | 놀기 제한 | C[0] `+0x676` | 맞음 (run69) — `f+0x70`, `KeSetTimer2` (call `+0x6d5`) |
| 3 | 묶음 수 | C[1] `+0x3ff` | `f+0x194` ±입력, 0 → ≠0 이면 `TryEnterAwayMode` (`+0x582`) → `CheckCreate` (`+0x59a`) |
| 5 | 최대 | C[3] `+0x494` | `f+0x17c`; **옛 최대 0 이고 `P+0x18 ≠ 0` 이거나 포트에 일이 있으면 (`+0x4bf ~ +0x4c7`) 끝에 `CreateThread` 1개 직접 (`+0x513`)**, 상한은 새 최대 (`+0x4e6 ~ +0x4e8`), 실패는 삼킴 (`+0x53c ~ +0x542`) |
| 8 | 목표 조정 `[추측: AdjustThreadGoal]` | `+0x21b CMP EBX,8 → +0x836` | **락 없이** 포트 `MaximumCount` (`_KQUEUE +0x2c`) ← 값 (**0 이면 `KeNumberProcessors`**) 하고 상태 0 (`+0x836 ~ +0x85a`, 조각 `FUN_1406e5e8d +0x0` = `+0x33f07d`). `TppAdjustRunningThreadGoalWithLock` 이 부름 |
| 9 | 콜백 종류 `[추측]` | `+0x27b ~ +0x2de` | `f+0x180` ±1; 이번에 안 읽음 |
| `0xa` | 스택 정보 | C[8] `+0x7cf` | `f+0x38`·`f+0x40` (16바이트) 쓰기, 마감이면 `0x80` |
| `0xc` | 놀기 일꾼 즉시 줄이기 | C[10] `+0x60b` | 위 3-3 |
| `0xd` | 플래그 | `+0x2b4` | `f+0x198` 비트 11 ← 값 비트 0 |
| `0xe` | 부드러운 최대 | C[12] `+0x5b3` | `f+0x18c` |
| `0xf` | CPU 집합 | C[13] `+0x8b2` + 조각 | 비트 15 켜졌으면 `0xc000a003`; 아니면 `KiValidateCpuSetMasks` → `f+0x78` ← 복사, 비트 14 켬 |
| 0 · 1 · 7 | | A 가 `0xc0000003` | 잘못된 클래스 |
| 6 | | A[6] = `+0x3f5` → `0xc0000002` | `STATUS_NOT_IMPLEMENTED` |

### 지금 우리 (PE_emulator)
- `worker_factory_set_length(cls)` 에 **`4 → 4`, `0xb → 4`** 를 더하면 길이·읽기·핸들·접근 검사는 이미 쓰는 앞 순서 (`+0x82 ~ +0x215`) 와 같다. **클래스 `0xb` 는 스레드 일이 없어 지금 바로 모델할 수 있다** (`f+0x19c` 저장, `P+0x21` 이면 `0x80`). **클래스 4 는 "값이 옛 최소보다 크지 않으면 칸 갱신만 하고 상태 0" 까지는 지금 가능**, 올릴 때는 일꾼을 만들어야 해서 Q2 길이 필요 (그 전엔 `stop` 이 맞다).
- 지금 코드 주석의 **`TODO(VM)` ("그 바이트 뜻·값 근거 없음, 0 으로 봄") 는 닫힌다** — `P+0x21` = `Shutdown`, 새 공장 0 (칸 표). **"공사 중: 클래스 5 … 자리 ②" 는 맞다** (`+0x4bf ~ +0x4c7`, 정확히는 "옛 최대 0 + `P+0x18` ≠ 0" 도 포함). **"자리 ① 클래스 3 → `TryEnterAwayMode` → `CheckCreate`" 도 맞다** (`+0x582`, `+0x59a`), 다만 `TryEnterAwayMode` 는 **묶음 ≠ 0 이고 포트가 비어 있으면 거짓**이라 (`+0x33 ~ +0x6a`) 보통은 `CheckCreate` 까지 안 가고 "멀리 있기" 알림만 건다.

---

## ★ 일감을 넣었는데 일꾼이 0 이면 — 커널이 하는 일 (한 장 요약) `[정적]`

test.exe 같은 **새 풀의 첫 `TppWorkPost`** (`NtReleaseWorkerFactoryWorker(풀+0x38)` 한 번) 기준:

1. **같은 syscall 안에서** `P+0x18 = 1`, `P+0x20 = 1`, 포트에 **0 패킷 1개** (`SignalState = 1`), 기다리는 일꾼 없음 → 직접 넘김 안 하고 줄에 붙임. (`NtRelease +0x10c ~ +0x42f`)
2. 이어서 **`CheckCreate(f, 0, 0)`** (`+0x201`): 만드는 중 0 · 움직이는 일꾼 0 < 최대 · 기다리는 일꾼 0 · 포트에 일 있음 · `CurrentCount 0 < CPU 수` · 기록판 비어 `false` · 부드러운 최대 0 → **전부 참.**
3. `f+0x190 = 1` → **`ExpWorkerFactoryCreateThread`**: 같은 프로세스에 **멈춘 채 스레드 만들고** (`NtCreateThreadEx` 길, `Rip = RtlUserThreadStart(TppWorkerThread, 풀)`, 스택 = exe 헤더 크기) **바로 풀어서** 돌려보냄. `f+0x188 = 1`.
4. 새 일꾼은 (ntdll 쪽) `NtWorkerFactoryWorkerReady` → `f+0x190 = 0`, `f+0x188 = 0`, `f+0x180 = f+0x184 = 1` → `NtWaitForWorkViaWorkerFactory` → 포트에서 **0 패킷을 꺼냄** (콜백이 `P+0x18 = 0`, `P+0x20 = 0`) → 그 일꾼이 풀의 일감 목록을 돌며 일을 한다 `[추측: TppWorkerThread 안은 안 읽음]`.
5. **둘째·셋째 `TppWorkPost` 가 `Ready` 전에 오면 일꾼을 더 안 만든다** (`f+0x190 ≠ 0` → `CheckCreate +0x8b`). 일꾼이 일을 집은 뒤 (`Wait +0x7a9`) 일이 남아 있고 기다리는 일꾼이 없으면 그때 다음 일꾼을 **하나씩** 만든다. 동시에 일하는 수가 CPU 수 이상이면 **30 ms → 120 ms 미룬 뒤 ("길게" = `ThreadCreationTimeout` 초 타이머는 만들기가 실패했을 때만 씀)** 만들어진다 (이 미룸은 시스템 관리 스레드가 함).

→ **우리가 "아무것도 안 해서 모두 잠드는" 건 진짜와 다르다.** 최소한 `Release` 가 (a) 칸 갱신 (b) 포트에 0 패킷 넣기 (c) 일꾼이 0 이고 조건이 맞으면 `PspCreateThread` 다리를 한 번 부르기 — 이 셋이 필요하다. 미루기 (30/120 ms) · 기록판 · 부드러운 최대 · 놀기 제한은 test.exe 길에서는 나중 일 (아래 "동적으로 확인").

---

## 동적분석으로 확인할 것

정적으로는 **간접 호출이 이 길에 없다** (`CheckCreate` · `CreateThread` · `Rtlp...` 호출은 모두 직접). 못 정하는 것은 **실행 때 값**:

1. **test.exe 에서 첫 `NtReleaseWorkerFactoryWorker` 가 실제로 그 자리에서 `ExpWorkerFactoryCreateThread` 까지 가나.** 추천 멈춤점 `nt!ExpWorkerFactoryCreateThread` (조건: 프로세스 이름 test.exe) — 콜스택에 `ExpWorkerFactoryCheckCreate+0x1a0` · `NtReleaseWorkerFactoryWorker+0x206` 가 보여야 한다. 대안 `nt!ZwCreateThreadEx` 입구에서 같은 스택 인자 (7·9·10번째) 확인.
2. **`NtCreateThreadEx` 가 받는 `CreateFlags` = `0x1` 인지 `0x11` 인지**와 `StackSize`/`MaximumStackSize` 값 (exe 헤더 값과 같은지). 추천 `nt!NtCreateThreadEx` 입구: `CreateFlags` = **7번째 인자** (입구 기준 `[rsp+0x38]`, 코드에선 `+0x69 MOV R13D,[RBP+0x330]`), `StackSize` = 9번째, `MaximumStackSize` = 10번째.
3. **`f+0x198` 비트 11 (`LoaderWorker`)** — ntdll 이 클래스 `0xd` 로 어떤 값을 넣는지 (`TpAllocPoolInternal +0x443`, 값 칸 `[RSP+0xb8]`). `TpAllocPoolInternal +0x450 JS` 는 상태만 보므로 동적 값 필요.
4. **`ExpWorkerFactoryThreadIdleTimeoutInSeconds` · `ExpWorkerFactoryThreadCreationTimeoutInSeconds` 의 실행 때 값** — 파일 초기값은 `0x43` (67) · `0xa` (10) 이고 레지스트리 표로 덮일 수 있다 (`[공백]`). `dq nt!ExpWorkerFactoryThreadIdleTimeoutInSeconds l1`.
5. **일꾼 수 칸의 실제 움직임**: 공장 객체 (`dt`/`dq` 로 `f+0x178 ~ +0x1a0`) 와 `P` 를 `Release` 직후 / 일꾼 `Ready` 직후 / 첫 `Wait` 직후 세 번. **`f+0x180` 과 `f+0x184` 가 항상 같은지** (클래스 9 때문에 갈릴 수 있음 `[추측]`).
6. **CPU 수 걸림 (걸림 ①)** 이 test.exe 의 VM 에서 실제로 걸리는가 (VM 의 `KeNumberProcessors` · 포트 `CurrentCount` 값). 걸리면 30 ms 타이머 (`ExpWorkerFactoryThreadCreationTimer`) 가 켜지고 관리 스레드가 처리한다.
7. **`ExpCheckThreadHistory` 반환값** (기록판이 채워진 뒤) — `ret` 지점 `CheckCreate +0x14d` 에서 `al`. 이름만 확인했고 뜻은 `[공백]`.
8. **첫 `Wait` 가 돌려주는 항목** 이 정말 0 인 4칸인지 (`IoRemoveIoCompletion` 출력 `+0x204 ~ +0x212`).
9. 일꾼이 `0x80`·`0x102` 를 받으면 ntdll 이 스레드를 끝내는지 (ntdll `TppWorkerThread`, 정적 후보 아래).

## 다음 요청 후보

1. **ntdll `TppWorkerThread` (3824B) + `TppWorkPost` 둘레** — 일꾼이 꺼낸 "0 패킷" 을 어떻게 알아보고, `Ready`·`Wait` 를 어떤 순서로 부르고, `0x80`·`0x102` 에 스레드를 어떻게 끝내나. 에뮬레이터가 일꾼 스레드 안쪽을 흉내내려면 필요. (`TpSetPoolMinThreads` (156B) · `TpSetPoolThreadBasePriority` (77B) 도 같이: 클래스 4·`0xb` 를 누가 어떤 값으로 부르는지.)
2. **`KiWakeQueueWaiter` · `KeRemoveQueueEx` · `KeTimeOutQueueWaiters` · `KiInsertQueue`** — 포트 `CurrentCount`·`SignalState` 가 정확히 언제 오르내리나 (걸림 ① 의 입력). 완료 포트 쪽 에뮬레이터와 같은 칸을 써야 하므로.
3. **`NtSetInformationWorkerFactory` 클래스 9 몸** (`+0x27b ~ +0x2de`, `+0x8e0 ~ +0x910`, `+0x459 ~ +0x4e6`) — `f+0x180` ±1 과 "값 1 이면 일꾼 만들기" 의 사용자 쪽 호출자. (클래스 8 은 이번에 닫음: 포트 `MaximumCount`.)
4. **`ExpCheckThreadHistory` 의 뜻 (걸림 ②)** 과 `Wait +0x740 ~ +0x7a3` 의 기록판 채우기 규칙 — 우선순위 낮음 (첫 일꾼에선 안 걸림).
5. **`NtQueryInformationWorkerFactory` 에뮬레이터 쪽** (`nt_query_information_worker_factory.cpp`) 은 위 칸 표 (Query 출력 0x78 바이트 매핑) 로 바로 맞출 수 있다 — 이번엔 칸 매핑만 확인: `Timeout`·`RetryTimeout` = 0, `Paused`·`TimerSet` = 0 (`+0x128 AND [RSP+0x58],0`), `QueuedToExWorker` = 비트 3, `MayCreate` = `P+0x1c == 0`, `CreateInProgress` = `f+0x190 > 0`, `InsertedIntoQueue` = `P+0x20`, `Shutdown` = `P+0x21`, `Binding` `f+0x194`, `ThreadMinimum` `f+0x178`, `ThreadMaximum` `f+0x17c`, `Pending` `f+0x188`, `Waiting` `P+0x1c`, `Total` `f+0x184`, `ReleaseCount` `P+0x18`, `StartRoutine` `f+0x18`, `StartParameter` `f+0x20`, `ProcessId` = 프로세스 객체 `+0x1d0`, `StackReserve` `f+0x38`, `StackCommit` `f+0x40`, `LastThreadCreationStatus` `f+0x1a0`; 클래스는 `7` 만 (아니면 `0xc0000003`, `+0x41`), 길이는 정확히 `0x78` (아니면 `0xc0000004`, `+0xb3`), 접근 `8` (`+0xe0`, call `+0xe8`).

## 판정

**분석성공.** 요청서 질문 4개와 특별 질문 (일꾼 0 일 때) 모두 `.asm` 줄 근거로 답했다 (`[정적]`). 공장 객체 칸 표 · 상태 비트 표 · `P` 칸 표를 한 장씩 만들었고 칸 **이름**은 공개 헤더와 맞춘 `[추측]` (칸 위치·동작은 `[정적]`). `ZwCreateThreadEx` 스텁 본문 (30B) · ntdll `TppWorkerThread` · 포트 쪽 `KeRemoveQueueEx`/`KiWakeQueueWaiter` 안쪽 · `ExpCheckThreadHistory` 의 뜻 · 클래스 8·9 몸은 이번 범위 밖이라 `[공백]`/`[추측]` 으로 남겼다. 버전 대조 일치, 스크립트 문제 없음, Ghidra 새 analyze 없음.

# 2026-11-12 run74 — 둘째 스레드 만들기 (Q141a, PE_emulator 요청 25 ㄱ): `NtCreateThreadEx +0x2cd PspCreateThread` → `+0x27e PspAllocateThread` · `+0x2e5 PspInsertThread`. 첫 문맥은 `NtCreateThreadEx` 가 **자기 안에서** 만든다 (`PspCreateUserContext` 안 부름, `Rip` = `RtlUserThreadStart`). 스택 크기 0 → exe 헤더는 **`RtlCreateUserStack +0x22d · +0x244` (0 인 쪽만 따로)**

## PE_emulator 쪽에서 쓸 것

- **한 줄**: 둘째 스레드도 첫 스레드와 **같은 몸 만들기 함수** (`PspAllocateThread` → `PspSetupUserStack` → `RtlCreateUserStack`, `MmCreateTeb`) 를 쓴다. 다른 건 셋 — 문맥을 만드는 곳 (`NtCreateThreadEx` 안), 스택 크기의 출처 (사용자 값 그대로, 0 이면 `RtlCreateUserStack` 이 헤더로), 만들기 플래그 (`0x40` InitialThread 가 절대 안 켜짐 → `SameTebFlags` 를 커널이 안 씀) `[정적]`.
- test.exe 의 `CreateThread(NULL, 0, Worker, NULL, 0, NULL)` (`[소스]` `sample/test.c:34`) 가 커널에 (0, 0) 을 넘긴다면: 예약 `M = 0x100000`, 커밋 `C = 0x1000`, 가드 `0x3000` — **첫 스레드와 같은 값** `[정적]` (유저 쪽이 정말 0 을 넘기는지는 `[추측]`, 아래 동적). (→ run90 `[정적]`: 넘긴다 — `KERNELBASE!CreateRemoteThreadEx +0x280 ~ +0x291` 이 `dwStackSize = 0` 이면 9·10번째를 (0, 0) 으로. 그래서 Worker 스택 = 위 값.)
- 질문마다 "지금 우리" (결과서 sast/69 · sast/70 그대로):

| 질문 | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 69-1 부르는 순서 | `NtCreateThreadEx` 직접 호출 12줄 중 10번째 `PspCreateThread` (`+0x2cd`/`+0x2d2`). 그 안 11줄 중 5번째 `PspAllocateThread` (`+0x27e`/`+0x283`), 7번째 `PspInsertThread` (`+0x2e5`/`+0x2ea`), 각 한 번 | 가정 없음 (해당 없음) | `_calls.tsv`, `NtCreateThreadEx.asm`, `PspCreateThread.asm` |
| 69-2(a) 시작 주소·인자 | `CONTEXT.Rcx` ← StartRoutine (`+0x22b`), `Rdx` ← Argument (`+0x236`) | **맞음** | `NtCreateThreadEx +0x22b` · `+0x236` |
| 69-2(b) `PspCreateUserContext` | **안 부름**. `NtCreateThreadEx` 가 `+0x1d3 ~ +0x26b` 에서 같은 값 세트를 직접 씀 (차이는 CET 일 때 `ContextFlags` 에 `0x40` 더함 하나) | 가정 없음. 재사용해도 레지스터 값은 같지만 **커널 이름·호출관계는 다름** | `NtCreateThreadEx +0x1d3 ~ +0x26b`, 지도에 `PspCreateUserContext` 없음 |
| 69-2(c) `CONTEXT.Rip` | `PsNtdllExports+0x10` 칸 = `RtlUserThreadStart` (`+0x1bf` 읽기 → `+0x220` 쓰기). 사용자 시작 주소가 아님 | "Rip = 시작 주소 그대로" 라면 **틀림** (요청서엔 가정 없음, 확인용) | `NtCreateThreadEx +0x1bf` · `+0x220`, run16 |
| 69-2(d) 스택을 누가 | 커널이 잡는다 (`PspAllocateThread +0x5ea` → `PspSetupUserStack +0x89` → `RtlCreateUserStack`). `StackSize`·`MaximumStackSize`·`ZeroBits` 를 0 검사 없이 그대로 넘김 (`NtCreateThreadEx +0x15f · +0x16a · +0x175`) | "첫 스레드 사슬 재사용" **맞음** (같은 함수 같은 자리). "크기 0 → exe 헤더" 는 sast/69 엔 없었고 → 70-2 로 **맞음** | `NtCreateThreadEx +0x154 ~ +0x175`, `PspSetupUserStack +0x28 · +0x89` |
| 69-3(a) `CreateFlags` 검사 | 허용 마스크 `0x7f`. 어기면 `0xC00000F5`. 맨 처음 검사 | 가정 없음 | `NtCreateThreadEx +0x70` · `+0x77` → `+0x303` |
| 69-3(b) 비트 대응 | `0x1`→`0x1`, `0x2`→`0x2`, `0x4`→`0x4`, `0x10`→`0x80`, `0x20`→`0x100`, `0x40`→`0x200`. `0x8` 은 안 읽힘. 만들기 플래그 `0x20`·`0x40` 은 이 길에서 **안 만듦** | 가정 없음 | `PspCreateThread +0x197 ~ +0x47c` |
| 69-3(c) 새 TEB `SameTebFlags` | `CreateFlags = 0` 이면 `PspAllocateThread +0x66f/+0x673` 에서 `+0x793` (TEB 쓰기) 를 **건너뜀** → 커널이 안 씀. 값은 새 TEB 페이지 그대로 `0` `[추측]` | "첫 스레드 값 `0x400` 을 둘째에도" 라면 **틀림** | `PspAllocateThread +0x66f` · `+0x673` · `+0x793` |
| 69-3(d) `CREATE_SUSPENDED` | 비트 `0x1` → `PspInsertThread +0x2e0 KeSuspendThread` (핸들·준비보다 먼저). 켜도 안 켜도 `+0x5a1 KeReadyThread` 는 불림 | 가정 없음 (비트값 1 은 asm 로 맞음) | `PspInsertThread +0x2d4 ~ +0x2e5` · `+0x5a1` |
| 69-4(a)(b) 속성 목록 검사 | `PspBuildCreateProcessContext` 가 스레드를 만들기 **전** (`NtCreateThreadEx +0xde`). `TotalLength ≥ 0x28`, `(TotalLength − 8) & 0x1f == 0`, 항목마다 `Size`·THREAD 비트 — 어기면 `0xC000000D` | 가정 없음 | `PspBuildCreateProcessContext +0x12a` · `+0x17a` · `+0x1c1 ~ +0x1cd` 등 |
| 69-4(c) 모르는 속성 | **거절** (무시 아님). THREAD 비트 없는 값은 기본 갈래까지도 못 감 | "그 밖은 멈춤" 방향은 **맞음**. 단 "`CLIENT_ID`·`TEB_ADDRESS` 둘만" 은 **좁다** — 그룹 친화도·이상적 프로세서·XState 도 정식으로 받음 (구현 범위는 사용자 결정) | `+0x1690` 기본 갈래, 아래 속성 표 |
| 69-4(d) 출력 포인터가 나쁨 | 보통은 속성 읽을 때 (만들기 전) 읽고-다시-쓰기 시험에서 실패 → 스레드·핸들 없음. 시험 뒤에 나빠지면 스레드는 만들어졌다가 스스로 끝남 | 가정 없음. "만들기 전 실패" 만 흉내 내도 거의 충분 | `PspBuildCreateProcessContext` try `[+0xee,+0x181b)`, `PspInsertThread +0x40c` · `+0x450` · `+0x573` |
| 69-5 `PspBuildCreateProcessContext` | 스레드 쪽도 부름 (`+0xde`). 다른 값은 셋째 인자 하나 (스레드 1 / 프로세스 0) | 가정 없음. 아래 속성 표가 스레드 길에서 받는 전체 | `NtCreateThreadEx +0xd5` vs `NtCreateUserProcess +0x28c` |
| 70-1 `RCX`·`RDX` | 커밋 · 예약(reserve) 크기 맞음 | **맞음** | `PspSetupUserStack +0x80` · `+0x7c` |
| 70-2 0 → 다른 값 | 있다. 0 인 쪽만 **따로** exe 헤더로 (`+0x22d` 커밋 ← `SizeOfStackCommit`, `+0x244` 예약 ← `SizeOfStackReserve`) | "스택 크기 0 → exe 헤더" **맞음** (칸·오프셋까지). 단 "둘 다 0 일 때만" 이 아니라 "0 인 쪽만", 그 뒤 다듬기 규칙이 붙음 | `RtlCreateUserStack +0x6e/+0x7b` → `+0x1eb ~ +0x250` |
| 70-3 다듬기 | 커밋 0 → `0x4000` · 커밋 ≥ 예약 → 예약 = 커밋 1MB 올림 · 커밋 쪽 올림 · 예약 64KB 올림 · `PEB.MinimumStackCommit` 이 크면 커밋을 그 값으로. **예약 최소 `0x40000` 은 이 함수에 없음** | 가정 없음. ①→②→③④→⑤ 순서가 구현 대상, `X = 0` 이면 ⑤ 빠짐 | `+0x81 ~ +0xb5`, `+0xb8 ~ +0xce`, `+0x299 ~ +0x2e4` |
| 70-4 `RtlImageNtHeader` | 70-2 의 헤더 읽기 맞음. 오프셋은 뒤지만 실행은 할당보다 **먼저** | 가정 없음 | `+0x202` (ret `+0x207`) |
| 70-5 할당 꼴 | 예약 = `ZwSetInformationProcess(0x29)` (`+0x104`), 커밋 = `ZwAllocateVirtualMemory(MEM_COMMIT, PAGE_READWRITE)` (`+0x170`), 가드 = 커밋 바로 아래 `3P` 를 `0x104` (`PAGE_READWRITE\|PAGE_GUARD`) 로 커밋 (`+0x1c6`, `M − C ≥ 3P` 일 때만). 실패면 `+0x28d ZwFreeVirtualMemory(MEM_RELEASE)` | 가정 없음. 예약은 `ZwSetInformationProcess(0x29)` 로, 커밋·가드는 `ZwAllocateVirtualMemory` 로 해야 **호출 이름이 맞음** | `+0x104` · `+0x170` · `+0x1c6` · `+0x28d` |
| 70-6 InitialTeb | `AllocationBase = A` (`+0x128`), `StackBase = A + M` (`+0x130`), `StackLimit = A + M − C` (`+0x187`) | 가정 없음 | `RtlCreateUserStack +0x123 ~ +0x187` |
| 70-7 `R9`·`R8` | `R9` = 페이지 크기 `P` (아래 56비트, `0x1000`) \| 선호 노드 (위 8비트, `> 0x40` 이면 `0xC000000D`). `R8` = ZeroBits → 예약 정보 칸 `+0x18` 에만 | 가정 없음. `R9` 의 `0x1000` 은 `MEM_COMMIT` 과 우연히 같은 숫자 | `+0x2e ~ +0x46`, `+0xe9` |

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (두 결과서 모두 기준 GUID 와 같음). 다시 analyze 안 함
- 버전 대조 (두 결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치. sast/69 는 run9·run11·run54 의 `[정적]` 줄 (`NtCreateUserProcess +0x295/+0x29a` · `+0xd1d/+0xd22` · `+0x1015/+0x101a`, `PspAllocateThread +0x640 ~ +0x655` · `+0x54a ~ +0x5a5` · `+0x793`) 도 다시 맞춤

## 목적

- PE_emulator 요청 25 ㄱ (Q141a): 둘째 스레드 (`NtCreateThreadEx`) 를 만드는 커널 길이 첫 스레드 (`NtCreateUserProcess`) 길과 **어디서 같고 어디서 다른가** — 부르는 순서, 첫 문맥, 스택, `CreateFlags`, 속성 목록.
- 같은 바퀴 (사용자 결정 2026-11-12 "이번 바퀴에 하나 더"): sast/69 가 못 닫은 2(d) "스택 크기 0 → exe 헤더" 를 `RtlCreateUserStack` 본문으로 닫기 (sast/70).

## 방법

- 정적 ①: `_workspace/sast/69_thread-create.md` — ntoskrnl
  - 새 지도: `run_ghidra.sh map ntoskrnl.exe NtCreateThreadEx` → `ms/disassemble/ntoskrnl/_map/NtCreateThreadEx_{edges,nodes}.tsv` (함수 2620개)
  - 새로 뽑음: `^(NtCreateThreadEx|PspCreateThread)$` depth 1 (20개), `^(PspSetupUserStack|MmCreateTeb)$` depth 0
  - 있던 것 읽기만: `PspBuildCreateProcessContext` (스레드 쪽 갈래만), `PspAllocateThread`, `PspCreateUserContext`, `PspInsertThread`, `NtCreateUserProcess` (비교용)
  - 예외 표 (`.pdata` try 범위·착지점) 는 임시 스크립트 + `objdump` 로 읽음 (레포 밖, 읽기 전용)
- 정적 ②: `_workspace/sast/70_thread-stack.md` — ntoskrnl `^RtlCreateUserStack$` depth 0 (이미 있던 `.asm`·`.c`, 새로 0개)
  - 같이 읽은 것: `PspSetupUserStack +0x28 ~ +0x89`, `NtCreateThreadEx +0x154 ~ +0x175`, `NtCreateUserProcess +0xd92 ~ +0xdbc`, `PspAllocateThread +0x11d · +0x5c6 ~ +0x5ea · +0x8c7`
  - 범위 밖으로 넓힘 (값의 출처): `MmCreatePeb +0x4c6/+0x4cc`, ntdll `LdrpInitializeExecutionOptions +0x406 ~ +0x413`, `_refs/MmRegistryState.tsv`, ntoskrnl PE 의 레지스트리 표 한 항목 (바이트), `sample/bin/test.exe` 헤더
- 결과서: `_workspace/sast/69_thread-create_result.md`, `_workspace/sast/70_thread-stack_result.md` (둘 다 **분석성공**)
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `NtCreateThreadEx +0x70` · `+0xde` · `+0x1bf` · `+0x22b` · `+0x2cd`, `PspAllocateThread +0x66f` · `+0x673`, `PspInsertThread +0x2e0` · `+0x5a1`, `RtlCreateUserStack +0x6e` · `+0x7b` · `+0x1eb` · `+0x1fb` · `+0x202` · `+0x22d` · `+0x244`. test.exe 헤더 (`sample/bin/test.exe`, PE32+) `SizeOfStackReserve 0x100000` · `SizeOfStackCommit 0x1000` 은 오케스트레이터가 파일에서 직접 읽음
- flow-writer 도 `.asm` 으로 다시 봤다: `NtCreateThreadEx +0x154` · `+0x15f` · `+0x16a` · `+0x175` · `+0x220` · `+0x236` · `+0x303`, `PspCreateThread +0x1a5` · `+0x1c6` · `+0x1cd` · `+0x1d7` · `+0x1e0` · `+0x27e` · `+0x2e5`, `PspSetupUserStack +0x28` · `+0x70` · `+0x75` · `+0x7c` · `+0x80` · `+0x89` · `+0xdc` · `+0xe3`, `PspAllocateThread +0x5ea` · `+0x62c` · `+0x793` (`+0x673` 목적지 = `+0x780`), `PspInsertThread +0x8e` · `+0x2d4`, `RtlCreateUserStack +0x81` · `+0x89` · `+0x8d` · `+0x90` · `+0xa7` · `+0xb5` · `+0xb8` · `+0xe4` · `+0xe9` · `+0xee` · `+0xf9` · `+0x104` · `+0x123` · `+0x128` · `+0x130` · `+0x14a` · `+0x158` · `+0x170` · `+0x187` · `+0x18b` · `+0x18e` · `+0x1a3` · `+0x1c6` · `+0x1f4` · `+0x20a` · `+0x20d` · `+0x214` · `+0x21a` · `+0x21e` · `+0x25d` · `+0x28d` · `+0x2c0` · `+0x2c7`, `NtCreateUserProcess +0xd8c ~ +0xdbc`, `MiBuildImageControlArea +0x175 ~ +0x181`
- 값 대조 (첫 스레드, 새 실행 없음): run44 덤프 읽기 로그 `ms/logs/04_baggage-read_K2.txt` (TEB 스택 칸 3028줄, `DeallocationStack` 1856줄, 스택 VAD 1372줄, 트랩 프레임 `Rsp` 1108줄, PEB `+0x318` 1635줄) 와 P0 ~ P2 의 같은 줄

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret | callee | 조건·뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtCreateThreadEx` | `+0xb8` | `+0xbd` | `memset` | `ctx` 0x260B 비움 (앞 `+0x70` `CreateFlags` 검사) | [정적] |
| `nt!NtCreateThreadEx` | `+0xde` | `+0xe3` | `nt!PspBuildCreateProcessContext` | `AttributeList ≠ NULL` 일 때만 (`+0xbd`·`+0xc0`), 셋째 인자 1 | [정적] |
| `nt!NtCreateThreadEx` | `+0x127` | `+0x12c` | `nt!ObpReferenceObjectByHandleWithTag` | 프로세스 핸들 | [정적] |
| `nt!NtCreateThreadEx` | `+0x191` | `+0x196` | `memset` | InitialTeb 칸 0x48B (`SecureProcess` 0 일 때) | [정적] |
| `nt!NtCreateThreadEx` | `+0x1d3` | `+0x1d8` | `nt!RtlGetExtendedContextLength2` | CONTEXT 길이 | [정적] |
| `nt!NtCreateThreadEx` | `+0x1f3` | `+0x1f8` | 스택 탐침 (같은 주소 이름 2개, 후보: `__chkstk`·`_alloca_probe`) | 스택에 CONTEXT 자리 | [정적] |
| `nt!NtCreateThreadEx` | `+0x205` | `+0x20a` | `memset` | CONTEXT 0 | [정적] |
| `nt!NtCreateThreadEx` | `+0x217` | `+0x21c` | `nt!RtlInitializeExtendedContext2` | `ContextFlags` | [정적] |
| `nt!NtCreateThreadEx` | `+0x27d` | `+0x282` | `nt!ObfDereferenceObjectWithTag` | 위 프로세스 참조 되돌림 | [정적] |
| `nt!NtCreateThreadEx` | **`+0x2cd`** | **`+0x2d2`** | **`nt!PspCreateThread`** | 13 인자 | [정적] |
| `nt!NtCreateThreadEx` | `+0x2d8` | `+0x2dd` | `nt!PspDeleteCreateProcessContext` | `ctx` 풀 정리 | [정적] |
| `nt!PspCreateThread` | `+0x349` | `+0x34e` | `nt!PspIsProcessReadyForRemoteThread` | 대상이 지금 프로세스가 아닐 때만 | [정적] |
| `nt!PspCreateThread` | `+0x204` | `+0x209` | `nt!ExAcquireRundownProtection` | 실패면 `0xC000010A` | [정적] |
| `nt!PspCreateThread` | **`+0x27e`** | **`+0x283`** | **`nt!PspAllocateThread`** | 5번째 (정상 길) | [정적] |
| `nt!PspCreateThread` | **`+0x2e5`** | **`+0x2ea`** | **`nt!PspInsertThread`** | 7번째 (정상 길) | [정적] |
| `nt!PspAllocateThread` | `+0x5ea` | `+0x5ef` | `nt!PspSetupUserStack` | 첫 스레드와 같은 자리 (x64 길) | [정적] |
| `nt!PspAllocateThread` | `+0x62c` | `+0x631` | `nt!MmCreateTeb` | 첫 스레드와 같은 자리 | [정적] |
| `nt!PspSetupUserStack` | `+0x70` | `+0x75` | `nt!KiStackAttachProcess` | 대상 프로세스에 붙기 | [정적] |
| `nt!PspSetupUserStack` | **`+0x89`** | **`+0x8e`** | **`nt!RtlCreateUserStack`** | `RCX` 커밋 · `RDX` 예약 · `R8` ZeroBits · `R9` `P \| 노드<<56` | [정적] |
| `nt!PspSetupUserStack` | `+0xac` | `+0xb1` | `nt!ExGenRandom` | `CONTEXT.Rsp` 랜덤 몫 | [정적] |
| `nt!PspSetupUserStack` | `+0xe3` | `+0xe8` | `nt!KiUnstackDetachProcess` | 떼기 | [정적] |
| `nt!RtlCreateUserStack` | `+0x202` | `+0x207` | `nt!RtlImageNtHeader` | 0 인 크기가 있을 때만 (실행은 맨 앞) | [정적] |
| `nt!RtlCreateUserStack` | `+0x104` | `+0x109` | `nt!ZwSetInformationProcess` | 클래스 `0x29`, 길이 `0x28` — 예약 `[추측: 본문 안 봄]` | [정적] |
| `nt!RtlCreateUserStack` | `+0x170` | `+0x175` | `nt!ZwAllocateVirtualMemory` | 커밋 `C`, `PAGE_READWRITE` | [정적] |
| `nt!RtlCreateUserStack` | `+0x1c6` | `+0x1cb` | `nt!ZwAllocateVirtualMemory` | 가드 `3P`, `0x104` (`M − C ≥ 3P` 일 때) | [정적] |
| `nt!RtlCreateUserStack` | `+0x28d` | `+0x292` | `nt!ZwFreeVirtualMemory` | 커밋·가드 실패 때 통째로 풀기 | [정적] |
| `nt!PspInsertThread` | `+0x8e` | `+0x93` | `nt!KeAbPreAcquire` | 프로세스 락을 **직접** 잡음 (flags `0x20` 없음) | [정적] |
| `nt!PspInsertThread` | `+0x2e0` | `+0x2e5` | `nt!KeSuspendThread` | 만들기 플래그 비트 0 일 때만 | [정적] |
| `nt!PspInsertThread` | `+0x5a1` | `+0x5a6` | `nt!KeReadyThread` | 늘 (정상 길 맨 끝) | [정적] |

- 지도 `_map/NtCreateThreadEx_{nodes,edges}.tsv` (깊이 10까지) 에 `PspCreateUserContext` 가 **없다**. 그 함수의 부르는 곳은 `NtCreateUserProcess +0xd1d` 하나뿐 (`callers_total` 1) — sast/69 결과서.

### 첫 스레드 길과 비교 (sast/69 · sast/70 결과서 표를 합침)

| 항목 | 첫 스레드 (`NtCreateUserProcess`) | 둘째 스레드 (`NtCreateThreadEx`) |
|---|---|---|
| CONTEXT 만드는 곳 | `PspCreateUserContext` (`+0xd1d/+0xd22`) | `NtCreateThreadEx` 안 (`+0x1d3 ~ +0x26b`), **같은 값 세트** |
| `Rip` | `PsNtdllExports+0x10` (`+0xd10`) | 같은 칸 (`+0x1bf`) = `RtlUserThreadStart` |
| `Rcx` · `Rdx` | 진입점 (`TransferAddress`, `+0xd09`) · PEB | StartRoutine · Argument |
| 스택 정보 커밋 (`+0x10`) | 이미지 정보 `CommittedStackSize` (`+0xdaa/+0xdb1`) | 9번째 인자 그대로 (`+0x158/+0x15f`) — 0 이 올 수 있음 |
| 스택 정보 예약 (`+0x18`) | 이미지 정보 `MaximumStackSize`, **최소 `0x40000`** (`+0xd92 ~ +0xda5`) | 10번째 인자 그대로 (`+0x163/+0x16a`) — 0 이 올 수 있음, 최소값 없음 |
| `RtlCreateUserStack` 헤더 갈래 (`+0x1eb`) | 이미지 정보가 0 이 아니면 안 탐 | 0 인 쪽이 있으면 탐 |
| 스택·TEB 잡기 | `PspAllocateThread +0x5ea/+0x62c` | **같음** |
| `PspAllocateThread` 부르는 곳 | `NtCreateUserProcess +0xe57/+0xe5c` | `PspCreateThread +0x27e/+0x283` |
| `PspInsertThread` 부르는 곳 | `NtCreateUserProcess +0x1015/+0x101a` | `PspCreateThread +0x2e5/+0x2ea` |
| 만들기 플래그 `0x20`·`0x40` | `NtCreateUserProcess +0xdfd` 가 늘 켬 (`0x60`) | **안 만듦** |
| 프로세스 락 (`+0x1c8`) | 호출자가 잡고 들어옴 | `PspInsertThread +0x8e` 가 직접 잡음 |
| `PspBuildCreateProcessContext` 셋째 인자 | 0 (`+0x28c`) | 1 (`+0xd5`) |
| `PEB.MinimumStackCommit` (다듬기 ⑤) | 레지스트리 값만 (IFEO 는 아직 안 읽힘) `[추측]` | IFEO 값까지 쓰일 수 있음 `[추측]` |

### 첫 문맥 (질문 69-2)

- `NtCreateThreadEx` 가 스택 위에 CONTEXT 를 만들고 (`RBX`) 이렇게 쓴다 `[정적]`:
  - `Rip` (+0xf8) ← `PsNtdllExports+0x10` (`+0x1bf` 읽기, `+0x220` 쓰기). 그 칸 = `RtlUserThreadStart` 는 run16 `[정적]`.
  - `Rcx` (+0x80) ← StartRoutine (`+0x22b`), `Rdx` (+0x88) ← Argument (`+0x236`).
  - 세그먼트·`MxCsr`·`FltSave` 값은 `PspCreateUserContext` 와 똑같다 (`SegCs 0x33` `+0x254`, `SegDs·SegEs 0x2b` `+0x242`, `SegFs 0x53`·`SegGs 0x2b` `+0x249`, `SegSs 0x2b` `+0x250`, `MxCsr 0x1f80` `+0x25f`, `ControlWord 0x27f` `+0x262`).
  - `ContextFlags` = `0x10000b`. 프로세스 `MitigationFlags2` (+0x754) 비트 14 (`CetUserShadowStacks`) 면 `0x40` 을 더하고 확장 마스크 `0x800` 을 붙인다 (`+0x196 ~ +0x1bc`). `PspCreateUserContext` 는 `0x10000b` 고정 — 차이는 이것뿐.
- 이어서 `PspAllocateThread +0x640 ~ +0x655` 가 `StartAddress` ← `Rip`, `Win32StartAddress` ← `Rcx` (run11) → 둘째 스레드는 `StartAddress` = `RtlUserThreadStart`, `Win32StartAddress` = 사용자 시작 함수.
- `Rsp` 는 여기서 안 쓴다 — `PspSetupUserStack +0xdc` 가 `StackBase − ((ExGenRandom(1) & 0x7f) << 4) − 0x28` 로 (`EPROCESS.MitigationFlags` +0x750 비트 6 이 켜져 있으면 랜덤 몫 0, `+0x9f/+0xa1`).
- 이 CONTEXT 는 프로세스 `SecureProcess` (`+0x170` 비트 0) 가 0 일 때만 만든다 (`+0x138`·`+0x13f JNZ`). 1 이면 `PspCreateThread +0x366` 이 플래그 `0x400` 을 켜고 다른 길. test.exe 는 일반 프로세스라 앞쪽 길 `[추측]`.

### `CreateFlags` (질문 69-3)

- 허용 마스크 `0x7f` — `NtCreateThreadEx +0x70 TEST R13D,0xffffff80` → `+0x77 JNZ` → `+0x303 MOV EAX,0xc00000f5` `[정적]`. 비트 이름 (`THREAD_CREATE_FLAGS_*`) 은 공개 헤더에 맞춘 `[추측: 이름]`.

| CreateFlags | 만들기 플래그 (`PspCreateThread`) | 그 뒤 쓰이는 곳 |
|---|---|---|
| `0x1` (CREATE_SUSPENDED) | `0x1` (`+0x1a5`) | `PspInsertThread +0x2d4` → `+0x2e0 KeSuspendThread` |
| `0x2` (SKIP_THREAD_ATTACH) | `0x2` (`+0x44d`) | `PspAllocateThread +0x56e ~ +0x572` → `SameTebFlags 0x8` |
| `0x4` (HIDE_FROM_DEBUGGER) | `0x4` (`+0x45d`) | `PspAllocateThread +0x3eb` → `+0xb04` `CrossThreadFlags` 비트 2 |
| `0x8` | — | 읽는 줄 없음 (통과만) |
| `0x10` (LOADER_WORKER) | `0x80` (`+0x1cd BTS 7`) | `PspAllocateThread +0x576 ~ +0x587` → `SameTebFlags 0x2000` |
| `0x20` (SKIP_LOADER_INIT) | `0x100` (`+0x46d BTS 8`) | `PspAllocateThread +0x590 ~ +0x5a0` → `SameTebFlags 0x4000` |
| `0x40` (BYPASS_PROCESS_FREEZE) | `0x200` (`+0x47c BTS 9`) | `PspAllocateThread +0x3f3` → `+0xb10` `KTHREAD.MiscFlags` 비트 21 |
| (없음) | `0x20`·`0x40` 안 만듦 | `0x40` → `SameTebFlags 0x400` (InitialThread) 는 **첫 스레드 길만** |

- run54 의 `+0x54a ~ +0x5a5` 사슬이 이걸로 이어진다: `0x2` ← `0x2`, `0x80` ← `0x10`, `0x100` ← `0x20`. `0x40` 은 이 길에서 절대 안 켜진다 `[정적]`.
- **`CreateFlags = 0` 일 때 `SameTebFlags`**: 만들기 플래그 0 (`PspCreateThread +0x1a1`) → `PspAllocateThread` `R12W = 0` → **`+0x66f TEST R12W,R12W` · `+0x673 JNZ +0x780` 안 탐** → `+0x793` (TEB `+0x17ee` 통째 쓰기) **건너뜀**. `MmCreateTeb` 에도 `+0x17ee` 쓰기가 없다 → 커널은 이 칸을 **안 쓴다** `[정적]`. 값은 새 TEB 페이지 그대로라 `0` 일 것 `[추측]` (동적 Q42).
- `CREATE_SUSPENDED` 는 핸들·준비 **전에** `KeSuspendThread +0x2e0` 로 멈춤 표시를 하고, 켜든 안 켜든 `KeReadyThread +0x5a1` 은 불린다 (sast/07 과 같음) `[정적]`. 출력 순서: `ObInsertObjectEx +0x3e5` → TEB 주소 `+0x40c` → ClientId `+0x450` → `+0x490` (`+0x5a0 |= 2`, 결과서는 `ThreadInserted`) → `PspCallThreadNotifyRoutines +0x4f5` → `PspCreateObjectHandle +0x520` → 핸들 쓰기 `+0x573` → `KeReadyThread +0x5a1`.

### 속성 목록 (질문 69-4 · 69-5)

- 검사는 모두 `PspBuildCreateProcessContext` 안, **스레드를 만들기 전** (`NtCreateThreadEx +0xde`, 프로세스 참조 `+0x127` 보다도 앞) `[정적]`.
- 크기 규칙 (모두 `0xC000000D`): `TotalLength ≥ 0x28` (`+0x12a`), `(TotalLength − 8) & 0x1f == 0` (항목 하나 0x20B, `+0x17a`), 항목 `Size` 가 속성별 값, 같은 속성 두 번 (`+0x1f8 ~ +0x200`), INPUT 비트 항목은 `ReturnLength` 칸 NULL (`+0x1d9 ~ +0x1e4`), **스레드 모드면 항목마다 THREAD 비트 `0x10000`** (`+0x1c1 ~ +0x1cd`). 목록 정렬 4B 어기면 정렬 예외.
- 스레드 길에서 받는 속성 (값은 asm 상수, 이름은 공개 헤더 맞춤 `[추측]`):

| 값 | 이름 `[추측]` | 갈래 | Size | 하는 일 |
|---|---|---|---|---|
| `0x10003` | CLIENT_ID | `+0x5bb` | `0x10` | 출력 포인터 → `ctx+0x18` → `PspInsertThread +0x450` |
| `0x10004` | TEB_ADDRESS | `+0x52c` | `8` | 출력 포인터 → `ctx+0x20` → `PspInsertThread +0x40c` |
| `0x3000c` | GROUP_AFFINITY | `+0xd5f` | `0x10` | `ctx+0x140`, `KeVerifyGroupAffinity` 통과해야. 스레드 전용 |
| `0x3000e` | IDEAL_PROCESSOR | `+0xcfe` | `4` | `KeGetProcessorIndexFromNumber` → `ctx+0x14`. 스레드 전용 |
| `0x3001e` | ENABLE_OPTIONAL_XSTATE_FEATURES | `+0x15bc` | `8` | 공유 데이터 두 칸 OR 의 부분집합, `& 0x40000` → `ctx+0x1e0`. 스레드 전용 |
| 그 밖의 THREAD 비트 값 | — | `+0x1690` | — | 기능 게이트 (`Feature_CreateProcessSegmentHeapEnablement…`) · 번호 `< 0x25` · 값 비트 `0x80000` · 크기가 다 맞아야 받음. 아니면 거절 |

- 프로세스용 속성 (`PARENT_PROCESS`·`TOKEN` 등) 은 THREAD 비트가 없어 스레드 모드에서 모두 거절.
- 출력 포인터가 나쁠 때 (69-4(d)) `[정적]`:
  - 보통: 속성 읽을 때 읽고-다시-쓰기 시험 (CLIENT_ID `+0x5e6 ~ +0x5ed`, TEB_ADDRESS `+0x558 ~ +0x55f`) 에서 예외 → try `[+0xee,+0x181b)` 착지점 `+0x181b` → 풀 정리 → `NtCreateThreadEx +0xe5 JS` 로 끝. **스레드·핸들 없음**, 돌려주는 값 = 예외 코드. 단 `ReturnLength` 칸은 이미 써 버린 뒤 (`+0x5af`·`+0x63d`).
  - 경쟁 (시험 뒤에 나빠짐): `PspInsertThread` 의 쓰기 셋 (`+0x40c`·`+0x450`·`+0x573`) 이 각각 try 안. TEB·ClientId 실패면 `ThreadInserted` 를 건너뛰고도 `KeReadyThread` 를 불러, 스레드가 처음 돌 때 `PspUserThreadStartup` 이 `0xC000004B` 로 스스로 끝난다 (sast/10). 핸들 쓰기 실패면 핸들 닫고 `KeRequestTerminationThread`. 호출자는 실패를 받고 핸들은 안 남는다.
- `PspBuildCreateProcessContext` 의 셋째 인자: 스레드 1 (`NtCreateThreadEx +0xd5`), 프로세스 0 (`NtCreateUserProcess +0x28c`). 바뀌는 것 — THREAD 비트 필수, 스레드 전용 속성 셋을 받음, 끝에서 `ctx+0x10` 비트 0 ← (셋째 ≠ 0) (`+0x1876 ~ +0x1882`, 읽는 곳은 안 따라감).
- 만든 `ctx` 를 스레드 만드는 중에 읽는 곳: `PspAllocateThread +0x165 ~ +0x1c8` (친화도·이상적 프로세서·XState), `PspInsertThread +0x1cb ~ +0x202` (TEB 주소 출력·친화도 → `KeStartThread +0x5dd`).

### 스택 — `RtlCreateUserStack` (질문 69-2(d) · 70-1 ~ 70-7)

- 넘어오는 길 `[정적]`: `NtCreateThreadEx +0x154` (`[StackInfo] = 0` — "이미 있음" 비트 늘 0) · `+0x15f` (커밋 ← 9번째 인자) · `+0x16a` (예약 ← 10번째) · `+0x175` (ZeroBits ← 8번째) → `PspCreateThread` 가 11번째 인자로 그대로 → `PspAllocateThread +0x5ea` → `PspSetupUserStack +0x28 TEST [R9],1` (0 이라 만듦) · `+0x80 RCX = [+0x10]` · `+0x7c RDX = [+0x18]` · `+0x75 R8 = [+0x8]` · `R9 = 0x1000 (| 노드 << 56)` → `+0x89 RtlCreateUserStack`. **중간에 0 검사·치환 없음.**
- `RtlCreateUserStack` (rva `0x938db0`, 738B) **실행 순서** `[정적]` (`_calls.tsv` 는 오프셋 순서라 `+0x202` 가 맨 뒤로 보이지만 실행은 앞):
  1. 노드 검사: `+0x2e/+0x31` 위 8비트 → `+0x42 CMP R14B,0x40` · `+0x46 JA` → `0xC000000D` (`+0x2d3`).
  2. 0 검사: `+0x6e` (커밋) · `+0x7b` (예약) 하나라도 0 이면 `+0x1eb` 로.
  3. 헤더 블록 `+0x1eb ~ +0x250`: `GS:[0x188]` → `+0x1f4 [RAX+0xb8]` (`_KTHREAD.ApcState.Process`, 붙은 프로세스) → `+0x1fb [RCX+0x2b0]` (`_EPROCESS.SectionBaseAddress`) → **`+0x202 RtlImageNtHeader`**. NULL 이면 `0xC000007B` (`+0x25d`). `Magic == 0x20b` (`+0x214`) 면 NT `+0x68` 커밋 (`+0x21a`) · `+0x60` 예약 (`+0x21e`), 아니면 4바이트 칸 (`+0x255`·`+0x258`). **`+0x22d CMOVZ`** (커밋이 0 일 때만) · **`+0x244 CMOVZ`** (예약이 0 일 때만) → `+0x250 JMP +0x81`.
     - 헤더는 PEB `ImageBaseAddress` 가 아니라 **프로세스 객체 칸** `SectionBaseAddress`. 붙은 프로세스라 원격 스레드도 그 프로세스 exe 헤더를 읽는다.
  4. 다듬기 (`c` 커밋, `m` 예약, `P` 페이지 크기):
     - ① 커밋이 그래도 0 → `0x4000` (`+0x81 ~ +0x89`)
     - ② `c ≥ m` 이면 `m = c` 를 1MB 로 올림 (`+0x8d/+0x90` → `+0x2c0 ~ +0x2c7`, 같을 때도)
     - ③ `C = c` 를 `P` 로 올림 (`+0x96 ~ +0xa4`, `P` 가 2의 거듭제곱인지 검사 없음)
     - ④ `M = m` 을 64KB 로 올림 (`+0xa7 ~ +0xb5`)
     - ⑤ `X = PEB.MinimumStackCommit` (`+0xb8 [R13+0x318]`) 이 0 이 아니고 `C < X` 면 `C = X` 를 `P` 로 올림, 그래서 `C ≥ M` 이면 `M = C` 를 1MB 로 (`+0x299 ~ +0x2e4`)
     - 예약 최소값·위 한도는 이 함수에 **없다**.
  5. 예약: `+0x104` (ret `+0x109`) `ZwSetInformationProcess(-1, 0x29, 정보 40B, 0x28)` — 정보 칸 `+0` 노드 (`+0xd4`) · `+0x10` `M` (`+0xe4`) · `+0x18` ZeroBits (`+0xe9`) · `+0x20` 출력 `A`. 클래스 이름 `ProcessThreadStackAllocation` 은 공개 헤더 값 맞춤 `[추측: 이름]`, "예약을 한다" 는 본문을 안 봐서 `[추측]` (`MEM_RESERVE` 가 함수 어디에도 없고, 뒤에서 `A + M − C` 를 쓰고 실패 때 `A` 를 `MEM_RELEASE` 하는 것으로 읽음). 실패면 InitialTeb 를 건드리기 전에 반환.
  6. InitialTeb (`+0x114` `RSI`): `+0x11c`·`+0x11f` 앞 두 칸 0, `+0x128` `AllocationBase = A`, `+0x130` `StackBase = A + M`.
  7. 커밋: `+0x170` (ret `+0x175`) `ZwAllocateVirtualMemory(-1, &(A + M − C), 0, &C, 0x1000 MEM_COMMIT, 4 PAGE_READWRITE)` (`+0x14a`·`+0x158`) → `+0x187` `StackLimit = A + M − C`.
  8. 가드: `M − C ≥ 3P` (`+0x18b CMP RBX,R15` · `+0x18e JC`) 면 `+0x1c6` (ret `+0x1cb`) `ZwAllocateVirtualMemory(…, 커밋 바닥 − 3P, 3P, MEM_COMMIT, 0x104)` (`+0x1a3`) — **3쪽** (`P = 0x1000` 이면 `0x3000`). InitialTeb 는 안 바꿈.
  9. 7·8 실패면 `+0x26c` → `+0x28d ZwFreeVirtualMemory(&A, &0, 0x8000 MEM_RELEASE)` (이미 쓴 InitialTeb 칸은 안 지움).
- 그 뒤: `MmCreateTeb +0x12a ~ +0x13a` 가 InitialTeb `+0x10/+0x18/+0x20` 을 TEB `StackBase`/`StackLimit`/`DeallocationStack` 로 옮김 (run54).
- `X` (`PEB+0x318`) 의 출처: 커널 `MmCreatePeb +0x4c6/+0x4cc` (`MmRegistryState+0x28` ← 레지스트리 `MinimumStackCommitInBytes`, run70) · ntdll `LdrpInitializeExecutionOptions +0x406 ~ +0x413` (IFEO 값이 더 클 때만). 그래서 첫 스레드는 레지스트리 값만, 둘째부터는 IFEO 값까지 볼 수 있다 `[추측: 순서로만]`.
- ntdll 에도 같은 이름 `RtlCreateUserStack` (rva `0xf5130`, 709B) 이 있다. 크기가 비슷해 같은 모양일 것 `[추측]` — 본문 안 봄. (→ run90 `[정적]`: 같은 꼴. 다른 점은 헤더를 PEB `ImageBaseAddress` + `RtlImageNtHeaderEx` 로 찾음 · PE32 갈래 없음 · 5번째 인자 (할당 단위) · `Nt*` syscall. 부르는 곳은 kernelbase `CreateFiberEx +0xce` 하나.)

### PE_emulator ㄱ-2 최종 답 (sast/70 결과서 요약)

- 둘째 스레드가 (0, 0) 을 받으면: 커널은 0 을 그대로 `RtlCreateUserStack` 까지 내려보내고, **0 인 쪽마다** exe 헤더 값으로 바꾼다 → 다듬기 → 예약 `M` · 커밋 `C` · 가드 `3P`.
- test.exe (`SizeOfStackReserve 0x100000`, `SizeOfStackCommit 0x1000`, 오케스트레이터가 파일에서 읽음) 면 `M = 0x100000`, `C = 0x1000`, 가드 `0x3000` (`PEB.MinimumStackCommit = 0` 일 때).
- **첫 스레드와 같은 값.** 첫 스레드는 이미지 정보 값으로 미리 채워 헤더 갈래를 안 타지만, 이미지 정보 두 칸이 곧 헤더 값이다 (아래 "결과서와 다르게 적은 곳" ①) — 같은 다듬기를 거쳐 같은 `M`·`C`.
- **다른 경우**: 헤더 예약이 `0x40000` 보다 작으면 첫 스레드만 `0x40000` 으로 올라가고 둘째는 헤더 값 (64KB 올림만). `MinimumStackCommit` 은 둘째부터 IFEO 값까지.

### 호출 관계

```
NtCreateThreadEx                            (인자 11개, 이름은 [추측])
 │  +0x70  CreateFlags & ~0x7f → 0xC00000F5        (맨 먼저)
 ├─ (1) +0xe3   PspBuildCreateProcessContext(목록, 모드, 1, &ctx)   목록 ≠ NULL 일 때만
 ├─ (2) +0x12c  ObpReferenceObjectByHandleWithTag  (프로세스)
 │  +0x154 ~ +0x175  StackInfo ← ZeroBits · StackSize · MaximumStackSize 그대로
 │  +0x1d8 ~ +0x21c  CONTEXT 만들기 → +0x220 Rip = RtlUserThreadStart · +0x22b Rcx · +0x236 Rdx
 ├─ (3) +0x282  ObfDereferenceObjectWithTag
 └─ (4) +0x2d2  PspCreateThread
      ├─ (1) +0x283  PspAllocateThread                          (첫 스레드와 같은 함수)
      │    │  +0x66f/+0x673  R12W = 0 이면 +0x793 (SameTebFlags 쓰기) 건너뜀
      │    ├─ (1) +0x5ef  PspSetupUserStack
      │    │    ├─ (1) +0x75  KiStackAttachProcess
      │    │    ├─ (2) +0x8e  RtlCreateUserStack
      │    │    │    ├─ (1) +0x207  RtlImageNtHeader            (크기 0 이 있을 때만)
      │    │    │    │   +0x22d / +0x244  0 인 쪽만 헤더 값으로
      │    │    │    ├─ (2) +0x109  ZwSetInformationProcess(0x29)   예약 M  [추측: 예약]
      │    │    │    ├─ (3) +0x175  ZwAllocateVirtualMemory     커밋 C, PAGE_READWRITE
      │    │    │    ├─ (4) +0x1cb  ZwAllocateVirtualMemory     가드 3P, 0x104 (M − C ≥ 3P)
      │    │    │    └─ (실패) +0x292  ZwFreeVirtualMemory       MEM_RELEASE
      │    │    ├─ (3) +0xb1  ExGenRandom                       +0xdc CONTEXT.Rsp
      │    │    └─ (4) +0xe8  KiUnstackDetachProcess
      │    └─ (2) +0x631  MmCreateTeb
      └─ (2) +0x2ea  PspInsertThread
           ├─ (1) +0x93   KeAbPreAcquire                       (프로세스 락 직접)
           ├─ (2) +0x2e5  KeSuspendThread                      (CreateFlags 0x1 일 때만)
           └─ (3) +0x5a6  KeReadyThread                        (늘)
```

## 사진과 맞춰 본 것 (run44 첫 스레드, 새 실행 없음)

- 둘째 스레드는 `main` 뒤라 run44 사진 범위 밖이다. 아래는 **같은 함수를 쓰는 첫 스레드** 값이다.
- `[관찰]` K2: TEB `StackBase` `0x9b2fd00000` · `StackLimit` `0x9b2fcff000` (`ms/logs/04_baggage-read_K2.txt:3028`), `DeallocationStack` `0x9b2fc00000` (`:1856`), PEB `MinimumStackCommit` 0 (`:1635`).
  - → `M = StackBase − AllocationBase = 0x100000`, `C = StackBase − StackLimit = 0x1000`. test.exe 헤더 (`0x100000`·`0x1000`) 와 같고, 예약은 최소 `0x40000` 보다 커서 그대로. `X = 0` 이라 ⑤ 안 탐. 위 식과 **맞는다**.
- `[관찰]` K2 스택 VAD `9b2fc00 ~ 9b2fcff` Commit **4** 쪽 (`:1372`, K3·P0·P1 도 4) = 커밋 1쪽 + 가드 3쪽 (`M − C = 0xff000 ≥ 0x3000` 이라 가드를 놓는 길). 위 식과 **맞는다**.
  - 그 뒤 P1a 5쪽 · P1b·P2 7쪽 (`_P1a.txt:1534`, `_P1b.txt:2221`, `_P2.txt:2233`), `StackLimit` `0x9b2fcfe000` → `0x9b2fcfc000` (`_P1a.txt:3426`, `_P1b.txt:4544`, `_P2.txt:4556`) — 커밋 2·4쪽 + 가드 3쪽으로 숫자가 맞는다. 스택을 쓰며 가드가 아래로 내려간 것으로 보인다 `[추측: 가드를 옮기는 Mm 쪽은 안 봄]`.
  - VAD 줄의 보호 칸 `READWRITE` 은 쪽마다 값이 아니다 `[추측]` — 가드 3쪽이 정말 `PAGE_GUARD` 인지는 동적.
- `[관찰]` K2 트랩 프레임 `Rsp` `0x9b2fcfff48` (`:1108`) = `StackBase − 0xb8` = `StackBase − 0x90 − 0x28` → 랜덤 몫 `0x90` = 9 × 16 (범위 0 ~ `0x7f0` 안). `PspSetupUserStack +0xdc` 식과 **어긋나지 않는다**.

## 확인된 것

- `[정적]` 둘째 스레드 길: `NtCreateThreadEx +0x2cd` (ret `+0x2d2`) `PspCreateThread` → `+0x27e` (ret `+0x283`) `PspAllocateThread` → `+0x2e5` (ret `+0x2ea`) `PspInsertThread`, 정상 길에서 각 한 번. run11 의 `[추측]` "이 스레드들과 `test!Worker` 는 `PspCreateThread` → `PspAllocateThread` 길" 이 **커널 쪽은** 코드로 맞다 (유저 쪽 `CreateThread` → `NtCreateThreadEx` 는 그대로 `[추측]`).
- `[정적]` 이 길에서 `PspCreateUserContext` 는 안 불린다. 문맥은 `NtCreateThreadEx +0x1d3 ~ +0x26b` 가 같은 값 세트로 직접 만든다. run16 의 `[추측]` "다른 스레드도 `Rip = RtlUserThreadStart`" 가 이 길에선 `[정적]` (`+0x1bf` → `+0x220`).
- `[정적]` `CreateFlags` 허용 마스크 `0x7f` (`+0x70`), 비트 대응표 위. `SameTebFlags` `0x400` (InitialThread) 은 첫 스레드 길에만. `CreateFlags = 0` 이면 커널이 새 TEB `SameTebFlags` 를 안 쓴다 (`PspAllocateThread +0x66f/+0x673`).
- `[정적]` 속성 목록 검사는 스레드를 만들기 전 (`+0xde`), 모르는 속성은 거절. 스레드 길에서 받는 속성 다섯 (위 표).
- `[정적]` 유저 스택은 둘 다 커널 `RtlCreateUserStack` 이 잡는다. 크기 0 → exe 헤더는 `+0x22d`·`+0x244` (0 인 쪽만). 예약 `+0x104` · 커밋 `+0x170` · 가드 3쪽 `+0x1c6`. `StackBase = A + M` (`+0x130`), `StackLimit = A + M − C` (`+0x187`), `AllocationBase = A` (`+0x128`).
- `[정적]` 첫 스레드 예약 최소 `0x40000` 은 `NtCreateUserProcess +0xd99 ~ +0xda5` 에만 있다.
- `[관찰]` + `[정적]` 첫 스레드 스택 값 (run44 K2) 이 위 식과 test.exe 헤더 값으로 그대로 나온다 (위 절).

## 정정

- run54 문서 (`MmCreateTeb` 13줄 표 바로 아래 "`INITIAL_TEB` 는 `RtlCreateUserStack` 이 채운다 (… `+0x128`)") 와 짐 지도 3.3 `NtTib` ① 줄의 "원본 `… RtlCreateUserStack (호출 +0x89) +0x128`": `+0x128` 은 **`AllocationBase` 한 칸**만 쓴다. `StackBase` 는 `+0x130`, `StackLimit` 는 `+0x187` (sast/70 결과서 6번, flow-writer `.asm` 확인). run54 맨 위에 정정 한 줄을 넣었고, 짐 지도 `NtTib` 줄을 고쳤다 (`DeallocationStack` 줄의 `+0x128` 은 맞음).

## 결과서와 다르게 적은 곳 · 보탠 것

- ① sast/70 "첫 스레드 길과 비교" 끝의 `[추측]` "이미지 정보의 `MaximumStackSize`·`CommittedStackSize` 가 헤더 `SizeOfStackReserve`·`SizeOfStackCommit` 와 같다는 건 이번에 안 확인" → **이미 `[정적]`**: `MiBuildImageControlArea +0x175/+0x179` (예약) · `+0x17d/+0x181` (커밋) 이 이미지 정보 `+0x10`·`+0x18` 에 쓴다 (run31, 짐 지도 3.23, flow-writer `.asm` 확인). run44 K2 값 (`M 0x100000`·`C 0x1000`) 도 맞는다. 그래서 "첫 스레드와 같은 값 (test.exe)" 은 `[정적]` + 첫 스레드 쪽 `[관찰]`.
- ② flow-writer 보탬: run44 사진 대조 (위 "사진과 맞춰 본 것") — 결과서엔 없음.
- ③ `NtCreateThreadEx +0x1f3` 의 callee 는 `_calls.tsv` 에 `alias:` 로 나온다. 하는 일 (스택 탐침) 로 적고 후보 이름 둘 (`__chkstk`·`_alloca_probe`, `pdb_aliases.py --where`) 을 붙였다.
- ④ `PspInsertThread +0x490` (`+0x5a0 |= 2`) 는 run12 가 "스레드 락 풀기" 단계로 적은 같은 줄이다. 결과서는 이 비트를 `ThreadInserted` 라고 부른다 — 서로 어긋나지 않는다.

## 아직 확인 필요

동적 — **Q42** 에 출처로 붙인다 (오케스트레이터). 대상은 test.exe 의 둘째 스레드 하나. `NtCreateThreadEx` 는 다른 스레드에도 걸리니 거르기: 프로세스 = test.exe **그리고** `r13d == 0` (CreateFlags) **그리고** `poi(@rbp+0x320)` = `test!Worker` `[추측: 거르기]`.

1. 새 TEB `SameTebFlags` 실제 값 (기대 `0` `[추측]`): `nt!PspAllocateThread+0x661` (MmCreateTeb 뒤, `rbx` = TEB) 에서 `dx ((nt!_TEB*)@rbx)->SameTebFlags`, `+0x66f` 에서 `r12w` (0 이면 `+0x793` 안 탐), `+0x6f4` 에서 다시.
2. 첫 CONTEXT 실제 값: `nt!NtCreateThreadEx+0x2cd` 에서 `rbx` = CONTEXT (`ContextFlags`·`SegCs`·`Rip`·`Rcx`·`Rdx`, `Rsp` 는 아직 0), `dq @r14+8 L3` (StackInfo). `nt!PspSetupUserStack+0xe3` 에서 `dq @r13+0x98 L1` (`Rsp`).
3. `RtlCreateUserStack` 실제 크기: `nt!PspSetupUserStack+0x89` 에서 `rcx`·`rdx`·`r8`·`r9` (유저가 (0, 0) 을 넘겼나 — `KERNELBASE!CreateRemoteThreadEx` 는 안 읽음 → run90 `[정적]`: 읽었고 (0, 0) 을 넘긴다 (`+0x280 ~ +0x291`). 여기선 값 대조만), `nt!RtlCreateUserStack+0x1eb` 가 서나 (헤더 갈래), `+0x207` 에서 `dq @rax+0x60 L2` · `dw @rax+0x18 L1`, `+0x81` 에서 `rax`·`rbx`.
4. `PEB+0x318`: `+0x104` 에서 `dq @r13+0x318 L1` (둘째 스레드 때도 0 인가 — run44 사진은 P2 까지 0).
5. `ZwSetInformationProcess(0x29)`: `+0x104` 에서 `dq @rsp+0x50 L5` (노드·`M`·ZeroBits), `+0x109` 에서 `eax`·`dq @rsp+0x70 L1` (`A`, 64KB 정렬인가) + `!address`. 가드 3쪽 보호값 (`+0x1c6` 뒤 `!address` / `!vad`). `+0x1d7` 에서 `dq poi(@rsp+0xe8) L5` (InitialTeb). 노드 값 (`r9 >> 0x38`) 이 0 인가.
6. (선택) `AttributeList` 가 NULL 인가·항목 수 (`nt!NtCreateThreadEx+0x70` 에서 `rdi`, `poi(@rdi)`), `SecureProcess` (`+0x170` 비트 0) 가 0 인가, `nt!PspInsertThread+0x8e` 가 서나 (첫 스레드 길이 아님), `+0x2e0` 이 안 서나, `+0x5a1` 시점. 기능 게이트 `Feature_CreateProcessSegmentHeapEnablement` 값.

- 멈춤점 메모 (결과서): `nt!RtlCreateUserStack` 은 `PAGE` 구역이라 안 올라와 있으면 `bp` 가 미해결일 수 있다 — 첫 스레드가 이미 거쳐서 올라와 있을 것 `[추측]`.

정적 — 다음 바퀴 후보 (대기열 번호는 오케스트레이터가):

- 유저 쪽 `KERNELBASE!CreateRemoteThreadEx` 가 `NtCreateThreadEx` 에 주는 `CreateFlags`·9·10번째 인자 (`dwStackSize`·`STACK_SIZE_PARAM_IS_A_RESERVATION`)·`AttributeList` — 위 동적 3 으로도 닫힘. (→ run90 `[정적]`: `kernel32!CreateThread +0x36` → `CreateRemoteThreadEx +0x2e2 NtCreateThreadEx` 가 스택 **(0, 0)** (`+0x280 ~ +0x291`) · ZeroBits 0 · 속성 **2개** (`0x10003` · `0x10004`, `TotalLength 0x48`) 를 넘긴다. CreateFlags 는 조건 셋이 다 거짓이면 0, 아니면 1 (`+0x22e ~ +0x274`) — 조건 (다) 만 동적 Q42.)
- `ZwSetInformationProcess` 클래스 `0x29` 갈래 (Mm 쪽) 가 정말 예약하나 — 동적 5 가 이상할 때만. (→ run90 `[정적]`: **예약만** — `NtSetInformationProcess +0x485` → `+0x50e MmAllocateUserStack` (`+0xcb` 할당 종류 `0x2000` 하나) → `+0x12f MiAllocateVirtualMemory` → `+0x334 MiReserveUserMemory`. 시작 64KB 정렬. 클래스 이름은 PDB 에 없어 `[추측: 이름]` 그대로.)
- `PspBuildCreateProcessContext` 끝 `ctx+0x10` 비트 0 을 누가 읽나 (`+0x1876 ~ +0x1882`). (→ run90: 못 찾음 — 네 함수 + 이웃 셋 안엔 읽는 줄 없음, 훑기 한계. `[공백]` 그대로 → 넓게 훑기는 Q168 ②.)
- `PspAllocateThread +0x8c7` 노드 갈래 (`KeGetProcessorNodeNumber` 뒤) 조건 — 노드 값이 0 이 아닐 때. (→ run90 `[정적]`: 이상적 프로세서 속성 (`0x3000e`) 이 있을 때만 (`+0x18b` · `+0x1b9 JNZ → +0x886`), 값 = **노드 + 1** (0 = 선호 없음). test.exe 는 그 속성이 없어 0.)
- 스레드 넘기기·기다림·끝내기·남의 스레드 문맥 → Q141b ~ e (이미 대기열).

## 디버거 메모 / 도구 메모

- 정적만이라 디버거 메모 없음.
- 도구 (sast/69 결과서 "스크립트 문제"): 질문 4(d) 는 "어느 쓰기가 예외로 보호되나" 가 필요했는데 스크립트가 `.pdata` 의 try 범위를 안 보여 줘서 임시 스크립트 + `objdump` 로 읽었다. 결과서 제안 — `.asm` 머리에 "try 범위 → 착지점" 줄 붙이기 — 은 하네스라 사용자 검토 몫 (오케스트레이터에게 그대로 전달).

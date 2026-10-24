# 2026-10-24 run41 — TLS 락 세부: TEB `+0x1850` = **`SchedulerSharedDataSlot`**, 칸마다 락 주소를 적는 **AutoBoost 용 8칸 표**. 맨 위 비트는 ntdll 이 아니라 **커널 `KiAbProcessPreContextSwitch +0x26e`** 가 켠다. `LdrShutdownProcess` 의 `RDI` = **TEB** (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 정적:
  - ntdll 26100.9457, pdb GUID `C3093720177DA6851519DCFFE8DF53FE1` (flow.md 기준과 같음). **새로 안 뽑음.** 이미 있는 `.asm`·`.c` 만 썼다.
  - ntoskrnl 26100.9457, pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음). 질문 2 의 답이 커널에 있어서 **6개 새로 뽑음** (depth 0).
- 버전 대조 (결과서): `LdrpInitializeProcess +0x1bbb`, `LdrpInitializeNode +0x16b`/`+0x19c`, `LdrpLoadDll +0x100`, 커널 `NtCreateUserProcess +0xc36`, `PspInsertThread +0x807` 가 `_calls.tsv` 와 같다.
- 구조체 칸 이름은 PDB 타입 레코드(`llvm-pdbutil dump -types`)로 읽었다: `_TEB`, `_LDR_DATA_TABLE_ENTRY`, `_PEB_LDR_DATA`, 커널 `_KTHREAD`.
- 결과서는 ntdll.dll 전체를 `objdump` 로 풀어 **꼴만 검색**했다 (레포 밖 임시 파일). 그런 숫자(쓰기 1곳, 같은 꼴 23곳 등)는 "검색한 꼴 기준" 이다.

| 함수 | 모듈 | rva | 이번에 본 것 |
|---|---|---|---|
| `LdrpCallTlsInitializers` | ntdll | `0x10e50` | 칸 적기 `+0x26 ~ +0x63`, 풀기 `+0x1a7 ~ +0x1ed`, 마스크 `+0x204` |
| `RtlpAcquireSRWLockSharedContended` | ntdll | `0x15f0c` | 본문 (TEB `+0x1850` 접근이 있나) |
| `RtlAbPreAcquire` | ntdll | `0x748a0` | 첫 갈림 `+0x3 ~ +0x6`, 칸 적기 `+0x3a` |
| `LdrpAcquireSchedulerSharedDataSlot` | ntdll | `0xf0ea4` | 표 받기 `+0x84`, TEB 에 넣기 `+0x92` |
| `ImageTlsCallbackCaller` | ntdll | `0xe83e0` | 전체 (55바이트) |
| `LdrShutdownProcess` | ntdll | `0xadda0` | `+0x28`, `+0xf9`, `+0x156 ~ +0x16d`, `+0x267`, `+0x2cc` |
| `RtlUserThreadStart` | ntdll | `0xacac0` | `+0x7 ~ +0x35` (같은 꼴 예) |
| `KiAbProcessPreContextSwitch` | ntoskrnl | `0x226c20` | 칸 비트 켜기 `+0x7e ~ +0x294` |
| `KiAbUmPostWait`, `KiAbProcessPostContextSwitch`, `KeAbUserModeEntryFree` | ntoskrnl | — | 비트 끄기 줄만 |
| `NtSetInformationThread` | ntoskrnl | — | 클래스 0x38·0x39 처리로 가는 호출 자리만 |
| `KeWaitForAlertByThreadId` | ntoskrnl | — | `KiAbUmPostWait` 부르는 자리만 |

## 목적

- Q56 (run28 "아직 확인 필요"): flow.md 2.7 `LdrpCallTlsInitializers` 락 잡기·풀기 둘레의 작은 질문 넷.
  1. TEB `+0x1850` 이 가리키는 8칸 표는 무엇인가 (이름, 누가 넣나, "SRW 락 소유 추적" `[추측]` 이 맞나).
  2. 칸 값의 맨 위 비트(bit 63)를 누가 켜나. 후보는 `RtlpAcquireSRWLockSharedContended` 였다 `[추측]`.
  3. `ImageTlsCallbackCaller` 의 `RtlAcquireSRWLockExclusive` 직접 호출 갈래는 왜 있나. sample 콜백은 그 갈래를 안 타나.
  4. `LdrShutdownProcess +0x168` 앞 `[RDI+0x58] ≠ 0` 조건은 무슨 뜻인가.

## 방법

- 정적: `_workspace/sast/36_tls-lock-detail.md`
  - ntdll: `^RtlpAcquireSRWLockSharedContended$` depth 1, `^LdrpCallTlsInitializers$`·`^ImageTlsCallbackCaller$`·`^LdrShutdownProcess$` depth 0. 전부 이미 있던 파일.
  - ntoskrnl (요청서 밖, 답이 커널에 있어서): `^(KiAbUmPostWait|NtSetInformationThread)$`, `^(KiAbProcessPreContextSwitch|KiAbProcessPostContextSwitch|KeWaitForAlertByThreadId|KeAbUserModeEntryFree)$` depth 0.
    - 찾은 방법: 지도(`_map`)에 없는 길이라, PDB 이름 목록(`KiAb*`, `KeAb*`)과 "`_KTHREAD +0x320` 을 읽는 곳" 검색.
- 결과서: `_workspace/sast/36_tls-lock-detail_result.md` (**분석성공**)
- 이 문서를 쓰며 위 `.asm` 의 인용 줄(`+0x..`)을 다시 맞춰 봤다. 하나만 결과서와 다르다 (아래 "도구 메모").

## 결과

비유 (도서관 대출 카드):

- 스레드마다 **대출 카드 한 장(8칸 표)** 을 받는다. 락(책)을 빌리면 빈 칸에 "이 책 빌림" 이라고 적고, 돌려주면 지운다.
- 사서(커널)는 스레드가 잠깐 자리를 비울 때(CPU 에서 내려갈 때) 카드를 훑는다. 빌린 책이 있으면 "이 사람이 책을 들고 있으니 빨리 돌아오게 도와주자" 고 **사서 장부에 따로 적고, 그 칸에 도장(bit 63)** 을 찍는다.
- 책을 돌려줄 때 그 칸에 도장이 있으면, 스레드는 사서에게 "장부에서도 지워 주세요" 라고 말해야 한다 (`NtSetInformationThread` 클래스 0x38). 도장이 없으면 그냥 지우면 된다.

### 정적 — 호출 지점 (`_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x1827` | `+0x182c` | `LdrpAllocateSchedulerSharedData` | (이번에 안 봄) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1837` | `+0x183c` | **`LdrpAcquireSchedulerSharedDataSlot`** | (이번에 안 봄) | [정적] |
| `ntdll!LdrpInitializeThread` | `+0x50` | `+0x55` | `LdrpAcquireSchedulerSharedDataSlot` | (새 스레드) | [정적] |
| `ntdll!LdrpAcquireSchedulerSharedDataSlot` | `+0x84` | `+0x89` | `NtSetInformationThread(-2, 0x39, 버퍼, 0x18)` | 목록이 비어 있지 않고 `NtGlobalFlag2 & 0x10` 꺼짐 | [정적] |
| `ntdll!LdrpCallTlsInitializers` | `+0x113` | `+0x118` | `RtlpAcquireSRWLockSharedContended(&LdrpTlsLock, 칸, 락 값)` | 빠른 잡기 실패 | [정적] (run28) |
| `ntdll!RtlpAcquireSRWLockSharedContended` | `+0x126` | `+0x12b` | `RtlAbPreAcquire(락, 칸)` | 칸 ≠ 0 (`+0x11b`) | [정적] |
| `ntdll!LdrpCallTlsInitializers` | `+0x1e8` | `+0x1ed` | `NtSetInformationThread(-2, 0x38, {칸 번호, 0}, 0x10)` | 칸 bit 63 켜짐 | [정적] (run28) |
| `nt!NtSetInformationThread` | `+0x630` | `+0x635` | `KeAbUserModeEntryFree(스레드, 칸 번호)` | 클래스 0x38, 크기 0x10, 현재 스레드 | [정적] |
| `nt!NtSetInformationThread` | `+0xc2c` | `+0xc31` | `PspSchedulerSharedDataRegionSlotAllocate` | 클래스 0x39 | [정적] |
| `nt!KeWaitForAlertByThreadId` | `+0x1de` | `+0x1e3` | `KiAbUmPostWait` | (이번에 안 봄) | [정적] |
| `ntdll!LdrShutdownProcess` | `+0x168` | `+0x16d` | `LdrpCallTlsInitializers(0, 모듈 항목)` | `TlsIndex ≠ 0` 이고 `TEB.ThreadLocalStoragePointer ≠ 0` | [정적] |
| `ntdll!LdrShutdownProcess` | `+0x2cc` | `+0x2d1` | `LdrpCallTlsInitializers(0, exe 항목)` | 같은 두 조건 (exe 쪽) | [정적] |
| `ntdll!ImageTlsCallbackCaller` | `+0x16` | `+0x1b` | CFG 간접 호출 (= 진짜 콜백) | 콜백 ≠ `RtlAcquireSRWLockExclusive` | [정적] (run28) |
| `ntdll!ImageTlsCallbackCaller` | `+0x29` | `+0x2e` | `RtlAcquireSRWLockExclusive` | 콜백 == 그 함수 | [정적] (run28) |

- `KiAbProcessPreContextSwitch` 를 부르는 곳 (`.c` 머리): `KiSwapThread`, `KiQuantumEnd`, `KiDispatchInterrupt`, `KiExitDispatcher`, `KiCheckForThreadDispatch`, `KiDirectSwitchThread`, `KiHandleDeferredPreemption`. 곧 **스레드가 CPU 에서 내려가는 때** (대기·시간 다 씀·선점).

### 1. TEB `+0x1850` = `SchedulerSharedDataSlot`

- `[정적: PDB 타입]` `_TEB` 의 `+0x1850` 칸 이름은 **`SchedulerSharedDataSlot`**, 타입 `void*`. 같은 오프셋에 겹친(union) 다른 이름은 없다.
  - 이웃: `+0x1848 ExtendedFeatureDisableMask`, `+0x1858 HeapWalkContext`. `_TEB64` 도 같은 이름 (`_TEB32` 는 `+0x1018`).
  - run28 이 "PDB 이름은 쓰임새와 안 맞아 `[추측]`" 이라 했던 것 → 이름은 **PDB 그대로가 맞다** (아래 정정).
- `[정적]` 이 칸에 **쓰는** 곳은 ntdll 에 **하나뿐**: `LdrpAcquireSchedulerSharedDataSlot +0x92` (`MOV [RDI+0x1850],RAX`, `RDI` = 인자로 받은 TEB). 나머지 91곳은 읽기 (결과서 `objdump` 검색).
  - 그 바로 앞 `+0x84` 에서 커널에 표 한 칸을 달라고 한다: `NtSetInformationThread(-2, 클래스 0x39, 버퍼, 0x18)`. 성공하면 돌려받은 주소를 TEB 에 적는다.
  - 조건 둘: 전역 목록 `LdrpSchedulerSharedDataListHead` 가 비어 있지 않고, PEB `NtGlobalFlag2` (`+0x7c4`) 의 `0x10` 비트가 꺼져 있을 때. 두 조건이 안 맞으면 TEB `+0x1850` 은 **0 으로 남는다**. `LdrpCallTlsInitializers` 는 0 일 때를 따로 처리한다 (`+0x36 TEST R8,R8` / `+0x39 JZ`).
- `[정적]` 부르는 자리: 첫 스레드는 `LdrpInitializeProcess +0x1837` (ret `+0x183c`), 바로 앞이 `+0x1827 LdrpAllocateSchedulerSharedData` (ret `+0x182c`). 새 스레드는 `LdrpInitializeThread +0x50` (ret `+0x55`).
  - 그래서 test.exe 메인 스레드는 **TLS 준비(`+0x18d0`)·모듈 초기화(`+0x1bbb`)·TLS 콜백보다 앞**에 표를 받는다 (오프셋 순서).
  - `.c` 머리의 caller 목록엔 `RtlCloneUserProcess`, `RtlCompleteProcessCloning` 도 있다 (오프셋은 안 봄).
- `[정적]` **"SRW 락 소유 추적 표" `[추측]` 은 방향은 맞고 이름이 좁다.**
  - 칸에는 **락 주소**가 들어간다: `LdrpCallTlsInitializers +0x63 MOV [RDX],R15` (R15 = `&LdrpTlsLock`), `RtlAbPreAcquire +0x3a` (빈 칸을 찾아 락 주소를 적음). 다 쓰면 `LdrpCallTlsInitializers +0x1ed` 에서 칸을 0 으로.
  - 이 표를 읽는 ntdll 함수 56개에 SRW 락 함수(`RtlAcquireSRWLockShared/Exclusive`, `RtlReleaseSRWLock*`, `RtlSleepConditionVariableSRW` 등)뿐 아니라 **임계영역**(`RtlEnterCriticalSection`/`RtlLeaveCriticalSection`/`RtlTryEnterCriticalSection`/`RtlpEnterCriticalSectionContended`), `RtlInitializeResource` 도 있다. SRW 전용이 아니다.
  - 커널 PDB 이름이 같은 표를 본다: `_KTHREAD +0x320 SchedulerSharedSystemSlot` (같은 표를 커널 주소로), `+0x478 UserAbEntries` (칸마다 0x58바이트 커널 항목), `+0x470 KernelAbEntries`. 함수 이름 `KiAb*`/`KeAb*`, 변수 `AutoBoostActive`·`AutoBoostLock`, 기능 이름 `Feature_UserModeAutoBoost`.
  - `Ab` = AutoBoost `[추측: 이름 풀이. PDB 이름들이 AutoBoost 를 같이 쓴다]`. AutoBoost 는 "락을 쥔 스레드의 우선순위를 올려 기다리는 쪽을 빨리 풀어 주는 기능" 이라는 일반 설명이 있다 `[추측: 일반 지식]`.
  - 이름은 이제 **"락 소유 칸 표 (AutoBoost 용, 스레드마다 8칸)"** 로 부른다.

### 2. 칸의 맨 위 비트(bit 63)는 커널이 켠다

- `[정적]` 후보 **`RtlpAcquireSRWLockSharedContended` 는 아니다.**
  - 이 함수 안엔 TEB `+0x1850` 접근이 **0줄**, 칸 값을 바꾸는 줄도 없다. 칸 포인터는 둘째 인자(`RDX` → `R14`)로 받기만 한다.
  - `+0x11b TEST R14,R14` 로 칸이 0 이 아닐 때만 `+0x126 RtlAbPreAcquire(락, 칸)` 을 부른다. 그런데 `RtlAbPreAcquire` 는 둘째 인자가 0 이 아니면 **그걸 그대로 돌려주고 끝난다** (`+0x3 TEST RDX,RDX` / `+0x6 JNZ` → `+0x43 MOV RAX,RDX`). 그래서 칸 값은 안 바뀐다. 왜 이렇게 짰는지는 모른다 (아래 공백).
  - 이 함수가 부르는 다른 것: `RtlpWaitCouldDeadlock`(+0x57), `NtTerminateProcess`(+0x239), `RtlpOptimizeSRWLockList`(+0x246), `RtlBackoff`(+0x168), `NtWaitForAlertByThreadId`(+0x116).
  - `RtlpAcquireSRWLockExclusiveContended` 도 TEB `+0x1850` 접근·`RtlAbPreAcquire` 호출이 없다 (결과서가 호출·`bts` 만 훑음).
  - ntdll 전체에서 칸 맨 위 바이트를 켜는 꼴(`bts …,0x3f`, `or byte [..+7],0x80`)은 **0곳** (검색한 꼴 기준).
- `[정적]` **켜는 곳 = 커널 `nt!KiAbProcessPreContextSwitch +0x26e`** (`OR byte [RSP+0x9f],0x80` — 스택의 칸 값 복사본 `[RSP+0x98]` 의 바이트 `+7` bit 7) → `+0x280 MOV [R14+RCX*8],RAX` 로 칸에 도로 쓴다 (`R14` = `[스레드+0x320]` 표, `RCX` = 칸 번호).
  - 켜는 조건 (칸마다):
    1. 칸 값이 0 이 아님 (`+0x7e TEST RDI,RDI` / `+0x81 JNZ`).
    2. **bit 1 이 꺼져 있음** (`+0x202 TEST DIL,2`). ntdll 이 "푸는 중" 으로 표시한 칸은 건너뛴다.
    3. bit 63 이 아직 꺼져 있음 (`+0x216 SHR RAX,0x38` / `+0x21c JS`). 이미 켜져 있으면 `+0x294` 쪽으로 간다.
    4. 그러면 칸의 락 주소를 커널 항목 `UserAbEntries[칸]` 의 `+0x10` 에 복사하고 (`+0x252`), 칸 bit 0 이 켜져 있으면 항목 `+0x19` = 1 (`+0x25f`, `+0x269`), 그리고 **bit 63 을 켠다** (`+0x26e`).
  - 해석 `[추측]`: bit 63 = "커널이 이 칸에 대해 자기 항목을 만들어 뒀다". **락을 쥔 채 CPU 에서 내려간 스레드만** 켜진다.
- `[정적]` 끄는 곳도 전부 커널: `KeAbUserModeEntryFree +0x92 ~ +0xa1`, `KiAbUmPostWait +0xd2` (`AND byte [RSP+0x57],0x7f`), `KiAbProcessPostContextSwitch +0x329` (`AND CL,0x7f`). ntdll 은 **읽기만** 한다 (`LdrpCallTlsInitializers +0x1ab`).
- `[정적]` 칸 값에서 락 주소만 꺼내는 마스크는 `0x7ffffffffffffffc` 로 커널·ntdll 이 같다: `KiAbUmPostWait +0x79 AND` / `+0x7e CMP RAX,R15`, `LdrpCallTlsInitializers +0x204 MOV R9,0x7ffffffffffffffc`.

#### 풀 때 (`LdrpCallTlsInitializers`) 와 클래스 0x38

- `[정적]` 순서:
  1. `+0x1a7 OR byte [RSI],2` — bit 1 켜기 ("푸는 중").
  2. `+0x1ab CMP byte [RSI+7],R13B(0)` / `+0x1af JGE` — bit 63 이 **꺼져 있으면** 3 을 건너뛴다.
  3. 켜져 있으면 칸 번호 = `(칸 주소 − [TEB+0x1850]) >> 3` (`+0x1c2 SUB`, `+0x1c9 SAR`) → `+0x1e8 NtSetInformationThread(-2, 0x38, {칸 번호, 0}, 0x10)`.
  4. `+0x1ed MOV [RSI],R13(0)` — 칸 지우기.
- `[정적]` 커널 쪽 (이름만): 클래스 0x38 은 입력 크기 0x10, 둘째 dword 0, 핸들이 현재 스레드일 때만 `NtSetInformationThread +0x630 → KeAbUserModeEntryFree(스레드, 칸 번호)`. 이게 커널 항목을 풀고 칸 bit 63 을 끈다.
- **`THREADINFOCLASS` 0x38 의 이름은 PDB 에 없다.** `ntdll.pdb`·`ntoskrnl.exe.pdb` 타입 목록에 `THREADINFOCLASS` enum 자체가 없다 (공개 심볼).
  - 외부 공개 헤더 기억으로는 `0x38` = `ThreadUpdateLockOwnership`, `0x39` = `ThreadSchedulerSharedDataSlot` `[추측]`. 정황: 0x39 의 커널 처리 함수가 `PspSchedulerSharedDataRegionSlotAllocate`, 0x38 의 처리 함수가 `KeAbUserModeEntryFree` 라 이름 뜻이 맞는다.
  - 문서에는 "클래스 0x38 (`[추측]` `ThreadUpdateLockOwnership`)" 으로만 쓴다.

### 3. `ImageTlsCallbackCaller` 의 직접 호출 갈래

- `[정적]` 꼴: `+0x7 LEA R8,[RtlAcquireSRWLockExclusive]` → `+0xe CMP RAX,R8` → `+0x11 JZ` (→ `+0x26`) → `+0x29 CALL RtlAcquireSRWLockExclusive`. 다르면 `+0x16` CFG 간접 호출. 두 갈래 다 셋째 인자 `R8D = 0` (`+0x13`, `+0x26`).
- `[정적]` **같은 꼴이 ntdll 에 23곳 더 있다** (결과서 `objdump` 스캔 — "LEA 함수주소 → 4명령 안 CMP → 바로 JE/JZ → 목적지 8명령 안 같은 주소 직접 CALL". 모양이 조금 다른 건 못 셌으니 **하한**). 예:
  - `RtlpTpWorkCallback +0x208 LEA RtlAcquireSRWLockShared` → `+0x234` 직접 CALL, `+0x217 LEA RtlAcquireSRWLockExclusive` → `+0x22d` 직접 CALL (같은 대상 함수).
  - `LdrQueryProcessModuleInformationEx +0x33f LEA LdrQueryModuleInfoLocalLoaderUnlock` → `+0x3a2` 직접 CALL.
  - `TpSetTimerEx +0x2a6 LEA` → `+0x2eb` 직접 CALL (대상 rva `0x829a0`, `_index` 이름 `TppFreeWait`, 이 주소엔 PDB 이름이 하나뿐 — `pdb_aliases.py --lookup`).
  - **`RtlUserThreadStart +0x13 LEA RCX,[rva 0x128f0]` / `+0x20 CMP RAX,RCX` / `+0x23 JZ` → `+0x30` 직접 CALL** (2.10. 대상 rva `0x128f0` = `RtlLeaveCriticalSection`, PDB 이름 하나뿐).
- `[추측]` 그래서 이건 `ImageTlsCallbackCaller` 만의 뜻 있는 갈래가 아니라, **컴파일러가 "자주 오는 대상은 먼저 비교해서 직접 부르기" 로 바꾼 꼴** (PGO 간접 호출 승격 꼴) 로 보인다. 바이너리만으로 컴파일러 동작을 증명할 순 없다. 근거는 "같은 꼴이 흔하다" 는 것.
- `[소스]` + `[정적+샘플]` **sample TLS 콜백은 이 갈래를 안 탄다.**
  - 콜백: `ExeTls` (`sample/test.c:8`), `Dll1Tls` (`dll1.c:8`), `Dll2Tls` (`dll2.c:5`). 각 이미지의 `.CRT$XLB` 칸 하나로 등록.
  - PE TLS 디렉터리 콜백 배열 (`sample/bin/*`, 결과서가 직접 읽음): test.exe `0x1400012a0`, dll1 `0x1800012a0`, dll2 `0x1800012a0` (모두 rva `0x12a0`). 배열은 모두 콜백 **하나** + 끝 0.
  - 비교 대상 `RtlAcquireSRWLockExclusive` 는 **ntdll 안** (rva `0x393d0`). 콜백은 각자 다른 이미지 안이라 실행 때 주소가 같을 수 없다 → `CMP` 는 늘 "다름" → `+0x16` 쪽.
  - dll1·dll2 선호 `ImageBase` 가 ntdll 선호 값과 같은 `0x180000000` 이지만, 실행 땐 모듈마다 다른 주소에 올라가니 결론은 같다.

### 4. `LdrShutdownProcess` 의 `[RDI+0x58]` = TEB `ThreadLocalStoragePointer`

- `[정적]` `RDI` 는 **TEB** 다: `+0x28 MOV RDI,GS:[0x30]` 뒤로 함수 안에서 안 바뀐다. 그래서 `[RDI+0x58]` = **`_TEB.ThreadLocalStoragePointer`** (PDB offset `0x58`).
  - 모듈 항목은 `RBX` 다: `+0xf9 LEA RBX,[RSI-0x20]` (`RSI` 는 `PebLdr+0x30` `InInitializationOrderModuleList` 를 도는 포인터, `_LDR_DATA_TABLE_ENTRY +0x20 = InInitializationOrderLinks`). 요청서의 "`RDI` = 모듈 항목, `+0x58` = `BaseDllName`" 짐작은 틀렸다 (아래 정정).
- `[정적]` 조건 한 줄: **"이 모듈에 TLS 가 있고, 지금 이 스레드에 TLS 배열 포인터가 있으면"** 그때만 종료 콜백(이유 0)을 부른다.
  - `+0x156 CMP word [RBX+0x6e],R12W(0)` / `+0x15b JZ` — `_LDR_DATA_TABLE_ENTRY +0x6e = TlsIndex`. TLS 디렉터리가 있는 모듈은 `LdrpInitializeTls +0x102`·`LdrpHandleTlsData +0x3a8` 에서 `0xffff` 가 된다 (run28). 그래서 `≠ 0` = "TLS 있는 모듈".
  - `+0x15d CMP qword [RDI+0x58],R12(0)` / `+0x161 JZ` — 위 TEB 칸.
  - 그다음 `+0x163 MOV RDX,RBX` / `+0x166 XOR ECX,ECX` / `+0x168 CALL LdrpCallTlsInitializers` (ret `+0x16d`).
  - exe 몫도 같은 모양: `+0x18f` `[LdrpImageEntry]` → `+0x196 CMP word [RDX+0x6e],R12W` / `+0x19b JNZ` → `+0x267 CMP qword [RDI+0x58],R12` → `+0x2cc CALL LdrpCallTlsInitializers` (ret `+0x2d1`).
- 프로세스 끝 길이라 이번 범위(진입점까지) 밖이다. 이름·뜻만 적는다.

### 표 칸 하나의 모양 (8칸 × 8바이트, 첫 칸 = `[TEB+0x1850]`, 빈 칸 = 0)

| 비트 | 뜻 | 누가 켜나 / 끄나 | 표시 |
|---|---|---|---|
| bit 2 ~ 62 | **락 주소** (`& 0x7ffffffffffffffc`) | 락 잡는 쪽(ntdll)이 적음 (`LdrpCallTlsInitializers +0x63`, `RtlAbPreAcquire +0x3a`). 풀고 나서 칸 전체 0 (`+0x1ed`) | [정적] |
| bit 63 | 커널이 이 칸에 자기 항목을 만들어 둠. 켜져 있으면 풀 때 커널에 알림 | **커널이 켬** `KiAbProcessPreContextSwitch +0x26e`. **커널이 끔** `KeAbUserModeEntryFree`, `KiAbUmPostWait +0xd2`, `KiAbProcessPostContextSwitch +0x329`. ntdll 은 읽기만 (`+0x1ab`) | 위치는 [정적], 뜻은 [추측] |
| bit 1 | "푸는 중". 커널이 이 칸을 건너뜀 | **ntdll 이 켬** `LdrpCallTlsInitializers +0x1a7` | [정적] |
| bit 0 | 뜻 모름. 커널이 읽어서 항목 `+0x19` 로 옮기고 (`PreContextSwitch +0x25f`), 대기가 끝나면 지움 (`KiAbUmPostWait +0x88`) | 켜는 ntdll 줄은 못 찾음 | [공백] |

- 칸 번호 = `(칸 주소 − 표 주소) / 8`.
- `lock bts [reg],0` 줄이 ntdll 에 여럿 있지만 (예: `RtlpAddDebugInfoToCriticalSection +0xf2`) **락 값 자체**의 bit 0 이지 이 칸이 아니다.

### 호출 관계

```
첫 스레드 (test.exe 메인)
LdrpInitializeProcess
 ├─ +0x182c  LdrpAllocateSchedulerSharedData                       (이번에 안 봄)
 ├─ +0x183c  LdrpAcquireSchedulerSharedDataSlot(TEB)
 │            ├─ +0x89  NtSetInformationThread(-2, 0x39, 버퍼, 0x18)  ── 커널 +0xc31 PspSchedulerSharedDataRegionSlotAllocate
 │            └─ +0x92  TEB+0x1850 = 받은 표 주소                       (조건 안 맞으면 0 으로 남음)
 ├─ +0x18d0  LdrpInitializeTls
 └─ … +0x1bbb / +0x1c99 → LdrpCallTlsInitializers (2.7)
        ├─ +0x26 ~ +0x63  표에서 빈 칸 찾아 &LdrpTlsLock 적기 (표가 0 이면 건너뜀)
        ├─ +0x6d CMPXCHG 공유 잡기 → 실패면 +0x118 RtlpAcquireSRWLockSharedContended(락, 칸, 값)
        │                                    └─ +0x12b RtlAbPreAcquire(락, 칸)   칸 ≠ 0 이면 그대로 돌려줌 (칸 안 바꿈)
        └─ 풀기: +0x1a7 bit 1 켬 → +0x1ab bit 63?
                   ├─ 켜짐 → +0x1ed NtSetInformationThread(-2, 0x38, {칸 번호, 0}, 0x10)
                   │           ── 커널 +0x635 KeAbUserModeEntryFree (bit 63 끔)
                   └─ +0x1ed 칸 = 0

커널 (스레드가 CPU 에서 내려갈 때마다)
KiSwapThread / KiQuantumEnd / KiDispatchInterrupt / …
 └─ KiAbProcessPreContextSwitch
      └─ 칸마다: 0 아님 · bit 1 꺼짐 · bit 63 꺼짐 → 항목에 락 주소 복사 (+0x252) → +0x26e bit 63 켬 → +0x280 칸에 씀
```

## 확인된 것

- `[정적: PDB 타입]` TEB `+0x1850` 이름은 `SchedulerSharedDataSlot` (`void*`), 겹친 이름 없음.
- `[정적]` 이 칸을 채우는 곳은 `LdrpAcquireSchedulerSharedDataSlot +0x92` 하나. 첫 스레드는 `LdrpInitializeProcess +0x1837` (ret `+0x183c`), 새 스레드는 `LdrpInitializeThread +0x50` (ret `+0x55`) 에서 부른다. 표 자체는 커널 클래스 0x39 (`PspSchedulerSharedDataRegionSlotAllocate`) 가 준다.
- `[정적]` 표 칸 = 락 주소 + 아래 2비트·맨 위 1비트 플래그. 마스크 `0x7ffffffffffffffc` 는 커널·ntdll 공통.
- `[정적]` 같은 표를 SRW 락·임계영역·리소스 함수가 쓴다. 커널 쪽 이름은 AutoBoost (`KiAb*`, `UserAbEntries`).
- `[정적]` bit 63 은 **커널 `KiAbProcessPreContextSwitch +0x26e`** 가 켠다 (스레드가 CPU 에서 내려갈 때, 칸이 차 있고 bit 1·bit 63 이 꺼져 있으면). ntdll 에는 켜는 줄이 없다.
- `[정적]` bit 1 은 ntdll 이 풀기 직전에 켠다 (`+0x1a7`). 커널은 그 칸을 건너뛴다.
- `[정적]` 클래스 0x38 은 커널에서 `KeAbUserModeEntryFree` 로 간다 (`NtSetInformationThread +0x630`).
- `[정적]` `ImageTlsCallbackCaller` 의 직접 호출 꼴은 ntdll 에 최소 23곳 더 있다. sample 콜백 셋은 다른 이미지 안이라 이 갈래를 안 탄다 (`[소스]` + PE 콜백 배열).
- `[정적]` `LdrShutdownProcess` 의 `RDI` = TEB, `[RDI+0x58]` = `ThreadLocalStoragePointer`. 조건 = "TLS 있는 모듈 && 이 스레드 TLS 배열 있음".

## 정정

- **요청서 sast/36 의 `[추측]` 둘**:
  - 질문 2 "bit 63 을 켜는 곳 = `RtlpAcquireSRWLockSharedContended` 안" → **틀렸다.** 그 함수는 표를 건드리지 않는다. 켜는 곳은 커널 `KiAbProcessPreContextSwitch +0x26e`.
  - 질문 4 "`RDI` = 모듈 항목, `+0x58` = `BaseDllName`" → **틀렸다.** `RDI` = TEB, `+0x58` = `ThreadLocalStoragePointer`. 모듈 항목은 `RBX`.
- **run28** (맨 위에도 한 줄 넣음):
  - "`+0x1e8` … 이 함수 안엔 그 비트를 켜는 줄이 없다 (켜는 쪽은 `+0x113` callee 일 것 `[추측]`)" → 앞 절반은 맞고, **뒷 짐작은 틀렸다** (커널이 켬).
  - "PDB 는 이 칸을 `SchedulerSharedDataSlot` 이라 부르지만 쓰임새(8칸 표)와 안 맞아 이름은 `[추측]`" → **PDB 이름이 맞다.** 칸에는 표 주소가 들어가고, 표는 스케줄러(AutoBoost)와 나눠 쓰는 데이터다. 쓰임새와 어긋나지 않는다.
- run23 의 "SRW 락 소유 추적 `[추측]`" 은 틀린 건 아니고 **좁았다** (임계영역·리소스도 씀). run23 문서는 안 고치고 flow.md 4장 run23 줄에만 반영했다.

## 아직 확인 필요

동적 (새 번호 없이 Q25 쪽 — TLS 길 실행 값, 마지막 하나는 Q30):

- sample 시동에서 TLS 락 칸의 bit 63 이 켜진 채 풀리는 일이 있나. 켜지려면 락을 쥔 동안 CPU 에서 내려가야 해서 거의 없을 것 `[추측]` → 그러면 `+0x1e8` 은 안 불린다.
  - `ntdll!LdrpCallTlsInitializers+0x1ab` 에서 `dq @rsi L1` (칸 값, bit 63), `ntdll!LdrpCallTlsInitializers+0x1e8` 에 안 서는지 (run28 D6 의 `ntdll+0x11038` 과 같은 자리).
- `[TEB+0x1850]` 이 `LdrpCallTlsInitializers +0x2f` 에서 0 이 아닌지, 몇 번 칸에 락 주소가 적히는지.
  - `ntdll!LdrpAcquireSchedulerSharedDataSlot+0x92` 에서 `RAX` (표 주소). 메인 스레드 몫은 `LdrpInitializeProcess+0x183c` 반환. test.exe 조건 필요.
- `ImageTlsCallbackCaller +0x11` 에서 늘 "다름" 인지 — run28 이 이미 적은 `ntdll+0xe8406` 에 안 서는지와 같은 확인 (Q25·Q28).
- 커널 `nt!KiAbProcessPreContextSwitch+0x26e` 는 모든 컨텍스트 스위치에 걸릴 수 있어 **시동 길에선 비추천** (결과서).
- `RtlUserThreadStart +0x13 ~ +0x30`: 칸 `Kernel32ThreadInitThunkFunction` 값을 `RtlLeaveCriticalSection` 주소와 비교해 같으면 직접 부른다. 두 값이 같아질 일은 없어 보인다 `[추측]`. Worker 종료 콜스택의 `RtlUserThreadStart+0x2c` (01_sprint `[관찰]`) 는 간접 갈래 ret 라서 **Worker 에선 이 직접 갈래를 안 탔다**. 메인 스레드는 Q30 에서 (`ntdll+0xacae3` `+0x23 JZ` 를 안 타는지).

정적 (작은 것, 대기열 Q74):

1. 표 칸 **bit 0** 은 누가 켜나 (ntdll 쪽 줄을 못 찾음).
2. `RtlpAcquireSRWLockSharedContended +0x11b ~ +0x126` 의 `RtlAbPreAcquire(락, 칸)` 은 칸이 0 이 아닐 때만 부르는데, 그때 `RtlAbPreAcquire` 는 아무것도 안 한다. 왜 그렇게 짰나 (정적으로는 "효과 없는 호출").
3. `LdrpAcquireSchedulerSharedDataSlot` 의 두 조건 — `LdrpSchedulerSharedDataListHead` 가 언제 차나 (`LdrpAllocateSchedulerSharedData` 가 채우나), PEB `NtGlobalFlag2 & 0x10` 의 뜻. 안 맞으면 표가 0.
4. `THREADINFOCLASS` 0x38/0x39 의 공식 이름 — PDB 에 없음. WinDbg `dt nt!_THREADINFOCLASS` 로 한 번 볼 수는 있지만 공개 심볼이면 안 나올 것.

## 도구 메모

- 결과서 질문 4 의 "`+0x19b CMP word [RDX+0x6e],0`" 은 `.asm` 으로 보면 **`+0x196 CMP word [RDX+0x6e],R12W` / `+0x19b JNZ`** 다 (`+0x18f` 가 `RDX = [LdrpImageEntry]`). 뜻은 같다. 이 문서는 `.asm` 을 따랐다.
- 결과서가 `RtlpAcquireSRWLockSharedContended` 아래에 "부르는 함수는 …" 이라 적은 목록은 **이 함수가 부르는 것**(callee) 이다 (`.asm` CALL 줄). 이 문서는 그렇게 고쳐 적었다.
- `pdb_aliases.py --lookup ntdll.dll` 로 rva `0x128f0`·`0x393d0`·`0x829a0` 를 봤다: 셋 다 이름이 하나뿐 (별칭 무리 아님). 모듈 이름은 `ntdll` 이 아니라 `ntdll.dll` 로 줘야 표를 찾는다.
- 칸 이름·enum 처럼 "PDB 에 있나 없나" 는 `llvm-pdbutil dump -types` 로 봤다 (결과서). 공개 PDB 엔 `THREADINFOCLASS` 같은 enum 이 빠져 있을 수 있다.

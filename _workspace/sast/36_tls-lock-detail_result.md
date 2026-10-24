# 36 tls-lock-detail — 정적 분석 결과

- 요청서: `_workspace/sast/36_tls-lock-detail.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), ntoskrnl (질문 2 때문에 조금)
  - pdb guid: ntdll `C3093720177DA6851519DCFFE8DF53FE1`, 커널 `C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID와 **일치**, 26100.9457 맞음)
- 스크립트 문제: 없음
- 뽑은 함수
  - ntdll: **새로 안 뽑음.** 이미 있는 `.c`/`.asm` 만 썼다 (`RtlpAcquireSRWLockSharedContended`, `LdrpCallTlsInitializers`, `ImageTlsCallbackCaller`, `LdrShutdownProcess`, 곁가지로 `LdrpAcquireSchedulerSharedDataSlot`, `RtlAbPreAcquire`, `RtlUserThreadStart`).
  - ntoskrnl: 6개 **새로 뽑음** (정규식 `^(KiAbUmPostWait|NtSetInformationThread)$` 와 `^(KiAbProcessPreContextSwitch|KiAbProcessPostContextSwitch|KeWaitForAlertByThreadId|KeAbUserModeEntryFree)$`, depth 0) → `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`.
    - 이유: 질문 2 의 "맨 위 비트를 누가 켜나" 의 답이 ntdll 이 아니라 커널 안에 있었다. 지도(`_map`)에 없는 길이라 PDB 이름 목록(`KiAb*`, `KeAb*`)과 `_KTHREAD +0x320` 읽는 곳 검색으로 찾았다.
  - 그 밖에 읽기 전용으로 ntdll.dll 전체를 `objdump` 로 풀어 패턴만 검색 (스크래치 파일, 레포 밖). 파일을 새로 남기지 않았다.
- 버전 확인: 일치
  - `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x16b → LdrpCallTlsInitializers`, `LdrpInitializeNode +0x19c → LdrpCallInitRoutine`, `LdrpLoadDll +0x100 → LdrpLoadDllInternal`, 커널 `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 모두 `_calls.tsv` 와 같음.

## 한눈에 (질문별 한 줄)

| # | 답 |
|---|---|
| 1 | TEB `+0x1850` = **`SchedulerSharedDataSlot`** (void*), 같은 오프셋에 다른 이름 없음. 넣는 곳 = `LdrpAcquireSchedulerSharedDataSlot +0x92` **하나뿐**. "락 소유 추적 표" = **대체로 예**. 단 SRW 락만이 아니라 임계영역·리소스도 쓰고, 커널 쪽 이름은 **AutoBoost** (`KiAb*`). |
| 2 | 비트 켜는 곳은 `RtlpAcquireSRWLockSharedContended` 가 **아니다 (아니오)**. 그 함수는 이 표를 건드리지 않는다. 켜는 곳 = 커널 **`KiAbProcessPreContextSwitch +0x26e`** (스레드가 CPU 에서 내려갈 때). `THREADINFOCLASS` 0x38 은 PDB 에 enum 이 없다. 정황상 `ThreadUpdateLockOwnership` `[추측, 높음]`. |
| 3 | 컴파일러가 만든 "자주 오는 대상은 직접 부르기" 꼴 `[추측]`. 같은 꼴이 ntdll 에 **23곳** 더 있다. test.exe·dll1·dll2 TLS 콜백은 이 갈래를 **안 탄다 (예)**. |
| 4 | `RDI` = **TEB** (모듈 항목 아님). `[RDI+0x58]` = `_TEB.ThreadLocalStoragePointer`. 조건 = "이 스레드에 TLS 배열 포인터가 있을 때만". |

## 질문별 답 [정적]

### 1. TEB `+0x1850` 이 가리키는 8칸 표

**1-a. 칸 이름·타입**
- 답: `_TEB` 의 offset 6224 (= `0x1850`) 칸은 `SchedulerSharedDataSlot`, 타입 `void*`. sast/23 의 이름이 맞다. 같은 오프셋에 겹친(union) 이름은 **없다**. 이웃 칸은 `+0x1848 ExtendedFeatureDisableMask`, `+0x1858 HeapWalkContext`.
- 근거: `ms/sym/ntdll.dll.pdb` 의 `_TEB` 타입 (`llvm-pdbutil dump -types`). 32/64비트용 `_TEB32`/`_TEB64` 에도 같은 이름 (`offset 4120` / `6224`).

**1-b. 이 칸에 표 주소를 넣는 곳**
- 답: ntdll 전체에서 `[reg+0x1850]` 에 **쓰는** 명령은 **딱 하나**: `LdrpAcquireSchedulerSharedDataSlot +0x92` (`MOV [RDI+0x1850],RAX`, RDI = 인자로 받은 TEB). 나머지 91곳은 전부 읽기.
  - 이 함수가 하는 일: `LdrpSchedulerSharedDataListHead` 목록이 비어 있지 않고 PEB `NtGlobalFlag2` (`+0x7c4`) 의 `0x10` 비트가 꺼져 있으면, `+0x84 NtSetInformationThread(-2, 0x39, 버퍼, 0x18)` 을 부르고 (`R9=0x18`, `EDX=R9+0x21=0x39`), 성공하면 돌려받은 포인터를 TEB `+0x1850` 에 적는다. 커널 쪽 이 클래스(0x39) 처리는 `NtSetInformationThread +0xc2c PspSchedulerSharedDataRegionSlotAllocate` (아래 2-c).
  - 이 함수를 부르는 곳 (`_calls.tsv`): `LdrpInitializeProcess +0x1837` (ret `+0x183c`, 바로 앞 `+0x1827 LdrpAllocateSchedulerSharedData`), `LdrpInitializeThread +0x50` (ret `+0x55`). `.c` 머리 caller 목록엔 `RtlCloneUserProcess`, `RtlCompleteProcessCloning` 도 있다 (오프셋은 이번에 안 봄).
  - 그래서 test.exe 메인 스레드는 `LdrpInitializeProcess +0x1837` 에서 표를 받는다. 이 위치는 모듈 초기화(`+0x1bbb`)와 TLS 콜백보다 **앞**이다. 단 위 두 조건이 안 맞으면 `[TEB+0x1850]` 이 0 으로 남을 수 있고, `LdrpCallTlsInitializers` 는 0 인 경우를 처리한다 (`+0x36 TEST R8,R8 / +0x39 JZ`).
- 근거: `ms/disassemble/ntdll/LdrpAcquireSchedulerSharedDataSlot.asm` `+0x84`, `+0x92`. ntdll.dll `objdump` 에서 `0x1850]` 검색 92건 중 쓰기 1건 (`180f0f36` = rva `0xf0f36`, 함수 시작 `0xf0ea4` + `0x92`).

**1-c. "SRW 락 소유 추적 표" `[추측]` 이 맞나**
- 답: **방향은 예, 이름은 좁다.** 표의 칸에는 **락 주소**가 들어가고 (아래 근거), 커널이 이걸 읽어서 락 주인을 올려 주는 데(AutoBoost) 쓴다. SRW 락 전용은 아니다.
- 근거 (정적):
  - 칸에 락 주소를 적는다: `LdrpCallTlsInitializers +0x63 MOV [RDX],R15` (R15 = `&LdrpTlsLock`), `RtlAbPreAcquire +0x3a MOV [RAX],R10` (R10 = 락 주소, 빈 칸을 찾아서 적음). 다 쓰면 `+0x1ed MOV [RSI],R13(=0)` 으로 지운다.
  - TEB `+0x1850` 을 읽는 함수 56개: `RtlAcquireSRWLockShared/Exclusive`, `RtlTryAcquireSRWLockExclusive`, `RtlReleaseSRWLockShared/Exclusive`, `RtlSleepConditionVariableSRW`, **`RtlEnterCriticalSection`/`RtlLeaveCriticalSection`/`RtlTryEnterCriticalSection`/`RtlpEnterCriticalSectionContended`**, `RtlInitializeResource`, `RtlAbPreAcquire`/`RtlAbPostRelease`, 그리고 SRW 락 잡기를 안에 풀어 쓴 `Ldrp*`·`Tpp*`·힙 함수들. 즉 임계영역도 같은 표를 쓴다.
  - 커널 쪽 PDB 이름이 같은 일을 한다: `_KTHREAD +0x320 = SchedulerSharedSystemSlot` (같은 표를 커널 주소로 본 것), `+0x478 = UserAbEntries` (칸마다 0x58바이트 커널 항목), `+0x470 = KernelAbEntries`. 함수 이름 `KiAb*`/`KeAb*`, 변수 이름 `AutoBoostActive`, `AutoBoostLock`, 기능 이름 `Feature_UserModeAutoBoost`. `Ab` = AutoBoost `[추측: 이름 풀이. PDB 이름들이 AutoBoost 를 같이 씀]`.
  - 커널 `KiAbUmPostWait` (`KeWaitForAlertByThreadId +0x1de` 가 부름) 은 `[thread+0x320]` 표를 돌며 **칸 값 & `0x7ffffffffffffffc` == 락 주소** 인 칸을 찾는다 (`+0x79 AND`, `+0x7e CMP RAX,R15`). 그래서 칸 값의 **bit 2~62 = 락 주소**.
- 이름 제안: "SRW 락 소유 추적 표" 대신 **"락 소유 칸 표 (AutoBoost 용, 스레드마다 8칸)"**.

### 2. 칸 값의 맨 위 비트 (바이트 `+7` 의 bit 7, 곧 bit 63)

**2-a. 후보 `RtlpAcquireSRWLockSharedContended` 가 켜나**
- 답: **아니오.**
  - 이 함수엔 TEB `+0x1850` 접근이 **0줄**이고, 칸 값을 바꾸는 줄도 없다. 부르는 함수는 `RtlpWaitCouldDeadlock`(+0x57), `NtTerminateProcess`(+0x239), `RtlpOptimizeSRWLockList`(+0x246), `RtlBackoff`(+0x168), `NtWaitForAlertByThreadId`(+0x116), `RtlAbPreAcquire`(+0x126) 뿐이다.
  - 칸 포인터는 둘째 인자(`RDX` → `R14`)로 받기만 한다. `+0x11b ~ +0x126` 에서 `R14 ≠ 0` 일 때만 `RtlAbPreAcquire(락, 칸)` 을 부르는데, `RtlAbPreAcquire` 는 둘째 인자가 0 이 아니면 **그걸 그대로 돌려주고 끝난다** (`+0x3 TEST RDX / +0x6 JNZ +0x43 → MOV RAX,RDX / RET`). 그래서 칸 값은 안 바뀐다.
  - `+0x113` 은 이 함수 안이 아니라 **`LdrpCallTlsInitializers` 가 이 함수를 부르는 자리** (ret `+0x118`) 다. 그 직전에 `RDX` 가 칸 포인터, `RCX` 가 `&LdrpTlsLock`, `R8` 가 실패한 `CMPXCHG` 가 돌려준 락 값.
  - `RtlpAcquireSRWLockExclusiveContended` (821바이트) 도 TEB `+0x1850` 접근과 `RtlAbPreAcquire` 호출이 없다 (`objdump` 로 호출·`bts` 만 훑음; `bts` 는 `+0x258 lock bts [rcx+0x24],2` 하나뿐, 대기 블록 비트). 같은 비트 켜기 **없음**.
  - ntdll 전체에서 칸 맨 위 바이트를 켜는 꼴(`bts …,0x3f`, `or byte [..+7],0x80`)은 **없다** (`objdump` 로 검색한 꼴 기준 0곳). `movabs 0x8000000000000000` 3곳은 전부 CONTEXT 플래그 계산 (`RtlRaiseException`, `RtlUnwindEx`, `RtlpWalkFrameChain`).
- 근거: `ms/disassemble/ntdll/RtlpAcquireSRWLockSharedContended.asm`, `RtlAbPreAcquire.asm`, `LdrpCallTlsInitializers.asm +0x10d ~ +0x113`.

**2-b. 실제로 켜는 곳 / 언제 켜나**
- 답: **커널** `nt!KiAbProcessPreContextSwitch +0x26e` (`OR byte [RSP+0x9f],0x80`; `[RSP+0x98]` 은 칸 값 복사본이라 바이트 `+7` 의 bit 7) → `+0x280 MOV [R14+RCX*8],RAX` 로 칸에 도로 쓴다 (`R14 = [thread+0x320]` 표, `RCX = 칸 번호`).
- 언제 (코드 조건, 칸마다):
  1. 칸 값이 0 이 아님 (`+0x7e/+0x81`).
  2. **bit 1 이 꺼져 있음** (`+0x202 TEST DIL,2 / JNZ 건너뜀`). 푸는 중인 칸은 건드리지 않는다.
  3. bit 63 이 아직 꺼져 있음 (`+0x216 SHR RAX,0x38 / +0x21c JS`). 이미 켜져 있으면 `+0x294` 로 가서, 커널 항목의 `+0x19` 가 0 이고 칸의 bit 0 이 켜진 경우에만 다시 처리.
  4. 그러면 칸의 락 주소(`& 0x7ffffffffffffffc`)를 커널 항목 `UserAbEntries[칸].+0x10` 에 복사하고 (`+0x252`), 칸의 bit 0 이 켜져 있으면 항목 `+0x19 = 1` (`+0x269`), 그리고 칸에 **bit 63 을 켠다**.
- 이 함수는 `KiSwapThread`, `KiQuantumEnd`, `KiDispatchInterrupt`, `KiExitDispatcher`, `KiCheckForThreadDispatch`, `KiDirectSwitchThread`, `KiHandleDeferredPreemption` 이 부른다 (`.c` 머리). 곧 **스레드가 CPU 에서 내려가는 때**(선점·대기·시간 다 씀).
- 해석 `[추측]`: bit 63 = "커널이 이 칸에 대해 자기 항목을 만들어 뒀다". 락을 쥔 채 CPU 에서 내려간 스레드만 이 비트가 켜진다. 그래서 풀 때 **켜져 있으면 커널에 알려야 한다** (아래 2-c).
- 끄는 곳 (같이 보면 도움이 됨): `KeAbUserModeEntryFree +0x92~+0xa1` (`& 0x7f`), `KiAbUmPostWait +0xd2` (`AND byte [RSP+0x57],0x7f`), `KiAbProcessPostContextSwitch +0x329` (`AND CL,0x7f`).
- 근거: `ms/disassemble/ntoskrnl/KiAbProcessPreContextSwitch.asm` `+0x7e`, `+0x202`, `+0x216`, `+0x252`, `+0x269`, `+0x26e`, `+0x280`. 한 번 더 확인: `LdrpCallTlsInitializers.asm +0x204 MOV R9,0x7ffffffffffffffc` 가 같은 마스크로 락 주소만 비교한다 (bit 63 과 bit 0~1 은 플래그).

**2-c. 풀 때 (`LdrpCallTlsInitializers` 쪽)와 `THREADINFOCLASS` 0x38**
- 풀기 순서 (`.asm`): `+0x1a7 OR byte [RSI],2` (bit 1 켜서 "푸는 중" 표시) → `+0x1ab CMP byte [RSI+7],0` / `+0x1af JGE` (bit 63 이 **꺼져 있으면** 아래 호출 건너뜀) → 켜져 있으면 `+0x1e8 NtSetInformationThread(-2, 0x38, {칸 번호, 0}, 0x10)` → `+0x1ed MOV [RSI],0` (칸 지움). 칸 번호 = `(칸 주소 − [TEB+0x1850]) >> 3` (`+0x1c2`, `+0x1c9`).
- 커널 쪽 (이름만): `NtSetInformationThread` 의 `case 0x38` — 입력 크기가 0x10 이어야 하고, 둘째 dword 가 0, 핸들이 **현재 스레드**일 때만 `NtSetInformationThread +0x630 → KeAbUserModeEntryFree(스레드, 칸 번호)`. 이 함수가 커널 항목(`UserAbEntries[칸]`)을 풀고 칸의 bit 63 을 끈다.
  - `case 0x39` 는 `+0xc2c PspSchedulerSharedDataRegionSlotAllocate` (1-b 의 표 받기) 이고 출력 버퍼에 `ProbeForWrite`.
- **`THREADINFOCLASS` 0x38 의 PDB 이름: PDB 에 없다.** `ntdll.pdb`·`ntoskrnl.exe.pdb` 의 타입 목록에 `THREADINFOCLASS` enum 이 들어 있지 않다 (공개 심볼이라 빠진 것; `llvm-pdbutil dump -types` 와 `strings` 로 `ThreadUpdateLockOwnership`·`ThreadSchedulerSharedDataSlot` 검색 → 변수/함수 이름 외엔 0건).
  - 외부 공개 헤더 기억(phnt, 24H2)으로는 `0x38 (56) = ThreadUpdateLockOwnership`, `0x39 (57) = ThreadSchedulerSharedDataSlot` `[추측, 높음]`. 근거 정황 둘: 0x39 의 커널 처리 함수 이름이 `PspSchedulerSharedDataRegionSlotAllocate` 로 일치, 0x38 의 처리 함수가 `KeAbUserModeEntryFree`(락 주인 항목 풀기)로 이름 뜻이 일치.
  - 이름을 문서에 확정으로 쓰려면 WinDbg `dt nt!_THREADINFOCLASS` 로 한 번 확인 필요 (공개 심볼에 없으면 안 나옴). 문서에는 "클래스 0x38 (`[추측]` ThreadUpdateLockOwnership)" 로 적기를 권한다.

### 3. `ImageTlsCallbackCaller` 의 `RtlAcquireSRWLockExclusive` 직접 호출 갈래

**3-a. 왜 있나 / 다른 곳에도 있나**
- 답: 컴파일러가 "간접 호출 대상이 자주 같으면 먼저 비교해서 직접 부르기" 로 바꾼 꼴로 보인다 `[추측]` (PGO 간접 호출 승격 꼴). 바이너리만으로 컴파일러 동작을 증명할 수는 없다. 근거는 **같은 꼴이 흔하다**는 것이다.
  - `ImageTlsCallbackCaller` 의 꼴: `+0x7 LEA R8,[RtlAcquireSRWLockExclusive]` → `+0xe CMP RAX,R8` → `+0x11 JZ +0x26` → (같으면) `+0x29 CALL RtlAcquireSRWLockExclusive` / (다르면) `+0x16 CALL [__guard_dispatch_icall]`. 두 갈래 모두 `R8D = 0` 을 셋째 인자로 준다 (`+0x13`, `+0x26`).
  - ntdll.dll 전체 `objdump` 를 훑어 "LEA 함수주소 → CMP → JE → 가서 그 주소로 직접 CALL" 꼴을 세니 **23곳**. (훑은 조건: LEA 뒤 4개 명령 안에 CMP, 바로 다음이 JE/JZ, 목적지 8개 명령 안에 같은 주소로 직접 CALL. 모양이 조금 다른 것은 못 셌으니 하한이다.) 예 (함수 +call 위치, 모두 `_index.tsv` 함수 시작 기준):
    - `RtlpTpWorkCallback +0x208 LEA RtlAcquireSRWLockShared` → `+0x234` 직접 CALL, 그리고 `+0x217 LEA RtlAcquireSRWLockExclusive` → `+0x22d` 직접 CALL. (이 둘은 `ImageTlsCallbackCaller` 와 **같은 대상 함수**.)
    - `LdrQueryProcessModuleInformationEx +0x33f LEA LdrQueryModuleInfoLocalLoaderUnlock` → `+0x3a2` 직접 CALL.
    - `TpSetTimerEx +0x2a6 LEA` → `+0x2eb` 직접 CALL (대상 `0x1800829a0`, 이름은 `_index` 에서 `TppFreeWait` 로 잡힘. ICF 별칭 여부는 확인 안 함).
    - 흐름도에 중요한 하나: **`RtlUserThreadStart +0x13 LEA RCX,[0x1800128f0]`, `+0x20 CMP RAX,RCX`, `+0x23 JZ`, `+0x30` 직접 CALL** — `Kernel32ThreadInitThunkFunction` 값이 `0x1800128f0` (`_index` 이름 `RtlLeaveCriticalSection`) 이면 그리로 직접 부른다. 아래 "새 공백" 참고.
  - 즉 `ImageTlsCallbackCaller` 에만 있는 꼴이 아니다.
- 근거: `ms/disassemble/ntdll/ImageTlsCallbackCaller.asm`, `ms/disassemble/ntdll/RtlUserThreadStart.asm`, ntdll.dll `objdump` 스캔.

**3-b. sample 의 TLS 콜백이 이 갈래를 타나**
- 답: **안 탄다 (예, "안 탄다" 가 맞다).**
- 근거 `[소스]` + 정적:
  - 콜백은 `ExeTls`(`sample/test.c:8`), `Dll1Tls`(`dll1.c:8`), `Dll2Tls`(`dll2.c:5`). 모두 자기 이미지의 `.CRT$XLB` 칸(함수 포인터 하나)로 등록.
  - PE 의 TLS 디렉터리 콜백 배열을 직접 읽었다 (`sample/bin/*`, `ms/bin/test.exe` 와 `sample/bin/test.exe` 는 `cmp` 로 같음): `test.exe` = `0x1400012a0` (rva `0x12a0`), `dll1.dll` = `0x1800012a0` (rva `0x12a0`), `dll2.dll` = `0x1800012a0` (rva `0x12a0`). 각 배열은 끝(0)까지 **한 개씩**.
  - 비교 대상 `RtlAcquireSRWLockExclusive` 는 **ntdll 이미지 안** (rva `0x393d0`). 콜백은 각각 test.exe/dll1/dll2 이미지 안 (rva `0x12a0`). 서로 다른 모듈이라 로드 후 주소 범위가 겹칠 수 없다. 따라서 `CMP RAX,R8` 은 항상 "다름" → `+0x16` 간접(CFG) 갈래.
  - 주의: dll1·dll2 의 선호 ImageBase 가 ntdll 의 선호 ImageBase 와 같은 `0x180000000` 이지만, 실행 때는 모듈마다 다른 주소에 올라간다. 이 결론엔 영향 없음.

### 4. `LdrShutdownProcess +0x168` 앞의 `[RDI+0x58] ≠ 0` 조건

- 답: **`RDI` 는 모듈 항목이 아니라 TEB** (`+0x28 MOV RDI,GS:[0x30]`, 함수 안에서 다시 안 바뀜). 그래서 `+0x58` 은 `BaseDllName` 이 아니라 **`_TEB.ThreadLocalStoragePointer`** (PDB offset 88 = `0x58`).
  - (모듈 항목은 `RBX` 다: `+0xf9 LEA RBX,[RSI-0x20]`, `RSI` 는 `PebLdr+0x30` 의 `InInitializationOrderModuleList` 를 도는 포인터. `_LDR_DATA_TABLE_ENTRY +0x20 = InInitializationOrderLinks`. `+0x58 = BaseDllName` 은 맞지만 이 줄의 레지스터가 아니다.)
- 조건 한 줄: **"이 모듈에 TLS 가 있고 (`[RBX+0x6e]` ≠ 0), 지금 이 스레드의 TLS 배열 포인터도 있으면 (`[RDI+0x58]` ≠ 0)" 그때만 `LdrpCallTlsInitializers(0, 모듈 항목)` 으로 종료 콜백(이유 0 = `DLL_PROCESS_DETACH`)을 부른다.**
  - `+0x156 CMP word [RBX+0x6e],R12W(0) / +0x15b JZ` : `_LDR_DATA_TABLE_ENTRY +0x6e = TlsIndex` (PDB offset 110). 이 값은 처음 0 이고, TLS 디렉터리가 있는 모듈은 `LdrpInitializeTls +0x102` 와 `LdrpHandleTlsData +0x3a8` 에서 **`0xffff`** 가 된다 (두 곳 다 `RtlImageDirectoryEntryToData(..,9)` 로 TLS 디렉터리를 찾은 뒤). 그래서 `≠ 0` = "TLS 있는 모듈" `[정적, 두 쓰기 줄 근거]`.
  - `+0x15d CMP qword [RDI+0x58],R12(0) / +0x161 JZ` : 위 TEB 칸.
  - 그다음 `+0x163 MOV RDX,RBX / +0x166 XOR ECX,ECX / +0x168 CALL LdrpCallTlsInitializers` (ret `+0x16d`).
  - 실행 파일 자신 몫은 `+0x267 CMP qword [RDI+0x58],0` (앞에 `+0x19b CMP word [RDX+0x6e],0`) 을 거쳐 `+0x2cc CALL LdrpCallTlsInitializers` (ret `+0x2d1`) 로 같은 모양.
- 프로세스 끝 길이라 sample 시동(진입점까지) 범위 밖. 이름·뜻만 적음.
- 근거: `ms/disassemble/ntdll/LdrShutdownProcess.asm` `+0x28`, `+0xf9`, `+0x156~+0x168`, `+0x267`, `+0x2cc`. PDB `_TEB`, `_LDR_DATA_TABLE_ENTRY`, `_PEB_LDR_DATA`.

## `_TEB +0x1850` 표 칸 하나의 모양

표 = 8칸 × 8바이트. 첫 칸 주소 = `[TEB+0x1850]`. 빈 칸 = 0. 칸 번호 = `(칸 주소 − 표 주소) / 8`.

| 비트 | 뜻 | 누가 켜나 / 끄나 | 근거 |
|---|---|---|---|
| bit 2 ~ 62 | **락 주소** (`& 0x7ffffffffffffffc`) | 켜기: 락 잡는 쪽이 칸에 락 주소를 적음 (`LdrpCallTlsInitializers +0x63`, `RtlAbPreAcquire +0x3a`). 끄기: 풀고 나서 칸 전체를 0 으로 (`+0x1ed`) | 커널과 ntdll 둘 다 이 마스크로 비교 (`KiAbUmPostWait +0x79`, `LdrpCallTlsInitializers +0x204`) |
| bit 63 (바이트 +7 의 bit 7) | 커널이 이 칸에 대한 자기 항목을 만들어 뒀다 `[추측: 해석]`. 켜져 있으면 풀 때 커널에 알려야 함 | **커널이 켬**: `KiAbProcessPreContextSwitch +0x26e`. **커널이 끔**: `KeAbUserModeEntryFree`, `KiAbUmPostWait +0xd2`, `KiAbProcessPostContextSwitch +0x329`. ntdll 은 **읽기만** (`LdrpCallTlsInitializers +0x1ab`) | 2-b |
| bit 1 | "푸는 중" 표시. 커널은 이 칸을 건너뜀 | **ntdll 이 켬**: `LdrpCallTlsInitializers +0x1a7` | `KiAbProcessPreContextSwitch +0x202` |
| bit 0 | 뜻 확정 못 함 `[공백]`. 커널이 칸 복사본에서 읽고 (`PreContextSwitch +0x25f`, 항목 `+0x19` 로 옮김) 대기가 끝나면 지움 (`KiAbUmPostWait +0x88`) | 켜는 쪽 ntdll 줄은 이번에 못 찾음 | — |

(`lock bts [reg],0` 줄이 ntdll 에 여럿 있지만 `RtlpAddDebugInfoToCriticalSection +0xf2` 등에서 **락 값 자체**의 bit 0 이지 이 칸이 아니다.)

## 동적분석으로 확인해야 할 것

정적으로 말할 수 없는 것만.

- sample 의 TLS 락 잡기에서 **bit 63 이 켜진 채로 풀리는 일이 실제로 있나.** 켜지려면 락을 쥔 동안 컨텍스트 스위치가 일어나야 해서 시동 길에서는 거의 안 일어날 것 같다 `[추측]`. 그러면 `+0x1e8 NtSetInformationThread(0x38)` 은 안 불린다.
- `ImageTlsCallbackCaller +0x11` 에서 `ZF` 가 항상 0 (직접 호출 갈래 안 탐) 인지.
- `[TEB+0x1850]` 이 `LdrpCallTlsInitializers` 에 들어올 때 0 이 아닌지 (`+0x2f` 에서 읽는 값), 8칸 중 몇 번 칸에 락 주소가 적히는지.
- 추천 멈춤점 (필요하면. 이번 항목은 작아서 없어도 됨):
  - `ntdll!LdrpCallTlsInitializers+0x1ab` : `RSI` = 칸 주소. 풀기 직전 칸 값 (`dq @rsi L1`) 과 bit 63 확인. `test.exe` 조건 필요.
  - `ntdll!LdrpCallTlsInitializers+0x1e8` : 도달하면 bit 63 이 켜졌다는 뜻 (안 걸리면 예상대로).
  - `ntdll!LdrpAcquireSchedulerSharedDataSlot+0x92` : 표 주소가 TEB 에 들어가는 순간 (`RAX` = 표 주소). 메인 스레드 몫은 `LdrpInitializeProcess+0x183c` 에서 반환.
  - `nt!KiAbProcessPreContextSwitch+0x26e` : bit 63 이 켜지는 순간. 모든 컨텍스트 스위치마다 걸릴 수 있어 쓸 때 조건 필수. 시동 길에선 비추천.
  - `nt!KeAbUserModeEntryFree` : 클래스 0x38 처리 입구.

## 새로 생긴 정적 공백

1. **`RtlUserThreadStart +0x13 ~ +0x30`** 에 같은 "자주 오는 대상 직접 부르기" 꼴이 있다. 직접 부르는 대상이 `0x1800128f0` (`_index` 이름 `RtlLeaveCriticalSection`) 인데, 이 자리가 비교하는 값은 `Kernel32ThreadInitThunkFunction` (kernel32 가 넣어 주는 시작 함수 포인터) 이다. 이 두 주소가 같아질 일이 없어 보인다 `[추측]`. 새 스레드 시작(다음 단계) 때 `+0x23 JZ` 가 안 탄다는 것을 확인하면 된다. `LdrpInitializeKernel32Functions +0xe4` 에서 값이 들어가는 줄은 `_refs/Kernel32ThreadInitThunkFunction.tsv` 에 이미 있음.
2. 위 표의 **bit 0** 은 누가 켜는지 모른다.
3. `RtlpAcquireSRWLockSharedContended +0x11b~+0x126` 의 `RtlAbPreAcquire(락, 칸)` 호출은 칸이 0 이 아닐 때만 하는데 그때 `RtlAbPreAcquire` 는 아무것도 안 한다 (그대로 돌려줌). 왜 그렇게 짰는지는 모른다 (정적으로는 "효과 없는 호출"). 칸이 0 이면 아예 안 부른다.
4. `THREADINFOCLASS` 0x38/0x39 의 공식 이름은 PDB 로 확정 못 함 (위 2-c).
5. `LdrpAcquireSchedulerSharedDataSlot` 의 `LdrpSchedulerSharedDataListHead` 가 비어 있지 않은 때와 PEB `NtGlobalFlag2 & 0x10` 의 뜻은 이번에 안 봤다. 이 둘이 안 맞으면 TEB `+0x1850` 이 0 으로 남는다.

## 판정

**분석성공.** 네 질문 모두 답이 있고 근거 줄이 있다. 질문 2 의 후보(`RtlpAcquireSRWLockSharedContended`)는 **틀렸고** 실제 켜는 곳은 커널 `KiAbProcessPreContextSwitch +0x26e` 다. 질문 4 의 `RDI` 도 요청서의 추정(모듈 항목)과 달리 **TEB** 였다. 두 가지 모두 요청서의 `[추측]` 을 정정한다. `THREADINFOCLASS` 0x38 이름만 PDB 로 확정 못 해서 `[추측]` 표시로 남겼다.

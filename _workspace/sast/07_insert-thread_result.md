# 07 insert-thread — 정적 분석 결과

- 요청서: `_workspace/sast/07_insert-thread.md` (Q4: `PspInsertThread`, `PspCallProcessNotifyRoutines`)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 안 함.** `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 다 있어서 export 만 했다.
- 뽑은 함수: 2개 (정규식 `^(PspInsertThread|PspCallProcessNotifyRoutines)$`, depth 0)
  - `ms/decomp/ntoskrnl/PspInsertThread.c`, `PspCallProcessNotifyRoutines.c`
  - `ms/disassemble/ntoskrnl/PspInsertThread.asm`, `PspCallProcessNotifyRoutines.asm`, `_calls.tsv` (덧붙여 쌓임)
  - export 로그: `ms/ghidra/ntoskrnl.export.sast07.stdout` (3초, SCRIPT ERROR 0건)
- 지도 먼저: `_map/NtCreateUserProcess_{nodes,edges}.tsv` 에서 두 함수의 깊이·caller 수·`PspInsertThread` 가 부르는 줄 59개를 먼저 봤고, 요청서의 지도 요약과 같았다.
- 버전 확인 (알려진 사실 대조, 일치):
  - `NtCreateUserProcess +0xc36 → PspAllocateProcess` 일치
  - `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 일치 (이번 `.asm` 의 `+0x802` call, `ret=+0x807`)
  - 덤: `NtCreateUserProcess +0x1015 → PspInsertThread` (ret `+0x101a`) 도 run3 `[관찰]` 과 같다.

## 스크립트 문제

막힌 것은 없다. 참고 두 가지 (고치지 않음, 제안만).

1. **`.asm`·`_calls.tsv` 에 안 들어오는 코드 구멍이 있다.** `PspInsertThread` 의 `+0x2e7 ~ +0x318` (49바이트) 이 `.asm` 에 없다.
   - 증상: 줄 번호가 `+0x2e5` 에서 `+0x319` 로 건너뛴다.
   - 원인 추정: 이 구멍은 점프로 안 닿고 (아마 예외 처리 뒤 이어 가는 자리: 레지스터를 스택에서 다시 읽고 `+0x319` 로 흘러 들어감 `[추측]`) Ghidra 가 함수 범위에 안 넣었다.
   - 이번 영향: 없음. 원본 바이트를 `objdump` 로 직접 풀어 보니 이 구멍엔 `call`·`jmp`·`jcc` 가 없다. 함수 범위 전체(`+0x0 ~ +0xa80`)에서 `.asm` 에 없는 `call`·분기 명령은 0개. `PspCallProcessNotifyRoutines` 도 같은 방법으로 확인, 빠진 호출 0개.
   - 제안: 다른 함수에서도 이런 구멍 때문에 호출이 가려질 수 있으니, 필요해지면 예외 표(`.pdata`)로 닿는 자리를 따라가는 기능을 사용자 검토 뒤에 검토. (지금 바퀴엔 불필요)
2. `_calls.tsv` 의 `indirect:cfg:?` 는 RAX 를 **레지스터 기준 메모리** (`[RSI]`, `[RSI+8]`, `[R14]`) 에서 읽어 오면 이름을 못 붙인다. 고장은 아니다. 이번엔 `.asm` 을 읽어 출처를 직접 따라갔다 (Q4).

참고: `PspInsertThread.c` (Ghidra 디컴파일) 는 스택 값을 잘못 읽은 곳(`puVar19 + -8 = 0x14...` 꼴)이 있어 제어 흐름이 어긋나 보인다. 아래 답은 전부 `.asm` 의 점프를 따라가 확인했다.

## 범위를 넓힌 곳 (이유)

- **export 는 넓히지 않았다** (요청서대로 depth 0, 2개).
- 대신 원본 바이트 몇 개를 `objdump` 로 **읽기만** 했다 (파일 안 만듦, 작업 폴더 밖 임시 영역). 이유와 길:
  - `PspLockProcessExclusive` (rva 0x460074, 83바이트), `PspUnlockProcessExclusive` (0x4600d0, 70바이트): 길 = `NtCreateUserProcess.asm +0xe6a` 와 `_calls.tsv` 의 `PspAllocateProcess +0x26e9`. 이유 = `PspInsertThread` 가 푸는 **프로세스 락의 주인**을 알려고 (Q2 곁가지).
  - `PspMapThreadCreationFlags` (0xa01168, 80바이트): 길 = `NtCreateUserProcess.asm +0xdce`. 이유 = `PspInsertThread` 4번째 인자 비트가 어디서 만들어지나.
  - `PspExitProcess +0x98 ~ +0xa7`: 길 = 지도 (`PspCallProcessNotifyRoutines` 의 caller 2곳). 이유 = 종료 쪽이 어떤 인자로 부르나 (Q4 다).

## 질문별 답 [정적]

용어: `ret=` 는 콜스택에 찍히는 `caller+0x..` 와 같은 값. 괄호는 call 명령 위치.
인자 이름은 편의상: `Thread`(1번째, R14), `Process`(2번째, R15), `flags`(4번째 포인터가 가리키는 값), `info`(7번째 인자 구조체, `NtCreateUserProcess` 의 지역 `[rbp+0x1a0]`).
필드 이름(스레드 목록, 잡 등)은 PDB 구조체를 안 봐서 오프셋으로만 적는다.

### 1. `PspInsertThread` 가 부르는 함수 — 하는 일 묶음과 첫 스레드의 보통 길

직접 부르는 줄 62개 (`_calls.tsv`: call 59 + 조각으로 가는 tail 3). 묶음별 대표:

| 묶음 | 대표 (ret_off, call_off) | 역할 |
|---|---|---|
| 락 잡고 풀기 | `KeAbPreAcquire` +0x93 / +0x6c0, `KeAbPostRelease` +0x266 / +0x2a2 / +0x4b2 (6곳), `ExfTryToWakePushLock` (6곳, 모두 "풀 때 기다리는 쪽이 있을 때만") | 인라인 푸시락. 잡는 쪽 `ExfAcquirePushLockExclusiveEx` (+0xae, +0x6da) 는 락이 이미 잡혀 있을 때만 |
| 잡(job) | `ExAcquireResourceSharedLite` +0x106 / `ExReleaseResourceLite` +0x5f0, `PspAssignProcessToJobList` +0x8ef | 프로세스가 잡에 속할 때 잡 자원 공유 잠금, 7번째 인자에 잡 값이 있을 때 잡 배정. `PspCheckForJobAffinityViolation` +0x992 (잡이 있고 친화도 버퍼를 만들었을 때) |
| 스케줄러 | `KeStartThread` +0x5e2, `KeSuspendThread` +0x2e5, `KeReadyThread` +0x5a6 | 시작 준비 / 멈춤 표시 / 실행 대기열로 |
| 스레드 객체 등록·핸들 | `SeCreateAccessStateEx` +0x3af, `ObInsertObjectEx` +0x3ea, `PspCreateObjectHandle` +0x525 | 접근 상태 만들기 → 객체 등록 → 핸들 만들기 (`SeDeleteAccessState` +0x530 로 접근 상태 정리) |
| 알림·추적 | `PspCallProcessNotifyRoutines` +0x807, `PspCallThreadNotifyRoutines` +0x4fa, `EtwTraceProcess` +0x7d3 / `EtwTraceThread` +0x4dc | 드라이버 알림과 ETW 기록 (`PoEnergyContextStart` +0x7e4 도 이 덩어리) |
| 프로세스·스레드 상태 손보기 | `PspChangeProcessExecutionState` +0x2ba, `ObReferenceObjectExWithTag` +0x2cd, `PspRecheckThreadOptionalXStateFeatures` +0x344, `PspRecheckThreadPasidMsrState` +0x356 | 실행 상태 바꾸기, 스레드 몫 참조 2개 더 잡기 (태그 `'Dflt'` = 0x746c6644), CPU 확장 기능 재확인. 곁가지: `KeEnableCoreIsolationMitigationPolicyThread` +0xa0e, `PspCreateSecureThread` +0x93d |
| 친화도 (조건부) | `KeQueryMaximumGroupCount` +0x733 / +0x842, `ExAllocatePool2` +0x74e, `memset` +0x866, `KeAddGroupAffinityEx` +0x87c, `ExFreePoolWithTag` +0x617 | `info` 의 bit12 (오프셋 `+8`) 가 켜졌을 때 그룹 친화도 버퍼 만들기 |
| 실패 정리 | `KeUninitThread` +0x194, `MmDeleteTeb` +0x9b0, `PspDeleteUserStack` +0x838, 조각: `KeRequestTerminationThread`, `KeForceResumeThread`, `KeRaiseUserException`, `ObCloseHandle` | 되돌리기. 아래 흐름엔 안 나옴 |

**test.exe 첫 스레드의 보통 길 (점프 따라간 순서, 오프셋 순서 아님)**

`NtCreateUserProcess +0x1015` 에서 들어온 길을 기준으로 한다 (아래 "전제" 참고).

1. 프로세스 락(프로세스+0x1c8) 새로 잡기(+0x93, +0xae)는 **건너뜀** (전제 ①).
2. `info` 에서 TEB 쓸 자리·그룹 친화도 표시 읽기. 잡이 있으면 +0x106. 프로세스 비트 검사: 프로세스+0x1f4 의 `0x4000000` 이 켜지고 `0x8` 이 꺼져 있어야 계속 (아니면 `0xc0000001` 로 실패 길). 그 밖에 `0x40000000` 이 켜진 프로세스는 flags bit1 / 스레드+0x74 bit10 에 따라 실패하는 조합도 있다 (+0x948).
3. (조건부) `PspCreateSecureThread` +0x93d.
4. **`KeStartThread` +0x5e2** (오프셋은 뒤쪽이지만 흐름은 제일 먼저).
5. 잡 자원 풀기(+0x5f0, 잡 있을 때만), 스레드 플래그에 프로세스 플래그 섞기, 프로세스+0x380 (스레드 수) 1 올리고 최대치 갱신, **"첫 스레드" 판정** (+0x792).
6. 프로세스+0x5e0 푸시락 (+0x6c0) → 프로세스 목록 꼬리에 스레드 줄 달기 (호출 없는 직접 코드, 목록이 깨졌으면 `INT 0x29`, +0x703) → 락 풀기 (+0x266).
7. 프로세스 락(프로세스+0x1c8) 풀기 (+0x2a2) — **호출자가 잡아 온 것** (Q2 곁가지).
8. `PspChangeProcessExecutionState` +0x2ba (flags 의 `0x40`) → `ObReferenceObjectExWithTag` +0x2cd → **`KeSuspendThread` +0x2e5 (flags bit0 일 때만)** → (조건부) `KeEnableCoreIsolation...` +0xa0e → `PspRecheck...` +0x344, +0x356.
9. `SeCreateAccessStateEx` +0x3af → **`ObInsertObjectEx` +0x3ea** → `ObfDereferenceObject` +0x3fd → 호출자 쪽에 TEB 주소·ClientId 써 주기 (+0x40c, +0x450).
10. 스레드 플래그 `+0x5a0` 에 `0x2` 켜기 (+0x490) → **스레드 락(스레드+0x590) 풀기** (XADD +0x499, `KeAbPostRelease` +0x4b2).
11. 첫 스레드면 (+0x4b4): `EtwTraceProcess` +0x7d3 (추적 켜졌을 때만) → `PoEnergyContextStart` +0x7e4 (프로세스+0x668 ≠ 0 일 때만) → **`PspCallProcessNotifyRoutines` +0x807**.
12. `EtwTraceThread` +0x4dc (추적 켜졌을 때만) → **`PspCallThreadNotifyRoutines` +0x4fa**.
13. **`PspCreateObjectHandle` +0x525** → `SeDeleteAccessState` +0x530.
14. `info+0x170` 이 0 이 아니면 **`PspAssignProcessToJobList` +0x8ef** → 핸들 값을 호출자에게 써 주기 (+0x573).
15. **`KeReadyThread` +0x5a6** → 반환.

요청서가 짚은 지점이 흐름에서 끼는 자리:

| 지점 | 오프셋 | 흐름 순서 | 앞뒤 |
|---|---|---|---|
| `KeStartThread` | +0x5e2 | 4번 (제일 먼저) | 스레드를 프로세스 목록에 올리기 **전** |
| 스레드 객체 등록 `ObInsertObjectEx` | +0x3ea | 9번 | `KeSuspendThread` 뒤, 스레드 락 풀기 앞 |
| 프로세스 알림 덩어리 | +0x7d3 ~ +0x807 | 11번 | 스레드 락 푼 뒤, 스레드 알림 앞 |
| 스레드 알림 `PspCallThreadNotifyRoutines` | +0x4fa | 12번 | 프로세스 알림 뒤, 핸들 앞 |
| 스레드 핸들 `PspCreateObjectHandle` | +0x525 | 13번 | 스레드 알림 뒤 |
| 잡 배정 | +0x8ef | 14번 | 핸들 뒤, `KeReadyThread` 앞 |

**run9 와 맞나**: 맞다 (멈춤 `+0x2e5` → 알림 `+0x807` → 준비 `+0x5a6`). 거기에 이번에 채워진 것:
- `KeStartThread` 는 멈춤보다도 **앞**.
- 알림 사이에 **객체 등록 → 스레드 락 풀기**가 끼고, 핸들·잡은 알림 **뒤**.
- run9 은 "첫 스레드일 때만 `[추측]`" 이라 했는데, 코드로 확인됨 → 아래 "첫 스레드 판정".
- run9 의 "`ThreadFlags` bit0" 는 정확히는 **4번째 인자(flags)의 bit0** (본문 서술은 맞음). 이 flags 는 `NtCreateUserProcess +0xdce` 에서 `PspMapThreadCreationFlags` 가 `ThreadFlags` 를 옮겨 만든다 (bit0→bit0 `CREATE_SUSPENDED`, 0x2→0x2, 0x4→0x4, 0x10→0x80, 0x20→0x100, 0x40→0x200).

**전제 (코드로 본 것)**
- ① `NtCreateUserProcess +0xdfd` 는 flags 에 `0x60` 을 **무조건** 켠다 (`OR [rbp+0x10], 0x60`, 이 자리로 오는 두 길이 다 거침). `PspAllocateThread` 는 이 값을 읽기만 한다 (`PspAllocateThread.c` 의 `param_9` 쓰임 전부 읽기). 그래서 `PspInsertThread` 입구의 `flags & 0x20` 검사 (+0x77) 는 이 길에서 항상 참 → 프로세스 락을 새로 안 잡고 (`+0x7b0`) 들어간다. `flags & 0x40` 은 항상 참 → `PspChangeProcessExecutionState` +0x2ba 를 탄다.
- ② 호출자가 락을 이미 잡았다는 근거: `PspAllocateProcess +0x26e9 → PspLockProcessExclusive`. 그리고 `PspAllocateThread` 가 실패하면 `NtCreateUserProcess +0xe6a` 에서 `PspUnlockProcessExclusive`. 두 함수는 프로세스+0x1c8 을 잡고(독점) 푸는 함수다 (원본 바이트로 확인). 그러니 `PspInsertThread` 가 +0x2a2 에서 푸는 것은 이 락이다 `[정적 + 간접]`.
- 그 밖에 조건부로 못 정한 것은 아래 "동적분석으로 확인해야 할 것" 으로 넘겼다.

**첫 스레드 판정** (+0x65a ~ +0x792): 스레드 수(프로세스+0x380)를 1 올린 뒤 값이 1 이고, 프로세스+0x7dc 의 bit0 가 **이번에 처음** 켜지면 지역 값 bit1 (`EDI |= 2`) 이 켜진다. 이게 +0x4b4 의 `TEST DIL,2` 로 11번(프로세스 알림 덩어리)에 들어가는 조건이다. 값이 2가 되면 프로세스+0x1f0 의 `0x8000` 을 켠다 (둘째 스레드 표시로 보임 `[추측]`).

**알림을 부르는 조건** (프로세스·스레드 둘 다 같은 조건, +0x7ee / +0x4e3): 프로세스+0x5fc 의 bit0 가 꺼져 있으면 부른다. 켜져 있으면 프로세스+0x640 이 0 이 아닐 때만 부른다. 프로세스+0x640 은 `PspCallProcessNotifyRoutines` 안에서 "서브시스템(Pico) 프로세스" 표시를 만드는 값이라 (플래그 bit1), +0x5fc bit0 는 "일반 프로세스가 아닌 종류(최소 프로세스 등)" 표시로 보인다 `[추측]`. 일반 프로세스(test.exe)는 꺼져 있을 것으로 본다 `[추측]` → 동적 확인.

### 2. 스레드 락: `PspAllocateThread` 가 잡고 안 푼 락을 `PspInsertThread` 가 푸나

- 답: **푼다.** 같은 락이다: `PspAllocateThread +0x4ee` 의 `LEA R12,[RSI+0x590]` (독점 잡기 `KeAbPreAcquire` +0x502 / `ExfAcquirePushLockExclusiveEx` +0x51c) ↔ `PspInsertThread` 가 `[R14+0x590]` (R14 = `Thread`) 를 푸는 자리.
- 푸는 자리 (보통 길): `+0x480` 에서 `LEA RAX,[R14+0x590]`, `+0x490` 에서 `OR.LOCK [R14+0x5a0],0x2` (스레드 플래그 bit1, "삽입됨"으로 보임 `[추측]`), **`+0x499` `XADD.LOCK [RAX],RBX` (RBX=-1, 락 풀기)**, 기다리는 쪽이 있으면 `ExfTryToWakePushLock` (+0x8cf, ret +0x8d4), 이어서 **`KeAbPostRelease` call +0x4ad / ret +0x4b2**.
  - 순서: `ObInsertObjectEx` (+0x3ea) 가 **끝난 직후**. 프로세스 알림(+0x807)·스레드 알림(+0x4fa)·핸들(+0x525)은 락을 푼 **뒤**.
- 다른 자리 (실패 길): ① `ObInsertObjectEx` 가 실패 (+0x48a `JS`) → +0xa13 `XADD` → (필요하면 `ExfTryToWakePushLock` +0xa27) → 조각 `FUN_140b0d692+0x3` 의 `KeAbPostRelease`. ② 초반 실패 (+0x167~+0x187): `[R14+0x590]` 를 `XADD` 로 풀고 `KeAbPostRelease` (call +0x187, ret +0x18c) 뒤 `KeUninitThread` (+0x194).
- 곁가지 (프로세스 쪽 락 둘):
  - 프로세스+0x5e0: 잡기 `+0x6c0` (ret) ~ 풀기 `+0x266` (ret). 안에서 호출은 목록에 줄 달기(직접 코드)뿐.
  - 프로세스+0x1c8: 호출자가 잡고 들어와 `+0x2a2` (ret, call +0x29d) 에서 푼다 (위 전제 ②).
- 근거: `PspInsertThread.asm` `+0x173`, `+0x480 ~ +0x4ad`, `+0xa13 ~ +0xa33`, 조각 `FUN_140b0d692`; `PspAllocateThread.asm` `+0x4ee ~ +0x51c`.

### 3. `KeStartThread`(+0x5e2) 와 `KeReadyThread`(+0x5a6) 의 차이

- **호출 위치**:
  - `KeStartThread` +0x5e2: 정상 길 맨 앞 (스레드를 프로세스 목록에 올리고 객체로 등록하기 전). 조건: 앞쪽 검사를 통과해 실패 길로 안 빠졌을 때만.
  - `KeReadyThread` +0x5a6: **모든 길의 맨 끝**. 정상 길뿐 아니라 실패 길 (알림이 거부해 `KeRequestTerminationThread` 를 부른 뒤, 객체 등록이 실패한 뒤) 도 같은 자리 (`+0x59e`) 로 모여서 `KeReadyThread` 를 부른다. 조건 없음.
- **이름·지도로 보이는 일** (본문은 안 뽑음):
  - `KeStartThread` (1182바이트): 지도에 나오는 호출이 `KiAcquireProcessLockExclusive`, `KiCopyAffinityEx`(3곳), `KeSelectInitialIdealProcessorForThread`, `KiAdjustProcessIdealProcessorSetsForThreadCreation`, `KiUpdateSharedReadyQueueAffinityThread`, `KiInitializeForegroundBoostThread`, `EtwTraceThreadAffinity`·`EtwTraceIdealProcessor` 등. **"어느 CPU 에서 돌 수 있나·이상적 CPU·우선순위 가산" 같은 스케줄링 속성 준비**로 보인다 `[추측: 이름과 지도]`. 실행 대기열에 넣는 호출(`...Ready...`)은 지도에 안 보인다.
  - `KeReadyThread` (111바이트): `KiFastReadyThread` (+0x26), `KiInSwapSingleProcess` (+0x5b) 만 부르는 얇은 함수. **"실행 대기(ready) 상태로 넣기"** 로 보인다 `[추측]`. 지도 `callers_total` = 1: 커널 전체에서 `CALL` 로 `KeReadyThread` 를 부르는 함수가 `PspInsertThread` 뿐이다 (함수 포인터로 부르는 것은 못 셈).
- **첫 스레드가 "실행될 수 있는 상태"가 되는 건**: 코드로 보이는 한 **`KeReadyThread`** (이 함수를 부른 뒤에야 대기열에 올라감 `[추측]`). `KeStartThread` 는 그 준비 작업.
- **멈춘 채(suspend)일 때**: flags bit0 가 켜져 있으면 `KeSuspendThread` (+0x2e5) 를 **`KeReadyThread` 앞에서** 부른다 (`[정적]`, 이미 run9). 그래도 `KeReadyThread` 는 **똑같이 불린다** (멈춤 여부로 갈리는 분기가 없음). 즉 "ready 를 안 하고 둔다" 가 아니라 "멈춤 표시를 먼저 해 두고 ready 한다" 모양이다. `KeSuspendThread` 직후 스레드+0x5a0 의 bit0 가 켜져 있으면 곧바로 `KeForceResumeThread` (+0x9fc, ret +0xa01) 로 푸는 길도 있다 (오류 길로 보임 `[추측]`).
  - **멈춘 스레드가 실제로 언제·어떻게 서는지** (`KiSuspendThread` 안의 APC 등) 는 이번 범위 밖이다 → `[추측]` 으로 두고 "동적 확인 거리".

### 4. `PspCallProcessNotifyRoutines`

`PspInsertThread` 가 `(Process, info, 1)` 로 부른다 (+0x7f4 `MOV R8B,R13B`, R13=1). 안에서 `info`(2번째)는 `NtCreateUserProcess` 가 만든 구조체.

**(a) 간접 호출 4곳 — 무엇을 부르는 자리** (이름은 PDB 데이터 심볼, 주소는 `.asm` 의 `0x140...`):

| 자리 (ret) | 어디서 함수 주소를 꺼내나 | 어떤 인자로 | 언제 가나 |
|---|---|---|---|
| +0xbe (+0xc3) | `PspBamExtensionHost` (0x140fc6ce0) → `ExGetExtensionTable` 가 돌려준 **표의 첫 칸** (`MOV RAX,[RSI]`) | `(Process, 프로세스+0x1d0, info)` | 표가 있고 (`+0xa0`), `info` 가 없거나 `info` 의 CreationStatus(+0x40)가 음수가 아닐 때 |
| +0x14d (+0x152) | **`PspCreateProcessNotifyRoutine`** (0x140f05540, 64칸 × 8바이트) 의 칸 → `ExReferenceCallBackBlock` 이 돌려준 블록의 `+8` | `(Process, 프로세스+0x1d0, info)` ("Ex" 형식) | 블록 플래그 `+0x10` 의 bit1 이 켜져 있고, 전역 `PspNotifyEnableMask`(0x140fd9a88) 의 bit2 가 켜졌을 때 |
| +0x1cf (+0x1d4) | `PspDamExtensionHost` (0x140fc7078) → 표의 첫 칸 (`MOV RAX,[R14]`) | 위 BAM 과 같은 모양 | 위 BAM 과 같은 조건 (BAM 다음) |
| +0x31c (+0x321) | 위와 **같은 목록** `PspCreateProcessNotifyRoutine`, 블록 `+8` | `(프로세스+0x2d0 = 부모 id, 프로세스+0x1d0 = 프로세스 id, Create)` (옛 3인자 형식) | 블록 플래그 bit1 이 **꺼져 있을 때**. 대상 주소가 `KeReleaseSpinLock` 과 같으면 +0x1af 에서 그 함수를 **직접** 부르는 분기가 따로 있고 (컴파일러가 자주 나온 대상을 직접 호출로 바꾼 모양 `[추측]`), 다르면 CFG 를 거쳐 +0x31c |

  - 목록 도는 법: 0 ~ 0x3f (64칸) 를 차례로 돌며 `ExReferenceCallBackBlock` (+0x118) 로 블록을 얻고, 다 부른 뒤 `ExDereferenceCallBackBlock` (+0x158) 로 돌려놓는다. 목록 돌기에 들어가는 조건은 `PspNotifyEnableMask` 의 bit1 또는 bit2 (+0x91 ~ +0x9e).
  - 프로세스+0x640 (Pico 표시) 이 0 이 아니면 블록 플래그 bit2 가 켜진 것만 부른다 (+0x183).
  - BAM / DAM 이름은 PDB 에서 온 것이다. 윈도우의 "백그라운드 / 데스크톱 활동 관리" 확장이 등록한 표로 보인다 `[추측]` (함수 이름은 이 빌드 PDB 에서만 확인).
  - 어떤 드라이버가 실제로 이 목록에 등록돼 있는지, 표 첫 칸이 어느 함수인지는 코드로는 모른다 (동적 확인 거리).
- 알림 정보 구조체: 크기 0x48 (`memset` +0x34), 채우는 모양이 `PS_CREATE_NOTIFY_INFO` (크기·플래그·부모 id·만든 스레드 id·파일 객체·이미지 이름·명령줄·CreationStatus) 와 같다 `[추측: 오프셋 대응은 맞음]`. 만든 스레드 id = **지금 실행 중인 스레드**(`gs:[0x188]` → +0x508) = 부모(cmd.exe) 스레드 (run3 `[관찰]` 과 맞음). 파일 객체·이미지 이름·명령줄은 `info` 에서 (`+0xb8`, `+0xf0` 또는 파일객체+0x58, `+0xd8` 가 가리키는 것의 `+0x70`) 꺼내고, 없으면 `PsReferenceProcessFilePointer` (+0x2ba, ret +0x2bf) 로 파일 객체를 얻고 끝에서 `ObfDereferenceObject` (+0x32a) 로 돌려놓는다.

**(b) `PsTerminateProcess` (+0x171, ret +0x176) 를 부르는 조건**: `info` 구조체가 만들어졌고 (= Create 이고 아래 조건), 목록 안 한 칸을 부른 뒤 `info` 의 `CreationStatus` (+0x40) 가 **음수**일 때다 (`+0x162 MOV EAX,[RBX+0x40]; TEST EAX,EAX; JNS 다음 칸`). 즉 **알림 함수가 `info->CreationStatus` 에 음수를 써서 "생성 거부"** 했을 때 그 값으로 `PsTerminateProcess(Process, 그 값)` 을 부르고 목록 돌기를 멈춘다 (+0x17e 로 뒤 BAM/DAM 도 건너뜀). 이 음수는 반환값(R12D)으로 `PspInsertThread +0x807` 에 돌아가고, 거기서 (+0x505 `JS`) **핸들 만들기(+0x525)를 건너뛰고 실패 길**로 간다 (스레드+0x5d8 에 상태 저장 → `KeRequestTerminationThread` → `KeReadyThread` → 음수 반환). 옛 3인자 형식 블록은 `info` 를 못 받으니 이 길을 못 만든다 `[정적]`.
  - `info` 가 만들어지는 조건 (+0x1f4 ~ +0x2ae): Create 가 참이고 `PspNotifyEnableMask` bit2 가 켜졌거나 DAM·BAM 표가 있을 때. 아니면 `info` 없음.

**(c) 보통 프로세스를 만들 때 생성/종료 중 어느 길**: **생성 길.**
  - 3번째 인자 `param_3` (Create, `R15B`) 가 0 이 아니면 +0x86 에서 `info` 를 만드는 쪽 (+0x1f4) 으로 간다. `PspInsertThread` 는 1 을 넘긴다 (+0x7f4).
  - 종료 쪽: `PspExitProcess +0xa2 → +0xa7` 은 `(Process, NULL, 0)` 으로 부른다 (원본 바이트: `xor r8d,r8d; xor edx,edx`). 이때는 `info` 없이 목록을 돌고 Ex 형식은 `info=NULL` 로 불린다.
  - 한 가지 더: `PspInsertThread` 쪽 문턱 (프로세스+0x5fc bit0 / 프로세스+0x640) 을 통과해야 이 함수까지 온다 (위 1번 "알림을 부르는 조건").

### 5. `PspCallThreadNotifyRoutines` (+0x4fa) 한 줄

- 지도·이름으로: `ExReferenceCallBackBlock`/`ExDereferenceCallBackBlock` 을 쓰며 목록을 돌고 CFG 간접 호출 3곳 (+0xbd, +0x11c, +0x186) 을 가진, 프로세스 쪽과 **같은 모양** 의 함수 (456바이트). **드라이버가 등록해 둔 "스레드 만들어짐/끝남" 알림 함수들을 부르는 곳**으로 보인다 `[추측]`.
- caller 는 `PspInsertThread +0x4f5` (인자 `(Thread, 1, 0)`) 와 `PspExitThread +0x142` (지도 깊이 6).
- `PspInsertThread` 에서는 **모든 스레드가** 부른다 (첫 스레드만이 아님). 부르는 조건은 위 "알림을 부르는 조건" 과 같다.

### 6. 표기 규칙

- `tail` (`PspInsertThread +0xa33`, 조각 `FUN_140b0d692` / `FUN_140b0d71b` / `FUN_140843eba`), `frag:` 조각 안 호출, `indirect-cfg` 는 reference.md 대로 읽었다.
- `PspInsertThread` 에는 alias 경고가 붙은 호출이 없다. `KeStartThread` 안 `KiCopyAffinityEx` 에 "이름 1개 더" 경고가 있으나 대표로 쓰지 않았다.
- 주인 없는 덩어리: 이번 두 함수엔 없다 (위 "스크립트 문제" 1 의 구멍은 점프로 안 닿는 코드).

## 동적분석으로 확인해야 할 것

dast 요청 필요: 있음 (Q19·Q38 과 합쳐서 한 번에).

1. **test.exe 첫 스레드가 멈춘 채인가**: `flags & 1` → `+0x2e0` 호출 여부. (Q19)
2. **프로세스 락이 호출자에서 들어오나**: 입구에서 `flags & 0x20` 과 프로세스+0x1c8 의 bit0 (잡힌 상태).
3. **첫 스레드 판정이 정말 타나**: `+0x802` 호출이 부모 스레드 컨텍스트에서 1번만 일어나고, 둘째 스레드(CreateThread)는 안 거치는지 (run4 의 새 스레드 때 `+0x802` 가 안 걸리는지).
4. **알림을 받는 쪽**: 4개의 간접 호출 자리 (`+0xbe`, `+0x14d`, `+0x1cf`, `+0x31c`) 에서 call 직전 `rax` (= 대상 함수). 실제로 어떤 자리가 타고 어떤 함수가 불리는지. 그리고 `PspNotifyEnableMask`, `PspCreateProcessNotifyRoutine` 64칸, `PspBamExtensionHost`, `PspDamExtensionHost` 값.
5. **`PsTerminateProcess` (+0x171) 가 안 타는지** (보통 안 탈 것 `[추측]`).
6. **잡 길** (+0x101 `ExAcquireResourceSharedLite`, +0x8ea `PspAssignProcessToJobList`) 이 test.exe 에서 안 타는지, `PspCreateSecureThread` (+0x938) 도.
7. **`KeStartThread` / `KeReadyThread` 전후 스레드 상태**: +0x5dd 와 +0x5a1 에서 `rcx` (= Thread) 의 상태 값(`dt nt!_KTHREAD State`)이 어떻게 바뀌는지.
8. **프로세스+0x5fc bit0 / 프로세스+0x640** 값 (일반 프로세스에서 알림 조건을 통과하는지).

추천 멈춤점 (call 명령 위치를 적음. 콜스택엔 ret 가 보임. 조건은 새 프로세스가 test.exe 일 때만. 입구에서 `rdx` = 새 프로세스 EPROCESS, `rcx` = 새 스레드):
- `nt!PspInsertThread` 입구: `rdx` 의 ImageFileName 으로 test.exe 걸러내고, `poi(@r9)` (flags) 를 기록.
- `nt!PspInsertThread+0x2e0` (KeSuspendThread, 걸리면 멈춘 채), `+0x5dd` (KeStartThread), `+0x5a1` (KeReadyThread), `+0x499` (스레드 락 풀기, 락 값 `poi(@rax)` 로 확인), `+0x802` (프로세스 알림, `r8b == 1`).
- `nt!PspCallProcessNotifyRoutines+0x14d`, `+0x31c`, `+0xbe`, `+0x1cf` (대상은 `rax`, `ln @rax`).

## 판정

**분석성공.** 질문 1~6 모두 `.asm` 줄과 함께 답했다. 정적으로 못 정한 것 (실제 등록된 알림 함수, 조건부 길의 실제 분기, 멈춘 스레드가 서는 시점) 은 위 동적 목록으로 넘겼다. 버전 대조도 일치.

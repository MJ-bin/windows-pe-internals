# 2026-10-21 run12 — `PspInsertThread` 안: `KeStartThread` → 멈춤 → 객체 등록 → 락 풀기 → 알림 → 핸들 → `KeReadyThread`, `PspCallProcessNotifyRoutines` 의 간접 호출 4곳 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: ntoskrnl (`ntoskrnl.exe`, 26100.9457), pdb `ntkrnlmp.pdb` GUID `C29EBFB06B78B3C020DCA66D99713F9E1`
  - run3(관찰)·run9 ~ run11(정적)와 같은 커널이다.
- 뽑은 함수 2개 (depth 0): `PspInsertThread`, `PspCallProcessNotifyRoutines`
  - 다른 함수 본문은 안 뽑았다. `KeStartThread`·`KeReadyThread`·`PspCallThreadNotifyRoutines` 는 이름·지도(`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_{nodes,edges}.tsv`)로만 봤다.
  - 결과서가 원본 바이트를 몇 군데 읽기만 했다 (`PspMapThreadCreationFlags`, `PspLockProcessExclusive`·`PspUnlockProcessExclusive`, `PspExitProcess +0x98 ~ +0xa7`). 아래에서 "(원본 바이트)" 라고 붙인다.

## 목적

- 대기열 Q4. 그림 1 의 `PspInsertThread` 아래는 run9 때 대표 3개(멈춤·알림·준비)뿐이었다. 안에서 무엇을 **어떤 흐름 순서로** 부르는지 본다.
  - 특히 `KeStartThread`(+0x5e2), 객체 등록, 스레드 알림, 스레드 핸들, 잡 배정이 어디에 끼는지.
- run11 에서 남긴 질문: `PspAllocateThread` 가 잡고 안 푼 **스레드 락**을 `PspInsertThread` 가 푸나.
- `KeStartThread` 와 `KeReadyThread` 차이. 멈춘 채(suspend)일 때 `KeReadyThread` 를 건너뛰나.
- `PspCallProcessNotifyRoutines` 의 간접 호출 4곳이 무엇을 부르는 자리인가, `PsTerminateProcess`(+0x176)는 언제 타나.

## 방법

- 정적: `_workspace/sast/07_insert-thread.md` — ntoskrnl, `^(PspInsertThread|PspCallProcessNotifyRoutines)$` depth 0
- 결과서: `_workspace/sast/07_insert-thread_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨):
  - `ms/disassemble/ntoskrnl/_calls.tsv`, `PspInsertThread.asm`, `PspCallProcessNotifyRoutines.asm`
  - `ms/decomp/ntoskrnl/PspInsertThread.c`, `PspCallProcessNotifyRoutines.c` (머리말 `callers:` 줄)
  - 지도 `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_{nodes,edges}.tsv`
- 오프셋 읽는 법 (run9 ~ 11 과 같음)
  - `call` = call 명령이 있는 자리
  - `ret` = call 다음 명령 자리 = **WinDbg 콜스택에 찍히는 오프셋**. flow.md 그림 라벨은 이걸 쓴다.
- 주의: `PspInsertThread` 는 **오프셋 순서 ≠ 흐름 순서**다. 결과서가 `.asm` 의 점프를 따라가 흐름 순서를 정했다. `PspInsertThread.c`(디컴파일)는 스택 값을 잘못 읽은 곳이 있어서 흐름 판단에 안 썼다 (결과서).

## 결과

### 정적 — 큰 줄기 (대표 지점)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0x1015` | `+0x101a` | `nt!PspInsertThread` | [26100.9457 확인] + [정적] (run9 에서 이미 맞춤) |
| `nt!PspInsertThread` | `+0x5dd` | `+0x5e2` | `nt!KeStartThread` | [정적] |
| `nt!PspInsertThread` | `+0x2e0` | `+0x2e5` | `nt!KeSuspendThread` (flags bit0 일 때만) | [정적] |
| `nt!PspInsertThread` | `+0x3e5` | `+0x3ea` | `nt!ObInsertObjectEx` (스레드 객체 등록) | [정적] |
| `nt!PspInsertThread` | `+0x4ad` | `+0x4b2` | `nt!KeAbPostRelease` (스레드 락 풀기 뒤처리) | [정적] |
| `nt!PspInsertThread` | `+0x802` | `+0x807` | `nt!PspCallProcessNotifyRoutines` (첫 스레드만) | [26100.9457 확인] + [정적] |
| `nt!PspInsertThread` | `+0x4f5` | `+0x4fa` | `nt!PspCallThreadNotifyRoutines` | [정적] |
| `nt!PspInsertThread` | `+0x520` | `+0x525` | `nt!PspCreateObjectHandle` (스레드 핸들) | [정적] |
| `nt!PspInsertThread` | `+0x8ea` | `+0x8ef` | `nt!PspAssignProcessToJobList` (조건부) | [정적] |
| `nt!PspInsertThread` | `+0x5a1` | `+0x5a6` | `nt!KeReadyThread` | [정적] |
| `nt!PspCallProcessNotifyRoutines` | `+0x14d` | `+0x152` | 간접 (`PspCreateProcessNotifyRoutine` 목록, Ex 형식) | [정적] |
| `nt!PspCallProcessNotifyRoutines` | `+0x31c` | `+0x321` | 간접 (같은 목록, 옛 형식) | [정적] |
| `nt!PspCallProcessNotifyRoutines` | `+0xbe` | `+0xc3` | 간접 (`PspBamExtensionHost` 표) | [정적] |
| `nt!PspCallProcessNotifyRoutines` | `+0x1cf` | `+0x1d4` | 간접 (`PspDamExtensionHost` 표) | [정적] |
| `nt!PspCallProcessNotifyRoutines` | `+0x171` | `+0x176` | `nt!PsTerminateProcess` (알림이 생성을 거부했을 때) | [정적] |

- run9 의 버전 대조와 같다: `PspInsertThread +0x807 → PspCallProcessNotifyRoutines`, `NtCreateUserProcess +0x101a → PspInsertThread` 는 run3 `[관찰]` 과 오프셋까지 같다.
- 이 함수를 부르는 곳 (머리말 `callers:`)
  - `PspInsertThread`: `NtCreateUserProcess`, `PspCreateThread`, `PspCreatePicoThread`
  - `PspCallProcessNotifyRoutines`: `PspInsertThread`, `PspExitProcess`

### 정적 — `PspInsertThread` 직접 호출, 하는 일 단위로

- 장부(`_calls.tsv`)에 **62줄** (call 59 + 조각으로 가는 tail 3). 숫자는 ret.

| 하는 일 | 대표 호출 |
|---|---|
| 스케줄러 | **`KeStartThread` `+0x5e2`**, **`KeSuspendThread` `+0x2e5`**, **`KeReadyThread` `+0x5a6`** |
| 스레드 객체 등록·핸들 | `SeCreateAccessStateEx` `+0x3af`, **`ObInsertObjectEx` `+0x3ea`**, **`PspCreateObjectHandle` `+0x525`**, `SeDeleteAccessState` `+0x530` |
| 알림·추적 | `EtwTraceProcess` `+0x7d3`, `PoEnergyContextStart` `+0x7e4`, **`PspCallProcessNotifyRoutines` `+0x807`**, `EtwTraceThread` `+0x4dc`, **`PspCallThreadNotifyRoutines` `+0x4fa`** |
| 락 잡고 풀기 | `KeAbPreAcquire` `+0x93` / `+0x6c0`, `KeAbPostRelease` `+0x266` / `+0x2a2` / `+0x4b2` 등 6곳, `ExfTryToWakePushLock` 6곳 (기다리는 쪽이 있을 때만) |
| 잡(job) | `ExAcquireResourceSharedLite` `+0x106` / `ExReleaseResourceLite` `+0x5f0`, `PspAssignProcessToJobList` `+0x8ef`, `PspCheckForJobAffinityViolation` `+0x992` |
| 프로세스·스레드 상태 손보기 | `PspChangeProcessExecutionState` `+0x2ba`, `ObReferenceObjectExWithTag` `+0x2cd`, `PspRecheckThreadOptionalXStateFeatures` `+0x344`, `PspRecheckThreadPasidMsrState` `+0x356` |
| 곁가지 (조건부) | `PspCreateSecureThread` `+0x93d`, `KeEnableCoreIsolationMitigationPolicyThread` `+0xa0e`, 그룹 친화도 버퍼 (`ExAllocatePool2` `+0x74e`, `KeAddGroupAffinityEx` `+0x87c` 등) |
| 실패 정리 | `KeUninitThread` `+0x194`, `MmDeleteTeb` `+0x9b0`, `PspDeleteUserStack` `+0x838`, 조각 안 `KeRequestTerminationThread`·`KeForceResumeThread`·`KeRaiseUserException`·`ObCloseHandle` |

### 정적 — test.exe 첫 스레드의 보통 길 (흐름 순서, 오프셋 순서 아님)

`NtCreateUserProcess +0x1015` 에서 들어온 길. 굵은 것이 그림에 넣은 대표.

1. 프로세스 락을 **새로 잡지 않고** 들어간다 (`+0x77` `flags & 0x20` 검사 → 이 길에선 늘 참, 아래 "전제").
2. 프로세스 비트 검사 (`프로세스+0x1f4`), 잡이 있으면 잡 자원 잠금 (`+0x106`). (조건부) `PspCreateSecureThread` `+0x93d`.
3. **`KeStartThread` `+0x5e2`** — 오프셋은 뒤쪽이지만 흐름은 제일 먼저.
4. 스레드 수(`프로세스+0x380`) 1 올리기 → **첫 스레드 판정** (`+0x792`).
5. 프로세스 `+0x5e0` 푸시락 (`+0x6c0`) → **프로세스의 스레드 목록 꼬리에 직접 달기** (호출 없음, 목록이 깨졌으면 `+0x703` `INT 0x29`) → 풀기 (`+0x266`).
6. 프로세스 락(`프로세스+0x1c8`) 풀기 `+0x2a2` — 호출자가 잡아 온 것 (아래 "전제").
7. `PspChangeProcessExecutionState` `+0x2ba` → `ObReferenceObjectExWithTag` `+0x2cd` → **`KeSuspendThread` `+0x2e5` (flags bit0 일 때만)** → `PspRecheck…` `+0x344`, `+0x356`.
8. `SeCreateAccessStateEx` `+0x3af` → **`ObInsertObjectEx` `+0x3ea`** (스레드 객체 등록) → 호출자에게 TEB 주소·ClientId 써 주기.
9. 스레드 플래그 `스레드+0x5a0` 에 `0x2` 켜기 (`+0x490`) → **스레드 락 풀기** (`+0x499` `XADD`, `KeAbPostRelease` `+0x4b2`).
10. 첫 스레드면 (`+0x4b4` `TEST DIL,2` → `+0x4b8` 로 뒤쪽 덩어리): `EtwTraceProcess` `+0x7d3` (추적 켜졌을 때) → `PoEnergyContextStart` `+0x7e4` (조건부) → **`PspCallProcessNotifyRoutines` `+0x807`** → `+0x809` 에서 `+0x4be` 로 돌아옴.
11. `EtwTraceThread` `+0x4dc` (추적 켜졌을 때) → **`PspCallThreadNotifyRoutines` `+0x4fa`**.
12. **`PspCreateObjectHandle` `+0x525`** (스레드 핸들) → `SeDeleteAccessState` `+0x530`.
13. (조건부, `info+0x170` ≠ 0) `PspAssignProcessToJobList` `+0x8ef` → 핸들 값을 호출자에게 써 주기.
14. **`KeReadyThread` `+0x5a6`** → 반환.

- 인자 이름은 결과서의 편의 이름이다: `Thread`(1번째), `Process`(2번째), `flags`(4번째 인자가 가리키는 값), `info`(7번째 인자, `NtCreateUserProcess` 의 지역 구조체).
- 필드 이름(스레드 목록, 잡 등)은 PDB 구조체를 안 봐서 오프셋으로만 적었다.

### 정적 — `PspCallProcessNotifyRoutines` 간접 호출 4곳

`PspInsertThread +0x7f4` 에서 3번째 인자 `Create = 1` 로 부른다. 이름은 PDB 데이터 심볼.

| ret (call) | 어디서 함수 주소를 꺼내나 | 인자 모양 | 언제 가나 |
|---|---|---|---|
| `+0x152` (`+0x14d`) | **`PspCreateProcessNotifyRoutine`** (64칸) 한 칸 → `ExReferenceCallBackBlock` 이 준 블록 `+8` | `(Process, 프로세스 id, info)` — "Ex" 형식 | 블록 플래그 bit1 켜짐 + 전역 `PspNotifyEnableMask` bit2 켜짐 |
| `+0x321` (`+0x31c`) | 같은 목록, 블록 `+8` | `(부모 id, 프로세스 id, Create)` — 옛 3인자 형식 | 블록 플래그 bit1 꺼짐. 대상이 `KeReleaseSpinLock` 이면 `+0x1b4` 에서 그걸 직접 부르는 갈래가 따로 있다 |
| `+0xc3` (`+0xbe`) | **`PspBamExtensionHost`** → `ExGetExtensionTable` 이 준 표의 첫 칸 | `(Process, 프로세스 id, info)` | 표가 있고, `info` 가 없거나 `info` 의 CreationStatus 가 음수가 아닐 때 |
| `+0x1d4` (`+0x1cf`) | **`PspDamExtensionHost`** → 표의 첫 칸 | BAM 과 같은 모양 | BAM 과 같은 조건 |

- **흐름 순서** (`.asm` 점프를 따라 봄): `info` 만들기 (`+0x1f4 ~ +0x288`) → **목록 64칸 돌기** (`+0x101 ~ +0x128`, 칸마다 Ex 형식 `+0x152` 또는 옛 형식 `+0x321` 중 하나) → 다 돌면 `+0x17e` 에서 `+0xa0` 으로 → **BAM** `+0xc3` → **DAM** `+0x1d4` → 끝.
  - `PspNotifyEnableMask` 의 bit1·bit2 가 둘 다 꺼져 있으면 목록 돌기를 건너뛰고 BAM 으로 바로 간다 (`+0x91 ~ +0x9e`).
- 목록 칸·표 첫 칸에 **실제로 어떤 함수가 있는지는 코드로 모른다** (실행 때 정해짐).
- `info` (크기 0x48) 를 채우는 모양이 `PS_CREATE_NOTIFY_INFO` 와 같다 `[추측: 오프셋 대응]`. "만든 스레드 id" 칸에는 **지금 도는 스레드**(`gs:[0x188]` → `+0x508`) = 부모 cmd.exe 스레드를 넣는다 `[정적]`.

### 호출 관계

```
NtCreateUserProcess
 └─ (10) +0x101a  PspInsertThread                      [관찰] run3 + [정적]   (번호 = 흐름 순서)
                   ├─ (1) +0x5e2  KeStartThread          [정적] 제일 먼저
                   │     (호출 없음: 프로세스의 스레드 목록에 직접 달기, 프로세스 락 +0x2a2 풀기)
                   ├─ (2) +0x2e5  KeSuspendThread        [정적] flags bit0 일 때만
                   │     (+0x3ea ObInsertObjectEx 객체 등록 → +0x499/+0x4b2 스레드 락 풀기)
                   ├─ (3) +0x807  PspCallProcessNotifyRoutines   [관찰] run3 + [정적] 첫 스레드만
                   │               ├─ (1) +0x152  (간접·대상 모름) PspCreateProcessNotifyRoutine 목록, Ex 형식  ┐ 칸마다
                   │               ├─ (2) +0x321  (간접·대상 모름) 같은 목록, 옛 형식                          ┘ 둘 중 하나
                   │               ├─ (3) +0xc3   (간접·대상 모름) PspBamExtensionHost 표 첫 칸
                   │               └─ (4) +0x1d4  (간접·대상 모름) PspDamExtensionHost 표 첫 칸
                   ├─ (4) +0x4fa  PspCallThreadNotifyRoutines    [정적] 모든 스레드
                   │     (+0x525 PspCreateObjectHandle 스레드 핸들, +0x8ef 잡 배정 조건부)
                   └─ (5) +0x5a6  KeReadyThread          [정적] 멈춘 스레드도 똑같이
```

## 확인된 것

- **`PspInsertThread` 흐름은 `KeStartThread` → (목록에 달기) → `KeSuspendThread` → 객체 등록 → 스레드 락 풀기 → 프로세스 알림 → 스레드 알림 → 스레드 핸들 → (잡) → `KeReadyThread`.** [정적]
  - run9 의 "멈춤 `+0x2e5` → 알림 `+0x807` → 준비 `+0x5a6`" 순서는 그대로 맞다.
  - 새로 채운 것: `KeStartThread`(`+0x5e2`)는 멈춤보다도 **앞**. 객체 등록과 스레드 락 풀기는 멈춤과 알림 **사이**. 스레드 핸들·잡은 알림 **뒤**.
- **run11 의 스레드 락을 `PspInsertThread` 가 푼다.** [정적] (run11 의 `[추측]` 이 맞았다)
  - 같은 락이다: `PspAllocateThread +0x4ee` `LEA R12,[RSI+0x590]` 로 잡은 것 ↔ `PspInsertThread +0x480` `LEA RAX,[R14+0x590]` (R14 = 새 스레드).
  - 푸는 자리: `+0x490` 스레드 플래그 `+0x5a0` 에 `0x2` 켜기 (뜻은 "삽입됨" 으로 보임 `[추측]`) → **`+0x499` `XADD.LOCK` (락 풀기)** → **`KeAbPostRelease` call `+0x4ad` / ret `+0x4b2`**.
  - `ObInsertObjectEx`(`+0x3ea`)가 끝난 **직후**다. 알림·핸들은 락을 푼 뒤.
  - 실패 길에서 푸는 자리도 따로 있다 (`+0xa13` 이후, 초반 실패 `+0x167 ~ +0x18c`).
- **프로세스 락(`프로세스+0x1c8`)은 호출자가 잡아 오고 `PspInsertThread` 가 `+0x2a2` 에서 푼다.** [정적 + 간접]
  - 잡는 곳: `PspAllocateProcess +0x26e9 → PspLockProcessExclusive`. `PspAllocateThread` 실패 때는 `NtCreateUserProcess +0xe6a` 에서 `PspUnlockProcessExclusive` (두 함수가 `+0x1c8` 을 잡고 푸는 것은 원본 바이트로 확인).
  - "전제": `NtCreateUserProcess +0xdfd` 가 flags 에 `0x60` 을 **무조건** 켠다 (`OR dword ptr [RBP+0x10],0x60`). 그래서 `PspInsertThread` 입구 `flags & 0x20` 검사는 이 길에서 늘 참 → 락을 새로 안 잡는다. `flags & 0x40` 도 늘 참 → `PspChangeProcessExecutionState` `+0x2ba` 를 탄다.
- **`PspInsertThread` 의 flags 는 `PspMapThreadCreationFlags` 가 `ThreadFlags` 를 옮겨 만든 값이고, bit0 는 bit0 로 간다.** [정적] (원본 바이트)
  - `NtCreateUserProcess +0xdce` (ret `+0xdd3`) `PspMapThreadCreationFlags`: bit0→bit0, `0x2`→`0x2`, `0x4`→`0x4`, `0x10`→`0x80`, `0x20`→`0x100`, `0x40`→`0x200`.
  - run9 에서 본 입구 검사(`ThreadFlags` 는 bit0 만 허용)와 합치면, 이 길에서 flags 에 실려 오는 `ThreadFlags` 비트는 bit0 하나뿐이다. 여기에 `+0xdfd` 의 `0x60` 이 더해진다.
- **`KeReadyThread` 는 멈춘 스레드에도 불린다.** [정적]
  - `+0x5a6` 은 **모든 길의 끝**이다. 멈춤 여부로 갈리는 분기가 없다. 즉 "ready 를 안 하고 둔다" 가 아니라 "멈춤 표시를 먼저 해 두고 ready 한다" 모양이다.
  - 실패 길(알림이 거부함, 객체 등록 실패)도 조각 `FUN_140b0d71b` 에서 `KeRequestTerminationThread` 를 부르거나 (다른 갈래에선 멈춰 둔 스레드를 `KeForceResumeThread` 로 풀고) 같은 자리(`+0x59e`)로 돌아와 `KeReadyThread` 를 부른다. 조각 안 `JMP` 3곳(`+0x1f`, `+0x31`, `+0x40`)이 `+0x59e` 로 간다.
  - 지도 `callers_total` = 1: 커널 안에서 `CALL` 로 `KeReadyThread` 를 부르는 함수는 `PspInsertThread` 하나뿐 (함수 포인터 호출은 못 셈).
  - 멈춘 스레드가 실제로 언제 서는지(`KeSuspendThread` 안쪽)는 이번 범위 밖이다. 정적으로 못 정했다.
- **`KeStartThread` 와 `KeReadyThread` 의 차이** — 이름·지도로 본 것이라 `[추측]`
  - `KeStartThread` (1182바이트): 지도에서 부르는 것이 `KiAcquireProcessLockExclusive`, `KeSelectInitialIdealProcessorForThread`, `KiAdjustProcessIdealProcessorSetsForThreadCreation`, `KiUpdateSharedReadyQueueAffinityThread`, `KiInitializeForegroundBoostThread` 등. "어느 CPU 에서 돌지·우선순위 가산" 같은 **스케줄링 준비**로 보인다. 대기열에 넣는 호출은 지도에 없다.
  - `KeReadyThread` (111바이트): `KiFastReadyThread`, `KiInSwapSingleProcess` 만 부르는 얇은 함수. **실행 대기 줄에 넣기**로 보인다.
- **첫 스레드 판정이 코드에 있다.** [정적] (run9 에서 `[추측]` 이던 것)
  - 스레드 수(`프로세스+0x380`)를 1 올린 값이 1 이고, `프로세스+0x7dc` bit0 가 **이번에 처음** 켜지면 지역 값 bit1 (`+0x792` `OR EDI,0x2`) 이 켜진다.
  - `+0x4b4` `TEST DIL,0x2` 가 이 비트를 보고 프로세스 알림 덩어리로 간다. 그러니 **프로세스 알림은 첫 스레드만**, 스레드 알림(`+0x4fa`)은 **모든 스레드**.
  - `[관찰]` run3: 부모 cmd.exe 스레드에서 test.exe 첫 스레드를 넣을 때 `+0x807` 로 불렸다. 코드와 맞다. (둘째 스레드 때 안 불리는 건 아직 안 봤다.)
- **알림을 부르는 문턱** (프로세스·스레드 알림 같은 조건, `+0x7ee` / `+0x4e3`) [정적]
  - `프로세스+0x5fc` bit0 가 꺼져 있으면 부른다. 켜져 있으면 `프로세스+0x640` 이 0 이 아닐 때만.
  - `+0x640` 은 `PspCallProcessNotifyRoutines` 안에서 Pico(서브시스템) 프로세스 표시로 쓰이는 값이라, `+0x5fc` bit0 는 "일반 프로세스가 아닌 종류" 표시로 보인다 `[추측]`. test.exe 는 꺼져 있을 것 `[추측]`.
- **`PsTerminateProcess`(`+0x176`)는 알림 함수가 생성을 거부했을 때만 탄다.** [정적]
  - 목록 한 칸을 부른 뒤 `+0x162` `MOV EAX,[RBX+0x40]` / `TEST` / `JNS` — `info` 의 CreationStatus(`+0x40`)가 **음수**면 `PsTerminateProcess(Process, 그 값)` 을 부르고 목록 돌기를 멈춘다.
  - 그 음수가 반환값으로 `PspInsertThread +0x807` 에 돌아가면, `+0x505` `JS` 로 **핸들 만들기를 건너뛰고** 실패 길 → `KeRequestTerminationThread` → `KeReadyThread` → 음수 반환.
  - 옛 3인자 형식 블록은 `info` 를 못 받으니 이 길을 못 만든다.
- **`PspInsertThread` 에서 부르면 "생성" 길이다.** [정적]
  - 3번째 인자 `Create` 가 0 이 아니면 `+0x86` 에서 `info` 만들기(`+0x1f4`)로 간다. `PspInsertThread` 는 1 을 준다 (`+0x7f4`).
  - 종료 쪽 caller `PspExitProcess +0xa7` 은 `(Process, NULL, 0)` 으로 부른다 (원본 바이트).
- **`PspCallThreadNotifyRoutines`** (`+0x4fa`, 456바이트): 프로세스 쪽과 같은 모양(콜백 블록 목록 + CFG 간접 호출 3곳)이다. 드라이버가 등록한 "스레드 생김/끝남" 알림을 부르는 곳으로 보인다 `[추측]`. caller 는 `PspInsertThread +0x4fa` 와 `PspExitThread`.

## 정정

- **"`ThreadFlags` bit0" 표현** (run9, flow.md 2.2 · 4.1)
  - 정확히는 **`PspInsertThread` 4번째 인자(flags)의 bit0** 다. 이 flags 는 `NtCreateUserProcess +0xdd3` `PspMapThreadCreationFlags` 가 `ThreadFlags` 를 옮겨 만든 값이다 (bit0 → bit0, 위 "확인된 것").
  - 그러니 run9 의 내용(멈출지 말지를 그 비트가 정한다)은 맞고, 이름만 정확히 한다. run9 문서는 고치지 않고 flow.md 2.2 를 고쳤다.
- **flow.md 2.2 `KeReadyThread` 의 "실패면 `+0x598` 에서 다른 길로 빠짐"** (run9 바퀴에 flow.md 에만 쓴 문장)
  - 실패 길도 조각 `FUN_140b0d71b` 에서 `KeRequestTerminationThread` (또는 `KeForceResumeThread`) 를 거친 뒤 `+0x59e` 로 돌아와 **`KeReadyThread` 를 부른다.** "다른 길로 빠짐" 이 아니라 "돌아서 같은 자리로 옴".
- **그림 1 `PspInsertThread` 아래 번호** (run9 때 `(1) Suspend (2) 알림 (3) Ready`)
  - `KeStartThread` 가 흐름상 제일 먼저라 `(1) KeStartThread (2) KeSuspendThread (3) 프로세스 알림 (4) 스레드 알림 (5) KeReadyThread` 로 다시 매겼다. 셋의 앞뒤 순서는 run9 와 같다.

## 아직 확인 필요

- **test.exe 첫 스레드가 멈춘 채인가**: `nt!PspInsertThread` 입구에서 `poi(@r9) & 1`, `+0x2e0` 이 걸리나. (Q19)
- **프로세스 락이 호출자에서 들어오나**: 입구에서 `poi(@r9) & 0x20`, `프로세스+0x1c8` bit0 (잡힌 상태).
- **첫 스레드 판정**: `+0x802` 가 test.exe 에서 한 번만 걸리고, 둘째 스레드(`test!Worker`, ntdll 일꾼) 때는 안 걸리는지. `+0x4f5`(스레드 알림)는 매번 걸려야 한다.
- **알림을 받는 쪽**: `nt!PspCallProcessNotifyRoutines+0x14d`, `+0x31c`, `+0xbe`, `+0x1cf` 에서 call 직전 `ln @rax`. 그리고 `PspNotifyEnableMask`, `PspCreateProcessNotifyRoutine` 64칸, `PspBamExtensionHost`, `PspDamExtensionHost` 값.
- **`PsTerminateProcess`(`+0x171`) 가 안 타는지** (보통 안 탈 것 `[추측]`).
- **잡 길·보안 스레드 길**: `+0x101` `ExAcquireResourceSharedLite`, `+0x8ea` `PspAssignProcessToJobList`, `+0x938` `PspCreateSecureThread` 가 test.exe 에서 안 타는지.
- **스레드 상태 변화**: `+0x5dd` (`KeStartThread` 직전), `+0x5a1` (`KeReadyThread` 직전), `KeReadyThread` 뒤에서 `dt nt!_KTHREAD @rcx State`.
- **알림 문턱 값**: test.exe 의 `프로세스+0x5fc` bit0, `프로세스+0x640`.
- **스레드 락 풀기**: `+0x499` 에서 `poi(@rax)` (풀기 전 락 값).
- 추천 멈춤점: 모두 부모 cmd.exe 로 `/p` 필터 (run3 방식). `PspInsertThread` 는 다른 스레드 생성 때도 불리니 입구에서 `rdx`(새 EPROCESS)의 이미지 이름으로 test.exe 를 거른다.
  - `nt!PspInsertThread`, `+0x2e0`, `+0x5dd`, `+0x5a1`, `+0x499`, `+0x802`
  - `nt!PspCallProcessNotifyRoutines+0x14d`, `+0x31c`, `+0xbe`, `+0x1cf`
- 정적으로 더 볼 것
  - 첫 스레드를 **누가·언제 풀어 주나** (멈춘 채였다면): 여전히 `NtCreateUserProcess` 밖 → Q5.
  - 멈춘 스레드가 `KeReadyThread` 뒤 실제로 서는 방법 (`KeSuspendThread` 안쪽).

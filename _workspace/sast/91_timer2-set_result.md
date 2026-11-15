# 91 timer2-set — 정적 분석 결과

- 요청서: `_workspace/sast/91_timer2-set.md` (Q176a = Q176 ②)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`, pdb guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` = 기준과 같음), ntdll (pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1`)
- 새로 analyze 한 것: 없음 (둘 다 이미 분석됨, `.gpr` + `_index.tsv` + `SetPdb:` 줄 있음). bcrypt 쪽 파일·프로젝트는 안 건드림.
- 뽑은 함수 (로그 `ms/ghidra/ntoskrnl.export.sast91{a,b,c}.stdout`, `ms/ghidra/ntdll.export.sast91a.stdout`):
  - a: ntoskrnl `^(NtSetTimer2|ExpSetTimer2|KeSetTimer2|NtCreateTimer2)$` depth 1 (32개; 이 중 `KiRequestTimer2Expiration`, `KiAcquireTimer2LockUnlessDisabled`, `KiAcquireTimer2CollectionLockIfInserted`, `ExpExTimerAttributesAreValid`, `ExpCheckIRTimerAccess`, `KeInitializeIRTimer` 를 읽음)
  - b: ntoskrnl `^(KeInitializeTimer2|KiExpireTimer2)$` depth 0
  - c: ntoskrnl `^(KiInitializeTimer2|KiSetDpcRequestFlag)$` depth 0 — **요청서 밖, 넓힌 이유**: `KeInitializeTimer2` 는 19 B 껍데기라 형 바이트를 쓰는 줄이 한 단계 아래 `KiInitializeTimer2` 에 있음 (질문 3). `KiSetDpcRequestFlag` 는 `KiRequestTimer2Expiration` 이 부르는 "이미 요청했나" 판정 (질문 1).
  - ntdll `^TppInitializeTimerSubQueue$` depth 0 (이미 있던 파일을 다시 뽑음)
  - 이미 있던 `KiRetireDpcList.asm`·`KiTimer2Expiration.asm` 은 호출 자리만 읽음 (본문 새로 안 읽음).
- 버전 확인 (알려진 사실, `_calls.tsv`): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치, ntdll `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` 일치. → 스크립트 오프셋·바이너리 믿어도 됨.
- 스크립트 문제: 없음.

표기: 모두 `[정적]` (코드에 쓰인 것). "실행 때 이렇게 간다" 는 아니다. `[추측]` 은 따로 표시. 오프셋은 `.asm` 의 `+0x..` (호출은 call 자리/ret 자리, 점프 목적지는 목적지 − 함수 시작).

## 한눈에 (한 줄 답)

1. **지난 시각 / 0 이면 `KeSetTimer2` 가 `KiExpireTimer2` 를 직접 부르지 않는다.** 대신 `+0x6bd/+0x6c2 KiRequestTimer2Expiration` 을 불러서 시계 소유 CPU 에 "타이머 DPC 요청" (flag 8) 을 건다 → 다음 시계 틱을 기다리지 않고 IRQL 이 내려가는 즉시 `KiRetireDpcList → KiTimer2Expiration → KiExpireTimer2` 가 돈다. 단 **그 DPC 가 `NtSetTimer2` 를 부른 스레드 위에서 도는지는 정적으로 못 정함** (현재 CPU == 시계 소유 CPU 인지에 달림).
2. 0 (`DueTime=0`) 은 따로 안 다룬다. 부호만 본다 (`>0` 절대, `≤0` 상대). 상대 0 = "지금". (`DueTime` **포인터** 가 NULL 이면 `NtSetTimer2` 가 `0xC00000F0`.)
3. 주기 = 3번째 인자 (포인터 → int64, 100ns). `Parameters` 구조체에는 주기 칸이 없다 (Version / Reserved / NoWakeTolerance 만). 다시 걸기 = `KeSetTimer2` 가 먼저 앞 것을 뺀다 (`+0xfe~+0x113`).
4. **동기/알림 = `NtCreateTimer2` 4번째 인자 `Attributes` 의 bit 31 (`0x80000000`)**: 서 있으면 **0x18 알림**, 아니면 **0x19 동기**. 스레드 풀 (`TppInitializeTimerSubQueue`) 은 `Attributes = 8` (NoWake 만, bit 31 없음) → **동기 0x19**. DesiredAccess = `0x100002`.

---

## 질문별 답 [정적]

### 질문 1. 이미 지난 시각이면 그 자리에서 만기 처리하나

**`NtSetTimer2` 인자 꼴** (시스템 호출 `NtSetTimer2(TimerHandle, DueTime*, Period*, Parameters*)`; 이름은 공개 헤더 관례, 칸 뜻은 `.asm` 근거):

| # | 레지스터 | 뜻 | 근거 |
|---|---|---|---|
| 1 | RCX | 타이머 핸들. 접근 `0x2` (TIMER_MODIFY_STATE) 로, 형 `ExpIRTimerObjectType` 으로 참조 | `ExpSetTimer2 +0xd8` (R8=형), `+0xf3 MOV EDX,2`, `+0xfb/+0x100 ObReferenceObjectByHandle` |
| 2 | RDX | `DueTime*` (int64 값). **NULL 이면 입구에서 `0xC00000F0`** | `NtSetTimer2 +0x4 TEST RDX` · `+0x7 JZ → +0x14 MOV EAX,0xc00000f0` |
| 3 | R8 | `Period*` (NULL 이면 주기 0 = 한 번) | `ExpSetTimer2 +0x79 TEST R8,R8` / `+0xab~+0xb9` 읽음 |
| 4 | R9 | `Parameters*` (16 B, NULL 가능) | `ExpSetTimer2 +0x7e~+0x9f` (16 B 복사) |

**차례 표** (`NtSetTimer2` 한 번이 가는 길 — 정적으로 코드에 쓰인 순서):

| 순서 | 함수 + 오프셋 | 하는 일 | 갈림 조건 |
|---|---|---|---|
| 1 | `NtSetTimer2 +0x4~+0x7` | DueTime 포인터 NULL 검사 | NULL → `+0x14` 로 `0xC00000F0` 돌려줌 |
| 2 | `NtSetTimer2 +0x9/+0xe` | `ExpSetTimer2` 호출 | |
| 3 | `ExpSetTimer2 +0x41~+0x51` | 호출한 스레드의 `PreviousMode` (`GS:[0x188]` 스레드 +0x232) 확인 | 0 (커널) 이면 `+0xc7` 로 복사 건너뜀 |
| 4 | `ExpSetTimer2 +0x53~+0xa9` | 사용자 포인터를 `0x7fffffff0000` 으로 눌러서 읽음: DueTime 8 B, Period 8 B, Parameters 16 B 를 커널 지역 변수로 복사 | Period 포인터 NULL → 안 읽음 (`+0x7c`), Parameters NULL → 안 읽음 (`+0x86`) |
| 5 | `ExpSetTimer2 +0xfb/+0x100` | `ObReferenceObjectByHandle` | 음수 (`+0x102 JS → +0x191`) 면 그 값 그대로 반환 |
| 6 | `ExpSetTimer2 +0x108~+0x13a` | Period/Parameters 검사 (질문 2 표) | 실패 → `+0x182` 로 가서 참조 풀고 오류 반환 |
| 7 | `ExpSetTimer2 +0x155~+0x16f` | 객체 `+0x90` ← DueTime 값, `+0x98` ← NoWakeTolerance (없으면 0) 복사 | |
| 8 | `ExpSetTimer2 +0x17d/+0x182` | **`KeSetTimer2(타이머, DueTime 값, Period 값, Parameters 포인터)`** | (DueTime 포인터 NULL 이면 대신 `+0x1c3/+0x1c8 KeCancelTimer2(타이머, 0)` — `NtSetTimer2` 는 입구에서 막으므로 `NtCancelTimer2` 쪽 길; `ExpSetTimer2` 의 callers = `NtSetIRTimer NtSetTimer2 NtCancelTimer2`) |
| 9 | `KeSetTimer2 +0x25/+0x28 → +0x6df~+0x6ec` | Period ≠ 0 이면 `KeMinimumIncrement` 보다 작을 때 그 값으로 올림 (부호 있는 비교라 음수도 올라감) | Period 0 이면 건너뜀 |
| 10 | `KeSetTimer2 +0x2e~+0x4f` | 타이머 `+0x81` (TypeFlags) 읽어 둠. **IRQL 을 2 (DISPATCH_LEVEL) 로 올림** (`MOV CR8`, ECX=2) | |
| 11 | `KeSetTimer2 +0x63/+0x66` | `TEST RSI,RSI; JG` — **DueTime 부호로 절대/상대를 가름** | `> 0` → `+0x678` 절대 / `≤ 0` (0 포함) → `+0x6c` 상대 |
| 12 | `KeSetTimer2 +0x678~+0x6a5` (절대일 때) | `+0x678 R15B=1` (절대 표시), 시스템 시간을 읽어 (`+0x685` 주소 `0xFFFFF78000000014` = SystemTime, HighResolution 이면 `+0x7b1/+0x7b6 RtlGetSystemTimePrecise`) **`DueTime > SystemTime` 이면 `상대 = SystemTime − DueTime` (음수), 아니면 0** (`+0x698 SUB`, `+0x69b CMP`, `+0x69e CMOVG`, `+0x6a2 MOV RSI,RAX`) 로 바꿔 `+0x6c` 로 | |
| 13 | `KeSetTimer2 +0x6c~+0x9c` | **상대 값을 "지금 (인터럽트 시간)" 에 더함**: 지금 = `0xFFFFF78000000008` (InterruptTime, `+0x6c`), HighResolution 이면 `+0x24c/+0x251 RtlGetInterruptTimePrecise`. `+0x86 SUB RDI,RSI` (RSI ≤ 0 이라 더해짐) | 넘침 (`+0x8c JC`, `+0x96 JZ -1`) → `+0x23b` 에서 `0xFFFFFFFFFFFFFFFE` (= 영영 안 옴) |
| 14 | `KeSetTimer2 +0x9f~+0xcc` | 허용 지연: Parameters 있고 타이머 형 바이트 (`+0x82`) ≠ `0x17` 이면 `만기 + NoWakeTolerance` (−1 이면 `-1` = 끝 없음) 를 "늦은 만기" 로. 아니면 늦은 만기 = 만기 | |
| 15 | `KeSetTimer2 +0xee/+0xf3` | `KiAcquireTimer2LockUnlessDisabled` — 타이머 잠금 (`[타이머] bit 7`) 잡음; `[타이머+1]` 의 `0x20` (Timer2Disabled) 켜져 있으면 TRUE | TRUE → `+0xf5 JNZ +0x4b6` → 아무것도 안 하고 FALSE 반환 |
| 16 | `KeSetTimer2 +0xfe/+0x103 → +0x10e/+0x113` | **이미 걸려 있으면 (Timer2Inserted `[타이머+1]` bit 0) 앞 것 뺌**: `KiAcquireTimer2CollectionLockIfInserted` → `KiRemoveTimer2` | 안 걸려 있으면 `+0x105 JZ +0x311` (만기 처리 중인 상태 확인, 아래) |
| 17 | `KeSetTimer2 +0x130~+0x147` | **칸 쓰기**: `+0x4` SignalState ← 0, `+0x48` ← 만기, `+0x50` ← 늦은 만기, `+0x58` ← Period, `+0x80` ← 절대 표시 (R15B) | |
| 18 | `KeSetTimer2 +0x1b2~+0x596` | 두 줄 (레드블랙 트리) 에 넣음: 첫째 줄 (노드 `+0x18`, 열쇠 `+0x48`, `+0x571/+0x576 RtlRbInsertNodeEx`), 둘째 줄 (노드 `+0x30`, 열쇠 `+0x50`, `+0x365/+0x36a RtlRbInsertNodeEx`). 형 바이트 `+0x82 == 0x17` 이면 첫째 줄 건너뜀 (`+0x1b5 JZ +0x264`) | 줄의 맨 앞 (가장 이른) 이 되면 `ESI`/`R12D` = 1 (`+0x593`, `+0x383`) |
| 19 | `KeSetTimer2 +0x38a~+0x392` | `OR ESI,R12D; TEST ESI,ESI; JNZ +0x4bf` — **머리가 됐을 때만** 아래 20번으로 | 머리 아님 → `+0x398` (요청 안 함) |
| 20 | `KeSetTimer2 +0x4bf~+0x4ec` | `KiNextTimer2DueTime` 이 내 만기보다 크면 내 만기로 낮춤 (`+0x4c3 CMP`, `+0x4cc MOV`). 그 다음 **`+0x4d8` 에서 `0xFFFFF78000000008` (InterruptTime) 을 다시 읽고 `+0x4e2 CMP [타이머+0x48],RAX` → `+0x4e6 JA +0x398`** (만기 > 지금이면 건너뜀). **만기 ≤ 지금이면 `+0x4ec MOVZX R14D,R13B` (R14B=1)** | 만기 > 지금 → R14B=0 |
| 21 | `KeSetTimer2 +0x3c6~+0x3f4` | 타이머 머리 상태 바이트에 "걸림" (`+1`) 을 CMPXCHG 로 써 넣고 잠금 풂 (`+0x3c8~+0x3f4`, 비트 `0xfffff07f` 지움 + `EBP<<8`) | |
| 22 | `KeSetTimer2 +0x421~+0x433` | `KiForceIdleDisabled == 0` 이고 `KiForceIdleState == 4` 이면 `+0x47c` 로 (요청 안 함) | 예외 갈래 |
| 23 | `KeSetTimer2 +0x435/+0x438 → +0x6bd/+0x6c2` | **R14B ≠ 0 이면 `KiRequestTimer2Expiration()` 호출**, 그리고 `+0x47c` 로 | R14B = 0 → 24번 |
| 24 | `KeSetTimer2 +0x43e~+0x7ac` | (R14B = 0 일 때) HighResolution (`[RSP+0x80]` bit 2) 이고 만기 < `지금 + KeMaximumIncrement` 이고 (`KiClockOwnerOneShotRequest == 0` 이거나 그보다 이르고 `KeMinimumIncrement` 보다 크게 이름) → `+0x7a7/+0x7ac KiSendClockInterruptToTargetProcessor(KiClockTimerOwner)` (시계 인터럽트 앞당김) | HighResolution 아니면 안 함 |
| 25 | `KeSetTimer2 +0x47c~+0x4b4` | IRQL 을 원래대로 내림 (`+0x49c MOV CR8`), 반환 값 (AL = 앞 것이 걸려 있었나) | `ExpSetTimer2` 는 AL 을 안 읽음 (`+0x182` 뒤 `+0x18f MOV EAX,EDI`) |

**`KiRequestTimer2Expiration` (0x26c778, 정확히 한 단계 아래 갈림)**:
- `+0xa` 시계 소유 CPU 번호 (`KiClockTimerOwner`) → `+0x1c` 그 CPU 의 PRCB (`KiProcessorBlock`) → `+0x20` PRCB `+0x38bc` → `+0x27/+0x2c KiSetDpcRequestFlag(…, 8)` (`+0x17 MOV EDX,8`). 옛 값이 `& 0xa9` 이면 이미 요청이 있는 것이라 `+0x2e JNZ +0x47` 로 끝.
- `+0x30/+0x39 CMP` 현재 PRCB (`GS:[0x20]`) 와 소유 PRCB 가 **다르면** `+0x3e~+0x47 KiSendSoftwareInterrupt(소유 CPU, 2)` (다른 CPU 에 DISPATCH 소프트 인터럽트).
- **같으면** `+0x53 CMP byte [PRCB+0x20],0` (NestingLevel): 0 이 아니면 `+0x59 [PRCB+6]=1` (InterruptRequest 표시) 로 끝, **0 이면 `+0xcb MOV EDX,0x2f` … `+0xdd/+0xe2 HalpInterruptSendIpi`** (자기 자신에게 IPI 한 번; 벡터 0x2f 가 DISPATCH 단계 `[추측]`). IRQL 이 2 인 동안은 보류돼 있다가 `KeSetTimer2 +0x49c MOV CR8` 로 내려가는 순간 배달된다 `[추측]` (인터럽트 반환 주소 = `KeSetTimer2 +0x4a0` 근처).
- 받는 쪽: `KiRetireDpcList +0x406 XCHG [PRCB+0x38bc]` → `+0x40d TEST DIL,0x8` → `+0x411 JNZ +0xbca` (= 0x14022447a − 0x1402238b0) → … `+0xca4/+0xca9 KiTimerExpiration` → `+0xca9 CMP byte [PRCB+0x21],0` (ClockOwner) → **`+0xcc6/+0xccb KiTimer2Expiration`**. `KiTimer2Expiration +0x4b CMP now,KiNextTimer2DueTime; +0x4e JC +0x16e` (아직이면 바로 끝), `+0x290 CMP now,[타이머+0x48]` 로 이른 순으로 꺼냄, `+0xd7/+0xdc KiExpireTimer2` (sast/89). 사이의 갈림 (`KiRetireDpcList +0xc51`, `+0xc5d`, `+0xc76`) 은 안 따라감 `[공백]`.

**"지난 시각 / 0 / 미래" 세 경우의 길**:

| 경우 | KeSetTimer2 안에서 | 만기 (`+0x48`) | 즉시 요청? | 만기 처리가 일어나는 곳 |
|---|---|---|---|---|
| **지난 절대 시각** (`DueTime > 0` 이고 `≤ SystemTime`) | `+0x66 JG` → `+0x678`; `+0x69e CMOVG` 안 탐 → 상대 **0** | = 지금 (InterruptTime) | **예** — 머리가 되면 (`+0x392`) `+0x4e6 JA` 안 뜀 → R14B=1 → `+0x6bd/+0x6c2 KiRequestTimer2Expiration` | DPC (`KiRetireDpcList +0xcc6` → `KiTimer2Expiration +0xd7` → `KiExpireTimer2`), 틱 안 기다림 |
| **`DueTime = 0`** (또는 상대 0) | `+0x66 JG` 안 탐 → `+0x6c` 상대. **0 을 따로 다루는 줄 없음** | = 지금 | **예** (위와 같음) | 위와 같음 |
| **미래 상대** (`DueTime < 0`) | `+0x6c~+0x9c` 지금 + \|DueTime\| | 지금보다 큼 | **아니오** — `+0x4e6 JA +0x398` → R14B=0. (`HighResolution` 이면 `+0x43e~+0x7ac` 로 시계 인터럽트만 앞당김) | 시계 인터럽트 쪽 `KiRetireDpcList` → `KiTimer2Expiration` 이 `지금 ≥ 만기` 일 때 (`+0x4b`, `+0x290`) — 다음 틱 이후 |
| **미래 절대** | `+0x678~+0x6a5` 로 상대로 바꾼 뒤 위와 같음 | 지금보다 큼 | 아니오 | 위와 같음 |

작은 주의 (정적으로 읽힌 것):
- "지금" 은 **KUSER_SHARED_DATA 의 InterruptTime (`+0x8`, 틱마다 갱신)**. 상대 `-1` (100 ns) 같은 값은 만기 = 지금 + 1 > 지금 → **즉시 요청 아님** → 다음 틱. 즉 "아주 작은 상대 값" 은 "0" 과 길이 다르다.
- 즉시 요청 판정은 **타이머가 줄 맨 앞 (머리) 이 됐을 때만** (`+0x392`). 머리가 아니면 (앞에 더 이른 타이머가 있음) 요청 안 함 (그 앞 타이머가 이미 만기·요청 대상이라서).
- HighResolution 타이머는 만기를 정밀 시계 (`RtlGetInterruptTimePrecise`) 로 계산하지만 `+0x4d8` 비교는 공유 InterruptTime 이라, "상대 0" 이어도 R14B=0 이 될 수 있고, 그때는 `+0x43e~+0x7ac` 가 시계 인터럽트를 앞당긴다 (`KiSendClockInterruptToTargetProcessor`).
- 앞 것이 "만기 처리 중" 이던 타이머 (`[타이머+1]` 하위 4비트 ≠ 0 인데 걸려 있지 않음): `+0x105 JZ +0x311` → `+0x31f EBP=8` (SetPending) 로 걸고 줄에는 안 넣음 (`+0x16e CMP EBP,R13D; +0x171 JNZ +0x72f` → `XOR R14B,R14B`, `+0x72f XOR R14B,R14B; +0x732 JMP +0x3ac` 로 줄 넣기·요청을 건너뜀); 만기 쪽 (`KiTimer2Expiration +0x382 TEST AL,8` → `+0x399 KiInsertTimer2`) 이 나중에 넣음. 이 경우도 신호 상태는 `+0x130` 에서 0 으로 지워짐.

**바로 만기라면: 신호 상태 · wait packet 배달이 `NtSetTimer2` 를 부른 스레드 안에서 일어나나 (부르는 줄만)**:
- `NtSetTimer2` · `ExpSetTimer2` · `KeSetTimer2` 의 호출 목록 (`_calls.tsv`) 어디에도 **`KiTimer2Expiration`, `KiExpireTimer2`, `KiInsertQueueInternal` 을 부르는 줄이 없다.** `KeSetTimer2` 가 부르는 것은 `KiRequestTimer2Expiration` (+0x6bd/+0x6c2) 뿐.
- 신호 (`SignalState`) 와 패킷 배달은 `KiRetireDpcList +0xcc6/+0xccb → KiTimer2Expiration +0xd7/+0xdc → KiExpireTimer2 → +0x3a8/+0x3ad (동기) · +0x702/+0x707 (알림) KiInsertQueueInternal` (sast/89) 쪽, 즉 **DPC 몫**이다. `NtSetTimer2` 의 호출 사슬 안이 아니다.
- 그 DPC 가 같은 스레드 위에서 (IRQL 내림 직후) 도는지는 **현재 CPU 가 시계 소유 CPU 인지** (`KiRequestTimer2Expiration +0x39 CMP`) 에 달림 → 정적으로 못 정함 (동적 몫). 다른 CPU 면 `KiSendSoftwareInterrupt` 로 그쪽에서 돈다.
- 그러므로 `NtSetTimer2` 가 `STATUS_SUCCESS` 를 돌려주는 시점과 완료 포트에 패킷이 들어가는 시점의 앞뒤는 정적으로 정해지지 않는다 (같은 CPU 면 거의 그 직전, 다른 CPU 면 경쟁).

### 질문 2. 주기 (Period) 와 다시 걸기

**주기 칸**: 구조체 칸이 아니라 **3번째 인자 `Period*`** (int64 하나). `ExpSetTimer2` 가 값으로 읽어 `KeSetTimer2` 의 R8 로 넘기고 (`+0x108 MOV R8,[RSP+0x40]` → `+0x17d`), `KeSetTimer2 +0x143 MOV [타이머+0x58],R14` (KTIMER2 `Period`). 단위는 100 ns 로 보임: 만기 쪽이 `Period` 를 InterruptTime 에 그대로 더한다 (`KiExpireTimer2 +0x2cf MOV R11,[RBX+0x58]; +0x2d6 ADD R11,R10`; 주기 갈래는 `+0xad/+0xb1 JNZ +0x2b4`, 다시 걸기 `+0x353/+0x358 KiInsertTimer2WithCollectionLockHeld`) `[참고, 요청서 범위 밖 한 줄]`. 거기서 다음 만기 = (만기 처리 때 넘어온 "지금") + Period 로 읽힘 (이전 만기 + Period 가 아님) — 확인 필요.

**`Parameters` (`_EXT_SET_PARAMETERS_V0`, 0x10 B, PDB 타입)**:

| 오프셋 | 이름 | 뜻 (코드에서 쓰는 방식) | 근거 |
|---|---|---|---|
| +0x0 | Version | **0 이어야 함**, 아니면 `0xC00000F1` | `ExpSetTimer2 +0x125 CMP dword [R9],0; +0x129 JNZ +0x1ca` |
| +0x4 | Reserved | 안 읽음 | |
| +0x8 | NoWakeTolerance | int64, **−1 이상**이어야 함 (`< −1` 이면 `0xC00000F1`). −1 = 허용 끝 없음 (`KeSetTimer2 +0xb1 CMP RAX,-1; +0xb5 JZ +0x538 → RSI=-1`), 그 외 = 늦은 만기 = 만기 + 값 (`+0xbb LEA RSI,[RAX+RDI]`, 넘침 → −2). **형 바이트 `+0x82 == 0x17` 인 타이머 (플래그 없는 보통 타이머)는 이 값을 안 씀** (`+0xa4 CMP byte [RBX+0x82],0x17; +0xab JZ +0xd2`) | `ExpSetTimer2 +0x12f/+0x134`; 객체 `+0x98` 에도 복사 (`+0x16f`) |

**이미 걸린 타이머를 다시 걸 때**: `KeSetTimer2 +0xfe/+0x103 KiAcquireTimer2CollectionLockIfInserted` (`[타이머+1]` bit 0 = Timer2Inserted 이면 컬렉션 잠금 잡고 TRUE) → `+0x10e/+0x113 KiRemoveTimer2` → `+0x120 AND.LOCK [KiTimer2CollectionLock]` 잠금 풂 → `+0x128 [RSP+0x88] = 1` (반환 값 TRUE = 앞 것이 걸려 있었음) → 같은 호출 안에서 곧바로 새 값으로 다시 넣음 (`+0x130~`). **신호 상태는 걸 때마다 0 으로 지움** (`+0x130`). `ExpSetTimer2` 는 반환 값을 안 쓰므로 사용자에게는 안 보임 (`NtSetTimer2` 는 "앞 것이 있었나" 를 돌려주지 않음).

**status 표 (`NtSetTimer2`)** (상수 이름은 NTSTATUS 표 기준 `[추측]`, 값은 `.asm`):

| 상황 | status | 근거 |
|---|---|---|
| 성공 (`KeSetTimer2` 를 부름; 반환 bool 은 버림. "disabled 타이머" 로 아무것도 안 해도 0) | `0` | `ExpSetTimer2 +0x26 XOR EDI,EDI`, `+0x182`~`+0x18f MOV EAX,EDI` |
| DueTime 포인터 NULL | `0xC00000F0` (INVALID_PARAMETER_2) | `NtSetTimer2 +0x7 JZ → +0x14` |
| 핸들 오류 (잘못된 핸들 / 형 아님 / 접근 모자람) | `ObReferenceObjectByHandle` 가 돌려준 값 그대로 (형은 `ExpIRTimerObjectType` 이라 옛 `NtCreateTimer` 핸들이면 형 불일치 `[추측: 값 0xC0000024]`, 접근 모자람 `[추측: 0xC0000022]`) | `ExpSetTimer2 +0x100/+0x102 JS +0x191` |
| Period ≠ 0 이고 타이머가 IdleResilient (`[+0x81]` bit 1) | `0xC000000D` (INVALID_PARAMETER) | `ExpSetTimer2 +0x115 JNZ +0x1ad; +0x1ad TEST byte [RBX+0x81],2; +0x1ba MOV EDI,0xc000000d` |
| Parameters.Version ≠ 0, 또는 NoWakeTolerance < −1 | `0xC00000F1` (INVALID_PARAMETER_3 이름, 4번째 인자 오류인데 이 값이 나옴) | `ExpSetTimer2 +0x129`, `+0x134 JL → +0x1ca MOV EDI,0xc00000f1` |
| 사용자 포인터를 읽다 틀림 | `.asm` 에 예외 처리기 줄이 안 보임 `[공백]` (SEH 로 status 를 바꾸는지 모름) | `ExpSetTimer2 +0x6a`, `+0xb6` 이 읽는 줄 |

### 질문 3. `NtCreateTimer2` 의 동기/알림 인자

**`NtCreateTimer2` 인자** (공개 헤더 관례 이름; 칸 뜻은 `.asm`):

| # | 레지스터 | 이름 | 뜻 | 근거 |
|---|---|---|---|---|
| 1 | RCX → R14 | TimerHandle (출력) | 사용자 모드면 쓰기 가능 검사 (`MOV RAX,[RCX]; MOV [RCX],RAX`, `+0x84/+0x87`), 끝에서 `+0x156 MOV [R14],RAX` | |
| 2 | RDX → R10 | Reserved1* | NULL 이면 안 봄. **NULL 이 아니려면 `Attributes & 2` 가 켜져 있어야 함**, 아니면 `0xC00000F0`. 켜져 있으면 4 B 를 읽어 `ExpCheckIRTimerAccess(값, PreviousMode)` (IdleResilient 타이머의 "종류" 칸) | `+0x4c JNZ +0x1be; +0x1c0 JNZ; +0x1c6 MOV EAX,0xc00000f0`; `+0x96~+0xa6` 읽음; `+0x195/+0x19a ExpCheckIRTimerAccess` |
| 3 | R8 → R11 | Reserved2 | **NULL 이어야 함**, 아니면 `0xC00000F1` | `+0x3b TEST R11; +0x3e JNZ +0x1cd` |
| 4 | R9D → EDI | **Attributes** | 아래 표 | `+0x2e ExpExTimerAttributesAreValid`, `+0x107 MOV R9D,EDI; +0x10a KeInitializeTimer2` |
| 5 | 스택 `[RSP+0x28]` (진입 시) | DesiredAccess | `ObInsertObjectEx` 의 접근 (`+0x135 MOV R8D,[RSP+0xa0]`, `+0x142/+0x147`) | |

**형 (0x18 / 0x19) 을 고르는 줄**: `NtCreateTimer2 +0x107 MOV R9D,EDI` → `+0x10a/+0x10f KeInitializeTimer2(객체, 0, 0, Attributes)` → `KeInitializeTimer2 +0x9/+0xe KiInitializeTimer2` → **`KiInitializeTimer2 +0xd TEST R9D,R9D` · `+0x10 SETNS AL` · `+0x13 ADD AL,0x18` · `+0x15 MOV [RCX],AL`**:

| Attributes bit 31 (`0x80000000`) | 객체 형 바이트 (`[타이머+0]`) | 뜻 |
|---|---|---|
| **서 있음** | **`0x18`** | 알림 (신호 그대로, `KiExpireTimer2 +0x702/+0x707`) |
| 없음 | **`0x19`** | 동기 (자동 리셋, `KiExpireTimer2 +0x3ad SUB [SignalState],1`) |

(`KeInitializeTimer2 +0x4 AND word [RCX+2],0` 이 형 바로 뒤 2 B 를 0 으로 지움. 같은 줄 `KeInitializeIRTimer +0x21 OR R9D,2` 가 IdleResilient 타이머도 같은 `KiInitializeTimer2` 로 보냄 → bit 31 규칙은 둘 다 같음.)

**다른 Attributes 비트** (`ExpExTimerAttributesAreValid` 0x3afc7c: 하위 `(A & 0x8000000e) == A` 아니면 `0xC00000F2` (`NtCreateTimer2 +0x35 JZ → +0x1b7`); 이어서 `A & 0xe` 가 표 `0x394d0` 의 여섯 값 `{0, 2, 4, 8, 6, 0xc}` 중 하나여야 함):

| 비트 | 값 | 이름 (PDB `_KTIMER2.TypeFlags`) | 효과 (코드에서) |
|---|---|---|---|
| 31 | `0x80000000` | (PDB 이름 없음. 공개 헤더의 `EX_TIMER_NOTIFICATION` 은 `[추측]`) | 형 `0x18` (위 표) |
| 1 | `0x2` | IdleResilient | `NtCreateTimer2 +0xaf JNZ +0x18e → +0x195/+0x19a ExpCheckIRTimerAccess` (Reserved1 4 B 읽은 뒤), `+0xf7~+0x101 JNZ +0x1a4 → +0x1ad/+0x1b2 KeInitializeIRTimer` (`KeInitializeTimer2` 대신), 이 타이머는 **Period ≠ 0 불가** (`ExpSetTimer2 +0x1ad`). `KeInitializeIRTimer +0x25/+0x3d`: `ExAusterityResilientTimersEnabled` 이고 종류 표가 1 이면 `0x20` (AusterityResilient) 도 켬 |
| 2 | `0x4` | HighResolution | `KeSetTimer2` 가 정밀 시계 사용 (`+0x76/+0x67b`), `KiHrTimerActiveCount` 올림 (`+0x4f5`), 시계 인터럽트 앞당김 (`+0x43e~+0x7ac`) |
| 3 | `0x8` | NoWake | `[타이머+0x81]` 에 들어감 (`KiInitializeTimer2 +0xa2 AND R9B,0x2e; +0xa8 MOV [R11+0x81],R9B`). 줄 번호 (`+0x82/+0x83`) 는 표 `KiTimer2Combinations` (rva 0x11958, 3 B × 9: `flags, +0x82, +0x83`) 에서 고름. `0x8` → `{0x20, 0x24}`; 플래그 0 → `{0x17, 0x04}` |
| 4,5 | `0x10`, `0x20` | PseudoHighRes, AusterityResilient | **인자로는 못 줌** (`0x8000000e` 밖). 안에서 자동: 플래그 0 이면 `PsTimerResolutionActive(프로세스)` 로 `0x10` (`KiInitializeTimer2 +0xe7~+0x113`) |
| (6, 0xa, 0xe 등) | | | `{0,2,4,8,6,0xc}` 에 없는 조합 (예: `0xa` = IdleResilient+NoWake, `0xe`) 은 `0xC00000F2` |

**`TppInitializeTimerSubQueue` 두 값** (ntdll `TppInitializeTimerSubQueue`, `.asm`; 호출은 `+0x4d/+0x52 NtCreateTimer2`):

| 줄 | 무엇 | 뜻 |
|---|---|---|
| `+0x37 MOV R9D,0x8` | **4번째 인자 `Attributes = 8`** | bit 3 = **NoWake** 만. bit 31 없음 → **동기 (형 0x19)**. bit 1 없음 → 일반 (비 IdleResilient) 타이머 → `KeInitializeTimer2` 길 (Period 허용). |
| `+0x44 MOV dword [RAX-0x38],0x100002` | **5번째 인자 `DesiredAccess = 0x100002`** (스택 `[RSP+0x20]`, `RAX` = 진입 시 RSP, 푸시 3개 + `SUB 0x40` 뒤라 `RAX-0x38 = RSP+0x20`) | `0x100000` (SYNCHRONIZE) + `0x2` (TIMER_MODIFY_STATE). 이 핸들이 `NtSetTimer2` 의 접근 `0x2` 와 대기·포트 맡김에 쓰임. |
| (나머지) | `+0x33 XOR EDX,EDX` (Reserved1=0), `+0x41 XOR R8D,R8D` (Reserved2=0), RCX = RBX = 서브큐 `+0x18` (핸들 칸) | |

→ 스레드 풀 타이머 = **동기 (0x19), NoWake, 접근 0x100002.** 뒤이어 `+0x65/+0x6a NtCreateWaitCompletionPacket`, `+0xa0/+0xa5 NtAssociateWaitCompletionPacket` (핸들 `[RBX]` = 타이머 맡김) 으로 포트에 붙임 (sast/89 와 맞음).

`NtCreateTimer2` 상태표: 성공 `0`; 속성 틀림 `0xC00000F2`; Reserved2 ≠ NULL `0xC00000F1`; Reserved1 ≠ NULL 인데 `Attributes & 2` 없음 `0xC00000F0`; IdleResilient 접근 검사 실패 `ExpCheckIRTimerAccess` (`0xC0000022` 갈래 `+0x88`, `0xC000000D` 갈래 `+0xb2`); 그 외 `ObCreateObjectEx` (`+0xe7/+0xec`) / `ObInsertObjectEx` (`+0x142/+0x147`) 값 그대로. 객체 본문 크기 0xa8 (`+0xd6 MOV [RSP+0x28],0xa8`), 객체 `+0x88` ← 0, `+0xa0` ← Attributes (`+0x10f`~`+0x117`).

### 질문 4. 지금 우리 (PE_emulator) 맞나·틀리나

(에뮬레이터 현재 코드는 이 요청 범위 밖이라 안 봄. 아래 "코드에 쓰인 값" 과 대조.)
- **지난 시각 / 0**: 코드는 "바로 요청" 쪽 → 에뮬레이터는 **지난 절대 시각과 `DueTime = 0` (상대 0) 을 다음 틱까지 안 기다리고 곧바로 만기 (신호 1 → 동기면 배달하며 0) 처리**하는 게 맞다. 단 "다음 틱 이후" 가 되는 경우: 미래 값 (심지어 상대 −1) 은 만기 > 지금이라 즉시 요청이 없다. 만기 처리 자리는 호출 사슬 안이 아니라 DPC (그래서 `NtSetTimer2` 가 돌아오기 직전/직후 어느 쪽이든 가능, 동적 몫).
- **Go·스레드 풀 타이머의 형**: **스레드 풀 = 동기 (0x19)** (`Attributes = 8`). **Go 는 코드로 못 정함** — 형은 호출자가 주는 `Attributes` bit 31 으로만 정해진다 (없으면 동기가 기본). Go 가 `NtCreateTimer2` 를 직접 부르는지, kernelbase 를 거치는지, 어떤 `Attributes` 를 주는지는 이 코드 범위 밖 (kernelbase PDB 없음). → 동적 확인 필요.

---

## 동적으로 확인해야 할 것 (VM 몫, 실행 때만 정해짐)

`[추측]` 모음 — 정적으로 못 정한 것:
1. **즉시 만기가 `NtSetTimer2` 를 부른 스레드 위에서 도나**: 현재 CPU == `KiClockTimerOwner` 인지 (`KiRequestTimer2Expiration +0x39`). 같으면 자기 IPI 가 `KeSetTimer2 +0x49c MOV CR8` 에서 배달돼 `KiExpireTimer2` 의 콜스택 맨 아래에 `NtSetTimer2 → ExpSetTimer2 → KeSetTimer2+0x4a0` 근처가 보여야 함.
2. 지난 시각 / 0 일 때 `+0x4e6 JA` 가 실제로 안 뛰는지 (R14B=1, `KiRequestTimer2Expiration` 로 감), 미래 값일 때 뛰는지.
3. 틱 안 걸린 "아주 작은 상대 값" 이 정말 다음 틱까지 걸리는지 (타이머 해상도에 따라 몇 ms).
4. Go 가 `NtCreateTimer2` 에 주는 `Attributes` (`r9d` bit 31, bit 2 HighResolution) 와 DesiredAccess, 그리고 만든 직후 타이머 형 바이트.
5. 사용자 포인터가 틀렸을 때 status (SEH 가 있는지).
6. `KiForceIdleState == 4` 인 때 요청이 빠지는 것 (정상 부팅에선 거의 안 걸림).

추천 멈춤점 (모든 프로세스에 걸리므로 test.exe 조건 필요):
- `nt!NtSetTimer2` 진입: `poi(rdx)` (DueTime), `r8` (Period 포인터; 있으면 `poi(r8)`), `r9` (Parameters). 반환 `eax`.
- `nt!KeSetTimer2` 진입: `rcx` (타이머) · `rdx` (DueTime 값) · `r8` (Period) · `r9`; `dq @rcx L0x11` (KTIMER2 0x88 B: `+0x0` 형, `+0x81` 플래그).
- `nt!KeSetTimer2+0x4e6` (`JA`): 직전 `poi(@rbx+0x48)` (만기) 와 `poi(0xfffff78000000008)` (지금) 비교, 뛰었나 (`+0x398` 로 감) 안 뛰었나 (`+0x4ec`).
- `nt!KiRequestTimer2Expiration` 진입 (콜스택 ret `KeSetTimer2+0x6c2`) + `poi(nt!KiClockTimerOwner)` 와 현재 CPU 번호.
- `nt!KiExpireTimer2` 진입 (ret `KiTimer2Expiration+0xdc`): `kb` 로 스레드·콜스택 (`KeSetTimer2+0x4a0` 이 보이면 같은 스레드), `poi(@rcx+0x4)` 신호 상태.
- `nt!KiInitializeTimer2` 진입: `r9d` (Attributes), 반환 뒤 `poi(@r11)&0xff` (0x18/0x19), `poi(@r11+0x81)&0xff`.
- `nt!NtCreateTimer2` 진입: `r9d`, 스택 `dwo(@rsp+0x28)` (DesiredAccess), 콜스택 ret (`TppInitializeTimerSubQueue+0x52`, Go 면 어느 쪽).

## 다음 요청 후보

1. **kernelbase `CreateWaitableTimerExW` / `SetWaitableTimerEx`** 가 `NtCreateTimer2` / `NtSetTimer2` 로 어떤 `Attributes`·`DueTime`·`Period` 를 넘기나 (Go 의 길). kernelbase PDB 가 없다 (`ms/sym/kernelbase.dll.pdb` 없음) → 먼저 `fetch_pdb.py kernelbase.dll` 필요.
2. `NtCancelTimer2` · `KeCancelTimer2` (취소·"Disable" 와 맡긴 wait packet 의 신호 처리).
3. `KiExpireTimer2` 의 주기 갈래 (`+0x2b4`~`+0x358`) — 다음 만기를 "이전 만기 + Period" 로 하나 "지금 + Period" 로 하나 (질문 2 의 확인 필요 자리).
4. Q176b 나머지: `NtCreateWaitCompletionPacket`, `NtRemoveIoCompletionEx`, `ObGetAssociatedWaitObject`, 닫기 처리기.
5. (정적 쪽 보강) `KiSendSoftwareInterrupt`, `KiRetireDpcList +0xbca~+0xcc6` 사이 갈림 (타이머 DPC 가 정말 이 요청 한 번으로 도는 조건).

## 판정

**분석성공.** 질문 1~3 에 `.asm` 줄 근거로 답함. 즉시 만기 여부 ("요청만 하고 DPC 가 만기 처리") 와 `NtCreateTimer2` Attributes bit 31 → 형 0x18/0x19, 스레드 풀 `Attributes = 8` (동기, NoWake), `DesiredAccess = 0x100002` 는 확정. 못 정한 것은 (a) DPC 가 부른 스레드 위에서 도나 (CPU 에 따라, 동적), (b) Go 의 `Attributes` (호출자 쪽), (c) 사용자 포인터 오류 status.

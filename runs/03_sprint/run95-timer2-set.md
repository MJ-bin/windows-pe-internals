# 2026-11-15 run95 — Timer2 걸기 · 만들기 (`NtSetTimer2` → `ExpSetTimer2` → `KeSetTimer2`, `NtCreateTimer2` → `KiInitializeTimer2`) (Q176a = Q176 ②, run93 남은 정적 — 한 바퀴로 끝): **이미 지난 시각이나 `DueTime = 0` 으로 걸어도 `KeSetTimer2` 는 만기 함수 (`KiExpireTimer2`) 를 직접 부르지 않는다. 만기 시각을 "지금" 으로 잡아 줄에 넣고, 그 타이머가 줄 맨 앞 (머리) 이 됐으면 `+0x4e2 CMP [타이머+0x48], 지금` · `+0x4e6 JA` 를 안 뛰어 `R14B = 1` → `+0x6bd KiRequestTimer2Expiration` 로 시계 소유 CPU 에 "타이머 DPC 요청" (비트 8) 만 건다. 만기 처리 (신호 · wait packet 배달) 는 다음 시계 틱을 기다리지 않고 IRQL 이 내려가는 대로 DPC (`KiRetireDpcList +0xcc6 KiTimer2Expiration` → `+0xd7 KiExpireTimer2`) 가 한다 — `NtSetTimer2` 의 호출 사슬 안이 아니다. 0 은 따로 안 다루고 부호만 본다 (`> 0` 절대, `≤ 0` 상대). 미래 값은 상대 −1 (100 ns) 이라도 즉시 요청이 없다. 동기 / 알림은 `NtCreateTimer2` 의 4번째 인자 `Attributes` bit 31 하나로 갈린다 (`KiInitializeTimer2 +0xd TEST R9D,R9D` · `+0x10 SETNS` · `+0x13 ADD AL,0x18` → bit 31 있으면 `0x18` 알림, 없으면 `0x19` 동기). 스레드 풀 타이머 큐는 `Attributes = 8` (NoWake 만) → 동기 `0x19`, 접근 `0x100002`.** `NtSetTimer2 +0x9 ExpSetTimer2` → `+0xfb ObReferenceObjectByHandle` (접근 2, 형 `ExpIRTimerObjectType`) → `+0x17d KeSetTimer2(타이머, DueTime, Period, Parameters)` → `+0x63` 부호 → `+0x6c ~ +0x9c` 지금 (`InterruptTime`) + |상대| → `+0xfe/+0x10e` 앞 것 빼기 → `+0x130 ~ +0x147` 칸 쓰기 (신호 0) → 줄 넣기 → `+0x392` 머리? → `+0x4e6` 만기 ≤ 지금? → `+0x6bd KiRequestTimer2Expiration` → `+0x27 KiSetDpcRequestFlag(…, 8)`

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### Q176a 결론 (run95)

- **지난 시각 · 0 은 "곧바로 만기" 가 맞다 — 단 그 자리 (호출 안) 가 아니라 DPC 로** `[정적]`.
  - 지난 절대 시각 (`DueTime > 0` 이고 `≤ SystemTime`) → `+0x69e CMOVG` 안 탐 → 상대 0. `DueTime = 0` → `+0x66 JG` 안 탐 → 상대 0. 둘 다 만기 = 지금.
  - 머리가 되면 `+0x4e6 JA` 를 안 뛰어 `+0x6bd KiRequestTimer2Expiration` → 시계 소유 CPU 에 DPC 요청 → `KiRetireDpcList` → `KiTimer2Expiration` → `KiExpireTimer2` (신호 · 패킷 배달은 run93 "배달 길 표").
  - **다음 틱을 기다리지 않는다.** 요청은 IRQL 이 내려가는 순간 (`KeSetTimer2 +0x49c MOV CR8`) 이나 그 CPU 의 디스패치 인터럽트로 처리된다 `[추측: 인터럽트 배달 시점]`.
- **에뮬레이터 모양 제안** — 지난 시각 · 0 이면 `NtSetTimer2` 를 끝낸 **직후** (같은 스레드가 돌아오기 전이든 직후든) 만기 처리 (신호 1 → 맡긴 패킷 배달 → 동기면 신호 0) 를 한 번 돌리면 된다. 순서 (`STATUS_SUCCESS` 반환 vs 포트에 패킷 들어감) 는 진짜에서도 CPU 에 따라 다르다 (아래 동적 ①).
- **미래 값은 다음 틱 이후** `[정적]` — 상대 −1 (100 ns) 처럼 아주 작아도 만기 = 지금 + 1 > 지금 → 즉시 요청 없음. 시계 틱의 `KiTimer2Expiration +0x4b` 비교 (`지금 ≥ KiNextTimer2DueTime`) 를 기다린다. "0" 과 "아주 작은 음수" 는 길이 다르다.
- **동기 / 알림** `[정적]` — `NtCreateTimer2` 인자 4 (`Attributes`) **bit 31 (`0x80000000`) 있으면 알림 `0x18`, 없으면 동기 `0x19`**. 기본이 동기다. 스레드 풀 (`TppInitializeTimerSubQueue +0x37 R9D = 8`) = **동기**.
- **Go 타이머의 형은 코드로 못 정한다** — 형은 부르는 쪽이 주는 `Attributes` 로만 정해진다. Go 가 무엇을 주는지는 이번 범위 밖 (kernelbase · Go 런타임) → 동적 ④ / 정적 ⑥.
- **`NtSetTimer2` 는 "앞 것이 걸려 있었나" 를 돌려주지 않는다** — `KeSetTimer2` 의 반환 바이트를 `ExpSetTimer2` 가 버린다 (`+0x182` 뒤 `+0x18f MOV EAX,EDI`). 다시 걸기는 같은 호출 안에서 앞 것을 빼고 새로 넣으며, **걸 때마다 신호를 0 으로 지운다** (`KeSetTimer2 +0x130`).

### 지금 PE_emulator 맞나 · 틀리나 (요청서 "에뮬레이터가 모르는 것" 기준 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

| 갈래 | 진짜 (26100.9457) `[정적]` | 에뮬레이터가 할 것 |
|---|---|---|
| 지난 절대 시각 | 만기 = 지금 → 머리면 즉시 DPC 요청 (`+0x6bd`) | 곧바로 만기 처리 (다음 틱 기다리지 않음) |
| `DueTime = 0` | 상대 0 과 같음 (0 을 따로 다루는 줄 없음) | 위와 같음 |
| 미래 상대 · 미래 절대 | 줄에 넣고 끝 (HighResolution 이면 시계 인터럽트만 앞당김 `+0x7a7`) | 시각이 올 때까지 기다림 |
| `DueTime` 포인터 NULL | `0xC00000F0` (`NtSetTimer2 +0x7 → +0x14`) | 같은 값 |
| `Parameters.Version ≠ 0` · `NoWakeTolerance < −1` | `0xC00000F1` (`ExpSetTimer2 +0x129` · `+0x134` → `+0x1ca`) | 같은 값 (4번째 인자인데 이 값) |
| Period ≠ 0 인데 IdleResilient 타이머 | `0xC000000D` (`+0x1ad ~ +0x1ba`) | 같은 값 |
| 핸들 오류 | `ObReferenceObjectByHandle` 값 그대로 (형 `ExpIRTimerObjectType`, 접근 2) | 옛 `NtCreateTimer` 핸들이면 형 불일치 `[추측: 0xC0000024]` |
| 스레드 풀 타이머 형 | 동기 `0x19` (`Attributes = 8`) | 동기 — 만기 배달 뒤 신호 0 (`KiExpireTimer2 +0x3ad`) |
| Go 타이머 형 | 부르는 쪽 `Attributes` bit 31 | 모름 — 동적 ④ |

### 원문 질문이 어디서 답해졌나

| 요청서 질문 | 답한 곳 | 한 줄 |
|---|---|---|
| 1 지난 시각 · 0 · 그 자리 만기? | "차례 표" · "세 경우의 길" · `KiRequestTimer2Expiration` | 직접 안 부름. 머리면 DPC 요청 → DPC 가 만기. 0 은 부호로만 |
| 2 주기 · 다시 걸기 · status | "인자 칸 표" · "주기 · 다시 걸기" · "status 표" | 주기 = 3번째 인자 (100 ns). 앞 것 빼고 다시 넣음, 신호 0. status 5갈래 |
| 3 동기 / 알림 인자 · TP 두 값 | "`NtCreateTimer2` 인자 표" · "형 표" · "TP 두 값" | `Attributes` bit 31. TP = 8 (NoWake) → 동기, 접근 `0x100002` |
| 4 지금 우리 맞나 | 위 표 | 지난 · 0 = 곧바로 (DPC). TP = 동기. Go = 모름 |

## 환경

- 26100.9457, **정적만** (VM 실행 없음). Timer2 · wait packet 은 커널 개체라 run44 사진에 칸이 없다 — 값 대조 없음, 사진도 다시 안 읽었다.
- 정적: ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1`, ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` — 둘 다 다시 analyze 안 함 (결과서).
  - ntoskrnl a: `^(NtSetTimer2|ExpSetTimer2|KeSetTimer2|NtCreateTimer2)$` depth 1 (32개 — `KiRequestTimer2Expiration` · `KiAcquireTimer2LockUnlessDisabled` · `KiAcquireTimer2CollectionLockIfInserted` · `ExpExTimerAttributesAreValid` · `ExpCheckIRTimerAccess` · `KeInitializeIRTimer` 읽음).
  - ntoskrnl b: `^(KeInitializeTimer2|KiExpireTimer2)$` depth 0.
  - ntoskrnl c (요청서 밖, 결과서가 넓힘): `^(KiInitializeTimer2|KiSetDpcRequestFlag)$` depth 0 — `KeInitializeTimer2` 가 19 B 껍데기라 형 바이트가 한 단계 아래에 있음.
  - ntdll: `^TppInitializeTimerSubQueue$` depth 0 (있던 파일 다시 뽑음).
  - 있던 `KiRetireDpcList` · `KiTimer2Expiration` `.asm` 은 호출 자리만.
- PDB 타입: `_KTIMER2` (`0x88`B — `+0x0` 헤더 · `+0x18` 노드 둘 (48B) · `+0x48` DueTime 둘 (16B) · `+0x58` Period · `+0x80` AbsoluteSystemTime · `+0x81` TypeFlags · `+0x82` CollectionIndex 2B), `_EXT_SET_PARAMETERS_V0` (`0x10`B) — flow-writer 가 `pdb_struct.py` 로 확인.
- 이름: `ExpSetTimer2 +0x9a` · `NtCreateTimer2 +0xa1` 의 callee 는 `_calls.tsv` 에 `alias:RtlCopyVolatileMemory+1` 로 찍힌 **메모리 복사 함수** — 같은 주소 (rva `0x6b96f0`) 이름 2개, 후보 `RtlCopyVolatileMemory` · `RtlpCopyDeviceMemory` (`pdb_aliases.py --where ntoskrnl.exe RtlCopyVolatileMemory`, flow-writer). 이 문서는 "복사" 로만 쓴다.
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` · ntdll `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` 일치.

## 목적

- run93 남은 정적 ② (Q176 → 사용자 결정 2026-11-15 "Q176 ② · Q178 ① 먼저" 로 나눈 Q176a). PE_emulator 쪽 물음: Go 런타임 · 스레드 풀 타이머가 `NtCreateTimer2` → `NtSetTimer2` → wait packet 맡김 → `NtRemoveIoCompletionEx` 로 기다리는데, (가) 지난 시각 · 0 으로 걸면 그 자리에서 울리나 다음 틱인가, (나) 만든 타이머가 동기 (`0x19`) 인지 알림 (`0x18`) 인지 정하는 인자.
- 범위: 걸기 · 만들기의 인자와 갈림만. 만기 뒤 배달 (`KiExpireTimer2` 본문 · `KiInsertQueueInternal`) 은 run93 에서 읽음.

## 방법

- 정적: `_workspace/sast/91_timer2-set.md` — 위 "환경" 의 뽑기 셋 + ntdll 하나.
- 결과서: `_workspace/sast/91_timer2-set_result.md` (**분석성공** — 못 정한 것 셋: DPC 가 부른 스레드 위에서 도나 (CPU 에 따라) · Go 의 `Attributes` · 사용자 포인터 오류 status).
- flow-writer 가 다시 본 것 (`.asm`):
  - `NtSetTimer2` 전부, `ExpSetTimer2 +0x26` · `+0x41 ~ +0x51` · `+0x79 ~ +0x86` · `+0xd8` · `+0xf3 ~ +0x115` · `+0x125 ~ +0x134` · `+0x155` · `+0x16f ~ +0x1ca`.
  - `KeSetTimer2 +0x25/+0x28` · `+0x63 ~ +0x96` · `+0xee ~ +0x143` · `+0x24c` · `+0x38a ~ +0x392` · `+0x435/+0x438` · `+0x49c/+0x4a0` · `+0x4bf ~ +0x4ec` · `+0x678 ~ +0x6c2` · `+0x6df` · `+0x7a7 ~ +0x7b6`.
  - `KiRequestTimer2Expiration` 전부 (`+0x0 ~ +0xe2`), `KeInitializeTimer2` 전부, `KiInitializeTimer2 +0x0 ~ +0x3e`.
  - `NtCreateTimer2 +0x2e ~ +0x3e` · `+0xd6 ~ +0x10f` · `+0x142` · `+0x195` · `+0x1ad/+0x1b2`.
  - `KiRetireDpcList +0x406 ~ +0x411` · `+0xca4 ~ +0xccb`, `KiTimer2Expiration +0x4b/+0x4e` · `+0xd7/+0xdc` · `+0x290` · `+0x382` · `+0x399`, `KiExpireTimer2 +0xad/+0xb1` · `+0x2cf ~ +0x2d6` · `+0x353/+0x358`.
  - ntdll `TppInitializeTimerSubQueue +0x0 ~ +0xc3` (CALL 은 `+0x4d` · `+0x65` · `+0xa0` · `+0xbe` · `+0x109` 다섯뿐).

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtSetTimer2` | `+0x9` | `+0xe` | `ExpSetTimer2` | `+0x4 TEST RDX` · `+0x7 JZ +0x14` (DueTime 포인터 NULL → `0xC00000F0`) | [정적] |
| `nt!ExpSetTimer2` | `+0x9a` | `+0x9f` | 복사 (같은 주소 이름 2개) | Parameters 16B 를 커널 지역에 (사용자 모드일 때) | [정적] |
| 〃 | `+0xfb` | `+0x100` | `ObReferenceObjectByHandle` | 접근 2 (`+0xf3`), 형 `ExpIRTimerObjectType` (`+0xd8`). 음수 → `+0x102 JS +0x191` | [정적] |
| 〃 | **`+0x17d`** | **`+0x182`** | **`KeSetTimer2`** | `(타이머, DueTime 값, Period 값, Parameters*)`. 반환은 안 읽음 | [정적] |
| 〃 | `+0x1c3` | `+0x1c8` | `KeCancelTimer2` | DueTime 포인터 NULL 일 때 — `NtCancelTimer2` 길 (`NtSetTimer2` 는 입구에서 막음) | [정적] |
| 〃 | `+0x18a` | `+0x18f` | `ObfDereferenceObjectWithTag` | 참조 풀기 | [정적] |
| `nt!KeSetTimer2` | `+0xee` | `+0xf3` | `KiAcquireTimer2LockUnlessDisabled` | TRUE (Disabled) → `+0xf5 JNZ +0x4b6` 아무것도 안 함 | [정적] |
| 〃 | `+0xfe` | `+0x103` | `KiAcquireTimer2CollectionLockIfInserted` | 이미 걸림 (`[타이머+1]` bit 0) 이면 TRUE. FALSE → `+0x105 JZ +0x311` | [정적] |
| 〃 | `+0x10e` | `+0x113` | `KiRemoveTimer2` | 앞 것 빼기 (다시 걸기) | [정적] |
| 〃 | `+0x24c` | `+0x251` | `RtlGetInterruptTimePrecise` | HighResolution 일 때 "지금" | [정적] |
| 〃 | `+0x365` · `+0x571` | `+0x36a` · `+0x576` | `RtlRbInsertNodeEx` | 둘째 줄 (노드 `+0x30`, 열쇠 `+0x50`) · 첫째 줄 (노드 `+0x18`, 열쇠 `+0x48`) | [정적] |
| 〃 | **`+0x6bd`** | **`+0x6c2`** | **`KiRequestTimer2Expiration`** | `+0x435 TEST R14B` · `+0x438 JNZ` — 머리이고 만기 ≤ 지금 | [정적] |
| 〃 | `+0x7a7` | `+0x7ac` | `KiSendClockInterruptToTargetProcessor` | R14B = 0 · HighResolution · 만기가 가까울 때 (시계 인터럽트 앞당김) | [정적] |
| 〃 | `+0x7b1` | `+0x7b6` | `RtlGetSystemTimePrecise` | 절대 시각 · HighResolution 일 때 | [정적] |
| `nt!KiRequestTimer2Expiration` | `+0x27` | `+0x2c` | `KiSetDpcRequestFlag` | `(소유 PRCB+0x38bc, 8)`. 옛 값 `& 0xa9` 면 `+0x2e JNZ` 끝 (이미 요청 있음) | [정적] |
| 〃 | `+0x42` | `+0x47` | `KiSendSoftwareInterrupt` | 현재 PRCB ≠ 소유 PRCB (`+0x39 CMP` · `+0x3c JZ`) → `(소유 CPU, 2)` | [정적] |
| 〃 | `+0xdd` | `+0xe2` | `HalpInterruptSendIpi` | 같은 CPU · `[PRCB+0x20]` 0 (`+0x53` · `+0x57 JZ +0x5f`) → `+0xcb EDX = 0x2f` | [정적] |
| `nt!NtCreateTimer2` | `+0x2e` | `+0x33` | `ExpExTimerAttributesAreValid` | FALSE → `+0x35 JZ +0x1b7` (`0xC00000F2`) | [정적] |
| 〃 | `+0xe7` | `+0xec` | `ObCreateObjectEx` | 본문 `0xa8` (`+0xd6`) | [정적] |
| 〃 | **`+0x10a`** | **`+0x10f`** | **`KeInitializeTimer2`** | `+0x107 MOV R9D,EDI` (= `Attributes`) | [정적] |
| 〃 | `+0x1ad` | `+0x1b2` | `KeInitializeIRTimer` | `Attributes & 2` (IdleResilient) 일 때 대신 (`+0x101 JNZ +0x1a4`) | [정적] |
| 〃 | `+0x195` | `+0x19a` | `ExpCheckIRTimerAccess` | IdleResilient · Reserved1 있을 때 | [정적] |
| 〃 | `+0x142` | `+0x147` | `ObInsertObjectEx` | 접근 = 5번째 인자 | [정적] |
| `nt!KeInitializeTimer2` | `+0x9` | `+0xe` | `KiInitializeTimer2` | 앞 `+0x4 AND word [RCX+2],0` | [정적] |
| `nt!KiRetireDpcList` | `+0xca4` | `+0xca9` | `KiTimerExpiration` | 비트 8 (`+0x40d TEST DIL,8` · `+0x411 JNZ +0xbca`) | [정적] |
| 〃 | **`+0xcc6`** | **`+0xccb`** | **`KiTimer2Expiration`** | `+0xca9 CMP byte [PRCB+0x21],0` 뒤 (시계 소유) | [정적] |
| `nt!KiTimer2Expiration` | `+0xd7` | `+0xdc` | `KiExpireTimer2` | run93 그대로. `+0x4b` · `+0x4e JC +0x16e` (아직이면 끝) | [정적] |
| 〃 | `+0x399` | `+0x39e` | `KiInsertTimer2` | `+0x382 TEST AL,8` — "만기 처리 중에 다시 걸림" (SetPending) 을 나중에 넣음 | [정적] |
| `ntdll!TppInitializeTimerSubQueue` | `+0x4d` | `+0x52` | `NtCreateTimer2` | `+0x34` RCX · `+0x37 R9D = 8` · `+0x41` R8 0 · `+0x44` `0x100002` · `+0x4b` RDX 0 | [정적] |

- `KeSetTimer2` 는 위 말고도 `KiAcquireSpinLockInstrumented` · `KxWaitForSpinLockAndAcquire` · `KiTraceSetTimer2` · `KiUpdateTimer2Collections` · `PsTimerResolutionActive` · `KiLowerIrqlProcessIrqlFlags` · `KiRaiseIrqlProcessIrqlFlags` · `KiReleaseSpinLockInstrumented` · `KiHvEnlightenedGuestPriorityKick` 를 부른다 (`_calls.tsv`) — 줄기 밖, 이름만.
- **`NtSetTimer2` · `ExpSetTimer2` · `KeSetTimer2` 의 호출 목록 어디에도 `KiTimer2Expiration` · `KiExpireTimer2` · `KiInsertQueueInternal` 이 없다** (`_calls.tsv`).

### `NtSetTimer2(TimerHandle, DueTime*, Period*, Parameters*)` 인자 칸 (이름은 공개 헤더 관례, 뜻은 `.asm`)

| # | 레지스터 | 뜻 | 근거 |
|---|---|---|---|
| 1 | RCX | 타이머 핸들 — 접근 `0x2`, 형 `ExpIRTimerObjectType` | `ExpSetTimer2 +0xd8` · `+0xf3` · `+0xfb` |
| 2 | RDX | `DueTime*` (int64). NULL → `0xC00000F0` | `NtSetTimer2 +0x4 ~ +0x14` |
| 3 | R8 | `Period*` (int64, 100 ns). NULL → 주기 0 (한 번) | `ExpSetTimer2 +0x79` · `+0x7c JNZ` |
| 4 | R9 | `Parameters*` (`_EXT_SET_PARAMETERS_V0` 16B), NULL 가능 | `ExpSetTimer2 +0x86 JZ` · `+0x9a` 복사 |

- 사용자 모드면 (`GS:[0x188]` 스레드 `+0x232` `PreviousMode`, `+0x41 ~ +0x51`) 포인터를 `0x7fffffff0000` 으로 눌러서 읽고 커널 지역 변수로 복사 (`+0x53 ~ +0xa9`). 커널 모드면 `+0x51 JZ +0xc7` 로 건너뜀.

### `KeSetTimer2` 차례 표 (결과서 25줄을 줄임 — 숫자는 결과서 순서)

| # | 자리 | 하는 일 | 갈림 |
|---|---|---|---|
| 9 | `+0x25/+0x28` → `+0x6df ~ +0x6ec` | Period ≠ 0 이면 `KeMinimumIncrement` 보다 작을 때 올림 (부호 있는 비교 — 음수도 올라감) | Period 0 → 건너뜀 |
| 10 | `+0x2e ~ +0x4f` | `TypeFlags` (`+0x81`) 읽기, **IRQL 2 로 올림** (`MOV CR8`) | — |
| 11 | **`+0x63 TEST RSI,RSI` · `+0x66 JG`** | **DueTime 부호로 절대 / 상대** | `> 0` → `+0x678` 절대 · `≤ 0` (0 포함) → `+0x6c` 상대 |
| 12 | `+0x678 ~ +0x6a5` (절대) | `R15B = 1`. SystemTime (`+0x685` KUSD `+0x14`, HighResolution 이면 `+0x7b1 RtlGetSystemTimePrecise`) 과 비교 — `DueTime > SystemTime` 이면 상대 = `SystemTime − DueTime` (음수), **아니면 0** (`+0x698 SUB` · `+0x69b CMP` · `+0x69e CMOVG` · `+0x6a2`) → `+0x6c` | — |
| 13 | `+0x6c ~ +0x9c` | **만기 = 지금 (KUSD `+0x8` `InterruptTime`, `+0x6c`) − 상대** (`+0x86 SUB RDI,RSI`). HighResolution 이면 `+0x24c RtlGetInterruptTimePrecise` | 넘침 (`+0x8c JC` · `+0x96 JZ`) → `+0x23b` 만기 `0xFFFFFFFFFFFFFFFE` (안 옴) |
| 14 | `+0x9f ~ +0xcc` | 늦은 만기 = 만기 + `NoWakeTolerance` (Parameters 있고 `+0x82 ≠ 0x17` 일 때, −1 = 끝 없음) | 아니면 늦은 만기 = 만기 |
| 15 | `+0xee/+0xf3` | 타이머 잠금. Disabled (`[+1] & 0x20`) 면 TRUE | TRUE → `+0x4b6` (아무것도 안 함, FALSE 반환) |
| 16 | `+0xfe/+0x103` → `+0x10e/+0x113` | **이미 걸렸으면 앞 것 빼기** (`KiRemoveTimer2`), 반환 값 TRUE (`+0x128`) | 안 걸렸으면 `+0x105 JZ +0x311` (만기 처리 중 확인 — 아래) |
| 17 | `+0x130 ~ +0x147` | **칸 쓰기**: `+0x4` `SignalState` ← **0** · `+0x48` 만기 · `+0x50` 늦은 만기 · `+0x58` Period · `+0x80` 절대 표시 | — |
| 18 | `+0x1b2 ~ +0x596` | 두 줄 (레드블랙 트리) 에 넣기. `+0x82 == 0x17` 이면 첫째 줄 건너뜀 (`+0x1b5 JZ +0x264`) | 줄 맨 앞이 되면 `ESI` / `R12D` = 1 (`+0x593` · `+0x383`) |
| 19 | **`+0x38a OR ESI,R12D` · `+0x392 JNZ +0x4bf`** | **머리가 됐을 때만** 20번으로 | 아니면 `+0x398` (요청 안 함) |
| 20 | `+0x4bf ~ +0x4ec` | `KiNextTimer2DueTime` 이 더 크면 내 만기로 (`+0x4c3` · `+0x4cc`). **`+0x4d8` 지금 다시 읽기 · `+0x4e2 CMP [타이머+0x48],지금` · `+0x4e6 JA +0x398`**. 만기 ≤ 지금이면 `+0x4ec R14B = 1` | 만기 > 지금 → R14B = 0 |
| 21 | `+0x3c6 ~ +0x3f4` | 머리 상태 바이트에 "걸림" CMPXCHG, 잠금 풂 | — |
| 22 | `+0x421 ~ +0x433` | `KiForceIdleDisabled == 0` 이고 `KiForceIdleState == 4` 면 `+0x47c` (요청 안 함) | 예외 갈래 |
| 23 | **`+0x435/+0x438` → `+0x6bd/+0x6c2`** | **R14B ≠ 0 → `KiRequestTimer2Expiration()`** | R14B = 0 → 24 |
| 24 | `+0x43e ~ +0x7ac` | HighResolution · 만기 < 지금 + `KeMaximumIncrement` 등 → `+0x7a7 KiSendClockInterruptToTargetProcessor(KiClockTimerOwner)` | HighResolution 아니면 안 함 |
| 25 | `+0x47c ~ +0x4b4` | IRQL 되돌림 (`+0x49c MOV CR8`), AL = 앞 것이 걸려 있었나 | `ExpSetTimer2` 는 안 읽음 |

- **만기 처리 중이던 타이머를 다시 걸 때** (`[+1]` 하위 4비트 ≠ 0 인데 걸려 있지 않음): `+0x105 JZ +0x311` → `+0x31f EBP = 8` (SetPending) — 줄 넣기 · 요청을 건너뛰고 (`+0x16e` · `+0x171 JNZ +0x72f` · `+0x72f XOR R14B,R14B`), 만기 쪽 `KiTimer2Expiration +0x382 TEST AL,8` → `+0x399 KiInsertTimer2` 가 나중에 넣는다. 이때도 신호는 `+0x130` 에서 0.

### `KiRequestTimer2Expiration` (rva `0x26c778`) — 즉시 요청의 몸

```
KiRequestTimer2Expiration ()                                                        [정적]
 │  +0xa   EDI = KiClockTimerOwner · +0x1c RBX = KiProcessorBlock[EDI] · +0x20 RCX = RBX+0x38bc · +0x17 EDX = 8
 ├─ (1) +0x27  KiSetDpcRequestFlag(소유 PRCB+0x38bc, 8)     옛 값 & 0xa9 → +0x2e JNZ 끝 (이미 요청 있음)
 │  +0x30/+0x39  현재 PRCB (GS:[0x20]) == 소유 PRCB ?
 ├─ (2) +0x42  KiSendSoftwareInterrupt(소유 CPU, 2)          다르면 — 그 CPU 에 DISPATCH 소프트 인터럽트
 │  같으면 +0x53 [PRCB+0x20] ≠ 0 → +0x59 [PRCB+6] = 1 (InterruptRequest) 로 끝
 │         0 이면 +0x64 AMD TPR 우회 검사 (켜졌으면 +0x7b HalpDisableInterrupts · MSR 0xc0010015)
 └─ (3) +0xdd  HalpInterruptSendIpi(…, 0x2f)                  자기 자신에게 IPI (+0xcb EDX = 0x2f)
```

- 받는 쪽: `KiRetireDpcList +0x406 XCHG [PRCB+0x38bc]` → `+0x40d TEST DIL,8` → `+0x411 JNZ +0xbca` → … `+0xca4 KiTimerExpiration` → `+0xca9 CMP byte [PRCB+0x21],0` → **`+0xcc6 KiTimer2Expiration`** → `+0x4b` 지금 vs `KiNextTimer2DueTime` (`+0x4e JC +0x16e` 아직이면 끝) → `+0x290` 이른 순으로 꺼냄 → `+0xd7 KiExpireTimer2` (run93). `KiRetireDpcList +0xbca ~ +0xca4` 사이 갈림 (`+0xc51` · `+0xc5d` · `+0xc76`) 은 안 따라감 `[공백]`.
- 벡터 `0x2f` 가 DISPATCH 단계 인터럽트라는 것 · IRQL 2 동안 보류됐다가 `KeSetTimer2 +0x49c` 에서 배달된다는 것은 `[추측]` (결과서). run78 의 `KiCheckForTimerExpiration +0x3d5 KiSetDpcRequestFlag(PRCB+0x38bc, 8)` → `+0x4f1 HalpInterruptSendIpi 0x2f` 와 **같은 꼴** — 틱이 할 요청을 걸기 쪽이 대신 건다.

### "지난 시각 / 0 / 미래" 세 경우의 길

| 경우 | `KeSetTimer2` 안에서 | 만기 (`+0x48`) | 즉시 요청? | 만기 처리가 일어나는 곳 |
|---|---|---|---|---|
| **지난 절대** (`DueTime > 0`, `≤ SystemTime`) | `+0x66 JG` → `+0x678` · `+0x69e CMOVG` 안 탐 → 상대 **0** | = 지금 | **예** — 머리면 `+0x4e6 JA` 안 뜀 → `+0x6bd` | DPC (`KiRetireDpcList +0xcc6` → `KiTimer2Expiration +0xd7` → `KiExpireTimer2`), 틱 안 기다림 |
| **`DueTime = 0`** | `+0x66 JG` 안 탐 → `+0x6c` 상대. **0 을 따로 다루는 줄 없음** | = 지금 | **예** (위와 같음) | 위와 같음 |
| **미래 상대** (`< 0`) | `+0x6c ~ +0x9c` 지금 + \|DueTime\| | > 지금 | **아니오** — `+0x4e6 JA +0x398` (HighResolution 이면 `+0x7a7` 시계 인터럽트만 앞당김) | 시계 틱 뒤 `KiTimer2Expiration` 이 `지금 ≥ 만기` 일 때 |
| **미래 절대** | `+0x678 ~ +0x6a5` 로 상대로 바꾼 뒤 위와 같음 | > 지금 | 아니오 | 위와 같음 |

- 즉시 요청은 **머리일 때만** (`+0x392`). 앞에 더 이른 타이머가 있으면 그 타이머가 이미 만기 · 요청 대상이라 안 한다.
- HighResolution 타이머는 만기를 정밀 시계로 계산하지만 `+0x4d8` 비교는 공유 `InterruptTime` 이라, 상대 0 이어도 R14B = 0 이 될 수 있다 — 그때는 `+0x7a7` 이 시계 인터럽트를 앞당긴다.

### 주기 · 다시 걸기

- **주기 = 3번째 인자** (구조체 칸 아님). `ExpSetTimer2 +0x108 MOV R8,[RSP+0x40]` → `+0x17d` → `KeSetTimer2 +0x143 MOV [RBX+0x58],R14` (`_KTIMER2.Period`).
- 단위 100 ns 로 읽힌다 — 만기 쪽이 `Period` 를 그대로 더한다: `KiExpireTimer2 +0xad CMP [RCX+0x58],R12` · `+0xb1 JNZ +0x2b4` (주기 갈래) → `+0x2cf MOV R11,[RBX+0x58]` · `+0x2d6 ADD R11,R10` → `+0x353 KiInsertTimer2WithCollectionLockHeld` (다시 넣기). 다음 만기가 "만기 처리 때의 지금 + Period" 인지 "이전 만기 + Period" 인지는 `R10` 이 무엇인지에 달림 — 결과서는 "지금 + Period" 로 읽었고 `[확인 필요]` (정적 ③).
- **다시 걸기**: `KeSetTimer2 +0xfe KiAcquireTimer2CollectionLockIfInserted` → `+0x10e KiRemoveTimer2` → `+0x120` 잠금 풂 → `+0x128` 반환 TRUE → 같은 호출 안에서 `+0x130 ~` 새 값으로 다시 넣음. 신호는 `+0x130` 에서 0.

### `Parameters` (`_EXT_SET_PARAMETERS_V0`, `0x10`B, PDB)

| 오프셋 | 이름 | 코드에서 | 근거 |
|---|---|---|---|
| `+0x0` | Version | 0 이어야 함, 아니면 `0xC00000F1` | `ExpSetTimer2 +0x125` · `+0x129 JNZ +0x1ca` |
| `+0x4` | Reserved | 안 읽음 | — |
| `+0x8` | NoWakeTolerance | int64, −1 이상 (`< −1` 이면 `0xC00000F1`). −1 = 끝 없음 (`KeSetTimer2 +0xb1` · `+0xb5`), 그 외 = 늦은 만기 = 만기 + 값 (`+0xbb`). 형 바이트 `+0x82 == 0x17` (플래그 없는 보통 타이머) 이면 안 씀 (`+0xa4` · `+0xab`). 객체 `+0x98` 에도 복사 (`ExpSetTimer2 +0x16f`) | `ExpSetTimer2 +0x12f` · `+0x134 JL` |

### status 표 (`NtSetTimer2`) — 상수 이름은 `[추측]`, 값은 `.asm`

| 상황 | status | 근거 |
|---|---|---|
| 성공 (Disabled 타이머로 아무것도 안 해도) | `0` | `ExpSetTimer2 +0x26 XOR EDI,EDI` · `+0x18f MOV EAX,EDI` |
| `DueTime` 포인터 NULL | `0xC00000F0` | `NtSetTimer2 +0x7 → +0x14` |
| 핸들 오류 | `ObReferenceObjectByHandle` 값 | `ExpSetTimer2 +0x102 JS +0x191` |
| Period ≠ 0 · IdleResilient (`[+0x81]` bit 1) | `0xC000000D` | `+0x115 JNZ +0x1ad` · `+0x1ad` · `+0x1ba` |
| Version ≠ 0 · NoWakeTolerance < −1 | `0xC00000F1` | `+0x129` · `+0x134` → `+0x1ca` |
| 사용자 포인터 읽다 틀림 | 모름 — `.asm` 에 예외 처리기 줄이 안 보임 `[공백]` | `+0x6a` · `+0xb6` 읽는 줄 |

### `NtCreateTimer2(TimerHandle*, Reserved1*, Reserved2, Attributes, DesiredAccess)` — 동기 / 알림

| # | 레지스터 | 이름 | 뜻 | 근거 |
|---|---|---|---|---|
| 1 | RCX → R14 | TimerHandle (출력) | 사용자 모드면 쓰기 검사, 끝에 `+0x156` 핸들 씀 | `+0x84` · `+0x87` · `+0x156` |
| 2 | RDX → R10 | Reserved1* | NULL 이 아니려면 `Attributes & 2` 필요 (아니면 `0xC00000F0`). 4B 읽어 `ExpCheckIRTimerAccess` | `+0x4c` · `+0x1c0` · `+0x1c6` · `+0x195` |
| 3 | R8 → R11 | Reserved2 | NULL 이어야 함, 아니면 `0xC00000F1` | `+0x3b` · `+0x3e JNZ +0x1cd` |
| 4 | R9D → EDI | **Attributes** | 아래 두 표 | `+0x2e` · `+0x107` · `+0x10a` |
| 5 | 스택 | DesiredAccess | `ObInsertObjectEx` 접근 | `+0x135` · `+0x142` |

**형을 고르는 줄** — `NtCreateTimer2 +0x107 MOV R9D,EDI` → `+0x10a KeInitializeTimer2(객체, 0, 0, Attributes)` → `+0x9 KiInitializeTimer2` → `+0xa MOV qword [RCX],0` (헤더 첫 8B — `SignalState` `+0x4` 포함 — 0) → **`+0xd TEST R9D,R9D` · `+0x10 SETNS AL` · `+0x13 ADD AL,0x18` · `+0x15 MOV [RCX],AL`**:

| `Attributes` bit 31 (`0x80000000`) | 형 바이트 `[타이머+0]` | 뜻 |
|---|---|---|
| 있음 | **`0x18`** | 알림 — 만기 뒤 신호 그대로 (`KiExpireTimer2 +0x702/+0x707`, run93) |
| 없음 (기본) | **`0x19`** | 동기 — 만기 배달 뒤 신호 1 내림 (`KiExpireTimer2 +0x3ad`, run93) |

- IdleResilient 길 (`KeInitializeIRTimer +0x21 OR R9D,2`) 도 같은 `KiInitializeTimer2` 로 가서 bit 31 규칙은 같다 (결과서).

**다른 `Attributes` 비트** (`ExpExTimerAttributesAreValid`: `(A & 0x8000000e) == A` 아니면 `0xC00000F2`, 이어 `A & 0xe` 가 `{0, 2, 4, 8, 6, 0xc}` 중 하나 — 결과서):

| 비트 | 값 | PDB `_KTIMER2.TypeFlags` | 효과 |
|---|---|---|---|
| 31 | `0x80000000` | (PDB 이름 없음, 공개 헤더 `EX_TIMER_NOTIFICATION` 은 `[추측]`) | 형 `0x18` |
| 1 | `0x2` | IdleResilient | `KeInitializeIRTimer` 길 · Reserved1 검사 · **Period ≠ 0 불가** (`ExpSetTimer2 +0x1ad`) |
| 2 | `0x4` | HighResolution | 정밀 시계 · `KiHrTimerActiveCount` (`+0x4f5`) · 시계 인터럽트 앞당김 |
| 3 | `0x8` | NoWake | `[+0x81]` 에 (`KiInitializeTimer2 +0xa2` · `+0xa8`). 줄 번호 `+0x82/+0x83` 은 표 `KiTimer2Combinations` 에서 — `0x8` → `{0x20, 0x24}`, 0 → `{0x17, 0x04}` (결과서) |
| 4 · 5 | `0x10` · `0x20` | PseudoHighRes · AusterityResilient | 인자로 못 줌. 안에서 자동 (`PsTimerResolutionActive` · `ExAusterityResilientTimersEnabled`) |
| — | `0xa` · `0xe` 등 | — | 표에 없는 조합 → `0xC00000F2` |

- `NtCreateTimer2` status: 성공 0 · 속성 틀림 `0xC00000F2` · Reserved2 ≠ NULL `0xC00000F1` · Reserved1 ≠ NULL 인데 bit 1 없음 `0xC00000F0` · IR 접근 검사 실패 (`ExpCheckIRTimerAccess`) · 그 외 `ObCreateObjectEx` / `ObInsertObjectEx` 값. 객체 본문 `0xa8` (`+0xd6`), 객체 `+0x88` ← 0 · `+0xa0` ← Attributes (`+0x10f ~ +0x117`) (결과서).

### 스레드 풀 타이머 큐 — `TppInitializeTimerSubQueue` 두 값

| 줄 | 무엇 | 뜻 |
|---|---|---|
| `+0x37 MOV R9D,0x8` | 4번째 인자 `Attributes = 8` | NoWake 만. bit 31 없음 → **동기 `0x19`**. bit 1 없음 → `KeInitializeTimer2` 길 (Period 허용) |
| `+0x44 MOV dword [RAX-0x38],0x100002` | 5번째 인자 `DesiredAccess` (`RAX` = 들어올 때 RSP → `RSP+0x20`) | `0x100000` (SYNCHRONIZE) + `0x2` (TIMER_MODIFY_STATE) `[추측: 상수 이름]` |
| `+0x4b XOR EDX,EDX` · `+0x41 XOR R8D,R8D` · `+0x34 MOV RCX,RBX` | Reserved1 = 0 · Reserved2 = 0 · RCX = 서브큐 `+0x18` (핸들 칸, `+0x1c LEA RBX,[RCX+0x18]`) | — |

- 이어서 `+0x65 NtCreateWaitCompletionPacket` → `+0xa0 NtAssociateWaitCompletionPacket` (대상 `[RBX]` = 방금 만든 타이머). **그 사이에 `NtSetTimer2` 는 없다** — 이 함수의 CALL 은 다섯뿐 (`+0x4d` · `+0x65` · `+0xa0` · `+0xbe TppGetCurrentThreadNumaNode` · `+0x109 NtClose`).

### 호출 관계

```
NtSetTimer2 (핸들, DueTime*, Period*, Parameters*)                                   [정적]
 │  +0x4 DueTime* == NULL → +0x14 0xC00000F0
 └─ +0x9  ExpSetTimer2
      │  +0x41 ~ +0xa9  사용자 포인터 → 커널 사본 (DueTime 8 · Period 8 · Parameters 16, +0x9a 복사)
      ├─ (1) +0xfb   ObReferenceObjectByHandle (접근 2, ExpIRTimerObjectType)
      │  +0x108 ~ +0x13a  Period ≠ 0 · IdleResilient → 0xC000000D · Version / NoWakeTolerance → 0xC00000F1
      │  +0x155 ~ +0x16f  객체 +0x90 ← DueTime · +0x98 ← NoWakeTolerance
      ├─ (2) +0x17d  KeSetTimer2 (타이머, DueTime, Period, Parameters)
      │        │  +0x63 / +0x66  > 0 절대 (+0x678 SystemTime 과 비교, 지났으면 0) · ≤ 0 상대
      │        │  +0x6c ~ +0x9c  만기 = InterruptTime − 상대         IRQL 2 (+0x2e ~ +0x4f)
      │        ├─ (1) +0xee   KiAcquireTimer2LockUnlessDisabled     Disabled → 아무것도 안 함
      │        ├─ (2) +0xfe   KiAcquireTimer2CollectionLockIfInserted
      │        ├─ (3) +0x10e  KiRemoveTimer2                      이미 걸렸으면 앞 것 빼기
      │        │  +0x130 SignalState = 0 · +0x48 만기 · +0x50 늦은 만기 · +0x58 Period
      │        ├─ (4) +0x571 · +0x365  RtlRbInsertNodeEx ×2       머리면 ESI / R12D = 1
      │        │  +0x392 머리? → +0x4e6 만기 > 지금? (InterruptTime +0x4d8)
      │        ├─ (5) +0x6bd  KiRequestTimer2Expiration           머리 · 만기 ≤ 지금 (R14B = 1)
      │        │        ├─ (1) +0x27  KiSetDpcRequestFlag(소유 PRCB+0x38bc, 8)
      │        │        ├─ (2) +0x42  KiSendSoftwareInterrupt(소유 CPU, 2)   다른 CPU
      │        │        └─ (3) +0xdd  HalpInterruptSendIpi(0x2f)              같은 CPU
      │        └─ (6) +0x7a7  KiSendClockInterruptToTargetProcessor   R14B = 0 · HighResolution
      │           +0x49c IRQL 되돌림 → AL (버려짐)
      └─ (3) +0x18a  ObfDereferenceObjectWithTag        → EAX = 0

[DPC]  KiRetireDpcList +0x40d 비트 8 → +0xcc6 KiTimer2Expiration → +0xd7 KiExpireTimer2      [정적]
       (동기 0x19 +0x3a8 KiInsertQueueInternal → +0x3ad 신호 −1 · 알림 0x18 +0x702 — run93)

NtCreateTimer2 (핸들*, Reserved1*, Reserved2, Attributes, DesiredAccess)            [정적]
 ├─ (1) +0x2e   ExpExTimerAttributesAreValid           (A & 0x8000000e) == A · A & 0xe ∈ {0,2,4,8,6,0xc}
 ├─ (2) +0xe7   ObCreateObjectEx                        본문 0xa8
 ├─ (3) +0x10a  KeInitializeTimer2 (객체, 0, 0, Attributes)     [IdleResilient 면 +0x1ad KeInitializeIRTimer]
 │        └─ +0x9  KiInitializeTimer2    +0xa 헤더 8B = 0 · +0xd ~ +0x15 형 = bit31 ? 0x18 : 0x19
 └─ (4) +0x142  ObInsertObjectEx (접근 = DesiredAccess)

TppInitializeTimerSubQueue (ntdll)                                                  [정적]
 ├─ (1) +0x4d  NtCreateTimer2 (서브큐+0x18, 0, 0, 8, 0x100002)     → 동기 0x19 · NoWake
 ├─ (2) +0x65  NtCreateWaitCompletionPacket
 └─ (3) +0xa0  NtAssociateWaitCompletionPacket (대상 = 그 타이머)   (run93)
```

## 확인된 것

- `[정적]` 지난 절대 시각 · `DueTime = 0` → 만기 = 지금 (`KeSetTimer2 +0x69e` · `+0x66`). 머리면 `+0x4e6 JA` 안 뜀 → `+0x6bd KiRequestTimer2Expiration` → `+0x27 KiSetDpcRequestFlag(…, 8)`.
- `[정적]` `NtSetTimer2` · `ExpSetTimer2` · `KeSetTimer2` 는 `KiTimer2Expiration` · `KiExpireTimer2` · `KiInsertQueueInternal` 을 부르지 않는다 (`_calls.tsv`). 만기 처리는 `KiRetireDpcList +0xcc6` 쪽.
- `[정적]` 미래 값은 상대 −1 이라도 즉시 요청 없음 (`+0x4e6 JA +0x398`).
- `[정적]` 0 을 따로 다루는 줄 없음 — 부호만 (`+0x63` · `+0x66`).
- `[정적]` 주기 = 3번째 인자 → `_KTIMER2.Period` (`+0x143`). Parameters 에는 주기 칸이 없다 (Version · Reserved · NoWakeTolerance).
- `[정적]` 다시 걸기 = 같은 호출 안에서 `KiRemoveTimer2` 뒤 다시 넣기. 신호는 걸 때마다 0 (`+0x130`). `NtSetTimer2` 반환엔 "앞 것 있었나" 가 없다.
- `[정적]` 동기 / 알림 = `NtCreateTimer2` `Attributes` bit 31 (`KiInitializeTimer2 +0xd ~ +0x15`). 만들 때 헤더 첫 8B (신호 포함) 0 (`+0xa`).
- `[정적]` 스레드 풀 타이머 큐 Timer2 = `Attributes 8` (NoWake) → 동기 `0x19`, 접근 `0x100002`. 만든 뒤 맡기기 전 `NtSetTimer2` 없음.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① (자리 바로 적음) `KiRequestTimer2Expiration` 의 `KiSendSoftwareInterrupt` call 은 `+0x42/+0x47` (결과서 "`+0x3e ~ +0x47`" 은 인자 준비부터 묶은 범위 — 뜻 같음). 같은 CPU 갈래 `+0x53 CMP` 뒤 `+0x57 JZ +0x5f` 이고, `+0x5f ~ +0xc8` 에 **AMD TPR 우회 갈래** (`+0x64 KiAmdTprLowerInterruptDelayDynamicWorkaround` 검사 → `+0x7b HalpDisableInterrupts` · MSR `0xc0010015`) 가 끼어 있다 — 결과서는 건너뜀. 결론 (자기 IPI `0x2f`) 에 영향 없음.
- ② (표시만 낮춤) `KiTimer2Expiration +0x290` 의 실제 줄은 `CMP R13,[RSI+0x30]` 이다. 결과서 "`CMP now,[타이머+0x48]`" 은 `RSI` 가 첫째 줄 노드 (`+0x18`) 를 가리킨다는 해석 (`0x18 + 0x30 = 0x48`) — `[정적 — 결과서 해석]` 으로 둔다.
- ③ (보탬, run93 과 이어짐) **test.exe 타이머 큐 Timer2 넷은 맡길 때 신호 0 이 코드로 선다** — `KiInitializeTimer2 +0xa` 가 헤더 첫 8B 를 0 으로 쓰고 (`_DISPATCHER_HEADER.SignalState` `+0x4` 포함), `TppInitializeTimerSubQueue` 는 `+0x4d` 만들기와 `+0xa0` 맡기기 사이에 `NtSetTimer2` 를 안 부른다. run93 의 "안 울림 `[추측: KeInitializeTimer2 안 읽음]`" 중 "만들 때 꺼짐" 은 `[정적]` 으로 오른다. "그 사이 다른 스레드가 그 핸들을 켜지 않았다" 만 `[추측]` 으로 남는다. **정정 아님** (run93 결론 그대로) — run93 문서는 안 고친다.
- ④ (보탬) run93 "배달 길 표" 의 Timer2 두 갈래 중 **test.exe 타이머 큐 타이머는 동기 갈래** (`KiExpireTimer2 +0xca == 0x19` → `+0x3a8` → `+0x3ad` 신호 −1) 를 탄다 (`Attributes 8` → `0x19`) `[정적]`.
- ⑤ (보탬) run78 과 맞물림 — 시계 틱 쪽 `KiCheckForTimerExpiration +0x3d5 KiSetDpcRequestFlag(PRCB+0x38bc, 8)` (run78) 과 걸기 쪽 `KiRequestTimer2Expiration +0x27` 이 **같은 비트 8 · 같은 PRCB 칸** 을 쓰고, 받는 쪽은 둘 다 `KiRetireDpcList +0x40d`. run78 은 KTIMER 표 (`KiTimerExpiration`) 만 따라갔고, 이번에 같은 DPC 안 `+0xcc6 KiTimer2Expiration` (시계 소유 CPU 일 때, `+0xca9`) 이 더해진다. 어긋나는 줄 없음 — **정정 없음**.
- ⑥ (결과서 고쳐 적음) 결과서 "다음 요청 후보 1" 의 "kernelbase PDB 가 없다 (`ms/sym/kernelbase.dll.pdb` 없음) → 먼저 `fetch_pdb.py`" — 서버엔 **`ms/sym/KernelBase.dll.pdb`** (대문자 K · B) 가 이미 있고, flow.md 기준의 kernelbase pdb (`72CCE506…`, run14 ~ run84) 가 그것이다 (flow-writer `ls`). 파일 이름 대소문자로 못 찾은 것으로 보인다 `[추측]`. 다만 `ms/disassemble/kernelbase/` 에 `_index.tsv` 는 없어서, 이름 확인은 `pdb_aliases.py --where KernelBase.dll …` 로.
- ⑦ (자리 바로 적음) 결과서 "`TppInitializeTimerSubQueue +0x33 XOR EDX,EDX` (Reserved1=0)" — `.asm` 의 `+0x33` 은 명령 경계가 아니다 (`+0x30 MOVZX R15D,R8B` · `+0x34 MOV RCX,RBX`). `XOR EDX,EDX` 는 **`+0x4b`** (call 바로 앞). 뜻 (Reserved1 = 0) 은 같다.
- ⑧ (보탬) `KeSetTimer2` 의 줄기 밖 callee 9개 (`KiTraceSetTimer2` · `KiUpdateTimer2Collections` · `PsTimerResolutionActive` 등) 는 `_calls.tsv` 에서 이름만 옮겼다.

## 아직 확인 필요

**동적** (결과서 "동적으로 확인해야 할 것" + 보탬. 이번 묶음은 VM 안 함, 대기열 번호는 오케스트레이터가 붙임):

- ① 즉시 만기가 `NtSetTimer2` 를 부른 스레드 위에서 도나 — 현재 CPU == `KiClockTimerOwner` 인지 (`KiRequestTimer2Expiration +0x39`). 같으면 `KiExpireTimer2` 콜스택 아래에 `KeSetTimer2+0x4a0` 근처가 보일 것 `[추측]`. **test.exe 로는 안 보일 수 있음** — 초기화 동안 `NtSetTimer2` 를 부르는 줄이 지도에 없다 (힙 GC 타이머는 만들기만 하고 안 켬, run69). 지난 시각으로 거는 샘플이 필요할 수 있음 (사용자 결정).
- ② 지난 시각 · 0 일 때 `+0x4e6 JA` 가 실제로 안 뛰나 (R14B = 1 → `+0x6bd`), 미래 값일 때 뛰나.
- ③ 아주 작은 상대 값 (−1) 이 정말 다음 틱까지 걸리나 (타이머 해상도에 따라 몇 ms).
- ④ Go 가 `NtCreateTimer2` 에 주는 `Attributes` (`r9d` bit 31 · bit 2) · DesiredAccess, 만든 직후 형 바이트 — Go 샘플 필요 (= run93 동적 ⑦, Q177).
- ⑤ 사용자 포인터가 틀렸을 때 `NtSetTimer2` status (SEH 가 있나).
- ⑥ `KiForceIdleState == 4` 일 때 요청이 빠지는 것 (정상 부팅에선 드묾 `[추측]`).
- ⑦ (보탬, **test.exe 로 볼 수 있음**) 타이머 큐 Timer2 넷의 형 바이트가 `0x19` · `+0x81` 이 `0x08` 인지 — `nt!KiInitializeTimer2` 반환 뒤 `db @r11 L1` · `db @r11+0x81 L3` (test.exe 조건, 콜스택 ret `TppInitializeTimerSubQueue+0x52`). Q177 ⑥ (맡김 6번 갈래) 과 같은 멈춤 묶음에 붙일 후보.
- 추천 멈춤점 (모든 프로세스에 걸리므로 test.exe 조건 필요):
  - `nt!NtSetTimer2` 진입: `poi(@rdx)` (DueTime) · `@r8` (있으면 `poi(@r8)`) · `@r9`. 반환 `eax`.
  - `nt!KeSetTimer2` 진입: `rcx` · `rdx` · `r8` · `r9`, `dq @rcx L0x11` (`_KTIMER2` `0x88`B).
  - `nt!KeSetTimer2+0x4e6`: `poi(@rbx+0x48)` vs `poi(0xfffff78000000008)`.
  - `nt!KiRequestTimer2Expiration` 진입 (ret `KeSetTimer2+0x6c2`) + `dd nt!KiClockTimerOwner L1` 과 현재 CPU 번호.
  - `nt!KiExpireTimer2` 진입 (ret `KiTimer2Expiration+0xdc`): `k` · `dd @rcx+4 L1`.
  - `nt!KiInitializeTimer2` 진입 `r9d`, `nt!NtCreateTimer2` 진입 `r9d` · `dd @rsp+0x28 L1`.

**정적** (결과서 "다음 요청 후보" + 보탬, 대기열 번호는 오케스트레이터가 붙임):

- ① kernelbase `CreateWaitableTimerExW` · `SetWaitableTimerEx` 가 `NtCreateTimer2` / `NtSetTimer2` 로 무엇을 넘기나 (Go 가 이 길이면). 단 이 API 가 `NtCreateTimer` (옛) 를 쓰는지 `NtCreateTimer2` 를 쓰는지부터. PDB 받기는 필요 없다 (위 "결과서와 다르게 적은 곳" ⑥).
- ② `NtCancelTimer2` · `KeCancelTimer2` — 취소 · Disable 과 맡긴 wait packet 의 신호.
- ③ `KiExpireTimer2` 주기 갈래 (`+0x2b4 ~ +0x358`) — 다음 만기 = "이전 만기 + Period" 인가 "지금 + Period" 인가 (`+0x2d6` 의 `R10` 출처).
- ④ `KiRetireDpcList +0xbca ~ +0xcc6` 사이 갈림 (`+0xc51` · `+0xc5d` · `+0xc76`) 과 `KiSendSoftwareInterrupt` — 요청 한 번으로 타이머 DPC 가 정말 도는 조건.
- ⑤ ntdll 스레드 풀 타이머 켜기 (`TpSetTimerEx` → `NtSetTimer2` 인자) — run69 남은 정적 ① 과 같은 자리. 스레드 풀이 거는 `DueTime` · `Period` · `Parameters` 모양.
- (Q176b 그대로) `NtCreateWaitCompletionPacket` · `NtRemoveIoCompletionEx` · `ObGetAssociatedWaitObject` · 닫기 처리기.

## flow.md · 짐 지도에 반영한 것

- flow.md:
  - 기준: ntkrnlmp · ntdll pdb 줄에 run95 몫, 근거 로그 run95 줄 (사진 안 씀).
  - 흐름도 앞 글: run95 문단 — **그림은 안 고침** (test.exe 초기화 길에 `NtSetTimer2` 가 없고, `NtCreateTimer2` 는 이미 2.4 글에만 있는 syscall 의 커널 안쪽).
  - 2.4 run62 절: (run95) 줄 하나 — 타이머 큐 Timer2 동기 · NoWake · 만들 때 신호 0. 2.4 머리 run 목록에 run95.
  - 3장: run78 절에 (run95) 줄 하나, "기다림 완료 패킷" 절의 `[추측]` 줄 · 남은 것 줄에 (run95) 표시, 새 절 **"Timer2 걸기 · 만들기"** (3장 끝).
  - 4장: run93 정적 줄 ② (해결됨 → run95), run93 동적 줄 4 (일부 → run95), run95 새 줄 셋.
  - 5장: run 표 · 03_sprint 요청서 번호 run95 줄.
- 짐 지도 (짐 지도 바퀴 아님 — 커널 개체 칸 메모만, **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**): 기준 run95 줄 · 판 목록 (쉰한째판), 4장 `TppPoolpGlobalPool` 짝 줄, 7.6 run62 "커널 개체 10 핸들" 아래 Timer2 메모, 7.6.9 커널 개체 줄, 8장 두 표.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점** `[추측]`: `KeSetTimer2` 는 "지금" 을 두 번 읽는다 (`+0x6c` 만기 계산 · `+0x4d8` 비교). 그 사이에 멈춤점을 걸면 멈춘 동안 `InterruptTime` 이 흘러, **미래 (작은 상대) 값이 "지난 값" 갈래 (R14B = 1) 로 바뀔 수 있다.** `+0x4e6` 을 보려면 그 자리에만 걸고, `+0x6c ~ +0x4d8` 사이엔 걸지 말 것.
- **함정 후보** (→ windbg-kd-runbook pitfalls 에 추가 제안): `KiExpireTimer2` · `KiTimer2Expiration` · `KiRetireDpcList` 는 **DPC 안에서** 돈다 — 그 순간의 프로세스는 우연히 그 CPU 에 있던 프로세스라 `$proc` (test.exe) 조건이 안 맞는다 `[추측: DPC 일반 성질]`. test.exe 의 타이머만 보려면 `@rcx` (타이머 주소) 를 `NtCreateTimer2` 쪽에서 먼저 적어 두고 그 주소로 조건을 건다.
- 도구 (결과서 "스크립트 문제"): 없음. `alias:RtlCopyVolatileMemory+1` 은 같은 주소 이름 2개 (위 "환경").

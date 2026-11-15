# 2026-11-15 run93 — 기다림 완료 패킷 (wait completion packet) 커널 본문: 맡김 · 배달 · 취소 · 꺼내기 (Q175, PE_emulator 요청 33 — 한 바퀴로 끝): **패킷 객체의 앞 `0x30` 바이트가 그대로 `KWAIT_BLOCK` 이다. 대상이 아직 안 울렸으면 `NtAssociateWaitCompletionPacket` 은 네 값 (Key · Apc · IoStatus · Information) 과 대상 · 포트 포인터를 패킷에 저장하고, 패킷 자체를 대상의 기다림 목록 꼬리에 단 뒤 status 0 · `AlreadySignaled` 0 을 돌려준다 (`STATUS_PENDING` 아님). 대상이 울리면 신호를 내는 함수 (`KeSetEvent` · `KiTimerWaitTest` · `KiExpireTimer2` · `KeReleaseSemaphoreEx`) 가 기다림 목록을 돌다 `WaitType 2` 블록을 만나 `BlockState 5` 로 바꾸고 **패킷 자체를** `KiInsertQueueInternal` 로 IOCP 줄에 넣은 뒤, 스레드를 깨울 때와 똑같이 신호를 1 쓴다 (자동 리셋 · 동기 타이머 꺼짐, 세마포어 −1). 패킷은 한 번 쓰면 끝 — `IoRemoveIoCompletion` 이 꺼낼 때 (`+0x342`) 나 취소가 뺄 때 연결 표시 `+0x68` 를 꺼야 다시 맡길 수 있다 (그 전엔 `0xC00000EF`). 취소는 안 울림 · 줄에 듦 (TRUE) 이면 0, 줄에 듦 (FALSE) 이면 `0x103`, 안 맡김이면 `0xC0000120`. 빈 줄에서 시간 0 으로 꺼내면 `0x102`. test.exe 초기화의 맡김 6번 (Timer2 4 · 이벤트 2) 은 모두 방금 만든 꺼진 대상이라 "안 울림" 갈래일 것이다 `[추측]`.** `NtAssociateWaitCompletionPacket` `+0x62/+0xdc/+0x115 ObReferenceObjectByHandle` (패킷 1 · 포트 2 · 대상 `0x100000`) → `+0x130 ObGetAssociatedWaitObject` → `+0x160 KeAcquireSpinLockRaiseToDpc(패킷+0x60)` → `+0x16c` 연결됨? · `+0x17c` 닫힘? → `+0x186 ~ +0x1c7` 패킷 칸 쓰기 → `+0x1d0 KeRegisterObjectNotification` (`+0x4a` `SignalState > 0` ? `+0x71 KiWaitSatisfyOther` · `+0x80 KiInsertQueueInternal` · `+0xa3 KiExitDispatcher` → 1 : `+0xac ~ +0xb6` 기다림 목록 꼬리 → 0) → `+0x1e0 KeReleaseSpinLock` → `+0x22d` `*AlreadySignaled` ← AL → 0

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### 요청 33 결론 (run93)

- **"안 울린 대상" 연결은 그냥 걸고 성공한다** `[정적]`.
  - status **0** (`STATUS_PENDING` 아님) — 안 울림 · 이미 울림 둘 다 `+0x1fa ~ +0x202` 로 나가고, `EDI` 는 셋째 `ObReferenceObjectByHandle` 의 0 그대로 (`+0x11a` → `+0x96 MOV EAX,EDI` → `+0xad RET`).
  - `AlreadySignaled` (8번째 인자, NULL 아니면) 에 **0** 을 쓴다 (`KeRegisterObjectNotification +0xde XOR AL,AL` → `+0x1d5 MOV SIL,AL` → `+0x22d MOV [RDX],SIL`). 이미 울렸으면 1 (`+0xa8 MOV AL,1`).
  - 쓰는 때는 연결을 다 끝내고 패킷 락을 푼 뒤 (`+0x1e0`).
- **배달은 신호를 내는 쪽이 그 자리에서 한다.** 넣는 함수는 `KiInsertQueueInternal` 하나 (`KiInsertQueue` · `KeInsertQueueEx` 아님). **줄 항목 = 패킷 객체 자체** (복사 없음).
- **네 값은 가공 없이 그대로** — 인자 4 · 5 · 6 · 7 → 패킷 `+0x30` · `+0x38` · `+0x48` (4바이트) · `+0x40` → 꺼낼 때 출력 항목 `Key +0` · `Apc +8` · `Status +0x10` · `Information +0x18`.
- **패킷은 한 번 쓰면 다시 맡겨야 한다.** 줄에 들어간 뒤에도 연결 표시 `+0x68` 는 1 로 남는다. 꺼내기 · 취소 전에 또 맡기면 `0xC00000EF`.
- **신호를 쓴다** — 스레드를 깨울 때와 결과가 같다. 다만 `KiWaitSatisfy*` 를 안 거치고 신호 함수가 직접 `SUB [SignalState],1` 한다 (맡길 때 이미 울렸으면 `KiWaitSatisfyOther` 가 같은 일).

### 팀원 몸과 맞나 · 틀리나 (결과서 · 요청서가 적은 팀원 몸 기준 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

| 갈래 | 지금 팀원 몸 | 진짜 (26100.9457) `[정적]` | 판정 |
|---|---|---|---|
| 맡기기 — 핸들 참조 순서 · 접근 | 패킷 1 → 포트 2 → 대상 `0x100000` | 같음 (`+0x5d` · `+0xcf` · `+0x108`) | **맞음** |
| 맡기기 — 뮤턴트 · 큐 · 기다릴 수 없는 대상 | `0xC00000F1` | `0xC00000F1` (`+0x13d` · `+0x14b` → `+0x242`) | **맞음** |
| 맡기기 — 이미 연결된 패킷 | `0xC00000EF` | `0xC00000EF` (`+0x16c/+0x171` → `+0x24c`) | **맞음** |
| 맡기기 — 이미 울림 | `AlreadySignaled` 1 · 신호 씀 · 바로 줄에 | 같음 (`KeRegisterObjectNotification +0x4a ~ +0xa8`) | **맞음** |
| 맡기기 — 안 울림 | 코드 `TODO(VM)` / 기본 대답 `wait_completion_async_unsupported` | status 0 · `AlreadySignaled` 0 · 대상 기다림 목록에 패킷을 닮 | `TODO` 답은 **"0 / 0"**. 기본 대답 (unsupported) 은 **틀림** |
| 맡기기 — 네 값 | "쪽지 넷은 기억하지 않음" | 패킷에 **저장** (`+0x192 ~ +0x1b5`) | **틀림 1** — 배달을 하려면 저장해야 함 |
| 맡기기 — `AlreadySignaled` 쓰기 실패 (틀린 사용자 주소) | 접근 위반을 돌려줌 | 연결은 이미 걸렸고 status 0 (예외를 삼킴) | **틀림 2 (작음)** — 바이트 해석이라 동적 확인 후보 |
| 맡기기 — 닫힌 패킷 (`+0x69`) | 멈춤 | `0xC000A003` (`+0x17c/+0x180` → `+0x207`) | 빈 곳 — **값 확정** (이름은 모름) |
| 배달 — 이벤트 · 타이머 · Timer2 · 세마포어 | "공사 중" | 아래 "배달 길 표" | **빈 곳 전부** |
| 꺼내기 — 패킷 항목 | (`nt_io_completion` 쪽, 요청서에 없음) | 연결 표시 끄기 (`+0x342`) · 포트 칸 0 (`+0x346`) · 참조 셋 풀기 | **해야 함** — 안 하면 다음 맡기기가 `0xC00000EF` |
| 꺼내기 — 빈 줄 · 시간 0 | `wait_would_block` | `0x102` (`STATUS_TIMEOUT`) + 항목 0개 | **틀림** (기다림 있으면 재움 — 그건 맞음) |
| 취소 — 안 맡김 (또는 이미 꺼냄) | `0xC0000120` | `0xC0000120` (`NtCancel +0x11c` / `+0x132`) | **맞음** — `0xC0000120` 은 이 갈래 하나뿐 |
| 취소 — 줄에 듦, TRUE | 0 | 0 (`IopCancel +0xb9 KeRemoveQueueEntry` → `+0x42`) | **맞음** |
| 취소 — 줄에 듦, FALSE | `0x103` | `0x103` (`NtCancel +0x14e ~ +0x158`) | **맞음** |
| 취소 — 안 울림 (기다림 목록에 있음) | 멈춤 (`wait_completion_waiting_state_unsupported`) | **0** — 목록에서 떼고 참조 셋 풀기 | 빈 곳. **`0xC0000120` 을 주면 틀림** |

### 원문 질문이 어디서 답해졌나

| 원문 요청 33 | 답한 곳 | 한 줄 |
|---|---|---|
| 1 안 울림 status · `AlreadySignaled` · 거는 꼴 · 네 값 · 재맡기 | 아래 "패킷 칸 표" · "맡기기 차례 표" | 0 / 0. 패킷 = 기다림 블록 (`WaitType 2` · `BlockState 4` · `Thread` 칸 = 포트). 네 값 그대로. 재맡기 `0xC00000EF` |
| 2 배달 함수 · 순서 · 신호 · 한 번 쓰기 · 넣는 함수 | 아래 "배달 길 표" | 신호 함수가 직접 → `KiInsertQueueInternal`. 신호 1 씀. 한 번 쓰기. 패킷 = 줄 항목 |
| 3 취소 갈래별 status | 아래 "취소 표" | 안 울림 0 · 줄에 듦 TRUE 0 / FALSE `0x103` · 안 맡김 `0xC0000120` |
| 4 `IoRemoveIoCompletion` 빈 줄 · 패킷 갈래 | 아래 "꺼내기" | 시간 0 → `0x102` + 0개. 항목 `+0x10` 바이트 2 = 패킷 |
| 5 최소 조건 표 | 아래 "최소 조건 표" | — |

### 최소 조건 표 (결과서 질문 5 — 에뮬레이터가 맡김 · 배달 · 취소를 진짜와 같게 하려면)

| 상황 | status | `AlreadySignaled` | 대상 신호 변화 | 줄에 들어가는 것 | 근거 |
|---|---|---|---|---|---|
| 맡기기: 이미 울림 (`SignalState > 0`) | 0 | **1** | 자동 리셋 이벤트 · 동기 타이머 · Timer2 동기 → 0, 세마포어 −1, 수동 이벤트 · 알림 타이머 · 프로세스 · 스레드 그대로 | **그 자리에서** 네 값 (인자 4 ~ 7 그대로). 기다리는 스레드가 있으면 바로 넘김. 연결 표시 켜짐 | `NtAssoc +0x1d0`, `KeRegisterObjectNotification +0x4a ~ +0xa8`, `KiWaitSatisfyOther +0x0 ~ +0x2e` |
| 맡기기: 안 울림 | 0 | **0** | 없음 | 아직 없음. **패킷에 네 값 저장** · 대상 기다림 목록 꼬리에 패킷 · 연결 표시 켜짐 | `KeRegisterObjectNotification +0xac ~ +0xde` |
| 나중에 울림 (이벤트 set · 타이머 만기 · 세마포어 놓기) | (신호 쪽 반환 — 패킷과 무관) | — | 동기 계열 1 → 0, 세마포어 패킷마다 −1, 알림 계열 그대로 | 저장해 둔 네 값 그대로. 패킷은 기다림 목록에서 떨어짐. 연결 표시는 켜진 채. 자동 리셋은 첫 블록 하나만 | 아래 "배달 길 표" |
| 꺼내기 (`IoRemoveIoCompletion`) | 0 (항목 1) | — | — | 네 값 그대로 출력. **연결 표시 끄기 · 참조 셋 풀기** | `IoRemoveIoCompletion +0x2e3 ~ +0x43d` |
| 취소: 안 울림 | **0** | — | 없음 | 목록에서 떼고 연결 끄기, 네 값 버림 | `IopCancel +0x35 ~ +0x90` |
| 취소: 줄에 듦, TRUE | **0** | — | 없음 | 줄에서 빼고 연결 끄기 (항목이 줄에 안 달려 있으면 `0x103`) | `IopCancel +0xb9`, `KeRemoveQueueEntry +0x3b` |
| 취소: 줄에 듦, FALSE | **`0x103`** | — | 없음 | 줄에 남음, 연결 유지 | `NtCancel +0x14e ~ +0x158` |
| 취소: 안 맡김 (또는 이미 꺼냄) | **`0xC0000120`** | — | — | — | `NtCancel +0x11c` / `+0x132` |
| 맡기기: 이미 연결된 패킷 | `0xC00000EF` | 안 씀 | — | — | `NtAssoc +0x16c → +0x24c` |
| 맡기기: 뮤턴트 · 큐 · 기다릴 수 없음 | `0xC00000F1` | 안 씀 | — | — | `NtAssoc +0x13d ~ +0x14b → +0x242` |
| 맡기기: 닫힌 패킷 | `0xC000A003` | 안 씀 | — | — | `NtAssoc +0x17c → +0x207` |
| 패킷 핸들 닫기 | (반환 없음) | — | — | 취소 (TRUE) 와 같음 + `+0x69` 켜기 | `IopCloseWaitCompletionPacket +0x42` · `+0xbe` |
| 빈 줄에서 꺼내기, 시간 0 | **`0x102`** (항목 0) | — | — | — | `IoRemoveIoCompletion +0xe4 ~ +0x109`, `KeRemoveQueueEx +0x3d3` |

### PE_emulator 확인거리 (우리 대기열 아님)

- 엔진 `NtAssociateWaitCompletionPacket` 기본 대답을 **"0 + `AlreadySignaled` 0 + 대상에 걸어 둠"** 으로. 걸어 둔 패킷은 대상이 울리는 syscall (`NtSetEvent` · `NtReleaseSemaphore` · Timer2 만기) 에서 IOCP 줄로 옮겨야 Go 의 `NtRemoveIoCompletionEx` 가 깬다 (막힌 자리 `seq 2256 → 2260`).
- 엔진 `NtRemoveIoCompletionEx` 가 패킷 항목을 꺼낼 때 연결 표시를 끄는지 — 안 끄면 Go 가 같은 패킷을 다시 맡길 때 `0xC00000EF` 가 나올 것 `[추측: Go 가 패킷을 다시 쓰는지는 안 봄]`.
- Go 타이머 (`NtCreateTimer2`) 가 동기인지 알림인지에 따라 만기 뒤 신호가 꺼지는지가 갈린다 (`KiExpireTimer2 +0x3ad` / `+0x707`) — 아래 동적.

## 환경

- 26100.9457, **정적만** (VM 실행 없음). test.exe 갈래 대입에 run44 사진을 쓰지 않았다 (패킷 · Timer2 · 이벤트 객체는 사진에 없음).
- 정적: ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1` — 다시 analyze 안 함, `export` 5번 (결과서).
  - 질문 1 · 3 (depth 1): `NtAssociateWaitCompletionPacket` (+ `$filt$0`) · `NtCancelWaitCompletionPacket` (+ 조각 `FUN_1406edca6`) · `IopCancelWaitCompletionPacket` · `IopCloseWaitCompletionPacket` · `ObGetAssociatedWaitObject` + callee 중 `KeRegisterObjectNotification` · `KiDeregisterObjectWaitBlock` · `KeRemoveQueueEntry` 등 19개.
  - 질문 2 · 4 (depth 0): `KiInsertQueueInternal` · `KiInsertQueue` · `KeInsertQueueEx` · `KeReleaseSemaphore` · `KeReleaseSemaphoreEx` · `KiWaitSatisfyOther` · `IoRemoveIoCompletion` · `KeRemoveQueueEx` · `NtSetEvent` · `NtSetEventEx` · `NtReleaseSemaphore` · `NtSetTimer2` · `NtCreateTimer2` · `ExpSetTimer2` · `KiExpireTimer2` · `KiProcessExpiredTimerList` · `KiTimer2Expiration`.
  - 있던 것 그대로: `KeSetEvent` · `KiTryUnwaitThread` · `KiTimerWaitTest` · `KiTimerExpiration` (run77 · run78).
- 이름: `_index.tsv` 에 요청서 이름 전부 있음. `IopCancelWaitCompletionPacket +0x35` 의 callee 는 `_calls.tsv` 에 `alias:KiDeregisterObjectWaitBlock+1` 로 찍혔지만, 같은 주소 (rva `0x32bb2c`) 이름은 C++ 꾸민 이름 `?KiDeregisterObjectWaitBlock@@YAEPEAXPEAU_KWAIT_BLOCK@@@Z` 하나뿐이다 (`pdb_aliases.py --where ntoskrnl.exe KiDeregisterObjectWaitBlock` — flow-writer 확인, `_index.tsv` 이름도 같음). 이 문서는 `KiDeregisterObjectWaitBlock` 으로 쓴다.
- **PDB 에 패킷 객체 타입이 없다** (`pdb_struct.py --list ntoskrnl.exe 'WAIT_COMPLETION|WaitCompletion'` → 0개, 결과서). 아래 패킷 칸 이름은 코드로 읽은 **우리 이름** `[추측: 이름]`, 오프셋 · 쓰임은 `[정적]`.
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치, PDB GUID 일치.

## 목적

- PE_emulator 요청 33 (병목 38 — Go 런타임 IOCP 31 · `NtSetTimer2` 뒤 끝 7). 사용자 결정 2026-11-15: Q104 VM 단계 앞 멈춤 자리에서 끼워 **지금 먼저**, 정적만. `NtCreateWaitCompletionPacket` · 객체 형식 등록은 범위 밖, 배달 길은 패킷 기다림 블록 갈래만.
- 안 울린 대상에 맡길 때 status · `AlreadySignaled` · 거는 꼴 · 네 값, 배달 함수 사슬과 신호 쓰기, 취소 갈래별 status, `IoRemoveIoCompletion` 빈 줄 갈림, 최소 조건 표.
- 관련: run62 · run69 — test.exe 초기화에서 맡김이 6번 있다. 이번은 그 커널 쪽 본문.

## 방법

- 정적: `_workspace/sast/89_wait-packet.md` — ntoskrnl, 질문 1 · 3 은 `^NtAssociateWaitCompletionPacket$` · `^NtCancelWaitCompletionPacket$` · `^IopCancelWaitCompletionPacket$` 등 depth 1, 질문 2 · 4 는 신호 함수 · 줄 함수 depth 0 (줄기만). 세마포어 · Timer2 caller 는 지도 `_map/*_edges.tsv` 에서 찾음.
- 결과서: `_workspace/sast/89_wait-packet_result.md` (**분석성공** — 못 정한 것은 타이머 종류 · 예외 착지 · 이름 · 형식 등록, 모두 "동적으로 확인할 것").
- flow-writer 가 다시 본 것 (`.asm`):
  - ntoskrnl `NtAssociateWaitCompletionPacket` 전부 + `$filt$0`, `KeRegisterObjectNotification` 전부, `KiWaitSatisfyOther` 전부, `IopCancelWaitCompletionPacket` 전부, `NtCancelWaitCompletionPacket` 의 결과서 줄 (`+0x2e` · `+0x57 ~ +0x5c` · `+0x75` · `+0x9e ~ +0xdf` · `+0x11c` · `+0x132 ~ +0x15b`), `KiDeregisterObjectWaitBlock +0x3b ~ +0x65`, `KeRemoveQueueEntry +0x0 ~ +0x90`, `IopCloseWaitCompletionPacket +0x0` · `+0x42` · `+0xbe/+0xc3`, `KiInsertQueueInternal` 의 결과서 줄 (`+0x14` · `+0x60` · `+0x74` · `+0x8a` · `+0x139` · `+0x175` · `+0x191 ~ +0x1b4`).
  - 배달 줄: `KeSetEvent +0x8d` · `+0xd1` · `+0x1b3/+0x1ba` · `+0x313` · `+0x31a` · `+0x4fe/+0x506` · `+0x53b ~ +0x55c` · `+0x587 ~ +0x5a0`, `KiTimerWaitTest +0x1e6` · `+0x201` · `+0x211` · `+0x238` · `+0x266 ~ +0x27b` · `+0x2c7` · `+0x2d9 ~ +0x2ee` · `+0x32a` · `+0x341 ~ +0x348`, `KiExpireTimer2 +0xca ~ +0xd2` · `+0x394 ~ +0x3ad` · `+0x6f2 ~ +0x707`, `KeReleaseSemaphoreEx +0x233 ~ +0x24d`, `KeReleaseSemaphore +0x22c/+0x231`.
  - 꺼내기 줄: `IoRemoveIoCompletion +0x8d/+0x92` · `+0xaf/+0xb7` · `+0xe4 ~ +0x11b` · `+0x1c9/+0x1cb` · `+0x204` · `+0x212` · `+0x28c/+0x290` · `+0x2e3` · `+0x2fb` · `+0x342/+0x346` · `+0x3bc` · `+0x403` · `+0x43d`, `KeRemoveQueueEx +0x356/+0x35a` · `+0x3d3` · `+0x3e9` · `+0x786 ~ +0x7a4` · `+0x891/+0x896` · `+0xb36`.
  - test.exe 대입 (보탬 ①): ntdll `TppInitializeTimerSubQueue +0x0 ~ +0xea`, `RtlpWnfRegisterTpNotification +0x0 ~ +0x8f`, `EtwpRegisterTpNotificationOnce +0x11 ~ +0x37`.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtAssociateWaitCompletionPacket` | `+0x62` | `+0x67` | `ObReferenceObjectByHandle` | 패킷, 접근 1 (`+0x5d`), 형 `IopWaitCompletionPacketObjectType` (`+0x43`) | [정적] |
| 〃 | `+0xdc` | `+0xe1` | `ObReferenceObjectByHandle` | 포트, 접근 2 (`+0xcf`), 형 `IoCompletionObjectType` (`+0xaf`) | [정적] |
| 〃 | `+0x115` | `+0x11a` | `ObReferenceObjectByHandle` | 대상, 접근 `0x100000` (`+0x108`), 형 없음 (`+0x105`). 반환을 `[RSP+0x34]` 에도 (`+0x121`) | [정적] |
| 〃 | `+0x130` | `+0x135` | `ObGetAssociatedWaitObject` | NULL → `+0x13d JZ +0x242`. 형 `& 0x7f` 가 2 · 4 면 `+0x14b JZ +0x242` (`0xC00000F1`) | [정적] |
| 〃 | `+0x160` | `+0x165` | `KeAcquireSpinLockRaiseToDpc` | 패킷 `+0x60`. 이어 `+0x16c` `[+0x68]` ≠ 0 → `+0x24c` · `+0x17c` `[+0x69]` ≠ 0 → `+0x207` | [정적] |
| 〃 | **`+0x1d0`** | **`+0x1d5`** | **`KeRegisterObjectNotification`** | `(대상 헤더, 포트, 패킷)`. 앞 `+0x186 ~ +0x1c7` 에서 패킷 칸 쓰기 | [정적] |
| 〃 | `+0x1e0` | `+0x1e5` | `KeReleaseSpinLock` | 그 뒤 `+0x1f8` · `+0x22d` `AlreadySignaled` 쓰기 | [정적] |
| 〃 | `+0x26f` · `+0x281` · `+0x293` | `+0x274` · `+0x286` · `+0x298` | `ObfDereferenceObjectWithTag` | 실패 길만 (성공 길은 `+0x1fa ~ +0x200` 에서 세 칸을 0 으로 지워 안 풂) | [정적] |
| `nt!KeRegisterObjectNotification` | `+0x45` | `+0x4a` | `KiAcquireKobjectLockSafe` | 대상 락 (IRQL 2, `+0x31 ~ +0x35`) | [정적] |
| 〃 | `+0x71` | `+0x76` | `KiWaitSatisfyOther` | `+0x4a CMP [대상+4],0` · `+0x4e JG` (이미 울림) | [정적] |
| 〃 | `+0x80` | `+0x85` | `KiInsertQueueInternal` | `(포트, 패킷)`, 앞 `+0x79 BlockState = 5` | [정적] |
| 〃 | `+0xa3` | `+0xa8` | `KiExitDispatcher` | 그 뒤 `+0xa8 MOV AL,1` | [정적] |
| `nt!KeSetEvent` | `+0x54e` | `+0x553` | `KiInsertQueueInternal` | 자동 리셋 갈래, `+0x53b CMP AL,2` → `+0x543 BlockState = 5`, 뒤 `+0x553 SUB [SignalState],1` | [정적] |
| 〃 | `+0x59b` | `+0x5a0` | `KiInsertQueueInternal` | 수동 리셋 갈래, `+0x587 CMP AL,2` → `+0x58f` | [정적] |
| `nt!KiTimerWaitTest` | `+0x276` | `+0x27b` | `KiInsertQueueInternal` | 동기 타이머, `+0x266` → `+0x26b`, 뒤 `+0x27b SUB [SignalState],1` | [정적] |
| 〃 | `+0x2e9` | `+0x2ee` | `KiInsertQueueInternal` | 알림 타이머, `+0x2d9` → `+0x2de` | [정적] |
| `nt!KiExpireTimer2` | `+0x3a8` | `+0x3ad` | `KiInsertQueueInternal` | Timer2 동기 (`+0xca CMP AL,0x19`), `+0x394` → `+0x39d`, 뒤 `+0x3ad SUB [SignalState],1` | [정적] |
| 〃 | `+0x702` | `+0x707` | `KiInsertQueueInternal` | Timer2 알림 (`+0xcc JNZ`), `+0x6f2` → `+0x6f7` | [정적] |
| `nt!KeReleaseSemaphoreEx` | `+0x248` | `+0x24d` | `KiInsertQueueInternal` | `+0x233` → `+0x23c`, 뒤 `+0x24d SUB [count],1` | [정적] |
| `nt!KeReleaseSemaphore` | `+0x22c` | `+0x231` | `KiInsertQueueInternal` | 드라이버용, 같은 꼴 (`+0x231 SUB`) | [정적] |
| `nt!KiInsertQueueInternal` | `+0x1af` | `+0x1b4` | `KiWakeOtherQueueWaiters` | 줄이 비어 있었고 기다리는 스레드가 있을 때 | [정적] |
| `nt!NtCancelWaitCompletionPacket` | `+0x57` | `+0x5c` | `ObReferenceObjectByHandle` | 패킷, 접근 1 (`+0x2e`) | [정적] |
| 〃 | `+0x75` | `+0x7a` | `KeAcquireSpinLockRaiseToDpc` | 패킷 락 → 포트 `[+0x58]` 읽기 · 참조 (`~ +0x93`) | [정적] |
| 〃 | `+0xac` | `+0xb1` | `KeAcquireInStackQueuedSpinLock` | 포트 쪽 락 (`+0x9e` `[+0x58]` == 0 이면 `+0xa1 JZ +0x11c` 로 먼저 빠짐) | [정적] |
| 〃 | `+0xb4` | `+0xb9` | `KeAcquireSpinLockRaiseToDpc` | 패킷 락 다시. `+0xc1 JZ +0x132` (`[+0x68]` == 0) · `+0xcb` 포트 바뀜 → `+0x13c` | [정적] |
| 〃 | **`+0xda`** | **`+0xdf`** | **`IopCancelWaitCompletionPacket`** | `(패킷, RemoveSignaledPacket, 이전 IRQL)`. FALSE 면 `+0x14e ~ +0x158 CMOVNZ EDI,0x103` | [정적] |
| `nt!IopCancelWaitCompletionPacket` | `+0x2a` | `+0x2f` | `ObGetAssociatedWaitObject` | 대상 `[+0x50]` | [정적] |
| 〃 | **`+0x35`** | **`+0x3a`** | **`KiDeregisterObjectWaitBlock`** | TRUE (`+0x3e`) → `+0x42` 정리. 같은 주소 이름 1개 (C++ 꾸민 이름) | [정적] |
| 〃 | `+0x4f` | `+0x54` | `KxReleaseSpinLock` | 패킷 락 | [정적] |
| 〃 | `+0x77` · `+0x81` · `+0x8b` | `+0x7c` · `+0x86` · `+0x90` | `ObfDereferenceObjectWithTag` | 대상 · 포트 · 패킷 | [정적] |
| 〃 | `+0xb9` | `+0xbe` | `KeRemoveQueueEntry` | `RemoveSignaledPacket` ≠ 0 (`+0xae` · `+0xb1 JZ +0xc2`) | [정적] |
| `nt!IopCloseWaitCompletionPacket` | `+0xbe` | `+0xc3` | `IopCancelWaitCompletionPacket` | `DL = 1`, 앞 `+0x42 [+0x69] = 1`. `+0x0 CMP R9,1` 일 때만 몸이 돈다 | [정적] |
| `nt!IoRemoveIoCompletion` | `+0x8d` | `+0x92` | `KeRemoveQueueEx` | 비면 안에서 재움 (`KeRemoveQueueEx +0x891/+0x896 KiCommitThreadWait`) | [정적] |
| `nt!NtSetEvent` → `NtSetEventEx` | `+0x7` → `+0xb9` | `+0xc` → `+0xbe` | `NtSetEventEx` → `KeSetEvent` | 이벤트 배달 입구 (결과서) | [정적] |
| `nt!NtReleaseSemaphore` | `+0x94` | `+0x99` | `KeReleaseSemaphoreEx` | 세마포어 배달 입구 (결과서) | [정적] |
| `nt!KiTimer2Expiration` | `+0xd7` | `+0xdc` | `KiExpireTimer2` | Timer2 만기 입구. 거는 쪽 `NtSetTimer2 +0x9 → ExpSetTimer2 +0x17d → KeSetTimer2` (결과서) | [정적] |
| `nt!KiTimerExpiration` → `KiProcessExpiredTimerList` | `+0x20e` 등 넷 → `+0xba` · `+0x483` | `+0x213` 등 → `+0xbf` · `+0x488` | `KiProcessExpiredTimerList` → `KiTimerWaitTest` | KTIMER 만기 입구 (run78 · 결과서) | [정적] |

- `NtSetEvent` · `NtReleaseSemaphore` · `KiTimer2Expiration` · `KiTimerExpiration` 입구 줄은 flow-writer 가 다시 안 읽었다 (결과서 그대로).
- 지도에서 `KiInsertQueueInternal` 을 부르는 caller 는 더 있다 (`KeSetProcess +0xed` · `KeTerminateThread +0x2c4` · `KeSignalGate +0x10f` · `KeReleaseMutantEx +0x37c` · `KiResumeThread +0x23c`, 결과서) — 따라가지 않았다.

### 패킷 칸 표 (PDB 에 타입 없음 — 이름은 우리 이름 `[추측: 이름]`, 오프셋 · 쓰임 `[정적]`)

| 오프셋 | 내용 | 쓰는 곳 |
|---|---|---|
| `+0x00 ~ +0x2f` | **`KWAIT_BLOCK`** — `WaitListEntry` `+0x0` · `WaitType` `+0x10` = **2** · `BlockState` `+0x11` = **4** · `Thread` 칸 `+0x18` = **IO 완료 포트** · `Object` `+0x20` = 대상 헤더. `WaitKey` · `SpareLong` · `SparePtr` 은 안 건드림 | `KeRegisterObjectNotification +0x17` (`MOV word [R8+0x10],0x402`) · `+0x1e` · `+0x28` |
| `+0x30` | KeyContext (인자 4) | `NtAssoc +0x192` |
| `+0x38` | ApcContext (인자 5) | `+0x19e` |
| `+0x40` | IoStatusInformation (인자 7) | `+0x1b5` |
| `+0x48` | IoStatus (인자 6, **4바이트만**) | `+0x1a9` |
| `+0x50` | 대상 객체 (참조 1 쥠) | `+0x1be` |
| `+0x58` | IO 완료 포트 객체 (참조 1 쥠) | `+0x1c7` · 0 으로: `IopCancel +0x42` · `IoRemoveIoCompletion +0x346` |
| `+0x60` | 패킷 스핀락 | `NtAssoc +0x154 ~ +0x165` |
| `+0x68` | BOOLEAN 연결됨 | 1: `NtAssoc +0x186` · 0: `IopCancel +0x4b` · `IoRemoveIoCompletion +0x342` |
| `+0x69` | BOOLEAN 닫힘 (맡기기 거부) | 1: `IopCloseWaitCompletionPacket +0x42` · 읽기: `NtAssoc +0x17c` |

- `BlockState` 숫자: 4 = 걸려 있음, 5 = 울려서 넘김 / 떼어짐 (코드로 읽은 뜻). run77 은 `_KWAIT_BLOCK_STATE` 4 를 `WaitBlockActive` 로 적었다 — 칸 타입이 바이트라 연결은 `[추측: 이름]` (run77 그대로).
- **참조 셋 (패킷 · 포트 · 대상) 은 연결이 풀릴 때까지 쥔다** — 성공 길은 `+0x1fa ~ +0x200` 에서 `R12` · `R14` · `ESI` 를 0 으로 지워 마지막 정리 (`+0x7b ~ +0x90` 의 풀기) 를 건너뛴다. 푸는 곳은 취소 (`IopCancel +0x77 ~ +0x8b`) 와 꺼내기 (`IoRemoveIoCompletion +0x3bc` · `+0x403` · `+0x43d`).

### 맡기기 차례 표 (`NtAssociateWaitCompletionPacket`, 인자 1 패킷 · 2 포트 · 3 대상 · 4 ~ 7 네 값 · 8 `AlreadySignaled`)

| # | 자리 | 하는 일 | 갈림 |
|---|---|---|---|
| 1 | `+0x2c ~ +0x3f` | `PreviousMode` (`KTHREAD +0x232`) → `[RSP+0x30]` · `[RSP+0x31]` | — |
| 2 | `+0x62/+0x67` · `+0xdc/+0xe1` · `+0x115/+0x11a` | 핸들 셋 참조 (패킷 1 · 포트 2 · 대상 `0x100000`) | 음수 → `+0x72` 정리 → 그 status |
| 3 | `+0x130/+0x135` → `+0x143 ~ +0x14b` | 대상의 기다림 헤더 찾기 · 형 검사 | NULL · 형 2 (뮤턴트) · 4 (큐) → `+0x242` **`0xC00000F1`** |
| 4 | `+0x160/+0x165` | 패킷 락 (`+0x60`) | — |
| 5 | `+0x16c ~ +0x171` | 연결됨 (`+0x68`) ? | 1 → `+0x24c` **`0xC00000EF`** |
| 6 | `+0x17c ~ +0x180` | 닫힘 (`+0x69`) ? | 1 → `+0x207` **`0xC000A003`** |
| 7 | `+0x186 ~ +0x1c7` | `+0x68` = 1 · 네 값 · 대상 · 포트 저장 | — |
| 8 | **`+0x1d0/+0x1d5`** | **`KeRegisterObjectNotification(헤더, 포트, 패킷)`** — 아래 | AL = 이미 울림 (1) / 안 울림 (0) |
| 9 | `+0x1e0/+0x1e5` | 패킷 락 풀기 | — |
| 10 | `+0x1ed ~ +0x1f8` → `+0x211 ~ +0x22d` | 8번째 인자 NULL 아니면: 사용자 모드면 1바이트 읽고-쓰기 확인 (`+0x229` · `+0x22b`, 위쪽 주소는 `0x7fffffff0000` 으로 눌러 일부러 틀리게) → **`+0x22d MOV [RDX],SIL`** | 예외 → `$filt$0` (사용자 모드일 때만 잡음) → 착지 `+0x232 ~ +0x241` (아래 보탬 ②) |
| 11 | `+0x1fa ~ +0x202` → `+0x72` → `+0x96 ~ +0xad` | 참조 셋 안 풂 (지움) → `EAX = EDI` (0) | **status 0** |

`KeRegisterObjectNotification(헤더 RCX, 포트 RDX, 패킷 R8)` (245B):

| 자리 | 하는 일 |
|---|---|
| `+0x17` · `+0x1e` · `+0x28` | 블록 꼴 (`WaitType 2` · `BlockState 4`) · `Thread` 칸 = 포트 · `Object` = 헤더 |
| `+0x2c ~ +0x45/+0x4a` | IRQL 2 · 대상 락 (`KiAcquireKobjectLockSafe`) |
| `+0x4a / +0x4e` | `CMP [헤더+4],0` · `JG +0x6e` — `SignalState > 0` 이면 이미 울림 |
| 이미 울림 `+0x71/+0x76` → `+0x79` → `+0x80/+0x85` → `+0x85` → `+0xa3/+0xa8` → `+0xa8` | `KiWaitSatisfyOther(헤더)` → `BlockState = 5` → `KiInsertQueueInternal(포트, 패킷)` → 락 풀기 → `KiExitDispatcher` → **AL = 1** |
| 안 울림 `+0x50 ~ +0x5b` → `+0xac ~ +0xb6` → `+0xba` → `+0xd6 ~ +0xde` | 목록 꼬리 검사 (깨졌으면 `INT 0x29`) → **패킷 `WaitListEntry` 를 헤더 `+8` (`WaitListHead`) 꼬리에** → 락 풀기 → IRQL 되돌림 → **AL = 0** |

`KiWaitSatisfyOther(헤더)` (45B) — 형 `& 7 == 1` (자동 리셋 이벤트 1 · 동기 타이머 9 · Timer2 동기 `0x19`) 면 `SignalState = 0` (`+0x1e`), 형 `& 0x7f == 5` (세마포어) 면 `SignalState--` (`+0x29`), 그 밖은 그대로 (`+0x1a`).

### 배달 길 표 — 대상이 나중에 울릴 때 (결과서 질문 2)

공통: 신호 함수가 자기 기다림 목록을 돌며 블록 `+0x10` (`WaitType`) 를 본다 — 1 = 스레드 (`KiTryUnwaitThread` 꼴), **2 = 패킷** (`BlockState = 5` → `KiInsertQueueInternal(블록+0x18 의 포트, 블록)`), 4 = DPC, 0 = WaitAll (`KiTryUnwaitThread`). **패킷은 `KiTryUnwaitThread` · `KiSatisfyThreadWait` 로 안 간다** (그 함수는 `+0x18` 을 스레드로 쓰므로 앞에서 갈림).

| 신호 내는 길 | 갈래 (함수 +오프셋) | 넣는 call/ret | 신호 · 목록 |
|---|---|---|---|
| 이벤트 자동 리셋 (형 1) | `KeSetEvent +0x31a` (`WaitType ≠ 1`) → `+0x53b` (`== 2`) → `+0x543` | `+0x54e/+0x553` | **`+0x553 SUB [SignalState],1`** (1 → 0) → `+0x55c JZ` 로 순회 끝 (**첫 블록 하나만**). 그 블록은 `+0x313` 에서 이미 떼어 둠 |
| 이벤트 수동 리셋 (형 0) | `KeSetEvent +0x1b3/+0x1ba` (`≠ 1`) → `+0x587` (`== 2`) → `+0x58f` | `+0x59b/+0x5a0` | 신호 그대로 (`+0x8d` 의 1). 목록은 끝에 통째로 비움 (`+0x4fe ~ +0x506`) |
| KTIMER 동기 (형 9) | `KiTimerWaitTest +0x1e6` (`≠ 8`) → `+0x2c7` → `+0x211 ~ +0x238` (하나 떼기) → `+0x266` (`== 2`) → `+0x26b` | `+0x276/+0x27b` | **`+0x27b SUB [SignalState],1`** → 0 이면 `+0x348` 끝 |
| KTIMER 알림 (형 8) | `KiTimerWaitTest +0x201` (`≠ 1`) → `+0x2d9` (`== 2`) → `+0x2de` | `+0x2e9/+0x2ee` → `+0x32a` | 신호 그대로. 목록 비움 `+0x341 ~ +0x345` |
| Timer2 동기 (형 `0x19`) | `KiExpireTimer2 +0xca` (`== 0x19`) → `+0xd2` 순회 → `+0x394` (`== 2`) → `+0x39d` | `+0x3a8/+0x3ad` | **`+0x3ad SUB [SignalState],1`** |
| Timer2 알림 (형 `0x18`) | `KiExpireTimer2 +0xcc JNZ` → `+0x6f2` (`== 2`) → `+0x6f7` | `+0x702/+0x707` → `+0x450` | 신호 그대로 |
| 세마포어 놓기 | `KeReleaseSemaphoreEx +0x233` (`== 2`) → `+0x23c` | `+0x248/+0x24d` | **`+0x24d SUB [count],1`** — 패킷 하나가 count 1 을 먹음. 0 이면 끝 |

`KiInsertQueueInternal(포트 RCX, 항목 RDX)` (834B):

- `+0x14 AND [항목],0` — 항목 `Flink` = 0 ("줄에 안 달림" 표시. `KeRemoveQueueEntry +0x3b/+0x3f` 가 이 칸으로 가름).
- `+0x60` 포트 락 → 기다리는 스레드가 있고 `CurrentCount < MaximumCount` 면 (`+0x74 ~ +0x8a`) 그 스레드에 **바로 넘김** (`+0x139` 스레드 `+0xc8` = 항목) — 이때 항목은 줄에 안 달림.
- 아니면 `+0x175` 근처 `SignalState` (줄 항목 수) +1 · `+0x191 ~ +0x19b` 포트 `EntryListHead` 꼬리에 항목 · 줄이 비어 있었고 기다리는 스레드가 있으면 `+0x1af KiWakeOtherQueueWaiters`.

### 취소 표 (`NtCancelWaitCompletionPacket(패킷 핸들, RemoveSignaledPacket)` → `IopCancelWaitCompletionPacket`)

`KiDeregisterObjectWaitBlock(대상, 블록)` 은 대상 락 안에서 `BlockState == 4` 일 때만 목록에서 떼고 (`+0x42 ~ +0x61`) 5 로 바꿔 (`+0x65`) TRUE.

| 상황 | 갈림 자리 | status | 한 일 |
|---|---|---|---|
| 안 울림 (목록에 있음, `BlockState 4`) | `IopCancel +0x3e` TRUE → `+0x42` | **0** (`RemoveSignaledPacket` 무관) | 목록에서 떼기 · `+0x58` = 0 (`+0x42`) · `+0x68` = 0 (`+0x4b`) · 락 풀기 · 참조 셋 풀기 (`+0x77 ~ +0x8b`) · 반환 TRUE (`+0x90`). 대상 신호 안 건드림 |
| 줄에 듦, TRUE | `+0xae` → `+0xb9/+0xbe KeRemoveQueueEntry` TRUE → `+0xc0 JNZ +0x42` | **0** | 줄에서 빼기 (`KeRemoveQueueEntry +0x41` 항목 수 −1 · `+0x5d ~ +0x63` 고리 풀기) + 위와 같은 정리. 네 값 버림 |
| 줄에 듦, TRUE 인데 항목이 줄에 안 달림 (스레드에 바로 넘겨짐 등) | `KeRemoveQueueEntry +0x3b/+0x3f` (`[항목] == 0`) → FALSE | **`0x103`** | 아무것도 안 바꿈 |
| 줄에 듦, FALSE | `IopCancel +0xae/+0xb1 JZ +0xc2` → FALSE → `NtCancel +0x14e ~ +0x158` (`[+0x68] ≠ 0` → `CMOVNZ EDI,0x103`) | **`0x103`** | 아무것도 안 바꿈 (나중에 꺼내기가 꺼냄) |
| 안 맡김 (또는 이미 꺼냄) | `NtCancel +0x9e/+0xa1` (`[+0x58] == 0`) → `+0x11c` · 또는 `+0xc1` (`[+0x68] == 0`) → `+0x132` | **`0xC0000120`** | 참조만 풀고 끝 |
| 두 락 사이 포트가 바뀜 | `NtCancel +0xcb` → `+0x13c` | `0xC000A003` (`+0x142`) | 락만 풀고 끝 |
| 핸들 문제 | `+0x57/+0x5c` | `ObReferenceObjectByHandle` 값 | — |

- **패킷 핸들을 닫으면** `IopCloseWaitCompletionPacket` 이 `+0x69` = 1 (`+0x42`) 하고 `IopCancelWaitCompletionPacket(…, TRUE)` (`+0xbe`) — 기다림 목록이든 줄이든 걸린 패킷이 조용히 취소된다. 몸이 도는 조건 `+0x0 CMP R9,1` 의 뜻 ("그 프로세스의 마지막 핸들") 과 이 함수가 객체 형식의 닫기 처리기로 등록됐는지는 `[추측]` (결과서, 형식 등록은 범위 밖).

### 꺼내기 — `IoRemoveIoCompletion` 갈림 자리만 (결과서 질문 4)

- `+0x8d/+0x92 KeRemoveQueueEx` — 비었고 기다려야 하면 그 안에서 잠 (`+0x891/+0x896 KiCommitThreadWait`). 깨우는 쪽은 위 `KiInsertQueueInternal` 의 "바로 넘김".
- **시간 0 + 빈 줄**: `KeRemoveQueueEx +0x356/+0x35a` → `+0x3d3` 값 `0x102` → `+0x3e9` 출력 첫 칸에 (Alertable 아닌 길 `+0x786 ~ +0x78d` 도 `+0x3d3` 로, `+0xb36` 도 `0x102`). 사용자 APC 가 와 있고 사용자 모드 기다림이면 대신 `0xc0` (`+0x793 ~ +0x7a4`). `IoRemoveIoCompletion` 은 첫 칸이 `0x80` · `0xc0` · `0x101` · `0x102` 면 "항목 아님" (`+0xe4 ~ +0x109` → `+0x28c`) → 항목 수 0 (`+0xb7`), **status = 그 값** (`0x102` = `STATUS_TIMEOUT`).
- **항목 가르기** `+0x10f MOVZX EAX,byte [항목+0x10]` (= 패킷이면 `WaitType` 칸):
  - 0 → IRP (`+0x11b` 항목 − `0xa8` → `IopCompleteRequest`)
  - **2 → 기다림 완료 패킷** (`+0x1c9/+0x1cb JZ +0x2e3`): 네 칸 읽기 (`+0x2e3 ~ +0x2fb`) → 패킷 락 → **`+0x342 [+0x68] = 0` · `+0x346 [+0x58] = 0`** → 락 풀기 → **참조 셋 풀기** (대상 `+0x3bc` · 포트 `+0x403` · 패킷 `+0x43d`)
  - 그 밖 (1) → 작은 완료 패킷 (`IopFreeMiniCompletionPacket`)
- 출력 항목 하나 `0x20`B: `Key +0` · `Apc +8` · `Status +0x10` (4B) · `Information +0x18` (`+0x204` · `+0x212`).

### 호출 관계

```
NtAssociateWaitCompletionPacket (패킷, 포트, 대상, Key, Apc, IoStatus, Info, *AlreadySignaled)      [정적]
 ├─ (1) +0x62 · +0xdc · +0x115  ObReferenceObjectByHandle ×3   (1 · 2 · 0x100000)
 ├─ (2) +0x130  ObGetAssociatedWaitObject           NULL · 뮤턴트 · 큐 → 0xC00000F1
 ├─ (3) +0x160  KeAcquireSpinLockRaiseToDpc(패킷+0x60)
 │        +0x16c [+0x68] → 0xC00000EF · +0x17c [+0x69] → 0xC000A003
 │        +0x186 ~ +0x1c7  +0x68 = 1 · +0x30/+0x38/+0x48/+0x40 = 네 값 · +0x50 대상 · +0x58 포트
 ├─ (4) +0x1d0  KeRegisterObjectNotification(헤더, 포트, 패킷)
 │        │  +0x17 WaitType 2 · BlockState 4 · +0x1e Thread 칸 = 포트 · +0x28 Object = 헤더
 │        ├─ (1) +0x45  KiAcquireKobjectLockSafe
 │        │  +0x4a SignalState > 0 ?
 │        │   ├ 예:  ├─ (2) +0x71  KiWaitSatisfyOther      자동 리셋 · 동기 타이머 → 0 / 세마포어 −1
 │        │   │      │  +0x79 BlockState = 5
 │        │   │      ├─ (3) +0x80  KiInsertQueueInternal(포트, 패킷)
 │        │   │      └─ (4) +0xa3  KiExitDispatcher   → AL = 1
 │        │   └ 아니오: +0xac ~ +0xb6  패킷 → 헤더 WaitListHead 꼬리   → AL = 0
 ├─ (5) +0x1e0  KeReleaseSpinLock
 │  +0x22d  *AlreadySignaled = AL       (예외 → $filt$0 → +0x232 착지 → status 0)
 └─ +0x96  EAX = 0

[나중에 대상이 울림]  기다림 목록을 돌며 WaitType 2 를 만나면                                    [정적]
 KeSetEvent        +0x54e (자동, 뒤 SUB) · +0x59b (수동)        ┐
 KiTimerWaitTest   +0x276 (동기, 뒤 SUB) · +0x2e9 (알림)        │
 KiExpireTimer2    +0x3a8 (동기, 뒤 SUB) · +0x702 (알림)        ├─▶ KiInsertQueueInternal(포트, 패킷)
 KeReleaseSemaphoreEx +0x248 (뒤 SUB)                           │      +0x14 Flink = 0 → 스레드에 바로 / 줄 꼬리
 KeRegisterObjectNotification +0x80 (맡길 때 이미 울림)         ┘

IoRemoveIoCompletion                                                                            [정적]
 ├─ (1) +0x8d  KeRemoveQueueEx            빈 줄 · 시간 0 → 0x102 / 기다림 → +0x891 KiCommitThreadWait
 │  +0x10f [항목+0x10] == 2 → +0x2e3 네 칸 → +0x342 [+0x68] = 0 · +0x346 [+0x58] = 0 → 참조 셋 풀기
 └─ (그 밖) IRP · 작은 완료 패킷

NtCancelWaitCompletionPacket (패킷, RemoveSignaledPacket)                                       [정적]
 │  +0x9e [+0x58] == 0 → 0xC0000120 · +0xc1 [+0x68] == 0 → 0xC0000120
 └─ (1) +0xda  IopCancelWaitCompletionPacket(패킷, Remove, IRQL)
          ├─ (1) +0x2a  ObGetAssociatedWaitObject([+0x50])
          ├─ (2) +0x35  KiDeregisterObjectWaitBlock(대상, 패킷)   BlockState 4 면 떼고 TRUE
          │        TRUE → +0x42 정리 (+0x58 · +0x68 = 0, 참조 셋) → 0
          └─ (3) +0xb9  KeRemoveQueueEntry(포트, 패킷)            Remove ≠ 0 일 때. TRUE → 정리 → 0
   FALSE → +0x14e ~ +0x158 [+0x68] ≠ 0 → 0x103
IopCloseWaitCompletionPacket  +0x42 [+0x69] = 1 → +0xbe IopCancelWaitCompletionPacket(…, TRUE)
```

### test.exe 초기화의 맡김 6번에 대입 (flow-writer 보탬 ①)

run62 · run69 의 6번은 **모두 `TpSetWaitEx` 를 거치지는 않는다** — 4번은 타이머 큐 만들기 (`TppInitializeTimerSubQueue +0xa0`) 가 직접 부르고, 2번이 `TpSetWaitEx +0x168` 이다.

| # | 부르는 자리 | 대상 | 맡길 때 대상 상태 | 갈래 |
|---|---|---|---|---|
| 1 · 2 | 기본 풀 `TpAllocPoolInternal +0x38e` → `TppInitializeTimerQueue +0x2c` · `+0x44` → **`TppInitializeTimerSubQueue +0xa0/+0xa5`** (run62 표 6) | 바로 앞 `+0x4d NtCreateTimer2` 의 출력 칸 (`[sub+0x18]`) — `+0x74 MOV R8,[RBX]` (RBX = `sub+0x18`, `+0x1c`) `[정적]`. 포트 = `[풀+0x40]` (`+0x9c`) | 만들고 아직 안 켬 → 안 울림 `[추측: KeInitializeTimer2 안 읽음]` | 안 울림 (Timer2 기다림 목록에 패킷) `[추측]` |
| 3 | WNF `RtlpWnfRegisterTpNotification +0x92` → `TpSetWaitEx +0x168/+0x16d` (run62 표 12) | `+0x28 NtCreateEvent(0x1f0003, 종류 1, 처음 0)` — `+0x16 R9D = 1` · `+0x1f [RSP+0x20] = 0` `[정적]` | 만들 때 꺼짐. 다만 `+0x77 NtSetWnfProcessNotificationEvent` 로 커널에 먼저 넘긴 뒤 맡긴다 — 그 사이 커널이 울릴 수 있음 `[추측]` | 안 울림일 것 `[추측]` |
| 4 | ETW Once `EtwpRegisterTpNotificationOnce +0x64` → `TpSetWaitEx +0x168/+0x16d` (run62 표 16) | `+0x32 NtCreateEvent(0x1f0003, 종류 1, 처음 0)` — `+0x15 R9D = 1` · `+0x2d [RSP+0x20] = 0` `[정적]` | 꺼짐. 커널에 넘기는 `NtTraceControl(0x1b)` 는 맡긴 **뒤** (`+0x8c`) `[정적: 순서]` | **안 울림** `[정적+추측: 그 사이 이 핸들을 쓰는 곳 없음]` |
| 5 · 6 | 로더 풀 `TpAllocPoolInternal +0x38e` → … → `TppInitializeTimerSubQueue +0xa0/+0xa5` (run69 표 7 ~ 12) | Timer2 (1 · 2 와 같은 함수) | 안 울림 `[추측]` | 안 울림 `[추측]` |

- 그래서 초기화 동안 커널에는 **패킷 6개가 대상 6개 (Timer2 4 · 동기 이벤트 2) 의 기다림 목록에 걸린 채** 남을 것이다 `[추측]` — 패킷마다 `+0x68` = 1, 참조 셋. 은퇴 (P2) 까지 대상이 울렸는지 (예: 힙 GC 타이머가 타이머 큐 Timer2 를 켬 — run69 `RtlpHpGCTimerSchedule`) 는 모른다.
- `TppInitializeTimerSubQueue` 는 맡기기 status 를 안 본다 (sast/64 결과서 "`+0xa0/+0xa5` 는 안 본다"). `TpSetWaitEx` 는 부호만 (`+0x16d`) · 이미 울림 바이트를 읽는다 (`+0x2e5`, 요청서 "이미 아는 것").
- 맡김 네 값 (1 · 2 · 5 · 6): Key = `sub+0x28` (`+0x89`), Apc = `풀+0x70` (`+0x81` · `+0x97`), IoStatus = 0 (`+0x92`), Information = 셋째 인자 바이트 (`+0x30` · `+0x8d`), `AlreadySignaled` = 지역 `[RSP+0x60]` (`+0x77` · `+0x7c`) `[정적]`.

## 확인된 것

- `[정적]` 안 울린 대상 맡기기: status 0 · `AlreadySignaled` 0 · 패킷 = 기다림 블록을 대상 `WaitListHead` 꼬리에 (`KeRegisterObjectNotification +0xac ~ +0xde`). 이미 울림: `KiWaitSatisfyOther` → `KiInsertQueueInternal` → 1.
- `[정적]` 패킷 앞 `0x30` = `KWAIT_BLOCK` (`WaitType 2` · `BlockState 4` · `Thread` 칸 = 포트). 네 값 · 대상 · 포트 · 연결 표시는 `+0x30 ~ +0x68`.
- `[정적]` 배달: 이벤트 (자동 · 수동) · KTIMER (동기 · 알림) · Timer2 (동기 · 알림) · 세마포어가 모두 `KiInsertQueueInternal` 로 **패킷 자체를** 넣는다. 동기 계열 · 세마포어는 넣은 뒤 `SUB [SignalState],1`.
- `[정적]` 연결 표시 `+0x68` 를 끄는 곳은 둘 — 취소 (`IopCancel +0x4b`) · 꺼내기 (`IoRemoveIoCompletion +0x342`). 그 전 재맡기는 `0xC00000EF`.
- `[정적]` 취소 status: 안 울림 0 · 줄에 듦 TRUE 0 (안 달렸으면 `0x103`) · FALSE `0x103` · 안 맡김 `0xC0000120` · 포트 바뀜 `0xC000A003`.
- `[정적]` 빈 줄 시간 0 → `0x102` + 항목 0. 패킷 항목은 `+0x10` 바이트 2 로 가름.
- `[정적]` test.exe 맡김 6번의 대상은 Timer2 4 · 동기 이벤트 2 (모두 방금 만든 것, 이벤트는 처음 상태 0).

## 결과서와 다르게 적은 곳 · 보탠 것

- ① (보탬) **test.exe 맡김 6번의 자리 · 대상** — 요청서는 "유저 쪽 `TpSetWaitEx` 가 `NtAssociateWaitCompletionPacket` 을 부르는 자리 (맡김 6번)" 라 적었지만, `TpSetWaitEx` 를 거치는 건 2번 (WNF · ETW) 이고 4번은 `TppInitializeTimerSubQueue +0xa0` 이 직접 부른다 (run62 표 6 · run69 표 7 ~ 12 와 같음). 대상 · 처음 상태는 위 "대입" 표 (`TppInitializeTimerSubQueue +0x74` · `RtlpWnfRegisterTpNotification +0x16` · `+0x1f` · `EtwpRegisterTpNotificationOnce +0x15` · `+0x2d`). 결론 (안 울림 갈래) 은 `[추측]`.
- ② (결과서 그대로, 표시만 낮춤) **`AlreadySignaled` 쓰기 예외의 착지** — `.asm` 은 `+0x232 ~ +0x241` 을 "읽지 않은 틈" 으로만 보인다. 결과서가 원본 바이트로 `[RSP+0x32] → R13B · [RSP+0x33] → R15B · [RSP+0x34] → EDI · JMP` (→ `+0x1fa`) 로 읽었다 — flow-writer 는 바이트를 다시 안 풀었다. 그래서 "status 0 으로 끝남" 은 `[정적 — 결과서 바이트 해석]` 으로 둔다 (동적 확인 후보).
- ③ (보탬) **run77 · run78 과 이어짐** — run77 은 `KeSetEvent` 의 블록 갈래를 "`WaitType 0` 블록만 `KiTryUnwaitThread`" 로, run78 은 `KiTimerWaitTest +0x32a KiTryUnwaitThread` 만 적었다. 이번에 같은 함수 안 `WaitType 2` 갈래 (`KeSetEvent +0x53b` · `+0x587`, `KiTimerWaitTest +0x266` · `+0x2d9`) 가 더해진다. 두 run 의 내용과 어긋나는 줄은 없다 — **정정 없음**.
- ④ (보탬) `KiInsertQueueInternal +0x175` 는 `MOV R8D,[RDI+4]` 다 — 결과서 "`+0x175 SignalState++`" 는 그 근처에서 올린다는 뜻으로 읽었다 (올리는 명령 줄은 flow-writer 가 안 찾음). 결론 (항목 수 +1) 은 결과서 그대로.

## 아직 확인 필요

**정적** (결과서 "다음 요청 후보" 1 ~ 4 + flow-writer 보탬 — 대기열 번호는 오케스트레이터가 붙임):

- ① `NtCreateWaitCompletionPacket` · `IopFreeWaitCompletionPacket` — 패킷 객체 크기, 만들 때 `KWAIT_BLOCK` 앞 칸 · `WaitKey` (`+0x12`) · `+0x60` 락 · `+0x68` / `+0x69` 처음 값 (팀원 몸 `TODO` 의 `GenericAll` 값도).
- ② `NtSetTimer2` → `ExpSetTimer2` → `KeSetTimer2` — 이미 지난 시각이면 그 자리에서 `KiExpireTimer2` 로 가나 (Go: 타이머 걸고 바로 맡김). `NtCreateTimer2` 의 동기 / 알림 인자 — 그리고 (보탬) `TppInitializeTimerSubQueue +0x37 R9D = 8` · `+0x44 0x100002` 이 어느 쪽인지.
- ③ `NtRemoveIoCompletionEx` → `IoRemoveIoCompletion` 인자 대응과 `KeRemoveQueueEx` 대기 몸 (APC · Alertable · 깨울 때 항목 건네기) — 팀원 `nt_io_completion` 용.
- ④ `ObGetAssociatedWaitObject` 가 대상 형 (파일 · 프로세스 · 스레드 · IOCP 등) 마다 어떤 헤더를 고르나 (`+0x2b ~ +0x88`, 형 정보 칸 `+0x20` · `+0xb0` · `+0xb4` · `+0xb6`).
- ⑤ (보탬) `IopCloseWaitCompletionPacket` 이 패킷 객체 형식의 닫기 처리기로 등록되는 곳 (형식 만들기 — 이번 범위 밖) 과 `0xC000A003` 이름. 결과서는 동적으로 넘겼지만 등록 자리는 정적으로 찾을 수 있을 것 `[추측]`.

**동적** (결과서 "동적분석으로 확인해야 할 것" + 보탬. 이번 묶음은 VM 안 함):

- ⑥ (보탬, **test.exe 로 볼 수 있음**) 초기화 맡김 6번이 정말 "안 울림" 갈래인지 — `nt!KeRegisterObjectNotification` 반환 AL 이 6번 다 0 인지 (`nt!NtAssociateWaitCompletionPacket+0x1d5` 에서 `r al`, test.exe 조건). 그리고 은퇴 (P2) 때 6 패킷의 `+0x68` · `+0x11` (`BlockState`) — 대상이 이미 울렸나. Q104 VM 단계에 붙일 후보 (결정은 오케스트레이터 · 사용자).
- ⑦ Go 가 만든 Timer2 가 동기인지 알림인지 (`KiExpireTimer2 +0x3ad` / `+0x707` 중 어느 쪽을 타나) — Go 샘플 필요 (test.exe 아님), 샘플 고르기는 사용자 결정.
- ⑧ `AlreadySignaled` 가 틀린 사용자 주소일 때 실제로 status 0 이 나오는지 (위 ②).
- ⑨ 패킷 객체 실제 크기 · `WaitKey` 처음 값 (정적 ① 이 먼저).
- ⑩ 스레드에 바로 넘겨진 항목을 취소가 못 빼서 `0x103` 이 되는 경우 (정적으론 갈래만).
- 추천 멈춤점 (커널 멈춤점은 모든 프로세스에 걸리므로 test.exe 조건 필요):
  - `nt!KeRegisterObjectNotification` 반환 (`AL`) · 들어올 때 `rdx` (포트) · `r8` (패킷).
  - `nt!KiInsertQueueInternal` 에서 `[rdx+0x10]` 바이트가 2 일 때만 — 콜스택 ret 이 `KeSetEvent+0x553` / `+0x5a0` · `KiTimerWaitTest+0x27b` / `+0x2ee` · `KiExpireTimer2+0x3ad` / `+0x707` · `KeReleaseSemaphoreEx+0x24d` · `KeRegisterObjectNotification+0x85` 중 어느 것인지.
  - `nt!IopCancelWaitCompletionPacket` 반환 (`AL`) · `nt!NtCancelWaitCompletionPacket` 반환 (`eax`).
  - 패킷 덤프 `dq @r8 L0xe` — `+0x30 ~ +0x5f` · `+0x68` / `+0x69` 가 위 칸 표와 같은지.

## flow.md · 짐 지도에 반영한 것

- flow.md:
  - 기준: ntkrnlmp pdb 줄에 run93 몫, 근거 로그 run93 줄 (사진 안 씀).
  - 흐름도 앞 글: run93 문단 — **그림은 안 고침** (test.exe 길은 그대로 — 맡김 6번은 이미 그림 밖 글 (2.4 run62 · run69 절) 에 있는 syscall 이고, 이번은 그 커널 안쪽).
  - 2.4 run62 절: (run93) 줄 하나 — 맡김 6번의 커널 쪽 · 대입 표 가리킴. 2.4 머리 run 목록에 run93.
  - 3장: run77 절 · run78 절에 (run93) `WaitType 2` 갈래 한 줄씩, 새 절 **"기다림 완료 패킷"** (bcrypt 절 뒤, 3장 끝).
  - 4장: run93 새 줄 셋 (동적 · 정적 · PE_emulator 쪽 · 보탠 것).
  - 5장: run 표 · 03_sprint 요청서 번호 run93 줄.
- 짐 지도 (짐 지도 바퀴 — 패킷 · Timer2 · 이벤트 객체는 커널 개체라 체크리스트 밖. **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**):
  - 이미 있는 줄에 (run93) 메모만 — 4장 `TppPoolpGlobalPool` · `LdrpThreadPool` 짝 줄 (대기 완료 패킷이 대상 기다림 목록에 걸림), 6장 "TP 개체 만들기" 부품 줄 (맡기기의 커널 쪽 사슬), 7.6 run62 "커널 개체 10 핸들", 7.6.9 커널 개체 줄.
  - 기준 run93 줄 · 판 목록 (마흔아홉째판), 8장 두 표.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 디버거 칸을 읽는 줄은 없다 `[정적: 읽은 줄 안에서]`. 다만 커널 멈춤점으로 `KiInsertQueueInternal` · `KeSetEvent` 처럼 모든 프로세스가 지나는 함수에 서면, 멈춘 사이 Timer2 만기 · 일꾼 깨움 순서가 바뀔 수 있다 `[추측]` — 조건부 멈춤점 + `g` 로 빨리 지나갈 것.
- **함정 후보** (→ windbg-kd-runbook pitfalls 에 추가 제안): `KiInsertQueueInternal` 의 항목이 패킷인지 가르는 조건은 `by(@rdx+0x10) == 2` 인데, **IRP 항목도 같은 함수로 들어오고 그 자리 바이트 뜻이 다르다** (`IoRemoveIoCompletion +0x10f` — 0 = IRP, 1 = 작은 완료 패킷). 조건 없이 걸면 IOCP 쓰는 모든 프로세스에서 선다 → 프로세스 조건 (`$proc`) 과 바이트 조건을 같이.
- 도구 (결과서 "스크립트 문제"): 없음. `alias:…+1` 표시는 C++ 꾸민 이름 하나뿐인 경우 (run92 와 같은 꼴) — 결과에 영향 없음.

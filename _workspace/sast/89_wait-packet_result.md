# 89 wait-packet — 정적 분석 결과

- 요청서: `_workspace/sast/89_wait-packet.md` (Q175, PE_emulator 요청 33)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), PDB guid+age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음 — `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- analyze 는 다시 하지 않음 (`.gpr` + `_index.tsv` + `SetPdb:` 줄 다 있음, 도는 Ghidra 없음). `export` 만 5번 돌림.
- 뽑은 함수 (`ms/disassemble/ntoskrnl/*.asm`, `ms/decomp/ntoskrnl/*.c`):
  - 질문 1·3 (depth 1): `NtAssociateWaitCompletionPacket` (+`$filt$0`) · `NtCancelWaitCompletionPacket` (+조각 `FUN_1406edca6`) · `IopCancelWaitCompletionPacket` · `IopCloseWaitCompletionPacket` · `ObGetAssociatedWaitObject` + 그 callee 중 `KeRegisterObjectNotification` · `KiDeregisterObjectWaitBlock` · `KeRemoveQueueEntry` 등 (19개)
  - 질문 2·4 (depth 0): `KiInsertQueueInternal` · `KiInsertQueue` · `KeInsertQueueEx` · `KeReleaseSemaphore` · `KeReleaseSemaphoreEx` · `KiWaitSatisfyOther` · `IoRemoveIoCompletion` · `KeRemoveQueueEx` · `NtSetEvent` · `NtSetEventEx` · `NtReleaseSemaphore` · `NtSetTimer2` · `NtCreateTimer2` · `ExpSetTimer2` · `KiExpireTimer2` · `KiProcessExpiredTimerList` · `KiTimer2Expiration`
  - 이미 있던 것 그대로 씀: `KeSetEvent` · `KiTryUnwaitThread` · `KiTimerWaitTest` · `KiTimerExpiration`
  - 안 뽑은 것: `IopFreeWaitCompletionPacket` — 이 길 어디서도 안 부름 (객체 삭제 처리기로 보임 `[추측]`). `KiTryUnwaitThread`·`KiSatisfyThreadWait` 는 스레드 쪽.
  - 넓힌 이유: 요청서 표의 "기다림 블록 걸기 함수" 가 callee 로 `KeRegisterObjectNotification` 으로 확인됐고, 세마포어·타이머2 는 `KiInsertQueueInternal` 을 부르는 caller 지도 (`_map/*_edges.tsv`) 에서 찾았다. `KiTryUnwaitThread`·`KiSatisfyThreadWait` 는 스레드 쪽이라 새로 뽑지 않음 (아래 Q2 근거).
- 버전 확인: `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치 · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치 (ret_off `+0x807`). PDB GUID 일치. → 스크립트와 바이너리 둘 다 믿어도 됨.
- 이름 확인: `_index.tsv` 에 요청서 이름 전부 있음. `KiDeregisterObjectWaitBlock` 은 PDB 에 C++ 이름 `?KiDeregisterObjectWaitBlock@@YAEPEAXPEAU_KWAIT_BLOCK@@@Z` 로 있음 = `BOOLEAN f(void* 대상, KWAIT_BLOCK*)`. `IopCancelWaitCompletionPacket +0x35` 에서는 Ghidra 가 `alias:KiDeregisterObjectWaitBlock+1` 로 찍었지만 별칭 표엔 이름 하나뿐 (같은 함수).
- PDB 에 패킷 객체 형식 (`_..WAIT_COMPLETION..`) 이 **없다** (`pdb_struct.py --list ntoskrnl.exe 'WAIT_COMPLETION|WaitCompletion'` → 0개). 아래 패킷 칸 이름은 코드에서 읽은 **우리 이름** `[추측: 이름]`, 오프셋·쓰임은 `[정적]`.

## 먼저 보는 그림 (한 장 요약)

- 패킷 객체의 **앞 0x30 바이트가 그대로 `KWAIT_BLOCK`** 이다. 안 울린 대상에 맡기면 이 패킷 자체를 대상의 기다림 목록 (`WaitListHead`) 꼬리에 단다. 새 블록을 따로 만들지 않는다.
- 울리면 **그 패킷 자체를** IOCP 줄 (`KiInsertQueueInternal`) 에 넣는다. 줄 항목 = 패킷 객체.
- 꺼낼 때 (`IoRemoveIoCompletion`) 는 항목 `+0x10` 바이트가 `2` 이면 "패킷" 으로 보고 패킷 안 네 칸을 읽는다.
- 패킷은 **한 번 쓰고 나면 다시 맡겨야** 한다 (연결 표시 `+0x68` 가 꺼내기 / 취소 때 꺼진다).

패킷 칸 (코드로 읽은 것):

| 오프셋 | 내용 | 근거 |
|---|---|---|
| `+0x00~+0x2f` | `KWAIT_BLOCK` (`WaitListEntry`+0x0, `WaitType`+0x10, `BlockState`+0x11, `Thread/NotificationQueue`+0x18, `Object`+0x20) | `KeRegisterObjectNotification +0x17/+0x1e/+0x28` |
| `+0x30` | KeyContext (인자 4) | `NtAssociateWaitCompletionPacket +0x192` |
| `+0x38` | ApcContext (인자 5) | `+0x19e` |
| `+0x40` | IoStatusInformation (인자 7) | `+0x1b5` |
| `+0x48` | IoStatus (인자 6, 4바이트) | `+0x1a9` |
| `+0x50` | 대상 객체 포인터 (참조 1개 쥠) | `+0x1be` |
| `+0x58` | IO 완료 포트 객체 포인터 (참조 1개 쥠) | `+0x1c7` |
| `+0x60` | 패킷 스핀락 | `+0x154~+0x165` |
| `+0x68` | BOOLEAN "맡겨짐 (연결됨)" | `+0x186` (1 로 씀) · `IopCancelWaitCompletionPacket +0x4b` (0 으로 씀) · `IoRemoveIoCompletion +0x342` (0 으로 씀) |
| `+0x69` | BOOLEAN "닫힘" (맡기기 거부 표시) | `IopCloseWaitCompletionPacket +0x42` (1 로 씀) · `NtAssociate +0x17c/+0x180` (읽고 거부) |

## 질문별 답 [정적]

### 1. `NtAssociateWaitCompletionPacket` — 대상이 아직 안 울렸을 때

인자 순서 (decomp 와 `.asm` 스택 칸으로 같이 확인): 1 패킷 핸들 · 2 IO 완료 포트 핸들 · 3 대상 핸들 · 4 KeyContext · 5 ApcContext · 6 IoStatus · 7 IoStatusInformation · 8 AlreadySignaled (BOOLEAN 포인터).

- **status**: **0 (STATUS_SUCCESS)**. `STATUS_PENDING` 아님. 성공 길에서 `EDI` 는 세 번째 `ObReferenceObjectByHandle` 반환 (0) 그대로고, 안 울린 갈래와 이미 울린 갈래 **둘 다** 0 이다 (`+0x1fa~+0x202` 에서 `R12/R14/ESI` 만 0 으로 지우고 `JMP +0x72` 로 나감).
  - 근거: `NtAssociateWaitCompletionPacket.asm` `+0x11a` (`MOV EDI,EAX`) → `+0x1d0/+0x1d5` (`KeRegisterObjectNotification`) → `+0x1fa` → `+0x96` (`MOV EAX,EDI`, `RET +0xad`).
- **AlreadySignaled 칸 (포인터가 NULL 이 아닐 때)**: 안 울렸으면 **0 을 쓴다**, 울렸으면 **1 을 쓴다.** 쓰는 값은 `KeRegisterObjectNotification` 반환 AL (`+0x1d5 MOV SIL,AL` → `+0x22d MOV [RDX],SIL`).
  - 안 울림이면 `KeRegisterObjectNotification +0xde` (`XOR AL,AL`), 울림이면 `+0xa8` (`MOV AL,1`).
  - 연결을 **다 끝낸 뒤** (+0x1e0 스핀락 풀고) 쓴다. 사용자 모드 호출이면 먼저 1바이트 읽고-쓰기 확인 (`+0x229/+0x22b`, 위쪽 주소는 `0x7fffffff0000` 으로 눌러서 일부러 틀리게 함) 후 `+0x22d` 에서 씀.
  - 쓰다가 접근 위반이 나면: 필터 `$filt$0` 는 사용자 모드 (`[RBP+0x31]` ≠ 0) 일 때만 잡는다. 착지점은 `.asm` 에 "읽지 않은 틈 `+0x232~+0x241`" 로만 나와서 **원본 바이트로 읽었다**: `MOV R13B,[RSP+0x32]; MOV R15B,[RSP+0x33]; MOV EDI,[RSP+0x34]; JMP` → `+0x1fa` 로 돌아가는 꼴. `[RSP+0x34]` 는 `+0x121` 에서 쓴 세 번째 참조의 status (0). → **연결은 이미 걸렸고 status 는 0 (예외 코드를 돌려주지 않음)** `[정적 — 바이트 해석, 동적 확인 후보]`.
- **무엇을 걸어 두나**: 패킷 자체를 기다림 블록으로 쓴다 (`KeRegisterObjectNotification(대상 헤더, 포트, 패킷)`, `+0x1d0`).
  - `WaitType` (+0x10) = **2**, `BlockState` (+0x11) = **4** (한 번에 `MOV word [R8+0x10],0x402`, `KeRegisterObjectNotification +0x17`).
  - **`Thread` 칸 (+0x18) 에는 스레드 대신 IO 완료 포트 객체 포인터** (`NotificationQueue` 쪽 해석, `+0x1e`). `Object` 칸 (+0x20) = 대상 헤더 (`+0x28`). `WaitKey`·`SpareLong`·`SparePtr` 은 안 건드림.
  - 대상 헤더 `SignalState` (+4) 가 0 이하이면 (`+0x4a CMP [RDI+4],0; JG`) 대상의 `WaitListHead` (+8) 꼬리에 패킷 `WaitListEntry` 를 단다 (`+0xac~+0xb6`, 대상 객체 락 `KiAcquireKobjectLockSafe +0x45/+0x4a` 잡은 채, IRQL 2).
  - BlockState 숫자의 이름 (4 = ?, 5 = ?) 은 PDB 에 열거형이 없어 모른다. 코드로는 4 = "걸려 있음", 5 = "울려서 줄로 넘김 / 떼어짐" (`KiDeregisterObjectWaitBlock +0x3b~+0x65`, `KeSetEvent +0x543` 등이 4 인 것만 떼고 5 로 바꿈).
- **패킷 안에 저장하는 것**: 위 칸 표 그대로 — 네 값 + 대상 포인터 + 포트 포인터 + 연결 표시. 참조 세 개 (패킷·포트·대상) 는 **연결이 풀릴 때까지 쥔다** (성공 길에서 `ESI/R12/R14` 를 0 으로 지워 마지막 정리에서 안 풀게 함, `+0x1fa~+0x202`).
- **네 값이 나중에 그대로 나오나**: **그렇다, 가공 없음.** 인자 4~7 이 `+0x30/+0x38/+0x48/+0x40` 에 그대로 들어가고 (`+0x192~+0x1b5`), `IoRemoveIoCompletion` 이 꺼낼 때 그 칸을 그대로 출력 항목 (`Key`+0, `Apc`+8, `Status`+0x10, `Information`+0x18) 에 옮긴다 (`IoRemoveIoCompletion +0x2e3~+0x2fb`, `+0x204~+0x212`). `IoStatus` 는 4바이트만 저장 (그 자리에 `+0x4c` 는 안 씀).
- **이미 다른 대상에 맡겨진 패킷을 또 맡기면**: **`0xC00000EF` (STATUS_INVALID_PARAMETER_1)**. 패킷 스핀락 잡은 뒤 `+0x16c/+0x171` (`[패킷+0x68] ≠ 0`) → `+0x24c`.
  - 중요: 연결 표시는 **줄에 들어간 뒤에도, 꺼내기·취소 전까지 1 로 남는다.** 그래서 울린 패킷을 꺼내기 (`IoRemoveIoCompletion`) 전에 또 맡기면 같은 `0xC00000EF`.
  - 그 밖 거부: 대상에서 대기 객체를 못 찾으면 / 형이 뮤턴트 (2)·큐 (4) 면 **`0xC00000F1`** (STATUS_INVALID_PARAMETER_3) — `+0x13a~+0x14b` → `+0x242`. 패킷이 "닫힘 (+0x69)" 이면 **`0xC000A003`** (`+0x17c/+0x180` → `+0x207`, 이름은 모름). 핸들 문제는 `ObReferenceObjectByHandle` 의 값 (패킷 접근 `0x1`, 포트 `0x2` = `IO_COMPLETION_MODIFY_STATE`, 대상 `0x100000` = SYNCHRONIZE).
- **이미 울렸을 때 (팀원 몸이 하는 꼴 맞는지)**: 맞다. `KeRegisterObjectNotification +0x4a` 에서 `SignalState > 0` 이면 (`JG +0x6e`) 그 자리에서 `KiWaitSatisfyOther(대상)` (+0x71/+0x76) → `BlockState=5` (+0x79) → `KiInsertQueueInternal(포트, 패킷)` (+0x80/+0x85) → `KiExitDispatcher` (+0xa3/+0xa8) → 반환 1. 네 값은 패킷 칸에 먼저 저장돼 있다 (+0x192~+0x1c7 이 호출 `+0x1d0` 보다 앞).
  - `KiWaitSatisfyOther` (45 B): 형 번호 `& 7 == 1` (동기 계열: 자동 리셋 이벤트 1 · 동기 타이머 9 · 타이머2 동기 0x19) 면 `SignalState = 0`; 형 `& 0x7f == 5` (세마포어) 면 `SignalState--`; 그 밖 (수동 이벤트·알림 타이머·프로세스·스레드) 은 아무것도 안 바꿈 (`.asm` `+0x0~+0x2e`).
- **지금 팀원 몸**:
  - 순서 (패킷 1 → 포트 2 → 대상 `0x100000`) · 뮤턴트/큐 `0xC00000F1` · 이미 연결 `0xC00000EF` — **맞다.**
  - 이미 울림 (AlreadySignaled 1, 신호 소비, 줄에 넣음) — **맞다.**
  - 안 울림: status 0 · AlreadySignaled 0 — **맞다** (코드의 `TODO(VM)` 에 대한 답이 "0 / 0"). 단 요청서 설명의 "안 울림 = `wait_completion_async_unsupported`" 는 **진짜가 아니다** (진짜는 그냥 걸고 0).
  - **틀림 1**: "쪽지 넷은 기억하지 않음" — 진짜는 맡길 때 패킷에 **저장한다** (`+0x30/+0x38/+0x40/+0x48`). 배달을 구현하려면 저장해야 함.
  - **틀림 2 (작음)**: AlreadySignaled 쓰기 확인이 틀리면 팀원 몸은 접근 위반을 돌려줌. 진짜는 (바이트 해석상) 삼키고 status 0 `[동적 확인 후보]`.
  - 닫힘 (`+0x69`) 갈래 `0xC000A003` 은 팀원 몸이 멈춤 — 값은 이 결과서로 확정.

### 2. 배달 길 — 대상이 울릴 때

**공통 줄기**: 신호를 내는 쪽 함수가 **자기 기다림 목록을 돌다가 `WaitType` (+0x10) 를 본다.** `1` = 스레드 (→ `KiTryUnwaitThread`), `2` = 패킷 (→ `BlockState=5` 로 바꾸고 → `KiInsertQueueInternal(블록+0x18 의 포트, 블록)`), `4` = DPC (→ `KeInsertQueueDpc`), `0` = WaitAll (→ `KiTryUnwaitThread`). **패킷은 `KiTryUnwaitThread`·`KiSatisfyThreadWait` 에 가지 않는다** (`KiTryUnwaitThread +0x1e` 가 `[블록+0x18]` 을 스레드로 쓰므로, 호출 앞에서 이미 가려진다).

| 신호를 내는 길 | 사슬 (함수 +호출/+ret) | 패킷 갈래에서 하는 일 |
|---|---|---|
| **이벤트** `NtSetEvent` | `NtSetEvent +0x7/+0xc → NtSetEventEx` ; `NtSetEventEx +0xb9/+0xbe → KeSetEvent` | |
| 자동 리셋 (동기, 형 1) | `KeSetEvent +0x31a` (`WaitType≠1`) → `+0x53b` (`==2`) → `+0x543` (`BlockState=5`) → **`+0x54e/+0x553` `KiInsertQueueInternal`** | 줄에 넣은 뒤 **`+0x553 SUB [SignalState],1`** (1 → 0, 이벤트가 꺼짐) 하고 `JZ +0x55c → +0xdb` 로 대기자 순회 끝 (**딱 하나 깨움**). 대기 목록에서는 이 패킷만 먼저 `+0x313` 에서 떼어 놓았음 |
| 수동 리셋 (알림, 형 0) | `KeSetEvent +0x1b3~+0x1ba` (`WaitType≠1`) → `+0x587` (`==2`) → `+0x58f` (`BlockState=5`) → **`+0x59b/+0x5a0` `KiInsertQueueInternal`** → `+0x19f` 다음 대기자 | 신호는 **안 끔** (`[SignalState]=1` 그대로, `+0x8d`). 대기 목록은 끝에서 통째로 비움 (`+0x4fe~+0x506`) |
| **타이머 (`NtCreateTimer` 쪽 KTIMER)** 만기 | `KiTimerExpiration +0x20e/+0x213 · +0x2ad/+0x2b2 · +0x949/+0x94e · +0x97c/+0x981 → KiProcessExpiredTimerList` ; `KiProcessExpiredTimerList +0xba/+0xbf · +0x483/+0x488 → KiTimerWaitTest` | |
| 동기 타이머 (형 9) | `KiTimerWaitTest +0x1e6` (`≠8`) → `+0x2c7` → `+0x211~+0x238` (대기자 하나 떼기) → `+0x266` (`==2`) → `+0x26b` (`BlockState=5`) → **`+0x276/+0x27b` `KiInsertQueueInternal`** | **`+0x27b SUB [SignalState],1`** → 0 이면 `JZ +0x348` 끝. 타이머가 꺼짐 |
| 알림 타이머 (형 8) | `KiTimerWaitTest +0x201` (`WaitType≠1`) → `+0x2d9` (`==2`) → `+0x2de` (`BlockState=5`) → **`+0x2e9/+0x2ee` `KiInsertQueueInternal`** → `+0x32a` 다음 | 신호 그대로. 목록 비움 `+0x341~+0x345` |
| **타이머2 (`NtCreateTimer2` → `KeInitializeTimer2`, Go 가 쓰는 것)** 만기 | `KiTimer2Expiration +0xd7/+0xdc → KiExpireTimer2` (`NtSetTimer2 +0x9/+0xe → ExpSetTimer2 +0x17d/+0x182 → KeSetTimer2` 로 걸림) | |
| 동기 타이머2 (형 0x19) | `KiExpireTimer2 +0xca` (`==0x19` 이면 안 뛰고 `+0xd2` 순회) → `+0x394` (`==2`) → `+0x39d` → **`+0x3a8/+0x3ad` `KiInsertQueueInternal`** | **`+0x3ad SUB [SignalState],1`**, 0 이면 `JZ +0x127` 끝 |
| 알림 타이머2 (형 0x18) | `KiExpireTimer2 +0xcc` (`JNZ +0x450` 순회) → `+0x6f2` (`==2`) → `+0x6f7` → **`+0x702/+0x707` `KiInsertQueueInternal`** → `+0x450` | 신호 그대로 |
| **세마포어 놓기** `NtReleaseSemaphore` | `NtReleaseSemaphore +0x94/+0x99 → KeReleaseSemaphoreEx` ; `KeReleaseSemaphoreEx +0x233` (`==2`) → `+0x23c` → **`+0x248/+0x24d` `KiInsertQueueInternal`** (드라이버용 `KeReleaseSemaphore` 도 같은 꼴: `+0x22c/+0x231`) | **`+0x24d SUB [count],1`** — **패킷 하나가 count 1 을 먹는다.** 0 이 되면 `JZ +0x15c` 로 끝 |
| (참고) 프로세스·스레드 끝, 게이트, 뮤텐트 해제 | 지도에서 `KiInsertQueueInternal` 을 부르는 caller: `KeSetProcess +0xed/+0xf2` · `KeTerminateThread +0x2c4/+0x2c9` · `KeSignalGate +0x10f/+0x114` · `KeReleaseMutantEx +0x37c/+0x381` · `KiResumeThread +0x23c/+0x241` | 따라가지 않음 (대상이 프로세스·스레드인 패킷도 같은 `KiInsertQueueInternal` 로 감을 보여 줌) |

- **`KiInsertQueueInternal` (0x223330, 834 B)** — 줄에 넣는 함수 (인자: 포트 = `RCX`, 항목 = `RDX`):
  - `+0x14` `AND [항목],0` — 항목의 `Flink` 를 0 으로 (줄에 달려 있지 않다는 표시. `KeRemoveQueueEntry` 가 이 칸으로 "줄에 있나" 를 가름: `KeRemoveQueueEntry +0x3b/+0x3f`).
  - 포트 락 (`BTS.LOCK [RDI],7`, `+0x60`) 잡고, 기다리는 스레드가 있고 `CurrentCount < MaximumCount` 이면 그 스레드에게 **바로 넘김** (`+0x74~+0x8a`, 스레드 `+0xc8` 에 항목 주소: `+0x139`) — 이때 항목은 줄에 안 달림.
  - 아니면 `+0x175` `SignalState (+4, 줄에 든 항목 수)++` · `+0x191~+0x19b` 포트 `EntryListHead` (+0x18) **꼬리에 항목을 단다** · 줄이 비어 있었고 기다리는 스레드가 있으면 `KiWakeOtherQueueWaiters` (`+0x1af/+0x1b4`).
  - → **`KiInsertQueue`·`KeInsertQueueEx` 가 아니라 `KiInsertQueueInternal`**. 그 둘의 callee 에는 `KiInsertQueueInternal` 이 없고, 위 신호 함수들은 `KiInsertQueueInternal` 만 부른다. **패킷 객체 자체가 줄 항목** (복사 없음, 앞 16바이트 = 줄 연결 고리).
- **대상의 신호를 쓰나**: **쓴다 (스레드 기다림과 같은 결과).** `KiWaitSatisfy*` 를 거치진 않고 신호 함수가 **직접** `SignalState` 를 1 줄인다 (이벤트 `+0x553`, 타이머 `+0x27b`, 타이머2 `+0x3ad`, 세마포어 `+0x24d`) — 스레드 갈래도 같은 `SUB` 를 한다 (`KeSetEvent +0xd1`). 즉 자동 리셋 이벤트 · 동기 타이머 · 타이머2 동기는 꺼지고, 세마포어 count 는 패킷마다 줄고, 알림 계열은 그대로. 맡길 때 이미 울려 있었으면 `KiWaitSatisfyOther` 가 같은 일을 한다 (Q1).
- **패킷은 한 번 쓰면 다시 맡겨야 하나**: **그렇다.** 울릴 때 패킷은 대기 목록에서 떨어진다 (동기: `KeSetEvent +0x313` 등에서 떼기, 알림: 목록 비움). 줄에 들어간 패킷은 연결 표시 `+0x68` 가 1 로 남은 채 기다리다가, **`IoRemoveIoCompletion` 이 꺼낼 때** (`+0x342`) 또는 **취소가 뺄 때** (`IopCancelWaitCompletionPacket +0x4b`) 0 이 된다. 그 뒤에야 다시 맡길 수 있다. 꺼내기 전에 맡기면 `0xC00000EF`.
- **지금 팀원 몸**: 이벤트·타이머·세마포어 모두 "배달 공사 중" — **빈 곳 = 이 표 전부.** 이미 울린 자리의 "줄에 바로 넣기" 만 있음 (맞음).

### 3. `NtCancelWaitCompletionPacket` · `IopCancelWaitCompletionPacket`

`NtCancelWaitCompletionPacket(패킷 핸들, RemoveSignaledPacket)` — 패킷 접근 `0x1` 로 참조 (`+0x2e` `MOV EDX,1`, `+0x57/+0x5c`). 흐름: 패킷 락 잡고 포트 포인터 `[+0x58]` 읽어 참조 하나 더 쥠 (`+0x75~+0x93`) → 락 풀고 → `[+0x58]==0` 이면 `0xC0000120` (`+0x9e/+0xa1 → +0x11c`) → 포트 쪽 락 (`+0xac/+0xb1`, 포트 `+0x40`) 잡고 → 패킷 락 다시 (`+0xb4/+0xb9`) → **`IopCancelWaitCompletionPacket(패킷, RemoveSignaledPacket, 이전 IRQL)`** (`+0xda/+0xdf`).

`IopCancelWaitCompletionPacket` (197 B): `ObGetAssociatedWaitObject(패킷+0x50)` (+0x2a/+0x2f) → **`KiDeregisterObjectWaitBlock(대상, 패킷)`** (+0x35/+0x3a). 그 함수는 대상 락 잡고 `BlockState == 4` 일 때만 대기 목록에서 떼고 5 로 바꿔 `TRUE`, 아니면 `FALSE` (`KiDeregisterObjectWaitBlock +0x3b~+0x65`).

| 상황 | 갈림 자리 | 돌려주는 status | 한 일 |
|---|---|---|---|
| **아직 안 울림** (대기 목록에 있음, `BlockState 4`) | `IopCancel +0x3e` (`KiDeregister…` TRUE) → `+0x42` | **0 (STATUS_SUCCESS)** — `RemoveSignaledPacket` 값과 무관 | 대기 목록에서 떼기 · `[+0x58]=0` (`+0x42`) · `[+0x68]=0` (`+0x4b`) · 락 풀기 (`+0x4f/+0x54`) · **대상·포트·패킷 참조 하나씩 풀기** (`+0x77/+0x81/+0x8b`) · 반환 TRUE (`+0x90`). 대상의 신호는 안 건드림 |
| **이미 줄에 듦**, `RemoveSignaledPacket = TRUE` | `IopCancel +0xae` → `+0xb9/+0xbe KeRemoveQueueEntry(포트, 패킷)` → TRUE 면 `+0xc0 → +0x42` | **0** | 줄에서 빼고 (`KeRemoveQueueEntry +0x41` count--, 연결 풀기 `+0x5d~+0x63`) 위와 같이 정리. 네 값은 버려짐 |
| 이미 줄에 듦, `RemoveSignaledPacket = TRUE` 인데 **항목이 줄에 안 달려 있음** (이미 스레드에게 직접 넘겨짐 등) | `KeRemoveQueueEntry +0x3b/+0x3f` (`[항목]==0`) → FALSE | **`0x103` (STATUS_PENDING)** | 아무것도 안 바꿈 (연결 유지) |
| 이미 줄에 듦, `RemoveSignaledPacket = FALSE` | `IopCancel +0xae/+0xb1` (`TEST BPL; JZ +0xc2`) → FALSE | **`0x103` (STATUS_PENDING)** — `NtCancel +0x14e~+0x158` (`[+0x68]≠0` 이면 `CMOVNZ EDI,0x103`) | 아무것도 안 바꿈. 패킷은 줄에 남고 연결 유지 → 이후 `IoRemoveIoCompletion` 이 꺼냄 |
| **맡겨지지 않은 패킷** (처음부터 안 맡김, 또는 이미 꺼내서 `+0x58/+0x68` 이 0) | `NtCancel +0x9e/+0xa1` (`[+0x58]==0`) → `+0x11c` · 또는 `+0xc1` (`[+0x68]==0`) → `+0x132` | **`0xC0000120` (STATUS_CANCELLED)** | 참조만 풀고 끝 |
| 포트가 도중에 바뀜 (두 락 사이 다툼) | `NtCancel +0xcb` (`[+0x58]≠RSI`) → `+0x13c` | `0xC000A003` | 락만 풀고 끝 |
| 핸들 문제 | `+0x57/+0x5c` | `ObReferenceObjectByHandle` 값 (잘못된 핸들, 형 틀림, 접근 `0x1` 없음 등) | |

- 같은 `IopCancelWaitCompletionPacket` 을 **닫기 처리기** `IopCloseWaitCompletionPacket` 도 부른다 (`+0xbe/+0xc3`, `DL=1` = `RemoveSignaledPacket` TRUE). 먼저 `[패킷+0x69]=1` 로 닫힘 표시 (`+0x42`). `+0x0 CMP R9,1` (4번째 인자) 가 1 일 때만 몸이 돈다 (닫기 처리기 인자 순서 기준 "그 프로세스의 마지막 핸들" 로 보임 `[추측]`). 즉 **패킷 핸들을 닫으면 대기 목록이든 IOCP 줄이든 걸린 패킷이 조용히 취소된다.**
- **지금 팀원 몸**:
  - "맡겨지지 않음 → `0xC0000120`" — **맞다**. 이 갈래 하나에만 `0xC0000120` 이다.
  - "줄에 든 것을 뺌 = 0" · "FALSE = `0x103`" (팀원 몸 현재 코드) — **맞다**.
  - "안 울린 패킷 취소" — 진짜는 **0 으로 성공** (대기 목록에서 떼고 참조 풀기). 팀원 몸은 지금 멈춤 (`wait_completion_waiting_state_unsupported`). **`0xC0000120` 으로 돌려주면 틀린다.**
  - 요청서가 적은 "줄에 든 것만 다루고 `0xC0000120`" 이 "안 울려서 줄에 안 든 연결된 패킷에 `0xC0000120`" 라는 뜻이면 **틀림.**

### 4. `IoRemoveIoCompletion` — 줄이 비었을 때 · 패킷 다루는 갈래 (갈림 자리만)

- 기다리는 길: `IoRemoveIoCompletion +0x8d/+0x92 → KeRemoveQueueEx` (인자: 포트·WaitMode·Alertable·Timeout 포인터·출력 배열·개수). 줄이 비고 기다려야 하면 `KeRemoveQueueEx` 안에서 스레드를 재운다 (`+0x891/+0x896 KiCommitThreadWait`, 이름 기준). 깨우는 쪽은 Q2 의 `KiInsertQueueInternal` 직접 넘김 (`+0x74~+0x139`).
- **시간 0 + 줄 비었을 때**: `KeRemoveQueueEx` 가 출력 배열 첫 칸에 값 **`0x102`** 를 써 넣는다 (`+0x3d3~+0x3e9`, 시간 0 길 `+0x356/+0x35a` 뒤. Alertable 아닌 길도 `+0x786~+0x78d` 에서 같은 `+0x3d3` 로 감. 다른 갈래 `+0xb36` 도 같은 값. 단 **사용자 APC 가 와 있고 사용자 모드 기다림이면** 대신 `0xc0` (STATUS_USER_APC) 를 씀: `+0x793~+0x7a4`). `IoRemoveIoCompletion` 은 첫 칸이 `0x80 · 0xc0 · 0x101 · 0x102` 면 "줄 항목이 아님" 으로 보고 (`+0xe4~+0x109 → +0x28c`) `NumEntriesRemoved = 0` (`+0xb7`), **status = 그 값 (`0x102` = STATUS_TIMEOUT)** 으로 돌려준다 (`+0x28c~+0x290 → +0xaf`, `EAX=EDI` 가 유지됨). `[정적]`
- **패킷 항목을 다르게 다루는 갈래**: `IoRemoveIoCompletion +0x10f` (`MOVZX EAX,byte [항목+0x10]`) 가 가른다:
  - `0` → IRP (`+0x11b` 항목−0xa8 → `IopCompleteRequest +0x1bd/+0x1c2`)
  - **`2` → 기다림 완료 패킷** (`+0x1c9/+0x1cb JZ +0x2e3`): 네 칸 읽기 (`+0x2e3~+0x2fb`) → IRQL 2 + 패킷 락 → **`[+0x68]=0` (`+0x342`) · `[+0x58]=0` (`+0x346`)** → 락 풀기 → **참조 셋 풀기: 대상 (`+0x3bc`) · 포트 (`+0x403`) · 패킷 (`+0x43d`)**.
  - 그 밖 (1) → 작은 완료 패킷 (`IopFreeMiniCompletionPacket +0x1ec/+0x1f1`)
  - 출력 항목은 한 개당 0x20 바이트: `Key` +0 · `Apc` +8 · `Status` +0x10 (4바이트) · `Information` +0x18.
- **지금 팀원 몸**: 요청서가 적은 "빈 줄 → `wait_would_block`" 은 **시간 0 이면 틀림** (진짜 = `STATUS_TIMEOUT 0x102` + 0 개). 기다림 있을 때는 정상 (재움). 꺼낼 때 **연결 표시를 끄고 (`+0x68`) 세 참조를 푸는 것** 을 `nt_io_completion` 쪽도 해야 다음 맡기기가 `0xC00000EF` 가 안 난다.

### 5. 최소 조건 표 — "에뮬레이터가 맡김·배달·취소를 진짜와 같게 하려면"

| 상황 | status | AlreadySignaled | 대상 신호 변화 | 줄에 들어가는 네 값 (Key / Apc / IoStatus / Information) | 근거 |
|---|---|---|---|---|---|
| 맡기기: **이미 울림** (`SignalState>0`) | 0 | **1 을 씀** | 자동 리셋 이벤트·동기 타이머 (·타이머2 동기) 꺼짐; 세마포어 −1; 수동 이벤트·알림 타이머·프로세스·스레드 그대로 | **그 자리에서** 줄 꼬리에 넣음 (스레드가 기다리고 있으면 바로 넘김). **인자 4·5·6·7 그대로**. 연결 표시 켜짐 | `NtAssoc +0x1d0`, `KeRegisterObjectNotification +0x4a~+0xa8` |
| 맡기기: **안 울림** | 0 | **0 을 씀** | 없음 | 아직 안 들어감. **패킷에 네 값 저장** (`+0x30/+0x38/+0x48/+0x40`) · 대상 대기 목록 꼬리에 패킷 연결 · 연결 표시 켜짐 | `KeRegisterObjectNotification +0xac~+0xde` |
| 나중에 울림: 이벤트 set / 타이머 만기 / 세마포어 놓기 | (호출 쪽 반환, 패킷과 무관) | — | 동기 계열은 `1→0`, 세마포어는 패킷마다 −1, 알림 계열 그대로 | **저장해 둔 네 값 그대로** 줄 꼬리에. 패킷은 대기 목록에서 떨어짐. 연결 표시는 **켜진 채** (꺼내기 전까지 재맡기기 `0xC00000EF`). 자동 리셋은 **패킷 하나만** 깨움 (첫 대기자) | Q2 표 |
| 꺼내기 (`IoRemoveIoCompletion`) | 0 (항목 수 1) | — | — | 네 값을 그대로 출력. **연결 표시 끄기 · 참조 셋 풀기** | `IoRemoveIoCompletion +0x2e3~+0x43d` |
| 취소: **안 울림** | **0** | — | 없음 | 대기 목록에서 떼고 연결 끄기. 네 값 버림 | `IopCancel +0x35~+0x90` |
| 취소: **줄에 듦**, Remove = **TRUE** | **0** | — | 없음 | 줄에서 빼고 연결 끄기 (단 항목이 줄에 안 달려 있으면 `0x103`) | `IopCancel +0xb9`, `KeRemoveQueueEntry +0x3b` |
| 취소: **줄에 듦**, Remove = **FALSE** | **`0x103`** | — | 없음 | 그대로 줄에 남음, 연결 유지 | `NtCancel +0x14e~+0x158` |
| 취소: **안 맡김** (또는 이미 꺼냄) | **`0xC0000120`** | — | — | — | `NtCancel +0x11c / +0x132` |
| 맡기기: **이미 연결된 패킷** | `0xC00000EF` | 안 씀 | — | — | `NtAssoc +0x16c → +0x24c` |
| 맡기기: **뮤턴트·큐·기다릴 수 없는 대상** | `0xC00000F1` | 안 씀 | — | — | `NtAssoc +0x13d~+0x14b → +0x242` |
| 맡기기: **닫힌 패킷** | `0xC000A003` | 안 씀 | — | — | `NtAssoc +0x17c → +0x207` |
| 패킷 핸들 닫기 | — (반환 없음) | — | — | 취소 (Remove=TRUE) 와 같음 + `+0x69` 켜기 | `IopCloseWaitCompletionPacket +0x42, +0xbe` |
| 빈 줄에서 꺼내기, 시간 0 | **`0x102`** (항목 수 0) | — | — | — | `IoRemoveIoCompletion +0xe4~+0x109`, `KeRemoveQueueEx +0x3d3` |

## 동적분석으로 확인해야 할 것 (실행 때만 정해지는 것 — `[추측]`)

- Go 가 만든 타이머 (`NtCreateTimer2`) 가 **동기 (자동 리셋) 인지 알림인지** — 코드는 두 갈래 (`KiExpireTimer2 +0x3ad` / `+0x707`). 정적으로는 Go 런타임이 자동 리셋이라고 `[추측]` 만 한다.
- `AlreadySignaled` 포인터가 틀린 사용자 주소일 때 실제로 status 0 이 나오는지 (바이트 해석). 
- 패킷 객체 실제 크기와 `WaitKey` (+0x12) 초기값 (`NtCreateWaitCompletionPacket` 이 정함 — 이번 범위 밖).
- `0xC000A003` 이름과, 닫기 처리기 (`IopCloseWaitCompletionPacket`) 가 객체 형식의 **닫기 처리기** 로 등록돼 있는지 (형식 등록은 이번 범위 밖).
- 줄에 패킷이 달린 채 스레드가 `NtRemoveIoCompletionEx` 로 기다리는 경우, 스레드에게 바로 넘겨지는 항목을 취소가 못 빼서 `0x103` 이 되는 것 (정적으로는 갈래만 확인).
- 추천 멈춤점 (모든 프로세스에 걸리므로 test.exe 조건 필요):
  - `nt!KeRegisterObjectNotification` 반환 (AL 값 = AlreadySignaled) 과 `rdx` (포트) · `r8` (패킷) — 안 울림 / 울림 가름.
  - `nt!KiInsertQueueInternal` 에서 `[rdx+0x10]==2` 일 때만 (패킷 항목) — 콜스택 ret 이 위 표의 `KeSetEvent+0x553/+0x5a0` · `KiTimerWaitTest+0x27b/+0x2ee` · `KiExpireTimer2+0x3ad/+0x707` · `KeReleaseSemaphoreEx+0x24d` · `KeRegisterObjectNotification+0x85` 중 어느 것인지.
  - `nt!IopCancelWaitCompletionPacket` 반환 (AL) 과 `NtCancelWaitCompletionPacket` 반환 (`eax`).
  - 패킷 객체 덤프 (`dq @r8 L0xe`) 로 `+0x30..+0x5f` 와 `+0x68/+0x69` 가 위 표와 같은지.

## 다음 요청 후보

1. `NtCreateWaitCompletionPacket` · `IopFreeWaitCompletionPacket` — 패킷 객체 크기, 만들 때 `KWAIT_BLOCK` 앞 칸과 `+0x60` 락, `+0x68/+0x69` 초기값. (팀원 몸 `TODO` 에 있는 `GenericAll` 값도 같이)
2. `NtSetTimer2` → `ExpSetTimer2` → `KeSetTimer2` — 이미 지난 시각이면 그 자리에서 `KiExpireTimer2` 로 가나, 아니면 다음 타이머 인터럽트인가 (Go: 타이머 걸고 바로 맡김). 그리고 `NtCreateTimer2` 의 동기/알림 인자.
3. `NtRemoveIoCompletionEx` → `IoRemoveIoCompletion` 인자 대응과 `KeRemoveQueueEx` 대기 몸 (APC·Alertable·깨울 때 항목 건네기) — 팀원 `nt_io_completion.cpp` 용.
4. `KiDeregisterObjectWaitBlock` / `KeRegisterObjectNotification` 이 대상 형 (파일 객체 · 프로세스 · 스레드 · IOCP) 마다 `ObGetAssociatedWaitObject` 로 어떤 헤더를 고르나 (`ObGetAssociatedWaitObject +0x2b~+0x88` 의 형 정보 칸 `+0x20/+0xb0/+0xb4/+0xb6`) — 팀원 몸이 대상 형을 이벤트·세마포어·타이머로 좁혀 둔 것을 넓힐 때.

## 판정

**분석성공.** 요청서 질문 1~5 전부 `.asm` 줄 근거로 답함. 정적으로 못 정한 것 (타이머 종류, 예외 착지, 이름·객체 형식 등록) 은 위 "동적으로 확인해야 할 것" 으로 넘김. 스크립트 문제 없음.

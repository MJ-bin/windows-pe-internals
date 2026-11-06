# 64 loader-pool — 정적 분석 결과

- 요청서: `_workspace/sast/64_loader-pool.md` (Q132, run69 예정, PE_emulator 요청 12)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`, **새로 analyze 안 함**) + 커널 `NtSetInformationWorkerFactory` 1개 (클래스 길이 확인용, 이것도 analyze 안 함)
  - pdb guid+age: ntdll `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`). 커널 쪽은 새로 받지 않았고 기존 `.gpr` 그대로 (기준 GUID `C29EBFB06B78B3C020DCA66D99713F9E1` 는 sast/63 에서 확인됨).
- 뽑은 함수 (새로 `.asm`·`.c`): ntdll 요청서 대상 `TpSetPoolWorkerThreadIdleTimeout` · `TpAllocWork` · `RtlpHpGCTimerEnable` (depth 1) · `TppWorkInitialize` · `TppInitializeTimer` · `TpSetPoolMaxThreads` (depth 1). 있던 것 그대로 읽음 (다시 안 뽑음): `LdrpEnableParallelLoading` · `TpAllocPoolInternal` · `_LdrpInitialize` · `LdrpDetectDetour` · `RtlGetSuiteMask` · `TppPoolUpdateNodeRelation` · `TppInitializeTimerQueue` · `TppInitializeTimerSubQueue` · `TppGetCurrentThreadNumaNode` · `TppCleanupGroupMemberInitialize` · `TppAdjustRunningThreadGoalWithLock`.
  - **요청서 밖으로 넓힌 것 (이유 있음)**
    - 커널 `NtSetInformationWorkerFactory` (depth 0, `ms/decomp/ntoskrnl/NtSetInformationWorkerFactory.c`) — 요청서가 "PDB 로" 라고 했지만 ntdll·커널 PDB 어디에도 `_WORKERFACTORYINFOCLASS` 열거가 없어서(아래 1번) 클래스마다 필요한 버퍼 길이만 보려고. 본문은 길이·핸들 검사 줄만 읽음.
    - ntdll `RtlpHpGCTimerSchedule` · `RtlpHpGCTimerCallback` (depth 0, 합쳐 163바이트) — 3번에서 "힙 GC 타이머 전역" 을 누가 쓰고 읽는지 보다가 그 전역이 이 둘의 입력이어서.
    - `refs` 로 전역 칸 읽는 곳 쓰는 곳: `LdrpThreadPool` · `LdrpMapAndSnapWork` · 힙 GC 타이머 전역(PDB 이름 없음, rva `0x1ce270`·`0x1ce278`·`0x1ce280`) · `TpAllocPoolInternal` (`ms/disassemble/ntdll/_refs/`).
- 버전 확인: `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess` 일치 · `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` 일치 · `LdrInitializeThunk +0x9/+0xe → LdrpInitialize` 일치 (`_calls.tsv`). 새로 뽑은 함수 크기도 요청서와 같음 (79·377·337B). → **일치**.
- 스크립트 문제: **없음.**

## 한 줄 결론 — "핸들 0" (질문 1)

- **진짜 코드 길에서는 핸들 0 이 될 수 없다** `[정적]`.
  - 핸들 칸 = 풀 `+0x38` (`_TP_POOL.WorkerFactory`). `TpAllocPoolInternal +0x303/+0x308` 의 `NtCreateWorkerFactory` 가 이 칸에 직접 쓴다.
  - 이 syscall 이 음수면 `TpAllocPoolInternal` 은 풀을 통째로 버리고 (`+0x310 JS` → `+0x465` → `+0x4e9` ~ `+0x5a0` 해제), **`LdrpThreadPool` 을 0 으로 둔 채** 음수를 돌려준다 (출력 칸은 `+0x28` 에서 0 으로 지우고, 성공이 확정된 `+0x4be` 에서만 풀 주소를 쓴다). `LdrpEnableParallelLoading +0x4d JS` 가 그걸 보고 `+0xe4` 로 뛰어 `TpSetPoolWorkerThreadIdleTimeout` 에 오지 않는다.
  - 그래서 `TpSetPoolWorkerThreadIdleTimeout` 에 닿는 풀은 "핸들이 방금 진짜로 만들어진 풀" 뿐이다.
- PE_emulator 에서 본 핸들 0 은 **에뮬레이터 기본 대답** (`NtCreateWorkerFactory` 에 "성공" 만 주고 핸들 칸을 안 써 줌) 때문이다. 풀 `+0x38` 은 힙을 `HEAP_ZERO_MEMORY` (`+0xa3 OR EDX,8`) 로 받아 0 이 남는다.
- 코드는 핸들 0 검사를 **안 한다**: `TpSetPoolWorkerThreadIdleTimeout +0x26` 이 `[풀+0x38]` 을 그대로 읽어 넘긴다. 핸들 0 이어도 이 함수는 안 막히고, 돌아온 상태는 아무도 안 본다 (아래 1-6). 즉 **에뮬레이터에서 핸들 0 이어도 로더 길은 안 깨진다.** 단 `+0x448` 의 클래스 `0xd` 는 상태를 검사하므로 거기선 "성공" 이어야 한다 (1-7).

## 질문별 답 [정적]

### 1. `TpSetPoolWorkerThreadIdleTimeout` (rva `0xe6520`, 79B)

근거 파일: `ms/disassemble/ntdll/TpSetPoolWorkerThreadIdleTimeout.asm`, `LdrpEnableParallelLoading.asm`, `TpAllocPoolInternal.asm`.

**1-1. 인자**
- `RCX` = 풀 포인터, `RDX` = INT64 시간 값. 부르는 곳 `LdrpEnableParallelLoading +0x53 MOV RCX,[LdrpThreadPool]` · `+0x5a MOV RDX,-0x11e1a300` · `+0x61/+0x66 CALL`.
- 시간 값 = **−300 000 000** (`0xFFFFFFFFEE1E5D00`). 윈도 상대 시각 (음수 = 상대, 100ns 단위) 관례로 **30초** `[추측: 단위는 NT 관례]`. 값은 상수이고 `LoaderThreads` 와 무관.

**1-2. 핸들은 풀의 어느 칸**
- **풀 `+0x38`** (`+0x26 MOV RCX,[RCX+0x38]`). PDB `_TP_POOL` (`pdb_struct.py ntdll.dll _TP_POOL`, 크기 `0x1d8`): `+0x38 WorkerFactory void*`, 옆 `+0x40 CompletionPort`.
- `TpSetPoolMaxThreads +0x28` 도 같은 칸 (`MOV RCX,[RCX+0x38]`). `TppInitializeTimer +0xb0` (`MOV RCX,[RCX+0x38]`) 도 마찬가지 — 이 풀의 `NtSetInformationWorkerFactory` 는 전부 이 칸.

**1-3. 클래스 `0x2` 이름**
- 코드: `+0x2f MOV R9D,8` · `+0x35 LEA EDX,[R9-6]` → 클래스 **2**, 길이 **8**.
- PDB 에 `_WORKERFACTORYINFOCLASS` 열거 **없음** (ntdll·ntoskrnl PDB 둘 다 `--list` 로 확인). 이름은 함수 이름(`...IdleTimeout`) 으로 **"유휴 시간 제한" `[추측]`** (공개 헤더 이름 `WorkerFactoryIdleTimeout` 과 일치할 것으로 봄).
- 커널 쪽 확인 `[정적]` (`ms/decomp/ntoskrnl/NtSetInformationWorkerFactory.c`): 클래스 2 는 **필요 길이 8**, 클래스 3·4·5·8·9·0xb·0xc·0xd·0xe 는 4, 클래스 0xa 는 0x10, 클래스 0xf 는 가변(최대 0x100). 길이가 안 맞으면 일꾼 공장을 열어 보기도 전에 끝. 즉 `R9D=8` 은 커널이 요구하는 값과 맞다.
- (참고) 이 함수 안 다른 클래스 번호 쓰임: 클래스 5 (`TpSetPoolMaxThreads +0x37`), 클래스 3 (`TppInitializeTimer +0xc7`), 클래스 8 (`TppAdjustRunningThreadGoalWithLock +0x93`), 클래스 `0xd` (`TpAllocPoolInternal +0x43c`). 호출 쪽 코드 위치는 4번 표.

**1-4. 버퍼 8바이트 내용**
- 인자로 받은 `RDX` 를 함수 시작에서 자기 집 칸에 저장 (`+0x0 MOV [RSP+0x10],RDX`), 그 주소 (`+0x2a LEA R8,[RSP+0x38]`) 를 버퍼로 넘긴다. **버퍼 = INT64 −300000000 하나.** 입력 전용 (set), 출력 없음.

**1-5. 그 밖에 풀에 쓰는 칸**
- **없음.** 이 함수는 풀 `+0x38` 을 읽을 뿐 풀에 아무것도 안 쓴다 (스택 8바이트와 syscall 만). `TpSetPoolMaxThreads` 도 같다. 유휴 시간·최대 스레드 수의 진짜 값은 커널 일꾼 공장 개체 안에만 있고 유저 풀 개체에는 사본이 없다 `[정적]`.

**1-6. `+0x44/+0x49 TppRaiseInvalidParameter` 로 가는 조건** (`+0xc` · `+0x11` · `+0x24` 셋 중 하나가 점프하면 `+0x44` — 목적지 `0x1800e6564 − 0x1800e6520 = +0x44`)
- `+0x9 TEST RCX,RCX` → `+0xc JZ` : **풀이 0**.
- `+0xe TEST RDX,RDX` → `+0x11 JNS` : **시간 값이 0 이상** (음수만 허용).
- `+0x13 ~ +0x20` 로 `PEB.Ldr` (`[GS:0x60]+0x18`) → `+0x20 CMP byte [Ldr+0x48],0` → `+0x24 JNZ` : **`ShutdownInProgress` ≠ 0** (PDB `_PEB_LDR_DATA +0x48 ShutdownInProgress`).
- 갈래 안: `+0x44 TppRaiseInvalidParameter` (그 안 `+0x66/+0x6b RtlRaiseException`, 코드 `0xc000000d`) → 돌아오면 `+0x49 MOV EAX,0xc000000d` → `+0x4e JMP +0x3e`.
- 로더가 넘기는 값 (풀 ≠ 0, 음수, 종료 중 아님) 으로는 **안 탄다** `[정적]`.

**1-7. syscall 실패는 어떻게 다루나 / PE_emulator 는 무엇을 돌려줘야 하나**
- 함수는 `NtSetInformationWorkerFactory` 가 준 상태 (`EAX`) 를 **그대로 돌려준다** (`+0x39/+0x3e CALL` → `+0x42 RET`, 사이에 `EAX` 건드리는 줄 없음).
- 하나뿐인 부르는 곳 `LdrpEnableParallelLoading +0x61/+0x66` 은 반환값을 **보지 않는다** (`+0x66` 부터 `EAX` 읽는 줄 없음, `EDI` 는 `+0x49` 와 `+0xe2` 에서만 채워짐). → **상태가 뭐든 로더는 똑같이 간다.**
  - 진짜 커널은 핸들이 일꾼 공장이면 0, 핸들이 0 이면 음수 `[추측: ObReferenceObjectByHandle 가 거부, 본문은 안 봄 — 커널은 Ob 호출 결과가 음수면 그 상태 그대로 끝냄 (.c 의 `iVar7 < 0` 줄)]`.
  - PE_emulator 권고: **`STATUS_SUCCESS` (0), 출력 쓰기 없음.** 핸들 0 에 음수를 줘도 되지만 의미가 없다.
- 같은 풀의 다른 `NtSetInformationWorkerFactory` 호출은 사정이 다르다:
  | 부르는 자리 | 클래스 | 상태 검사 | 에뮬레이터가 줘야 하는 것 |
  |---|---|---|---|
  | `TpAllocPoolInternal +0x443/+0x448` | `0xd` | **검사함** (`+0x44e TEST` → `+0x450 JS +0x465` 이면 풀 폐기) | **0 이상 필수.** 아니면 `LdrpThreadPool` 이 0 으로 남아 이후가 전부 달라짐 |
  | `TpSetPoolWorkerThreadIdleTimeout +0x39/+0x3e` | 2 | 안 봄 | 아무 값 (권고 0) |
  | `TpSetPoolMaxThreads +0x3b/+0x40` | 5 | 안 봄 (다음 줄이 `RtlGetCurrentServiceSessionId` 호출) | 아무 값 (권고 0) |
  | `TppInitializeTimer +0xcb/+0xd0` | 3 | 안 봄 (`+0xd0 JMP`) | 아무 값 (권고 0) |
  | `TppAdjustRunningThreadGoalWithLock +0x9b/+0xa0` | 8 | 안 봄 | (이 길 안 탐) |
- `NtCreateWorkerFactory` (`+0x310 JS`), `NtCreateIoCompletion` (`+0x29f JS`), `NtCreateTimer2` (`TppInitializeTimerSubQueue +0x54 JS`), `NtCreateWaitCompletionPacket` (`+0x6e JS`) 는 검사한다 (음수면 정리하고 실패). `NtAssociateWaitCompletionPacket` (`+0xa0/+0xa5`) 는 안 본다.

**1-8. 같이 — 워커 팩토리 핸들 칸은 언제 채워지나, 로더 전용 풀은 기본 풀과 다르게 만드나**
- 채워지는 때: `TpAllocPoolInternal +0x2cb LEA RSI,[RDI+0x38]` 로 칸 주소를 잡고 `+0x300 MOV RCX,RSI` → `+0x303/+0x308 NtCreateWorkerFactory` 가 성공하는 순간. 그 전에는 0.
- **로더 풀은 기본 풀과 다른 인자로 만든다 — 맞다.** `TpAllocPoolInternal` 의 둘째 인자 (`EDX`) 가 다르다.
  | | 둘째 인자 | 부르는 곳 |
  |---|---|---|
  | 기본 풀 | 0 (`XOR EDX,EDX`) | `TppPoolpReferenceGlobalPool +0xbb/+0xc2` (ret `+0xc7`) |
  | 로더 풀 (`LdrpThreadPool`) | **1** (`MOV EDX,1`) | `LdrpEnableParallelLoading +0x38/+0x44` (ret `+0x49`) |
  | 그 밖 | 공개 `TpAllocPool +0x27` 가 `JMP` 로 넘김 (이 함수 본문은 안 봄, 둘째 인자를 어떻게 만드는지 `[공백]`) | `_refs/TpAllocPoolInternal.tsv` |
- 인자가 하는 일: `+0x2f AND EDX,R12D` (R12D=1) → `+0x32` 로 `[RSP+0xb8]` 에 저장 → `+0x316 CMP [RSP+0xb8],R15D(=0)` → `+0x31e JNZ 0x1800e5e2e` (= `+0x42e`). 값이 1 이면 **`+0x42e ~ +0x448` `NtSetInformationWorkerFactory` (클래스 `0xd`, 4바이트, 값 = 1)** 를 더 부르고, 성공하면 `+0x459 MOV [RDI+0x17a],AX` (풀 `PoolFlags` = 1) 후 `+0x460 JMP +0x324` 로 본 줄기에 합류.
  - 커널 쪽: 클래스 `0xd` 는 값의 **bit0 를 일꾼 공장 `+0x198` 의 bit 11 (`0x800`) 에 복사**한다 (`NtSetInformationWorkerFactory.c` 의 `(값 & 1) << 0xb | 옛값 & 0xfffff7ff`). 이 bit 의 **뜻은 모름** `[공백]` (이름을 짐작하지 않음). 이름은 공개 헤더의 `WorkerFactoryFlags` 일 것 `[추측]`.
  - **run62(sast/57)에서 `[공백]` 로 남았던 `TpAllocPoolInternal +0x443` 갈래가 이것이다 → 닫힘:** 기본 풀 길은 둘째 인자 0 이라 안 탄다. 로더 풀만 탄다.
- `NtCreateWorkerFactory` 도 로더 풀에서 **나온다** (기본 풀과 같은 `+0x303/+0x308`, 같은 인자 모양). 인자 `[정적]`:
  - `RCX=&풀+0x38` (출력 핸들), `EDX=0xf00ff`, `R8=NULL`, `R9=[풀+0x40]` (바로 앞 `NtCreateIoCompletion` 이 `+0x292` 에서 만든 핸들), `[RSP+0x20]=-1` (현재 프로세스), `[RSP+0x28]=0x18001a2e0` (일꾼 스레드 시작 루틴), `[RSP+0x30]=풀 포인터` (시작 매개변수), `[RSP+0x38]=max(ActiveProcessorCount*4, 0x200)` = 이 VM(2코어) 은 **`0x200`**, `[RSP+0x40]/[RSP+0x48]` = exe 헤더 스택 예약/확정 크기 (`+0x76 R13=[NT+0x60]`, `+0x7a R14=[NT+0x68]`, `PEB+0x318 MinimumStackCommit` 이 0 이 아니면 확정 크기를 그 값 이상으로 올림 `+0x5ca CMP R14,RCX` · `+0x5cd CMOVC`).
  - 접근 마스크 `0xf00ff` 는 커널이 요구하는 접근 4 (`ObReferenceObjectByHandle(핸들, 4, ExpWorkerFactoryObjectType, …)`) 를 포함.
- 결론: `TpSetPoolWorkerThreadIdleTimeout` 이 불릴 땐 핸들이 `NtCreateWorkerFactory` 가 써 준 진짜 값이고, `LdrpThreadPool` 이 0 이 아니라는 것 자체가 그 보증이다. **핸들 0 은 진짜 길에선 불가능.**

닫힘 여부: **정적으로 닫힘.** (핸들의 숫자 값, 커널이 클래스 2 값을 어떻게 쓰는지는 동적/커널 몫이지만 로더 길 판단엔 필요 없음.)

### 2. `TpAllocWork` (rva `0x84900`, 377B)

근거: `ms/disassemble/ntdll/TpAllocWork.asm`, `TppWorkInitialize.asm`, `TppCleanupGroupMemberInitialize.asm`, `LdrpEnableParallelLoading.asm`.

**2-1. 인자** (`LdrpEnableParallelLoading +0xdd/+0xe2`)
- `RCX = &LdrpMapAndSnapWork` (출력 칸, `+0xa9 LEA`). `RDX = 0x1800e9ef0` (`+0x9d LEA`) = **`LdrpWorkCallback`** (rva `0xe9ef0`, 218B — `_index.tsv` 이름 맞음. 본문은 이번에 안 봄). `R8 = 0` (문맥, `+0xbc XOR R8D,R8D`). `R9 = &환경` (`+0x94 LEA R9,[RBP-0x50]`).
- 환경 = PDB `_TP_CALLBACK_ENVIRON_V3` (크기 `0x48`) 로 채움:
  | 칸 | 값 | 줄 |
  |---|---|---|
  | `+0x0 Version` | 3 | `+0xc8 MOV [RBP-0x50],3` |
  | `+0x8 Pool` | `LdrpThreadPool` | `+0x8d MOV RAX,[LdrpThreadPool]` · `+0xc4 MOV [RBP-0x48],RAX` |
  | `+0x10 CleanupGroup` · `+0x18 CancelCallback` | 0 | `+0x98 AND [RBP-0x40],0` · `+0xa4 AND [RBP-0x38],0` |
  | `+0x20 RaceDll` · `+0x28 ActivationContext` | 0 | `+0xbf MOVDQA [RBP-0x30],XMM0(0)` |
  | `+0x30 FinalizationCallback` | 0 | `+0xb0 AND [RBP-0x20],0` |
  | `+0x38 u.Flags` | 0 | `+0xb8 AND [RBP-0x18],0` |
  | `+0x3c CallbackPriority` | **1** | `+0xcf MOV [RBP-0x14],1` (공개 헤더 이름으로 NORMAL `[추측]`) |
  | `+0x40 Size` | `0x48` | `+0xd6 MOV [RBP-0x10],0x48` |

**2-2. `+0x90/+0x95 RtlAllocateHeap`**
- 힙 = `PEB.ProcessHeap` (`+0x7d MOV RCX,GS:[0x60]` · `+0x8c MOV RCX,[RCX+0x30]`). 크기 **`0xf0`** (`+0x86 MOV R8D,0xf0`) = PDB `_TP_WORK` 크기와 같음. 플래그 **`TppHeapTag + 0x200000`, 거기에 `OR 8`** (`+0x6e ~ +0x7a`; `8` = 0으로 채움). 이 프로세스 `TppHeapTag` 는 끝까지 0 `[관찰, sast/57]` → 플래그 `0x200008`.
- 실패 (`+0xa3 JZ` → `+0x10b`): 상태 `0xc0000017`.

**2-3. `+0xd5/+0xda TppWorkInitialize(일감, 문맥, 환경, 환경 플래그, 멤버 VFuncs, 작업 VFuncs)` 가 채우는 칸** (`_TP_WORK` `0xf0` = 멤버 `0xc8` + `_TP_TASK` `0x20` 중 `Task` 는 `+0xc8`, `WorkState` `+0xe8`)
- 멤버 부분 (`TppCleanupGroupMemberInitialize`, `TppWorkInitialize +0x2c/+0x31` 이 부름. 환경 ≠ 0 이라 `+0x58 JNZ` → `+0x2cc` 쪽):
  - `+0x0 Refcount` = 1 · `+0x8 VFuncs` = `TppWorkpCleanupGroupMemberVFuncs` · `+0x58 Context` = `R8`(0) · `+0xa8 Flags` = 환경 플래그 · `+0x90 Pool` = 환경 `Pool` · `+0x10 CleanupGroup` / `+0x18 CancelCallback` / `+0x60 ActivationContext` / `+0x88 RaceDll` = 환경에서 복사 · `+0x20 FinalizationCallback` = 0 (뒤에 `TpAllocWork` 가 채움) · `+0xc0 CallbackPriority` = 환경 값 (1; 3 이상이면 `0xc000000d`).
  - 스레드 정보에서: `+0x68 SubProcessTag` ← `TEB+0x1720` · `+0x70 ActivityId` (16바이트) ← `TEB+0x1710`.
  - `+0x28 CleanupGroupMemberLinks` 고리 초기화.
  - 활성 활성화 컨텍스트: 환경 값이 0 이라 `RtlQueryInformationActivationContext` (클래스 1, `+0xf3/+0xf8`) 로 지금 컨텍스트를 읽어 `+0x60` 에 담음 (분기 끝까지는 안 따라감 `[정적, 정밀 추적 안 함]`).
  - **풀 쪽 부수 효과:** 풀 `Refcount` (`+0x0`) `INC.LOCK` (`+0x2a2`), 풀 `PoolObjectList` (`+0x50`) 꼬리에 `+0x98 PoolObjectLinks` 를 연결 (풀 `Lock` `+0x48` SRW 를 잡고, `+0x1da BTS.LOCK` · `+0x224 RtlReleaseSRWLockExclusive`). 풀에는 아무 syscall 도 안 부름.
- `TppWorkInitialize` 자기 몫 (`TppWorkInitialize.asm`): `+0x4f/+0x57` `Task.Callbacks`(`+0xc8`) = `TppWorkpTaskVFuncs` · `+0x5e ~ +0x60` `Task.NumaNode`(`+0xd0`) 초기 0 · `+0x11c` 에서 고른 NUMA 노드 번호를 `+0xd0` 에 · `+0x128` `Task.IdealProcessor`(`+0xd4`) ← `TEB+0x1746` · `+0x12b MOV [R14+0xe8],1` (`WorkState` = Insertable 1).
  - 환경 풀의 `+0x1a8` 이 `ActiveProcessorCount` (`+0x1b8` 이 0 이면 `0x7ffe03c0`) 와 **다를 때만** `+0xc4 ~ +0xde` 에서 풀 `Lock` 을 잡고 `TppAdjustRunningThreadGoalWithLock` 을 부른다 (→ 그 안에 `NtSetInformationWorkerFactory` 클래스 8 + `TppPoolUpdateNodeRelation` 이 있음). 로더 풀은 만들 때 `+0x1a8` 을 같은 값으로 채우므로 (`TpAllocPoolInternal +0x228 ~ +0x237`) **안 탄다** `[정적 + 관찰 sast/57 같은 비교]`.
  - 이 함수는 `BindingCount` (풀 `+0x1b0`) 를 **올리지 않는다** (타이머와 다름, 3번 참고).

**2-4. `+0x104/+0x109 TppCleanupGroupAddMember` 조건**
- `+0xfb CMP [RSI+0x10],RDI(0)` → `+0xff JZ +0x114`: 일감의 `CleanupGroup`(`+0x10`) 이 0 이면 건너뜀. 로더 환경의 `CleanupGroup` = 0 → **안 부름.**

**2-5. syscall 이 있나 (`TppWorkInitialize` 아래까지)**
- **없음** (기본 길). 갈래: `RtlAllocateHeap` (힙이 자라면 `NtAllocateVirtualMemory` — 공용 부품, 표 밖), SRW 락, `RtlQueryInformationActivationContext` (호출 목록에 syscall 없음 → "없음" 으로 셈 `[정적, 정밀 추적 안 함]`). 안 타는 갈래에만 `TppAdjustRunningThreadGoalWithLock` 의 syscall 2개가 있다 (2-3).

**2-6. 나머지 줄**
- `+0xe4 MOV [RSI+0x50],R15` : 일감 `+0x50 WorkCallback` = `RDX` (콜백). `+0xf3/+0xf7` : `+0x20 FinalizationCallback` = 환경 `+0x30`. `+0xa5/+0xaa` : `+0xb0 AllocCaller` = **`TpAllocWork` 를 부른 곳의 돌아올 주소** (`[RSP+0x68]`) = `LdrpEnableParallelLoading +0xe2`.
- 성공하면 `+0x118 MOV [R14],RSI` — `*출력 = 일감 포인터`. 시작에서 `+0x67 MOV [R14],0` 으로 지움.
- 오류 갈래: `+0x137 TppRaiseInvalidParameter` + `EAX=0xc000000d` (`+0x13c`) — 조건 : 출력 칸 NULL (`+0x32 / +0x35`), 콜백 NULL (`+0x3b / +0x3e`), 환경 `u.Flags` 하위 2비트 밖이 켜짐 (`+0x44 TEST EDI,0xfffffffc` / `+0x4a JNZ`), `PEB.Ldr.ShutdownInProgress` ≠ 0 (`+0x5d / +0x61`). 로더 값으로는 **안 탄다.**
- `TppWorkInitialize` 실패 (`+0xe2 JS +0x114` → `+0x116 JS +0x143`) 면 `+0x169/+0x16e RtlFreeHeap` 로 일감을 돌려주고 음수. 예외 정리 함수 `TpAllocWork$fin$0` 도 같은 해제.

**2-7. 결과를 어느 전역에**
- **`LdrpMapAndSnapWork`** (rva `0x1d2640`, 8바이트 = `_TP_WORK*`) — `LdrpEnableParallelLoading +0xa9` 가 칸 주소를 줌. (요청서가 짐작한 `LdrpWorkQueue`·`LdrpLoaderWork` 이름은 아님.) 이 전역을 읽고 쓰는 곳 (`_refs/LdrpMapAndSnapWork.tsv`): `LdrpQueueWork +0x59/+0x76` (≠0 이면 일감 보냄) · `LdrpMinimalMapModule +0x296` (≠0 검사) · `LdrpDetectDetour +0xb3/+0xc9/+0xd5` (기다렸다 풀고 0 으로 지움) · `LdrpCompleteProcessCloning +0x78` (0 으로 지움) · 쓰는 곳은 `TpAllocWork +0x118` (출력).

닫힘 여부: **정적으로 닫힘.** 동적 몫은 힙 commit syscall 개수와 `LdrpWorkCallback` 본문(요청 밖) 뿐.

### 3. `RtlpHpGCTimerEnable` (rva `0x11f5dc`, 337B)

근거: `ms/disassemble/ntdll/RtlpHpGCTimerEnable.asm`, `_LdrpInitialize.asm`, `TppInitializeTimer.asm`, `RtlpHpGCTimerSchedule.asm`, `RtlpHpGCTimerCallback.asm`, `_refs/0x1ce270.tsv` · `0x1ce278.tsv` · `0x1ce280.tsv`.

**3-1. 인자**: `RDX` = 풀 (`_LdrpInitialize +0x215 MOV RDX,[LdrpThreadPool]` 가 그대로 둠). `RCX` 는 안 씀 (`+0x52 MOV RCX,[RAX+0x18]` 로 덮음).

**3-2. 조건 — 누가 언제 부르나 (`_LdrpInitialize`)**
- `LdrpInitializeProcess` 가 음수가 아닌 값을 주면 (`+0x167/+0x16c` 뒤 `+0x172 TEST` · `+0x174 JS +0x1a7` 안 탐) → `+0x1e3 JMP +0x205` → `+0x205 [LdrInitState]=3` · `+0x20f BTR.LOCK [PEB+0x50],1` (`ProcessInitializing` 끄기) → **`+0x215 MOV RDX,[LdrpThreadPool]`** · `+0x21c TEST RDX,RDX` · **`+0x21f JNZ 0x18008c209` (= `+0x27d`)** → `+0x27d/+0x282 CALL RtlpHpGCTimerEnable` → `+0x282 JMP +0x221`. `LdrpThreadPool` = 0 이면 `+0x221` 로 바로.
- 반환값은 안 본다 (`+0x221 MOV EDI,[RSP+0x30]` 이 이전 상태를 다시 읽음).

**3-3. test.exe 에서 타나 / `LdrpThreadPool` 은 누가 채우나**
- `LdrpThreadPool` (rva `0x1d25c0`) 을 **쓰는 곳은 `TpAllocPoolInternal` 하나** (출력 칸, `+0x28` 에서 0 으로 지움, `+0x4be` 에서 풀 주소). 호출 쪽에서 칸 주소를 주는 곳은 `LdrpEnableParallelLoading +0x3d LEA RCX` 하나 (`_refs/LdrpThreadPool.tsv` 5줄: 읽기 `_LdrpInitialize +0x215`, `LdrpEnableParallelLoading +0x53/+0x73/+0x8d`, 칸 주소 `+0x3d`). → **`LdrpEnableParallelLoading` 이 채운다.**
- `LdrpEnableParallelLoading` 은 `LdrpInitializeProcess +0x1a42/+0x1a47` 에서 조건 없이 불린다 (`+0x1a26 ~ +0x1a3c` 는 인자 `ECX = [PEB.ProcessParameters+0x40c LoaderThreads]` 를 준비할 뿐: `AppCompatFlags (PEB+0x2c8)` 의 bit 28 이 켜져 있으면 `LoaderThreads` 를 1 로 덮어씀). 풀이 만들어지면 (1번 결론) 풀 ≠ 0 → **test.exe 에서 탄다** `[정적]`. PE_emulator 가 본 `LoaderThreads=0` 이면 `EBX=4`. 풀 만들기가 실패하면 안 탄다.

**3-4. 본문** (모양)
- `+0x24 MOV RAX,[0x1801ce270]` → `+0x2b TEST` → `+0x2e JZ +0x3a`: **이미 타이머가 있으면** `+0x30 EBX=0xff` → `+0x35 JMP +0x136` 로 끝 (아무것도 안 함).
- `+0x77 CMP [Ldr+0x48],DIL` → `+0x7b JNZ 0x18011f708` (= `+0x12c`): 종료 중이면 `TppRaiseInvalidParameter` + `EBX=0xc000000d`.
- 환경 (스택 `RBP-0x50`, `_TP_CALLBACK_ENVIRON_V3`): 버전 3 (`+0x59`), `Pool` = `RDX` (`+0x6c`), 나머지 0, **`CallbackPriority` = 2** (`+0x70 MOV [RBP-0x14],2`, 공개 헤더 이름으로 LOW `[추측]`), 크기 `0x48` (`+0x65`). 일감의 우선순위 1 과 다르다.
- **`+0xa3/+0xa8 RtlAllocateHeap`**: `ProcessHeap`, 크기 **`0x168`** (`+0x8a MOV R8D,0x168` = PDB `_TP_TIMER` 크기), 플래그 `TppHeapTag + 0x100000`, `OR 8` (`+0x90 ~ +0x9c`) → test.exe 에서 `0x100008`. 실패면 `+0xb0 EBX=0xc0000017` 후 끝.
- `+0xb7 ~ +0xbf` : 타이머 `+0xb0 AllocCaller` ← 자기 돌아올 주소 (`[RBP+8]` = `_LdrpInitialize +0x282`).
- **`+0xea/+0xef TppInitializeTimer`**: `RCX=타이머` · `DL=0` (`+0xd4 XOR EDX,EDX` → `WaitTimer`=0) · `R8=&0x1ce270` (`+0xc2 LEA R8`, **문맥 = 전역 칸 자체**) · `R9=&환경` (`+0xbb`) · 스택 `TppTimerpCleanupGroupMemberVFuncs` / `TppTimerpTaskVFuncs` (`+0xde ~ +0xe5` / `+0xcd ~ +0xd6`).
- 성공이면 `+0xf5 LEA RAX,[0x18011eb20]` (= **`RtlpHpGCTimerCallback`**) → `+0xff MOV [RSI+0x50],RAX` (타이머 콜백) · `+0xfc MOV RDI,RSI` → `+0x103 TEST EBX` → `+0x109 CMPXCHG.LOCK [0x1801ce270],RDI` (**0 → 타이머 포인터**, 이기면 `+0x112 JZ +0x128` → `EBX=0` 로 끝).
- **실패 갈래**
  - `+0x121 TpReleaseTimer` (`+0x126` 로 돌아옴) — 오직 **CMPXCHG 에서 졌을 때** (다른 스레드가 먼저 넣음): `+0x112` 안 뛰고 `+0x114 EBX=0xff` → `+0x119 TEST RDI` → `+0x11e MOV RCX,RDI` → `+0x121`. 만든 타이머를 돌려준다. `TppInitializeTimer` 가 음수일 때는 `+0xf3 JS +0x103` → `+0x105 JS +0x119` 에서 `RDI`=0 이라 `TpReleaseTimer` 는 안 부른다 (힙 블록 새는 건 이 함수의 모양 그대로 `[정적]`).
  - `+0x12c TppRaiseInvalidParameter` — 종료 중일 때만 (위 `+0x7b`).
- 반환: 0 = 새로 심음, `0xff` = 이미 있음/졌음, `0xc0000017` = 메모리 없음, `0xc000000d` = 종료 중, 그 밖은 `TppInitializeTimer` 가 준 음수.

**3-5. `+0xea/+0xef TppInitializeTimer` 아래 (줄기 한 단계) — syscall**
- `NtCreateTimer2` · `NtSetTimer2` · `NtAssociateWaitCompletionPacket` 류는 **안 나온다.** 이 타이머는 만들기만 하고 **켜지 않는다.** 켜는 건 `RtlpHpGCTimerSchedule` (아래 3-7). 타이머 큐용 `NtCreateTimer2` 등은 풀을 만들 때 (`TpAllocPoolInternal +0x38e`) 이미 6번 나왔다.
- `TppInitializeTimer` 안 syscall 은 **하나**: `+0x4b ~ +0x57` 에서 풀 `BindingCount` (`+0x1b0`) 에 `XADD.LOCK` 로 1 더하고, **옛 값이 0 이면** (`+0x62 TEST` → `+0x64 JLE +0xac` → `+0xac TEST EDX` → `+0xae JLE` 안 뜀) `+0xcb/+0xd0` `NtSetInformationWorkerFactory(핸들=[풀+0x38], 클래스 3, &1, 4)`.
  - 로더 풀에 `BindingCount` 를 올리는 개체는 이 타이머가 **처음이고 하나뿐**이다 (`LdrpThreadPool` 을 쓰는 곳은 `LdrpEnableParallelLoading` 과 이 길뿐, 일감은 안 올림 — 2-3). → 옛 값 0 → **syscall 1번** `[정적]`.
  - `+0x66 MOV [RBX+0xf0],0` (`Lock`) · `+0x71 MOV [RBX+0x161],SIL` (`WaitTimer`) · `+0x7d/+0x81` `+0x20 FinalizationCallback` ← 환경 `+0x30`. `+0x85 CMP [RBX+0x10],0` → `CleanupGroup` = 0 이라 `TppCleanupGroupAddMember` 안 부름.
- `TppWorkInitialize` 쪽은 2-3 과 같다 (타이머는 `_TP_WORK` 로 시작하는 `_TP_TIMER`).

**3-6. 쓰는 전역 (힙 GC 타이머 전역)**
- **PDB 이름 없음** (`llvm-pdbutil -publics` 로 `.data` 의 앞뒤 이름 표를 뒤져도 rva `0x1ce270` 근처에 공개 이름 없음). `.data` 안 이름 없는 작은 묶음 `[정적 + 추측: 크기 0x18]`:
  | 칸 | 크기 | 뜻 | 쓰는 곳 · 읽는 곳 |
  |---|---|---|---|
  | `0x1ce270` | 8 | **타이머 포인터** (`_TP_TIMER*`) | 비움 `RtlInitializeHeapManager +0x7d` · 채움 `RtlpHpGCTimerEnable +0x109` (CMPXCHG) · 비움 `RtlUnlockHeapManagerForCloning +0x15` · 읽음 `RtlpHpGCTimerSchedule +0x6` 외 힙 함수 십여 곳 |
  | `0x1ce278` | 8 | **기한** (파일시각 8바이트) | 채움 `RtlInitializeHeapManager +0x96` (`RtlpHpQueryGCTimerInterval` 결과 `[추측]`) · `RtlpHpGCTimerSchedule +0x48` 이 `TpSetTimerEx` 에 주소로 넘김 · 힙 함수들이 `LEA` 로 주소 사용 |
  | `0x1ce280` | 4 (자리 8) | **"예약됨" 깃발** | 비움 `RtlInitializeHeapManager +0x85` · 세움 `RtlpHpGCTimerSchedule +0x31` (`CMPXCHG 0→1`) · 지움 **타이머 콜백 `RtlpHpGCTimerCallback +0x29 MOV [RBX+0x10],0`** (`RBX` = 문맥 = `&0x1ce270`, `+0x10` = `0x1ce280`) |
- `RtlInitializeHeapManager` 는 `LdrpInitializeProcess +0x9ed/+0x9f2` 에서 불려서 `+0x1a42` 보다 앞이다 (`_map/…_edges.tsv`). 파일 속 초기 바이트는 0 이 아니지만 거기서 지워지므로 `RtlpHpGCTimerEnable +0x24` 의 "이미 있음" 검사는 처음엔 안 걸린다 `[정적]`.
- 타이머 개체 자체: `_TP_TIMER` `0x168` 바이트 힙 블록 1개 (`ProcessHeap`).

**3-7. 켜기 — 이 요청서 밖이지만 짐 지도에 필요한 것**
- 타이머를 **켜는 곳**은 `RtlpHpGCTimerSchedule` (rva `0x11d308`, 109B): `+0x6 MOV RCX,[0x1801ce270]` ≠ 0 이고 (`+0x14` 에서 0 이면 `0xc00000a3`) 종료 중이 아니며 (`+0x1b CMP [Ldr+0x48]` — 아니면 `0xc000010a`) `+0x31 CMPXCHG.LOCK [0x1801ce280],EDX(1)` 이 0→1 로 이기면 (졌으면 `0x103`) `+0x52 TpSetTimerEx(타이머, &0x1ce278, 0, 1000)` (`R9D=0x3e8` = 1000, `+0x42`). → 안 쪽 syscall 은 `TpSetTimerEx` (rva `0x7eb60`, 1157B) 아래 — **이번엔 안 열었음 `[공백]`**.
- 부르는 힙 코드: `RtlAllocateHeap +0x9ef` (바로 앞 `+0x9df` 에서 칸 하나를 1 로 켜고 `+0x9e5 MOV EAX,[0x1801ce280]` · `+0x9ed JNZ` 로 이미 예약됐으면 건너뜀), `RtlpHpMetadataAlloc +0x53b` 등 (지도 `edges` 에 두 곳; 읽기 `_refs` 에 힙 함수 수십 곳). 즉 `RtlpHpGCTimerEnable` 이 타이머를 심은 **뒤의** 힙 사용(할당·해제)에서 처음 조건을 맞추는 순간 `TpSetTimerEx` 로 켜진다. 정적으론 "언제"를 못 정한다.
- 콜백 `RtlpHpGCTimerCallback` (rva `0x11eb20`, 54B) 은 일꾼 스레드가 불러야 돌아간다 (`RtlpEnumProcessHeaps` → 깃발 지움). 일꾼 스레드가 없는 에뮬레이터에선 안 돈다.

닫힘 여부: 조건·전역·syscall 은 **정적으로 닫힘.** "힙 코드가 init 창 안에서 타이머를 켜나(`RtlpHpGCTimerSchedule`/`TpSetTimerEx`)" 는 **동적 필요.**

### 4. `LdrpEnableParallelLoading` 덫 안 syscall 전체 표 (test.exe 기본 길)

기본 길 가정: `LoaderThreads` = 0 · `RtlGetSuiteMask` bit 16 꺼짐 · `LdrpDetourExist` = 0 · `ActiveProcessorCount` = 2 · `TppHeapTag` = 0 · `TppPoolpDefaultPoolCpuSetCount` = 0 · KUSD `+0x386` = 0 (뒤 넷은 `[관찰, sast/57]`). 아래 번호 = 부르는 차례.

| # | 부르는 자리 (함수 + call/ret) | syscall | 클래스·인자 | 조건 | 횟수 |
|---|---|---|---|---|---|
| 1 | `LdrpEnableParallelLoading +0x1c/+0x21` → `LdrpDetectDetour +0x9c/+0xa1` | `NtQueryInformationThread` | 핸들 `-2`(`+0x91 MOV RCX,-2`), 클래스 `0x2a` (`+0x98 LEA EDX,[R9+0x26]`, R9=4), 버퍼 4B (`[RSP+0x40]`), 길이 4, 반환길이 NULL | `+0x8 CMP [LdrpDetourExist],BL` · `+0x12 JNZ` 로 이미 1 이면 건너뜀 (첫 호출은 0) | 1 |
| 2~3 | `+0x44/+0x49` → `TpAllocPoolInternal +0x240/+0x245` → `TppPoolUpdateNodeRelation +0x4b/+0x50` | `NtQuerySystemInformationEx` | 클래스 `0x6b` (`+0x47 LEA ECX,[R8+0x67]`), 입력 4B 값 6 | 첫 대답이 `0xc0000004` 면 힙에 길이만큼 잡고 한 번 더 (`+0x56 / +0x5b / +0x61 ~ +0xbd`) | 2 |
| 4 | 〃 `+0x292/+0x297` | `NtCreateIoCompletion` | 출력 `&풀+0x40`, 접근 `0x1f0003`, 동시 실행 수 `max(ActiveProcessorCount+1, 4)` (`+0x276 CMP ESI,4`, `+0x279 LEA R9D,[RSI+1]`, `+0x27d JC +0x47e`) = 이 VM 은 4 | 항상 | 1 |
| 5 | 〃 `+0x303/+0x308` | `NtCreateWorkerFactory` | 1-8 인자 | 항상 | 1 |
| 6 | 〃 `+0x443/+0x448` | `NtSetInformationWorkerFactory` | **클래스 `0xd`**, 4B, 값 1 | 둘째 인자 bit0 = 1 (`+0x316 / +0x31e`) — **로더 풀만** | 1 |
| 7~12 | 〃 `+0x38e/+0x393` → `TppInitializeTimerQueue +0x2c/+0x31`·`+0x44/+0x49` → `TppInitializeTimerSubQueue +0x4d/+0x52`·`+0x65/+0x6a`·`+0xa0/+0xa5` | `NtCreateTimer2` → `NtCreateWaitCompletionPacket` → `NtAssociateWaitCompletionPacket` | 큐 둘(절대/상대) 각각 이 순서로 | 항상 | 각 2 (=6) |
| 13 | `+0x61/+0x66` → `TpSetPoolWorkerThreadIdleTimeout +0x39/+0x3e` | `NtSetInformationWorkerFactory` | **클래스 2**, 8B, −300000000 | 1-6 오류 갈래 아님 | 1 |
| 14 | `+0x7a/+0x7f` → `TpSetPoolMaxThreads +0x3b/+0x40` | `NtSetInformationWorkerFactory` | **클래스 5**, 4B, 값 3 (`EBX−1`, `EBX=4`) | 오류 갈래 (`+0xf / +0x13 / +0x26`) 아님 | 1 |
| — | `+0xdd/+0xe2` → `TpAllocWork` (아래 `TppWorkInitialize` 포함) | (없음) | — | 2번 | 0 |
| (15) | **함수 밖** `_LdrpInitialize +0x27d/+0x282` → `RtlpHpGCTimerEnable +0xea/+0xef` → `TppInitializeTimer +0xcb/+0xd0` | `NtSetInformationWorkerFactory` | **클래스 3**, 4B, 값 1 | 풀 `BindingCount` 옛 값 0 | 1 |

- **덫 안 합계 = 14번** (표 1~14). 그 밖에 `RtlAllocateHeap` 이 힙을 늘릴 때의 `NtAllocateVirtualMemory` 는 표 밖 (공용 부품; 이 구간에서 부르는 큰 할당은 풀 `0x1d8` · 노드/큐 배열 · 관계 버퍼 · 일감 `0xf0` 등).
- **PE_emulator "14번째 = `…IdleTimeout`" 과의 차이 1개**: 위 asm 순서로는 `IdleTimeout` 은 **13번째**, `MaxThreads` 가 14번째다. 14번째로 보였다면 그 앞에 **표 밖 1개**가 끼어 있다는 뜻이다 — 후보 1순위 힙 확장 `NtAllocateVirtualMemory` (PE_emulator 의 "표 밖" 줄), 후보 2 `NtQuerySystemInformationEx` 를 3번 부름 (`+0x56 CMP EAX,0xc0000004` 가 계속 맞으면 `TppPoolUpdateNodeRelation +0xbd JMP` 가 `+0x23` (= `0x1800e603b − 0x1800e6018`) 으로 되돌아가 다시 부름). 앞 13개 이름을 위 순서와 대조하면 가려진다.
- 안 타는 갈래 (이름만):
  | 자리 | syscall | 안 타는 조건 |
  |---|---|---|
  | `TpAllocPoolInternal +0x415/+0x41a TpSetPoolThreadCpuSets` | `NtSetInformationWorkerFactory` 클래스 `0xf` | `+0x3ff` 의 `TppPoolpDefaultPoolCpuSetCount` = 0 → `+0x409 JZ +0x420` |
  | `TpAllocPoolInternal +0x4df/+0x4e4 TppETWPoolCreate` | `NtTraceEvent` | KUSD `+0x386` = 0 → `+0x4d6 JZ +0x5b7` |
  | `TpSetPoolMaxThreads +0x81/+0x86 TppETWPoolThreadMax` | `NtTraceEvent` | KUSD `+0x386` = 0 → `+0x51 JNZ` 안 뜀 |
  | `TppGetCurrentThreadNumaNode +0x84/+0x89` · `TppWorkInitialize +0xd0/+0xd5` → `TppAdjustRunningThreadGoalWithLock +0x9b/+0xa0`, `+0xa3/+0xa8` | `NtSetInformationWorkerFactory` 클래스 8 + `NtQuerySystemInformationEx` | 풀 `+0x1a8` ≠ `ActiveProcessorCount` 일 때만 (같으니 `+0x71 JZ` / `+0xc2 JZ` 로 건너뜀) |
  | `TpAllocWork` | (없음) | — |
- 갈림길 정리 (바뀌면 표가 달라지는 곳):
  - `LoaderThreads` (`ECX`) : 0 이면 `+0x29 RtlGetSuiteMask` 로 가서 `bit 16` 이 꺼지면 `EBX = 4`, 켜지면 `EBX = 0`. 0 이 아니면 `min(값, 0x10)` (`+0xf9 ~ +0x105`). **`EBX ≤ 1` 이면 `+0x82 JBE +0xe4` 로 `TpAllocWork` 를 건너뛴다** (1 → 최대 1; 0 → 최대 1).
  - `LdrpDetourExist` ≠ 0 이면 (`+0x8b JNZ +0xe4`) 역시 `TpAllocWork` 건너뜀. 첫 `LdrpDetectDetour` 에서 `NtQueryInformationThread` 가 준 버퍼가 1 이면 이 깃발이 켜진다 (`+0xa5 CMP [RSP+0x40],1`, `+0xac MOV [LdrpDetourExist],1`).
  - `TpAllocPoolInternal` 이 음수면 (`+0x4d JS +0xe4`) 6~14번이 통째로 사라진다.

## PE_emulator 가 그대로 쓸 작은 표

| 함수 (rva) | 인자 | 쓰는 칸 | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|
| `TpSetPoolWorkerThreadIdleTimeout` (`0xe6520`) | `RCX`=풀, `RDX`=INT64 (<0) | 없음 (스택 8B 버퍼만) | `TppRaiseInvalidParameter` (오류 갈래만) | `NtSetInformationWorkerFactory(풀+0x38, 2, &값, 8)` ×1 | syscall 상태 그대로 (오류 갈래면 `0xc000000d`). 부르는 쪽은 안 봄 |
| `TpSetPoolMaxThreads` (`0xe6490`) | `RCX`=풀, `EDX`=개수 (≥0) | 없음 | `RtlGetCurrentServiceSessionId`, (ETW 켜졌을 때만) `TppETWPoolThreadMax` | `NtSetInformationWorkerFactory(풀+0x38, 5, &개수, 4)` ×1 | 없음 (void) |
| `TpAllocWork` (`0x84900`) | `RCX`=출력 `PTP_WORK*`, `RDX`=콜백, `R8`=문맥, `R9`=환경 (V3, 풀 지정) | 일감 `0xf0` 힙 블록 (멤버 `0xc8` + `Task` `0x20` + `WorkState`), 풀 `Refcount`(`+0x0`)·`PoolObjectList`(`+0x50`), `*출력` | `RtlAllocateHeap` · `TppWorkInitialize` → `TppCleanupGroupMemberInitialize` · (CleanupGroup 있으면) `TppCleanupGroupAddMember` · (실패) `RtlFreeHeap` | 없음 (기본 길) | 0 / `0xc0000017` / `0xc000000d` / `TppWorkInitialize` 음수 |
| `RtlpHpGCTimerEnable` (`0x11f5dc`) | `RDX`=풀 (`RCX` 안 씀) | `_TP_TIMER` `0x168` 힙 블록, 풀 `Refcount`·`PoolObjectList`·`BindingCount`(`+0x1b0`), 전역 `0x1ce270` (타이머 포인터) | `RtlAllocateHeap` · `TppInitializeTimer` → `TppWorkInitialize` · (졌을 때) `TpReleaseTimer` · (종료 중) `TppRaiseInvalidParameter` | `NtSetInformationWorkerFactory(풀+0x38, 3, &1, 4)` ×1 (옛 `BindingCount`=0) | 0 / `0xff` / `0xc0000017` / `0xc000000d` / 음수. 부르는 쪽은 안 봄 |
| `LdrpDetectDetour` (`0xe5890`) | 없음 | `LdrpDetourExist`(`0x1d2610`, 1B), (켜지면) `LdrpMapAndSnapWork` 지움 | `LdrpLogInternal`(서명 불일치), `TpWaitForWork`+`TpReleaseWork` (이미 일감 있을 때만) | `NtQueryInformationThread(-2, 0x2a, &4B, 4, NULL)` ×1 | 없음 |
| `TpAllocPoolInternal` (`0xe5a00`) | `RCX`=출력 `PTP_POOL*` (`&LdrpThreadPool`), `EDX`=1 (로더) / 0 (기본) | 풀 `0x1d8` 힙 블록 (`+0x0 Refcount`=1, `+0x38 WorkerFactory`, `+0x40 CompletionPort`, `+0x17a PoolFlags`=1, `+0x1a8`=`ActiveProcessorCount`, `+0x1ac`=`0xfffffffe`, `+0x180 PoolLinks` → `TppPoolpList`), 노드/큐/NUMA 배열 힙 블록 3개, `*출력`=풀 | `RtlImageNtHeaderEx` · `RtlAllocateHeap`×4 · `TppPoolUpdateNodeRelation` · `TppGetCurrentThreadNumaNode` · `TppInitializeTimerQueue` · SRW 락 | 1번 표 2~12 (11개) | 0 이상 성공 / 음수 (실패면 `*출력`=0) |
| `LdrpEnableParallelLoading` (`0xe572c`) | `ECX`=`LoaderThreads` | `LdrpThreadPool`·`LdrpMapAndSnapWork`·`LdrpDetourExist`(읽기) | 위 전부 | 14개 (표 1~14) | `TpAllocWork` 결과 (불렸을 때) 아니면 `TpAllocPoolInternal` 결과. `LdrpInitializeProcess` 는 안 봄 |

## 짐 지도 반영거리 `[정적]`

- **유저 전역 (ntdll `.data`, 에뮬레이터 안)**
  - `LdrpThreadPool` (`0x1d25c0`, 8B) = 로더 풀 포인터. `LdrpMapAndSnapWork` (`0x1d2640`, 8B) = 로더 일감 포인터. `LdrpDetourExist` (`0x1d2610`, 1B).
  - 힙 GC 타이머 묶음 (`0x1ce270` 8B 타이머 포인터 / `0x1ce278` 8B 기한 / `0x1ce280` 깃발) — 이름 없음. 앞 둘은 힙 관리자 초기화(`RtlInitializeHeapManager`)가 먼저 지우거나 씀, 타이머 포인터는 `_LdrpInitialize +0x27d` 길에서 채워짐.
  - `TppPoolpList` (`0x1cc728`, 풀 연결 고리) 에 로더 풀이 기본 풀과 같이 걸린다 (`TpAllocPoolInternal +0x3b5 ~ +0x3e0`, 락 `TppPoolpListLock`).
- **힙 블록 (`ProcessHeap`)**: 풀 `0x1d8` · 노드 큐 `TppNumberNodes*0x48` · NUMA 배열 `TppNumberNodes*4` · 근접 정보 `TppMaximumGroups*TppNumberNodes*0x10` · 일감 `0xf0` · 타이머 `0x168` (+ 노드 관계를 읽는 임시 버퍼, 쓰고 `+0x16a RtlFreeHeap`). 이 중 풀 블록의 플래그는 `TppHeapTag + 0xc0000 | 8`.
- **풀 개체 칸 값** (PDB `_TP_POOL`): `+0x38`/`+0x40` 핸들 (**0 아님**), `+0x17a PoolFlags`=1 (**로더 풀만**), `+0x1a8`=`ActiveProcessorCount`, `+0x1ac`=`0xfffffffe`, `+0x1b0 BindingCount` = 타이머 심은 뒤 1, `+0x0 Refcount` = 1 (`+0x273`) 에 일감·타이머가 걸릴 때마다 +1, `+0x8 QueueState` 는 만들 때 `ActiveProcessorCount` 의 하위 16비트 (`+0x263 ~ +0x26f`, 2) 로 시작. 일꾼 수 등은 일감 큐에 일이 들어갈 때 달라짐 — 이번 요청 밖.
- **커널 개체 (에뮬레이터 바깥, 이 풀 몫)**: IoCompletion 1 · WorkerFactory 1 (최대 스레드 `0x200` → `TpSetPoolMaxThreads` 로 **3**, 유휴 시간 −30초, 플래그 bit 11 = 1, `BindingCount` = 1) · Timer2 2 · WaitCompletionPacket 2. 힙 GC 타이머가 켜지면 (3-7) 그 아래 개체가 더 생길 수 있음.
- 일꾼 스레드가 언제 커널에서 생기는지는 이번 요청 밖 (`NtSetInformationWorkerFactory` 본문 중 클래스 2·3·5 의 분기는 점프표라 Ghidra 가 못 풀었음 `[공백]`).

## 동적분석으로 확인해야 할 것 (Q104 VM 단계에 얹을 몫)

질문별 상태: **1번 정적으로 닫힘 · 2번 정적으로 닫힘 · 3번 정적으로 닫힘 (단 켜는 시점은 동적 필요) · 4번 정적으로 닫힘 (횟수는 표 밖 힙 확장 때문에 ±)**.

dast 요청 필요 (Q104 VM 단계에 얹을 것, 멈춤점은 test.exe 일 때만 걸리게):
1. **핸들이 진짜로 0 이 아닌지 + 풀 칸 사진**: `ntdll!LdrpInitializeProcess +0x1a47` (= rva `0x8de4b`, `LdrpEnableParallelLoading` 돌아온 직후)
   - 읽을 칸: `poi(ntdll!LdrpThreadPool)` 가 풀 포인터 → `dt ntdll!_TP_POOL <풀>`; `+0x38`·`+0x40` ≠ 0, `+0x17a`=1, `+0x1a8`=2, `+0x1ac`=`0xfffffffe`, `+0x1b0`=0.
   - `dq ntdll!LdrpMapAndSnapWork L1` ≠ 0, `db ntdll!LdrpDetourExist L1` = 0, `dt ntdll!_TP_WORK poi(ntdll!LdrpMapAndSnapWork)` (`+0x50 WorkCallback` = ntdll+0xe9ef0, `+0x90 Pool` = 위 풀, `+0xc0` = 1, `+0xe8` = 1).
2. **syscall 순서·개수**: `ntdll!NtSetInformationWorkerFactory` (rva `0x1643c0`) 입구에서 `rcx`·`edx`·`r9d` 찍기 — 기대: (`h`,`0xd`,4) → (`h`,2,8) → (`h`,5,4), 그리고 `_LdrpInitialize +0x27d` 뒤 (`h`,3,4). 같은 `h` 인지 확인. 입구에서 `NtCreateWorkerFactory` (`ntdll+0x162840`) 와 비교해도 됨. 가능하면 이 구간 `syscall` 전체 개수 (표 밖 `NtAllocateVirtualMemory` 가 몇 번인지).
3. **입력 값 확인**: `dd poi(@$peb+0x20)+0x40c L1` (`LoaderThreads`) · `dd 0x7ffe02d0 L1` (SuiteMask bit 16) · `dd 0x7ffe03c0 L1` · `db 0x7ffe0386 L1` · `dd ntdll!TppHeapTag L1` · `dd ntdll!TppPoolpDefaultPoolCpuSetCount L1`.
4. **힙 GC 타이머 켜기**: `ntdll+0x1ce270 L3` (`dq`) — 은퇴 직전과 직후에 읽어 `[+0]` ≠ 0 / `[+8]` 기한 / `[+0x10]` 깃발 변화 확인; `ntdll!RtlpHpGCTimerSchedule` (rva `0x11d308`) 와 `ntdll!TpSetTimerEx` (rva `0x7eb60`) 입구가 은퇴 전에 타는지.
5. 커널 일꾼 공장 객체 값 (유휴 시간 −30초·최대 3·바인딩 1) 은 `!object`/`dt nt!_…` 이름이 PDB 에 있는지부터 확인 필요 — 이름 모름 `[공백]`.

추천 멈춤점 요약: `ntdll!LdrpInitializeProcess +0x1a47` (사진) · `ntdll!NtSetInformationWorkerFactory` 입구 (조건 `process test.exe`) · `ntdll!TpSetPoolWorkerThreadIdleTimeout +0x39` (rva `0xe6559`, `rcx`=핸들) · `ntdll!TpAllocPoolInternal +0x308` (rva `0xe5d08`, `NtCreateWorkerFactory` 돌아온 직후 `poi(@rdi+0x38)`) · `ntdll!_LdrpInitialize +0x27d` (rva `0x8c209`) · `ntdll!RtlpHpGCTimerSchedule` 입구.

## 판정

**분석성공.** 요청 4개 질문 모두 asm 줄 근거로 답함. 핵심(1번 "핸들 0")은 "진짜 길에선 불가능, 에뮬레이터 기본 대답 때문이며 로더 길은 안 깨진다 (클래스 `0xd` 만 성공 필수)" 로 정적으로 닫힘. 남은 `[공백]`: 클래스 `0xd`·2·3·5 의 커널 쪽 이름 (PDB 열거 없음), 힙 GC 타이머가 켜지는 시점과 `TpSetTimerEx` 아래 syscall, `LdrpWorkCallback` 본문(요청 밖), 덫 안 표 밖 힙 확장 syscall 횟수.

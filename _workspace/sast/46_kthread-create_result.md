# 46 kthread-create — 정적 분석 결과 (Q78d1)

- 요청서: `_workspace/sast/46_kthread-create.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) — **기준과 같음.** 사진 로그 `ms/logs/04_baggage-read_K2.txt` 의 `!lmi nt` (`GUID {C29EBFB0-6B78-B3C0-20DC-A66D99713F9E}`, `Age: 1`) 와도 같다.
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음, Ghidra 프로세스 없음).
- **새로 뽑은 함수 9개, 전부 depth 0**, 한 번에 (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`). `_calls.tsv` 는 `comm -23` 0줄 (줄이 안 사라짐), 4,281 → 4,304줄.
  - 요청서 표에 있던 것 2개: `KeInitializeTimerEx`, `MmCreateKernelStack`.
  - **요청서 표에 없던 것 7개 — 요청서 마지막 줄 ("`KeInitThread` 의 다른 callee 중 칸을 쓰는 것") 에 따라 넣음**: `KeAbInitializeThreadState` (**`AutoBoostActive`·`PropagateBoostsEntry`·`IoSelfBoostsEntry`**), `KeInitializeApc` (`SchedulerApcFill*`), `KeInitializeTimer` (`Timer`), `KeInitializeEvent` (3.14 아는 칸 `SuspendEvent` — 이름만 보려고), `KiAllocateXStateStack` (`StateSaveAreaDecoupled`), `KiCreateKernelShadowStack`, `KeCopyXfdMaskToTeb` (이 둘은 조건부 갈래 확인용).
  - **요청서가 적은 `KeInitializeTimerEx` 는 `KeInitThread` 가 부르는 함수가 아니다.** `KeInitThread +0x172` 가 부르는 건 **`KeInitializeTimer`** (rva `0x45b9d0`). `KeInitializeTimerEx` 는 뽑아 놓기만 했다.
- 이미 `.c`/`.asm` 이 있어서 **읽기만 한 것** (다시 안 뽑음): `PspAllocateThread`, `KeInitThread`, `KiInitializeContextThread`, `KxContextToKframes`, `KiStackAttachProcess`, `KeStartThread` (Q78d4 로 넘길 줄만), `PspInsertThread` (앞부분만), `NtCreateUserProcess` (`PspAllocateThread` 를 부르는 인자 줄만).
- **Ghidra 로 안 뽑고 `objdump -d` (scratchpad `nt.dis`) 로만 본 곳** (`.asm` 없음 — 함수+오프셋으로만 적는다): 커널 전체 `[reg+칸]` 쓰기 훑기 (`끝까지 0` 138칸 + `K1~K2` 42칸 전체, 오탐 있음), `KeAccumulateTicks +0x58`, `KiSystemServiceUser +0x11`, `KeAbPreAcquire +0x52`, 그 밖에 "범위 밖 길" 표에 이름만 적은 함수 전부.
- **이름 확인**: 이 결과서에 적은 함수 이름 전부를 `ms/decomp/ntoskrnl/_index.tsv` 로 한 번 더 확인했다 (끝에 "이름 확인 결과" 한 줄). 전역 변수 (`KiKernelCetEnabled`, `KeFeatureBits` …) 는 `pdb_aliases.py --where` 로 PDB 이름과 rva 를 확인했다 (사진·`.asm` 의 주소와 같음).
- **버전 확인: 일치.** 호출 지점 — `NtCreateUserProcess` call `+0xe57` / ret **`+0xe5c`** → `PspAllocateThread`, `PspAllocateThread` call `+0x6f4` / ret `+0x6f9` → `KeInitThread`, `KeInitThread` call `+0x2fc` / ret `+0x301` → `KiInitializeContextThread` (`_calls.tsv`; 앞 바퀴의 `NtCreateUserProcess +0xc36 → PspAllocateProcess` 도 그대로). 그리고 **사진 K2 값과 코드가 칸마다 맞는다**: `StateSaveArea` = `StackBase − 0x9c0` (`KiXSaveAreaLength` 를 0x40 으로 내림), `InitialStack` = 그 − 0x50, `KernelStack` = `InitialStack − 0x310`, `Affinity` = 객체 + 0x860, `UserAffinity` + 0x870, `KernelAbEntries` + 0x880, `UserAbEntries` + 0xaa0 (= 0x880 + 0x220), `ExpectedRunTime` = `0x4c4b40` (전역 `KiShortExecutionCycles` 가 `0x4c4b4` 면 `<< 4` 와 같음 — 거꾸로 센 값, D1 에서 읽어 확인), `MiscFlags` `0x4001`, `ThreadFlags` `0x20040`, `ModeHistory` `0xaaaaaaaa`, `RealtimePriorityFloor` 0x20, `NpxState` `0xe5`. **K2 에서 0 이 아닌 8바이트 덩어리 수가 코드로 센 것과 칸마다 같다** (예: `WaitBlock` 8, `Timer` 4, `SchedulerApcFill3` 6). 바이너리와 타깃 VM 이 같다고 볼 수 있다.

## 스크립트 문제

없다. (`run_ghidra.sh export` 한 번, `pdb_struct.py` 여러 번, `pdb_aliases.py --where` 여러 번 — 모두 문제없이 돌았다. 도구 제안은 맨 아래.)

## 판단 기준 (한 줄)

**만들기 길** = `NtCreateUserProcess → PspAllocateThread` 본문 + 새 `_KTHREAD` 포인터를 넘겨받는 callee 줄기 (`KeInitThread`, `KiInitializeContextThread`, `KeAbInitializeThreadState`, `KeInitializeApc`/`Event`/`Timer` (넘기는 주소가 스레드 안 칸), `KiAllocateXStateStack`, `KiCreateKernelShadowStack`, `KeCopyXfdMaskToTeb`, `MmCreateKernelStack` (인자 구조체로 결과만 돌려줌)). 이 함수들의 `.asm` 에서 칸 오프셋으로 쓰는 줄이 있으면 → 값 0 을 쓰거나 **같은 DWORD·QWORD 를 읽어 일부 비트만 바꿔 되쓰는 줄은 "0 을 씀" (run50 기준과 같음)**, 켜는 줄이 있는데 test.exe 에서 조건이 안 맞으면 **"조건부 안 탐"**; 만들기 길 안에 줄이 없으면 커널 전체 `objdump` 훑기 (`[reg+칸오프셋]` 쓰기, 오프셋 숫자 일치라 **오탐 있음**) 로 이름 있는 스레드·스케줄러 쪽 함수가 보이면 **"범위 밖 길에서 씀 `[추측]`"**, 그것도 안 보이면 **"아무도 안 씀 (만들기 길 안)"**.

- **몸통 0 채움은 `PspAllocateThread +0x307 memset(스레드, 0, 크기)`** 가 한다 (`ObCreateObjectEx` 호출 `+0x2e2` 바로 뒤, 크기 = 객체를 만들 때 넘긴 크기 `[RSP+0x98]` 그대로 → 몸통 + 친화도 블록 + `_KLOCK_ENTRIES` 블록 전부). 그래서 모든 칸은 0 에서 시작한다. `ObpAllocateObject` 쪽 0 채움 비트는 안 봤다 (memset 이 있어서 필요 없음). "아무도 안 씀" 의 0 은 이 memset 에서 온다.
- **같은 이름이 다른 칸**: 이 결과서의 `ThreadLock` 은 **KTHREAD `+0x40` (스케줄러 스핀락)**. baggage 3.13 의 `ThreadLock` 줄 (`PspAllocateThread +0x51c`, `PspInsertThread +0x499`) 은 **ETHREAD `+0x590` 푸시락** 이라 다른 칸이다.
- **부모(현재) 스레드 칸은 안 센다**: `PspAllocateThread` 는 새 프로세스 주소 공간에 TEB·스택을 쓰려고 `KiStackAttachProcess` (`PspAllocateThread +0x78d`, `MmCreateTeb +0x7a`, `PspSetupUserStack +0x70`) 를 부른다. 이 함수는 `MOV RBX, GS:[0x188]` (`KiStackAttachProcess +0xe`) 로 **지금 도는 스레드 (부모 cmd)** 의 `ApcState`·`ApcStateIndex` (`+0x162`)·`MiscFlags` 비트 11 `ProcessDetachActive` (`+0x184 OR 0x800` → `+0x238 AND`) 를 썼다 되돌린다. 새 스레드 칸이 아니다. `PspAllocateThread` 안에서 부르는 `KeAbPreAcquire +0x52` (`ThreadFlags` 비트 16)·`KxContextToKframes +0x3ba` (`Header` 비트 24) 도 호출한 쪽 (부모) 스레드의 칸이다.

## 요약 표 (180칸 → ?)

| | 칸 | 아는 칸 (nt) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (만들기 길 안) | 범위 밖 길에서 씀 `[추측]` | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `K1~K2` | 42 | **42** | — | — | — | — | 0 |
| `끝까지 0` | 138 | — | **35** | **12** | **21** | **70** | 0 |
| **합** | **180** | **42** | **35** | **12** | **21** | **70** | **0** |

- `K1~K2` 42 = 쓰는 줄을 찾고 사진 K2 값과 맞춰 봄. `*` 칸 (사진에서 뒤에 또 바뀜) 은 **처음 값만** 닫음 — 뒤에 바꾸는 쪽은 이름만.
- `0 을 씀` 35 = 같은 DWORD 되쓰기 (`MiscFlags` 15 + `ThreadFlags` 20). run49 방식으로 합치면 baggage 2장 "아는 칸" 에 `42 + 35 + 12 = 89` 가 들어가고 (줄이 있는 칸), "아무도 안 씀" 은 `21`, "범위 밖 길" 70 은 flow-writer 가 정한다 (run47·run49·run50 과 같은 방식).
- 조건부 12 중 **1칸 (`ExtendedFeatureDisableMask`) 은 "갈래를 안 탔다" 와 "탔는데 값이 0" 을 정적으로 못 가른다** `[공백]` (둘 다 사진 0). 전역 두 개를 읽으면 닫힌다 (D1).
- **사진 주의**: K1 (`PspAllocateThread` 입구) 에서 읽은 이 스레드 주소는 **만들기 전이라 옛 객체 값** 이다 (`Process` 가 다른 EPROCESS `…714c080`, 스택 값도 다름). 그래서 `끝까지 0` 판정은 **K2 ~ P2 일곱 장** 기준이다. K1 에서 0 이 아니던 `끝까지 0` 칸 5개 (`CetUserShadowStack`, `SpecCtrl` 2, `BamQosLevel`, `QueueListEntry`) 도 K2 부터는 0.

## 한눈에

- **`K1~K2` 42칸은 세 함수가 거의 다 쓴다**: `KeInitThread` (스택 값·`ThreadFlags`·`ApcState`·`Timer`·`WaitBlock`·`Process`·`MutantListHead`·리스트 머리 표시·`ModeHistory`), `PspAllocateThread` (`Header` 에너지 비트·`Affinity`·`UserAffinity`·`RealtimePriorityFloor`·`AbEntries` 둘), `KiInitializeContextThread` (`StateSaveArea`·`PreviousMode`·`NpxState`). 나머지는 도우미 셋: `KeAbInitializeThreadState` (`AutoBoostActive`·`PropagateBoostsEntry`·`IoSelfBoostsEntry`), `KeInitializeApc` (`SchedulerApc` 안쪽), `KeInitializeTimer` (`Timer` 안쪽).
- **겹치는 칸 (`*Fill*`)** 은 같은 바이트를 가리키는 자리다. `WaitBlockFill5~11` 은 **대기 블록 4개의 `Thread` 칸** (`+0x158 · +0x188 · +0x1b8 · +0x1e8`) 과 마지막 블록의 줄 연결 (`+0x1d0 · +0x1d8`) 과 `+0x1e0` DWORD 를 어디까지 덮느냐만 다르다. `SchedulerApcFill1~5` 는 `_KAPC` 앞쪽 (`Type`·`Size`·`Thread`·루틴 넷·`NormalContext`) 을 어디까지 덮느냐만 다르다. 같은 줄이 여러 칸의 쓰는 줄이다.
- **`끝까지 0` 138칸 중 `MiscFlags` 21 + `ThreadFlags` 20 = 비트 41개가 약 3분의 1이다.** 둘 다 `KeInitThread` 가 DWORD 를 읽고 몇 비트만 바꿔 통째로 되쓴다 (`+0xe7 → +0xfd`, `+0x80 → +0x9d`). 그 중 6비트 (`SecureThread`·`SystemThread`·`CetUserShadowStack`·`BypassProcessFreeze`·`CetKernelShadowStack`·`StateSaveAreaDecoupled`) 는 켜는 줄이 따로 있는데 **test.exe 첫 스레드는 안 탄다** (사진 K2 `MiscFlags` `0x4001` 에 해당 비트가 없음).
- **test.exe 는 "사용자 스레드, 프로세스가 `_CONTEXT` 를 줌, 새 커널 스택을 만듦, CET 꺼짐" 길이다.** 갈림길 네 개: (1) 시스템 스레드 갈래는 `ThreadContext == 0` 일 때 (`KeInitThread +0x112`) — `NtCreateUserProcess +0xe3a` 가 `_CONTEXT` 포인터를 넘겨서 안 탐. (2) 커널 스택은 `*param_2 == 0` 이라 `MmCreateKernelStack` 을 부른다 (`+0x234`). (3) 커널 CET 는 전역 `KiKernelCetEnabled` (`+0x2b0`) 가 꺼져 있다 (사진 `MiscFlags` 비트 22 = 0). (4) 사용자 CET 는 프로세스 `MitigationFlags2.CetUserShadowStacks` (비트 14, `PspAllocateThread +0x5b9`) 가 꺼져 있다 (sast44: K1 `0x40000000` 에 비트 14 없음, 사진 `MiscFlags` 비트 20 = 0).
- **만들기 길 안에 간접 호출은 없다.** 위 함수 14개의 `.asm` 에서 `CALL reg`·`CALL [mem]`·`cfg:@` 를 찾았는데 0개. (`KeInitThread` 에 `INT 0x29` 하나는 연결 리스트 망가짐 검사 `fastfail` 이다.) → Q35 와 무관.

## 질문별 답 [정적]

경로 약어 (`(호출 +0x..)` = 부르는 자리, ret 아님):

- `①` = `NtCreateUserProcess`
- `①A` = `① → PspAllocateThread (호출 +0xe57)`
- `①K` = `①A → KeInitThread (호출 +0x6f4)`
- `①C` = `①K → KiInitializeContextThread (호출 +0x2fc)`

`KeInitThread` 둘째 인자 (이하 `param_2`, `R15`) 는 `PspAllocateThread` 가 `[RSP+0x120]` 부터 채운 안내 블록이다 (`+0x679 ~ +0x6e9`). 칸 이름은 `KeInitThread` 가 쓰는 자리로 센 것: `+0x00` 이미 있는 커널 스택 (0 = 새로 만듦) · `+0x08` 시작 루틴 (`PspUserThreadStartup` rva `0x910de0`; 시스템 스레드는 `PspSystemThreadStartup` `0x494470`, 보안 스레드는 `PspSecureThreadStartup` `0xaaf3b0`) · `+0x10` 시작 인자 (호출자가 준 값 `[RSP+0xa8]`) · `+0x18` 시작 주소 (`_CONTEXT.Rip`) · `+0x20` **`ThreadContext` (0 이면 시스템 스레드)** · `+0x28` TEB (`MmCreateTeb` 결과, 3.14 `Teb`) · `+0x30` 프로세스 · `+0x38` 이상적 노드 + 1 (0 = 정하지 않음) · `+0x3c` 플래그 (test.exe 는 0) · `+0x40` 확장 기능 마스크 (아래 `ExtendedFeatureDisableMask`).

### 1. `K1~K2` 42칸 쓰는 줄

**답: 42칸 모두 쓰는 줄을 찾았고 사진 K2 값과 칸마다 맞춰 봤다. 이 중 20칸 (`*`) 은 K3 이후 또 바뀌는데, 처음 값 줄만 정확이고 뒤에 바꾸는 쪽은 이름만 적었다.**

#### 1.1 칸마다 (baggage 3.14 양식, 바로 옮길 수 있게)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `Header*` (24B) | ① | `Type` = 6 (스레드 객체); 기다림 줄 머리 (`+0x8`) 자기 연결; **`ThreadControlFlags.EnergyProfiling`** (DWORD 비트 21) 켬 — 에너지 추정이 켜져 있을 때. 안 타는 줄 둘: `SchedulerAssist` (비트 22; `KiSchedulerAssistThreadFlagEnabled` ≠ 0 일 때 `+0x32b`), `Minimal` (비트 26; 사용자 스택 인자가 없고 프로세스 `PicoContext` ≠ 0 인 피코 갈래 `+0x2ca9e9`). 사진 K2 `0x200006` | 직접 (`lock bts`, `MOV`) | nt | `①K +0x54 · +0x58` (자기 연결), `+0x1eb` (`Type`=6); `①A +0x321` (조건 `PoEnergyEstimationEnabled` 호출 `+0x223` → `+0x30c`); 안 타는 줄 `①A +0xadb` · `+0x2ca9e9` | `[정적]` sast46 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3` (K3 `0xa00006` 비트 23 ← `KeStartThread +0x353`, Q78d4) |
| `+0x30` `StackLimit` · `+0x38` `StackBase` (+ `+0x28` `InitialStack` 첫 값) | ① | `StackBase` = `MmCreateKernelStack` 가 돌려준 스택 맨 위 (인자 구조체 `+0x18`, `MmCreateKernelStack +0x299` 가 씀); `StackLimit` = `StackBase − KeKernelStackSize` (전역). `InitialStack` 도 같은 값으로 먼저 쓰고 `KiInitializeContextThread +0x2ca` 가 덮는다 (3.14 아는 줄은 이 둘째 쓰기) | 직접 | nt | `①K +0x29f` (`StackBase`) · `+0x2ac` (`StackLimit`) · `+0x29b` (`InitialStack` 첫 쓰기); 값 `①K → MmCreateKernelStack (호출 +0x273)` 결과 `[RSP+0x80]` (`+0x282`) | `[정적]` sast46 · 정확 · 사진 `K1~K2` (`…9ad08000` / `…9ad01000`, 차 `0x7000` = `KeKernelStackSize`) |
| `+0x54` `ExpectedRunTime*` | ① | 전역 `KiShortExecutionCycles` (`+0xa0`) 를 4 비트 왼쪽으로 민 값. 사진 K2 `0x4c4b40` (전역이 `0x4c4b4` 였을 것 — 거꾸로 센 값, D1) | 직접 | nt | `①K +0xa9` | `[정적]` sast46 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1a~P1b`·`P1b~P2` (스케줄러, `KiSwapThread` 쪽 `[추측]`) |
| `+0x60` `StateSaveArea` | ① | 커널 스택 맨 위에서 `KiXSaveAreaLength` (전역) 만큼 내려와 0x40 으로 내림 (= 스택 안의 XSAVE 영역 주소). 사진 K2 `StackBase − 0x9c0`. 갈래: 따로 스택을 만드는 `KiAllocateXStateStack +0x5a` (AMX 기능이 있을 때만 — 안 탐) | 직접 | nt | `①C +0x2d2` (값 `+0x39 SUB`, `+0x3f AND RDI,-0x40`); 안 타는 줄 `①K → KiAllocateXStateStack (호출 +0x4b9) +0x5a` | `[정적]` sast46 · 정확 · 사진 `K1~K2` (`…9ad07640`) |
| `+0x74` `MiscFlags.AutoBoostActive` (비트 0) · `MiscFlags*` | ① | 비트 0 켬 (`OR [+0x74], 1`). 사진 K2 `0x4001` = 이 비트 + 3.14 아는 칸 `ApcQueueable` (비트 14). DWORD 의 다른 쓰는 줄 모음은 2.1 | 직접 (`OR`) | nt | `①K → KeAbInitializeThreadState (호출 +0x2c0) +0xb2` | `[정적]` sast46 · 정확 · 사진 `K1~K2` → 바뀜 `P1b~P2` (`Alertable` 비트 4 — 7.2 `P1b~P2` 칸) |
| `+0x78` `ThreadFlags.EnableStackSwap` (비트 6) · `.KernelStackResident` (비트 17) · `ThreadFlags*` | ① | 두 비트 켬 (`OR 0x20040`). 같은 DWORD 에서 비트 2 `AutoAlignment` 만 프로세스 값 복사. 사진 K2 `0x20040` | 직접 | nt | `①K +0x97` (`OR EDX,0x20040`) → `+0x9d` (`MOV [+0x78]`) | `[정적]` sast46 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3` (비트 13 ← `KeStartThread +0xb9 · +0x1b7`, Q78d4), `P1a~P1b` (비트 16 ← `KeAbPreAcquire +0x52` `[추측]`) |
| `+0x98` `ApcStateFill*` (43B) | ① | `ApcState` 앞쪽: 커널 APC 줄 머리 (`+0x98`) 와 유저 APC 줄 머리 (`+0xa8`) 를 각각 자기 연결, `Process` (`+0xb8`) = 새 프로세스. 사진 K2 8바이트 덩어리 5개 | 직접 | nt | `①K +0xb3 · +0xb7` (머리 0), `+0xc1 · +0xc5` (머리 1), `+0xcc` (`Process`) | `[정적]` sast46 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3`·`P0~P1` (APC 줄 변화, 3.14 `ApcState` 줄) |
| `+0x100` `Timer*` (64B) | ① | `KeInitializeTimer`: `Type` = 8, 기다림 줄 머리 자기 연결, `DueTime`·`Period`·`TimerType` = 0. 이어서 `Dpc` 칸 (`+0x130`) 에 `KiWaitAlways`·`KiWaitNever` 와 타이머 자기 주소로 섞은 값 (DPC 포인터 자리에 "없음" 을 같은 방식으로 섞은 것으로 보임 `[추측]`). 마지막으로 `WaitBlock[3]` 을 이 타이머의 기다림 줄에 매단다 (줄 머리가 `+0x1d0` 를 가리키게 됨). 사진 K2 덩어리 4개 (`Type`·줄 머리 둘·`Dpc`) | 직접 / 주소 넘김 (→ `KeInitializeTimer`) | nt | `①K → KeInitializeTimer (호출 +0x172) +0x9 · +0xc · +0x10 · +0x13 · +0x17 · +0x1a`; `①K +0x193` (`Dpc`), `+0x1d2 · +0x1d5 · +0x1d9 · +0x1dd` (줄 연결) | `[정적]` sast46 · 정확 · 사진 `K1~K2` → 바뀜 `P1a~P1b` (타이머 걸림) |
| `+0x140` `WaitBlock*` (192B) · `WaitBlockFill5~11*` · `+0x186` `WaitIrql*` | ① | **대기 블록 4개의 `Thread` 칸 (`+0x158 · +0x188 · +0x1b8 · +0x1e8`) 에 모두 새 스레드**. 마지막 블록 (`WaitBlock[3]`, 타이머 기다림용) 은 추가로 `+0x1d0 · +0x1d8` (줄 연결, 위 `Timer` 줄) 과 `+0x1e0` DWORD = `0x1020401` (`WaitType`·`BlockState`·`WaitKey` 자리). `WaitIrql` = 1 (`param_2+0x3c` 비트 1 이 켜져 있으면 2). 겹침: `Fill5` ⊃ 블록0 `Thread`; `Fill6` ⊃ 블록0~1; `Fill7` ⊃ 블록0~2 + 블록3 줄 연결 + `+0x1e0`; `Fill8` ⊃ 블록0; `Fill9` ⊃ 블록0~1; `Fill10` ⊃ 블록0~2; `Fill11`·`WaitBlock` ⊃ 전부. 사진 K2 8바이트 덩어리 수: `WaitBlock` 8 · `Fill5` 2 · `Fill6` 3 · `Fill7` 7 · `Fill8` 1 · `Fill9` 3 · `Fill10` 4 · `Fill11` 8 — **코드로 센 수와 같음** | 직접 | nt | `①K +0x69 ~ +0x7e` (루프 4번, `+0x73 MOV [RAX],RDI`, `+0x76` 0x30 씩 전진); `+0x19e` (`+0x1e0`); `+0x10c` (`WaitIrql`, 값 `+0x101 ~ +0x10a`) | `[정적]` sast46 · 정확 · 사진 `K1~K2` → 바뀜 `K3~P0`·`P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` (대기, `KiCommitThreadWait` 쪽 `[추측]`); `WaitIrql` → 바뀜 `P0~P1` |
| `+0x220` `Process` | ① | `param_2+0x30` 의 새 프로세스 (EPROCESS). 같은 값이 `ApcState.Process` (`+0xb8`) 에도 들어감 (위 `ApcStateFill`). 사진 K2 `…908b080` = test.exe | 직접 | nt | `①K +0xd7` (`+0xd3` 읽기) | `[정적]` sast46 · 정확 · 사진 `K1~K2` |
| `+0x228` `UserAffinity` · `+0x240` `Affinity` | ① | **몸통 뒤 확장 영역 안의 `_KAFFINITY_EX` 를 가리키는 포인터.** `Affinity` = 객체 + `[RSP+0xb8]` (= 0x798 + 에너지 칸 0xc8 = **0x860**), `UserAffinity` = 그 뒤 8 + 그룹 수 × 8 (**0x870**). 안쪽은 `Count` = 1, `Size` = 그룹 수 (`KeQueryMaximumGroupCount` 호출 `+0x23c`), 비트맵은 `memset` 0. 비트맵에 실제 친화도가 들어가는 때는 안 봄 `[공백]` (Q78d4: `KeStartThread` 쪽) | 직접 (포인터) | nt | `①A +0x36c` (`Affinity`) · `+0x3ab` (`UserAffinity`); 안쪽 `+0x379 ~ +0x39a` · `+0x3b2 ~ +0x3c7` | `[정적]` sast46 · 정확 (포인터) · 사진 `K1~K2` (`…58e0` / `…58f0`) |
| `+0x232` `PreviousMode` | ① | 1 (사용자 모드). 시스템 스레드는 0 (`+0x39b`). 시스템 호출 진입마다 `KiSystemServiceUser +0x11` 이 같은 값 1 을 다시 씀 (objdump) | 직접 | nt | `①C +0x25e` | `[정적]` sast46 · 정확 · 사진 `K1~K2` |
| `+0x250` `NpxState` | ① | `((KeEnabledSupervisorXStateFeatures & 0x400) \| KUSER_SHARED_DATA+0x3d8) & ~0x40003 \| (확장 마스크 값) \| 1`, `MiscFlags` 비트 20 (`CetUserShadowStack`) 이 켜져 있으면 `\| 0x800`. 사진 K2 `0xe5` (= 공유 데이터 기능 `…e7` 에서 비트 0·1 을 정리하고 1 을 더한 모양) | 직접 | nt | `①C +0x25b` (값 `+0x211 ~ +0x257`); 시스템 스레드 갈래 `+0x388` | `[정적]` sast46 · 정확 · 사진 `K1~K2` |
| `+0x288` `SchedulerApcFill1~5*` | ① | `KeInitializeApc` 가 `_KAPC` 앞쪽을 채운다: `Type` = 0x12, `Size` = 0x58, `AllFlags` = 0, `Thread` = 새 스레드, `KernelRoutine`·`RundownRoutine` = rva `0x4a10c0` (같은 자리에 PDB 이름 100개 — 하는 일 없는 stub; 뜻이 맞는 이름은 `KiSchedulerApcNop`·`KiSchedulerApcRundown`), `NormalRoutine` = `KiSchedulerApc` (rva `0x2c8950`), `NormalContext` = 새 스레드, `ApcStateIndex`·`ApcMode`·`Inserted` = 0. `Fill1` = 앞 3바이트 (`0x580012`), `Fill2` = 4바이트 (`QuantumReset` 자리까지), `Fill3` = 64B, `Fill4` = 72B, `Fill5` = 83B. `SystemArgument1·2` (`WaitPrcb`·`LegoData` 자리) 는 안 씀 | 주소 넘김 (→ `KeInitializeApc`) | nt | `①K → KeInitializeApc (호출 +0x152) +0x5 (Type) · +0x8 (Size) · +0x56 (AllFlags) · +0x2f (Thread) · +0x3b (KernelRoutine) · +0x26 (RundownRoutine) · +0x3f (NormalRoutine) · +0x4e (NormalContext) · +0x1f · +0x4b · +0x52` | `[정적]` sast46 · 정확 · 사진 `K1~K2` → 바뀜 `K2~K3` (`Fill2` 는 `QuantumReset` 겹침 ← `KeStartThread +0x1c1`; `Fill3~5` 는 APC 큐에 넣을 때), `P0~P1` (`Fill5`) |
| `+0x308` `MutantListHead` | ① | 자기 연결 (비어 있음). 사진 K2 `…5388 - …5388` | 직접 | nt | `①K +0x62 · +0x66` (`+0x5b` 주소 만들기) | `[정적]` sast46 · 정확 · 사진 `K1~K2` |
| `+0x328` `PropagateBoostsEntry` · `+0x330` `IoSelfBoostsEntry` | ① | 둘 다 1 ("줄에 안 들어 있음" 표시) | 직접 | nt | `①K → KeAbInitializeThreadState (호출 +0x2c0) +0xbb · +0xc2` | `[정적]` sast46 · 정확 · 사진 `K1~K2` |
| `+0x370` `GlobalForegroundListEntry` / `ForegroundDpcStackListEntry` · `+0x3f0` `GlobalUpdateVpThreadPriorityListEntry` / `UpdateVpThreadPriorityDpcStackListEntry` | ① | 각각 앞 8바이트 (`Flink`/`Next`) 에 1. **뒤 8바이트 (`InGlobalForegroundList` `+0x378`, `InGlobalUpdateVpThreadPriorityList` `+0x3f8`) 는 안 씀** (0). 사진 K2 `[1 - 0]` | 직접 | nt | `①K +0x2c5` (`+0x370`) · `+0x2cc` (`+0x3f0`) | `[정적]` sast46 · 정확 · 사진 `K1~K2` |
| `+0x404` `RealtimePriorityFloor` | ① | `0x20` | 직접 | nt | `①A +0x3ff` | `[정적]` sast46 · 정확 · 사진 `K1~K2` |
| `+0x44c` `ModeHistory*` | ① | 사용자 스레드는 `0xaaaaaaaa`, 시스템 스레드 (`MiscFlags` 비트 10) 는 0 | 직접 | nt | `①K +0x333` (값 `+0x323 ~ +0x32d`) | `[정적]` sast46 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` (시계 틱마다 한 칸씩 밂 — `KeAccumulateTicks +0x58` objdump `[추측]`) |
| `+0x470` `KernelAbEntries` · `+0x478` `UserAbEntries` | ① | **몸통 뒤 확장 영역 안 `_KLOCK_ENTRIES` 블록을 가리키는 포인터.** 커널용 (6칸짜리, 0x220B) = 객체 + **0x880**, 사용자용 (8칸짜리, 0x2d0B) = 객체 + **0xaa0**. 블록 앞부분: `+0x0` = 스레드, `+0xc` = 칸 수 (6 / 8). **사용자용은 `ThreadContext ≠ 0` 일 때만** (`+0x359`). 블록 안 칸 상태 초기화는 `KeAbInitializeThreadState` (아래 `+0x4b ~ +0xad`) | 직접 (포인터) | nt | `①A +0x352` (커널용; 블록 쓰기 `+0x34b · +0x34e`) · `+0x8e1` (사용자용; 블록 쓰기 `+0x8da · +0x8dd`) | `[정적]` sast46 · 정확 (포인터) · 사진 `K1~K2` (`…5900` / `…5b20`) |

- 위 표는 `K1~K2` 42칸을 모두 센다: `Header` 1 + 스택 둘 (`StackLimit`·`StackBase`) 2 + `ExpectedRunTime` 1 + `StateSaveArea` 1 + `MiscFlags` 둘 2 + `ThreadFlags` 셋 3 + `ApcStateFill` 1 + `Timer` 1 + 대기 블록 묶음 (`WaitBlock`·`WaitBlockFill5~11`·`WaitIrql`) 9 + `Process` 1 + 친화도 둘 2 + `PreviousMode` 1 + `NpxState` 1 + `SchedulerApcFill1~5` 5 + `MutantListHead` 1 + 부스트 줄 둘 2 + 리스트 머리 넷 4 + `RealtimePriorityFloor` 1 + `ModeHistory` 1 + `AbEntries` 둘 2 = **42**.
- **칸 수 대조**: 7.2 `K1~K2 (42)` 목록의 이름을 하나씩 위 표에서 찾았다 — 빠진 이름 0, 더 들어간 이름 0 (`ktcells.py` scratchpad 로 이름을 PDB 칸 표 `struct__KTHREAD.tsv` 와 짝지음). `InitialStack`·`SchedulerApc`·`SuspendEvent` 는 3.14 아는 칸이라 42 에 안 들어가고 표에서 "값 대조" 로만 언급했다.
- `*` 칸 (사진에서 뒤에 또 바뀜) 은 모두 20개: `Header`, `ExpectedRunTime`, `MiscFlags`, `ThreadFlags`, `ApcStateFill`, `Timer`, `WaitBlock`, `WaitBlockFill5~11` (7), `WaitIrql`, `SchedulerApcFill2~5` (4), `ModeHistory`. 바뀐 구간은 K2~P2 일곱 장을 칸마다 비교해 근거 칸에 적었다.
- 값 대조: 사진 K2 를 `ktraw.py` (scratchpad) 로 칸마다 뽑아 비교. K1 은 옛 객체라 비교에 안 씀 (위 "사진 주의").

### 2. `끝까지 0` 138칸 가르기

**답: 0 을 씀 35 · 조건부 안 탐 12 · 아무도 안 씀 21 · 범위 밖 길에서 씀 70 (합 138). 판단 기준은 위 한 줄.**

#### 2.1 0 을 씀 (35) — 같은 DWORD 되쓰기

| 묶음 | 칸 | 쓰는 줄 | 비고 |
|---|---|---|---|
| `MiscFlags` 15비트 | `ReadyTransition` · `WaitNext` · `SystemAffinityActive` · `Reserved1` · `ApcInterruptRequest` · `QuantumEndMigrate` · `TimerActive` · `ProcessDetachActive` · `Reserved2` · `ScbReadyQueue` · `Reserved3` · `WaitNextClearWobPriorityFloor` · `TimerSuspended` · `SuspendedWaitMode` · `Reserved` (24:8) | `①K +0xe7` (DWORD 읽기) → `+0xfd` (`MOV [R12], EDX`, `ApcQueueable` 비트 14 만 바꿔 되씀) | 기준: 같은 DWORD 를 쓰는 줄이 있으면 0 을 쓴 것으로 봄 (run50 과 같음). 범위 밖에서 이 비트를 켜는 곳 `[추측]` (비트마다 `BTS`/`BTR`/`OR`/`AND` 즉시값을 풀어 짝지음 — `ktbits.py` scratchpad): 대기 (`KeWaitForMultipleObjects`·`KeDelayExecutionThread`·`KeWaitForMutexObject`·`KiBeginThreadWait` — `WaitNext`), APC (`KeInsertQueueApc`·`KiSignalThreadForApc` — `ApcInterruptRequest`), 친화도 (`KeSetSystemGroupAffinityThread`·`KeRevertToUserGroupAffinityThread` — `SystemAffinityActive`), 붙이기 (`KiStackAttachProcess`·`KiAttachProcess`·`KiDetachProcess` — `ProcessDetachActive`, 부모 스레드), 퀀텀·스케줄러 (`KiQuantumEnd` — `QuantumEndMigrate`, `KiAddThreadToScbQueue` — `ScbReadyQueue`, `KiCommitThreadWait` — `TimerActive`, `KiSchedulerApc` — `SuspendedWaitMode`) |
| `ThreadFlags` 20비트 | `UserIdealProcessorFixed` · `IsolationWidth` · `AutoAlignment` · `DisableBoost` · `AlertedByThreadId` · `QuantumDonation` · `GuiThread` · `DisableQuantum` · `ChargeOnlySchedulingGroup` · `DeferPreemption` · `QueueDeferPreemption` · `ForceDeferSchedule` · `FreezeCount` · `TerminationApcRequest` · `TerminateRequestReason` (18:2) · `ProcessStackCountDecremented` · `RestrictedGuiThread` · `VpBackingThread` · `EtwStackTraceCrimsonApcDisabled` · `EtwStackTraceApcInserted` (24:8) | `①K +0x80` (DWORD 읽기) → `+0x9d` (`MOV [RDI+0x78], EDX`) | **`AutoAlignment` (비트 2) 는 프로세스 `KPROCESS.ProcessFlags.AutoAlignment` (`+0x88` 비트 0) 를 복사한다** (`+0x83 ~ +0x95`) — test.exe 는 0 (사진). 범위 밖에서 켜는 곳 `[추측]`: `KeStartThread` (비트 0 `UserIdealProcessorFixed` `+0x20f`, 비트 5 `QuantumDonation` 지움 `+0x1d3`; Q78d4), `PsConvertToGuiThread` (`GuiThread`·`RestrictedGuiThread`), `KiFreezeSingleThread` (`FreezeCount`), `KeRequestTerminationThread` (`TerminationApcRequest`), `KiOutSwapKernelStacks` (`ProcessStackCountDecremented`), `KeSetThreadSchedulerAssist` (`VpBackingThread`), ETW 스택 APC (`Etwp…StackWalkApc` — 상위 비트) |

#### 2.2 조건부 안 탐 (12) — test.exe 가 왜 안 타나

| 칸 | 켜는 줄 | 조건 | test.exe 가 왜 안 탐 |
|---|---|---|---|
| `MiscFlags.SecureThread` (비트 8) | `①K +0x4d8` (`BTS EDX,8`) | 시스템 스레드 갈래 (`ThreadContext == 0`, `+0x112` → `+0x1b9`) 이면서 `param_2` 플래그 비트 4 (`+0x1bf`). 플래그 0x10 은 `PspAllocateThread +0xba7` 가 `SecureProcess` 프로세스의 시스템 스레드에만 줌 | 사용자 스레드 (`ThreadContext` 있음: `NtCreateUserProcess +0xe3a` 인자). 사진 비트 8 = 0 |
| `MiscFlags.SystemThread` (비트 10) | `①K +0x1c5` (`BTS EDX,0xa`) | 같은 시스템 스레드 갈래 (플래그 0x10 없음) | 위와 같음. 사진 비트 10 = 0 |
| `MiscFlags.CetUserShadowStack` (비트 20) | `①A +0xb3f`; 복제 갈래 `+0x2caaf6` | 프로세스 `MitigationFlags2.CetUserShadowStacks` 비트 14 (`[R15+0x754] & 0x4000`, `+0x5b9`) | sast44: K1 `MitigationFlags2` = `0x40000000` (비트 14 없음). 사진 비트 20 = 0 |
| `MiscFlags.BypassProcessFreeze` (비트 21) | `①A +0xb10` | 스레드 만들기 플래그 `0x200` (`+0x3f3`) | 플래그 안 줌. 사진 비트 21 = 0 |
| `MiscFlags.CetKernelShadowStack` (비트 22) | `①K +0x391 ~ +0x397` | 전역 `KiKernelCetEnabled` (nt+`0x1201841`) ≠ 0 (`+0x2b0`) | 사진 비트 22 = 0 → 꺼짐 `[관찰 간접]` (전역 값 자체는 D1) |
| `MiscFlags.StateSaveAreaDecoupled` (비트 23) | `KiAllocateXStateStack +0x52` (호출 `①K +0x4b9`) | `KeFeatureBits` 비트 0x37 (`+0x2d3`) 이 켜져 있고 `param_2+0x40 ≠ 0` (`+0x4a6`) | 사진 비트 23 = 0 |
| `KernelShadowStack` · `KernelShadowStackInitial` · `KernelShadowStackBase` · `KernelShadowStackLimit` (4칸) | `①K +0x40c · +0x417 · +0x423 · +0x44f` (호출 `+0x3ec KiCreateKernelShadowStack`) | `KiKernelCetEnabled` ≠ 0 이고 `param_2` 플래그 비트 0 이 꺼짐 (`+0x39f`) | 위 `CetKernelShadowStack` 과 같음. 사진 4칸 모두 0 |
| `ExtendedFeatureDisableMask` | `①K +0x486` (조건 `KeFeatureBits` 비트 0x37, `+0x2d3`) | 값 = `~(param_2+0x40) & KUSER_SHARED_DATA+0x3d8 & 0x40000` (AMX 기능 비트만) | **`[공백]`**: 갈래를 안 탔거나, 탔는데 공유 데이터에 AMX 비트가 없어 값이 0 — 사진은 둘 다 0. 전역 두 개 (`KeFeatureBits`, `KUSER_SHARED_DATA+0x3d8`) 를 읽으면 가려진다 (D1). 프로세스 쪽 같은 이름 칸은 `KPROCESS +0x198` |
| `IptSaveArea` | `KiInitializeContextThread +0x61` | 전역 `KiIptSaveAreaLength` (nt+`0xfc6c70`) ≠ 0 (`+0x46`) | 사진 0 → 길이 0 `[관찰 간접]` (Intel PT 저장 영역을 안 씀) |

#### 2.3 아무도 안 씀 — 만들기 길 안 (21)

`SListFaultAddress`, `RelativeTimerBias`, `Spare13`, `Spare18`, `LastXStateSaveDebugInfo`, `ThreadFlags2.AbContextSwitchState`, `ThreadFlags2.ThreadFlags2Reserved`, `QueueListEntry`, `Spare24`, `SListFaultCount`, `LegoData`, `CallbackNestingLevel`, `Spare6`, `InGlobalForegroundList`, `Spare26`, `Spare27`, `TracingPrivate`, `ReservedPreviousReadyTimeValue`, `InGlobalUpdateVpThreadPriorityList`, `SecureCallCoreIsolationCount`, `Padding`.

- 근거: `PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread` 의 `.asm` 에서 스레드 포인터 (`RSI`·`RDI`·`R12`·`RBX`) 로 쓰는 줄 전수 (`asmw.py` scratchpad, 칸 오프셋은 `.asm` 의 `+0x` 열에서 옮김) 에 이 칸들이 없고, 도우미 callee (`KeInitializeApc`·`Timer`·`Event`·`KeAbInitializeThreadState`) 의 쓰기도 이 칸을 안 건드린다 (`_KAPC` 의 쓰는 칸은 `SystemArgument1/2` 앞뒤 — `WaitPrcb`·`LegoData` 자리는 비어 있음). 전체 훑기에서도 이름 있는 스레드 쪽 쓰기가 안 보임 (또는 오탐뿐: `RelativeTimerBias +0xf8` 은 `ExpSetTimerObject` 의 타이머 객체 칸).
- **훑기의 한계 (알려 둠)**: `LEA` 로 칸 주소를 만든 뒤 그 포인터로 쓰는 줄 (리스트 삽입 등) 은 훑기에 안 잡힌다. `QueueListEntry` (`KeRemoveQueueEx` 가 매달 가능성) 가 그런 후보다 `[추측]`.

#### 2.4 범위 밖 길에서 씀 `[추측]` (70)

이름 있는 쓰는 함수가 만들기 길 밖에 있는 칸 (**전체 훑기, 오프셋 숫자 일치라 틀릴 수 있음**; 전부 `.asm` 없는 objdump 근거):

| 칸 | 쓰는 곳 (범위 밖) |
|---|---|
| `ThreadLock` (KTHREAD `+0x40`) | `KiBeginThreadWait`, `KeDelayExecutionThread`, `KeWaitForMultipleObjects`, `KeWaitForMutexObject`, `KeRemoveQueueEx`, `KeRemovePriQueue` (잡았다 놓음 → 끝까지 0) |
| `SchedulingGroup` | `KiRemoveThreadFromSchedulingGroup`, `KiTransitionSchedulingGroupGeneration`, `KeStartThread +0x32b` (Q78d4) |
| `Alerted` | `KiBeginThreadWait`, `KeDelayExecutionThread`, `KeWaitForMultipleObjects`, `KeWaitForMutexObject`, `KeRemoveQueueEx` |
| `Tag` | `KeUpdateThreadTag`, `KiIpiUpdateThreadTag` |
| `SameThreadTransientFlags` 5칸 (`CalloutActive`·`ReservedStackInUse`·`UserStackWalkActive`·`…Reserved`·바이트 전체) | `KiExpandKernelStackAndCalloutOnStackSegment`, `RtlpWalkFrameChain` |
| `SpecCtrl` 3칸 | `SwapContext` |
| `Queue` | `KeRemoveQueueEx`, `KeRemovePriQueue` |
| `KernelApcDisable` · `CombinedApcDisable` | 크리티컬 리전 들어가기·나오기 (인라인): `KeEnterCriticalRegionThread`, `KiLeaveCriticalRegionUnsafe`, `KeWaitForMutexObject`, `KiWaitSatisfyMutant`, `KiTryUnwaitThread`, `KiSchedulerApc` — 썼다 되돌림이라 끝까지 0 `[추측]` |
| `ThreadCounters` | `KeEnableProfiling`, `KeDisableProfiling` |
| `XStateSave` | `KeRestoreExtendedAndSupervisorState` |
| `Win32Thread` | `PsSetThreadWin32Thread` |
| `ThreadFlags2` · `ThreadFlags2.DisableKasan` | `KasaniReport`, `KcsanValidateAddress` (KASAN/KCSAN 점검용 코드) |
| `BamQosLevel` | `KeApplyWobBamQos`, `KeSetThreadSchedulerAssist`, `KiUpdateThreadPriority` |
| `HardwareFeedbackClass` | `KeStartThread +0x380` (Q78d4), `KiUpdateThreadHgsFeedback` |
| `PriorityDecrement` 3칸 | `KeSetPriorityThread`, `KiComputeNewPriority`, `KiQuantumEnd`, `KiClearForegroundBoost`, `KiRemoveBoostThread` |
| `NextProcessor.SharedReadyQueue` | `KiDeferredReadySingleThread`, `KiAddThreadToPrcbQueue`, `KiFindStandbyThreadForSteal` |
| `QueuePriority` | `KeRemovePriQueue`, `KiInsertQueue`, `KiInsertQueueInternal`, `KeInsertQueueApc` |
| `UserAffinityPrimaryGroup` · `AffinityPrimaryGroup` | `KeStartThread +0x156 · +0x14f` (Q78d4), `KiSetAffinityThread`, `KiSetIdealProcessorThread`, `KiSetSystemAffinityThread`, `KiComputeThreadAffinity` |
| `Preempted` · `AdjustReason` | `KiDeferredReadySingleThread`, `KiDirectSwitchThread`, `KiCommitThreadWait`, `KiProcessThreadWaitList` |
| `ApcStateIndex` | `KiAttachProcess`, `KiStackAttachProcess +0x162`, `KiDetachProcess`, `KiUnstackDetachProcess`, `KeForceDetachProcess` (붙이기 — 부모 스레드 쪽) |
| `Saturation` | `KeSetBasePriorityThread`, `KeSetActualBasePriorityThread`, `KeRemovePriQueue`, `KiWakePriQueueWaiter` |
| `KernelTime` · `UserTime` | `KeAccumulateTicks`, `KeUpdateTotalCyclesCurrentThread`, `KiCaptureTotalCyclesCurrentThread`, `KiEndThreadCycleAccumulation` |
| `WaitPrcb` | `KiInsertPrcbWaitEntry`, `KiRemovePrcbWaitEntry`, `KiSignalThread` |
| `AbWaitEntryCount` · `AbOwnedEntryCount` · `AbEntryCountValue` | `KiAbConvertWaiterToOwnerEntry`, `KiAbEntryGetLockedHeadEntry` |
| `FreezeFlags` 4칸 | `KiFreezeSingleThread`, `KiThawSingleThread`, `KeForceResumeThread` |
| `SecureThreadCookie` | `KeUnsecureThread` |
| `PriorityFloorCounts` · `PriorityFloorSummary` · `AbCompletedIoBoostCount` · `AbCompletedIoQoSBoostCount` · `KeReferenceCount` | `KiAbSetMinimumThreadPriority`, `KiAbCpuBoostOwners`, `KiAbIoBoostOwners`, `KiAbThreadRemoveBoostsSlow`, `KeAdjustPriorityFloor`, `KeAdjustWobPriority` |
| `ForegroundLossTime` | `KiClearForegroundBoost` |
| `OtherTransferCount` | `IopUpdateIrpTransferCount`, `NtSetInformationFile`, `KeFoldProcessStatisticsThread` (run50 프로세스 I/O 카운터와 같은 줄기) |
| `QueuedScb` | `KiAddThreadToScbQueue`, `KiRemoveThreadFromScbQueue`, `KiMoveScbThreadsToNewReadylist` |
| `ThreadTimerDelay` | `KeStartThread +0x36c` (Q78d4), `KiAdjustThreadTimer` |
| `PpmPolicy` | `KeSetThreadPpmPolicy` |
| `SchedulerAssist` · `SchedulerAssistPriorityFloor` · `BamQosLevelFromAssistPage` | `KeSetThreadSchedulerAssist`, `KiUpdateVpBackingThreadPriorityFromTopLevel` |
| `AbWaitObject` | `KiCommitThreadWait`, `KeWaitForMutexObject`, `KiWaitForAllObjects`, `KeInsertQueueEx` |
| `HgsFeedbackStartTime` · `HgsFeedbackCycles` · `HgsInvalidFeedbackCount` · `HgsLowerPerfClassFeedbackCount` · `HgsHigherPerfClassFeedbackCount` | `KiBeginThreadAccountingPeriod`, `KiUpdateThreadHgsFeedback`, `KiStartThreadCycleAccumulationContextSwap` |
| `SystemAffinityTokenListHead` | `KeSetSystemMultipleGroupAffinityThread`, `KeRevertToUserMultipleGroupAffinityThread` |
| `CoreIsolationReasons` | `KiAdjustCoreIsolationReasonThread` |
| `SchedulerSharedOffset` | `PspSchedulerSharedDataRegionSlotFree` |
| `KcsanThread` | `KcsanValidateAddress` |
| `SchedulerAssistYieldCounter` · `SchedulerAssistYieldBoostCount` · `SchedulerAssistLastYieldBoostTime` | `KiVpBackingThreadYieldExecution`, `KiCommitThreadWait` |

- 이 표의 줄마다 칸 수: 1+1+1+1+5+3+1+2+1+1+1+2+1+1+3+1+1+2+2+1+1+2+1+3+4+1+5+1+1+1+1+1+3+1+5+1+1+1+1+3 = **70**. 칸 이름을 하나씩 짝지어 (`ktclass.py` scratchpad) 빠진 칸 0, 겹친 칸 0 을 확인했다.
- **`KeStartThread` (Q78d4 몫) 가 이 138칸 중 쓰는 칸**: `SchedulingGroup`, `HardwareFeedbackClass`, `UserAffinityPrimaryGroup`, `AffinityPrimaryGroup`, `ThreadTimerDelay` 와 `ThreadFlags` DWORD (비트 0·5). 사진 K2~P2 에서 모두 0 이니 0 을 쓰거나 조건이 안 맞은 것 (Q78d4 가 `KeStartThread` 줄을 읽을 때 볼 것).

### 3. 갈림길과 입력 — 간접 호출

- **만들기 길 안에 `_KTHREAD` 칸을 쓰는 간접 호출은 없다.** `.asm` 14개 (`PspAllocateThread`, `KeInitThread`, `KiInitializeContextThread`, `KeAbInitializeThreadState`, `KeInitializeApc`, `KeInitializeTimer`, `KeInitializeEvent`, `MmCreateKernelStack`, `KiAllocateXStateStack`, `KiCreateKernelShadowStack`, `KeCopyXfdMaskToTeb`, `KxContextToKframes`, `PspSetupUserStack`, `MmCreateTeb`) 에서 `CALL reg`·`CALL [mem]`·`JMP reg` 를 찾았는데 0개다. → Q35 에 넣을 것 없음.
- `KeInitializeApc` 에 함수 주소를 *인자로* 넘기는 줄은 있다 (`KeInitThread +0x12d` · `+0x139` · `+0x145`: `KiSchedulerApc` rva `0x2c8950` 과 stub rva `0x4a10c0` · 두 번째 `KeInitializeApc` `+0x34a` 는 `KiUpdateTebApc` rva `0x312820`). 이것은 값으로 저장될 뿐 이 스레드 만들기 길에서 부르지는 않는다.
- **입력 (갈림길 값)**: `ThreadContext` — `NtCreateUserProcess +0xe3a` 가 `R13` 을 넘기고, `R13` 은 `PspCreateUserContext` (`+0xd1d`, 새 `_CONTEXT`) 또는 복제 길 `PspGetContextThreadInternal` (`+0xd39`) 가 채운 버퍼라 0 이 아니다. 사용자 스택 인자 (`PspAllocateThread` 의 열한째 인자) 는 `PspSetupUserStack` (`+0x5ea`) 를 타는 쪽 — 이 값이 0 인 복제·피코 갈래 (`+0x5b3 → +0x2ca9a9`) 는 TEB 를 현재 스레드 것에서 베낀다. test.exe 는 사진 `Teb` = `MmCreateTeb` 가 만든 TEB (PEB 바로 뒤 `+0x1000`) 라 안 탄다 `[관찰 간접]`.

### 4. 줄기와 경로

- **줄기** (칸 값을 정하는 함수까지 끝까지): `PspAllocateThread` 본문, `KeInitThread` 본문, `KiInitializeContextThread`, `KeAbInitializeThreadState`, `KeInitializeApc`, `KeInitializeTimer`, (`KeInitializeEvent` — 3.14 아는 칸), `MmCreateKernelStack` (결과를 받는 줄 `KeInitThread +0x282` 까지만).
- **줄기 밖 (이름만)**: `memset`, 풀 할당 (`ObCreateObjectEx`), `PoEnergyEstimationEnabled`, `KeQueryMaximumGroupCount`, `KeQuerySystemTimePrecise`, `KeAbPreAcquire`·`ExfAcquirePushLockExclusiveEx`·`KeAbPostRelease` (ETHREAD 푸시락), `PsAssignThreadId`, `PspSetupUserStack`·`MmCreateTeb`·`KiStackAttachProcess`·`KiUnstackDetachProcess` (부모 스레드·TEB·스택), `KxContextToKframes` (트랩 프레임 — Q78d2), `PsInitializeThreadRngState`, `ObInitializeFastReference`, `KiGetIdealNodeProcessByGroup`, `KeSelectInitialIdealProcessorForThread`, `KiAllocateXStateStack`·`KiCreateKernelShadowStack`·`KeCopyXfdMaskToTeb` (안 타는 갈래), `MmCreateKernelStack` 안쪽 (스택 쪽 할당·`MiReservePtes` …), 스택 안 XSAVE 영역 내용 (`StateSaveArea`·`NpxState` 가 가리키는 곳 — 칸 표 없는 짐).
- 칸마다 경로·근거는 위 1.1 표 (3장 양식). 0 칸 묶음은 2.1~2.4.

## 동적분석으로 확인해야 할 것

"사진" = 이미 있는 덤프 (`ms/logs/04_baggage-read_*.txt`, `ms/dump/03_baggage-snap/`) 로 답할 수 있는지. 번호는 이 조각 안 번호 (D1~D4).

| # | 질문 | 추천 멈춤점 / 찍을 것 | 사진으로? |
|---|---|---|---|
| D1 | 커널 전역 값: `KiKernelCetEnabled` (nt+`0x1201841`), `KeFeatureBits` (nt+`0xfc6b70`, **비트 0x37**), `KiIptSaveAreaLength` (nt+`0xfc6c70`), `KiSchedulerAssistThreadFlagEnabled` (nt+`0xfc6e00`), `KiShortExecutionCycles` (nt+`0xfc6df4`, 기대 `0x4c4b4`), `KeKernelStackSize` (nt+`0xfc5020`, 기대 `0x7000`), `KiXSaveAreaLength` (nt+`0xfc5114`, 기대 `0x980` 초과 `0x9c0` 이하 — 사진 `StackBase − StateSaveArea` = `0x9c0` 에서 거꾸로), `KeEnabledSupervisorXStateFeatures` (nt+`0xfc6ca0`), `KUSER_SHARED_DATA+0x3d8` (`0xfffff780000003d8`, 기대: 하위 바이트의 비트 2·5·6·7 켜짐, 비트 18 (`0x40000`) 꺼짐 — 사진 `NpxState` `0xe5` 에서 거꾸로) | `dq`/`dd`/`db` 한 번씩 | **가능** (전역은 덤프에 있음) → Q79. 이것으로 조건부 12칸의 "왜 안 탔나" 와 `ExtendedFeatureDisableMask` 의 `[공백]` 이 닫힌다 |
| D2 | `PspAllocateThread` 가 `KeInitThread` 를 부른 **직후** 스레드 한 장 (만들기 전 옛 객체 잔재 없이 `KeInitThread` 가 쓴 칸만 보려고) | `nt!PspAllocateThread+0x6f9` (rva `0x843065`) 에서 `dt nt!_KTHREAD @rsi` — 조건: 현재 프로세스 = cmd.exe | 불가 (사진 K1 은 옛 객체). **낮은 우선순위** — K2 값이 이미 코드와 칸마다 맞아서 꼭 필요하진 않다 |
| D3 | `KernelApcDisable`·`CombinedApcDisable` 이 새 스레드 만들기 길에서 정말 안 쓰이나 (인라인 크리티컬 리전 줄은 훑기에 안 잡힐 수 있음) | 새 스레드 주소가 정해진 뒤 `ba w2 <스레드>+0x1e4` | 불가 (썼다 되돌린 것도 0). **낮은 우선순위** |
| D4 | 사진 K1 자리의 옛 객체 (이 주소를 쓰던 앞 스레드) 가 왜 남아 있나 | — | 필요 없음 (판정에 안 씀) |

- 급한 것은 없다. D1 은 사진 (Q79 에 전역 읽기 추가) 으로 닫히고, 나머지는 닫지 않아도 칸 표는 완성된다.
- 디버거가 붙어서 달라지는 점: 없음 (모든 근거가 정적 + 이미 찍은 사진).

## Q78d2·Q78d3·Q78d4 로 넘길 것 (지나가며 본 줄)

**Q78d2 (`_ETHREAD` 나머지·`_KTRAP_FRAME`)**
- `_ETHREAD` 칸을 `PspAllocateThread` 가 쓰는 줄 (오프셋은 `.asm` 의 `+0x` 열, 칸 이름은 `pdb_struct.py _ETHREAD`): `CreateTime` (+0x4c0) ← `+0x4bf KeQuerySystemTimePrecise` (`KeQuerySystemTimeUnsafe` 가 0 을 돌려주면 공유 데이터 시각을 베낌 `+0x758 ~ +0x767`), 자기 연결 리스트 머리 — `KeyedWaitChain` (+0x4c8) `+0x439 · +0x43d`, `BoostList` (+0x5f0) `+0x447 · +0x44b`, `DeboostList` (+0x600) `+0x455 · +0x459`, `PropertySet` (+0x658, 머리 + 락) `+0x46a · +0x46e · +0x471`, `IrpList` (+0x540) `+0x47c · +0x480`, `ActiveTimerListHead` (+0x4f8) `+0x498 · +0x49c`, `OwnerEntryListHead` (+0x6b8) `+0x4cb · +0x4cf`, `DisownedOwnerEntryListHead` (+0x6d0) `+0x4e0 · +0x4e4` — 락 칸 0: `BoostListLock` (+0x610) `+0x45c`, `IrpListLock` (+0x618) `+0x483`, `ActiveTimerListLock` (+0x4f0) `+0x48a`, `DisownedOwnerEntryListLock` (+0x6c8) `+0x4d2`, `Win32kThreadLock` (+0x778) `+0x4e7`, `Silo` (+0x698) = −3 `+0x49f`, `MmSlabIdentity` (+0x727) = 0xff `+0x41a`, `ReadClusterSize` (+0x598) = 0xf `+0x410`; `Cid.UniqueProcess` (+0x508) ← 프로세스 `+0x1d0` (읽기 `+0x3d3`, 쓰기 `+0x3da`), `Cid.UniqueThread` ← `PsAssignThreadId` (호출 `+0x52d`), `KeyedWaitSemaphore` (+0x518) ← `KeInitializeSemaphore` (호출 `+0x42d`), `StartAddress` (+0x4e0) ← `_CONTEXT.Rip` (`+0x640 ~ +0x647`) · `Win32StartAddress` (+0x560) ← `_CONTEXT.Rcx` (`+0x64e ~ +0x655`), `ThreadLock` (+0x590, 푸시락: 잡기 `+0x505`, 호출 `+0x517`), `EnergyValues` (+0x688) 포인터 `+0x31a`, `CrossThreadFlags` 비트 2 `|= 4` (`+0xb04`, 만들기 플래그 비트 2 일 때) · 비트 17 `IndirectCpuSets` (`+0xaea`, 그룹 수 > 1) + `SelectedCpuSetsIndirect` (+0x690) 포인터 `+0xaf8`, `SameThreadPassiveFlags` 비트 9 `SecureContext` (`+0xbb2`, 보안 시스템 스레드), `ProcessFastRef` (+0x790) ← `ObInitializeFastReference` (호출 `+0x71a`), `UpdateTebApc` (+0x720) ← `KeInitThread +0x368` (`KiUpdateTebApc` rva `0x312820`). 이 줄들의 사진 구간·값 대조는 안 했다.
- `_KTRAP_FRAME` `K1~K2` 3칸: `ExceptionActive` = 1 ← **`KiInitializeContextThread +0x198`** (`MOV byte [RAX+0x2b],1`, `RAX` = `R15 − 0x190`), `MxCsr` = `0x1f80` ← **`+0x1f1`** (`MOV [R11+0x2c],0x1f80`), `EFlags*` ← `KxContextToKframes` (호출 `+0x1ac`, 안 읽음). 커널 스택 안 시작 프레임 (`+0x1bc ~ +0x1e8`: `[RBX]`·`[RBX+8]`·`[RBX+0x10]` ← `param_2` 의 시작 주소·시작 루틴 …, `[RBX+0x138] = 0x1406afb60`, `[R13+0x38]` = `KiStartUserThread` rva `0x6afbd0`) 은 칸 표 없는 짐.

**Q78d3 (`_TEB`)**
- `PspAllocateThread +0x793` (`MOV [TEB+0x17ee], R12W`) — `SameTebFlags`. 값은 만들기 플래그에서 만든 16비트 (`+0x54a ~ +0x5a5`: 플래그 0x40 → `InitialThread` 자리 (비트 10), 0x2 → `SkipThreadAttach` (비트 3), 0x80 → `LoaderWorker` (비트 13), 0x100 → `SkipLoaderInit` (비트 14)). TEB 를 만드는 줄은 `MmCreateTeb` (호출 `PspAllocateThread +0x62c`), 사용자 스택은 `PspSetupUserStack` (호출 `+0x5ea`). 복제 갈래는 TEB `ClientId`·`IsImpersonating`·`MuiImpersonation`·`SchedulerSharedDataSlot` 도 쓴다 (`+0x2cab6d ~ +0x2cabbd`, 안 탐).

**Q78d4 (`K2` 이후 구간 — `KeStartThread`·`PspInsertThread`·스케줄러)**
- **`KeStartThread` (`.asm` 있음) 가 스레드 칸을 쓰는 줄** (`RBX` = 스레드): `Header` 비트 18 `GroupScheduling` (`+0x339`), 비트 23 (`+0x353` → 사진 K3 `0xa00006`), `ThreadFlags` DWORD (`+0xb9 · +0x1b7`; 비트 5 지움 `+0x1d3`, 비트 0 켬 `+0x20f`), `Priority` (+0xc3) ← `+0xc8`, `UserIdealProcessor` (+0xc4) ← `+0x17c`, `IdealProcessor` (+0x24c) ← `+0x187`, `BasePriority` (+0x233) ← `+0xc2`, `QuantumTarget` (+0x20) ← `+0x1dc`, `QuantumReset` (+0x28b) ← `+0x1c1`, `WobPriority` (+0x31b) = 0x20 ← `+0xce`, `SchedulingGroup` ← `+0x32b`, `HardwareFeedbackClass` ← `+0x380`, `UserAffinityPrimaryGroup` ← `+0x156`, `AffinityPrimaryGroup` ← `+0x14f`, `ThreadTimerDelay` ← `+0x36c`. → 7.2 `K2~K3` 10칸 중 8칸 (`QuantumTarget`·`ThreadFlags.SharedReadyQueueAffinity`·`Priority`·`UserIdealProcessor`·`BasePriority`·`IdealProcessor`·`QuantumReset`·`WobPriority`) 이 여기서 닫히고, 나머지 둘 (`SavedApcState`·`ThreadListEntry`) 은 다른 함수 몫이다. 이 결과서 2.4 의 5칸 (`SchedulingGroup`·`HardwareFeedbackClass`·`UserAffinityPrimaryGroup`·`AffinityPrimaryGroup`·`ThreadTimerDelay`) 도 같은 함수에서 확인된다.
- `PspInsertThread +0x77b` (`Flags2.PrimaryTokenFrozen`) 는 sast45 에서 이미 넘긴 것 — 여기서 다시 안 봄.
- `ModeHistory` 가 이후 시계 틱마다 밀리는 줄 (`KeAccumulateTicks +0x58`), `ExpectedRunTime` 갱신 (`KiSwapThread` 쪽) — 이름만 `[추측]`.

## 정정 · 새로 알게 된 것 (flow-writer 용)

1. **K1 사진의 스레드 칸은 옛 객체 값이다** (이 주소를 쓰던 앞 스레드, `Process` 가 다름). 7.2 의 `끝까지 0` 판정은 K2~P2 기준이고, K1 에서 0 이 아니던 5칸 (`MiscFlags.CetUserShadowStack`, `SpecCtrl.RunningNonRetpolineCode`, `SpecCtrl`, `BamQosLevel`, `QueueListEntry`) 도 K2 부터 0. baggage 2.1 표의 `K1~K2` 는 "처음 값이 K1 과 K2 사이에 생긴 칸" 인데, 스레드 구조체는 K1 때 객체가 아직 없어서 K1 열이 의미가 없다 — 7.2 `_KTHREAD` 의 `K1~K2` 42칸은 사실상 "K2 에 처음 값이 보이는 칸" 이라고 적어 두면 좋다.
2. **`InitialStack` 은 두 번 쓴다**: `KeInitThread +0x29b` (스택 맨 위, `StackBase` 와 같은 값) → `KiInitializeContextThread +0x2ca` (XSAVE 영역 아래로 내린 값) 가 덮는다. 3.14 `InitialStack` 줄은 둘째 쓰기라 맞다 — 첫 쓰기 줄을 덧붙일 것.
3. **3.14 `ApcQueueable` 줄**: 켜는 조건이 구체화됨 — `param_2+0x3c` 비트 1 이 **꺼져 있을 때** 켠다 (`KeInitThread +0xf5 ~ +0xfd`). test.exe 는 플래그 0 이라 켜짐.
4. **3.14 `SchedulerApc` 줄**: `KernelRoutine`·`RundownRoutine` 은 rva `0x4a10c0` (같은 자리에 PDB 이름 100개 — 아무 일도 안 하는 stub, `KiSchedulerApcNop`·`KiSchedulerApcRundown`), `NormalRoutine` = `KiSchedulerApc`. 3.14 의 `KeInitializeApc (호출 +0x152)` 줄과 맞음.
5. **KTHREAD `ThreadLock` (`+0x40`) ≠ ETHREAD `ThreadLock` (`+0x590`)** — 3.13 의 `ThreadLock` 두 줄 (`PspAllocateThread +0x51c`, `PspInsertThread +0x499`) 은 ETHREAD 것. KTHREAD 쪽은 만들기 길에서 안 쓰고 스케줄러·대기 코드가 잡았다 놓는다 (2.4).
6. **`KeInitThread` 가 부르는 타이머 초기화는 `KeInitializeTimer`** (요청서·문서에 `KeInitializeTimerEx` 로 적힌 곳이 있으면 고칠 것).
7. **부모(현재) 스레드 칸 주의**: `PspAllocateThread` 안에서 `KiStackAttachProcess`·`KiUnstackDetachProcess` 가 `ApcState`·`ApcStateIndex`·`MiscFlags.ProcessDetachActive` 를 쓰는 줄은 새 스레드가 아니라 부모 cmd 스레드의 칸이다 — 3.14 에 줄을 넣지 말 것.
8. **새 칸 표 없는 짐 후보** (7.6 에 올릴지는 사용자와): 몸통 뒤 확장 영역의 `_KAFFINITY_EX` 2개 (각 `8 + 그룹 수×8` B, `PDB` 있음 0x108), `_KLOCK_ENTRIES` 2개 (커널용 6칸 0x220B / 사용자용 8칸 0x2d0B, 블록 `PDB` 있음 0x68 + 칸 `×0x58`), 에너지 칸 (`+0x798 ~ +0x860`, 0xc8B), 커널 스택 7쪽 (`StackLimit ~ StackBase`) 안의 XSAVE 영역 (0x9c0B) · 트랩 프레임 (0x190B) · 시작 프레임, 대기 블록 4개 (`_KWAIT_BLOCK` 0x30B 씩 — 이미 `WaitBlock` 칸으로 셈).
9. **숫자 (baggage 7.2 `_KTHREAD` 모름 227)**: 이 조각으로 `K1~K2` 42 와 `끝까지 0` 138 이 모두 정해져서 모름 칸은 227 − 180 = **47** 이 남는다 — `K2~K3` 10 · `K3~P0` 9 · `P0~P1` 9 · `P1~P1a` 7 · `P1a~P1b` 6 · `P1b~P2` 6 (Q78d4 몫). 아는 칸은 3.14 의 10 에 `42 + 35 + 12 = 89` (줄이 있는 칸) 가 더해져 99, "아무도 안 씀" 21, "범위 밖 길" 70 (flow-writer 가 어디에 셀지 정함).
10. **훑기 한계** (판단 기준 끝의 오탐 말고): `LEA` 로 칸 주소를 만들어 쓰는 줄 (리스트 삽입 등) 과 `SHL [mem]`·`SHR [mem]`·`ROL [mem]` 같은 이동 명령은 훑기에 안 잡힌다. `ModeHistory` 의 틱마다 밀기는 `KeAccumulateTicks` 의 `MOV` 가 잡혔지만, 다른 이동 줄은 있어도 못 봤을 수 있다.

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **고칠 버그는 없다.** `run_ghidra.sh export` 한 번 (3.6초), `pdb_struct.py` (KTHREAD·ETHREAD·KPROCESS·EPROCESS·KAPC·KTIMER·KWAIT_BLOCK·KAPC_STATE·DISPATCHER_HEADER·KAFFINITY_EX·KLOCK_ENTRIES·TEB·KTRAP_FRAME), `pdb_aliases.py --where` 모두 문제없이 돌았다.
- **제안 1 (sast/43·44 제안을 다시, 이번에 더 구체적으로)**: `scripts/field_writers.py <바이너리> --struct _KTHREAD [--cells 목록]`. sast44 때 뽑아 둔 `objdump -d` 결과 (`nt.dis`, 같은 바이너리) 를 다시 쓰고 `_index.tsv` 로 함수 이름을 붙이는 임시 스크립트 (`scratchpad`: `ntscan.py`, `ktscan.py`, `ktbits.py`, `asmw.py`) 를 짰다. 칸마다 "후보 줄 (함수+오프셋+명령) · 기준 레지스터가 `GS:[0x188]` 에서 왔나 · 이름 힌트" 를 TSV 로 뽑으면 오탐을 많이 줄일 수 있다. **이번에 필요했던 추가 기능 둘**: (a) **DWORD 안 비트별 분리** — `MiscFlags`·`ThreadFlags` 같은 비트 묶음은 `BTS`/`BTR`/`OR imm`/`AND imm` 의 즉시값으로 어느 비트를 건드리는지 풀어야 "비트마다 쓰는 곳" 이 나온다 (`ktbits.py`). (b) **칸이 중첩 구조체의 시작일 때** (`SchedulerApc` = `_KAPC`, `Timer` = `_KTIMER`, `SuspendEvent` = `_KEVENT`, `ApcState` = `_KAPC_STATE`, `WaitBlock` = `_KWAIT_BLOCK[4]`) 는 "그 칸 주소를 callee 에 넘기고 callee 가 구조체 오프셋으로 쓰는" 경우라 훑기가 놓친다. `pdb_struct.py` 로 중첩 구조체를 펼치고 호출 지점의 `LEA RCX,[RDI+칸]` 인자를 짝지어 주는 기능이 있으면 좋다.
- 제안 2 (사소): `pdb_aliases.py --lookup <바이너리> <rva>` 가 "별칭 무리 아님 (이름이 하나뿐이거나 표에 없음)" 만 말하고 **이름 하나뿐인 주소의 그 이름을 안 알려준다.** 이번에 `0x6afb60`·`0x2c8950` 의 이름을 `_index.tsv` 로 따로 찾았다. `--lookup` 이 `_index.tsv` (또는 PDB) 도 찾아 주면 한 번에 끝난다. (함수 시작이 아닌 주소 `0x6afb60` 은 `_index.tsv` 에도 없었다.)
- 겹침: `pdb_struct.py` 의 앞 바퀴 제안 (`--expand`) 은 이번에도 `MiscFlags`·`ThreadFlags`·`Header` 비트에 필요했다 — sast/38 `struct__KTHREAD.tsv` 와 `_DISPATCHER_HEADER` 를 따로 뽑아 대신 봤다.

## 이름 확인 결과

결과서의 backtick 이름 중 함수처럼 생긴 183개를 `ms/decomp/ntoskrnl/_index.tsv` 와 맞춰 봤다 (scratchpad 스크립트). `_index.tsv` 에 없는 것 43개는 전부 **구조체 칸 이름**(`KernelAbEntries`·`Minimal` …)이거나 **전역 변수**(아래)이거나 **별칭 이름**이고, 함수 이름이 빠진 것은 0개다. 별칭 둘: `KiSchedulerApcNop`·`KiSchedulerApcRundown` 은 rva `0x4a10c0` 의 PDB 이름 100개 무리 안 이름 (`ntoskrnl.exe.aliases.tsv`), `KiStartUserThread` (rva `0x6afbd0`) 는 PDB 에 있으나 Ghidra 가 함수로 안 잡음 (`pdb_aliases.py --where`). 전역 이름 `KiKernelCetEnabled`·`KeFeatureBits`·`KiIptSaveAreaLength`·`KiSchedulerAssistThreadFlagEnabled`·`KiShortExecutionCycles`·`KeKernelStackSize`·`KiXSaveAreaLength`·`KeEnabledSupervisorXStateFeatures`·`KiWaitAlways`·`KiWaitNever` 는 `pdb_aliases.py --where` 로 PDB 에 있음을 확인 (rva 가 `.asm` 의 주소와 같음).

## 판정

**분석성공.** 요청서 질문 1~4 모두 답했다. 180칸 → 아는 칸 (nt) 42 · 0 을 씀 35 · 조건부 안 탐 12 · 아무도 안 씀 21 · 범위 밖 길 70 · 여전히 모름 0. 버전은 사진 K2 값과 코드가 칸마다 일치. 정적으로 못 닫은 것: `ExtendedFeatureDisableMask` 의 "갈래를 안 탔나 vs 값 0" (D1 전역 두 개), 확장 영역 안 `_KAFFINITY_EX` 비트맵이 채워지는 때 (Q78d4), "범위 밖 길" 70칸·"아무도 안 씀" 21칸의 근거가 전체 훑기 (오탐 가능, `LEA` 쓰기는 못 잡음) 라는 점.

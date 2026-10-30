# 47 ethread-create — 정적 분석 결과 (Q78d2)

- 요청서: `_workspace/sast/47_ethread-create.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) — **기준과 같음 (PDB 일치).** 사진 로그 `ms/logs/04_baggage-read_K2.txt` 의 `!lmi nt` 와도 같다 (sast/46 에서 확인한 줄 그대로).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음, Ghidra 프로세스 없음).
- **새로 뽑은 함수 10개, 전부 depth 0** (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`). `_calls.tsv` 는 `comm -23` 0줄 (줄이 안 사라짐), 4,304 → 4,307줄. 요청서가 이미 있다고 한 `PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread`·`KxContextToKframes` 등은 **다시 안 뽑았다** (파일 시각 10-22 그대로).
  - 요청서 마지막 줄 ("`PspAllocateThread` callee 중 `_ETHREAD` 칸을 쓰는 것") 에 따라: `KeInitializeSemaphore` (세마포어 칸), `PsInitializeThreadRngState` → `RtlInitializeTRng` (`RngState`), `PsAssignThreadId` (`Cid`, 아는 칸이지만 다른 칸을 안 쓰는지 확인), `KeQuerySystemTimePrecise` · `KeQuerySystemTimeUnsafe` (`CreateTime`), `PoEnergyEstimationEnabled` · `KeQueryMaximumGroupCount` (갈림길 값).
  - **요청서 표에 없던 것 2개 — 조건부 갈래 확인용**: `PspMapThreadCreationFlags` (`NtCreateUserProcess +0xdce` 가 만들기 플래그를 바꾸는 곳 → `HideFromDebugger` 갈림길), `ExBuildPasidMsrForThread` (`KiInitializeContextThread +0x40c` 의 callee → `PasidMsrValid` 비트를 켜는 줄).
- 이미 있어서 **읽기만 한 것**: `PspAllocateThread`, `KeInitThread`, `KiInitializeContextThread`, `KxContextToKframes`, `KeInitializeApc`, `PspCreateUserContext`, `ObInitializeFastReference`, `NtCreateUserProcess` (인자 줄·`+0xcc7 ~ +0xe57` 만), `PspInsertThread` (`CrossThreadFlags` 등 쓰는 줄만, Q78d4 로 넘길 것), `MmCreateTeb`·`PspSetupUserStack`·`KiStackAttachProcess` (`_ETHREAD` 칸 쓰는 줄 없음 확인).
- **Ghidra 로 안 뽑고 `objdump -d` (scratchpad `nt.dis`) 로만 본 곳** (`.asm` 없음 — 함수+오프셋으로만 적는다): 커널 전체 `[reg+칸]` 쓰기 훑기 (`_ETHREAD` 칸 전부), 트랩 프레임 진입 코드 (`KiSystemServiceUser` +0x0 ~ +0x5d, `KiSaveDebugRegisterState` +0x2f ~ +0xe3, `KiInterruptSubDispatch +0x20`, `KiServiceInternal +0x5b`), "범위 밖 길" 표에 이름만 적은 함수 전부.
- **훑기를 전보다 넓혔다**: sast/46 훑기는 `_index.tsv` 의 함수 크기 안만 봤다 → Ghidra 가 "떨어진 범위"로 묶은 조각 (`PspAllocateThread +0x2ca9a9` 같은 먼 오프셋) 안 쓰기를 놓쳤다. 이번엔 먼 조각을 점프 따라가며 주인 함수에 붙였다 (scratchpad `orphan.py`, 조각 64,841 명령). 덕분에 `PspInsertThread +0x9d6` (`SelfTerminate`) 같은 줄이 잡혔다.
- **이름 확인**: 이 결과서에 적은 함수 이름 전부를 `ms/decomp/ntoskrnl/_index.tsv` 로 한 번 더 확인했다 (끝에 "이름 확인 결과"). 전역 변수 이름은 `pdb_aliases.py --where` 로 PDB rva 를 확인했다.
- **버전 확인: 일치.** 호출 지점 `_calls.tsv` — `NtCreateUserProcess` call `+0xe57` / ret **`+0xe5c`** → `PspAllocateThread` (그리고 `+0xc31 / +0xc36 → PspAllocateProcess` 도 그대로), `PspAllocateThread` call `+0x6f4` / ret `+0x6f9` → `KeInitThread`, `KeInitThread` call `+0x2fc` / ret `+0x301` → `KiInitializeContextThread`, `KiInitializeContextThread` call `+0x1ac` / ret `+0x1b1` → `KxContextToKframes`. **사진 K2 값과 코드가 칸마다 맞는다**: 자기 연결 리스트 머리 8개의 값이 모두 "스레드 주소 + 칸 오프셋" (예: `KeyedWaitChain` = `…5080 + 0x4c8 = …5548`), `EnergyValues` = 스레드 + 0x798, `ProcessFastRef` = 프로세스 + 0xf (`…b08f`), `ReadClusterSize` 0xf, `Silo` −3, `MmSlabIdentity` 0xff, `UpdateTebApc` 0 이 아닌 8바이트 덩어리 7개 (코드로 센 5 + `RngState` 겹침 2), 세마포어 4/4, 트랩 프레임 `ExceptionActive` 1 · `MxCsr` `0x1f80` · `EFlags` `0x200`. 바이너리와 타깃 VM 이 같다고 볼 수 있다.

## 스크립트 문제

없다. (`run_ghidra.sh export` 세 번, `pdb_struct.py`·`pdb_aliases.py --where` 여러 번 — 모두 문제없이 돌았다. 도구 제안은 맨 아래.)

## 판단 기준 (한 줄)

**만들기 길** = `NtCreateUserProcess → PspAllocateThread` 본문 + 새 `_ETHREAD` 포인터를 넘겨받는 callee (`KeInitializeSemaphore`, `PsInitializeThreadRngState → RtlInitializeTRng`, `ObInitializeFastReference`, `KeQuerySystemTimePrecise`, `KeInitThread → KeInitializeApc (UpdateTebApc)`, `PsAssignThreadId`) + 트랩 프레임은 `KeInitThread → KiInitializeContextThread → KxContextToKframes` (+ 그 callee `ExBuildPasidMsrForThread`). 이 함수들의 `.asm` 에서 칸 오프셋으로 쓰는 줄이 있으면 → 값 0 을 쓰거나 (**명시 0 저장**) 입력값을 옮겨 쓰면 **"0 을 씀"**, 켜는 줄이 있는데 test.exe 에서 조건이 안 맞으면 **"조건부 안 탐"**; 만들기 길 안에 줄이 없으면 커널 전체 `objdump` 훑기 (`[reg+칸오프셋]` 쓰기, 오프셋 숫자 일치라 **오탐 있음**) 로 이름 있는 스레드 쪽 함수가 보이면 **"범위 밖 길에서 씀 `[추측]`"**, 그것도 안 보이면 **"아무도 안 씀 (만들기 길 안)"**.

- **몸통 0 채움은 두 군데**:
  - `_ETHREAD`: `PspAllocateThread +0x307 memset(스레드, 0, [RSP+0x98])` (call `+0x307` / ret `+0x30c`). **크기 `[RSP+0x98]` 는 `ObCreateObjectEx` (call `+0x2e2` / ret `+0x2e7`; baggage 6장의 `+0x2e7` 은 ret 자리) 에 넘긴 객체 크기 그대로** (`+0x2ab` 가 계산): 사용자 스레드는 `0x880 (두 `_KAFFINITY_EX` 앞) + 0x220 (커널 `_KLOCK_ENTRIES`) + 0x2d0 (사용자 `_KLOCK_ENTRIES`) = 0xd70`, 시스템 스레드는 0xaa0 (사용자 블록 없음). 에너지 칸 0xc8 이 끼면 몸통 0x798 뒤가 0x860 부터 시작. → **`_ETHREAD` 몸통 0x798 전체 + 뒤쪽 확장 영역 전부를 민다.** 그래서 107칸 모두 0 에서 시작한다. 사진으로도 맞다: K1 의 이 주소는 옛 객체라 `RundownProtect` 1 · `CrossThreadFlags.Terminated` 1 같은 쓰레기 값이 있었는데 K2 부터 0.
  - 트랩 프레임: `KiInitializeContextThread +0x187 memset(R15−0x190, 0, 0x190)` (call `+0x187` / ret `+0x18c`). 이 스택은 새로 만든 커널 스택 (`MmCreateKernelStack`) 이라 앞서 0 이었는지는 정적으로 안 가른다 (K1 사진의 같은 주소는 이 주소를 쓰던 옛 내용 — `ExceptionActive` 0xb3, `PreviousIrql` 0xb, `Dr3` 0x200001 같은 쓰레기 값). **트랩 프레임 칸의 0 을 확실히 보장하는 줄은 이 memset 하나다.**
- **셈 방식 (요청서 "주의" 한 줄에 대한 답)**: **run50·run52 와 같게 셌다.** 두 번 모두 "몸통 memset" 은 칸마다 쓰는 줄로 안 치고 ("겹치는 안전장치" / "'아무도 안 씀' 의 0 은 이 memset 에서 온다"), **명시 0 저장·입력값 쓰기·같은 DWORD 되쓰기**만 "0 을 씀" 으로 셌다. 요청서 문구 그대로 "memset 이 덮으면 0 을 씀" 으로 세면 `_ETHREAD` 107 + 트랩 프레임 32 = **139칸이 전부 "0 을 씀"** 이 되어 4가지 가름이 사라진다 (아래 요약 표 밑에 그 경우의 숫자를 한 줄로 적었다). 둘이 다른 곳: **트랩 프레임은 memset 이 0 을 보장하는 유일한 줄**이라 (스택이 새것이라 앞서 0 이었는지 정적으로 모름) 의미가 `_ETHREAD` 와 조금 다르다 — 그래서 같은 4가지로 가르되 "아무도 안 씀 12" 칸에 `0 은 memset 에서 옴` 을 표시했다. 어느 쪽으로 셀지는 오케스트레이터·flow-writer 가 정한다.
- **같은 이름이 다른 칸**: 이 결과서의 `ThreadLock` 은 쓰지 않는다 (ETHREAD `+0x590` 푸시락은 3.13 아는 칸). `Dr0`·`ShadowStackFrame` 처럼 **겹치는 칸 (같은 바이트)** 은 같은 가름을 따른다 (`SelectedCpuSets` = `SelectedCpuSetsIndirect`, `Dr0` = `ShadowStackFrame`, `Dr1 ~ Dr7` ⊂ `Spare`).
- **부모(현재) 스레드 칸은 안 센다**: `PspAllocateThread` 안의 `KiStackAttachProcess`·`KxContextToKframes +0x3ba` (`BTR.LOCK [RCX],0x18`, RCX = `gs:[0x188]` 의 현재 스레드) 는 부모 스레드 칸이다.

## 요약 표 (169칸 → ?)

| | 칸 | 아는 칸 (nt) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (만들기 길 안) | 범위 밖 길에서 씀 `[추측]` | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `_ETHREAD` `K1~K2` | 27 | **27** | — | — | — | — | 0 |
| `_ETHREAD` `끝까지 0` | 107 | — | **6** | **10** | **22** | **69** | 0 |
| `_KTRAP_FRAME` `K1~K2` | 3 | **3** | — | — | — | — | 0 |
| `_KTRAP_FRAME` `끝까지 0` | 32 | — | **1** | **13** | **12** | **6** | 0 |
| **`K1~K2` 합** | **30** | **30** | — | — | — | — | **0** |
| **`끝까지 0` 합** | **139** | — | **7** | **23** | **34** | **75** | **0** |
| **합** | **169** | **30** | **7** | **23** | **34** | **75** | **0** |

- `K1~K2` 30 = 쓰는 줄을 찾고 사진 K2 값과 맞춰 봄. `_ETHREAD` 의 `Tcb` 는 통째 칸이라 쓰는 줄이 따로 없고 **"안쪽 칸 (3.14 `_KTHREAD` 237칸) 으로 셈"** — 아는 칸 27 에 포함하되 3.13 에는 "→ 3.14" 한 줄. `*` 칸 (사진에서 뒤에 또 바뀜) 은 **처음 값만** 닫음 — 뒤에 바꾸는 쪽은 이름만.
- run49 방식으로 합치면 baggage 2장 "아는 칸" 에 `30 + 7 + 23 = 60` (줄이 있는 칸) 이 들어가고 (`_ETHREAD` 5 → **48**, `_KTRAP_FRAME` 6 → **23**), "아무도 안 씀" 은 `34`, "범위 밖 길" 75 는 flow-writer 가 정한다 (run47·run49·run50·run52 와 같은 방식). 모름이 남는 곳은 요청서가 범위 밖이라 한 `_ETHREAD` 뒤 구간 10칸 (`K2~K3` 5 · `K3~P0` 4 · `P1~P1a` 1) 과 `_KTRAP_FRAME` `P1~P1b` 25 (Q78d4).
- **memset 을 "0 을 씀" 으로 쳤을 때의 숫자 (대안)**: `끝까지 0` 139 → 0 을 씀 139 (`_ETHREAD` 107 · 트랩 프레임 32), 나머지 칸은 "범위 밖 길에서도 씀" 이름만 덧붙임 (75칸 — 위 표의 75와 같은 칸) / "조건부 갈래도 있음" (23칸). 아는 칸에 들어가는 수는 `30 + 139 = 169`.
- 조건부 10 + 13 중 **갈래를 안 탔는지, 탔는데 값이 0 인지**를 정적으로 못 가르는 칸은 없다 (켜는 쪽이 비트·포인터라 K2 값 0 = 안 탐). 갈림길 입력 몇 개는 사진 값으로 간접 확인했고, 전역 값 확인은 D1.

## 한눈에

- **`_ETHREAD` `K1~K2` 27칸은 거의 `PspAllocateThread` 한 함수가 쓴다**: 자기 연결 리스트 머리 8개 (`KeyedWaitChain`·`ActiveTimerListHead`·`IrpList`·`BoostList`·`DeboostList`·`PropertySet`·`OwnerEntryListHead`·`DisownedOwnerEntryListHead`), 상수 3 (`ReadClusterSize` 0xf · `Silo` −3 · `MmSlabIdentity` 0xff), `CreateTime`, `EnergyValues`. 도우미가 쓰는 칸은 `KeInitializeSemaphore` (세마포어 두 칸), `KeInitThread → KeInitializeApc` (`UpdateTebApc*` 묶음 6칸), `PsInitializeThreadRngState → RtlInitializeTRng` (`RngState`·`UsedByRngState`), `ObInitializeFastReference` (`ProcessFastRef`).
- **겹치는 칸 (`UpdateTebApcFill1~5`·`MmSlabIdentity`·`RngState`)** 은 같은 88바이트 (`_KAPC`) 를 가리키는 자리다: `KeInitializeApc` 가 앞 `Type`·`Size`·`Thread`·루틴·`NormalContext`·`ApcStateIndex`·`ApcMode`·`Inserted` 만 쓰고, `SpareLong0` (+4) 의 맨 위 바이트 (0x727) 는 `PspAllocateThread +0x41a` 가 미리 `0xff` 로 써 둔 `MmSlabIdentity`, `SystemArgument1/2` (+0x40/+0x48 = 0x760/0x768) 는 `RngState` 16바이트가 차지한다 (이름이 달라도 같은 바이트). 이래서 사진 `UpdateTebApc` 가 0 아닌 8바이트 덩어리 7개 (코드 5 + `RngState` 2).
- **`끝까지 0` `_ETHREAD` 107칸 중 6칸만 만들기 길이 0 을 직접 쓴다** (락 5 + `RundownProtect`). 10칸은 "조건부 안 탐". 나머지는 만들기 길 밖 — 비트 묶음 (`CrossThreadFlags` 19 · `SameThreadPassiveFlags` 14 · `SameThreadApcFlags` 8+7) 이 약 절반이고, 이 묶음을 켜는 곳은 대부분 `NtSetInformationThread`·주소 공간 락·`Dbgk`·가장하기 (`PsImpersonateClient`) 같은 이름이 보인다 (`[추측]`, 오탐 가능).
- **test.exe 는 "사용자 스레드, 프로세스가 `_CONTEXT` 를 줌, 사용자 스택 인자 있음, 프로세서 그룹 1개, 피코·복제 아님, 숨김 플래그 없음" 길이다.** 갈림길 (아래 2.2): (1) 만들기 플래그 비트 2 (숨김) 꺼짐, (2) `KeQueryMaximumGroupCount() ≤ 1`, (3) 사용자 스택 인자 ≠ 0 → 피코·복제 줄 (`+0x5b3 → +0x2ca9a9`) 안 탐, (4) `ThreadContext ≠ 0` → 시스템 스레드 줄 (`+0x541 → +0x8ed`) 안 탐, (5) `NpxState` 비트 10 (`0x400`) 없음 → `ExBuildPasidMsrForThread` 안 부름.
- **트랩 프레임은 `KiInitializeContextThread` 한 함수가 만든다**: `+0x187` memset(0x190) → `+0x198` `ExceptionActive`=1 → `+0x1ac KxContextToKframes` (`EFlags`·`Rip`·`Rsp`·`Rcx`·`Rdx`·`Rbp`·`Rax`·`R8~R11`·`SegCs`·`SegSs`) → `+0x1f1` `MxCsr`=0x1f80. **핵심: `+0x157 ~ +0x15f` 가 `ContextFlags` 를 `0x100001 | (원래 & 6)` 로 줄여서 넘긴다** → `KxContextToKframes` 의 부동소수점 (0x8)·디버그 레지스터 (0x10)·XSTATE (0x40)·CET (0x80) 갈래는 **이 호출 자리에서는 입력과 무관하게 늘 안 탄다.** 그래서 트랩 프레임 `Xmm0~5`·`Dr0~7` 은 "조건부 안 탐" 이고 `MxCsr` 는 CONTEXT 가 아니라 상수.
- **만들기 길 안에 간접 호출은 없다.** `.asm` 26개 (아래 3) 에서 `CALL reg`·`CALL [mem]`·`JMP reg` 0개 → Q35 와 무관.

## 질문별 답 [정적]

경로 약어 (`(호출 +0x..)` = 부르는 자리, ret 아님):

- `①` = `NtCreateUserProcess`
- `①A` = `① → PspAllocateThread (호출 +0xe57)`
- `①K` = `①A → KeInitThread (호출 +0x6f4)`
- `①C` = `①K → KiInitializeContextThread (호출 +0x2fc)`
- `①KC` = `①C → KxContextToKframes (호출 +0x1ac)`

### 1. `K1~K2` 30칸 쓰는 줄

**답: 30칸 모두 쓰는 줄을 찾았고 사진 K2 값과 칸마다 맞춰 봤다. 이 중 `*` 칸 8개는 K2 이후 또 바뀌는데 (아래 표), 처음 값 줄만 정확이고 뒤에 바꾸는 쪽은 이름만 적었다.** 값의 출처는 칸마다 (상수 / 계산 / 인자 / 프로세스에서 / 전역).

#### 1.1 `_ETHREAD` `K1~K2` 27칸 (baggage 3.13 양식, 바로 옮길 수 있게)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `Tcb*` (0x4c0B) | ① | 통째 `_KTHREAD` — 쓰는 줄이 따로 없고 **안쪽 칸 (3.14, run52) 으로 셈** | — | nt | → 3.14 (`①A → KeInitThread …`) | `[정적]` sast46 · 안쪽으로 셈 · 사진 `K1~K2` → 바뀜 `K2~K3` 이후 매 구간 (안쪽 칸) |
| `+0x4c0` `CreateTime` | ① | 이 스레드를 만든 시각 (FILETIME 8B). **계산 (전역 시각)**: `KeQuerySystemTimeUnsafe` (= 공유 데이터 `KUSD+0x14` `SystemTime` 8바이트 읽기, `KeQuerySystemTimeUnsafe +0x0 ~ +0xa`) 가 0 이 아니면 `KeQuerySystemTimePrecise` 가 `[RCX]` 에 쓴다 (공유 데이터 시각 + 성능 카운터 보정, `KeQuerySystemTimePrecise +0xc7`). 0 이면 (부팅 직후) `KUSD+0x14` 를 그대로 베낌. 사진 K2 `0x01dd646a00bc6af3`, 뒤 구간 안 바뀜 | 주소 넘김 (→ `KeQuerySystemTimePrecise`) / 직접 (대체) | nt | `①A → KeQuerySystemTimePrecise (호출 +0x4bf)`; 갈림길 `+0x4aa` (호출) · `+0x4b2` (JZ); 대체 `+0x758 ~ +0x767` | `[정적]` sast47 · 정확 · 사진 `K1~K2` (K1 은 옛 값) |
| `+0x4c8` `ExitTime` (8B) · `KeyedWaitChain` (16B) | ① | 자기 연결 리스트 머리 (비어 있음). `ExitTime` 은 `KeyedWaitChain.Flink` 와 같은 8바이트 (겹침) → 값 = 스레드 + 0x4c8. 사진 K2 `…5548` = `…5080 + 0x4c8` | 직접 | nt | `①A +0x439 · +0x43d` (LEA `+0x432`) | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x4d8` `PostBlockList` (16B) | ① | 앞 8B (`ForwardLinkShadow`) 는 memset 0, **뒤 8B 는 `StartAddress` (+0x4e0) 와 같은 바이트** — 아는 칸 `StartAddress` 줄이 그 값을 쓴다 (`RtlUserThreadStart`). 사진 K2 `[0 − 0x7ffafb70cac0]` | 직접 | nt | `①A +0x640 ~ +0x647` (3.13 `StartAddress` 줄과 같은 줄) | `[정적]` sast47 · 정확 (겹침) · 사진 `K1~K2` |
| `+0x4f8` `ActiveTimerListHead` | ① | 자기 연결 (비어 있음). 사진 K2 `…5578` | 직접 | nt | `①A +0x498 · +0x49c` (LEA `+0x491`) | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x518` `KeyedWaitSemaphore` · `AlpcWaitSemaphore` (같은 32B `_KSEMAPHORE`) | ① | `KeInitializeSemaphore(스레드+0x518, 0, 1)`: `Type` 5, `Size` 8, `SignalState` 0 (인자 `EDX`), 기다림 줄 자기 연결, `Limit` 1 (인자 `R8D`). 사진 K2 8바이트 덩어리 4/4 | 주소 넘김 (→ `KeInitializeSemaphore`) | nt | `①A → KeInitializeSemaphore (호출 +0x42d)` ← `+0x4 · +0x7 · +0xb · +0xe · +0x12 · +0x15` | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x540` `IrpList` | ① | 자기 연결 (비어 있음). 사진 K2 `…55c0` | 직접 | nt | `①A +0x47c · +0x480` (LEA `+0x475`) | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x598` `ReadClusterSize` | ① | `0xf` (상수 — 읽기 앞당김 크기 기본값) | 직접 | nt | `①A +0x410` | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x5f0` `BoostList` · `+0x600` `DeboostList` | ① | 자기 연결 (비어 있음). 사진 K2 `…5670` · `…5680` | 직접 | nt | `①A +0x447 · +0x44b` (LEA `+0x440`) / `+0x455 · +0x459` (LEA `+0x44e`) | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x658` `PropertySet` (24B) | ① | `_PS_PROPERTY_SET`: 머리 (+0x658) 자기 연결 + 락 (+0x668) 0. 사진 K2 덩어리 2/3 | 직접 | nt | `①A +0x46a · +0x46e · +0x471` (LEA `+0x463`) | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x688` `EnergyValues` | ① | **전역 (에너지 추정 켜짐) + 계산 (스레드 + 0x798)**: `PoEnergyEstimationEnabled()` (전역 `PopEnergyEstimationEnabled`, nt+`0xf0e670`) 가 켜져 있으면 **스레드 + 0x798** (몸통 바로 뒤 에너지 칸 0xc8B) 를 가리키는 포인터를 넣고, 같은 때 `Header` 비트 21 `EnergyProfiling` 도 켬 (sast46 이 센 줄). 꺼져 있으면 null. 사진 K2 `…5818` = 스레드 + 0x798 → 켜져 있음 `[관찰]` | 직접 | nt | `①A +0x31a` (LEA `+0x313`); 갈림길 `+0x223 (호출) → +0x30c` | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x698` `Silo` | ① | `−3` (0xfffffffffffffffd, 상수 — "아직 실로 안 정해짐" 표시로 보임 `[추측]`) | 직접 | nt | `①A +0x49f` | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x6b8` `OwnerEntryListHead` · `+0x6d0` `DisownedOwnerEntryListHead` | ① | 자기 연결 (비어 있음). 사진 K2 `…5738` · `…5750` | 직접 | nt | `①A +0x4cb · +0x4cf` (LEA `+0x4c4`) / `+0x4e0 · +0x4e4` (LEA `+0x4d9`) | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x720` `UpdateTebApc*` (88B `_KAPC`) · `UpdateTebApcFill1` (3B) · `UpdateTebApcFill2` (4B) · `UpdateTebApcFill3*` (64B) · `UpdateTebApcFill4*` (72B) · `UpdateTebApcFill5*` (83B) | ① | **run52 의 둘째 `KeInitializeApc` 와 같은 호출이다**: `KeInitThread +0x339 LEA RCX,[RDI+0x720]` → `+0x368 CALL KeInitializeApc`. 인자: `Thread` = 새 스레드, `Environment` 0, `KernelRoutine` = rva `0x4a10c0` (하는 일 없는 stub — 같은 자리에 PDB 이름 100개), `RundownRoutine` 0, `NormalRoutine` = **`KiUpdateTebApc` (rva `0x312820`)**, `ApcMode` 0, `NormalContext` = 새 스레드. 채우는 칸 (`_KAPC` 안): `Type` 0x12 · `Size` 0x58 · `AllFlags` 0 · `Thread` · `KernelRoutine` · `RundownRoutine` 0 · `NormalRoutine` · `NormalContext` · `ApcStateIndex` 0 · `ApcMode` 0 · `Inserted` 0. **안 쓰는 칸**: `SpareByte1` (+3 = `Win32kPriorityFloor` 자리), `SpareLong0` (+4 ~ +7 = 아래 `MmSlabIdentity` 등), `ApcListEntry` (+0x10), `SystemArgument1/2` (+0x40/+0x48 = `RngState` 자리). `Fill1` = 앞 3바이트 (`0x580012`), `Fill2` = 4바이트 (+ `SpareByte1`), `Fill3` = 64B (`NormalContext` 까지), `Fill4` = 72B (+ `SystemArgument1`), `Fill5` = 83B (+ `SystemArgument2`·`ApcStateIndex`·`ApcMode`·`Inserted`). 사진 K2 `UpdateTebApc` 0 이 아닌 8바이트 덩어리 7개 = 코드 5 (`Type·Size`+`MmSlabIdentity`, `Thread`, `KernelRoutine`, `NormalRoutine`, `NormalContext`) + `RngState` 2 | 주소 넘김 (→ `KeInitializeApc`) | nt | `①K → KeInitializeApc (호출 +0x368)` ← `+0x5 (Type) · +0x8 (Size) · +0x56 (AllFlags) · +0x2f (Thread) · +0x3b (KernelRoutine) · +0x26 (RundownRoutine) · +0x3f (NormalRoutine) · +0x4e (NormalContext) · +0x1f · +0x4b · +0x52`; 인자 `KeInitThread +0x34a ~ +0x365` | `[정적]` sast47 · 정확 · 사진 `K1~K2` → 바뀜 `UpdateTebApc`·`Fill4`·`Fill5`: `K3~P0`·`P0~P1` (바이트 0x724~0x726 = 3.13 `K3~P0` 의 `LastSoftParkElection*` 칸, `KiUpdateSoftParkElectionStatisticsOnInsertion` `[추측]`) + `P1~P1a`·`P1a~P1b`·`P1b~P2` (0x760~0x76f = `RngState` 소비); `Fill3`: `K3~P0`·`P0~P1` (0x724~0x726 만) |
| `+0x727` `MmSlabIdentity` | ① | `0xff` (상수). `_KAPC.SpareLong0` 의 맨 위 바이트 — `KeInitializeApc` 가 `SpareLong0` 을 안 써서 값이 남는다 (쓰는 순서: `+0x41a` 가 먼저, `KeInitThread` 가 나중) | 직접 | nt | `①A +0x41a` | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x760` `RngState*` (16B) · `+0x768` `UsedByRngState*` (8B) | ① | **계산 (난수)**: 스레드 자기 필드 (`+0x510` 스레드 ID, `+0x508` 프로세스 ID, `+0x4c0` `CreateTime`, KTHREAD `+0xf0` `Teb`, `+0x28` `InitialStack`), 현재 스레드 (`gs:[0x188]`), 스레드 주소, `RDTSC`, `gs:[0x20]` PRCB `+0x8640` 을 모두 XOR 해 씨앗을 만들고 (`PsInitializeThreadRngState +0x10 ~ +0x63`), `RtlInitializeTRng(스레드+0x760, 씨앗)` 이 splitmix64 로 8바이트 둘을 낸다 (`+0x6` 에서 뒤 8B 를 먼저 0 으로 → `+0x54` 가 두 번 쓰며 앞 8B, 뒤 8B 를 채움). `UsedByRngState` = 뒤 8B. 사진 K2 `0xe4e96a3b6fecb8ce` | 주소 넘김 (→ `PsInitializeThreadRngState → RtlInitializeTRng`) | nt | `①A → PsInitializeThreadRngState (호출 +0x70b)` → `RtlInitializeTRng (호출 +0x70)` ← `+0x6 · +0x54` | `[정적]` sast47 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1`·`P1~P1a`·`P1a~P1b`·`P1b~P2` (스레드 난수를 뽑을 때마다 상태가 밀림 — 풀 할당 쪽 `ExAllocateHeapPool`·`RtlpHpAllocateHeap` 계열 `[추측]`, 이름만) |
| `+0x790` `ProcessFastRef` | ① | `프로세스 주소 | 0xf` (빠른 참조: 아래 4비트에 미리 확보한 참조 수 15). `ObInitializeFastReference` 가 프로세스 객체에 참조 15개를 미리 더하고 (`ObpIncrPointerCountEx(프로세스−0x30, 0xf)`, `+0x1e`) 이 값을 쓴다 (`+0x30`). 사진 K2 `…b08f` = 프로세스 `…b080` + 0xf (K1 은 0) | 주소 넘김 (→ `ObInitializeFastReference`) | nt | `①A → ObInitializeFastReference (호출 +0x71a)` ← `+0x30` | `[정적]` sast47 · 정확 · 사진 `K1~K2` |

- 위 표의 칸 수: `Tcb` 1 + `CreateTime` 1 + `ExitTime`·`KeyedWaitChain` 2 + `PostBlockList` 1 + `ActiveTimerListHead` 1 + 세마포어 2 + `IrpList` 1 + `ReadClusterSize` 1 + `BoostList`·`DeboostList` 2 + `PropertySet` 1 + `EnergyValues` 1 + `Silo` 1 + `OwnerEntryListHead`·`DisownedOwnerEntryListHead` 2 + `UpdateTebApc` 묶음 6 + `MmSlabIdentity` 1 + `RngState`·`UsedByRngState` 2 + `ProcessFastRef` 1 = **27**. 요청서 7.2 `K1~K2 (27)` 목록의 이름을 하나씩 위 표에서 찾았다 — 빠진 이름 0, 더 들어간 이름 0 (scratchpad `etcells.py`).
- `*` 칸은 `Tcb`·`UpdateTebApc`·`Fill3`·`Fill4`·`Fill5`·`RngState`·`UsedByRngState` = 7개 (7.2 의 `*` 표시와 같음). 사진 K2~P2 일곱 장을 칸마다 비교해 근거 칸에 적었다 (scratchpad `etvals.py`).
- **쓰는 순서** (사진 값 설명): `memset +0x307` → `EnergyValues +0x31a` → `RundownProtect +0x3cc` → `ReadClusterSize +0x410` → `MmSlabIdentity +0x41a` → `KeInitializeSemaphore +0x42d` → 리스트 머리들 `+0x439 ~ +0x4e7` → `CreateTime +0x4bf` → `PsAssignThreadId +0x52d` → `KeInitThread +0x6f4` (`UpdateTebApc`) → `PsInitializeThreadRngState +0x70b` → `ObInitializeFastReference +0x71a`. `RngState` 씨앗이 `CreateTime`·`Cid`·`Teb`·`InitialStack` 을 쓰므로 순서가 이래야 한다.

#### 1.2 `_KTRAP_FRAME` `K1~K2` 3칸 (baggage 3.15 양식)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x2b` `ExceptionActive` | ① | `1` (상수). 트랩 프레임 = `InitialStack − 0x190` (`R15 − 0x190`, `+0x18c LEA RAX`) 의 한 바이트. 사진 K2 1 (K1 은 쓰레기 `0xb3`) | 직접 (바이트) | nt | `①C +0x198` | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x2c` `MxCsr` | ① | `0x1f80` (상수 — 부동소수점 기본 제어값). `KxContextToKframes` 의 FP 갈래가 `CONTEXT.MxCsr & KiMxCsrMask` 로 쓰는 줄 (`Kx +0x310`) 은 안 타서 (아래 2.5) **CONTEXT 값이 아니라 이 상수가 남는다.** 사진 K2 `0x1f80` | 직접 | nt | `①C +0x1f1` (`R11 = R15 − 0x190` ← `+0x1b5`) | `[정적]` sast47 · 정확 · 사진 `K1~K2` |
| `+0x178` `EFlags*` | ① | **인자 (CONTEXT) + 상수**: `(CONTEXT.EFlags & 0x210dd5) \| 0x200` (사용자 모드: IF 비트 켬). 모드 인자 `R14B` = 1 (`①C +0x193`) 이라 `CMOVZ` 가 안 골라 `BTS R8D,9` 한 값을 쓴다. `CONTEXT.EFlags` (+0x44) 는 `NtCreateUserProcess +0xcc7 memset` 으로 0 이고 `PspCreateUserContext` 가 안 건드려서 결과 `0x200`. 사진 K2 `0x200` | 주소 넘김 (→ `KxContextToKframes`) | nt | `①KC +0x5f` (읽기 `+0x40`, `AND +0x47`, `BTS +0x53`, `TEST R14B +0x58`, `CMOVZ +0x5b`) | `[정적]` sast47 · 정확 · 사진 `K1~K2` → 바뀜 `P0~P1` (`0x202`)·`P1~P1a` (`0x246`)·`P1a~P1b` (`0x202`) — `PspSetContext +0x5a` (맡긴 문맥으로 다시 씀)·시스템 호출 진입 `[추측]`, 이름만 |

- 칸 수 대조: 7.2 `_KTRAP_FRAME` `K1~K2 (3)` = `ExceptionActive`, `MxCsr`, `EFlags*` — 빠진 이름 0, 더 들어간 이름 0.
- 3.15 메모 ("`+0x187 memset`, `+0x198` `ExceptionActive`, `+0x1f1` `MxCsr`") 는 `.asm` 줄이 그대로 맞다. 메모의 "`EFlags*` ← `KxContextToKframes` (호출 `+0x1ac`, 안 읽음)" 를 읽었다: `Kx +0x5f`.

### 2. 끝까지 0 139칸 가르기

**답: `_ETHREAD` 107 = 0 을 씀 6 · 조건부 안 탐 10 · 아무도 안 씀 22 · 범위 밖 길에서 씀 69 (합 107). `_KTRAP_FRAME` 32 = 0 을 씀 1 · 조건부 안 탐 13 · 아무도 안 씀 12 · 범위 밖 길에서 씀 6 (합 32). 판단 기준은 위 한 줄.** 사진 확인: 139칸 모두 K2 ~ P2 일곱 장에서 0 (scratchpad `etvals.py`, 예외 0).

#### 2.1 `_ETHREAD` — 0 을 씀 (6) — 명시 0 저장

(DWORD 되쓰기로 0 을 쓰는 칸은 없다 — `CrossThreadFlags`·`SameThreadPassiveFlags`·`SameThreadApcFlags` 를 읽고 되쓰는 줄은 만들기 길 안에 없다. `CrossThreadFlags` DWORD 되쓰기는 `PspInsertThread +0x617 ~ +0x649` 에 있고 Q78d4.)

| 칸 | 쓰는 줄 | 비고 |
|---|---|---|
| `ActiveTimerListLock` | `①A +0x48a` (MOV [RSI+0x4f0], 0) | 값 0 (명시 0 저장) |
| `RundownProtect` | `①A +0x3cc` (MOV [RSI+0x588], 0) | 값 0 (명시 0 저장) |
| `BoostListLock` | `①A +0x45c` (MOV [RSI+0x610], 0) | 값 0 (명시 0 저장) |
| `IrpListLock` | `①A +0x483` (MOV [RSI+0x618], 0) | 값 0 (명시 0 저장) |
| `DisownedOwnerEntryListLock` | `①A +0x4d2` (MOV [RSI+0x6c8], 0) | 값 0 (명시 0 저장) |
| `Win32kThreadLock` | `①A +0x4e7` (MOV [RSI+0x778], 0) | 값 0 (명시 0 저장) |

- 같은 형태의 줄이지만 K1~K2 칸인 것 (표 1.1): `PropertySet` 락 `+0x471`. 아는 칸: `ThreadLock` `+0x409` (초기화 0).

#### 2.2 `_ETHREAD` — 조건부 안 탐 (10) — test.exe 가 왜 안 타나

| 칸 | 켜는 줄 | 조건 | test.exe 가 왜 안 탐 |
|---|---|---|---|
| `CrossThreadFlags.HideFromDebugger` (비트 2) | `①A +0xb04` (`OR [RSI+0x5a0], 4`) | `+0x3eb TEST AL,4` — 만들기 플래그 (`PspAllocateThread` 아홉째 인자 `[RSP+0x260]` = `NtCreateUserProcess` 지역 `[RBP+0x10]`, `NtCreateUserProcess +0xe1b LEA`) 의 비트 2. 이 플래그는 `NtCreateUserProcess +0xdce PspMapThreadCreationFlags` 가 입력 `[RBP+0x7d8]` 의 비트를 옮긴 것 (입력 비트 0x4 → 출력 0x4 `PspMapThreadCreationFlags +0x1b`), 이어서 `+0xde0 OR 0x10` · `+0xdfd OR 0x60` (+ 조건부 `+0xdf8` 비트 10) 만 더함 | 입력에 숨김 비트를 안 줌 `[추측]` (CreateProcess 기본값). **사진 K2 `CrossThreadFlags` 비트 2 = 0** `[관찰 간접]` |
| `CrossThreadFlags.IndirectCpuSets` (비트 17) · `SelectedCpuSets` · `SelectedCpuSetsIndirect` (같은 8B) | `①A +0xaea` (`BTS [RSI+0x5a0],0x11`) · `+0xaf8` (`[RSI+0x690] = 스레드 + R14D`) | `KeQueryMaximumGroupCount()` (= 전역 `KiMaximumGroups`, nt+`0xfc5014`; 호출 `+0x23c`) **> 1** 일 때만 (`+0x252 CMP R12W,1` → `+0x259 JA +0xacb` → `R14D ≠ 0` → `+0x338 TEST R14D` → `JNZ +0xaea`). 이때 몸통 뒤 확장 영역에 CPU 집합 칸이 더 붙음 | 프로세서 그룹이 1개 (VM) `[관찰 간접]`: 사진 K2 비트 17 = 0, 0x690 = 0. 전역 값은 D1 |
| `SameThreadPassiveFlags.ClonedThread` (비트 4) · `UserFsBase` · `UserGsBase` · `SetContextState` | `①A +0x2caa99` (`OR [RSI+0x5a4],0x10`) · `+0x2ca9d7` · `+0x2ca9e2` (`UserFs/GsBase` ← 인자 구조체 `+0x8/+0x10`) · `+0x2caa31` (`ExAllocatePool2 'PsSc'` 결과) | **복제·피코 갈래**: `+0x5ab ~ +0x5b3` 에서 사용자 스택 인자 (`[RSP+0x70]`, 11번째 인자) = 0 이면 `+0x2ca9a9` 로 간다. 거기서 `EPROCESS.PicoContext` (`+0x640`, `+0x2ca9a9 CMP`) ≠ 0 이면 **피코 줄** (UserFsBase·UserGsBase·SetContextState, `Header` 비트 26 `Minimal` `+0x2ca9e9`), = 0 이면 **복제 줄** (`PsQueryThreadStartAddress` `+0x2caa77`, `ClonedThread` `+0x2caa99`, 프로세스 `MitigationFlags2` 비트 14 면 `+0x2caaf6` CET 비트) | 사용자 스택 인자 ≠ 0 → 일반 길 (`PspSetupUserStack` `+0x5ea`) `[관찰 간접]`: 사진 K2 `Teb` = `MmCreateTeb` 결과, `EPROCESS.PicoContext` = null, `Header` 비트 26 = 0 (`0x200006`), `ClonedThread` = 0 |
| `SameThreadPassiveFlags.SecureContext` (비트 9) | `①A +0xbb2` (`BTS [RSI+0x5a4],9`) | **시스템 스레드 갈래** (`ThreadContext == 0`, `+0x541 TEST R13 → JZ +0x8ed`) 안에서 만들기 플래그 `0x400` (`+0x960 TEST [RAX],0x400`) 이고 보안 프로세스 (`EPROCESS.SecureState` `+0x170` 비트 0 → `[RSP+0x50]`, `+0x134 ~ +0x142`; 검사 `+0xb8d`) 일 때 (그때 시작 루틴도 `PspSecureThreadStartup` rva `0xaaf3b0`) | `ThreadContext ≠ 0` (`NtCreateUserProcess +0xe3a` 가 `_CONTEXT` 를 넘김) → 사용자 스레드. 사진 비트 9 = 0 |
| `SameThreadApcFlags.PasidMsrValid` (0x5a9 비트 7) | `ExBuildPasidMsrForThread +0x1c` (`OR byte [RCX+0x5a9],0x80`; `①C +0x40c` 가 부름) | 세 겹: 전역 `KeFeatureBits` 비트 0x38 (`①C +0x291`) · `KUSD.XState.EnabledFeatures ≠ 0` (`+0x3ee`) · **`NpxState` 비트 10 (`0x400`)** (`+0x3f7 ~ +0x403`); 불러도 `EPROCESS.DeviceAsid` (`+0x600`, `ExBuildPasidMsrForThread +0xe`) = 0 이면 그냥 돌아온다 | 사진 K2 `NpxState` = `0xe5` (비트 10 없음), `DeviceAsid` = 0, `PasidMsrValid` = 0. 전역은 D1 |

- 결과: 위 표 5줄 = **10칸** (`HideFromDebugger` 1 + `IndirectCpuSets`·`SelectedCpuSets`·`SelectedCpuSetsIndirect` 3 + `ClonedThread`·`UserFsBase`·`UserGsBase`·`SetContextState` 4 + `SecureContext` 1 + `PasidMsrValid` 1).
- 이 갈래들의 범위 밖 길 (이름만 `[추측]`): `UserFsBase` ← `SwapContext` (스레드 전환 때 읽어 저장), `SetContextState` ← `PspExitThread` (풀기), `SecureContext` ← `PsCallEnclave`.
- `BypassProcessFreeze`·`CetUserShadowStack` 같은 `_KTHREAD` 쪽 조건부는 sast46 에서 이미 센 칸이다. 여기서 새로 센 것은 `_ETHREAD` 칸뿐.

#### 2.3 `_ETHREAD` — 아무도 안 씀 (만들기 길 안) (22)

`ForwardLinkShadow`, `MmLockOrdering`, `CrossThreadFlags.RundownFail`, `CrossThreadFlags.UmsForceQueueTermination`, `CrossThreadFlags.ReservedCrossThreadFlags`, `SameThreadPassiveFlags.ActivePageLists`, `SameThreadPassiveFlags.WorkloadClass`, `SameThreadPassiveFlags.GenerateDumpOnBadHandleAccess`, `SameThreadPassiveFlags.ReservedSameThreadPassiveFlags`, `SameThreadApcFlags.StartAddressInvalid`, `SameThreadApcFlags.SuppressSymbolLoad`, `AllowUserWritesToExecutableMemory`, `AllowKernelWritesToExecutableMemory`, `DisablePageFaultClustering`, `LockOrderState`, `MmReserved`, `AlpcWaitListEntry`, `CmCallbackListHead`, `VerifierContext`, `CmThreadInfo`, `UpdateTebSpareByte2`, `MmFaultClusterContext`

- 근거: `PspAllocateThread` 의 `.asm` 전수 (스레드 포인터 `RSI` 로 쓰는 줄 모두, 먼 조각 포함) 에 이 칸들이 없고, callee `.asm` 에서도 `_ETHREAD` 범위 [0x4c0, 0x798) 로 쓰는 줄이 `PsAssignThreadId +0x18` (`Cid.UniqueThread`, 아는 칸) 하나뿐이다 (`KeInitThread` 와 그 callee 12개, `PsInitializeThreadRngState`·`RtlInitializeTRng`·`KeInitializeSemaphore`·`ObInitializeFastReference` 를 `.asm` 줄 단위로 확인; `PspSetupUserStack`·`KiStackAttachProcess`·`PspCreateUserContext` 도 없고, `MmCreateTeb +0x175 · +0x17e` 는 TEB 쪽 기준). 훑기에서도 이름 있는 스레드 쪽 쓰기가 안 보인다 (또는 다른 구조체 오탐: `MmLockOrdering` ← `MmStoreRegister`, `MmReserved`·`ForwardLinkShadow` ← Job 쪽 함수).
- 값 0 은 `PspAllocateThread +0x307 memset` 에서 온다.
- **훑기의 한계 (알려 둠)**: `LEA` 로 칸 주소를 만든 뒤 그 포인터로 쓰는 줄 (리스트 삽입 등) 은 훑기에 안 잡힌다. `AlpcWaitListEntry` (ALPC 가 리스트에 매다는 칸)·`CmCallbackListHead` (등록 콜백 매달기) 가 그런 후보다 `[추측]`. 먼 조각은 이번에 따라갔지만 (위 "훑기를 전보다 넓혔다") 조각 주인이 확실치 않은 `FUN_…` 조각은 이름만 있다.

#### 2.4 `_ETHREAD` — 범위 밖 길에서 씀 `[추측]` (69)

이름 있는 쓰는 함수가 만들기 길 밖에 있는 칸 (**전체 훑기, 오프셋 숫자 일치라 틀릴 수 있음**; `.asm` 없는 objdump 근거. `PspInsertThread` 줄만 `.asm` 있음 — Q78d4):

| 칸 | 쓰는 곳 (범위 밖) |
|---|---|
| `TerminationPort` | NtRegisterThreadTerminatePort +0x71 (같은 8B 무리) |
| `ReaperLink` | 같은 8B (0x4e8) 무리 — 이름 후보는 위와 같음. reaper 줄에 매다는 쪽 (PspExitThread) 은 훑기에 안 잡힘 |
| `KeyedWaitValue` | NtWaitForKeyedEvent +0x108·+0x1ef, NtReleaseKeyedEvent +0x170 |
| `ClientSecurity` | PsImpersonateClient +0x14c, PsRestoreImpersonation +0x8f, NtOpenThreadTokenEx |
| `TopLevelIrp` | IoSetTopLevelIrp +0x9 |
| `DeviceToVerify` | IoSetDeviceToVerify +0x0, IoSetHardErrorOrVerifyDevice +0xc |
| `ChargeOnlySession` | PspExitThread +0x790 (정리 쪽, 오탐 가능) |
| `LegacyPowerObject` | PspExitThread +0x718 (정리 쪽, 오탐 가능) |
| `CrossThreadFlags.Terminated` | PspTerminateThreadByPointer, PspInsertThread 실패 경로 `+0x96f` (BTS.LOCK, `.asm` 있음, Q78d4) |
| `CrossThreadFlags.ActiveImpersonationInfo` | PsImpersonateClient, PsRestoreImpersonation, PsDisableImpersonation, NtOpenThreadTokenEx |
| `CrossThreadFlags.HardErrorsAreDisabled` | PsSetThreadHardErrorsAreDisabled, IoSetThreadHardErrorMode |
| `CrossThreadFlags.BreakOnTermination` | NtSetInformationThread |
| `CrossThreadFlags.SkipCreationMsg` | DbgkpSetProcessDebugObject (떨어진 조각 `FUN_140b2dea5`, `lock or [r13+0x5a0], 0x40`) |
| `CrossThreadFlags.SkipTerminationMsg` | DbgkpSetProcessDebugObject |
| `CrossThreadFlags.CopyTokenOnOpen` | NtOpenThreadTokenEx, PsImpersonateClient, PsRestoreImpersonation |
| `CrossThreadFlags.DisableDynamicCodeOptOut` | NtSetInformationThread |
| `CrossThreadFlags.ExplicitCaseSensitivity` | NtSetInformationThread |
| `CrossThreadFlags.PicoNotifyExit` | PspCreatePicoThread |
| `CrossThreadFlags.DbgWerUserReportActive` | DbgkQueueUserExceptionReport, DbgkUserReportWorkRoutine, NtSetInformationThread |
| `CrossThreadFlags.ForcedSelfTrimActive` | MiSelfTrim, MiUnlockWorkingSetShared |
| `CrossThreadFlags.SamplingCoverage` | EtwpCovSampCaptureApc, EtwpCovSampCaptureSample |
| `CrossThreadFlags.ImpersonationSchedulingGroup` | IopProcessWorkItem, PspUpdateContainerImpersonation |
| `SameThreadPassiveFlags` | DWORD 통째: ExpWorkerThread, MmStoreProbeAndLockPages, MiBackgroundZeroLocalPages, CcWorkerThread, SmStWorker 등 (비트별 아래) |
| `SameThreadPassiveFlags.ActiveExWorker` | ExpWorkerThread |
| `SameThreadPassiveFlags.MemoryMaker` | CcWorkerThread, CcAsyncLazywriteWorker, MiDereferenceSegmentThread, SmKmStoreHelperWorker |
| `SameThreadPassiveFlags.StoreLockThread` | MmStoreProbeAndLockPages |
| `SameThreadPassiveFlags.KeyedEventInUse` | NtWaitForKeyedEvent, NtReleaseKeyedEvent |
| `SameThreadPassiveFlags.SelfTerminate` | NtTerminateProcess +0x1f5, PspInsertThread `+0x9d6` (`.asm` 있음, Q78d4) |
| `SameThreadPassiveFlags.RespectIoPriority` | CmpSetRespectIoPriorityThread +0x21 |
| `SameThreadPassiveFlags.ZeroPageThread` | MiBackgroundZeroLocalPages |
| `SameThreadPassiveFlags.BalanceSetManager` | MiWorkingSetManager, MiPartitionWorkingSetManager |
| `SameThreadApcFlags` | DWORD 통째: LOCK_ADDRESS_SPACE, UNLOCK_ADDRESS_SPACE_*, MiLockVad, MiUnlockVad, PsSetSystemPagePriorityThread 등 (비트별 아래) |
| `SameThreadApcFlags.OwnsProcessAddressSpaceExclusive` | LOCK_ADDRESS_SPACE, UNLOCK_ADDRESS_SPACE_UNORDERED, MiLockAddressSpaceToo |
| `SameThreadApcFlags.OwnsProcessAddressSpaceShared` | LOCK_ADDRESS_SPACE_SHARED, UNLOCK_ADDRESS_SPACE_SHARED |
| `SameThreadApcFlags.HardFaultBehavior` | SmStWorker |
| `SameThreadApcFlags.EtwCalloutActive` | EtwTracePageFault +0x98·+0xde |
| `SameThreadApcFlags.Prefetching` | PsSetCurrentThreadPrefetching +0x2f |
| `SameThreadApcFlags.OwnsVadExclusive` | MiLockVad, MiUnlockVad, MiObtainReferencedVadEx |
| `SystemPagePriorityActive` | PsSetSystemPagePriorityThread +0x2f, PsRevertToUserPagePriorityThread +0x20 |
| `SystemPagePriority` | 같은 두 함수 |
| `OwnsVadShared` | MiLockVadShared, MiUnlockVadShared |
| `SlabReplenishInProgress` | MiCreateSlabEntry +0x1e5·+0x5ad |
| `CacheManagerActive` | MmCheckCachedPageStates, MmFlushSection, MiPfExecuteReadList |
| `ActiveFaultCount` | MiPrefetchVirtualMemory, MiIssueHardFault, MmPrefetchPagesEx, MiPrefetchControlArea (inc/dec) |
| `SharedPsModuleLockAcquires` | MiAcquireLoadLock +0x3b, MiReleaseLoadLock +0x15 |
| `AlpcMessageId` | AlpcpDispatchNewMessage, AlpcpDispatchReplyToWaitingThread, AlpcpDispatchReplyToPort, AlpcpCancelMessage, AlpcpReceiveSynchronousReply |
| `AlpcMessage` | AlpcpReceiveMessagePort, AlpcpSendLegacySynchronousRequest |
| `AlpcReceiveAttributeSet` | 같은 칸 앞 4B — 위와 같은 함수 |
| `ExitStatus` | PspExitThread +0x100, PspTerminateThreadByPointer +0x6e, PspInsertThread 실패 경로 `+0x2ca1d6` (`.asm` 있음, Q78d4) |
| `CacheManagerCount` | IoPageReadEx +0x2b3 |
| `IoBoostCount` | ExpApplyPriorityBoost, PsBoostThreadIo, PsEnterPriorityRegion, PsLeavePriorityRegion |
| `IoQoSBoostCount` | ExpApplyPriorityBoost, ExpBoostIoAfterAcquire |
| `IoQoSThrottleCount` | IoUpdateThreadIoRateThrottle +0x6 |
| `ReservedForSynchTracking` | ExpAcquireResourceSharedLite, ExpAcquireSharedStarveExclusive, ExReleaseResourceLite, EtwpGetTrackingLockSlotForThread |
| `ActivityId` | IoSetActivityIdThread, IoClearActivityIdThread, IoPropagateActivityIdToThread, IopCallDriverReference |
| `SeLearningModeListHead` | SeSetLearningModeObjectInformation, SeClearLearningModeObjectInformation |
| `AdjustedClientToken` | PsImpersonateClient, PsRestoreImpersonation, PsDisableImpersonation, NtOpenThreadTokenEx, PspThreadDelete |
| `WorkOnBehalfThread` | PspUpdateContainerImpersonation, PspRevertContainerImpersonation, IopProcessWorkItem |
| `PicoContext` | PspCreatePicoThread +0x330, VmpProcessContextSetup |
| `ThreadName` | NtSetInformationThread +0x9df |
| `EtwSupport` | EtwpTraceStackWalk, EtwpExpandStackAndWalkFrameChain |
| `FlsData` | PspExitThread +0x311·+0x50e, PsTlsSetValue +0x7a |
| `LastSoftParkElectionRunTime` | KiUpdateSoftParkElectionStatisticsOnInsertion |
| `LastSoftParkElectionGeneration` | KiUpdateSoftParkElectionStatisticsOnInsertion, KiCommitThreadWait |
| `UserIsolationDomain` | KiAdjustUserIsolationDomainThread, KiIpiUpdateUserIsolationDomain |
| `Win32kPriorityFloor` | PsAdjustWin32kPriorityFloor +0x4d |
| `LastSoftParkElectionWorkloadType` | KiUpdateSoftParkElectionStatisticsOnInsertion +0x4e |
| `HeapData` | RtlpHpAllocateHeap 계열 (`gs:0x188` 에서 온 스레드, 오탐 가능) |
| `ThreadIndex` | PspQueryThreadIndexInformation +0xe5 |

- 비트 묶음의 DWORD 셈: `CrossThreadFlags` 19비트 중 조건부 2 · 아무도 안 씀 3 (`RundownFail`·`UmsForceQueueTermination`·`Reserved`) · 범위 밖 14. `SameThreadPassiveFlags` 칸 15 (DWORD 1 + 비트 14) 중 조건부 2 · 아무도 안 씀 4 · 범위 밖 9. `SameThreadApcFlags` 영역 16 (DWORD 1 + 0x5a8 비트 8 + 0x5a9 비트 6 + 0x5aa 비트 1) 중 조건부 1 (`PasidMsrValid`) · 아무도 안 씀 4 · 범위 밖 11. (19 + 15 + 16 = 50 칸, 나머지 57 은 락·리스트·포인터·카운터 칸.)
- `PspInsertThread` (Q78d4) 가 `_ETHREAD` 칸을 쓰는 줄 (`.asm` 있음): `+0x617 ~ +0x649` (`CrossThreadFlags` 비트 9~14 갈아 끼움 — 아래 "Q78d4 로 넘길 것"), `+0x490` (`ThreadInserted`), `+0x96f` (`Terminated`, 실패 경로), `+0x9d6` (`SelfTerminate`), `+0x2ca1d6` (`ExitStatus`, 실패 경로).

#### 2.5 `_KTRAP_FRAME` `끝까지 0` 32칸 — 0 을 씀 1 · 조건부 안 탐 13 · 아무도 안 씀 12 · 범위 밖 6

**먼저 한 줄: `KiInitializeContextThread +0x157 ~ +0x15f` 가 `ContextFlags` 를 `(원래 & 6) | 0x100001` 로 줄여서 `KxContextToKframes` 에 넘긴다** (R9D = `0x100003` 또는 `0x100001`; 원래 `PspCreateUserContext +0x5` 의 `0x10000b` 면 `0x100003`). `KxContextToKframes` 안 블록은 비트가 켜져 있을 때만 탄다:

| 블록 | 조건 | 이 호출에서 | 트랩 프레임 칸 |
|---|---|---|---|
| CONTROL | `& 0x100001 == 0x100001` (`Kx +0x21 ~ +0x35`) | **탐** | `EFlags`·`Rip`·`Rsp`·`SegSs`·`SegCs` (`+0x5f · +0x6d · +0x7b · +0x91 · +0xa7`) |
| INTEGER | `& 0x100002 == 0x100002` (`+0xc9 ~ +0xd3`) | **탐** (원래 `0x10000b` 의 비트 1) | `Rax`·`Rcx`·`Rdx`·`R8~R11` (`+0xdd ~ +0x11f`), **`Rbp` (`+0x12a`)**; `Rbx`·`Rdi`·`Rsi` 는 예외 프레임 (`[RBP+0x100…]`) |
| XSTATE | `& 0x100040` + 모드 1 (`+0x193 ~ +0x1a5`) | 안 탐 (0x40 지움) | — |
| CET | `& 0x100080` (`+0x215 ~ +0x221`) | 안 탐 (0x80 지움) | `ShadowStackFrame` 읽기 |
| FLOATING_POINT | `& 0x100008` (`+0x227 ~ +0x233`) | **안 탐** (0x8 지움) | `Xmm0`~`Xmm5` (`+0x240 · +0x24b · +0x259 · +0x267 · +0x275 · +0x283`), `MxCsr` (`+0x310`) |
| DEBUG_REGISTERS | `& 0x100010` (`+0x2f8 ~ +0x344`) | **안 탐** (0x10 지움) | `Dr0`~`Dr3` (`+0x357 · +0x362 · +0x36d · +0x378`, 사용자 모드 한계 검사 `+0x41c · +0x42c · +0x43c`), `Dr6` (`+0x384`), `Dr7` (`+0x395`) |
| SEGMENTS (0x4) | — 이 함수는 이 비트를 **검사하지 않는다** | — | `SegDs`·`SegEs`·`SegFs`·`SegGs` 는 안 씀 |

| 가름 | 칸 | 쓰는 줄 · 근거 |
|---|---|---|
| **0 을 씀 (1)** | `Rbp` (+0x158) | `①KC +0x12a` (값 = `CONTEXT.Rbp` = `[RBX+0xa0]`, `+0x123` 읽기). CONTEXT 가 memset 0 이라 값 0 — **입력값 0 을 씀** (INTEGER 블록). 사진 K2~P2 0. 범위 밖에서도 씀: `PspInitializeThunkContext`·`PspSetContext` (맡긴 문맥), `KiCallUserMode` — 이름만 `[추측]` |
| **조건부 안 탐 (13)** | `Xmm0` · `Xmm2` · `Xmm3` · `Xmm4` · `Xmm5` (5) | 켜는 줄 `①KC +0x240 · +0x259 · +0x267 · +0x275 · +0x283`. 조건 `ContextFlags & 0x8` — **호출자가 비트를 지워서 늘 안 탐** (입력·test.exe 와 무관). 범위 밖에서도 씀 `[추측]`: 시스템 호출 진입 `KiSystemServiceUser +0x3c · +0x44 ~ +0x50` (`movaps [rbp−0x10 …]`, `.asm` 없음), `KxContextToKframes` (다른 호출자 — `NtContinue`·`NtSetContextThread`, 0x8 켜진 때) |
| | `Dr0` · `Dr1` · `Dr2` · `Dr3` · `Dr6` · `Dr7` (6) · `ShadowStackFrame` (= `Dr0` 와 같은 8B) · `Spare` (= `Dr1 ~ Dr7` 와 같은 40B) (2) | 켜는 줄 위 표. 조건 `ContextFlags & 0x10` — 호출자가 지워서 늘 안 탐. 사진 0. 범위 밖에서도 씀 `[추측]`: `KiSaveDebugRegisterState +0x2f` 외 (`mov [rbp+0x58 …0x80], dr0 …dr7` = 트랩 프레임 `+0xd8 ~ +0x100`), 시스템 호출 진입 `KiSystemServiceUser +0x5d` (`Dr7` 아래 16비트 0), `KxContextToKframes` |
| **아무도 안 씀 (12)** | `P5` · `DebugControl` · `SegDs` · `SegEs` · `SegFs` · `SegGs` · `Fill0` · `Logging` · `Fill1` · `Fill2` · `Fill3` · `Fill4` | 만들기 길 안 쓰는 줄 없음 — `KxContextToKframes` 는 SEGMENTS 비트를 안 보고 `SegDs`~`SegGs` 를 안 쓴다 (CONTEXT 의 세그먼트 값 `PspCreateUserContext +0x3d · +0x44` 는 트랩 프레임으로 안 간다). **0 을 보장하는 줄은 `KiInitializeContextThread +0x187 memset` 하나** (K1 사진은 같은 주소의 옛 내용이라 쓰레기). 훑기에서도 이름 있는 쓰는 곳이 안 보임 (`Fill3`·`Fill4` 의 훑기 후보 `PspInitializeThunkContext` 는 오프셋 숫자만 같고 `_CONTEXT` 쪽 줄로 보여 뺐다 `[추측]`) |
| **범위 밖 길에서 씀 `[추측]` (6)** | `PreviousIrql` (+0x29) | 인터럽트 진입이 이전 IRQL 을 씀: `KiInterruptSubDispatch +0x20` (`mov [rbp−0x57], al`), `KiInterruptSubDispatchNoLock`·`NoLockNoEtw`, `KiHvInterruptSubDispatch`, `KiIpiInterruptSubDispatch`, `KiNmiInterrupt +0x1f9`, `KiMcheckAbort` |
| | `LastBranchToRip` (+0x110) · `LastBranchFromRip` (+0x118) · `LastExceptionToRip` (+0x120) · `LastExceptionFromRip` (+0x128) | `KiSaveDebugRegisterState +0xb5 · +0xbb · +0x9e · +0xaa · +0xdd · +0xe3 · +0xc9 · +0xcf` (`rdmsr` 결과를 `[rbp+0x90 ~ +0xac]` 에 = 트랩 프레임 `+0x110 ~ +0x12c`), 디버그 제어가 켜졌을 때 |
| | `TrapFrame` (+0x138) | `KiServiceInternal +0x5b` (`mov [rbp+0xb8], r10` ← `[rbx+0x90]` = 스레드 `TrapFrame` 포인터 = 이전 프레임) — 커널 모드 시스템 호출 |

- 근거 보충: 트랩 프레임 진입 코드의 기준 레지스터는 `rbp = 트랩 프레임 + 0x80` (`KiSystemServiceUser +0x0` 이 `[rbp−0x55]` = 프레임 `+0x2b` 에 쓰고 `[rbp−0x54]` 에 `stmxcsr` = `+0x2c`). 이 보정으로 훑었다. `rbp` 기준 훑기는 오탐이 많아 (`P5` 45곳 등) 이름이 칸 뜻과 맞는 것만 "범위 밖" 에 넣었다.
- 사진: K1 의 트랩 프레임 자리는 옛 내용 (`ExceptionActive` 0xb3, `MxCsr` 0xfffff805, `P5` 0xfffff805b3bd0b40, `Dr3` 0x200001 …), K2 에서 한꺼번에 0 → 그 사이에 `+0x187 memset` 이 있다. 32칸 모두 K2~P2 에서 0 (P1a 이후 같은 자리에 시스템 호출 프레임이 덮이는 건 3.15 머리 설명 그대로이고, 이 32칸은 그때도 0).
- `Rax`·`R8~R11` (`+0x30`, `+0x48 ~ +0x60`) 도 `Kx +0xdd ~ +0x11f` 가 CONTEXT 값 (0) 으로 쓴다. 이 칸들은 7.2 `P1~P1a` 소속이라 이번 범위 밖 (Q78d4).

### 3. 갈림길과 입력 — 간접 호출

- **만들기 길 안에 이 두 구조체 칸을 쓰는 간접 호출은 없다.** `.asm` 26개 (`PspAllocateThread`, `KeInitThread`, `KiInitializeContextThread`, `KxContextToKframes`, `KeAbInitializeThreadState`, `KeInitializeApc`, `KeInitializeTimer`, `KeInitializeEvent`, `MmCreateKernelStack`, `KiAllocateXStateStack`, `KiCreateKernelShadowStack`, `KeCopyXfdMaskToTeb`, `PspSetupUserStack`, `MmCreateTeb`, + 이번에 읽은 `PsInitializeThreadRngState`, `RtlInitializeTRng`, `KeInitializeSemaphore`, `ObInitializeFastReference`, `PsAssignThreadId`, `KeQuerySystemTimePrecise`, `KeQuerySystemTimeUnsafe`, `KeQueryMaximumGroupCount`, `PoEnergyEstimationEnabled`, `ExBuildPasidMsrForThread`, `PspMapThreadCreationFlags`, `PspCreateUserContext`) 에서 `CALL reg`·`CALL [mem]`·`JMP reg`·`cfg:@` 를 찾았는데 **0개**다 (`.asm` 의 명령 열만 봄, 주석 제외). → Q35 에 넣을 것 없음.
- 갈림길 입력값 (`.asm` 줄): `ThreadContext` — `NtCreateUserProcess +0xe3a` 가 `R13` 을 넘기고 (`+0xd1d PspCreateUserContext` 가 채운 CONTEXT, 또는 `+0xd39` 복제 길). 사용자 스택 인자 — 일반 길이면 ≠ 0 (사진 `Teb` 로 간접). 만들기 플래그 — `[RBP+0x10]` (`PspMapThreadCreationFlags` 출력 + `0x10 | 0x60`). 이 다섯 갈림길은 위 2.2 표.
- **`KeInitializeApc` 에 함수 주소를 *인자로* 넘기는 줄**은 있다 (`KeInitThread +0x34a`: `KiUpdateTebApc` rva `0x312820`, `+0x35b`: stub `0x4a10c0`). 값으로 저장될 뿐 이 길에서 부르지 않는다.

### 4. 줄기와 경로

- **줄기** (칸 값을 정하는 함수까지 끝까지): `PspAllocateThread` 본문, `KeInitializeSemaphore`, `KeQuerySystemTimeUnsafe → KeQuerySystemTimePrecise` (`CreateTime`), `PsInitializeThreadRngState → RtlInitializeTRng`, `ObInitializeFastReference`, `KeInitThread → KeInitializeApc` (`UpdateTebApc`), `KiInitializeContextThread` (트랩 프레임 만들기), `KxContextToKframes`, `ExBuildPasidMsrForThread` (조건부 줄).
- **줄기 밖 (이름만)**: `memset`, `ObCreateObjectEx`·풀 할당, `PoEnergyEstimationEnabled`·`KeQueryMaximumGroupCount` (갈림길 값만), `PsAssignThreadId` (`Cid`, 아는 칸), `KeAbPreAcquire`·`ExfAcquirePushLockExclusiveEx`·`KeAbPostRelease` (ETHREAD 푸시락), `ObpIncrPointerCountEx` (`ObInitializeFastReference` 안, 프로세스 참조 늘림), `PspSetupUserStack`·`MmCreateTeb`·`KiStackAttachProcess`·`KiUnstackDetachProcess` (부모 스레드·TEB·스택), `RtlLocateExtendedFeature`·`RtlLocateSupervisorFeature` (XSAVE 영역 안), `VslKernelShadowStackAssist` (CET 갈래), `PspCreateUserContext` (`CONTEXT` 채움, 입력 쪽), `PspMapThreadCreationFlags` (플래그 변환), 스택 안 XSAVE 영역 내용·시작 프레임 (`KiInitializeContextThread +0x1bc ~ +0x1e8`, 칸 표 없는 짐).
- 칸마다 경로·근거는 위 1.1·1.2 표 (3장 양식). 0 칸 묶음은 2.1 ~ 2.5.

## 동적분석으로 확인해야 할 것

"사진" = 이미 있는 덤프 (`ms/logs/04_baggage-read_*.txt`, `ms/dump/03_baggage-snap/`) 로 답할 수 있는지. 번호는 이 조각 안 번호 (D1~D4).

| # | 질문 | 추천 멈춤점 / 찍을 것 | 사진으로? |
|---|---|---|---|
| D1 | 커널 전역 값: `PopEnergyEstimationEnabled` (nt+`0xf0e670`, 기대 1 — 사진 `EnergyValues` 가 채워진 것에서 거꾸로), `KiMaximumGroups` (nt+`0xfc5014`, 기대 1), `KeFeatureBits` (nt+`0xfc6b70`) **비트 0x38** (PASID 기능 — sast46 D1 의 비트 0x37 과 같이 읽기), `KUSER_SHARED_DATA+0x3d8` `XState.EnabledFeatures` (sast46 D1 과 같음), 공유 데이터 `+0x14` `SystemTime` (≠ 0 이면 `CreateTime` 이 `KeQuerySystemTimePrecise` 길) | `dd`/`dq` 한 번씩 (SystemTime 은 `dq 0xfffff78000000014 L1`) | **가능** (전역은 덤프에 있음) → Q79. 이것으로 조건부 중 전역에 걸린 칸 (`IndirectCpuSets`·`PasidMsrValid`·`EnergyValues`) 의 "왜 안 탔나" 가 닫힌다 |
| D2 | `NtCreateUserProcess` 가 `PspAllocateThread` 에 넘기는 만들기 플래그와 사용자 스택 인자 (조건부 갈래 입력) — 기대: 플래그 `0x70` (`0x10 \| 0x60`, 비트 2·9 없음), 사용자 스택 인자 ≠ 0 | `nt!NtCreateUserProcess+0xe57` 에서 `dd poi(@rsp+0x40) L1` (아홉째 인자 = `[RSP+0x40]`) 와 `dq @rsp+0x50 L1` (열한째 인자) — 조건: 현재 프로세스 = cmd.exe | 불가 (사진은 호출 뒤 값만). **낮은 우선순위** — K2 `CrossThreadFlags` 비트 2 = 0, `Teb` 가 `MmCreateTeb` 결과인 것이 이미 같은 뜻 |
| D3 | `KxContextToKframes` 에 들어가는 `ContextFlags` 가 정말 `0x100003` 인가 (FP·DEBUG 비트가 지워지는지) — 정적으로 이미 닫힘, 값만 확인 | `nt!KxContextToKframes` 입구 `r r9d` (조건: cmd.exe 가 부모 + 새 스레드 만들 때) | 불가. **낮은 우선순위** — 코드 (`KiInitializeContextThread +0x157 ~ +0x15f`) 로 닫혀 있어서 꼭 필요하진 않다 |
| D4 | 사진 K1 자리의 옛 객체·옛 스택 값이 왜 남아 있나 (이 주소를 쓰던 앞 스레드) | — | 필요 없음 (판정에 안 씀) |

- 급한 것은 없다. D1 은 사진 (Q79 에 전역 읽기 추가) 으로 닫히고, 나머지는 닫지 않아도 칸 표는 완성된다.
- 디버거가 붙어서 달라지는 점: 없음 (모든 근거가 정적 + 이미 찍은 사진).
- Q35 (간접 호출) 에 새로 넣을 것: 없음.

## Q78d3·Q78d4 로 넘길 것 (지나가며 본 줄)

**Q78d3 (`_TEB`)**
- `PspAllocateThread +0x793` (`MOV [TEB+0x17ee], R12W` — `SameTebFlags`) 와 복제 갈래의 TEB 쓰기 (`+0x2cab6d ~ +0x2cabbd`, 안 탐) 는 sast46 에서 이미 넘긴 줄 그대로. 이번에 `MmCreateTeb +0x175 · +0x17e` 가 `[RCX+0x6b8]`·`[RCX+0x6b4]` 에 쓰는 것을 봤다 — `RCX` 가 TEB 쪽 기준이라 `_ETHREAD` 칸이 아니다 (훑기가 `EtwSupport` 로 오인한 줄).

**Q78d4 (`K2` 이후 구간 — `KeStartThread`·`PspInsertThread`·스케줄러)**
- `_ETHREAD` 뒤 구간 10칸 (`ThreadListEntry`·`CrossThreadFlags`·`ThreadIoPriority`·`ThreadPagePriority`·`KernelStackReference` / `LastExpectedRunTime`·`LastSoftParkElectionGroupAffinity`·`LastSoftParkElectionQos`·`LastSoftParkElectionRunningType` / `SchedulerSharedDataObject`) 의 쓰는 줄 후보 — **`PspInsertThread` (`.asm` 있음)**:
  - `+0x2cd` `KernelStackReference` (+0x5ec) ← `R13D` (1 로 보임).
  - `+0x617 ~ +0x649` `CrossThreadFlags` DWORD 를 읽고 **비트 9~14 만 갈아 끼움**: 비트 9~11 (`ThreadIoPriority`) ← `EPROCESS+0x1f4` 를 18 비트 오른쪽으로 민 값의 `0xe00` 마스크, 비트 12~14 (`ThreadPagePriority`) ← `EPROCESS+0x1f0` 의 `0x7000` 마스크. 다른 비트는 그대로 되쓴다 (이 결과서의 `CrossThreadFlags` 비트 칸 19개에 대해 DWORD 되쓰기 줄이 이 자리에 있다는 뜻 — 이 결과서는 만들기 길 밖이라 세지 않았다).
  - `+0x490` `ThreadInserted` (아는 칸), `+0x6e3` `ThreadListEntry` (`LEA RCX,[R14+0x578]`, 뒤 줄은 읽지 않음), 실패 경로 `+0x96f` (`Terminated`), `+0x9d6` (`SelfTerminate`, 조건: 프로세스 `+0x1f4` 비트 30 `+0x5f7` 과 `[RSP+0xe8]` 가 가리키는 값의 비트 1 `+0x9ce`), `+0x2ca1d6` (`ExitStatus`).
- `LastSoftParkElection*` (`+0x6f8 ~ +0x71f`) 와 `UpdateTebApc` 바이트 0x724~0x726 은 같은 쓰는 곳으로 보인다 (`KiUpdateSoftParkElectionStatisticsOnInsertion`, 이번에 `KiCommitThreadWait` 도 보임 `[추측]`).
- `_KTRAP_FRAME` `P1~P1b` 25칸 중 만들기 길이 값을 쓰는 것: `Rax` (+0x30, `Kx +0xdd`), `R8~R11` (+0x48 ~ +0x60, `Kx +0xfe · +0x109 · +0x114 · +0x11f`) — 모두 CONTEXT 값 0 (K2 0), 뒤 (P1~) 는 시스템 호출 진입이 덮는다 (`KiSystemServiceUser +0x2c ~ +0x38`). 나머지 `P1Home`~`P4Home`·`GsBase`·`FaultAddress`·`NmiPreviousSpecCtrl`·`Rbx`·`Rdi`·`Rsi`·`ErrorCode` 등은 만들기 길이 안 쓴다 (예외 프레임으로 감).

## 정정 · 새로 알게 된 것 (flow-writer 용)

1. **트랩 프레임 `ContextFlags` 는 `0x100001 | (원래 & 6)` 로 잘려서 들어간다** (`KiInitializeContextThread +0x157 ~ +0x15f`). 그래서 `KxContextToKframes` 의 FP·DEBUG·XSTATE·CET 갈래는 이 길에서 입력과 무관하게 죽은 코드다. `MxCsr` 가 CONTEXT 에서 안 오고 `+0x1f1` 의 상수라는 설명과 이어진다 (3.15 `MxCsr` 줄에 "CONTEXT 값 아님" 을 덧붙일 것).
2. **3.15 `SegCs`·`SegSs` (짐작 2) 를 정확으로 올릴 수 있다**: 트랩 프레임에 쓰는 줄은 `KxContextToKframes +0xa7` (`SegCs` — 로컬 CONTEXT 의 `SegCs` 가 0x33 이면 0x33, 아니면 0x23, `CMOVZ`) 와 `+0x91` (`SegSs` = 0x2b). 로컬 CONTEXT 에 미리 0x33·0x2b 를 쓰는 줄은 `KiInitializeContextThread +0x166` (`MOV [RSP+0x68], AX`)·`+0x16e` (`[RSP+0x72]`). 지금 3.15 경로 칸의 "`KiInitializeContextThread +0xb5`" 는 CONTEXT 복사 줄 자리라 고칠 것 (`+0xb5` 는 `ThreadContext` 읽기).
3. **3.13 `Cid` 줄에서 `PsAssignThreadId` 는 다른 `_ETHREAD` 칸을 안 쓴다**: `+0x18` `[RBX+0x510]` (`Cid.UniqueThread`) 하나만. `Cid.UniqueProcess` 는 `PspAllocateThread +0x3da` (프로세스 `+0x1d0` ← `+0x3d3`).
4. **`UpdateTebApc` 는 run52 의 둘째 `KeInitializeApc` 와 같은 호출이다** (`KeInitThread +0x339 LEA RCX,[RDI+0x720]` → `+0x368`). 첫째 호출 (`SchedulerApc`, `+0x152`) 과 달리 `RundownRoutine` = 0, `NormalRoutine` = `KiUpdateTebApc` (rva `0x312820`, PDB 이름 확인함).
5. **`_KAPC` 겹침 정리**: `UpdateTebApc` (+0x720, 88B) 안에 `Win32kPriorityFloor` (+0x723), `LastSoftParkElectionQos/WorkloadType/RunningType` (+0x724 ~ +0x726), `MmSlabIdentity` (+0x727) 가 `SpareByte1`·`SpareLong0` 자리에 박혀 있다. `SystemArgument1/2` 자리 (+0x760/+0x768) 는 `RngState`. 7.2 에서 `K1~K2` 5칸 (`UpdateTebApc`·`Fill3~5`) 이 서로 다른 칸으로 보이지만 같은 88바이트를 어디까지 덮느냐만 다르다 (`WaitBlockFill*` 와 같은 꼴).
6. **`_ETHREAD` 몸통 뒤 확장 영역** (이름 있는 칸 표가 없는 짐, 7.6 후보): `EnergyValues` 칸 (+0x798 ~ +0x860, 0xc8B — `_THREAD_ENERGY_VALUES`), `_KAFFINITY_EX` 둘 (sast46 에서 이미 후보), `_KLOCK_ENTRIES` 둘 (sast46), 프로세서 그룹이 2개 이상일 때 붙는 CPU 집합 칸 (안 탐). 이번에 새로 센 것은 에너지 칸 크기 0xc8 (`+0x230 AND EBX,0xc8`) 의 코드 출처.
7. **K1 사진 주의 (정정 아님, 확인)**: 트랩 프레임 칸도 `_ETHREAD` 와 마찬가지로 K1 에서 옛 값 (쓰레기). 7.2 의 `_KTRAP_FRAME` `K1~K2` 도 사실상 "K2 에 처음 값이 보이는 칸" — 위 `끝까지 0` 판정은 K2 ~ P2 기준이라 문제없다.
8. **숫자 (baggage 7.2)**: 이 조각으로 `_ETHREAD` 모름 144 → **10** (`K2~K3` 5 · `K3~P0` 4 · `P1~P1a` 1), `_KTRAP_FRAME` 모름 60 → **25** (`P1~P1a` 24 · `P1a~P1b` 1). 아는 칸: `_ETHREAD` 5 → **48** (= 5 + 27 + 6 + 10), `_KTRAP_FRAME` 6 → **23** (= 6 + 3 + 1 + 13). 아무도 안 씀 `_ETHREAD` 22 · `_KTRAP_FRAME` 12, 범위 밖 길 `_ETHREAD` 69 · `_KTRAP_FRAME` 6 (flow-writer 가 어디에 셀지 정함). 이 결과서의 전체: 169 = 30 (아는) + 7 (0 을 씀) + 23 (조건부) + 34 (아무도 안 씀) + 75 (범위 밖) + 0 (모름).
9. **훑기 한계 (sast46 것에 더해)**: (a) `LEA` 로 만든 주소로 쓰는 줄·`SHL [mem]` 같은 이동 명령은 안 잡힌다. (b) 먼 조각은 이번에 따라갔지만 (`orphan.py`) 주인이 확실치 않은 `FUN_…` 조각은 이름이 없다 (`CrossThreadFlags` 비트 3 의 `FUN_140b40776` 등). (c) 트랩 프레임은 기준 레지스터가 `rbp = 프레임 + 0x80` 이라 보정이 필요하다. (d) `_ETHREAD` 오프셋 (0x4c0 이상) 은 EPROCESS·Job·ALPC 구조체와 숫자가 많이 겹쳐 오탐이 많다 (예: 0x4e8 · 0x5b8 · 0x6e8).

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **고칠 버그는 없다.** `run_ghidra.sh export` 세 번 (각 4초 안팎), `pdb_struct.py` (`_ETHREAD`·`_KTRAP_FRAME`·`_KUSER_SHARED_DATA`·`_KSEMAPHORE`·`_KAPC`·`_PS_PROPERTY_SET`·`_RTL_THREAD_RNG_STATE`·`_EPROCESS`·`_KTHREAD`·`_XSTATE_CONFIGURATION`), `pdb_aliases.py --where` 모두 문제없이 돌았다.
- **제안 1 (sast/43·44·46 제안을 다시, 이번에 더 구체적으로)**: `scripts/field_writers.py <바이너리> --struct _ETHREAD [--cells 목록]`. 이번에도 임시 스크립트 (scratchpad: `etscan.py`, `orphan.py`, `etbits.py`, `etsum2.py`, `tfscan.py`, `etvals.py`, `etcells.py`) 를 짰다. 그중 하네스 스크립트 (`field_writers.py`) 로 합치면 좋을 기능 셋: (a) **먼 조각 (Ghidra "떨어진 범위") 의 주인 붙이기** — `_index.tsv` 크기 안만 훑으면 `PspAllocateThread +0x2ca9a9`·`PspInsertThread +0x9d6` 같은 줄이 빠진다. 점프 목표를 따라 조각을 주인 함수에 붙이면 해결된다 (`orphan.py`). (b) **비트 묶음 풀기** (`bts`/`btr`/`or`/`and` 즉시값 → 비트 번호; `etbits.py`). (c) **기준 레지스터 보정** — 트랩 프레임 진입 코드는 `rbp = 프레임 + 0x80`, 스레드 쪽은 `gs:[0x188]` 에서 온 레지스터인지 표시.
- 제안 2: `pdb_struct.py` 의 겹치는 칸 (union·`Fill*`) 을 "같은 바이트 묶음" 으로 합쳐 보여 주는 옵션. 이번에 `UpdateTebApc` 6칸·`SelectedCpuSets` 2칸·`Dr0`/`ShadowStackFrame` 이 같은 바이트라는 걸 손으로 맞췄다.
- 제안 3 (사소): `ExportFunctions` 가 이미 `.asm` 이 있는 함수는 건너뛴다고 알려 주면 좋다 (이번엔 정규식으로 새 것만 골랐다).

## 이름 확인 결과

결과서의 backtick·표 안 함수 이름 163개를 `ms/decomp/ntoskrnl/_index.tsv` 와 맞춰 봤다 (scratchpad 스크립트). **함수 이름이 빠진 것은 0개다.** `_index.tsv` 에 없는 것은 모두 (a) 구조체 칸 이름·레지스터·명령어, (b) 전역 변수 4개 — `PopEnergyEstimationEnabled`·`KiMaximumGroups`·`KeFeatureBits`·`KiMxCsrMask` 는 `pdb_aliases.py --where` 로 PDB 에 있음을 확인 (rva `0xf0e670`·`0xfc5014`·`0xfc6b70`·`0xfc5110`, `.asm` 의 주소와 같음), (c) ntdll 함수 `RtlUserThreadStart` (nt 가 아님), (d) 접두어 `Dbgk`·`Kx` 와 스크립트 이름이다. 별칭 이름 둘: `KiUpdateTebApc` (rva `0x312820`) 는 `pdb_aliases.py --where` 로 PDB 이름 확인. stub (rva `0x4a10c0`) 는 PDB 이름 100개 무리 안 이름이라 `KiSchedulerApcNop`·`KiSchedulerApcRundown` (sast46 에서 확인) 으로만 부른다. `FUN_140b2dea5` 는 Ghidra 가 이름을 못 붙인 조각이고 `DbgkpSetProcessDebugObject` 에 속한다 (`+0x1ec`·`+0x209` 의 `je`·`jmp` 가 이 조각으로 뛴다 — `nt.dis` 점프 목표로 확인).

## 판정

**분석성공.** 요청서 질문 1~4 모두 답했다. 169칸 → 아는 칸 (nt) 30 · 0 을 씀 7 · 조건부 안 탐 23 · 아무도 안 씀 34 · 범위 밖 길 75 · 여전히 모름 0 (`_ETHREAD` 27+107, `_KTRAP_FRAME` 3+32). **PDB 일치**: GUID+Age `C29EBFB06B78B3C020DCA66D99713F9E1` 기준과 같음, `_calls.tsv` 호출 지점 (`+0xe5c`·`+0x6f9`·`+0x301`·`+0x1b1`) 과 사진 K2 값이 칸마다 일치 → 버전 일치. 정적으로 못 닫은 것: 조건부 갈래의 전역 입력 (`PopEnergyEstimationEnabled`·`KiMaximumGroups`·`KeFeatureBits` 비트 0x38 — D1, 사진으로 닫힘), 사용자 스택 인자·만들기 플래그의 입력값 (D2, 낮음), "범위 밖 길" 75칸·"아무도 안 씀" 34칸의 근거가 전체 훑기 (오탐 가능, `LEA` 쓰기는 못 잡음) 라는 점. 요청서의 memset 셈 문구와 run50·run52 의 셈이 다른 점은 위 "판단 기준" 에서 밝히고 두 숫자를 다 적었다 (오케스트레이터가 정함).

# 48 teb-fill — 정적 분석 결과 (Q78d3)

- 요청서: `_workspace/sast/48_teb-fill.md`
- 모듈 / 바이너리: ntoskrnl · ntdll (`ms/bin/.../System32/`). KERNELBASE·KERNEL32 는 "쓰는 함수 이름" 확인용으로 objdump 만 (`.asm` 안 뽑음).
- **PDB 일치 (둘 다)**:
  - ntoskrnl `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) = 기준값 = 사진 로그 `ms/logs/04_baggage-read_K2.txt` 의 `ntkrnlmp.pdb\C29EBFB06B78B3C020DCA66D99713F9E1`.
  - ntdll `ntdll.pdb` guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`) = 사진 로그 `04_baggage-read_P2.txt` 의 `lm` 줄 `ntdll.pdb\C3093720177DA6851519DCFFE8DF53FE1` (baggage 머리의 기준 `…53FE` + age 1 과도 같음).
- 분석: 이미 돼 있어서 다시 안 걸었다 (두 모듈 모두 `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 있음, Ghidra 프로세스 없음).
- **새로 뽑은 함수 25개, 전부 depth 0** (`ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`). `_calls.tsv` 는 `comm -23` 0줄 (줄이 안 사라짐): 커널 4,307 → 4,332줄, ntdll 2,188 → 2,299줄.
  - 커널 9: `KeUpdateTebSchedulingPropertiesCurrentThread` · `KiUpdateTebSchedulingPropertiesCurrentThread` · `KiUpdateTebApc` (`P0~P1` 다섯 칸), `RtlCreateUserStack` (`DeallocationStack` 의 출처), `MiAllocateFromSubAllocatedRegion` (TEB 페이지가 왜 0 인가), `PspWriteTebImpersonationInfo` · `KeRaiseUserException` (범위 밖 길 근거 줄), `KiQueueTebUpdateApc` · `MmDeleteTeb` (뽑기만 하고 안 읽음 — Q78d4·종료 쪽 자료).
  - ntdll 16: `LdrInitializePerThreadRng` · `RtlInitializeTRng` · `LdrpGenRandom` (`RngState` 씨앗), `RtlNtStatusToDosError` · `RtlSetLastWin32ErrorAndNtStatusFromNtStatus` · `RtlRestoreLastWin32Error` (`LastStatusValue`·`LastErrorValue`), `RtlFlsSetValue` (`FlsData`), `RtlpLocalInfoAllocFromCache` · `RtlpSubSegmentInitialize` · `RtlpLowFragHeapAllocFromContext` · `RtlpHpVsContextHandleContention` (`HeapData`), `RtlpCheckRelativeDrive` (`HardErrorMode`), `RtlTlsAlloc` (`TlsSlots`·`TlsExpansionSlots`), `RtlpHpEnvFlsCleanup` (`HeapFlsData`), `RtlpAllocateActivationContextStackFrame` · `RtlSetThreadErrorMode` (뽑기만 하고 안 읽음 — 범위 밖 확인용, `RtlpAllocateActivationContextStackFrame +0x1fd [RCX+0x2c8]` 은 TEB 가 아니라 활성화 문맥 프레임 캐시 칸이라 오탐으로 버림).
- 이미 있어서 **읽기만 한 것** (다시 안 뽑음, 파일 시각 10-22/24 그대로): 커널 `MmCreateTeb` · `PspAllocateThread` · `PspUserThreadStartup` · `PspSetupUserStack` · `KeInitThread` (`+0x1e4`·`+0x48d ~ +0x49f` 줄만) · `KeCopyXfdMaskToTeb` · `PspMapThreadCreationFlags` · `NtCreateUserProcess` (`+0x1b0 ~ +0x1d4`, `+0xdc1 ~ +0xe57` 줄만). ntdll `_LdrpInitialize` · `LdrpInitializeProcess` (TEB 줄만) · `LdrpAllocateTls` · `RtlpInitializeThreadActivationContextStack` · `RtlAllocateHeap` · `LdrpDrainWorkQueue` · `LdrpDropLastInProgressCount`. KERNELBASE `CreateProcessInternalW` (`+0x6cb`·`+0x30af` 줄만).
- **Ghidra 로 안 뽑고 `objdump -d` 로만 본 곳** (`.asm` 없음 — 함수+오프셋으로만 적는다): ntdll·커널·KERNELBASE·KERNEL32 전체 TEB 쓰기 훑기 (scratchpad `tebscan2.py`·`tebscan3.py`, `callers.py`), 범위 밖 표의 함수 이름 대부분 (`Rtl*Thread*`·`Tpp*`·`Etw*`·`RtlpFlsSetValue +0xf4` 등), KERNELBASE 쪽 이름 전부.
- **훑기 한계 (한 줄)**: TEB 를 `gs:[0x30]` 로 얻은 함수는 레지스터를 따라가며 (분기·레이블 합치기 있음, 호출 뒤 휘발 레지스터는 버림) 쓰기를 모았고, TEB 를 **인자로 받은** 함수 (예 `RtlpInitializeThreadActivationContextStack`(RCX), `RtlpFlsSetValue`(슬롯 포인터 인자)) 와 **인덱스 레지스터 쓰기** (`TlsSlots[i]`) 는 숫자 일치 후보를 눈으로 가려냈다 → 오탐·누락 가능. 커널은 `[x+0xf0]` (`_KTHREAD.Teb`) 에서 로드한 레지스터만 따라갔다 (`PRCB`·`PCR` 오프셋과 겹치는 `gs:[…]` 쓰기는 오탐이라 버림).
- **버전 확인: 일치.** 알려진 사실 `_calls.tsv` 대조: `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess` · `PspInsertThread` `+0x802 / +0x807` → `PspCallProcessNotifyRoutines` · `LdrInitializeThunk` `+0x9 / +0xe` → `LdrpInitialize` · `LdrpInitializeInternal` `+0x55 / +0x5a` → `_LdrpInitialize` · `_LdrpInitialize` `+0x167 / +0x16c` → `LdrpInitializeProcess` · `LdrpInitializeProcess` `+0x1bb6 / +0x1bbb` → `LdrpInitializeGraphRecurse` · `LdrpInitializeNode` `+0x166 / +0x16b`·`+0x197 / +0x19c` → `LdrpCallTlsInitializers`·`LdrpCallInitRoutine`. 이번 길: `NtCreateUserProcess` `+0xe57 / +0xe5c` → `PspAllocateThread` → `+0x62c / +0x631` `MmCreateTeb` · `+0x5ea / +0x5ef` `PspSetupUserStack` · `PspUserThreadStartup` `+0x94 / +0x99`. **사진 K2 TEB 값이 코드와 칸마다 맞는다** (아래 1·2번 표 "사진" 열).

## 스크립트 문제

없다. `run_ghidra.sh export` 여섯 번, `pdb_struct.py`(`_TEB`·`_TEB32`)·`pdb_aliases.py --where` 모두 돌았다. Ghidra 시작 때마다 `Module manifest file error on line 3 of file: ~/.config/ghidra/…/Extensions/GhidraMCP/Module.manifest` 경고가 한 줄 나오지만 결과에는 영향 없다 (사용자 설정의 확장 모듈 manifest 문제). 도구 제안은 맨 아래.

## 판단 기준 (한 줄)

**길** = 커널 `NtCreateUserProcess → PspAllocateThread` 본문 + 그 callee 중 TEB 를 건드리는 것 (`MmCreateTeb`, `PspSetupUserStack → RtlCreateUserStack`, `KeInitThread → KeCopyXfdMaskToTeb`) + 첫 스레드 시작 `KiStartUserThread → PspUserThreadStartup` 의 callee; ntdll 은 **지도 안** (`_map/LdrInitializeThunk_nodes.tsv` 1,248개; 공용 부품 `helper` 는 노드는 길 안, 그 안쪽 callee 는 밖). 그 길의 `.asm`(또는 objdump) 에 TEB 칸을 쓰는 줄이 있으면 → 값 0 을 쓰거나 (**명시 0 저장**) 입력값을 옮겨 쓰면 **"0 을 씀"**; 켜는 줄이 있는데 test.exe 에서 조건이 안 맞으면 **"조건부 안 탐"**; 길 안에 줄이 없고 이름 있는 함수가 길 밖에서 쓰면 **"범위 밖 길"**(ntdll / nt / 다른 DLL); 어디서도 안 보이면 **"아무도 안 씀 (초기화 길 안)"**.

- **셈은 sast/46·47 과 같다**: 명시 0 저장·입력값 쓰기·같은 DWORD 되쓰기만 "0 을 씀". 이번엔 **몸통 `memset` 이 없다** — `MmCreateTeb` 도 `MiAllocateFromSubAllocatedRegion` 도 `memset` 을 안 부른다 (`_calls.tsv`: 앞쪽은 `KiStackAttachProcess`·`MiAllocateFromSubAllocatedRegion`·`KiUnstackDetachProcess` 뿐, 뒤쪽은 `RtlFindClearBitsAndSetEx`·`MiCommitExistingVad` 등). TEB 페이지는 서브할당 구역을 **새로 커밋한 요구 시 0 페이지**라 처음부터 0 이다 `[정적]` 부재 + `[추측]` 커밋 방식 (사진 K2 에서 0 확인). 그래서 "요청서 문구 vs run50·52 셈" 의 **대안 숫자는 없다**.
- **겹치는 칸**: `CurrentIdealProcessor` = `IdealProcessorValue` (같은 4B), `ReservedPad0 · ReservedPad1 · ReservedPad2 · IdealProcessor` 는 그 4B 안 바이트 하나씩. `SameTebFlags` 는 16비트 한 칸 + 비트 16개.
- **범위 밖 길 한 칸에 둘 이상일 때**: 길 안 분류 (0 을 씀 / 조건부) 가 있으면 그것으로 셈, 범위 밖 이름은 "범위 밖에서도 씀" 으로 덧붙임. 길 안 줄이 없으면 ntdll > nt > 다른 DLL 순으로 하나에만 셈.

## 요약 표 (133칸 → ?)

| 사진 구간 | 칸 | 아는 칸 nt | 아는 칸 ntdll | 아는 칸 다른 DLL | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (길 안) | 범위 밖 ntdll | 범위 밖 nt | 범위 밖 다른 DLL | 모름 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `K1~K2` | 8 | **8** | — | — | — | — | — | — | — | — | 0 |
| `P0~P1` | 5 | **5** | — | — | — | — | — | — | — | — | 0 |
| `P1~P1a` | 5 | — | **5** | — | — | — | — | — | — | — | 0 |
| `P1a~P1b` | 1 | — | **1** | — | — | — | — | — | — | — | 0 |
| `P1b~P2` | 2 | — | **2** | — | — | — | — | — | — | — | 0 |
| `끝까지 0` | 112 | — | — | — | **12** (nt 11 · ntdll 1) | **13** (nt 8 · ntdll 5) | **28** | **19** | **1** | **39** | 0 |
| **합** | **133** | **13** | **8** | **0** | **12** | **13** | **28** | **19** | **1** | **39** | **0** |

- 아는 칸 21 = `K1~K2` 8 + `P0~P1` 5 + `P1~P2` 8. `P1b~P2` 의 `LastStatusValue`·`FlsData` 는 **쓰는 줄이 ntdll** (`RtlNtStatusToDosError +0x1d`, `RtlFlsSetValue +0x104`) 이고 **부르는 쪽 (KERNELBASE·CRT) 은 `[공백]`** → 쓴 주인 ntdll 로 셈.
- 133 = 21 + 12 + 13 + 28 + 59 (범위 밖 19+1+39) + 0. 사진 `K2 ~ P2` 일곱 장에서 `끝까지 0` 112칸 모두 0 이다 (scratchpad `photo.py`·`cells.py`, 예외 0 — `SameTebFlags` 의 `RanProcessInit`·`LoadOwner` 같은 비트는 위 21+4 쪽).
- baggage 2장 합치기 (run49 방식, 제안): `_TEB` 아는 칸 4 → **4 + 21 + 12 + 13 = 50**, 아무도 안 씀 **28**, 범위 밖 길 **59**, 모름 **0** (137 = 50 + 28 + 59).
- 쓴 주인 별 쓰는 줄 수 (아는 칸 21 + 0 을 씀 12 + 조건부 13, 줄 기준): 커널 쪽 `MmCreateTeb` 13 · `PspAllocateThread` 1 (+ 복제 갈래 `+0x2cab6d ~ +0x2cabbd` 안 탐) · `KiUpdateTeb…` 2 · `KeCopyXfdMaskToTeb` 1(조건부), ntdll 쪽 `_LdrpInitialize` 1 · `RtlpInitializeThreadActivationContextStack` 1 · `LdrpAllocateTls` 1 · `LdrInitializePerThreadRng→RtlInitializeTRng` 1 · 힙 할당기 (HeapData·HeapWalkContext·RngState 갱신) · `LdrpDrainWorkQueue`/`LdrpDropLastInProgressCount` 2 · `RtlNtStatusToDosError` 1 · `RtlFlsSetValue` 1.

## 한눈에

- **TEB 는 두 손을 거친다.** 커널이 틀을 만든다 (`MmCreateTeb` 가 13줄: 스택 경계·ID·PEB 포인터·상수 둘·정적 문자열 머리), 커널이 첫 스레드 시작 때 두 번 더 쓴다 (`SameTebFlags` 16비트 통째, `CurrentLocale` + 프로세서 정보 5칸). 그 뒤 ntdll 이 `LdrpInitializeProcess` 안에서 TLS 벡터·난수 씨앗·활성화 문맥 스택·`RanProcessInit`·`SchedulerSharedDataSlot` 을 채우고, 로드 중에 `LoadOwner` 를 켰다 끄고, 힙 할당기가 `HeapData`·`RngState` 를 계속 갱신한다.
- **`P0~P1` 다섯 칸은 `PspUserThreadStartup +0x94 → KeUpdateTebSchedulingPropertiesCurrentThread → KiUpdateTebSchedulingPropertiesCurrentThread` 가 쓴다.** 이상적 프로세서 4바이트 (`+0x136`) 와 그룹 친화도 16바이트 (`+0x141`). 값은 스레드 객체 (`UserIdealProcessor`·`UserAffinity`) 에서 온다. 같은 함수가 `UpdateTebApc` (`KiUpdateTebApc`) 로도 불린다 — 친화도가 바뀔 때 다시 쓰는 길 (Q78d4).
- **`K1~K2` 8칸 중 7칸은 `MmCreateTeb` 의 64비트 갈래 13줄**, 나머지 둘 (`SameTebFlags`·`InitialThread`) 은 `PspAllocateThread +0x793` 의 16비트 쓰기 하나다. `MmCreateTeb +0x175 · +0x17e` 는 **64비트 TEB 가 아니라 `_TEB32.RealClientId`** (WOW64 x86 갈래, test.exe 안 탐).
- **`SameTebFlags` 는 첫 스레드에선 늘 `0x400` (InitialThread) 뿐이다 `[정적]`.** `NtCreateUserProcess +0x1bf` 가 `ThreadFlags & 0xfffffffe` 가 있으면 거절하고 (`0xc000000d`), `+0xdfd` 가 `0x60` 을 항상 더한다. 그래서 `SkipThreadAttach`·`LoaderWorker`·`SkipLoaderInit` 를 켜는 `PspAllocateThread +0x56a · +0x587 · +0x599` 는 이 길에선 코드로 닫혀 있다. 사진 K2 `0x400`.
- **`RngState` 는 커널이 아니라 ntdll 이 씨를 뿌린다**: `LdrpInitializeProcess +0x153 → LdrInitializePerThreadRng → RtlInitializeTRng` (앞 16바이트). 뒤 16바이트는 아무도 안 쓴다. 그 뒤 힙 할당기가 `xoroshiro128+` 모양으로 앞 16바이트를 갱신한다 (`RtlAllocateHeap +0x229 · +0x234` 등).
- **간접 호출은 길 안에 하나뿐** (`_LdrpInitialize +0x9a CALL RAX` — `UseWOW64` 갈래, x64 test.exe 는 안 탐).

## 질문별 답 [정적]

경로 약어 (`(호출 +0x..)` = 부르는 자리, ret 아님). baggage 3장 줄임 표에 **새로 제안**하는 둘:

- `①TB…` = `①A… → MmCreateTeb (호출 +0x62c)`. `①TB… +0x..` 는 `MmCreateTeb` 안 줄. (`①A…` 는 run52 줄임: `NtCreateUserProcess → PspAllocateThread (호출 +0xe57)`)
- `①TS…` = `①T… → KeUpdateTebSchedulingPropertiesCurrentThread (호출 +0x94) → KiUpdateTebSchedulingPropertiesCurrentThread (호출 +0x15)`. `①TS… +0x..` 는 마지막 함수 안 줄. (`①T…` 는 `KiStartUserThread → PspUserThreadStartup`)
- ntdll 은 baggage 3장의 `②…` (`LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess`) 그대로.

### 1. `K1~K2` 8칸 쓰는 줄

**답: 8칸 모두 줄을 찾았고 사진 K2 값과 칸마다 맞춰 봤다. 전부 커널 (nt).** `MmCreateTeb` 의 메모리 쓰기 줄은 모두 50줄 (run11 "약 47줄" 과 같은 묶음): **64비트 갈래(EDI = 4) 13줄 (`+0xc1 ~ +0x13a`)**, WOW64 x86 갈래 (EDI = 5, TEB 뒤 `+0x2000` 에 `_TEB32`) 19줄 (`+0x14a ~ +0x1db`), EDI = 6 갈래 17줄 (`+0x1e9 ~ +0x27f`). test.exe 는 `EPROCESS.WoW64Process` (+0x310) = 0 이라 `R13 = 0` → EDI = 4 (`+0x58 ~ +0x6a`). 크기 `0x1878` → 페이지 올림 `0x2000` (`+0x85 ~ +0xa2`).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x40` `ClientId` (16B) | ① | 프로세스 ID · 스레드 ID 한 쌍. `_ETHREAD.Cid` (`+0x508`) 를 그대로 베낀다 (`MmCreateTeb` 세 번째 인자 = `PspAllocateThread +0x615 LEA R8,[RSI+0x508]`). `Cid.UniqueProcess` ← `PspAllocateThread +0x3da`, `Cid.UniqueThread` ← `PsAssignThreadId +0x18` (3.13 아는 칸). 사진 `0x1dd0`·`0x26ec` | 직접 (`KiStackAttachProcess` 로 붙은 채) | nt | `①TB… +0xc9 · +0xcd` (`UniqueThread`, 읽기 `[R14+8]`) · `+0xd1 · +0xd4` (`UniqueProcess`) | `[정적]` sast48 · 정확 · 사진 `K1~K2` |
| `+0x60` `ProcessEnvironmentBlock` | ① | `EPROCESS.Peb` (`+0x2e0`) 값 = PEB 주소. 사진 `0x9b2fa66000` (TEB 바로 앞 쪽) | 직접 | nt | `①TB… +0x116 · +0x11d` | `[정적]` sast48 · 정확 · 사진 `K1~K2` |
| `+0x2e8` `TxFsContext` | ① | 상수 `0xfffe` (`+0xd8 MOV R9D,0xfffe`). "트랜잭션 파일 시스템 기본 보기" 표시로 보임 `[추측]` | 직접 | nt | `①TB… +0xd8 · +0xde` | `[정적]` sast48 · 정확 · 사진 `K1~K2` |
| `+0x7d8` `RealClientId` (16B) | ① | `ClientId` 와 같은 값 (가장하기에서 원래 ID 를 되찾으려는 사본). `[R14]`·`[R14+8]` | 직접 | nt | `①TB… +0xe5 · +0xe9` (`+0x7e0`) · `+0xf0 · +0xf3` (`+0x7d8`) | `[정적]` sast48 · 정확 · 사진 `K1~K2` |
| `+0x1258` `StaticUnicodeString` (16B) | ① | 머리만 채운다: `MaximumLength` = `0x20a` (= 522 = `StaticUnicodeBuffer` 크기, `+0xfa`·`+0x100` → `+0x125a`), `Buffer` = TEB + `0x1268` (`+0x108 LEA` → `+0x10f`). `Length` (`+0x1258`) 는 **안 쓴다** (0). 사진 `Buffer = 0x9b2fa68268`, `MaximumLength 0x20a` | 직접 | nt | `①TB… +0x100 · +0x10f` | `[정적]` sast48 · 정확 · 사진 `K1~K2` |
| `+0x1478` `DeallocationStack` | ① | `INITIAL_TEB.AllocatedStackBase` (유저 스택 예약 구역의 맨 아래). `MmCreateTeb` 두 번째 인자 `[RDX+0x20]` 를 옮긴다. 그 값은 `RtlCreateUserStack` 이 `INITIAL_TEB` 에 `AllocatedStackBase` (`+0x20`) · `StackBase` (`+0x10`) · `StackLimit` (`+0x18`) 로 채운 것. 사진 `0x9b2fc00000` (`StackBase = 0x9b2fd00000`, 예약 1MB) | 직접 (옮김) | nt | `①TB… +0x121 · +0x136 · +0x13a`; 원본 `①A → PspSetupUserStack (호출 +0x5ea) → RtlCreateUserStack (호출 +0x89) +0x128` | `[정적]` sast48 · 정확 · 사진 `K1~K2` |
| `+0x17ee` `SameTebFlags*` (16비트 통째) | ① | 만들기 플래그에서 계산한 값. 첫 스레드는 `0x400`. 계산 `+0x54a ~ +0x5a5`: 플래그 `0x40` → `(x & 0x40) << 4` = `0x400` (`InitialThread`, 비트 10) · `0x2` → `\|= 0x8` (`SkipThreadAttach`, 비트 3) · `0x80` → `\|= 0x2000` (`LoaderWorker`, 비트 13) · `0x100` → `\|= 0x4000` (`SkipLoaderInit`, 비트 14). 값이 0 이 아니면 (`+0x66f TEST R12W`) 프로세스에 붙어 **16비트 통째** 쓴다. 사진 `0x400`. `*` = 뒤에 `RanProcessInit`·`LoadOwner` 비트가 바뀜 (아래 2번) | 직접 | nt | `①A +0x793` (`MOV [RBX+0x17ee],R12W`; 붙기 `+0x78d`, 떼기 `FUN_140843198`·`KiUnstackDetachProcess`) ← 값 `+0x54a ~ +0x5a5` | `[정적]` sast48 · 정확 · 사진 `K1~K2` → 바뀜 `P1~P1a`·`P1a~P1b`·`P1b~P2` |
| `+0x17ee` `SameTebFlags.InitialThread` (비트 10) | ① | 위 같은 줄. 만들기 플래그 `0x40` 는 `NtCreateUserProcess +0xdfd OR [RBP+0x10],0x60` 이 **항상** 켠다 → 항상 1 | 직접 (위와 같은 쓰기) | nt | `①A +0x555 ~ +0x563` (계산) · `+0x793` (쓰기) | `[정적]` sast48 · 정확 · 사진 `K1~K2` (1) |

- 칸 수 대조: 7.2 `K1~K2 (8)` 이름 여덟 개를 위 표에서 하나씩 찾았다 — 빠진 이름 0, 더 들어간 이름 0.
- **`MmCreateTeb +0x175 · +0x17e` 는 어느 칸인가: `_TEB32.RealClientId`** (`pdb_struct.py ntdll.dll _TEB32`: `+0x6b4 RealClientId`, 크기 `0x1038` = `0x3038 − 0x2000`). `[RCX+0x6b8]` = `UniqueThread` ← `[R14+8]`, `[RCX+0x6b4]` = `UniqueProcess` ← `[R14]`. `RCX = TEB + 0x2000` (32비트 TEB) 이고 이 줄은 EDI = 5 (WOW64 x86) 갈래 (`+0x14a ~ +0x1db`) 안이다 → **test.exe 는 안 탄다, 64비트 TEB 칸이 아님** (sast/47 이 `_ETHREAD` 칸이 아니라고 한 것의 답).
- 같은 대입 묶음이 끝까지 0 칸에 명시 0 을 쓰는가: **없다.** 64비트 갈래 13줄은 모두 값이 있다 (`NtTib` 안쪽 `+0x20 Version = 0x1e00` · `+0x30 Self` · `+0x8 StackBase` · `+0x10 StackLimit` 포함 — 아래 "정정" 1). EDI = 6 갈래의 `+0x23d` (`[둘째 TEB + 0x1740]` = `GdiBatchCount` := 0) 는 **둘째 TEB** 쪽 쓰기이고 이 갈래 자체를 안 탄다.
- 복제 갈래 (`PspAllocateThread +0x5ab ~ +0x5b3` 에서 사용자 스택 인자 = 0) 는 `ClientId`·`RealClientId` 도 새로 쓰지만 (`+0x2cab6d`·`+0x2cab79`, `MOVDQU` 16B) **만들어 둔 TEB 를 재사용하는 길**이고 test.exe 는 안 탄다 (사진 `Teb` = `MmCreateTeb` 결과).

### 2. `P0~P2` 13칸 쓰는 줄

**답: 13칸 모두 줄을 찾았다. `P0~P1` 5칸은 커널 (`PspUserThreadStartup` 의 callee), 나머지 8칸은 ntdll.** `*` 칸은 처음 값을 쓰는 줄만, 뒤에 바꾸는 쪽은 이름만.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x1744` `CurrentIdealProcessor` (4B `_PROCESSOR_NUMBER`) = `IdealProcessorValue` · `+0x1746` `ReservedPad2` · `+0x1747` `IdealProcessor` | ① | 이 스레드의 이상적 프로세서. 4바이트 = `Group` (2B, = `ReservedPad0·1` 자리) · `Number` (1B, = `ReservedPad2`) · `Number` 사본 (1B, = `IdealProcessor`). 값: `_KTHREAD.UserIdealProcessor` (`+0xc4`) → 전역 `KiGlobalState` 표 (`+0x8c ~ +0xaa`) → 그룹 = 값 >> 6, 번호 = 값 & 0x3f; 입력이 0 이면 0 으로 시작 (`+0x16 AND dword [RAX+0x10],0`). 사진 P1 `0x01010000` = 그룹 0, 번호 1 | 직접 (DWORD 한 번) | nt | `①TS… +0x136` (`MOV [RCX+0x1744],EAX`); 앞에서 `UserIdealProcessor` 는 `KeInitThread → KeSelectInitialIdealProcessorForThread` (sast46) | `[정적]` sast48 · 정확 · 사진 `P0~P1` |
| `+0x1860` `PrimaryGroupAffinity` (16B `_GROUP_AFFINITY`) | ① | `Mask` = `UserAffinity` 비트맵의 `UserAffinityPrimaryGroup` 번째 8B (`+0x51 ~ +0x68`: `[KTHREAD+0x228]` `_KAFFINITY_EX` → `Bitmap[그룹]`), `Group` = `UserAffinityPrimaryGroup` (`+0x230`, 2B). 뒤 `Reserved` 6B 는 이 함수가 안 쓴 스택 자리 (`[RSP+0x3a ~ +0x3f]`) 를 그대로 옮긴다 `[정적]` — 사진은 0. 사진 P1 `0x3` (프로세서 0·1) | 직접 (16B 한 번) | nt | `①TS… +0x141` (`MOVDQU [RCX+0x1860],XMM0`) | `[정적]` sast48 · 정확 (`Reserved` 6B 값만 스택 쓰레기라 에뮬레이터는 0) · 사진 `P0~P1` |
| `+0x58` `ThreadLocalStoragePointer` | ② | **정적 TLS 벡터** 포인터. `LdrpTlsBitmap` 이 0 이 아닐 때만 (`+0x43`), `LdrpGetNewTlsVector` 가 만든 벡터를 TEB `+0x58` 에 놓는다 (`+0x1de ADD RAX,0x58` → `+0x1e2`). test.exe 는 exe TLS 가 있다. 사진 `0x25996f383e0` | 직접 | ntdll | `②… → LdrpInitializeTls (호출 +0x18cb) → LdrpAllocateTls (호출 +0x15d) +0x1e2` | `[정적]` sast48 · 정확 · 사진 `P1~P1a` |
| `+0x260` `RngState*` (32B, 앞 16B 만) | ② | **씨앗**: `LdrInitializePerThreadRng` 가 `LdrSystemDllInitBlock`·TEB 주소·`KUSD` 틱·스택 주소를 섞어 씨앗을 만들고 (`+0x4 ~ +0x40`) `RtlInitializeTRng(TEB+0x260, 씨앗)` 으로 **꼬리 호출** (`+0x54 JMP`). `RtlInitializeTRng` 가 splitmix64 로 8B 둘을 쓴다 (`+0x6` 에서 `[RCX+8]` 를 먼저 0, `+0x54` 가 `[R8]` 두 번). 뒤 16B (`+0x270 ~ +0x27f`) 는 안 쓴다. 사진 앞 16B 가 0 이 아님 | 직접 (`RtlInitializeTRng`) | ntdll | `②… → LdrpInitializeProcess` 의 `LdrInitializePerThreadRng (호출 +0x153) +0x54 ⇒ RtlInitializeTRng +0x6 · +0x54` | `[정적]` sast48 · 정확 · 사진 `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2`: 힙 할당기가 xoroshiro 식으로 갱신 (`RtlAllocateHeap +0x229 · +0x234`, `RtlpHpMetadataAlloc +0x2b2`·`RtlpHpReallocMove`·`RtlpHpLfhSlotAllocateSlow` 등, 이름만) |
| `+0x290` `_ActivationStack` (40B) | ② | 활성화 문맥 스택 한 개를 TEB 안에 만든다 (`RCX` = TEB): `ActiveFrame` 0 (`+0x18`) · `FrameListCache` 빈 리스트 (`+0x1f`·`+0x23`) · `NextCookieSequenceNumber` 1 (`+0x2b`) · `StackId` = 틱 값에서 계산 (`+0x26 ~ +0x44`) · `Flags` 0 (`+0x15`) → 2 (`+0x47 MOV [R8+0x2a8],2`). 이미 포인터가 있으면 (`+0x5`) 건너뜀. 사진 15바이트 0 아님 | 직접 | ntdll | `②… → RtlpInitializeThreadActivationContextStack (호출 +0xb0d) +0x15 ~ +0x47` | `[정적]` sast48 · 정확 · 사진 `P1~P1a` |
| `+0x2c8` `ActivationContextStackPointer` | ② | 위 스택의 주소 = TEB + `0x290` (사진 `0x9b2fa67290`). 맨 마지막에 쓴다 | 직접 | ntdll | `②… → RtlpInitializeThreadActivationContextStack (호출 +0xb0d) +0x52` | `[정적]` sast48 · 정확 · 사진 `P1~P1a` |
| `+0x17b0` `HeapData*` (4B = 바이트 4개) | ② | 한 DWORD 가 아니라 **바이트 넷**을 힙 LFH·VS 가 따로 쓴다: `[+0x17b0]` ← `RtlpLowFragHeapAllocFromContext +0xc3`, `[+0x17b1]` ← `RtlpHpVsContextHandleContention +0x45`, `[+0x17b2]` ← `RtlpLocalInfoAllocFromCache +0x548` · `RtlpLowFragHeapAllocFromContext +0x6de` · `RtlpSubSegmentInitialize +0x29e` (한 칸 +1 하는 링 머리), `[+0x17b3]` ← `…+0x6d2` · `+0x86e` · `+0x415` (`(주소 하위바이트) × 0x1d` 해시). 사진 P1a `0x25700000` = `[b3] 0x25`·`[b2] 0x70`, 이후 `[b2]` 만 `0x74` → `0xba` | 직접 (바이트) | ntdll | `②… → (힙 할당 길) → RtlpLocalInfoAllocFromCache` (지도 d8) 외 | `[정적]` sast48 · 정확 (줄 후보 여섯 곳, **어느 줄이 먼저 실행되나는 `[공백]` D5**) · 사진 `P1~P1a` → 바뀜 `P1a~P1b`·`P1b~P2` |
| `+0x17ee` `SameTebFlags.LoadOwner*` (비트 12) | ② | 켜기: `LdrpDrainWorkQueue +0xd3` (`OR [RAX+0x17ee],0x1000`, 일감 대기열을 비우는 스레드가 "로드 주인" 이 될 때). 끄기: `LdrpDropLastInProgressCount +0x12` (`AND 0xefff`). 부르는 자리 (`LdrpInitializeProcess`): 켬 `+0x1302 · +0x19c1 · +0x1aac · +0x1b8c`, 끔 `+0x1753 · +0x1b40 · +0x1be7`. P1a (`+0x199d`) 에서 0, P1b (`+0x1bb6`) 에서 1, P2 에서 0 이니 짝은 **`+0x1b8c` 켬 → `+0x1be7` 끔**으로 보임 `[추측]` | 직접 | ntdll | `②… → LdrpDrainWorkQueue +0xd3` / `②… → LdrpDropLastInProgressCount +0x12` | `[정적]` sast48 · run24 확인 · 정확 · 사진 `P1a~P1b` → 바뀜 `P1b~P2` (끔) |
| `+0x1250` `LastStatusValue` | ② | 마지막 NTSTATUS. `RtlNtStatusToDosError` 가 **입구에서 무조건** 받은 상태를 TEB 에 저장 (`+0xf MOV RAX,GS:[0x30]` → `+0x1d MOV [RAX+0x1250],ECX`). 사진 P2 `0xc0000034` (STATUS_OBJECT_NAME_NOT_FOUND). **누가 불렀나는 `[공백]`** (KERNELBASE `BaseSetLastNTError` → `RtlSetLastWin32ErrorAndNtStatusFromNtStatus +0x4` 길 후보, D2). 힙 오류 경로도 직접 쓴다 (`RtlAllocateHeap +0x3ed` 등, 오류 때만) | 직접 | ntdll | `RtlNtStatusToDosError +0x1d` (지도 d7 helper; 호출자 `[공백]`) | `[정적]` sast48 · 정확 (줄) · 사진 `P1b~P2` |
| `+0x17c8` `FlsData` | ② | 스레드 FLS 블록 포인터. 처음 `RtlFlsSetValue` 가 불리면 (`TEB+0x17c8 == 0`) 0x58B 블록을 `RtlpFlsHeapAlloc` 으로 만들어 0 으로 채우고 (`+0xc0 ~ +0xf8`) **TEB 에 놓는다** (`+0x104 MOV [RDI+0x17c8],RBX`), 프로세스 FLS 목록에 연결. 사진 P2 `0x25996f3c160` (프로세스 힙 안). 부르는 쪽은 KERNELBASE `FlsSetValue` (= 사용자 CRT 의 `FlsAlloc`/`FlsSetValue`) `[추측]`, 정확한 호출자 `[공백]` (D3) | 직접 | ntdll | `RtlFlsSetValue +0x104` (**지도 밖** — 사용자 코드 → KERNELBASE → ntdll) | `[정적]` sast48 · 정확 (줄) · 사진 `P1b~P2` |

- 칸 수 대조: 7.2 `P0~P1 (5)` · `P1~P1a (5)` · `P1a~P1b (1)` · `P1b~P2 (2)` 이름 열세 개를 표에서 하나씩 찾았다 — 위 표 8줄이 13칸 (`CurrentIdealProcessor` 줄 1줄 = 칸 4개: `CurrentIdealProcessor`·`IdealProcessorValue`·`ReservedPad2`·`IdealProcessor`).
- `P0~P1` 다섯이 `PspUserThreadStartup` 인지: **맞다**. `+0x94 CALL KeUpdateTebSchedulingPropertiesCurrentThread` (`_calls.tsv` ret `+0x99`). 같은 일을 하는 `KiUpdateTebApc` (APC 루틴, `UpdateTebApc` 의 `NormalRoutine`, 3.13) 도 같은 함수를 부르지만 (`+0x4`) 이 길에선 APC 가 아직 안 걸린다 (큐에 넣는 쪽 `KiQueueTebUpdateApc` ← `KiSetIdealProcessorThread +0x10e`·`KiSetAffinityThread +0xf1`, Q78d4).
- 3.3 아는 칸 4 (`NtTib`·`CurrentLocale`·`RanProcessInit`·`SchedulerSharedDataSlot`) 는 안 건드렸다. `CurrentLocale` 는 같은 함수 `PspUserThreadStartup +0x78`, 그 뒤 `+0x94` 가 위 다섯 칸.

### 3. 끝까지 0 112칸 가르기

**답: 0 을 씀 12 · 조건부 안 탐 13 · 아무도 안 씀 28 · 범위 밖 길 59 (ntdll 19 · nt 1 · 다른 DLL 39) = 112.** 판단 기준은 위 한 줄. 사진 확인: 112칸 모두 K2 ~ P2 일곱 장에서 0 (예외 0).

#### 3.1 0 을 씀 (12)

| 칸 | 쓰는 줄 | 비고 |
|---|---|---|
| `SameTebFlags.SafeThunkCall` · `InDebugPrint` · `HasFiberData` · `WerInShipAssertCode` · `SuppressDebugMsg` · `DisableUserStackWalk` · `RtlExceptionAttached` · `SessionAware` · `SkipFileAPIBrokering` (9) | `①A +0x793` (`MOV [TEB+0x17ee],R12W`) | 16비트 값 `R12W = 0x400` 의 **0 비트** — 입력(만들기 플래그)에서 계산한 값을 통째 쓰는 줄이라 "입력값 쓰기" 로 셈 (요청서 3번, run52 DWORD 되쓰기 기준과 같음). 범위 밖에서도 씀 `[추측]`: `InDebugPrint` ← `vDbgPrintExWithPrefixInternalHelper +0xd6 · +0x1bf`, `WerInShipAssertCode` ← `ShipAssert +0x71 · +0x1b5`, `HasFiberData` ← KERNELBASE `ConvertThreadToFiber +0x101` · `ConvertFiberToThread +0x3c`; 나머지 여섯은 쓰는 이름을 못 찾음 |
| `ReservedPad0` · `ReservedPad1` (2) | `①TS… +0x136` | 이상적 프로세서 DWORD 의 그룹 2바이트 = 입력값(그룹 번호) 0. 사진 P1 `0x01010000` 의 앞 2바이트가 0 |
| `HeapWalkContext` (1) | `RtlAllocateHeap +0x3d2` (`MOV GS:[0x1858],R15`, `R15D` 는 `+0x1b XOR` 로 0 고정) | **쓰고 되돌림**: `+0x6a` 가 힙 주소 (`RDI+0x14`) 를 넣었다가 (`MOV GS:[0x1858],R8`) 끝에서 0 으로 되돌린다. 같은 짝이 `RtlpHpReAllocateHeap +0x21 · +0x129`(0), `RtlpHpAllocateHeapSlow`·`RtlpHpTagAllocateHeap` 등. 사진은 할당 도중이 아니라 항상 0 |

- 같은 DWORD 되쓰기로 0 을 쓰는 칸은 이 밖에 없다 (`CrossTebFlags` 되쓰기 줄은 못 찾음).

#### 3.2 조건부 안 탐 (13) — test.exe 가 왜 안 타나

| 칸 | 켜는 줄 | 조건 | test.exe 가 왜 안 탐 |
|---|---|---|---|
| `SameTebFlags.SkipThreadAttach` (비트 3) · `LoaderWorker` (13) · `SkipLoaderInit` (14) | `①A +0x56a` (`OR DX,8`) · `+0x587` (`CMOVZ`) · `+0x599` (`OR R12W,0x4000`) | 만들기 플래그 비트 `0x2` · `0x80` · `0x100` | **코드로 닫혀 있다**: `NtCreateUserProcess +0x1bf TEST [RBP+0x7d8],0xfffffffe` → `+0x1ce JZ` 가 `ThreadFlags` 에 비트 0 말고는 거절 (`0xc000000d`). 그래서 `PspMapThreadCreationFlags` 출력은 비트 0 (→ `0x1`) 뿐이고 `+0xde0 OR 0x10` · `+0xdfd OR 0x60` 만 더해진다. KERNELBASE `CreateProcessInternalW +0x30af` 가 `ThreadFlags` 로 넘기는 `ESI` 는 `+0x6cb LEA ESI,[RAX+1]` 상수 1 (그 뒤 `ESI` 를 바꾸는 줄 못 찾음). 이 셋은 `NtCreateThreadEx` 길 (로더 일꾼, Q79) 에서만 켜진다. 사진 `0x400` |
| `SameTebFlags.ClonedThread` (비트 6) · `IsImpersonating` · `MuiImpersonation` | `①A +0x2cabac ~ +0x2cabb2` (`MOV R9D,0x40` → `OR AX,R9W`) · `+0x2cab81` (`[+0x179c] = EDI = 0`) · `+0x2cab87` (`[+0x17e8] = EDI = 0`) | **복제 갈래**: 사용자 스택 인자 (`[RSP+0x70]`) = 0 이고 `EPROCESS.PicoContext` (+0x640) = 0 (`+0x5ab ~ +0x5b3 → +0x2ca9a9`) | 사용자 스택 인자 ≠ 0 → 일반 길 (`PspSetupUserStack` `+0x5ea`), 사진 `Teb` 가 `MmCreateTeb` 결과. (같은 갈래가 `SchedulerSharedDataSlot` := 0 도 씀, 3.3 아는 칸) 범위 밖에서도 씀: `IsImpersonating`·`MuiImpersonation` ← 커널 `PspWriteTebImpersonationInfo +0xc0 · +0xc6` (가장하기, `.asm` 있음), ntdll `InitializeTEBUserLangList +0x191` · `RtlpInitializeUserList +0x209` |
| `WowTebOffset` | `①TB… +0x1ca` (EDI = 5) · `+0x271` (EDI = 6) | `EPROCESS.WoW64Process ≠ 0` (`+0x58 ~ +0x6a`) | x64 네이티브 exe → `WoW64Process = 0` → EDI = 4. (`PspAllocateThread +0x79b CMP [R15+0x310]` 도 같은 조건) |
| `ExtendedFeatureDisableMask` | `KeCopyXfdMaskToTeb +0x96` (`MOV [RSI+0x1848],RAX`) ← `KeInitThread +0x49a` | `KeFeatureBits` 비트 `0x37` (`KeInitThread +0x2d3`, `KeCopyXfdMaskToTeb +0x33`) 켜짐 + `KTHREAD.Teb ≠ 0` (`+0x42`) + 시스템 스레드 아님 (`KeInitThread +0x48d TEST [MiscFlags],0x400`). 값 = `~param_2[0x40] & KUSD+0x3d8 & 0x40000` (AMX 비트만) | **탔는지 / 탔는데 값 0 인지 `[공백]`** (둘 다 사진 0) — sast46 D1 (`KeFeatureBits` 비트 `0x37`, `KUSER_SHARED_DATA+0x3d8`) 이 같은 것 |
| `LastErrorValue` | ntdll 오류 경로 `RtlAllocateHeap +0x401` · `RtlFreeHeap +0x255 · +0x832` · `RtlpReAllocateHeap*` · `RtlSizeHeap +0x69b` · `RtlDebug*Heap`, 그리고 `RtlRestoreLastWin32Error +0x1c` (= `RtlSetLastWin32Error`) | 힙 호출이 실패할 때 / 저장했던 값과 **다를 때만** (`+0x17 CMP [RAX+0x68],ECX; JZ`) | 초기화 길에서 힙 실패 없음 · 오류 값이 안 변함 (사진 P2 까지 0). 범위 밖에서도 씀 `[추측]`: KERNELBASE `TlsGetValue +0x49` · `FlsGetValue +0x2f` (성공 때 0), `CreateProcessInternalW +0x6dd3` |
| `HardErrorMode` | `RtlpCheckRelativeDrive +0x15f` (← 0x10) · `+0x1ba` (이전 값 `EBX` 되씀) | 경로가 **드라이브 상대 경로** (`C:file`) 일 때만 (지도 d8) | test.exe 길에 드라이브 상대 경로가 있나는 `[공백]` — 쓰면 0x10 을 넣고 되돌려 0 으로 끝남 (사진 0). 범위 밖에서도: `RtlSetThreadErrorMode +0x36` |
| `HeapFlsData` | `RtlpFlsSetValue +0xf4` (`MOV [RDI],RBX`, 슬롯 포인터 인자 = `&TEB.HeapFlsData`; `.asm` 없음) ← `RtlpHpEnvFlsSetValue` ← `RtlpHpEnvTlsSetValue` (지도 d10) ← `RtlpHpLfhThreadDataInitializeSet` 등 | 세그먼트 힙 LFH 가 스레드별 데이터를 만들 때 | 사진 P2 까지 0 → 안 탔다 `[관찰 간접]`. 범위 밖에서도 씀: `RtlpHpEnvFlsCleanup +0x57 · +0x6d` (정리 표시 1) |
| `TlsSlots` (512B) · `TlsExpansionSlots` | `RtlTlsAlloc +0x168` (새 슬롯 0 비움 `AND [..+0x1480],0`) · `+0x1ef` (확장 배열 포인터), `RtlTlsAlloc` 부르는 곳 `RtlpHpEnvTlsAlloc +0x58` (지도 d8) | `TlsAlloc` 이 불릴 때 / 슬롯 번호가 64 이상일 때만 확장 | 초기화 길에서 `RtlTlsAlloc` 이 불리는지 `[공백]` (불렸다면 `TlsSlots` 는 0 을 씀 쪽, 사진 0). 범위 밖에서도 씀: `RtlTlsSetValue +0x28` (`TlsSlots[i]`) · `+0x91`, `LdrShutdownThread +0x260` |

- 결과: 위 표가 **13칸** (`SkipThreadAttach`·`LoaderWorker`·`SkipLoaderInit` 3 + `ClonedThread`·`IsImpersonating`·`MuiImpersonation` 3 + `WowTebOffset` 1 + `ExtendedFeatureDisableMask` 1 + `LastErrorValue` 1 + `HardErrorMode` 1 + `HeapFlsData` 1 + `TlsSlots` 1 + `TlsExpansionSlots` 1). 갈래를 안 탔는지 / 탔는데 값이 0 인지 정적으로 못 가르는 칸: `ExtendedFeatureDisableMask`(D1) · `HardErrorMode`·`TlsSlots`·`TlsExpansionSlots` (D4 — 탔어도 0 으로 끝나는 칸).

#### 3.3 아무도 안 씀 (초기화 길 안) (28)

`EnvironmentPointer`, `CountOfOwnedCriticalSections`, `FpSoftwareStatusRegister`, `SystemReserved1`, `PlaceholderHydrationAlwaysExplicit`, `PlaceholderReserved`, `Padding0`, `InstrumentationCallbackSp`, `InstrumentationCallbackPreviousPc`, `InstrumentationCallbackPreviousSp`, `InstrumentationCallbackDisabled`, `UnalignedLoadStoreExceptions`, `Padding1`, `Padding2`, `Padding3`, `TlsLinks`, `Vdm`, `Padding4`, `Instrumentation`, `Padding5`, `Padding6`, `ChpeV2CpuAreaInfo`, `Unused`, `Padding7`, `CrossTebFlags`, `CrossTebFlags.SpareCrossTebBits`, `EffectiveContainerId`, `Padding8`

- 근거: 커널 `MmCreateTeb` 64비트 갈래 13줄과 `PspAllocateThread` · `PspUserThreadStartup` · `KiUpdateTeb…` · `KeCopyXfdMaskToTeb` 에 이 칸들이 없고, ntdll 지도 함수 TEB 훑기 (`gs:[0x30]` 추적 + 인자 TEB 후보 눈 확인) 에도 이 오프셋을 쓰는 줄이 없다. 값 0 은 **새로 커밋된 TEB 페이지**에서 온다 (위 판단 기준).
- 이 안의 사정: `Padding0 ~ Padding8` 9칸은 정렬용 빈 칸 (쓰는 코드 없음). `Instrumentation*` 5칸 (`InstrumentationCallbackSp·PreviousPc·PreviousSp·Disabled`, `UnalignedLoadStoreExceptions`) 은 OS 가 읽기만 하고 **사용자가 설치한 계측 콜백**이 쓰는 칸으로 보인다 `[추측]` (커널 `KiSetupForInstrumentationReturn` 은 트랩 프레임 `Rip`/`R10` 을 바꿀 뿐 TEB 를 안 쓴다 — `objdump` 로 확인). `Vdm`(옛 NTVDM)·`FpSoftwareStatusRegister`·`EnvironmentPointer`·`SystemReserved1` 은 옛 칸. `ChpeV2CpuAreaInfo` 는 ARM64 전용. `TlsLinks`·`CountOfOwnedCriticalSections`·`Instrumentation`·`EffectiveContainerId` 는 이름만 보면 쓸 코드가 있을 법한데 nt·ntdll·KERNELBASE·KERNEL32 에서 쓰는 줄을 못 찾았다 `[공백]` (훑기 한계 안).
- `MmCreateTeb` 의 EDI = 6 갈래가 쓰는 `Vdm` (`+0x24b`) 는 **둘째 TEB** 의 `Vdm` 이다 (64비트 TEB 의 `Vdm` 은 읽기만).

#### 3.4 범위 밖 길에서 씀 (59)

**ntdll (19)** — 지도 밖 함수 (objdump 근거, `.asm` 없음):

| 칸 | 쓰는 함수 (오프셋) |
|---|---|
| `PlaceholderCompatibilityMode` | `RtlSetThreadPlaceholderCompatibilityMode +0x19` (커널에도 같은 이름 `+0x33`) |
| `ProxiedProcessId` | `RtlSetProxiedProcessId +0x14` |
| `WorkingOnBehalfTicket` | `RtlSetThreadWorkOnBehalfTicket +0x55`, `RtlClearThreadWorkOnBehalfTicket +0x59`, `TpWorkOnBehalfSetTicket +0x6f`, `Tpp*Callback*` (스레드 풀). 커널 `ExpWorkerFactoryFinishDeferredWork +0x4a` |
| `DbgSsReserved` | `DbgUiSetThreadDebugObject +0x9` (`+0x16a8`). KERNELBASE `SaveProcessHandle +0x62`·`SaveThreadHandle +0x69`·`RemoveHandles +0x80` |
| `ActivityId` | `EtwEventActivityIdControl +0x2a · +0xfc`, `RtlpTpWorkCallback +0x107`, `TppAlpcpExecuteCallback +0x27a` 등. 커널 `EtwActivityIdControl` |
| `SubProcessTag` | `RtlSetThreadSubProcessTag +0x2e`, `Tpp*` |
| `WaitingOnLoaderLock` | `RtlpWaitOnCriticalSection +0x5f · +0x258` (임계 구역 경합 때; `RtlEnterCriticalSection` 은 지도 helper) |
| `ThreadPoolData` | `TppAllocThreadData +0x6e`, `TppFreeThreadData +0x37` |
| `MuiGeneration` | `RtlGetThreadPreferredUILanguages +0x91c · +0xaa0 · +0xaea` |
| `CurrentTransactionHandle` | `RtlSetCurrentTransaction +0xf` |
| `ActiveFrame` | `RtlPushFrame +0x14`, `RtlPopFrame +0xd` |
| `PreferredLanguages` | `RtlSetThreadPreferredUILanguages +0x2b3 · +0x2c8` |
| `UserPrefLanguages` | `RtlpUpdateTEBLanguage +0xdb`, `RtlpInitializeUserList +0x2d4` |
| `MergedPrefLanguages` | `InitializeTEBUserLangList +0x2b3`, `RtlSetThreadPreferredUILanguages +0x31d` |
| `LockCount` | `RtlLockCurrentThread +0x84`, `RtlUnlockCurrentThread +0x23` |
| `ResourceRetValue` | `LdrpSearchResourceSection_U +0xd63`, `RtlCleanUpTEBLangLists +0xb6` |
| `LastSleepCounter` · `SpinCallCount` | `RtlDelayExecution +0xfd` / `+0x109`·`+0x6a` |
| `Rcu` | `RtlpRcuCurrentThreadData +0x77 · +0x7d` |

**nt (1)**: `ExceptionCode` ← `KeRaiseUserException +0x63` (`MOV [RAX+0x2c0],ESI`, `RAX = [R14+0xf0]`; `.asm` 있음). 부르는 곳 `NtClose +0x230`·`ObpCloseHandle +0x20e` 등 (잘못된 핸들 예외).

**다른 DLL (39)** — 쓰는 DLL 짐작 `[추측]` (이름·objdump 근거, 깊이 안 팜):

- `gdi32` (GDI, 6칸): `GdiTebBatch`, `GdiCachedProcessHandle`, `GdiClientPID`, `GdiClientTID`, `GdiThreadLocalInfo`, `GdiBatchCount` (+ `Win32ClientInfo` 아래)
- `user32` (3): `Win32ClientInfo`, `User32Reserved`, `UserReserved`. `win32kbase.sys` (GUI 변환, 1): `Win32ThreadInfo`
- `opengl32` (8): `glDispatchTable`, `glReserved1`, `glReserved2`, `glSectionInfo`, `glSection`, `glTable`, `glCurrentRC`, `glContext`
- `ws2_32` (1): `WinSockData`. `rpcrt4` (2): `ActiveRpcHandle`, `ReservedForNtRpc`. `ole32`/`combase` (4): `ReservedForOle`, `TxnScopeEnterCallback`, `TxnScopeExitCallback`, `TxnScopeContext` (ntdll `TppWorkerThread` 은 읽기만). `advapi32`/`sechost` (2): `PerflibData`, `EtwTraceData`. CSR (1): `CsrClientThread` (ntdll 에 `CsrNewThread` 가 없음)
- WOW64 (1): `WOW32Reserved` (`wow64cpu`; KERNELBASE `Wow64SetThreadDefaultGuestMachine +0x33` 도 씀)
- KERNELBASE (4, 쓰는 줄 확인): `StaticUnicodeBuffer` (`BaseCopyStream +0x39e …`, `FatalAppExitA +0x49`), `NlsCache` (`GetCurrentNlsCache +0x1021 · +0x1096`, `CompareStringW +0xcc7` …), `GuaranteedStackBytes` (`SetThreadStackGuarantee +0x120`, `SwitchToFiberContext +0x112`), `SavedPriorityState` (`SetThreadPriority +0x1b7 · +0x271`)
- 이름만으로 짐작 (6): `ReservedForPerf` · `ReservedForCodeCoverage` · `ReservedForWdf` · `ReservedForCrt` · `ReservedForDebuggerInstrumentation` · `pShimData` (apphelp)

- 위 묶음 합 = gdi32 6 + user32 3 + win32kbase 1 + opengl32 8 + ws2_32 1 + rpcrt4 2 + ole 4 + advapi32 2 + CSR 1 + WOW64 1 + KERNELBASE 4 + 이름만 6 = **39** (`Win32ClientInfo` 는 user32 에 한 번만 셈).

### 4. 간접 호출

**답: 이 길에서 TEB 칸을 쓰는 간접 호출은 0개** — 읽은 `.asm` 36개 (커널 14: `MmCreateTeb`·`PspAllocateThread`·`PspUserThreadStartup`·`KiUpdateTebSchedulingPropertiesCurrentThread`·`KeUpdateTebSchedulingPropertiesCurrentThread`·`KiUpdateTebApc`·`KeCopyXfdMaskToTeb`·`PspSetupUserStack`·`RtlCreateUserStack`·`PspMapThreadCreationFlags`·`PspWriteTebImpersonationInfo`·`KeRaiseUserException`·`KiQueueTebUpdateApc`·`MmDeleteTeb`, ntdll 22) 에서 `CALL reg`·`CALL [mem]`·`JMP reg`·`cfg:` 를 찾아 커널 **0개**, ntdll **1개**: `_LdrpInitialize +0x9a CALL RAX` (`g_LdrpWow64LdrpInitialize`, `UseWOW64 ≠ 0` 갈래 — x64 test.exe 는 안 탄다, TEB32 를 쓰는 쪽). → Q35 에 넣을 것 없음 (이 갈래가 x64 에서 안 타는 것만 메모).

### 5. 줄기와 경로

- **줄기** (칸 값을 정하는 함수까지 끝까지): 커널 `PspAllocateThread` (플래그 계산 + `+0x793`), `MmCreateTeb`, `PspSetupUserStack → RtlCreateUserStack` (`INITIAL_TEB`), `KiUpdateTebSchedulingPropertiesCurrentThread`; ntdll `_LdrpInitialize`, `LdrInitializePerThreadRng → RtlInitializeTRng`, `RtlpInitializeThreadActivationContextStack`, `LdrpAllocateTls`, `LdrpDrainWorkQueue`/`LdrpDropLastInProgressCount`, 힙 할당기 (`RtlAllocateHeap` + LFH 함수들), `RtlNtStatusToDosError`, `RtlFlsSetValue`.
- **줄기 밖 (이름만)**: `KiStackAttachProcess`/`KiUnstackDetachProcess`, `MiAllocateFromSubAllocatedRegion`(서브할당·커밋), `PsGetSessionLocaleId`, `PspNotifyThreadCreation`·`PspInitializeThunkContext`(TEB 안 씀 — `[RBX+0xf0]` 로 TEB 를 읽는 줄이 없다), `LdrpGetNewTlsVector`, `LdrpGenRandom`, `RtlpFlsHeapAlloc`, 범위 밖 표의 함수 전부.
- 칸마다 경로·근거는 위 1·2번 표 (3장 양식), 0 칸 묶음은 3.1 ~ 3.4.

## 동적분석으로 확인해야 할 것

"사진" = 이미 있는 덤프 (`ms/logs/04_baggage-read_*.txt`, `ms/dump/03_baggage-snap/`) 로 답할 수 있는지. 번호는 이 조각 안 번호 (D1~D6). **dast 요청 필요: D2, D3** (콜스택이 필요한 둘). 나머지는 낮음.

| # | 질문 | 추천 멈춤점 / 찍을 것 | 사진으로? |
|---|---|---|---|
| D1 | `ExtendedFeatureDisableMask` (TEB `+0x1848`) 갈래를 탔나 — `KeFeatureBits` 비트 `0x37` 과 `KUSER_SHARED_DATA+0x3d8` 의 비트 18 (`0x40000`) | `dq nt+0xfc6b70 L1` (`KeFeatureBits`), `dd 0xfffff780000003d8 L1` — sast46 D1 과 **같은 읽기** | **가능** (전역은 덤프에 있음) → Q79. 이것으로 `_KTHREAD`·`_TEB` 두 `ExtendedFeatureDisableMask` 칸이 같이 닫힘 |
| D2 | TEB `LastStatusValue` = `0xc0000034` 를 쓴 호출자 (P1b~P2 사이) — `LdrpInitializeGraphRecurse` 아래 DLL 시작 코드인가, KERNELBASE `BaseSetLastNTError` 인가 | `bp ntdll!RtlNtStatusToDosError ".if (@ecx == 0xc0000034) { k; dd @$teb+0x1250 L1 } .else { gc }"` — 조건: 현재 프로세스 test.exe, P1b (`LdrpInitializeProcess +0x1bb6`) 뒤 | 불가 (호출자는 덤프에 없음). 중간 우선순위 |
| D3 | TEB `FlsData` 를 처음 놓는 `RtlFlsSetValue` 의 호출자 (사용자 CRT → KERNELBASE `FlsSetValue` → ntdll 인가) | `bp ntdll!RtlFlsSetValue ".if (poi(@$teb+0x17c8)==0) { k } .else { gc }"` — 조건 test.exe, P1b 뒤 | 불가 (콜스택). 중간 우선순위 |
| D4 | 초기화 길에서 `RtlTlsAlloc`·`RtlpHpEnvTlsAlloc` 이 불리나 (`TlsSlots`·`TlsExpansionSlots`·`HeapFlsData` 가 조건부 안 탐 / 탔는데 0) | `bp ntdll!RtlTlsAlloc` 와 `bp ntdll!RtlpHpEnvTlsAlloc` 의 hit 수 — NtContinue 전까지 | 불가 (호출 여부). 낮음 (사진 P2 `HeapFlsData` 0 이 간접 증거) |
| D5 | `HeapData` 바이트를 처음 쓰는 줄 (`RtlpLocalInfoAllocFromCache +0x548` 외 여섯 곳 중 어느 것) | `ba w1 @$teb+0x17b2` / `+0x17b3` 를 `LdrpInitializeProcess` 입구부터 걸고 `r rip` | 불가. 낮음 |
| D6 | `LoadOwner` 켬/끔 짝이 `+0x1b8c` ↔ `+0x1be7` 인가 | `ba w2 @$teb+0x17ee` 를 `LdrpInitializeProcess +0x199d` 부터, 맞으면 `k` 의 `LdrpInitializeProcess+0x1b91` · `+0x1bec` | 불가. 낮음 (사진 P1a/P1b/P2 값은 이미 일치) |

- 디버거가 붙어서 달라지는 점: 없음 (모든 근거가 정적 + 이미 찍은 사진). Q35 (간접 호출) 에 새로 넣을 것: 없음.

## Q78d4 로 넘길 것 (지나가며 본 줄)

- `KiQueueTebUpdateApc` (rva `0x3bc0f4`) 호출자 `KiSetIdealProcessorThread +0x10e` · `KiSetAffinityThread +0xf1` → `UpdateTebApc` 가 걸리면 `KiUpdateTebApc → KiUpdateTebSchedulingPropertiesCurrentThread` 로 `P0~P1` 다섯 칸을 첫 스레드 시작 뒤에 다시 쓴다. 큐에 넣는 조건·시점은 Q78d4 (`KeStartThread`·스케줄러 쪽).
- `KeStartThread` 쪽에서 `_KTHREAD.UserAffinity`·`UserAffinityPrimaryGroup`·`UserIdealProcessor` 가 최종으로 정해지는 줄 → `P0~P1` 칸 입력의 마지막 출처 (sast46 은 `KeInitThread` 에서 처음 값만).
- `MmDeleteTeb` (rva `0x976370`, 종료 쪽) 은 이번에 뽑아 두기만 했다 — TEB 해제 (Q80 이후).

## 정정 · 새로 알게 된 것 (flow-writer 용)

1. **3.3 `NtTib` 줄에 커널 쓰는 줄이 빠져 있다.** `NtTib` 안쪽 `StackBase` (`+0x8`) · `StackLimit` (`+0x10`) · `Version` (`+0x20` = `0x1e00`) · `Self` (`+0x30` = TEB 주소) 를 `MmCreateTeb` 가 쓴다 (`+0x12a · +0x132 · +0xc1 · +0xc5`; 사진 K2 에서 `NtTib` 12바이트가 이미 0 아님). 지금 줄은 ntdll `ArbitraryUserPointer` 만 적었다 → 줄 하나 더 (① nt, `①TB… +0xc1 ~ +0x132`, 정확, 사진 `K1~K2`). `ExceptionList` (`+0x0`) 는 64비트 갈래가 안 쓴다 (EDI = 5 갈래만 `+0x1d8`).
2. **3.3 머리 메모 (run52·run53) 정리**: `PspAllocateThread +0x793` 의 `.asm` 줄·`+0x54a ~ +0x5a5` 비트 계산은 메모 그대로 맞다. 보탬 — 첫 스레드 값은 늘 `0x400` (`NtCreateUserProcess +0x1bf` 가 `ThreadFlags` 비트 0 만 허용 + `+0xdfd OR 0x60`). `MmCreateTeb +0x175 · +0x17e` 는 `_TEB32.RealClientId` (3.13 쪽 "EtwSupport 오인" 메모와 이어짐).
3. **`MmCreateTeb` 의 메모리 쓰기 줄은 모두 50** (64비트 13 · x86 WOW64 19 · EDI = 6 17 · 나머지 1), run11 "약 47줄" 과 같은 묶음. test.exe 는 13줄만.
4. **TEB 의 0 은 `memset` 이 아니라 새 페이지**: `MmCreateTeb`·`MiAllocateFromSubAllocatedRegion` 에 `memset` 호출이 없다 (`RtlFindClearBitsAndSetEx` → `MiCommitExistingVad`).
5. **`RngState` 는 ntdll 이 씨를 뿌린다** (커널 `_ETHREAD.RngState` 와 별개): `LdrpInitializeProcess +0x153 LdrInitializePerThreadRng → RtlInitializeTRng` (splitmix64, 앞 16B). 사진 `P1~P1a` 와 맞다. `_ETHREAD.RngState` (3.13) 와 이름이 같아도 다른 칸이다.
6. **`HeapData` 는 DWORD 하나가 아니라 바이트 4개**를 서로 다른 LFH·VS 함수가 쓴다 (3.8 힙 전역과 같은 계열). 7.2 의 한 칸 `HeapData*` 로 두되 줄은 바이트 단위.
7. **`LastStatusValue` 는 `RtlNtStatusToDosError` 입구가 무조건 쓴다** — 이 함수를 부르는 모든 길이 TEB 를 덮는다 (상태 0 이어도). 그래서 `LastStatusValue` 를 쓰는 "한 줄" 은 `+0x1d` 이고 호출자는 사슬이 길다. 에뮬레이터는 이 함수를 구현하면 TEB 칸이 따라 맞는다.
8. **`FlsData` (스레드 FLS) 와 `HeapFlsData` (힙용 FLS) 는 같은 코드 모양의 두 갈래**: `RtlFlsSetValue +0x104` (TEB 직접) vs `RtlpFlsSetValue +0xf4` (슬롯 포인터 인자). 둘 다 첫 쓰기에서 0x58B 블록을 만든다.
9. 3장 "근거 줄" 에 줄 수가 늘어나는 곳: `_TEB` 4 → 이 결과서 1·2번 표 18줄 + `NtTib` 보탬 1줄 = 약 23줄 (0 을 씀·조건부 25칸은 묶음 줄, flow-writer 가 정함). `②` 줄 중 `LoadOwner` 는 run24 에서 이미 정적 확인 (`LdrpDrainWorkQueue +0xd3`, `LdrpDropLastInProgressCount +0x12`) — 이번에 부르는 자리를 더했다.

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **고칠 버그는 없다.** `run_ghidra.sh export` 여섯 번 (각 3~4초), `pdb_struct.py` (`_TEB`·`_TEB32`)·`pdb_aliases.py --where` 문제없이 돌았다.
- **제안 1 (sast/43·44·46·47 제안을 이어서)**: `scripts/field_writers.py <바이너리> --struct _TEB`. 이번에도 임시 스크립트 (scratchpad: `tebscan2.py`·`tebscan3.py`·`tebcell.py`·`inmap.py`·`callers.py`·`photo.py`·`cells.py`·`classify.py`) 를 짰다. 합치면 좋은 기능: (a) **분기·레이블 합치기가 있는 레지스터 추적** — 선형으로만 훑으면 `pop r14; ret` 뒤 레이블에서 `gs:[0x30]` 를 잃어 `_LdrpInitialize +0xea` 같은 줄이 빠졌다 (첫판 `tebscan.py` 가 그랬다). (b) **인자로 받은 구조체 포인터 따라가기** — `RtlpInitializeThreadActivationContextStack(RCX=TEB)`, `RtlpFlsSetValue(슬롯 포인터)` 처럼 TEB 를 인자로 받는 함수는 호출 자리에서 인자를 거슬러 올라가야 한다. (c) **`.asm` 이 있으면 그 `+0x` 로 바로 바꿔 쓰기** (objdump 주소 → 함수 + 오프셋 → `.asm` 대조).
- 제안 2: `pdb_struct.py` 가 `union` 겹침 칸 (`CurrentIdealProcessor`·`IdealProcessorValue`·`ReservedPad0~2`·`IdealProcessor`) 을 "같은 바이트 묶음" 으로 한 줄에 보여 주는 옵션 (sast/47 제안 2 와 같음).
- 제안 3 (사소): `ExportFunctions` 가 이미 `.asm` 이 있는 함수는 건너뛴다고 알려 주면 좋다 (이번에도 정규식으로 새 것만 골랐다).

## 이름 확인 결과

이 결과서에 적은 함수 이름을 `ms/decomp/{ntoskrnl,ntdll}/_index.tsv`(와 KERNELBASE `_index.tsv`)에 대 봤다 (scratchpad 스크립트 대신 `awk`/`grep` 로 핵심 이름 확인). 커널 이름 전부 있음 (`MmCreateTeb` 0x9318c4 · `PspAllocateThread` 0x84296c · `PspUserThreadStartup` 0x910de0 · `KeUpdateTebSchedulingPropertiesCurrentThread` 0x3127e8 · `KiUpdateTebApc` 0x312820 · `KiUpdateTebSchedulingPropertiesCurrentThread` 0x312838 · `KiQueueTebUpdateApc` 0x3bc0f4 · `PspWriteTebImpersonationInfo` 0x84fe50 · `RtlCreateUserStack` 0x938db0 · `MiAllocateFromSubAllocatedRegion` 0x931c04 · `KeRaiseUserException` · `KiSetupForInstrumentationReturn` 0x4592e0). ntdll 이름 전부 있음 (`RtlpInitializeThreadActivationContextStack` 0xecb30 · `LdrInitializePerThreadRng` 0x8c328 · `RtlInitializeTRng` 0xecc70 · `RtlFlsSetValue` 0x9e9f0 · `RtlpFlsSetValue` 0x9eb80 · `RtlpHpEnvFlsSetValue` 0x9f0a8 · `RtlpHpEnvTlsSetValue` 0x9f030 · `RtlTlsAlloc` 0x9ed40 · `RtlTlsSetValue` 0x9ef70 · `RtlNtStatusToDosError` 0x723d0 · `LdrpAllocateTls` 0x104a0 · `LdrpDrainWorkQueue` 0xad400 · `LdrpDropLastInProgressCount` 0xae14c …). **`_index.tsv` 에 없는 것**: 구조체 칸 이름·레지스터·명령어, 전역 `KeFeatureBits`·`KiGlobalState`·`LdrpTlsBitmap` (PDB 이름으로 `.asm` 주석에 나옴, `_index.tsv` 대상 아님), `UpdateTebApc`·`StartAddress` 같은 3.13 칸 이름. 별칭: `RtlRestoreLastWin32Error` = `RtlSetLastWin32Error` (같은 주소, `.asm` 주석 `ALIASES(1)`), `KiUpdateTebApc` 는 `pdb_aliases.py --where` 로 PDB 이름 확인 (rva 0x312820 단독). `FUN_140843198` 은 Ghidra 가 이름을 못 붙인 `PspAllocateThread` 조각 (`+0x82c` 에서 시작).

## 판정

**분석성공.** 요청서 질문 1~5 모두 답했다. 133칸 → 아는 칸 21 (nt 13 · ntdll 8 · 다른 DLL 0) · 0 을 씀 12 · 조건부 안 탐 13 · 아무도 안 씀 28 · 범위 밖 길 59 (ntdll 19 · nt 1 · 다른 DLL 39) · 여전히 모름 0. **PDB 일치**: ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1`, ntdll `C3093720177DA6851519DCFFE8DF53FE1` — 둘 다 기준·사진 로그와 같고, 알려진 사실 호출 지점 (`+0xc36`·`+0x807`·`+0xe`·`+0x5a`·`+0x16c`·`+0x1bbb`·`+0x16b/+0x19c`) 이 모두 맞다; 사진 K2 ~ P2 TEB 값이 칸마다 코드와 일치. 정적으로 못 닫은 것: `ExtendedFeatureDisableMask` "탔나 / 값 0" (D1, 사진으로 닫힘), `LastStatusValue`·`FlsData` 의 **호출자** (D2·D3, 콜스택 필요), `HeapData` 가 처음 써지는 줄 (D5), `TlsSlots`·`TlsExpansionSlots`·`HardErrorMode` 의 갈래를 탔나 (D4), "범위 밖 길" 59칸·"아무도 안 씀" 28칸의 근거가 전체 훑기 (TEB 인자 전달·인덱스 쓰기 누락 가능, 오프셋 숫자 일치 오탐 가능)라는 점. 몸통 `memset` 이 없어 요청서 셈 문구와 run50·52 셈의 차이는 이번엔 숫자에 영향이 없다.

# 135 wow64-kernel — 정적 분석 결과

- 요청서: `_workspace/sast/135_wow64-kernel.md` (원문 `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` C 절 1 ~ 10)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/ntoskrnl.exe`)
- **PDB GUID 확인**: `ms/sym/ntoskrnl.exe.pdb.info.txt` 의 `guid_age=C29EBFB06B78B3C020DCA66D99713F9E1` = 기준 `C29EBFB06B78B3C020DCA66D99713F9E1` **같다** (빌드 일치). 다시 analyze 안 함 (`.gpr` + `_index.tsv` 34982줄 + analyze 로그 `SetPdb:` 줄 다 있음).
- 뽑은 함수 (모두 `^이름$` export, depth 0 — 지도로 길을 보고 필요한 것만 추가):
  - 요청서가 지정한 8개: `MiInitializeWowPeb` · `PspWow64SetupUserProcessAddressSpace` · `PspWow64ThunkProcessParameters` · `PspWow64SetupUserStack` · `RtlpWow64CreateUserStack` · `PspWow64InitThread` · `PspWow64InitThreadGuestx86` · `PspWow64SetupCpuArea`
  - 길을 따라 더 뽑은 것 (이유: 일이 한두 단계 아래에서 일어나서): `MiCreatePebOrTeb` · `MiAllocateFromSubAllocatedRegion` · `MiAllocateNewSubAllocatedRegion` (PEB32·TEB 자리, Q4·Q8) · `MiGetUserReservationHighestAddress` (Q8) · `MiGetImageWowSubsystemIndex` (Q8) · `MiFindEmptyAddressRange` · `MiSelectUserAddress` (Q8 읽는 곳 줄) · `PspSetupUserStack` (Q7 — CPU 영역을 자르는 `PspWow64SetupCpuArea` 의 호출자) · `PsWow64IsMachineSupported` · `PspQueryComPlusRunUnderWoW` · `PsGetProcessMachine` (Q1) · `RtlpArchContextFlagFromMachine` · `RtlWow64GetCpuAreaEnabledFeatures` (Q7) · `entry` (= PDB 이름 `KiSystemStartup`, Q9) · `MmMapApiSetView` · `KeCopyXfdMaskToPeb` · `PspMapSiloSharedDataView` · `PspUpdatePebForAffinityChange` · `PspWritePebAffinityInfoAttached` · `PspTrySetProcessPebThrottlingFlags` · `PspGetStandardHandleList` · `MmInitializeProcessAddressSpace` (PEB32 를 쓰는 다른 곳 — "다음 요청 후보" 용)
  - 이미 있던 asm (읽기): 요청서 7행 그대로 + `PspSetupUserProcessAddressSpace` · `PspPrepareSystemDllInitBlock` · `MiComputeProcessUserVa` · `NtCreateUserProcess` · `PspBuildCreateProcessContext` · `MmCreatePeb`
  - 위치: `ms/disassemble/ntoskrnl/<함수>.asm`, `ms/decomp/ntoskrnl/<함수>.c`, 호출 표 `ms/disassemble/ntoskrnl/_calls.tsv`
- 구조체 칸 이름: PDB (`pdb_struct.py`) — `_EWOW64PROCESS` · `_PEB32` · `_TEB32` · `_TEB` · `_NT_TIB32` · `_EPROCESS` · `_KTHREAD`/`_ETHREAD` · `_KPCR` · `_KGDTENTRY64` · `_PS_NTDLL_EXPORTS` · `_MI_SECTION_WOW_STATE` · `_SECTION_IMAGE_INFORMATION` 는 **있음**. `_WOW64_CPURESERVED` · `_WOW64_CONTEXT` · `_CONTEXT32` · `_SYSTEM_DLL_TYPE` 은 **PDB 에 없음** (이름을 쓰면 표에 `[추측: 공개 자료 이름]` 으로 따로 표시).
- 보조 근거 (결과서에 이름을 적은 것만): objdump (`ntoskrnl.exe` 전체 디스어셈블, 스크래치패드) — Ghidra 가 안 보여 주는 "누가 이 칸을 읽나" 훑기 (`+0x368`, `+0x310`, `sgdt/lgdt`). PE 파일 바이트 직접 읽기 (`NtdllWowX86Exports` 이름 8개, `PspSystemDlls` 칸 값, `MiWowSubsystems6432`). 둘 다 임시 파이썬·objdump 이고 레포에 안 둠.
- **버전 확인 (알려진 사실)**: `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치 · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv`). 그리고 sast/50 에서 이미 본 `PspSelectMachineForProcess +0x54 ~ +0xd4` (x64 이미지면 출력 `+2` 바이트 0) 가 이번 읽기와 같다. → **일치.** 스크립트 오프셋 계산도 믿을 만함 (`MiComputeProcessUserVa +0xa8` 을 objdump 주소 − 함수 시작으로 다시 계산해 같은 값).

## 스크립트 문제

없음. 참고 한 가지: `PspSelectMachineForProcess.asm +0x108` 의 `alias:?GetStoragePropertyPost…+37` 은 reference.md 가 경고한 ICF 가짜 이름이다. 원본 바이트 (`objdump 0x140490960`) 는 `xor eax,eax; ret` (3바이트) 라서 **항상 0 을 돌려주는 빈 함수**로 읽었다.

---

## 먼저 읽는 한 줄 답 (원문 C 1 ~ 10)

| # | 한 줄 답 |
|---|---|
| 1 | **x86 (`0x14c`) 이미지는 `PspSelectMachineForProcess` 가 `PsWow64IsMachineSupported(0x14c)` 가 참일 때 WoW64 로 정한다.** 출력 4바이트: `+0` 기계 (`0x14c`) → `EPROCESS.Machine`(+0x6ec) · `+2` "WoW64 칸 필요" 1 → `'WowP'` 할당 · `+3` `OverrideAddressSpace` (x86 이미지가 .NET AnyCPU 라 64비트로 돌 때만 1). 못 지원하면 `0xc000007b`. `Wow64VaSpace4Gb` 는 이 함수가 아니라 `PspAllocateProcess +0x1015` 와 `MiComputeProcessUserVa +0xf0` 이 켠다. |
| 2 | **`'WowP'` = 비페이지 풀 0x10 바이트 (`_EWOW64PROCESS`: `+0x0 Peb` · `+0x8 NtdllType`).** `Peb` 는 처음엔 **1** (`+0x100b`), 나중에 `MiInitializeWowPeb +0x78` 이 PEB32 주소로 덮는다. `NtdllType` 은 `PspWow64PickBestNtdll` 결과 (복제 길이면 부모 값). `Machine` 은 이 구조체가 아니라 `EPROCESS +0x6ec`. |
| 3 | **예.** x86 이면 칸 번호 **1**. `PspSystemDlls[1]` = `PspWowX86SystemDllData` (플래그 `0x8` · 기계 `0x14c`, PE 파일 바이트로 확인). `PspWow64PickBestNtdll` 의 일반 반복 (`+0x0f ~ +0x2e`) 이 `i=1` 에서 맞는다. `PsWow64GetProcessNtdllType` 은 `[WoW64Process+8]` 을 읽을 뿐. |
| 4 | **PEB32 = `MmCreatePeb +0x67b` 가 64비트 PEB 를 다 쓴 뒤 `MiInitializeWowPeb` 를 불러 만든다.** 자리는 프로세스 안 **"서브 할당 영역"** (2MB VAD `'VadS'`) 의 한 페이지 (`MiCreatePebOrTeb(1)` → `MiAllocateFromSubAllocatedRegion`). 영역 자체가 `HighestUserAddress` (WoW64 x86 이면 `0xffff0000` 또는 `0x7fff0000`) 아래라 4GB 아래. 64비트 PEB 와 같은 영역에서 **다음 칸** 이라 보통 PEB64 바로 옆 (`[추측]`: 사이에 다른 할당이 없을 때). 쓰는 칸 27개 (아래 표). `_EWOW64PROCESS.Peb` ← PEB32 주소 (`MiInitializeWowPeb +0x6c ~ +0x78`) **적는다.** |
| 5 | **32비트 매개변수 블록을 새로 만든다.** `PspWow64ThunkProcessParameters` 가 새 프로세스 안에 `ZwAllocateVirtualMemory` (최고 주소 = `HighestUserAddress − 1`, `+0x85`) 로 블록을 잡고, 64비트 `RTL_USER_PROCESS_PARAMETERS` 를 32비트 모양으로 칸마다 옮긴다 (정수 칸은 하위 32비트, 문자열은 새 블록 뒤쪽에 복사, `RuntimeData` 핸들표는 9바이트 → 5바이트 항목). 결과는 `PEB32 +0x10 ProcessParameters` (`+0x761`) 와 문맥 `[+0xe8]` (`+0x76c`). 32비트 `LdrSystemDllInitBlock` 은 **따로** (`PspSetupUserProcessAddressSpace +0x214` → `PspPrepareSystemDllInitBlock(1)`), 서로 안 읽는다. |
| 6 | **TEB32 = TEB64 + `0x2000`**, `MmCreateTeb +0x14a ~ +0x1d8` 대입 19줄을 `_TEB32` 칸 이름으로 풀었다 (표). 칸 값은 ClientId · PEB32 · `InitialTeb` 의 32비트 스택 세 칸 · TEB64 주소 (`GdiBatchCount` 칸) 에서 온다. `PspAllocateThread +0x793 ~ +0x7e3`: `SameTebFlags` 값을 TEB64 `+0x17ee` 와 TEB32 `+0xfca` 둘 다에 쓴다 (값이 0 이면 건너뜀). |
| 7 | **32비트 스택: 크기 = (매개변수 → 없으면 PE32 `SizeOfStackCommit`/`Reserve` → 없으면 commit `0x4000`), 자리 = `ZwSetInformationProcess(ProcessThreadStackAllocation)` 가 정함 (4GB 아래). `StackBase32 = base+reserve`, `StackLimit32 = top−commit`, 그 아래 `0x2000` 가드.** 64비트 스택은 WoW64 스레드에서 따로 reserve `0x40000` · commit `0x8000` 로 만들고, 그 맨 위에서 **CPU 영역** (x86 `CONTEXT` + 확장) 을 잘라 낸다 (`PspWow64SetupCpuArea`). CPU 영역 주소는 **TEB64 `TlsSlots[1]` (`+0x1488`)** 에 쓴다 (`PspWow64InitThread +0xae`). 첫 32비트 문맥: `Eip` = 32비트 `RtlUserThreadStart` (`PsWowX86SharedInformation +0x20`), `Eax` = 문맥64 `Rcx` (시작 주소), `Ebx` = 문맥64 `Rdx`, `Esp` = `StackBase32 − 랜덤(0~0x7fc) − 0x10`, `CS 0x23 · SS/DS/ES/GS 0x2b · FS 0x53`, `EFlags 0x202`. 64비트 쪽 문맥은 읽기만 하고 안 바꾼다. |
| 8 | **4GB 상한의 뿌리는 `EPROCESS.HighestUserAddress` (`+0x368`).** `MiComputeProcessUserVa` 가 x86 WoW64 에 `0xffff0000` (LAA) / `0x7fff0000` (LAA 아님) 로 쓰고 (`Wow64VaSpace4Gb` 는 LAA 일 때 켬), 읽는 곳: `MiGetUserReservationHighestAddress +0x12` → PEB/TEB 서브 영역 (`MiAllocateNewSubAllocatedRegion +0x8b`) · `MiAllocateVirtualMemoryPrepare +0x3ac/+0x517` · `MiSelectUserAddress +0x9d` · `MiFindEmptyAddressRange +0x2c1` · `MiInsertVad +0x5c` · `MmMapViewOfSection +0xdf/+0x198` · `NtMapViewOfSection +0x1d1/+0x555` 외. 플래그 비트 자체를 읽는 곳은 `MiComputeProcessUserVa +0x137` 하나뿐. 이미지는 `MiSelectImageBase` 가 x86 이면 ESI=3 → WoW 전용 겹침표 (`0x140e2d7a8`) 와 천장 `0x78000000`. |
| 9 | **GDT 칸 4 · 10 의 바이트를 쓰는 줄은 ntoskrnl 에 없다 (로더 몫 `[공백]`), 커널이 만지는 건 두 가지 + SYSRET 용 STAR 한 줄.** (가) `entry` (= `KiSystemStartup`) `+0xe1/+0xe5`: 부팅 CPU (Number 0) 만 `GDT+0x50` 의 한계 하위 16비트 = **`0x3c00`**. (나) `SwapContext` 가 **스레드마다** 칸 10 (`GDT+0x50`) 의 **베이스** (`+0x52` 워드, `+0x54`·`+0x57` 바이트) 를 새로 쓴다 (`+0x538 ~ +0x542`): WoW64 이면 `KTHREAD.Teb + 0x2000` (= TEB32), 아니면 `ETHREAD.UserFsBase` 하위 32비트. 그 뒤 `FS=0x53` 을 올리고 `FS_BASE MSR` 을 `UserFsBase` 로 되돌린다 (`+0x545 ~ +0x55d`). 나가는 스레드가 WoW64 면 MSR 을 `UserFsBase` 에 저장 (`+0x138 ~ +0x14f`). 칸 4 · 5 · 6 의 순서는 `KiInitializeBootStructures +0x42e` 의 `STAR = 0x0023_0010_…` 이 정한다. |
| 10 | **32비트 코드 (CS `0x20~0x27`) 에서 난 예외도 64비트 `KiUserExceptionDispatcher` 로 간다** (`PsNtdllExports +0x20`, `KiDispatchException +0x58c`, 트랩 프레임 `CS = 0x33` `+0x580 ~ +0x583`). WoW64 전용 손질: `+0x5d1 ~ +0x5e9` WoW64 프로세스면 `+0x74f` 로 → `0x80000002` (정렬) 이고 `EFLAGS.AC` 가 켜져 있으면 AC 를 끄고 계속; CS 가 `0x20` 대면 `0x80000003 → 0x4000001f`, `0x80000004 → 0x4000001e` 로 바꾸고 `Rsp` 를 `& 0xfffffff0`. |

(B 4 도 겸사로 닫힘: 32비트 ntdll 이름 8개 — 아래 "표 B-0".)

---

## 질문별 답 [정적]

### 1 ~ 3. 기계 고르기 · `'WowP'` · 칸 번호

**Q1. `PspSelectMachineForProcess`** (rva `0xa2bb9c`, 호출 `PspAllocateProcess +0xa87/+0xa8c`)

- 인자 `[정적]`: `RCX` = 부모 프로세스 (`R13`), `RDX` = `CreateInfo` (`RBP`), `R8` = 출력 4바이트 (`[RSP+0x80]`, 아래).
  - `CreateInfo +0x38` 부터는 `MiSectionImageInformation` 으로 채워짐: `PspAllocateProcess +0x986/+0x98c` `LEA R8,[R15+0x38]; CALL MmGetSectionInformation(…, 4, …)`. 그래서 `[+0x68]` = `Machine` (`SECTION_IMAGE_INFORMATION +0x30`), `[+0x6b]` = `ImageFlags` (`+0x33`: 비트0 `ComPlusNativeReady`, 비트1 `ComPlusILOnly`, 비트5 `ComPlusPrefer32bit`).
  - `[+0xa8]` (워드) · `[+0x1e8]` (dword) 는 NtCreateUserProcess 의 속성 값: `PspBuildCreateProcessContext +0x1723 ~ +0x1732` (크기 2 → `+0xa8`), `+0x16fa ~ +0x1716` (크기 4, 값 0~7 → `+0x1e8`). 속성 **번호** 는 안 찾음 `[공백]`.
- **보통 x86 exe 의 길** (`.NET` 아님, 속성 없음, 부모 x64): 
  1. `+0x15 R15W = Machine (0x14c)`; `+0x54/+0x58 CMP R15W,0x14c; JZ +0x11c` → `+0x11c ~ +0x123` `ImageFlags & 3 == 3` 이면 `R14B = 1` (.NET 길, 아래). 아니면 그냥 진행.
  2. `+0x5e` `[RDX+0xa8]` = 0 → `+0x76` `PsGetProcessMachine(부모)` = `0x8664` → `+0x8f JZ` 로 `+0xa3` 으로 건너뜀 (`BX` = 0).
  3. `+0xac ~ +0xb7` 고른 기계 `BX` = (요청·부모에서 온 `BX` 가 0 이 아니면 그것, 아니면 이미지 기계) = `0x14c`. 이미지와 같으니 `+0xba JNZ` 안 탐.
  4. `+0xc0 CMP BX,0x8664` 다르니 `+0xf0 ~ +0xf8` **`PsWow64IsMachineSupported(0x14c)`**. 0 이면 `+0xfc MOV EAX,0xc000007b` (= `STATUS_INVALID_IMAGE_FORMAT`) 로 끝. 아니면 `+0x103 DIL = 1` → `+0xd1 [out] = BX`, `+0xd4 [out+2] = DIL (1)`, `+0xd8 [out+3] = CL (0)`.
  - `PsWow64IsMachineSupported` (`PsWow64IsMachineSupported.asm +0x0 ~ +0x45`): 기계가 0 이거나 `RtlWow64GetEquivalentMachineCHPE(기계) ≠ 기계` 면 0. 아니면 시스템 DLL 종류 0 ~ 5 를 돌며 `PsQuerySystemDllInfo(i)` 의 첫 dword 가 `& 8` 이고 상위 워드(기계) 가 같은 칸이 있으면 1 → **`PspWowX86SystemDllData` (플래그 `0x8`, 기계 `0x14c`) 가 채워져 있어야 x86 이 허용**된다.
- **x64 exe**: `+0x54` 에서 `JZ` 안 타고 → `BX = 0x8664` → `+0xc0 CMP BX,DX` 같음 → `+0xc5 ~ +0xd8` 쓰기: 기계 `0x8664`, `+2` = 0, `+3` = 0 (sast/50 과 같음).
- **.NET AnyCPU (x86 이미지인데 `ImageFlags & 3 == 3`)**: `+0xa3 JNZ → +0x137` 흩어진 범위 `+0x10a83a ~ +0x10a8e2`. 고른 기계 `BX` 가 0 이면 `0x8664` (양쪽 갈래 다 `0x8664` — x64 빌드에서 `if` 가 같은 값으로 접힘). `Prefer32bit` (`+0x10a842 TEST [RBP+0x6b],0x20`) 이거나 기계가 `0x14c`/`0x1c4` 면 "32비트로 돌림" (`DIL = 1`). 아니면 `PspQueryComPlusRunUnderWoW` (`+0x10a860/+0x10a865`; IFEO `ZwQuerySystemInformation(0x3b)` 값 등) 가 "WoW 로 돌려라" 를 말하면 `DIL = 1`, 아니면 `+0x10a87a [RSP+0x78] = 1` → **출력 `+3` = 1 (`OverrideAddressSpace`)**, 기계 `0x8664`, WoW 없음.
- 명시 요청 기계 (`[CreateInfo+0xa8]` ≠ 0) 인데 이미지 기계와 다르면 `+0x108/+0x10d` 의 빈 함수 (항상 0) 를 거쳐 `+0x111 TEST SIL` → 요청이 속성으로 온 것이면 `0xc000007b`, 아니면 이미지 기계로 되돌림 (`+0x116 MOVZX EBX,R15W`).
- 출력 → `PspAllocateProcess`: `+0xaa2` 기계 → `[RSP+0xcc]` → **`+0x1043 [EPROCESS+0x6ec] = 기계`** 와 `+0xfef MOVZX EDX,R11W` (`PspWow64PickBestNtdll` 인자). `+0xaa9/+0xab3` `+2` 바이트 ≠ 0 → `OR EBX,0x40` (지역 플래그 `0x40`). `+0xac5/+0xac7` `+3` 바이트 ≠ 0 → `BTS [RSP+0xb0],0x11` (= `EPROCESS.Flags.OverrideAddressSpace`, 비트 17; 나중에 `+0x13ef ~ +0x13f6 OR [R14+0x1f4],EAX` 로 EPROCESS 에 합쳐짐).
- **`Wow64VaSpace4Gb` (`Flags +0x1f4` 비트 9) 를 켜는 곳** (이 함수 아님): `PspAllocateProcess +0x100e BT R12D,0x17; +0x1015 OR.LOCK [R14+0x1f4],0x200` — `R12D` = `PspAllocateProcess` 의 `Flags` 인자 (스택 `[RSP+0x8b0]`; 이 비트 `0x800000` 은 `NtCreateUserProcess +0x1b6 TEST EBX,0xff317838` 거름망을 통과하는 **허용 비트** 이지만 이름은 `[공백]`). 또 `MiComputeProcessUserVa +0xf0` (Q8).

**Q2. `'WowP'` (`PspAllocateProcess +0xfc4`, ret `+0xfc9`)**

- 조건: `+0xfad TEST BL,0x40; +0xfb0 JZ` — `BL` 의 `0x40` 은 (가) 복제 길에서 부모 `[+0x310] ≠ 0` (`+0x92a OR EBX,0x40`) 또는 (나) `PspSelectMachineForProcess` 출력 `+2` ≠ 0 (`+0xab3`) 일 때만 켜진다. test.exe (x64) 는 둘 다 아니라 안 탄다 (sast/50 `[정적]` 과 같음).
- 호출: `ExAllocatePool2(Flags = 0x40, Size = 0x10, Tag = 0x50776f57 'WowP')` — `+0xfb6 EDX=0x10; +0xfbb LEA ECX,[RDX+0x30]` (=0x40 = `POOL_FLAG_NON_PAGED`); `+0xfbe R8D=0x50776f57`. 초기화 안 함 플래그가 없어서 0 으로 채워진다 `[소스: ExAllocatePool2 공개 의미]`.
- `+0xfcc [R14+0x310] = RAX` → **`EPROCESS.WoW64Process`**.

| `_EWOW64PROCESS` 칸 | 값 | 쓰는 줄 | 값의 출처 |
|---|---|---|---|
| (풀) | 0x10 바이트 · 비페이지 · 태그 `WowP` · 0 채움 | `PspAllocateProcess +0xfc4` | `[정적]` |
| `+0x0 Peb` (`void*`) | **1** (임시) | `+0x100b MOV [RAX],R13` (`R13 = 1`: `+0x6b5 MOV R13D,0x1` 뒤 이 길에서 안 바뀜) | 상수 `[정적]` (R13 을 눈으로 추적 — 약간 `[추측]`) |
| `+0x0 Peb` | **PEB32 주소** | `MiInitializeWowPeb +0x6c ~ +0x78` `RAX=[RSI+0x310]; RDX=[RSP+0x30]; MOV [RAX],RDX` | `MiCreatePebOrTeb(1)` 출력 |
| `+0x8 NtdllType` (`_SYSTEM_DLL_TYPE`, dword) | **`PspWow64PickBestNtdll(CreateInfo, 기계)`** 결과; `[RSP+0x118] ≠ 0` (복제 길에서 부모 `[+0x310]+8`, `+0x937`) 이면 그 값 | `+0xfdc ~ +0xff6` 고르기, `+0xffb MOV [R10+8],EAX` | `[정적]` |
| (기계) | `Machine` 은 이 구조체에 **없다** (`_EWOW64PROCESS` 크기 0x10) → `EPROCESS +0x6ec Machine` | `+0x1043` | Q1 |

**Q3. `PspWow64PickBestNtdll` · `PsWow64GetProcessNtdllType`**: **예, x86 이면 1.**

- `PspWow64PickBestNtdll(CreateInfo, DX=기계)` (`PspWow64PickBestNtdll.asm`):
  - `CreateInfo ≠ 0` 이고 `[CreateInfo+0x11]` 최상위 비트가 **꺼져 있으면** (`+0x3a CMP byte [RCX+0x11],0; +0x3e JGE +0xd`) 일반 반복 `+0xf ~ +0x38`: `i = 0..5` 에서 `PsQuerySystemDllInfo(i)` → 첫 dword `& 8` 이고 `[정보+2]` (기계) = 인자 기계면 `i` 를 돌려줌.
  - 비트가 켜져 있으면 두 번째 반복 `+0x43 ~ +0x71`: 플래그 `0xc` 둘 다 켜진 종류만 (x86 칸은 `0x8` 뿐이라 안 맞음 → 0). 이 비트 뜻 `[공백]` (x86 시험 exe 에선 꺼져 있을 것 `[추측]`).
  - 못 찾으면 `+0x73 XOR EAX,EAX` = 0.
- PE 파일 바이트로 확인: `PspSystemDlls` (rva `0xfd8130`) = `{0xfd8980 (Native), 0xfd8940 (WowX86), 0, 0, 0xfd89c0, 0}`; 정보 첫 dword: Native `0x86640013` (기계 `0x8664`, 플래그 `0x13`), **WowX86 `0x014c0008` (기계 `0x14c`, 플래그 `0x8`)**. 반복 `i=0` Native (기계 `0x8664` ≠ `0x14c`) 건너뜀, `i=1` WowX86 맞음 → **1**.
- `PsWow64GetProcessNtdllType(Process)` (`PsWow64GetProcessNtdllType.asm +0x0 ~ +0x11`): `RAX = [Process+0x310]`; 0 이면 0, 아니면 `[RAX+8]`. `PspWow64GetSharedInformation(종류)` (`PspWow64GetSharedInformation.asm +0x0 ~ +0xe`): 종류 1 이면 `&PsWowX86SharedInformation` (rva `0xfda900`), 아니면 0.

### 4 ~ 5. PEB32 · 32비트 매개변수

**Q4. `MiInitializeWowPeb`** (rva `0x930784`, 호출 `MmCreatePeb +0x67b/+0x680`)

- 인자: `RCX` = `MmCreatePeb` 의 지역 값 묶음 (`[RSP+0x70]` 부터: `+0x0` Subsystem · `+0x4/+0x8` 부시스템 버전 · `+0xc/+0x10` OS 메이저/마이너 · `+0x14` OS 플랫폼 · `+0x18` 빌드 · `+0x1a` CSD · `+0x20/+0x28` 힙 예약/커밋 · `+0x3c` SessionId · `+0x60/+0x61` 템플릿 바이트 · `+0x68` 뮤턴트 · `+0x70` 이미지 베이스 · `+0x78/+0x7a/+0x7c` 코드페이지 · `+0x80/+0x88` 윤초), `RDX` = exe 의 NT 헤더 (`RtlImageNtHeader`), `R8` = EPROCESS, `R9` = 16바이트 출력 (친화도 마스크·그룹; `MiInitializeWowPeb` 안 `[RSP+0x88]` 로 보관).
- `MmCreatePeb +0x598 CMP [R13+0x310],0; +0x5a0 JNZ → +0x66b` → **64비트 PEB 를 다 쓴 뒤** (`+0x43f ~ +0x57a`) WoW64 프로세스면 이 함수를 부른다. 이 함수는 `MmCreatePeb` 가 이미 PEB64 에 쓴 값을 PEB32 에도 쓰는 것이 대부분이다.
- **자리 고르기 `[정적]`**:
  - 시작 갈래: `+0x303 ~ +0x321` 기계가 `0x14c` 또는 `0x1c4` 면 `+0x369` 로: `EPROCESS.SectionObject (+0x2a8)` → `MiSectionControlArea` (`+0x379/+0x37e`) → 이미지 섹션 맞는지 (`[CA+0x38] & 0x20`) → `RtlImageDirectoryEntryToData(SectionBaseAddress(+0x2b0), 10 = LOAD_CONFIG)` (`+0x3a9/+0x3ae`) 로 **PE32 형 load config 를 다시 읽어** 친화도 마스크 (`[+0x30]`) 와 `CSDVersion` (`[+0x34]`) 을 32비트 자리에서 가져온다 (`+0x3ee ~ +0x4c4`). 거기 이어서 `+0x404 ~ +0x469` NT 헤더에서 부시스템·`Win32VersionValue` 로 OS 버전을 다시 계산.
  - 힙 크기 손질 (`+0x33c ~ +0x35e`, `+0x4d1` 의 `0x100000`): `HeapSegmentReserve > 0x40000000` 이면 `0x100000` 으로, `HeapSegmentCommit > Reserve` 면 `0x2000` 으로.
  - **`+0x469 MOV ECX,EDI; +0x46b JMP +0x58` → `MiCreatePebOrTeb(ECX=1, …, R8=&[RSP+0x30])`** (`+0x5d/+0x62`). 종류 1 = PEB32 (`MiCreatePebOrTeb.asm +0x4d: EDX = 0x4b8`, 쪽 올림 `0x1000`). (종류 0 = PEB64 `0x7d8`, 2 = `0x800`, 3/4 = `0x1878` → 올림 `0x2000`, 5 = `0x3038` → `0x4000`, 6 = `0x3878` → `0x4000`.) `MiCreatePebOrTeb` 는 `+0x25 MiAllocateFromSubAllocatedRegion` 하나로 위임.
  - `MiAllocateFromSubAllocatedRegion`: 프로세스 `[+0x410]` (= `EPROCESS.Vm.VmWorkingSetList`, `_MMSUPPORT_INSTANCE +0x10`) 안 목록 (`+0x408` 머리) 에서 `RtlFindClearBitsAndSetEx(비트표, 페이지수, 힌트 = [칸+0x20]>>2)` (`+0x9a ~ +0xa5`, ret `+0xaa`) 로 빈 페이지를 찾고 다음 힌트를 `뒤쪽` 으로 옮긴다 (`+0xb7 ~ +0xcd`). 없으면 `MiAllocateNewSubAllocatedRegion` (`+0x1e6/+0x1eb`) — **`'VadS'` VAD 한 개, 기본 0x200 페이지 (2MB), 실패하면 절반씩 줄여 0x10 페이지까지** (`MiAllocateNewSubAllocatedRegion +0x1f ~ +0x25`, `+0x212 ~ +0x236`). 새 영역의 첫 힌트는 `ExGenRandom` (`+0x186/+0x18b`) 로 영역 안 아무 페이지 (`%` 0x200, `+0x1a4 SHL EDX,2`).
  - **주소 상한**: `MiAllocateNewSubAllocatedRegion +0x8b/+0x90 MiGetUserReservationHighestAddress(Process)` → `MiSelectUserAddress` (`+0xff/+0x104`) 의 최고 주소 인자. `MiGetUserReservationHighestAddress.asm +0x12 ~ +0x1c`: `min(0x7ffffffeffff, EPROCESS.HighestUserAddress − 1)` (`ZeroBits` 마스크가 있으면 `-1 >> ZeroBits` 도). `HighestUserAddress` 는 Q8.
  - 정리: PEB64 · PEB32 · TEB 들은 **구분 없이 같은 서브 영역에서** 순서대로 잘려 나가므로 (종류는 크기만 정함) (가) 4GB 아래인 것은 영역이 `HighestUserAddress` 아래이기 때문이고 (나) PEB32 는 PEB64 의 **다음 페이지** (영역 안에서 빈 칸을 위쪽으로 찾는 next-fit; 사이에 다른 할당이 끼면 아님, 영역이 가득 차면 새 영역) `[추측: 거리]`. `MmCreatePeb +0x146` 이 PEB64 를 먼저, `MiInitializeWowPeb +0x5d` 가 그 뒤.
- **`EPROCESS.WoW64Process` (`_EWOW64PROCESS.Peb`) 에 PEB32 주소를 적는다**: `+0x6c RAX=[RSI+0x310]; +0x73 RDX=[RSP+0x30]; +0x78 MOV [RAX],RDX` (Q2 표).

**PEB32 에 쓰는 칸 (`MiInitializeWowPeb +0xb8 ~ +0x1b4`, x86/ARMNT 갈래)** — `RDX` = PEB32 주소, `RBX` = 위 지역 값 묶음. 이름은 PDB `_PEB32`.

| 칸 (오프셋 · PDB 이름) | 값 | 출처 | 쓰는 줄 |
|---|---|---|---|
| `+0x0 InheritedAddressSpace` | 템플릿 `[RBX+0x60]` 바이트 | `MmCreatePeb` 의 2번째 인자 템플릿 `[RDI+0]` | `+0x167` |
| `+0x3 BitField` | 템플릿 `[RBX+0x61]` | 템플릿 `[RDI+3]` | `+0x16c` |
| `+0x4 Mutant` | `[RBX+0x68]` (하위 32비트; 보통 `-1`) | 템플릿 `[RDI+8]` | `+0x172` |
| `+0x8 ImageBaseAddress` | `[RBX+0x70]` 하위 32비트 | `EPROCESS.SectionBaseAddress` (+0x2b0) | `+0x178` |
| `+0x64 NumberOfProcessors` | `KeNumberProcessors` | 전역 | `+0xf2 ~ +0xf8` |
| `+0x68 NtGlobalFlag` | `NtGlobalFlag` | 전역 | `+0xfb ~ +0x101` |
| `+0x70 CriticalSectionTimeout` (8바이트) | 전역 `rva 0xe2db90` (이름 `[공백]`) | 전역 | `+0x110 ~ +0x117` |
| `+0x78 HeapSegmentReserve` | `[RBX+0x20]` (손질됨, 위) | `MmRegistryState+0x48` 계열 | `+0xe6 ~ +0xe9` |
| `+0x7c HeapSegmentCommit` | `[RBX+0x28]` | `MmRegistryState+0x40` 계열 | `+0xec ~ +0xef` |
| `+0x80 HeapDeCommitTotalFreeThreshold` | `MmRegistryState+0x38` | 전역 | `+0x127 ~ +0x12e` |
| `+0x84 HeapDeCommitFreeBlockThreshold` | `MmRegistryState+0x30` | 전역 | `+0x134 ~ +0x13b` |
| `+0xa4 OSMajorVersion` | `[RBX+0xc]` | 기본 10, `Win32VersionValue` 로 덮음 | `+0xb8` |
| `+0xa8 OSMinorVersion` | `[RBX+0x10]` | 기본 0 | `+0xc1` |
| `+0xac OSBuildNumber` (워드) | `[RBX+0x18]` | 서버 사일로 전역 `+0x538` (26100) | `+0xcb` |
| `+0xae OSCSDVersion` (워드) | `[RBX+0x1a]` | `CmNtCSDVersion`, load config `[+0x34]` | `+0xdf` |
| `+0xb0 OSPlatformId` | `[RBX+0x14]` | 기본 2 | `+0xd5` |
| `+0xb4 ImageSubsystem` | `[RBX]` | exe 헤더 `+0x5c` | `+0x14a ~ +0x14c` |
| `+0xb8 ImageSubsystemMajorVersion` | `[RBX+4]` | 헤더 `+0x48` | `+0x152 ~ +0x155` |
| `+0xbc ImageSubsystemMinorVersion` | `[RBX+8]` | 헤더 `+0x4a` | `+0x15b ~ +0x15e` |
| `+0x1d4 SessionId` | `[RBX+0x3c]` | `PsGetProcessSessionId` | `+0x141 ~ +0x144` |
| `+0x208 MinimumStackCommit` | `MmRegistryState+0x28` | 전역 | `+0x11b ~ +0x121` |
| `+0x228 ActiveCodePage` · `+0x22a OemCodePage` · `+0x22c UseCaseMapping` (워드) | `[RBX+0x78/0x7a/0x7c]` | 서버 사일로 `+0x408/+0x448`, `0x6a5` | `+0x17f · +0x18a · +0x195` |
| `+0x470 LeapSecondData` | `[RBX+0x80]` 하위 32비트 | `ExLeapSecondDataSectionPointer` 뷰 | `+0x19c ~ +0x1a2` |
| `+0x474 LeapSecondFlags` | `[RBX+0x88]` | 0 | `+0x1a8 ~ +0x1ae` |
| `+0x478 NtGlobalFlag2` | `NtGlobalFlag2` | 전역 | `+0x104 ~ +0x10a` |

- 같은 함수가 **쓰지 않는** 칸 (0 이거나 다른 곳에서): `ProcessParameters +0x10` (아래 Q5), `ApiSetMap +0x38` (`MmMapApiSetView +0xd8`), `SharedData +0x50` (`PspMapSiloSharedDataView +0xcad6a`), `ActiveProcessAffinityMask +0xc0` (`PspWritePebAffinityInfoAttached +0x84`), `ExtendedFeatureDisableMask +0x480` (`KeCopyXfdMaskToPeb +0x245033` 앞 조각), `Ldr`·`ProcessHeap`·`BeingDebugged` 등은 이번에 못 찾음 (ntdll 이 채우거나 `[공백]`).
- 기계가 x86/ARMNT 가 아닌 갈래 (`+0x4f` 시작, 종류 2): 같은 값을 64비트 `_PEB` 모양 칸 (`+0x118` 등) 에 쓴다 (`+0x1b9 ~ +0x2d1`). x86 에서는 안 탄다.

**Q5. `PspWow64SetupUserProcessAddressSpace` · `PspWow64ThunkProcessParameters`**

- 호출: `PspSetupUserProcessAddressSpace +0x1f2/+0x1f7`. 앞에서 `+0x170 ~ +0x19c` 64비트 매개변수 블록을 새 프로세스에 잡아 `[ctx+0xe0]` 에 두고, `+0x1b4 PspCopyAndFixupParameters` 로 채우고, `+0x1e1/+0x1e6 PspPrepareSystemDllInitBlock(0)` 를 부른 **뒤**에 이 함수가 온다 (`ctx = RSI`).
- `PspWow64SetupUserProcessAddressSpace.asm`: `+0x4 R8 = [Process+0x310]` 없으면 끝 (`+0x10 JNZ`). 기계 `0x14c` 또는 `0x1c4` (`+0x25 ~ +0x55`) 일 때만 `+0x2b RCX=[R8] (= PEB32 주소)`, `+0x35 [ctx+0xec] = ECX (PEB32 하위 32비트)`, `+0x3b RDX = [ctx+0xd8]` (**커널이 먼저 잡아 둔 64비트 매개변수 사본** — `+0xe0` 의 유저 사본이 아님), `+0x2e R8 = &[ctx+0xe8]` → `PspWow64ThunkProcessParameters(PEB32, 매개변수64, &[ctx+0xe8])` (`+0x42/+0x47`).
- `PspWow64ThunkProcessParameters` (`PspWow64ThunkProcessParameters.asm`):
  - **크기** (`+0x1f ~ +0x44`): `최대길이(+0x0) + EnvironmentSize(+0x3f0) − 0x188` (`DefaultThreadpoolCpuSetMaskCount(+0x438)` ≠ 0 이면 `−0x184`).
  - **할당** (`+0x5f ~ +0x85`): `R8 = [현재 프로세스+0x368] − 1` (`HighestUserAddress − 1`) 을 `ZeroBits` 로, `MEM_COMMIT|MEM_RESERVE` (`0x3000`), `PAGE_READWRITE` (4) 로 `ZwAllocateVirtualMemory(-1, &base, ZeroBits, &size, …)` (`+0x85/+0x8a`; 이때 `PspSetupUserProcessAddressSpace +0xf5` 로 새 프로세스에 붙은 상태) — **4GB (또는 2GB) 아래에 잡히는 줄**.
  - **칸 옮기기 꼴** (`RDI` = 64비트 `RTL_USER_PROCESS_PARAMETERS`, `RAX` = 새 32비트 블록): `+0xb1/+0xbb` `MaximumLength = Length = 총 크기`; `+0xbe ~ +0x1d3` 정수 칸을 하위 32비트로 (Flags `+8` · DebugFlags `+0xc` · ConsoleHandle `+0x10` · ConsoleFlags `+0x14 ← 64비트 +0x18` · StandardInput/Output/Error `+0x18/+0x1c/+0x20 ← +0x20/+0x28/+0x30` · StartingX…ShowWindowFlags `+0x4c ~ +0x6c ← +0x88 ~ +0xa8` · `LoaderThreads +0x2a0 ← +0x40c` · `DefaultThreadpoolThreadMaximum +0x2bc ← +0x43c`); **문자열 칸**은 길이 (워드 2개) 를 옮기고 버퍼를 **새 블록 `+0x2c8` 이후**에 복사한 뒤 32비트 포인터를 적는다: `CurrentDirectory.DosPath (+0x24 ~ +0x28)` · `CurrentDirectory.Handle (+0x2c)` · `DllPath (+0x30 ~ +0x34)` · `ImagePathName (+0x38 ~ +0x3c)` · `CommandLine (+0x40 ~ +0x44)` · `WindowTitle (+0x70 ~ +0x74)` · `DesktopInfo (+0x78 ~ +0x7c)` · `ShellInfo (+0x80 ~ +0x84)` · `RedirectionDllName (+0x2a4 ~ +0x2a8)` · `HeapPartitionName (+0x2ac ~ +0x2b0)` (복사는 `+0x212 ~ +0x531` 의 `memcpy` 14곳); `DefaultThreadpoolCpuSetMasks (+0x2b4/+0x2b8)` 는 8바이트 항목째 복사 (`+0x4f8 ~ +0x536`); **`RuntimeData (+0x88)`** 는 CRT 핸들 상속표 (`4 + 9×개수` 바이트: 개수, 플래그 바이트들, 8바이트 핸들들) 를 `4 + 5×개수` 바이트 (핸들은 하위 32비트) 로 줄여 옮김 (`+0x5b4 ~ +0x6be`); `Environment` 는 맨 뒤 (`+0x6fb [RAX+0x48] = R14D`, `+0x740` memcpy `EnvironmentSize`), `+0x70d EnvironmentSize (+0x290)` · `+0x721 EnvironmentVersion (+0x294)` · `+0x753 ProcessGroupId (+0x29c) ← 64비트 +0x408`. 32비트 칸 이름·오프셋은 공개 `RTL_USER_PROCESS_PARAMETERS` 32비트 모양과 맞춤 `[추측: 이름]` (PDB 에 `_RTL_USER_PROCESS_PARAMETERS` 는 64비트만).
  - **결과**: `+0x761 MOV [R15+0x10],EAX` → **`PEB32 +0x10 ProcessParameters`**; `+0x76c MOV [R13],EAX` → `[ctx+0xe8]`; 반환 0 (`+0x770`).
- **32비트 `LdrSystemDllInitBlock` 과의 관계**: 서로 **독립**. 순서는 `+0x1e1` (native 블록, 종류 0) → `+0x1f2` (이 함수: 32비트 매개변수 · `PEB32.ProcessParameters`) → `+0x20a PsWow64GetProcessNtdllType` (= 1) → `+0x214/+0x219 PspPrepareSystemDllInitBlock(1, ctx)` (32비트 블록). `PspPrepareSystemDllInitBlock` 종류 1 갈래는 `+0x2d6 ~ +0x313`: `PspWow64GetSharedInformation(1)` 의 `[+0x38]` (= 32비트 ntdll 내보내기 `LdrSystemDllInitBlock` 의 유저 주소, 표 B-0 의 칸 6) 을 블록 주소로 삼고 (`SharedNtdll32LdrSystemDllInitBlock = 7` 번째 칸 = 이 `+0x38`), 첫 dword `Size` 가 `0x128` 인지 검사 (`+0x98 ~ +0x9f`, 아니면 `0xc0000059`), `+0xa5 ~ +0xda` 에 두 ntdll 기본 주소, `+0xde ~ +0xf7` **`PsWowX86SharedInformation` 16칸을 `+0x18` 부터 통째로 복사** (그래서 64비트 블록도 32비트 진입점 표를 갖게 됨 — Process.cpp:1043 주석과 일치), `+0xfb ~ +0x106` 난수 `+0x98`, 이어서 ctx `+0x150 ~ +0x168` · `+0x1c0 ~ +0x1d8` 에서 온 값 (시작 블록 `+0xa0 ~ +0xe8` 칸, 줄 `+0x126 ~ +0x152`) 과 `[프로세스+0x410]` 구조의 `+0x4c0/+0x4c8` (native 는 `+0x4a0/+0x4a8`) 값. 이 구조 칸 이름은 PDB 에 없음 `[공백]`. 매개변수 블록 (프로세스가 처음 읽는 입력) 과 시작 블록 (ntdll 이 읽는 시스템 값) 은 **둘 다 같은 attach 안에서 순서대로** 쓰일 뿐 서로의 주소를 담지 않는다.
  - `[ctx+0xe8]` (32비트 매개변수 주소) · `[ctx+0xec]` (PEB32) 를 읽는 곳은 이번에 안 따라감 `[공백]` (NtCreateUserProcess 가 만든 쪽에 PEB 주소를 돌려주는 자리로 짐작 `[추측]`).

### 6 ~ 7. TEB32 · 스택 · CPU 영역

**Q6. `MmCreateTeb` x86 갈래** (rva `0x9318c4`)

- 갈래 고르기: `+0x58 ~ +0x6a` `R13 = [WoW64Process]->Peb` (PEB32, 위 Q2) ≠ 0 이면 WoW; `+0x2e9 ~ +0x2f3` 기계 `0x14c`/`0x1c4` 면 종류 5 (`EDI=5`, 크기 `0x3038` → `0x4000`), 아니면 6 (`+0x2df`). `+0x85 EDX=0x1878; +0xa7 MiAllocateFromSubAllocatedRegion` (`+0xa7/+0xac`) 로 4 페이지 (`0x4000`) 를 서브 영역에서 받음 — **TEB64 = 영역 안 주소, TEB32 = TEB64 + `0x2000`** (`+0x14a LEA RCX,[RBX+0x2000]`). 인자: `RCX` EPROCESS · `RDX` `InitialTeb` (확장: 64비트 스택 `+0x10/+0x18/+0x20`, 32비트 스택 `+0x28/+0x30/+0x38`) · `R8` ClientId · 5번째 = 출력 TEB64.
- **`+0x14a ~ +0x1db` 대입 19줄 → `_TEB32` 칸** (`RCX` = TEB32 주소, `RBX` = TEB64 주소):

| # | 줄 | 쓰는 칸 (오프셋 · PDB 이름) | 값 | 값의 출처 |
|---|---|---|---|---|
| 1 | `+0x156` | `+0x10 NtTib.FiberData/Version` | `0x1e00` | 상수 (`R8D`; TEB64 `+0x20` 에도 같은 값 `+0xc1`) |
| 2 | `+0x15a` | `+0x18 NtTib.Self` | TEB32 주소 (32비트) | `ECX` |
| 3 | `+0x161` | `+0x24 ClientId.UniqueThread` | `[R8+8]` 하위 32비트 | 인자 ClientId |
| 4 | `+0x167` | `+0x20 ClientId.UniqueProcess` | `[R8]` 하위 32비트 | 인자 ClientId |
| 5 | `+0x16a` | `+0x1d0 TxFsContext` | `0xfffe` | 상수 (TEB64 `+0x2e8` 도 `+0xde`) |
| 6 | `+0x175` | `+0x6b8 RealClientId.UniqueThread` | `[R8+8]` 하위 32비트 | ClientId |
| 7 | `+0x17e` | `+0x6b4 RealClientId.UniqueProcess` | `[R8]` 하위 32비트 | ClientId |
| 8 | `+0x184` | `+0xbfa StaticUnicodeString.MaximumLength` | `0x20a` (522) | 상수 |
| 9 | `+0x192` | `+0xbfc StaticUnicodeString.Buffer` | TEB32 `+0xc00` (= `StaticUnicodeBuffer`) | `LEA EAX,[RCX+0xc00]` |
| 10 | `+0x198` | `+0x0 NtTib.ExceptionList` | `0xffffffff` | `OR [RCX],-1` |
| 11 | `+0x19b` | `+0x30 ProcessEnvironmentBlock` | **PEB32 주소** | `R13D` = `[WoW64Process].Peb` |
| 12 | `+0x19f` | `+0xf70 GdiBatchCount` (PDB 이름. 값이 TEB64 주소라 wow64 가 쌍둥이를 찾는 칸으로 쓸 것으로 짐작 `[추측]`) | TEB64 주소 하위 32비트 | `EBX` |
| 13 | `+0x1ab` | `+0xf18 Vdm` | TEB64 `+0x1690 (Vdm)` 하위 32비트 (지금 0) | `[RBX+0x1690]` |
| 14 | `+0x1b4` | `+0x4 NtTib.StackBase` | `InitialTeb +0x28` | `PspWow64SetupUserStack` 가 채운 32비트 스택 맨 위 |
| 15 | `+0x1ba` | `+0x8 NtTib.StackLimit` | `InitialTeb +0x30` | 커밋된 맨 아래 |
| 16 | `+0x1c0` | `+0xe0c DeallocationStack` | `InitialTeb +0x38` | 예약 시작 |
| 17 | `+0x1ca` | TEB64 `+0x180c WowTebOffset` | `+0x2000` | `ECX − EBX` |
| 18 | `+0x1d2` | `+0xfdc WowTebOffset` (TEB32) | `−0x2000` | `NEG EAX` |
| 19 | `+0x1d8` | TEB64 `+0x0 NtTib.ExceptionList` | **TEB32 주소** (64비트 TEB 가 쌍둥이를 가리킴) | `MOV [RBX],RCX` |

  이 19줄 외에 TEB32 에 쓰는 곳: `PspAllocateThread +0x7db` (`SameTebFlags`, 아래). **TEB32 에 쓰지 않는 것** (0 으로 시작): `WOW32Reserved +0xc0`·`+0xc4` (run121: wow64.dll 이 씀), `TlsSlots`, `ActivationContextStackPointer`, `LastErrorValue` 등. TEB64 쪽 (이 함수 공통부 `+0xb6 ~ +0x13a`): `+0x20 = 0x1e00` · `+0x30 Self` · `+0x40/+0x48 ClientId` · `+0x60 ProcessEnvironmentBlock = EPROCESS.Peb (PEB64)` · `+0x8/+0x10/+0x1478 ← InitialTeb +0x10/+0x18/+0x20` · `+0x2e8 = 0xfffe` · `+0x7d8 RealClientId` · `+0x1258` 문자열 · `+0x180c WowTebOffset`.
- **`PspAllocateThread +0x793 ~ +0x7e3`**:
  - 흐름: `+0x66f TEST R12W,R12W; +0x673 JNZ +0x780` — **`R12W` (= `SameTebFlags` 값) 가 0 이 아닐 때만** 이 블록이 돈다 (`+0x780 ~ +0x78d` 새 프로세스에 붙음).
  - `+0x793 MOV [RBX+0x17ee],R12W` → TEB64 `SameTebFlags`.
  - `+0x79b CMP [R15+0x310],RDI; +0x7a2 JZ +0x7e3` (WoW64 아니면 건너뜀). `+0x7aa LEA RAX,[RBX+0x2000]`; `+0x7b1 R8D = [RSP+0x58]` (같은 플래그 값). `+0x7b7 CMP [R15+0x6ec],0x8664; +0x7bf JNZ +0x7d3`: 기계가 `0x8664` 면 (ARM64 호스트의 x64 WoW 갈래) `+0x7c9 [RAX+0x17ee]`; **아니면 (x86) `+0x7db MOV [RAX+0xfca],R8W` = TEB32 `SameTebFlags`**.
  - `R12W` 값 (`+0x54a ~ +0x5a5`): 스레드 생성 플래그 `R8D` 에서 `(& 0x40) << 4` = `InitialThread` (비트 10) · `& 0x2` 이면 `SkipThreadAttach` (`| 0x8`) · `& 0x80` 이면 `| 0x2000` (`LoaderWorker`) · `& 0x100` 이면 `| 0x4000` (`SkipLoaderInit`).
  - `+0x9eb` (`+0x9e6` 호출 · `PspWow64SetupUserStack`) · `+0xb7d` (`+0xb78` 호출 · `PspWow64InitThread`) 는 Q7.

**Q7. 스택 · CPU 영역**

- **`PspAllocateThread` 안 순서** (WoW64 프로세스, `+0x5d6 CMP [R15+0x310],0; +0x5de JNZ +0x976`): (가) `+0x976 ~ +0x9a9` **64비트 스택**: 지역 매개변수 (`+0x97e [RSP+0x108] = 0x8000` commit, `+0x98a [RSP+0x110] = 0x40000` reserve, ZeroBits 0) 로 `PspSetupUserStack` — 호출자가 요청한 크기를 **무시하고 reserve 256KB · commit 32KB**. (나) `+0x9e6/+0x9eb PspWow64SetupUserStack(Process, X, InitialTeb, X, 선호 노드)` — 호출자 요청 크기는 여기 (32비트 스택) 로. (다) `+0x62c MmCreateTeb`. (라) `+0xb78/+0xb7d PspWow64InitThread(Process, InitialTeb, 문맥64, TEB64)` (`+0x661 CMP [R15+0x310],0; +0x669 JNZ +0xb6a`).
- **`PspWow64SetupUserStack`** (`PspWow64SetupUserStack.asm`): `+0x19 TEST [R9],1; +0x35 JNZ +0x9b` 입력 구조 `X` 첫 바이트 비트 0 (호출자가 스택을 줬다는 뜻 `[추측]`) 이면 건너뜀. 아니면 `+0x3e` 새 프로세스에 붙고 `+0x5c/+0x61 RtlpWow64CreateUserStack(RCX=[X+0x10] commit, RDX=[X+0x18] reserve, R8=[X+8] ZeroBits, R9D=선호 노드, InitialTeb)`; 성공하면 `+0x75 ~ +0x7b X` 첫 바이트 비트 2 (`0x4`) 를 켬 (= 커널이 32비트 스택을 만들었다).
- **`RtlpWow64CreateUserStack`** (`RtlpWow64CreateUserStack.asm`) — 크기:
  - `+0x58 TEST RCX,RCX; +0x5f JNZ` / `+0xb3` : commit 이나 reserve 가 0 이면 `+0x65 ~ +0x83` **exe 의 PE32 헤더** (`RtlImageNtHeader(SectionBaseAddress)`) 를 읽어 `+0x8b EAX = [NT+0x64]` (`SizeOfStackCommit`) · `+0x8e EDX = [NT+0x60]` (`SizeOfStackReserve`) 로 채움 (헤더가 PE32 가 아니면 `+0xc1 0xc000007b`).
  - `+0x11a ~ +0x138`: commit 도 0 이면 **`0x4000`**; commit ≥ reserve 이면 reserve = commit 을 1MB 로 올림 (`+0x12b ~ +0x132`); commit 쪽 올림 `+0xfff` (`+0x138 ~ +0x146`), reserve 는 64KB 올림 (`+0x149 ~ +0x150`).
  - `+0x157 ~ +0x170` · `+0x2c6 ~ +0x2e0`: **새 프로세스 PEB64 `+0x318` (`MinimumStackCommit`)** 가 0 이 아니고 commit 보다 크면 commit 을 그 값으로 올리고 reserve 를 `commit` 쪽 1MB 올림으로.
  - 자리: `+0x1aa/+0x1af ZwSetInformationProcess(-1, 0x29 = ProcessThreadStackAllocation, &{선호노드, ReserveSize = reserve, ZeroBits = R8, 출력 StackBase}, 0x28)` — 주소는 **MM 이 정한다** (`[공백]`: MM 쪽에서 `HighestUserAddress` 에 자르는지는 안 따라감, 아래 Q8 에서 32비트 프로세스의 `HighestUserAddress` 는 4GB 아래라 같은 상한을 쓴다고 짐작 `[추측]`).
  - 채움: `+0x1bf ~ +0x1c7` `InitialTeb +0x38 = base` (AllocatedBase32) · `+0x28 = base + reserve` (**StackBase32**); `+0x1cb ~ +0x1fe` `[top − commit, top)` 을 `ZwAllocateVirtualMemory(MEM_COMMIT 0x1000, PAGE_READWRITE 4)` (`+0x1fe/+0x203`), `+0x20a ~ +0x213 InitialTeb +0x30 = 커밋 시작` (**StackLimit32**); reserve − commit ≥ `0x2000` 이면 그 아래 `0x2000` 을 `0x104` (= `PAGE_GUARD | PAGE_READWRITE`) 로 한 번 더 커밋 (`+0x24d/+0x252`) — **가드 2 페이지**. 실패하면 `+0x2aa ZwFreeVirtualMemory` 로 되돌림.
  - TEB32 로: `NtTib.StackBase ← +0x28`, `StackLimit ← +0x30`, `DeallocationStack ← +0x38` (Q6 표 14 ~ 16).
- **`PspSetupUserStack +0x153/+0x158 PspWow64SetupCpuArea(&InitialTeb.StackBase, Process)`** — 64비트 스택 만든 직후 (`+0x89` `RtlCreateUserStack`) 에 맨 위를 잘라냄. **`PspWow64SetupCpuArea`** (`PspWow64SetupCpuArea.asm`): 기계 → `RtlpArchContextFlagFromMachine` (x86 → `0x10000`, `RtlpArchContextFlagFromMachine.asm +0x0 ~ +0x44`) 이 0 이면 `0xc000000d`; `RtlpGetLegacyContextLength` · `RtlGetExtendedContextLength2` 로 길이 `L` 을 구해 `StackBase = ((((StackBase − 4) & −4) − 5) & −8) − L` 을 정렬 길이로 내림, 다시 `−4`, `& −0x10` (`+0x54 ~ +0x83`). 그 다음 `PspSetupUserStack +0xa7 ~ +0xdc` 가 랜덤 `(ExGenRandom & 0x7f) << 4` (스택 랜덤화 꺼짐 `Process.MitigationFlags & 0x40` 이면 0) 와 `0x28` 을 더 빼서 문맥64 `Rsp` (`[ctx+0x98]`) 를 정한다.
- **`PspWow64InitThread`** (`PspWow64InitThread.asm`, 문맥64 = `RBX`):
  - `+0x4c ~ +0x64`: 기계 (`PsWow64GetProcessMachine`: WoW64Process 없으면 `0x8664`, 있으면 `EPROCESS.Machine`, 0 이면 `0x8664`) → 문맥 플래그 → 0 이면 `0xc000000d`.
  - `+0x6a/+0x71 RSI = 문맥64.Rip; CMP RSI,[PsNtdllExports+0x10]` — **64비트 `RtlUserThreadStart` 와 같으면 (= 보통의 시작)** `+0x14d ~ +0x161 RSI = [PspWow64GetSharedInformation(PsWow64GetProcessNtdllType) + 0x20]` = **32비트 `RtlUserThreadStart`** (표 B-0 칸 4). 다르면 그 `Rip` 를 그대로 `Eip` 로.
  - `+0x7e/+0x85 R12 = 문맥64.Rcx` (문맥 `+0x80`) · `R13 = 문맥64.Rdx` (문맥 `+0x88`); `+0x8c/+0x95` `R15 = InitialTeb +0x10` (**CPU 영역 시작** = 위에서 잘라 낸 StackBase), `[RSP+0x50] = InitialTeb +0x28` (StackBase32).
  - `+0xa3` 새 프로세스에 붙고, **`+0xae MOV [TEB64+0x1488],R15` → TEB64 `TlsSlots[1]` (`_TEB +0x1480 TlsSlots`) 가 CPU 영역을 가리킨다.** `+0xb5 ~ +0xbf [R15] = 기계 << 16` (영역 첫 dword: 하위 워드 플래그 0, **상위 워드 기계 `0x14c`**; `_WOW64_CPURESERVED` 모양 `[추측: 이름]`). `+0xc7 RtlWow64GetCpuAreaEnabledFeatures` (`+0x0 ~ +0x41`: 문맥 플래그에 `0x10000` 이 있으면 `KUSER_SHARED_DATA +0x3d8` XState 기능에서 아키텍처가 금지한 것을 빼고, 남는 게 있으면 플래그 `|= 0x10040`) → `+0xdc/+0xe1 RtlInitializeExtendedContext2(R15+4, 플래그, &문맥EX, 기능)` — **영역 `+0x4` 부터 x86 `CONTEXT` (+ 확장 헤더)**.
  - `+0xe1 CMP EBX,0x14c; +0xe7 JNZ +0x10a` x86 이 아니면 `0xc000000d` (ARMNT 는 여기서 거절). 맞으면 `+0x103/+0x108 PspWow64InitThreadGuestx86(Process, 문맥EX, RSI = EIP, R9 = StackBase32, [RSP+0x20] = R12, [RSP+0x28] = R13)`.
- **`PspWow64InitThreadGuestx86`** (`PspWow64InitThreadGuestx86.asm`): `+0x14 ~ +0x20 RBP = 문맥EX + [문맥EX+8]` (= `CONTEXT_EX.Legacy.Offset` 만큼 떨어진 **x86 `CONTEXT` 본체**). 쓰는 x86 `CONTEXT` 칸 (이름은 공개 `CONTEXT32` — PDB 에 없음 `[추측: 이름]`):

| x86 `CONTEXT` 칸 | 값 | 출처 | 줄 |
|---|---|---|---|
| `+0x0 ContextFlags` | `|= 0x1003f` (`CONTEXT_i386` 전부 + 확장 레지스터) | 상수 | `+0xe4` |
| `+0x4 ~ +0x18` `Dr0..Dr3 · Dr6 · Dr7` | 0 | | `+0xcc ~ +0xe0` |
| `+0x1c FloatSave.ControlWord` | `0x27f` | 상수 | `+0xb8` |
| `+0x24 FloatSave.TagWord` | `0xffff` | 상수 | `+0xbd` |
| `+0x1c ~ +0x8b` 나머지 `FloatSave` | 0 (`memset 0x70`) | | `+0x7e ~ +0xa2` |
| `+0x8c SegGs` · `+0x94 SegEs` · `+0x98 SegDs` · `+0xc8 SegSs` | **`0x2b`** | 상수 | `+0x72 · +0x78 · +0x82 · +0x88` |
| `+0x90 SegFs` | **`0x53`** | 상수 | `+0x8e` |
| `+0xbc SegCs` | **`0x23`** | 상수 | `+0x98` |
| `+0xa4 Ebx` | 문맥64 `Rdx` (`R13`) | Rdx = 시작 매개변수 (WoW64 면 32비트 PEB 값, KxContextToKframes 근거 참고) | `+0x4f ~ +0x59` |
| `+0xb0 Eax` | 문맥64 `Rcx` (`R12`) — 시작 주소 (exe 진입점) | | `+0x3e ~ +0x49` |
| `+0xb8 Eip` | **32비트 `RtlUserThreadStart`** (`PsWowX86SharedInformation +0x20`) 또는 문맥64 `Rip` | `PspWow64InitThread +0x14d` | `+0x6c` |
| `+0xc4 Esp` | **`StackBase32 − 랜덤 (ExGenRandom & 0x1ff) × 4 − 0x10`** (스택 랜덤화 꺼짐 `Process.MitigationFlags & 0x40` 이면 랜덤 0) | `+0x29 ExGenRandom`, `+0x42 TEST [RBX+0x750],0x40`, `+0x56 SUB EDX,0x10` | `+0x64` |
| `+0xc0 EFlags` | `0x202` | 상수 | `+0x108` |
| `+0xcc ExtendedRegisters` (512바이트) | 0 으로 민 뒤 FCW `0x27f` (`+0xeb`) · MXCSR `0x1f80` (문맥 `+0xe4`, 줄 `+0xf8`) · MXCSR 마스크 `0xffff` (문맥 `+0xe8`, 줄 `+0x102`) | 상수 | `+0xac ~ +0x102` |
| `Edi · Esi · Ecx · Edx · Ebp` | 안 씀 (영역이 새 스택 페이지라 0 `[추측]`) | | |

  - 64비트 쪽 문맥과의 관계: **`PspWow64InitThread` 는 문맥64 를 읽기만 한다** (`[RBX+0x80/0x88/0xf8]`). 64비트 `Rip/Rcx/Rdx/Rsp` 는 그대로고 스레드는 **64비트 모드 (CS `0x33`) 로 시작** (`KxContextToKframes +0x9a ~ +0xa7`: 문맥 `SegCs` 가 `0x33` 이면 `0x33`, 아니면 `0x23`; `SegSs = 0x2b` `+0x91`) → 64비트 ntdll `LdrInitializeThunk` → `UseWOW64` → `g_LdrpWow64LdrpInitialize` 가 위 CPU 영역 (`TEB64.TlsSlots[1]`) 을 읽어 32비트로 넘어간다 (그쪽은 A·B 몫).

### 9. GDT

- **칸 4 (`0x20`, 셀렉터 `0x23`) · 칸 10 (`0x50`, 셀렉터 `0x53`) 의 바이트를 ntoskrnl 이 쓰는 줄은 아래 둘뿐**. `KiInitializeBootStructures.asm` 전체 (`+0x0 ~ +0x4c4` 이후 `KiSaveInitialProcessorControlState` 호출까지) 에서도 GDT 칸을 쓰는 줄은 없다 (아래 (다) STAR 만 셀렉터 가정을 정한다). GDT 를 읽는/쓰는 명령 전체를 훑은 결과 (`sgdt/lgdt` 가 든 함수 7곳: `entry +0x98` · `KiGetGdtIdt` · `KiSaveInitialProcessorControlState +0x26` · `KiSaveProcessorControlState +0x3c` · `KiRestoreProcessorControlState +0x2f` · `HalpLMStub +0x5f` · `KiNmiInterrupt`/`KiMcheckAbort` `+0x70` 은 읽기만) + `[GDT+0x52/+0x54/+0x57]` 에 쓰는 줄 (`SwapContext` 조각 하나) — 칸 4 를 쓰는 줄은 못 찾음 → **칸 4 바이트와 칸 10 의 나머지 (접근 바이트·플래그·한계 상위) 는 로더 (`winload`) 가 준 GDT 그대로 `[공백]`** (VM `dq @gdtr` 로).
- **(가) `entry` (= `KiSystemStartup`, rva `0xb593e0`)**: 부팅 직후 `+0x98 SGDT [RDX+0x2d6]`, `+0x9f R8 = [RDX+0x2d8]` (GDT 베이스), `+0xda CMP [R10+0x24],0` (`R10` = 로더 `Prcb`, `+0x24` = `Number`) 가 0 (부팅 CPU) 이면 `+0xe1 MOV AX,0x3c00; +0xe5 MOV [R8+0x50],AX` → **칸 10 의 `LimitLow` = `0x3c00`**. 이어서 `+0xea ~ +0xf8` 커널이 `DS=ES=0x2b`, `FS=0x53` 을 올린다. 이 함수가 `+0x1b5/+0x1ba` 로 `KiInitializeBootStructures` 를 부른다 (그 안에서는 안 씀). 다른 CPU 의 GDT 는 이번에 안 따라감 `[공백]`.
- **(다) `KiInitializeBootStructures +0x42e ~ +0x468` — SYSCALL/SYSRET 이 믿는 셀렉터**: `+0x42e MOV EDX,0x230010; +0x433 ECX=0xc0000081; +0x438 WRMSR` → **`STAR = 0x0023_0010_0000_0000`** (STAR[47:32] = 커널 CS `0x10`, STAR[63:48] = 유저 기준 `0x23`) → `SYSRET` 은 64비트로 갈 때 `CS = 0x23+0x10 = 0x33` (칸 6), `SS = 0x23+8 = 0x2b` (칸 5), 32비트로 갈 때 `CS = 0x23` (칸 4). 이어 `+0x43a ~ +0x449 CSTAR (0xc0000083) = KiSystemCall32` (`KiKvaShadow` 면 `KiSystemCall32Shadow`, `+0x3bd`), `+0x44b ~ +0x45a LSTAR (0xc0000082) = KiSystemCall64`, `+0x45c ~ +0x468 FMASK (0xc0000084) = 0x4700` (`TF·DF·IF·NT` 비움). → 칸 4 · 5 · 6 의 **순서 (0x20 → 0x23 / 0x28 → 0x2b / 0x30 → 0x33) 가 STAR 한 줄에 박혀 있다**: 에뮬레이터 GDT 가 이 순서를 따라야 `SYSRET` 흉내 (`Syscall.cpp:1177/1178` 의 `CS 0x33 · SS 0x2b`) 와 맞는다.
- **(나) `SwapContext` — 칸 10 베이스를 스레드 바꿀 때마다 고친다** (`SwapContext.asm` 조각, `(= SwapContext +…)` 표기). 새 스레드 `RSI`, 이전 `RDI`, `RBX` = PRCB:
  - `+0x504 BT [RSI+0x74],0xa; +0x509 JC +0x5af` — `KTHREAD.SystemThread` (비트 10) 면 이 블록 통째로 건너뜀.
  - `+0x50f ~ +0x52c`: `EAX = [RSI+0x678]` (**`ETHREAD.UserFsBase` 하위 32비트**); 새 스레드의 프로세스 `[+0x310] ≠ 0` (WoW64) 이면 `+0x526 EAX = [RSI+0xf0] (KTHREAD.Teb) + 0x2000` (= **TEB32 주소**).
  - `+0x531 RCX = [RBX−0x180]` (= `KPCR.GdtBase`), **`+0x538 [RCX+0x52] = AX` (BaseLow) · `+0x53f [RCX+0x54] = AL` (BaseMid) · `+0x542 [RCX+0x57] = AH` (BaseHigh)** → 칸 10 (`GDT+0x50`) 의 베이스 하위 32비트.
  - `+0x545 MOV EAX,0x53; +0x54a MOV FS,AX` → FS 로드, `+0x54c ~ +0x55d` `[RSI+0x678/0x67c]` 로 **`IA32_FS_BASE` (`0xc0000100`) 를 `UserFsBase` 로 복원** (FS 를 올리면 베이스가 서술자 값으로 덮이므로).
  - 이어 `+0x55f ~ +0x57e` DS/ES/GS 가 `0x2b` 가 아니면 `0x2b` 로 (GS 는 `SWAPGS` 사이에서), `+0x582 ~ +0x5ad` `IA32_KERNEL_GS_BASE (0xc0000102)` = `[RSI+0xf0]` (`KTHREAD.Teb` = **TEB64**; `[RSI+3] & 4` 면 `UserGsBase` `[RSI+0x680]`).
  - **이전 스레드 쪽**: `+0x138 CMP [RAX+0x310],0; +0x140 JZ` (이전 프로세스가 WoW64) 이면 `+0x142 ~ +0x14f RDMSR 0xc0000100 → [RDI+0x678/0x67c]` — 지금 FS 베이스를 `UserFsBase` 에 저장.
  - 정리: WoW64 스레드는 칸 10 베이스 = TEB32, FS_BASE MSR = `UserFsBase` (그 스레드가 만든 값; 64비트 모드로 돌 때 쓰임), 64비트 스레드는 칸 10 베이스에 `UserFsBase` 하위 32비트가 들어가지만 셀렉터 `0x53` 을 안 써서 무의미.
- 코드에서 `0x23` 이 나오는 곳: `PspWow64InitThreadGuestx86 +0x98` (SegCs), `KxContextToKframes +0x9e` (기본 CS), `KiDispatchException +0x5f8 ~ +0x5fb` (CS `& 0xfff8 == 0x20`). `0x53`: `entry +0xf4`, `SwapContext +0x545`, `PspWow64InitThreadGuestx86 +0x8e`.

### 8. 주소 공간 상한

- **`EPROCESS.HighestUserAddress` (`+0x368`) 를 쓰는 곳**: `MiComputeProcessUserVa` (rva `0x90a974`, `MmInitializeProcessAddressSpace +0x136/+0x13b` 가 부름):
  - `+0x54 ~ +0x71`: 섹션이 이미지이고 `Flags.OverrideAddressSpace` (`+0x6c BT R8D,0x11`) 가 꺼져 있을 때만 줄인다 (켜져 있으면 `0x7fffffff0000` 그대로).
  - `+0x8b TEST [이미지정보+0x2c],0x20` (**`ImageCharacteristics` 의 `LARGE_ADDRESS_AWARE`**): LAA 이고 WoW64 (`+0x91 CMP [RBX+0x310],0`) 이고 기계 `0x14c`/`0x1c4` (`+0xcd ~ +0x103`) 이면 `+0xde ~ +0xf0` **`*출력 = 0x100000000`, `EDI = 0xffff0000`, `OR.LOCK Flags, 0x200` (`Wow64VaSpace4Gb`)**.
  - LAA 가 아니면 `+0x137 TEST EDX,R8D` (EDX=`0x200`: **`Wow64VaSpace4Gb` 비트를 읽는 유일한 줄**) — 이미 켜져 있으면 (`PspAllocateProcess +0x1015` 로) 위 WoW 갈래, 아니면 `+0x140 ~ +0x152` `*출력 = 0x100000000`, `EDI = 0x7fff0000` (**2GB − 64KB**, 플래그는 안 켬).
  - `+0xa8 MOV [RBX+0x368],RDI` → `HighestUserAddress`.
- **읽는 곳** (`.asm` 줄 확인 · objdump 훑기, "이름·줄만"):

| 함수 | 줄 | 하는 일 (요약) |
|---|---|---|
| `MiGetUserReservationHighestAddress` | `+0x12` | `min(0x7ffffffeffff, HighestUserAddress − 1, −1 >> ZeroBits)` |
| `MiAllocateNewSubAllocatedRegion` | `+0x8b/+0x90` (호출) | PEB/TEB 서브 영역의 최고 주소로 씀 |
| `MiAllocateVirtualMemoryPrepare` | `+0x3ac` · `+0x517` | `NtAllocateVirtualMemory` 의 최고 주소 (`− 1`) 를 정함 (`[R13+8]` 에 저장) |
| `MiSelectUserAddress` | `+0x9d` (`+0xb7 CMP RAX,0x100000000; +0xba JBE`) | 최고 주소가 4GB 이하면 작은 주소 공간용 갈래 |
| `MiFindEmptyAddressRange` | `+0x2c1` | 빈 주소 범위 찾기 상한 |
| `MiInsertVad` | `+0x5c` (`SHR 0xc; CMP RCX,RAX`) | VAD 끝이 상한 쪽수 넘으면 갈래 |
| `MmMapViewOfSection` | `+0xdf` · `+0x198` | 뷰 매핑 상한 |
| `NtMapViewOfSection` | `+0x1d1` · `+0x555` | 같음 |
| `PspWow64ThunkProcessParameters` | `+0x5f` | 32비트 매개변수 블록 `ZeroBits` |
| 그 밖 (objdump 훑기, 줄은 `.asm` 없음) | `AlpcpCreateView +0x192/+0x4b4` · `MiAllocateProcessVads +0x80` · `MiMapLockedPagesInUserSpace +0x115` · `MiFinishVadDeletion +0x4c` · `MiInitializeProcessBottomUpEntropy +0x7c/+0xd4/+0x103` · `MiInitializeProcessTopDownEntropy +0xf` · `MiCfgInitializeProcess +0x3e/+0xea` · `MmGetMaximumUserAddress +0x10` · `MmInitializeProcessAddressSpace +0x318/+0x31f` (복제 길 부모 값 복사) · `RtlpWalkWowStack +0xdb` | |

- **PEB · TEB · 스택 · 이미지가 4GB 아래로 오는 까닭**:
  - PEB/TEB: 같은 서브 영역 (위 Q4), 최고 주소 `MiGetUserReservationHighestAddress`.
  - 32비트 매개변수 블록: 직접 `ZeroBits = HighestUserAddress − 1` (Q5).
  - 32비트 스택: `ZwSetInformationProcess(ProcessThreadStackAllocation)` — MM 이 고르는 부분이라 안 따라감 `[공백]`. 64비트 스택은 `RtlCreateUserStack` (같음).
  - **이미지: `MiSelectImageBase`** (rva `0x972de0`):
    - `+0x23 BT [RCX+0xc],0xf` 가 켜지면 (WoW 이미지) `+0x6c/+0x71 MiGetImageWowSubsystemIndex` — 기계를 표 `MiWowSubsystems6432` (rva `0x12cc8`, 항목 1개 = `0x014c`, PE 바이트) 와 비교해 `i` (맞음) 또는 `−1`. 결과 `ESI = i + 3` (x86 → **3**), 못 찾으면 `ESI = 4` (`+0x7c`). 꺼져 있으면 `ESI = (기본 베이스 ≤ 0xffffffff) ? 1 : 0` (`+0x4b SETBE`), 다만 `ImageFlags.BaseBelow4gb` (`+0x3a TEST [RCX+0x33],0x10`) 이고 DLL (`+0x274 BT [RCX+0x2c],0xd`) 이면 `ESI = 2` (`+0x280`).
    - `+0x13b ~ +0x154`: `ESI == EDX(=1)` → `+0x233` (겹침표 `0x140e2d800`, 천장 `0x78000000`); **`ESI == 2` (`+0x148 JZ → 0x140b244d1`, 흩어진 범위 `+0x1b16f1 ~`)**: 푸시락 `0x140e2d768` 잡고 두 겹침표 (`0x140e2d7a8` 과 `0x140e2d800`) 를 `RtlCopyBitMapEx` + `RtlMergeBitMapsEx` 로 합쳐 둘 다 빈 칸에서 고름 (천장 `0x78000000`); **`ESI == 3` (`+0x151 JZ +0x252`)**: 표 `0x140e2d7a8 + (ESI−3) << 6` (= WoW 표 첫 칸, `_MI_SECTION_WOW_STATE` 가 0x40 간격 `[추측: 같은 모양]`), 크기 `[0x140e2d7e8]`, **천장 `0x78000000`** (`+0x26a MOV EAX,0x78000000`). 이어 `+0x192 RtlFindClearBitsEx` → `+0x1a2 MiObtainRelocationBits` → 베이스 = `천장 − (비트 + 개수) × 0x10000` (`+0x1cd ~ +0x1d1`). 그 밖 (ESI = 0) 은 표 `0x140e2d770`, 천장 `0x7fffffff0000`.
    - 결과: x86 이미지가 **선호 주소에 못 앉고 다시 정해져야 할 때** `0x78000000` (1.875GB) 아래 64KB 단위 겹침표에서 고른다 → 4GB (사실은 1.875GB) 아래.
  - `NtAllocateVirtualMemory` 가 4GB/2GB 위를 막는 줄: `MiAllocateVirtualMemoryPrepare +0x3ac ~ +0x3bd` (`RAX = HighestUserAddress − 1; CMP RDX,RAX; CMOVBE`), `+0x517 ~ +0x528` (`CMP RCX,R11; CMOVNC`). 막는 비교 자체 (주소 > 상한 이면 `STATUS_…`) 는 더 아래 VAD 삽입 (`MiInsertVad +0x5c ~ +0x6a`) 과 `MiSelectUserAddress` 에서 일어난다 `[공백: 어느 상태 코드인지는 안 봄]`.

### 10. `KiDispatchException` WoW64 · 32비트 갈래

- 구조: `+0x247 TEST DIL,AL; +0x24a JZ +0x5d1` — `DIL` = 이전 모드 (유저 1), `AL` = `[프로세스+0x5fc]` (`Flags3`, 비트 0 `Minimal`). **유저 모드 + 최소 프로세스가 아니면** `+0x5d1`.
- `+0x5d1 ~ +0x5e9`: `RCX = 현재 스레드의 프로세스; CMP [RCX+0x310],0; JNZ +0x74f` — **WoW64 프로세스**는 `+0x74f CMP [R14],0x80000002` (`STATUS_DATATYPE_MISALIGNMENT`): 아니면 `+0x5ef` 로 합류; 맞고 `+0x75c TEST [R12+0x178],0x40000` (**트랩 프레임 `EFlags` 의 `AC` 비트 18**) 이 켜져 있으면 `+0x76e CLI; +0x76f BTR [R12+0x178],0x12` 로 AC 를 끄고 (`+0x779 ~ +0x797` 뒤처리, 조각 `+0x2fe81f ~ +0x2fe834`) 예외를 삼키고 계속.
- `+0x5ef ~ +0x5ff`: `AX = [문맥64 +0x38] (SegCs); AX &= 0xfff8; CMP AX,0x20; JNZ +0x250` — **CS 가 `0x20 ~ 0x27` (= 셀렉터 `0x23`, 32비트 코드)** 일 때만:
  - `+0x605 ~ +0x60d` 코드 `0x80000003` (브레이크포인트) 이면 `+0x2fe836 [R14] = 0x4000001f`; `+0x613 ~ +0x61e` `0x80000004` (싱글 스텝) 이면 `[R14] = 0x4000001e` (`STATUS_WX86_SINGLE_STEP`); 그 뒤 `+0x2fe83d ~ +0x2fe845` `R15 &= 0xfffffff0` (**32비트 16바이트 정렬 — `Rsp` 하위 32비트만**) 을 `[RBP+0x18]·[RBP+0x48]` 에 → `+0x250` 공통 길.
- **배달 대상**: 공통 길 끝 `+0x58c MOV RAX,[PsNtdllExports+0x20]` (rva `0xfc74d0` = `KiUserExceptionDispatcher`, 64비트 ntdll; `_PS_NTDLL_EXPORTS +0x20`) → `+0x593 [R12+0x168] (트랩 프레임 Rip) = RAX`; `+0x580/+0x583 LEA EAX,[RBX+0x32]; [R12+0x170] (SegCs) = AX` (`+0x528 MOV EBX,0x1` 이라 `RBX = 1` → **`0x33`**); `+0x578 [R12+0x180] (Rsp) = RDI − 0x4d0` (예외 틀이 놓인 자리). → **32비트 코드의 예외도 64비트 ntdll `KiUserExceptionDispatcher` (CS `0x33`) 로 간다** (32비트 ntdll 의 `KiUserExceptionDispatcher` 가 아님 — 그쪽은 wow64 가 `Wow64PrepareForException` 으로 넘긴다, run82). `PsWowX86SharedInformation +0x8` (32비트 `KiUserExceptionDispatcher`) 은 이 함수에서 안 읽는다 (`grep` 로 확인).
- 문맥 플래그 손질: `+0x348/+0x34d PsWow64GetProcessMachine == 0x14c` 이면 `+0x35b AND RDI,-0x60001` (64비트 문맥 플래그에서 `0x60001` 비트를 뺌; 뜻 `[공백]`).
- 예외 틀이 어느 스택 (64비트용 vs 32비트 `ESP` 아래) 에 놓이는지는 `RDI` 를 만드는 줄을 안 따라갔다 `[공백]` (위 `R15 & 0xfffffff0` 으로 보아 32비트 스택 쪽일 가능성 `[추측]`).

---

## 표 B-0. `PsWowX86SharedInformation` 8칸 (원문 B 4 겸사)

PE 파일 바이트 (`NtdllWowX86Exports` rva `0xc801a0`, 16바이트 × 8 = {이름 문자열, 기록할 칸}) — `NtdllExportInformation[1]` = `{0xc801a0, 8}` (sast/11):

| 칸 (`PsWowX86SharedInformation` rva `0xfda900` +) | 32비트 ntdll 함수 이름 | 이 결과서에서 읽는 곳 |
|---|---|---|
| `+0x00` | `LdrInitializeThunk` | |
| `+0x08` | `KiUserExceptionDispatcher` | (`KiDispatchException` 은 안 읽음, Q10) |
| `+0x10` | `KiUserApcDispatcher` | |
| `+0x18` | `KiUserCallbackDispatcher` | |
| `+0x20` | **`RtlUserThreadStart`** | `PspWow64InitThread +0x15d` (`MOV RSI,[RAX+0x20]`) → x86 `Eip` |
| `+0x28` | `RtlpQueryProcessDebugInformationRemote` | |
| `+0x30` | (이름 표에 없음 — WOW ntdll 기본 주소, run48) | |
| `+0x38` | **`LdrSystemDllInitBlock`** | `PspPrepareSystemDllInitBlock +0x2e2` (32비트 시작 블록 주소) |
| `+0x40` | `RtlpFreezeTimeBias` | |

(native 표 19칸의 이름은 sast/11 에 있음 — 같은 `NtdllExports` 형식.)

---

## 칸 표 요약 (baggage 꼴: 공간 = 유저 / 커널)

| 짐 (구조체 · 칸) | 공간 | 단계 | 놓는 법 | 쓰는 줄 (함수 +오프셋) |
|---|---|---|---|---|
| `EPROCESS.Machine (+0x6ec)` | 커널 | 프로세스 만들기 | `PspSelectMachineForProcess` 출력 | `PspAllocateProcess +0x1043` |
| `EPROCESS.WoW64Process (+0x310)` → `_EWOW64PROCESS` (0x10) | 커널 (비페이지 풀) | 프로세스 만들기 | `ExAllocatePool2` | `PspAllocateProcess +0xfc4/+0xfcc` |
| `_EWOW64PROCESS.Peb` | 커널 (풀) | PEB 만들기 | 1 → PEB32 주소 | `+0x100b` → `MiInitializeWowPeb +0x78` |
| `_EWOW64PROCESS.NtdllType` | 커널 | 프로세스 만들기 | `PspWow64PickBestNtdll` | `PspAllocateProcess +0xffb` |
| `EPROCESS.Flags.OverrideAddressSpace` (비트 17) | 커널 | 프로세스 만들기 | 출력 `+3` | `PspAllocateProcess +0xac7`, `+0x13f6` |
| `EPROCESS.Flags.Wow64VaSpace4Gb` (비트 9) | 커널 | 프로세스 만들기 / 주소 공간 | `Flags` 인자 비트 23 / LAA | `PspAllocateProcess +0x1015` · `MiComputeProcessUserVa +0xf0` |
| `EPROCESS.HighestUserAddress (+0x368)` | 커널 | 주소 공간 | `0xffff0000` / `0x7fff0000` | `MiComputeProcessUserVa +0xa8` |
| PEB32 (1 페이지, 27칸) | **유저** (4GB 아래, PEB64 옆) | PEB 만들기 | `MiCreatePebOrTeb(1)` + 값 쓰기 | `MiInitializeWowPeb +0x5d ~ +0x1ae` |
| PEB32 `+0x10 ProcessParameters` | 유저 | 주소 공간 만들기 | 32비트 블록 | `PspWow64ThunkProcessParameters +0x761` |
| 32비트 `RTL_USER_PROCESS_PARAMETERS` | 유저 (4GB 아래) | 주소 공간 만들기 | `ZwAllocateVirtualMemory` + 변환 복사 | `+0x85` · `+0xb1 ~ +0x740` |
| 32비트 `LdrSystemDllInitBlock` | 유저 (32비트 ntdll 이미지 안) | 주소 공간 만들기 | `PspPrepareSystemDllInitBlock(1)` | `PspSetupUserProcessAddressSpace +0x214` |
| TEB64 + TEB32 (4 페이지, TEB32 = +0x2000) | 유저 (4GB 아래) | 스레드 만들기 | `MiAllocateFromSubAllocatedRegion` | `MmCreateTeb +0xa7` |
| TEB32 19칸 | 유저 | 스레드 만들기 | 위 표 | `MmCreateTeb +0x156 ~ +0x1d8` |
| TEB32.`SameTebFlags (+0xfca)` | 유저 | 스레드 만들기 | 플래그 워드 | `PspAllocateThread +0x7db` |
| 32비트 스택 (reserve = 요청/헤더, commit, 가드 2 페이지) | 유저 (4GB 아래) | 스레드 만들기 | `ZwSetInformationProcess` + `ZwAllocateVirtualMemory` ×2 | `RtlpWow64CreateUserStack +0x1aa ~ +0x24d` |
| 64비트 스택 (reserve `0x40000`, commit `0x8000`) | 유저 | 스레드 만들기 | `RtlCreateUserStack` (WoW 지역 매개변수) | `PspAllocateThread +0x97e ~ +0x9a9` |
| CPU 영역 (`+0` 헤더 dword, `+4` x86 `CONTEXT` + 확장) | 유저 (64비트 스택 맨 위) | 스레드 만들기 | 스택 맨 위를 잘라 냄 + 문맥 초기화 | `PspSetupUserStack +0x153` → `PspWow64SetupCpuArea +0x54 ~ +0x83` · `PspWow64InitThread +0xb5 ~ +0xe1` |
| TEB64.`TlsSlots[1] (+0x1488)` | 유저 | 스레드 만들기 | CPU 영역 주소 | `PspWow64InitThread +0xae` |
| x86 `CONTEXT` 값 (EIP · ESP · 세그먼트) | 유저 (CPU 영역 안) | 스레드 만들기 | 표 위 | `PspWow64InitThreadGuestx86 +0x3e ~ +0x108` |
| GDT 칸 10 한계 `0x3c00` | **커널** (CPU 가 읽는 GDT) | 부팅 | 부팅 CPU 한 번 | `entry +0xe1/+0xe5` |
| GDT 칸 10 베이스 (TEB32 또는 `UserFsBase`) | 커널 | **스레드 바꿀 때마다** | 문맥 전환 | `SwapContext +0x538 · +0x53f · +0x542` |
| `FS_BASE` MSR ← `UserFsBase` | 커널 (CPU) | 스레드 바꿀 때마다 | `WRMSR` | `SwapContext +0x558 ~ +0x55d` |
| GDT 칸 4 · 칸 10 나머지 바이트 | 커널 | 부팅 (로더) | ntoskrnl 에서 안 씀 | `[공백]` — `dq @gdtr` |

---

## 호출 지점 (WinDbg `caller+0x…` = `_calls.tsv` `ret_off`)

| caller | call_off / ret_off | callee |
|---|---|---|
| `PspAllocateProcess` | `+0xa87` / `+0xa8c` | `PspSelectMachineForProcess` |
| `PspAllocateProcess` | `+0xfc4` / `+0xfc9` | `ExAllocatePool2` (`'WowP'`) |
| `PspAllocateProcess` | `+0xff6` / `+0xffb` | `PspWow64PickBestNtdll` |
| `PspSelectMachineForProcess` | `+0x76` / `+0x7b` · `+0xf3` / `+0xf8` · `+0x10a860` / `+0x10a865` | `PsGetProcessMachine` · `PsWow64IsMachineSupported` · `PspQueryComPlusRunUnderWoW` |
| `MmCreatePeb` | `+0x67b` / `+0x680` | `MiInitializeWowPeb` |
| `MiInitializeWowPeb` | `+0x5d` / `+0x62` | `MiCreatePebOrTeb` |
| `MiCreatePebOrTeb` | `+0x25` / `+0x2a` | `MiAllocateFromSubAllocatedRegion` |
| `MiAllocateFromSubAllocatedRegion` | `+0x1e6` / `+0x1eb` | `MiAllocateNewSubAllocatedRegion` |
| `MiAllocateNewSubAllocatedRegion` | `+0x8b` / `+0x90` · `+0xff` / `+0x104` | `MiGetUserReservationHighestAddress` · `MiSelectUserAddress` |
| `PspSetupUserProcessAddressSpace` | `+0x1e1` / `+0x1e6` · `+0x1f2` / `+0x1f7` · `+0x214` / `+0x219` | `PspPrepareSystemDllInitBlock(0)` · `PspWow64SetupUserProcessAddressSpace` · `PspPrepareSystemDllInitBlock(종류)` |
| `PspWow64SetupUserProcessAddressSpace` | `+0x42` / `+0x47` | `PspWow64ThunkProcessParameters` |
| `PspWow64ThunkProcessParameters` | `+0x85` / `+0x8a` | `ZwAllocateVirtualMemory` |
| `PspAllocateThread` | `+0x5ea` / `+0x5ef` (비 WoW) · `+0x9a9` / `+0x9ae` (WoW) | `PspSetupUserStack` |
| `PspAllocateThread` | `+0x9e6` / `+0x9eb` · `+0x62c` / `+0x631` · `+0xb78` / `+0xb7d` | `PspWow64SetupUserStack` · `MmCreateTeb` · `PspWow64InitThread` |
| `PspSetupUserStack` | `+0x153` / `+0x158` | `PspWow64SetupCpuArea` |
| `PspWow64SetupUserStack` | `+0x5c` / `+0x61` | `RtlpWow64CreateUserStack` |
| `RtlpWow64CreateUserStack` | `+0x1aa` / `+0x1af` · `+0x1fe` / `+0x203` · `+0x24d` / `+0x252` | `ZwSetInformationProcess` · `ZwAllocateVirtualMemory` ×2 |
| `PspWow64InitThread` | `+0xdc` / `+0xe1` · `+0x103` / `+0x108` | `RtlInitializeExtendedContext2` · `PspWow64InitThreadGuestx86` |
| `entry` (`KiSystemStartup`) | `+0x1b5` / `+0x1ba` | `KiInitializeBootStructures` |

---

## 지금 우리 (PE_emulator-AR) 에 맞나·틀리나 — "에뮬레이터가 지어야 할 것"

(에뮬레이터 `../PE_emulator-AR/Src/nt/` 을 grep 한 범위만; 있는 것은 줄 번호.)

- **AR02** (`Process.cpp _CheckX64Only` 풀기 · `WoW64Process` 칸 · `SystemDll.cpp` `PspSystemDlls[1]`):
  - `_CheckX64Only` (`Process.cpp:344 · 1113 · 1161 · 1446 · 1620 · 1749`, `AddressSpace.cpp:750 · 1168` 주석이 "x64 만" 으로 줄인 곳 전부) → **x86 이미지 (`Machine 0x14c`, PE32) 를 받고**, `PspSelectMachineForProcess` 판정 (위 Q1 순서: 이미지 기계 · 속성 · 부모 · `PsWow64IsMachineSupported` · .NET `ImageFlags&3==3` 갈래) 을 흉내. 출력 3칸 → `Machine`, `'WowP'` 여부, `OverrideAddressSpace`.
  - `ST_EPROCESS` 에 `WoW64Process` 칸을 **새로 지을 것**: 0x10 바이트 `{Peb, NtdllType}` (`AddressSpace.cpp:1168` 는 "칸을 안 둔다 (D10)" — 이번에 둔다), `Machine` 은 `ST_EPROCESS` 의 `+0x6ec` 칸.
  - `PspSystemDlls[1]` = `SystemDll.cpp:133/156` 에 이미 `PspWowX86SystemDllData = { "SysWOW64", 0x0008, 0x014c, false }` — **진짜 PE 바이트와 같다** (플래그 `0x8`, 기계 `0x14c`). 맞음. `PspWow64PickBestNtdll` 은 "x86 → 1" 한 줄 (`Process.cpp:1119` "줄임" 주석이 이미 이 가정) — 단 `[CreateInfo+0x11]` 비트 7 갈래는 안 만들어도 x86 에선 안 탄다 `[추측]`.
  - `PsWowX86SharedInformation` 8칸 (`SystemDll.cpp:242` "공사 중") → **표 B-0 의 이름 8개** 로 채움 (칸 `+0x20` 32비트 `RtlUserThreadStart`, `+0x38` `LdrSystemDllInitBlock` 이 커널 안에서 읽힘). `PspSystemDlls[1].+0x20` 의 베이스는 `MiSelectImageBase` ESI=3 (Q8) 로 정해야 진짜와 같다 (`SystemDll.cpp:154` TODO).
  - 이미 알고 있는 차이 (`Process.cpp:1043 · 1119 ~ 1123`: "공유 16칸 0 … 진짜 x64도 채움 [관찰] run44"): 이번 결과로 **채울 값의 출처가 정해졌다.** 부팅 때 `PspInitializeSystemDlls` 가 표 B-0 의 8칸 (`+0x0 ~ +0x40`, `+0x30` 은 WOW ntdll 기본 주소) 만 32비트 ntdll 내보내기에서 찾아 채우고 (나머지 `+0x48 ~ +0x78` 은 0), `PspPrepareSystemDllInitBlock` 이 **16칸 전체를 시작 블록 `+0x18` 에 복사** 한다. 이 복사는 종류 0 (x64 native 블록) 에서도 돈다 (`PspPrepareSystemDllInitBlock +0xb7 ~ +0xc3`: `PsWow64GetProcessNtdllType` 이 0 이면 `CMOVZ ECX,ESI` 로 1 → `PspWow64GetSharedInformation(1)`; `+0xde ~ +0xf7` 복사) → **x64 길도 AR03 에서 8칸 값이 들어가야 진짜와 같다** `[정적]`.
- **AR03** (PEB32 · 32비트 블록 · 매개변수):
  - `MmCreatePeb` 끝에 `MiInitializeWowPeb` **새로 지을 것**: PEB32 1 페이지 (서브 영역에서, PEB64 다음 칸, `HighestUserAddress` 아래), 위 27칸 표, `_EWOW64PROCESS.Peb` 갱신 (1 → 주소), load config 를 PE32 모양으로 다시 읽음 (친화도·CSD), 힙 값 손질 (`> 1GB → 1MB`, `commit > reserve → 8KB`).
  - 서브 영역: 지금은 `_SelectPebBase` (`Process.cpp:150` 둘레, "에뮬레이터 규칙: 주소는 여기 한 곳에서만 고른다", `MiCreatePebOrTeb` 본문은 run 문서 없음 이라 적힘) 가 PEB 자리를 따로 고른다. 이번에 `MiCreatePebOrTeb` → `MiAllocateFromSubAllocatedRegion` → `MiAllocateNewSubAllocatedRegion` 본문을 읽었으므로 (Q4) **서브 영역 (2MB `'VadS'`, 힌트부터 위쪽으로 빈 칸 찾기 next-fit, 영역 첫 힌트 랜덤, 상한 `HighestUserAddress`) 흉내**를 PEB64 · PEB32 · TEB 가 함께 쓰게 하면 진짜와 같은 꼴 (실제 거리는 VM `[관찰]`).
  - 32비트 매개변수 블록 (`PspWow64ThunkProcessParameters`) · 32비트 `LdrSystemDllInitBlock` (`PspPrepareSystemDllInitBlock(1)`, `Size == 0x128` 검사 포함) **새로 지을 것** (`Process.cpp:1161` 가 "안 함" 으로 둔 곳). 그 밖 PEB32 쓰기: `ApiSetMap +0x38` (`AddressSpace.cpp:750` 이 "안 탐" 으로 둔 `MmMapApiSetView +0x96 ~ +0xec` WoW 갈래), `SharedData +0x50`, `ExtendedFeatureDisableMask +0x480`, `ActiveProcessAffinityMask +0xc0`.
- **AR04** (TEB32 · 32비트 스택 · CPU 영역):
  - `MmCreateTeb` WoW 갈래 (4 페이지, TEB32 = +0x2000, 위 19줄 표, TEB64 `+0x180c`/`+0x0`) **새로 지을 것** (`Process.cpp:1749` 가 "안 함" 으로 둔 `PspAllocateThread +0x79b ~ +0x7e3` 와 합쳐).
  - 32비트 스택 (`RtlpWow64CreateUserStack`: PE32 헤더 값 · 기본 `0x4000` · 1MB/64KB 올림 · `MinimumStackCommit` · 가드 2 페이지) · 64비트 스택 (WoW 에선 reserve `0x40000` · commit `0x8000` 고정) · CPU 영역 자르기 (`PspWow64SetupCpuArea`) · `TlsSlots[1]` · x86 `CONTEXT` 초기값 (위 표) **새로 지을 것**. 에뮬레이터의 이 개념은 아직 없음 (`SystemInfo`·`Process` 에서 grep 해도 `TlsSlots`/CPU 영역 없음).
- **AR05** (GDT 칸 4 · 10 · FS `0x53` · 모드 바꾸기):
  - `AddressSpace.cpp:73 _InstallUserGdt` 는 지금 **칸 5 (`0x28`, 유저 데이터 `0x00CFF3000000FFFF`) 와 칸 6 (`0x30`, 64비트 유저 코드 `0x00AFFB000000FFFF`) 만** 두고 칸 4 (`0x20`, 32비트 유저 코드 `0x23`) 는 0 (`AddressSpace.cpp:80` "공사 중"), 한계 `USER_GDT_LIMIT = 0x37` (칸 0 ~ 6, `AddressSpace.h:84 ~ 85`). → 틀린 곳: WoW64 는 **칸 4 를 새로 지어야** 하고 칸 10 (`0x50`) 까지 덮으려면 **한계를 `0x57` 이상으로** 늘려야 한다 (`AddressSpace.h:84` 주석은 "칸 8 이후는 근거 없음"). 칸 4 바이트는 진짜 값이 ntoskrnl 에 없어서 (로더 몫) `[공백]` — VM `dq @gdtr` 로 받기 전까지는 표준 값 (`0x00cffb000000ffff`, 32비트 DPL3 코드 · 한계 4GB) 을 쓰고 `[추측]` 로 표시.
  - 칸 10 (`0x50`, `0x53`): 한계 `0x3c00` (`entry +0xe5`), 접근 바이트·플래그 `[공백]` (VM). **베이스는 스레드마다 TEB32 로** (`SwapContext +0x538 ~ +0x542`) — 에뮬레이터 스케줄러 (`Scheduler.cpp`) 의 스레드 바꿈에 이 한 줄을 넣어야 함. `FS` 로드 `0x53` + `FS_BASE MSR` 복원 (`+0x545 ~ +0x55d`), 이전 스레드의 `UserFsBase` 저장 (`+0x138 ~ +0x14f`) 도.
  - `KxContextToKframes` CS 규칙 (`0x33` 이면 `0x33`, 아니면 `0x23`, `SS 0x2b`) 은 `Syscall.cpp:1177/1178` (`wSegCs = 0x33 / wSegSs = 0x2b`) 와 x64 쪽은 맞음. 32비트 문맥 (`CS 0x23`) 길은 **새로 지을 것**.
- **AR06 이후 메모**: `KiDispatchException` 의 WoW64 갈래 (AC 비트 삼킴 · `0x4000001f/1e` 변환 · 항상 64비트 `KiUserExceptionDispatcher`, CS `0x33`) 는 예외 배달 쪽에 **새로 지을 것** (AR07 이후). `HighestUserAddress` 상한과 `MiSelectImageBase` WoW 겹침표 (`0x78000000`) 는 VM·이미지 매핑 (`VirtualMemory.cpp`) 에 **새로 지을 것** (AR07 이후).

---

## 동적으로 확인할 것 (원문 D 와 합침)

정적으로 못 닫은 것과 멈춤점. WinDbg 오프셋은 `ret_off`/명령 위치 그대로.

1. **`Wow64VaSpace4Gb` · `HighestUserAddress` 값**: x86 시험 exe (LAA 켠 것 / 끈 것 두 가지) 로 `bp nt!MiComputeProcessUserVa+0xaf` (`+0xa8` 쓰기 직후) 에서 `? poi(@rbx+0x368)` (`0xffff0000` / `0x7fff0000` 기대), `dd @rbx+0x1f4 L1` 비트 9.
2. **`PspSelectMachineForProcess` 출력**: `bp nt!PspSelectMachineForProcess+0xd1` (쓰기 직전) 에서 `r bx` · `r dil` · `r cl` (기계 · WoW 칸 필요 · override), `bp nt!PspSelectMachineForProcess+0xfc` 에 걸리면 `0xc000007b`. 그리고 `[CreateInfo+0xa8]` (`dw @rbp+0xa8 L1`) · `[CreateInfo+0x11]` 최상위 비트.
3. **`'WowP'`**: `bp nt!PspAllocateProcess+0xfcc` → `!pool @rax`, `dq @rax L2` (`Peb = 1`, `NtdllType`). `bp nt!PspAllocateProcess+0xffb` 직후 `[R10+8]` = 1 인지.
4. **PEB32**: `bp nt!MiInitializeWowPeb+0x7b` → `? poi(poi(@rsi+0x310))` (PEB32 주소), PEB64 와의 거리 (`? @rax − poi(@rsi+0x2e0)`), `dt nt!_PEB32 <주소>` 를 `bp nt!MiInitializeWowPeb+0x2e8` (쓰기 끝) 에서. 서브 영역: `!vad` 에서 `'VadS'` 2MB 영역, `dq poi(poi(@rsi+0x400+0x10)+0x408)` 목록.
5. **32비트 매개변수**: `bp nt!PspWow64ThunkProcessParameters+0x765` (`+0x761` 쓰기 직후) → `dd @r15+0x10 L1` (PEB32 `ProcessParameters`), `dt nt!_RTL_USER_PROCESS_PARAMETERS`(32비트 모양은 수동) 총 크기 `@rbx`.
6. **TEB32**: `bp nt!MmCreateTeb+0x1db` → `dt nt!_TEB32 @rcx` (19칸 + `WowTebOffset`), `dq @rbx L1` (= TEB32 주소). 새 스레드 첫 번째 (`InitialThread`) 와 두 번째 비교.
7. **스택·CPU 영역**: `bp nt!RtlpWow64CreateUserStack+0x259` (성공 반환) 에서 InitialTeb (`[RSP+0x...]` — 5번째 인자 `@r14`): `dq @r14+0x28 L3` (StackBase32/Limit32/AllocatedBase32). `bp nt!PspWow64InitThreadGuestx86+0x112` (EFlags 쓴 뒤, `RBP` 복원 전) → `dd @rbp L0x2cc/4` (x86 `CONTEXT` 전체), `dq poi(poi(@rsp+0x...)...` 대신 간단히 TEB64 `+0x1488` (`dq @r15?`) 은 `bp nt!PspWow64InitThread+0xb5` 에서 `? @r15`. **확인할 것**: CPU 영역 주소 = `TEB64+0x1488` 값, `Eip` 가 32비트 `ntdll!RtlUserThreadStart` 와 같은지, `Esp` 랜덤 범위, 64비트 `Rsp` (`[ctx+0x98]`) 가 CPU 영역 아래인지.
8. **GDT**: `dq @gdtr+0x20 L1` · `dq @gdtr+0x50 L2` (칸 4 · 칸 10 바이트 — 접근 바이트 `0xfb`/`0xf3`, 한계 `0x3c00`, 플래그), `bp nt!SwapContext+0x545` 에서 (새 스레드가 WoW64 일 때) `dq @gdtr+0x50 L1` 이 TEB32 주소를 담는지, `r fs` (`0x53`), `rdmsr c0000100` 전후, `ETHREAD +0x678 UserFsBase` (`dt nt!_ETHREAD UserFsBase`).
9. **예외**: x86 시험 exe 에서 `int 3` / 정렬 위반 (EFLAGS.AC 켠 채) 일으켜 `bp nt!KiDispatchException+0x5d1` · `+0x74f` · `+0x58c` 에 걸리는지, 예외 코드 `0x4000001f` 로 바뀌는지, 도착 `Rip` = `ntdll!KiUserExceptionDispatcher` (64비트) 인지, `Rsp` 가 어느 스택 (32비트 `Esp` 근처 vs 64비트) 인지.
10. **`PsWowX86SharedInformation`**: `dq nt!PsWowX86SharedInformation L9` (+0x30 WOW ntdll 기본 주소 포함), `dq nt!PspSystemDlls L6`, `dq poi(nt!PspSystemDlls+8) L8` (칸 1 `+0x20` 값). `bp nt!PspWow64InitThread+0x15d` 에서 `RSI` 가 32비트 `RtlUserThreadStart` 와 같은지.
11. **`LdrpInitializeProcess +0x17fe` 가 돌아오나** (원문 D) — wow64.dll 몫 (Q253b).

## 다음 요청 후보

- **PEB32 를 쓰는 다른 곳** (이번에 20곳 중 6곳만 몸을 봄): `PspGetStandardHandleList +0x12` · `PspTrySetProcessPebThrottlingFlags +0x4b` (`CrossProcessFlags +0x28`) · `PsQueryProcessCommandLine +0xab` · `PsQueryProcessExceptionFlags +0x104` · `PspApplyWorkingSetLimitsToProcess +0x18c` · `MmInitializeProcessAddressSpace +0x376` · `NtQueryInformationProcess +0x16a5/+0x19b8` (`ProcessWow64Information`) · `EtwpCoverageEnsureUserModeView +0x152` 등. 한 줄씩 칸 이름만 (AR03).
- `MiSelectImageBase` 전체 (WoW 겹침표 `0x140e2d7a8`·`0x140e2d800`·`0x140e2d770` 의 PDB 이름 — `MiState` 안 칸으로 짐작), `MiObtainRelocationBits` (AR07).
- `ZwSetInformationProcess(ProcessThreadStackAllocation)` 의 MM 쪽 (`NtSetInformationProcess` → …) — 32비트 스택 주소를 `HighestUserAddress` 로 자르는 줄.
- 부팅 때 GDT 가 만들어지는 곳 (로더 → 다른 CPU 복사: `KeStartAllProcessors` · `KiInitializeProcessor`).
- `PspSetupUserProcessAddressSpace` 가 쓰는 `[ctx+0xe8]`/`[ctx+0xec]` 를 읽는 곳 (PEB 주소를 만든 쪽에 돌려주는 자리).
- `PS_ATTRIBUTE` 번호 (`CreateInfo +0xa8`/`+0x1e8`) 와 `Flags` 인자 비트 23 (`0x800000`) 의 이름.
- Q253b (wow64 DLL 넷) · Q253c (SysWOW64 ntdll) — 이번 B 4 (이름 8개) 는 닫았으므로 Q253c 에서 뺄 것.

## 판정

**분석성공.** 원문 C 1 ~ 10 모두 줄 단위 근거로 답함. 정적으로 못 닫은 것 (GDT 칸 4 바이트 · 칸 10 나머지 · 32비트 스택 주소를 MM 이 고르는 줄 · 예외 틀이 놓이는 스택 · `Flags` 비트 23 · 몇몇 속성 번호) 은 `[공백]`/`[추측]` 으로 남기고 동적 목록에 넣음. 버전 대조 일치 (`NtCreateUserProcess +0xc36`, `PspInsertThread +0x807`, PDB GUID).

# 2026-11-23 (작성일, 정적 분석 2026-11-23) run139 — WoW64 (x86) 초기화의 **커널 쪽** (Q253a, 정적만): x86 exe 는 `PspSelectMachineForProcess` 가 `PsWow64IsMachineSupported(0x14c)` 로 WoW64 로 정하고, 커널이 **`'WowP'` 0x10 B (`{Peb, NtdllType = 1}`) · PEB32 (PEB64 와 같은 서브 영역, 27칸) · 32비트 매개변수 블록 · TEB32 (= TEB64 + `0x2000`, 19줄) · 32비트 스택 · CPU 영역 (64비트 스택 맨 위, TEB64 `+0x1488`) · x86 첫 문맥 (`Eip` = 32비트 `RtlUserThreadStart`, CS `0x23` · FS `0x53`)** 을 짓는다. 4GB 상한의 뿌리는 `EPROCESS.HighestUserAddress`, GDT 칸 10 베이스는 **`SwapContext` 가 스레드마다 TEB32 로** 고친다 (칸 4 바이트는 ntoskrnl 에 없음 — 로더 몫)

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator-AR 요청** (갈래 AR "x86 exe 를 진짜 WoW64 로", 원 요청 `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` 의 **C 절**) 이다. test.exe 는 x64 라 이 갈래를 안 탄다 — `PspSelectMachineForProcess` 출력 `+2` 가 0 이라 `'WowP'` (`PspAllocateProcess +0xfad TEST BL,0x40`) 를 건너뛰고 `[정적]`, EPROCESS `WoW64Process : (null)` (`ms/logs/04_baggage-read_P1a.txt:160` · `_P2.txt:160`, run106 이 맞춘 줄) `[관찰]`. 그림 1 · 2 에 WoW64 노드가 없어 **그림은 안 바꿨다.** flow.md 는 기준 · 2.1 (WoW 줄 아홉 자리) · 2.3 부팅 절 한 줄 · 3장 (run75 · run81 절 한 줄씩, run138 절 고침, 끝에 새 절) · 4장 · 4.1 · 5장만 고쳤다.

> **대기열**: **Q253a** (AR01 C 절. 사용자 결정 2026-11-23 "다 끝나면 이것도 바로 이어서" — 요청 차례 C → A → B 의 첫째. 디컴파일은 먼저 끝났고 문서는 run138 (A 절, Q253b) 뒤). 결과서 판정 **분석성공** (C 1 ~ 10 전부, 못 닫은 몇 줄은 `[추측]` · `[공백]`).
> - 이어 쓰는 앞 run: **run10** (`MmCreatePeb +0x680 MiInitializeWowPeb`), **run11 · run32 · run50 · run89** (`PspAllocateProcess +0xa87 · +0xfc4 · +0xffb`, `PspSetupUserProcessAddressSpace +0x1f2 · +0x214`, `PspAllocateThread +0x9eb · +0xb7d`, `Flags.Wow64VaSpace4Gb` 이름만), **run48** (`PspSystemDlls[1]` · `PsWowX86SharedInformation`), **run54 · run79** (`MmCreateTeb` x86 갈래 19줄 · `MmDeleteTeb` `0x4000`), **run16 · run29** (CS `0x33`/`0x23` · SS `0x2b`), **run75** (`SwapContext +0x545 ~ +0x55d`), **run81** (`KiDispatchException +0x5d1 ~ +0x62a` 이름만), **run138** (wow64 DLL — "CPU 영역 TEB64 `+0x1488` 은 커널이 짓는다 `[추측]`"). 모두 이번 줄과 **맞는다** (아래 "앞 run 과 이어 읽기"). 닫힌 `[추측]` 은 run43 · run48 · run89 · run138 맨 위에 "고쳐 읽기 (run139)" 한 줄씩.
> - 결과서 **표 B-0** (`PsWowX86SharedInformation` 8칸의 32비트 ntdll 이름) 이 원문 **B 4** 를 겸사로 닫았다 → Q253c (sast/137) 에서 빠짐.
> - 남은 동적 → **Q262 (동적: 결과서 동적 1~11 — Q253d·Q260 VM 과 합침)**, 남은 정적 → **Q263 (정적: 결과서 다음 요청 후보 — PEB32 쓰는 나머지 곳·`MiSelectImageBase` WoW 겹침표·`ProcessThreadStackAllocation` MM 쪽·다른 CPU GDT·`[ctx+0xe8/+0xec]`, 그리고 `[공백]` GDT 칸 4·10 바이트 (로더 몫))**.

## 요약

- **C1 기계 고르기** `[정적]` — `PspSelectMachineForProcess` (rva `0xa2bb9c`): 이미지 기계 `0x14c` 이고 `.NET` 이 아니면 (`ImageFlags & 3 ≠ 3`) 고른 기계 = `0x14c` → `PsWow64IsMachineSupported(0x14c)` (시스템 DLL 칸 중 플래그 `& 8` · 기계 `0x14c` 칸이 있어야 참) → 출력 4 B `{+0 기계 0x14c, +2 WoW 칸 필요 1, +3 OverrideAddressSpace 0}`. 못 지원하면 `0xc000007b`. x86 이미지가 `.NET` AnyCPU 면 64비트로 돌리고 `+3` = 1. 출력은 `PspAllocateProcess` 가 `EPROCESS.Machine (+0x6ec)` (`+0x1043`) · 지역 플래그 `0x40` (`+0xab3`) · `Flags.OverrideAddressSpace` (`+0xac7` → `+0x13f6`) 로.
- **C2 `'WowP'`** `[정적]` — `+0xfc4 ExAllocatePool2(0x40 비페이지, 0x10, 'WowP')` → `+0xfcc EPROCESS.WoW64Process (+0x310)`. PDB `_EWOW64PROCESS` = `{+0x0 Peb, +0x8 NtdllType}`. `Peb` 는 처음 **1** (`+0x100b`), `MiInitializeWowPeb +0x78` 이 PEB32 주소로 덮는다. `Machine` 은 이 안에 없다.
- **C3 칸 번호** `[정적]` — **x86 → 1.** `PspWow64PickBestNtdll` 반복이 `i = 1` (`PspWowX86SystemDllData`, 첫 dword `0x014c0008`) 에서 맞는다. `PsWow64GetProcessNtdllType` 은 `[WoW64Process+8]` 을 읽을 뿐.
- **C4 PEB32** `[정적]` — `MmCreatePeb` 가 PEB64 를 다 쓴 뒤 (`+0x598` WoW64 검사) `+0x67b MiInitializeWowPeb` → `+0x5d MiCreatePebOrTeb(종류 1, 크기 `0x4b8`)` → `MiAllocateFromSubAllocatedRegion`. 자리 = PEB64 · TEB 와 **같은 서브 영역** (2MB `'VadS'`, 상한 `HighestUserAddress`) 의 빈 페이지. 27칸을 쓰고 `_EWOW64PROCESS.Peb` 에 주소를 적는다.
- **C5 32비트 매개변수** `[정적]` — `PspSetupUserProcessAddressSpace +0x1f2` → `PspWow64SetupUserProcessAddressSpace` → `+0x42 PspWow64ThunkProcessParameters`: 새 프로세스 안에 `ZwAllocateVirtualMemory` (상한 `HighestUserAddress − 1`) 로 블록을 잡아 64비트 매개변수를 32비트 모양으로 칸마다 옮기고, `+0x761` **PEB32 `+0x10 ProcessParameters`** · `+0x76c [ctx+0xe8]` 에 적는다. 32비트 `LdrSystemDllInitBlock` (`+0x214 PspPrepareSystemDllInitBlock(1)`) 과는 서로 안 읽는다.
- **C6 TEB32** `[정적]` — `MmCreateTeb` 종류 5 (`0x3038` → 4 쪽) 를 서브 영역에서 받아 **TEB32 = TEB64 + `0x2000`**, 19줄 (`+0x156 ~ +0x1d8`) 을 `_TEB32` 칸 이름으로 풀었다. `PspAllocateThread +0x7db` 가 TEB32 `SameTebFlags (+0xfca)` 도 쓴다 (플래그 ≠ 0 일 때만).
- **C7 스택 · CPU 영역** `[정적]` — WoW64 스레드는 **64비트 스택을 reserve `0x40000` · commit `0x8000` 고정**으로 만들고 (`PspAllocateThread +0x97e ~ +0x9a9`), 그 맨 위를 `PspSetupUserStack +0x153 PspWow64SetupCpuArea` 가 잘라 **CPU 영역** 으로 둔다. 32비트 스택은 `+0x9e6 PspWow64SetupUserStack` → `RtlpWow64CreateUserStack` (PE32 헤더 · 기본 commit `0x4000` · 가드 2 쪽). `+0xb78 PspWow64InitThread` 가 **TEB64 `+0x1488` (`TlsSlots[1]`) ← CPU 영역** (`+0xae`), 영역 `+0x0` ← `0x014c0000` (`+0xbf`), `+0x4` 부터 x86 `CONTEXT` → `PspWow64InitThreadGuestx86`: `Eip` = 32비트 `RtlUserThreadStart` (`PsWowX86SharedInformation +0x20`), `Eax` = 시작 주소, `Ebx` = 문맥64 `Rdx`, `Esp` = `StackBase32 − 랜덤 × 4 − 0x10`, CS `0x23` · SS/DS/ES/GS `0x2b` · FS `0x53`, `EFlags 0x202`. 64비트 쪽 문맥은 읽기만 한다.
- **C8 4GB 상한** `[정적]` — 뿌리 = `EPROCESS.HighestUserAddress (+0x368)`: `MiComputeProcessUserVa +0xa8` 가 x86 WoW64 에 `0xffff0000` (LAA, 이때 `Wow64VaSpace4Gb` 켬 `+0xf0`) / `0x7fff0000` (LAA 아님). 비트 자체를 읽는 줄은 `+0x137` 하나. PEB/TEB 서브 영역 · 매개변수 블록 · `NtAllocateVirtualMemory` · 뷰 매핑이 이 값을 상한으로 쓴다. x86 이미지 기준 주소는 `MiSelectImageBase` ESI = 3 → WoW 겹침표 · 천장 `0x78000000`.
- **C9 GDT** `[정적]` — 칸 4 · 10 의 바이트를 쓰는 줄은 **ntoskrnl 에 없다** (로더 몫 `[공백]`). 커널이 만지는 건 (가) `entry` (= `KiSystemStartup`) `+0xe5` 부팅 CPU 칸 10 한계 하위 `0x3c00`, (나) **`SwapContext +0x538 ~ +0x542` 가 스레드마다 칸 10 베이스 ← TEB32 (WoW64) / `UserFsBase` 하위 32비트**, 그 뒤 `FS = 0x53` · `FS_BASE` MSR ← `UserFsBase`, (다) `KiInitializeBootStructures +0x42e` `STAR = 0x0023_0010_…` 가 칸 4 · 5 · 6 순서 (`0x23` / `0x2b` / `0x33`) 를 정한다.
- **C10 예외** `[정적]` — 32비트 코드 (CS `0x20 ~ 0x27`) 에서 난 예외도 **64비트 `KiUserExceptionDispatcher`** (CS `0x33`) 로 간다. WoW64 손질: 정렬 예외 + `EFLAGS.AC` 면 AC 끄고 삼킴 (`+0x74f ~ +0x76f`), CS `0x23` 이면 `0x80000003 → 0x4000001f` · `0x80000004 → 0x4000001e` · `Rsp & 0xfffffff0`.
- **B4 (겸사)** `[정적: PE]` — `PsWowX86SharedInformation` 8칸 = 32비트 ntdll `LdrInitializeThunk` · `KiUserExceptionDispatcher` · `KiUserApcDispatcher` · `KiUserCallbackDispatcher` · `RtlUserThreadStart` · `RtlpQueryProcessDebugInformationRemote` · (`+0x30` 기본 주소) · `LdrSystemDllInitBlock` · `RtlpFreezeTimeBias`.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔일곱째 바퀴. plan **Q253a**. **짐 지도 바퀴 아님.**
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`, 기준과 같음). **다시 analyze 안 함** — `^이름$` export depth 0 (요청서 8개 + 길 따라 22개).
- 버전 대조 (결과서 "버전 확인"): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines`, sast/50 의 `PspSelectMachineForProcess +0x54 ~ +0xd4` — 모두 일치.
- flow-writer 가 다시 본 것 (`.asm`, 결과서 줄과 맞춤): `PspSelectMachineForProcess +0x15 · +0x1f · +0x54 · +0x58 · +0x5e · +0x65 · +0x76 · +0x7b · +0x8c · +0x8f · +0xc0 · +0xd1 · +0xd4 · +0xd8 · +0xf3 · +0xfc · +0x103 · +0x108 · +0x116 · +0x11c`, `PspAllocateProcess +0x6b5 · +0x92a · +0x937 · +0xa87 · +0xaa2 · +0xaa9 · +0xab3 · +0xac5 · +0xac7 · +0xfad · +0xfb6 · +0xfbb · +0xfbe · +0xfc4 · +0xfcc · +0xff6 · +0xffb · +0x100b · +0x100e · +0x1015 · +0x1043 · +0x13ef · +0x13f6`, `MmCreatePeb +0x146 · +0x598 · +0x5a0 · +0x67b`, `MiInitializeWowPeb` 쓰기 줄 전부 (`+0xb8 ~ +0x1ae`) · `+0x5d · +0x6c · +0x73 · +0x78 · +0x303 · +0x369 · +0x3a9 · +0x469 · +0x46b · +0x2e8`, `MiCreatePebOrTeb +0x25 · +0x4d`, `MiAllocateFromSubAllocatedRegion +0x9a · +0xa5 · +0x1e6`, `MiAllocateNewSubAllocatedRegion +0x1f · +0x25 · +0x8b · +0xff · +0x186 · +0x1a4 · +0x212 · +0x236`, `MiGetUserReservationHighestAddress +0x12 · +0x1c`, `PspWow64SetupUserProcessAddressSpace +0x4 ~ +0x42`, `PspWow64ThunkProcessParameters +0x5f · +0x85 · +0xb1 · +0xbb · +0x6fb · +0x70d · +0x721 · +0x740 · +0x753 · +0x761 · +0x76c · +0x770`, `PspSetupUserProcessAddressSpace +0x1e1 · +0x1f2 · +0x20a · +0x214`, `PspPrepareSystemDllInitBlock +0x98 · +0x9f · +0xb7 · +0xc3 · +0xde · +0xef · +0xf7 · +0x2dd · +0x2e2`, `MmCreateTeb +0x58 · +0x6a · +0x85 · +0xa7 · +0x14a ~ +0x1db` (19줄 전부) `· +0x271 · +0x2df · +0x2e9 · +0x2f3`, `PspAllocateThread +0x5d6 · +0x5de · +0x5ea · +0x62c · +0x661 · +0x669 · +0x66f · +0x673 · +0x780 ~ +0x7e3` (전부) `· +0x976 · +0x97e · +0x98a · +0x9a9 · +0x9e6 · +0xb78`, `PspWow64SetupUserStack +0x19 · +0x35 · +0x3e · +0x5c · +0x75 · +0x7b`, `RtlpWow64CreateUserStack +0x58 · +0x5f · +0x8b · +0x8e · +0xc1 · +0x11a · +0x12b · +0x138 · +0x149 · +0x157 · +0x1aa · +0x1bf · +0x1c7 · +0x1fe · +0x20a · +0x213 · +0x24d · +0x259 · +0x2aa`, `PspSetupUserStack +0x89 · +0xa7 · +0xdc · +0x153`, `PspWow64SetupCpuArea +0x54 · +0x83`, `PspWow64InitThread +0x4c · +0x64 · +0x6a · +0x71 · +0x7e · +0x85 · +0x8c · +0x95 · +0xa3 · +0xae · +0xb5 · +0xbf · +0xc7 · +0xdc · +0xe1 · +0xe7 · +0x103 · +0x14d · +0x15d · +0x161`, `PspWow64InitThreadGuestx86` 쓰기 줄 전부 (`+0x14 ~ +0x112`), `entry +0x98 · +0x9f · +0xda · +0xe1 · +0xe5 · +0xea · +0xf4 · +0xf8 · +0x1b5`, `KiInitializeBootStructures +0x42e · +0x433 · +0x438 · +0x43a · +0x44b · +0x45c · +0x468`, `SwapContext +0x138 ~ +0x14f` · 조각 `+0x4fc ~ +0x578` (전부), `KiDispatchException +0x247 · +0x24a · +0x348 · +0x35b · +0x528 · +0x571 · +0x578 · +0x580 · +0x583 · +0x58c · +0x593 · +0x5d1 · +0x5e9 · +0x5ef · +0x5f8 · +0x5fb · +0x5ff · +0x605 · +0x613 · +0x74f · +0x75c · +0x76e · +0x76f`, `MiComputeProcessUserVa +0x54 · +0x6c · +0x8b · +0x91 · +0xa8 · +0xde · +0xf0 · +0x137 · +0x140 · +0x152`, `MiSelectImageBase +0x23 · +0x3a · +0x4b · +0x6c · +0x7c · +0x148 ~ +0x154 · +0x18d · +0x1ab · +0x1cd ~ +0x1d1 · +0x233 ~ +0x26f · +0x274 · +0x280` · 조각 `+0x1b16f1 ~ +0x1b17bf`, `KxContextToKframes +0x91 · +0x9a · +0x9e · +0xa7`, `_calls.tsv` 의 `PspAllocateThread` · `PspSetupUserStack` · `PspWow64InitThread` · `MmCreatePeb` 줄. 어긋난 곳은 아래 "flow-writer 대조".

## 목적

- PE_emulator 갈래 AR: x86 exe 를 **진짜** WoW64 코드로 돌린다. 에뮬레이터는 커널 · 64비트 ntdll 초기화를 흉내 내고 `LdrpInitializeProcess +0x17fe` 부터 진짜 wow64 코드를 돌릴 계획이라, 그 코드가 읽는 상태를 **커널이 진짜 모양대로** 지어 줘야 한다. 요청서 `_workspace/sast/135_wow64-kernel.md` (원문 C 1 ~ 10):
  1. `PspSelectMachineForProcess` 조건 · 출력. 2. `'WowP'` 칸. 3. x86 칸 번호 1 인가. 4. PEB32 자리 · 칸. 5. 32비트 매개변수 블록 · 32비트 시작 블록과의 관계. 6. TEB32 19줄 칸별 · `PspAllocateThread +0x79b ~ +0x7e3`. 7. 32비트 스택 · CPU 영역 · 첫 x86 문맥. 8. 4GB 상한. 9. GDT 칸 4 · 10. 10. 32비트 예외 배달.
- 쓰는 곳 (원문): AR02 (C 1 ~ 3) · AR03 (C 4 · 5 · B 4) · AR04 (C 6 · 7) · AR05 (C 9) · AR07 이후 (C 8 · 10).

## 방법

- 정적 sast/135 (결과서 `_workspace/sast/135_wow64-kernel_result.md`, **분석성공**, 스크립트 문제 없음):
  - ntoskrnl `^이름$` export depth 0 — 요청서 8개 (`MiInitializeWowPeb` · `PspWow64SetupUserProcessAddressSpace` · `PspWow64ThunkProcessParameters` · `PspWow64SetupUserStack` · `RtlpWow64CreateUserStack` · `PspWow64InitThread` · `PspWow64InitThreadGuestx86` · `PspWow64SetupCpuArea`) + 길 따라 22개 (`MiCreatePebOrTeb` · `MiAllocateFromSubAllocatedRegion` · `MiAllocateNewSubAllocatedRegion` · `MiGetUserReservationHighestAddress` · `MiGetImageWowSubsystemIndex` · `MiFindEmptyAddressRange` · `MiSelectUserAddress` · `PspSetupUserStack` · `PsWow64IsMachineSupported` · `PspQueryComPlusRunUnderWoW` · `PsGetProcessMachine` · `RtlpArchContextFlagFromMachine` · `RtlWow64GetCpuAreaEnabledFeatures` · `entry` · `MmMapApiSetView` · `KeCopyXfdMaskToPeb` · `PspMapSiloSharedDataView` · `PspUpdatePebForAffinityChange` · `PspWritePebAffinityInfoAttached` · `PspTrySetProcessPebThrottlingFlags` · `PspGetStandardHandleList` · `MmInitializeProcessAddressSpace`). 있던 `.asm` (`PspSelectMachineForProcess` · `PspAllocateProcess` · `PspAllocateThread` · `MmCreateTeb` · `MiSelectImageBase` · `KiInitializeBootStructures` · `KiDispatchException` · `KxContextToKframes` · `SwapContext` · `MmCreatePeb` · `PspSetupUserProcessAddressSpace` · `PspPrepareSystemDllInitBlock` · `MiComputeProcessUserVa` 등) 은 읽기만.
  - 구조체 칸 이름: PDB (`pdb_struct.py`) — `_EWOW64PROCESS` · `_PEB32` · `_TEB32` · `_TEB` · `_NT_TIB32` · `_EPROCESS` · `_KTHREAD`/`_ETHREAD` · `_KPCR` · `_KGDTENTRY64` · `_PS_NTDLL_EXPORTS` · `_MI_SECTION_WOW_STATE` · `_SECTION_IMAGE_INFORMATION` 있음. **`_WOW64_CPURESERVED` · `_WOW64_CONTEXT` · `_CONTEXT32` · `_SYSTEM_DLL_TYPE` 은 PDB 에 없음** (이 문서에서 x86 `CONTEXT` 칸 이름은 공개 `CONTEXT32` 모양 `[추측: 이름]`).
  - 보조 (결과서 쪽 일회용, 레포에 안 둠): ntoskrnl 전체 objdump 훑기 ("누가 `+0x368` · `+0x310` 을 읽나", `sgdt/lgdt`), PE 바이트 직접 읽기 (`NtdllWowX86Exports` 이름 8개 · `PspSystemDlls` 칸 값 · `MiWowSubsystems6432`).
- 같은 시간에 decompiler 가 SysWOW64 ntdll (sast/137, Q253c) 을 돈다 — 그 결과는 이번에 안 씀.

## 결과

비유 (run138 과 같은 그림):

- **WoW64 = 큰 건물 (64비트) 안 옛날식 사무실 (32비트).** run138 은 통역사 (wow64.dll) 와 사무실 문 (wow64cpu.dll) 이야기였다. 이번엔 **건물주 (커널) 가 사무실을 열기 전에 미리 깔아 두는 것** 이다.
- 건물주는 (1) 입주 신청서를 보고 "옛날식 사무실이 필요하다" 고 도장을 찍고 (기계 고르기 · `'WowP'`), (2) 옛날식 안내판 (PEB32) 과 개업 서류 사본 (32비트 매개변수) 을 낮은 층 (4GB 아래) 에 붙이고, (3) 직원마다 옛날식 이름표 (TEB32) 를 원래 이름표 바로 옆 (`+0x2000`) 에 달고, (4) 옛날식 책상 (32비트 스택) 과 **"처음 출근하면 여기부터" 쪽지 (CPU 영역의 x86 문맥)** 를 둔다. 쪽지 자리는 64비트 이름표 칸 (`TlsSlots[1]`) 에 적어 둔다.
- 사무실 출입증 (GDT 칸 10) 의 주소 칸은 **직원이 바뀔 때마다 건물주가 고쳐 쓴다** (`SwapContext`) `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!PspAllocateProcess` | `+0xa87` | `+0xa8c` | `PspSelectMachineForProcess(부모, CreateInfo, &출력 4B)` | [정적] |
| 〃 | `+0xfc4` | `+0xfc9` | `ExAllocatePool2(0x40, 0x10, 'WowP')` — 지역 플래그 `0x40` 일 때만 (`+0xfad TEST BL,0x40`) | [정적] |
| 〃 | `+0xff6` | `+0xffb` | `PspWow64PickBestNtdll(CreateInfo, 기계)` → `+0xffb [WowP+8]` | [정적] |
| `nt!PspSelectMachineForProcess` (rva `0xa2bb9c`) | `+0x76` · `+0xf3` · `+0x10a860` | `+0x7b` · `+0xf8` · `+0x10a865` | `PsGetProcessMachine(부모)` · `PsWow64IsMachineSupported(기계)` · `PspQueryComPlusRunUnderWoW` (`.NET` 갈래) | [정적] |
| 〃 | `+0x108` | `+0x10d` | 항상 0 을 돌려주는 빈 함수 (`xor eax,eax; ret`, 결과서 objdump `0x140490960`. Ghidra 표시 이름은 ICF 가짜 이름 — 결과서 "스크립트 문제") | [정적] |
| `nt!MmCreatePeb` | `+0x67b` | `+0x680` | `MiInitializeWowPeb(지역 값 묶음, NT 헤더, EPROCESS, 친화도 출력)` — `+0x598 CMP [R13+0x310],0` · `+0x5a0 JNZ` (WoW64 만) | [정적] |
| `nt!MiInitializeWowPeb` (rva `0x930784`) | `+0x5d` | `+0x62` | `MiCreatePebOrTeb(1, …)` → PEB32 자리 | [정적] |
| 〃 | `+0x3a9` | `+0x3ae` | `RtlImageDirectoryEntryToData(이미지, 10 = LOAD_CONFIG)` (x86 · ARMNT 갈래) | [정적] |
| `nt!MiCreatePebOrTeb` | `+0x25` | `+0x2a` | `MiAllocateFromSubAllocatedRegion` | [정적] |
| `nt!MiAllocateFromSubAllocatedRegion` | `+0xa5` · `+0x1e6` | `+0xaa` · `+0x1eb` | `RtlFindClearBitsAndSetEx` · `MiAllocateNewSubAllocatedRegion` (빈 칸 없을 때) | [정적] |
| `nt!MiAllocateNewSubAllocatedRegion` | `+0x8b` · `+0xff` · `+0x186` | `+0x90` · `+0x104` · `+0x18b` | `MiGetUserReservationHighestAddress` · `MiSelectUserAddress` · `ExGenRandom` | [정적] |
| `nt!PspSetupUserProcessAddressSpace` | `+0x1e1` · `+0x1f2` · `+0x20a` · `+0x214` | `+0x1e6` · `+0x1f7` · `+0x20f` · `+0x219` | `PspPrepareSystemDllInitBlock(0)` · **`PspWow64SetupUserProcessAddressSpace`** · `PsWow64GetProcessNtdllType` · `PspPrepareSystemDllInitBlock(종류 1)` | [정적] |
| `nt!PspWow64SetupUserProcessAddressSpace` (rva `0x490f64`) | `+0x42` | `+0x47` | `PspWow64ThunkProcessParameters(PEB32, 커널 사본 매개변수 [ctx+0xd8], &[ctx+0xe8])` | [정적] |
| `nt!PspWow64ThunkProcessParameters` (rva `0x490fc4`) | `+0x85` | `+0x8a` | `ZwAllocateVirtualMemory(-1, …, 0x3000, 4)` — 32비트 블록 | [정적] |
| `nt!PspPrepareSystemDllInitBlock` | `+0xb7` · `+0xc3` · `+0x2dd` | `+0xbc` · `+0xc8` · `+0x2e2` | `PsWow64GetProcessNtdllType` · `PspWow64GetSharedInformation(종류 또는 1)` · `PspWow64GetSharedInformation(1)` (32비트 블록 주소 `[+0x38]`) | [정적] (run32 그대로) |
| `nt!PspAllocateThread` | `+0x5ea` / `+0x9a9` | `+0x5ef` / `+0x9ae` | `PspSetupUserStack` — 보통 / **WoW64 (reserve `0x40000` · commit `0x8000`)** | [정적] |
| 〃 | `+0x9e6` | `+0x9eb` | `PspWow64SetupUserStack(Process, X, InitialTeb, X, 노드)` | [정적] |
| 〃 | `+0x62c` | `+0x631` | `MmCreateTeb` (WoW64 면 종류 5 — TEB64 + TEB32) | [정적] |
| 〃 | `+0xb78` | `+0xb7d` | `PspWow64InitThread(Process, InitialTeb, 문맥64, TEB64)` — `+0x661 CMP [R15+0x310],0` · `+0x669 JNZ` | [정적] |
| `nt!PspSetupUserStack` | `+0x153` | `+0x158` | `PspWow64SetupCpuArea(&InitialTeb.StackBase, Process)` (WoW64 만) | [정적] |
| `nt!PspWow64SetupUserStack` (rva `0xa31390`) | `+0x5c` | `+0x61` | `RtlpWow64CreateUserStack(commit, reserve, ZeroBits, 노드, InitialTeb)` | [정적] |
| `nt!RtlpWow64CreateUserStack` (rva `0xa31440`) | `+0x1aa` · `+0x1fe` · `+0x24d` · `+0x2aa` | `+0x1af` · `+0x203` · `+0x252` · `+0x2af` | `ZwSetInformationProcess(-1, 0x29)` 예약 · `ZwAllocateVirtualMemory` 커밋 · 〃 가드 · `ZwFreeVirtualMemory` (실패) | [정적] |
| `nt!PspWow64InitThread` (rva `0x939140`) | `+0x4c` · `+0x59` · `+0xa3` | `+0x51` · `+0x5e` · `+0xa8` | `PsWow64GetProcessMachine` · `RtlpArchContextFlagFromMachine` · `KiStackAttachProcess` | [정적] |
| 〃 | `+0xc7` · `+0xdc` | `+0xcc` · `+0xe1` | `RtlWow64GetCpuAreaEnabledFeatures` · `RtlInitializeExtendedContext2(영역+4, …)` | [정적] |
| 〃 | `+0x103` | `+0x108` | **`PspWow64InitThreadGuestx86`** (x86 만, `+0xe1 CMP EBX,0x14c`) | [정적] |
| 〃 | `+0x150` · `+0x158` | `+0x155` · `+0x15d` | `PsWow64GetProcessNtdllType` · `PspWow64GetSharedInformation` → `+0x15d [RAX+0x20]` (32비트 `RtlUserThreadStart`) | [정적] |
| 〃 | `+0x122` | `+0x127` | `KiUnstackDetachProcess` | [정적] |
| `nt!entry` (= PDB `KiSystemStartup`) | `+0x1b5` | `+0x1ba` | `KiInitializeBootStructures` (부팅 — 흐름도 범위 앞) | [정적] |
| `nt!MiComputeProcessUserVa` | — | — | (← `MmInitializeProcessAddressSpace +0x136/+0x13b`) `+0xa8` `HighestUserAddress` 쓰기 | [정적] |
| `nt!MiSelectImageBase` | `+0x6c` · `+0x18d` · `+0x1ab` | `+0x71` · `+0x192` · `+0x1b0` | `MiGetImageWowSubsystemIndex` · `RtlFindClearBitsEx` · `MiObtainRelocationBits` | [정적] |

### 1 ~ 3. 기계 고르기 · `'WowP'` · 칸 번호 [정적]

- **보통 x86 exe 길** (부모 x64, 속성 없음, `.NET` 아님): `+0x15 R15W = [CreateInfo+0x68]` (`SECTION_IMAGE_INFORMATION.Machine` — `CreateInfo +0x38` 부터가 `PspAllocateProcess +0x986/+0x98c MmGetSectionInformation(…, 4, …)` 사본) → `+0x54 CMP R15W,R9W` (`+0x1f R9D = 0x14c`) · `+0x58 JZ +0x11c` → `ImageFlags & 3 == 3` 아님 → `+0x5e` 요청 기계 `[+0xa8]` 0 → `+0x76 PsGetProcessMachine(부모)` = `0x8664` → `+0x8f JZ +0xa3` → 고른 기계 = 이미지 `0x14c` → `+0xc0 CMP BX,DX` (`DX = 0x8664`) 다름 → `+0xf3 PsWow64IsMachineSupported(0x14c)` → 참이면 `+0x103 DIL = 1` → **`+0xd1 [out] = 0x14c` · `+0xd4 [out+2] = 1` · `+0xd8 [out+3] = 0`**. 거짓이면 `+0xfc 0xc000007b`.
- `PsWow64IsMachineSupported`: 기계 0 이거나 `RtlWow64GetEquivalentMachineCHPE(기계) ≠ 기계` 면 0. 아니면 시스템 DLL 종류 0 ~ 5 중 첫 dword 가 `& 8` 이고 상위 워드 = 기계인 칸이 있으면 1 → **`PspWowX86SystemDllData` 가 부팅 때 채워져 있어야 x86 이 허용** (run48 `PsLocateSystemDlls` 칸).
- **x64 exe** (test.exe): `+0x54` 에서 안 갈라지고 `+0xc5 ~ +0xd8` 기계 `0x8664`, `+2` = 0, `+3` = 0 (sast/50 과 같음).
- **`.NET` AnyCPU (x86 이미지 + `ImageFlags & 3 == 3`)**: 흩어진 범위 `+0x10a83a ~ +0x10a8e2`. `Prefer32bit` (`[CreateInfo+0x6b] & 0x20`) 거나 요청 · 부모 기계가 `0x14c`/`0x1c4` 면 32비트 (WoW), 아니면 `PspQueryComPlusRunUnderWoW` 가 "WoW 로" 를 말할 때만 WoW, 그 밖엔 **기계 `0x8664` + `+3` = 1 (`OverrideAddressSpace`)**.
- **`PspAllocateProcess` 로 돌아와서**: `+0xaa2` 기계 → `[RSP+0xcc]` → `+0x1043 [EPROCESS+0x6ec]`; `+0xaa9/+0xab3` `+2` ≠ 0 → `OR EBX,0x40`; `+0xac5/+0xac7` `+3` ≠ 0 → `BTS [RSP+0xb0],0x11` → `+0x13ef ~ +0x13f6 OR [EPROCESS+0x1f4]` (`Flags.OverrideAddressSpace`, 비트 17).
- **`'WowP'`**: `+0xfad TEST BL,0x40` (복제 길 부모 `[+0x310] ≠ 0` `+0x92a`, 또는 위 `+0xab3`) → `+0xfb6 EDX=0x10 · +0xfbb ECX=0x40 · +0xfbe R8D='WowP' · +0xfc4 ExAllocatePool2` → `+0xfcc [EPROCESS+0x310] = RAX`.

| `_EWOW64PROCESS` 칸 (PDB) | 값 | 쓰는 줄 | 표시 |
|---|---|---|---|
| (풀) | 0x10 B · 비페이지 · `'WowP'` · 0 채움 (초기화 안 함 플래그 없음) | `PspAllocateProcess +0xfc4` | [정적] (0 채움은 `ExAllocatePool2` 공개 뜻 `[추측]`) |
| `+0x0 Peb` | **1** (임시) | `+0x100b MOV [RAX],R13` (`R13 = 1` `+0x6b5`, 이 길에서 안 바뀜 — 결과서가 눈으로 따라감) | [정적] (R13 추적은 약간 `[추측]`) |
| `+0x0 Peb` | **PEB32 주소** | `MiInitializeWowPeb +0x6c ~ +0x78` | [정적] |
| `+0x8 NtdllType` | `PspWow64PickBestNtdll` 결과 (x86 → **1**), 복제 길이면 부모 값 (`+0x937`) | `+0xfdc ~ +0xff6` 고르기 · `+0xffb` | [정적] |

- **칸 번호 1**: `PspSystemDlls` (rva `0xfd8130`) PE 바이트 = `{Native, WowX86, 0, 0, …, 0}`, 정보 첫 dword Native `0x86640013` · **WowX86 `0x014c0008`**. `PspWow64PickBestNtdll` 일반 반복 (`[CreateInfo+0x11]` 최상위 비트 꺼짐 — 뜻 `[공백]`) 이 `i = 1` 에서 맞는다. 그 비트가 켜지면 플래그 `0xc` 칸만 보는 반복이라 x86 은 0.
- **`Wow64VaSpace4Gb` (`Flags` 비트 9) 를 켜는 곳은 이 함수가 아님**: `PspAllocateProcess +0x100e BT R12D,0x17` · `+0x1015 OR.LOCK [R14+0x1f4],0x200` (`R12D` = `PspAllocateProcess` 의 `Flags` 인자, 비트 23 이름 `[공백]`) 와 `MiComputeProcessUserVa +0xf0` (아래 8).

### 4 ~ 5. PEB32 · 32비트 매개변수 [정적]

- **자리**: `MiInitializeWowPeb +0x469 ~ +0x46b` (x86 · ARMNT 갈래 끝, `JMP +0x58`) → `+0x5d MiCreatePebOrTeb(ECX = 1)` — 종류 1 크기 `0x4b8` (`MiCreatePebOrTeb +0x4d`, 쪽 올림 `0x1000`; 종류 0 PEB64 `0x7d8`, 3/4 TEB `0x1878`, 5 WoW TEB `0x3038` → `0x4000`). → `MiAllocateFromSubAllocatedRegion` 이 프로세스 `[+0x410]` 구조 (`EPROCESS.Vm` 의 `VmWorkingSetList`) 의 목록 (`+0x408` 머리) 에서 `RtlFindClearBitsAndSetEx(비트표, 쪽 수, 힌트)` 로 빈 쪽을 찾고, 없으면 `MiAllocateNewSubAllocatedRegion` — **`'VadS'` VAD 하나, 기본 `0x200` 쪽 (2MB), 실패하면 반씩 줄여 `0x10` 쪽까지**, 상한 `MiGetUserReservationHighestAddress` (`min(0x7ffffffeffff, HighestUserAddress − 1)`), 첫 힌트 `ExGenRandom`.
  - 그래서 PEB64 · PEB32 · TEB 는 **같은 서브 영역에서 차례로** 잘린다. 4GB 아래인 것은 영역이 `HighestUserAddress` 아래라서. PEB32 가 PEB64 바로 다음 쪽인지는 next-fit 이라 보통 그렇다 `[추측: 거리]` (Q262 4).
- **쓰는 칸** (`MiInitializeWowPeb +0xb8 ~ +0x1ae`, `RDX` = PEB32, PDB `_PEB32`) — 대부분 `MmCreatePeb` 가 PEB64 에 쓴 값과 같은 출처:

| PEB32 칸 | 값 / 출처 | 줄 |
|---|---|---|
| `+0x0 InheritedAddressSpace` · `+0x3 BitField` · `+0x4 Mutant` | 견본 (`MmCreatePeb` 둘째 인자) `[+0]` · `[+3]` · `[+8]` 하위 32비트 | `+0x167` · `+0x16c` · `+0x172` |
| `+0x8 ImageBaseAddress` | `EPROCESS.SectionBaseAddress` 하위 32비트 | `+0x178` |
| `+0x64 NumberOfProcessors` · `+0x68 NtGlobalFlag` · `+0x478 NtGlobalFlag2` | 전역 `KeNumberProcessors` · `NtGlobalFlag` · `NtGlobalFlag2` | `+0xf2 ~ +0xf8` · `+0xfb ~ +0x101` · `+0x104 ~ +0x10a` |
| `+0x70 CriticalSectionTimeout` (8B) | 전역 rva `0xe2db90` (이름 `[공백]`) | `+0x110 ~ +0x117` |
| `+0x78 HeapSegmentReserve` · `+0x7c HeapSegmentCommit` | 손질한 값 (`> 0x40000000` → `0x100000`, commit > reserve → `0x2000`, `+0x33c ~ +0x35e`) | `+0xe6 ~ +0xe9` · `+0xec ~ +0xef` |
| `+0x80` · `+0x84 HeapDeCommit*Threshold` | `MmRegistryState+0x38` · `+0x30` | `+0x127 ~ +0x12e` · `+0x134 ~ +0x13b` |
| `+0xa4 OSMajorVersion` · `+0xa8 OSMinorVersion` · `+0xac OSBuildNumber` · `+0xae OSCSDVersion` · `+0xb0 OSPlatformId` | 기본 10 · 0 · 26100 · CSD · 2 — x86 갈래는 **PE32 load config 를 다시 읽어** (`+0x3a9`) 친화도 · CSD 를 32비트 자리에서 가져오고 `Win32VersionValue` 로 버전을 다시 계산 (`+0x404 ~ +0x4c4`) | `+0xb8` · `+0xc1` · `+0xcb` · `+0xdf` · `+0xd5` |
| `+0xb4 ImageSubsystem` · `+0xb8` · `+0xbc ImageSubsystem*Version` | exe 헤더 | `+0x14a ~ +0x15e` |
| `+0x1d4 SessionId` | `PsGetProcessSessionId` | `+0x141 ~ +0x144` |
| `+0x208 MinimumStackCommit` | `MmRegistryState+0x28` | `+0x11b ~ +0x121` |
| `+0x228 ActiveCodePage` · `+0x22a OemCodePage` · `+0x22c UseCaseMapping` | 서버 사일로 NLS (`0x6a5`) | `+0x17f` · `+0x18a` · `+0x195` |
| `+0x470 LeapSecondData` · `+0x474 LeapSecondFlags` | leap second 뷰 하위 32비트 · 0 | `+0x19c ~ +0x1a2` · `+0x1a8 ~ +0x1ae` |

  - 이 함수가 **안 쓰는** 칸: `ProcessParameters +0x10` (아래), `ApiSetMap +0x38` (`MmMapApiSetView`), `SharedData +0x50`, `ActiveProcessAffinityMask +0xc0`, `ExtendedFeatureDisableMask +0x480` (각각 run57 · run59 의 함수들이 WoW64 면 PEB32 에도 씀 — 결과서 "다른 곳"), `Ldr` · `ProcessHeap` · `BeingDebugged` 등 (못 찾음 `[공백]`).
- **32비트 매개변수** (`PspWow64ThunkProcessParameters`): 크기 = `MaximumLength + EnvironmentSize − 0x188` (`+0x1f ~ +0x44`) → `+0x5f R8 = HighestUserAddress − 1` 을 `ZeroBits` 로 `+0x85 ZwAllocateVirtualMemory(…, 0x3000, PAGE_READWRITE)` (새 프로세스에 붙은 채) → 정수 칸은 하위 32비트, **문자열 열 개는 버퍼를 새 블록 `+0x2c8` 뒤에 복사하고 32비트 포인터**, `RuntimeData` (CRT 핸들 상속표) 는 항목 9 B → 5 B 로 줄여 옮김, `Environment` 는 맨 뒤 (`+0x6fb` · `+0x740`) → **`+0x761 PEB32 +0x10 = 블록`** · `+0x76c [ctx+0xe8] = 블록`. 입력은 `[ctx+0xd8]` = **커널이 먼저 잡아 둔 64비트 사본** (유저 사본 `[ctx+0xe0]` 아님). 32비트 칸 이름은 공개 32비트 모양 `[추측: 이름]` (PDB 에 64비트만).
- **순서**: `PspSetupUserProcessAddressSpace +0x1e1` 64비트 시작 블록 (종류 0) → `+0x1f2` 이 함수 (PEB32 하위 32비트도 `+0x35 [ctx+0xec]`) → `+0x20a PsWow64GetProcessNtdllType` (= 1) → `+0x214 PspPrepareSystemDllInitBlock(1)` (32비트 ntdll 의 `LdrSystemDllInitBlock` = `PsWowX86SharedInformation +0x38` 칸, `Size == 0x128` 검사). 두 블록은 서로의 주소를 안 담는다. `[ctx+0xe8]` · `[ctx+0xec]` 를 읽는 곳은 안 따라감 `[공백]` (Q263).

### 6. TEB32 [정적]

- 갈래: `MmCreateTeb +0x58 ~ +0x6a` `[WoW64Process].Peb` ≠ 0 이면 WoW → `+0x2e9 CMP EDI,5` 기계 `0x14c`/`0x1c4` 면 종류 5 (아니면 6 `+0x2df`) → `+0xa7 MiAllocateFromSubAllocatedRegion` (PEB 와 같은 함수 · 같은 서브 영역) 로 4 쪽 → **`+0x14a RCX = TEB64 + 0x2000` (TEB32)**.

| # | 줄 | `_TEB32` 칸 (PDB) | 값 · 출처 |
|---|---|---|---|
| 1 | `+0x156` | `+0x10 NtTib.Version` (FiberData 자리) | `0x1e00` (TEB64 `+0x20` 도 같은 값) |
| 2 | `+0x15a` | `+0x18 NtTib.Self` | TEB32 주소 |
| 3 · 4 | `+0x161` · `+0x167` | `+0x24` · `+0x20 ClientId` | ClientId 하위 32비트 |
| 5 | `+0x16a` | `+0x1d0 TxFsContext` | `0xfffe` |
| 6 · 7 | `+0x175` · `+0x17e` | `+0x6b8` · `+0x6b4 RealClientId` | ClientId (run54 이 이미 이 둘은 TEB32 칸이라 함) |
| 8 · 9 | `+0x184` · `+0x192` | `+0xbfa StaticUnicodeString.MaximumLength` · `+0xbfc Buffer` | `0x20a` · TEB32 `+0xc00` |
| 10 | `+0x198` | `+0x0 NtTib.ExceptionList` | `0xffffffff` |
| 11 | `+0x19b` | `+0x30 ProcessEnvironmentBlock` | **PEB32** (`R13D`) |
| 12 | `+0x19f` | `+0xf70 GdiBatchCount` (PDB 이름) | **TEB64 주소** 하위 32비트 (쌍둥이 찾기 칸으로 보임 `[추측]`) |
| 13 | `+0x1ab` | `+0xf18 Vdm` | TEB64 `+0x1690` (지금 0) |
| 14 · 15 · 16 | `+0x1b4` · `+0x1ba` · `+0x1c0` | `+0x4 StackBase` · `+0x8 StackLimit` · `+0xe0c DeallocationStack` | `InitialTeb +0x28` · `+0x30` · `+0x38` (32비트 스택, 아래 7) |
| 17 | `+0x1ca` | TEB64 `+0x180c WowTebOffset` | `+0x2000` |
| 18 | `+0x1d2` | TEB32 `+0xfdc WowTebOffset` | `−0x2000` |
| 19 | `+0x1d8` | TEB64 `+0x0 NtTib.ExceptionList` | **TEB32 주소** |

- 이 밖에 TEB32 에 쓰는 커널 줄: `PspAllocateThread +0x7db` (`SameTebFlags`). `WOW32Reserved +0xc0` · `+0xc4` (wow64.dll 몫, run121 · run138), `TlsSlots`, `LastErrorValue` 등은 0 으로 시작.
- **`PspAllocateThread +0x793 ~ +0x7e3`**: `+0x66f TEST R12W,R12W` · `+0x673 JNZ +0x780` — 플래그 워드가 0 이 아닐 때만 → `+0x78d` 붙기 → `+0x793 TEB64 +0x17ee` → `+0x79b` WoW64 아니면 `+0x7e3` 로 → `+0x7a4 R12D = 0x8664` · `+0x7aa RAX = TEB64 + 0x2000` · `+0x7b1 R8D = [RSP+0x58]` (같은 플래그) · `+0x7b7 CMP [R15+0x6ec],R12W` → 기계 `0x8664` (ARM64 호스트의 x64 WoW) 면 `+0x7c9 [RAX+0x17ee]`, **x86 이면 `+0x7db [RAX+0xfca]` = TEB32 `SameTebFlags`**.

### 7. 스택 · CPU 영역 · 첫 x86 문맥 [정적]

```
nt!PspAllocateThread   (WoW64 프로세스 — +0x5d6 CMP [R15+0x310],0 · +0x5de JNZ +0x976)
 ├─ (1) +0x9ae  PspSetupUserStack(지역: commit 0x8000 · reserve 0x40000)   64비트 스택 — 호출자 크기 무시
 │              ├─ +0x8e   RtlCreateUserStack
 │              └─ +0x158  PspWow64SetupCpuArea(&InitialTeb.StackBase, P)   맨 위를 잘라 CPU 영역 (StackBase 를 내림)
 │              (+0xa7 ~ +0xdc 문맥64 Rsp = 내린 StackBase − 랜덤 − 0x28)
 ├─ (2) +0x9eb  PspWow64SetupUserStack           호출자 크기는 여기로 (32비트 스택)
 │              └─ +0x61   RtlpWow64CreateUserStack
 │                          ├─ +0x1af ZwSetInformationProcess(-1, 0x29)  자리 = MM 이 정함
 │                          ├─ +0x203 ZwAllocateVirtualMemory  commit (StackLimit32)
 │                          └─ +0x252 ZwAllocateVirtualMemory  가드 0x2000 (0x104)
 ├─ (3) +0x631  MmCreateTeb                     종류 5 — TEB64 + TEB32 (위 6)
 └─ (4) +0xb7d  PspWow64InitThread(P, InitialTeb, 문맥64, TEB64)
                ├─ +0xa8   KiStackAttachProcess   → +0xae [TEB64+0x1488] = CPU 영역 · +0xbf [영역] = 0x014c0000
                ├─ +0xe1   RtlInitializeExtendedContext2(영역+4, 0x10000 …)   x86 CONTEXT (+ 확장 헤더)
                ├─ +0x15d  (보통 시작이면) Eip ← PsWowX86SharedInformation+0x20
                └─ +0x108  PspWow64InitThreadGuestx86   x86 CONTEXT 값 (아래 표)
```

(나무 오프셋은 ret 자리. 실행 순서는 결과서 "`PspAllocateThread` 안 순서" — 스택 둘 → TEB → 초기화.)

- **32비트 스택 크기** (`RtlpWow64CreateUserStack`): 입력 commit · reserve 가 0 이면 exe 의 **PE32 헤더** `SizeOfStackCommit (+0x64)` · `SizeOfStackReserve (+0x60)` (`+0x8b` · `+0x8e`, PE32 아니면 `+0xc1 0xc000007b`) → commit 도 0 이면 `0x4000` (`+0x11a`) → commit ≥ reserve 면 reserve 를 1MB 올림 (`+0x12b`) → commit 쪽 올림 · reserve 64KB 올림 (`+0x138` · `+0x149`) → PEB64 `+0x318 MinimumStackCommit` 이 더 크면 그 값 (`+0x157`).
- **자리**: `+0x1aa ZwSetInformationProcess(-1, 0x29 = ProcessThreadStackAllocation, {노드, reserve, ZeroBits, 출력})` — 64비트 `RtlCreateUserStack` (run74 · run90) 과 같은 꼴. MM 이 `HighestUserAddress` 로 자르는지는 안 따라감 `[공백]` (4GB 아래일 것 `[추측]`, Q263).
- **채움**: `+0x1bf InitialTeb +0x38 = base` · `+0x1c7 +0x28 = base + reserve` (**StackBase32**) · `+0x1fe` 맨 위 commit 커밋 · `+0x213 +0x30 = 커밋 시작` (**StackLimit32**) · reserve − commit ≥ `0x2000` 이면 그 아래 `0x2000` 을 `0x104` 로 (**가드 2 쪽** — 64비트는 3 쪽, run74). 실패면 `+0x2aa ZwFreeVirtualMemory`. 성공하면 `PspWow64SetupUserStack +0x75 ~ +0x7b` 가 입력 구조 첫 바이트 비트 2 를 켬.
- **CPU 영역** (`PspWow64SetupCpuArea +0x54 ~ +0x83`): 기계 → 문맥 플래그 (x86 `0x10000`, 0 이면 `0xc000000d`) → 길이 `L` = 레거시 + 확장 → `StackBase = ((((StackBase − 4) & −4) − 5) & −8) − L` 을 정렬 길이로 내림, `−4`, `& −0x10`. 그다음 `PspSetupUserStack +0xa7 ~ +0xdc` 가 랜덤 `(ExGenRandom & 0x7f) << 4` 와 `0x28` 을 빼서 **문맥64 `Rsp`** — 그래서 64비트 `Rsp` 는 CPU 영역 아래 `[정적: 흐름]`.
- **`PspWow64InitThread`**: `+0x6a/+0x71 RSI = 문맥64.Rip; CMP RSI,[PsNtdllExports+0x10]` — 64비트 `RtlUserThreadStart` 와 같으면 (보통) `+0x14d ~ +0x161 RSI = [PspWow64GetSharedInformation(종류) + 0x20]` = **32비트 `RtlUserThreadStart`**, 다르면 그 `Rip` 그대로. `+0x7e/+0x85 R12 = 문맥64.Rcx` · `R13 = 문맥64.Rdx`, `+0x95 R15 = InitialTeb +0x10` (CPU 영역 시작). `+0xae [TEB64+0x1488] = R15` → **`_TEB.TlsSlots[1]` 가 CPU 영역을 가리킴**. `+0xb5 ~ +0xbf [R15] = 기계 << 16` → **영역 `+0x0` 플래그 워드 0 · `+0x2` 기계 `0x14c`**. `+0xc7` XState 기능 (KUSD `+0x3d8`) 이 남으면 플래그 `|= 0x10040`. `+0xe1 CMP EBX,0x14c` 아니면 `0xc000000d` (ARMNT 거절).
- **x86 `CONTEXT`** (`PspWow64InitThreadGuestx86`, `RBP` = 영역 + 4 + `CONTEXT_EX.Legacy.Offset`, 칸 이름 `[추측: 공개 CONTEXT32]`):

| 칸 | 값 | 줄 |
|---|---|---|
| `+0x0 ContextFlags` | `0x1003f` 를 OR 로 켬 (`CONTEXT_i386` 전부 + 확장 레지스터) | `+0xe4` |
| `+0x4 ~ +0x18 Dr0 ~ Dr7` | 0 | `+0xcc ~ +0xe0` |
| `+0x1c FloatSave` (`0x70`) | 0, `ControlWord 0x27f` · `TagWord 0xffff` | `+0x7e ~ +0xa2` · `+0xb8` · `+0xbd` |
| `+0x8c SegGs` · `+0x94 SegEs` · `+0x98 SegDs` · `+0xc8 SegSs` | **`0x2b`** | `+0x72` · `+0x78` · `+0x82` · `+0x88` |
| `+0x90 SegFs` | **`0x53`** | `+0x8e` |
| `+0xbc SegCs` | **`0x23`** | `+0x98` |
| `+0xa4 Ebx` | 문맥64 `Rdx` (PEB — WoW64 면 32비트 값, run16) | `+0x4f ~ +0x59` |
| `+0xb0 Eax` | 문맥64 `Rcx` (exe 진입점) | `+0x3e ~ +0x49` |
| `+0xb8 Eip` | **32비트 `RtlUserThreadStart`** (또는 문맥64 `Rip`) | `+0x6c` |
| `+0xc4 Esp` | **`StackBase32 − (ExGenRandom & 0x1ff) × 4 − 0x10`** (스택 랜덤화 꺼짐 `MitigationFlags & 0x40` 이면 랜덤 0) | `+0x29` · `+0x42` · `+0x56` · `+0x64` |
| `+0xc0 EFlags` | `0x202` | `+0x108` |
| `+0xcc ExtendedRegisters` (`0x200`) | 0, FCW `0x27f` · MXCSR `0x1f80` (`+0xe4`) · MXCSR 마스크 `0xffff` (`+0xe8`) | `+0xac ~ +0x102` |
| `Edi · Esi · Ecx · Edx · Ebp` | 안 씀 (새 스택 쪽이라 0 `[추측]`) | — |

- **64비트 문맥은 읽기만** (`[RBX+0x80/0x88/0xf8]`). 스레드는 **64비트 모드 (CS `0x33`) 로 시작** (`KxContextToKframes +0x9a ~ +0xa7`, run29) → 64비트 ntdll `LdrInitializeThunk` → `UseWOW64` → `wow64!Wow64LdrpInitialize` 가 이 CPU 영역을 읽어 32비트로 넘어간다 (run138).

### 8. 4GB 상한 [정적]

- **쓰는 곳** `MiComputeProcessUserVa` (← `MmInitializeProcessAddressSpace +0x136/+0x13b`): 이미지 섹션이고 `OverrideAddressSpace` (`+0x6c BT R8D,0x11`) 가 꺼져 있을 때만 줄임. `+0x8b` `ImageCharacteristics & LARGE_ADDRESS_AWARE` · `+0x91` WoW64 · 기계 `0x14c`/`0x1c4` → **`+0xde ~ +0xf0` `0xffff0000` + `Wow64VaSpace4Gb` 켬**. LAA 아니면 `+0x137 TEST EDX,R8D` (**비트를 읽는 유일한 줄**) — 이미 켜져 있으면 (`PspAllocateProcess +0x1015`) 위 갈래, 아니면 `+0x140 ~ +0x152` **`0x7fff0000`** (플래그 안 켬). `+0xa8 [EPROCESS+0x368] = RDI`.
- **읽는 곳** (결과서 표 — `.asm` 줄 + objdump 훑기): `MiGetUserReservationHighestAddress +0x12` (→ PEB/TEB 서브 영역 `MiAllocateNewSubAllocatedRegion +0x8b`), `MiAllocateVirtualMemoryPrepare +0x3ac · +0x517` (`NtAllocateVirtualMemory` 상한), `MiSelectUserAddress +0x9d` (≤ 4GB 면 작은 공간 갈래), `MiFindEmptyAddressRange +0x2c1`, `MiInsertVad +0x5c`, `MmMapViewOfSection +0xdf · +0x198`, `NtMapViewOfSection +0x1d1 · +0x555`, `PspWow64ThunkProcessParameters +0x5f` 외 (줄만 — `.asm` 없는 곳은 결과서 objdump). 위를 막는 상태 코드는 안 봄 `[공백]`.
- **이미지** (`MiSelectImageBase`): `+0x23 BT [이미지 정보+0xc],0xf` (WoW 이미지) 면 `+0x6c MiGetImageWowSubsystemIndex` (표 `MiWowSubsystems6432` = `{0x014c}`, PE 바이트) → x86 이면 ESI = 3 → `+0x151 CMP ESI,3` · `+0x154 JZ +0x252` → 겹침표 `0x140e2d7a8` (`_MI_SECTION_WOW_STATE` 꼴 `[추측]`) · **천장 `0x78000000`** (`+0x26a`) → `+0x18d RtlFindClearBitsEx` · `+0x1ab MiObtainRelocationBits` → 베이스 = 천장 − (비트 + 개수) × `0x10000` (`+0x1cd ~ +0x1d1`). 곧 x86 이미지가 **선호 주소에 못 앉을 때** 1.875GB 아래에서 고른다. ESI = 2 (`BaseBelow4gb` DLL) 는 두 표를 합쳐 고름 (조각 `+0x1b16f1 ~`).

### 9. GDT [정적]

- **칸 4 (`0x20`) 바이트 · 칸 10 (`0x50`) 의 접근 바이트 · 플래그 · 한계 상위를 쓰는 줄은 ntoskrnl 에 없다** — `sgdt/lgdt` 가 든 함수 7곳 (`entry +0x98` · `KiGetGdtIdt` · `KiSaveInitialProcessorControlState` · `KiSaveProcessorControlState` · `KiRestoreProcessorControlState` · `HalpLMStub` · `KiNmiInterrupt`/`KiMcheckAbort` 읽기만) 과 `[GDT+0x52/+0x54/+0x57]` 쓰기 (`SwapContext` 조각 하나) 를 훑은 결과 (결과서). → **로더 (`winload`) 가 준 GDT 그대로** `[공백]` (Q262 8 `dq @gdtr`).
- (가) **`entry` (= `KiSystemStartup`)** `+0x98 SGDT` · `+0x9f R8 = GDT 베이스` · `+0xda CMP [Prcb+0x24],0` (`Number` 0 = 부팅 CPU) → `+0xe1 AX = 0x3c00` · **`+0xe5 [GDT+0x50] = 0x3c00`** (칸 10 `LimitLow`). 이어 `+0xea DS/ES = 0x2b` · `+0xf4 ~ +0xf8 FS = 0x53`. 다른 CPU 의 GDT 는 안 따라감 `[공백]` (Q263).
- (나) **`SwapContext`** (조각, `.asm` 의 `(= SwapContext +…)` 표기 — 함수 시작에서 잰 거리): 새 스레드 `RSI`, PRCB `RBX`.
  - `+0x504 BT [RSI+0x74],0xa` · `+0x509 JC` — 시스템 스레드면 통째로 건너뜀.
  - `+0x50f ~ +0x51e` 프로세스 `[+0x310]` 검사 · `EAX = [RSI+0x678]` (**`ETHREAD.UserFsBase`** 하위 32비트), WoW64 면 `+0x526 ~ +0x52c EAX = KTHREAD.Teb (+0xf0) + 0x2000` (= **TEB32**).
  - `+0x531 RCX = [RBX−0x180]` (`KPCR.GdtBase`) → **`+0x538 [RCX+0x52] = AX` · `+0x53f [RCX+0x54] = AL` · `+0x542 [RCX+0x57] = AH`** = 칸 10 베이스 하위 32비트.
  - `+0x545 ~ +0x54a FS = 0x53` → `+0x54c ~ +0x55d IA32_FS_BASE (0xc0000100) ← UserFsBase` (FS 를 올리면 베이스가 서술자 값으로 덮이므로 되돌림). 이어 `+0x55f ~` DS/ES/GS 를 `0x2b` 로, `KERNEL_GS_BASE ← KTHREAD.Teb` (TEB64).
  - 나가는 쪽: `+0x138 CMP [이전 프로세스+0x310],0` → WoW64 면 `+0x142 ~ +0x14f RDMSR 0xc0000100 → [RDI+0x678/0x67c]` (FS 베이스를 `UserFsBase` 에 저장).
  - 정리: **WoW64 스레드는 칸 10 베이스 = TEB32 (32비트 코드의 `FS:[0]`), `FS_BASE` MSR = `UserFsBase`** (64비트 모드에서 쓰는 값). 64비트 스레드도 칸 10 베이스에 `UserFsBase` 하위 32비트가 들어가지만 셀렉터 `0x53` 을 안 쓰니 무의미 `[추측]`.
- (다) **`KiInitializeBootStructures +0x42e ~ +0x468`**: `+0x42e EDX = 0x230010` · `+0x438 WRMSR 0xc0000081` → **`STAR = 0x0023_0010_0000_0000`** — `SYSRET` 이 64비트로 갈 때 CS `0x23 + 0x10 = 0x33` (칸 6) · SS `0x23 + 8 = 0x2b` (칸 5), 32비트로 갈 때 CS `0x23` (칸 4). 이어 `CSTAR = KiSystemCall32` (KVA 섀도면 `…Shadow`), `LSTAR = KiSystemCall64`, `FMASK = 0x4700` (`+0x45c ~ +0x468`). → **칸 4 · 5 · 6 순서가 이 한 줄에 박혀 있다.**
- `0x23` 이 코드에 나오는 곳: `PspWow64InitThreadGuestx86 +0x98` · `KxContextToKframes +0x9e` · `KiDispatchException +0x5fb` (CS `& 0xfff8 == 0x20`). `0x53`: `entry +0xf4` · `SwapContext +0x545` · `PspWow64InitThreadGuestx86 +0x8e`.

### 10. 32비트 코드의 예외 [정적]

- `KiDispatchException +0x247 TEST DIL,AL` · `+0x24a JZ +0x5d1` — 유저 모드 + 최소 프로세스 (`Flags3` 비트 0) 아님 → `+0x5d1`.
- `+0x5d1 ~ +0x5e9` 프로세스 `[+0x310] ≠ 0` → `+0x74f CMP [R14],0x80000002` (정렬) 이고 `+0x75c` 트랩 `EFlags & 0x40000` (**AC**) 면 `+0x76e CLI · +0x76f BTR …,0x12` 로 AC 끄고 **예외를 삼키고 계속**. 아니면 `+0x5ef` 로.
- `+0x5ef ~ +0x5ff` 문맥 `SegCs & 0xfff8 == 0x20` 일 때만 (32비트 코드): `+0x605` `0x80000003` → `0x4000001f`, `+0x613` `0x80000004` → `0x4000001e` (`STATUS_WX86_SINGLE_STEP`), 그 뒤 `R15 &= 0xfffffff0` (조각 `+0x2fe83d ~ +0x2fe845`) → `+0x250` 공통 길.
- **배달 대상은 늘 64비트**: `+0x58c RAX = [PsNtdllExports+0x20]` (`KiUserExceptionDispatcher`) · `+0x593` 트랩 `Rip` · `+0x580/+0x583 SegCs = RBX + 0x32` (`+0x528 EBX = 1` → `0x33`) · `+0x571/+0x578 Rsp = RDI − 0x4d0`. `PsWowX86SharedInformation +0x8` (32비트 `KiUserExceptionDispatcher`) 는 이 함수에서 안 읽는다 — 32비트 처리기로는 wow64 가 `Wow64PrepareForException` 으로 넘긴다 (run82 · run138).
- `+0x348 PsWow64GetProcessMachine == 0x14c` 면 `+0x35b AND RDI,-0x60001` (뜻 `[공백]`). 예외 틀이 어느 스택에 놓이나 (`RDI` 출처) 는 안 따라감 `[공백]` (`R15 & 0xfffffff0` 으로 보아 32비트 스택 쪽일 수 `[추측]`).

### 표 B-0. `PsWowX86SharedInformation` 8칸 (원문 B 4) [정적: PE]

`NtdllExportInformation[1] = {NtdllWowX86Exports (rva 0xc801a0), 8}` (sast/11 · run48), 항목 16 B `{이름, 칸}` — 결과서가 PE 바이트를 읽음:

| 칸 (rva `0xfda900` +) | 32비트 ntdll 이름 | 커널 안에서 읽는 곳 |
|---|---|---|
| `+0x00` | `LdrInitializeThunk` | (아래 "이어 읽기" — 64비트 시작 블록 `+0x18`) |
| `+0x08` | `KiUserExceptionDispatcher` | 안 읽음 (C10) |
| `+0x10` | `KiUserApcDispatcher` | |
| `+0x18` | `KiUserCallbackDispatcher` | |
| `+0x20` | **`RtlUserThreadStart`** | `PspWow64InitThread +0x15d` → x86 `Eip` |
| `+0x28` | `RtlpQueryProcessDebugInformationRemote` | |
| `+0x30` | (이름 없음 — WOW ntdll 기본 주소, run48) | |
| `+0x38` | **`LdrSystemDllInitBlock`** | `PspPrepareSystemDllInitBlock +0x2e2` (32비트 시작 블록 주소) |
| `+0x40` | `RtlpFreezeTimeBias` | |

- 16칸 중 나머지 `+0x48 ~ +0x78` 은 0. `PspPrepareSystemDllInitBlock +0xde ~ +0xf7` 이 16칸을 시작 블록 `+0x18` 부터 통째 복사 — **종류 0 (64비트 블록) 에서도** (`+0xb7 ~ +0xc3`, 종류 0 이면 1 로 바꿔 `PspWow64GetSharedInformation(1)`, run32 줄 그대로). 그래서 x64 프로세스의 64비트 ntdll 블록도 이 표를 갖는다.

### 호출 관계 (한 장)

```
nt!PspAllocateProcess                                                      [정적]
 ├─ (1) +0xa8c  PspSelectMachineForProcess   → 출력 {0x14c, 1, 0}  (+0xf8 PsWow64IsMachineSupported)
 ├─ (2) +0xfc9  ExAllocatePool2('WowP', 0x10)   → EPROCESS+0x310 · +0x100b Peb = 1
 ├─ (3) +0xffb  PspWow64PickBestNtdll         → NtdllType = 1
 │       +0x1015 Wow64VaSpace4Gb (Flags 비트 23 일 때) · +0x1043 Machine
 ├─ (4) MmInitializeProcessAddressSpace … MiComputeProcessUserVa +0xa8  HighestUserAddress 0xffff0000 / 0x7fff0000
 ├─ (5) +0x2314 MmCreatePeb
 │              └─ +0x680  MiInitializeWowPeb
 │                          └─ +0x62  MiCreatePebOrTeb(1)  → +0x2a MiAllocateFromSubAllocatedRegion   PEB32 · 27칸 · Peb ← 주소
 └─ (6) +0x239c PspSetupUserProcessAddressSpace
                ├─ (1) +0x1e6  PspPrepareSystemDllInitBlock(0)      64비트 블록 (+0x18 ← WOW 16칸)
                ├─ (2) +0x1f7  PspWow64SetupUserProcessAddressSpace
                │              └─ +0x47  PspWow64ThunkProcessParameters   +0x8a ZwAllocateVirtualMemory · +0x761 PEB32+0x10
                └─ (3) +0x219  PspPrepareSystemDllInitBlock(1)      32비트 블록 (= WOW 표 +0x38)
nt!PspAllocateThread  (위 7 나무)  64비트 스택 + CPU 영역 → 32비트 스택 → MmCreateTeb (TEB32 19줄) → +0x7db SameTebFlags → PspWow64InitThread
nt!SwapContext  (스레드마다)  +0x538 ~ +0x542 GDT 칸 10 베이스 ← TEB32 · +0x54a FS 0x53 · +0x55d FS_BASE ← UserFsBase
```

(오프셋은 ret 자리. `PspAllocateProcess` 의 `(4)` 는 `+0x2095` 사슬 안 — run10.)

## 확인된 것

- x86 exe 를 WoW64 로 정하는 조건 · 출력 · `EPROCESS` 칸 셋 (`Machine` · `WoW64Process` · `Flags.OverrideAddressSpace`) 과 `Wow64VaSpace4Gb` 를 켜는 두 곳 — `[정적]` (위 1 ~ 3).
- `_EWOW64PROCESS` = 0x10 B `{Peb, NtdllType}`, `Peb` 1 → PEB32, x86 `NtdllType` = 1 — `[정적]`.
- PEB32 · TEB (64 + 32) 는 같은 서브 영역 (2MB `'VadS'`) 에서 잘리고, 그 영역 상한이 4GB 아래 상한의 이유 — `[정적]`. `MmCreateTeb +0xa7` 의 `MiAllocateFromSubAllocatedRegion` 은 이름 그대로 "서브 할당 영역에서 쪽 받기" (run43 `[추측: 이름]` 닫힘).
- PEB32 27칸 · TEB32 19줄 칸 이름 · 32비트 매개변수 블록의 자리와 꼴 · x86 첫 문맥 전 칸 — `[정적]`.
- **CPU 영역은 커널이 짓는다**: 자리 `PspWow64SetupCpuArea` (64비트 스택 맨 위), 가리키는 칸 `PspWow64InitThread +0xae` (TEB64 `+0x1488`), 머리 `+0xbf` (`0x014c0000`), x86 `CONTEXT` `+0x4` — `[정적]`. run138 의 "커널이 지음 `[추측]`" 닫힘.
- GDT 칸 10 베이스를 스레드마다 TEB32 로 고치는 줄 · STAR 의 셀렉터 순서 — `[정적]`. 칸 4 바이트는 ntoskrnl 에 없다 — `[정적: 훑기]` (없음을 본 것, 값은 `[공백]`).
- 32비트 코드 예외도 64비트 `KiUserExceptionDispatcher` 로 — `[정적]`.
- `PsWowX86SharedInformation` 8칸 이름 — `[정적: PE]`.

## 앞 run 과 이어 읽기 (모두 맞음 — 고친 줄 없음)

- **run138 (wow64 DLL)** 과 맞물림:
  - run138 CPU 영역 꼴 `{+0x0 플래그 WORD, +0x2 Machine 0x14c} + +0x4 x86 CONTEXT` = 이번 `PspWow64InitThread +0xbf` (`[R15] = 0x14c << 16`) · `+0xdc RtlInitializeExtendedContext2(R15+4, …)` — **같다**. 플래그 처음 값 0 (run138 "처음 값 = 커널 `[추측]`") → `[정적]` 0.
  - run138 첫 진입 `IRETQ {Eip, 0x23, EFlags, Esp, 0x2b}` · FS `0x53` = 이번 x86 문맥 `SegCs 0x23` · `SegSs 0x2b` · `SegFs 0x53` 과 맞는다.
  - run138 `Wow64LdrpInitialize` 가 `Eip` 를 64비트 ntdll `LdrSystemDllInitBlock +0x18` 값 (wow64 이름 `Ntdll32LoaderInitRoutine`) 으로 바꾼다 → 그 칸 = 시작 블록 `+0x18` = `PsWowX86SharedInformation +0x00` 사본 = **32비트 `LdrInitializeThunk`** (표 B-0 + run32 복사 줄) `[정적: 두 결과서 이어 읽기]`. 커널이 놓은 `Eip` (32비트 `RtlUserThreadStart`) 를 wow64 가 로더 입구로 바꿔 끼우는 꼴이 **64비트 쪽 (커널 `RtlUserThreadStart` → `PspInitializeThunkContext` 가 `LdrInitializeThunk`, run16)** 과 같다 `[추측: 비교]`.
  - run138 `Wow64Info` = PEB32 `+0x490` (≥ `0x24` B) — PEB32 할당 크기 `0x4b8` (`MiCreatePebOrTeb +0x4d`) 안에 든다 (`0x490 + 0x28 = 0x4b8`) `[추측: 크기만 맞춰 봄]`. 커널이 그 칸에 쓰는 줄은 `MiInitializeWowPeb` 에 없다 (마지막 쓰기 `+0x478`).
- **run16** "`Rdx` ← PEB (WoW64 면 32비트 값)" → x86 `Ebx` 로 간다 (`Guestx86 +0x4f ~ +0x59`). run16 의 "`PspWow64InitThread +0x71` 도 `PsNtdllExports+0x10` 을 읽는다" 는 **비교** 줄이다 (64비트 `RtlUserThreadStart` 로 시작하는 문맥이면 32비트 것으로 바꿈).
- **run29** `KxContextToKframes` CS `0x33`/`0x23` · SS `0x2b` — WoW64 첫 스레드도 64비트 문맥 (`0x33`) 으로 출발.
- **run32** `PspPrepareSystemDllInitBlock` 종류 1 갈래 `+0x2dd/+0x2e2` · 16칸 복사 `+0xef` — 결과서 `+0xde ~ +0xf7` 과 같은 고리.
- **run48** 칸 플래그 `0x8` = "WOW" — `PsWow64IsMachineSupported` · `PspWow64PickBestNtdll` 이 이 비트로 고른다.
- **run54** `MmCreateTeb +0x175 · +0x17e` = `_TEB32.RealClientId` — 이번 표 6 · 7 과 같다.
- **run79** `PspExitThread` WoW64 32비트 스택 풀기 `[TEB+0x2e0c]` = TEB32 (`+0x2000`) `+0xe0c DeallocationStack` (이번 표 16) · `MmDeleteTeb +0xa7 0x4000` = 종류 5 크기와 맞는다.
- **run81** `KiDispatchException +0x5d1 ~ +0x62a` "WoW64 · 최소 프로세스 — 이름만" → 위 10 으로 몸이 채워짐 (run81 문서는 `[추측]` 이 아니라 "이름만" 이라 고쳐 읽기는 안 넣음).
- **run75** `SwapContext +0x545 ~ +0x55d` FS `0x53` · `UserFsBase` — 그 바로 앞 `+0x538 ~ +0x542` 칸 10 베이스 쓰기가 이번에 보탬.
- **run50 · 짐 지도 3.11** `Machine` · `WoW64Process` · `Flags.Wow64VaSpace4Gb` · `OverrideAddressSpace` · `HighestUserAddress` 줄 — 줄 자리가 같다. 다만 짐 지도 `Wow64VaSpace4Gb` 줄의 조건 글 "32비트 주소 공간일 때만" 은 **"LAA (4GB) 이거나 `Flags` 인자 비트 23 일 때"** 가 정확하다 (LAA 아닌 x86 은 2GB 이고 플래그를 안 켬) — 짐 지도 바퀴 때 고칠 감 (이번엔 안 고침).

## flow-writer 대조 (결과서와 `.asm` 이 조금 다르게 적힌 곳 — 뜻은 같음)

- `PspSelectMachineForProcess +0x54` 는 `CMP R15W,R9W` (`+0x1f R9D = 0x14c`), `+0xc0` 은 `CMP BX,DX` (`+0x65` · `+0x7b EDX = 0x8664`) — 결과서는 상수로 풀어 씀.
- `PspAllocateThread +0x7b7 CMP [R15+0x6ec],R12W` 앞에 **`+0x7a4 MOV R12D,0x8664`** 가 있다 (결과서는 `CMP …,0x8664` 로 줄임. 그 전 `R12W` 는 플래그 워드라 이 줄이 없으면 헷갈림).
- `MiSelectImageBase +0x148` · `+0x151` 은 `CMP ESI,2` · `CMP ESI,3`, 갈라지는 `JZ` 는 **`+0x14b` · `+0x154`**. `MiObtainRelocationBits` 는 call **`+0x1ab`** / ret `+0x1b0` (결과서 `+0x1a2` 는 그 앞 `LEA`). `RtlFindClearBitsEx` 는 call `+0x18d` / ret `+0x192` (결과서 `+0x192` = ret).
- `SwapContext +0x504 ~ +0x578` 은 `.asm` 의 조각 (`frag:FUN_1406b686f+…`) 을 함수 시작에서 잰 거리 (`(= SwapContext +…)`). `UserFsBase` 읽기는 정확히 `+0x51e` (`+0x50f` 는 프로세스 포인터, `+0x516` WoW64 검사). WinDbg 가 이 조각을 `SwapContext+0x5xx` 로 보일지는 사이에 다른 공개 이름이 없어야 함 `[추측]`.
- `KiDispatchException +0x578 [R12+0x180] = R10` — `R10 = RDI − 0x4d0` 은 `+0x571 LEA`.
- `MiInitializeWowPeb +0xe6` · `+0xec` 는 읽기 줄 (쓰기는 바로 뒤 — 결과서 범위 `+0xe6 ~ +0xe9` · `+0xec ~ +0xef` 와 같음). `PspWow64InitThread +0x14d` 는 범위 시작 (`MOV RCX,R14`), 실제 칸 읽기는 `+0x15d`.
- 결과서 표 "칸 표 요약" 의 `MiInitializeWowPeb +0x5d ~ +0x1ae` 는 자리 + 쓰기 범위를 한 칸에 적은 것.
- 그 밖 위 "환경" 의 줄 모두 결과서와 같다.

## 짐 지도 (`baggage.md`) 에 넣을지 — 판단만 (이번엔 안 고침)

- 이번에 커널이 짓는다고 확인된 **WoW64 짐** — 유저: PEB32 (1 쪽 · 27칸) · 32비트 매개변수 블록 · TEB32 (19 + 1칸) · 32비트 스택 (예약 · 커밋 · 가드) · 64비트 스택 (고정 크기) · CPU 영역 (머리 + x86 `CONTEXT`) · TEB64 `TlsSlots[1]` · 32비트 `LdrSystemDllInitBlock`. 커널: `_EWOW64PROCESS` (0x10 B) · `EPROCESS` 다섯 칸 · GDT 칸 10 베이스 · `FS_BASE` MSR. 결과서 "칸 표 요약" 이 이미 짐 지도 꼴 (공간 · 단계 · 놓는 법 · 쓰는 줄) 이다.
- **판단: 넣는 게 맞다 — 다만 3장 (test.exe 짐 표) 이 아니라 7.6 에 새 절 하나 ("WoW64 x86 프로세스일 때만 놓이는 짐 — 조건부 갈래") 로.** 이유:
  - 짐 지도 3장 · 2장 숫자는 **test.exe (x64) 에 실제로 놓인 칸** 을 run44 사진으로 센 것이다. test.exe 는 WoW64 를 안 타서 (`WoW64Process` null `[관찰]`) 이 짐들은 사진에 없다 → 3장에 넣으면 2장 숫자 · 7.1 "0 그대로" 셈과 섞인다.
  - 그래도 CLAUDE.md "구현할지로 거르지 않는다" 와 PE_emulator-AR 이 이 목록을 쓴다는 점 → 목록에는 있어야 한다. 7.6 은 이미 "칸 표가 아직 없는 짐 · 범위 밖 짐" (부팅 · csrss · dll CRT) 을 받는 자리다.
  - 3.11 `EPROCESS` 의 WoW64 다섯 줄은 이미 "조건부, 안 탐" 으로 있다 → 그 줄 조건 글만 위 "이어 읽기" 대로 고치면 된다 (`Wow64VaSpace4Gb`).
- 고칠지 · 언제 (짐 지도 바퀴로) 는 오케스트레이터 · 사용자 결정.

## 정정

- 없음 (앞 run 이 틀린 줄은 못 찾음). 닫힌 `[추측]` 만 고쳐 읽기 한 줄씩: **run43** (`MiAllocateFromSubAllocatedRegion` "TEB 자리 `[추측: 이름]`"), **run48** (`PsWowX86SharedInformation` "복사하는 것으로 읽힘 `[추측]`"), **run89** (`PspSelectMachineForProcess` "하는 일 `[추측]`" · "커널이 CLR 헤더 없는 PE32 를 64비트로 안 띄운다 `[추측]`"), **run138** (CPU 영역 "커널이 지음 `[추측]`").
  - run89 쪽은 **절반만** 닫힘: 커널은 x86 이미지를 `ImageFlags & 3 == 3` 일 때만 64비트로 돌린다 `[정적]`. CLR 헤더 없는 PE32 에서 그 두 비트가 꺼져 있는지 (`ImageFlags` 를 채우는 MM 줄) 는 안 봄 `[공백]`.

## 아직 확인 필요

- **동적 → Q262** (결과서 "동적으로 확인할 것" 1 ~ 11 — Q253d · Q260 VM 과 합침, x86 시험 exe 필요 — LAA 켠 것 · 끈 것):
  1. `nt!MiComputeProcessUserVa+0xaf` — `? poi(@rbx+0x368)` (`0xffff0000` / `0x7fff0000`), `dd @rbx+0x1f4 L1` 비트 9.
  2. `nt!PspSelectMachineForProcess+0xd1` — `r bx` · `r dil` · `r cl`, `+0xfc` 에 안 걸림, `dw @rbp+0xa8 L1` · `[CreateInfo+0x11]` 비트 7.
  3. `nt!PspAllocateProcess+0xfcc` `!pool @rax` · `dq @rax L2`, `+0xffb` 뒤 `[R10+8]` = 1.
  4. `nt!MiInitializeWowPeb+0x7b` PEB32 주소 · PEB64 와의 거리, `+0x2e8` 에서 `dt nt!_PEB32`, `!vad` 의 `'VadS'` 2MB.
  5. `nt!PspWow64ThunkProcessParameters+0x765` — `dd @r15+0x10 L1`, 블록 크기.
  6. `nt!MmCreateTeb+0x1db` — `dt nt!_TEB32 @rcx`, `dq @rbx L1`. 첫 스레드 · 둘째 스레드 비교.
  7. `nt!RtlpWow64CreateUserStack+0x259` — `dq <InitialTeb>+0x28 L3`; `nt!PspWow64InitThread+0xb5` `? @r15` (= TEB64 `+0x1488`); `nt!PspWow64InitThreadGuestx86+0x112` x86 `CONTEXT` 전체 — `Eip` = 32비트 `ntdll!RtlUserThreadStart`, `Esp` 범위, 문맥64 `Rsp` 가 CPU 영역 아래.
  8. `dq @gdtr+0x20 L1` · `dq @gdtr+0x50 L2` (칸 4 · 10 바이트), `nt!SwapContext+0x545` 에서 (WoW64 스레드) 칸 10 = TEB32, `r fs`, `rdmsr c0000100`, `dt nt!_ETHREAD UserFsBase`.
  9. x86 시험 exe 에서 `int 3` · 정렬 위반 (AC 켬) → `nt!KiDispatchException+0x5d1` · `+0x74f` · `+0x58c`, 코드 `0x4000001f`, 도착 `Rip` = 64비트 `ntdll!KiUserExceptionDispatcher`, `Rsp` 가 어느 스택.
  10. `dq nt!PsWowX86SharedInformation L9` · `dq nt!PspSystemDlls L6`, `nt!PspWow64InitThread+0x15d` `rsi`.
  11. `LdrpInitializeProcess +0x17fe` 가 돌아오나 — Q260 2 와 같은 줄.
- **정적 → Q263** (결과서 "다음 요청 후보"): PEB32 를 쓰는 나머지 곳 (`PspGetStandardHandleList +0x12` · `PspTrySetProcessPebThrottlingFlags +0x4b` · `PsQueryProcessCommandLine +0xab` · `NtQueryInformationProcess +0x16a5/+0x19b8` 등 — 칸 이름만), `MiSelectImageBase` WoW 겹침표 셋의 PDB 이름 · `MiObtainRelocationBits`, `ZwSetInformationProcess(ProcessThreadStackAllocation)` 의 MM 쪽 (32비트 스택 자리 상한), 다른 CPU 의 GDT (`KeStartAllProcessors` · `KiInitializeProcessor`), `[ctx+0xe8]` · `[ctx+0xec]` 를 읽는 곳, `PS_ATTRIBUTE` 번호 (`CreateInfo +0xa8` / `+0x1e8`) · `Flags` 비트 23 이름, 그리고 `[공백]` GDT 칸 4 · 칸 10 나머지 바이트 (로더 몫 — 정적으로는 winload, 아니면 Q262 8).
- 그 밖 `[공백]`: `[CreateInfo+0x11]` 비트 7 뜻, `CriticalSectionTimeout` 전역 (rva `0xe2db90`) 이름, `KiDispatchException +0x35b` 비트 `0x60001` 뜻 · 예외 틀 스택, `NtAllocateVirtualMemory` 가 상한 위를 막을 때 상태 코드, wow64cpu (x64 DLL) 가 4GB 아래에 오는 까닭 (이번은 x86 이미지 갈래만 — Q261 ④ 그대로).

## PE_emulator-AR 쪽 (결과서 "지금 우리 맞나·틀리나" 요약 — `../PE_emulator-AR` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| AR | 에뮬레이터가 지어야 할 것 (결과서) |
|---|---|
| AR02 | `_CheckX64Only` (`Process.cpp` · `AddressSpace.cpp` 여러 곳) 풀고 위 1 의 판정 흉내 → `Machine` · `'WowP'` 여부 · `OverrideAddressSpace`. `ST_EPROCESS` 에 `WoW64Process` 0x10 B `{Peb, NtdllType}` **새로**. `PspSystemDlls[1]` (`SystemDll.cpp`) 은 진짜 PE 바이트와 **같다 (맞음)**. `PsWowX86SharedInformation` 8칸을 표 B-0 이름으로 채움 — **x64 길도** 시작 블록 `+0x18` 에 이 16칸이 복사되므로 진짜와 같으려면 필요 |
| AR03 | `MiInitializeWowPeb` **새로** (27칸 · `Peb` 갱신 · PE32 load config 다시 읽기 · 힙 값 손질). 서브 영역 흉내 (2MB `'VadS'` · next-fit · 첫 힌트 랜덤 · 상한 `HighestUserAddress`) 를 PEB64 · PEB32 · TEB 가 함께 쓰면 진짜 꼴. 32비트 매개변수 블록 · 32비트 `LdrSystemDllInitBlock` (`Size 0x128` 검사) **새로**. PEB32 의 `ApiSetMap` · `SharedData` · `ExtendedFeatureDisableMask` · `ActiveProcessAffinityMask` |
| AR04 | `MmCreateTeb` WoW 갈래 (4 쪽, TEB32 = `+0x2000`, 19줄, TEB64 `+0x180c` · `+0x0`) + `SameTebFlags +0xfca` **새로**. 32비트 스택 · 64비트 스택 (`0x40000` / `0x8000` 고정) · CPU 영역 자르기 · `TlsSlots[1]` · x86 `CONTEXT` 초기값 **새로** |
| AR05 | `_InstallUserGdt` 는 칸 5 · 6 만 — **칸 4 (`0x20`) 새로** (진짜 바이트는 `[공백]`, VM 전까지 표준 32비트 DPL3 코드 `0x00cffb000000ffff` `[추측]`), **한계를 `0x57` 이상으로** (칸 10). 칸 10 한계 `0x3c00`, 베이스는 **스레드 바꿀 때마다 TEB32** (스케줄러에 한 줄) + `FS 0x53` + `FS_BASE ← UserFsBase` + 나가는 스레드 저장. STAR 순서 (`0x23` · `0x2b` · `0x33`) 는 지금 `Syscall.cpp` 의 `0x33` · `0x2b` 와 맞음 |
| AR07 이후 | `KiDispatchException` WoW64 갈래 (AC 삼킴 · `0x4000001f/1e` · 늘 64비트 처리기), `HighestUserAddress` 상한 · `MiSelectImageBase` WoW 겹침표 (`0x78000000`) |

## 디버거 메모 / 도구 메모

- 새 함정 없음. 참고 하나: `SwapContext` 의 FS · GDT 줄은 Ghidra 가 다른 함수 조각 (`FUN_1406b686f`) 으로 잡은 범위에 있다. `.asm` 의 `(= SwapContext +…)` 거리로 멈춤점을 걸 때는 `u nt!SwapContext+0x538 L1` 로 명령이 `MOV word ptr [RCX+0x52],AX` 인지 먼저 확인할 것 `[추측: 표시 이름]` → windbg-kd-runbook pitfalls 에 넣을지는 Q262 때 실제로 보고 정하면 됨.

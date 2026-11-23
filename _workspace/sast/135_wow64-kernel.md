# 135 wow64-kernel — 정적 분석 요청

- 날짜: 2026-11-23
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator-AR 요청. test.exe 는 x64 라 WoW64 갈래를 안 탐)
- 대기열: **Q253a** (PE_emulator-AR 요청 `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` 의 **C 절** — 원문을 먼저 읽을 것, 특히 "이미 아는 것" 의 run 근거 목록). 사용자 결정 2026-11-23 "다 끝나면 이것도 바로 이어서". 요청 차례 C → A → B 중 첫째. **정적만** (D = VM 은 Q253d, 미룸).
- 대상 빌드: Windows 11 24H2 26100.9457 x64 `ntoskrnl.exe` — **이미 analyze 됨**. 다시 analyze 하지 말고 `^이름$` export 만.
  - 있는 asm (읽기): `PspSelectMachineForProcess` · `PspWow64PickBestNtdll` · `PsWow64GetProcessNtdllType` · `PsWow64GetProcessMachine` · `PspWow64GetSharedInformation` · `MmCreateTeb` · `PspAllocateProcess` · `PspAllocateThread` · `MiSelectImageBase` · `KiInitializeBootStructures` · `KiDispatchException` · `KxContextToKframes` · `SwapContext`.
  - export 할 것 (`_index.tsv`): `MiInitializeWowPeb` `0x930784` (1213 B) · `PspWow64SetupUserProcessAddressSpace` `0x490f64` (86) · `PspWow64ThunkProcessParameters` `0x490fc4` (1905) · `PspWow64SetupUserStack` `0xa31390` (167) · `RtlpWow64CreateUserStack` `0xa31440` (696) · `PspWow64InitThread` `0x939140` (358) · `PspWow64InitThreadGuestx86` `0x9394ac` (285) · `PspWow64SetupCpuArea` `0x9390a4` (149). 그 밖은 필요하면 (`pdb_aliases.py --where ntoskrnl.exe` 로 이름 확인).
  - 구조체 칸 이름은 PDB 타입으로 (`_EWOW64PROCESS`·`_TEB32`·`_PEB32`·`_WOW64_CPURESERVED`·`_WOW64_CONTEXT` 류 — 있으면 그 이름·오프셋, 없으면 "PDB 에 없음").
- 주의: 같은 시간에 다른 decompiler 는 없음. flow-writer·explainer·committer 가 runs/·plan 을 쓸 수 있음 (`ms/` 안 건드림). 뒤이어 Q253b (wow64 DLL 넷) · Q253c (SysWOW64 ntdll) 가 같은 원문으로 올 것 — 이번엔 ntoskrnl 만.

## 왜 (요청한 쪽 말 요약)

- PE_emulator 갈래 AR: x86 exe·dll 을 **진짜 WoW64** 로 돌리려 함 (지금 엔진은 `emulator supports x64 only` 로 거절). 에뮬레이터가 커널 · 64비트 ntdll 초기화를 흉내 내고 `LdrpInitializeProcess +0x17fe` `g_LdrpWow64LdrpInitialize` 부터는 진짜 코드를 돌릴 계획 → 그 진짜 코드가 읽는 상태 (PEB32 · TEB32 · 32비트 스택 · CPU 영역 · 32비트 `LdrSystemDllInitBlock` · GDT 칸) 를 커널이 진짜 모양대로 지어 줘야 함.
- C 가 커널 짓기 바퀴 AR02 ~ AR05 를 연다 (원문 "답이 오면 쓰는 곳").

## 알고 싶은 것 (원문 C 1 ~ 10 — 번호 그대로)

1. `PspSelectMachineForProcess` — x86 (`0x14c`) exe 를 WoW64 로 정하는 조건과 출력 (`WoW64Process` · `Machine` · `Flags.Wow64VaSpace4Gb` · `OverrideAddressSpace` 등 어느 칸에 무엇을).
2. `PspAllocateProcess +0xfc4` `'WowP'` 0x10B 할당 칸 꼴 (`_EWOW64PROCESS` PDB 타입 이름·칸, 각 칸에 쓰는 줄·값).
3. `PspWow64PickBestNtdll` · `PsWow64GetProcessNtdllType` — x86 이면 칸 번호 1 인가 (예/아니오 + 줄).
4. **`MiInitializeWowPeb`** — PEB32 자리 (어떻게 고르나 — 4GB 아래 · 64비트 PEB 와의 거리), 쓰는 칸 · 값 (64비트 `MmCreatePeb` run10 꼴 표), `EPROCESS.WoW64Process` (`_EWOW64PROCESS.Peb`) 에 PEB32 주소를 적나.
5. `PspWow64SetupUserProcessAddressSpace` · `PspWow64ThunkProcessParameters` — 32비트 매개변수 블록을 만드나, 어디에 (PEB32 `ProcessParameters`), 칸 바꾸기 꼴 (64 → 32 문자열·포인터), 32비트 `LdrSystemDllInitBlock` (`PspSetupUserProcessAddressSpace +0x214`, `SharedNtdll32LdrSystemDllInitBlock = 7`) 과의 관계.
6. **`MmCreateTeb` x86 19줄 `+0x14a ~ +0x1db` 칸별** (`_TEB32` 칸 이름 · 값 출처) · `PspAllocateThread +0x79b ~ +0x7e3`.
7. **`PspWow64SetupUserStack` → `RtlpWow64CreateUserStack`** (32비트 스택 크기 · 자리 · TEB32 `StackBase/Limit`) · **`PspWow64InitThread` → `PspWow64InitThreadGuestx86` · `PspWow64SetupCpuArea`** (CPU 영역 자리 — TEB 어느 칸이 가리키나 · 첫 32비트 EIP · ESP · 세그먼트 · 64비트 쪽 첫 문맥 (`RtlUserThreadStart` 칸 `+0x71`) 과의 관계).
8. 주소 공간 상한 — `Wow64VaSpace4Gb` 를 읽는 곳 (VAD 할당 · `NtAllocateVirtualMemory` 가 4GB / 2GB 위를 막는 줄 — 이름·줄만), PEB · TEB · 스택 · 이미지가 4GB 아래로 오는 까닭 (`MiSelectImageBase` ESI 2·3 갈래 `+0x148 · +0x151`).
9. **GDT** — 칸 4 (`0x20`, 셀렉터 `0x23`) · 칸 10 (`0x50`, `0x53`) 을 부팅 때 쓰는 줄 (`KiInitializeBootStructures` 둘레) 과 바이트 (베이스·한계·형식), 칸 10 베이스를 스레드 바꿀 때 고치는 줄이 있나 (`SwapContext` 앞뒤 — `+0x545 ~ +0x55d` FS 베이스 ← `ETHREAD +0x678` 와의 관계).
10. (급하지 않음) `KiDispatchException +0x5d1 ~ +0x62a` — 32비트 코드에서 난 예외를 어디로 배달하나 (64비트 `KiUserExceptionDispatcher` 인지).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `PspSelectMachineForProcess` · `PspAllocateProcess` (`+0xa87`·`+0xfc4`·`+0xffb` 둘레만) · `PspWow64PickBestNtdll` · `PsWow64GetProcessNtdllType` | 0 (있음) | 1 ~ 3 |
| ntoskrnl | `MiInitializeWowPeb` · `PspWow64SetupUserProcessAddressSpace` · `PspWow64ThunkProcessParameters` | 1 | 4 · 5 |
| ntoskrnl | `MmCreateTeb` (있음) · `PspAllocateThread` (`+0x79b ~ +0x7e3` · `+0x9eb` · `+0xb7d`) · `PspWow64SetupUserStack` · `RtlpWow64CreateUserStack` · `PspWow64InitThread` · `PspWow64InitThreadGuestx86` · `PspWow64SetupCpuArea` | 1 | 6 · 7 |
| ntoskrnl | `Wow64VaSpace4Gb` 읽는 곳 (refs) · `MiSelectImageBase` (있음) | 0 | 8 |
| ntoskrnl | `KiInitializeBootStructures` (있음) 둘레 GDT 쓰기 · `SwapContext` (있음) | 0 | 9 |
| ntoskrnl | `KiDispatchException` (있음) | 0 | 10 |

- 범위: 칸·값·자리·검사 줄까지. 메모리 관리 본문 (VAD 넣기), 문맥 전환 본문, 32비트 쪽 코드 (wow64*.dll · SysWOW64 ntdll — Q253b·Q253c) 는 이름만.
- 분량이 크면 원문 차례대로 **1 ~ 3 → 4 · 5 → 6 · 7 → 9 → 8 → 10** 으로 결과서에 먼저 쓰고, 나머지는 "다음 요청으로" 라고 적어도 된다.

## 받고 싶은 것

- 결과서 `_workspace/sast/135_wow64-kernel_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/ntoskrnl/<함수>.asm`. 근거는 `[정적] ms/disassemble/ntoskrnl/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (C 1 ~ 10 마다 한 줄).
  3. 표: `구조체 · 칸 (오프셋 · PDB 이름) · 값 · 출처 · 쓰는 줄` — `_EWOW64PROCESS` · PEB32 · 32비트 매개변수 · TEB32 · 32비트 스택 · CPU 영역 · GDT 칸 4 · 10 각각. (짐 지도 `baggage.md` 꼴이면 좋음 — 공간: 유저 / 커널.)
  4. PDB GUID 확인 줄 (ntoskrnl — 기준 `C29EBFB06B78B3C020DCA66D99713F9E1` 과 같은지).
- **"지금 우리 (PE_emulator-AR) 맞나·틀리나"** 는 원문 "답이 오면 쓰는 곳" 의 AR02 ~ AR05 (`Process.cpp _CheckX64Only` · `WoW64Process` 칸 · `SystemDll.cpp PspSystemDlls[1]` · PEB32 · TEB32 · 스택 · CPU 영역 · GDT) 마다 "에뮬레이터가 지어야 할 것" 한 줄씩 (아직 코드가 없으면 "새로 지을 것" 으로).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (원문 D 와 합치게 — 멈춤점 `caller+0x…`) 과 **"다음 요청 후보"**.

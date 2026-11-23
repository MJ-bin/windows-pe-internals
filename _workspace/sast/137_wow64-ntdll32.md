# 137 wow64-ntdll32 — 정적 분석 요청

- 날짜: 2026-11-23
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator-AR 요청. test.exe 는 x64 라 WoW64 갈래를 안 탐)
- 대기열: **Q253c** (PE_emulator-AR 요청 `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` 의 **B 절** — 원문을 먼저 읽을 것). 사용자 결정 2026-11-23 "다 끝나면 이것도 바로 이어서". 요청 차례 C → A → B 의 셋째. **정적만.**
- **먼저 읽는 것**: `_workspace/sast/136_wow64-dlls_result.md` 전부 — 특히 "다음 요청 후보 1" (이번 요청의 ① ~ ④ 를 거기서 그대로 받음), A2 (전환 꼴 · `Wow64Transition` 을 `CpupResolveReverseImports +0xb2` 가 씀 · TEB32 `+0xc0` = `BTCpuGetBopCode` 결과), A3 (`Wow64SetupInitialCall` — 32비트 문맥 `Eip = LdrSystemDllInitBlock +0x18`, `[Esp+4]` 문맥 사본, `[Esp+8]` NtDll32Base), A4 (번호 비트 12~13 = 표, 비트 16 이상 = 터보 썽크 번호).
- 대상 빌드: Windows 11 24H2 26100.9457 **x86 (32비트)** `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/SysWOW64/ntdll.dll` — **아직 analyze 안 됨**.
  - **이름 겹침 주의**: 64비트 ntdll 과 파일·PDB 이름이 같다 (`ntdll.dll` / `wntdll.pdb` 류일 수 있음 — PDB 이름은 디버그 디렉터리로 확인). **64비트 쪽 (`ms/sym/ntdll.dll.pdb`, `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`, `ms/ghidra/ntdll.*`) 을 덮어쓰지 말 것.** 모듈 이름을 `ntdll32` 로 써서 `ms/decomp/ntdll32/` · `ms/disassemble/ntdll32/` · `ms/ghidra/ntdll32.*` 에 두고, PDB 는 겹치지 않는 자리 (예 `ms/sym/ntdll32/` 또는 `ms/sym/ntdll32.dll.pdb`) 에 받는다. 스크립트가 이름을 파일 이름에서 정하면 그 점을 결과서 "스크립트 문제" 에 적는다 (고치지는 말 것 — 하네스는 오케스트레이터 몫). Ghidra 언어는 x86 32비트 (`x86:LE:32:default`).
  - 근거 표기: `[정적] ms/disassemble/ntdll32/<함수>.asm +오프셋`. 64비트 쪽과 헷갈리지 않게 결과서 안에서 "ntdll32" / "ntdll (64)" 로 부른다.
- 주의: 같은 시간에 다른 decompiler 가 **ntoskrnl** (sast/135) 을 돈다 — 그쪽은 건드리지 않는다. 새 analyze 전에 `free -g`. scratchpad 임시 파일은 `s137/` 하위 폴더에.

## 왜 (요청한 쪽 말 요약)

- PE_emulator 갈래 AR: x86 exe 를 진짜 WoW64 로. 길 = 진짜 `SysWOW64\ntdll.dll` → `Wow64Transition` → `wow64cpu.dll` → `wow64.dll` → 64비트 ntdll `syscall`. 엔진은 `LdrpInitializeProcess +0x17fe` 부터 진짜 코드에 넘김 (sast/136: `Wow64LdrpInitialize` 는 안 돌아옴).
- 그 뒤 처음 도는 32비트 코드가 이 ntdll32 이다 → 엔진·커널이 지어 줘야 하는 PEB32 · TEB32 · 32비트 `LdrSystemDllInitBlock` 칸 목록이 필요 (원문 "답이 오면 쓰는 곳": B 4 → AR03, B → AR07 이후).

## 알고 싶은 것 (원문 B 1 ~ 4 + sast/136 넘김 ① ~ ④)

1. **32비트 `LdrInitializeThunk` 입구** — 받는 것 (스택 `[Esp+4]` 문맥 · `[Esp+8]` NtDll32Base — sast/136 짐작이 맞나), 첫 몇 함수 (64비트판 run15 ~ run17 꼴: `LdrpInitialize` → `_LdrpInitialize` → `LdrpInitializeProcess` 까지 이름과 부르는 줄). `LdrSystemDllInitBlock +0x18` (`Ntdll32LoaderInitRoutine`) 의 값이 정말 32비트 `LdrInitializeThunk` 인가 (export 주소 · 커널 쪽 이름 표 근거).
2. **32비트 `LdrpInitializeProcess` 가 처음 읽는 칸** — PEB32 · TEB32 · 32비트 `LdrSystemDllInitBlock` (자리: 32비트 ntdll 안 전역인지) 칸 목록 (`구조체 · 오프셋 · 읽는 줄 · 0 이면 어떻게 되나`). 짐 지도 수준까지는 필요 없음 — "읽는 칸 목록" 이면 됨. 64비트판 (run18 · run32 · run114) 과 다른 점만 따로.
3. **`Nt*` 스텁 꼴** — `EAX` 에 싣는 값 (번호 · 비트 16 이상 터보 번호 — sast/136 A4 와 맞나), 부르는 곳 (`Wow64Transition` 칸 `CALL [mem]` 인지 `fs:[0xc0]` 인지 — 둘 다면 어느 스텁이 어느 쪽), 인자 위치 (`EDX` = 스택 포인터?), 돌아온 뒤 (`RET n`). 대표 스텁 셋 (`NtContinue` · `NtAllocateVirtualMemory` · `NtTerminateProcess`) 바이트 그대로. `Wow64Transition` export 의 처음 값 (파일 안 값 — 0 인가).
4. **`KiUserExceptionDispatcher` · `KiUserApcDispatcher` · `KiUserCallbackDispatcher` (32비트) 입구** — 스택에서 무엇을 받나 (이름·첫 줄들). 그리고 `NtContinue` (32비트) → `whNtContinue` → 64비트 `NtContinueEx` 로 가는 길 (32비트 쪽은 스텁까지만).
5. **`NtdllExportInformation[1]` WOW 8칸 이름 8개** = `LdrSystemDllInitBlock +0x18 ~ +0x50` (sast/136 넘김 ④ 의 표): 커널 쪽 이름 표 (ntoskrnl rva `0xc7ff90` `[1]` — **ntoskrnl 프로젝트는 지금 sast/135 가 쓰므로 Ghidra 로 열지 말고 `pefile` 로 PE 데이터만 읽을 것**) 의 글 8개와, 그 이름들이 ntdll32 export 에 있는지 (rva). `[4]` 가 32비트 `RtlUserThreadStart` 인가 (`[추측]` 확인).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntdll32 | `LdrInitializeThunk` → `LdrpInitialize` → `_LdrpInitialize` → `LdrpInitializeProcess` (앞부분 — 칸 읽기까지) | 1 | 1 · 2 |
| ntdll32 | `Nt*` 스텁 셋 · `Wow64Transition` (export 데이터) | 0 | 3 |
| ntdll32 | `KiUserExceptionDispatcher` · `KiUserApcDispatcher` · `KiUserCallbackDispatcher` · `RtlUserThreadStart` | 0 | 4 · 5 |
| ntoskrnl (PE 데이터만) | 이름 표 rva `0xc7ff90` `[1]` | — | 5 |

- 범위: 입구 · 읽는 칸 · 스텁 꼴까지. 32비트 로더 본문 (DLL 올리기 · import 연결 — 64비트판과 같은 꼴이면 "같음" 한 줄), wow64*.dll 쪽 (sast/136 에 있음), 커널 쪽 (sast/135) 은 이름만.
- 분량이 크면 **5 → 1 → 3 → 2 → 4** 로 결과서에 먼저 쓰고 나머지는 "다음 요청으로" 라고 적어도 된다 (5 가 AR03 을 막음).

## 받고 싶은 것

- 결과서 `_workspace/sast/137_wow64-ntdll32_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/ntdll32/<함수>.asm`. 근거는 `[정적] ms/disassemble/ntdll32/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1) 입구 사슬 표 · (2) `구조체 · 오프셋 · PDB 이름 · 읽는 줄 · 0 이면` · (3) 스텁 바이트 표 · (4) 디스패처 입구 표 · (5) `칸 · 오프셋 · 커널 이름 표 글 · ntdll32 export rva`.
  4. PDB GUID 확인 줄 (ntdll32 — PDB 파일 이름 · GUID · 둔 자리).
- **"지금 우리 (PE_emulator-AR) 맞나·틀리나"** 는 원문 "답이 오면 쓰는 곳" 의 AR03 (PEB32 · 32비트 블록) · AR07 마다 "커널/엔진이 지어 둘 것" 한 줄씩.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (원문 D 와 합치게 — 32비트 `LdrInitializeThunk` 첫 명령의 `esp` 값들, 멈춤점은 32비트 모듈이라 `ntdll32` 로드 주소 + rva 꼴) 과 **"다음 요청 후보"**.

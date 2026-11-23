# 136 wow64-dlls — 정적 분석 요청

- 날짜: 2026-11-23
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator-AR 요청. test.exe 는 x64 라 WoW64 갈래를 안 탐)
- 대기열: **Q253b** (PE_emulator-AR 요청 `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` 의 **A 절** — 원문을 먼저 읽을 것). 사용자 결정 2026-11-23 "다 끝나면 이것도 바로 이어서". 요청 차례 C → A → B 의 둘째 (C = sast/135 가 같은 시간에 ntoskrnl 을 돈다). **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `ntdll.dll` (64비트) — **이미 analyze 됨**. 있음: `LdrpLoadWow64.asm` · `RtlpWow64ThunkEnvironmentTo64.asm`. 그 밖은 `^이름$` export 만 (색인에 `RtlWow64GetCpuAreaInfo` · `RtlWow64GetCurrentCpuArea` · `RtlpWow64CtxFromAmd64` 등 있음).
  - `wow64.dll` (356,936 B) · `wow64cpu.dll` (51,128 B) · `wow64win.dll` (575,888 B) · `wow64base.dll` (59,392 B) · `wow64con.dll` (113,120 B) — **아직 analyze 안 됨**. PDB: `wow64cpu.dll.pdb` 만 `ms/sym/` 에 있음 (GUID 확인), 나머지는 `fetch_pdb.py`.
  - **analyze 차례**: wow64cpu (작음) → wow64 → (질문 4 의 win32k 한 줄에 필요하면) wow64win. wow64base·wow64con 은 질문 5 가 import·문자열로 닫히면 analyze 하지 않는다. 한 번에 하나, 매번 `free -g`.
  - 32비트 쪽 (`SysWOW64\ntdll.dll`) 은 **이번 범위 아님** (Q253c 다음 요청). 32비트 코드로 들어가는 자리까지만.
- 주의: 같은 시간에 다른 decompiler 가 **ntoskrnl** (sast/135) 을 돈다 — 그쪽은 건드리지 않는다. flow-writer·explainer·committer 가 runs/·plan 을 쓸 수 있음.

## 왜 (요청한 쪽 말 요약)

- PE_emulator 갈래 AR: x86 exe 를 진짜 WoW64 로. 길 = 진짜 `SysWOW64\ntdll.dll` → `Wow64Transition` → `wow64cpu.dll` → `wow64.dll` (· `wow64win.dll`) → 64비트 ntdll `syscall` → 에뮬레이터 커널.
- 에뮬레이터는 64비트 ntdll 초기화를 흉내 내다가 **`LdrpInitializeProcess +0x17fe` `CALL g_LdrpWow64LdrpInitialize(CONTEXT)` 부터 진짜 코드를 돌릴** 계획 (`[정적] run17 · run18 · run19 · run106` — `UseWOW64 = TEB.WowTebOffset > 0`, `+0x17c6 LdrpLoadWow64`, `+0x1801 JMP +0x1d1b`). A 1 · 3 → AR06 (은퇴 자리), A 2 → AR04 · AR05 (CPU 영역 · 모드 바꾸기).
- 이미 아는 것: `[정적] run82` — `KiUserExceptionDispatcher +0x1 Wow64PrepareForException` (rva `0x1e70e0`), WoW64 초기화용 포인터 표 `0x173848`. `[정적] run121` — wow64.dll 이 TEB32 `+0xc0 · +0xc4` 에 씀.

## 알고 싶은 것 (원문 A 1 ~ 5 — 번호 그대로)

1. **`LdrpLoadWow64`** (64비트 ntdll, 있는 asm) — 만드는 DLL 이름 글 (`+0x6e` 의 `0x18017bf78` 글 그대로), 올리는 DLL (`LdrLoadDll` 류 인자), `LdrGetProcedureAddress` 로 찾는 함수 이름 표 (`+0xcd` 둘레 — 몇 칸 · 각 칸 이름 글 · 각 칸을 어느 ntdll 전역에 쓰나: `g_LdrpWow64LdrpInitialize` · `Wow64PrepareForException` 표 `0x173848` 외). 실패 때 status 와 부른 쪽 (`+0x17c6` 뒤) 이 하는 일.
2. **wow64cpu.dll** — 64 → 32 · 32 → 64 전환 명령 꼴 (`jmp far m16:32` · `retf` · `iretq` 중 무엇, 셀렉터 값 `0x23`/`0x33`), CPU 영역 (32비트 문맥) 의 자리와 꼴 (TEB 어느 칸이 가리키나 — `TlsSlots[1]` 류 · `WOW64_CPURESERVED` / `WOW64_CONTEXT` 칸, 64비트 ntdll `RtlWow64GetCurrentCpuArea` 와 대조), 32비트 코드로 처음 들어가는 함수 (`BTCpuSimulate` `[추측]` — PDB 이름으로 확인) 와 그 앞 준비, `Wow64Transition` 이 가리키는 곳 (누가 그 칸에 값을 쓰나 — 32비트 ntdll 의 칸에 64비트 쪽이 쓰는 줄).
3. **wow64.dll** — `g_LdrpWow64LdrpInitialize` 가 가리키는 함수 (`Wow64LdrpInitialize` `[추측]`) 의 차례: 프로세스 초기화 (`ProcessInit` 류) · 스레드 초기화 · CPU dll 올리기 (이름 글 · 레지스트리로 고르나) · 32비트 ntdll `LdrInitializeThunk` 를 부르는 자리 (어떻게 주소를 얻나 — `PsWowX86SharedInformation` / 32비트 ntdll export). **이 함수가 돌아오나** (`LdrpInitializeProcess +0x1801` 이 실제로 타나) — 예/아니오 + 줄. 그 동안 읽는 PEB32 · TEB32 · 64비트 PEB/TEB 칸, 부르는 syscall (`Nt*` 이름), 읽는 레지스트리 키 · 값.
4. **wow64.dll syscall 다리** — 32비트 `Nt*` 가 들어오는 입구 함수 (`Wow64SystemServiceEx` `[추측]`) 와 인자 변환 표 꼴 (서비스 표 구조 · `whNt*` 이름 꼴 `[추측]` · 번호 → 표 칸 고르기 — 번호의 윗 비트로 표 (nt / win32k / console) 고르는지), 변환 실패 때 돌려주는 상태. win32k 쪽은 wow64win.dll 이 같은 꼴인지 한 줄.
5. **wow64base.dll · wow64con.dll** — 초기화에서 올라오나, 누가 올리나 (이름만 — import·`LoadLibrary` 글·레지스트리 근거).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntdll | `LdrpLoadWow64` (있음) + 이름 글·표 (PE 데이터) | 0 | 1 |
| wow64cpu | 전환 함수 · `BTCpuSimulate` 류 · CPU 영역 초기화 (`BTCpuProcessInit`·`BTCpuThreadInit` 류) | 1 | 2 |
| wow64 | `Wow64LdrpInitialize` 류 · 프로세스/스레드 초기화 · CPU dll 올리기 · 32비트 `LdrInitializeThunk` 부르는 줄 | 1 | 3 |
| wow64 | syscall 입구 · 서비스 표 (PE 데이터) | 0~1 | 4 |
| wow64win · wow64base · wow64con | 이름·import 만 (필요하면 입구 하나) | 0 | 4 · 5 |

- 범위: 초기화 차례 · 전환 꼴 · 읽는 칸 · 다리 꼴까지. 개별 `whNt*` 변환 본문, 32비트 ntdll 본문 (Q253c), 커널 쪽 (sast/135) 은 이름만.
- 분량이 크면 원문 차례 **1 → 3 → 2 → 4 → 5** 로 결과서에 먼저 쓰고 (원문: "A 1 · 3 → AR06 은퇴 자리" 가 먼저), 나머지는 "다음 요청으로" 라고 적어도 된다.

## 받고 싶은 것

- 결과서 `_workspace/sast/136_wow64-dlls_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (A 1 ~ 5 마다 한 줄).
  3. 표: (1) `칸 · 이름 글 · 쓰는 전역 · 줄` · (2) 전환 꼴 표 + CPU 영역 칸 표 (`오프셋 · 이름 · 크기 · 누가 쓰나`) · (3) `차례 · 함수 · 하는 일 · 읽는 칸 / syscall / 레지스트리 · 실패 때 · 줄` · (4) 다리 꼴 표.
  4. PDB GUID 확인 줄 (새로 analyze 한 모듈마다).
- **"지금 우리 (PE_emulator-AR) 맞나·틀리나"** 는 원문 "답이 오면 쓰는 곳" 의 AR04 · AR05 · AR06 · AR07 마다 "에뮬레이터가 해야 할 것 / 진짜 코드가 읽으니 지어 둘 것" 한 줄씩.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (원문 D 와 합치게 — `Wow64LdrpInitialize` 가 돌아오나 등, 멈춤점 `caller+0x…`) 과 **"다음 요청 후보"** (Q253c SysWOW64 ntdll 에 넘길 것 포함).

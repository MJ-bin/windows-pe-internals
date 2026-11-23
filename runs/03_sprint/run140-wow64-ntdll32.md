# 2026-11-23 (작성일, 정적 분석 2026-11-23) run140 — WoW64 (x86) 초기화의 **32비트 ntdll** (Q253c, 정적만): `SysWOW64\ntdll.dll` (이 문서에서 **ntdll32**, PDB `wntdll.pdb`) 의 `LdrInitializeThunk` 는 스택으로 **`[Esp+4]` = 문맥 사본 · `[Esp+8]` = ntdll32 기준 주소** 를 받고, 64비트판과 같은 사슬 (`LdrpInitialize` ⇒ `LdrpInitializeInternal` → `_LdrpInitialize` → `LdrpInitializeProcess`) 을 타는데 **`UseWOW64` 갈래가 없어 `LdrpInitializeProcess` 가 끝까지 돌고 돌아온다** → `+0x24 NtContinue(문맥, 1)`. `Nt*` 스텁 509개는 `mov eax,N · mov edx,&_Wow64SystemServiceCall · call edx · ret n` 이고 `_Wow64SystemServiceCall` = **`JMP [Wow64Transition]`** (파일 속 값 0). `fs:[0xc0]` 를 직접 부르는 스텁은 `NtQueryInformationProcess` 하나뿐이다

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator-AR 요청** (갈래 AR "x86 exe 를 진짜 WoW64 로", 원 요청 `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` 의 **B 절**) 이다. test.exe 는 x64 라 이 갈래를 안 탄다 — 64비트 ntdll `UseWOW64` 0 (flow.md 2.4 run44 "갈림 값" `[관찰]`), EPROCESS `WoW64Process : (null)` (`ms/logs/04_baggage-read_P1a.txt:160` · `_P2.txt:160`, run106 이 맞춘 줄 `[관찰]`). 그래서 32비트 ntdll 이 도는 순간이 없다. 그림 1 · 2 에 WoW64 노드가 없어 **그림은 안 바꿨다.** flow.md 는 기준 (PDB 줄 · 근거 줄 · 정적 근거 메모) · 2.1 `PspPrepareSystemDllInitBlock` 표 B-0 줄 아래 한 줄 · 3장 (run138 절 몇 줄, run139 절 뒤 새 절) · 4장 · 5장만 고쳤다. 짐 지도 (`baggage.md`) 는 안 건드림.

> **대기열**: **Q253c** (AR01 B 절. 사용자 결정 2026-11-23 "다 끝나면 이것도 바로 이어서" — 요청 차례 C → A → B 의 셋째). 결과서 판정 **분석성공** (B 1 ~ 3 · B 4 디스패처 · B 5 rva 대조, 못 닫은 몇 줄은 `[추측]` · `[공백]`).
> - 원문 **B 4 (이름 8개)** 는 run139 표 B-0 이 이미 닫았다. 이번엔 그 8개가 ntdll32 export 에 있는지 (rva) 만 대조했다 — 아래 5.
> - 이어 쓰는 앞 run: **run138** (wow64 DLL — `Wow64SetupInitialCall` 이 `[Esp+4]` · `[Esp+8]` 을 놓고 `Eip` ← 블록 `+0x18`, `CpupResolveReverseImports +0xb2` 가 `Wow64Transition` 을 씀, 서비스 표 · 번호 비트), **run139** (커널 — TEB32 19줄 · PEB32 27칸 · 표 B-0 · x86 첫 문맥), **run15 ~ run19 · run32 · run114** (64비트 `LdrInitializeThunk` ~ `LdrpInitializeProcess` — 비교용). 모두 이번 줄과 **맞는다** (아래 "앞 run 과 이어 읽기").
> - 남은 동적 → **Q265 (동적: 결과서 동적 9개 — ntdll32 로드 주소 0x4b280000·fs:[0xc0] 갈림·32비트 LdrInitializeThunk 첫 esp·fs:[0xfdc]·첫 NtContinue 의 TEB64 +0x14b8 목록·NtContinueEx/NtSetInformationThread(0x1d) — Q253d·Q260·Q262 VM 과 합침)**, 남은 정적 → **Q266 (정적: 결과서 다음 요청 후보 — NtSetInformationThread 0x1d 커널 쪽·TEB64 +0x14b8 틀 넣는 곳·ntdll32 초기화가 부르는 whNt* 본문·32비트 RtlUserThreadStart→BaseThreadInitThunk)**.

## 요약

- **B1 입구 사슬** `[정적]` — ntdll32 `LdrInitializeThunk` (rva `0x34230`, export #151) 는 stdcall 두 인자를 스택에서 읽는다: `+0x8 ECX = [EBP+8]` (= 들어올 때 `[Esp+4]`, **문맥**), `+0x5 EDX = [EBP+0xc]` (= `[Esp+8]`, **ntdll32 기준 주소**). `[Esp]` 반환 칸은 안 쓴다. 사슬: `+0xc → +0x11 LdrpInitialize` (PDB `_LdrpInitialize@8`) → `+0x30 JMP LdrpInitializeInternal` → `+0xaa → +0xaf _LdrpInitialize` (PDB `__LdrpInitialize@8`) → `+0xcf → +0xd4 LdrpInitializeProcess`. 돌아오면 **`+0x24 → +0x29 NtContinue(문맥, 1)`**. 64비트판 (`+0xe` · `+0x3c JMP` · `+0x5a` · `+0x16c`, NtContinue `+0x18`) 과 같은 꼴, 오프셋만 다르다.
- **블록 `+0x18` 값 = 32비트 `LdrInitializeThunk`** `[정적]` — 커널 이름 표 0번 글 `LdrInitializeThunk` (run139 표 B-0) 이 ntdll32 export rva `0x34230` 에 있다. run138 의 "`Eip` ← 블록 `+0x18`" · "`[Esp+4]` 사본 · `[Esp+8]` 기준" 이 **받는 쪽 코드로도 맞다.**
- **B2 `LdrpInitializeProcess` 가 읽는 칸** `[정적]` — TEB32 `+0x0` · `+0x18` · `+0x30` · `+0xfca` · `+0xfdc`, **TEB32 `+0xfdc` 가 음수면 TEB64 (= TEB32 − 0x2000) `+0x14d0 TlsSlots[10]` → `Wow64Info[0]`** (= 쪽 크기 `0x1000`), PEB32 18곳 (결과서), 32비트 매개변수 칸들, **ntdll32 자기 안** `LdrSystemDllInitBlock` (rva `0x13d2a0`, 파일에 `Size = 0x128` 이 이미 있음), KUSD 몇 칸. `UseWOW64` · `LdrpLoadWow64` · `g_LdrpWow64*` 줄이 **없다** → 끝까지 돈다 (kernel32 · import · TLS 콜백까지).
- **B3 `Nt*` 스텁** `[정적]` — 509개 = nt 표 칸 0 ~ 508 하나씩. `EAX` 비트 0 ~ 11 = 칸, 12 ~ 13 = 표 (모두 0 = nt), 16 이상 = wow64cpu 터보 번호 (sast/136 A4 와 같음). `EDX` 는 인자 포인터가 **아니라** `_Wow64SystemServiceCall` 주소. 인자는 32비트 스택에 있고 wow64cpu 가 `R11 = 원래 Esp + 8` 로 잡는다. `fs:[0xc0]` 직접 호출은 **`NtQueryInformationProcess` 한 스텁** (ntdll32 가 윗바이트 `0x4b` 주소에 올라왔을 때만).
- **B4 디스패처 · `NtContinue` 길** `[정적]` — 32비트 `KiUserExceptionDispatcher` `[Esp]` 기록 · `[Esp+4]` 문맥, `KiUserApcDispatcher` `[Esp]` 루틴 · `[Esp+4 ~ +0xc]` 인자 · `[Esp+0x10]&1` · 문맥 `Esp+0x14`, `KiUserCallbackDispatcher` `[Esp+4]` 번호 → PEB32 `+0x2c` 표. 32비트 `NtContinue` → wow64 `whNtContinue` → `Wow64NtContinue` → `CpuSetContext` → wow64cpu `BTCpuSetContext` (보통 64비트 **`NtSetInformationThread(−2, 0x1d, 문맥, 0x2cc)`**) · `NtTestAlert`. 64비트 **`NtContinueEx` 는 TEB64 `+0x14b8` 목록에 맞는 틀이 있을 때만**.
- **B5 이름 8개 rva** `[정적: PE]` — 8개 모두 ntdll32 export 에 있다. `[4]` = 32비트 `RtlUserThreadStart` (`0x7c000`) 확정. sast/136 "넘김 ④" 칸 번호는 바로잡는다: **블록 `+0x48` = 이름 없는 ntdll32 기준 주소, `+0x50` = `LdrSystemDllInitBlock`, `+0x58` = `RtlpFreezeTimeBias`** (run139 표 B-0 과 같음).

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔여덟째 바퀴. plan **Q253c**. **짐 지도 바퀴 아님.**
- 정적: **ntdll32** (`SysWOW64\ntdll.dll`, 1,822,400 B, 머신 `0x14c`, ImageBase `0x4b280000`, SizeOfImage `0x1bf000`) pdb **`wntdll.pdb` GUID `7C1F7D48576AAE47EB48E43D5FB752AD1`** (`ms/sym/ntdll32.dll.pdb`, flow-writer 가 디버그 디렉터리를 `pefile` 로 다시 읽어 같음). **처음 analyze** (약 75 초, `SetPdb:` 줄 있음, 함수 4687). 64비트 ntdll (`ntdll.pdb`) 과 PDB 이름도 다르다.
  - 이 문서의 `ntdll32!이름` 은 우리 Ghidra 모듈 이름이다. WinDbg 가 이 모듈을 어떤 이름으로 보일지는 안 봄 `[공백]` (Q265).
  - 주소 `0x4b2b4230` 꼴은 파일 ImageBase 기준 (Ghidra). 실제로 올라오는 주소는 Q265.
- 버전 대조 (결과서 "버전 확인" — 모두 일치): ntdll32 블록 첫 dword `0x128` ↔ sast/135 `PspPrepareSystemDllInitBlock` `Size == 0x128` 검사 (run139), `NtdllWowX86Exports` 8 이름 (run139 표 B-0), ntdll32 `_TEB` `+0xfdc` · `+0xfca` · `+0xc0` · `+0xc4` ↔ run139 TEB32 표 · run138, ntdll32 `_PEB` sizeof `0x490` ↔ 64비트 PDB `_PEB32` (run138), 스텁 칸 · `ret n` ↔ wow64 `sdwhnt32` 인자 바이트 표 (run138).
- flow-writer 가 다시 본 것 (`.asm` · `_refs` · PE, 결과서 줄과 맞춤): ntdll32 `LdrInitializeThunk +0x0 ~ +0x2f` 전부 · 떨어진 범위 `+0x85a48 ~ +0x85a5d`, `_LdrpInitialize@8 +0x0 ~ +0x30` 전부, `_LdrpInitializeInternal@8 +0xb · +0x1f · +0x4d · +0x52 · +0x76 · +0x8e · +0x9f · +0xaa`, `__LdrpInitialize@8 +0x7 · +0x12 · +0x2f · +0x38 · +0x45 · +0x60 · +0x65 · +0x6d · +0x81 · +0x86 · +0xcf · +0x196 · +0x1ad · +0x1c8 · +0x215 · +0x217 · +0x238 · +0x277 · +0x27f · +0x288 · +0x29a · +0x2a6 · +0x2ae`, `_LdrpInitializeProcess@8 +0xf · +0x15 · +0x2f · +0x39 · +0x5e · +0x97 · +0xbd · +0xc9 · +0x128 · +0x13f · +0x177 · +0x180 · +0x22c · +0x233 · +0x34c · +0x3fc ~ +0x448` (전부) `· +0x4a0 · +0x4e6 · +0x4f0 · +0x56a · +0x57b · +0x58c · +0x58f · +0x7bd · +0x7c9 · +0x831 · +0x8d2 · +0xa1d · +0xa22 · +0xa3e · +0xa5d · +0xaa6 · +0xb03 · +0xb6f · +0xcb2 · +0xe34 · +0xfe4 · +0x10c6 · +0x13dc · +0x14d6 · +0x169f · +0x1712 · +0x172f ~ +0x174c · +0x17d8 ~ +0x1804 · +0x1857 · +0x1913 · +0x1981 · +0x198d · +0x1993 · +0x19c7`, `_LdrInitializeMrdata@0 +0x22`, `_InitSecurityCookie@0 +0x20`, `_LdrInitializePerThreadRng@4 +0x11 · +0x1c`, `_LdrpInitializeNtdllDataTableEntry@16 +0x92 ~ +0x9b`, `_RtlpInitEnvironmentBlock@0` call 줄 전부, `_RtlpWow64ThunkEnvironmentTo32@0 +0x26 · +0x35 · +0x52`, `_RtlpWow64GetNativeSystemInformation@16 +0x0 ~ +0x7`, 스텁 `NtContinue` · `NtContinueEx` · `NtAllocateVirtualMemory` · `NtTerminateProcess` · `NtCallbackReturn` · `NtTestAlert` · `NtWow64GetNativeSystemInformation` · `_Wow64SystemServiceCall@0` · `KiFastSystemCall` · `KiIntSystemCall` · `RtlUserThreadStart` 전부, `KiUserExceptionDispatcher` · `KiUserApcDispatcher` · `KiUserCallbackDispatcher` 전부, `_refs/Wow64Transition.tsv` (3줄) · `_refs/LdrSystemDllInitBlock.tsv` 머리. wow64 `whNtContinue` 전부 · `Wow64NtContinue +0x3f · +0x7b ~ +0xb9 · +0xca · +0x1bf · +0x1d8 · +0x1e6 · +0x205 · +0x20a · +0x216` · `CpuSetContext +0x2d` · `whNtContinueEx +0x13 · +0x4e · +0x59`, wow64cpu `BTCpuSetContext +0x47 ~ +0x76 · +0xa0`. 64비트 ntdll `LdrInitializeThunk` 전부 · `LdrpInitializeProcess +0x1b45 · +0x1b59 · +0x1bb6` (비교). PE (`pefile`): ntdll32 export 8 이름 · `Wow64Transition` · `LdrSystemDllInitBlock` rva, 그 두 칸의 파일 값, `Nt*`/`Zw*` 515 주소의 스텁 바이트 (509 = 508 + `NtQueryInformationProcess`), 디버그 디렉터리. 어긋난 곳은 아래 "flow-writer 대조".

## 목적

- PE_emulator 갈래 AR: x86 exe 를 **진짜** WoW64 코드로 돌린다. 엔진은 64비트 ntdll 초기화를 흉내 내다 `LdrpInitializeProcess +0x17fe` 부터 진짜 wow64 코드에 넘긴다 (run138). 그 뒤 **처음 도는 32비트 코드가 ntdll32** 이므로, 그 코드가 읽는 PEB32 · TEB32 · 32비트 블록 칸과 syscall 꼴이 필요하다. 요청서 `_workspace/sast/137_wow64-ntdll32.md` (원문 B 1 ~ 3 + sast/136 넘김 ① ~ ④):
  1. 32비트 `LdrInitializeThunk` 입구가 받는 것 · 첫 함수 사슬 · 블록 `+0x18` 이 정말 그 함수인가. 2. `LdrpInitializeProcess` 가 처음 읽는 칸 (64비트판과 다른 점). 3. `Nt*` 스텁 꼴 · `Wow64Transition` 대 `fs:[0xc0]` · 인자 · `Wow64Transition` 파일 값. 4. 디스패처 셋 입구 · `NtContinue` (32비트) 가 가는 길. 5. 이름 8개의 ntdll32 export rva · `[4]` 가 `RtlUserThreadStart` 인가.
- 쓰는 곳 (원문): AR03 (PEB32 · 32비트 블록), AR06 (은퇴 자리), AR07 이후 (syscall 표면).

## 방법

- 정적 sast/137 (결과서 `_workspace/sast/137_wow64-ntdll32_result.md`, **분석성공**):
  - ntdll32 **처음 analyze** — `ms/bin/ntdll32.dll` (SysWOW64 `ntdll.dll` 의 하드링크) 로 `fetch_pdb.py` → `run_ghidra.sh analyze`. 64비트 ntdll 쪽 (`ms/sym/ntdll.dll.pdb` · `ms/decomp/ntdll/` · `ms/disassemble/ntdll/` · `ms/ghidra/ntdll.*`) 은 안 덮음 (결과서가 수정 시각 확인).
  - `.asm` 은 depth 0: 앞 사슬 5 · 스텁 · 디스패처 15 · 앞쪽 부품 11 (결과서 "만든 파일"), `_refs/{Wow64Transition,LdrSystemDllInitBlock}.tsv`, `_calls.tsv` (268줄).
  - 구조체 칸 이름: ntdll32 PDB (`pdb_struct.py ntdll32.dll _PEB _TEB _RTL_USER_PROCESS_PARAMETERS`).
  - wow64 (sast/136 이 analyze 끝냄) 에서 `NtContinue` 길 `.asm` 7개를 더 뽑음 (`whNtContinue` · `whNtContinueEx` · `whNtTerminateProcess` · `whNtTestAlert` · `Wow64NtContinue` · `Wow64NtTerminateProcess` · `CpuSetContext`).
  - ntoskrnl 은 Ghidra 로 안 열고 (sast/135 가 쓰는 중이었음) `pefile` 로 `NtdllExportInformation` (rva `0xc7ff90`) 만 다시 읽음 → run139 표 B-0 과 같은 8줄.
  - 보조 (결과서 쪽 일회용, 레포에 안 둠): ntdll32 `.text` 전체 objdump (`fs:0xc0` 찾기 · 스텁 509개 풀기), wow64 `.text` 에서 `0x14b8` 찾기.
- **x86 PDB 이름은 `@N` 장식이 붙는다** (`_LdrpInitialize@8` = 64비트 `LdrpInitialize`, `__LdrpInitialize@8` = `_LdrpInitialize`, `_LdrpInitializeProcess@8` …). 이 문서는 **64비트와 같은 이름 (장식 뗀 것)** 으로 쓰고 처음 한 번 장식 이름을 밝힌다. `.asm` 파일 이름은 장식 그대로다.

## 결과

비유 (run138 · run139 와 같은 그림):

- **WoW64 = 큰 건물 (64비트) 안 옛날식 사무실 (32비트).** run139 = 건물주 (커널) 가 깔아 둔 것, run138 = 통역사 (wow64.dll) 와 문 (wow64cpu.dll). 이번엔 **사무실 안에서 처음 일하는 직원 = 32비트 ntdll** 이다.
- 직원은 문으로 들어오면서 쪽지 두 장 (`[Esp+4]` 문맥 사본, `[Esp+8]` 자기 사무실 주소) 을 받는다. 그리고 64비트 쪽 직원과 **같은 순서로** 사무실을 꾸미는데, 64비트 쪽처럼 "통역사를 불러 넘기기" 단계가 없어서 **끝까지 다 꾸미고 돌아온다** `[추측: 비유]`.
- 바깥에 부탁할 일 (`Nt*`) 이 있으면 모든 직원이 **같은 문고리 하나** (`Wow64Transition` 칸) 를 잡아당긴다. 문고리에 뭐가 걸릴지는 통역사가 나중에 채워 넣는다 (파일 속엔 0) `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/ntdll32/_calls.tsv` · `.asm`, wow64 는 `ms/disassemble/wow64/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll32!LdrInitializeThunk` (rva `0x34230`) | `+0xc` | `+0x11` | `LdrpInitialize` (PDB `_LdrpInitialize@8`) — `ECX` = 문맥 `[EBP+8]`, `EDX` = 기준 `[EBP+0xc]` | [정적] |
| 〃 | `+0x24` | `+0x29` | **`NtContinue(문맥, 1)`** (`+0x1f PUSH 1` · `+0x21 PUSH [EBP+8]`; 같은 주소 이름 2개 `_NtContinue@8` · `_ZwContinue@8`) — `+0x11 _LdrDelegatedLdrInitializeThunk` 가 0 일 때 | [정적] |
| 〃 | `+0x2a` | `+0x2f` | `RtlRaiseStatus` (NtContinue 가 돌아오면), 뒤 `INT3` | [정적] |
| `ntdll32!LdrpInitialize` (rva `0x34265`) | `+0xf` | `+0x14` | `_InitSecurityCookie@0` (쿠키 처음일 때만) | [정적] |
| 〃 | `+0x30` | — (`JMP`) | **`LdrpInitializeInternal`** (PDB `_LdrpInitializeInternal@8`, 꼬리호출) | [정적] |
| `ntdll32!LdrpInitializeInternal` (rva `0xb0eac`) | `+0x4d` · `+0x52` · `+0x76` · `+0x8e` | `+0x52` · `+0x57` · `+0x7b` · `+0x93` | `NtCreateEvent` · `_LdrpInitializeHotPatching@0` · `_LdrpLoadPatchedNtdll@8` · `_LdrpInitializationComplete@12` (핫패치 갈래) | [정적] |
| 〃 | `+0xaa` | `+0xaf` | **`_LdrpInitialize`** (PDB `__LdrpInitialize@8`) | [정적] |
| `ntdll32!_LdrpInitialize` (rva `0x342a0`) | `+0x60` · `+0x86` | `+0x65` · `+0x8b` | `NtCreateEvent` · `_LdrInitializeMrdata@0` (첫 스레드) | [정적] |
| 〃 | `+0xcf` | `+0xd4` | **`LdrpInitializeProcess`** (PDB `_LdrpInitializeProcess@8`) | [정적] |
| 〃 | `+0x1ad` | `+0x1b2` | `_LdrpInitializationComplete@12` | [정적] |
| 〃 | `+0x277` · `+0x27f` | `+0x27c` · `+0x284` | `_LdrpInitializeTeb@4` · `_LdrpInitializeThread@4` (나중 스레드) | [정적] |
| 〃 | `+0x29a` | `+0x29f` | `NtTestAlert` | [정적] |
| 〃 | `+0x2a6` · `+0x2ae` | `+0x2ab` · `+0x2b3` | `_LdrpInitializationFailure@4` · `NtTerminateProcess(-1, 상태)` (실패) | [정적] |
| `ntdll32!LdrpInitializeProcess` (rva `0xc9344`, 6597 B) | `+0x5e` · `+0x97` | `+0x63` · `+0x9c` | `NtQueryInformationProcess(-1, 0x24 쿠키, …)` (쿠키 전역이 0 일 때) — `fs:[0xc0]` 갈림이 있는 그 스텁 (아래 3) | [정적] |
| 〃 | `+0x233` | `+0x238` | `RtlImageNtHeaderEx(…, PEB32.ImageBaseAddress)` | [정적] |
| 〃 | `+0x58f` | `+0x594` | `RtlNormalizeProcessParams(PEB32.ProcessParameters)` (정규화 안 됐을 때) | [정적] |
| 〃 | `+0x831` | `+0x836` | `NtQuerySystemInformation(0xc5, …)` | [정적] |
| 〃 | `+0x8d2` | `+0x8d7` | `_RtlpInitEnvironmentBlock@0` → `+0x85/+0x8a _RtlpWow64ThunkEnvironmentTo32@0` · `+0x8f/+0x94 _RtlpWow64GetNativeSystemInformation@16` → `+0x7/+0xc NtWow64GetNativeSystemInformation(1, 버퍼, 0xc, 0)` · `+0x9f/+0xa4 _RtlpSetEnvironmentVariable@8` | [정적] |
| 〃 | `+0xa22` · `+0xaa6` · `+0xb03` · `+0xb6f` | `+0xa27` · `+0xaab` · `+0xb08` · `+0xb74` | `NtOpenDirectoryObject(\KnownDlls32)` · `NtOpenSymbolicLinkObject(KnownDllPath)` · `NtQuerySymbolicLinkObject` · `NtClose` | [정적] |
| 〃 | `+0xe34` | `+0xe39` | `_LdrpInitializeNtdllDataTableEntry@16` (ntdll32 항목) | [정적] |
| 〃 | `+0xfe4` · `+0x10c6` | `+0xfe9` · `+0x10cb` | `_LdrpAllocateModuleEntry@4` · `_LdrpInsertDataTableEntry@4` (exe 항목) | [정적] |
| 〃 | `+0x14d6` | `+0x14db` | `_LdrpInitializeKernel32Functions@4` | [정적] |
| 〃 | `+0x169f` | `+0x16a4` | `_LdrpMapAndSnapDependency@4` | [정적] |
| 〃 | `+0x1735` | `+0x173a` | `_LdrpDoDebuggerBreak@0` (PEB32 `BeingDebugged` ≠ 0 일 때) | [정적] |
| 〃 | `+0x17fc` | `+0x17fe` | **`CALL ESI` = `_Kernel32ThreadInitThunkFunction`** (`+0x17e2` 에서 읽음) — **64비트 `+0x17fc/+0x17fe` (`g_LdrpWow64LdrpInitialize`) 와 오프셋만 같은 다른 줄** | [정적] |
| 〃 | `+0x1857` | `+0x185c` | `LdrpInitializeGraphRecurse` | [정적] |
| 〃 | `+0x1913` | `+0x1918` | `_LdrpCallTlsInitializers@8` | [정적] |
| 〃 | `+0x1993` | `+0x1995` | `CALL ESI` = PEB32 `PostProcessInitRoutine` (`+0x1981`, 0 이 아닐 때) | [정적] |
| `ntdll32!KiUserExceptionDispatcher` (rva `0x7bf70`) | `+0xa` · `+0x18` · `+0x25` · `+0x45` | `+0xf` · `+0x1d` · `+0x2a` · `+0x4a` | `RtlDispatchException(기록, 문맥)` · `NtContinue(문맥, 0)` · `NtRaiseException(기록, 문맥, 0)` · `RtlRaiseException` | [정적] |
| `ntdll32!KiUserApcDispatcher` (rva `0x7be40`) | `+0x32` · `+0x43` · `+0x4b` | `+0x34` · `+0x48` · `+0x50` | `CALL ECX` (APC 루틴) · `NtContinue(Esp+0x14 문맥, 플래그)` · `RtlRaiseStatus` | [정적] |
| `ntdll32!KiUserCallbackDispatcher` (rva `0x7bf20`) | `+0x34` · `+0x3b` · `+0x43` | `+0x36` · `+0x40` · `+0x48` | `CALL ECX` (PEB32 `+0x2c` 표 칸) · `NtCallbackReturn(0, 0, EAX)` · `RtlRaiseStatus` | [정적] |
| `ntdll32!RtlUserThreadStart` (rva `0x7c000`) | `+0x8` | — (`JMP`) | `__RtlUserThreadStart@8` (rva `0x6e166`) — 앞에 `[Esp+4] ← EAX` · `[Esp+8] ← EBX` | [정적] |
| `ntdll32!Nt*` 스텁 (예 `NtContinue` rva `0x79fd0`) | `+0xa` | `+0xc` | `_Wow64SystemServiceCall@0` (rva `0xb5b70`) = `JMP [Wow64Transition]` | [정적] |
| `wow64!whNtContinue` (rva `0x1d410`) | `+0x9` | `+0xe` | `Wow64NtContinue(32비트 문맥, TestAlert)` | [정적] |
| `wow64!Wow64NtContinue` (rva `0x1d42c`) | `+0x3f` | `+0x44` | `CpuSetContext` → `+0x2d` `BtFuncs+0x88` (= `BTCpuSetContext`) | [정적] |
| 〃 | `+0x1e6` · `+0x20a` | `+0x1ed` · `+0x211` | 64비트 `NtContinueEx` (TEB64 `+0x14b8` 목록에 틀이 있을 때) · 64비트 `NtTestAlert` (목록이 비고 TestAlert 일 때) | [정적] |
| `wow64cpu!BTCpuSetContext` (rva `0x1010`) | `+0x6a` · `+0xa0` | `+0x71` · `+0xa7` | 64비트 `NtSetInformationThread(핸들, 0x1d, 문맥, 0x2cc)` · `RtlCopyContext` (CPU 영역 비트 0 켜짐 + 현재 스레드) | [정적] |
| `wow64!whNtContinueEx` (rva `0x2ddb0`) | `+0x59` | `+0x60` | 64비트 `NtContinueEx` (늘) | [정적] |

- `alias:` 로 나온 callee 는 모두 `Nt*`/`Zw*` 짝 (예 `_NtCreateEvent@20` · `_ZwCreateEvent@20`) 이거나 장식 하나 차이 (`RtlRaiseStatus` · `_RtlRaiseStatus@4`) 라 PDB 이름을 그대로 썼다. CFG 검사 칸 `CALL [0x4b3bb000]` 의 파일 값은 같은 주소 이름 4개 (후보: `_guard_check_icall` · `@_guard_check_icall_nop@4` · `RtlDebugPrintTimes` · `_RtlDebugPrintTimes@0`) 라 "CFG 검사" 로만 적는다.

### 1. 입구 사슬 (B1) [정적]

- **받는 것**: 머리 `MOV EDI,EDI; PUSH EBP; MOV EBP,ESP` 뒤 `+0x5 EDX = [EBP+0xc]` · `+0x8 ECX = [EBP+8]`. 들어올 때 기준으로 **`[Esp+4]` = 문맥 (PCONTEXT)**, **`[Esp+8]` = ntdll32 기준 주소**, `[Esp]` 는 아무도 안 읽는 반환 칸. 이 둘을 `ECX` · `EDX` 로 `LdrpInitialize` 에 넘긴다 (fastcall 꼴).
- **사슬** (64비트 run15 ~ run17 꼴):

| 순 | 함수 (PDB 장식 이름 · rva · 크기) | 받는 것 | 부르는 줄 | 64비트 대응 |
|---|---|---|---|---|
| 0 | `LdrInitializeThunk` (`0x34230`, 72 B) | 스택 `[Esp+4]` · `[Esp+8]` | `+0xc → +0x11` `LdrpInitialize`, 돌아오면 `+0x24 → +0x29 NtContinue(문맥, 1)` | `+0x9 → +0xe` · `+0x13 → +0x18 NtContinue(문맥, 1)` |
| 1 | `LdrpInitialize` = `_LdrpInitialize@8` (`0x34265`, 53 B) | `ECX` 문맥 · `EDX` 기준 | `+0x30 JMP LdrpInitializeInternal` | `+0x3c JMP` |
| 2 | `LdrpInitializeInternal` = `_LdrpInitializeInternal@8` (`0xb0eac`, 192 B) | 〃 | `+0xaa → +0xaf _LdrpInitialize` (TEB32 `SameTebFlags & 0x4000` 이면 `+0x26 JNZ` 로 안 부름) | `+0x5a` |
| 3 | `_LdrpInitialize` = `__LdrpInitialize@8` (`0x342a0`, 667 B) | 〃 | 첫 스레드 (`+0x2f CMPXCHG [_LdrpProcessInitialized]` 0 → 1 이김): `+0x8b LdrInitializeMrdata` → `+0xcf → +0xd4 LdrpInitializeProcess` | `+0x16c` |
| 4 | `LdrpInitializeProcess` = `_LdrpInitializeProcess@8` (`0xc9344`, 6597 B) | `ECX` 문맥 (`+0x15`) · `EDX` 기준 (`+0xf`, 뒤에 `+0x56a _LdrpSystemDllBase`) | 아래 2 | — |

- `LdrInitializeThunk +0x11 ~ +0x19`: `_LdrDelegatedLdrInitializeThunk` 가 0 이 아니면 떨어진 범위 `+0x85a48 ~ +0x85a5d` 에서 그 함수를 `(문맥, _LdrpDelegatedNtdllBase)` 로 부르고 `RET 8` — 이번 범위 밖.
- `LdrpInitialize +0x14 ~ +0x23` (쿠키가 처음일 때만): `FS:[0x30]` → PEB32 `+0x10` (매개변수) → `+0x8 Flags` 부호 비트면 `_LdrpIsSecureProcess = 1`.
- **블록 `+0x18` 값이 정말 이 함수인가 → 예.** ① 커널 이름 표 0번 글 = `"LdrInitializeThunk"` → `PsWowX86SharedInformation +0x00` (run139 표 B-0, 결과서가 `pefile` 로 다시 읽음). ② `PspPrepareSystemDllInitBlock +0xde ~ +0xf7` 이 이 표를 블록 `+0x18` 부터 통째 복사 (run32 · run139). ③ ntdll32 export `LdrInitializeThunk` rva `0x34230` (flow-writer `pefile` 로도 같음). wow64 가 그 값을 `Ntdll32LoaderInitRoutine` 로 읽어 `Eip` 로 쓴다 (run138).

### 2. `LdrpInitializeProcess` 가 읽는 칸 (B2) [정적]

이름은 ntdll32 PDB. "누가 채우나" 는 run139 (커널) · run138 (wow64) 대조. 같은 칸이 여러 줄이면 첫 줄 + 대표만 (전체는 결과서 2절).

**TEB32 (`FS:[0x18]`) · TEB64 · `Wow64Info`**

| 칸 | 읽는 줄 | 0 이면 | 누가 채우나 |
|---|---|---|---|
| TEB32 `+0x0 ExceptionList` | `__SEH_prolog4` 가 저장 (`_LdrpInitialize +0x7`, `LdrpInitializeProcess +0xa`), `+0x7bd` 에서 되돌림 | 저장만 | 커널 `MmCreateTeb +0x198` = `0xffffffff` (run139 표 10) |
| TEB32 `+0x18 Self` | `LdrpInitializeInternal +0xb`, `_LdrpInitialize +0x12`, `LdrpInitializeProcess +0xbd` · `+0x409` | FS 기준이 TEB32 여야 하고 `+0x18` 이 자기여야 `+0x41b` 판정이 맞음 | 커널 `MmCreateTeb +0x15a` (run139 표 2) |
| TEB32 `+0x30` PEB | `LdrpInitialize +0x14`, `_LdrpInitialize +0x45`, `LdrpInitializeProcess +0xc9` | 널 참조 (`+0x128` 쓰기) | 커널 `MmCreateTeb +0x19b` (run139 표 11) |
| TEB32 `+0xfca SameTebFlags` | `LdrpInitializeInternal +0x1f` (`0x4000` SkipLoaderInit), `_LdrpInitialize +0x38` (`0x2000`) · `+0x238` (`0x40`), 쓰기 `+0x65` (`0x20`), `LdrpInitializeProcess +0x13dc` (`0x400`, 관리형 exe 만) | 첫 스레드는 0 이 정상 (모두 일반 길) | 커널 `PspAllocateThread +0x7db` (run139) |
| **TEB32 `+0xfdc WowTebOffset`** | **`+0x40f`** → `+0x415 TEST` · `+0x417 JNS` → 음수면 `+0x419 ADD EAX,ECX` | 음수 (`−0x2000`) 면 `EAX` = **TEB64**. 0 · 양수면 TEB32 그대로 (→ 아래 `+0xe38` 갈래) | 커널 `MmCreateTeb +0x1d2` = `−0x2000` (run139 표 18) |
| TEB32 `+0xe38 TlsSlots[10]` | `+0x420` — `EAX` 가 TEB32 일 때 (`+0x41b CMP EAX,[EAX+0x18]` 같음) | 그 값 0 이면 `+0x42e` 널 참조 | 아무도 안 씀 `[공백]` — WoW 길은 이 줄을 안 탄다 |
| **TEB64 `+0x14d0 TlsSlots[10]`** | **`+0x428`** (`+0x41e JNZ` — TEB64 `+0x18` 은 자기가 아님) | 0 이면 `+0x42e` 널 참조 | wow64 `Wow64LdrpInitialize` (run138, `Wow64Info` = PEB32 + `0x490`) |
| **`Wow64Info +0x0`** | **`+0x42e`** → `+0x430 _NativePageSize` → `+0x43b ~ +0x448` 비트 세어 `_NativePageShift` | **0 이면 `+0x441 ~ +0x446` 고리가 안 끝난다** `[정적 읽기]` — 2 의 거듭제곱 (`0x1000`) 이어야 함 | wow64 `ProcessInit +0x54` (run138) |

**PEB32 (`_PEB`, sizeof `0x490`)** — 대표 칸

| 칸 | 읽는 줄 | 0 이면 | 커널이 쓰나 (run139 27칸) |
|---|---|---|---|
| `+0x0 InheritedAddressSpace` | `_LdrpInitialize +0x1c8` (첫 스레드 아닐 때) | 새 프로세스는 0 | 예 |
| `+0x2 BeingDebugged` | `+0x172f` → `+0x1735 LdrpDoDebuggerBreak` | 멈춤 안 함 | 안 씀 (디버거 붙을 때 `[공백]`) |
| `+0x3 BitField` | `+0x9d0` (`2`) · `+0x1041` (`0x10`) · `+0x1072` (`4`) | 일반 길 (`\KnownDlls32` 도 엶) | 예 |
| `+0x8 ImageBaseAddress` | `+0x22c` → `+0x233 RtlImageNtHeaderEx` | NT 헤더 못 찾음 → 뒤에서 널 참조 `[추측]` | 예 |
| `+0xc Ldr` | **쓰기** `+0x128` (`&PebLdr` = `0x4b3b7360`) | — | 안 씀 |
| **`+0x10 ProcessParameters`** | `LdrpInitialize +0x1a`, `+0x177`, `+0x58c` … | **필수 — 0 이면 바로 널 참조** | 예 (`PspWow64ThunkProcessParameters +0x761`) |
| `+0x18 ProcessHeap` · `+0x1c FastPebLock` · `+0xa0 LoaderLock` · `+0x28 CrossProcessFlags` | ntdll32 가 **쓴다** (`+0x779` 칸 주소 넘김 · `+0x726` · `_LdrpInitialize +0x6d` · `+0x81 BTS` / `+0x196 BTR`) | 0 에서 시작 | 안 씀 |
| `+0x64 NumberOfProcessors` | `+0x13f CMP …,1` | 0 · 1 이면 스핀 횟수 셋이 0 | 예 |
| `+0x68 NtGlobalFlag` | `+0x4f0` 외 여섯 줄 | 디버그 플래그 모두 꺼짐 | 예 |
| `+0x70 CriticalSectionTimeout` | `+0x57b` · `+0x57e` → `_RtlpTimeout` | 그대로 쓰고 `_RtlpTimeoutDisable` 판정 | 예 |
| `+0x14c PostProcessInitRoutine` | `+0x1981` → `+0x1993 CALL ESI` | 안 부름 | 안 씀 |
| `+0x1d8 AppCompatFlags` | `+0x6f6` · `+0x1612` · `+0x1635` | 보정 꺼짐 | 안 씀 `[추측]` |
| `+0x1e8 pShimData` | `+0x56f` | shim 안 올림 | wow64 `ProcessInit +0x188` 이 PEB64 에서 복사 (run138) |
| `+0x2c KernelCallbackTable` | (이 함수 아님) `KiUserCallbackDispatcher +0x28` | — | — |

**`RTL_USER_PROCESS_PARAMETERS` (32비트 판)** — `+0x8 Flags` (부호 비트 · 비트 0 정규화 · `0x20000000`), `+0x38/+0x3c ImagePathName`, `+0x24/+0x28 CurrentDirectory` (비었으면 `RtlGetNtSystemRoot` 앞 3 글자로 채움 `+0xbca ~ +0xc36`), `+0x48 Environment` · `+0x290` · `+0x294` (`_RtlpInitEnvironmentBlock`), `+0x2a0 LoaderThreads`, `+0x2b4 ~ +0x2bc` 스레드 풀 셋. 모두 커널 `PspWow64ThunkProcessParameters` 가 만든 블록 (run139).

**ntdll32 자기 안 `LdrSystemDllInitBlock`** (export rva `0x13d2a0`, `.mrdata`, 파일 속 처음 값은 `+0x0 Size = 0x128` 하나 — flow-writer `pefile` 로 `28 01 00 00` 확인)

| 블록 칸 | 읽는 줄 | 0 이면 |
|---|---|---|
| `+0x8` (SystemDllWowRelocation, run32 이름 후보) | `_LdrpInitializeNtdllDataTableEntry +0x96` (← `+0xe34`) — ntdll32 항목 `OriginalBase` 에서 뺌 | 뺄 것 없음 |
| `+0x98` (난수) | `LdrpInitializeProcess +0x39`, `_InitSecurityCookie +0x20`, `_LdrInitializePerThreadRng +0x11` 외 | 덜 섞일 뿐 |
| `+0x9c` (플래그) | 호출된 쪽 (`_LdrpSnapModule +0x28e` 등) — 본체는 안 읽음 | CFG 비트 꺼짐 |
| `+0xa0 ~ +0xb7` (완화 옵션 24 B) | **`+0x34c`** (`MOVSD.REP` 로 지역에 복사) · `+0x39c` · `+0x3d4` · `+0x73b` | "정책 없음" |
| `+0xb8 · +0xbc` · `+0xd8 ~ +0xef` · `+0xf0 · +0xf8` (CFG · 감사) | 호출된 쪽 | 해당 없음 |
| **안 읽는 칸**: `+0x10`, **`+0x18 ~ +0x97` (WOW 16칸)**, `+0xc0`, `+0xc8 · +0xd0` | `_refs` 에 읽는 줄 0 | `+0x18 ~ +0x97` 은 64비트 ntdll 쪽 블록 사본을 wow64 가 읽을 뿐 (run138) |

- KUSD (`0x7ffe0000`, 32비트 코드가 같은 주소로 읽음): `+0x4` · `+0x320 ~ +0x328` (`_LdrInitializePerThreadRng +0x1c ~ +0x36`), `+0x2d5 MitigationPolicies` (`+0x2fe` 외), `+0x330 Cookie` (`+0x13e5`, 관리형 exe 만).

**64비트판 (run18 · run32 · run114) 과 다른 점만**

1. **WoW 전용 읽기** `+0x409 ~ +0x448` (TEB32 `+0xfdc` → TEB64 `TlsSlots[10]` → `Wow64Info[0]` → `_NativePageSize` · `_NativePageShift`).
2. **`UseWOW64` · `LdrpLoadWow64` · `g_LdrpWow64LdrpInitialize` 줄이 없다** (결과서가 ntdll32 `.asm` 전체에서 그 글자 0 — flow-writer 도 `grep` 0). 그래서 32비트 `LdrpInitializeProcess` 는 **끝까지 돈다**: ntdll32 항목 (`+0xe39`) → exe 항목 (`+0xfe9` · `+0x10cb`) → `LdrpInitializeKernel32Functions` (`+0x14db`) → 의존 DLL (`+0x16a4`) → `+0x1712 LdrInitState = 2` → `Kernel32ThreadInitThunkFunction` (`+0x17fe`) → `LdrpInitializeGraphRecurse` (`+0x185c`) → TLS 콜백 (`+0x1918`) → `PostProcessInitRoutine` (`+0x1995`) → `+0x19c7 JMP +0x4e6` (`EAX = 0`) → `+0x7c9 RET`. **x86 exe 의 kernel32 올리기 · import 연결 · TLS 콜백은 이 32비트 코드가 한다.**
3. **`\KnownDlls32`** — `+0xa22 NtOpenDirectoryObject` 의 이름 글 = 파일 상수 `0x4b287a00` (결과서 바이트 확인, 길이 `0x18`). 64비트는 `\KnownDlls`.
4. **환경 변수 WoW 변환** — `_RtlpInitEnvironmentBlock` 안 `_RtlpWow64ThunkEnvironmentTo32` (표 `_Wow64EnvironmentThunkTable` rva `0x1c9c`, 3줄: `ProgramFiles` · `CommonProgramFiles` · `PROCESSOR_ARCHITECTURE`) 와 `NtWow64GetNativeSystemInformation(1, 버퍼, 0xc, 0)` (스텁 `EAX = 0x1f5`).
5. **자기 블록** — 64비트와 이름이 같은 별개 변수 (ntdll32 이미지 안), `+0x18 ~ +0x97` 을 안 읽는다.
6. **첫 syscall 이 별난 스텁** — `+0x5e NtQueryInformationProcess` (아래 3).

### 3. `Nt*` 스텁 (B3) [정적]

- 스텁 수: ntdll32 export `Nt*`/`Zw*` 515 주소 중 **509 = 508 (같은 꼴) + `NtQueryInformationProcess` (다른 꼴)**. 나머지 6 (`NtCurrentTeb` · `NtGetTickCount` · `NtdllDefWindowProc_A/W` · `NtdllDialogWndProc_A/W`) 은 syscall 스텁이 아님 (flow-writer `pefile` 로 다시 셈, 같음).
- 대표 셋 (`.asm` +0x0 ~ +0xc, 바이트는 결과서):

| 스텁 (rva) | 바이트 | 풀이 | `EAX` 쪼개기 | wow64 `sdwhnt32` 칸 |
|---|---|---|---|---|
| `NtContinue` (`0x79fd0`) | `b8 43 00 00 00 ba 70 5b 33 4b ff d2 c2 08 00` | `mov eax,0x43 · mov edx,0x4b335b70 · call edx · ret 8` | 칸 `0x43`, 터보 0 | `whNtContinue`, 인자 8 ✓ |
| `NtAllocateVirtualMemory` (`0x79d00`) | `b8 18 00 00 00 ba 70 5b 33 4b ff d2 c2 18 00` | `… · ret 0x18` | 칸 `0x18`, 터보 0 | `whNtAllocateVirtualMemory`, `0x18` ✓ |
| `NtTerminateProcess` (`0x79e60`) | `b8 2c 00 07 00 ba 70 5b 33 4b ff d2 c2 08 00` | `mov eax,0x7002c · … · ret 8` | 칸 `0x2c`, **터보 7** | `whNtTerminateProcess`, 8 ✓ |
| `_Wow64SystemServiceCall@0` (`0xb5b70`, 6 B) | `ff 25 14 b0 3b 4b` | **`JMP [0x4b3bb014]`** = 기준 + `0x13b014` = **`Wow64Transition`** | — | — |

- **`EAX`** = `mov eax, imm32`: 비트 0 ~ 11 = 칸, 12 ~ 13 = 표, 16 이상 = 터보 번호 (sast/136 A4 와 같음). 508 스텁의 표 번호는 **모두 0 (nt 표)**, 칸 번호 0 ~ 508 이 겹침 없이 508 개 (+ `NtQueryInformationProcess` `0x19` = 509 = nt 표 칸 수). 터보 번호 (flow-writer 가 `pefile` 로 다시 셈): **0 × 409**, 1 × 4 (`NtFlushWriteBuffer` · `NtIsSystemResumeAutomatic` · `NtQueryPortInformationProcess` · `NtYieldExecution`), 2 × 1 (`NtTestAlert`, `EAX = 0x201d3`), 3 × 24, 4 × 8, 5 × 10, 6 × 2, 7 × 14, 8 × 5, 10 × 7, 11 · 13 · 14 · 19 · 20 · 21 · 24 · 28 · 29 · 30 × 1, 12 · 17 · 26 × 4, 27 × 2 — 모두 32칸 터보 표 안 (결과서 목록엔 1 · 2 가 빠짐, 아래 대조).
- **부르는 곳**: 508 스텁 → `call edx` → `_Wow64SystemServiceCall` → **`Wow64Transition` 칸** (`_refs/Wow64Transition.tsv` 의 코드 참조는 이 한 줄뿐). `fs:0xc0` 를 쓰는 명령은 ntdll32 `.text` 전체에서 **`NtQueryInformationProcess +0x11` (rva `0x79d21`) 하나** (결과서 objdump 훑기).
  - `NtQueryInformationProcess` (37 B): `mov eax,0x19 · call $+5 · pop edx` 로 자기 주소를 얻고 `cmp byte [edx+0x14],0x4b` — 그 칸 = 같은 함수 안 **재배치되는 dword `0x4b280000` (선호 ImageBase) 의 윗바이트**. ntdll32 가 윗바이트 `0x4b` 주소에 앉으면 `call fs:[0xc0]`, 아니면 `mov edx,&_Wow64SystemServiceCall · call edx`. 두 길의 값은 같다 (둘 다 `BTCpuGetBopCode()` — `Wow64Transition` 은 `CpupResolveReverseImports +0xb2`, TEB32 `+0xc0` 은 `CpuThreadInit +0x32`, run138). 왜 이렇게 갈리는지는 모름 `[공백]`.
- **인자**: `EDX` 는 인자 포인터가 **아니다** (진짜 네이티브 `KiFastSystemCall` 의 `mov edx,esp` 와 다름). 인자는 32비트 스택 — `call edx` 가 스텁 `+0xc` 반환을 하나 더 얹고, wow64cpu `CpupReturnFromSimulatedCode` 가 `r8d = [esp]` (저장할 `Eip`), `esp += 4`, `r11 = esp + 4` (= 원래 `Esp + 8` = 첫 인자) 를 잡아 `Wow64SystemServiceEx` 의 `RDX` 로 넘긴다 (run138 `0x1a38 ~ 0x1a4a`).
- **돌아온 뒤**: wow64cpu 가 `[r13+0x34]` (`Eax`) 에 결과를 넣고 32비트로 돌아가면 `Eip` = 스텁 `+0xc` 의 **`RET n`** (`n` = wow64 nt 표 인자 바이트).
- **`Wow64Transition`** (export rva `0x13b014`, `.mrdata` 읽기 · 쓰기): **파일 속 값 0** (flow-writer `pefile` 로 `00 00 00 00` 확인). wow64 가 `NtProtectVirtualMemory` 로 잠깐 열어 쓴다 (run138).
- ntdll32 안 네이티브 스텁 `KiFastSystemCall` (`0x7c010`: `mov edx,esp · sysenter`) · `KiIntSystemCall` (`0x7c030`: `lea edx,[esp+8] · int 0x2e`) 은 있지만 **`Nt*` 스텁은 안 부른다** (509 바이트로 확인). 다른 코드가 부르는지는 안 봄 `[공백]`.

### 4. 디스패처 입구 · `NtContinue` 길 (B3 · B4) [정적]

| 함수 (rva) | 스택에서 받는 것 | 하는 일 | 끝 |
|---|---|---|---|
| `KiUserExceptionDispatcher` (`0x7bf70`, 77 B) | `[Esp]` = `EXCEPTION_RECORD*` (`+0x5`), `[Esp+4]` = `CONTEXT*` (`+0x1`) | `CLD` → `RtlDispatchException(기록, 문맥)` (`+0xf`) | `AL ≠ 0` → `NtContinue(문맥, 0)` (`+0x1d`); `AL = 0` → `NtRaiseException(기록, 문맥, 0)` (`+0x2a`); 둘이 돌아오면 `RtlRaiseException` (`+0x4a`) |
| `KiUserApcDispatcher` (`0x7be40`, 82 B) | `[Esp]` = APC 루틴 (`+0x29 POP EAX`), `[Esp+4 ~ +0xc]` = 루틴 인자 셋, `[Esp+0x10]` 비트 0 (`+0x22 · +0x26`), **문맥 = `Esp+0x14`** (`+0x1e`), SEH 틀 자리 `Esp+0x310` (`+0x0`) | SEH 걸기 → CFG 검사 → `CALL ECX` (`+0x34`) | SEH 되돌림 (`+0x34` `[EDI+0x2fc]` = `Esp+0x310` ✓) → `NtContinue(문맥, 플래그)` (`+0x48`) → `RtlRaiseStatus` 고리 |
| `KiUserCallbackDispatcher` (`0x7bf20`, 74 B) | `[Esp]` 건너뜀 (`+0x1e ADD ESP,4`), **`[Esp+4]` = 번호** (`+0x21 POP EDX`), 그 뒤 = 콜백 인자; SEH 틀 `[Esp+0x10/+0x14]` | `FS:[0x30]` → **PEB32 `+0x2c KernelCallbackTable`** (`+0x28`) → `[표 + 번호 × 4]` → CFG → `CALL ECX` (`+0x36`) | `NtCallbackReturn(0, 0, EAX)` (`+0x40`) → `RtlRaiseStatus` 고리 |
| `RtlUserThreadStart` (`0x7c000`, 13 B) | **`EAX` = 시작 주소, `EBX` = 인자** (커널 첫 x86 문맥 `Eax` · `Ebx`, run139) | `[Esp+4] ← EAX` · `[Esp+8] ← EBX` → `JMP __RtlUserThreadStart@8` | 그 뒤는 범위 밖 (Q266) |

- 디스패처 셋의 스택 틀을 누가 쌓나는 wow64 쪽 (`Wow64PrepareForException` · `Wow64ApcRoutine` · `Wow64KiUserCallbackDispatcher`) `[추측]` — 이번엔 안 따라감 (Q266).
- **32비트 `NtContinue` 가 가는 길** (wow64 · wow64cpu `.asm`):
  1. 스텁 (칸 `0x43`) → `Wow64SystemServiceEx` → `whNtContinue` (`+0x4 DL = [RCX+4]` TestAlert, `+0x7 ECX = [RCX]` 32비트 문맥) → `+0x9 → +0xe Wow64NtContinue`.
  2. `Wow64NtContinue +0x3f → +0x44 CpuSetContext` → `+0x2d BtFuncs+0x88` = **`BTCpuSetContext`**: 현재 스레드 (`+0x49 CMP RBX,-2`) 이고 CPU 영역 `[영역] & 1` 이 켜져 있으면 (`+0x54`) `RtlCopyContext` 로 바로 덮고 (`+0xa0`), 아니면 **`NtSetInformationThread(RBX, 0x1d, 문맥, 0x2cc)`** (`+0x59 ~ +0x6a`). 32비트 코드가 syscall 중일 땐 비트 0 이 꺼져 있다 (`RunSimulatedCode +0x2f BTR`, run138) → **보통 `NtSetInformationThread(0x1d)` 쪽** `[정적 추론]`.
  3. `Wow64NtContinue +0x97` TEB64 `+0x14b8` (`TlsSlots[7]`) 목록 머리 → `+0xa1 JZ` (비었으면 `+0x205`) / `+0xa7 CMP [RBX+0x10],R13` (이 32비트 문맥을 가진 틀) → 있으면 그 틀의 64비트 문맥으로 **64비트 `NtContinueEx`** (`+0x1e6 → +0x1ed`). 비었으면 TestAlert 일 때 **64비트 `NtTestAlert`** (`+0x20a → +0x211`), `+0x216 EAX = 32비트 문맥 Eax` 를 돌려줌 → 32비트로 복귀.
  4. 32비트 `NtContinueEx` 스텁 (칸 `0xa5`) 은 따로 `whNtContinueEx +0x59 → +0x60` 로 **늘** 64비트 `NtContinueEx`.
- 정리: **`LdrInitializeThunk +0x24` 의 `NtContinue` 는 "문맥을 CPU 영역에 넣기 (+ 보통 `NtSetInformationThread(0x1d)`) + `NtTestAlert`"** 이고, 64비트 `NtContinueEx` 는 `+0x14b8` 목록에 틀이 있을 때뿐이다. 첫 스레드 때 그 목록이 비었는지는 `[공백]` — 결과서가 wow64 `.text` 에서 `+0x14b8` 에 **넣는** 줄을 못 찾음 (쓰는 줄은 `Wow64NtContinue +0xca` · rva `0x20723` 둘) → 처음엔 0 일 것 `[추측]` (Q265 · Q266).
- 그러면 `NtContinue` 뒤 32비트로 다시 들어갈 때 문맥 = 사본의 처음 값 (`Eip` = 32비트 `RtlUserThreadStart`, `Eax` = 진입점, `Ebx` = PEB32 — run139 첫 x86 문맥) 으로 이어진다 `[추측: run138 · run139 이어 읽기]` (Q265).

### 5. 이름 8개 · ntdll32 export rva (B4 → run139 표 B-0 에 덧붙임) [정적: PE]

| 슬롯 `PsWowX86SharedInformation +` | 블록 `LdrSystemDllInitBlock +` | 이름 (run139 표 B-0) | ntdll32 export rva (번호) | 읽는 이 |
|---|---|---|---|---|
| `+0x00` | `+0x18` | `LdrInitializeThunk` | `0x34230` (151) | wow64 `Ntdll32LoaderInitRoutine` → 32비트 `Eip` (run138) |
| `+0x08` | `+0x20` | `KiUserExceptionDispatcher` | `0x7bf70` (117) | wow64 (run138) |
| `+0x10` | `+0x28` | `KiUserApcDispatcher` | `0x7be40` (115) | wow64 |
| `+0x18` | `+0x30` | `KiUserCallbackDispatcher` | `0x7bf20` (116) | wow64 |
| `+0x20` | `+0x38` | **`RtlUserThreadStart`** | `0x7c000` (1619) | 커널 `PspWow64InitThread +0x15d` (run139), wow64 `ThunkStartupContext64TO32 +0x74` (run138) |
| `+0x28` | `+0x40` | `RtlpQueryProcessDebugInformationRemote` | `0xd1470` (1692) | 안 봄 |
| `+0x30` | `+0x48` | (이름 없음 — **ntdll32 기준 주소**) | — | wow64 `NtDll32Base` (run138) |
| `+0x38` | `+0x50` | **`LdrSystemDllInitBlock`** | `0x13d2a0` (191) | 커널 `PspPrepareSystemDllInitBlock +0x2e2` (run139) |
| `+0x40` | `+0x58` | `RtlpFreezeTimeBias` | `0x139798` (1669) | 안 봄 |

- rva 8개는 flow-writer 가 `pefile` 로 다시 읽어 같다 (번호는 결과서). 전달 export 아님.
- **run139 표 B-0 과 칸 오프셋 · 이름 · 순서 모두 같다** (어긋남 없음). 이번에 보탠 것은 rva 열과 블록 열뿐.

### 호출 관계 (한 장)

```
(wow64cpu RunSimulatedCode — IRETQ, CS 0x23 · Eip = 블록 +0x18 값, run138)
ntdll32!LdrInitializeThunk   [Esp+4] 문맥 · [Esp+8] ntdll32 기준                              [정적]
 ├─ (1) +0x11  LdrpInitialize                       (_LdrpInitialize@8)
 │              └─ +0x30 JMP  LdrpInitializeInternal  (+0x1f SameTebFlags 0x4000 이면 건너뜀)
 │                             └─ +0xaf  _LdrpInitialize   (__LdrpInitialize@8, UseWOW64 비교 없음)
 │                                        ├─ (1) +0x8b  LdrInitializeMrdata
 │                                        └─ (2) +0xd4  LdrpInitializeProcess   끝까지 돌고 +0x7c9 RET
 │                                                    ├─ (1) +0x63 · +0x9c  NtQueryInformationProcess(0x24)  ← fs:[0xc0] 갈림 스텁 (쿠키 0 일 때)
 │                                                    ├─     +0x40f ~ +0x430  TEB32+0xfdc → TEB64+0x14d0 → Wow64Info[0]
 │                                                    ├─ (2) +0x8d7  RtlpInitEnvironmentBlock  (+0x8a 환경 WoW 변환 · +0x94 NtWow64GetNativeSystemInformation)
 │                                                    ├─ (3) +0xa27  NtOpenDirectoryObject(\KnownDlls32)
 │                                                    ├─ (4) +0xe39  LdrpInitializeNtdllDataTableEntry   (블록 +0x8)
 │                                                    ├─ (5) +0x14db LdrpInitializeKernel32Functions
 │                                                    ├─ (6) +0x16a4 LdrpMapAndSnapDependency
 │                                                    ├─ (7) +0x17fe Kernel32ThreadInitThunkFunction  (CALL ESI)
 │                                                    ├─ (8) +0x185c LdrpInitializeGraphRecurse
 │                                                    └─ (9) +0x1918 LdrpCallTlsInitializers
 └─ (2) +0x29  NtContinue(문맥, 1) ⇢ wow64!whNtContinue → Wow64NtContinue
                                       ├─ +0x44  CpuSetContext ⇢ wow64cpu!BTCpuSetContext +0x71 NtSetInformationThread(0x1d)
                                       ├─ +0x1ed NtContinueEx   (TEB64 +0x14b8 목록에 틀이 있을 때만)
                                       └─ +0x211 NtTestAlert    (목록이 비고 TestAlert)
ntdll32!Nt* (509)   mov eax,N · mov edx,&_Wow64SystemServiceCall · call edx · ret n
 └─ +0xc  _Wow64SystemServiceCall   JMP [Wow64Transition]  ⇢ wow64cpu (run138)
          (NtQueryInformationProcess 만: 기준 윗바이트 0x4b 면 call fs:[0xc0])
```

(오프셋은 ret 자리. `⇢` 는 간접 · syscall 다리. 콜스택으로 본 적 없음.)

## 확인된 것

- 32비트 `LdrInitializeThunk` 가 `[Esp+4]` 문맥 · `[Esp+8]` ntdll32 기준을 받고, 64비트와 같은 이름 사슬로 `LdrpInitializeProcess` 까지 간다 — `[정적]`.
- 블록 `+0x18` 값 = 32비트 `LdrInitializeThunk` — `[정적: PE]` (이름 표 + export rva) + `[정적]` (복사 줄, run32 · run139).
- 32비트 `LdrpInitializeProcess` 에 `UseWOW64` 갈래가 없어 끝까지 돌고 돌아온다. x86 exe 의 kernel32 · import · TLS 콜백은 이 32비트 코드 몫 — `[정적]`.
- 32비트 로더가 읽는 칸 목록 (TEB32 · TEB64 `TlsSlots[10]` · `Wow64Info[0]` · PEB32 · 32비트 매개변수 · 자기 블록 · KUSD) — `[정적]`.
- `Nt*` 스텁 509개 꼴 · `EAX` 비트 · `Wow64Transition` 한 칸 · `fs:[0xc0]` 는 한 스텁뿐 · `Wow64Transition` 파일 값 0 — `[정적]` + `[정적: PE]`.
- 디스패처 셋 · `RtlUserThreadStart` (32비트) 입구가 받는 것 — `[정적]`.
- 32비트 `NtContinue` → `whNtContinue` → `Wow64NtContinue` → `CpuSetContext` → `BTCpuSetContext` 길과 `NtContinueEx` 를 부르는 조건 — `[정적]`. 첫 스레드에서 어느 쪽인지는 `[공백]`.
- 이름 8개 모두 ntdll32 export 에 있고 `[4]` = `RtlUserThreadStart` — `[정적: PE]`.

## 앞 run 과 이어 읽기 (모두 맞음)

- **run138 (wow64 DLL)**:
  - `Wow64SetupInitialCall` 이 놓는 `[Esp+4]` 사본 · `[Esp+8]` `NtDll32Base` · `Eip ← Ntdll32LoaderInitRoutine` = 이번 받는 쪽 `LdrInitializeThunk +0x5 · +0x8` 과 맞는다. → run138 맨 위 "고쳐 읽기 (run140)".
  - run138 "32비트 `Nt*` 스텁이 어느 쪽을 부르는지는 Q253c" → **`Wow64Transition` (508 + 한 스텁의 다른 갈래)**, `fs:[0xc0]` (TEB32 `WOW32Reserved`) 는 `NtQueryInformationProcess` 만.
  - run138 번호 비트 (12 ~ 13 표 · 16 이상 터보) 와 nt 표 509칸 = 이번 스텁 509개와 맞는다.
- **run139 (커널)**:
  - TEB32 `+0xfdc = −0x2000` (`MmCreateTeb +0x1d2`) — 32비트 로더가 이 값이 **음수여야** TEB64 로 간다. 64비트 쪽은 TEB64 `+0x180c = +0x2000` (`+0x1ca`) 이 **양수여야** `UseWOW64` (run18 `+0x5f2`). 같은 쌍둥이 칸이 양쪽 갈림을 다 정한다 `[정적: 이어 읽기]`.
  - TEB32 `+0x18` Self · `+0x30` PEB32 · `+0x0` `0xffffffff` · `SameTebFlags` 가 이번 읽는 칸과 맞는다. PEB32 `+0x10` (`PspWow64ThunkProcessParameters +0x761`) 이 0 이면 32비트 로더가 바로 죽는다.
  - 표 B-0 — 위 5, 어긋남 없음.
  - 첫 x86 문맥 `Eax` = 진입점 · `Ebx` = PEB → 32비트 `RtlUserThreadStart` 가 `EAX` · `EBX` 를 스택 인자로 옮긴다 (`+0x0` · `+0x4`) — 맞물린다.
- **run15 ~ run17 · run19 (64비트 사슬)** — 오프셋 대응: `LdrInitializeThunk +0xe` ↔ `+0x11`, `LdrpInitialize +0x3c` ↔ `+0x30`, `LdrpInitializeInternal +0x5a` ↔ `+0xaf`, `_LdrpInitialize +0x16c` ↔ `+0xd4`, `NtContinue` `+0x18` ↔ `+0x29`. 64비트 `LdrpInitializeProcess +0x17fc/+0x17fe` (`g_LdrpWow64LdrpInitialize`, run19) 와 32비트 `+0x17fc/+0x17fe` (`Kernel32ThreadInitThunkFunction`) 는 **오프셋만 같은 다른 줄** — 64비트에서 그 함수는 `+0x1b59/+0x1b5e`, 그래프는 `+0x1bb6/+0x1bbb` (64비트 `.asm`).
- **run32** 블록 칸 이름 후보 (`+0x8` 등) 그대로 — 32비트 블록도 같은 꼴 (`Size 0x128`).

## flow-writer 대조 (결과서와 `.asm` · PE 가 다르게 적힌 곳)

- **터보 번호 목록**: 결과서 3절은 "0 이 409개, 3 · 4 · 5 · … · 30 이 나머지" 라고 1 · 2 를 뺐다. 스텁 바이트를 다시 세면 **1 × 4 (`NtFlushWriteBuffer` · `NtIsSystemResumeAutomatic` · `NtQueryPortInformationProcess` · `NtYieldExecution`), 2 × 1 (`NtTestAlert` `0x201d3`)** 이 더 있다 (합 99 = 508 − 409 로 맞음). `ms/disassemble/ntdll32/NtTestAlert.asm +0x0 MOV EAX,0x201d3` 도 같다. "32칸 터보 표 안" 이라는 결론은 그대로.
- `LdrpInitializeProcess +0x435` 는 `[_NativePageShift] = EBX` (결과서 "= 0" — 이 자리 `EBX` 가 0 이라는 뜻, `+0x43f MOV ECX,EBX` 도 같은 값으로 셈 시작). 고리 앞 `+0x43b TEST DL,AL; +0x43d JNZ` 가 하나 더 있다 (비트가 이미 맞으면 고리 건너뜀).
- `+0xa5d` 의 갈 곳을 결과서는 `+0x4b3497e4` 로 적었는데, 함수 기준 오프셋은 **`+0x4a0`** (주소 `0x4b3497e4`). `+0xac7` 도 같은 곳으로 간다.
- `\KnownDlls32` 글 상수 `0x4b287a00` 은 `+0xa22` 줄이 아니라 `+0x2f` 에서 지역 칸에 담아 둔 것을 `+0xa1d` 쪽 객체 속성으로 넘긴다 (결과서 "`+0xa22` 의 이름 = 상수" 와 뜻은 같음).
- `_LdrpInitializeNtdllDataTableEntry +0x96` 은 `+0x94 JNZ` 를 지나야 (그 자리 `EBX` = 0) 탄다 — 조건부 줄.
- `_RtlpWow64ThunkEnvironmentTo32` 의 표 이름 rva `0x1c9c` 는 `.asm` 주석 (`_Wow64EnvironmentThunkTable+0x8` = `0x4b281ca4`) 과 맞는다.
- `NtWow64GetNativeSystemInformation` 은 Ghidra 주석에 같은 주소 이름 3개 (`RtlGetNativeSystemInformation` 포함), `pdb_aliases.py --lookup ntdll32.dll 0x7baf0` 는 2개 (`_NtWow64GetNativeSystemInformation@16` · `_ZwWow64GetNativeSystemInformation@16`) — 셋째는 export 이름으로 보임 `[추측]`. 문서는 `Nt…` 이름으로 씀.
- 그 밖 위 "환경" 의 줄 모두 결과서와 같다.

## 정정

- 틀린 걸 단정한 run 은 없다. 바로잡는 것은 **sast/136 결과서 "다음 요청 후보 1 ④" 의 칸 번호 표** (결과서 파일은 안 고침):
  - 거기: "`[4]`(+0x38) `RtlUserThreadStart` `[추측]` · `[5]`(+0x40) · `[6]` 기준 주소 (+0x48) · `[7]`(+0x50, 미사용)".
  - 바로: 블록 `+0x38` = `RtlUserThreadStart` (**확정**), `+0x40` = `RtlpQueryProcessDebugInformationRemote`, **`+0x48` = 이름 없는 ntdll32 기준 주소 (이름 표 줄이 아님)**, **`+0x50` = `LdrSystemDllInitBlock`**, **`+0x58` = `RtlpFreezeTimeBias`**. 이름 8개는 블록 `+0x18 ~ +0x40` · `+0x50` · `+0x58` 에 있다. wow64 가 읽는 `+0x18 · +0x20 · +0x28 · +0x30 · +0x38 · +0x48` 는 그대로 맞다.
- run138 맨 위에 "고쳐 읽기 (run140)" 한 줄 — 위 ④ 표 바로잡기, `[Esp+4]` · `[Esp+8]` · `Eip` 의 받는 쪽 확인, 스텁이 부르는 쪽.
- run139 는 고칠 줄 없음 (표 B-0 · TEB32 칸 모두 맞음).

## 아직 확인 필요

- **동적 → Q265** (결과서 "동적으로 확인할 것" 1 ~ 9 — Q253d · Q260 · Q262 VM 과 합침, x86 시험 exe 필요). 멈춤점은 **ntdll32 로드 주소 + rva** (주소는 `dq nt!PsWowX86SharedInformation+0x30 L1` 또는 `lm`), 32비트 레지스터는 `.effmach x86`:
  1. `ntdll32+0x34230` — `r esp`, `dd esp L3` (`[esp+8]` = 기준?), 세그먼트 (`0x23 · 0x2b · 0x53`), `dd fs:[0x18]` · `fs:[0x30]` · **`fs:[0xfdc]` (`0xffffe000`?)** · `fs:[0xfca]`.
  2. 사슬 — `ntdll32+0x342a0` · `+0xc9344` · `+0x34241` (= `LdrInitializeThunk +0x11`, 돌아옴) · `+0x34259` (= `+0x29`, **걸리면 안 됨**).
  3. `ntdll32+0xc9772` (`LdrpInitializeProcess +0x42e`) — `r eax`, `dd @eax L1` (**`0x1000`**), TEB64 `+0x14d0`.
  4. `dd ntdll32+0x13d2a0 L4a` — 블록 칸.
  5. `ntdll32+0xb5b70` · `+0x79d21` (`call fs:[0xc0]` 걸리나) · `+0x79d1f` — `dd ntdll32+0x13b014 L1` = `dd fs:[0xc0] L1`?, `dd esp L4`.
  6. `ntdll32+0xc9c1b` (`+0x8d7`) · `+0x7baf0` — 환경 변수 바뀜.
  7. 32비트 `NtContinue` — `ntdll32+0x79fd0`, `wow64!Wow64NtContinue+0x44` · `+0x1ed` (걸리나) · `+0x211`, `wow64cpu!BTCpuSetContext+0x71` (걸리나), **`dq @$teb+0x14b8 L1` (0 인가)**, CPU 영역 비트 0.
  8. 디스패처 셋 입구 (예외 · APC · 콜백이 날 때).
  9. **ntdll32 가 `0x4b280000` (선호) 에 앉나 `0x7…` 인가** — `NtQueryInformationProcess` 스텁 갈림을 정함.
- **정적 → Q266** (결과서 "다음 요청 후보"): ① `BTCpuSetContext` 가 부르는 **`NtSetInformationThread(0x1d = ThreadWow64Context)` 의 커널 쪽** (CPU 영역 비트 0 을 켜나) + TEB64 `+0x14b8` 목록에 **틀을 넣는 곳** (`Wow64PrepareForException` · `Wow64ApcRoutine` · `Wow64KiUserCallbackDispatcher`) — 32비트 디스패처 스택을 누가 쌓나와 같이. ② ntdll32 초기화가 부르는 `whNt*` 본문 (`whNtQuerySystemInformation` · `whNtWow64GetNativeSystemInformation` · `whNtQueryInformationProcess` · `whNtOpenDirectoryObject` · `whNtOpenSymbolicLinkObject` · `whNtQuerySymbolicLinkObject` · `whNtCreateEvent`). ③ 32비트 `__RtlUserThreadStart@8` → `BaseThreadInitThunk` (32비트) → exe 진입점 (flow.md 의 x86 판). (결과서 후보 3 `LdrpInitializeProcess` 뒤쪽 · 5 `fs:[0xc0]` 까닭은 Q266 안에서 낮은 순위.)
- 그 밖 `[공백]`: `KnownDlls32` 열기 실패 때 상태 반환 규칙, `+0x14bc ~ +0x14cb` 조건 칸 `[ESI+0x5c]` 뜻, `InitialThread` 비트를 커널이 켜나, `KiFastSystemCall` 을 부르는 다른 코드, WinDbg 의 ntdll32 표시 이름.

## PE_emulator-AR 쪽 (결과서 "지금 우리 맞나·틀리나" 요약 — `../PE_emulator-AR` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| AR | 커널 · 엔진이 지어 둘 것 (ntdll32 가 읽음) |
|---|---|
| AR03 | **PEB32** (sizeof `0x490` + **바로 뒤 `Wow64Info`**): `+0x0 · +0x3 · +0x8 · +0x10 · +0x64 · +0x68 · +0x70` (run139 `MiInitializeWowPeb`), **`+0x10` 은 32비트 매개변수 블록 — 0 이면 `LdrpInitialize +0x1d` 에서 바로 죽음**. `+0xc · +0x18 · +0x1c · +0x28 · +0xa0` 은 ntdll32 가 씀 (비워 둠). **32비트 `LdrSystemDllInitBlock` = ntdll32 이미지 안** (기준 + `0x13d2a0`, 슬롯 `+0x38`) — 그대로 올리면 `Size 0x128` 은 이미 있음. 새로 쓸 칸 `+0x8` · `+0x18 ~ +0x58` · `+0x98` · `+0x9c` · `+0xa0 ~ +0xb7` · `+0xd8 ~ +0xef` · `+0xb8 · +0xc0` · `+0xf0 ~ +0x127`. **`Wow64Info[0] = 0x1000`** — 진짜 wow64 를 돌리면 자동, 흉내 내면 직접 (TEB64 `+0x14d0` → 그 주소). **TEB32 `+0xfdc` 는 음수 `−0x2000` 이어야** (0 · 양수면 아무도 안 쓰는 TEB32 `+0xe38` 을 읽어 죽음) |
| AR04 (덧붙임) | TEB32 `+0x0` · `+0x18` · `+0x30` · `+0xfca` (0) · `+0xfdc`. 32비트 코드가 TEB64 (= TEB32 − `0x2000`) 를 직접 읽으므로 **TEB64 도 4GB 아래** (run139 서브 영역과 맞음) |
| AR06 | **32비트 `LdrpInitializeProcess` 는 돌아온다** → `LdrInitializeThunk +0x24 NtContinue(문맥, 1)`. 이것은 에뮬레이터가 보는 64비트 `NtContinue` 가 아니라 wow64 안 `CpuSetContext` (+ 보통 64비트 `NtSetInformationThread(−2, 0x1d)`) · 64비트 `NtTestAlert` 로 간다. "32비트 초기화 끝" 은 그 둘과 CPU 영역 재진입으로 잡아야 `[정적 추론]` |
| AR07 이후 | 초기화 중 syscall: `NtQueryInformationProcess(0x24)` · `NtQuerySystemInformation(0 · 0xc5)` · `NtCreateEvent` · **`NtWow64GetNativeSystemInformation` (칸 `0x1f5`, 32비트 전용)** · `NtOpenDirectoryObject(\KnownDlls32)` · `NtOpenSymbolicLinkObject` / `NtQuerySymbolicLinkObject` (실패하면 초기화가 죽음, `+0xa3e JGE` 아니면 `+0xa5d` → `+0x4a0`) · `NtClose` — 모두 `whNt*` 를 거침. ntdll32 를 선호 기준에 올리면 `NtQueryInformationProcess` 만 `fs:[0xc0]` → **TEB32 `+0xc0` 도 채워져야** (진짜 wow64 `ThreadInit` 이 채움) |

## 디버거 메모 / 도구 메모

- 새 디버거 함정 없음 (정적만).
- **도구 (결과서 "스크립트 문제", 기록만 — 하네스 제안은 오케스트레이터 몫)**:
  - `run_ghidra.sh` 는 모듈 이름을 파일 이름에서 정하고 (`${bin%.*}` 소문자) `find_bin()` 이 `*/System32/<이름>` 을 먼저 고른다 → `ntdll.dll` 로는 SysWOW64 쪽을 못 고르고 64비트 프로젝트와 이름이 겹친다. `fetch_pdb.py` · `pdb_aliases.py` · `pdb_struct.py` 도 `ms/sym/<바이너리 이름>.pdb` 라 같은 문제.
  - 이번 우회: `ms/bin/ntdll32.dll` **하드링크** (바이트 같음, `ms/bin/` 은 커밋 안 됨) → `fetch_pdb.py ntdll32.dll` → `run_ghidra.sh analyze ntdll32.dll`. 결과서 제안: `BIN_PATH=` · `MODULE=` 환경변수, 또는 `ntdll32.dll=SysWOW64/ntdll.dll` 꼴 인자.
  - x86 PDB 이름에 `@N` 장식 (`_LdrpInitializeProcess@8`) — `^이름$` 정규식 · `.asm` 파일 이름에 장식을 넣어야 한다. `LdrInitializeThunk` · `KiUser*` · `Nt*` · `RtlUserThreadStart` 는 장식 없음.
  - `pefile` 은 `python3 -I` 로는 안 잡힌다 (사용자 site-packages 에 있음) — flow-writer 는 scratchpad 에서 `-I` 없이 돌렸다.
- 동적 때 참고 (Q265): 32비트 모듈 멈춤점은 `bp ntdll32기준+rva` 꼴이고 모듈 이름 표시는 확인 전이다. 64비트 `LdrpInitializeProcess+0x17fe` 와 32비트 `+0x17fe` 는 다른 줄이니 모듈을 헷갈리지 말 것 → windbg-kd-runbook pitfalls 에 넣을지는 Q265 때 실제로 보고 정하면 됨.

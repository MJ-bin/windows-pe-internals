# 137 wow64-ntdll32 — 정적 분석 결과

- 요청서: `_workspace/sast/137_wow64-ntdll32.md` (원문 `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` B 절, 앞 결과서 `sast/136_wow64-dlls_result.md`)
- 판정: **분석성공** (B 1 ~ 5 전부 닫음. 정적으로 못 닫은 줄은 `[추측]`·`[공백]` 로 표시, 맨 아래 "동적으로 확인할 것")
- 우선순위: 사용자 메시지대로 **1 → 3 → 2 → 4** 순으로 읽었고, **5 는 sast/135 표 B-0 을 가리키고** ntdll32 export rva 대조만 덧붙임.
- 이 결과서에서 "ntdll32" = `SysWOW64\ntdll.dll` (x86), "ntdll (64)" = `System32\ntdll.dll`.
- 다른 decompiler (sast/135 ntoskrnl) 는 건드리지 않음. ntoskrnl 은 Ghidra 로 안 열고 `pefile` 로 PE 데이터만 읽음 (5절).
- 64비트 쪽 (`ms/sym/ntdll.dll.pdb`, `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`, `ms/ghidra/ntdll.*`) **덮어쓰지 않음** (수정 시각 10/19 그대로 확인).

## 스크립트 문제 (고치지 않음, 제안만)

1. **파일 이름이 겹치는 바이너리를 못 고름.**
   - 증상: `run_ghidra.sh` 는 모듈 이름을 파일 이름에서 정하고 (`module = ${bin%.*}` 소문자) `find_bin()` 이 `*/System32/<이름>` 을 먼저 고른다. `ntdll.dll` 로는 SysWOW64 쪽을 고를 수 없고, 모듈 이름도 `ntdll` 로 정해져서 64비트 프로젝트와 겹친다. `fetch_pdb.py` · `pdb_aliases.py` · `pdb_struct.py` 도 `ms/sym/<바이너리 이름>.pdb` 로 정해서 같은 문제.
   - **우회 (이번에 한 것)**: `ln "…/SysWOW64/ntdll.dll" ms/bin/ntdll32.dll` (하드링크 — 새 파일 이름만 하나 더 생김, 바이트는 그대로, `ms/bin/` 은 커밋 안 됨). 그 이름으로 `fetch_pdb.py ntdll32.dll` → `run_ghidra.sh analyze ntdll32.dll` → `ms/{sym,ghidra,decomp,disassemble}/…ntdll32…`. 스크립트는 안 고침.
   - 제안: `run_ghidra.sh`·`fetch_pdb.py` 에 환경변수 `BIN_PATH=<절대경로>` 와 `MODULE=<모듈 이름>` (없으면 지금 동작) 을 받게 하기. 또는 인자 `ntdll32.dll=SysWOW64/ntdll.dll` 꼴.
2. (스크립트 문제 아님, 알아 둘 것) **x86 PDB 이름은 장식이 붙는다**: `_LdrpInitialize@8` (= 64비트 `LdrpInitialize`), `__LdrpInitialize@8` (= `_LdrpInitialize`), `_LdrpInitializeProcess@8`, `_Wow64SystemServiceCall@0`. 정규식은 장식 포함 (`^_LdrpInitializeProcess@8$`). `.asm`/`.c` 파일 이름에도 `@` 가 들어감. `LdrInitializeThunk`·`KiUser*`·`Nt*`·`RtlUserThreadStart` 는 장식 없음. 문서에는 64비트와 같은 이름 (장식 뗀 것) 을 쓰고 처음 한 번 장식 이름을 밝힘.

## PDB GUID 줄

| 모듈 | PDB 파일 이름 (디버그 디렉터리) | guid+age (바이너리에서 읽음) | 둔 자리 | analyze |
|---|---|---|---|---|
| ntdll32 (`SysWOW64\ntdll.dll`, 1,822,400 바이트, ImageBase `0x4b280000`, SizeOfImage `0x1bf000`, 머신 `0x14c`) | `wntdll.pdb` | `7C1F7D48576AAE47EB48E43D5FB752AD1` | `ms/sym/ntdll32.dll.pdb` (2,281,472 바이트) + `.info.txt` + `.aliases.tsv` (별칭 561곳·이름 1184개) | 함. `ms/ghidra/ntdll32.analyze.log` 54 줄 `SetPdb: ntdll32.dll <- …ntdll32.dll.pdb` 있음, `ExportIndex` 4687 함수 (`ms/decomp/ntdll32/_index.tsv`), 약 75 초 |

- 커널 GUID 확인은 해당 없음 (ntoskrnl 을 Ghidra 로 안 엶).
- 64비트 ntdll (`ms/sym/ntdll.dll.pdb`, 파일 이름 `ntdll.pdb`) 과는 PDB 이름도 다르다 (`wntdll.pdb`).

## 버전 확인 (기존 근거와 대조) — 전부 일치

32비트 ntdll 은 스킬 "알려진 사실" 표에 줄이 없어서, 이미 확인된 **커널·wow64 쪽 근거와 맞춰** 봤다.

| 이번에 본 것 | 기존 근거 | 결과 |
|---|---|---|
| ntdll32 `LdrSystemDllInitBlock` (export, rva `0x13d2a0`) 의 파일 속 첫 dword = `0x128` | sast/135 Q5 : `PspPrepareSystemDllInitBlock(1)` 이 `Size == 0x128` 검사 (`+0x98 ~ +0x9f`) | 일치 (파일 바이트) |
| ntoskrnl `NtdllWowX86Exports` (rva `0xc801a0`, 8칸) 이름 8개 | sast/135 표 B-0, sast/42 | 일치 (pefile 로 다시 읽음, 5절) |
| ntdll32 `_TEB` (PDB) `+0xfdc WowTebOffset` · `+0xfca SameTebFlags` · `+0xc0 WOW32Reserved` · `+0xc4 CurrentLocale` | sast/135 표 (`MmCreateTeb +0x1d2` 가 `+0xfdc` 에 `−0x2000`, `PspAllocateThread +0x7db` 가 `SameTebFlags`), sast/136 (wow64 가 `+0xc0 · +0xc4` 에 씀) | 일치 (칸 이름·오프셋) |
| ntdll32 `_PEB` (PDB) sizeof `0x490`, `+0x10 ProcessParameters`, `+0x64 NumberOfProcessors`, `+0x68 NtGlobalFlag`, `+0x70 CriticalSectionTimeout` | 64비트 PDB `_PEB32` sizeof `0x490` (sast/136), sast/135 Q4 표 (커널이 `+0x64 · +0x68 · +0x70` 을 씀) | 일치 |
| 스텁 `EAX & 0xfff` 와 `RET n` ↔ wow64.dll `sdwhnt32` (`0x3a550`) 의 `ServiceTables[0]` 함수 배열 · 인자 바이트 표 (`0x3e930`) | sast/136 A4 (표 칸 = 번호 비트 0~11, 인자 바이트 표) | 일치 7개 (아래 3절 표) |
| 호출 사슬 `LdrInitializeThunk → LdrpInitialize →(JMP) LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess` | 스킬 "알려진 사실" 64비트 줄 (`LdrInitializeThunk +0xe`, `_LdrpInitialize +0x16c`) 과 같은 사슬 | 사슬 일치. 오프셋은 32비트 코드라 다름 (1절 표) |

## 만든 파일

- `ms/bin/ntdll32.dll` (하드링크, 커밋 안 됨).
- `ms/sym/ntdll32.dll.{pdb,pdb.info.txt,aliases.tsv}`, `ms/ghidra/ntdll32.*` (프로젝트 · analyze · export · refs 로그).
- `ms/decomp/ntdll32/_index.tsv` (4687 함수) + `.c`, `ms/disassemble/ntdll32/*.asm` + `_calls.tsv` + `_refs/{Wow64Transition,LdrSystemDllInitBlock}.tsv`.
  - 앞 사슬: `LdrInitializeThunk` · `_LdrpInitialize@8` · `__LdrpInitialize@8` · `_LdrpInitializeInternal@8` · `_LdrpInitializeProcess@8` (깊이 0).
  - 스텁·디스패처: `NtContinue` · `NtContinueEx` · `NtAllocateVirtualMemory` · `NtTerminateProcess` · `NtCallbackReturn` · `NtTestAlert` · `_Wow64SystemServiceCall@0` · `KiUserExceptionDispatcher` · `KiUserApcDispatcher` · `KiUserCallbackDispatcher` · `RtlUserThreadStart` · `RtlUserFiberStart` · `KiFastSystemCall` · `KiFastSystemCallRet` · `KiIntSystemCall`.
  - 앞쪽 부품 (읽는 칸 확인용, 깊이 0): `_InitSecurityCookie@0` · `_LdrInitializePerThreadRng@4` · `_LdrInitializeMrdata@0` · `_LdrpLocateMrdata@0` · `_LdrpInitializeNtdllDataTableEntry@16` · `_RtlpInitParameterBlock@0` · `_RtlpInitEnvironmentBlock@0` · `_RtlInitializeProcessExceptionChain@4` · `_RtlpWow64ThunkEnvironmentTo32@0` · `_RtlpWow64GetNativeSystemInformation@16` · `NtWow64GetNativeSystemInformation`.
- wow64 프로젝트 (sast/136 에서 analyze 끝, 읽기만): `ms/disassemble/wow64/{whNtContinue,whNtContinueEx,whNtTerminateProcess,whNtTestAlert,Wow64NtContinue,Wow64NtTerminateProcess,CpuSetContext}.asm` (NtContinue 길 때문에 4번 질문에 필요해서 뽑음).
- 임시 스크립트 (PE 표 읽기, 스텁 훑기, 전체 `.text` objdump): `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/s137/` (세션 임시. 근거는 아래 표·줄로 남김). 그중 `n32_text.s` 는 ntdll32 `.text` 전체를 objdump 로 푼 것 (스텁 · `fs:0xc0` 검색용).

---

## 먼저 읽는 한 줄 답

1. **B1 입구**: 32비트 `LdrInitializeThunk` (rva `0x34230`, export 번호 151) 는 **스택에서 `(PCONTEXT, PVOID NtDll32Base)` 둘을 받는다 — `[Esp+4]` = 문맥, `[Esp+8]` = ntdll32 기준 주소 (sast/136 짐작이 맞음)**, `[Esp]` 는 쓰지 않는 반환 칸. 사슬은 64비트와 같다: `LdrInitializeThunk +0xc/+0x11` `LdrpInitialize` → `+0x30` `JMP` `LdrpInitializeInternal` → `+0xaa/+0xaf` `_LdrpInitialize` → `+0xcf/+0xd4` `LdrpInitializeProcess`. 돌아오면 `+0x24/+0x29` **`NtContinue(문맥, TestAlert=1)`**. `LdrSystemDllInitBlock +0x18` 값 = 32비트 `LdrInitializeThunk` 맞음 (커널 이름 표 `[1]` 의 첫 칸이 `"LdrInitializeThunk"` → 블록 `+0x18` 로 복사, 근거 sast/135).
2. **B3 스텁 꼴**: 모든 `Nt*` 스텁 = `mov eax, N ; mov edx, &_Wow64SystemServiceCall ; call edx ; ret n`. **`_Wow64SystemServiceCall` (rva `0xb5b70`, 6 바이트) = `JMP [Wow64Transition]`** (= 내보낸 4바이트 변수 rva `0x13b014`, **파일 속 값 0**). `fs:[0xc0]` 직접 호출은 **`NtQueryInformationProcess` 한 스텁뿐** (ImageBase 상수의 윗바이트가 `0x4b` 일 때만, 아니면 `call edx`). `EAX` = 표 칸 (비트 0~11) + 터보 번호 (비트 16 이상), 표 비트 12~13 은 ntdll32 안 스텁 509개 모두 0 (nt 표) — sast/136 A4 와 맞음. **`EDX` 는 인자 포인터가 아니다** (thunk 주소를 담을 뿐). 인자는 32비트 스택에 있고 wow64cpu 가 `R11 = Esp + 8` 로 잡는다.
3. **B2 `LdrpInitializeProcess` 읽는 칸**: 64비트와 같은 큰 줄기 (`UseWOW64` 갈래·`LdrpLoadWow64` 없음 — 끝까지 돌고 **돌아온다**). 읽는 칸은 TEB32 `+0x18 · +0x30 · +0xfca · +0xfdc`, **TEB64 `TlsSlots[10]` (`+0x14d0`) → `Wow64Info[0]` (= 0x1000)** (TEB32 `+0xfdc` 가 음수면 `TEB32 + WowTebOffset = TEB64` 로 가서 읽음), PEB32 18곳, `RTL_USER_PROCESS_PARAMETERS` 칸들, ntdll32 **자기 안의** `LdrSystemDllInitBlock` (`.mrdata`, rva `0x13d2a0`, 파일에 `Size=0x128` 이미 있음), KUSD 몇 칸. 64비트와 **다른 점**: Wow64Info 읽기, `\KnownDlls32`, 환경 변수 WoW 변환 (`ProgramFiles` ← `ProgramFiles(x86)` …) 과 `NtWow64GetNativeSystemInformation`.
4. **B3/B4 디스패처·NtContinue**: 32비트 `KiUserExceptionDispatcher` = `[Esp]` EXCEPTION_RECORD\*, `[Esp+4]` CONTEXT\* 를 받아 `RtlDispatchException` → 성공이면 `NtContinue(ctx, 0)`, 실패면 `NtRaiseException`. `KiUserApcDispatcher` = `[Esp]` APC 루틴, `[Esp+4..+0xc]` 인자 셋, `[Esp+0x10]&1` 플래그, **CONTEXT 가 `Esp+0x14`** 에 있고 끝나면 `NtContinue(ctx, 플래그)`. `KiUserCallbackDispatcher` = `[Esp+4]` 번호 → `PEB32+0x2c` 표 → 부르고 `NtCallbackReturn`. **`NtContinue` (32비트) → `whNtContinue` → `Wow64NtContinue` → `CpuSetContext` → `BTCpuSetContext` (+ `NtTestAlert`)** 로 가고, 64비트 `NtContinueEx` 는 **TEB64 `+0x14b8` 목록에 이 문맥을 가진 틀이 있을 때만** 부른다. 처음 스레드 시작 때의 `NtContinue` 에서 그 목록이 비어 있는지는 `[공백]` (동적). `BTCpuSetContext` 는 CPU 영역 비트 0 이 꺼져 있으면 **64비트 `NtSetInformationThread(-2, 0x1d, 문맥, 0x2cc)`** 를 부른다 (이건 에뮬레이터 커널이 받아야 할 syscall).
5. **B4 이름 8개**: sast/135 **표 B-0** (`NtdllWowX86Exports`, rva `0xc801a0`) 그대로. ntdll32 export 에 8개 전부 있음 (rva 5절 표). **`[4]` = 32비트 `RtlUserThreadStart` 맞음** (`PsWowX86SharedInformation +0x20` → 블록 `+0x38`). 단 sast/136 "넘김 ④" 의 칸 번호 표는 **이름 8개 중 `LdrSystemDllInitBlock` 이 블록 `+0x50` (슬롯 `+0x38`), `RtlpFreezeTimeBias` 가 `+0x58`, `+0x48` 은 이름 없는 "기준 주소"** 로 바로잡는다 (아래 5절).

---

## (1) B1 — 입구 사슬 표

모든 줄은 `ms/disassemble/ntdll32/<함수>.asm` (+ `_calls.tsv`). `ret=` 는 WinDbg `caller+0x…` 와 같은 값.

| 순 | 함수 (PDB 이름 · Ghidra 이름 · rva · 크기) | 받는 것 | 부르는 줄 (call → ret) | 근거 · 비고 |
|---|---|---|---|---|
| 0 | `LdrInitializeThunk` (rva `0x34230`, 72 B, export #151) | 스택 (stdcall 2인자): `[Esp+4]` = `PCONTEXT` (`+0x8 MOV ECX,[EBP+8]`), `[Esp+8]` = `NtDll32Base` (`+0x5 MOV EDX,[EBP+0xc]`). `[Esp]` = 안 채운 반환 칸 | `+0xc → +0x11` `_LdrpInitialize@8` (`ECX` = 문맥, `EDX` = 기준 주소) | `LdrInitializeThunk.asm` +0x0 ~ +0xc. `MOV EDI,EDI; PUSH EBP; MOV EBP,ESP` 머리 |
| 0' | (같은 함수, 돌아온 뒤) | `+0x11 MOV ESI,[_LdrDelegatedLdrInitializeThunk]` 가 0 이면 | **`+0x24 → +0x29` `NtContinue(문맥 = [EBP+8], TestAlert = 1)`** (`+0x1f PUSH 1`, `+0x21 PUSH [EBP+8]`); 안 바뀌고 돌아오면 `+0x2a → +0x2f` `RtlRaiseStatus` · `+0x2f INT3` | `+0x11 ~ +0x2f`. 0 이 아니면 `+0x19 JNZ` → 떨어진 범위 `+0x85a48 ~` 에서 `_LdrDelegatedLdrInitializeThunk(문맥, _LdrpDelegatedNtdllBase)` 를 부르고 `RET 8` (`+0x85a5d`) — 이 갈래는 이번 범위 밖 |
| 1 | `LdrpInitialize` = `_LdrpInitialize@8` (rva `0x34265`, 53 B) | 레지스터 (fastcall 꼴): `ECX` = 문맥, `EDX` = 기준 주소 (`+0x9 ESI = EDX`, `+0xb EDI = ECX`) | `+0xf → +0x14` `_InitSecurityCookie@0` (쿠키 처음일 때만); **`+0x30` `JMP` → `_LdrpInitializeInternal@8`** (꼬리호출, `_calls.tsv` `tail`) | `_LdrpInitialize@8.asm` +0x0 ~ +0x30. `+0x14 ~ +0x23`: `FS:[0x30]` → PEB32 `+0x10` ProcessParameters → `+0x8 Flags` 부호 비트가 켜지면 `_LdrpIsSecureProcess = 1` (**쿠키가 처음일 때만**). 64비트 `LdrpInitialize` (99 B, `+0x3c JMP`) 와 같은 역할. 콜스택엔 이 프레임이 안 보임 |
| 2 | `LdrpInitializeInternal` = `_LdrpInitializeInternal@8` (rva `0xb0eac`, 192 B) | `ECX` = 문맥 (`+0x12 [ESP+8]`), `EDX` = 기준 (`+0x16 EBX`) | `+0x4d → +0x52` `NtCreateEvent` · `+0x52 → +0x57` `_LdrpInitializeHotPatching` · `+0x76 → +0x7b` `_LdrpLoadPatchedNtdll` (핫패치 켜졌을 때) · `+0x8e → +0x93` `_LdrpInitializationComplete` (이상 `_LdrpHotPatchInitialized` cmpxchg 0→1 이긴 쪽만) · **`+0xaa → +0xaf` `__LdrpInitialize@8`** | `_LdrpInitializeInternal@8.asm`. `+0x1f TEST word [TEB+0xfca],0x4000` (**SkipLoaderInit**) 이 켜지면 `+0x26 JNZ` → `+0xb9` 끝으로 (`__LdrpInitialize` 를 안 부름) |
| 3 | `_LdrpInitialize` = `__LdrpInitialize@8` (rva `0x342a0`, 667 B) | `ECX` = 문맥 (`[EBP-0x20]`), `EDX` = 기준 (`[EBP-0x28]`) | 첫 스레드 (`+0x2f CMPXCHG [_LdrpProcessInitialized]` 0→1 이 이김): `+0x60 → +0x65` `NtCreateEvent` · **`+0x86 → +0x8b` `_LdrInitializeMrdata@0`** · **`+0xcf → +0xd4` `_LdrpInitializeProcess@8`** · `+0x1ad → +0x1b2` `_LdrpInitializationComplete`. 이후 스레드: `+0x277 → +0x27c` `_LdrpInitializeTeb@4` · `+0x27f → +0x284` `_LdrpInitializeThread@4`. 끝에 `+0x29a → +0x29f` `NtTestAlert`; 실패 `+0x2a6 → +0x2ab` `_LdrpInitializationFailure` → `+0x2ae → +0x2b3` `NtTerminateProcess(-1, 상태)` | `__LdrpInitialize@8.asm`. **`UseWOW64` 비교가 없다** (64비트 `_LdrpInitialize +0x22d` 에는 있음 — `grep` 로 32비트 `.asm` 전체에 `UseWOW64`·`g_LdrpWow64*` 0개) |
| 4 | `LdrpInitializeProcess` = `_LdrpInitializeProcess@8` (rva `0xc9344`, 6597 B) | `ECX` = 문맥 (`+0x15 [EBP-0x124]`), `EDX` = ntdll32 기준 (`+0xf [EBP-0xb4]`; 뒤에 `LdrpSystemDllBase` 에 저장 `+0x56a`) | 아래 2절 | 64비트의 `+0x17c6 LdrpLoadWow64` · `+0x17fc g_LdrpWow64LdrpInitialize` 줄이 **없다**. 큰 줄기 (PEB.Ldr → IFEO → 힙 → `\KnownDlls32` → ntdll 항목 → exe 항목 → `LdrpInitializeGraphRecurse` → TLS → 끝) 는 64비트와 같다 |

- 64비트 대응 (스킬 "알려진 사실" 표): `LdrInitializeThunk +0xe → LdrpInitialize`, `LdrpInitialize +0x3c JMP`, `LdrpInitializeInternal +0x5a → _LdrpInitialize`, `_LdrpInitialize +0x16c → LdrpInitializeProcess`. 32비트는 각각 `+0x11`, `+0x30`, `+0xaf`, `+0xd4`.
- **`LdrSystemDllInitBlock +0x18` 의 값이 정말 `LdrInitializeThunk` 인가**: 예. 근거 셋. ① 커널 이름 표 `NtdllExportInformation[1]` (rva `0xc7ff90` → `{0xc801a0, 8}`) 의 0번 줄 = `{"LdrInitializeThunk", 슬롯 PsWowX86SharedInformation+0x0}` (pefile 로 확인, 5절). ② `PspPrepareSystemDllInitBlock +0xde ~ +0xf7` 가 `PsWowX86SharedInformation` 을 블록 `+0x18` 부터 통째로 복사 (sast/135 Q5). ③ ntdll32 export 에 `LdrInitializeThunk` 가 있다 (rva `0x34230`). 커널이 계산하는 유저 주소 = ntdll32 기준 + `0x34230` (`PspLookupEntryPoint`, sast/42). wow64 가 이 값을 `Ntdll32LoaderInitRoutine` 로 복사해 `Eip` 로 쓴다 (sast/136 A3).
- **`[Esp+4]` · `[Esp+8]` 가 맞나**: 예 (위 0 행). 사본 문맥 주소·기준 주소는 wow64 `Wow64SetupInitialCall` 이 `Esp − 0x2cc` 아래 틀에 놓는다 (sast/136 A3 15 단계).
- 이 사슬이 처음으로 부르는 syscall (스텁 이름만): `NtCreateEvent` (`_LdrpInitializeInternal +0x4d`, `__LdrpInitialize +0x60`), `NtQuerySystemInformation(0)` (`_LdrInitializeMrdata +0x22`), `NtQueryInformationProcess(-1, 0x24 = ProcessCookie)` (`_LdrpInitializeProcess +0x5e`, `+0x97` — 쿠키 전역이 0 일 때만). 뒤쪽 목록은 2절 끝.

## (2) B2 — `LdrpInitializeProcess` 가 읽는 칸

범위: `_LdrpInitializeProcess@8.asm` 전체 (+0x0 ~ +0x19dc) + 앞쪽 부품. "처음 읽는" 순서대로, 같은 칸이 여러 곳이면 첫 줄 + 대표만. 이름은 ntdll32 PDB (`pdb_struct.py ntdll32.dll _PEB _TEB _RTL_USER_PROCESS_PARAMETERS`). "커널이 쓰나" 는 sast/135 (Q4 PEB32 표, MmCreateTeb 19줄) 대조.

### TEB32 (`FS:[0x18]`) · TEB64 · Wow64Info

| 구조체 · 칸 | 읽는 줄 | 0 이면 · 비고 | 누가 채우나 |
|---|---|---|---|
| TEB32 `+0x0 NtTib.ExceptionList` (`FS:[0]`) | `__SEH_prolog4` 가 이전 값을 저장 (`__LdrpInitialize +0x7`, `_LdrpInitializeProcess +0xa`) 하고 끝에서 `FS:[0]` 로 되돌림 (`_LdrpInitializeProcess +0x7bd`) | 어떤 값이든 저장만 함 `[추측: 예외가 나서 체인을 걸을 때 끝 표시는 -1 이어야 함]` | 커널/wow64 `[공백]` |
| TEB32 `+0x18 NtTib.Self` | `_LdrpInitializeInternal +0xb`, `__LdrpInitialize +0x12`, `_LdrpInitializeProcess +0xbd`, `+0x409` (`MOV EAX,FS:[0x18]`) | **FS 기준 = TEB32 주소**, 그 `+0x18` 이 자기 자신이어야 `+0x41b CMP EAX,[EAX+0x18]` 가 TEB32 로 판정 | 커널 `MmCreateTeb` 19줄 (sast/135) |
| TEB32 `+0x30 ProcessEnvironmentBlock` | `_LdrpInitialize +0x14` (`FS:[0x30]`), `__LdrpInitialize +0x45`, `_LdrpInitializeProcess +0xc9` | 0 이면 바로 널 참조 (`+0xcc`, 이어서 `[ESI+0xc]` 쓰기 `+0x128`) | 커널 (PEB32 주소, `MmCreateTeb`) |
| TEB32 `+0xfca SameTebFlags` (워드) | `_LdrpInitializeInternal +0x1f` (비트 14 `SkipLoaderInit` `0x4000`) · `__LdrpInitialize +0x38` (비트 13 `LoaderWorker` `0x2000`), `+0x238` (비트 6 `ClonedThread` `0x40`), `+0x288` (비트 13), 쓰기 `+0x65` (비트 5 `RanProcessInit`) · `_LdrpInitializeProcess +0x13dc` (비트 10 `InitialThread` `0x400`, 관리형 exe 일 때만) | 새 프로세스 첫 스레드는 0 이 정상 (모든 갈림이 "일반 길"). `InitialThread` 를 커널이 켜는지는 `[공백]` | 커널 `PspAllocateThread +0x7db` (sast/135) |
| TEB32 `+0xfdc WowTebOffset` (long) | **`_LdrpInitializeProcess +0x40f`** `MOV ECX,[EAX+0xfdc]` → `+0x415 TEST ECX,ECX; JNS +0x41b` → 음수면 `+0x419 ADD EAX,ECX` | 음수 (`−0x2000`) 면 `EAX` = **TEB64** (`TEB32 − 0x2000`). 0 또는 양수면 `EAX` 는 TEB32 그대로 | 커널 `MmCreateTeb +0x1d2` (`NEG EAX`, sast/135 표 18번) |
| TEB32 `+0xe38 TlsSlots[10]` (= `+0xe10 TlsSlots` + 40) | `+0x41e JNZ`(아님) → `+0x420 MOV EAX,[EAX+0xe38]` — **`EAX` 가 TEB32 일 때** (= `WowTebOffset >= 0`) | 그 포인터가 0 이면 `+0x42e MOV EAX,[EAX]` 가 널 참조. 일반 WoW 길은 위 줄이 아니라 아래 TEB64 갈래 | `[공백]` (wow64 · 커널 어디서도 안 씀: wow64 는 TEB64 쪽에만 씀 — sast/136) |
| **TEB64 `+0x14d0 TlsSlots[10]`** (`+0x1480 TlsSlots` + 80) | `+0x41b CMP EAX,[EAX+0x18]` (TEB64 `+0x18` 은 `SubSystemTib` 라 자기와 다름 → `+0x41e JNZ`) → **`+0x428 MOV EAX,[EAX+0x14d0]`** | = `Wow64Info` 주소. 0 이면 `+0x42e` 에서 널 참조 | **wow64 `Wow64LdrpInitialize +0x8f`** (sast/136 — 에뮬레이터가 안 채워도 됨, 진짜 wow64 가 돌면) |
| **`Wow64Info +0x0`** (PEB32 바로 뒤 `PEB32 + 0x490`, 4바이트) | `+0x42e MOV EAX,[EAX]` → `+0x430 [_NativePageSize] = EAX`, `+0x435 [_NativePageShift] = 0` → `+0x43d ~ +0x448` 로 비트 수 세어 `_NativePageShift` | **0 이면 `+0x441 SHR EAX,1; +0x443 INC ECX; +0x444 TEST DL,AL; +0x446 JZ +0x441` 루프가 끝나지 않는다** (`EAX` 가 0 이라 `TEST` 가 계속 0; `DL` 을 상수 1 로 읽음) `[정적 읽기]` — 값은 2 의 거듭제곱 (`0x1000`) 이어야 함 | wow64 `ProcessInit +0x54` (`0x1000`, sast/136) |
| `FS:[0x30]` 한 번 더 | `_LdrpInitializeProcess +0x6f6`, `+0x1612`, `+0x162f` (PEB32 `+0x1d8` 읽기) | — | — |

### PEB32 (`_PEB`, sizeof `0x490` — 64비트 PDB `_PEB32` 와 같음)

| 칸 | 읽는 줄 | 0 이면 · 값 쓰임 | 커널이 쓰나 (sast/135) |
|---|---|---|---|
| `+0x0 InheritedAddressSpace` (byte) | `__LdrpInitialize +0x1c8 CMP byte [EDI],0` (**첫 스레드가 아닐 때** 갈래), 쓰기 `+0x215` | 새 프로세스는 0 — 안 탐 | 예 (템플릿 `+0x167`) |
| `+0x2 BeingDebugged` | `_LdrpInitializeProcess +0x172f CMP byte [EDI+2],0` → `+0x1735 _LdrpDoDebuggerBreak`; `__LdrpInitialize +0x217` | 0 이면 중단점 안 걸림 | 디버거 붙을 때 `[공백]` |
| `+0x3 BitField` (byte) | `+0x9d0 TEST [EDI+3],2` (`IsProtectedProcess`), `+0x1041 TEST [EDI+3],0x10` (`IsPackagedProcess`), `+0x1072 TEST [EDI+3],4` (`IsImageDynamicallyRelocated`) | 0 이면 일반 길 (`\KnownDlls32` 도 엶) | 예 (`+0x16c`) |
| `+0x8 ImageBaseAddress` | `+0x22c PUSH [ESI+8]` → `+0x233 RtlImageNtHeaderEx` (ret `+0x238`), 앞으로 `+0x53a`, `+0x59a`, `+0x1090`, `+0x10be` | 0 이면 NT 헤더를 못 찾고 `+0x1099 CMP [EAX+0x34]…` 에서 널 참조 `[추측]` | 예 (`+0x178`, 32비트로 자른 `EPROCESS.SectionBaseAddress`) |
| `+0xc Ldr` | **쓰기** `+0x128 [ESI+0xc] = &PebLdr` (읽지 않음) | — | 안 씀 (ntdll 이 만듦) |
| `+0x10 ProcessParameters` | `+0x177`, `+0x58c PUSH [ESI+0x10]` (`RtlNormalizeProcessParams`), `+0x8ec`, `+0xc4a`, `+0xd44`; `_LdrpInitialize@8 +0x1a` | **0 이면 즉시 널 참조** (`_LdrpInitialize +0x1d CMP [EAX+8]`) — 필수 | 예 (`PspWow64ThunkProcessParameters`, sast/135 Q5) |
| `+0x18 ProcessHeap` | 읽기 `+0xcb2 MOV EAX,FS:[0x30]; PUSH [EAX+0x18]` (힙 만든 **뒤**), 쓰기 `_LdrpInitializeProcessHeap` (`+0x779 LEA ESI,[EDI+0x18]` 로 칸 주소를 넘김) | 시작은 0 | 안 씀 |
| `+0x1c FastPebLock` | 쓰기 `+0x726 MOV [ESI+0x1c],EDI` (EDI = `0x4b3b7240`, 바로 앞 `+0x719 RtlInitializeCriticalSectionEx` 로 만든 임계 구역) | — | 안 씀 |
| `+0x28 CrossProcessFlags` | `__LdrpInitialize +0x81 BTS.LOCK [EDI+0x28],1` (`ProcessInitializing` 켜기), `+0x196 BTR` (끄기) | 읽고 고침 (0 에서 시작) | `[공백]` |
| `+0x64 NumberOfProcessors` | `+0x13f CMP [ESI+0x64],1` | 0 또는 1 이면 SRW 락·조건변수·`WaitOnAddress` 스핀 횟수 3곳이 0 으로 남음 (`+0x136 ~ +0x172`) | 예 (`+0xf2`) |
| `+0x68 NtGlobalFlag` | `+0x4f0 TEST [ESI+0x68],2`, `+0x5b8 AND`, `+0x5c3 OR` (LoadConfig 의 `GlobalFlagsClear/Set` 반영), `+0x5e7`, `+0x62d (0x1000)`, `+0x6d5`, `+0x7f2 (0x800000)` | 0 이면 모든 디버그 플래그 꺼짐 (일반 길) | 예 (`+0xfb`) |
| `+0x70 CriticalSectionTimeout` (8바이트) | `+0x57b [_RtlpTimeout] = [ESI+0x70]`, `+0x57e [+0x74]` | 0 이면 그대로 쓰고 `_RtlpTimeoutDisable` 판정 (`+0x5f3 ~ +0x60a`) | 예 (`+0x110`) |
| `+0x14c PostProcessInitRoutine` | `+0x1981 MOV ESI,[EDI+0x14c]` → 0 이 아니면 `+0x198d` CFG 검사 → `+0x1993 CALL ESI` (ret `+0x1995`) | 0 이면 안 부름 | 안 씀 (0) |
| `+0x1d8 AppCompatFlags` (8바이트 중 하위) | `+0x6f6`, `+0x1612 (0x400000)`, `+0x1635 (0x10000000)` | 0 이면 해당 보정 꺼짐 | 안 씀 (0) `[추측]` |
| `+0x1e8 pShimData` | 읽기 `+0x56f [EBP-0xc0]`, 쓰기 `+0x15ff [EDI+0x1ec] = 0` (`AppCompatInfo`) | 0 이면 shim 엔진 안 올림 (`+0x15f5 TEST EAX,EAX; JZ`) | **wow64 `ProcessInit +0x188`** 이 64비트 PEB `+0x2d8` 값을 복사 (sast/136) |
| `+0xa0 LoaderLock` | **쓰기** `__LdrpInitialize +0x6d MOV [EDI+0xa0],0x4b3b6354` (ntdll32 안 전역 주소 — `LdrpLoaderLock` 으로 보임 `[추측: 이름]`) | — | 안 씀 |

(이 함수 본체가 **안 읽는** 대표 칸: `+0x38 ApiSetMap` (뒤쪽 `LdrpSnap…` 쪽이 읽음, 이번 범위 밖), `+0x2c KernelCallbackTable` (`KiUserCallbackDispatcher` 가 읽음, 4절).)

### `RTL_USER_PROCESS_PARAMETERS` (PEB32 `+0x10` 이 가리키는 32비트 판, sizeof `0x2c8`)

| 칸 | 읽는 줄 | 0 이면 | 비고 |
|---|---|---|---|
| `+0x8 Flags` | `_LdrpInitialize +0x1d` (**부호 비트** → `_LdrpIsSecureProcess`), `_LdrpInitializeProcess +0x180 TEST [ECX+8],DL` (비트 0 = 정규화됨, 안 됐으면 `RtlNormalizeProcessParams` `+0x58f` 가 함), `+0xc4d`·`+0xd47 (0x20000000)` (`_LdrpIncludeAlternateForwarders` 갈래) | 0 이면 "정규화 안 됨, 일반 프로세스" | 커널 `PspWow64ThunkProcessParameters` |
| `+0x38 ImagePathName` (len · maxlen · buffer `+0x3c`) | `+0x17a MOV EAX,[ECX+0x3c]`, `+0x193 MOVZX [ECX+0x38]`, `+0x8f5/+0x8f8` | buffer 0 이면 앞쪽 `\??\` 떼기 비교가 안 맞고 exe 항목 이름이 비게 됨 `[추측]` | 같은 곳 |
| `+0x24 CurrentDirectory.DosPath` (len `+0x24`, buffer `+0x28`) | `+0xbaa`, `+0xbad` (보안 프로세스가 아닐 때) | 비었거나 첫 글자가 0 이면 **`RtlGetNtSystemRoot` 앞 3글자 (`C:\` 꼴) + 널 로 채움** (`+0xbca ~ +0xc36`) | 같은 곳 |
| `+0x48 Environment` · `+0x290 EnvironmentSize` · `+0x294 EnvironmentVersion` | `_RtlpInitEnvironmentBlock +0x1b ~ +0x76` (복사본을 만들어 바꿔 씀, `INC [ESI+0x294]`) | 0 이면 새 환경 블록을 만듦 (`RtlCreateEnvironmentEx`, `+0x61`) | 같은 곳 |
| `+0x2a0 LoaderThreads` | `+0x164a` (`AppCompatFlags & 0x10000000` 이면 씀), `+0x1650` 읽기 → `_LdrpEnableParallelLoading` | 0 이면 병렬 로딩 꺼짐 | 같은 곳 |
| `+0x2b4 · +0x2b8 · +0x2bc` (`DefaultThreadpoolCpuSetMasks` · `…Count` · `…ThreadMaximum`) | `+0x86e`, `+0x878`, `+0x889` (`_TpSetDefaultPoolCpuSets`, `_TppPoolpGlobalPoolMaxThreadsOverride`) | 0 이면 안 씀 | 같은 곳 |

### ntdll32 자기 안 `LdrSystemDllInitBlock` (export, `.mrdata` rva `0x13d2a0`, 파일 속 처음 값은 `Size = 0x128` 하나뿐)

자리는 **ntdll32 이미지 안 전역** (64비트 ntdll 의 것과 별개). 커널 `PspPrepareSystemDllInitBlock(1)` 이 새 프로세스 안 이 주소에 채운다 (sast/135). 블록 칸 이름은 run32 의 `[추측]` 이름 후보.

| 블록 칸 | 읽는 줄 (`_refs/LdrSystemDllInitBlock.tsv`) | 0 이면 | 읽는 이 |
|---|---|---|---|
| `+0x0` (Size, 파일 값 `0x128`) | `_LdrpLoadDelegatedNtdll +0x13e` (지연 ntdll 갈래만) | — | 커널이 `0x128` 인지 검사 |
| `+0x8` (SystemDllWowRelocation) | **`_LdrpInitializeNtdllDataTableEntry +0x96`** `MOV EAX,[+0x8]; SUB [ESI+0x80],EAX` (ntdll 항목 `OriginalBase` 에서 뺌) — `_LdrpInitializeProcess +0xe34` 가 부름 | 0 이면 뺄 게 없음 (`OriginalBase` = 실제 기준) | ntdll32 |
| `+0x98` (RngData) | `_LdrpInitializeProcess +0x39` (→ `_RtlInitializeProcessExceptionChain`), `_InitSecurityCookie +0x20`, `_LdrInitializePerThreadRng +0x11`, `_LdrpProcessMappedModule +0xf6`, `_RtlpInitRandomExVector +0x24`, `_AvrfMiniLoadDll +0x697` | 0 이면 난수가 덜 섞일 뿐 (XOR) | ntdll32 |
| `+0x9c` (Flags) | `_LdrpSnapModule +0x28e`, `LdrGetProcedureAddressForCaller +0x26d`, `LdrControlFlowGuardEnforced +0xd` (**`LdrpInitializeProcess` 본체는 안 읽음**, 호출된 쪽이 읽음) | 0 이면 CFG 관련 비트 꺼짐 | ntdll32 |
| `+0xa0 ~ +0xb7` (24 B, MitigationOptionsMap) | **`_LdrpInitializeProcess +0x34c · +0x39c · +0x3d4 · +0x73b`** (`MOVSD.REP` 로 24 B 를 지역에 복사한 뒤 `SHRD`/`AND` 로 몇 비트 쌍을 꺼내 `_RtlpProcessECVPolicy` · `_LdrpEnforceIntegrityContinuity` · `_LdrpAuditIntegrityContinuity` 를 정함; `+0x743 ~ +0x76e` 는 같은 복사로 힙 정보 설정 (`RtlSetHeapInformation`) 조건을 봄) + `_RtlpComputePath +0x54`, `LdrpGetImportDescriptorForSnap +0x51`, `_LdrpPayloadRestrictionMitigationsEnabled` | 0 이면 "정책 없음" (`+0x37d`/`+0x389`/`+0x391` 갈림에서 `_RtlpProcessECVPolicy` 가 0 또는 기본) | ntdll32 |
| `+0xb8 · +0xbc` (CfgBitMap 쪽) · `+0xb0` | `LdrControlFlowGuardEnforced +0x0 · +0x5`, `_LdrpSnapModule +0x281 · +0x286`, `RtlValidateUserCallTarget +0x6`, `LdrpValidateUserCallTarget +0x0`, `RtlInitializeNtUserPfn +0xb · +0x14 · +0xf6 · +0xfb` (**호출된 쪽**) | 0 이면 CFG 검사 꺼짐 | ntdll32 (뒤쪽) |
| `+0xd8 ~ +0xef` (24 B Audit map) · `+0xf0 · +0xf8` | `_LdrpPayloadRestrictionMitigationsEnabled` 여섯 곳, `_LdrpCfgProcessLoadConfig +0x284 · +0x28c`, `_LdrpCfgCheckRoutineCallback +0x17 · +0x23` | 0 이면 해당 없음 | ntdll32 (뒤쪽) |
| **안 읽는 칸**: `+0x10`, `+0x18 ~ +0x97` (32비트 진입점 표 16칸), `+0xc0`, `+0xc8 · +0xd0` | `_refs` 에 읽는 줄 0 | — | **`+0x18 ~ +0x97` 은 64비트 ntdll 쪽 복사본을 wow64.dll 이 읽을 뿐 ntdll32 는 안 읽는다** (sast/136 A3) |

### 시스템 상수 (KUSD `0x7ffe0000`, 32비트 코드가 같은 주소로 읽음)

| 칸 | 읽는 줄 | 비고 |
|---|---|---|
| `+0x4 TickCountMultiplier`, `+0x320/+0x324/+0x328 TickCount` | `_LdrInitializePerThreadRng +0x1c ~ +0x36` (`0x7ffe0320/0324/0328`, `0x7ffe0004`) | 난수 시드 (`_RtlInitializeTRng`, `+0xa9`) |
| `+0x2d5 MitigationPolicies` | `_LdrpInitializeProcess +0x2fe`, `+0x1534`, `+0x1570`, `+0x1587`, `+0x174c` (NX · SEHOP · `CurDirDevicesSkippedForDlls`) | 0 이면 정책 "기본" |
| `+0x330 Cookie` | `_LdrpInitializeProcess +0x13e5` (관리형 exe 일 때만) | — |

### 64비트판 (run18 · run32 · run114) 과 **다른 점만**

1. **WoW 전용 읽기**: `+0x409 ~ +0x430` (TEB32 `+0xfdc` → TEB64 → `TlsSlots[10]` → `Wow64Info[0]` → `_NativePageSize`). 64비트는 이 값을 `KUSD`/`NtQuerySystemInformation` 쪽에서 얻는다 `[추측]` (이번엔 안 봄).
2. **`UseWOW64` · `LdrpLoadWow64` · `g_LdrpWow64LdrpInitialize` 줄이 없다.** 32비트 `LdrpInitializeProcess` 는 끝까지 돈다: ntdll 항목 (`+0xe34 → +0xe39 _LdrpInitializeNtdllDataTableEntry`) → exe 항목 (`+0xfe4 → +0xfe9 _LdrpAllocateModuleEntry`, `+0x10c6 _LdrpInsertDataTableEntry`) → `_LdrpInitializeKernel32Functions` (`+0x14d6 → +0x14db`, 앞의 `+0x14bc ~ +0x14cb` 조건이 맞을 때 — 조건 값 `[ESI+0x5c]` 의 뜻은 안 따라감 `[공백]`) → 의존 DLL (`+0x169f → +0x16a4 _LdrpMapAndSnapDependency`) → `+0x1712 _LdrInitState = 2` → `_Kernel32ThreadInitThunkFunction` (`+0x17fc CALL ESI`, ret `+0x17fe`) → `_LdrpInitializeGraphRecurse` (`+0x1857 → +0x185c`) → TLS 콜백 (`+0x1913 → +0x1918 _LdrpCallTlsInitializers`) → `PostProcessInitRoutine` (`+0x1993 CALL ESI`, ret `+0x1995`) → 돌아옴 (`+0x19c7 JMP +0x4e6` → `EAX = 0` → 끝 `+0x7c9 RET`). 즉 **x86 exe 의 kernel32 올리기 · import 연결 · TLS 콜백은 이 32비트 코드가 한다** (64비트 `+0x17fe` 이후 그림은 이 안에서 일어남).
3. **`\KnownDlls32`**: `_LdrpInitializeProcess +0xa22 NtOpenDirectoryObject` 의 이름 = 파일 안 상수 (`0x4b287a00`) `\KnownDlls32` (바이트 확인, 길이 `0x18`) — 64비트는 `\KnownDlls`. 이어서 `+0xaa6 NtOpenSymbolicLinkObject` (`KnownDllPath`), `+0xb03 NtQuerySymbolicLinkObject`, `+0xb6f NtClose`.
4. **환경 변수 WoW 변환 + 네이티브 정보**: `+0x8d2 → +0x8d7 _RtlpInitEnvironmentBlock` → `+0x85 → +0x8a _RtlpWow64ThunkEnvironmentTo32` (표 `_Wow64EnvironmentThunkTable` rva `0x1c9c`, 3줄 × 0x14: `ProgramFiles` ← `ProgramFiles(x86)` · `ProgramW6432`, `CommonProgramFiles` ← `CommonProgramFiles(x86)` · `CommonProgramW6432`, `PROCESSOR_ARCHITECTURE` ← `"x86"` · `PROCESSOR_ARCHITEW6432`) + `+0x8f → +0x94 _RtlpWow64GetNativeSystemInformation` (**`NtWow64GetNativeSystemInformation(클래스 1, 버퍼, 0xc, 0)`**, 스텁 `EAX = 0x1f5`) 로 프로세서 구조 (워드 `0xc` 이면 한 번 더 `_RtlpSetEnvironmentVariable`, `+0x98 ~ +0x9f`).
5. **자기 안 `LdrSystemDllInitBlock`**: 64비트는 PEB 쪽으로 읽는 칸을 같은 이름의 변수에서 읽는데 그건 64비트 ntdll 의 것. 32비트 쪽은 별개 변수 (위 표) 이고 `+0x18 ~ +0x97` 을 안 읽는다.
6. **첫 syscall 이 이상한 스텁**: `+0x5e NtQueryInformationProcess(-1, 0x24 …)` 가 `fs:[0xc0]` 갈림이 있는 스텁 (3절).

### 이 함수가 앞쪽에서 부르는 syscall (스텁 이름만 — 에뮬레이터 syscall 표면 AR07 용)

`NtQueryInformationProcess` (클래스 `0x24` 쿠키, `+0x5e` · `+0x97`; 뒤쪽 `+0x1513` · `NtSetInformationProcess` `+0x15da` · `+0x17dd` 도 있음) · `NtQuerySystemInformation` (클래스 0: `_LdrInitializeMrdata +0x22`; 클래스 `0xc5`: `+0x831`) · `NtCreateEvent` · `NtWow64GetNativeSystemInformation` (`+0x8f` 로 호출) · `NtOpenDirectoryObject` · `NtOpenSymbolicLinkObject` · `NtQuerySymbolicLinkObject` · `NtClose` · (`_LdrpInitializeExecutionOptions` 안 레지스트리 — 64비트와 같음). 모두 32비트 ntdll 의 `Nt*` 스텁 → wow64 `whNt*` 를 거친다.

## (3) B3 — `Nt*` 스텁 바이트 표

`Nt*` 스텁 수 (`ms/bin/ntdll32.dll` 내보내기 `Nt*`/`Zw*` 515개 주소 중 syscall 스텁): **509개 = nt 서비스 표 칸 0 ~ 508 하나씩** (wow64 nt 표 509칸과 같음, sast/136 A4). 508개는 `mov eax,N; mov edx,thunk; call edx; ret n/ret`, 나머지 하나 (`NtQueryInformationProcess`) 만 꼴이 다름. 나머지 6개 (`NtGetTickCount` · `NtCurrentTeb` · `NtdllDefWindowProc_A/W` · `NtdllDialogWndProc_A/W`) 는 syscall 스텁이 아님.

| 스텁 (rva) | 바이트 그대로 | 풀이 | `EAX` 쪼개기 | wow64 표 대조 (`sdwhnt32` 칸) |
|---|---|---|---|---|
| `NtContinue` (`0x79fd0`, `ms/disassemble/ntdll32/NtContinue.asm` +0x0 ~ +0xc) | `b8 43 00 00 00 ba 70 5b 33 4b ff d2 c2 08 00 90` | `mov eax,0x43; mov edx,0x4b335b70; call edx; ret 8` | 칸 `0x43` (표 0), 터보 0 = `ServiceNoTurbo` | 칸 `0x43` = `whNtContinue`, 인자 바이트 8 ✓ |
| `NtAllocateVirtualMemory` (`0x79d00`) | `b8 18 00 00 00 ba 70 5b 33 4b ff d2 c2 18 00 90` | `… ; ret 0x18` | 칸 `0x18`, 터보 0 | 칸 `0x18` = `whNtAllocateVirtualMemory`, 인자 `0x18` ✓ |
| `NtTerminateProcess` (`0x79e60`) | `b8 2c 00 07 00 ba 70 5b 33 4b ff d2 c2 08 00 90` | `mov eax,0x7002c; … ; ret 8` | 칸 `0x2c`, **터보 7** (`EAX >> 16`) | 칸 `0x2c` = `whNtTerminateProcess`, 인자 8 ✓ |
| `NtContinueEx` (`0x7a5f0`) | (같은 꼴) `EAX = 0xa5`, `ret 8` | | 칸 `0xa5`, 터보 0 | `whNtContinueEx`, 8 ✓ |
| `NtCallbackReturn` (`0x79bd0`) | `EAX = 5`, `ret 0xc` | | 칸 5 | `whNtCallbackReturn`, `0xc` ✓ |
| `NtQueryInformationProcess` (`0x79d10`, **37 바이트, 꼴이 다름**) | `b8 19 00 00 00 e8 00 00 00 00 5a 80 7a 14 4b 75 0e 64 ff 15 c0 00 00 00 c2 14 00 00 00 28 4b ba 70 5b 33 4b ff d2 c2 14 00` | `mov eax,0x19; call $+5; pop edx; cmp byte [edx+0x14],0x4b; jne L; call dword fs:[0xc0]; ret 0x14; (…) L: mov edx,0x4b335b70; call edx; ret 0x14` | 칸 `0x19`, 터보 0 | `whNtQueryInformationProcess`, `0x14` ✓ |
| `NtWow64GetNativeSystemInformation` (`0x7baf0`) | `EAX = 0x1f5`, `ret 0x10` | | 칸 `0x1f5` | `whNtWow64GetNativeSystemInformation`, `0x10` ✓ |
| **`_Wow64SystemServiceCall@0`** (`0xb5b70`, `ms/disassemble/ntdll32/_Wow64SystemServiceCall@0.asm` +0x0) | `ff 25 14 b0 3b 4b` | **`JMP DWORD PTR [0x4b3bb014]`** (= 파일 안 기준 `0x4b280000` + `0x13b014` = **`Wow64Transition`**) | — | — |

- **부르는 곳**: **거의 모든 스텁은 `Wow64Transition` 칸을 `_Wow64SystemServiceCall` 의 `JMP [mem]` 로 간접으로 쓴다** (`_refs/Wow64Transition.tsv`: 읽는 줄은 `_Wow64SystemServiceCall@0 +0x0` 하나뿐, 나머지 둘은 export 표 `DATA` · 외부 참조). `ntdll32 .text` 전체 (`n32_text.s`, 468,948 줄) 를 훑어 `fs:0xc0` 를 쓰는 명령을 찾으면 **`NtQueryInformationProcess +0x11` (`call DWORD PTR fs:0xc0`, rva `0x79d21`) 단 하나**. 즉 **`fs:[0xc0]` 쪽은 그 한 스텁만**, 나머지 508개는 `Wow64Transition` 쪽.
  - `NtQueryInformationProcess` 갈림: `+0xb 80 7a 14 4b` = `CMP byte [EDX+0x14], 0x4b` 에서 `EDX` = `+0xa` 뒤의 `pop edx` 로 얻은 **자기 코드 주소 (rva `0x79d1a` 의 실제 주소)**. `+0x14` 칸 = rva `0x79d2e` = 같은 함수 안에 박힌 **재배치되는 dword `0x4b280000` (= 선호 ImageBase) 의 윗바이트** (`PE 재배치표`: rva `0x79d2b` HIGHLOW). 그러니 **ntdll32 가 윗바이트 `0x4b` 인 주소 (= 선호 기준 `0x4b280000` 근처) 에 올라오면 `call fs:[0xc0]`, 다른 기준 (보통 ASLR 로 `0x7…`) 이면 `call edx` (= `Wow64Transition` 쪽)** — 두 길의 값은 같다 (둘 다 `BTCpuGetBopCode()` 결과, sast/136 A2: `Wow64Transition` 은 wow64 가 `CpupResolveReverseImports +0xb2` 에서 쓰고, TEB32 `+0xc0` 은 `CpuThreadInit +0x32`). 의도는 모름 `[추측: 쿠키 조회가 로더 초기화 중이라 …]`. **에뮬레이터가 ntdll32 를 선호 기준에 올리면 이 한 스텁만 `fs:[0xc0]` 를 쓴다** → TEB32 `+0xc0` 도 채워져야 함 (진짜 wow64 `ThreadInit` 이 채움).
- **`EAX` 에 싣는 값**: `mov eax, imm32`. **비트 0~11 = 서비스 표 칸**, **비트 12~13 = 표 번호**, **비트 16 이상 = wow64cpu 터보 번호** (sast/136 A4 와 같음).
  - 508개 스텁의 `EAX` 를 전부 풀면: 표 번호 (비트 12~13) 는 **전부 0** (nt 표 — ntdll32 는 win32k·console·base 표를 안 씀, 그쪽은 `win32u`/`kernel32` 가 따로), 칸 번호 0 ~ 508 이 겹침 없이 508개 (`NtQueryInformationProcess` 의 `0x19` 를 더해 509 = 표 칸 수), 터보 번호는 0 ~ 30 (**32칸 터보 표 안**; 0 이 409개 = `ServiceNoTurbo`, 3 · 4 · 5 · 6 · 7 · 8 · 10 · 11 · 12 · 13 · 14 · 17 · 19 · 20 · 21 · 24 · 26 · 27 · 28 · 29 · 30 이 나머지, 예: `NtClose` = 3, `NtTerminateProcess` = 7, `NtReadFile`/`NtWriteFile` = 26, `NtDeviceIoControlFile` = 27, `NtQuerySystemTime` = 24, `NtWaitForMultipleObjects` = 29 — 이름은 sast/136 A2 의 "터보 썽크" 와 같은 이름들).
- **인자 위치**: **`EDX` 는 인자 포인터가 아니다** — 스텁이 `EDX` 에 넣는 건 `_Wow64SystemServiceCall` 주소뿐 (진짜 네이티브 `KiFastSystemCall` 의 `mov edx,esp` 와 다름). 인자는 **32비트 스택** 에 있다 (`Nt*` 가 불릴 때 `[Esp]` = 부른 쪽 반환, `[Esp+4]` ~ = 인자). `call edx` 가 `[Esp]` 에 스텁 안 반환 주소 (`+0xc`) 를 하나 더 얹고, wow64cpu `CpupReturnFromSimulatedCode +0x0 ~ +0x12` 가 `r8d = [esp]` (= 스텁 `+0xc` → 저장되는 `Eip`), `esp += 4`, **`r11 = esp + 4`** (= `R11 = 원래 Esp + 8` = 첫 인자 `[Esp+8]`) 를 잡는다 (`_objdump_text_0x1000.s` `1a38 ~ 1a4a`). 그 `R11` 이 `Wow64SystemServiceEx` 의 `RDX` (sast/136 A4).
- **돌아온 뒤**: 64비트 쪽 처리가 끝나면 wow64cpu 가 `[r13+0x34]` 에 `EAX` 를 저장하고 (`ServiceNoTurbo +0xb`) `RunSimulatedCode` 머리로 돌아가 32비트로 복귀 → `Eip` = 스텁 `+0xc` 의 **`RET n`** (표의 `ret 8` / `ret 0x18` / …). `n` = wow64 nt 표 인자 바이트 (위 표 ✓).
- `Wow64Transition` export (rva `0x13b014`, 구역 `.mrdata`, `0xc0000040` = 읽기·쓰기): **파일 안 처음 값 = 0** (4바이트 `00 00 00 00`). 로더가 `LdrProtectMrdata` 로 쓰기 가능하게 하는 구역이고 wow64 가 `NtProtectVirtualMemory(4바이트, PAGE_READWRITE)` 로 쓴다 (sast/136 A2). 값 = `BTCpuGetBopCode()` (wow64cpu `KiFastSystemCall` rva `0x7000` 또는 `KiFastSystemCall2` rva `0x3010` 의 실제 주소).
- ntdll32 안에 네이티브 스텁도 있음: `KiFastSystemCall` (`0x7c010`: `mov edx,esp; sysenter`), `KiIntSystemCall` (`0x7c030`: `lea edx,[esp+8]; int 0x2e`). **`Nt*` 스텁은 이걸 안 부른다** (스텁이 부르는 건 `_Wow64SystemServiceCall` 뿐 — 스텁 509개 바이트로 확인 `[정적]`). 다른 코드가 `KiFastSystemCall` 을 부르는지는 안 봄 `[공백]`.

## (4) B3 · B4 — 디스패처 입구 표

| 함수 (rva · 크기) | 스택에서 받는 것 | 첫 줄 · 하는 일 | 끝 | 근거 |
|---|---|---|---|---|
| `KiUserExceptionDispatcher` (`0x7bf70`, 77 B) | `[Esp]` = `EXCEPTION_RECORD*` (`+0x5 MOV EBX,[ESP]`), `[Esp+4]` = `CONTEXT*` (`+0x1 MOV ECX,[ESP+4]`) | `+0x0 CLD`; 둘을 `PUSH ECX; PUSH EBX` 로 되밀어 **`RtlDispatchException(기록, 문맥)`** (`+0xa → +0xf`) | `AL != 0` (처리됨) → `+0x13 POP EBX; POP ECX` → **`NtContinue(문맥, 0)`** (`+0x18 → +0x1d`, `+0x15 PUSH 0`); `AL == 0` → **`NtRaiseException(기록, 문맥, 0)`** (`+0x25 → +0x2a`); 그 둘이 돌아오면 (실패) 지역에 `{상태 = EAX, 플래그 1, …}` 기록을 만들어 `RtlRaiseException` (`+0x45 → +0x4a`), `RET 8` (`+0x4a`) | `KiUserExceptionDispatcher.asm` |
| `KiUserApcDispatcher` (`0x7be40`, 82 B) | `[Esp+0x0]` = APC 루틴 (`+0x29 POP EAX` → `ECX`), `[Esp+4 ~ +0xc]` = 루틴에 그대로 넘길 인자 셋 (루틴이 `CALL ECX` 로 불릴 때 스택에 남음), `[Esp+0x10]` 비트 0 = 알림 플래그 (`+0x22 MOV ESI,[ESP+0x10]; +0x26 AND ESI,1`), **`CONTEXT` 가 `Esp+0x14`** (`+0x1e LEA EDI,[ESP+0x14]`), `Esp+0x310` 에 SEH 틀 자리 (`+0x0 LEA EAX,[ESP+0x310]`; `[EAX] = FS:[0]`, `[EAX+4] = 0x4b2fbe20`, `FS:[0] = EAX`) | `+0x2c CALL [guard_check_icall]` → `+0x32 CALL ECX` (APC 루틴) | `+0x34 MOV ECX,[EDI+0x2fc]; FS:[0] = ECX` (SEH 되돌림 — `EDI+0x2fc` = `Esp+0x310` ✓) → **`NtContinue(EDI = 문맥, ESI = 플래그)`** (`+0x43 → +0x48`) → 돌아오면 `RtlRaiseStatus` (`+0x4b → +0x50`) 반복 | `KiUserApcDispatcher.asm`. 누가 이 틀을 쌓나 = wow64 `Wow64ApcRoutine` (64비트 `KiUserApcDispatcher +0x6e` 가 부름, sast/136) `[추측]` — 이번엔 안 따라감 |
| `KiUserCallbackDispatcher` (`0x7bf20`, 74 B) | `[Esp+0]` 는 건너뜀 (`+0x1e ADD ESP,4`), **`[Esp+4]` = 번호** (`+0x21 POP EDX`), 그 뒤 스택 = 콜백이 받을 인자 (`+0x10/+0x14` 에 SEH 틀을 얹음: `[ESP+0x10] = FS:[0]`, `[ESP+0x14] = 0x4b2fbea0`) | `+0x22 EAX = FS:[0x30]` (PEB32) → **`+0x28 EAX = [EAX+0x2c]` (PEB32 `KernelCallbackTable`)** → `+0x2b ECX = [EAX + EDX*4]` → `+0x2e` CFG 검사 → `+0x34 CALL ECX` | `+0x3b → +0x40` **`NtCallbackReturn(0, 0, EAX)`** (`+0x37 PUSH 0; +0x39 PUSH 0; +0x36 PUSH EAX`), 돌아오면 `RtlRaiseStatus` (`+0x43 → +0x48`) 반복 | `KiUserCallbackDispatcher.asm` |
| `RtlUserThreadStart` (`0x7c000`, 13 B) | **`EAX` = 시작 주소, `EBX` = 인자** (커널이 첫 32비트 문맥 `Eax` ← 64비트 `Rcx`, `Ebx` ← `Rdx` 로 채움 — sast/135 Q7). `[Esp]` = 반환 칸 | **`+0x0 MOV [ESP+4],EAX; +0x4 MOV [ESP+8],EBX; +0x8 JMP __RtlUserThreadStart@8`** (rva `0x6e166`, 꼬리호출) | `_RtlUserThreadStart(시작, 인자)` 에서 이어짐 (이번 범위 밖) | `RtlUserThreadStart.asm` |

- **`NtContinue` (32비트) 가는 길** (엔진의 은퇴 자리가 `NtContinue` 인 64비트판과 비교):
  1. 32비트 `NtContinue` 스텁 (칸 `0x43`, `ret 8`) → `Wow64SystemServiceEx` → `whNtContinue` (wow64.dll rva `0x1d410`, 20 B: `+0x4 MOV DL,[RCX+4]` (TestAlert), `+0x7 MOV ECX,[RCX]` (32비트 문맥 주소), **`+0x9 → +0xe` `Wow64NtContinue`**).
  2. `Wow64NtContinue` (rva `0x1d42c`): **`+0x3f → +0x44` `CpuSetContext`** (32비트 문맥을 CPU 영역에 넣음; `CpuSetContext +0x2d` 가 `BtFuncs+0x88` = `BTCpuSetContext` 를 `(RCX = −2, RDX = −1, R8 = TEB, R9 = 문맥)` 로 부름) → **`BTCpuSetContext` (wow64cpu rva `0x1010`)**: `RtlWow64GetCpuAreaInfo` 로 CPU 영역을 얻고, 현재 스레드 (`+0x49 CMP RBX,-2`) 이고 **CPU 영역 비트 0 이 켜져 있으면** (`+0x54 TEST byte [RAX],1`) `RtlCopyContext` 로 바로 덮고 (`+0xa0`), **꺼져 있으면 `NtSetInformationThread(-2, 0x1d (= ThreadWow64Context), 문맥, 0x2cc)`** (`+0x6a → +0x71`). 32비트 코드가 syscall 중일 때는 비트 0 이 꺼져 있다 (`RunSimulatedCode +0x2f BTR` 가 32비트로 들어갈 때 지움, sast/136 A2) → **보통 `NtSetInformationThread(0x1d)` 쪽** `[정적 추론]`.
  3. 이어서 `Wow64NtContinue` 는 **TEB64 `+0x14b8`** (= `TlsSlots[7]`) 목록을 훑어 (`+0x97 MOV RBX,[RAX+0x14b8]`) 항목의 `+0x10` 이 이 32비트 문맥 포인터와 같은 틀이 있으면 (`+0xa7 CMP [RBX+0x10],R13`) 그 틀의 64비트 문맥 (`[RBX+0x8]`) 으로 **64비트 `NtContinueEx`** 를 부른다 (**`+0x1e6 → +0x1ed`**, 인자 = `{플래그 1, TestAlert ? 3 : 2, …}`, `+0x1bf ~ +0x1d8`). 목록이 비어 있으면 (`+0xa1 JZ → +0x205`) `NtContinueEx` 를 **안 부르고** TestAlert 가 켜졌으면 **64비트 `NtTestAlert`** (`+0x20a → +0x211`) 를 부른 뒤 `EAX = 32비트 문맥 [+0xb0] (= Eax)` 를 돌려준다 (`+0x216 MOV EAX,ESI`). → wow64cpu 가 `[r13+0x34]` 에 저장하고 32비트로 복귀 (3절).
  4. 32비트 `NtContinueEx` 스텁 (칸 `0xa5`) 은 따로 `whNtContinueEx` (rva `0x2ddb0`) → **64비트 `NtContinueEx` 를 `+0x59 → +0x60` 로 직접 부름** (`[0x18003b940]`, 인자를 32 → 64비트로 넓힘 `+0x13 ~ +0x4e`).
  - 정리: **32비트 `LdrInitializeThunk +0x24` 의 `NtContinue` 는 (정적 읽기로는) "문맥을 CPU 영역에 설치 + (필요하면) `NtSetInformationThread(0x1d)` + `NtTestAlert`" 이고, 64비트 `NtContinueEx` 로 가는 건 `+0x14b8` 목록에 틀이 있을 때뿐이다.** 시작 때 그 목록이 비어 있는지 (= 처음 스레드의 NtContinue 가 `NtContinueEx` 를 부르는지) 는 `[공백]` — wow64.dll `.text` 전체에서 `+0x14b8` 을 쓰는 줄이 `Wow64NtContinue` (읽기 `+0x97`, 쓰기 `+0xca`) 와 rva `0x20709 · 0x20723` (다른 함수, 읽기 · 쓰기) 뿐이고, 목록에 **넣는** 줄은 못 찾음 (wow64_text.s `grep '0x14b8'`) → 처음엔 0 일 것 `[추측]`.

## (5) B4 — `NtdllExportInformation[1]` 8칸 이름 · ntdll32 export rva

**커널 쪽 이름 표는 sast/135 "표 B-0" 을 보라** (`_workspace/sast/135_wow64-kernel_result.md`, `NtdllWowX86Exports` rva `0xc801a0`, 16바이트 × 8 = `{이름 문자열, 기록할 칸}`). 이번에 ntoskrnl 을 **Ghidra 로 열지 않고 `pefile` 로 PE 데이터만 다시 읽어** 같은 8줄이 나오는 것을 확인했다 (`NtdllExportInformation` rva `0xc7ff90` `[1]` = `{0x140c801a0, 8}`, `[0]` = `{0x140c80220, 19}`, `[2 ~ 5]` 비어 있음). 아래는 표 B-0 에 **ntdll32 export rva · 블록 칸** 을 덧붙인 것.

| 슬롯 `PsWowX86SharedInformation` (rva `0xfda900`) `+` | 블록 `LdrSystemDllInitBlock` `+` (= 슬롯 `+0x18`) | 커널 이름 표 글 (표 B-0) | **ntdll32 export rva (번호)** | 읽는 이 |
|---|---|---|---|---|
| `+0x00` | `+0x18` | `LdrInitializeThunk` | `0x34230` (151) | wow64 `ProcessInit` → `Ntdll32LoaderInitRoutine` → 32비트 `Eip` (sast/136) |
| `+0x08` | `+0x20` | `KiUserExceptionDispatcher` | `0x7bf70` (117) | wow64 `Ntdll32KiUserExceptionDispatcher` |
| `+0x10` | `+0x28` | `KiUserApcDispatcher` | `0x7be40` (115) | wow64 `Ntdll32KiUserApcDispatcher` |
| `+0x18` | `+0x30` | `KiUserCallbackDispatcher` | `0x7bf20` (116) | wow64 `Ntdll32KiUserCallbackDispatcher` |
| `+0x20` | `+0x38` | **`RtlUserThreadStart`** | `0x7c000` (1619) | 커널 `PspWow64InitThread +0x15d` (첫 32비트 `Eip`), wow64 `ThunkStartupContext64TO32 +0x74` (`Eip` 와 비교) |
| `+0x28` | `+0x40` | `RtlpQueryProcessDebugInformationRemote` | `0xd1470` (1692) | 쓰는 곳 안 봄 (wow64 도 안 읽음, sast/136) |
| `+0x30` | `+0x48` | (이름 표에 없음 — **ntdll32 기본 주소**, 부팅 때 `PspInitializeSystemDlls +0x77 PspWow64GetSharedInformation(1)` → `+0x80 MOV [RAX+0x30],RCX`) | — | wow64 `NtDll32Base` |
| `+0x38` | `+0x50` | **`LdrSystemDllInitBlock`** | `0x13d2a0` (191) | 커널 `PspPrepareSystemDllInitBlock +0x2e2` (32비트 블록 주소) |
| `+0x40` | `+0x58` | `RtlpFreezeTimeBias` | `0x139798` (1669) | 안 봄 |

- 8개 이름 **전부 ntdll32 export 에 있음** (이름으로 찾음, 전달 export 아님).
- **`[4]` (이름 표의 5번째 줄, 슬롯 `+0x20`, 블록 `+0x38`) = 32비트 `RtlUserThreadStart`** 확정 `[정적]` (`[추측]` 이었던 sast/136 줄 해소).
- **sast/136 "다음 요청 후보 1 ④" 의 번호표 바로잡기**: 거기엔 "`[6]` 기준 주소 (`+0x48`) · `[7]` (`+0x50`, 미사용)" 로 적혔는데, 이름 8개는 슬롯 `+0x00 ~ +0x28`, `+0x38`, `+0x40` 에 있고 (`+0x30` 은 이름 없는 기준 주소) 블록으로는 `+0x18 ~ +0x40`, **`+0x50` (= `LdrSystemDllInitBlock`)**, `+0x58` (= `RtlpFreezeTimeBias`) 다. wow64 가 읽는 `+0x18 · +0x20 · +0x28 · +0x30 · +0x38 · +0x48` 은 그대로 맞다.

---

## 지금 우리 (PE_emulator-AR) 맞나·틀리나 — 원문 "답이 오면 쓰는 곳"

- **AR03 (PEB32 · 32비트 블록 · 매개변수)** — 커널/엔진이 **지어 둘 것** (ntdll32 가 읽음):
  - **PEB32** (`sizeof 0x490` + **바로 뒤 `Wow64Info`**): `+0x0 · +0x3 · +0x8 · +0x10 · +0x64 · +0x68 · +0x70` (표의 "커널이 쓰나 = 예" 줄, `MiInitializeWowPeb`), `ProcessParameters` (`+0x10`) 는 **32비트 판으로 새로 지은** `RTL_USER_PROCESS_PARAMETERS` (`PspWow64ThunkProcessParameters`) — **0 이면 `_LdrpInitialize +0x1d` 에서 바로 널 참조**. `+0xc Ldr` · `+0x18 ProcessHeap` · `+0x1c` · `+0xa0` · `+0x28` 은 ntdll32 가 직접 씀 (비워 둠). `+0x1e8 pShimData` 는 wow64 가 PEB64 에서 복사.
  - **32비트 `LdrSystemDllInitBlock`** = **ntdll32 이미지 안** (`.mrdata`, ntdll32 기준 + `0x13d2a0`, 슬롯 `+0x38` 이 그 주소). 에뮬레이터가 ntdll32 를 그대로 올리면 `Size = 0x128` 은 이미 들어 있다. 새로 쓸 칸: `+0x8` (SystemDllWowRelocation), `+0x18 ~ +0x58` (이름 8 + 기준 주소 칸, 위 5절 표 — 이 칸은 **64비트 블록에도 같이 복사**), `+0x98` (난수), `+0x9c` (플래그), `+0xa0 ~ +0xb7` / `+0xd8 ~ +0xef` (완화 옵션 24 B 둘), `+0xb8 · +0xc0` (CFG 비트맵 · 크기 — sast/135 는 32비트 블록 값의 출처를 `[프로세스 +0x410 구조] +0x4c0 · +0x4c8` 로 적음, 이번에 재확인 안 함), `+0xf0 ~ +0x127` (CFG 함수 7칸). **ntdll32 가 안 읽는 칸** (`+0x10`, `+0x18 ~ +0x97`, `+0xc8 · +0xd0`) 은 틀려도 ntdll32 쪽엔 영향이 없다 (`+0x18 ~ +0x97` 은 wow64 가 64비트 쪽 복사본에서 읽음).
  - **`Wow64Info` 4칸 이상 (`+0x0` = `0x1000` 은 wow64 `ProcessInit +0x54` 가 씀)**: ntdll32 `+0x42e` 가 읽는다. 진짜 wow64 가 돌면 자동으로 맞지만, 엔진이 `LdrpInitializeProcess +0x17fe` 이후 wow64 를 **돌리지 않고** 흉내 낸다면 **TEB64 `+0x14d0` → `Wow64Info[0] = 0x1000`** 을 직접 지어야 한다.
  - 틀릴 수 있는 것: PEB32 크기를 `0x490` 으로만 잡으면 `Wow64Info` 가 모자람 (sast/136 AR04 와 같은 말). `TEB32 +0xfdc` 를 양수/0 으로 두면 ntdll32 는 TEB32 `+0xe38` 쪽을 읽는다 (그 칸은 아무도 안 씀 → 널 참조) — **음수 `−0x2000`** 이어야 한다.
- **AR04 (TEB32 · 32비트 스택 · CPU 영역)** (덧붙임): ntdll32 가 TEB32 에서 읽는 칸 = `+0x0` (SEH 체인), `+0x18` (Self = FS 기준), `+0x30` (PEB32), `+0xfca` (SameTebFlags — 0), `+0xfdc` (**`−0x2000`**, 음수여야 함). TEB64 `+0x14d0 TlsSlots[10]` 은 wow64 가 씀. **32비트 코드가 TEB64 (= TEB32 − 0x2000) 를 직접 읽으므로 TEB64 도 4GB 아래 같은 영역에 있어야** 한다 (sast/135 "서브 할당 영역" 설명과 맞음).
- **AR06 (은퇴 자리)**: **32비트 `LdrpInitializeProcess` 는 돌아온다** (64비트 `+0x17fe` 와 달리) — 끝에서 `LdrInitializeThunk +0x24 → NtContinue(문맥, 1)` 를 부른다. 이 `NtContinue` 는 **에뮬레이터가 보는 syscall 이 아니라** wow64 안에서 `whNtContinue → Wow64NtContinue → CpuSetContext` (+ 보통 64비트 `NtSetInformationThread(-2, 0x1d)`) 와 64비트 `NtTestAlert` 로 간다. 그 뒤 **`RunSimulatedCode` 로 다시 32비트로 들어갈 때** 첫 스레드 문맥 (`Eip = RtlUserThreadStart`) 이 쓰인다. 그러니 "32비트 ntdll 초기화 끝" 은 64비트 `NtContinue` 가 아니라 이 `NtTestAlert` · `NtSetInformationThread(0x1d)` 와 CPU 영역 재진입으로 잡아야 한다 `[정적 추론]`.
- **AR07 이후 (syscall 표면 · 벽)**: ntdll32 초기화 중 64비트 쪽이 받아야 할 syscall 은 위 "앞쪽에서 부르는 syscall" 표. 특히 **`NtWow64GetNativeSystemInformation` (칸 `0x1f5`)** 은 32비트 전용 번호라 `whNtWow64GetNativeSystemInformation` (wow64.dll rva `0xc3d0`) 을 거쳐 네이티브 정보 syscall 로 간다 (환경 변수 WoW 변환 때 `PROCESSOR_ARCHITECTURE` 판단). `\KnownDlls32` 디렉터리 열기 (`NtOpenDirectoryObject`) · `KnownDllPath` 심볼릭 링크 (`NtOpenSymbolicLinkObject`/`NtQuerySymbolicLinkObject`) 는 **실패하면 ntdll32 초기화가 죽는다** (`+0xa3e JGE` 아니면 `+0xa5d JMP +0x4b3497e4` → 에러 반환 → `_LdrpInitializationFailure`, 64비트와 같은 처리 — 정확한 상태 반환 규칙은 `[공백]`).

## 동적으로 확인할 것 (원문 D 와 합치면)

32비트 모듈이므로 멈춤점은 **ntdll32 로드 주소 + rva** 꼴. ntdll32 로드 주소 = `dq nt!PsWowX86SharedInformation+0x30 L1` (커널 안 값, sast/135 D 10) 또는 첫 32비트 `CONTEXT` 의 `Edx`... 32비트 레지스터 보려면 `.effmach x86` (첫 32비트 중단 후).

| # | 무엇 | 멈춤점 (ntdll32 기준 + rva) | 볼 것 |
|---|---|---|---|
| 1 | **32비트 `LdrInitializeThunk` 입구의 `esp`** | `ntdll32+0x34230` | `r esp`, `dd esp L3` → `[esp+4]` (문맥 사본 주소 — `esp+0x?` 근처), `[esp+8]` (= ntdll32 기준 주소와 같은지); `r cs, ss, ds, es, fs, gs` (`0x23 · 0x2b · 0x2b · 0x2b · 0x53 · 0x2b`); `dd poi(esp+4) L1` (ContextFlags); `dd fs:[0x18] L1` (= FS 기준 = TEB32), `dd fs:[0x30] L1` (PEB32), `dd fs:[0xfdc] L1` (**`0xffffe000`**), `dd fs:[0xfca] L1` |
| 2 | 사슬 진행 | `ntdll32+0x342a0` (`__LdrpInitialize`), `ntdll32+0xc9344` (`LdrpInitializeProcess`), `ntdll32+0x34241` (= `LdrInitializeThunk +0x11`, `LdrpInitialize` 돌아옴), `ntdll32+0x34259` (= `LdrInitializeThunk +0x29`, `NtContinue` 뒤 — **걸리면 안 됨**) | 첫 번째 스레드에서 `+0x34241` 에 오나 (= `LdrpInitializeProcess` 가 돌아오나) |
| 3 | **`Wow64Info` · TEB64 `TlsSlots[10]`** | `ntdll32+0xc9772` (= `LdrpInitializeProcess +0x42e`, `MOV EAX,[EAX]`) | `r eax` (= `Wow64Info` 주소), `dd @eax L1` (**`0x1000`**), `dq @$teb64+0x14d0 L1`; `ebx`/`ecx` 로 갈래 확인 |
| 4 | 32비트 `LdrSystemDllInitBlock` | (아무 때나, 32비트 모듈 안) `dd ntdll32+0x13d2a0 L4a` | `+0x0 = 0x128`, `+0x8`, `+0x18 ~ +0x58` (9칸: 이름 8 + 기준), `+0x98`, `+0xa0 …`, `+0xb8` |
| 5 | **스텁 → `Wow64Transition`** | `ntdll32+0xb5b70` (`_Wow64SystemServiceCall`), `ntdll32+0x79d21` (`NtQueryInformationProcess` 의 `call fs:[0xc0]` — 걸리나), `ntdll32+0x79d1f` (`jne` 방향) | `dd ntdll32+0x13b014 L1` (= wow64cpu `KiFastSystemCall(2)` 주소), `dd fs:[0xc0] L1` (같은 값), `r eax` (칸/터보), `r edx`; `dd esp L4` (`[esp]` = 스텁 `+0xc`, `[esp+4]` = 부른 쪽 반환, `[esp+8]` = 첫 인자) |
| 6 | 환경 변수 WoW 변환 | `ntdll32+0xc9c1b` (= `LdrpInitializeProcess +0x8d7`, `_RtlpInitEnvironmentBlock` 돌아옴), `ntdll32+0x7baf0` (`NtWow64GetNativeSystemInformation`) | `eax` (`0x1f5`), 환경 블록 (`ProgramFiles`) 이 바뀌었나 |
| 7 | 32비트 `NtContinue` | `ntdll32+0x79fd0` (스텁), `wow64!Wow64NtContinue+0x44` (`CpuSetContext` 뒤), `wow64!Wow64NtContinue+0x1ed` (`NtContinueEx` 뒤 — **걸리나**), `wow64!Wow64NtContinue+0x211` (`NtTestAlert` 뒤), `wow64cpu!BTCpuSetContext+0x71` (`NtSetInformationThread` 뒤 — 걸리나) | `dq @$teb+0x14b8 L1` (목록 머리 — **0 인지**), `dd poi(...)`; CPU 영역 앞 바이트 (비트 0) 값 (`dq @$teb+0x1488 L1`, `db <영역> L1`) |
| 8 | 디스패처 입구 (예외 · APC · 콜백이 일어날 때) | `ntdll32+0x7bf70`, `ntdll32+0x7be40`, `ntdll32+0x7bf20` | `dd esp L8`, APC 는 `dd esp L6` + `[esp+0x10]&1`; 예외는 `dd poi(esp)` (기록) · `dd poi(esp+4)` (문맥) |
| 9 | ntdll32 가 어디에 올라오나 | `lm` 의 ntdll32 시작 주소 | `0x4b280000` (선호) 인가 아니면 `0x7…` 인가 — `NtQueryInformationProcess` 스텁이 어느 길을 탈지 정함 |

## 다음 요청 후보

1. **wow64 쪽 "처음 NtContinue 이후"**: `BTCpuSetContext` 가 부르는 `NtSetInformationThread(0x1d)` 의 **커널 쪽** (`PspSetContextThreadInternal`/`NtSetInformationThread` WoW 갈래, CPU 영역 비트 0 을 켜는지) + TEB64 `+0x14b8` 목록에 넣는 곳 (`Wow64PrepareForException` · `Wow64ApcRoutine` · `Wow64KiUserCallbackDispatcher` 가 쌓는 틀) — 32비트 디스패처 입구 스택을 누가 만드는지와 같이.
2. **AR07 syscall 표면**: ntdll32 초기화가 부르는 `whNt*` 본문 (`whNtQuerySystemInformation` · `whNtWow64GetNativeSystemInformation` · `whNtQueryInformationProcess` · `whNtOpenDirectoryObject` · `whNtOpenSymbolicLinkObject` · `whNtQuerySymbolicLinkObject` · `whNtCreateEvent`) — 64비트 에뮬레이터 syscall 로 어떻게 내려가는지.
3. **32비트 `LdrpInitializeProcess` 뒤쪽**: 64비트판과 같은 꼴이라고 보지만 `_LdrpInitializeKernel32Functions` · `_LdrpMapAndSnapDependency` (kernel32 올리기, x86 exe 의 import) 는 `SysWOW64\kernel32.dll`·`\KnownDlls32` 와 얽힘 (AR07 이후).
4. **`__RtlUserThreadStart@8` → `BaseThreadInitThunk` (32비트)**: 첫 32비트 코드가 exe 진입점으로 가는 길 (flow.md 의 x86 판).
5. ntdll32 가 선호 기준에서 `NtQueryInformationProcess` 스텁이 `fs:[0xc0]` 를 쓰는 **까닭** (정적으로는 목적 모름).

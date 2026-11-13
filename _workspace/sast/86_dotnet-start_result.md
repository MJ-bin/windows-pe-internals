# 86 dotnet-start — 정적 분석 결과

- 요청서: `_workspace/sast/86_dotnet-start.md` (Q152, PE_emulator 요청 30). 첫 답은 "무엇이 필요한가" 지도.
- 모듈 / 바이너리:
  - ntdll (`ms/bin/.../System32/ntdll.dll`). 다시 analyze 안 함. `.gpr`·`_index.tsv`·`SetPdb:` 다 있음.
  - mscoree (`ms/bin/.../System32/mscoree.dll`, x64). **이번에 analyze 함** (03:08~03:09, 약 1분). PDB 붙음 (`SetPdb:` 줄 있음), 함수 1334개, `_index.tsv` 있음.
  - **mscoree PDB GUID+Age = `C5C034E4B3EDB32A23F05CE2A83C18431`** (`ms/sym/mscoree.dll.pdb`, 별칭 표 `mscoree.dll.aliases.tsv`).
- 뽑은 함수:
  - ntdll 3개 (`^(LdrpCorValidateImage|LdrpCorFixupImage|LdrpCorInitialize)$`, depth 0) → `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`. 요청서 대상 3개 중 `LdrpCorProcessImports` 는 이미 `.asm` 이 있었음.
  - ntdll 은 그 밖에 이미 있던 `.asm` 만 읽음: `LdrpInitializeProcess`, `LdrpCompleteMapModule`, `LdrpProcessMappedModule`, `LdrpProcessMachineMismatch`, `LdrpRelocateImage`, `LdrpInitializeThread`, `_LdrpInitialize`, `LdrpInitializeKernel32Functions`.
  - ntdll `refs` 3번 (읽기 전용, 몇 초): `LdrpCorExeMainRoutine`, `UseCOR`, `RtlImageDirectoryEntryToData` → `ms/disassemble/ntdll/_refs/`.
  - mscoree: 지도 (`_CorExeMain` 뿌리, 314개 노드) + 4번 export (깊이 0~1). 핵심 함수는 아래 "근거 파일" 에 모음.
- ntdll 프로젝트를 쓸 때 `ps` 로 보니 ntdll 쓰는 Ghidra 프로세스 없었음 (다른 decompiler 둘은 gdi32·gdi32full 등 다른 프로젝트). user32·win32u·sxs·sxssrv·kernelbase·basesrv 프로젝트는 열지 않음.
- 버전 확인: **일치** (ntdll).
  - `LdrpInitializeProcess +0x1bb6 / +0x1bbb → LdrpInitializeGraphRecurse` (`_calls.tsv`)
  - `_LdrpInitialize +0x167 / +0x16c → LdrpInitializeProcess` (`.asm` 로 다시 확인. 이 호출 앞에서 `RCX = R12 = 받은 CONTEXT`)
  - mscoree 는 처음이라 알려진 사실 표가 없음. 대신 PE 내보내기 표와 대조: `_CorExeMain` rva `0x7850`, `LoadLibraryShim` `0x23ee0`, `CLRCreateInstance` `0x13d0`, `_CorExeMain2` `0x26d40` 이 Ghidra 함수 시작과 다 같음.

## 스크립트 문제 (맨 위)

- 파일: `.claude/skills/ghidra-headless-export/scripts/ExportFunctions.java` 578줄 (`PrintWriter` 로 `.c` 쓰는 곳).
- 증상: mscoree 를 depth 1 로 뽑을 때 `run_ghidra.sh` 종료 코드 4. 로그에 `FileNotFoundException: .../ms/decomp/mscoree/_Wrapper_unsigned_short____ptr64__void___cdecl_DoNothing_...` (파일 이름이 너무 길어서 열기 실패).
- 원인 추정: C++ 템플릿 소멸자 `~Wrapper<unsigned_short*___ptr64,&void___cdecl_DoNothing<...>...>` 의 PDB 이름이 파일 이름 한계(255바이트)를 넘음. `InitShimImpl` 이 부르는 함수라 depth 1 에 걸림.
- 영향: 그 함수 파일과 그 뒤 순서의 일부 보조 함수 (`memset$thunk` 같은 것) 가 안 만들어졌을 수 있음. **이 결과서가 쓰는 함수는 전부 파일이 있음** (내가 읽은 것만 인용). 이후 깊이 0 으로 필요한 함수만 따로 뽑아 막힘 없었음.
- 고칠 방법 제안: 파일 이름이 150바이트 넘으면 앞 120글자 + `__<rva 16진수>` 로 줄이기 (같은 이름 둘째부터 `__<rva>` 붙이는 규칙이 이미 있으니 같은 자리에서). 고치는 건 오케스트레이터가 사용자 검토 뒤에.
- 덧붙임 1: mscoree 는 같은 이름 함수가 둘인 곳이 있음 (`_CorExeMain` 0x7850 / 0x2f6e8, `LoadLibraryShim` 0x2740 / 0x23ee0, `GetFullPath` 등). `_calls.tsv` 는 caller 이름으로만 모으니 **둘이 한 줄 묶음에 섞인다**. `.asm` 이 `…__2f6e8.asm` / `…__23ee0.asm` 로 갈라져 있으니 오프셋은 `.asm` 로 볼 것.
- 덧붙임 2: Ghidra 가 mscoree 의 인자 없는 바깥 함수 (`NoGuiFromShim` 의 설정 이름 같은 것) 를 `.c` 에서 틀리게 그림 → 그 부분은 `[공백]` 로 둠.

---

## 한눈에: 무엇이 필요한가 (지도)

IL-only 형 .NET Framework exe (x64 IL-only, 또는 AnyCPU = PE32 IL-only) 를 켠다고 칠 때, 차례대로 무엇이 필요한가. 열 표시: **덤프** = 지금 `ms/bin` 에 있나.

| # | 누가 | 하는 일 | 필요한 것 | 덤프 | 근거 |
|---|---|---|---|---|---|
| 0 | 커널 | 이 이미지를 어느 기계로 띄울지 정함 (.NET 정보를 보는 갈래가 있음) | `PspAllocateProcess +0xa87 / +0xa8c → PspSelectMachineForProcess` → (떨어진 범위 안) `+0x10a860 / +0x10a865 PspQueryComPlusRunUnderWoW` | (코드) | 이번 범위 밖. 호출 줄만 `_calls.tsv` 로 봄 `[정적]`. 본문은 안 읽음 |
| 1 | ntdll `LdrpInitializeProcess` | exe 머리를 보고 `UseCOR` / IL-only 표시 | exe 의 PE 머리 (Magic, 데이터 디렉터리 14) | (코드) | Q1 표 1~6 |
| 2 | ntdll `LdrpCorFixupImage` | PE32(i386) AnyCPU 머리를 메모리에서 PE32+ 로 고침 | exe 의 CLR 헤더 `Flags` | (코드) | Q1 |
| 3 | ntdll `LdrpCorInitialize` | **mscoree.dll 을 ntdll 이 직접 올림** + `_CorExeMain` 주소를 암호화해 둠 | `C:\Windows\System32\MSCOREE.DLL`, 환경변수 `COMPLUS_InstallRoot`·`COMPLUS_Version` | **있음** | Q1 |
| 4 | ntdll `LdrpCorValidateImage` | IL-only 인데 TLS 디렉터리가 있으면 거부 | exe 의 데이터 디렉터리 9 | (코드) | Q1 |
| 5 | ntdll `LdrpInitializeProcess +0x18b4` | **시작 문맥 `CONTEXT.Rcx` 를 `_CorExeMain` 으로 바꿈** | 3 에서 얻은 주소 | (코드) | Q1 |
| 6 | ntdll | mscoree 의 초기화 (`DllMain`) · IL-only 면 exe 의 import 는 안 걷음 | mscoree 의 import = `KERNEL32.dll` 하나 | 있음 | Q1 |
| 7 | 첫 스레드 | `RtlUserThreadStart(Rcx=_CorExeMain)` → `mscoree!_CorExeMain` | — | — | Q1 |
| 8 | mscoree `InitShimImpl` | 런타임 폴더 고르기 | 환경변수 `COMPlus_Version`·`COMPlus_InstallRoot`·`COMPlus_DefaultVersion`, 레지스트리 `HKLM\Software\Microsoft\.NETFramework` (`InstallRoot`), `...\.NETFramework\Policy\` (하위 키·값), `mscoree.dll.local` 파일 유무 | 레지스트리는 파일 아님 | Q2 |
| 9 | mscoree | `<InstallRoot>\<버전>\mscoreei.dll` 있나 보고 올림 | **`mscoreei.dll`** | **없음** | Q2 |
| 10 | mscoreei → clr | 진짜 런타임 시작 | `clr.dll` 외 (이름만 `[추측]`) | **없음** | Q3 |

- **없으면 어떻게 되나 (9)**: mscoree 는 "상태 3 = 못 찾음" 으로 두고 옛 길 (`GetInstallation`) 로 가서 실패 코드로 `ExitProcess` 한다 (Q2 마지막 항목).
- 표에서 **덤프에 더해야 하는 것은 9·10** (+ 레지스트리 값). 3~8 은 지금 덤프로 가능.

---

## 질문별 답 [정적]

### Q1. ntdll 로더의 CLR 헤더 갈래

**답 (한 문장씩)**
- CLR 헤더는 **두 길**로 본다. (a) 머리 Magic 이 `PE32 (0x10b)` 이고 이 프로세스가 WoW64 가 아니면 곧바로 `UseCOR` 로 치고 `LdrpCorFixupImage`. (b) 아니면 `RtlImageDirectoryEntryToData(이미지, TRUE, 14)` 가 NULL 이 아닌지 보고, `Flags & 1` (ILONLY) 이면 IL-only 표시.
- `UseCOR` 가 서면 **ntdll 이 직접** `MSCOREE.DLL` 을 올린다 (`LdrpCorInitialize`). exe 의 import 로 올리는 게 아니다. IL-only 이면 exe 의 import 표는 **아예 안 걷는다**.
- **`AddressOfEntryPoint` 는 무시된다.** 첫 스레드가 `UseCOR` 이면 `CONTEXT.Rcx` (커널이 넣은 exe 진입점) 를 `_CorExeMain` 주소로 **바꿔 쓴다**. IL-only 이면 `LdrpImageEntry.EntryPoint` 도 안 적는다.
- 지금 우리 (PE_emulator): 로더 흉내에 CLR 갈래가 없다고 하니 **빠져 있음 (틀림 → 추가 필요)**. 단 갈래 하나하나가 작다 (아래).

**차례 표** (함수 = `LdrpInitializeProcess` 이고 rva `0x8c404`. `ms/disassemble/ntdll/LdrpInitializeProcess.asm`. call/ret 는 `_calls.tsv` 와 같음)

| # | 로더 단계 | 함수 +오프셋 (call/ret) | 하는 일 |
|---|---|---|---|
| 1 | 초기화 | `+0x334` / `+0x33b` / `+0x33e` | `UseCOR=0`, IL-only 표시(R12B)=0, `[RSP+0x58]`(저장 칸)=0 |
| 2 | WoW64 여부 | `+0x5e3`, `+0x5f2` | `TEB.WowTebOffset` (`[R13+0x180c]`) 읽어 `UseWOW64 = (> 0)` |
| 3 | PE32 갈래 | `+0x602 ~ +0x611`, `+0x618`, `+0x627 / +0x62c` | `NtHeaders.Magic == 0x10b` 이고 `WowTebOffset <= 0` 이면 `UseCOR=1`, IL-only=1, `LdrpCorFixupImage(PEB.ImageBase)` 부름. 음수면 `+0x630` 에서 실패 출구 (`+0xa7b`) |
| 4 | CLR 디렉터리 갈래 | `+0x63b ~ +0x64b`, `+0x662 / +0x667` | WoW64 도 아니고 `UseCOR` 도 아직 0 일 때만: `RtlImageDirectoryEntryToData(base, TRUE, 14)` |
| 5 | 〃 결과 | `+0x66a`, `+0x66c`, `+0x673`, `+0x67b`, `+0x67f` | NULL 아니면 `UseCOR=1`. `COR20.Flags` (`[RAX+0x10]`) 의 비트 0 (ILONLY) 이 서면 IL-only=1 로 저장 |
| 6 | LoadConfig 건너뛰기 | `+0x6de ~ +0x6e9`, `+0x6ef / +0x6f4` | `WoW64` 이거나 (`UseCOR` 이고 3번의 PE32 갈래로 온 경우) 면 `LdrImageDirectoryEntryToLoadConfigEx` 를 안 부름 |
| 7 | 모듈 항목 표시 | `+0x13d7 ~ +0x13f0` | `UseCOR` 면 항목 `Flags` 비트 22 (`CorImage`), IL-only 면 비트 24 (`CorILOnly`) 도 켬 |
| 8 | 재배치 건너뛰기 | `+0x141f`, `+0x1422`, `+0x1430 / +0x1435` | 기본 주소가 다를 때 `LdrpProtectAndRelocateImage` 를 부르는 곳인데, IL-only 면 건너뜀 (CLR 이 한다) `[추측: 누가 대신 하는지]` |
| 9 | 진입점 기록 | `+0x14e9 / +0x14ee LdrpProcessMappedModule` | 안에서: `Flags & (ImageDll|CorILOnly) == ImageDll` 일 때만 `EntryPoint = base + AddressOfEntryPoint` 를 적음 (`LdrpProcessMappedModule +0x4e ~ +0x78`). **IL-only 면 안 적음** (`+0x5a ~ +0x5d` 의 `CMP ECX,4; JNZ +0x80`). 진입점 값 0 이면 0 을 적고 그냥 지나감 (`+0x6c` → `+0x235` → `+0x78`) |
| 10 | **mscoree 올림** | `+0x183c ~ +0x1843`, `+0x1845`, `+0x184d / +0x1852 LdrpCorInitialize`, `+0x1856 ~ +0x1858` | `UseCOR` 일 때만. 음수면 실패 출구 (`+0xa7b`). `[RSP+0x178]` 에 mscoree 모듈 항목을 받음 |
| 11 | IL-only 검증 | `+0x185e / +0x1861`, `+0x1867 / +0x186c LdrpCorValidateImage`, `+0x1870 ~ +0x1872` | IL-only 표시일 때만. 음수면 실패 출구 |
| 12 | **시작 문맥 바꾸기** | `+0x1878 ~ +0x188a`, `+0x188c ~ +0x18b4` | `TEB.SameTebFlags & 0x400` (`InitialThread`) 이면 `LdrpCorExeMainRoutine` 을 풀어서 `CONTEXT.Rcx` (`[RBX+0x80]`, `RBX = [RSP+0xe0]` = 받은 CONTEXT) 에 씀 (`+0x18b4`). **IL-only 와 상관없이 `UseCOR` 이면 한다** (혼합 모드도) |
| 13 | TLS 준비 | `+0x18cb / +0x18d0 LdrpInitializeTls` | (참고) 10~12 뒤 |
| 14 | mscoree 초기화 | `+0x1925 / +0x1928`, `+0x193c / +0x1941 LdrpInitializeGraphRecurse` | `[RSP+0x178]` 가 있으면 그 모듈의 그래프 노드 (`[+0x98]`) 로 초기화 (mscoree 의 진입함수 rva `0x9780` 이 불림 `[추측: 본문 안 봄]`) |
| 15 | **import 갈림길** | `+0x1a73 / +0x1a76` | IL-only 표시면 → 16, 아니면 → 17 |
| 16 | 〃 IL-only | `+0x1a78`, `+0x1a7f / +0x1a84 LdrpCorProcessImports`, `+0x1a87 → +0x1af2` | exe 의 import 를 **걷지 않고** 상태만 `6 → 7` 로 올리고 알림 (`AVrfDllLoadNotification`·`LdrpSendDllNotifications`·`LdrpLogDllState`). `LdrpPrepareModuleForExecution` 도 건너뜀 |
| 17 | 〃 보통 / 혼합 모드 | `+0x1aa4 / +0x1aa9 LdrpMapAndSnapDependency`, `+0x1aed / +0x1af2 LdrpPrepareModuleForExecution` | 평소 exe 처럼 import 를 걷고 (`mscoree.dll` 은 10 에서 이미 올라와 있음) 준비 |

**`LdrpCorInitialize`** (rva `0x8a808`, 호출한 곳은 `LdrpInitializeProcess` 하나. `ms/disassemble/ntdll/LdrpCorInitialize.asm`)
- 환경변수 두 개를 봄 (`FastPebLock` 잡고, `+0x93 / +0x98`, `+0xc0 / +0xc5`): `COMPLUS_InstallRoot` (`0x13` 글자), `COMPLUS_Version` (`0xf` 글자). 값이 있으면 "버퍼가 작다" (`0xc0000023`) 가 돌아오는 걸로 "있다" 를 잼.
- **둘 다 있을 때만** 이름 그대로 `MSCOREE.DLL` 을 올림 (`+0xf4 JZ +0x1de`). **보통은** `LdrpBuildSystem32FileName` (`+0x106/+0x10b`) 으로 시스템 폴더 전체 경로를 만든다. 이름 글자는 ntdll 안 `LdrpMscoreeDllName` = `MSCOREE.DLL`.
- `LdrpLoadDll(이름, …, 플래그 1, 결과 받을 곳)` (`+0x16a / +0x16f`) → `LdrpReleaseDllPath` → `LdrpGetProcedureAddress(mscoree 의 DllBase, "_CorExeMain", 0, &주소)` (`+0x19a / +0x19f`).
- 성공이면 `LdrpCorExeMainRoutine = ROR(쿠키 ^ 주소, 쿠키 & 0x3f)` (`+0x1a5 ~ +0x1bb`, 쿠키 = `KUSER_SHARED_DATA+0x330`). `+0x1c2` 로 결과 모듈 항목을 호출자에게 넘김. 이름을 못 찾으면 `LdrpDecrementModuleLoadCountEx` (`+0x1d7`).
- 돌려주는 값 = `LdrpLoadDll` 나 `LdrpGetProcedureAddress` 가 낸 상태. **mscoree 가 없으면 이 상태로 프로세스 초기화가 실패한다** (호출한 쪽 `+0x1856 ~ +0x1858`).
- `LdrpCorExeMainRoutine` 을 읽고 쓰는 곳은 **딱 3곳** (`refs`, `_refs/LdrpCorExeMainRoutine.tsv`): 쓰기 `LdrpCorInitialize +0x1bb`, 읽기 `LdrpInitializeProcess +0x189d`, `LdrpInitializeThread +0x448`.

**`LdrpCorFixupImage`** (rva `0x109374`, 388B. `ms/disassemble/ntdll/LdrpCorFixupImage.asm`)
- 조건: `Magic == 0x10b` (`+0x4b ~ +0x54`), `Machine == 0x14c` (`+0x5a ~ +0x63`), `COR20.Flags & 2` (**32BITREQUIRED**) 가 **꺼짐** (`+0x69 ~ +0x6d`). 맞으면:
  - 머리가 든 첫 쪽(0x1000)을 `NtProtectVirtualMemory(-1, …, PAGE_READWRITE=4)` 로 쓰기 가능하게 (`+0xcc / +0xd1`) → 선택 헤더를 `0x10` 바이트 뒤로 밀고 (`RtlCopyMemory` `+0xe7 / +0xec`, `SizeOfOptionalHeader += 0x10` `+0xf3`) → `ImageBase`·스택/힙 크기를 8바이트로 넓힘 (`+0x106 ~ +0x12d`) → `Magic = 0x20b` (`+0x135`) → 보호 되돌림 (`+0x144 / +0x149`).
  - 즉 **메모리 안에서 PE32 AnyCPU 머리를 PE32+ 로 바꾼다.** 선택 헤더가 쪽 안에 안 들어가면 `0xc000007b` (`+0xad`).
- 맞지 않으면 (`+0x15b`): `Machine` 이 `KUSER_SHARED_DATA.ImageNumberLow/High` (`+0x2c`/`+0x2e`) 범위 밖이면 `0xc000007b`, 안이면 0 (`+0x15f ~ +0x181`).
- 부른 곳: `LdrpInitializeProcess +0x627 / +0x62c` (exe, 3번), `LdrpCompleteMapModule +0x2a6 / +0x2ab` (DLL, 아래).

**`LdrpCorValidateImage`** (rva `0x10e4b4`, 37B. `ms/disassemble/ntdll/LdrpCorValidateImage.asm`)
- `RtlImageDirectoryEntryToData(base, TRUE, **9**)` (`+0x11 / +0x16`, 9 = TLS 디렉터리) 가 NULL 이 아니면 `0xc000007b` (STATUS_INVALID_IMAGE_FORMAT), NULL 이면 0 (`+0x16 ~ +0x1b`, `NEG; SBB; AND 0xc000007b`). → **IL-only 이미지는 TLS 디렉터리가 있으면 안 된다.**
- 부른 곳: `LdrpInitializeProcess +0x1867 / +0x186c` (exe, IL-only 일 때만), `LdrpCompleteMapModule +0x285 / +0x28a` (DLL).

**`LdrpCorProcessImports`** (rva `0xb7e0`, 97B. `ms/disassemble/ntdll/LdrpCorProcessImports.asm`)
- 그래프 노드 상태 `6` 쓰고 (`+0x14`) `AVrfDllLoadNotification` (`+0x1b / +0x20`) → `LdrpSendDllNotifications(entry, 1)` (`+0x2e / +0x33`) → `LdrpLogDllState` (`+0x41 / +0x46`) → 상태 `7` (`+0x4d`). **import 는 하나도 안 본다.**
- 부른 곳: `LdrpInitializeProcess +0x1a7f / +0x1a84` (exe IL-only), `LdrpMapDllWithSectionHandle +0x2d0 / +0x2d5` (DLL 이 `CorILOnly` 일 때, sast/21 에서 읽음).

**DLL 쪽 (참고 — exe 길과 별개).** `LdrpCompleteMapModule` 은 `RtlImageDirectoryEntryToData` 가 아니라 머리를 직접 읽는다 (`+0x7c ~ +0x9f`: Magic `0x20b` 이면 `NumberOfRvaAndSizes > 0xe` 인지와 디렉터리 14 의 VA; PE32 면 `+0x145`). 있으면 `+0x263 ~ +0x279` 에서 `CorImage`·(`Flags & 1` 이면) `CorILOnly` 를 켜고 `+0x285` 검증, 이어 `ctx+0x20 & 0x200000` (`LdrpProcessMachineMismatch` 가 켠 것, `+0x37`) 이면 `+0x2a6` Fixup. `ctx+0x20 & 0x800000` 이면 `0xc000007b` (`+0x109 ~ +0x117`). 자세한 건 sast/33.

**14번 디렉터리를 읽는 곳, 다시 정리** (ntdll 에서 `RtlImageDirectoryEntryToData` 를 부르는 곳은 `refs` 로 32곳. 부르기 직전 바이트에서 14(0xe) 를 직접 넣는 곳은 아래 4곳, 나머지는 다른 번호 `[정적: 바이트 확인, lea 꼴은 레지스터가 0 이라고 보고 읽음]`):
- `LdrpInitializeProcess +0x662 / +0x667` (exe) — 위 4·5번
- `LdrpCorFixupImage +0x42 / +0x47` — `Flags & 2` 읽음
- `LdrpProcessMachineMismatch +0x27 / +0x2c` — DLL 이 IL-only (`Flags & 1`) 면 `ctx+0x20` 비트 21 켜고 0 돌려줌 (`+0x31 ~ +0x3c`), 아니면 하드 에러 `0x4000000e` 띄움 (`+0x90`)
- `LdrpRelocateImage +0x12b / +0x130` — DLL 이 IL-only 면 재배치 안 하고 성공 (`+0x15d`, `+0x161`, `+0x163 JMP`)

**첫 스레드가 아닌 쪽의 같은 일.** `LdrpInitializeThread` 에도 똑같이 "`UseCOR` 이고 `InitialThread` 면 `CONTEXT.Rcx` 를 `_CorExeMain` 으로" 가 있다 (`+0x40 / +0x47` → `+0x421 ~ +0x458`, `R9 = 받은 CONTEXT`). 평소 길에선 프로세스 초기화 때 위 12번에서 이미 끝난다. 이 줄을 언제 타는지는 정적으로 못 정함 (동적 목록 참고).

**exe 의 `AddressOfEntryPoint` 는?**
- 커널은 그대로 `Rcx = exe 진입점` 으로 만든다 (sast/11·69: `NtCreateUserProcess +0xd09` 의 `TransferAddress`). 이 `Rcx` 를 ntdll 이 **덮어쓴다** (12번). 그래서 `RtlUserThreadStart` 는 exe 진입점 대신 `mscoree!_CorExeMain` 을 부른다.
- 진입점 값이 **0 이어도** 로더가 막지 않는다 (9번). 실제로 덤프의 x64 IL-only exe 는 진입점이 0 이다 (아래 Q4).

**64비트 IL-only 와 혼합 모드(C++/CLI) 의 갈림** (같은 코드, 표시 하나 차이)

| | x64 IL-only (PE32+) | AnyCPU IL-only (PE32 i386) | 혼합 모드 (PE32+, ILONLY 꺼짐) |
|---|---|---|---|
| 판정 | 4·5번 (디렉터리 14, `Flags&1`) | 3번 (Magic `0x10b` + 비 WoW64) | 4번 (디렉터리 14만 있음) |
| `LdrpCorFixupImage` | 안 부름 | 부름 (머리를 PE32+ 로) | 안 부름 |
| `CorImage` / `CorILOnly` | 둘 다 | 둘 다 | `CorImage` 만 |
| `EntryPoint` 기록 | 안 함 | 안 함 | 함 (`AddressOfEntryPoint` 있으면) |
| 재배치 | ntdll 이 안 함 | 〃 | 함 |
| TLS 디렉터리 | 있으면 `0xc000007b` | 〃 | 허용 |
| exe import 걷기 | **안 걷음** (`LdrpCorProcessImports`) | 〃 | 걷음 (`LdrpMapAndSnapDependency`) |
| `Rcx` 바꾸기 / mscoree 올림 | 함 | 함 | 함 |

- AnyCPU(PE32) 가 `UseWOW64 = 0` 으로 들어온다는 건 커널이 이 프로세스를 64비트로 띄웠다는 뜻이다. 그 판단은 커널 쪽 (`PspSelectMachineForProcess`) 이라 이번에 안 봄. `32BITREQUIRED` (`Flags & 2`) 가 서 있는 PE32 가 `WowTebOffset <= 0` 으로 오면 `LdrpCorFixupImage` 가 `0xc000007b` 를 낸다 (위 `+0x15b` 길) → 그런 exe 는 WoW64 로 가야 정상 `[추측: 커널 쪽]`.

### Q2. `mscoree!_CorExeMain` → 런타임 고르기 첫 단계

근거 파일: `ms/disassemble/mscoree/{_CorExeMain,ShellShim__CorExeMain,InitShimImpl,FindLatestVersion,FindLatestVersionWithCallback,GetShimDllPathName,CheckShimImplCallback,LoadLibraryShim,LoadLibraryWrapper,UpdateShimImplStatus,EnvGetString}.asm` 와 같은 이름 `.c` (`ms/decomp/mscoree/`), 지도 `ms/disassemble/mscoree/_map/_CorExeMain_*`.

**줄기** (모두 `[정적]`. 오프셋은 `.asm` 의 call/ret)
1. `RtlUserThreadStart → BaseThreadInitThunk → mscoree!_CorExeMain` (rva `0x7850`, 58B): `+0x6 / +0xb ShellShim__CorExeMain` 부름. 돌아오면 상태가 0 이면 `+0x17 / +0x1c InitShimImpl`, 상태가 1 이면 `+0x2d / +0x32 DisplayMessageBoxForNoShimImpl`.
2. `ShellShim__CorExeMain` (rva `0x7894`): 상태 `g_shimImplStatus` 가 0 이면 `+0xa3 / +0xa8 InitShimImpl`. 그다음 갈림 — 상태 1 → `0x80131701` 돌려줌 (`+0x2e ~ +0x31`, `+0xf8`), 상태 3 → 아래 "없으면" (`+0x37 ~ +0x3a`, `+0xb2`), 상태 2 → `mscoreei` 모듈에서 `GetProcAddress(…, "_CorExeMain_RetAddr")` (`+0xc1 / +0xc8`) 가 있으면 그걸로, 없으면 `GetProcAddress(…, "_CorExeMain")` (`+0xe0 / +0xe7`) 로 **꼬리 점프** (`+0x9e` / `+0x88`, CFG 검사를 거침). 꼬리 점프라 콜스택엔 mscoree 줄이 안 남는다.
3. `InitShimImpl` (rva `0x27b0`, 808B) 가 런타임 고르기 첫 단계 (아래).

**읽는 환경변수** (`EnvGetString` = `GetEnvironmentVariableW("COMPlus_" + 이름)` `.asm`/`.c`; 시스템 호출 없음, PEB 의 환경 블록만 봄):

| 이름 | 어디서 | 뜻 |
|---|---|---|
| `COMPlus_Version`, `COMPlus_InstallRoot` | `FindLatestVersion +0x24 / +0x29`, `+0x3e / +0x43` | **둘 다** 있고 `<InstallRoot>\<Version>\mscoreei.dll` 이 실제로 있으면 레지스트리는 안 보고 그 버전 사용 (`CheckShimImplCallback`, `+0x163 / +0x168`) |
| `COMPlus_InstallRoot` | `GetShimDllPathName +0xc0 / +0xc7` | 설치 폴더. 없거나 빈 값이면 레지스트리 |
| `COMPlus_DefaultVersion` | `FindLatestVersionWithCallback +0x712 / +0x719` | 레지스트리에서 못 골랐을 때의 기본 버전 |

**읽는 레지스트리** (`HKEY_LOCAL_MACHINE` = `0xffffffff80000002`. 이 DLL 은 레지스트리 함수를 **지연 import (ADVAPI32)** 해서 처음 쓸 때 ADVAPI32.dll 이 올라온다 — `.didat` 의 지연 import 표 `0x5d7b4`):

| 키 / 값 | 어디서 | 하는 일 |
|---|---|---|
| `Software\Microsoft\.NETFramework\Policy\` | `FindLatestVersionWithCallback +0xdd / +0xe4` `RegOpenKeyExW` | 하위 키 (`v4.0`, `v2.0` 같은 이름 `[추측: 이름]`) 를 `RegQueryInfoKeyW` (`+0x155`) · `RegEnumKeyExW` (`+0x1e7`) 로 거꾸로 돌면서 후보를 고름 |
| `...\Policy\<하위 키>` | `+0x3cf / +0x3d6` `RegOpenKeyExW`, `+0x3fd / +0x402 FindLatestBuild` | 값 이름들을 `RegEnumValueW` 로 돌며 `_wtoi` 해서 **가장 큰 빌드 번호** (`FindLatestBuild`). 버전 문자열은 점(`.`) 으로 붙임 |
| `Software\Microsoft\.NETFramework` 의 값 `InstallRoot` | `GetShimDllPathName +0x13f / +0x146` 열기, `+0x176 / +0x17d` 값 읽기 (문자열 형 `==1`), `+0x1e0` 닫기 | 설치 폴더 (키를 열 때 읽기 권한 `0x20019`, WoW 보기 플래그 없음 = 이 프로세스 자기 보기) |

**읽는 파일·경로**:
- `mscoree.dll.local` (자기 파일 이름 + `.local`) 이 있나: `GetModuleFileNameW` + `GetFileAttributesW` (`GetShimDllPathName +0x4be`, `+0x4f2`, `+0x537`, `+0x56b` 등 — 같은 일이 갈래마다 한 번씩 나옴). 있으면 레지스트리 대신 **mscoree.dll 이 있는 폴더**를 런타임 폴더로 씀 (`g_UseLocalRuntime`).
- 후보 경로 = `<InstallRoot>` (+ 필요하면 `\`) + `<버전>` + `\` + **`mscoreei.dll`** (`GetShimDllPathName`, 이름 글자는 `InitShimImpl +0x68` 의 `LEA` → `0x18004e988` = `mscoreei.dll`). 만든 경로가 `GetFullPathNameW` 로 정규화한 것과 `_wcsicmp` 로 같아야 함.
- 존재 확인: `FindFirstFileW` / `FindClose` (`InitShimImpl +0x9b ~ +0xcc`, `CheckShimImplCallback +0x53 / +0x5a`, `+0x68 / +0x6f`).
- 올리기: `LoadLibraryShim` (`LoadLibraryShim.asm` rva `0x2740`) → `GetShimDllPathName` (`+0x23 / +0x28`) → `LoadLibraryWrapper` (`+0x31 / +0x36`) → `LoadLibraryExW(경로, NULL, 8)` (`LOAD_WITH_ALTERED_SEARCH_PATH`). 그다음 `InitShimImpl` 이 `GetProcAddress` 네 번 (`+0x10c`: `RegisterShimImplCallback`, `+0x125`: `RegisterShimImplCleanupCallback`, `+0x13e`: `SetShellShimInstance`, `+0x160`: `OnShimDllMainCalled`, 각 ret `+0x113`/`+0x12c`/`+0x145`/`+0x167`) 하고 콜백을 등록 (CFG 간접 호출, `+0x1b8` 쪽). 올리기가 실패했거나, 앞의 둘이 둘 다 없으면 (`FreeLibrary` 도 함) 상태 1.

**상태값 `g_shimImplStatus`** (코드에서 읽은 뜻): 0 = 아직 안 함, 1 = 파일은 있는데 올리기·내보내기 찾기 실패, 2 = mscoreei 올림 (모듈은 `g_hShimImplInst`), 3 = 버전이나 파일을 못 찾음.

**API → 시스템 호출 대응 [추측]** (kernelbase 의 `Reg*`·`GetFileAttributesW`·`FindFirstFileW` 본문은 안 봤다. kernelbase 프로젝트는 다른 decompiler 가 쓰는 중이라 이번엔 안 열었음. `LoadLibraryExW` 만 기존 `.asm` 로 확인: `+0xf8 / +0xff LdrLoadDll`)

| 부르는 API | 아마 가는 NT 함수 `[추측]` |
|---|---|
| `LoadLibraryExW(…, 8)` | `LdrLoadDll` `[정적]` → 평소 DLL 올리는 길 (run20 ~ run30) |
| `RegOpenKeyExW` / `RegQueryValueExW` / `RegEnumKeyExW` / `RegQueryInfoKeyW` / `RegEnumValueW` / `RegCloseKey` | `NtOpenKey(Ex)` / `NtQueryValueKey` / `NtEnumerateKey` / `NtQueryKey` / `NtEnumerateValueKey` / `NtClose` |
| `GetFileAttributesW`, `FindFirstFileW` | `NtQueryAttributesFile`·`NtQueryFullAttributesFile` / `NtOpenFile` + `NtQueryDirectoryFile` 계열 |
| ADVAPI32 지연 로드 (첫 레지스트리 호출 때) | `LdrLoadDll` → 다시 DLL 올리는 길 |

**mscoreei 가 없으면** (`FindFirstFileW` 가 실패하거나 버전을 못 골라서; `InitShimImpl +0x52 / +0x76 / +0xab` 의 `JS`/`JZ` 가 모두 `+0x309` 로 감) — 우리 덤프가 이 경우:
- `+0x309 ~ +0x315 / +0x31a`: `UpdateShimImplStatus(3, "mscoreei.dll")` → 상태 3.
- `ShellShim__CorExeMain +0x3a → +0xad ~ +0xb2` 가 `_CorExeMain` 의 **둘째 함수** (rva `0x2f6e8`, 91B) 로 꼬리 점프.
- 그 함수 (`_CorExeMain__2f6e8.asm`): `+0x18 / +0x1d GetInstallation(1, &모듈, 0)` → 실패 (음수, `+0x21`) 면 `+0x53 ~ +0x55 ExitProcess(그 HRESULT)` (ret `+0x5c`). 성공이면 `+0x2f / +0x36 GetProcAddress(모듈, "_CorExeMain")` 로 점프 (`+0x45`).
- `GetInstallation` (rva `0x28c2c`) 은 **옛 런타임 고르기 길** (CLR 2.0 시절): `RuntimeRequest::RequestRuntimeDll` (`+0xf2 / +0xf7`) → `FindVersionedRuntime` → `GetInstallRoot` (위와 같은 환경변수·`.local`·`InstallRoot` 레지스트리) → 폴더를 못 찾으면 (`InstallRoot` 가 NULL) **메시지 상자** ("A fatal error occurred. However, mscorees.dll could not be loaded to display the appropriate error message. Please reinstall the .NET Framework." 또는 `mscorees.dll` 자원 글줄; 제목 "Error") 후 실패 코드. 찾으면 `ComputeVersionString` (환경변수 `Version`·`BuildFlavor` 이름이 보임) → `LoadVersionedRuntime` (문자열 `v1.0`·`v1.1`·`svr`).
- **어떤 HRESULT 로 끝나는지는 정적으로 못 정함** `[공백]`. 메시지 상자는 `NoGuiFromShim()` 이 0 일 때만 (설정값, 이름 안 읽힘 `[공백]`) 뜨고, 뜨려면 **USER32 지연 로드** + `MessageBoxW` 가 필요 (우리 sast/82 쪽 길).
- 상태 1 (파일은 있는데 못 올림) 이면 `0x80131701` 을 돌려주고 `_CorExeMain` 쪽에서 메시지 상자 (`+0x2d`) 를 띄움. 이 코드는 `CLR_E_SHIM_RUNTIMEEXPORT` 로 보임 `[추측: 이름]`.

지금 우리 (PE_emulator): **없음 → 추가 필요**. mscoree 는 진짜 코드를 그대로 돌리면 되고 (덤프에 있음), 필요한 건 레지스트리·파일 열기에 대한 대답이다.

### Q3. `clr.dll` 시작까지 큰 줄기 (이름·경로 수준)

우리 덤프에 `mscoreei.dll`·`clr.dll`·`Microsoft.NET` 이 없어서 **정적 근거는 mscoree 쪽 경계까지**다. 그 뒤는 `[추측]` (.NET Framework 4.x 일반 지식, 이 저장소에서 확인 안 함).

- `[정적]` mscoree 가 mscoreei 에서 꺼내 쓰는 이름 (`InitShimImpl`, `ShellShim__CorExeMain`, `LoadLibraryShim` 내보내기 `rva 0x23ee0`): `RegisterShimImplCallback`, `RegisterShimImplCleanupCallback`, `SetShellShimInstance`, `OnShimDllMainCalled`, `_CorExeMain_RetAddr`, `_CorExeMain`, `LoadLibraryShim`, `LoadLibraryShim_RetAddr`.
- `[정적]` mscoree 는 `LoadLibraryShim(파일이름, 버전, …)` 를 **밖에 내보낸다** (`LoadLibraryShim__23ee0.asm`). 상태 2 면 mscoreei 의 같은 이름 함수로 넘김, 3 이면 안쪽 `LoadLibraryShim`. 같은 `GetShimDllPathName` 으로 `<InstallRoot>\<버전>\<파일이름>` 을 만든다 → **clr.dll 도 같은 규칙의 경로로 올릴 것**이라는 뜻 `[추측: 호출자는 mscoreei 쪽 코드]`.
- `[추측]` mscoreei 안에서 하는 일: exe 의 CLR 헤더에서 요구 버전 (메타데이터 문자열 `v4.0.30319` 가 덤프 exe 들에 있음, Q4) 과 `exe.config` 를 읽고 → `clr.dll` 올림 → `_CorExeMain` 내보내기 → 관리 코드 시작.
- `[추측]` 그 뒤 필요할 만한 것 (JIT·GC 안 보는 범위라 이름만): `clrjit.dll`, `mscorlib.dll` (또는 NGen 이미지), `clrcompression.dll`, `<InstallRoot>\<버전>\Config\machine.config`. 확정은 VM 에서 파일 열림 목록으로만 가능.
- `[정적]` mscoree 쪽에서 딸려 올라오는 DLL: 정적 import = `KERNEL32.dll` 하나. 지연 import = `USER32` (메시지 상자), `ADVAPI32` (레지스트리·이벤트 로그), `SHLWAPI`, `VERSION`, `OLEAUT32`, `urlmon`. 메시지 자원은 `System32\MUI\0409\mscorees.dll` (덤프에 **있음**).

지금 우리 (PE_emulator): **없음 (하지만 이 단계는 보류가 맞음)**.

### Q4. Framework exe 와 .NET Core/5+ exe 를 머리에서 가르는 법

로더가 보는 칸은 **데이터 디렉터리 14 와 Magic 뿐**이다 (Q1 3·4번). import 표는 로더가 판정에 안 쓴다. 아래는 덤프에 실제로 있는 **관리 코드 exe 11개** (`ms/bin` 훑어서 `scan` 한 결과; 임시 PE 머리 읽기 스크립트로 확인, 레포에는 안 남김) 로 확인한 모양이다.

| 종류 | 머리 모양 | 덤프 예 |
|---|---|---|
| **x64 IL-only** | PE32+, Machine `0x8664`, **디렉터리 14 있음 (크기 72)**, CLR `Flags=0x9`, **`AddressOfEntryPoint = 0`, import 표 없음 (디렉터리 1 비어 있음)** | `tzsync.exe`, `AppVStreamingUX.exe` (`System32\AppV\`), `powershell_ise.exe` (x64, `WindowsPowerShell\v1.0\`) |
| **AnyCPU IL-only** | PE32, Machine `0x14c`, 디렉터리 14, `Flags=0x1` 또는 `0x9`, `32BITREQUIRED`(비트1) 꺼짐, **import 는 `mscoree.dll!_CorExeMain` 하나**, 진입점 첫 바이트 `FF 25` (그 import 로 점프하는 껍데기) | `ScriptRunner.exe`, `UevAppMonitor.exe`, `Microsoft.Uev.SyncController.exe` |
| **x86 전용 IL-only** | PE32, `0x14c`, `Flags` 에 `32BITREQUIRED` (`0x3`·`0xb`) | `UevTemplate*Generator.exe` 셋, `powershell_ise.exe` (x86) |
| **혼합 모드 (C++/CLI)** | PE32+, 디렉터리 14, `Flags=0x8` (ILONLY **꺼짐**), 평범한 import 표 + `mscoree.dll!_CorExeMain`·`CorBindToRuntimeEx`, `.nep` 구역 | `FileHistory.exe` |
| **.NET Core / 5+ 앱호스트** | 네이티브 PE32+ exe, **디렉터리 14 없음**, `mscoree` import 없음, 평범한 네이티브 import (kernel32 등) | 덤프에 **샘플 없음** `[추측: 일반 지식]` |

- 덤프의 관리 코드 exe 11개 모두: CLR 헤더 `cb=72`, 런타임 버전 `2.5`, 메타데이터 버전 문자열 `v4.0.30319`, 진입점 토큰 `0x06......` (MethodDef) `[정적: 바이트 읽음]`. 부차 구분은 `Flags` 와 Machine 이다.
- **앱호스트 쪽 표식은 코드로 확인 못 함** `[추측: 일반 지식, 덤프에 샘플 없음]`: 앱호스트는 `hostfxr.dll` 을 실행 때 찾아 올리고 (`DOTNET_ROOT` 나 `Program Files\dotnet` 아래), 응용 dll 경로를 박아 넣는 자리 표시 문자열을 exe 안에 갖는다. 단일 파일 번들은 그 exe 끝(오버레이)에 묶음을 붙이고, 앱호스트 안의 정해진 표식 바이트열로 시작 위치를 가리킨다. → 로더 입장에선 **그냥 네이티브 exe** (`UseCOR=0`) 라 이 요청의 갈래와 상관없다.
- 가르는 순서 (이 저장소 코드 기준): ① Magic == `0x10b` 이고 비 WoW64 → Framework AnyCPU. ② 디렉터리 14 가 있다 → Framework x64 (ILONLY 비트로 IL-only/혼합). ③ 둘 다 아니다 → 네이티브 (앱호스트 포함).
- 주의: 디렉터리 14 만 있고 `mscoree` import 가 없는 exe 가 정상이다 (x64 IL-only). **"mscoree import 가 있나" 로 가르면 틀린다.**

지금 우리 (PE_emulator): 머리 읽기만이라 **쉬움. 단 "진입점 0 · import 없음" 인 exe 를 로더가 통과시키는지 확인 필요** (PE 읽는 쪽 코드는 이 저장소 밖이라 `[공백]`).

### Q5. 덤프에 더할 파일 / 할 만한가

**덤프에 더할 파일** (확실한 정도 순)

| 순 | 파일 / 값 | 확실도 | 근거 |
|---|---|---|---|
| 1 | `Windows\Microsoft.NET\Framework64\v4.0.30319\mscoreei.dll` | 이름·구성은 `[정적]`, 폴더 글자는 `[추측]` | 경로 조립 = `<InstallRoot>` + `<버전>` + `mscoreei.dll` (`GetShimDllPathName`). 폴더 글자는 레지스트리 값이 정함 |
| 2 | 레지스트리: `HKLM\Software\Microsoft\.NETFramework` 값 `InstallRoot` (문자열), `HKLM\Software\Microsoft\.NETFramework\Policy\` 하위 키와 값 (예: `v4.0` 키 아래 `30319`) | 키·값 **이름**은 `[정적]`, **실제 값**은 `[확인 필요]` | `GetShimDllPathName +0x13f / +0x176`, `FindLatestVersionWithCallback +0xdd / +0x3cf`. 값은 VM 에서만 |
| 3 | `...\v4.0.30319\clr.dll` | `[추측]` (mscoreei 안쪽) | Q3 |
| 4 | `...\v4.0.30319\clrjit.dll`, `mscorlib.dll`(+ NGen 이미지), `clrcompression.dll`, `Config\machine.config` | `[추측]` | 관리 코드를 실제로 돌릴 때 필요할 만한 것 |
| 5 | 돌려 볼 관리 코드 exe 가 쓰는 어셈블리 (`System.dll` 등) | `[추측]` | exe 마다 다름 |
| — | 이미 있음: `System32\mscoree.dll`, `System32\MUI\0409\mscorees.dll`, `SysWOW64\mscoree.dll` | `[정적: 파일 있음]` | |
| — | 이미 있음: 시험용 관리 코드 exe 11개 (Q4) | | `ms/bin` |

- **확정하려면 VM 에서 .NET exe (예: `ScriptRunner.exe` 나 `tzsync.exe`) 를 한 번 돌려 파일 열기 목록과 `InstallRoot` 값을 뽑아야 한다** (정적으로는 이름·규칙까지).

**"PE_emulator 가 할 만한가" 한 줄:** **1단계 (ntdll 의 CLR 갈래 + `mscoree!_CorExeMain` 의 `InitShimImpl` 까지) 는 할 만하다** — 바뀌는 곳이 `LdrpInitializeProcess` 안 6군데 + 작은 함수 3개 (`LdrpCorFixupImage` 388B, `LdrpCorValidateImage` 37B, `LdrpCorInitialize`) 뿐이고, 필요한 mscoree 는 이미 덤프에 있다. **2단계 (mscoreei → clr 로 진짜 관리 코드 시작) 는 지금은 아니다** — 덤프에 .NET Framework 폴더를 통째로 더해야 하고 정확한 목록은 VM 에서만 정해지며, CLR 안쪽이 부르는 syscall 양이 이 요청 범위 밖이라 비용을 못 쟀다. 대상 샘플은 전체의 2% (310개).

## 동적분석으로 확인해야 할 것

- **`CONTEXT.Rcx` 가 바뀌는 순간**: `ntdll!LdrpInitializeProcess+0x18b4` 에서 `[RBX+0x80]` 쓰기 직전 `RAX` (= `_CorExeMain` 주소), 직전 `[RBX+0x80]` (= exe 진입점). 이어서 `NtContinue` 직전 CONTEXT 와 비교 (run7·run29 의 첫 스레드 시작 문맥과 이어짐).
- **PE32 AnyCPU 가 정말 64비트로 오는지**: `ScriptRunner.exe` 의 `TEB.WowTebOffset` (`+0x180c`) 이 0 인지, `LdrpInitializeProcess +0x627` 에 걸리는지, `LdrpCorFixupImage` 전후 머리의 `Magic`. 커널 쪽 결정 (`PspSelectMachineForProcess`) 은 다음 요청 후보.
- **`KUSER_SHARED_DATA+0x2c`/`+0x2e` (`ImageNumberLow/High`) 의 값** (`LdrpCorFixupImage +0x15f`, `+0x177` 이 읽음): 정적으로 값을 모름.
- **`LdrpInitializeThread +0x40` 길을 타는 경우가 있나** (첫 스레드가 여기로 오는 때). 정적으로 못 정함.
- **mscoree 가 읽는 레지스트리 값·열려는 파일 경로의 실제 목록** (`InstallRoot` 값, `Policy\v4.0` 아래 값, 결국 고른 버전 문자열, 실제로 `LoadLibraryExW` 에 넘어가는 전체 경로): VM 에서 확인.
- **mscoreei 가 없는 상태의 끝**: 어떤 HRESULT 로 `ExitProcess` 하나, 메시지 상자가 뜨나 (`NoGuiFromShim` 설정값).
- 추천 멈춤점 (모두 `test.exe` 가 아니라 **.NET exe 일 때만** 걸리게 조건 필요):
  - `ntdll!LdrpCorInitialize` 입구와 `+0x16a` (`LdrpLoadDll` 부르기 전 `RCX` = 경로 문자열), `+0x1bb` (암호화 저장)
  - `ntdll!LdrpInitializeProcess+0x18b4` (Rcx 쓰기)
  - `mscoree!InitShimImpl` 입구, `+0x6f` (`GetShimDllPathName` 결과 경로), `+0xab` (`FindFirstFileW` 결과), `+0xe7` (`LoadLibraryShim`)
  - `mscoree!ShellShim__CorExeMain+0x9e` / `+0x88` / `+0xb2` (어디로 점프하나, 상태 값 `g_shimImplStatus`)

## 다음 요청 후보

1. **커널 쪽 기계 종류 결정**: `PspSelectMachineForProcess` 본문 (`PspAllocateProcess +0xa87`) + `PspQueryComPlusRunUnderWoW` + `ExpReadComPlusPackage` (어떤 레지스트리 값을 읽나) + 섹션 만들 때 CLR 정보를 `ImageInformation` 에 싣는 줄. AnyCPU(PE32) 를 64비트로 띄우는 근거.
2. **kernelbase 의 `RegOpenKeyExW`·`RegQueryValueExW`·`RegEnumKeyExW`·`RegEnumValueW`·`GetFileAttributesW`·`FindFirstFileW` → NT 함수 이름** (위 대응표의 `[추측]` 확정). sast/84 의 kernelbase 작업이 끝난 뒤.
3. **mscoree 옛 길 (상태 3)**: `GetInstallation` / `FindVersionedRuntime` / `GetInstallRoot` / `ComputeVersionString` 본문 — 덤프에 mscoreei 가 없을 때 에뮬레이터가 어디서 어떤 HRESULT 로 끝나는지 정확히.
4. **동적 (Q104 계통)**: VM 에서 `ScriptRunner.exe` (AnyCPU) 와 `tzsync.exe` (x64 IL-only) 를 켜서 위 "동적" 목록 + 파일·레지스트리 열기 목록 → "덤프에 더할 파일" 확정.
5. mscoree 의 `DllMain` (rva `0x9780`) 이 하는 일 (14번에서 불림). 지금은 `[추측]`.

## 판정

**분석성공** — 요청 범위 (ntdll 로더의 CLR 갈래, mscoree 의 런타임 고르기 첫 단계, 머리 가르기) 는 근거 줄과 함께 다 답했다. 단 Q3 은 덤프에 파일이 없어 이름 수준 `[추측]`, Q2 의 syscall 이름은 API 수준 `[추측]`, mscoree export 에서 스크립트 오류 하나 (위 "스크립트 문제", 결과 사용에는 영향 없음).

### 근거 파일 요약 (모두 `ms/` 아래, 커밋 안 됨)

- ntdll: `disassemble/ntdll/{LdrpInitializeProcess,LdrpCorInitialize,LdrpCorFixupImage,LdrpCorValidateImage,LdrpCorProcessImports,LdrpCompleteMapModule,LdrpProcessMappedModule,LdrpProcessMachineMismatch,LdrpRelocateImage,LdrpInitializeThread,_LdrpInitialize,LdrpInitializeKernel32Functions}.asm`, `_calls.tsv`, `_refs/{LdrpCorExeMainRoutine,UseCOR,RtlImageDirectoryEntryToData}.tsv`
- mscoree: `disassemble/mscoree/*.asm` (위 Q2), `_calls.tsv`, `_map/_CorExeMain_{edges,nodes}.tsv`·`_summary.txt`, `decomp/mscoree/_index.tsv`, `ms/sym/mscoree.dll.{pdb,aliases.tsv,pdb.info.txt}`, `ms/ghidra/mscoree.*`
- 로그: `ms/ghidra/ntdll.export.sast86a.stdout`, `ntdll.refs.sast86_*.stdout`, `mscoree.analyze.stdout`, `mscoree.export.sast86{a,b,c,d}.stdout`, `mscoree.map.sast86.stdout`

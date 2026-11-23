# 2026-11-13 run89 — .NET exe 시작 길: ntdll CLR 헤더 갈래 (`LdrpInitializeProcess` `UseCOR` → `LdrpCorFixupImage` · `LdrpCorInitialize` · `LdrpCorValidateImage` · 맡긴 문맥 `Rcx` ← `_CorExeMain` · `LdrpCorProcessImports`) → mscoree `_CorExeMain` → `ShellShim__CorExeMain` → `InitShimImpl` (Q152, PE_emulator 요청 30 — 한 바퀴로 끝): **.NET Framework exe 는 ntdll 이 PE 머리 두 칸 (Magic · 데이터 디렉터리 14) 만 보고 `UseCOR` 를 세운다. 그러면 ntdll 이 `MSCOREE.DLL` 을 System32 에서 직접 올리고 (exe import 로 올리는 게 아님), 커널이 맡긴 시작 문맥의 `Rcx` (= exe 진입점) 를 `mscoree!_CorExeMain` 으로 덮는다 — exe `AddressOfEntryPoint` 는 안 쓴다 (x64 IL-only exe 는 그 값이 0 이고 import 표도 없다). IL-only 면 exe import 표도 안 걷는다. mscoree 는 환경변수 · `HKLM\Software\Microsoft\.NETFramework` (`InstallRoot` · `Policy\`) 로 `<InstallRoot>\<버전>\mscoreei.dll` 을 찾아 `LoadLibraryExW(…, 8)` 한다 — 우리 덤프엔 그 폴더가 없어 "못 찾음" (상태 3) → 옛 길 `GetInstallation` → `ExitProcess`. 판단: 1단계 (ntdll 갈래 + mscoree `InitShimImpl`) 는 할 만하고, 2단계 (mscoreei → clr) 는 덤프에 Framework 폴더가 없어 지금은 아님. test.exe 는 이 갈래를 안 탄다 (`UseCOR` 0 `[관찰]` run44, 디렉터리 14 없음 `[정적+샘플]`).** ntdll `LdrpInitializeProcess +0x334` `UseCOR` = 0 → (PE32) `+0x611` = 1 → `+0x627/+0x62c LdrpCorFixupImage` / (디렉터리 14) `+0x662/+0x667 RtlImageDirectoryEntryToData(…, 14)` → `+0x66c` = 1 · `+0x67f` IL-only → `+0x13e0` · `+0x13ec` 항목 `CorImage` · `CorILOnly` → `+0x184d/+0x1852 LdrpCorInitialize` (`+0x106 LdrpBuildSystem32FileName` → `+0x16a LdrpLoadDll` → `+0x19a LdrpGetProcedureAddress("_CorExeMain")` → `+0x1bb LdrpCorExeMainRoutine`) → `+0x1867/+0x186c LdrpCorValidateImage` → **`+0x18b4 MOV [CONTEXT+0x80],_CorExeMain`** → `+0x193c/+0x1941 LdrpInitializeGraphRecurse` (mscoree) → `+0x1a73` 갈림 → `+0x1a7f/+0x1a84 LdrpCorProcessImports` → … `NtContinue` → `RtlUserThreadStart(rcx)` → `mscoree!_CorExeMain +0x6 ShellShim__CorExeMain +0xa3 InitShimImpl` (`+0x31 FindLatestVersion` · `+0x6f GetShimDllPathName` · `+0x9b FindFirstFileW` · `+0xe7 LoadLibraryShim → LoadLibraryWrapper +0x16 LoadLibraryExW`) ⇒ `mscoreei!_CorExeMain` (상태 2) / ⇒ `_CorExeMain` (rva `0x2f6e8`) `+0x18 GetInstallation` → `+0x55 ExitProcess` (상태 3)

> **고쳐 읽기 (run139, sast/135 — Q253a)**: 0 단계 `PspSelectMachineForProcess` 의 "하는 일 `[추측]`" 은 이제 `[정적]` — 이미지 기계 `0x14c` 이고 `ImageFlags & 3 ≠ 3` 이면 `PsWow64IsMachineSupported` 로 WoW64 (출력 `{0x14c, 1, 0}`), `ImageFlags & 3 == 3` (`.NET`) 이면 `Prefer32bit` · 요청/부모 기계 · `PspQueryComPlusRunUnderWoW` 로 가르고 아니면 **64비트 + `OverrideAddressSpace`**. "결과서와 다르게 적은 곳 · 보탠 것" ③ 의 "커널이 CLR 헤더 없는 PE32 를 64비트로 안 띄운다 `[추측]`" 은 **절반 닫힘**: 커널 고르기는 그 두 비트가 켜졌을 때만 64비트로 돌린다 `[정적]`, CLR 헤더 없는 PE32 에서 두 비트가 꺼져 있는지 (`ImageFlags` 채우는 MM 줄) 는 안 봄 `[공백]`. → `runs/03_sprint/run139-wow64-kernel.md`

## PE_emulator 쪽에서 쓸 것

### 한눈에: 무엇이 필요한가 (0 ~ 10 단계)

IL-only 꼴 .NET Framework exe (x64 IL-only, 또는 AnyCPU = PE32 IL-only) 를 켠다고 칠 때 차례대로. "덤프" 열 = 지금 `ms/bin` (= PE_emulator 덤프와 같은 꼴) 에 있나. 줄은 모두 결과서 + 오케스트레이터 · flow-writer 가 `.asm` 으로 다시 봄.

| # | 누가 | 하는 일 | 필요한 것 | 덤프 | 근거 줄 | 표시 |
|---|---|---|---|---|---|---|
| 0 | 커널 | 이 exe 를 어느 기계 (x64 / WoW64) 로 띄울지 정함. .NET 정보를 묻는 갈래가 있음 | `PspAllocateProcess +0xa87 / +0xa8c PspSelectMachineForProcess` → (떨어진 조각) `+0x10a860 / +0x10a865 PspQueryComPlusRunUnderWoW` | 코드 | `ms/disassemble/ntoskrnl/_calls.tsv` 줄만, 본문 안 읽음 | 호출 줄 `[정적]`, 하는 일 `[추측]` |
| 1 | ntdll `LdrpInitializeProcess` | exe 머리를 보고 `UseCOR` · IL-only 표시를 세움 | exe PE 머리 (Magic, 디렉터리 14, CLR 헤더 `Flags`) | 코드 | `+0x334` · `+0x602 ~ +0x611` · `+0x662 / +0x667` · `+0x66c ~ +0x67f` | `[정적]` |
| 2 | ntdll `LdrpCorFixupImage` | (PE32 AnyCPU 만) **메모리 속 머리를 PE32 → PE32+ 로 고침** | CLR `Flags` 비트 1 (32BITREQUIRED) 꺼짐, 첫 쪽 보호 바꾸기 (`NtProtectVirtualMemory` 두 번) | 코드 | `+0x627 / +0x62c`, 안 `+0xcc` · `+0x135` · `+0x144` | `[정적]` |
| 3 | ntdll `LdrpCorInitialize` | **`MSCOREE.DLL` 을 ntdll 이 직접 올림** + `_CorExeMain` 주소를 KUSD 쿠키로 섞어 둠 | 보통 `C:\Windows\System32\MSCOREE.DLL`. 환경변수 `COMPLUS_InstallRoot` · `COMPLUS_Version` **둘 다** 있으면 이름만으로 | **있음** | `+0x184d / +0x1852`, 안 `+0x93` · `+0xc0` · `+0x106` · `+0x16a` · `+0x19a` · `+0x1bb` | `[정적]` |
| 4 | ntdll `LdrpCorValidateImage` | (IL-only 만) TLS 디렉터리가 있으면 `0xc000007b` | exe 디렉터리 9 | 코드 | `+0x1867 / +0x186c`, 안 `+0x11` | `[정적]` |
| 5 | ntdll `LdrpInitializeProcess` | (첫 스레드면) **맡긴 시작 문맥 `Rcx` ← `_CorExeMain`** | 3 에서 섞어 둔 값, KUSD `Cookie` | 코드 | `+0x1883` (`SameTebFlags & 0x400`) · `+0x189d` · `+0x18b4 MOV [RBX+0x80],RAX` | `[정적]` |
| 6 | ntdll | mscoree (와 의존) 켜기 · IL-only 면 exe import 안 걷기 | mscoree import = `KERNEL32.dll` 하나 (+ 지연 6개) | 있음 | `+0x193c / +0x1941 LdrpInitializeGraphRecurse` · `+0x1a73 / +0x1a76` · `+0x1a7f / +0x1a84 LdrpCorProcessImports` | `[정적]` (mscoree `DllMain` 본문은 `[추측]`) |
| 7 | 첫 스레드 | `NtContinue` → `RtlUserThreadStart(rcx = _CorExeMain)` → `BaseThreadInitThunk` → `mscoree!_CorExeMain` | — | — | flow.md 2.10 + 5 번 | `[정적]` |
| 8 | mscoree `InitShimImpl` | 런타임 폴더 · 버전 고르기 | 환경변수 `COMPlus_Version` · `COMPlus_InstallRoot` · `COMPlus_DefaultVersion`, 레지스트리 `HKLM\Software\Microsoft\.NETFramework` 값 `InstallRoot` · `…\Policy\` 하위 키 · 값, `mscoree.dll.local` 유무 | 레지스트리 값은 덤프 밖 | `_CorExeMain +0x6` → `ShellShim__CorExeMain +0xa3` → `InitShimImpl +0x31` · `+0x6f` · `+0x9b` | `[정적]` |
| 9 | mscoree | `<InstallRoot>\<버전>\mscoreei.dll` 이 있나 보고 `LoadLibraryExW(경로, NULL, 8)` | **`mscoreei.dll`** | **없음** | `InitShimImpl +0xe7` → `LoadLibraryShim +0x31` → `LoadLibraryWrapper +0x16` | `[정적]` |
| 10 | mscoreei → clr | 진짜 런타임 시작 | `clr.dll` 외 | **없음** | 이름만 | `[추측]` |

- **mscoreei 가 없으면 (지금 덤프)**: `InitShimImpl` 이 상태 3 ("못 찾음") 을 적고 (`+0x309 ~ +0x315 / +0x31a UpdateShimImplStatus(3, "mscoreei.dll")`), `ShellShim__CorExeMain +0x3a` → `+0xb2` 가 `_CorExeMain` 의 **둘째 몸** (rva `0x2f6e8`) 으로 꼬리 점프 → `+0x18 GetInstallation` (옛 고르기 길) 실패면 `+0x55 ExitProcess(그 HRESULT)` (ret `+0x5c`). 어떤 HRESULT 인지, 메시지 상자가 뜨는지는 `[공백]` (Q166 ③, Q167).
- 덤프에 더해야 하는 건 **9 · 10** (+ 레지스트리 값). 0 ~ 8 은 지금 덤프로 된다.

### 할 만한가 (한 줄)

**1단계 (ntdll CLR 갈래 + mscoree `InitShimImpl` 까지) 는 할 만하다. 2단계 (mscoreei → clr 로 진짜 관리 코드 시작) 는 덤프에 .NET Framework 폴더가 없어서 지금은 아니다.**

- 1단계가 작은 까닭 `[정적]`:
  - 엔진 (로더 흉내) 이 할 일은 `LdrpInitializeProcess` 안 갈래 자리 열 (판정 `+0x602` · `+0x662`, 항목 비트 `+0x13e0` · `+0x13ec`, 재배치 건너뛰기 `+0x141f`, mscoree 올림 `+0x184d`, 검증 `+0x1867`, `Rcx` 덮기 `+0x18b4`, 그래프 `+0x193c`, import 갈림 `+0x1a73`) + 작은 함수 넷 (`LdrpCorFixupImage` 388B · `LdrpCorValidateImage` 37B · `LdrpCorInitialize` · `LdrpCorProcessImports` 97B).
  - 은퇴 뒤 mscoree 는 **진짜 코드** 그대로 돈다 (덤프에 `System32\mscoree.dll` · `MUI\0409\mscorees.dll` 있음). 엔진은 레지스트리 · 파일 묻기에 **대답만** 하면 된다 (syscall 쪽).
  - 1단계만 해도 .NET exe 가 **"런타임 못 찾음" 으로 정상적으로 끝나는 데까지** 간다 (위 "mscoreei 가 없으면").
- 2단계가 지금 아닌 까닭: 덤프에 Framework 폴더를 통째로 더해야 하고, 정확한 파일 목록은 VM 에서만 정해지며 (Q167), CLR 안쪽이 부르는 syscall 양은 이 요청 범위 밖이라 비용을 못 쟀다. 대상 샘플은 전체의 2% (310개).

### 덤프에 더할 파일

| 순 | 파일 / 값 | 확실도 | 근거 |
|---|---|---|---|
| 1 | `Windows\Microsoft.NET\Framework64\v4.0.30319\mscoreei.dll` | 이름 · 경로 짜는 법은 `[정적]`, 폴더 글자는 `[추측]` | 경로 = `<InstallRoot>` + `<버전>` + `\mscoreei.dll` (`GetShimDllPathName`, 이름 글자 `InitShimImpl +0x68 LEA` → `0x18004e988` = `mscoreei.dll`, flow-writer 가 파일 바이트로 다시 읽음). 폴더 글자는 레지스트리 값이 정함 |
| 2 | 레지스트리: `HKLM\Software\Microsoft\.NETFramework` 값 `InstallRoot` (문자열), `HKLM\Software\Microsoft\.NETFramework\Policy\` 하위 키 · 값 (예: `v4.0` 키 아래 `30319`) | 키 · 값 **이름** `[정적]`, **실제 값** `[확인 필요]` | `GetShimDllPathName +0x13f / +0x146` (열기) · `+0x176 / +0x17d` (값) · `+0x1e0` (닫기), `FindLatestVersionWithCallback +0xdd / +0xe4` · `+0x3cf / +0x3d6` · `+0x3fd / +0x402 FindLatestBuild`. 값은 VM 에서만 (Q167) |
| 3 | `…\v4.0.30319\clr.dll` | `[추측]` (mscoreei 안쪽) | mscoree 가 내보내는 `LoadLibraryShim` 이 같은 경로 규칙을 씀 (`LoadLibraryShim__23ee0.asm`) |
| 4 | `…\v4.0.30319\clrjit.dll`, `mscorlib.dll` (+ NGen 이미지), `clrcompression.dll`, `Config\machine.config` | `[추측]` | 관리 코드를 실제로 돌릴 때 필요할 만한 것 |
| 5 | 돌려 볼 관리 코드 exe 가 쓰는 어셈블리 (`System.dll` 등) | `[추측]` | exe 마다 다름 |
| — | 이미 있음: `System32\mscoree.dll`, `System32\MUI\0409\mscorees.dll`, `SysWOW64\mscoree.dll` | 파일 있음 | |
| — | 이미 있음: 시험용 관리 코드 exe 11개 (아래 표) | 파일 있음 | `ms/bin` |

- 확정하려면 VM 에서 .NET exe (`ScriptRunner.exe` AnyCPU · `tzsync.exe` x64 IL-only) 를 한 번 돌려 파일 열기 목록과 `InstallRoot` 값을 뽑아야 한다 (Q167).

### Framework exe 와 .NET Core 앱호스트를 PE 머리로 가르는 법

로더가 판정에 보는 칸은 **Magic 과 데이터 디렉터리 14 뿐**이다 (`LdrpInitializeProcess +0x602` · `+0x662`). import 표는 판정에 안 쓴다.

가르는 차례 (이 저장소 코드 기준):
1. Magic == `0x10b` (PE32) 이고 이 프로세스가 WoW64 가 아님 → **Framework AnyCPU** (`UseCOR`, `LdrpCorFixupImage`).
2. 디렉터리 14 가 있음 → **Framework x64**. CLR 헤더 `Flags` 비트 0 (ILONLY) 이 서면 IL-only, 꺼지면 혼합 모드 (C++/CLI).
3. 둘 다 아님 → **네이티브** (.NET Core / 5+ 앱호스트 포함 — 로더 입장에선 그냥 네이티브 exe).

| 종류 | 머리 모양 | 덤프 예 (flow-writer 가 다시 셈한 것은 굵게) |
|---|---|---|
| x64 IL-only | PE32+, Machine `0x8664`, 디렉터리 14 (크기 72), CLR `Flags = 0x9`, **`AddressOfEntryPoint = 0`, import 디렉터리 비어 있음** | **`tzsync.exe`** (`System32`), `AppVStreamingUX.exe` (`System32\AppV\`), `powershell_ise.exe` (x64) |
| AnyCPU IL-only | PE32, Machine `0x14c`, 디렉터리 14, `Flags = 0x1` 또는 `0x9` (32BITREQUIRED 꺼짐), import 는 `mscoree.dll!_CorExeMain` 하나, 진입점 = 그 import 로 뛰는 `FF 25` 껍데기 | **`ScriptRunner.exe`** (AEP `0x47fa`, `Flags 0x9`), `UevAppMonitor.exe`, `Microsoft.Uev.SyncController.exe` |
| x86 전용 IL-only | PE32, `0x14c`, `Flags` 에 32BITREQUIRED (`0x3` · `0xb`) | `UevTemplate*Generator.exe` 셋, `powershell_ise.exe` (x86) |
| 혼합 모드 (C++/CLI) | PE32+, 디렉터리 14, `Flags = 0x8` (ILONLY 꺼짐), 평범한 import 표 + `mscoree.dll!_CorExeMain` · `CorBindToRuntimeEx` | **`FileHistory.exe`** (AEP `0x80f6`, import 6개 DLL) |
| .NET Core / 5+ 앱호스트 | 네이티브 PE32+, **디렉터리 14 없음**, mscoree import 없음 | 덤프에 샘플 없음 `[추측: 일반 지식]` |

- 덤프의 관리 코드 exe 11개 모두 CLR 헤더 `cb = 72`, 런타임 버전 `2.5`, 메타데이터 버전 글자 `v4.0.30319` (결과서, 바이트 읽음).
- **주의: "mscoree import 가 있나" 로 가르면 틀린다** — x64 IL-only exe 는 import 표가 아예 없다 (`tzsync.exe` 디렉터리 1 = `(0, 0)`, flow-writer 다시 셈).
- 앱호스트 표식 (`hostfxr.dll` 찾기, 응용 dll 경로 자리, 단일 파일 번들 끝 묶음) 은 덤프에 샘플이 없어 코드로 못 봤다 `[추측: 일반 지식]`.
- 참고 `[정적+샘플]`: sample 셋 (`test.exe` · `dll1.dll` · `dll2.dll`) 은 모두 PE32+ · 디렉터리 14 없음 (flow-writer `pefile`) → 1 · 2 둘 다 아니오.

### 우리 지도에서 어디서 갈라지나 (flow.md 2.4 ~ 2.6 · 2.10, test.exe 길)

```
LdrpInitializeProcess (flow.md 2.4)                                     test.exe 는 ─ 쪽, .NET 은 ═ 쪽
 │  앞쪽 묶음 3
 │  +0x334  UseCOR = 0 · IL-only 표시 [RSP+0x58] = 0          ← test.exe 도 지남. 끝값 0 [관찰] run44
 │  앞쪽 묶음 5
 │  +0x5f2  UseWOW64 = (TEB.WowTebOffset > 0)
 │  +0x602  Magic == 0x10b 이고 WowTebOffset ≤ 0 ?
 │     ═ 예 → +0x611 UseCOR = 1 · IL-only = 1 → +0x62c LdrpCorFixupImage           (AnyCPU)
 │  +0x667  RtlImageDirectoryEntryToData(exe, TRUE, 14)  (UseWOW64 0 · UseCOR 0 일 때만)
 │     ═ NULL 아님 → +0x66c UseCOR = 1 · +0x67f IL-only = (Flags & 1)                (x64 / 혼합)
 │     ─ test.exe: Magic 0x20b, 디렉터리 14 없음 → 둘 다 아니오 [정적+샘플]
 │  뒤1 (exe 모듈 항목)
 │  +0x13e0 · +0x13ec  항목 Flags 비트 22 CorImage · 비트 24 CorILOnly
 │  +0x141f  IL-only 면 LdrpProtectAndRelocateImage (+0x1435) 건너뜀
 │  +0x14ee  LdrpProcessMappedModule — IL-only 면 EntryPoint (+0x38) 안 적음
 │  뒤4
 │  +0x183c  UseCOR ?
 │     ═ 예 → +0x1852 LdrpCorInitialize (MSCOREE.DLL 올림)  → +0x186c LdrpCorValidateImage (IL-only)
 │            → +0x18b4 맡긴 CONTEXT.Rcx ← _CorExeMain (첫 스레드)
 │  +0x18d0  LdrpInitializeTls
 │  +0x1941  LdrpInitializeGraphRecurse (mscoree 노드 — [RSP+0x178] ≠ 0 일 때만)
 │  뒤5  +0x19a2 LdrpInitializeKernel32Functions (2.5)   ← .NET 이면 kernel32 가 이미 올라와 있을 것 [추측]
 │  뒤6  +0x1a73 IL-only ?
 │     ═ 예 → +0x1a84 LdrpCorProcessImports → +0x1a87 JMP +0x1af2  (import · 일감 · 실행 준비 다 건너뜀)
 │     ─ 아니오 → +0x1aa9 LdrpMapAndSnapDependency → +0x1ab1 LdrpDrainWorkQueue → +0x1af2 Prepare   (2.6 — test.exe · 혼합 모드)
 └─ (은퇴, 2.10) NtContinue → RtlUserThreadStart(rcx)    rcx = exe 진입점 (test.exe) / _CorExeMain (.NET)
```

- **test.exe 는 ═ 쪽을 하나도 안 탄다**: `UseCOR` 는 `+0x334` 에서 0 을 받고 그대로 끝난다 — 사진 P1a · P1b · P2 `UseCOR` 바이트 0 (`ms/logs/04_baggage-read_P1a.txt:5132` · `_P1b.txt:6250` · `_P2.txt:6262`, `+0x92c` 바이트), `LdrpCorExeMainRoutine` 0 (`_P1a.txt:5081` · `_P1b.txt:6199` · `_P2.txt:6211`) `[관찰]`. 모듈 6개의 `CorImage` · `CorILOnly` 도 모두 `0y0` (`_P2.txt:3423` · `3425` 등) `[관찰]`.
- `[추측]` **.NET exe 는 kernel32 · kernelbase 가 더 일찍 올라온다**: mscoree 의 import 는 `KERNEL32.dll` 하나 `[정적: PE 표]` 이고, `LdrpLoadDll` 은 의존 DLL 을 같이 올린다 (run20 ~ run26). 그래서 `+0x184d` (mscoree) 때 kernel32 · kernelbase 가 함께 올라오고, `+0x193c` 에서 kernelbase → kernel32 → mscoree 차례로 켜질 것이다 — test.exe 길 (`+0x19a2` 에서 처음) 보다 앞. 확인은 동적 (Q167 7).
- 혼합 모드 (C++/CLI) 는 `+0x1a73` 에서 test.exe 와 같은 길 (import 걷기) 로 간다 — 다만 그 앞에 mscoree 가 이미 올라와 있고 `Rcx` 도 이미 덮여 있다.

### 원문 질문이 어디서 답해졌나

| 원문 요청 30 | 답한 곳 | 한 줄 · 지금 PE_emulator |
|---|---|---|
| 1 ntdll 로더 갈래 — IL-only 검사, 진입점을 `_CorExeMain` 으로 바꾸는 자리, mscoree 를 언제 · 어떻게 (import 냐 로더냐), 함수 이름 · 차례 | 위 "한눈에" 1 ~ 7, 아래 "결과" ntdll 표 | **로더가 따로** 올린다 (`+0x184d LdrpCorInitialize`), 진입점은 **맡긴 문맥 `Rcx` 를 덮는** 꼴 (`+0x18b4`). 함수 넷 (`LdrpCorFixupImage` · `LdrpCorInitialize` · `LdrpCorValidateImage` · `LdrpCorProcessImports`). 지금 PE_emulator: **빠짐 → 추가 필요** (작음) |
| 2 `_CorExeMain` → 런타임 고르기 — 레지스트리 · 파일 · syscall | "한눈에" 8 · 9, 아래 mscoree 표 | 레지스트리 키 · 값 이름 · 파일 이름 · API 는 `[정적]`, API → NT 함수는 `[추측]` (Q166 ②). 지금: **없음 → 진짜 mscoree 를 돌리고 대답만 맞추면 됨** |
| 3 `clr.dll` 시작까지 큰 줄기 | "한눈에" 10, "덤프에 더할 파일" | 덤프에 파일이 없어 mscoree 경계까지만 `[정적]`, 그 뒤 `[추측]`. 지금: **보류가 맞음** |
| 4 Core / 5+ 와 Framework 를 머리에서 가르기 | "PE 머리로 가르는 법" | Framework 쪽 `[정적]`, 앱호스트 표식 `[추측]`. 지금: 머리 읽기라 **쉬움** — 단 "진입점 0 · import 없음" exe 를 PE_emulator 의 PE 읽는 쪽이 통과시키는지 확인 필요 (이 저장소 밖 `[공백]`) |
| (돌려받는 꼴) 덤프에 더할 파일 · 할 만한가 | 위 두 절 | — |
| (지금 우리) 로더에 CLR 갈래 없음 `[추측]` | "한눈에" | 맞다면 1 · 2 · 4 · 5 · 6 이 빠짐 |

### PE_emulator 확인거리 (우리 대기열 아님)

- 엔진 KUSD 의 `ImageNumberLow` · `ImageNumberHigh` (`+0x2c` · `+0x2e`) 가 진짜와 같은 **`0x8664` · `0x8664`** 인가 `[관찰]` run44. `LdrpCorFixupImage` 가 고치지 않는 길 (`+0x15b`) 에서 이 범위로 Machine 을 본다 — 범위가 다르면 결과가 달라진다.
- `Rcx` 를 섞어 두는 쿠키는 KUSD `Cookie` (`+0x330`) 다 (프로세스 쿠키 `RtlEncodePointer` 와 다름). 넣는 쪽 · 푸는 쪽이 모두 ntdll 안이라 값은 아무거나 되지만, 그 사이에 바뀌면 안 된다 `[정적]`.
- 엔진의 PE 읽는 쪽이 `AddressOfEntryPoint = 0` · import 디렉터리 `(0, 0)` 인 exe 를 거르지 않는지, 커널 흉내가 그런 exe 의 시작 `Rcx` 로 무엇을 넣는지 (진짜 커널 쪽은 Q166 ⑥).

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 사진 값은 run44 덤프 읽기 로그를 다시 읽었다 (flow-writer).
- 정적:
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` — 다시 analyze 안 함. depth 0 셋 새로 (`LdrpCorValidateImage` · `LdrpCorFixupImage` · `LdrpCorInitialize`), 있던 `.asm` 읽기 (`LdrpInitializeProcess` · `LdrpCorProcessImports` · `LdrpCompleteMapModule` · `LdrpProcessMappedModule` · `LdrpProcessMachineMismatch` · `LdrpRelocateImage` · `LdrpInitializeThread` · `_LdrpInitialize` · `LdrpInitializeKernel32Functions`), refs 셋 (`_refs/{LdrpCorExeMainRoutine,UseCOR,RtlImageDirectoryEntryToData}.tsv`).
  - **mscoree pdb `C5C034E4B3EDB32A23F05CE2A83C18431`** — **처음 analyze** (`System32\mscoree.dll` x64, 함수 1334, `_index.tsv` 있음, 별칭 표 `ms/sym/mscoree.dll.aliases.tsv`). 지도 `ms/disassemble/mscoree/_map/_CorExeMain_*` (314 노드) + export 네 번.
- 버전 대조 (결과서): ntdll `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` (`_calls.tsv`), `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess` (`.asm`). mscoree 는 처음이라 PE 내보내기 표와 Ghidra 함수 시작을 맞춤 — `_CorExeMain` `0x7850` · `LoadLibraryShim` `0x23ee0` · `CLRCreateInstance` `0x13d0` · `_CorExeMain2` `0x26d40` 다 같음.

## 목적

- PE_emulator 요청 30 (.NET exe 시작 길, 원문 급함 낮음 — 요청 26 ~ 30 중 5순위) 의 첫 답 = **"무엇이 필요한가" 지도** (사용자 결정 2026-11-12). 할 만한지 판단용.
- ntdll 로더의 CLR 헤더 갈래 (판정 · mscoree 올림 · 진입점 바꾸기 · import 건너뛰기), mscoree 의 런타임 고르기 첫 단계, clr 까지 큰 줄기, Framework / Core 머리 가르기, 덤프에 더할 파일.

## 방법

- 정적: `_workspace/sast/86_dotnet-start.md` — ntdll `^(LdrpCorValidateImage|LdrpCorFixupImage|LdrpCorInitialize)$` depth 0 + 있던 `.asm` + refs 셋, mscoree analyze → 지도 (`_CorExeMain` 뿌리) → export 네 번 (깊이 0 ~ 1).
- 결과서: `_workspace/sast/86_dotnet-start_result.md` (**분석성공** — Q3 은 덤프에 파일이 없어 이름 수준 `[추측]`, Q2 syscall 이름은 API 수준 `[추측]`).
- 오케스트레이터가 `.asm` 으로 맞춘 줄 (결정 기록): `UseCOR` 면 `LdrpCorInitialize` 로 mscoree 를 직접 올리고 `+0x18b4` 에서 `Rcx` 를 덮음, IL-only 면 import 안 걸음, x64 IL-only exe 는 AEP 0 · import 없음.
- flow-writer 가 다시 본 것:
  - ntdll `.asm`: `LdrpInitializeProcess +0x32c ~ +0x345` · `+0x5d9 ~ +0x6f7` · `+0xa7b` · `+0x13d2 ~ +0x143b` · `+0x14e0 ~ +0x14ee` · `+0x1834 ~ +0x18d8` · `+0x191d ~ +0x1945` · `+0x1a47 ~ +0x1b02` (R12 이 `+0x13d2` ~ `+0x1a73` 사이 다시 안 써짐도), `LdrpCorInitialize` 전부 (`+0x0 ~ +0x1e8`), `LdrpCorFixupImage +0x0 ~ +0x6d` · `+0xa3 ~ +0x184`, `LdrpCorValidateImage` 전부, `LdrpCorProcessImports` 전부, `LdrpCompleteMapModule +0x7c ~ +0x9f` · `+0x109 ~ +0x14f` · `+0x255 ~ +0x2af`, `LdrpProcessMappedModule +0x45 ~ +0x83` · `+0x233 ~ +0x237`, `LdrpInitializeThread +0x3c ~ +0x50` · `+0x41a ~ +0x45f`, `_calls.tsv` 의 `LdrpCor*` 부르는 곳 7줄, `_refs` 둘.
  - mscoree `.asm`: `_CorExeMain` 전부 · `_CorExeMain__2f6e8` call 줄 · `ShellShim__CorExeMain` 전부 · `InitShimImpl` call 줄 · `+0x2e7 ~ +0x324` · `FindLatestVersion` · `GetShimDllPathName` · `LoadLibraryShim` · `LoadLibraryWrapper` · `CheckShimImplCallback` · `EnvGetString` call 줄, `FindLatestVersionWithCallback +0xdd` · `+0x155` · `+0x1e7` · `+0x3cf` · `+0x3fd` · `+0x712 ~ +0x719`. 글자 상수 (`mscoreei.dll` · `GetProcAddress` 이름 여섯 · `DefaultVersion`) 는 `mscoree.dll` 파일 바이트로 다시 읽음.
  - ntoskrnl `_calls.tsv`: `PspAllocateProcess +0xa87` · `PspSelectMachineForProcess +0x10a860` · `ExpQuerySystemInformation → ExpReadComPlusPackage`.
  - PE 머리 (`pefile`, scratchpad 임시 스크립트): `tzsync.exe` · `ScriptRunner.exe` · `FileHistory.exe` · `mscoree.dll` · sample 셋.
  - 이름: `pdb_aliases.py --lookup ntdll.dll 0x166500` (복사 — 같은 주소 이름 2개 `memcpy` · `memmove`). mscoree 쪽 `alias:…+1` 은 모두 **같은 함수의 C++ 꾸민 이름 하나** (`?InitShimImpl@@YAXXZ` 등) 라 이름 그대로 쓴다.
  - 사진 값 (run44 덤프 읽기 로그): ntdll `.data` 의 `UseCOR` · `LdrpCorExeMainRoutine` 줄, 모듈 항목 `CorImage` · `CorILOnly`, KUSD `ImageNumberLow` · `ImageNumberHigh` · `ComPlusPackage` · `Cookie` (아래 "사진 대조").

## 결과

### 정적 — 호출 지점 (ntdll, `ms/disassemble/ntdll/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | (직접) | — | 전역 `UseCOR` (rva `0x1d292c`, 1B) | `+0x334 MOV [UseCOR],R14B` (= 0) · `+0x33b` · `+0x33e` IL-only 표시 `[RSP+0x58]` = 0. 쓰는 곳은 셋뿐 (`_refs/UseCOR.tsv` — `+0x334` · `+0x611` · `+0x66c`) | [정적] |
| 〃 | `+0x627` | `+0x62c` | `LdrpCorFixupImage` (rva `0x109374`) | **PE32 갈래**: `+0x602 CMP [R15+0x18],0x10b` (Magic) · `+0x60d TEST ECX` (`WowTebOffset`, `+0x5e3`) · `+0x60f JG` → `+0x611` `UseCOR` = 1 · `+0x618` · `+0x61b` IL-only = 1. 음수면 `+0x630 JMP` → 실패 출구 `+0xa7b` | [정적] (flow-writer) |
| 〃 | `+0x662` | `+0x667` | `RtlImageDirectoryEntryToData(base, TRUE, 14)` | `+0x63b UseWOW64 == 0` · `+0x644 UseCOR == 0` 일 때만. NULL 아니면 `+0x66c` `UseCOR` = 1 · `+0x673 TEST [RAX+0x10],R15B` (`Flags & 1`) · `+0x67b CMOVNZ` · `+0x67f` IL-only 저장 | [정적] (flow-writer) |
| 〃 | `+0x6ef` | `+0x6f4` | `LdrImageDirectoryEntryToLoadConfigEx` | **안 부름**: WoW64 (`+0x6d5`) 이거나, `UseCOR` 이고 PE32 갈래로 왔을 때 (`+0x6de` · `+0x6e7 TEST EBX` — `EBX` = `+0x620` 의 1) | [정적] |
| 〃 | (직접) | — | exe 항목 `Flags` (`+0x68`) | `+0x13d2 MOV R12D,[RSP+0x58]` (IL-only 다시 실음) · `+0x13d7 CMP UseCOR` → `+0x13e0 BTS EAX,0x16` · `+0x13e4` 저장 (`CorImage`) · `+0x13e7 TEST R12B` → `+0x13ec BTS EAX,0x18` · `+0x13f0` 저장 (`CorILOnly`) | [정적] (flow-writer) |
| 〃 | `+0x1430` | `+0x1435` | `LdrpProtectAndRelocateImage` | `+0x141f TEST R12B` · `+0x1422 JNZ` — IL-only 면 건너뜀 (누가 대신 고치나 `[추측: CLR]`, Q166 ⑦) | [정적] |
| 〃 | `+0x14e9` | `+0x14ee` | `LdrpProcessMappedModule` | 안 `+0x54 AND ECX,0x1000004` · `+0x5a CMP ECX,4` · `+0x5d JNZ` — `ImageDll` 만 켜지고 `CorILOnly` 꺼졌을 때만 `+0x78` `EntryPoint` 를 적음. AEP 0 이면 `+0x6c JZ` → `+0x235` 0 → `+0x78` 0 을 적고 지나감 | [정적] (flow-writer) |
| 〃 | **`+0x184d`** | **`+0x1852`** | **`LdrpCorInitialize`** (rva `0x8a808`) | `+0x183c CMP UseCOR` · `+0x1843 JZ`. 인자 `&[RSP+0x178]` (mscoree 항목 받을 칸). `+0x1856 TEST` · `+0x1858 JS` → 실패 출구 | [정적] (오케스트레이터 · flow-writer) |
| 〃 | `+0x1867` | `+0x186c` | `LdrpCorValidateImage` (rva `0x10e4b4`) | `+0x185e TEST R12B` · `+0x1861 JZ` (IL-only 만). `+0x1872 JS` → 실패 | [정적] |
| 〃 | (직접) | — | 맡긴 `CONTEXT.Rcx` | `+0x1878 ~ +0x188a` `TEST [RBX+0x17ee],0x400` (TEB `SameTebFlags` `InitialThread`) → `+0x188c` KUSD `Cookie` (`[0x7ffe0330]`) · `+0x189d` `LdrpCorExeMainRoutine` 읽기 · `+0x18a4 ROR` · `+0x18a9 XOR` · `+0x18ac RBX ← [RSP+0xe0]` (받은 CONTEXT) · **`+0x18b4 MOV [RBX+0x80],RAX`**. IL-only 와 상관없이 `UseCOR` 면 (혼합 모드도) | [정적] (오케스트레이터 · flow-writer) |
| 〃 | `+0x193c` | `+0x1941` | `LdrpInitializeGraphRecurse` | `+0x191d RCX ← [RSP+0x178]` · `+0x1925 TEST` · `+0x1928 JZ` (mscoree 항목이 있을 때만). 첫 인자 = 그 항목 `+0x98` (노드) | [정적] (flow-writer) |
| 〃 | **`+0x1a7f`** | **`+0x1a84`** | **`LdrpCorProcessImports`** (rva `0xb7e0`, 97B) | **갈림 = `+0x1a73 TEST R12B,R12B` · `+0x1a76 JZ +0x1a89`**. IL-only 면 이 호출 뒤 `+0x1a87 JMP +0x1af2` → `+0x1a89 LdrpThreadTokenSetMainThreadToken` · `+0x1aa4 LdrpMapAndSnapDependency` · `+0x1aac LdrpDrainWorkQueue` · `+0x1abd LdrpThreadTokenUnsetMainThreadToken` · `+0x1aed LdrpPrepareModuleForExecution` 를 **다 건너뜀** | [정적] (flow-writer) |
| `ntdll!LdrpCorInitialize` | `+0x6e` · `+0xd5` | `+0x73` · `+0xda` | `RtlEnterCriticalSection` / `RtlLeaveCriticalSection` (`FastPebLock`) | 환경변수 읽기 앞뒤 | [정적] |
| 〃 | `+0x93` | `+0x98` | `RtlQueryEnvironmentVariable(NULL, LdrpComPlusInstallRootName, 0x13, NULL, 0, …)` | `+0x98 ~ +0x9f` 돌아온 값 `0xc0000023` (버퍼 작음) = "있다" | [정적] |
| 〃 | `+0xc0` | `+0xc5` | `RtlQueryEnvironmentVariable(…, LdrpComPlusVersionName, 0xf, …)` | 위가 있을 때만. 이것도 있으면 `+0xca CMOVZ` 로 `BL` = 0 (처음 `+0x49 BL = 1`) | [정적] (flow-writer) |
| 〃 | `+0x106` | `+0x10b` | `LdrpBuildSystem32FileName(&버퍼, LdrpMscoreeDllName)` | `+0xf2 TEST BL` · `+0xf4 JZ +0x1de` — 환경변수 **둘 다** 있으면 건너뛰고 이름 `MSCOREE.DLL` 그대로 (`+0x1de LEA RCX,[LdrpMscoreeDllName]`) | [정적] (flow-writer) |
| 〃 | `+0x16a` | `+0x16f` | `LdrpLoadDll(이름, …, 1, &항목)` | `+0x180 JS` → 실패 반환 | [정적] |
| 〃 | `+0x179` | `+0x17e` | `LdrpReleaseDllPath` | | [정적] |
| 〃 | `+0x19a` | `+0x19f` | `LdrpGetProcedureAddress(mscoree DllBase, "_CorExeMain", 0, &주소)` | `+0x1a3 JS` → `+0x1d7` | [정적] |
| 〃 | (직접) | — | 전역 `LdrpCorExeMainRoutine` (rva `0x1d25f0`, 8B) | `+0x1a5` KUSD `Cookie` → `+0x1b3 XOR` · `+0x1b8 ROR (Cookie & 0x3f)` → **`+0x1bb MOV [LdrpCorExeMainRoutine],RDX`** → `+0x1c2 MOV [RSI],RDI` (항목을 호출자 칸에). 쓰는 곳은 이 하나, 읽는 곳 `LdrpInitializeProcess +0x189d` · `LdrpInitializeThread +0x448` (`_refs/LdrpCorExeMainRoutine.tsv`) | [정적] |
| 〃 | `+0x1c8` | `+0x1cd` | `LdrpDereferenceModule` | 성공 길 | [정적] |
| 〃 | `+0x1d7` | `+0x1dc` | `LdrpDecrementModuleLoadCountEx` | `_CorExeMain` 을 못 찾았을 때 | [정적] |
| `ntdll!LdrpCorFixupImage` | `+0x2d` · `+0x42` | `+0x32` · `+0x47` | `RtlImageNtHeaderEx` · `RtlImageDirectoryEntryToData(…, 14)` | 디렉터리 결과를 **NULL 검사 없이** `+0x69 TEST [RAX+0x10],2` 에서 읽음 | [정적] (flow-writer) |
| 〃 | `+0xcc` | `+0xd1` | `NtProtectVirtualMemory(-1, 첫 쪽, PAGE_READWRITE)` | 조건: `+0x4b ~ +0x54` Magic `0x10b` · `+0x5a ~ +0x63` Machine `0x14c` · `+0x69 ~ +0x6d` 32BITREQUIRED 꺼짐. 선택 헤더가 쪽을 넘으면 `+0xad JA` → `0xc000007b` | [정적] |
| 〃 | `+0xe7` | `+0xec` | `memcpy` (같은 주소 이름 2개 `memcpy` · `memmove`) | 선택 헤더 `0x10` 뒤로 밀기 → `+0xf3` `SizeOfOptionalHeader += 0x10` → `+0x106 ~ +0x12d` 8바이트로 넓히기 → **`+0x135 Magic = 0x20b`** | [정적] |
| 〃 | `+0x144` | `+0x149` | `NtProtectVirtualMemory` (되돌림) | | [정적] |
| 〃 | (직접) | — | (고치지 않는 길) | `+0x15b` Machine → `+0x15f CMP [0x7ffe002c]` (`ImageNumberLow`) · `+0x167 JNC` · `+0x177 CMP [0x7ffe002e]` (`ImageNumberHigh`) · `+0x17f JA` — 범위 밖이면 `0xc000007b`, 안이면 0 | [정적] + 값 [관찰] (아래) |
| `ntdll!LdrpCorValidateImage` | `+0x11` | `+0x16` | `RtlImageDirectoryEntryToData(…, 9)` (TLS) | `+0x16 NEG` · `+0x19 SBB` · `+0x1b AND 0xc000007b` — TLS 디렉터리가 있으면 거부 | [정적] |
| `ntdll!LdrpCorProcessImports` | `+0x1b` | `+0x20` | `AVrfDllLoadNotification` | 앞 `+0x14` 노드 `State` = 6 | [정적] |
| 〃 | `+0x2e` · `+0x41` | `+0x33` · `+0x46` | `LdrpSendDllNotifications(항목, 1)` · `LdrpLogDllState` | 뒤 `+0x4d` 노드 `State` = 7. **import 는 하나도 안 읽음** | [정적] |
| `ntdll!LdrpCompleteMapModule` (DLL) | `+0x285` | `+0x28a` | `LdrpCorValidateImage` | 앞 `+0x263 ~ +0x26b` 항목 `CorImage` · `+0x26f TEST [RAX+0x10],1` → `+0x279 ~ +0x27d` `CorILOnly`. 머리를 직접 읽음 (`+0x7c ~ +0x9f` PE32+, `+0x145` PE32). `ctx+0x20 & 0x800000` 이면 `+0x109 ~ +0x117` `0xc000007b` | [정적] (flow-writer) |
| 〃 | `+0x2a6` | `+0x2ab` | `LdrpCorFixupImage` | `+0x294 TEST [R13+0x20],0x200000` (`LdrpProcessMachineMismatch` 가 켠 비트, run38) | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` (DLL) | `+0x2d0` | `+0x2d5` | `LdrpCorProcessImports` | DLL 이 `CorILOnly` 일 때 (sast/21) | [정적] |
| `ntdll!LdrpInitializeThread` | (직접) | — | 맡긴 `CONTEXT.Rcx` | `+0x40 CMP UseCOR` · `+0x47 JNZ +0x421` → `+0x426 TEST [RBX+0x17ee],0x400` → `+0x448` 풀기 → `+0x458 MOV [R9+0x80],RAX`. 첫 스레드가 이 함수로 오는 때만 뜻이 있음 (언제인지 Q167 4) | [정적] |

### 정적 — 호출 지점 (mscoree, `ms/disassemble/mscoree/*.asm`)

같은 이름 함수가 둘인 곳은 `.asm` 이 `…__<rva>.asm` 로 갈린다 (`_CorExeMain` `0x7850` / `0x2f6e8`, `LoadLibraryShim` `0x2740` / `0x23ee0`). `_calls.tsv` 는 둘을 한 묶음으로 섞으니 줄은 `.asm` 으로 읽었다.

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `mscoree!_CorExeMain` (rva `0x7850`, 58B) | `+0x6` | `+0xb` | `ShellShim__CorExeMain` (rva `0x7894`) | 보통은 그 안에서 꼬리 점프로 끝나 안 돌아옴 | [정적] |
| 〃 | `+0x17` | `+0x1c` | `InitShimImpl` | 돌아왔고 상태 0 일 때 | [정적] |
| 〃 | `+0x2d` | `+0x32` | `DisplayMessageBoxForNoShimImpl` | 상태 1 (파일은 있는데 못 올림) | [정적] |
| `mscoree!ShellShim__CorExeMain` | `+0xa3` | `+0xa8` | `InitShimImpl` (rva `0x27b0`, 808B) | 상태 `g_shimImplStatus` 0 일 때 (`+0x10 JZ`) | [정적] |
| 〃 | `+0xc1` · `+0xe0` | `+0xc8` · `+0xe7` | `GetProcAddress(mscoreei, "_CorExeMain_RetAddr" / "_CorExeMain")` | 상태 2 (`+0x1c`) | [정적] (글자는 flow-writer 파일 바이트) |
| 〃 | ⇒ `+0x9e` · `+0x88` | — | (꼬리 JMP, CFG 중개) `mscoreei!_CorExeMain_RetAddr` / `_CorExeMain` | 콜스택에 mscoree 줄이 안 남음 | [정적] |
| 〃 | ⇒ `+0xb2` | — | (꼬리 JMP) `_CorExeMain` 둘째 몸 (rva `0x2f6e8`) | 상태 3 (`+0x37 CMP EAX,3` · `+0x3a JZ`) | [정적] |
| 〃 | — | — | (`+0xf8`) `0x80131701` 반환 | 상태 1, 또는 `g_bShimImplDllUninitialized`, 또는 주소가 NULL | [정적] |
| `mscoree!InitShimImpl` | `+0x31` | `+0x36` | `FindLatestVersion` | 음수면 `+0x52 JS` → `+0x309` | [정적] |
| 〃 | `+0x6f` | `+0x74` | `GetShimDllPathName("mscoreei.dll", …)` (`+0x68 LEA`) | 음수면 `+0x76 JS` → `+0x309` | [정적] |
| 〃 | `+0x9b` · `+0xcc` | `+0xa2` · `+0xd3` | `FindFirstFileW` · `FindClose` | 없으면 `+0xab JZ` → `+0x309` | [정적] |
| 〃 | `+0xe7` | `+0xec` | `LoadLibraryShim` (rva `0x2740`) | | [정적] |
| 〃 | `+0x10c` · `+0x125` · `+0x13e` · `+0x160` | `+0x113` · `+0x12c` · `+0x145` · `+0x167` | `GetProcAddress` — `RegisterShimImplCallback` · `RegisterShimImplCleanupCallback` · `SetShellShimInstance` · `OnShimDllMainCalled` | 그다음 `+0x1b8` CFG 간접 (콜백 등록). 상태 2 는 `+0x300` | [정적] |
| 〃 | `+0x272` · `+0x2d6` | `+0x279` · `+0x2dd` | `FreeLibrary` | 앞 둘이 다 없을 때 → 상태 1 | [정적] |
| 〃 | `+0x315` | `+0x31a` | `UpdateShimImplStatus(3, "mscoreei.dll")` | `+0x309` (못 찾음 모임) | [정적] |
| `mscoree!FindLatestVersion` | `+0x24` · `+0x3e` | `+0x29` · `+0x43` | `EnvGetString` (`COMPlus_` + 이름 → `GetEnvironmentVariableW`) — `Version` · `InstallRoot` | 둘 다 있고 파일이 있으면 레지스트리 안 봄 | [정적] |
| 〃 | `+0xbb` | `+0xc0` | `FindLatestVersionWithCallback` | | [정적] |
| 〃 | `+0x163` | `+0x168` | `CheckShimImplCallback` | 안 `+0x2b GetShimDllPathName` · `+0x53 FindFirstFileW` · `+0x68 FindClose` | [정적] |
| `mscoree!FindLatestVersionWithCallback` | `+0xdd` | `+0xe4` | `RegOpenKeyExW(HKLM, "Software\Microsoft\.NETFramework\Policy\")` (지연 import ADVAPI32) | | [정적] |
| 〃 | `+0x155` · `+0x1e7` | `+0x15c` · `+0x1ee` | `RegQueryInfoKeyW` · `RegEnumKeyExW` | 하위 키를 거꾸로 돌며 후보 | [정적] |
| 〃 | `+0x3cf` · `+0x3fd` | `+0x3d6` · `+0x402` | `RegOpenKeyExW(…\Policy\<하위 키>)` · `FindLatestBuild` | 값 이름을 `_wtoi` 해 가장 큰 빌드 | [정적] |
| 〃 | `+0x719` | `+0x71e` | `EnvGetString("DefaultVersion")` (`+0x712 LEA`) | 레지스트리로 못 골랐을 때 | [정적] (flow-writer) |
| `mscoree!GetShimDllPathName` | `+0xc0` | `+0xc7` | `GetEnvironmentVariableW("COMPlus_InstallRoot")` | 없거나 빈 값이면 레지스트리 | [정적] |
| 〃 | `+0x13f` · `+0x176` · `+0x1e0` | `+0x146` · `+0x17d` · `+0x1e7` | `RegOpenKeyExW(HKLM, "Software\Microsoft\.NETFramework", 0x20019)` · `RegQueryValueExW("InstallRoot")` · `RegCloseKey` | 문자열 형만 | [정적] |
| 〃 | `+0x248` · `+0x2a8` · `+0x36e` | … | `GetFullPathNameW` | 짠 경로와 정규화한 경로가 같아야 함 (`_wcsicmp`) | [정적] |
| 〃 | `+0x4be` · `+0x4f2` · `+0x537` … | … | `GetModuleFileNameW` · `GetFileAttributesW` | `mscoree.dll.local` 이 있으면 mscoree 폴더를 런타임 폴더로 | [정적] |
| `mscoree!LoadLibraryShim` (rva `0x2740`) | `+0x23` · `+0x31` | `+0x28` · `+0x36` | `GetShimDllPathName` · `LoadLibraryWrapper` | | [정적] |
| `mscoree!LoadLibraryWrapper` | `+0x16` | `+0x1d` | `LoadLibraryExW(경로, NULL, 8)` (`+0x12 LEA R8D,[RDX+8]` — `LOAD_WITH_ALTERED_SEARCH_PATH` `[추측: 이름]`) | → kernelbase `LoadLibraryExW +0xf8/+0xff LdrLoadDll` (결과서, 기존 `.asm`) | [정적] |
| `mscoree!_CorExeMain` (rva `0x2f6e8`, 91B — 둘째 몸) | `+0x18` | `+0x1d` | `GetInstallation(1, &모듈, 0)` (옛 고르기 길) | 음수면 `+0x21 JS` → `+0x53` | [정적] |
| 〃 | `+0x2f` | `+0x36` | `GetProcAddress(모듈, "_CorExeMain")` → ⇒ `+0x45` 꼬리 JMP | 성공 길 | [정적] |
| 〃 | `+0x55` | `+0x5c` | `ExitProcess(HRESULT)` | 실패 길 — 지금 덤프는 여기 `[추측: GetInstallation 이 실패한다고 칠 때]` | [정적] |

- 레지스트리 함수는 모두 **지연 import** (IAT `0x18006a0xx`, `.didat` 표 — ADVAPI32). 처음 부를 때 ADVAPI32.dll 을 올린다 (결과서). mscoree 의 정적 import 는 `KERNEL32.dll` 하나, 지연 import 6개 (`USER32` · `ADVAPI32` · `SHLWAPI` · `VERSION` · `OLEAUT32` · `urlmon`) — flow-writer `pefile` 로 같음.
- API → NT 함수 짝 (`RegOpenKeyExW` → `NtOpenKey(Ex)` 등) 은 kernelbase 본문을 안 봐서 `[추측]` (Q166 ②). `LoadLibraryExW` → `LdrLoadDll` 만 `[정적]`.

### 호출 관계

```
[첫 스레드 — 로더 시작, flow.md 2.4]  LdrpInitializeProcess
 │  +0x334 UseCOR = 0                           (+0x611 / +0x66c 에서만 1)
 ├─ +0x62c  LdrpCorFixupImage                   PE32 갈래 (Magic 0x10b · 비 WoW64)
 │     ├─ +0x32 RtlImageNtHeaderEx   ├─ +0x47 RtlImageDirectoryEntryToData(14)  (NULL 검사 없음)
 │     └─ (0x14c · 32BITREQUIRED 꺼짐) +0xd1 NtProtectVirtualMemory → +0xec memcpy → +0x135 Magic=0x20b → +0x149 NtProtectVirtualMemory
 ├─ +0x667  RtlImageDirectoryEntryToData(14)    → +0x66c UseCOR = 1 · +0x67f IL-only
 │  +0x13e0 / +0x13ec  exe 항목 CorImage / CorILOnly
 ├─ +0x1852 LdrpCorInitialize                   UseCOR 일 때
 │     ├─ +0x98 / +0xc5  RtlQueryEnvironmentVariable  COMPLUS_InstallRoot · COMPLUS_Version
 │     ├─ +0x10b LdrpBuildSystem32FileName      (둘 다 있으면 건너뛰고 이름만)
 │     ├─ +0x16f LdrpLoadDll  "MSCOREE.DLL"     (+ 의존 KERNEL32 [추측])
 │     ├─ +0x19f LdrpGetProcedureAddress "_CorExeMain"
 │     └─ +0x1bb LdrpCorExeMainRoutine = ROR(Cookie ^ 주소, Cookie & 0x3f)
 ├─ +0x186c LdrpCorValidateImage                IL-only 일 때 — +0x16 RtlImageDirectoryEntryToData(9)
 │  +0x18b4 CONTEXT.Rcx ← 풀어 낸 _CorExeMain   첫 스레드 (TEB SameTebFlags 0x400)
 ├─ +0x1941 LdrpInitializeGraphRecurse          mscoree 노드 (→ mscoree DllMain rva 0x9780 [추측])
 └─ +0x1a84 LdrpCorProcessImports               IL-only 일 때 — 노드 State 6 → 7, 알림만, import 안 읽음
        ├─ +0x20 AVrfDllLoadNotification   ├─ +0x33 LdrpSendDllNotifications   └─ +0x46 LdrpLogDllState
      → +0x1a87 JMP +0x1af2  (LdrpMapAndSnapDependency · LdrpDrainWorkQueue · LdrpPrepareModuleForExecution 건너뜀)

[은퇴 → 첫 스레드 — flow.md 2.10]  NtContinue ══▶ RtlUserThreadStart(rcx = _CorExeMain) → BaseThreadInitThunk
[mscoree]  _CorExeMain (0x7850)
 ├─ +0xb   ShellShim__CorExeMain
 │     ├─ +0xa8  InitShimImpl                    상태 0 일 때
 │     │     ├─ +0x36  FindLatestVersion
 │     │     │     ├─ +0x29 / +0x43  EnvGetString  COMPlus_Version · COMPlus_InstallRoot
 │     │     │     ├─ +0xc0  FindLatestVersionWithCallback
 │     │     │     │     ├─ +0xe4  RegOpenKeyExW  …\.NETFramework\Policy\   → +0x15c RegQueryInfoKeyW · +0x1ee RegEnumKeyExW
 │     │     │     │     ├─ +0x3d6 RegOpenKeyExW  …\Policy\<키>  → +0x402 FindLatestBuild
 │     │     │     │     └─ +0x71e EnvGetString   COMPlus_DefaultVersion
 │     │     │     └─ +0x168 CheckShimImplCallback → GetShimDllPathName · FindFirstFileW
 │     │     ├─ +0x74  GetShimDllPathName("mscoreei.dll")
 │     │     │        ├─ +0xc7 GetEnvironmentVariableW  COMPlus_InstallRoot
 │     │     │        ├─ +0x146 RegOpenKeyExW · +0x17d RegQueryValueExW "InstallRoot" · +0x1e7 RegCloseKey
 │     │     │        └─ GetModuleFileNameW · GetFileAttributesW  (mscoree.dll.local)
 │     │     ├─ +0xa2  FindFirstFileW   (없으면 → +0x31a UpdateShimImplStatus(3))
 │     │     ├─ +0xec  LoadLibraryShim → +0x36 LoadLibraryWrapper → +0x1d LoadLibraryExW(…, 8)
 │     │     └─ +0x113 / +0x12c / +0x145 / +0x167  GetProcAddress ×4 → 콜백 등록 → 상태 2
 │     ├─ 상태 2: +0xc8 / +0xe7 GetProcAddress → ⇒ +0x9e / +0x88  mscoreei!_CorExeMain(_RetAddr)
 │     └─ 상태 3: ⇒ +0xb2  _CorExeMain (0x2f6e8)
 │                          ├─ +0x1d GetInstallation → (성공) +0x36 GetProcAddress ⇒ +0x45
 │                          └─ (실패) +0x5c ExitProcess(HRESULT)
 └─ (돌아오면) +0x1c InitShimImpl · +0x32 DisplayMessageBoxForNoShimImpl (상태 1)
```

### 사진 대조 (run44 덤프 읽기 로그, 새 실행 없음 — flow-writer)

| 칸 | 값 | 줄 | 뜻 |
|---|---|---|---|
| ntdll `UseCOR` (rva `0x1d292c`, 1B) | **0** (P1a · P1b · P2) | `ms/logs/04_baggage-read_P1a.txt:5132` · `_P1b.txt:6250` · `_P2.txt:6262` (`…832920` 줄 둘째 qword `0x2` — 낮은 바이트 `+0x928` 은 `LdrpHotPatchInitialized` 2, `+0x92c` 는 0) | test.exe 는 갈래 안 탐 `[관찰]` |
| ntdll `LdrpCorExeMainRoutine` (rva `0x1d25f0`, 8B) | **0** (P1a · P1b · P2) | `_P1a.txt:5081` · `_P1b.txt:6199` · `_P2.txt:6211` | 쓰는 줄 (`LdrpCorInitialize +0x1bb`) 안 탐 `[관찰]` |
| 모듈 항목 `Flags.CorImage` · `CorILOnly` (6개 모듈) | `0y0` 모두 | `_P2.txt:3423` · `3425` · `3528` · `3530` · `3633` … | 쓰는 줄 안 탐 `[관찰]` |
| KUSD `ImageNumberLow` (`+0x2c`) · `ImageNumberHigh` (`+0x2e`) | **`0x8664` · `0x8664`** | 커널 별칭 `_supp_<K1 ~ P2>.txt:188 ~ 189` (8장 같음), 유저 주소 `_P1a.txt:2896 ~ 2897` · `_P1b.txt:4014 ~ 4015` · `_P2.txt:4026 ~ 4027` | 결과서 "동적 3" 닫힘 `[관찰]` |
| KUSD `Cookie` (`+0x330`) | `0xe8e5ccd6` | `_supp_P0.txt:254` · `_P1a.txt:2962` | `_CorExeMain` 섞기 쿠키 (값은 아무거나 — 넣고 푸는 쪽이 같음) `[관찰]` |
| KUSD `ComPlusPackage` (`+0x2e0`) | **1** | `_supp_K1.txt:224` · `_P1a.txt:2932` | 쓰는 쪽 모름. 커널 `ExpReadComPlusPackage` 이름만 (Q166 ①) `[관찰]` |

- `ImageNumberLow == ImageNumberHigh == 0x8664` 라는 건: `LdrpCorFixupImage` 의 "고치지 않는 길" (`+0x15b`) 은 **Machine 이 `0x8664` 일 때만 통과**하고, 32BITREQUIRED 가 선 PE32 (`0x14c`) 가 64비트 프로세스로 오면 `0xc000007b` 로 끝난다 `[정적 조건 + 관찰 값]`. 그런 exe 를 커널이 WoW64 로 보내는지는 커널 쪽 (Q166 ①).

## 확인된 것

- `[정적]` ntdll 은 exe 머리를 **두 길**로 본다: (a) Magic `0x10b` 이고 비 WoW64 면 디렉터리 14 를 보지 않고 곧바로 `UseCOR` (`+0x602 ~ +0x611`), (b) 아니면 `RtlImageDirectoryEntryToData(…, 14)` (`+0x662 / +0x667`) 가 NULL 이 아니면 `UseCOR` · `Flags & 1` 로 IL-only (`+0x66c ~ +0x67f`). `UseCOR` 를 쓰는 곳은 이 셋 + 처음 0 (`+0x334`) 뿐이다 (`_refs`).
- `[정적]` `UseCOR` 면 **ntdll 이 `MSCOREE.DLL` 을 직접 올린다** (`+0x184d LdrpCorInitialize` → `+0x16a LdrpLoadDll`). exe 의 import 로 올리는 게 아니다. 이름은 보통 `LdrpBuildSystem32FileName` 으로 System32 전체 경로, 환경변수 `COMPLUS_InstallRoot` · `COMPLUS_Version` 이 **둘 다** 있으면 이름만.
- `[정적]` **진입점은 맡긴 문맥 `Rcx` 를 덮는 꼴로 바뀐다** (`+0x18b4 MOV [RBX+0x80],RAX`, `RBX = [RSP+0xe0]` = 받은 CONTEXT). 첫 스레드 (`SameTebFlags & 0x400`) 일 때만, IL-only 와 상관없이 `UseCOR` 면. 값은 `LdrpCorInitialize +0x1bb` 가 KUSD `Cookie` 로 섞어 둔 `_CorExeMain` 주소를 푼 것.
- `[정적]` IL-only 면 exe 항목 `EntryPoint` 를 안 적고 (`LdrpProcessMappedModule +0x5a ~ +0x5d`), 재배치도 안 하고 (`+0x141f`), **import 표를 안 걷는다** — 갈림 `+0x1a73 TEST R12B` · `+0x1a76 JZ`, IL-only 쪽 `+0x1a7f LdrpCorProcessImports` (노드 상태 6 → 7 + 알림만) → `+0x1a87 JMP +0x1af2` 로 `LdrpThreadTokenSetMainThreadToken` · `LdrpMapAndSnapDependency` · `LdrpDrainWorkQueue` · `LdrpThreadTokenUnsetMainThreadToken` · `LdrpPrepareModuleForExecution` 를 다 건너뛴다.
- `[정적]` IL-only 이미지는 TLS 디렉터리가 있으면 `0xc000007b` (`LdrpCorValidateImage`).
- `[정적]` PE32 AnyCPU 는 `LdrpCorFixupImage` 가 **메모리 속 머리를 PE32+ 로 고친다** (첫 쪽을 쓰기 가능으로 → 선택 헤더 `0x10` 밀기 → 8바이트로 넓히기 → `Magic = 0x20b` → 보호 되돌림).
- `[정적+샘플]` + `[관찰]` test.exe 는 이 갈래를 하나도 안 탄다: Magic `0x20b` · 디렉터리 14 없음 (sample 셋 다), `UseCOR` · `LdrpCorExeMainRoutine` · 항목 `CorImage` · `CorILOnly` 모두 사진 끝까지 0 (run44).
- `[관찰]` KUSD `ImageNumberLow` · `ImageNumberHigh` = `0x8664` · `0x8664`, `ComPlusPackage` = 1, `Cookie` = `0xe8e5ccd6` (run44, 8장 같음).
- `[정적]` mscoree `_CorExeMain` → `ShellShim__CorExeMain` → `InitShimImpl` 이 런타임 고르기 첫 단계다. 환경변수 셋 · 레지스트리 두 키 · `mscoree.dll.local` · `<InstallRoot>\<버전>\mscoreei.dll` 을 본다. mscoreei 를 올리면 꼬리 점프로 `mscoreei!_CorExeMain(_RetAddr)` 에 넘기고, 못 찾으면 옛 길 `GetInstallation` → 실패면 `ExitProcess`.
- `[정적: PE 표]` x64 IL-only exe (`tzsync.exe`) 는 AEP 0 · import 없음, AnyCPU (`ScriptRunner.exe`) 는 PE32 · `mscoree.dll!_CorExeMain` 하나, 혼합 모드 (`FileHistory.exe`) 는 `Flags 0x8` · 평범한 import + mscoree 둘 (flow-writer 다시 셈, 결과서와 같음).

## 결과서와 다르게 적은 곳 · 보탠 것

- ① **"`LdrpCorProcessImports +0x1a7f`" (결정 기록) 은 자리 표기가 섞였다.** `+0x1a7f` 는 `LdrpInitializeProcess` 안의 **call 자리** (ret `+0x1a84`) 이고, `LdrpCorProcessImports` 자체는 97B (`+0x0 ~ +0x60`) 다. "import 를 안 걷는" 실제 자리는 **갈림 `LdrpInitializeProcess +0x1a73 TEST R12B,R12B` · `+0x1a76 JZ +0x1a89`** (R12 = `+0x13d2` 에서 다시 실은 IL-only 표시 `[RSP+0x58]`, 그 사이 R12 를 쓰는 줄 없음) 와 **`+0x1a87 JMP +0x1af2`** (import 길 통째로 건너뛰기). `LdrpCorProcessImports` 안은 import 표를 아예 안 읽는다 (`+0x14` 상태 6 → 알림 셋 → `+0x4d` 상태 7). (오케스트레이터 부탁)
- ② (보탬) **IL-only 가 건너뛰는 것** — 결과서는 `LdrpPrepareModuleForExecution` 만 적었다. `+0x1a87 JMP` 는 `LdrpThreadTokenSetMainThreadToken` (`+0x1a89`) · `LdrpMapAndSnapDependency` (`+0x1aa4`) · `LdrpDrainWorkQueue` (`+0x1aac`) · `LdrpThreadTokenUnsetMainThreadToken` (`+0x1abd`) 도 건너뛴다.
- ③ (보탬) **`LdrpCorFixupImage` 는 디렉터리 14 결과를 NULL 검사 없이 읽는다** (`+0x42/+0x47` → `+0x69 TEST [RAX+0x10],2`). 그리고 PE32 갈래 (`LdrpInitializeProcess +0x602`) 는 디렉터리 14 를 보지 않고 `UseCOR` 를 세운다. 그러니 CLR 헤더 없는 PE32 (`0x14c`) exe 가 64비트 프로세스로 오면 이 줄에서 널 읽기다 `[정적]`. 커널이 그런 exe 를 64비트로 안 띄운다는 건 `[추측]` (Q166 ①).
- ④ (보탬, 결과서 "동적 3" 닫힘) **KUSD `ImageNumberLow` · `ImageNumberHigh` = `0x8664` · `0x8664`** `[관찰]` run44 — 위 "사진 대조".
- ⑤ **`FindLatestVersionWithCallback +0x712 / +0x719`** (결과서 call / ret 꼴) → `+0x712` 는 이름 글자 `LEA` (`DefaultVersion`), call 은 **`+0x719`** / ret `+0x71e`.
- ⑥ (보탬) 사진 값 — `UseCOR` · `LdrpCorExeMainRoutine` · 항목 `CorImage` · `CorILOnly` 끝까지 0, KUSD `ComPlusPackage` 1 · `Cookie` `[관찰]`. 짐 지도 모르던 칸 넷이 이걸로 닫혔다 (아래 "짐 지도에 반영한 것").
- ⑦ (보탬) **PE 머리 다시 셈** (`pefile`): `tzsync.exe` · `ScriptRunner.exe` · `FileHistory.exe` · `mscoree.dll` 은 결과서 Q4 · Q3 값과 같다. sample 셋 (Magic `0x20b`, 디렉터리 14 없음) 은 결과서에 없던 것.
- ⑧ (보탬, `[추측]`) **.NET exe 는 kernel32 · kernelbase 가 `+0x184d` 에서 먼저 올라오고 `+0x193c` 에서 먼저 켜질 것** — 위 "우리 지도에서 어디서 갈라지나". 결과서엔 없는 짐작이라 Q167 7 로.
- ⑨ (보탬) DLL 쪽 `CorImage` · `CorILOnly` 쓰는 줄 = `LdrpCompleteMapModule +0x263 ~ +0x26b` · `+0x279 ~ +0x27d` (결과서는 "`+0x263 ~ +0x279` 에서 켬").
- ⑩ 이름: mscoree 의 `alias:…+1` 은 같은 함수의 C++ 꾸민 이름 하나라 이름 그대로 썼다. ntdll 복사 `alias:RtlCopyMemory+3` → 같은 주소 이름 2개 `memcpy` · `memmove` (run88 과 같음, `pdb_aliases.py --lookup ntdll.dll 0x166500`).
- ⑪ **바뀌는 곳 개수**: 결과서 "`LdrpInitializeProcess` 안 6군데 + 작은 함수 3개" → 이 문서 "갈래 자리 열 + 작은 함수 넷". 자리는 `.asm` 으로 다시 센 수 (판정 2 · 항목 비트 2 · 재배치 1 · mscoree 올림 1 · 검증 1 · `Rcx` 1 · 그래프 1 · import 갈림 1) 이고, 함수엔 `LdrpCorProcessImports` (97B) 를 더했다.
- ⑫ **지금 덤프로 되는 단계**: 결과서 "3 ~ 8 은 지금 덤프로 가능" → 이 문서 "0 ~ 8". 0 ~ 2 는 커널 · ntdll 코드 (이미 덤프에 있음) 라 더할 파일이 없어서 넣었다.

## 정정

- **짐 지도 7.1 전역 셈 (예전부터 묵음)**: "전역 끝까지 0 중 모르는 칸 266" 은 run60 뒤 안 고쳐졌다. run62 (12) · run65 (6) · run66 (5) · run68 (3) · run69 (1) 이 닫은 끝까지 0 전역 27개를 안 뺐다 → run88 뒤 **239** (2.1 "ntdll 전역 모름 … 끝까지 0 239" · 7.3 `.data` 217 + `.mrdata` 22 와 같음), run89 뒤 **237**. 같은 절 "아는 칸 중 끝까지 0 전역 30" 도 296 − 239 = **57** (run89 뒤 59) 이어야 맞다. 짐 지도 장부 셈이라 run 문서 맨 위 정정 줄은 안 넣는다 (run88 의 구조체 셈 정정과 같은 꼴).

## 아직 확인 필요

**정적 — 대기열 Q166** (결과서 "다음 요청 후보" 1 · 2 · 3 · 5 + 새로 생긴 것):

- ① 커널 쪽 기계 고르기: `PspSelectMachineForProcess` 본문 (`PspAllocateProcess +0xa87`) · `PspQueryComPlusRunUnderWoW` (`+0x10a860`) · `ExpReadComPlusPackage` (`ExpQuerySystemInformation` 조각이 부름 — 어떤 레지스트리 값, KUSD `ComPlusPackage` 를 쓰나) · 섹션 만들 때 CLR 정보를 `ImageInformation` 에 싣는 줄. AnyCPU 를 64비트로 띄우는 근거, CLR 헤더 없는 PE32 · 32BITREQUIRED PE32 가 64비트로 안 오는지 (위 ③).
- ② kernelbase `RegOpenKeyExW` · `RegQueryValueExW` · `RegEnumKeyExW` · `RegEnumValueW` · `RegQueryInfoKeyW` · `GetFileAttributesW` · `FindFirstFileW` → NT 함수 이름 (지금 `[추측]`).
- ③ mscoree 옛 길 (상태 3): `GetInstallation` · `FindVersionedRuntime` · `GetInstallRoot` · `ComputeVersionString` 본문 — 덤프에 mscoreei 가 없을 때 어떤 HRESULT 로 끝나나, `NoGuiFromShim` 설정 이름 (Ghidra `.c` 가 틀리게 그림 `[공백]`).
- ④ mscoree `DllMain` (진입점 rva `0x9780`) 이 하는 일 (`+0x193c` 에서 불림, 지금 `[추측]`).
- ⑤ (새로) `LdrpInitializeThread +0x40` 갈래 — 첫 스레드가 `LdrpInitializeProcess` 가 아니라 이 함수로 오는 때가 정적으로 있나 (`_LdrpInitialize +0x3a` 옛 값 2 인데 `InitialThread` 인 경우).
- ⑥ (새로) 커널이 `AddressOfEntryPoint = 0` 인 exe 를 받는 길 — `MiVerifyImageHeader` (run31) 에 0 검사가 있나, 그때 `TransferAddress` (= 처음 `Rcx`) 가 `ImageBase` 인가.
- ⑦ (새로) IL-only exe 재배치를 누가 하나 — ntdll 은 `+0x141f` 로 건너뛴다 (결과서 `[추측: CLR]`). 커널 `MiRelocateImage` (run31 · run73) 가 이미 고친 상태인지.

**동적 — 대기열 Q167** (새 행. .NET exe 가 있어야 본다 — `ScriptRunner.exe` AnyCPU · `tzsync.exe` x64 IL-only, **test.exe 로 못 봄, 사용자 결정 몫**). 멈춤점은 모두 그 exe 일 때만 걸리게 조건:

- 1 `Rcx` 바뀌는 순간: `ntdll!LdrpInitializeProcess+0x18b4` 에서 `r rax` (= `mscoree!_CorExeMain`), 직전 `dq @rbx+0x80 L1` (= exe 진입점 — x64 IL-only 면 AEP 0 이라 무엇이 들어있나), 이어 `NtContinue` 직전 CONTEXT 와 비교.
- 2 AnyCPU 가 정말 64비트로 오나: `ScriptRunner.exe` 의 TEB `+0x180c` (`WowTebOffset`) 0, `ntdll!LdrpInitializeProcess+0x627` 에 서나, `LdrpCorFixupImage` 앞뒤 머리 `Magic`.
- 4 `ntdll!LdrpInitializeThread+0x421` 에 서는 경우가 있나.
- 5 mscoree 가 읽는 실제 값 · 경로: `InstallRoot` 값, `Policy\v4.0` 아래 값, 고른 버전 글자, `LoadLibraryExW` 에 넘어가는 전체 경로 (`mscoree!LoadLibraryWrapper+0x16` `du @rcx`), 파일 · 레지스트리 열기 목록 → "덤프에 더할 파일" 확정.
- 6 mscoreei 가 없을 때의 끝: `mscoree!_CorExeMain+0x55` (rva `0x2f6e8` 쪽, `ExitProcess`) `r ecx`, 메시지 상자가 뜨나.
- 7 (새로, 위 ⑧) `LdrpInitializeKernel32Functions` 입구 (`+0x19a2` 안) 에서 `lm` — kernel32 · kernelbase 가 이미 올라와 있나, `+0x193c` 에서 그 둘의 시작 함수가 먼저 불리나.
- 추천 멈춤점 (결과서): `ntdll!LdrpCorInitialize` 입구 · `+0x16a` (`du` 이름) · `+0x1bb`, `ntdll!LdrpInitializeProcess+0x18b4`, `mscoree!InitShimImpl` 입구 · `+0x6f` · `+0xab` · `+0xe7`, `mscoree!ShellShim__CorExeMain+0x9e` · `+0x88` · `+0xb2` (상태 `g_shimImplStatus`).
- (결과서 "동적 3" `ImageNumberLow/High` 는 run44 사진으로 닫힘 — 위 ④.)

**test.exe 로 되는 것 → Q104 에 안 붙임**: 이 갈래의 test.exe 쪽 값 (`UseCOR` · `LdrpCorExeMainRoutine` · 항목 비트 넷 · KUSD 칸) 은 run44 사진으로 이미 `[관찰]`. 새로 볼 게 없다.

## flow.md · 짐 지도에 반영한 것

- flow.md:
  - 기준: ntdll pdb 줄에 run89 몫, **mscoree pdb 새 줄**, 근거 로그 run89 줄 (run44 로그의 `UseCOR` · `LdrpCorExeMainRoutine` · 항목 비트 · KUSD 칸).
  - 흐름도 앞 글: run89 문단 하나 — **그림은 안 고침** (test.exe 는 CLR 갈래를 안 탐).
  - 2.4: 머리 run 목록, `LdrpInitializeProcess` 나무 세 줄 (`+0x5f2` 줄 · `LdrpInitializeTls` 줄 · `LdrpMapAndSnapDependency` 줄에 run89 한 마디), "뒤쪽에서 쓰는 앞쪽 값" 의 `UseCOR` = 0 `[추측]` → `[관찰]` run44 + `[정적+샘플]`, 새 절 **"CLR 헤더 갈래 (.NET exe) — test.exe 는 안 탐"**.
  - 2.6: 머리 run 목록, `LdrpInitializeProcess +0x1aa4` 후보 줄에 갈림 자리 한 마디. SxS 절의 `+0xb0d` 를 `+0xb0d` (ret `+0xb12`) 로 (run88 뒷정리 — 2.4 나무 ret `+0xb12` 와 맞춤).
  - 2.10: 머리 run 목록, `RtlUserThreadStart` 절에 `rcx` = `_CorExeMain` (.NET) 한 줄.
  - 4장: run89 새 줄 셋 (동적 Q167 · 정적 Q166 · PE_emulator 쪽 · 결과서와 다르게 적은 곳 · 짐 지도 셈 정정). 4.1 그림 2 `(9)` 줄에 IL-only 갈림 자리.
  - 5장: run 표 · 03_sprint 요청서 번호 run89 줄.
- 짐 지도 (짐 지도 바퀴 — **새 짐 칸은 없지만 모르던 칸 넷을 닫음**, 모두 test.exe 는 "0 을 씀" 또는 "조건부 안 탐"):
  - 3.5 모듈 항목 `Flags.CorImage` · `Flags.CorILOnly` (exe · DLL 줄 넷), 3.8 `.data` `UseCOR` (줄 둘) · `LdrpCorExeMainRoutine` (줄 하나). 근거 칸 `sast86`, 줄 끝 "Q152 로 닫힘".
  - 2장 모듈 항목 ntdll 39 → 41 · 모름 26 → 24, `.data` ntdll 159 → 161 · 모름 277 → 275, 합 셋, 구조체 아는 칸 736 → 738 (정확 682 → 684) · 모름 303 → 301, 전역 아는 것 186 → 188 · 모름 307 → 305. 1장 그림 두 줄. 2.1 모름 줄 둘.
  - 3.7 KUSD 머리 "ntdll 은 읽기만" 줄에 읽는 줄 (`ImageNumberLow/High` · `Cookie`), 7.2 KUSD 실마리 (값 `0x8664` · `ComPlusPackage` 1), 4장 진입점 짝 줄에 `Rcx` 덮기, 5장 ② 한 줄.
  - **7.1 셈 정정** (전역 끝까지 0 모름 "266" 은 run60 뒤 안 고쳐진 숫자 → run89 전 239, run89 뒤 237), 7.2 · 7.3 목록, 8장 두 표, 기준 run89 줄 · 판 목록 (마흔다섯째판).

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음. 새 함정 없음.
- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 디버거 여부를 보는 줄은 없다 `[정적: 읽은 줄 안에서]`. `+0x188c` 의 KUSD `+0x330` 은 쿠키 (run19 에서 이미 정리) 라 디버거와 상관없다.
- 도구 (결과서 "스크립트 문제"): `ExportFunctions.java` 가 긴 C++ 템플릿 이름 (`InitShimImpl` 이 부르는 소멸자, 같은 주소 이름 9개) 으로 파일 이름 길이 한계를 넘어 mscoree depth 1 뽑기가 종료 코드 4 로 멈췄다 — 하네스 제안 ⑫ 로 이미 적혀 있음. 이 문서가 쓴 함수는 모두 파일이 있다.
- 도구 (flow-writer): mscoree 는 같은 이름 함수 둘이 `_calls.tsv` 에서 한 묶음으로 섞인다 — 줄은 `…__<rva>.asm` 로 읽을 것 (결과서 덧붙임 1).

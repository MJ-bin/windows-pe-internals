# 97 k32-leftovers — 정적 분석 결과

- 요청서: `_workspace/sast/97_k32-leftovers.md` (대기열 Q59 + Q60, 정적만)
- 모듈 / 바이너리: kernel32 (`ms/bin/.../System32/kernel32.dll`, pdb guid+age `AE9E9D8F7AC1467A645320FDC23235D01`), ntdll (읽기만), kernelbase (PE 표 읽기만), apisetschema (`.apiset` 표 읽기만). 커널 PDB GUID 확인은 이번 요청에 ntoskrnl 이 없어 해당 없음 (sast/96 쪽은 안 건드림).
- 뽑은 함수 (kernel32, 이번에 새로 export):
  - `^BasepInitializeTermsrvFpns$` depth 1 → 4개: `BasepInitializeTermsrvFpns` · `IsTerminalServerCompatible` · `IsSystemLUID` · `IsTSAppCompatEnabled` (`GetProcAddress`·`LoadLibraryExW` 는 import 라 파일 없음)
  - `^(BaseThreadInitThunk|LoadLibraryExW|LoadLibraryW|LoadLibraryA)$` depth 0 → 4개
  - 덤으로 `^TermsrvSyncUserIniFileExt$` depth 0 (17 B, 칸을 읽는 곳 확인용) + `refs` 두 번 (`0xc1cb8` 0xd0 바이트, `TermsrvSyncUserIniFileExt`) → `ms/disassemble/kernel32/_refs/`
  - ntdll 은 있던 것 그대로 읽음: `RtlGetSuiteMask.asm` · `LdrpInitializeProcess.asm` · `LdrpInitializeKernel32Functions.asm` · `RtlUserThreadStart.asm` · `RtlImageNtHeader.asm` + `_refs/Kernel32ThreadInitThunkFunction.tsv`
  - PE 표 (export·import·IAT·ApiSet) 는 python `pefile` 로 읽었다 (임시 스크립트는 scratchpad, 레포 밖).
- 버전 확인 (일치):
  - ntdll `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` (`.asm` 1479줄) 알려진 사실과 같다.
  - kernel32 `BaseThreadInitThunk` rva `0x2cd70` = run44 `[관찰]` `Kernel32ThreadInitThunkFunction` 값 `KERNEL32+0x2cd70` (baggage.md 2528행) 과 같다. `+0x26/+0x2d → RtlGetSuiteMask`, `+0x36/+0x3b → BasepInitializeTermsrvFpns` 는 run25 와 같다.
  - 스크립트 문제: 없음.

## 한눈에

1. `BasepInitializeTermsrvFpns` = **터미널 서비스(TS) 호환 함수 포인터 준비.** 맨 처음 줄 하나로 전역 `pfnTermsrvSyncUserIniFile` 을 **무조건** 채운다. 그 뒤 exe 가 "TS 인식" 이 아니고 TS 앱 호환이 켜진 서버일 때만 `tsappcmp.dll` 을 올려 `GetProcAddress` 12개로 `gp…`·`pfn…` 칸 12개를 채운다.
2. test.exe 는 `DllCharacteristics = 0x8160` (비트 15 = TS 인식) 이라서 **함수 안 `+0x29 JZ` 가 안 갈라지고 `+0x2b → +0x2d` 로 나가 0 을 돌려준다.** `tsappcmp.dll` 은 안 올린다. 이 갈래로 **들어가는 것 자체는 예** (`SuiteMask = 0x110` 이라 `& 0x10` 이 켜짐 `[관찰]`).
3. test.exe 에서 이 함수가 실제로 바꾸는 전역은 **딱 하나: `pfnTermsrvSyncUserIniFile` (kernel32 rva `0xc1d08`)** = kernel32 + `0x72b60` (`TermsrvSyncUserIniFile`). baggage.md 에는 이 칸이 없다 → **새 짐 1칸**.
4. `KERNEL32!LoadLibraryExW` 는 forwarder 가 아니라 7바이트 `JMP [rip+X]` 스텁이다. X = kernel32 IAT `0x8b3e8` = `api-ms-win-core-libraryloader-l1-2-0.dll!LoadLibraryExW` → ApiSet 표에서 `kernelbase.dll` → `KERNELBASE!LoadLibraryExW` (rva `0x21150`). `LoadLibraryW`·`LoadLibraryA` 도 같은 꼴의 스텁이다.

---

## Q59 — `BasepInitializeTermsrvFpns` 안쪽

### 전체 호출 사슬 (처음 부르는 자리까지)

| 단계 | 어디서 | 무엇을 | 근거 |
|---|---|---|---|
| 1 | ntdll `LdrpInitializeKernel32Functions +0xfa` (ret `+0xff`) | `LdrGetProcedureAddressForCaller(kernel32, "BaseThreadInitThunk", 0, &Kernel32ThreadInitThunkFunction, …)` — 칸 `0x1d2760` 을 채움. 이름 문자열은 ntdll rva `0x173ba8` 의 ANSI_STRING (길이 19) = `BaseThreadInitThunk` (PE 에서 직접 읽음) | `LdrpInitializeKernel32Functions.asm` `+0xe4`·`+0xfa`, `_refs/Kernel32ThreadInitThunkFunction.tsv`, run44 `[관찰]` 값 `KERNEL32+0x2cd70` |
| 2 | ntdll `LdrpInitializeProcess +0x1b45` | 그 칸을 읽고 (`+0x1b4f JZ` → NULL 이면 `+0x1b83` 로 건너뜀) `ECX = 1` (`+0x1b56 LEA ECX,[RDX+1]`), `EDX = 0` (`+0x1b54`), `R8 = 0` (`+0x1b51`) 로 `+0x1b59` (ret `+0x1b5e`) CFG 간접 호출 | `LdrpInitializeProcess.asm` 1455~1461줄 |
| 3 | 돌아온 뒤 | `+0x1b62 MOV [RSP+0x50],EAX`, `+0x1b64 JS` (→ `+0xa7b` 실패 꼬리), 아니면 `+0x1b72` `LdrpInitializePerUserWindowsDirectory` | 같은 파일 1462~1466줄 |

- `[추측]` 이던 "`LdrpInitializeProcess +0x1b59` 의 `Kernel32ThreadInitThunkFunction(1,0,0)` = `BaseThreadInitThunk(1,0,0)`" 는 **`[정적]` 로 닫힌다**: 칸 값은 kernel32 에서 이름 `BaseThreadInitThunk` 로 찾은 주소 (1행) 이고, run44 `[관찰]` 도 `KERNEL32+0x2cd70`.
- 이 칸을 읽는 ntdll 코드는 두 곳뿐이다 (`_refs`): `LdrpInitializeProcess +0x1b45` (인자 1) 와 `RtlUserThreadStart +0x7` (새 스레드, `+0x25 XOR ECX,ECX` 로 인자 **0**). 그래서 **첫 인자 ≠ 0 갈래는 프로세스 초기화 때 한 번뿐**이다 (ntdll 쪽 코드 기준).

### 질문 1 — `BasepInitializeTermsrvFpns` 가 하는 일

- 답 `[정적]`: 아래 표. 함수 안에 `SYSCALL` 명령은 없다. depth 1 함수 중 syscall 을 부르는 것은 `IsSystemLUID` 뿐이고 (`NtOpenProcessToken` · `NtQueryInformationToken` · `NtClose`), **test.exe 길에선 안 탄다** (`+0x29` 에서 이미 나감). `IsTerminalServerCompatible` 이 부르는 `RtlImageNtHeader` → `RtlImageNtHeaderEx` 는 메모리만 읽는다 (syscall·호출 없음, `RtlImageNtHeader.asm`).
- 근거: `ms/disassemble/kernel32/BasepInitializeTermsrvFpns.asm`, `IsTerminalServerCompatible.asm`, `IsSystemLUID.asm`, `IsTSAppCompatEnabled.asm`, `_calls.tsv`.

| 오프셋 (call / ret) | 부르는 것 · 하는 일 | 쓰는 전역 | test.exe |
|---|---|---|---|
| `+0x14 ~ +0x1b` | `LEA RAX,[0x180072b60]` → `MOV [0x1800c1d08],RAX` | **`pfnTermsrvSyncUserIniFile`** (rva `0xc1d08`) = kernel32 + `0x72b60` (`TermsrvSyncUserIniFile`, 1386 B) | **탄다 (무조건)** |
| `+0xf` | `AND [RSP+0x30],0` (지역 변수 0) | (스택) | 탄다 |
| `+0x22` / `+0x27` | `IsTerminalServerCompatible` (이 함수 안 오프셋은 `.asm` 참고) — `PEB.ImageBaseAddress` (`GS:[0x60]` → `+0x10`) → `RtlImageNtHeader` → NT 헤더 `+0x5e` (`OptionalHeader.DllCharacteristics`) 를 부호 있는 16비트로 비교 (`+0x27 CMP`, `+0x2b SETL`) = **비트 15 (0x8000, TS 인식) 켜졌으면 1** | 없음 | **1 을 돌려줌** |
| `+0x29` | `JZ +0x3e` — 0 이면 아래 `IsSystemLUID` 로 감. 1 이면 안 가고 `+0x2b XOR EAX,EAX` → `+0x2d ~ +0x3c` 정리 → `RET` (0 반환) | 없음 | **여기서 나감** (`+0x2b`) |
| `+0x3e` / `+0x43` | `IsSystemLUID` — `NtOpenProcessToken(-1, 8)` (`IsSystemLUID` 안 `+0x33`/`+0x3a`) → `NtQueryInformationToken(…, 10 = TokenStatistics, 0x38)` (`+0x5e`/`+0x65`) → `NtClose` (`+0x71`/`+0x78`). AuthenticationId 가 `0x3e7` (SYSTEM) 이거나 토큰 열기·조회가 실패하면 1 | 없음 | 안 탐 |
| `+0x45` | `JNZ +0x2b` — SYSTEM 이면 나감 | | 안 탐 |
| `+0x4c` / `+0x51` | `IsTSAppCompatEnabled(&지역변수)` — `GetVersionExW` (`IsTSAppCompatEnabled` 안 `+0x3d`/`+0x44`) 로 `wSuiteMask`·`wProductType` 를 보고 "TS 비트 켜짐 · `SINGLEUSERTS`(0x100) 꺼짐 · 제품 종류가 2 또는 3 (서버)" 일 때만 1 을 지역 변수에 둔다. 반환값은 항상 0 (`+0x7c XOR EAX,EAX`) | 없음 | 안 탐 |
| `+0x53` / `+0x55 ~ +0x5a` | `JS +0x2d` (음수면 그 값 반환) · 지역 변수 0 이면 `JZ +0x2b` 로 나감 | | 안 탐 |
| `+0x5c ~ +0x65` / `+0x6b` (ret `+0x72`) | `LoadLibraryExW(L"tsappcmp.dll", NULL, 0x800 = LOAD_LIBRARY_SEARCH_SYSTEM32)` — kernel32 **자기 IAT** `0x8b3e8` 를 거쳐 kernelbase 로 곧장 감 (자기 스텁은 안 거침). 글자 상수는 kernel32 rva `0x92b28` (PE 에서 읽음) | 없음 | 안 탐 |
| `+0x77 ~ +0x7d` | `RDI = RAX`. NULL 이면 `+0x7f ~ +0x88` 에서 TEB `+0x1250` (`LastStatusValue`) 를 읽어 `+0x8e JMP +0x2d` 로 나감 (그 값이 반환값 → 음수면 프로세스 초기화 실패 길) | 없음 | 안 탐 |
| `+0x90 ~ +0xc5` 루프 (12바퀴, `RBX` 0 → `0xc0`, 16씩) | 표 `rva 0x89310` (아래) 에서 `{이름 포인터, 칸 주소}` 를 꺼내 `GetProcAddress` (`+0xa7`/`+0xae`, import `api-ms-win-core-libraryloader-l1-2-0.dll!GetProcAddress`) → 결과를 **칸에 씀** (`+0xbb MOV [RCX],RAX`), `+0xc5 JNZ → +0x99` 로 되돌아감. 끝나면 `+0xc7 JMP +0x2b` | **아래 12칸** | 안 탐 |

- 12칸 표 (kernel32 `.rdata` rva `0x89310`, 16바이트 × 12. PE 에서 직접 읽었고 칸 이름은 PDB 공개 이름):

| # | `tsappcmp.dll` 에서 찾는 이름 | 쓰는 칸 (kernel32 `.data` rva · PDB 이름) | 칸을 읽는 곳 (`_refs/0xc1cb8.tsv`, 이름만) |
|---|---|---|---|
| 0 | `TermsrvGetComputerName` | `0xc1d30` `gpTermsrvGetComputerName` | `GetComputerNameW +0xfa` |
| 1 | `TermsrvAdjustPhyMemLimits` | `0xc1d50` `gpTermsrvAdjustPhyMemLimits` | `GlobalMemoryStatus +0xc1` |
| 2 | `TermsrvGetWindowsDirectoryA` | `0xc1d18` `pfnTermsrvGetWindowsDirectoryA` | `TermsrvGetWindowsDirectoryA +0` |
| 3 | `TermsrvGetWindowsDirectoryW` | `0xc1d10` `pfnTermsrvGetWindowsDirectoryW` | `TermsrvGetWindowsDirectoryW +0` · `BaseDllFindIniFileNameMapping +0x66` |
| 4 | `TermsrvConvertSysRootToUserDir` | `0xc1cb8` `pfnTermsrvConvertSysRootToUserDir` | `TermsrvConvertSysRootToUserDir +0` |
| 5 | `TermsrvBuildIniFileName` | `0xc1d48` `gpTermsrvBuildIniFileName` | `BaseDllOpenIniFileOnDisk +0x69` |
| 6 | `TermsrvCORIniFile` | `0xc1d38` `gpTermsrvCORIniFile` | `BaseDllOpenIniFileOnDisk +0x15e` |
| 7 | `GetTermsrCompatFlags` | `0xc1d58` `gpGetTermsrCompatFlags` | `TermsrvSyncUserIniFile +0x18f` |
| 8 | `TermsrvBuildSysIniPath` | `0xc1d68` `gpTermsrvBuildSysIniPath` | `TermsrvSyncUserIniFile +0xd2` |
| 9 | `TermsrvCopyIniFile` | `0xc1d78` `gpTermsrvCopyIniFile` | `TermsrvSyncUserIniFile +0x2f4` |
| 10 | `TermsrvGetString` | `0xc1d70` `gpTermsrvGetString` | `TermsrvIniSyncLoop +0x60` |
| 11 | `TermsrvLogInstallIniFile` | `0xc1d40` `gpTermsrvLogInstallIniFile` | `BaseDllReadWriteIniFileOnDisk +0x189` |

- `pfnTermsrvSyncUserIniFile` (`0xc1d08`) 를 읽는 곳은 `TermsrvSyncUserIniFileExt +0` (17 B: 칸을 읽어 NULL 이 아니면 CFG 로 간접 호출, 아니면 `RET`) **한 곳**. 그 함수를 부르는 코드는 `BaseDllReadWriteIniFileOnDisk +0x166` 하나, 거기에 kernel32 export 표 칸 (`0xa7dc8`) 도 있다 (`_refs/TermsrvSyncUserIniFileExt.tsv`).
- 겹침 주의: 이 12칸 + `pfnTermsrvSyncUserIniFile` 은 `InitializeRegTermsrvFpns` (run25·sast/63, baggage 2396행 "`pfnTermsrv…` 9칸") 가 채우는 9칸 (`0xc1cc0 ~ 0xc1d00`, 8바이트씩, `+0xb0`~`+0x18e`) 과 **서로 다른 칸**이다. `0xc1cb8` 는 이 함수 몫 (표 4번).
- Ghidra `.c` 주의: `IsSystemLUID.c` 는 반환값을 버리고 `void` 로 그렸다. 실제는 `.asm` `+0xac MOV EAX,1` (열기·조회 실패), `+0x8b XOR EAX,EAX` (SYSTEM 아님), `+0xb3 ~ +0xbb` (`HighPart == 0` 이면 1) 로 값을 돌려준다.

### 질문 2 — `RtlGetSuiteMask` 의 `0x10` 비트와 test.exe 의 갈래

- 답 `[정적]` + `[관찰]`:
  - `ntdll!RtlGetSuiteMask` (rva `0x5c820`) 는 **두 곳 중 하나를 읽는다** (`RtlGetSuiteMask.asm`): `+0x0` `GS:[0x60]` (PEB) → `+0x9` `[PEB+0x90]` (= `PEB.SharedData`, pdb_struct `_PEB`) → 그 포인터가 NULL (`+0x13 JZ`) 이거나 가리키는 첫 4바이트가 0 (`+0x15 CMP`, `+0x18 JZ`) 이면 `+0x2f` (`JZ` 목적지 `0x18005c84f − 0x18005c820`) 로 가서 **`[0x7ffe02d0]` = KUSD `SuiteMask`** 를 읽어 돌려준다 (`+0x36 RET`). 아니면 `[SharedData + 0x14]` (`+0x2a`) 를 돌려준다 (`+0x2d RET`). 즉 **KUSD 에서 오는 것이 기본**이고, `PEB.SharedData` 가 채워진 프로세스에선 다른 값이 올 수 있다.
  - 비트 `0x10` = `VER_SUITE_TERMINAL` (`SuiteMask` 값 `0x110` = `0x10` TERMINAL + `0x100` SINGLEUSERTS). 이름 풀이는 `IsTSAppCompatEnabled` (함수 안 오프셋) 가 `0x10` 과 `0x100` 을 따로 보는 것 (`+0x4d TEST byte … ,0x10`, `+0x57 MOV EAX,0x100` / `+0x5c TEST` / `+0x64 JNZ`) 과 `GetVersionExW` 구조체 칸 위치 (`wSuiteMask` = 구조체 `+0x118`) 로 맞춘다 (이름은 `[추측]`, 값 `0x110` 의 뜻은 로그의 `suite: TerminalServer SingleUserTS` 와 일치).
  - **test.exe 를 돌린 VM 값** `[관찰]` (기존 로그, 이번에 새로 돌린 것 아님):
    - KUSD `SuiteMask = 0x110`: `ms/logs/04_baggage-read_P1a.txt:2922`, `P1b:4040`, `P2:4052`. `vertarget` 줄 `suite: TerminalServer SingleUserTS` (`P1a:6`).
    - `PEB.SharedData = (null)`: `P1a:1764` (+ `3338~3340` 으로 직접 읽음), `P1b:2462`, `P2:2474`.
    - P1a = `LdrpInitializeProcess +0x199d`, P1b = `+0x1bb6` (`03_baggage-snap_result.md`). 문제의 호출 자리 `+0x1b59` 는 둘 **사이**라서, 호출 앞·뒤 모두 `SharedData` 가 NULL 이고 KUSD 값이 같다 → `BaseThreadInitThunk +0x26` 의 결과는 `0x110` 으로 보는 것이 맞다 (`AL = 0x10`, `+0x32 TEST AL,0x10` 켜짐, `+0x34 JZ` 안 탐).
  - 그래서 **test.exe 는 `BasepInitializeTermsrvFpns` 를 부르는 갈래를 탄다 — 예** (입력 값은 `[관찰]`, 길 계산은 `[정적]`). 실제로 그 호출이 일어났다는 직접 관찰은 없다 → `[VM]` (아래 "동적으로 확인할 것" 1).
  - 안으로 들어가서는 위 표대로 **`+0x2b` 로 나간다** (test.exe `DllCharacteristics = 0x8160`: 파일 `sample/bin/test.exe` 의 PE 헤더에서 읽음 `[정적]`, 실행 때 `_SECTION_IMAGE_INFORMATION.DllCharacteristics = 0x8160` `[관찰]` `P1a:1634` · `P1b:2332`, `TransferAddress` 가 `…1d40` = EP `0x1d40` 과 같음). 서버가 아니라서 (`SINGLEUSERTS` 켜짐, Education 클라이언트) TS 인식 비트가 꺼진 exe 였어도 `IsTSAppCompatEnabled` 가 0 을 줘서 `tsappcmp.dll` 은 안 올렸을 것이다 (`+0x57 ~ +0x64`: `SINGLEUSERTS` 켜지면 0).
- 근거: `ms/disassemble/ntdll/RtlGetSuiteMask.asm`, `ms/disassemble/kernel32/IsTSAppCompatEnabled.asm`, `IsTerminalServerCompatible.asm`, 위 로그 줄.

### 질문 3 — `BaseThreadInitThunk(1, 0, 0)` 의 나머지

- 답 `[정적]`: 첫 인자 ≠ 0 갈래 (`+0xa JNZ` 목적지 `0x18002cd96 − 0x18002cd70 = +0x26`) 에는 **`RtlGetSuiteMask` (`+0x26`/`+0x2d`) 와 `BasepInitializeTermsrvFpns` (`+0x36`/`+0x3b`) 말고 부르는 것이 없다.** `_calls.tsv` 의 `BaseThreadInitThunk` 줄은 4개이고 (`+0x12` CFG 간접 · `+0x19` `RtlExitUserThread` 는 첫 인자 0 갈래) 이 갈래엔 위 둘뿐이다. 시작 함수 (`DllMain` 이나 exe 시작) 는 이 갈래에서 안 부른다 (`RDX`·`R8` 은 이 갈래에서 안 쓰임).
- 돌아오는 자리:
  - `+0x34 JZ` (비트 꺼짐) 목적지 `+0x49` (`0x18002cdb9 − …`) → `+0x49 XOR EAX,EAX` → `+0x4b JMP` 목적지 `+0x42` → `+0x42 ADD RSP,0x20` · `+0x46 POP RBX` · `+0x47 RET` (0 반환).
  - 비트 켜짐: `+0x3b TEST EAX,EAX` → `+0x3d CMOVS EBX,EAX` (음수면 `EBX` 에 담음, 아니면 `EBX` = `+0x6 XOR EBX,EBX` 의 0 그대로) → `+0x40 MOV EAX,EBX` → `+0x47 RET`. 즉 **반환값은 0 또는 `BasepInitializeTermsrvFpns` 가 준 음수 NTSTATUS** 뿐이다.
  - test.exe: `BasepInitializeTermsrvFpns` 가 0 을 줌 → `BaseThreadInitThunk` 도 0 → ntdll `LdrpInitializeProcess +0x1b5e` 에서 `+0x1b64 JS` 안 탐 → `+0x1b72 LdrpInitializePerUserWindowsDirectory` 로 이어진다.
- 짐 지도에 넣을 전역 칸: **생긴다. 1칸 (kernel32 `.data`)**
  - `pfnTermsrvSyncUserIniFile` — kernel32 rva `0xc1d08`, 8 B, 공간 = 유저 (kernel32 데이터), 단계 ② (`LdrpInitializeProcess +0x1b59` 호출 안, 곧 P1a ~ P1b 사이), 놓는 법 = 직접 (`BasepInitializeTermsrvFpns +0x1b`), 값 = kernel32 베이스 + `0x72b60` (`TermsrvSyncUserIniFile`), 경로 = `LdrpInitializeProcess +0x1b59 → BaseThreadInitThunk +0x36 → BasepInitializeTermsrvFpns +0x1b`. test.exe 에서 **무조건** 쓰인다 (`+0x1b` 는 `+0x29 JZ` 보다 앞).
  - 나머지 12칸 (위 표) 과 `InitializeRegTermsrvFpns` 9칸은 test.exe 에서 **0 그대로** (안 쓰임). baggage.md 2396행은 "`pfnTermsrv…` 9칸 … NULL·0 그대로" 라고만 적혀 있어서 이 12칸 (+ `0xc1cb8`·`0xc1d10`·`0xc1d18`) 과 `0xc1d08` 은 빠져 있다 (grep 으로 `pfnTermsrvSyncUserIniFile`·`BasepInitializeTermsrvFpns` 를 baggage.md 에서 찾았으나 없음).
  - 이 칸은 값이 "주소" 라서 에뮬레이터에선 kernel32 가 올라온 자리 + `0x72b60` 이 들어가야 같다. 아직 사진으로 안 봤다 → `[VM]` (동적 2).

---

## Q60 — `KERNEL32!LoadLibraryExW` → `KERNELBASE!LoadLibraryExW`

### 질문 4 — kernel32 `LoadLibraryExW` 스텁 바이트·IAT 칸·export 표

- 답 `[정적]`:

| 함수 (kernel32) | rva · 크기 | 바이트 (PE 에서 직접) | 명령 | IAT 칸 (kernel32) | import 이름 (칸의 주인) |
|---|---|---|---|---|---|
| `LoadLibraryExW` | `0x3bd50` · 7 B (+ `CC` 패딩) | `48 FF 25 91 F6 04 00` | `JMP qword ptr [rip+0x4f691]` → `0x3bd50+7+0x4f691 = 0x8b3e8` | `0x8b3e8` | `api-ms-win-core-libraryloader-l1-2-0.dll!LoadLibraryExW` (이름으로 import) |
| `LoadLibraryW` | `0x3ee30` · 7 B | `48 FF 25 C9 C5 04 00` | `JMP [rip+0x4c5c9]` → `0x8b400` | `0x8b400` | `api-ms-win-core-libraryloader-l1-2-1.dll!LoadLibraryW` |
| `LoadLibraryA` | `0x428f0` · 7 B | `48 FF 25 11 8B 04 00` | `JMP [rip+0x48b11]` → `0x8b408` | `0x8b408` | `api-ms-win-core-libraryloader-l1-2-1.dll!LoadLibraryA` |

  - kernel32 export 표: 디렉터리 rva `0xa65f0` ~ `0xb535c` (크기 `0xed6c`). `LoadLibraryExW` = 서수 1020, 주소 칸 값 `0x3bd50` — **디렉터리 범위 밖의 코드 rva** 라서 forwarder 문자열이 아니다. `LoadLibraryW` (서수 1021, `0x3ee30`) · `LoadLibraryA` (서수 1018, `0x428f0`) 도 같다. `forwarder = None` (pefile).
  - 그 칸이 `KERNELBASE.dll!LoadLibraryExW` import 인가: **파일 속 이름은 `KERNELBASE.dll` 이 아니라 ApiSet 이름이다.** kernel32 의 import 에는 `api-ms-win-core-libraryloader-l1-2-0.dll` (24개 함수 묶음, `LoadLibraryExW` 포함), `…l1-2-1.dll` (3개: `LoadLibraryW`·`LoadLibraryA`·`FindResourceW`), `…l1-2-2/3`, `…l2-1-0` 이 있다. 로더가 올릴 때 ApiSet 표로 바꿔 준다.
  - ApiSet 풀이 (apisetschema.dll `.apiset` v6 표, 984개 항목): 항목 `api-ms-win-core-libraryloader-l1-2-3` (해시 길이 34글자 = `…-l1-2` 까지) → 값 **하나, 이름 없는 기본값 `kernelbase.dll`**. 끝 버전 숫자 (`-0`·`-1`) 는 찾을 때 안 보는 규칙이라 `l1-2-0`·`l1-2-1` 둘 다 이 항목으로 간다 (규칙대로 풀었고 ntdll 의 `ApiSetpSearchForApiSet` 코드는 이번에 안 읽음 `[추측]`; 최종 결과는 run7 `[관찰]` `KERNELBASE!LoadLibraryExW` 콜스택이 받쳐 줌).
  - kernelbase export (PE 표): `LoadLibraryExW` rva `0x21150` (480 B, forwarder 아님), `LoadLibraryW` `0xfc5b0` (10 B), `LoadLibraryA` `0xefd00` (411 B). kernelbase `.asm` 은 `ms/disassemble/kernelbase/LoadLibraryExW.asm` 만 이전부터 있다 (이번엔 안 뽑음).
- 근거: `ms/disassemble/kernel32/LoadLibraryExW.asm` (`+0x0 JMP qword ptr [0x18008b3e8]`), `LoadLibraryW.asm` (`[0x18008b400]`), `LoadLibraryA.asm` (`[0x18008b408]`), PE import·export 표, `apisetschema.dll`.
- 주의: `ms/decomp/kernel32/LoadLibraryExW.c` 는 Ghidra 가 이 JMP 를 "자기 자신을 부르는 함수" 로 그렸다 (`pHVar1 = LoadLibraryExW(...)`). 스텁이라 생긴 모양일 뿐이다 (`.asm` 이 맞다).
- test.exe 쪽: import `KERNEL32.dll!LoadLibraryExW` (이름으로), IAT `0x6e250` (`sample/bin/test.exe` PE 에서 확인, 요청서 값과 같음).

### 질문 5 — test.exe → kernel32 스텁 → kernelbase 한 번 `JMP` 가 맞나

- 답 `[정적]`: **맞다.** test.exe IAT `0x6e250` 는 kernel32 export `LoadLibraryExW` (`+0x3bd50`) 로 채워지고, 거기서 `JMP [kernel32 IAT 0x8b3e8]` 한 번으로 `KERNELBASE!LoadLibraryExW` (`+0x21150`) 에 넘어간다. `CALL` 이 아니라 `JMP` 라서 **돌아올 주소가 안 쌓이고**, kernelbase 함수가 `RET` 하면 곧장 test.exe 로 돌아간다 → 콜스택에 kernel32 프레임이 안 남는다. run7 `[관찰]` (`KERNELBASE!LoadLibraryExW ← 0x7ff70ee67723` = test.exe 안, `runs/01_sprint/run7-user.md:37`) 과 맞는다.
- 덧붙임: kernel32 안 코드 (예: `BasepInitializeTermsrvFpns +0x6b`, `InitializeRegTermsrvFpns +0x78`) 는 자기 스텁이 아니라 **같은 IAT 칸 `0x8b3e8` 를 직접 부른다** (스텁을 안 거침).

### 질문 6 — `LoadLibraryW`·`LoadLibraryA` 도 스텁인가

- 답 `[정적]`: **둘 다 O.** 각각 7 B `JMP [rip+X]` (위 표), kernelbase 쪽으로만 넘긴다. forwarder 아님.
- 덤: kernelbase `LoadLibraryW` (`0xfc5b0`, 10 B) 는 바이트 `45 33 C0 33 D2 E9 …` = `xor r8d,r8d; xor edx,edx; jmp kernelbase!LoadLibraryExW` (`0xfc5b0+10+rel = 0x21150` 로 계산 맞음) 라서 `LoadLibraryW(x)` = `LoadLibraryExW(x, NULL, 0)`. 이건 PE 바이트로만 본 것 (`.asm` 은 안 뽑음). `LoadLibraryA` (`0xefd00`) 는 411 B 짜리 진짜 함수.

---

## 동적으로 확인해야 할 것

1. **`[VM]` `BasepInitializeTermsrvFpns` 가 실제로 불리고 `+0x2d` 로 나가나**: `BaseThreadInitThunk +0x2d` 에서 `al` 이 `0x10` 켜짐 (정적 계산은 `0x110`), `BasepInitializeTermsrvFpns +0x29` 에서 `JZ` 안 탐, 돌아온 뒤 `BaseThreadInitThunk +0x3b` 의 `eax = 0`. (한 번만 서야 한다 — 새 스레드는 인자 0 이라 안 들어옴.)
2. **`[VM]` `pfnTermsrvSyncUserIniFile` 값**: `dq kernel32+0xc1d08 L1` — P1a (호출 앞) 에서 0, P1b·P2 (호출 뒤) 에서 `kernel32+0x72b60` 이면 맞다. 이미 있는 사진(`ms/dump/` P1a·P1b·P2)에서 읽을 수 있으면 새로 돌리지 않아도 된다. 같이 `dq kernel32+0xc1cb8 L0x19` 로 12칸·9칸이 0 인지 (`0xc1d08` 만 값).
3. 추천 멈춤점 (새로 돌릴 때): `kernel32!BasepInitializeTermsrvFpns` 입구 (test.exe 조건 필요, 한 번) 와 `kernel32!BasepInitializeTermsrvFpns+0x2d` (나가는 자리, `eax`). 인자·`rcx` 값 (`ntdll!Kernel32ThreadInitThunkFunction` 호출 자리 `LdrpInitializeProcess +0x1b59` 에서 `ecx=1, edx=0, r8=0`) 도 한 줄로.
4. 이 요청 범위 밖이라 안 한 것: `BaseThreadInitThunk +0x12` 의 `indirect:cfg:?` 는 `RAX` 가 `+0xf MOV RAX,RDX` 로 채워지는 레지스터 이동이라 이름이 없다 (run25 `[정적]` 대로 시작 함수 간접 호출).

## 다음 요청 후보

- (문서, flow-writer) baggage.md 에 `pfnTermsrvSyncUserIniFile` (kernel32 rva `0xc1d08`) 한 줄 추가, 2396행 "9칸" 설명에 "`BasepInitializeTermsrvFpns` 12칸은 별개, test.exe 0 그대로" 보충. flow.md 2.10 · 2.4/2.9 공백 (Q59·Q60) 닫기: "`(1,0,0)` = `BaseThreadInitThunk` `[정적]` 확정", "`LoadLibraryExW` 스텁 = `JMP` 한 번".
- (정적, 작음, 선택) `LdrpInitializeProcess +0x1b72 LdrpInitializePerUserWindowsDirectory` — `BaseThreadInitThunk` 바로 뒤에 불리고 flow.md 2.6 에 "뜻 모름" 으로 남아 있다 (`.asm` 아직 없음, `RtlpSystem32Dirs` 를 다시 쓰는 줄이 있다고 run39 refs 에 적힘).
- (정적, 필요할 때만) kernelbase `LoadLibraryW`·`LoadLibraryA` `.asm` — 이번엔 PE 바이트만 봤다. test.exe 는 `LoadLibraryExW` 만 쓰므로 급하지 않다.
- (동적) 위 1·2번. 이미 있는 사진으로 되면 dast 요청 없이 flow-writer 가 `ms/dump` 읽기만 부탁하면 된다.

## 판정

**분석성공** — Q59 (질문 1·2·3) · Q60 (질문 4·5·6) 모두 근거 줄 (`.asm` 오프셋, PE 표 값, 기존 로그 줄) 과 함께 답했다. 못 정한 것은 "test.exe 에서 이 호출이 실제로 일어났나" 와 `pfnTermsrvSyncUserIniFile` 의 실제 값 둘이고, 둘 다 `[VM]` 으로 넘겼다.

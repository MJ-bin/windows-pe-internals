# 2026-11-15 run101 — kernel32 남은 둘: `BaseThreadInitThunk(1,0,0)` 갈래 안 `BasepInitializeTermsrvFpns` 와 `KERNEL32!LoadLibraryExW` 스텁 (Q59 + Q60, 정적만, 짐 지도 바퀴): **`LdrpInitializeProcess +0x1b59` 의 `Kernel32ThreadInitThunkFunction(1,0,0)` 은 `BaseThreadInitThunk(1,0,0)` 이다 `[정적]` (칸 값은 `LdrpInitializeKernel32Functions +0xfa` 가 이름 `BaseThreadInitThunk` 로 찾은 주소, run44 `[관찰]` 값 `KERNEL32+0x2cd70`).** 그 갈래는 `+0x26 RtlGetSuiteMask` → (`AL & 0x10`) `+0x36 BasepInitializeTermsrvFpns` 둘만 부른다. test.exe VM 은 KUSD `SuiteMask = 0x110` (`[관찰]` run44) 이라 안으로 들어가고, 들어가자마자 **`+0x1b` 에서 kernel32 전역 `pfnTermsrvSyncUserIniFile` (rva `0xc1d08`) ← kernel32 + `0x72b60` 을 무조건 쓴 뒤** `+0x22 IsTerminalServerCompatible` 이 1 (test.exe `DllCharacteristics 0x8160`, 비트 15 = TS 인식) 이라 `+0x2b` 에서 0 을 돌려주고 나간다 — `tsappcmp.dll` 은 안 올리고, 나머지 함수 포인터 12칸은 0 그대로. **새 짐 1칸.** Q60: **`KERNEL32!LoadLibraryExW` 는 forwarder 가 아니라 7바이트 `JMP [rip+0x4f691]` 스텁** → kernel32 IAT `0x8b3e8` = `api-ms-win-core-libraryloader-l1-2-0.dll!LoadLibraryExW` → ApiSet `kernelbase.dll` → `KERNELBASE!LoadLibraryExW` (rva `0x21150`). `CALL` 이 아니라 `JMP` 라 콜스택에 kernel32 프레임이 안 남는다 — run7 `[관찰]` 과 맞는다. `LoadLibraryW` · `LoadLibraryA` 도 같은 꼴.

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 값 대조는 run44 사진 덤프 읽기 로그를 다시 읽은 것뿐이다 (아래 "사진 로그 대조").
- 정적: **kernel32 pdb `AE9E9D8F7AC1467A645320FDC23235D01`** (기준과 같음). kernel32 은 이미 analyze 되어 있어 export 만 새로 했다 (결과서).
  - 버전 대조 (결과서): ntdll `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` 그대로, kernel32 `BaseThreadInitThunk` rva `0x2cd70` = run44 `[관찰]` 칸 값 `KERNEL32+0x2cd70`, `+0x26/+0x2d` · `+0x36/+0x3b` = run25 그대로.
  - ntdll · kernelbase · apisetschema 는 읽기만 (있던 `.asm`, PE 표).
- 다른 decompiler 가 같은 시간에 ntdll (sast/103) · ntoskrnl (sast/104) 을 돌리는 중이다. sast/98 ~ 104 결과는 이 문서에 안 넣었다.

## 목적

- **Q59**: run25 에서 이름만 본 `BaseThreadInitThunk` 첫 인자 ≠ 0 갈래 — `BasepInitializeTermsrvFpns` 가 무엇을 하고 어느 전역을 쓰나, test.exe 가 이 갈래를 타나, 짐 지도에 넣을 칸이 생기나.
- **Q60**: test.exe 가 import 하는 `KERNEL32!LoadLibraryExW` 가 run7 콜스택의 `KERNELBASE!LoadLibraryExW` 로 어떻게 가나 (forwarder 인가, 스텁인가).
  - run30 해설서가 짚은 "근거끼리 어긋남" (run30 호출 지점 표는 `KERNEL32!LoadLibraryExW`, 호출 관계 트리 · flow.md 는 `KERNELBASE!LoadLibraryExW`) 을 이걸로 맞춘다.
- 앞 바퀴 짝: run25 (`BaseThreadInitThunk` depth 1 이름만), run30 (Q18 범위 — test.exe 쪽 `LoadLibraryExW` 호출 자리), run7 (`KERNELBASE!LoadLibraryExW` 콜스택 `[관찰]`).

## 방법

- 정적: `_workspace/sast/97_k32-leftovers.md` — kernel32
  - depth 1: `^BasepInitializeTermsrvFpns$` → 4개 (`BasepInitializeTermsrvFpns` · `IsTerminalServerCompatible` · `IsSystemLUID` · `IsTSAppCompatEnabled`)
  - depth 0: `^(BaseThreadInitThunk|LoadLibraryExW|LoadLibraryW|LoadLibraryA)$` → 4개, 덤으로 `^TermsrvSyncUserIniFileExt$` (칸을 읽는 곳)
  - `refs`: `0xc1cb8` (`0xd0` 바이트 — 함수 포인터 칸 묶음 전부), `TermsrvSyncUserIniFileExt` → `ms/disassemble/kernel32/_refs/`
  - ntdll 은 있던 것 읽기만: `RtlGetSuiteMask.asm` · `LdrpInitializeProcess.asm` · `LdrpInitializeKernel32Functions.asm` · `RtlUserThreadStart.asm` · `RtlImageNtHeader.asm` · `_refs/Kernel32ThreadInitThunkFunction.tsv`
  - PE 표 (export · import · IAT · ApiSet `.apiset`) 는 결과서가 `pefile` 로 직접 읽었다 (임시 스크립트, 레포 밖).
- 결과서: `_workspace/sast/97_k32-leftovers_result.md` (**분석성공** — `[VM]` 으로 넘긴 것 둘: 이 호출이 실제로 일어났나, `pfnTermsrvSyncUserIniFile` 실제 값).
- flow-writer 가 다시 본 것:
  - `.asm`: `BaseThreadInitThunk` (`+0x8` · `+0xa` · `+0x26` · `+0x32` · `+0x34` · `+0x36` · `+0x3b` · `+0x3d` · `+0x40` · `+0x47` · `+0x49` · `+0x4b`), `BasepInitializeTermsrvFpns` (`+0xf` · `+0x14` · `+0x1b` · `+0x22` · `+0x29` · `+0x2b` · `+0x3e` · `+0x45` · `+0x4c` · `+0x53` · `+0x5a` · `+0x5e` · `+0x65` · `+0x6b` · `+0x7d` · `+0x88` · `+0x8e` · `+0x92` · `+0x99`), `IsTerminalServerCompatible` (`+0x4` · `+0xd` · `+0x11` · `+0x27` · `+0x2b`), `LoadLibraryExW` · `LoadLibraryW` · `LoadLibraryA` (`+0x0` 한 줄씩), `TermsrvSyncUserIniFileExt` (`+0x0` · `+0xa`), ntdll `RtlGetSuiteMask` (`+0x9` · `+0x13` · `+0x18` · `+0x2a` · `+0x2f`).
  - `_calls.tsv` (kernel32): `BaseThreadInitThunk` 4줄, `BasepInitializeTermsrvFpns` 5줄, `IsSystemLUID` 4줄, `IsTSAppCompatEnabled` 3줄, `IsTerminalServerCompatible` 1줄. `alias:` 없음.
  - `_refs/0xc1cb8.tsv` (`0xc1d08` 쓰기 `BasepInitializeTermsrvFpns +0x1b` 하나 · 읽기 `TermsrvSyncUserIniFileExt +0x0` 하나, `0xc1d10` 은 읽기 둘 + 표 칸 하나 — 쓰는 줄 없음 = 루프가 간접으로 씀), `_refs/TermsrvSyncUserIniFileExt.tsv` (부르는 곳 `BaseDllReadWriteIniFileOnDisk +0x166` 하나 + export 표 칸 `0xa7dc8` + CFG 표 칸).
  - 결과서 값과 다른 줄 없음.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/kernel32/_calls.tsv` · ntdll `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeKernel32Functions` | `+0xfa` | `+0xff` | `ntdll!LdrGetProcedureAddressForCaller(kernel32, "BaseThreadInitThunk", 0, &Kernel32ThreadInitThunkFunction, …)` | 칸 `0x1d2760` 을 채움 (`+0xe4` 가 칸 주소를 실음). 이름 = ntdll rva `0x173ba8` ANSI_STRING (길이 19) | [정적] (run20 그대로) + 값 `KERNEL32+0x2cd70` [26100.9457 확인] run44 |
| `ntdll!LdrpInitializeProcess` | `+0x1b59` | `+0x1b5e` | 칸 `Kernel32ThreadInitThunkFunction` (CFG 간접) **= `KERNEL32!BaseThreadInitThunk`** | 칸 읽기 `+0x1b45`, NULL 이면 `+0x1b4f JZ` 로 건너뜀. `ECX = 1` (`+0x1b56 LEA ECX,[RDX+1]`), `EDX = 0` (`+0x1b54`), `R8 = 0` (`+0x1b51`). 돌아와 `+0x1b64 JS` → 실패 꼬리, 아니면 `+0x1b72 LdrpInitializePerUserWindowsDirectory` | [정적] (대상 이름은 이번에 닫힘) |
| `ntdll!RtlUserThreadStart` | `+0x27` | `+0x2c` | 같은 칸 (CFG 간접) | `+0x25 XOR ECX,ECX` → 첫 인자 **0**. 이 칸을 읽는 ntdll 코드는 이 둘뿐 (`_refs`) | [정적] (run29 그대로) |
| `KERNEL32!BaseThreadInitThunk` | `+0x26` | `+0x2d` | `ntdll!RtlGetSuiteMask` | 첫 인자 ≠ 0 (`+0x8 TEST ECX,ECX` · `+0xa JNZ` → `+0x26`) | [정적] (run25 그대로) |
| `KERNEL32!BaseThreadInitThunk` | `+0x36` | `+0x3b` | `KERNEL32!BasepInitializeTermsrvFpns` | `+0x32 TEST AL,0x10` · `+0x34 JZ +0x49` 안 탈 때 | [정적] (run25 그대로) |
| `KERNEL32!BasepInitializeTermsrvFpns` | `+0x22` | `+0x27` | `KERNEL32!IsTerminalServerCompatible` | 무조건 (`+0x1b` 칸 쓰기 뒤) | [정적] |
| `KERNEL32!BasepInitializeTermsrvFpns` | `+0x3e` | `+0x43` | `KERNEL32!IsSystemLUID` | 위가 0 일 때 (`+0x29 JZ`) — test.exe 안 탐 | [정적] |
| `KERNEL32!BasepInitializeTermsrvFpns` | `+0x4c` | `+0x51` | `KERNEL32!IsTSAppCompatEnabled(&지역)` | SYSTEM 이 아닐 때 (`+0x45 JNZ` 안 탐) — test.exe 안 탐 | [정적] |
| `KERNEL32!BasepInitializeTermsrvFpns` | `+0x6b` | `+0x72` | IAT `0x8b3e8` (`LoadLibraryExW`) → `KERNELBASE!LoadLibraryExW(L"tsappcmp.dll", NULL, 0x800)` | 지역 ≠ 0 일 때 (`+0x5a JZ` 안 탐). 글자 상수 kernel32 rva `0x92b28`. 자기 스텁이 아니라 IAT 칸을 직접 부름 — test.exe 안 탐 | [정적] |
| `KERNEL32!BasepInitializeTermsrvFpns` | `+0xa7` | `+0xae` | IAT `GetProcAddress` (`api-ms-win-core-libraryloader-l1-2-0`) | 12바퀴 루프 (`+0x90 ~ +0xc5`), 결과를 칸에 `+0xbb` — test.exe 안 탐 | [정적] |
| `KERNEL32!IsTerminalServerCompatible` | `+0x11` | `+0x18` | `ntdll!RtlImageNtHeader` (exe = `PEB.ImageBaseAddress`, `+0x4` · `+0xd`) | 그 뒤 `+0x27 CMP word [NT헤더+0x5e],0` · `+0x2b SETL` = `DllCharacteristics` 비트 15 | [정적] |
| `KERNEL32!IsSystemLUID` | `+0x33` · `+0x5e` · `+0x71` | `+0x3a` · `+0x65` · `+0x78` | `NtOpenProcessToken(-1, 8)` · `NtQueryInformationToken(…, 10, …, 0x38)` · `NtClose` | test.exe 안 탐 | [정적] |
| `KERNEL32!IsTSAppCompatEnabled` | `+0x3d` | `+0x44` | `GetVersionExW` | test.exe 안 탐 | [정적] |
| `KERNEL32!TermsrvSyncUserIniFileExt` | `+0xa` (`JNZ`) | — | CFG 중개 `0x180087010` → 칸 `pfnTermsrvSyncUserIniFile` 의 함수 | `+0x0` 칸 읽기, NULL 이면 `+0x10 RET`. 부르는 곳은 `BaseDllReadWriteIniFileOnDisk +0x166` 하나 (+ export 표 칸) — 은퇴 뒤 ini 파일 API 길 | [정적] |

### 정적 — Q60 스텁 (`.asm` + PE 표, 결과서)

| 함수 (kernel32) | rva · 크기 | 바이트 | 명령 | kernel32 IAT 칸 | import 이름 | → ApiSet → 실제 |
|---|---|---|---|---|---|---|
| `LoadLibraryExW` | `0x3bd50` · 7B | `48 FF 25 91 F6 04 00` | `JMP qword ptr [0x18008b3e8]` | `0x8b3e8` | `api-ms-win-core-libraryloader-l1-2-0.dll!LoadLibraryExW` | `kernelbase.dll` → `KERNELBASE!LoadLibraryExW` rva `0x21150` (480B, forwarder 아님) |
| `LoadLibraryW` | `0x3ee30` · 7B | `48 FF 25 C9 C5 04 00` | `JMP qword ptr [0x18008b400]` | `0x8b400` | `api-ms-win-core-libraryloader-l1-2-1.dll!LoadLibraryW` | `KERNELBASE!LoadLibraryW` rva `0xfc5b0` (10B) |
| `LoadLibraryA` | `0x428f0` · 7B | `48 FF 25 11 8B 04 00` | `JMP qword ptr [0x18008b408]` | `0x8b408` | `api-ms-win-core-libraryloader-l1-2-1.dll!LoadLibraryA` | `KERNELBASE!LoadLibraryA` rva `0xefd00` (411B) |

- export 표: kernel32 export 디렉터리 rva `0xa65f0 ~ 0xb535c`. `LoadLibraryExW` (서수 1020) 의 주소 칸 값 `0x3bd50` 은 디렉터리 범위 밖 = 코드 rva → **forwarder 문자열이 아니다** (pefile `forwarder = None`). `LoadLibraryW` (1021) · `LoadLibraryA` (1018) 도 같다 `[정적: PE 표]`.
- ApiSet: `apisetschema.dll` `.apiset` v6 표 (984 항목) 의 `api-ms-win-core-libraryloader-l1-2-3` 항목 → 이름 없는 기본값 하나 `kernelbase.dll`. 끝 버전 숫자는 찾을 때 안 보는 규칙이라 `l1-2-0` · `l1-2-1` 도 이 항목으로 간다 — 이 규칙은 결과서가 일반 규칙대로 푼 것이고 ntdll `ApiSetpSearchForApiSet` 코드는 이번에 안 읽었다 `[추측]`. 최종 결과 (kernelbase) 는 run7 `[관찰]` 콜스택이 받쳐 준다.
- test.exe 쪽: import `KERNEL32.dll!LoadLibraryExW` (이름으로), IAT `0x6e250` (run30 · 결과서 PE 확인).
- 덤 (PE 바이트로만, `.asm` 안 뽑음): `KERNELBASE!LoadLibraryW` 는 `xor r8d,r8d; xor edx,edx; jmp KERNELBASE!LoadLibraryExW` → `LoadLibraryW(x)` = `LoadLibraryExW(x, NULL, 0)` `[정적: PE 바이트]`.

### `BasepInitializeTermsrvFpns` 가 쓰는 칸 (결과서 질문 1 · 3)

| 칸 (kernel32 `.data`, PDB 이름) | rva | 쓰는 줄 | 값 | test.exe |
|---|---|---|---|---|
| **`pfnTermsrvSyncUserIniFile`** | `0xc1d08` | `+0x14 LEA RAX,[0x180072b60]` · **`+0x1b MOV [0x1800c1d08],RAX`** (`+0x29` 갈림보다 앞 — 무조건) | kernel32 + `0x72b60` (= `TermsrvSyncUserIniFile`, 1386B) | **씀** |
| `gpTermsrvGetComputerName` · `gpTermsrvAdjustPhyMemLimits` · `pfnTermsrvGetWindowsDirectoryA` · `pfnTermsrvGetWindowsDirectoryW` · `pfnTermsrvConvertSysRootToUserDir` · `gpTermsrvBuildIniFileName` · `gpTermsrvCORIniFile` · `gpGetTermsrCompatFlags` · `gpTermsrvBuildSysIniPath` · `gpTermsrvCopyIniFile` · `gpTermsrvGetString` · `gpTermsrvLogInstallIniFile` (12칸) | `0xc1d30` · `0xc1d50` · `0xc1d18` · `0xc1d10` · `0xc1cb8` · `0xc1d48` · `0xc1d38` · `0xc1d58` · `0xc1d68` · `0xc1d78` · `0xc1d70` · `0xc1d40` | `+0xbb MOV [RCX],RAX` (루프, 표 kernel32 `.rdata` rva `0x89310`, 16B × 12 = `{이름, 칸 주소}`) | `tsappcmp.dll` 의 같은 이름 함수 | **0 그대로** (루프 안 탐) |

- 12칸을 읽는 곳 (이름만, `_refs/0xc1cb8.tsv`): `GetComputerNameW` · `GlobalMemoryStatus` · `TermsrvGetWindowsDirectoryA/W` · `BaseDllFindIniFileNameMapping` · `TermsrvConvertSysRootToUserDir` · `BaseDllOpenIniFileOnDisk` ×2 · `TermsrvSyncUserIniFile` ×3 · `TermsrvIniSyncLoop` · `BaseDllReadWriteIniFileOnDisk`. 칸마다 짝은 결과서 표.
- **겹침 주의**: run68 의 `InitializeRegTermsrvFpns` 9칸 (`0xc1cc0 ~ 0xc1d00`, `_BaseDllInitialize +0x364`) 과 **서로 다른 칸**이다. `0xc1cb8` 은 이 함수 몫 (표 4번).
- `pfnTermsrvGetWindowsDirectoryW` (`0xc1d10`) 는 ntdll 이 이름으로 찾아 쓰는 `TermsrvGetWindowsDirectoryW` (run20 · run25) 가 읽는 칸이다. 쓰는 줄은 이 루프뿐이라 (`_refs`: 읽기 둘 + 표 칸 하나) **test.exe 에선 0 그대로** `[정적]` → run25 의 "칸을 채우는 쪽은 터미널 서비스 쪽일 것 `[추측]`" 이 `[정적]` 으로 닫힌다.

### 사진 로그 대조 (run44, 새로 돌린 것 아님)

| 무엇 | 값 | 로그 줄 | 표시 |
|---|---|---|---|
| KUSD `SuiteMask` (`0x7ffe02d0`) | `0x110` | `ms/logs/04_baggage-read_P1a.txt:2922` · `_P1b.txt:4040` · `_P2.txt:4052` | [26100.9457 확인] |
| `vertarget` | `suite: TerminalServer SingleUserTS` | `_P1a.txt:6` | [26100.9457 확인] |
| `PEB.SharedData` (`+0x90`) | `(null)` | `_P1a.txt:1764` (+ `3338 ~ 3340` 직접 읽기 0) · `_P1b.txt:2462` · `_P2.txt:2474` | [26100.9457 확인] |
| exe `_SECTION_IMAGE_INFORMATION.DllCharacteristics` | `0x8160` | `_P1a.txt:1634` · `_P1b.txt:2332` | [26100.9457 확인] |
| `Kernel32ThreadInitThunkFunction` | `KERNEL32+0x2cd70` | run44 (P1a ~ P1b 에 생김, baggage.md 3.8) | [26100.9457 확인] |
| kernel32 `.data` `pfnTermsrvSyncUserIniFile` | — | 사진 로그에 없음 (run44 는 ntdll `.data` 만 읽음) | [확인 필요] |

- P1a = `LdrpInitializeProcess +0x199d`, P1b = `+0x1bb6`. 호출 자리 `+0x1b59` 는 둘 **사이**라 앞뒤 모두 `SharedData` NULL · `SuiteMask 0x110` 이다. 그래서 `RtlGetSuiteMask` 는 `+0x13 JZ` → `+0x2f` KUSD 갈래로 `0x110` 을 돌려준다고 본다 (값 `[관찰]` + 길 `[정적]`).

### 호출 관계

Q59 (프로세스 초기화 때 한 번, 뒤7):

```
ntdll!LdrpInitializeKernel32Functions
 └─ +0xfa  LdrGetProcedureAddressForCaller(kernel32, "BaseThreadInitThunk") → 칸 Kernel32ThreadInitThunkFunction   [정적] run20 · 값 [관찰] run44
ntdll!LdrpInitializeProcess
 └─ +0x1b59 (간접, CFG)  칸(1, 0, 0) = KERNEL32!BaseThreadInitThunk                       [정적] run101
     ├─ (1) +0x26  ntdll!RtlGetSuiteMask    PEB.SharedData NULL → +0x2f KUSD SuiteMask = 0x110   [정적] + 값 [관찰] run44
     └─ (2) +0x36  KERNEL32!BasepInitializeTermsrvFpns     (AL & 0x10 켜짐)
             │  +0x1b  pfnTermsrvSyncUserIniFile ← kernel32+0x72b60      ← 새 짐 (무조건)
             ├─ (1) +0x22  IsTerminalServerCompatible → RtlImageNtHeader(exe) → DllCharacteristics 비트 15 → 1
             │      +0x29 JZ 안 탐 → +0x2b XOR EAX,EAX → RET 0                 ← test.exe 는 여기서 나감
             ├─ (2) +0x3e  IsSystemLUID → NtOpenProcessToken · NtQueryInformationToken · NtClose     (안 탐)
             ├─ (3) +0x4c  IsTSAppCompatEnabled → GetVersionExW                                       (안 탐)
             ├─ (4) +0x6b  [IAT 0x8b3e8] KERNELBASE!LoadLibraryExW("tsappcmp.dll", NULL, 0x800)     (안 탐)
             └─ (5) +0xa7  [IAT] GetProcAddress ×12 → 칸 12개                                         (안 탐)
     돌아옴: EAX = 0 (+0x3d CMOVS 안 탐) → LdrpInitializeProcess +0x1b64 JS 안 탐 → +0x1b72 LdrpInitializePerUserWindowsDirectory
```

Q60 (진입점 뒤, CRT 가 부름 — 지도 밖 CRT 안 사슬은 run30):

```
test!try_get_function_slow
 └─ +0x6d  CALL [test IAT 0x6e250]  = KERNEL32!LoadLibraryExW (7B 스텁)            [정적] run30 · run101
             ══▶ +0x0 JMP [kernel32 IAT 0x8b3e8]                                  [정적] run101
                  = api-ms-win-core-libraryloader-l1-2-0!LoadLibraryExW → (ApiSet) kernelbase.dll
                  KERNELBASE!LoadLibraryExW (rva 0x21150) → ntdll 로더 (LdrpLoadDll)   [관찰] run7
                  RET → 곧장 test!try_get_function_slow +0x73 (kernel32 프레임 없음)   [관찰] run7 ret …67723
```

## 확인된 것

- `[정적]` **`LdrpInitializeProcess +0x1b59` 의 `(1,0,0)` 호출 대상 = `KERNEL32!BaseThreadInitThunk`.** 칸 값은 kernel32 에서 이름 `BaseThreadInitThunk` 로 찾은 주소 (`LdrpInitializeKernel32Functions +0xfa`), 그 export 는 forwarder 아님 (run25), 실행 값 `KERNEL32+0x2cd70` `[관찰]` (run44). 남은 건 "이 호출이 실제로 일어났나" 의 직접 관찰뿐 (동적 1).
- `[정적]` 칸 `Kernel32ThreadInitThunkFunction` 을 읽는 ntdll 코드는 둘 — `LdrpInitializeProcess +0x1b45` (첫 인자 1) 와 `RtlUserThreadStart +0x7` (첫 인자 0). 그래서 첫 인자 ≠ 0 갈래는 **프로세스 초기화 때 한 번뿐**.
- `[정적]` 첫 인자 ≠ 0 갈래는 `RtlGetSuiteMask` 와 `BasepInitializeTermsrvFpns` 둘만 부른다. 반환값은 0 또는 `BasepInitializeTermsrvFpns` 가 준 음수 NTSTATUS 뿐 (`+0x3d CMOVS`).
- `[정적]` `RtlGetSuiteMask` 는 `PEB.SharedData` 가 NULL 이거나 첫 4바이트가 0 이면 KUSD `SuiteMask` (`0x7ffe02d0`) 를, 아니면 `[SharedData + 0x14]` 를 돌려준다. test.exe 는 `SharedData` NULL `[관찰]` → KUSD `0x110` `[관찰]` → 비트 `0x10` 켜짐 → 갈래를 탄다 (길 `[정적]`).
  - `0x10` = TERMINAL, `0x100` = SINGLEUSERTS 라는 이름은 `[추측]` (`IsTSAppCompatEnabled` 가 두 비트를 따로 보는 것과 `vertarget` 글 `TerminalServer SingleUserTS` 로 맞춤).
- `[정적]` `BasepInitializeTermsrvFpns` 는 **맨 앞에서 무조건** `pfnTermsrvSyncUserIniFile` ← kernel32 + `0x72b60`. 그 뒤 `IsTerminalServerCompatible` 이 1 이면 (exe `DllCharacteristics` 비트 15) 0 을 돌려주고 끝. test.exe `0x8160` `[정적+샘플]` · `[관찰]` run44 → 비트 15 켜짐 → **`+0x2b` 로 나감**. syscall 0, `tsappcmp.dll` 안 올림.
  - 서버가 아니라서 (SINGLEUSERTS 켜짐) TS 인식 비트가 꺼진 exe 였어도 `IsTSAppCompatEnabled` 가 지역 0 을 줘 `tsappcmp.dll` 은 안 올렸을 것이다 (`+0x55 ~ +0x5a`, 결과서 `IsTSAppCompatEnabled +0x57 ~ +0x64`).
- `[정적]` **새 짐 1칸**: kernel32 `.data` `pfnTermsrvSyncUserIniFile` (rva `0xc1d08`, 8B) — 유저, 단계 ② (P1a ~ P1b 사이), 놓는 법 직접, 쓴 주인 다른 DLL (KERNEL32), 경로 `LdrpInitializeProcess +0x1b59 → BaseThreadInitThunk (호출 +0x36) → BasepInitializeTermsrvFpns +0x1b`. 값은 주소라 kernel32 가 올라온 자리 + `0x72b60` (실제 값 `[VM]`). 읽는 곳 `TermsrvSyncUserIniFileExt +0x0` 하나 (부르는 곳 `BaseDllReadWriteIniFileOnDisk +0x166` — ini 파일 API, 은퇴 뒤).
- `[정적]` 12칸 (`gp…` · `pfn…`) 과 run68 `InitializeRegTermsrvFpns` 9칸은 서로 다른 칸이고 test.exe 에선 둘 다 **0 그대로**.
- `[정적]` **`KERNEL32!LoadLibraryExW` = 7B `JMP [IAT 0x8b3e8]` 스텁** (forwarder 아님), 그 칸 = `api-ms-win-core-libraryloader-l1-2-0.dll!LoadLibraryExW` → kernelbase. `JMP` 라 돌아올 주소가 안 쌓여 run7 `[관찰]` 콜스택 (`KERNELBASE!LoadLibraryExW` ← `test` 안 `…67723`) 에 kernel32 프레임이 없는 것과 맞다.
  - 그래서 run30 의 두 이름은 **둘 다 맞다**: 호출 지점 표의 `KERNEL32!LoadLibraryExW` 는 test.exe 가 **import 하는 이름**, 트리 · flow.md 의 `KERNELBASE!LoadLibraryExW` 는 **실행이 실제로 닿는 함수** (콜스택에 보이는 이름). 사이에 스텁 `JMP` 하나가 낀다.
  - kernel32 안 코드 (`BasepInitializeTermsrvFpns +0x6b`, run68 `InitializeRegTermsrvFpns +0x78`) 는 스텁을 거치지 않고 같은 IAT 칸을 직접 부른다.
- `[정적]` `LoadLibraryW` · `LoadLibraryA` 도 같은 꼴의 7B 스텁 (O · O).

## 결과서와 다르게 적은 곳 · 보탠 것

- 다르게 적은 곳: 없음.
- 보탠 것 (flow-writer, `_refs`):
  - `pfnTermsrvGetWindowsDirectoryW` (`0xc1d10`) 를 쓰는 줄이 루프뿐이라는 것 — run25 · flow.md 2.5 의 `TermsrvGetWindowsDirectoryW` `[추측]` 을 닫는 데 썼다. 이 칸을 ntdll `LdrpInitializeKernel32Functions` 가 찾아 `LdrpInitializePerUserWindowsDirectory` 로 넘기는 함수가 test.exe 에서 무엇을 돌려주는지는 이번에 안 봤다 (`TermsrvGetWindowsDirectoryW` 18B 의 "0 이면 그냥 돌아옴" 값 — run25 결과서 몫).
  - run30 이름 어긋남 풀이 (위 "확인된 것").

## 이전 run 에서 닫은 것

- run25 "아직 확인 필요" `BasepInitializeTermsrvFpns` 안쪽 (Q18 → Q59) — 닫힘.
- run30 "모듈 이름 주의" (`KERNEL32` vs `KERNELBASE`) · flow.md 2.10 `[확인 필요]` Q60 — 닫힘.
- 정정은 없다 (앞 문서가 틀린 게 아니라 비어 있던 것). 이전 문서 맨 위 정정 줄도 안 넣었다.

## 아직 확인 필요

### 동적 (`[VM]`, 결과서 "동적으로 확인해야 할 것")

1. **`BasepInitializeTermsrvFpns` 가 실제로 불리고 `+0x2b` 로 나가나** — test.exe 조건 멈춤점 `kernel32!BasepInitializeTermsrvFpns` 입구 (한 번만 서야 함) · `kernel32!BaseThreadInitThunk+0x2d` 에서 `r al` (`0x10` 켜짐 기대) · `+0x3b` 에서 `r eax` (0 기대). 호출 자리 `ntdll!LdrpInitializeProcess+0x1b59` 에서 `r ecx, edx, r8` (`1, 0, 0`).
2. **`pfnTermsrvSyncUserIniFile` 실제 값** — `dq kernel32+0xc1d08 L1`: P1a 에서 0, P1b · P2 에서 `kernel32+0x72b60` 기대. 같이 `dq kernel32+0xc1cb8 L0x19` 로 12칸 · 9칸 0, `0xc1d08` 만 값인지. **이미 있는 사진 덤프 (`ms/dump/03_baggage-snap/` P1a · P1b · P2) 에서 읽을 수 있으면 새로 돌리지 않아도 된다** (run88 처럼 서버 오프라인 읽기).
3. (Q30 · Q31 쪽 덤) `KERNEL32!LoadLibraryExW` 에 멈춤점을 걸면 서지만 `k` 엔 안 남는다 — `KERNELBASE!LoadLibraryExW` 입구 `k` 에서 바로 위가 `test!try_get_function_slow+0x73` 인지 (run7 그대로 기대).

### 정적 (결과서 "다음 요청 후보" + 보탬)

1. `ntdll!LdrpInitializeProcess +0x1b72 LdrpInitializePerUserWindowsDirectory` — `BaseThreadInitThunk(1,0,0)` 바로 뒤, flow.md 2.4 · 2.6 에 "뜻 모름" (`RtlpSystemDirs` · `RtlpSystem32Dirs` 를 다시 쓰는 줄, run39 refs). `.asm` 아직 없음. 넘겨받는 `TermsrvGetWindowsDirectoryW` (칸 `0xc1d10` 0) 의 반환값도 같이. **(해결됨 → run107, Q71)** — 칸 0 이라 `TermsrvGetWindowsDirectoryW` 가 0 을 주고, `LdrpInitializePerUserWindowsDirectory +0x4e` 에서 0 반환 → 아무것도 안 바꿈 `[정적]` (칸 값은 `[VM]`).
2. (필요할 때만) kernelbase `LoadLibraryW` · `LoadLibraryA` `.asm` — 이번엔 PE 바이트만. test.exe 는 `LoadLibraryExW` 만 쓴다.
3. (낮음) ntdll `ApiSetpSearchForApiSet` — "끝 버전 숫자는 안 본다" 규칙을 코드로 (지금 `[추측]`, 결과는 run7 `[관찰]` 이 받침).
4. (낮음) test.exe 의 다른 KERNEL32 import (`OutputDebugStringA` 등, flow.md 2.10 끝 `[확인 필요]`) 도 같은 스텁 꼴인지 — run36 forwarder 목록 밖이면 같은 꼴일 것 `[추측]`.

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (kernel32 · ntdll · kernelbase pdb 줄, 근거 로그 (run101)), 1장 그림 2 (`z1` → `KERNEL32!BaseThreadInitThunk (1,0,0)` 를 KERNEL32 상자로, 새 노드 `kt1` `BasepInitializeTermsrvFpns` · `ks1` `KERNEL32!LoadLibraryExW` 스텁) + 그림 메모, 2.4 (뒤7 트리 · 간접 호출 `+0x1b5e` 줄), 2.5 (`TermsrvGetWindowsDirectoryW` 칸), 2.10 (트리 · `BaseThreadInitThunk` 안쪽 · Q60 `[확인 필요]` · `OutputDebugStringA` 메모), 절 머리 run 목록 (그림 2 · 2.4 · 2.5 · 2.10), 4장 (2.4 · 2.9 줄, 2.10 줄 둘, 새 줄 둘), 4.1 (그림 2 회색 상자 줄 · 진입점 → `LoadLibraryExW` 줄), 5장 (run101 · sast/97).
- baggage.md: 7.6.8 표에 새 줄 1 (`pfnTermsrvSyncUserIniFile`) + 9칸 줄 보충 + 12칸 줄 (0 그대로), 짐 수 메모, 5장 ② run101 한 줄, 1장 메모, 새 7.33, 기준 · 8장. **체크리스트 밖 (다른 DLL `.data`) 이라 2장 숫자 그대로.**

## 디버거 메모 / 도구 메모

- Ghidra `.c` 주의 (결과서): `ms/decomp/kernel32/LoadLibraryExW.c` 는 `JMP [IAT]` 스텁을 "자기 자신을 부르는 함수" 로 그린다 — `.asm` 이 맞다. `IsSystemLUID.c` 는 반환값을 버린 `void` 로 그린다 — `.asm` `+0xac` · `+0x8b` · `+0xb3 ~ +0xbb` 로 값을 돌려준다. 새 함정은 아니고 run30 "import 이름으로 찾으면 0줄 — IAT 칸 rva 로 찾는다" 와 같은 무리라 pitfalls 제안은 안 한다.

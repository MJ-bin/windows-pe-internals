# 2026-11-16 (작성일) run107 — 검색 경로 주변 작은 정적 (Q71, 정적만, 짐 지도 바퀴): **`LdrpInitializePerUserWindowsDirectory` (`LdrpInitializeProcess +0x1b72`) 는 test.exe 에서 아무것도 안 바꾸고 `+0x4e` 에서 0 을 돌려준다** — 넘겨받은 `KERNEL32!TermsrvGetWindowsDirectoryW` 가 칸 `pfnTermsrvGetWindowsDirectoryW` (kernel32 rva `0xc1d10`, run101 — 0 그대로) 를 보고 0 을 주기 때문 `[정적]`. 터미널 서비스 앱 호환 환경에서만 `RtlpSystemDirs` 뒤에 "사용자 Windows 폴더" 를 붙이고, 검색 경로 캐시 3개를 **비우고 (다시 짓지는 않음)**, 같은 폴더를 `AddDllDirectory` 목록에 넣는다. **`LdrSystemDllInitBlock+0xa0` 비트 60~61 = 완화 옵션 15 `PS_MITIGATION_OPTION_IMAGE_LOAD_PREFER_SYSTEM32`** — 1 이면 시스템 폴더 글이 목록 **맨 앞으로 옮겨진다** (run39 의 "목록 전체가 시스템 폴더 하나로" 는 틀림 → 정정). **모듈 항목 `+0x118 DependentLoadFlags` = PE 로드 구성 `DependentLoadFlags` (`+0x4e` WORD) & 마스크** (`LdrpCfgProcessLoadConfig +0xa7`), 6 모듈 파일 값이 사진과 모두 같다. **`<Windows>` 글 = `RtlGetNtSystemRoot()`** = KUSD `NtSystemRoot` (`C:\WINDOWS` `[관찰]`), `ProcessParameters.Flags & 0x20000000` 은 run32 비트 29 와 같은 칸·같은 비트.

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 값 대조는 run44 사진 덤프 읽기 로그와 앞 run 이 옮겨 둔 사진 값을 다시 읽은 것뿐이다 (아래 "사진 로그 대조").
- 정적: **ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`** (기준과 같음). 다시 analyze 안 함, export 둘 (결과서 — `ms/ghidra/ntdll.export.sast102a.stdout` · `…sast102b.stdout`, 둘 다 exit 0).
  - 버전 대조 (결과서): `LdrpInitializeProcess +0x1bb6 / ret +0x1bbb → LdrpInitializeGraphRecurse`, `+0x1b72 / ret +0x1b77 → LdrpInitializePerUserWindowsDirectory` 그대로 (`_calls.tsv`, sast/97). 모듈 파일 값 6개 (`DependentLoadFlags`) 가 사진과 모두 같아 바이너리가 타깃 VM 것과 같다고 본다 (결과서).
- 커널 쪽은 Ghidra 를 안 돌렸다. ntoskrnl 기존 `.asm` 두 개 (`PspSetupUserProcessAddressSpace`, `PspPrepareSystemDllInitBlock`) 를 **읽기만** 했다 (커널 PDB `C29EBFB06B78B3C020DCA66D99713F9E1`).
- kernel32 는 `.asm` 이 없는 `TermsrvGetWindowsDirectoryW` (rva `0x35f10`) 몸통을 `objdump` 로 직접 읽었다 (결과서 + flow-writer 가 같은 바이트를 한 번 더 봄 — `+0x0 MOV RAX,[rip→0xc1d10]` · `+0x7 TEST RAX,RAX` · `+0xa JNZ +0xe` · `+0xc RET` · `+0xe JMP`).
- "ntdll 전체에서 그 줄 찾기" 는 sast/100 이 만든 스크래치 `objdump -d` 풀이 (`ntdll.dis`, 421,500줄, 커밋 안 함) 를 다시 썼다 (결과서, run106 과 같은 방법).
- 다른 decompiler 가 같은 시간에 ntdll · ntoskrnl (sast/119) 을 돌리는 중이다. **sast/103 · 105 ~ 119 결과는 이 문서에 안 넣었다.**

## 목적

- **Q71** (run39 · sast/34 "작은 정적 넷" + 번호 없이 남긴 셋, run101 정적 ①):
  - (a) `LdrpInitializePerUserWindowsDirectory` (`LdrpInitializeProcess +0x1b72`, dll 올리기 **뒤**) 가 `RtlpSystemDirs` · `RtlpSystem32Dirs` 를 다시 쓰는 줄 (`+0x157` · `+0x164`) — 무엇을, 어떤 조건에서, 그 뒤 검색 경로 캐시를 다시 짓나. test.exe 에서 타나.
  - (b) `LdrSystemDllInitBlock+0xa0` 비트 60~61 의 뜻 (run39 "1 이면 목록이 시스템 폴더 하나").
  - (c) 모듈 항목 `+0x118 DependentLoadFlags` 를 쓰는 줄 (sample 값 0 의 근거가 링크 옵션뿐이었음).
  - (d) `<Windows>` 글의 출처, `ProcessParameters.Flags & 0x20000000` 이 run32 비트 29 와 같은가.
  - (5, 작은 것) `RtlpComputeDllPathWithOptions` 안쪽 차례, 경로 구조체 `P+0x28 ~ +0x77` 을 읽는 곳, `.` 칸이 PEB `CurrentDirectory` 로 풀리나.
- 앞 바퀴 짝: run39 (`runs/02_sprint/run39-dll-search-path.md` — 검색 경로 짓기), run32 (`PspPrepareSystemDllInitBlock` · `ProcessParameters.Flags` 비트 29), run101 (`runs/03_sprint/run101-k32-leftovers.md` — `BaseThreadInitThunk(1,0,0)` 뒤가 `+0x1b72`, 칸 `pfnTermsrvGetWindowsDirectoryW` 0 그대로).

## 방법

- 정적: `_workspace/sast/102_search-path-small.md` — ntdll
  - 새로 뽑음 (depth 0): `^(RtlpSignalSystemDirsModification|LdrAddDllDirectory)$` + `^RtlpInvalidatePathCache$` (3개).
  - 이미 있던 `.asm` · `.c` 를 그대로 읽음 (다시 안 뽑음): `LdrpInitializePerUserWindowsDirectory`, `RtlpComputeDllPathWithOptions`, `LdrpInitializeDllPath`, `LdrpComputeLazyDllPath`, `LdrpGetDllPath`, `RtlpComputePath`, `RtlpGetCachedPath`, `RtlpComputeDllPath`, `LdrpCfgProcessLoadConfig`, `LdrpInitializeProcess`, `RtlGetNtSystemRoot` 등.
  - 전수 검색: `objdump` 풀이 + `_index.tsv` (`LdrSystemDllInitBlock+0xa0` 참조 전부, `+0x118]` 쓰기 · 읽기, `0x20000000` 상수). 열거형 `PS_MITIGATION_OPTION` 은 ntdll PDB 형 표.
- 결과서: `_workspace/sast/102_search-path-small_result.md` (**분석성공** — 못 정한 것: `P+0x28 ~ +0x77` 읽는 곳, `CurrentDirectory` 를 읽는 갈래).
- flow-writer 가 다시 본 것 (`.asm` 줄):
  - `LdrpInitializePerUserWindowsDirectory` (`+0x2d` · `+0x47` · `+0x4c` · `+0x4e` · `+0x50` · `+0x75` · `+0x77` · `+0x97` · `+0xbc` · `+0xed` · `+0x112` · `+0x121` · `+0x132` · `+0x14d` · `+0x157` · `+0x164` · `+0x16c` · `+0x17b`)
  - `RtlpSignalSystemDirsModification` (`+0xf` ~ `+0xa3` 전부), `RtlpInvalidatePathCache` (`+0x0` ~ `+0x18` 전부), `LdrAddDllDirectory` (`+0x5c` · `+0x1b5` · `+0x1e8`)
  - `LdrpInitializeProcess` (`+0xcdc` · `+0xcec` · `+0xd36` · `+0xfdb` · `+0x10a6` · `+0x10bd` · `+0x1123` · `+0x1146` · `+0x1b4c` · `+0x1b4f` · `+0x1b6a` · `+0x1b72` · `+0x1b7b` · `+0x1b7d`), `LdrpInitializeKernel32Functions +0x12b`
  - `RtlGetNtSystemRoot` (`+0x0` ~ `+0x35` 전부)
  - `RtlpComputePath` (`+0x21` · `+0x2b` · `+0x32` · `+0x135` · `+0x138` · `+0x168` · `+0x37a` · `+0x397` · `+0x460` · `+0x484` · `+0x613` · `+0x671` · `+0x699` · `+0x6a1`)
  - `LdrpCfgProcessLoadConfig` (`+0x80` · `+0x86` · `+0x8c` · `+0xa7` · `+0xad` · `+0xaf` · `+0x67e` · `+0x69e` · `+0x6f1`), `LdrpProcessMappedModule` (`+0x107` · `+0x13a` · `+0x145`)
  - `RtlpComputeDllPathWithOptions` (`+0x2b` · `+0x6f` · `+0x92` · `+0x99` · `+0xa2` · `+0xc4` · `+0xdd` · `+0x12a`), `LdrpGetDllPath` (`+0xa1` · `+0xa8` · `+0x15c` ~ `+0x192`), `RtlpGetCachedPath` (`+0x27` · `+0x43` · `+0xbe` · `+0xe9` · `+0x100` ~ `+0x111`)
  - `_calls.tsv` (ntdll): 아래 표의 줄 전부.
  - `alias:` 하나 — `RtlpComputePath +0x699` 의 callee 는 "메모리 복사" 함수다. 같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove` (`.asm` 주석 `ALIASES(3)`).

## 결과

비유 (주소록 = DLL 검색 목록, 주소록 표지 = 경로 구조체 `P`, 복사본 서랍 = 검색 경로 캐시):

- `LdrpInitializePerUserWindowsDirectory` 는 "손님마다 따로 쓰는 Windows 폴더가 있으면 주소록 시스템 칸 끝에 한 줄 더 적는" 사람이다. 물어볼 곳 (`TermsrvGetWindowsDirectoryW`) 이 "그런 폴더 없어요 (0)" 하면 **아무것도 안 하고 돌아간다.** test.exe 가 그 경우다.
- 적을 일이 있으면 주소록 시스템 칸을 새 종이에 옮겨 적고, **복사본 서랍 셋을 비운다.** 새로 채우지는 않는다 — 다음에 누가 주소록을 펼칠 때 새로 만든다.
- 비트 60~61 은 "시스템 폴더 먼저 보기" 스위치다. 켜지면 시스템 칸이 주소록 **맨 위**로 올라가고 나머지 줄은 순서 그대로 남는다. 주소록이 한 줄로 줄어드는 게 아니다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeKernel32Functions` | `+0x12b` | `+0x130` | `LdrGetProcedureAddressForCaller("TermsrvGetWindowsDirectoryW")` → 출력 칸 `[RSP+0x188]` (caller 쪽) | 함수 주소만 찾아 둠 | [정적] (run20 그대로) |
| `ntdll!LdrpInitializeProcess` | `+0x1b72` | `+0x1b77` | `LdrpInitializePerUserWindowsDirectory(그 함수 주소)` | `+0x1b6a MOV RCX,[RSP+0x188]`. 앞 `+0x1b4c TEST` · `+0x1b4f JZ +0x1b83` (칸 `Kernel32ThreadInitThunkFunction` 0 이면 안 부름). 뒤 `+0x1b7b TEST EAX` · `+0x1b7d JS` (음수면 실패 꼬리) | [정적] (call 자리 run101 그대로) |
| `ntdll!LdrpInitializePerUserWindowsDirectory` | `+0x47` | `+0x4c` | CFG 간접 호출 = `KERNEL32!TermsrvGetWindowsDirectoryW(버퍼 [RSP+0x60], 0x104)` | `+0x2d MOV RAX,RCX`. 돌아와 `+0x4c ADD EAX,EAX` · **`+0x4e JNZ +0x77`** — 0 이면 `+0x50 XOR EAX,EAX` → `+0x75 RET` | [정적] |
| `ntdll!LdrpInitializePerUserWindowsDirectory` | `+0xbc` | `+0xc1` | `RtlAllocateHeap` (옛 길이 + 새 폴더 + 2) | `+0x77 CMP [RtlpSystemDirs+8],0` · `+0x97 JZ +0x171` 를 지난 뒤. 실패면 `+0xed LdrpLogInternal` → `0xc0000017` | [정적] |
| `ntdll!LdrpInitializePerUserWindowsDirectory` | `+0x112` · `+0x121` · `+0x132` | `+0x117` · `+0x126` · `+0x137` | `RtlAppendUnicodeStringToString` ×2 · `RtlAppendUnicodeToString` | 옛 `RtlpSystemDirs` + 새 폴더 + `;` | [정적] |
| `ntdll!LdrpInitializePerUserWindowsDirectory` | `+0x14d` | `+0x152` | `RtlFreeHeap` (옛 버퍼) | 이어 **`+0x157 MOVDQU [RtlpSystemDirs]`** · **`+0x164 MOVQ [RtlpSystem32Dirs+8]`** | [정적] |
| `ntdll!LdrpInitializePerUserWindowsDirectory` | `+0x16c` | `+0x171` | `RtlpSignalSystemDirsModification` | 캐시 비우기 | [정적] |
| `ntdll!LdrpInitializePerUserWindowsDirectory` | `+0x17b` | `+0x180` | `LdrAddDllDirectory(&새 폴더, &쿠키)` | 반환값 안 봄, `+0x180 JMP +0x50` (0 반환) | [정적] |
| `ntdll!RtlpSignalSystemDirsModification` | `+0x16` / `+0x4f` | `+0x1b` / `+0x54` | `RtlAcquireSRWLockExclusive` / `RtlReleaseSRWLockExclusive` (`RtlpCachedPathLock`) | | [정적] |
| `ntdll!RtlpSignalSystemDirsModification` | `+0x22` · `+0x31` · `+0x40` | `+0x27` · `+0x36` · `+0x45` | `RtlpInvalidatePathCache(&RtlpDllSearchPath)` · `(&RtlpExeSearchPath)` · `(&RtlpSearchPath)` | 칸을 0 으로, 참조가 0 이 된 블록만 돌려줌 | [정적] |
| `ntdll!RtlpSignalSystemDirsModification` | `+0x6b` · `+0x87` · `+0xa3` | `+0x70` · `+0x8c` · `+0xa8` | `RtlFreeHeap` (블록 셋, 각각 0 이 아닐 때만) | 락을 푼 뒤 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xcdc` | `+0xce1` | `RtlGetNtSystemRoot` | → `+0xcec RtlInitUnicodeStringEx(&[RSP+0x120], 결과)` (ret `+0xcf1`) = **`<Windows>`** | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xd36` · `+0x1123` · `+0x1146` | `+0xd3b` · `+0x1128` · `+0x114b` | `RtlAppendUnicodeStringToString` (`<Windows>` 를 System32 글 · `RtlpSystemDirs` 두 조각에) | | [정적] |
| `ntdll!LdrpProcessMappedModule` | `+0x107` | `+0x10c` | `LdrInitSecurityCookie` (다섯째 인자 `[RSP+0x88]` = 로드 구성 포인터) | | [정적] |
| `ntdll!LdrpProcessMappedModule` | `+0x145` | `+0x14a` | `LdrpCfgProcessLoadConfig(항목, NT 머리, 로드 구성)` | `+0x13a MOV R8,[RSP+0x88]` | [정적] (run63 그대로) |
| `ntdll!LdrpCfgProcessLoadConfig` | `+0x67e` | `+0x683` | `LdrpIsModuleUnderSystem32` | 걸러 낸 `DependentLoadFlags` ≠ 0 (`+0xad TEST` · `+0xaf JNZ +0x67b`) 일 때만 | [정적] (run63 그대로) |
| `ntdll!LdrpGetDllPath` | `+0xa8` | `+0xad` | `RtlpGetCachedPath` | 기본: `+0xa1 LEA RCX,[RtlpDllSearchPath]`. 옵션: `+0x17f LEA RCX,[RtlpDllSearchPathWithOptions]` · `R8D` = 플래그 → `+0x192 JMP` 로 같은 call 자리 | [정적] |
| `ntdll!RtlpGetCachedPath` | `+0xbe` / `+0xe9` | `+0xc3` / `+0xee` | `RtlpComputeDllPath` / `RtlpComputeDllPathWithOptions` | 콜백 주소 비교 (run39) | [정적] (run39 그대로) |
| `ntdll!RtlpComputeDllPathWithOptions` | `+0x2b` / `+0xd3` | `+0x30` / `+0xd8` | `RtlAcquireSRWLockShared` / `RtlReleaseSRWLockShared` (`LdrpDllDirectoryLock`) | | [정적] |
| `ntdll!RtlpComputeDllPathWithOptions` | `+0xc4` | `+0xc9` | `RtlpComputePath` | 쌓은 칸 코드 목록으로 | [정적] |
| `ntdll!RtlpComputePath` | `+0x699` | `+0x69e` | 메모리 복사 (같은 주소 이름 4개, 후보 `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`) | **비트 60~61 = 1 일 때만** — 시스템 폴더 글을 항목 0 번으로 먼저 복사 | [정적] |

### (a) `LdrpInitializePerUserWindowsDirectory` (rva `0xfb04c`, 388바이트) — test.exe 에선 아무것도 안 바꿈 (결과서 1)

- **넘겨받는 것** `[정적]`: kernel32 `TermsrvGetWindowsDirectoryW` 의 주소 (위 표 첫 두 줄). 이 함수가 그것을 `(버퍼 0x104 글자, 0x104)` 로 부른다.
- **`TermsrvGetWindowsDirectoryW` 몸통** `[정적: objdump]`: `+0x0 MOV RAX,[kernel32+0xc1d10]` → `+0x7 TEST RAX,RAX` → `+0xa JNZ +0xe` → `+0xc RET`. 칸이 0 이면 **RAX = 0 그대로 돌아간다.** 0 이 아니면 `+0xe JMP` 로 칸의 함수 (tsappcmp 쪽) 에 넘긴다.
  - 그 칸 = **`pfnTermsrvGetWindowsDirectoryW`** (kernel32 `.data` rva `0xc1d10`, Ghidra 주석 `BaseDllFindIniFileNameMapping +0x66`). 쓰는 줄은 `BasepInitializeTermsrvFpns` 의 `GetProcAddress` 12바퀴 루프뿐이고 test.exe 는 그 루프 앞 `+0x2b` 에서 나간다 → **0 그대로** (run101 `[정적]`, 갈래 값 KUSD `SuiteMask 0x110` · PEB `SharedData` null · exe `DllCharacteristics 0x8160` 은 run44 `[관찰]`).
- **함수 안 차례** `[정적]`:
  1. `+0x47` 간접 호출 → `+0x4c ADD EAX,EAX` (글자 수 → 바이트) → **`+0x4e JNZ +0x77`**. 0 이면 `+0x50 XOR EAX,EAX` → `+0x75 RET` (성공 0).
  2. (0 이 아닐 때) `+0x77 CMP [RtlpSystemDirs+8],0` · `+0x97 JZ +0x171` — `RtlpSystemDirs` 가 아직 없으면 (예: `LdrpPolicyBits & 1`) 글 바꾸기를 건너뛰고 `+0x171` 로.
  3. `+0xbc RtlAllocateHeap` → `+0x112 / +0x121 / +0x132` 이어 붙이기 (옛 글 + 새 폴더 + `;`) → `+0x14d RtlFreeHeap` (옛 버퍼).
  4. 쓰기: **`+0x157 MOVDQU [RtlpSystemDirs]`** (16B — 길이 · 최대 길이 · 새 버퍼) · **`+0x164 MOVQ [RtlpSystem32Dirs+8]`** (새 버퍼 포인터만, 길이 칸 그대로 — 옛 버퍼를 풀었으니 그 포인터를 고쳐 둠).
  5. `+0x16c RtlpSignalSystemDirsModification` → 캐시 비우기 (아래).
  6. `+0x17b LdrAddDllDirectory(&새 폴더, &쿠키)` → `+0x180 JMP +0x50` (0 반환, 반환값 안 봄).
- **캐시 = 비우기만, 다시 안 짓는다** `[정적]`:
  - `RtlpSignalSystemDirsModification`: `RtlpCachedPathLock` 독점 (`+0x16`) 안에서 `RtlpDllSearchPath` (`+0x22`) · `RtlpExeSearchPath` (`+0x31`) · `RtlpSearchPath` (`+0x40`) 를 `RtlpInvalidatePathCache` 로 비우고, 락을 푼 뒤 (`+0x4f`) 돌려받은 블록을 `+0x6b / +0x87 / +0xa3 RtlFreeHeap`.
  - `RtlpInvalidatePathCache` (`+0x0 ~ +0x18`): 칸 값을 읽고 0 을 쓰고 (`+0x5`), 블록 참조수 `[+0x50]` 를 1 내려 0 이 되면 그 블록을 돌려준다 (`+0xf ADD …,-1` · `+0x14 CMOVNZ`). **참조가 남아 있으면 (경로 구조체 `P` 가 아직 들고 있으면) 블록은 안 풀린다** — `P` 는 `LdrpReleaseDllPath` (`LdrpInitializeProcess +0x1bfb`) 때 내려놓는다 (결과서).
  - 다음에 누가 쓰면 `RtlpGetCachedPath +0x43 JZ` → 다시 계산 → `+0x111 MOV [R14],RDI` 로 다시 저장 (결과서 + flow-writer `.asm`).
  - `LdrAddDllDirectory`: `+0x5c TEST [LdrpPolicyBits],4` (꺼지면 `0xc000000d`; 사진 `LdrpPolicyBits = 0x16` `[관찰]` run65 → 켜짐), `+0x1b5` `LdrpUserDllDirectories` 끝에 그 폴더, `+0x1e8` `RtlpDllSearchPathWithOptions` 칸을 0 으로.
- **안 고치는 칸** `[정적]`: `LdrpSystem32` (`ntdll+0x1d2990`) — `LdrpInitializeProcess +0x10a6` 이 같은 버퍼 포인터를 넣어 둔 칸인데 이 함수는 안 고친다. 그래서 이 길이 타는 환경에선 옛 버퍼가 풀린 뒤 **허공 포인터**가 된다 (읽는 곳 `LdrpIsModuleUnderSystem32 +0x49`). 실제로 무슨 일이 나는지는 `[추측]` — test.exe 는 이 길을 안 타서 상관없다.
- **test.exe 에서 타나 — 아니오** `[정적]` (조건 칸 하나만 `[VM]` 확인 남음): 칸 `pfnTermsrvGetWindowsDirectoryW` 0 → 반환 0 → `+0x4e JNZ` 안 탐 → `+0x75 RET` 0. **`RtlpSystemDirs` · `RtlpSystem32Dirs` · 캐시 셋 · `AddDllDirectory` 목록 모두 그대로.**
  - 그래서 진입점 뒤 `LoadLibraryExW` ×3 에 영향 **없음** `[정적]`.
  - 켜지는 환경에서도: `P` 가 이미 들고 있는 목록은 그대로고, **그 뒤 새로 시작하는 LoadLibrary 가 `LdrpInitializeDllPath` 로 `P` 를 0 으로 비운 뒤** (`+0x13 ~ +0x35`) 캐시를 다시 지어 새 글을 본다 (결과서).
  - `LdrpComputeLazyDllPath` · `LdrpGetDllPath` · `LdrpInitializeDllPath` 세 함수엔 `RtlpSystemDirs` · `<Windows>` 글을 읽는 줄이 없다 (`_refs` 읽는 곳 목록에 없음, 결과서). 읽는 곳은 `RtlpComputePath +0x456 / +0x524 / +0x53b` (`RtlpSystemDirs`), `+0x3ac` · `RtlpAddSystemPath +0x20` (`RtlpSystem32Dirs`).

### (b) `LdrSystemDllInitBlock+0xa0` 비트 60~61 — "시스템 폴더 먼저" 스위치 (결과서 2)

- **뜻** `[정적]`: 블록 `+0xa0` 은 커널이 `PspPrepareSystemDllInitBlock +0x12b / +0x138` 로 복사하는 24바이트 완화 옵션 지도 (run32). **칸 = 옵션 번호 × 4 비트** (2비트가 상태).
  - PDB 열거형 `PS_MITIGATION_OPTION` 에서 번호 15 = **`PS_MITIGATION_OPTION_IMAGE_LOAD_PREFER_SYSTEM32`** → 15 × 4 = 60.
  - 칸 배치는 다른 읽기 4곳으로 맞춰 봤다 (결과서): 번호 3 `HEAP_TERMINATE` = 비트 12~13 (`LdrpInitializeProcess +0xa1f SHR 0xc`, run46), 번호 10 `CONTROL_FLOW_GUARD` = 40~41 (`LdrGetProcedureAddressForCaller +0x68c SHR 0x28`), 번호 17 `LOADER_INTEGRITY_CONTINUITY` = 68~69 (`+0x4d2 SHR 4`), 번호 19 `RESTRICT_SET_THREAD_CONTEXT` = 둘째 8바이트 비트 12 (`RtlRestoreContext +0x16d BT 0xc`).
  - 값 이름 (`ALWAYS_ON` 등) 은 SDK 기억이라 `[추측: 이름]`. **코드가 보는 값은 1 뿐**이다.
- **ntdll 에서 읽는 곳 (전수, `objdump` 로 `0x1801ea4f0` 참조 전부 — 결과서)**:

| 읽는 줄 | 보는 비트 | 쓰임 |
|---|---|---|
| `RtlpComputePath +0x21 MOV R14,[+0xa0]` → `+0x2b SHR 0x3c` → `+0x32 AND 3` | **60~61** | `+0x37a CMP R14B,1` / `+0x460 CMP R14B,1` (아래). 0 · 2 · 3 이면 영향 없음 |
| `LdrGetProcedureAddressForCaller +0x685`, `LdrControlFlowGuardEnforcedWithExportSuppression +0x13` | 40~41 | CFG 내보내기 억제 (값 3) |
| `LdrpInitializeProcess +0x4a4` (16바이트 복사 + `+0x4d2`) | 68~69 | `LdrpEnforceIntegrityContinuity` · `LdrpAuditIntegrityContinuity` |
| `LdrpInitializeProcess +0x9fa` (복사 + `+0xa1f`) | 12~13 | `RtlSetHeapInformation` (run46) |
| `RtlRestoreContext +0x150 / +0x162`, `RcConsolidateFrames +0xe / +0x32` | 둘째 8바이트 비트 60 · 12 | `+0xa8` 쪽 (run82) |

- **`RtlpComputePath` 에서 값 1 이 하는 일** `[정적]`:
  1. 칸 코드 **2** (`RtlpSystemDirs`, `+0x456 ~ +0x484`) 또는 **7** (`RtlpSystem32Dirs`, `+0x367 ~ +0x397`) 를 만나면, 크기를 더한 뒤 `+0x37a / +0x460 CMP R14B,1` 이 맞을 때 `[RSP+0x48]` 에 그 글 주소를 적는다 (`+0x397` / `+0x484`, 마지막 것이 이김).
  2. 블록을 만든 뒤 `+0x135 TEST RDI,RDI` / `+0x138 JNZ +0x671` → `+0x671` 이 **그 시스템 폴더 글을 항목 0 번으로 먼저 복사** (`+0x699` 메모리 복사, 칸 코드 `+0x68f` 에 7 또는 2) 하고 개수를 1 로 둔 채 (`+0x6a1`) 일반 고리 (`+0x13e`) 로 간다.
  3. 일반 고리에서 `RDI ≠ 0` 이면 (`+0x165 TEST` / `+0x168 JNZ +0x60c`) 코드 2 · 7 인 칸은 건너뛰고 (`+0x613 JZ +0x204` / `+0x61c ~ +0x622`) 나머지는 원래 순서대로 쓴다.
  - 뜻: **시스템 폴더 글을 맨 앞으로 옮기고 나머지는 그대로.** 기본 줄 1 (`5 2 4 3 8`) 이면 `2 5 4 3 8` 이 된다. **run39 의 "목록 전체가 시스템 폴더 하나로" 는 틀렸다** (아래 "정정").
- **test.exe**: 사진 `ntdll+0x1ea4f0` = 0 `[관찰]` (run46) → 안 탄다.

### (c) 모듈 항목 `+0x118 DependentLoadFlags` — 로드 구성 `+0x4e` & 마스크 (결과서 3)

- **ntdll 전수 (`objdump`, `+0x118]` 중 항목 칸 맞는 것 — 결과서)**:

| 줄 | 하는 일 |
|---|---|
| **`LdrpCfgProcessLoadConfig +0xa7 MOV [RSI+0x118],ECX`** | **쓰기 (주 경로).** `+0x8c MOVZX ECX,word [RDI+0x4e]` = 로드 구성 디렉터리 `DependentLoadFlags` (구성 크기 ≥ `0x94` 일 때만 — `+0x80 CMP [RDI],0x94` · `+0x86 JC`). 마스크 `+0x90 ~ +0xa5` = `(((LdrpPolicyBits & 4) \| 0x7b) << 8) \| 0x8000` |
| `LdrpCfgProcessLoadConfig +0x69e` (`+0x69a BTS 9` / `+0x6f1 BTS 8`) | **고쳐 쓰기.** 걸러 낸 값 ≠ 0 (`+0xad` · `+0xaf JNZ +0x67b`) 이고 `+0x67e LdrpIsModuleUnderSystem32` 가 참일 때 — exe 면 비트 9 (`0x200`), 아니면 비트 8 (`0x100`) |
| `LdrpMapDllSearchPath +0xc1 MOV R9D,[RCX+0x118]` | 읽기 (부모 항목, run39) |
| `LdrpValidateIntegrityContinuity +0x33 TEST [RCX+0x118],0x8000` | 읽기 (`0x8000` 비트, 부르는 곳 `LdrpMapDllNtFileName +0x407`) |

  - 다른 쓰는 줄은 없다. `LdrpCfgProcessLoadConfig` 를 부르는 곳은 `LdrpProcessMappedModule +0x145` 와 `AvrfMiniLoadDll +0x869` 둘뿐 (결과서).
- **값을 어디서 가져오나** `[정적]`: `LdrpProcessMappedModule +0x107 LdrInitSecurityCookie` 가 다섯째 인자 `[RSP+0x88]` 에 로드 구성 포인터를 받아 오고 (그 안 `+0xe8 LdrImageDirectoryEntryToLoadConfigEx`), `+0x13a MOV R8,[RSP+0x88]` 로 `LdrpCfgProcessLoadConfig` 에 넘긴다 = **PE 로드 구성 디렉터리**.
- **값이 없을 때**: 항목은 `LdrpAllocateModuleEntry +0x23 / +0x29` 에서 `HEAP_ZERO_MEMORY` (플래그 `|= 8`, `+0x20`) 로 0 으로 시작한다. 로드 구성이 없거나 크기 < `0x94` 면 `+0x7a JZ` / `+0x86 JC → +0x454` 로 나가 안 쓴다 → 0 그대로 (결과서).
- **`LdrpIsModuleUnderSystem32` 가 System32 바로 아래 모듈에서 거짓인 까닭** `[정적]`: `FullDllName − BaseDllName` (폴더 길이) 이 `LdrpSystem32.Length + 2` 보다 **길 때만** (`+0x1a ~ +0x22 JA +0x2f`) 앞부분을 `LdrpSystem32` 와 비교한다. 곧 **System32 하위 폴더 안 모듈만 참**, ntdll · KernelBase 처럼 System32 바로 아래 모듈은 거짓 (결과서). run63 이 남긴 "ntdll 에서 거짓인 까닭" 이 이것이다.
- **6 모듈 대조** (결과서가 PE 파일 `+0x4e` 를 직접 읽음 + flow-writer 가 사진 줄을 다시 읽음):

| 모듈 | 파일 값 (로드 구성 `+0x4e`) | 사진 P1b `+0x118` | 비트 8·9 갈래 |
|---|---|---|---|
| test.exe | 0 | 0 (`04_baggage-read_P1b.txt:3439`) | 걸러 낸 값 0 → 안 감 |
| ntdll | `0x800` | `0x800` (`:3544`) | 마스크 통과, System32 바로 아래 → 거짓, 안 더함 |
| KERNEL32 | 0 | 0 (`:3649`) | 안 감 |
| KERNELBASE | `0x800` | `0x800` (`:3754`) | ntdll 과 같음 |
| dll1 | 0 | 0 (`:3859`) | 안 감 |
| dll2 | 0 | 0 (`:3964`) | 안 감 |

  - 모듈 이름은 같은 로그 `BaseDllName` 줄 (`:3386` · `3491` · `3596` · `3701` · `3806` · `3911`) 로 맞췄다.
  - 그래서 "sample 값 0 의 근거가 링크 옵션뿐" 은 **코드 (구성 `+0x4e` & 마스크) + 파일 값 + 사진** 셋으로 닫힌다. dll1 · dll2 의 부모 (exe · dll1) 값이 0 이라 `LdrpMapDllSearchPath +0xe1` 이 부모 문맥의 `P` 를 그대로 쓴다는 run39 의 줄도 `[정적+관찰]` 이 된다.

### (d) `<Windows>` 글의 출처 · `Flags & 0x20000000` (결과서 4)

- **`<Windows>` = `RtlGetNtSystemRoot()`** `[정적]`: `LdrpInitializeProcess +0xcdc` (ret `+0xce1`) → `+0xcec RtlInitUnicodeStringEx(&[RSP+0x120], 결과)`. 이 `[RSP+0x120]` 이 System32 글 (`+0xd36` 이어 붙이기) 과 `RtlpSystemDirs` 의 `<Windows>\system;<Windows>;` 두 조각 (`+0x1123 / +0x1146`) 에 쓰인다. 글자 상수 (`\system;`, `;`, `forwarders\alt;` / `forwarders;`) 는 결과서가 PE 파일에서 읽음.
- **`RtlGetNtSystemRoot`** (`+0x0 ~ +0x35`): `+0x0 MOV RAX,GS:[0x60]` → `+0x9 MOV RAX,[RAX+0x90]` (**`PEB.SharedData`**) → 0 이거나 (`+0x10 TEST` · `+0x13 JNZ`) 첫 DWORD 가 0 이면 (`+0x1c CMP [RAX],0` · `+0x1f JZ`) **`+0x15 MOV EAX,0x7ffe0030` = KUSD `NtSystemRoot`** (PDB `_KUSER_SHARED_DATA +0x30`). 아니면 (서버 silo) `+0x31 ADD RAX,0x1e` = `SharedData+0x1e` (PDB `_SILO_USER_SHARED_DATA +0x1e NtSystemRoot`).
- test.exe: `PEB.SharedData = (null)` `[관찰]` (`04_baggage-read_P1b.txt:2462`), KUSD `NtSystemRoot = "C:\WINDOWS"` `[관찰]` (`04_baggage-read_P2.txt:4028`) → **`<Windows>` = `C:\WINDOWS`**.
- **`ProcessParameters.Flags & 0x20000000` = run32 비트 29, 같은 칸 · 같은 비트** `[정적]`:
  - ntdll 읽는 줄 (전수, `0x20000000` 상수 — 결과서): `LdrpInitializeProcess +0xfdb` · `+0x10bd` (둘 다 `TEST [RAX+0x8],0x20000000`, `RAX = PEB.ProcessParameters`), `LdrpInitializeKernel32Functions +0x21a`, `RtlpComputeDllPathWithOptions +0x92`. 전부 `[PEB+0x20]+0x8` = PDB `_RTL_USER_PROCESS_PARAMETERS +0x8 Flags`.
  - 커널 쓰는 줄: `PspSetupUserProcessAddressSpace +0xe6 BTS EBX,0x1d` → `+0xea MOV [RBP+0x8],EBX` (조건 `+0xdd CMP [RAX+0x3f8],0` = `OneCoreForwardersEnabled`). 같은 오프셋 `+0x8`, 같은 비트 29 (결과서).
  - 켜지면: `RtlpSystemDirs` 에 `<Windows>\System32\forwarders\alt;` (`LdrpIncludeAlternateForwarders` 참일 때, `+0x10c6 ~ +0x10ee`) · `…forwarders;` (`+0x10f2 ~ +0x1111`), `RtlpComputeDllPathWithOptions` 넷째 인자 켬 (아래 5), `LdrpInitializeKernel32Functions` 는 kernel32 찾기 건너뜀 (run20).
  - test.exe: `Flags = 0x4001` `[관찰]` (`ms/logs/04_baggage-read_K1.txt:2337`, run104 · run65) → 비트 29 꺼짐 → 안 붙음.

### 5 `RtlpComputeDllPathWithOptions` 안쪽 (rva `0xba650`, 315바이트) (결과서 5)

- **인자**: `RCX` = 플래그 (`EBX`), `RDX` = 경로 글 (그대로 `RtlpComputePath` 의 `R8` — 코드 5 가 그 글의 폴더를 씀). 잠금 `LdrpDllDirectoryLock` 공유 (`+0x2b / +0xd3`).
- **플래그 → 칸 코드 차례** (`+0x30 ~ +0xb1`, 스택에 dword 로 쌓음 — 뜻은 run39 칸 코드 표):

| 순서 | 줄 | 플래그 비트 | 쌓는 코드 | 뜻 |
|---|---|---|---|---|
| 1 | `+0x34 ~ +0x46` | 8 (`0x100`) | **5** | 응용 프로그램 폴더 (경로 글이 있으면 그 폴더, 없으면 exe 폴더) |
| 2 | `+0x51 ~ +0x5d` | 9 (`0x200`) | **1** | exe 폴더 |
| 3 | `+0x65 JC +0x102` → `+0x102 ~ +0x125` | 10 (`0x400`) | **6**, `LdrpDllDirectory` 길이 ≠ 0 (`+0x108`) 이면 이어서 **0** | `LdrpUserDllDirectories` (AddDllDirectory 목록), 이어서 SetDllDirectory 폴더 |
| 4 | `+0x6f TEST EBX,0x4800` | 11 (`0x800`) 또는 14 (`0x4000`) | **7** | `RtlpSystem32Dirs` (System32 하나) |
| 5 | `+0xa2 CMP [LdrpAppPackagesPath],BP` | (플래그 아님) 패키지 글 길이 ≠ 0 | **8** | `LdrpAppPackagesPath` |

  - 코드 **2** (`RtlpSystemDirs` 덩어리) · **3** (`PATH`) · **4** (`.`) 는 이 함수 목록에 **절대 안 들어간다** → "옵션 있는" 검색은 PATH · 현재 폴더를 안 본다 `[정적]`.
  - 넷째 인자 (forwarders 포함) = `Flags & 0x20000000` 켜짐 (`+0x92 TEST` / `+0x99 JNZ +0x12a`) **그리고** 플래그 비트 11 (`0x800`) 일 때만 1 (`+0x12a MOV R9B,1` / `+0x12d BT 0xb`). `0x4000` 만이면 0.
  - 반환 블록에 `+0xdd MOV [RBX+0x74],1` (PATH 가 없다는 표시 — `RtlpGetCachedPath +0x5a` 에서 환경 판 비교를 건너뜀 `[추측: 이름]`).
  - 플래그 `0x1000` (기본 폴더들) 은 들어오기 전에 `LdrpGetDllPath +0x15c ~ +0x16c` 에서 `0xa00` 이 더해진다 (`+0x162 OR EBX,0xa00`, `LdrpPolicyBits & 4` 면 `+0x16c BTS EBX,0xa`). `+0x17a BTS R8D,8` / `+0x18e CMOVZ` 는 `[RSP+0x78]` 표시가 켜졌을 때 `0x100` 을 더한다 (그 표시 = 플래그가 정확히 `8` — SDK 이름 `LOAD_WITH_ALTERED_SEARCH_PATH` `[추측: 이름]`).
  - **캐시를 안 쓴다**: 옵션 호출은 `R8D` (플래그) ≠ 0 이라 `RtlpGetCachedPath +0x27 JNZ` 로 매번 새로 계산, 저장 안 함 → `RtlpDllSearchPathWithOptions` 칸은 이 길로는 안 채워지는 것으로 보인다 `[정적 추정]`. 그러면 `LdrAddDllDirectory +0x1e8` 의 0 쓰기는 늘 빈 칸에 쓰는 것.
- **`P+0x28 ~ +0x77` 읽는 곳 — 못 찾음** `[정적]`: 쓰는 곳은 `LdrpGetDllPath +0xca ~ +0xec` (캐시 블록 앞 `0x50`B 를 `P+0x28` 로 복사). `P` 를 받는 함수 (`LdrpSearchPath` · `LdrpReleaseDllPath` · `LdrpMapDllSearchPath` · `LdrpFindLoadedDllInternal` · `LdrpFindLoadedDll` · `LdrpInitializeDllPath` · `LdrpComputeLazyDllPath`) 를 다 따라가도 읽는 줄이 없다 (결과서). 다른 함수가 `P` 를 레지스터로 받아 읽을 가능성은 `[공백]`.
- **`.` 칸이 `CurrentDirectory` 로 풀리나**:
  - 옵션 목록엔 `.` (코드 4) 가 없다 (위).
  - 기본 목록 (`RtlpComputeDllPath`, 줄 0 · 1) 의 `.` 은 `RtlpComputePath +0x270 ~ +0x287` 이 **글자 `.;` 만 써 넣는다**. `RtlpComputePath` 안에서 `ProcessParameters.CurrentDirectory` 를 읽는 줄은 없다 (`GS:[0x60]` 은 `+0xe9` · `+0x215` · `+0x239` · `+0x2cc` 뿐 — 힙 · `FastPebLock` 용, 결과서).
  - 풀리는 곳은 후보를 전체 경로로 만들 때다: `LdrpResolveDllName` → `LdrpGetFullPath` → `RtlGetFullPathName_Ustr` 안에 `[PEB+0x20]+0x38` (PDB `+0x38 CurrentDirectory`) 를 읽는 줄이 `+0xeed` · `+0xf1f` · `+0x1059` 셋 (결과서 `objdump`). 어느 갈래인지는 안 읽었다 `[추측: 상대 경로 갈래]`.

### 사진 로그 대조 (run44 · run46 · run65, 새로 돌린 것 아님 — flow-writer)

| 정적 | 사진 값 | 맞나 |
|---|---|---|
| (a) 칸 `pfnTermsrvGetWindowsDirectoryW` 0 → 0 반환 | kernel32 `.data` 는 run44 이 안 읽음 (run101). 갈래 값 KUSD `SuiteMask 0x110` · PEB `SharedData` null · exe `DllCharacteristics 0x8160` (run101 이 옮긴 run44 줄) | 칸 자체는 **못 봄** — `[VM]` 동적 1. 갈래 값은 0 그대로 쪽과 맞음 |
| (a) `RtlpSystemDirs` · `RtlpSystem32Dirs` 그대로 | 짐 지도 3.8 사진 `≤P1a` (P1a 뒤 바뀌었는지는 칸 값 비교를 안 함) | 이번에 다시 안 읽음 — 동적 2 |
| (b) 비트 60~61 = 1 일 때만 | `ntdll+0x1ea4f0` = 0 (run46) | **맞음** — 안 탐 |
| (c) `+0x118` = 파일 `+0x4e` & 마스크, 비트 8·9 안 더함 | 6 모듈 `+0x118` (`04_baggage-read_P1b.txt:3439 ~ 3964`, 위 표) | **맞음** (6/6) |
| (c) 마스크 `0x7f00` 쪽 (`LdrpPolicyBits & 4`) | `LdrpPolicyBits = 0x16` (run65) | 비트 2 켜짐 → `0x7f00 \| 0x8000`. `0x800` 은 어느 쪽이든 통과 |
| (d) `<Windows>` = KUSD `NtSystemRoot` | PEB `SharedData : (null)` (`_P1b.txt:2462`), KUSD `NtSystemRoot : "C:\WINDOWS"` (`_P2.txt:4028`) | **맞음** |
| (d) 비트 29 꺼짐 | `ProcessParameters.Flags 0x4001` (run104 · run65) | **맞음** — forwarders 안 붙음 |

### 호출 관계

```
(사용자 Windows 폴더 — test.exe 는 +0x4e 에서 끝)
LdrpInitializeKernel32Functions
 └─ +0x130  LdrGetProcedureAddressForCaller("TermsrvGetWindowsDirectoryW")  → [RSP+0x188] (LdrpInitializeProcess 쪽)
LdrpInitializeProcess
 │  +0x1b4f  Kernel32ThreadInitThunkFunction == 0 → 건너뜀
 └─ +0x1b77  LdrpInitializePerUserWindowsDirectory(RCX = [RSP+0x188])
              ├─ +0x4c   (CFG 간접) KERNEL32!TermsrvGetWindowsDirectoryW(buf, 0x104)
              │            +0x0 RAX = [kernel32+0xc1d10] (pfnTermsrvGetWindowsDirectoryW, 0) → +0xc RET 0   ← test.exe
              │  +0x4e  0 → +0x50 / +0x75 RET 0                                                       ← test.exe
              │  (0 이 아닐 때만, 아래)
              │  +0x97  RtlpSystemDirs 없음 → +0x171
              ├─ +0xc1   RtlAllocateHeap → +0x117 / +0x126 / +0x137 이어 붙이기 → +0x152 RtlFreeHeap
              │  +0x157 [RtlpSystemDirs] = 새 글    +0x164 [RtlpSystem32Dirs+8] = 새 버퍼   (LdrpSystem32 는 안 고침)
              ├─ +0x171  RtlpSignalSystemDirsModification
              │           ├─ +0x1b  RtlAcquireSRWLockExclusive(RtlpCachedPathLock)
              │           ├─ +0x27 / +0x36 / +0x45  RtlpInvalidatePathCache(RtlpDllSearchPath / RtlpExeSearchPath / RtlpSearchPath)
              │           ├─ +0x54  RtlReleaseSRWLockExclusive
              │           └─ +0x70 / +0x8c / +0xa8  RtlFreeHeap (참조 0 이 된 블록만)
              └─ +0x180  LdrAddDllDirectory     +0x5c LdrpPolicyBits & 4 · +0x1b5 LdrpUserDllDirectories · +0x1e8 RtlpDllSearchPathWithOptions = 0

(<Windows> 글)
LdrpInitializeProcess
 ├─ +0xce1  RtlGetNtSystemRoot        PEB.SharedData null → 0x7ffe0030 (KUSD NtSystemRoot "C:\WINDOWS")
 ├─ +0xcf1  RtlInitUnicodeStringEx(&[RSP+0x120])
 └─ +0xd3b · +0x1128 · +0x114b  RtlAppendUnicodeStringToString   (System32 글 · <Windows>\system; · <Windows>;)
     (+0xfdb · +0x10bd  Flags & 0x20000000 → forwarders 조각 — test.exe 꺼짐)

(비트 60~61 — test.exe 는 0)
RtlpComputePath
 │  +0x21 / +0x2b / +0x32  R14 = (+0xa0 >> 60) & 3
 │  +0x37a / +0x460  R14B == 1 → [RSP+0x48] = 시스템 폴더 글 (코드 7 / 2)
 │  +0x138  RDI ≠ 0 → +0x671
 └─ +0x69e  메모리 복사 — 시스템 폴더 글을 항목 0 번으로 → 일반 고리에서 코드 2·7 건너뜀 (+0x613)

(DependentLoadFlags)
LdrpProcessMappedModule
 ├─ +0x10c  LdrInitSecurityCookie        다섯째 인자 [RSP+0x88] = 로드 구성
 └─ +0x14a  LdrpCfgProcessLoadConfig     +0x8c 구성 +0x4e → +0xa7 [항목+0x118] = 값 & 마스크
             └─ +0x683  LdrpIsModuleUnderSystem32 (걸러 낸 값 ≠ 0 일 때)  참이면 +0x69e 비트 9 (exe) / 8 (DLL)
                         (폴더 길이 > LdrpSystem32.Length + 2 일 때만 비교 — System32 바로 아래는 거짓)

(옵션 있는 검색 — test.exe 초기화 길엔 없음)
LdrpGetDllPath
 │  +0x162 OR 0xa00 (· +0x16c BTS 0xa) · +0x17f &RtlpDllSearchPathWithOptions · R8D = 플래그 → +0x192 JMP
 └─ +0xad   RtlpGetCachedPath   (+0x27 R8 ≠ 0 → 캐시 안 씀)
             └─ +0xee  RtlpComputeDllPathWithOptions   코드 5 → 1 → 6 → 0 → 7 → 8 (2·3·4 없음)
                         └─ +0xc9  RtlpComputePath
```

## 확인된 것

- `[정적]` **`LdrpInitializePerUserWindowsDirectory` 는 `TermsrvGetWindowsDirectoryW` 가 0 을 주면 `+0x4e` 에서 0 을 돌려주고 끝난다.** test.exe 는 칸 `pfnTermsrvGetWindowsDirectoryW` (kernel32 rva `0xc1d10`) 가 0 그대로 (run101) 라 이 쪽이다 → `RtlpSystemDirs` · `RtlpSystem32Dirs` · 캐시 셋 · `AddDllDirectory` 목록 모두 그대로, 진입점 뒤 `LoadLibraryExW` ×3 에 영향 없음. 칸 값 자체는 `[VM]` 한 줄 남음.
- `[정적]` 켜지는 환경에선 `+0x157` `RtlpSystemDirs` = 옛 글 + 사용자 Windows 폴더 + `;`, `+0x164` `RtlpSystem32Dirs` 버퍼 포인터만 고침, **캐시 셋은 비우기만** (`RtlpSignalSystemDirsModification` → `RtlpInvalidatePathCache` ×3), 같은 폴더를 `LdrAddDllDirectory` 로 넣는다. `LdrpSystem32` 은 안 고쳐 허공 포인터가 된다.
- `[정적]` **`LdrSystemDllInitBlock+0xa0` 비트 60~61 = 옵션 15 `PS_MITIGATION_OPTION_IMAGE_LOAD_PREFER_SYSTEM32`.** ntdll 에서 읽는 곳은 `RtlpComputePath +0x21` 하나. 1 이면 시스템 폴더 글을 **맨 앞으로 옮긴다** (나머지 순서 그대로). test.exe 0 `[관찰]` run46.
- `[정적+관찰]` **`DependentLoadFlags` = PE 로드 구성 `+0x4e` & 마스크** (`LdrpCfgProcessLoadConfig +0xa7`, 비트 8·9 는 System32 하위 폴더 모듈만 `+0x69e`). 6 모듈 파일 값 = 사진 P1b 값. sample 값 0 의 근거가 코드 · 파일 · 사진 셋이 됐다.
- `[정적]` `LdrpIsModuleUnderSystem32` 는 System32 **하위 폴더** 안 모듈만 참 (폴더 길이 > `LdrpSystem32.Length + 2`). run63 이 남긴 "ntdll 에서 거짓인 까닭" 이 닫혔다.
- `[정적+관찰]` **`<Windows>` = `RtlGetNtSystemRoot()`** (`LdrpInitializeProcess +0xcdc`) — PEB `SharedData` null 이라 KUSD `NtSystemRoot` = `C:\WINDOWS`.
- `[정적]` **`ProcessParameters.Flags & 0x20000000` 은 run32 비트 29 (`PspSetupUserProcessAddressSpace +0xe6`) 와 같은 칸 · 같은 비트.** ntdll 읽는 줄 넷.
- `[정적]` `RtlpComputeDllPathWithOptions` 칸 차례 `5 → 1 → 6 → 0 → 7 → 8`, `RtlpSystemDirs` · `PATH` · `.` 은 안 넣음, 캐시 안 씀.
- `[정적]` 기본 목록의 `.` 은 글자 `.;` 로만 들어가고, `CurrentDirectory` 는 `RtlGetFullPathName_Ustr` 가 읽는다 (갈래는 `[추측]`).

## 결과서와 다르게 적은 곳 · 보탠 것

- **칸 이름**: 결과서 1 (a) · 동적 1 의 `gpTermsrvGetWindowsDirectoryW` → **`pfnTermsrvGetWindowsDirectoryW`** 로 적었다. 같은 칸 (rva `0xc1d10`) 의 이름이 kernel32 `.asm` 주석 (`BaseDllFindIniFileNameMapping +0x66 MOV RAX,[0x1800c1d10] ; pfnTermsrvGetWindowsDirectoryW`) · sast/97 결과서 · run101 에서 `pfn…` 이다. 결과서의 `gp…` 는 옆 칸들 (`gpTermsrvGetComputerName` 등) 이름에서 옮겨 온 것으로 보인다 `[추측]`.
- **`RtlpDllSearchPath` 쓰는 줄**: 결과서는 "`RtlpDllSearchPath` ← `LdrpGetDllPath +0xa1`" 로 적었는데, `+0xa1` 은 **`LEA RCX,[RtlpDllSearchPath]` (주소 넘김)** 이고, 실제로 칸에 쓰는 줄은 **`RtlpGetCachedPath +0x111 MOV [R14],RDI`** (`RtlpCachedPathLock` 독점 `+0x107` 안, `+0x10c CMP [R14],RBX` 로 아직 비었을 때만) 이다 (flow-writer `.asm`). 짐 지도 3.8 `RtlpDllSearchPath` 줄 ("쓰는 줄 오프셋은 문서에 없음", `[추측]` · 짐작) 에 이 줄을 근거로 붙였다 — **짐작 → 정확으로 올릴지 (2장 숫자가 바뀜) 는 오케스트레이터 결정**으로 남겼다.
- **옵션 호출 자리**: 결과서 "옵션 목록 호출 (`LdrpGetDllPath +0x17f`)" 의 `+0x17f` 는 `LEA RCX,[RtlpDllSearchPathWithOptions]` 이고, call 은 `+0x192 JMP` 로 돌아가 기본과 **같은 `+0xa8`** 이다 (`.asm`). 표에 그렇게 적었다.
- **문서 줄 번호**: 결과서 "고칠 문서" 의 `baggage.md 779 · 782` 는 지금 **803 · 806**, `flow.md 2721 · 2768` 은 지금 **2795 (2.4 트리)** 와 **4226 (2.6 조건부 줄)** 이다 (그 뒤 바퀴들이 줄을 더해서). 내용 기준으로 고쳤다.
- **`TermsrvGetWindowsDirectoryW` 바이트**: flow-writer 가 `objdump` 로 같은 다섯 줄을 한 번 더 봤다 (`+0xe` 는 5바이트 `JMP rel32`).
- **보탠 것**: (c) 표의 모듈 이름 맞추기 (`BaseDllName` 줄), `LdrpPolicyBits = 0x16` 으로 마스크가 `0x7f00 | 0x8000` 쪽이라는 것 (run65 사진 값을 옮김).

## 정정

- **run39** (`runs/02_sprint/run39-dll-search-path.md` "시스템 폴더 대체 모드" 줄 · "아직 확인 필요" 2): "`LdrSystemDllInitBlock+0xa0` 비트 60~61 이 1 이면 **목록 전체를 시스템 폴더 하나로** 바꾼다" → **틀렸다.** 1 이면 시스템 폴더 글 (코드 2 `RtlpSystemDirs` 또는 7 `RtlpSystem32Dirs`) 이 **맨 앞 항목으로 옮겨지고** 나머지 칸은 원래 순서대로 남는다 (`RtlpComputePath +0x37a` · `+0x460` · `+0x671` · `+0x699` · `+0x613`). 비트 이름은 `PS_MITIGATION_OPTION_IMAGE_LOAD_PREFER_SYSTEM32` (=15). run39 문서 맨 위에 정정 한 줄을 넣었다.
  - test.exe 는 값 0 이라 실행 흐름 · 지도 결과는 안 바뀐다.
- flow.md 2.6 같은 문장 (조건부 줄) · 4장 run39 Q71 줄의 "(1 이면 목록이 시스템 폴더 하나 …)" 도 고쳤다.

## 이전 run 에서 닫은 것

- run39 · sast/34 "작은 정적 넷" **Q71** — (a) · (b) · (c) · (d) 와 번호 없이 남긴 셋 (`RtlpComputeDllPathWithOptions` 안 · `P+0x28 ~ +0x77` · `.` 칸) 모두 답. 남은 것: `P+0x28 ~ +0x77` 의 읽는 곳 (`[공백]`), `CurrentDirectory` 갈래 (아래 정적 1).
- run101 정적 ① (`LdrpInitializePerUserWindowsDirectory` + 넘겨받는 `TermsrvGetWindowsDirectoryW` 반환값) — 닫힘. run101 문서 그 줄에 **(해결됨 → run107, Q71)** 를 붙였다.
- run63 정적 ③ (`LdrpIsModuleUnderSystem32` 가 ntdll 에서 거짓인 까닭) — 닫힘 (폴더 길이 조건).
- run39 · flow.md 2.6 "부모 `+0x118` 이 0 이라는 건 `[추측: 링크 옵션]`" — `[정적+관찰]` 로.

## 아직 확인 필요

### 동적 (`[VM]`, 결과서 "동적분석으로 확인해야 할 것" — 사진으로 닫힌 것은 뺌)

1. **`kernel32+0xc1d10` (`pfnTermsrvGetWindowsDirectoryW`) = 0 인가** — 이 바퀴의 핵심 조건 한 칸. 같이 `ntdll+0xfb098` (`LdrpInitializePerUserWindowsDirectory +0x4c`) 에서 `r rax` = 0 이고 `+0x4e` 가 안 갈리는지. 멈춤점 하나 (`ntdll+0xfb098`, test.exe 조건). `+0x157` · `+0x16c` · `+0x17b` (`ntdll+0xfb1a3` · `0xfb1b8` · `0xfb1c7`) 는 안 서야 맞다.
   - 새 실행 없이도: 이미 있는 사진 덤프 (`ms/dump/03_baggage-snap/` P1b · P2) 에서 kernel32 `.data` `+0xc1d10` 를 오프라인으로 읽으면 닫힌다 (run101 동적 2 와 같은 방법, 짐 지도 7.33).
2. 사진 한 장 더 (같은 실행): `dq ntdll+0x1d2360 L2` (`RtlpSystemDirs`), `dq ntdll+0x1d3050 L1` (`RtlpDllSearchPath` 칸) 이 `+0x1b72` 앞뒤로 안 바뀌는지.
3. (다음 요청 후보와 짝) `RtlGetFullPathName_Ustr` 의 `CurrentDirectory` 읽기 3곳 중 test.exe 가 실제로 타는 곳.

- 결과서 동적 4 (`+0xa0` 비트 60~61 = 0, `+0x118` 값) 는 사진 (run46 · run44 P1b) 으로 이미 닫혀 뺐다.

### 정적 (결과서 "다음 요청 후보")

1. (작음) `RtlGetFullPathName_Ustr` (시작 `0x18003f3f0`) `+0xeed` / `+0xf1f` / `+0x1059` — 어느 갈래에서 `CurrentDirectory` 를 읽나, `.\name` 같은 상대 경로가 어떻게 풀리나.
2. (작음) `LdrpInitializePolicy` 의 `LdrpPolicyBits` 비트 1 · 2 · 4 가 각각 무슨 정책인가 (사진 `0x16`). 이번 코드가 `& 1` (Windows 두 조각 빠짐) · `& 4` (`AddDllDirectory` · 마스크 `0x7f00`) 를 쓴다.
3. (선택) `RtlGetSearchPath` · `RtlGetExePath` 의 목록 코드 (캐시 `RtlpSearchPath` · `RtlpExeSearchPath`) — 이번엔 "비워진다" 까지만.
4. (보탬, 짐 지도) 이번에 쓰는 줄이 보인 `.data` 전역 넷 — `RtlpExeSearchPath` · `RtlpSearchPath` · `RtlpDllSearchPathWithOptions` (0 쓰기, 조건부 안 탐) · `LdrpUserDllDirectories` (`LdrAddDllDirectory +0x1b5`, 조건부 안 탐 — 사진 `≤P1a` 값을 놓은 줄은 따로) — 짐 지도 7.3 "모름" 에서 3.8 로 옮길지 (2장 숫자가 바뀜, 오케스트레이터 결정). 그리고 `RtlpDllSearchPath` 짐작 → 정확 (위 "결과서와 다르게").

### PE_emulator 쪽 (결과서 "지금 우리 맞나" 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

- (a) **맞음, 주석만 낡음** — `LdrpInitializePerUserWindowsDirectory` 빈 몸통 (`Src/ntdll/LdrInit.cpp:5565`). 진짜도 test.exe 에선 아무것도 안 바꾸니 결과가 같다. 주석 "본문 asm 이 없어" 는 이제 asm 이 있어 낡았다. 진짜는 `TermsrvGetWindowsDirectoryW` 를 불러 0 을 받는 호출이 하나 있는데 우리는 안 한다 (줄임).
- (b) **맞음, 주석 보탬** — `Src/nt/Process.cpp:836 ~ 844` 가 24바이트를 0 으로 둠 (사진도 0). 주석 "알려진 읽기" 에 `RtlpComputePath +0x21` (비트 60~61) · `LdrGetProcedureAddressForCaller +0x685` 등이 빠짐 — 동작은 같다.
- (c) **맞을 것, 대조 한 번** — 진짜 `LdrpProcessMappedModule` 을 Unicorn 안에서 부르므로 (`Src/ntdll/LdrInit.cpp:4148`) `+0x118` 은 진짜 코드가 쓴다. `Src` 에 `DependentLoadFlags` 를 직접 쓰는 줄은 없다. 위 6 모듈 표 값과 진단으로 한 번 맞춰 볼 가치가 있다.
- (d) **맞음** — `Src/ntdll/LdrInit.cpp:3443` 이 `+0x8 & 0x20000000` 을 같은 줄로 읽고, `RtlGetNtSystemRoot` 를 진짜 코드로 부르고, `Src/nt/Process.cpp:868` 이 같은 비트를 꺼 둔다 (사진 `0x4001`).
- 5 **확인 필요** — `Src` 에 `RtlpComputeDllPathWithOptions` · 옵션 플래그 줄은 없다. `RtlpComputeDllPath` 길은 진짜 코드를 Unicorn 안에서 돌리는 것으로 보인다 (`Src/ntdll/LdrInit.cpp:5331` 주석). sample `LoadLibraryExW` 가 플래그를 주면 이 함수가 불릴 수 있다 (플래그 0 이면 안 불림).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntdll · ntkrnlmp pdb 줄, 근거 로그 (run107)), 1장 그림 2 메모 (`DependentLoadFlags` 0 근거 한 마디 — 노드 · 화살표는 그대로), 2.4 (절 머리 · 뒤7 트리 `+0x1b77` 줄 · 앞쪽 9 `<Windows>` 출처), 2.4 ntdll 항목 마무리 절 (`DependentLoadFlags` 쓰는 줄 · `LdrpIsModuleUnderSystem32` 조건), 2.5 (절 머리 · `TermsrvGetWindowsDirectoryW` 칸 줄), 2.6 (절 머리 · 파일로 올리는 길 `DependentLoadFlags` `[추측]` 줄 · 검색 경로 절 — 조건부 줄 **정정** · `<Windows>` · 비트 29 · 옵션 검색 · `.` 칸 · `P` 공유 줄 · `LdrpInitializePerUserWindowsDirectory` 줄), 4장 (2.6 Q23 줄 사진으로 닫힌 칸 · 2.6 Q71 줄 해결됨 + 정정 · run63 ③ 해결됨 · run101 ① 해결됨 · run107 새 줄 셋 — 동적 · 정적 · PE_emulator), 5장 (run107 · sast/102).
- baggage.md: 기준 (run107 줄 · 쉰일곱째판), 3.5 `DependentLoadFlags` 줄 (6 모듈 사진 · 파일 값 · 하위 폴더 조건), 3.8 `RtlpSystem32Dirs +0x8` · `RtlpSystemDirs` 둘째 줄 ("뜻 모름" → 뜻 + **조건부 — test.exe 안 탐**), 3.8 `RtlpDllSearchPath` 줄 (쓰는 줄 근거 붙임, 짐작 그대로), 3.9 `LdrSystemDllInitBlock` 줄 (`+0xa0` 비트 60~61 · 읽는 곳 전수), 새 7.38, 8장 (run107). **칸 수 · 줄 수 · 2장 숫자 그대로** (고친 줄만, 새 줄 없음).
- 이전 run 문서: run39 맨 위 정정 한 줄, run101 "아직 확인 필요" 정적 1 에 (해결됨 → run107, Q71).

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 중 디버거 칸을 읽는 것은 없다 `[정적: 읽은 줄 안에서]`. (`+0x1b40 LdrpDoDebuggerBreak` 는 바로 앞 줄이지만 이 갈래와 무관.)
- pitfalls 제안 없음. 도구는 run106 과 같은 `objdump` 풀이 재사용 (하네스 제안은 run106 에 이미 있음).

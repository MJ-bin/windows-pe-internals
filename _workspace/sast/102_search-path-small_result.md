# 102 search-path-small — 정적 분석 결과

- 요청서: `_workspace/sast/102_search-path-small.md` (Q71, 정적만)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), ntdll pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1`. 커널 PDB 는 `C29EBFB06B78B3C020DCA66D99713F9E1` (info.txt 로 기준과 같음 확인, 커널은 Ghidra 안 돌림).
- 새로 뽑은 함수: 3개 (`^(RtlpSignalSystemDirsModification|LdrAddDllDirectory)$` + `^RtlpInvalidatePathCache$`, depth 0) → `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`. 로그 `ms/ghidra/ntdll.export.sast102a.stdout`, `…102b.stdout` (둘 다 exit 0).
- 이미 있던 `.asm`·`.c` 를 그대로 읽음 (다시 안 뽑음): `LdrpInitializePerUserWindowsDirectory`, `RtlpComputeDllPathWithOptions`, `LdrpInitializeDllPath`, `LdrpComputeLazyDllPath`, `LdrpGetDllPath`, `RtlpComputePath`, `RtlpGetCachedPath`, `RtlpComputeDllPath`, `LdrpCfgProcessLoadConfig`, `LdrpInitializeProcess` 등. ntoskrnl 은 기존 `.asm` 두 개 (`PspSetupUserProcessAddressSpace`, `PspPrepareSystemDllInitBlock`) 를 **읽기만** 했다.
- 전수 검색: 이미 있던 스크래치 `objdump -d` 풀이 (`ntdll.dis`, 421,500줄, sast/100 이 만든 것) 를 다시 썼다. kernel32 는 `TermsrvGetWindowsDirectoryW` 20바이트와 export 표만 `objdump` 로 직접 읽음 (`.asm` 없음).
- 버전 확인: `LdrpInitializeProcess +0x1bb6` CALL / ret `+0x1bbb` → `LdrpInitializeGraphRecurse` **일치**. `+0x1b72` / ret `+0x1b77` → `LdrpInitializePerUserWindowsDirectory` **일치** (`_calls.tsv`, sast/97). 모듈 파일 값 6개 (`DependentLoadFlags`, 아래 3번) 가 사진 `[관찰]` 값과 모두 같아서 바이너리가 타깃 VM 것과 같다고 본다.
- 스크립트 문제: 없음. (`indirect:cfg:?` 는 `MOV RAX,RCX` 로 채운 칸이라 문서대로 `?`.)

## 한눈에

| # | 한 줄 답 |
|---|---|
| 1 (a) | **test.exe 에서는 이 함수가 아무것도 안 바꾸고 `+0x4e` 에서 0 을 돌려준다** `[정적]` (kernel32 `TermsrvGetWindowsDirectoryW` 가 0 을 주기 때문). 터미널 서비스 앱 호환 서버에서만 `RtlpSystemDirs` 뒤에 "사용자 Windows 폴더" 를 붙이고, 검색 경로 캐시 3개를 **비우고** (다시 짓지는 않음), 같은 폴더를 `AddDllDirectory` 에 넣는다. |
| 2 (b) | 비트 60~61 = PDB 열거형 `PS_MITIGATION_OPTION_IMAGE_LOAD_PREFER_SYSTEM32` (=15, 칸 = 번호×4). ntdll 에서 읽는 곳은 `RtlpComputePath +0x21` 한 곳. 값 1 이면 시스템 폴더 글이 **맨 앞으로 옮겨진다** (run39 의 "시스템 폴더 하나로 바꾼다" 는 틀렸다). 사진 값 0. |
| 3 (c) | 모듈 항목 `+0x118` 은 `LdrpCfgProcessLoadConfig +0xa7` 이 **PE 로드 구성 `DependentLoadFlags` (`+0x4e` WORD) & 마스크** 로 쓰고, System32 **하위 폴더** 안 모듈이면 `+0x69e` 에서 비트 8·9 를 더한다 (System32 바로 아래 모듈은 해당 없음). 다른 쓰는 줄 없음. 파일 값 6개가 사진과 모두 일치. |
| 4 (d) | `<Windows>` = `RtlGetNtSystemRoot()` (`LdrpInitializeProcess +0xcdc`) = 보통 KUSD `0x7ffe0030` `NtSystemRoot` (`"C:\WINDOWS"` `[관찰]`). `Flags & 0x20000000` 은 run32 비트 29 와 **같은 칸·같은 비트** (`RTL_USER_PROCESS_PARAMETERS+0x8`). |
| 5 | `RtlpComputeDllPathWithOptions` 의 조각 차례는 `5 → 1 → 6 → 0 → 7 → 8` (아래 표). `P+0x28 ~ +0x77` 을 읽는 곳은 못 찾음. `.` 칸은 이 함수 목록엔 없고, 기본 목록의 `.` 은 글자로만 들어가며 `RtlGetFullPathName_Ustr` 가 `CurrentDirectory` 를 읽는 줄이 있다. |

## 질문별 답 [정적]

### 1 (a) `LdrpInitializePerUserWindowsDirectory` (rva `0xfb04c`, 388바이트)

근거 파일: `ms/disassemble/ntdll/LdrpInitializePerUserWindowsDirectory.asm` / `.c`, `RtlpSignalSystemDirsModification.asm`, `RtlpInvalidatePathCache.asm`, `LdrAddDllDirectory.asm`, `RtlpGetCachedPath.asm`.

**부르는 쪽과 인자.**
- `LdrpInitializeProcess +0x1b6a MOV RCX,[RSP+0x188]` → `+0x1b72` CALL (ret `+0x1b77`). `[RSP+0x188]` 는 `LdrpInitializeKernel32Functions +0x12b` (ret `+0x130`) 가 채운 **kernel32 `TermsrvGetWindowsDirectoryW` 함수 주소** (sast/15, 이름 문자열 `ntdll+0x173bc8`).
- 앞 조건: `+0x1b4c TEST RAX,RAX` / `+0x1b4f JZ +0x1b83` — `Kernel32ThreadInitThunkFunction` 이 0 이면 이 함수를 아예 안 부른다. 뒤 조건: `+0x1b7b TEST EAX,EAX` / `+0x1b7d JS` (음수면 초기화 실패 꼬리).

**함수 안 차례.**
1. `+0x2d MOV RAX,RCX` (함수 주소) → `+0x47` CFG 중개 호출 (ret `+0x4c`), 인자 = 버퍼 `[RSP+0x60]` (0x104 글자) · `EDX=0x104`. 이게 `TermsrvGetWindowsDirectoryW(buf, 0x104)`.
2. `+0x4c ADD EAX,EAX` (글자 수 → 바이트) / `+0x4e JNZ +0x77`. **0 이면 `+0x50 XOR EAX,EAX` → `+0x75 RET` (성공 0).**
3. `+0x77 CMP [RtlpSystemDirs+8],0` / `+0x97 JZ +0x171`: `RtlpSystemDirs` 가 아직 없으면 (예: `LdrpPolicyBits & 1`) 글 바꾸기를 통째로 건너뛰고 `+0x171` 로.
4. 있으면 `+0xbc RtlAllocateHeap` (ret `+0xc1`, 크기 = 옛 길이 + 새 폴더 바이트 + 2). 실패하면 `+0xed LdrpLogInternal` (ldrinit.c 줄 `0x119e`, "Failed to reallocate the system dirs string !") 후 `+0xf2 MOV EAX,0xc0000017` → `+0xf7 JMP +0x52` (반환 `STATUS_NO_MEMORY`).
5. 새 버퍼에 이어 붙이기: `+0x112` 옛 `RtlpSystemDirs` (ret `+0x117`) → `+0x121` 새 폴더 `[RSP+0x40]` (ret `+0x126`) → `+0x132` `L";"` (ret `+0x137`).
6. `+0x14d RtlFreeHeap` (옛 `RtlpSystemDirs.Buffer`, ret `+0x152`).
7. 쓰기: `+0x157 MOVDQU [RtlpSystemDirs],XMM0` (16바이트 = 길이·최대길이·새 버퍼) · `+0x164 MOVQ [RtlpSystem32Dirs+8],XMM0` (새 버퍼 포인터).
8. `+0x16c RtlpSignalSystemDirsModification` (ret `+0x171`) → 캐시 비우기.
9. `+0x17b LdrAddDllDirectory(&새폴더, &쿠키[RSP+0x50])` (ret `+0x180`) → `+0x180 JMP +0x50` (반환 0). 반환값은 안 본다.

**바뀌는 글 · 캐시 표.**

| 대상 | 쓰는 줄 | 무엇이 어떻게 | 비고 |
|---|---|---|---|
| `RtlpSystemDirs` (`ntdll+0x1d2360`, 16B) | `+0x157` | 글 = 옛 글 + 사용자 Windows 폴더 + `;` (옛 글은 이미 `;` 로 끝남) | 읽는 곳: `RtlpComputePath +0x456 / +0x524 / +0x53b` (`_refs`) |
| `RtlpSystem32Dirs.Buffer` (`ntdll+0x1d2338`) | `+0x164` | 새 버퍼 포인터만 (길이 칸은 그대로) | 앞 버퍼를 풀어서 이 포인터가 허공을 가리키지 않게 고치는 것. 읽는 곳 `RtlpComputePath +0x3ac`, `RtlpAddSystemPath +0x20` |
| `LdrpSystem32` (`ntdll+0x1d2990`) | **안 고침** | `LdrpInitializeProcess +0x10a6` 이 같은 버퍼 포인터를 넣어 둔 칸인데 여기서 안 바꾼다 → 옛 버퍼가 풀린 뒤 **허공 포인터** `[정적]`. 읽는 곳 `LdrpIsModuleUnderSystem32 +0x49` | 이 길은 test.exe 에서 안 타서 영향 없음. 타는 환경에서의 결과는 `[추측]` |
| 검색 경로 캐시 3개 | `+0x16c` → `RtlpSignalSystemDirsModification` | `RtlpCachedPathLock` 독점 안에서 `RtlpDllSearchPath` (`+0x22`) · `RtlpExeSearchPath` (`+0x31`) · `RtlpSearchPath` (`+0x40`) 칸을 0 으로 (`RtlpInvalidatePathCache`) → 참조가 0 이 된 블록은 `+0x6b / +0x87 / +0xa3 RtlFreeHeap` | **무효로 한다, 지금 다시 짓지 않는다.** 다음에 쓸 때 `RtlpGetCachedPath +0x43 JZ` → 다시 계산 → `+0x111` 에서 다시 저장 |
| `AddDllDirectory` 목록 | `+0x17b` → `LdrAddDllDirectory` | `LdrpUserDllDirectories` 끝에 그 폴더 (`+0x1b5`), 그리고 `RtlpDllSearchPathWithOptions` 칸을 0 으로 (`+0x1e8`) | `+0x5c TEST [LdrpPolicyBits],4` 가 꺼지면 `0xc000000d` (사진 `LdrpPolicyBits = 0x16` `[관찰]` run65 → 켜짐) |

- `RtlpInvalidatePathCache` (`+0x0 ~ +0x18`): 칸 값을 읽고 0 으로 쓰고 (`+0x5`), 블록 참조수 `[+0x50]` 를 1 내려서 0 이 되면 블록 포인터를 돌려준다 (`+0xf ~ +0x14`). 그래서 **옛 경로 구조체 `P` 가 이미 그 블록을 들고 있으면 (참조수가 남아) 블록은 안 풀린다** — `P` 는 `LdrpReleaseDllPath` (`LdrpInitializeProcess +0x1bfb`) 때 내려놓는다 `[정적]`.
- 캐시 3개의 쓰는 쪽: `RtlpDllSearchPath` ← `LdrpGetDllPath +0xa1` (DLL 올릴 때 목록), `RtlpSearchPath` ← `RtlGetSearchPath +0x10`, `RtlpExeSearchPath` ← `RtlGetExePath +0x1a` (이름으로 본 추측: 파일 찾기·프로그램 실행용 `[추측]`).
- `RtlpDllSearchPathWithOptions` 칸은 `RtlpGetCachedPath` 가 `R8 ≠ 0` 이면 저장을 안 해서 (`+0x27 JNZ +0xa7` → `R15B = 0`) 옵션 목록 호출 (`LdrpGetDllPath +0x17f`, `R8D = 플래그 ≠ 0`) 에서는 채워지지 않는 걸로 보인다 `[정적 추정]`. 그러면 `LdrAddDllDirectory +0x1e8` 의 0 쓰기는 늘 빈 칸에 쓰는 것.

**test.exe 에서 타나? — 아니오 (정적), `[VM]` 한 줄로 확인.**
- 조건 칸 = kernel32 전역 `gpTermsrvGetWindowsDirectoryW` (`kernel32+0xc1d10`). `kernel32!TermsrvGetWindowsDirectoryW` (rva `0x35f10`) 는 `+0x0 MOV RAX,[kernel32+0xc1d10]` / `+0x7 TEST` / `+0xa JNZ +0xe` / `+0xc RET` — 칸이 0 이면 그대로 0 을 돌려주고, 아니면 CFG 로 그 칸의 함수 (tsappcmp 쪽) 로 간다 (`+0xe JMP`).
- 그 칸은 `BasepInitializeTermsrvFpns` 가 `tsappcmp.dll` 을 올려서 `GetProcAddress` 로 채울 때만 (표 `PTR_s_TermsrvGetComputerName_180089310` 의 4번째 줄: 이름 → 칸 `0xc1d10`) 채워진다. test.exe 는 `DllCharacteristics = 0x8160` (TS 인식) 이라 그 갈래에 안 들어가고 `tsappcmp.dll` 을 안 올린다 (sast/97 질문 2).
- 그래서 `+0x4e JNZ` 안 탐 → `+0x75 RET` 0. **`RtlpSystemDirs`·`RtlpSystem32Dirs`·캐시·`AddDllDirectory` 모두 그대로.**
- 그 결과: 진입점 뒤 `LoadLibraryExW` ×3 에 영향 **없음** `[정적]`. 켜지는 환경 (TS 앱 서버) 에서도 `P` 가 이미 들고 있는 목록은 그대로 두고, **그 뒤 새로 시작하는 LoadLibrary 가 `LdrpInitializeDllPath` 로 `P` 를 0 으로 비운 뒤** (`LdrpInitializeDllPath +0x13 ~ +0x35`) 캐시를 다시 짓는다 → 새 글이 보인다 `[정적]`.
- `LdrpComputeLazyDllPath`·`LdrpGetDllPath`·`LdrpInitializeDllPath` 세 함수에는 `RtlpSystemDirs`·`<Windows>` 글이 없다 (`_refs` 읽는 곳 목록에 없음). `P+0x00` 은 한 번 짓고 (`LdrpComputeLazyDllPath +0x2f JNZ` 로 이미 있으면 건너뜀) 무효화 신호를 안 받는다.

**지금 우리 (PE_emulator):** **결과는 맞음.** `LdrpInitializePerUserWindowsDirectory` 가 빈 몸통 (`Src/ntdll/LdrInit.cpp:5565`, 주석은 "본문 asm 이 없어" 로 낡음 → 이제 asm 있음) 이고 test.exe 에서는 진짜도 아무것도 안 바꾸니 같다. 다만 진짜는 `TermsrvGetWindowsDirectoryW` 를 불러 0 을 받는 호출이 하나 있는데 우리는 그 호출을 안 한다 (줄임). `baggage.md` 779·782 줄은 **test.exe 에서 안 일어난다** 로 고쳐야 한다 (아래 "고칠 문서").

### 2 (b) `LdrSystemDllInitBlock+0xa0` 비트 60~61

근거 파일: `RtlpComputePath.asm`, `LdrGetProcedureAddressForCaller.asm`, `LdrControlFlowGuardEnforcedWithExportSuppression.asm`, `LdrpInitializeProcess.asm`, `RtlRestoreContext.asm`, `_refs/LdrSystemDllInitBlock.tsv`, PDB `PS_MITIGATION_OPTION`.

- **뜻**: 블록 `+0xa0` 은 커널이 `PspPrepareSystemDllInitBlock +0x12b / +0x138` 로 복사하는 24바이트 완화 옵션 지도 (run32). 칸 = **옵션 번호 × 4 비트** (2비트가 상태). PDB 열거형 `PS_MITIGATION_OPTION` (ntdll PDB 형 표) 에서 번호 15 = `PS_MITIGATION_OPTION_IMAGE_LOAD_PREFER_SYSTEM32` → 15×4 = 60. 칸 배치는 코드의 다른 읽기 4곳으로 맞춰 봤다 (번호 3 `HEAP_TERMINATE` = 비트 12~13 ← `LdrpInitializeProcess +0xa1f SHR 0xc`, 번호 10 `CONTROL_FLOW_GUARD` = 40~41 ← `LdrGetProcedureAddressForCaller +0x68c SHR 0x28`, 번호 17 `LOADER_INTEGRITY_CONTINUITY` = 둘째 8바이트 비트 4 ← `+0x4d2 SHR 4`, 번호 19 `RESTRICT_SET_THREAD_CONTEXT` = 둘째 8바이트 비트 12 ← `RtlRestoreContext +0x16d BT 0xc`). 값 이름 (`ALWAYS_ON` 등) 은 SDK 기억이라 `[추측: 이름]`, **코드가 보는 값은 1 뿐**이다.
- **ntdll 에서 읽는 곳 (전수, `objdump` 로 `0x1801ea4f0` 참조 전부)**:

| 읽는 줄 | 보는 비트 | 쓰임 |
|---|---|---|
| `RtlpComputePath +0x21 MOV R14,[+0xa0]` → `+0x2b SHR 0x3c` → `+0x32 AND 3` | **60~61** | `+0x37a CMP R14B,1` / `+0x460 CMP R14B,1` (아래). 1 이 아니면 (0·2·3) 영향 없음 |
| `LdrGetProcedureAddressForCaller +0x685`, `LdrControlFlowGuardEnforcedWithExportSuppression +0x13` | 40~41 | CFG 내보내기 억제 (값 3) |
| `LdrpInitializeProcess +0x4a4` (16바이트 복사 + `+0x4d2`) | 68~69 | `LdrpEnforceIntegrityContinuity` · `LdrpAuditIntegrityContinuity` |
| `LdrpInitializeProcess +0x9fa` (복사 + `+0xa1f`) | 12~13 | `RtlSetHeapInformation` (run46) |
| `RtlRestoreContext +0x150 / +0x162`, `RcConsolidateFrames +0xe / +0x32` | 둘째 8바이트 비트 60·12 | `+0xa8` 쪽 |

  `+0x4a4`·`+0x9fa` 의 스택 사본 (`[RSP+0x250 / 0x270 / 0x290]`) 은 `LdrpInitializeProcess` 안에서 더 읽지 않는다.
- **`RtlpComputePath` 에서 값 1 이 하는 일** `[정적]`:
  1. 칸 종류 코드 **2** (`RtlpSystemDirs`, `+0x456 ~ +0x484`) 또는 **7** (`RtlpSystem32Dirs`, `+0x367 ~ +0x397`) 를 만나면, 크기를 더한 뒤 `+0x37a / +0x460 CMP R14B,1` 이 맞을 때 `[RSP+0x48]` 에 그 UNICODE_STRING 주소를 적는다 (`+0x397` / `+0x484`, 마지막 것이 이긴다).
  2. 블록을 만든 뒤 `+0x135 TEST RDI,RDI` / `+0x138 JNZ +0x671`. `+0x671` 은 **그 시스템 폴더 글을 항목 0 번으로 먼저 복사** (`+0x699` memcpy, 항목 코드 `+0x68f` 에 7 또는 2) 하고 개수를 1 로 둔 채 (`+0x6a1`) 일반 고리 (`+0x13e`) 로 간다.
  3. 일반 고리에서 `RDI ≠ 0` 이면 (`+0x165 TEST RDI` / `+0x168 JNZ +0x60c`) 코드 2·7 인 칸은 건너뛴다 (`+0x613 JZ +0x204` / `+0x61c ~ +0x622`) 나머지 칸은 원래 순서대로 쓴다.
  - 뜻: **"시스템 폴더 글을 맨 앞으로 옮기고 나머지는 그대로"** (PreferSystem32Images 이름과 맞다). 기본 목록 줄 1 (`5 2 4 3 8`) 이면 `2 5 4 3 8` 이 된다. **run39 (`flow.md` 2768 줄 등) 의 "목록 전체가 시스템 폴더 하나로" 는 틀렸다 — 정정 필요.**
- test.exe: 사진 `ntdll+0x1ea4f0` = 0 `[관찰]` (run46) → 안 탐.

**지금 우리 (PE_emulator):** 맞음. `Src/nt/Process.cpp:836 ~ 844` 가 24바이트를 0 으로 두고 사진도 0 이다. 다만 같은 파일 주석 "알려진 읽기는 앞 16B 비트 (`+0x4a4`, `+0x9fa`)" 에 `RtlpComputePath +0x21` (비트 60~61) 과 `LdrGetProcedureAddressForCaller +0x685` 등이 빠져 있다 — 값이 0 이라 동작은 같고 주석만 더하면 된다.

### 3 (c) 모듈 항목 `+0x118 DependentLoadFlags`

근거 파일: `LdrpCfgProcessLoadConfig.asm`, `LdrpProcessMappedModule.asm`, `LdrpAllocateModuleEntry.asm`, `LdrInitSecurityCookie.asm`, `LdrpMapDllSearchPath.asm`, `LdrpValidateIntegrityContinuity.asm`. PDB `_LDR_DATA_TABLE_ENTRY +0x118 DependentLoadFlags`.

- **ntdll 전수 (`objdump` `+0x118]` 쓰기·읽기 중 항목 칸 맞는 것)**:

| 줄 | 하는 일 |
|---|---|
| `LdrpCfgProcessLoadConfig +0xa7 MOV [RSI+0x118],ECX` | **쓰기 (주 경로).** `+0x8c MOVZX ECX,word [RDI+0x4e]` = 로드 구성 디렉터리 **`DependentLoadFlags` (`+0x4e` WORD)** (`+0x80 CMP [RDI],0x94 / +0x86 JC` 로 구성 크기 ≥ `0x94` 일 때만). 마스크 `+0x90 ~ +0xa5` = `(((LdrpPolicyBits & 4) \| 0x7b) << 8) \| 0x8000` (`0x7b00` 또는 `0x7f00`, 거기에 `0x8000`). |
| `LdrpCfgProcessLoadConfig +0x692 / +0x69e` | **고쳐 쓰기.** `+0xad TEST EDX,ECX / +0xaf JNZ +0x67b` (검색 비트가 하나라도 있으면) → `+0x67e LdrpIsModuleUnderSystem32` (ret `+0x683`) 가 참일 때, 항목이 exe (`+0x68b CMP RSI,[LdrpImageEntry]`) 면 비트 9 (`+0x69a BTS 9`, `0x200`), 아니면 비트 8 (`+0x6f1 BTS 8`, `0x100`) 를 더해 다시 쓴다. `LdrpIsModuleUnderSystem32` 는 `FullDllName − BaseDllName` (폴더 길이) 가 `LdrpSystem32.Length + 2` 보다 **길 때만** (`+0x1a ~ +0x22 JA +0x2f`) 앞부분을 `LdrpSystem32` 와 비교한다 → **System32 하위 폴더 안 모듈만 참, System32 바로 아래 모듈 (ntdll·KernelBase) 은 거짓** `[정적]`. |
| `LdrpMapDllSearchPath +0xc1 MOV R9D,[RCX+0x118]` | 읽기 (부모 항목) — run39. |
| `LdrpValidateIntegrityContinuity +0x33 TEST [RCX+0x118],0x8000` | 읽기 (`0x8000` 비트). 부르는 곳 `LdrpMapDllNtFileName +0x407`. |

  다른 쓰는 줄은 없다. `LdrpCfgProcessLoadConfig` 를 부르는 곳은 `LdrpProcessMappedModule +0x145` (ret `+0x14a`) 와 `AvrfMiniLoadDll +0x869` 둘뿐.
- **값을 어디서 가져오나**: `LdrpProcessMappedModule` 이 `LdrInitSecurityCookie` (`+0x107`, ret `+0x10c`) 의 다섯째 인자 `[RSP+0x88]` 로 받은 로드 구성 포인터 (`LdrInitSecurityCookie +0xe8 LdrImageDirectoryEntryToLoadConfigEx` → `+0x4a MOV [RCX],R8`) 를 `+0x13a MOV R8,[RSP+0x88]` 로 `LdrpCfgProcessLoadConfig` 에 넘긴다 = **PE 로드 구성 디렉터리**.
- **값이 없을 때**: 항목은 `LdrpAllocateModuleEntry +0x23 / +0x29` 에서 0x138 바이트를 `HEAP_ZERO_MEMORY` (플래그 `|= 8`, `+0x20`) 로 받아 0 으로 시작한다. 로드 구성이 없거나 크기 < `0x94` 면 `+0x7a JZ / +0x86 JC → +0x454` 로 나가서 안 쓴다 → 0 그대로.
- **파일 값 vs 사진 대조 (6개 모두 일치)**: `PE 로드 구성 +0x4e` 를 파일에서 직접 읽으면 test.exe `0`, dll1 `0`, dll2 `0`, kernel32 `0`, ntdll `0x800`, KernelBase `0x800`. 사진 (`ms/logs/04_baggage-read_P1b.txt:3439 / 3544 / 3649 / 3754 / 3859 / 3964`) 의 같은 모듈 `+0x118` 값과 똑같다 `[관찰]`. 그러니 "sample 값 0 의 근거가 링크 옵션뿐" 은 이제 **코드 (구성 `+0x4e` & 마스크) + 파일 값 + 사진** 셋으로 닫힌다. test.exe·dll1·dll2·kernel32 는 파일 값 자체가 0 이라 `+0xad TEST EDX,ECX` 가 0 → `+0x67b` 안 가고, ntdll·KernelBase 는 `0x800` 이 마스크를 통과하지만 System32 바로 아래라 `LdrpIsModuleUnderSystem32` 가 거짓 → 비트 8·9 안 더해져 사진 `0x800` 그대로다 (모두 사진과 같음).
- `baggage.md 599` 줄 (`+0x118` ② `+0x8c ~ +0xa7`) 은 이미 이 쓰기를 가리키고 있어서 맞다. 비트 8·9 를 더하는 갈래 (`+0x69e / +0x6f5`) 는 문서에 더 있으면 좋다 (ntdll 은 `LdrpIsModuleUnderSystem32` 거짓 — run63, 사진 `0x800`).

**지금 우리 (PE_emulator):** 맞을 것 (확인 필요 한 곳). 우리는 진짜 `LdrpProcessMappedModule` 을 Unicorn 안에서 부르므로 (`Src/ntdll/LdrInit.cpp:4148`) `+0x118` 은 진짜 코드가 쓴다. `Src` 에 `DependentLoadFlags` 를 직접 쓰는 줄은 없다. 파일에서 읽은 값과 진단으로 한 번 대조할 가치가 있다.

### 4 (d) `<Windows>` 글 출처, `Flags & 0x20000000`

근거 파일: `LdrpInitializeProcess.asm`, `RtlGetNtSystemRoot.asm`, ntoskrnl `PspSetupUserProcessAddressSpace.asm` (읽기만).

- **`<Windows>` 글** = `LdrpInitializeProcess +0xcdc CALL RtlGetNtSystemRoot` (ret `+0xce1`) → `+0xcec RtlInitUnicodeStringEx(&[RSP+0x120], 그 결과)` (ret `+0xcf1`). 이 `[RSP+0x120]` 가 `<Windows>` 로 쓰이는 곳: System32 글 (`+0xd36 Append`, ret `+0xd3b`, 이어 `+0xd4a` `SlashSystem32SlashString`), 그리고 `RtlpSystemDirs` 의 `<Windows>\system;<Windows>;` 두 조각 (`+0x1123 / +0x1146 Append`, `+0x1134` 가 `\system;` `ntdll+0x19bd08` 글, `+0x1157` 이 `;`). 글자 상수 3개는 PE 파일에서 읽음 (`\system;`, `;`, `forwarders\alt;` / `forwarders;`).
- `RtlGetNtSystemRoot` (`+0x0 ~ +0x35`): `+0x0 MOV RAX,GS:[0x60]` → `+0x9 MOV RAX,[RAX+0x90]` (**`PEB.SharedData`**, PDB `_PEB +0x90`) → 0 이거나 그 첫 DWORD 가 0 (`+0x10 TEST` / `+0x1c CMP [RAX],0`) 이면 **`+0x15 MOV EAX,0x7ffe0030` = KUSD `NtSystemRoot`** (PDB `_KUSER_SHARED_DATA +0x30`, WCHAR[260]). 아니면 (서버 silo) `+0x31 ADD RAX,0x1e` = `SharedData+0x1e` (PDB `_SILO_USER_SHARED_DATA +0x1e NtSystemRoot`).
- test.exe: `PEB.SharedData = (null)` `[관찰]` (`04_baggage-read_P1b.txt:2462`), KUSD `NtSystemRoot = "C:\WINDOWS"` `[관찰]` (`04_baggage-read_P2.txt:4028`) → **`<Windows>` = `C:\WINDOWS`.**
- **`ProcessParameters.Flags & 0x20000000` = run32 비트 29, 같은 칸·같은 비트** `[정적]`:
  - ntdll 읽는 줄 (전수, `objdump` `0x20000000` 상수): `LdrpInitializeProcess +0xfdb` (`MOV RAX,[RDI+0x20]` = `PEB.ProcessParameters`, `TEST [RAX+0x8],0x20000000`) · `+0x10bd` (같은 모양) · `LdrpInitializeKernel32Functions +0x21a` · `RtlpComputeDllPathWithOptions +0x92`. 전부 `[PEB+0x20]+0x8`. PDB `_RTL_USER_PROCESS_PARAMETERS +0x8 Flags`.
  - 커널 쓰는 줄: `PspSetupUserProcessAddressSpace +0xe6 BTS EBX,0x1d` → `+0xea MOV [RBP+0x8],EBX` (조건 `+0xdd CMP [RAX+0x3f8],0` = `OneCoreForwardersEnabled`). 같은 오프셋 `+0x8`, 같은 비트 29.
  - 켜지면 ntdll 이 하는 일: `RtlpSystemDirs` 에 `<Windows>\System32\forwarders\alt;` (`LdrpIncludeAlternateForwarders` 참일 때, `+0x10c6 ~ +0x10ee`) 와 `...forwarders;` (`+0x10f2 ~ +0x1111`) 를 붙임, `RtlpComputeDllPathWithOptions` 는 4번째 인자 (forwarders 포함) 를 켬 (아래 5번 표), `LdrpInitializeKernel32Functions` 는 kernel32 찾기 건너뜀 (run20).
  - test.exe: `Flags = 0x4001` `[관찰]` (run65 / run44) → 비트 29 꺼짐 → 안 붙음.

**지금 우리 (PE_emulator):** 맞음. `Src/ntdll/LdrInit.cpp:3443` 이 `+0x8 & 0x20000000` 을 같은 줄로 읽고, `RtlGetNtSystemRoot` 를 진짜 코드로 부르고, `Src/nt/Process.cpp:868` 이 같은 비트를 "꺼짐" 으로 둔다 (사진 `0x4001`).

### 5 `RtlpComputeDllPathWithOptions` 안쪽 (rva `0xba650`, 315바이트)

근거 파일: `RtlpComputeDllPathWithOptions.asm` / `.c` (Ghidra `.c` 와 asm 이 같은 해석), `LdrpGetDllPath.asm`, `RtlpGetCachedPath.asm`, `RtlpComputePath.asm`.

- **인자**: `RCX` = 플래그 (`EBX`), `RDX` = 경로 글 (`RDI`, 그대로 `RtlpComputePath` 의 `R8` — 코드 5 가 그 글의 폴더를 씀). 잠금 `LdrpDllDirectoryLock` 공유 (`+0x2b / +0xd3`).
- **플래그 → 칸 종류 코드 차례** (`+0x30 ~ +0xb1`, 스택 `[RSP+0x20..]` 에 dword 로 쌓음):

| 순서 | 줄 | 플래그 비트 | 쌓는 코드 | 뜻 (`RtlpComputePath` 표, run39) |
|---|---|---|---|---|
| 1 | `+0x34 ~ +0x46` | 8 (`0x100`) | **5** | 응용 프로그램 폴더 (경로 글이 있으면 그 폴더, 없으면 exe 폴더) |
| 2 | `+0x51 ~ +0x5d` | 9 (`0x200`) | **1** | exe 폴더 |
| 3 | `+0x65 JC +0x102` → `+0x102 ~ +0x125` | 10 (`0x400`) | **6**, 그리고 `LdrpDllDirectory` 길이 ≠ 0 (`+0x108`) 이면 이어서 **0** | `LdrpUserDllDirectories` (AddDllDirectory 목록), 이어서 SetDllDirectory 폴더 |
| 4 | `+0x6f TEST EBX,0x4800` | 11 (`0x800`) 또는 14 (`0x4000`) | **7** | `RtlpSystem32Dirs` (System32 하나) |
| 5 | `+0xa2 CMP [LdrpAppPackagesPath],BP` | (플래그 아님) 패키지 글 길이 ≠ 0 | **8** | `LdrpAppPackagesPath` |

  - 코드 **2** (`RtlpSystemDirs` 덩어리) · **3** (`PATH`) · **4** (`.`) 는 **이 함수 목록에 절대 안 들어간다.** 그래서 "옵션 있는" 검색은 PATH·현재 폴더를 안 본다 `[정적]`.
  - 4번째 인자 (forwarders 포함) = `ProcessParameters.Flags & 0x20000000` 이 켜져 있고 (`+0x92 TEST` / `+0x99 JNZ +0x12a`) **그리고** 플래그 비트 11 (`0x800`) 이 켜졌을 때만 1 (`+0x12a MOV R9B,1` / `+0x12d BT 0xb` / `+0x131 JC +0xa2`, 아니면 `+0x137 JMP +0x9f` 에서 0). `0x4000` 만이면 0.
  - 반환 블록에 `+0xdd MOV [RBX+0x74],1` 표시 (경로 안에 PATH 가 없다는 뜻 — `RtlpGetCachedPath +0x5a CMP [RBX+0x74],DIL` 에서 환경 버전 `+0x3f8` 비교를 건너뜀 `[추측: 이름]`).
  - 플래그 `0x1000` (기본 폴더들) 은 이 함수에 들어오기 전에 `LdrpGetDllPath +0x15c ~ +0x16c` 에서 `0xa00` 이 더해진다 (+ `LdrpPolicyBits & 4` 면 `0x400` 도). `+0x17a BTS R8D,8` / `+0x18e CMOVZ` 는 `[RSP+0x78]` 표시 (`+0x246 ~ +0x254`, 플래그가 정확히 `8` 일 때 켜지는 갈래 — SDK 이름 `LOAD_WITH_ALTERED_SEARCH_PATH` `[추측: 이름]`) 가 켜져 있을 때 `0x100` 을 더한다.
  - **이 함수는 캐시를 안 쓴다**: `LdrpGetDllPath +0x16c ~ +0x192` 가 넘기는 `R8D` (플래그) ≠ 0 이라 `RtlpGetCachedPath +0x27 JNZ +0xa7` 로 가서 매번 새로 계산, 저장 안 함 (위 1번 표 설명).
- **`P+0x28 ~ +0x77` 읽는 곳**: 쓰는 곳은 `LdrpGetDllPath +0xca ~ +0xec` (캐시 블록 앞 0x50바이트를 `LdrpComputeLazyDllPath +0x43` 이 넘긴 `P+0x28` 으로 복사). **읽는 곳은 못 찾았다.** `P` 를 받는 함수를 `_map` 으로 다 따라가 봤다: `LdrpSearchPath` 는 `P+0x0 / +0x8 / +0x10` 만 읽음 (`+0xe4`, `+0x102`, `+0x10c`, `+0x159`, `+0x1cc` …), `LdrpReleaseDllPath` 는 `P+0x7c · +0x0`, `LdrpMapDllSearchPath` / `LdrpFindLoadedDllInternal` / `LdrpFindLoadedDll` 은 `P` 를 그대로 넘기기만 (`+0xe1`, `+0x10e`, `+0x211`, `+0x226`), `LdrpInitializeDllPath` 는 0 으로 채움, `LdrpComputeLazyDllPath` 는 값을 `P+0x28` 으로 넘김. 이 범위 안에서는 읽는 곳이 없다 `[정적]` (다른 함수가 `P` 를 레지스터로 몰래 받을 가능성은 `[공백]`).
- **`.` 칸이 `CurrentDirectory` 로 풀리나**:
  - `RtlpComputeDllPathWithOptions` 목록에는 `.` 칸 (코드 4) 이 **없다** (위 표).
  - 기본 목록 (`RtlpComputeDllPath`, 줄 0·1) 의 `.` 은 `RtlpComputePath +0x270 ~ +0x287` 이 **글자 `.;` 만 써 넣는다** (`+0x277 MOV EAX,[ntdll+0x17e304]` → `+0x27d MOV [R14],AX`, 이어 `;`). `RtlpComputePath` 안에서 `PEB.ProcessParameters.CurrentDirectory` 를 읽는 줄은 없다 (`GS:[0x60]` 은 `+0xe9`, `+0x215`, `+0x239`, `+0x2cc` 뿐, 힙·`FastPebLock` 용).
  - 풀리는 곳은 후보 경로를 전체 경로로 만들 때다: `LdrpResolveDllName` → `LdrpGetFullPath` → `RtlGetFullPathName_Ustr` 안에 `[PEB+0x20]+0x38` (= `ProcessParameters.CurrentDirectory`, PDB `+0x38 _CURDIR`) 를 읽는 줄이 **`+0xeed`, `+0xf1f`, `+0x1059` 셋** 있다 (`objdump`, 시작 `0x18003f3f0`). 어느 갈래에서 읽는지(상대 경로일 때인지) 는 안 읽었다 `[추측: 상대 경로 갈래]`.

**지금 우리 (PE_emulator):** 확인 필요. `Src` 에 `RtlpComputeDllPathWithOptions`·옵션 플래그 줄은 없고, `RtlpComputeDllPath` 길은 진짜 코드를 Unicorn 안에서 돌리는 것으로 보인다 (`Src/ntdll/LdrInit.cpp:5331` 주석). sample 의 `LoadLibraryExW` 가 플래그를 주는지에 따라 이 함수가 불릴 수 있다 → 플래그가 0 이면 안 불리고 `RtlpComputeDllPath` 길이다.

## 고칠 문서 (결과서가 하지 않음 — 오케스트레이터·flow-writer 몫)

- `runs/03_sprint/baggage.md` 779·782 줄 (`RtlpSystem32Dirs +0x8` 다시 쓰기, `RtlpSystemDirs` 다시 쓰기, "뜻 모름, dll 올리기 뒤"): **뜻이 정해졌고 test.exe 에서는 안 일어난다** (`+0x4e` 에서 0 반환, `[정적]` + `[VM]` 확인 한 줄).
- `runs/03_sprint/flow.md` 2721 줄 ("뜻 모름") 과 2768 줄 근처 (`LdrSystemDllInitBlock+0xa0` 비트 60~61 이 1 이면 목록이 시스템 폴더 하나로): **"시스템 폴더 글이 맨 앞으로" 로 정정**. `run39` 문서도 같은 문장.
- 3번 `DependentLoadFlags` 의 비트 8·9 더하는 갈래 한 줄 추가 후보.

## 동적분석으로 확인해야 할 것

1. `kernel32+0xc1d10` (`gpTermsrvGetWindowsDirectoryW`) = 0 인가. 이게 이 요청서의 핵심 조건 한 칸이다. 같이 `ntdll!LdrpInitializePerUserWindowsDirectory +0x4c` (`ntdll+0xfb098`) 에서 `rax` = 0 이고 `+0x4e` 가 안 갈리는지.
2. 사진 한 장 안 더: `dq ntdll+0x1d2360 L2` (`RtlpSystemDirs`), `dq ntdll+0x1d3050 L1` (`RtlpDllSearchPath` 칸) 이 `+0x1b72` 앞뒤로 안 바뀌는지.
3. 추천 멈춤점: **`ntdll+0xfb098`** (test.exe 에서만 조건). 하나면 된다. 나머지 (`+0x157`, `+0x16c`, `+0x17b`) 는 안 서야 맞다.
4. `LdrSystemDllInitBlock+0xa0` 비트 60~61 = 0 은 이미 사진 (run46) 으로 닫힘. `+0x118` 도 사진으로 닫힘. 추가 동적 필요 없음.
5. `RtlGetFullPathName_Ustr` 의 `CurrentDirectory` 읽기 3곳 중 test.exe 가 실제로 타는 곳 (필요하면 다음 요청 후보).

## 다음 요청 후보

- (정적, 작음) `RtlGetFullPathName_Ustr` 의 `+0xeed / +0xf1f / +0x1059` — 어느 갈래에서 `CurrentDirectory` 를 읽나, 상대 경로 `.\name` 이 어떻게 풀리나.
- (정적, 작음) `LdrpInitializePolicy` 의 `LdrpPolicyBits` 비트 1·2·4 가 각각 무슨 정책인가 (사진 `0x16`). 이번 코드가 `& 1` (Windows 두 조각 빠짐), `& 4` (`AddDllDirectory`·마스크 `0x7f00`) 를 쓴다.
- (정적, 선택) `RtlGetSearchPath`·`RtlGetExePath` 의 목록 코드 (캐시 `RtlpSearchPath`·`RtlpExeSearchPath`) — 이번에 "비워진다" 까지만 봄.
- (문서) 위 "고칠 문서" 3건.

## 판정

**분석성공.** 다섯 질문 모두 `.asm` 줄 근거로 답했다. 1번은 test.exe 에서 안 타는 쪽으로 정적 결론이 났고 `[VM]` 한 줄 (`kernel32+0xc1d10`) 만 남는다. 2번에서 run39 의 한 문장 (비트 60~61 = 1 이면 목록이 하나로) 을 정정했다. 못 정한 것: `P+0x28 ~ +0x77` 의 읽는 곳 (이 범위 안에 없음, 그 밖은 `[공백]`), `CurrentDirectory` 가 읽히는 갈래.

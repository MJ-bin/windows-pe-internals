# 83 gdi32-init — 정적 분석 결과 (Q150b, "최소 길 지도")

- 요청서: `_workspace/sast/83_gdi32-init.md` (짝: `_workspace/sast/82_user32-init_result.md` — 끝의 "Q150b 로 넘길 것"을 따름)
- 모듈 / 바이너리 / PDB (guid+age):
  - gdi32 (`ms/bin/.../System32/gdi32.dll`, 190KB) = `92C6EC2ABE478836C99BC83C2F3AE3551`
  - gdi32full (`.../gdi32full.dll`, 1.2MB) = `A6245E4F5C78EB06438EA4820F1F6EB41`
  - win32u = `6295787D0B7E537E98F77F31F4426D1C1` (sast/82 값, 읽기만)
- 새로 analyze: gdi32 (`ms/ghidra/gdi32.gpr`, `_index.tsv` 1319 함수), gdi32full (`ms/ghidra/gdi32full.gpr`, 2649 함수). 둘 다 `SetPdb:` 줄 있음, `SCRIPT ERROR` 0개 (stdout 에 `ClassNotFoundException: WindowsResourceReference` 한 줄은 Ghidra 가 자기 예제 스크립트를 훑다 낸 것, 우리 스크립트와 무관). 각각 1분 안쪽.
- 뽑은 함수 (모두 `-readOnly`):
  - gdi32 depth 2: `Gdi32DllInitialize`(진입점)·`GdiDllInitialize`·`GdiProcessSetup` (둘 다 6바이트 `JMP` 껍데기)·`LpkpInitializeEditControl`·`__security_init_cookie` → `ms/decomp/gdi32/`, `ms/disassemble/gdi32/`
  - gdi32full depth 1: `entry`(진입점)·`DllMain`·`_DllMainCRTStartup`·`dllmain_dispatch`·`dllmain_crt_*`·`GdiDllInitialize`·`GdiProcessSetup`·`UspProcessAttach`·`GdiInitializeLanguagePack`·`GdiSetServerAttr`(=`GdiDllInitializeWrapper`)·WIL/CRT 정적 초기화 12개 + 그 callee → `ms/decomp/gdi32full/`, `ms/disassemble/gdi32full/` (약 75개)
  - user32 depth 0 (읽기 전용): `GetAppCompatFlags2`·`GetThreadDpiAwarenessContext`·`InitializeLpkHooks`·`GetDpiForSystem`. win32u depth 0: `NtGdiInit2`·`NtGdiInit`·`NtUserGetThreadState` (번호는 sast/82 의 `ms/disassemble/win32u/_syscalls.tsv` 그대로, 스텁 `.asm` 의 `MOV EAX,0x12ee` 와 일치).
- 지도: `ms/disassemble/gdi32full/_map/GdiDllInitialize_{edges,nodes}.tsv` (90개), `ms/disassemble/gdi32/_map/Gdi32DllInitialize_*`. `refs`: gdi32full `0xc1fe0`·`0xc1fe8`·`0xc1ff0`(NtGdiInit/NtGdiInit2/NtGdiInitSpool 가져오기 칸)·`0x888e0`·`0x888f0`; gdi32 `0xa6e0`·`0xc400`·`0x280e0`·`0x27b80`; user32 `0xb4fe0`·`0xb4fd8`.
- 다른 모듈 읽기: **kernelbase·ntdll 프로젝트는 안 열었다** (sast/84·86 이 쓰는 중). 파일(`.dll`) 자체를 `objdump` 로 읽고 이름은 `_index.tsv` 로 붙였다 — 아래에서 `[objdump]` 로 표시. win32kbase 도 `.sys` 파일 + 공개 PDB 이름만 (`[objdump + PDB 공개 이름]`). **Ghidra 프로젝트는 만들지 않았다.**
- 버전 확인 (이번 모듈 대조, 처음 분석한 모듈이라 알려진 사실 표에 gdi32 줄은 없음):
  - PE 헤더 `AddressOfEntryPoint`: gdi32 `0x5ed0` = Ghidra `Gdi32DllInitialize` 시작, gdi32full `0x89720` = `entry` 시작 (일치). win32u 는 `AddressOfEntryPoint = 0` (진입점 없음).
  - `.asm` 의 `CALL [0x1800c1fe8]` = PE 가져오기 표 `win32u!NtGdiInit2` 칸 `0xc1fe8` (일치). gdi32 `JMP [0x1800280e0]` = 지연 가져오기 표 `GdiDllInitialize` 칸 `0x280e0` (일치).
  - sast/82 와 이어지는 자리: user32 `_UserClientDllInitialize +0x996/+0x99d` → `GdiDllInitialize` 가져오기 칸 `0xb4fe0` (`refs` 가 이 한 곳만 읽음, 일치). `ClientThreadSetup +0x93/+0x9a` → `GdiProcessSetup` 칸 `0xb4fd8` (한 곳만).
  - 알려진 사실 표 (ntdll): `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess` 일치 (`ms/disassemble/ntdll/_calls.tsv`, 파일만 읽음). 커널 PDB GUID 는 이번에 안 건드림.
- 스크립트 문제: 없음. (임시 확인용 `pefile`/`objdump` 파이썬은 scratchpad 에만 두고 레포에 안 넣음.)
- 한계 (PEB `+0xf8` 쓰기 찾기): user-mode DLL(gdi32·gdi32full·user32·ntdll·kernelbase)은 `objdump` 줄에서 `gs:0x60` 뒤 8줄 안에 `[reg+0xf8]` 가 나오는지 훑는 방식이라, PEB 포인터를 다른 함수에서 받아 쓰는 모양은 못 잡는다. 그래도 `baggage.md` 의 PEB "끝까지 0" 목록(`GdiSharedHandleTable`)과 어긋나지 않는다. win32kbase 는 `PsGetProcessPeb` 호출 뒤 150줄 안에서 `QWORD PTR [reg+0xf8],` 쓰기를 찾았고 (9곳 중 `0x1400e20da` 호출 뒤 `0x1400e223b` 하나가 PEB 쓰기 모양), 함수 이름은 "가장 가까운 앞쪽 공개 PDB 이름"(`GdiProcessCallout`) 이라 함수 크기는 안 확인했다.
- 한계: **gdi32·gdi32full PDB 도 공개 심볼만이라 구조체 타입이 0개** (`llvm-pdbutil dump -types` → 0 records). GDI 공유 메모리 칸 이름은 PDB 로 못 붙임 → "코드가 읽는 오프셋"은 `[정적]`, 이름은 `[추측]`.

## 먼저 읽을 것 (핵심 여섯 줄)

1. **gdi32 의 `DllMain`(= `Gdi32DllInitialize`, rva `0x5ed0`) 은 `GdiDllInitialize` 를 안 부른다.** 하는 일: `DisableThreadLibraryCalls`, `__security_init_cookie`, `GetProcessMitigationPolicy(…,0xb)`, `GetModuleHandleA("gdi32full.dll")`(없으면 `"gdi32min.dll"`) → `GetProcAddress("GdiDllInitializeWrapper")` → 그 주소를 CFG 간접 호출 → `TRUE`. 그 `GdiDllInitializeWrapper` 는 gdi32full 안 **`RET 0` 한 줄** (rva `0x71030`, `GdiSetServerAttr` 와 같은 주소). → **gdi32 `DllMain` 에는 win32u syscall 이 0개, `GdiSharedHandleTable` 도 안 읽는다.** sast/82 가 "같은 함수가 gdi32 `DllMain` 과 user32 `DllMain` 에서 두 번 불린다" 고 짐작한 건 **틀림** — `GdiDllInitialize` 를 부르는 곳은 user32 `+0x996` 하나뿐 (`refs`).
2. **gdi32full 의 진입점(`entry`, rva `0x89720`) 은 `reason == 1` (PROCESS_ATTACH) 에서 아무것도 안 하고 `TRUE` 만 돌려준다** (`+0x2a JNZ` 로 `_DllMainCRTStartup` 호출을 건너뜀; 0·2·3 일 때만 호출). **진짜 초기화는 user32 가 `GdiDllInitialize` 를 부를 때 시작**한다: gdi32full `GdiDllInitialize` (rva `0x82070`) → `InitializeGdiHeap` → `_DllMainCRTStartup(hmodUser32, 1, 0)` → CRT 초기화 → **gdi32full `DllMain` (rva `0x89600`)**.
3. gdi32full `DllMain` 의 syscall 은 **`NtGdiInit2` (win32u `0x12ee`) 하나뿐** (`+0x2e`). `NtGdiInit`(`0x12ed`) 은 gdi32·gdi32full 어디서도 안 부른다 (`refs`: 가져오기 칸 `0xc1fe0` 은 내보내기 껍데기만 읽음). 이게 **이 스레드의 첫 win32k syscall** 이라 (user32 `DllMain` 의 win32k syscall 은 sast/82 에서 전부 조건부) 커널이 스레드를 GUI 스레드로 바꾸고 `__ClientThreadSetup` 콜백이 돌 자리이다 `[추측: 타이밍]`.
4. **돌려준 값이 0 이면 `DllMain` 이 FALSE** (`+0x4a JZ`): `NtGdiInit2` 반환 RAX 가 `gdi32!gCookie` 로 저장되는데 0 이면 안 됨. 이어서 `GdiProcessSetup` 이 **PEB `+0xf8` (`GdiSharedHandleTable`) 가 가리키는 메모리를 읽으니** (`+0x161` 에서 `[PEB+0xf8]+0x180094`), 그 칸은 `NtGdiInit2` 가 끝날 때까지 채워져 있어야 한다. 그걸 쓰는 코드는 gdi32·gdi32full·user32·ntdll·kernelbase 어디에도 없고, **`win32kbase!GdiProcessCallout +0x22b` 가 쓴다** `[objdump + PDB 공개 이름]`. 우리 엔진(PE_emulator)이 흉내 내야 할 큰 몫.
5. 그 뷰(shared memory)에서 `DllMain` 줄기가 읽는 곳은 셋: `+0x180094` (dword, 0 이면 언어팩 건너뜀), `+0x180140`·`+0x180148` (각 8B, `GetStockObject(0x12 DC_BRUSH / 0x13 DC_PEN)` 이 읽는 스톡 핸들, **둘 다 0 이 아니어야** `+0x9e`/`+0xa3` 에서 FALSE 가 안 남). 즉 **최소 `0x180150` 바이트 읽기 가능 + 그 두 칸에 0 아닌 값**.
6. `GdiDllInitialize` 가 돌려주는 값은 `0` (성공·이미 함·reason≠1), `0xc0000017` (힙 못 만듦), `0xc0000001` (`DllMain` FALSE). user32 는 **음수만 FALSE** 로 보므로 뒤 둘이 `0xc0000142` 로 이어진다. `GdiProcessSetup` 은 user32 `ClientThreadSetup +0x93` 에서도 불려서 **두 번 불리는 건 이 함수** 다 (`gbFirst` 로 두 번째는 큰 부분을 건너뜀).

## 질문별 답 [정적]

### 질문 1. gdi32 `DllMain` (PROCESS_ATTACH)

**진입 줄기** (`ms/disassemble/gdi32/Gdi32DllInitialize.asm`; Ghidra 이름은 PDB 대로 `Gdi32DllInitialize`, `^DllMain$` 는 gdi32 에 없음):

| 자리 call/ret | 하는 일 | syscall | 실패면 |
|---|---|---|---|
| `+0x4 CMP EDX,1` / `+0x7 JZ → +0x14` | reason ≠ 1 이면 바로 `TRUE`(`+0x9 MOV EAX,1`, 반환 `+0xe`) | — | — |
| `+0x14/+0x1b` | `DisableThreadLibraryCalls(hmod)` → 이 DLL 은 스레드 attach/detach 를 안 받음 | 없음 (kernelbase → ntdll 로더 칸) | |
| `+0x20/+0x25` | `__security_init_cookie` | `QueryPerformanceCounter` 안에서 `NtQueryPerformanceCounter 0x31` 가능 (KUSER_SHARED_DATA 의 `QpcBypassEnabled` 가 꺼졌을 때만; 모든 MSVC DLL 공통) `[추측]` | |
| `+0x2d/+0x34`, `+0x4b/+0x52` | `GetCurrentProcess`, `GetProcessMitigationPolicy(self, 0xb, &v, 4)` (`+0x39 R9D=4`, `+0x47 LEA EDX,[R9+7]`=0xb) | `[ntdll 0x19] NtQueryInformationProcess` 클래스 `0x34` (ProcessMitigationPolicy), 입력 `{정책 0xb}` 길이 8 — kernelbase `GetProcessMitigationPolicy +0x47/+0x4e` (rva `0x216f0`) `[objdump]` | 실패(`EAX==0`)면 `+0x59 JZ → +0x68` 로 건너뜀. 성공이면 `g_systemCallFilterId` ← `v & 0xf` (gdi32 rva `0x25780`, 처음 값 0). **FALSE 아님** |
| `+0x6f/+0x76` | `GetModuleHandleA("gdi32full.dll")` | 없음 (로더 목록 조회) | 0 이면 `+0x7e JZ → +0xab` |
| `+0xb2/+0xb9` | (위가 0 일 때만) `GetModuleHandleA("gdi32min.dll")` | 없음 | 0 이면 `+0xc1 JNZ` 안 타고 `+0xc3 XOR EAX,EAX` → **FALSE** (`+0xc5 JMP → +0xe`) |
| `+0x8a/+0x91` | `GetProcAddress(hmod, "GdiDllInitializeWrapper")` | 없음 (CFG 내보내기 억제 갈래에서 `NtSetInformationVirtualMemory 0x1b0` 가능 — 이번에 안 봄) `[추측]` | 0 이면 `+0x99 JZ → +0xc3` → **FALSE** |
| `+0x9b/+0xa0` | CFG 간접 호출 (`CALL 0x18000f010` = `__guard_dispatch_icall`, 대상 RAX = 위 주소) → gdi32full `0x71030` = **`RET 0`** | 없음 | |
| `+0xa0 MOV EAX,1` | `TRUE` | | |

- **`NtGdi*` syscall: 없음.** gdi32 `DllMain` 줄기는 win32u 를 한 번도 안 부른다 (`ms/disassemble/gdi32/_calls.tsv`, 지도 `Gdi32DllInitialize_edges.tsv`). gdi32 가 가져오는 win32u 함수 118개 (`NtGdiInitSpool` 포함) 는 가져오기 표에만 있고 `DllMain` 이 안 부른다.
- **`GdiSharedHandleTable` (PEB `+0xf8`) 은 gdi32 `DllMain` 에서 안 읽는다.** gdi32 변수 칸만 쓰이는데, 그 칸을 **쓰는 쪽은 gdi32full** (아래 질문 2).
- **FALSE 자리 (gdi32 `DllMain`, 둘뿐)**: `+0x99 JZ → +0xc3` (`GdiDllInitializeWrapper` 못 찾음), `+0xc1 → +0xc3` (gdi32full 도 gdi32min 도 아직 안 올라옴). → **gdi32full 이 `GdiDllInitialize` 이전에 이미 메모리에 올라와 있어야** 한다. gdi32 가 가져오기 표로 gdi32full 에 붙어 있으니 (아래) 로더가 먼저 올림.
- **gdi32 와 gdi32full 은 서로를 정적으로 가져온다 (순환)** `[정적]`:
  - gdi32 → gdi32full: 정적 가져오기 `api-ms-win-gdi-internal-uap-l1-1-0.dll` (17개 함수; API 세트 스키마 `ms/bin/.../apisetschema.dll` 의 `.apiset` 를 직접 읽어 `→ gdi32full.dll` 로 풀림). 그리고 지연 가져오기 `ext-ms-win-gdi-*` 여러 개 (대부분 → `gdi32full.dll`; `ext-ms-win-gdi-edgegdi-l1-1-0` 은 호스트 비어 있음).
  - gdi32full → gdi32: 정적 가져오기 `GDI32.dll` 68개 (**변수 칸 포함**: `gCookie`·`gMaxGdiHandleCount`·`pGdiSharedMemory`·`pGdiSharedHandleTable`·`pGdiDevCaps`·`GdiBatchLimit`·`gW32PID`·`ghICM`·`fpClosePrinter`, 그리고 `LpkpInitializeEditControl`·`GdiSetLastError`), `USER32.dll` 36개 (`GetAppCompatFlags2`·`GetThreadDpiAwarenessContext`·`InitializeLpkHooks`·서수 2713 …), `win32u.dll` 345개, `ntdll.dll`, `msvcp_win.dll`, `api-ms-win-crt-*` (UCRT).
  - 그래서 **user32 → gdi32 → gdi32full → user32** 순환이다. `DllMain` 호출 차례는 로더가 정한다 → 동적 확인 (아래). gdi32full 진입점이 `reason 1` 에서 아무것도 안 하는 설계는 이 순환 때문에 (그리고 user32 가 PEB `+0x58` 콜백 표를 `+0x3fc` 에서 `+0x996` 보다 **먼저** 깔아야 첫 win32k syscall 콜백이 돌 수 있어서) 이렇게 짠 것으로 읽힌다 `[추측]`.
- **win32u 에는 `DllMain` 이 없다** (`AddressOfEntryPoint = 0`): 올라오기만 하면 끝.

**지금 우리 (PE_emulator) 맞나·틀리나**
- 정보 없음: PE_emulator 는 user32 `DllMain` 에서 막혀 gdi32 까지 못 갔다. 알아둘 점: gdi32 `DllMain` 은 **엔진이 줄 syscall 답이 `NtQueryInformationProcess(0x34)` 하나뿐** (실패해도 계속)이고, 진짜 일은 gdi32full 로 간다.
- sast/82 의 "gdi32 `GdiDllInitialize` 가 `DllMain` 에서 불리고 user32 에서도 불린다" 는 짐작은 **틀림** (위 핵심 1).

### 질문 2. gdi32full `DllMain`

**진입 줄기**

- `entry` (rva `0x89720`, 81B, `ms/disassemble/gdi32full/entry.asm`): reason 0·2·3 이면 `+0x2c/+0x31` `_DllMainCRTStartup` (rva `0x86620`), 0 이면 이어서 `+0x3a/+0x3f` `UninitializeGdiHeap`. **reason 1 은 `+0x2a JNZ → +0x3f` 로 건너뛰고 `EBX = 1` 을 돌려준다.** (`+0x21 JZ`·`+0x26 JZ` 둘은 목적지 `+0x2c`.)
- gdi32full 은 `DisableThreadLibraryCalls` 를 안 불러서 (gdi32 가 부른 건 gdi32 자기 `hmod`) 스레드 attach(2)/detach(3) 는 `_DllMainCRTStartup → dllmain_dispatch → dllmain_crt_dispatch (CRT 스레드 훅) → DllMain(reason 2/3 → +0x1e JNZ → +0x100 TRUE)` 로 온다. (새 스레드는 이 바퀴 밖.)
- **진짜 PROCESS_ATTACH 길**: user32 `_UserClientDllInitialize +0x996/+0x99d` → gdi32 `GdiDllInitialize` (rva `0xa6e0`, `JMP [0x1800280e0]`) → **지연 가져오기** (`ext-ms-win-gdi-internal-uap-init-l1-1-0.dll` → gdi32full) → gdi32full `GdiDllInitialize` (rva `0x82070`, `GdiDllInitializeWrapper` 와는 **다른** 함수):

| 자리 call/ret (gdi32full `GdiDllInitialize`) | 하는 일 | 실패/건너뜀 |
|---|---|---|
| `+0xa ~ +0x16` | `*gMaxGdiHandleCount = 0x1000000` (gdi32 변수, **매번** 씀) | |
| `+0x1c CMP EDX,1`, `+0x1f JNZ → +0x61` | reason ≠ 1 이면 `EBX = 0` 으로 바로 반환 | |
| `+0x21 CMP [GdiDllFullyInitialized],DL`, `+0x27 JZ → +0x61` | **이미 한 번 성공했으면 (플래그 1) 반환 0** — 두 번째 호출은 여기서 끝 | |
| `+0x29/+0x2e` | `InitializeGdiHeap` (rva `0x93828`): `GdiHeap == 0` 일 때 `HeapCreate(0,0,0)` (`+0x18/+0x1f`) | 실패(`TEST EAX`) → `+0x32 EBX = 0xc0000017` (`STATUS_NO_MEMORY`) → **user32 FALSE** |
| `+0x43/+0x48` | `_DllMainCRTStartup(RDI = hmod 인자 = user32 의 hmod, 1, 0)` | 반환 0 이면 `+0x4a JNZ` 안 타고 `+0x4e/+0x53` `UninitializeGdiHeap(0)`, `+0x53 EBX = 0xc0000001` → **user32 FALSE** |
| `+0x5a` | `GdiDllFullyInitialized = 1` | |
| `+0x61 MOV EAX,EBX` | 반환 (0 / 위 둘) | |

- `_DllMainCRTStartup` (`+0x1c/+0x21` `__security_init_cookie` 는 reason 1 일 때만, `+0x38` 꼬리 점프 → `dllmain_dispatch`, rva `0x864e8`) → reason 1: `_pDefaultRawDllMain` (널이면 건너뜀) → `+0x6e/+0x73` `dllmain_crt_dispatch` (rva `0x862f0`, reason 1 → `+0x3c` 꼬리 → `dllmain_crt_process_attach`, rva `0x86348`) → `+0x89/+0x8e` **`DllMain` (rva `0x89600`)**. `DllMain` 이 0 이면 `+0xa5/+0xaa` `DllMain(reason 0)` 와 `+0xb2/+0xb7` CRT 해제 (정리) 후 0 반환.
- `dllmain_crt_process_attach` (CRT): `+0x1d` `__scrt_initialize_crt`, `+0x4f` `__scrt_dllmain_before_initialize_c`, `+0x58 _RTC_Initialize`, `+0x75/+0x7a` `_initterm_e(__xi_a..__xi_z)` (2개: `initialize_legacy_wide_specifiers`·`initialize_msvcrt_compatibility`, 둘 다 `__local_stdio_*_options` 만), `+0x7e/+0x83` `__scrt_dllmain_after_initialize_c` (ucrtbase 의 `_configure_narrow_argv`·`_initialize_narrow_environment` 호출), `+0x95/+0x9a` `_initterm(__xc_a..__xc_z)` (12개: WIL·기능 스테이징 정적 초기화 — `ms/disassemble/gdi32full/_dynamic_initializer_for_*.asm` 의 `_calls.tsv` 가 `atexit`·객체 만들기뿐, **syscall·win32k 없음**). 이 CRT 쪽 실패는 ucrtbase 호출이 실패할 때뿐 (`+0x24 JZ`, `+0x7c JNZ`, `+0x85 JZ` → 반환 0).

**`DllMain` (reason = 1) 줄기** (`ms/disassemble/gdi32full/DllMain.asm`)

| 자리 call/ret | 부르는 것 | syscall (번호) | 읽고 쓰는 것 | 실패면 |
|---|---|---|---|---|
| `+0x24/+0x29` | `AssertProcessIsNotFontdrvhost` (rva `0x68700`) → `+0x56` `K32GetModuleBaseNameW(self, NULL, buf, 0x11)` | `[ntdll 0x19] NtQueryInformationProcess` (클래스 0 = 기본 정보, 길이 `0x30`) 하나 + **`[ntdll 0x3f] NtReadVirtualMemory` 다섯 번** (자기 프로세스 `-1`; PEB `+0x10` ImageBaseAddress → PEB `+0x18` Ldr → `Ldr+0x20` 메모리 순서 목록 머리 → 목록 칸마다 `0x88`B 항목 (`DllBase` 와 비교, test.exe 면 한 칸) → `BaseDllName` 글자 `0x22`B). kernelbase `K32GetModuleBaseNameW` (rva `0xb6900`) 안 호출 자리 `+0x7d`, `+0x241`, `+0xc2`, `+0x108`, `+0x159`, `+0x1ce` (ret = call + 7) `[objdump]` | 이름이 `fontdrvhost.exe`(`0x1800cb49a`) 와 `wcsnicmp` 로 같을 때만 뒤 갈래 (`MicrosoftTelemetryAssertTriggeredNoArgs`, 우리 exe 는 아님) | **실패해도 계속** (`+0x64 JZ`, 반환 안 봄) |
| `+0x29/+0x2e` | `ReadDisableMetaFilesRegKey` (rva `0x5de34`) | `[ntdll 0x12] NtOpenKey` (`+0xfe/+0x105`, 접근 `0x20019`, 속성 `0x40`, 키 `\Registry\Machine\Software\Microsoft\Windows NT\CurrentVersion\GRE_Initialize`) → `[ntdll 0x17] NtQueryValueKey` (`+0x14c/+0x153`, 값 `DisableMetaFiles`, 클래스 2 = 부분 정보, 길이 `0x14`) → `[ntdll 0xf] NtClose` (`+0x161/+0x168`) | `gbDisableMetaFiles` ← 값 (REG_DWORD 4B 일 때만, `+0x192`) | 키·값 없어도 됨 (`+0x10c JS`), **FALSE 아님** |
| **`+0x2e/+0x35`** | **`CALL [NtGdiInit2]`** (가져오기 칸 `0xc1fe8`, 인자 없음) | **`[win32u 0x12ee] NtGdiInit2`** — **이 스레드의 첫 win32k syscall** | 반환 `RAX` → `+0x44 MOV [gCookie],RCX` (`gCookie` = gdi32 변수 rva `0x257a0`) | `+0x4a JZ → +0xaa` 반환 0 이면 **FALSE** |
| `+0x56/+0x5d` | `LpkpInitializeEditControl(&LpkEditControlInternal, 0xe)` (gdi32 rva `0x6140`) | 없음 | 14칸(0x70B) 을 gdi32 `LpkEditControl` 로 복사, `EDX == 0xe` 일 때만 `1` | `+0x62 CMP EAX,1`, `+0x65 JNZ → +0xaa` **FALSE** (실제론 항상 1) |
| `+0x67/+0x6c` | `GdiProcessSetup` (rva `0x81a00`) — 아래 | `GetAppCompatFlags2` 안 `[win32u 0x1000] NtUserGetThreadState(0xe)` 조건부 | PEB `+0xf8` 읽기 | `+0x6e JS → +0xaa` **FALSE** |
| `+0x70/+0x75` | `InitializeCFontCache` (rva `0x3f304`): `operator new` 두 번 (0x40·0x30B) + 해시 표 | 없음 (힙) | `gpCFontMap` | 항상 `1` 반환 (메모리 못 얻으면 예외) |
| `+0x7c/+0x81`, `+0x8b/+0x90` | `GetStockObject(0x12)`·`GetStockObject(0x13)` (rva `0x25e90`) | 없음 (`ECX=0x12/0x13` 은 DPI 갈래 안 타고 `+0x65` 기본 갈래: **`*pGdiSharedMemory + 0x1800b0 + ECX*8`** 읽기) | `ghbrDCBrush` ← `[+0x180140]`, `ghbrDCPen` ← `[+0x180148]` | `+0x9e JZ → +0xaa` (DCBrush 0), `+0xa0/+0xa3 TEST/SETNZ` (DCPen 0 이면 반환 0) → **FALSE** |
| `+0xa6` | `TRUE` | | |

- **FALSE 자리 전체 (gdi32full `DllMain`, reason 1)**: `+0x4a`(`NtGdiInit2`=0), `+0x65`, `+0x6e`(`GdiProcessSetup` 음수), `+0x77`(`InitializeCFontCache`=0, 실제론 안 남), `+0x9e`/`+0xa3`(스톡 DC 브러시·펜 핸들 0) — 목적지는 모두 `+0xaa XOR EAX,EAX` (`0x1800896aa − 0x180089600`), 반환은 `+0xac JMP → +0x105`.
- **`GdiProcessSetup`** (rva `0x81a00`, 1172B, `ms/disassemble/gdi32full/GdiProcessSetup.asm`) — `gbFirst` (rva `0x108474`, **파일 안 처음 값 `1`**) 로 두 갈래:
  - `gbFirst != 0` (첫 호출):
    - `+0x28/+0x2f`·`+0x4a/+0x51`·`+0x6c/+0x73`·`+0x8e/+0x95`·`+0xb0/+0xb7`·`+0xd2/+0xd9` 임계구역 6개 `RtlInitializeCriticalSection` (`semGlLoad`·`semLocal`·`semListIcmInfo`·`semColorTransformCache`·`semColorSpaceCache`·`semUMPD`; 음수면 `JS → +0x489` 반환),
    - `+0xf7/+0xfe` `GetACP` (→ `guintAcp`, 동아시아 코드페이지 `0x3a4`·`0x3b5/0x3b6`·`0x3a8` 이면 `+0x12d` `vSetCheckDBCSTrailByte`),
    - `+0x142 ~ +0x14f` `gW32PID` ← `TEB+0x40`(ClientId.UniqueProcess) `& ~3`,
    - `+0x151 ~ +0x185` **PEB(`GS:[0x60]`)`+0xf8` 읽고 `+0x161 CMP [그곳+0x180094],0`; 0 이 아니면 `+0x180` `GdiInitializeLanguagePack(그 값)`** (rva `0x52550`: `LpkGetEditControl`·`LpkInitialize`(`InitializeCriticalSectionAndSpinCount`·`LpkCheckForMirrorSignature`)·user32 `InitializeLpkHooks` — user32 의 `ClientThreadSetup` 이 `NtUserRegisterLPK` 를 부를지 정하는 쪽, sast/82 후보 5),
    - `+0x18e` `gbFirst = 0`, `+0x199 ~ +0x1d1` 목록 머리 셋을 제 자신으로 (ICM·색 공간·색 변환),
    - `+0x1d8 ~ +0x3d3` `RtlEncodePointer(0)` ×25 → `fpStartDocDlgW`·`fpOpenPrinterW`·… 인쇄 훅 칸에 (ntdll 쿠키로 인코딩된 널),
    - `+0x3e6/+0x3eb` `UspProcessAttach` (rva `0x6a1c4`: `TlsAlloc` + `InitializeCriticalSection` ×2 + `memset`; 반환 1),
    - `+0x3fa/+0x401` `GetModuleHandleW(L"gdi32.dll")`, `+0x415/+0x41c` `GetProcAddress("DeleteEnhMetaFile")` → `pfnDeleteEnhMetaFile`.
  - **두 갈래 공통 끝 (`gbFirst == 0` 이면 `+0x1b JZ → +0x428` 로 바로 여기)**: `+0x428 ~ +0x45d` PEB `+0xf8` 를 읽어 **`pGdiSharedMemory`·`pGdiSharedHandleTable` ← 그 값, `pGdiDevCaps` ← 그 값 `+ 0x180000`** (모두 gdi32 변수), `+0x460/+0x46b` `GetAppCompatFlags2(0x400)` → `GdiBatchLimit` ← 호환 플래그 `0x200` 꺼졌으면 `0x14`, 켜졌으면 0 (`+0x470 ~ +0x485`).
  - 반환 `EBX` = 마지막 `RtlInitializeCriticalSection` 결과 (첫 호출) / 0 (두 번째 호출).
- **user32 `GetAppCompatFlags2`** (rva `0x6100`, 읽기만): `TEB+0x78` (`Win32ThreadInfo`) 이 0 이면 `+0x45/+0x4c` **`NtUserGetThreadState(0xe)` (`[win32u 0x1000]`)** 를 부르고 반환 0 이면 그냥 0; 아니면 TEB `+0x810`·`+0x818` 칸을 읽는다 (CLIENTINFO 칸 이름은 `[추측]`). → `NtGdiInit2` 가 TEB `+0x78` 을 채웠으면 syscall 없음, 안 채웠으면 `0x1000` 이 한 번 더.

**gdi32 가 gdi32full 을 부르나 / 따로 import 되나**: 둘 다 (위 순환 설명). 부르는 방법 셋: ① 정적 가져오기 `api-ms-win-gdi-internal-uap-l1-1-0` (함수 17개 → 로드 시 gdi32full 이 같이 올라옴), ② `DllMain` 의 `GetModuleHandleA("gdi32full.dll")` + `GetProcAddress("GdiDllInitializeWrapper")` (`RET 0`), ③ **지연 가져오기** `GdiDllInitialize` (슬롯 `0x280e0`, `ext-ms-win-gdi-internal-uap-init-l1-1-0`) · `GdiProcessSetup` (슬롯 `0x27b80`, `ext-ms-win-gdi-internal-desktop-l1-1-0`). 둘 다 스키마에서 `gdi32full.dll` 로 풀림.

**지연 가져오기를 푸는 길** (`LdrResolveDelayLoadedAPI`, ntdll rva `0xa47d0`, **`[objdump]`** — ntdll 프로젝트는 sast/86 이 쓰는 중이라 안 열었음):
- gdi32·gdi32full·user32·win32u 모두 PE `GuardFlags` 에 `0x1000` (`PROTECT_DELAYLOAD_IAT`) 이 있다. run63 의 `LdrpCfgProcessLoadConfig +0xc5` (`Flags |= 0x8000`) 와 이어지면, `LdrResolveDelayLoadedAPI +0x17f test [항목+0x68],0x8000` → `+0x188` **`LdrpHandleProtectedDelayload`** (rva `0xa6100`) 로 간다 (아니면 `+0x192` `LdrpHandleUnprotectedDelayLoad`).
- `LdrpHandleProtectedDelayload` 안: `+0x13e LdrpLoadForwardedDll` (API 세트 이름 → gdi32full, 이미 올라와 있으니 찾기만), `+0x2f7 LdrpResolveProcedureAddress`, `+0x3f0`·`+0x550` **`LdrpWriteBackProtectedDelayLoad`** (rva `0xa45b0`) → 그 안 `+0x89`·`+0xf1` **`NtProtectVirtualMemory [ntdll 0x50]` 두 번** (`.didat` 쪽 쓰기 열기·닫기 `[추측]`).
- 즉 **`GdiDllInitialize` 한 번 풀 때마다 (그리고 `GdiProcessSetup` 도) `NtProtectVirtualMemory` 두 번**이 syscall 목록에 더해진다. 동적으로 확인 (아래).

**지금 우리 (PE_emulator) 맞나·틀리나**
- 정보 없음 (우리는 user32 `DllMain` 에서 멈춤). 알아둘 점 셋: ① 엔진이 `NtGdiInit2` 를 거절하거나 0 을 돌려주면 `0xc0000142` 로 끝난다. ② 0 이 아닌 값을 돌려줘도 **PEB `+0xf8` 이 0 이면 `GdiProcessSetup +0x161` 에서 접근 위반(주소 `0x180094`)**. ③ `gdi32full!DllMain` 은 reason 1 에서 로더가 부르는 게 아니다 — user32 `DllMain` 이 `GdiDllInitialize` 를 부를 때만 돈다.

### 질문 3. "창을 안 만드는 프로세스가 진입점까지 가는 최소 조건" 표 둘

전제: 창 없는 보통 프로세스 (`gfServerProcess == 0`), user32 `psi` 는 0 으로 채운 읽기 가능 메모리 (sast/82), 매니페스트·IFEO 없음, 로더 차례는 user32 `DllMain` 이 gdi32 → gdi32full 보다 **마지막에 `GdiDllInitialize` 를 부르는** 것으로 읽는다. **표 A·B 는 요청 28 의 마지막 답.** user32 줄 근거는 sast/82 표 (`§82:` 로 가리킴), gdi32·gdi32full 줄 근거는 이 결과서.

#### 표 A. syscall 답 표 (차례 순)

번호: `[ntdll]` 은 ntdll `Nt*` 스텁의 `MOV EAX,imm` (스텁 바이트 직접 읽음), `[win32u]` 는 `ms/disassemble/win32u/_syscalls.tsv`.

| 차례 | DLL · 자리 | syscall (번호 · 이름) | nt 엔진이 줄 답 | 이게 없으면 |
|---|---|---|---|---|
| A0 | (로더 차례에 따라 user32 앞/뒤) gdi32 `Gdi32DllInitialize +0x4b/+0x52` → kernelbase `GetProcessMitigationPolicy +0x47/+0x4e` | `[ntdll 0x19]` `NtQueryInformationProcess` 클래스 `0x34`(ProcessMitigationPolicy), 입력 `{0xb}`, 길이 8 | 성공 + 플래그 dword = 0 (실패도 됨) | 실패도 통과 (`g_systemCallFilterId` 0 유지). 가짜 답으로 5/7 이 오면 user32 의 `g_systemCallFilterId` 갈래가 바뀜 (§82 표 12·13) |
| A1 | user32 `+0x1d1/+0x1d8` (§82 표 3) | `[ntdll 0x36]` `NtQuerySystemInformation(0 SystemBasicInformation, 0x40B)` | 성공 + 버퍼 `+0x28` = `MaximumUserModeAddress` | `+0x1df JS` → user32 FALSE |
| A2 | user32 `+0x1f3` `RtlInitializeNtUserPfn` 안 `LdrProtectMrdata` (§82 표 4) | `[ntdll 0x50]` `NtProtectVirtualMemory` ×2 (`.mrdata` 열기·닫기) | 성공 | `InitializeNtdllUserPfn` 0 → FALSE |
| A3 | user32 `+0x2aa/+0x2b1` `CsrClientConnectToServer` → `CsrpClientConnectToServer` → `CsrClientCallServer +0xed/+0xf2` (§82 표 7·(a)) | `[ntdll 0x8e]` `NtAlpcSendWaitReceivePort` (서버 3 USERSRV 연결, 응답 포트는 BASESRV 때 연 것) | 메시지 `+0x34` Status = 0 + 응답 버퍼(0x248B) `+8` 부터 0x240B `SHAREDINFO`, 그 첫 8B `psi` 는 `0x1b68`B 이상 읽기 가능한 메모리 | 음수 → `+0x2b8 JS` FALSE. `psi` 널 → `+0x38e` 접근 위반. `CsrPortHandle == 0` 이면 `NtOpenSection` 갈래(우리가 본 `0xc000003a`) |
| A4 | user32 `+0x463` `GetProcessMitigationPolicy` (§82 표 12) | `[ntdll 0x19]` 같은 모양 (A0 과 동일) | 성공 + 0 (또는 실패) | 실패도 통과 |
| A5 | user32 `InitInstrument +0x98 ~ +0x156` (§82 표 14), `InitProcessGdiScaling/SystemDpiBehavior` (표 17) | `[ntdll 0x12]` `NtOpenKey` · `[ntdll 0x17]` `NtQueryValueKey` · `[ntdll 0xf]` `NtClose` (IFEO·`Error Message Instrument`·`Policies\...\Display` 키) | 키 없음(`STATUS_OBJECT_NAME_NOT_FOUND`) 도 됨, 있으면 값 | 통과 (키 없는 게 기본) |
| **A6** | **user32 `+0x996/+0x99d` `GdiDllInitialize` → gdi32 껍데기 `JMP [0x1800280e0]` → 지연 가져오기 풀기** (`LdrResolveDelayLoadedAPI +0x188 → LdrpHandleProtectedDelayload → LdrpWriteBackProtectedDelayLoad +0x89/+0xf1`) `[objdump]` | `[ntdll 0x50]` `NtProtectVirtualMemory` ×2 (gdi32 `.didat` 쪽) | 성공 (진짜 보호 바꿈. 슬롯 `0x280e0` 에 gdi32full `GdiDllInitialize` 주소가 쓰임). API 세트 `ext-ms-win-gdi-internal-uap-init-l1-1-0` → `gdi32full.dll` (PEB `ApiSetMap`) | 풀기 실패 → `LdrpRedirectDelayloadFailure` 갈래 `[추측]` |
| A7 | gdi32full `GdiDllInitialize +0x29 → InitializeGdiHeap +0x18/+0x1f` `HeapCreate(0,0,0)` → ntdll `RtlCreateHeap` | `[ntdll 0x18]` `NtAllocateVirtualMemory` (힙 예약·커밋; 정확한 횟수는 이번에 안 풂) | 성공 | 실패 → `GdiDllInitialize` `0xc0000017` → user32 FALSE |
| A8 | gdi32full `_DllMainCRTStartup +0x1c` `__security_init_cookie` | `QueryPerformanceCounter` 가 `[ntdll 0x31]` `NtQueryPerformanceCounter` 를 부를 수 있음 (`QpcBypassEnabled` 꺼진 경우) `[추측]` | 성공 + 카운터 값 | 공통 (다른 DLL 에서도 한 번 겪은 길) |
| A9 | gdi32full `DllMain +0x24/+0x29` → `K32GetModuleBaseNameW` (kernelbase `+0x7d`, `+0x241`, `+0xc2`, `+0x108`, `+0x159`, `+0x1ce`) `[objdump]` | `[ntdll 0x19]` `NtQueryInformationProcess(자기, 클래스 0, 0x30B)` + `[ntdll 0x3f]` `NtReadVirtualMemory(자기 -1)` ×5 | NtQIP: 성공 + `PebBaseAddress` (offset `+0x8`). NtRVM: **자기 주소 공간에서 그대로 읽어 줌** (PEB `+0x10`·`+0x18`, `Ldr+0x20`, `LDR_DATA_TABLE_ENTRY` 0x88B, 이름 글자) | 실패하면 `GetModuleBaseNameW` 가 0 → 통과 (결과 안 봄). 성공했는데 이름이 `fontdrvhost.exe` 면 텔레메트리 갈래 |
| A10 | gdi32full `DllMain +0x29/+0x2e` `ReadDisableMetaFilesRegKey` | `[ntdll 0x12]` `NtOpenKey` (`...\CurrentVersion\GRE_Initialize`, `0x20019`) · `[ntdll 0x17]` `NtQueryValueKey` (`DisableMetaFiles`, 클래스 2, `0x14`B) · `[ntdll 0xf]` `NtClose` | 키 없음 가능, 있으면 REG_DWORD 4B | 통과 (`gbDisableMetaFiles` 0 유지) |
| **A11** | **gdi32full `DllMain +0x2e/+0x35`** | **`[win32u 0x12ee]` `NtGdiInit2`** (인자 없음) | **0 이 아닌 64비트 값** (`gCookie` 에 저장). 부수 효과(진짜 커널이 하는 것): ① 스레드를 GUI 스레드로 바꿈 ② **PEB `+0xf8` 에 GDI 공유 메모리 뷰 연결** (`win32kbase!GdiProcessCallout +0x22b`) ③ (`__ClientThreadSetup` 콜백, A12) | 반환 0 → `+0x4a JZ` **FALSE** → `GdiDllInitialize` `0xc0000001` → user32 FALSE → `0xc0000142`. PEB `+0xf8` 이 0 이면 A13 에서 접근 위반 |
| A12 | (A11 안에서 커널 → 유저 콜백) `user32!__ClientThreadSetup` (`KernelCallbackTable[84]`, PEB `+0x58`) → `ClientThreadSetup +0x93 GdiProcessSetup`, `+0x272 NtUserRemoteConnectState`, `+0x29c NtUserGetThreadDesktop`, … (§82 질문 3) | `[win32u 0x151f]`·`[win32u 0x107b]`·`[win32u 0x1475]`·(`0x150a` 조건부) | §82 질문 3 대로 | **엔진이 이 콜백을 안 부르면** `ClientThreadSetup` 이 안 돌아 `pdiLocal`·TEB `+0x820` 등이 안 채워짐. 진입점 도달엔 필수 아님 `[추측]` (A13 의 `GdiProcessSetup` 이 `DllMain +0x67` 에서 대신 첫 호출이 됨) |
| A13 | gdi32full `DllMain +0x67/+0x6c` `GdiProcessSetup` → `+0x464/+0x46b` `GetAppCompatFlags2(0x400)` (user32, `+0x45/+0x4c`) | `[win32u 0x1000]` `NtUserGetThreadState(0xe)` — **TEB `+0x78` 이 0 일 때만** | 아무 값 (0 이면 `GetAppCompatFlags2` 가 0 반환) | 통과 |
| A14 | gdi32full `GetStockObject` (DC_BRUSH·DC_PEN) | **syscall 없음** (공유 메모리 읽기) | — | — |
| A15 | user32 `+0x9b8` 이후 `InitProcessDpiAwareness` 등 (§82 표 19·20, (c)) | 조건부 win32k 다섯 (`0x1576`·`0x1440`·`0x13ec`·`0x1579`·`0x157b`) | `psi` 0 채움·매니페스트 없음이면 **안 부름** | — |

- 위 줄 중 **`DllMain` 이 FALSE 로 끝나게 하는 syscall 자리는 A1·A2·A3·A7·A11 다섯**, 나머지는 실패해도 통과.
- A6~A13 은 한 스레드가 user32 `DllMain` 안에서 `GdiDllInitialize` 한 번 부르는 동안 일어난다. `GdiProcessSetup` (A12 의 `ClientThreadSetup` 과 A13) 도 지연 가져오기라 `GdiProcessSetup` 을 처음 풀 때 A6 처럼 `NtProtectVirtualMemory` ×2 가 한 번 더 붙는다.

#### 표 B. 데이터 칸 표

| 어디 | 구조체 · 칸 | 값 | 읽는 줄 | 이게 없으면 |
|---|---|---|---|---|
| **PEB `+0xf8`** `GdiSharedHandleTable` | 포인터 | **`NtGdiInit2` 가 끝나는 때까지** GDI 공유 메모리 뷰 시작 주소 (ntdll·kernelbase 는 안 씀; 짐 지도 `baggage.md` 의 "끝까지 0" 목록에 있음; 쓰는 건 `win32kbase!GdiProcessCallout +0x22b` `[objdump + PDB 공개 이름]`) | `GdiProcessSetup +0x15a`·`+0x173`(첫 호출만), `+0x438`(매번) | 0 → `+0x161 CMP [0+0x180094]` 접근 위반 (주소 `0x180094`). 이름 한계: 구조체 타입 PDB 없음 `[칸 이름 추측]` |
| 그 뷰 `+0x180094` | dword | 언어팩(복합 스크립트) 켜짐 표시 `[의미 추측]`. **0 이면 `GdiInitializeLanguagePack` 건너뜀 (최소)** | `GdiProcessSetup +0x161` | 0 아니면 `LpkGetEditControl`·`LpkInitialize`·user32 `InitializeLpkHooks` 가 더 돔 → `ClientThreadSetup` 의 `NtUserRegisterLPK` 조건 |
| 그 뷰 `+0x1800b0 + 8*i` (스톡 객체 핸들 표) | qword × 22 (`i = 0 ~ 0x15`) | **`i = 0x12` (`+0x180140`, DC_BRUSH)·`i = 0x13` (`+0x180148`, DC_PEN) 은 0 이 아닌 값**. 나머지 칸은 `DllMain` 줄기가 안 읽음 | `GetStockObject +0x72` ← `DllMain +0x7c`·`+0x8b` | 둘 중 하나 0 → `+0x9e`/`+0xa3` **FALSE** |
| 그 뷰 `+0x180000` | `pGdiDevCaps` 가 가리킴 | 포인터 값만 계산(읽지 않음) | `GdiProcessSetup +0x456` | 읽는 건 그림·`GetDeviceCaps` 쪽 (다음 요청) |
| 그 뷰 전체 크기 | | 최소 `0x180150` 바이트 읽기 가능 (실제 뷰는 더 큼 `[추측]`) | 위 칸들 | 작으면 접근 위반 |
| TEB `+0x78` `Win32ThreadInfo` | 포인터 | `NtGdiInit2` 가 GUI 스레드로 바꾸고 나면 0 아닌 값이 정상 `[추측]` | `GetAppCompatFlags2 +0x12` | 0 이어도 `NtUserGetThreadState(0xe)` 한 번 더 부를 뿐 |
| TEB `+0x40` ClientId.UniqueProcess | dword | 프로세스 ID | `GdiProcessSetup +0x142` → `gW32PID` (`& ~3`) | |
| PEB `+0x10`·`+0x18`, `Ldr+0x20`, `LDR_DATA_TABLE_ENTRY` (`DllBase +0x30`, `BaseDllName +0x58/+0x60`) | | 실제 로더 목록 (exe 가 `InMemoryOrder` 목록 첫 칸) | kernelbase `K32GetModuleBaseNameW` 가 `NtReadVirtualMemory` 로 읽음 (A9) | 읽기 실패 → 이름 못 얻음 → 통과 |
| PEB `+0x58` `KernelCallbackTable` | 포인터 | user32 가 `+0x3fc` 에서 `apfnDispatch` 로 채움 (**`GdiDllInitialize` 보다 먼저**), `[84]` = `__ClientThreadSetup` | 커널이 A11 안에서 읽음 | 0 이면 콜백 못 부름 (`ClientThreadSetup` 안 돌아감) |
| gdi32 데이터 내보내기 칸 (gdi32full 이 가져오기 표로 씀) | `gMaxGdiHandleCount` (`0x25798`), `gCookie` (`0x257a0`), `pGdiSharedMemory` (`0x25778`), `pGdiSharedHandleTable` (`0x257a8`), `pGdiDevCaps` (`0x25788`), `GdiBatchLimit` (`0x25770`), `gW32PID` (`0x257b8`), `ghICM` (`0x257b0`), `fpClosePrinter` | `GdiDllInitialize` 가 `gMaxGdiHandleCount = 0x1000000`; `DllMain` 이 `gCookie`; `GdiProcessSetup` 이 나머지 | gdi32full 의 가져오기 칸 `0xc10a0`·`0xc1088`·`0xc0fb8`·`0xc10a8`·`0xc1060`·`0xc1078`·`0xc1090`·`0xc1058`·`0xc1050` | gdi32 가 안 올라왔으면 가져오기 실패 (로더 단계에서 끝) |
| gdi32full 전역 | `gbFirst` (`0x108474`), `GdiDllFullyInitialized` (`0x10c590`), `GdiHeap` (`0x109d20`), `gbInitialized` (`0x108cf4`) | 파일 안 처음 값: `gbFirst = 1`, 나머지 0 | `GdiProcessSetup +0x15`, `GdiDllInitialize +0x21`, `InitializeGdiHeap +0x8` | (우리 쪽 메모리 이미지가 파일 값 그대로면 문제 없음) |
| gdi32 `.didat` 칸 (`0x280e0` GdiDllInitialize, `0x27b80` GdiProcessSetup) | 지연 가져오기 슬롯 | 처음엔 `DelayLoad_*` 껍데기(`gdi32 rva 0xa765`·`0xc40c`)를 가리킴; `LdrpWriteBackProtectedDelayLoad` 가 gdi32full 주소로 바꿈 | gdi32 `GdiDllInitialize +0x0`, `GdiProcessSetup +0x0` | |
| PEB `ApiSetMap` | API 세트 스키마 | `api-ms-win-gdi-internal-uap-l1-1-0`·`ext-ms-win-gdi-internal-uap-init-l1-1-0`·`ext-ms-win-gdi-internal-desktop-l1-1-0` → `gdi32full.dll` | 로더·`LdrpLoadForwardedDll` | 이름 못 풀면 gdi32 가 안 올라옴/지연 로드 실패 |
| 레지스트리 | `HKLM\Software\Microsoft\Windows NT\CurrentVersion\GRE_Initialize` · 값 `DisableMetaFiles` (REG_DWORD) | 없어도 됨 | `ReadDisableMetaFilesRegKey +0xfe`·`+0x14c` | 통과 |
| user32 쪽 칸 (psi, 임계구역, PEB `+0x2c0`·`+0x90`·`+0x30` …) | | sast/82 질문 2 표 (`gSharedInfo` 0x240B · `psi` 읽기 가능 `0x1b68`B · `psi+0x8bc` 비트 `0x20` 0 …) 그대로 | §82 | §82 |

**지금 우리 (PE_emulator) 맞나·틀리나**
- 맞음(방향): 우리가 흉내 낼 `NtGdiInit2` 답이 이번 길의 핵심 하나다. 틀림(바로잡을 점): "공유 핸들 표 = `SharedSection` 뷰" 같은 가정이 있으면 안 맞는다 — user32 의 `SharedSection`·`gSharedInfo` (csrss 가 줌)와, gdi32 의 GDI 공유 메모리 (PEB `+0xf8`, win32k 가 줌) 는 **서로 다른 두 길**이다.
- 우리가 `NtGdiInit2` 에서 반드시 해야 할 최소 셋: ① 0 이 아닌 값 반환 ② PEB `+0xf8` 에 위 크기·칸을 가진 읽기 가능 메모리 연결 ③ (선택) `KernelCallbackTable[84]` 호출.

### 질문 4. 다음 요청 후보

1. **`win32kbase!GdiProcessCallout` (rva `0xe2010`, 안 `+0xca` `PsGetProcessPeb`, `+0x22b` PEB `+0xf8` 쓰기) 와 GDI 공유 메모리를 채우는 곳** — 뷰 안 `+0x180094`·`+0x1800b0` 표·`+0x180000` 장치 정보를 누가 어떻게 채우는지. 우리 엔진이 흉내 내야 할 몫이라 가장 급함. 단 "win32k 안쪽 깊이 안 넣음" 결정에서 예외가 필요 → **사용자 결정 필요**.
2. **`NtGdiInit2` 를 받는 win32kfull 쪽** — 반환 cookie 의 정체, 스레드를 GUI 로 바꾸는 순서와 `__ClientThreadSetup` 콜백이 `NtGdiInit2` **전에** 도는지 **중에** 도는지 (위 핵심 6의 `GdiProcessSetup` 첫 호출 자리).
3. **ntdll `LdrResolveDelayLoadedAPI` → `LdrpHandleProtectedDelayload` → `LdrpWriteBackProtectedDelayLoad` → `LdrpResolveProcedureAddress`** (지연 가져오기 해결 길, `NtProtectVirtualMemory` 2번·API 세트 풀이). 이번엔 ntdll 프로젝트가 쓰이는 중이라 `[objdump]` 로만 읽음. flow.md run82 에도 "첫 UEF 에서 delay-load" 가 후속으로 있음.
4. **창 만들기**: user32 `RegisterClassExW`·`CreateWindowExW`·`GetMessageW`/`DispatchMessageW` 가 읽는 `gSharedInfo` 칸 (§82 후보 1·3), `ClientThreadSetup` 의 `InitClientDrawing`·`NtUserRegisterLPK` (§82 후보 4·5).
5. **GDI 그리기**: gdi32full `CreateCompatibleDC`·`GetDC`·`SelectObject`·`BitBlt` 가 읽는 **GDI 공유 메모리 칸** (핸들 표 `GdiSharedHandleTable` 항목 모양, `pGdiDevCaps` 가 가리키는 `+0x180000`), `GdiBatchLimit` 이 쓰이는 TEB `GdiTebBatch` 칸 (`+0x2f0`), `NtGdi*` 중 첫 몇 개의 번호·답. 지금은 이름만.
6. **`LpkInitialize`·`InitializeLpkHooks`** (언어팩 플래그 `+0x180094` 가 켜졌을 때) — 최소 길에선 안 타지만 켜지면 user32 `NtUserRegisterLPK` 가 붙음.
7. **`msvcp_win.dll`** (gdi32full 이 정적으로 가져오는 C++ 런타임, 이번에 안 봄) 의 `DllMain`.

## 동적분석으로 확인해야 할 것 (Q104 에 붙임)

1. **`DllMain` 호출 차례**: `gdi32full!entry` → `gdi32!Gdi32DllInitialize` → `user32!UserClientDllInitialize` 중 누가 먼저인가 (로더가 정함, 순환 때문에 실행 때만 확정). 멈춤점: 각 `entry` 입구, `ntdll!LdrpCallInitRoutine`. `gdi32full!entry` 가 reason 1 에서 `+0x2a` 로 건너뛰는지 (`rax = 1` 로 돌아오고 `_DllMainCRTStartup` 안 불림).
2. **`NtGdiInit2` 주변**: `gdi32full!DllMain+0x2e` 입구에서 `rsp`, `+0x35` 에서 `rax` (cookie 값), 그 전에 `dq @$peb+0xf8 L1` 가 0 인지, 후엔 얼마인지, 그리고 그 사이에 `user32!__ClientThreadSetup` 이 불렸는지 (`bp user32!ClientThreadSetup` + `kb`). 커널 쪽: `ba w8 @$peb+0xf8` (test.exe 조건) → `win32kbase!GdiProcessCallout+0x22b`.
3. **GDI 공유 메모리 뷰 내용**: `dd poi(@$peb+f8)+180094 L1`, `dq poi(@$peb+f8)+1800b0 L16`, 뷰 크기 (`!address poi(@$peb+f8)`), `+0x180140`·`+0x180148` 이 0 아닌 값인지.
4. **`GdiProcessSetup` 첫 호출이 누구냐**: `gdi32full!GdiProcessSetup` 입구에서 `kb` — 호출자가 `user32!ClientThreadSetup+0x9a` (콜백이 `NtGdiInit2` 안에서 돌았다는 뜻) 인지 `gdi32full!DllMain+0x6c` 인지. 두 번째 호출이 `+0x1b JZ → +0x428` 로 바로 점프하는지 (`gbFirst` = 0).
5. **TEB `+0x78`** 이 `GetAppCompatFlags2 +0x12` 시점에 0 인가 → `win32u!NtUserGetThreadState` (`0x1000`) 가 불리는가.
6. **`K32GetModuleBaseNameW`** 안 `NtReadVirtualMemory` 가 정말 5번인지, 결과 이름이 `test.exe` 인지 (`kernelbase!K32GetModuleBaseNameW+0x84` ret 의 `eax`).
7. **지연 가져오기**: `ntdll!LdrpWriteBackProtectedDelayLoad+0x89`·`+0xf1` 가 `GdiDllInitialize`·`GdiProcessSetup` 에서 각각 걸리는가, 슬롯 `gdi32+0x280e0`·`+0x27b80` 이 풀린 뒤 gdi32full 주소인가.
8. **`NtQueryInformationProcess` 클래스 `0x34`** 의 실제 반환 (gdi32 `+0x52`, kernelbase `GetProcessMitigationPolicy+0x4e`) — 플래그 dword 값, 성공 여부.
9. **`GRE_Initialize\DisableMetaFiles`** 가 VM 에 있나 (`gbDisableMetaFiles` 값).
10. `PEB+0xf8+0x180094` 가 켜져 있나 → 켜져 있으면 `GdiInitializeLanguagePack` 이 돌고 `user32!InitializeLpkHooks` 가 불리는지.
11. CFG 간접 호출 `gdi32!Gdi32DllInitialize+0x9b` 의 `rax` (= `gdi32full+0x71030`) 가 CFG 검사를 통과하는지 (`GetProcAddress` 가 내보내기 억제를 푸는 `NtSetInformationVirtualMemory` 가 따로 보이는지) `[추측]`.
12. 이 모두의 syscall 로그 순서 (A6~A13) — test.exe 조건 syscall 추적으로 한 번에 보면 좋음.

추천 멈춤점 (우선순위): `gdi32full!DllMain+0x2e` / `+0x35`, `gdi32full!GdiProcessSetup` 입구, `user32!ClientThreadSetup` 입구, `nt!PsConvertToGuiThread` (조건 `test.exe`), `win32kbase!GdiProcessCallout+0x22b`.

## sast/82 "Q150b 로 넘길 것" 답 한눈에

| sast/82 가 넘긴 것 | 답 |
|---|---|
| ① `GdiDllInitialize` 안 `NtGdi*` syscall? | **`NtGdiInit2` (`0x12ee`) 하나** (`gdi32full!DllMain +0x2e`). `NtGdiInit` (`0x12ed`) 은 아님. 첫 win32k syscall → 스레드 GUI 전환·`__ClientThreadSetup` 콜백 자리 |
| ② `GdiSharedHandleTable` 을 어디서 받나 | gdi32full `GdiProcessSetup +0x15a`/`+0x173`/`+0x438` 이 PEB `+0xf8` 읽음. 쓰는 쪽은 win32kbase `GdiProcessCallout +0x22b` |
| ③ 음수를 돌려주는 자리 | `0xc0000017` (`+0x32`, 힙), `0xc0000001` (`+0x53`, `DllMain` FALSE) |
| ④ 두 번째 호출이 건너뛰는 것 | `GdiDllInitialize` 자체는 한 곳에서 한 번만 불림(`GdiDllFullyInitialized`); `GdiProcessSetup` 은 두 곳(`ClientThreadSetup +0x93`·`DllMain +0x67`)에서 불려 `gbFirst` 로 큰 몫을 건너뜀 |
| ⑤ `GdiProcessSetup` (user32 가 gdi32 에서 가져오는 이름) | gdi32 껍데기 `JMP [0x180027b80]` → 지연 가져오기 `ext-ms-win-gdi-internal-desktop-l1-1-0` → gdi32full `0x81a00` |

## 판정

**분석성공 (최소 길 지도 수준).** 요청서 질문 1~4 에 모두 `.asm` `+0x` 줄 근거와 함께 답했고 질문 3 의 표 둘을 만들었다. 한계: ① GDI 공유 메모리 칸 이름·의미는 PDB 타입이 없어 `[추측]`. ② kernelbase·ntdll·win32kbase 쪽 줄은 `objdump` 로 읽은 것 (`.asm` 이 없음, 프로젝트 안 열음) — 오프셋은 `call` 자리 기준이고 ret 은 +5/+7. ③ `NtGdiInit2` 안 콜백 타이밍(첫 `GdiProcessSetup` 호출 자리), 로더 `DllMain` 차례, 지연 가져오기의 `NtProtectVirtualMemory` 횟수는 동적 확인 필요.

# 105 dll-crt-small — 정적 분석 결과

- 요청서: `_workspace/sast/105_dll-crt-small.md` (Q73, 정적만)
- 모듈 / 바이너리: **dll1.dll**, **dll2.dll** (MS 바이너리가 아니라 sample 빌드, `ms/bin/dll1.dll`·`dll2.dll`, 로컬 pdb). Ghidra 프로젝트 `ms/ghidra/dll1`·`dll2` 는 이미 분석돼 있어서 **다시 analyze 안 했다.**
  - 둘 다 `/MT` (`sample/build.bat`) 라서 UCRT·vcruntime 이 dll 안에 들어 있다. 가져오는 DLL 은 `KERNEL32.dll` (88개 이름) 뿐이고, dll1 은 거기에 `dll2.dll` 하나 (`Dll2Hello`) 를 더 가져온다. **`syscall` 명령은 dll1 의 `.asm` 어디에도 없다** (grep 0개). `TlsAlloc`·`DisableThreadLibraryCalls` 도 안 가져온다.
- 뽑은 함수 (모두 `ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`):
  - dll1 요청 대상, depth 1: `__acrt_execute_initializers`, `__scrt_uninitialize_crt`, `__scrt_dllmain_crt_thread_attach`, `__scrt_dllmain_crt_thread_detach` (callee 7개 딸려 옴). `try_get_function_slow`·`dllmain_crt_dispatch` 는 run40 때 뽑은 `.asm` 그대로 읽었다.
  - **범위를 한두 칸 넓힌 것 (depth 0, 이유: 질문 1·3 이 "이름", "syscall 이 있나" 를 물어서)**
    - 길 A 를 끝까지 보려고: `__acrt_tls2_supported`, `__acrt_initialize_ptd`, `try_get_FlsGetValue2`, `get_FlsGetValue2`, `__acrt_FlsGetValue2`, `__acrt_initialize`, `__acrt_initialize_winapi_thunks`, `__acrt_lock`, `__acrt_unlock`, `initialize_pointers`.
    - 질문 2: `__acrt_uninitialize`, `__acrt_execute_uninitializers`, `__acrt_uninitialize_winapi_thunks`, `__acrt_uninitialize_ptd`, `__vcrt_uninitialize`, `__vcrt_uninitialize_ptd`, `uninitialize_vcruntime`, `__scrt_dllmain_uninitialize_c`, `__scrt_dllmain_uninitialize_critical`.
    - 질문 3: `__acrt_thread_attach/detach`, `__vcrt_thread_attach/detach`, `__acrt_getptd_noexit`, `__acrt_freeptd`, `__vcrt_getptd_noexit`, `__vcrt_freeptd_for_this_thread`, `internal_get_ptd_head_slow`, `destroy_ptd_array`, `destroy_fls`, `__vcrt_initialize_ptd`, `_calloc_base`, `_free_base`, `calloc`, `free`, `__acrt_FlsAlloc/Free/GetValue/SetValue` (JMP 스텁).
  - dll2: 위와 같은 이름 depth 0 (27개 + `destroy_fls`·`__vcrt_initialize_ptd`) — 오프셋 대조용.
  - **kernelbase·kernel32 에서 5개씩 depth 0**: `VirtualProtect`, `FlsGetValue`, `FlsSetValue`, `FlsFree`, `FlsAlloc` ("이 길에 syscall 이 있나" 에 답하려고 dll 이 부르는 줄을 한 칸 더 따라감. 이 두 프로젝트는 sast/103·106 이 안 쓴다). `ms/disassemble/kernelbase/_calls.tsv`·`kernel32/_calls.tsv` 에 줄이 덧붙었다.
  - **ntdll·ntoskrnl 프로젝트는 안 건드렸다.** ntdll 은 이미 있는 `.asm` 만 읽었고 (`RtlFlsSetValue`, `LdrShutdownProcess`, `LdrpProcessDetachNode`, `LdrpCallInitRoutine`), `RtlFlsGetValue2` 는 `.asm` 이 없어서 **ntdll.dll 파일 바이트를 `objdump` 로만** 봤다 (68바이트).
- 데이터 읽기: 표·문자열·후보 id 는 `pefile` 로 파일 바이트에서 직접 읽었다 (임시 스크립트는 scratchpad, 레포 밖).
- 버전 확인 (알려진 사실 = run40/sast/35 의 dll 줄):
  - 요청서 rva 12개 (dll1·dll2 × 6) 가 `_index.tsv` 와 **전부 일치** (dll2 가 dll1 보다 모두 `0x10` 앞).
  - run40 표의 호출 지점이 지금 `_calls.tsv` 와 **일치**: `dllmain_dispatch +0x76/+0x7b`·`+0x91/+0x96`·`+0xe8/+0xed`, `dllmain_crt_dispatch +0x42`·`+0x51` (꼬리 JMP), `try_get_function_slow +0x6d/+0x73` (dll1·dll2 둘 다).
  - `__scrt_uninitialize_crt`·`__acrt_uninitialize` 두 함수는 Ghidra `.asm` 과 `objdump` 가 **명령까지 같다** (원본 바이트 대조).
  - 스크립트 문제 없음.
- **dll1 ↔ dll2 대조**: 이번에 읽은 함수 31개 중 `.asm` 이 안쪽 오프셋·명령까지 **같다** (주소만 지우고 비교). 다른 건 칸 주소뿐:
  - 함수 시작 rva 가 전부 dll2 쪽이 `0x10` 앞 (`__scrt_uninitialize_crt` `0x23c0`/`0x23b0`, `dllmain_crt_dispatch` `0x1990`/`0x1980`, `__scrt_dllmain_crt_thread_attach` `0x2110`/`0x2100`, `…_detach` `0x2140`/`0x2130`, `__acrt_execute_initializers` `0x50e88`/`0x50e78`, `try_get_function_slow` `0x4f598`/`0x4f588`).
  - `try_get_function_slow +0x5a` 의 `module_names` 표 칸: dll1 `0x708b0` / dll2 `0x708a0`.
  - `FlsGetValue2` 후보 id 배열·이름 문자열: dll1 `0x70f58`/`0x70f60` / dll2 `0x70f48`/`0x70f50`. 내용 (id `[1, 19]`, `"FlsGetValue2"`) 은 같다.
  - `.fptable` (`0x8a000`)·`module_handles` (`0x834a0`)·`__acrt_use_tls2_apis` 같은 `.data` 칸은 두 dll 이 같은 rva.
  - 아래 `+0x..` 는 **dll1 기준이고 dll2 에도 그대로 맞다.**

## 질문별 답 [정적]

### 1. (a) `FlsGetValue2` 길 — dll 에서도 `try_get_function_slow` 로 가나?

- **답: 간다. test.exe (sast/25 길 A) 와 같은 길이다.** 이름·모듈은 요청서 짐작 (`kernel32`/`fibers-l2`) 과 다르다 — 아래 표.
- 길 (dll1, 괄호 안은 ret `+0x..`):

```
ntdll LdrpInitializeNode → … → dll1!_DllMainCRTStartup (이유 1) ══JMP══▶ dllmain_dispatch
dllmain_dispatch            +0x76 (+0x7b)   dllmain_crt_dispatch  ══JMP══▶ dllmain_crt_process_attach        (run40)
dllmain_crt_process_attach  +0x12 (+0x17)   __scrt_initialize_crt (ECX = 0, +0x10 XOR ECX,ECX)
__scrt_initialize_crt       +0x1d (+0x22)   __acrt_initialize
__acrt_initialize           +0xe  꼬리 JMP  __acrt_execute_initializers (RCX = 표 rva 0x6fd50, RDX = 표 끝 rva 0x6fe50)
__acrt_execute_initializers +0x25 (+0x2a)   표 항목을 `_guard_dispatch_icall` (JMP [0x18006e2e8]) 로 부름, 표 8번째 쌍 = __acrt_initialize_ptd
__acrt_initialize_ptd       +0x4  (+0x9)    __acrt_tls2_supported
__acrt_tls2_supported       +0x32 (+0x37)   try_get_function_slow (ECX = 3, RDX = "FlsGetValue2", R8/R9 = 후보 id 배열 [시작, 끝))
try_get_function_slow       +0x6d (+0x73)   LoadLibraryExW(이름, NULL, 0x800)
```

  - 표 `__acrt_initializers` (dll1 rva `0x6fd50`, 16칸 × 16바이트 = {시작 함수, 끝 함수}). 순서 (0부터): 0 `initialize_global_variables` · 1 `initialize_pointers` · 2 `__acrt_initialize_winapi_thunks` · 3 `initialize_global_state_isolation` · 4 `__acrt_initialize_locks` · 5 `__acrt_initialize_heap` · 6 (시작 없음, 끝 `uninitialize_vcruntime`) · **7 `__acrt_initialize_ptd`** · 8 `__acrt_initialize_lowio` · 9 `__acrt_initialize_command_line` · 10 `__acrt_initialize_multibyte` · 11~13 (끝 함수만) · 14 `initialize_environment` · 15 `initialize_c`. test.exe 의 "8번째 쌍" 과 같다.
  - `__acrt_tls2_supported` 가 칸 `.fptable+0x18` (`+0x4` 읽기) 이 0 이면 풀러 간다 (`-1` = 이미 실패, 0 아닌 값 = 이미 푼 주소). `+0x2f LEA ECX,[RAX+3]` → **함수 id 3**. `try_get_FlsGetValue2` (`0x4eedc`) 는 같은 일을 하는 사본인데 **부르는 곳이 없다** (`refs` 0줄), `get_FlsGetValue2` 도 마찬가지. 진짜 길은 `__acrt_tls2_supported` 쪽이다.
  - 풀린 주소가 쓰이는 곳: `__acrt_initialize_ptd +0x10` 이 결과를 `__acrt_use_tls2_apis` (rva `0x831c0`) 에 저장 → `__acrt_getptd_noexit +0x16` 이 보고 `+0x2e` 에서 `__acrt_FlsGetValue2` (rva `0x4fd2c`, 10바이트: `MOV RAX,[.fptable+0x18]` · `JMP RAX`) 로 부른다.
- 이름·모듈 (원본 바이트, `.rdata`):

| 항목 | dll1 rva | 값 |
|---|---|---|
| API 이름 (ANSI) | `0x70f60` | `FlsGetValue2` |
| 후보 id 배열 (4바이트 × 2) | `0x70f58` | `[1, 19]` |
| `module_names[1]` (UTF-16) | 포인터 표 `0x708b0` + `1*8` → `0x1800709b0` | **`api-ms-win-core-fibers-l1-1-2`** |
| `module_names[19]` | `0x180070e60` | **`kernelbase`** |
| 모듈 핸들 캐시 `module_handles` | `0x834a0` (id × 8) | 파일 값 0 |
| 함수 포인터 표 `.fptable` | `0x8a000` (0x100바이트, 구역 속성 읽기+쓰기, `+0x18` = id 3) | 파일 값 0 |

  - 요청서가 짐작한 `kernel32`·`fibers-l2` 는 **아니다.** 첫 후보 `api-ms-win-core-fibers-l1-1-2`, 둘째 `kernelbase`. test.exe 와 같은 id·같은 이름 (sast/25 표).
  - 첫 후보가 `LoadLibraryExW` 로 열리면 곧바로 `+0x19e` (ret `+0x1a4`) `GetProcAddress(모듈, "FlsGetValue2")` 하고 `+0x1a7 JMP +0xf2` 로 끝낸다 (둘째 후보는 안 간다). 첫 후보가 실패하면 캐시에 `-1` (`+0xd3 ~ +0xd7`) 넣고 둘째 `kernelbase` 로 (`+0xea JNZ +0x3c`).
  - **`api-ms-win-core-fibers-l1-1-2` 는 `kernelbase.dll` 로 풀린다** [정적: 24H2 `apisetschema.dll` 의 `.apiset` 표에서 그 이름 → 기본 호스트 `kernelbase.dll`]. 그리고 **`KERNELBASE!FlsGetValue2` 는 `NTDLL.RtlFlsGetValue2` 로 가는 forwarder** [정적: KernelBase.dll export 표 forwarder 문자열]. 그래서 `GetProcAddress` 가 돌려주는 주소 = **`ntdll!RtlFlsGetValue2`** (rva `0xd6930`, 68바이트: `gs:[0x17c8]` (= TEB `FlsData`, PDB `_TEB`) 읽고 칸 찾기만, 호출·syscall 없음 — `objdump`).
- **은퇴 전 (P2 앞) syscall 을 내나? → 낸다. 이 길만으로 `NtProtectVirtualMemory` 가 적어도 3번 (dll 마다).**
  - dll 의 CRT 코드 자체엔 `syscall` 이 없다. syscall 이 나오는 건 부르는 쪽이다:

| 자리 (dll1) | 부르는 것 | 안쪽 | syscall |
|---|---|---|---|
| `__acrt_initialize_winapi_thunks +0x20` (ret `+0x26`), 표 2번 (ptd 앞) | `VirtualProtect(.fptable, 0x100, 2 = 읽기 전용, &old)` | kernel32 `VirtualProtect` = 7바이트 스텁 `JMP [IAT 0x8b690]` → `api-ms-win-core-memory-l1-1-0` → (ApiSet 기본 호스트) `kernelbase.dll` → `kernelbase!VirtualProtect +0x34` (ret `+0x3b`) `NtProtectVirtualMemory` | **1번** |
| `try_get_function_slow +0xf9` (ret `+0xfe`) | `__acrt_lock(0xe)` = `EnterCriticalSection` (IAT `0x6e0b8`) | 안 막히면 커널 안 감 | 없음 `[추측: 혼자 돌 때]` |
| `+0x6d` (ret `+0x73`) | `LoadLibraryExW(L"api-ms-win-core-fibers-l1-1-2", NULL, 0x800)` | kernelbase `LoadLibraryExW +0x130` (ret `+0x137`) `LdrGetDllPath`, `+0xf8` (ret `+0xff`) `LdrLoadDll` | ntdll 안쪽은 **정적으로 안 읽음 → 확인 필요** |
| `+0x11d` (ret `+0x123`) | `VirtualProtect(.fptable, 0x100, 4 = 읽기+쓰기)` | 위와 같음 | **1번** |
| `+0x13c` | `XCHG [.fptable + id*8], 풀린 주소 (없으면 -1)` | — | 없음 |
| `+0x14e` (ret `+0x154`) | `VirtualProtect(…, 2 = 읽기 전용)` | 위와 같음 | **1번** |
| `+0x19e` (ret `+0x1a4`) | `GetProcAddress(모듈, "FlsGetValue2")` | kernelbase `GetProcAddress +0x7b` (ret `+0x82`) `LdrGetProcedureAddressForCaller` | ntdll 안쪽 **확인 필요** |

  - **`try_get_function_slow` 는 API 하나를 풀 때마다 (찾든 못 찾든) `+0xf0` 이후 `VirtualProtect` 두 번 (`+0x11d`, `+0x14e`) 을 탄다.** 그래서 dll 하나가 `DllMain` 앞 세 길 (A `FlsGetValue2`·B `LCMapStringEx`·C `AreFileApisANSI`, sast/25) 을 다 타면 `VirtualProtect` 는 1 + 2×3 = 7번쯤이다 (세 길이 dll 에서 실제로 세 번 타는지는 동적 — sast/35 가 남긴 공백, Q31).
  - `LoadLibraryExW` 플래그: **`0x800` (`LOAD_LIBRARY_SEARCH_SYSTEM32`)**, `+0x62 XOR EDX,EDX` (파일 핸들 NULL), `+0x67 MOV R8D,0x800`. 실패하면 `+0x7f` (ret `+0x85`) `GetLastError`: `0x57` (잘못된 인자) 이고 이름이 `api-ms-` (`+0x9a`, ret `+0x9f`, 문자열 rva `0x70ee8`) 도 `ext-ms-` (`+0xb0`, ret `+0xb5`, rva `0x70ef8`) 도 아닐 때만 `+0xc1` (ret `+0xc7`) 플래그 0 으로 한 번 더. 첫 후보 `api-ms-win-core-fibers-l1-1-2` 는 `api-ms-` 로 시작해서 **플래그 0 재시도는 안 탄다.** 둘째 후보 `kernelbase` 는 재시도 갈래가 열려 있다.
  - 이 호출이 `LoadLibraryExW` 라는 건 run40 의 `try_get_function_slow +0x73` 과 같다. `kernel32!LoadLibraryExW` 스텁 → `kernelbase!LoadLibraryExW` 는 run101.
- **지금 PE_emulator 맞나?: 확인 필요.**
  - 엔진 CLAUDE.md 를 보면 보호 syscall (`NtProtectVirtualMemory`) 은 이 레포가 직접 대답하는 종류라 위 3번은 문제 없을 것 같다 `[추측]`. dll 의 `_DllMainCRTStartup` (이유 1) "호출 약속" (나와도 되는 syscall 목록) 에 `NtProtectVirtualMemory` 가 3번 이상, 그리고 `LoadLibraryExW`·`GetProcAddress` 가 ntdll 안에서 내는 것이 들어 있는지는 못 봤다.
  - `PE_emulator/Src` 에서 `FlsGetValue2`·`RtlFls*`·`fibers-l1` 을 직접 다루는 줄은 grep 0개 — 이 길은 진짜 kernelbase·ntdll 코드가 돈다는 전제여야 한다. 그 코드가 `.fptable` 칸 `+0x18` 에 `ntdll!RtlFlsGetValue2` 를 써 넣어야 `__acrt_use_tls2_apis` 가 1 이 된다.

### 2. (b) `__scrt_uninitialize_crt(ECX = lpReserved ≠ 0, EDX = 0)` — 이유 0 에서 무엇이 달라지나

- **답: 갈림은 두 칸이다. ① `__scrt_uninitialize_crt` 안의 `DL` (`from_exit`) 가림은 dll 길에선 안 갈린다 (`DL` 은 늘 0). ② 진짜 갈림은 `ECX` (= `lpReserved ≠ 0`) 가 `__acrt_uninitialize`·`__vcrt_uninitialize` 안에서 갈린다.**
- 부르는 쪽 (이유 0):
  - `dllmain_dispatch +0xe8` (ret `+0xed`) → `dllmain_crt_dispatch` `+0x6 JZ +0x47` → **`+0x47 TEST R8,R8` · `+0x4a SETNZ CL`** → `+0x51` 꼬리 JMP `dllmain_crt_process_detach` (`CL` = `lpReserved ≠ 0`).
  - `process_detach +0xa MOVZX EDI,CL` 맡김 → `+0x41 __scrt_dllmain_uninitialize_c` → `+0x46 __scrt_uninitialize_type_info` → `+0x4b _RTC_Terminate` → `+0x5d __scrt_release_startup_lock` (이 앞은 `lpReserved` 에 안 갈림) → **`+0x62 XOR EDX,EDX` · `+0x64 MOVZX ECX,DIL` · `+0x68` (ret `+0x6d`) `__scrt_uninitialize_crt`** → `+0x70` (ret `+0x75`) `__scrt_dllmain_uninitialize_critical`.
  - `__scrt_uninitialize_crt` 를 부르는 곳은 이것 하나다 (`refs` 1줄).
  - 값이 무엇이 오나: 프로세스가 끝날 때 `ntdll!LdrShutdownProcess +0x17a` (`LdrpCallInitRoutine`, ret `+0x17f`) 가 **`R9 = R13 = 1`** (`+0x57 LEA R13D,[R12+1]`, `+0x16d MOV R9,R13`) 을 넘긴다 → **`lpReserved = 1` (≠ 0) → `CL = 1`.** DLL 을 `FreeLibrary` 로 내릴 때 길 `LdrpProcessDetachNode +0x136 XOR R9D,R9D` (call `+0x143`, ret `+0x148`) 은 **`lpReserved = 0` → `CL = 0`.** sample 은 dll 을 정적으로 import 하니 `main` 이 끝날 때 앞쪽 (`CL = 1`) 만 탄다.
- `__scrt_uninitialize_crt` (dll1 rva `0x23c0`, 43바이트):
  - `+0x6 CMP [is_initialized_as_dll],0` · `+0x10 JZ +0x16` · `+0x12 TEST DL,DL` · `+0x14 JNZ +0x23` — 즉 **`is_initialized_as_dll ≠ 0` 이고 `DL ≠ 0` 일 때만 안쪽을 통째로 건너뛴다.** `is_initialized_as_dll` (rva `0x82d20`) 는 `__scrt_initialize_crt +0x8` (`ECX = 0` 일 때만) 가 켠다 → dll 길에서는 1. 그런데 dll 길은 `DL = 0` 이라 **건너뛰기는 안 탄다.** (`DL ≠ 0` 길 = 이 dll 의 `exit`/`_cexit` 쪽 종료일 것 `[추측]`, 이번 요청 밖.)
  - 안 건너뛰면: `+0x16` (ret `+0x1b`) **`__acrt_uninitialize(ECX = CL 그대로)`**, `+0x1e` (ret `+0x23`) **`__vcrt_uninitialize(ECX = BL = CL)`**, `+0x23 MOV AL,1` 반환.
- 갈림 ① `__acrt_uninitialize` (rva `0x21b14`, 53바이트):

| `CL` | 하는 일 | 근거 |
|---|---|---|
| **1** (프로세스 끝, `lpReserved ≠ 0`) | `__piob` (rva `0x839c0`, 표준 스트림 표) 가 0 이 아니면 **`_flushall`** (`+0x12`, ret `+0x17`) 만 하고 `AL = 1` 반환. **정리 함수 표는 안 돈다.** | `+0x4 TEST CL,CL` · `+0x6 JZ +0x1e` 안 탐 · `+0x8 CMP` · `+0x10 JZ +0x17` |
| **0** (`FreeLibrary`, `lpReserved = 0`) | `+0x1e` 에서 `RDX = 표 끝`, `RCX = __acrt_initializers` 를 싣고 **`+0x30` 꼬리 JMP `__acrt_execute_uninitializers`** (rva `0x50f08`) | `+0x6 JZ +0x1e` 탐 |

  - `__acrt_execute_uninitializers` 는 표 끝에서 거꾸로, 칸마다 **두 번째(끝) 함수** 가 0 이 아니면 `ECX = 0` 으로 부른다 (`+0x15 MOV RAX,[RBX-8]` · `+0x1e XOR ECX,ECX` · `+0x20` ret `+0x25` CFG 간접). 부르는 차례 (표 번호): 15 `uninitialize_c` · 14 `uninitialize_environment` · 13 `uninitialize_allocated_memory` · 12 `uninitialize_allocated_io_buffers` · 11 `report_memory_leaks` · (10 없음) · 9 `__acrt_uninitialize_command_line` · 8 `__acrt_uninitialize_lowio` · 7 `__acrt_uninitialize_ptd` · 6 `uninitialize_vcruntime` · 5 `__acrt_uninitialize_heap` · 4 `__acrt_uninitialize_locks` · 3 `uninitialize_global_state_isolation` · 2 `__acrt_uninitialize_winapi_thunks` (0·1 은 끝 함수 없음). 안쪽 locale·stdio 는 이름만.
  - 눈에 띄는 것: `__acrt_uninitialize_ptd +0xf` (ret `+0x14`) `FlsFree(__acrt_flsindex)`, `__acrt_uninitialize_winapi_thunks +0x1f` (ret `+0x25`) 가 `module_handles` 칸마다 `FreeLibrary` (아까 `LoadLibraryExW` 로 연 모듈을 닫음, `CL = 0` 일 때만 — `+0x6 TEST CL,CL` · `+0x8 JNZ +0x39`), `uninitialize_vcruntime` (`XOR ECX,ECX` · 꼬리 JMP `__vcrt_uninitialize`).
- 갈림 ② `__vcrt_uninitialize` (rva `0x32b0`, 25바이트): `+0x4 TEST CL,CL` · `+0x6 JNZ +0x12` → `CL ≠ 0` 이면 아무것도 안 하고 `AL = 1`. `CL = 0` 이면 `+0x8` (ret `+0xd`) `__vcrt_uninitialize_ptd` (`FlsFree(__vcrt_flsindex)`) 와 `+0xd` (ret `+0x12`) `__vcrt_uninitialize_locks`.
- 한 줄: **프로세스 끝 (`CL = 1`)** = `_flushall` 만 (스트림이 있을 때), 정리 표·`FreeLibrary`·`FlsFree` 는 **안 한다.** **`FreeLibrary` (`CL = 0`)** = 표를 거꾸로 다 돌고 `FlsFree`·`FreeLibrary`·락 정리까지 한다.
- sample 에서 `CL = 1` 일 때 `_flushall` 이 불리나: `__piob` 를 쓰는 곳은 `__acrt_initialize_stdio` (rva `0x52060`) 하나이고 (`_refs/0x839c0.tsv`), 그 함수는 **`.CRT$XI` 표** (`.CRT$XIA` rva `0x6e320`, 항목 셋 중 둘째 `0x6e330`) 에 있어서 `dllmain_crt_process_attach +0x6b` (ret `+0x70`) `_initterm_e` 가 부른다 → `__piob ≠ 0` 이라 **`_flushall` 이 불린다** `[정적]`. 스트림에 쓴 게 없으니 `WriteFile` 로 이어지는지는 안쪽을 안 봐서 모른다 (`Log` 는 `OutputDebugStringA`·`WriteFile` 직접이고 CRT 스트림은 안 쓴다 — `sample/log.h`).
- **지금 PE_emulator 맞나?: 확인 필요.** 프로세스 끝 길이면 `CL = 1` 이라 `FreeLibrary`·`FlsFree` 같은 정리가 없다는 게 정적 답이다 — `155_process-exit` 가 dll1·dll2 `PROCESS_DETACH` 를 진짜로 받는 건 확인했다고 적었다 (`PE_emulator/_workspace/00_planner/155_process-exit.md:114~119`). `_flushall` 이 `NtWriteFile` 을 내는지는 모른다.

### 3. (c) `dllmain_crt_dispatch` 이유 2·3 길 — 무엇을 부르나, TLS·FLS 정리?, syscall?

- **답: 이유 2 = `__scrt_dllmain_crt_thread_attach` (스레드마다 CRT 칸 만들기), 이유 3 = `__scrt_dllmain_crt_thread_detach` (CRT 칸 돌려주기). 둘 다 TLS 가 아니라 FLS (fiber local storage) 칸 + 힙 한 덩이씩이다. dll 코드에 `syscall` 은 없다.**
- 가름 (`dllmain_crt_dispatch`, rva `0x1990`, 86바이트):

| 이유 (`EDX`) | 줄 | 가는 곳 |
|---|---|---|
| 0 | `+0x4 TEST` · `+0x6 JZ +0x47` | `+0x51` 꼬리 JMP `dllmain_crt_process_detach` (질문 2) |
| 1 | `+0x8 SUB 1` · `+0xb JZ +0x3b` | `+0x42` 꼬리 JMP `dllmain_crt_process_attach` (run40) |
| **2** | `+0xd SUB 1` · `+0x10 JZ +0x2e` | **`+0x2e` (ret `+0x33`) `__scrt_dllmain_crt_thread_attach`**, `MOVZX EAX,AL` |
| **3** | `+0x12 CMP EDX,1` · `+0x15 JZ +0x21` | **`+0x21` (ret `+0x26`) `__scrt_dllmain_crt_thread_detach`**, `MOVZX EAX,AL` |
| 그 밖 | `+0x17 MOV EAX,1` | 그냥 1 |

  - 이 두 함수의 다른 부르는 곳은 `_CRT_INIT +0x21`·`+0x2e` 뿐인데 `_CRT_INIT` (rva `0x1d00`) 은 **부르는 곳이 없다** (`refs` 0줄, `.pdata` 만) → 안 쓰는 사본.
  - `dllmain_dispatch` 쪽 **순서가 이유마다 다르다**: 이유 2 는 `+0x76` (ret `+0x7b`) 에서 `dllmain_crt_dispatch` 를 **`DllMain` 앞에** 부르고, 0 이 오면 (`+0x83 JZ +0x11e`) `DllMain` 을 건너뛴다. 이유 3 은 `DllMain` (`+0x91`) **뒤에** `+0xe8` (ret `+0xed`) 에서 부른다.
  - sample `DllMain` 은 `DisableThreadLibraryCalls` 를 안 부른다 (`DllMain.asm` — `ReasonName`·`Log` 만) → 새 스레드가 생기면 이유 2·3 이 온다.
- **이유 2 `__scrt_dllmain_crt_thread_attach`** (rva `0x2110`, 41바이트):
  1. `+0x4` (ret `+0x9`) **`__vcrt_thread_attach`** → `+0x4` (ret `+0x9`) `__vcrt_getptd_noexit`. 실패 (`AL = 0`) 면 `+0xb JZ +0x1b` 로 `AL = 0` 반환.
  2. `+0xd` (ret `+0x12`) **`__acrt_thread_attach`** → `+0x4` (ret `+0x9`) `__acrt_getptd_noexit`. 성공이면 `+0x14 JNZ +0x22` 로 `AL = 1`.
  3. acrt 가 실패하면 `+0x16` (ret `+0x1b`) `__vcrt_thread_detach` 로 되돌리고 `AL = 0`.
  - `__vcrt_getptd_noexit` (rva `0x19a80`, 209바이트): `__vcrt_flsindex` (rva `0x82140`) 가 `-1` 이면 0 반환. 아니면 `+0x1e GetLastError` → `+0x2c FlsGetValue(idx)` → 값이 `0` (이 스레드 첫 번) 이면 `+0x4d FlsSetValue(idx, -1)` → **`+0x6a` (ret `+0x6f) calloc(1, 0x80)`** (`+0x5b MOV EDX,0x80` · `+0x65 MOV ECX,1`) → `+0x80 FlsSetValue(idx, 새 덩어리)` → `+0x8c` 덩어리 `+0x78` 에 `0xfffffffe` → `+0xb9 SetLastError` 로 에러값 되돌림.
  - `__acrt_getptd_noexit` (rva `0x4982c`, 144바이트): **`__acrt_use_tls2_apis` (rva `0x831c0`) 가 1 이면** (`+0x16 CMP` · `+0x1d JZ +0x49`) `+0x2e` (ret `+0x33`) **`__acrt_FlsGetValue2`** (= `ntdll!RtlFlsGetValue2`, 질문 1) 로 읽고, 값이 `0` 이면 `+0x3f` (ret `+0x44`) **`internal_get_ptd_head_slow`**. 0 이면 (`+0x49` 이하) 옛 길: `GetLastError` → `+0x61 __acrt_FlsGetValue` → `+0x72 internal_get_ptd_head_slow` → `SetLastError`.
  - `internal_get_ptd_head_slow` (rva `0x49540`, 181바이트): `GetLastError` → `FlsSetValue(idx, -1)` (`+0x21`) → **`_calloc_base(1, 0x3c8)`** (`+0x42`, ret `+0x47` — 안에서 `HeapAlloc`, `_calloc_base +0x55`) → `FlsSetValue(idx, 덩어리)` (`+0x68`) → `construct_ptd_array` (`+0x8b`, ret `+0x90`) → `SetLastError`.
  - **정리해서 말하면 이유 2 가 하는 일** = 새 스레드의 FLS 칸 두 개 (vcrt 용 `0x80`바이트 덩어리, acrt 용 `0x3c8`바이트 덩어리 + 안쪽 배열) 를 **힙에서 받아 FLS 칸에 넣는 것.** 이미 칸이 있으면 (예: 주 스레드는 `process_attach` 때 `__acrt_initialize_ptd +0x2a` 가 만들었다) 읽기만.
- **이유 3 `__scrt_dllmain_crt_thread_detach`** (rva `0x2140`, 21바이트): `+0x4` (ret `+0x9`) **`__acrt_thread_detach`** → `+0x4` (ret `+0x9`) `__acrt_freeptd`; `+0x9` (ret `+0xe`) **`__vcrt_thread_detach`** → `+0x4` (ret `+0x9`) `__vcrt_freeptd_for_this_thread`; `+0xe MOV AL,1` (**항상 1**).
  - `__acrt_freeptd` (rva `0x4977c`, 78바이트): `+0x1e __acrt_FlsGetValue` → 값이 있으면 `+0x33` (ret `+0x38`) `__acrt_FlsSetValue(idx, 0)` (**FLS 칸을 먼저 비움**) → `+0x3b` (ret `+0x40`) `destroy_ptd_array` (안에서 `_free_base` 열 번쯤 + 람다 둘) → `+0x43` (ret `+0x48`) `_free_base(덩어리)` (안에서 `HeapFree`, `_free_base +0x16`).
  - `__vcrt_freeptd_for_this_thread` (rva `0x19940`): `+0x14 FlsGetValue` → `+0x25 FlsSetValue(idx, 0)` → 값이 있고 `__vcrt_startup_thread_ptd` (rva `0x82d80`, 주 스레드 시작용 정적 칸) 가 아니면 `+0x48` 꼬리 JMP `free`.
- **TLS 인가 FLS 인가**: 둘 다 **FLS** (`FlsAlloc/FlsGetValue/FlsSetValue/FlsFree` + 선택적으로 `FlsGetValue2`). `TlsAlloc` 쓰는 줄 없음. `__declspec(thread)` (`.tls` 구역, 로더가 `LdrpInitializeThread` 등에서 채우는 암시적 TLS) 는 이 두 함수가 **안 건드린다.** FLS 칸에는 파괴자 (콜백) 가 걸려 있다: acrt `destroy_fls` (rva `0x49320`, `__acrt_initialize_ptd +0x9 LEA RCX` → `+0x16 FlsAlloc`), vcrt `__vcrt_freefls` (rva `0x19920`, `__vcrt_initialize_ptd +0x4 LEA RCX` → `+0xb FlsAlloc`). 이유 3 이 칸을 먼저 비우니 (`FlsSetValue(idx, 0)`) 스레드가 끝날 때 ntdll 이 FLS 콜백을 불러도 이 칸은 비어 있을 것이다 `[추측]`. 콜백을 ntdll 이 언제 부르나는 이번 범위 밖.
- **syscall 이 있나?**
  - dll 코드: **없다.** 부르는 줄은 전부 `KERNEL32` 가져오기 (`GetLastError`·`SetLastError`·`FlsGetValue`·`FlsSetValue`·`HeapAlloc`·`HeapFree`) 와 함수 포인터 `FlsGetValue2`.
  - 한 칸 안쪽 (이번에 읽음): `GetLastError`/`SetLastError` 는 TEB 읽고 쓰기. `FlsGetValue2` (= `ntdll!RtlFlsGetValue2`) 는 TEB `FlsData` 읽기만 (syscall·호출 없음, `objdump`). `kernelbase!FlsGetValue` (`+0x1f`, ret `+0x26`) 와 `FlsSetValue` (`+0x4`, ret `+0xb`) 는 얇은 겉포장 → `ntdll!RtlFlsGetValue`/`RtlFlsSetValue` (kernel32 쪽 `FlsGetValue` 는 99바이트짜리 같은 모양의 사본, `FlsSetValue` 는 7바이트 스텁 → `api-ms-win-core-fibers-l1-1-0`). 기존 `ntdll/RtlFlsSetValue.asm` 에 `SYSCALL` 명령 0개, 안에서 `RtlpFlsHeapAlloc` (+0x94, +0xc0)·`RtlAcquireSRWLockExclusive` (+0x10b)·`RtlReleaseSRWLockExclusive` (+0x142)·`memset` (+0x176) 을 부른다. `HeapAlloc`·`HeapFree` 는 힙 함수 (`RtlAllocateHeap`/`RtlFreeHeap`).
  - 그러므로 **직접 syscall 은 없고, 힙이 모자라거나 비울 때만 커널로 간다** (`NtAllocateVirtualMemory`/`NtFreeVirtualMemory`) — 새 스레드의 첫 FLS 칸 (`RtlpFlsHeapAlloc`) 과 `calloc`/`free` 가 그런 자리다. 실제로 나는지는 **정적으로 못 정함 → 동적**.
- 이 길은 지도 밖이다 (새 스레드 = 다음 범위). 위는 이름과 오프셋 수준 + 질문이 물은 syscall 까지만이다.
- **지금 PE_emulator 맞나?: 확인 필요.** 진짜 코드가 도는 길이라 CRT 쪽을 흉내 낼 건 없다. 다만 새 스레드의 `FlsData` 가 0 으로 시작해서 `RtlFlsGetValue2` 가 0 을 돌려주는 것이 맞으려면 TEB `+0x17c8` 이 새 스레드 TEB 에서 0 이어야 하고, `NtCreateThreadEx` 가 `155_process-exit` 메모대로 아직 기본 대답 (`thread_attribute_list_unsupported`, Worker 안 돎, `PE_emulator/_workspace/00_planner/155_process-exit.md:122`) 이라 **이 길은 아직 못 탄다.** Worker 가 돌기 시작하면 이유 2·3 에서 위 호출들이 나온다.

## 근거 줄 모음 (`.asm`, dll1)

| 파일 | 줄 |
|---|---|
| `ms/disassemble/dll1/__acrt_execute_initializers.asm` | `+0x25` (ret `+0x2a`) · `+0x56` (ret `+0x5b`, 실패 때 거꾸로 `ECX=0` 로 끝 함수) |
| `__acrt_tls2_supported.asm` | `+0x4` 읽기 · `+0x1a ~ +0x2f` 인자 · `+0x32` (ret `+0x37`) |
| `__acrt_initialize_ptd.asm` | `+0x4` (ret `+0x9`) · `+0x10` 쓰기 · `+0x16` (ret `+0x1b`) `FlsAlloc` · `+0x2a` (ret `+0x2f`) |
| `try_get_function_slow.asm` | `+0x62 ~ +0x73` · `+0x7f` · `+0x9a` · `+0xb0` · `+0xc1` · `+0xd3 ~ +0xd7` · `+0xf9` · `+0x11d` · `+0x13c` · `+0x14e` · `+0x15a` · `+0x192` · `+0x19e` · `+0x1a7` · `+0x1ac` (`abort`) |
| `__scrt_uninitialize_crt.asm` | `+0x6 ~ +0x14` · `+0x16` · `+0x1e` |
| `__acrt_uninitialize.asm` | `+0x4 ~ +0x17` · `+0x1e ~ +0x30` |
| `dllmain_crt_dispatch.asm` | `+0x4 ~ +0x17` · `+0x21` · `+0x2e` · `+0x42` · `+0x47 ~ +0x51` |
| `__scrt_dllmain_crt_thread_attach.asm`·`..._detach.asm` | 전체 |
| `dllmain_crt_process_detach.asm` | `+0x41 ~ +0x75` |
| `dllmain_dispatch.asm` | `+0x3f ~ +0x45` · `+0x76` · `+0x83` · `+0x91` · `+0xd7 ~ +0xe8` · `+0xed ~ +0xf5` |
| `ntdll/LdrShutdownProcess.asm` (읽기만) | `+0x35` · `+0x57` · `+0x16d` · `+0x17a` |
| `ntdll/LdrpProcessDetachNode.asm` (읽기만) | `+0x136` · `+0x139` · `+0x143` |

## 동적으로 확인할 것 (선택 — 정적 답은 위에서 나왔다)

1. **(Q31 과 겹침)** `LoadLibraryExW` 입구 `poi(@rsp)` 가 dll1 `try_get_function_slow +0x73` (rva `0x4f60b`; dll2 `0x4f5fb`) 일 때 `du @rcx` → 첫 번이 `api-ms-win-core-fibers-l1-1-2` (길 A) 인가, `r r8d` = `0x800` 인가, 돌려주는 `rax` = `kernelbase` 베이스인가. `k` 에서 `__acrt_tls2_supported +0x37` → `__acrt_initialize_ptd +0x9` → `__acrt_execute_initializers +0x2a` 가 보이면 길 A 확인. dll 마다 몇 번 타는지 (A·B·C 셋인가) 는 Q31 이 이미 세는 항목.
2. 은퇴 전 syscall: 같은 구간에서 `NtProtectVirtualMemory` 가 `VirtualProtect` 줄 (dll1 ret rva `0x509fe`, `0x4f6bb`, `0x4f6ec`; dll2 `0x509ee`, `0x4f6ab`, `0x4f6dc`) 마다 한 번씩 나는지, 인자 `.fptable` (dll 베이스 + `0x8a000`, 크기 `0x100`) · 보호 `2 → 4 → 2`. **`LoadLibraryExW` (`api-ms-…`) 와 `GetProcAddress` 가 ntdll 안에서 내는 syscall 수** (0 이어도 이상하지 않다 — 이미 올라온 `kernelbase` 이기 때문. 정적으로 못 정함).
3. 값: P2 (`NtContinue` 입구) 때 `dq dll1+0x8a018 L1` = `ntdll!RtlFlsGetValue2` 주소인가, `db dll1+0x831c0 L1` = 1 인가, `!vprot dll1+8a000` = `PAGE_READONLY` 인가 (dll2 도 같은 칸).
4. 이유 2·3 (새 스레드, `CreateThread`): `__scrt_dllmain_crt_thread_attach` (dll1 `+0x2110`) 입구~ret 사이 syscall 수 (예상 0, `NtAllocateVirtualMemory` 는 힙이 모자랄 때만), `__acrt_getptd_noexit +0x33` (rva `0x4985f`) `rax` = 0 (새 스레드라 FLS 칸이 비어 있음), `__scrt_dllmain_crt_thread_detach` (`+0x2140`) 입구~ret 사이 syscall 수, 이유 3 뒤 ntdll 이 FLS 콜백 (`destroy_fls` rva `0x49320`·`__vcrt_freefls` rva `0x19920`) 을 부르는지.
5. 프로세스 끝 (이유 0): `__scrt_uninitialize_crt` 입구 (dll1 rva `0x23c0`) `cl` = 1 (`LdrShutdownProcess +0x17a` 에서 온 것), `+0x16` 이 서서 `__acrt_uninitialize` 의 `+0x12` (`_flushall`) 가 불리는지, 그 사이 `NtWriteFile`/`NtFlushBuffersFile` 이 나는지, `__acrt_uninitialize_winapi_thunks` (`dll1+0x50b38`) 가 **안** 서는지 (`CL = 1` 이면 `FreeLibrary` 없음).
6. 값 하나 (싸다): `dq dll1+0x839c0 L1` (`__piob`) ≠ 0 인가 (`.CRT$XI` 둘째 항목 `__acrt_initialize_stdio` 가 돈 결과).

추천 멈춤점: `dll1+0x4f605` (call 줄, 인자 `rcx`·`r8d` 를 볼 때) 와 `dll1+0x4f60b` (ret, `rax`), `dll1+0x23c0` (질문 2 입구), `dll1+0x2110` / `dll1+0x2140` (질문 3). 모두 `dll2` 는 `−0x10`. **dast 요청 필요 여부: 필수는 없다** (정적 답이 나왔다). 2·3 은 Q31·Q104 묶음에 한 줄씩 붙이면 된다.

## 다음 요청 후보

- **(정적, ntdll — 103 이 끝난 뒤)** `LdrLoadDll` 이 ApiSet 이름 (`api-ms-win-core-fibers-l1-1-2`, 확장자 없음, 플래그 `0x800`) 을 받아 **이미 올라온 `kernelbase.dll` 로 풀어 돌려줄 때 syscall 이 몇 번인가** (`LdrpLoadDllInternal`·`LdrpApplyApiSet…`·`LdrpFindLoadedDllByName…` 갈림). 질문 1 의 마지막 빈칸이고 PE_emulator "호출 약속" 에 바로 필요하다.
- **(정적, ntdll)** `RtlFlsSetValue`·`RtlpFlsHeapAlloc` 이 새 스레드 첫 칸에서 `RtlAllocateHeap` 말고 직접 부르는 것, 그리고 `LdrShutdownThread`·`LdrpInitializeThread` 가 FLS 콜백 (`RtlpFlsDataCleanup`) 을 어디서 부르는지 — 질문 3 의 "FLS 정리" 뒷부분.
- **(정적, dll1)** `__acrt_initialize_stdio` 와 `_flushall` 의 안쪽 — 프로세스 끝에 syscall (`NtWriteFile`) 을 내는 조건 (질문 2 의 빈칸). 범위 밖이라 이번엔 이름만.
- 새 요청 없이 닫는 것: 이 결과로 Q73 (a)(b)(c) 정적은 닫힌다. 동적 몫은 Q31·Q104 에 한 줄씩.

## 판정

**분석성공.** 세 질문 모두 `.asm` 줄로 답했다. (a) dll 에서도 같은 길, 이름은 `FlsGetValue2` → `api-ms-win-core-fibers-l1-1-2` → `kernelbase` (forwarder 로 `ntdll!RtlFlsGetValue2`), 은퇴 전 `NtProtectVirtualMemory` ×3 이상. (b) 프로세스 끝 `CL = 1` 은 `_flushall` 만, `FreeLibrary` `CL = 0` 은 표 거꾸로 전부. (c) 이유 2·3 은 FLS 칸 + 힙, dll 코드에 syscall 없음. dll1·dll2 명령 동일. 못 정한 것은 ntdll 로더 (`LdrLoadDll`·`LdrGetProcedureAddressForCaller`) 안의 syscall 수와 힙이 커널로 가는 때 (둘 다 "확인 필요", 범위 밖 요청 후보로 넘김).

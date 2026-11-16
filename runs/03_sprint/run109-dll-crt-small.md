# 2026-11-16 (작성일) run109 — sample dll CRT 작은 정적 셋 (Q73, 정적만): **dll1 · dll2 도 `FlsGetValue2` 를 test.exe 와 같은 길 (`__acrt_initialize_ptd +0x4` → `__acrt_tls2_supported +0x32` → `try_get_function_slow +0x6d LoadLibraryExW(L"api-ms-win-core-fibers-l1-1-2", NULL, 0x800)`) 로 찾는다** `[정적]`. 그 ApiSet 은 `kernelbase.dll` 로 풀리고, `KERNELBASE!FlsGetValue2` 는 forwarder `NTDLL.RtlFlsGetValue2` 라 함수 포인터 칸 `.fptable +0x18` 에 들어가는 값은 **`ntdll!RtlFlsGetValue2`** `[정적: PE 표]`. 이 길만으로 **dll 마다 은퇴 전 `NtProtectVirtualMemory` 가 적어도 3번** (`VirtualProtect` — `__acrt_initialize_winapi_thunks +0x20` 1번 + `try_get_function_slow +0x11d` · `+0x14e` 2번). 이유 0 (DETACH) 은 프로세스 끝 (`lpReserved = 1` → `CL = 1`) 이면 **`_flushall` 만** 하고 정리 표 · `FreeLibrary` · `FlsFree` 는 안 한다. 이유 2 · 3 은 **FLS 칸 + 힙 덩이 만들기 · 비우기** 이고 dll 코드에 `syscall` 은 없다.

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표. 함수 안 오프셋은 dll1 · dll2 가 같고 시작 rva 만 dll2 가 `0x10` 앞이다.)

> **지도 범위**: 2026-10-23 결정 "진입점 = 경계, CRT 한 상자" 그대로다. 이 문서의 CRT 함수는 flow.md 2.8 "CRT 안쪽 (참고)" 에만 적고 그림 2 는 안 고쳤다. 은퇴 전에 나는 syscall (`NtProtectVirtualMemory` ×3 이상) 은 글로만 적었다.

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 새 사진 읽기도 없다. 사진 값은 run44 덤프 읽기 로그의 TEB `FlsData` 두 줄만 다시 읽었다 (flow-writer, 아래 "확인된 것").
- 정적: **dll1.pdb `B1FF0915-FAC1-447A-AEB8-10FF10AD0564` age 1, dll2.pdb `FC67F681-4188-49D8-83B5-609127E1DE39` age 1** (run40 과 같음, sample 빌드의 로컬 pdb). Ghidra 프로젝트 `ms/ghidra/dll1` · `dll2` 는 이미 분석돼 있어 **다시 analyze 안 함** (결과서).
  - 둘 다 `/MT` (`sample/build.bat`) 라 UCRT · vcruntime 이 dll 안에 있다. 가져오는 DLL 은 `KERNEL32.dll` 뿐 (+ dll1 은 `dll2.dll` 의 `Dll2Hello`) `[정적: PE import]` (결과서).
  - 버전 대조 (결과서): 요청서 rva 12개 (dll1 · dll2 × 6) 가 `_index.tsv` 와 전부 같다. run40 표의 호출 지점 (`dllmain_dispatch +0x76/+0x7b` · `+0x91/+0x96` · `+0xe8/+0xed`, `dllmain_crt_dispatch +0x42` · `+0x51`, `try_get_function_slow +0x6d/+0x73`) 이 지금 `_calls.tsv` 와 같다. `__scrt_uninitialize_crt` · `__acrt_uninitialize` 는 Ghidra `.asm` 과 `objdump` 가 명령까지 같다.
- kernelbase (pdb `72CCE506944DE0267C172AD64287F9BD1`) · kernel32 (pdb `AE9E9D8F7AC1467A645320FDC23235D01`): `VirtualProtect` · `FlsGetValue` · `FlsSetValue` · `FlsFree` · `FlsAlloc` 다섯씩 depth 0 새로 뽑음 ("이 길에 syscall 이 있나" 에 답하려고 한 칸 더). `_calls.tsv` 두 개에 줄이 붙었다.
- ntdll (pdb `C3093720177DA6851519DCFFE8DF53FE1`): **프로젝트 안 건드림.** 이미 있는 `.asm` (`RtlFlsSetValue`, `LdrShutdownProcess`, `LdrpProcessDetachNode`, `LdrpCallInitRoutine`) 만 읽었고, `RtlFlsGetValue2` (rva `0xd6930`, 68바이트) 는 `.asm` 이 없어 **ntdll.dll 바이트를 `objdump` 로만** 봤다 (결과서).
- PE 표 (`pefile`, 결과서): dll1 `.rdata` 의 이름 문자열 · 후보 id 배열 · `module_names` 표, 24H2 `apisetschema.dll` `.apiset` 표, KernelBase.dll export forwarder 문자열.
- 다른 decompiler 의 sast/106 ~ 120 결과는 이 문서에 **안 넣었다** (다음 바퀴들 몫).

## 목적

- **Q73** (run40 · sast/35 "새 정적 공백" 셋):
  - (a) `__acrt_execute_initializers` 의 함수 포인터 표에서 `FlsGetValue2` 를 찾는 길이 dll 에서도 `try_get_function_slow` (`LoadLibraryExW` · `GetProcAddress`) 로 가나. 이름 · 모듈 이름 · 플래그. 은퇴 전 (P2 앞) syscall 을 내나.
  - (b) `__scrt_uninitialize_crt(ECX = (lpReserved ≠ 0), EDX = 0)` 안쪽 — 이유 0 에서 무엇이 달라지나.
  - (c) `dllmain_crt_dispatch` 이유 2 · 3 (`__scrt_dllmain_crt_thread_attach/detach`) 이 무엇을 부르나, TLS · FLS 정리인가, syscall 이 있나.
- 앞 바퀴 짝: run40 (`runs/02_sprint/run40-dll-crt-entry.md`, sast/35 — 이유 1 길), run30 (`runs/02_sprint/run30-exe-crt-start.md`, sast/25 — test.exe 의 같은 CRT 길 A · B · C), run84 (`runs/03_sprint/run84-console-crt.md`, sast/81 — `/MT` CRT lowio).

## 방법

- 정적: `_workspace/sast/105_dll-crt-small.md`
  - dll1 depth 1: `^__acrt_execute_initializers$` · `^__scrt_uninitialize_crt$` · `^__scrt_dllmain_crt_thread_attach$` · `^__scrt_dllmain_crt_thread_detach$` (callee 7개 딸려 옴). `try_get_function_slow` · `dllmain_crt_dispatch` 는 run40 때 뽑은 `.asm` 그대로.
  - 넓힌 것 (depth 0, 결과서가 이유를 적음): 길 A 끝까지 (`__acrt_tls2_supported` · `__acrt_initialize_ptd` · `__acrt_FlsGetValue2` · `__acrt_initialize_winapi_thunks` 등 10), 질문 2 (`__acrt_uninitialize` · `__acrt_execute_uninitializers` · `__vcrt_uninitialize` 등 9), 질문 3 (`__acrt_thread_attach/detach` · `__vcrt_thread_attach/detach` · `__acrt_getptd_noexit` · `internal_get_ptd_head_slow` · `__acrt_freeptd` · `__vcrt_freeptd_for_this_thread` · `destroy_fls` · `__vcrt_initialize_ptd` 등).
  - dll2: 같은 이름 depth 0 — 오프셋 대조만. 31개 함수의 `.asm` 이 주소만 빼고 **명령 · 안쪽 오프셋까지 같다** (결과서).
- 결과서: `_workspace/sast/105_dll-crt-small_result.md` (**분석성공** — 못 정한 것: ntdll 로더 (`LdrLoadDll` · `LdrGetProcedureAddressForCaller`) 안의 syscall 수, 힙이 커널로 가는 때).
- flow-writer 가 다시 본 것 (`.asm` · `_calls.tsv`):
  - dll1: `__acrt_initialize_ptd` (`+0x4` ~ `+0x3d` 전부), `__acrt_tls2_supported` (`+0x4` ~ `+0x32` 전부), `__acrt_initialize_winapi_thunks` (전부), `__vcrt_initialize_ptd` (전부), `_calls.tsv` 의 `__scrt_initialize_crt` · `__acrt_getptd_noexit` · `internal_get_ptd_head_slow` · `try_get_function_slow` 줄.
  - kernelbase: `VirtualProtect` (전부), `FlsAlloc` · `FlsSetValue` · `FlsGetValue` · `FlsFree` 의 호출 줄.
  - dll2 `.asm` 의 `.data` 칸 주석 (`__vcrt_flsindex` · `__acrt_flsindex` · `is_initialized_as_dll` · `__acrt_use_tls2_apis` · `module_handles` 가 dll1 과 같은 rva).
  - `alias:` 하나 — `__acrt_getptd_noexit +0x3f` · `+0x72` 의 callee 는 "이 스레드의 acrt 칸을 처음 만드는" 함수다. 같은 주소 이름 2개, 후보: `internal_get_ptd_head_slow` · `?internal_get_ptd_head_slow@@YAPEAU__acrt_ptd@@XZ` (C++ 꾸민 이름 — `.asm` 주석 `ALIASES(1)`). 이 문서에선 앞 이름을 쓴다.

## 결과

비유 (연장통 = DLL, 선반 = 모듈 핸들 캐시, 공구 목록 = `.fptable` 함수 포인터 표):

- CRT 가 "이 연장 (`FlsGetValue2`) 있나요?" 하고 연장통을 연다. 첫 후보 이름표는 `api-ms-win-core-fibers-l1-1-2` 인데, 실제로 열리는 통은 이미 집에 있는 `kernelbase` 다. 그 통에서 꺼낸 연장은 "ntdll 에 있음" 쪽지 (forwarder) 라서, 공구 목록에 적히는 건 ntdll 연장 주소다.
- 공구 목록은 평소엔 **유리 덮개 (읽기 전용)** 가 씌워져 있다. 한 줄 적을 때마다 덮개를 열고 (`VirtualProtect` 4) 닫는다 (`VirtualProtect` 2). 덮개를 여닫는 일은 커널이 해 준다 → `NtProtectVirtualMemory`.

### 정적 — 호출 지점 (`ms/disassemble/dll1/_calls.tsv` · `.asm`, dll2 같은 오프셋)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `dll1!dllmain_dispatch` | `+0x76` | `+0x7b` | `dllmain_crt_dispatch` | 이유 1 · 2 (`DllMain` 앞) | [정적] (run40 그대로) |
| `dll1!dllmain_crt_dispatch` | `+0x42` | — (꼬리 `JMP`) | `dllmain_crt_process_attach` | 이유 1 (`+0x8 SUB 1` · `+0xb JZ +0x3b`) | [정적] (run40 그대로) |
| `dll1!dllmain_crt_process_attach` | `+0x12` | `+0x17` | `__scrt_initialize_crt` (`ECX = 0`, `+0x10 XOR ECX,ECX`) | | [정적] |
| `dll1!__scrt_initialize_crt` | `+0x14` | `+0x19` | `__vcrt_initialize` | `__vcrt_initialize` 본문은 안 뽑음 | [정적] (`_calls.tsv`) |
| `dll1!__scrt_initialize_crt` | `+0x1d` | `+0x22` | `__acrt_initialize` | | [정적] |
| `dll1!__acrt_initialize` | `+0xe` | — (꼬리 `JMP`) | `__acrt_execute_initializers(표 rva 0x6fd50, 끝 0x6fe50)` | | [정적] |
| `dll1!__acrt_execute_initializers` | `+0x25` | `+0x2a` | 표 항목 시작 함수 (CFG 간접 `_guard_dispatch_icall`) | 16칸, 2번 = `__acrt_initialize_winapi_thunks`, **7번 = `__acrt_initialize_ptd`** (test.exe 의 "8번째 쌍" 과 같음). 실패하면 `+0x56` (ret `+0x5b`) 에서 거꾸로 끝 함수 | [정적] |
| `dll1!__acrt_initialize_winapi_thunks` | `+0x20` | `+0x26` | `VirtualProtect(.fptable, 0x100, 2, &old)` | **조건 없음** — 표 2번이라 ptd 보다 먼저. 2 = 읽기 전용 | [정적] |
| `dll1!__acrt_initialize_ptd` | `+0x4` | `+0x9` | `__acrt_tls2_supported` | 결과 → `+0x10` `__acrt_use_tls2_apis` (rva `0x831c0`) | [정적] |
| `dll1!__acrt_initialize_ptd` | `+0x16` | `+0x1b` | `__acrt_FlsAlloc(destroy_fls)` (콜백 `+0x9 LEA RCX,[rva 0x49320]`) | 결과 → `+0x1b` `__acrt_flsindex` (rva `0x82160`) | [정적] |
| `dll1!__acrt_initialize_ptd` | `+0x2a` | `+0x2f` | `__acrt_getptd_noexit` | 첫 스레드의 acrt 칸 만들기 (아래 질문 3 과 같은 함수) | [정적] |
| `dll1!__acrt_tls2_supported` | `+0x32` | `+0x37` | `try_get_function_slow(ECX = 3, RDX = "FlsGetValue2", R8/R9 = 후보 id [1, 19])` | 칸 `.fptable+0x18` (`+0x4` 읽기) 이 0 일 때만. `-1` 이면 실패 처리, 0 아닌 값이면 이미 푼 주소 | [정적] |
| `dll1!try_get_function_slow` | `+0x6d` | `+0x73` | `LoadLibraryExW(L"api-ms-win-core-fibers-l1-1-2", NULL, 0x800)` | 모듈 캐시 `module_handles[1]` (rva `0x834a0 + 8`) 가 비었을 때. `+0x62 XOR EDX,EDX` · `+0x67 MOV R8D,0x800` | [정적] (call 자리 run40 그대로) |
| `dll1!try_get_function_slow` | `+0xc1` | `+0xc7` | `LoadLibraryExW(이름, NULL, 0)` | 실패 · 에러 `0x57` · 이름이 `api-ms-` (`+0x9a`) 도 `ext-ms-` (`+0xb0`) 도 아닐 때만 → 길 A 첫 후보는 **안 탐** | [정적] |
| `dll1!try_get_function_slow` | `+0xf9` / `+0x15a` | `+0xfe` / `+0x15f` | `__acrt_lock(0xe)` / `__acrt_unlock` (= `EnterCriticalSection` / `Leave…`) | 안 막히면 커널 안 감 `[추측: 혼자 돌 때]` | [정적] |
| `dll1!try_get_function_slow` | `+0x11d` | `+0x123` | `VirtualProtect(.fptable, 0x100, 4)` | 4 = 읽기+쓰기. 찾든 못 찾든 (`+0xf0` 뒤) | [정적] |
| `dll1!try_get_function_slow` | `+0x13c` | — | `XCHG [.fptable + id*8], 주소 (없으면 -1)` | 호출 아님 — **값이 바뀌는 순간** | [정적] |
| `dll1!try_get_function_slow` | `+0x14e` | `+0x154` | `VirtualProtect(…, 2)` | 다시 읽기 전용 | [정적] |
| `dll1!try_get_function_slow` | `+0x19e` | `+0x1a4` | `GetProcAddress(모듈, "FlsGetValue2")` | 첫 후보가 열리면 곧바로 → `+0x1a7 JMP +0xf2` (둘째 후보 `kernelbase` 로는 안 감) | [정적] |
| `KERNELBASE!VirtualProtect` | `+0x34` | `+0x3b` | `NtProtectVirtualMemory(-1, …)` | 보통은 이 한 번. `+0x96` (ret `+0x9d`) 둘째 호출은 첫 결과가 `0xc0000045` 이고 `+0x6d RtlFlushSecureMemoryCache` 가 참일 때만 (`+0x5c CMP` · `+0x61 JNZ` · `+0x7b JZ`) | [정적] (flow-writer `.asm`) |
| `KERNEL32!VirtualProtect` | — | — | 7바이트 스텁 `JMP [IAT 0x8b690]` → `api-ms-win-core-memory-l1-1-0` → `KERNELBASE!VirtualProtect` | ApiSet 기본 호스트 `kernelbase.dll` | [정적] (결과서) |
| `KERNELBASE!LoadLibraryExW` | `+0x130` / `+0xf8` | `+0x137` / `+0xff` | `LdrGetDllPath` / `LdrLoadDll` | 안쪽 ntdll syscall 수는 안 읽음 | [정적] (결과서) |
| `KERNELBASE!GetProcAddress` | `+0x7b` | `+0x82` | `LdrGetProcedureAddressForCaller` | 안쪽은 안 읽음 | [정적] (결과서) |
| `KERNELBASE!FlsAlloc` / `FlsSetValue` / `FlsGetValue` / `FlsFree` | `+0xe` / `+0x4` / `+0x1f` / `+0x4` | `+0x15` / `+0xb` / `+0x26` / `+0xb` | `ntdll!RtlFlsAlloc` / `RtlFlsSetValue` / `RtlFlsGetValue` / `RtlFlsFree` | 얇은 겉포장 (실패면 `BaseSetLastNTError`) | [정적] (flow-writer `.asm`) |
| `dll1!__acrt_getptd_noexit` | `+0x2e` | `+0x33` | `__acrt_FlsGetValue2` (= `MOV RAX,[.fptable+0x18]` · `JMP RAX` → `ntdll!RtlFlsGetValue2`) | `__acrt_use_tls2_apis` == 1 일 때 (`+0x16 CMP` · `+0x1d JZ +0x49`) | [정적] |
| `dll1!__acrt_getptd_noexit` | `+0x3f` / `+0x72` | `+0x44` / `+0x77` | 이 스레드의 acrt 칸을 처음 만드는 함수 (같은 주소 이름 2개, 후보 `internal_get_ptd_head_slow` · 꾸민 이름) | 읽은 값이 0 일 때. `+0x72` 는 옛 길 (`+0x61 __acrt_FlsGetValue` 뒤) | [정적] |
| `dll1!internal_get_ptd_head_slow` | `+0x21` · `+0x42` · `+0x68` · `+0x8b` | `+0x26` · `+0x47` · `+0x6d` · `+0x90` | `__acrt_FlsSetValue(idx, -1)` · `_calloc_base(1, 0x3c8)` · `__acrt_FlsSetValue(idx, 덩어리)` · `construct_ptd_array` | | [정적] |
| `dll1!__vcrt_initialize_ptd` | `+0xb` · `+0x25` | `+0x11` · `+0x2b` | `FlsAlloc(__vcrt_freefls)` → `__vcrt_flsindex` (rva `0x82140`) · `FlsSetValue(idx, &__vcrt_startup_thread_ptd)` (rva `0x82d80`) | 부르는 쪽은 `__vcrt_initialize` 쪽일 것 `[추측: 본문 안 뽑음]` | [정적] (flow-writer `.asm`) |
| `dll1!dllmain_dispatch` | `+0xe8` | `+0xed` | `dllmain_crt_dispatch` | 이유 0 · 3 (`DllMain` `+0x91` 뒤) | [정적] (run40 그대로) |
| `dll1!dllmain_crt_dispatch` | `+0x51` | — (꼬리 `JMP`) | `dllmain_crt_process_detach` (`CL` = `lpReserved ≠ 0`) | 이유 0 (`+0x6 JZ +0x47` · `+0x47 TEST R8,R8` · `+0x4a SETNZ CL`) | [정적] |
| `dll1!dllmain_crt_process_detach` | `+0x68` | `+0x6d` | `__scrt_uninitialize_crt(ECX = CL, EDX = 0)` | `+0x62 XOR EDX,EDX` · `+0x64 MOVZX ECX,DIL`. 부르는 곳은 이것 하나 (`refs` 1줄) | [정적] |
| `dll1!__scrt_uninitialize_crt` | `+0x16` / `+0x1e` | `+0x1b` / `+0x23` | `__acrt_uninitialize(CL)` / `__vcrt_uninitialize(CL)` | `+0x6 ~ +0x14`: `is_initialized_as_dll ≠ 0` **이고** `DL ≠ 0` 일 때만 건너뜀 → dll 길 (`DL = 0`) 은 늘 탐 | [정적] |
| `dll1!__acrt_uninitialize` | `+0x12` / `+0x30` | `+0x17` / — (꼬리 `JMP`) | `_flushall` / `__acrt_execute_uninitializers` | `CL = 1` 이고 `__piob ≠ 0` 이면 `_flushall` 만 · `CL = 0` 이면 꼬리 `JMP` (아래 질문 2) | [정적] |
| `dll1!dllmain_crt_dispatch` | `+0x2e` / `+0x21` | `+0x33` / `+0x26` | `__scrt_dllmain_crt_thread_attach` / `__scrt_dllmain_crt_thread_detach` | 이유 2 (`+0x10 JZ +0x2e`) / 이유 3 (`+0x15 JZ +0x21`) | [정적] |
| `ntdll!LdrShutdownProcess` | `+0x17a` | `+0x17f` | `LdrpCallInitRoutine(…, R9 = R13 = 1)` | `+0x57 LEA R13D,[R12+1]` · `+0x16d MOV R9,R13` → 프로세스 끝 `lpReserved = 1` | [정적] (결과서, 있던 `.asm`) |
| `ntdll!LdrpProcessDetachNode` | `+0x143` | `+0x148` | `LdrpCallInitRoutine(…, R9 = 0)` | `+0x136 XOR R9D,R9D` → `FreeLibrary` 길 `lpReserved = 0` | [정적] (결과서, 있던 `.asm`) |

### 호출 관계

```
(이유 1 — dll2 먼저, dll1 나중. 은퇴 전 P1b~P2)
ntdll LdrpCallInitRoutineInternal+0x22 → dllN!_DllMainCRTStartup ══JMP══▶ dllmain_dispatch            (run40)
dllmain_dispatch
 └─ +0x7b  dllmain_crt_dispatch ══JMP (+0x42)══▶ dllmain_crt_process_attach
            └─ +0x17  __scrt_initialize_crt(0)
                       ├─ +0x19  __vcrt_initialize       … __vcrt_initialize_ptd  +0x11 FlsAlloc · +0x2b FlsSetValue  [추측: 사이]
                       └─ +0x22  __acrt_initialize ══JMP (+0xe)══▶ __acrt_execute_initializers
                                  └─ +0x2a  표 항목 (CFG 간접)
                                       ├─ [2] __acrt_initialize_winapi_thunks
                                       │        └─ +0x26  VirtualProtect(.fptable, 2) → KERNELBASE +0x3b NtProtectVirtualMemory   ①
                                       └─ [7] __acrt_initialize_ptd
                                                ├─ +0x9   __acrt_tls2_supported
                                                │          └─ +0x37  try_get_function_slow(3, "FlsGetValue2")
                                                │                     ├─ +0x73   LoadLibraryExW("api-ms-win-core-fibers-l1-1-2", 0x800) → KERNELBASE +0xff LdrLoadDll
                                                │                     ├─ +0xfe   __acrt_lock(0xe)
                                                │                     ├─ +0x123  VirtualProtect(4) → NtProtectVirtualMemory   ②
                                                │                     │  +0x13c  XCHG .fptable+0x18 ← ntdll!RtlFlsGetValue2
                                                │                     ├─ +0x154  VirtualProtect(2) → NtProtectVirtualMemory   ③
                                                │                     ├─ +0x15f  __acrt_unlock
                                                │                     └─ +0x1a4  GetProcAddress("FlsGetValue2") → KERNELBASE +0x82 LdrGetProcedureAddressForCaller
                                                │  +0x10  __acrt_use_tls2_apis ← 1
                                                ├─ +0x1b  __acrt_FlsAlloc(destroy_fls) → KERNELBASE FlsAlloc +0x15 RtlFlsAlloc
                                                └─ +0x2f  __acrt_getptd_noexit → +0x33 RtlFlsGetValue2 (0) → +0x44 internal_get_ptd_head_slow
                                                                                    (FlsSetValue · _calloc_base(0x3c8) · FlsSetValue)
 └─ +0x96  DllMain(1)

(이유 0 — 프로세스 끝, 지도 밖)
ntdll LdrShutdownProcess +0x17f (R9 = 1) → … → dllmain_dispatch +0xed → dllmain_crt_dispatch ══JMP (+0x51)══▶ dllmain_crt_process_detach (CL = 1)
 └─ +0x6d  __scrt_uninitialize_crt(1, 0)
            ├─ +0x1b  __acrt_uninitialize(1) → +0x17 _flushall (__piob ≠ 0)        ← 정리 표 · FreeLibrary · FlsFree 없음
            └─ +0x23  __vcrt_uninitialize(1) → 아무것도 안 함

(이유 2 · 3 — 새 스레드, 지도 밖)
dllmain_dispatch +0x7b → dllmain_crt_dispatch +0x33 __scrt_dllmain_crt_thread_attach   (DllMain 앞)
                                                    ├─ +0x9   __vcrt_thread_attach → __vcrt_getptd_noexit → calloc(1, 0x80)
                                                    └─ +0x12  __acrt_thread_attach → __acrt_getptd_noexit → (RtlFlsGetValue2) → internal_get_ptd_head_slow
dllmain_dispatch +0xed → dllmain_crt_dispatch +0x26 __scrt_dllmain_crt_thread_detach   (DllMain 뒤)
                                                    ├─ +0x9   __acrt_thread_detach → __acrt_freeptd (FlsSetValue(0) → destroy_ptd_array → _free_base)
                                                    └─ +0xe   __vcrt_thread_detach → __vcrt_freeptd_for_this_thread (FlsSetValue(0) → free)
```

### (a) `FlsGetValue2` 길 — test.exe 와 같은 길 (결과서 1)

- **이름 · 모듈** `[정적: dll1 .rdata 바이트]`:

| 항목 | dll1 rva (dll2) | 값 |
|---|---|---|
| API 이름 (ANSI) | `0x70f60` (`0x70f50`) | `FlsGetValue2` |
| 후보 id 배열 (4B × 2) | `0x70f58` (`0x70f48`) | `[1, 19]` |
| `module_names[1]` (UTF-16) | 표 `0x708b0` (`0x708a0`) + 8 → `0x709b0` | **`api-ms-win-core-fibers-l1-1-2`** |
| `module_names[19]` | `0x70e60` | **`kernelbase`** |
| 모듈 핸들 캐시 `module_handles` | `0x834a0` (같음, id × 8) | 파일 값 0 |
| 함수 포인터 표 `.fptable` | `0x8a000` (같음, `0x100`B, 구역 속성 읽기+쓰기, `+0x18` = id 3) | 파일 값 0 |

  - test.exe 와 id · 이름이 같다 (sast/25 표, run30 2.10 길 A). 요청서가 짐작한 `kernel32` · `fibers-l2` 는 **아니다.**
- **풀리는 곳** `[정적: PE 표]`: `api-ms-win-core-fibers-l1-1-2` → (24H2 `apisetschema.dll` `.apiset` 기본 호스트) **`kernelbase.dll`** → `KERNELBASE!FlsGetValue2` = forwarder **`NTDLL.RtlFlsGetValue2`** → `GetProcAddress` 가 돌려주는 값 = `ntdll!RtlFlsGetValue2` (rva `0xd6930`). 이 함수는 `gs:[0x17c8]` (= TEB `FlsData`) 를 읽고 칸을 찾기만 한다 — 호출 · syscall 없음 `[정적: objdump]`.
- **풀린 주소가 쓰이는 곳**: `__acrt_initialize_ptd +0x10` 이 결과 (참/거짓) 를 `__acrt_use_tls2_apis` 에 적고, `__acrt_getptd_noexit +0x16` 이 그것을 보고 `+0x2e` 에서 `__acrt_FlsGetValue2` (`.fptable+0x18` 로 `JMP`) 를 부른다.
- **`try_get_FlsGetValue2` (`0x4eedc`) · `get_FlsGetValue2`**: 같은 일을 하는 사본인데 부르는 곳이 없다 (`refs` 0줄). 진짜 길은 `__acrt_tls2_supported` 쪽.
- **은퇴 전 syscall** `[정적]`:
  - dll CRT 코드에는 `syscall` 이 **없다** (dll1 `.asm` grep 0).
  - 나는 곳은 부르는 쪽이다: `VirtualProtect` → `KERNELBASE!VirtualProtect +0x34` → **`NtProtectVirtualMemory`** — 위 트리 ① ② ③, **dll 마다 적어도 3번**, 보호 값 차례 `2 → 4 → 2`, 인자 `.fptable` (dll 베이스 + `0x8a000`, `0x100`B).
  - `try_get_function_slow` 는 API 하나를 풀 때마다 (찾든 못 찾든) `VirtualProtect` 를 두 번 탄다. dll 이 `DllMain` 앞에서 세 길 (A `FlsGetValue2` · B `LCMapStringEx` · C `AreFileApisANSI`) 을 다 타면 1 + 2×3 = **7번쯤** `[정적 + 추측: 세 길이 dll 에서 정말 세 번 타는지는 동적 Q31]`.
  - `LoadLibraryExW` · `GetProcAddress` 가 ntdll 안에서 내는 syscall 수는 **안 읽음** → 확인 필요 (이미 올라온 `kernelbase` 라 0 이어도 이상하지 않다 `[추측]`).
  - `__acrt_lock` 은 임계 구역이라 혼자 돌 땐 커널 안 감 `[추측]`.
- **플래그**: `LoadLibraryExW` 셋째 인자 **`0x800`** (`LOAD_LIBRARY_SEARCH_SYSTEM32`) — run40 과 같다. 첫 후보는 `api-ms-` 로 시작해서 실패해도 **플래그 0 재시도 (`+0xc1`) 를 안 탄다.** 둘째 후보 `kernelbase` 는 재시도 갈래가 열려 있다.

### (b) 이유 0 (DETACH) — 갈림은 `CL` 하나 (결과서 2)

- `__scrt_uninitialize_crt` (rva `0x23c0`, 43B) 안 `DL` (`from_exit`) 가림은 dll 길에선 **안 갈린다**: 건너뛰기 조건은 `is_initialized_as_dll ≠ 0` (rva `0x82d20`, `__scrt_initialize_crt +0x8` 이 `ECX = 0` 일 때 켬 → dll 길에선 1) **이고** `DL ≠ 0` 인데, dll 길은 `DL = 0` 이다. `DL ≠ 0` 길은 이 dll 의 `exit` / `_cexit` 쪽일 것 `[추측]`.
- 진짜 갈림 = `CL` (`lpReserved ≠ 0`):

| `CL` | 언제 | `__acrt_uninitialize` (rva `0x21b14`) | `__vcrt_uninitialize` (rva `0x32b0`) |
|---|---|---|---|
| **1** | 프로세스 끝 — `LdrShutdownProcess +0x17a` 가 `R9 = 1` | `__piob` (rva `0x839c0`) ≠ 0 이면 **`_flushall` 만** (`+0x12`), `AL = 1`. 정리 표 안 돎 | `+0x6 JNZ +0x12` → 아무것도 안 함 |
| **0** | `FreeLibrary` — `LdrpProcessDetachNode +0x136 XOR R9D,R9D` | `+0x30` 꼬리 `JMP __acrt_execute_uninitializers` — 표 끝에서 거꾸로 끝 함수 (15 `uninitialize_c` … 7 `__acrt_uninitialize_ptd` (`FlsFree(__acrt_flsindex)`) … 2 `__acrt_uninitialize_winapi_thunks` (`module_handles` 칸마다 `FreeLibrary`)) | `__vcrt_uninitialize_ptd` (`FlsFree(__vcrt_flsindex)`) · `__vcrt_uninitialize_locks` |

- sample 은 dll 을 **정적으로 import** 하니 `main` 이 끝날 때 `CL = 1` 쪽만 탄다 `[정적 + 소스]`.
- `__piob` ≠ 0 인가: `__piob` 를 쓰는 곳은 `__acrt_initialize_stdio` (rva `0x52060`) 하나 (`_refs/0x839c0.tsv`), 그 함수는 `.CRT$XI` 표 (`.CRT$XIA` rva `0x6e320`, 둘째 항목 `0x6e330`) 에 있어 `dllmain_crt_process_attach +0x6b` (ret `+0x70`) `_initterm_e` 가 부른다 → **`_flushall` 이 불린다** `[정적]`. 스트림에 쓴 게 없어 `WriteFile` 로 이어지는지는 안쪽을 안 봐서 모른다 (`Log` 는 CRT 스트림을 안 씀 — `sample/log.h` `[소스]`).

### (c) 이유 2 · 3 — FLS 칸 + 힙 덩이, syscall 없음 (결과서 3)

- `dllmain_crt_dispatch` (rva `0x1990`, 86B) 가름: 0 → `+0x51` 꼬리 `JMP` detach · 1 → `+0x42` 꼬리 `JMP` attach · **2 → `+0x2e` thread_attach** · **3 → `+0x21` thread_detach** · 그 밖 1.
  - 다른 부르는 곳 `_CRT_INIT +0x21` · `+0x2e` 는 `_CRT_INIT` (rva `0x1d00`) 을 부르는 곳이 없어 안 쓰는 사본 (`refs` 0줄, `.pdata` 만).
  - 순서: 이유 2 는 `DllMain` **앞** (`dllmain_dispatch +0x76`, 0 이 오면 `+0x83 JZ +0x11e` 로 `DllMain` 건너뜀), 이유 3 은 `DllMain` (`+0x91`) **뒤** (`+0xe8`).
  - sample `DllMain` 은 `DisableThreadLibraryCalls` 를 안 부른다 (`DllMain.asm`) → `CreateThread` 1개면 이유 2 · 3 이 온다 `[정적 + 소스]`.
- **이유 2 `__scrt_dllmain_crt_thread_attach`** (rva `0x2110`, 41B): `+0x4 __vcrt_thread_attach` → `__vcrt_getptd_noexit` (`__vcrt_flsindex` 칸이 비었으면 `FlsSetValue(idx, -1)` → **`calloc(1, 0x80)`** → `FlsSetValue(idx, 덩어리)` → 덩어리 `+0x78` 에 `0xfffffffe`) → 실패면 `AL = 0`. `+0xd __acrt_thread_attach` → `__acrt_getptd_noexit` (`__acrt_use_tls2_apis` 1 이면 `RtlFlsGetValue2`, 0 이면 `internal_get_ptd_head_slow` → **`_calloc_base(1, 0x3c8)`** + `construct_ptd_array`). acrt 실패면 `+0x16 __vcrt_thread_detach` 로 되돌림.
- **이유 3 `__scrt_dllmain_crt_thread_detach`** (rva `0x2140`, 21B): `+0x4 __acrt_thread_detach` → `__acrt_freeptd` (`FlsGetValue` → 있으면 **`FlsSetValue(idx, 0)` 먼저** → `destroy_ptd_array` → `_free_base`) · `+0x9 __vcrt_thread_detach` → `__vcrt_freeptd_for_this_thread` (`FlsSetValue(idx, 0)` → 시작 스레드용 정적 칸 `__vcrt_startup_thread_ptd` 가 아니면 `free`) · `+0xe MOV AL,1` (**늘 1**).
- **TLS 가 아니라 FLS** 다. `TlsAlloc` 줄 없음. `.tls` 구역 (로더가 채우는 암시적 TLS) 은 안 건드린다. FLS 칸에는 파괴자 콜백이 걸려 있다 — acrt `destroy_fls` (rva `0x49320`), vcrt `__vcrt_freefls` (rva `0x19920`). 이유 3 이 칸을 먼저 비우니 스레드 끝에 ntdll 이 콜백을 불러도 비어 있을 것 `[추측]` (콜백을 언제 부르나는 범위 밖).
- **syscall**: dll 코드엔 없다. 부르는 건 KERNEL32 가져오기 (`GetLastError` · `SetLastError` · `FlsGetValue` · `FlsSetValue` · `HeapAlloc` · `HeapFree`) 와 `FlsGetValue2` 포인터뿐. 한 칸 안쪽도 kernelbase 겉포장 → `ntdll!RtlFls*` 이고, 기존 `ntdll/RtlFlsSetValue.asm` 에 `SYSCALL` 0개 (안에서 `RtlpFlsHeapAlloc` `+0x94` · `+0xc0`, `RtlAcquireSRWLockExclusive` `+0x10b`, `RtlReleaseSRWLockExclusive` `+0x142`, `memset` `+0x176`). 그래서 **힙이 모자라거나 비울 때만 커널로 간다** (`NtAllocateVirtualMemory` / `NtFreeVirtualMemory`) — 실제로 나는지는 동적.

## 확인된 것

- `[정적]` **dll1 · dll2 도 `FlsGetValue2` 를 `try_get_function_slow` 로 찾는다** — 표 7번 `__acrt_initialize_ptd +0x4` → `__acrt_tls2_supported +0x32` → `try_get_function_slow +0x6d LoadLibraryExW(L"api-ms-win-core-fibers-l1-1-2", NULL, 0x800)` → `+0x19e GetProcAddress`. test.exe 길 A (run30) 와 id · 이름 · 순서가 같다.
- `[정적: PE 표]` `api-ms-win-core-fibers-l1-1-2` → `kernelbase.dll`, `KERNELBASE!FlsGetValue2` → `NTDLL.RtlFlsGetValue2`. `.fptable +0x18` 에 들어갈 값 = `ntdll!RtlFlsGetValue2`.
- `[정적]` **은퇴 전 dll 마다 `NtProtectVirtualMemory` 적어도 3번** (`__acrt_initialize_winapi_thunks +0x20` 무조건 1 + `try_get_function_slow +0x11d` · `+0x14e`), `KERNELBASE!VirtualProtect +0x34` 하나로. 세 길을 다 타면 7번쯤 `[추측: 횟수]`.
- `[정적]` 이유 0: 프로세스 끝 (`LdrShutdownProcess` `R9 = 1` → `CL = 1`) = `_flushall` 만 (`__piob ≠ 0` — `.CRT$XI` 의 `__acrt_initialize_stdio` 가 켬). `FreeLibrary` (`CL = 0`) = 정리 표 거꾸로 + `FlsFree` ×2 + `FreeLibrary` (캐시 모듈) + 락 정리. `DL` 가림은 dll 길에서 안 갈림.
- `[정적]` 이유 2 = FLS 칸 둘 (vcrt `0x80`B · acrt `0x3c8`B + 배열) 을 힙에서 받아 넣기, 이유 3 = 칸을 0 으로 비우고 덩이 돌려주기 (늘 1 반환). dll 코드에 syscall 없음.
- `[정적]` dll1 ↔ dll2: 이번에 읽은 31개 함수가 주소만 빼고 같고, 이번에 쓴 `.data` 칸 (`__vcrt_flsindex` `0x82140` · `__acrt_flsindex` `0x82160` · `is_initialized_as_dll` `0x82d20` · `__acrt_use_tls2_apis` `0x831c0` · `module_handles` `0x834a0` · `.fptable` `0x8a000`) 은 두 dll 이 같은 rva. `.rdata` 칸 (이름 · id 배열 · `module_names`) 만 dll2 가 `0x10` 앞.
- `[정적 길 + 관찰 구간]` (flow-writer) TEB `FlsData` (`+0x17c8`) 는 사진 P1b 에서 `(null)` (`ms/logs/04_baggage-read_P1b.txt:2769`), P2 에서 `0x00000259'96f3c160` (`_P2.txt:2781`) — 그 구간 (그래프 켜기) 안에서 처음 `RtlFlsSetValue` 를 부를 수 있는 코드로 이번에 **dll CRT 의 `FlsSetValue` 줄** (`__vcrt_initialize_ptd +0x25`, `internal_get_ptd_head_slow +0x21` 등 → KERNELBASE `FlsSetValue +0x4` → `RtlFlsSetValue`) 이 나왔다. 그중 어느 줄이 첫 번인지 (dll2 의 vcrt 쪽일 것) 는 `[추측]` — 짐 지도 7.17 D3 그대로.

## 결과서와 다르게 적은 곳 · 보탠 것

- **보탬 (flow-writer `.asm`)**: `KERNELBASE!VirtualProtect` 에는 `NtProtectVirtualMemory` 호출 줄이 **둘** (`+0x34` · `+0x96`). 둘째는 첫 결과가 `0xc0000045` 이고 `RtlFlushSecureMemoryCache` 가 참일 때만 → 보통 길은 한 번. 결과서의 "`VirtualProtect` 1번 = syscall 1번" 은 보통 길 기준으로 맞다.
- **보탬**: `__acrt_flsindex` rva `0x82160` (`__acrt_initialize_ptd +0x1b`), vcrt 쪽 `__vcrt_initialize_ptd +0xb FlsAlloc` · `+0x25 FlsSetValue(idx, &__vcrt_startup_thread_ptd)` · `+0x2f` 그 칸 `+0x78` 에 `0xfffffffe` — 결과서가 이름만 적은 첫 스레드 vcrt 칸이 이 줄이다. `__vcrt_initialize_ptd` 를 누가 부르나는 `__vcrt_initialize` 본문을 안 뽑아 `[추측]` (`__scrt_initialize_crt +0x14 __vcrt_initialize` 아래일 것).
- **보탬**: `__acrt_initialize_winapi_thunks` 의 `VirtualProtect` 는 **조건 없음** (`+0x0 ~ +0x20` 갈래 없음).
- **보탬 (run84 와 잇기)**: `__acrt_initializers` 표 8번이 `__acrt_initialize_lowio` 라서 dll2 · dll1 CRT 준비 (은퇴 전) 에서 **불린다** `[정적: 표 순서]`. 그 안이 run84 의 `initialize_stdio_handles_nolock` (`GetStdHandle` · `GetFileType`) 인지는 안쪽을 안 읽어 `[추측]` 그대로.
- 결과서 "방법" 의 `LdrShutdownProcess` · `LdrpProcessDetachNode` 줄은 기존 ntdll `.asm` 을 읽은 것이라 flow-writer 는 다시 안 봤다.

## 정정

- 없음. run40 · run30 · run84 의 줄과 어긋나는 곳이 없었다. (요청서의 `kernel32` / `fibers-l2` 짐작은 문서에 적힌 적이 없는 짐작이라 정정 대상이 아니다.)

## 이전 run 에서 닫은 것

- **Q73** (a) · (b) · (c) 정적 — 모두 답 (결과서 판정 분석성공).
- flow.md 2.8 "test.exe 길 A (`FlsGetValue2`) 는 함수 포인터 표를 거쳐 지도가 못 따라가서 dll 쪽은 안 봤다" → dll 쪽도 길 A `[정적]`.
- flow.md 2.10 "`api-ms-…` 가 24H2 에서 어디로 풀리는지는 정적으로 모름" → 길 A 첫 후보 (`fibers-l1-1-2`) 는 `kernelbase` `[정적: apisetschema]` (같은 CRT · 같은 이름이라 test.exe 에도 그대로). 길 B (`localization-l1-2-1`) 는 그대로 모름.

## 아직 확인 필요

### 동적 (결과서 "동적으로 확인할 것" — 필수 없음. 대기열 번호는 오케스트레이터가 붙임. 멈춤점은 test.exe 조건, `dllN+rva` 는 `lm` 으로 그때 베이스)

1. **(Q31 과 겹침)** `KERNELBASE!LoadLibraryExW` 입구 `poi(@rsp)` = `dll1+0x4f60b` (dll2 `+0x4f5fb`) 일 때 `du @rcx` — 첫 번이 `api-ms-win-core-fibers-l1-1-2` 인가, `r r8d` = `0x800`, 돌려준 `rax` = kernelbase 베이스. `k` 에 `__acrt_tls2_supported+0x37` → `__acrt_initialize_ptd+0x9` → `__acrt_execute_initializers+0x2a` 가 보이면 길 A.
2. **은퇴 전 syscall 수**: 같은 구간에서 `NtProtectVirtualMemory` 가 `VirtualProtect` ret 자리 (dll1 rva `0x509fe` · `0x4f6bb` · `0x4f6ec`; dll2 `0x509ee` · `0x4f6ab` · `0x4f6dc`) 마다 한 번씩, 보호 `2 → 4 → 2`, 주소 `dllN+0x8a000` 크기 `0x100`. `LoadLibraryExW` (`api-ms-…`) 와 `GetProcAddress` 가 ntdll 안에서 내는 syscall 수 (0 일 수 있음).
3. **값 (P2 덤프로 가능 — Q79 에 붙일 후보)**: `dq dll1+0x8a018 L1` = `ntdll!RtlFlsGetValue2` (= ntdll 베이스 + `0xd6930`), `db dll1+0x831c0 L1` = 1, `!vprot dll1+8a000` = `PAGE_READONLY`, `dq dll1+0x834a8 L1` = kernelbase 베이스, `dd dll1+0x82160 L1` · `dd dll1+0x82140 L1` = FLS 색인 (−1 아님), `dq dll1+0x839c0 L1` (`__piob`) ≠ 0. dll2 같은 rva. **이미 있는 사진 덤프 (`ms/dump/03_baggage-snap/` P2) 를 오프라인으로 읽으면 새 실행 없이 닫힐 수 있다** (dll 이미지 `.data` 쪽이 덤프에 있으면).
4. **TEB `FlsData` 첫 호출자** (짐 지도 7.17 D3 과 같은 질문): `bp ntdll!RtlFlsSetValue ".if (poi(@$teb+0x17c8)==0) { k } .else { gc }"` — 위 프레임이 `KERNELBASE!FlsSetValue+0xb` ← `dll2!__vcrt_initialize_ptd+0x2b` 인가.
5. 이유 2 · 3 (새 스레드): `dll1+0x2110` · `dll1+0x2140` 입구 ~ ret 사이 syscall 수 (예상 0), `dll1+0x4985f` (`__acrt_getptd_noexit +0x33`) `rax` = 0, 이유 3 뒤 ntdll 이 `destroy_fls` (`+0x49320`) · `__vcrt_freefls` (`+0x19920`) 를 부르나.
6. 이유 0 (프로세스 끝): `dll1+0x23c0` 입구 `cl` = 1, `__acrt_uninitialize +0x12` (`_flushall`) 가 서나, 그 사이 `NtWriteFile` / `NtFlushBuffersFile`, `__acrt_uninitialize_winapi_thunks` (`dll1+0x50b38`) 가 **안** 서나.

### 정적 (결과서 "다음 요청 후보")

1. **(ntdll)** `LdrLoadDll` 이 ApiSet 이름 (`api-ms-win-core-fibers-l1-1-2`, 확장자 없음, 플래그 `0x800`) 을 이미 올라온 `kernelbase.dll` 로 풀어 돌려줄 때 syscall 이 몇 번인가 (`LdrpLoadDllInternal` · ApiSet 풀기 · 이미 올라온 모듈 찾기 갈래). 질문 1 의 마지막 빈칸, PE_emulator "호출 약속" 에 바로 필요. 같은 질문을 `LdrGetProcedureAddressForCaller` (`GetProcAddress`) 에도.
2. **(ntdll)** `RtlFlsSetValue` · `RtlpFlsHeapAlloc` 이 첫 칸에서 `RtlAllocateHeap` 말고 직접 부르는 것, `RtlFlsAlloc` 이 TEB `FlsData` 를 건드리나, `LdrShutdownThread` · `LdrpInitializeThread` 가 FLS 콜백 (`RtlpFlsDataCleanup` 류) 을 어디서 부르나.
3. **(dll1, 범위 밖이라 낮음)** `__acrt_initialize_stdio` · `_flushall` 안쪽 — 프로세스 끝에 `NtWriteFile` 을 내는 조건. `__acrt_initialize_lowio` 가 `initialize_stdio_handles_nolock` 을 부르는지 (run84 `[추측]` 닫기).
4. **(dll1, 작음)** `__vcrt_initialize` 본문 — `__vcrt_initialize_ptd` 를 부르는 자리 (위 트리 `[추측: 사이]`).

### PE_emulator 쪽 (결과서 "지금 우리 맞나" 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

- (a) **확인 필요** — 보호 syscall 은 엔진이 직접 대답하는 종류라 3번은 괜찮을 것 `[추측]`. dll `_DllMainCRTStartup` (이유 1) "호출 약속" 에 `NtProtectVirtualMemory` ×3 이상과 `LoadLibraryExW` · `GetProcAddress` 의 ntdll 안 syscall 이 들어 있는지는 못 봄. `PE_emulator/Src` 에 `FlsGetValue2` · `RtlFls*` · `fibers-l1` 을 직접 다루는 줄은 grep 0 → 진짜 kernelbase · ntdll 코드가 돌아 `.fptable+0x18` 에 `ntdll!RtlFlsGetValue2` 를 써야 `__acrt_use_tls2_apis` = 1 이 된다.
- (b) **확인 필요** — 프로세스 끝 (`CL = 1`) 은 `FreeLibrary` · `FlsFree` 정리가 없다는 게 정적 답. `155_process-exit` 메모 (`PE_emulator/_workspace/00_planner/155_process-exit.md:114~119`) 가 dll1 · dll2 `PROCESS_DETACH` 를 진짜로 받는다고 적음. `_flushall` 이 `NtWriteFile` 을 내는지는 모름.
- (c) **아직 못 탐** — 새 스레드 TEB `+0x17c8` 이 0 으로 시작해야 `RtlFlsGetValue2` 가 0 을 준다. `NtCreateThreadEx` 가 아직 기본 대답 (`155_process-exit.md:122`, Worker 안 돎) 이라 이 길은 아직 안 나온다.

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (kernelbase · kernel32 · ntdll · dll1/dll2 pdb 줄, 근거 로그 (run109)), 2.8 (절 머리 · `Log` 줄의 lowio `[추측]` 반쯤 · CRT 안쪽 표 아래 run109 절 새로), 2.10 (절 머리 · `try_get_function_slow` 길 A 풀리는 곳), 3장 `test!Worker` (이유 2 · 3 CRT 길 한 줄 · 끝날 때 이유 3), 4장 (2.8 Q73 줄 해결됨 · 2.8 · 2.10 Q31 줄 덧붙임 · run109 새 줄 셋 — 동적 · 정적 · PE_emulator), 5장 (run109 · sast/105).
- baggage.md: 기준 (run109 줄 · 쉰아홉째판), 3.3 `FlsData` 줄 근거 한 마디 (부르는 쪽 dll CRT 길 `[정적]`, 첫 호출자는 `[추측]` 그대로), 1장 메모 1, 새 **7.6.13** (sample dll CRT 칸 — 체크리스트 밖), 새 7.40, 8장 근거 줄 한 줄. **2장 숫자 그대로.**
- 그림: 안 바꿈 (CRT 한 상자 원칙 — 진입점 상자 → `?` `KERNELBASE!LoadLibraryExW ×3` 그대로).

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 중 디버거 칸을 읽는 것은 없다 `[정적: 읽은 줄 안에서]`.
- pitfalls 제안 없음. 결과서의 `pdb_aliases.py` 는 `--lookup dll1.dll 0x49540` 에서 "별칭 무리 아님" 을 돌려줬다 (`.asm` 주석 `ALIASES(1)` 은 C++ 꾸민 이름 하나 — 같은 함수의 두 표기). 도구 문제는 아니다.

# 2026-10-23 run30 — test.exe 진입점 → `main`: `mainCRTStartup` 은 쿠키만 섞고 **`JMP` 로 `__scrt_common_main_seh` 에 넘긴다** (프레임 안 남음). CRT 준비와 `main` 호출(`+0x10a`, ret `+0x10f`)은 거기서 한다. run7 의 `…67723` = **`test!try_get_function_slow +0x73`** (정적)

> **지도 범위 (2026-10-23 사용자 결정, 이 문서를 쓰는 동안)**: "진입점 = 경계, CRT 한 상자". exe 진입점(`AddressOfEntryPoint` = `mainCRTStartup`)은 커널이 헤더 값을 `Rcx` 로 넘길 뿐이고, CRT 는 컴파일러가 넣은 코드(OS 아님)라 nt·ntdll 초기화 지도 밖이다. 아래 CRT 분석은 **참고**로 둔다. 지도(flow.md 그림 2)에 남긴 건 CRT 가 `main` 전에 ntdll 로더를 다시 부르는 것(`LoadLibraryExW` ×3)과 `main` 뿐이다.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: **test.exe** — MS 바이너리가 아니라 sample 빌드다 (`sample/bin/`, `.gitignore`). 사용자가 VM 에서 실제로 돌린 파일을 그대로 올렸다.
  - pdb: 로컬 `sample/bin/test.pdb`, 서명 `C66E7084C5CA4874B1677F6F1C36A0511` (age 1). exe 의 CodeView 서명과 글자까지 같다 (결과서).
  - 이미지 베이스 `0x140000000`, 진입점 rva `0x1d40`. Ghidra analyze 함수 3356개 — `/MT` 라서 UCRT·vcruntime 이 test.exe 안에 들어 있다 `[소스]` (`sample/build.bat` `CFLAGS=… /MT`).
  - dll1·dll2 는 손대지 않았다 (Q55 몫).
- 버전 대조 (결과서 + 01_sprint 기록):
  - rva `0x1d40` 의 PDB 이름 = `mainCRTStartup`. run5 `[관찰]` "진입점 `mainCRTStartup` (RVA `0x1D40`)" 과 같다.
  - run7 `[관찰]` 의 `LoadLibraryExW` 돌아올 주소 `0x7ff70ee67723` 의 아래 16비트 `0x7723` 이 되는 ret 은 test.exe 안 `LoadLibraryExW` 호출 9곳 중 **하나뿐**이다 (rva `0x57723`).
  - 그 rva 로 빼면 베이스가 `0x7ff70ee10000` 이다. run5 `[관찰]` 의 메인 스레드 시작 주소 `0x7ff70ee11d40` − `0x1d40` 도 같은 `0x7ff70ee10000` 이다 (두 로그 값으로 계산).
  - 단 01_sprint 기록으로는 run3 ~ run5 와 run6 ~ run8 이 **따로 빌드한** test.exe 다 (flow.md 5장). 올라온 빌드는 두 기록 모두와 맞는다. 어느 쪽 빌드인지까지는 모른다.

| 함수 | rva | 크기 (`_index.tsv`) | 이번에 본 것 |
|---|---|---|---|
| `test!mainCRTStartup` | `0x1d40` | 18바이트 | 전체 (`.asm` 4줄) |
| `test!__scrt_common_main_seh` | `0x1b00` | 348바이트 | 호출 자리 전부, `main` 앞뒤 |
| `test!main` | `0x1320` | 124바이트 | 부르는 곳만 (본문은 소스가 있음) |
| `test!try_get_function_slow` | `0x576b0` | 433바이트 | 본문 (`LoadLibraryExW` 를 부르는 함수라 예외로 읽음) |
| `test!__scrt_common_main` | `0x1ae0` | 18바이트 | 부르는 곳 없음 (아래) |
| `test!invoke_main` | `0x1cb0` | 54바이트 | 부르는 곳 없음 (아래) |

## 목적

- Q18 (사용자 결정으로 범위를 넓힘): kernel32 `BaseThreadInitThunk` 본문은 run25, CFG 중개가 `JMP` 뿐인 건 run29 로 이미 답했다. 남은 "`BaseThreadInitThunk +0x12` (ret `+0x17`) 간접 호출 대상 → `main`" 을 test.exe 정적으로 본다.
  - 진입점 `0x1d40` 의 이름, 받은 인자를 쓰나.
  - `mainCRTStartup` → `main` 사슬과 `main` 을 부르는 줄.
  - run7 의 `LoadLibraryExW` ×3 을 부른 test.exe 안 함수, 무엇을 로드하나, 왜 세 번인가.
  - test.exe 안 CFG, TLS 콜백 배열.

## 방법

- 정적: `_workspace/sast/25_exe-crt-start.md` — test.exe, `mainCRTStartup` 에서 `main` 까지 사슬 + `LoadLibraryExW` 를 부르는 길 위 함수. depth 0 으로 여섯 번에 나눠 41개.
  - 사슬 위 함수 말고는 이름과 호출 자리만 봤다. 예외: `try_get_function_slow` 본문.
  - CRT 초기화 함수 표(`__acrt_initializers`, `.CRT$XI*`, `.CRT$XC*`)는 지도(`map`)가 못 따라가서, 표 안 함수를 **뿌리로 각각** 지도를 돌려 이었다 (결과서).
  - `LoadLibraryExW` 를 부르는 곳은 IAT 칸 rva `0x6e250` 으로 찾았다 (`refs test.exe 0x6e250`). import 이름으로 찾으면 0줄이 나온다 (결과서 스크립트 문제).
  - PE 헤더·로드 설정·TLS 디렉터리는 결과서가 PE 파일에서 읽었다. DLL 이름 문자열은 원본 바이트(UTF-16LE).
- 결과서: `_workspace/sast/25_exe-crt-start_result.md` (**분석성공**)
- 근거 파일 (레포 밖, 커밋 안 함): `ms/disassemble/test/{mainCRTStartup,__scrt_common_main_seh,try_get_function_slow,…}.asm`, `ms/disassemble/test/_calls.tsv`, `_refs/main.tsv`, `_refs/0x6e250.tsv`, `ms/decomp/test/_index.tsv`.
- 이번에 `.asm`·`_calls.tsv` 와 다시 맞춰 본 줄: `mainCRTStartup +0x4/+0x9`·`+0xd JMP`, `__scrt_common_main_seh +0x14/+0x19`·`+0x5c/+0x61`·`+0x10a/+0x10f`·`+0x171/+0x176`, `try_get_function_slow +0x67`·`+0x6d/+0x73`·`+0xc1/+0xc7`·`+0x19e`, 길 A·B·C 의 call/ret 전부, `_refs/main.tsv` 3줄. 어긋난 곳 없음.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/test/_calls.tsv`, 대표만)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 (요약) | 표시 |
|---|---|---|---|---|---|
| `test!mainCRTStartup` | `+0x4` | `+0x9` | `test!__security_init_cookie` | 늘. 인자 없음 | [정적] |
| `test!mainCRTStartup` | `+0xd` `JMP` | (꼬리) | `test!__scrt_common_main_seh` (`+0x0`) | 늘. 앞 `+0x9 ADD RSP,0x28` 로 자기 프레임을 걷음 | [정적] |
| `test!__scrt_common_main_seh` | `+0x14` | `+0x19` | `test!__scrt_initialize_crt(1)` | 늘 (길 A·B 의 뿌리) | [정적] |
| `test!__scrt_common_main_seh` | `+0x29` | `+0x2e` | `test!__scrt_acquire_startup_lock` | | [정적] |
| `test!__scrt_common_main_seh` | `+0x5c` | `+0x61` | `test!_initterm_e` (`.CRT$XIA`~`XIZ` 표) | 길 C 의 뿌리 | [정적] |
| `test!__scrt_common_main_seh` | `+0x7d` | `+0x82` | `test!_initterm` (`.CRT$XCA`~`XCZ` 표) | | [정적] |
| `test!__scrt_common_main_seh` | `+0x99` | `+0x9e` | `test!__scrt_release_startup_lock` | | [정적] |
| `test!__scrt_common_main_seh` | `+0x9e` | `+0xa3` | `test!__scrt_get_dyn_tls_init_callback` | 값 0 이면 `+0xaa` 에서 다음 호출을 건너뜀 | [정적] |
| `test!__scrt_common_main_seh` | `+0xc5` | `+0xcb` | 위 콜백 (CFG 간접 `CALL [__guard_dispatch_icall_fptr]`) | 콜백 ≠ 0. 시작값 0 이라 안 탈 것 | [정적] |
| `test!__scrt_common_main_seh` | `+0xed`·`+0xf5`·`+0xfd` | `+0xf2`·`+0xfa`·`+0x102` | `_get_initial_narrow_environment`·`__p___argv`·`__p___argc` | `main` 인자 준비 | [정적] |
| `test!__scrt_common_main_seh` | **`+0x10a`** | **`+0x10f`** | **`test!main`** | 늘 | [정적] |
| `test!__scrt_common_main_seh` | `+0x111` | `+0x116` | `test!__scrt_is_managed_app` | `main` 뒤 | [정적] |
| `test!__scrt_common_main_seh` | `+0x171` | `+0x176` | `test!exit(main 반환값)` | 관리 코드 앱이 아니면 (`_cexit` `+0x11f` 는 관리 앱일 때) | [정적] |
| `test!try_get_function_slow` | **`+0x6d`** | **`+0x73`** | `KERNEL32!LoadLibraryExW(이름, NULL, 0x800)` (IAT `0x6e250`) | 모듈 캐시가 비었을 때 | [정적] + run7 돌아올 주소 `…67723` [관찰] 과 같은 자리 |
| `test!try_get_function_slow` | `+0xc1` | `+0xc7` | `LoadLibraryExW(이름, NULL, 0)` | 위가 실패, 에러 `0x57`, 이름이 `api-ms-`·`ext-ms-` 아님 | [정적] |
| `test!try_get_function_slow` | `+0x19e` | `+0x1a4` | `GetProcAddress(모듈, API 이름)` | 모듈을 얻었을 때 | [정적] |

- `0x800` 은 `LOAD_LIBRARY_SEARCH_SYSTEM32` 다 (결과서). 시스템 폴더에서만 찾으라는 뜻.
- 모듈 이름 주의: import 는 **`KERNEL32!LoadLibraryExW`** (`[정적]`, `KERNEL32.dll` import, IAT `0x6e250`), run7 `[관찰]` 과 아래 호출 관계 트리의 이름은 **`KERNELBASE!LoadLibraryExW`** 다. 둘의 연결(kernel32 export 가 forwarder 인가, 스텁인가)은 근거가 없다 → `[확인 필요]` Q60.

### 정적 — `main` 을 부르는 곳은 하나뿐

- `_refs/main.tsv`: `main` 을 가리키는 곳은 셋이다.
  - `__scrt_common_main_seh +0x10a` `CALL` — 이게 실제 길.
  - `invoke_main +0x31` `JMP` — 그런데 **`invoke_main` 을 부르는 곳이 없다** (`.pdata` 참조 하나뿐).
  - `.pdata` (함수 범위 표).
- `__scrt_common_main` (rva `0x1ae0`) 도 `mainCRTStartup` 과 똑같은 18바이트인데 부르는 곳이 없다.
- 그래서 둘 다 CRT 라이브러리에 같이 딸려 온 **안 쓰는 사본**으로 본다. 왜 남았는지는 `[추측]` (`invoke_main` 이 `__scrt_common_main_seh` 안으로 합쳐지고 단독 사본만 남음).

### 정적 — `LoadLibraryExW` 를 부르는 자리 9곳 (`_refs/0x6e250.tsv`)

| 함수 | call | ret (rva) | 아래 16비트 |
|---|---|---|---|
| `try_get_first_available_module` | `+0x60` / `+0xb3` | `0x575d2` / `0x57625` | `75d2` / `7625` |
| **`try_get_function_slow`** | **`+0x6d`** / `+0xc1` | **`0x57723`** / `0x57777` | **`7723`** / `7777` |
| `try_get_module` | `+0x55` / `+0xa5` | `0x578bf` / `0x5790f` | `78bf` / `790f` |
| `try_get_proc_address_from_first_available_module` | `+0x68` / `+0xbb` | `0x579ce` / `0x57a21` | `79ce` / `7a21` |
| `try_load_library_from_system_directory` | `+0x11` (`+0x64` 는 `JMP`) | `0x57aa3` | `7aa3` |

- run7 의 `…7723` 과 맞는 건 `try_get_function_slow +0x73` 하나뿐이다. 베이스가 64KB 단위로 놓이니 아래 16비트가 rva 와 같아야 한다.
- run7 표는 세 번을 한 줄 "`LoadLibraryExW` ← `0x7ff70ee67723`" 로 적었다. 세 번 모두 그 자리였다면 `+0xc1` (ret `0x57777`, 다시 시도) 길은 안 탔다 `[추측: run7 원본 로그가 레포에 없어 요약 표로만 판단]`.

### 정적 — `try_get_function_slow` 가 하는 일

비유: 가게(UCRT)가 "이 연장(API) 있나요?" 할 때 **연장통(DLL) 후보 목록**을 앞에서부터 열어 보는 함수다. 한 번 연 연장통은 선반(캐시)에 올려 두고 다시 안 연다.

1. 후보 모듈 id 목록을 앞에서부터 돈다.
2. 모듈 핸들 캐시(`module_handles[id]`, rva `0x83ef0`)가 비었으면 `+0x6d` `LoadLibraryExW(module_names[id], NULL, 0x800)`.
3. 성공하면 캐시에 넣고 `+0x19e` `GetProcAddress`. **그 모듈에 API 가 없어도 다음 후보로 안 간다** (`+0x1a7 JMP`).
4. 실패하면 (에러 `0x57` 이고 `api-ms-`/`ext-ms-` 이름이 아니면 `+0xc1` 에서 플래그 0 으로 한 번 더) 캐시에 `-1` 을 넣고 다음 후보로.
5. 찾은 주소(없으면 `-1`)를 함수 포인터 표 섹션 **`.fptable`** (rva `0x8a000`) 에 적는다. 쓰기 앞뒤로 `VirtualProtect`. **CFG 와 상관없는 이름이다.**

| API | 후보 모듈 (순서대로) |
|---|---|
| `FlsGetValue2` | `api-ms-win-core-fibers-l1-1-2` → `kernelbase` |
| `LCMapStringEx`, `LocaleNameToLCID` | `api-ms-win-core-localization-l1-2-1` → `kernel32` |
| `AreFileApisANSI` | `kernel32` 하나뿐 |

- 이름 문자열은 결과서가 원본 바이트(UTF-16LE)로 읽었다. `module_names[]` 는 rva `0x729e0`.
- `api-ms-…` 이름이 24H2 에서 실제로 어느 DLL 로 풀리는지, 첫 시도에 되는지는 정적으로 모른다 (Q31).

### 정적 — `main` 전에 `try_get_function_slow` 로 가는 세 길

세 길 모두 `__scrt_common_main_seh` 의 `+0x19` 또는 `+0x61` 아래다. 괄호는 ret.

```
__scrt_common_main_seh
 ├─ +0x19  __scrt_initialize_crt
 │          └─ +0x22  __acrt_initialize
 │                     ══▶ +0xe JMP (꼬리, 프레임 없음)  __acrt_execute_initializers
 │                          └─ +0x2a  표 항목 CFG 간접 호출 (_guard_dispatch_icall = jmp rax, 프레임 없음)
 │                               ├─ [표 8번째]  __acrt_initialize_ptd
 │                               │     └─ +0x9  __acrt_tls2_supported
 │                               │           └─ +0x37  try_get_function_slow  ── 길 A (FlsGetValue2)
 │                               └─ [표 11번째] __acrt_initialize_multibyte
 │                                     └─ +0x52  setmbcp_internal
 │                                           └─ +0x16d  _setmbcp_nolock
 │                                                 └─ +0x296  setSBUpLow
 │                                                       └─ +0x108  __acrt_LCMapStringA
 │                                                             └─ +0x71  __acrt_LCMapStringA_stat
 │                                                                   └─ +0x169  __acrt_LCMapStringEx
 │                                                                         └─ +0x4f  try_get_function_slow  ── 길 B (LCMapStringEx)
 └─ +0x61  _initterm_e
            └─ +0x22  표 항목 CFG 간접 호출 (.CRT$XI 첫 항목)
                 └─ pre_c_initialization
                       └─ +0x55  _configure_narrow_argv
                             └─ +0x5a  __acrt_GetModuleFileNameA
                                   └─ +0xb2  __acrt_AreFileApisANSI
                                         └─ +0x32  try_get_function_slow  ── 길 C (AreFileApisANSI)
                                               └─ +0x73  LoadLibraryExW
```

- 길 B 의 `__acrt_initialize_multibyte` 는 깃발(rva `0x83864`)이 있어 **처음 한 번만** 일한다. 표 쪽이 `_initterm_e` 보다 먼저라 B 가 그 한 번일 가능성이 높다 `[추측]`.
  - `_configure_narrow_argv +0x3e` (ret `+0x43`) 도 `__acrt_initialize_multibyte` 를 부르지만, 깃발 때문에 두 번째는 일을 안 할 것 `[추측]`.
- 길 B 에서 API 주소가 없으면 `__acrt_LCMapStringEx +0xb0` → `__acrt_LocaleNameToLCID` 로도 가지만, 후보 모듈이 같아서 캐시 때문에 `LoadLibraryExW` 가 늘지 않는다 (결과서).
- 다른 표 항목들(`__acrt_initializers` 나머지, `.CRT$XI` 나머지 5개, `.CRT$XC`, `__vcrt_initialize`)에서는 `try_get_function_slow` 로 가는 정상 길이 없었다 (지도. 표 안 더 깊은 간접 호출은 못 봄).
- **세 번 = 세 길이 각자 첫 후보 모듈을 한 번씩 로드** `[추측]`. 그러려면 세 이름(`api-ms-win-core-fibers-l1-1-2`, `api-ms-win-core-localization-l1-2-1`, `kernel32`)이 모두 첫 시도에 풀려야 한다. 하나라도 실패하면 다음 후보로 더 불러서 횟수가 달라진다 → Q31.

### 정적 — 예상 콜스택 (위가 가장 안쪽)

- **`main` 입구에서 `k`**: `test!main` → `test!__scrt_common_main_seh+0x10f` → `KERNEL32!BaseThreadInitThunk+0x17` → `ntdll!RtlUserThreadStart+0x2c`.
  - **`mainCRTStartup` 프레임은 없다** (`+0xd` 꼬리 `JMP`). 이렇게 안 보이면 꼬리 점프 가정이 틀린 것.
  - `mainCRTStartup+0x9` 는 `__security_init_cookie` 안에서 `k` 를 찍을 때만 보인다.
- **`mainCRTStartup` 입구에서 `k`**: `test!mainCRTStartup` → `BaseThreadInitThunk+0x17` → `RtlUserThreadStart+0x2c` (run29 CFG 중개 `JMP` 뿐 + 진입점 값이 `mainCRTStartup`).
- **`LoadLibraryExW` 에서 `k` (길 A 예)**: `KERNELBASE!LoadLibraryExW` → `test!try_get_function_slow+0x73` → `__acrt_tls2_supported+0x37` → `__acrt_initialize_ptd+0x9` → `__acrt_execute_initializers+0x2a` → `__scrt_initialize_crt+0x22` → `__scrt_common_main_seh+0x19` → `BaseThreadInitThunk+0x17` → `RtlUserThreadStart+0x2c`.
  - `__acrt_initialize` 는 꼬리 `JMP` 라 안 보인다. 표 항목 간접 호출의 `_guard_dispatch_icall` 도 `jmp rax` 라 안 보인다.
  - 길 B·C 는 위 나무의 ret 들이 그 자리에 들어간다.

### 정적 — test.exe 안 CFG

- **CFG 함수 표는 없다.** `DllCharacteristics = 0x8160` (`GUARD_CF` `0x4000` 비트 없음), `GuardCFFunctionTable = 0`, `GuardCFFunctionCount = 0`, `GuardFlags = 0x100` (결과서, PE 파일).
- **`__guard_dispatch_icall_fptr` 는 있다** (rva `0x6e2f8`). 값은 `_guard_dispatch_icall_nop` (rva `0x6bc70`, 내용 `jmp rax`). `__guard_check_icall_fptr` 도 아무것도 안 하는 함수. 즉 test.exe 안 간접 호출은 **검사 없이 그냥 점프**한다.
- `[소스]` `build.bat` 에 `/guard:cf` 가 없다. 로드 설정 디렉터리는 CRT 가 넣는 기본형 (rva `0x79090`).
- kernel32 쪽 CFG 검사(`BaseThreadInitThunk +0x12`)가 함수 표 없는 exe 의 진입점을 어떻게 통과시키나는 `[추측]` 만 있다 (함수 표가 없는 이미지는 "전부 유효" 로 처리한다고 알려짐, 이 빌드에선 확인 안 함) → Q30.

### 정적 — TLS 콜백 배열 (질문 6)

- `_tls_used` (rva `0x79500`) 의 `AddressOfCallBacks` = rva `0x6e380`. 배열 = **`[ExeTls (0x1400012a0), 0]`**. 바로 앞뒤 `__xl_a`·`__xl_z` 도 0.
- CRT 가 따로 넣은 TLS 콜백은 없다. CRT 의 `__dyn_tls_init_callback`·`__dyn_tls_dtor_callback` 은 시작값 0 이고 (`thread_local` 안 씀), 배열이 아니라 `__scrt_common_main_seh +0x9e~+0xe8` 에서 따로 검사해 부른다.
- 그래서 run28 `LdrpCallTlsInitializers` (배열을 0 까지 돎) 는 test.exe 에서 `ExeTls` 한 번 부르고 멈출 것이다 `[정적]`.

### 호출 관계

```
[KERNEL32]  BaseThreadInitThunk
 └─ +0x17  ? 진입점(PEB) — CFG 중개 JMP, 프레임 없음                 (run25·run29, 대상은 Q30)
[test.exe]  mainCRTStartup  (rva 0x1d40 = 스레드 시작 주소 [관찰 run5]. 받은 PEB 안 씀)
 ├─ +0x9   __security_init_cookie
 ══▶ +0xd JMP (꼬리, 프레임 걷음)
[test.exe]  __scrt_common_main_seh
 ├─ (1) +0x19   __scrt_initialize_crt   … 길 A·B → try_get_function_slow
 ├─ (2) +0x61   _initterm_e             … 길 C   → try_get_function_slow
 │                                                  └─ +0x73  KERNELBASE!LoadLibraryExW   (×3 [관찰 run7])
 ├─ (3) +0x10f  main
 └─ (4) +0x176  exit(main 반환값)
```

### 그림 반영 (flow.md 그림 2)

- 결정대로 test.exe 상자엔 **진입점 경계 상자 하나**만 둔다: "exe 진입점 = PE `AddressOfEntryPoint` (rva `0x1d40`, `test!mainCRTStartup`) — 여기부터 컴파일러 CRT (OS 아님)", 점선 (`(정적)`).
- 그 상자 → (1) `?` `KERNELBASE!LoadLibraryExW ×3` (→ ntdll 로더), → (2) `?` `test!main`. 둘 다 사이에 CRT 함수가 끼고 caller 를 콜스택으로 본 적이 없어서 `?` 다. run7 `[관찰]` 의 돌아올 자리 `test+0x57723` 이 CRT 안이라는 건 상자 글에 적었다.
- `__scrt_common_main_seh`·`try_get_function_slow` 등 CRT 함수는 그림에 안 넣고 flow.md 2.10 "CRT 안쪽 (참고 — 지도 범위 밖)" 에만 둔다.

## 확인된 것

- (`[정적]`) 진입점 rva `0x1d40` 의 PDB 이름은 **`mainCRTStartup`** 이다 (run5 `[관찰]` 과 같음). 18바이트이고 **받은 인자(`rcx` = PEB)를 읽지 않는다.**
- (`[정적]`) `mainCRTStartup` 은 `+0x4` (ret `+0x9`) `__security_init_cookie` 하나만 부르고 **`+0xd` 에서 `JMP` 로 `__scrt_common_main_seh` 에 넘긴다.** 점프 전에 자기 프레임을 걷어서 콜스택에 안 남는다.
- (`[정적]`) **CRT 준비와 `main` 호출은 `__scrt_common_main_seh`** 몫이다. `main` 은 `+0x10a` (ret **`+0x10f`**) 에서 직접 `call` 로 불린다. 다른 길(`invoke_main`)은 아무도 안 부르는 사본이다.
- (`[정적]` + `[관찰]`) run7 의 `LoadLibraryExW` 돌아올 주소 `0x7ff70ee67723` = test.exe rva `0x57723` = **`try_get_function_slow +0x73`** (call `+0x6d`). UCRT 가 늦게 묶는 API 를 찾으려고 `LoadLibraryExW(이름, NULL, 0x800)` 을 쓰는 함수다.
- (`[정적]`) 그 함수로 가는 길은 `main` 전에 셋 (`FlsGetValue2` / `LCMapStringEx` / `AreFileApisANSI`) 이고, 모두 `__scrt_common_main_seh +0x19` 또는 `+0x61` 아래다. 세 번이 이 세 길이라는 건 `[추측]`.
- (`[정적]`) test.exe 는 CFG 함수 표가 없고, 안쪽 간접 호출 중개(`_guard_dispatch_icall_nop`)는 `jmp rax` 뿐이다.
- (`[정적]`) test.exe TLS 콜백 배열은 `[ExeTls, 0]` 하나뿐이다.

### flow.md 2.10 `[추측]` 두 줄 판정

| 예전 `[추측]` | 판정 | 근거 |
|---|---|---|
| "`mainCRTStartup` 은 CRT 준비(`LoadLibraryExW` 3번 포함)를 하고 `main` 을 부른다" | **방향은 맞지만 함수가 다르다.** `mainCRTStartup` 은 쿠키만 섞고 `JMP` 로 넘긴다. CRT 준비와 `main` 호출은 **`__scrt_common_main_seh`** 가 한다 `[정적]`. `LoadLibraryExW` 3번은 그 아래 세 길이 있다까지 `[정적]`, 세 길이 그 세 번이라는 건 `[추측]` (Q31) | 위 호출 지점 표, 세 길 나무 |
| "그 코드(`…67723`)가 test.exe 의 어느 함수인지는 이름으로 확인하지 않았다" | **`test!try_get_function_slow +0x73`** (rva `0x57723`) `[정적]`. 9곳 중 아래 16비트가 맞는 ret 은 하나뿐 | `LoadLibraryExW` 9곳 표 |

## 정정

- run29 "호출 관계" 아래 "`main` 에서 `k` 예상" 의 `test!mainCRTStartup+?` 프레임은 **안 보일 것이다.** `mainCRTStartup` 이 `+0xd` 에서 `JMP` 로 넘겨 프레임이 없다. 예상은 `test!main` → `test!__scrt_common_main_seh+0x10f` → `BaseThreadInitThunk+0x17` → `RtlUserThreadStart+0x2c`. run29 문서 맨 위에 한 줄 넣었다.
  - run29 의 다른 예상 "`mainCRTStartup` **입구**에서 `k` 하면 바로 위가 `BaseThreadInitThunk+0x17`" 은 그대로 맞다.

## 아직 확인 필요

동적 확인거리는 오케스트레이터가 이미 대기열에 넣었다. 오프셋은 `test + rva` (베이스는 실행마다 다르지만 아래 16비트는 같다). test.exe 안이라 다른 프로세스에 안 걸리지만, 커널 디버거에선 test.exe 가 올라온 뒤에 걸어야 한다.

- **Q30**:
  - `test+0x1d40` (`mainCRTStartup` 입구) 에서 `k`: 바로 위가 `KERNEL32!BaseThreadInitThunk+0x17` 인가 (이어 붙이기).
  - kernel32 의 CFG 검사가 CFG 표 없는 test.exe 진입점을 어떻게 통과시키나.
  - `test+0x1320` (`main` 입구) 에서 `k`: 바로 아래가 `__scrt_common_main_seh+0x10f` 이고 `mainCRTStartup` 이 없는가. `test+0x1b00` 입구에서도 `mainCRTStartup` 프레임이 없는가.
- **Q31**:
  - `test+0x5771d` (`try_get_function_slow +0x6d`, call) 에서 `du @rcx` (이름), `r8` = `0x800`, `k` 로 길 A·B·C 구분.
  - `test+0x57723` (ret) 에서 `rax` (0 이면 다음 후보로 가서 횟수가 는다).
  - 세 번이 넘거나 `test+0x57777` (플래그 0 다시 시도의 ret) 에 서면 "세 길이 각자 첫 후보" `[추측]` 이 틀린 것.
- (2026-10-23 결정으로 좁힘) Q30 = 진입점 도착까지 + CFG 통과, Q31 = `LoadLibraryExW` ×3 이 ntdll 로더에서 무엇을 올리나 (이름·`LdrpLoadDll` 결과만). 위 CRT 프레임·길 A·B·C 구분은 참고로만.
- 정적으로 남은 것:
  - **Q59**: Q18 원래 범위 중 kernel32 `BasepInitializeTermsrvFpns` 안쪽 (`BaseThreadInitThunk` 첫 인자 ≠ 0 갈래) — 이번엔 안 봤다.
  - **Q55** (출처로 붙음): dll1·dll2 시작 함수 안 `LoadLibraryExW` ×3 (run7 `0x7ffd80e2f5fb`·`0x7ffd80ebf60b`) 도 같은 UCRT 함수일 것 `[추측]` — dll 정적으로 확인 가능 (Q55 와 같은 바이너리).

## 디버거 메모 / 도구 메모

- MS 바이너리가 아닌 sample 빌드를 넣을 때: `ms/bin/test.exe`, `ms/sym/test.exe.pdb` 로 **복사**해서 `run_ghidra.sh` 를 그대로 썼다. 다시 빌드해 올리면 복사도 다시 해야 한다 (결과서).
- import 함수를 부르는 곳은 이름(`refs test.exe LoadLibraryExW`, 0줄)이 아니라 **IAT 칸 rva**(`refs test.exe 0x6e250`)로 찾는다. `_index.tsv` 의 `LoadLibraryExW` 는 `JMP [IAT]` 조각일 뿐이다.
- 지도(`map`)는 함수 포인터 표를 못 따라가서 CRT 초기화 표 안 함수는 뿌리를 따로 줘야 했다.
- `fetch_pdb.py` 는 로컬 PDB 를 모른다 (없으면 msdl 에서 받으려 함, 있으면 `.info.txt` 를 msdl 주소로 덮어씀). 이번엔 `--info` 로만 썼다.
- 위 셋은 오케스트레이터가 하네스 후보 (36)~(38) 로 이미 적어 두었다.
- **WinDbg 에서 이름이 다르게 보일 수 있다** `[추측]`: test.pdb 가 디버거 VM 심볼 경로에 없으면 `test+0x1b00` 처럼 rva 로만 보인다. 이번 예상 콜스택의 이름은 PDB 기준이다.

---

이 바퀴로 **단계 2 (ntdll 지도) 의 처음 항목이 모두 끝났다** (Q8 ~ Q18). 바퀴 사이에 새로 붙은 단계 2 항목(예: Q55)은 대기열에 남아 있다.

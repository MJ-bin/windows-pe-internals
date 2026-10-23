# 25 exe-crt-start — 정적 분석 결과

- 요청서: `_workspace/sast/25_exe-crt-start.md`
- 모듈 / 바이너리: **test.exe** (`sample/bin/test.exe` → `ms/bin/test.exe` 로 복사, sha256 앞 `60d03d93…`, 551424바이트). MS 바이너리가 아님. 이미지 베이스 `0x140000000`, 진입점 rva `0x1d40`.
- PDB: 로컬 `sample/bin/test.pdb` → `ms/sym/test.exe.pdb` 로 복사. 서명 `C66E7084C5CA4874B1677F6F1C36A0511` (GUID `C66E7084-C5CA-4874-B167-7F6F1C36A051`, age 1).
- 분석: Ghidra 11.3.2 analyze 22초, 함수 3356개 (`/MT` 라서 UCRT·vcruntime 이 test.exe 안에 들어 있다). 로그 `ms/ghidra/test.analyze.log` 에 `SetPdb:` 줄 있음.
- 뽑은 함수: depth 0 으로 여섯 번에 나눠 41개 → `ms/decomp/test/`, `ms/disassemble/test/` (사슬 위 함수 + `LoadLibraryExW` 를 부르는 길 위의 함수). `_calls.tsv`, 지도 `_map/` (`mainCRTStartup`, `__scrt_common_main_seh` 와 CRT 초기화 표 함수 19개 뿌리), `_refs/` (`LoadLibraryExW`, IAT `0x6e250`, `main`, `invoke_main` …).
- dll1·dll2 는 손대지 않았다 (analyze 도 안 걸었다).
- 버전 확인: **일치.**
  - PDB 서명: `test.exe` 의 CodeView 서명과 `test.pdb` 안 PDB 정보 스트림(GUID+age 1)이 글자까지 같다 (MSF 를 직접 읽어 확인).
  - rva `0x1d40` 의 PDB 이름 = `mainCRTStartup` (flow.md 2.3 `[관찰]` 과 같음).
  - run7 `[관찰]` 의 `0x7ff70ee67723`: 베이스가 64KB 정렬이면 rva `0x57723` 이어야 하는데 (`0x7ff70ee67723 - 0x57723 = 0x7ff70ee10000`), test.exe 안 `LoadLibraryExW` 호출 9곳 중 이 값이 되는 ret 이 **정확히 하나** 있다. 바이너리가 VM 것과 같다는 두 번째 증거.

## 스크립트 문제

고칠 버그는 없다. MS 바이너리가 아닌 파일을 넣으면서 불편했던 점만 적는다.

- 넣은 방법: `cp -p sample/bin/test.exe ms/bin/test.exe`, `cp -p sample/bin/test.pdb ms/sym/test.exe.pdb`. 그다음 `run_ghidra.sh analyze test.exe` 가 그대로 돌았다 (모듈 이름 `test`). 링크 대신 복사한 이유: 샘플을 다시 빌드해 올리면 둘이 어긋나는지 눈으로 보기 쉽다. 다시 올리면 복사를 다시 해야 한다.
- `fetch_pdb.py test.exe` 는 `ms/sym/test.exe.pdb` 가 없으면 `msdl` 에서 `test.pdb` 를 받으려 한다 (MS 서버에 없는 파일이라 헛걸음). 있으면 "이미 있음" 으로 넘어가지만 `.info.txt` 를 `msdl` 주소로 써 버린다. 이번엔 `--info` 로 GUID 만 읽었다. 로컬 PDB 는 복사만 하고 `fetch_pdb.py` 는 `--info` 로만 쓴다는 안내가 스킬에 있으면 좋다.
- `pdb_aliases.py test.exe` 는 로컬 PDB 로 잘 돈다 (`ms/sym/test.exe.aliases.tsv`).
- `refs test.exe LoadLibraryExW` 는 **0줄**이 나온다. `_index.tsv` 의 `LoadLibraryExW` (rva `0x6b90b`) 는 import 라이브러리가 만든 `JMP [IAT]` 조각 함수이고, 실제 호출은 모두 IAT 칸(`CALL [0x14006e250]`)으로 직접 간다. **import 를 부르는 곳은 `refs test.exe 0x6e250` (IAT rva)** 로 찾아야 한다. 스킬 안내에 "import 는 IAT 칸으로 찾는다"가 있으면 좋다 (ntdll 쪽 `indirect:import:` 와 같은 이야기).
- 지도(`map`)는 **함수 포인터 표**를 못 따라간다. `__acrt_execute_initializers`, `_initterm_e`, `_initterm` 이 표 안 함수를 간접 호출하는데 지도엔 `indirect-cfg` 끝으로만 나온다. 그래서 표 안 함수(`__acrt_initializers` 16쌍, `.CRT$XI*`, `.CRT$XC*`)를 **뿌리로 각각 지도를 돌려서** 이었다. 표를 읽어 `표 항목` 간선으로 넣어 주는 기능이 있으면 CRT·로더 길에 도움이 된다 (제안만, 이번 결론에는 영향 없음).

## 한눈에

- 진입점 rva `0x1d40` = `mainCRTStartup`. **받은 인자(`rcx`)는 쓰지 않고 버린다.**
- `mainCRTStartup` 은 `__security_init_cookie` 를 한 번 부르고 **`JMP` 로 `__scrt_common_main_seh` 에 넘긴다** (꼬리 점프, 돌아올 자리 없음). 그래서 **`main` 에서 `k` 를 찍으면 `mainCRTStartup` 프레임은 보이지 않고** `test!__scrt_common_main_seh+0x10f` 가 나온다.
- `main` 은 `__scrt_common_main_seh +0x10a` (ret `+0x10f`) 에서 **직접 `call`** 로 불린다. 중간 `invoke_main` 은 이 빌드에서 안 쓰이는 사본이다 (아무도 안 부름).
- run7 의 `…7723` = test.exe rva **`0x57723`** = **`test!try_get_function_slow +0x73`** (call 은 `+0x6d`). UCRT 가 "늦게 묶는 API"를 찾으려고 `LoadLibraryExW(이름, NULL, 0x800)` 을 쓰는 함수다.
- 그 함수가 `main` 전에 **세 길**에서 불릴 수 있다 (`FlsGetValue2` / `LCMapStringEx` / `AreFileApisANSI`). 세 길 모두 `__scrt_common_main_seh` 의 `+0x14`(`__scrt_initialize_crt`) 또는 `+0x5c`(`_initterm_e`) 아래다. 세 번의 이유 `[추측]`.
- test.exe 에는 CFG 표가 **없다** (`GuardCFFunctionTable` = 0). 하지만 `__guard_dispatch_icall_fptr` 는 **있다** (그냥 `jmp rax` 로 이어지는 빈 검사).
- TLS 콜백 배열은 `[ExeTls, 0]` **하나뿐**.

## 질문별 답 [정적]

### 1. 진입점 이름과 인자

- 답 (예): rva `0x1d40` 의 PDB 이름은 **`mainCRTStartup`** (`_index.tsv`: `mainCRTStartup 140001d40 0x1d40 18` 바이트).
- 인자는 **버린다.** 이 함수는 18바이트뿐이다. `rcx`·`rdx`·`r8` 어느 것도 읽지 않는다. PDB 타입은 `ulong __cdecl mainCRTStartup(void *)` 라서 `RtlUserThreadStart`·`BaseThreadInitThunk` 가 넘기는 PEB 포인터를 받는 모양이지만 본문에서 쓰이지 않는다.
  - 다음 함수 `__scrt_common_main_seh` 도 첫 줄들이 `rbx`·`rsi` 를 저장하고 `ecx = 1` 을 넣을 뿐, 넘어온 `rcx` 를 읽지 않는다. `__security_init_cookie` 는 인자 없는 함수다.
- 근거: `ms/disassemble/test/mainCRTStartup.asm` 전체 (`+0x0 SUB RSP,0x28`, `+0x4 CALL`, `+0x9 ADD RSP,0x28`, `+0xd JMP`). 원본 바이트로도 `48 83 ec 28 e8 c7 05 00 00 48 83 c4 28 e9 ae fd ff ff` 로 확인.

### 2. `mainCRTStartup` → `main` 사슬

**사슬 (call 오프셋 → ret 오프셋, 함수 시작 기준)**

| 단계 | 함수 (rva) | 부르는 줄 | ret | 부르는 대상 |
|---|---|---|---|---|
| 0 | `mainCRTStartup` (`0x1d40`) | `+0x4` | `+0x9` | `__security_init_cookie` (쿠키 값 섞기. 인자 없음) |
| 0 | 〃 | `+0xd` `JMP` | (꼬리) | `__scrt_common_main_seh` 시작(`+0x0`)으로 점프. 점프 전에 `ADD RSP,0x28` 로 자기 프레임을 걷어서 **돌아올 자리가 안 남는다.** 목적지 `0x140001b00 − 0x140001d40 = −0x240` 은 다른 함수의 시작이다. |
| 1 | `__scrt_common_main_seh` (`0x1b00`) | `+0x14` | `+0x19` | `__scrt_initialize_crt` (인자 `ecx = 1`) |
| 1 | 〃 | `+0x29` | `+0x2e` | `__scrt_acquire_startup_lock` |
| 1 | 〃 | `+0x5c` | `+0x61` | `_initterm_e` (`.CRT$XIA`~`XIZ`, rva `0x6e338`~`0x6e370`) |
| 1 | 〃 | `+0x7d` | `+0x82` | `_initterm` (`.CRT$XCA`~`XCZ`, rva `0x6e320`~`0x6e330`) |
| 1 | 〃 | `+0x99` | `+0x9e` | `__scrt_release_startup_lock` |
| 1 | 〃 | `+0x9e` | `+0xa3` | `__scrt_get_dyn_tls_init_callback` (콜백이 0 이면 `+0xaa` 에서 건너뜀. 값 칸 `__dyn_tls_init_callback` rva `0x84070` 은 `.data` 의 0 채움 구간이라 시작값 0) |
| 1 | 〃 | `+0xc5` | `+0xcb` | 위 콜백을 `CALL [__guard_dispatch_icall_fptr]` 로 부르는 줄 (콜백 시작값이 0 이라 `+0xaa` 에서 건너뛰는 코드) |
| 1 | 〃 | `+0xed` / `+0xf5` / `+0xfd` | `+0xf2` / `+0xfa` / `+0x102` | `_get_initial_narrow_environment` / `__p___argv` / `__p___argc` |
| 1 | 〃 | **`+0x10a`** | **`+0x10f`** | **`main`** (rva `0x1320`) |
| 1 | 〃 | `+0x111` | `+0x116` | `__scrt_is_managed_app` (`main` 이후) |
| 1 | 〃 | `+0x171` | `+0x176` | `exit` (`main` 반환값을 `ecx` 로. 관리 코드 앱이 아니면 이 길. `_cexit`(`+0x11f`)은 관리 앱일 때만) |

- **`main` 을 부르는 줄: `__scrt_common_main_seh +0x10a`, ret `+0x10f`** (rva `0x1c0a` → `0x1c0f`). 원본 바이트 `e8 11 f7 ff ff` → 목적지 rva `0x1320` 맞음.
  - `main` 으로 가는 다른 줄: `invoke_main +0x31` 의 `JMP` 하나. 그런데 **`invoke_main` (rva `0x1cb0`) 을 부르는 곳이 없다** (참조는 `.pdata` 하나뿐). `__scrt_common_main` (rva `0x1ae0`) 도 `mainCRTStartup` 과 똑같은 18바이트인데 부르는 곳이 없다. 둘 다 CRT 라이브러리에 같이 들어온 안 쓰는 사본이다 (`[추측]` CRT 라이브러리는 최적화돼 들어와 `invoke_main` 이 `__scrt_common_main_seh` 안으로 합쳐졌고, 단독 사본만 남았다).
- `main` 이 돌아온 뒤: 반환값을 `ebx` 에 담고 `__scrt_is_managed_app` → (아니면) `exit(ebx)`. 이름과 자리만 (본문은 범위 밖).
- 근거: `ms/disassemble/test/mainCRTStartup.asm`, `__scrt_common_main_seh.asm`, `_calls.tsv`, `ms/disassemble/test/_refs/main.tsv`, `invoke_main.tsv`, `__scrt_common_main.tsv`.

### 3. `LoadLibraryExW` ×3

**3-1. `…7723` ret 이 있는가: 있다, 하나뿐.**

- `LoadLibraryExW` 는 test.exe 에서 IAT 칸 rva **`0x6e250`** (`KERNEL32.dll` import, `api-ms-…` 아님) 로 부른다. 부르는 자리 전부 (`refs test.exe 0x6e250`, `_refs/0x6e250.tsv`):

| 함수 | call | ret (rva) | 하위 16비트 |
|---|---|---|---|
| `try_get_first_available_module` | `+0x60` / `+0xb3` | `0x575d2` / `0x57625` | `75d2` / `7625` |
| **`try_get_function_slow`** | **`+0x6d`** / `+0xc1` | **`0x57723`** / `0x57777` | **`7723`** / `7777` |
| `try_get_module` | `+0x55` / `+0xa5` | `0x578bf` / `0x5790f` | `78bf` / `790f` |
| `try_get_proc_address_from_first_available_module` | `+0x68` / `+0xbb` | `0x579ce` / `0x57a21` | `79ce` / `7a21` |
| `try_load_library_from_system_directory` | `+0x11` (+ `+0x64` 는 `JMP`, 돌아올 자리 없음) | `0x57aa3` | `7aa3` |

- **`…7723` 이 되는 ret 은 `try_get_function_slow +0x73` (rva `0x57723`) 하나.** `0x7ff70ee67723 − 0x57723 = 0x7ff70ee10000` (64KB 정렬 맞음). 호출 줄 `+0x6d` 는 `CALL [0x14006e250]` 이고 바이트 `ff 15 2d 6b 01 00`, 바로 앞에서 `r8d = 0x800` (`LOAD_LIBRARY_SEARCH_SYSTEM32`), `rdx = 0`, `rcx = module_names[id]` 를 넣는다.
- 함수 이름: **`try_get_function_slow`** (rva `0x576b0`, 433바이트). UCRT 의 "늦게 묶는 API 찾기" 공용 함수다. 부르는 함수가 65곳 (`__acrt_AreFileApisANSI`, `__acrt_LCMapStringEx`, `__acrt_tls2_supported`, `try_get_function` 등).
- 근거: `ms/disassemble/test/try_get_function_slow.asm` `+0x5a`~`+0x73`, `ms/decomp/test/try_get_function_slow.c` (요약).

**3-2. 무엇을 부르려고 쓰나**

- 이 함수가 하는 일 `[정적]` (`.asm` 흐름 기준):
  1. 후보 모듈 목록 `[시작, 끝)` (4바이트 id 배열)을 앞에서부터 돈다.
  2. 모듈 핸들 캐시 `module_handles[id]` (rva `0x83ef0`) 가 비어 있으면 `LoadLibraryExW(module_names[id], NULL, 0x800)` (`+0x6d`).
  3. 성공하면 캐시에 넣고 `GetProcAddress(모듈, API 이름)` (`+0x19e`) 로 바꿔서 끝낸다. **성공한 모듈에서 API 가 없어도 다음 후보로 안 가고 끝낸다** (`GetProcAddress` 뒤 곧바로 `+0x1a7 JMP`).
  4. 실패하면: 에러가 `0x57` (잘못된 인자)이고 이름이 `api-ms-`·`ext-ms-` 로 시작하지 않을 때만 플래그 `0` 으로 한 번 더 (`+0xc1`, ret `0x57777`). 아니면 캐시에 `-1` 을 넣고 **다음 후보 모듈로** 간다.
  5. 찾은 함수 주소(없으면 `-1`)를 함수 포인터 표 `function_pointers` 에 적는다. 이 표가 섹션 **`.fptable`** (rva `0x8a000`, 0x100바이트, 평소 읽기 전용이라 쓰기 전후로 `VirtualProtect` 를 부른다). **CFG 와 상관없는 이름이다.**
- 그러므로 "왜 세 번인가": 모듈 캐시 때문에 **같은 모듈은 한 번만** 로드하고, 후보는 앞에서부터 성공할 때까지다. 그래서 세 번은 "목록을 돌며 하나씩"이 아니라 **서로 다른 API 세 개가 각자 첫 후보 모듈을 로드한 것**으로 읽는 게 가장 자연스럽다 `[추측]` (아래 3-3).
- 후보 모듈과 문자열 (UTF-16LE 원본 바이트, `module_names[]` 는 rva `0x729e0` 의 포인터 배열):

| API (function_id) | 후보 id → 이름 (순서대로) | 이름 문자열 rva |
|---|---|---|
| `FlsGetValue2` (id 3) | 1 `api-ms-win-core-fibers-l1-1-2` → 19 `kernelbase` | `0x72ae0` / `0x72f90` |
| `LCMapStringEx` (id 0x12), `LocaleNameToLCID` (id 0x14) | 5 `api-ms-win-core-localization-l1-2-1` → 18 `kernel32` | `0x72bd0` / `0x72f78` |
| `AreFileApisANSI` (id 0) | 18 `kernel32` 하나뿐 | `0x72f78` |

  - 원본 바이트 예: `kernel32` = `6b 00 65 00 72 00 6e 00 65 00 6c 00 33 00 32 00 00 00`, `api-ms-win-core-fibers-l1-1-2` = `61 00 70 00 69 00 2d 00 6d 00 73 00 2d 00 77 00 69 00 6e 00 2d 00 63 00 …` (60바이트). 후보 id 배열은 rva `0x73088`(`FlsGetValue2`), `0x73260`(`LCMapStringEx`), `0x73038`(`AreFileApisANSI`), `0x73298`(`LocaleNameToLCID`).
  - 위 이름이 **실제로 어떤 DLL 로 풀리는지**(`api-ms-…` 가 24H2 에서 있나, 없으면 다음 후보로 가나)는 정적으로 모른다 → 동적.

**3-3. 2번 사슬의 어디에서 불리나 (직접 호출 길, 표 포함)**

세 길 모두 `main` 전, `__scrt_common_main_seh` 의 `+0x14` 또는 `+0x5c` 아래다. 읽은 순서 = 실행 순서일 가능성이 높다 `[추측]` (`__acrt_initializers` 표를 앞에서부터 돌고, 그다음 `_initterm_e`).

- **길 A, `FlsGetValue2`** (`__scrt_initialize_crt` 쪽):
  `__scrt_common_main_seh +0x14`(ret `+0x19`) → `__scrt_initialize_crt +0x1d`(ret `+0x22`) → `__acrt_initialize` (`JMP` 꼬리호출 → 프레임 없음) → `__acrt_execute_initializers +0x25`(ret `+0x2a`, 표 항목을 `_guard_dispatch_icall` 로 간접 호출) → 표 8번째 쌍 `__acrt_initialize_ptd` (`+0x4`, ret `+0x9`) → `__acrt_tls2_supported` (`+0x32`, ret `+0x37`) → `try_get_function_slow` → `+0x6d` (ret `+0x73`).
- **길 B, `LCMapStringEx`**:
  위와 같은 `__acrt_execute_initializers +0x2a` → 표 11번째 쌍 `__acrt_initialize_multibyte` (`+0x4d`, ret `+0x52`) → `setmbcp_internal` (`+0x168`, ret `+0x16d`) → `_setmbcp_nolock` (`+0x291`, ret `+0x296`) → `setSBUpLow` (`+0x103`, ret `+0x108`) → `__acrt_LCMapStringA` (`+0x6c`, ret `+0x71`) → `__acrt_LCMapStringA_stat` (`+0x164`, ret `+0x169`) → `__acrt_LCMapStringEx` (`+0x4a`, ret `+0x4f`) → `try_get_function_slow` (`+0x6d`, ret `+0x73`).
  - `__acrt_initialize_multibyte` 는 `initialized` 깃발(rva `0x83864`)이 있어 **처음 한 번만** 일을 한다. 표 쪽이 `_initterm_e` 보다 앞이므로 B 가 이 한 번일 가능성이 높다.
- **길 C, `AreFileApisANSI`** (`_initterm_e` 쪽):
  `__scrt_common_main_seh +0x5c`(ret `+0x61`) → `_initterm_e +0x1d`(ret `+0x22`, 표 항목 간접 호출) → `.CRT$XI` 첫 항목 `pre_c_initialization` (`+0x50`, ret `+0x55`) → `_configure_narrow_argv` (`+0x55`, ret `+0x5a`) → `__acrt_GetModuleFileNameA` (`+0xad`, ret `+0xb2`) → `__acrt_AreFileApisANSI` (`+0x2d`, ret `+0x32`) → `try_get_function_slow` (`+0x6d`, ret `+0x73`).
- 다른 길은 없다: `__acrt_initializers` 의 나머지 항목, `.CRT$XI` 의 나머지 5개, `.CRT$XC` 의 `pre_cpp_initialization`, `__vcrt_initialize` 등을 뿌리로 지도를 돌려 보니 `try_get_function_slow` 로 가는 정상 호출 길이 없었다 (`abort`·`_exit` 같은 오류 길은 뺐다). 지도는 직접 호출과 `_guard_dispatch` 호출의 끝까지만 보므로, 표 항목 안의 더 깊은 간접 호출은 못 본다.
- **세 번 = 세 길이 각자 첫 후보를 로드** `[추측]`: A 의 `api-ms-win-core-fibers-l1-1-2`, B 의 `api-ms-win-core-localization-l1-2-1`, C 의 `kernel32` (또는 B·C 에서 `kernel32` 가 쓰이는 조합). 이게 맞으려면 세 이름이 모두 첫 시도에 풀려야 한다. 하나라도 실패하면 다음 후보(`kernelbase`/`kernel32`)로 더 부르므로 횟수가 달라질 수 있다 → 동적(Q31)으로 인자 이름을 본다.
  - 참고: 길 B 의 `__acrt_LCMapStringEx` 가 함수 주소가 없다고 보면 `+0xb0` 에서 `__acrt_LocaleNameToLCID` (`+0x39` 에서 `try_get_function_slow`) 로 간다. 후보 모듈이 같은 `[5, 18]` 이라 모듈 캐시 때문에 `LoadLibraryExW` 가 늘지는 않는다.

### 4. `BaseThreadInitThunk` 와 이어 붙이기 (`+0x17` → `main`)

`KERNEL32!BaseThreadInitThunk +0x12` (ret `+0x17`) 가 CFG 중개를 거쳐 `RDX` (= 진입점 rva `0x1d40` = `mainCRTStartup`) 를 부른다고 보면 `[정적: run25·run29 + 진입점 값 일치]` (진입점 값이 `mainCRTStartup` 인 건 이번에 확인, 실제 간접 호출 대상은 동적 Q30):

```
ntdll!RtlUserThreadStart            +0x2c   (run29)
KERNEL32!BaseThreadInitThunk        +0x17   (run25)
  └ mainCRTStartup (rva 0x1d40) 으로 들어감. 자기 안에서 __security_init_cookie 를 부르고(+0x9),
    +0xd 에서 JMP 로 __scrt_common_main_seh 로 넘기며 프레임이 사라진다.
test!__scrt_common_main_seh         +0x19   __scrt_initialize_crt 안 (표 → 길 A·B, LoadLibraryExW)
test!__scrt_common_main_seh         +0x61   _initterm_e 안 (길 C, LoadLibraryExW)
test!__scrt_common_main_seh         +0x10f  ← main 호출 후 돌아올 자리
test!main                           (rva 0x1320)
```

- **`main` 에서 `k` 예상 (위가 가장 안쪽)**: `test!main` → `test!__scrt_common_main_seh+0x10f` → `KERNEL32!BaseThreadInitThunk+0x17` → `ntdll!RtlUserThreadStart+0x2c`. **`mainCRTStartup` 은 없다.** 요청서의 "`mainCRTStartup+?`" 자리는 비는 게 정상이고, 그 자리에 `__scrt_common_main_seh+0x10f` 가 온다. (`k` 모양이 이렇지 않으면 꼬리 점프 가정이 틀린 것.)
- `mainCRTStartup+0x9` 는 `__security_init_cookie` 안에서 `k` 를 찍을 때만 보인다.
- 각 `LoadLibraryExW` 에서 `k` 예상 (위가 가장 안쪽, 길 A 예):
  `KERNELBASE!LoadLibraryExW` → `test!try_get_function_slow+0x73` → `test!__acrt_tls2_supported+0x37` → `test!__acrt_initialize_ptd+0x9` → `test!__acrt_execute_initializers+0x2a` → `test!__scrt_initialize_crt+0x22` → `test!__scrt_common_main_seh+0x19` → `BaseThreadInitThunk+0x17` → `RtlUserThreadStart+0x2c`.
  - 길 B 는 `try_get_function_slow+0x73` 위에 `__acrt_LCMapStringEx+0x4f` → `__acrt_LCMapStringA_stat+0x169` → `__acrt_LCMapStringA+0x71` → `setSBUpLow+0x108` → `_setmbcp_nolock+0x296` → `setmbcp_internal+0x16d` → `__acrt_initialize_multibyte+0x52` → `__acrt_execute_initializers+0x2a` → …
  - 길 C 는 `try_get_function_slow+0x73` 위에 `__acrt_AreFileApisANSI+0x32` → `__acrt_GetModuleFileNameA+0xb2` → `_configure_narrow_argv+0x5a` → `pre_c_initialization+0x55` → `_initterm_e+0x22` → `__scrt_common_main_seh+0x61` → …
  - `_guard_dispatch_icall` 은 `jmp rax` 로 가는 빈 중개라(아래 5번) 표 항목 호출에서도 프레임이 안 남는다 → `__acrt_execute_initializers+0x2a`, `_initterm_e+0x22` 가 바로 부른 쪽 위에 보인다.

### 5. test.exe 안 CFG

- 답: **CFG 표는 없다 (아니오). `__guard_dispatch_icall_fptr` 는 있다 (예).** `[정적]`
  - `DllCharacteristics = 0x8160` (`GUARD_CF` `0x4000` 비트 없음). 로드 설정 디렉터리(`_load_config_used`, rva `0x79090`, 0x140바이트)는 있다 (CRT 가 넣는 기본형).
  - `GuardCFFunctionTable = 0`, `GuardCFFunctionCount = 0`. `GuardFlags = 0x100` (사양의 `IMAGE_GUARD_CF_INSTRUMENTED` 에 해당: "CFG 검사 호출 모양으로 컴파일됨", 함수 표 있음 `0x400` 비트는 꺼짐).
  - `GuardCFDispatchFunctionPointer` = `__guard_dispatch_icall_fptr` (rva `0x6e2f8`), `GuardCFCheckFunctionPointer` = `__guard_check_icall_fptr` (`0x6e2e8`). 값은 각각 `_guard_dispatch_icall_nop` (rva `0x6bc70`, 내용 `ff e0` = `jmp rax`), `_guard_check_icall_nop` (`0x2710`, 아무것도 안 함). 즉 **검사 없이 그냥 점프.**
  - test.exe 코드의 간접 호출은 `_guard_dispatch_icall` (rva `0x6bcb0`, `jmp [0x6e2f8]`) 이나 `CALL [0x14006e2f8]` 로 간다: `__scrt_common_main_seh +0xc5`, `__acrt_execute_initializers +0x25/+0x56`, `_initterm(_e) +0x1d` 등.
- kernel32 쪽 CFG 검사가 test.exe 진입점을 어떻게 통과시키나 `[추측]`: 함수 표가 없는 이미지는 로더가 CFG 비트맵을 "전부 유효"로 처리한다고 알려져 있으나 이 빌드(26100.9457)에서는 확인하지 않았다. 정적으로는 못 정한다 → 동적 Q30 (`BaseThreadInitThunk +0x12` 에서 ntdll `LdrpDispatchUserCallTarget` 쪽 흐름과 대상이 `mainCRTStartup` 인지).

### 6. TLS 콜백 자리

- 답: **`ExeTls` 하나뿐이고 0 으로 끝난다.** CRT 가 따로 넣은 콜백은 없다. `[정적]`
  - `_tls_used` (`IMAGE_TLS_DIRECTORY64`, rva `0x79500`): `AddressOfCallBacks = 0x14006e380` (rva `0x6e380`), `AddressOfIndex = 0x140082ce0` (`_tls_index`), TLS 원본 데이터 `0x140079f90`~`0x140079f91` (1바이트), `SizeOfZeroFill = 0`, `Characteristics = 0x100000`.
  - 배열 내용 (rva `0x6e380`): `[0] = 0x1400012a0` (`ExeTls`), `[1] = 0`. 바로 앞 `0x6e378` (`__xl_a`) 과 `0x6e388` (`__xl_z`) 은 둘 다 0 이다.
  - CRT 의 `__dyn_tls_init_callback` (rva `0x84070`) 과 `__dyn_tls_dtor_callback` (`0x84068`) 은 시작값이 0 (`thread_local` 을 안 써서 안 채워짐). CRT 는 이 값들을 TLS 배열에 넣는 게 아니라 `__scrt_common_main_seh +0x9e`~`+0xe8` 에서 따로 검사해 부른다 (값 0 이면 건너뜀).
  - 그러므로 `LdrpCallTlsInitializers` (run28) 는 `ExeTls` 한 번 부르고 0 에서 멈춘다.

## flow.md 2.10 `[추측]` 판정

- "`mainCRTStartup` 은 CRT 준비(`LoadLibraryExW` 3번 포함)를 하고 `main` 을 부른다":
  - **고쳐 쓰는 게 좋다 (방향은 맞지만 이름이 다르다).** `mainCRTStartup` 자신은 `__security_init_cookie` 만 부르고 바로 `__scrt_common_main_seh` 로 점프한다. CRT 준비와 `main` 호출은 **`__scrt_common_main_seh`** 몫이다. 그래서 실행 중 콜스택에 이름으로 남는 건 `__scrt_common_main_seh`.
  - "`LoadLibraryExW` 3번 포함" 은 정적으로 **길이 있다**까지 확인 (세 길, 위 3-3). 세 번이 실제로 일어났다는 건 run7 `[관찰]`, 세 길이 그 세 번이라는 건 `[추측]`.
- "그 코드(`…67723`)가 test.exe 의 어느 함수인지": **`test!try_get_function_slow +0x73`** (rva `0x57723`). `[정적]` (이름: UCRT `winapi_thunks` 의 안쪽 함수. 이름은 PDB 그대로.)

## 그림 2 (`m1`, `m2` 와 사이 `?`) 바꿀 후보 (대표 지점만)

1. **`test!mainCRTStartup` (rva `0x1d40`) `(정적)`** — `__security_init_cookie` 를 부르고(`+0x9`), `+0xd` 에서 `JMP` 로 `__scrt_common_main_seh` 에 넘김. **프레임이 남지 않음** (인자 `PEB` 는 안 씀). "(추측: 스레드 시작 주소)" 를 이걸로 바꾼다. 스레드 시작 주소가 `0x1d40` 인 것은 `[관찰]` run4·5.
2. **`test!__scrt_common_main_seh` (rva `0x1b00`) `(정적)`** — CRT 준비 + `main` 호출. 대표 줄 3개: `+0x14` `__scrt_initialize_crt` (표 `__acrt_initializers`), `+0x5c` `_initterm_e` (`.CRT$XI` 표), `+0x10a` `main` (ret `+0x10f`).
3. **`test!try_get_function_slow` `+0x6d` → `KERNELBASE!LoadLibraryExW` ×3 (rva `0x57723`)** — `m2` 의 "test.exe 안 `0x7ff70ee67723`" 을 이 이름으로 바꾼다. 부르는 자리 세 길: `FlsGetValue2`(`__acrt_initialize_ptd`), `LCMapStringEx`(`__acrt_initialize_multibyte` 안), `AreFileApisANSI`(`_initterm_e` → `pre_c_initialization`). 이름 매핑은 `[추측]`.
4. **`test!main` (rva `0x1320`)** — `__scrt_common_main_seh +0x10a` 에서 직접 호출. `k` 는 `__scrt_common_main_seh+0x10f` ← `BaseThreadInitThunk+0x17` ← `RtlUserThreadStart+0x2c`.

(CRT 안 나머지 초기화 함수는 그림에 넣지 않는다. `ExeTls`는 이미 있는 그림의 TLS 콜백 부분.)

## 동적분석으로 확인해야 할 것

- **Q30 와 겹침** (`RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup` 의 `k`):
  - `BaseThreadInitThunk +0x12` 의 간접 호출 대상이 정말 `test+0x1d40` 인지, 그리고 CFG 검사가 test.exe (CFG 표 없음) 진입점을 어떻게 통과시키는지.
  - 멈춤점: `test+0x1d40` (`mainCRTStartup` 입구). `k` 에서 `BaseThreadInitThunk+0x17` 바로 위에 `test!mainCRTStartup` 이 보이면 이어붙임이 맞다.
- **Q31 와 겹침** (`mainCRTStartup` → `LoadLibraryExW` ×3 → `main` 의 `k`):
  - `test+0x5771d` (`try_get_function_slow` 의 `LoadLibraryExW` 호출 줄). 여기서 `rcx` = 이름(UTF-16, `du @rcx`), `r8 = 0x800`. 세 번 모두 각각 `k` 로 길 A·B·C 중 어느 길인지, 이름이 `api-ms-win-core-fibers-l1-1-2` / `api-ms-win-core-localization-l1-2-1` / `kernel32` 인지 본다.
  - `test+0x57723` (ret). `rax` = 모듈 핸들 또는 0 (0 이면 같은 함수가 다음 후보로 간다 → 호출 횟수가 3 보다 늘 수 있다).
  - 3번이 넘거나, `test+0x57777` (플래그 0 으로 다시 부르는 ret) 이 걸리면 위 `[추측]` 이 틀린 것.
- **`main` 입구의 `k`**: `test+0x1320`. 아래 프레임이 `test!__scrt_common_main_seh+0x10f` 인지 (정적 예측). `test+0x1b00` (`__scrt_common_main_seh` 입구) 에서도 `mainCRTStartup` 프레임이 없는지.
- 커널 디버거에서 `test.exe` 일 때만 서도록 조건을 붙이는 방법은 dast-mentor 몫. 위 `test+…` 는 모듈 이름 `test` 기준 (rva), 이 베이스는 실행마다 달라도 아래 16비트는 같다 (`…7723`).
- `ntdll!LdrpDispatchUserCallTarget` 쪽 CFG 비트맵 처리는 이번 정적으로는 안 봤다 (범위 밖).

## 판정

**분석성공.** 요청서 질문 1~6 모두 정적으로 답했다 (3번 "왜 세 번"만 `[추측]`, 4·5번의 CFG 통과 방식은 동적). 진입점 이름·PDB 서명·run7 의 `…7723` 모두 일치해서 올린 test.exe 와 VM 의 빌드는 같다고 본다.

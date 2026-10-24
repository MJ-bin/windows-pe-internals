# 35 dll-crt-entry — 정적 분석 결과

- 요청서: `_workspace/sast/35_dll-crt-entry.md`
- 모듈 / 바이너리: **dll1.dll, dll2.dll** (`sample/bin/` → `ms/bin/` 로 복사. MS 바이너리가 아님). 둘 다 이미지 베이스 `0x180000000`, SizeOfImage `0x8c000`.
  - dll1 sha256 앞 `ecea5143…` (550400바이트), 진입점 rva `0x1d60`.
  - dll2 sha256 앞 `455c874f…` (550912바이트), 진입점 rva `0x1d50`.
  - 둘 다 `/MT` (정적 CRT) + `/Od` + `/Z7` 빌드 `[소스]` → UCRT·vcruntime 이 dll 안에 들어 있다.
- PDB: `sample/bin/dll1.pdb`, `dll2.pdb` → `ms/sym/dll1.dll.pdb`, `ms/sym/dll2.dll.pdb` 로 복사 (MS 서버에서 받지 않음).
  - **dll 과 pdb 는 짝이 맞다.** dll 의 CodeView 서명과 pdb 정보 스트림(MSF 를 직접 읽음)이 글자까지 같다. dll1 `B1FF0915-FAC1-447A-AEB8-10FF10AD0564`, dll2 `FC67F681-4188-49D8-83B5-609127E1DE39`, 둘 다 age 1.
- 분석: Ghidra 11.3.2 analyze 각 21초. 함수 dll1 3333개, dll2 3332개. 로그 `ms/ghidra/dll1.analyze.log`, `dll2.analyze.log` 에 `SetPdb:` 줄 있음.
- 뽑은 함수: 모듈마다 13개 (depth 0 을 네 번 + 시작 사슬 depth 1 한 번) → `ms/decomp/dll1/`, `ms/decomp/dll2/`, `ms/disassemble/dll1/`, `ms/disassemble/dll2/`.
  - 사슬: `_DllMainCRTStartup`, `dllmain_dispatch`, `dllmain_crt_dispatch`, `dllmain_crt_process_attach`, `dllmain_crt_process_detach`, `dllmain_raw`, `DllMain`, `__scrt_initialize_crt`, `__scrt_dllmain_before_initialize_c`, `__scrt_dllmain_after_initialize_c`, `__security_init_cookie`, `_guard_dispatch_icall_nop`, 그리고 `try_get_function_slow`.
  - 호출 지점 `_calls.tsv` (모듈마다 66줄), 지도 `_map/dllmain_crt_process_attach_*` (뿌리 하나, 깊이 12), 참조 `_refs/` (`0x6e1f8`·`0x6e1e8` = `LoadLibraryExW` IAT 칸, `0x6e3a0` = `_pRawDllMain`, `0x84078` = `__dyn_tls_init_callback`, `DllMain`).
- 버전 확인 (MS 빌드 GUID 대조는 해당 없음, 대신 이것들):
  - dll 과 pdb 짝: **일치** (위).
  - 진입점 rva → PDB 이름: dll1 `0x1d60`, dll2 `0x1d50` 둘 다 `_DllMainCRTStartup`. 요청서의 pefile 값과 **일치**.
  - run6 `[관찰]` `DllMain` 은 두 dll 모두 rva `0x12e0` → `_index.tsv` 에서 둘 다 `DllMain 0x12e0` **일치**.
  - run8 `[관찰]` `LdrpCallInitRoutine` 의 `rcx` 가 `_DllMainCRTStartup` 과 정확히 일치 → 진입점 rva 가 `_DllMainCRTStartup` 이므로 **일치**.
  - sast/25 의 test.exe `try_get_function_slow` (433바이트, `LoadLibraryExW` call `+0x6d` / ret `+0x73`) 와 dll 쪽이 같은 모양 (아래 3번) **일치**.

## 스크립트 문제

고칠 버그는 없다. 불편했던 점만 적는다.

- 넣은 방법 (sast/25 와 같음): `cp -p sample/bin/dll1.dll ms/bin/dll1.dll`, `cp -p sample/bin/dll1.pdb ms/sym/dll1.dll.pdb` (dll2 도 같게). 그다음 `run_ghidra.sh analyze dll1.dll` 이 그대로 돌았다. `fetch_pdb.py` 는 쓰지 않았다 (MS 서버에 없는 파일이라). `pdb_aliases.py dll1.dll dll2.dll` 은 로컬 PDB 로 잘 돈다.
- `_guard_dispatch_icall_nop.asm` 이 **머리 한 줄뿐**이다. Ghidra 가 이 함수를 "1바이트짜리 인라인 함수"로 잡아서다 (`_index.tsv` size 1). 원본 바이트는 `ff e0` = `JMP RAX` (rva dll1 `0x6b9a0`, dll2 `0x6b990`). test.exe 의 `_index.tsv` 에도 같은 이름이 크기 1로 잡혀 있다. 읽는 데는 문제없다 (CFG 검사 없이 `rax` 로 점프하는 빈 껍데기). 크기 2짜리를 1로 잡는 건 Ghidra 쪽 일이라 고칠 곳은 없다.
- 지도(`map`)는 함수 포인터 표를 못 따라간다 (sast/25 에서 적은 것과 같음). 이번 CRT 안쪽은 한 상자 원칙이라 표 항목을 이어 붙이지 않았다.

## 한눈에

- 진입점은 **`_DllMainCRTStartup`**. 이름을 PDB 가 준다. 하는 일은 적다: 이유가 1 이면 `__security_init_cookie` 한 번, 그다음 **`JMP` 로 `dllmain_dispatch` 에 넘긴다** (꼬리 점프, 돌아올 자리 없음).
- 그래서 **콜스택엔 `_DllMainCRTStartup` 프레임이 안 보인다.** `DllMain` 을 부르는 줄은 **`dllmain_dispatch +0x91`** (ret `+0x96`) 이다.
- **`lpReserved`(R8) 는 `DllMain` 까지 바뀌지 않고 간다.** 중간에 값을 바꾸거나 0 으로 만드는 줄이 없다. (코드로는 `RDI` → `R8` → `RSI` → `R8` 로 옮겨 다닐 뿐이다.)
- `DllMain` 은 이유 1·2 일 때 **CRT 준비가 다 끝난 뒤** 불린다 (`dllmain_dispatch +0x76` 의 `dllmain_crt_dispatch` 가 먼저). run7 `[확인 필요]` 는 코드로 **맞다.**
- run7 `LoadLibraryExW` ×3 의 두 주소는 둘 다 **`try_get_function_slow +0x73`** 인데, 모듈이 갈린다: **`…f5fb` = dll2 (rva `0x4f5fb`)**, **`…f60b` = dll1 (rva `0x4f60b`)**. 즉 run7 9번(`0x7ffd80ebf60b`)은 dll2 가 아니라 **dll1** 이다. run7 정정 노트와 맞는다.
- dll1 과 dll2 는 이 사슬이 **명령어까지 같고** 함수 시작 주소만 다르다.

## 질문별 답 [정적]

### 1. 진입점 이름은 `_DllMainCRTStartup` 인가

- **답: 예.** dll1 rva `0x1d60`, dll2 rva `0x1d50` 의 PDB 이름이 둘 다 `_DllMainCRTStartup` (61바이트). 별칭 표(`ms/sym/dll1.dll.aliases.tsv`, dll2)에 이 자리 항목이 없어서 다른 이름과 겹치지 않는다.
- 타입 (PDB): `int __cdecl _DllMainCRTStartup(HINSTANCE__ *, ulong, void *)` 모양. 셋째 인자가 `void *` = `lpReserved` 자리.
- 근거: `ms/decomp/dll1/_index.tsv` (`_DllMainCRTStartup 180001d60 0x1d60 61`), `ms/decomp/dll2/_index.tsv` (`180001d50 0x1d50 61`), `ms/disassemble/dll1/_DllMainCRTStartup.asm`.

### 2. 진입점 → `DllMain` 사슬, `R8` 은 바뀌지 않고 가나

**함수 시작 rva (dll1 / dll2).** 안쪽 오프셋(`+0x..`)은 두 dll 이 똑같다.

| 함수 | dll1 | dll2 | 크기 |
|---|---|---|---|
| `_DllMainCRTStartup` | `0x1d60` | `0x1d50` | 61 |
| `dllmain_dispatch` | `0x1ba0` | `0x1b90` | 304 |
| `dllmain_crt_dispatch` | `0x1990` | `0x1980` | 86 |
| `dllmain_crt_process_attach` | `0x19f0` | `0x19e0` | 259 |
| `dllmain_crt_process_detach` | `0x1b00` | `0x1af0` | 140 |
| `DllMain` | `0x12e0` | `0x12e0` | 52 |

**사슬 한 줄 그림 (이유 = 1, 프로세스 시작).** 점프 목적지는 "주소 − 이 함수 시작"으로 적었다.

```
ntdll LdrpCallInitRoutineInternal --CALL--> _DllMainCRTStartup      (RCX=DllBase, EDX=1, R8=lpReserved)
  +0xf   RDI = R8        <- R8 을 RDI 에 맡김 (+0xa 에서 RDI 원래 값은 PUSH 해 둠)
  +0x1a  이유가 1 이 아니면 JNZ → +0x21 (쿠키 건너뜀)
  +0x1c  CALL __security_init_cookie (ret +0x21)                    ← 이유 1 일 때만
  +0x21  R8 = RDI        <- R8 되싣기 (EDX = EBX, RCX = RSI 도)
  +0x38  JMP → dllmain_dispatch +0x0    (자기 프레임 걷고 점프: 목적지 = 이 함수 시작 −0x1c0 = 다른 함수의 시작)
dllmain_dispatch                                                    (같은 RCX/EDX/R8, 돌아올 곳 = ntdll)
  +0x5   [RSP+0x18] = R8     <- 호출자 그림자 칸에 흘려 적기만 함 (이후 어디서도 안 읽음)
  +0x1b  RSI = R8            <- 여기에 맡김. EDI = EDX(이유), R14 = RCX(DllBase)
  +0x3f~+0x45  (이유 − 1) > 1 이면 JA → +0x89 (이유 0·3: CRT 준비 없이 바로 DllMain)
  이유 1·2:
  +0x47  _pRawDllMain 읽기 → 0 이라 +0x53 에서 EBX=1 로 건너뜀 (아래 3번째 항목)
  +0x6e  R8 = RSI, +0x71 EDX = EDI, +0x73 RCX = R14
  +0x76  CALL dllmain_crt_dispatch (ret +0x7b)
          dllmain_crt_dispatch: 이유 1 → +0x3b RDX = R8 (R8 을 둘째 인자 자리로 옮김)
                                +0x42 JMP → dllmain_crt_process_attach +0x0 (목적지 = 이 함수 시작 +0x60)
          dllmain_crt_process_attach: +0xa RSI = RDX (맡김) … CRT 준비 … +0xec RET (EAX=1) → 돌아가는 곳 dllmain_dispatch +0x7b
  +0x81~+0x83  EAX == 0 이면 JZ → +0x11e (DllMain 안 부르고 끝, 반환 0)
  +0x89  R8 = RSI, +0x8c EDX = EDI, +0x8e RCX = R14
  +0x91  CALL DllMain (ret +0x96)                                   ← R8 = 처음 받은 lpReserved
DllMain +0x0  [RSP+0x18] = R8 (흘려 적기만. 소스가 reserved 를 안 씀 [소스])
```

- **R8 이 바뀌지 않나: 예 (바뀌지 않고 간다).**
  - R8 을 쓰는 줄(= 값을 바꿀 수 있는 줄)은 위 그림의 `MOV R8,…` 뿐이다. 전부 `RDI`(진입점) 또는 `RSI`(dispatch)에서 **원래 값을 되싣는 줄**이다. 0 을 넣거나 다른 값을 넣는 줄이 없다.
  - `RDI`·`RSI` 는 호출 규약상 부른 함수가 지켜 주는 보존 레지스터다 (`_DllMainCRTStartup` 은 `+0xa PUSH RDI` / `+0x37 POP RDI`, `dllmain_dispatch` 는 `+0x13 PUSH RSI` / `+0x3d POP RSI` 로 자기 몫을 지킨다). `dllmain_dispatch` 에서 `RSI` 에 값을 넣는 줄은 `+0x1b` 하나뿐이다.
  - 값이 갈리는 갈래는 없다. 갈래마다 `DllMain` 호출 앞은 똑같이 `R8 = RSI` 이다: `dllmain_dispatch +0x89` (첫 `DllMain` 호출 `+0x91`), `+0xa5` (둘째 호출 `+0xad`, 아래 "실패 길"). 이유가 0·3 일 때도 `+0x45 JA → +0x89` 로 같은 줄에 온다.
- **`dllmain_dispatch` 의 그 밖의 줄:**
  - 이유 0 이고 `__proc_attached` 가 0 이하면 (`+0x23` `TEST EDX,EDX`, `+0x27` `CMP [__proc_attached],EDX`, `+0x2d` `JG → +0x3f`) `DllMain` 을 안 부르고 반환 0 (`+0x2f` `XOR EAX,EAX`, `+0x3e` `RET`). R8 과 상관없는 조건.
  - **실패 길** (`DllMain` 이 0 을 돌려주고 이유가 1 일 때: `+0x9c`~`+0xa3`): 이유 0 으로 `DllMain` 을 다시 부르고 (`+0xad`, R8 = RSI) → `+0xb2` `TEST RSI,RSI` / `+0xb5` `SETNZ CL` / `+0xb8` `CALL dllmain_crt_process_detach` → `_pRawDllMain` 이 있으면 (`+0xd1` `CALL [guard]`, R8 = RSI) 부른다. 우리 `DllMain` 은 항상 1 을 돌려주므로 `[소스]` 이 길은 안 탄다.
  - 이유 0·3 이면 `DllMain` 을 먼저 부르고 (`+0x91`), 그 **뒤에** `+0xe8` `CALL dllmain_crt_dispatch` (R8 = RSI, `+0xe0`). 순서가 이유 1·2 와 반대다.

**`fdwReason == 1` 일 때 `DllMain` 앞에서 부르는 CRT 함수 (이름과 call 자리, depth 1).**

- `_DllMainCRTStartup`: `__security_init_cookie` (`+0x1c` / ret `+0x21`).
- `dllmain_dispatch`: `_pRawDllMain` 읽기(`+0x47`) → **값 0** 이라 안 부름, `dllmain_crt_dispatch` (`+0x76` / ret `+0x7b`).
  - `_pRawDllMain` 칸(dll1 rva `0x6e3a0`, dll2 `0x6e390`)은 `.rdata` 안 읽기 전용이고 **파일 속 값이 0** 이다 (`ms/disassemble/dll1/_refs/0x6e3a0.tsv`: 읽는 곳 4곳, 쓰는 곳 0). 그래서 `dllmain_dispatch +0x4e` `TEST RAX,RAX` / `+0x51` `JNZ` 가 안 뛰고 `+0x53` `EBX = 1` 로 건너뛴다. `dllmain_raw`(`0x1ce0`)는 부르는 곳이 없는 사본이다 (본체가 `dllmain_dispatch` 안에 합쳐짐).
- `dllmain_crt_dispatch`: 이유 1 → `dllmain_crt_process_attach` 로 **`JMP`** (`+0x42`, 돌아올 자리 없음).
- `dllmain_crt_process_attach` (이름, call → ret, 부르는 순서):

| call | ret | 부르는 것 | 메모 |
|---|---|---|---|
| `+0x12` | `+0x17` | `__scrt_initialize_crt` (`ECX = 0`) | 안에서 `__isa_available_init` → `__vcrt_initialize` → `__acrt_initialize` (`.asm` 확인). 실패하면 `+0x19 JZ → +0xed` 로 반환 0 |
| `+0x1f` | `+0x24` | `__scrt_acquire_startup_lock` | |
| `+0x45` | `+0x4a` | `__scrt_dllmain_before_initialize_c` | `_initialize_onexit_table` ×2 |
| `+0x4e` | `+0x53` | `_RTC_Initialize` | |
| `+0x53` | `+0x58` | `__scrt_initialize_type_info` | |
| `+0x58` | `+0x5d` | `__scrt_initialize_default_local_stdio_options` | |
| `+0x6b` | `+0x70` | `_initterm_e` (`.CRT$XIA`~`XIZ`, rva `0x6e320`~`0x6e340`) | |
| `+0x74` | `+0x79` | `__scrt_dllmain_after_initialize_c` | `+0x20` 에서 `_configure_narrow_argv`, `+0x30` 에서 `_initialize_narrow_environment` |
| `+0x8b` | `+0x90` | `_initterm` (`.CRT$XCA`~`XCZ`, rva `0x6e310`~`0x6e318`) | |
| `+0xa0` | `+0xa5` | `__scrt_release_startup_lock` | |
| `+0xaa` | `+0xaf` | `__scrt_get_dyn_tls_init_callback` | 칸 `__dyn_tls_init_callback` (rva `0x84078`) 시작값 0 → `+0xb6 JZ → +0xd8` 로 건너뜀 |
| `+0xbb` | `+0xc0` | `__scrt_is_nonwritable_in_current_image` | 위 칸이 0 이 아닐 때만 |
| `+0xd2` | `+0xd8` | 동적 TLS 콜백 (`CALL [guard]`, `R8 = RSI`, `EDX = 2`, `RCX = R14`) | 위 칸이 0 이 아닐 때만 |
| `+0xd8` | | `INC [__proc_attached]` 후 `+0xde` `EAX = 1`, `+0xec` `RET` | |

  - 이미 시작한 적이 있으면 (`+0x2e` `CMP [__scrt_current_native_startup_state],0` / `+0x35 JNZ → +0xf9`) `+0xfe` `__scrt_fastfail` 로 죽는다.
  - 이 사슬 이름은 모두 `ms/disassemble/dll1/_calls.tsv` 에 있다. 괄호 안 안쪽 이름 몇 개는 `__scrt_initialize_crt`·`__scrt_dllmain_*` 를 따로 뽑은 `.asm` 에서 읽었다. 더 안쪽(`__acrt_*`)은 한 상자 원칙으로 안 따라갔다.
- **"`DllMain` 이 CRT 준비 뒤에 불린다" (run7 `[확인 필요]`): 맞다.** 이유 1 일 때 `DllMain` 호출(`dllmain_dispatch +0x91`)은 CRT 준비 호출(`+0x76` → `process_attach` 전체)이 **끝나고 돌아온 뒤**(`+0x7b`)에 있다. 한 가지만 다르게 읽어야 한다: `_DllMainCRTStartup` **자기 몸**이 하는 건 쿠키 준비뿐이고, 나머지 준비는 `dllmain_dispatch` 가 부르는 `dllmain_crt_process_attach` 몫이다.
  - 이유 2 (스레드 시작)도 같은 순서 (`CRT 먼저` → `DllMain`). `dllmain_crt_dispatch +0x2e` → `__scrt_dllmain_crt_thread_attach`. 이유 0·3 은 반대로 `DllMain` 먼저.
- **`lpReserved` 를 CRT 가 읽어서 정하는 줄:** **있다, 이유 0 (프로세스 끝) 쪽에만.** 이유 1 에서는 읽지 않고 옮기기만 한다.
  1. `dllmain_crt_dispatch +0x47` `TEST R8,R8` / `+0x4a` `SETNZ CL` / `+0x51` `JMP dllmain_crt_process_detach` — **이유 0 일 때** `R8 ≠ 0` 이면 `CL = 1`. 이 `CL` 이 `dllmain_crt_process_detach` 의 인자(`+0xa` `MOVZX EDI,CL`)가 되어 `+0x64` `MOVZX ECX,DIL` → `+0x68` `CALL __scrt_uninitialize_crt (ECX = 그 값, EDX = 0)` 로 간다. 이름으로는 "끝나는 중인가(`is_terminating`)" 자리 `[추측]`. 그 안쪽에서 무엇이 달라지는지는 안 따라갔다.
  2. `dllmain_dispatch +0xb2` `TEST RSI,RSI` / `+0xb5` `SETNZ CL` / `+0xb8` `CALL dllmain_crt_process_detach` — `DllMain` 이 이유 1 에서 0 을 돌려준 **실패 길**에서 같은 식.
  3. 이유 1 에서는 `dllmain_crt_dispatch +0x3b` `RDX = R8`, `process_attach +0xa` `RSI = RDX` 로 옮겨 맡기고, 동적 TLS 콜백이 있을 때만 `+0xc4` `R8 = RSI` 로 그 콜백에 넘긴다 (콜백 칸 시작값 0 이라 건너뜀). **값으로 무엇을 정하지는 않는다.**
- 근거 파일: `ms/disassemble/dll1/_DllMainCRTStartup.asm`, `dllmain_dispatch.asm`, `dllmain_crt_dispatch.asm`, `dllmain_crt_process_attach.asm`, `dllmain_crt_process_detach.asm`, `DllMain.asm`, `_calls.tsv`. 디컴파일 `ms/decomp/dll1/_DllMainCRTStartup.c` 는 "이유 1 이면 쿠키, 그다음 `dllmain_dispatch(셋째 인자 그대로)`" 한 덩어리로 그려서 위와 같다.

**콜스택 모양 (코드 기준)**

- `DllMain` 입구에서 돌아올 주소 = `dllmain_dispatch +0x96`. 그 아래가 곧 ntdll (`LdrpCallInitRoutineInternal` 쪽). `k` 에 **`_DllMainCRTStartup` 은 안 나온다** (`+0x38` `JMP` 로 프레임을 걷었으므로). `dllmain_crt_dispatch` 도 없다 (`process_attach` 로 `JMP`).
- 그러므로 run8 `[관찰]` "`rcx` 가 `_DllMainCRTStartup` 과 일치, 직후 `DllMain`" 은 맞지만, flow.md 2.8 의 `_DllMainCRTStartup → DllMain` 그림은 "`_DllMainCRTStartup` 입구 → (꼬리 점프) → `dllmain_dispatch` → `DllMain`" 으로 읽어야 한다. 호출 관계(콜스택)상 `DllMain` 의 부모는 `dllmain_dispatch`.

### 3. run7 의 `LoadLibraryExW` ×3 돌아올 주소 둘

**답: 둘 다 UCRT `try_get_function_slow +0x73` 이고, 모듈이 갈린다.**

| run7 `[관찰]` 주소 | 아래 16비트 | 맞는 dll | rva | 함수 + 오프셋 | 그때의 베이스 (주소 − rva) |
|---|---|---|---|---|---|
| 7번 `0x7ffd80e2f5fb` | `f5fb` | **dll2** | `0x4f5fb` | `try_get_function_slow +0x73` (call `+0x6d`) | `0x7ffd80de0000` |
| 9번 `0x7ffd80ebf60b` | `f60b` | **dll1** | `0x4f60b` | `try_get_function_slow +0x73` (call `+0x6d`) | `0x7ffd80e70000` |

- 방법: 두 dll 의 `.text` 에서 `CALL [IAT LoadLibraryExW]` 를 **바이트로 전부 훑고** (`ff 15` + 목적지 계산), Ghidra `refs` 결과와도 맞췄다 (둘이 같음). IAT 칸은 dll1 rva `0x6e1f8`, dll2 rva `0x6e1e8`. 이 칸을 부르는 `call` 은 모듈마다 9곳(ret 9개) + `JMP` 하나(`try_load_library_from_system_directory +0x64`, 돌아올 자리 없음). `LoadLibraryExW` 조각 함수(`JMP [IAT]`, dll1 rva `0x6b5fb`, dll2 `0x6b5eb`)를 `call` 하는 곳은 없다.
- ret rva 전체 (모두 `.text` 의 `0x4f4xx`~`0x4f9xx`):

| 모듈 | ret rva (아래 16비트) |
|---|---|
| dll1 | `0x4f4ba` `0x4f50d` **`0x4f60b`** `0x4f65f` `0x4f7a7` `0x4f7f7` `0x4f8b6` `0x4f909` `0x4f98b` |
| dll2 | `0x4f4aa` `0x4f4fd` **`0x4f5fb`** `0x4f64f` `0x4f797` `0x4f7e7` `0x4f8a6` `0x4f8f9` `0x4f97b` |

- 후보 `…f5fb`·`…f60b` (rva 후보 `0x0f5fb, 0x1f5fb, … 0x8f5fb` / `0x0f60b … 0x8f60b`) 중 **ret 자리가 되는 것:** dll1 에서 `f60b` **1개**·`f5fb` 0개, dll2 에서 `f5fb` **1개**·`f60b` 0개. 그 하나가 각각 `try_get_function_slow` (dll1 시작 rva `0x4f598`, dll2 `0x4f588`, 둘 다 433바이트) 의 `+0x73` 이다.
- **요청서가 먼저 물은 것 (dll2 가 run6 베이스 `0x7ffd80e70000` 그대로면 rva `0x4f60b`):** **아니다.** dll2 의 rva `0x4f60b` 는 `try_get_function_slow +0x83` 이고, 이 자리는 `+0x7f` 의 `CALL [GetLastError]` (6바이트, rva `0x4f607`~`0x4f60c`) **한가운데**라서 돌아올 주소가 될 수 없다. 반대로 dll1 의 rva `0x4f60b` 는 명령 경계이고 바로 앞이 `LoadLibraryExW` call (`ff 15 ed eb 01 00`) 이다.
  - 따라서 run7 9번 주소는 dll2 가 아니라 **dll1** 의 것이어야 하고, 그러면 그때 dll1 베이스가 `0x7ffd80e70000` (run6 에서 dll2 가 있던 자리). 이는 run7 정정 노트("run7 에서 dll1 이 run6 의 dll2 자리에 올라옴")와 맞는다.
  - 같은 베이스로 run7 10번 `dll2!DllMain`(`0x7ffd80e712e0`)을 풀면 `0x7ffd80e70000 + 0x12e0` = **dll1 의 `DllMain`** — 같은 증거가 한 번 더 나온다.
  - 7번이 dll2 라면 dll2 베이스는 `0x7ffd80de0000` (64KB 정렬 맞음). 두 이미지 범위(`0x7ffd80de0000`~`0x7ffd80e6c000`, `0x7ffd80e70000`~`0x7ffd80efc000`)는 겹치지 않는다.
  - 초기화 순서 **dll2 (7번) → dll1 (9번)** 도 run8 `[관찰]` (dll2 → dll1 → exe) 과 같다.
- **"모듈 미확인" 풀림:** `0x7ffd80e2f5fb` = **dll2**. (이 주소의 아래 16비트 `f5fb` 는 dll1 에는 ret 자리로 없고 dll2 에만 있다.)
- 이 함수가 하는 일은 sast/25 3-2 와 같다 (`LoadLibraryExW(이름, NULL, 0x800)`, 모듈 캐시 `module_handles` / 이름 표 `module_names` 는 dll1 rva `0x834a0` / `0x708b0`, dll2 `module_names` 만 `0x708a0` 으로 다름 — 데이터 위치 차이뿐). 호출 줄은 `+0x6d` call `[IAT]`, 앞에서 `R8D = 0x800` (`+0x67`), `EDX = 0` (`+0x62`), `RCX = module_names[id]` (`+0x5a` 로 읽은 값, `+0x64` 에서 옮김).
- 근거: `ms/disassemble/dll1/try_get_function_slow.asm` `+0x5a`~`+0x73`, `dll2/try_get_function_slow.asm` 같은 줄, `_refs/0x6e1f8.tsv`, `dll2/_refs/0x6e1e8.tsv`.

**세 번이 DllMain 앞인가:** 그렇다고 읽힌다. 지도를 `dllmain_crt_process_attach` 뿌리로 돌렸을 때 `try_get_function_slow` 로 가는 직접 호출 길은 모두 이 함수 아래였다 (`__scrt_initialize_crt` `+0x12` / `_initterm_e` `+0x6b` / `__scrt_dllmain_after_initialize_c` `+0x74` 가 뿌리 쪽 첫 칸). 이 함수는 `dllmain_dispatch +0x76` 의 `dllmain_crt_dispatch` 에서 `JMP` 로 들어가므로 `DllMain` 호출(`+0x91`)보다 앞이다. `DllMain` 안의 `Log` 는 소스가 Win32 API 만 쓰고 CRT 함수는 안 쓴다고 해서 `[소스]` (`sample/log.h`) 그 아래로는 이 길이 안 생긴다 (`Log` 본문은 안 뽑음). 세 길 중 지도(직접 호출만)가 잡은 것:
  - `AreFileApisANSI`: `process_attach +0x74` → `__scrt_dllmain_after_initialize_c +0x20` → `_configure_narrow_argv +0x55` → `__acrt_GetModuleFileNameA +0xad` → `__acrt_AreFileApisANSI +0x2d` → `try_get_function_slow` (ret `+0x32`).
  - `LCMapStringEx`: 같은 `+0x74` → `+0x20` → `_configure_narrow_argv +0x3e` → `__acrt_initialize_multibyte +0x4d` → `setmbcp_internal +0x168` → `_setmbcp_nolock +0x291` → `setSBUpLow +0x103` → `__acrt_LCMapStringA +0x6c` → `__acrt_LCMapStringA_stat +0x164` → `__acrt_LCMapStringEx +0x4a`.
  - `FlsGetValue2` (sast/25 길 A) 는 함수 포인터 표(`__acrt_execute_initializers`) 를 거쳐서 지도가 못 따라간다 → 이 dll 에서 같은 길인지는 **안 따라감** (한 상자 원칙). 위 둘은 지도 기준이고 `.asm` 로 읽은 건 `+0x20` 줄 하나다.

### 4. dll1 과 dll2 의 진입점 쪽 구조가 같은가

**같다.** 같은 CRT 함수 사슬, 같은 명령, 같은 함수 안 오프셋. 위 함수 아홉 개(`_DllMainCRTStartup`, `dllmain_dispatch`, `dllmain_crt_dispatch`, `dllmain_crt_process_attach`, `dllmain_crt_process_detach`, `dllmain_raw`, `DllMain`, `try_get_function_slow`, `__security_init_cookie`)의 `.asm` 를 절대 주소만 지우고 비교하면 **머리 줄 말고 같다** (`try_get_function_slow +0x5a` 의 `module_names` 칸 `0x708b0` ↔ `0x708a0` 하나만 데이터 위치 차이).

- 다른 것은 함수 시작 주소가 `DllMain`(같음, `0x12e0`) 이후부터 dll2 가 **`0x10` 앞** 이라는 점 (표 위). `try_get_function_slow` 도 `0x4f598` ↔ `0x4f588`. 그래서 같은 함수 `+0x73` 이어도 ret rva 가 dll1 `0x4f60b`, dll2 `0x4f5fb` 로 갈린다.
- 데이터 칸 위치도 `0x10` 차이 (`_pRawDllMain` dll1 `0x6e3a0` ↔ dll2 `0x6e390`, `__guard_dispatch_icall_fptr` dll1 `0x6e2e8` ↔ dll2 `0x6e2d8`). 다만 `__dyn_tls_init_callback` 칸은 둘 다 rva `0x84078`.

## 동적으로 확인할 것 (Q27·Q31 에 붙일 멈춤점 추천)

dll 의 베이스는 실행마다 바뀐다 (run8: 재부팅 없이도 run6 과 달라져 `dll1` `…4f190000`, `dll2` `…4ea20000`). 고정 주소가 아니라 **`lm` 으로 그때의 베이스를 얻어 `베이스 + rva`** 로 걸어야 한다 (run8 의 BEFORE-GRAPH 방법). 아래 rva 는 이 PDB 짝 dll 기준.

| 무엇을 보나 | dll1 | dll2 | 볼 것 |
|---|---|---|---|
| `_DllMainCRTStartup` 입구 (Q27) | `dll1+0x1d60` | `dll2+0x1d50` | `r8` = `lpReserved`, `edx` = 1. ntdll 쪽에서 본 `LdrpCallInitRoutineInternal` 의 `r8`(= `r9` 원본)과 같은 값인가. `@r8` 이 첫 스레드 `CONTEXT` 포인터인가 (sast/23) |
| `dllmain_dispatch` 입구 | `dll1+0x1ba0` | `dll2+0x1b90` | `r8` 이 위와 같은가 (`+0x38` 꼬리 점프 뒤) |
| `DllMain` 을 부르기 직전 | `dll1+0x1c31` (call 자리) | `dll2+0x1c21` | `r8`, `edx`=1, `rcx`=DllBase. 이 줄에서 `k` 가 `dll?!dllmain_dispatch` 이고 `_DllMainCRTStartup` 은 없는지 |
| `DllMain` 입구 | `dll1+0x12e0` | `dll2+0x12e0` | `r8` 이 위 두 값과 같은가 (**답: 같다고 코드가 말함**). `k` 의 돌아올 주소 `dll?+0x1c36` (`dllmain_dispatch +0x96`) |
| `DllMain` 돌아온 직후 | `dll1+0x1c36` | `dll2+0x1c26` | `eax` = 1 |
| CRT 준비 호출 끝난 자리 (`DllMain` 앞) | `dll1+0x1c1b` | `dll2+0x1c0b` | `eax` = 1 이면 `DllMain` 으로 감. 이 줄이 `LoadLibraryExW` ×3 보다 **뒤**인가 (run7 `[확인 필요]` 를 순서로) |
| `LoadLibraryExW` ×3 돌아올 주소 (Q31) | `dll1+0x4f60b` | `dll2+0x4f5fb` | `KERNELBASE!LoadLibraryExW` 입구에서 `poi(@rsp)` 가 이 값인 것만 세고, `du @rcx` 로 이름 3개를 본다. 위 표처럼 "run7 7번 = dll2, 9번 = dll1" 이 맞는지도 이 값으로 확인 가능 |
| 동적 TLS 콜백 칸 (선택) | `dll1+0x84078` | `dll2+0x84078` | `dq` 로 0 인지 (0 이면 `process_attach +0xb6` 에서 건너뜀) |

- 이 dll 안에서 `R8` 이 중간에 바뀔 곳은 코드로 없다. 그래도 값을 한 번 확인할 가치는 있다: **ntdll 이 실제로 넘긴 `r8` 이 `CONTEXT` 포인터인지** (Q27) 가 아직 동적 확인 대기이기 때문.

## 새로 생긴 정적 공백

1. **`FlsGetValue2` 길 (함수 포인터 표 `__acrt_execute_initializers` 쪽)** 이 dll 에서도 `try_get_function_slow` 로 가는지: 안 따라갔다 (한 상자 원칙, 지도가 표를 못 따라감). 3번 중 두 길(AreFileApisANSI, LCMapStringEx)은 지도로 잡았고, 정확히 어떤 세 길이 dll 에서 세 번이 되는지는 동적(Q31, 인자 이름)으로 본다.
2. **`__scrt_uninitialize_crt(ECX = lpReserved ≠ 0, EDX = 0)` 안쪽**: 이유 0 에서 `R8` 이 이 첫 인자의 근원인데 안에서 뭐가 달라지는지는 안 읽었다. 대표 지점 목표(진입점 도달)엔 덜 중요.
3. **`dllmain_crt_dispatch` 의 이유 2·3 (스레드) 길**의 `__scrt_dllmain_crt_thread_attach/detach` 는 안 읽었다 (범위 밖).
4. **flow.md 2.8 그림**: `_DllMainCRTStartup → DllMain` 은 `_DllMainCRTStartup` 입구 → (꼬리 점프) → `dllmain_dispatch` → `DllMain`, 콜스택 부모는 `dllmain_dispatch +0x96` 으로 고쳐야 한다. 같은 곳 4장 "dll1·dll2 시작 함수 안 `LoadLibraryExW` ×3" 줄은 `dllmain_dispatch +0x76` → `process_attach` 안 (`DllMain` 앞) 이며, run7 두 주소는 `…f5fb` = dll2, `…f60b` = dll1 이다.

## 판정

**분석성공.** 네 질문 모두 코드로 답이 나왔다. 실행 때의 실제 `r8` 값(`CONTEXT` 포인터 여부)과 `LoadLibraryExW` 세 번의 이름은 동적(Q27·Q31)이다.

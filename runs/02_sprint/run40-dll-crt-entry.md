# 2026-10-24 run40 — dll1·dll2 진입점 `_DllMainCRTStartup` 은 쿠키만 하고 **`JMP` 로 `dllmain_dispatch` 에 넘긴다** (프레임 안 남음). CRT 준비가 끝난 뒤 `dllmain_dispatch +0x91` (ret `+0x96`) 에서 `DllMain`, `lpReserved` 는 그대로. run7 `LoadLibraryExW` 두 자리 = **dll2·dll1 `try_get_function_slow +0x73`** (정적)

> **지도 범위**: 2026-10-23 결정 "진입점 = 경계, CRT 한 상자" 는 dll 에도 그대로다. 진입점 뒤 CRT 함수(`dllmain_dispatch` 등)는 이 문서와 flow.md 2.8 "CRT 안쪽 (참고)" 에만 적고, 그림 2 에는 안 넣는다.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 정적: **dll1.dll, dll2.dll**. MS 바이너리가 아니라 sample 빌드다 (`sample/bin/` → `ms/bin/` 로 복사, `.gitignore`).
  - pdb: 로컬 `sample/bin/dll1.pdb`, `dll2.pdb` (MS 서버에서 안 받음).
    - dll1 서명 `B1FF0915-FAC1-447A-AEB8-10FF10AD0564` age 1, dll2 `FC67F681-4188-49D8-83B5-609127E1DE39` age 1.
    - dll 의 CodeView 서명과 pdb 정보 스트림이 글자까지 같다 (결과서 — MSF 를 직접 읽음). **짝이 맞다.**
  - 둘 다 이미지 베이스 `0x180000000`, SizeOfImage `0x8c000`. Ghidra 첫 analyze, 함수 dll1 3333개, dll2 3332개.
  - `[소스]` `sample/build.bat`: `/Z7 /Od /MT`, `/LD`. `/MT` 라서 UCRT·vcruntime 이 dll 안에 들어 있다.
- 버전 대조 (결과서 + 01_sprint 기록):
  - 진입점 rva → PDB 이름: dll1 `0x1d60`, dll2 `0x1d50` 둘 다 `_DllMainCRTStartup` (61바이트). 요청서의 `pefile` 값과 같다.
  - run6 `[관찰]` `DllMain` 두 dll 모두 rva `0x12e0` → `_index.tsv` 둘 다 `DllMain 0x12e0`. 같다.
  - run8 `[관찰]` `LdrpCallInitRoutine` 의 `rcx` = `_DllMainCRTStartup` → 진입점 rva 의 PDB 이름과 같다.
  - run7 `[관찰]` `LoadLibraryExW` 돌아올 주소 두 개의 아래 16비트가 각각 **한 dll 에만** ret 자리로 있다 (아래 3). VM 에서 돈 dll 이 이 빌드라는 데 보탬이 되는 값이다. 다만 01_sprint 기록만으로 "같은 빌드" 까지 단정하진 않는다 `[추측]`.

| 함수 | dll1 rva | dll2 rva | 크기 | 이번에 본 것 |
|---|---|---|---|---|
| `_DllMainCRTStartup` | `0x1d60` | `0x1d50` | 61 | 전체 |
| `dllmain_dispatch` | `0x1ba0` | `0x1b90` | 304 | 전체 (`R8` 흐름, 호출 자리) |
| `dllmain_crt_dispatch` | `0x1990` | `0x1980` | 86 | 전체 |
| `dllmain_crt_process_attach` | `0x19f0` | `0x19e0` | 259 | 호출 자리 (이름만) |
| `dllmain_crt_process_detach` | `0x1b00` | `0x1af0` | 140 | `lpReserved` 가 닿는 줄만 |
| `DllMain` | `0x12e0` | `0x12e0` | 52 | 첫 줄 (`R8` 을 받는 쪽) |
| `try_get_function_slow` | `0x4f598` | `0x4f588` | 433 | `LoadLibraryExW` 호출 줄 |

- 안쪽 오프셋(`+0x..`)은 두 dll 이 **명령까지 같다** (결과서가 절대 주소만 지우고 비교). 함수 시작 rva 만 `DllMain` 뒤로 dll2 가 `0x10` 앞이다. 아래 오프셋은 둘 다에 맞다.

## 목적

- Q55 (run28·run30 flow-writer 후보):
  1. dll1·dll2 의 PE 진입점 이름이 `_DllMainCRTStartup` 인가.
  2. 진입점 → `DllMain` 사슬, 그리고 셋째 인자 `lpReserved`(진입 때 `R8`) 가 `DllMain` 의 `R8` 까지 **바뀌지 않고** 가나. `DllMain` 이 CRT 준비 **뒤에** 불리나 (run7 `[확인 필요]`).
  3. run7 의 `LoadLibraryExW` ×3 돌아올 주소 두 개 (`0x7ffd80e2f5fb`, `0x7ffd80ebf60b`) 는 어느 dll 의 어느 함수인가.
  4. dll1·dll2 진입점 쪽 구조가 같은가.

## 방법

- 정적: `_workspace/sast/35_dll-crt-entry.md` — dll1·dll2 각각 `^_DllMainCRTStartup$` depth 1, `^dllmain_dispatch$` depth 1, `^DllMain$`·`^try_get_function_slow$` depth 0. 모듈마다 13개 뽑음.
  - 넣은 방법은 test.exe (sast/25) 와 같다: `cp -p` 로 dll·pdb 를 `ms/bin/`·`ms/sym/` 에 놓고 `run_ghidra.sh analyze`. `fetch_pdb.py` 는 안 씀.
  - `LoadLibraryExW` 를 부르는 곳은 IAT 칸 rva (dll1 `0x6e1f8`, dll2 `0x6e1e8`) 로 찾았다 (`_refs/`). 결과서는 `.text` 의 `ff 15` 바이트도 전부 훑어 Ghidra 결과와 맞췄다 (같음).
  - 지도 `_map/dllmain_crt_process_attach_*` (뿌리 하나, 깊이 12) — `try_get_function_slow` 로 가는 길.
- 결과서: `_workspace/sast/35_dll-crt-entry_result.md` (**분석성공**)
- 이 문서를 쓰며 `ms/disassemble/dll1/` 의 `_DllMainCRTStartup.asm` 전체, `dllmain_dispatch.asm` 호출·`R8` 줄, `dllmain_crt_dispatch.asm` `+0x3b`·`+0x42`·`+0x47`·`+0x51`, `dllmain_crt_process_attach.asm` 호출 줄, 두 dll `try_get_function_slow.asm` `+0x67`·`+0x6d`·`+0x73`·`+0x7f` 를 다시 맞춰 봤다 (결과서와 같음).

## 결과

비유 (회사 접수처):

- `_DllMainCRTStartup` = 정문 경비. 출입증에 도장(보안 쿠키)만 찍고, **자기 자리를 비운 채** 접수처(`dllmain_dispatch`)로 바로 들여보낸다. 그래서 나중에 "누가 데려왔나" 를 거꾸로 따라가면 경비는 안 나온다.
- `dllmain_dispatch` = 접수처. 먼저 사무실 준비(CRT 준비)를 다 시킨 뒤, 손님(우리 `DllMain`)을 부른다. 손님에게 줄 서류(`lpReserved`)는 **받은 그대로** 건넨다.

### 정적 — 호출 지점 (`ms/disassemble/dll1/_calls.tsv`, dll2 같은 오프셋)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpCallInitRoutineInternal` | `+0x1d` | `+0x22` | `dllN!_DllMainCRTStartup` (CFG 중개는 `JMP` 뿐) | [정적] (run28) |
| `dllN!_DllMainCRTStartup` | `+0x1c` | `+0x21` | `__security_init_cookie` (이유 1 일 때만) | [정적] |
| `dllN!_DllMainCRTStartup` | `+0x38` | — (꼬리 `JMP`) | **`dllN!dllmain_dispatch`** | [정적] |
| `dllN!dllmain_dispatch` | `+0x76` | `+0x7b` | `dllmain_crt_dispatch` (이유 1·2: `DllMain` **앞**) | [정적] |
| `dllN!dllmain_crt_dispatch` | `+0x42` | — (꼬리 `JMP`) | `dllmain_crt_process_attach` (이유 1) | [정적] |
| `dllN!dllmain_dispatch` | `+0x91` | **`+0x96`** | **`dllN!DllMain`** | [정적] |
| `dllN!dllmain_dispatch` | `+0xad` | `+0xb2` | `DllMain` (이유 0 — 이유 1 에서 `DllMain` 이 0 을 돌려준 실패 길) | [정적] |
| `dllN!dllmain_dispatch` | `+0xb8` | `+0xbd` | `dllmain_crt_process_detach` (같은 실패 길) | [정적] |
| `dllN!dllmain_dispatch` | `+0xe8` | `+0xed` | `dllmain_crt_dispatch` (이유 0·3: `DllMain` **뒤**) | [정적] |
| `dllN!dllmain_crt_dispatch` | `+0x51` | — (꼬리 `JMP`) | `dllmain_crt_process_detach` (이유 0) | [정적] |
| `dllN!try_get_function_slow` | `+0x6d` | **`+0x73`** | `LoadLibraryExW` (`CALL [IAT]`, `(이름, NULL, 0x800)`) | [정적] |

- `dllmain_dispatch` 의 간접 호출 셋 (`+0x5a`·`+0xd1`·`+0x112`) 은 `_pRawDllMain` 칸 쪽이다. 칸 값이 파일에서 0 이라 우리 길에선 안 탄다 (아래 2).

`dllmain_crt_process_attach` 가 부르는 것 (이유 1, 부르는 순서, call → ret):

| call | ret | callee | 메모 |
|---|---|---|---|
| `+0x12` | `+0x17` | `__scrt_initialize_crt` (`ECX = 0`) | 실패면 `+0x19 JZ +0xed` 로 0 반환 |
| `+0x1f` | `+0x24` | `__scrt_acquire_startup_lock` | |
| `+0x45` | `+0x4a` | `__scrt_dllmain_before_initialize_c` | |
| `+0x4e` | `+0x53` | `_RTC_Initialize` | |
| `+0x53` | `+0x58` | `__scrt_initialize_type_info` | |
| `+0x58` | `+0x5d` | `__scrt_initialize_default_local_stdio_options` | |
| `+0x6b` | `+0x70` | `_initterm_e` (`.CRT$XI`) | |
| `+0x74` | `+0x79` | `__scrt_dllmain_after_initialize_c` | 안에서 `_configure_narrow_argv` 등 |
| `+0x8b` | `+0x90` | `_initterm` (`.CRT$XC`) | |
| `+0xa0` | `+0xa5` | `__scrt_release_startup_lock` | |
| `+0xaa` | `+0xaf` | `__scrt_get_dyn_tls_init_callback` | 칸(rva `0x84078`) 시작값 0 → `+0xb6 JZ +0xd8` 로 건너뜀 |
| `+0xd2` | `+0xd8` | 동적 TLS 콜백 (CFG 간접, `R8 = RSI`) | 위 칸이 0 아닐 때만 |
| — | — | `+0xd8 INC [__proc_attached]`, `+0xde EAX = 1`, `+0xec RET` | |

- 이미 시작한 적이 있으면 (`+0x2e`·`+0x35 JNZ +0xf9`) `+0xfe __scrt_fastfail` 로 죽는다.
- 안쪽(`__acrt_*` 표 등)은 한 상자 원칙으로 안 따라갔다.

### 정적 — `lpReserved` (`R8`) 가 가는 길 (이유 1)

```
ntdll LdrpCallInitRoutineInternal +0x1d ──CALL──▶ _DllMainCRTStartup   (RCX = DllBase, EDX = 1, R8 = lpReserved)
  +0xf   RDI = R8                     맡김 (+0xa 에서 RDI 원래 값은 PUSH)
  +0x1a  이유 ≠ 1 이면 JNZ +0x21
  +0x1c  __security_init_cookie        (ret +0x21)
  +0x21  R8 = RDI                      되싣기 (EDX = EBX, RCX = RSI 도)
  +0x37  POP RDI → +0x38 JMP ══▶ dllmain_dispatch    (자기 프레임을 걷고 점프. 돌아올 곳 = ntdll 그대로)
dllmain_dispatch
  +0x5   [RSP+0x18] = R8               호출자 그림자 칸에 흘려 적기만 (다시 안 읽음)
  +0x1b  RSI = R8                      맡김. RSI 에 쓰는 줄은 이것 하나
  +0x3f ~ +0x45  (이유 − 1) > 1 이면 JA +0x89   (이유 0·3: CRT 준비 없이 바로 DllMain 쪽)
  +0x47  _pRawDllMain 읽기 → 0 → +0x53 EBX = 1   (안 부름)
  +0x6e  R8 = RSI → +0x76 dllmain_crt_dispatch   (ret +0x7b)
           └─ +0x3b RDX = R8 → +0x42 JMP ══▶ dllmain_crt_process_attach (+0xa RSI = RDX 맡김 … +0xec RET, EAX = 1)
  +0x81 ~ +0x83  EAX == 0 이면 JZ +0x11e      (DllMain 안 부르고 0 반환)
  +0x89  R8 = RSI, +0x8c EDX = EDI, +0x8e RCX = R14
  +0x91  CALL DllMain                  (ret +0x96)   ★ R8 = 처음 받은 lpReserved
DllMain +0x0  [RSP+0x18] = R8          흘려 적기만. 소스는 reserved 를 안 씀 [소스]
```

### 호출 관계 (콜스택 모양, 코드 기준)

```
ntdll!LdrpInitializeNode +0x19c
 └─ LdrpCallInitRoutine +0x93
     └─ LdrpCallInitRoutineInternal +0x22
         └─ dllN!_DllMainCRTStartup              ← 입구에서만 보임
              ├─ +0x21  __security_init_cookie
              └═ +0x38 JMP (꼬리) ═▶ dllN!dllmain_dispatch      ← 여기부터 _DllMainCRTStartup 프레임 없음
                   ├─ (1) +0x7b  dllmain_crt_dispatch ═▶ (JMP) dllmain_crt_process_attach
                   │              └─ … CRT 준비 … └─ try_get_function_slow +0x73 → LoadLibraryExW ×3 (run7)
                   └─ (2) +0x96  dllN!DllMain (1)
```

- `DllMain` 입구에서 `k` 를 찍으면 (예상): `dllN!DllMain` ← **`dllN!dllmain_dispatch+0x96`** ← `ntdll!LdrpCallInitRoutineInternal+0x22` ← `LdrpCallInitRoutine+0x93` ← `LdrpInitializeNode+0x19c`.
  - **`_DllMainCRTStartup` 은 안 나온다** (꼬리 `JMP`). `dllmain_crt_dispatch` 도 없다 (`process_attach` 로 `JMP`). 이렇게 안 나오면 꼬리 점프 가정이 틀린 것 → Q27.

### 정적 — run7 `LoadLibraryExW` 돌아올 주소 두 개

| run7 `[관찰]` | 아래 16비트 | 모듈 | rva | 함수 + 오프셋 | 그때 베이스 (주소 − rva) |
|---|---|---|---|---|---|
| 7번 `0x7ffd80e2f5fb` | `f5fb` | **dll2** | `0x4f5fb` | `try_get_function_slow +0x73` (call `+0x6d`) | `0x7ffd80de0000` |
| 9번 `0x7ffd80ebf60b` | `f60b` | **dll1** | `0x4f60b` | `try_get_function_slow +0x73` (call `+0x6d`) | `0x7ffd80e70000` |

- 어떻게 갈렸나:
  - dll 은 64KB 정렬로 올라가니 rva 의 아래 16비트는 주소와 같다.
  - `LoadLibraryExW` IAT 칸을 부르는 `call` 은 모듈마다 9곳이다 (+ 꼬리 `JMP` 1곳, 돌아올 자리 없음). 그 ret rva 9개 중 `…f5fb` 는 **dll2 에만**, `…f60b` 는 **dll1 에만** 있다 (결과서 표).
  - dll2 의 rva `0x4f60b` 는 `+0x7f CALL [GetLastError]` (rva `0x4f607` ~ `0x4f60c`) **한가운데**라 돌아올 주소가 될 수 없다 (`.asm` 확인). 그래서 run7 9번을 "dll2 범위 안" 으로 읽은 건 틀렸다 (아래 정정).
- 맞물림 셋:
  - 9번 베이스 `0x7ffd80e70000` 은 run6 에서 dll2 가 있던 자리다. run7 맨 위 정정 노트 "dll1 이 run6 의 dll2 자리에 올라옴" 과 같다.
  - 같은 베이스로 run7 10번 `dll2!DllMain` (`ba` 를 건 run6 주소 `0x7ffd80e712e0`) 을 풀면 `0x7ffd80e70000 + 0x12e0` = **dll1 의 `DllMain`** 이다.
  - 7번(dll2) → 9번(dll1) 순서는 run8 `[관찰]` 초기화 순서 dll2 → dll1 → exe 와 같다. 두 베이스의 이미지 범위(`0x7ffd80de0000 ~ 0x7ffd80e6c000`, `0x7ffd80e70000 ~ 0x7ffd80efc000`) 는 겹치지 않는다.
- 세 번이 `DllMain` **앞**인 근거 `[정적]`: 지도를 `dllmain_crt_process_attach` 뿌리로 돌렸을 때 `try_get_function_slow` 로 가는 직접 호출 길이 모두 이 함수 아래다 (`__scrt_initialize_crt +0x12`, `_initterm_e +0x6b`, `__scrt_dllmain_after_initialize_c +0x74` 가 첫 칸). 이 함수는 `dllmain_dispatch +0x76` 에서 들어가므로 `+0x91 DllMain` 보다 앞이다.
  - 지도가 잡은 길 둘 (call 자리): `AreFileApisANSI` — `process_attach +0x74` → `__scrt_dllmain_after_initialize_c +0x20` → `_configure_narrow_argv +0x55` → `__acrt_GetModuleFileNameA +0xad` → `__acrt_AreFileApisANSI +0x2d`. `LCMapStringEx` — 같은 `+0x74` → `+0x20` → `_configure_narrow_argv +0x3e` → `__acrt_initialize_multibyte` → … → `__acrt_LCMapStringEx +0x4a`.
  - 셋째 길 `FlsGetValue2` (test.exe 의 길 A) 는 함수 포인터 표를 거쳐 지도가 못 따라간다. dll 에서도 같은 길인지는 안 봤다 (한 상자 원칙).
- 하는 일은 test.exe 의 같은 함수(run30)와 같다: `+0x5a` 이름 표 `module_names[id]` → `+0x62 EDX = 0`, `+0x67 R8D = 0x800` → `+0x6d LoadLibraryExW(이름, NULL, 0x800)`. 데이터 칸 위치만 다르다 (`module_names` dll1 `0x708b0`, dll2 `0x708a0`).

### 그림 반영 (flow.md 그림 2)

- dll2·dll1 진입점 상자 `f1`·`i1` 을 **CRT 경계 상자**로 바꿨다 (test.exe 진입점 상자 `m1` 과 같은 모양, 점선). 상자 글에 rva·꼬리 `JMP`·`DllMain` 의 부모 `dllmain_dispatch +0x96`·`lpReserved` 그대로를 적었다. CRT 함수 노드는 늘리지 않았다.
- 진입점 상자 → `DllMain` 화살표는 그대로 `?` 다. 사이에 `dllmain_dispatch` 가 끼어 있고, 실행으로 본 적이 없다.
- 진입점 상자 → **`KERNELBASE!LoadLibraryExW ×3`** 화살표를 dll 마다 하나씩 넣었다 (`(1) ?`, 그다음 `(2) ?` `DllMain`). run7 `[관찰]` 의 두 자리가 어느 dll 것인지 이번에 갈려서다. exe 상자 `m1` 과 같은 규칙 (CRT 가 ntdll 로더를 다시 부르는 것만 화살표).

## 확인된 것

- `[정적]` dll1·dll2 의 PE 진입점 = PDB `_DllMainCRTStartup` (dll1 `0x1d60`, dll2 `0x1d50`). run8 `[관찰]` `rcx` 와 같다.
- `[정적]` `_DllMainCRTStartup` 은 이유 1 이면 `+0x1c __security_init_cookie` 하나만 부르고 **`+0x38` 꼬리 `JMP` 로 `dllmain_dispatch`** 에 넘긴다. 콜스택에 `_DllMainCRTStartup` 프레임이 안 남는다.
- `[정적]` `DllMain` 을 부르는 줄 = **`dllmain_dispatch +0x91`** (ret `+0x96`). 콜스택상 `DllMain` 의 부모는 `dllmain_dispatch` 다.
- `[정적]` **`lpReserved` 는 `DllMain` 까지 바뀌지 않는다.** `R8` 을 쓰는 줄은 모두 맡겨 둔 보존 레지스터(`RDI`, `RSI`)에서 되싣는 줄이다. 0 이나 다른 값을 넣는 갈래가 없다.
- `[정적]` 이유 1·2 는 **CRT 준비(`+0x76`) 가 끝나고 돌아온 뒤** `DllMain` (`+0x91`). run7 `[확인 필요]` "DLL 시작 함수가 CRT 준비를 먼저 하고, 그다음 DllMain" 은 코드로 **맞다.** 다만 `_DllMainCRTStartup` 자기 몸은 쿠키만 하고, 준비는 `dllmain_dispatch` → `dllmain_crt_process_attach` 몫이다.
  - 이유 0·3 은 반대로 `DllMain` (`+0x91`) **먼저**, 그 뒤 `+0xe8 dllmain_crt_dispatch`.
- `[정적]` CRT 가 `lpReserved` 를 **읽어서 정하는 곳은 이유 0 (프로세스 끝) 에만** 있다: `dllmain_crt_dispatch +0x47 TEST R8,R8` / `+0x4a SETNZ CL` → `dllmain_crt_process_detach` → `+0x68 __scrt_uninitialize_crt(ECX = 그 값, EDX = 0)`. 실패 길 `dllmain_dispatch +0xb2 TEST RSI,RSI` 도 같은 식. 이유 1 에선 옮기기만 한다 (동적 TLS 콜백이 있을 때만 그 콜백에 넘김 — 칸 시작값 0).
- `[정적]` `_pRawDllMain` (dll1 rva `0x6e3a0`, dll2 `0x6e390`) 은 `.rdata` 읽기 전용, 파일 값 0, 쓰는 곳 0. 그래서 `dllmain_dispatch +0x51 JNZ` 가 안 뛰고 `+0x53` 으로 건너뛴다. `dllmain_raw` (dll1 `0x1ce0`) 는 부르는 곳이 없는 사본이다.
- `[정적]` + `[관찰]` run7 7번 `0x7ffd80e2f5fb` = **dll2** `try_get_function_slow +0x73`, 9번 `0x7ffd80ebf60b` = **dll1** `try_get_function_slow +0x73`. 두 dll 모두 test.exe 와 같은 UCRT 함수로 `LoadLibraryExW(이름, NULL, 0x800)` 을 부른다.
- `[정적]` dll1 과 dll2 의 진입점 쪽은 **같은 코드**다 (명령·함수 안 오프셋 같음, 시작 rva 와 데이터 칸만 `0x10` 차이).

### flow.md 2.8 `[추측]` 판정

- "`LdrpCallInitRoutine` → `_DllMainCRTStartup` → `DllMain` 순서로 불린다. 사이에 다른 함수가 있는지는 모른다" → **사이에 하나 있다** `[정적]`: `_DllMainCRTStartup` ═(꼬리 `JMP`)▶ `dllmain_dispatch` → `+0x96` `DllMain`. 실행은 Q27.
- "CRT 가 자기 준비를 끝낸 뒤 우리 `DllMain` 을 불러주는 구조. `LoadLibraryExW` 3번도 CRT 준비 과정" → **코드로 맞다** `[정적]`. 세 번이 정말 그 세 길인지는 Q31.
- "DLL 도 `/MT` 라 같은 UCRT 함수일 것" → **맞다** `[정적]` (`try_get_function_slow +0x73`).
- "`_DllMainCRTStartup` 이 넷째 인자를 `DllMain` 의 `lpReserved` 로 그대로 넘기는지 (CRT 안)" → **그대로 넘긴다** `[정적]`. 그 값이 정말 첫 스레드 `CONTEXT` 포인터인지는 Q27.

## 정정

- **run7 표 9번** "`KERNELBASE!LoadLibraryExW` ← `0x7ffd80ebf60b` (dll2 주소 범위 안)" → **dll1** 이다 (`try_get_function_slow +0x73`, rva `0x4f60b`, 그때 dll1 베이스 `0x7ffd80e70000`).
  - "dll2 범위" 는 run6 때의 dll2 범위였다. run7 에선 그 자리에 dll1 이 올라와 있었다. dll2 의 rva `0x4f60b` 는 명령 한가운데라 돌아올 주소가 될 수 없다 `[정적]`.
- **run7 표 7번** "← `0x7ffd80e2f5fb` (모듈 미확인)" → **dll2** 이다 (`try_get_function_slow +0x73`, rva `0x4f5fb`, 그때 dll2 베이스 `0x7ffd80de0000`).
  - run7 "아직 확인 필요" 의 짐작 "7번 주소가 dll1 이라면 순서는 dll1 → dll2" 도 틀렸다 (7번 = dll2, 순서 dll2 → dll1). run7 맨 위 정정 노트(run8 이후)와 결론은 같다.
- **run7 표 10번** `dll2!DllMain` = dll1 의 `DllMain` 이라는 run7 정정 노트의 "가능성이 크다" 는, 9번에서 구한 dll1 베이스로 한 번 더 맞는다 (`0x7ffd80e70000 + 0x12e0`).
- 01_sprint run7 문서는 고치지 않았다 (오케스트레이터 지시). 정정은 이 절과 flow.md 2.8·4장·5장에 있다.
- flow.md 2.8 의 "`_DllMainCRTStartup` → `DllMain`" 은 틀린 말은 아니지만 **콜스택상 부모는 `dllmain_dispatch`** 로 읽어야 한다 (2.8 글을 고침).

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" → 새 항목 없이 **Q27** (사슬·`r8`) · **Q31** (`LoadLibraryExW` ×3) 에 출처로 붙임). dll 베이스는 실행마다 바뀌니 `lm` 으로 그때 베이스를 얻어 `dllN+rva` 로 건다 (run8 방법). 유저 주소라 test.exe 조건 필수:

| 무엇을 보나 | dll1 | dll2 | 볼 것 | 항목 |
|---|---|---|---|---|
| `_DllMainCRTStartup` 입구 | `dll1+0x1d60` | `dll2+0x1d50` | `r8` = ntdll 이 넘긴 `r9` 원본 (`poi(ntdll!LdrpProcessInitContextRecord)`) 인가, `edx` = 1, `k` 바로 위 `LdrpCallInitRoutineInternal+0x22` | Q27 |
| `dllmain_dispatch` 입구 | `dll1+0x1ba0` | `dll2+0x1b90` | `r8` 이 위와 같은가 (꼬리 `JMP` 뒤) | Q27 |
| CRT 준비 끝 (`+0x7b`) | `dll1+0x1c1b` | `dll2+0x1c0b` | `eax` = 1. `LoadLibraryExW` ×3 보다 **뒤**인가 | Q27·Q31 |
| `DllMain` 부르기 직전 (`+0x91`) | `dll1+0x1c31` | `dll2+0x1c21` | `r8`, `edx` = 1, `rcx` = DllBase | Q27 |
| `DllMain` 입구 | `dll1+0x12e0` | `dll2+0x12e0` | `r8` 같은가, `k` 돌아올 주소 `dll1+0x1c36` / `dll2+0x1c26` (`dllmain_dispatch+0x96`), `_DllMainCRTStartup` 없음 | Q27 |
| `DllMain` 돌아온 직후 | `dll1+0x1c36` | `dll2+0x1c26` | `eax` = 1 | Q27 |
| `LoadLibraryExW` ×3 돌아올 자리 | `dll1+0x4f60b` | `dll2+0x4f5fb` | `KERNELBASE!LoadLibraryExW` 입구에서 `poi(@rsp)` 가 이 값인 것만 세고 `du @rcx` 이름 셋. "dll2 → dll1" 순서 | Q31 |
| 동적 TLS 콜백 칸 (선택) | `dll1+0x84078` | `dll2+0x84078` | `dq` 로 0 인지 | Q27 |

정적 (새 공백 → 새 **Q73** (a)·(b)·(c)):

1. (Q73 a) `FlsGetValue2` 길 (함수 포인터 표 `__acrt_execute_initializers`) 이 dll 에서도 `try_get_function_slow` 로 가는지. 지도가 표를 못 따라가서 안 봤다 (한 상자 원칙이라 동적 Q31 이름으로 보는 게 맞을 수 있음).
2. (Q73 b) `__scrt_uninitialize_crt(ECX = (lpReserved ≠ 0), EDX = 0)` 안쪽 — 이유 0 에서 무엇이 달라지나. 진입점 도착 목표엔 덜 중요.
3. (Q73 c) `dllmain_crt_dispatch` 이유 2·3 (스레드) 길 `__scrt_dllmain_crt_thread_attach/detach` 안쪽 (범위 밖).

## 다른 항목 정리

- **Q54 (문서로 닫음, 2026-10-24 사용자 결정, 새 run 없음)**: flow.md 4장 2.7 "이름·뜻 못 정한 것 (run27)" 줄의 Q54 부분을 근거와 함께 갱신했다 — `ProcessStaticImport` 켜는 곳 = sast/17 `LdrpAllocateModuleEntry +0x194` `[정적]`, 간선 `+0x18` 비트 0 = sast/18·22 (켬 `LdrpLoadDependentModuleInternal +0x17d9`·`LdrpRecordModuleDependency +0x11a`, 지움 `+0xede`) `[정적]`, test.exe 에선 GR 이 건너뛸 간선 없을 것 `[추측]`. 남은 전수 확인은 정적 Q72.

## 디버거 메모 / 도구 메모

- 디버거 안 씀.
- 도구 (결과서): 고칠 버그 없음.
  - `_guard_dispatch_icall_nop.asm` 이 머리 한 줄뿐이다. Ghidra 가 2바이트 `JMP RAX` (`ff e0`) 를 크기 1 함수로 잡아서다. 읽는 데는 문제없다.
  - `map` 은 여전히 함수 포인터 표를 못 따라간다 (sast/25 와 같음, 하네스 후보 37).
  - `_calls.tsv` 에서 `DllMain +0x16` callee 가 `alias:ReasonName+1` 로 찍힌다. 같은 주소의 다른 이름은 구역 이름 `.text$mn` 이라 함수 별칭이 아니다. `ReasonName` 은 `sample/log.h` 의 함수다 `[소스]`.
- 멈춤점 메모 (다음 dast 용, 제안): `_DllMainCRTStartup` 에서 멈추고 `k` 를 찍은 뒤 `g` 로 `DllMain` 까지 가면, 두 `k` 의 차이로 꼬리 점프를 바로 볼 수 있다. dll 주소는 run7 처럼 빗나가니 고정 주소 `ba` 대신 `lm` 뒤 `dllN+rva` 로.

# 38 baggage-checklist — 정적 분석 결과

- 요청서: `_workspace/sast/38_baggage-checklist.md` (03_sprint 짐 지도 첫 바퀴 A = plan Q76. 체크리스트만 만든다)
- 모듈 / 바이너리 / PDB
  - ntoskrnl: `ms/bin/.../System32/ntoskrnl.exe`, `ms/sym/ntoskrnl.exe.pdb`, guid=`C29EBFB06B78B3C020DCA66D99713F9E` (기준과 같음)
  - ntdll: `ms/bin/.../System32/ntdll.dll`, `ms/sym/ntdll.dll.pdb`, guid=`C3093720177DA6851519DCFFE8DF53FE`
  - 두 프로젝트 모두 이미 analyze 끝남 (`.gpr` + `_index.tsv` + `SetPdb:` 줄). 다시 analyze 안 했다.
- 뽑은 함수: 정규식 `^MmCreateTeb$`, depth 1 → 5개 (`MmCreateTeb`, `KiStackAttachProcess`, `KiUnstackDetachProcess`, `MiAllocateFromSubAllocatedRegion`, `__security_check_cookie`) → `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`. 4초. `_calls.tsv` 는 57줄 늘고(MmCreateTeb 5 + 도우미 셋 52) 줄어든 줄은 없다.
- 버전 확인: export 뒤 `_calls.tsv` 로 대조 — `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines`, `PspAllocateThread +0x631 → MmCreateTeb`, `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x16b/+0x19c`, `LdrInitializeThunk +0xe → LdrpInitialize`, `LdrpInitializeInternal +0x5a → _LdrpInitialize`, `_LdrpInitialize +0x16c → LdrpInitializeProcess`, `LdrpLoadDll +0x100` **전부 일치.** 4번에서 문서 오프셋을 `.asm` 로 다시 본 줄들도 맞았다.
- 스크립트 문제: 없음.
- 쓴 명령·임시 스크립트는 맨 아래 "근거"에.

## 한눈에

- 구조체 21개 (커널 14 + 유저 7). 직접 칸 **1,456개** (그중 비트 칸 330). 이미 아는 쓴 자리: **정확 138 / 짐작 43 / 모름 1,275.**
  - 짐작 43 중 22 는 `_RTL_USER_PROCESS_PARAMETERS` 의 "블록 통째 복사" (칸 단위 근거 아님).
  - 오케스트레이터가 센 직접 칸 수 6개 (`_EPROCESS` 263 · `_KPROCESS` 65 · `_ETHREAD` 149 · `_KTHREAD` 237 · `_PEB` 124 · `_TEB` 137) 와 크기 (0x840 · 0x1c8 · 0x798 · 0x4c0 · 0x7d8 · 0x1878) 모두 **일치.**
- ntdll 전역 (쓰기 가능 구역): `.data` **438**, `.mrdata` **50** (오케스트레이터 수와 일치) + `.00cfg` 5. 쓴 자리를 아는 것: `.data` 47 · `.mrdata` 6 · `.00cfg` 1.
- callee: 함수 15개, `_calls.tsv` 줄 637 (nt 432 + ntdll 205), 겹침 빼고 서로 다른 이름 nt 252 / ntdll 133. flow.md 에 나옴 99 + 87, 이름만 110 + 32, **어디에도 없음 43 + 14.**
- `LdrSystemDllInitBlock` 블록 타입은 두 PDB 모두 **없다** (run32 말이 맞다).

## 질문별 답 [정적]

### 1. 구조체 칸 목록 (PDB 타입)

표 파일: `ms/disassemble/ntoskrnl/_baggage/struct_<이름>.tsv` (커널 14개), `ms/disassemble/ntdll/_baggage/struct_<이름>.tsv` (유저 7개). 칸마다 (오프셋, 이름, 타입, 크기, 비트 위치) 가 있다.

| 구조체 | 뽑은 PDB | 크기 | 직접 칸 (비트 칸) | 펼친 칸 | 이미 아는: 정확 | 짐작 | 모름 |
|---|---|---|---|---|---|---|---|
| `_EPROCESS` | nt | 0x840 | 263 (98) | 100 | 17 | 5 | 241 |
| `_KPROCESS` | nt | 0x1c8 | 65 (12) | 14 | 0 | 0 | 65 |
| `_ETHREAD` | nt | 0x798 | 149 (51) | 0 | 5 | 0 | 144 |
| `_KTHREAD` | nt | 0x4c0 | 237 (65) | 0 | 9 | 1 | 227 |
| `_KTRAP_FRAME` | nt | 0x190 | 66 (0) | 0 | 4 | 2 | 60 |
| `_HANDLE_TABLE` | nt | 0x80 | 17 (5) | 0 | 0 | 0 | 17 |
| `_MMVAD` | nt | 0x88 | 9 (0) | 10 | 0 | 0 | 9 |
| `_MMVAD_SHORT` | nt | 0x40 | 14 (0) | 62 | 0 | 0 | 14 |
| `_TOKEN` | nt | 0x5c8 | 55 (2) | 0 | 0 | 0 | 55 |
| `_SECTION` | nt | 0x40 | 9 (3) | 32 | 0 | 0 | 9 |
| `_CONTROL_AREA` | nt | 0x80 | 17 (0) | 51 | 1 | 1 | 15 |
| `_SEGMENT` | nt | 0x48 | 11 (0) | 21 | 1 | 0 | 10 |
| `_SECTION_IMAGE_INFORMATION` | nt | 0x40 | 26 (7) | 0 | 16 | 0 | 10 |
| `_FILE_OBJECT` | nt | 0xd8 | 30 (0) | 0 | 0 | 0 | 30 |
| `_PEB` | ntdll | 0x7d8 | 124 (23) | 0 | 19 | 12 | 93 |
| `_TEB` | ntdll | 0x1878 | 137 (17) | 0 | 4 | 0 | 133 |
| `_RTL_USER_PROCESS_PARAMETERS` | ntdll | 0x450 | 40 (0) | 0 | 18 | 22 | 0 |
| `_PEB_LDR_DATA` | ntdll | 0x58 | 9 (0) | 0 | 7 | 0 | 2 |
| `_LDR_DATA_TABLE_ENTRY` | ntdll | 0x138 | 65 (31) | 0 | 31 | 0 | 34 |
| `_LDR_DDAG_NODE` | ntdll | 0x50 | 10 (0) | 0 | 6 | 0 | 4 |
| `_KUSER_SHARED_DATA` | ntdll | 0xa80 | 103 (16) | 0 | 0 | 0 | 103 |

- 직접 칸 = 필드 목록(`LF_FIELDLIST`)의 `LF_MEMBER` 개수. 비트 칸도 한 칸으로 센다 (오케스트레이터 수와 같은 기준). 펼친 칸은 이 수에 안 들어간다.
- **이 PDB 는 이름 없는 union/struct 를 이미 평평하게 펴서 담았다.** `Flags3` 같은 칸은 정수 칸 하나(`unsigned long`) 뒤에 비트 칸들이 같은 오프셋으로 줄지어 나온다.
  - 비트 칸: 표의 `bits` 열 = `위치:길이` (예: `ProcessFirstResume` = `15:1`, `container` = `Flags3`). `size` 는 담는 정수의 바이트 수.
  - 같은 자리를 두 이름이 나눠 쓰는 union 대안은 `note` 열에 `union-alt-of:<먼저 나온 이름>`. 비트 칸이 정수 칸보다 앞에 나오는 곳(예: `_KTHREAD +0x74`)은 `container-listed-after`.
- `<unnamed-tag>` 형식이 칸 타입으로 남은 곳은 안쪽을 펴서 `level=X` 줄로 적었다: `_EPROCESS` 4곳 (`MitigationFlagsValues` `MitigationFlags2Values` `MitigationFlags3Values` `SyscallUsageValues`), `_KPROCESS.SecureState`, `_MMVAD.u4`, `_MMVAD_SHORT.u`·`u5`, `_SECTION.u1`·`u`, `_CONTROL_AREA.u`·`u1`·`u2`·`u3`, `_SEGMENT.u1`·`u2`.
  - 이름에 `FLAGS`/`OPTIONS` 가 든 이름 붙은 형식 (`_MMVAD_FLAGS`, `_SEGMENT_FLAGS`, `_KEXECUTE_OPTIONS` …) 도 비트를 펴서 X 줄로 적었다.
  - 그 밖에 비트가 든 이름 붙은 형식 (`_EX_PUSH_LOCK` `_EX_FAST_REF` `_DISPATCHER_HEADER` `_RTL_BALANCED_NODE` `_KAPC_STATE` …) 은 **펴지 않고** `note` 에 `bits-in-type:<형식>(비트 수)` 만 붙였다. 필요하면 다음 바퀴에서 펴자.
- 구조체 단위로만 아는 것 (`_MMVAD*` `_HANDLE_TABLE` `_TOKEN` `_SECTION` `_FILE_OBJECT` `_KPROCESS` …) 은 각 표 맨 위 `# struct_level:` 줄에 있다. 칸 단위 근거는 없다.

타입 이름 확인 — 요청서의 예상 이름이 **전부 그대로 있다** (다른 이름으로 바꿀 것 없음):

| 요청서 | PDB (ntoskrnl) | 크기 / 칸 |
|---|---|---|
| 핸들 표 | `_HANDLE_TABLE` | 0x80 / 17 |
| VAD | `_MMVAD` (`Core` 격인 `_MMVAD_SHORT` 따로) | 0x88 / 9, 0x40 / 14 |
| 토큰 | `_TOKEN` | 0x5c8 / 55 |
| 섹션 | `_SECTION` · `_CONTROL_AREA` · `_SEGMENT` | 0x40 · 0x80 · 0x48 |
| 이미지 정보 | `_SECTION_IMAGE_INFORMATION` (0x40). 세그먼트 `u2.ImageInformation` 이 가리키는 건 `_MI_SECTION_IMAGE_INFORMATION` (0x50 = 앞 0x40 + `_MI_EXTRA_IMAGE_INFORMATION` 0x10) | 0x40 / 26 |
| 파일 객체 | `_FILE_OBJECT` | 0xd8 / 30 |

- 아직 표를 안 만든 이웃 타입 (PDB 에 있음): `_HANDLE_TABLE_ENTRY`(0x10 union) · `_OBJECT_HEADER`(0x38) · `_SUBSECTION`(0x38) · `_MSUBSECTION`(0x98) · `_SECTION_OBJECT_POINTERS`(0x18) · `_PS_NTDLL_EXPORTS`(0xa8, 21칸) · `_CONTEXT`(0x4d0).
- **유저 공간 7개: ntdll PDB 와 ntoskrnl PDB 에 둘 다 있고, 크기·칸 수·모든 줄(오프셋·이름·타입·비트)이 완전히 같다.** 그래서 표는 ntdll PDB 기준으로 만들었다.
- 참고로 커널 구조체를 ntdll PDB 에서 보면 칸이 적다: `_EPROCESS` 261 (커널 PDB 263), `_KTHREAD` 235 (237) — `+0x7b8` 의 `SyscallProvider`·`SyscallProviderProcessLinks`·`SyscallProviderDispatchContext` 가 `SyscallProviderReserved` 하나로, `_KTHREAD +0x7e` 의 `SpecCtrl` 류가 `Spare0` 로 가려져 있다. 크기는 같다. 커널 구조체는 커널 PDB 를 썼다.
- **`LdrSystemDllInitBlock` 타입: 없다.** 두 PDB 모두에서 `InitBlock` · `SystemDll` · `DLL_INIT` 를 찾았지만 블록 형식(0x128 바이트)은 없다. 있는 것: `_PS_NTDLL_EXPORTS` (첫 칸 `LdrSystemDllInitBlock` 은 `void*` = 블록 주소를 담는 칸), `_SYSTEM_DLL_TYPE` 열거형, `_WOW64_SHARED_INFORMATION` 열거형 (`SharedNtdll32LdrSystemDllInitBlock = 7`).
  - 블록 자체는 ntdll 전역 `LdrSystemDllInitBlock` (`.mrdata`, rva `0x1ea450`). 다음 이름까지 거리가 `0x128` = 296 이라 run32 가 말한 블록 크기와 같다.

근거: `llvm-pdbutil-14 dump -types` 두 PDB, 파서 `ms/ghidra/_tmp_sast38/pdbtypes.py` + `gen_structs.py`.

### 2. ntdll 전역 변수 목록 (`ntdll.dll.pdb` 공개 이름 중 데이터)

표 파일: `ms/disassemble/ntdll/_baggage/globals.tsv` (쓰기 가능 3구역 493줄), `globals_rdata.tsv` (`.rdata` 1,546줄, 읽기 전용이라 4번 안 함).

| 구역 | rva | 가상 크기 | 쓰기 (PE 머리) | 데이터 이름 | 쓴 자리를 아는 것 (정확/짐작) | 이름이 문서에 나오나 |
|---|---|---|---|---|---|---|
| `.data` | 0x1cc000 | 0x8f58 | 쓰기 | **438** | 47 (45/2) | 95 (나머지 343 은 문서에 이름도 없음) |
| `.mrdata` | 0x1e7000 | 0x35b8 | 쓰기 (실행 중엔 ntdll 이 필요할 때만 열고 닫음, run18 묶음 3) | **50** | 6 (3/3) | 11 (39 없음) |
| `.00cfg` | 0x1e6000 | 0x28 | 읽기 전용 | 5 (`__guard_dispatch_icall_fptr` 외 CFG 포인터 4) | 1 (0/1) | 1 |
| `.rdata` | 0x173000 | 0x58acf | 읽기 전용 | 1,546 | — | — |

- 오케스트레이터 수 `.data` 438 · `.mrdata` 50 **맞다.** 그 밖에 데이터 이름이 아닌 것: `.text` 안 라벨 11, `SCPCFG*` 구역 안 라벨 8, 구역 없는 절대값 8 (`__guard_*_count` 등). 데이터 이름 합계 2,066 = 438 + 50 + 5 + 1,546 + 11 + 8 + 8.
- 쓰기 가능으로 PE 머리에 적힌 구역은 `.data` · `.mrdata` 둘뿐이다. `.00cfg` 는 읽기 전용 구역이지만 로더가 값을 넣는 CFG 함수 포인터 5칸이 있어서 따로 셌다.
- **크기는 전부 `[추측]`.** 공개 PDB 에는 전역 변수 형식·크기 줄(`S_GDATA32`)이 하나도 없다 (`-globals` 에 `S_PROCREF`·`S_LPROCREF`·`S_ANNOTATIONREF` 뿐). "다음 이름까지 거리"(상한)이고, 마지막 이름은 구역 끝까지. 장식된 이름 4개 (`?g_SbTableEntry_…@@3U_SWITCHBRANCH_…`, `?RtlpHpEnvFlsContext@@3U_RTLP_FLS_CONTEXT@@A`) 의 형식도 PDB 에 없다. 같은 RVA 에 이름 둘인 곳은 `.data` 한 곳 (`same_rva_names` 열).
  - 시험: `LdrSystemDllInitBlock` 거리 296 = run32 의 블록 0x128 과 같음.

근거: `llvm-pdbutil-14 dump -publics` (S_PUB32 6,930개 중 함수 4,864 / 데이터 2,066), 구역 표는 PE 머리를 직접 읽음 (`pesec.py`; `-section-headers` 는 구역 이름이 깨져서).

### 3. 초기화 함수들의 callee 목록

표 파일: `ms/disassemble/ntoskrnl/_baggage/callees.tsv` (432줄), `ms/disassemble/ntdll/_baggage/callees.tsv` (205줄), 각각 옆에 `callees_summary.tsv`.

| 함수 | 줄 수 | 조각 점프 (호출 아님) | 호출 줄 | 그중 꼬리 JMP | 그중 간접 | **서로 다른 callee** | flow.md 에 나옴 | run·결과서에 이름만 | **어디에도 없음** | 본문 뽑아 둔 것 | 오케스트레이터 수 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| nt `NtCreateUserProcess` | 83 | 0 | 83 | 0 | 2 | 61 | 29 | 32 | 0 | 16/61 | 61 |
| nt `PspAllocateProcess` | 144 | 0 | 144 | 0 | 2 | 105 | 25 | 40 | **40** | 12/105 | 105 |
| nt `MmCreatePeb` | 19 | 1 | 18 | 0 | 0 | 15 | 7 | 8 | 0 | 5/15 | 16 |
| nt `PspSetupUserProcessAddressSpace` | 20 | 0 | 20 | 0 | 0 | 18 | 14 | 3 | 1 | 7/18 | 18 |
| nt `PspAllocateThread` | 49 | 5 | 44 | 0 | 0 | 37 | 13 | 24 | 0 | 8/37 | 40 |
| nt `MmCreateTeb` (이번에 export) | 5 | 0 | 5 | 0 | 0 | 4 | 2 | 1 | 1 | 4/4 | 0 |
| nt `PspInsertProcess` | 21 | 0 | 21 | 0 | 0 | 20 | 8 | 12 | 0 | 1/20 | 20 |
| nt `PspInsertThread` | 62 | 3 | 59 | 0 | 0 | 39 | 14 | 25 | 0 | 5/39 | 42 |
| nt `PspUserThreadStartup` | 9 | 0 | 9 | 0 | 0 | 9 | 9 | 0 | 0 | 2/9 | 9 |
| nt `PspInitializeThunkContext` | 20 | 0 | 20 | 0 | 1 | 15 | 9 | 5 | 1 | 8/15 | — |
| ntdll `LdrInitializeThunk` | 3 | 0 | 3 | 0 | 0 | 3 | 3 | 0 | 0 | 3/3 | — |
| ntdll `LdrpInitialize` | 4 | 0 | 4 | 1 | 0 | 4 | 4 | 0 | 0 | 1/4 | 4 |
| ntdll `LdrpInitializeInternal` | 6 | 0 | 6 | 0 | 0 | 6 | 6 | 0 | 0 | 4/6 | 6 |
| ntdll `_LdrpInitialize` | 23 | 0 | 23 | 0 | 1 | 20 | 16 | 4 | 0 | 10/20 | 20 |
| ntdll `LdrpInitializeProcess` | 169 | 0 | 169 | 0 | 4 | 109 | 67 | 28 | **14** | 41/109 | 109 |

- **세는 법**: 호출 줄 = 줄 수 − 조각 점프 (`ret_off` 가 `frag` 인 줄 = 자기 조각으로 가는 JMP, 호출이 아님). 서로 다른 callee = `alias:` 접두사 떼고, `indirect:*`·`FUN_` 조각 이름 뺀 이름 수. 오케스트레이터 수와 다른 3곳 (`MmCreatePeb` 16·`PspAllocateThread` 40·`PspInsertThread` 42) 은 **조각 점프 대상 `FUN_…` 이름을 callee 로 센 것**이라 1~3 많다. 나머지는 같다.
- "flow.md 에 나옴" = `runs/02_sprint/flow.md`(또는 01) 에 그 이름이 한 번이라도 나옴. "이름만" = run·결과서에만. alias 줄은 별칭 표(`ms/sym/*.aliases.tsv`)의 이름 전부로 찾았다 (`alias_names` 열). `?…@…` 장식 이름·`Feature_…__private_…` 는 앞쪽 핵심 이름도 같이 찾았다.
- **`MmCreateTeb`**: 호출 5줄, callee 4개 — `+0x7a KiStackAttachProcess`, `+0xa7 MiAllocateFromSubAllocatedRegion`, `+0x299`·`+0x2d3 KiUnstackDetachProcess`, `+0x2ac __security_check_cookie`. 지도(`_map/NtCreateUserProcess_edges.tsv`)의 5줄과 같다. 함수 몸집은 811바이트. 새 팩트: **TEB 에 직접 쓰는 대입이 약 47줄** (`ms/decomp/ntoskrnl/MmCreateTeb.c`, 64비트 갈래 + WOW64 갈래 둘 합산). 문서(flow.md 2.1)는 "본문은 안 봤다. TEB 에 무엇을 쓰는지 모른다"였다 → 칸별 확인은 Q78.
- 간접 호출 줄 (대상은 동적으로만): nt 5곳 `NtCreateUserProcess +0x5b2`·`+0x11bb`, `PspAllocateProcess +0x1215`·`+0x17bf`, `PspInitializeThunkContext +0x35b`; ntdll 5곳 `_LdrpInitialize +0x9a`, `LdrpInitializeProcess +0x17fc`·`+0x1b59 (Kernel32ThreadInitThunkFunction)`·`+0x1cdf`·`+0x1d16 (PEB+0x230)`.
- 지도에 **없는** 함수: `PspUserThreadStartup`, `PspInitializeThunkContext` 둘 (`SwapContext` 의 `RET` 로 들어오는 길이라 `NtCreateUserProcess` 지도에 안 닿는다. 장부 줄은 이전 export 로 있다). 나머지 13개는 지도(`_nodes.tsv`)에 있다.
- 본문을 뽑아 둔 callee 비율이 낮다: `PspAllocateProcess` 12/105, `PspInsertProcess` 1/20, `PspInsertThread` 5/39, `LdrpInitializeProcess` 41/109.

근거: `ms/disassemble/{ntoskrnl,ntdll}/_calls.tsv` 와 `gen_callees.py`.

### 4. 이미 아는 "누가 씀" 대조

- 표 열: 구조체 표의 `writer` `evidence` `conf` `known_note`, 전역 표의 `writer` `evidence` `conf`, 그리고 줄 하나가 (칸, 쓰는 곳) 한 쌍인 `known_writers.tsv` (nt 72줄, ntdll 289줄).
- **확실도 기준**: `정확` = 근거에 (쓰는 함수 + 칸 오프셋 또는 이름) 이 같이 있고 PDB 칸과 오프셋·이름이 맞음. 함수만 있고 줄 오프셋이 없어도 칸이 분명하면 정확 (오프셋은 `writer` 에 있는 만큼만). `짐작` = 범위·묶음으로만 말했거나(`MmCreatePeb` 의 `+0xb8 ~ +0xe0`), 칸 이름이 문서에서 `[추측]` 이거나, 쓰기가 간접(주소 넘김·복사)인 것. `모름` = 근거에서 못 찾음.
- **근거 문서 모음**: `runs/01_sprint` 7개 + `runs/02_sprint` 35개 (해설서 `*-해설.md` 뺌) + `_workspace/sast/01~37_*_result.md` 37개 = 79개 파일 20,895줄 (이 38번 결과서는 뺌). `ms/disassemble/*/_refs/*.tsv` 는 따로 스크립트로 읽었다.
- **본 곳**: `runs/02_sprint/flow.md` 의 2.1(커널 생성) · 2.2 중 멈춤/풀기 부분(1606~1765줄) · 2.3(스레드 출발) · 2.4(로더 시작) 는 **줄 단위로 읽음**. 그 밖(2.2 앞쪽 부모 유저 코드, 2.5~2.10), `runs/01_sprint`·`runs/02_sprint` run 문서, `_workspace/sast/*_result.md` 는 **칸 이름·오프셋·함수 이름 검색**으로 찾음 (줄 단위로 다 읽지 않았다 → 2.5~2.10 에서 쓰는 칸 (DLL 항목·TLS·kernel32 준비) 은 빠졌을 수 있다). `_refs/*.tsv` 는 WRITE·READ_WRITE(= 정확)·DATA(= 주소 취함, 짐작) 줄을 스크립트로 전부 반영.
- **문서 오프셋을 `.asm` 로 다시 본 것**: 문서가 "호출 자리(ret)" 로 적었는데 칸에 쓰는 줄이 따로 있는 곳은 `.asm` 에서 저장 줄을 찾아 `writer` 에 반영했다 (`known_note` 에 "(.asm 확인)"): `_EPROCESS.UniqueProcessId` `PspAllocateProcess +0x27e4`, `CreateTime` `+0x27c4`/`+0x27dd`, `_PEB.FastPebLock` `LdrpInitializeProcess +0x9e1`, `ProcessHeap` (출력 칸 `+0xa39` LEA, 호출 `+0xa4b`), `LoaderLock` `_LdrpInitialize +0xfa`, `ProcessInitializing` `+0x107`/`+0x20f`, `_TEB.SameTebFlags` `+0xea`. 그 밖에 직접 맞춰 본 줄(`OwnerProcessId +0x710` · `ProcessFirstResume +0x4e` · `Ldr +0x1c1` · `StartAddress`/`Win32StartAddress +0x647`/`+0x655` · `KeInitThread +0xed`/`+0x152`/`+0x163` · `LdrpCurrentDllInitializer +0xbd`/`+0x220` · `TlsIndex +0x102` · `InLegacyLists +0x5e` · `SectionBaseAddress +0xc5`) 은 문서와 같았다.
- 문서끼리·PDB 와 어긋난 곳: **오프셋이 PDB 와 안 맞는 줄은 없었다.** run10 의 PEB 표는 이름이 `[추측]` 이었는데 PDB 이름과 7줄 모두 맞는다. 다만 ntdll 항목 쪽 "`LoadCount` = -1" 은 문서가 항목 칸처럼 적었지만 PDB 에서 항목에는 `ObsoleteLoadCount`(+0x6c) 뿐이고 `LoadCount` 는 DDAG 노드(+0x18) 칸이라 `_LDR_DDAG_NODE.LoadCount` 에 짐작으로 올렸다.
- **구조체별 해석**: `_SECTION_IMAGE_INFORMATION` 은 run31(= sast26) 표 덕에 26칸 중 16칸을 안다. `_RTL_USER_PROCESS_PARAMETERS` 는 `모름` 0 이지만 22칸은 "통째 복사" 짐작이다. `_PEB` 는 커널(MmCreatePeb·PspCopyAndFixupParameters)과 ntdll(Ldr·ProcessHeap·FastPebLock·TLS 비트맵·LoaderLock)이 나눠 쓴다. `_EPROCESS` 는 칸 단위로 읽은 문서가 적다 (`PspAllocateProcess` 144줄을 14묶음으로만 읽음). `_KUSER_SHARED_DATA` 는 ntdll 이 읽기만 한다 (쓰는 쪽 근거 없음).
- **전역**: 쓴 자리를 아는 54개 중 36개는 `_refs` WRITE 줄 (명령 그대로), 나머지는 문서·DATA 줄. `_refs` 에서 **DATA 줄만 있는 전역 6개** (`Kernel32ThreadInitThunkFunction` `g_LdrpWow64LdrpInitialize` `LdrpKnownDllDirectoryHandle` `LdrpLoadCompleteEvent` `LdrpWorkCompleteEvent` `__guard_dispatch_icall_fptr`) 는 코드가 주소만 넘긴다 → 받은 함수가 쓴다 (`__guard_dispatch_icall_fptr` 는 PE 로드 구성이 가리키는 칸, 실행 중 쓰는 코드 줄 없음 = 모름).
- callee 대조는 3번 표 (flow.md / 이름만 / 어디에도 없음). 어디에도 없는 57개의 이름은 `callees.tsv` 의 `doc=none` 줄.

## 표 파일 경로와 열 설명

모두 `ms/disassemble/<모듈>/_baggage/` (커밋 안 됨). 첫 줄 `# …` 가 열 이름. 탭 구분.

| 파일 | 줄 | 열 |
|---|---|---|
| `ntoskrnl/_baggage/struct_<이름>.tsv` ×14, `ntdll/_baggage/struct_<이름>.tsv` ×7 (이름은 앞 밑줄 포함: `struct__EPROCESS.tsv`) | 칸 1줄 | `struct` `src_pdb` `seq`(직접 칸 번호, 펼친 칸은 `3.2` 꼴) `off`(16진) `size`(바이트) `path`(칸 이름, 펼친 칸은 `부모.자식`) `type` `bits`(`위치:길이`) `container`(비트가 든 정수 칸) `level`(`D` 직접 / `X` 펼침) `note` `writer` `evidence` `conf`(정확/짐작/모름) `known_note` |
| `ntdll/_baggage/globals.tsv` | 전역 1줄 | `name` `section` `rva` `size` `size_basis`(전부 `[추측]`) `sec_flags` `same_rva_names` `next_name` `writer` `evidence` `conf` |
| `ntdll/_baggage/globals_rdata.tsv` | `.rdata` 데이터 이름 | 위와 같음 (writer 열 비어 있음) |
| `{ntoskrnl,ntdll}/_baggage/callees.tsv` | `_calls.tsv` 줄 1개 | `caller` `call_off` `ret_off` `callee`(alias 접두사 뗌) `kind`(call/tail/indirect/fragjmp, `+in_frag`·`+in_seh`) `alias_names` `doc`(flow/run_only/none/unnamed) `flow_hits` `other_docs` `body`(`asm` = 본문 뽑아 둠) |
| `{ntoskrnl,ntdll}/_baggage/callees_summary.tsv` | 함수 1줄 | 3번 표의 숫자 |
| `{ntoskrnl,ntdll}/_baggage/known_writers.tsv` | (칸, 쓰는 곳) 1쌍 | `kind`(struct/global) `name` `seq` `off` `path` `writer` `evidence` `conf` `note` |

임시 스크립트·원본: `ms/ghidra/_tmp_sast38/` (`run_all.sh` 가 순서대로 다시 만든다: `gen_structs.py` → `gen_globals.py` → `gen_callees.py` → `apply_annot.py` → `apply_globals.py`). 사람이 문서에서 옮긴 "누가 씀" 목록은 `annot_struct.py` (다음 바퀴에서 이어서 채우기 좋다).

## Q77(사진)에서 떠야 할 것 — 추천

**커널 (test.exe 프로세스 기준, 첫 스레드 하나)** — 주소는 프로세스 객체에서 따라간다 (칸 오프셋은 PDB):

| 대상 | 주소를 얻는 곳 | 크기 |
|---|---|---|
| `_EPROCESS` (안에 `_KPROCESS`) | 프로세스 객체 | 0x840 |
| 첫 `_ETHREAD` (안에 `_KTHREAD`) | `EPROCESS.ThreadListHead`(+0x370) 또는 현재 스레드 | 0x798 |
| `_KTRAP_FRAME` | `KTHREAD.InitialStack`(+0x28) − 0x190 (run15) | 0x190 |
| `_HANDLE_TABLE` (+ 핸들 항목 표) | `EPROCESS.ObjectTable`(+0x300) | 0x80 |
| `_TOKEN` | `EPROCESS.Token`(+0x248, `_EX_FAST_REF` 하위 4비트 지움) | 0x5c8 |
| VAD 나무 (`_MMVAD_SHORT` 0x40 / `_MMVAD` 0x88 마다) | `EPROCESS.VadRoot`(+0x558) | 노드 수만큼 |
| exe 섹션 | `EPROCESS.SectionObject`(+0x2a8) → `_SECTION`(0x40) → `u1.ControlArea`(+0x28) `_CONTROL_AREA`(0x80) → `Segment` `_SEGMENT`(0x48) → `u2.ImageInformation`(+0x38) = `_MI_SECTION_IMAGE_INFORMATION`(0x50, 앞 0x40 = `_SECTION_IMAGE_INFORMATION`) | |
| `_FILE_OBJECT` | `EPROCESS.ImageFilePointer`(+0x330) | 0xd8 |

**유저 (test.exe 문맥)**:

| 대상 | 주소를 얻는 곳 | 크기 |
|---|---|---|
| `_PEB` | `EPROCESS.Peb`(+0x2e0) | 0x7d8 |
| `_TEB` | `KTHREAD.Teb`(+0xf0) | 0x1878 |
| `_RTL_USER_PROCESS_PARAMETERS` + 환경 블록·문자열 | `PEB.ProcessParameters`(+0x20); 환경은 `Environment`(+0x80), 크기 `EnvironmentSize`(+0x3f0) | 0x450 + 문자열 |
| `_PEB_LDR_DATA` | `PEB.Ldr`(+0x18) = ntdll `PebLdr` 전역 (`.data` rva 0x1d2880) | 0x58 |
| `_LDR_DATA_TABLE_ENTRY` + `_LDR_DDAG_NODE` | `PebLdr` 목록(`InLoadOrderModuleList` +0x10)을 따라; 항목의 `DdagNode`(+0x98) | 0x138 + 0x50, 모듈마다 |
| `_KUSER_SHARED_DATA` | 고정 주소 `0x7ffe0000` (문서에 `0x7ffe0308` `0x7ffe0384` 로 나옴: run34, sast15). 커널 쪽 별칭 주소는 이 문서군에 없음 `[공백]` | 0xa80 (페이지 0x1000). 시각 칸은 계속 변해서 ①②를 빼면 소음 → 한 번만 |
| 저장된 `_CONTEXT` | `LdrInitializeThunk` 입구 `Rcx` (run16) | 0x4d0 |
| API set 지도 | **`PEB.ApiSetMap`(+0x68) 값** (ntdll 이 `ApiSetpResolveHost(PEB+0x68, …)` 로 읽음, sast18). 커널이 `PspSetupUserProcessAddressSpace +0x255 MmMapApiSetView` 로 올림 (run32). 크기는 지도 머리글의 크기 칸 `[추측: 공개 지식]` 또는 VAD/`!address` 영역 크기 | 영역 크기 |
| 그 밖에 PEB 가 가리키는 별도 영역 | `LeapSecondData`(+0x7b8, 커널이 올림) · `SharedData`(+0x90) · `ProcessHeap`(+0x30, 로더 항목이 사는 곳; 힙 전체 말고 요약) · `TlsBitmap`(+0x78) · `ActivationContextData`(+0x2f8)/`SystemDefaultActivationContextData`(+0x308) · `WaitOnAddressHashTable`(+0x3a0, PEB 안 0x400) | |
| TEB 가 가리키는 영역 | `ThreadLocalStoragePointer`(+0x58) · `SchedulerSharedDataSlot`(+0x1850, 8칸 표) · `ActivationContextStackPointer`(+0x2c8) · 유저 스택 (`NtTib.StackBase`/`StackLimit`, `DeallocationStack` +0x1478) | |

**ntdll 쓰기 가능한 구역 (rva = ntdll 베이스 + 값; 베이스는 실행마다 바뀜)**:

| 구역 | rva | 크기 | 메모 |
|---|---|---|---|
| `.data` | `0x1cc000` | `0x8f58` (4,587 qword) | 파일엔 0x2000 만, 나머지는 0 으로 시작 (run17). 전역 438개, 쓴 자리 아는 것 47 |
| `.mrdata` | `0x1e7000` | `0x35b8` (1,719 qword) | `LdrSystemDllInitBlock` 이 `0x1ea450` (0x128). 평소 읽기 전용이라 읽기만 |
| `.00cfg` | `0x1e6000` | `0x28` (5 qword) | CFG 함수 포인터 5칸 (`__guard_dispatch_icall_fptr` 가 맨 앞) |

- ② − ① 에서 바뀌는 ntdll 전역은 위 표에서 쓴 자리를 아는 54개가 먼저 후보다. 바뀐 칸이 `모름` 줄이면 Q78 에서 주인을 가른다.
- 사진에 같이 넣으면 좋은 커널 쪽: 전역 `PsNtdllExports` (`_PS_NTDLL_EXPORTS` 0xa8, ntdll 안 진입점 주소 표 — 커널이 `LdrInitializeThunk`·`RtlUserThreadStart` 를 여기서 꺼냄, run16). 프로세스마다 다른 게 아니라 한 번이면 된다.
- 추천 멈춤점 (이미 plan Q77 에 있음, 새로 정한 것 아님): ① `ntdll!LdrInitializeThunk` 입구 (rva `0xe1420`), ② `ntdll!NtContinue` 입구 (rva `0x161610`, `LdrInitializeThunk +0x13` 이 부름). test.exe 일 때만 서게 조건을 붙인다 (dast-mentor 몫).

## 새로 생긴 공백

1. **커널 객체 쪽 칸 단위 근거가 거의 없다**: `_HANDLE_TABLE` · `_MMVAD*` · `_TOKEN` · `_SECTION` · `_FILE_OBJECT` · `_KPROCESS` 는 아는 칸 0. `_EPROCESS` 모름 241/263, `_KTHREAD` 227/237. 이유: `PspAllocateProcess`(144줄, callee 105개 중 **문서에 이름도 없는 것 40개**, 본문 뽑은 것 12개)를 14묶음으로만 읽었고, `KeInitThread` 는 일부, `KeStartThread` (1,182바이트)·`KeInitializeProcess` · `ObInitProcess` · `PspInitializeProcessSecurity` · `PsAssignThreadId` · `PspApplyMitigationOptions` · `MiAllocateProcessVads` · `MiInsertProcessVads` 는 본문(.asm)을 안 뽑았다.
2. **`MmCreateTeb` 가 TEB 에 쓰는 칸** — 이번에 본문을 처음 뽑았다 (대입 약 47줄). 칸별 표는 Q78.
3. **PDB 에 없는 타입**: `LdrSystemDllInitBlock` 블록(0x128) · `InitialTeb`(`PspSetupUserStack` 의 스택 정보) · `PS_CREATE_INFO` · `PS_ATTRIBUTE_LIST` · 간선 칸 0x20바이트 (run23) · `_SWITCHBRANCH_*` · `_RTLP_FLS_CONTEXT`. 이 칸들은 문서의 오프셋(`+0x98` `+0xa0` `+0xb8` …)으로만 적어야 한다.
4. **`.mrdata` 를 쓰는 곳**: `LdrInitializeMrdata` (rva `0x8c390`, 109바이트, `_LdrpInitialize +0x14a` 가 부름) 본문을 아직 안 읽었다 (run17 `[공백]`). `LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected`·`LdrpMrdataUnprotected` 의 후보 쓰는 곳. `.mrdata` 50개 중 44개가 쓴 자리 모름.
5. **지도에 없는 함수 둘** — `PspUserThreadStartup`, `PspInitializeThunkContext` (3번). 지도 뿌리를 `PspUserThreadStartup` 으로 하나 더 만들면 이 둘 아래의 깊이·caller 가 색인으로 보인다 (`run_ghidra.sh map ntoskrnl.exe PspUserThreadStartup`, 몇 초).
6. `.data` 438개 중 문서에 이름도 없는 것 343개, 이름은 나오는데 쓴 자리를 못 찾은 것 48개 (주로 읽기). 초기화 중에 쓰는지 안 쓰는지 정적으로 판단 못 한 상태.
7. 하네스 제안 (고치지 않음): PDB 타입 → 칸 목록 TSV 를 만드는 `pdb_types.py` 가 `ghidra-headless-export/scripts/` 에 있으면 다음 빌드·다른 모듈에서 `ms/ghidra/_tmp_sast38/pdbtypes.py`+`gen_structs.py` 를 다시 짤 일이 없다.

## 동적분석으로 확인해야 할 것

- 사진 ①(`LdrInitializeThunk` 입구)·②(`NtContinue` 입구)에서 PDB 칸 값이 바뀐 곳과 위 표의 `정확`/`짐작`/`모름` 을 대조 — 정적으로 `모름` 인 칸이 실제로 바뀌는지가 "빠짐없음" 확인이다.
- 포인터·간접으로 쓰는 칸: 간접 호출 10줄의 대상, 주소만 넘기는 전역 6개 (`Kernel32ThreadInitThunkFunction` 등)의 실제 값.
- 한계: 이 결과서의 "누가 씀" 은 문서에 이미 적힌 것을 옮긴 것이다. 새로 코드를 읽은 건 `MmCreateTeb` 와 `.asm` 로 다시 맞춰 본 줄뿐이다.

## 판정

**분석성공.** 요청서 네 질문 모두 표와 숫자로 답했고(오케스트레이터가 센 수와 일치 확인), `MmCreateTeb` 를 export 해 callee 목록을 채웠다. 한계: 4번(이미 아는 쓴 자리)은 flow.md 2.1·2.3·2.4 와 2.2 의 멈춤/풀기 부분만 줄 단위로 읽었고, 나머지(2.5~2.10)·run 문서·결과서는 검색으로만 찾았다.

## 근거 (쓴 명령)

- `llvm-pdbutil-14 dump -summary|-types|-publics|-globals|-section-headers ms/sym/{ntoskrnl.exe,ntdll.dll}.pdb` → `ms/ghidra/_tmp_sast38/{nt_types,ntdll_types,ntdll_publics}.txt`
- `bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh export ntoskrnl.exe '^MmCreateTeb$' 1`
- `python3 .claude/skills/ghidra-headless-export/scripts/pdb_aliases.py --where ntoskrnl.exe MmCreateTeb` (이름 확인), `--where ntdll.dll LdrInitializeMrdata`
- 임시 스크립트 `ms/ghidra/_tmp_sast38/`: `pdbtypes.py` `pesec.py` `pubs.py` `gen_structs.py` `gen_globals.py` `gen_callees.py` `docs.py` `refs_index.py` `annot_struct.py` `apply_annot.py` `apply_globals.py` `run_all.sh`
- 확인한 `.asm`: `PspAllocateProcess` `PsMultiResumeThread` `MiMapProcessExecutable` `KeInitThread` `PspAllocateThread` `MmCreateTeb` (ntoskrnl), `LdrpInitializeProcess` `_LdrpInitialize` `LdrpInitializeNode` `LdrpInitializeNtdllDataTableEntry` `LdrpInsertModuleToIndexLockHeld` `LdrpInsertDataTableEntry` `LdrpInitializeTls` (ntdll)

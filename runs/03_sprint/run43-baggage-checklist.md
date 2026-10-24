# 2026-10-24 run43 — 짐 지도 체크리스트: 구조체 21개 직접 칸 1,456 (아는 쓴 자리 정확 138 · 짐작 43 · 모름 1,275), ntdll 쓰기 가능 전역 493 (아는 것 54), 초기화 함수 15개 callee 637줄 (어디에도 없음 57). `MmCreateTeb` 첫 export — callee 4개, TEB 대입 약 47줄

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 03_sprint 첫 바퀴 (짐 지도 A, plan Q76, pe-flow-cycle 단계 4).
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E`, ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE` (flow.md 기준과 같음). 두 프로젝트 다 다시 analyze 안 함.
- 버전 대조 (결과서): `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807`, `PspAllocateThread +0x631 → MmCreateTeb`, `LdrpInitializeProcess +0x1bbb`, `LdrInitializeThunk +0xe`, `LdrpInitializeInternal +0x5a`, `_LdrpInitialize +0x16c` 등이 `_calls.tsv` 와 전부 맞다.
- 이 문서를 쓰며 `MmCreateTeb` 의 call 줄 5개를 `.asm`·`_calls.tsv` 로 다시 맞춰 봤다 (아래 표). 결과서와 같다.

## 목적

- 짐 지도 체크리스트를 만든다 (요청서 질문 넷):
  1. PDB 구조체 칸 목록 (커널 14 + 유저 7)
  2. ntdll 전역 변수 목록 (쓰기 가능 구역)
  3. 초기화 함수 15개의 callee 목록 (`MmCreateTeb` 는 장부에 줄이 없어 export)
  4. 이미 있는 run 문서·결과서·`_refs` 의 "누가 씀" 을 칸마다 대조
- 칸마다 실제로 쓰는 줄을 새로 찾는 것 (Q78), 상태 사진 (Q77) 은 이번 범위 밖.

## 방법

- 정적: `_workspace/sast/38_baggage-checklist.md` — ntoskrnl `^MmCreateTeb$` depth 1 export (5개: `MmCreateTeb`, `KiStackAttachProcess`, `KiUnstackDetachProcess`, `MiAllocateFromSubAllocatedRegion`, `__security_check_cookie`. `_calls.tsv` 57줄 늘고 줄어든 줄 없음).
- PDB 타입 레코드·공개 이름: `llvm-pdbutil-14 dump -types / -publics / -globals` 두 PDB. 구역 표는 PE 머리를 직접 읽음. 하네스 밖 임시 스크립트 `ms/ghidra/_tmp_sast38/` (`run_all.sh` 로 다시 만듦).
- 결과서: `_workspace/sast/38_baggage-checklist_result.md` (**분석성공**)
- 표 원본 (커밋 안 함): `ms/disassemble/ntoskrnl/_baggage/`, `ms/disassemble/ntdll/_baggage/` — `struct__<이름>.tsv` ×21, `globals.tsv`, `globals_rdata.tsv`, `callees.tsv`·`callees_summary.tsv`, `known_writers.tsv`. 열 설명은 결과서 "표 파일 경로와 열 설명".
- 문서: 짐 지도 첫판 `runs/03_sprint/baggage.md` (flow-writer 가 이 표들로 만듦).

## 결과

비유 (이사 짐 목록):

- 집(프로세스)에 들어갈 **가구 칸 목록**을 설계도(PDB)에서 다 베껴 왔다. 칸이 1,456개.
- 그중 "이 칸엔 누가 무엇을 넣는다" 를 지난 기록(run9 ~ run42)에서 찾은 게 181칸. 나머지 1,275칸은 아직 빈칸이다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`, 이번에 새로 생긴 줄)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!MmCreateTeb` | `+0x7a` | `+0x7f` | `nt!KiStackAttachProcess` | [정적] |
| `nt!MmCreateTeb` | `+0xa7` | `+0xac` | `nt!MiAllocateFromSubAllocatedRegion` | [정적] |
| `nt!MmCreateTeb` | `+0x299` | `+0x29e` | `nt!KiUnstackDetachProcess` | [정적] |
| `nt!MmCreateTeb` | `+0x2ac` | `+0x2b1` | `nt!__security_check_cookie` | [정적] |
| `nt!MmCreateTeb` | `+0x2d3` | `+0x2d8` | `nt!KiUnstackDetachProcess` | [정적] |

- caller 쪽: `PspAllocateThread +0x62c` (ret `+0x631`) → `MmCreateTeb` (run11 과 같음).

### 호출 관계

```
PspAllocateThread
 └─ +0x631  MmCreateTeb (rva 0x9318c4, 811바이트)
      ├─ (1) +0x7f   KiStackAttachProcess              새 프로세스에 붙기
      ├─ (2) +0xac   MiAllocateFromSubAllocatedRegion  TEB 자리 [추측: 이름]
      │        실패 (+0xb0 JS) → +0x2d8 KiUnstackDetachProcess → 끝
      │      (TEB 대입 약 47줄 — 64비트 갈래 + WOW64 갈래, 디컴파일 기준)
      ├─ (3) +0x29e  KiUnstackDetachProcess            떼기 (성공 길)
      └─ (4) +0x2b1  __security_check_cookie
```

### 1. 구조체 칸 목록

| 구조체 | PDB | 크기 | 직접 칸 (비트 칸) | 아는 쓴 자리: 정확 | 짐작 | 모름 |
|---|---|---|---|---|---|---|
| `_EPROCESS` | nt | 0x840 | 263 (98) | 17 | 5 | 241 |
| `_KPROCESS` | nt | 0x1c8 | 65 (12) | 0 | 0 | 65 |
| `_ETHREAD` | nt | 0x798 | 149 (51) | 5 | 0 | 144 |
| `_KTHREAD` | nt | 0x4c0 | 237 (65) | 9 | 1 | 227 |
| `_KTRAP_FRAME` | nt | 0x190 | 66 (0) | 4 | 2 | 60 |
| `_HANDLE_TABLE` | nt | 0x80 | 17 (5) | 0 | 0 | 17 |
| `_MMVAD` | nt | 0x88 | 9 (0) | 0 | 0 | 9 |
| `_MMVAD_SHORT` | nt | 0x40 | 14 (0) | 0 | 0 | 14 |
| `_TOKEN` | nt | 0x5c8 | 55 (2) | 0 | 0 | 55 |
| `_SECTION` | nt | 0x40 | 9 (3) | 0 | 0 | 9 |
| `_CONTROL_AREA` | nt | 0x80 | 17 (0) | 1 | 1 | 15 |
| `_SEGMENT` | nt | 0x48 | 11 (0) | 1 | 0 | 10 |
| `_SECTION_IMAGE_INFORMATION` | nt | 0x40 | 26 (7) | 16 | 0 | 10 |
| `_FILE_OBJECT` | nt | 0xd8 | 30 (0) | 0 | 0 | 30 |
| `_PEB` | ntdll | 0x7d8 | 124 (23) | 19 | 12 | 93 |
| `_TEB` | ntdll | 0x1878 | 137 (17) | 4 | 0 | 133 |
| `_RTL_USER_PROCESS_PARAMETERS` | ntdll | 0x450 | 40 (0) | 18 | 22 | 0 |
| `_PEB_LDR_DATA` | ntdll | 0x58 | 9 (0) | 7 | 0 | 2 |
| `_LDR_DATA_TABLE_ENTRY` | ntdll | 0x138 | 65 (31) | 31 | 0 | 34 |
| `_LDR_DDAG_NODE` | ntdll | 0x50 | 10 (0) | 6 | 0 | 4 |
| `_KUSER_SHARED_DATA` | ntdll | 0xa80 | 103 (16) | 0 | 0 | 103 |
| **합** | | | **1,456 (330)** | **138** | **43** | **1,275** |

- 직접 칸 = 필드 목록의 `LF_MEMBER` (비트 칸도 한 칸). 이 PDB 는 이름 없는 union/struct 를 이미 평평하게 펴서 담았다.
- 요청서의 예상 타입 이름 (`_HANDLE_TABLE`, `_MMVAD`/`_MMVAD_SHORT`, `_TOKEN`, `_SECTION`/`_CONTROL_AREA`/`_SEGMENT`, `_FILE_OBJECT`) 은 **전부 그대로** 있다.
- 유저 7개는 ntdll·ntoskrnl PDB 에서 크기·칸·줄이 완전히 같다. 커널 구조체는 ntdll PDB 쪽이 칸이 적다 (`_EPROCESS` 261, `_KTHREAD` 235 — 일부가 `Reserved` 칸으로 가려짐) → 커널 PDB 를 씀.
- **`LdrSystemDllInitBlock` 블록 타입은 두 PDB 모두 없다** (run32 말이 맞다). 전역 `LdrSystemDllInitBlock` (`.mrdata` rva `0x1ea450`) 에서 다음 이름까지 거리 `0x128` = 블록 크기.

### 2. ntdll 전역 변수 (쓰기 가능 구역)

| 구역 | rva | 가상 크기 | 데이터 이름 | 쓴 자리 아는 것 (정확/짐작) |
|---|---|---|---|---|
| `.data` | `0x1cc000` | `0x8f58` | 438 | 47 (45/2) |
| `.mrdata` | `0x1e7000` | `0x35b8` | 50 | 6 (3/3) |
| `.00cfg` | `0x1e6000` | `0x28` | 5 | 1 (0/1) |

- 오케스트레이터가 센 `.data` 438 · `.mrdata` 50 이 맞다. `.00cfg` 는 PE 머리엔 읽기 전용이지만 로더가 값을 넣는 CFG 포인터 칸이라 따로 셌다.
- 크기는 전부 `[추측]` (공개 PDB 에 전역 크기 줄이 없음 → 다음 이름까지 거리).

### 3. 초기화 함수 15개의 callee

| 함수 | 호출 줄 | 서로 다른 callee | flow.md 에 나옴 | run·결과서에 이름만 | 어디에도 없음 |
|---|---|---|---|---|---|
| nt `NtCreateUserProcess` | 83 | 61 | 29 | 32 | 0 |
| nt `PspAllocateProcess` | 144 | 105 | 25 | 40 | **40** |
| nt `MmCreatePeb` | 18 | 15 | 7 | 8 | 0 |
| nt `PspSetupUserProcessAddressSpace` | 20 | 18 | 14 | 3 | 1 |
| nt `PspAllocateThread` | 44 | 37 | 13 | 24 | 0 |
| nt `MmCreateTeb` | 5 | 4 | 2 | 1 | 1 |
| nt `PspInsertProcess` | 21 | 20 | 8 | 12 | 0 |
| nt `PspInsertThread` | 59 | 39 | 14 | 25 | 0 |
| nt `PspUserThreadStartup` | 9 | 9 | 9 | 0 | 0 |
| nt `PspInitializeThunkContext` | 20 | 15 | 9 | 5 | 1 |
| ntdll `LdrInitializeThunk` | 3 | 3 | 3 | 0 | 0 |
| ntdll `LdrpInitialize` | 4 | 4 | 4 | 0 | 0 |
| ntdll `LdrpInitializeInternal` | 6 | 6 | 6 | 0 | 0 |
| ntdll `_LdrpInitialize` | 23 | 20 | 16 | 4 | 0 |
| ntdll `LdrpInitializeProcess` | 169 | 109 | 67 | 28 | **14** |

- `_calls.tsv` 줄은 nt 432 + ntdll 205 = 637 (조각 점프 줄 포함). 오케스트레이터 수와 다른 3곳 (`MmCreatePeb` 16 · `PspAllocateThread` 40 · `PspInsertThread` 42) 은 조각 점프 대상 `FUN_…` 을 callee 로 센 차이다.
- 간접 호출 줄 10곳 (대상은 동적으로만): nt `NtCreateUserProcess +0x5b2`·`+0x11bb`, `PspAllocateProcess +0x1215`·`+0x17bf`, `PspInitializeThunkContext +0x35b`; ntdll `_LdrpInitialize +0x9a`, `LdrpInitializeProcess +0x17fc`·`+0x1b59`·`+0x1cdf`·`+0x1d16`.
- 지도(`_map/`)에 없는 함수: `PspUserThreadStartup`, `PspInitializeThunkContext` (`SwapContext` 의 `RET` 로 들어오는 길이라 `NtCreateUserProcess` 뿌리에서 안 닿음, run15).

### 4. 이미 아는 "누가 씀" 대조

- 칸·전역마다 근거 (run 번호·결과서·`_refs` WRITE/READ_WRITE/DATA 줄) 와 확실도를 붙였다. `정확` = 쓰는 함수와 칸 이름·오프셋이 근거에 같이 있음, `짐작` = 범위로만 말함·칸 이름이 문서에서 `[추측]`·주소 넘김.
- 근거 문서: `runs/01_sprint` 7개 + `runs/02_sprint` 35개 (해설서 뺌) + sast/01 ~ 37 결과서. flow.md 2.1 · 2.2 일부 · 2.3 · 2.4 는 줄 단위, 나머지는 이름 검색 (결과서 한계).
- 짐작 43 중 22 는 `_RTL_USER_PROCESS_PARAMETERS` 의 "블록 통째 복사".
- 문서 오프셋을 `.asm` 로 다시 본 것이 있다: `_EPROCESS.UniqueProcessId` = `PspAllocateProcess +0x27e4` 저장 줄 (문서는 호출 자리 `+0x2703`), `CreateTime` `+0x27c4`/`+0x27dd`, PEB `FastPebLock` `+0x9e1`, `ProcessHeap` `+0xa39`, `LoaderLock` `_LdrpInitialize +0xfa`, `ProcessInitializing` `+0x107`/`+0x20f`, TEB `SameTebFlags` `+0xea`.
- 짐 지도 3장 (`baggage.md`) 은 이 대조 (`known_writers.tsv` 361줄) 를 (칸, 쓰는 함수) 줄로 묶어 옮겼다. flow-writer 가 flow.md 에서 본 쓴 자리 6줄을 더했다 (이미 아는 칸에만):
  - `_LDR_DATA_TABLE_ENTRY.DllBase` (DLL) ← 커널 `NtMapViewOfSection` (`LdrpMinimalMapModule +0x174`, run38)
  - `_LDR_DDAG_NODE.State` ← `LdrpNotifyLoadOfGraph +0x19` (7), `LdrpInitializeNode +0x1f`·`+0x2ba` (8 → 9), `LdrpInitializeGraphRecurse +0x6e` (-4) (run27)
  - `LdrpMapAndSnapWork` ← `LdrpEnableParallelLoading +0xe2 TpAllocWork`, `LdrpWorkQueueLock` ← `LdrpInitParallelLoadingSupport` 가 만듦 (run24)

## 확인된 것

- `[정적]` `MmCreateTeb` 는 새 프로세스에 붙고 (`+0x7a`) TEB 자리를 받은 뒤 (`+0xa7`) TEB 칸에 직접 대입하고 떼고 (`+0x299`) 돌아온다. 할당이 실패하면 `+0xb0 JS` 로 `+0x2d3` 쪽에서 떼고 끝 (`.asm`, `_calls.tsv`).
- `[정적]` run10 의 PEB 표 이름 7줄 (`[추측]` 이던 것) 이 PDB `_PEB` 칸 이름과 모두 맞는다 (결과서) → flow.md 2.1 이름 칸 `[정적]`.
- `[정적]` `LdrSystemDllInitBlock` 블록 타입은 두 PDB 에 없다 (결과서) → run32 의 칸 이름은 계속 `[추측]`.
- `[정적]` 오프셋이 PDB 와 안 맞는 문서 줄은 없었다 (결과서). 다만 문서의 `LoadCount = -1` 은 PDB 상 노드 칸 (`_LDR_DDAG_NODE +0x18`) 이라 짐작으로 붙였다 — 문서가 틀렸다기보다 칸 이름이 애매했던 것 (정정 아님).

## 아직 확인 필요

- 사진 (Q77): `LdrInitializeThunk` 입구 ①·`NtContinue` 입구 ② 에서 구조체·ntdll 쓰기 가능 구역을 떠서 "0 그대로"·② − ① 을 센다. 떠야 할 것·주소 얻는 칸은 결과서 "Q77(사진)에서 떠야 할 것". 간접 호출 10줄, 주소만 넘기는 전역 6개 값도.
- 쓴 자리·줄기 (Q78): `MmCreateTeb` TEB 칸별 표, 커널 객체 칸 (`PspAllocateProcess` callee 중 문서에 없는 40, 안 뽑은 `KeStartThread`·`KeInitializeProcess`·`ObInitProcess`·`PspInitializeProcessSecurity`·`PsAssignThreadId`·`PspApplyMitigationOptions`·`MiAllocateProcessVads`·`MiInsertProcessVads`), `LdrInitializeMrdata` 본문, 지도 뿌리 `PspUserThreadStartup` 추가, `.data` 문서에 없는 343개, flow.md 2.5 ~ 2.10 을 줄 단위로 대조 (이번에 빠진 쓴 자리가 더 있을 수 있음 — 예: 모름 칸인데 flow.md 에 쓴 곳이 있는 `ProcessAttachFailed`·`LoadOwner`·`EntryPointActivationContext`, baggage.md 7.5).

## 도구 메모

- 하네스 제안 (결과서, 고치지 않음): PDB 타입 → 칸 목록 TSV 를 만드는 스크립트 (`pdb_types.py` 같은 것) 가 `ghidra-headless-export/scripts/` 에 있으면 다음 빌드·다른 모듈에서 `ms/ghidra/_tmp_sast38/pdbtypes.py` + `gen_structs.py` 를 다시 짤 일이 없다.
- `_refs` WRITE 줄만으로는 API 에 주소를 넘겨 쓰는 전역 (락·이벤트) 이 안 잡힌다 (`LdrpWorkQueueLock` 은 복제 길 줄만 있었음). 쓴 자리 대조 때 DATA (`LEA`) 줄도 같이 보자.

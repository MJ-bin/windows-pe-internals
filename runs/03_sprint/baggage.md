# 짐 지도 — 초기화 동안 nt·ntdll 이 세팅하는 것

## 기준

- 빌드 26100.9457, 대상 test.exe (sample/)
- 범위: 프로세스 생성 (`NtCreateUserProcess`) → 은퇴 (`NtContinue` 로 `RtlUserThreadStart` 에 가는 순간)
- 근거: run43 (sast/38 체크리스트, 정적만). 3장의 "이미 아는 쓴 자리" 는 run9 ~ run42 · sast/01 ~ 37 결과서 · ntdll `_refs` 에서 옮긴 것 (8장). **사진 없음** — Q77 전
- PDB: ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E`, ntdll `C3093720177DA6851519DCFFE8DF53FE` (flow.md 기준과 같음)
  - 커널 구조체 14개는 ntoskrnl PDB, 유저 구조체 7개는 ntdll PDB 로 셌다. 유저 7개는 두 PDB 에서 크기·칸·줄이 완전히 같다 (결과서 1번).
- 첫판 (2026-10-24, run43). 칸 = PDB **직접 칸** (필드 목록의 `LF_MEMBER`, 비트 칸도 한 칸. 펼친 칸은 안 셈).
- 표 원본 (칸마다 오프셋·타입·비트, 쓴 자리 대조): `ms/disassemble/ntoskrnl/_baggage/`, `ms/disassemble/ntdll/_baggage/` (커밋 안 함. 열 설명은 sast/38 결과서 "표 파일 경로와 열 설명")

## 쓰는 말

- **공간**: `유저` (에뮬레이터 가상 주소 공간, 게스트가 직접 읽음) / `커널` (에뮬레이터 바깥, nt 엔진·syscall로만 닿음)
- **단계**: `①` 커널 단계 (`NtCreateUserProcess` ~ 유저로 넘어가기) / `②` ntdll 단계 (`LdrInitializeThunk` ~ `NtContinue`)
- **놓는 법**:
  - `직접`: 함수가 칸에 바로 쓴다 (커널은 새 프로세스에 붙은 상태에서 유저 칸에 바로 쓴다)
  - `주소 넘김`: 칸 주소를 넘기고, 받은 함수가 쓴다
  - `붙이기`: 파일·섹션 내용을 주소 공간에 붙인다
  - `syscall`: ntdll이 syscall을 부르면 커널이 채운다
  - `입력`: 부모·부팅·파일·레지스트리에서 온 값
- **쓴 주인**: `nt` / `ntdll` / `다른 DLL` (KERNELBASE·KERNEL32 등 — 범위 밖, 표시만) / `사용자 코드` (TLS·DllMain)
- **태그**: flow-doc 표시 태그 그대로 (`[관찰]` / `[정적]` / `[소스]` / `[추측]` / `[공백]`)

## 1. 공간 그림

- 숫자 = 3장에서 쓴 자리를 아는 칸 수 (주인별). `[모름 N]` = 7장으로 간 칸 수. 칸 수와 단계 표시는 3장 표에서 셌다.
- 한 칸을 nt 와 ntdll 이 둘 다 쓰면 양쪽에 센다.

```
유저 공간 (test.exe 주소 공간 — 게스트가 직접 읽음)
├─ PEB  _PEB 0x7d8 ............................................ ① nt 22 · ② ntdll 9   [모름 93]
│   └─ 프로세스 매개변수  _RTL_USER_PROCESS_PARAMETERS 0x450 .... ① nt 40 (그중 통째 복사 짐작 22) · ② ntdll 1   [모름 0]
├─ TEB  _TEB 0x1878 (메인 스레드) ............................. ① nt 1 · ② ntdll 3   [모름 133]
│        (① MmCreateTeb 의 TEB 대입 약 47줄 — 칸별로 안 갈라 [공백])
├─ ntdll 이미지 (커널이 ① 에 붙임)
│   ├─ .data  rva 0x1cc000 · 0x8f58 · 전역 438 ................. ② ntdll 46 · nt 1 (syscall)   [모름 391]
│   │   └─ PebLdr = _PEB_LDR_DATA 0x58 .......................... ② ntdll 7   [모름 2]
│   ├─ .mrdata  rva 0x1e7000 · 0x35b8 · 전역 50 ................ ① nt 1 (LdrSystemDllInitBlock) · ② ntdll 5   [모름 44]
│   └─ .00cfg  rva 0x1e6000 · 0x28 · 5칸 ......................... [공백] (짐작 1, 쓰는 코드 줄 없음)   [모름 4]
├─ 힙 위, 모듈마다 한 쌍 (`LdrpAllocateModuleEntry` 가 같이 만듦 — 어느 힙인지는 [공백])
│   ├─ 모듈 항목  _LDR_DATA_TABLE_ENTRY 0x138 .................. ② ntdll 31 · ② nt 1 (DllBase, syscall)   [모름 34]
│   └─ 그래프 노드  _LDR_DDAG_NODE 0x50 ......................... ② ntdll 6   [모름 4]
└─ KUSER_SHARED_DATA  0x7ffe0000 · 0xa80 ......................... [공백] (ntdll 은 읽기만)   [모름 103]

커널 공간 (에뮬레이터 바깥 — nt 엔진·syscall 로만 닿음)
├─ 프로세스 객체  _EPROCESS 0x840 ................................ ① nt 22   [모름 241]
│   ├─ Pcb = _KPROCESS 0x1c8 ...................................... [공백] (구조체 단위로만)   [모름 65]
│   ├─ ObjectTable (+0x300) → 핸들 표  _HANDLE_TABLE 0x80 ......... [공백]   [모름 17]
│   ├─ VadRoot (+0x558) → 메모리 구역 장부  _MMVAD 0x88 / _MMVAD_SHORT 0x40 ... [공백]   [모름 9 / 14]
│   ├─ Token (+0x248) → 토큰  _TOKEN 0x5c8 ......................... [공백]   [모름 55]
│   ├─ SectionObject (+0x2a8) → exe 섹션 묶음 (① 부모 스레드)
│   │   ├─ _SECTION 0x40 ........................................... [공백]   [모름 9]
│   │   ├─ _CONTROL_AREA 0x80 ...................................... ① nt 2   [모름 15]
│   │   ├─ _SEGMENT 0x48 ........................................... ① nt 1   [모름 10]
│   │   └─ 이미지 정보  _SECTION_IMAGE_INFORMATION 0x40 ............ ① nt 16   [모름 10]
│   └─ ImageFilePointer (+0x330) → 파일 객체  _FILE_OBJECT 0xd8 ... [공백]   [모름 30]
└─ 스레드 객체  _ETHREAD 0x798 (메인 스레드) ...................... ① nt 5   [모름 144]
    ├─ Tcb = _KTHREAD 0x4c0 ...................................... ① nt 10 (그중 ApcState 는 ② 끝에도)   [모름 227]
    └─ 트랩 프레임  _KTRAP_FRAME 0x190 (InitialStack − 0x190) .... ① nt 6 · ② 끝 nt 4 (NtContinue)   [모름 60]
```

- 칸 오프셋 (`+0x300` 등) 은 PDB 칸이고, 화살표 (→) 는 "그 칸이 가리키는 것" 이다 (결과서 Q77 추천 표). 유저 스택 위 맡긴 문맥 (`_CONTEXT`), API set 지도, 환경 블록 같은 구역은 아직 칸 표가 없어 그림에 없다 → 7장.

## 2. 구조체별 요약

| 구조체·구역 | 공간 | 칸 수 | nt 가 씀 | ntdll 이 씀 | 다른 DLL | 0 그대로 | 아직 모름 |
|---|---|---|---|---|---|---|---|
| `_PEB` | 유저 | 124 | 22 | 9 | 0 | 사진 전 | 93 |
| `_RTL_USER_PROCESS_PARAMETERS` | 유저 | 40 | 40 | 1 | 0 | 사진 전 | 0 |
| `_TEB` | 유저 | 137 | 1 | 3 | 0 | 사진 전 | 133 |
| `_PEB_LDR_DATA` | 유저 | 9 | 0 | 7 | 0 | 사진 전 | 2 |
| `_LDR_DATA_TABLE_ENTRY` | 유저 | 65 | 1 | 31 | 0 | 사진 전 | 34 |
| `_LDR_DDAG_NODE` | 유저 | 10 | 0 | 6 | 0 | 사진 전 | 4 |
| `_KUSER_SHARED_DATA` | 유저 | 103 | 0 | 0 | 0 | 사진 전 | 103 |
| ntdll `.data` 전역 | 유저 | 438 | 1 | 46 | 0 | 사진 전 | 391 |
| ntdll `.mrdata` 전역 | 유저 | 50 | 1 | 5 | 0 | 사진 전 | 44 |
| ntdll `.00cfg` 전역 | 유저 | 5 | 0 | 0 | 0 | 사진 전 | 4 (+ 짐작 1 은 주인 모름) |
| `_EPROCESS` | 커널 | 263 | 22 | 0 | 0 | 사진 전 | 241 |
| `_KPROCESS` | 커널 | 65 | 0 | 0 | 0 | 사진 전 | 65 |
| `_ETHREAD` | 커널 | 149 | 5 | 0 | 0 | 사진 전 | 144 |
| `_KTHREAD` | 커널 | 237 | 10 | 0 | 0 | 사진 전 | 227 |
| `_KTRAP_FRAME` | 커널 | 66 | 6 | 0 | 0 | 사진 전 | 60 |
| `_HANDLE_TABLE` | 커널 | 17 | 0 | 0 | 0 | 사진 전 | 17 |
| `_MMVAD` | 커널 | 9 | 0 | 0 | 0 | 사진 전 | 9 |
| `_MMVAD_SHORT` | 커널 | 14 | 0 | 0 | 0 | 사진 전 | 14 |
| `_TOKEN` | 커널 | 55 | 0 | 0 | 0 | 사진 전 | 55 |
| `_SECTION` | 커널 | 9 | 0 | 0 | 0 | 사진 전 | 9 |
| `_CONTROL_AREA` | 커널 | 17 | 2 | 0 | 0 | 사진 전 | 15 |
| `_SEGMENT` | 커널 | 11 | 1 | 0 | 0 | 사진 전 | 10 |
| `_SECTION_IMAGE_INFORMATION` | 커널 | 26 | 16 | 0 | 0 | 사진 전 | 10 |
| `_FILE_OBJECT` | 커널 | 30 | 0 | 0 | 0 | 사진 전 | 30 |
| **구조체 유저 7개 합** | | **488** | 64 | 57 | 0 | 사진 전 | **369** |
| **구조체 커널 14개 합** | | **968** | 62 | 0 | 0 | 사진 전 | **906** |
| **ntdll 전역 3구역 합** | | **493** | 2 | 51 | 0 | 사진 전 | **439** |

- 구조체 21개 직접 칸 **1,456** = 아는 칸 181 (정확 138 · 짐작 43) + 모름 **1,275** — 결과서 1번 표 합과 같다. 구조체마다 "아는 칸 = 정확 + 짐작" 은 3장 각 절 머리 줄.
- ntdll 전역 (쓰기 가능 2구역 + `.00cfg`) **493** = 쓴 자리 아는 것 54 (`.data` 47 · `.mrdata` 6 · `.00cfg` 1) + 모름 439 — 결과서 2번 표와 같다. 읽기 전용 `.rdata` 데이터 이름 1,546개 (`globals_rdata.tsv`) 는 짐이 아니라 안 셌다.
- **nt 가 씀 / ntdll 이 씀** = 3장 표에서 그 주인 줄이 하나라도 있는 칸 수. 한 칸을 둘이 쓰면 양쪽에 센다 (매개변수 `LoaderThreads`). syscall 로 커널이 채우는 칸은 nt 로 셌다 (`LdrpInitCompleteEvent`, DLL 항목 `DllBase`).
- `ntdll 이 씀` 에는 **범위 밖 길 (종료·복제·API) 에서만** 쓰는 전역 2개가 들어 있다: `LdrpDllDirectory`, `LdrpForkInProgress`. 초기화 동안엔 처음 값 그대로일 후보 → 사진으로 가린다 (7장).
- **다른 DLL 0** = 문서·결과서에 KERNELBASE·KERNEL32 등이 이 칸들에 쓰는 자리가 아직 하나도 없다는 뜻. 사진 (Q77) 의 ② − ① 차이에서 생길 수 있다.
- **0 그대로** 는 정적으로는 못 센다 (아무도 안 쓴다는 증거는 사진뿐). 그래서 전부 "사진 전" (Q77).
- **아직 모름** = 결과서 대조에서 쓴 자리를 못 찾은 칸. 이름은 7장에 다 있다.

## 3. 짐 표

읽는 법:

- 3장에는 **쓴 자리를 아는 칸만** 둔다 (결과서 4번 대조의 정확·짐작). 모르는 칸은 7장.
- 줄 하나 = (칸, 쓰는 함수). 같은 함수 안 여러 줄은 오프셋을 `·` 로 이었다. 근거가 같은 칸 여럿 (범위로만 말한 것, 통째 복사 등) 은 한 줄에 모았다. 같은 칸을 다른 함수가 또 쓰면 줄을 나눴다.
- 줄의 출처는 결과서 4번 대조 (`known_writers.tsv`). 그 밖에 flow-writer 가 이 문서를 쓰다 flow.md 에서 본 쓴 자리 **6줄**을 더했다 (근거 칸 **flow-writer 더함**). 이미 아는 칸에 쓴 곳만 더해서 칸 수는 안 바뀐다.
  - 결과서 4번은 flow.md 2.5 ~ 2.10 · run 문서를 검색으로만 봤다. 빠진 쓴 자리가 더 있을 수 있다 → Q78.
- **근거** 칸: `[정적]` = 확실도 정확 (쓰는 함수와 칸 이름·오프셋이 근거에 같이 있음), `[추측]` = 짐작 (왜 짐작인지는 무엇을 칸). `_refs` = ntdll 참조 표 (`ms/disassemble/ntdll/_refs/`) 의 WRITE·READ_WRITE·DATA 줄.
- **단계** 칸: `①` / `②` (쓰는 말) 에 더해 `② 끝` = `NtContinue` 안 (커널), `② 일꾼` = ntdll 일꾼 스레드 (flow.md 3장), `—` = 초기화 범위 밖 길 (종료·복제·API), `?` = 지도 경로 모름.
- **경로** 칸: 함수 이름 사슬, `→` 부름, `⇒` 꼬리 JMP. 오프셋은 마지막 함수 뒤 (그 함수 안 쓰는 줄. `호출 +0x..` 은 부르는 자리). 사슬 머리는 줄여 쓴다:

| 줄임 | 사슬 |
|---|---|
| `①…` | `NtCreateUserProcess` — 부모 cmd.exe 스레드의 시스템 콜 안 (flow.md 2.1) |
| `①T…` | `KiStartUserThread → PspUserThreadStartup` — test.exe 스레드, 유저로 가기 전 (2.3). `KiStartUserThread` 에는 call 이 아니라 `SwapContext` 의 `RET` 로 들어온다 |
| `②…` | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess` (2.4) |
| `②M…` | DLL 하나를 붙여 명부에 올리는 `LdrpMapDllWithSectionHandle` 까지. kernel32·kernelbase: `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpFindOrPrepareLoadingModule → LdrpLoadKnownDll → LdrpMapDllWithSectionHandle` (2.5, 지도 이름 사슬). dll1·dll2: `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal → (일감) LdrpProcessWork → LdrpMapDllSearchPath → LdrpMapDllNtFileName → LdrpMapDllWithSectionHandle` (2.6) |
| `{ntdll · exe · DLL}` | 모듈 항목마다 부르는 자리가 다르다 (아래 표) |

모듈마다 부르는 자리 (flow.md 2.4·2.6, call 또는 ret):

| 함수 | ntdll 항목 | exe 항목 | DLL 항목 |
|---|---|---|---|
| `LdrpAllocateModuleEntry` | `LdrpInitializeNtdllDataTableEntry +0x6c` | `LdrpInitializeProcess +0x135e` | 의존 DLL `LdrpLoadDependentModuleInternal +0x115c`, `LdrLoadDll` 길 `LdrpFindOrPrepareLoadingModule +0x47a` |
| `LdrpInsertDataTableEntry` | `LdrpInitializeNtdllDataTableEntry +0xf7` | `LdrpInitializeProcess +0x1455` | `LdrpMapDllWithSectionHandle +0x146` |
| `LdrpInsertModuleToIndexLockHeld` | `… +0x116 LdrpInsertModuleToIndex` 안 | `… +0x14d2 LdrpInsertModuleToIndex` 안 | `LdrpMapDllWithSectionHandle +0x151` (직접) |
| `LdrpProcessMappedModule` | `LdrpInitializeNtdllDataTableEntry +0x124` | `LdrpInitializeProcess +0x14ee` | `LdrpMapDllWithSectionHandle +0x26c` |

**유저 공간**

### 3.1 `_PEB` — PEB (유저, `0x7d8`, ntdll PDB)

- 칸 124 · 아는 칸 31 (정확 19 · 짐작 12) · 모름 93 · 줄 14

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `InheritedAddressSpace` · `+0x3` `BitField` · `+0x8` `Mutant` | ① | `PspAllocateProcess` 가 넘긴 견본을 복사 | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 |
| `+0x10` `ImageBaseAddress` | ① | exe 를 붙인 주소 (`_EPROCESS.SectionBaseAddress` 를 옮김) | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 |
| `+0x18` `Ldr` | ② | ntdll 전역 `PebLdr` 주소 (빈 목록 3개로 채운 뒤) | 직접 | ntdll | `②… +0x1c1` | `[정적]` run18 · 정확 |
| `+0x20` `ProcessParameters` | ① | 새 프로세스 안 매개변수 블록 주소 (복사가 끝난 뒤). 갈래는 기능 스위치 (Q35) | 직접 (켜짐 갈래는 `RtlWriteULong64ToUser`) | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters +0x52b · +0x347` | `[정적]` run32 · sast27 · 정확 |
| `+0x30` `ProcessHeap` | ② | 프로세스 힙 핸들. 쓰는 줄은 callee 안 (안 봄) | 주소 넘김 (칸 주소 `+0xa39`) | ntdll | `②… → LdrpInitializeProcessHeap (호출 +0xa4b)` | `[정적]` run18 · 정확 |
| `+0x38` `FastPebLock` | ② | ntdll 전역 `FastPebLock` 주소 (`RtlInitializeCriticalSectionEx` 로 만든 뒤) | 직접 | ntdll | `②… +0x9e1` | `[정적]` run18 · 정확 |
| `+0x50` `ProcessInitializing` (`CrossProcessFlags` 비트 1) | ② | 켬 (`BTS.LOCK`) → 끝에 끔 (`BTR.LOCK`). WOW64 길은 `LdrpInitializeProcess +0x17e7` 이 끔 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x107 · +0x20f` | `[정적]` run17 · sast12 · 정확 |
| `+0x78` `TlsBitmap` · `+0x80` `TlsBitmapBits` · `+0x238` `TlsExpansionBitmap` · `+0x240` `TlsExpansionBitmapBits` | ② | TLS 자리 비트맵 — 비트맵 구조와 비트 칸을 가리키게 설정 (칸마다 줄은 묶음으로만 봄) | 직접 | ntdll | `②… +0x774 ~ +0x8a3` | `[정적]` run18 · sast13 · 정확 |
| `+0xb8` `NumberOfProcessors` · `+0xbc` `NtGlobalFlag` · `+0xc0` `CriticalSectionTimeout` · `+0xc8` `HeapSegmentReserve` · `+0xd0` `HeapSegmentCommit` · `+0xd8` `HeapDeCommitTotalFreeThreshold` · `+0xe0` `HeapDeCommitFreeBlockThreshold` | ① | 프로세서 개수·전역 플래그 등 — run10 은 `+0xb8 ~ +0xe0` **범위로만** 말함 | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[추측]` run10 · 짐작 |
| `+0x110` `LoaderLock` | ② | `&LdrpLoaderLock` | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0xfa` | `[정적]` run17 · sast12 · 정확 |
| `+0x118` `OSMajorVersion` · `+0x11c` `OSMinorVersion` · `+0x120` `OSBuildNumber` · `+0x122` `OSCSDVersion` · `+0x124` `OSPlatformId` | ① | 버전 숫자 (exe 헤더 값이 있으면 덮어씀) — `+0x118 ~ +0x124` **범위로만** | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[추측]` run10 · 짐작 |
| `+0x128` `ImageSubsystem` · `+0x12c` `ImageSubsystemMajorVersion` · `+0x130` `ImageSubsystemMinorVersion` | ① | exe 헤더의 서브시스템 값 (`+0x128 ~ +0x130`, 칸 3개와 정확히 맞음) | 직접 (값은 파일 헤더 = 입력) | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 |
| `+0x2c0` `SessionId` | ① | 세션 번호 | 직접 | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 |
| `+0x7b8` `LeapSecondData` | ① | leap second 뷰 주소 (뷰는 `MmMapViewOfSection` `+0x132` 로 붙임) | 직접 (뷰는 붙이기) | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 |

### 3.2 `_RTL_USER_PROCESS_PARAMETERS` — 프로세스 매개변수 (유저, `0x450`, ntdll PDB)

- 칸 40 · 아는 칸 40 (정확 18 · 짐작 22) · 모름 0 · 줄 9
- 블록 전체: 부모 (KERNELBASE) 가 만든 사본을 커널이 새 프로세스로 복사 (run32). 복사 뒤 ntdll `LdrpInitializeProcess +0x6c5 RtlNormalizeProcessParams` 가 무엇을 하는지는 모름.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `MaximumLength` · `+0x4` `Length` · `+0xc` `DebugFlags` · `+0x18` `ConsoleFlags` · `+0x88` `StartingX` · `+0x8c` `StartingY` · `+0x90` `CountX` · `+0x94` `CountY` · `+0x98` `CountCharsX` · `+0x9c` `CountCharsY` · `+0xa0` `FillAttribute` · `+0xa4` `WindowFlags` · `+0xa8` `ShowWindowFlags` · `+0xf0` `CurrentDirectores` · `+0x3f0` `EnvironmentSize` · `+0x3f8` `EnvironmentVersion` · `+0x400` `PackageDependencyData` · `+0x408` `ProcessGroupId` · `+0x438` `DefaultThreadpoolCpuSetMaskCount` · `+0x43c` `DefaultThreadpoolThreadMaximum` · `+0x440` `HeapMemoryTypeMask` · `+0x448` `AttributeList` | ① | 부모 쪽이 만든 블록 값 **그대로** (블록 통째 복사). `PackageDependencyData` 포인터와 `CurrentDirectores` 안쪽은 Fixup 도 안 함 (run32, 이유 모름). 칸 단위로 말한 문서는 없음 | 입력 (통째 복사) | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters +0x41f · +0x324` | `[추측]` run32 · 짐작 |
| `+0x8` `Flags` | ① | 비트 29 `0x20000000` (조건 silo `OneCoreForwardersEnabled`) · 비트 15 `0x8000` (조건 `PspGlobalFlags` 비트 0). **붙기 전 커널 사본**에 쓰고 복사 때 같이 넘어감 | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace +0xe6 · +0xb1` | `[정적]` run32 · sast27 · 정확 |
| `+0x8` `Flags` | ① | 비트 14 `0x4000` (조건부) | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters +0x82` | `[정적]` run32 · sast27 · 정확 |
| `+0x10` `ConsoleHandle` | ① | 부모 콘솔 핸들을 새 프로세스용으로 복제한 값 (서브시스템 3 이고 핸들 > 0 일 때) | 주소 넘김 (`ObDuplicateObject`) | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters → ObDuplicateObject (ret +0xbc)` | `[정적]` run32 · 정확 |
| `+0x20` `StandardInput` · `+0x28` `StandardOutput` · `+0x30` `StandardError` | ① | 표준 핸들 복제 (복제할 목록이 있을 때, 세 번 도는 고리) | 주소 넘김 (`ObDuplicateObject`) | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters → ObDuplicateObject (ret +0x130)` | `[정적]` run32 · 정확 |
| `+0x38` `CurrentDirectory` · `+0x50` `DllPath` · `+0x60` `ImagePathName` · `+0x70` `CommandLine` · `+0xb0` `WindowTitle` · `+0xc0` `DesktopInfo` · `+0xd0` `ShellInfo` · `+0xe0` `RuntimeData` · `+0x410` `RedirectionDllName` · `+0x420` `HeapPartitionName` | ① | Fixup: 안쪽 `Buffer` 포인터만 새 주소 기준 절대 주소로 (`CurrentDirectory` 는 `DosPath.Buffer`) | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters` | `[정적]` run32 · 정확 |
| `+0x80` `Environment` · `+0x430` `DefaultThreadpoolCpuSetMasks` | ① | Fixup: 새 주소 기준 절대 주소로 | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspCopyAndFixupParameters` | `[정적]` run32 · 정확 |
| `+0x40c` `LoaderThreads` | ① | 0 이면 전역 `PsDefaultLoaderThreads` 로 채움 (붙기 전 커널 사본) | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace +0xb8 ~ +0xc7` | `[정적]` run32 · sast27 · 정확 |
| `+0x40c` `LoaderThreads` | ② | IFEO `MaxLoaderThreads` 로 덮을 수 있음 (값이 있을 때) | 직접 | ntdll | `②… → LdrpInitializeExecutionOptions (호출 ret +0x536)` | `[정적]` run18 · run32 · 정확 |

### 3.3 `_TEB` — TEB (메인 스레드) (유저, `0x1878`, ntdll PDB)

- 칸 137 · 아는 칸 4 (정확 4 · 짐작 0) · 모름 133 · 줄 4

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `NtTib` | ② | 안쪽 `ArbitraryUserPointer`(+0x28) 만 DLL 이름으로 바꿨다가 `+0x17b ~ +0x17f` 에서 되돌림 | 직접 | ntdll | `②M… → LdrpMinimalMapModule +0xe2 ~ +0xea` | `[정적]` run38 · 정확 |
| `+0x108` `CurrentLocale` | ① | `PsGetSessionLocaleId` 결과 (SEH `try` 안) | 직접 | nt | `KiStartUserThread → PspUserThreadStartup +0x78` | `[정적]` run15 · 정확 |
| `+0x17ee` `RanProcessInit` (`SameTebFlags` 비트 5) | ② | `SameTebFlags` 에 `0x20` 켬 (첫 스레드 길) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0xea` | `[정적]` run17 · sast12 · 정확 |
| `+0x1850` `SchedulerSharedDataSlot` | ② | 락 소유 칸 표 주소 — 바로 앞 `+0x84 NtSetInformationThread`(클래스 0x39) 로 커널에서 받은 값. 조건이 안 맞으면 0 으로 남음 | 직접 | ntdll | `②… → LdrpAcquireSchedulerSharedDataSlot +0x92` | `[정적]` run41 · 정확 |

### 3.4 `_PEB_LDR_DATA` — 로더 명부 머리 (= ntdll 전역 `PebLdr`) (유저, `0x58`, ntdll PDB)

- 칸 9 · 아는 칸 7 (정확 7 · 짐작 0) · 모름 2 · 줄 12

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `Length` | ② | `0x58` (= PDB 크기) | 직접 | ntdll | `②… +0x158` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `+0x4` `Initialized` | ② | 바이트 하나 (값은 레지스터 `R15B` — 사진으로) | 직접 | ntdll | `②… +0x166` | `[정적]` `_refs` WRITE · 정확 |
| `+0x10` `InLoadOrderModuleList` | ② | 빈 목록 머리 | 직접 | ntdll | `②… +0x174 · +0x17b` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `+0x10` `InLoadOrderModuleList` | ② | 항목 끼우기 — 꼬리 (exe 만 머리) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0xaf · +0xeb` | `[정적]` `_refs` WRITE · run35 · 정확 |
| `+0x20` `InMemoryOrderModuleList` | ② | 빈 목록 머리 | 직접 | ntdll | `②… +0x189 · +0x190` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `+0x20` `InMemoryOrderModuleList` | ② | 항목 끼우기 — 꼬리 (exe 만 머리) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x57 · +0x107` | `[정적]` `_refs` WRITE · run35 · 정확 |
| `+0x30` `InInitializationOrderModuleList` | ② | 빈 목록 머리 (`+0x19e`·`+0x1a5`) → ntdll 항목 (핫패치면 패치 항목도) 머리에 끼움 (`+0x12a6`·`+0x12aa`·`+0x12d7`) | 직접 | ntdll | `②… +0x19e · +0x1a5 · +0x12a6 · +0x12aa · +0x12d7` | `[정적]` `_refs` WRITE · run35 · 정확 |
| `+0x30` `InInitializationOrderModuleList` | ② | 나머지 모듈을 켜기 직전 꼬리에 (고리 A, exe 빼고) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse → LdrpInitializeNode +0x2e1` | `[정적]` `_refs` WRITE · run27 · 정확 |
| `+0x48` `ShutdownInProgress` | ② | 처음 값 (`R14B`) | 직접 | ntdll | `②… +0x1ac` | `[정적]` `_refs` WRITE · 정확 |
| `+0x48` `ShutdownInProgress` | — | 종료 길에서 다시 씀 (범위 밖) | 직접 | ntdll | `LdrShutdownProcess +0xae` | `[정적]` `_refs` WRITE · 정확 |
| `+0x50` `ShutdownThreadId` | ② | 처음 값 (`R14`) | 직접 | ntdll | `②… +0x1b3` | `[정적]` `_refs` WRITE · 정확 |
| `+0x50` `ShutdownThreadId` | — | 종료 길에서 다시 씀 (범위 밖) | 직접 | ntdll | `LdrShutdownProcess +0xa7` | `[정적]` `_refs` WRITE · 정확 |

### 3.5 `_LDR_DATA_TABLE_ENTRY` — 모듈 항목 (모듈마다 하나) (유저, `0x138`, ntdll PDB)

- 칸 65 · 아는 칸 31 (정확 31 · 짐작 0) · 모름 34 · 줄 38 (그중 flow-writer 더함 1)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `InLoadOrderLinks` | ② | 적재 순서 목록에 끼우기 — 꼬리 (exe 는 `+0xbd ~ +0x10e` 머리) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x87 ~ +0xaf` | `[정적]` run35 · 정확 |
| `+0x10` `InMemoryOrderLinks` | ② | 메모리 순서 목록에 끼우기 — 꼬리 (exe 는 머리) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x4d ~ +0x57` | `[정적]` run35 · 정확 |
| `+0x20` `InInitializationOrderLinks` | ② | ntdll 항목을 초기화 순서 목록 머리에. 나머지 모듈은 `LdrpInitializeNode` 고리 A 가 꼬리에 (위 `_PEB_LDR_DATA` 표) | 직접 | ntdll | `②… +0x1283 ~ +0x12d7` | `[정적]` run35 · run18 · 정확 |
| `+0x30` `DllBase` | ② | exe 항목: `PEB.ImageBaseAddress` | 직접 | ntdll | `②… +0x144c` | `[정적]` run19 · 정확 |
| `+0x30` `DllBase` | ② | ntdll 항목: 이미지 시작 주소 | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0xdc` | `[정적]` run35 · 정확 |
| `+0x30` `DllBase` | ② | DLL 항목: 기준 주소 칸 = 이 칸 → **커널이 직접 채운다** (0 으로 시작) | syscall (`NtMapViewOfSection`) | nt | `②M… → LdrpMinimalMapModule +0x174` | `[정적]` run38 (flow.md 2.6) · 정확 · **flow-writer 더함** |
| `+0x38` `EntryPoint` | ② | PE 헤더 `AddressOfEntryPoint` 로 정함 (ntdll 은 헤더 값이 0 이라 0) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpProcessMappedModule +0x4a ~ +0x78` | `[정적]` run35 · 정확 |
| `+0x40` `SizeOfImage` · `+0x80` `TimeDateStamp` · `+0x120` `CheckSum` | ② | NT 헤더 값을 항목에 옮김 | 직접 (값은 헤더 = 입력) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0xa ~ +0x2e` | `[정적]` run35 · 정확 |
| `+0x48` `FullDllName` | ② | exe 항목: `ImagePathName` | 직접 | ntdll | `②… +0x13aa` | `[정적]` run19 · 정확 |
| `+0x48` `FullDllName` | ② | ntdll 항목: `<루트>\SYSTEM32\ntdll.dll` | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0xb6 ~ +0xd7` | `[정적]` run35 · 정확 |
| `+0x58` `BaseDllName` | ② | exe 항목: 파일 이름 부분 | 직접 | ntdll | `②… +0x1408` | `[정적]` run19 · 정확 |
| `+0x58` `BaseDllName` | ② | ntdll 항목: 상수 `NtDllName` (`ntdll.dll`) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0xb6 ~ +0xd7` | `[정적]` run35 · 정확 |
| `+0x68` `Flags` | ② | 로드 문맥 `+0x20` 의 비트 몇 개를 옮김 (어느 비트인지·오프셋은 문서에 없음) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 |
| `+0x68` `ImageDll` (`Flags` 비트 2) | ② | 켬 | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0x11a` | `[정적]` run35 · 정확 |
| `+0x68` `ProcessStaticImport` (`Flags` 비트 5) | ② | 켬 — `LdrInitState == 1` 이고 지금 스레드가 `LdrpDllNotificationLock` 주인이 아닐 때 | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0x194` | `[정적]` run22 · 정확 |
| `+0x68` `InLegacyLists` (`Flags` 비트 6) | ② | 켬 (이미 켜졌으면 아무것도 안 함) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x5e` | `[정적]` run35 · 정확 |
| `+0x68` `InIndexes` (`Flags` 비트 7) | ② | 켬 (`BTS`) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0xec` | `[정적]` run35 · 정확 |
| `+0x68` `InExceptionTable` (`Flags` 비트 9) | ② | ntdll 항목: `Flags` 에 `0x204` 중 새 비트 | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x78 ~ +0x93` | `[정적]` run35 · 정확 |
| `+0x68` `ProcessAttachCalled` (`Flags` 비트 19) | ② | 진입점을 부른 뒤 켬 (예외 없었을 때) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse → LdrpInitializeNode +0x231` | `[정적]` run27 · 정확 |
| `+0x6c` `ObsoleteLoadCount` | ② | ntdll 항목: `0xffff` (안 내려가는 모듈) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x78 ~ +0x93` | `[정적]` run35 · 정확 |
| `+0x6c` `ObsoleteLoadCount` | ② | exe 항목: 문서엔 "`LoadCount` = -1" 만 있음 (`LoadCount` 는 PDB 상 노드 칸) | 직접 | ntdll | `②…` | `[추측]` run19 · 짐작 |
| `+0x6c` `ObsoleteLoadCount` | ② | 새 항목 처음 값 = 6 (오프셋 미상) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 |
| `+0x6e` `TlsIndex` | ② | DLL 항목: `0xffff` (TLS 디렉터리 있을 때) | 직접 | ntdll | `②… → … LdrpSnapModule → LdrpHandleTlsData +0x3a8` | `[정적]` run28 · 정확 |
| `+0x6e` `TlsIndex` | ② | exe 항목: `0xffff` (TLS 디렉터리 있을 때) | 직접 | ntdll | `②… → LdrpInitializeTls +0x102` | `[정적]` run28 · 정확 |
| `+0x70` `HashLinks` | ② | 해시 표 `LdrpHashTable` 버킷 꼬리에 (버킷 = 해시 & 0x1f) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x28 ~ +0x7a` | `[정적]` run35 · 정확 |
| `+0x98` `DdagNode` | ② | 같이 만든 새 그래프 노드 (항목 0x138 + 노드 0x50 한 쌍) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 |
| `+0xb0` `LoadContext` | ② | 로드 문맥 (문맥 `+0x38` = 항목, 서로 가리킴) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0x7f ~ +0x96` | `[정적]` run22 · 정확 |
| `+0xb8` `ParentDllBase` | ② | 부모 `DllBase` | 직접 | ntdll | `②M… +0x286 ~ +0x28f` | `[정적]` run22 · run26 · 정확 |
| `+0xc8` `BaseAddressIndexNode` | ② | 트리 색인 `LdrpModuleBaseAddressIndex` 에 넣기 (키 = `DllBase`) | 주소 넘김 (`RtlRbInsertNodeEx`) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0xe7` | `[정적]` run35 · 정확 |
| `+0xe0` `MappingInfoIndexNode` | ② | 트리 색인 `LdrpMappingInfoIndex` 에 넣기 (키 = TimeDateStamp, SizeOfImage) | 주소 넘김 (`RtlRbInsertNodeEx`) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0x91` | `[정적]` run35 · 정확 |
| `+0xf8` `OriginalBase` | ② | NT 헤더 `ImageBase` 로 덮음 (ntdll 은 앞서 `+0x9f ~ +0xa6` 이 뺀 값을 덮음) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpProcessMappedModule +0x97` | `[정적]` run35 · 정확 |
| `+0x108` `BaseNameHashValue` | ② | 0 이면 `LdrpHashUnicodeString(&BaseDllName)` 결과로 채움 | 주소 넘김 | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertDataTableEntry +0x1d` | `[정적]` run35 · 정확 |
| `+0x10c` `LoadReason` | ② | exe 항목: 부모 칸이 0 이라 4 로 시작 (이 함수가 다시 쓰는 줄은 없음) | 직접 | ntdll | `②… → LdrpAllocateModuleEntry` | `[추측]` run22 · 짐작 |
| `+0x10c` `LoadReason` | ② | 의존 DLL 새 항목 = 인자 4 | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0x1179` | `[정적]` run23 · 정확 |
| `+0x110` `ImplicitPathOptions` | ② | 로드 문맥 `+0x10` 이 가리키는 구조체의 `+0x18` | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0x86 ~ +0x8d` | `[정적]` run22 · 정확 |
| `+0x114` `ReferenceCount` | ② | 새 항목 = 2 | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry +0xfb` | `[정적]` run22 · 정확 |
| `+0x130` `HotPatchState` | ② | = 1 (`LoadReason` 9 일 때) | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0x118a` | `[정적]` run23 · 정확 |
| `+0x130` `HotPatchState` | ② | 핫패치 둘째 호출 때 원래 3 / 패치 2 (오프셋 미상) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry` | `[추측]` run35 · 짐작 |

### 3.6 `_LDR_DDAG_NODE` — 그래프 노드 (모듈마다 하나) (유저, `0x50`, ntdll PDB)

- 칸 10 · 아는 칸 6 (정확 6 · 짐작 0) · 모름 4 · 줄 18 (그중 flow-writer 더함 3)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `Modules` | ② | 이 항목 하나든 목록 | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 |
| `+0x18` `LoadCount` | ② | 새 노드 처음 값 = 1 (오프셋 미상) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpAllocateModuleEntry` | `[정적]` run22 · 정확 |
| `+0x18` `LoadCount` | ② | 간선을 안 쓰는 경우 (부모 = 자식 · 자식 `State` 9 …) 자식 것을 1 되돌림 | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0x1294` | `[정적]` run23 · 정확 |
| `+0x18` `LoadCount` | ② | ntdll: -1 (문서는 "`LoadCount` = -1" 만 적음 — PDB 에서 이 이름은 노드 칸) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x78 ~ +0x93` | `[추측]` run35 · 짐작 |
| `+0x20` `LowestLink` | ② | = min (고리 묶기 번호, 오프셋 미상) | 직접 | ntdll | `②… → LdrpPrepareModuleForExecution → LdrpCondenseGraph → LdrpCondenseGraphRecurse` | `[정적]` run24 · 정확 |
| `+0x28` `Dependencies` | ② | 간선 칸을 부모 노드 목록 꼬리에 끼움 | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0xf69` | `[정적]` run23 · 정확 |
| `+0x30` `IncomingDependencies` | ② | 간선 칸 `+0x10` 을 자식 노드 목록 꼬리에 | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0xf8b` | `[정적]` run23 · 정확 |
| `+0x38` `State` | ② | ntdll 노드 = 9 (`ReadyToRun`) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x150` | `[정적]` run35 · 정확 |
| `+0x38` `State` | ② | = 2 (`Mapped`) | 직접 | ntdll | `②… → {ntdll · exe · DLL} → LdrpProcessMappedModule +0x1c2` | `[정적]` run24 · run35 · 정확 |
| `+0x38` `State` | ② | exe 노드 = 9 (`+0x1508`) → 다시 2 (`+0x1a60`) | 직접 | ntdll | `②… +0x1508 · +0x1a60` | `[정적]` run19 · 정확 |
| `+0x38` `State` | ② | = 4 (`Snapping`, `+0x4ac`) / IAT 없으면 = 5 (`Snapped`, `+0x5ae`) | 직접 | ntdll | `②… → LdrpMapAndSnapDependency +0x4ac · +0x5ae` | `[정적]` run22 · 정확 |
| `+0x38` `State` | ② | 부모 노드 = 3 (`WaitingForDependencies`) | 직접 | ntdll | `②… → LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal +0x162d` | `[정적]` run23 · 정확 |
| `+0x38` `State` | ② | 부모 노드 = 4 (자식이 Mapped 되어 카운터 0 일 때) | 직접 | ntdll | `②M… → LdrpProcessMappedModule → LdrpSignalModuleMapped +0x52` | `[정적]` run24 · 정확 |
| `+0x38` `State` | ② | importer 노드 = 5 | 직접 | ntdll | `②… → {LdrpMapAndSnapDependency · LdrpProcessWork} → LdrpSnapModule +0xb87` | `[정적]` run36 · 정확 |
| `+0x38` `State` | ② | 혼자인 노드 5 → 6 (`Condensed`) | 직접 | ntdll | `②… → LdrpPrepareModuleForExecution → LdrpCondenseGraph → LdrpCondenseGraphRecurse +0x96 ~ +0xa3` | `[정적]` run24 · 정확 |
| `+0x38` `State` | ② | = 7 (`ReadyToInit`), 실패면 `+0x29` 에서 6 으로 되돌림 | 직접 | ntdll | `②… → LdrpPrepareModuleForExecution → LdrpNotifyLoadOfGraph +0x19` | `[정적]` run27 (flow.md 2.7, 줄만 봄) · 정확 · **flow-writer 더함** |
| `+0x38` `State` | ② | = 8 (`Initializing`, `+0x1f` 무조건) → 끝에 9 (`+0x2ba`, 실패면 -4) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse → LdrpInitializeNode +0x1f · +0x2ba` | `[정적]` run27 (flow.md 2.7) · 정확 · **flow-writer 더함** |
| `+0x38` `State` | ② | 자식이나 자기 켜기가 실패하면 = -4 (`InitError`) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse +0x6e` | `[정적]` run27 (flow.md 2.7) · 정확 · **flow-writer 더함** |

### 3.7 `_KUSER_SHARED_DATA` — 공유 사용자 데이터 (KUSD) (유저, `0xa80`, ntdll PDB)

- 칸 103 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 103 · 줄 0
- ntdll 은 **읽기만** 한다 (`Cookie` +0x330, `UserModeGlobalLogger` 쪽 +0x384 등 — run19 · run20 · run28). 쓰는 쪽 근거 문서 없음 `[공백]`.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

**유저 공간 — ntdll 전역**

### 3.8 ntdll `.data` 전역 (유저, rva `0x1cc000`, 크기 `0x8f58`)

- 구역: 쓰기 가능 (파일엔 0x2000 만, 나머지는 0 으로 시작 — run17)
- 전역 438 · 쓴 자리 아는 것 47 (정확 45 · 짐작 2) · 모름 391 · 줄 73 (그중 flow-writer 더함 2). 크기는 전부 `[추측]` (다음 이름까지 거리, 결과서 2번)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `LdrpTlsList` (`0x1cc938`) | ② | TLS 목록에 새 항목 잇기 | 직접 | ntdll | `②… → {LdrpInitializeTls (exe) · … LdrpHandleTlsData (DLL)} → LdrpAllocateTlsEntry +0x10a` | `[정적]` `_refs` WRITE · run28 · 정확 |
| `LdrpDebugFlags` (`0x1ccf60`) | ② | 묶음 5 "전역 켜고 끄기" 에 이름이 나옴 (칸 단위 줄은 문서에 없음) | 직접 | ntdll | `②… +0x57a ~ +0x773` | `[추측]` run18 · sast13 · 짐작 |
| `LdrpAdvapi32DllHandle` (`0x1d2310`) | ② | ADVAPI32 모듈 핸들 (레지스트리 값이 있을 때만) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrpCodeAuthzInitialize +0x19e` | `[정적]` run20 · 정확 |
| `RtlpSystem32Dirs` (`0x1d2330`) | ② | 16바이트 쓰기 (`MOVDQU`, 묶음 9 시스템 경로 — 내용은 안 봄) | 직접 | ntdll | `②… +0x109e` | `[정적]` `_refs` WRITE · run39 · 정확 |
| `RtlpSystem32Dirs` (`0x1d2330`) | ② | `+0x8` 칸을 다시 씀 (뜻 모름, dll 올리기 뒤) | 직접 | ntdll | `②… → LdrpInitializePerUserWindowsDirectory +0x164` | `[정적]` `_refs` WRITE · run39 · 정확 |
| `RtlpSystem32Dirs` (`0x1d2330`) | — | `+0x10` 칸 `CMPXCHG.LOCK` — test.exe 길엔 관여 안 함 (플래그 `0x2000` 일 때만, run39) | 직접 | ntdll | `… LdrpSearchPath → RtlpLookupSafeCurDirList +0x135` | `[정적]` `_refs` READ_WRITE · run39 · 정확 |
| `RtlpSystemDirs` (`0x1d2360`) | ② | DLL 검색 목록의 시스템 폴더 덩어리 (`System32;Windows\system;Windows;`) | 직접 | ntdll | `②… +0x1161` | `[정적]` `_refs` WRITE · run39 · 정확 |
| `RtlpSystemDirs` (`0x1d2360`) | ② | 다시 씀 (뜻 모름, dll 올리기 뒤) | 직접 | ntdll | `②… → LdrpInitializePerUserWindowsDirectory +0x157` | `[정적]` `_refs` WRITE · run39 · 정확 |
| `LdrpDllDirectory` (`0x1d2370`) | — | DLL 폴더 API 가 씀 (이름으로 본 범위 밖 `[추측]`, 지도 경로 없음) | 직접 | ntdll | `LdrSetDllDirectory +0xcf` | `[정적]` `_refs` WRITE · 정확 |
| `LdrInitState` (`0x1d23c8`) | ② | 처음 값 (`EBX`) → 네이티브 길 끝에 3 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x101 · +0x205` | `[정적]` `_refs` WRITE · run17 · run19 · 정확 |
| `LdrInitState` (`0x1d23c8`) | ② | = 1 (병렬 켠 직후 `+0x1a4c`) · = 2 (실행 준비 뒤 `+0x1b1e`) · = 3 (WOW64 길만 `+0x17dd`) | 직접 | ntdll | `②… +0x1a4c · +0x1b1e · +0x17dd` | `[정적]` `_refs` WRITE · run19 · 정확 |
| `SecurityCookieInitialized` (`0x1d23ce`) | ② | = 1 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize → InitSecurityCookie +0x42` | `[정적]` `_refs` WRITE · run17 · 정확 |
| `SpecialMachineFramesInitialized` (`0x1d23d0`) | ② | = 1 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize → InitSpecialMachineFrames +0x57` | `[정적]` `_refs` WRITE · run17 · 정확 |
| `LdrpModuleBaseAddressIndex` (`0x1d2420`) | ② | 트리에 항목 넣기 (키 = `DllBase`) | 주소 넘김 (`RtlRbInsertNodeEx`) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0xe7` | `[정적]` run35 · 정확 |
| `LdrpMappingInfoIndex` (`0x1d2430`) | ② | 트리에 항목 넣기 (키 = TimeDateStamp, SizeOfImage) | 주소 넘김 (`RtlRbInsertNodeEx`) | ntdll | `②… → {ntdll · exe · DLL} → LdrpInsertModuleToIndexLockHeld +0x91` | `[정적]` run35 · 정확 |
| `LdrpTlsLock` (`0x1d2460`) | ② | TLS 락 잡고 풀기 (`CMPXCHG.LOCK` 6줄, `AND.LOCK` 1줄) | 직접 | ntdll | `②… → {LdrpInitializeGraphRecurse → LdrpInitializeNode (DLL) · 직접 (exe)} → LdrpCallTlsInitializers +0x6d · +0xae · +0x25a · +0x2c7 · +0x31e · +0x336 · +0x3bf` | `[정적]` `_refs` · run28 · 정확 |
| `LdrpTlsLock` (`0x1d2460`) | ? | `= 0x11` | 직접 | ntdll | `LdrpUnlockTlsDelayedReclaimTable +0x85 (지도 경로 모름)` | `[정적]` `_refs` WRITE · 정확 |
| `LdrpRetryingModuleIndex` (`0x1d25b0`) | ② | 16바이트 쓰기 (`MOVDQU`, 뜻 모름) | 직접 | ntdll | `②… → LdrpDrainWorkQueue +0x13b` | `[정적]` `_refs` WRITE · run24 · 정확 |
| `LdrpDetourExist` (`0x1d2610`) | ② | = 1 (조건은 이름으로 본 "detour 를 찾으면" `[추측]`) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpDetectDetour +0x7a · +0xac` | `[정적]` `_refs` WRITE · run21 · 정확 |
| `LdrpDetourExist` (`0x1d2610`) | ② | `R15B` 값 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x1d7` | `[정적]` `_refs` WRITE · 정확 |
| `LdrpDetourExist` (`0x1d2610`) | ? | = 1 | 직접 | ntdll | `LdrpCaptureCriticalThunks +0xa1 (지도 경로 모름)` | `[정적]` `_refs` WRITE · 정확 |
| `LdrpMainThreadToken` (`0x1d2618`) | ② | 메인 스레드 토큰 (`RCX`) | 직접 | ntdll | `{②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal · ②… (+0x1a8e)} → LdrpThreadTokenSetMainThreadToken +0x29` | `[정적]` `_refs` WRITE · run21 · 정확 |
| `LdrpMainThreadToken` (`0x1d2618`) | ② | = 0 | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpThreadTokenUnsetMainThreadToken +0x1f` | `[정적]` `_refs` WRITE · run21 · 정확 |
| `LdrpLoadCompleteEvent` (`0x1d2620`) | ② | 이벤트 (자동 리셋) 만들기 — 칸 주소를 넘김 (`+0x12` `LEA`) | 주소 넘김 | ntdll | `②… → LdrpInitParallelLoadingSupport → LdrpCreateLoaderEvents +0x12` | `[정적]` run19 · run24 · `_refs` DATA · 정확 |
| `LdrpRetryQueue` (`0x1d2630`) | ② | 빈 목록 머리 | 직접 | ntdll | `②… → LdrpInitParallelLoadingSupport +0x2c · +0x33` | `[정적]` `_refs` WRITE · 정확 |
| `LdrpRetryQueue` (`0x1d2630`) | ② | 비우기 (재시도 일감을 통째로 `LdrpWorkQueue` 로 옮긴 뒤) | 직접 | ntdll | `②… → LdrpDrainWorkQueue +0x12d · +0x134` | `[정적]` `_refs` WRITE · run24 · 정확 |
| `LdrpRetryQueue` (`0x1d2630`) | ② | 꼬리에 넣기 (재시도일 때 `[추측: 이름]`) | 직접 | ntdll | `… LdrpMapDllNtFileName → LdrpCheckForRetryLoading +0x182` | `[정적]` `_refs` WRITE · run26 · 정확 |
| `LdrpMapAndSnapWork` (`0x1d2640`) | ② | 일감 개체 (콜백 `LdrpWorkCallback`). 스레드 수 > 1 이고 `LdrpDetourExist == 0` 일 때만 | 주소 넘김 (`TpAllocWork`) | ntdll | `②… → LdrpEnableParallelLoading +0xe2` | `[정적]` run24 (flow.md 2.6) · 정확 · **flow-writer 더함** |
| `LdrpMapAndSnapWork` (`0x1d2640`) | ② | 지움 (flow.md 2.6 "지우는 곳", 조건은 안 봄) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpDetectDetour +0xd5` | `[정적]` `_refs` WRITE · run24 · 정확 |
| `LdrpMapAndSnapWork` (`0x1d2640`) | — | 지움 (`AND 0`, 복제 길 `[추측: 이름]`) | 직접 | ntdll | `LdrpCompleteProcessCloning +0x78` | `[정적]` `_refs` READ_WRITE · run24 · 정확 |
| `LdrpWorkCompleteEvent` (`0x1d2660`) | ② | 이벤트 (자동 리셋) 만들기 — 칸 주소를 넘김 (`+0x35` `LEA`) | 주소 넘김 | ntdll | `②… → LdrpInitParallelLoadingSupport → LdrpCreateLoaderEvents +0x35` | `[정적]` run19 · run24 · `_refs` DATA · 정확 |
| `LdrpWorkInProgress` (`0x1d2668`) | ② | 책임자 끝낼 때 = 1 (`+0xa3`·`+0x1af`), 일감 꺼낼 때 +1 (`+0x155`) | 직접 | ntdll | `②… → LdrpDrainWorkQueue +0xa3 · +0x155 · +0x1af` | `[정적]` `_refs` · run24 · 정확 |
| `LdrpWorkInProgress` (`0x1d2668`) | ② | 일감 끝에 −1 (인자 "센 적 없음" 0 일 때) | 직접 | ntdll | `②… → LdrpDrainWorkQueue → LdrpProcessWork +0x185` | `[정적]` `_refs` WRITE · run24 · 정확 |
| `LdrpWorkInProgress` (`0x1d2668`) | ② | 같은 일 (예외 정리 조각) | 직접 | ntdll | `②… → LdrpDrainWorkQueue → LdrpProcessWork$fin$0 +0x49` | `[정적]` `_refs` WRITE · 정확 |
| `LdrpWorkInProgress` (`0x1d2668`) | ② | = 0 (책임자 내려놓기) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal → LdrpDropLastInProgressCount +0x25` | `[정적]` `_refs` READ_WRITE · run24 · 정확 |
| `LdrpWorkInProgress` (`0x1d2668`) | ② 일꾼 | +1 (일감 꺼낼 때) | 직접 | ntdll | `(ntdll 일꾼 스레드) TppWorkpExecuteCallback +0x207` | `[정적]` `_refs` READ_WRITE · run24 · 정확 |
| `LdrpWorkInProgress` (`0x1d2668`) | ② 일꾼 | 더하기 (`ADD`). 이 함수는 실행 때 안 불릴 수 있음 (run24) | 직접 | ntdll | `(일꾼) TppWorkpExecuteCallback → LdrpWorkCallback +0x5e` | `[정적]` `_refs` READ_WRITE · run24 · 정확 |
| `LdrpWorkQueueLock` (`0x1d2680`) | ② | 잠금 (임계 구역) 만들기. 잡고 푸는 곳은 `LdrpQueueWork +0x1e`·`+0x59`, `LdrpDrainWorkQueue`·`LdrpWorkCallback` | 주소 넘김 | ntdll | `②… → LdrpInitParallelLoadingSupport` | `[정적]` run24 (flow.md 2.6) · 정확 · **flow-writer 더함** |
| `LdrpWorkQueueLock` (`0x1d2680`) | — | 칸 넷을 다시 씀 (복제 길 `[추측: 이름]`) | 직접 | ntdll | `LdrpCompleteProcessCloning +0x1f · +0x27 · +0x2e · +0x34` | `[정적]` `_refs` WRITE · 정확 |
| `LdrpWorkQueue` (`0x1d26b0`) | ② | 빈 목록 머리 | 직접 | ntdll | `②… → LdrpInitParallelLoadingSupport +0xe · +0x1c` | `[정적]` `_refs` WRITE · run24 · 정확 |
| `LdrpWorkQueue` (`0x1d26b0`) | ② | 꼬리에 일감 (로드 문맥 `+0x40`) 끼우기 | 직접 | ntdll | `… {LdrpLoadDependentModuleInternal · LdrpMapAndSnapDependency · LdrpSignalModuleMapped} → LdrpQueueWork +0x4d` | `[정적]` `_refs` WRITE · run24 · 정확 |
| `LdrpWorkQueue` (`0x1d26b0`) | ② | 머리에서 빼기 · 재시도 큐 통째로 옮기기 | 직접 | ntdll | `②… → LdrpDrainWorkQueue +0x7c · +0x115 · +0x126` | `[정적]` `_refs` WRITE · run24 · 정확 |
| `LdrpWorkQueue` (`0x1d26b0`) | ② 일꾼 | 머리에서 빼기 | 직접 | ntdll | `(일꾼) TppWorkpExecuteCallback +0x1f7` | `[정적]` `_refs` WRITE · run24 · 정확 |
| `LdrpWorkQueue` (`0x1d26b0`) | ② 일꾼 | 머리에서 빼기 | 직접 | ntdll | `(일꾼) TppWorkpExecuteCallback → LdrpWorkCallback +0x4e` | `[정적]` `_refs` WRITE · run24 · 정확 |
| `LdrpIsHotPatchingEnabled` (`0x1d26f0`) | ② | `PEB.ProcessParameters.Flags` `0x2000000` 비트와 `NtManageHotPatch` 결과로 정함 | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → LdrpInitializeHotPatching +0x55` | `[정적]` `_refs` WRITE · run17 · 정확 |
| `LdrpIsHotPatchingEnabled` (`0x1d26f0`) | ② | = 0 (`LdrpImageExpansionMitigation == 2` 일 때만) | 직접 | ntdll | `②… +0x587` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `FastPebLock` (`0x1d2700`) | ② | 임계 구역 만들기 (그 뒤 `+0x9e1` 이 PEB `+0x38` 에 연결) | 주소 넘김 (`RtlInitializeCriticalSectionEx`) | ntdll | `②… → RtlInitializeCriticalSectionEx (호출 +0x9cd)` | `[정적]` run18 · 정확 |
| `Kernel32ThreadInitThunkFunction` (`0x1d2760`) | ② | kernel32 에서 이름 `BaseThreadInitThunk` 로 찾은 주소. 직접 쓰는 줄은 없음 | 주소 넘김 (칸 주소 `+0xe4`) | ntdll | `②… → LdrpInitializeKernel32Functions → LdrGetProcedureAddressForCaller (호출 ret +0xff)` | `[정적]` run19 · run20 · `_refs` DATA · 정확 |
| `LdrpForkInProgress` (`0x1d2768`) | — | 켜고 끔 (프로세스 복제 길, sast12) | 직접 | ntdll | `RtlCloneUserProcess +0x169 · +0x218 · +0x293` | `[정적]` `_refs` WRITE · sast12 · 정확 |
| `LdrpForkInProgress` (`0x1d2768`) | — | = 0 (복제 길) | 직접 | ntdll | `RtlCompleteProcessCloning +0x58 · +0xd4` | `[정적]` `_refs` WRITE · 정확 |
| `LdrpForkInProgress` (`0x1d2768`) | — | = 1 (복제 길) | 직접 | ntdll | `RtlPrepareForProcessCloning +0x109` | `[정적]` `_refs` WRITE · sast12 · 정확 |
| `LdrpKnownDllPath` (`0x1d2770`) | ② | `\KnownDlls` 링크에서 읽은 경로 (`UNICODE_STRING` 칸 셋) | 직접 | ntdll | `②… +0xe64 · +0xe6b · +0xe72` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `LdrpLargePageDllKeyHandle` (`0x1d2780`) | ② | `R12` 값 (IFEO 쪽, 뜻 모름) | 직접 | ntdll | `②… → LdrpInitializeExecutionOptions +0x3d4` | `[정적]` `_refs` WRITE · 정확 |
| `LdrpLargePageDllKeyHandle` (`0x1d2780`) | ② | `R15` 값 (뜻 모름) | 직접 | ntdll | `②… +0x179e` | `[정적]` `_refs` WRITE · 정확 |
| `LdrpImageEntry` (`0x1d2828`) | ② | exe 모듈 항목 주소 (`refs` 46줄 중 쓰기는 여기 하나) | 직접 | ntdll | `②… +0x1374` | `[정적]` `_refs` WRITE · run19 · 정확 |
| `LdrpHeap` (`0x1d2860`) | ② | `R12` 값 (묶음 7 "로더 힙" — 이름으로 본 뜻 `[추측]`) | 직접 | ntdll | `②… +0xb12` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `UseWOW64` (`0x1d2868`) | ② | `TEB.WowTebOffset > 0` (x64 test.exe 면 0 `[추측]`) | 직접 | ntdll | `②… +0x5f2` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `PebLdr` (`0x1d2880`) | ② | 이 전역이 곧 `_PEB_LDR_DATA` 한 벌 — 쓰는 줄 20개는 위 `_PEB_LDR_DATA` 표와 같음 (종료 길 2줄 포함) | 직접 | ntdll | `_PEB_LDR_DATA` 표 | `[정적]` `_refs` WRITE · run18 · 정확 |
| `LdrpNtDllDataTableEntry` (`0x1d28d8`) | ② | ntdll 항목을 가리키는 포인터 (성공 길 끝에서만) | 직접 | ntdll | `②… → LdrpInitializeNtdllDataTableEntry +0x154` | `[정적]` run35 · 정확 |
| `LdrpIsSecureProcess` (`0x1d28e0`) | ② | = 1 (`ProcessParameters.Flags` 맨 윗 비트가 켜졌을 때) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize +0x5a` | `[정적]` `_refs` WRITE · run17 · 정확 |
| `LdrpAppHeaders` (`0x1d28f8`) | ② | exe NT 헤더 주소 | 직접 | ntdll | `②… +0x36b` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `LdrpNtdllHotPatchContext` (`0x1d2908`) | ② | 지역 칸 주소 (`+0xa7`) → `_LdrpInitialize` 에서 돌아오면 비움 (`+0x5e`) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal +0xa7 · +0x5e` | `[정적]` `_refs` · run17 · 정확 |
| `LdrpHotPatchInitialized` (`0x1d2928`) | ② | 0 → 1 (`CMPXCHG.LOCK`, 처음 온 스레드) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal +0x3b` | `[정적]` `_refs` READ_WRITE · run17 · 정확 |
| `LdrpInitCompleteEvent` (`0x1d2948`) | ② | 이벤트 핸들 — `NtCreateEvent(&LdrpInitCompleteEvent, …)` 가 칸에 받음 | syscall | nt | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → NtCreateEvent (ret +0xea)` | `[정적]` sast12 · run17 · 정확 |
| `LdrpProcessInitContextRecord` (`0x1d2950`) | ② | 첫 스레드 CONTEXT 레코드 (`+0x1b83`) → 0 (`+0x1bec`) | 직접 | ntdll | `②… +0x1b83 · +0x1bec` | `[정적]` `_refs` WRITE · run19 · 정확 |
| `LdrpSystemDllBase` (`0x1d2970`) | ② | ntdll 시작 주소 (`LdrInitializeThunk` 가 받은 `Rdx`) | 직접 | ntdll | `②… +0x68c` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `LdrpImageExpansionMitigation` (`0x1d2978`) | ② | IFEO `ImageExpansionMitigation` 값 | 직접 | ntdll | `②… → LdrpInitializeExecutionOptions +0x4d0` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `LdrpProcessInitialized` (`0x1d2988`) | ② | 0 → 1 (`+0x3a CMPXCHG.LOCK`, 첫 스레드 판정). `+0x290` 에도 `CMPXCHG.LOCK` 한 줄 (뜻은 안 봄) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x3a · +0x290` | `[정적]` `_refs` READ_WRITE · run17 · 정확 |
| `LdrpSystem32` (`0x1d2990`) | ② | 시스템 폴더 문자열 칸 | 직접 | ntdll | `②… +0x10a6 · +0x10b2` | `[정적]` `_refs` WRITE · run39 · 정확 |
| `LdrpCurrentDllInitializer` (`0x1d3048`) | ② | 지금 켜는 모듈 항목 (`+0x220` 에서 되돌림) | 직접 | ntdll | `②… → LdrpInitializeGraphRecurse → LdrpInitializeNode +0xbd · +0x220` | `[정적]` run27 · 정확 |
| `RtlpDllSearchPath` (`0x1d3050`) | ② | 프로세스 힙에 캐시 블록을 잡고 가리킴 (처음 DLL 검색 때, 쓰는 줄 오프셋은 문서에 없음) | 직접 | ntdll | `… LdrpSearchPath → LdrpComputeLazyDllPath → … RtlpGetCachedPath → RtlpComputePath` | `[추측]` run39 · 짐작 |
| `LdrpHashTable` (`0x1d30a0`) | ② | 해시 표 비우기 (묶음 3) | 직접 | ntdll | `②… +0x3c5 · +0x3c9` | `[정적]` `_refs` WRITE · run18 · 정확 |
| `LdrpSaferIsDllAllowedRoutine` (`0x1d32a0`) | ② | 함수 주소 (레지스트리 값이 있을 때만) | 직접 | ntdll | `②… → LdrpInitializeKernel32Functions → LdrpCodeAuthzInitialize +0x1a8` | `[정적]` run20 · 정확 |

### 3.9 ntdll `.mrdata` 전역 (유저, rva `0x1e7000`, 크기 `0x35b8`)

- 구역: 쓰기 가능 — 실행 중엔 ntdll 이 필요할 때만 열고 닫음 (run18)
- 전역 50 · 쓴 자리 아는 것 6 (정확 3 · 짐작 3) · 모름 44 · 줄 6. 크기는 전부 `[추측]` (다음 이름까지 거리, 결과서 2번)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `AvrfAppVerifierMode` (`0x1e7044`) | ② (AppVerifier 만) | `R14D` 값. 이 값이 0 이면 `LdrpInitializeProcess +0x1568` 이 AppVerifier 묶음을 건너뜀 | 직접 | ntdll | `AVrfInitializeVerifier +0x414 (부르는 자리 확인은 Q78)` | `[정적]` `_refs` WRITE · run19 · 정확 |
| `g_pfnSE_InstallAfterInit` (`0x1e70c0`) | ② (shim 만 `[추측]`) | shim 엔진 함수 값 (읽는 쪽 `+0x1cb0 ~ +0x1cd1` 이 KUSD 쿠키로 풂) | 직접 | ntdll | `LdrpGetShimEngineInterface +0x4d9 (지도 경로 모름)` | `[정적]` `_refs` WRITE · run19 · 정확 |
| `g_LdrpWow64LdrpInitialize` (`0x1e70c8`) | ② (WOW64 만) | 칸 주소를 넘김 (파일 처음 값 0). 함수 밖 데이터 참조 1줄도 있음 | 주소 넘김 | ntdll | `②… → LdrpLoadWow64 +0xe8` | `[추측]` run17 · `_refs` DATA · 짐작 |
| `LdrpKnownDllDirectoryHandle` (`0x1e7120`) | ② | `\KnownDlls` 디렉터리 핸들 칸 — 칸 주소를 실음 (`LEA`). 바로 뒤 `+0xdb8` 이 `NtOpenDirectoryObject` (출력 칸일 것 `[추측]`) | 주소 넘김 (→ syscall `[추측]`) | ntdll | `②… +0xdb1` | `[추측]` `_refs` DATA · run18 · 짐작 |
| `LdrSystemDllInitBlock` (`0x1ea450`) | ① | 커널이 새 프로세스 안 ntdll 의 이 블록 (0x128) 에 직접 씀: `+0x98` 난수, `+0xa0` 24B 완화 옵션, `+0xb8` CFG 비트맵 주소, `+0xf0 ~` SCP 표 등. 블록 타입은 두 PDB 에 다 없음 (run43) | 직접 | nt | `①… → PspAllocateProcess → PspSetupUserProcessAddressSpace → PspPrepareSystemDllInitBlock (호출 +0x1e6)` | `[정적]` run32 · run43 · 정확 |
| `__security_cookie` (`0x1ea5a8`) | ② | 쿠키 값 — 이름으로 본 것 (문서는 호출만 적음) | 직접 | ntdll | `LdrInitializeThunk → LdrpInitialize → InitSecurityCookie (호출 +0x41)` | `[추측]` run17 · sast12 · 짐작 |

### 3.10 ntdll `.00cfg` 칸 (유저, rva `0x1e6000`, 크기 `0x28`)

- 구역: PE 머리엔 읽기 전용. 로더가 값을 넣는 CFG 함수 포인터 칸 5개 (결과서 2번)
- 전역 5 · 쓴 자리 아는 것 1 (정확 0 · 짐작 1) · 모름 4 · 줄 1. 크기는 전부 `[추측]` (다음 이름까지 거리, 결과서 2번)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `__guard_dispatch_icall_fptr` (`0x1e6000`) | — | PE 로드 구성 `GuardCFDispatchFunctionPointer` 가 가리키는 칸. 실행 중 쓰는 코드 줄 없음 (`_refs` 는 함수 밖 데이터 참조 3줄: `entry+0x3ac`·`_load_config_used+0x78`·`RtlpScpCfgNtdllExports+0x48`) | 입력 (파일 값) `[추측]` | 모름 | — | `[추측]` run28 · `_refs` DATA · 짐작 |

**커널 공간**

### 3.11 `_EPROCESS` — 프로세스 객체 (커널, `0x840`, ntoskrnl PDB)

- 칸 263 · 아는 칸 22 (정확 17 · 짐작 5) · 모름 241 · 줄 17

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `Pcb` | ① | 커널 프로세스 구조 초기화 (`KeInitializeProcess` 호출 — 칸마다 줄은 안 봄) | 주소 넘김 | nt | `①… → PspAllocateProcess → KeInitializeProcess (호출 +0x171e)` | `[추측]` sast04 · run10 · 짐작 |
| `+0x1c8` `ProcessLock` | ① | 잡기 — 호출자가 잡아서 `PspInsertThread` 로 들고 감 (flow.md `[정적 + 간접]`) | 주소 넘김 | nt | `①… → PspAllocateProcess → PspLockProcessExclusive (+0x26e9)` | `[추측]` run12 · 짐작 |
| `+0x1c8` `ProcessLock` | ① | 풀기 | 직접 | nt | `①… → PspInsertThread +0x2a2` | `[정적]` run12 · 정확 |
| `+0x1d0` `UniqueProcessId` | ① | `PspCidTable` 에 만든 핸들 값 (`ExCreateHandleEx` ret `+0x2703` 뒤). "이게 프로세스 ID" 는 문서 `[추측]` | 직접 | nt | `①… → PspAllocateProcess +0x27e4` | `[정적]` sast04 · run10 (저장 줄 `.asm` 확인) · 정확 |
| `+0x1d8` `ActiveProcessLinks` | ① | 시스템 프로세스 목록 맨 뒤에 잇기 (목록 잠금 안, 호출 없이) | 직접 | nt | `①… → PspInsertProcess +0xa6 ~ +0x109` | `[정적]` run11 · 정확 |
| `+0x1f8` `CreateTime` | ① | 생성 시각 (`KeQuerySystemTimePrecise`. "생성 시각" 은 문서 `[추측]`) | 주소 넘김 (칸 주소 `+0x27bd`) · 다른 갈래 `+0x27dd` 직접 | nt | `①… → PspAllocateProcess → KeQuerySystemTimePrecise (호출 +0x27c4)` | `[정적]` sast04 (`.asm` 확인) · 정확 |
| `+0x248` `Token` | ① | 토큰 붙이기 (`PspInitializeProcessSecurity` 호출 — "토큰을 붙임" 은 문서 `[추측]`) | 주소 넘김 | nt | `①… → PspAllocateProcess → PspInitializeProcessSecurity (호출 +0x1927)` | `[추측]` sast04 · 짐작 |
| `+0x2b0` `SectionBaseAddress` | ① | exe 를 붙인 주소 (`+0xb8 MmMapViewOfSectionEx` 결과) | 직접 | nt | `①… → PspAllocateProcess → MmInitializeProcessAddressSpace → MiMapProcessExecutable +0xc5` | `[정적]` run10 · 정확 |
| `+0x2d8` `OwnerProcessId` | ① | 부모 PID (ProcessFlags 비트 9 가 꺼졌으면 `OR 2`) | 직접 | nt | `①… → PspAllocateProcess +0x710` | `[정적]` run33 · run34 · 정확 |
| `+0x2e0` `Peb` | ① | 새 PEB 주소 (출력 칸) | 주소 넘김 (칸 주소 `+0x22f2`) | nt | `①… → PspAllocateProcess → MmCreatePeb` | `[정적]` run10 · 정확 |
| `+0x300` `ObjectTable` | ① | 핸들 표 준비 (`ObInitProcess` 호출 — "핸들 표 준비" 는 문서 `[추측]`) | 주소 넘김 | nt | `①… → PspAllocateProcess → ObInitProcess (호출 +0x1991)` | `[추측]` sast04 · run10 · 짐작 |
| `+0x370` `ThreadListHead` | ① | 프로세스 스레드 목록 꼬리에 새 스레드 달기 (`+0x5e0` 푸시락 안, 호출 없음). 문서는 목록 머리 오프셋을 안 적음 | 직접 | nt | `①… → PspInsertThread +0x6c0 ~ +0x266` | `[추측]` run12 · 짐작 |
| `+0x380` `ActiveThreads` | ① | 스레드 수 +1 (오프셋 미상) | 직접 | nt | `①… → PspInsertThread` | `[정적]` run12 · 정확 |
| `+0x5fc` `Flags3` — 0 `Minimal` · 5 `AuxiliaryProcess` · 7 `IndirectCpuSets` · 12 `SystemProcess` · 23 `VmProcessorHost` · 27 `DisallowUserTerminate` · 30 `MemoryCompressionProcess` | ① | `Flags3` 에 쓰는 비트 0·5·7·12·23·27·30 (문서는 비트 번호만, 이름은 PDB 로 맞춤. 오프셋 미상) | 직접 | nt | `①… → PspAllocateProcess` | `[정적]` run33 · 정확 |
| `+0x5fc` `ProcessFirstResume` (`Flags3` 비트 15) | ① | 첫 풀어줌 표시 켜기 (켜는 곳은 이 한 줄뿐, 끄는 곳 없음) | 직접 | nt | `(부모, 다음 시스템 콜) NtResumeThread → PsMultiResumeThread +0x4e` | `[정적]` run13 · run33 · 정확 |
| `+0x678` `SequenceNumber` | ① | 전역 순번 +1 을 적음 | 직접 | nt | `①… → PspInsertProcess +0xa6 ~ +0x109` | `[정적]` run11 · 정확 |
| `+0x7dc` `ThreadWasActive` (`Flags4` 비트 0) | ① | 첫 스레드 판정 비트 ("이번에 처음 켜지면"). 켜는 줄 오프셋은 문서에 없음, 비트 이름은 PDB 로 맞춤 | 직접 | nt | `①… → PspInsertThread` | `[추측]` run12 · 짐작 |

### 3.12 `_KPROCESS` — 프로세스 객체 앞부분 (`Pcb`) (커널, `0x1c8`, ntoskrnl PDB)

- 칸 65 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 65 · 줄 0
- 초기화 (구조체 단위): `PspAllocateProcess +0x171e KeInitializeProcess` (sast04 · run10). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.13 `_ETHREAD` — 스레드 객체 (메인 스레드) (커널, `0x798`, ntoskrnl PDB)

- 칸 149 · 아는 칸 5 (정확 5 · 짐작 0) · 모름 144 · 줄 6

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x4e0` `StartAddress` | ① | `CONTEXT.Rip` 를 옮겨 적음 (= `RtlUserThreadStart`, run16) | 직접 | nt | `①… → PspAllocateThread +0x640 ~ +0x647` | `[정적]` run11 · sast06 · 정확 |
| `+0x508` `Cid` | ① | 스레드 ID 칸 | 주소 넘김 | nt | `①… → PspAllocateThread → PsAssignThreadId (호출 +0x532)` | `[정적]` sast06 · run11 · 정확 |
| `+0x560` `Win32StartAddress` | ① | `CONTEXT.Rcx` 를 옮겨 적음 (= exe 진입점) | 직접 | nt | `①… → PspAllocateThread +0x64e ~ +0x655` | `[정적]` run11 · sast06 · 정확 |
| `+0x590` `ThreadLock` | ① | 잡기 | 직접 | nt | `①… → PspAllocateThread +0x51c` | `[정적]` run11 · 정확 |
| `+0x590` `ThreadLock` | ① | 풀기 (`XADD`, 이어서 `KeAbPostRelease +0x4b2`) | 직접 | nt | `①… → PspInsertThread +0x499` | `[정적]` run12 · 정확 |
| `+0x5a0` `ThreadInserted` (`CrossThreadFlags` 비트 1) | ① | 켬 (`0x2`) | 직접 | nt | `①… → PspInsertThread +0x490` | `[정적]` run12 · 정확 |

### 3.14 `_KTHREAD` — 스레드 객체 앞부분 (`Tcb`) (커널, `0x4c0`, ntoskrnl PDB)

- 칸 237 · 아는 칸 10 (정확 9 · 짐작 1) · 모름 227 · 줄 14

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x28` `InitialStack` | ① | 새 커널 스택 | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread +0x2ca` | `[정적]` run15 · 정확 |
| `+0x58` `KernelStack` | ① | 첫 스택 자리 (맨 위에 `KiStartUserThread` 주소) | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread +0x2ce` | `[정적]` run15 · 정확 |
| `+0x74` `ApcQueueable` (`MiscFlags` 비트 14) | ① | 기본으로 켬 (`BTS`) | 직접 | nt | `①… → PspAllocateThread → KeInitThread +0xed` | `[정적]` run13 · 정확 |
| `+0x74` `SuspendSchedulerApcWait` (`MiscFlags` 비트 19) | ① | 멈춤 APC 안에서 기다리는 동안 켬 (`+0xf2`) → 깨면 끔 (`+0xf7`) | 직접 | nt | `①T… (APC 인터럽트) KiApcInterrupt → KiDeliverApc → KiSchedulerApc +0xf2 · +0xf7` | `[정적]` run13 · 정확 |
| `+0x98` `ApcState` | ① | 안쪽 `KernelApcPending` = 1 (오프셋 미상, 새 스레드 갈래 `[추측]`) | 직접 | nt | `①… → PspInsertThread → KeSuspendThread → KiSuspendThread → KiSignalThreadForApc` | `[정적]` run13 · 정확 |
| `+0x98` `ApcState` | ② 끝 | 안쪽 `UserApcPending` 을 켤 수 있음 (유저 APC 큐가 비어 있지 않을 때) | 직접 | nt | `LdrInitializeThunk → NtContinue (SYSCALL 0x43) nt!NtContinue ⇒ NtContinueEx → KiContinueEx → KeTestAlertThread (호출 +0xc9)` | `[정적]` run29 · 정확 |
| `+0xf0` `Teb` | ① | `MmCreateTeb` 결과를 `KeInitThread` 인자로 넘김 (칸 이름 `Teb` 는 문서 `[추측]`) | 직접 | nt | `①… → PspAllocateThread → KeInitThread` | `[추측]` run11 · 짐작 |
| `+0x1e6` `SpecialApcDisable` | ① | −1 (특수 APC 막기 시작) → `+0x2de` 에서 되돌림 | 직접 | nt | `①T… → PspInitializeThunkContext +0x113 · +0x2de` | `[정적]` run16 · 정확 |
| `+0x284` `SuspendCount` | ① | +1 | 직접 | nt | `①… → PspInsertThread → KeSuspendThread +0x60` | `[정적]` run13 · 정확 |
| `+0x284` `SuspendCount` | ① | −1 (0 이 될 때만 아래 `KiResumeThread`) | 직접 | nt | `(부모) NtResumeThread → PsMultiResumeThread → KeResumeThread +0x68` | `[정적]` run13 · 정확 |
| `+0x288` `SchedulerApc` | ① | `KeInitializeApc` — NormalRoutine = `KiSchedulerApc`, 커널 모드 | 주소 넘김 | nt | `①… → PspAllocateThread → KeInitThread → KeInitializeApc (호출 +0x152)` | `[정적]` run13 · 정확 |
| `+0x2e0` `SuspendEvent` | ① | `KeInitializeEvent` — 처음 신호 상태 (1) | 주소 넘김 | nt | `①… → PspAllocateThread → KeInitThread → KeInitializeEvent (호출 +0x163)` | `[정적]` run13 · 정확 |
| `+0x2e0` `SuspendEvent` | ① | `SignalState` 1 → 0 (멈춰 두기) | 직접 | nt | `①… → PspInsertThread → KeSuspendThread → KiSuspendThread +0x6c` | `[정적]` run13 · 정확 |
| `+0x2e0` `SuspendEvent` | ① | `SignalState` = 1 (풀어 주기) | 직접 | nt | `(부모) NtResumeThread → PsMultiResumeThread → KeResumeThread → KiResumeThread +0x20` | `[정적]` run13 · 정확 |

### 3.15 `_KTRAP_FRAME` — 트랩 프레임 (메인 스레드, `InitialStack − 0x190`) (커널, `0x190`, ntoskrnl PDB)

- 칸 66 · 아는 칸 6 (정확 4 · 짐작 2) · 모름 60 · 줄 11

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x38` `Rcx` | ① | 처음 = exe 진입점 | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread → KxContextToKframes +0xe8` | `[정적]` run16 · 정확 |
| `+0x38` `Rcx` | ① | 유저 스택에 맡긴 원래 문맥의 주소 | 직접 | nt | `①T… → PspInitializeThunkContext → PspSetContextThreadInternal → PspGetSetContextSpecialApc → PspGetSetContextInternal → PspSetContext +0xe3` | `[정적]` run16 · 정확 |
| `+0x40` `Rdx` | ① | 처음 = PEB | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread → KxContextToKframes +0xf3` | `[정적]` run16 · 정확 |
| `+0x40` `Rdx` | ① | `PspSystemDlls[0]+0x20` 값 (ntdll 시작 주소로 쓰임, run18) | 직접 | nt | `①T… → PspInitializeThunkContext → PspSetContextThreadInternal → PspGetSetContextSpecialApc → PspGetSetContextInternal → PspSetContext +0xee` | `[정적]` run16 · 정확 |
| `+0x168` `Rip` | ① | 처음 = `RtlUserThreadStart` (CONTEXT 에서) | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread → KxContextToKframes +0x6d` | `[정적]` run16 · 정확 |
| `+0x168` `Rip` | ① | `LdrInitializeThunk` 로 바꿔 끼움 (`PsNtdllExports+0x8`) | 직접 | nt | `①T… → PspInitializeThunkContext → PspSetContextThreadInternal → PspGetSetContextSpecialApc → PspGetSetContextInternal → PspSetContext +0x68` | `[정적]` run16 · 정확 |
| `+0x168` `Rip` · `+0x180` `Rsp` · `+0x38` `Rcx` · `+0x40` `Rdx` | ② 끝 | 맡긴 문맥으로 다시 씀 — `Rip` 가 `RtlUserThreadStart` 로 바뀌는 순간 (`Rcx` = 진입점, `Rdx` = PEB) | 주소 넘김 (→ `KeContextToKframes → KxContextToKframes`) | nt | `LdrInitializeThunk → NtContinue (SYSCALL 0x43) nt!NtContinue ⇒ NtContinueEx → KiContinueEx → KiContinuePreviousModeUser +0x422` | `[정적]` run29 · sast24 · 정확 |
| `+0x170` `SegCs` | ① | `0x33` — 로컬 CONTEXT 사본에서 손본 뒤 넘김 (옮기는 칸 목록엔 안 나옴) | 주소 넘김 (→ `KxContextToKframes`) | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread +0xb5` | `[추측]` run15 · run16 · 짐작 |
| `+0x180` `Rsp` | ① | 처음 = CONTEXT `Rsp` (`PspSetupUserStack` 이 정한 값) | 직접 | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread → KxContextToKframes +0x7b` | `[정적]` run16 · 정확 |
| `+0x180` `Rsp` | ① | 맡긴 문맥 자리 − 0x28 | 직접 | nt | `①T… → PspInitializeThunkContext → PspSetContextThreadInternal → PspGetSetContextSpecialApc → PspGetSetContextInternal → PspSetContext +0x76` | `[정적]` run16 · 정확 |
| `+0x188` `SegSs` | ① | `0x2b` — 같은 사본 | 주소 넘김 (→ `KxContextToKframes`) | nt | `①… → PspAllocateThread → KeInitThread → KiInitializeContextThread +0xb5` | `[추측]` run15 · run16 · 짐작 |

### 3.16 `_HANDLE_TABLE` — 핸들 표 (커널, `0x80`, ntoskrnl PDB)

- 칸 17 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 17 · 줄 0
- 만드는 곳 (구조체 단위, 이름으로 본 `[추측]`): `PspAllocateProcess +0x1991 ObInitProcess` (run10 · sast04). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.17 `_MMVAD` — 메모리 구역 장부 (긴 꼴) (커널, `0x88`, ntoskrnl PDB)

- 칸 9 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 9 · 줄 0
- 만드는 곳 (구조체 단위): `MmInitializeProcessAddressSpace +0x161 MiAllocateProcessVads`, `+0x1a8 MiInsertProcessVads` (run10 · sast04). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.18 `_MMVAD_SHORT` — 메모리 구역 장부 (짧은 꼴) (커널, `0x40`, ntoskrnl PDB)

- 칸 14 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 14 · 줄 0
- `_MMVAD` 와 같음 (`_MMVAD` 의 앞부분 격). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.19 `_TOKEN` — 토큰 (커널, `0x5c8`, ntoskrnl PDB)

- 칸 55 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 55 · 줄 0
- 붙이는 곳 (구조체 단위, `[추측]`): `PspAllocateProcess +0x1927 PspInitializeProcessSecurity` (sast04), `PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange` (run15). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.20 `_SECTION` — exe 섹션 객체 (커널, `0x40`, ntoskrnl PDB)

- 칸 9 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 9 · 줄 0
- 만드는 길 (구조체 단위): `NtCreateUserProcess +0x93c MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap` (run31). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

### 3.21 `_CONTROL_AREA` — exe 컨트롤 영역 (커널, `0x80`, ntoskrnl PDB)

- 칸 17 · 아는 칸 2 (정확 1 · 짐작 1) · 모름 15 · 줄 2
- 만드는 곳 (구조체 단위): `MiBuildImageControlArea` (컨트롤 영역 + 서브섹션, 풀) (run31 · sast26). 같은 파일이면 공유 길 `MiShareExistingControlArea` (`MiCreateImageOrDataSection +0x1d5`).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x30` `NumberOfUserReferences` | ① | +1 (`INC`) — 같은 파일 컨트롤 영역이 이미 있을 때 (공유 길) | 직접 | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection +0x11d` | `[정적]` sast26 · run31 · 정확 |
| `+0x38` `u` | ① | 자리표시용 컨트롤 영역 (스택 사본) 에 `BeingCreated` (+0x38 비트 1) 켬 — 진짜 컨트롤 영역 칸은 아님 | 직접 | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection` | `[추측]` sast26 · run31 · 짐작 |

### 3.22 `_SEGMENT` — exe 세그먼트 (커널, `0x48`, ntoskrnl PDB)

- 칸 11 · 아는 칸 1 (정확 1 · 짐작 0) · 모름 10 · 줄 1
- 만드는 곳 (구조체 단위): `MiBuildImageControlArea` (세그먼트 0x98바이트, 풀) (run31 · sast26).

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x38` `u2` | ① | `u2.ImageInformation` = 세그먼트 `+0x48` 쪽 이미지 정보 주소 | 직접 | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea +0x13e` | `[정적]` sast26 · run31 · 정확 |

### 3.23 `_SECTION_IMAGE_INFORMATION` — exe 이미지 정보 (커널, `0x40`, ntoskrnl PDB)

- 칸 26 · 아는 칸 16 (정확 16 · 짐작 0) · 모름 10 · 줄 17

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `TransferAddress` | ① | `ImageBase + AddressOfEntryPoint` (처음 만드는 줄) | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea +0x16b ~ +0x172` | `[정적]` run31 · sast26 · 정확 |
| `+0x0` `TransferAddress` | ① | 새 기본 주소면 델타를 더함 (조건부) | 직접 | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiRelocateImage (조각 FUN_140b24658 +0x5d ~ +0x65 = nt+0x1b1279)` | `[정적]` run31 · sast26 · 정확 |
| `+0x10` `MaximumStackSize` | ① | = `SizeOfStackReserve` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x18` `CommittedStackSize` | ① | = `SizeOfStackCommit` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x20` `SubSystemType` | ① | = `Subsystem` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x24` `SubSystemMinorVersion` | ① | = `MinorSubsystemVersion` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x26` `SubSystemMajorVersion` | ① | = `MajorSubsystemVersion` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x28` `MajorOperatingSystemVersion` | ① | = `MajorOperatingSystemVersion` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x2a` `MinorOperatingSystemVersion` | ① | = `MinorOperatingSystemVersion` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x2c` `ImageCharacteristics` | ① | = 파일 헤더 `Characteristics` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x2e` `DllCharacteristics` | ① | = 선택 헤더 `DllCharacteristics` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x30` `Machine` | ① | = 파일 헤더 `Machine` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x32` `ImageContainsCode` | ① | = `SizeOfCode ≠ 0` 또는 진입점 ≠ 0 이면 1 | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x33` `ImageDynamicallyRelocated` (`ImageFlags` 비트 2) | ① | 켬 (`OR byte [R15+0x33],4`) | 직접 | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiRelocateImage (조각 FUN_140b24658, nt+0x1b1357)` | `[정적]` run31 · sast26 · 정확 |
| `+0x34` `LoaderFlags` | ① | = 선택 헤더 `LoaderFlags` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x38` `ImageFileSize` | ① | = 파일 크기 하위 32비트 | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |
| `+0x3c` `CheckSum` | ① | = 선택 헤더 `CheckSum` | 직접 (값은 파일 헤더 = 입력) | nt | `①… → MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea (오프셋 미상)` | `[정적]` sast26 · run31 · 정확 |

### 3.24 `_FILE_OBJECT` — exe 파일 객체 (커널, `0xd8`, ntoskrnl PDB)

- 칸 30 · 아는 칸 0 (정확 0 · 짐작 0) · 모름 30 · 줄 0
- 만드는 곳 (구조체 단위): `NtCreateUserProcess +0x6cb IopCreateFile` (exe 열기, run9). 칸 단위 근거 없음.
- **쓴 자리를 아는 칸 없음.** 칸 이름은 7장.

## 4. 두 공간 짝

| 유저 쪽 | 커널 쪽 | 무엇이 맞아야 하나 | 근거 |
|---|---|---|---|
| PEB 자체 (`_PEB`) | `_EPROCESS.Peb` (+0x2e0) | 같은 주소 — `MmCreatePeb` 가 출력 칸에 씀 | `[정적]` run10 |
| `_PEB.ImageBaseAddress` (+0x10) = 붙인 exe 이미지 | `_EPROCESS.SectionBaseAddress` (+0x2b0), exe 섹션 (`SectionObject` +0x2a8 → `_SECTION` → `_CONTROL_AREA` → `_SEGMENT`), VAD 구역 | 같은 값 (`MiMapProcessExecutable +0xc5` → `MmCreatePeb` 가 옮김). 붙인 구역이 VAD 에도 있을 것 `[추측]` | `[정적]` run10 · 결과서 Q77 표 |
| exe 진입점: 맡긴 문맥 `Rcx`, `_ETHREAD.Win32StartAddress` | `_SECTION_IMAGE_INFORMATION.TransferAddress` | `ImageBase + AddressOfEntryPoint` (다른 주소면 + 델타). 같은 값이 `PspCreateUserContext` → CONTEXT → 트랩 프레임 → 맡긴 문맥으로 흐른다 (flow.md 2.1 "진입점 값이 흘러가는 길") | `[정적]` run31 · run16 · run11 |
| TEB (`_TEB`) | `_KTHREAD.Teb` (+0xf0) | 같은 주소 (`MmCreateTeb` 결과 → `KeInitThread`). 칸 이름은 `[추측]` | `[추측]` run11 |
| 매개변수 블록 (`PEB.ProcessParameters` +0x20 이 가리킴) | 커널 사본 (만들기 정보 `+0xd8`, `NtCreateUserProcess +0xacb PspCaptureProcessParameters`) | 내용 = 커널 사본 (붙기 전 `Flags` 비트 15·29·`LoaderThreads` 를 손본 것). 안쪽 포인터 12개는 새 주소 기준으로 고친 값 | `[정적]` run32 |
| 매개변수 `ConsoleHandle`·`StandardInput`·`StandardOutput`·`StandardError` | 새 프로세스 핸들 표 (`_HANDLE_TABLE`) | 핸들 값 ↔ 핸들 표 항목 (`ObDuplicateObject` 로 부모 것을 복제). 핸들 표 쪽 칸은 `[공백]` | `[정적]` run32 |
| ntdll 전역 `LdrSystemDllInitBlock` (`.mrdata` rva `0x1ea450`, 0x128) | `PsNtdllExports.LdrSystemDllInitBlock` (+0x0, 부팅 때 채운 블록 주소), 새 프로세스 작업 집합 구조 `+0x4a0` (블록 `+0xb8` 출처), `PspApplyMitigationOptions` 에 넘긴 24바이트 (블록 `+0xa0`) | 블록 주소 = 이 프로세스 안 ntdll 전역. `+0xb8` = CFG 비트맵 주소, `+0xa0` = 완화 옵션 (칸 이름 `[추측]`) | `[정적]` run32 · run16 |
| ntdll 시작 주소 (`LdrpSystemDllBase`, `LdrInitializeThunk` 가 받은 `Rdx`) | `PspSystemDlls[0]` 칸 `+0x20` (부팅 때 정한 주소) | 같은 값. `PspMapSystemDll +0x172` 가 그 주소에 붙었는지 검사한다 | `[정적]` run16 · run18 |
| ntdll 함수 `LdrInitializeThunk`·`RtlUserThreadStart` | `PsNtdllExports` (`+0x8`·`+0x10`, `_PS_NTDLL_EXPORTS` 0xa8) | 표 값 = 이 프로세스 안 그 함수 주소 (ntdll 이 부팅 때 정한 주소에 붙으니 같을 것 `[추측]`) | `[정적]` run16 |
| 유저 스택 위 맡긴 문맥 (`_CONTEXT`, `LdrInitializeThunk` 의 `Rcx`) | `_KTRAP_FRAME` (`Rip` +0x168 · `Rsp` +0x180 · `Rcx` +0x38 · `Rdx` +0x40) | ① 끝: 트랩 `Rip` = `LdrInitializeThunk`, 맡긴 문맥 `Rip` = `RtlUserThreadStart`·`Rcx` = 진입점·`Rdx` = PEB, `ContextFlags` = `0x10001b` (CET 면 `0x10005b`). ② 끝 (`NtContinue`): 맡긴 문맥을 트랩 프레임에 다시 씀 | `[정적]` run16 · run29 · run42 |
| `_TEB.CurrentLocale` (+0x108) | 세션 로케일 (`PsGetSessionLocaleId`) | 같은 값 | `[정적]` run15 |
| `_TEB.SchedulerSharedDataSlot` (+0x1850) | `NtSetInformationThread` 클래스 0x39 → `PspSchedulerSharedDataRegionSlotAllocate` | 커널이 내준 표 주소 (조건이 안 맞으면 0) | `[정적]` run41 |
| 이벤트 핸들 전역 (`LdrpInitCompleteEvent`, `LdrpLoadCompleteEvent`, `LdrpWorkCompleteEvent`) | 핸들 표 항목 → 이벤트 객체 | 핸들 값 ↔ 핸들 표. 이벤트 객체 쪽 칸은 `[공백]` | `[정적]` run17 · run24 |
| `LdrpKnownDllDirectoryHandle` (`.mrdata`) | 핸들 표 항목 → `\KnownDlls` 디렉터리 객체 | `NtOpenDirectoryObject` 의 출력 칸일 것 `[추측]` | `[추측]` run18 · `_refs` DATA |
| DLL 항목 `DllBase` (kernel32·kernelbase·dll1·dll2) | VAD 구역 + DLL 섹션 (`NtMapViewOfSection`) | 붙인 주소 — 커널이 칸에 직접 쓴다. VAD 구역 시작과 같을 것 `[추측]` | `[정적]` run38 |
| `PEB.ApiSetMap` (+0x68) | `MmMapApiSetView` 가 올린 뷰 (`PspSetupUserProcessAddressSpace +0x255`) | 뷰 주소 `[추측]` — 이 칸을 쓰는 줄은 `[공백]` | `[정적]` run32 (뷰 올리기) · 결과서 Q77 표 |
| `PEB.LeapSecondData` (+0x7b8) | leap second 데이터 섹션 뷰 (`MmCreatePeb` 안 `MmMapViewOfSection +0x132`) | 뷰 주소 | `[정적]` run10 |
| `_KUSER_SHARED_DATA` (`0x7ffe0000`) | 커널 쪽 별칭 주소 `[공백]` (이 문서군에 없음) | 같은 물리 페이지 `[추측]` | 결과서 Q77 표 |

## 5. 단계 순서

- 짐이 놓이는 순서를 **코드 순서** (flow.md 트리 · 오프셋) 로 적었다 `[정적]`. 갈림길·고리·일꾼 스레드 때문에 실행 순서는 다를 수 있다 (flow.md 2.1 주의). 실행 순서는 사진 (Q77) 과 동적에서.
- 괄호 안 오프셋은 그 함수 안 쓰는 줄 또는 부르는 자리. 칸 이름 뒤 "(짐작)" 은 3장에서 짐작인 줄.

### ① 커널 단계

1. **부모 cmd.exe 스레드 — `NtCreateUserProcess`** (2.1)
   1. `+0x93c MmCreateSpecialImageSection` 아래 exe 섹션: 같은 파일 컨트롤 영역이 있으면 `MiCreateImageOrDataSection +0x11d` `NumberOfUserReferences` +1 (공유 길) / 없으면 `MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea` — `_SEGMENT.u2` (`+0x13e`), `TransferAddress` (`+0x16b ~ +0x172`) 와 헤더 값 칸 14개 → `MiRelocateImage` (다른 주소면 `TransferAddress` += 델타, `ImageDynamicallyRelocated`).
   2. `+0xc36 PspAllocateProcess` — `OwnerProcessId` (`+0x710`) → `Pcb` (`+0x171e`, 짐작) → `Token` (`+0x1927`, 짐작) → `ObjectTable` (`+0x1991`, 짐작) → `Flags3` 비트 7개 (오프셋 미상) → `+0x2095 MmInitializeProcessAddressSpace → MiMapProcessExecutable` `SectionBaseAddress` (`+0xc5`) → `+0x2314 MmCreatePeb` `Peb` 칸 + PEB 22칸 → `+0x239c PspSetupUserProcessAddressSpace`: 매개변수 커널 사본 `Flags` 비트 15·29·`LoaderThreads` → (새 프로세스에 붙기) → `PspCopyAndFixupParameters`: 블록 통째 복사 → Fixup·핸들 복제·`Flags` 비트 14 → PEB `ProcessParameters` → `PspPrepareSystemDllInitBlock` ntdll `LdrSystemDllInitBlock` → (`+0x26e9` 프로세스 락, 짐작) → `CreateTime` (`+0x27c4`) → `UniqueProcessId` (`+0x27e4`).
   3. `+0xe5c PspAllocateThread` — `ThreadLock` 잡기 (`+0x51c`) → `Cid` (`+0x532`) → (`+0x5ef PspSetupUserStack` 유저 스택) → `+0x631 MmCreateTeb` (TEB, 칸별 `[공백]`) → `StartAddress`·`Win32StartAddress` (`+0x647`·`+0x655`) → `+0x6f9 KeInitThread`: `ApcQueueable` (`+0xed`) → `SchedulerApc` (`+0x152`) → `SuspendEvent` (`+0x163`) → `Teb` (짐작) → `KiInitializeContextThread`: `SegCs`·`SegSs` 손보기 (`+0xb5`, 짐작) → `KxContextToKframes` 트랩 `Rip`·`Rsp`·`Rcx`·`Rdx` 처음 → `InitialStack`·`KernelStack` (`+0x2ca`·`+0x2ce`).
   4. `+0xfc2 PspInsertProcess` — `ActiveProcessLinks`·`SequenceNumber` (`+0xa6 ~ +0x109`).
   5. `+0x101a PspInsertThread` (흐름 순서, run12) — `ActiveThreads` +1 → `ThreadWasActive` (짐작) → `ThreadListHead` (짐작) → `ProcessLock` 풀기 (`+0x2a2`) → (flags bit0) `KeSuspendThread`: `SuspendCount` +1 (`+0x60`) → `KiSuspendThread`: `SuspendEvent` 0 (`+0x6c`) → `KiSignalThreadForApc` `ApcState` → `ThreadInserted` (`+0x490`) → `ThreadLock` 풀기 (`+0x499`) → `KeReadyThread`.
2. **부모의 다음 시스템 콜 `NtResumeThread`** (2.2, run13·run14) — `PsMultiResumeThread +0x4e` `ProcessFirstResume` → `KeResumeThread +0x68` `SuspendCount` −1 → `KiResumeThread +0x20` `SuspendEvent` = 1.
3. **test.exe 스레드 — `KiStartUserThread → PspUserThreadStartup`** (2.3) — (멈춘 스레드면) APC 인터럽트 → `KiSchedulerApc +0xf2` `SuspendSchedulerApcWait` 켜고 잠듦 → (2 가 풀어 줌) → `+0xf7` 끔 → `+0x78` TEB `CurrentLocale` → `+0xb3 PspInitializeThunkContext`: `SpecialApcDisable` (`+0x113`) → 원래 문맥을 유저 스택에 맡김 (`RtlCopyContext`, 7장 — 표 없음) → `PspSetContext` 트랩 `Rip` = `LdrInitializeThunk`·`Rsp`·`Rcx`·`Rdx` → `SpecialApcDisable` 되돌림 (`+0x2de`) → `IRETQ` 로 유저.
   - 2 와 3 의 앞뒤 (스레드가 먼저 잠드나, 부모가 먼저 푸나) 는 동적 Q19.

### ② ntdll 단계 (test.exe 첫 스레드)

1. **`LdrpInitialize`** (2.4) — `SecurityCookieInitialized` (`InitSecurityCookie +0x42`, `__security_cookie` 짐작) → `LdrpIsSecureProcess` (`+0x5a`, 조건부) → `SpecialMachineFramesInitialized` (`InitSpecialMachineFrames +0x57`) ⇒ 꼬리 점프.
2. **`LdrpInitializeInternal`** — `LdrpHotPatchInitialized` 0 → 1 (`+0x3b`) → `LdrpInitializeHotPatching` `LdrpIsHotPatchingEnabled` (`+0x55`) → `LdrpNtdllHotPatchContext` (`+0xa7`) → `_LdrpInitialize` → 돌아와 `LdrpNtdllHotPatchContext` 비움 (`+0x5e`).
3. **`_LdrpInitialize`** — `LdrpProcessInitialized` 0 → 1 (`+0x3a`) → `LdrpInitCompleteEvent` (`NtCreateEvent`, syscall) · TEB `RanProcessInit` (`+0xea`) → PEB `LoaderLock` (`+0xfa`) → `LdrInitState` (`+0x101`) → PEB `ProcessInitializing` 켬 (`+0x107`) → `LdrInitializeMrdata` (`+0x14a`, 본문 안 봄) → **`LdrpInitializeProcess`** (4) → `LdrpDetourExist` (`+0x1d7`) → `LdrInitState` = 3 (`+0x205`) → `ProcessInitializing` 끔 (`+0x20f`).
4. **`LdrpInitializeProcess`** (앞쪽 run18 → 뒤쪽 run19)
   - 앞쪽: `PebLdr` 길이·빈 목록 셋·종료 칸 처음 값 (`+0x158 ~ +0x1b3`) → PEB `Ldr` (`+0x1c1`) → `LdrpAppHeaders` (`+0x36b`) → `LdrpHashTable` 비우기 (`+0x3c5`) → `LdrpInitializeExecutionOptions` (`LdrpLargePageDllKeyHandle`·`LdrpImageExpansionMitigation`, 매개변수 `LoaderThreads` 덮기) → `LdrpIsHotPatchingEnabled` = 0 (조건, `+0x587`) → `UseWOW64` (`+0x5f2`) → `LdrpSystemDllBase` (`+0x68c`) → PEB TLS 비트맵 4칸 (`+0x774 ~ +0x8a3`) → `FastPebLock` 만들기 (`+0x9cd`) → PEB `FastPebLock` (`+0x9e1`) → PEB `ProcessHeap` (`+0xa4b`) → `LdrpHeap` (`+0xb12`) → `LdrpKnownDllDirectoryHandle` (`+0xdb1`, 짐작) → `LdrpKnownDllPath` (`+0xe64 ~ +0xe72`) → `RtlpSystem32Dirs`·`LdrpSystem32` (`+0x109e ~ +0x10b2`) → `RtlpSystemDirs` (`+0x1161`) → **ntdll 항목** (`+0x11a2 LdrpInitializeNtdllDataTableEntry`: 항목·노드 → 칸 → 명부·해시 → 색인 → 마무리 → `State` 9 → `LdrpNtDllDataTableEntry`) → 초기화 순서 목록 머리 (`+0x1283 ~ +0x12d7`).
   - 뒤쪽: `+0x12e3 LdrpInitParallelLoadingSupport` (큐·재시도 큐·잠금·이벤트 둘) → **exe 항목** (`+0x135e LdrpAllocateModuleEntry` → `LdrpImageEntry` `+0x1374` → `FullDllName`·`BaseDllName`·`DllBase` → 명부 머리 → 색인 → 마무리 → `State` 9 `+0x1508`) → `LdrpLargePageDllKeyHandle` (`+0x179e`) → (WOW64 만 `LdrpLoadWow64`) → TEB `SchedulerSharedDataSlot` (`+0x183c`) → `LdrpInitializeTls` (`LdrpTlsList`, exe `TlsIndex`) → **`LdrpInitializeKernel32Functions`** (kernel32·kernelbase 항목 = `②M…` 길, `Kernel32ThreadInitThunkFunction`, `LdrpDetourExist`·`LdrpMainThreadToken`·일감 전역 …) → `LdrpEnableParallelLoading` (`LdrpMapAndSnapWork`) → `LdrInitState` = 1 (`+0x1a4c`) → exe 노드 `State` 2 (`+0x1a60`) → **`LdrpMapAndSnapDependency`** (dll1·dll2: 항목·노드·간선·일감·`State` 2 ~ 5) → `LdrpDrainWorkQueue(1)` → `LdrpPrepareModuleForExecution` (그래프 정리 `LowestLink`·`State` 6, `State` 7) → `LdrInitState` = 2 (`+0x1b1e`) → `LdrpInitializePerUserWindowsDirectory` (`RtlpSystemDirs`·`RtlpSystem32Dirs` 다시) → `LdrpProcessInitContextRecord` (`+0x1b83`) → **`LdrpInitializeGraphRecurse`** (모듈마다 `LdrpInitializeNode`: `State` 8 → 초기화 순서 목록 꼬리 → `LdrpCurrentDllInitializer` → TLS 콜백 (`LdrpTlsLock`) → 진입점 → `ProcessAttachCalled` → `State` 9) → `LdrpProcessInitContextRecord` = 0 (`+0x1bec`) → exe TLS (`+0x1c99 LdrpCallTlsInitializers`, `LdrpTlsLock`).
5. **은퇴** — `LdrInitializeThunk +0x18 NtContinue` → 커널 `KiContinueEx → KeTestAlertThread` (`ApcState`) → `KiContinuePreviousModeUser +0x422` 트랩 `Rip`·`Rsp`·`Rcx`·`Rdx` → `IRETQ` → `RtlUserThreadStart`.

## 6. 공용 부품

- **아직 없음.** 끝까지 본 공용 부품이 아직 없다 (Q78 에서 채움).
- 3장에 "주소 넘김" 으로 나온 받는 함수들 (`RtlInitializeCriticalSectionEx`, `RtlRbInsertNodeEx`, `ObDuplicateObject`, `KeInitializeApc`, `KeInitializeEvent`, `LdrGetProcedureAddressForCaller`, `TpAllocWork` 등) 이 후보다. 이름만 적어 둔다 — 본문은 안 봤다.

## 7. 빈 곳

`_workspace/plan.md` 로 옮길 때 출처는 baggage.md. 이번 바퀴에 생긴 빈 곳은 오케스트레이터 결정대로 새 항목 없이 **Q77 (사진)·Q78 (쓴 자리·줄기)** 에 출처로 붙는다.

### 7.1 0 그대로 — 사진 전

- 2장 "0 그대로" 는 전부 비어 있다. 정적으로는 "아무도 안 쓴다" 를 못 보인다 → **Q77** 사진 ① (`LdrInitializeThunk` 입구) · ② (`NtContinue` 입구) 에서 처음 값과 같은 칸을 센다.
- 먼저 볼 후보: 범위 밖 길에서만 쓰는 전역 (`LdrpDllDirectory`, `LdrpForkInProgress`), 아래 7.2·7.3 의 모르는 칸 전부.

### 7.2 구조체별 모르는 칸 (직접 칸 이름, 오프셋 순) — 모두 1,275칸

- `이름 [ … ]` = 그 정수 칸과 안의 비트 칸들 (다 모름). `이름 (모르는 비트만) [ … ]` = 정수 칸은 모르고 비트 몇 개는 앎 (아는 비트는 3장). `(X 의 나머지 비트) [ … ]` = 정수 칸 X 는 아는데 그 안 비트 몇 개는 모름. 오프셋·타입은 표 원본 `struct_<이름>.tsv`.

**`_PEB`** (모름 93 / 124)

```text
ReadImageFileExecOptions, BeingDebugged, (BitField 의 나머지 비트) [ImageUsesLargePages, IsProtectedProcess,
IsImageDynamicallyRelocated, SkipPatchingUser32Forwarders, IsPackagedProcess, IsAppContainer,
IsProtectedProcessLight, IsLongPathAwareProcess], Padding0, SubSystemData, AtlThunkSListPtr, IFEOKey,
CrossProcessFlags (모르는 비트만) [ProcessInJob, ProcessUsingVEH, ProcessUsingVCH, ProcessUsingFTH,
ProcessPreviouslyThrottled, ProcessCurrentlyThrottled, ProcessImagesHotPatched, ReservedBits0], Padding1,
KernelCallbackTable, UserSharedInfoPtr, SystemReserved, AtlThunkSListPtr32, ApiSetMap, TlsExpansionCounter,
Padding2, ReadOnlySharedMemoryBase, SharedData, ReadOnlyStaticServerData, AnsiCodePageData, OemCodePageData,
UnicodeCaseTableData, NumberOfHeaps, MaximumNumberOfHeaps, ProcessHeaps, GdiSharedHandleTable,
ProcessStarterHelper, GdiDCAttributeList, Padding3, Padding4, ActiveProcessAffinityMask, GdiHandleBuffer,
PostProcessInitRoutine, Padding5, AppCompatFlags, AppCompatFlagsUser, pShimData, AppCompatInfo, CSDVersion,
ActivationContextData, ProcessAssemblyStorageMap, SystemDefaultActivationContextData,
SystemAssemblyStorageMap, MinimumStackCommit, SparePointers, PatchLoaderData, ChpeV2ProcessInfo,
AppModelFeatureState, SpareUlongs, ActiveCodePage, OemCodePage, UseCaseMapping, UnusedNlsField,
WerRegistrationData, WerShipAssertPtr, EcCodeBitMap, pImageHeaderHash, TracingFlags [HeapTracingEnabled,
CritSecTracingEnabled, LibLoaderTracingEnabled, SpareTracingBits], Padding6,
CsrServerReadOnlySharedMemoryBase, TppWorkerpListLock, TppWorkerpList, WaitOnAddressHashTable,
TelemetryCoverageHeader, CloudFileFlags, CloudFileDiagFlags, PlaceholderCompatibilityMode,
PlaceholderCompatibilityModeReserved, LeapSecondFlags [SixtySecondEnabled, Reserved], NtGlobalFlag2,
ExtendedFeatureDisableMask, Win32ClientProcInfo
```

**`_RTL_USER_PROCESS_PARAMETERS`** (모름 0) — 없음.

**`_TEB`** (모름 133 / 137)

```text
EnvironmentPointer, ClientId, ActiveRpcHandle, ThreadLocalStoragePointer, ProcessEnvironmentBlock,
LastErrorValue, CountOfOwnedCriticalSections, CsrClientThread, Win32ThreadInfo, User32Reserved, UserReserved,
WOW32Reserved, FpSoftwareStatusRegister, ReservedForDebuggerInstrumentation, SystemReserved1, HeapFlsData,
RngState, PlaceholderCompatibilityMode, PlaceholderHydrationAlwaysExplicit, PlaceholderReserved,
ProxiedProcessId, _ActivationStack, WorkingOnBehalfTicket, ExceptionCode, Padding0,
ActivationContextStackPointer, InstrumentationCallbackSp, InstrumentationCallbackPreviousPc,
InstrumentationCallbackPreviousSp, TxFsContext, InstrumentationCallbackDisabled, UnalignedLoadStoreExceptions,
Padding1, GdiTebBatch, RealClientId, GdiCachedProcessHandle, GdiClientPID, GdiClientTID, GdiThreadLocalInfo,
Win32ClientInfo, glDispatchTable, glReserved1, glReserved2, glSectionInfo, glSection, glTable, glCurrentRC,
glContext, LastStatusValue, Padding2, StaticUnicodeString, StaticUnicodeBuffer, Padding3, DeallocationStack,
TlsSlots, TlsLinks, Vdm, ReservedForNtRpc, DbgSsReserved, HardErrorMode, Padding4, Instrumentation,
ActivityId, SubProcessTag, PerflibData, EtwTraceData, WinSockData, GdiBatchCount, CurrentIdealProcessor,
IdealProcessorValue, ReservedPad0, ReservedPad1, ReservedPad2, IdealProcessor, GuaranteedStackBytes, Padding5,
ReservedForPerf, ReservedForOle, WaitingOnLoaderLock, Padding6, SavedPriorityState, ReservedForCodeCoverage,
ThreadPoolData, TlsExpansionSlots, ChpeV2CpuAreaInfo, Unused, MuiGeneration, IsImpersonating, NlsCache,
pShimData, HeapData, Padding7, CurrentTransactionHandle, ActiveFrame, FlsData, PreferredLanguages,
UserPrefLanguages, MergedPrefLanguages, MuiImpersonation, CrossTebFlags [SpareCrossTebBits], SameTebFlags (모르는
비트만) [SafeThunkCall, InDebugPrint, HasFiberData, SkipThreadAttach, WerInShipAssertCode, ClonedThread,
SuppressDebugMsg, DisableUserStackWalk, RtlExceptionAttached, InitialThread, SessionAware, LoadOwner,
LoaderWorker, SkipLoaderInit, SkipFileAPIBrokering], TxnScopeEnterCallback, TxnScopeExitCallback,
TxnScopeContext, LockCount, WowTebOffset, ResourceRetValue, ReservedForWdf, ReservedForCrt,
EffectiveContainerId, LastSleepCounter, SpinCallCount, Padding8, ExtendedFeatureDisableMask, HeapWalkContext,
PrimaryGroupAffinity, Rcu
```

**`_PEB_LDR_DATA`** (모름 2 / 9)

```text
SsHandle, EntryInProgress
```

**`_LDR_DATA_TABLE_ENTRY`** (모름 34 / 65)

```text
FlagGroup, (Flags 의 나머지 비트) [PackagedBinary, MarkedForRemoval, LoadNotificationsSent, TelemetryEntryProcessed,
ShimDll, VerifierProvider, ShimEngineCalloutSent, LoadInProgress, LoadConfigProcessed, EntryProcessed,
ProtectDelayLoad, AuxIatCopyPrivate, ReservedFlags3, DontCallForThreads, ProcessAttachFailed,
ScpInExceptionTable, CorImage, DontRelocate, CorILOnly, ChpeImage, ChpeEmulatorImage, ReservedFlags5,
Redirected, ReservedFlags6, CompatDatabaseProcessed], EntryPointActivationContext, Lock, NodeModuleLink,
SwitchBackContext, LoadTime, DependentLoadFlags, SigningLevel, ActivePatchImageBase
```

**`_LDR_DDAG_NODE`** (모름 4 / 10)

```text
ServiceTagList, LoadWhileUnloadingCount, CondenseLink, PreorderNumber
```

**`_KUSER_SHARED_DATA`** (모름 103 / 103)

```text
TickCountLowDeprecated, TickCountMultiplier, InterruptTime, SystemTime, TimeZoneBias, ImageNumberLow,
ImageNumberHigh, NtSystemRoot, MaxStackTraceDepth, CryptoExponent, TimeZoneId, LargePageMinimum,
AitSamplingValue, AppCompatFlag, RNGSeedVersion, GlobalValidationRunlevel, TimeZoneBiasStamp, NtBuildNumber,
NtProductType, ProductTypeIsValid, Reserved0, NativeProcessorArchitecture, NtMajorVersion, NtMinorVersion,
ProcessorFeatures, Reserved1, Reserved3, TimeSlip, AlternativeArchitecture, BootId, SystemExpirationDate,
SuiteMask, KdDebuggerEnabled, MitigationPolicies [NXSupportPolicy, SEHValidationPolicy,
CurDirDevicesSkippedForDlls, Reserved], CyclesPerYield, ActiveConsoleId, DismountCount, ComPlusPackage,
LastSystemRITEventTickCount, NumberOfPhysicalPages, SafeBootMode, VirtualizationFlags, Reserved12,
SharedDataFlags [DbgErrorPortPresent, DbgElevationEnabled, DbgVirtEnabled, DbgInstallerDetectEnabled,
DbgLkgEnabled, DbgDynProcessorEnabled, DbgConsoleBrokerEnabled, DbgSecureBootEnabled, DbgMultiSessionSku,
DbgMultiUsersInSessionSku, DbgStateSeparationEnabled, SpareBits], DataFlagsPad, TestRetInstruction,
QpcFrequency, SystemCall, Reserved2, FullNumberOfPhysicalPages, SystemCallPad, TickCount, TickCountQuad,
ReservedTickCountOverlay, TickCountPad, Cookie, CookiePad, ConsoleSessionForegroundProcessId, TimeUpdateLock,
BaselineSystemTimeQpc, BaselineInterruptTimeQpc, QpcSystemTimeIncrement, QpcInterruptTimeIncrement,
QpcSystemTimeIncrementShift, QpcInterruptTimeIncrementShift, UnparkedProcessorCount, EnclaveFeatureMask,
TelemetryCoverageRound, UserModeGlobalLogger, ImageFileExecutionOptions, LangGenerationCount, Reserved4,
InterruptTimeBias, QpcBias, ActiveProcessorCount, ActiveGroupCount, Reserved9, QpcData, QpcBypassEnabled,
QpcReserved, TimeZoneBiasEffectiveStart, TimeZoneBiasEffectiveEnd, XState, FeatureConfigurationChangeStamp,
Spare, UserPointerAuthMask, Reserved10
```

**`_EPROCESS`** (모름 241 / 263)

```text
RundownProtect, Flags2 [JobNotReallyActive, AccountingFolded, NewProcessReported, ExitProcessReported,
ReportCommitChanges, LastReportMemory, ForceWakeCharge, CrossSessionCreate, NeedsHandleRundown,
RefTraceEnabled, PicoCreated, EmptyJobEvaluated, DefaultPagePriority, PrimaryTokenFrozen,
ProcessVerifierTarget, RestrictSetThreadContext, AffinityPermanent, AffinityUpdateEnable, PropagateNode,
ExplicitAffinity, Flags2Available1, EnableReadVmLogging, EnableWriteVmLogging,
FatalAccessTerminationRequested, DisableSystemAllowedCpuSet, Flags2Available2, InPrivate], Flags
[CreateReported, NoDebugInherit, ProcessExiting, ProcessDelete, ManageExecutableMemoryWrites, VmDeleted,
OutswapEnabled, Outswapped, FailFastOnCommitFail, Wow64VaSpace4Gb, AddressSpaceInitialized,
SetTimerResolution, BreakOnTermination, DeprioritizeViews, WriteWatch, ProcessInSession, OverrideAddressSpace,
HasAddressSpace, LaunchPrefetched, Reserved, VmTopDown, ImageNotifyDone, PdeUpdateNeeded, VdmAllowed,
ProcessRundown, ProcessInserted, DefaultIoPriority, ProcessSelfDelete, SetTimerResolutionLink],
ProcessQuotaUsage, ProcessQuotaPeak, PeakVirtualSize, VirtualSize, SessionProcessLinks, ExceptionPortData,
ExceptionPortValue [ExceptionPortState], MmReserved, AddressCreationLock, PageTableCommitmentLock,
RotateInProgress, ForkInProgress, CommitChargeJob, CloneRoot, NumberOfPrivatePages, MmReserved2, Win32Process,
Job, SectionObject, Cookie, WorkingSetWatch, Win32WindowStation, InheritedFromUniqueProcessId, Session,
Spare1, QuotaBlock, DebugPort, WoW64Process, DeviceMap, EtwDataSource, PageDirectoryPte, ImageFilePointer,
ImageFileName, PriorityClass, SecurityPort, SeAuditProcessCreationInfo, JobLinks, HighestUserAddress,
ImagePathHash, DefaultHardErrorProcessing, LastThreadExitStatus, PrefetchTrace, LockedPagesList,
ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount,
OtherTransferCount, CommitChargeLimit, CommitCharge, CommitChargePeak, Vm, MmProcessLinks, ModifiedPageCount,
ExitStatus, VadRoot, VadHint, VadCount, VadPhysicalPages, VadPhysicalPagesLimit, AlpcContext,
TimerResolutionLink, TimerResolutionStackRecord, RequestedTimerResolution, SmallestTimerResolution, ExitTime,
InvertedFunctionTable, InvertedFunctionTableLock, ActiveThreadsHighWatermark, LargePrivateVadCount,
ThreadListLock, WnfContext, ServerSilo, SignatureLevel, SectionSignatureLevel, Protection, HangCount,
GhostCount, PrefilterException, Flags3 (모르는 비트만) [ReplacingPageRoot, Crashed, JobVadsAreTracked,
VadTrackingDisabled, SubsystemProcess, RelinquishedCommit, HighGraphicsPriority, CommitFailLogged,
ReserveFailLogged, AllImagesAtBasePristineBase, AddressPolicyFrozen, ForegroundExternal, ForegroundSystem,
HighMemoryPriority, EnableProcessSuspendResumeLogging, EnableThreadSuspendResumeLogging,
SecurityDomainChanged, SecurityFreezeComplete, VmProcessorHostTransition, AltSyscall, TimerResolutionIgnore,
EnableProcessRemoteExecProtectVmLogging, EnableProcessLocalExecProtectVmLogging,
EnableProcessImpersonationLogging], DeviceAsid, SvmData, SvmProcessLock, SvmLock, SvmProcessDeviceListHead,
LastFreezeInterruptTime, DiskCounters, PicoContext, EnclaveTable, EnclaveNumber, EnclaveLock,
HighPriorityFaultsAllowed, EnergyContext, VmContext, CreateInterruptTime, CreateUnbiasedInterruptTime,
TotalUnbiasedFrozenTime, LastAppStateUpdateTime, LastAppStateUptime, LastAppState, SharedCommitCharge,
SharedCommitLock, SharedCommitLinks, AllowedCpuSets, DefaultCpuSets, AllowedCpuSetsIndirect,
DefaultCpuSetsIndirect, DiskIoAttribution, DxgProcess, Win32KFilterSet, Machine, MmSlabIdentity, Spare0,
ProcessTimerDelay, KTimerSets, KTimer2Sets, ThreadTimerSets, VirtualTimerListLock, VirtualTimerListHead,
WakeChannel, WakeInfo, MitigationFlags, MitigationFlagsValues, MitigationFlags2, MitigationFlags2Values,
PartitionObject, SecurityDomain, ParentSecurityDomain, CoverageSamplerContext, MmHotPatchContext,
DynamicEHContinuationTargetsTree, DynamicEHContinuationTargetsLock, DynamicEnforcedCetCompatibleRanges,
DisabledComponentFlags, PageCombineSequence, EnableOptionalXStateFeaturesLock, PathRedirectionHashes,
SyscallProvider, SyscallProviderProcessLinks, SyscallProviderDispatchContext, MitigationFlags3,
MitigationFlags3Values, Flags4 (모르는 비트만) [MinimalTerminate, ImageExpansionDisable, SessionFirstProcess],
SyscallUsage, SyscallUsageValues, SupervisorDeviceAsid, SupervisorSvmData, NetworkCounters, Execution,
ThreadIndexTable, FreezeWorkLinks, PpmPolicyLock, BamPpmPolicy
```

**`_KPROCESS`** (모름 65 / 65)

```text
Header, ProfileListHead, DirectoryTableBase, ThreadListHead, ProcessLock, ProcessTimerDelay,
DeepFreezeStartTime, Affinity, AutoBoostState, ReadyListHead, SwapListEntry, ActiveProcessors, ProcessFlags
[AutoAlignment, DisableBoost, DisableQuantum, DeepFreeze, TimerVirtualization, CheckStackExtents,
CacheIsolationEnabled, PpmPolicy, VaSpaceDeleted, MultiGroup, ForegroundProcess, ReservedFlags], Spare0c,
BasePriority, QuantumReset, Visited, Flags, ActiveGroupsMask, ActiveGroupPadding,
IdealProcessorAssignmentBlock, Padding, Padding2, SchedulerAssistYieldBoostCount,
SchedulerAssistYieldBoostAllowedTime, Spare0d, IdealGlobalNode, Spare1, StackCount, ProcessListEntry,
CycleTime, ContextSwitches, SchedulingGroup, KernelTime, UserTime, ReadyTime, FreezeCount, Spare4,
UserDirectoryTableBase, AddressPolicy, Spare2, InstrumentationCallback, SecureState, KernelWaitTime,
UserWaitTime, LastRebalanceQpc, PerProcessorCycleTimes, ExtendedFeatureDisableMask, PrimaryGroup, Spare3,
UserCetLogging, CpuPartitionList, AvailableCpuState
```

**`_ETHREAD`** (모름 144 / 149)

```text
Tcb, CreateTime, ExitTime, KeyedWaitChain, PostBlockList, ForwardLinkShadow, TerminationPort, ReaperLink,
KeyedWaitValue, ActiveTimerListLock, ActiveTimerListHead, KeyedWaitSemaphore, AlpcWaitSemaphore,
ClientSecurity, IrpList, TopLevelIrp, DeviceToVerify, ChargeOnlySession, LegacyPowerObject, ThreadListEntry,
RundownProtect, ReadClusterSize, MmLockOrdering, CrossThreadFlags (모르는 비트만) [Terminated, HideFromDebugger,
ActiveImpersonationInfo, HardErrorsAreDisabled, BreakOnTermination, SkipCreationMsg, SkipTerminationMsg,
CopyTokenOnOpen, ThreadIoPriority, ThreadPagePriority, RundownFail, UmsForceQueueTermination, IndirectCpuSets,
DisableDynamicCodeOptOut, ExplicitCaseSensitivity, PicoNotifyExit, DbgWerUserReportActive,
ForcedSelfTrimActive, SamplingCoverage, ImpersonationSchedulingGroup, ReservedCrossThreadFlags],
SameThreadPassiveFlags [ActiveExWorker, MemoryMaker, StoreLockThread, ClonedThread, KeyedEventInUse,
SelfTerminate, RespectIoPriority, ActivePageLists, SecureContext, ZeroPageThread, WorkloadClass,
GenerateDumpOnBadHandleAccess, BalanceSetManager, ReservedSameThreadPassiveFlags], SameThreadApcFlags
[OwnsProcessAddressSpaceExclusive, OwnsProcessAddressSpaceShared, HardFaultBehavior, StartAddressInvalid,
EtwCalloutActive, SuppressSymbolLoad, Prefetching, OwnsVadExclusive], SystemPagePriorityActive,
SystemPagePriority, AllowUserWritesToExecutableMemory, AllowKernelWritesToExecutableMemory, OwnsVadShared,
PasidMsrValid, SlabReplenishInProgress, CacheManagerActive, DisablePageFaultClustering, ActiveFaultCount,
LockOrderState, SharedPsModuleLockAcquires, MmReserved, AlpcMessageId, AlpcMessage, AlpcReceiveAttributeSet,
AlpcWaitListEntry, ExitStatus, CacheManagerCount, IoBoostCount, IoQoSBoostCount, IoQoSThrottleCount,
KernelStackReference, BoostList, DeboostList, BoostListLock, IrpListLock, ReservedForSynchTracking,
CmCallbackListHead, ActivityId, SeLearningModeListHead, VerifierContext, AdjustedClientToken,
WorkOnBehalfThread, PropertySet, PicoContext, UserFsBase, UserGsBase, EnergyValues, SelectedCpuSets,
SelectedCpuSetsIndirect, Silo, ThreadName, SetContextState, EtwSupport, OwnerEntryListHead,
DisownedOwnerEntryListLock, DisownedOwnerEntryListHead, SchedulerSharedDataObject, CmThreadInfo, FlsData,
LastExpectedRunTime, LastSoftParkElectionRunTime, LastSoftParkElectionGeneration,
LastSoftParkElectionGroupAffinity, UserIsolationDomain, UpdateTebApc, UpdateTebApcFill1, Win32kPriorityFloor,
UpdateTebApcFill2, LastSoftParkElectionQos, LastSoftParkElectionWorkloadType, LastSoftParkElectionRunningType,
MmSlabIdentity, UpdateTebApcFill3, RngState, UpdateTebApcFill4, UsedByRngState, UpdateTebApcFill5,
UpdateTebSpareByte2, HeapData, Win32kThreadLock, ThreadIndex, MmFaultClusterContext, ProcessFastRef
```

**`_KTHREAD`** (모름 227 / 237)

```text
Header, SListFaultAddress, QuantumTarget, StackLimit, StackBase, ThreadLock, CycleTime, CurrentRunTime,
ExpectedRunTime, StateSaveArea, SchedulingGroup, WaitRegister, Running, Alerted, MiscFlags (모르는 비트만)
[AutoBoostActive, ReadyTransition, WaitNext, SystemAffinityActive, Alertable, Reserved1, ApcInterruptRequest,
QuantumEndMigrate, SecureThread, TimerActive, SystemThread, ProcessDetachActive, Reserved2, ScbReadyQueue,
Reserved3, WaitNextClearWobPriorityFloor, TimerSuspended, SuspendedWaitMode, CetUserShadowStack,
BypassProcessFreeze, CetKernelShadowStack, StateSaveAreaDecoupled, Reserved], ThreadFlags
[UserIdealProcessorFixed, IsolationWidth, AutoAlignment, DisableBoost, AlertedByThreadId, QuantumDonation,
EnableStackSwap, GuiThread, DisableQuantum, ChargeOnlySchedulingGroup, DeferPreemption, QueueDeferPreemption,
ForceDeferSchedule, SharedReadyQueueAffinity, FreezeCount, TerminationApcRequest, AutoBoostEntriesExhausted,
KernelStackResident, TerminateRequestReason, ProcessStackCountDecremented, RestrictedGuiThread,
VpBackingThread, EtwStackTraceCrimsonApcDisabled, EtwStackTraceApcInserted], Tag, SameThreadTransientFlags
[CalloutActive, ReservedStackInUse, UserStackWalkActive, SameThreadTransientFlagsReserved], SpecCtrl
[RunningNonRetpolineCode, SpecCtrlSpare], SystemCallNumber, ReadyTime, FirstArgument, TrapFrame, ApcStateFill,
Priority, UserIdealProcessor, WaitStatus, WaitBlockList, WaitListEntry, SwapListEntry, Queue,
RelativeTimerBias, Timer, WaitBlock, WaitBlockFill4, ContextSwitches, WaitBlockFill5, State, Spare13,
WaitIrql, WaitMode, WaitBlockFill6, WaitTime, WaitBlockFill7, KernelApcDisable, CombinedApcDisable,
WaitBlockFill8, ThreadCounters, WaitBlockFill9, XStateSave, WaitBlockFill10, Win32Thread, WaitBlockFill11,
Spare18, LastXStateSaveDebugInfo, ThreadFlags2 [DisableKasan, AbContextSwitchState, ThreadFlags2Reserved],
BamQosLevel, HardwareFeedbackClass, PriorityDecrement [ForegroundBoost, UnusualBoost], QueueListEntry,
NextProcessor [NextProcessorNumber, SharedReadyQueue], QueuePriority, Process, UserAffinity,
UserAffinityPrimaryGroup, PreviousMode, BasePriority, Spare24, Preempted, AdjustReason, AdjustIncrement,
AffinityVersion, Affinity, AffinityPrimaryGroup, ApcStateIndex, WaitBlockCount, IdealProcessor, NpxState,
SavedApcState, SavedApcStateFill, WaitReason, Saturation, SListFaultCount, SchedulerApcFill1, QuantumReset,
SchedulerApcFill2, KernelTime, SchedulerApcFill3, WaitPrcb, SchedulerApcFill4, LegoData, SchedulerApcFill5,
CallbackNestingLevel, UserTime, ThreadListEntry, MutantListHead, AbWaitEntryCount, AbOwnedEntryCount,
AbEntryCountValue, FreezeFlags [FreezeCount2, FreezeNormal, FreezeDeep], WobPriority, SecureThreadCookie,
SchedulerSharedSystemSlot, PropagateBoostsEntry, IoSelfBoostsEntry, PriorityFloorCounts, PriorityFloorSummary,
AbCompletedIoBoostCount, AbCompletedIoQoSBoostCount, KeReferenceCount, DecayBoost, Spare6, ForegroundLossTime,
GlobalForegroundListEntry, ForegroundDpcStackListEntry, InGlobalForegroundList, ReadOperationCount,
WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount,
QueuedScb, ThreadTimerDelay, Spare26, PpmPolicy, Spare27, TracingPrivate, SchedulerAssist, AbWaitObject,
ReservedPreviousReadyTimeValue, KernelWaitTime, UserWaitTime, GlobalUpdateVpThreadPriorityListEntry,
UpdateVpThreadPriorityDpcStackListEntry, InGlobalUpdateVpThreadPriorityList, SchedulerAssistPriorityFloor,
RealtimePriorityFloor, KernelShadowStack, KernelShadowStackInitial, KernelShadowStackBase,
KernelShadowStackLimit, ExtendedFeatureDisableMask, HgsFeedbackStartTime, HgsFeedbackCycles,
HgsInvalidFeedbackCount, HgsLowerPerfClassFeedbackCount, HgsHigherPerfClassFeedbackCount, ModeHistory,
SystemAffinityTokenListHead, IptSaveArea, ResourceIndex, CoreIsolationReasons, BamQosLevelFromAssistPage,
SecureCallCoreIsolationCount, SchedulerSharedOffset, SchedulerSharedSwappablePage, KernelAbEntries,
UserAbEntries, KcsanThread, SchedulerAssistYieldCounter, SchedulerAssistYieldBoostCount,
SchedulerAssistLastYieldBoostTime, Padding
```

**`_KTRAP_FRAME`** (모름 60 / 66)

```text
P1Home, P2Home, P3Home, P4Home, P5, PreviousMode, InterruptRetpolineState, PreviousIrql, FaultIndicator,
NmiMsrIbrs, ExceptionActive, MxCsr, Rax, R8, R9, R10, R11, GsBase, GsSwap, Xmm0, Xmm1, Xmm2, Xmm3, Xmm4, Xmm5,
FaultAddress, ContextRecord, Dr0, Dr1, Dr2, Dr3, Dr6, Dr7, ShadowStackFrame, Spare, DebugControl,
LastBranchToRip, LastBranchFromRip, LastExceptionToRip, LastExceptionFromRip, SegDs, SegEs, SegFs, SegGs,
TrapFrame, NmiPreviousSpecCtrl, NmiPreviousSpecCtrlPad, Rbx, Rdi, Rsi, Rbp, ErrorCode, ExceptionFrame, Fill0,
Logging, Fill1, EFlags, Fill2, Fill3, Fill4
```

**`_HANDLE_TABLE`** (모름 17 / 17)

```text
NextHandleNeedingPool, ExtraInfoPages, TableCode, QuotaProcess, HandleTableList, UniqueProcessId, Flags
[StrictFIFO, EnableHandleExceptions, Rundown, Duplicated, RaiseUMExceptionOnInvalidHandleClose],
HandleContentionEvent, HandleTableLock, FreeLists, ActualEntry, DebugInfo
```

**`_MMVAD`** (모름 9 / 9)

```text
Core, VadFlags2, Subsection, FirstPrototypePte, LastContiguousPte, ViewLinks, VadsProcess, u4, FileObject
```

**`_MMVAD_SHORT`** (모름 14 / 14)

```text
NextVad, ExtraCreateInfo, VadNode, StartingVpn, EndingVpn, StartingVpnHigh, EndingVpnHigh, CommitChargeHigh,
SpareNT64VadUChar, ReferenceCount, PushLock, u, CommitCharge, u5
```

**`_TOKEN`** (모름 55 / 55)

```text
TokenSource, TokenId, AuthenticationId, ParentTokenId, ExpirationTime, TokenLock, ModifiedId, Privileges,
AuditPolicy, SessionId, UserAndGroupCount, RestrictedSidCount, VariableLength, DynamicCharged,
DynamicAvailable, DefaultOwnerIndex, UserAndGroups, RestrictedSids, PrimaryGroup, DynamicPart, DefaultDacl,
TokenType, ImpersonationLevel, TokenFlags, TokenInUse, IntegrityLevelIndex, MandatoryPolicy, LogonSession,
OriginatingLogonSession, SidHash, RestrictedSidHash, pSecurityAttributes, Package, Capabilities,
CapabilityCount, CapabilitiesHash, LowboxNumberEntry, LowboxHandlesEntry, pClaimAttributes, TrustLevelSid,
TrustLinkedToken, IntegrityLevelSidValue, TokenSidValues, IndexEntry, DiagnosticInfo,
BnoIsolationHandlesEntry, SessionObject, TokenFlags2 [IsInstaller, IsAgentic], AntiTamperSids,
AntiTamperSidCount, AntiTamperSidHash, AntiTamperEntry, VariablePart
```

**`_SECTION`** (모름 9 / 9)

```text
SectionNode, StartingVpn, EndingVpn, u1, SizeOfSection, u, InitialPageProtection, SessionId,
NoValidationNeeded
```

**`_CONTROL_AREA`** (모름 15 / 17)

```text
Segment, ListHead, AweContext, NumberOfSectionReferences, NumberOfPfnReferences, NumberOfMappedViews, u1,
FilePointer, ControlAreaLock, ModifiedWriteCount, WaitList, u2, FileObjectLock, LockedPages, u3
```

**`_SEGMENT`** (모름 10 / 11)

```text
ControlArea, TotalNumberOfPtes, SegmentFlags, NumberOfCommittedPages, SizeOfSegment, ExtendInfo, BasedAddress,
SegmentLock, u1, PrototypePte
```

**`_SECTION_IMAGE_INFORMATION`** (모름 10 / 26)

```text
ZeroBits, SubSystemVersion, OperatingSystemVersion, ImageFlags (모르는 비트만) [ComPlusNativeReady, ComPlusILOnly,
ImageMappedFlat, BaseBelow4gb, ComPlusPrefer32bit, Reserved]
```

**`_FILE_OBJECT`** (모름 30 / 30)

```text
Type, Size, DeviceObject, Vpb, FsContext, FsContext2, SectionObjectPointer, PrivateCacheMap, FinalStatus,
RelatedFileObject, LockOperation, DeletePending, ReadAccess, WriteAccess, DeleteAccess, SharedRead,
SharedWrite, SharedDelete, Flags, FileName, CurrentByteOffset, Waiters, Busy, LastLock, Lock, Event,
CompletionContext, IrpListLock, IrpList, FileObjectExtension
```

### 7.3 ntdll 전역 — 쓴 자리 모르는 것 (rva 순) — 모두 439개

- 이름은 PDB 공개 이름. 크기는 표 원본 `globals.tsv` (전부 `[추측]`). 문서에 이름조차 없는 것이 `.data` 343, `.mrdata` 39 (결과서 2번).

**ntdll `.data`** (모름 391 / 438)

```text
_pctype, _pwctype, __initiallocinfo, __lconv_c, RtlpContinuationContextMachineFrameEntries,
?g_SbTableEntry_VerifyVersionInfoSwitch_RTL@@3U_SWITCHBRANCH_MODULE_TABLE_ENTRY@@A,
?g_SbTableEntry_HeapPaddingAndLFHSubsegmentCommitSwitch_RTL@@3U_SWITCHBRANCH_MODULE_TABLE_ENTRY@@A,
RtlpSecMemListHead, RtlpLockedSectionList, RtlpDebugPageHeapTable, NormalizationListHead, TppPoolpList,
TppCleanupGroupMemberpNoPoolList, LdrpUserDllDirectories, LdrpSnapsLock, LdrpSnapsLock_DEBUG,
LdrpLoaderLock_DEBUG, LdrpLoaderLock, LdrpDllNotificationList, RtlCriticalSectionList,
LdrpDllNotificationLock_DEBUG, LdrpDllNotificationLock,
?g_SbCachedModuleTable_RTL@@3U_SWITCHBRANCH_CACHED_MODULE_TABLE@@A, ?dazSupported@?1??_control87@@9@9,
__isa_info, _matherr_flag, __mb_cur_max, ShowSnaps, g_sqmSessionHandle, g_SxsMaxDeadActivationContexts,
GlobalRtlNlsState, NlsAnsiCodePage, Utf8TableInfo, RtlpSrwLockAllowImplicitUpgrade,
PossibleDeadlockReportThreshold, RtlpPtrTree, RtlpLargestLfhBlock, RtlpOptimalFlushMethod, RtlpClFlushSize,
RtlpIsFlushRequired, __ft_g_token, TelemetryEventThrottle, RtlpHeapFailureInfo,
g_SymCryptCpuFeaturesNotPresent, RtlpDiskSpeedPolicy, RtlpUnloadEventTraceExSize,
RtlpUnloadEventTraceExNumber, LdrpQueryProcessModuleInformationLoopDetectorCount, ModuleNamePlaceholderBuffer,
LdrpPolicyBits, SbDynamicContextDllCount, RuntimeGlobal,
__@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUmgwooUwzbglmzUlyquivUznwGEUkxsOlyq@ntdll, AVrfpHandleTraces,
AVrfpDebug, g_disableEscalation, g_werEscalationData, g_SxsCurrentDeadActivationContexts,
g_SxsTrackReleaseStacks, g_SxsKeepActivationContextsAlive, g_SxsFreeActivationContexts,
g_SxsLiveActivationContexts, RtlpFlsContext, RtlCriticalSectionDebugSList, EtwpFreeRegistrationList,
RtlpHpHeapGlobals, ?RtlpHpEnvFlsContext@@3U_RTLP_FLS_CONTEXT@@A, LdrpThunkSignature,
RtlpCaptureActivationContextActivationStacks,
Feature_ProjectTessera_CreateProcessInSandbox__private_reporting,
g_wil_details_subscribeFeatureStateCacheToConfigurationChanges, Feature_UxAccOptimization__private_reporting,
Feature_Standalone_26_08_NonSec__private_reporting, Feature_Standalone_26_06_NonSec__private_reporting,
Feature_DriverSetup_ReofferFingerprinting_v2__private_reporting,
Feature_LearningModeTraceService__private_reporting, Feature_SettingsDel__private_reporting,
Feature_Tessera_202605Retail__private_reporting, Feature_Standalone_Future__private_reporting,
g_wil_details_ensureSubscribedToFeatureConfigurationChanges, Feature_TestLoc02__private_reporting,
Feature_ImplVal__private_reporting, g_wil_details_pfnFeatureLoggingHook, Feature_TestLoc03__private_reporting,
LdrpInvertedFunctionTableSRWLock, MuiCacheSWRLock, LdrpInitOnceLoadAsDataCrits, NlsMbOemCodePageTag,
NlsEmptyLeadByteInfoTable, NlsMbCodePageTag, Feature_KVS_BugFixes_55830752__private_reporting,
Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_reporting, RtlpCsVerifyDoNotBreak,
PossibleDeadlockReportCounter, RtlpCriticalSectionVerifier, RtlpLockedMemoryBlockLookasideCount,
RtlpMemoryBlockLookasideLock, RtlpMemoryZoneLock, RtlpLockedMemoryZoneCount,
Feature_GetPreferredThreadLanguage_Watson_AccessViolation_BugFix__private_reporting, InitRegistryInfoCritSect,
g_RegInfo, RtlpDisableBreakOnFailureCookie, RtlpGlobalTagHeap, RtlpPropStoreLock, RtlpPtrTreeLock,
RtlpFreezeTimeBias, Feature_FasterCrc32__private_reporting, RtlpHpHeapFeatures, RtlpHeapKey,
RtlpDisableHeapLookaside, RtlpSecMemLock, RtlpLDNumBlocks, RtlpTempBlocks, RtlpLeakHeapAddress,
RtlpLeaksCount, RtlpCrtHeapAddress, RtlpPreviousStartAddress, RtlpShutdownProcessFlags, RtlpLDPreviousPage,
RtlpLDCrtPage, RtlpValidateHeapHdrsEnable, RtlpIsDrainRequired, __ft_g_isTestReadyMedia, __ft_g_init_once,
g_dwLastErrorToBreakOn, g_isErrorOriginProviderEnabled, gCustomCultureRegKey, gLocaleDataRegKey,
gLocaleTables, pTblPtrs, gBaseAddress, TrkContext, RtlpExceptionLog2, RtlpLockedSectionListLock,
RtlpProtectedPoliciesSRWLock, RtlpPerformanceCounterFrequency, RtlpCapChkTelemetryRunOnceCtx, RtlpHpNullGUID,
RtlpHpTagInitVar, RtlpRandomExInit, RtlpHeapErrorHandlerThreshold, RtlpHpStackTraceProviderHandle,
RtlpHeapStackTraceLog, RtlpHpStackTraceLogInstance, RtlpHpStackTrackingContext, RtlpHpAppCompatFlags,
RtlpHpHeapManager, x86_cpu_enable_avx512, x86_cpu_enable_ssse3, x86_cpu_enable_simd, g_isUcpListInitialized,
UcpExclusiveLock, UcpInitOnUnexpectedCodePathOnce, UcpMaxTriggeredNodeExcceded, UcpLockOwningThreadHandle,
UcIsTraceLoggingInitialized, RtlpLowFragHeapGlobalFlags, RtlpLFHKey, RtlpHpEnvThreadSuspendOwnershipLock,
g_SymCryptFlags, g_SymCryptCpuFeaturesPresentCheck, EtwpRegistrationTable, EtwpGuidEntryTable, EtwpProvLock,
EtwpPrivSessionLock, EtwpRegisterTpInitOnce, TppMaximumGroups, TppNumberNodes, TppPoolpSerializedPoolLock,
TppPoolpGlobalPoolStackSize, TppPoolpListLock, TppPoolpSerializedPool, TppPoolpGlobalPool,
TppPoolpGlobalPoolLock, TppPoolpGlobalPoolMaxThreadsOverride, TppPoolpGlobalPoolMaxThreads,
TppPoolpDefaultPoolCpuSetLock, TppPoolpDefaultPoolCpuSetCount, TppHeapTag,
TppCleanupGroupMemberpNoPoolListLock, RtlpTpIoRegistered, RtlpTpIoTreeLock, RtlpTpIoRegisteredLock,
RtlpTpIoTree, RtlpTpIoDllNotificationCookie, RtlpDiskSpeedInit, g_MicrosoftTelemetryAssertLock,
g_MicrosoftTelemetryAssertsTriggeredList, LdrpPathLock, RtlpCachedPathLock, LdrpDefaultDllDirectories,
RtlpSearchPathMode, LdrpDllDirectoryLock, LdrpAppPackagesPathVersion, LdrpCreateActCtxLanguageW,
LdrpReleaseActCtxW, RtlpUnloadEventTraceEx, LdrpUnloadIndex, LdrpUnloadIndexEx, LdrpManifestProberRoutine,
g_ShimsEnabled, LdrpShouldCreateStackTraceDb, g_ShimsLoading, SecurityCookieInitCount, g_pShimmedModuleList,
Feature_Servicing_LoaderSnapsBuffer__private_reporting, SpecialMachineFramesInitCount, LdrpIllegalCWDDevices,
RtlpCurDirRef, Feature_TreatDosDevicePathsAsNormalFiles__private_reporting,
?CookieValue@?1??RtlpGetCookieValue@@9@9, LdrpForkActiveLock, LdrpForkConditionVariable, LdrpVchLock,
LdrpVehLock, RtlCriticalSectionLock, LdrpModuleDatatableLock, LdrpDelayedTlsReclaimTable,
LdrpCODScenarioTriggered, VsmEnclaveTelemetryInitRunOnce, LibLoaderTelemetryInitRunOnce,
CastGuardTelemetryInitRunOnce, LdrpCODScenarioLock, CastGuardProvidersRegistered,
CentennialIssueTracker_InitializedState, VSMEnclaveProvidersRegistered, LdrpThreadPool,
Feature_PackagedComElevationSupport_v2__private_reporting, LdrpRedirectionTree, SbpContextLock,
__@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUzkkxlnkzgUxzxsvUrmgviuzxvUorynrmUlyquivUznwGEUxlnkzgxzxsvkOlyq@compatcachemin,
__@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUzkkxlnkzgUhdrgxsyzxpUxlmgvcgUolzwviUlyquivUznwGEUkivxlnkOlyq@SwitchContextLoader,
LdrpCorExeMainRoutine, LdrpRedirectionByFunctionCalloutFunc, LdrpRedirectionModule,
LdrpRedirectionCalloutFunc, LdrpStatistics, LdrpActiveThreadCount, LdrpTlsHeap, LdrpPotentialTlsLeaks,
LdrpStaticTlsBitmapVector, LdrpActualBitmapSize, LdrpTlsBitmap, RtlpTimeoutDisable,
RtlFailedCriticalDebugAllocations, RtlpTimeout, LdrpSnapsUnicodeString2, LdrpSnapsUnicodeString,
LdrpFailureData, LdrpFatalHardErrorCount, RtlpEnvironmentHashTable, NtdllBaseTag,
RtlpUnhandledExceptionFilter, LdrpSchedulerSharedDataListHeadLock, LdrpEnforceIntegrityContinuity,
LdrpHotPatchInitCompleteEvent, LdrpAuditIntegrityContinuity, LdrpSuppressedExportOverrideListPtr, UseCOR,
LdrpSchedulerSharedDataListHead, g_pShimmedModuleListLength, g_LdrBreakOnLdrpInitializeProcessFailure,
LdrpHeapPartitionName, g_pShimEngineModule, LdrpUseImpersonatedDeviceMap, RtlpUnloadEventTrace,
LdrpLoaderLockAcquisitionCount, RtlpSearchPath, RtlpDllSearchPathWithOptions, RtlpExeSearchPath,
LdrpAppPackagesPath, LdrpOriginalAppPackagesPath,
__@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUkilxhmzkUmgwooUlyquivUznwGEUkivxlnkOlyq@pssntlib,
__@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUgsivzwklloUxlnkzgUwzbglmzUlyquivUznwGEUgkxlnkzgOlyq@tpcompat,
__@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUgsivzwklloUmgwooUwzbglmzUlyquivUznwGEUgkkOlyq@tp,
TppPoolpDefaultPoolCpuSets, g_hPrivLoggerNotificationProvider, PrivateLoggerNotificationEntry,
__@@_PchSym_@00@KxulyqvxgPillgKxunrmpvimvoUvgdUmgwooUwzbglmzUlyquivUznwGEUvgdmgwookOlyq@etwntdll,
EtwpGlobalSequence, EtwpLoggerArray, EtwpRegistrationCount, EtwpRegistrationSequence, EtwpMaxLoggers,
g_SymCryptCpuid1, RtlpHpEnvEnableSimulatedLargePageCommit, RtlpLowFragHeapRandomData, RtlpHpLfhPerfFlags,
RtlpHpHeapVaCallbacksRegistrar, UcpTriggeredList, UcpTriggeredNodeCount, NormalizationListLock,
RtlpGetCurrentProcessorNumberHow, RtlpAffinityState, RtlpHpEnvHandle, RtlpRandomExConstantVector,
RtlpRandomExAuxVarY, RtlpHpTagContext, RtlpDphInitFailureFlags, RtlpWaitOnAddressSpinCycleCount, RtlpRcuState,
RtlpCtPublishInProgress, RtlpFcProcessManager, RtlpStackTraceDatabase, RtlpExceptionLogSize,
RtlpExceptionLogCount, RtlProcessorFeaturesBuffer, RtlProcessorFeaturesBitMap,
ConditionVariableSpinCycleCount, gSystemLocale, g_hUserDiagnosticProvider, __ft_g_api_info,
__ft_g_feature_store_change_registration_handle, RtlpValidateHeapTagsEnable, RtlpHeapStopOn,
RtlpHeapInvalidBreakPoint, RtlpLeakHeap, RtlpProcessMemoryMap, RtlpBusyList, RtlpLeakList,
RtlSecureMemorySystemRangeStart, SmtDelayedConfiguration, RtlpHypervisorSharedUserVa,
RtlpPropStoreEntriesTotalCount, RtlpPropStoreEntries, RtlpPropStoreEntriesActiveCount,
RtlpPebHeapListStaticBuffer, RtlpProcessHeaps, RtlpProcessHeapsLock, RtlpDefaultHeapDebuggingOptions,
RegistryInfoCritSect, RtlpForceCSDebugInfoCreation, RtlpRaiseExceptionOnPossibleDeadlock, RtlpStaticDebugInfo,
RtlpForceCSToUseEvents, SRWLockSpinCycleCount, RtlpModernAppKey, LoadAsDataTableBlockCount, LoadAsDataCrits,
LoadAsDataTable, LoadAsDataTableCount, AlternateResourceModules, AlternateResourceModuleCount,
AltResMemBlockCount, gMUICacheType, RtlpDynamicFunctionTableLock, LdrpEnclaveList, LdrpEnclaveListLock,
TrustedAppsRemoteBreakinRoutine, AVrfpVerifierDllsString, AVrfpPageHeapPreviousStopData, AVrfpVerifierLock,
AVrfpEnabledSystemWide, AVrfpVerifierProvidersList, AVrfpVerifierFlags, AvrfpLoaderEntry,
AVrfpCurrentUserKeyPath, AVrfpVerifierStopMessageFunction, AVrfpPageHeapStopData, LdrpMrdataLock,
CsrServerApiRoutine, CsrClientProcess, CsrInitOnceDone, CsrPortName, CsrProcessId, CsrPortMemoryRemoteDelta,
CsrPortHandle, CsrPortHeap, CsrPortBaseTag, CsrHeap, HotPatchSpareGlobals,
Feature_UxAccOptimization__private_featureState, Feature_Standalone_26_06_NonSec__private_featureState,
Feature_TestLoc03__private_featureState, Feature_DriverSetup_ReofferFingerprinting_v2__private_featureState,
Feature_ProjectTessera_CreateProcessInSandbox__private_featureState,
Feature_LearningModeTraceService__private_featureState, Feature_Standalone_Future__private_featureState,
Feature_TestLoc02__private_featureState, Feature_Standalone_26_08_NonSec__private_featureState,
Feature_SettingsDel__private_featureState, Feature_Tessera_202605Retail__private_featureState,
Feature_ImplVal__private_featureState,
Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_featureState,
Feature_KVS_BugFixes_55830752__private_featureState,
Feature_GetPreferredThreadLanguage_Watson_AccessViolation_BugFix__private_featureState,
Feature_FasterCrc32__private_featureState, Feature_Servicing_LoaderSnapsBuffer__private_featureState,
Feature_TreatDosDevicePathsAsNormalFiles__private_featureState,
Feature_PackagedComElevationSupport_v2__private_featureState
```

**ntdll `.mrdata`** (모름 44 / 50)

```text
__guard_check_icall_fptr, LdrpMrdataHeap, LdrpMrdataSize, LdrpMrdataHeapUnprotected, LdrpMrdataBase,
LdrpMaximumUserModeAddress, LdrpAllocationGranularity, AvrfpAPILookupCallbacksEnabled, AVrfpEnabled,
RtlGuardAllowSuppressedCalls, AvrfpAPILookupCallbackRoutine, RtlpDynamicCallbackTableTreeMax,
RtlpDynamicCallbackTableTreeMin, RtlpDynamicFunctionTable, RtlpDynamicFunctionTableTreeMin,
RtlpDynamicFunctionTableTreeMax, RtlpProtectedPolicies, RtlpProtectedPoliciesActiveCount,
RtlpProtectedPoliciesTotalCount, g_pfnSE_GetProcAddressForCaller, g_pfnApphelpQueryModSettingsAllocProc,
g_pfnSE_DllUnloaded, g_pfnSE_ProcessDying, g_pfnSE_ShimDllLoaded, g_pfnApphelpCheckModuleProc,
Wow64ApcRoutine, g_pfnSE_InitializeEngine, Wow64PrepareForException, g_pfnSE_LdrResolveDllName,
g_pfnSE_InstallBeforeInit, g_LdrpWow64PrepareForDebuggerAttach, g_pfnSE_LdrEntryRemoved, g_pfnSE_DllLoaded,
g_LdrpWow64SuspendLocalProcess, g_LdrpWow64SuspendLocalThread, LdrpMrdataUnprotected, NtUserPfn,
LdrpUnalignedUserCallTargetBitMap, RtlpUnwindHistoryTable, LdrpGuardDispatchIcallNoESFptr,
LdrpInvertedFunctionTables, LdrpGuardCheckIcallNoESFptr, LdrpVectorHandlerList, __security_cookie_complement
```

**ntdll `.00cfg`** (모름 4 / 5)

```text
__guard_xfg_check_icall_fptr, __guard_xfg_dispatch_icall_fptr, __guard_xfg_table_dispatch_icall_fptr,
__castguard_check_failure_os_handled_fptr
```

### 7.4 짐작이라 다시 볼 것 (3장에 있지만 칸 단위 근거가 약함)

- `_RTL_USER_PROCESS_PARAMETERS` 22칸 — "블록 통째 복사" 로만 앎. 사진 ① 값 = 부모 블록 값인지 (Q77).
- `_PEB` 12칸 — `MmCreatePeb` 가 범위 (`+0xb8 ~ +0xe0`, `+0x118 ~ +0x124`) 로만 말함. 칸마다 쓰는 줄 (Q78).
- `_EPROCESS` `Pcb`·`Token`·`ObjectTable` — 부르는 줄만 앎 (callee 본문 안 봄). `ThreadListHead`·`ThreadWasActive` 쓰는 줄 오프셋, `ProcessLock` 잡는 쪽 (flow.md 4장 2.1 `[정적 + 간접]`).
- `_KTHREAD.Teb` (칸 이름 `[추측]`), `_KTRAP_FRAME.SegCs`·`SegSs` (옮기는 칸 목록에 안 나옴), `_CONTROL_AREA.u` (진짜 컨트롤 영역 칸 아님).
- `_LDR_DATA_TABLE_ENTRY` exe `ObsoleteLoadCount`·`LoadReason`, ntdll `HotPatchState`, `_LDR_DDAG_NODE` ntdll `LoadCount` — 문서의 `LoadCount = -1` 이 PDB 로는 노드 칸 (`+0x18`) 이라 이름 맞추기가 짐작.
- 전역: `LdrpDebugFlags`, `RtlpDllSearchPath`, `g_LdrpWow64LdrpInitialize`, `LdrpKnownDllDirectoryHandle`, `__security_cookie` (이름으로만), **`__guard_dispatch_icall_fptr`** (쓰는 코드 줄 없음 — 짐작으로 셌지만 주인은 사실상 모름).

### 7.5 모르는 칸인데 문서에 실마리가 있는 것 (3장으로 안 옮김 — 결과서 숫자를 그대로 두려고)

- TEB 칸 다수 ← `MmCreateTeb` 의 TEB 대입 약 47줄 (64비트 갈래 + WOW64 갈래, 결과서·run43). 칸별 표는 Q78.
- `_LDR_DATA_TABLE_ENTRY` `ProcessAttachFailed` (`Flags` 비트) ← `LdrpInitializeNode +0x29e` (진입점 실패 길, flow.md 2.7 run27).
- `_LDR_DATA_TABLE_ENTRY.EntryPointActivationContext` (+0x88) ← `LdrpFindDllActivationContext` (`LdrpMapAndSnapDependency +0x63`, flow.md 2.7 "run22 가 넣는 칸").
- `_TEB` `LoadOwner` (`SameTebFlags` `0x1000`) ← `LdrpDrainWorkQueue +0xd3` 켬, `LdrpDropLastInProgressCount` 끔 (flow.md 2.6 run24).
- `_PEB.ApiSetMap` (+0x68) ← `MmMapApiSetView` (`PspSetupUserProcessAddressSpace +0x255`) 가 올리는 뷰 (run32). 칸 쓰는 줄은 모름.
- PEB 칸 후보 ← `KeCopyXfdMaskToPeb` (`+0x227`), `PspUpdatePebForAffinityChange` (`+0x234`) — 이름으로 PEB 에 쓸 것 `[추측]` (flow.md 2.1 run32).
- `_PEB.PostProcessInitRoutine` (+0x230) — 읽는 곳 `LdrpInitializeProcess +0x1d16` 뿐, 넣는 쪽 모름 (run19, 동적 Q48).
- `.mrdata` `LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected`·`LdrpMrdataUnprotected` ← `LdrInitializeMrdata` (`_LdrpInitialize +0x14a`, 109바이트, 본문 안 봄) 후보 (결과서 공백 4).
- `LdrpProcessInitialized`·`LdrpHotPatchInitialized` 의 1 → 2 ← `LdrpInitializationComplete` (`_LdrpInitialize` ret `+0x252`, `LdrpInitializeInternal` ret `+0xdf`) 쪽으로 보임 `[추측]` (flow.md 2.4 "상태 1 → 2"). 칸 주소를 넘기는 줄 확인은 Q78.
- 락·이벤트처럼 **API 에 주소를 넘겨 쓰는 전역**은 `_refs` WRITE 줄로 안 잡힌다 (예: `LdrpWorkQueueLock` 은 복제 길 WRITE 줄만 있었고, 만들기·잡기는 flow.md 2.6 에서 찾아 더함). `.data` 모름 391 중에도 이런 것이 있을 것 `[추측]` → Q78 은 `_refs` DATA 줄 (`LEA`) 도 같이 본다.

### 7.6 칸 표가 아직 없는 짐

- PDB 에 있는 이웃 타입 (표 안 만듦, 결과서): `_HANDLE_TABLE_ENTRY` (0x10) · `_OBJECT_HEADER` (0x38) · `_SUBSECTION` (0x38) · `_MSUBSECTION` (0x98) · `_SECTION_OBJECT_POINTERS` (0x18) · `_PS_NTDLL_EXPORTS` (0xa8) · `_CONTEXT` (0x4d0) · `_MI_SECTION_IMAGE_INFORMATION` (0x50).
- PDB 에 **없는** 타입 (오프셋으로만 적어야 함): `LdrSystemDllInitBlock` 블록 (0x128) · `InitialTeb` (`PspSetupUserStack` 스택 정보) · `PS_CREATE_INFO` · `PS_ATTRIBUTE_LIST` · 간선 칸 0x20바이트 (run23) · `_LDRP_LOAD_CONTEXT` 칸 정보 (run22) · `_KCONTINUE_ARGUMENT` (run29) · `_SWITCHBRANCH_*` · `_RTLP_FLS_CONTEXT`.
- 칸이 아니라 구역인 짐: 유저 스택 위 **맡긴 문맥** (`_CONTEXT`, `PspInitializeThunkContext +0x285 RtlCopyContext`), 환경 블록 (`Environment` +0x80, 크기 `EnvironmentSize`), 매개변수 문자열들, API set 지도 뷰, leap second 뷰, `PEB.SharedData`·`TlsBitmap`·`ActivationContextData` 가 가리키는 것, TEB `ThreadLocalStoragePointer` (+0x58) 배열, 유저 스택, 프로세스 힙 (로더 항목이 사는 곳 — 결과서 Q77 표). 덤프 대상·주소 얻는 칸은 결과서 "Q77 에서 떠야 할 것".
- 커널 쪽 별칭 주소 (KUSD), 섹션·VAD·핸들 표 항목은 커널 객체라 칸 표 + 사진이 같이 필요하다.

### 7.7 결과서가 넘긴 공백 (sast/38 "새로 생긴 공백")

- 커널 객체 칸 근거가 거의 없다: `_HANDLE_TABLE`·`_MMVAD*`·`_TOKEN`·`_SECTION`·`_FILE_OBJECT`·`_KPROCESS` 아는 칸 0, `_EPROCESS` 모름 241/263, `_KTHREAD` 227/237. `PspAllocateProcess` callee 105 중 문서에 이름도 없는 40, 본문 뽑은 것 12. 안 뽑은 `KeStartThread`·`KeInitializeProcess`·`ObInitProcess`·`PspInitializeProcessSecurity`·`PsAssignThreadId`·`PspApplyMitigationOptions`·`MiAllocateProcessVads`·`MiInsertProcessVads` → **Q78**.
- `MmCreateTeb` 가 TEB 에 쓰는 칸 (대입 약 47줄) → **Q78**.
- `.mrdata` 를 쓰는 곳 `LdrInitializeMrdata` 본문 (`.mrdata` 50 중 44 모름) → **Q78**.
- 지도에 없는 함수 둘 `PspUserThreadStartup`·`PspInitializeThunkContext` — 지도 뿌리를 `PspUserThreadStartup` 으로 하나 더 (`run_ghidra.sh map ntoskrnl.exe PspUserThreadStartup`) → **Q78**.
- `.data` 438 중 문서에 이름도 없는 343, 이름은 나오는데 쓴 자리를 못 찾은 48 → **Q78** (줄 찾기) · **Q77** (바뀌나).
- 초기화 함수 15개의 callee 중 어디에도 없는 57개 (nt 43 · ntdll 14, `callees.tsv` 의 `doc=none`) — "짐을 놓나 / 잡일 / 안 타는 갈래" 가르기 → **Q78**.
- 간접 호출 10줄의 실제 대상, 주소만 넘기는 전역 6개의 실제 값 → **Q77**.

## 8. 근거 run

| run | 무엇을 채웠나 |
|---|---|
| run43 | **첫판** (sast/38 체크리스트): 구조체 21개 칸 목록 (직접 칸 1,456), ntdll 전역 493 (`.data` 438 · `.mrdata` 50 · `.00cfg` 5), 초기화 함수 15개 callee 637줄, 이미 아는 쓴 자리 대조 (칸 정확 138 · 짐작 43 · 모름 1,275, 전역 54). `MmCreateTeb` 첫 export. `LdrSystemDllInitBlock` 블록 타입 없음 확인 |
| run10 (sast/04) | 3장 근거 줄: `_PEB` 7 · `_EPROCESS` 7 |
| run11 (sast/06) | 3장 근거 줄: `_EPROCESS` 2 · `_ETHREAD` 4 · `_KTHREAD` 1 |
| run12 | 3장 근거 줄: `_EPROCESS` 5 · `_ETHREAD` 2 |
| run13 | 3장 근거 줄: `_EPROCESS` 1 · `_KTHREAD` 9 |
| run15 | 3장 근거 줄: `_TEB` 1 · `_KTHREAD` 2 · `_KTRAP_FRAME` 2 |
| run16 | 3장 근거 줄: `_KTHREAD` 1 · `_KTRAP_FRAME` 10 |
| run17 (sast/12) | 3장 근거 줄: `_PEB` 2 · `_TEB` 1 · `.data` 전역 11 · `.mrdata` 전역 2 |
| run18 (sast/13) | 3장 근거 줄: `_PEB` 4 · `_RTL_USER_PROCESS_PARAMETERS` 1 · `_PEB_LDR_DATA` 3 · `_LDR_DATA_TABLE_ENTRY` 1 · `.data` 전역 11 · `.mrdata` 전역 1 |
| run19 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 4 · `_LDR_DDAG_NODE` 1 · `.data` 전역 7 · `.mrdata` 전역 2 |
| run20 | 3장 근거 줄: `.data` 전역 3 |
| run21 | 3장 근거 줄: `.data` 전역 3 |
| run22 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 9 · `_LDR_DDAG_NODE` 3 |
| run23 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 2 · `_LDR_DDAG_NODE` 4 |
| run24 | 3장 근거 줄: `_LDR_DDAG_NODE` 4 · `.data` 전역 18 |
| run26 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 1 · `.data` 전역 1 |
| run27 | 3장 근거 줄: `_PEB_LDR_DATA` 1 · `_LDR_DATA_TABLE_ENTRY` 1 · `_LDR_DDAG_NODE` 3 · `.data` 전역 1 |
| run28 | 3장 근거 줄: `_LDR_DATA_TABLE_ENTRY` 2 · `.data` 전역 2 · `.00cfg` 전역 1 |
| run29 (sast/24) | 3장 근거 줄: `_KTHREAD` 1 · `_KTRAP_FRAME` 1 |
| run31 (sast/26) | 3장 근거 줄: `_CONTROL_AREA` 2 · `_SEGMENT` 1 · `_SECTION_IMAGE_INFORMATION` 17 |
| run32 (sast/27) | 3장 근거 줄: `_PEB` 1 · `_RTL_USER_PROCESS_PARAMETERS` 9 · `.mrdata` 전역 1 |
| run33 | 3장 근거 줄: `_EPROCESS` 3 |
| run34 | 3장 근거 줄: `_EPROCESS` 1 |
| run35 | 3장 근거 줄: `_PEB_LDR_DATA` 3 · `_LDR_DATA_TABLE_ENTRY` 19 · `_LDR_DDAG_NODE` 3 · `.data` 전역 3 |
| run36 | 3장 근거 줄: `_LDR_DDAG_NODE` 1 |
| run38 | 3장 근거 줄: `_TEB` 1 · `_LDR_DATA_TABLE_ENTRY` 1 |
| run39 | 3장 근거 줄: `.data` 전역 7 |
| run41 | 3장 근거 줄: `_TEB` 1 |
| `_refs` (ntdll 참조 표) | 3장 전역·`_PEB_LDR_DATA` 줄 대부분 (WRITE·READ_WRITE = 정확, DATA = 짐작) |

- 3장 근거 칸의 run 번호는 결과서 4번이 문서에서 옮긴 것이다 (해설서 `*-해설.md` 는 뺌). sast 번호는 그 run 의 결과서.


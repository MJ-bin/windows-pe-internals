# 40 process-heap — 정적 분석 결과 (ntdll 프로세스 힙 만들기, Q78a)

## 보통 길 시스템 콜 목록 (test.exe, `LdrpInitializeProcess +0x9ed ~ +0xa50`)

- 보통 길 = 아래 조건이 모두 test.exe 값일 때. **NT 힙** 길이다 (세그먼트 힙 아님). 구간 안 시스템 콜 **12개** (무작위 몫이 0 이면 11개).
  - NtOpenKey 2 · NtQueryInformationProcess 1 · NtQuerySystemInformation 3 · NtAllocateVirtualMemoryEx 3 · NtAllocateVirtualMemory 2 · NtFreeVirtualMemory 1.
- `+0x..` 는 **ret 자리** (WinDbg 콜스택 `caller+0x..` 와 같은 기준). `_calls.tsv` 의 `ret_off`. 시스템 콜 번호는 ntdll 스텁의 `mov eax, imm` 값 (26100.9457, `[정적]`).
- 사슬 줄임: `②…` = `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess`. `RtlInitializeHeapManager` 는 끝에서 `RtlHpInitializeHeapManager` 로 **꼬리 JMP** (`+0x1a0`) 해서, 그 아래 콜스택엔 `RtlInitializeHeapManager` 프레임이 안 보이고 `LdrpInitializeProcess +0x9f2` 가 바로 보인다.

| # | 시스템 콜 (번호) | 부르는 함수 `+call (ret)` · 사슬 | 클래스 / 인자 | 크기 | 횟수 | 타는 조건 | 확인 |
|---|---|---|---|---|---|---|---|
| 1 | `NtOpenKey` (0x12) | `RtlpOpenBaseImageFileOptionsKey +0x7b (+0x80)` ← `RtlpOpenImageFileOptionsKeyEx +0xa9 (+0xae)` ← `RtlpHpQueryGCTimerInterval +0x27 (+0x2c)` ← `RtlInitializeHeapManager +0x78 (+0x7d)` ← `②… +0x9ed` | IFEO **바탕 키** `\Registry\Machine\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options`, 접근 `9`, 속성 `0x240` | 핸들 1개 (결과를 전역 `rva 0x1ce430` 에 캐시) | 1 | `RtlpHpQueryGCTimerInterval` 에 이미지 이름이 있음. 이 길에서 **처음** 여는 곳이다: `LdrpInitializeExecutionOptions +0xa6` 이 매개변수 `Flags & 0x4000` 이면 IFEO 열기를 건너뛰고, `LdrpInitializeProcess +0x8f2` 는 `NtGlobalFlag & 0x1000` 이거나 `LdrpShouldCreateStackTraceDb` ≠ 0 일 때만이다 | `[정적]` + `[관찰]` P1 `Flags=0x4001`, P1a 캐시 `0x1ce430`=`0x10` |
| 2 | `NtOpenKey` (0x12) | `RtlpOpenImageFileOptionsKeyEx +0xe7 (+0xec)` (위와 같은 사슬) | 바탕 키 아래 이름 = 이미지 이름 마지막 조각 `test.exe`, 속성 `0x240`, 접근 `9` (열기만, R8B=0) | 핸들 1개 | 1 | #1 성공 뒤. test.exe 키가 **없으면** 실패 (기대 `0xC0000034`) 로 끝나고 아래 줄은 안 탐 | `[정적]` + 간접 `[관찰]` (GC 간격이 기본값 1000ms, #1 뒤 줄) |
| 3 | `NtQueryInformationProcess` (0x19) | `RtlpGetModifiedProcessCookie +0x22 (+0x27)` ← `RtlInitializeHeapManager +0x9d (+0xa2)` | 프로세스 `-1`, 클래스 **`0x24`** (`ProcessCookie`, 이름은 공개 헤더 기준 `[추측]` — PDB 열거형 없음), 반환 길이 칸 `NULL` | 출력 **4바이트** | 1 | 항상. 성공하면 쿠키 → `RtlpDisableBreakOnFailureCookie` 에 쓰지만 곧 0 으로 덮임 (아래 질문 2) | `[정적]` |
| 4 | `NtQuerySystemInformation` (0x36) | `RtlpInitializeLowFragHeapManager +0x84 (+0x89)` ← `RtlInitializeHeapManager +0x191 (+0x196)` | 클래스 **0** (`SystemBasicInformation`, `[정적] PDB`) | **0x40바이트** (버퍼 `+0x38` 의 1바이트 = `NumberOfProcessors` 만 씀) | 1 | 항상. 실패하면 `RtlpAffinityState` 가 1 로 남음 | `[정적]` + `[관찰]` `RtlpAffinityState`=2 |
| 5 | `NtQuerySystemInformation` (0x36) | `RtlHpInitializeHeapManager +0xa9 (+0xae)` ← (꼬리 JMP) `RtlInitializeHeapManager +0x1a0` ← `②… +0x9ed` | 클래스 **0x3e** (`SystemEmulationBasicInformation`, `[정적] PDB`) | **0x40바이트** (`+0x28` = `MaximumUserModeAddress` 만 씀) | 1 | 항상. **실패하면** `RtlHpHeapManagerStart` (`+0xd8`) 를 건너뛴다 → #6~#8 (큰 예약 둘) 이 안 생긴다 (이때도 `RtlHpHeapManagerInitialize` 는 이미 돌았음) | `[정적]` + `[관찰]` 예약 둘이 있음 = 성공 |
| 6 | `NtAllocateVirtualMemoryEx` (0x78) | `RtlpHpEnvAllocVA +0xdd (+0xe2)` ← `RtlCSparseBitmapStart +0xbb (+0xc0)` ← `RtlHpHeapManagerStart +0x2c (+0x31)` ← `RtlHpInitializeHeapManager +0xd8 (+0xdd)` | **예약**: 형 `0x102000` (`MEM_RESERVE \| MEM_TOP_DOWN`), 보호 `4`, 확장 인자 1개 (주소 조건 `{0,0,0}`) | **`0x2001000`** (32MB + 4KB) | 1 | #5 성공. `nbits=0xFFFFFFE` (= 사용자 공간 끝 `0x7FFFFFFF0000` ÷ 1MB × 2) → 32MB 비트맵 + 4KB 요약 쪽 | `[정적]` + `[관찰]` VAD `7ff5ed090–7ff5ef090` |
| 7 | `NtAllocateVirtualMemoryEx` (0x78) | `RtlpHpEnvAllocVA +0xdd (+0xe2)` ← `RtlCSparseBitmapStart +0x114 (+0x119)` ← 위와 같음 | **확정**: 형 `0x1000` (`MEM_COMMIT`, 0x40000000 비트는 안에서 지움), 보호 `4`, 확장 인자 0개 | `0x1000` (1쪽, 번지 = 예약 시작 + `0x2000000`) | 1 | #6 성공 뒤, 두 번째 조각 크기 ≤ `0x1000` 일 때 | `[정적]` + `[관찰]` VAD 확정 1쪽, `ctx1+0` = `7ff5ef090000` |
| 8 | `NtAllocateVirtualMemoryEx` (0x78) | `RtlpHpEnvAllocVA +0xdd (+0xe2)` ← `RtlCSparseBitmapStart +0xbb (+0xc0)` ← `RtlpHpVaMgrCtxStart +0x59 (+0x5e)` ← `RtlHpHeapManagerStart +0x51 (+0x56)` ← `RtlHpInitializeHeapManager +0xd8` | **예약**: `0x102000`, 보호 `4`, 확장 인자 1개 | **`0x100020000`** (4GB + 128KB) | 1 | #6·#7 뒤. `nbits=0x800000000`. **확정은 없다**: 두 번째 조각 `0x20000` > `0x1000` 이라 `+0x114` 가 안 불림 | `[정적]` + `[관찰]` VAD `7ff4ed070–7ff5ed08f`, 확정 0 |
| 9 | `NtQuerySystemInformation` (0x36) | `RtlpCreateHeap +0xe07 (+0xe0c)` ← `LdrpInitializeProcessHeap +0x191 (+0x196)` ← `②… +0xa4b` | 클래스 **0** (`SystemBasicInformation`) | **0x40바이트** (`+0x28` `MaximumUserModeAddress` 를 전역 `0x1ce6e8` 에 캐시, `0x1ce6f0` 에 `0x10000`) | 1 | 전역 `0x1ce6e8` 이 0 일 때 (이 프로세스에선 이 힙이 첫 호출). 실패하면 힙 만들기 실패 → `LdrpInitializeProcess +0xa55` 로그 → `0xc0000017` | `[정적]` + `[관찰]` `0x1ce6e8`=`7ffffffeffff` |
| 10 | `NtAllocateVirtualMemory` (0x18) | `RtlpCreateHeap +0x459 (+0x45e)` | 프로세스 `-1`, 형 `0x2000` (**예약**), 보호 `4` (`HEAP_CREATE_ENABLE_EXECUTE` 0x40000 이면 `0x40`) | **`0x100000 + n×0x10000`** (n = 0~31, 아래 질문 2) | 1 | 호출자가 힙 터(Base)를 안 줬을 때 (`Base = NULL`). `0x100000` = test.exe PE 헤더 `SizeOfHeapReserve` | `[정적]` + `[관찰]` 힙 VAD 1MB |
| 11 | `NtFreeVirtualMemory` (0x1e) | `RtlpSecMemFreeVirtualMemory +0x2a (+0x2f)` ← `RtlpCreateHeap +0x4a1 (+0x4a6)` | 형 `0x8000` (`MEM_RELEASE`), 번지 = 예약 시작, 크기 = **`n×0x10000`** | 앞쪽 무작위 몫만 풀어 줌 | 1 (n≠0 일 때) | n ≠ 0 (31/32). n=0 이면 안 부름 | `[정적]` (풀린 뒤라 사진엔 흔적 없음) |
| 12 | `NtAllocateVirtualMemory` (0x18) | `RtlpCreateHeap +0x512 (+0x517)` | 형 `0x1000` (**확정**), 보호 `4` | **`0x2000`** (8KB = 첫 커밋: `SizeOfHeapCommit 0x1000` 을 `+0x1fff & ~0x1fff` 로 올림) | 1 | #10 성공 뒤 | `[정적]` |

구간 **앞**에서 일어나 힙 종류에 영향을 주는 시스템 콜 (구간 밖이라 위 12개엔 안 셈):

| 시스템 콜 | 부르는 곳 | 클래스 / 인자 | 조건·결과 |
|---|---|---|---|
| `NtOpenKey` (0x12) | `RtlpHpApplyGlobalSegmentHeapConfigurations +0x68 (+0x6d)` ← `RtlSetGlobalHeapFeatures +0x17` ← `LdrpInitializeExecutionOptions +0x61e` ← `②… +0x531` | `\Registry\Machine\SYSTEM\CurrentControlSet\Control\Session Manager\Segment Heap`, 접근 `1`, 속성 `0x40` | **IFEO 를 건너뛰어도 항상 부름** (`Flags & 0x4000` 이어도 `+0x604` 로 가서 `RtlSetGlobalHeapFeatures` 는 부른다). 키가 없으면 여기서 끝. 있으면 `NtQueryValueKey` (+0x97, 클래스 2, 값 이름 `Enabled`, 4바이트 DWORD) 와 `NtClose` (+0xdc) |
| `NtQuerySystemInformation` (0x36) 클래스 `0xc5` (`SystemHypervisorSharedPageInformation`, `[정적] PDB`) | `LdrpInitializeProcess +0xb4d (+0xb52)` | 8바이트 | **구간(묶음 7) 안이지만 힙 뒤**. 요청한 함수 밖이라 본문은 안 봄. 전역 `RtlpHypervisorSharedUserVa` 에 씀 |

**보통 길에서 안 타는 시스템 콜** (요청서가 짚은 것 + 찾은 것):

| 시스템 콜 | 자리 | 타는 조건 | test.exe |
|---|---|---|---|
| `NtOpenPartition` (0x131) | `LdrpInitializeProcessHeap +0x214 (+0x219)` | `LdrpHeapPartitionName.Buffer ≠ 0` (= 매개변수 `HeapPartitionName` (`+0x420`) 의 Buffer (`+0x428`) 가 있음) | 안 탐: `HeapPartitionName` 빈 문자열 `[관찰]` P1. 전역 `LdrpHeapPartitionName` 0 `[관찰]` P1a |
| `NtQueryVirtualMemory` (0x23) ×2 | `RtlpCreateHeap +0xd60 (+0xd65)` (클래스 `0`) · `+0x1043 (+0x1048)` (클래스 `3`) | 호출자가 **힙 터 `Base ≠ NULL`** 을 줬을 때 (이미 있는 메모리 위에 힙을 만듦) | 안 탐: `RtlpCreateHeap(…, Base=0, …)` (`LdrpInitializeProcessHeap +0x189 XOR EDX,EDX`) |
| `NtQueryValueKey` (0x17) `GCInterval` (클래스 2) + `NtClose` (0xf) | `LdrQueryImageFileKeyOption +0xce`, caller `RtlpHpQueryGCTimerInterval +0x52`; `NtClose` `+0x82` | #2 에서 test.exe 키가 열렸을 때 | 안 탐 기대. `[관찰]` GC 간격 전역 `0x1ce278` = −10,000,000 (= 기본 1000ms × −10000) |
| `NtOpenKey` + `NtQueryValueKey` + `NtClose` (제품 종류 레지스트리) | `RtlGetNtProductType +0x5d` → `RtlpGetNtProductTypeFromRegistry` (`+0xd1` / `+0x138` / `+0xe5`), `\Registry\Machine\System\CurrentControlSet\Control\ProductOptions` 값 `ProductType` | `KUSER_SHARED_DATA.ProductTypeIsValid` (`+0x268`) = 0 && `PEB.SharedData` 가 null | 안 탐: `ProductTypeIsValid`=1, `NtProductType`=1 `[관찰]` P1a |
| `NtTraceEvent` | `RtlpHeapLogRangeCreate +0x7e`, `RtlInitializeCriticalSectionEx +0x118` 등 | `KUSER_SHARED_DATA.UserModeGlobalLogger` (`+0x380` 계열) ≠ 0 / `PEB.TracingFlags` 비트 | 안 탐: 모두 0 `[관찰]` P1a |
| `NtAllocateVirtualMemory` (커밋) | `RtlpInitializeHeapSegment +0x101 (+0x106)` | 머리 크기 + `0x50` 이 첫 커밋 끝을 넘을 때 | 안 탐: 첫 빈 블록 = 힙 `+0x740` < `+0x2000` `[관찰]` `FirstEntry=…f30740` |
| `NtCreateKey` / `NtSetValueKey` / `NtDeleteKey` | `RtlpOpenImageFileOptionsKeyEx +0x14a / +0x1ed / +0x253` | 만들기 모드 (R8B ≠ 0) | 안 탐 (GC 간격 조회는 열기만) |
| 세그먼트 힙 길 시스템 콜들 (`NtAllocateVirtualMemoryEx`, `NtQuerySystemInformationEx` …) | `RtlpHpHeapCreate` 아래 | `RtlpHpHeapFeatures & 1` | 안 탐 (아래 질문 1·3) |
| `NtQueryVirtualMemory`·재시도 `NtFreeVirtualMemory` | `RtlpSecMemFreeVirtualMemory +0x8b / +0xbf` | 첫 `NtFreeVirtualMemory` 가 `0xC0000045` 이고 `RtlpSecMemListHead` 가 안 비었을 때 | 안 탐 (목록은 자기 자신을 가리키는 빈 상태가 정적 초기값) |

## 한눈에 (핵심)

- **test.exe 프로세스 힙은 NT 힙이다** (세그먼트 힙 아님). 정적: 가르는 조건 9개 중 8개가 test.exe 값에선 "아님" (④ `pShimData` 안 두 바이트만 못 봄). 사진: P1a 덤프 힙 `+0x10` = `0xffeeffee` (NT 힙 `SegmentSignature`; 세그먼트 힙이면 `0xddeeddee`), `+0x14`=2, `RtlpHpHeapFeatures`=0. 요청서 3번 갈림길은 **전부 "안 탄다"** 로 닫힌다 (아래 표).
- **큰 예약 둘은 힙 관리자 것이다 — 짐작이 맞다.** 32MB + 4KB 예약 (확정 1쪽) 과 약 4GB 예약 (정확히 `4GB + 128KB`, 확정 0) 은 `RtlHpHeapManagerStart` 가 만든 "희소 비트맵" 둘이다. 정적으로 계산한 크기 `0x2001000` / `0x100020000` 이 사진 VAD 쪽 수와 **정확히 같다** (`0x2001` 쪽 / `0x100020` 쪽). 덤프의 `RtlpHpHeapManager` 칸 값도 계산과 맞는다.
- **PEB `+0x30 ProcessHeap` 을 쓰는 줄 = `LdrpInitializeProcessHeap +0x1a2`**. `+0xe8 NumberOfHeaps` 는 두 번 쓴다 (`RtlInitializeHeapManager +0xe7` 에서 0, `RtlpProcessHeapsInsert +0xb9` 에서 1), `+0xec`=0x10 (`+0x10a`), `+0xf0`=`RtlpPebHeapListStaticBuffer` (`+0xf5`), 그 `[0]` 에 힙 주소 (`RtlpProcessHeapsInsert +0xca`). 이 힙을 만드는 **첫 할당은 힙 자신 안에서** 한다 (`RtlpProcessHeapsInsert` 가 `PEB.ProcessHeap` 이 0 이면 새 힙에서 `RtlAllocateHeap`).
- **`RtlCreateHeap` 은 `RtlpCreateHeap` 껍데기** (예, 7번째 인자 0 을 붙여 넘김). 하지만 `RtlInitializeHeapManager` 없이 부르면 안 된다 — 프로세스 힙 목록 머리·락·키·난수 상태가 없어서 `RtlpProcessHeapsInsert` 에서 널을 읽는다.
- **정적으로 못 닫은 것**: `PEB.pShimData` (`25996d10000`, 0 아님) 안 두 바이트 (`+0x11b2`·`+0x11aa`), test.exe IFEO 키 열기 결과, 힙 앞쪽 무작위 몫 n, ProcessCookie 반환. 전부 동적 질문 (D1·D4·D5·D9). 결과(NT 힙) 자체는 사진이 이미 정했다.

## 0. 메타

- 요청서: `_workspace/sast/40_process-heap.md` (03_sprint, 대기열 Q78a, 예정 run46)
- 모듈 / 바이너리: ntdll (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/ntdll.dll`), pdb guid_age = `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`, baggage.md 기준과 같음)
  - **이미 analyze 끝난 프로젝트** (`.gpr` + `_index.tsv` + `SetPdb:` 줄). **다시 analyze 안 했다.** 도는 Ghidra 프로세스 없음.
- 뽑은 함수 (모두 `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`; 읽기만, 요청서 기준 + 길 따라 더):
  - 1차 (요청서 표, depth 1): 24개 → 115개 (정규식 `^(LdrpInitializeProcessHeap|RtlInitializeHeapManager|RtlpCreateHeap|RtlSetHeapInformation|RtlHpGlobalsInitialize|RtlpHpOptIntoSegmentHeap|RtlpHpInitializePerfPolicies|RtlpHpQueryGCTimerInterval|RtlpOpenImageFileOptionsKeyEx|RtlpGetModifiedProcessCookie|RtlpInitializeLowFragHeapManager|RtlHpInitializeHeapManager|RtlHpHeapManagerInitialize|RtlHpHeapManagerStart|RtlpHpEnvAllocVA|RtlpCreateHeapEncoding|RtlpInitializeHeapSegment|RtlpPopulateListIndex|RtlpProcessHeapsInsert|RtlpHpConvertCreationFlags|RtlpHpHeapCreate|RtlGetSuiteMask|SbSelectProcedure|RtlpHpLfhContextEnable)$`). `RtlCreateHeap`·`RtlCreateTagHeap` 는 depth 0 으로 따로.
  - 길을 따라 더 뽑음 (depth 0 또는 1, 이유는 아래 "더 뽑은 것"): `RtlCSparseBitmapInitialize` `RtlpHpVaMgrCtxInitialize` `RtlpHpCustomVaCallbacksRegistrarInitialize` `RtlpHpVaMgrCtxAllocatorReference` `LdrpHandleUserCopyAttributeList` `RtlSetGlobalHeapFeatures` `RtlpHpSetLowFragHeapGlobalFlags` `RtlpHpApplyGlobalSegmentHeapConfigurations` `LdrQueryImageFileExecutionOptions` `LdrQueryImageFileExecutionOptionsEx` `RtlQueryApplicationKeyOption` `RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit` `RtlpCreateUCREntry` `RtlpInsertFreeBlock` `RtlpHeapAddListEntry` `RtlpGetHeapProtection` `RtlpHpHeapCheckCommitLimit` `RtlGetCurrentServiceSessionId` `RtlpCreateLowFragHeap` `RtlpGetNtProductTypeFromRegistry`.
  - 결과: `.asm`/`.c` 342개 (전 225개), `_calls.tsv` 1,625 → 2,165줄 (줄어든 줄 없음; 백업 `_calls.pre-40.tsv`). `_refs` 17개 새로 (`RtlpDebugPageHeapTable` `0x1ce268` `RtlpHpHeapFeatures` `RtlpLowFragHeapGlobalFlags` `RtlpDisableHeapLookaside` `RtlpHeapKey` `RtlpProcessHeaps` `RtlpProcessHeapsLock` `RtlpPebHeapListStaticBuffer` `RtlpHpHeapGlobals` `0x1ce6e8` `RtlpHeapErrorHandlerThreshold` `RtlpDisableBreakOnFailureCookie` `0x1ce430` `RtlpLFHKey` `RtlpLargestLfhBlock` `RtlpHpLfhPerfFlags` `RtlpHpHeapVaCallbacksRegistrar` `RtlpAffinityState` `RtlpDefaultHeapDebuggingOptions` `RtlpHpEnvHandle` `RtlpLowFragHeapRandomData` `RtlpSecMemListHead` `RtlpHpAppCompatFlags` `0x1d21a0` `RtlpHeapFailureInfo` `RtlpHpHeapManager`).
- PDB 구조체 칸 (`_PEB` `_HEAP` `_HEAP_SEGMENT` `_HEAP_COUNTERS` `_HEAP_ENTRY` `_HEAP_LIST_LOOKUP` `_SEGMENT_HEAP` `_RTL_USER_PROCESS_PARAMETERS` `_TEB`, 열거형 `_SYSTEM_INFORMATION_CLASS` `_PS_MITIGATION_OPTION`) 는 `llvm-pdbutil-14 dump -types` 를 스크래치 폴더에 떠서 맞췄다 (커밋 안 함).
- **버전 확인: 일치.**
  - 스킬 표: `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x16c → LdrpInitializeProcess` — `_calls.tsv` 에서 일치.
  - 정적 해독 ↔ 사진 (P1a 덤프, `ms/logs/04_baggage-read_P1a.txt`) 대조 **20곳 이상 일치**: 힙 `+0x38`=0xff · `+0x40`=`…f30740` · `+0x48`=`…2f000` · `+0x50`=0xf6 · `+0x54`=1 · `+0x7c`=0x100000 · `+0x10`/`+0x14`, `RtlpHpLfhPerfFlags`=`0x2403`, `RtlpAffinityState`=2, `0x1d21d8`=`0x4000`, `RtlpHpHeapManager+0`=`&RtlpHpHeapGlobals`, 희소 비트맵 `+0x10`=`0xffffffe`/`0x800000000`, `+0x33`=0x0c, `+0x34`=0x10, VAD 쪽 수 셋 (아래 질문 6). 스크립트 오프셋 계산과 바이너리 둘 다 믿어도 된다.
- 스크립트 문제: **없음** (스크립트는 안 고쳤다). 도구 제안 하나는 맨 끝.
- 읽을 때 주의: Ghidra `.c` 는 인자 번호·`param_N` 이 어긋난 곳이 있었다 (`RtlpHpEnvAllocVA` 의 스택 인자, `RtlpCreateHeap` 의 큰 지역 변수). 흐름·오프셋은 전부 `.asm` 줄로 맞췄다. 디컴파일 코드는 이 결과서에 안 붙였다.

## 질문별 답 [정적]

### 1. 호출 사슬과 순서

- 답 (실행 순서. `+call (ret)`, 조건은 괄호). 근거는 `ms/disassemble/ntdll/<함수>.asm` 의 같은 오프셋 줄, `_calls.tsv`.

| 순서 | 함수 `+call (ret)` | 하는 일 | 조건 |
|---|---|---|---|
| ① | `LdrpInitializeProcess +0x9ed (+0x9f2) RtlInitializeHeapManager(&이미지 이름)` | 아래 ①-a~①-h. 인자 = 지역 `UNICODE_STRING` (= `ProcessParameters.ImagePathName`, 앞 `\??\` 는 `+0x2d5~+0x31b` 가 뗌) | 항상 (음수면 `+0x9f4 JS` 로 함수 끝) |
| ①-a | `RtlInitializeHeapManager +0xd (+0x12) RtlHpGlobalsInitialize` | `RtlpHpHeapGlobals` (0x80B) 를 0 으로 밀고 난수 키·상태 채움 (질문 5 표) | 항상 |
| ①-b | `+0x28 (+0x2d) RtlpHpOptIntoSegmentHeap` | 세그먼트 힙으로 갈지 정함 (질문 3 표). 반환 ≠ 0 이면 `RtlpHpHeapFeatures \|= 1` (`+0x31`) | 항상. test.exe → 0 |
| ①-c | `+0x56 (+0x5b) SbSelectProcedure` → `+0x67 (+0x6c) indirect-cfg` | 앱 호환 스위치 함수 고르고 부름 | **①-b 반환 ≠ 0 일 때만** (`+0x2f JZ → +0x70`). 안 탐 |
| ①-d | `+0x70 (+0x75) RtlpHpInitializePerfPolicies` | LFH 성능 플래그 `RtlpHpLfhPerfFlags` 정함 | 항상 |
| ①-e | `+0x78 (+0x7d) RtlpHpQueryGCTimerInterval` → `RtlpOpenImageFileOptionsKeyEx` | GC 간격 (기본 1000ms) 을 IFEO 에서 조회 — **여기서 NtOpenKey 2번** | 항상 (이미지 이름이 있으니) |
| ①-f | `+0x9d (+0xa2) RtlpGetModifiedProcessCookie` | NtQueryInformationProcess 0x24 | 항상 |
| ①-g | `+0xe7 ~ +0x10a` PEB 칸 3개 직접 쓰기, `+0x103~+0x123` `RtlpProcessHeaps` 머리, `+0x12a (+0x12f) RtlInitializeCriticalSectionEx(&RtlpProcessHeapsLock, 0, 0x10000000)`, `+0x154~+0x16d` `RtlpHeapKey`, `+0x174 (+0x179) RtlGetSuiteMask`, `+0x191 (+0x196) RtlpInitializeLowFragHeapManager` | PEB 힙 목록 준비 (`NumberOfHeaps=0`, `ProcessHeaps=RtlpPebHeapListStaticBuffer`, `MaximumNumberOfHeaps=0x10`), 락, 키, LFH 관리자 | 항상 |
| ①-h | `+0x1a0 tail RtlHpInitializeHeapManager` → `+0x3e (+0x43) RtlHpHeapManagerInitialize` → `+0xa9 (+0xae) NtQuerySystemInformation(0x3e)` → `+0xd8 (+0xdd) RtlHpHeapManagerStart` | 힙 관리자 자료 구조 0 으로 만들기 → 사용자 공간 끝 알아내기 → 큰 예약 둘 (희소 비트맵 `+0x2c`, VA 관리자 `+0x51`) | `Start` 는 시스템 콜 성공 때만 |
| ② | `LdrpInitializeProcess +0xa34 (+0xa39) RtlSetHeapInformation(0, 1, 0, 0)` | 클래스 1 = "손상 때 종료" 켜기 (`RtlpDisableBreakOnFailureCookie = 0`, 시스템 콜 없음) | `LdrSystemDllInitBlock +0xa0` 첫 8바이트의 비트 12~13 이 1 (`+0xa1a~+0xa28`). test.exe → 0 → **안 탐** `[관찰]` |
| ③ | `+0xa4b (+0xa50) LdrpInitializeProcessHeap(LoadConfig, NT 헤더, 매개변수, &PEB.ProcessHeap)` | 힙 인자 정리 → `RtlpCreateHeap` → PEB `ProcessHeap` 쓰기 | 항상. 음수면 `+0xa55` 로그 후 `0xc0000017` |
| ③-a | `LdrpInitializeProcessHeap +0x191 (+0x196) RtlpCreateHeap(플래그, 0, 예약, 커밋, 0, &매개변수, 0)` | 힙 만들기 본체 | 항상 |
| ③-b | `+0x1a2` `MOV [R15],RAX` → **PEB `+0x30` ProcessHeap** | 힙 주소 기록 | `RtlpCreateHeap` 가 0 이 아닐 때 (0 이면 `+0x280` → `0xc0000017`) |
| ③-c | `+0x264 (+0x269) RtlpHpLfhContextEnable(힙+0x340, &0x1d21d8)` | 세그먼트 힙의 LFH 켜기 | 힙 `+0x10` 이 `0xddeeddee` (세그먼트 힙) 일 때만. 안 탐 |

- `RtlpCreateHeap` 안 순서 (NT 힙 길): `+0x116` 페이지 힙 전역 검사 → `+0x12b` 플래그 비트 8 → `+0x135` `RtlpHpHeapFeatures & 1` → (NT 길) `NtGlobalFlag` 비트를 힙 플래그로 합침 (`+0x171~+0x1b9`) → `PEB +0xc8/+0xd0/+0xe0/+0xd8` 읽기 (`+0x1d0/+0x1ea/+0x204/+0x21e`) → `+0xe07 NtQuerySystemInformation(0)` (한 번) → 크기 올림 → `+0x3df~+0x3f5` 무작위 몫 → `+0x459 NtAllocateVirtualMemory` → (n≠0) `+0x4a1 RtlpSecMemFreeVirtualMemory` → `+0x512 NtAllocateVirtualMemory` (커밋) → 머리 칸 채우기 (`+0x5a1 ~`) → `+0x5ff RtlpCreateHeapEncoding` → `+0x6df RtlInitializeCriticalSectionEx` (힙 락) → `+0x746 RtlpInitializeHeapSegment` → `+0x797 RtlpPopulateListIndex` → 한계값 칸들 (`+0x7a1~+0x95c`) → `+0x915 RtlAllocateHeap(힙, 0x80000a, 0x100)` → `+0x96f RtlpProcessHeapsInsert` → 로그 검사 (`+0x97c~`, 아래 질문 2 표) → `+0x9e0` `CompatibilityFlags &= 0x7fffffff`.
- **`RtlCreateHeap` (`0xaa330`, 39바이트) 는 `RtlpCreateHeap` 껍데기인가?** → **예.** `RtlCreateHeap +0x1d (+0x22) RtlpCreateHeap` 한 번뿐. 같은 6개 인자 (`Flags`, `HeapBase`, `ReserveSize`, `CommitSize`, `Lock`, `Parameters`) 를 그대로 넘기고, 7번째 인자 (스택 `+0x30`, `RtlCreateHeap +0x4 AND [RSP+0x30],0`) 에 0 을 붙인다. 7번째는 `RtlpProcessHeapsInsert` 의 노드 플래그로 간다 (`param_7 & 3`). `LdrpInitializeProcessHeap` 도 7번째에 0 (`+0x17f`).
- **`RtlCreateHeap` 만 따로 부르면 안 되는 이유가 있나?** → **예.** `RtlpProcessHeapsInsert` (`RtlpCreateHeap +0x96f`, 세그먼트 길은 `+0xc35`) 가 `RtlInitializeHeapManager` 가 만든 것에 기댄다:
  - 프로세스 힙 목록의 머리·꼬리 (`RtlpProcessHeaps` `+0x0`/`+0x8`, `RtlInitializeHeapManager +0x116/+0x123`). 안 만들면 `RtlpProcessHeapsInsert +0x79` 가 꼬리 포인터 (0) 를 따라가서 접근 위반. 꼬리가 자기 머리를 안 가리키면 `+0x91 INT 0x29` (fast fail 3).
  - 락 `RtlpProcessHeapsLock` (`+0x12a`) — `+0x60 RtlEnterCriticalSection` 에서 씀.
  - `PEB.ProcessHeaps`/`MaximumNumberOfHeaps` (`+0xf5`/`+0x10a`) — `+0xca` 가 `**(PEB+0xf0) = 힙` 으로 정적 배열에 씀.
  - 난수 상태 (`RtlpHpHeapGlobals +0x40`, `RtlHpGlobalsInitialize`) — 안 만들면 0 에서 안 움직여 무작위 몫·`Encoding` 이 전부 0 (죽지는 않지만 보안 값이 없음). `RtlpHeapKey` (`CommitRoutine` 인코딩), 세그먼트 길은 `RtlpHpHeapManager` (큰 예약 둘).
  - 근거: `RtlpProcessHeapsInsert.asm` +0x79~+0x91, +0xb9~+0xca; `RtlInitializeHeapManager.asm` +0xe7~+0x16d.
  - 힙을 **더** 만들 땐 반대로 `PEB.ProcessHeap` 이 있어야 한다 (`+0x29 [PEB+0x30]` 읽고 `+0x3e RtlAllocateHeap(그 힙, 8, 0x30)` 으로 노드를 거기서 받음). 첫 힙만 0 이라서 자기 자신에서 받는다.
- **NT 힙 길 / 세그먼트 힙 길 갈림 조건과 test.exe**:
  - 갈림은 `RtlpCreateHeap` 안에서 "세그먼트 힙용 매개변수 구조체 포인터" (`puVar7`) 가 정해졌나 (디컴파일 `if (puVar7 == 0) {NT 길} else {세그먼트 길}`) 이다. 세그먼트 길 = `+0xbf2 (+0xbf7) RtlpHpConvertCreationFlags` → `+0xc18 (+0xc1d) RtlpHpHeapCreate` → `+0xc35 (+0xc3a) RtlpProcessHeapsInsert` (실패하면 `+0xc41 RtlpHpHeapDestroy`) → `+0xc59/+0xc73 RtlAcquire/ReleaseSRWLockExclusive(&RtlpHpStackTrackingContext)` → `+0xc7b RtlpHpHeapLoggingStateSync`.
  - 정해지는 경우는 둘: **(가)** 호출자가 플래그 `0x100` (`HEAP_CREATE_SEGMENT_HEAP`, SDK 이름) 을 줌 (`+0x12b BT EDI,8`). 이때 "자라는 힙 (`0x2`) 이고 `Base = NULL` 이고 `Lock = NULL`" 이 아니면 실패 (NULL 반환). **(나)** 플래그 `0x100` 이 없어도 `RtlpHpHeapFeatures & 1` (`+0x135`) 이고 `0x2` 가 있고 `Base = NULL` 이고 `Lock = NULL` 이고 (`Parameters` 가 없거나 `SegmentReserve`·`SegmentCommit`·`VirtualMemoryThreshold`·`CommitRoutine` 이 0) 이면 **NT 플래그 요청을 세그먼트 힙으로 바꿔 준다.**
  - `LdrpInitializeProcessHeap` 쪽: `RtlpHpHeapFeatures & 1` (`+0x139`) 이면 플래그 `0x102` + 힙 매개변수 `{0x500003, 2, 파티션 쪽 값, -1}` 를 직접 만들어 넘김 (`+0x142~+0x15c`). exe `LoadConfig.ProcessHeapFlags` (`+0x48`, `& 0xffff0fff`) 에 `0x100` 이 있어도 세그먼트로 간다.
  - **test.exe = NT 힙 길.** 정적: `RtlpHpHeapFeatures & 1` 이 꺼짐 (질문 3), LoadConfig `ProcessHeapFlags`=0 (`sample/bin/test.exe` 헤더로 확인) → 플래그 `2`, `Base = 0`, `Lock = 0`. 사진: P1a 힙 `+0x10` = `0xffeeffee`, `+0x14` = 2 `[관찰]`.

### 2. 보통 길 시스템 콜

- 답: **맨 앞 표** 가 줄마다 (함수+오프셋, 이름, 클래스, 크기, 횟수, 조건) 를 담았다. 요청서가 짚은 것만 따로 답한다.
- **`NtQuerySystemInformation` 클래스 0**: 0x40바이트, **이 구간에서 2번** (#4 `RtlpInitializeLowFragHeapManager +0x84`, #9 `RtlpCreateHeap +0xe07`). 요청서 짐작 (0x40바이트) 맞음 `[정적]`. #9 는 전역 `0x1ce6e8` 캐시가 0 일 때만 (이 프로세스에서 `RtlpCreateHeap` 의 첫 호출). `RtlpCreateHeap +0xe07` 의 클래스도 0 (`XOR ECX,ECX`). 이 구간 밖에서 `RtlpCreateHeap` 를 또 부르는 사람 (`LdrpInitializeTlsHeap`, `CsrpConnectToServer` 등) 은 캐시가 있어 이 호출이 없다.
- **클래스 `0x3e`** (`SystemEmulationBasicInformation`): 0x40바이트, 1번 (#5). **실패하면 건너뛰는 것**: `RtlHpHeapManagerStart` (`+0xb0 JNS` 가 안 맞으면 `+0xb2` 에서 바로 끝). 큰 예약 둘과 희소 비트맵 시작이 없다. 힙 관리자는 초기화만 되고 시작이 안 된 채로 남는다 (NT 힙 프로세스 힙은 영향 없음, 세그먼트 힙을 나중에 만들면 실패).
- **`NtQueryInformationProcess` `0x24` (4바이트)**: 맞음. `RtlpGetModifiedProcessCookie` 가 (프로세스 `-1`, 클래스 `0x24`, 버퍼 4바이트, 반환 길이 `NULL`) 로 부름. 쿠키 `c` 를 `((c × 0x7fffffed + 0x7fffffc3) mod 0x7fffffff)` 로 섞어서 `RtlpDisableBreakOnFailureCookie` 에 씀 (`RtlInitializeHeapManager +0xa2`). 실패하면 0. **이 값은 곧 0 으로 덮인다**: `RtlpDisableHeapLookaside` 비트 0 이 꺼져 있으면 (기본) `+0xe0` 이 0 을 쓴다. 그래서 시스템 콜은 하지만 값은 남지 않는다 (`[관찰]` P1a 0, baggage 7.3 "끝까지 0" 과 맞음).
- **IFEO `NtOpenKey`** (`RtlpOpenImageFileOptionsKeyEx`):
  - 바탕 키 (#1): 이름은 `RtlpEmptyString+0x20` 자리의 `UNICODE_STRING` (`rva 0x1733f8`) = `\Registry\Machine\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options`, 접근 `9` (`KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS`), 속성 `0x240` (`OBJ_KERNEL_HANDLE | OBJ_CASE_INSENSITIVE`, SDK 값). 전역 `0x1ce430` 에 `CMPXCHG.LOCK` 로 캐시 (`RtlpOpenBaseImageFileOptionsKey +0x90`; 남이 먼저 넣었으면 내 핸들을 `NtClose` `+0x9e`).
  - exe 키 (#2): 뿌리 = 바탕 키 핸들, 이름 = `ImagePathName` 의 **마지막 `\` 뒤** (`test.exe`, `RtlpOpenImageFileOptionsKeyEx +0x69~+0x7e`), 접근 `9`, 속성 `0x240`. 열기만 (`R8B=0`).
  - **키가 없을 때의 길**: `NtOpenKey` 가 음수 (기대 `0xC0000034`) → `+0x100 JNS` 가 안 맞아 `+0x106` 에서 그대로 반환 → `RtlpHpQueryGCTimerInterval +0x2e JS +0x75` 로 가서 기본값 1000ms 를 돌려주고 (`+0x75 CMP [RSP+0x48],0` 핸들 0 이라 `NtClose` 도 안 함). 키가 **있으면** `RtlpProcessIFEOKeyFilter` (`+0x282`) 가 필터 키를 따라가고, `LdrQueryImageFileKeyOption` 이 `GCInterval` (REG_DWORD 4바이트, `NtQueryValueKey` 클래스 2) 를 읽어 ×1000 (`+0x5f IMUL`) 하고, 마지막에 `NtClose`.
  - test.exe 가 **이 열기를 처음 한다** 는 근거: 매개변수 `Flags` 가 `0x4001` (`[관찰]` P1) 이라 `LdrpInitializeExecutionOptions +0xa6 TEST [RAX+8],0x4000 → JNZ +0x604` 가 IFEO 를 건너뛴다 (비트 `0x4000` 은 "IFEO 키 없음" 표시로 보임, SDK 이름 `RTL_USER_PROC_IMAGE_KEY_MISSING` `[추측]`). `LdrpInitializeProcess +0x8f2` 는 `NtGlobalFlag & 0x1000` 도 `LdrpShouldCreateStackTraceDb` 도 아니면 안 탄다 (`[관찰]` `NtGlobalFlag`=0, `LdrpShouldCreateStackTraceDb` 는 baggage 7.3 "끝까지 0"). 지도에서 `RtlpOpenImageFileOptionsKeyEx` 를 부르는 길은 이 둘과 `RtlpHpQueryGCTimerInterval` 뿐 (`LdrQueryImageFileExecutionOptionsEx` 는 `LdrpInitializeProcess +0x8f2` 아래).
- **`NtAllocateVirtualMemory` / `…Ex` / `NtFreeVirtualMemory`** (표 #6~#8, #10~#12):
  - 힙 터 (NT 힙): 예약 크기 = `SizeOfHeapReserve` (test.exe 헤더 `0x100000`) 를 `+0x1fff & ~0x1fff` (8KB 올림) + **무작위 몫**. 커밋 = `SizeOfHeapCommit` (`0x1000`) 을 같은 식으로 올려 `0x2000`. 자라는 힙 (`0x2`) 이고 `Base=NULL` 이면 세그먼트 플래그를 2 로 하고 `LastValidEntry` 쪽에 4KB 를 남긴다 (`+0xa28 MOV EBX,0x1000`, `+0xa2d`). `예약 − 0x1000 < 커밋` 이면 예약을 `(예약 + 0x10fff) & ~0xffff` 로 더 올리는데 (`+0xa3c ~ +0xa4c`) test.exe 는 `0xff000 ≥ 0x2000` 이라 해당 없음.
  - **무작위 몫**: `n = (상태 × −3) & 0x1f` → 몫 = `n × 0x10000` (0 ~ 0x1f0000). 상태 = **`RtlpHpHeapGlobals +0x40` (`0x1cdf00`) 의 xorshift64\* 난수** 를 한 칸 돌린 값 (`+0x3df CMPXCHG.LOCK`, 시프트 12·25(왼)·27, 곱 `0x2545f4914f6cdd1d`). 이 상태는 `RtlHpGlobalsInitialize` 가 심는다: 씨앗 = `TEB.ClientId.UniqueThread` (`GS:[0x48]`) XOR `((KUSER_SHARED_DATA.TickCountMultiplier(+0x4) × 0x7ffe0320 의 8바이트) >> 24)` → `RtlRandom` 두 번 (`+0x4c`, `+0x61`) 을 이어 붙여 `&RtlpHpHeapGlobals` 와 XOR (0 이면 1). 시스템 콜 없이 유저 코드·KUSER 값만 쓴다. 그 뒤 키들이 같은 상태를 계속 돌린다 (`RtlpHpHeapGlobals+0x0/+0x8` 두 번 · `RtlpHeapKey` · `RtlpLFHKey` 각 한 번 → `RtlpCreateHeap` 의 몫 → `RtlpCreateHeapEncoding` 두 번).
  - 예약은 `RESERVE 0x100000 + 몫` 한 덩어리로 받고 (`#10`), n≠0 이면 **앞쪽 `몫` 만큼 `MEM_RELEASE` 로 풀어** (`#11`) 힙 시작 주소를 흔든다. 그래서 사진 VAD 가 1MB 하나이고 앞 구멍이 없다.
  - 힙 관리자 큰 예약 둘: 표 #6~#8. 계산: `RtlCSparseBitmapStart(비트 수 N)` → 데이터 쪽 `r1 = ((N+7)>>3)` 을 4KB 올림, 요약 쪽 `r2 = (((r1>>12)+7)>>3)` 을 4KB 올림, 한 번에 `r1+r2` 를 `MEM_RESERVE|MEM_TOP_DOWN` 로 예약. `r2 ≤ 0x1000` 이면 요약 쪽 1쪽만 `MEM_COMMIT`. ① `N=0xFFFFFFE` (사용자 공간 끝 `0x7FFFFFFF0000` ÷ 1MB × 2) → `r1=0x2000000`, `r2=0x1000` → 예약 `0x2001000`, 확정 `0x1000`. ② VA 관리자 `N = 0x20 × 8 × ((0x7FFFFFFEFFFF>>20)+1) = 0x800000000` → `r1=0x100000000`, `r2=0x20000` → 예약 `0x100020000`, 확정 없음. 번지 = 위에서부터 (TOP_DOWN) ①이 위, ②가 바로 아래.
- **`LdrpInitializeProcessHeap +0x214 NtOpenPartition`**: 조건 = 전역 `LdrpHeapPartitionName.Buffer` (`rva 0x1d2968`) ≠ 0 (`+0x100 CMP`). 처음엔 0 이라 `+0x117~+0x11f` 가 매개변수 `HeapPartitionName` (`+0x420`, 16바이트) 을 전역에 복사하고 그 `Buffer` (`+0x428`) 가 0 이 아니면 `+0x133 JNZ +0x1dd` 로 간다. 간 뒤 `NtOpenPartition(&핸들, 0x1f0003, {len 0x30, 이름=LdrpHeapPartitionName}, …)`; 실패면 로그 `Cannot open partition.` 후 반환 (`+0x246`), 성공이면 `HeapMemoryTypeMask` (`+0x440`) 검사 (0 이면 8, `& 0x1c` 가 0 이면 로그 `Invalid partition heap page types.` → `0xc000000d`). test.exe 는 안 탐 `[관찰]` (`HeapPartitionName ""`).
- **`RtlpCreateHeap +0xd60 / +0x1043 NtQueryVirtualMemory`**: 조건 = `Base ≠ NULL` (`+0xd60` 는 `Base` 영역 조회, 클래스 0; `+0x1043` 은 확정된 영역이면 클래스 3 으로 크기 조회, `CommitRoutine` 이 없을 때). test.exe 는 `Base=0` → 안 탐.
- 크기·횟수·조건은 맨 앞 표. 구간 안 NtOpenKey 는 **2번** (+ 구간 앞 Segment Heap 레지스트리 1번).

### 3. 갈림길

| 갈림길 | 조건 (위치) | test.exe 가 가는 쪽 | 근거 |
|---|---|---|---|
| **세그먼트 힙 / NT 힙을 고르는 조건 9개** (`RtlpHpOptIntoSegmentHeap`, 순서대로, 하나라도 맞으면 거기서 끝) | | **전부 "아님" → 반환 0 → NT 힙** | |
| ① 전역 "세그먼트 힙 끔" | `+0x34 TEST AL,8` — `RtlpLowFragHeapGlobalFlags` (`rva 0x1d2218`) 비트 3 → 0 반환 | 꺼짐 | P1a 값 `1` (비트 3·4 없음) `[관찰]`. 이 비트를 켜는 곳: `RtlpHpApplyGlobalSegmentHeapConfigurations` (레지스트리 `Segment Heap\Enabled` = 0), `RtlSetGlobalHeapFeatures` (IFEO `HeapFeatures` 0x...2) |
| ② 전역 "세그먼트 힙 켬" | `+0x44 TEST AL,0x10` 비트 4 → 1 반환, `*param_2 = 비트 5` | 꺼짐 | 위와 같음 (레지스트리 `Enabled ≠ 0` 이면 `\|= 0x10`, `Enabled & 2` 이면 `\|= 0x30`) |
| ③ 만들 때 받은 속성 (**이름 없는 전역 `rva 0x1ce268`**) | `+0x56 [0x1ce268] & 3 == 1` → 1 반환, `*param_2 = 1` | 0 | 쓰는 곳 = **`LdrpHandleUserCopyAttributeList +0x82`** 하나 (`_refs/0x1ce268.tsv`; 읽는 곳 = `RtlpHpOptIntoSegmentHeap +0x56` 하나). 그 함수는 `LdrpInitializeProcess +0x3d9 (+0x3de)` 에서 불리고 (**힙 관리자보다 앞**), 매개변수 `AttributeList` (`+0x448`) 의 항목 중 종류 `0xa0022` 인 것의 가리키는 8바이트 (0 또는 1 만 허용, 그 밖이면 `0xc000000d`) 를 통째로 저장한 뒤 `AttributeList` 를 0 으로 지운다. test.exe: `AttributeList` null `[관찰]` P1, `[0x1ce268]`=0 `[관찰]` P1a |
| ④ `PEB.pShimData` | `+0x184~+0x1a0` — `pShimData(+0x2d8) ≠ 0` 이고 `[pShimData+0x11b2] & 4` 이면: `[pShimData+0x11aa] & 0x40` 이 있으면 1 (`*param_2=1`) 아니면 **0 반환 (여기서 끝, 뒤 조건 안 봄)** | `pShimData` ≠ 0 (VAD `25996d10–25996d11`, 2쪽) — 두 바이트 값은 **못 봄** (D1). 결과는 NT 힙으로 닫힘 | `[관찰]` `PEB+0x2d8`=`25996d10000` (P1a). 바이트는 `[공백]` |
| ⑤ 스위트 마스크 | `+0x1aa RtlGetSuiteMask` 비트 16 → 1 반환 (별도로 `RtlInitializeHeapManager +0x174~+0x186` 도 같은 비트로 `RtlpLowFragHeapGlobalFlags \|= 4`, `RtlpLargestLfhBlock=0x400`) | 꺼짐 | `KUSER_SHARED_DATA.SuiteMask`=`0x110` `[관찰]` P1a. `RtlGetSuiteMask` 는 `PEB.SharedData`(silo) 가 null 이면 `0x7ffe02d0` (KUSD `+0x2d0`) 를 읽을 뿐 레지스트리는 안 읽음 (질문 4) |
| ⑥ 매니페스트 `heapType` | `+0x1e6 RtlQueryActivationContextApplicationSettings(…SMI/2020/WindowsSettings, "heapType")` 가 `"SegmentHeap"` (`_wcsnicmp` `+0x200`) → 1 반환 | 아님 | **`sample/bin/test.exe` 에 리소스 디렉터리 자체가 없다** (PE 헤더 확인) → 매니페스트 없음. 게다가 이 시점 `PEB.ActivationContextData` (`+0x2f8`) 는 null (P1a 에도 null `[관찰]`) → 읽을 데이터가 없어 음수 반환 `[추측]` (D3) |
| ⑦ 패키지 앱 | `+0x20f TEST [PEB+3],0x10` (`IsPackagedProcess`, PDB `_PEB.BitField` 비트 4) → `RtlQueryPackageClaimsEx(-4, …)` (`+0x240`): 실패면 1, 성공이면 클레임 `& 0x88000` 이 0 일 때 1 아니면 0 | 비트 0 → 안 탐 | `BitField`=`0x4` (비트 2 = `IsImageDynamicallyRelocated` 만) `[관찰]` P1a |
| ⑧ 파티션 힙 이름 | `+0x255 CMP [ProcessParameters+0x428],0` (`HeapPartitionName.Buffer`) ≠ 0 → 1 반환 | 빈 값 | `[관찰]` P1 `HeapPartitionName ""` |
| ⑨ 이미지 이름 목록 | `+0x274 RtlpHpFindImageNameInList` — 이미지 이름 마지막 조각을 24개와 `_wcsnicmp`: `svchost.exe` `runtimebroker.exe` `csrss.exe` `smss.exe` `services.exe` `lsass.exe` `wininit.exe` `msmpeng.exe` `nissrv.exe` `dwm.exe` `rdpinit.exe` `searchindexer.exe` `smartscreen.exe` `sihost.exe` `systemsettingsbroker.exe` `settingsynchost.exe` `securityhealthservice.exe` `audiodg.exe` `telemetryhost.exe` `rdpshell.exe` `winlogon.exe` `containersettingsworker.exe` `vmcomputeagent.exe` `rdpclip.exe` | `test.exe` 는 목록에 없음 → 0 | `[정적]` 문자열을 `.rdata` 에서 읽음 |
| **그 밖의 갈림** | | | |
| `RtlpCreateHeap` 안에서 승격 | 위 질문 1 (나) | 안 탐 (`RtlpHpHeapFeatures`=0 `[관찰]`) | |
| 디버그 힙 `RtlDebugCreateHeap` | `+0xe60`. NT 길에서 합친 플래그 `& 0x61000000 ≠ 0` 이고 비트 28 꺼짐. `NtGlobalFlag` 비트 6 (`0x40`) → `0x40000000`, 비트 7 (`0x80`) → `0x20000000` 로 합쳐짐. (비트 4·5·21·12 는 꼬리·해제 검사·병합 끔·스택 추적 플래그 `0x20/0x40/0x80/0x8000000` 로만 합쳐지고 디버그 힙은 아님) | 안 탐 | `NtGlobalFlag`=0 `[관찰]` P1a |
| 페이지 힙 `RtlpDebugPageHeapCreate` | ㉮ `+0x116` 전역 `0x1cc578` ≠ 0 && `Base=NULL` && `Lock=NULL` → `+0x11e5`. ㉯ `+0xfd1/+0x1210` 전역 `0x1cc588` ≠ 0 → `+0xff9` | 안 탐 | `0x1cc578`=0 `[관찰]` P1a (쓰는 곳: `LdrpInitializeProcess +0xcca` 힙 **뒤**, 앱 검증기 `LdrpInitializeApplicationVerifierPackage +0x1d8`·`AvrfMiniLoadDll`). `0x1cc588` 은 정적 0, 쓰는 곳 `AvrfMiniLoadDll` 뿐 |
| 태그 힙 | `RtlpCreateHeap +0x558` (`NtGlobalFlag & 0x800` → 머리 뒤에 `+0x810` 태그 칸, 플래그 `0x4000000`), `RtlCreateTagHeap +0x3b~+0x45` | 안 탐 | `NtGlobalFlag`=0. `RtlCreateTagHeap` (`LdrpInitializeProcess +0xb78 (+0xb7d)`) 는 `+0x45 JZ → +0x67` 로 **0 반환** → `NtdllBaseTag`=0 `[관찰]`, `RtlpGlobalTagHeap`=0 `[관찰]` |
| 파티션 힙 | `LdrpInitializeProcessHeap +0x100~+0x111` → `NtOpenPartition` | 안 탐 | `[관찰]` 위 ⑧ |
| `+0xa34 RtlSetHeapInformation(0,1,0,0)` | `LdrSystemDllInitBlock+0xa0` (`MitigationOptionsMap`, 24바이트) 첫 8바이트 `>>12 & 3 == 1`. PDB 열거형 `PS_MITIGATION_OPTION_HEAP_TERMINATE` = 3 `[정적] PDB` → 비트 12~13 (옵션 번호 ×4비트 배치는 `[추측]`) | 안 탐 | `[관찰]` P1a `ntdll+0x1ea4f0` 첫 8바이트 = 0 |
| `RtlInitializeHeapManager +0x67 indirect-cfg` | ①-b 반환 ≠ 0 일 때만 도달. 대상 = `SbSelectProcedure(0xabababab, 0, &g_SbModuleTable_RTL, 1)` 의 반환 (`+0x5b`) 을 전역 `g_RegInfo+8` (`0x1ce698`) 에 넣은 값 | 안 탐 (`g_RegInfo+8`=0 `[관찰]` P1a) | 후보는 둘로 좁힘: `g_SbModuleTable_RTL` (`rva 0x173438`) → 항목 `g_SbTableEntry_HeapPaddingAndLFHSubsegmentCommitSwitch_RTL` (`rva 0x1cc3f0`, 이름 `HeapPaddingAndLFHSubsegmentCommitSwitch`, 소유 `Win32AppCompat`) 의 두 갈래: ⓐ `RtlpHpAppCompatDontChangePolicy` (설명 "Use the heaps default app compat policies.", `rva 0xfac60` = 3바이트 `ret`, 같은 주소 이름 26개 (ICF)), ⓑ `RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit` (설명 "Force lfh subsegments to be fully committed and pad all allocations", `rva 0x144160`, 27바이트: `RtlGetSuiteMask` 비트 16 이면 `RtlpHpAppCompatFlags \|= 2`). 어느 쪽인지는 `SbSelectProcedure` 가 OS 버전·`pShimData+0x810` 으로 고름 (D11) |

### 4. 읽는 입력

| 읽는 것 | 칸 (PDB 이름) | 읽는 곳 (함수 +오프셋) | test.exe 값 |
|---|---|---|---|
| **PEB** (`GS:[0x60]`) | `+0x3 BitField` 비트 4 `IsPackagedProcess` | `RtlpHpOptIntoSegmentHeap +0x20f` | 0 `[관찰]` |
| | `+0x18 Ldr` | `RtlpCreateHeap` 의 `DbgPrint("HEAP[%wZ]: ")` 쪽 (오류 길) 만 | null (P1) → 오류 길은 안 읽음 |
| | `+0x20 ProcessParameters` | `RtlpHpOptIntoSegmentHeap +0x255` (`+0x428`) · `LdrpInitializeProcessHeap +0x117/+0x298` (`+0x420`, `+0x428`, `+0x440`) · `LdrpHandleUserCopyAttributeList +0x15/+0x19` (`+0x448`) · `LdrpInitializeExecutionOptions +0xa2/+0xa6` (`+0x8` `Flags`) · `ImagePathName` (`+0x60`) 은 `LdrpInitializeProcess +0x2a6~+0x2c1` 가 지역 `UNICODE_STRING` 으로 옮김 | `Flags`=`0x4001`, `HeapPartitionName ""`, `AttributeList` null, `ImagePathName`=`C:\windows-pe-internals-main\…\test.exe` `[관찰]` P1 |
| | `+0x2d8 pShimData` | `RtlpHpOptIntoSegmentHeap +0x184`, `SbSelectProcedure` (`+0x810`, 안 탐) | `25996d10000` `[관찰]` |
| | `+0xbc NtGlobalFlag` | `RtlpCreateHeap +0x7f` (비트 `0x10/0x20/0x40/0x80/0x200000/0x1000`), `+0x558` (`0x800`), `RtlInitializeHeapManager +0xb1` (`0x100000`), `RtlCreateTagHeap +0x3b` (`0x800`) | 0 `[관찰]`. LoadConfig `GlobalFlagsSet/Clear` 가 앞서 고침 (`LdrpInitializeProcess +0x707~+0x723`; test.exe 는 0 둘) |
| | `+0xc8 HeapSegmentReserve` · `+0xd0 HeapSegmentCommit` · `+0xe0 HeapDeCommitFreeBlockThreshold` · `+0xd8 HeapDeCommitTotalFreeThreshold` | `RtlpCreateHeap +0x1d0 / +0x1ea / +0x204 / +0x21e` (매개변수 값이 0 일 때만 대신 씀) | `0x100000` / `0x2000` / `0x1000` / `0x10000` `[관찰]` P1a. 힙 머리 `+0xa0` / `+0xa8` / `+0xb0`(÷16) / `+0xb8`(÷16) 로 감 |
| | `+0x30 ProcessHeap` | `RtlpProcessHeapsInsert +0x29` | 0 (첫 힙) |
| | `+0x90 SharedData` (silo) | `RtlGetSuiteMask +0x9`, `RtlGetNtProductType +0x6`, `RtlGetCurrentServiceSessionId` | null `[관찰]` → KUSD 값을 읽음 |
| | `+0x2f8 ActivationContextData` | `RtlQueryActivationContextApplicationSettings` (`param_2=0`) | null `[관찰]` |
| | `+0x378 TracingFlags` (`HeapTracingEnabled` 비트 0) | `RtlpCreateHeap +0x10ee · +0xf1c` (`TracingFlags & 1`) | 0 `[관찰]` |
| **KUSER_SHARED_DATA** (`0x7ffe0000`) | `+0x4 TickCountMultiplier`, `+0x320` (8바이트 = `TickCountQuad`, 이름 `[추측]`) | `RtlHpGlobalsInitialize +0x21~+0x32` (난수 씨앗) | `[관찰]` `TickCountMultiplier`=`0xfa00000` |
| | `+0x264 NtProductType`, `+0x268 ProductTypeIsValid` | `RtlGetNtProductType +0x41 / +0x4b` | 1 / 1 `[관찰]` |
| | `+0x2d0 SuiteMask` | `RtlGetSuiteMask +0x2f` | `0x110` `[관찰]` |
| | `+0x310 FullNumberOfPhysicalPages` (0 이면 `+0x2e8 NumberOfPhysicalPages`) | `RtlpQueryPhysicalMemoryPolicy +0x23` | `0x1ffe86` (≈ 8GB) `[관찰]` → 정책값 `0x14` (> 10) |
| | `+0x274 ProcessorFeatures[0x2a]` · `[0x3c]` | `RtlIsProcessorFeaturePresent +0xf` (`RtlHpGlobalsInitialize +0x10f/+0x124`) | 둘 다 있음 → `RtlpHpHeapGlobals+0x28`=7 `[관찰]` |
| | `+0x380 UserModeGlobalLogger[0]` · `[4]` (`+0x388`) · `[5]` (`+0x38a`) | `RtlpCreateHeap +0x51f~+0x538 · +0x97c~+0x98f · +0x995~+0x9ac · +0x9b2~+0x9c7` (힙 ETW 로그 켜짐 검사) | 전부 0 `[관찰]` |
| **TEB** | `GS:[0x30]` → `+0x180c WowTebOffset` | `RtlpHpInitializePerfPolicies +0xa/+0x1f` | 0 (네이티브) |
| | `GS:[0x48]` `ClientId.UniqueThread` | `RtlHpGlobalsInitialize +0x29` (씨앗) | — |
| **exe NT 헤더** | `OptionalHeader.Magic`(`+0x18`)·`MajorSubsystemVersion`(`+0x48`)·`MinorSubsystemVersion`(`+0x4a`)·`SizeOfHeapReserve`(`+0x70`)·`SizeOfHeapCommit`(`+0x78`) | `LdrpInitializeProcessHeap +0x16f / +0x175 / +0x30b / +0xf8 / +0xfc` | `0x20b` / 6 / 0 / `0x100000` / `0x1000` `[정적+샘플]` (`sample/bin/test.exe`). PE32(`0x10b`)·WOW64 면 예약·커밋 0 으로 바꿈. 서브시스템 3.50 이하는 플래그 `0x10000` 을 더함 (test.exe 는 6.0 → 안 함) |
| **exe LoadConfig** | `Size`(+0x0) · `DeCommitFreeBlockThreshold`(+0x18) · `DeCommitTotalFreeThreshold`(+0x20) · `MaximumAllocationSize`(+0x30) · `VirtualMemoryThreshold`(+0x38) · `ProcessHeapFlags`(+0x48) | `LdrpInitializeProcessHeap +0x82~+0xe9` (크기 검사 뒤 0 아니면 힙 매개변수에 복사; `ProcessHeapFlags & 0xffff0fff` 가 0 이면 기본 `2`) | 크기 `0x140`, 칸 전부 0 `[정적+샘플]` → 기본값 |
| **ntdll 전역 (읽기)** | `RtlpLowFragHeapGlobalFlags` (`0x1d2218`) | `RtlpHpOptIntoSegmentHeap +0x28` | 비트 3·4 없음 |
| | `0x1ce268` · `RtlpHpHeapFeatures` (`0x1ce6d0`) · `RtlpDisableHeapLookaside` (`0x1ce6e0`) · `0x1cc578` · `0x1cc588` · `RtlpHeapKey` · `0x1ce6e8` · `UseWOW64` · `LdrpHeapPartitionName` | 질문 3 표와 `RtlpCreateHeap` 위 오프셋 | 모두 0 이거나 없음 `[관찰]` |
| **IFEO·레지스트리 값 이름** | 이 구간이 읽는 것: `GCInterval` (`RtlpHpQueryGCTimerInterval +0x40`). 구간 앞 `LdrpInitializeExecutionOptions` 가 읽는 힙 관련 이름: `DisableHeapLookaside` (→ `0x1ce6e0`), `FrontEndHeapDebugOptions`, `HeapFeatures` (8바이트), `GlobalFlag` (→ `PEB.NtGlobalFlag`). 레지스트리: `Segment Heap` 키의 `Enabled`. 힙 뒤: `DebugProcessHeapOnly` (`LdrpInitializeProcess +0xcae`), `StackTraceDatabaseSizeInMb` (`+0x8e3` 문자열, `+0x8f2`) | | test.exe: `Flags & 0x4000` 이라 `LdrpInitializeExecutionOptions` 의 IFEO 읽기는 **전부 건너뜀** (`HeapFeatures`=0, `FrontEndHeapDebugOptions`=0 으로 `RtlSetGlobalHeapFeatures` 만 부름) |

- **`KUSER_SHARED_DATA.ProductTypeIsValid` 가 0 일 때 레지스트리를 읽으러 가는가?** → **예. 단 `RtlGetSuiteMask` 가 아니라 `RtlGetNtProductType` 가 간다.** `RtlGetNtProductType +0x41 CMP byte [0x7ffe0268],0` → `+0x49 JZ +0x5d` → `RtlpGetNtProductTypeFromRegistry` (`NtOpenKey` `\Registry\Machine\System\CurrentControlSet\Control\ProductOptions` → `NtQueryValueKey` 값 `ProductType` → `LanmanNt`/`ServerNt`/`WinNt` 비교 → `NtClose`). 실패면 `*type = 1`. `RtlGetSuiteMask` 는 `PEB.SharedData` 가 있으면 silo 의 `+0x14`, 아니면 `0x7ffe02d0` 만 읽는다. 이 구간에서 `RtlGetNtProductType` 를 부르는 곳 = `RtlpHpInitializePerfPolicies +0x34`, (세그먼트 길) `RtlpQueryPhysicalMemoryPolicy +0x13`. test.exe 는 `ProductTypeIsValid`=1 이라 안 간다 `[관찰]`.

### 5. 쓰는 상태

- **PEB `+0x30 ProcessHeap` 을 실제로 쓰는 줄** = **`LdrpInitializeProcessHeap +0x1a2`** (`MOV [R15],RAX`; `R15` = 4번째 인자 = `LdrpInitializeProcess +0xa39 LEA R9,[RDI+0x30]` 로 넘긴 칸 주소). `RtlpCreateHeap` 가 돌려준 힙 주소를 `+0x19c` 0 검사 뒤에 쓴다. `RtlpProcessHeapsInsert` 가 도는 동안엔 이 칸은 아직 0.
- **`+0xe8 NumberOfHeaps`**: `RtlInitializeHeapManager +0xe7` (= 0) → `RtlpProcessHeapsInsert +0xb9` (= 1, `PEB.ProcessHeap` 이 0 이어서 `R14 == RDI` 일 때만). **`+0xec MaximumNumberOfHeaps`**: `RtlInitializeHeapManager +0x10a` (= `0x10`). **`+0xf0 ProcessHeaps`**: `RtlInitializeHeapManager +0xf5` (= `&RtlpPebHeapListStaticBuffer`, `rva 0x1d3ec0`, 16칸 × 8바이트) → 그 `[0]` = 힙 주소 (`RtlpProcessHeapsInsert +0xca`). 오프셋은 PDB `_PEB` 로 맞음 `[정적] PDB`. 사진: P1 은 `NumberOfHeaps`·`MaximumNumberOfHeaps`·`ProcessHeaps` 가 0·null, P1a 는 1 · `0x10` · `7ffafb833ec0` → `25996f30000` `[관찰]`.

**칸별 "쓰는 곳" 표 (baggage.md 3장 양식)**

경로 줄임: `②…` = 위와 같음. `②… (호출 +0x9ed) → …` 는 `LdrpInitializeProcess` 에서 그 함수를 부르는 자리.

#### 3.1 `_PEB` 4칸 (유저, 단계 ②)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x30` `ProcessHeap` | ② | 프로세스 힙 주소 (NT 힙 `_HEAP` 시작, 1MB 예약) | 주소 넘김 (칸 주소 `+0xa39`) → 받은 함수가 직접 씀 | ntdll | `②… (호출 +0xa4b) → LdrpInitializeProcessHeap +0x1a2` | `[정적]` sast40 · 정확 · 사진 `P1~P1a` (`25996f30000`) |
| `+0xe8` `NumberOfHeaps` | ② | 먼저 0, 곧 1 (프로세스 힙 하나뿐) | 직접 (두 번) | ntdll | `②… (호출 +0x9ed) → RtlInitializeHeapManager +0xe7` (0) · `②… (호출 +0xa4b) → LdrpInitializeProcessHeap (호출 +0x191) → RtlpCreateHeap (호출 +0x96f) → RtlpProcessHeapsInsert +0xb9` (1) | `[정적]` sast40 · 정확 · 사진 `P1~P1a` (P1a=1) |
| `+0xec` `MaximumNumberOfHeaps` | ② | `0x10` (정적 배열 `RtlpPebHeapListStaticBuffer` 칸 수) | 직접 | ntdll | `②… (호출 +0x9ed) → RtlInitializeHeapManager +0x10a` | `[정적]` sast40 · 정확 · 사진 `P1~P1a` (`0x10`) |
| `+0xf0` `ProcessHeaps` | ② | 힙 주소 배열 (처음엔 ntdll 정적 배열 `RtlpPebHeapListStaticBuffer` 가리킴, `[0]` = 프로세스 힙) | 직접 (칸) · 직접 (배열 `[0]`) | ntdll | `②… → RtlInitializeHeapManager +0xf5` (칸) · `… → RtlpProcessHeapsInsert +0xca` (배열 `[0]`) | `[정적]` sast40 · 정확 · 사진 `P1~P1a` (`7ffafb833ec0` → `25996f30000`) |

- 읽는 칸 (`HeapSegmentReserve` 등 4칸) 은 쓰지 않는다 (① 쪽 `MmCreatePeb` 가 씀, 값은 위 표). PEB `+0x48 IFEOKey` 는 이 구간이 안 쓴다 (`RtlpOpenImageFileOptionsKeyEx` 는 핸들을 호출자에게 돌려줄 뿐; 사진 null).

#### 3.8 ntdll `.data` 전역 중 이 구간이 쓰는 것 (유저)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `RtlpHpHeapGlobals` (`0x1cdec0`, 0x80B) `+0x0·+0x8` | ② | 난수 키 둘 (xorshift 상태를 한 칸씩 돌려 ×`0x2545f4914f6cdd1d`) | 직접 | ntdll | `②… → RtlInitializeHeapManager (호출 +0xd) → RtlHpGlobalsInitialize +0xc1 · +0x101` | `[정적]` · 정확 · 사진 `≤P1a` (`89c4…`·`0034…`) |
| `RtlpHpHeapGlobals +0x10` | ② | `&RtlpHeapFailureInfo` (포인터만) | 직접 | ntdll | `… → RtlHpGlobalsInitialize +0x108` | `[정적]` · 정확 · 사진 `≤P1a` (`7ffafb82d0e0`) |
| `RtlpHpHeapGlobals +0x28` | ② | 프로세서 기능 비트 (2: 기능 0x2a 있음, 4: 0x3c 있음, 1) | 직접 | ntdll | `… → RtlHpGlobalsInitialize +0x118 · +0x13e` | `[정적]` · 정확 · 사진 `≤P1a` (7) |
| `RtlpHpHeapGlobals +0x40` (`0x1cdf00`) | ② | 난수 상태 (씨앗 = `UniqueThread ^ tick 곱` → `RtlRandom` 두 번 → `&Globals` XOR). 이후 계속 CMPXCHG 로 돎 | 직접 | ntdll | `… → RtlHpGlobalsInitialize +0x6c (+0xa1·+0xe6)` · 이어 돌리는 곳 `RtlInitializeHeapManager +0x154` · `RtlpInitializeLowFragHeapManager +0x52` · `RtlpCreateHeap +0x3df` · `RtlpCreateHeapEncoding +0x59·+0x96` | `[정적]` `_refs` · 정확 · 사진 `≤P1a` |
| `RtlpHeapKey` (`0x1ce6d8`) | ② | 키 (상태 한 칸 돌려 ×상수) — `CommitRoutine` 인코딩에 씀 | 직접 | ntdll | `… → RtlInitializeHeapManager +0x16d` | `[정적]` `_refs` · 정확 · 사진 `≤P1a` |
| `RtlpLFHKey` (`0x1d2220`) | ② | 키 | 직접 | ntdll | `… → RtlInitializeHeapManager (호출 +0x191) → RtlpInitializeLowFragHeapManager +0x73` | `[정적]` `_refs` · 정확 · 사진 `≤P1a` |
| `RtlpAffinityState` (`0x1d37a0`) `+0x0`·`+0x4` | ② | 프로세서 수를 2 의 거듭제곱으로 올림 (최대 `0x40`), 실패면 1. `+0x4`=0 | 직접 | ntdll | `… → RtlpInitializeLowFragHeapManager +0xbf · +0xcc` (값 출처 = 시스템 콜 #4) | `[정적]` `_refs` · 정확 · 사진 `≤P1a` (2) |
| `RtlpDefaultHeapDebuggingOptions` (`0x1d3fa0`, 0x30B) | ② | 0 으로 (`+0xc` 칸은 `RtlpDisableHeapLookaside & 0x30` 일 때만 값) | 직접 | ntdll | `… → RtlpInitializeLowFragHeapManager +0xc5 · +0xd6 · +0xdd (· +0x11a)` | `[정적]` `_refs` · 정확 · 사진 `끝까지 0` |
| `RtlpHpLfhPerfFlags` (`0x1d3520`) | ② | LFH 성능 플래그: 제품종류 1 + 메모리 정책값 > 10 → `0x240f`, 세그먼트 매개 `*param_2`=0 이라 `0x2403`. (정책 ≤ 10 이면 `0x2040`, 서버 `0x280f`, WOW64 이면 `& ~4`) | 직접 | ntdll | `… (호출 +0x70) → RtlpHpInitializePerfPolicies +0x57 · +0x80 · +0x94` | `[정적]` `_refs` · 정확 · 사진 `≤P1a` (`0x2403`, 계산과 같음) |
| `RtlpHpHeapManager` (`0x1ce900`, 맨앞 `+0x0`) | ② | `+0x0` = `&RtlpHpHeapGlobals`, `+0x8..` 0 으로 `0x38d8` | 직접 | ntdll | `… (꼬리) → RtlHpInitializeHeapManager (호출 +0x3e) → RtlHpHeapManagerInitialize +0x1d · +0x2d · +0x3d` | `[정적]` `_refs` · 정확 · 사진 `≤P1a` (`7ffafb82dec0`) |
| `RtlpHpHeapManager +0x10` 희소 비트맵 #1 (`0x1ce910`, 0x48B) | ② | `+0x0`=요약 쪽 번지, `+0x8`=예약 시작, `+0x10`=비트 수 `0xFFFFFFE`, `+0x20`=-1, `+0x33`=0x0c, `+0x34`=0x10, `+0x38`=1 | 직접 (칸) · `syscall` (예약·확정, #6·#7) | ntdll (+ nt: VAD) | `… → RtlHpHeapManagerInitialize +0x49 RtlCSparseBitmapInitialize` (칸 0·`+0x20`·`+0x34`) → `RtlHpHeapManagerStart +0x2c → RtlCSparseBitmapStart +0xbb · +0x114` | `[정적]` · 정확 · 사진 `≤P1a` (`7ff5ef090000`·`7ff5ed090000`·`0xffffffe`·`…0c`·`0x10`·1 = 계산과 같음) |
| `RtlpHpHeapManager +0x58` VA 관리자 맥락 (`0x1ce958`) | ② | 6개 할당자 참조 칸 (`+0x860` 쪽), `+0x18`=5, 안의 희소 비트맵 (`+0x20`): 요약 번지 `7ff5ed070000`, 예약 시작 `7ff4ed070000`, 비트 수 `0x800000000` | 직접 (칸) · `syscall` (예약 #8) | ntdll (+ nt: VAD) | `… → RtlHpHeapManagerInitialize +0x55 RtlpHpVaMgrCtxInitialize` → `RtlHpHeapManagerStart +0x51 → RtlpHpVaMgrCtxStart +0x59 (RtlCSparseBitmapStart) · +0x86 ×6 (RtlpHpVaMgrCtxAllocatorReference)` | `[정적]` · 정확 · 사진 `≤P1a` (위 값 `[관찰]` 일치) |
| `RtlpHpHeapVaCallbacksRegistrar` (`0x1d3540`) | ② | 빈 등록부 머리 (`+0x4`=0x800, `+0x8` 목록 머리, 나머지 0) | 직접 | ntdll | `… → RtlHpHeapManagerInitialize +0x5a RtlpHpCustomVaCallbacksRegistrarInitialize +0x7 · +0x11 · +0x1f · +0x30 · +0x4b` | `[정적]` `_refs` · 정확 · 사진 `≤P1a` (`0x800` 칸 확인) |
| `0x1d21d8` (이름 없음) · `0x1d21a0~0x1d21d0` | ② | `0x1d21d8`=`0x4000` (LFH 켜기 설정 인자), 앞 칸 4개 0 | 직접 | ntdll | `… → RtlHpHeapManagerInitialize +0x69 · +0x75` (`LdrpInitializeProcessHeap +0x25d` 와 `RtlpHpHeapCreate +0x2b1` 이 읽음) | `[정적]` `_refs` · 정확 · 사진 `≤P1a` (`0x4000`) |
| `RtlpHpEnvFlsContext` (`0x1cdf40`, 약 0x60B) · `RtlpHpEnvHandle` (`0x1d39b0`) | ② | 0 으로 지우고 FLS 머리 스스로 가리키게 (`+0x48 ↔ +0x50`). 핸들 16바이트 0 | 직접 | ntdll | `… (꼬리) → RtlHpInitializeHeapManager +0x50~+0xa1` | `[정적]` `_refs` · 정확 · 사진 `≤P1a` / `끝까지 0` (`RtlpHpEnvHandle`) |
| `0x1ce270` · `0x1ce278` · `0x1ce280` (이름 없음, GC 타이머 상태) | ② | `0x1ce278` = GC 간격(ms) × −10000 (상대 100ns, 기본 −10,000,000), 나머지 0 | 직접 | ntdll | `… → RtlInitializeHeapManager +0x7d · +0x85 · +0x96` (값 = `RtlpHpQueryGCTimerInterval` 반환) | `[정적]` · 정확 · 사진 `≤P1a` (`ffffffffff676980`) — baggage 목록에 없던 것, 더함 |
| `RtlpDisableBreakOnFailureCookie` (`0x1ce6a0`) | ② | 섞은 프로세스 쿠키 → 곧 0 으로 덮음 (`RtlpDisableHeapLookaside` 비트 0 이 꺼짐; `NtGlobalFlag & 0x100000` 이어도 0) | 직접 | ntdll | `… → RtlInitializeHeapManager +0xa2 · +0xd0 · +0xe0` (`RtlSetHeapInformation +0x6b` 도 0, 안 탐) | `[정적]` `_refs` · 정확 · 사진 `끝까지 0` (0 을 씀) |
| `RtlpHeapErrorHandlerThreshold` (`0x1ce880`) | ② | `NtGlobalFlag & 0x100000` 이고 값이 1 이하일 때만 2 | 직접 (조건) | ntdll | `… → RtlInitializeHeapManager +0xc6` | `[정적]` `_refs` · 정확 · 사진 `끝까지 0` (조건 안 맞음) |
| `RtlpProcessHeaps` (`0x1d3f40`, 머리+꼬리 16B) | ② | 먼저 자기 자신을 가리키는 빈 목록, 이어 힙 노드 하나 (`힙+0x860`) 가 들어감 | 직접 | ntdll | `… → RtlInitializeHeapManager +0x116 · +0x123` (빈 목록) · `…→ RtlpCreateHeap (호출 +0x96f) → RtlpProcessHeapsInsert +0x9a · +0x9d · +0xa1 · +0xa4` (노드) | `[정적]` `_refs` · 정확 · 사진 `≤P1a` (Flink=Blink=`25996f30860`) |
| `RtlpProcessHeapsLock` (`0x1d3f60`, 0x28B) | ② | 임계 구역 만들기 (`DebugInfo` 는 ntdll 정적 배열 `RtlpStaticDebugInfo` 에서 받음; 플래그 `0x10000000` = SDK `RTL_CRITICAL_SECTION_FLAG_FORCE_DEBUG_INFO` `[추측]`) | 직접 | ntdll | `… → RtlInitializeCriticalSectionEx (호출 +0x12a)` → `RtlpAddDebugInfoToCriticalSection` (공용 부품) | `[정적]` · 정확 · 사진 `≤P1a` (`DebugInfo`=`7ffafb834020`, ntdll `.data` 안) |
| `RtlpPebHeapListStaticBuffer` (`0x1d3ec0`, 0x80B) | ② | `[0]` = 프로세스 힙 주소 (나머지 0) | 직접 | ntdll | `… → RtlpProcessHeapsInsert +0xca` (주소는 `RtlInitializeHeapManager +0xee` 가 PEB 에 넘김) | `[정적]` `_refs` · 정확 · 사진 `≤P1a` |
| `0x1ce6e8` · `0x1ce6f0` (이름 없음; PDB 이름 `RtlpDisableHeapLookaside` 뒤 `+8/+0x10`) | ② | `0x1ce6e8` = 사용자 공간 최대 주소 (시스템 콜 #9), `0x1ce6f0` = `0x10000` (직접 상수) | 직접 | ntdll | `… → RtlpCreateHeap +0xe1c` (`0x1ce6e8`), `+0xdeb` (`0x1ce6f0`) | `[정적]` `_refs` · 정확 · 사진 `≤P1a` (`7ffffffeffff`·`0x10000`) — 목록에 없던 것, 더함 |
| `0x1ce430` (이름 없음 — Ghidra 주석 `LdrpInitOnceLoadAsDataCrits+0x8`, baggage `≤P1a` 의 `LdrpInitOnceLoadAsDataCrits*` 에 묻혀 있을 수 있음. IFEO 바탕 키 핸들 캐시) | ② | `NtOpenKey` 로 연 바탕 키 핸들 (#1) | 직접 (`CMPXCHG.LOCK`) | ntdll | `… → RtlpHpQueryGCTimerInterval (호출 +0x27) → RtlpOpenImageFileOptionsKeyEx (호출 +0xa9) → RtlpOpenBaseImageFileOptionsKey +0x90` | `[정적]` `_refs` · 정확 · 사진 `≤P1a` (`0x10`) — 목록에 없던 것, 더함 |
| `LdrpHeapPartitionName` (`0x1d2960`, 16B `UNICODE_STRING`) | ② | 매개변수 `HeapPartitionName` 복사 (test.exe 는 빈 값 → 0 을 씀) | 직접 | ntdll | `②… (호출 +0xa4b) → LdrpInitializeProcessHeap +0x11f` | `[정적]` · 정확 · 사진 `끝까지 0` |
| `LdrpHeap` (`0x1d2860`) | ② | (이 구간 뒤) `PEB.ProcessHeap` 값을 복사 | 직접 | ntdll | `②… +0xb12` (이미 3.8 에 있음) | `[정적]` run18 · 사진 `≤P1a` (`25996f30000` = PEB 힙) |
| `NtdllBaseTag` (`0x1d28e4`) | ② | `RtlCreateTagHeap` 반환 (= 0, `NtGlobalFlag & 0x800` 아님) | 직접 | ntdll | `②… +0xb7d` | `[정적]` · 정확 · 사진 `끝까지 0` (0 을 씀) |

- **이 구간이 안 쓰는 것 (목록에 있지만 다른 곳이 쓴다 / 정적 초기값)**
  - `RtlpLowFragHeapGlobalFlags` (`0x1d2218`): 이 구간은 **안 씀** (`RtlInitializeHeapManager +0x17f` 는 스위트 비트 16 일 때만). 구간 앞에서 레지스트리·IFEO 가 비트 3·4·5 를 쓸 수 있었지만 test.exe 는 안 씀. P1a 값 `1` 의 비트 0 은 **LFH 만들기** `RtlpCreateLowFragHeap +0x24a~+0x256` (`& 3 == 0` 일 때 한 비트 켬) 가 쓴다 (힙 안 블록 나눠주기 쪽, 범위 밖 — baggage 7.6 기록만).
  - `RtlpLowFragHeapRandomData*` (`0x1d3420~`): `RtlpInitializeLfhRandomDataArray` (`RtlpCreateLowFragHeap +0x25c`, 세그먼트 길은 `RtlpHpLfhContextInitialize +0x164` 꼬리) 가 쓴다. 이 구간 아님 (LFH 켜질 때).
  - `RtlpDisableHeapLookaside` (`0x1ce6e0`): 쓰는 곳은 IFEO `DisableHeapLookaside` 읽기 (`LdrpInitializeExecutionOptions +0x127` 가 칸 주소를 넘김) 하나. test.exe 는 IFEO 를 건너뛰어 안 씀, `[관찰]` 0.
  - `RtlpLargestLfhBlock` (`0x1cd0a8`): 정적 초기값 `0x4000` (파일 `.data` 에 있음). 이 구간이 쓰는 건 `RtlInitializeHeapManager +0x186` (스위트 비트 16 일 때 `0x400`) 하나 → test.exe 는 안 씀 `[관찰]` `0x4000`.
  - `RtlpHeapFailureInfo` (`0x1cd0e0`): 정적 초기값 (`0x000006d000000002`). 쓰는 곳은 오류 길 (`RtlpLogHeapFailure`, `RtlpLocateRelatedBlocks`) 뿐. 이 구간은 주소만 `RtlpHpHeapGlobals+0x10` 에 넣음.
  - `RtlpDebugPageHeapTable` (`0x1cc570`) · `RtlpSecMemListHead` (`0x1cc538`, 정적으로 자기를 가리키는 빈 목록): 이 구간은 **읽기만** (`RtlpCreateHeap +0x116`, `RtlpSecMemFreeVirtualMemory +0x47`). 쓰는 곳은 앱 검증기·보안 메모리 등록 쪽.
  - `RtlpHpHeapFeatures` (`0x1ce6d0`) · `RtlpHpAppCompatFlags` (`0x1ce8f8`) · `RtlpGlobalTagHeap` (`0x1ce6a8`) · `g_RegInfo+8` (`0x1ce698`): 조건부로만 씀 — test.exe 는 안 씀 (모두 0 `[관찰]`, 쓰는 줄: `RtlInitializeHeapManager +0x31`, `RtlpHpSetLowFragHeapGlobalFlags +0x85`·`RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit +0xf`, `RtlCreateTagHeap +0x97`, `+0x5b`).
  - 공용 부품이 쓰는 것: `RtlCriticalSectionList*`·`RtlpStaticDebugInfo*` (임계 구역 만들기 — `RtlpProcessHeapsLock`·힙 락 둘 다 거침, 3.8 의 `FastPebLock` 줄과 같은 부품).

#### 3.x (새 절 제안) `_HEAP` — NT 힙 머리 (유저, `0x2c8`, ntdll PDB) — 프로세스 힙 하나를 만드는 동안 쓰는 칸

- 힙 번지 = P1a `25996f30000`. 칸 이름은 PDB `_HEAP` (`+0x0 Segment: _HEAP_SEGMENT` 가 첫 `0x70` 바이트에 겹침, `+0x0 Entry`). 값 중 `[관찰]` 은 P1a 덤프 (`PEB+0x30` 힙의 앞 `0x80` 바이트, `ms/logs/04_baggage-read_P1a.txt` 구획 16) 에서 읽은 것. 나머지 칸 값은 정적 계산이다 (P1a/P1b/P2 덤프 `dq 25996f30000 L0x5a` 로 확인 가능, D13).
- 단계 ②, 놓는 법 `직접`, 쓴 주인 ntdll, 경로는 모두 `②… (호출 +0xa4b) → LdrpInitializeProcessHeap (호출 +0x191) → RtlpCreateHeap (…)`. 아래 줄의 `+0x..` 는 `RtlpCreateHeap` 안이며 다른 함수는 이름을 붙임.

| `_HEAP` 칸 (PDB) | 값 | 쓰는 곳 | 근거 |
|---|---|---|---|
| `+0x8 Entry.Size` · `+0xa Flags` · `+0xf UnusedBytes` | 머리 크기 `(0x2f0+0xf)>>4` (16바이트 단위) · 1 · 1; 마지막에 `Encoding` 으로 XOR (`+0x8` 이 `01009bfbf7f5dcaf` 로 보임) | `+0x5a1 · +0x5aa · +0x5b3`; XOR `RtlpInitializeHeapSegment +0x2cf · +0x2d8` | `[정적]` · `[관찰]` 인코딩된 값 |
| `+0x98 Signature` | `0xeeffeeff` | `+0x5bc` | `[정적]` |
| `+0x70 Flags` | 호출자 플래그 `& ~0x10000000` (test.exe = `2`) | `+0x5d3` | `[정적]` · `[관찰]` 2 |
| `+0x74 ForceFlags` | `플래그 & 0x6001007d` (= 0) | `+0x652` | `[정적]` · `[관찰]` 0 |
| `+0x78 CompatibilityFlags` | 만드는 동안 `\|= 0x80000000` (`+0x709`), 끝에 `& 0x7fffffff` (`+0x9e0`). `RtlpDisableHeapLookaside & 1` 이면 `= 1` (`+0x874`) | 왼쪽 | `[정적]` · `[관찰]` 0 |
| `+0x90 Interceptor` | 0 (스택 추적 플래그 `0x8000000` 이면 인터셉터 번호) | `+0x5db` (`+0x632`) | `[정적]` |
| `+0x7c EncodeFlagMask` · `+0x80 Encoding` (`+0x88` 4바이트·`+0x8a`=0x10·`+0x8c` 2바이트·`+0x8e`=0) | `0x100000` · 난수로 채운 마스크 (조건: `CompatibilityFlags & 2 == 0` 이고 `Flags & 0x4000000 == 0`) | `RtlpCreateHeapEncoding +0x1d · +0x24 · +0x31 · +0x6b · +0xad · +0xb5` (호출 `+0x5ff`) | `[정적]` · `[관찰]` `+0x7c`=`0x100000` |
| `+0x238 Counters` 0x80B | 0 으로 지움; `+0x290 HeapPollInterval` = 1 (`+0x609`) | `+0x5f5 memset`; `+0x609` | `[정적]` |
| `Counters.TotalMemoryReserved` (`+0x238`) · `TotalMemoryCommitted` (`+0x240`) · `TotalSegments` (`+0x260`) · `TotalUCRs` (`+0x264`) · `TotalMemoryLargeUCR` (`+0x248`, 조건) | 세그먼트 크기만큼 더하고 줄임, 1, 1 | `RtlpInitializeHeapSegment +0x1fa · +0x20b · +0x32e`; `RtlpCreateUCREntry` | `[정적]` |
| 첫 세그먼트 칸 (`_HEAP_SEGMENT`, 힙 자신) `+0x10 SegmentSignature`=`0xffeeffee` · `+0x14 SegmentFlags`=2 · `+0x18 SegmentListEntry` · `+0x28 Heap` · `+0x30 BaseAddress` · `+0x38 NumberOfPages`=`0xff` · `+0x40 FirstEntry`=힙`+0x740` · `+0x48 LastValidEntry`=힙`+0xff000` · `+0x50 NumberOfUnCommittedPages` · `+0x54 NumberOfUnCommittedRanges` · `+0x58 SegmentAllocatorBackTraceIndex` (스택 추적 시만) · `+0x60 UCRSegmentList` | 값은 왼쪽. `FirstEntry` = `0x2f0` (락까지 넣은 머리) + `0x450` (블록 색인 `HEAP_LIST_LOOKUP`+힌트) | `RtlpInitializeHeapSegment +0x1c5 · +0x1ca · +0x1ce · +0x1d1 · +0x1d8 · +0x1dc · +0x1e8 · +0x212` (호출 `+0x746`), UCR `+0x2bb RtlpCreateUCREntry` | `[정적]` · `[관찰]` `+0x10`·`+0x14`·`+0x18`·`+0x28`·`+0x30`·`+0x38`=0xff·`+0x40`=`…740`·`+0x48`=`…2f000`·`+0x50`=0xf6(커밋이 9쪽으로 자란 뒤)·`+0x54`=1 |
| `+0x120 SegmentList` · `+0x110 VirtualAllocdBlocks` · `+0xf0 UCRList` · `+0x150 FreeLists` | 자기를 가리키는 빈 목록 (`SegmentList` 에는 첫 세그먼트 연결, `FreeLists` 에는 큰 빈 블록 하나) | `+0x679~+0x6b9`; `RtlpInitializeHeapSegment +0x306~+0x310` (SegmentList), `+0x2ea RtlpInsertFreeBlock` (FreeLists, `+0xc0 TotalFreeSize` 도 올림) | `[정적]` · `[관찰]` `+0x18` 이 `힙+0x120` |
| `+0xd2 HeaderValidateLength` · `+0xd8 HeaderValidateCopy` | `0x2c8` · 0 | `+0x661 · +0x66d` | `[정적]` |
| `+0x160 LockVariable` | 힙 안 임계 구역 (`힙+0x2c8`, 0x28B, `+0x6df` 가 만듦) | `+0x6fd` | `[정적]` |
| `+0x2f0` 블록 색인 (`_HEAP_LIST_LOOKUP`: `+0x8 ArraySize`=0x80 · `+0x20 ListHead`=`힙+0x150` · `+0x28 ListsInUseUlong` · `+0x30 ListHints`) · `+0x138 BlocksIndex` | 머리 뒤 `0x38+0x10+0x400` 바이트에 만들고 `+0x138` 이 가리킴 | `+0x76d~+0x78b`; `RtlpPopulateListIndex` (호출 `+0x797`) 첫 줄 | `[정적]` |
| `+0xd0 ProcessHeapsListIndex` | 0 | `+0x7a1` | `[정적]` |
| `+0xa0 SegmentReserve` · `+0xa8 SegmentCommit` · `+0xb0 DeCommitFreeBlockThreshold` · `+0xb8 DeCommitTotalFreeThreshold` | PEB `+0xc8`·`+0xd0` 값 그대로 (`0x100000`, `0x2000`), PEB `+0xe0`·`+0xd8` 값 ÷16 (`0x100`, `0x1000`) — 매개변수 값이 0 이면 | `+0x7b5 · +0x7c9 · +0x7e1 · +0x7f9` | `[정적]` (입력은 `[관찰]` PEB 값) |
| `+0xc8 MaximumAllocationSize` | `0x7ffffffdefff` (= 사용자 공간 최대 `0x7ffffffeffff` − `0x10000` − `0x1000`) | `+0x80d` | `[정적]` |
| `+0x94 VirtualMemoryThreshold` | `(0xff000+0xf)>>4` = `0xff00` | `+0x829` | `[정적]` |
| `+0x168 CommitRoutine` | `0 ^ RtlpHeapKey` (= 키) | `+0x843` | `[정적]` |
| `+0x2b8 TuningParameters.CommittThresholdShift` · `+0x2c0 MaxPreCommittThreshold` | 4 · `0x1fe000` | `+0x84f · +0x85b` | `[정적]` |
| `+0x100 AlignRound` · `+0x108 AlignMask` | `0x1f` (꼬리 검사 플래그 `0x20` 이면 `+0x10`) · `0xfffffffffffffff0` | `+0x884 · +0x8aa · +0x894` | `[정적]` |
| `+0x198 FrontEndHeap` · `+0x1a0 FrontHeapLockCount` · `+0x1a2 FrontEndHeapType` · `+0x1a3 RequestedFrontEndHeapType` · `+0x140 UCRIndex` | 모두 0 (LFH 는 나중에 켜짐 — 범위 밖) | `+0x8b7 · +0x8c3 · +0x8cf · +0x8db · +0x8e7` | `[정적]` |
| `+0x1a8 FrontEndHeapUsageData` · `+0x1b0 FrontEndHeapMaximumIndex` | `RtlAllocateHeap(힙, 0x80000a, 0x100)` 가 돌려준 블록 · `0x80` (그 블록의 바로 앞 1바이트도 1 로 씀) | `+0x915 · +0x91f · +0x93b · +0x944` | `[정적]` · 조건 `RtlpDisableHeapLookaside & 1 == 0` 이고 `(Flags & 3) == 2` |
| `+0x233 ReadOnly/InternalFlags` · `+0x170 StackTraceInitVar` | 0 · 0 | `+0x950 · +0x95c` | `[정적]` |
| `+0x188 UserContext` | `ProcessHeaps` 노드 주소 (`힙+0x860`) | `RtlpProcessHeapsInsert +0x72` (호출 `+0x96f`) | `[정적]` · `[관찰]` 노드 = `RtlpProcessHeaps` 의 Flink |

- **세그먼트 힙 길이었다면** (test.exe 는 안 탐, 개요만 `[정적]`): `RtlpHpHeapCreate` (`+0xc18`) 가 `_SEGMENT_HEAP` (`0xa00`) 을 `RtlpHpHeapAllocate` → `RtlpHpAllocVA` → `RtlpHpEnvAllocVA` → `NtAllocateVirtualMemoryEx` 로 받아 `+0x0 EnvHandle` (16B) · `+0x10 Signature`=`0xddeeddee` · `+0x14 GlobalFlags` · `+0x40~+0x57` 0 · `+0x70 StackTraceInitVar` 0 · `+0x140 SegContexts[0]` (`RtlpHpSegContextInitialize`, 단위 `0x100000`) · `+0x200 SegContexts[1]` (`0x1000000`) · `+0x2c0 VsContext` · `+0x340 LfhContext` 를 채운다. 프로세서 수는 `RtlpHpEnvQueryProcessorCount` (`NtQuerySystemInformationEx`, 한 번, 캐시 `0x1d2214`), LFH 켜기는 `RtlpHpLfhContextEnable` → `RtlpHpEnvTlsAlloc` (`RtlpFlsAlloc` + `RtlTlsAlloc`, 시스템 콜 없음). `RtlpHpHeapCreate +0x2d2 RtlpHpSegContextReserve` 가 세그먼트 한 개를 예약 (`RtlpHpSegSegmentAllocate`, 역시 `…EnvAllocVA`). 요청 범위 밖이라 깊이는 여기까지.

### 6. run44 사진과 맞춰 보기

- P1 → P1a 에 새로 생긴 VAD 중 세 개 (`ms/logs/04_baggage-read_P1a.txt` 구획 06b `!vad`, 단위 = 쪽 번호):

| 사진 VAD (P1a) | 쪽 수 | 확정 | 정적 계산 | 맞음 | 주인 |
|---|---|---|---|---|---|
| `25996f30 – 2599702f` | `0x100` (= 1MB) | 9쪽 | 예약 `0x100000` (`SizeOfHeapReserve`), 첫 커밋 `0x2000` (2쪽) → 그 뒤 힙이 자라 9쪽 (`NumberOfUnCommittedPages` `0xf6` = 255−9 `[관찰]`) | 예 | 프로세스 힙 (`RtlpCreateHeap +0x459 / +0x512`) |
| `7ff4ed070 – 7ff5ed08f` | `0x100020` (= `0x100020000` ≈ 4GB + 128KB) | 0 | `r1=0x100000000` + `r2=0x20000` | **정확히 같음** | VA 관리자 희소 비트맵 (`RtlpHpVaMgrCtxStart` → `RtlCSparseBitmapStart`, #8). 시작 `7ff4ed070000` = 덤프 `0x1ce958+0x28` |
| `7ff5ed090 – 7ff5ef090` | `0x2001` (= 32MB + 4KB) | 1쪽 | `r1=0x2000000` + `r2=0x1000`, 확정 1쪽 (#6·#7) | **정확히 같음** | 힙 관리자 희소 비트맵 (`RtlHpHeapManagerStart +0x2c`). 시작 `7ff5ed090000` = 덤프 `0x1ce910+0x8`, 확정 쪽 `7ff5ef090000` = `0x1ce910+0x0` |

- **뒤의 두 큰 예약이 힙 관리자 (비트맵·VA 관리, `RtlpHpEnvAllocVA`) 것인가?** → **예.** 둘 다 `RtlpHpEnvAllocVA +0xdd → NtAllocateVirtualMemoryEx` 로 받았고 (`TOP_DOWN` 이라 위에서부터 ① 32MB, ② 4GB 가 바로 아래), 크기는 사용자 공간 끝 (`NtQuerySystemInformation` 클래스 `0x3e` 의 `MaximumUserModeAddress + 1`, 덤프 `ctx1+0x10` = `0xffffffe`) 에서 나온다. 요청서의 짐작 (4GB·32MB+4KB 는 힙 관리자 것) 은 맞다.
- 확정 쪽 수 (0 과 1) 도 정적 코드와 같다: 4GB 쪽은 `r2` 가 `0x1000` 을 넘어서 확정 호출 (`RtlCSparseBitmapStart +0x114`) 이 없고, 32MB 쪽은 `r2=0x1000` 이라 1쪽 확정.
- 덤프로 이미 정해진 것 (다시 동적으로 안 묻는다): NT 힙 (헤더 `+0x10`), `RtlpHpHeapFeatures`=0, `+0xa34` 안 탐 (`LdrSystemDllInitBlock+0xa0`=0), 큰 예약 둘의 번지·크기·확정, `NtQuerySystemInformation` 세 번 다 성공 (`RtlpAffinityState`=2, `0x1ce6e8`, `ctx1+0x10`), `RtlpHpLfhPerfFlags`=`0x2403`, `0x1ce430`=`0x10` (IFEO 바탕 키 열림), GC 간격 기본 (`0x1ce278`).

## Q78 에서 이 조각으로 닫히는 칸

- **PEB** (`_PEB`): `+0x30 ProcessHeap` — 3.1 줄의 "쓰는 줄은 callee 안 (안 봄)" 을 `LdrpInitializeProcessHeap +0x1a2` 로 바꿀 수 있다. `+0xe8 NumberOfHeaps` · `+0xec MaximumNumberOfHeaps` · `+0xf0 ProcessHeaps` — 7.2 `P1~P1a` 모름 3칸이 **아는 칸** 으로 (줄 3개, 위 3.1 표). PEB 는 아는 칸 +3 (36), 모름 −3. 읽기만 하는 `+0xc8~+0xe0` 4칸은 그대로 (①).
- **ntdll `.data` 전역** (`≤P1a`·`끝까지 0` 쪽 모름 → 아는 것으로): `RtlpHeapKey` · `RtlpLFHKey` · `RtlpHpHeapManager` · `RtlpHpHeapGlobals` · `RtlpProcessHeaps` · `RtlpProcessHeapsLock` · `RtlpPebHeapListStaticBuffer` · `RtlpHpLfhPerfFlags` · `RtlpHpHeapVaCallbacksRegistrar` · `RtlpAffinityState` · `?RtlpHpEnvFlsContext@@3U_RTLP_FLS_CONTEXT@@A` · (끝까지 0 쪽, 0 을 쓰거나 조건이 안 맞음) `RtlpDefaultHeapDebuggingOptions` · `RtlpHpEnvHandle` · `RtlpDisableBreakOnFailureCookie` · `RtlpHeapErrorHandlerThreshold` · `LdrpHeapPartitionName` · `NtdllBaseTag`. 전부 위 3.8 표에 쓴 자리 있음.
- **목록에 없던 칸 (더함)**: `0x1ce270`·`0x1ce278`·`0x1ce280` (GC 타이머 상태), `0x1ce6e8`·`0x1ce6f0` (사용자 공간 최대 주소 캐시), `0x1ce430` (IFEO 바탕 키 핸들 캐시), `0x1d21d8`·`0x1d21a0~` (LFH 설정). 이름이 PDB 에 없거나 이웃 이름의 `+N` 이라 3.8 의 크기 짐작에 묻혀 있었을 가능성이 있다.
- **이 조각이 닫지 않고 넘기는 것**: `RtlpLowFragHeapGlobalFlags` · `RtlpLowFragHeapRandomData*` (LFH 만들기, 힙 안 블록 나눠주기 쪽 — 7.6 기록만) · `RtlpDisableHeapLookaside` (IFEO 값, test.exe 는 0 그대로) · `RtlpLargestLfhBlock` · `RtlpHeapFailureInfo` · `RtlpDebugPageHeapTable` · `RtlpSecMemListHead` (정적 초기값·다른 길이 씀). 나머지 Q78 이 "이 블록은 어디서 왔나" 를 물을 땐 **프로세스 힙 = `_HEAP` 시작 `25996f30000` (이 실행), 첫 빈 블록 = 힙+`0x740`, 힙 안 첫 블록 = `FrontEndHeapUsageData` (`0x100`B, 힙+`0x750`), 둘째 = `ProcessHeaps` 노드 (`0x30`B, 힙+`0x860`)** 까지 정해졌다 (뒤 블록은 `RtlAllocateHeap` 본문이라 범위 밖).
- **VAD** (커널 `_MMVAD` 쪽 짐): 이 조각이 만든 VAD 3개 (1MB 힙, 4GB+128KB, 32MB+4KB). `_MMVAD` 칸 표는 아직 없음 (7.6).
- 공용 부품 (한 번만 보고 이름으로 가리킴): `RtlInitializeCriticalSectionEx`·`RtlpAddDebugInfoToCriticalSection` (임계 구역), `RtlAllocateHeap` (본문 범위 밖), `RtlRandom`·xorshift (키·몫), `RtlpSecMemFreeVirtualMemory`, `RtlpHpEnvAllocVA`, `RtlCSparseBitmapInitialize`/`Start`.

## 동적분석으로 확인해야 할 것 (번호는 dast 요청서에서 그대로 쓸 것)

기준: `LdrpInitializeProcess +0x9ed` 이전~`+0xa50` 구간, 첫 스레드 (`LdrInitializeThunk` 아래), test.exe 일 때만 멈추게. 모두 함수 입구가 아니면 **call 자리 (ret 자리가 아님)** 를 쓴다. `[RSP+..]` 는 그 멈춘 순간 기준.

| # | 무엇을 | 추천 멈춤점 | 찍을 것 (기대) | 사진으로 답됨? |
|---|---|---|---|---|
| **D1** | `PEB.pShimData` 안 두 바이트 — ④ 조건. NT 힙 결과는 이미 정해졌지만 **어느 조건이 정했나** 를 못 가름 | `ntdll!RtlpHpOptIntoSegmentHeap+0x190` (`TEST [RAX+0x11b2],4`, RAX = pShimData) | `r rax` · `db @rax+0x11a8 L0x10` (`+0x11aa` 의 `0x40` 비트, `+0x11b2` 의 `0x4` 비트) (기대: `+0x11b2 & 4` 없음, 또는 `+0x11aa & 0x40` 없음) | 일부: 덤프에 사용자 쪽 `25996d10000` (2쪽) 이 들어 있으면 `.process /p /r` 뒤 `db 25996d10000+11a8 L10` 으로 가능 (읽기는 이번 범위 밖) |
| **D2** | `RtlpHpOptIntoSegmentHeap` 반환과 `*param_2` | `ntdll!RtlInitializeHeapManager+0x2d` (ret 자리) | `r eax` (기대 0) · `dd @rsp+0x38 L1` (기대 0) | 간접으로 답됨 (`RtlpHpHeapFeatures`=0, 힙 `+0x10`=`0xffeeffee`) — 낮은 우선순위 |
| **D3** | 매니페스트 `heapType` 조회 결과 (⑥) — `PEB.ActivationContextData` 가 null 일 때 음수로 끝나는지 | `ntdll!RtlpHpOptIntoSegmentHeap+0x1eb` (ret 자리) | `r eax` (기대 음수) | 아니오 (정적 `[추측]`) |
| **D4** | IFEO 열기 두 번의 결과와 **이 길이 이 프로세스의 첫 IFEO 열기인지** | ① `ntdll!RtlpOpenBaseImageFileOptionsKey+0x80` ② `ntdll!RtlpOpenImageFileOptionsKeyEx+0xec` (둘 다 ret 자리). 처음 걸릴 때 `k` | ① `r eax` (기대 0) · `dq @rbp+0x18 L1` (핸들, 기대 `0x10` 근처) ② `r eax` (기대 `0xc0000034`) · `k` 에 `RtlpHpQueryGCTimerInterval+0x2c` | 일부: 바탕 키 캐시 `0x1ce430`=`0x10` (열림), GC 간격 기본 → exe 키 없음 `[관찰]`. 호출 순서·결과 값은 동적 |
| **D5** | `NtQueryInformationProcess(0x24)` 반환·쿠키 (클래스 `0x24` 이름 `ProcessCookie` 확인) | `ntdll!RtlpGetModifiedProcessCookie+0x27` | `r eax` (기대 0) · `dd @rsp+0x40 L1` (쿠키 ≠ 0) · 그 뒤 `+0x2f` 에서 계산값 | 아니오 (결과 칸이 곧 0 으로 덮임) |
| **D6** | `NtQuerySystemInformation` 세 번의 순서·반환 | `ntdll!NtQuerySystemInformation` 입구에 조건 `@rcx==0 \|\| @rcx==0x3e` (call 자리를 `k` 로 확인) | 호출자 `RtlpInitializeLowFragHeapManager+0x89` (클래스 0) → `RtlHpInitializeHeapManager+0xae` (0x3e) → `RtlpCreateHeap+0xe0c` (0) 순서, 각 `eax`=0 | 예 (세 번 다 성공 `[관찰]` 간접). 순서만 동적 |
| **D7** | 힙 관리자 예약 세 번의 인자·돌려받은 번지 | `ntdll!RtlpHpEnvAllocVA+0xdd` (call 자리, 3번 걸림) | `dq @rdx L1`(번지 칸) · `dq @r8 L1`(크기) · `r r9`(형) · `dd @rsp+0x20 L1`(보호) · `dd @rsp+0x30 L1`(확장 개수). 기대: (0x2001000, 0x102000, 4, 1) → (0x1000, 0x1000, 4, 0) → (0x100020000, 0x102000, 4, 1). 각 `+0xe2` 뒤 번지 칸 값 | 예 (번지·크기·확정 일치 `[관찰]`). 순서와 `MEM_TOP_DOWN` 효과만 동적 |
| **D8** | `RtlpCreateHeap` 첫 입구 인자 (정적으로 값이 정해졌는지) | `ntdll!RtlpCreateHeap` 입구 (caller `LdrpInitializeProcessHeap+0x196`) | `r rcx` (=2) · `r rdx` (=0) · `r r8` (=`0x100000`) · `r r9` (=`0x1000`) · `dq @rsp+0x28 L1` (Lock=0) · `dq @rsp+0x30 L1` (매개변수 포인터) → `dd poi(@rsp+0x30) L1` (=`0x60`) | 아니오. 정적으로 확정이라 낮은 우선순위 |
| **D9** | 무작위 몫 n 과 `NtFreeVirtualMemory` 실제 여부 | `ntdll!RtlpCreateHeap+0x459` (call 자리) | `dq @rsp+0x90 L1` (몫 = n×0x10000) · `dq @rsp+0x98 L1` (크기 = `0x100000`+몫); `+0x4a1` 에서 멈추는지; `RtlpSecMemFreeVirtualMemory+0x2f` 에서 `r eax` | 아니오 (풀린 뒤 흔적 없음) |
| **D10** | `+0xa34` 와 `RtlSetHeapInformation` 안 타는지 (재확인용) | `ntdll!LdrpInitializeProcess+0xa34` | 안 걸려야 정상 | 예 (`LdrSystemDllInitBlock+0xa0`=0 `[관찰]`) — 필요 없음 |
| **D11** | `RtlInitializeHeapManager +0x67` indirect-cfg 안 타는지, 타면 대상 | `ntdll!RtlInitializeHeapManager+0x67` | 안 걸려야 정상. 걸리면 `r rax` → `ln @rax` (후보: `RtlpHpAppCompatDontChangePolicy` `0xfac60` / `RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit` `0x144160`) | 예 (`g_RegInfo+8`=0 `[관찰]`, `SbSelectProcedure` 안 불림) |
| **D12** | (선택) 구간 앞 `Segment Heap` 레지스트리 키가 있는지 | `ntdll!RtlpHpApplyGlobalSegmentHeapConfigurations+0x6d` (ret 자리) | `r eax` (기대 `0xc0000034`) | 예 (간접: `RtlpLowFragHeapGlobalFlags`=1 → 비트 3·4 꺼짐 → 키가 있어도 효과 없음) |
| **D13** | (선택) 힙 머리 나머지 칸 값 — 3.x 표의 정적 계산 검증 | 정적 값이라 멈춤점 불필요. 덤프로 | P1a/P1b/P2 덤프에서 `dq 25996f30000 L0x5a` → `+0xa0=0x100000`, `+0xa8=0x2000`, `+0xb0=0x100`, `+0xb8=0x1000`, `+0xc8=0x7ffffffdefff`, `+0x94=0xff00`, `+0x100=0x1f`, `+0x108=0xfffffffffffffff0`, `+0x1b0=0x80`, `+0x2b8=4`, `+0x2c0=0x1fe000`, `+0x290=1` (LFH 켜진 뒤 `+0x198`·`+0x1a2` 는 바뀜) | 예 (덤프로 가능, 읽기는 이번 범위 밖) |

- dast 요청 필요: **D1·D4·D5·D9** (정적으로 못 정한 것). 나머지는 사진·정적으로 닫혔고, 덤프를 이미 가진 사람이 확인만 하면 된다 (D1·D13 은 덤프로 가능).
- 콜스택 기대 (D7, `NtAllocateVirtualMemoryEx` 에서 `k`): `ntdll!RtlpHpEnvAllocVA+0xe2` ← `RtlCSparseBitmapStart+0xc0` (또는 `+0x119`) ← `RtlHpHeapManagerStart+0x31` (VA 관리자는 `RtlpHpVaMgrCtxStart+0x5e` ← `RtlHpHeapManagerStart+0x56`) ← `RtlHpInitializeHeapManager+0xdd` ← `LdrpInitializeProcess+0x9f2` ← `_LdrpInitialize+0x16c` (**`RtlInitializeHeapManager` 프레임은 꼬리 JMP 라 안 보임**).

## 요청서에 없던 것을 더 뽑은 이유

- **지도 기준 줄기를 따라가다 일이 한두 단계 아래에서 일어나서 더 뽑았다.**
  - 큰 예약 둘: `RtlCSparseBitmapInitialize`·`RtlpHpVaMgrCtxInitialize`·`RtlpHpCustomVaCallbacksRegistrarInitialize`·`RtlpHpVaMgrCtxAllocatorReference` (`RtlHpHeapManagerInitialize`/`RtlpHpVaMgrCtxStart` 아래, 비트맵 한계값 `+0x34` 와 6개 할당자 참조) → 크기 계산이 사진과 맞는지 보려고.
  - 세그먼트 힙 선택 조건: `LdrpHandleUserCopyAttributeList` (이름 없는 전역 `0x1ce268` 의 유일한 쓴 자리), `RtlSetGlobalHeapFeatures`·`RtlpHpSetLowFragHeapGlobalFlags`·`RtlpHpApplyGlobalSegmentHeapConfigurations` (전역 비트와 레지스트리 시스템 콜), `LdrQueryImageFileExecutionOptions(Ex)`·`RtlQueryApplicationKeyOption` (IFEO 가 이 길에서 처음 열리는지), `RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit` (indirect-cfg 후보), `RtlGetCurrentServiceSessionId`.
  - 머리 칸: `RtlpCreateUCREntry`·`RtlpInsertFreeBlock`·`RtlpHeapAddListEntry`·`RtlpGetHeapProtection`·`RtlpHpHeapCheckCommitLimit` (`RtlpInitializeHeapSegment` 아래), `RtlpCreateLowFragHeap` (`RtlpLowFragHeapGlobalFlags` 의 P1a 값이 이 구간이 아니라는 확인).
  - `RtlpGetNtProductTypeFromRegistry` (질문 4 레지스트리 답).
- **본문을 깊이 읽지 않은 곳 (이름만)**: `RtlpProcessIFEOKeyFilter`·`RtlpCreateIFEOKeyFilterKey`·`RtlpOpenBaseImageFileOptionsKeyEx` (IFEO 키가 **있을 때** 길, test.exe 는 안 탐), `RtlpHpHeapAllocate`/`RtlpHpSegContextReserve`/`RtlpHpSegContextInitialize` 등 세그먼트 힙 길 본문, `RtlpHeapLogRangeCreate`·`RtlpLogHeapCommit`·`RtlpLogHeapCreateEvent`·`RtlpHpHeapLoggingStateSync` (로그, 공용), `RtlDebugCreateHeap`·`RtlpDebugPageHeapCreate`·`RtlDebugCreateTagHeap` (갈림 안 탐), `RtlpHeapAddListEntry` (색인 칸 수만).
- **지도가 덜 펼쳐진 곳**: 없음. 요청서 "지도에서 본 것" 의 줄 번호 (`RtlpCreateHeap +0x459/+0x512/+0x5ff/+0x746/+0x797/+0x915/+0x96f/+0xc18/+0xc35/+0xd60/+0x1043/+0xe07/+0xe60/+0xff9`, `RtlInitializeHeapManager` 11줄, `LdrpInitializeProcessHeap` 8줄) 가 `_calls.tsv` 와 전부 같다. `+0xbf2 RtlpHpConvertCreationFlags` 도 확인.

## 판정

**분석성공.** 요청서 질문 1~6 에 모두 답이 있고 (정적 + 사진 덤프 근거), test.exe 의 프로세스 힙이 NT 힙이라는 것, 시스템 콜 12개 목록, 큰 예약 둘의 정확한 크기 (사진과 일치), PEB 힙 4칸의 쓴 자리를 닫았다. 정적으로 못 닫은 것은 `pShimData` 두 바이트·IFEO exe 키 열기 결과·무작위 몫 n·ProcessCookie 반환 (D1·D4·D5·D9) 로 모두 결과(NT 힙)에는 영향이 없다.

## 도구 메모 (스크립트 문제 아님, 제안만)

- PDB 구조체 칸을 한 줄 명령으로 뽑는 도구가 스킬에 아직 없다 (sast/26 이 이미 제안). 이번에도 `llvm-pdbutil-14 dump -types` 결과를 읽는 임시 스크립트로 `_PEB`·`_HEAP`·`_SEGMENT_HEAP` 등을 맞췄다. `run_ghidra.sh` 옆에 "구조체 이름 → 칸 표 (오프셋·타입·비트)" 명령이 있으면 짐 지도 칸 표를 만들 때마다 편하다.
- 하나 더: `refs` 로 전역을 볼 때 PDB 이름 뒤 `+0x8`·`+0x10` 자리 변수 (예: `RtlpDisableHeapLookaside+0x8` = 사용자 공간 최대 주소) 는 이름 없는 별개 변수다. `.asm` 의 전역 주석이 "가장 가까운 앞쪽 이름 + 칸" 이라 그 이름의 일부처럼 보이니 baggage 의 전역 칸 크기 짐작에서 주의.

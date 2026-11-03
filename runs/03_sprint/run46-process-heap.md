# 2026-10-27 run46 — ntdll 프로세스 힙 만들기 (`LdrpInitializeProcess +0x9ed ~ +0xa50`, 묶음 7): test.exe 프로세스 힙은 **NT 힙**, 구간 시스템 콜 ~~12개~~ **13개** (run61 정정), P1→P1a 큰 예약 둘은 **힙 관리자** 것, PEB `+0x30 ProcessHeap` 을 쓰는 줄 = `LdrpInitializeProcessHeap +0x1a2` (정적)

> **정정 (run61)**: 힙 구간 시스템 콜은 **13개** 다 (무작위 몫 n = 0 이면 12개). `RtlHpGlobalsInitialize` 의 첫 `RtlRandom` (call `+0x4c`) 이 상태 0 인 `RtlpRandomExInit` 을 켜면서 `RtlpInitRandomExVector` 안 `NtQueryInformationProcess(-1, 0x24, …, 4)` (call `+0x24` / ret `+0x29`) 를 한 번 부른다 — 이 문서는 "시스템 콜 없이" 로 적었다. 맨 앞 표에 새 #1, 옛 #1 ~ #12 → #2 ~ #13, `NtQueryInformationProcess` 1 → 2. → `run61-random-ex-init.md` "정정"

## 보통 길 시스템 콜 목록 (함수 / 이름 / 클래스 / 크기 / 횟수 / 조건)

- 보통 길 = test.exe 값으로 가는 길 (NT 힙 길). 구간 `LdrpInitializeProcess +0x9ed ~ +0xa50` 안 시스템 콜 ~~**12개** (무작위 몫 n 이 0 이면 #11 이 빠져 11개)~~ → (run61) **13개** (무작위 몫 n 이 0 이면 #12 가 빠져 12개).
  - `NtOpenKey` 2 · `NtQueryInformationProcess` ~~1~~ **2** (run61) · `NtQuerySystemInformation` 3 · `NtAllocateVirtualMemoryEx` 3 · `NtAllocateVirtualMemory` 2 · `NtFreeVirtualMemory` 1
- "함수" 칸 = 시스템 콜 스텁을 부르는 함수 `+ret` 자리 (WinDbg 콜스택 `함수+0x..` 와 같은 기준, `_calls.tsv` 의 ret). 사슬 위쪽은 아래 "호출 관계".
- 번호 = ntdll 스텁의 시스템 콜 번호 (`mov eax, imm`, `[정적]`). 클래스 이름은 PDB 열거형 `_SYSTEM_INFORMATION_CLASS` 에 있으면 그 이름 (`0x24` 는 PDB 에 없어 공개 헤더 이름 `[추측]`).
- `NtQuerySystemInformation` 을 부르는 줄은 `_calls.tsv` 에 `alias:NtQuerySystemInformation+1` 로 적혀 있다 — 같은 주소에 이름이 하나 더 있다는 표시 (`Nt`/`Zw` 짝으로 보임 `[추측]`, 이번에 `pdb_aliases.py --lookup` 표는 없어 못 봄). 결과서는 스텁 번호 `0x36` 로 맞췄다.
- 표시: `[정적]` = 코드 줄. 사진 = run44 P1a 덤프 값이 그 결과와 맞음 (실행을 멈춰 본 것은 아님).
- (run61) 맨 앞에 #1 (`RtlpInitRandomExVector`) 을 넣어 **번호가 하나씩 밀렸다** (옛 #n = 새 #n+1). 이 문서 안 `#` 번호는 모두 새 번호로 고쳤다.

| # | 함수 (ret 자리) | 이름 (번호) | 클래스 / 인자 | 크기 | 횟수 | 조건 | 표시 |
|---|---|---|---|---|---|---|---|
| 1 | (run61 새로) `RtlpInitRandomExVector +0x29` (← `RtlRandom +0x53` ← `RtlHpGlobalsInitialize +0x51` ← `RtlInitializeHeapManager +0x12`) | `NtQueryInformationProcess` (#4 와 같은 스텁) | 프로세스 `-1`, 클래스 **`0x24`** (`ProcessCookie` `[추측]`), 반환 길이 `NULL` | 출력 **4바이트** (지역 `[RSP+0x58]`) | 1 | 항상 — `RtlpRandomExInit` 이 파일 값 0 으로 시작해 **첫 `RtlRandom` 만** 탐 (둘째 `+0x61` 은 상태 2 라 안 탐). 결과 ^ `LdrSystemDllInitBlock+0x98` 이 씨앗 → `RtlpRandomExConstantVector` 128 dword · `RtlpRandomExAuxVarY`. 실패면 `RtlRaiseStatus` | `[정적]` sast56 · 사진 `0x1ce878` = 2 (run61) |
| 2 | `RtlpOpenBaseImageFileOptionsKey +0x80` (← `RtlpOpenImageFileOptionsKeyEx +0xae` ← `RtlpHpQueryGCTimerInterval +0x2c` ← `RtlInitializeHeapManager +0x7d`) | `NtOpenKey` (0x12) | IFEO **바탕 키** `\Registry\Machine\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options`, 접근 `9`, 속성 `0x240` | 핸들 1개 (전역 rva `0x1ce430` 에 캐시) | 1 | 항상 (이미지 이름이 있음). 이 프로세스에서 **처음** 여는 IFEO (앞 `LdrpInitializeExecutionOptions` 는 매개변수 `Flags & 0x4000` 이라 건너뜀) | `[정적]` · 사진 `0x1ce430`=`0x10` |
| 3 | `RtlpOpenImageFileOptionsKeyEx +0xec` | `NtOpenKey` (0x12) | 바탕 키 아래 `test.exe` (이미지 이름 마지막 조각), 접근 `9`, 열기만 | 핸들 1개 | 1 | #2 성공 뒤. 키가 없으면 실패 (기대 `0xC0000034`) → GC 간격 기본값, 아래 `NtQueryValueKey`·`NtClose` 안 탐 | `[정적]` · 사진 GC 간격 기본 (간접) |
| 4 | `RtlpGetModifiedProcessCookie +0x27` (← `RtlInitializeHeapManager +0xa2`) | `NtQueryInformationProcess` (0x19) | 프로세스 `-1`, 클래스 **`0x24`** (`ProcessCookie` `[추측]` — PDB 열거형 없음), 반환 길이 `NULL` | 출력 **4바이트** | 1 | 항상. 결과는 `RtlpDisableBreakOnFailureCookie` 에 섞어 넣지만 곧 0 으로 덮임 | `[정적]` |
| 5 | `RtlpInitializeLowFragHeapManager +0x89` (← `RtlInitializeHeapManager +0x196`) | `NtQuerySystemInformation` (0x36) | 클래스 **0** (`SystemBasicInformation`, `[정적] PDB`) | **0x40바이트** (`+0x38` 1바이트 = 프로세서 수만 씀) | 1 | 항상. 실패하면 `RtlpAffinityState` = 1 로 남음 | `[정적]` · 사진 `RtlpAffinityState`=2 |
| 6 | `RtlHpInitializeHeapManager +0xae` (← 꼬리 JMP `RtlInitializeHeapManager +0x1a0`) | `NtQuerySystemInformation` (0x36) | 클래스 **0x3e** (`SystemEmulationBasicInformation`, `[정적] PDB`) | **0x40바이트** (`+0x28` 사용자 공간 최대 주소만 씀) | 1 | 항상. **실패하면** `RtlHpHeapManagerStart` 를 건너뜀 → #7~#9 없음 | `[정적]` · 사진 큰 예약 둘 있음 = 성공 |
| 7 | `RtlpHpEnvAllocVA +0xe2` (← `RtlCSparseBitmapStart +0xc0` ← `RtlHpHeapManagerStart +0x31` ← `RtlHpInitializeHeapManager +0xdd`) | `NtAllocateVirtualMemoryEx` (0x78) | **예약** 형 `0x102000` (`MEM_RESERVE \| MEM_TOP_DOWN`), 보호 `4`, 확장 인자 1개 | **`0x2001000`** (32MB + 4KB) | 1 | #6 성공. 비트 수 `0xFFFFFFE` (사용자 공간 끝 ÷ 1MB × 2) → 32MB 비트맵 + 4KB 요약 | `[정적]` · 사진 VAD `7ff5ed090–7ff5ef090` |
| 8 | `RtlpHpEnvAllocVA +0xe2` (← `RtlCSparseBitmapStart +0x119`) | `NtAllocateVirtualMemoryEx` (0x78) | **확정** 형 `0x1000`, 보호 `4`, 확장 인자 0개 | `0x1000` (1쪽, 예약 시작 + `0x2000000`) | 1 | #7 뒤, 요약 쪽 크기 ≤ `0x1000` 일 때 | `[정적]` · 사진 확정 1쪽 |
| 9 | `RtlpHpEnvAllocVA +0xe2` (← `RtlCSparseBitmapStart +0xc0` ← `RtlpHpVaMgrCtxStart +0x5e` ← `RtlHpHeapManagerStart +0x56`) | `NtAllocateVirtualMemoryEx` (0x78) | **예약** `0x102000`, 보호 `4`, 확장 인자 1개 | **`0x100020000`** (4GB + 128KB) | 1 | #7·#8 뒤. 비트 수 `0x800000000`. 요약 쪽 `0x20000` > `0x1000` 이라 **확정 없음** | `[정적]` · 사진 VAD `7ff4ed070–7ff5ed08f`, 확정 0 |
| 10 | `RtlpCreateHeap +0xe0c` (← `LdrpInitializeProcessHeap +0x196` ← `LdrpInitializeProcess +0xa50`) | `NtQuerySystemInformation` (0x36) | 클래스 **0** | **0x40바이트** (`+0x28` 을 전역 `0x1ce6e8` 에 캐시) | 1 | 전역 `0x1ce6e8` 이 0 일 때 (이 프로세스 첫 `RtlpCreateHeap`). 실패면 힙 실패 → `0xc0000017` | `[정적]` · 사진 `0x1ce6e8`=`7ffffffeffff` |
| 11 | `RtlpCreateHeap +0x45e` | `NtAllocateVirtualMemory` (0x18) | 프로세스 `-1`, 형 `0x2000` (**예약**), 보호 `4` | **`0x100000 + n×0x10000`** (n = 0~31 무작위) | 1 | 힙 터 `Base = NULL` 일 때. `0x100000` = test.exe 헤더 `SizeOfHeapReserve` | `[정적]` · 사진 힙 VAD 1MB |
| 12 | `RtlpSecMemFreeVirtualMemory +0x2f` (← `RtlpCreateHeap +0x4a6`) | `NtFreeVirtualMemory` (0x1e) | 형 `0x8000` (`MEM_RELEASE`), 번지 = 예약 시작 | **`n×0x10000`** (앞쪽 무작위 몫만 풂) | 0~1 | n ≠ 0 (31/32) | `[정적]` (풀린 뒤라 사진에 흔적 없음) |
| 13 | `RtlpCreateHeap +0x517` | `NtAllocateVirtualMemory` (0x18) | 형 `0x1000` (**확정**), 보호 `4` | **`0x2000`** (`SizeOfHeapCommit 0x1000` 을 8KB 로 올림) | 1 | #11 성공 뒤 | `[정적]` |

- 구간 **앞**에서 힙 종류에 영향을 주는 시스템 콜 (위 ~~12개~~ 13개 (run61) 엔 안 셈): `RtlpHpApplyGlobalSegmentHeapConfigurations +0x6d` `NtOpenKey` — `\Registry\Machine\SYSTEM\CurrentControlSet\Control\Session Manager\Segment Heap` (경로 `LdrpInitializeExecutionOptions +0x61e → RtlSetGlobalHeapFeatures +0x17 →`). IFEO 를 건너뛰어도 항상 부른다. 키가 있으면 `NtQueryValueKey` (`Enabled`, 4바이트)·`NtClose` 가 더 붙는다 `[정적]`.
- 구간(묶음 7) 안이지만 힙 **뒤**: `LdrpInitializeProcess +0xb52` `NtQuerySystemInformation` 클래스 `0xc5` (`SystemHypervisorSharedPageInformation`, `[정적] PDB`), 8바이트 → 전역 `RtlpHypervisorSharedUserVa`. 본문은 안 봄.
- **보통 길에서 안 타는 시스템 콜** `[정적]` (조건·test.exe 값은 결과서 표):
  - `NtOpenPartition` (`LdrpInitializeProcessHeap +0x219`) — 매개변수 `HeapPartitionName` 이 있을 때. test.exe 빈 값 (사진 P1).
  - `NtQueryVirtualMemory` ×2 (`RtlpCreateHeap +0xd65` · `+0x1048`) — 호출자가 힙 터 `Base ≠ NULL` 을 줄 때. test.exe `Base = 0`.
  - `NtQueryValueKey` `GCInterval` + `NtClose` (`LdrQueryImageFileKeyOption`) — #3 에서 test.exe 키가 열렸을 때만.
  - 제품 종류 레지스트리 `NtOpenKey`·`NtQueryValueKey`·`NtClose` (`RtlGetNtProductType → RtlpGetNtProductTypeFromRegistry`) — KUSD `ProductTypeIsValid` = 0 일 때. test.exe 1 (사진).
  - `NtTraceEvent` (힙 ETW 로그) — KUSD `UserModeGlobalLogger`·`PEB.TracingFlags` 가 켜졌을 때. 모두 0 (사진).
  - 세그먼트 커밋 `NtAllocateVirtualMemory` (`RtlpInitializeHeapSegment +0x106`) — 머리 + `0x50` 이 첫 커밋 끝을 넘을 때. 첫 빈 블록 = 힙 `+0x740` < `+0x2000`.
  - `NtCreateKey`·`NtSetValueKey`·`NtDeleteKey` (`RtlpOpenImageFileOptionsKeyEx`) — 만들기 모드일 때.
  - 세그먼트 힙 길 시스템 콜 (`RtlpHpHeapCreate` 아래) — `RtlpHpHeapFeatures & 1` 일 때.
  - 재시도 `NtQueryVirtualMemory`·`NtFreeVirtualMemory` (`RtlpSecMemFreeVirtualMemory`) — 첫 해제가 `0xC0000045` 이고 `RtlpSecMemListHead` 가 안 비었을 때.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 03_sprint 넷째 바퀴 (짐 지도 바퀴, plan **Q78a** — Q78 "쓴 자리·줄기" 의 첫 조각, 사용자 요청 2026-10-27).
- 정적: ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE` (flow.md 기준과 같음). 이미 analyze 된 프로젝트에서 뽑기만 함 (다시 analyze 안 함).
- 버전 대조 (결과서): `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x16c → LdrpInitializeProcess` 가 `_calls.tsv` 와 같다. 정적 해독 ↔ run44 P1a 덤프 값 20곳 이상 일치 (힙 머리 칸, `RtlpHpLfhPerfFlags`, 희소 비트맵 칸, VAD 쪽 수 셋).
- 이 문서를 쓰며 다시 맞춘 것: 아래 "호출 지점" 표의 call/ret 을 `ms/disassemble/ntdll/_calls.tsv` 로, PEB 칸 쓰는 줄 (`RtlInitializeHeapManager +0xe7`·`+0xf5`·`+0x10a`, `RtlpProcessHeapsInsert +0xb9`·`+0xca`, `LdrpInitializeProcessHeap +0x1a2`) 을 `.asm` 로 한 번 더 봤다. 결과서와 같다.

## 목적

- 짐 지도 Q78 (칸마다 쓴 자리·줄기) 중 **프로세스 힙 구간 하나**를 먼저 닫는다.
  - run44 사진은 PEB `ProcessHeap`·`NumberOfHeaps`·`MaximumNumberOfHeaps`·`ProcessHeaps` 와 ntdll 힙 전역이 "P1~P1a 에 값이 생김 / `≤P1a`" 까지만 보여 줬다. 쓴 자리는 모두 묶음 7 안이라 한 구간으로 닫힌다.
  - 이 힙은 뒤 칸들 (매개변수·환경 블록, 모듈 항목·노드, TLS, 활성화 컨텍스트 스택) 이 블록을 받는 곳이다. 먼저 닫아 두면 나머지 Q78 에서 "이 블록은 어디서 왔나" 를 칸마다 다시 안 판다.
- 요청서 질문 6개: (1) 호출 사슬과 순서, (2) 보통 길 시스템 콜 전부, (3) 갈림길 (세그먼트 / NT, 디버그·페이지·태그·파티션 힙), (4) 읽는 입력, (5) 쓰는 상태 (PEB 4칸, ntdll 전역, `_HEAP` 머리), (6) run44 P1→P1a VAD 3개와 맞추기.
- 범위: `_HEAP` 머리는 "프로세스 힙 하나를 만드는 동안 머리에 쓰는 칸" 까지. **블록 나눠 주기 내부 (`RtlAllocateHeap` 본문, LFH) 는 범위 밖** — 사용자 결정 2026-10-26 "기록만", 2026-10-27 "힙 내부 정적 (후보 B) 안 넣음" 그대로.

## 방법

- 정적: `_workspace/sast/40_process-heap.md` — ntdll, 요청 표 24개 depth 1 (+ `RtlCreateHeap`·`RtlCreateTagHeap` depth 0) + 길을 따라 20개 더 (희소 비트맵·VA 관리자, 세그먼트 힙 선택 조건, 머리 칸 아래 함수, 제품 종류 레지스트리). 이유는 결과서 "요청서에 없던 것을 더 뽑은 이유".
  - `.asm`/`.c` 225 → 342개, `_calls.tsv` 1,625 → 2,165줄 (줄어든 줄 없음), `_refs` 새로 17개 이상.
  - 구조체 칸 (`_PEB` `_HEAP` `_HEAP_SEGMENT` `_HEAP_COUNTERS` `_HEAP_ENTRY` `_HEAP_LIST_LOOKUP` `_SEGMENT_HEAP` …, 열거형 `_SYSTEM_INFORMATION_CLASS` `_PS_MITIGATION_OPTION`) 는 `llvm-pdbutil-14 dump -types` 로 맞춤 (스크래치, 커밋 안 함).
  - 값 대조는 run44 덤프 읽기 로그 `ms/logs/04_baggage-read_P1a.txt` (구획 06b `!vad`, 구획 16 힙 앞 `0x80` 바이트 등). 새 동적 실행은 없다.
- 결과서: `_workspace/sast/40_process-heap_result.md` (**분석성공**)
- 문서: 이 run 문서, `baggage.md` (3.1 PEB 4칸, 3.8 ntdll 전역, 7.6.1 `_HEAP` 머리), `flow.md` 2.4 묶음 7·그림 2·4장.

## 결과

비유 (가게 창고 짓기):

- 창고 관리실 (힙 관리자) 을 먼저 차리고 → 창고 하나 (프로세스 힙) 를 짓고 → 창고 주소를 가게 장부 (PEB `ProcessHeap`) 에 적는다.
- 관리실을 차릴 때 **큰 빈 땅 둘** (32MB + 4KB, 4GB + 128KB) 을 미리 맡아 둔다. run44 사진의 큰 예약 둘이 이것이다.
- 창고 자체는 1MB 땅을 빌려 앞쪽을 조금 (무작위) 잘라 내고 시작한다 — 창고 위치를 매번 흔들려는 것.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x9ed` | `+0x9f2` | `ntdll!RtlInitializeHeapManager` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xa34` | `+0xa39` | `ntdll!RtlSetHeapInformation` (조건부, test.exe 안 탐) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xa4b` | `+0xa50` | `ntdll!LdrpInitializeProcessHeap` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xb78` | `+0xb7d` | `ntdll!RtlCreateTagHeap` (test.exe 는 0 반환) | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0xd` | `+0x12` | `ntdll!RtlHpGlobalsInitialize` | [정적] |
| `ntdll!RtlHpGlobalsInitialize` | `+0x4c` · `+0x5c` | `+0x51` · `+0x61` | `ntdll!RtlRandom` (PDB 별칭 `RtlRandomEx`) — (run61) 첫 호출이 `+0x4e`/`+0x53` `RtlpInitRandomExVector` → `+0x24`/`+0x29` `NtQueryInformationProcess` (#1) | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x28` | `+0x2d` | `ntdll!RtlpHpOptIntoSegmentHeap` | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x56` · `+0x67` | `+0x5b` · `+0x6c` | `ntdll!SbSelectProcedure` → 간접 (cfg) — 앞 반환 ≠ 0 일 때만, test.exe 안 탐 | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x70` | `+0x75` | `ntdll!RtlpHpInitializePerfPolicies` | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x78` | `+0x7d` | `ntdll!RtlpHpQueryGCTimerInterval` | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x9d` | `+0xa2` | `ntdll!RtlpGetModifiedProcessCookie` | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x12a` | `+0x12f` | `ntdll!RtlInitializeCriticalSectionEx` (`RtlpProcessHeapsLock`) | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x174` | `+0x179` | `ntdll!RtlGetSuiteMask` | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x191` | `+0x196` | `ntdll!RtlpInitializeLowFragHeapManager` | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x1a0` | 꼬리 JMP | `ntdll!RtlHpInitializeHeapManager` | [정적] |
| `ntdll!RtlHpInitializeHeapManager` | `+0x3e` | `+0x43` | `ntdll!RtlHpHeapManagerInitialize` | [정적] |
| `ntdll!RtlHpInitializeHeapManager` | `+0xa9` | `+0xae` | 시스템 정보 조회 스텁 (클래스 `0x3e`) — `alias:` 표시, 후보 `NtQuerySystemInformation` | [정적] |
| `ntdll!RtlHpInitializeHeapManager` | `+0xd8` | `+0xdd` | `ntdll!RtlHpHeapManagerStart` | [정적] |
| `ntdll!LdrpInitializeProcessHeap` | `+0x191` | `+0x196` | `ntdll!RtlpCreateHeap` | [정적] |
| `ntdll!LdrpInitializeProcessHeap` | `+0x214` | `+0x219` | `ntdll!NtOpenPartition` (조건부, 안 탐) | [정적] |
| `ntdll!LdrpInitializeProcessHeap` | `+0x264` | `+0x269` | `ntdll!RtlpHpLfhContextEnable` (세그먼트 힙일 때만, 안 탐) | [정적] |
| `ntdll!RtlpCreateHeap` | `+0xe07` | `+0xe0c` | 시스템 정보 조회 스텁 (클래스 0) — `alias:` 표시 | [정적] |
| `ntdll!RtlpCreateHeap` | `+0x459` | `+0x45e` | `ntdll!NtAllocateVirtualMemory` (예약) | [정적] |
| `ntdll!RtlpCreateHeap` | `+0x4a1` | `+0x4a6` | `ntdll!RtlpSecMemFreeVirtualMemory` (n ≠ 0) | [정적] |
| `ntdll!RtlpCreateHeap` | `+0x512` | `+0x517` | `ntdll!NtAllocateVirtualMemory` (확정) | [정적] |
| `ntdll!RtlpCreateHeap` | `+0x5ff` | `+0x604` | `ntdll!RtlpCreateHeapEncoding` | [정적] |
| `ntdll!RtlpCreateHeap` | `+0x6df` | `+0x6e4` | `ntdll!RtlInitializeCriticalSectionEx` (힙 안 락) | [정적] |
| `ntdll!RtlpCreateHeap` | `+0x746` | `+0x74b` | `ntdll!RtlpInitializeHeapSegment` | [정적] |
| `ntdll!RtlpCreateHeap` | `+0x797` | `+0x79c` | `ntdll!RtlpPopulateListIndex` | [정적] |
| `ntdll!RtlpCreateHeap` | `+0x915` | `+0x91a` | `ntdll!RtlAllocateHeap` (힙 자신에서 `0x100`바이트, 본문 범위 밖) | [정적] |
| `ntdll!RtlpCreateHeap` | `+0x96f` | `+0x974` | `ntdll!RtlpProcessHeapsInsert` | [정적] |
| `ntdll!RtlpCreateHeap` | `+0xbf2` · `+0xc18` · `+0xc35` | `+0xbf7` · `+0xc1d` · `+0xc3a` | `RtlpHpConvertCreationFlags` · `RtlpHpHeapCreate` · `RtlpProcessHeapsInsert` — 세그먼트 힙 길, 안 탐 | [정적] |
| `ntdll!RtlCreateHeap` | `+0x1d` | `+0x22` | `ntdll!RtlpCreateHeap` (껍데기, 7번째 인자 0 을 붙임) | [정적] |

### 호출 관계 (보통 길, 대표 지점)

```
LdrpInitializeProcess
 ├─ (1) +0x9f2  RtlInitializeHeapManager(&이미지 이름)            [정적]
 │       ├─ +0x12   RtlHpGlobalsInitialize           난수 상태·키 (RtlpHpHeapGlobals)
 │       │       ├─ +0x51   RtlRandom (첫 호출) → +0x53 RtlpInitRandomExVector → +0x29 NtQueryInformationProcess 0x24 (#1)   (run61)
 │       │       └─ +0x61   RtlRandom (둘째 — 상태 2, 시스템 콜 없음)   (run61)
 │       ├─ +0x2d   RtlpHpOptIntoSegmentHeap         세그먼트 힙 조건 9개 → test.exe 0 (NT 힙)
 │       ├─ +0x75   RtlpHpInitializePerfPolicies     RtlpHpLfhPerfFlags
 │       ├─ +0x7d   RtlpHpQueryGCTimerInterval       NtOpenKey ×2 (#2·#3) → GC 간격 기본 1000ms
 │       ├─ +0xa2   RtlpGetModifiedProcessCookie     NtQueryInformationProcess 0x24 (#4)
 │       │  +0xe7 ~ +0x10a  PEB NumberOfHeaps = 0 · ProcessHeaps = &RtlpPebHeapListStaticBuffer · MaximumNumberOfHeaps = 0x10
 │       │  +0x116 · +0x123  RtlpProcessHeaps 빈 목록
 │       ├─ +0x12f  RtlInitializeCriticalSectionEx   RtlpProcessHeapsLock
 │       │  +0x16d  RtlpHeapKey
 │       ├─ +0x196  RtlpInitializeLowFragHeapManager  NtQuerySystemInformation 0 (#5) → RtlpAffinityState, RtlpLFHKey
 │       └─ +0x1a0 ⇒ RtlHpInitializeHeapManager (꼬리 JMP — 콜스택엔 RtlInitializeHeapManager 프레임 없음)
 │               ├─ +0x43   RtlHpHeapManagerInitialize   RtlpHpHeapManager 0 으로·칸 준비
 │               │  +0xae   NtQuerySystemInformation 0x3e (#6)
 │               └─ +0xdd   RtlHpHeapManagerStart        큰 예약 둘 (#7·#8 32MB+4KB, #9 4GB+128KB)
 │  +0xa34  RtlSetHeapInformation(0,1,0,0)   LdrSystemDllInitBlock+0xa0 비트 12~13 이 1 일 때만 — test.exe 안 탐
 └─ (2) +0xa50  LdrpInitializeProcessHeap(LoadConfig, NT 헤더, 매개변수, &PEB.ProcessHeap)   [정적]
         │  +0x11f  LdrpHeapPartitionName = 매개변수 HeapPartitionName (빈 값)
         ├─ +0x196  RtlpCreateHeap(2, Base 0, 0x100000, 0x1000, Lock 0, &매개변수, 0)   NT 힙 길
         │       ├─ +0xe0c  NtQuerySystemInformation 0 (#10)   → 0x1ce6e8 캐시
         │       ├─ +0x45e  NtAllocateVirtualMemory 예약 (#11)  1MB + 무작위 몫
         │       ├─ +0x4a6  RtlpSecMemFreeVirtualMemory (#12, n ≠ 0)  앞쪽 몫 풀기
         │       ├─ +0x517  NtAllocateVirtualMemory 확정 (#13)  8KB
         │       │  +0x5a1 ~  _HEAP 머리 칸
         │       ├─ +0x604  RtlpCreateHeapEncoding
         │       ├─ +0x74b  RtlpInitializeHeapSegment     첫 세그먼트 (힙 자신)
         │       ├─ +0x79c  RtlpPopulateListIndex          블록 색인
         │       ├─ +0x91a  RtlAllocateHeap(힙, 0x80000a, 0x100)   FrontEndHeapUsageData (본문 범위 밖)
         │       └─ +0x974  RtlpProcessHeapsInsert         RtlpProcessHeaps 노드 · ProcessHeaps[0] · NumberOfHeaps = 1
         └─ +0x1a2  MOV [R15],RAX  → PEB +0x30 ProcessHeap = 힙 주소
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 두 칸 들여쓴 `+0x..` 줄 (호출 기호 없음) 은 그 함수 안 쓰는 줄.
- 콜스택 기대 (큰 예약 때 `k`): `RtlpHpEnvAllocVA+0xe2` ← `RtlCSparseBitmapStart+0xc0` (또는 `+0x119`) ← `RtlHpHeapManagerStart+0x31` (VA 관리자는 `RtlpHpVaMgrCtxStart+0x5e` ← `RtlHpHeapManagerStart+0x56`) ← `RtlHpInitializeHeapManager+0xdd` ← `LdrpInitializeProcess+0x9f2` ← `_LdrpInitialize+0x16c` — `RtlInitializeHeapManager` 는 안 보인다 (꼬리 JMP). `[정적]`

### 갈림길 — test.exe 가 가는 쪽 (질문 3)

| 갈림길 | 조건 (위치) | test.exe | 근거 |
|---|---|---|---|
| 세그먼트 힙 / NT 힙 (`RtlpHpOptIntoSegmentHeap`, 조건 9개를 순서대로, 하나라도 맞으면 거기서 끝) | ① 전역 `RtlpLowFragHeapGlobalFlags` 비트 3 (끔) · ② 비트 4 (켬) · ③ 이름 없는 전역 `0x1ce268 & 3 == 1` · ④ `PEB.pShimData` 안 두 바이트 · ⑤ 스위트 마스크 비트 16 · ⑥ 매니페스트 `heapType = SegmentHeap` · ⑦ 패키지 앱 · ⑧ 매개변수 `HeapPartitionName` · ⑨ 이미지 이름 목록 24개 | **전부 아님 → 0 → NT 힙** (④ 바이트 값만 못 봄) | `[정적]` + 사진: 힙 `+0x10` = `0xffeeffee` (NT 힙 표시. 세그먼트 힙이면 `0xddeeddee`), `RtlpHpHeapFeatures` = 0 |
| `RtlpCreateHeap` 안 NT → 세그먼트 승격 | 플래그 `0x100` 또는 `RtlpHpHeapFeatures & 1` + 자라는 힙·`Base`·`Lock` NULL | 안 탐 | 위와 같음 |
| `+0xa34 RtlSetHeapInformation(0,1,0,0)` | `LdrSystemDllInitBlock+0xa0` 첫 8바이트 `>>12 & 3 == 1` (PDB `PS_MITIGATION_OPTION_HEAP_TERMINATE` = 3, 비트 배치는 `[추측]`) | 안 탐 | 사진 `ntdll+0x1ea4f0` = 0 |
| `RtlInitializeHeapManager +0x67` 간접 호출 | ①-b 반환 ≠ 0 일 때만. 대상 후보 둘: 3바이트 `ret` 빈 함수 (`rva 0xfac60`, 같은 주소 이름 26개 — 결과서가 고른 이름 `RtlpHpAppCompatDontChangePolicy`) / `RtlpHpAppCompatEnablePaddingAndLFHSubsegmentCommit` (`rva 0x144160`) | 안 탐 | 사진 `g_RegInfo+8` (`0x1ce698`) = 0 |
| 디버그 힙 `RtlDebugCreateHeap` (`RtlpCreateHeap +0xe60`) | `NtGlobalFlag` 비트 `0x40`·`0x80` 등 | 안 탐 | 사진 `NtGlobalFlag` = 0 |
| 페이지 힙 `RtlpDebugPageHeapCreate` (`+0x11e5`·`+0xff9`) | 전역 `0x1cc578` / `0x1cc588` ≠ 0 | 안 탐 | 사진 0 |
| 태그 힙 (`RtlpCreateHeap +0x558`, `RtlCreateTagHeap`) | `NtGlobalFlag & 0x800` | 안 탐 — `RtlCreateTagHeap` 는 0 반환 | 사진 `NtdllBaseTag`·`RtlpGlobalTagHeap` = 0 |
| 파티션 힙 (`LdrpInitializeProcessHeap +0x100 ~ +0x111` → `NtOpenPartition`) | `HeapPartitionName.Buffer ≠ 0` | 안 탐 | 사진 P1 빈 값 |

- `RtlCreateHeap` (`rva 0xaa330`, 39바이트) = `RtlpCreateHeap` **껍데기**다 (같은 6개 인자 + 7번째 0). 다만 `RtlInitializeHeapManager` 없이 부르면 안 된다 — `RtlpProcessHeapsInsert` 가 기대는 목록 머리·락·PEB 힙 배열·난수 상태가 없다 (목록 꼬리 0 을 따라가 접근 위반, 또는 `INT 0x29`). `[정적]`
- 첫 힙만 특별하다: `RtlpProcessHeapsInsert` 는 `PEB.ProcessHeap` 이 0 이면 **새 힙 자신**에서 노드 (`0x30`B) 를 받고 `NumberOfHeaps` 를 1 로 쓴다. 다음 힙부터는 프로세스 힙에서 받는다. `[정적]`

### 쓰는 상태 (질문 5) — 짐 지도로 옮김

- **PEB 4칸** (`baggage.md` 3.1):
  - `+0x30 ProcessHeap` ← `LdrpInitializeProcessHeap +0x1a2` (`R15` = 칸 주소, `LdrpInitializeProcess +0xa39` 의 `LEA R9,[RDI+0x30]`). `RtlpCreateHeap` 반환이 0 이 아닐 때 (`+0x19c` 검사).
  - `+0xe8 NumberOfHeaps` ← `RtlInitializeHeapManager +0xe7` (0) → `RtlpProcessHeapsInsert +0xb9` (1).
  - `+0xec MaximumNumberOfHeaps` ← `RtlInitializeHeapManager +0x10a` (`0x10`).
  - `+0xf0 ProcessHeaps` ← `RtlInitializeHeapManager +0xf5` (`&RtlpPebHeapListStaticBuffer`). 배열 `[0]` = 힙 주소 ← `RtlpProcessHeapsInsert +0xca`.
  - 사진 P1 → P1a: 0·0·null → 1 · `0x10` · `7ffafb833ec0` (`[0]` = `25996f30000`), `ProcessHeap` = `25996f30000`.
- **ntdll 전역** (`baggage.md` 3.8): 이름 있는 것 17개가 "쓴 자리 아는 것" 이 됨 + 이름 없는 자리 5묶음 (`0x1ce270~0x1ce280` GC 타이머, `0x1ce430` IFEO 바탕 키 핸들, `0x1ce6e8`·`0x1ce6f0` 사용자 공간 최대 주소, `RtlpHpHeapManager` 끝쪽 `0x1d21a0~0x1d21d8`). 이름 없는 자리는 다른 이름의 짐작 크기 안에 있어 전역 수 (438) 는 안 바뀐다.
- **`_HEAP` 머리** (`baggage.md` 7.6.1, 체크리스트 밖 짐): 힙 하나를 만드는 동안 `RtlpCreateHeap`·`RtlpCreateHeapEncoding`·`RtlpInitializeHeapSegment`·`RtlpProcessHeapsInsert` 가 쓰는 칸 25줄.

### run44 사진과 맞춰 보기 (질문 6)

| 사진 VAD (P1a, 쪽 번호) | 쪽 수 | 확정 | 정적 계산 | 맞음 | 주인 |
|---|---|---|---|---|---|
| `25996f30 – 2599702f` | `0x100` (1MB) | 9쪽 | 예약 `0x100000`, 첫 확정 `0x2000` → 뒤에 9쪽으로 자람 (`NumberOfUnCommittedPages` `0xf6`) | 예 | 프로세스 힙 (`RtlpCreateHeap +0x45e / +0x517`) |
| `7ff4ed070 – 7ff5ed08f` | `0x100020` (4GB + 128KB) | 0 | `0x100000000` + `0x20000` | **정확히 같음** | VA 관리자 희소 비트맵 (#9). 시작 = 덤프 `0x1ce958+0x28` |
| `7ff5ed090 – 7ff5ef090` | `0x2001` (32MB + 4KB) | 1쪽 | `0x2000000` + `0x1000`, 확정 1쪽 (#7·#8) | **정확히 같음** | 힙 관리자 희소 비트맵 (`RtlHpHeapManagerStart`). 시작 = 덤프 `0x1ce910+0x8` |

- 요청서 짐작 "4GB·32MB+4KB 예약은 힙 관리자 것" 이 맞다. 정확한 크기는 4GB 가 아니라 **4GB + 128KB** (`0x100020000`).
- 위에서부터 (`MEM_TOP_DOWN`) 32MB 쪽이 위, 4GB 쪽이 바로 아래다.

## 확인된 것

- **test.exe 프로세스 힙은 NT 힙이다.** `[정적]` 세그먼트 힙 조건 9개 중 8개가 test.exe 값으로 "아님", ④ 는 바이트를 못 봤지만 결과는 사진 (`+0x10` = `0xffeeffee`, `+0x14` = 2, `RtlpHpHeapFeatures` = 0) 이 정했다. (결과서 질문 1·3)
- **구간 시스템 콜 ~~12개~~ → 13개** (run61 정정) (`NtOpenKey` 2 · `NtQueryInformationProcess` ~~1~~ **2** · `NtQuerySystemInformation` 3 · `NtAllocateVirtualMemoryEx` 3 · `NtAllocateVirtualMemory` 2 · `NtFreeVirtualMemory` 1). 맨 앞 표. `[정적]`
- **큰 예약 둘은 힙 관리자의 희소 비트맵**이고, 크기 `0x2001000`·`0x100020000` 이 사진 VAD 쪽 수와 정확히 같다. 둘 다 사용자 공간 끝 (`NtQuerySystemInformation` 0x3e) 에서 계산된다. `[정적]` + 사진.
- **PEB `+0x30 ProcessHeap` 을 쓰는 줄 = `LdrpInitializeProcessHeap +0x1a2`** (`.asm` `MOV qword ptr [R15],RAX`). run18 의 "칸 주소를 `+0xa39` 에서 넘김" 과 이어진다. `[정적]`
- **PEB 힙 목록 3칸은 힙보다 먼저** `RtlInitializeHeapManager` 가 준비하고 (`NumberOfHeaps` 0, 정적 배열, 최대 16), 첫 힙을 넣을 때 `RtlpProcessHeapsInsert` 가 `NumberOfHeaps` = 1·배열 `[0]` 을 쓴다. `[정적]`
- **IFEO 는 이 구간에서 처음 열린다** (`RtlpHpQueryGCTimerInterval`). 앞 `LdrpInitializeExecutionOptions` 는 매개변수 `Flags` (`0x4001`, 사진 P1) 의 `0x4000` 비트 때문에 IFEO 읽기를 건너뛴다. `[정적]` (비트 이름 `RTL_USER_PROC_IMAGE_KEY_MISSING` 은 `[추측]`)
- 힙 주소 무작위: 예약을 `0x100000 + n×0x10000` 로 받고 앞 `n×0x10000` 을 풀어 시작을 흔든다. n 의 출처는 `RtlpHpHeapGlobals +0x40` xorshift 난수 (씨앗 = TEB 스레드 ID ^ KUSD 틱 값 → `RtlRandom` 두 번). ~~시스템 콜 없이 만든다.~~ → (run61 정정) 첫 `RtlRandom` 만 `RtlpInitRandomExVector` 안 `NtQueryInformationProcess(0x24)` 1번 (#1, 난수 벡터 첫 켜기). 그 뒤 난수는 시스템 콜 없이 만든다. `[정적]`
- `RtlCreateHeap` = `RtlpCreateHeap` 껍데기 (7번째 인자 0). `[정적]`
- 힙 안 첫 블록 둘: `FrontEndHeapUsageData` (`0x100`B, 힙 `+0x750`), `ProcessHeaps` 노드 (`0x30`B, 힙 `+0x860`). 첫 빈 블록 = 힙 `+0x740`. 그 뒤 블록은 `RtlAllocateHeap` 본문이라 범위 밖. `[정적]` (+ 사진 `FirstEntry` `…f30740`, `RtlpProcessHeaps` Flink = `25996f30860`)

## 정정

- **run18 묶음 7 · run45 일 52 — `RtlSetHeapInformation` 은 조건부다.** run18 은 묶음 7 을 "힙 관리자 → 힙 정보 → `LdrpInitializeProcessHeap`" 로, run45 일 52 는 사슬에 `+0xa39 RtlSetHeapInformation` 을 조건 없이 적었다. 실제로는 `LdrSystemDllInitBlock+0xa0` 첫 8바이트의 비트 12~13 이 1 일 때만 부르고 (`LdrpInitializeProcess +0xa1a ~ +0xa28`), test.exe 는 0 이라 **안 탄다** (사진 `ntdll+0x1ea4f0` = 0). 부르면 클래스 1 "손상 때 종료" 를 켠다 (시스템 콜 없음). run18·run45 맨 위에 정정 한 줄.
- **run45 일 52 — "태그 힙" 은 test.exe 에선 만들지 않는다.** `RtlCreateTagHeap` (`+0xb78`) 은 `NtGlobalFlag & 0x800` 이 아니면 `+0x45 JZ` 로 0 을 돌려준다 → `NtdllBaseTag` = 0, `RtlpGlobalTagHeap` = 0 (사진). 같은 정정 줄에 넣음.
- **짐 지도 3.1 · 3.8 (이 스프린트 문서라 바로 고침)**: PEB `+0x30` 줄 "쓰는 줄은 callee 안 (안 봄)" → `LdrpInitializeProcessHeap +0x1a2`. `LdrpHeap` 줄 "묶음 7 '로더 힙' — 이름으로 본 뜻 `[추측]`" → 별도 로더 힙이 아니라 `PEB.ProcessHeap` 값 복사 (run18 "LdrpHeap = PEB 힙" + 사진 같은 값).

## 아직 확인 필요

- 동적 (결과서 번호 그대로, plan **Q91** 로):
  - **필요 (정적으로 못 정함)**:
    - **D1** `PEB.pShimData` 안 두 바이트 (`+0x11aa` 비트 `0x40`, `+0x11b2` 비트 `0x4`) — NT 힙 결과는 정해졌지만 "어느 조건이 정했나" 를 못 가림. 멈춤점 `ntdll!RtlpHpOptIntoSegmentHeap+0x190` (`r rax` · `db @rax+0x11a8 L0x10`). 덤프로도 가능할 수 있음 (`25996d10000` 2쪽이 덤프에 있으면).
    - **D4** IFEO 열기 두 번의 결과와 "이 프로세스 첫 IFEO 열기" 인지 — `ntdll!RtlpOpenBaseImageFileOptionsKey+0x80` (`r eax` 0, 핸들 `0x10` 근처), `ntdll!RtlpOpenImageFileOptionsKeyEx+0xec` (`r eax` 기대 `0xc0000034`), 처음 걸릴 때 `k`.
    - **D5** `NtQueryInformationProcess(0x24)` 반환·쿠키 — `ntdll!RtlpGetModifiedProcessCookie+0x27` (`r eax`, `dd @rsp+0x40 L1`). 값이 곧 0 으로 덮여 사진으론 못 봄.
    - **D9** 무작위 몫 n 과 `NtFreeVirtualMemory` 실제 여부 — `ntdll!RtlpCreateHeap+0x459` (`dq @rsp+0x90 L1` 몫, `dq @rsp+0x98 L1` 크기), `+0x4a1` 에 서나, `RtlpSecMemFreeVirtualMemory+0x2f` `r eax`.
  - **사진·정적으로 이미 닫힘 (확인만, 낮은 우선순위)**:
    - **D2** `RtlpHpOptIntoSegmentHeap` 반환·`*param_2` (기대 0·0) — 사진으로 간접 답됨.
    - **D3** 매니페스트 `heapType` 조회가 음수로 끝나는지 (`RtlpHpOptIntoSegmentHeap+0x1eb`) — 정적 `[추측]`, 사진으론 안 됨. 선택.
    - **D6** `NtQuerySystemInformation` 세 번의 순서·반환 — 세 번 다 성공은 사진으로 간접 답됨, 순서만 동적.
    - **D7** 힙 관리자 예약 세 번의 인자·번지 (`RtlpHpEnvAllocVA+0xdd`) — 번지·크기·확정은 사진과 일치, 순서·`MEM_TOP_DOWN` 효과만 동적.
    - **D8** `RtlpCreateHeap` 첫 입구 인자 (2, 0, `0x100000`, `0x1000`, Lock 0, 매개변수) — 정적으로 확정.
    - **D10** `+0xa34` 안 타는지 — 사진 (`LdrSystemDllInitBlock+0xa0` = 0) 으로 답됨, 필요 없음.
    - **D11** `RtlInitializeHeapManager +0x67` 간접 호출 안 타는지 — 사진 (`g_RegInfo+8` = 0) 으로 답됨.
    - **D12** (선택) 구간 앞 `Segment Heap` 레지스트리 키 유무 — 간접 답됨 (전역 비트 3·4 꺼짐이라 효과 없음).
    - **D13** (선택) 힙 머리 나머지 칸 값 — 덤프 `dq 25996f30000 L0x5a` 로 정적 계산 검증 (덤프 읽기는 이번 범위 밖).
- 정적 (나머지 Q78 이 넘겨받는 것):
  - 조건부로만 쓰는 힙 전역 (`RtlpHpHeapFeatures`·`RtlpHpAppCompatFlags`·`RtlpGlobalTagHeap`·`g_RegInfo`·`RtlpLowFragHeapGlobalFlags`·`RtlpLargestLfhBlock`·`RtlpDisableHeapLookaside`·`RtlpLowFragHeapRandomData`) — 쓰는 줄은 결과서에 있으나 이번엔 "닫힌 칸" 으로 안 셈 (baggage 7.5). 경로를 다 잇고 3장으로 옮길지는 나머지 Q78.
  - 묶음 7 의 힙 **뒤** 부분 (`+0xa50 ~ +0xb7d`: 기능 스위치, 로더 스냅 버퍼, 활성화 컨텍스트 스택, `+0xb52` 클래스 `0xc5` → `RtlpHypervisorSharedUserVa`) 본문은 안 봄.
  - 이 조각이 만든 VAD 3개의 커널 쪽 `_MMVAD` 칸 (baggage 7.6, Q79 VAD 노드 `dt`).

## 도구 메모

- (결과서 제안, 하네스라 오케스트레이터가 사용자에게) PDB 구조체 칸을 한 줄 명령으로 뽑는 도구 — sast/26 에 이어 두 번째 제안.
- `refs`·`.asm` 전역 주석은 "가장 가까운 앞쪽 이름 + 칸" 으로 보인다. `RtlpDisableHeapLookaside+0x8` (= 사용자 공간 최대 주소 캐시) 처럼 **이름 없는 별개 변수**가 앞 이름의 일부처럼 보이니, 짐 지도 전역 크기 짐작 (다음 이름까지 거리) 에 그런 자리가 묻혀 있을 수 있다 (이번에 다섯 묶음).
- 이번 바퀴에 `pdb_aliases.py --lookup ntdll` 표 (`ms/sym/ntdll.aliases.tsv`) 가 없었다. `alias:NtQuerySystemInformation+1` 의 이름 전부는 못 봤다 (결과서는 스텁 번호로 맞춤).

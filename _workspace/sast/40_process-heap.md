# 40 process-heap — 정적 분석 요청

- 날짜: 2026-10-27
- 스프린트: 03_sprint (짐 지도 바퀴, 예정 run46)
- 대기열: **Q78a** — Q78 (짐 지도 C, 쓴 자리·줄기) 에서 떼어 낸 첫 조각 "ntdll 프로세스 힙 만들기 구간" (사용자 요청 2026-10-27). 나머지 Q78 은 바로 이어서 한다.
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntdll.dll (PDB GUID 확인), 샘플 test.exe
- 관련:
  - `runs/02_sprint/run18-proc-init-setup.md` 묶음 7 (`LdrpInitializeProcess +0x9e5 ~ +0xb7d`, 호출 13곳 표만 있음)
  - `runs/03_sprint/baggage.md` 3장 PEB `+0x30 ProcessHeap` 줄 ("쓰는 줄은 callee 안 (안 봄)"), 2.1 의 `P1~P1a` 칸 (`NumberOfHeaps`·`MaximumNumberOfHeaps`·`ProcessHeaps`), 3.8 전역 `LdrpHeap`, `≤P1a` 전역 목록 (`RtlpHeapKey`·`RtlpLFHKey`·`RtlpHpHeapManager`·`RtlpHpHeapGlobals*`·`RtlpProcessHeaps*`·`RtlpProcessHeapsLock`·`RtlpPebHeapListStaticBuffer`·`RtlpLowFragHeapGlobalFlags`·`RtlpLowFragHeapRandomData*`·`RtlpHpLfhPerfFlags`·`RtlpHpHeapVaCallbacksRegistrar`·`RtlpDisableHeapLookaside`·`RtlpLargestLfhBlock`·`RtlpHeapFailureInfo`·`RtlpDebugPageHeapTable`·`RtlpSecMemListHead` 등)
  - `runs/03_sprint/run44-baggage-snap.md` (P1 → P1a 사진 차이), `run45-work-list.md` (`RtlpInitParameterBlock` 이 이 힙에서 블록을 받음)
  - 이전 요청 sast/38 (체크리스트), sast/39 (일 목록)

## 왜 이 구간을 먼저 (Q78a)

- Q78 범위 (체크리스트 21종 + ntdll 전역) 안의 PEB `+0x30 ProcessHeap`·`+0xe8 NumberOfHeaps`·`+0xec MaximumNumberOfHeaps`·`+0xf0 ProcessHeaps` 와 ntdll 힙 전역은 run44 에서 "P1~P1a 사이에 값이 생김" 만 봤다. 쓴 자리는 전부 `LdrpInitializeProcess` 묶음 7 안이라 구간 하나로 한꺼번에 닫는다.
- 이 힙은 뒤 칸들의 출발점이다 (매개변수 블록·환경, 모듈 항목·노드, TLS, 활성화 컨텍스트 스택이 다 여기서 블록을 받음). 먼저 닫아 두면 나머지 Q78 에서 "이 블록은 어디서 왔나" 를 칸마다 다시 파지 않는다 (공용 부품은 한 번만).
- `ms/decomp/ntdll/` 에 `RtlAllocateHeap.c`·`RtlFreeHeap.c` 만 있고, 아래 함수들은 `.c` 가 없다.

## 알고 싶은 것

1. **호출 사슬과 순서** (call/ret 오프셋과 함께)
   - `LdrpInitializeProcess +0x9ed RtlInitializeHeapManager` → (조건) `+0xa34 RtlSetHeapInformation` → `+0xa4b LdrpInitializeProcessHeap` → `RtlpCreateHeap` 아래.
   - `RtlCreateHeap` (`0xaa330`, 39바이트) 는 `RtlpCreateHeap` 껍데기인가? 예/아니오 + 넘기는 인자.
   - `RtlCreateHeap` 만 따로 부르면 안 되는 이유가 있나? (예: `RtlpProcessHeapsInsert` 가 `PEB.ProcessHeaps` 준비 — `RtlInitializeHeapManager` 쪽에서 한 일 — 에 기대는지)
   - `RtlpCreateHeap` 은 NT 힙 길 (`+0x459/+0x512 NtAllocateVirtualMemory` → `RtlpCreateHeapEncoding` → `RtlpInitializeHeapSegment` → `RtlpPopulateListIndex` → `+0x96f RtlpProcessHeapsInsert`) 과 세그먼트 힙 길 (`+0xbf2 RtlpHpConvertCreationFlags` → `+0xc18 RtlpHpHeapCreate` → `+0xc35 RtlpProcessHeapsInsert`) 이 갈라지는 것으로 보인다 (지도 기준). 가르는 조건은 무엇이고, test.exe 의 프로세스 힙은 어느 길인가?
2. **보통 길 (test.exe) 에서 나오는 시스템 콜 전부**
   - 줄마다: 부르는 함수 (+오프셋), 시스템 콜 이름, 정보 클래스 번호 (PDB 열거형에 이름이 있으면 `[정적] PDB` 로), 버퍼 크기, 횟수, 타는 조건.
   - 특히:
     - `NtQuerySystemInformation` 클래스 0 (0x40바이트, 몇 번), 클래스 0x3e (실패하면 무엇을 건너뛰나). `RtlpCreateHeap +0xe07` 의 클래스도.
     - `NtQueryInformationProcess` 0x24 (4바이트)
     - IFEO `NtOpenKey` (`RtlpOpenImageFileOptionsKeyEx`: 바탕 키·exe 키, 키가 없을 때의 길)
     - `NtAllocateVirtualMemory` / `NtAllocateVirtualMemoryEx` / `NtFreeVirtualMemory`: 예약·확정 크기, 무작위 몫 계산 (어디서 무작위 값을 얻나)
     - `LdrpInitializeProcessHeap +0x214 NtOpenPartition` 을 타는 조건
     - `RtlpCreateHeap +0xd60 / +0x1043 NtQueryVirtualMemory` 를 타는 조건
3. **갈림길** — 각각 test.exe 가 어느 쪽을 탈지 적는다. 정적으로 못 정하면 동적 질문으로 넘긴다.
   - 세그먼트 힙 / NT 힙을 고르는 조건 전부: IFEO 전역 비트, 이름 없는 전역 rva `0x1ce268` (`_refs` 로 누가 읽고 쓰는지 찾기), `PEB.pShimData`, 매니페스트 heapType, 패키지 앱 비트, 이미지 이름 목록.
   - 디버그 힙 (`RtlDebugCreateHeap`), 페이지 힙 (`RtlpDebugPageHeapCreate`), 태그 힙 (`RtlCreateTagHeap`, `NtGlobalFlag & 0x800`), 파티션 힙 (매개변수 블록 `HeapPartitionName` → `NtOpenPartition`).
   - `LdrSystemDllInitBlock + 0xa0` 비트 12~13 → `RtlSetHeapInformation(0, 1, 0, 0)` 를 타는 조건 (`+0xa34`).
   - `RtlInitializeHeapManager +0x67` `indirect-cfg` 의 대상 (`+0x56 SbSelectProcedure` 결과). 정적으로 후보를 좁히고, 못 정하면 동적 질문.
4. **읽는 입력** — 오프셋과 PDB 칸 이름으로
   - PEB, `KUSER_SHARED_DATA` (`0x7ffe0000`), TEB, 매개변수 블록, exe NT 헤더·LoadConfig (`ProcessHeapFlags` 등), ntdll 전역, IFEO 값 이름.
   - `KUSER_SHARED_DATA.ProductTypeIsValid` 가 0 일 때 레지스트리를 읽으러 가는가? (`RtlGetSuiteMask` 근처, 예/아니오 + 줄)
5. **쓰는 상태**
   - PEB `+0x30 ProcessHeap` 을 **실제로 쓰는 줄** (run18 은 "callee 안" 이라고만 함, 칸 주소를 `+0xa39` 에서 넘김). `+0xe8 NumberOfHeaps`·`+0xec MaximumNumberOfHeaps`·`+0xf0 ProcessHeaps` 도 같이 (오프셋은 PDB `_PEB` 로 확인).
   - ntdll 전역 중 이 구간이 처음 쓰는 것 — 위 "관련" 의 `≤P1a` 힙 전역 목록을 기준으로, 쓰는 줄 (함수+오프셋) 과 값의 출처. 목록에 없는데 쓰는 전역이 있으면 더한다.
   - `_HEAP`·`_HEAP_SEGMENT` (세그먼트 힙이면 `_SEGMENT_HEAP` 등) 머리 중 여기서 채우는 칸 (PDB 칸 이름으로). **사용자 요청에 들어 있음** — 범위는 "프로세스 힙 하나를 만드는 동안 머리에 쓰는 칸" 까지. 힙에서 블록을 나눠 주는 내부 (`RtlAllocateHeap` 본문, LFH 버킷 동작) 는 이번 범위 밖 (baggage 7.6 "기록만" 결정 2026-10-26 그대로).
   - 결과서에 칸마다 "쓰는 곳" 을 baggage.md 3장 양식 (쓴 자리 / 놓는 법 / 경로) 으로 적어 flow-writer 가 옮길 수 있게.
6. **run44 사진과 맞춰 보기**
   - P1 → P1a 에 새로 생긴 VAD 중 세 개: 1MB 프로세스 힙, 약 4GB 예약 (확정 0), 32MB + 4KB 예약 (확정 1쪽) — 값은 `runs/03_sprint/run44-baggage-snap.md`·`ms/logs/04_*` (Q77c) 에서 찾는다.
   - 뒤의 두 큰 예약이 힙 관리자 (비트맵·VA 관리, `RtlpHpEnvAllocVA` 등) 것인가? 정적으로 계산한 크기가 사진과 맞나? 정적으로 안 닫히면 동적 질문으로 넘긴다.
   - 사진이 이미 답하는 것은 다시 동적으로 묻지 않는다 (덤프 `ms/dump/03_baggage-snap/P1/`, `P1a/` 로 볼 수 있으면 그렇게 적기만 — 덤프 읽기는 이번 범위 밖).

## 대상

rva 는 `ms/decomp/ntdll/_index.tsv` 로 확인함 (2026-10-27, 모두 있음).

| 모듈 | 함수 | rva | depth | 이유 |
|---|---|---|---|---|
| ntdll | `LdrpInitializeProcessHeap` | `0x9e300` | 1 | 몸통 (`+0xa4b`) |
| ntdll | `RtlInitializeHeapManager` | `0x9c334` | 1 | 몸통 (`+0x9ed`) |
| ntdll | `RtlpCreateHeap` | `0xa8820` | 1 | 힙 만들기 본체 (4703바이트, 큰 함수) |
| ntdll | `RtlCreateHeap` | `0xaa330` | 0 | 껍데기 확인 |
| ntdll | `RtlSetHeapInformation` | `0xbe960` | 0 | `+0xa34` 조건 호출 |
| ntdll | `RtlHpGlobalsInitialize` `0x955f0`, `RtlpHpOptIntoSegmentHeap` `0x9c558`, `RtlpHpInitializePerfPolicies` `0x9cd00`, `RtlpHpQueryGCTimerInterval` `0x11fa84`, `RtlpOpenImageFileOptionsKeyEx` `0x59048`, `RtlpGetModifiedProcessCookie` `0x9c4e0` | | 1 | `RtlInitializeHeapManager` 보통 길 |
| ntdll | `RtlpInitializeLowFragHeapManager` `0x9c808`, `RtlHpInitializeHeapManager` `0xebc44`, `RtlHpHeapManagerInitialize` `0xebd2c`, `RtlHpHeapManagerStart` `0x98dbc`, `RtlpHpEnvAllocVA` `0x9a640` | | 1 | 힙 관리자 시작 · 큰 예약 후보 |
| ntdll | `RtlpCreateHeapEncoding` `0xaa26c`, `RtlpInitializeHeapSegment` `0xaa360`, `RtlpPopulateListIndex` `0x5cfbc`, `RtlpProcessHeapsInsert` `0xaa6a4` | | 1 | NT 힙 길 · PEB 힙 목록 |
| ntdll | `RtlCreateTagHeap` `0xdc550` | | 0 | 태그 힙 갈림길 |
| ntdll | (세그먼트 힙 길이면) `RtlpHpConvertCreationFlags`, `RtlpHpHeapCreate` 와 그 보통 길 | `_index.tsv` 로 | 1 | 1번 질문 답에 따라 |

- 필요하면 더 넣어도 된다 (예: `RtlGetSuiteMask`, `SbSelectProcedure`, `RtlpHpLfhContextEnable`). 빠진 게 보이면 이유와 함께 적는다.
- 줄기 위는 끝까지 (더 부르는 게 없거나 syscall), 줄기 밖 callee 는 이름만, 공용 부품 (`memset`, `RtlInitializeCriticalSectionEx`, 락, 로그 `LdrpLogInternal`·`RtlpLogHeap*`, `DbgPrint`, 실패 보고) 은 이름만.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `RtlInitializeHeapManager` 깊이 5, caller 1, expanded. 직접 호출 11줄: `+0xd RtlHpGlobalsInitialize`, `+0x28 RtlpHpOptIntoSegmentHeap`, `+0x56 SbSelectProcedure`, `+0x67 indirect-cfg`, `+0x70 RtlpHpInitializePerfPolicies`, `+0x78 RtlpHpQueryGCTimerInterval`, `+0x9d RtlpGetModifiedProcessCookie`, `+0x12a RtlInitializeCriticalSectionEx`, `+0x174 RtlGetSuiteMask`, `+0x191 RtlpInitializeLowFragHeapManager`, `+0x1a0 tail RtlHpInitializeHeapManager`.
- `LdrpInitializeProcessHeap` 깊이 5, caller 1, expanded. 직접 호출 8줄: `memset` ×2, `+0x191 RtlpCreateHeap`, `+0x1bd __security_check_cookie`, `+0x214 NtOpenPartition`, `+0x241 / +0x2fc LdrpLogInternal`, `+0x264 RtlpHpLfhContextEnable`.
- `RtlpCreateHeap` 깊이 6, caller 8, expanded. 직접 호출 약 45줄. 눈에 띄는 것: `NtAllocateVirtualMemory` (`+0x459`, `+0x512`), `RtlpCreateHeapEncoding` (`+0x5ff`), `RtlpInitializeHeapSegment` (`+0x746`), `RtlpPopulateListIndex` (`+0x797`), `RtlAllocateHeap` (`+0x915`), `RtlpProcessHeapsInsert` (`+0x96f`, `+0xc35`), `RtlpHpHeapCreate` (`+0xc18`), `NtQueryVirtualMemory` (`+0xd60`, `+0x1043`), `NtQuerySystemInformation` (`+0xe07`), `RtlDebugCreateHeap` (`+0xe60`), `RtlpDebugPageHeapCreate` (`+0xff9`).
- `RtlpProcessHeapsInsert` 깊이 7, caller 1 (지도 기준 — 위에서 두 곳이 같은 함수에서 부름). `RtlHpHeapManagerInitialize` 깊이 7.
- 지도 깊이 기준으로 `RtlInitializeHeapManager` 아래 일부는 depth 6~7. 지도가 덜 펼쳐진 곳이 있으면 결과서에 적는다.

## 이미 아는 것

- `[정적]` run18: 묶음 7 = `+0x9e5 ~ +0xb7d`. `+0xa4b / +0xa50 LdrpInitializeProcessHeap`, 실패하면 `+0xa55` 로그 후 `0xc0000017`. PEB `+0x30` 칸 주소를 `+0xa39` 에서 넘김. 이 함수 (`LdrpInitializeProcess`) 가 직접 부르는 `RtlAllocateHeap` 4곳은 모두 그 뒤 (묶음 9). `+0xb12` 에서 `LdrpHeap` = `R12` (PEB 힙). IFEO 값 이름 `DebugProcessHeapOnly` (`+0xcae`).
- `[관찰]` run44: PEB `ProcessHeap` null (P1) → `25996f30000` (P1a 와 P2). `NumberOfHeaps`·`MaximumNumberOfHeaps`·`ProcessHeaps` 와 `CrossProcessFlags`·`TppWorkerpList` 가 `P1~P1a` 에 바뀜. 위 "관련" 의 힙 전역은 `≤P1a`. 모듈 항목 주소는 프로세스 힙 안.
- `[정적]` run45: `RtlpInitParameterBlock` (`+0xc3f`) 이 `RtlAllocateHeap(PEB.ProcessHeap, 0, Length)` 로 매개변수 블록을 이 힙에 옮김.
- `[추측]` (사용자 요청서의 짐작, 맞는지 볼 것): 클래스 0 은 0x40바이트, `NtQueryInformationProcess` 0x24 는 4바이트, 4GB 예약·32MB+4KB 예약은 힙 관리자 것.

## 받고 싶은 것 (결과서 `_workspace/sast/40_process-heap_result.md`)

- **맨 앞 요약 표**: "보통 길 시스템 콜 목록 (함수+오프셋 / 이름 / 클래스 / 크기 / 횟수 / 조건)".
- 질문 1~6 답 + 근거 (`.asm` 의 +0x 줄). 태그 `[정적]` / `[정적] PDB` / `[추측]` / `[공백]`.
- 칸별 "쓰는 곳" 표 (PEB 4칸, ntdll 전역, `_HEAP` 머리) — baggage.md 3장 양식으로.
- Q78 목록에서 이 조각으로 닫히는 칸 목록 (나머지 Q78 이 무엇을 넘겨받는지 알 수 있게).
- **동적 질문 목록** (번호 붙여서): 정적으로 못 정한 갈림길·간접 호출 대상·크기 — 각각 추천 멈춤점 (함수+오프셋) 과 찍을 것. 사진 덤프로 답할 수 있는 건 따로 표시.
- 결과서에 코드를 길게 붙이지 않고 우리 말로 요약한다.

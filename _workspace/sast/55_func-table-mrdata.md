# 55 func-table-mrdata — 정적 분석 요청

- 날짜: 2026-11-03
- 스프린트: 03_sprint (짐 지도 바퀴)
- 대기열: **Q111** (PE_emulator 요청 5, 사용자 결정 2026-11-02: Q109 다음, Q104 VM 단계 앞)
- 관련: run18 (`LdrpInitializeProcess` 묶음), baggage.md 3.9 `.mrdata` (`LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataUnprotected` "쓴 자리 모름", 7장 `≤P1a` 줄 — `LdrpMrdataSize`·`LdrpMrdataBase`·`LdrpInvertedFunctionTables*`), baggage 3.8 "목록 밖 자리", run45 (`LdrInitializeMrdata` 는 이 다섯을 안 씀), run59 (동적 함수표 셋 `_LdrpInitialize +0x10d ~ +0x13e`)
- 왜: PE_emulator 다음 바퀴 (39 = 로드맵 2-B③b, `LdrpInitializeProcess +0x3e6 ~ +0x4a1`) 에서 이 다섯 함수를 "공사 중" 빈 함수로 둠. 본문·쓰는 칸·syscall 이 필요
- 원문: `../PE_emulator/_workspace/00_planner/38_app-headers.md` "internals 에 알릴 것 — 요청 5" 절 (읽기만, 고치지 않음)

## 알고 싶은 것

### 1. ⓡ ntdll 본문 다섯 (급함)

대상 (지도 `_map/LdrInitializeThunk_nodes.tsv`): `RtlInsertInvertedFunctionTable` (rva `0xa4074`, 140B) · `RtlpxLookupFunctionTable` (rva `0x14840`, 1339B) · `RtlRemoveInvertedFunctionTable` (rva `0xa7968`, 46B) · `LdrProtectMrdata` (rva `0xa3ec0`, 195B — 지도 "helper", caller 21) · `LdrControlFlowGuardEnforcedWithExportSuppression` (rva `0xa60c0`, 45B)

(a) **무엇을 쓰나** — 함수마다 쓰는 전역·칸 표 (칸 이름은 PDB 로):
   - `LdrpInvertedFunctionTables` (`.mrdata` rva `0x1e7430`, 0x28B 라고 PE_emulator 가 적음 — 크기·타입 이름 PDB 확인) 머리 (`CurrentSize`·`MaximumSize`·`Epoch`·`Overflow`?) 와 칸 0 (24B? `FunctionTable`·`ImageBase`·`SizeOfImage`·`SizeOfTable`?)
   - `LdrProtectMrdata` 가 `LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataUnprotected` (그리고 `LdrpMrdataLock`?) 를 쓰나 읽기만 하나 — **baggage 3.9 "쓴 자리 모름" 셋이 이 함수 몫인가** (예/아니오 + 줄). 아니면 그 셋을 처음 쓰는 곳 이름만 (찾을 수 있으면)
   - 인자 꼴: `LdrProtectMrdata(인자 하나 = 보호 켬/끔?)` — 끔·켬 갈래, 중첩 셈 (카운터) 이 있나
(b) **syscall 을 부르나** — 특히 `LdrProtectMrdata` → `NtProtectVirtualMemory` (인자: 주소 = `LdrpMrdataBase`?, 크기, 새 보호 `PAGE_READWRITE`/`PAGE_READONLY` 값). 함수마다 syscall·하위 호출 목록 (지도: `RtlInsertInvertedFunctionTable` → `+0x4b LdrProtectMrdata` · `+0x5d RtlpInsertInvertedFunctionTableEntry` (rva `0xa4110`, 227B) · `+0x67 LdrProtectMrdata`; `RtlRemoveInvertedFunctionTable` 은 `RtlxRemoveInvertedFunctionTable` → `RtlpRemoveInvertedFunctionTableEntry` (rva `0xa87a8`, 105B) 로 가는지 확인). 줄기 끝까지 (CLAUDE.md "자세함"): 하위 `RtlpInsert…Entry`·`RtlpRemove…Entry` 본문도 봄
(c) **`LdrpInitializeProcess +0x415 Insert` 뒤 `+0x470 Remove` 를 왜 하나** — 사이 구간 (지도·`.asm` 에서 본 것):
   - `+0x400 RtlImageNtHeaderEx` → `+0x415 RtlInsertInvertedFunctionTable` → `+0x43a RtlpxLookupFunctionTable` → `+0x441 LdrProtectMrdata` → `+0x44e MOVAPS [LdrpInvertedFunctionTables+0x10]` · `+0x45e MOVSD [+0x20]` · `+0x467 MOV dword [+0x20],EBX` → `+0x470 RtlRemoveInvertedFunctionTable` → `+0x47c LdrProtectMrdata` → `+0x481 LdrpCaptureCriticalThunks` → `+0x488 LdrProtectMrdata` → `+0x48d LdrControlFlowGuardEnforcedWithExportSuppression`
   - 무엇을 넣고 (exe? ntdll?) 무엇을 빼나, 직접 쓰기 `+0x44e ~ +0x467` 이 칸 0 에 무엇을 놓나 (값 출처 — `RtlpxLookupFunctionTable` 결과?), 끝난 뒤 표 머리·칸 0 의 **정적 기대 값** (test.exe 기준, 모르면 꼴만)
   - `+0x441`·`+0x47c`·`+0x488` 세 `LdrProtectMrdata` 의 인자 (켬/끔 순서)
(d) **test.exe 에서 `LdrControlFlowGuardEnforcedWithExportSuppression` 반환값** — 무엇을 읽어 결정하나 (전역·PEB 칸·이미지 헤더 비트). run44 사진 `LdrpSuppressedExportOverrideListPtr` 끝까지 0 → 1 이 아님 `[추측]` (PE_emulator) 이 맞나. `+0x492` 뒤 갈래가 무엇을 하는지 이름만
(e) `RtlpxLookupFunctionTable` 은 1339B 로 크다 — **`+0x43a` 이 부를 때 타는 갈래만** 따라가고 나머지는 갈래 조건·이름만. 다른 caller (`RtlLookupFunctionEntry +0x48` 등) 는 이름만

### 2. ⓢ 작은 덤 (급하지 않음)

- `LdrpHandleUserCopyAttributeList` (rva `0x15ddb0`, `.asm` 있음) `+0x82` 가 쓰는 `.data` rva `0x1ce268` 의 이름·뜻 — PDB 공개 이름 없음 (PE_emulator 확인). 가까운 공개 이름과 거리 (`LdrpThunkSignature` 짐작 크기 안이라는데 맞나), 그 칸을 읽는 함수가 있나 (이름만)
- 속성 번호 `0xa0022` 이 무엇인가 (매개변수 블록 `+0x448` 속성 목록 — `PS_ATTRIBUTE_*` 꼴이면 번호 풀이: 번호·스레드/입력/추가 비트), 값 아래 두 비트 뜻

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^RtlInsertInvertedFunctionTable$`, `^RtlpInsertInvertedFunctionTableEntry$` | 1 | 1(a)(b) |
| ntdll | `^RtlRemoveInvertedFunctionTable$`, `^RtlxRemoveInvertedFunctionTable$`, `^RtlpRemoveInvertedFunctionTableEntry$` | 1 | 1(a)(b) |
| ntdll | `^LdrProtectMrdata$` | 1 | 1(a)(b) |
| ntdll | `^LdrControlFlowGuardEnforcedWithExportSuppression$` | 1 | 1(d) |
| ntdll | `^RtlpxLookupFunctionTable$` | 1 | 1(e) — `+0x43a` 갈래만 |
| ntdll | `^LdrpInitializeProcess$` | 0 | 1(c) `+0x3e6 ~ +0x4a1` 구간만 (`.asm` 있음) |
| ntdll | `^LdrpHandleUserCopyAttributeList$` | 0 | 2 (`.asm` 있음) |

- 정확한 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where ntdll <이름>`.
- 공용 부품 (`RtlAcquireSRWLockExclusive` 등 락, `memmove`) 은 이름만.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- 깊이·caller: `RtlpxLookupFunctionTable` 깊이 4 caller 14 · `RtlInsertInvertedFunctionTable` 5/3 (`LdrpInitializeProcess +0x415`, `LdrpProcessMappedModule +0x177`, …) · `LdrProtectMrdata` 5/21 helper · `RtlRemoveInvertedFunctionTable` 5/2 · `LdrControlFlowGuardEnforcedWithExportSuppression` 5/11 · `RtlpInsertInvertedFunctionTableEntry` 6/1 · `RtlpRemoveInvertedFunctionTableEntry` 7/1
- `.asm`·`.c` 는 다섯 모두 아직 없음 (`_index.tsv` 이름·크기만) — export 필요
- `LdrpInitializeProcess.asm` 의 구간 줄은 위 1(c) 에 옮김 (오케스트레이터가 `.asm` 에서 읽음)

## 이미 아는 것

- `[정적]` run45: `LdrInitializeMrdata` 본문은 `LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected`·`LdrpMrdataUnprotected` 를 **안 쓴다**
- `[정적]` run59: 동적 함수표 셋 (`RtlpDynamicFunctionTable*` 등) 은 `_LdrpInitialize +0x10d ~ +0x13e` 가 씀 — 이번 대상 아님
- `[관찰]` run44 사진: `LdrpMrdataSize`·`LdrpMrdataBase`·`LdrpInvertedFunctionTables*` 는 `≤P1a` 에 값, `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` 끝까지 0, `LdrpSuppressedExportOverrideListPtr` 끝까지 0
- PE_emulator 가 파일에서 읽은 값: `LdrpInvertedFunctionTables` 파일 값 `CurrentSize 1`·`MaximumSize 0x200`·칸 0 모두 0 (아직 확인 안 함)
- `[관찰]` run44: 매개변수 `+0x430 ~ +0x448` 0 → test.exe 는 `LdrpHandleUserCopyAttributeList` 목록 걷기를 안 탐

## 이번에 안 하는 것

- 사진 P1a 의 `LdrpInvertedFunctionTables` 머리·칸 0 실제 값 (VM / 덤프 → Q104 VM 단계에 얹혀 있음)
- `LdrpCaptureCriticalThunks` 본문 (이름만), `RtlLookupFunctionEntry` 등 예외 처리 길
- Q112·Q113 몫

## 받고 싶은 것

- 질문별 답 (예/아니오 또는 이름) + 근거 `+0x` 줄
- **오프셋 적는 법 (run56~run58 에서 세 번 틀림)**: 점프·호출 목적지는 **목적지 절대 주소 − 함수 시작 주소** 로 셈해서 적는다 (주소 끝 세 자리를 오프셋으로 적지 않는다). call 자리는 `.asm` 의 `CALL … ret=` 줄 그대로
- PE_emulator 가 그대로 쓸 **작은 표**: 함수 × (인자 · 쓰는 칸 · 하위 호출 · syscall (번호·인자) · 반환)
- 짐 지도 반영거리: baggage 3.9 `.mrdata` 칸 쓴 자리, `LdrpInvertedFunctionTables` 칸 표
- 동적으로 확인할 것 (있으면 — Q104 VM 단계에 얹을 수 있게: 멈춤점·읽을 전역)

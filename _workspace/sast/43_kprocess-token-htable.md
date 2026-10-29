# 43 kprocess-token-htable — 정적 분석 요청

- 날짜: 2026-10-29
- 스프린트: 03_sprint (짐 지도 바퀴, 예정 run49)
- 대기열: **Q78c1** — Q78c (프로세스 객체, 모르는 칸 약 378) 를 그 자리에서 나눈 첫 조각 (오케스트레이터 기본값 2026-10-29: 구조체별, `PspAllocateProcess` 안 호출 순서대로 — Q78c1 = `_KPROCESS`·`_TOKEN`·`_HANDLE_TABLE` 137칸, Q78c2 = `_EPROCESS` 241칸)
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` 3.12 `_KPROCESS` · 3.16 `_HANDLE_TABLE` · 3.19 `_TOKEN` (셋 다 아는 칸 0), 7.2 의 같은 세 구조체 모르는 칸 이름, 7.8 (`HANDLE_TABLE.UniqueProcessId` K2~K3 부모 PID → 자기 PID, 쓴 자리 `[공백]`), 5장 ① K2~K3
  - `runs/02_sprint/run10-allocate-process.md` (sast/04: `PspAllocateProcess` callee), `run15` (`PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange`)
  - `runs/03_sprint/run44-baggage-snap.md` (사진), `run47-section-chain.md` (Q78b 양식 — 같은 방식으로)
  - 이전 요청 sast/41 (Q78b), sast/38 (체크리스트 — `KeInitializeProcess`·`ObInitProcess`·`PspInitializeProcessSecurity` 를 "안 뽑은 callee" 로 남김)

## 범위

- 구조체 세 개의 **모르는 칸 137개** (baggage 7.2):
  - `_KPROCESS` 65: `K1 이전` 15 (Header*, ProfileListHead, DirectoryTableBase, ThreadListHead*, Affinity, AutoBoostState, ReadyListHead, ActiveProcessors, BasePriority, QuantumReset, ActiveGroupsMask, IdealProcessorAssignmentBlock, PerProcessorCycleTimes, CpuPartitionList, AvailableCpuState) · `K2~K3` 2 (StackCount*, ProcessListEntry) · `K3~P0` 2 (ProcessFlags, PpmPolicy) · 끝까지 0 46 (7.2 그대로)
  - `_HANDLE_TABLE` 17: `K1 이전` 10 (NextHandleNeedingPool, TableCode, QuotaProcess, HandleTableList, UniqueProcessId*, Flags, Flags.Duplicated, Flags.RaiseUMExceptionOnInvalidHandleClose, FreeLists*, ActualEntry*) · 끝까지 0 7
  - `_TOKEN` 55: `K1 이전` 29 · 끝까지 0 26 (7.2 그대로). 사진 8장 모두 같은 토큰 주소 `ffffb689ae6b6060`, 바뀜 없음.
  - (`*` = 사진에서 뒤에 또 바뀐 칸)
- 칸 이름·오프셋은 `pdb_struct.py ntoskrnl.exe _KPROCESS _HANDLE_TABLE _TOKEN` (이름 없는 공용체 안쪽은 sast/38 `ms/disassemble/ntoskrnl/_baggage/struct__*.tsv` 로 — sast/41 과 같은 방법).
- **범위 밖**: `_EPROCESS` 칸 (Q78c2), 핸들 표 **안 항목** (상속 핸들 내용 — baggage 7.6 C 류, 이름만), 토큰 안 가변 영역 (SID·권한 배열 내용 — 칸 표가 없는 짐, 이름만), 프로세스 주소 공간 (`DirectoryTableBase` 의 페이지 표 내용 — 이름만).

## 알고 싶은 것

1. **`_KPROCESS` 65칸** — `PspAllocateProcess +0x1719 KeInitializeProcess` (ret `+0x171e`) 안
   - 칸마다 쓰는 줄 (함수+오프셋), 값의 출처 (인자 — 부모·`PspAllocateProcess` 가 고른 값 / 계산 / 상수). `DirectoryTableBase` 는 어디서 받은 값인가 (주소 공간 만든 쪽 이름만).
   - 그 밖에 `_KPROCESS` 를 쓰는 `PspAllocateProcess` 안의 다른 호출 (`KeSetCpuSetsProcess +0x1a00`, `KeSetExpectedConcurrencyCountProcess +0x1a19`, `PspSetProcessPriorityClass +0x1a3b`, `KeSecureProcess +0x279b`, `PspSetProcessAffinitySafe +0x235f`, `KeInitializeProcessUserCetLogging +0x2008` 등) 이 쓰는 칸.
   - 사진에서 뒤에 바뀐 칸: `StackCount`·`ProcessListEntry` (`K2~K3` — 스레드 넣기·목록 넣기 쪽 후보), `ProcessFlags`·`PpmPolicy` (`K3~P0`), `Header`·`ThreadListHead` 바뀜 — 쓰는 함수 이름 (줄기 밖이면 이름만).
   - 끝까지 0 46칸: 0 을 쓰는지 / 할당 때 0 그대로인지 / 조건부 안 탐인지 (run47 의 셋 구분).
2. **`_TOKEN` 55칸** — `PspAllocateProcess +0x1922 PspInitializeProcessSecurity` → `+0xf7 SeSubProcessToken` → (`SepDuplicateToken` 등)
   - 새 프로세스 토큰은 부모 토큰을 **복제**하나, **같은 객체를 공유**하나? (사진: 8장 모두 같은 주소 — 이 주소가 부모 cmd 토큰과 같은지는 정적으로 알 수 있으면)
   - 복제라면 칸마다: 부모 토큰에서 복사 (= 입력) / 새로 정함 (`TokenId`·`ModifiedId`·`ParentTokenId`·`TokenInUse` 등 — 줄과 값 출처 `ExAllocateLocallyUniqueId` 류) / 0 그대로.
   - `+0x1d7 SeAssignPrimaryToken` 과 `ObInitializeFastReference` (`+0x110`) 가 쓰는 곳 (`EPROCESS.Token` 쪽은 Q78c2 로 넘기되 줄은 적어 둔다).
   - `PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange` (run15) 이 토큰 칸을 쓰는가 (예/아니오).
3. **`_HANDLE_TABLE` 17칸** — `PspAllocateProcess +0x198c ObInitProcess` → `+0x40 ExCreateHandleTable` / `+0xcc ExDupHandleTable`
   - test.exe (cmd 가 `CreateProcess` 로 띄움) 는 어느 길인가 — 새로 만들기 / 부모 표 복제 (상속 핸들). 가르는 조건 (인자·플래그) 과, run44 사진 (`K1 이전` 핸들 수 4, `Flags.Duplicated` 값) 으로 정할 수 있으면 정한다.
   - 칸마다 쓰는 줄. `QuotaProcess`·`HandleTableList` (전역 목록 넣기)·`UniqueProcessId` (처음 값 = 부모 PID 인 이유).
   - **`UniqueProcessId` K2~K3 부모 PID `0x19a8` → 자기 PID `0x1dd0`** 를 쓰는 줄 (baggage 7.8 `[공백]`). 후보: `PspInsertProcess` 근처 — 줄기 위면 끝까지.
   - `FreeLists`·`ActualEntry`·`NextHandleNeedingPool` 바뀜 (핸들 4 → 26 → 44 → 46) 은 핸들 만들기 공용 부품 (`ExCreateHandle` 류) — 이름만.
4. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거). 경로는 `NtCreateUserProcess` 부터 지도 기준 (`①… → PspAllocateProcess +0x1719 → KeInitializeProcess +0x..`). 줄기 위는 끝까지, 줄기 밖 callee·공용 부품 (`memset`, `ExAllocatePool*`, 그룹 마스크 도우미 `Ke*GroupMask`, `KiCopyAffinityEx`, 락, `ExSaAllocate` 등) 은 이름만.
5. **갈림길과 입력**
   - 이 조각에서 부모·시스템 상태에 따라 갈리는 곳 (상속 핸들 / 작업(Job) / 앱 컨테이너·`Feature_DecoupleAppContainerAndIntegrityLevel` / CET / VBS `VslCreateSecureProcess` 류 / NUMA 노드) 을 갈림길로 적고, test.exe 가 어느 쪽인지 정적·사진으로 정하거나 동적 질문으로.
   - 입력 (부모 토큰, 부모 핸들 표, `PspAllocateProcess` 인자) 을 칸과 함께.

## 대상

rva 는 `ms/decomp/ntoskrnl/_index.tsv` 기준 (2026-10-29 확인, 지도 깊이·caller·상태).

| 모듈 | 함수 | rva | depth | 이유 |
|---|---|---|---|---|
| ntoskrnl | `KeInitializeProcess` | `0xa67c14` (1192B, 지도 2·2) | 1 | `_KPROCESS` 몸통 |
| ntoskrnl | `PspInitializeProcessSecurity` | `0xab1318` (511B, 2·1) | 1 | 토큰 붙이기 |
| ntoskrnl | `SeSubProcessToken` | `0xab1520` (2268B, 3·1) | 1 | 토큰 복제/공유 |
| ntoskrnl | `SepDuplicateToken` | `0x87ca6c` (2313B, 2·18) | 0 | 복제 본체 후보 |
| ntoskrnl | `ObInitProcess` | `0x921a4c` (435B, 2·1) | 1 | 핸들 표 |
| ntoskrnl | `ExCreateHandleTable` `0x921c08` (3·7), `ExDupHandleTable` `0x922880` (3·1) | | 0 | 두 길 |
| ntoskrnl | (필요하면) `SeAssignPrimaryToken`, `PspInsertProcess` (`UniqueProcessId` 쓰기 후보), `PspAllocateProcess` 의 `_KPROCESS` 쓰는 다른 callee | `_index.tsv` / `pdb_aliases.py --where` 로 | 0~1 | 1·2·3번 답에 따라 |

- `PspAllocateProcess` 본문은 이미 `.c`/`.asm` 있음 (sast/04) — 다시 뽑지 말고 읽기만.
- 이미 `.c` 가 있는 함수는 다시 뽑지 않는다 (덮어쓰기 전에 있는지 확인).

## 지도에서 본 것 (`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*`)

- `PspAllocateProcess` 직접 호출 (이 조각 관련): `+0x1719 KeInitializeProcess`, `+0x1922 PspInitializeProcessSecurity`, `+0x198c ObInitProcess`, 그 밖에 `+0x1a00 KeSetCpuSetsProcess`, `+0x1a19 KeSetExpectedConcurrencyCountProcess`, `+0x1a3b PspSetProcessPriorityClass`, `+0x12b3 PspSelectNodeForProcess`, `+0x12c3/+0x1326 KeSelectGroupFromNode`, `+0x139d KeSelectNodeForAffinity`, `+0x2008 KeInitializeProcessUserCetLogging`, `+0x235f PspSetProcessAffinitySafe`, `+0x279b KeSecureProcess`, `+0x2689 PsReferencePrimaryTokenWithTag`, `+0x26ac SeSetTokenTamperProtection`, `+0x264a SeConstructAntiTamperPolicy`.
- `KeInitializeProcess` 직접 호출 약 27줄: `memset` ×3, `KiCopyAffinityEx` ×2, `KiInitializeProcessAvailableCpuState +0x12e`, `KiInitializeIdealProcessorAssignmentBlock +0x148`, 그룹 마스크 도우미 여럿, `ExSaAllocate +0x2c6`, `KeQueryMaximumProcessorCountEx`, `KiComputeProcessAutoBoostTreeCount +0x38c`, `KiStartIdealProcessorAssignmentBlock +0x3c0`, `MmGetNextNode +0x3fd`, `VslCreateSe…` (`+0x45c` 근처, VBS 쪽).
- `PspInitializeProcessSecurity`: `+0xa7 PsGetProcessSessionId`, `+0xf7 SeSubProcessToken`, `+0x110 ObInitializeFastReference`, `+0x143 PsReferencePrimaryTokenWithTag`, `+0x156 PspIdentityBasedJobBreakaway`, `+0x1c3 ObFastDereferenceObject`, `+0x1d7 SeAssignPrimaryToken`.
- `ObInitProcess`: `+0x40 ExCreateHandleTable`, `+0x5f SepAuditingForSubCategory`, `+0xaa ObReferenceProcessHandleTable`, `+0xcc ExDupHandleTable`, `+0x126 ExSweepHandleTable`·`+0x12e ExDestroyHandleTable` (실패 정리), `+0x144 SeCaptureSubjectContext`, `+0x199 ExEnumHandleTable`.

## 이미 아는 것

- `[정적]` sast/04·run10: `PspAllocateProcess +0x1719/+0x171e KeInitializeProcess`, `+0x1922 PspInitializeProcessSecurity`, `+0x198c ObInitProcess` (이름과 자리만, 본문 안 봄).
- `[정적]` run47 (Q78b) 방식: 칸마다 쓰는 줄 / "0 그대로" 를 조건부 안 탐·0 을 씀·할당 때 0 (아무도 안 씀) 으로 가름.
- `[관찰]` run44: 세 구조체 칸 대부분 `K1 이전`. 토큰 주소 8장 같음. `HANDLE_TABLE.UniqueProcessId` K2~K3 부모 → 자기. 핸들 수 4·4·4·4·4·26·44·46.
- `[추측]` 토큰은 부모 것을 복제한다 (`SeSubProcessToken` 이름으로 본 것).

## 받고 싶은 것 (결과서 `_workspace/sast/43_kprocess-token-htable_result.md`)

- 맨 앞 요약 표: 구조체별 "모르는 칸 → 아는 칸 (nt) / 입력 (부모에서 복사) / 아무도 안 씀 / 여전히 모름" 숫자.
- 질문 1~5 답 + 근거 (`.asm` 의 +0x 줄). 태그 `[정적]` / `[정적] PDB` / `[추측]` / `[공백]`.
- 칸별 "쓰는 곳" 표 (baggage.md 3장 양식, 3.12·3.16·3.19 에 바로 옮길 수 있게).
- 동적 질문 목록 (번호, 추천 멈춤점, 찍을 것, 사진 덤프로 답할 수 있는지).
- Q78c2 (`_EPROCESS`) 로 넘길 것 — 이 조각에서 본 `_EPROCESS` 쓰기 줄 (예: `Token` 빠른 참조, `ObjectTable`) 은 줄만 적어서 넘긴다.
- 코드는 길게 붙이지 않고 우리 말로.

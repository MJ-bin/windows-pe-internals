# 47 ethread-create — 정적 분석 요청

- 날짜: 2026-10-30
- 스프린트: 03_sprint (짐 지도 바퀴, 예정 run53)
- 대기열: **Q78d2** — Q78d (첫 스레드) 의 둘째 조각 (오케스트레이터 기본값 2026-10-30: 시간 순서 + 구조체별)
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` 3.13 `_ETHREAD` (아는 칸 5 — 다시 보지 않는다. 머리의 **"(run52, sast/46) Q78d2 에서 볼 것"** 메모 — 지나가며 본 `PspAllocateThread` 쓰는 줄, 확인해서 옮긴다), 3.15 `_KTRAP_FRAME` (아는 칸 6, 머리의 **"Q78d2 에서 볼 것"** 메모 — `KiInitializeContextThread +0x187 memset` → `+0x198`·`+0x1f1`), 7.2 두 구조체 모르는 칸 이름 (구간별), 2.1 아래 run52 줄 (K1 = 옛 객체 → 끝까지 0 은 K2 ~ P2 기준)
  - `runs/03_sprint/run52-kthread-create.md`·sast/46 결과서 (**같은 방식으로** — 네 가지 가름, DWORD 되쓰기 기준, 오프셋 훑기의 한계 표시)
  - `runs/02_sprint/run11`·`run15`·`run16` (`PspAllocateThread`·`KxContextToKframes` 본문 요약)

## 범위

- **169칸**:
  - `_ETHREAD` `K1~K2` 27 (7.2 그대로): Tcb*, CreateTime, ExitTime, KeyedWaitChain, PostBlockList, ActiveTimerListHead, KeyedWaitSemaphore, AlpcWaitSemaphore, IrpList, ReadClusterSize, BoostList, DeboostList, PropertySet, EnergyValues, Silo, OwnerEntryListHead, DisownedOwnerEntryListHead, UpdateTebApc*, UpdateTebApcFill1, UpdateTebApcFill2, MmSlabIdentity, UpdateTebApcFill3*, RngState*, UpdateTebApcFill4*, UsedByRngState*, UpdateTebApcFill5*, ProcessFastRef
    - `Tcb` 는 `_KTHREAD` 통째 칸 (안쪽은 run52 에서 닫힘 — 이 칸은 "안쪽 칸으로 셈" 같은 한 줄 판단만).
    - `UpdateTebApc*` 묶음은 run52 run 문서의 둘째 `KeInitializeApc` (`KeInitThread +0x368`, `KiUpdateTebApc`) 와 이어지는지 확인.
  - `_ETHREAD` `끝까지 0` 107 (7.2 그대로 — `CrossThreadFlags`·`SameThreadPassiveFlags`·`SameThreadApcFlags` 비트, 락, ALPC·IO·종료 칸 등)
  - `_KTRAP_FRAME` `K1~K2` 3: ExceptionActive, MxCsr, EFlags*
  - `_KTRAP_FRAME` `끝까지 0` 32 (7.2 그대로)
- **끝까지 0 칸은 만들기 길 (`NtCreateUserProcess → PspAllocateThread` 와 그 callee, `KeInitThread → KiInitializeContextThread → KxContextToKframes` 까지) 안에서만 본다**: 네 가지 (0 을 씀 / 조건부 안 탐 + 조건 / 아무도 안 씀 (만들기 길 안) / 범위 밖 길에서 씀 `[추측]`). 비트 칸 기준은 run52 와 같음 (DWORD·바이트를 통째로 쓰는 줄이면 0 을 씀).
- 몸통 0 채움: 스레드 객체는 `PspAllocateThread` 의 `ObCreateObjectEx` (call `+0x2e2`, ret `+0x2e7` — baggage 6장은 ret 자리 `+0x2e7` 로 적혀 있음, 오케스트레이터 `.asm` 확인) 뒤 `+0x307 memset` (ret `+0x30c`, 크기 `[RSP+0x98]`) — 이것이 `_ETHREAD` 몸통 전체인지 (크기 인자) 확인. 트랩 프레임은 `KiInitializeContextThread +0x187 memset(0x190)`.
  - **주의**: 몸통 `memset` 이 칸을 덮으면 그 칸은 "0 을 씀" 이다. 그래도 끝까지 0 칸 중 다른 길에서 0 아닌 값을 쓰는 칸은 "범위 밖 길에서도 씀" 을 이름만 덧붙인다 (run50·run52 와 셈 방식이 같은지 결과서에 한 줄 — 다르면 왜).
- 칸 이름·오프셋은 `pdb_struct.py ntoskrnl.exe _ETHREAD` / `_KTRAP_FRAME` (비트 칸은 sast/38 `ms/disassemble/ntoskrnl/_baggage/struct__ETHREAD.tsv`·`struct__KTRAP_FRAME.tsv`).
- **범위 밖**: `_ETHREAD` 뒤 구간 10칸 (`K2~K3` 5 · `K3~P0` 4 · `P1~P1a` 1) 과 `_KTRAP_FRAME` `P1~P1b` 25 (Q78d4), `_TEB` (Q78d3), `ALPC`·`IRP` 등 리스트 안에 나중에 달리는 항목 내용.

## 알고 싶은 것

1. **`K1~K2` 30칸 쓰는 줄** (`_ETHREAD` 27 + `_KTRAP_FRAME` 3) — 칸마다 함수+오프셋, 값의 출처 (상수 / 계산 / 인자 / 프로세스에서 / 전역).
   - 3.13 메모의 줄 (`CreateTime` `+0x4bf`, 리스트 머리 8개, 락 칸 5개, `Silo`·`MmSlabIdentity`·`ReadClusterSize`) 을 `.asm` 으로 확인해서 옮길 수 있게. 나머지 후보: `ExitTime`·`PostBlockList`·`KeyedWaitSemaphore`·`AlpcWaitSemaphore` (`KeInitializeSemaphore` 류), `EnergyValues`, `RngState`·`UsedByRngState`, `ProcessFastRef` (프로세스 참조).
   - 트랩 프레임 `ExceptionActive`·`MxCsr` (3.15 메모 그대로 확인), `EFlags*` (`KxContextToKframes` — 값 출처가 CONTEXT 인지 상수인지).
   - `*` 칸은 처음 값을 쓰는 줄만. 뒤에 바꾸는 쪽은 이름만.
2. **끝까지 0 139칸 가르기** (`_ETHREAD` 107 + `_KTRAP_FRAME` 32) — 네 가지. 조건부 갈래 (`HideFromDebugger`·`SkipCreationMsg` 등 생성 플래그, 피코, 보안 컨텍스트, 디버그 레지스터 (`Dr*`·`DebugControl`), CET (`ShadowStackFrame`) 등) 는 조건과 test.exe 가 왜 안 타는지 (생성 인자·사진 값).
3. **간접 호출** — 만들기 길 안에서 이 두 구조체 칸을 쓰는 간접 호출이 있으면 대상을 정적으로 좁히고, 못 정하면 동적 질문 (Q35 와 같으면 Q35 로). 없으면 "0개" 와 본 `.asm` 수.
4. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거). 경로는 run52 의 줄임 (`①A…`, `①K…`, `①KC…`) 을 그대로 쓴다. 공용 부품 (`memset`, 할당, 락, `KeInitialize*` 도우미 — 6장에 있으면 이름만) 은 이름만.

## 대상

rva 는 `ms/decomp/ntoskrnl/_index.tsv` 기준. **이미 `.c`·`.asm` 이 있는 함수는 다시 뽑지 않는다** (`PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread`·`KxContextToKframes`·`MmCreateTeb`·`PspSetupUserStack` 은 있음).

| 모듈 | 함수 | rva | depth | 이유 |
|---|---|---|---|---|
| ntoskrnl | `PspAllocateThread` | `0x84296c` (있음) | 0 | `_ETHREAD` 직접 쓰기 — 읽기만 |
| ntoskrnl | `KiInitializeContextThread` · `KxContextToKframes` | 있음 | 0 | 트랩 프레임 — 읽기만 |
| ntoskrnl | (필요하면) `PspAllocateThread` callee 중 `_ETHREAD` 칸을 쓰는 것 (세마포어 초기화, 난수 상태, 에너지 값 등) | `_index.tsv` / `pdb_aliases.py --where` | 0 | 1·2번 답에 따라 |

- objdump 로만 본 근거는 함수+오프셋으로 적고 "`.asm` 없음" 표시. 오프셋은 `.asm` 의 +0x 열에서만 읽는다.

## 이미 아는 것

- `[정적]` 3.13 아는 칸 5 (`StartAddress` `PspAllocateThread +0x647`, `Cid` `+0x52d PsAssignThreadId`, `Win32StartAddress` `+0x655`, `ThreadLock` `+0x51c`, `ThreadInserted`), 3.15 아는 칸 6 — 다시 보지 않는다.
- `[정적]` run52: `_KTHREAD` 쪽 (`Tcb` 안쪽) 은 닫힘. 스레드 객체 몸통 `memset` `PspAllocateThread +0x307`.
- `[관찰]` run44: 값 대조는 `ms/logs/04_baggage-read*` 의 K2 ~ P2 (K1 은 옛 객체).

## 받고 싶은 것 (결과서 `_workspace/sast/47_ethread-create_result.md`)

- 맨 앞 요약 표: "169칸 → 아는 칸 (nt) / 0 을 씀 / 조건부 안 탐 / 아무도 안 씀 (만들기 길 안) / 범위 밖 길에서 씀 / 여전히 모름" 숫자, 구조체별·`K1~K2`·끝까지 0 따로.
- 질문 1~4 답 + 근거 (`.asm` 의 +0x 줄). 태그 `[정적]` / `[정적] PDB` / `[추측]` / `[공백]`.
- 칸별 "쓰는 곳" 표 (baggage.md 3.13 · 3.15 양식, 바로 옮길 수 있게). 끝까지 0 쪽은 묶어서 줄 수를 줄여도 된다.
- 동적 질문 목록 (번호, 추천 멈춤점, 찍을 것, 사진 덤프로 답할 수 있는지 — 덤프면 Q79 로).
- Q78d3·Q78d4 로 넘길 것.
- 코드는 길게 붙이지 않고 우리 말로.

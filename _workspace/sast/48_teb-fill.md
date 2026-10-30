# 48 teb-fill — 정적 분석 요청

- 날짜: 2026-10-30
- 스프린트: 03_sprint (짐 지도 바퀴, 예정 run54)
- 대기열: **Q78d3** — Q78d (첫 스레드) 의 셋째 조각 (오케스트레이터 기본값 2026-10-30: 시간 순서 + 구조체별)
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe · ntdll.dll (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` 3.3 `_TEB` (아는 칸 4 — 다시 보지 않는다. 머리의 **"(run52) Q78d3 에서 볼 것"**·**"(run53)"** 메모 — `PspAllocateThread +0x793` `SameTebFlags`, 복제 갈래, `MmCreateTeb +0x175 · +0x17e`), 7.2 `_TEB` 모르는 칸 이름 (구간별), 7.5 실마리 (`MmCreateTeb` TEB 대입 약 47줄, `LoadOwner` ← `LdrpDrainWorkQueue +0xd3` / `LdrpDropLastInProgressCount`)
  - `runs/03_sprint/run52-kthread-create.md`·`run53-ethread-create.md` 와 sast/46·47 결과서 (**같은 방식으로** — 네 가지 가름, 셈 기준: 명시 0 저장·입력값 쓰기·같은 DWORD 되쓰기만 "0 을 씀", 몸통 0 채움만으로는 안 셈)
  - `runs/02_sprint/run11` (`MmCreateTeb` 요약), run15 (`PspUserThreadStartup`), run17·run24·run38·run41 (ntdll 쪽 TEB 쓰기)

## 범위

- `_TEB` 모르는 칸 **133칸** 전부:
  - `K1~K2` 8: ClientId, ProcessEnvironmentBlock, TxFsContext, RealClientId, StaticUnicodeString, DeallocationStack, SameTebFlags*, SameTebFlags.InitialThread
  - `P0~P1` 5: CurrentIdealProcessor, IdealProcessorValue, ReservedPad2, IdealProcessor, PrimaryGroupAffinity
  - `P1~P1a` 5: ThreadLocalStoragePointer, RngState, _ActivationStack, ActivationContextStackPointer, HeapData*
  - `P1a~P1b` 1: SameTebFlags.LoadOwner*
  - `P1b~P2` 2: LastStatusValue, FlsData
  - `끝까지 0` 112 (7.2 그대로)
- 보는 길 (TEB 는 커널이 만들고 유저가 채운다 — 두 모듈):
  - 커널: `PspAllocateThread` (`+0x62c MmCreateTeb`, `+0x793 SameTebFlags`), `MmCreateTeb` 본문 (64비트 갈래 — WOW64 갈래는 안 타는 조건만), `PspUserThreadStartup` (`P0~P1` 다섯 칸 후보 — 이상적 프로세서·그룹 친화도).
  - ntdll: `LdrInitializeThunk` 부터 `NtContinue` 까지 지도에 있는 함수 (run17·run24·run38·run41 등에서 TEB 칸을 쓰는 곳). `P1~P2` 칸은 쓰는 ntdll 함수를 찾는다.
  - **KERNELBASE·KERNEL32 등 다른 DLL** 이 쓰는 칸 (`P1a~P1b`·`P1b~P2` 일 수 있음 — `LastStatusValue`·`FlsData` 등) 은 쓰는 DLL·함수 이름까지만 (짐 지도 "다른 DLL" 열). 본문 깊이 파지 않는다.
- **끝까지 0 112칸**은 위 길 안에서 네 가지 (0 을 씀 / 조건부 안 탐 + 조건 / 아무도 안 씀 (초기화 길 안) / 범위 밖 길에서 씀 `[추측]`). TEB 는 새로 커밋된 페이지라 처음부터 0 이다 — `MmCreateTeb` 이 `memset`·0 채움을 하는지, 아니면 페이지가 원래 0 인지 한 줄로 적고, 셈은 run52·run53 기준 그대로.
  - 유저 칸 중 GDI·OpenGL·Winsock·OLE·RPC·CSR·WOW64 처럼 다른 DLL 이 나중에 쓰는 칸은 "범위 밖 길 (다른 DLL)" 로 묶어서 이름만 — 줄마다 찾지 않아도 된다 (어느 DLL 인지 짐작이면 `[추측]`).
  - ntdll 이 초기화 길 밖 (예외 처리, 스레드 풀, 디버그 출력, 파이버, 트랜잭션 등) 에서 쓰는 칸은 "범위 밖 길 (ntdll)" 로, 쓰는 ntdll 함수 이름 하나 정도.
- 칸 이름·오프셋은 `pdb_struct.py ntdll.dll _TEB` (비트 칸 `SameTebFlags`·`CrossTebFlags` 는 sast/38 `ms/disassemble/ntoskrnl/_baggage/struct__TEB.tsv` 또는 ntdll 쪽 같은 표). 커널 쪽 코드는 TEB 를 `_TEB` 타입 없이 오프셋으로 쓸 수 있으니 오프셋으로 짝짓는다.
- **범위 밖**: 일꾼 스레드 2개의 TEB (C10, Q79), `TlsSlots`·`TlsExpansionSlots` 가 가리키는 안쪽 내용, `GdiTebBatch` 등 큰 배열 안쪽, `_TEB32` (WOW64).

## 알고 싶은 것

1. **`K1~K2` 8칸 쓰는 줄** — `MmCreateTeb` 대입 줄 (run11 "약 47줄") 에서 칸마다 오프셋, 값의 출처 (PEB 주소 / CID / 스택 / 상수). `SameTebFlags`·`InitialThread` 는 3.3 메모 (`PspAllocateThread +0x793`, 비트 계산 `+0x54a ~ +0x5a5`) 를 `.asm` 으로 확인. `MmCreateTeb +0x175 · +0x17e` 가 어느 TEB 칸인지.
   - 같은 대입 묶음이 끝까지 0 칸 (`NtTib` 안쪽 이외, 예: `StaticUnicodeString` 머리, `ExceptionCode` 등) 에 명시적으로 0 을 쓰면 "0 을 씀" 으로.
2. **`P0~P2` 13칸 쓰는 줄** — 칸마다 함수+오프셋, 모듈 (nt / ntdll / 다른 DLL), 값의 출처. `P0~P1` 다섯은 `PspUserThreadStartup` 또는 그 callee 인지 (`KeQueryIdealProcessor`·그룹 친화도 류). `ThreadLocalStoragePointer` (TLS 벡터 — `LdrpAllocateTls` 류), `RngState`, `_ActivationStack`·`ActivationContextStackPointer` (`RtlpInitializeActivationContextStack` 류), `HeapData`, `LoadOwner` (7.5 실마리 확인), `LastStatusValue`, `FlsData` (FLS — ntdll 인지 KERNELBASE 인지).
   - `*` 칸은 처음 값을 쓰는 줄만. 뒤에 바꾸는 쪽은 이름만.
3. **끝까지 0 112칸 가르기** — 위 "보는 길" 안에서 네 가지. 조건부 갈래 (WOW64 `WowTebOffset`, 복제 갈래 `IsImpersonating`·`MuiImpersonation`, 디버그·계측 `Instrumentation*`, 파이버 `HasFiberData`, CET `ExtendedFeatureDisableMask` 등) 는 조건과 test.exe 가 왜 안 타는지. `SameTebFlags` 비트들은 `+0x793` 16비트 통째 쓰기 기준으로 "0 을 씀" 인지 판단 (run52 DWORD 되쓰기 기준과 같음).
4. **간접 호출** — 이 길에서 TEB 칸을 쓰는 간접 호출이 있으면 대상을 좁히고, 못 정하면 동적 질문 (Q35 와 같으면 Q35 로). 없으면 "0개" 와 본 `.asm` 수.
5. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거). 커널 쪽은 run52·53 줄임 (`①A…`, `①K…`, `①KC…`, `①KX…`), ntdll 쪽은 baggage 3장에 있는 ② 줄임을 그대로. 공용 부품은 이름만.

## 대상

rva 는 `ms/decomp/<모듈>/_index.tsv` 기준. **이미 `.c`·`.asm` 이 있는 함수는 다시 뽑지 않는다** (`PspAllocateThread`·`MmCreateTeb`·`PspSetupUserStack`·`PspUserThreadStartup` 은 있음, ntdll 은 지도 함수 다수 있음).

| 모듈 | 함수 | rva | depth | 이유 |
|---|---|---|---|---|
| ntoskrnl | `MmCreateTeb` | `0x9318c4` (있음) | 0 | TEB 대입 묶음 — 읽기만 |
| ntoskrnl | `PspAllocateThread` | `0x84296c` (있음) | 0 | `SameTebFlags` `+0x793`, 복제 갈래 — 읽기만 |
| ntoskrnl | `PspUserThreadStartup` | `0x910de0` (있음) | 0 | `P0~P1` 후보 — 읽기만 |
| ntoskrnl | (필요하면) `MmCreateTeb`·`PspUserThreadStartup` callee 중 TEB 칸을 쓰는 것 | `_index.tsv` / `pdb_aliases.py --where` | 0 | 1·2번 답에 따라 |
| ntdll | (필요하면) TEB 칸을 쓰는 지도 함수 중 `.c`/`.asm` 이 없는 것 | `_index.tsv` | 0 | 2·3번 답에 따라 |
| kernelbase | (필요하면, 이름만 확인) `LastStatusValue`·`FlsData` 를 쓰는 함수 | `_index.tsv` | 0 | "다른 DLL" 열 |

- objdump 로만 본 근거는 함수+오프셋으로 적고 "`.asm` 없음" 표시. 오프셋은 `.asm` 의 +0x 열에서만 읽는다.
- ntdll 쪽 훑기 (어느 함수가 TEB 칸을 쓰나) 는 `gs:` / `[reg+0x30]` 처럼 TEB 를 얻는 방식이 여러 가지라 오프셋 훑기만으로는 약하다 — 훑기 한계를 결과서에 한 줄로.

## 이미 아는 것

- `[정적]` 3.3 아는 칸 4 (`NtTib` `LdrpMinimalMapModule +0xe2 ~ +0xea`, `CurrentLocale` `PspUserThreadStartup +0x78`, `RanProcessInit` `_LdrpInitialize +0xea`, `SchedulerSharedDataSlot` `LdrpAcquireSchedulerSharedDataSlot +0x92`) — 다시 보지 않는다.
- `[관찰]` run44: K1 엔 TEB 가 없음 (전부 `????`). 값 대조는 `ms/logs/04_baggage-read*` 의 K2 ~ P2.

## 받고 싶은 것 (결과서 `_workspace/sast/48_teb-fill_result.md`)

- 맨 앞 요약 표: "133칸 → 아는 칸 (nt / ntdll / 다른 DLL) / 0 을 씀 / 조건부 안 탐 / 아무도 안 씀 (초기화 길 안) / 범위 밖 길에서 씀 (ntdll / 다른 DLL) / 여전히 모름" 숫자, 사진 구간별 따로.
- 질문 1~5 답 + 근거 (`.asm` 의 +0x 줄). 태그 `[정적]` / `[정적] PDB` / `[추측]` / `[공백]`.
- 칸별 "쓰는 곳" 표 (baggage.md 3.3 양식, 바로 옮길 수 있게). 끝까지 0 쪽은 묶어서 줄 수를 줄여도 된다.
- 동적 질문 목록 (번호, 추천 멈춤점, 찍을 것, 사진 덤프로 답할 수 있는지 — 덤프면 Q79 로).
- Q78d4 로 넘길 것.
- 코드는 길게 붙이지 않고 우리 말로.

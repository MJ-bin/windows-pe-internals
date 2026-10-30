# 2026-10-30 run52 — 스레드 객체 앞부분을 만들 때 (Q78d1): `_KTHREAD` 모르는 칸 180 → **여전히 모름 0**. `K1~K2` 42칸은 `KeInitThread`·`PspAllocateThread`·`KiInitializeContextThread` 와 도우미 셋 (`KeAbInitializeThreadState`·`KeInitializeApc`·`KeInitializeTimer`) 이 쓰고, 끝까지 0 138칸은 0 을 씀 35 · 조건부 안 탐 12 · 아무도 안 씀 21 · 범위 밖 길 70 `[추측]` 으로 갈림. test.exe 첫 스레드는 **사용자 스레드 · 새 커널 스택 · CET 꺼짐** 길 (정적)

## 요약 표 (모르는 칸 180 → ?)

- 결과서 맨 앞 표 그대로. 분류 뜻은 run50 과 같다:
  - **아는 칸 (nt)** = 사진에서 값이 있고 (`K1~K2`), nt 에서 그 칸을 쓰는 줄을 찾음. `*` 칸 (사진에서 뒤에 또 바뀜) 은 **처음 값을 쓰는 줄만** 닫았다
  - **0 을 씀** = 사진 값이 0 이고, 만들기 길이 그 칸에 0 을 씀 (이번엔 모두 "같은 DWORD 를 읽어 몇 비트만 바꿔 되쓰기")
  - **조건부 안 탐** = 켜는 줄이 있는데 test.exe 에선 조건이 안 맞음
  - **아무도 안 씀 (만들기 길 안)** = 만들기 길 (+ 부른 함수) 에 쓰는 줄이 없고, 커널 전체를 훑어도 이름 있는 쓰는 곳이 안 보임. 몸통 0 채움 그대로
  - **범위 밖 길에서 씀 `[추측]`** = 만들기 길에는 쓰는 줄이 없고, 대기·스케줄러·APC·종료 같은 **다른 길**의 이름 있는 함수가 쓰는 것으로 보임
  - **여전히 모름** = 줄 후보만 있고 못 정함

| | 칸 | 아는 칸 (nt) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (만들기 길 안) | 범위 밖 길에서 씀 `[추측]` | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `K1~K2` | 42 | **42** | — | — | — | — | 0 |
| 끝까지 0 | 138 | — | **35** | **12** | **21** | **70** | 0 |
| **합** | **180** | **42** | **35** | **12** | **21** | **70** | **0** |

- **"범위 밖 길" 70 과 "아무도 안 씀" 21 은 커널 전체를 칸 오프셋 숫자로 훑은 결과가 근거다** (`objdump -d`, `.asm` 없음). 그래서 한계가 둘 있다:
  - 오프셋 숫자만 같으면 걸린다 → 다른 구조체 칸이 섞일 수 있다 (오탐. 예: `RelativeTimerBias +0xf8` 은 `ExpSetTimerObject` 의 타이머 객체 칸이었다).
  - `LEA` 로 칸 주소를 만든 뒤 그 포인터로 쓰는 줄 (리스트 삽입 등), `SHL`·`SHR`·`ROL [mem]` 같은 이동 명령은 안 잡힌다 → 놓칠 수 있다 (예: `QueueListEntry` 는 `KeRemoveQueueEx` 가 매달 수 있음 `[추측]`).
  - 그러니 두 묶음 사이 경계가 약하다. 둘 다 "초기화 동안 쓰는 줄이 없다" 는 점만 `[정적]` (만들기 길 함수 `.asm` 전수).
- 조건부 12 중 **`ExtendedFeatureDisableMask` 1칸은 "갈래를 안 탔다" 와 "탔는데 값이 0" 을 정적으로 못 가른다** `[공백]` (둘 다 사진 0). 전역 두 개를 읽으면 닫힌다 (동적 D1).

짐 지도 (`baggage.md` 2장) 로 옮길 때 — run50 과 같은 규칙:

- "아는 칸" 42 + "0 을 씀" 35 + "조건부 안 탐" 12 = **89** → "nt 가 씀" (줄이 있는 칸)
- "아무도 안 씀" **21** → 2장 표 아래 줄 (표에 열 없음)
- "범위 밖 길에서만 씀 `[추측]`" **70** → 모름에서 빼고 "아무도 안 씀" 옆에 따로 한 줄 (run50 쓰는 말)
- 그리고 이미 아는 칸 **`Teb` (+0xf0) 이 짐작 → 정확**: 이 문서를 쓰며 `.asm` 에서 쓰는 줄을 봤다 (`KeInitThread +0x1e0` 이 시작 정보 묶음 `+0x28` (= `MmCreateTeb` 결과) 을 읽어 **`+0x1e4 MOV [RDI+0xf0],RAX`**). PDB 칸 이름도 `Teb` (`struct__KTHREAD.tsv`). 아래 "정정".

짐 지도 2장 숫자 변화 (`_KTHREAD`):

| | nt 가 씀 | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|
| `_KTHREAD` (237칸) | 10 → **99** | 0 → **21** | 0 → **70** | 227 → **47** | 9 + 1 → **99 + 0** |

- 새 아는 칸 89 는 모두 정확 (쓰는 줄 `[정적]`, 결과서 + `.asm`). 옛 짐작 `Teb` 1 → 정확. 그래서 정확 +90 · 짐작 −1.
  - `ExtendedFeatureDisableMask` 도 정확으로 셌다 — 줄 (`KeInitThread +0x486`) 과 조건은 `[정적]`, 그 갈래를 탔는지만 동적 (run50 `MitigationFlags` 후보 ⓑ 와 같은 셈).
- 남은 모름 47 = `K2~K3` 10 · `K3~P0` 9 · `P0~P1` 9 · `P1~P1a` 7 · `P1a~P1b` 6 · `P1b~P2` 6 → **Q78d4** (`KeStartThread`·스케줄러).
- 짐 지도 전체: 구조체 21개 직접 칸 1,456 = 아는 칸 507 → **596** (정확 449 → **539** · 짐작 58 → **57**) + 아무도 안 씀 77 → **98** + 범위 밖 길에서만 씀 53 → **123** + 모름 819 → **639**.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 요청서·결과서 날짜.
- 03_sprint 열째 바퀴 (짐 지도 바퀴, plan **Q78d1** — Q78d 첫 스레드 (모름 564) 를 시간 순서 + 구조체별로 나눈 첫 조각, 오케스트레이터 기본값 2026-10-30).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음). 사진 로그 `ms/logs/04_baggage-read_K2.txt` 의 `!lmi nt` 와도 같다 (결과서). 이미 analyze 된 프로젝트에서 뽑기만 했다.
- 버전 대조 (결과서): `NtCreateUserProcess` call `+0xe57` / ret `+0xe5c` → `PspAllocateThread`, `PspAllocateThread` call `+0x6f4` / ret `+0x6f9` → `KeInitThread`, `KeInitThread` call `+0x2fc` / ret `+0x301` → `KiInitializeContextThread` (`_calls.tsv`). 그리고 **사진 K2 값과 코드가 칸마다 맞는다**:
  - `StackBase − StackLimit` = `0x7000` (`KeKernelStackSize`), `StateSaveArea` = `StackBase − 0x9c0`, `InitialStack` = 그 − `0x50`, `KernelStack` = `InitialStack − 0x310`.
  - `Affinity` = 객체 + `0x860`, `UserAffinity` + `0x870`, `KernelAbEntries` + `0x880`, `UserAbEntries` + `0xaa0`.
  - `MiscFlags` `0x4001`, `ThreadFlags` `0x20040`, `ModeHistory` `0xaaaaaaaa`, `RealtimePriorityFloor` `0x20`, `NpxState` `0xe5`, `ExpectedRunTime` `0x4c4b40`.
  - K2 에서 0 이 아닌 8바이트 덩어리 수가 코드로 센 것과 칸마다 같다 (예: `WaitBlock` 8, `Timer` 4, `SchedulerApcFill3` 6).
- 이 문서를 쓰며 다시 맞춘 것 (flow-writer):
  - `_calls.tsv` (4,304줄) 로 call/ret: `PspAllocateThread +0x2e2/+0x2e7 ObCreateObjectEx`, `+0x307/+0x30c memset`, `+0x223/+0x228 PoEnergyEstimationEnabled`, `+0x23c/+0x241 KeQueryMaximumGroupCount`, `+0x52d/+0x532 PsAssignThreadId`, `+0x78d/+0x792 KiStackAttachProcess`; `KeInitThread +0x152 KeInitializeApc`·`+0x163 KeInitializeEvent`·**`+0x172 KeInitializeTimer`**·`+0x273 MmCreateKernelStack`·`+0x2c0 KeAbInitializeThreadState`·`+0x2fc KiInitializeContextThread`·`+0x368 KeInitializeApc`·`+0x3ec KiCreateKernelShadowStack`·`+0x49a KeCopyXfdMaskToTeb`·`+0x4b9 KiAllocateXStateStack`; `KiInitializeContextThread +0x187/+0x18c memset`·`+0x1ac/+0x1b1 KxContextToKframes`.
  - `.asm` 쓰는 줄: `KeInitThread +0x80`·`+0x97`·`+0x9d` (`ThreadFlags`), `+0xde`·`+0xe7`·`+0xed`·`+0xf5`·`+0xfd` (`MiscFlags`), `+0xa9`·`+0xd7`·`+0x10c`·`+0x19e`·`+0x1e4`·`+0x1eb`·`+0x29b`·`+0x29f`·`+0x2ac`·`+0x2c5`·`+0x2cc`·`+0x333`·`+0x486`·`+0x4d8`·`+0x1c5`; `PspAllocateThread +0x321`·`+0x352`·`+0x36c`·`+0x3ab`·`+0x3ff`·`+0x8e1`; `KiInitializeContextThread +0x61`·`+0x198`·`+0x1f1`·`+0x25e`·`+0x2ca`·`+0x2ce`·`+0x2d2`. 오케스트레이터도 `KeInitThread +0xde → +0xe7 → +0xfd`·`+0x80 → +0x9d`·`+0x172`, `KiInitializeContextThread +0x187`·`+0x198`·`+0x1f1` 을 `.asm` 으로 확인했다.
  - `.asm` 이 없는 곳 (결과서가 `objdump -d` 로만 봄): 커널 전체 `[reg+칸]` 쓰기 훑기 (끝까지 0 138칸 + `K1~K2` 42칸), `KeAccumulateTicks +0x58`, `KiSystemServiceUser +0x11`, `KeAbPreAcquire +0x52`, "범위 밖 길" 표의 함수 전부. 이 문서에선 결과서의 함수+오프셋 그대로 쓰고 "`.asm` 없음" 이라고 붙였다.

## 목적

- 짐 지도 Q78 의 여섯째 조각 (Q78d 의 첫 조각). `_KTHREAD` (baggage 3.14) 모르는 칸 227 중 **만들 때 몫 180** 을 닫는다.
  - `K1~K2` 42 — 칸마다 쓰는 줄 (함수+오프셋) 과 값의 출처 (프로세스에서 복사 / 인자 / 계산 / 상수 / 전역).
  - 끝까지 0 138 — 만들기 길 (`NtCreateUserProcess → PspAllocateThread` 와 callee, `KeInitThread`·`KiInitializeContextThread` 까지) 안에서 0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길 로 가르기.
- 갈림길 (시스템 스레드·보안 스레드·CET·UMS·GUI) 과 간접 호출.
- 범위 밖: `_ETHREAD` 나머지·`_KTRAP_FRAME` (Q78d2), `_TEB` (Q78d3), `K2` 뒤 구간 47칸 (Q78d4), XSAVE 영역 안쪽 내용 (칸 표 없는 짐).

## 방법

- 정적: `_workspace/sast/46_kthread-create.md` — ntoskrnl. 새로 depth 0 **9개** (한 번에): 요청서 표 2개 (`KeInitializeTimerEx`·`MmCreateKernelStack`) + 요청서 마지막 줄 ("`KeInitThread` 의 다른 callee 중 칸을 쓰는 것") 에 따라 7개 (`KeAbInitializeThreadState`·`KeInitializeApc`·`KeInitializeTimer`·`KeInitializeEvent`·`KiAllocateXStateStack`·`KiCreateKernelShadowStack`·`KeCopyXfdMaskToTeb`).
  - **요청서가 적은 `KeInitializeTimerEx` 는 `KeInitThread` 가 부르는 함수가 아니다** — `+0x172` 는 `KeInitializeTimer` (rva `0x45b9d0`). `KeInitializeTimerEx` 는 뽑아 놓기만 했다.
  - 이미 `.c`/`.asm` 이 있어 읽기만: `PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread`·`KxContextToKframes`·`KiStackAttachProcess`·`KeStartThread` (Q78d4 로 넘길 줄만)·`PspInsertThread` (앞부분)·`NtCreateUserProcess` (인자 줄만).
  - `_calls.tsv` 4,281 → 4,304줄 (줄이 하나도 안 사라짐).
  - 칸 이름·오프셋은 `pdb_struct.py ntoskrnl.exe _KTHREAD` 와 sast/38 `struct__KTHREAD.tsv` (이름 없는 공용체 안 비트·`*Fill*` 칸).
  - 값 대조: run44 덤프 읽기 로그 `ms/logs/04_baggage-read_K2.txt` (K2 ~ P2 일곱 장 비교). 새 동적 실행은 없다.
- 결과서: `_workspace/sast/46_kthread-create_result.md` (**분석성공**)
- 문서: 이 run 문서, `baggage.md` (기준, 1장, 2장·2.1, 3장 줄임·3.3·3.13·3.14 다시 씀·3.15 머리 메모, 5장 ①, 6장, 7장 머리·7.1·7.2·7.4·7.6·7.8, 새 7.15, 8장), `flow.md` 기준·2.1 (트리 `·` 줄, `PspAllocateThread`·`KeInitThread` 절, 새 `KiInitializeContextThread` 문단)·4장·4.1·5장. 그림 (mermaid) 은 안 바꿨다 (아래 "그림").

## 결과

비유 (새 직원 책상 차리기, flow.md 2.1 비유를 이어서):

- `_KTHREAD` = 새 직원의 **업무 카드**. 본사 (`PspAllocateThread`) 가 빈 카드를 받아 (`+0x2e2 ObCreateObjectEx`) 지우개로 한 번 싹 밀고 (`+0x307 memset`), 팀장 (`KeInitThread`) 에게 넘긴다.
- 팀장은 칸을 이렇게 채운다:
  - **기본 서식** — 종류 (`Type` 6), 기다림 줄 넷, 타이머, 멈춤 APC·이벤트, 빈 목록 머리들.
  - **사물함 (커널 스택)** — 새로 하나 받아서 (`MmCreateKernelStack`) 위·아래 끝을 적는다.
  - **"어느 가게 소속"** — 프로세스 칸.
  - **표시 묶음 두 장 (`MiscFlags`·`ThreadFlags`)** — 묶음을 통째로 읽어 두세 칸만 체크하고 통째로 되쓴다. 그래서 나머지 비트는 다 0 이 된다.
- 마지막으로 비서 (`KiInitializeContextThread`) 가 사물함 맨 위에 개인 짐 상자 자리 (`StateSaveArea`) 를 잡고 "사용자 모드 직원" 표시 (`PreviousMode` 1) 를 단다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0xe57` | `+0xe5c` | `nt!PspAllocateThread` | [정적] |
| `nt!PspAllocateThread` | `+0x223` | `+0x228` | `PoEnergyEstimationEnabled` (켜져 있으면 `+0x321` `Header` 에너지 비트, 에너지 칸 `0xc8`B) | [정적] |
| `nt!PspAllocateThread` | `+0x23c` | `+0x241` | `KeQueryMaximumGroupCount` (친화도 블록 크기) | [정적] |
| `nt!PspAllocateThread` | `+0x2e2` · `+0x307` | `+0x2e7` · `+0x30c` | `ObCreateObjectEx` (스레드 객체) · **`memset`** (몸통 + 친화도 블록 + 잠금 칸 블록 전부 0) | [정적] |
| `nt!PspAllocateThread` | `+0x395` · `+0x3c7` | `+0x39a` · `+0x3cc` | `memset` (친화도 비트맵 둘) | [정적] |
| `nt!PspAllocateThread` | `+0x6f4` | `+0x6f9` | `nt!KeInitThread` (둘째 인자 = 시작 정보 묶음 `[RSP+0x120]`) | [정적] |
| `nt!KeInitThread` | `+0x152` · `+0x163` · `+0x172` | `+0x157` · `+0x168` · `+0x177` | `KeInitializeApc` (`SchedulerApc`) · `KeInitializeEvent` (`SuspendEvent`) · **`KeInitializeTimer`** (`Timer`) | [정적] |
| `nt!KeInitThread` | `+0x273` | `+0x278` | `MmCreateKernelStack` (`param_2+0x00` = 0 일 때 — test.exe 탐) | [정적] |
| `nt!KeInitThread` | `+0x2c0` | `+0x2c5` | `KeAbInitializeThreadState` (`AutoBoostActive`·부스트 줄 둘·잠금 칸 블록 안쪽) | [정적] |
| `nt!KeInitThread` | `+0x2fc` | `+0x301` | `nt!KiInitializeContextThread` | [정적] |
| `nt!KeInitThread` | `+0x368` | `+0x36d` | `KeInitializeApc` (ETHREAD `UpdateTebApc`, `KiUpdateTebApc` — Q78d2) | [정적] |
| `nt!KeInitThread` | `+0x3ec` · `+0x49a` · `+0x4b9` | `+0x3f1` · `+0x49f` · `+0x4be` | `KiCreateKernelShadowStack` · `KeCopyXfdMaskToTeb` · `KiAllocateXStateStack` — 갈래, test.exe 안 탐 | [정적] |
| `nt!KiInitializeContextThread` | `+0x187` | `+0x18c` | `memset` (트랩 프레임 `R15 − 0x190`, `0x190`B — Q78d2) | [정적] |
| `nt!KiInitializeContextThread` | `+0x1ac` | `+0x1b1` | `KxContextToKframes` (트랩 프레임 칸 — Q78d2) | [정적] |

### 호출 관계 (대표 지점)

```
NtCreateUserProcess
 └─ +0xe5c  PspAllocateThread                                                                        [정적]
         │  +0x2e7 ObCreateObjectEx → +0x30c memset (몸통 + 확장 영역 0)
         │  +0x321 Header 에너지 비트 · +0x352 KernelAbEntries · +0x36c Affinity · +0x3ab UserAffinity · +0x3ff RealtimePriorityFloor
         │  +0x8e1 UserAbEntries (ThreadContext ≠ 0 일 때)
         └─ +0x6f9  KeInitThread
                 │  +0x54~+0x7e Header 줄 머리·WaitBlock 넷 · +0x9d ThreadFlags · +0xa9 ExpectedRunTime
                 │  +0xb3~+0xcc ApcState 앞쪽 · +0xd7 Process · +0xfd MiscFlags · +0x10c WaitIrql
                 ├─ +0x157  KeInitializeApc            SchedulerApc
                 ├─ +0x168  KeInitializeEvent          SuspendEvent
                 ├─ +0x177  KeInitializeTimer          Timer → +0x193 Dpc 자리 · +0x19e/+0x1d2~+0x1dd WaitBlock[3] 매달기
                 │  +0x1e4 Teb · +0x1eb Header.Type = 6
                 ├─ +0x278  MmCreateKernelStack        → +0x29b InitialStack(첫) · +0x29f StackBase · +0x2ac StackLimit
                 ├─ +0x2c5  KeAbInitializeThreadState  +0xb2 MiscFlags 비트 0 · +0xbb/+0xc2 부스트 줄 둘
                 │  +0x2c5/+0x2cc 리스트 머리 넷 · +0x333 ModeHistory
                 └─ +0x301  KiInitializeContextThread
                         │  +0x18c memset(트랩 프레임) → +0x198 · +0x1f1 (Q78d2)
                         ├─ +0x1b1  KxContextToKframes  (Q78d2)
                         │  +0x25b NpxState · +0x25e PreviousMode = 1
                         │  +0x2ca InitialStack · +0x2ce KernelStack · +0x2d2 StateSaveArea
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 쓰는 줄.
- 코드 순서는 오프셋 순이다. 단 오프셋 순서가 늘 실행 순서는 아니다 (flow.md 2.1 주의).

### 시작 정보 묶음 (`KeInitThread` 둘째 인자) — 칸 자리 (질문 4 곁가지)

- `PspAllocateThread` 가 `[RSP+0x120]` 부터 채운다 (`+0x679 ~ +0x6e9`). 칸 이름은 `KeInitThread` 가 쓰는 자리로 센 것 `[정적]` (PDB 타입 없음 — 결과서 이름):

| 자리 | 뜻 | test.exe |
|---|---|---|
| `+0x00` | 이미 있는 커널 스택 (0 = 새로 만듦) | 0 → `MmCreateKernelStack` 탐 |
| `+0x08` | 시작 루틴 (`PspUserThreadStartup` rva `0x910de0`; 시스템 스레드 `PspSystemThreadStartup` `0x494470`, 보안 스레드 `PspSecureThreadStartup` `0xaaf3b0`) | `PspUserThreadStartup` (run11) |
| `+0x10` | 시작 인자 (`[RSP+0xa8]`) | — |
| `+0x18` | 시작 주소 (`_CONTEXT.Rip`) | `RtlUserThreadStart` |
| `+0x20` | **`ThreadContext`** — 0 이면 시스템 스레드 (`KeInitThread +0x112`) | 0 아님 (`NtCreateUserProcess +0xe3a` 가 `_CONTEXT` 포인터를 넘김) |
| `+0x28` | TEB (`MmCreateTeb` 결과) → `+0x1e4` `Teb` 칸 | TEB `9b2fa67000` (run44) |
| `+0x30` | 프로세스 | test.exe EPROCESS |
| `+0x38` | 이상적 노드 + 1 (0 = 정하지 않음) | — |
| `+0x3c` | 플래그 (비트 1 → `ApcQueueable` 안 켬·`WaitIrql` 2, 비트 0 → 커널 그림자 스택 안 만듦, 비트 4 → 보안 스레드) | 0 |
| `+0x40` | 확장 기능 마스크 (`ExtendedFeatureDisableMask`·`StateSaveAreaDecoupled` 조건) | — (D1) |

### 몸통 0 채움

- **`PspAllocateThread +0x307 memset(스레드, 0, 크기)`** — `ObCreateObjectEx` (`+0x2e2`) 바로 뒤. 크기는 객체를 만들 때 넘긴 크기 (`[RSP+0x98]`) 그대로라 **몸통 `0x798` + 에너지 칸 `0xc8` + 친화도 블록 둘 + 잠금 칸 블록 둘** 전부가 0 이 된다 `[정적]`.
- 그래서 모든 칸은 0 에서 시작한다. "아무도 안 씀" 의 0 은 이 `memset` 에서 온다. run49·run50 의 `ObpAllocateObject` 0 채움 비트는 이번엔 안 봤다 (`memset` 이 있어서 필요 없음, 결과서).
- 몸통 뒤 확장 영역 (객체 + `0x798` 부터): 에너지 칸 `0xc8`B → `Affinity` 가 가리키는 `_KAFFINITY_EX` (+`0x860`) → `UserAffinity` 가 가리키는 것 (+`0x870`) → `KernelAbEntries` 가 가리키는 `_KLOCK_ENTRIES` 6칸 블록 (+`0x880`, `0x220`B) → `UserAbEntries` 8칸 블록 (+`0xaa0`, `0x2d0`B). 사진 K2 포인터 값과 맞다.

### `K1~K2` 42칸 (질문 1)

- 칸마다 쓰는 줄·값·경로는 **짐 지도 3.14** (결과서 1.1 표를 3장 양식으로 옮김). 묶어 보면:
  - `KeInitThread` — `Header` (`Type` 6·줄 머리), 스택 둘, `ExpectedRunTime`, `ThreadFlags` 두 비트, `ApcState` 앞쪽, `Timer` 의 `Dpc`·줄 연결, `WaitBlock` 묶음 9칸, `Process`, `MutantListHead`, 리스트 머리 넷, `ModeHistory`.
  - `PspAllocateThread` — `Header` 에너지 비트 (`+0x321`), `Affinity`·`UserAffinity` (확장 영역 포인터), `RealtimePriorityFloor` `0x20`, `KernelAbEntries`·`UserAbEntries`.
  - `KiInitializeContextThread` — `StateSaveArea`, `PreviousMode` 1, `NpxState`.
  - 도우미 셋 (줄기 끝 — 더 부르는 게 없음, `_calls.tsv` 0줄) — `KeAbInitializeThreadState` (`MiscFlags.AutoBoostActive`·`PropagateBoostsEntry`·`IoSelfBoostsEntry`), `KeInitializeApc` (`SchedulerApcFill1~5`), `KeInitializeTimer` (`Timer` 안쪽).
- **겹치는 칸 (`*Fill*`) 은 같은 바이트를 가리키는 자리다.** `WaitBlockFill5~11` 은 대기 블록 4개의 `Thread` 칸 (`+0x158 · +0x188 · +0x1b8 · +0x1e8`) 과 마지막 블록의 줄 연결 (`+0x1d0 · +0x1d8`)·`+0x1e0` DWORD 를 어디까지 덮느냐만 다르다. `SchedulerApcFill1~5` 는 `_KAPC` 앞쪽을 어디까지 덮느냐만 다르다. 그래서 **같은 줄이 여러 칸의 쓰는 줄이다**.
- `*` 칸 20개 (사진에서 K2 뒤 또 바뀜) 는 **처음 값 줄만** 닫았다. 뒤에 바꾸는 쪽은 이름만: `KeStartThread` (`Header` 비트 23 `+0x353`, `ThreadFlags` 비트 13 `+0xb9`·`+0x1b7`, `QuantumReset` 겹침 `+0x1c1` — Q78d4), 대기·스케줄러 (`KiCommitThreadWait`·`KiSwapThread` 쪽 `[추측]`), 시계 틱 (`ModeHistory` ← `KeAccumulateTicks +0x58`, `.asm` 없음 `[추측]`), `KeAbPreAcquire +0x52` (`ThreadFlags` 비트 16 `[추측]`, `.asm` 없음).
- 결과서 칸 수 대조: 7.2 `K1~K2 (42)` 이름을 하나씩 짝지어 빠진 이름 0 · 더 들어간 이름 0 (결과서 scratchpad 스크립트).

### 끝까지 0 138칸 가르기 (질문 2)

- 판단 기준 (결과서 한 줄, run50 과 같음): 만들기 길 함수의 `.asm` 에서 칸 오프셋으로 쓰는 줄이 있으면 → 값 0 을 쓰거나 같은 DWORD·QWORD 를 읽어 일부 비트만 바꿔 되쓰면 **0 을 씀**, 켜는 줄인데 test.exe 조건이 안 맞으면 **조건부 안 탐**. 줄이 없으면 커널 전체 훑기로 이름 있는 스레드·스케줄러 쪽 쓰는 함수가 보이면 **범위 밖 길 `[추측]`**, 안 보이면 **아무도 안 씀**.
- **0 을 씀 35** — 모두 같은 DWORD 되쓰기:
  - `MiscFlags` 15비트 — `KeInitThread +0xde LEA R12,[RDI+0x74]` → `+0xe7` 읽기 → `+0xed`/`+0xf1` 비트 14 (`ApcQueueable`) 만 바꿈 → **`+0xfd MOV [R12],EDX`** 되쓰기. 비트: `ReadyTransition`·`WaitNext`·`SystemAffinityActive`·`Reserved1`·`ApcInterruptRequest`·`QuantumEndMigrate`·`TimerActive`·`ProcessDetachActive`·`Reserved2`·`ScbReadyQueue`·`Reserved3`·`WaitNextClearWobPriorityFloor`·`TimerSuspended`·`SuspendedWaitMode`·`Reserved` (24:8).
  - `ThreadFlags` 20비트 — `KeInitThread +0x80` 읽기 → `+0x97 OR EDX,0x20040` → **`+0x9d MOV [RDI+0x78],EDX`**. 비트 2 `AutoAlignment` 만 프로세스 `KPROCESS.ProcessFlags.AutoAlignment` 를 복사한다 (`+0x83 ~ +0x95`, test.exe 0). 나머지 비트 이름은 짐 지도 3.14 줄.
  - 범위 밖에서 이 비트들을 켜는 곳 (대기·APC·친화도·붙이기·퀀텀·GUI·얼림·종료·ETW) 은 `[추측]` 이름만 (결과서 2.1).
- **조건부 안 탐 12** — 갈림길 넷 (아래 "test.exe 가 탄 길") 과 전역 조건:

| 칸 | 켜는 줄 | 왜 안 탐 |
|---|---|---|
| `MiscFlags.SecureThread` (8) | `KeInitThread +0x4d8` | 시스템 스레드 갈래 (`+0x112` → `+0x1b9`) + 플래그 비트 4 — 사용자 스레드 |
| `MiscFlags.SystemThread` (10) | `KeInitThread +0x1c5` | 같은 시스템 스레드 갈래 — 사용자 스레드 |
| `MiscFlags.CetUserShadowStack` (20) | `PspAllocateThread +0xb3f` (복제 갈래 `+0x2caaf6`) | 프로세스 `MitigationFlags2` 비트 14 (`+0x5b9`) — sast/44: `0x40000000` 에 없음 |
| `MiscFlags.BypassProcessFreeze` (21) | `PspAllocateThread +0xb10` | 만들기 플래그 `0x200` (`+0x3f3`) — 안 줌 |
| `MiscFlags.CetKernelShadowStack` (22) | `KeInitThread +0x391 ~ +0x397` | 전역 `KiKernelCetEnabled` (`+0x2b0`) — 사진 비트 22 = 0 `[관찰 간접]` |
| `MiscFlags.StateSaveAreaDecoupled` (23) | `KiAllocateXStateStack +0x52` (호출 `KeInitThread +0x4b9`) | `KeFeatureBits` 비트 `0x37` (`+0x2d3`) + `param_2+0x40 ≠ 0` (`+0x4a6`) |
| `KernelShadowStack`·`…Initial`·`…Base`·`…Limit` (4) | `KeInitThread +0x40c · +0x417 · +0x423 · +0x44f` (호출 `+0x3ec KiCreateKernelShadowStack`) | `KiKernelCetEnabled` + 플래그 비트 0 꺼짐 (`+0x39f`) |
| `ExtendedFeatureDisableMask` | `KeInitThread +0x486` | `KeFeatureBits` 비트 `0x37` (`+0x2d3`). 값 = `~(param_2+0x40) & KUSD+0x3d8 & 0x40000` — **안 탔는지 / 탔는데 0 인지 `[공백]`** (D1) |
| `IptSaveArea` | `KiInitializeContextThread +0x61` | 전역 `KiIptSaveAreaLength` ≠ 0 (`+0x46`) — 사진 0 `[관찰 간접]` |

- **아무도 안 씀 (만들기 길 안) 21**: `SListFaultAddress`, `RelativeTimerBias`, `Spare13`, `Spare18`, `LastXStateSaveDebugInfo`, `ThreadFlags2.AbContextSwitchState`, `ThreadFlags2.ThreadFlags2Reserved`, `QueueListEntry`, `Spare24`, `SListFaultCount`, `LegoData`, `CallbackNestingLevel`, `Spare6`, `InGlobalForegroundList`, `Spare26`, `Spare27`, `TracingPrivate`, `ReservedPreviousReadyTimeValue`, `InGlobalUpdateVpThreadPriorityList`, `SecureCallCoreIsolationCount`, `Padding`.
  - 근거: `PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread` `.asm` 에서 스레드 포인터 (`RSI`·`RDI`·`R12`·`RBX`) 로 쓰는 줄 전수에 없고, 도우미 callee 쓰기도 이 칸을 안 건드린다. 전체 훑기에서도 이름 있는 스레드 쪽 쓰기가 안 보임 (오탐뿐). 한계는 맨 앞 요약 표 아래.
- **범위 밖 길에서 씀 `[추측]` 70** (전체 훑기, `.asm` 없음):

| 칸 | 쓰는 곳 (범위 밖) `[추측]` |
|---|---|
| `ThreadLock` (KTHREAD `+0x40`) | `KiBeginThreadWait`, `KeDelayExecutionThread`, `KeWaitForMultipleObjects`, `KeWaitForMutexObject`, `KeRemoveQueueEx`, `KeRemovePriQueue` (잡았다 놓음) |
| `SchedulingGroup` | `KiRemoveThreadFromSchedulingGroup`, `KiTransitionSchedulingGroupGeneration`, `KeStartThread +0x32b` (Q78d4) |
| `Alerted` | `KiBeginThreadWait`, `KeDelayExecutionThread`, `KeWaitForMultipleObjects`, `KeWaitForMutexObject`, `KeRemoveQueueEx` |
| `Tag` | `KeUpdateThreadTag`, `KiIpiUpdateThreadTag` |
| `SameThreadTransientFlags` 5칸 | `KiExpandKernelStackAndCalloutOnStackSegment`, `RtlpWalkFrameChain` |
| `SpecCtrl` 3칸 | `SwapContext` |
| `Queue` | `KeRemoveQueueEx`, `KeRemovePriQueue` |
| `KernelApcDisable` · `CombinedApcDisable` | 크리티컬 리전 들어가기·나오기 (인라인 — `KeEnterCriticalRegionThread`, `KiLeaveCriticalRegionUnsafe` 등). 썼다 되돌림 (D3) |
| `ThreadCounters` | `KeEnableProfiling`, `KeDisableProfiling` |
| `XStateSave` | `KeRestoreExtendedAndSupervisorState` |
| `Win32Thread` | `PsSetThreadWin32Thread` |
| `ThreadFlags2` · `.DisableKasan` | `KasaniReport`, `KcsanValidateAddress` |
| `BamQosLevel` | `KeApplyWobBamQos`, `KeSetThreadSchedulerAssist`, `KiUpdateThreadPriority` |
| `HardwareFeedbackClass` | `KeStartThread +0x380` (Q78d4), `KiUpdateThreadHgsFeedback` |
| `PriorityDecrement` 3칸 | `KeSetPriorityThread`, `KiComputeNewPriority`, `KiQuantumEnd`, `KiClearForegroundBoost`, `KiRemoveBoostThread` |
| `NextProcessor.SharedReadyQueue` | `KiDeferredReadySingleThread`, `KiAddThreadToPrcbQueue`, `KiFindStandbyThreadForSteal` |
| `QueuePriority` | `KeRemovePriQueue`, `KiInsertQueue`, `KiInsertQueueInternal`, `KeInsertQueueApc` |
| `UserAffinityPrimaryGroup` · `AffinityPrimaryGroup` | `KeStartThread +0x156 · +0x14f` (Q78d4), `KiSetAffinityThread` 등 |
| `Preempted` · `AdjustReason` | `KiDeferredReadySingleThread`, `KiDirectSwitchThread`, `KiCommitThreadWait`, `KiProcessThreadWaitList` |
| `ApcStateIndex` | `KiAttachProcess`, `KiStackAttachProcess +0x162`, `KiDetachProcess` 등 (붙이기 — 부모 스레드 쪽) |
| `Saturation` | `KeSetBasePriorityThread`, `KeSetActualBasePriorityThread`, `KeRemovePriQueue`, `KiWakePriQueueWaiter` |
| `KernelTime` · `UserTime` | `KeAccumulateTicks`, `KeUpdateTotalCyclesCurrentThread` 등 |
| `WaitPrcb` | `KiInsertPrcbWaitEntry`, `KiRemovePrcbWaitEntry`, `KiSignalThread` |
| `AbWaitEntryCount` · `AbOwnedEntryCount` · `AbEntryCountValue` | `KiAbConvertWaiterToOwnerEntry`, `KiAbEntryGetLockedHeadEntry` |
| `FreezeFlags` 4칸 | `KiFreezeSingleThread`, `KiThawSingleThread`, `KeForceResumeThread` |
| `SecureThreadCookie` | `KeUnsecureThread` |
| `PriorityFloorCounts` · `PriorityFloorSummary` · `AbCompletedIoBoostCount` · `AbCompletedIoQoSBoostCount` · `KeReferenceCount` | `KiAbSetMinimumThreadPriority`, `KeAdjustPriorityFloor` 등 |
| `ForegroundLossTime` | `KiClearForegroundBoost` |
| `OtherTransferCount` | `IopUpdateIrpTransferCount`, `NtSetInformationFile`, `KeFoldProcessStatisticsThread` |
| `QueuedScb` | `KiAddThreadToScbQueue`, `KiRemoveThreadFromScbQueue`, `KiMoveScbThreadsToNewReadylist` |
| `ThreadTimerDelay` | `KeStartThread +0x36c` (Q78d4), `KiAdjustThreadTimer` |
| `PpmPolicy` | `KeSetThreadPpmPolicy` |
| `SchedulerAssist` · `SchedulerAssistPriorityFloor` · `BamQosLevelFromAssistPage` | `KeSetThreadSchedulerAssist`, `KiUpdateVpBackingThreadPriorityFromTopLevel` |
| `AbWaitObject` | `KiCommitThreadWait`, `KeWaitForMutexObject`, `KiWaitForAllObjects`, `KeInsertQueueEx` |
| `Hgs…` 5칸 | `KiBeginThreadAccountingPeriod`, `KiUpdateThreadHgsFeedback`, `KiStartThreadCycleAccumulationContextSwap` |
| `SystemAffinityTokenListHead` | `KeSetSystemMultipleGroupAffinityThread`, `KeRevertToUserMultipleGroupAffinityThread` |
| `CoreIsolationReasons` | `KiAdjustCoreIsolationReasonThread` |
| `SchedulerSharedOffset` | `PspSchedulerSharedDataRegionSlotFree` |
| `KcsanThread` | `KcsanValidateAddress` |
| `SchedulerAssistYield…` 3칸 | `KiVpBackingThreadYieldExecution`, `KiCommitThreadWait` |

- 칸 수는 결과서 2.4 (줄마다 칸 수 합 70, 빠진 칸 0 · 겹친 칸 0 — 결과서 scratchpad 스크립트로 짝지음). 위 표의 줄임 (`등`) 은 결과서 표에 더 있다.
- **`KeStartThread` (Q78d4 몫) 가 이 138칸 중 쓰는 칸**: `SchedulingGroup`·`HardwareFeedbackClass`·`UserAffinityPrimaryGroup`·`AffinityPrimaryGroup`·`ThreadTimerDelay` 와 `ThreadFlags` DWORD (비트 0·5). 사진 K2 ~ P2 에서 모두 0 이니 0 을 쓰거나 조건이 안 맞은 것 — Q78d4 가 `KeStartThread` 줄을 읽을 때 같이 본다. 지금은 "범위 밖 길" 로 셌다 (Q78d4 에서 "nt 가 씀" 으로 옮길 수 있음).

### test.exe 가 탄 길 (갈림길 넷)

| 갈림길 | 가르는 곳 | test.exe |
|---|---|---|
| 시스템 스레드 | `ThreadContext == 0` (`KeInitThread +0x112`) | 사용자 스레드 — `NtCreateUserProcess +0xe3a` 가 `_CONTEXT` 포인터 (`PspCreateUserContext` `+0xd1d` 가 채운 버퍼) 를 넘김 |
| 커널 스택 | `param_2+0x00 == 0` (`+0x234`) | 새로 만듦 → `+0x273 MmCreateKernelStack` |
| 커널 CET (그림자 스택) | 전역 `KiKernelCetEnabled` (`+0x2b0`) | 꺼짐 (사진 `MiscFlags` 비트 22 = 0) `[관찰 간접]` |
| 사용자 CET | 프로세스 `MitigationFlags2.CetUserShadowStacks` (비트 14, `PspAllocateThread +0x5b9`) | 꺼짐 (sast/44 K1 `0x40000000`, 사진 비트 20 = 0) |

- 그 밖에 안 타는 갈래: 보안 스레드 (플래그 `0x10`, `PspAllocateThread +0xba7` 가 `SecureProcess` 의 시스템 스레드에만), 복제·피코 (`PspAllocateThread +0x5b3 → +0x2ca9a9`, 사용자 스택 인자 0 — test.exe 는 TEB 를 새로 만듦, run44 `[관찰]`), AMX 따로 스택 (`KiAllocateXStateStack`), GUI 스레드 (만들기 길에 없음, `PsConvertToGuiThread` 범위 밖 `[추측]`).

### 간접 호출 (질문 3)

- **만들기 길 안에 `_KTHREAD` 칸을 쓰는 간접 호출은 없다** `[정적]`. `.asm` 14개 (`PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread`·`KeAbInitializeThreadState`·`KeInitializeApc`·`KeInitializeTimer`·`KeInitializeEvent`·`MmCreateKernelStack`·`KiAllocateXStateStack`·`KiCreateKernelShadowStack`·`KeCopyXfdMaskToTeb`·`KxContextToKframes`·`PspSetupUserStack`·`MmCreateTeb`) 에서 `CALL reg`·`CALL [mem]`·`JMP reg` 0개 → Q35 와 무관.
- `KeInitThread` 에 `INT 0x29` 하나 (`+0x1b7`) 는 연결 리스트 망가짐 검사 (fastfail) 다.
- 함수 주소를 **값으로** 넘기는 줄은 있다: `KeInitThread +0x12d` (`KiSchedulerApc` rva `0x2c8950`) · `+0x139`·`+0x145` (하는 일 없는 빈 함수 rva `0x4a10c0` — 같은 주소 이름 100개, 후보 `KiSchedulerApcNop`·`KiSchedulerApcRundown`) → `KeInitializeApc`. 둘째 `KeInitializeApc` (`+0x368`) 는 `KiUpdateTebApc` (rva `0x312820`, ETHREAD 칸 — Q78d2). 이 스레드 만들기 길에서 부르지는 않는다.

### 부모 (지금 도는) 스레드 칸 — 새 스레드 짐이 아님

- `PspAllocateThread` 는 새 프로세스 주소 공간에 TEB·스택을 쓰려고 `KiStackAttachProcess` 를 부른다 (`PspAllocateThread +0x78d`, `MmCreateTeb +0x7a`, `PspSetupUserStack +0x70`). 이 함수는 `GS:[0x188]` (`KiStackAttachProcess +0xe`) = **지금 도는 스레드 = 부모 cmd 스레드** 의 `ApcState`·`ApcStateIndex` (`+0x162`)·`MiscFlags.ProcessDetachActive` (`+0x184` → `+0x238`) 를 썼다 되돌린다 `[정적]`.
- `PspAllocateThread` 안 `KeAbPreAcquire +0x52` (`ThreadFlags` 비트 16)·`KxContextToKframes +0x3ba` (`Header` 비트 24) 도 호출한 쪽 (부모) 스레드 칸이다.
- 그래서 이 줄들은 짐 지도 3.14 (새 스레드) 에 안 넣었다.

## 확인된 것

- **`K1~K2` 42칸 모두 처음 값을 쓰는 줄** — `KeInitThread` 본문, `PspAllocateThread` 본문, `KiInitializeContextThread`, 도우미 셋 (`KeAbInitializeThreadState`·`KeInitializeApc`·`KeInitializeTimer`). `[정적]` · 사진 K2 값과 칸마다 맞음 (짐 지도 3.14)
- **몸통 0 채움 = `PspAllocateThread +0x307 memset`** (몸통 + 확장 영역 전부) `[정적]`
- **`MiscFlags`·`ThreadFlags` 는 `KeInitThread` 가 DWORD 를 읽어 몇 비트만 바꿔 통째로 되쓴다** (`+0xe7 → +0xfd`, `+0x80 → +0x9d`) — 그래서 두 묶음의 나머지 35비트는 "0 을 씀" `[정적]` (오케스트레이터·flow-writer `.asm` 확인)
- **`KeInitThread +0x172` 는 `KeInitializeTimer`** (요청서의 `KeInitializeTimerEx` 아님) `[정적]`
- **`InitialStack` 은 두 번 쓴다**: `KeInitThread +0x29b` (스택 맨 위, `StackBase` 와 같은 값) → `KiInitializeContextThread +0x2ca` (XSAVE 영역 아래로 내린 값) `[정적]`
- **`ApcQueueable` 켜는 조건**: 시작 정보 묶음 플래그 (`+0x3c`) 비트 1 이 꺼져 있을 때 (`KeInitThread +0xf5 ~ +0xfd`). test.exe 는 플래그 0 이라 켜짐 `[정적]`
- **`Teb` (+0xf0) 쓰는 줄 = `KeInitThread +0x1e4`** (값 `+0x1e0` = 시작 정보 묶음 `+0x28` = `MmCreateTeb` 결과) `[정적]` (flow-writer `.asm` 확인). run11 이후 짐작이던 칸 이름도 PDB 와 같음
- **test.exe 첫 스레드 = 사용자 스레드 · 새 커널 스택 · 커널 CET 꺼짐 · 사용자 CET 꺼짐** `[정적]` + 사진 비트 `[관찰 간접]`
- **만들기 길 안 간접 호출 없음** `[정적]`
- 끝까지 0 138칸은 0 을 씀 35 · 조건부 안 탐 12 · 아무도 안 씀 21 · 범위 밖 길 70 `[추측]` 로 갈렸다. 여전히 모름 0

## 정정

- **baggage 3.14 `Teb` 짐작 → 정확** (위). run11 문서는 안 고쳤다 (그때 "칸 이름 `[추측]`" 은 그때 근거로 맞는 표시였다). 짐 지도 7.4 줄 `(해결됨 → run52)`.
- **baggage 3.13 `Cid` 줄의 "`PsAssignThreadId (호출 +0x532)`" 는 ret 자리다** — call 은 `+0x52d` (`_calls.tsv`). 짐 지도는 이 스프린트 문서라 바로 고쳤다 (`호출 +0x52d`).
- **같은 이름 다른 칸**: baggage 3.13 의 `ThreadLock` 두 줄 (`PspAllocateThread +0x51c`, `PspInsertThread +0x499`) 은 **ETHREAD `+0x590` 푸시락** 이다. 이번 바퀴의 `ThreadLock` (끝까지 0, 범위 밖 길) 은 **KTHREAD `+0x40` 스케줄러 스핀락** — 다른 칸이다. 3.13 줄은 틀리지 않았다 (오프셋이 `+0x590`). 두 절 머리에 메모만 달았다.
- **사진 K1 의 이 스레드 주소는 옛 객체 값이다** (만들기 전 — `Process` 가 다른 EPROCESS `…714c080`, 스택 값도 다름). 그래서 `_KTHREAD`·`_ETHREAD` 의 `K1~K2` 는 사실상 "K2 에 처음 값이 보이는 칸" 이고, 끝까지 0 판정은 **K2 ~ P2 일곱 장** 기준이다. K1 에서 0 이 아니던 끝까지 0 칸 5개 (`MiscFlags.CetUserShadowStack`, `SpecCtrl.RunningNonRetpolineCode`, `SpecCtrl`, `BamQosLevel`, `QueueListEntry`) 도 K2 부터 0. run44 의 2.1 표 숫자는 그대로 둔다 (run44 가 K1 열을 이미 "객체가 아직 없음" 으로 셌고 — 3.13 "K1 은 스레드 생기기 전 메모리라 뺐다" — 숫자는 안 바뀜). 짐 지도 2.1 아래에 한 줄.
- 이전 run 문서 맨 위 정정 안내: **없음** (틀린 이전 run 없음 — 위 셋은 보탬·짐 지도 안 표기).

## 아직 확인 필요

- 동적 (결과서 번호 그대로, D1 ~ D4). **D1 은 이미 있는 덤프로 → plan Q79 에 출처로**, D2·D3 은 낮은 우선순위 (실행 중 멈춤, 갈 곳은 오케스트레이터), D4 는 안 해도 됨. 급한 것은 없다 — 이것들을 안 닫아도 3.14 칸 표는 (Q78d4 몫 47 빼고) 완성돼 있다.
  - **D1** (덤프, Q79) 커널 전역 값: `KiKernelCetEnabled` (nt+`0x1201841`), `KeFeatureBits` (nt+`0xfc6b70`, **비트 `0x37`**), `KiIptSaveAreaLength` (nt+`0xfc6c70`), `KiSchedulerAssistThreadFlagEnabled` (nt+`0xfc6e00`), `KiShortExecutionCycles` (nt+`0xfc6df4`, 기대 `0x4c4b4`), `KeKernelStackSize` (nt+`0xfc5020`, 기대 `0x7000`), `KiXSaveAreaLength` (nt+`0xfc5114`, 기대 `0x980` 초과 `0x9c0` 이하), `KeEnabledSupervisorXStateFeatures` (nt+`0xfc6ca0`), KUSD `+0x3d8` (`0xfffff780000003d8`, 기대 비트 2·5·6·7 켜짐, 비트 18 `0x40000` 꺼짐). `dq`/`dd`/`db` 한 번씩. → 조건부 12칸의 "왜 안 탔나" 와 **`ExtendedFeatureDisableMask` `[공백]`** 이 닫힌다. (`KeFeatureBits` 비트 `0x37` 은 run49 D5 와 같은 값 — 한 번에 읽으면 둘 다 닫힘)
  - **D2** (멈춤, 낮음) `nt!PspAllocateThread+0x6f9` (rva `0x843065`) 에서 `dt nt!_KTHREAD @rsi` — `KeInitThread` 가 쓴 칸만 (옛 객체 잔재 없이). 조건: 지금 도는 프로세스 = cmd.exe. K2 값이 이미 코드와 칸마다 맞아 꼭 필요하진 않다.
  - **D3** (멈춤, 낮음) `KernelApcDisable`·`CombinedApcDisable` 이 새 스레드 만들기 길에서 정말 안 쓰이나 (인라인 크리티컬 리전 줄은 훑기에 안 잡힐 수 있음) — 새 스레드 주소가 정해진 뒤 `ba w2 <스레드>+0x1e4`.
  - **D4** (안 함) 사진 K1 자리의 옛 객체가 왜 남아 있나 — 판정에 안 씀.
- 정적 (넘길 것 — 짐 지도 절 머리 메모):
  - **Q78d2** (`_ETHREAD`·`_KTRAP_FRAME`, 짐 지도 3.13·3.15 머리 "Q78d2 에서 볼 것"): `PspAllocateThread` 가 쓰는 ETHREAD 줄 목록 (`CreateTime` `+0x4bf`, 자기 연결 리스트 머리 여덟, 락 칸 0 다섯, `Silo` −3 `+0x49f`, `MmSlabIdentity` `0xff` `+0x41a`, `ReadClusterSize` `0xf` `+0x410`, `Cid` 둘, `KeyedWaitSemaphore` `+0x42d`, `EnergyValues` `+0x31a`, `CrossThreadFlags` 비트 둘, `ProcessFastRef` `+0x71a`, `UpdateTebApc` ← `KeInitThread +0x368`). 트랩 프레임 두 줄 — **`KiInitializeContextThread +0x187 memset(트랩 프레임 = R15 − 0x190, 0x190)`** → `+0x198` `ExceptionActive` (+0x2b) = 1, `+0x1f1` `MxCsr` (+0x2c) = `0x1f80`. 이 `memset` 이 **트랩 프레임 끝까지 0 32칸 가름의 실마리** (몸통 0 채움). `EFlags*` ← `KxContextToKframes` (안 읽음).
  - **Q78d3** (`_TEB`, 짐 지도 3.3 머리 메모): `PspAllocateThread +0x793` (`MOV [TEB+0x17ee], R12W`) — `SameTebFlags`. 값은 만들기 플래그에서 만든 16비트 (`+0x54a ~ +0x5a5`: 플래그 `0x40` → `InitialThread` (비트 10), `0x2` → `SkipThreadAttach` (비트 3), `0x80` → `LoaderWorker` (비트 13), `0x100` → `SkipLoaderInit` (비트 14)). 복제 갈래는 TEB `ClientId` 등도 씀 (`+0x2cab6d ~ +0x2cabbd`, 안 탐).
  - **Q78d4** (`K2` 뒤 구간, 짐 지도 3.14 머리 "Q78d4 에서 볼 것"): `KeStartThread` 가 스레드 칸을 쓰는 줄 (`Header` 비트 18 `+0x339`·비트 23 `+0x353`, `ThreadFlags` `+0xb9`·`+0x1b7`·`+0x1d3`·`+0x20f`, `Priority` `+0xc8`, `UserIdealProcessor` `+0x17c`, `IdealProcessor` `+0x187`, `BasePriority` `+0xc2`, `QuantumTarget` `+0x1dc`, `QuantumReset` `+0x1c1`, `WobPriority` `+0xce`, 그리고 범위 밖 길 5칸 `+0x32b`·`+0x380`·`+0x156`·`+0x14f`·`+0x36c`) → 7.2 `K2~K3` 10칸 중 8칸. 친화도 블록 비트맵이 채워지는 때 `[공백]`. `ModeHistory` 틱마다 밀기·`ExpectedRunTime` 갱신 — 이름만 `[추측]`.
  - (작은 것) "범위 밖 길" 70·"아무도 안 씀" 21 의 쓰는 함수를 `.asm` 로 한 줄씩 확인 (지금은 오프셋 숫자 훑기, `LEA` 뒤 쓰기는 못 잡음). 결과서 도구 제안 (`field_writers.py`) 이 있으면 쉬워짐.
- 칸 표가 없는 짐 후보 (결과서 정정 8) → 짐 지도 7.6 에 **기록만**: 몸통 뒤 확장 영역의 `_KAFFINITY_EX` 2개, `_KLOCK_ENTRIES` 2개 (커널용 6칸 `0x220`B / 사용자용 8칸 `0x2d0`B), 에너지 칸 (`0xc8`B), 커널 스택 7쪽 안의 XSAVE 영역 (`0x9c0`B) · 트랩 프레임 (`0x190`B — 칸 표 3.15 있음) · 시작 프레임.

## 그림

- flow.md 그림 (mermaid) 은 안 바꿨다. 이번 바퀴는 `PspAllocateThread`·`KeInitThread` 안 대표 지점이 늘었을 뿐 (몸통 `memset`·`KeInitializeTimer`·`MmCreateKernelStack`·`KeAbInitializeThreadState`) 새 단계·새 caller 가 없다. 2.1 글 트리에 run49·run50 처럼 `·` 줄로만 넣었다 ("그림엔 없음").

## 도구 메모

- 결과서 "도구 평가" — 하네스라 오케스트레이터가 사용자에게:
  - **제안 1 (sast/43·44 제안을 다시, 더 구체적으로)**: `scripts/field_writers.py <바이너리> --struct _KTHREAD [--cells 목록]`. 이번에 더 필요했던 것 둘 — (a) **DWORD 안 비트별 분리** (`BTS`/`BTR`/`OR imm`/`AND imm` 즉시값을 풀어 "비트마다 쓰는 곳"), (b) **칸이 중첩 구조체의 시작일 때** (`SchedulerApc` = `_KAPC`, `Timer` = `_KTIMER`, `WaitBlock` = `_KWAIT_BLOCK[4]` …) 호출 지점의 `LEA RCX,[RDI+칸]` 인자와 callee 의 구조체 오프셋 쓰기를 짝지어 주기.
  - 제안 2 (사소): `pdb_aliases.py --lookup` 이 이름 하나뿐인 주소의 그 이름도 알려 주기 (이번에 `0x2c8950` 등을 `_index.tsv` 로 따로 찾음).
  - `pdb_struct.py --expand` (앞 바퀴 제안) 는 이번에도 `MiscFlags`·`ThreadFlags`·`Header` 비트에 필요했다.
- 이번 바퀴엔 디버거를 안 붙였다. 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

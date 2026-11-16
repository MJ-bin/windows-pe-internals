# run112 thread-later — 해설

> 이 글은 [run112 문서](run112-thread-later.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

> **고쳐 읽기 (run113)**: 2.12 "헷갈릴 수 있는 곳" 의 `[추측: 이 해설의 읽기]` (`int3` 창구가 `Rax` · `R8 ~ R11` 에 0 을 적었다) 는 run113 이 받쳤다. `KiBreakpointTrap +0x18 · +0x24 ~ +0x30` 은 P1 에서도 이 다섯 칸을 적고 `[정적]`, 같은 입구가 같은 때 적은 `Rcx` · `Rdx` · `ExceptionActive` 는 P1 사진에서 0 이 아니다 (`ms/logs/04_baggage-read_P1.txt:1061 ~ 1069`) `[관찰]`. 그래서 "P1 에서도 0" = "0 을 써서 0", 표의 "P1a 부터 유저 값" = "0 이 아닌 첫 값". 셈 (덮임 25) 은 그대로. 본문은 그대로 두었다 → [run113 해설 2.12](run113-ntdll-user-structs-해설.md#212-정정--run112-트랩-프레임-그리고-요청서-짐작)

- 대기열: **Q78d4** — Q78d (첫 스레드) 넷 중 **넷째 · 마지막 조각**. 스레드 "그 뒤 구간" (커널) 의 모르는 칸 82 (`_workspace/plan.md`, 이번에 **끝** → **Q78d 전체가 끝**)
  - Q78d 를 나눈 법: Q78d1 `_KTHREAD` 만들 때 180 ([run52](run52-kthread-create-해설.md)) → Q78d2 `_ETHREAD`·`_KTRAP_FRAME` 만들 때 169 ([run53](run53-ethread-create-해설.md)) → Q78d3 `_TEB` 133 ([run54](run54-teb-fill-해설.md)) → **Q78d4 그 뒤 구간 82 (이번)**. 합 564.
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다.
  - 다음 문서 차례: **Q78e1** (sast/109) → … (`plan.md` 맨 위 "다음 항목").
  - 남은 것이 간 곳 (4절):
    - 동적 → 새 **Q200** ① (급함) · ② · ③, 그리고 이미 있던 **Q79** (커널 전역 덤프) · **Q35** (syscall 번호) 에 덧붙임
    - 정적 → 새 **Q201** ① ~ ⑤
- 근거:
  - run 문서 [`run112-thread-later.md`](run112-thread-later.md) (가장 중요)
  - 요청서 [`_workspace/sast/108_thread-later.md`](../../_workspace/sast/108_thread-later.md) · 결과서 [`108_thread-later_result.md`](../../_workspace/sast/108_thread-later_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다.** 글은 기준 · 2.1 · 2.2 · 2.3 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): 3.13 · 3.14 · 3.15 **다시 씀**, 2장 숫자가 **크게** 바뀜 (3절 표).
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 이 글의 `[관찰]` 은 모두 **run44 때 찍어 둔 사진 일곱 장** (K2 · K3 · P0 · P1 · P1a · P1b · P2) 을 결과서가 칸마다 다시 읽은 것이다.
- **결과서와 run 문서의 숫자가 다른 곳이 하나 있다** (`KeStartThread` 5칸, 2.6). 이 글은 **run 문서 숫자**를 따른다.

---

### 이미 푼 말 — 링크만

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) · 인사 카드 (`_ETHREAD`) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- `_KTHREAD` = 인사 카드 첫 장 **업무 카드** → [run52 해설 "비유"](run52-kthread-create-해설.md#비유-그대로-이어-쓴다)
- 네 가지 가름 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길) → [run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법)
- **DWORD 되쓰기** = 표시 묶음을 통째로 다시 쓰면 안 바꾼 비트에도 "0 을 쓴" 것 → [run52 해설 2.7](run52-kthread-create-해설.md#27-핵심-2-dword-하나를-되쓰면-왜-다른-비트에-0-을-쓴-것이-되나)
- 지우개 (`memset`) 는 안 세고 펜만 센다 → [run53 해설 2.4](run53-ethread-create-해설.md#24-핵심-1-memset-이-칸을-0-으로-밀었는데-왜-0-을-씀-이-아닌가)
- 트랩 프레임 = 직원 책상의 **출발 메모** → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- **이상적 프로세서** = 직원의 **선호 자리**, **친화도** = **앉아도 되는 자리 목록** → [run54 해설 2.9](run54-teb-fill-해설.md#29-p0p1-5칸-첫-출근-날-적는-선호-자리)
- CPU = **계산대**, 준비 줄 (우선순위별 줄, "같이 쓰는 줄") → [run75 해설 2.5](run75-thread-switch-해설.md) · 교대 (`SwapContext`) → [run75 해설 2.8](run75-thread-switch-해설.md#28-교대--kiswapcontext--swapcontext)
- 퀀텀 = 모래시계. `QuantumTarget` = **눈금**, `CycleTime` = **쌓인 모래**, `KiCyclesPerClockQuantum` = 눈금 한 칸 → [run76 해설 2.6](run76-thread-quantum-해설.md#26-눈금-한-칸은-얼마--첫-눈금과-kicyclesperclockquantum-질문-2-이어서)
- 기다림 번호표 (`WaitBlock`) · 커밋 · 깨움 → [run77 해설 2.6](run77-thread-wake-해설.md) · [2.10](run77-thread-wake-해설.md#210-깨운-다음--준비-줄까지-그리고-둘째-넘기기-길)
- 멈춰 둔 직원 풀어 주기 (`NtResumeThread`), 쪽지 (APC) → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- 오프셋 순서 ≠ 실행 순서 → [run12 해설 2.3](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다)
- 시스템 콜 = **신청 창구**, 시스템 콜 번호 = **단축 번호**, 서비스 표 → [run56 해설 "비유"](run56-syscall-args-해설.md#비유-그대로-이어-쓴다)
- `int3` · 페이지 오류 입구 = **사고 접수 창구** → [run81 해설 2.4](run81-exception-kernel-해설.md)
- `KUSER_SHARED_DATA` (KUSD) = 커널이 모든 프로세스에 보여 주는 공용 정보 페이지 → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md)
- 스케줄러 공유 데이터 = 관리사무소가 가게 안에 갖다 놓는 **카드 묶음 상자**, 카드 받기 (클래스 `0x39`) → [run65 해설 2.9](run65-ldr-init-bodies-해설.md) · [run110 해설 "비유"](run110-tls-lock-small-해설.md)
- 남의 땅에 잠깐 들어가기 (`KiStackAttachProcess`) → [run10 해설](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)
- 프리페처 → [run51 해설](run51-eprocess-later-해설.md#새-말-프리페처)
- `KiKvaShadow` (유저로 나가는 문이 둘) → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- 사진 지점 K2 ~ P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **트랩 프레임 덮임** → 2.12 (이번 바퀴에서 새로 생긴 가름)
- 이번에만 나오는 칸 이름 몇 개 (`KernelStackReference` · `AffinityVersion` · `ResourceIndex` · `SavedApcState`) 는 그 자리에서 한 줄씩.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, CPU = **계산대**.
- `_ETHREAD` = **인사 카드**, 그 첫 장 `_KTHREAD` = **업무 카드**, TEB = 직원 **개인 안내판**.
- run 문서가 이번에 쓴 그림 (그대로 가져온다):
  - `PspInsertThread` = **인사과**. 가게 직원 명부에 이름을 올리고, 사원증 칸 (`CrossThreadFlags`) 에 가게 기본값을 찍는다.
  - `KeStartThread` = **근무 조건 담당**. 근무 등급 (`Priority`) · 모래시계 눈금 (`QuantumTarget`) · 선호 자리 (`IdealProcessor`) 를 정한다.
  - `KiDeferredReadySingleThread` = **줄 관리인**. 새 직원을 준비 줄에 세우며 시각 (`WaitTime`) · 자리표 판 번호 (`AffinityVersion`) 를 적는다.
- **하나만 바꿔 부른다.**
  - run 문서는 트랩 프레임을 "**출입문 기록지**" 라고 불렀다.
  - 이 글은 앞 해설들대로 **출발 메모** 라고 부른다. 대신 이번에 새로 안 것을 붙인다: 이 메모는 직원이 관리사무소 **창구를 지날 때마다 그 창구가 다시 적는다** (2.12). run 문서의 "출입문 기록지" 와 같은 뜻이다.

### 한 줄로 먼저 — 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| 스레드 그 뒤 구간 모르는 칸 | **82 → 여전히 모름 0** |
| 82칸이 간 곳 | 아는 칸 **55** · 범위 밖 길 `[추측]` **2** · **트랩 프레임 덮임 25** (새 가름, 짐 아님) |
| `K2~K3` 15칸 | `PspInsertThread` (`_ETHREAD` 5) + `KeStartThread` (`_KTHREAD` 9) + `KeSuspendThread` (`SuspendCount` 바이트) `[정적]`, 사진 K3 와 칸마다 맞음 `[관찰]` |
| `K3~P0` 13칸 | 부모의 `NtResumeThread` → … → `KiDeferredReadySingleThread` (준비 줄) + `KiSwapThread` · `SwapContext` (첫 계산대) `[정적]` |
| `P0 ~ P2` 27칸 | 기다림 · 깨움 · 파일 카운터 · syscall 입구 · 스케줄러 공유 표 `[정적]` |
| `CrossThreadFlags` 비트 칸 | 17칸을 **"0 을 씀" 으로 다시 셈** (DWORD 되쓰기, 오케스트레이터 결정) |
| `KeStartThread` "범위 밖 길" 5칸 | 쓰는 줄 확인 → run 문서는 **"nt 가 씀"** 으로 옮김 (결과서는 그대로 둠, 2.6) |
| `UserIdealProcessor` · `UserAffinity` 최종 출처 | **`KeStartThread`**. 다시 쓰는 길 `KiQueueTebUpdateApc` 는 초기화에서 **안 탄다** `[정적: 호출 길 없음]` |
| run77 `WaitTime` `[추측]` | **닫힘** → `KiDeferredReadySingleThread +0x11f8` `[정적]` |
| 간접 호출 | 칸을 직접 쓰는 것 **0개** `[정적]` |
| 정정 | **run52** 의 `ThreadFlags` 비트 13 쓰는 줄 (2.15) |
| 못 닫은 것 | `P0~P1` 에 **커널이 파일을 한 번 읽고 한 번 기다렸다** `[관찰]` — 누가인지 정적으로 못 정함 (급함, Q200 ①) |

이 글에서 붙잡아 둘 세 줄:

> **(1) 첫 스레드 커널 구조체 셋에 "모름" 이 하나도 안 남았다.**
> 82칸 = 아는 칸 55 + 범위 밖 2 + 트랩 프레임 덮임 25. 짐 지도 2장의 모름이 300 → 218 로 줄었다.
>
> **(2) 트랩 프레임 25칸은 짐이 아니다.**
> 창구 (`int3` · 시스템 콜 · 페이지 오류) 를 지날 때마다 그 순간 레지스터로 다시 적는 칸이다. 한 번 놓고 두는 물건이 아니다.
>
> **(3) 사진이 말하는데 코드로는 못 정한 것이 하나 있다.**
> 새 스레드가 `P0~P1` 에서 커널 안에서 파일을 읽고 기다렸다. 지도엔 그런 일이 없었다. 후보는 프리페처 `[추측]` → 동적 Q200 ①.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run52 · 53 · 54 는 첫 직원을 **뽑을 때** (`PspAllocateThread` 안, `K1~K2`) 를 봤다. 이번엔 그 **다음** 이다: 명부에 올리고 → 풀어 주고 → 처음 계산대에 서고 → 일하는 동안.

```
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)
 커널, 부모 cmd 스레드 안  (K2~K3)
  NtCreateUserProcess
   ├─ PspAllocateThread            첫 직원 뽑기 (run52 · 53 · 54)
   └─ +0x101a  PspInsertThread     인사과: 명부에 올리기                 ← 이번 (2.3)
        ├─ (1) +0x5e2  KeStartThread    근무 조건 담당                    ← 이번 (2.4)
        ├─ +0x2cd  KernelStackReference = 1                               ← 이번
        ├─ +0x2e5  KeSuspendThread      멈춰 두기 (CREATE_SUSPENDED)       ← 이번
        └─ +0x617 ~ +0x649  CrossThreadFlags 되쓰기                       ← 이번 (2.5)
 부모의 다음 시스템 콜  (K3~P0)
  NtResumeThread → … → KiDeferredReadySingleThread   줄 관리인            ← 이번 (2.7)
 새 스레드가 처음 계산대에  (K3~P0)
  KiSwapThread → KiSwapContext → SwapContext                              ← 이번 (2.7)
 test.exe 메인 스레드  (P0 ~ P2)
  PspUserThreadStartup     P0~P1: 기다림 한 번 · 파일 읽기 한 번 — 누가?   ← 이번 (2.10)
  → LdrInitializeThunk (P1 = int3 로 멈춘 곳) → ntdll 로더
       시스템 콜마다: KiSystemCall64 → KiSystemServiceUser → KiSystemServiceStart ← 이번 (2.8 · 2.12)
       NtSetInformationThread (0x39) → PspSchedulerSharedDataRegionSlotAllocate  ← 이번 (2.8)
  → NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다 (run 문서 "호출 관계" 그대로).
- `PspInsertThread` 아래 줄은 **오프셋 순서가 아니라 흐름 순서** 로 적었다. `KeStartThread` 가 제일 먼저다 (run12).
- 지도의 큰 길은 **그대로** 다. 새 단계도 새 caller 도 없다. 이미 있던 상자 **안쪽** 을 채웠다.

### 무엇이 궁금했나

짐 지도 7.2 에 첫 스레드 커널 구조체의 "모르는 칸" 이 82 남아 있었다.

- `_ETHREAD` 10 · `_KTHREAD` 47 · `_KTRAP_FRAME` 25.
- 모두 **뽑은 뒤** 구간 (`K2~K3` ~ `P1b~P2`) 에 처음 값이 생기는 칸이다.

질문 넷 (요청서):

1. `K2~K3` 15칸: 누가 어느 줄에서 쓰나?
2. `K3~P0` 이후 42칸: 누가 쓰나? (스케줄러 · 기다림 · syscall 입구 쪽)
3. `_KTRAP_FRAME` 25칸: 어떻게 가르나?
4. 간접 호출이 칸을 쓰나?

덧붙여 앞 바퀴들이 넘긴 것 둘:

- `CrossThreadFlags` 비트 칸 19개를 run52 · 53 기준 (DWORD 되쓰기) 으로 "0 을 씀" 으로 다시 셀 수 있나?
- TEB 의 선호 자리 · 앉아도 되는 자리 다섯 칸을 **나중에 다시 쓰는 길** (`KiQueueTebUpdateApc`) 이 첫 스레드에서 타나? (run54 넘김)

### 바퀴 전에 알던 것

- `[정적]` (run12) `PspInsertThread` 가 `KeStartThread` 를 부르고, 만들기 플래그 비트 0 이면 `KeSuspendThread` 로 멈춰 둔다.
- `[정적]` (run49) `KeStartThread` 의 **프로세스 쪽** 칸 (`ProcessListEntry` · `ThreadListHead` · `StackCount`). 스레드 쪽은 Q78d 로 넘김.
- `[정적]` (run52 메모) `KeStartThread` 가 쓰는 스레드 칸 후보. 그중 "비트 13 은 `+0xb9` · `+0x1b7`" 줄은 이번에 **틀린 것으로** 밝혀졌다 (2.15).
- `[정적]` (run53 메모) `PspInsertThread +0x2cd`, `+0x617 ~ +0x649` (`CrossThreadFlags` 비트 9 ~ 14), `+0x6e3`.
- `[정적]` (run54) TEB `P0~P1` 다섯 칸은 `KeUpdateTebSchedulingPropertiesCurrentThread` 가 업무 카드 값을 읽어 쓴다. 다시 쓰는 길이 탈지는 몰랐다.
- `[정적]` (run75 ~ run78) 스케줄러 · 퀀텀 · 기다림 · 타이머 줄 후보를 짐 지도 3.14 머리 "Q78d4 에서 볼 것" 에 모아 둠. 칸 수는 안 바꿈.
- `[추측]` (run77, 짐 지도 3.14 머리 메모) `WaitTime` 의 P0 값 `0x14dff` 는 기다림 **전** 이라 다른 쓰는 줄이 있을 것.
- `[관찰]` (run44) 이 칸들의 사진 값 일곱 장.

---

## 2. 찾은 것

### 2.1 한눈에 — 82칸은 어디로 갔나

run 문서 "요약 표" 를 구간만 묶어서 옮겼다.

| | 칸 | 아는 칸 (nt 가 씀) | 범위 밖 길 `[추측]` | 트랩 프레임 덮임 | 여전히 모름 |
|---|---|---|---|---|---|
| `_ETHREAD` (`K2~K3` 5 · `K3~P0` 4 · `P1~P1a` 1) | 10 | 10 | — | — | 0 |
| `_KTHREAD` `K2~K3` | 10 | 10 | — | — | 0 |
| `_KTHREAD` `K3~P0` | 9 | 9 | — | — | 0 |
| `_KTHREAD` `P0~P1` | 9 | 8 | 1 (`SavedApcStateFill`) | — | 0 |
| `_KTHREAD` `P1~P1a` | 7 | 6 | 1 (`ResourceIndex`) | — | 0 |
| `_KTHREAD` `P1a~P1b` · `P1b~P2` | 12 | 12 | — | — | 0 |
| `_KTRAP_FRAME` (`P1~P1a` 24 · `P1a~P1b` 1) | 25 | — | — | 25 | 0 |
| **합** | **82** | **55** | **2** | **25** | **0** |

- 아는 칸 55 를 구간으로 보면 (flow.md 4장): `K2~K3` 15 · `K3~P0` 13 · `P0 ~ P2` 27.
- 사진과 칸마다 맞춰 봤다 `[관찰]` run44.

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `├─ +0x617 ~ +0x649  CrossThreadFlags DWORD 되쓰기 (비트 9~14 ← EPROCESS +0x1f4 · +0x1f0)`

- **누가**: `PspInsertThread` (인사과). 부모 cmd 스레드가 커널 안에서 돌리는 코드다.
- **어디서**: `+0x617` 에서 4바이트 묶음을 **읽고**, `+0x632` · `+0x649` 에서 **통째로 두 번 되쓴다**.
- **누구를**: 새 직원 인사 카드 `+0x5a0` `CrossThreadFlags`.
- **무엇을 위해**: 가게 (`_EPROCESS`) 의 기본 IO 우선순위 (`+0x1f4`) 와 쪽 우선순위 (`+0x1f0`) 를 직원 사원증에 옮겨 찍으려고.
- 모두 `[정적]`. 사진 K3 `0x5402` `[관찰]`.

#### 줄 ②: `├─ +0x3b5  NextProcessor   +0x11f8 WaitTime = TickCount`

- **누가**: `KiDeferredReadySingleThread` (줄 관리인).
- **언제**: 부모가 `NtResumeThread` 로 새 직원을 풀어 줄 때. **새 직원은 아직 한 번도 안 돌았다.** 이 줄을 돌리는 건 부모 스레드다.
- **어디서 / 누구를**: `+0x11f8` 에서 새 직원 업무 카드 `WaitTime` (`+0x1b4`) 에.
- **무엇을**: KUSD `TickCount` 의 아래 32비트 (`[0xfffff78000000320]`). 줄에 선 **시각** 이다.
- `[정적]`. 사진 P0 `0x14dff` `[관찰]`. 이 줄이 run77 의 `[추측]` 을 닫았다 (2.7).

#### 줄 ③: `KiSystemCall64 ─(+0x2a2 이어짐)─ KiSystemServiceUser  +0xcb FirstArgument · +0xd2 SystemCallNumber`

- **"─( )─"** 는 호출이 아니라 **이어지는 길** 이다. `KiSystemCall64` 가 `+0x2a2` 에서 그대로 `KiSystemServiceUser` 로 흘러간다.
- **누가**: test.exe 메인 스레드 자신. ntdll 이 시스템 콜을 낼 때마다 지나는 **신청 창구 입구** 다.
- **무엇을**: 업무 카드에 이번 **단축 번호** (`SystemCallNumber`, `+0xd2`) 와 첫 인자 (`FirstArgument`, `+0xcb`) 를 적는다.
- `[정적]`. 사진 P1a `SystemCallNumber` `0xd` `[관찰]`.
- 창구를 지날 때마다 다시 적는 칸이라, 사진엔 **마지막 번호** 만 남는다.

### 2.3 `K2~K3` ① — 인사과 `PspInsertThread`: 인사 카드 5칸

모두 `[정적]`, 사진 K3 와 맞다 `[관찰]` run44.

| 칸 | 쓰는 줄 | 값의 출처 | 사진 K3 |
|---|---|---|---|
| `+0x578` `ThreadListEntry` | `+0x6e3` (`LEA`) → `+0x239 ~ +0x243` 꼬리 달기 (앞 `+0x6bb ~ +0x6d5` 프로세스 푸시락) | 가게 `_EPROCESS +0x370 ThreadListHead` | 자기 연결 한 칸 (첫 직원) |
| `+0x5a0` `CrossThreadFlags` | `+0x617` 읽기 → `+0x632` · `+0x649` 통째 되쓰기 | 아래 두 줄 | `0x5402` |
| ㄴ `ThreadIoPriority` (비트 9 ~ 11) | `+0x61e ~ +0x632` | 가게 `+0x1f4 Flags.DefaultIoPriority` | 2 |
| ㄴ `ThreadPagePriority` (비트 12 ~ 14) | `+0x63b ~ +0x649` | 가게 `+0x1f0 Flags2.DefaultPagePriority` | 5 |
| `+0x5ec` `KernelStackReference` | `+0x2cd` | 상수 1 (`+0xbc MOV R13D,1`) | 1 |

- **`ThreadListEntry`** = 가게 **직원 명부** 의 한 줄.
  - 첫 직원이라 명부엔 자기 하나뿐이다. 그래서 사진에 "자기 연결 한 칸" 으로 보인다.
  - 업무 카드 쪽에도 명부가 하나 더 있다 (`_KTHREAD +0x2f8`, 2.4). 인사과 명부와 근무 조건 명부, 두 권에 같은 직원이 올라간다고 보면 된다.
- **`KernelStackReference`** = 칸 이름 그대로 "커널 스택을 붙잡고 있는 수" 로 읽힌다 `[추측: 이름]`.
  - 바로 앞 줄 `+0x2c8` 이 `ObReferenceObjectExWithTag(스레드, 2, 'Dflt')` 를 부르고, 그 직후 `+0x2cd` 가 1 을 적는다 `[정적]`.
  - 뒤에 이 수를 올리고 내리는 쪽은 이름만 있다: `PsReferenceKernelStack` · `PsDereferenceKernelStack` · `PspReaper` (결과서).
- **IO 우선순위 · 쪽 우선순위** 는 가게 기본값을 그대로 옮긴다 `[정적]`. 각각 디스크 일 · 메모리 쪽을 다룰 때 이 직원 몫을 얼마나 앞에 세울지 정하는 값으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

### 2.4 `K2~K3` ② — 근무 조건 담당 `KeStartThread`: 업무 카드 9칸 + 멈춤 횟수

`RBX` = 새 직원, `RDI` = 가게 (프로세스). 모두 `[정적]`, 사진 K3 와 맞다 `[관찰]`.

| 칸 | 쓰는 줄 | 값의 출처 | 사진 K3 |
|---|---|---|---|
| `+0xc3` `Priority` · `+0x233` `BasePriority` | `+0xc8` · `+0xc2` | 가게 `BasePriority` | 8 · 8 |
| `+0x28b` `QuantumReset` | `+0x1c1` | 가게 `+0x91 QuantumReset` | 2 |
| `+0x20` `QuantumTarget` | `+0x1dc` (`+0x1c7 IMUL`) | 가게 `QuantumReset` × 전역 `KiCyclesPerClockQuantum` | `0x7f2814` |
| `+0xc4` `UserIdealProcessor` · `+0x24c` `IdealProcessor` | `+0x17c` · `+0x187` (값 `+0x173 KeSelectInitialIdealProcessorForThread`) | 계산 (입력 `R8` ≠ 0 이면 입력값 — test.exe 는 0) | 1 · 1 |
| `+0x2f8` `ThreadListEntry` | `+0x1e9 ~ +0x2fe` (목록 잠금 `+0x1e0` 안) | 가게 `_KPROCESS +0x30 ThreadListHead` | 자기 연결 한 칸 |
| `+0x31b` `WobPriority` | `+0xce` | 상수 `0x20` | 32 |
| `+0x78` `ThreadFlags` 비트 13 `SharedReadyQueueAffinity` | `+0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55` | 계산 | 1 |
| `+0x258` `SavedApcState` 꼬리 바이트 `SuspendCount` (`+0x284`) | `PspInsertThread +0x2e0 → KeSuspendThread +0x60` | 상수 (+1) | 1 |

한 줄씩 짚어 보면:

- **근무 등급** (`Priority`) 은 가게 등급을 그대로 받는다. 그래서 첫 직원은 8.
- **모래시계 눈금** (`QuantumTarget`) = 가게 `QuantumReset` (2) × 눈금 한 칸 (`KiCyclesPerClockQuantum`).
  - 결과서 계산: `0x7f2814` = 2 × 4,166,666. 전역 값 자체는 덤프에서 확인할 몫이다 (Q79, 4절).
- **선호 자리** (`UserIdealProcessor` · `IdealProcessor`) 는 `KeSelectInitialIdealProcessorForThread` 가 고른다.
  - 그 안의 실제 계산 (`KeSelectIdealProcessor`) 은 이름만 봤다 → Q201 ⑤.
  - test.exe 는 "이 자리로 해 주세요" 라는 입력이 없다 (입력 `R8` = 0). 그래서 `+0x20f` 비트 0 도 안 켜진다.
- **`WobPriority`** 는 상수 `0x20` 을 적는다. 칸의 뜻은 이번 근거에 없다.
- **비트 13 `SharedReadyQueueAffinity`** = "앉아도 되는 자리 목록 (친화도) 이 **같이 쓰는 줄** 의 계산대들을 다 덮으면 1" (run 문서).
  - [run75 해설 2.5](run75-thread-switch-해설.md) 에서 본 "이 계산대 줄" / "같이 쓰는 줄" 의 그 줄이다.
  - 이 줄이 run52 정정의 주인공이다 (2.15).
- **`SuspendCount`** 는 `KeStartThread` 가 아니라 바로 뒤 `KeSuspendThread` 가 1 로 올린다. "멈춰 둠 1번" 이다.
  - P0 에선 다시 0 이다. 부모의 `NtResumeThread` 가 내렸다 (결과서).

같은 함수가 칸 밖에서 쓰는 것도 봤다 (run 문서, 요청서 칸 밖):

- `+0xb9` · `+0x1b7` = `ThreadFlags` **DWORD 통째 쓰기**. 비트 3 `DisableBoost` · 비트 8 `DisableQuantum` 에 가게 값을 넣는다.
- 친화도 비트맵: `+0xec` · `+0x134` · `+0x147` `KiCopyAffinityEx` 가 가게 `[RDI+0x50]` → 지역 사본 → `Affinity` · `UserAffinity` 로 복사한다.
  - **run52 의 `[공백]` "친화도 블록 비트맵이 채워지는 때" 가 이걸로 닫혔다** `[정적]`.
- 그리고 2.6 의 다섯 칸.

### 2.5 `CrossThreadFlags` 17칸 — 왜 "0 을 씀" 으로 다시 세나

[run52 해설 2.7](run52-kthread-create-해설.md#27-핵심-2-dword-하나를-되쓰면-왜-다른-비트에-0-을-쓴-것이-되나) 의 규칙을 그대로 쓴다. 표시 묶음 (DWORD) 을 통째로 되쓰면, 안 바꾼 비트에도 펜이 닿은 것이다.

#### 단계별로

1. `[정적]` `PspInsertThread +0x617` 이 `CrossThreadFlags` 4바이트를 **읽는다**.
2. `[정적]` 바꾸는 비트는 9 ~ 14 (`ThreadIoPriority` · `ThreadPagePriority`) **뿐** 이다.
3. `[정적]` `+0x632` · `+0x649` 가 그 4바이트를 **통째로 두 번** 쓴다.
4. 그래서 → 나머지 비트에는 읽은 값 (0) 을 **그대로 다시 적은** 것이 된다. run52 · 53 기준이면 "0 을 씀" 이다.
5. `[관찰]` 사진 K3 ~ P2 일곱 장이 모두 `0x5402`.
   - 켜진 비트는 아는 칸 셋뿐이다: `ThreadInserted` (비트 1, 앞 `+0x490` 이 켬) · IO 우선순위 2 · 쪽 우선순위 5.
   - 결과서 풀이: `0x5402` = 비트 1 + 비트 10 + 비트 12 + 비트 14.

#### 19칸이 어떻게 갈렸나

| 가름 (바퀴 전) | 칸 수 | 이번 |
|---|---|---|
| 조건부 안 탐 (`HideFromDebugger` · `IndirectCpuSets`) | 2 | **그대로** |
| 아무도 안 씀 (`RundownFail` · `UmsForceQueueTermination` · `ReservedCrossThreadFlags`) | 3 | → **0 을 씀** |
| 범위 밖 길 (`Terminated` · `ActiveImpersonationInfo` … `ImpersonationSchedulingGroup`) | 14 | → **0 을 씀** |

- **조건부 2칸은 왜 그대로인가?** 이 둘은 켜는 줄이 `PspAllocateThread` 에 따로 있고, test.exe 는 그 갈래를 안 탄다 (sast/47, 결과서). 가름의 이유가 "그 갈래를 안 탐" 이라서 바꾸지 않았다.
- **이건 셈 방식 결정이다** (오케스트레이터 결정, run52 · run53 기준 그대로).
  - 코드 사실 (`+0x632` · `+0x649` 가 DWORD 통째로 씀) 은 `[정적]` 이다.
  - 그걸 "0 을 씀" 으로 **세느냐** 는 규칙의 문제다.
- 범위 밖 14칸을 나중에 **켜는** 쪽 (`NtSetInformationThread` · `PsImpersonateClient` · `Dbgk…` 등) 은 이름만 남는다 `[추측]` (sast/47 2.4).
- 같은 방법이 `SameThreadPassiveFlags` · `SameThreadApcFlags` 에는 **안 먹는다**. 그 DWORD 를 되쓰는 줄이 만들기 길에 없다 (sast/47 1장, 결과서).

### 2.6 `KeStartThread` 5칸 — run 문서가 결과서와 다르게 센 곳

이 글의 숫자가 결과서와 다른 까닭이 여기 있다.

#### 무엇이 있었나

- run52 (sast/46) 는 `_KTHREAD` 의 다섯 칸을 "**범위 밖 길**" 로 셌다: `SchedulingGroup` · `HardwareFeedbackClass` · `UserAffinityPrimaryGroup` · `AffinityPrimaryGroup` · `ThreadTimerDelay`.
- 이번에 결과서가 그 다섯 칸을 쓰는 줄을 `KeStartThread` 안에서 찾았다. flow-writer 도 `.asm` 에서 봤다: `+0x32b` · `+0x380` · `+0x156` · `+0x14f` · `+0x36c` `[정적]`.
- 이 줄들은 **만들기 길 (`①ST…`) 안** 에 있다.

#### 두 셈

| | 결과서 | run 문서 (이 글) |
|---|---|---|
| 다섯 칸 | 범위 밖 그대로 (`CrossThreadFlags` 만 다시 셈) | **"nt 가 씀 (0 을 씀 / 입력값)"** — 짐 지도 3.14 새 줄 `+0x68` |
| `_KTHREAD` 아는 칸 · 범위 밖 | 144 · 72 | **149 · 67** |
| 구조체 21개 아는 칸 · 범위 밖 | 811 · 245 | **816 · 240** |
| 아는 칸 중 끝까지 0 | 322 | **327** |

- 오케스트레이터 기본값으로 run52 기준을 따랐다 ("결정 → run52 기준으로 옮김").
- 왜 run52 기준인가: run52 는 만들기 길 안에서 명시적으로 쓰는 줄 (0 저장 · 입력값 쓰기) 이 있으면 "0 을 씀" 으로 셌다. 이번 다섯 칸도 같은 꼴이다.

### 2.7 `K3~P0` — 풀어 주면: 줄에 서고, 처음 계산대에 선다

여기서 중요한 건 **누가 이 칸들을 쓰나** 다.

- 앞 절반 (줄에 세우기) 은 **부모 cmd 스레드** 가 쓴다. `NtResumeThread` 는 부모가 낸 다음 시스템 콜이다 (flow.md 2.2).
- 뒤 절반 (처음 계산대) 은 새 직원이 교대해 들어오는 순간이다.

#### 길 `[정적]`

```
NtResumeThread ─ +0x9b PsMultiResumeThread ─ +0x25 KeResumeThread
   ├─ +0x87  KiResumeThread          (멈춤 신호등 풀기, run13)
   └─ +0xa5  KiExitDispatcher ─ +0x46 KiProcessThreadWaitList
                                  └─ +0x106 KiDeferredReadySingleThread   ← 줄 관리인
```

- 짐 지도에선 이 사슬을 **`①R…`** 로 줄여 쓴다 (2.15).

#### 줄 관리인이 적는 칸 `[정적]`

| 줄 | 칸 | 값 | 사진 |
|---|---|---|---|
| `+0x3b5` | `NextProcessor` | 0 (첫 값) | (2.13) |
| `+0x11f8` | `WaitTime` | KUSD `TickCount` 아래 32비트 | P0 `0x14dff` |
| `+0x122e` | `State` | 1 (줄에 섬) | |
| `+0x151d` | `_ETHREAD` `LastExpectedRunTime` | | |
| `+0x152b` → `KiUpdateSoftParkElectionStatisticsOnInsertion` (`+0x1f` · `+0x41` · `+0x5b ~ +0x7e`) | `_ETHREAD` `LastSoftParkElection*` 셋 | | |
| `+0x171f ~` | `WaitListEntry` (= `SwapListEntry`, 같은 자리) | 준비 줄 고리 | |
| `+0x2a9` 비교 → `+0x1877 KiComputeThreadAffinity +0x212` | `AffinityVersion` | 전역 `KiCpuSetSequence` | P0 `0x18` |

- **`WaitTime`** — run77 의 `[추측]` 이 닫혔다.
  1. `[관찰]` P0 값 `0x14dff`. 부팅 22분 16초쯤의 틱 수다.
  2. `[추측]` (run77 메모) P0 는 기다림 **전** 이라, 기다림 함수 (`KeWaitForSingleObject +0x1b9 ~ +0x1c5`) 가 아닌 다른 쓰는 줄이 있을 것.
  3. `[정적]` (이번) 그 줄 = **줄 관리인 `+0x11f8`**. 줄에 세울 때도 "지금 시각" 을 적는다.
  - 비유: 기다림 표에 "언제부터 기다렸나" 를 적는 칸이 줄에 설 때도 같이 쓰인다. 대기실에 들어갈 때도, 줄에 설 때도 시계를 본다.
- **`AffinityVersion`** = 자리표 판 번호 (run 문서 비유).
  - 관리사무소 전체의 "자리표 판" 에는 번호 (`KiCpuSetSequence`) 가 붙어 있다.
  - 줄 관리인은 직원 카드의 번호가 전역 번호와 **다르면** (`+0x2a9 CMP [RBX+0x238],RAX`) 앉아도 되는 자리를 다시 계산하고 (`KiComputeThreadAffinity`) 번호를 맞춰 적는다 `[정적]`.
  - 이 칸은 사진 K3 까지 0 이었다 (그래서 `K3~P0` 칸이다). 그러니 처음 줄에 설 때 전역 번호와 달라서 다시 계산했을 것이다 `[추측]`.
  - 사진 P0 `0x18` `[관찰]` 은 그때 맞춰 적은 전역 번호로 보인다 `[추측]`. 전역 값 자체는 Q79 에서 덤프로 본다 (기대 `0x18`).
- **`LastSoftParkElection*`** 셋은 이름이 가리키는 통계 칸이다. 칸의 뜻은 이번 근거에 없다. 쓰는 줄만 닫혔다.

#### 처음 계산대에 설 때 `[정적]`

- `KiSwapThread +0x5a` `CurrentRunTime` = 0 · `+0x190` `State` = 2 (돌고 있음).
- → `KiSwapContext` → `SwapContext +0x18` `Running` = 1 · `+0x5bb` `ContextSwitches` +1.
- 사진 P0: `State` 2 · `ContextSwitches` 1 `[관찰]`. **처음 교대 한 번** 이 숫자로 맞는다.
- `CycleTime` (쌓인 모래, run76) 도 이 구간 칸이다. 줄은 짐 지도 3.14.
- 중간 단계 (`State` 0 → 1 또는 7 → 2) 의 `KiResumeThread` · `KiTryUnwaitThread` 쪽 갈래는 아직 안 봤다 → Q201 ②.

### 2.8 `P0 ~ P2` — 일하는 동안 생기는 칸

여기 칸들은 대부분 **카운터** 이거나 **계속 다시 쓰는 칸** 이다.

- 짐 지도엔 "처음 값 한 줄 + 바꾸는 쪽 이름" 으로 적었다 (14칸: `CycleTime` · `CurrentRunTime` · `ContextSwitches` · `WaitTime` · 파일 카운터 5 · `KernelWaitTime` · `UserWaitTime` · `ReadyTime` · `QuantumTarget` 뒤 값 · `LastExpectedRunTime`).

#### `P0~P1` — 기다림 한 번, 파일 일 한 번 `[정적]` (쓰는 줄)

- 기다림: `KeWaitForSingleObject +0x116 · +0x1d8 · +0x1dd · +0x1e2 · +0x7d2` → `KiCommitThreadWait +0x5b · +0xbf`.
  - [run77 해설 2.6](run77-thread-wake-해설.md) 의 "번호표 걸기 → 커밋 → 잔다" 그 길이다.
- 깸: `KiTryUnwaitThread +0x1bb` `KernelWaitTime` (커널 모드로 기다린 시간).
- 파일 카운터: `ReadOperationCount` · `ReadTransferCount` · `OtherOperationCount` — `IopReadFile` 꼬리 · `IopUpdateIrpTransferCount` · `NtQueryInformationFile` 등.
- **누가 이 길을 탔는지** 는 정적으로 못 정했다 → 2.10.

#### `P1~P1a` — 첫 시스템 콜들

- 신청 창구 입구가 업무 카드에 적는 셋 `[정적]`:
  - `KiSystemServiceUser +0xd2` `SystemCallNumber` (단축 번호)
  - `KiSystemServiceUser +0xcb` `FirstArgument` (`KiSystemCall64 +0x83` 이 저장한 `R10`)
  - `KiSystemServiceStart +0x0` `TrapFrame` (출발 메모가 어디 있나 = `RSP`)
  - 사진 P1a `[관찰]`: `0xd` · `0xfffffffffffffffe` · `…7460` (= `InitialStack − 0x190`).
- **스케줄러 공유 표 자리** `[정적]`:
  - ntdll `LdrpAcquireSchedulerSharedDataSlot +0x84` 가 `NtSetInformationThread` 클래스 `0x39` 를 낸다 (sast/36). [run110 해설](run110-tls-lock-small-해설.md) 의 **카드 받기** 다.
  - 그 커널 쪽: `NtSetInformationThread +0xc2c` → `PspSchedulerSharedDataRegionSlotAllocate`
    - `+0x1a4` `SchedulerSharedSwappablePage` · `+0x1ca` `SchedulerSharedSystemSlot` · `+0x1d1` `_ETHREAD.SchedulerSharedDataObject`.
  - 즉 직원이 상자에서 카드 한 장을 받을 때, 관리사무소도 **업무 카드 · 인사 카드에 "이 직원 카드는 상자 몇 번 자리" 를 적어 둔다.**
  - `[관찰]` `KTHREAD +0x468` 과 `ETHREAD +0x6e0` 이 사진에서 같은 주소 (`…995fbe10`). 상자 (영역 객체) 와 그 안 첫 쪽 (`_KSWAPPABLE_PAGE`) 이 같은 주소여서 그렇게 보인다 `[추측]`.
- `WaitReason` 의 0 이 아닌 첫 값 `0x1f` 는 `KiDispatchInterrupt +0x13e` (초인종 선점, [run77 해설 2.16](run77-thread-wake-해설.md#216-사진과-맞춰-보기--메인-스레드-기다림-칸)) `[정적]`.

#### `P1a~P1b` · `P1b~P2`

- `AutoBoostEntriesExhausted` (`KeAbPreAcquire +0x52`) · `ReadyTime` · `WaitMode` · `AdjustIncrement` · `DecayBoost` · `UserWaitTime` · `Alertable` · `WaitStatus` · `NextProcessor` 1 · 쓰기 카운터 `[정적]`.
- 칸마다의 줄 · 값 · 사진은 짐 지도 3.13 · 3.14 의 새 줄 (근거 칸 `sast108`).

### 2.9 범위 밖 길 2칸 — 값은 생겼는데, 쓴 줄을 못 정했다

둘 다 사진에 **초기화 동안 값이 생긴다** `[관찰]`. 그런데 이 줄기 안에서 누가 썼는지 못 정했다. 그래서 "범위 밖 길 `[추측]`" 으로 따로 두 줄 (짐 지도 7.4).

#### `SavedApcStateFill` (`+0x258`, 43바이트)

- **`SavedApcState`** = 직원이 **남의 땅에 잠깐 들어갈 때** ([run10 해설](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)) 자기 쪽지 묶음 (`ApcState`) 을 맡겨 두는 보관함으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- `[관찰]` 사진 `P0~P1` 에 목록 머리 둘이 **자기 연결**, `Process` 0.
- `[추측]` (run 문서) `KiStackAttachProcess` · `KiAttachProcess` 가 `ApcState` 를 옮겼다 되돌린 흔적으로 보인다.
- 이 구간에 누가 "남의 땅" 에 들어갔는지 못 정했다 → 2.10 과 같은 질문 (Q200 ①).

#### `ResourceIndex` (`+0x460`)

- 커널 안 자물쇠 한 종류 (`ERESOURCE`, run 문서 "실행 리소스") 의 **주인 표 안에서 이 직원 자리 번호** (`ExpFindCurrentThread +0xf7` 등) `[정적]`.
- 이웃 칸 `+0x464 SchedulerSharedOffset` 과 **이름만 비슷하고 다른 일** 이다. 헷갈리기 쉽다.
- 처음 1 이 되는 자물쇠가 무엇인지 못 정했다 → Q200 ② (낮음), 정적 짝 Q201 ④.

### 2.10 핵심 — `P0~P1` 에 지도에 없는 커널 일이 있다

이번 바퀴에서 제일 중요한 "아직" 이다. 단계별로.

1. `[관찰]` run44 사진, P0 → P1 사이 새 직원 업무 카드:
   - `ReadOperationCount` 0 → 1 · `ReadTransferCount` 0 → 1862 · `OtherOperationCount` 0 → 2
   - `KernelWaitTime` 0 → 1 · `WaitBlockCount` 0 → 1 (`WaitReason` 0)
   - `SavedApcStateFill` 목록 머리 둘이 자기 연결
   - → 이 구간에 **커널이 이 직원 몫으로 파일을 한 번 읽고 (1862바이트), 한 번 기다렸다.**
2. 짐 지도 5.0 은 이 구간을 "`PspUserThreadStartup` → `PspInitializeThunkContext`" 로만 적어 두었다.
3. `[정적]` 그 길의 `PspSetContextThreadInternal` 은 대상이 **지금 스레드** 면 APC 없이 그 자리에서 처리한다 (`+0xf7 CMP R13,R14` → `+0x131`). → **기다림이 안 생긴다.**
4. 그래서 → 지도에 적힌 길 말고 **다른 무언가** 가 기다리고 파일을 읽었다.
5. `[추측]` 후보: `PspNotifyThreadCreation +0xda` 의 `PfProcessCreateNotification` (프리페처, [run51 해설](run51-eprocess-later-해설.md#새-말-프리페처)).
   - 결과서 훑기: 프리페처 쪽 `PfSnPopulateReadList +0x85` · `PfSnAppLaunchScenarioControl +0xb9` 가 `KiStackAttachProcess` 를 부른다 `[정적]`.
   - 그렇다면 "남의 땅에 들어가서 (→ `SavedApcStateFill`) 파일을 읽었다 (→ 카운터)" 가 한 사건일 수 있다 `[추측]`.
6. **아직 확인 아님.** 정적으로는 이 구간에 어느 길을 탔는지 못 정한다.
   - 동적 **Q200 ①** (급함): P0 에서 새 스레드 주소를 얻고 `ba w8 <스레드>+0x380` · `ba w8 <스레드>+0x258` → `k`.
   - 정적 짝 **Q201 ①**: `PfProcessCreateNotification` 본문 (rva `0x944508`).

- 왜 급한가 `[추측: 이 해설의 읽기]`: 지도에 없는 커널 일이 초기화 줄기 **한가운데** 끼어 있다는 뜻이라, 지도와 짐 지도 둘 다 빠진 단계가 있을 수 있다.

### 2.11 선호 자리 · 앉아도 되는 자리 — 최종 출처는 `KeStartThread`

[run54 해설 2.9](run54-teb-fill-해설.md#29-p0p1-5칸-첫-출근-날-적는-선호-자리) 끝에 "같은 함수가 나중에 또 불릴 수 있다 (Q78d4)" 라고 남겼던 질문의 답이다.

#### 답

- 최종 출처 = **`KeStartThread`** (`+0x17c` `UserIdealProcessor` · `+0x147` `KiCopyAffinityEx` → `UserAffinity`) `[정적]`.
- 첫 스레드 초기화 줄기에 이 값을 **다시 바꾸는 호출이 없다** `[정적]`.
- 사진 `UserIdealProcessor` K3 1 → P2 1 `[관찰]`.

#### 다시 쓰는 길 (`KiQueueTebUpdateApc`) 은 왜 안 타나 — 단계별로

1. `[정적]` `KiQueueTebUpdateApc` 를 부르는 곳은 `KiSetIdealProcessorThread +0x10e` · `KiSetAffinityThread +0xf1` **둘뿐** 이다.
2. `[정적]` 그 둘 위로 유저에서 들어오는 길은 `NtSetInformationThread` 의 친화도 · 이상적 프로세서 갈래 (`+0x12ac` · `+0xb29`) 뿐이다.
3. `[정적]` 초기화 줄기 ntdll `.c` 에서 본 `NtSetInformationThread` 클래스는 `5` · `0x38` · `0x39` 뿐이다.
4. 그래서 → 첫 스레드 초기화에선 이 쪽지를 큐에 넣을 길이 없다 `[정적: 호출 길 없음]`.
5. 한계 (run 문서): `.c` 가 있는 11개 파일만 봤고, `NtSetInformationProcess` 의 친화도 갈래는 안 봤다.
   - 실행으로 확인: **Q200 ③** (낮음) `bp nt!KiQueueTebUpdateApc` 가 `P0 ~ P2` 사이 안 걸리나.

- 그래서 TEB `P0~P1` 다섯 칸은 `PspUserThreadStartup +0x94 → KeUpdateTebSchedulingPropertiesCurrentThread` 가 `KeStartThread` 값을 **읽어서 한 번 쓴 그대로** 다 (run54).
- 비유: 근무 조건 담당이 업무 카드에 적은 선호 자리를, 첫 출근 날 개인 안내판에 한 번 옮겨 적는다. 그 뒤 "자리 바꿔 주세요" 신청은 초기화 동안 안 들어온다.

### 2.12 새 가름 — 트랩 프레임 덮임 25

#### 새 말: 트랩 프레임 덮임

- **트랩 프레임 덮임** = 커널에 들어올 때마다 **그 순간 레지스터로 덮어쓰는 칸**. 짐처럼 한 번 놓고 두는 게 아니다.
  - 비유: 출발 메모는 사실 **창구를 지날 때마다 그 창구가 다시 적는 메모지** 다. 신청 창구 (시스템 콜) · 사고 접수 창구 (`int3` · 페이지 오류) 가 각자 자기가 맡은 칸만 고쳐 적는다.
  - 이번 창구가 안 적는 칸에는 **앞 창구가 적은 글씨가 그대로 남는다.**
- 그래서 이 25칸은 아는 칸 · 아무도 안 씀 · 범위 밖 · 모름 **어디에도 안 넣고** 따로 센다 (짐 지도 2장 "덮임").
- 짐으로 쓸 트랩 프레임 값은 그대로다: `K1~K2` 의 처음 문맥 9칸 + `P0~P1` 의 바꿔 끼우기 (`Rip` · `Rcx` · `Rdx` · `Rsp`) (짐 지도 2.1 아래 줄).

#### 어느 창구가 어느 칸을 적나 (run 문서 표에서 대표만) `[정적]`

| 칸 | 적는 창구 | 사진 (처음 값) |
|---|---|---|
| `Rax` · `R8` ~ `R11` | **모든 창구** (`KiSystemCall64 +0x7f` · `KiSystemServiceUser +0x2c ~ +0x38` · `KiBreakpointTrap +0x18 · +0x24 ~ +0x30` · `KiPageFault +0x14 · +0x20 ~ +0x2c`) | P1a 부터 유저 값 |
| `Rbx` · `Rdi` · `Rsi` | 신청 창구 `KiSystemCall64 +0x55 · +0x5c · +0x63` | P1a |
| `PreviousMode` | 신청 창구 `KiSystemServiceUser +0x4` (= `KiSystemCall64 +0x2a6`, 1) | P1a 1 |
| `FaultAddress` | 페이지 오류 창구 `KiPageFault +0x358` (`CR2`) | P1a (= 매개변수 블록 주소) |
| `FaultIndicator` | `KiPageFault +0x383` (읽기 0 · 쓰기 1 · 실행 8) | P1a 1 → P1b 8 |
| `GsBase` · `GsSwap` | `KiPageFault +0x2de · +0x2e1` | P1a (= TEB 주소) |
| `ErrorCode` | CPU 가 #PF · #GP 때 밀어 넣음 | P1a 7 → P1b `0x14` |
| `P1Home` ~ `P4Home` | 입구 코드는 안 씀. `KiSystemServiceStart +0x90c ~ +0x917` (win32k 첫 호출 앞뒤 임시 보관) + 아래 함수의 인자 자리 `[추측]` | P1a (`P4Home` 은 P1b) |

- 나머지 칸 (`Xmm1` · `ContextRecord` · `NmiMsrIbrs` · `NmiPreviousSpecCtrl*` 등) 은 run 문서 3절 표. 그중 이름으로 쓰는 줄을 못 찾은 칸, `dt` 로 못 읽은 칸 (`Xmm1`) 은 `[공백]` 이다.
- 같은 바이트에 이름이 둘 붙은 짝이 많다 (`PreviousMode` · `InterruptRetpolineState`, `FaultAddress` · `ContextRecord`, `ErrorCode` · `ExceptionFrame` …). [run52 해설](run52-kthread-create-해설.md#25-k1k2-42칸-네-사람--도우미-셋) 의 겹치는 칸과 같은 꼴이다.

#### 첫 덮임은 `P1` 의 `int3` — 단계별로

1. `[관찰]` 사진 P1 의 출발 메모: `Rip` = `LdrInitializeThunk +1`. → P1 에서 멈춘 `int3` 창구 (`KiBreakpointTrap`) 가 이미 덮은 메모다.
2. `[관찰]` 이 24칸은 P1 에서도 0 이다.
3. `[정적]` + `[관찰]` 24칸의 첫 값은 `P1~P1a` 의 **신청 창구 · 페이지 오류 창구** 가 적었다.
4. `[관찰]` P1a · P1b · P2 도 `int3` 로 멈춘 사진이다. 그래서 그 사진의 `P1Home` · `PreviousMode` · `GsBase` · `FaultAddress` · `Rbx` · `ErrorCode` 등은 **`int3` 창구가 안 적는 칸에 남은 앞 창구 글씨** 다.

- 요청서는 "첫 덮임 = 첫 syscall (사진 `P1` 직전)" 이라고 봤다. **사진은 다르다** — 첫 덮임은 P1 의 `int3` 다 (정정, 2.15).
- 헷갈릴 수 있는 곳: 위 표에서 `Rax` · `R8 ~ R11` 은 `int3` 창구도 적는 칸인데, P1 에서 0 이다.
  - run53 에서 만들 때 이 칸들의 처음 값이 0 이었다 (`KxContextToKframes +0xdd · +0xfe ~ +0x11f`). 그래서 P1 순간의 유저 레지스터 값도 0 이었고, `int3` 창구가 그 0 을 적은 것으로 읽힌다 `[추측: 이 해설의 읽기 — run 문서에 이 연결은 없다]`.

#### 디버거가 붙어서 달라지는 점

- P1 이 `int3` 라서 `KiBreakpointTrap` 이 첫 메모를 적었다.
- 디버거 없이 돌리면 첫 syscall (`KiSystemCall64`) 이 첫 덮임일 수 있다 `[추측]`.
- 사진 쪽 함정 (run 문서 "디버거 메모"): **`int3` 로 멈춘 사진의 트랩 프레임 칸을 "이 순간의 짐" 으로 읽지 말 것.**

#### 신청 창구 입구가 둘일 수 있다

- 시스템 콜 명령이 뛰어가는 주소 (LSTAR) 가 `KiSystemCall64` 인지 그림자 변형 `KiSystemCall64Shadow` 인지는 정적으로 못 정한다 (`KiKvaShadow` 전역 · MSR 값).
  - `KiKvaShadow` → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) 의 "유저로 나가는 문이 둘" 과 같은 전역이다.
  - LSTAR = 시스템 콜 명령이 들어갈 커널 주소를 담은 CPU 레지스터로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 둘 다 Ghidra 가 함수로 안 잡아서 `objdump` 로만 봤다 (run56 과 같은 사정).
- → Q79 (전역은 덤프, LSTAR 는 라이브만).

### 2.13 사진 구간과 코드 첫 쓰기가 어긋나 보이는 곳

짐 지도 2.1 표의 "구간" 은 사진에서 **0 이 아닌 첫 값이 보인 구간** 이다. 그래서 값이 처음에 0 으로 쓰이는 칸은 어긋나 보인다.

| 칸 | 코드 첫 쓰기 | 사진 구간 | 까닭 |
|---|---|---|---|
| `NextProcessor` | `K3~P0` (줄 관리인 `+0x3b5`, 값 0) | `P1b~P2` | 첫 값이 0 이라 사진엔 0 |
| `WaitReason` · `WaitMode` · `WaitStatus` · `MiscFlags.Alertable` | `P0~P1` 의 첫 기다림 (값 0) | `P1~P1a` ~ `P1b~P2` | 사진은 0 이 아닌 첫 값 |
| `WaitTime` | `K3~P0` (줄 관리인) | `K3~P0` | 맞음 (2.7) |

- 둘 다 틀린 게 아니다. 짐 지도는 2.1 표 (사진 구간) 를 **안 고치고**, 3장 근거 칸에 "코드 첫 쓰기" 를 같이 적었다.
- 나머지 칸은 사진 구간과 코드 순서가 맞다.

### 2.14 간접 호출 — 칸을 직접 쓰는 건 0개

- 읽은 `.asm` 32개 안 `CALL reg` · `JMP reg` 는 5개. 모두 **"고르는"** 코드다 `[정적]`.
  - `KiSystemServiceStart +0xfa` `JMP R11` — 인자 복사 점프표.
  - `KiSystemServiceStart +0x193` · `+0xa24` · `+0xa7f` `CALL RAX` — **서비스 본체** (`KeServiceDescriptorTable` · `…Shadow` · `…Filter` 에서 단축 번호로 고름).
  - `KiDeferredReadySingleThread +0x17f7` `JMP RCX` — 함수 안 `switch`.
- 칸을 쓰는 건 고른 `Nt*` 의 몫이다.
- 첫 스레드가 어떤 `Nt*` 를 불렀는지는 사진에 **마지막 번호** (`0xd` · `0xe` · `0x1d3`) 만 남아 있다 → Q35.

### 2.15 정정과 "결과서와 다르게 적은 곳"

#### 정정 셋 (run 문서 "정정")

1. **run52** — "`ThreadFlags` 비트 13 (`SharedReadyQueueAffinity`) 은 `KeStartThread +0xb9` · `+0x1b7`" 은 **틀렸다**.
   - `+0xb9` = 비트 3 `DisableBoost`, `+0x1b7` = 비트 8 `DisableQuantum` (둘 다 가게 값, DWORD 통째 쓰기).
   - **비트 13 = `KeStartThread +0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55`** (`XOR.LOCK [RDX+0x78],0x2000`) `[정적]`.
   - run52 문서 맨 위에 한 줄, [run52 해설](run52-kthread-create-해설.md) 맨 위에도 "고쳐 읽기 (run112)" 한 줄을 넣었다.
   - 비트 3 · 8 은 run52 의 "0 을 씀 20" 칸이라 가름은 그대로다.
2. **짐 지도 7.1 셈** — run111 이 `_PEB.NtGlobalFlag2` (끝까지 0) 를 아는 칸으로 옮기며 "아는 칸 중 끝까지 0" 을 304 그대로 뒀다. → 305 로 고치고, 이번 17칸 · 셈 결정 5칸을 더해 **327**.
3. **요청서 전제** "트랩 프레임 첫 덮임 = 첫 syscall" → 사진과 다르다. 첫 덮임은 P1 의 `int3` (2.12). 요청서는 중간 산출물이라 안 고친다.

#### 결과서와 다르게 적은 곳 · 보탠 것

- **경로 줄임을 바꿨다.** 결과서의 `①P` (`PspInsertThread`) · `①S` (`KeStartThread`) 는 짐 지도에 **이미 다른 뜻** 으로 있다 (`①P…` = `PspAllocateProcess` run49, `①S…` = exe 섹션 사슬 run47).

  | 결과서 | 짐 지도 | 뜻 |
  |---|---|---|
  | `①P` | **`①IT…`** | `①… → PspInsertThread (호출 +0x1015)` |
  | `①S` | **`①ST…`** | `①IT… → KeStartThread (호출 +0x5dd)` |
  | `R` | **`①R…`** | 부모 `NtResumeThread → … → KiDeferredReadySingleThread` |
  | `SC` | **`②SC…`** | 새 스레드 `KiSystemCall64 → KiSystemServiceUser → KiSystemServiceStart` (이어지는 길) |

  - 결과서를 읽다 짐 지도로 넘어갈 때 이 표를 옆에 두면 덜 헷갈린다.
- `KeStartThread` 5칸 셈 → 2.6.
- 결과서 `SwapContext +0x5bb` 는 `.asm` 에 조각 줄 `frag:FUN_1406b686f+0x28c` 로 있다 (`FUN_1406b686f.asm` 파일은 따로 없음).
- `KiCopyAffinityEx` 는 `.asm` 표시가 `alias:KiCopyAffinityEx+1` 이지만, PDB 에서 그 주소 이름이 하나뿐이라 이름 그대로 썼다.
- `KeStartThread +0xb9` · `+0x1b7` 은 비트 OR 가 아니라 `ThreadFlags` **DWORD 통째 쓰기** 다 (flow-writer `.asm`).

### 2.16 PE_emulator 에게 (run 문서 "PE_emulator 쪽", 짧게)

`../PE_emulator` 코드는 결과서만 봤다. 고칠지는 사용자가 정한다.

- `ThreadListEntry` = **맞음** (`Src/nt/Process.cpp` 줄 1687).
- `KeStartThread` · `KeSuspendThread` 는 "안 함" → `Priority` · `QuantumTarget` · `UserIdealProcessor` … 아홉 칸 · `CrossThreadFlags` 우선순위 비트 · `KernelStackReference` 는 **아직 없음 (빈 칸, 틀린 건 아님)**.
  - `Process.cpp` 줄 2046 ~ 2056 이 바로 이 결과 (Q78d4) 를 기다리고 있었다. `UserIdealProcessor` 1 · `UserAffinity` = 가게 `Affinity` 사본이 그 값이다.
- syscall 입구 모델 (`Src/nt/Syscall.cpp` 줄 925 ~ 949) = **맞음 (범위 안)**. 다만 `KTHREAD +0x80 / +0x88 / +0x90` 칸은 없다. 읽는 쪽이 생기면 `KiSystemServiceUser +0xcb · +0xd2`, `KiSystemServiceStart +0x0` 을 따라야 한다 (확인 필요).
- 스케줄러: `Scheduler.h` 의 `QuantumTarget 0x7f2814` 는 맞음. `Scheduler.cpp` 는 결과서가 안 읽어서 **확인 필요**.

### 2.17 정리 — 비유로 한 번 더

- **인사과** (`PspInsertThread`) 가 새 직원을 가게 직원 명부에 올리고, 사원증에 가게 기본 우선순위를 찍는다. 사원증 묶음을 **통째로** 다시 찍어서, 나머지 빈칸에도 펜이 닿았다 (17칸 "0 을 씀").
- **근무 조건 담당** (`KeStartThread`) 이 등급 8 · 모래시계 눈금 · 선호 자리 1 · 앉아도 되는 자리 목록을 정하고, 업무 명부에도 올린다. 이 선호 자리가 초기화 끝까지 그대로 간다.
- 인사과가 "일단 멈춰 둬" (`KeSuspendThread`) 하고, 부모가 다음 신청으로 풀어 주면 **줄 관리인** 이 직원을 준비 줄에 세우며 시각 · 자리표 판 번호를 적는다.
- 처음 계산대에 서면 "근무 중" · 교대 1번이 생긴다.
- 일하는 동안 창구를 지날 때마다 **출발 메모** 가 다시 적힌다. 이 메모는 짐이 아니다.
- 딱 하나, 첫 출근 직후 **누군가 이 직원 몫으로 파일을 읽고 기다렸다.** 누군지 아직 모른다 (프리페처일 것 `[추측]`).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 절 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 | — | ntoskrnl pdb 줄에 run112, `KiSystemCall64Shadow` 도 Ghidra 함수가 아니라 `objdump` 로만 봄 |
| 2.1 run44 `[관찰]` 줄 | `Priority` · `BasePriority` 8, `QuantumReset` 2 는 "`KeStartThread` 몫일 것 `[추측]`" | → `[정적]` **`KeStartThread +0xc8` · `+0xc2` · `+0x1c1`** |
| 2.1 `PspInsertThread` | — | 새 `[정적]` 줄: `_ETHREAD` 다섯 칸 (`+0x2cd` · `+0x617 ~ +0x649` · `+0x6e3`) + `[관찰]` K3 |
| 2.2 `KeStartThread` | "`[추측]` 본문은 안 뽑았다 … 스케줄링 준비로 보인다" | 그 `[추측]` 줄에 취소선 → 새 `[정적]` 줄: 업무 카드 아홉 칸 · 비트 13 정정 · 다섯 칸 · `[관찰]` K3 |
| 2.2 `KiResumeThread` 쪽 | — | 새 `[정적]` 줄: 처음 준비 줄에 세우는 길 (`KiExitDispatcher → KiProcessThreadWaitList → KiDeferredReadySingleThread`) + 처음 계산대 |
| 2.2 시스템 콜 입구 | — | 새 `[정적]` 줄: 입구가 쓰는 셋 + **창구마다 트랩 프레임 칸이 다르다** + 간접 호출 |
| 2.3 `PspUserThreadStartup` | run54 "다시 쓰는 길" 이 열려 있음 | `[정적: 호출 길 없음]` 안 탐 + 새 `[관찰]` 줄 "P0 → P1 에 한 번 기다리고 파일을 한 번 읽었다" + 후보 `[추측]` |
| 4장 | Q78d4 관련 넘김 줄 넷 | `(해결됨)` + 새 줄 넷 (동적 · 정적 · PE_emulator · 셈 결정) |
| 5장 | — | run112 두 표 |

- 한 줄로: **2.1 ~ 2.3 의 `KeStartThread` · `PspInsertThread` 쪽 `[추측]` 이 `[정적]` 이 됐고, 2.3 에 `[관찰]` 수수께끼가 하나 새로 붙었다.**

### 짐 지도 `baggage.md` 2장 — 숫자가 크게 바뀌었다

#### 구조체 21개, 직접 칸 1,456

| 가름 | 바퀴 전 | 바퀴 뒤 | 무엇 때문에 |
|---|---|---|---|
| 아는 칸 | 739 (정확 708 · 짐작 31) | **816** (정확 785 · 짐작 31) | 이번 82칸 중 55 · `CrossThreadFlags` 17 · `KeStartThread` 5 |
| 아무도 안 씀 | 160 | **157** | `CrossThreadFlags` 3칸이 0 을 씀으로 |
| 범위 밖 길 `[추측]` | 257 | **240** | `CrossThreadFlags` 14칸 · `KeStartThread` 5칸이 빠지고, `SavedApcStateFill` · `ResourceIndex` 2칸이 들어옴 |
| **트랩 프레임 덮임** (새) | — | **25** | `_KTRAP_FRAME` `P1~P1a` 24 · `P1a~P1b` 1 |
| 모름 | 300 | **218** | 이번 82칸 |

- 합은 그대로 1,456 이다.
- 커널 14 구조체만 보면 모름 **106 → 24** (`SharedCommitCharge` 1 + VAD 23).
- "구간이 정해진 모르는 칸" (K1 이전이 아닌 곳) 은 **92 → 10** (유저 7 · 커널 0 · 전역 3).
- 아는 칸 중 끝까지 0 (7.1): 304 → **327** (run111 몫 1 정정 + `CrossThreadFlags` 17 + `KeStartThread` 5).

#### 스레드 구조체 셋

| 구조체 | 칸 | 아는 칸 | 아무도 안 씀 | 범위 밖 | 덮임 | 모름 |
|---|---|---|---|---|---|---|
| `_ETHREAD` | 149 | 48 → **75** | 22 → **19** | 69 → **55** | — | 10 → **0** |
| `_KTHREAD` | 237 | 99 → **149** | 21 | 70 → **67** | — | 47 → **0** |
| `_KTRAP_FRAME` | 66 | 23 | 12 | 6 | **25** | 25 → **0** |

- `_ETHREAD` +27 = 이번 10 + `CrossThreadFlags` 17.
- `_KTHREAD` +50 = 이번 45 + `KeStartThread` 5.
- 3장: 3.13 31 → **42줄**, 3.14 50 → **80줄**, 3.15 19 → **24줄**.

### 큰 그림에서

- **Q78d (첫 스레드) 가 다 끝났다.** run52 · 53 · 54 · 112 네 조각으로, 첫 스레드 커널 구조체 셋 + TEB 의 모르는 칸 564 를 모두 닫았다.
- 에뮬레이터 쪽에서 보면: 첫 스레드를 만들 때 `KeStartThread` 몫 아홉 칸이 이제 **무엇을 어디서** 가져오는지 줄 단위로 있다 (2.4). `Process.cpp` 가 기다리던 값이다.
- 남은 큰 구멍은 **`P0~P1` 의 커널 파일 읽기 · 기다림** 하나다 (2.10). 지도에 빠진 단계일 수 있어서 동적 Q200 ① 이 급함으로 붙었다.
- 다음 문서 차례는 **Q78e1** (sast/109, ntdll 유저 구조체 쪽) 이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `P0~P1` 에 누가 파일을 읽고 (`ReadOperationCount` 1 · `ReadTransferCount` 1862 · `OtherOperationCount` 2) 누가 남의 땅에 들어갔나 (`SavedApcStateFill`) | 이 구간에 어느 길을 탔는지 정적으로 못 정함. 사진은 결과만 보여 줌 | 동적 **Q200 ①** (급함): `ba w8 <스레드>+0x380` · `ba w8 <스레드>+0x258` → `k` (기대 `PfProcessCreateNotification` `[추측]`) · 정적 짝 **Q201 ①** (본문 rva `0x944508`) |
| `ResourceIndex` 가 처음 1 이 되는 자물쇠 | 쓰는 줄 후보는 있으나 이 줄기에서 누가 불렀는지 못 정함 | 동적 **Q200 ②** (낮음) `ba w1 <스레드>+0x460` · 정적 **Q201 ④** (낮음) `ExpFindCurrentThread` · `ExpFindEmptyEntry` |
| `KiQueueTebUpdateApc` 가 정말 한 번도 안 불리나 | 정적은 "호출 길 없음" 까지. 본 파일이 11개뿐, `NtSetInformationProcess` 친화도 갈래 안 봄 | 동적 **Q200 ③** (낮음) `bp nt!KiQueueTebUpdateApc` |
| 커널 전역 값 (`KiCpuSetSequence` 기대 `0x18` · `KiCyclesPerClockQuantum` 기대 `0x3f940a` · `KiKvaShadow` · `KiDynamicHeteroCpuPolicyExpectedCycles` · `KeSoftParkedQueueThreshold`), LSTAR 가 어느 입구인가 | 전역은 실행 때 값. LSTAR 는 MSR 이라 라이브만 | 동적 **Q79** (덤프 읽기에 더함. LSTAR `rdmsr c0000082` 는 라이브) |
| `SystemCallNumber` `0xd` · `0xe` · `0x1d3` 은 어느 `Nt*` 인가, `P1~P1a` 첫 스레드 syscall 목록 | 사진엔 마지막 번호만 남음 | 동적 **Q35** (`ba w8 <스레드>+0x80` 또는 `KiSystemServiceStart` 조건부 멈춤점, `KiServiceTable` 로 풀기) |
| `K3~P0` 의 `State` 0 → (1 / 7) → 2 중간 줄 | `KiResumeThread` · `KiTryUnwaitThread` 의 초기화 상태 갈래를 이번에 안 봄 | 정적 **Q201 ②** |
| 스케줄러 공유 표 마무리 (한 칸 크기 · 스레드마다 칸 나누기 · `Retrieve`) | 이번엔 칸 셋 쓰는 줄만 | 정적 **Q201 ③** (run110 의 Q198 ① 과 겹침 — 차례 때 묶기) |
| `UserIdealProcessor` 1 의 계산 본문 (`KeSelectIdealProcessor`) | 이름만 봄 | 정적 **Q201 ⑤** |
| 트랩 프레임 `NmiMsrIbrs` · `ContextRecord` · `NmiPreviousSpecCtrl*` 를 그 이름으로 쓰는 줄, `Xmm1` 값 | 같은 바이트 다른 이름이라 이름으로 쓰는 줄이 안 보임, `dt` 가 `_M128A` 로만 보여 줌 | `[공백]` 그대로 (짐이 아닌 덮임 칸이라 대기열 번호 없음) |

- run 문서의 "⑥ 동적 D1 · D2 를 한 dast 로 묶기" 는 Q201 에 따로 번호가 없다. 묶을지는 차례 때 정할 일로 보인다 `[추측]`.

---

## 5. 더 읽기

- run 문서: [`run112-thread-later.md`](run112-thread-later.md) — 요약 표, 호출 지점 표, 트랩 프레임 표 (3절), 사진 구간과 어긋나는 것 (2.7), 정정
- 결과서: [`_workspace/sast/108_thread-later_result.md`](../../_workspace/sast/108_thread-later_result.md) — 칸마다 쓰는 줄 · 값의 출처 · 사진 (1.1 · 1.2 · 2.1 ~ 2.5), `CrossThreadFlags` 19칸 표 (1.3), 동적 D1 ~ D5
- 요청서: [`_workspace/sast/108_thread-later.md`](../../_workspace/sast/108_thread-later.md)
- flow.md: [2.1](flow.md) (`PspInsertThread`) · 2.2 (`KeStartThread` · 준비 줄 길 · 시스템 콜 입구) · 2.3 (`PspUserThreadStartup`) · 4장 Q78d4 줄 · 5장 run112
- 짐 지도: [`baggage.md`](baggage.md) 2장 (숫자) · 2.1 · 3장 줄임 표 (`①IT…` · `①ST…` · `①R…` · `②SC…`) · **3.13 · 3.14 · 3.15** · 7.1 · 7.4 · 7.43
- 앞 조각 해설: [run52](run52-kthread-create-해설.md) (`_KTHREAD` 만들 때) · [run53](run53-ethread-create-해설.md) (`_ETHREAD` · 트랩 프레임 만들 때) · [run54](run54-teb-fill-해설.md) (TEB)
- 스케줄러 해설: [run75](run75-thread-switch-해설.md) (넘기기) · [run76](run76-thread-quantum-해설.md) (퀀텀) · [run77](run77-thread-wake-해설.md) (기다림 · 깨움) · [run78](run78-thread-timer-해설.md) (타이머)

# run80 thread-setcontext — 해설

> 이 글은 [run80 문서](run80-thread-setcontext.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q141e**: PE_emulator 요청 25 의 ㅁ — 기다리는 **남의** 스레드에 문맥 넣기 (커널) (`_workspace/plan.md`, 이번에 **끝**)
  - 요청 25 다섯 묶음 (ㄱ ~ ㅁ) 의 **마지막 바퀴** 다. 4절 끝에서 다섯 묶음을 한 번 돌아본다.
  - 다음 항목: **Q148a** (PE_emulator 요청 26 — CPU 예외, 커널 쪽).
    - 사용자 결정 2026-11-12: 요청 26 ~ 30 (Q148a ~ Q152) 이 먼저, Q142 ~ Q147 은 그 뒤. Q104 VM 단계 앞에서 멈춘다.
  - 이번에 남은 것은 셋으로 갈렸다 (4절):
    - 싼 동적 하나 (Worker 의 칸 두 개) → **Q104**
    - 작은 정적 여섯 → 새 **Q153**
    - 새 샘플이 있어야 하는 동적 → 새 **Q154** (할지 말지는 사용자 결정)
- 근거:
  - run 문서 [`run80-thread-setcontext.md`](run80-thread-setcontext.md) (가장 중요)
  - 요청서 [`_workspace/sast/76_thread-setcontext.md`](../../_workspace/sast/76_thread-setcontext.md) · 결과서 [`76_thread-setcontext_result.md`](../../_workspace/sast/76_thread-setcontext_result.md)
    - 판정 **분석성공 (부분)**. 다섯 질문 모두 줄로 답했다.
    - 못 닫은 셋: 보통 스레드의 `SetContextState` 가 정말 0 인지 · 끝나는 중에 이미 들어간 쪽지 · 스택 거슬러 오르기 끝 조건과 유저 모드 B 의 `Rax`.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 (PDB 목록·사진 메모 한 줄) · 2.2 "시스템 콜 입구·출구" 세 곳 · 2.3 `PspInitializeThunkContext` 8번 한 줄 · 3장 "기다림·깨움"·"스레드 끝내기" 한 줄씩 + **새 절 "기다리는 남의 스레드에 문맥 넣기"** · 4장 고친 줄 둘 + 새 줄 셋 · 5장 한 줄
  - 짐 지도 [`baggage.md`](baggage.md): 근거만 (새 짐 칸 없음, 서른여섯째판)
  - run77 문서 **정정** (맨 위 안내 + 두 곳), run79 문서 두 곳
- 이번 바퀴는 **정적만**이다.
  - test.exe 는 `SetThreadContext` 를 안 부른다 `[소스]` (`sample/test.c`). 그래서 VM 으로도 이 길은 못 본다.
  - 값 대조는 run44 때 찍어 둔 **메인 스레드** 사진 일곱 장에서 칸 두 개만 다시 읽었다 (flow-writer, 새 실행 없음).
- **이번 바퀴에서 조심할 곳 셋**:
  1. run 문서 "정정" 절엔 두 줄이 있다. 그런데 **진짜 정정은 run77 하나** 다. "run16 정정" 은 고칠 게 없었다 — 틀린 사슬은 오케스트레이터 요청서에만 있었다 (2.16).
  2. `State = 7` 을 쓰는 줄은 `KeInsertQueueApc` 몸 안이 **아니다.** 거기서 부르는 도우미 `KiEnterDeferredReadyState` 안이다 (2.7).
  3. `KiDeliverApc +0x76f` 가 `PspGetSetContextSpecialApc` 로 간다는 건 **간접 호출** 이라 줄로 이어진 게 아니다 `[공백 → 동적]` (2.9).

이미 푼 말은 **링크만** 건다.

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) · 인사 카드 (`_ETHREAD`) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- `_KTHREAD` = 인사 카드 첫 장 **업무 카드**, 커널 스택 = **관리사무소 안 작업대** → [run52 해설 "비유"](run52-kthread-create-해설.md#비유-그대로-이어-쓴다)
- CONTEXT = **업무 시작 메모**, 트랩 프레임 = 직원 책상의 **출발 메모**, `PspSetContext` = 출발 메모에 실제로 붙이는 **게시 담당**, 자기 출발 메모 바꿔 끼우기 (`PspInitializeThunkContext`) → [run16 해설 2.3](../02_sprint/run16-thunk-context-해설.md#23-비유-하나로-처음부터-끝까지) · [2.6](../02_sprint/run16-thunk-context-해설.md)
  - 같은 2.6 의 `SpecialApcDisable` = **"작업 중, 쪽지는 잠깐 뒤에"** 표지
- `IRETQ` = 출발 메모대로 **순간 이동** → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- `SYSRET` = **"돌아갈 문" 쪽지** (`RCX`) 대로 나가는 보통 출구, **답 봉투** (`RAX`), 휘발 레지스터 = 지워서 돌려보내는 **메모지** → [run56 해설 2.10](run56-syscall-args-해설.md)
- 비휘발 레지스터 = 직원 **개인 서랍** → [run15 해설 2.4](../02_sprint/run15-user-thread-startup-해설.md#24-새-스레드는-불려서-가-아니라-교대의-ret-로-떨어져서-출발한다), 풀어 보기 (unwind) = 스택을 한 칸씩 거슬러 읽기 → [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) 2.3
- APC = 직원 책상 **쪽지**, 이벤트 = **신호등** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
  - `KernelApcPending` = **"쪽지 있음" 깃발**, `KiDeliverApc` = 쪽지를 펴는 쪽 → [run13 해설](../02_sprint/run13-resume-thread-해설.md) 2.4 · 2.6
- IRQL = **"방해 금지" 단계 표지판** (1 = APC 단계) → [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)
- 번호표 = **"나 여기서 기다려요"**, `KiTryUnwaitThread` = **어깨 두드리기** → [run13 해설 2.8](../02_sprint/run13-resume-thread-해설.md#28-깨우는-방식--신호등-초록--어깨-두드리기-정적)
- `KeWaitForMutexObject` 로 찍혔지만 `KeWaitForSingleObject` → [run13 해설 2.10](../02_sprint/run13-resume-thread-해설.md#210-이름-하나--kewaitformutexobject-로-찍혔지만-kewaitforsingleobject)
- 신호등 명판, 수동/자동 신호등, 커밋 = **"정말 잔다" 도장**, 기다림 진행 단계 (`WaitRegister.State`), `0x100` = **"처음부터 다시 확인" 도장**, 몸에 풀어 넣기, 깨운 직원 명단 (`PRCB+0x2d08`), 퇴장 담당 (`KiExitDispatcher`), 깨움 가산점, 출구 `+0x25d` 첫 쓰기 → [run77 해설](run77-thread-wake-해설.md) 2.3 · 2.6 · 2.7 · 2.8 · 2.10 · 2.13
- 진 번호표를 떼는 `KiProcessThreadWaitList`, 겹칠 때 "먼저 잠근 쪽이 이긴다", 알람 시계, 상대 마감은 시작 때 한 번 → [run78 해설](run78-thread-timer-해설.md) 2.5 · 2.14 · 2.15
- 남을 퇴근시키기 = 쪽지만 붙이고 돌아옴 (`SchedulerApc`·`KiSignalThreadForApc`) → [run79 해설](run79-thread-exit-해설.md) 2.14
- CPU = **계산대**, 준비 줄, `State` 번호 (2 · 5 · 7), 교대 (`KiSwapThread` → `SwapContext`), `WaitStatus` 를 들고 돌아간다 → [run75 해설](run75-thread-switch-해설.md) 2.3 · 2.8 · 2.9 · 2.10
- 간접 호출 → [run9 해설](../02_sprint/run9-create-user-process-해설.md#직접-호출과-간접-호출)
- Pico → [run11 해설 2.4](../02_sprint/run11-allocate-thread-해설.md#24-갈림길-작업대개인-안내판은-언제-만드나)
- 사진 지점 K2 ~ P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

**비유 한 가지 주의.**

- 이 글도 run75 ~ run79 처럼 **CPU = 계산대, 스레드 = 직원** 이다.
  - 기다리는 물건은 run77 해설 그림 그대로다. 이벤트 = **신호등**, 기다림 블록 = **번호표**, 깨우기 = **어깨 두드리기**.
  - APC 는 앞 해설들처럼 **쪽지** 다.
- 이번 이야기의 두 직원:
  - **A** = 문맥을 넣는 직원 (`SetThreadContext` 를 부른 쪽)
  - **B** = 신호등 앞에서 자고 있던 직원 (syscall 안에서 기다리는 중)
- 새로 나오는 그림 넷:
  - **급한 쪽지** = 특별 커널 APC (2.3). flow.md 3장 새 절과 같은 말이다.
  - **꾸러미** = A 가 자기 작업대 위에 펼친 서류 묶음 (2.3). run 문서·결과서가 쓰는 말 그대로다.
  - **작은 신호등** = A 의 꾸러미 안 이벤트. A 는 이 앞에서 잔다.
  - **메모 보관 봉투** = `SetContextState` (2.13).
- flow.md 새 절은 기다리는 물건을 **택배** 로 그렸다 (택배함 = 기다림 줄).
  - 같은 일을 다른 그림으로 본 것이다. 이 글은 run77 해설처럼 신호등·번호표로 이어 쓴다.

### 한 줄로 먼저

- A 는 B 의 출발 메모 (트랩 프레임) 를 **직접 안 고친다** `[정적]`.
  - B 책상에 **급한 쪽지** 하나를 붙이고, B 가 그 쪽지를 펼 때까지 **알람 없이 잔다** (`PspSetContextThreadInternal +0x2bf` → `+0x2dd`).
  - 급한 쪽지 = **특별 커널 APC**. "그다음 본업" 칸 (`NormalRoutine`) 이 비어 있는 커널 쪽지다. 자세히는 2.3.
- 쪽지는 자는 B 를 **그 자리에서 한 번 깨운다** `[정적]`.
  - B 의 결과 칸에 `0x100` ("처음부터 다시 확인" 도장) 을 쓰고, 신호등 줄에서 B 의 번호표를 뗀다.
  - **신호등 불은 안 건드린다.**
- B 는 계산대를 받자마자 쪽지를 편다 (`KiSwapThread +0x2ee`) `[정적]`.
  - 쪽지 안에서 **자기 출발 메모를 그 자리에서** 고친다 (`PspSetContext`). 체크한 묶음만.
  - 그리고 A 의 작은 신호등을 켠다 → A 는 성공 `0` 을 들고 돌아간다.
- 그다음 B 는 기다림을 **처음부터 다시** 한다 `[정적]`.
  - 신호가 와 있으면 **먹는다** (자동 신호등이면 빨강으로).
  - 같은 절대 마감으로 다시 잔다.
- B 가 끝내 유저로 돌아갈 때 `RAX` 는 넣은 문맥의 `Rax` 가 **아니라 마지막 기다림의 결과** 다 `[정적]`.
  - 출구 `KiSystemServiceStart +0x25d` 가 쪽지보다 **나중에** 출발 메모 `Rax` 칸을 덮는다.
  - `RCX` 에는 고친 새 `Rip` 이 들어가 `SYSRET` 으로 새 자리에서 시작한다.
- 그래서 PE_emulator 의 "기다림 이어짐 · 결과 버림 · 신호 안 먹음" 은 **반쯤 맞음 · 맞음 · 틀림** 이다. 그리고 **"A 도 잔다"** 가 빠져 있다 `[정적]`.
- run77 의 짐작 "헤더 비트 `0x8000000` 길 (`KiRestoreSetContextState`) = 남이 문맥을 넣은 경우" 는 **틀렸다** `[정적: 전수 grep]`.
  - 그 길은 **Pico 쪽 스레드만** 탄다. Pico 는 Linux 프로그램 같은 다른 종류 프로세스를 돌릴 때 쓰는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명, run11 해설 2.4).
  - run44 사진의 메인 스레드는 그 비트가 일곱 장 모두 꺼져 있었다 `[관찰]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 → … → NtContinue 은퇴 → 진입점 → main
 │
 ├─ 초기화 중: 메인 스레드가 "자기" 출발 메모를 바꿔 끼움           (run16, PspInitializeThunkContext)
 │              → 대상이 자기라 쪽지 없이 바로 PspSetContext
 │
 └─ main 뒤 (지도 밖)
      ├─ CreateThread → Worker                                      (run74)
      ├─ 계산대 주고받기 · 몫                                        (run75 · run76)
      ├─ WaitForSingleObject(t, INFINITE) · 깨움 · 마감             (run77 · run78)
      └─ Worker 퇴근                                                 (run79)

 test.exe 에는 없는 일   [소스] sample/test.c 에 SetThreadContext 없음
      직원 A 가, 신호등 앞에서 자는 직원 B 의 출발 메모를 바꿈        ← 이번
```

- 흐름도 (`flow.md` 그림) 의 범위는 **진입점까지** 다. 이번 일은 범위 **밖** 이고, test.exe 는 아예 안 한다.
  - 그래서 그림엔 안 넣고, flow.md 3장에 절 하나로 적었다.
- 그럼 왜 봤나?
  - `../PE_emulator` 가 여러 스레드를 돌리면서 "기다리는 남의 스레드에 문맥 넣기" 를 흉내 내고 있었다. 그 흉내가 진짜와 맞는지 물어 왔다.
  - 요청 25 다섯 조각 중 **마지막** 이다:
    - ㄱ 만들기 (run74) · ㄴ 넘기기·퀀텀 (run75 · run76) · ㄷ 기다림·깨움·타이머 (run77 · run78) · ㄹ 끝내기 (run79) · **ㅁ 남의 스레드 문맥 (이번)**.
- 지도와 닿는 곳은 하나다.
  - flow.md 2.3 `PspInitializeThunkContext` 8번 — 초기화 때 메인 스레드가 **자기** 문맥을 바꾸는 길 (run16).
  - 같은 함수 `PspSetContextThreadInternal` 의 **남** 갈래가 이번이다. 실제로 칸을 쓰는 손 (`PspSetContext`) 도 같다 (2.10).

### 무엇이 궁금했나

한 문장으로: **A 가 syscall 안에서 기다리는 B 에게 `NtSetContextThread` 를 하면, B 의 기다림은 어떻게 되고 B 는 유저로 돌아갈 때 무엇을 들고 가나.**

- 계기: PE_emulator 의 지금 흉내 `[추측]` (요청서):
  - B 의 `Rip` 등을 바꿔 두고 **기다림은 이어진다.**
  - B 가 나중에 깨면 **기다림 결과는 버리고** 새 `Rip` 로 간다.
  - B 가 **신호로** 깨도 그 신호를 **안 먹는다** (요청서도 "진짜는 먹을 것 `[추측]`" 이라고 적었다).
- 질문은 이렇게 쪼갰다 (요청서를 줄임):

| # | 질문 | 이 글 어디서 |
|---|---|---|
| 1 · 1' | A 가 쪽지를 거는 줄, A 가 기다리는 줄 | 2.5 · 2.6 |
| 2(a) | 쪽지가 B 의 기다림을 깨나 — 조건 | 2.7 |
| 2(b) | 신호와 쪽지가 겹치면 | 2.8 |
| 3(a) | B 는 쪽지를 어디서 펴나 | 2.9 |
| 3(b) | 출발 메모의 어느 칸을 바꾸나 (`Rax` 포함) | 2.10 |
| 3(c) | B 는 다시 기다리나 | 2.11 |
| 4(a) | 신호로 깨면 신호를 먹나 | 2.11 |
| 4(b)(c) | 유저로 돌아갈 때 `RAX`·`RCX`·`R11` | 2.12 · 2.13 |
| 5 | A 는 무엇을 돌려받나 | 2.14 |

- 이 길은 test.exe 로는 VM 으로도 못 본다 `[소스]`. 그래서 **정적이 유일한 길** 이었다 (요청서).

### 바퀴 전에 알던 것

- `[정적]` run16: `PspSetContextThreadInternal +0xf7 CMP R13,R14` — 대상이 **자기** 면 `+0x131` (ret `+0x136`) `PspGetSetContextSpecialApc` 를 직접 부른다. **남** 이면 쪽지 (APC) 를 걸고 기다린다 (줄은 안 봤다).
  - 자기 길의 사슬: `PspGetSetContextSpecialApc +0x64` → `PspGetSetContextInternal` → `+0x3ea PspSetContext` → `+0x68` 이 출발 메모 `Rip` 칸에 씀.
- `[정적]` run77: 깨난 쪽은 받은 값을 그대로 돌려준다. **`0x100` 일 때만** 처음부터 다시 (`KeWaitForSingleObject +0x860` → `+0x891 JMP +0x104`).
  - 출구: `KiSystemServiceStart +0x25d` 가 출발 메모 `Rax` 칸에 서비스 반환값을 처음 쓴다 → `+0x540` 그 칸을 읽어 → `+0x5bb SYSRET`.
  - `+0x2d3 KiRestoreSetContextState` 는 헤더 비트 `0x8000000` 이 설 때만 — "**남이 문맥을 넣은 경우로 보인다**" `[추측]` (이번에 정정, 2.16).
- `[정적]` run78: 상대 마감은 `KeWaitForSingleObject +0x3b6 ~ +0x3d7` 에서 **한 번만** 계산한다.
- `[정적]` run79: 남을 퇴근시킬 땐 쪽지 (`SchedulerApc`) 를 붙이고 (`KeRequestTerminationThread` → `KiInsertQueueApc` · `KiSignalThreadForApc`) 곧 SUCCESS. 쪽지를 받은 직원 안에서 `KiSchedulerApc +0x1d4` → `KiDeliverApc +0x46b KiSchedulerApcTerminate` 로 가는 "중간 한 걸음" 은 `[추측]`.
- `[정적]` run13: `KernelApcPending` = "쪽지 있음" 깃발. `KiDeliverApc` 는 쪽지에 적힌 주소를 **간접 호출** 로 부르는 자리가 있다.
- `[소스]` test.exe 는 `SetThreadContext` 를 안 부른다.
- `[추측]` PE_emulator: 기다림 이어짐 · 결과 버림 · 신호 안 먹음.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서 "호출 관계" 에서 대표만 남겼다.
가지의 `+0x..` 는 **돌아올 자리 (ret)**, 가지 밖 `+0x..` 는 그 줄 자리다. 괄호 번호는 run 문서 그대로라 중간이 빈다. ★ 는 이 글 절.

```
[A — 문맥을 넣는 직원]
nt!NtSetContextThread (핸들, ctx)
 │  +0x63 / +0x6f / +0xa4  막는 검사 → 0xc000060a / 0xc0000008                               ★ 2.14
 └─ (2) +0xc7  PspSetContextThreadInternal(B, ctx, 모드, 모드, 1)                               ★ 2.5
      ├─ (2) +0x257  RtlpReadExtendedContext            유저 CONTEXT → A 커널 스택 버퍼
      │  +0xf7 / +0xfa  B == 나 ? 예 → +0x136 PspGetSetContextSpecialApc 직접 (run16)
      ├─ (3) +0x278  KeInitializeEvent(꾸러미+0x60, 알림형, 꺼짐)                                ★ 2.6
      ├─ (4) +0x2ae  KeInitializeApc(…, PspGetSetContextSpecialApc, 0, 0, 커널)   특별 커널 APC
      ├─ (5) +0x2c4  KeInsertQueueApc(apc, 1, B, 2)      FALSE → +0x33e 0xc0000001                ★ 2.7
      │    │  +0x134 ApcQueueable · +0x19c KernelApcPending = 1 · 조건 넷 (+0x1b7 ~ +0x1ea)
      │    ├─ (2) +0x215  KiRemovePrcbWaitEntry(B)
      │    ├─ (3) +0x21d  KiEnterDeferredReadyState(B)    +0x31 / +0x41 / +0x4e State = 7
      │    │  +0x235 WaitStatus = 0x100 · +0x24b WaitRegister.Apc
      │    └─ (4) +0x270  KiExitDispatcher(Prcb, 0, 1, 2, …)
      │         └─ KiProcessThreadWaitList   +0x155 ~ +0x16f B 블록을 이벤트 목록에서 뗌 · +0x9f AdjustIncrement 2
      └─ (6) +0x2e2  KeWaitForSingleObject(이벤트, 0, 커널, FALSE, NULL)   A 잠 → +0x154 EBX = [꾸러미+0x5c]

[B — 기다리던 직원, 다시 돌 차례]
KiSwapThread (run75)                                                                            ★ 2.9
 ├─ (1) +0x248  KiSwapContext → SwapContext +0x5c1 ~ +0x5e6  AL = 커널 APC 지금 배달
 │  +0x25c  RDI = WaitStatus (0x100)   ← 쪽지보다 먼저
 └─ (2) +0x2f3  KiDeliverApc(0, 0, 0)
      │  +0x138 NormalRoutine 0 → 특별 APC 길 · +0x4f7 KernelApcInProgress |= 2
      └─ +0x774  (간접, guard_dispatch_icall) PspGetSetContextSpecialApc     [공백 → 동적: 간접 호출]
           ├─ (1) +0x69  PspGetSetContextInternal(꾸러미, 1, &이벤트칸)                           ★ 2.10
           │    │  +0xa4 / +0xb2 트랩 = InitialStack − 0x190 · +0xb9 → 스택 거슬러 오르기 (+0x29d RtlVirtualUnwind2)
           │    │  +0x2c0 SET · +0x3d2 SetContextState == 0 (보통)                                 ★ 2.13
           │    └─ +0x3ef  PspSetContext    +0x68 Rip · +0x76 Rsp · +0xd8 Rax (INTEGER) … · +0x305 상태값 0
           └─ (2) +0x8b  KeSetEvent(A 의 이벤트, 1, FALSE)       → A 깸
 │  +0x29a RAX = 0x100 → KiCommitThreadWait +0x225 → KeWaitForSingleObject +0x860 / +0x891 JMP +0x104   ★ 2.11
 │  … 신호면 +0x5f0 먹고 R14D = 0 / 마감이면 0x102 → +0x2c4 EAX
KiSystemServiceStart                                                                            ★ 2.12
 │  +0x25d  트랩 Rax ← 기다림 결과 (쪽지가 쓴 Rax 덮음)
 │  +0x291 / +0x297  헤더 비트 27 꺼짐 (보통) → 건너뜀
 └─ ══▶ +0x540 RAX · +0x56c RCX = 새 Rip · +0x573 R11 = 새 EFlags · +0x5bb SYSRET   (유저 새 Rip)
```

- 모두 `[정적]`. 단 `KiDeliverApc +0x774` 아래 한 고리만 `[공백 → 동적]` 이다 (2.9).
- 이 그림에 처음 나오는 새 말 (자세히는 2.3 · 2.10 · 2.13):
  - **꾸러미** = A 가 자기 커널 스택 위에 만든 서류 묶음 (쪽지 + 새 메모 사본 + 결과 칸 + 작은 신호등).
  - **스택 거슬러 오르기** = B 의 커널 스택을 함수 하나씩 되짚어 올라가며, B 의 유저 개인 서랍 (비휘발 레지스터) 이 어디 맡겨져 있는지 찾는 일.
  - **`SetContextState`** = 인사 카드의 "메모 보관 봉투" 칸. 보통 직원은 비어 있다.
- 이번에 새로 뽑은 함수는 **7개** (모두 depth 0): 대상 `KeInsertQueueApc` + 넓힌 여섯 (`NtSetContextThread` · `KeInitializeApc` · `PspSetContextState` · `KeRundownApcQueues` · `KiRequestSoftwareInterrupt` · `KiCheckForKernelApcDelivery`). 지도는 새로 안 만들었다. 나머지는 이미 있던 `.asm` 을 다시 읽었다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ (5) +0x2c4 KeInsertQueueApc(apc, 1, B, 2)`

- **누가**: `PspSetContextThreadInternal` — A 의 문맥에서 돈다.
- **어디서**: 부르는 자리 `+0x2bf`, 돌아올 자리 `+0x2c4`.
  - 멈추면 A 의 콜스택에 `nt!PspSetContextThreadInternal+0x2c4` 로 보일 자리다. 아직 안 봤다.
- **누구를**: `KeInsertQueueApc` — 쪽지를 대상 직원에게 넣는 함수.
- **인자 넷** `[정적]` (바로 앞 줄들):
  - 첫째 = 꾸러미 맨 앞의 쪽지 (`KAPC`).
  - `+0x2b7 EDX = 1` — **넣기**. 0 이면 읽기다 (`PspGetSetContextInternal +0x2c0` 이 이 값으로 갈린다).
  - `+0x2b4 R8 = R13` — **B**.
  - `+0x2ae R9D = 2` — 깨울 때 붙일 **가산점 +2**. 이 2 가 `KiExitDispatcher` 의 `Increment` 로 가서 B 의 `AdjustIncrement` 가 된다 (2.7).
- **무엇을 위해**: B 책상에 급한 쪽지를 붙이고, B 가 자고 있으면 그 자리에서 깨운다 (2.7).
- 돌아온 값이 0 (FALSE) 이면 `+0x2c4 TEST AL,AL / +0x2c6 JZ` → `+0x33e` 에서 `0xc0000001` 로 끝난다. **기다리지 않는다** (2.14).

#### 줄 2: `KeInsertQueueApc +0x235` — 호출이 아니라 **쓰기** 줄

```
KeInsertQueueApc +0x235  MOV qword [RSI+0xc8],0x100
```

- **누가**: `KeInsertQueueApc`. 여기서 `RSI` = **B** (결과서).
- **어디에**: B 업무 카드 `+0xc8` = `WaitStatus` — B 가 깨어날 때 들고 갈 **결과 칸** (run75 해설 2.9).
- **무엇을**: `0x100` — run77 해설 2.7 의 **"처음부터 다시 확인" 도장**.
- **무엇을 위해**: B 가 깨어나면 이 값을 들고 `KeWaitForSingleObject` 로 돌아가, `+0x860` 에서 보고 **처음부터 다시** 기다리게 하려는 것이다 (2.11).
- 신호등 담당이 깨울 땐 이 칸에 번호표의 `WaitKey` (한 물건이면 0) 를 쓴다 (run77). 쪽지가 깨울 땐 `0x100` 이다. **같은 칸, 다른 값** 이다.

#### 줄 3: `KiSwapThread +0x25c` — 쪽지를 펴기 **전에** 결과 칸을 읽는다

```
KiSwapThread +0x25c  MOV RDI,[R13+0xc8]
```

- **누가**: `KiSwapThread` — B 가 다시 계산대를 받아 교대에서 돌아온 직후다. `R13` = 지금 직원 (B).
- **무엇을**: 결과 칸 (`0x100`) 을 `RDI` 에 담아 둔다.
- **그다음**: `+0x2ee` 에서 쪽지를 펴고 (`KiDeliverApc`), `+0x29a MOV RAX,RDI` 로 **담아 둔 값을 그대로** 돌려준다 (2.9).
- **무엇을 위해**: 쪽지 안에서 무슨 일이 있든, B 가 위로 들고 갈 값은 이미 `0x100` 으로 정해져 있다. 그래서 B 는 쪽지를 편 뒤 반드시 **처음부터 다시** 기다리러 간다.
- 비유: 일어나자마자 결과 칸의 도장부터 **주머니에 넣고**, 그다음에 쪽지를 편다.

### 2.3 무대 — 새 말과 이번에 쓰는 칸

뒤에서 오프셋이 나오면 이 절로 돌아오면 된다.

#### 새 말 — 특별 커널 APC (급한 쪽지)

- 쪽지 (`_KAPC`, `0x58`B) 에는 "부를 일" 칸이 둘 있다 `[정적]`:
  - `KernelRoutine +0x20` · `NormalRoutine +0x30`.
  - 공개 PDB 는 `+0x20` 부터 24바이트를 `Reserved` 로만 보여 준다. 이름은 `KeInitializeApc` 가 인자를 적는 줄 (`+0x3b [RCX+0x20] = R9` · `+0x3f [RCX+0x30]`) 에서 읽었다.
- **특별 커널 APC** = `NormalRoutine` 칸이 **0 인** 커널 모드 쪽지다 `[정적]` (결과서의 정의).
  - 일반적으로는 `KernelRoutine` 이 먼저 커널에서 돌고, `NormalRoutine` 이 있으면 그다음에 도는 것으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 이번 쪽지: `KernelRoutine = PspGetSetContextSpecialApc`, `NormalRoutine = 0`. 펴면 `PspGetSetContextSpecialApc` **하나만** 돈다.
- 코드에서 보이는 "특별" 의 차이 `[정적]`:
  - **깨울 때**: 특별이면 `KernelApcDisable` · `KernelApcInProgress` 두 칸을 **안 본다** (`KeInsertQueueApc +0x1d5` 에서 건너뜀). 보통 쪽지는 두 칸이 0 이어야 깨운다 (`+0x39c` · `+0x3aa`). 특별 쪽지를 막을 수 있는 칸은 `SpecialApcDisable` 하나다 (2.7).
  - **펼 때**: `KiDeliverApc +0x135 TEST / +0x138 JZ +0x2b1` — `NormalRoutine 0` 이면 특별 쪽지 길로 간다 (2.9).
  - **만들 때**: `KeInitializeApc +0x43 CMOVZ` — `NormalRoutine 0` 이면 모드를 0 (커널) 으로 **강제** 한다 (flow-writer 보탬 ⑥).
- 비유: **급한 쪽지.**
  - "관리사무소 안에서 바로 할 일" 한 줄만 적혀 있고, "그다음 본업" 칸은 비어 있다.
  - 그래서 "쪽지 받지 마세요" 표지가 웬만큼 걸려 있어도 들어간다. 막을 수 있는 건 가장 센 표지 하나 (run16 해설의 **"작업 중, 쪽지는 잠깐 뒤에"**) 뿐이다.
- 견줄 것: run79 의 **퇴근 쪽지** (`SchedulerApc`) 는 `NormalRoutine = KiSchedulerApc` 인 **보통** 커널 쪽지다 (2.7 끝).

#### 새 말 — 꾸러미 (A 작업대 위의 서류 묶음)

- `PspSetContextThreadInternal` 의 지역 칸 하나 (`RBP+0x10`). run 문서·결과서가 **"꾸러미"** 라고 부른다 `[정적]`:

| 꾸러미 안 | 무엇 | 이 글에서 부르는 말 |
|---|---|---|
| 앞 `0x58` 바이트 | `KAPC` | **급한 쪽지** |
| `+0x59` | 비트 모음 — 비트 0 "남이라 이벤트 있음" `[추측: 이름]`, 비트 1 "스택 거슬러 오르기" (2.10) | 꾸러미 표시 |
| `+0x5c` | 상태값 | **결과 칸** (B 가 적는다) |
| `+0x60` | 이벤트 (알림형, 꺼짐) | A 의 **작은 신호등** |
| `+0x78` | 컨텍스트 포인터 | **새 메모 사본** 을 가리킴 |
| `+0x80` | 포인터 표 | B 의 **개인 서랍 자리 목록** (2.10) |

- 핵심: 이 모두가 **A 의 커널 스택 위** 에 있다 (결과서). 2.6 에서 "A 는 왜 기다리나" 의 열쇠가 된다.
- 비유: A 가 관리사무소 안 **자기 작업대** 위에 서류 꾸러미 하나를 펼친다. 급한 쪽지, 새 메모 사본, 결과 칸, 작은 신호등. B 는 쪽지를 펴면 이 꾸러미를 보고 일하고, 결과 칸에 적고, A 의 작은 신호등을 켠다.

#### 업무 카드 (`_KTHREAD`) · 인사 카드 (`_ETHREAD`) 에서 이번에 쓰는 칸

| 카드 | 칸 | 이름 | 이 글에서 |
|---|---|---|---|
| 업무 카드 | `+0x0` | `Header` 첫 DWORD **비트 27** (`0x8000000`, 공개 PDB 이름 없음) | 명판 귀퉁이 **"봉투에 메모 있음" 표시** (2.13) |
| | `+0x28` | `InitialStack` | 관리사무소 작업대 맨 위 |
| | `+0x40` | `ThreadLock` | 직원 자물쇠 |
| | `+0x70` | `WaitRegister` (`Apc` 비트 5 = `0x20`) | 기다림 진행 단계 + "쪽지 왔음" 비트 |
| | `+0x74` | `MiscFlags` (`SystemThread` 비트 10 · `ApcQueueable` 비트 14) | "쪽지 붙일 수 있음" |
| | `+0x90` | `TrapFrame` | |
| | `+0x98` | `ApcState` | 쪽지 목록 머리 |
| | `+0xc0` · `+0xc1` · `+0xc2` | `KernelApcInProgress` · `KernelApcPending` · `UserApcPending` (셋 다 `[추측: 이름]`) | 쪽지 펴는 중 · **"쪽지 있음" 깃발** · 유저 쪽지 있음 |
| | `+0xc8` | `WaitStatus` | **결과 칸** |
| | `+0x184` | `State` (2 Running · 5 Waiting · 7 DeferredReady) | 상태 번호 |
| | `+0x186` | `WaitIrql` | 기다릴 때의 방해 금지 단계 |
| | `+0x1e4` | `KernelApcDisable` | 보통 쪽지 막기 |
| | `+0x1e6` | `SpecialApcDisable` | **"작업 중, 쪽지는 잠깐 뒤에"** (가장 센 표지) |
| 인사 카드 | `+0x6a8` | `SetContextState` (`_CONTEXT*`) | **메모 보관 봉투** (2.13) |
| | `+0x670` | `PicoContext` | |

- 이름은 PDB (`pdb_struct.py`, run 문서 "칸 이름"), 쓰임은 `[정적]`.

#### 출발 메모 (`_KTRAP_FRAME`, `0x190`B) — 이번에 쓰는 칸

| 칸 | 이름 | 출구 (`KiSystemServiceStart`) 에서 부르는 자리 |
|---|---|---|
| `+0x30` | `Rax` | `[RBP-0x50]` |
| `+0x38` · `+0x40` · `+0x48 ~ +0x60` | `Rcx` · `Rdx` · `R8 ~ R11` | |
| `+0x140 ~ +0x158` | `Rbx` · `Rdi` · `Rsi` · `Rbp` | |
| `+0x168` | `Rip` | `[RBP+0xe8]` |
| `+0x170` | `SegCs` | `[RBP+0xf0]` |
| `+0x178` | `EFlags` | `[RBP+0xf8]` |
| `+0x180` | `Rsp` | `[RBP+0x100]` |
| `+0x188` | `SegSs` | |

- 출구에서는 `RBP = 트랩 프레임 + 0x80` 이라 위처럼 보인다 `[정적]` (run77 해설 2.13 과 같다).

#### 새 메모 (`_CONTEXT`) 맨 위 체크 칸 — `ContextFlags +0x30`

| 값 | 이름 (표준) | 무엇을 바꾸나 (2.10) |
|---|---|---|
| `0x100001` | CONTROL | `Rip` · `Rsp` · `EFlags` · 세그먼트 |
| `0x100002` | INTEGER | `Rax` · `Rcx` · `Rdx` · `R8 ~ R11` + 비휘발 `Rbx Rbp Rsi Rdi R12 ~ R15` |
| `0x100008` | FLOATING_POINT | `Xmm` · `MxCsr` |
| `0x100010` | DEBUG_REGISTERS | 디버그 레지스터 (이름만) |
| `0x100040` | XSTATE | (이름만) |

- 숫자는 `[정적]`, 이름은 표준 이름이다 (run 문서).
- 비유: 새 메모 맨 위의 **체크 칸**. "이 묶음만 바꿔 주세요." 체크 안 한 묶음은 손대지 않는다.

### 2.4 이번 바퀴의 핵심 — A·B 시간 순서 한 장

run 문서 "순서 한 장" 17단계를 **장면 열** 로 묶었다. 오른쪽 `#` 은 run 문서 표 번호다.
줄은 모두 `[정적]` — **코드에 쓰인 순서** 다. 실제로 어느 계산대에서 언제 도는지는 런타임이다.

| 장면 | # | 누가 | 줄 | 하는 일 | 비유 |
|---|---|---|---|---|---|
| ① 신청 | 1 ~ 3 | A | `NtSetContextThread +0x48 / +0x4d` → `+0xc2 / +0xc7` → `PspSetContextThreadInternal +0x252` · `+0xf7 / +0xfa` | 손잡이 → B, 막는 검사, 새 메모를 A 커널 스택으로 복사, "남이다" | A 가 창구에 "B 메모를 이걸로" 신청. 새 메모는 A 작업대로 베껴 둠 |
| ② 꾸러미 | 4 | A | `+0x273` · `+0x2a9` | 작은 신호등 (꺼짐) · 급한 쪽지 | A 작업대 위에 꾸러미 펼치기 |
| ③ 쪽지 넣고 깨우기 | 5 · 6 | A | `+0x2bf / +0x2c4 KeInsertQueueApc` → `KiExitDispatcher` → `KiProcessThreadWaitList` | 깃발 · 조건 넷이면 B 를 깨움: 대기 목록에서 뗌, `State = 7`, 명단 push, **`WaitStatus = 0x100`**, 번호표 뗌, 가산점 +2, 준비 줄 | B 책상에 급한 쪽지. 자는 B 를 그 자리에서 깨워 "처음부터 다시 확인" 도장. 신호등 불은 그대로 |
| ④ A 잠 | 7 | A | `+0x2dd / +0x2e2 KeWaitForSingleObject` | 작은 신호등에서 잠 (마감 없음). B 가 이미 끝냈으면 신호 상태라 안 잠 | A 는 자기 작은 신호등 앞에서 잔다. 알람 없음 |
| ⑤ B 가 돈다 | 8 | B | `SwapContext +0x5c1 ~ +0x5e6` → `KiSwapThread +0x24b` · **`+0x25c`** · `+0x277` | "커널 APC 있음" AL = 1, `0x100` 을 먼저 읽어 둠 | 계산대를 받자마자 도장부터 주머니에 |
| ⑥ 쪽지 펴기 | 9 | B | `KiSwapThread +0x2ee / +0x2f3 KiDeliverApc` → `+0x138 JZ +0x2b1` · `+0x76f` | 특별 쪽지 길 → `PspGetSetContextSpecialApc` | 급한 쪽지를 편다 |
| ⑦ 메모 고치기 | 10 | B | `PspGetSetContextSpecialApc +0x64 / +0x69` → `PspGetSetContextInternal +0xa4` · `+0x298` · `+0x3d2` · **`+0x3ea / +0x3ef PspSetContext`** | 개인 서랍 자리 찾기, 출발 메모 `Rip`·`Rsp`·`EFlags` (CONTROL), `Rax ~ R11` + 비휘발 (INTEGER) | B 가 **자기 손으로** 출발 메모를 고쳐 붙인다 (게시 담당) |
| ⑧ A 깨우기 | 11 · 12 | B → A | `PspGetSetContextInternal +0x305` · `PspGetSetContextSpecialApc +0x86 / +0x8b KeSetEvent` → A `+0x2e2 JMP +0x154` | 결과 칸에 `0` → A 의 신호등 켬 → A 는 `0` 반환 | A 의 작은 신호등을 초록으로. A 는 "다 됐다" 를 들고 돌아감. **B 는 아직 기다림 중일 수 있다** |
| ⑨ 처음부터 다시 | 13 · 14 | B | `KiSwapThread +0x29a` → `KiCommitThreadWait +0x225` → `KeWaitForSingleObject +0x846` · **`+0x860` · `+0x891 JMP +0x104`** → `+0x5f0` 또는 `+0x7b7` · `+0x841` | 신호면 **가져가고** `R14D = 0`. 아니면 번호표 다시 걸고 잠 (마감 지났으면 `0x102`) | 도장 보고 신호등으로 돌아가 처음부터. 초록이면 지나가며 가져감 |
| ⑩ 유저로 | 15 ~ 17 | B | `KeWaitForSingleObject +0x2c4` → `KiSystemServiceStart +0x195` → **`+0x25d`** → `+0x291` · `+0x540 ~ +0x5bb SYSRET` | 출발 메모 `Rax` ← 기다림 결과, 비트 27 꺼짐 → `RCX = 새 Rip` · `R11 = 새 EFlags` · `RSP = 새 Rsp`, `RAX = 기다림 결과` | 답 봉투엔 **신호등 결과**, 돌아갈 문 쪽지엔 **고친 새 자리** |

- 장면 ③ 의 "준비 줄로" 뒤, A 가 곧바로 B 에게 계산대를 넘길 수도 있다 (`KiExitDispatcher +0x69 JNZ +0x134`, sast/73 `[추측]`). 그래서 ④ 와 ⑤ 의 앞뒤는 런타임이다.
- 장면 ③ 의 `State = 7` 은 run 문서 표 5번에서 flow-writer 가 줄 자리 (`+0x218 KiEnterDeferredReadyState` 안) 를 넣어 고쳐 적은 것이다 (2.7, 2.17).

#### 한 이야기로

1. A 가 관리사무소 창구에 "B 의 출발 메모를 이걸로 바꿔 주세요" 를 낸다.
2. 창구는 새 메모를 **A 작업대 위** 꾸러미에 베껴 두고, 같은 꾸러미에 **작은 신호등** (빨강) 과 **급한 쪽지** 를 만든다.
3. 급한 쪽지를 B 책상에 붙인다. B 는 신호등 앞에서 자고 있다.
   - 쪽지 담당은 B 를 **그 자리에서 깨운다.** B 의 번호표를 신호등 줄에서 떼고, 결과 칸에 **"처음부터 다시 확인"** 도장을 찍고, 대기 줄에 세운다 (가산점 +2).
   - **신호등 불은 안 건드린다.**
4. A 는 자기 작은 신호등 앞에서 잔다. 알람은 없다.
5. B 가 계산대를 받는다. 결과 칸 도장부터 주머니에 넣고, 쪽지를 편다.
6. 쪽지대로 **B 가 자기 출발 메모를** 고쳐 붙인다. 체크한 묶음만. 그리고 A 꾸러미의 결과 칸에 "0" 을 쓰고, A 의 작은 신호등을 켠다.
7. A 는 깨서 "0 (성공)" 을 들고 돌아간다.
8. B 는 주머니의 도장을 보고 신호등으로 돌아가 **처음부터 다시** 본다. 그 사이 초록이 됐으면 지나가며 가져간다. 아니면 다시 번호표 걸고 잔다.
9. B 가 끝내 일어나면, 답 봉투엔 **신호등 결과** 가, 돌아갈 문 쪽지엔 **고친 새 자리** 가 들어간다. B 는 새 자리에서 일을 시작한다.

### 2.5 A 쪽 ① — 창구 검사, 그리고 새 메모 사본 (질문 1 · 5 일부)

#### `NtSetContextThread` — 신청 창구 `[정적]`

| 줄 | 하는 일 | 걸리면 |
|---|---|---|
| `+0x30 EDX = 0x10` → `+0x48 / +0x4d ObReferenceObjectByHandle` | 손잡이 → B 의 카드. 권한 `0x10` (THREAD_SET_CONTEXT) | `+0x51 JS` 그 상태 |
| `+0x63` | 내 가게 `Flags2 & 0x20000` (`RestrictSetThreadContext`) + B 가 **같은 가게** | `+0xd8` `0xc000060a` |
| `+0x6f` · `+0xa4` | B 가 `SystemThread` 이거나 `PicoContext ≠ 0` | `+0x78` `0xc0000008` |
| `+0xb1 [RSP+0x20] = 1` → **`+0xc2 / +0xc7 PspSetContextThreadInternal(B, ctx, 모드, 모드, 1)`** | 본 일로 | |

- 다섯째 인자 `1` 은 꾸러미 비트 1 을 켠다. 그 비트가 2.10 의 **스택 거슬러 오르기** 를 켠다.
- 비유: 창구는 세 가지를 본다. (1) 손잡이 권한, (2) "같은 가게끼리 메모 바꾸기 금지" 표시, (3) 관리사무소 직원이나 Pico 직원인지. 통과하면 본 담당에게 넘긴다.

#### `PspSetContextThreadInternal` 앞부분 `[정적]`

- `+0x9c / +0xa1 RtlpSanitizeContextFlags` — 체크 칸 정리. 실패면 `+0xa3 JS +0x169`.
- 유저가 부른 거면 `+0x1d4 ~ +0x252` 에서 **유저 CONTEXT 를 A 커널 스택 버퍼로 복사** 한다 (`+0x252 / +0x257 RtlpReadExtendedContext`). `+0x238` 에서 꾸러미의 컨텍스트 포인터 = 그 버퍼.
- **`+0xf7 CMP R13,R14 / +0xfa JNZ +0x264`** — `R13` = B, `R14` = 지금 직원 (A).
  - 같으면 run16 길: `+0x131` (ret `+0x136`) `PspGetSetContextSpecialApc` 를 **쪽지 없이 직접**.
  - 다르면 `+0x264` 로 — 이번 길.
- 비유: 본 담당은 A 가 가져온 새 메모를 **A 의 작업대 위로 베껴** 둔다. 대상이 A 자신이면 쪽지 없이 바로 붙이고 (run16), 남이면 쪽지를 쓴다.

### 2.6 A 쪽 ② — 급한 쪽지 넣기, 그리고 A 도 잔다 (질문 1 · 1')

#### 걸음 `[정적]` (`+0x264` 부터, 실행 순서)

| # | 줄 (call / ret) | 하는 일 | 비유 |
|---|---|---|---|
| 1 | `+0x264 OR BL,1` | 꾸러미 비트 0 ("남이라 이벤트 있음" `[추측: 이름]`) | |
| 2 | `+0x273 / +0x278 KeInitializeEvent(꾸러미+0x60, 0, 0)` | **알림형** (수동 재설정), **꺼진 채로** | 작은 신호등, 빨강 |
| 3 | **`+0x2a9 / +0x2ae KeInitializeApc(KAPC, B, 환경 0, PspGetSetContextSpecialApc, Rundown 0, Normal 0, 모드 0, 0)`** | KernelRoutine = `PspGetSetContextSpecialApc` · Rundown 0 · Normal 0 · 커널 모드 · 원래 환경 = **특별 커널 APC** | 급한 쪽지 쓰기 |
| 4 | `+0x2ae R9D = 2` · `+0x2b4 R8 = B` · `+0x2b7 EDX = 1` → **`+0x2bf / +0x2c4 KeInsertQueueApc`** | B 에게 넣기 (2.7) | B 책상에 붙이기 |
| 5 | `+0x2c4 TEST AL,AL / +0x2c6 JZ +0x33e` | 넣기 실패면 **`+0x33e EBX = 0xc0000001`**, 안 기다림 | 책상에 쪽지를 못 붙이면 돌아감 |
| 6 | **`+0x2dd / +0x2e2 KeWaitForSingleObject(이벤트, Executive, 커널, 경보 아님, 마감 없음)`** → `+0x2e2 JMP +0x154` | **A 잠**. 반환값은 안 본다 | 작은 신호등 앞에서 잔다. 알람 없음 |
| 7 | `+0x154 EBX = [RBP+0x6c]` (꾸러미 `+0x5c`) → 음수면 그대로 → 유저 호출이면 `EtwTiLogSetContextThread` (이름만) → 반환 | B 가 결과 칸에 적은 값 | 깨서 결과 칸을 읽고 돌아감 |

- `KeInitializeApc` 인자 자리는 `KeInitializeApc` 줄 (`+0x3b [RCX+0x20] = R9` · `+0x26 [RCX+0x28]` · `+0x3f [RCX+0x30]` · `+0x1f [RCX+0x50] = R8B` · `+0x4b [RCX+0x51]`) 과 맞춰 읽었다 (run 문서).
- 이름 주의: `+0x2dd` 의 callee 는 `_calls.tsv` 에 `alias:KeWaitForMutexObject+1` 로 찍힌다. PDB 이름은 `KeWaitForSingleObject` 다 (run13 해설 2.10).

#### A 는 왜 기다리나 — 단계별로

1. `[정적]` 급한 쪽지·새 메모 사본·결과 칸·작은 신호등이 **모두 A 의 커널 스택 위 꾸러미** 에 있다 (2.3).
2. `[정적]` B 는 쪽지를 펼 때 이 꾸러미를 받아 일하고 (`PspGetSetContextSpecialApc +0x64` 의 첫 인자 = 꾸러미), 결과 칸에 쓰고 (`PspGetSetContextInternal +0x305`), A 의 신호등을 켠다 (`PspGetSetContextSpecialApc +0x86 KeSetEvent`).
3. 결과서는 이것을 **"A 가 끝까지 기다려야 하는 까닭"** 으로 읽었다. A 는 B 가 일을 끝낼 때까지 자기 작업대 자리를 지킨다.
4. `[정적]` 마감이 없고 경보도 안 받는다 (`R9D = 0`). 그래서 **B 가 영영 쪽지를 안 펴면 A 도 영영 잔다** (결과서 "예"). 깨우는 건 B 쪽지 안의 `KeSetEvent` 하나뿐이다.

- **A 가 `0` 을 받는 때** = B 가 쪽지를 **돌린 뒤** 다 `[정적]`.
  - B 가 새 `Rip` 에서 **유저로 돌기 시작한 뒤가 아니다.** 그때 B 는 아직 신호등 앞에서 기다리는 중일 수 있다 (2.11).
- 비유: A 는 B 책상에 쪽지를 붙이고, 자기 작업대 앞 작은 신호등이 초록이 될 때까지 선 채로 잔다. B 가 쪽지대로 메모를 고치고 그 신호등을 켜 줘야 A 가 일어난다. 알람이 없으니, B 가 쪽지를 안 펴면 A 는 계속 서 있다.

#### run79 의 "남 퇴근시키기" 와 나란히

| | 남 퇴근시키기 (run79) | 남의 메모 바꾸기 (이번) |
|---|---|---|
| 쪽지 | 업무 카드에 박힌 `SchedulerApc` (보통 커널 쪽지) | A 작업대 위 꾸러미의 새 쪽지 (특별 커널 쪽지) |
| 실제 일은 누가 | 그 직원 **자신** (자기 문맥) | 그 직원 **자신** (자기 문맥) |
| 부른 쪽은 | 곧 SUCCESS (`PspTerminateThreadByPointer +0x7c XOR EAX,EAX`), **안 기다림** | **기다림** (`+0x2dd`), 성공은 B 가 쪽지를 돌린 뒤 |

- 둘 다 `[정적]`. "남의 일은 쪽지로 맡기고, 실제 일은 그 직원이 자기 문맥에서 한다" 는 모양이 같다. 다른 건 **부른 쪽이 기다리느냐** 다.

#### PE_emulator 에게

- "B 에게 넣어 두고 A 는 바로 돌아옴" 이면 **다름**. "A 도 기다림" 이면 **맞음** (run 문서 표 1 · 1').
- 성공 값 `0` 은 **맞음**. "곧바로 성공" 은 **틀림** (표 5).

### 2.7 쪽지가 B 를 깨운다 — `KeInsertQueueApc` (질문 2(a))

#### 첫머리 `[정적]`

- `+0x8d ~ +0x98` — 쪽지의 `KernelRoutine` 이 `0x140ab1f40` 이면 `+0x4fe` 다른 길. **우리 쪽지는 아니다.**
  - 그 주소의 이름은 `KeSpecialUserApcKernelRoutine` 이다 (`_index.tsv`, flow-writer 보탬 ③. 결과서는 `[공백]`). 이름으로 보아 특별 **유저** 쪽지 쪽이다 `[추측: 이름]`.
- IRQL 2 (`+0xed`), B 의 직원 자물쇠 (`ThreadLock`, `+0x110`).
- **넣어도 되나**:
  - `+0x134` B 의 `ApcQueueable` 이 꺼져 있으면 → `+0x394 DIL = 0` (FALSE). 2.14 의 `0xc0000001` 이 여기서 온다.
  - `+0x141` 쪽지가 이미 `Inserted` 면 → FALSE.
- `+0x16a / +0x16f KiInsertQueueApc` — B 의 커널 쪽지 목록 (`ApcState +0x98`) 에 끼운다. `NormalRoutine 0` 이면 특별 쪽지 자리.
- `+0x18a` 대상이 지금 계산대의 나 자신이면 `+0x296` 다른 길 · `+0x194` 유저 쪽지면 `+0x408`. 둘 다 이번 길 아님.
- **`+0x19c KernelApcPending = 1`** — B 책상에 **"쪽지 있음" 깃발**.

#### B 는 지금 무슨 상태인가 — `+0x1a8 State` `[정적]`

| B 의 `State` | 하는 일 |
|---|---|
| 2 (Running, 계산대에서 일하는 중) | `+0x476` 소프트웨어 인터럽트만 (`+0x49a KiRequestSoftwareInterrupt` / 다른 계산대면 `+0x48e KiSendSoftwareInterrupt`) |
| **5 (Waiting, 기다리는 중)** | **아래 조건 넷** |
| 그 밖 | `+0x24f` 아무것도 안 하고 TRUE (깃발만 서 있음) |

#### 깨우기 조건 넷 (`State == 5` 일 때) `[정적]`

| 줄 | 검사 | 뜻 | 비유 |
|---|---|---|---|
| `+0x1bf` | `WaitIrql == 0` | B 가 보통 단계 (PASSIVE_LEVEL) 에서 기다리는 중 (결과서) | 방해 금지 표지판 0 단계에서 자는 중 |
| `+0x1cb` | `SpecialApcDisable == 0` | 가장 센 표지가 안 걸림 | "작업 중, 쪽지는 잠깐 뒤에" 가 없음 |
| `+0x1d5` | `NormalRoutine == 0` 이면 다음 둘 건너뜀. 보통 쪽지면 `+0x39c KernelApcDisable == 0` · `+0x3aa KernelApcInProgress == 0` 도 필요 | 급한 쪽지라 면제 | |
| `+0x1e0 ~ +0x1ea` | `WaitRegister.State` 가 1 → 깨움. 아니면 `+0x4de CMP 4 / +0x4e1 JNZ +0x24b` (4 면 깨움, 그 밖은 `Apc` 비트만) | 1 = 커밋됨, 4 (run77 해설 2.9 표) | **"정말 잔다" 도장** 이 찍혔나 |

#### 깨우는 일 — 한 걸음씩 `[정적]`

| 줄 (call / ret) | 하는 일 | 비유 |
|---|---|---|
| `+0x1f0 ~ +0x209` | 큐 동시성 카운트 | |
| **`+0x210 / +0x215 KiRemovePrcbWaitEntry(B)`** | 대기 목록에서 뗌 (결과서). 이름으로 보아 계산대 장부 쪽 기다림 목록이다 `[추측: 이름]` | 계산대 "자는 직원" 목록에서 지움 |
| **`+0x218 / +0x21d KiEnterDeferredReadyState(B)`** | **`State = 7`** — 그 함수 안 `+0x31` · `+0x41` · `+0x4e`. Waiting 이었으면 기다린 시간도 더함 (`+0xf ~ +0x3a`) | "줄 설 사람" 표시 |
| `+0x21d ~ +0x22e` | `PRCB+0x2d08` 에 push | 깨운 직원 명단에 올림 |
| **`+0x235`** | **`WaitStatus = 0x100`** | 결과 칸에 "처음부터 다시 확인" 도장 |
| `+0x240` | `AbWaitObject = 0` | |
| `+0x24b` | `WaitRegister.Apc` 켬 | "쪽지 왔음" 비트 |
| `+0x24f` · `+0x254` | TRUE · 직원 자물쇠 풀기 | |
| **`+0x26b / +0x270 KiExitDispatcher(Prcb, 0, 1, 2, 옛 IRQL)`** | → `KiProcessThreadWaitList`: 블록마다 `+0x155 BlockState == 4` 인 것을 `+0x15f ~ +0x16f` 객체 목록에서 **뗌** · `+0x98 AdjustReason = 1` · `+0x9f AdjustIncrement = 2` · `+0x101 KiDeferredReadySingleThread` | 퇴장 담당이 신호등 줄에서 B 번호표를 떼고, 가산점 +2 붙여 대기 줄에 |

- `KiTryUnwaitThread` (어깨 두드리기 담당) 는 **안 부른다.** 같은 일을 `KeInsertQueueApc` 가 **몸에 풀어 넣었다** `[정적]` (run77 해설 2.8 "몸에 풀어 넣기").
- 번호표를 떼는 쪽이 `KiProcessThreadWaitList` 인 까닭 `[정적]`: `KeInsertQueueApc` 는 번호표의 `BlockState` 를 안 바꾼다. 그래서 `BlockState == 4` 그대로인 표를 이 함수가 뗀다.
  - [run78 해설 2.14](run78-thread-timer-해설.md) 의 "진 번호표 떼기" 와 같은 함수·같은 갈래다. run78 쪽은 같은 갈래의 실제 쓰기 줄 `+0x259 / +0x25c` 를 적었고, run80 문서는 그 앞 `+0x15f ~ +0x16f` 를 적었다. 오프셋이 달라 보여도 같은 떼기다.

#### 신호등 불은 안 건드린다 `[정적]`

- 이 길에는 이벤트 `SignalState` 를 쓰는 줄이 **없다.**
- 그래서 B 는 **번호표만 떼이고**, 신호등 불은 그대로다.
- 이게 2.8 (겹칠 때) 과 2.11 (신호를 먹나) 의 바탕이다.

#### 조심 — `State = 7` 은 도우미 안에서 쓴다 (run 문서 "다르게" ①)

- 오케스트레이터가 확인을 부탁한 문장: "`KeInsertQueueApc +0x210 ~ +0x240` 이 `State = 7` 로 바꾼다".
- flow-writer 가 `.asm` 으로 본 것 `[정적]`:
  - `KeInsertQueueApc.asm` 안에는 `[RSI+0x184]` 에 **쓰는 줄이 없다.** `+0x1a8` · `+0x408` 두 곳 모두 `MOVZX` **읽기** 다.
  - `State = 7` 은 `+0x218` (ret `+0x21d`) 로 부르는 **`KiEnterDeferredReadyState` 안** `+0x31` · `+0x41` · `+0x4e` 의 `MOV byte [RCX+0x184],0x7` 이다. 85바이트짜리 도우미다.
- 결과서 본문 (`+0x218 KiEnterDeferredReadyState(B)` (State=7)) 은 맞았다. 시간 순서 표 5번만 줄 자리가 빠졌고, run 문서 표는 그 자리를 넣어 적었다.
- 쌍둥이 `KiSignalThreadForApc` 는 같은 일을 몸에 풀어 넣어 `+0xfc` 에서 **직접** 쓴다.
- 도구 메모 (run 문서): "칸에 쓰는 줄" 이 **불린 도우미 안** 에 있으면 caller `.asm` 을 grep 해도 안 나온다. 같은 일을 몸에 풀어 넣은 쌍둥이가 있으면 둘을 나란히 보면 빨리 찾는다.
- 비유: 쪽지 담당은 직원 상태 칸을 "줄 설 사람" 으로 바꾸는 도장을 **자기 손으로 안 찍고 도장 담당에게** 맡긴다. 쌍둥이 담당은 같은 도장을 자기 손으로 찍는다. 그래서 쪽지 담당의 손만 보면 도장이 안 보인다.

#### `KiSignalThreadForApc` 도 같은 몸 — run79 퇴근 쪽지와 이어진다 `[정적]`

- `KiSignalThreadForApc` 의 걸음: `+0x3c` `KernelApcPending` · `+0x69` `WaitIrql` · `+0x75` `SpecialApcDisable` · `+0x83` `NormalRoutine` · `+0x8e ~ +0x98` `WaitRegister.State` · `+0xbe KiRemovePrcbWaitEntry` · **`+0xfc State = 7`** · `+0x103 ~ +0x114` push · **`+0x11b WaitStatus = 0x100`** · `+0x131` `Apc` 비트.
- 차이는 `R9B` 비트 0 하나 (`+0x57 / +0x5b` 켜져 있으면 안 깨움).
- 퇴근 쪽 두 곳 `KeRequestTerminationThread +0x89` · `KiSchedulerApc +0x201` 은 둘 다 `R9D = 0` 으로 부른다 (`+0x7d` · `+0x1f5 XOR R9D,R9D`). → 같은 조건으로 같은 `0x100`.
- **flow-writer 덧붙임 ① — "같은 규칙" 의 차이 두 칸**:
  - 퇴근 쪽지 `SchedulerApc` 는 `NormalRoutine = KiSchedulerApc` 인 **보통** 커널 쪽지다 (`KeInitThread +0x12d` → `KeInitializeApc` 여섯째 인자 `+0x3f [RCX+0x30]`).
  - 보통 쪽지는 깨우기 전에 `KernelApcDisable == 0` · `KernelApcInProgress == 0` 두 칸을 **더** 본다 (`KiSignalThreadForApc +0x83 / +0x88 → +0x219` · `+0x227`, `KeInsertQueueApc +0x1d5 / +0x1da → +0x39c` · `+0x3aa`).
  - 배달은 `KiDeliverApc +0x20e / +0x21b` 가 `KiSchedulerApc` 를 **직접** 부르고, 돌아와 `+0x241 JMP +0xac` 로 목록을 다시 본다.
- 그래서 [run79 해설 2.14](run79-thread-exit-해설.md) 의 "중간 한 걸음 `[추측]`" 이 **일부** 닫혔다. 깨우기 규칙과 배달 자리까지 `[정적]`.
  - 남은 몫: 다시 넣은 퇴근 쪽지가 어느 목록으로 들어가 어느 갈래로 `KiSchedulerApcTerminate` 에 닿나 → **Q153 ①** (4절).

#### PE_emulator 에게

- "기다림 이어짐 (그대로 둠)" 은 **반쯤** 맞다. B 는 기다림에서 **한 번 나왔다가** 다시 들어간다 (run 문서 표 2(a)).
- 조건 넷이 없으면 **모자람** 이다.
- 규칙 2 에 "보통 커널 쪽지면 두 칸 더" 한 줄이면 퇴근 쪽지까지 **한 규칙** 으로 된다 (덧붙임 ①, 넣을지는 PE_emulator 판단).

### 2.8 신호와 쪽지가 겹칠 때 (질문 2(b))

[run78 해설 2.15](run78-thread-timer-해설.md) 의 "먼저 잠근 쪽이 이긴다" 와 같은 그림이다. 이번엔 신호등 담당 vs **쪽지 담당** 이다.

| 경우 | 줄 `[정적]` | 결과 | 비유 |
|---|---|---|---|
| **신호가 먼저** (B 가 이미 `State ≠ 5`) | `KeInsertQueueApc +0x1b7 / +0x1b9` → `+0x24f` | 쪽지 담당은 **깃발만** 세우고 끝. B 는 신호 결과 (`0`) 를 들고 깨고, **돌아오는 길에** 쪽지를 편다. 최종 반환 `0` | 이미 일어난 B 책상에 깃발만 꽂고 간다 |
| **쪽지가 먼저** (B 를 `State = 7` 로 만든 뒤 신호) | (run77 · run78) 신호등 담당이 `State ≠ 5` 로 B 를 지나침 | 자동 신호등이면 빨강으로 **안 돌리고** 남긴다 → B 가 처음부터 다시 볼 때 **가져감** (2.11) | 늦은 신호등 담당이 "이미 일어났네" 하고 지나간다. 초록은 B 몫으로 남는다 |
| **커밋 전** (`WaitRegister.State` 가 1·4 가 아님) | `KeInsertQueueApc +0x4de / +0x4e1 → +0x24b` (`Apc` 비트만) → `KiCommitThreadWait +0x97 ~ +0xa3` · `+0x435 / +0x437` · `+0x4fa` | B 는 **안 자고** `0x100` 으로 나온다 | "정말 잔다" 도장 찍기 직전에 "쪽지 왔음" 비트를 보고 아예 안 잔다 |
| **기다림에 들어오기 전에** 이미 깃발 | `KeWaitForSingleObject +0x154 / +0x15b` → `+0x708` | 기다림을 시작하자마자 그 자리에서 쪽지를 편다 (이때 `KeInsertQueueApc` 는 `State ≠ 5` 라 안 깨움) | 신호등 앞에 서자마자 깃발을 보고 쪽지부터 편다 |

- 신호가 먼저인 경우 결과서: "B 는 이미 신호를 먹었고 (`WaitStatus=0`), 그 뒤 쪽지만 돈다."
- 실제 어느 쪽이 먼저인지는 계산대가 둘 이상이라 **런타임** 이다.
- PE_emulator: 겹침 처리가 없으면 더해야 한다. 다만 CPU 하나·순서 고정이면 덜 중요하다 `[추측: 에뮬레이터 범위]` (run 문서 표 2(b)).

### 2.9 B 가 다시 돌 때 — 쪽지는 `KiSwapThread` 가 직접 편다 (질문 3(a))

요청서는 "소프트웨어 인터럽트 `KiApcInterrupt` 로 펴나, 직접 펴나" 를 물었다. 답은 **직접** 이다 `[정적]`.

#### `SwapContext` 꼬리 — "지금 펴도 돼" 를 `AL` 로 알려 준다 `[정적]`

- `+0x5c1` `KernelApcPending == 1` 인가.
- `+0x5ca ~ +0x5d0` 옛 IRQL 과 `SpecialApcDisable` 이 **둘 다 0** 인가.
- 모두 맞으면 → **`+0x5e6 SETZ AL`** (AL = 1).
- 하나라도 아니면 → `+0x5de HalRequestSoftwareInterrupt(1)` 로 APC 인터럽트를 요청하고 AL = 0.
- 비유: 교대 담당이 자리를 넘겨주며 "책상에 깃발 있고, 지금 방해 금지도 아니고, 가장 센 표지도 없네 — **지금 펴**" 라고 쪽지 한 장 (`AL`) 을 건넨다.

#### `KiSwapThread` 돌아온 뒤 — 한 걸음씩 `[정적]`

| 줄 | 하는 일 |
|---|---|
| `+0x243 / +0x248 KiSwapContext` → `+0x24b ESI = AL` | 교대에서 돌아옴. "지금 펴" 쪽지를 `ESI` 에 |
| **`+0x25c RDI = [R13+0xc8]`** | **결과 칸 (`0x100`) 을 쪽지 펴기 전에 읽어 둠** (2.2 줄 3) |
| `+0x263` | `TimerActive` 떼기 (run78) |
| `+0x274 TEST SIL,SIL / +0x277 JNZ +0x2ca` | 펴도 되면 → |
| `+0x2ca ~ +0x2e3` | IRQL 1 (APC 단계) 로 |
| **`+0x2ee / +0x2f3 KiDeliverApc(0, 0, 0)`** | 쪽지 펴기 |
| `+0x2f3 JMP +0x279` → `+0x28e` → **`+0x29a RAX = RDI`** → `+0x2ac RET` | 읽어 둔 `0x100` 을 그대로 돌려줌 |

- 그래서 `0x100` 은 쪽지와 **상관없이** 그대로 위로 올라간다 → `KeWaitForSingleObject +0x860 / +0x891` 에서 처음부터 다시 (2.11).

#### `KiDeliverApc` — 특별 쪽지 길 `[정적]`

1. `+0xa5` 커널 쪽지 목록 → `+0x10a` 쪽지 하나.
2. `+0x111` `NormalRoutine` · `+0x115` `KernelRoutine` 을 읽는다.
3. **`+0x135 TEST / +0x138 JZ +0x2b1`** — `NormalRoutine 0` 이면 특별 쪽지 길.
4. 목록에서 떼고 · APC 단계로 · **`+0x4f7 KernelApcInProgress |= 2`** (쪽지 펴는 중).
5. 알려진 루틴 몇 개와 비교한 뒤, 마지막 **`+0x76f` 간접 호출** (`guard_dispatch_icall` `0x1406b77f0`).
6. 돌아와 `+0x52b` `&= ~2` → `+0x535 JMP +0xac` (목록 다시 봄).

#### 간접 호출 — 여기만 줄이 끊긴다 `[공백 → 동적]`

- `+0x76f` 는 쪽지의 `KernelRoutine` 칸에서 읽은 주소로 가는 **간접 호출** 이다. `_calls.tsv` 에는 `indirect:cfg:?` 로 찍힌다.
- 이 쪽지면 `PspGetSetContextSpecialApc` 로 갈 것이다. 그건 **쪽지 칸에 적힌 값 (2.6 의 `KeInitializeApc`) 에서 읽은 짐작** 이지, 줄로 이어진 건 아니다.
- 실행으로 닫으려면 `nt!KiDeliverApc+0x76f` 에서 `rax` 를 봐야 한다 → 새 샘플이 있어야 한다 (**Q154**, run 문서 동적 5).
- 비유: 쪽지 펴는 담당이 쪽지에 적힌 내선 번호로 전화를 건다. 번호를 쓴 사람 (A) 은 알지만, 실제로 그 번호로 걸리는지는 통화 기록 (실행) 을 봐야 안다.

#### 그 밖에 커널 쪽지를 펴는 자리 (이름만, 결과서)

- `KiExitDispatcher +0xa2` · `KiExitThreadWait +0x242` (커밋 전 깨움) · `KeWaitForSingleObject +0x708` · `+0xb82` · `+0xefc` · `KiApcInterrupt` · `KiCheckForKernelApcDelivery +0x2d` · `KiCheckForThreadDispatch +0x61`.

#### PE_emulator 에게

- "깨난 직후 쪽지 확인" **하나로 합쳐도 된다** (결과서 규칙 7). `KiApcInterrupt` 소프트웨어 인터럽트는 건너뛰어도 된다.
- 단 순서는 지킨다: **결과 칸을 먼저 읽고 → 쪽지를 펴고 → 읽어 둔 값을 돌려준다** (규칙 3).

### 2.10 쪽지 안 — B 가 자기 출발 메모를 고친다 (질문 3(b))

#### 어느 출발 메모인가 — 맨 아래 것 `[정적]`

- `PspGetSetContextSpecialApc +0x64 / +0x69 PspGetSetContextInternal(꾸러미, 1, &이벤트칸)` — B 의 문맥에서 돈다.
- 유저가 부른 거면 `+0xa4 RDI = [B+0x28]` (`InitialStack`) · `+0xb2 − 0x190` = B 의 **맨 아래 트랩 프레임**.
  - B 가 `NtWaitForSingleObject` 로 **syscall 해서 들어올 때 붙인 그 출발 메모** 다.
- run16 해설 2.6 의 "`InitialStack − 0x190`" 과 같은 자리다.

#### 새 말 — 스택 거슬러 오르기 (개인 서랍 자리 찾기)

- 출발 메모 (트랩 프레임) 에는 유저 레지스터가 **다 있지 않다.**
  - `Rax` · `Rcx` · `Rdx` · `R8 ~ R11` · `Rip` · `Rsp` · `EFlags` 등은 트랩 칸에 있다.
  - 비휘발 레지스터 (`Rbx` `Rbp` `Rsi` `Rdi` `R12 ~ R15` `Xmm6 ~ 15`) 는 B 가 커널 안에서 거쳐 간 함수들의 **커널 스택 프레임에 흩어져 저장돼 있다** `[정적]` (결과서: "B 가 `KeWaitForSingleObject` 등 커널 프레임 안에서 자는 동안").
- 그래서 `PspGetSetContextInternal` 은 꾸러미 비트 1 이 켜져 있으면 (`+0xb9`, 2.5 의 다섯째 인자 1) **B 의 커널 스택을 함수 하나씩 거슬러 올라간다** `[정적]`:
  - `+0x298` (ret `+0x29d`) `RtlVirtualUnwind2` 루프. 트랩 프레임에 닿을 때까지 (`+0x2af / +0x2b3`) `[추측: 닿는 조건]`.
  - 찾은 자리를 꾸러미의 **포인터 표** (`+0x80`) 에 모은다.
- [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) 2.3 의 **풀어 보기 (unwind)** 를 실제로 쓰는 자리다.
- 비유: B 의 **개인 서랍** 내용은 출발 메모에 다 붙어 있지 않다. B 가 관리사무소 안에서 창구를 하나씩 지나칠 때마다, 그 창구가 "내가 잠깐 쓸 테니" 하며 서랍 물건 몇 개를 **자기 선반 (커널 스택)** 에 맡아 두었다. 그래서 창구를 거꾸로 하나씩 되짚으며 "`Rbx` 는 이 선반, `Rsi` 는 저 선반" 하고 **목록** 을 만든다.
- 끝 조건 (`+0x2af`) 과 `+0xa8 TEST [RDI+8],1 / +0xac JNZ +0x360` 루프의 뜻은 안 풀었다 → **Q153 ⑥** (규칙 7 로 건너뛰므로 급하지 않음).

#### 고치기 — `PspSetContext` `[정적]`

- `+0x2c0 / +0x2c4` 넣기 (SET) 확인 → 검증 (CET·XSTATE, 이름만) → **`+0x3d2 SetContextState == 0`** (보통, 2.13) → **`+0x3ea / +0x3ef PspSetContext(트랩, 포인터 표, 컨텍스트, 모드)`**.
- `PspSetContext` 가 쓰는 칸 (체크 칸별):

| 체크 칸 | 갈림 줄 | 쓰는 줄 | 칸 |
|---|---|---|---|
| CONTROL | `+0x28 AND 0x100001 / +0x35` | `+0x3b ~ +0x5a` · **`+0x68`** · **`+0x76`** · `+0x87 ~ +0xa2` | `EFlags` (트랩 `+0x178`, 유저면 걸러서 IF 켬) · **`Rip` (`+0x168`)** · **`Rsp` (`+0x180`)** · `SegSs` / `SegCs` |
| INTEGER | **`+0xc4 AND 0x100002 / +0xce`** | **`+0xd4 / +0xd8`** · `+0xdc ~ +0x10f` | **`Rax` (`+0x30`)** · `Rcx` · `Rdx` · `R8 ~ R11` |
| 〃 | | `+0x11e ~ +0x1a3` | `Rbx` `Rbp` `Rsi` `Rdi` `R12 ~ R15` → **포인터 표가 가리키는 선반** |
| FLOATING_POINT | `+0x1ba` | `+0x1ca ~ +0x2c7` | `Xmm0 ~ 5` (트랩) · `Xmm6 ~ 15` (포인터 표) · `MxCsr` |
| DEBUG_REGISTERS | `+0x2d3` | `+0x2f2 ~ +0x35b` | 이름만 |

- **INTEGER 가 없으면** `+0xce JNZ +0x1a6` 로 `Rax` 칸을 **안 건드린다** `[정적]`. 요청서의 "`CONTEXT_INTEGER` 일 때만 `Rax`?" → **예.**
- 비유: run16 해설의 **게시 담당** 이 다시 나온다. 새 메모 맨 위 **체크 칸** 을 보고, 체크된 묶음만 출발 메모에 고쳐 붙인다. 개인 서랍 물건은 목록을 들고 선반마다 찾아가 바꿔 놓는다.

#### 끝 — 결과 칸 쓰고, A 깨우기 `[정적]`

1. `+0x2f8 EAX = 0` → **`+0x305 [꾸러미+0x5c] = 0`** — 결과 칸에 "성공".
2. `+0x32e` — 남이면 이벤트 주소를 `PspGetSetContextSpecialApc` 에 돌려준다.
3. **`PspGetSetContextSpecialApc +0x86 / +0x8b KeSetEvent(A 의 이벤트, 1, FALSE)`** — 남일 때만 (`+0x71` 포인터 ≠ 0). → **A 깸.**

- 쪽지 안에서 실패하면 (스택 거슬러 오르기 `+0x504`, 검증 `+0x39e` · `+0x58e` · `+0x3cc`) 그 코드가 결과 칸으로 간다 (2.14).

#### run16 (자기 길) 과 같은 손

- run16 의 메인 스레드도 `PspSetContextThreadInternal` → `PspGetSetContextSpecialApc` → `PspGetSetContextInternal` → **`+0x3ea PspSetContext` → `+0x68`** 로 `Rip` 을 고쳤다. 다만 쪽지 없이 **직접** 불렀다 (`+0x131`).
- 이번 남 길은 같은 손에 **쪽지 한 겹** 이 더 씌워진 것이다 `[정적]`.

#### 왜 B 가 직접 고치나 — 이 해설의 짐작

- 코드는 "왜" 를 말해 주지 않는다.
- 다만 개인 서랍 자리를 찾는 길이 **B 의 커널 스택을 거슬러 오르는** 일이다 (결과서: `+0x104 RtlpCaptureContext` 로 지금 문맥을 찍고 시작).
- 그러니 B 자신의 문맥에서 도는 쪽이 맞는 모양이다 `[추측: 이 해설의 짐작]`.

#### PE_emulator 에게

- "B 의 `Rip` 등을 바꿔 둠" — **맞음**. 단 **체크 칸에 있는 묶음만**, `Rax` 는 INTEGER 일 때만 (run 문서 표 3(b), 규칙 4).
- 스택 거슬러 오르기는 **건너뛰어도 된다** — 에뮬레이터는 레지스터를 스레드 문맥에 바로 들고 있다 (규칙 7).

### 2.11 B 는 처음부터 다시 기다리고, 신호가 있으면 먹는다 (질문 3(c) · 4(a))

#### 처음부터 다시 — 한 걸음씩 `[정적]`

1. `KiSwapThread` 가 `0x100` 을 돌려줌 (2.9) → `KiCommitThreadWait +0x225 ~ +0x23b` 손 안 대고 그대로 (run77).
2. `KeWaitForSingleObject +0x846` 돌아옴 → **`+0x860 CMP R14D,0x100`** → 맞으니 아래로.
3. IRQL 2 · `WaitIrql` 되살림 → **`+0x891 JMP +0x104`** — 처음부터. 같은 물건이다 (인자는 스택에 그대로).
4. 다시 `+0x154` 깃발 (`KernelApcPending`) 을 본다 — 쪽지를 이미 다 폈으니 **0**.
5. `+0x1a6 State = 5` → **신호 검사가 마감 검사보다 먼저** (run78).
   - 그 사이 신호가 왔으면 → **`+0x5f0` 에서 가져가고** `R14D = 0` 으로 끝.
   - 아니면 번호표를 다시 걸고 (`+0x7b7`) `KiCommitThreadWait` 로 다시 잔다 (`+0x841`).

#### 마감은 처음 그대로 `[정적]`

- 상대 마감은 `+0x3b6 ~ +0x3d7` 에서 **한 번만** 계산한다. 그 뒤 `+0x3de JMP +0x104` 가 **반복 지점 아래** 로 뛴다.
- 그래서 처음부터 다시 돌아도 **처음 계산한 절대 시각** 그대로다. "남은 시간으로 다시 계산" 이 아니다.
- 쪽지가 오래 걸려 그 시각이 지났으면 → `0x102` 로 끝난다.
- [run78 해설 2.5](run78-thread-timer-해설.md) "반복해도 안 늘어난다" 와 같은 줄이다.
- 비유: 알람 시계는 처음 맞춘 시각 그대로다. 쪽지를 펴느라 잠깐 일어났다고 알람을 다시 맞추지 않는다.

#### 신호가 있으면 먹는다 `[정적]`

- 다시 도는 `+0x104` 는 **평범한 기다림** 이다.
  - 자동 신호등: `+0x5f0 AND AL,7 / +0x5f4 JZ +0x645` → **`+0x645 [R12+4] = 0`** — 지나가며 빨강으로.
  - 뮤턴트: `+0x3e3 ~` 에서 주인 = B (run77).
- 결과서 grep: `KeWaitForSingleObject` · `KiCommitThreadWait` · `KiSwapThread` · `KiTryUnwaitThread` · `KeInsertQueueApc` · `KiDeliverApc` `.asm` 에서 `0x6a8` · `0x8000000` 을 찾으면 **0건**.
  - 하나 걸리는 `+0x3fd CMP EAX,0x80000000` 은 뮤턴트 재귀 한도다.
- 즉 기다림 코드에는 **"문맥이 들어왔다" 를 읽는 줄이 어디에도 없다.**

#### 왜 "먹는다" 인가 — 단계별로

1. `[정적]` 쪽지는 신호등 불을 안 건드린다 (2.7). 번호표만 뗀다.
2. `[정적]` B 는 `0x100` 을 들고 `+0x891 JMP +0x104` 로 **처음부터** 다시 들어간다.
3. `[정적]` 처음부터의 기다림은 평범한 길이다. 신호면 가져간다 (`+0x645`).
4. `[정적]` 그 길 어디에도 "메모가 바뀌었으니 다르게 하라" 를 보는 줄이 없다 (grep 0건).
5. 그래서 → 신호가 쪽지보다 먼저 왔든 (2.8 첫 줄), 나중에 왔든 (2.8 둘째 줄), B 는 **신호를 먹는다.**

- 비유: B 는 쪽지를 편 뒤 "처음부터 다시 확인" 도장대로 신호등으로 돌아간다. 그 사이 초록이 됐으면 평소처럼 지나가며 빨강으로 돌린다. 신호등 담당은 B 의 메모가 바뀌었는지 **모른다.** 알 방법도 없다.

#### 이 결론이 덜 닿는 곳 `[공백]`

- `KeWaitForMultipleObjects` · `KeDelayExecutionThread` (Sleep) 의 `0x100` 반복은 안 읽었다 → **Q153 ④** (Q145 ② 와 묶을 만함).
- test.exe 는 `WaitForSingleObject` 만 쓴다 (결과서).

#### PE_emulator 에게

| 지금 우리 | 판정 | 왜 |
|---|---|---|
| 기다림 이어짐 | **반쯤 맞음** | 같은 물건·같은 절대 마감으로 이어지긴 한다. 하지만 그 사이 **한 번 나오고** (`0x100`, 번호표 떼임), 다시 **검사** 한다 |
| 결과 버림 | **맞음** | `0x100` 은 버리고 다시 돈다. 단 최종 결과는 **다시 한 기다림의 결과** 다 (2.12) |
| 신호 안 먹음 | **틀림** | 먹는다. 자동 신호등 0, 뮤턴트 주인 = B |

### 2.12 유저 `RAX` 는 기다림 결과 — 누가 나중에 쓰나 (질문 4(b)(c))

요청서의 질문 그대로: 출구에서 `+0x25d` 와 `+0x2d3 KiRestoreSetContextState` 중 **어느 쪽이 나중에 쓰나.**
보통 직원 답: **`+0x25d`** 다 `[정적]`.

#### 값이 지나가는 줄 `[정적]`

| 어디서 | 줄 | 무엇 |
|---|---|---|
| `KeWaitForSingleObject` | `+0x2c4 MOV EAX,R14D` | 기다림 결과 (신호 `0`, 마감 `0x102`, 에러 코드) |
| `NtWaitForSingleObject` → `KiSystemServiceStart` | `+0x193 CALL RAX` (ret `+0x195`) → `+0x1a0` 출구 | `RAX` 그대로 |
| 〃 | `+0x1fb` `UserApcPending` 0 → `+0x202 JZ +0x25d` | 유저 쪽지 없음 |
| 〃 | **`+0x25d MOV [RBP-0x50],RAX`** | **출발 메모 `Rax` ← 기다림 결과** (쪽지가 쓴 `Rax` 를 덮음) |
| 〃 | **`+0x291 TEST [RCX],0x8000000 / +0x297 JZ +0x2d8`** | 헤더 비트 27 꺼짐 (보통) → `KiRestoreSetContextState` **건너뜀** |
| 조각 `FUN_1406c20cc` | `+0x540 RAX = [RBP-0x50]` | 답 봉투 |
| 〃 | `+0x544 R8 = [RBP+0x100]` (`Rsp`) · `+0x552 EDX = 0` · `+0x554 ~ +0x568 Xmm0 ~ 5 = 0` | |
| 〃 | **`+0x56c RCX = [RBP+0xe8]`** (출발 메모 `Rip`) · **`+0x573 R11 = [RBP+0xf8]`** (`EFlags`) | 돌아갈 문 쪽지 = **새 `Rip`** |
| 〃 | `+0x58a RSP = R8` · `+0x5b8 SWAPGS` · **`+0x5bb SYSRET`** | 나감 |

- 비휘발 `Rbx` `Rdi` `Rsi` `Rbp` 는 `+0x1a0 ~ +0x1ae` 에서 트랩 칸을 읽어 되살린다. 그 칸 (또는 포인터 표가 가리킨 선반) 을 쪽지가 고쳤다면 그 값이다 (결과서).

#### 왜 `+0x25d` 가 나중인가 — 단계별로

1. `[정적]` 쪽지 안 `PspSetContext +0xd8` 가 출발 메모 `Rax` 칸에 쓴다 (INTEGER 일 때, 2.10).
2. `[정적]` 그 쪽지는 `KiSwapThread` 안에서 펼쳐진다. `KiSwapThread` 는 `KiCommitThreadWait` → `KeWaitForSingleObject` → `NtWaitForSingleObject` 아래, 즉 **`+0x193 CALL RAX` (서비스) 가 아직 안 끝난 동안** 이다.
3. `[정적]` 서비스가 끝나고 돌아온 **뒤** `+0x25d` 가 같은 칸에 기다림 결과를 쓴다.
4. `[정적]` 헤더 비트 27 이 꺼져 있으면 그 뒤로 이 칸을 다시 쓰는 길이 없고, `+0x540` 이 읽는다.
5. 그래서 → 유저 `RAX` = **기다림 결과**. 넣은 문맥의 `Rax` 는 사라진다.

- [run77 해설 2.13](run77-thread-wake-해설.md) "서비스 안에서 그 칸에 써도 소용없다" 와 **같은 이야기** 다. 이번엔 서비스 안에서 쓴 게 **쪽지** 였을 뿐이다.
- 갈림 조건은 "문맥을 넣었나" 가 아니라 **"이 직원이 syscall 출구를 타나 + 헤더 비트 27"** 이다 (결과서).

#### `RCX` · `R11` 은 신경 안 써도 된다 `[정적]`

- `SYSRET` 이라 `RCX = 새 Rip`, `R11 = 새 EFlags`, `RSP = 새 Rsp`. 넣은 문맥의 `Rcx` · `R11` 값은 **안 쓰인다.**
- `RDX` · `R9` · `R10` · `Xmm0 ~ 5` 는 0 이다.
- 호출 규약상 유저가 syscall 뒤 `RCX` · `R11` 을 믿지 않으니 괜찮다 (run 문서 표 4(c)).

#### 하나 더 — "신호라 안 자고 끝남" 이어도 반환값은 레지스터 `[정적]`

- run 문서 "칸 이름" 메모: 기다림 결과는 칸이 아니라 **반환 레지스터** 로 본다.
- 신호라 안 자고 끝나는 길은 `WaitStatus` 칸을 **안 쓰고** `R14D = 0` → `+0x2c4 MOV EAX,R14D` 다.
- 이번 길에서 B 의 `WaitStatus` 칸엔 `0x100` 이 남아 있을 수 있다. 그래도 유저 `RAX` 는 `0` 이다.
- [run77 해설 2.15](run77-thread-wake-해설.md) "`WaitStatus` 칸에 옛 값이 남는 길" 과 같은 꼴이다.

#### 비유

- 퇴장 담당이 B 를 내보낼 때:
  - **답 봉투** (`RAX`) 에는 신호등 결과 ("0번 물건 받음" 이나 "알람 울림") 를 넣는다. A 가 메모에 적어 준 숫자는 이미 덮였다.
  - **돌아갈 문 쪽지** (`RCX`) 는 출발 메모의 `Rip` 칸에서 꺼낸다. 그 칸은 쪽지가 고쳤으니 **새 자리** 다.
- 그래서 B 는 새 자리에서, 손에는 신호등 결과를 쥐고 시작한다.

#### PE_emulator 에게 (규칙 6)

- `RAX` = **마지막 기다림의 반환값**. "결과 버림" 이 "`RAX` 를 아무 값으로" 라면 **틀림** (run 문서 표 4(b)).
- `RCX` = 트랩 `Rip` (새 값), `R11` = 트랩 `EFlags`, `RSP` = 트랩 `Rsp`. `RDX` · `R9` · `R10` · `Xmm0 ~ 5` 는 0 으로 둬도 된다.

### 2.13 헤더 비트 27 길은 Pico 쪽만 — `SetContextState` (질문 4(b'))

#### 새 말 — `SetContextState` (메모 보관 봉투) 와 헤더 비트 27

- `_ETHREAD.SetContextState` (`+0x6a8`) = `_CONTEXT` 를 가리키는 칸 (PDB) `[정적]`.
- 이 칸이 **있으면** (0 이 아니면), 쪽지 안에서 출발 메모를 **그 자리에서 안 고친다** `[정적]`:
  - `PspGetSetContextInternal +0x3da JNZ +0x610` → (`+0x610` 유저 모드 · `+0x61a` 꾸러미 비트 1 · `+0x625 [B+3] & 8`) → `PspSetContextState`.
  - `PspSetContextState`: `+0x1a TEST [RCX+3],8` · `+0x21 R14 = [RCX+0x6a8]` · 이미 서 있으면 `+0x28 JNZ +0x77` · 처음이면 확장 문맥 준비 → **`+0x79 BTS.LOCK [RBP],0x1b`** (헤더 비트 27 켬) → `+0x9a RtlCopyContext` (봉투에 합쳐 둠).
- 그리고 **유저로 나가는 문에서** 적용한다 (아래 갈래 2).
- 비유: 인사 카드에 **"메모 보관 봉투"** 칸이 있는 직원이 있다.
  - 봉투가 있는 직원에게 새 메모가 오면, 출발 메모에 바로 붙이지 않고 **봉투에 넣어 둔다.**
  - 그리고 업무 카드 명판 귀퉁이에 **"봉투에 메모 있음" 표시** (비트 27) 를 켠다.
  - 나가는 문의 담당이 그 표시를 보면 봉투 메모를 출발 메모에 붙이고, **순간 이동** (`IRETQ`) 문으로 내보낸다.
  - 보통 직원은 봉투 칸이 **비어 있다.** 그래서 새 메모를 바로 붙인다 (갈래 1).

#### 두 갈래 나란히 `[정적]`

| | 갈래 1 — 봉투 없음 (보통) | 갈래 2 — 봉투 있음 (Pico 쪽) |
|---|---|---|
| 쪽지 안에서 | `PspGetSetContextInternal +0x3ea PspSetContext` 가 출발 메모를 **그 자리에서** 고침 | `PspSetContextState` 가 봉투에 합쳐 둠, `+0x79` 비트 27 켬 |
| 출구에서 `Rax` 칸 | **`+0x25d`** 기다림 결과 — **나중에 쓰는 쪽** | `+0x299` 기다림 결과 → `KiRestoreSetContextState +0x72` 봉투 읽기 → `+0x90 KeContextToKframes` → **`KxContextToKframes +0xc9` · `+0xd9 / +0xdd` 문맥 `Rax`** (INTEGER 면) — **나중에 쓰는 쪽** |
| `Rcx ~ R11` | 새 `Rip` · 새 `EFlags` (`SYSRET` 이 채움) | `+0x29f ~ +0x2cf` 에서 0 으로 → INTEGER 면 문맥 값 |
| 비트 27 | 꺼진 채 (`+0x291 / +0x297` 건너뜀) | `KiRestoreSetContextState +0xa8 BTR` 로 끔 |
| 문 | `+0x5bb SYSRET` | 조각 `+0x393 RAX = [RBP-0x50]` · `+0x3c8 SWAPGS` · **`+0x3cb IRETQ`** (`KiKvaShadow` 면 `+0x3b1 JMP KiKernelExit`) |
| 유저 `RAX` | **기다림 결과** | INTEGER 넣었으면 **문맥 `Rax`**, 아니면 기다림 결과 |

- `KiRestoreSetContextState` 는 `+0x2d3` 으로 불리고 **돌아오지 않는다** (ret `+0x2d8` 은 실제로 안 돌아옴).
- `IRETQ` 가 둘이다 (flow-writer 보탬 ⑤): `+0x12d TEST byte [RBP+0xf0],1` (트랩 `SegCs` 하위 비트) `/ +0x134 JZ +0x3cd` 로 갈린다.
  - 유저로 갈 때: 조각 `FUN_1406aecef` 의 `+0x393` · `+0x3c8 SWAPGS` · **`+0x3cb IRETQ`**.
  - 커널 모드로 돌아갈 때: `+0x401 MOV RAX,[RBP-0x50]` · `+0x416 IRETQ` (SWAPGS 없음).
- 비유: 같은 출발 메모 `Rax` 칸에 **두 사람이 차례로** 쓴다. 보통 직원은 퇴장 담당 (`+0x25d`) 이 마지막이고, 봉투 직원은 봉투 붙이는 담당 (`KxContextToKframes`) 이 마지막이다.

#### 어느 갈래가 보통인가 — 단계별로

1. `[정적: 전수 grep]` 헤더 비트 27 을 **켜는** 곳은 바이너리 전체에서 **`PspSetContextState +0x79` 하나** 다. 끄는 곳은 `KiRestoreSetContextState +0xa8`.
   - `or …, 0x8000000` 꼴 6곳은 다른 구조체로 읽힌다 `[추측: 이름만 보고 가름]`.
2. `[정적]` `PspSetContextState` 에 오는 건 `SetContextState ≠ 0` 인 직원뿐이다 (`PspGetSetContextInternal +0x3d2 / +0x3da`).
3. `[정적: 전수 grep]` `SetContextState` 칸에 **값을 넣는** 곳은 **`PspAllocateThread +0x2caa31` 하나** 다 (`ExAllocatePool2 … 'PsSc'`).
   - 거기 오는 길: `+0x5b3 JZ` (스택 인자 `[RSP+0x70] == 0`) → 가게 `PicoContext ≠ 0`.
   - 풀고 칸을 되쓰는 곳: `PspExitThread +0x85f` · `PspAllocateThread +0x2ca940` (만들기 실패 되돌림). 나머지 `+1704` 꼴은 다른 구조체다.
4. 그래서 → **보통 Win32 직원의 `NtSetContextThread` 는 갈래 1** 이다 `[정적]`.
5. `[관찰]` run44 사진 (메인 스레드 `ffff828598445080`, flow-writer 대조):
   - `SetContextState` — K2 ~ P2 일곱 장 모두 `(null)`.
   - 헤더 첫 DWORD — K2 `0x00200006` · K3 ~ P2 `0x00a00006` → **비트 27 일곱 장 모두 꺼짐.**
   - 초기화 동안 메인 스레드는 갈래 1 이다. 보통 갈래와 맞다.

- 전수 grep 은 어떻게 했나: `run_ghidra.sh refs` 는 구조체 칸 오프셋을 못 찾는다. 그래서 결과서가 `llvm-objdump-14` 로 ntoskrnl 코드 전체를 레포 밖 scratchpad 에 풀어 `[reg + 1704]` · `bts/btr …, 27` · `or …, 0x8000000` **꼴** 로 찾았다. **꼴 기준이라 하한이다** (run 문서 방법).
- 아직 확인 아님:
  - **Worker** 는 `main` 뒤라 사진 밖이다 → **Q104** 동적 1 (싸다, 새 샘플 없이).
  - `NtCreateThreadEx` 가 `PspAllocateThread` 의 그 스택 인자에 **늘** 0 이 아닌 값을 넘기는지 — 결과서는 `NtCreateThreadEx +0x14c LEA R14,[RSP+0x70]` · `+0x282` 한 갈래만 봤다 `[공백]` → **Q153 ②**.
- `PspAllocateThread` 의 이 갈림은 [run11 해설 2.4](../02_sprint/run11-allocate-thread-해설.md#24-갈림길-작업대개인-안내판은-언제-만드나) 의 **"둘째 갈림길 — 보통 길인가"** 그 자리다 (스택 인자 0 이면 Pico 길 / 본뜨기 길).

#### PE_emulator 에게

- 갈래 2 (`SetContextState` 미룸 · `KiRestoreSetContextState` · `IRETQ`) 는 **건너뛰어도 된다** — Pico 전용 (결과서 규칙 7).

### 2.14 A 가 받는 값 (질문 5)

| 상황 | A 가 받는 값 | 줄 `[정적]` |
|---|---|---|
| **성공** (B 가 쪽지를 돌림) | **`0`** | `PspGetSetContextInternal +0x2f8 / +0x305` → `PspSetContextThreadInternal +0x154` → `+0x167` → `NtSetContextThread +0xc7` → `+0x91` |
| **B 가 끝나는 중** (`ApcQueueable` 꺼짐) | **`0xc0000001`** | `PspExitThread +0x4e3 KeRundownApcQueues` 의 `+0x1a BTR ApcQueueable` → 그 뒤 `KeInsertQueueApc +0x134 / +0x13b` FALSE → `PspSetContextThreadInternal +0x2c6` → `+0x33e` |
| 같은 가게 제한 (`RestrictSetThreadContext`) | `0xc000060a` | `NtSetContextThread +0x63` → `+0xd8` |
| B 가 시스템 스레드 / Pico 스레드 | `0xc0000008` | `NtSetContextThread +0x6f` · `+0xa4` → `+0x78` |
| 유저 호출인데 B 가 시스템 스레드 | `0xc0000030` | `PspSetContextThreadInternal +0xbe / +0xc6` → `+0x1c4` (결과서) |
| 쪽지 안에서 실패 | 그 코드 (결과 칸 `+0x5c` 로) | `PspGetSetContextInternal +0x305` (실패 자리 `+0x504` · `+0x39e` · `+0x58e` · `+0x3cc`) |

- B 가 끝나는 중이면 — [run79 해설 2.9](run79-thread-exit-해설.md) "8 — 쪽지 다 떼기" 의 그 `KeRundownApcQueues` 다. 퇴근 절차가 "쪽지 붙일 수 있음" 을 끈 **뒤** 에 온 쪽지는 못 들어간다.
- **이미 들어간 뒤** B 가 끝나는 사이를 막는 줄은 `+0x1f KiAcquireReleaseThreadLock` · `+0x2f KeLeaveCriticalRegionThread` (쪽지 펴기) 로 보이나, 본문을 안 읽었다 `[추측]` `[공백]`.
  - 커널 쪽지가 남아 있으면 `KeRundownApcQueues +0x96 ~ +0xb3 KeBugCheckEx(0x20)` 다.
  - 버려지면 A 가 영영 자나? — **Q153 ③**.
- 비유: A 의 결과 칸에는 B 가 쪽지 안에서 적은 값이 들어간다. B 가 이미 퇴근 중이라 책상에 쪽지를 못 붙이면, 창구가 바로 "실패" 를 돌려준다.

### 2.15 PE_emulator 에게 — 한 장으로

run 문서 "PE_emulator 쪽에서 쓸 것" 을 한 장으로 줄였다. 모두 `[정적]`.

| 지금 우리 | 진짜 | 판정 |
|---|---|---|
| 기다림 이어짐 | 같은 물건·같은 절대 마감으로 이어짐. 단 그 사이 `0x100` 으로 **한 번 나오고** (번호표 떼임) 처음부터 다시 **검사** | **반쯤 맞음** |
| 결과 버림 | `0x100` 은 버리고 다시 돎. 유저 `RAX` = **다시 한 기다림의 결과** | **맞음** (`RAX` 를 아무 값으로 두면 틀림) |
| 신호 안 먹음 | 먹음 (자동 신호등 0, 뮤턴트 주인 = B). "문맥이 들어왔다" 를 보는 줄 없음 | **틀림** |
| (빠짐) A 는 바로 돌아옴? | A 도 마감 없이 잔다 (`+0x2dd`). `0` 은 B 가 쪽지를 돌린 뒤 | **더해야 함** |
| 새 `Rip` 로 감 | 맞음 — 기다림이 **끝난 뒤에** 간다 (지금 바로가 아니다) | **맞음** |

#### 최소 규칙 여덟 (run 문서 그대로, 한 줄씩)

1. **A 쪽**: B 가 끝나는 중이면 즉시 `0xc0000001`. 아니면 새 메모를 A 쪽에 복사하고, A 전용 작은 신호등 (꺼짐) + B 에 특별 커널 쪽지 하나 → A 는 마감 없이 잔다. 결과 `0`.
2. **깨우는 조건**: B 가 `Waiting` · 커밋됨 · `WaitIrql == 0` · `SpecialApcDisable == 0` 이면 번호표 떼고 `WaitStatus = 0x100`, 준비 줄 (+2). **신호는 안 건드림.** 이미 깬 B 면 깃발만. 커밋 전이면 `Apc` 비트만.
3. **B 가 다시 돌 때**: 깃발이 서 있으면 쪽지 먼저. `WaitStatus` 는 **쪽지 전에** 읽어 두었다가 그대로.
4. **쪽지가 하는 일**: 체크 칸에 있는 묶음만 쓴다. 쓴 뒤 A 의 신호등을 켠다.
5. **B 는 `0x100` 이면 처음부터** — 신호 (있으면 먹음) → 마감 → 같은 절대 마감으로 다시 잠.
6. **유저로 돌아갈 때**: `RAX` = 마지막 기다림 반환값, `RCX` = 새 `Rip`, `R11` = 새 `EFlags`, `RSP` = 새 `Rsp`. "신호라 안 자고 끝남" 이어도 반환값은 레지스터 값 (`0`).
7. **건너뛰어도 되는 것**: 스택 거슬러 오르기, `SetContextState` 갈래 (Pico), CET·XSTATE·보안 컨텍스트·디버그 레지스터·WoW64, ETW, `KiApcInterrupt` ("깨난 직후 쪽지 확인" 하나로).
8. **유저 모드에서 돌던 B** (syscall 안이 아님): `RAX` 도 반영될 것으로 읽히나 줄 단위로는 안 따라갔다 `[공백]` → **Q153 ⑤**.

#### flow-writer 덧붙임 셋 (규칙에 넣을지는 PE_emulator 판단)

- ① 퇴근 쪽지와 "같은 규칙" 의 차이 두 칸 — 보통 커널 쪽지면 `KernelApcDisable` · `KernelApcInProgress` 도 0 이어야 깨운다 (2.7).
- ② 사진: 메인 스레드 `SetContextState` 일곱 장 `(null)`, 비트 27 일곱 장 꺼짐 `[관찰]` (2.13).
- ③ `KeInsertQueueApc +0x8d` 의 `0x140ab1f40` = `KeSpecialUserApcKernelRoutine`. 문맥 넣기 쪽지와는 다른 길 (2.7).

### 2.16 헷갈리기 쉬운 곳 — "정정" 둘 중 진짜는 하나

run 문서 "정정" 절엔 두 줄이 있다. 하나는 **진짜 정정**, 하나는 **정정할 게 없었다** 는 기록이다. 천천히 나눠 보자.

#### ① 진짜 정정 — run77 의 "비트 27 = 남이 문맥을 넣은 경우" `[추측]` → 틀림

- **전** (run77 덧붙임 ① 끝 · "결과서와 다르게 적은 곳" ⑤ 끝):
  - "`KiRestoreSetContextState` (`KiSystemServiceStart +0x2d3`) 는 출구에서 스레드 헤더 비트 `0x8000000` 이 설 때만 — **남이 문맥을 넣은 경우 (Q141e) 로 보인다** `[추측]`."
- **후** `[정적]`:
  - **보통 스레드에 남이 문맥을 넣어도 이 길은 안 탄다.**
  - 비트 27 을 켜는 곳은 `PspSetContextState +0x79` 하나, 거기 오는 건 `SetContextState` 가 있는 스레드뿐, 그 칸에 값을 넣는 곳은 Pico 쪽 `PspAllocateThread +0x2caa31` 하나 (결과서 전수 grep, 2.13).
  - 보통 스레드는 쪽지 안 `+0x3ea PspSetContext` 가 출발 메모를 그 자리에서 고치고 `SYSRET` 길로 간다. 유저 `RAX` 는 `+0x25d` 가 덮은 서비스 반환값.
- **같이 고친 오프셋**:
  - run77 호출 표·`RAX` 절의 "돌아오지 않고 `KiRestoreSetContextState +0x416 IRETQ` (앞 `+0x401 MOV RAX,[RBP-0x50]`)" → **유저로 가는 `IRETQ` 는 조각 `+0x3cb`** (앞 `+0x393` · `+0x3c8 SWAPGS`). `+0x416` 은 트랩 `SegCs` 가 커널일 때 쪽이다 (`+0x12d / +0x134`).
  - **"트랩 `Rax` 칸에서 `RAX` 를 읽는다" 는 결론은 그대로** 다. 읽는 줄 자리만 바뀌었다.
- 반영: run77 문서 맨 위 정정 안내 한 줄, 덧붙임 ① 끝과 호출 표 `+0x2d3` 줄에 `(run80 정정)` 표시.
- **[run77 해설](run77-thread-wake-해설.md) 을 읽을 때 고쳐 읽을 곳**:
  - 2.13 "서비스 안에서 그 칸에 써도 소용없다" 5 · 6번 — `KiRestoreSetContextState` 쪽은 이번에 닫혔다 (2.13 갈래 2). `KiInitiateUserApc` 쪽은 아직 `[공백]`.
  - 2.14 "run 문서가 고친 것" 의 "어떤 때 서나 — 남이 문맥을 넣은 경우 (Q141e) 로 보인다 `[추측]`" → **Pico 쪽 스레드만** `[정적: 전수 grep]`.
  - 2.14 "세 출구 나란히" 표 셋째 줄 (`KiRestoreSetContextState +0x401` · `+0x416 IRETQ`) → 유저로 갈 땐 조각 `+0x393` · `+0x3cb IRETQ`.
- 비유: run77 때는 명판 귀퉁이 표시 (비트 27) 를 보고 "남이 메모를 바꿔 넣으면 켜지는 표시겠지" 라고 짐작했다. 이번에 표시를 켜는 손을 바이너리 전체에서 찾아보니 **봉투가 있는 직원 (Pico 쪽) 에게만** 켜는 손이 하나 있었다. 보통 직원은 메모를 바로 붙이니 표시가 필요 없다.

#### ② 정정할 게 없었다 — "run16 정정"

- 결과서 맺음 "정정 두 가지" 의 첫 줄이 이랬다:
  - "run16 의 `PspGetSetContextSpecialApc +0x64 → PspGetSetContextInternal → KeContextToKframes` 에서 마지막 고리는 `PspSetContext` 다" — 결과서는 이걸 **"run16 정정"** 으로 적었다.
- 그런데 flow-writer 가 확인해 보니 **run16 문서와 flow.md 에는 이 틀린 사슬이 없다** `[정적]`:
  - run16 은 호출 표 (`PspGetSetContextInternal +0x3ea / +0x3ef PspSetContext`), 2.3 ⑤ 절, 호출 관계 (`+0x3ef PspSetContext +0x68`) 모두 처음부터 **`PspSetContext`** 로 적었다.
  - flow.md 2.3 `PspInitializeThunkContext` 8번 줄도 `+0x3ef PspSetContext` 다.
  - run16 의 `KeContextToKframes` 줄 (`KiContinuePreviousModeUser +0x422`, `KiContinueEx +0x1e0` "이름만 본 후보") 은 **`NtContinue` 얘기** 라 맞다.
  - (이 글을 쓰며 보니 [run16 해설](../02_sprint/run16-thunk-context-해설.md) 2.6 도 `+0x3ea` (ret `+0x3ef`) `PspSetContext` 로 적혀 있다.)
- **틀린 사슬은 오케스트레이터가 쓴 요청서 sast/76 질문 3(b) 문장에만 있었다.** 결과서는 그 문장을 보고 "run16 정정" 이라고 적은 것이다.
  - plan.md 결정 기록도 이 줄에 취소선을 긋고 "오케스트레이터 실수 정정" 으로 남겼다.
- 그래서 → **run16 문서·flow.md 는 고치지 않았다. 정정 안내도 안 넣었다** (run 문서 "run16 · run77 · run79 에 반영한 것": run16 "없음").
- 덤 — `KeContextToKframes` 는 그럼 어디서 쓰이나 `[정적]` (`_calls.tsv`, 부르는 곳 셋):
  - `KiRestoreSetContextState +0x90` — 이번 갈래 2 (봉투 붙이기).
  - `KiContinueEx +0x1e0` · `KiContinuePreviousModeUser +0x422` — `NtContinue` 길.
  - `PspGetSetContextInternal` 은 **안 부른다.**
  - [run16 해설](../02_sprint/run16-thunk-context-해설.md) 2.6 덤 "`KxContextToKframes` 와 `PspSetContext` 는 칸 대응이 똑같다 (쌍둥이 코드)" 를 떠올리면 헷갈림이 풀린다. **같은 일을 하는 손이 둘** 이고, 보통 문맥 넣기는 `PspSetContext` 쪽 손을 쓴다.
- 비유: 요청서에 "게시 담당 대신 다른 담당이 붙인다" 고 잘못 적어 보냈다. 결과서가 "옛 기록 (run16) 이 틀렸다" 고 답했지만, 옛 기록을 펴 보니 처음부터 **게시 담당** 이라고 맞게 적혀 있었다. 틀린 건 심부름 쪽지 (요청서) 하나뿐이었다.

### 2.17 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

- ① `State = 7` 줄 자리 — `KeInsertQueueApc` 몸이 아니라 `+0x218` 로 부르는 `KiEnterDeferredReadyState` 안 `+0x31` · `+0x41` · `+0x4e` (2.7 "조심").
- ② 보탬: 보통 커널 쪽지의 두 칸 (`KernelApcDisable` · `KernelApcInProgress`) — 결과서 "퇴근 쪽지와 문맥 넣기 쪽지는 같은 규칙 하나" 에 붙는 조건 (2.7).
- ③ 보탬: `KeInsertQueueApc +0x8d` 의 `0x140ab1f40` 이름 = `KeSpecialUserApcKernelRoutine` (결과서 `[공백]`) (2.7).
- ④ 보탬: 사진 대조 (`SetContextState` · 헤더 비트 27). 결과서 동적 후보 첫 줄의 **메인 스레드 몫** 은 이것으로 `[관찰]` (초기화 구간). Worker 몫만 남는다 (2.13).
- ⑤ 보탬: `KiRestoreSetContextState` 의 `IRETQ` 는 둘 — 유저 쪽 조각 `+0x3cb`, 커널 쪽 `+0x416`. run77 정정의 근거 (2.13 · 2.16).
- ⑥ 보탬: `KeInitializeApc +0x43 CMOVZ` — `NormalRoutine 0` 이면 모드를 0 (커널) 으로 강제. 결과서 "ApcMode = 0" 의 이유가 인자 0 하나만이 아니다 (2.3).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 | … run79 까지 | + run80 (7개 처음 뽑음, `PspSetContextThreadInternal`·`KiSignalThreadForApc`·`KiDeliverApc`·`PspSetContext`·`KiRestoreSetContextState`·`KiSystemServiceStart` 등 다시 읽기, `SetContextState`·헤더 비트 27 쓰는 곳은 ntoskrnl 전체 `llvm-objdump` grep) |
| 기준 — 사진 메모 | run79 줄까지 | + run80 줄: 메인 `SetContextState` (`02 ETHREAD`) · 헤더 첫 DWORD (`02c ETHREAD_RAW`) 다시 읽음, 새 실행 없음 |
| 2.2 "시스템 콜 입구·출구" — 표에 안 든 길 | `+0x2d3 KiRestoreSetContextState` (스레드 플래그 `0x8000000`) | + 켜는 곳은 `PspSetContextState +0x79` 하나, `SetContextState` 있는 Pico 쪽만 `[정적: 전수 grep]`, run44 메인 일곱 장 꺼짐 `[관찰]` |
| 2.2 같은 절 run77 `RAX` 줄 | `KiRestoreSetContextState +0x416 IRETQ` (앞 `+0x401`) | **(run80 정정)** 유저로는 조각 `+0x3cb` (앞 `+0x393`), `+0x416`·`+0x401` 은 트랩 `SegCs` 커널 쪽 |
| 2.2 같은 절 새 줄 둘 | — | `KiRestoreSetContextState` 몫 닫힘 (`+0x299` → `+0x90 KeContextToKframes` → `KxContextToKframes +0xc9 / +0xd9 / +0xdd`, `KiInitiateUserApc` 는 그대로 `[공백]`) · 보통 스레드에 남이 넣어도 이 길 아님, 유저 `RAX` = 서비스 반환값 `[정적]` |
| 2.3 `PspInitializeThunkContext` 8번 | 현재 스레드라 APC 없이 … `PspSetContext +0x68` | + (run80) `+0x3d2 SetContextState == 0` 일 때 `+0x3ea PspSetContext`, 메인 `SetContextState` 일곱 장 `(null)` `[관찰]`, 남이면 3장 새 절로 |
| 3장 "기다림·깨움" 절 | 다시 검사는 `0x100` 하나 | + "(APC 로 깨는 예 → 아래 '남의 스레드에 문맥 넣기', run80)" |
| 3장 "스레드 끝내기" 절 남 끝내기 줄 | 중간 한 걸음은 `[추측]` | + run80 에서 일부: `KiSignalThreadForApc` = `KeInsertQueueApc` 와 같은 몸 + 보통 APC 두 칸, `KiDeliverApc +0x21b KiSchedulerApc` 직접 → `+0x241 JMP +0xac` |
| 3장 **새 절** "기다리는 남의 스레드에 문맥 넣기" | — | 비유 (급한 쪽지·택배) + 트리 + `[정적]` 다섯 + `[정적: 전수 grep]` 하나 + 사진 `[관찰]` + 동적 남은 것 |
| 4장 run77 정적 줄 (Q145) | 트랩 `Rax` 채우는 안쪽은 **Q141e** | + (일부 해결 → run80, `KiInitiateUserApc` 남음) |
| 4장 run79 정적 줄 | 대기열 번호 없음 | **Q147** + ① 일부 해결 (run80) |
| 4장 새 줄 셋 (run80) | — | 실행 확인 (1 Worker → Q104, 2 ~ 9 새 샘플) · 정적 남은 것 ① ~ ⑥ · 결과서와 다르게 적은 곳·보탠 것·run77 정정 |
| 5장 | — | run80 줄 |

태그가 바뀐 곳:

- run77 `[추측]` "헤더 비트 27 = 남이 문맥을 넣은 경우" → **틀림**, Pico 쪽만 `[정적: 전수 grep]`.
- run16 · flow.md 2.3 "남이면 APC 를 걸고 기다림" (이름만) → 쪽지 거는 줄·기다리는 줄·깨우는 줄까지 `[정적]`.
- run77 `[공백]` "`KiRestoreSetContextState` 안에서 `Rax` 를 무엇으로" → `KxContextToKframes +0xd9 / +0xdd` (INTEGER 면 문맥 `Rax`) `[정적]`.
- run79 `[추측: 중간 한 걸음]` → 깨우기 규칙·배달 자리까지 `[정적]` (남은 몫은 Q153 ①).
- PE_emulator `[추측]` 셋 → 반쯤 맞음 · 맞음 · 틀림 `[정적]`.
- 메인 스레드 `SetContextState` · 헤더 비트 27 → `[관찰]` (사진).

### 짐 지도 · run77 · run79 문서에서 바뀐 것

- 짐 지도 (`baggage.md`): **새 짐 칸 없음, 2장 숫자 그대로.** 짐 지도 바퀴가 아니다 — test.exe 는 `SetThreadContext` 를 안 부르고, 남의 문맥 넣기는 은퇴 뒤 일이라서다. 이미 있는 줄에 근거만 붙었다.
  - 3.13 `+0x6a8 SetContextState` — 값을 넣는 곳 `+0x2caa31` 하나, 풀고 되쓰는 곳 둘, 읽는 곳 셋 (`PspGetSetContextInternal +0x3d2` · `PspSetContextState +0x21` · `KiRestoreSetContextState +0x72`), 사진 일곱 장 `(null)`.
  - 3.14 `+0x0 Header*` — 첫 DWORD 비트 27: 켜는 곳 하나 · 끄는 곳 하나 · 보는 곳은 유저로 돌아가는 출구들. 사진 일곱 장 꺼짐.
  - 3.14 `+0x288 SchedulerApc` — 보통 커널 쪽지라 깨울 때 두 칸 더, 배달은 `KiDeliverApc +0x20e / +0x21b KiSchedulerApc` 직접.
  - 3.14 머리 메모 "(run80) 문맥 넣기 줄" (은퇴 뒤라 표에 안 넣음).
  - 판 목록 **서른여섯째판**, 기준 근거 목록·8장 두 표에 한 줄씩.
- run77 문서: 맨 위 정정 안내, 덧붙임 ① 끝과 호출 표 `+0x2d3` 줄에 `(run80 정정)`. `RAX` 절의 `[공백] → Q141e` 줄 · "다르게" ⑤ 끝 · 정적 ⑤ 에 "(→ run80 …)" (닫힌 몫 `KiRestoreSetContextState`·`KeContextToKframes`, 남은 몫 `KiInitiateUserApc`).
- run79 문서: "남의 스레드 끝내기" (a) 의 `[추측: 중간 한 걸음]` 줄 끝과 정적 ① 에 run80 이 닫은 몫·남은 몫.

### 큰 그림에서

- **지도 쪽**:
  - flow.md 2.3 의 `PspInitializeThunkContext` 8번 (초기화 때 메인 스레드가 **자기** 출발 메모를 바꿔 끼움) 과 이번 **남** 길이 같은 함수 `PspSetContextThreadInternal` 의 두 갈래로 나란히 읽힌다.
    - 자기: 쪽지 없이 바로 `PspSetContext`.
    - 남: 급한 쪽지 + A 기다림 + B 가 자기 문맥에서 `PspSetContext`.
  - 3장 "기다림·깨움" 의 `0x100` ("처음부터 다시 확인") 이 **누가, 언제** 찍는지 대표 예가 생겼다. run77 은 "쪽지가 와서 잠깐 깨우는 값" 이라는 일반 설명 `[추측]` 만 있었다.
- **요청 25 의 줄기**:
  - 직원이 들어오고 (run74), 계산대를 받고 (run75 · run76), 기다리고 깨고 (run77 · run78), 퇴근하고 (run79), **남의 출발 메모가 바뀐다** (이번).
  - run79 와 이번은 같은 모양이다: **남의 일은 쪽지로 맡기고, 실제 일은 그 직원이 자기 문맥에서 한다** `[정적]`. 다른 건 부른 쪽이 기다리느냐 (2.6) 와 쪽지 종류 (보통 / 특별, 2.7) 다.
- **PE_emulator 쪽**:
  - "기다림 이어짐 · 결과 버림 · 신호 안 먹음" 을 고칠 근거가 생겼다. 가장 큰 차이는 **"A 도 잔다"** 와 **"신호를 먹는다"** 다.
  - 퇴근 쪽지와 문맥 쪽지를 **한 규칙** 으로 묶을 수 있다 (두 칸 차이만, 2.7).
  - 유저 `RAX` 는 `+0x25d` 하나로 설명된다. run77 에서 짠 "출발 메모 `Rax` 칸에 쓰고 마지막에 그 칸에서 읽는다" 틀 그대로다.
- **다음**: **Q148a** — PE_emulator 요청 26 (CPU 예외를 유저 예외로 넘기는 길, 커널 쪽). 요청 26 ~ 30 이 끝나면 멈추고, 그 뒤 Q142 ~ Q147.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe **Worker** 의 `SetContextState` 가 0 인가, 헤더 비트 27 이 꺼져 있나. Worker 를 `PspAllocateThread` 의 어느 갈래가 만들었나 | 메인은 run44 사진 `[관찰]`. Worker 는 `main` 뒤라 사진 밖 | **Q104** 동적 1 (싸다, 새 샘플 없이: `dt nt!_ETHREAD <Worker> SetContextState` · `dd <Worker> L1`, `nt!PspAllocateThread+0x5ab` 에서 `poi(@rsp+0x70)` ≠ 0 기대) |
| 쪽지 넣기·깨우기 실제 값 (`KernelRoutine` · `NormalRoutine 0` · `r9 = 2`, `+0x240` 에서 `0x100` · `State 7` · 신호 그대로) | test.exe 에 `SetThreadContext` 가 없다 `[소스]` — VM 으로도 못 봄 | **Q154** (새 샘플 동적 2 · 3, 사용자 결정) |
| A 가 정말 `+0x2dd` 에서 자고, B 의 `KeSetEvent` 뒤에 `+0x2e2` 로 오나 | 같음 | **Q154** (동적 4) |
| `KiDeliverApc +0x76f` 간접 호출이 정말 `PspGetSetContextSpecialApc` 로 가나 | 간접 호출 `[공백 → 동적]` | **Q154** (동적 5) |
| `PspSetContext` 앞뒤 출발 메모 `Rip`·`Rsp`·`Rax` 실제 값, `+0x860 / +0x891` 다시 도나, `+0x645` 로 신호 먹나, `+0x25d` 앞뒤 `rax`, 유저 첫 명령에서 `rax = 0` · `rcx = 새 Rip` | 같음 | **Q154** (동적 6 · 7 · 8) |
| 신호와 쪽지가 겹칠 때 실제 순서 | 계산대 둘 이상, 런타임 | **Q154** (동적 9, 2 ~ 8 을 여러 번) |
| 다시 넣은 퇴근 쪽지가 어느 목록·어느 갈래로 `KiSchedulerApcTerminate` 에 닿나 (`KiSchedulerApc +0x1df` 의 `R13B`, `KiDeliverApc +0x392 ~ +0x443`) | 함수 둘의 줄만 남음 `[추측]` | 정적 **Q153 ①** (Q147 ① 남은 몫) |
| 보통 스레드가 늘 `PspAllocateThread` `[RSP+0x70] ≠ 0` 길로만 오나 | `NtCreateThreadEx +0x14c` · `+0x282` 한 갈래만 봄 `[공백]` | 정적 **Q153 ②** (Q104 동적 1 로도 닫힘) |
| 끝나는 중인 B 에 **이미 들어간** 문맥 쪽지 — 펴지나 버려지나 (버려지면 A 영영 잠?) | `KeLeaveCriticalRegionThread` · `KiFlushQueueApc` 본문 안 읽음 `[추측]` `[공백]` | 정적 **Q153 ③** |
| `KeWaitForMultipleObjects` · `KeDelayExecutionThread` 의 `0x100` 반복 | 안 읽음 `[공백]` | 정적 **Q153 ④** (Q145 ② 와 묶을 만함) |
| **유저 모드에서 돌던** B 에 문맥 넣기 — `KiApcInterrupt` → … → `IRETQ` 에서 `Rax` 가 문맥 값으로 가나 (덮는 `+0x25d` 가 없음) | 줄 단위로 안 따라감 `[공백]` | 정적 **Q153 ⑤** — PE_emulator 가 "돌고 있는 스레드" 를 다룰 때 필요 |
| 스택 거슬러 오르기 끝 조건 (`+0x2af / +0x2b3`), `+0xa8` 루프 뜻 | 줄기 밖 `[추측]` | 정적 **Q153 ⑥** (규칙 7 로 건너뛰므로 급하지 않음) |
| `KiInitiateUserApc` 안에서 출발 메모 `Rax` 를 무엇으로 | 이번 대상 아님 `[공백]` (run77 정적 ⑤ 의 남은 몫) | 대기열 항목 없음 |
| 줄기 밖 (CET · XSTATE · 보안 컨텍스트 · 디버그 레지스터 · WoW64 · ETW · `KiSendSoftwareInterrupt`) | 이름만 봤다 | 대기열 항목 없음 |

- 새 샘플 모양 (run 문서): 스레드 둘. B 는 `WaitForSingleObject(ev, INFINITE)` 중. A 가 `SetThreadContext(B, {Rip = X, Rsp = Y, Rax = 0x1234, ContextFlags = CONTROL | INTEGER})` → 그다음 A 가 `SetEvent(ev)`.
- 추천 멈춤점 (run 문서, **모두 그 프로세스 스레드일 때만** 조건 — 커널 멈춤점은 모든 프로세스에 걸린다):
  - `nt!KeInsertQueueApc` 입구 (`rdx == 1`, 부른 스레드 = A) · `nt!KeInsertQueueApc+0x240`
  - `nt!PspSetContextThreadInternal+0x2dd`
  - `nt!KiSwapThread+0x2ee` → `nt!KiDeliverApc+0x76f` (`rax`)
  - `nt!PspSetContext` 입구·뒤 (`dt nt!_KTRAP_FRAME @rcx Rip Rsp Rax`)
  - `nt!KeWaitForSingleObject+0x860` (`r14d == 0x100`) · `+0x891` · `+0x645`
  - `nt!KiSystemServiceStart+0x25d` · `+0x291`
- WinDbg 에서 기다림 함수 이름은 `nt!KeWaitForSingleObject` 다 (Ghidra 이름 `KeWaitForMutexObject` 아님).

### 요청 25 다섯 묶음 돌아보기

요청 25 는 PE_emulator 가 둘째 스레드까지 돌리면서 "커널은 스레드를 진짜로 어떻게 다루나" 를 다섯 묶음으로 물은 것이었고, 일곱 바퀴 모두 정적만으로 답했다 (값 대조는 run44 때 찍어 둔 사진으로만, 새 실행 없음). ㄱ 만들기는 [run74 해설](run74-thread-create-해설.md) — 둘째 직원은 다른 신청 창구로 들어와도 몸을 만드는 담당은 첫 직원 때와 같았다. ㄴ 넘기기·몫은 [run75 해설](run75-thread-switch-해설.md) (계산대를 넘길 땐 우선순위로 고른다) 과 [run76 해설](run76-thread-quantum-해설.md) (퀀텀은 깎지 않고, 벽시계 째깍 때 깃발만 꽂는다). ㄷ 기다림·깨움·알람은 [run77 해설](run77-thread-wake-해설.md) (깨우는 쪽이 그 자리에서 채운다) 과 [run78 해설](run78-thread-timer-해설.md) (마감은 시작 때 한 번 맞추고, 째깍마다 보고 잡무 단계에서 깨운다). ㄹ 끝내기는 [run79 해설](run79-thread-exit-해설.md) — 퇴근은 직원 본인이 자기 문맥에서 한다. ㅁ 남의 문맥 넣기는 이 글 — 급한 쪽지로 잠깐 깨워 본인이 자기 출발 메모를 고치고, 처음부터 다시 기다린다. 실행 확인은 VM 대기열로 넘겼고 (run74 는 **Q42**, run75 ~ run80 은 **Q104**), 이번 ㅁ 의 대부분은 새 샘플이 있어야 해서 **Q154** 로 따로 섰다.

---

## 5. 더 읽기

- run 문서: [`run80-thread-setcontext.md`](run80-thread-setcontext.md) — "PE_emulator 쪽에서 쓸 것" 질문별 표·**순서 한 장 (A·B 17단계)**·최소 규칙 여덟·flow-writer 덧붙임 ① ~ ③, 호출 지점 표, A 쪽 · 깨우기 · B 쪽 · 출구 · A 가 받는 값 절, 호출 관계 그림, **결과서와 다르게 적은 곳 · 보탠 것**, **정정** (run77 진짜 · run16 아님), 짐 지도 반영, 동적 아홉·정적 여섯
- 결과서: [`_workspace/sast/76_thread-setcontext_result.md`](../../_workspace/sast/76_thread-setcontext_result.md) — "한눈에" 그림, 질문별 답, A·B 시간 순서 표 (5번 `State = 7` 줄 자리는 run 문서 표로 고쳐 읽기), 지금 우리와 비교, 최소 규칙, 동적 후보 (맺음 "정정 두 가지" 의 첫 줄 "run16 정정" 은 run 문서 "정정" 둘째로 고쳐 읽기)
- 요청서: [`_workspace/sast/76_thread-setcontext.md`](../../_workspace/sast/76_thread-setcontext.md) (질문 3(b) 의 `→ KeContextToKframes` 사슬은 틀린 문장 — 2.16 ②)
- flow.md: [3장 새 절 "기다리는 남의 스레드에 문맥 넣기"](flow.md) · 2.2 "시스템 콜 입구·출구" 표 아래 run80 줄 · 2.3 `PspInitializeThunkContext` 8번 run80 줄 · 3장 "기다림·깨움"·"스레드 끝내기" run80 한 마디 · 4장 run80 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.13 `SetContextState` · 3.14 `Header*` · `SchedulerApc` · 3.14 머리 "(run80) 문맥 넣기 줄"
- 정정이 들어간 문서: [`run77-thread-wake.md`](run77-thread-wake.md) 맨 위
- 앞 이야기:
  - [run16 해설](../02_sprint/run16-thunk-context-해설.md) — 자기 출발 메모 바꿔 끼우기, 게시 담당 (`PspSetContext`), `KxContextToKframes` 쌍둥이
  - [run77 해설](run77-thread-wake-해설.md) — `0x100` 도장, 몸에 풀어 넣기, 퇴장 담당, 출구 `+0x25d` (2.13 · 2.14 는 이 글 2.16 ① 로 고쳐 읽기)
  - [run78 해설](run78-thread-timer-해설.md) — 진 번호표 떼기, 겹칠 때, 마감은 한 번
  - [run79 해설](run79-thread-exit-해설.md) — 남 퇴근시키기 쪽지, `KeRundownApcQueues`
  - [run75 해설](run75-thread-switch-해설.md) — 교대, `WaitStatus` 를 들고 돌아간다
  - [run56 해설](run56-syscall-args-해설.md) — 보통 출구 `SYSRET`, 답 봉투·돌아갈 문
  - [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) — 출발 메모·순간 이동, 개인 서랍, 풀어 보기
  - [run13 해설](../02_sprint/run13-resume-thread-해설.md) — 쪽지·깃발·쪽지 펴는 쪽
  - [run11 해설](../02_sprint/run11-allocate-thread-해설.md) — `PspAllocateThread` 의 보통 길 / Pico 길

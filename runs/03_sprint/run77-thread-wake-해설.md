# run77 thread-wake — 해설

> 이 글은 [run77 문서](run77-thread-wake.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.
>
> **고쳐 읽기 (run78)**: 2.9 끝 줄 ("여러 물건 기다림의 남은 표는 깨난 쪽이 나가면서 뗀다 `KiCommitThreadWait +0x2b2 ~ +0x32a`") 과 2.16 첫머리 ("`WaitRegister` 안쪽 비트와 번호표 안쪽은 로그에 없다") 는 run78 에서 정정됐다. → [run77 문서](run77-thread-wake.md) 맨 위 정정 안내 · [run78 문서](run78-thread-timer.md) "정정" 절 · [run78 해설 2.16](run78-thread-timer-해설.md#216-헷갈리기-쉬운-곳--run77-정정-둘) (ⓐ · ⓑ).

- 대기열: **Q141c1**: Q141c 의 첫 조각 — 깨움과 syscall 출구 (PE_emulator 요청 25 ㄷ-1·ㄷ-3) (`_workspace/plan.md`, 이번에 **끝**)
  - Q141c 는 함수가 많아서 시작할 때 둘로 나눴다. 깨움·출구 (이번, Q141c1) · 타이머 (다음, **Q141c2**).
  - 다음 항목: **Q141c2** → Q141d → Q141e → Q142. 그다음 **Q104 VM 단계** 앞에서 멈춘다 (사용자 결정 2026-11-12 "vm 제외하고 Q142 까지").
  - 동적 몫 여덟은 **Q104** 에 붙였다. 작은 정적 넷은 새 **Q145** (맨 뒤). 4절에서 다룬다.
- 근거:
  - run 문서 [`run77-thread-wake.md`](run77-thread-wake.md) (가장 중요)
  - 요청서 [`_workspace/sast/73_thread-wake.md`](../../_workspace/sast/73_thread-wake.md) · 결과서 [`73_thread-wake_result.md`](../../_workspace/sast/73_thread-wake_result.md) (판정 **분석성공**. 정적으로 못 닫은 다섯 — `WaitRegister.State` 이름, 깨난 스레드 준비 줄 앞/뒤, `KeWaitForMultipleObjects`, `KiRestoreSetContextState`/`KiInitiateUserApc` 안 트랩 `Rax`, `NtSetEvent` 의 `Wait` 인자. 첫째는 flow-writer 가 PDB 열거형으로 반쯤 닫음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 2.2 제목 · 2.2 "시스템 콜 입구·출구" 세 줄 · 2.2 `KiResumeThread` 한 줄 · 3장 "스레드 넘기기" 한 줄 + 새 절 "기다림·깨움과 syscall 출구" · 4장 고친 줄 다섯 + 새 줄 셋 · 5장
  - 짐 지도 [`baggage.md`](baggage.md): 3.14 머리 기다림·깨움 줄 · `WaitBlock*` 근거 · 7.2 한 마디 (새 짐 칸 없음)
  - run75 · run76 문서에 한 줄씩
- 이번 바퀴는 **정적만**이다. 값 대조는 run44 때 찍어 둔 **메인 스레드** 사진 일곱 장으로만 했다 (flow-writer, 새 실행 없음).
- **이번 바퀴에서 조심할 곳 둘** (2.14 · 2.15):
  - 결과서가 "로더 끝 `NtContinue` 는 `KiRestoreSetContextState` 의 `IRETQ` 길" 이라고 적은 곳을 run 문서가 고쳤다.
  - 신호로 바로 끝난 기다림은 `WaitStatus` 칸을 **안 쓴다**. 그래서 칸에 옛 값이 남을 수 있다. "칸 값 = 함수가 돌려준 값" 이 아닐 때가 있다.

이미 푼 말은 **링크만** 건다.

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- `_KTHREAD` = 인사 카드 첫 장 **업무 카드** → [run52 해설 "비유"](run52-kthread-create-해설.md#비유-그대로-이어-쓴다)
- APC = 직원 책상 **쪽지**, 이벤트 = **신호등** (초록 1 / 빨강 0, 칸 `SignalState`) → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- 기다림 블록 = 신호등 앞에 거는 **"나 여기서 기다려요" 번호표**, `KiTryUnwaitThread` = **어깨 두드리기** → [run13 해설 2.8](../02_sprint/run13-resume-thread-해설.md#28-깨우는-방식--신호등-초록--어깨-두드리기-정적)
- 신호등은 "상태" 로 남는다 (늦게 온 직원도 초록이면 그냥 지나감), 경주 → [run13 해설 2.9](../02_sprint/run13-resume-thread-해설.md#29-경주--부모가-먼저-초록불을-켜면-정적)
- `KeWaitForMutexObject` 로 찍혔지만 `KeWaitForSingleObject` (본명과 간판 이름) → [run13 해설 2.10](../02_sprint/run13-resume-thread-해설.md#210-이름-하나--kewaitformutexobject-로-찍혔지만-kewaitforsingleobject)
- 세마포어 = 횟수를 세는 신호등 → [run53 해설](run53-ethread-create-해설.md) ("묶음별로")
- CPU = **계산대**, `_KPRCB` = **계산대 장부** (공개 PDB 가 `+0x6d0` 에서 잘림), 업무 카드 칸 표, `State` 번호 → [run75 해설 2.3](run75-thread-switch-해설.md#23-무대-먼저--계산대-장부-prcb-와-업무-카드의-칸)
- 준비 줄 · "다음 차례" 자리 · 밀린 명단 (DeferredReady, `State` 7) · 줄 앞/뒤 규칙 → [run75 해설 2.4 · 2.10](run75-thread-switch-해설.md#210-줄에-다시-세우기--기본-맨-뒤-선점당했고-몫이-남은-직원만-맨-앞)
- `KiSwapContext` = 계산대 **교대**, `call` 이 "한참 뒤에" 돌아오는 이유 → [run75 해설 2.8](run75-thread-switch-해설.md)
- `WaitStatus` = 돌아올 때 들고 오는 **"왜 깨어났나" 한 줄 결과** → [run75 해설 2.9](run75-thread-switch-해설.md)
- `WaitReason` = 업무 카드 **"사유" 칸**, 교대 담당을 부르는 **문** 표, 디스패치 인터럽트 = **초인종** → [run76 해설 2.7 · 2.13 · 2.14](run76-thread-quantum-해설.md)
- IRQL = **"방해 금지" 단계 표지판**, 락 = **"작성 중" 자물쇠** → [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리) · [run11 해설](../02_sprint/run11-allocate-thread-해설.md)
- 트랩 프레임 = 직원 책상의 **출발 메모**, `IRETQ` = 출발 메모대로 **순간 이동** → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- `SYSRET` = 주머니 쪽지대로 나가는 **보통 출구**, 답 봉투 (`RAX`) → [run56 해설 2.10 · 2.11](run56-syscall-args-해설.md)
- 로더 끝 `NtContinue` 는 반환하지 않고 `IRETQ` → [run29 해설 2.3 · 2.11](../02_sprint/run29-continue-start-해설.md)
- 사진 지점 K2 ~ P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가), 메인 스레드 사진 일곱 장 → [run75 해설 2.12](run75-thread-switch-해설.md#212-사진과-맞춰-보기--메인-스레드-업무-카드-일곱-장)

**비유 한 가지 주의.**

- 이 글은 run75·run76 처럼 **CPU = 계산대, 스레드 = 직원** 으로 이어 쓴다.
- 기다리는 대상은 run13 해설의 그림을 그대로 쓴다. 이벤트 = **신호등**, 기다림 블록 = **번호표**, 깨우기 = **어깨 두드리기**.
  - 이번에 새로 나오는 뮤턴트는 **창고 열쇠** 로 부른다 (2.3).
- flow.md 3장 새 절은 같은 일을 **택배** 비유로 적었다 (택배함 = 기다림 줄, 이름표 = 기다림 블록, 택배 기사 = `KeSetEvent` 등).
  - 같은 일을 다른 그림으로 본 것일 뿐이다. 이 글에선 신호등·번호표로 통일한다.

### 한 줄로 먼저

- 진짜 커널은 **깨우는 쪽이 그 자리에서** 기다림을 채운다 `[정적]`.
  - 자동 재설정 이벤트는 깨우는 순간 0 (`KeSetEvent +0xd1`).
  - 뮤턴트는 놓는 쪽이 주인을 넘긴다 (`KeReleaseMutantEx +0x330` → `KiWaitSatisfyMutant +0x40`).
  - 깨난 직원의 `WaitStatus` 도 깨우는 쪽이 쓴다. 값은 번호표의 `WaitKey` (한 물건 기다림이면 0).
- 깨난 쪽은 **다시 묻지 않는다** `[정적]`. 받은 `WaitStatus` 를 그대로 돌려준다. 다시 보는 건 값이 `0x100` 일 때 하나뿐.
- 그래서 PE_emulator 의 "깨움 = 실행 가능 표시만, 깨난 쪽이 syscall 을 다시 물음" 은 **틀림** `[정적]`.
- 기다리러 들어갈 때 이미 신호면 **안 잔다**. 그 자리에서 가져간다 `[정적]`.
- 돌려준 값은 그대로 유저 `RAX` 가 된다. 출발 메모 `Rax` 칸에 적었다가 (`KiSystemServiceStart +0x25d`) 마지막에 그 칸을 읽어 (`+0x540`) `SYSRET` `[정적]`.
- 사진 (메인 스레드): 초기화 동안 자기 번호표 0번 (`WaitBlock[0]`) 으로 기다렸다 `[관찰]`. 마지막 사진의 `WaitStatus` 칸엔 `0x100` 이 남아 있었다 `[관찰]` — 칸에 옛 값이 남는 길로 읽힌다 (2.15).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 만들기                 (run9 ~ run12)
 └─ 첫 스레드는 KiSchedulerApc +0xf2 KeWaitForSingleObject 에서 잠든다          (run13)
      └─ 부모 NtResumeThread → … → KiResumeThread → KiTryUnwaitThread 로 깨움    (run13, 이번에 한 줄 더)
 └─ … ntdll 초기화 … 로더가 NtWaitForSingleObject 로 일꾼을 기다림               (run24)
 └─ main: WaitForSingleObject(t, INFINITE)    (sample/test.c:35, 지도 범위 밖)

 계산대 주고받기 — 모든 직원이 몇 번이고
   기다림으로 내놓기 → KiSwapThread                                            (run75)
   양보 · 몫 끝 · 선점                                                         (run75 · run76)
   기다림 안쪽 · 깨우기                                                        ← 이번
   마감 지난 기다림 (타이머)                                                    (Q141c2, 다음)
 syscall 출구 → SYSRET                                                         (run56, 이번에 RAX 줄 자세히)
```

- 이번 주제도 run75·run76 처럼 지도의 **한 단계가 아니다.**
  - 모든 직원이 초기화 내내 몇 번이고 다시 타는 길이다.
  - 그래서 flow.md 그림엔 안 넣고, 3장에 절 하나 ("기다림·깨움과 syscall 출구") 로 적었다.
- 지도와 맞물리는 곳은 셋이다.
  - **run13 의 첫 스레드 멈춤 풀기**: `KiResumeThread` 가 `KiTryUnwaitThread` 에 넘기는 값과, 깨난 첫 스레드가 받는 값 (2.9).
  - **run75 의 `KiSwapThread`**: 돌아올 때 `WaitStatus` 를 돌려준다고만 봤다. **그 칸을 누가 쓰나** 가 이번 답이다 (2.8).
  - **run56 의 syscall 출구**: 유저 `RAX` 가 정해지는 줄을 더 자세히 봤다 (2.13).

### 무엇이 궁금했나

한 문장으로: **깨울 때 결과를 누가 정하나 — 깨우는 쪽이 그 자리에서? 깨난 쪽이 다시 물어서?** 그리고 그 결과가 유저 `RAX` 까지 어떻게 가나.

- 계기: `../PE_emulator` 의 nt 엔진은 지금 이렇다 `[추측]` (요청서).
  - 기다림: 스레드를 "잠듦" 으로 표시하고 다른 스레드로 넘김.
  - 깨움 (`NtSetEvent`·`NtReleaseMutant` 등 팀원이 짠 syscall 몸): 기다리던 스레드를 **"실행 가능" 표시만**.
  - 깨난 스레드가 자기 syscall 을 **다시 물어** (조건 다시 검사) 결과를 받음.
- 진짜에 맞춰 달라는 요청 25 ㄷ-1 (깨움)·ㄷ-3 (syscall 출구). ㄷ-2 (타이머) 는 Q141c2.
- 질문은 여섯 (요청서를 줄임):

| # | 질문 | 이 글 어디서 |
|---|---|---|
| 1 | 기다리는 쪽 `KeWaitForSingleObject` → `KiCommitThreadWait`: (a) 이미 신호면? (b) 아니면 번호표·`State`·타이머·`KiSwapThread` (c) 깨난 뒤 그대로 돌려주나, 반복이 있나 | 2.5 · 2.6 · 2.7 |
| 2 | 깨우는 쪽 `KeSetEvent`: (a) 자동/수동 (b) `WaitStatus` 쓰는 줄 (c) 깨우는 함수 이름·준비 줄 (d) 반환값 | 2.8 · 2.10 |
| 3 | `KiTryUnwaitThread`: 번호표 떼기, `WaitStatus`, 경쟁 | 2.9 |
| 4 | `KeReleaseMutant`: (a) 몸 (b) 주인을 그 자리에서 넘기나 (c) 버려진 뮤턴트 | 2.11 |
| 5 | `KePulseEvent`: 몇을 깨우고 언제 0 으로 | 2.12 |
| 6 | syscall 출구: 돌려준 값이 유저 `RAX` 로 가는 길 | 2.13 · 2.14 |

- test.exe 가 실제로 쓰는 기다림은 **스레드 객체** 기다림이다 `[소스]` (`sample/test.c:35`).
  - 스레드 객체가 신호를 받는 쪽 (끝내기) 은 **Q141d** 에서 본다. 이번엔 이벤트·뮤턴트·공통 기다림 틀.
- 타이머 만료는 **Q141c2**. 이번엔 마감을 거는 줄만 이름·오프셋으로.

### 바퀴 전에 알던 것

- `[정적]` run75: `KiCommitThreadWait +0x220` → `KiSwapThread`. `KiSwapThread` 는 `_KTHREAD.WaitStatus` (`+0xc8`) 를 돌려준다 (`+0x25c` · `+0x29a`). 준비 줄에 꿰는 건 `KiDeferredReadySingleThread`.
- `[정적]` run13: `KiResumeThread` 가 `SuspendEvent.SignalState = 1` 을 직접 쓰고, 번호표 종류가 1 이면 `KiTryUnwaitThread` 로 깨운다. 멈춘 첫 스레드는 `KiSchedulerApc +0xf2 KeWaitForSingleObject` 로 잔다.
- `[정적]` run52·run53 표: `AbWaitObject` 를 쓰는 함수, `Preempted`·`AdjustReason` 을 쓰는 함수 목록.
- `[정적]` `pdb_aliases.py`: `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject`. `KiSystemServiceExit` = `KiSystemServiceStart +0x1a0` (함수로 안 잡힘).
- `[정적]` run56: 보통 출구는 `SYSRET`. 유저 `RAX` 는 `+0x391` 에서 출발 메모 `Rax` 칸에 두고 조각 `+0x34` 에서 읽는다.
- `[관찰]` run75 사진: 메인 스레드 P1 → P1a 사이 교대 5번이 `KiSwapThread` 식과 안 맞음 `[추측]` — 다른 길이 있다는 단서.
- `[추측]` PE_emulator: 깨움 = 표시만, 깨난 쪽이 다시 물음.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서 "호출 관계" 에서 대표만 남겼다.
가지의 `+0x..` 는 **돌아올 자리 (ret)**, 가지 밖 `+0x..` 는 그 줄 자리다. 번호 `(n)` 은 run 문서 번호 그대로. ★ 는 이 글 절.

```
[기다리는 쪽 — 유저 syscall]
KiSystemServiceStart
 └─ +0x195  NtWaitForSingleObject                                    (CALL RAX +0x193)
      └─ +0x6a   ObWaitForSingleObject
           ├─ (2) +0xbf   KeWaitForSingleObject (Ghidra KeWaitForMutexObject)  EDX = 6 UserRequest
           │    │  +0x104   ← 다시 도는 지점                                                  ★ 2.7
           │    │  +0x19f   번호표 = 자기 WaitBlock[0] · +0x1a6 State = 5 먼저                 ★ 2.5
           │    │  +0x205 ~ +0x213  신호? → 그 자리에서 가져감 → +0x251 State = 2 → 반환 (안 잠)
           │    │  +0x7b7 ~ +0x7c1  객체 줄 맨 뒤에 번호표 · +0x7d2 WaitBlockCount = 1           ★ 2.6
           │    ├─ (1) +0x846  KiCommitThreadWait
           │    │    │  +0xa3  이미 깨워졌거나 APC·Alert → 안 잠
           │    │    │  +0xbf  WaitRegister.State = 1 · +0x160 Preempted = 0
           │    │    ├─ (2) +0x1f7  KiInsertTimerTable           마감 있을 때 (Q141c2)
           │    │    └─ (3) +0x225  KiSwapThread                 ← 여기서 잠. 돌아오면 WaitStatus (run75)
           │    │  +0x849 R14 = 반환 · +0x860 0x100 이면 → +0x891 JMP +0x104                    ★ 2.7
           │    └─ +0x2c4 MOV EAX,R14D · RET
           └─ (3) +0xdf   ObfDereferenceObjectWithTag      → +0xdf MOV EAX,EBX · RET
 │  +0x25d  트랩 Rax = RAX · … · +0x391 마지막 저장                                            ★ 2.13
 │  +0x540 (조각 +0x34)  RAX = 트랩 Rax
 └─ ══▶ +0x5bb (조각 +0xaf) SYSRET

[깨우는 쪽]
KeSetEvent (Event, Increment, Wait)                                                            ★ 2.8
 │  +0x8d  SignalState = 1 먼저 · 옛 값 ≠ 0 이면 아무도 안 깨움
 │  자동: 번호표마다 (풀어 넣음) +0x469 State = 7 · +0x47d push PRCB+0x2d08 · +0x484 WaitStatus = WaitKey
 │        첫 성공 뒤 +0xd1 SignalState 1 → 0
 │  수동: 전부 (+0x2ec WaitStatus), 1 유지
 ├─ (1) +0x2cf  KiEnterDeferredReadyState                 수동, 커밋된 기다림
 ├─ (2) +0x6ab / +0x6d5  KiTryUnwaitThread (0x100)        WaitType 0 번호표만                    ★ 2.9
 └─ (3) +0x111  KiExitDispatcher                                                               ★ 2.10
      ├─ (1) +0x46   KiProcessThreadWaitList               PRCB+0x2d08 ≠ 0
      │    └─ +0x106  KiDeferredReadySingleThread
      ├─ (3) +0x1a2  KiQueueReadyThread(깨운 쪽 자신)       NextThread ≠ 0 이고 이전 IRQL < 2
      └─ (4) +0x1b1  KiSwapContext                          두 번째 넘기기 길

KeReleaseMutant
 └─ +0x2b  KeReleaseMutantEx                                                                   ★ 2.11
      │  +0xc0 SignalState + 1 → 1 이 되면 +0x15a OwnerThread = 0
      │  번호표마다 +0x306 WaitStatus = WaitKey
      ├─ (2) +0x335  KiWaitSatisfyMutant                     +0x40 OwnerThread = 깨운 스레드
      │  +0x192 SignalState − 1
      └─ (4) +0x206  KiExitDispatcher

KePulseEvent                                                                                   ★ 2.12
 │  +0x5b  옛 값 ≠ 0 → +0xaa · 아니면 +0x60 SignalState = 1
 ├─ (1) +0x9d / (2) +0x175 / (3) +0x1ed  KiTryUnwaitThread
 │  +0xaa  SignalState = 0 (늘)
 └─ (4) +0xd5   KiExitDispatcher
```

- 모두 `[정적]`. 실행해서 본 줄은 없다.
- 이번에 **처음 뽑은 함수가 16개** 다 (모두 depth 0). 새 지도는 `_map/KeSetEvent_*` (9개) · `_map/KeWaitForMutexObject_*` (2,861개).
  - `KeSetEvent` 지도는 깊이 1 이 8개뿐이고 대부분 공용 도구라 안 펼쳐졌다. 그래서 준비 줄로 가는 길 (`KiExitDispatcher` 아래) 은 결과서가 **이름으로** 따로 뽑았다.
- 이름 주의 둘:
  - PDB `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject` (rva `0x217eb0`). `.asm` 파일 이름은 `KeWaitForMutexObject`, WinDbg 는 `nt!KeWaitForSingleObject`. run13 해설 2.10 의 "본명과 간판 이름" 그대로다. 이 글은 본명 (PDB 이름) 으로 쓴다.
  - `KiSystemServiceExit` = `KiSystemServiceStart +0x1a0`. 출구 뒤쪽은 Ghidra 조각 `FUN_1406c20cc` (= `KiSystemServiceStart +0x50c`) 에 있다. 이 글은 함수 기준 (`+0x540`·`+0x5bb`) 으로 쓰고 조각 기준 (`+0x34`·`+0xaf`, run56 표기) 을 괄호로 붙인다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `│    │    └─ (3) +0x225  KiSwapThread   ← 여기서 잠`

- **누가**: `KiCommitThreadWait`. 기다림을 "확정" 하고 재우는 담당이다 (2.6).
- **어디서**: 부르는 자리 `+0x220`, 돌아올 자리 `+0x225`.
  - 멈추면 콜스택에 `nt!KiCommitThreadWait+0x225` 로 보일 자리다. 아직 안 봤다.
- **누구를**: `KiSwapThread`. run75 의 교대 준비 담당이다.
- **무엇을 위해**: 이 직원이 계산대를 내놓고 **잔다.**
  - 이 `call` 은 [run75 해설 2.8](run75-thread-switch-해설.md) 처럼 **한참 뒤에** 돌아온다. 누가 깨워서 이 직원 차례가 다시 올 때다.
  - 돌아올 때 들고 오는 값 = **`WaitStatus`** (run75 `+0x25c` · `+0x29a`) `[정적]`.
- 돌아온 뒤 `+0x225 MOV R15,RAX` → `+0x228 MOV RAX,R15` → `+0x23b RET`. **사이에 계산이 없다** `[정적]`. 받은 걸 그대로 위로 넘긴다 (2.7).

#### 줄 2: `KeSetEvent +0xd1  SUB [R15],1`

호출이 아니라 **명령 한 줄**이다. 그런데 "자동 재설정 이벤트를 누가 0 으로 만드나" 의 답이 이 줄이다.

- **누가**: `KeSetEvent` — **깨우는 쪽**.
- **무엇을**: `R15` 가 가리키는 칸에서 1 을 뺀다. 그 칸은 이벤트 `SignalState` 다. 같은 함수 `+0x8d MOV [R15],1` 이 먼저 1 을 적은 그 칸이다 `[정적]`.
- **언제 여기 오나**: 자동 재설정 루프에서 깨움에 **성공**했을 때만 (`+0x4b4 JMP +0xd1`). 실패면 `+0x4ae JZ +0xa8` 로 다음 번호표 `[정적]`.
- **그래서**: 한 명을 깨우자마자 **깨우는 쪽이 그 자리에서** 1 → 0. 깨난 직원이 나중에 와서 끌 일이 없다.
- 비유: 신호등 담당이 초록을 켜고, 한 명을 통과시키자마자 **자기 손으로** 빨강으로 돌린다.

#### 줄 3: `KeWaitForSingleObject +0x860  CMP R14D,0x100` · `+0x867  JNZ +0x2c4`

- **누가**: `KeWaitForSingleObject` — **깨난 쪽**. `KiCommitThreadWait` 에서 돌아온 직후다.
- **무엇을**: 받은 값 (`R14D`, `+0x849 MOV R14,RAX`) 이 `0x100` 인지 본다.
  - 아니면 → `+0x2c4` 로 가서 그대로 반환.
  - 맞으면 → 아래로 흘러 `+0x891 JMP +0x104` — **처음부터 다시** (2.7).
- **그래서**: "깨난 쪽이 다시 묻나?" 의 답은 **"`0x100` 일 때만"** 이다 `[정적]`.

### 2.3 무대 — 이번에 쓰는 칸과 새 말

뒤에서 오프셋이 나오면 이 절로 돌아오면 된다.

#### 새 말 — 기다릴 수 있는 물건의 머리 (`_DISPATCHER_HEADER`)

- 이번에 나오는 물건은 넷이다: **이벤트** (자동·수동), **세마포어**, **뮤턴트**, **스레드 객체**.
- `KeWaitForSingleObject` 는 물건 맨 앞 머리의 `Type` 칸으로 갈래를 고른다 `[정적]` (`+0x205 AND CL,0x7f` → `+0x208 CMP CL,2` 뮤턴트? / `+0x211 TEST EDX,EDX` 신호?).
- 기다릴 수 있는 커널 물건이면 이 머리로 시작하는 것으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 머리 칸 (PDB):

| 칸 | 이름 | 이 글에서 부르는 말 |
|---|---|---|
| `+0x0` | `Type` (비트 7 = 헤더 락) | 물건 종류 · 기둥 **"작성 중" 자물쇠** |
| `+0x4` | `SignalState` | **불** (신호 상태) |
| `+0x8` | `WaitListHead` | **번호표 줄** 머리 |

- 비유: 신호등 기둥에 붙은 **명판** — "종류 · 불 · 번호표 줄".
- 종류 번호는 PDB 열거형 `_KOBJECTS` 로 확인한 것만:

| `Type` | PDB 이름 | 이 글에서 |
|---|---|---|
| 0 | `EventNotificationObject` | **수동** 재설정 이벤트 |
| 1 | `EventSynchronizationObject` | **자동** 재설정 이벤트 |
| 2 | `MutantObject` | 뮤턴트 (창고 열쇠) |
| 5 | `SemaphoreObject` | 세마포어 |
| 6 | `ThreadObject` | 스레드 객체 (직원 자신) |
| 9 | `TimerSynchronizationObject` | 동기 타이머 (자동 이벤트와 같은 모양으로 처리됨) |

#### 새 말 — 자동 재설정 / 수동 재설정 이벤트

- **수동** (Notification, `Type 0`): 켜면 누가 끌 때까지 켜진 채다. 기다리던 직원은 **전부** 지나간다 `[정적]` (2.8 수동 루프).
- **자동** (Synchronization, `Type 1`): 한 명이 지나가면 **꺼진다.** **한 명만** 지나간다 `[정적]` (2.5 `+0x645`, 2.8 `+0xd1`).
- 비유:
  - 수동 = 보통 **신호등**. 초록이면 다 건넌다. 담당이 빨강으로 돌릴 때까지 초록.
  - 자동 = **한 명 통과하면 바로 빨강으로 돌아가는 개찰구 신호등**.
- 이번 바퀴의 핵심 질문이 바로 "자동의 '바로 빨강' 을 **누가** 하나" 다.
  - 이미 초록인데 직원이 왔으면 → 그 직원이 지나가면서 (2.5).
  - 잠든 직원이 있었으면 → **깨우는 쪽이** 그 자리에서 (2.8).

#### 새 말 — 뮤턴트

- 한 번에 한 직원만 가질 수 있는 **주인 있는 잠금** (뮤텍스의 커널 쪽 이름) 으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 코드에서 보이는 것 `[정적]`:
  - `_KMUTANT`: `MutantListEntry +0x18` · **`OwnerThread +0x28`** (주인) · `Abandoned +0x30` 비트 0 · `AbEnabled` 비트 1 · `ApcDisable +0x31`.
  - `SignalState` 가 **1 = 빔**, **0 이하 = 누가 가짐**. 주인이 또 가지면 −1 (재귀), 놓을 때 +1.
  - 직원은 가진 뮤턴트를 업무 카드 `MutantListHead (+0x308)` 목록에 꿴다.
- 비유: 가게 **창고 열쇠 하나**.
  - 열쇠 걸이에 걸려 있으면 (1) 빈 것.
  - 직원이 들고 가면 열쇠에 **이름표** (`OwnerThread`) 를 달고, 직원 **열쇠 꾸러미** (`MutantListHead`) 에 꿴다.
  - 같은 직원이 또 빌리면 "또 빌림" 숫자만 하나 내려간다.
- "버려진 뮤턴트" 는 2.11 에서 푼다.

#### 번호표 (기다림 블록, `_KWAIT_BLOCK`, `0x30`B) — 칸

[run13 해설 2.8](../02_sprint/run13-resume-thread-해설.md#28-깨우는-방식--신호등-초록--어깨-두드리기-정적) 의 **"나 여기서 기다려요" 번호표** 다. 이번에 칸을 처음 다 열어 봤다.

| 칸 | 이름 | 이 글에서 부르는 말 |
|---|---|---|
| `+0x0` | `WaitListEntry` | 신호등 번호표 줄에 꿰는 고리 |
| `+0x10` | `WaitType` | 번호표 **종류**: 1 · 0 · 2 · 4 (아래) |
| `+0x11` | `BlockState` | 번호표 **상태** (4 = 걸려 있음, 2.9) |
| `+0x12` | **`WaitKey`** | **번호표에 적힌 번호** — 깨어날 때 결과값이 된다 |
| `+0x18` | `Thread` | 누구 번호표인가 |
| `+0x20` | `Object` | 무엇을 기다리나 |

- `WaitType` 숫자 뜻 (결과서 읽기) `[정적]`, 이름은 `[추측]` (`_WAIT_TYPE` 은 PDB 에 없다):
  - **1** = 하나라도 풀리면 깸 (WaitAny 로 보임). `KeWaitForSingleObject` 가 이 값을 쓴다.
  - **0** = 전부 풀려야 깸 (WaitAll).
  - 2 = 큐 (완료 포트) 알림, 4 = DPC.
- 직원마다 업무 카드 `+0x140` 에 번호표 **4장** 이 붙어 있다 (`0x30`B 씩) `[정적]`.
  - `Thread` 칸은 직원을 뽑을 때 `KeInitThread +0x69 ~ +0x7e` 가 네 장 모두에 미리 적는다 (run52 `[정적]`).
- 한 물건 기다림은 **0번 표** (`WaitBlock[0]`) 를 쓴다 `[정적]` (`KeWaitForSingleObject +0x19f LEA R9,[RDI+0x140]`).

#### `WaitKey` 와 `WaitStatus` — 둘을 잇는 게 이번 답

- `WaitStatus (+0xc8)` = run75 해설 2.9 의 **"왜 깨어났나" 한 줄 결과**. run75 는 "돌려준다" 까지만 봤다.
- 이번 답 `[정적]`: **깨우는 쪽이** 이 칸에 **번호표의 `WaitKey`** 를 적는다 (2.8).
- 한 물건 기다림은 번호표에 `WaitKey = 0` 을 적어 건다 (`KeWaitForSingleObject +0x1e2`) → 깨나면 `WaitStatus = 0` = `STATUS_WAIT_0`.
- 비유: 번호표에 **"0번 물건"** 이라고 적혀 있다. 깨우는 담당이 그걸 보고 업무 카드 결과 칸에 **"0번 물건 받음"** 을 옮겨 적는다.

#### 업무 카드 (`_KTHREAD`) 에서 이번에 쓰는 칸

| 칸 | 이름 | 이 글에서 부르는 말 |
|---|---|---|
| `+0x70` | `WaitRegister` (비트 `State` 0~2 · `Apc` 5 · `Alert` 7 …) | **기다림 진행 단계** 바이트 (2.6) |
| `+0x74` | `MiscFlags` (`Alertable` 비트 4 · `TimerActive` 비트 9) | |
| `+0xc8` | `WaitStatus` | **결과 칸** |
| `+0xd0` | `WaitBlockList` | 지금 쓰는 번호표 |
| `+0xd8` | `WaitListEntry` | 줄 고리 (run75). 깨운 직원 명단에도 이 고리로 |
| `+0x140` | `WaitBlock` ×4 | 번호표 4장 |
| `+0x184` | `State` | **2** Running · **5** Waiting · **7** DeferredReady (PDB `_KTHREAD_STATE`) |
| `+0x187` | `WaitMode` | 유저가 부른 기다림이면 1 |
| `+0x236` · `+0x237` | `AdjustReason` · `AdjustIncrement` | 깨움 가산점 메모 (2.10) |
| `+0x24b` | `WaitBlockCount` | 번호표 몇 장 썼나 |
| `+0x283` | `WaitReason` | 사유 칸 (run76) |
| `+0x2c8` | `WaitPrcb` | 어느 계산대의 기다림 목록에 올라 있나 (깨울 때 0, 2.9) |
| `+0x308` | `MutantListHead` | 열쇠 꾸러미 |
| `+0x3d0` | `AbWaitObject` | 깨울 때 0 |
| `+0x3e0` · `+0x3e8` | `KernelWaitTime` · `UserWaitTime` | 기다린 시간 합 |

- 이름은 PDB (`pdb_struct.py`, 결과서), 쓰임은 `[정적]`.

### 2.4 이번 바퀴의 핵심 — "표시만, 다시 묻기" vs "그 자리에서 채우기"

이게 이번 바퀴에서 가장 중요한 그림이다. 장면 하나로 나란히 보자.

> 장면: 직원 A 가 자동 재설정 이벤트 E 를 기다리며 잔다. 직원 B 가 E 를 켠다 (`SetEvent`).

#### PE_emulator 지금 `[추측]` (요청서)

1. A: "잠듦" 표시, 다른 직원에게 계산대를 넘김.
2. B: A 를 **"실행 가능" 표시만** 한다.
3. A: 다시 계산대를 받으면 자기 syscall 을 **다시 물어** (E 를 다시 보고) 결과를 정한다.

- 비유: 신호등 담당이 자는 직원 어깨만 **툭** 치고 간다. 직원이 일어나서 신호등을 **다시 보러** 간다.

#### 진짜 커널 `[정적]`

1. A: `KeWaitForSingleObject` — `State = 5` 를 먼저 적고 (`+0x1a6`) E 를 본다. 빨강이다.
   - 0번 표 (`WaitKey 0`) 를 E 줄 **맨 뒤** 에 건다 (`+0x7b7`) → `KiCommitThreadWait` → `KiSwapThread` 에서 잔다 (`KiCommitThreadWait +0x220`).
2. B: `KeSetEvent` — E 를 먼저 초록 (1) 으로 (`+0x8d`). E 줄 번호표를 앞에서부터 본다.
   - A 의 표를 **뗀다** (`+0x313`).
   - A 의 `State = 7` (`+0x469`), "깨운 직원 명단" 에 올린다 (`+0x47d`).
   - A 의 결과 칸에 **`WaitStatus = WaitKey` (0) 를 써 준다** (`+0x484`).
   - 성공했으니 E 를 **그 자리에서 0** 으로 (`+0xd1`).
3. A: 차례가 오면 `KiSwapThread` 가 결과 칸 (0) 을 돌려준다 → `KiCommitThreadWait` 그대로 → `KeWaitForSingleObject` 는 `0x100` 이 아니니 그대로 반환 (`+0x860` → `+0x2c4`).
   - **E 를 다시 안 본다.**

- 비유: 신호등 담당이 번호표를 떼어 가며, 직원 업무 카드 결과 칸에 **"0번 물건 받음"** 을 적어 주고, 자동 신호등이면 **직접 빨강으로** 돌리고, 직원을 대기 줄 명단에 올린다. 직원은 일어나서 **카드만 읽고** 간다.

#### 나란히

| 무엇 | PE_emulator 지금 `[추측]` | 진짜 커널 `[정적]` |
|---|---|---|
| 들어갈 때 이미 신호면 | (요청서에 없음) | **안 잔다.** 그 자리에서 가져감 (2.5) |
| 깨우는 쪽이 하는 일 | "실행 가능" 표시만 | 표 떼기 · `WaitStatus = WaitKey` · `State = 7` · 명단 push · 자동 이벤트 0 / 뮤턴트 주인 넘김 |
| 깨난 쪽이 하는 일 | 자기 syscall 을 다시 물음 | `WaitStatus` **그대로** 반환 |
| 다시 보는 경우 | 늘 | `0x100` 일 때만 (2.7) |
| 결과를 정하는 쪽 | 깨난 쪽 | **깨우는 쪽** |

#### 왜 이 차이가 결과를 바꿀 수 있나 — 단계별로

1. `[정적]` 진짜: 자동 E 는 B 가 A 를 깨운 **그 순간** 이미 0 이다 (`KeSetEvent +0xd1`).
2. `[정적]` 그 뒤 직원 C 가 E 를 기다리러 오면 빨강이다 → 줄 맨 뒤에 서서 잔다 (`KeWaitForSingleObject +0x213` 을 안 타고 `+0x7b7`).
3. 그런데 만약 nt 엔진이 깨울 때 E 를 1 로 남겨 두고, A 가 나중에 다시 와서 가져간다면?
   - A 가 계산대를 받기 **전에** C 가 오면, C 가 초록을 보고 먼저 가져갈 수 있다 (2.5 의 "신호면 안 잔다" 길).
   - 그러면 A 는 다시 보고 빨강 → **또 잔다.** 진짜에선 A 가 받았을 신호를 C 가 받는 셈이다.
4. 뮤턴트도 같은 꼴이다. 진짜는 놓는 순간 첫 대기자에게 주인을 넘기니 (`KeReleaseMutantEx +0x330`) **끼어들 틈이 없다** `[정적]`.

- 3 은 `[추측: 이 해설의 짐작]` 이다. PE_emulator 가 깨울 때 E 를 어떻게 두는지는 요청서에 없다 ("표시만" 까지만). 근거는 1·2·4 의 줄이다.
- 비유: "어깨만 치고 가는" 방식이면, 자던 직원이 일어나 신호등까지 가는 사이에 **새치기** 가 생길 수 있다. 진짜는 물건을 아예 **손에 쥐여 주고** 간다.

### 2.5 기다리는 쪽 ① — 먼저 "기다림" 표시, 신호면 안 잔다 (질문 1(a))

#### 인자 `[정적]` (결과서)

- `RCX` 물건 · `EDX` `WaitReason` · `R8B` `WaitMode` · `R9B` `Alertable` · 스택에 마감 포인터.
- 유저 syscall 길이면 `ObWaitForSingleObject +0xb5` 가 `EDX = 6` (`UserRequest`) 을 넣는다.

#### 기다림마다 새로 쓰는 칸 — 다시 도는 지점 `+0x104` 바로 뒤 `[정적]` (flow-writer `.asm`)

- `+0x112` `Alertable` 끔 · `+0x116` `WaitRegister = 0` · `+0x11a` `WaitMode` · `+0x126` 인자가 `Alertable` 이면 켬.
- **`Alertable`** = 알림 (APC 등) 이 오면 깰 수 있는 기다림이라는 표시로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이번 코드에선 **기다림마다 지웠다가 다시 쓰는 비트** 다 `[정적]`. 2.16 사진 P2 에서 다시 나온다.

#### 순서 `[정적]` (`RDI` = 지금 직원)

| # | 줄 | 하는 일 | 비유 |
|---|---|---|---|
| 1 | `+0x19f LEA R9,[RDI+0x140]` | 번호표 = **자기 `WaitBlock[0]`** | 0번 표를 꺼냄 |
| 2 | **`+0x1a6`** · `+0x1b0` · `+0x1b9 ~ +0x1c5` · `+0x1cb` | **`State = 5` 를 먼저** · `WaitReason` · `WaitTime` = KUSD `+0x320` (`TickCount`) 아래 32비트 · `ThreadLock = 0` | 신호를 보기도 **전에** 카드에 "기다리는 중" |
| 3 | `+0x1d8 ~ +0x1e7` | 표 채우기: `WaitType = 1` · `BlockState = 4` · **`WaitKey = 0`** (`+0x1e2`) · `Object` | 표에 "0번 물건" 이라고 씀 |
| 4 | `+0x1eb` | `BTS.LOCK [R12],7` 헤더 락 | 기둥 자물쇠 |
| 5 | `+0x205` · `+0x208 / +0x20b` · `+0x211 / +0x213` | `Type` → 뮤턴트면 `+0x3e3` / `SignalState > 0` 이면 `+0x5f0` | 불 보기 |

- "기다리는 중" 을 왜 신호를 보기도 전에 적나 — 코드는 이유를 말해 주지 않는다. PE_emulator 표에는 "순서만 다르다 (`Waiting` 을 먼저 적고 신호면 되돌림)" 로 적혔다.

#### 신호면 그 자리에서 (즉시 채움) — 물건마다 `[정적]`

`KiWaitSatisfyOther`·`KiWaitSatisfyMutant` 의 몸이 여기 **풀려 있다** (호출 없음, 2.8 "새 말 — 풀어 넣기").

| 물건 | 줄 | 하는 일 | 비유 |
|---|---|---|---|
| 자동 이벤트 (`Type 1`, 동기 타이머도 같은 모양) | `+0x5f0 AND AL,7` / `+0x5f2 CMP AL,1` / `+0x5f4 JZ +0x645` → **`+0x645 MOV [R12+4],R10D`** | `SignalState = 0` | 지나가면서 직접 빨강 |
| 세마포어 (`Type 5`) | `+0x5f6 CMP CL,5` → `+0x5ff ~ +0x602` | `SignalState − 1` | 숫자 하나 깎고 지나감 |
| 수동 이벤트 (`Type 0`) · **스레드 객체 (`Type 6`)** | `+0x5f9 JNZ +0x246` | **아무것도 안 바꿈** | 불은 그대로, 그냥 지나감 |
| 뮤턴트 (`Type 2`) | `+0x3e3 ~` (아래) | 주인 = 나 | 열쇠 들고 감 |

- 뮤턴트 갈래 (`+0x3f0 TEST EDX,EDX / +0x3f2 JLE +0x733`):
  - **열쇠가 걸려 있으면** (`SignalState > 0`): `+0x408 ~ +0x410` −1 → 0 이 됨 → **가져감**:
    - `+0x416 WaitStatus = 0` · `+0x444 / +0x44c` (`ApcDisable` 이면) `KernelApcDisable − 1` · **`+0x489 OwnerThread = 나`** · `+0x499 OR WaitStatus,0x80` (버려진 것이면, 2.11) · `+0x4c6 ~ +0x4e9` 내 `MutantListHead` 맨 뒤에 꿰기 · `+0x4ed` 락 풀기 · `+0x4f6 State = 2` · `+0x507 RSI = WaitStatus` (돌려줄 값).
  - **내가 이미 들고 있으면** (`SignalState ≤ 0` 이고 `+0x733 CMP RDI,[R12+0x28]` 가 같음): −1 만 하고 바로 끝 (재귀). 한도 `0x80000000` (`+0x3fd / +0x402`) 에 닿으면 `+0xbc4 KiFastExitThreadWait` 뒤 `0xc0000191` 로 예외 (`+0xbc9 ~ +0xbce`).
  - **남이 들고 있으면**: 기다리러 간다 (2.6).
- 끝: `+0x246` 헤더 락 풀기 → **`+0x251 State = 2`** → `+0x2c4 MOV EAX,R14D` (`R14D` = `+0x1ad` 에서 넣은 0) → `+0x2da RET`. 반환 **0 = `STATUS_WAIT_0`**.
- test.exe 의 `WaitForSingleObject(thread)` 는 **스레드 객체** 라 "아무것도 안 바꿈" 갈래 모양이다 `[소스]` `sample/test.c:35` + `[정적]`. 스레드 객체가 언제 신호가 되나 (끝내기) 는 Q141d.
- [run13 해설 2.9](../02_sprint/run13-resume-thread-해설.md#29-경주--부모가-먼저-초록불을-켜면-정적) 의 "신호등은 상태로 남는다" 와 같은 그림이다. 깨우는 쪽이 먼저 와서 켜 두면, 나중에 온 직원이 보고 **안 자고** 지나간다.
- PE_emulator: "신호면 안 잔다" 가 없으면 **모자람** → 추가 (run 문서 표 1(a)).

#### 안 자고 끝나는 다른 갈래 (줄만) `[정적]`

- 마감을 이미 넘김 (`+0x356 / +0x359 JA +0x240`, 절대 마감이 0 이면 `+0x237`) → **`+0x240 R14D = 0x102`** (`STATUS_TIMEOUT`).
  - 같은 시각이면 이 검사를 지나 번호표를 걸고, 타이머 쪽에서 정해진다 (Q141c2).
- 이미 `Alerted` (`+0x37d` · `+0xb32`) → `0x101`.
- 유저 APC 대기 → `+0x3a8 R14D = 0xc0`.

### 2.6 기다리는 쪽 ② — 신호가 아니면: 번호표 걸기 → 커밋 → 잔다 (질문 1(b))

#### 번호표 걸기 `[정적]`

- `+0x35f ~ +0x36c` 꼬리 검사 → **`+0x7b7 ~ +0x7c1` 물건 `WaitListHead` 맨 뒤에 꿰기** → `+0x7c5` 헤더 락 풀기 → `+0x7d2 WaitBlockCount = 1` → `+0x837 R8D` = 마감 종류 (0 없음 · 1 절대 · 2 상대) → **`+0x841 KiCommitThreadWait`** (ret `+0x846`).
- 비유: 빨강이면 0번 표를 신호등 줄 **맨 뒤** 에 건다. 그리고 기둥 자물쇠를 풀고 "확정 담당" 에게 간다.
- PE_emulator: "잠듦 표시 → 넘김" 은 **맞음** (run 문서 표 1(b)).

#### 새 말 — 커밋 (`KiCommitThreadWait`)

- 이름 그대로 **기다림을 확정** 하는 함수다. 하는 일 `[정적]`: 마지막 검사 → `WaitRegister.State = 1` → 마감이면 타이머 → `KiSwapThread`.
- 비유: 번호표를 건 뒤, 업무 카드에 **"정말 잔다" 도장** 을 찍고, (마감이 있으면) 알람을 맞추고, 계산대를 내놓는다.

#### 새 말 — 기다림 진행 단계 (`WaitRegister.State`)

- 업무 카드 `+0x70` 바이트의 아래 3비트. 숫자 뜻 (결과서 읽기) `[정적]`:
  - **0** = 번호표는 걸었고 아직 커밋이 안 끝남
  - **1** = 커밋됨 (`KiCommitThreadWait +0xbf` 가 씀)
  - **2** = 커밋 전에 누가 깨움
  - 3 · 4 · 5 · 6 은 2.9 표.
- 이름: PDB 에 **`_KWAIT_STATE`** 열거형이 있다 (flow-writer `llvm-pdbutil`).
  - 0 `WaitInProgress` · 1 `WaitCommitted` · 2 `WaitAborted` · 3 `WaitSuspendInProgress` · 4 `WaitSuspended` · 5 `WaitResumeInProgress` · 6 `WaitResumeAborted`.
  - 그런데 칸 타입은 그냥 3비트 바이트다. 그래서 **칸과 이 열거형을 잇는 건 `[추측: 이름]`** 이다. 숫자 뜻은 결과서 읽기와 다 맞는다.
- 비유: 업무 카드의 **"기다림 진행 단계"** 칸 — 0 "줄 서는 중" · 1 "확정, 자는 중" · 2 "확정 전에 깨워짐".

#### `KiCommitThreadWait` 안 `[정적]` (`RBX` = 직원)

| 줄 | 하는 일 | 비유 |
|---|---|---|
| `+0x38 / +0x3b JNZ +0x23d` → **`+0x23d`** · `+0x242` → `+0x24c JMP +0x41` | 마감이 있으면 먼저 `TimerActive` 켜기 · `Timer` 머리 | 알람 준비 |
| `+0x5b` | `WaitBlockList = 번호표` | 지금 쓰는 표 적기 |
| `+0x71` | 스레드 락 | |
| **`+0x97` / `+0xa1 TEST AL,0xe7` / `+0xa3 JNZ +0x251`** | `WaitRegister` 에 State·APC·Alert 가 이미 있으면 **안 잔다** (아래) | 도장 찍기 전에 누가 이미 깨웠거나 쪽지가 왔으면 |
| `+0xac` · `+0xb6` · **`+0xbf`** | **`WaitRegister.State = 1`** → `+0xc2 KiPerformWaitDeferredWork` (이름만) | "정말 잔다" 도장 |
| **`+0x160`** | **`Preempted = 0`** | "자리 뺏김" 도장 지우기 (run75: 깨어나면 줄 **뒤** 근거의 한쪽) |
| `+0x167 / +0x16a` … `+0x194` · **`+0x1f2`** | 마감이 있으면 만기 시각을 `Timer` 에 · **`KiInsertTimerTable`** (절대 마감은 `+0x4e3 KiComputeDueTime` 먼저, 이미 만기면 `+0x1f9 JZ +0x395` → `+0x39e KiTimerWaitTest`) | 알람 맞추기 (Q141c2) |
| **`+0x220`** (ret `+0x225`) | **`KiSwapThread`** | 잔다 (2.2 줄 1) |

- 조심: `+0x1f2` · `+0x23d` 는 **`KiCommitThreadWait` 오프셋** 이다. 결과서 요약만 읽으면 `KeWaitForSingleObject` 오프셋으로 읽히기 쉽다 (run 문서 "다르게" ①). 이 글은 늘 함수 이름을 붙여 쓴다.

#### 커밋 전에 이미 끝난 기다림 (`+0xa3 JNZ +0x251`) `[정적]`

- 깨우는 쪽 (2.9 표) 에 "`WaitRegister.State` 가 0 이면 2 로 바꾸고 `WaitStatus` 를 쓴다" 는 갈래가 있다. 이 길이 그 짝이다.
  - 번호표를 건 (`+0x7b7`) 뒤 도장 (`+0xbf`) 을 찍기 전에 깨우는 쪽이 올 수 있다는 뜻으로 읽힌다. 실제로 언제 생기나는 런타임 일이다.
- 줄:
  - `+0x251 R13D = 0x101` · `+0x257 TEST AL,7 / +0x259 JZ +0x435`.
  - `WaitRegister.State` 가 **0** 이면 APC·Alert 때문 → `+0x4fa` `0x100` 또는 `+0x43d ~ +0x451` `0x101`/`0xc0`.
  - **0 이 아니면 누가 이미 `WaitStatus` 를 써 놨다** → `+0x262 State = 2` · `+0x287 R15 = WaitStatus` · 남은 표 떼기 (`+0x2b2 ~ +0x332`, `BlockState == 4` 만) · `+0x2de KiExitThreadWait` → `+0x2e3 JMP +0x228` (`MOV RAX,R15`).
- **`KiSwapThread` 로 안 간다.** 자지 않고 그 값을 들고 간다.
- 비유: "정말 잔다" 도장을 찍으려는데, 카드 결과 칸에 **이미 "받음" 이 적혀 있다**. 그럼 안 자고 그 결과를 들고 간다.
- 이것도 "깨우는 쪽이 채운다" 의 한 모습이다. 깨난 쪽은 물건을 다시 안 본다.

### 2.7 깨난 뒤 — 받은 값을 그대로, `0x100` 일 때만 처음부터 (질문 1(c))

#### `KiCommitThreadWait` — 손 안 댄다 `[정적]`

- `+0x225 MOV R15,RAX` · `+0x228 MOV RAX,R15` · `+0x22b ~ +0x23b` 레지스터 되살리고 `RET`. **사이에 계산 없음.**
- `KiSwapThread` 가 `WaitStatus (+0xc8)` 를 돌려주는 줄은 run75 (`+0x25c` · `+0x29a`).

#### `KeWaitForSingleObject` — `0x100` 만 본다 `[정적]`

1. `+0x846 XOR R10D,R10D` — 돌아온 자리 (ret).
2. **`+0x849 MOV R14,RAX`** — 받은 값을 `R14` 에. (결과서는 `+0x846` 으로 적었다 → run 문서 "다르게" ②.)
3. `+0x850 / +0x853` 자동부스트 갈래 (`+0xb41 ~ +0xb6a`, 이름만) · `+0x859 AbWaitObject = 0`.
4. **`+0x860 CMP R14D,0x100` / `+0x867 JNZ +0x2c4`** → `+0x2c4 MOV EAX,R14D` (하위 32비트) → 반환.

#### 새 말 — `STATUS_KERNEL_APC` (`0x100`)

- 이름은 결과서·run 문서가 붙인 상태값 이름이다.
- 이 코드에서의 뜻 `[정적]`: 이 값으로 돌아오면 `KeWaitForSingleObject` 가 **처음부터 다시** 한다.
- 이 값을 만드는 곳 `[정적]`:
  - `KiCommitThreadWait +0x4fa MOV [RBX+0xc8],0x100` — 자기 전에 `WaitRegister.Apc` 비트 (`+0x435 TEST AL,0x20`) 가 서 있을 때.
  - 깨우는 쪽이 **`WaitType 0` (전부 기다림)** 번호표를 깨울 때 일부러 `0x100` 을 넘긴다 (2.8 · 2.9).
- 일반적으로 알려진 설명으로는, 기다리는 동안 커널 APC (쪽지) 가 와서 잠깐 깨워 처리한 뒤 원래 기다림을 다시 하게 하는 값이다 `[추측]`.
- 비유: 카드 결과 칸의 **"처음부터 다시 확인"** 도장. 이 도장일 때만 직원이 신호등을 **다시 보러** 간다.

#### `0x100` 이면 — 처음부터 `[정적]`

- `+0x86d ~ +0x876` IRQL 2 · `+0x88d` `WaitIrql` 되살림 · **`+0x891 JMP +0x104`**.
- 헤더 락부터 **전부 다시** 한다. 신호면 가져가고 (2.5), 아니면 다시 번호표를 건다 (2.6).
- 상대 마감은 처음 계산한 시각을 그대로 쓴다 (`+0x3b6 ~ +0x3d7` → `+0x3de JMP +0x104`). 다시 돌 때 마감이 늘어나지 않는다.

#### 그래서 답

| 돌려주는 값 | 언제 |
|---|---|
| `0` | 신호로 깸 / 신호라 안 잠 (한 물건 번호표 `WaitKey 0`) |
| `0x80` | 버려진 뮤턴트를 받음 (2.11) |
| `0x101` | `Alerted` |
| `0x102` | 마감 지남 |
| `0xc0` | 유저 APC 대기 |
| `0x100` | **안 나간다.** 처음부터 다시 돈다 |

- 요청서 `[추측]` "깨난 쪽이 다시 묻는다" → **틀림** `[정적]`. 다시 보는 건 `0x100` 하나.
- 비유 정리: 일어난 직원은 카드 결과 칸만 읽고 간다. "처음부터 다시 확인" 도장일 때만 신호등으로 돌아간다.

### 2.8 깨우는 쪽 ① — `KeSetEvent`: 신호 먼저, 그 자리에서 채우기 (질문 2)

인자: `KeSetEvent(Event, Increment, Wait)`.

#### 첫머리 — 자동/수동 가르기 `[정적]`

- `+0x12 TEST byte [RCX],0x7f / +0x20 JNZ +0x137` → `Type ≠ 0` 이면 `+0x137 XOR EDI,EDI` (**자동**), 0 이면 `+0x30 EDI = 1` (**수동**).
- 수동 + 이미 1 + `Wait == 0` 이면 락도 안 잡고 `+0x147 MOV EAX,1` 반환 (`+0x26 / +0x2a JZ +0x13e` · `+0x13e / +0x141`).

#### 신호를 **먼저** 켠다 `[정적]`

- `+0x64` 헤더 락 · **`+0x74` 옛 값 → `+0x86` 돌려줄 값으로 저장 → `+0x8d SignalState = 1`**.
- `+0x94 / +0x96 JNZ +0xe3` — 옛 값이 이미 1 (이미 초록) 이면 **아무도 안 깨운다.**
- 비유: 담당은 먼저 초록을 켠다. 원래 초록이었으면 어깨 두드릴 사람이 없다.

#### 자동 재설정 루프 (`+0xa8` 부터) — 한 걸음씩 `[정적]`

1. 번호표를 앞에서 **하나** 뗀다 (`+0x313`).
2. `+0x31a ~ +0x31e` `WaitType == 1` 이면 풀어 넣은 깨움 (아래 "새 말"): `+0x326 R13D = WaitKey`.
3. 커밋된 기다림이면: **`+0x469 State = 7`** · `+0x470 R14B = 1` (성공 표시) · **`+0x47d` 깨운 직원 명단 push** · **`+0x484 WaitStatus = WaitKey`** · `+0x48b AbWaitObject = 0`.
   - 커밋 전 기다림이면 `+0x4cd` · `+0x4d6`.
4. `+0x4ab / +0x4ae` — 실패 (이미 다른 쪽이 깨운 직원 등) 면 다음 표 (`+0xa8`). `SignalState` 는 안 줄인다.
5. 성공이면 `+0x4b4 JMP +0xd1` → **`+0xd1 SUB [R15],1`** (1 → 0) → `+0xd5 JNZ` 아니면 끝 (2.2 줄 2).
6. 깨울 표가 하나도 없으면 (`+0xa8 / +0xab JZ +0xdb`) **1 로 남는다.** 다음에 오는 직원이 2.5 의 `+0x645` 로 가져간다.

- 그래서: 자동은 **깨움에 성공한 첫 한 명** 만, 그리고 그 자리에서 0.
- 큐 블록 (`WaitType 2`, `+0x54e KiInsertQueueInternal`, 이것도 한 몫으로 셈) · DPC 블록 (`WaitType 4`, `+0x71f KeInsertQueueDpc`) 은 이름만.

#### 수동 재설정 루프 (`+0x1a3` 부터) `[정적]`

- 번호표를 하나씩 안 떼고 **전부** 깨운다: `+0x1c0 R13D = WaitKey` → 커밋됨 `+0x2ca KiEnterDeferredReadyState` · `+0x2e5` push · **`+0x2ec WaitStatus`** / 커밋 전 `+0x168` · `+0x171`.
- 끝에 줄 머리째 비운다 (`+0x4fe ~ +0x506`).
- **`SignalState` 를 줄이는 줄이 없다** → 1 유지.
- PE_emulator: 수동은 **맞음**. 자동은 "첫 하나만 + 그 자리에서 0" **추가** (run 문서 표 2(a)).

#### `WaitStatus` 를 쓰는 줄 넷 (질문 2(b)) `[정적]`

| 경우 | 수동 루프 | 자동 루프 |
|---|---|---|
| 커밋된 기다림 | `+0x2ec` | `+0x484` |
| 커밋 전 기다림 | `+0x171` | `+0x4d6` |

- 값은 모두 **그 번호표의 `WaitKey`**. `KeWaitForSingleObject` 가 건 표는 0 → `STATUS_WAIT_0`.
- 여러 물건 기다림이면 번호 n → `STATUS_WAIT_0 + n` 이 되는 모양이지만, `KeWaitForMultipleObjects` 는 안 읽었다 `[공백]` (Q145 ②).

#### 돌려주는 값 (질문 2(d)) `[정적]`

- **옛 `SignalState`** (0 / 1). `+0x86` 에 저장 → `+0x10c KiExitDispatcher` 뒤 `+0x111` 에서 꺼냄 → `+0x135 RET`.

#### 새 말 — 몸에 풀어 넣기

- 한 물건 번호표 (`WaitType 1`) 를 깨울 때 `KeSetEvent` 는 `KiTryUnwaitThread` 를 **부르지 않는다.** 같은 순서를 자기 몸 안에 풀어 넣었다 `[정적]` (결과서: 수동 `+0x1b3 ~ +0x2fe`, 자동 `+0x31a ~ +0x4f0`).
- 비유: 어깨 두드리기 담당을 부르지 않고, 그 담당의 **업무 설명서를 신호등 담당이 자기 손으로 그대로** 따라 한다.
- 그래서 "누가 누구를 부르나" 명부 (`_calls.tsv`) 엔 **안 나온다.**
  - 2.5 의 `KiWaitSatisfyOther`·`KiWaitSatisfyMutant` 도 `KeWaitForSingleObject` 에 풀려 있어 같다.
  - "누가 이 일을 하나" 를 callee 이름으로만 찾으면 놓친다. 칸 오프셋 (`+0x28` `OwnerThread` 등) 으로 `.asm` 을 grep 해야 한다 (run 문서 도구 메모).
- 진짜 `KiTryUnwaitThread` 를 부르는 건 `WaitType 0` (전부 기다림) 번호표뿐이다: **`+0x6a6` · `+0x6d0`** (ret `+0x6ab` · `+0x6d5`), `R8D = 0x100`, `R9D = 0`. 자동 루프 쪽은 `+0x6b4 JMP +0xa8` 로 `SUB` 를 건너뛴다 — 이벤트도 안 내린다.
- PE_emulator: 깨우는 함수는 **이름만 다름**. "표시 → 준비 줄" 두 단계면 된다 (run 문서 표 2(c)).

### 2.9 깨우는 쪽 ② — 어깨 두드리기 `KiTryUnwaitThread` 의 꼴 (질문 3)

인자: `KiTryUnwaitThread(Prcb, 번호표, WaitStatus, 칸)` → `AL` 이 1 이면 깨움.

#### 누가 부르나 (이번에 본 것) `[정적]`

- `KeSetEvent +0x6a6` · `+0x6d0`, `KeReleaseMutantEx +0x472` — `WaitType 0` 번호표, `0x100`.
- `KePulseEvent +0x98` · `+0x170` · `+0x1e8` (2.12).
- 2.2 `KiResumeThread +0xa6` (run13, 아래).
- 모두 `R9 = 0` 으로 부른다. `R9 ≠ 0` 갈래 (`+0x237 ~ +0x2e0`, 뮤턴트 넘기기까지) 를 부르는 곳은 이번에 안 봤다 (caller 32곳) `[공백]` → Q145 ④.

#### 걸음 `[정적]`

1. `+0x1e` 직원 = 표 `Thread` · `+0x33` 스레드 락.
2. **`+0x57 ~ +0x60` `State ≠ 5` 면 `+0x186` 으로 — 아무것도 안 한다.** 이미 깨운 직원은 지나친다.
3. `WaitRegister.State` (`+0x66 ~ +0x6f`) 로 갈린다:

| 값 (`_KWAIT_STATE` 이름, `[추측: 이름]`) | 줄 | 하는 일 |
|---|---|---|
| 1 `WaitCommitted` · 4 `WaitSuspended` | `+0x72 JNZ +0x2e5` → `+0x2e8 JZ +0x78` | **깨움** (아래) |
| 0 `WaitInProgress` | `+0x2ee / +0x2f0` → `+0x2f2 ~ +0x2f8` | `WaitRegister.State = 2` (`WaitAborted`) · `+0x2fb WaitStatus` · `+0x302` · `+0x30d BlockState = 0` → 성공 (2.6 "커밋 전에 이미 끝난 기다림" 의 짝) |
| 5 `WaitResumeInProgress` | `+0x316 / +0x319` → `+0x31b ~ +0x321` | `State = 6` (`WaitResumeAborted`) 로만. 깨우지 않음 |
| 3 `WaitSuspendInProgress` | `+0x330` → `+0x339` | `BlockState = 2` 만. 깨우지 않음 |
| 2 · 6 · 7 | 위 어디에도 안 걸림 | 아무것도 안 함 |

4. 끝: `+0x194` 스레드 락 풀기 · `+0x19c ~ +0x1a7 BlockState + 1` (성공·지나침 **모두**) · `+0x1b9 RET`.

#### 깨움 (1·4) 안 `[정적]`

| 줄 | 하는 일 | 비유 |
|---|---|---|
| `+0x78 ~ +0x91` | `Queue` 동시성 (이름만) | |
| `+0xa3` · **`+0x100`** | **`WaitPrcb` 줄에서 떼기**, `WaitPrcb = 0` | 계산대 "자는 직원" 목록에서 지움 |
| `+0x114 ~ +0x142` | 기다린 시간을 `KernelWaitTime` / `UserWaitTime` 에 더함 (`WaitMode` 로 갈림) | 근무 기록 |
| **`+0x149`** | **`State = 7`** (DeferredReady) | "줄 설 사람" |
| `+0x150 ~ +0x161` | PRCB `+0x2d08` 맨 앞 push | 깨운 직원 명단에 (2.10) |
| **`+0x168`** | **`WaitStatus = R12`** (넘겨받은 값) | 결과 칸 쓰기 |
| `+0x16f` · `+0x17a` | `AbWaitObject = 0` · `SIL = 1` (성공) | |

#### 번호표를 신호등 줄에서 떼는 건 **부른 쪽** `[정적]`

- 이 함수는 표의 `BlockState` 만 만진다. 줄에서 떼는 줄은 없다.
- 떼는 쪽: `KeSetEvent +0x313` (자동), `+0x4fe` (수동, 머리째), `KeReleaseMutantEx +0x247`.
- 여러 물건 기다림의 **남은 표** 는 깨난 쪽이 나가면서 뗀다 (`KiCommitThreadWait +0x2b2 ~ +0x32a`). 한 물건 기다림은 표가 하나라 깨난 쪽이 따로 안 뗀다.

#### 경쟁 — 두 계산대가 동시에 오면

- 먼저 스레드 락을 잡은 쪽이 `State = 7` 로 바꾼다. 뒤에 온 쪽은 `State ≠ 5` 로 지나친다 (줄 근거는 위, 실제 순서는 런타임).
- [run13 해설 2.9](../02_sprint/run13-resume-thread-해설.md#29-경주--부모가-먼저-초록불을-켜면-정적) 의 "경주" 와 같은 종류다. 비유: 두 담당이 같은 직원 어깨로 동시에 손을 뻗어도, 먼저 잡은 쪽만 깨운다.
- PE_emulator: CPU 하나·한 번에 한 직원이면 락·`WaitRegister` 경쟁은 필요 없다 `[추측: 에뮬레이터 범위]`. 단 **"이미 깨운 직원은 지나침" 은 필요** 하다 (한 직원이 두 물건을 기다릴 때, 결과서).

#### run13 첫 스레드 멈춤 풀기에 한 줄 더 `[정적]`

- `KiResumeThread +0x98 R8D = 번호표 WaitKey` · `+0x9d R9D = 0` 으로 `KiTryUnwaitThread` 를 부른다 (flow-writer `.asm`).
- 멈춘 스레드는 `KiSchedulerApc +0xf2 KeWaitForSingleObject` 로 기다리니 (run13) 표 `WaitKey = 0` (`+0x1e2`).
- 그래서 멈춤에서 풀린 첫 스레드가 `+0xf7` 에서 받는 값은 **0** 이다 `[정적: 줄]`. flow.md 2.2 `KiResumeThread` 절에 붙었다.

### 2.10 깨운 다음 — 준비 줄까지, 그리고 둘째 넘기기 길

#### 새 말 — 깨운 직원 명단 (`PRCB+0x2d08`)

- 계산대 장부 `+0x2d08` 칸. 깨운 직원을 쌓는 **단일 연결 목록 머리** 다 `[추측: 이름]`.
  - 공개 PDB 의 `_KPRCB` 는 `+0x6d0` 에서 잘려서 이름이 없다 ([run75 해설 2.3](run75-thread-switch-해설.md#23-무대-먼저--계산대-장부-prcb-와-업무-카드의-칸)).
- 쓰임은 `[정적]`:
  - push 하는 쪽: `KeSetEvent +0x2e5` · `+0x47d`, `KeReleaseMutantEx +0x2ff`, `KiTryUnwaitThread +0x161` — 업무 카드 고리 (`+0xd8`) 를 꿴다.
  - 비우는 쪽: `KiProcessThreadWaitList +0x1e / +0x38` **하나**.
  - sast/71 의 `[추측: 대기 목록 머리]` 보다 뜻이 좁혀졌다.
- 비유: 계산대 장부 귀퉁이의 **"방금 깨운 직원" 메모**. 아직 줄에 세운 건 아니다. `State 7` 은 run75 해설의 "밀린 명단" (DeferredReady) 과 같은 번호다.

#### 새 말 — `KiExitDispatcher`

- 깨우기를 마치고 헤더 락을 푼 뒤 **늘** 부르는 마무리 담당이다 `[정적]` (`KeSetEvent +0x10c`, `KeReleaseMutantEx +0x201`, `KePulseEvent +0xd0`). run13 의 `KeSuspendThread` 끝에도 나왔다.
- 비유: 신호등 일을 마치고 나가면서 "방금 깨운 직원" 메모를 정리하는 **퇴장 담당**.

#### 준비 줄로 — 한 걸음씩 `[정적]`

1. `KiExitDispatcher +0x18 / +0x23` — 명단 (`PRCB+0x2d08`) 이 비었으면 건너뜀.
2. `+0x25 / +0x27` — `Wait` 비트가 켜졌으면 `+0x10a KiDirectSwitchThread` 로 (깨운 직원에게 곧장, 이름만). 그러면 3 은 안 탄다.
3. **`+0x41 KiProcessThreadWaitList`** → `+0x1e / +0x38` 명단 비우기 → 직원마다:
   - `+0x98 AdjustReason = 1` · `+0x9f AdjustIncrement = Increment` → **`+0x101 KiDeferredReadySingleThread`** ([run75 해설 2.10](run75-thread-switch-해설.md#210-줄에-다시-세우기--기본-맨-뒤-선점당했고-몫이-남은-직원만-맨-앞) 의 "줄에 꿰는 곳").
- `Increment` 는 `KeSetEvent` 인자 그대로 온다 (`+0x5` → `+0xf2` → `KiExitDispatcher R9D` → `R8D`).
- `AdjustReason == 1` 이라 `KiDeferredReadySingleThread +0x3e / +0x67` 을 안 타고, `AdjustIncrement` 를 읽는 갈래 (`+0x6d`) 로 간다.
  - 우선순위 16 이상이면 다른 계산 (`+0xdb / +0xe5`). **계산 본체는 안 읽었다** → Q145 ③.
- 줄 앞/뒤: `Preempted` 가 `KiCommitThreadWait +0x160` 에서 0 이 됐으니 **뒤** 로 보인다. 다만 이 갈래의 `[RSP+0x58]` 초깃값 (`+0x63`, 0) 에 기댄 `[추측]` 이다 (run75 한계 그대로).
- 비유: 깨운 직원마다 **"깨움 가산점 +n" 메모** 를 붙이고 대기 줄에 세운다. 가산점이 등급을 실제로 얼마나 올리는지는 아직 모른다.

#### 둘째 넘기기 길 — 깨운 쪽이 그 자리에서 넘어간다 `[정적]`

1. `KiDeferredReadySingleThread +0x1ce2 KiCommitRescheduleContextEntry` 가 "다음 차례" (`NextThread`) 를 정했으면 (run75 질문 2(a)),
2. `KiExitDispatcher +0x56` · `+0x5a / +0x5e` (이전 IRQL ≥ 2 면 안 함) · **`+0x69 / +0x6c JNZ +0x134`**
3. → `+0x187 WaitReason = 0x20` (`WrPreempted`) · **`+0x19d KiQueueReadyThread`** (깨운 쪽 **자기** 를 준비 줄에) · **`+0x1ac KiSwapContext`**.

- **`KiSwapThread` 를 안 거친다.**
- 비유: 신호등 담당이 깨운 직원이 "다음 차례" 자리에 앉혀졌으면, 퇴장하면서 **자기가 계산대를 내주고** 줄에 선다.
- [run76 해설 2.14](run76-thread-quantum-해설.md) 의 "교대 담당을 부르는 문" 표에 한 줄이 더해지는 셈이다:

| 문 | 줄 | 사유 번호 | 근거 |
|---|---|---|---|
| 기다림 | `KiSwapThread +0x243` | — | run75 |
| 양보 | `KeYieldExecution +0x21f` | `0x21` | run75 |
| 몫 끝 | `KiQuantumEnd +0xbd6` | `0x1e` | run76 |
| 초인종 선점 | `KiDispatchInterrupt +0x171` | `0x1f` | run76 |
| 선점 | `KiCheckForThreadDispatch +0x10e` | `0x20` | run76 |
| 미룬 선점 | `KiHandleDeferredPreemption +0x297` | `0x26` | run76 |
| **깨운 직후** | **`KiExitDispatcher +0x1ac`** | **`0x20`** | **이번** |

- 모두 `[정적]`. 이 길이 run75 "P1 → P1a 교대 5번" 의 후보가 되는지는 2.16.

### 2.11 뮤턴트 — 놓는 쪽이 열쇠를 건넨다 (질문 4)

#### (a) 몸은 `KeReleaseMutantEx` `[정적]`

- `KeReleaseMutant(Mutant, Increment, Abandoned, Wait)` (52B) 는 `Flags` 만 만든다:
  - `+0x6 ~ +0x10` `Flags = (Abandoned ≠ 0) + 4` · `+0x16 ~ +0x22` `Wait` 면 `|2` → **`+0x26 KeReleaseMutantEx`** (ret `+0x2b`) → 이전 상태 반환.
  - 비트: 0 = 버림 · 1 = `Wait` · 2 = 오류를 반환값 대신 예외로 (`KeReleaseMutantEx +0x509`).
- 비유: 바깥 접수대 (`KeReleaseMutant`) 는 신청서 체크박스만 채워서 안쪽 담당 (`…Ex`) 에게 넘긴다.

#### (b) 놓기 — 한 걸음씩 `[정적]`

| # | 줄 | 하는 일 | 비유 |
|---|---|---|---|
| 1 | `+0x6a` · `+0x7a` · `+0x84 / +0x88` | 헤더 락 · `EBX` = 옛 `SignalState` · 버림이면 `+0x550` 으로 | |
| 2 | `+0x93 / +0x97` → `+0x4fc ~ +0x506` | **주인 아님** → `Abandoned` 면 `0x80`, 아니면 **`0xc0000046`** (`STATUS_MUTANT_NOT_OWNED`). 비트 2 면 `+0x515` 예외 | 남의 열쇠는 못 반납 |
| 3 | **`+0xbd ~ +0xc0`** · `+0xc5` · `+0xcb ~ +0xcd` | `SignalState + 1`. 아직 1 미만 (재귀) 이면 락만 풀고 끝 | "또 빌림" 하나 갚기 |
| 4 | `+0x110 ~ +0x152` · **`+0x15a`** | 1 (빔) 이 되면: 주인 꾸러미에서 떼고 **`OwnerThread = 0`** | 열쇠가 걸이로 |
| 5 | `+0x15d / +0x160` · `+0x247` · … · **`+0x306`** | 번호표를 앞에서부터 떼며, `WaitType 1` · `State == 5` 이면 깨움: 커밋됨 `+0x2d7 KiRemovePrcbWaitEntry` · `+0x2df KiEnterDeferredReadyState` · `+0x2ff` push · **`WaitStatus = WaitKey`** (커밋 전 `+0x4a0`) | 줄 맨 앞 직원 깨우기 |
| 6 | `+0x324 ~ +0x32b` → **`+0x330 KiWaitSatisfyMutant(뮤턴트, 깨운 직원, Prcb)`** | 물건이 뮤턴트일 때. `RDX` = 깨운 직원 (`+0x32d`) | **열쇠를 그 직원 손에** |
| 7 | **`+0x192`** | `SignalState − 1` (1 → 0) → 끝 | 걸이는 다시 빔 |
| 8 | `+0x201` | `KiExitDispatcher` (2.10 과 같은 준비 줄 길) | |

- 깨울 직원이 없으면 `SignalState` 는 **1 로 남는다.** 다음에 오는 직원이 2.5 의 뮤턴트 갈래로 가져간다.
- `KiWaitSatisfyMutant` 가 하는 일 `[정적]`:
  - `+0x4 / +0xa` `ApcDisable` 이면 깨운 직원 `KernelApcDisable − 1`.
  - **`+0x40 OwnerThread = 깨운 직원`**.
  - `+0x3c ~ +0x4d` `Abandoned` 면 끄고 **`+0x4d OR WaitStatus,0x80`** (아래 (c)).
  - `+0x58 ~ +0x69` `AbEnabled` 면 `AbWaitObject` 에 뮤턴트.
  - **`+0x74 ~ +0x93` 깨운 직원 `MutantListHead` 맨 뒤에 꿰기.**
- 비유: 열쇠를 걸이에 돌려놓는 **그 순간**, 줄 맨 앞 직원 손에 바로 쥐여 주고 이름표를 바꿔 달고 그 직원 꾸러미에 꿴다. 걸이는 다시 빈다. 줄에 아무도 없을 때만 열쇠가 걸이에 남는다.
- `WaitType 0` 번호표는 `+0x472 KiTryUnwaitThread (0x100)` — **주인 안 넘기고**, 깨난 쪽이 처음부터 다시 본다.
- PE_emulator "깨난 쪽이 가져감" → **틀림** (run 문서 표 4(b)).

#### (c) 새 말 — 버려진 뮤턴트

- 주인이 정상으로 놓지 않은 채 사라진 뮤턴트로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 누가 언제 "버림" 으로 놓는지는 이번 근거에 없다.
- 코드 `[정적]`:
  - 놓는 쪽이 `Flags` 비트 0 (버림) 으로 불리면 `KeReleaseMutantEx +0x84 / +0x88 JNZ +0x550` → **`+0x550 Abandoned = 1`** · **`+0x560 SignalState = 1`** → 위 (b) 의 깨우기로 이어진다.
  - 가져가는 쪽이 `Abandoned` 를 끄고 **`WaitStatus |= 0x80`** (`STATUS_ABANDONED_WAIT_0`).
  - `WaitKey` 를 쓴 **뒤** 에 OR 하므로 (`+0x306` → `+0x330`) `0x80` 이 남는다.
  - 같은 모양 줄 셋: `KiWaitSatisfyMutant +0x4d` · `KeWaitForSingleObject +0x499` (안 잔 길) · `KiTryUnwaitThread +0x29a`.
- 비유: 전 주인이 사라져 관리사무소가 대신 걸이에 걸어 둔 열쇠. 다음 직원은 **열쇠를 그대로 받는다.** 다만 결과 칸에 **"전 주인 행방불명"** 딱지 (`0x80`) 가 붙는다.
- PE_emulator: 없으면 **추가** (run 문서 표 4(c)).

### 2.12 `KePulseEvent` — 잠깐 초록, 끝에 늘 빨강 (질문 5)

인자: `KePulseEvent(Event, Increment, Wait)`.

#### 걸음 `[정적]`

1. `+0x1f / +0x25` `Type` 저장 · `+0x52` 헤더 락 · `+0x57 R12D = 옛 SignalState` (돌려줄 값, `+0xda`).
2. **`+0x5b / +0x5e JNZ +0xaa`** — 옛 값 ≠ 0 이면 (이미 신호 = 기다리는 쪽 없음) 깨우지 않고 `+0xaa` 로.
3. 0 이면 **`+0x60 SignalState = 1`** → `+0x6e / +0x71`:
   - **수동**: 모든 번호표에 `+0x98 KiTryUnwaitThread` (반환값 안 봄). 끝에 머리째 비움 (`+0x9f ~`).
   - **자동**: 표 하나씩 떼며 `+0x170 KiTryUnwaitThread` → **`AL ≠ 0` 일 때만** `+0x179 SUB [RBX+4],1` → 0 이면 그만. `WaitType 0` 표는 `+0x1e8` (`0x100`, 줄이지 않음 — flow-writer 보탬).
4. **`+0xaa AND [RBX+4],0`** — 모든 길이 지나는 합류점. **끝에 늘 0.**
5. `+0xae` 락 풀기 → `+0xd0 KiExitDispatcher` → `+0xf4 RET`.

#### `KeSetEvent` 와 나란히

| | `KeSetEvent` | `KePulseEvent` |
|---|---|---|
| 한 물건 표 깨우기 | 몸에 풀어 넣음 | 진짜 `KiTryUnwaitThread` 호출 |
| 자동, 깨운 직원 있음 | 끝에 0 | 끝에 0 |
| 자동, 깨울 직원 없음 | **1 로 남음** | **0** |
| 수동 | **1 유지** | **0** |
| 반환 | 옛 값 | 옛 값 |

- 비유: 펄스 = 초록을 **잠깐 깜빡** 켜서 지금 줄에 선 사람만 보내고 (수동이면 전부, 자동이면 한 명) 바로 빨강으로 돌린다. 아무도 없었어도 빨강.
- PE_emulator "깨우고 0 으로" → **맞음**. 수동 전부·자동 하나 구분만 (run 문서 표 5).

### 2.13 syscall 출구 — 돌려준 값이 유저 `RAX` 까지 (질문 6)

#### 다시 짧게 — 출발 메모와 두 문

- 트랩 프레임 = 직원 책상의 **출발 메모** ([run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)). 칸 `Rax +0x30`.
  - 출구에서는 `RBP = 트랩 프레임 + 0x80` 이라 `[RBP-0x50]` = 출발 메모 `Rax` 칸이다 `[정적]`.
- **`SYSRET`** = 들어올 때 `RCX` 주머니에 넣어 둔 **"돌아갈 문" 쪽지대로 나가는 보통 출구** ([run56 해설 2.10](run56-syscall-args-해설.md)).
- **`IRETQ`** = 출발 메모 다섯 칸대로 나가는 **순간 이동** 문 (run15). 2.14 에서 다시 나온다.

#### 값이 지나가는 줄 `[정적]`

| 어디서 | 줄 | 값 |
|---|---|---|
| `KeWaitForSingleObject` | `+0x2c4 MOV EAX,R14D` | `0` · `0x80` · `0x101` · `0x102` · `0xc0` (`0x100` 은 안 나감) |
| `ObWaitForSingleObject` | `+0xba` → `+0xbf MOV EBX,EAX` · `+0xda ObfDereferenceObjectWithTag` → **`+0xdf MOV EAX,EBX`** → `+0xf6 RET` | 그대로 (`RAX` 윗 32비트 0) |
| `NtWaitForSingleObject` | `+0x65` → `+0x6a ADD RSP,0x38` → `+0x6e RET` | **안 건드림** |
| `KiSystemServiceStart` | `+0x193 CALL RAX` → `+0x195` · `+0x1a0 ~ +0x1ae` `RBX`·`RDI`·`RSI` 되살림 | `RAX` 그대로 |
| 〃 | **`+0x25d MOV [RBP-0x50],RAX`** | **출발 메모 `Rax` = 서비스 반환값** (첫 쓰기) |
| 〃 | `+0x268` · `+0x279/+0x284` · `+0x2e9/+0x301` · `+0x316/+0x353` | 도우미를 부를 때마다 칸에 저장했다 다시 읽음 |
| 〃 | **`+0x391 MOV [RBP-0x50],RAX`** | 마지막 저장 (run56 표의 그 줄) |
| 조각 `FUN_1406c20cc` | **`+0x540` (조각 `+0x34`) `MOV RAX,[RBP-0x50]`** | **유저 `RAX` 가 정해지는 곳** |
| 〃 | `+0x5b8 SWAPGS` · **`+0x5bb` (조각 `+0xaf`) `SYSRET`** | 나감 |

- 에러 갈래: 핸들이 잘못이면 `ObWaitForSingleObject +0x50 JS +0xdf` 로 그 에러가 그대로. `ExCrossVmMutantObjectType` 이면 `+0x146` `0xc0000024`.
- 유저 APC 가 대기 중이면 `+0x204` 에 저장 → `+0x248 KiInitiateUserApc` → `+0x257` 다시 읽고 `+0x25b JMP +0x1f2`.
- 비유: 창구 담당 (서비스) 이 건넨 **답** 을 퇴장 담당이 손님 출발 메모 `Rax` 칸에 적는다. 중간에 다른 담당을 부를 때마다 메모에 적었다 다시 읽는다. 마지막에 그 칸을 읽어 **답 봉투** 에 넣고 `SYSRET` 문으로 내보낸다.

#### 서비스 안에서 그 칸에 써도 소용없다 — 단계별로

1. `[정적]` 서비스 (예: `NtWaitForSingleObject`) 는 `+0x193 CALL RAX` **안에서** 돈다.
2. `[정적]` 돌아온 **뒤** `+0x25d` 가 출발 메모 `Rax` 칸에 서비스 반환값을 덮어쓴다.
3. `[정적]` 그 뒤로는 그 칸을 저장·복원만 하고, 마지막에 `+0x540` 이 그 칸을 읽는다.
4. 그래서 → 서비스가 칸에 미리 뭘 써 둬도 `+0x25d` 가 지운다.
5. `[정적]` 커널이 유저 `RAX` 를 바꾸려면 다른 길이어야 한다: `+0x2d3 KiRestoreSetContextState` (`IRETQ`) 나 `+0x248 KiInitiateUserApc`.
6. 두 함수 안에서 칸을 **무엇으로** 채우나는 안 읽었다 `[공백]` → Q141e.

- 결론 `[정적]`: 정상 syscall 이면 **유저 `RAX` = 서비스 반환값**. `NtWaitForSingleObject` 면 `KeWaitForSingleObject` 의 32비트 값.
- run56 에서 달라진 점: run56 은 "`+0x391` 에 두고 조각 `+0x34` 에서 읽는다" 까지였다. 이번에 **첫 쓰기 `+0x25d`** 와 그 사이 저장·복원이 채워졌다.
- PE_emulator: "반환값 = 유저 `RAX`" 는 **맞음**. 출발 메모 `Rax` 칸을 거치게 하면 다른 출구와 틀을 같이 쓸 수 있다 (run 문서 표 6, 2.14).

### 2.14 헷갈리기 쉬운 곳 ① — 로더 끝 `NtContinue` 는 어느 출구인가

결과서와 run 문서가 다르게 적은 곳이다. 천천히 보자.

#### 결과서가 적은 것

- 질문 6: "`NtContinue` 로 새 문맥을 넣을 때 유저 `RAX` 는 `KiRestoreSetContextState` 가 읽는 `TrapFrame.Rax` 칸".
- 최소 규칙: "`NtContinue` 처럼 문맥을 바꾸는 호출은 `SYSRET` 길이 아니라 `KiRestoreSetContextState` 의 `IRETQ` 길 (Q141e)".

#### run 문서가 고친 것 (flow-writer 덧붙임 ① · "다르게" ⑤)

- **로더 끝 `NtContinue` 는 `KiSystemServiceStart` 출구까지 안 온다** `[정적]` (run29).
  - 성공한 `NtContinue` 는 `NtContinueEx` 가 반환하지 않고 **자기 몸에서** `IRETQ` 로 간다.
- 그 길의 유저 `RAX` 도 **같은 출발 메모 `Rax` 칸** 에서 온다 `[정적]` (flow-writer `.asm`):
  - `NtContinueEx +0x3ee` (조각 `FUN_1406ae17a +0x64`) `MOV RAX,[RBP-0x50]`.
  - 칸에 무엇이 들었나는 `KxContextToKframes` 가 맡긴 문맥에서 옮긴 값일 것이다 `[추측: Rax 옮기는 줄은 안 봄]`.
- `KiRestoreSetContextState` (`KiSystemServiceStart +0x2d3`) 는 출구에서 **스레드 헤더 비트 `0x8000000`** 이 설 때만 탄다 (`+0x291 / +0x297`) `[정적]`.
  - 어떤 때 서나 — **남이 문맥을 넣은 경우** (Q141e) 로 보인다 `[추측]`.

#### 왜 결과서 쪽이 아닌가 — 단계별로

1. `[정적]` (run29) 성공한 `NtContinue` 는 `NtContinueEx` 가 `RET` 하지 않고 `IRETQ` 로 바로 유저에 간다.
2. `[정적]` (run56) 그러니 `KiSystemServiceStart +0x195` 로 돌아오지 않는다.
3. `[정적]` `+0x2d3 KiRestoreSetContextState` 는 `+0x195` **뒤** 출구 코드 안에 있다.
4. 그래서 → 로더 끝 `NtContinue` 는 `+0x2d3` 에 닿을 일이 없다. "로더 끝 `NtContinue` = `KiRestoreSetContextState` 길" 은 아니다.

- [run56 해설 2.11](run56-syscall-args-해설.md) "로더 끝 `NtContinue` 는 이 표를 안 쓴다" 와 같은 이야기다.

#### 세 출구 나란히

| 출구 | 언제 | 유저 `RAX` 를 읽는 줄 | 나가는 문 |
|---|---|---|---|
| 보통 출구 | 보통 syscall (`NtWaitForSingleObject` 등) | `KiSystemServiceStart +0x540` (조각 `+0x34`) | `+0x5bb SYSRET` |
| `NtContinueEx` 몸 | 로더 끝 `NtContinue` 성공 (run29) | `NtContinueEx +0x3ee` (조각 `FUN_1406ae17a +0x64`) | `IRETQ` |
| `KiRestoreSetContextState` | 출구에서 스레드 헤더 비트 `0x8000000` 이 설 때 (어떤 때인지 `[추측]`, Q141e) | `KiRestoreSetContextState +0x401` | `+0x416 IRETQ` |

- 줄은 모두 `[정적]`.
- **셋 다 "출발 메모 `Rax` 칸 → `RAX`" 다. 읽는 함수만 다르다.**
- 비유: 문은 셋이지만, 셋 다 나가기 직전에 **출발 메모의 같은 칸** 을 읽어 답 봉투에 넣는다. 누가 읽어 주느냐만 다르다.
- PE_emulator 에게: "출발 메모 `Rax` 칸에 쓰고 마지막에 그 칸에서 읽는다" 틀로 짜면 셋이 같은 틀이다. 로더 끝 `NtContinue` 를 흉내 낼 땐 `NtContinueEx` 쪽 (run29) 을 본다.
- 이 정정은 Q141e 쪽에도 전했다 (`_workspace/plan.md` 결정 기록).

### 2.15 헷갈리기 쉬운 곳 ② — `WaitStatus` 칸에 옛 값이 남는 길

#### 한 줄로

- **잠들었다 깬 길**: 칸 = 돌려준 값.
- **안 자고 바로 끝난 길** (뮤턴트 빼고): 칸을 **안 쓴다** → 칸에 지난번 값이 남는다.

#### 단계별로

1. `[정적]` 잠든 길: `KiSwapThread` 가 칸 (`+0xc8`) 을 읽어 돌려준다 (run75 `+0x25c` · `+0x29a`) → `KiCommitThreadWait` 그대로 → `KeWaitForSingleObject` 반환. **칸과 반환값이 같다.**
2. `[정적]` 안 잔 길 (이벤트·세마포어·스레드 객체): 반환값은 레지스터 `R14D = 0` (`+0x1ad`) 에서만 온다 → `+0x2c4`.
3. `[정적]` `KeWaitForSingleObject` 안에서 `+0xc8` 칸을 쓰는 줄은 **뮤턴트 갈래 둘뿐** 이다 (`+0x416` 0 · `+0x499` OR `0x80`) (flow-writer grep).
4. 그래서 → 이벤트·세마포어·스레드 객체로 바로 끝나면 칸은 **안 바뀐다.** 직전 기다림 값이 남는다.
5. 특히 이런 순서면: `0x100` 으로 깨어나 `+0x891` 로 처음부터 다시 돌고 → 이번엔 신호여서 바로 끝남. 그러면 **함수는 0 을 돌려주는데 칸엔 `0x100` 이 남는다** `[정적: 줄]` + `[추측: 실제 순서]`.

- 비유: 업무 카드 결과 칸은 **잠들었다 깬 날에만** 새로 적힌다. 초록불이라 그냥 지나간 날은 칸을 안 고친다. 그래서 칸엔 **지난번 결과** 가 그대로 있다. (열쇠를 들고 간 날만 예외로 칸을 고친다.)

#### 사진 P2 와 맞춰 보기

- `[관찰]` P2 `WaitStatus 0x100` (2.16 표).
- `0x100` 을 쓰는 줄 후보 `[정적]`: `KiCommitThreadWait +0x4fa` (자기 전에 `WaitRegister.Apc`), 깨우는 쪽 `WaitType 0` 갈래 (`KeSetEvent +0x69a` · `+0x6c4`, `KePulseEvent +0x148` · `+0x1dc`, `KeReleaseMutantEx +0x466` 근처).
- `KeWaitForSingleObject` 였다면 `+0x860` 에서 처음부터 다시 돌았을 것이고, 다시 돌아 즉시 채움 (뮤턴트 아님) 으로 끝났다면 칸에 `0x100` 이 남는다 `[정적: 줄]` + `[추측: 실제 순서]`.
- 어느 줄이 썼는지는 동적 → Q104 (run 문서 동적 7).

#### PE_emulator 에게

- 에뮬레이터가 "깨난 스레드의 결과" 를 **칸에서** 읽는다면, 즉시 채움 때도 칸을 0 으로 써 두거나 반환값을 따로 들고 가야 한다 `[추측: 에뮬레이터 설계]` (run 문서 덧붙임 ③).
- 덤프를 읽을 때도 같다. 이 칸 값을 "마지막 기다림 함수가 돌려준 값" 으로 바로 읽으면 안 된다 (run 문서 "칸 값 ≠ 함수가 돌려준 값 일 수 있다").

### 2.16 사진과 맞춰 보기 — 메인 스레드 기다림 칸

flow-writer 가 run44 덤프 읽기 로그 (`02b KTHREAD` 구획) 를 다시 읽었다. 새 실행은 없다.

- 일곱 장 모두 `dt nt!_KTHREAD ffff828598445080` — **같은 직원** (메인 스레드).
- 사진은 커널 디버거가 **멈춘 순간** 이라 그때 이 직원은 돌고 있다 (`State 2`).
- `WaitRegister` 안쪽 비트와 번호표 안쪽 (`WaitType`·`BlockState`·`WaitKey`·`Object`) 은 **로그에 없다.**

| 칸 | K2 | K3 | P0 | P1 | P1a | P1b | P2 |
|---|---|---|---|---|---|---|---|
| `State (+0x184)` | 0 | 0 | 2 | 2 | 2 | 2 | 2 |
| `WaitStatus (+0xc8)` | 0 | 0 | 0 | 0 | 0 | 0 | **`0x100`** |
| `WaitBlockList (+0xd0)` | 0 | 0 | 0 | **`…984451c0`** | 같음 | 같음 | 같음 |
| `WaitBlockCount (+0x24b)` | 0 | 0 | 0 | **1** | 1 | 1 | 1 |
| `WaitReason (+0x283)` | 0 | 0 | 0 | 0 | **`0x1f`** | **6** | 0 |
| `WaitMode (+0x187)` | 0 | 0 | 0 | 0 | 0 | **1** | 0 |
| `MiscFlags.Alertable` | 0 | 0 | 0 | 0 | 0 | 0 | **1** |
| `AdjustReason (+0x236)` | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| `AdjustIncrement (+0x237)` | 0 | 0 | 0 | 0 | 0 | **1** | 0 |
| `KernelWaitTime (+0x3e0)` | 0 | 0 | 0 | 1 | 1 | 5 | 5 |
| `UserWaitTime (+0x3e8)` | 0 | 0 | 0 | 0 | 0 | **2** | 2 |
| `AbWaitObject` · `WaitPrcb` | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| (참고) `ContextSwitches (+0x154)` | 0 | 0 | 1 | `0x17` | `0x1c` | `0x2a` | `0x31` |

모두 `[관찰]`. 로그 줄 번호는 run 문서 표에 있다.

#### P1 부터: 번호표는 자기 0번 표 — 단계별로

1. `[관찰]` P1 부터 `WaitBlockList = 0xffff8285984451c0` = 직원 + `0x140` (`WaitBlock[0]`), `WaitBlockCount 1`.
2. `[정적]` `KeWaitForSingleObject +0x19f LEA R9,[RDI+0x140]` · `+0x7d2 WaitBlockCount = 1` → `KiCommitThreadWait +0x5b` 가 쓰는 자리와 같다.
3. `[정적]` `+0x5b` 는 `KiCommitThreadWait` 까지 간 기다림에서만 쓰인다.
4. 그래서 → **P0 ~ P1 에 한 번 이상, 실제로 번호표를 걸고 커밋하러 간 기다림이 있었다** `[추측: 읽기 — 다른 기다림 함수도 같은 칸을 쓸 수 있음]`.
5. 같은 구간 `KernelWaitTime` 0 → 1 (깨우는 쪽이 더하는 칸, `KiTryUnwaitThread +0x114 ~ +0x142`) 과도 어긋나지 않는다.

- [run75 해설 2.12](run75-thread-switch-해설.md#212-사진과-맞춰-보기--메인-스레드-업무-카드-일곱-장) 의 "P0 → P1 사이 `KiSwapThread` (기다림 쪽) 를 딱 한 번 탔다" 읽기와도 어긋나지 않는다 `[추측: 이 해설이 이어 읽은 것]`.

#### P1a `WaitReason 0x1f` — run75 "교대 5번" 의 마지막 한 번 — 단계별로

1. `[관찰]` P1a `WaitReason 0x1f` (`WrDispatchInt`).
2. `[정적]` 이 칸에 상수 `0x1f` 를 쓰는 줄은 뽑은 ntoskrnl `.asm` **513개 중 `KiDispatchInterrupt +0x13e` 하나** 다 (flow-writer `grep`). 초인종 선점 갈래다 (run76 해설 2.14 표의 "초인종 선점" 문).
   - 다른 값: `KiExitDispatcher +0x187` · `KiCheckForThreadDispatch +0xea` 는 `0x20`, `KiQuantumEnd +0xb98` 은 `0x1e`.
3. 그래서 → run75 "P1 → P1a 교대 5번" 중 **마지막 한 번은 초인종 선점 쪽** 으로 읽힌다 `[추측: 읽기]`.
4. 한계:
   - 레지스터 값으로 쓰는 줄 (`KeWaitForSingleObject +0x1b0` 등 인자 그대로) 은 grep 에 안 잡힌다.
   - 앞 네 번은 칸이 덮여 모른다.
   - 결과서가 짚은 `KiExitDispatcher +0x1ac` 길 (2.10) 은 **앞 네 번의 후보로만** 남는다.

#### P1b — 유저 기다림에서 `Increment 1` 로 깨워진 꼴

- `[관찰]` `WaitReason 6` (`UserRequest`) · `WaitMode 1` · `UserWaitTime` 0 → 2.
  - `[정적]` 유저가 부른 기다림 syscall 의 꼴이다: `ObWaitForSingleObject +0xb5 EDX = 6` · `NtWaitForSingleObject +0x16 / +0x1f` (`WaitMode` = `PreviousMode`) · `KiTryUnwaitThread +0x114 ~ +0x142` (`WaitMode` 로 `UserWaitTime` 갈래).
  - 같은 구간에 로더가 `LdrpDrainWorkQueue` → `NtWaitForSingleObject` 로 일꾼을 기다리는 길 (run24, flow.md 2.6) 과 어긋나지 않는다 `[추측: 읽기]`.
- `[관찰]` `AdjustIncrement 1` · `AdjustReason 0`.
  - 깨우는 쪽이 `Increment` 를 적는 `KiProcessThreadWaitList +0x9f` 와 맞는다 → P1a ~ P1b 에 **`Increment 1` 로 깨워졌다** `[추측: 읽기]`.
  - `AdjustReason` 은 `+0x98` 이 1 로 쓰지만 사진은 0. run75 의 `KiDeferredReadySingleThread +0x26d` (둘 다 0 으로 되돌림) 와 맞는 순서다 `[추측: 읽기]`.
  - 같은 구간의 `DecayBoost` 0 → 1 · `Priority` 8 → 9 (run76 `[관찰]`) 의 출처 후보다. 깨움 가산점 계산 본체는 안 읽었다 `[추측]`.
  - P2 `AdjustIncrement 0` 은 그 뒤 다른 깨움이 덮은 것 `[추측]`.

#### P2 — `WaitStatus 0x100` · `Alertable 1`

- `[관찰]` 칸에 남은 마지막 결과가 `STATUS_KERNEL_APC` 다. 왜 남을 수 있는지는 2.15.
- `Alertable` 은 기다림마다 `+0x112` 끔 / `+0x126` 켬 으로 다시 쓰는 비트 `[정적]` → **마지막 기다림은 Alertable 이었다** `[추측: 읽기]`.

#### `AbWaitObject` · `WaitPrcb` 일곱 장 모두 0 `[관찰]`

- 깨울 때 둘 다 0 으로 되돌리는 줄 (`KiTryUnwaitThread +0x16f` · `+0x100`, `KeSetEvent +0x48b`, `KeWaitForSingleObject +0x859`) 과 맞는다.

#### 디버거 영향

- 사진은 커널 디버거가 멈춘 순간이다. 멈출 때마다 교대·기다림 숫자가 조금 달라질 수 있다 `[추측: 디버거 영향]` (run75 해설 2.12 와 같은 주의).

### 2.17 PE_emulator 에게 — 최소 규칙 + 덧붙임 넷

결과서 끝 "흉내 낼 최소 규칙" 을 run 문서가 그대로 옮겼다. CPU 하나·한 번에 한 직원 에뮬레이터 기준이다. IRQL·헤더 락·스레드 락·`WaitRegister`·RCU·자동부스트는 생략 가능 `[추측: 에뮬레이터 범위]`. 짧게:

**기다리는 쪽 (한 물건)**

1. 먼저 `State = Waiting`, `WaitReason`·`WaitTime` 적기.
2. 신호면 **그 자리에서** 가져가고 `State = Running`, **안 자고** `0` 반환.
   - 자동 이벤트 0 · 세마포어 −1 · 수동 이벤트·스레드 객체 그대로.
   - 뮤턴트: 비었으면 −1·주인 = 나·꾸러미에 (`Abandoned` 면 끄고 `WaitStatus = 0x80`) / 주인이 나면 재귀 −1 / 남이면 기다림.
3. 마감이 이미 지났으면 `0x102`. (같은 시각은 Q141c2. "0 이하면 바로 `0x102`" 로 줄여도 된다 `[추측]`.)
4. 아니면 번호표 (`WaitType 1`, `WaitKey 0`) 를 줄 **맨 뒤** 에 걸고 잔다 (마감이면 타이머 — Q141c2).
5. 깨어나면 **`WaitStatus` 를 그대로 반환.** `0x100` 이면 1번부터.

**깨우는 쪽**

- 번호표를 **앞에서부터**, `State == Waiting` 인 직원만. 깨움 = ① 표 떼기 ② `WaitStatus = WaitKey` ③ `State = DeferredReady` + 깨운 직원 목록 ④ `AbWaitObject = 0`.
- `KeSetEvent` 자동: 1 로 → **성공한 첫 하나만** → 0. 없으면 1. 반환 = 옛 값.
- `KeSetEvent` 수동: 1 로, **전부**, 1 유지. 이미 1 이면 아무도 안 깨움.
- `KePulseEvent`: 이미 신호가 아니면 1 로 → 같은 깨움 → **끝에 늘 0**. 반환 = 옛 값.
- 뮤턴트 놓기: 주인 아니면 `0xc0000046` (`Abandoned` 면 `0x80`). 주인이면 +1, 1 미만이면 끝. 1 이면 꾸러미에서 떼고 `OwnerThread = 0` → **성공한 첫 직원에게 그 자리에서 넘김** (주인·−1 = 0·꾸러미·`Abandoned` 면 `|0x80`). 없으면 1.
- `WaitType 0` (전부 기다림) 은 `WaitStatus = 0x100` 으로 깨워 깨난 쪽이 다시 보게 한다 (test.exe 엔 안 나옴).
- 깨운 직원들은 준비 줄로 (보통 뒤). 깨운 쪽이 IRQL < 2 이고 `NextThread` 가 정해졌으면 **깨운 쪽이 그 자리에서 넘어간다** (`KiExitDispatcher +0x1ac`).

**syscall 출구**

- 서비스 반환값 = 유저 `RAX`. 출발 메모 `Rax` 칸에 쓰고 마지막에 그 칸에서 읽으면 다른 출구와 같은 틀.
- `NtContinue` 는 → **덧붙임 ① 으로 고쳐 읽기** (2.14).

flow-writer 덧붙임 (넣을지는 PE_emulator 판단):

- ① 로더 끝 `NtContinue` 는 `KiRestoreSetContextState` 길이 아니다. `NtContinueEx` 몸의 `IRETQ`, `RAX` 는 `NtContinueEx +0x3ee` 가 같은 칸에서 (2.14).
- ② 깨운 쪽이 그 자리에서 넘어갈 때 (`KiExitDispatcher +0x6c` → `+0x1ac`) 깨운 쪽 `WaitReason = 0x20` (`+0x187`) `[정적]`. 사유 칸 하나로 "마지막에 어느 문으로 계산대를 내놨나" 를 대충 가를 수 있다 (2.10 표, 2.16 P1a).
- ③ 즉시 채움 갈래는 `WaitStatus` 칸을 안 쓴다 → 칸에 옛 값이 남을 수 있다 (2.15).
- ④ `WaitRegister.State`·`BlockState` 숫자에 PDB 열거형 이름이 있다 (`_KWAIT_STATE`, `_KWAIT_BLOCK_STATE` 4 `WaitBlockActive` · 5 `WaitBlockInactive` 등). 칸과의 연결은 `[추측: 이름]` (2.6). `_WAIT_TYPE` 은 PDB 에 없다.

"지금 우리" 판정 (run 문서 표):

| 무엇 | 판정 |
|---|---|
| 들어갈 때 신호면 안 잔다 | 없으면 **모자람** → 추가 |
| "잠듦 표시 → 넘김" | **맞음** (순서만: `Waiting` 먼저, 신호면 되돌림) |
| 깨난 쪽이 다시 묻는다 | **틀림** (`0x100` 하나만 다시) |
| 수동 이벤트 전부·유지 | **맞음** |
| 자동 이벤트 | "첫 하나만 + 그 자리에서 0" **추가** |
| `WaitStatus` 쓰기 | 깨우는 쪽이 → **추가** |
| 깨우는 함수 | 이름만 다름. "표시 → 준비 줄" 두 단계면 됨 |
| `KiTryUnwaitThread` 경쟁 | 락 불필요 `[추측: 에뮬레이터 범위]`, "이미 깨운 직원 지나침" 은 **필요** |
| 뮤턴트 "깨난 쪽이 가져감" | **틀림** — 놓는 쪽이 넘김 |
| 버려진 뮤턴트 | 없으면 **추가** |
| `KePulseEvent` "깨우고 0" | **맞음** (수동 전부·자동 하나 구분만) |
| 반환값 = 유저 `RAX` | **맞음** |

### 2.18 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

run 문서 ① ~ ⑩ (오케스트레이터·flow-writer `.asm` 확인):

- ① 결과서 1(b) 의 `+0x1f2` · `+0x23d` → 둘 다 **`KiCommitThreadWait`** 오프셋 (2.6).
- ② 결과서 "`+0x846 MOV R14,RAX`" → `+0x846` 은 ret 자리 (`XOR R10D,R10D`), `MOV R14,RAX` 는 **`+0x849`** (2.7).
- ③ 결과서 `[추측: KeInitThread, 안 확인]` "번호표 `Thread` 칸은 미리 채워 둔다" → 이미 **`[정적]`** (`KeInitThread +0x69 ~ +0x7e`, run52, 짐 지도 3.14 `WaitBlock*` 줄) (2.3).
- ④ 결과서 "못 닫은 것" `WaitRegister.State` 이름 → PDB `_KWAIT_STATE`, 연결은 `[추측: 이름]` (2.6). `BlockState` 도 `_KWAIT_BLOCK_STATE`. `_KTHREAD_STATE`·`_KWAIT_REASON` 숫자도 PDB 로 맞춤.
- ⑤ 결과서 "`NtContinue` → `KiRestoreSetContextState` 의 `IRETQ`" → 로더 끝 `NtContinue` 는 `NtContinueEx` 가 직접 `IRETQ`, `RAX` 는 `NtContinueEx +0x3ee` (2.14).
- ⑥ 보탬: `KePulseEvent` 자동 루프의 셋째 `KiTryUnwaitThread` **`+0x1e8`** (ret `+0x1ed`, `WaitType 0`, `0x100`) (2.12).
- ⑦ 보탬: `+0x104` 뒤 기다림마다 다시 쓰는 칸 (`+0x112`·`+0x116`·`+0x11a`·`+0x126`), `+0x19f` 0번 표, `WaitTime` = KUSD `TickCount`, 그리고 즉시 채움 갈래는 `WaitStatus` 칸을 안 씀 (2.5 · 2.15).
- ⑧ 보탬: `KiResumeThread +0x98 / +0x9d` 가 `WaitKey`·0 을 넘김 → 첫 스레드가 받는 값 0 (2.9).
- ⑨ 보탬: 사진 대조 전체 (2.16). P1a `0x1f` 로 결과서의 `KiExitDispatcher +0x6c` 후보는 "마지막 한 번" 에는 안 맞는다.
- ⑩ `_calls.tsv` 의 `alias:` callee 둘 → PDB 이름으로: `ObWaitForSingleObject +0xba` 는 `KeWaitForSingleObject`, `KeWaitForSingleObject +0xbce` · `KeReleaseMutantEx +0x515` 는 `RtlRaiseStatus` (Ghidra `ExRaiseStatus`). 글에는 "상태 값으로 예외를 던지는 함수" 로.

- 정정은 없다 — run75·run76 이 틀린 곳을 이번 바퀴가 드러내지 않았다 (run 문서).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 | … run76 까지 | + run77 (기다림·깨움 16개 처음 뽑음, `KiSystemServiceStart` 출구 읽기, PDB 열거형 넷 확인) |
| 기준 — 사진 메모 | run76 줄까지 | + run77 줄: 메인 스레드 `02b KTHREAD` 기다림 칸 다시 읽음. `WaitRegister`·번호표 안쪽은 로그에 없음. 새 실행 없음 |
| 2.2 제목 | … run76 | + run77 |
| 2.2 "시스템 콜 입구·출구" 표 아래 | `+0x391` 출발 메모 `Rax` 에 둠 → 조각 `+0x34` (run56) | + run77 세 줄 `[정적]`: 첫 쓰기 `+0x25d` · 저장·복원 · 마지막 `+0x391` · 읽기 `+0x540` · `SYSRET` `+0x5bb` / 서비스 안 쓰기는 덮임, 바꾸는 길 둘 / `NtContinueEx +0x3ee` 도 같은 칸 / 예 `NtWaitForSingleObject` |
| 2.2 `nt!KiResumeThread` 절 | `KiTryUnwaitThread` 로 깨움 (run13) | + 넘기는 값 `+0x98 WaitKey` · `+0x9d 0`, 첫 스레드가 `KiSchedulerApc +0xf7` 에서 받는 값 **0** `[정적: 줄]` |
| 3장 "스레드 넘기기" 사진 줄 (P1 → P1a 5번) | 몫 끝 길은 아닌 쪽 `[추측: 읽기]` (run76) | + 후보 `KiExitDispatcher +0x6c` → `+0x1ac` `[추측]`, 단 **마지막 한 번은 선점** (`KiDispatchInterrupt +0x13e`) `[추측: 읽기]` |
| 3장 **새 절** "기다림·깨움과 syscall 출구" | — | 택배 비유 + 트리 + `[정적]` 다섯 + 사진 `[관찰]` + 동적 → Q104 |
| 4장 run74 · run76 정적 줄 | 대기열 번호 없음 | **Q142** · **Q144** |
| 4장 run75 동적 6 · 정적 줄 | P1 → P1a 5번 모름 · 기다림·깨움은 Q141c | + run77 후보 한 줄 · "(해결됨, Q141c1 → run77)" |
| 4장 run76 동적 5 | `DecayBoost` 1 출처 | + P1b `AdjustIncrement 1` `[관찰]` 한 줄 |
| 4장 새 줄 셋 (run77) | — | 동적 · 정적 남은 것 ① ~ ④ · 결과서와 다르게 ① ~ ⑩ |
| 5장 | — | run77 두 줄 |

태그가 바뀐 곳:

- PE_emulator `[추측]` "깨움 = 표시만, 깨난 쪽이 다시 물음" → **틀림** `[정적]`.
- run75 "`WaitStatus` 를 누가 어떤 값으로 쓰나 → Q141c" → **깨우는 쪽이 번호표 `WaitKey` 로** `[정적]`.
- run13 "`KiTryUnwaitThread` 로 깨움" → + 넘기는 값과 첫 스레드가 받는 값 0 `[정적: 줄]`.
- run56 "`+0x391` 출발 메모 `Rax`" → 첫 쓰기 `+0x25d` 부터 `[정적]`.
- 결과서 `[추측]` 번호표 `Thread` 칸 → 이미 `[정적]` (run52).
- 결과서 "`WaitRegister.State` 이름 없음" → `_KWAIT_STATE` `[추측: 이름]` (숫자 뜻은 맞음).
- run75 `[추측]` "P1 → P1a 5번은 `KiSwapThread` 아닌 길" → 마지막 한 번은 선점 `[추측: 읽기]`.
- run76 `[추측]` "P1b `DecayBoost 1` 은 깨움 쪽" → `AdjustIncrement 1` `[관찰]` 로 더 기운다 `[추측: 읽기]`.
- 결과서 "`NtContinue` → `KiRestoreSetContextState`" → `NtContinueEx` 몸의 `IRETQ`, `RAX` 는 `+0x3ee` `[정적]`.

### 짐 지도 · run75·run76 문서에서 바뀐 것

- 짐 지도 (`baggage.md`): **새 짐 칸 없음**, 2장 숫자 그대로. 짐 지도 바퀴가 아니다 — 주제가 특정 단계의 짐이 아니라 기다리고 깨우기라서.
  - 3.14 `_KTHREAD` 머리에 "Q78d4 에서 볼 것 — 기다림·깨움 줄": 7.2 `_KTHREAD` 모름 47 중 기다림 칸 **13** (`State`·`WaitTime`·`WaitRegister`·`WaitBlockList`·`WaitBlockFill4`·`WaitBlockCount`·`KernelWaitTime`·`WaitReason`·`WaitMode`·`AdjustIncrement`·`UserWaitTime`·`MiscFlags.Alertable`·`WaitStatus`) 의 **쓰는 줄 후보**.
    - 칸 수는 안 바꿨다. 기다릴 때마다·깨울 때마다 다시 쓰는 칸이라 "처음 값 한 줄" 로 못 닫는다.
  - 3.14 `WaitBlock*` 줄 근거 칸에 한 마디: 한 물건 기다림이면 `KeWaitForSingleObject +0x19f` · `+0x1d8 ~ +0x1e7` 이 `WaitBlock[0]` 을 쓴다 `[정적]`, 사진 `WaitBlockList` = `+0x140` `[관찰]`.
  - 7.2 `_KTHREAD` 가리킴 줄에 한 마디.
- run75 문서: "아직 확인 필요" 6 (P1 → P1a 5번) 에 run77 후보 한 줄, "다음 바퀴 후보" 끝에 "(해결됨, Q141c1 → run77)".
- run76 문서: "아직 확인 필요" 5 (`DecayBoost` 출처) 에 P1b `AdjustIncrement 1` 한 줄, "다음 바퀴 후보" 에 대기열 번호 **Q144**.

### 큰 그림에서

- **지도 쪽**:
  - run75 가 "`KiSwapThread` 는 `WaitStatus` 를 돌려준다" 까지 봤다. 이번에 **그 칸을 누가 쓰나** (깨우는 쪽, 번호표 `WaitKey`) 가 닫혔다.
  - 교대 담당을 부르는 문에 **"깨운 직후" (`KiExitDispatcher +0x1ac`)** 가 하나 더해졌다 (2.10 표).
  - run13 첫 스레드 멈춤 풀기에서 첫 스레드가 받는 값 (0) 까지 이어졌다.
  - run56 syscall 출구에서 유저 `RAX` 줄이 처음 쓰기부터 마지막 읽기까지 이어졌다.
- **PE_emulator 쪽**:
  - "표시만, 다시 묻기" 를 **"깨우는 쪽이 그 자리에서 채우기"** 로 고칠 근거가 생겼다.
  - 이벤트 (자동·수동)·세마포어·뮤턴트·펄스·syscall 출구까지 최소 규칙으로 정리됐다.
  - `NtContinue` 쪽은 결과서 그대로 쓰면 안 되고 덧붙임 ① 로 고쳐 읽어야 한다.
- **사진 쪽**:
  - run75 "P1 → P1a 교대 5번" 이 하나 더 좁혀졌다. 마지막 한 번은 선점 쪽 `[추측: 읽기]`. 앞 네 번은 깨운 직후 길이 후보.
  - run76 의 `DecayBoost 1` 출처가 깨움 가산점 쪽으로 기운다 `[추측: 읽기]`.
- **다음** (같은 요청 25):
  - **Q141c2** (타이머): 이번에 이름만 본 `KiCommitThreadWait +0x1f2 KiInsertTimerTable` · `+0x39e KiTimerWaitTest` 쪽. 마감 지난 기다림을 누가 어떻게 끝내나.
  - **Q141d** (끝내기): test.exe `WaitForSingleObject(thread)` 를 실제로 깨우는 쪽 — 스레드 객체가 신호를 받는 길.
  - **Q141e** (남의 문맥): `KiRestoreSetContextState`·`KiInitiateUserApc` 안에서 출발 메모 `Rax` 칸을 무엇으로 채우나.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe `WaitForSingleObject(thread, INFINITE)` 가 실제로 번호표 걸기 → `KiSwapThread` 까지 가나, 커밋 전에 끝난 길 (`+0x251`) 은 안 타나, `0x100` 으로 한 번이라도 돌아와 반복하나 | 실행 때 일 | **Q104** 동적 1 (`nt!KeWaitForSingleObject+0x7b7` · `nt!KiCommitThreadWait+0x220` · `+0x251` · `KeWaitForSingleObject+0x860` 의 `r14d`) |
| `PRCB+0x2d08` 이 정말 "깨운 직원 명단" 인가 | 이름 없는 칸 (공개 PDB `_KPRCB` 잘림) | **Q104** 동적 2 (push 직후·`KiProcessThreadWaitList` 입구에서 `dq @$prcb+0x2d08 L1`) |
| `KiExitDispatcher +0x6c` 둘째 넘기기 길을 실제로 타나, run75 "P1 → P1a" 앞 네 번이 이 길인가 | 실행 때 일. 사진은 마지막 한 번 사유만 남김 | **Q104** 동적 3 (`nt!KiExitDispatcher+0x1ac` 에서 `k`) |
| 서비스 반환 `RAX` = 유저 `RAX` 를 실제로 확인, 서비스 안에서 출발 메모 `Rax` 에 쓰는 곳 | 정적으론 줄만 | **Q104** 동적 4 (`+0x195` `rax` vs `+0x540` 뒤 `rax`·`dq @rbp-0x50 L1`, `ba w8 @rbp-0x50`) |
| `WaitRegister.State` 실제 값 (커밋된 기다림이 1 인가 4 인가, 5·3·6 은 언제) | 실행 때 값. 칸과 열거형 연결은 `[추측: 이름]` | **Q104** 동적 5 (`nt!KiCommitThreadWait+0xbf`) |
| 스레드 객체 기다림의 `WaitStatus` 는 누가 쓰나 | 스레드 객체가 신호 받는 쪽은 이번 범위 밖 | 정적 **Q141d** · **Q104** 동적 6 (`KeWaitForSingleObject+0x846` `rax`, `ba w8 <메인 스레드>+0xc8`) |
| 사진 P2 `WaitStatus 0x100` 을 쓴 줄, 그 뒤 처음부터 다시 돌았나 | 사진은 값만 보여 준다 | **Q104** 동적 7 (`ba w8 <메인 스레드>+0xc8` 에서 `k`) |
| 사진 P1b `AdjustIncrement 1` 을 넘긴 쪽 (`NtSetEvent` 쪽인가) | 사진은 값만. `NtSetEvent` 안 읽음 | **Q104** 동적 8 (`nt!KiProcessThreadWaitList+0x9f` `r8b`·`k`) · 정적 **Q145 ①** |
| `NtSetEvent`·`NtReleaseMutant`·`NtPulseEvent` 가 넘기는 `Increment`·`Wait` (`Wait` 면 `KiDirectSwitchThread` 갈래) | 이번 대상 아님 | 정적 **Q145 ①** — PE_emulator 팀원 syscall 몸과 바로 닿는 곳 |
| `KeWaitForMultipleObjects` — `WaitKey` 번호 (`STATUS_WAIT_0 + n`), `WaitAll` (`WaitType 0`) 번호표, 블록 수 > 3 일 때 블록 배열 | 이번 대상 아님 `[공백]` | 정적 **Q145 ②** |
| 깨움 가산점 계산 (`KiDeferredReadySingleThread +0x6d` 갈래가 `DecayBoost`·`Priority` 를 어떻게), 깨난 직원 줄 앞/뒤 | 계산 본체 안 읽음. 앞/뒤는 run75 한계 그대로 `[추측]` | 정적 **Q145 ③** (Q144 ③ 과 묶을 만함) |
| `KiTryUnwaitThread` `R9 ≠ 0` 갈래 (`+0x237 ~ +0x2e0`) 를 부르는 곳 | caller 32곳 중 안 본 쪽 `[공백]` | 정적 **Q145 ④** |
| `KiInitiateUserApc`·`KiRestoreSetContextState`·`KeContextToKframes` 안에서 출발 메모 `Rax` 를 무엇으로, 스레드 헤더 비트 `0x8000000` 이 언제 서나, `NtContinueEx` 길 칸에 무엇이 들었나 | 안 읽음 `[공백]` / `[추측]` | 정적 **Q141e** |
| 마감 지난 기다림 끝내기 (`KiInsertTimerTable` · `KiTimerWaitTest` · `KiComputeDueTime`), 같은 시각 마감 | 이번 범위 밖 | **Q141c2** (다음 바퀴) |
| `PRCB+0x2d08` 등 칸의 진짜 이름 | 공개 PDB 의 `_KPRCB` 가 `+0x6d0` 에서 잘림 | 대기열 없음 (`[추측: 이름]` 으로 둠) |

- 추천 멈춤점 (run 문서, 모두 `nt!`, WinDbg 이름은 `nt!KeWaitForSingleObject`): `KeWaitForSingleObject+0x1a6` (`rdi`) · `+0x7b7` · `+0x841` · `+0x846` (`rax`) · `+0x860` (`r14d`), `KiCommitThreadWait+0xbf` · `+0x251` · `+0x220`, `KeSetEvent+0x484` / `+0x2ec` / `+0x4d6` / `+0x171` (`r13`) · `+0xd1` · `+0x10c`, `KiTryUnwaitThread` 입구 (`r8`) · `+0x168`, `KeReleaseMutantEx+0x330` (`rdx`) · `KiWaitSatisfyMutant+0x40`, `KiProcessThreadWaitList+0x101` · `KiExitDispatcher+0x1ac`, `KiSystemServiceStart+0x193` / `+0x195` · `+0x25d` · `+0x391` · `+0x540` · `+0x5bb`.
  - 커널 멈춤점은 모든 프로세스에 걸린다. test.exe 스레드일 때만 서게 조건을 붙여야 한다.

---

## 5. 더 읽기

- run 문서: [`run77-thread-wake.md`](run77-thread-wake.md) — "PE_emulator 쪽에서 쓸 것" 표·최소 규칙·**flow-writer 덧붙임 ① ~ ④**, 호출 지점 표, 기다리는 쪽 · 깨우는 쪽 · 뮤턴트 · syscall 출구 절, 호출 관계 그림, **사진과 맞춰 본 것**, **결과서와 다르게 적은 곳 · 보탠 것**
- 결과서: [`_workspace/sast/73_thread-wake_result.md`](../../_workspace/sast/73_thread-wake_result.md) — "한눈에" 그림, 질문별 답, 최소 규칙, 동적 확인 목록·추천 멈춤점 (`NtContinue` 줄은 run 문서 덧붙임 ① 로 고쳐 읽기)
- 요청서: [`_workspace/sast/73_thread-wake.md`](../../_workspace/sast/73_thread-wake.md)
- flow.md: [3장](flow.md) 새 절 "기다림·깨움과 syscall 출구 — `nt!KeWaitForSingleObject` → …" · 같은 장 "스레드 넘기기" 절 run77 줄 · [2.2](flow.md) "시스템 콜 입구·출구" 표 아래 run77 세 줄 · `nt!KiResumeThread` 절 run77 줄 · 4장 run77 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.14 머리 "Q78d4 에서 볼 것 — 기다림·깨움 줄", 3.14 `WaitBlock*` 줄
- 앞 이야기: [run13 해설](../02_sprint/run13-resume-thread-해설.md) (신호등·번호표·어깨 두드리기·경주) · [run75 해설](run75-thread-switch-해설.md) (고르기·줄 세우기·교대·`WaitStatus` 돌려주기) · [run76 해설](run76-thread-quantum-해설.md) (사유 칸·교대 문·초인종) · [run56 해설](run56-syscall-args-해설.md) (보통 출구 `SYSRET`, 로더 끝 `NtContinue`) · [run29 해설](../02_sprint/run29-continue-start-해설.md) (`NtContinueEx` 의 `IRETQ`) · [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) (출발 메모·순간 이동) · [run52 해설](run52-kthread-create-해설.md) (업무 카드·번호표 4장) · [run24 해설](../02_sprint/run24-work-queue-해설.md) (로더가 일꾼을 기다리는 길)

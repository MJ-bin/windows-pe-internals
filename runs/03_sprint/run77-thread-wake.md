# 2026-11-12 run77 — 기다림·깨움과 syscall 출구 (Q141c1, PE_emulator 요청 25 ㄷ-1·ㄷ-3): **깨우는 쪽이 그 자리에서 기다림을 채운다.** `KeSetEvent` 는 신호 (1) 를 먼저 적고 블록마다 깨난 스레드의 `WaitStatus` 를 **블록 `WaitKey`** 로 쓰며 (`+0x484` 등), 자동 재설정이면 깨움에 성공한 첫 하나 뒤 **그 자리에서 0** (`+0xd1`). 뮤턴트는 놓는 쪽 `KeReleaseMutantEx +0x330 KiWaitSatisfyMutant` 가 **주인을 넘긴다** (`+0x40`). 깨난 쪽 `KeWaitForSingleObject` (Ghidra `KeWaitForMutexObject`) 는 `KiCommitThreadWait +0x220 KiSwapThread` 에서 받은 `WaitStatus` 를 **그대로** 돌려주고, `0x100` 일 때만 `+0x891` 로 처음부터 다시. 유저 `RAX` 는 출구 `KiSystemServiceStart +0x25d` 가 트랩 프레임 `Rax` 에 적었다가 `+0x540` 에서 다시 읽어 `+0x5bb SYSRET`

## PE_emulator 쪽에서 쓸 것

- **한 줄**: 지금 우리 nt 엔진의 "깨움 = 실행 가능 표시만, 깨난 스레드가 자기 syscall 을 **다시 물어** 결과를 받음" 은 **틀림** `[정적]`.
  - 진짜는 **깨우는 쪽이 그 자리에서** 기다림을 채운다: 자동 재설정 이벤트를 0 으로 · 뮤턴트 주인을 넘김 · 깨난 스레드의 `WaitStatus` 를 씀.
  - 깨난 쪽은 **다시 묻지 않고** 그 `WaitStatus` 를 그대로 돌려준다. 다시 검사하는 건 값이 `0x100` (`STATUS_KERNEL_APC`) 일 때 하나뿐.
  - 기다리러 들어갈 때 이미 신호 상태면 **안 자고** 그 자리에서 가져간다 (이벤트 0 · 세마포어 −1 · 뮤턴트 주인 = 나).
  - 유저가 받는 `RAX` = 서비스 반환값 그대로 (`NtWaitForSingleObject` 는 `KeWaitForSingleObject` 의 32비트 값).
- run44 사진의 test.exe 메인 스레드 값 (`[관찰]`, 새 실행 없음 — 아래 "사진과 맞춰 본 것"):
  - `WaitBlockList` 가 P1 부터 **스레드 자신의 `WaitBlock[0]`** (`+0x140`), `WaitBlockCount 1` — 한 객체 기다림이 쓰는 블록과 같은 자리.
  - P1b 는 `WaitReason 6` (`UserRequest`) · `WaitMode 1` · `AdjustIncrement 1` — 유저 syscall 로 기다렸다가 `Increment 1` 로 깨워진 꼴 `[추측: 읽기]`.
  - P2 는 `WaitStatus 0x100` — 칸에 남은 마지막 값이 `STATUS_KERNEL_APC`. **칸 값 ≠ 함수가 돌려준 값** 일 수 있다 (아래 덧붙임 ③).
- 질문마다 "지금 우리" (결과서 sast/73 그대로, 근거 줄은 오케스트레이터·flow-writer 가 `.asm` 으로 다시 봄):

| 질문 | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1(a) 이미 신호면 | **그 자리에서 가져가고 안 잔다.** `State = 5` 를 먼저 적고 (`+0x1a6`) 헤더 락 뒤 검사 → 자동 재설정 이벤트 `SignalState = 0` (`+0x645`), 세마포어 −1 (`+0x602`), 뮤턴트 비었으면 `OwnerThread = 나` (`+0x489`), **수동 이벤트·스레드 객체는 안 바꿈** → `State = 2` (`+0x251`) → `STATUS_WAIT_0` | 없으면 **모자람** — "신호면 안 잔다" 추가 | `KeWaitForSingleObject +0x1a6` · `+0x1eb` · `+0x205` · `+0x208/+0x20b` · `+0x211/+0x213` · `+0x5f0 ~ +0x5f9` · `+0x645` · `+0x5ff/+0x602` · `+0x3e3` · `+0x408 ~ +0x410` · `+0x416` · `+0x489` · `+0x246` · `+0x251` · `+0x2c4` |
| 1(b) 신호가 아니면 | 자기 `WaitBlock[0]` (`WaitType 1` · `BlockState 4` · `WaitKey 0` · 객체) 를 객체 기다림 줄 **맨 뒤** 에 달고 (`+0x7b7 ~ +0x7c1`) → `+0x841 KiCommitThreadWait` → `WaitRegister.State = 1` (`+0xbf`) → 마감이 있으면 `KiCommitThreadWait +0x23d` `TimerActive` · **`KiCommitThreadWait +0x1f2 KiInsertTimerTable`** → **`KiCommitThreadWait +0x220 KiSwapThread`** | **맞음** ("잠듦 표시 → 넘김"). 순서만: `Waiting` 을 먼저 적고 신호면 되돌림 | `KeWaitForSingleObject +0x19f` · `+0x1d8 ~ +0x1e7` · `+0x7b7` · `+0x7c1` · `+0x7c5` · `+0x7d2` · `+0x837` · `+0x841`, `KiCommitThreadWait +0x38/+0x3b` · `+0x23d` · `+0x5b` · `+0xa3` · `+0xbf` · `+0x160` · `+0x1f2` · `+0x220` |
| 1(c) 깨난 뒤 | `KiCommitThreadWait` 는 `KiSwapThread` 반환값 (= `WaitStatus`, run75) 을 **손 안 대고** 반환 (`+0x225 ~ +0x23b`). `KeWaitForSingleObject` 는 **`0x100` 이면 `+0x891 JMP +0x104`** (처음부터 다시 검사), 아니면 `+0x2c4` 그대로 (하위 32비트) | "깨난 쪽이 다시 묻는다" → **틀림** (`0x100` 하나만 다시) | `KiCommitThreadWait +0x225` · `+0x228` · `+0x23b`, `KeWaitForSingleObject +0x846` · `+0x849` · `+0x860/+0x867` · `+0x891` · `+0x2c4` |
| 2(a) `KeSetEvent` 자동·수동 | 옛 값 저장 → **`SignalState = 1` 먼저** (`+0x8d`) → 이미 신호였으면 아무도 안 깨움. **자동** (`Type ≠ 0`): 블록을 앞에서 하나씩 떼며 깨움에 **성공한 첫 하나** 뒤 `+0xd1 SUB [R15],1` → 0 으로 끝. 깨울 게 없으면 1 로 남음. **수동**: 전부 깨우고 1 유지 (줄 머리째 비움 `+0x4fe ~ +0x506`) | 수동 **맞음**. 자동은 "첫 하나만 + 그 자리에서 0" **추가** | `KeSetEvent +0x12/+0x20` · `+0x30` · `+0x137` · `+0x74` · `+0x86` · `+0x8d` · `+0x94/+0x96` · `+0xa8/+0xab` · `+0x4ab/+0x4ae` · `+0x4b4` · `+0xd1` · `+0x1a3` · `+0x4fe` |
| 2(b) `WaitStatus` 쓰기 | 값 = **그 블록의 `WaitKey`** (`KeWaitForSingleObject` 블록은 0 = `STATUS_WAIT_0`). 커밋된 기다림 `+0x484` (자동) · `+0x2ec` (수동), 커밋 전 `+0x4d6` · `+0x171` | 깨우는 쪽이 써야 함 → **추가** | `KeSetEvent +0x326` · `+0x1c0` · `+0x484` · `+0x2ec` · `+0x4d6` · `+0x171`, `KeWaitForSingleObject +0x1e2` |
| 2(c) 깨우는 함수 | 한 객체 블록 (`WaitType 1`) 은 `KeSetEvent` 몸에 **풀어 넣음** (호출 없음). 진짜 `KiTryUnwaitThread` 는 `WaitType 0` (전부 기다림) 블록에만, `WaitStatus = 0x100` 으로 (`+0x6a6` · `+0x6d0`). 준비 줄로 보내는 건 깨운 뒤 `+0x10c KiExitDispatcher` → `+0x41 KiProcessThreadWaitList` → `+0x101 KiDeferredReadySingleThread` (run75) | 이름만 다름. "표시 → 준비 줄" 두 단계면 됨 | `KeSetEvent +0x6a6` · `+0x6d0` · `+0x10c`, `KiExitDispatcher +0x18/+0x23` · `+0x41`, `KiProcessThreadWaitList +0x1e` · `+0x38` · `+0x98` · `+0x9f` · `+0x101` |
| 2(d) 반환 | 옛 `SignalState` (0 / 1) | 가정 없음 | `KeSetEvent +0x86` · `+0x111` · `+0x147` |
| 3 `KiTryUnwaitThread` | 스레드 락 → **`State ≠ 5` 면 아무것도 안 하고 지나침** → `WaitRegister.State` 로 갈림 (1·4 깨움 / 0 → 2 로 바꾸고 `WaitStatus` / 5 → 6 / 3 → 블록만) → 깨움 = `WaitPrcb` 줄에서 떼기 · 기다린 시간 더하기 · **`State = 7`** · PRCB `+0x2d08` push · **`WaitStatus = R8`**. **블록을 객체 줄에서 떼는 건 부른 쪽** | 락·`WaitRegister` 경쟁은 불필요 `[추측: 에뮬레이터 범위]`. **"이미 깨운 스레드는 지나침" 은 필요** | `KiTryUnwaitThread +0x1e` · `+0x33` · `+0x57 ~ +0x60` · `+0x66 ~ +0x72` · `+0x100` · `+0x149` · `+0x161` · `+0x168` · `+0x2e5 ~ +0x2fb` · `+0x316 ~ +0x321` · `+0x330` · `+0x339` · `+0x194` · `+0x19c ~ +0x1a7` |
| 4(a) 몸 함수 | **`KeReleaseMutantEx`** (`KeReleaseMutant +0x26`). 감싸기는 `Flags = Abandoned + 4`, `Wait` 면 `\|2` 만 만듦 | 가정 없음 | `KeReleaseMutant +0x6 ~ +0x22` · `+0x26` · `+0x2b` |
| 4(b) 주인 넘기기 | **예, 놓는 쪽이 그 자리에서.** 주인 아니면 `0xc0000046`. 주인이면 `SignalState + 1` → 1 (빔) 이 되면 내 목록에서 떼고 `OwnerThread = 0` → 블록 앞에서부터 깨움 (`WaitStatus = WaitKey`) → **`+0x330 KiWaitSatisfyMutant`** (`OwnerThread = 깨운 스레드` `+0x40`, 그 스레드 `MutantListHead` 에 꿰기) → `+0x192` 다시 0. 깨울 게 없으면 1 로 남김 | "깨난 쪽이 가져감" → **틀림** | `KeReleaseMutantEx +0x93/+0x97` · `+0xbd/+0xc0` · `+0xc5` · `+0xcb/+0xcd` · `+0x15a` · `+0x247` · `+0x306` · `+0x324 ~ +0x32b` · `+0x330` · `+0x192` · `+0x201`, `KiWaitSatisfyMutant +0x40` · `+0x74 ~ +0x93` |
| 4(c) 버린 뮤턴트 | 있다. 버리기 `+0x550 Abandoned = 1` · `+0x560 SignalState = 1`. 가져가는 쪽이 `Abandoned` 를 끄고 **`WaitStatus \|= 0x80`** (`STATUS_ABANDONED_WAIT_0`) — `WaitKey` 를 쓴 **뒤** 에 OR | 없으면 **추가** | `KeReleaseMutantEx +0x84/+0x88` · `+0x550` · `+0x560` · `+0x4fc ~ +0x506`, `KiWaitSatisfyMutant +0x44 ~ +0x4d`, `KeWaitForSingleObject +0x499`, `KiTryUnwaitThread +0x29a` |
| 5 `KePulseEvent` | 옛 값이 0 이면 1 로 적고 깨움 (**수동 전부 / 자동은 성공한 첫 하나**, 진짜 `KiTryUnwaitThread` 호출) → **`+0xaa` 끝에 늘 0** (안 깨웠어도, 이미 신호였어도) | **맞음** ("깨우고 0 으로"). 수동 전부·자동 하나 구분만 | `KePulseEvent +0x57` · `+0x5b/+0x5e` · `+0x60` · `+0x6e/+0x71` · `+0x98` · `+0x170` · `+0x175/+0x177` · `+0x179` · `+0xaa` · `+0xd0` · `+0xda` |
| 6 syscall 출구 | `KeWaitForSingleObject` 반환 `EAX` → `ObWaitForSingleObject +0xbf / +0xdf` 그대로 → `NtWaitForSingleObject +0x6a ~ +0x6e` 안 건드림 → `KiSystemServiceStart +0x195` → **`+0x25d` 트랩 `Rax` 에 적음** → (도우미 앞뒤 저장·복원, 마지막 `+0x391`) → **`+0x540` (조각 `FUN_1406c20cc +0x34`) 다시 읽음** → **`+0x5bb SYSRET`** | 일반 syscall "반환값 = 유저 `RAX`" **맞음**. 트랩 `Rax` 칸을 거치게 하면 다른 출구와 틀을 같이 쓸 수 있음 | `ObWaitForSingleObject +0xb5` · `+0xba` · `+0xbf` · `+0xdf` · `+0xf6`, `NtWaitForSingleObject +0x65` · `+0x6a` · `+0x6e`, `KiSystemServiceStart +0x193` · `+0x195` · `+0x25d` · `+0x268` · `+0x391` · `+0x540` · `+0x5bb` |

### PE_emulator 가 흉내 낼 최소 규칙 (결과서 sast/73 끝을 그대로 옮김)

(단일 CPU · 한 번에 한 스레드 에뮬레이터 기준. IRQL·헤더 락·스레드 락·`WaitRegister`·RCU·자동부스트는 생략 가능 `[추측: 에뮬레이터 범위]`. 우선순위·`Increment` 는 우선순위를 도입할 때.)

**기다리는 쪽 (한 객체)**

1. 먼저 `Thread.State = Waiting`, `WaitReason`·`WaitTime` 적기.
2. 객체가 신호면 **그 자리에서** 가져가고 `State = Running`, **안 자고** `STATUS_WAIT_0 (0)` 반환:
   - 자동 재설정 이벤트 (`Type 1`, 동기 타이머 같은 모양): `SignalState = 0`.
   - 세마포어: `SignalState − 1`.
   - 수동 이벤트·스레드 객체: 아무것도 안 바꿈 (신호 유지).
   - 뮤턴트: `SignalState > 0` 이면 빔 → `SignalState − 1`, `OwnerThread = 나`, `Abandoned` 였으면 끄고 `WaitStatus = 0x80`, 내 뮤턴트 목록에 꿰기. `SignalState ≤ 0` 인데 주인이 나면 재귀 (`SignalState − 1`, 그대로 반환). 주인이 남이면 기다림.
3. 마감이 이미 지났으면 안 자고 `STATUS_TIMEOUT (0x102)`. (같은 시각이면 타이머 쪽에서 곧바로 만기 처리 — Q141c2. 에뮬레이터에서는 "0 이하면 바로 `0x102`" 로 줄여도 된다 `[추측]`.)
4. 아니면 `WaitBlock` (`WaitType = 1`, `WaitKey = 0`, 객체) 을 객체 목록 **맨 뒤** 에 달고 → 잔다 (`State` 는 이미 `Waiting`; 마감 있으면 타이머 등록 — Q141c2).
5. 깨어나면 **`Thread.WaitStatus` 를 그대로 반환**한다. 다시 검사하지 않는다. 예외 하나: `WaitStatus == 0x100` 이면 1번부터 다시 한다 (APC 로 깬 경우, `WaitAll` 에서 신호가 하나 왔을 때).

**깨우는 쪽**

- 객체 목록의 블록을 **앞에서부터** 보며, 스레드가 `State == Waiting` 인 것만 깨운다 (이미 깨운 스레드는 지나침). 깨움 = 그 자리에서 ① 블록을 객체 목록에서 뗀다 ② `Thread.WaitStatus = 블록.WaitKey` (한 객체면 0) ③ `State = DeferredReady` 로 하고 "깨운 스레드 목록" 에 넣는다 ④ `AbWaitObject = 0`.
- **`KeSetEvent` 자동 재설정**: 이벤트를 먼저 신호 (1) 로 적고, 깨움에 **성공한 첫 스레드 하나만** 깨운 뒤 `SignalState = 0`. 깨울 스레드가 없으면 신호 (1) 로 남는다. 반환값 = 옛 `SignalState`.
- **`KeSetEvent` 수동 재설정**: 신호 (1) 로 적고 **전부** 깨운다. 신호 유지. (이미 신호였으면 아무도 안 깨우고 끝.)
- **`KePulseEvent`**: 이미 신호가 아니면 신호 (1) 로 적고 위와 같이 깨운다 (수동 전부 / 자동 성공한 첫 하나). **끝에 항상 `SignalState = 0`.** 반환값 = 옛 `SignalState`.
- **뮤턴트 놓기 (`KeReleaseMutant`)**: 주인이 아니면 `0xc0000046` (`Abandoned` 가 서 있으면 `0x80`). 주인이면 `SignalState + 1`; 아직 1 미만 (재귀) 이면 끝. 1 이 되면 내 목록에서 떼고 `OwnerThread = 0`; 기다리는 스레드를 앞에서부터 보며 **깨움에 성공한 첫 스레드에게 그 자리에서 주인을 넘긴다** (`OwnerThread = 그 스레드`, `SignalState − 1 = 0`, 그 스레드의 뮤턴트 목록에 꿰기, `Abandoned` 였으면 `WaitStatus |= 0x80`). 없으면 `SignalState = 1` 로 둔다.
- **`WaitType == 0` (전부 기다림)** 은 신호가 하나 와도 안 채우고 `WaitStatus = 0x100` 으로 깨워서 깨난 쪽이 처음부터 다시 검사하게 한다 (이번 test.exe 에는 안 나옴).
- 깨운 스레드들은 `NextThread`/준비 줄로 (sast/71 질문 3 `KiDeferredReadySingleThread`: 보통 뒤). 깨운 쪽이 IRQL < 2 이고 `NextThread` 가 정해졌으면 **깨운 쪽이 그 자리에서 자기를 준비 줄에 넣고 넘어간다** (`KiExitDispatcher +0x1ac`) — `KiSwapThread` 를 안 거치는 두 번째 넘기기 길.

**syscall 출구**

- 서비스 반환값 (`NtWaitForSingleObject` 면 위 `WaitStatus` 하위 32비트) = 유저 `RAX`. 트랩 프레임 `Rax` 칸에 쓰고 마지막에 그 칸에서 읽는 것까지 따라 하면 `NtContinue` 와 같은 틀을 쓸 수 있다.
- `NtContinue` 처럼 문맥을 바꾸는 호출은 `SYSRET` 길이 아니라 `KiRestoreSetContextState` 의 `IRETQ` 길 (Q141e). → **flow-writer 덧붙임 ① 으로 고쳐 읽기**

- flow-writer 덧붙임 (규칙에 넣을지는 PE_emulator 쪽 판단):
  - ① **로더 끝 `NtContinue` 는 `KiRestoreSetContextState` 길이 아니다.** 성공한 `NtContinue` 는 출구 (`KiSystemServiceExit`) 까지 안 오고 `NtContinueEx` 가 **자기 몸에서** `IRETQ` 로 간다 (run29, flow.md 2.10 · 2.2 "시스템 콜 입구·출구" 표 아래 줄). 그 길의 유저 `RAX` 도 트랩 `Rax` 칸에서 온다: `NtContinueEx +0x3ee` (조각 `FUN_1406ae17a +0x64`) `MOV RAX,[RBP-0x50]` `[정적]` (flow-writer `.asm`). 칸에 무엇이 들었는지는 `KxContextToKframes` 가 맡긴 문맥에서 옮긴 값일 것 `[추측: Rax 옮기는 줄은 안 봄]`. `KiRestoreSetContextState` (`KiSystemServiceStart +0x2d3`) 는 출구에서 스레드 헤더 비트 `0x8000000` 이 설 때만 — 남이 문맥을 넣은 경우 (Q141e) 로 보인다 `[추측]`. 그래서 "트랩 `Rax` 칸에서 읽는다" 는 틀은 셋 (`SYSRET` · `NtContinueEx` · `KiRestoreSetContextState`) 모두 같고, **어느 함수가 읽느냐만 다르다.**
  - ② **깨운 쪽이 그 자리에서 넘어갈 때 (`KiExitDispatcher +0x6c` → `+0x1ac`) 깨운 쪽 `WaitReason = 0x20` (`WrPreempted`, `+0x187`)** 을 적는다 `[정적]`. 같은 값을 쓰는 줄은 `KiCheckForThreadDispatch +0xea` 도 있다 (run76). 디스패치 인터럽트 선점은 `0x1f` (`KiDispatchInterrupt +0x13e`), 퀀텀 끝은 `0x1e` (`KiQuantumEnd +0xb98`). 칸 하나로 "마지막에 어느 길로 CPU 를 내놨나" 를 대충 가를 수 있다 — 아래 사진 P1a 가 `0x1f`.
  - ③ **즉시 채움 갈래 (안 자는 길) 는 `WaitStatus` 칸을 안 쓴다.** 이벤트·세마포어·스레드 객체면 반환값은 레지스터 (`R14D = 0`, `+0x1ad` → `+0x2c4`) 에서만 오고, `+0xc8` 칸을 쓰는 줄은 뮤턴트 갈래 (`+0x416` 0 · `+0x499` OR `0x80`) 뿐이다 `[정적]` (flow-writer `grep`). 그래서 **칸에는 옛 기다림 값이 남을 수 있다** (사진 P2 `0x100`). 에뮬레이터가 "깨난 스레드의 결과" 를 칸에서 읽는다면, 즉시 채움 때도 칸을 0 으로 써 두거나 반환값을 따로 들고 가야 한다 `[추측: 에뮬레이터 설계]`.
  - ④ `WaitRegister.State` · `BlockState` 숫자에 **PDB 열거형 이름이 있다** (flow-writer `llvm-pdbutil`): `_KWAIT_STATE` = 0 `WaitInProgress` · 1 `WaitCommitted` · 2 `WaitAborted` · 3 `WaitSuspendInProgress` · 4 `WaitSuspended` · 5 `WaitResumeInProgress` · 6 `WaitResumeAborted`, `_KWAIT_BLOCK_STATE` = 0 `WaitBlockBypassStart` · 1 `…BypassComplete` · 2 `…SuspendBypassStart` · 3 `…SuspendBypassComplete` · 4 `WaitBlockActive` · 5 `WaitBlockInactive` · 6 `WaitBlockSuspended`. 칸 타입은 그냥 바이트 (3비트 `unsigned char`, `volatile unsigned char`) 라 **칸과 열거형의 연결은 `[추측: 이름]`** 이지만, 숫자 뜻이 결과서 읽기 (0 = 커밋 전, 1 = 커밋됨, 2 = 커밋 전에 깨워짐, 블록 4 = 달려 있음) 와 다 맞는다. 결과서가 "이름 없음" 으로 남긴 5 → 6 (`KiTryUnwaitThread +0x31b ~ +0x321`) 은 "다시 시작 중 → 다시 시작 취소" 로 읽힌다 `[추측: 이름]`. `_WAIT_TYPE` (`WaitType` 0 · 1 · 2 · 4) 은 PDB 에 없다 (결과서와 같음).

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서, 기준 GUID 와 같음). 다시 analyze 안 함
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines`, sast/71 의 `KiCommitThreadWait +0x220/+0x225 → KiSwapThread` 일치
- **이름 주의**: PDB `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject` (rva `0x217eb0`, `pdb_aliases.py --where` 로 PDB 이름은 `KeWaitForSingleObject` 하나). `.asm`·`.c` 파일 이름은 `KeWaitForMutexObject`, `_calls.tsv` 는 `alias:KeWaitForMutexObject+1`. WinDbg 는 `nt!KeWaitForSingleObject`. 이 문서는 PDB 이름으로 쓴다.
- `KiSystemServiceExit` = `KiSystemServiceStart +0x1a0` (함수로 안 잡힘, 결과서 `pdb_aliases.py --where`). 출구 뒤쪽은 Ghidra 조각 `FUN_1406c20cc` (= `KiSystemServiceStart +0x50c`) 에 있다. 이 문서는 함수 기준 오프셋 (`+0x540` · `+0x5bb`) 을 쓰고 조각 기준 (`+0x34` · `+0xaf`, run56 표기) 을 괄호로 붙인다.
- 칸 이름 (`pdb_struct.py`, 결과서):
  - `_KTHREAD`: `ThreadLock +0x40` · `WaitRegister +0x70` (비트: `State` 0 ~ 2 · `Affinity` 3 · `Priority` 4 · `Apc` 5 · `UserApc` 6 · `Alert` 7) · `MiscFlags +0x74` (`WaitNext` 비트 2 · `Alertable` 비트 4 · `TimerActive` 비트 9) · `WaitStatus +0xc8` · `WaitBlockList +0xd0` · `WaitListEntry +0xd8` · `Timer +0x100` · `WaitBlock +0x140` (4개, 하나 `0x30`B) · `State +0x184` · `WaitIrql +0x186` · `WaitMode +0x187` · `WaitTime +0x1b4` · `KernelApcDisable +0x1e4` · `Preempted +0x235` · `AdjustReason +0x236` · `AdjustIncrement +0x237` · `WaitBlockCount +0x24b` · `WaitReason +0x283` · `WaitPrcb +0x2c8` · `MutantListHead +0x308` · `AbWaitObject +0x3d0` · `KernelWaitTime +0x3e0` · `UserWaitTime +0x3e8`
  - `_KWAIT_BLOCK` (`0x30`B): `WaitListEntry +0x0` · `WaitType +0x10` · `BlockState +0x11` · `WaitKey +0x12` · `Thread +0x18` · `Object +0x20`
  - `_DISPATCHER_HEADER`: `Type +0x0` (비트 7 = 헤더 락) · `SignalState +0x4` · `WaitListHead +0x8`. `_KOBJECTS` (PDB 열거형): 0 `EventNotificationObject` · 1 `EventSynchronizationObject` · 2 `MutantObject` · 5 `SemaphoreObject` · 6 `ThreadObject` · 9 `TimerSynchronizationObject`
  - `_KMUTANT`: `MutantListEntry +0x18` · `OwnerThread +0x28` · `Abandoned +0x30` 비트 0 · `AbEnabled` 비트 1 · `ApcDisable +0x31`
  - `_KTRAP_FRAME`: `Rax +0x30` · `Rip +0x168`. 출구에서 `RBP = 트랩 프레임 + 0x80` 이라 `[RBP-0x50]` = 트랩 `Rax`
  - 열거형 (flow-writer 가 PDB 로 확인): `_KTHREAD_STATE` 2 `Running` · 5 `Waiting` · 7 `DeferredReady`. `_KWAIT_REASON` 0 `Executive` · 5 `Suspended` · 6 `UserRequest` · `0x1e` `WrQuantumEnd` · `0x1f` `WrDispatchInt` · `0x20` `WrPreempted`. `_KWAIT_STATE` · `_KWAIT_BLOCK_STATE` 는 위 덧붙임 ④
- **이름 없는 칸**: `PRCB+0x2d08` = 깨운 스레드를 쌓는 단일 연결 목록 머리 `[추측: 이름]` (공개 PDB `_KPRCB` 는 `+0x6d0` 에서 잘림). 근거 `[정적]`: push 하는 쪽 (`KeSetEvent +0x2e5` · `+0x47d`, `KeReleaseMutantEx +0x2ff`, `KiTryUnwaitThread +0x161`), 비우는 쪽 `KiProcessThreadWaitList +0x1e / +0x38` 하나. sast/71 의 `[추측: 대기 목록 머리]` 보다 좁혀졌다.

## 목적

- PE_emulator 요청 25 ㄷ-1·ㄷ-3 (Q141c1): nt 엔진의 "깨움 = 실행 가능 표시만, 깨난 스레드가 syscall 을 다시 물음" 을 진짜 커널에 맞추기 위해 — 기다리는 쪽 (`KeWaitForSingleObject` → `KiCommitThreadWait`) 의 신호 검사·블록 달기·깨난 뒤 반복, 깨우는 쪽 (`KeSetEvent` · `KiTryUnwaitThread` · `KeReleaseMutant` · `KePulseEvent`) 이 그 자리에서 무엇을 채우나, 그리고 돌려준 값이 syscall 출구에서 유저 `RAX` 로 가는 길.
- 타이머 만료 (`KiTimerExpiration` 등) 는 Q141c2, 스레드 객체가 신호 받는 쪽 (끝내기) 은 Q141d — 이번엔 이름·오프셋만.

## 방법

- 정적: `_workspace/sast/73_thread-wake.md` — ntoskrnl
  - 지도: `run_ghidra.sh map ntoskrnl.exe KeSetEvent` (9함수) · `… KeWaitForMutexObject` (2,861함수) → `ms/disassemble/ntoskrnl/_map/`. `KeSetEvent` 지도는 깊이 1 이 8개뿐이고 대부분 공용 도구라 안 펼쳐졌다 — 준비 줄로 가는 길 (`KiExitDispatcher` 아래) 은 결과서가 이름으로 따로 뽑았다.
  - 새로 뽑음 16개 (모두 depth 0):
    - 요청 대상 11: `KeWaitForMutexObject` (= PDB `KeWaitForSingleObject`, 4086B) · `KiCommitThreadWait` (1413B) · `KeSetEvent` (1920B) · `KiTryUnwaitThread` (845B) · `KeReleaseMutant` (52B) · `KePulseEvent` (497B) · `KiWaitSatisfyMutant` (165B) · `KiWaitSatisfyOther` (45B) · `KiProcessThreadWaitList` (775B) · `NtWaitForSingleObject` (109B) · `ObWaitForSingleObject` (321B)
    - 넓힌 5: `KeReleaseMutantEx` (1521B — `KeReleaseMutant` 의 몸, depth 2 대신 이름으로) · `KiExitDispatcher` (619B) · `KiEnterDeferredReadyState` (85B) · `KiProcessDeferredReadyList` · `KiExitThreadWait` (준비 줄 길 확인용, 호출 자리만)
    - 있던 것 읽기만: `KiSystemServiceStart` (run56)
  - `KiWaitSatisfyMutant`·`KiWaitSatisfyOther` 는 `KeWaitForSingleObject` 의 callee 목록에 **없다** — 몸에 풀어 넣어져 있어 `_calls.tsv` 이름 찾기로 안 나온다 (결과서 "스크립트 문제").
- 결과서: `_workspace/sast/73_thread-wake_result.md` (**분석성공**. 정적으로 못 닫은 다섯 — `WaitRegister.State` 이름, 깨난 스레드 준비 줄 앞/뒤, `KeWaitForMultipleObjects`, `KiRestoreSetContextState`/`KiInitiateUserApc` 안 트랩 `Rax`, `NtSetEvent` 의 `Wait` 인자 — 는 아래 "아직 확인 필요". 첫째는 flow-writer 가 PDB 열거형으로 반쯤 닫음, 덧붙임 ④)
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `KeWaitForSingleObject +0x1a6` · `+0x645` · `+0x7b7` · `+0x841` · `+0x860` · `+0x891`, `KiCommitThreadWait +0x1f2` · `+0x220` · `+0x23d`, `KeSetEvent +0x8d` · `+0xd1` · `+0x10c`, `KeReleaseMutantEx +0x330`, `KiWaitSatisfyMutant +0x40` · `+0x4d`, `KePulseEvent +0xaa`, `KiSystemServiceStart +0x193` · `+0x25d` · `+0x2d3` · 조각 `FUN_1406c20cc` `+0x540` · `+0x5bb` (함수 기준)
- flow-writer 도 `.asm` 으로 다시 봤다: `KeWaitForSingleObject +0x104` · `+0x112` · `+0x116` · `+0x11a` · `+0x126` · `+0x19f` · `+0x1ad` · `+0x1b0` · `+0x1b9 ~ +0x1c5` · `+0x1d8` · `+0x1dd` · `+0x1e2` · `+0x1e7` · `+0x1eb` · `+0x205 ~ +0x213` · `+0x237` · `+0x240` · `+0x246` · `+0x251` · `+0x2c4` · `+0x2da` · `+0x356` · `+0x359` · `+0x35f` · `+0x36c` · `+0x37d` · `+0x3a8` · `+0x3b6` · `+0x3d7` · `+0x3de` · `+0x3e3` · `+0x3f0 ~ +0x416` · `+0x444` · `+0x44c` · `+0x489` · `+0x499` · `+0x4c6` · `+0x4e9` · `+0x4ed` · `+0x4f6` · `+0x507` · `+0x5f0 ~ +0x602` · `+0x60c` · `+0x733` · `+0x738` · `+0x7c1` · `+0x7c5` · `+0x7d2` · `+0x837` · `+0x846 ~ +0x8a2` · `+0xb32` · `+0xb41 ~ +0xb6a` · `+0xbb2` · `+0xbc4` · `+0xbc9` · `+0xbce`, `KiCommitThreadWait +0x38` · `+0x3b` · `+0x5b` · `+0x71` · `+0x97` · `+0xa1` · `+0xa3` · `+0xac` · `+0xb6` · `+0xbf` · `+0xc2` · `+0x160` · `+0x167` · `+0x16a` · `+0x170` · `+0x174` · `+0x194` · `+0x1f9` · `+0x216 ~ +0x24c` · `+0x251 ~ +0x332` · `+0x395 ~ +0x3a3` · `+0x435 ~ +0x45f` · `+0x4d6 ~ +0x50c` · `+0x55d ~ +0x56f`, `KeSetEvent +0x5` · `+0x12` · `+0x20` · `+0x26` · `+0x2a` · `+0x30` · `+0x64` · `+0x74` · `+0x86` · `+0x94` · `+0x96` · `+0xa8` · `+0xab` · `+0xd5` · `+0xf2` · `+0x111` · `+0x135` · `+0x137` · `+0x13e` · `+0x141` · `+0x147` · `+0x168` · `+0x171` · `+0x178` · `+0x1a3` · `+0x1c0` · `+0x2ca` · `+0x2e5` · `+0x2ec` · `+0x313` · `+0x31a` · `+0x31e` · `+0x326` · `+0x469` · `+0x470` · `+0x47d` · `+0x484` · `+0x48b` · `+0x4ab` · `+0x4ae` · `+0x4b4` · `+0x4cd` · `+0x4d6` · `+0x4fe ~ +0x509` · `+0x543` · `+0x54e` · `+0x553` · `+0x693 ~ +0x6d5` · `+0x711` · `+0x71f`, `KiTryUnwaitThread +0x1e` · `+0x33` · `+0x57` · `+0x5e` · `+0x60` · `+0x66` · `+0x6f` · `+0x72` · `+0xa3` · `+0x100` · `+0x149` · `+0x161` · `+0x168` · `+0x16f` · `+0x17a` · `+0x186` · `+0x194` · `+0x19c` · `+0x1a5` · `+0x1a7` · `+0x1b9` · `+0x29a` · `+0x2e5 ~ +0x339`, `KeReleaseMutant +0x6 ~ +0x2b`, `KeReleaseMutantEx +0x6a` · `+0x7a` · `+0x84` · `+0x88` · `+0x93` · `+0x97` · `+0xbd` · `+0xc0` · `+0xc5` · `+0xcb` · `+0xcd` · `+0x15a` · `+0x192` · `+0x196` · `+0x201` · `+0x247` · `+0x2d7` · `+0x2df` · `+0x2ff` · `+0x306` · `+0x324 ~ +0x330` · `+0x37c` · `+0x466` · `+0x472` · `+0x4a0` · `+0x4fc` · `+0x501` · `+0x506` · `+0x509` · `+0x515` · `+0x522` · `+0x530` · `+0x550` · `+0x560`, `KiWaitSatisfyMutant +0x4` · `+0xa` · `+0x3c ~ +0x4d` · `+0x58` · `+0x69` · `+0x74` · `+0x93`, `KePulseEvent +0x1f` · `+0x25` · `+0x52` · `+0x57` · `+0x5b` · `+0x5e` · `+0x60` · `+0x6e` · `+0x71` · `+0x8a` · `+0x8f` · `+0x98` · `+0x9f` · `+0xae` · `+0xd0` · `+0xda` · `+0xf4` · `+0x11f` · `+0x131 ~ +0x1ed`, `KiProcessThreadWaitList +0x1e` · `+0x38` · `+0x98` · `+0x9f` · `+0x101` · `+0x303`, `KiExitDispatcher +0x0 ~ +0x6c` · `+0x10a` · `+0x134 ~ +0x1b1`, `NtWaitForSingleObject +0x16` · `+0x1f` · `+0x65` · `+0x6a` · `+0x6e`, `ObWaitForSingleObject +0x47` · `+0x4e` · `+0x50` · `+0x93` · `+0x9a` · `+0xb5` · `+0xba` · `+0xbf` · `+0xc1` · `+0xda` · `+0xdf` · `+0xf6` · `+0x146`, `KiSystemServiceStart +0x193 ~ +0x3f0` · `+0x5be ~ +0x5ea` · 조각 `FUN_1406c20cc +0x0 ~ +0xaf`, `KiRestoreSetContextState +0x90` · `+0x401` · `+0x416`, `NtContinueEx +0x17` · 조각 `FUN_1406ae17a +0x64` (= `+0x3ee`), `KiDispatchInterrupt +0x43` · `+0x13e` · `+0x14d` · `+0x171`, `KiResumeThread +0x81 ~ +0xab`, `KiSchedulerApc +0xe2 ~ +0xf7`
- PDB 열거형 (`_KTHREAD_STATE` · `_KWAIT_REASON` · `_KWAIT_STATE` · `_KWAIT_BLOCK_STATE` · `_KOBJECTS`) 과 `_KWAIT_STATUS_REGISTER`·`_KWAIT_BLOCK` 칸 타입: `llvm-pdbutil-14 dump -types ms/sym/ntoskrnl.exe.pdb` (스크래치, 커밋 안 함) — flow-writer
- 값 대조 (메인 스레드 `ffff828598445080`, 새 실행 없음): run44 덤프 읽기 로그 `ms/logs/04_baggage-read_<K2 ~ P2>.txt` 구획 `02b KTHREAD` 의 기다림 칸 — flow-writer

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret | callee | 조건·뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtWaitForSingleObject` | `+0x65` | `+0x6a` | `nt!ObWaitForSingleObject` | 늘. 돌아와서 `RAX` 안 건드리고 `+0x6e RET` | [정적] |
| `nt!ObWaitForSingleObject` | `+0x47` | `+0x4c` | `nt!ObpReferenceObjectByHandleWithTag` | 핸들 → 객체. 음수면 `+0x50 JS +0xdf` 로 그 에러 반환 | [정적] |
| `nt!ObWaitForSingleObject` | **`+0xba`** | **`+0xbf`** | **`nt!KeWaitForSingleObject`** (`_calls.tsv` `alias:KeWaitForMutexObject+1`) | `+0xb5 EDX = 6` (`UserRequest`) · `R8B` = `WaitMode` · `R9B` = `Alertable`. 반환 `+0xbf MOV EBX,EAX` → `+0xdf MOV EAX,EBX` → `+0xf6 RET` | [정적] |
| `nt!KeWaitForSingleObject` | **`+0x841`** | **`+0x846`** | **`nt!KiCommitThreadWait`** | 신호 아님 + 마감 안 지남 → 블록을 단 뒤 (`+0x7b7`). 마감 종류 `+0x837 R8D` (0 없음 · 1 절대 · 2 상대) | [정적] |
| `nt!KeWaitForSingleObject` | `+0xbc4` | `+0xbc9` | `nt!KiFastExitThreadWait` | 뮤턴트 재귀 한도 `0x80000000` (`+0x3fd/+0x402`) | [정적] |
| `nt!KeWaitForSingleObject` | `+0xbce` | `+0xbd3` | 상태 값으로 예외를 던지는 함수 | `ECX = 0xc0000191` (`+0xbc9`). PDB 이름 `RtlRaiseStatus`, Ghidra 이름 `ExRaiseStatus` (`pdb_aliases.py --where`) | [정적] |
| `nt!KeWaitForSingleObject` | `+0xb4a` · `+0xb5e` | `+0xb4f` · `+0xb63` | `nt!KeAbPreAcquire` · `nt!KeAbPostReleaseEx` | 깨난 뒤 `+0x850 TEST RSI / +0x853 JNZ +0xb41` 자동부스트 갈래 → `+0xb6a JMP +0x859` 로 합류 (이름만) | [정적] |
| `nt!KiCommitThreadWait` | `+0xc2` | `+0xc7` | `nt!KiPerformWaitDeferredWork` | `WaitRegister.State = 1` 쓴 뒤 (이름만) | [정적] |
| `nt!KiCommitThreadWait` | `+0x4e3` | `+0x4e8` | `nt!KiComputeDueTime` | 절대 마감 (`+0x170/+0x174 JNZ +0x4d6`) (Q141c2) | [정적] |
| `nt!KiCommitThreadWait` | **`+0x1f2`** | **`+0x1f7`** | **`nt!KiInsertTimerTable`** | 마감이 있을 때 (`+0x167/+0x16a`). 이미 만기면 `+0x1f9 JZ +0x395` → `+0x39e KiTimerWaitTest` (Q141c2) | [정적] |
| `nt!KiCommitThreadWait` | **`+0x220`** | **`+0x225`** | **`nt!KiSwapThread`** | 여기서 잔다. 반환 → `+0x225 MOV R15,RAX` → `+0x228 MOV RAX,R15` → `+0x23b RET` (run75) | [정적] |
| `nt!KiCommitThreadWait` | `+0x2ef` | `+0x2f4` | `nt!KiAcquireKobjectLockSafe` | 커밋 전에 끝난 길 (`+0xa3 JNZ +0x251`) 에서 남은 블록 떼기 (`BlockState == 4` 만, `+0x2f4`) | [정적] |
| `nt!KiCommitThreadWait` | `+0x2de` | `+0x2e3` | `nt!KiExitThreadWait` | 같은 길 끝 → `+0x2e3 JMP +0x228` (`MOV RAX,R15` — 자지 않고 돌아감) | [정적] |
| `nt!KiCommitThreadWait` | `+0x56a` | `+0x56f` | `nt!KiProcessThreadWaitList` | 같은 길, PRCB `+0x2d08` 이 비어 있지 않을 때 (`+0x2c6 / +0x2ce`) | [정적] |
| `nt!KeSetEvent` | **`+0x6a6`** · **`+0x6d0`** | `+0x6ab` · `+0x6d5` | **`nt!KiTryUnwaitThread`** | `WaitType` 이 1·2·4 가 아닌 (= 0, 전부 기다림) 블록만. `R8D = 0x100` (`+0x69a` · `+0x6c4`), `R9D = 0`. 자동 루프 쪽은 `+0x6b4 JMP +0xa8` 로 `SUB` 를 건너뜀 | [정적] |
| `nt!KeSetEvent` | `+0x2ca` | `+0x2cf` | `nt!KiEnterDeferredReadyState` | 수동 루프, 커밋된 기다림 깨울 때 (`State = 7` 만 쓰는 도우미). 자동 루프는 `+0x469` 에 풀어 넣음 | [정적] |
| `nt!KeSetEvent` | `+0x54e` | `+0x553` | `nt!KiInsertQueueInternal` | 큐 알림 블록 (`WaitType 2`, `+0x543 BlockState = 5`) — 이것도 한 몫 (`+0x553 SUB`) | [정적] |
| `nt!KeSetEvent` | `+0x71f` | `+0x724` | `nt!KeInsertQueueDpc` | DPC 블록 (`WaitType 4`, `+0x711 SignalState = 0`) | [정적] |
| `nt!KeSetEvent` | **`+0x10c`** | **`+0x111`** | **`nt!KiExitDispatcher`** | 늘 (헤더 락 푼 뒤). `R9D` = `Increment` (`+0xf2`) | [정적] |
| `nt!KiExitDispatcher` | **`+0x41`** | **`+0x46`** | **`nt!KiProcessThreadWaitList`** | PRCB `+0x2d08 ≠ 0` (`+0x18/+0x23`) 이고 `Wait` 비트 꺼짐 (`+0x25/+0x27`). `EDX` ← `R8D` (`AdjustReason`), `R8D` ← `R9D` (`Increment`) | [정적] |
| `nt!KiExitDispatcher` | `+0x10a` | `+0x10f` | `nt!KiDirectSwitchThread` | `Wait` 비트 켜짐 (`+0x27 JNZ +0xff`) — 깨운 스레드에게 곧장 (이름만) | [정적] |
| `nt!KiExitDispatcher` | `+0x19d` | `+0x1a2` | `nt!KiQueueReadyThread` | `NextThread ≠ 0` (`+0x56` · `+0x69/+0x6c JNZ +0x134`) 이고 이전 IRQL < 2 (`+0x5a/+0x5e`). 깨운 쪽 자기를 준비 줄에, `+0x187 WaitReason = 0x20` | [정적] |
| `nt!KiExitDispatcher` | **`+0x1ac`** | **`+0x1b1`** | **`nt!KiSwapContext`** | 같은 갈래 — `KiSwapThread` 를 안 거치는 넘기기 | [정적] |
| `nt!KiProcessThreadWaitList` | **`+0x101`** | **`+0x106`** | **`nt!KiDeferredReadySingleThread`** | 쌓인 스레드마다, `+0x98 AdjustReason` · `+0x9f AdjustIncrement` 쓴 뒤 (run75 질문 3) | [정적] |
| `nt!KiProcessThreadWaitList` | `+0x303` | `+0x308` | `nt!KiInswapAndReadyThread` | 커널 스택이 안 올라와 있는 스레드 (이름만) | [정적] |
| `nt!KeReleaseMutant` | **`+0x26`** | **`+0x2b`** | **`nt!KeReleaseMutantEx`** | 늘. `Flags` 만 만들어 넘김 | [정적] |
| `nt!KeReleaseMutantEx` | `+0x2d7` · `+0x2df` | `+0x2dc` · `+0x2e4` | `nt!KiRemovePrcbWaitEntry` · `nt!KiEnterDeferredReadyState` | 커밋된 기다림을 깨울 때 (`KiTryUnwaitThread` 몸을 풀어 넣은 쪽) | [정적] |
| `nt!KeReleaseMutantEx` | **`+0x330`** | **`+0x335`** | **`nt!KiWaitSatisfyMutant`** | 객체가 뮤턴트일 때 (`+0x324 ~ +0x32b`). `RDX` = 깨운 스레드 (`+0x32d`) | [정적] |
| `nt!KeReleaseMutantEx` | `+0x472` | `+0x477` | `nt!KiTryUnwaitThread` | `WaitType 0` 블록, `R8D = 0x100`, `R9D = 0` (`+0x466`) — 주인 안 넘김 | [정적] |
| `nt!KeReleaseMutantEx` | `+0x201` | `+0x206` | `nt!KiExitDispatcher` | 끝 (2(c) 와 같은 준비 줄 길) | [정적] |
| `nt!KeReleaseMutantEx` | `+0x515` | `+0x51a` | 상태 값으로 예외를 던지는 함수 (PDB `RtlRaiseStatus`, Ghidra `ExRaiseStatus`) | `Flags` 비트 2 + 실패 (`+0x509 TEST BPL,4`) | [정적] |
| `nt!KePulseEvent` | `+0x52` | `+0x57` | `nt!KiAcquireKobjectLockSafe` | 헤더 락 | [정적] |
| `nt!KePulseEvent` | **`+0x98`** | **`+0x9d`** | **`nt!KiTryUnwaitThread`** | 수동 루프 — `WaitType 1` 은 `R8D = WaitKey` (`+0x8a`), 0 은 `+0x148 R8D = 0x100` 뒤 같은 줄로 (`+0x14e JMP +0x8f`). 반환값 안 봄 | [정적] |
| `nt!KePulseEvent` | **`+0x170`** | **`+0x175`** | **`nt!KiTryUnwaitThread`** | 자동 루프 `WaitType 1`. `AL ≠ 0` 일 때만 `+0x179 SUB` (`+0x175/+0x177`) | [정적] |
| `nt!KePulseEvent` | `+0x1e8` | `+0x1ed` | `nt!KiTryUnwaitThread` | 자동 루프 `WaitType 0` — `+0x1dc R8D = 0x100`, `SUB` 없이 다음 블록 (flow-writer `.asm`, 결과서엔 없음) | [정적] |
| `nt!KePulseEvent` | `+0xd0` | `+0xd5` | `nt!KiExitDispatcher` | 끝 (`+0xaa` 에서 0 으로 내린 뒤) | [정적] |
| `nt!KiSystemServiceStart` | **`+0x193`** | **`+0x195`** | 서비스 함수 (`CALL RAX`, `_calls.tsv` `indirect:?`) | 반환 `RAX` = 서비스 결과 (run56) | [정적] |
| `nt!KiSystemServiceStart` | `+0x248` | `+0x24d` | `nt!KiInitiateUserApc` | 유저 APC 대기 (`+0x1fb TEST [RCX+0xc2],3` / `+0x202 JZ` 안 탐). 앞 `+0x204` 트랩 `Rax` 저장, 뒤 `+0x257` 다시 읽음 | [정적] |
| `nt!KiSystemServiceStart` | `+0x2d3` | `+0x2d8` | `nt!KiRestoreSetContextState` | 스레드 헤더 비트 `0x8000000` (`+0x291/+0x297`). 돌아오지 않고 `KiRestoreSetContextState +0x416 IRETQ` (앞 `+0x401 MOV RAX,[RBP-0x50]`) | [정적] |

- 줄기 밖 (이름만): IRQL (`KiRaise/LowerIrqlProcessIrqlFlags`) · 락 스핀 (`KiCheckVpBackingLongSpinWaitHypercall` · `HvlNotifyLongSpinWait`) · RCU/SRCU · 자동부스트 (`KeAbPreAcquire` · `KeAbPostReleaseEx` · `KeAbPreWakeupThread` · `KiAbEntryRemoveFromTree`) · 우선순위 되돌리기 (`KiSetPriorityThread` · `KiComputeThreadPriorityNonStacking`) · 타이머 (`KiInsertTimerTable` · `KiComputeDueTime` · `KiTimerWaitTest` · `KiTraceSetTimer` — Q141c2) · 큐·DPC 블록 (`KiInsertQueueInternal` · `KeInsertQueueDpc`) · `KiDirectSwitchThread` · `KiDeliverApc` · ETW (`EtwTraceReadyThread`).

### 기다리는 쪽 — `KeWaitForSingleObject` → `KiCommitThreadWait` (질문 1)

- **인자** (결과서): `RCX` 객체 · `EDX` `WaitReason` · `R8B` `WaitMode` · `R9B` `Alertable` · 스택 마감 포인터.
- **처음부터 다시 도는 지점 `+0x104`** 바로 뒤에 기다림마다 지우고 다시 쓰는 칸 (flow-writer `.asm`): `+0x112 AND [RDI+0x74],~0x10` (`Alertable` 끔) · `+0x116 MOV [RDI+0x70],0` (`WaitRegister = 0`) · `+0x11a` `WaitMode` · `+0x126 OR [RDI+0x74],0x10` (인자 `Alertable` 이면 켬).
- **순서** (`RDI` = 지금 스레드):
  1. `+0x19f LEA R9,[RDI+0x140]` — 블록은 **스레드 자신의 `WaitBlock[0]`** (flow-writer `.asm`).
  2. **`+0x1a6 State = 5` (`Waiting`) 를 먼저** · `+0x1b0 WaitReason` · `+0x1b9 ~ +0x1c5 WaitTime` = KUSD `+0x320` (`TickCount`, `pdb_struct.py`) 의 아래 32비트 (flow-writer `.asm`) · `+0x1cb ThreadLock = 0`.
  3. `+0x1d8 ~ +0x1e7` 블록 채우기: `WaitType = 1` · `BlockState = 4` · **`WaitKey = 0`** (`+0x1e2`) · `Object`. `Thread` 칸은 여기서 안 쓴다 — 스레드 만들 때 `KeInitThread +0x69 ~ +0x7e` 가 네 블록 모두에 미리 적어 둔다 (run52 `[정적]`, 짐 지도 3.14 `WaitBlock*` 줄 — 결과서의 `[추측: KeInitThread, 안 확인]` 은 이걸로 닫힘).
  4. `+0x1eb BTS.LOCK [R12],7` 헤더 락 → `+0x205 AND CL,0x7f` (`Type`) → `+0x208 CMP CL,2 / +0x20b JZ +0x3e3` (뮤턴트) / `+0x211 TEST EDX,EDX / +0x213 JG +0x5f0` (`SignalState > 0` 이면 신호).
- **(a) 신호면 그 자리에서** (`KiWaitSatisfyOther`·`KiWaitSatisfyMutant` 의 몸이 풀려 있다 — 호출 없음):
  - 이벤트·세마포어: `+0x5f0 AND AL,7 / +0x5f2 CMP AL,1 / +0x5f4 JZ +0x645` → **`+0x645 MOV [R12+4],R10D` (0)** — 자동 재설정 이벤트 (타이머 동기형도 같은 모양). 세마포어 `+0x5f6 CMP CL,5` → `+0x5ff ~ +0x602` `SignalState − 1`. 그 밖 (**수동 이벤트 `Type 0` · 스레드 객체 `Type 6`**) 은 `+0x5f9 JNZ +0x246` — **아무것도 안 바꿈**. test.exe 의 `WaitForSingleObject(thread)` 가 이 모양 `[소스]` `sample/test.c:35`.
  - 뮤턴트: `+0x3f0 TEST EDX,EDX / +0x3f2 JLE +0x733` → 주인이 나면 (`+0x733 CMP RDI,[R12+0x28]` / `+0x738`) 재귀 · 한도 `+0x3fd/+0x402` · `+0x408 ~ +0x410` −1, 0 이 아니면 바로 끝 / **0 이 되면 가져감**: `+0x416 WaitStatus = 0` · `+0x444/+0x44c` `KernelApcDisable − 1` (`ApcDisable` 일 때) · **`+0x489 OwnerThread = 나`** · `+0x499 OR WaitStatus,0x80` (버려진 것) · `+0x4c6 ~ +0x4e9` 내 `MutantListHead` 맨 뒤에 꿰기 · `+0x4ed` 락 풀기 · `+0x4f6 State = 2` · `+0x507 RSI = WaitStatus` (돌려줄 값).
  - 끝: `+0x246` 헤더 락 풀기 → **`+0x251 State = 2`** → `+0x2c4 MOV EAX,R14D` (`R14D` = `+0x1ad` 의 0) → `+0x2da RET`.
  - 안 자고 끝나는 다른 갈래: 마감을 이미 넘김 (`+0x356 / +0x359 JA +0x240`, 절대 마감 0 이면 `+0x237`) → **`+0x240 R14D = 0x102`** · `Alerted` (`+0x37d` · `+0xb32`) → `0x101` · 유저 APC 대기 `+0x3a8 R14D = 0xc0`.
- **(b) 신호가 아니면**: `+0x35f ~ +0x36c` 꼬리 검사 → **`+0x7b7 ~ +0x7c1` 객체 `WaitListHead` 맨 뒤에 꿰기** → `+0x7c5` 헤더 락 풀기 → `+0x7d2 WaitBlockCount = 1` → `+0x837 R8D = 마감 종류` → **`+0x841 KiCommitThreadWait`**.
  - `KiCommitThreadWait` 안 (`RBX` = 스레드):
    - `+0x38 / +0x3b JNZ +0x23d` — 마감이 있으면 먼저 **`KiCommitThreadWait +0x23d BTS [RCX+0x74],9` (`TimerActive`)** · `+0x242 OR [RCX+0x100],0x40000080` (`Timer` 머리) → `+0x24c JMP +0x41`.
    - `+0x5b WaitBlockList = 블록` · `+0x71` 스레드 락.
    - **`+0x97 / +0xa1 TEST AL,0xe7 / +0xa3 JNZ +0x251`** — `WaitRegister` 에 State·APC·Alert 가 이미 있으면 **안 잔다** (아래 (c) 끝).
    - 없으면 `+0xac AND AL,0xf9 / +0xb6 OR AL,SIL(1) / +0xbf MOV [RBX+0x70],AL` — **`WaitRegister.State = 1`** (`WaitCommitted` `[추측: 이름]`) → `+0xc2 KiPerformWaitDeferredWork`.
    - **`+0x160 Preempted = 0`** — 자기 전에 지운다 (run75 "`Preempted == 0` 이면 맨 뒤" 의 근거 한쪽).
    - 마감: `+0x167 / +0x16a JZ +0x216` (없으면 건너뜀) · `+0x170 CMP R12D,2 / +0x174 JNZ +0x4d6` (절대면 `+0x4e3 KiComputeDueTime`) · `+0x194` 만기 시각을 `Timer` 에 · **`KiCommitThreadWait +0x1f2 KiInsertTimerTable`** · 이미 만기면 `+0x1f9 JZ +0x395` → `+0x39e KiTimerWaitTest` (Q141c2).
    - **`KiCommitThreadWait +0x220 KiSwapThread`** (ret `+0x225`).
- **(c) 깨난 뒤**:
  - `KiCommitThreadWait +0x225 MOV R15,RAX` · `+0x228 MOV RAX,R15` · `+0x22b ~ +0x23b` 레지스터 되살리고 `RET` — **사이에 계산 없음.** `KiSwapThread` 가 `WaitStatus (+0xc8)` 를 돌려주는 줄은 run75 (`+0x25c` · `+0x29a`).
  - `KeWaitForSingleObject +0x846 XOR R10D,R10D` · **`+0x849 MOV R14,RAX`** · `+0x850 / +0x853` 자동부스트 갈래 (`+0xb41 ~ +0xb6a`, 이름만) · `+0x859 AbWaitObject = 0` · **`+0x860 CMP R14D,0x100 / +0x867 JNZ +0x2c4`** → `+0x2c4 MOV EAX,R14D` (하위 32비트).
  - **`0x100` 이면**: `+0x86d ~ +0x876` IRQL 2 · `+0x88d` `WaitIrql` 되살림 · **`+0x891 JMP +0x104`** — 헤더 락부터 **전부 다시** (신호면 가져가고, 아니면 다시 단다). 상대 마감은 처음 계산한 시각을 그대로 쓴다 (`+0x3b6 ~ +0x3d7` → `+0x3de JMP +0x104`) — 다시 돌 때 늘어나지 않는다.
  - `0x100` 을 만드는 쪽: `KiCommitThreadWait +0x4fa MOV [RBX+0xc8],0x100` (`WaitRegister.Apc` 비트 `+0x435 TEST AL,0x20`), 그리고 깨우는 쪽 `WaitType 0` 갈래 (질문 2(c)).
  - **커밋 전에 이미 끝난 기다림** (`+0xa3 JNZ +0x251`): `+0x251 R13D = 0x101` · `+0x257 TEST AL,7 / +0x259 JZ +0x435` — `WaitRegister.State` 0 이면 APC·Alert 때문 (`+0x4fa` `0x100` 또는 `+0x43d ~ +0x451` `0x101`/`0xc0`) · 0 이 아니면 **누가 이미 `WaitStatus` 를 써 놨다** → `+0x262 State = 2` · `+0x287 R15 = WaitStatus` · 남은 블록 떼기 (`+0x2b2 ~ +0x332`, `BlockState == 4` 만) · `+0x2de KiExitThreadWait` → `+0x2e3 JMP +0x228` (`+0x228 MOV RAX,R15`). `KiSwapThread` 로 안 간다.

### 깨우는 쪽 — `KeSetEvent` · `KiTryUnwaitThread` · `KePulseEvent` (질문 2·3·5)

- **`KeSetEvent(Event, Increment, Wait)`**:
  - 갈림: `+0x12 TEST byte [RCX],0x7f / +0x20 JNZ +0x137` → `Type ≠ 0` 이면 `+0x137 XOR EDI,EDI` (자동), 0 이면 `+0x30 EDI = 1` (수동). 수동 + 이미 1 + `Wait == 0` 이면 락도 안 잡고 `+0x147 MOV EAX,1` 반환 (`+0x26 / +0x2a JZ +0x13e` · `+0x13e / +0x141`).
  - `+0x64` 헤더 락 · **`+0x74` 옛 값 → `+0x86` 돌려줄 값 저장 → `+0x8d SignalState = 1`** → `+0x94 / +0x96 JNZ +0xe3` (이미 신호면 아무도 안 깨움).
  - **자동** (`+0xa8` 부터): 블록 하나씩 떼며 (`+0x313`) `+0x31a ~ +0x31e WaitType == 1` 이면 풀어 넣은 깨움 (`+0x326 R13D = WaitKey` → 커밋됨 `+0x469 State = 7` · `+0x470 R14B = 1` · `+0x47d` PRCB 목록 push · **`+0x484 WaitStatus = WaitKey`** · `+0x48b AbWaitObject = 0`; 커밋 전 `+0x4cd` · `+0x4d6`) → `+0x4ab / +0x4ae` 실패면 다음 블록, 성공이면 `+0x4b4 JMP +0xd1` → **`+0xd1 SUB [R15],1` (1 → 0)** → `+0xd5 JNZ` 아니면 끝. 깨울 게 없으면 (`+0xa8 / +0xab JZ +0xdb`) 1 로 남음.
  - **수동** (`+0x1a3` 부터): 블록을 하나씩 안 떼고 전부 깨움 (`+0x1c0 R13D = WaitKey` → 커밋됨 `+0x2ca KiEnterDeferredReadyState` · `+0x2e5` push · **`+0x2ec WaitStatus`**; 커밋 전 `+0x168` · `+0x171`), 끝에 머리째 비움 `+0x4fe ~ +0x506`. **줄이는 줄이 없다** → 1 유지.
  - **`+0x10c KiExitDispatcher`** → `+0x111` 옛 값 반환 → `+0x135 RET`.
- **`KiTryUnwaitThread(Prcb, 블록, WaitStatus, 칸)`** — `AL` 1 = 깨움:
  - `+0x1e` 스레드 = 블록 `Thread` · `+0x33` 스레드 락 · **`+0x57 ~ +0x60` `State ≠ 5` 면 `+0x186` 으로 (아무것도 안 함)**.
  - `WaitRegister.State` (`+0x66 ~ +0x6f`):

    | 값 (`_KWAIT_STATE` 이름 `[추측: 이름]`) | 줄 | 하는 일 |
    |---|---|---|
    | 1 `WaitCommitted` · 4 `WaitSuspended` | `+0x72 JNZ +0x2e5` → `+0x2e8 JZ +0x78` | **깨움**: `Queue` 동시성 (`+0x78 ~ +0x91`) · **`WaitPrcb` 줄에서 떼기** (`+0xa3` 락 · `+0x100 WaitPrcb = 0`) · 기다린 시간 `KernelWaitTime`/`UserWaitTime` (`+0x114 ~ +0x142`, `WaitMode` 로 갈림) · **`+0x149 State = 7`** · `+0x150 ~ +0x161` PRCB `+0x2d08` 맨 앞 push · **`+0x168 WaitStatus = R12`** · `+0x16f AbWaitObject = 0` · `+0x17a SIL = 1` |
    | 0 `WaitInProgress` | `+0x2ee / +0x2f0` → `+0x2f2 ~ +0x2f8` | `WaitRegister.State = 2` (`WaitAborted`) · `+0x2fb WaitStatus` · `+0x302` · `+0x30d BlockState = 0` → 성공 |
    | 5 `WaitResumeInProgress` | `+0x316 / +0x319` → `+0x31b ~ +0x321` | `State = 6` (`WaitResumeAborted`) 로만, 깨우지 않음 |
    | 3 `WaitSuspendInProgress` | `+0x330` → `+0x339` | `BlockState = 2` 만, 깨우지 않음 |
    | 2 · 6 · 7 | 위 어디에도 안 걸림 | 아무것도 안 함 |

  - 끝: `+0x194` 스레드 락 풀기 · `+0x19c ~ +0x1a7 BlockState + 1` (성공·지나침 모두) · `+0x1b9 RET`.
  - **블록을 객체 줄에서 떼는 줄은 이 함수에 없다** — 부른 쪽이 뗀다 (`KeSetEvent +0x313`, 수동은 `+0x4fe` 머리째, `KeReleaseMutantEx +0x247`). 여러 객체 기다림의 **남은 블록** 은 깨난 쪽이 나가면서 뗀다 (`KiCommitThreadWait +0x2b2 ~ +0x32a`). 한 객체 기다림은 블록이 하나라 깨난 쪽이 따로 안 뗀다.
  - 두 CPU 가 동시에 오면 먼저 스레드 락을 잡은 쪽이 `State = 7` 로 바꾸므로 뒤에 온 쪽은 `State ≠ 5` 로 지나친다 (줄 근거는 위, 실제 순서는 런타임).
  - 이번 대상 셋은 모두 `R9 = 0` 으로 부른다 (`KeSetEvent +0x697` · `+0x6c1`, `KePulseEvent +0x8f` · `+0x167`, `KeReleaseMutantEx +0x466`). `R9 ≠ 0` 갈래 (뮤턴트 넘기기까지, `+0x237 ~ +0x2e0`) 를 부르는 곳은 이번에 안 봤다 (caller 32곳) `[공백]`.
  - 2.2 의 `KiResumeThread +0xa6` (첫 스레드 멈춤 풀기, run13) 도 같은 꼴로 부른다: `+0x98 R8D = WaitKey` · `+0x9d R9D = 0` (flow-writer `.asm`). 멈춘 스레드는 `KiSchedulerApc +0xf2 KeWaitForSingleObject` 로 기다리니 (run13) `WaitKey = 0` → 깨난 `WaitStatus = 0` 이다 `[정적: 줄]`.
- **`KePulseEvent(Event, Increment, Wait)`**:
  - `+0x1f / +0x25` `Type` 저장 · `+0x52` 헤더 락 · `+0x57 R12D = 옛 SignalState` (돌려줄 값 `+0xda`).
  - **`+0x5b / +0x5e JNZ +0xaa`** — 옛 값 ≠ 0 이면 (이미 신호 = 기다리는 쪽 없음) 깨우지 않음.
  - 0 이면 **`+0x60 SignalState = 1`** → `+0x6e / +0x71 JNZ +0x11f`:
    - 수동: 모든 블록 `+0x98 KiTryUnwaitThread` (반환값 안 봄), 끝에 머리째 비움 `+0x9f ~`.
    - 자동: 블록 하나씩 떼며 `+0x170 KiTryUnwaitThread` → **`AL ≠ 0` 일 때만** `+0x179 SUB [RBX+4],1` → 0 이면 그만. `WaitType 0` 은 `+0x1e8` (`0x100`, 줄이지 않음).
  - **`+0xaa AND [RBX+4],0`** — 모든 길이 지나는 합류점. **끝에 늘 0** (`KeSetEvent` 와 다름).
  - `+0xae` 락 풀기 → `+0xd0 KiExitDispatcher` → `+0xf4 RET`.
- **준비 줄로 가는 길** (깨운 뒤, 깨우는 쪽이 함):
  - `KeSetEvent +0x10c` → `KiExitDispatcher +0x18 / +0x23` (PRCB `+0x2d08` 비었으면 건너뜀) → `+0x25 / +0x27` (`Wait` 비트면 `+0x10a KiDirectSwitchThread`) → **`+0x41 KiProcessThreadWaitList`** → `+0x1e / +0x38` 목록 비우기 · 스레드마다 `+0x98 AdjustReason` (= 1) · `+0x9f AdjustIncrement` (= `KeSetEvent` 의 `Increment` 그대로 — `+0x5` → `+0xf2` → `KiExitDispatcher R9D` → `R8D`) · **`+0x101 KiDeferredReadySingleThread`** (run75 질문 3).
  - `AdjustReason == 1` 이라 `KiDeferredReadySingleThread +0x3e / +0x67` 을 안 타고 `AdjustIncrement` 를 읽는 갈래 (`+0x6d`) 로 간다. 우선순위 16 이상이면 다른 계산 (`+0xdb / +0xe5`) — 계산 본체는 안 읽음.
  - 줄 앞/뒤: `Preempted` 가 `KiCommitThreadWait +0x160` 에서 0 이라 **뒤** 로 보이지만, 이 갈래의 `[RSP+0x58]` 초깃값 (`+0x63`, 0) 에 기댄 `[추측]` (run75 한계 그대로).
  - **깨운 쪽이 그 자리에서 넘어가는 길**: `KiDeferredReadySingleThread +0x1ce2 KiCommitRescheduleContextEntry` 가 `NextThread` 를 정했으면 (run75 질문 2(a)) → `KiExitDispatcher +0x56` · `+0x5a / +0x5e` (이전 IRQL ≥ 2 면 안 함) · **`+0x69 / +0x6c JNZ +0x134`** → `+0x187 WaitReason = 0x20` (`WrPreempted`) · `+0x19d KiQueueReadyThread` (자기를 준비 줄에) · **`+0x1ac KiSwapContext`**. `KiSwapThread` 를 안 거치는 **두 번째 넘기기 길**이다 `[정적]`. run75 "P1 → P1a 넘김 5번" 의 후보 `[추측]` — 단 P1a 사진은 `0x1f` (아래 "사진과 맞춰 본 것").

### 뮤턴트 — `KeReleaseMutant` → `KeReleaseMutantEx` (질문 4)

- `KeReleaseMutant(Mutant, Increment, Abandoned, Wait)`: `+0x6 ~ +0x10` `Flags = (Abandoned ≠ 0) + 4` · `+0x16 ~ +0x22` `Wait` 면 `|2` → **`+0x26 KeReleaseMutantEx`** → `+0x2b` 이전 상태 반환. 비트: 0 = 버림 · 1 = `Wait` · 2 = 오류를 예외로 (`KeReleaseMutantEx +0x509`).
- `KeReleaseMutantEx`:
  - `+0x6a` 헤더 락 · `+0x7a EBX = 옛 SignalState` · `+0x84 / +0x88 JNZ +0x550` (버림 갈래).
  - **주인 아님**: `+0x93 CMP [RDI+0x28],RAX / +0x97 JNZ +0x4ce` → `+0x4fc ~ +0x506` `Abandoned` 면 `0x80`, 아니면 **`0xc0000046`** (`STATUS_MUTANT_NOT_OWNED`) · 비트 2 면 `+0x515` 예외.
  - 주인: **`+0xbd ~ +0xc0 SignalState + 1`** → `+0xc5` (아직 재귀) / `+0xcb ~ +0xcd` 이면 락만 풀고 끝. **1 (빔) 이 되면**: 주인 목록에서 떼고 (`+0x110 ~ +0x152`) **`+0x15a OwnerThread = 0`** → 블록 앞에서부터 (`+0x15d / +0x160`): `+0x247` 떼기 → `WaitType 1` 이면 스레드 락 → `State == 5` → 커밋됨 `+0x2d7 KiRemovePrcbWaitEntry` · `+0x2df KiEnterDeferredReadyState` · `+0x2ff` push · **`+0x306 WaitStatus = WaitKey`** (커밋 전 `+0x4a0`) → `+0x324 ~ +0x32b` 뮤턴트면 **`+0x330 KiWaitSatisfyMutant(뮤턴트, 깨운 스레드, Prcb)`** → **`+0x192 SignalState − 1`** (1 → 0) → 끝. 깨울 게 없으면 1 로 남음.
  - `+0x201 KiExitDispatcher` (Wait 에 따라 0/3, `AdjustReason 1`, `Increment`).
  - `WaitType 0` 블록은 `+0x472 KiTryUnwaitThread(0x100)` — 주인 안 넘김. 큐 `+0x37c`, DPC `+0x522` (`SignalState = 0`) · `+0x530`.
- `KiWaitSatisfyMutant`: `+0x4 / +0xa` `ApcDisable` 이면 깨운 스레드 `KernelApcDisable − 1` · **`+0x40 OwnerThread = 깨운 스레드`** · `+0x3c ~ +0x4d` `Abandoned` 면 끄고 **`+0x4d OR WaitStatus,0x80`** · `+0x58 ~ +0x69` `AbEnabled` 면 `AbWaitObject` 에 뮤턴트 · **`+0x74 ~ +0x93` 깨운 스레드 `MutantListHead` 맨 뒤에 꿰기**.
- 버림: `+0x550 Abandoned = 1` · `+0x560 SignalState = 1` → 위 돌기로. `WaitKey` 를 쓴 **뒤** 에 OR 하므로 (`+0x306` → `+0x330`) `0x80` 이 남는다. 같은 모양 줄 셋: `KiWaitSatisfyMutant +0x4d` · `KeWaitForSingleObject +0x499` · `KiTryUnwaitThread +0x29a`.

### syscall 출구 (질문 6)

| 어디서 | 줄 | 하는 일 |
|---|---|---|
| `KeWaitForSingleObject` | `+0x2c4 MOV EAX,R14D` | `0` · `0x80` · `0x101` · `0x102` · `0xc0` (`0x100` 은 안 나감 — 다시 돎) |
| `ObWaitForSingleObject` | `+0xba` → `+0xbf MOV EBX,EAX` · `+0xc1` → `+0xda ObfDereferenceObjectWithTag` → **`+0xdf MOV EAX,EBX`** → `+0xf6 RET` | 그대로 (`RAX` 윗 32비트 0). 핸들 에러는 `+0x50 JS +0xdf` 로 그 값. `ExCrossVmMutantObjectType` (`+0x93 / +0x9a`) 면 `+0x146 0xc0000024` |
| `NtWaitForSingleObject` | `+0x65` → `+0x6a ADD RSP,0x38` → `+0x6e RET` | `RAX` 안 건드림. 마감은 유저 포인터에서 복사 (`+0x34 ~ +0x49`, 결과서) |
| `KiSystemServiceStart` | `+0x193 CALL RAX` → `+0x195` · `+0x198` 횟수 +1 · `+0x1a0 ~ +0x1ae` `RBX`·`RDI`·`RSI` 트랩 프레임에서 되살림 | `RAX` 는 그대로 |
| 〃 | `+0x1be / +0x1c5` 이전 모드 유저? (아니면 `+0x5be` 커널 `RET` 길) · `+0x1cb ~ +0x1eb` IRQL·APC·`PreviousMode` 검사 · `+0x1f1 CLI` · `+0x1fb / +0x202` 유저 APC 대기? | 대기면 `+0x204` 저장 → `+0x248 KiInitiateUserApc` → `+0x257` 다시 읽고 `+0x25b JMP +0x1f2` |
| 〃 | **`+0x25d MOV [RBP-0x50],RAX`** | **트랩 `Rax` = 서비스 반환값** (첫 쓰기) |
| 〃 | `+0x261 KiSynchronizeUserIsolationDomainEntry` → `+0x268` 다시 읽음 · `+0x279 / +0x284` (`KiUpdateStibpPairing` 앞뒤) · `+0x299` (`SetContext` 갈래 → `+0x2d3 KiRestoreSetContextState`, 안 돌아옴) · `+0x2e9 / +0x301` (`KiCopyCounters` 앞뒤) · `+0x316 / +0x353` (`KiRestoreDebugRegisterState` 앞뒤) | 도우미를 부를 때마다 칸에 저장했다 다시 읽음 |
| 〃 | **`+0x391 MOV [RBP-0x50],RAX`** | 마지막 저장 (run56 표의 "`+0x391` 트랩 `Rax` 에 둠") |
| 조각 `FUN_1406c20cc` | **`+0x540` (조각 `+0x34`) `MOV RAX,[RBP-0x50]`** · `+0x544` `R8` = 유저 `Rsp` · `+0x54b` `R9` = 유저 `Rbp` · `+0x552` `EDX = 0` · `XMM0 ~ 5` 0 · `+0x56c` `RCX` = 유저 `Rip` · `+0x573` `R11` = `EFlags` · `+0x5b8 SWAPGS` · **`+0x5bb` (조각 `+0xaf`) `SYSRET`** | **유저 `RAX` 가 정해지는 곳.** `KiKvaShadow` 면 `+0x581` 에서 `KiKernelSysretExit` 로 (run56) — `RAX` 는 이미 읽은 뒤 |

- 결론 `[정적]`: 서비스 반환 `RAX` → 트랩 `Rax` → (저장·복원) → `+0x540` 에서 다시 읽어 `SYSRET`. 정상 syscall 이면 **유저 `RAX` = 서비스 반환값**. 서비스 안에서 트랩 `Rax` 에 써도 `+0x25d` 가 덮으니, 커널이 유저 `RAX` 를 바꾸려면 `+0x2d3 KiRestoreSetContextState` (`IRETQ`) 나 `+0x248 KiInitiateUserApc` 길을 타야 한다.
- `KiInitiateUserApc` · `KiRestoreSetContextState` 안에서 트랩 `Rax` 를 무엇으로 채우나는 안 읽음 `[공백]` → Q141e. (`KiRestoreSetContextState +0x90 KeContextToKframes` 를 부르고 `+0x401` 에서 칸을 읽어 `+0x416 IRETQ`.)

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리.

```
[기다리는 쪽 — 유저 syscall]
KiSystemServiceStart
 └─ +0x195  NtWaitForSingleObject                          (CALL RAX +0x193)
      └─ +0x6a   ObWaitForSingleObject
           ├─ (1) +0x4c   ObpReferenceObjectByHandleWithTag
           ├─ (2) +0xbf   KeWaitForSingleObject  (Ghidra KeWaitForMutexObject)   EDX = 6 UserRequest
           │    │  +0x104   ← 다시 도는 지점 (Alertable·WaitRegister·WaitMode 다시 씀)
           │    │  +0x19f   블록 = 자기 WaitBlock[0] · +0x1a6 State = 5 · +0x1d8 ~ +0x1e7 블록 (WaitKey 0)
           │    │  +0x205 ~ +0x213  신호? → +0x645 이벤트 0 / +0x602 세마포어 −1 / +0x489 뮤턴트 주인 / 그 밖 그대로
           │    │                    → +0x251 State = 2 → +0x2c4 반환 (안 잠)
           │    │  +0x240   마감 지남 → 0x102
           │    │  +0x7b7 ~ +0x7c1  객체 줄 맨 뒤에 블록 · +0x7d2 WaitBlockCount = 1
           │    ├─ (1) +0x846  KiCommitThreadWait
           │    │    │  +0x23d TimerActive (마감 있을 때) · +0x5b WaitBlockList
           │    │    │  +0xa3  이미 깨워졌거나 APC·Alert → +0x251 … 안 잠
           │    │    │  +0xbf  WaitRegister.State = 1 · +0x160 Preempted = 0
           │    │    ├─ (1) +0xc7   KiPerformWaitDeferredWork
           │    │    ├─ (2) +0x1f7  KiInsertTimerTable           마감 있을 때 (Q141c2)
           │    │    └─ (3) +0x225  KiSwapThread                 ← 여기서 잠. 돌아오면 WaitStatus (run75)
           │    │  +0x849 R14 = 반환 · +0x860 0x100 이면 → +0x891 JMP +0x104
           │    └─ +0x2c4 MOV EAX,R14D · RET
           └─ (3) +0xdf   ObfDereferenceObjectWithTag      → +0xdf MOV EAX,EBX · RET
 │  +0x25d  트랩 Rax = RAX · … · +0x391 마지막 저장
 │  +0x540 (조각 +0x34)  RAX = 트랩 Rax
 └─ ══▶ +0x5bb (조각 +0xaf) SYSRET

[깨우는 쪽]
KeSetEvent (Event, Increment, Wait)
 │  +0x8d  SignalState = 1 먼저 · 옛 값 ≠ 0 이면 아무도 안 깨움
 │  자동: 블록마다 (풀어 넣음) +0x469 State = 7 · +0x47d push PRCB+0x2d08 · +0x484 WaitStatus = WaitKey
 │        첫 성공 뒤 +0xd1 SignalState 1 → 0
 │  수동: 전부 (+0x2ec WaitStatus), 1 유지
 ├─ (1) +0x2cf  KiEnterDeferredReadyState                 수동, 커밋된 기다림
 ├─ (2) +0x6ab / +0x6d5  KiTryUnwaitThread (0x100)        WaitType 0 블록만
 └─ (3) +0x111  KiExitDispatcher
      ├─ (1) +0x46   KiProcessThreadWaitList               PRCB+0x2d08 ≠ 0
      │    └─ +0x106  KiDeferredReadySingleThread          +0x98 AdjustReason 1 · +0x9f AdjustIncrement = Increment (run75)
      ├─ (2) +0x10f  KiDirectSwitchThread                  Wait 비트일 때 (그러면 (1) 안 탐)
      │  +0x69 / +0x6c  NextThread ≠ 0 이고 이전 IRQL < 2 → +0x187 WaitReason 0x20
      ├─ (3) +0x1a2  KiQueueReadyThread(깨운 쪽 자신)
      └─ (4) +0x1b1  KiSwapContext                          두 번째 넘기기 길

KePulseEvent
 │  +0x5b  옛 값 ≠ 0 → +0xaa · 아니면 +0x60 SignalState = 1
 ├─ (1) +0x9d   KiTryUnwaitThread                          수동, 전부
 ├─ (2) +0x175  KiTryUnwaitThread                          자동, AL ≠ 0 이면 +0x179 −1 → 0 이면 그만
 ├─ (3) +0x1ed  KiTryUnwaitThread (0x100)                  자동, WaitType 0
 │  +0xaa  SignalState = 0 (늘)
 └─ (4) +0xd5   KiExitDispatcher

KiTryUnwaitThread (Prcb, 블록, WaitStatus, 칸)
 │  +0x57 ~ +0x60  State ≠ 5 → 지나침
 │  WaitRegister.State 1·4: +0x100 WaitPrcb = 0 · +0x149 State = 7 · +0x161 push · +0x168 WaitStatus
 │                    0: +0x2f8 State = 2 · +0x2fb WaitStatus   5: → 6   3: BlockState = 2
 └─ +0x19c ~ +0x1a7  BlockState + 1 · RET

KeReleaseMutant
 └─ +0x2b  KeReleaseMutantEx (Flags = Abandoned + 4 | Wait 2)
      │  +0x97 주인 아님 → 0xc0000046 (Abandoned 면 0x80)
      │  +0xc0 SignalState + 1 → 1 이 되면 +0x15a OwnerThread = 0
      │  블록마다 +0x306 WaitStatus = WaitKey
      ├─ (1) +0x2dc / +0x2e4  KiRemovePrcbWaitEntry · KiEnterDeferredReadyState
      ├─ (2) +0x335  KiWaitSatisfyMutant                     +0x40 OwnerThread = 깨운 스레드 · +0x4d |0x80 · +0x74 ~ +0x93 목록
      │  +0x192 SignalState − 1
      ├─ (3) +0x477  KiTryUnwaitThread (0x100)              WaitType 0
      └─ (4) +0x206  KiExitDispatcher
```

## 사진과 맞춰 본 것 (run44 메인 스레드 `ffff828598445080`, 새 실행 없음)

- 일곱 장 모두 `dt nt!_KTHREAD ffff828598445080` (같은 스레드, run76 과 같은 구획). 사진은 커널 디버거가 멈춘 순간이라 그때 이 스레드는 돌고 있다 (`State 2`).
- `WaitRegister` 는 묶음 (`_KWAIT_STATUS_REGISTER`) 이라 로그에 안쪽 비트가 없다. 블록 안 (`WaitType`·`BlockState`·`WaitKey`·`Object`) 도 로그에 없다.

| 칸 | K2 | K3 | P0 | P1 | P1a | P1b | P2 | 로그 줄 (`ms/logs/04_baggage-read_<P>.txt`) |
|---|---|---|---|---|---|---|---|---|
| `State (+0x184)` | 0 | 0 | 2 | 2 | 2 | 2 | 2 | K2 777 · K3~P1 782 · P1a~P2 780 |
| `WaitStatus (+0xc8)` | 0 | 0 | 0 | 0 | 0 | 0 | **`0x100`** | K2 765 · K3~P1 770 · P1a~P2 768 |
| `WaitBlockList (+0xd0)` | 0 | 0 | 0 | **`…984451c0`** | 같음 | 같음 | 같음 | K2 766 · K3~P1 771 · P1a~P2 769 |
| `WaitBlockCount (+0x24b)` | 0 | 0 | 0 | **1** | 1 | 1 | 1 | K2 823 · K3~P1 828 · P1a~P2 826 |
| `WaitReason (+0x283)` | 0 | 0 | 0 | 0 | **`0x1f`** | **6** | 0 | K2 828 · K3~P1 833 · P1a~P2 831 |
| `WaitMode (+0x187)` | 0 | 0 | 0 | 0 | 0 | **1** | 0 | K2 780 · K3~P1 785 · P1a~P2 783 |
| `MiscFlags.Alertable (+0x74 비트 4)` | 0 | 0 | 0 | 0 | 0 | 0 | **1** | K2 701 · K3~P1 706 · P1a~P2 704 (`MiscFlags` `0n16385` → P2 `0n16401`: K2 722 · K3~P1 727 · P1a~P2 725) |
| `AdjustReason (+0x236)` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | K2 817 · K3~P1 822 · P1a~P2 820 |
| `AdjustIncrement (+0x237)` | 0 | 0 | 0 | 0 | 0 | **1** | 0 | K2 818 · K3~P1 823 · P1a~P2 821 |
| `KernelWaitTime (+0x3e0)` | 0 | 0 | 0 | 1 | 1 | 5 | 5 | K2 885 · K3~P1 890 · P1a~P2 888 |
| `UserWaitTime (+0x3e8)` | 0 | 0 | 0 | 0 | 0 | **2** | 2 | K2 886 · K3~P1 891 · P1a~P2 889 |
| `AbWaitObject (+0x3d0)` · `WaitPrcb (+0x2c8)` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | K2 883 · 838 · K3~P1 888 · 843 · P1a~P2 886 · 841 |
| (참고) `ContextSwitches (+0x154)` | 0 | 0 | 1 | `0x17` | `0x1c` | `0x2a` | `0x31` | K2 775 · K3~P1 780 · P1a~P2 778 |

- **기다림 블록은 스레드 자신의 `WaitBlock[0]` 이다** `[관찰]` + `[정적]`: P1 부터 `WaitBlockList` = `0xffff8285984451c0` = 스레드 + `0x140` (`WaitBlock[0]`), `WaitBlockCount 1`. `KeWaitForSingleObject +0x19f LEA R9,[RDI+0x140]` · `+0x7d2 WaitBlockCount = 1` → `KiCommitThreadWait +0x5b` 와 같은 자리다. `+0x5b` 는 `KiCommitThreadWait` 까지 간 기다림에서만 쓰이니 **P0 ~ P1 에 한 번 이상 실제로 블록을 달고 커밋하러 간 기다림이 있었다** `[추측: 읽기 — 다른 기다림 함수도 같은 칸을 쓸 수 있음]`. 같은 구간 `KernelWaitTime` 0 → 1 (깨움 쪽이 더하는 칸, `KiTryUnwaitThread +0x114 ~ +0x142`) 과도 어긋나지 않는다.
- **P1a `WaitReason 0x1f` (`WrDispatchInt`)** `[관찰]`: 이 칸에 상수 `0x1f` 를 쓰는 줄은 뽑은 ntoskrnl `.asm` 513개 중 **`KiDispatchInterrupt +0x13e` 하나** (`MOV byte [RSI+0x283],0x1f`, 선점 갈래 — 퀀텀 끝 표시 0 + `NextThread ≠ 0` → `+0x14d KiQueueReadyThread` · `+0x171 KiSwapContext`, run76) (flow-writer `grep`). `KiExitDispatcher +0x187` 과 `KiCheckForThreadDispatch +0xea` 는 `0x20`, `KiQuantumEnd +0xb98` 은 `0x1e`. 그래서 **run75 "P1 → P1a 넘김 5번" 중 마지막 한 번은 디스패치 인터럽트 선점 쪽** 으로 읽힌다 `[추측: 읽기]` — 레지스터 값으로 쓰는 줄 (`KeWaitForSingleObject +0x1b0` 등 인자 그대로) 은 `grep` 에 안 잡히고, 앞 네 번은 칸이 덮여 모른다. 결과서가 짚은 `KiExitDispatcher +0x1ac` 길은 **앞 네 번의 후보로만** 남는다.
- **P1b `WaitReason 6` (`UserRequest`) · `WaitMode 1` · `UserWaitTime` 0 → 2** `[관찰]`: 유저가 부른 기다림 syscall 의 꼴이다 — `ObWaitForSingleObject +0xb5 EDX = 6` · `NtWaitForSingleObject +0x16 / +0x1f` (`WaitMode` = `PreviousMode`) · `KiTryUnwaitThread +0x114 ~ +0x142` (`WaitMode` 로 `UserWaitTime` 갈래) `[정적]`. 같은 구간에 로더가 `LdrpDrainWorkQueue` → `NtWaitForSingleObject` 로 일꾼을 기다리는 길 (run24, flow.md 2.6) 과 어긋나지 않는다 `[추측: 읽기]`.
- **P1b `AdjustIncrement 1` · `AdjustReason 0`** `[관찰]`: 깨우는 쪽이 `Increment` 를 적는 줄 `KiProcessThreadWaitList +0x9f` 와 맞는다 → P1a ~ P1b 에 `Increment 1` 로 깨워졌다 `[추측: 읽기]`. `AdjustReason` 은 `+0x98` 이 1 로 쓰지만 사진은 0 — run75 의 `KiDeferredReadySingleThread +0x26d` (둘 다 0 으로 되돌림) 와 맞는 순서다 `[추측: 읽기]`. 같은 구간 `DecayBoost` 0 → 1 · `Priority` 8 → 9 (run76 `[관찰]`) 의 출처 후보 — 깨움 부스트 계산 본체는 안 읽음 `[추측]`. P2 `AdjustIncrement 0` 은 그 뒤 다른 깨움이 덮은 것 `[추측]`.
- **P2 `WaitStatus 0x100` · `Alertable 1` · `WaitReason 0` · `WaitMode 0`** `[관찰]`: 칸에 남은 마지막 결과가 `STATUS_KERNEL_APC` 다. `0x100` 을 쓰는 줄은 `KiCommitThreadWait +0x4fa` (자기 전에 `WaitRegister.Apc`) 와 깨우는 쪽 `WaitType 0` 갈래 (`KeSetEvent +0x69a` · `+0x6c4`, `KePulseEvent +0x148` · `+0x1dc`, `KeReleaseMutantEx +0x466` 근처) `[정적]`. `KeWaitForSingleObject` 였다면 `+0x860` 에서 처음부터 다시 돌았을 것이고, 다시 돌아 **즉시 채움** 으로 끝났다면 (뮤턴트 아님) `+0xc8` 을 다시 안 쓰므로 `0x100` 이 칸에 남는다 `[정적: 줄]` + `[추측: 실제 순서]` (위 덧붙임 ③). `Alertable` 은 `KeWaitForSingleObject +0x112` 끔 / `+0x126` 켬 이 기다림마다 다시 쓰는 비트 → 마지막 기다림은 Alertable 이었다 `[추측: 읽기]`.
- `AbWaitObject` · `WaitPrcb` 가 일곱 장 모두 0 `[관찰]` — 깨울 때 둘 다 0 으로 되돌리는 줄 (`KiTryUnwaitThread +0x16f` · `+0x100`, `KeSetEvent +0x48b`, `KeWaitForSingleObject +0x859`) 과 맞는다.

## 확인된 것

- `[정적]` 기다리는 쪽: `KeWaitForSingleObject` 는 `State = 5` 를 먼저 적고 (`+0x1a6`) 신호면 **그 자리에서** 가져가고 `State = 2` 로 되돌려 안 잔다 (`+0x645` 이벤트 · `+0x602` 세마포어 · `+0x489` 뮤턴트, 수동 이벤트·스레드 객체는 그대로). 아니면 자기 `WaitBlock[0]` (`WaitKey 0`) 을 객체 줄 맨 뒤에 (`+0x7b7`) → `+0x841 KiCommitThreadWait` → `KiCommitThreadWait +0xbf WaitRegister.State = 1` → `KiCommitThreadWait +0x220 KiSwapThread`.
- `[정적]` 깨난 뒤: `KiCommitThreadWait +0x225 ~ +0x23b` 그대로 반환 → `KeWaitForSingleObject +0x860` `0x100` 이면 `+0x891 JMP +0x104` 로 처음부터, 아니면 `+0x2c4` 반환. 다른 반복은 없다.
- `[정적]` 깨우는 쪽: `KeSetEvent +0x8d` 신호 먼저 → 블록마다 `WaitStatus = WaitKey` (`+0x484` · `+0x2ec` · `+0x4d6` · `+0x171`), `State = 7`, PRCB `+0x2d08` push. 자동은 성공한 첫 하나 뒤 `+0xd1` 0, 수동은 전부·1 유지. 반환 = 옛 값. 준비 줄 = `+0x10c KiExitDispatcher` → `+0x41 KiProcessThreadWaitList` → `+0x101 KiDeferredReadySingleThread`.
- `[정적]` `KiTryUnwaitThread` 는 `State ≠ 5` 면 지나치고, 블록을 객체 줄에서 떼지 않는다 (부른 쪽 몫). 한 객체 블록 깨움은 `KeSetEvent`·`KeReleaseMutantEx` 가 몸에 풀어 넣었고, 진짜 호출은 `WaitType 0` (`0x100`) · `KePulseEvent` · `KiResumeThread` 쪽.
- `[정적]` 뮤턴트: `KeReleaseMutant +0x26 KeReleaseMutantEx` 가 1 이 되면 `+0x15a OwnerThread = 0` → 첫 깨운 스레드에 `+0x330 KiWaitSatisfyMutant` (`+0x40 OwnerThread`) → `+0x192` 다시 0. 주인 아니면 `0xc0000046`. 버림은 `WaitStatus |= 0x80`.
- `[정적]` `KePulseEvent` 는 깨운 뒤 `+0xaa` 에서 늘 0.
- `[정적]` 유저 `RAX`: `KiSystemServiceStart +0x25d` 트랩 `Rax` ← 서비스 반환값 → `+0x391` 마지막 저장 → `+0x540` (조각 `+0x34`) 다시 읽음 → `+0x5bb SYSRET`. `NtWaitForSingleObject`·`ObWaitForSingleObject` 는 값을 안 바꾼다.
- `[정적]` 깨운 쪽이 그 자리에서 넘어가는 둘째 길 `KiExitDispatcher +0x6c` → `+0x187 WaitReason 0x20` · `+0x19d KiQueueReadyThread` · `+0x1ac KiSwapContext`.
- `[관찰]` run44: 메인 스레드 `WaitBlockList` = 스레드 + `0x140` · `WaitBlockCount 1` (P1 ~ P2), `WaitReason` P1a `0x1f` · P1b 6 · P2 0, P1b `WaitMode 1` · `AdjustIncrement 1` · `UserWaitTime 2`, P2 `WaitStatus 0x100` · `Alertable 1`, `AbWaitObject`·`WaitPrcb` 늘 0.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① 결과서 질문 1(b) 의 "`+0x1f2`" · "`+0x23d`" 는 (질문 1 이 `KeWaitForSingleObject` 로 시작해서 요약만 읽으면 그 함수 오프셋으로 읽히기 쉽지만) **둘 다 `KiCommitThreadWait` 오프셋** (`KiCommitThreadWait +0x1f2 KiInsertTimerTable`, `+0x23d BTS TimerActive`) (오케스트레이터 `.asm`). 이 문서는 함수 이름을 붙여 적었다.
- ② 결과서 1(c) "`+0x846 MOV R14,RAX`" → `+0x846` 은 ret 자리 (`XOR R10D,R10D`), `MOV R14,RAX` 는 **`+0x849`** (flow-writer `.asm`).
- ③ 결과서 1(b) "`WaitBlock.Thread` 를 쓰는 줄은 이 함수에 없다 (스레드 만들 때 미리 채워 둔다 `[추측: KeInitThread, 안 확인]`)" → **이미 `[정적]`**: `KeInitThread +0x69 ~ +0x7e` 가 네 블록 `Thread` 칸 모두에 새 스레드를 적는다 (run52, 짐 지도 3.14 `WaitBlock*` 줄).
- ④ 결과서 "정적으로 못 닫은 것" 첫째 "`WaitRegister.State` 의 이름·값" → PDB 에 **`_KWAIT_STATE` 열거형** (0 `WaitInProgress` ~ 6 `WaitResumeAborted`) 이 있고 숫자 뜻이 결과서 읽기와 다 맞는다. 칸 타입은 3비트 바이트라 연결은 `[추측: 이름]` (위 덧붙임 ④). `BlockState` 도 `_KWAIT_BLOCK_STATE` (4 `WaitBlockActive` · 5 `WaitBlockInactive`). `_KTHREAD_STATE` · `_KWAIT_REASON` 숫자도 PDB 로 맞춤.
- ⑤ 결과서 syscall 출구 "`NtContinue` 로 새 문맥을 넣을 때 유저 `RAX` 는 `KiRestoreSetContextState` 가 읽는 `TrapFrame.Rax` 칸" · 최소 규칙 "`NtContinue` 처럼 … `KiRestoreSetContextState` 의 `IRETQ` 길" → **로더 끝 `NtContinue` 는 이 출구에 안 온다** (run29: 성공하면 `NtContinueEx` 가 직접 `IRETQ`). 그 길의 `RAX` 는 `NtContinueEx +0x3ee` (조각 `FUN_1406ae17a +0x64`) 가 같은 트랩 `Rax` 칸에서 읽는다 (flow-writer `.asm`). `KiRestoreSetContextState` 는 스레드 헤더 비트 `0x8000000` 갈래 — 어떤 때 서는지는 Q141e `[추측]` (위 덧붙임 ①).
- ⑥ flow-writer 보탬: `KePulseEvent` 자동 루프의 셋째 `KiTryUnwaitThread` 호출 **`+0x1e8` (ret `+0x1ed`)** — `WaitType 0` 블록, `+0x1dc R8D = 0x100`, `SUB` 없이 다음 블록. 결과서엔 `+0x98` · `+0x170` 둘만.
- ⑦ flow-writer 보탬: `KeWaitForSingleObject +0x104` 반복 지점 바로 뒤 `+0x112 / +0x116 / +0x11a / +0x126` 이 기다림마다 `Alertable` · `WaitRegister` · `WaitMode` 를 다시 쓴다, `+0x19f` 블록 = 자기 `WaitBlock[0]`, `+0x1b9 ~ +0x1c5` `WaitTime` = KUSD `TickCount`. 그리고 즉시 채움 갈래는 `WaitStatus` 칸을 안 쓴다 (`+0xc8` 쓰는 줄은 `+0x416` · `+0x499` 뿐) — 덧붙임 ③.
- ⑧ flow-writer 보탬: `KiResumeThread +0x98 / +0x9d` 가 `KiTryUnwaitThread` 에 `WaitKey`·0 을 넘긴다 → 첫 스레드가 멈춤에서 풀릴 때 `WaitStatus = 0` (2.2).
- ⑨ flow-writer 보탬: 사진 대조 전체 (위 "사진과 맞춰 본 것") — 결과서엔 없음. P1a `WaitReason 0x1f` 로 결과서의 `KiExitDispatcher +0x6c` 후보는 "마지막 한 번" 에는 안 맞는다.
- ⑩ `_calls.tsv` 의 `alias:` callee 둘: `ObWaitForSingleObject +0xba` → PDB 이름 `KeWaitForSingleObject` 하나 (Ghidra `KeWaitForMutexObject`), `KeWaitForSingleObject +0xbce` · `KeReleaseMutantEx +0x515` → PDB 이름 `RtlRaiseStatus` 하나 (Ghidra `ExRaiseStatus`). `pdb_aliases.py --lookup` 은 둘 다 "별칭 무리 아님", `--where` 로 PDB 이름을 맞췄다. 결과서의 `ExRaiseStatus` 는 이 문서에서 "상태 값으로 예외를 던지는 함수 (PDB `RtlRaiseStatus`)" 로 적었다.

## run75 · run76 에 반영한 것

- run75 문서 "아직 확인 필요" 6 (P1 → P1a 넘김 5번) 에 한 줄: 결과서 후보 `KiExitDispatcher +0x6c → +0x1ac` `[추측]` + P1a 사진 `WaitReason 0x1f` = `KiDispatchInterrupt +0x13e` 쪽 (마지막 한 번). 그리고 "정적 — 다음 바퀴 후보" 끝 "기다림·깨움 … Q141c" 에 "(Q141c1 → run77)".
- run76 문서 "정적 — 다음 바퀴 후보" 에 대기열 번호 **Q144** (오케스트레이터가 plan.md 에 붙인 번호). "아직 확인 필요" 5 (`DecayBoost` 출처) 에 P1b `AdjustIncrement 1` 한 줄.
- 정정은 없다 (run75·run76 이 틀린 곳을 이번 바퀴가 드러내지 않았다).

## 짐 지도에 반영한 것 (짐 지도 바퀴 아님)

- **새 짐 칸 없음, 2장 숫자 그대로.** 7.2 `_KTHREAD` 모름 47 중 기다림 칸 (`K3~P0` `State`·`WaitTime*` · `P0~P1` `WaitRegister*`·`WaitBlockList`·`WaitBlockFill4*` (`+0x140` 20B = `WaitBlock[0]` 앞부분)·`WaitBlockCount`·`KernelWaitTime*` · `P1~P1a` `WaitReason*` · `P1a~P1b` `WaitMode*`·`AdjustIncrement*`·`UserWaitTime` · `P1b~P2` `MiscFlags.Alertable`·`WaitStatus`) — 13칸 — 의 **쓰는 줄 후보** 를 3.14 머리 "Q78d4 에서 볼 것 — 기다림·깨움 줄" 에 모았다 (run75·run76 줄과 같은 꼴). 칸 수는 안 바꿈 — 초기화 동안 몇 번이고 다시 쓰는 칸이라 "처음 값 한 줄" 로 못 닫는다.
- 3.14 `WaitBlock*` 줄 근거 칸의 "(대기, `KiCommitThreadWait` 쪽 `[추측]`)" 옆에 run77 한 마디: 한 객체 기다림이면 `KeWaitForSingleObject +0x19f` · `+0x1d8 ~ +0x1e7` 이 `WaitBlock[0]` 을 쓴다 `[정적]`, 사진 `WaitBlockList` = `+0x140` `[관찰]`.
- 7.2 `_KTHREAD` 가리킴 줄에 run77 한 마디.

## 아직 확인 필요

동적 — **Q104 VM 단계** 에 붙인다 (오케스트레이터. 이번 묶음은 VM 안 함). 커널 멈춤점은 모든 프로세스에 걸리니 test.exe 스레드일 때만.

1. **test.exe 가 실제로 타는 기다림 길**: 메인 스레드 `WaitForSingleObject(thread, INFINITE)` `[소스]` `sample/test.c:35` 가 `nt!KeWaitForSingleObject` 에서 ① 신호 아님 → `+0x7b7` 블록 달기 → `nt!KiCommitThreadWait+0x220` (`KiSwapThread`) 까지 가나 ② `nt!KiCommitThreadWait+0x251` (커밋 전에 끝난 길) 은 안 타나 ③ `+0x860` 에서 `r14d` 가 `0x100` 으로 한 번이라도 와서 `+0x891` 로 반복하나.
2. **`PRCB+0x2d08` 의 뜻**: 깨우는 쪽 push 직후 (`nt!KeSetEvent+0x484` 근처 · `nt!KiTryUnwaitThread+0x161`) `dq @$prcb+0x2d08 L1`, `nt!KiProcessThreadWaitList` 입구에서 같은 값.
3. **`KiExitDispatcher +0x6c` `NextThread` 갈래가 실제로 타나**: `nt!KiExitDispatcher+0x1ac` 에서 `k` (test.exe 스레드가 깨운 쪽일 때). run75 "P1 → P1a 넘김 5번" 의 앞 네 번 후보 — `nt!KiSwapContext` 입구 `k` 와 같이 (run75 동적 6).
4. **서비스 반환 `RAX` → 유저 `RAX`**: `nt!KiSystemServiceStart+0x195` 의 `rax` 와 `+0x540` 뒤 `rax` · `dq @rbp-0x50 L1` 이 같은지. 서비스 안에서 트랩 `Rax` 에 쓰는 곳 — `ba w8 @rbp-0x50` (`+0x1a0` 에서 `rbp` = 트랩 프레임 + `0x80`). `KiInitiateUserApc`·`KiRestoreSetContextState` 가 이 칸을 바꾸는지는 Q141e 와 같이.
5. **`WaitRegister.State` 실제 값**: 커밋된 기다림에서 1 인가 4 인가, 5·3·6 은 언제 (`dt nt!_KTHREAD @$thread WaitRegister` 비트, `nt!KiCommitThreadWait+0xbf`).
6. **누가 `WaitStatus` 를 쓰나 (스레드 객체)**: 스레드 객체가 신호 받는 쪽은 Q141d. `nt!KeWaitForSingleObject+0x846` 에서 `rax` (기대 0), `ba w8 <메인 스레드>+0xc8`.
7. (사진에서 새로) **P2 `WaitStatus 0x100` 을 쓴 줄**: `ba w8 ffff828598445080+0xc8` (주소는 run44 부팅 기준 — 새 실행이면 그 실행의 메인 스레드) 에서 `k` — `KiCommitThreadWait+0x4fa` 인가, 깨우는 쪽 `0x100` 갈래인가, 그 뒤 `KeWaitForSingleObject+0x891` 로 다시 돌았나.
8. (사진에서 새로) **P1b `AdjustIncrement 1` 을 넘긴 쪽**: `nt!KiProcessThreadWaitList+0x9f` 에서 `r8b` 와 `k` (누가 `Increment 1` 로 깨웠나 — `NtSetEvent` 쪽인가) — run76 동적 5 (`DecayBoost` 를 1 로 올린 곳) 와 같은 순간일 것 `[추측]`.
- 추천 멈춤점 (결과서, 모두 `nt!` — 조건: test.exe 스레드. WinDbg 이름은 `nt!KeWaitForSingleObject`): `KeWaitForSingleObject+0x1a6` (`rdi` = 지금 스레드) · `+0x7b7` · `+0x841` · `+0x846` (`rax`) · `+0x860` (`r14d`), `KiCommitThreadWait+0xbf` · `+0x251` · `+0x220`, `KeSetEvent+0x484` / `+0x2ec` / `+0x4d6` / `+0x171` (`r13` = `WaitKey`) · `+0xd1` · `+0x10c`, `KiTryUnwaitThread` 입구 (`r8`) · `+0x168`, `KeReleaseMutantEx+0x330` (`rdx`) · `KiWaitSatisfyMutant+0x40`, `KiProcessThreadWaitList+0x101` · `KiExitDispatcher+0x1ac`, `KiSystemServiceStart+0x193` / `+0x195` · `+0x25d` · `+0x391` · `+0x540` · `+0x5bb`.

정적 — 다음 바퀴 후보 (대기열 번호는 오케스트레이터가):

- ① **`NtSetEvent` 가 `KeSetEvent` 에 무엇을 넘기나** — `Increment` (사진 P1b `AdjustIncrement 1` 과 맞나) 와 `Wait` (참이면 `KiExitDispatcher +0x10a KiDirectSwitchThread` 갈래). `NtReleaseMutant`·`NtPulseEvent` 도 같은 질문. PE_emulator 팀원 syscall 몸과 바로 닿는 곳.
- ② **`KeWaitForMultipleObjects`** — `WaitKey` 번호 (`STATUS_WAIT_0 + n`), `WaitAll` (`WaitType 0`) 블록 채우기, 블록 수 > 3 일 때 블록 배열. `test.exe` 엔 안 나오지만 ntdll·kernelbase 가 부를 수 있음.
- ③ **깨움 부스트 계산** — `KiDeferredReadySingleThread +0x6d` 갈래 (`AdjustReason 1` · `AdjustIncrement`) 가 `DecayBoost`·`Priority` 를 어떻게 바꾸나 (run76 의 P1b `DecayBoost 1` 출처, run75·run76 의 앞/뒤 한계와 같은 함수). Q144 ③ (우선순위 계산 본체) 와 묶을 수 있다.
- ④ `KiTryUnwaitThread` 의 `R9 ≠ 0` 갈래 (`+0x237 ~ +0x2e0`, 뮤턴트 넘기기까지) 를 부르는 곳 — caller 32곳 중 이번에 안 본 쪽.
- ⑤ `KiInitiateUserApc` · `KiRestoreSetContextState` · `KeContextToKframes` 안에서 트랩 `Rax` 를 채우는 줄 — 이미 **Q141e** 몫 (결과서).
- 타이머 만료 (`KiCommitThreadWait +0x1f2 KiInsertTimerTable` · `+0x39e KiTimerWaitTest` · `KiComputeDueTime`) 는 이미 **Q141c2**, 스레드 객체 신호 (끝내기) 는 **Q141d**.

## 디버거 메모 / 도구 메모

- 정적만이라 디버거 메모 없음.
- 도구: `KiWaitSatisfyMutant`·`KiWaitSatisfyOther` 처럼 **몸에 풀어 넣은 함수는 `_calls.tsv` 에 안 나온다.** "누가 이 일을 하나" 를 callee 이름으로만 찾으면 놓친다 — 칸 오프셋 (`+0x28` `OwnerThread` 등) 으로 `.asm` 을 `grep` 해야 한다 (결과서 "스크립트 문제").
- 도구: PDB 에 있는 열거형 (`_KWAIT_STATE` 등) 은 `pdb_struct.py` 로는 안 보인다 (구조체 칸만). `llvm-pdbutil-14 dump -types` 에서 `LF_ENUM` 이름으로 찾았다. 칸 타입이 그냥 바이트면 열거형과 자동으로 안 이어진다.

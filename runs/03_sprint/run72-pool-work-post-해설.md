# run72 pool-work-post — 해설

> 이 글은 [run72 문서](run72-pool-work-post.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q137**: PE_emulator 요청 23, "로더 일꾼 풀을 켰을 때 import 일감 길" (`_workspace/plan.md`, 이번에 **끝**)
  - 덤으로 **Q134 (e)** `LdrpWorkCallback` 본문도 닫혔다 (2.3 끝).
  - 새로 생긴 할 일: **Q139** (정적 + 덤프 오프라인, 맨 뒤). 4절에서 다룬다.
  - 동적 1 ~ 5 는 **Q104 VM 단계** 에 얹었다.
  - 다음 항목: **Q104 VM 단계**. 여기서 멈춘다.
- 근거:
  - run 문서 [`run72-pool-work-post.md`](run72-pool-work-post.md) (가장 중요)
  - 요청서 [`_workspace/sast/67_pool-work-post.md`](../../_workspace/sast/67_pool-work-post.md)
  - 결과서 [`_workspace/sast/67_pool-work-post_result.md`](../../_workspace/sast/67_pool-work-post_result.md) (판정: **분석성공**)
  - flow.md 변경 (3절에서 짧게): 그림 2 새 상자 셋 · 2.6 새 절 둘 · 3장 일꾼 줄 · 4장 · 5장
- 이번 바퀴는 **정적만** 했다.
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1b.txt`·`_P2.txt`) 를 flow-writer 가 **다시 읽은 것** 이다. 새로 실행한 게 아니다.

이미 푼 말은 **링크만** 건다.

- 스레드 풀 = **일꾼 사무실**, 일꾼 공장 = 사무실에 딸린 **파견소**, IO 완료 포트 = 사무실 앞 **우편함** → [run62 해설 2.2](run62-tp-etw-init-해설.md#22-새-말--스레드-풀-한-벌의-부품)
- 로더 풀 = **짐 들이기 팀 전용 사무실**, 풀 `+0x38` = 파견소 **번호표** (핸들) → [run69 해설 2.3](run69-loader-pool-해설.md)
- 일감 개체 `LdrpMapAndSnapWork` = 대기실 벽 **호출 버튼**, `LdrpWorkQueue` = **게시판**, 지시서 (로드 문맥 `ctx`) = 게시판에 꽂는 **쪽지**, `LdrpDrainWorkQueue` = **짐 들이기 책임자** → [run24 해설](../02_sprint/run24-work-queue-해설.md)
- `0x40000003` = 창구 대답 "**놓았어요. 그런데 원래 자리가 아닌 곳에 놓였어요**", 재배치 = 번지수 고쳐 쓰기 → [run38 해설](../02_sprint/run38-map-module-해설.md)
- 기본 대답 = "syscall 성공, 출력 칸은 안 건드림" → [run66 해설 2.10](run66-apphelp-cache-해설.md)
- 사진 P1b · P2 → [run44 해설 2.8](run44-baggage-snap-해설.md)

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, ntdll = 가게 안 **관리실**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**.
- DLL = **건물**, 땅에 갖다 놓기 = 매핑.
- 짐 들이기 팀 전용 사무실 (로더 풀) 에는 파견소 (일꾼 공장) 가 딸려 있다. 진짜 파견소는 관리사무소 안에 있고, 사무실은 **번호표** 만 들고 있다.

### 한 줄로 먼저

| 물음 | 답 |
|---|---|
| 호출 버튼을 누르면 (`TppWorkPost`) 기다리나? | **안 기다린다.** 넣기만 하고 돌아온다 `[정적]` (2.3) |
| syscall 은 몇 번? | **첫 넣기만 1번** (`NtReleaseWorkerFactoryWorker`), 둘째부터 0 `[정적]` (2.4) |
| 번호표가 0 (PE_emulator 기본 대답) 이면? | **같은 길.** 번호표를 확인도 안 하고, 대답도 안 듣는다 `[정적]` (2.5) |
| 재시도 길은 언제 열리나? | dll1·dll2 맵 결과가 `0x40000003` / `0x40000036` 일 때**만** `[정적]` (2.7 ~ 2.9) |
| test.exe 는 그 길을 타나? | **정적으로 안 닫힘.** dll1·dll2 가 같은 `ImageBase` 를 원하는 게 눈에 띄지만, 사진은 각자 다른 자리를 보여 준다 (2.10) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 출발
 └─ ntdll LdrInitializeThunk → _LdrpInitialize → LdrpInitializeProcess
      ├─ +0x1a47 LdrpEnableParallelLoading    팀 사무실 차리기 + 호출 버튼 달기 (run69)
      ├─ (사진 P1b)
      ├─ +0x1aa4 LdrpMapAndSnapDependency     exe 가 import 하는 DLL 들이기
      │    ├─ … LdrpQueueWork → TpPostWork ⇒ TppWorkPost      버튼 누르기   ← 이번 ①
      │    └─ … LdrpMinimalMapModule → (조건) 재시도 길       다시 들이기   ← 이번 ②
      └─ … NtContinue 은퇴 (사진 P2) → main
```

- 첫 넣기가 exe import 의 첫 `LdrpQueueWork` 일 거라는 건 `[추측: 순서]` 다 (run 문서·짐 지도).

### 무엇이 궁금했나

- PE_emulator 는 지금까지 사무실을 "기본 대답" 으로만 세워 두고, 일꾼 없이 첫 스레드가 `LdrpDrainWorkQueue(1)` 로 게시판을 혼자 비웠다 (run24).
- 그런데 PE_emulator 81 이 진짜 `LdrpEnableParallelLoading` 을 덫 안에서 돌리려 한다. 그러면 호출 버튼이 진짜로 달린다 (`LdrpMapAndSnapWork ≠ 0`).
- 버튼이 달리면 길이 둘 열린다:
  1. `LdrpQueueWork +0x7d TpPostWork` → `TppWorkPost` — 여기서 **기다리나? syscall 은 몇 번?**
  2. 버튼이 있을 때만 열리는 **재시도 길** — test.exe 가 이걸 타나?

### 바퀴 전에 알던 것

- `[정적]` (run24) `LdrpQueueWork` 는 쪽지를 게시판 끝에 붙이고, 버튼이 달려 있으면 `TpPostWork` 를 부른다.
- `[추측]` (run24) `TpPostWork` 는 콜백을 그 자리에서 돌리지 않을 것이다.
- `[정적]` (run69) 팀 사무실·버튼은 `+0x1a47` 에서 만든다. PE_emulator 기본 대답이면 번호표 칸이 0 이다.
- `[관찰]` (run69) 사진 P1b 에 `LdrpMapAndSnapWork` 값이 있다 → 진짜 VM 에선 버튼이 달렸다.
- `[정적]` (run38) `LdrpMinimalMapModule` 은 대답이 경고 코드이고 조건이 맞으면 `LdrpCheckForRetryLoading` 을 부른다. 그 안은 안 봤다.
- `[추측: 이름]` (run26) `LdrpRetryQueue` 는 재시도용 줄이다.

---

## 2. 찾은 것

### 2.1 한눈에

run 문서 호출 관계를 줄였다 `[정적]`.

```
① 버튼 누르기
LdrpQueueWork +0x7d/+0x82  TpPostWork(LdrpMapAndSnapWork)
 └─ +0x3c 꼬리 JMP  TppWorkPost(일감)
      ├─ +0x14c  Insertable 끔 — 이미 꺼져 있었으면 +0x158 JZ → 끝 (둘째부터)
      ├─ 풀 큐 TaskQueue[1][노드 0] 꼬리에 일감 붙이기 (락 +0x2c5 … +0x403)
      ├─ +0x4a8  풀 QueueState: PendingReleaseCount 0 → 1
      └─ +0x4f3/+0x4f8  NtReleaseWorkerFactoryWorker(풀 +0x38)      ← 첫 넣기 1번

② 재시도 길 (맵 결과가 0x40000003 · 0x40000036 일 때만)
LdrpMinimalMapModule
 ├─ +0x174/+0x179  NtMapViewOfSection → 경고 코드
 ├─ +0x2a9/+0x2ae  LdrpCheckForRetryLoading(ctx, 1) → 참
 ├─ +0x2b6  0xc000022d → +0x2d4 NtUnmapViewOfSection      (도로 치움)
LdrpDrainWorkQueue(1) +0xeb ~ +0x14b                       (재시도 줄 → 게시판)
LdrpProcessWork +0x149/+0x14e  LdrpMapDllRetry             (다시 들이기)
```

### 2.2 새 말 — 큐, 그리고 "줄이 둘이다"

- **큐** = 맨 끝에 넣고 맨 앞에서 꺼내는 줄. 먼저 넣은 것부터 나온다 `[추측]` (일반적으로 알려진 설명). 자세히는 [run24 해설 2.3](../02_sprint/run24-work-queue-해설.md).

이번 글엔 큐가 **둘** 나온다. 헷갈리기 쉬워서 먼저 갈라 둔다.

| 큐 | 어디 | 비유 | 무엇이 들어가나 |
|---|---|---|---|
| `LdrpWorkQueue` | ntdll 전역 (run24) | 관리실 **게시판** | 지시서 쪽지 (DLL 하나 들이기) |
| 풀 `TaskQueue[1][노드 0]` | 팀 사무실 (풀 개체) 안 (이번) | 사무실 앞 **우체통** | 일감 개체 하나 (= "게시판 봐 주세요" 편지) |

- 우체통은 run62 의 **우편함** (IO 완료 포트, `NtCreateIoCompletion`) 과 **다른 통** 이다. 이번 글의 우체통은 풀 개체 안의 큐다.
- `[정적]` 둘은 따로다. 그래서 PE_emulator 가 첫 스레드의 `LdrpDrainWorkQueue(1)` 로 **게시판** 을 비우는 길은 우체통과 상관이 없다 (run 문서, run24).

### 2.3 ★ `TppWorkPost` 는 넣기만 하고 돌아온다

#### 한 줄 읽기 — `TppWorkPost +0x4f3` (call) / `+0x4f8` (ret)

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!TppWorkPost` | `+0x4f3` | `+0x4f8` | `ntdll!NtReleaseWorkerFactoryWorker(풀 +0x38)` | `PendingReleaseCount` 0 → 1 로 올린 넣기 (= 첫 넣기) | [정적] |

- **누가**: 버튼을 누를 때 실제로 일하는 함수 `TppWorkPost`. `TpPostWork` 가 인자 검사만 하고 `+0x3c` 에서 **꼬리 JMP** 로 넘겨준다 (돌아오지 않고 그대로 건너감).
- **어디서**: `+0x4f3` 에서 부르고 `+0x4f8` 로 돌아온다.
- **누구를**: 신청 창구 `NtReleaseWorkerFactoryWorker`. 넘기는 건 풀 `+0x38`, 즉 **파견소 번호표** 하나다.
- **무엇을 위해**: 이름대로면 파견소에 "**일꾼 하나 풀어 주세요**" 라고 알리는 쪽지 `[추측: 이름]`. 커널 쪽 본문은 안 읽었다 (4절).

#### 기다림이 없다 — 어떻게 아나 `[정적]`

- 이 함수 안에도, 한 단계 아래 (깊이 1) 에도 `NtWait*` · `NtRemoveIoCompletion` · `NtWaitForWorkViaWorkerFactory` 가 **없다** (`_calls.tsv` 17줄).
- 막힐 수 있는 곳은 딱 하나: 우체통 **락이 이미 잠겨 있을 때** `+0x3d7 RtlpAcquireSRWLockExclusiveContended` (안에 `NtWaitForAlertByThreadId`). test.exe 초기화에선 락을 쥔 다른 스레드가 없어서 안 탄다.
- 그래서 run24 의 `[추측]` "`TpPostWork` 는 콜백을 그 자리에서 안 돌린다" 가 이번에 **`[정적]`** 이 됐다.

비유:

- 사무실 앞 **우체통에 편지를 넣고 바로 돌아선다.** 일꾼이 와서 편지를 꺼내 가는지는 안 지켜본다.
- 처음 넣을 때만 파견소에 "**한 명 보내 주세요**" 쪽지를 한 장 건넨다.

#### 덤 — 편지를 받은 일꾼은 무엇을 하나 (`LdrpWorkCallback`, Q134 (e))

- `[정적]` 받은 인자 셋을 **하나도 안 쓴다.** 그냥 **게시판** (`LdrpWorkQueue`) 이 빌 때까지 쪽지를 하나씩 떼어 `+0xb3 LdrpProcessWork` 로 처리한다 (sast/19 와 같음).
- `[정적]` 자기 syscall 은 첫 일감 때의 `NtSetInformationThread` (메인 스레드 토큰 가장, `LdrpMainThreadToken ≠ 0` 일 때) 뿐. 기다림 없음.
- 비유: 편지 내용은 안 읽는다. 편지는 "게시판 좀 봐 주세요" 신호일 뿐이다.

### 2.4 "한 명 보내 주세요" 쪽지는 첫 넣기만 — 왜?

새 칸 하나:

- **`Insertable`** = 일감 `+0xe8 WorkState` 의 비트 0. 이름대로 "이 편지를 우체통에 **넣을 수 있음**" 표시 `[추측: 이름]`.
  - 일감을 만들 때 1 로 둔다 (`TppWorkInitialize +0x12b`, run69) `[정적]`.

단계별로 `[정적]`:

1. 첫 넣기: `+0x14c` 에서 `Insertable` 을 1 → 0 으로 끄고 셈 (`PendingCallbackCount`) 을 +1 한다.
2. 옛 값이 1 이었으니 계속 간다 → 우체통에 붙이고 → 풀 `QueueState` 의 `PendingReleaseCount` 0 → 1 (`+0x4a8`) → `+0x4f3` 쪽지.
3. 둘째 넣기: `+0x14c` 에서 셈만 +1. 그런데 옛 `Insertable` 이 이미 0 → `+0x156 TEST EDX,EDX` · `+0x158 JZ` → `+0x32a` (끝).
4. 그래서 둘째부터는 **우체통에도 안 넣고, syscall 도 0** 이다.

- 첫 넣기 뒤 일감은 우체통에 그대로 남는다 (일꾼이 안 가져가면). 이후 `TpPostWork` 는 **셈만 올린다** (run 문서).
- 비유: 우체통에 같은 편지가 이미 들어 있으면, 새로 넣지 않고 봉투 위 **"한 통 더" 숫자** 만 올린다.

### 2.5 번호표가 0 이어도 같은 길

PE_emulator 에 가장 중요한 줄이다 `[정적]`.

- `+0x4ef MOV RCX,[RDI+0x38]` — 번호표를 **꺼내서 넘기기만** 한다. 0 인지 검사 안 한다.
- `+0x4f8 JMP` → `+0x4b9` — 창구 **대답도 안 본다.** 아무 상태를 줘도 된다 (권고 `STATUS_SUCCESS`).
- 풀의 일꾼 수 칸 (`+0x1a0 AvailableWorkerCount`) 은 아예 안 읽는다.

그래서 run 문서 syscall 표의 세 칸은 이렇게 된다:

| | A. 기본 대답 첫 넣기 (번호표 0 · 일꾼 0) | B. 진짜 첫 넣기 (번호표 ≠ 0 · 일꾼 0) | C. 둘째 이후 |
|---|---|---|---|
| `NtReleaseWorkerFactoryWorker` | **1번** (번호표 0) | **1번** (진짜 번호표) | **0번** |

- A 와 B 는 **같은 길** 이다. 나머지 조건부 syscall 다섯 (`NtTraceEvent` · `NtAlertThreadByThreadId` · `TppAdjustRunningThreadGoalWithLock` 쪽 · 락 기다림 · 해제 쪽) 은 test.exe 초기화에선 안 탄다 `[정적 조건 + 관찰 KUSD]`.
  - 예: `NtTraceEvent` 는 KUSD `+0x386` ≠ 0 일 때만인데, 사진에서 0 이었다 `[관찰]` (run44 사진, run62 대조).
- 비유: 관리실은 번호표가 빈칸이어도 확인 없이 창구에 내밀고, 창구가 뭐라 하든 듣지 않고 돌아선다. 그러니 PE_emulator 가 번호표를 안 채워 줬어도 (run69) **이 자리에선 차이가 없다.**

### 2.6 덫을 진짜로 켤 때 맞춰 둘 칸 셋

run 문서 표를 줄였다 `[정적]`. 셋 다 **만들 때 이미 그 모양** 이라 따로 손댈 건 없다. 틀리면 무슨 일이 나는지만 보면 된다.

| 칸 | 값 | 틀리면 |
|---|---|---|
| 일감 `+0xe8 WorkState` 비트 0 `Insertable` | **1** | 첫 넣기부터 `+0x158 JZ` 로 끝 — 우체통에 안 넣고 syscall 도 없음 |
| 우체통 머리 `+0x0 Flink` · `+0x8 Blink` | **자기 자신** | 이웃 검사 실패 → **`+0x2e4 INT 0x29`** (즉시 중단) |
| 우체통 `+0x10 Lock` | **0** | `+0x3d7` 락 기다림 → 안에서 `NtWaitForAlertByThreadId` (막힘) |

- 머리가 "자기 자신" 을 가리킨다 = 줄이 비어 있다는 모양이다 `[추측: 일반적으로 알려진 목록 관례]`.

#### 조심할 갈래 하나

- `LdrpDetourExist` 가 켜지면 `LdrpDetectDetour +0xc4 TpWaitForWork(일감, 1)` 이 불릴 수 있다 `[정적]`.
  - 에뮬레이터가 `NtQueryInformationThread(-2, 0x2a)` 에 `1` 을 주면 이 길이다. 기본 0 이면 안 탄다.
  - 둘째 인자 1 은 "대기 중 콜백 취소" 쪽이라 일꾼 없이 막히진 않을 것으로 보인다 `[추측]`. `TpWaitForWork` 본문은 안 읽었다 `[공백]`.

### 2.7 새 말 — 재시도, 그리고 재시도 길 한 바퀴

- **재시도** = 한 번 한 일을 **물렀다가 나중에 다시** 하는 것. 여기선 DLL 하나를 땅에 놓았다가 도로 치우고, 나중에 처음부터 다시 놓는다.
  - 비유: 건물 세트를 땅에 놓았는데 창구가 "원래 자리가 아니에요" 라고 하면, 일단 **도로 들어내고** 지시서를 "**나중에 다시**" 바구니에 넣는다.

길은 세 마디다 `[정적]`.

**첫 시도** — 놓았다가 도로 치움

1. `LdrpMinimalMapModule +0x174/+0x179 NtMapViewOfSection` → 대답이 `0x40000003` 또는 `0x40000036`.
2. 조건이 맞으면 (2.9) `+0x2a9/+0x2ae LdrpCheckForRetryLoading(ctx, 1)` → 참.
3. `+0x2b6 MOV EBX,0xc000022d` → `+0x2d4 NtUnmapViewOfSection` 으로 놓은 걸 지우고, 명부 줄 `DllBase` = 0.
4. 위로 올라가 `LdrpMapDllNtFileName` 이 섹션·파일 핸들을 `NtClose` ×2 로 닫는다.
5. `LdrpProcessWork +0x74` 가 `0xc000022d` 를 보면 **오류 기록 없이 조용히 끝** (`+0x79 JZ` → `+0x15d`, 상태 칸 `[ctx+0x28]` 에도 안 씀).

- 첫 시도 syscall 합계 **6개** (조건부 `NtSystemDebugControl` 하나 빼고).

**되돌리기** — 바구니를 게시판에 쏟기 (`LdrpDrainWorkQueue(1) +0xeb ~ +0x14b`)

- 게시판이 비었고 `LdrpWorkInProgress == 1` 이면, `+0xeb` 에서 재시도 줄 (`LdrpRetryQueue`) 이 비었나 본다.
- 안 비었으면 줄을 **통째로** 게시판 (`LdrpWorkQueue`) 으로 옮기고 (`+0x100 ~ +0x126`), 재시도 줄을 비우고, `+0x13b` 에서 재시도 이름 트리를 0 으로 지우고, `+0x14b JMP` 로 처음부터.
- syscall **없음**.
- 비유: 책임자가 게시판 일을 다 끝낸 뒤 "나중에 다시" 바구니를 **게시판에 통째로 쏟아** 한 번 더 돈다.

**둘째 시도** — 다시 들이기 (`LdrpProcessWork +0x149/+0x14e LdrpMapDllRetry`)

- 지시서 `+0x20` 비트 20 ("재시도 줄에 들어감" 표시) 이 켜져 있으면 이쪽으로 온다 (`+0x51 BT` · `+0x55 JC`).
- `+0x81 LdrpFindExistingModule` — 같은 모듈이 이미 올라와 있으면 `+0xf2 LdrpLoadContextReplaceModule` 로 가고 새로 안 놓는다.
- 없으면 `+0x9a LdrpGetNtPathFromDosPath` (안에서 `NtQueryAttributesFile` 1번) → `+0xad LdrpMapDllNtFileName` (첫 시도와 같은 길).
  - 이번엔 비트 20 이 켜져 있어서 재시도 검사가 `+0x31` 에서 바로 거짓. 또 물러나지 않는다.
  - 대답이 `0x40000003` 이면 이번엔 그대로 재배치 쪽 (`LdrpCompleteMapModule +0x31b LdrpRelocateImage`) 으로 간다.
- **경로 찾기는 다시 안 한다.** 이미 정해진 `FullDllName` 으로 바로 연다.

- 왜 병렬일 때 이렇게 한 번 물러나는지, 그 **이유** 는 이번 근거에 없다. 모른다.

### 2.8 `LdrpCheckForRetryLoading` — 이름과 달리 "줄에 넣을지 정하고 넣는다"

- `[정적]` 돌려주는 건 `AL` **참/거짓** 뿐이다. 상태 코드가 아니다.
  - `0xc000022d` 는 **부른 쪽** 이 만든다: `LdrpMinimalMapModule +0x2b6`, `LdrpMapDllNtFileName +0x22e`. 이름 `STATUS_RETRY` 는 기억 `[추측]`.
- `[정적]` 하는 일 (임계 구역 안, syscall 없음):
  1. 지시서 `+0xb0` ≠ 0 이거나, `+0x20` 비트 20·26 이 켜져 있으면 → 거짓.
  2. 재시도 이름 트리 **`LdrpRetryingModuleIndex`** 에서 이 DLL 이름을 찾는다.
  3. 있으면 → 비트 20 켜고, 지시서를 **`LdrpRetryQueue`** 꼬리에 넣고 → 참.
  4. 없고 `DL ≠ 0` 이면 → 트리에 이름을 넣고 (`+0x14e RtlRbInsertNodeEx`) 3번처럼 → 참. `DL == 0` 이면 거짓.
- 부르는 곳이 둘이다:
  - `LdrpMinimalMapModule +0x2a9` (`DL = 1`) — 놓아 본 뒤, 경고 코드일 때.
  - `LdrpMapDllNtFileName +0x46` (`DL = 0`) — **모든 DLL 이름 열기 앞**. 이미 재시도 중인 이름이면 파일을 열기도 전에 바구니로 보낸다. 트리가 비어 있으면 그냥 거짓 (syscall 0).
- 비유: "나중에 다시" 바구니 옆에 **이름 명단** 이 있다. 명단에 이름을 올리고 바구니에 지시서를 넣는 사람이 이 함수다. 같은 이름의 짐이 또 오면, 들이려 하지도 않고 바로 바구니로 보낸다.
- run26 의 `LdrpRetryQueue` `[추측: 이름]` 은 이번에 넣는 길 (`+0x158 ~ +0x182`) 이 정해졌다 `[정적]`.

### 2.9 문 넷 — 셋은 이미 열려 있다

재시도 길이 열리는 조건을 test.exe 에 대 보면 `[정적]`:

| 문 | test.exe 에서 |
|---|---|
| 버튼 `LdrpMapAndSnapWork ≠ 0` | **열림** — 병렬 켜기 뒤. run44 P1b 에 값 있음 `[관찰]` (run69). kernel32 올릴 때는 0 (run24) |
| 지시서 `+0xb0 == 0` | **열림 (늘 0)** — ntdll 에 그 칸을 쓰는 줄을 못 찾음 (`objdump` 훑기, `lea` 뒤 쓰기·`rep stos` 류는 못 잡는 한계) |
| 지시서 `+0x20` 비트 20·26 꺼짐 (첫 시도) | **열림** — 새 지시서는 비트 15 만 켬 (`LdrpAllocatePlaceHolder +0x7c`) |
| 맵 결과 `0x40000003` / `0x40000036` | **실행 때 정해짐** ← 남은 하나 |

- 그러니 질문은 하나로 줄었다: **dll1·dll2 를 놓을 때 창구가 뭐라고 대답하나?**

### 2.10 ★ dll1·dll2 는 같은 자리를 원한다 — 그런데 사진은?

#### 눈에 띄는 것 `[정적+샘플]`

| 파일 | `ImageBase` (헤더에 적힌 "원래 자리") |
|---|---|
| `dll1.dll` | **`0x180000000`** |
| `dll2.dll` | **`0x180000000`** |

- 둘이 **같은 번지** 를 원한다. 둘 다 원래 자리에 놓을 수는 없다.
- 그럼 둘째 DLL 은 "원래 자리 아니에요" (`0x40000003`) 를 받고 재시도 길을 탈까? → 커널 쪽 규칙에 달렸다.

#### 커널 쪽 규칙 — 단계별로 `[정적]`

1. 창구가 `0x40000003` 을 쓰는 곳은 `MiMapViewOfImageSection +0xc0a` 한 자리다.
   - 조건: 실제로 놓인 번지 ≠ 섹션의 기준 주소 (`_SEGMENT.BasedAddress`).
   - 즉 "원래 자리" 의 기준은 **헤더 값이 아니라 섹션의 기준 주소** 다.
2. 섹션의 기준 주소는 **섹션을 만들 때** `MiSelectImageBase` 가 고른다. 갈림은 넷째 인자 비트 0:
   - **켜짐** → 헤더 `ImageBase` 그대로 (`0x180000000`).
   - **꺼짐 + DLL** → 전역 비트맵에서 **이미지 파일마다 다른 칸** (`0x7fffffff0000` 에서 아래로 64KB 칸).
3. 그 비트를 주는 `MiCreateNewSection` 줄은 안 읽었다 `[공백]`.

새 말 하나:

- **비트맵** = 칸마다 "쓰는 중 / 비었음" 을 비트 하나로 적은 표 `[추측]` (일반적으로 알려진 설명).
  - 비유: 관리사무소가 땅 꼭대기 구역을 64KB 구획으로 나눠 둔 **구획 배정표**. 건물 세트마다 구획을 하나씩 따로 정해 준다.

그래서 갈림길은 이렇다:

| 비트 0 | dll1·dll2 기준 주소 | 둘째 DLL 대답 | 재시도 길 |
|---|---|---|---|
| 꺼짐 (비트맵 갈래) | 각자 다른 칸 | 자리가 비어 있으면 **`0`** (기대 `[추측]`) | 안 탐 |
| 켜짐 (헤더 그대로) | 둘 다 `0x180000000` | **반드시 `0x40000003`** | **탐** |

- 비트맵 갈래라면 헤더 값이 같다는 건 상관이 없다. 각자 다른 구획을 받으니까.

#### 사진이 보여 주는 것

- `[관찰]` run44 P1b (`ms/logs/04_baggage-read_P1b.txt`):
  - dll1: `DllBase` = `OriginalBase` = `0x7ffae12e0000`
  - dll2: `DllBase` = `OriginalBase` = `0x7ffadb950000`
  - P2 도 같은 값.
- 둘 다 `0x180000000` 이 **아니고**, 서로 **다르다**.

읽는 법 — 근거 → 그래서 → 아직 확인 아님:

1. 근거: `OriginalBase` 는 `LdrpProcessMappedModule +0x97` 이 **메모리에 올라온 NT 헤더의 `ImageBase`** 로 덮는 칸이다 (run35).
2. 그래서: 그 순간 메모리 헤더에 적힌 "원래 자리" 는 파일 값 `0x180000000` 이 아니라 **각자의 `DllBase`** 였다.
3. 그런데 그 헤더를 **누가 고쳤는지** 는 이 칸으로 못 가린다 `[추측]`:
   - 커널이 섹션을 만들 때 고쳤을 수도 있고,
   - ntdll 재배치 (`LdrpCompleteMapModule +0x31b`) 가 고쳤을 수도 있다 (재배치가 헤더 `ImageBase` 를 고치는지 안 읽었다).
4. 두 주소가 꼭대기 구역 (`0x7fffffff0000` 아래) 에 **따로** 있는 모양은 비트맵 갈래와 어긋나지 않는다 `[추측: 주소 모양으로만]`.

- 정리: 둘이 **충돌 없이 각자 다른 자리를 받은 것으로 보인다.** 하지만 이건 `[추측]` 이다.
- 재시도가 정말 안 일어났는지도 사진으론 못 가린다. 일어났더라도 `Drain(1) +0x12d ~ +0x13b` 가 재시도 줄·이름 트리를 빈 꼴로 되돌린 뒤라서다 `[정적: 길]`.
- PE_emulator 쪽: dll1·dll2 맵에 `0` 을 주면 이 길은 안 탄다 (run 문서 결론 2).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 그림 2 `LdrpQueueWork` → `TpPostWork` 아래 | `TpPostWork` 에서 끝 | **`TppWorkPost` (넣기만)** → **`NtReleaseWorkerFactoryWorker` (첫 넣기 1번)** 상자 둘 `[정적]` |
| 그림 2 `LdrpMinimalMapModule` 상자 | 붙이기·대답 그대로 위로 | + "경고 코드 · 일감 개체 ≠ 0 이면 재시도 검사" |
| 2.6 run24 질문 "`TpPostWork` 가 콜백을 그 자리에서 돌리나" | `[추측]` 안 돌림 | 해결 → **안 돌린다** `[정적]` |
| 2.6 새 절 "풀 큐에 넣기" | — | 2.1 ① 트리 + 사실 5줄 |
| 2.6 새 절 "재시도 길" | — | 2.1 ② 트리 + 사실 5줄 |
| 2.6 맨 위 | — | 커널 기준 주소 규칙 `[정적]` + 비트 `[공백]` + P1b `DllBase = OriginalBase` `[관찰]` |
| 3장 일꾼 | — | 로더 풀 첫 넣기에 `NtReleaseWorkerFactoryWorker` 딱 1번. 일꾼을 만드는 계기인지는 `[추측]` |
| 4장 | `LdrpRetryQueue` 넣는 쪽 `[추측: 이름]` · `ctx+0xb0` 뜻 모름 | 넣는 쪽 해결 `[정적]` · `ctx+0xb0` 은 test.exe 길에서 늘 0 (이름은 여전히 모름) · 새 동적·정적 줄 |

- 짐 지도 `baggage.md` 도 늘었다: 3.8 (재시도 전역 — 1줄 더함, 2줄 고침), 새 7.6.12 (첫 넣기 때 바뀌는 일감·풀·우체통 칸, 커널 파견소 몫, 재시도 때 지시서 칸), 새 7.31 (동적 질문).

### 큰 그림에서

- 이제 run69 의 "팀 사무실 차리기" 다음 이야기, 곧 **"버튼을 누르면 무슨 일이 나나"** 가 syscall 단위로 정해졌다.
  - 버튼 = 우체통에 편지 한 통, 첫 번만 파견소 쪽지 1장. 기다림 없음.
- PE_emulator 에겐 좋은 소식이다. 번호표를 안 채운 기본 대답으로도 **같은 길** 이라, 덫을 진짜로 켜도 여기서 막히지 않는다 `[정적]`.
- 남은 불확실함은 하나로 모였다: **dll1·dll2 를 놓을 때 창구 대답**. 이게 `0` 이면 재시도 길은 지도에서 "안 타는 갈래" 로 남고, `0x40000003` 이면 2.7 의 세 마디가 실제 흐름에 들어온다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| dll1·dll2 (그리고 kernel32·kernelbase) 맵 결과 값 | 실행 때 값. 커널 규칙의 갈림 비트가 정적으로 안 닫힘 | **Q104** 동적 1 (`LdrpMinimalMapModule +0x179` 에서 `r eax`) |
| 재시도 길이 실제로 열렸나 | 사진은 되돌린 뒤 모습이라 못 가림 | **Q104** 동적 2 (`LdrpCheckForRetryLoading` · `LdrpMapDllRetry` 입구, `Drain +0xeb`) |
| 첫 넣기 syscall 이 초기화 동안 딱 1번인지, 넣기 뒤 칸 값 | 풀·일감은 힙 안이라 사진에 없음 | **Q104** 동적 3 (`TppWorkPost +0x4f3`) |
| 조건 입력 값 (KUSD `+0x386`, TEB `+0x1850` 등) | 실행 때 값 | **Q104** 동적 4 (5는 선택: `NtSetEvent` 횟수) |
| `MiCreateNewSection` 이 갈림 비트 (`MiRelocateImage` 여섯째 인자 `& 1`) 에 주는 값 | 이번 범위 밖 — 그 줄을 안 읽음 | **Q139 (a)** 정적 — 닫히면 동적 1 기대값이 정해짐 |
| `EPROCESS.Flags3` 비트 13 (`AllImagesAtBasePristineBase`) 뜻, `_SEGMENT.BasedAddress` 를 채우는 줄 | 안 읽음 | **Q139 (b)** 정적 |
| `TpWaitForWork` 본문 (2.6 조심할 갈래) | 안 읽음 | **Q139 (c)** 정적 |
| 커널 `NtReleaseWorkerFactoryWorker` — 쪽지가 일꾼을 새로 만드는 계기인가 | 커널 본문 안 읽음 | **Q139 (d)** 정적 (Q134 (b) 와 같이) |
| 지시서 `+0xb0` 을 다른 모양으로 쓰는 곳이 있나 | `objdump` 훑기 한계 (`lea` 뒤 쓰기·`rep stos`) | **Q139 (e)** 정적 |
| dll1·dll2 섹션의 진짜 기준 주소 (`_SEGMENT.BasedAddress`) | 이번엔 로그만 다시 읽음 | **Q139 (f)** — run44 덤프를 서버에서 오프라인으로 (그 페이지가 덤프에 있다면 `[추측]`). 같으면 맵 결과 `0` 이 거의 닫힘 |

---

## 5. 더 읽기

- run 문서: [`run72-pool-work-post.md`](run72-pool-work-post.md) — syscall 표 전체 (첫 넣기·재시도 길), 조건 표, 첫 넣기 때 쓰는 칸 표
- 결과서: [`_workspace/sast/67_pool-work-post_result.md`](../../_workspace/sast/67_pool-work-post_result.md)
- 요청서: [`_workspace/sast/67_pool-work-post.md`](../../_workspace/sast/67_pool-work-post.md)
- flow.md: [2.6](flow.md) 맨 위 (run72) 줄 · "풀 큐에 넣기 `TpPostWork` ⇒ `TppWorkPost`" 절 · "재시도 길 `LdrpCheckForRetryLoading`" 절 · 그림 2 `tp1`~`tp3` 상자
- 짐 지도: [`baggage.md`](baggage.md) 3.8 · 7.6.12 · 7.31
- 앞 이야기: [run24 해설](../02_sprint/run24-work-queue-해설.md) (게시판·버튼·책임자) · [run38 해설](../02_sprint/run38-map-module-해설.md) (창구 대답·재배치) · [run62 해설](run62-tp-etw-init-해설.md) (사무실·파견소·우편함) · [run69 해설](run69-loader-pool-해설.md) (팀 사무실·번호표)

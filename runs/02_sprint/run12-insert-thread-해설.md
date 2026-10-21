# run12 insert-thread — 해설

> 이 글은 [run12 문서](run12-insert-thread.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q4** — 2.1 `nt!PspInsertThread`, `nt!PspCallProcessNotifyRoutines` callee (`_workspace/plan.md`)
  - callee = 이 두 함수가 **직접 부르는 함수 목록**.
- 근거:
  - run 문서 [`run12-insert-thread.md`](run12-insert-thread.md) (가장 중요)
  - 요청서 [`_workspace/sast/07_insert-thread.md`](../../_workspace/sast/07_insert-thread.md)
  - 결과서 [`_workspace/sast/07_insert-thread_result.md`](../../_workspace/sast/07_insert-thread_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q4, Q5, Q19, Q39
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다.

이 글은 [run9 해설](run9-create-user-process-해설.md), [run10 해설](run10-allocate-process-해설.md), [run11 해설](run11-allocate-thread-해설.md) 다음 편이다.

- 기초 말(오프셋, call/ret, 콜스택, 태그, 간접 호출, 핸들 …)은 run9 해설 2절에 있다. 여기선 **링크만** 건다.
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**
  - `_ETHREAD` = 직원 **인사 카드** (flow.md 에선 "사원증")
  - 프로세스 목록 = **상가 명부**
  - 스레드 락 = 인사 카드에 걸어 둔 **"작성 중" 자물쇠** (run11)
  - 멈춘 채 만들기 = 목에 건 **"대기" 팻말**, `KeReadyThread` = **출근 대기 줄**에 세우기 (run9)
  - 핸들 = **손잡이** (flow.md 의 이번 비유에선 "출입증")
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**

한 줄로 먼저 말하면:

- run11 에서 첫 직원(test.exe 첫 스레드)을 **뽑았다.** 이번엔 그 직원을 **정식으로 들여보내는** 과정, `PspInsertThread` 의 순서를 코드에서 따라갔다.
- 순서는 "근무 조건 정하기 → 명단에 이름 달기 → (대기 팻말) → 사원 등록 → 자물쇠 풀기 → 바깥에 알리기 → 손잡이 만들기 → 대기 줄에 세우기" 다.
- 덤으로, "새 가게 생겼어요" 알림이 **누구에게, 어떤 길로** 가는지 (주소를 꺼내는 곳까지만) 봤다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (유저 쪽): "새 프로그램 띄워 줘"
     └─ 시스템 콜로 커널에 부탁
         └─ nt!NtCreateUserProcess                    (run9, Q1)
              ├─ +0xc36   PspAllocateProcess          (run10, Q2) 새 가게 차리기
              ├─ +0xe5c   PspAllocateThread           (run11, Q3) 첫 직원 뽑기
              ├─ +0xfc2   PspInsertProcess            (run11, Q3) 상가 명부에 올리기
              ├─ +0x101a  nt!PspInsertThread          ← 이번 바퀴 (Q4) 첫 직원 들여보내기
              │            └─ +0x807 nt!PspCallProcessNotifyRoutines  ← 이번 바퀴 (Q4) 바깥에 알리기
              └─ …        손잡이(핸들) 건네기 등
 ─ ─ (2.2 공백: 첫 직원은 언제 일을 시작하나?) ─ ─
 test.exe 첫 스레드 출발 (커널) nt!PspUserThreadStartup
  └─ ntdll!LdrInitializeThunk  (유저 쪽 첫 자리)
      └─ ntdll 로더: dll 올리기, TLS 콜백, DllMain
          └─ main
```

- 이번 구간에서 일하는 건 여전히 **부모 cmd.exe 의 스레드 하나**다 `[관찰]` (run3).
- `PspInsertThread` 는 `NtCreateUserProcess` 의 큰 줄기에서 **마지막 큰 상자**다. 그 뒤엔 손잡이 건네기·결과 적기뿐이다 (run9).
- 그래서 이번 바퀴로 **그림 1 의 nt 함수(Q1 ~ Q4)를 한 번씩 다 열어 봤다** (`plan.md` 결정 기록).

### 무엇이 궁금했나

run 문서 "목적" 그대로, 네 가지다.

1. `PspInsertThread` 안에서 무엇을 **어떤 흐름 순서로** 부르나.
   - run9 때는 대표 3개(멈춤·알림·준비)만 알았다.
   - 특히 `KeStartThread`(`+0x5e2`), 객체 등록, 스레드 알림, 스레드 핸들, 잡 배정이 어디에 끼나.
2. run11 에서 남긴 질문: `PspAllocateThread` 가 잡고 안 푼 **스레드 락**("작성 중" 자물쇠)을 `PspInsertThread` 가 푸나.
3. `KeStartThread` 와 `KeReadyThread` 는 뭐가 다른가. 멈춘 채(suspend)일 때 `KeReadyThread` 를 건너뛰나.
4. `PspCallProcessNotifyRoutines` 의 **간접 호출 4곳**은 무엇을 부르는 자리인가. `PsTerminateProcess`(`+0x176`)는 언제 타나.

### 바퀴 전에 알던 것

- `[관찰]` (run3) 부모 cmd.exe 스레드에서
  - `NtCreateUserProcess +0x101a` → `PspInsertThread`
  - `PspInsertThread +0x807` → `PspCallProcessNotifyRoutines`
  - 그 시점에 `!process 0 0 test.exe` 로 test.exe 가 **보였다.**
- `[정적]` (run9) `PspInsertThread` 안의 세 자리
  - `+0x2e5` `KeSuspendThread` — "`ThreadFlags` bit0" 가 켜졌을 때만 (이 표현은 이번에 다듬어진다 → 2.11)
  - `+0x807` 알림 — 오프셋은 크지만 `+0x5a6` 보다 먼저 ([run9 해설 3.7](run9-create-user-process-해설.md#37-오프셋이-큰데-먼저-실행되는-줄--오프셋-순서--실행-순서))
  - `+0x5a6` `KeReadyThread` — 멈춤 여부와 상관없이 지나는 끝 쪽
  - 그래서 run9 의 흐름은 **멈춤 → 알림 → 준비** 였다.
- `[추측]` (run9) 알림 덩어리는 **첫 스레드일 때만** 들어가는 모양.
- `[공백]` (run9) `PspMapThreadCreationFlags` 가 bit0 를 그대로 bit0 로 옮기는지 몰랐다.
- `[정적]` (run11) `PspAllocateThread` 는 스레드 락(`+0x51c` 에서 잡음)을 **풀지 않고** 돌아간다. `[추측]` `PspInsertThread` 가 푼다.
- `[추측]` (flow.md) 알림 받는 쪽은 백신·EDR 같은 **드라이버**들.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서 "호출 관계" 를 조금 줄였다.

```
NtCreateUserProcess
 └─ (10) +0x101a  PspInsertThread                      [관찰] run3 + [정적]   (아래 번호 = 흐름 순서)
                   ├─ (1) +0x5e2  KeStartThread          [정적] 제일 먼저
                   ├─ (2) +0x2e5  KeSuspendThread        [정적] flags bit0 일 때만
                   │     (+0x3ea ObInsertObjectEx 객체 등록 → +0x499/+0x4b2 스레드 락 풀기)
                   ├─ (3) +0x807  PspCallProcessNotifyRoutines   [관찰] run3 + [정적] 첫 스레드만
                   │               ├─ (1) +0x152  (간접·대상 모름) 등록 목록, Ex 형식  ┐ 칸마다
                   │               ├─ (2) +0x321  (간접·대상 모름) 등록 목록, 옛 형식  ┘ 둘 중 하나
                   │               ├─ (3) +0xc3   (간접·대상 모름) BAM 표 첫 칸
                   │               └─ (4) +0x1d4  (간접·대상 모름) DAM 표 첫 칸
                   ├─ (4) +0x4fa  PspCallThreadNotifyRoutines    [정적] 모든 스레드
                   │     (+0x525 PspCreateObjectHandle 스레드 핸들, +0x8ef 잡 배정 조건부)
                   └─ (5) +0x5a6  KeReadyThread          [정적] 멈춘 스레드도 똑같이
```

읽을 때 주의:

- 오프셋은 모두 **돌아올 자리(ret)** 다. 콜스택에 찍히는 숫자 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)).
- **이번엔 `(1)` `(2)` … 가 "흐름 순서"** 다. 오프셋 크기 순서가 **아니다.** (run11 까지는 코드 순서와 거의 같았다.) → 2.3 에서 자세히.
- 괄호 줄 `( … )` 은 그림 상자로는 안 넣은, 사이에 끼는 일이다.
- `[관찰]` 이 붙은 건 run3 에서 본 두 줄(`+0x101a`, `+0x807`)뿐이다. 나머지는 **노선도에 길이 있다**는 것까지다.
- 장부(`_calls.tsv`)에 `PspInsertThread` 의 직접 호출은 **62줄** (call 59 + 조각으로 가는 tail 3) 이다 `[정적]`. 위는 그중 대표만.

### 2.2 한 줄씩 읽는 법

트리 한 줄을 "**누가 / 어디서 / 누구를 / 무엇을 위해**" 로 풀어 읽어 보자.

**`PspInsertThread` 의 (1) `+0x5e2` `KeStartThread` `[정적]`**

- 누가: `PspInsertThread` (← `NtCreateUserProcess +0x101a`)
- 어디서: 돌아올 자리 `+0x5e2` (부르는 자리 `+0x5dd`)
- 누구를: `KeStartThread`
  - run 문서 "하는 일 단위" 표에서 `KeSuspendThread`·`KeReadyThread` 와 함께 **스케줄러** 묶음에 든다.
  - **스케줄러** = 어느 스레드가 언제 CPU 를 쓸지 정하는 커널 쪽 일 `[추측]` (일반적으로 알려진 설명). 비유: 어느 직원이 언제 계산대에 설지 정하는 관리사무소의 **배치 담당**.
- 무엇을 위해: 이름과 지도로 보면 "어느 CPU 에서 돌지, 우선순위 가산" 같은 **근무 조건 정하기** `[추측]` → 2.9.
- 눈여겨볼 점: 오프셋 `+0x5e2` 는 함수의 **꽤 뒤쪽**인데, 흐름으로는 **제일 먼저** 탄다 `[정적]`. → 2.3.
- 디버거로 보면: 콜스택에 `nt!PspInsertThread+0x5e2` 로 찍힐 자리다 (아직 안 봤다).

**`PspCallProcessNotifyRoutines` 의 (1) `+0x152` (간접·대상 모름) `[정적]`**

- 누가: `PspCallProcessNotifyRoutines` (← `PspInsertThread +0x807`)
- 어디서: 돌아올 자리 `+0x152` (부르는 자리 `+0x14d`)
- 누구를: **모른다.** 함수 이름이 코드에 박혀 있지 않은 [간접 호출](run9-create-user-process-해설.md#직접-호출과-간접-호출)이다.
  - 아는 건 "**어디서 주소를 꺼내나**" 뿐: `PspCreateProcessNotifyRoutine` 이라는 64칸짜리 목록의 한 칸 → 거기 꽂힌 블록의 `+8` 칸.
- 무엇을 위해: 드라이버가 "새 프로세스 생기면 불러 줘" 하고 맡겨 둔 함수를 부르는 자리로 보인다 `[추측]` (flow.md). → 2.7.
- 그래서 flow.md 그림 1 에서 이 상자는 **회색**이다. 상자 글이 함수 이름이 아니라 "주소 꺼내는 곳" 이다 (→ 3절).

### 2.3 오프셋 순서와 흐름 순서가 다르다

이번 바퀴에서 제일 헷갈리기 쉬운 대목이다. 천천히 가 보자.

**먼저, run9 에서 이미 한 번 봤다**

- run9 해설 3.7: `+0x807`(알림)이 `+0x5a6`(준비)보다 숫자가 큰데 **먼저** 실행됐다.
- 이유: `+0x4b8` 에서 뒤쪽 덩어리로 건너가 알림을 부르고, `+0x809` 에서 `+0x4be` 로 돌아왔기 때문 `[정적]`.
- 비유: 레시피 중간에 "**부록 3쪽을 보고 다시 여기로 오세요**". 부록은 책 뒤쪽에 있지만 먼저 읽는다.

**이번엔 그런 "부록 보고 오기" 가 여러 번이다**

결과서가 `.asm` 의 점프를 하나하나 따라가서 흐름 순서를 정했다 `[정적]`. 대표 지점만 흐름 순서대로 늘어놓으면 이렇다 (run 문서 "보통 길" 14단계에서 골랐다).

| 흐름 순서 | 하는 일 | ret 오프셋 |
|---|---|---|
| 1 | **`KeStartThread`** | `+0x5e2` |
| 2 | 스레드 수 올리기 → 첫 스레드 판정 | `+0x792` |
| 3 | 프로세스의 스레드 목록에 달기 (푸시락 잡기 ~ 풀기) | `+0x6c0` ~ `+0x266` |
| 4 | 프로세스 락 풀기 | `+0x2a2` |
| 5 | **`KeSuspendThread`** (flags bit0 일 때만) | `+0x2e5` |
| 6 | **`ObInsertObjectEx`** (스레드 객체 등록) | `+0x3ea` |
| 7 | **스레드 락 풀기** | `+0x499` / `+0x4b2` |
| 8 | **`PspCallProcessNotifyRoutines`** (첫 스레드만) | `+0x807` |
| 9 | **`PspCallThreadNotifyRoutines`** | `+0x4fa` |
| 10 | **`PspCreateObjectHandle`** (스레드 핸들) | `+0x525` |
| 11 | (조건부) `PspAssignProcessToJobList` | `+0x8ef` |
| 12 | **`KeReadyThread`** | `+0x5a6` |

오른쪽 숫자만 위에서 아래로 읽어 보면:

- `+0x5e2` → `+0x792` → `+0x6c0` → `+0x266` → `+0x2a2` → `+0x2e5` → `+0x3ea` → `+0x4b2` → `+0x807` → `+0x4fa` → `+0x525` → `+0x8ef` → `+0x5a6`
- 커졌다 작아졌다 한다. 코드가 **뒤로 갔다가, 앞으로 돌아왔다가, 다시 뒤로** 간다는 뜻이다.
- 비유: 이 레시피 책은 "부록 보고 오기" 가 여러 군데다. 쪽수 순서대로 읽으면 요리 순서가 엉망이 된다.

**그래서 `KeStartThread` 는 왜 "제일 먼저" 인가**

- 근거 `[정적]`: 결과서가 입구부터 점프를 따라가 보니, 앞쪽 검사(프로세스 비트 등)를 통과한 정상 길에서 처음 만나는 대표 호출이 `+0x5dd` 의 `KeStartThread` 였다.
- 그 뒤에야 스레드 목록에 달기 → 멈춤 → 객체 등록 순으로 간다.
- 즉 숫자는 뒤쪽이어도 **"스레드를 목록에 올리기 전, 객체 등록 전"** 자리다 (flow.md 2.2 `KeStartThread` 절).

**왜 디컴파일(`.c`)을 안 보고 `.asm` 점프를 따라갔나**

- `PspInsertThread.c` 는 Ghidra 가 스택 값을 잘못 읽은 곳이 있어서, 흐름이 어긋나 보였다 (결과서).
- 그래서 흐름 판단은 전부 `.asm` 점프로 했다 `[정적]`.

**run9 의 "멈춤 → 알림 → 준비" 는 어디에 끼나**

run9 의 세 개는 **순서 그대로 맞았다** `[정적]`. 그 사이사이에 새 줄이 끼었을 뿐이다.

```
run9 :                멈춤 ─────────────────────────→ 알림 ─────────────────────→ 준비
run12: KeStartThread → 멈춤 → 객체 등록 → 락 풀기 → 알림(프로세스) → 알림(스레드) → 핸들 → (잡) → 준비
       (+0x5e2)       (+0x2e5) (+0x3ea)  (+0x4b2)  (+0x807)       (+0x4fa)       (+0x525) (+0x8ef) (+0x5a6)
```

- 앞에: `KeStartThread` 가 멈춤보다도 **앞**.
- 멈춤과 알림 사이: **객체 등록 → 스레드 락 풀기**.
- 알림 뒤: **스레드 핸들 → (잡)**.
- 그래서 flow.md 그림 1 의 번호도 `(1) Suspend (2) 알림 (3) Ready` 에서 `(1) KeStartThread (2) KeSuspendThread (3) 프로세스 알림 (4) 스레드 알림 (5) KeReadyThread` 로 다시 매겼다 (run 문서 "정정").

비유로 한 번에 (태그 없음, flow.md 의 비유):

- 근무 조건 정하기(`KeStartThread`) → 부서 명단에 이름 적기 → (대기 팻말) → 사원 등록 → "작성 중" 자물쇠 풀기 → 사장님·관리자들에게 "새 사람 왔어요" → 손잡이 만들기 → 출근 대기 줄에 세우기(`KeReadyThread`).

### 2.4 run11 의 "작성 중" 자물쇠를 여기서 푼다

run11 에서 남긴 질문의 답이다. **푼다** `[정적]`. run11 의 `[추측]` 이 맞았다.

**같은 자물쇠인가 — 주소로 맞춰 본다** `[정적]`

| 누가 | 어디서 | 명령 | 뜻 |
|---|---|---|---|
| `PspAllocateThread` (run11) | `+0x4ee` | `LEA R12,[RSI+0x590]` | 새 스레드 `+0x590` 칸의 자물쇠를 **잡으려고** 주소를 쥔다 |
| `PspInsertThread` (run12) | `+0x480` | `LEA RAX,[R14+0x590]` | 새 스레드 `+0x590` 칸의 자물쇠를 **풀려고** 주소를 쥔다 |

- `RSI` 와 `R14` 는 각 함수 안에서 **새 스레드**를 가리킨다 (결과서).
- 둘 다 "새 스레드 + `0x590`" 이니 **같은 자물쇠**다.
- 비유: run11 의 인사 카드 담당이 카드에 "작성 중" 자물쇠를 건 채 넘겼고, 다음 담당(`PspInsertThread`)이 **그 자물쇠**를 연다.

**어디서 푸나** `[정적]` (보통 길)

1. `+0x490` — 스레드 플래그 `스레드+0x5a0` 에 `0x2` 를 켠다. 뜻은 "삽입됨" 으로 보인다 `[추측]`.
2. **`+0x499` `XADD.LOCK`** — 자물쇠를 푼다.
   - **XADD** = 칸의 값에 어떤 수를 더하면서, 더하기 전 값을 돌려받는 CPU 명령이다. `LOCK` 이 붙으면 그 사이에 다른 CPU 가 끼어들지 못한다 `[추측]` (일반적으로 알려진 설명).
   - 여기선 `-1` 을 더해서 자물쇠를 연다 `[정적]` (결과서: `RBX=-1`).
   - 비유: 자물쇠 다이얼을 한 칸 돌려 여는 동작. 함수를 부르지 않고 **그 자리에서 손으로** 연다.
3. **`KeAbPostRelease`** call `+0x4ad` / ret `+0x4b2` — 풀고 난 **뒤처리** (run 문서 표현).
   - 기다리는 쪽이 있을 때만 그 사이에 `ExfTryToWakePushLock` 이 끼어든다 (결과서).

**언제 푸나** `[정적]`

- `ObInsertObjectEx`(`+0x3ea`, 객체 등록)가 끝난 **직후**.
- 알림(`+0x807`, `+0x4fa`)과 핸들(`+0x525`)은 자물쇠를 **푼 뒤**다.
- 비유: 사원 등록이 끝나야 "작성 중" 을 떼고, 그다음에야 바깥에 소개하고 손잡이를 만든다.
- 실패 길에서 푸는 자리도 따로 있다 (`+0xa13` 이후, 초반 실패 `+0x167 ~ +0x18c`).

**이 함수 안의 다른 자물쇠 둘** (헷갈리지 말자)

| 자물쇠 | 어디 칸 | 누가 잡나 | 어디서 푸나 | 태그 |
|---|---|---|---|---|
| 스레드 락 ("작성 중") | `스레드+0x590` | `PspAllocateThread` (run11) | `+0x499` / `+0x4b2` | `[정적]` |
| 프로세스 락 | `프로세스+0x1c8` | **호출자 쪽** (`PspAllocateProcess +0x26e9 → PspLockProcessExclusive`) | `+0x2a2` | `[정적 + 간접]` |
| 스레드 목록 푸시락 | `프로세스+0x5e0` | `PspInsertThread` 자신 (`+0x6c0`) | `+0x266` | `[정적]` |

- **푸시락** = 커널이 짧은 일에 쓰는 가벼운 자물쇠 종류 `[추측]` (일반적으로 알려진 설명). 비유: 서랍 하나에 거는 작은 걸쇠.
- 셋째 줄 사이에서 하는 일: 프로세스의 **스레드 목록 꼬리에 새 스레드를 직접 단다** `[정적]`. 호출 없이 직접 코드다. 목록이 깨졌으면 `+0x703` `INT 0x29` 로 그 자리에서 멈춘다.
  - run11 의 `PspInsertProcess` 가 상가 명부에 **직접 손으로 적던** 것과 같은 모양이다 ([run11 해설 2.8](run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다)). 이번엔 가게 안 **직원 명단**이다.
- 둘째 줄 "프로세스 락" 이 `[정적 + 간접]` 인 이유:
  - `PspInsertThread` 안에서 `+0x2a2` 로 **푸는** 건 코드로 봤다.
  - 그런데 **잡는** 자리는 이 함수 안이 아니다. `PspAllocateProcess +0x26e9` 가 잡는다는 걸 장부로, `PspLockProcessExclusive`·`PspUnlockProcessExclusive` 가 `+0x1c8` 을 잡고 푸는 함수라는 걸 원본 바이트로 봤다.
  - 두 쪽을 이어 붙인 결론이라 "간접" 이다.

### 2.5 flags 는 어디서 오나 — "전제"

`PspInsertThread` 는 4번째 인자가 가리키는 값(결과서가 `flags` 라고 부름)을 보고 갈림길을 정한다. 이 값의 출처도 이번에 보였다 `[정적]` (원본 바이트).

**한 단계씩**

1. 유저가 `NtCreateUserProcess` 에 주는 `ThreadFlags` — 입구 검사로 **bit0 만 허용**된다 (run9, [비트와 플래그](run9-create-user-process-해설.md#비트와-플래그)).
2. `NtCreateUserProcess +0xdce` (ret `+0xdd3`) `PspMapThreadCreationFlags` 가 그걸 **flags 로 옮겨 적는다.**
   - bit0 → **bit0 그대로**. (나머지: `0x2`→`0x2`, `0x4`→`0x4`, `0x10`→`0x80`, `0x20`→`0x100`, `0x40`→`0x200`)
   - run9 해설 3.6 의 "근거 2" 에서 `[공백]` 이던 것이 풀렸다.
   - 1번과 합치면, 이 길에서 flags 에 실려 오는 `ThreadFlags` 비트는 **bit0 하나뿐**이다.
3. `NtCreateUserProcess +0xdfd` 가 flags 에 `0x60` 을 **무조건** 켠다 (`OR dword ptr [RBP+0x10],0x60`).
   - `0x20` = "프로세스 락은 이미 잡아 왔음". 그래서 `PspInsertThread` 입구(`+0x77`)의 `flags & 0x20` 검사는 이 길에서 늘 참 → 락을 **새로 안 잡는다**.
   - `0x40` = 늘 참 → `PspChangeProcessExecutionState` `+0x2ba` 를 탄다.
4. `PspInsertThread` 가 flags bit0 를 보고 `KeSuspendThread`(`+0x2e5`)를 부를지 정한다.

비유 (태그 없음): 신청서의 첫 체크 칸(`ThreadFlags` bit0)을 관리사무소 내부 양식(flags)에 **같은 칸**으로 옮겨 적고, 내부 양식엔 "자물쇠는 이미 잠가 왔음" 같은 도장(`0x60`)을 늘 찍어서 다음 담당에게 넘긴다.

### 2.6 가게 알림은 첫 직원 때만, 직원 알림은 매번

`PspInsertThread` 안에는 알림이 **두 종류**다.

| 알림 | 자리 | 언제 | 태그 |
|---|---|---|---|
| 프로세스 알림 `PspCallProcessNotifyRoutines` | `+0x807` | **첫 스레드만** | `[정적]` + `[관찰]` run3 (첫 스레드 때 불림) |
| 스레드 알림 `PspCallThreadNotifyRoutines` | `+0x4fa` | **모든 스레드** | `[정적]` |

비유: 가게에 **첫 직원**이 들어올 때 "새 **가게** 열었어요" 를 한 번 알린다. "새 **직원** 왔어요" 는 직원이 들어올 때마다 알린다.

**첫 스레드 판정** — run9 의 `[추측]` 이 `[정적]` 이 됐다

1. 스레드 수(`프로세스+0x380`)를 1 올린다.
2. 그 값이 1 이고, `프로세스+0x7dc` bit0 가 **이번에 처음** 켜지면,
3. 함수 안 임시 값(지역 값)에 bit1 을 켠다 (`+0x792` `OR EDI,0x2`).
4. 나중에 `+0x4b4` `TEST DIL,0x2` 가 이 비트를 보고, 켜져 있으면 뒤쪽 덩어리(프로세스 알림)로 간다.

- 비유: 직원 수 칸을 1 올려 보고 "1명이네? 첫 직원이다" 하고 메모지에 체크해 둔다. 조금 뒤 그 체크를 보고 가게 개업 알림을 돌린다.
- `[관찰]` run3 에서 test.exe 첫 스레드 때 `+0x807` 로 불린 것과 맞는다.
- 다만 **둘째 스레드 때 안 불리는 것**은 아직 디버거로 안 봤다.

**알림 문턱** (두 알림이 같은 조건, `+0x7ee` / `+0x4e3`) `[정적]`

- `프로세스+0x5fc` bit0 가 **꺼져 있으면** 부른다.
- 켜져 있으면 `프로세스+0x640` 이 0 이 아닐 때만 부른다.
- 뜻은 `[추측]`: `+0x640` 은 `PspCallProcessNotifyRoutines` 안에서 **Pico** 프로세스 표시로 쓰이는 값이라 ([Pico](run11-allocate-thread-해설.md#24-갈림길-작업대개인-안내판은-언제-만드나) 는 run11 해설 2.4), `+0x5fc` bit0 는 "일반 프로세스가 아닌 종류" 표시로 보인다. test.exe 는 꺼져 있을 것 `[추측]`.

**다른 스레드들은?**

- `PspInsertThread` 를 부르는 곳은 `NtCreateUserProcess`, `PspCreateThread`, `PspCreatePicoThread` 셋이다 `[정적]`.
- `[추측]` (flow.md 2.6) test.exe 의 나중 스레드들(`test!Worker`, ntdll 일꾼)이 `PspCreateThread` → `PspInsertThread` 를 거친다면, **스레드 알림만** 가고 프로세스 알림은 안 간다. 걸어 보진 않았다.

### 2.7 드라이버 알림 — 간접 호출 4곳

이제 `+0x807` 상자 안, `PspCallProcessNotifyRoutines` 다.

처음 나온 말:

- **알림 함수(콜백)** = 드라이버가 커널에 "이런 일 생기면 **이 함수를 불러 줘**" 하고 미리 맡겨 둔 함수 `[추측]` (일반적으로 알려진 설명).
  - 비유: 관리사무소 게시판에 붙은 **연락처 명단**. "새 가게 생기면 여기로 전화 주세요" — 경비업체(백신·EDR) 같은 곳들이 적어 둔다.
  - 관리사무소는 명단을 **위에서부터 한 칸씩** 보며 전화를 건다. 명단에 누가 적혀 있을지는 그날그날 다르다.
- 그래서 코드만 봐서는 **누구에게 전화가 가는지 모른다.** 아는 건 "**명단이 어디 붙어 있나**" 뿐이다. 이게 [간접 호출](run9-create-user-process-해설.md#직접-호출과-간접-호출)의 뜻이다.

**전화번호를 꺼내는 곳 4가지** `[정적]` (이름은 PDB 데이터 심볼)

| ret (call) | 어디서 함수 주소를 꺼내나 | 넘기는 것 | 언제 가나 |
|---|---|---|---|
| `+0x152` (`+0x14d`) | `PspCreateProcessNotifyRoutine` (64칸) 한 칸 → 블록 `+8` | `(Process, 프로세스 id, info)` — **Ex 형식** | 블록 플래그 bit1 켜짐 + 전역 `PspNotifyEnableMask` bit2 켜짐 |
| `+0x321` (`+0x31c`) | 같은 목록, 블록 `+8` | `(부모 id, 프로세스 id, Create)` — **옛 3인자 형식** | 블록 플래그 bit1 꺼짐 |
| `+0xc3` (`+0xbe`) | `PspBamExtensionHost` → 표 첫 칸 | `(Process, 프로세스 id, info)` | 표가 있고, `info` 가 없거나 CreationStatus 가 음수가 아닐 때 |
| `+0x1d4` (`+0x1cf`) | `PspDamExtensionHost` → 표 첫 칸 | BAM 과 같음 | BAM 과 같음 |

말 풀이:

- **콜백 블록** = 명단 한 칸에 꽂힌 **카드**. 카드 `+8` 자리에 전화번호(함수 주소)가 적혀 있다 `[정적]`. 카드를 꺼낼 때 `ExReferenceCallBackBlock`, 다 쓰면 `ExDereferenceCallBackBlock` 으로 돌려놓는다 (결과서).
- **Ex 형식 / 옛 형식** = 같은 명단 안에 **두 종류 카드**가 섞여 있다. 카드의 플래그 bit1 로 구분한다 `[정적]`.
  - Ex 형식: 가게 장부(`Process`)와 **신고서 사본(`info`)** 까지 건넨다.
  - 옛 형식: 부모 id, 프로세스 id, "생김/없어짐" 표시(`Create`) 세 개만 건넨다.
  - 비유: 새 연락처는 "자세한 신고서" 를 받고, 옛 연락처는 "몇 번 가게가 생겼어요" 한 줄만 받는다.
- **`info`** (이 함수 안) = 알림 함수에게 건네는 **0x48 바이트 신고서 사본** `[정적]`.
  - 모양이 `PS_CREATE_NOTIFY_INFO` 와 같다 `[추측: 오프셋 대응]`.
  - 그 안의 "**만든 스레드 id**" 칸에는 **지금 도는 스레드** = 부모 cmd.exe 스레드를 넣는다 `[정적]`. run3 `[관찰]`(부모 스레드에서 불림)과 맞는다.
  - 주의: 결과서는 `PspInsertThread` 의 7번째 인자(`NtCreateUserProcess` 의 지역 구조체)도 `info` 라고 부른다. 여기 `info` 는 이 함수 **안에서 새로 만드는** 0x48 바이트 묶음이다 (`+0x1f4 ~ +0x288`).
- **BAM / DAM** = PDB 에 있는 이름이다. 윈도우의 **백그라운드 / 데스크톱 활동 관리** 확장이 등록한 표로 보인다 `[추측]` (이름으로 본 것). 비유: 게시판 명단과 별도로, 관리사무소 **안쪽 부서 두 곳**에도 따로 알린다.

**전화 거는 순서** `[정적]` (`.asm` 점프를 따라 봄)

1. `info`(신고서 사본) 만들기 — `+0x1f4 ~ +0x288`
2. **명단 64칸 돌기** — `+0x101 ~ +0x128`. 칸마다 카드 종류에 따라 `+0x152`(Ex) 또는 `+0x321`(옛) **둘 중 하나**.
   - 전역 `PspNotifyEnableMask` 의 bit1·bit2 가 **둘 다 꺼져 있으면** 이 단계를 통째로 건너뛴다 (`+0x91 ~ +0x9e`).
3. 다 돌면 `+0x17e` 에서 `+0xa0` 으로 → **BAM** `+0xc3`
4. **DAM** `+0x1d4` → 끝

- 그래서 트리에서 (1)(2) 옆에 "칸마다 둘 중 하나" 라고 묶어 둔 것이다.
- 덤 `[정적]`: 옛 형식 쪽에서 대상이 `KeReleaseSpinLock` 이면 `+0x1b4` 에서 그걸 **직접** 부르는 갈래가 따로 있다 (run 문서).

**`PspInsertThread` 에서 부르면 "생김" 길이다** `[정적]`

- 3번째 인자 `Create` 가 0 이 아니면 `+0x86` 에서 `info` 만들기(`+0x1f4`)로 간다.
- `PspInsertThread` 는 `+0x7f4` 에서 `Create = 1` 을 준다.
- 다른 caller `PspExitProcess +0xa7` 은 `(Process, NULL, 0)` 으로 부른다 (원본 바이트). 가게가 **문 닫을 때**도 같은 명단에 알린다는 뜻이다.

**대상 함수 이름은 정적으로 모른다**

- 명단 칸·표 첫 칸에 **실제로 어떤 함수가 있는지**는 실행 때 정해진다 (run 문서).
- `[추측]` (flow.md) 백신·EDR 같은 드라이버들. 하지만 이 VM 에 무엇이 등록돼 있는지는 디버거로만 안다 → Q39 (4절).

### 2.8 알림 받은 쪽이 "안 돼" 하면 — `PsTerminateProcess` 로 가는 길

알림은 "알려 주기" 로 끝나지 않는다. **거부권**이 있다 `[정적]`.

**단계별로**

1. 명단 한 칸(Ex 형식)에 전화를 건다 (`+0x152`).
2. 끊고 나서 `+0x162` 에서 `info` 의 **CreationStatus** 칸(`+0x40`)을 읽는다.

   ```
   +0x162  MOV EAX,[RBX+0x40]   ; 신고서의 CreationStatus 칸
           TEST / JNS           ; 음수가 아니면 다음 칸으로
   ```

   (오른쪽 `;` 뒤 설명은 이 해설이 붙인 것이다.)

   - **CreationStatus** = 신고서의 "**허가 / 거부**" 도장 칸. 알림 함수가 여기에 **음수**를 써 넣으면 거부다 `[정적]`.
   - 윈도우 커널 상태 값은 음수가 실패를 뜻한다 `[추측]` (일반적으로 알려진 설명).
3. 음수면 `+0x171` (ret `+0x176`) 에서 **`PsTerminateProcess(Process, 그 값)`** 을 부른다. 그리고 **명단 돌기를 멈춘다.** `+0x17e` 로 가서 BAM·DAM 도 건너뛴다 (flow.md).
4. 그 음수가 반환값으로 `PspInsertThread +0x807` 에 돌아간다.
5. `PspInsertThread` 는 `+0x505` `JS`(음수면 점프)로 **스레드 핸들 만들기(`+0x525`)를 건너뛰고** 실패 길로 간다.
6. 실패 길: `KeRequestTerminationThread` → 그래도 **`KeReadyThread`** → 음수 반환.

비유 (태그 없음):

- 경비업체에 "새 가게 생겼어요" 전화를 걸었더니, 신고서에 **거부 도장**을 찍어 돌려보냈다.
- 관리사무소는 그 자리에서 가게 **폐업 절차**(`PsTerminateProcess`)를 시작하고, 남은 연락처엔 전화하지 않는다.
- 직원에게 손잡이도 안 만들어 준다.

**옛 형식 카드는 거부 못 한다** `[정적]`

- 옛 3인자 형식은 `info`(신고서)를 **아예 못 받는다.** 도장 찍을 칸이 없으니 거부도 못 한다.
- 비유: "몇 번 가게 생겼어요" 한 줄만 받는 연락처는, 들을 수만 있고 반대할 수는 없다.

**test.exe 때는?**

- 보통 안 탈 것 `[추측]`. test.exe 가 멀쩡히 `main` 까지 갔으니까(01_sprint). 하지만 `+0x171` 이 정말 안 걸리는지는 아직 안 봤다 → Q39.

### 2.9 `KeStartThread` 와 `KeReadyThread` 는 뭐가 다른가

둘 다 본문을 뽑지 않았다. **이름과 지도로만** 본 것이라 `[추측]` 이다.

| | `KeStartThread` | `KeReadyThread` |
|---|---|---|
| 흐름 자리 `[정적]` | 맨 앞 (`+0x5e2`) | 맨 끝, 모든 길이 모이는 곳 (`+0x5a6`) |
| 크기 | 1182바이트 | 111바이트 |
| 지도에서 부르는 것 | `KiAcquireProcessLockExclusive`, `KeSelectInitialIdealProcessorForThread`, `KiAdjustProcessIdealProcessorSetsForThreadCreation`, `KiUpdateSharedReadyQueueAffinityThread`, `KiInitializeForegroundBoostThread` 등 | `KiFastReadyThread`, `KiInSwapSingleProcess` 만 |
| 하는 일로 보이는 것 `[추측]` | "어느 CPU 에서 돌지·우선순위 가산" 같은 **스케줄링 준비** | **실행 대기 줄에 넣기** |

- `KeStartThread` 의 지도에는 **대기 줄에 넣는 호출이 없다** (run 문서).
- 그래서 `[추측]` "실행될 수 있는 상태" 가 되는 건 `KeStartThread` 가 아니라 끝의 `KeReadyThread` 쪽이다 (flow.md).
- 비유 (flow.md): `KeStartThread` 는 **근무지·근무 시간 정하기**, `KeReadyThread` 는 **출근 대기 줄에 세우기**. 이름에 "Start" 가 있어도 출발 신호는 아니다.

### 2.10 멈춘 직원도 대기 줄에 선다 — 그럼 언제 실제로 서 있나?

run 문서 목적 3번의 뒤쪽 질문 "멈춘 채일 때 `KeReadyThread` 를 건너뛰나?" 의 답은 **안 건너뛴다** `[정적]`.

**근거를 하나씩**

1. `+0x5a6` 은 **모든 길의 끝**이다. 멈춤 여부로 갈리는 분기가 **없다** `[정적]`.
2. 실패 길도 여기로 온다 `[정적]`.
   - 알림이 거부했거나 객체 등록이 실패하면, 함수 몸통에서 떨어진 **조각** `FUN_140b0d71b` 로 간다.
     - **조각** = 함수 몸통의 일부가 멀리 따로 떨어져 있는 덩어리. 자주 안 가는 길을 모아 둔 곳 ([run11 해설 2.11](run11-allocate-thread-해설.md#211-그-밖에-확인된-것) 의 "먼 덩어리" 와 같은 종류).
   - 조각 안에서 `KeRequestTerminationThread` 를 부르거나, 다른 갈래에선 멈춰 둔 스레드를 `KeForceResumeThread` 로 푼다.
   - 그리고 조각 안 `JMP` 3곳(`+0x1f`, `+0x31`, `+0x40`)이 전부 `+0x59e` 로 **돌아온다.** 거기서 `KeReadyThread`.
3. 지도 `callers_total` = 1: 커널 안에서 `CALL` 로 `KeReadyThread` 를 부르는 함수는 `PspInsertThread` 하나뿐이다 `[정적]` (함수 포인터 호출은 못 셈).

**그래서:**

- "멈출 거면 줄에 안 세운다" 가 **아니다.**
- "**대기 팻말을 먼저 걸고**, 그다음 줄에 세운다" 모양이다 (run 문서).
- 비유 (태그 없음): 팻말 건 직원도 출근 대기 줄에는 선다. 줄에 섰다고 바로 계산대에 가는 건 아니다.
  - flow.md 의 `[추측]`: 팻말이 걸려 있으면 줄에 서도 바로 일하진 못한다.

**그럼 언제 "실제로 서 있게" 되나 — 모른다**

- 팻말(`KeSuspendThread`)이 **언제 효력을 내는지**는 `KeSuspendThread` 안쪽 일이다. 이번엔 본문을 안 뽑았다 → 정적으로 못 정했다 (run 문서).
- 팻말을 **누가·언제 떼 주는지**도 여전히 `NtCreateUserProcess` 밖이다 → Q5.
- test.exe 첫 스레드가 **애초에 팻말을 걸었는지**(flags bit0)도 실행 때 값이다 → Q19, Q39.
- 세 질문 다 4절에 모아 뒀다.

### 2.11 정정 두 개 (+ 그림 번호)

run 문서 "정정" 절이다. 무엇이 틀렸고, 왜 생겼나.

**정정 1 — "`ThreadFlags` bit0" 라는 표현** (run9, flow.md 2.2·4.1)

- 무엇이: run9 에서 `KeSuspendThread` 조건을 "`ThreadFlags` bit0 일 때만" 이라고 썼다.
- 정확히는: **`PspInsertThread` 4번째 인자(flags)의 bit0** 다. `ThreadFlags` 는 `NtCreateUserProcess` 의 인자이고, `PspInsertThread` 가 보는 건 그걸 옮겨 적은 flags 다 (2.5).
- 뜻은 같다: bit0 → bit0 그대로라서, "`ThreadFlags` bit0 가 켜져 있으면 멈춘다" 는 내용은 맞다. **이름만 정확히** 한 것이다.
- 왜 생겼나: run9 때는 `PspMapThreadCreationFlags` 본문을 안 봐서 두 값을 구분할 근거가 없었다 (run9 해설 3.6 "근거 2" 가 `[공백]`). 이번에 옮겨 적는 모양을 보고 나서 이름을 갈랐다.
- 고친 곳: run9 문서는 그대로 두고, flow.md 2.2 를 고쳤다 (run 문서).

**정정 2 — flow.md 2.2 `KeReadyThread` 의 "실패면 `+0x598` 에서 다른 길로 빠짐"**

- 무엇이: run9 바퀴에 **flow.md 에만** 쓴 문장이다 (run 문서).
- 실제로는: 실패 길도 조각 `FUN_140b0d71b` 에서 `KeRequestTerminationThread`(또는 `KeForceResumeThread`)를 거친 뒤 `+0x59e` 로 **돌아와서** `KeReadyThread` 를 부른다 (2.10).
  - "다른 길로 빠짐" 이 아니라 "**돌아서 같은 자리로 옴**".
- 왜 생겼나: 근거 문서엔 이유가 따로 적혀 있지 않다. `[추측]` run9 는 `PspInsertThread` 를 "알려진 사실 대조용" 으로 처음 뽑은 바퀴라 (요청서), 실패 길이 건너간 **조각 안쪽의 되돌아오는 `JMP`** 까지는 따라가지 않았던 것으로 보인다. 함수 몸통만 보면 "빠져나간다" 로 읽히기 쉽다.

**덤 — 그림 1 번호를 다시 매김**

- run9: `(1) Suspend (2) 알림 (3) Ready`
- run12: `(1) KeStartThread (2) KeSuspendThread (3) 프로세스 알림 (4) 스레드 알림 (5) KeReadyThread`
- 셋의 앞뒤 순서는 run9 와 같다. 사이에 새 상자가 낀 것이다 (2.3).

### 2.12 그 밖에 확인된 것

- `[정적]` 62줄을 **하는 일 단위**로 묶으면 (run 문서 표, 대표만):
  - 스케줄러: `KeStartThread`, `KeSuspendThread`, `KeReadyThread`
  - 객체 등록·핸들: `SeCreateAccessStateEx` → `ObInsertObjectEx` → `PspCreateObjectHandle` → `SeDeleteAccessState`
  - 알림·추적: 두 알림 + `EtwTraceProcess`·`EtwTraceThread` (ETW = 커널의 사건 기록 장치 `[추측]` 일반적으로 알려진 설명. 추적이 켜졌을 때만 부른다 `[정적]`)
  - 잡: `ExAcquireResourceSharedLite`/`ExReleaseResourceLite`, `PspAssignProcessToJobList` (조건부, 7번째 인자 `info+0x170` ≠ 0)
  - 실패 정리: `KeUninitThread`, `MmDeleteTeb`, `PspDeleteUserStack`, 조각 안 `KeRequestTerminationThread`·`KeForceResumeThread` 등
- `[정적]` 스레드 핸들(`+0x525`)과 run11 의 객체 등록(`ObInsertObjectEx`)은 **다른 창구**다 ([run11 해설 2.9](run11-allocate-thread-해설.md#29-obinsertobjectex-는-부모에게-줄-손잡이가-아니다) 와 같은 짝). 이번엔 프로세스가 아니라 **스레드**의 등록·손잡이다.
  - 등록이 끝나면 호출자에게 TEB 주소·ClientId 를 써 주고, 핸들 값은 잡 단계 뒤에 써 준다 `[정적]`.
- `[정적]` `PspCallThreadNotifyRoutines` (`+0x4fa`, 456바이트)는 프로세스 쪽과 **같은 모양**(콜백 블록 목록 + CFG 간접 호출 3곳)이다. caller 는 `PspInsertThread +0x4fa` 와 `PspExitThread`.
  - `[추측]` 드라이버가 등록한 "스레드 생김/끝남" 알림을 부르는 곳. 본문은 안 뽑았다.
- `[정적]` 결과서가 `.asm` 에 빠진 49바이트 구멍(`+0x2e7 ~ +0x318`)을 원본 바이트로 확인했다. 그 안에 호출·분기는 **없다**. 그러니 이번 결론에 영향 없다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만 적는다.

| 자리 | 전 (run11 뒤) | 후 (run12) |
|---|---|---|
| 기준 | 정적 근거 run9 ~ run11 | 정적 근거 **run9 ~ run12** |
| 두 그림 사이 공백 설명 | 풀어 주는 호출이 `NtCreateUserProcess` 에 없다 | 한 줄 추가: `PspInsertThread` 는 멈춤 여부와 상관없이 끝에서 `KeReadyThread` 를 부른다 `[정적]` |
| 범례 | 색 = 모듈 6가지 | **회색 = 모듈을 모르는 함수** 추가. 간접 호출이라 대상이 실행 때 정해지고, 상자 글은 "주소를 꺼내는 곳" |
| 그림 1 | `PspInsertThread` 아래 `(1) Suspend (2) 알림 (3) Ready` | `(1) KeStartThread (2) KeSuspendThread (3) 프로세스 알림 (4) 스레드 알림 (5) KeReadyThread`. 상자 2개 추가(`KeStartThread`, `PspCallThreadNotifyRoutines`). `PspCallProcessNotifyRoutines` 아래 **회색 상자 4개** (`+0x152`, `+0x321`, `+0xc3`, `+0x1d4`) |
| 2.1 트리 | `PspInsertThread` 아래 3줄 | 위 2.1 트리와 같음 (번호 = 흐름 순서) |
| 2.1 `PspMapThreadCreationFlags` | `[공백]` bit0 가 bit0 로 가는지 모름 | **`[정적]`** bit0 → bit0 (원본 바이트). `+0xdfd` 의 `0x60` 무조건 |
| 2.1 `PspAllocateThread` 스레드 락 | `[추측]` 호출자가 푼다 | **`[정적]`** `PspInsertThread +0x499` / `+0x4b2` 가 같은 락(`+0x590`)을 푼다 |
| 2.1 `PspInsertThread` 절 | `[정적]` 3줄 순서 + `[추측]` 첫 스레드를 등록한다 | **`[정적]`** 62줄, 흐름 순서 12단계, 첫 스레드 판정, 알림 문턱, 거부 때 실패 길. 비유("회사 명부에 올리기")는 `[추측]` |
| 2.1 `PspCallProcessNotifyRoutines` 절 | `[추측]` 첫 스레드일 때만, `[추측]` 드라이버에 알림 | 첫 스레드만 → **`[정적]`**. 생성 길(`Create = 1`), 간접 호출 4곳 표, `PsTerminateProcess` 조건 `[정적]`. "드라이버(백신·EDR)" 는 `[추측]` 그대로 + "거부권도 있다" |
| 2.1 새 절 | — | `PspCallThreadNotifyRoutines` (정적) |
| 2.2 트리 | `ThreadFlags` → `PspMapThreadCreationFlags` → 멈춤 / 준비 | flags 이름 정리, `+0xdfd` 줄, `(1) KeStartThread` 추가, 준비는 "멈췄든 안 멈췄든, 실패 길도" |
| 2.2 `KeSuspendThread` | "플래그 bit0" | **flags bit0** + 정정 메모 (2.11 정정 1). 흐름 자리 추가 |
| 2.2 `KeReadyThread` | "실패면 `+0x598` 에서 다른 길로 빠짐" | **정정** (2.11 정정 2): 모든 길의 끝. 멈춘 스레드에도 불림 `[정적]`. 실제로 서는 시점은 `[공백]` |
| 2.2 새 절 | — | `KeStartThread` (정적): 흐름상 제일 먼저 `[정적]`, 하는 일은 `[추측]` |
| 2.6 다른 스레드 | `[추측]` `PspCreateThread` → `PspAllocateThread` | 한 줄 추가: `PspInsertThread` caller 에도 `PspCreateThread` `[정적]`. 스레드 알림만 갈 것 `[추측]` |
| 4장 공백 표 | — | 해결 5줄 (`PspMapThreadCreationFlags`, 스레드 락, `PspInsertThread` 순서, 첫 스레드만, `KeStartThread` 자리). 새 줄 5개 (간접 호출 대상, 알림 문턱, 프로세스 락, `KeStartThread` 본문, 실제로 서는 시점) |
| 4.1 | `Suspend`, `Ready` 2줄 | 9줄로 (각각 디버거로 무엇을 보면 되는지) |
| 5장 | run11 까지 | run12 줄, 요청서 번호 표에 run12 줄 |

태그로 보면 핵심 변화는 네 가지다:

- `PspInsertThread` 흐름: 대표 3개 → **12단계 `[정적]`** (오프셋 순서 아님).
- 스레드 락 누가 푸나: `[추측]` → **`[정적]`**.
- 프로세스 알림 "첫 스레드만": `[추측]` → **`[정적]`**.
- `PspMapThreadCreationFlags` bit0: `[공백]` → **`[정적]`**.

### 큰 그림에서 어떻게 이어지나

- **그림 1 의 nt 큰 상자를 한 번씩 다 열었다.**
  - run9 (`NtCreateUserProcess`) → run10 (가게 차리기) → run11 (첫 직원 뽑기, 상가 명부) → run12 (첫 직원 들여보내기, 바깥에 알리기).
  - 부모 cmd.exe 스레드가 커널에서 하는 일의 **큰 줄기**가 코드로 이어졌다 `[정적]`.
- **run11 에서 걸어 둔 자물쇠가 여기서 풀렸다.** 두 바퀴에 걸친 "잡기 → 풀기" 짝이 맞았다.
- **2.2 공백이 조금 좁혀졌다.**
  - 전: "멈춤 갈림길이 있다" 까지.
  - 후: "멈춰도 줄에는 선다 (`KeReadyThread`)" 까지. 남은 건 "**팻말을 누가·언제 떼나**" 하나로 모인다 → Q5 (정적), Q19 (동적).
- **run3 관찰과 또 맞았다.** "알림이 부모 cmd.exe 스레드에서 불림" `[관찰]` ↔ `info` 의 "만든 스레드 id" 칸 = 지금 도는 스레드 `[정적]`.
- **다음 구간** (`plan.md`: 다음 항목 Q5):
  - Q5 — 첫 스레드를 풀어 주는 함수, 부모 쪽에서 그걸 부르는 곳.
  - Q6·Q7 — 그림 2 쪽 첫 출발 (`PspUserThreadStartup`, 유저 첫 위치).
  - Q39 — 이번 바퀴의 동적 확인 (Q38 과 한 run 으로 묶을 수 있음).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 첫 스레드가 **멈춘 채인가** (flags bit0, `+0x2e0` 이 걸리나) | 실행 때 값이다 | Q19·Q39 (동적): `PspInsertThread` 입구 `poi(@r9) & 1` |
| 멈춘 스레드가 `KeReadyThread` 뒤 **실제로 서는 시점** (팻말이 언제 효력을 내나) | `KeSuspendThread` 본문을 안 뽑았다 (범위 밖) | run 문서 "정적으로 더 볼 것", flow.md 4장 공백 줄. **대기열에 따로 줄은 없다.** 실행 순서는 Q19 (동적) |
| 팻말을 **누가·언제 떼 주나** | `NtCreateUserProcess` 밖이다 | Q5 (정적) |
| 알림 받는 쪽 **실제 함수** (`+0x14d`, `+0x31c`, `+0xbe`, `+0x1cf` 의 대상), `PspNotifyEnableMask`·64칸 목록·BAM/DAM 표 값 | 간접 호출이라 대상이 실행 때 정해진다 | Q39 (동적): call 직전 `ln @rax` |
| `PsTerminateProcess`(`+0x171`), 잡 길(`+0x101`, `+0x8ea`), 보안 스레드 길(`+0x938`)을 **안 타는지** | 조건부 길이고 조건 값이 실행 때 값이다 | Q39 (동적) |
| 프로세스 락(`+0x1c8`)이 정말 **호출자에서 잡혀 들어오나** | `[정적 + 간접]` (두 함수 따로 본 것을 이어 붙임) | Q39 (동적): 입구 `poi(@r9) & 0x20`, 새 EPROCESS `+0x1c8` bit0 |
| `KeStartThread`·`KeReadyThread` 전후 **스레드 상태** | 본문을 안 뽑았고, 상태는 실행 때 값이다 | Q39 (동적): `+0x5dd`, `+0x5a1` 에서 `dt nt!_KTHREAD @rcx State` |
| 프로세스 알림이 **둘째 스레드 때 정말 안 불리나** (`+0x802` 가 한 번만) | 코드로 판정 자리는 봤지만 실행은 안 봤다 | run 문서 4장, flow.md 4.1. **Q39 줄에는 따로 안 적혀 있다** |
| 알림 문턱 값 (`프로세스+0x5fc` bit0, `+0x640`) 과 뜻 `[추측]` | 실행 때 값이고, 뜻은 쓰임새로 짐작한 것 | run 문서 4장, flow.md 4장·4.1. **Q39 줄에는 따로 안 적혀 있다** |
| 스레드 락 풀기 직전 락 값 (`+0x499` 에서 `poi(@rax)`) | 실행 때 값이다 | run 문서 4장, flow.md 4.1. **Q39 줄에는 따로 안 적혀 있다** |
| `KeStartThread` 가 정말 "스케줄링 준비" 인가 `[추측]` | 본문을 안 뽑았다 (이름·지도만) | flow.md 4장 공백 줄. 대기열에 따로 줄은 없다 |
| `스레드+0x5a0` 의 `0x2` 가 "삽입됨" 인가, `info`(0x48)가 `PS_CREATE_NOTIFY_INFO` 인가 `[추측]` | PDB 구조체를 안 봤다 (오프셋으로만) | 대기열에 따로 줄은 없다 |
| `PspCallThreadNotifyRoutines` 본문 `[추측]` | 본문을 안 뽑았다 | 대기열에 따로 줄은 없다 |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run12-insert-thread.md`](run12-insert-thread.md)
- 정적 요청서: [`_workspace/sast/07_insert-thread.md`](../../_workspace/sast/07_insert-thread.md)
- 정적 결과서 (62줄 묶음 표 전체, 보통 길 15단계, "전제", 간접 호출 4곳의 주소·인자, 동적 확인 목록): [`_workspace/sast/07_insert-thread_result.md`](../../_workspace/sast/07_insert-thread_result.md)
- 바퀴 전 관찰: [`runs/01_sprint/`](../01_sprint/) run3 (생성)
- 앞 바퀴 해설:
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — 3.6 멈춤 갈림길, 3.7 오프셋 순서 ≠ 실행 순서
  - [`run11-allocate-thread-해설.md`](run11-allocate-thread-해설.md) — 2.3 "작성 중" 자물쇠, 2.8 상가 명부
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "그림 1 — 부모 cmd.exe 스레드" (회색 상자), 범례의 회색
  - "2.1" 의 `PspMapThreadCreationFlags`, `PspAllocateThread`, `PspInsertThread`, `PspCallProcessNotifyRoutines`, `PspCallThreadNotifyRoutines` 절
  - "2.2" 의 트리, `KeSuspendThread`, `KeReadyThread`, `KeStartThread` 절
  - "4. 공백 목록", "4.1 추측·정적으로 둔 호출 관계"
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q4, Q5, Q19, Q39

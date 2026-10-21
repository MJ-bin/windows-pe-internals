# run11 allocate-thread — 해설

> 이 글은 [run11 문서](run11-allocate-thread.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q3** — 2.1 `nt!PspAllocateThread`, `nt!PspInsertProcess` callee (`_workspace/plan.md`)
  - callee = 이 두 함수가 **직접 부르는 함수 목록**.
- 근거:
  - run 문서 [`run11-allocate-thread.md`](run11-allocate-thread.md) (가장 중요)
  - 요청서 [`_workspace/sast/06_allocate-thread.md`](../../_workspace/sast/06_allocate-thread.md)
  - 결과서 [`_workspace/sast/06_allocate-thread_result.md`](../../_workspace/sast/06_allocate-thread_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다.

이 글은 [run9 해설](run9-create-user-process-해설.md), [run10 해설](run10-allocate-process-해설.md) 다음 편이다.

- 기초 말(커널·유저, 오프셋, call/ret, 콜스택, 태그 …)은 run9 해설 2절에, 주소 공간·PEB·구조체 칸 오프셋은 run10 해설 2절에 있다. 여기선 **링크만** 건다.
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**
  - 주소 공간 = **빈 땅**, 매핑 = **건물 갖다 놓기**, PEB = 가게 문 앞 **안내판**
  - `_EPROCESS` = 관리사무소가 가게마다 두는 **장부**
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**

한 줄로 먼저 말하면:

- run10 에서 가게(test.exe)를 차렸다. 이번엔 그 가게의 **첫 직원을 뽑는** 순서(인사 카드 → 사번 → 작업대 → 개인 안내판 → 첫 출근 쪽지)와,
- 새 가게를 **상가 명부에 올리는** 방법을 코드에서 확인한 바퀴다.
- 덤으로, 01_sprint 에서 본 "모든 스레드가 `PspUserThreadStartup` 에서 출발한다" 의 **코드 쪽 이유**를 찾았다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (유저 쪽): "새 프로그램 띄워 줘"
     └─ 시스템 콜로 커널에 부탁
         └─ nt!NtCreateUserProcess                    (run9, Q1)
              ├─ +0xc36   PspAllocateProcess          (run10, Q2) 새 가게: 땅 → 건물 → 관리실 → 안내판
              ├─ +0xd22   PspCreateUserContext        (run9, 정적)
              ├─ +0xe5c   nt!PspAllocateThread        ← 이번 바퀴 (Q3) 첫 직원 뽑기
              ├─ +0xfc2   nt!PspInsertProcess         ← 이번 바퀴 (Q3) 상가 명부에 올리기
              ├─ +0x101a  PspInsertThread             (Q4)
              └─ …        손잡이(핸들) 건네기 등
 ─ ─ (2.2 공백: 첫 직원은 언제 일을 시작하나?) ─ ─
 test.exe 첫 스레드 출발 (커널) nt!PspUserThreadStartup   ← 여기서 출발하게 되는 "이유"를 이번에 찾음
  └─ ntdll!LdrInitializeThunk  (유저 쪽 첫 자리)
      └─ ntdll 로더: dll 올리기, TLS 콜백, DllMain
          └─ main
```

- 이번 구간에서 일하는 건 여전히 **부모 cmd.exe 의 스레드 하나**다 `[관찰]` (run3).
- run9 는 `NtCreateUserProcess` 의 할 일 목록을, run10 은 그중 `+0xc36` 상자 안을 봤다.
- run11 은 같은 목록의 **다음 두 줄**, `+0xe5c` 와 `+0xfc2` 상자 안을 연 바퀴다.

### 무엇이 궁금했나

1. `PspAllocateThread` 안에서 아래 다섯 가지가 일어나나, **어떤 순서**인가.
   - (가) 스레드 객체 만들기 (나) 스레드 ID (다) 유저 스택 (라) TEB (마) `KeInitThread`
   - (라) 는 points.md 에 `MmCreateTeb` 라는 이름으로 `[확인 필요]` 였다.
2. 첫 스레드가 `PspUserThreadStartup` 에서 출발하게 되는 곳이 **코드에 있나**.
   - 그리고 유저 쪽 시작 주소(`test!mainCRTStartup`)는 **어떤 길로** 들어오나.
3. `PspInsertProcess` 는 새 프로세스를 목록에 **어떻게** 올리나. 핸들도 만드나.

### 바퀴 전에 알던 것

- `[관찰]` (run3) `NtCreateUserProcess +0xe5c` → `PspAllocateThread`, `+0xfc2` → `PspInsertProcess`.
  - `PspAllocateThread` 는 프로세스를 목록에 올리기 **전에** 불린다.
- `[관찰]` (run3) 그 뒤 `PspCallProcessNotifyRoutines` 때 `!process 0 0 test.exe` 로 test.exe 가 **보였다.** → 그 전에 어디선가 목록에 올라갔다는 뜻.
- `[관찰]` (run4·5) test.exe 의 **모든 새 스레드(4개)** 가 `nt!PspUserThreadStartup` 에서 출발한다.
- `[관찰]` (run4·5) 메인 스레드의 Win32 Start Address = `test!mainCRTStartup`.
- `[정적]` (run9) 두 호출 자리는 코드에서도 같다 (call `+0xe57`, `+0xfbd`).
- `[정적]` (run9) `PspAllocateThread` 앞에서 `PspCreateUserContext`(`+0xd22`)를 부른다. 첫 스레드의 유저 시작 문맥 후보.
- `[정적]` (run9) 부모(cmd.exe)가 받는 **프로세스 핸들**은 `NtCreateUserProcess +0x10a7` `PspCreateObjectHandle` 에서 만든다 ([핸들](run9-create-user-process-해설.md#핸들)).
- `[정적]` (요청서, 지도) `PspUserThreadStartup` 은 함수 목록(`_index.tsv`)엔 있는데 **호출 지도에는 없다.** 누가 부르는지 화살표가 안 이어진다.
  - 이게 이번 바퀴의 작은 수수께끼였다. → 2.5 에서 풀린다.
- `[추측]` (run11 전 flow.md 2.1)
  - `PspAllocateThread` = 첫 스레드를 만든다. 아직 일은 시작하지 않는다.
  - `PspInsertProcess` = 새 프로세스를 시스템 프로세스 목록에 올린다.
  - `MmCreateTeb` 는 `PspAllocateThread` 안쪽일 것 (TEB 는 스레드마다 하나라서).

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서의 "호출 관계" 그대로다.

```
NtCreateUserProcess
 ├─ (7) +0xe5c  PspAllocateThread                    [관찰] run3 + [정적]   (번호 = 코드 순서)
 │               ├─ (1) +0x2e7  ObCreateObjectEx       [정적] (가) 스레드 객체
 │               ├─ (2) +0x532  PsAssignThreadId       [정적] (나) 스레드 ID
 │               ├─ (3) +0x5ef  PspSetupUserStack      [정적] (다) 유저 스택   ← 유저 스레드 + 보통 길일 때만
 │               ├─ (4) +0x631  MmCreateTeb            [정적] (라) TEB       ← (3) 성공일 때만
 │               └─ (5) +0x6f9  KeInitThread           [정적] (마) 시작점 = PspUserThreadStartup 주소 (+0x681 LEA)
 └─ (9) +0xfc2  PspInsertProcess                     [관찰] run3 + [정적]
                 ├─ (1) +0xa6   PspLockProcessListExclusive    [정적] 목록 잠금
                 │     (호출 없음: +0xa6 ~ +0x109 에서 직접 ActiveProcessLinks 잇기)
                 ├─ (2) +0x115  PspUnlockProcessListExclusive  [정적] 목록 풀기
                 └─ (3) +0x1e3  ObInsertObjectEx               [정적] 객체 등록 (부모용 핸들 아님)
```

읽을 때 주의 (run10 해설과 같음):

- 오프셋은 모두 **돌아올 자리(ret)** 다. 디버거 콜스택에 찍히는 숫자.
- 들여쓴 줄의 오프셋은 **바로 위 함수 안의 자리**다. `+0x631` 은 `PspAllocateThread+0x631`.
- `(1)` `(2)` … 는 같은 함수 안의 코드 순서다.
- 이번 줄은 run3 에서 본 두 줄(`+0xe5c`, `+0xfc2`) 말고 **전부 `[정적]`** 이다. 노선도에 길이 있다는 것까지다.

### 2.2 한 줄씩 읽는 법

트리 한 줄을 "**누가 / 어디서 / 누구를 / 무엇을 위해**" 로 풀어 읽어 보자.

**`PspAllocateThread` 의 (4) `+0x631` `MmCreateTeb` `[정적]`**

- 누가: `PspAllocateThread` (← `NtCreateUserProcess +0xe5c`)
- 어디서: 돌아올 자리 `+0x631` (부르는 자리 `+0x62c`)
- 누구를: `MmCreateTeb`
  - points.md 의 `[확인 필요]` 이름이 **맞았다** `[정적]`. 함수 목록에 있고(rva `0x9318c4`), 부르는 곳은 지도에서 **이 하나뿐**이다.
- 무엇을 위해: 새 스레드의 **TEB** 를 만든다.
  - **TEB** = Thread Environment Block. 스레드마다 하나씩, 그 프로세스의 **유저 쪽 메모리** 안에 놓이는 정보 묶음 `[추측]` (일반적으로 알려진 설명).
  - 비유: [PEB](run10-allocate-process-해설.md#peb) 가 가게 문 앞 **안내판**이면, TEB 는 직원 한 명 한 명이 차는 **개인 안내판(이름표)**. (flow.md 에선 "개인 서랍", "스레드 이름표" 라고도 불렀다.)
- 조건: 바로 앞 (3) 유저 스택이 **성공했을 때만** 온다 `[정적]` (`+0x5f8` 의 `JS` 로, 실패면 정리 길로).
- 무엇을 주고받나 `[정적]` (결과서 요약): 프로세스, InitialTeb, 스레드의 Cid 자리, TEB 를 돌려받을 칸. 돌려받은 TEB 는 (5) `KeInitThread` 로 넘어간다.
- 디버거로 보면: 콜스택에 `nt!PspAllocateThread+0x631` 로 찍힐 자리다 (아직 안 봤다).

**`PspInsertProcess` 의 (1) `+0xa6` `PspLockProcessListExclusive` `[정적]`**

- 누가: `PspInsertProcess` (← `NtCreateUserProcess +0xfc2`)
- 어디서: 돌아올 자리 `+0xa6` (부르는 자리 `+0xa1`)
- 누구를: `PspLockProcessListExclusive` — 이름 그대로 "프로세스 목록을 **혼자 쓰게 잠근다**".
- 무엇을 위해: 이 잠금 **다음 줄부터** `+0x115` 풀기 전까지, 새 프로세스를 목록에 잇는다. 그런데 그 잇는 일은 **함수 호출이 아니다.** → 2.8 에서 자세히.

### 2.3 첫 직원 뽑기 다섯 걸음

`PspAllocateThread` 안의 (가)~(마) 를 **순서대로 한 줄**로 펴 보면 이렇다. 이번 바퀴에서 제일 중요한 표다.

| 걸음 | 하는 일 | 직원 비유 | 어디서 (ret) | 부르는 함수 | 태그 |
|---|---|---|---|---|---|
| (가) | 스레드 객체(`_ETHREAD`) 만들기 | **인사 카드** 한 장 만들기 | `+0x2e7` | `ObCreateObjectEx` | `[정적]` |
| (나) | 스레드 ID | **사번** 매기기 | `+0x532` | `PsAssignThreadId` | `[정적]` |
| (다) | 유저 스택 | 유저 쪽 **작업대** 마련 | `+0x5ef` | `PspSetupUserStack` | `[정적]` |
| (라) | TEB | **개인 안내판** 걸기 | `+0x631` | `MmCreateTeb` | `[정적]` |
| (마) | 커널 쪽 스레드 초기화 | "첫 출근하면 여기부터" **쪽지** 붙이기 | `+0x6f9` | `KeInitThread` | `[정적]` |

처음 나온 말 한 줄씩 (뜻은 `[추측]` 일반적으로 알려진 설명, 이 프로젝트에서 본 건 따로 태그):

- **`_ETHREAD`** = 커널이 스레드 하나마다 두는 **정보 묶음**. 커널만 본다.
  - 비유: `_EPROCESS` 가 관리사무소의 **가게 장부**였으니, `_ETHREAD` 는 관리사무소의 **직원 인사 카드**다 (flow.md 에선 "사원증").
  - `ObCreateObjectEx` 의 `Ob` = 커널이 "물건(객체)" 을 만들고 세는 쪽 함수라는 머리말. 결과서는 이 호출이 스레드 종류(`PsThreadType`)로 `_ETHREAD` 를 만들고 0으로 채운다고 읽었다 `[정적]`.
- **스레드 ID** = 스레드마다 붙는 번호. 비유: **사번**.
  - `[정적]` (결과서) `_ETHREAD` 안 `Cid` 칸의 스레드 ID 를 채운다.
- **유저 스택** = 스레드가 **유저 쪽에서** 함수를 부를 때 [콜스택](run9-create-user-process-해설.md#콜스택)이 쌓이는 메모리.
  - 비유: 직원이 가게 안에서 일할 때 쓰는 **작업대**. 책갈피 더미를 여기 쌓는다.
- **TEB** = 위 2.2 에서 푼 대로 직원 **개인 안내판**.
- **`KeInitThread`** = 스레드의 **커널 쪽 몸**을 준비하는 함수. 비유: 관리사무소 안에서 이 직원이 **처음 움직일 자리**를 정해 두는 일. → 2.5 에서 이게 핵심이 된다.

순서의 근거 `[정적]`:

- ret 숫자: `+0x2e7` < `+0x532` < `+0x5ef` < `+0x631` < `+0x6f9`.
- 결과서가 디컴파일(`.c`) 흐름과도 맞춰 봤다. 같은 순서다. 이 함수는 **주소 순서 = 실행 순서**로 보인다 (run 문서).

왜 이 순서가 그럴듯한가 (비유, 태그 없음):

- 인사 카드가 있어야 거기에 사번을 적는다.
- 작업대가 있어야, 개인 안내판에 "작업대는 어디" 를 적을 수 있다.
- 다 갖춰져야 "첫 출근하면 여기부터" 쪽지를 붙인다.
- 아직 **출근(실행)은 안 한다** `[추측]` (flow.md 2.1). 첫 직원이 언제 일을 시작하는지는 2.2 공백(Q5)이다.

걸음 사이사이 다른 일도 있다 (run 문서 "하는 일 단위로" 표, 대표만) `[정적]`:

- (가) 전: 크기 정하기 — 인사 카드 뒤에 붙일 덤 공간 계산 (`PoEnergyEstimationEnabled` `+0x228`, `KeQueryMaximumGroupCount` `+0x241`)
- (가)(나) 사이: 칸 채우기(`KeInitializeSemaphore` `+0x432`, 만든 시각 `KeQuerySystemTimePrecise` `+0x4c4`), 그리고 **스레드 락 잡기**(`KeAbPreAcquire` `+0x502`, `ExfAcquirePushLockExclusiveEx` `+0x51c`)
  - 스레드 락 = 인사 카드에 거는 "작성 중" 자물쇠 (비유). 이 자물쇠를 성공 길에서 **푸는 호출이 이 함수 안에 없다** `[정적]` → 2.11.
- (마) 뒤: 마무리(`PsInitializeThreadRngState` `+0x710`, `ObInitializeFastReference` `+0x71f`, `__security_check_cookie` `+0x73c`)

### 2.4 갈림길: 작업대·개인 안내판은 언제 만드나

표의 (다)(라)는 **아무 때나** 오는 게 아니다. 갈림길이 두 겹이다 `[정적]`.

**첫째 갈림길 — 유저 스레드인가** (5번째 인자)

- 5번째 인자 = **유저 CONTEXT** (CONTEXT 는 2.6 에서 푼다).
- 0이 아니면: 유저 스레드 길 → (다)(라)로 간다.
- 0이면: **시스템 스레드** 길 → 작업대·개인 안내판을 **건너뛴다.**
  - 비유: 관리사무소 **안에서만** 일하는 직원. 가게에 나갈 일이 없으니 가게 안 작업대도, 개인 안내판도 필요 없다.

**둘째 갈림길 — 보통 길인가** (11번째 인자, 스택 정보)

- 0이 아니면: **보통 길**. 위 표 순서대로 작업대·안내판을 **새로** 만든다.
- 0이면: 함수의 **먼 쪽 덩어리**로 가서 둘 중 하나.
  - **Pico 길** — Pico 는 Windows 가 Linux 프로그램 같은 다른 종류 프로세스를 돌릴 때 쓰는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - **"지금 스레드를 본뜨는" 길** — 지금 일하는 스레드의 TEB 를 쓰고, 작업대·안내판을 새로 안 만든다 `[정적]` (결과서). 어떤 경우인지는 코드로 못 정했다 `[추측]`.
  - 비유: [run10 의 보통 길과 복제 길](run10-allocate-process-해설.md#보통-길과-복제-길)과 닮았다. 새로 짓느냐, 옆 걸 본뜨느냐.

**그래서 test.exe 첫 스레드는?**

- 근거 1 `[정적]`: `NtCreateUserProcess` 가 11번째 인자로 주는 값은 두 갈래다. `+0xadd` 에서 `rbp+0x720` 이거나, `+0xbb5` 에서 0.
- 근거 2: 어느 쪽인지는 **실행 때 값**이 정한다. 코드만으로는 못 정했다 (run 문서).
- 그래서 → **아직 모른다.** 이번 바퀴엔 짐작도 따로 세우지 않았다. 디버거로 볼 거리다 → Q38 (4절).

**그 밖의 곁가지** `[정적]` (장부에 없음, 디컴파일로 확인 — 2.7)

- 32비트 프로세스([WOW64](run10-allocate-process-해설.md#310-ntdll-쪽-조금-더))면 `PspWow64SetupUserStack`, `PspWow64InitThread` 가 더 불린다.
  - `[추측]` (flow.md) 64비트 test.exe 면 WOW64 쪽은 안 탄다.
- CET 면 유저 스택 다음에 `PspSetupUserShadowStack` 이 불린다.
  - CET 섀도 스택 = 돌아올 주소를 **따로 한 벌 더** 적어 두고 맞춰 보는 CPU 보안 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 책갈피 더미의 **사본**.

### 2.5 `PspUserThreadStartup` 은 "불리는" 게 아니라 "주소가 건네진다"

이번 바퀴에서 제일 반가운 대목이다. 차근차근 가 보자.

**출발점 — 01_sprint 에서 본 것** `[관찰]` (run4·5)

- test.exe 의 새 스레드 4개가 **전부** 커널의 `nt!PspUserThreadStartup` 에서 출발했다.
- 콜스택 맨 아래 쪽 모양도 4개 모두 같았다 (flow.md 2.3).

**수수께끼** `[정적]` (요청서)

- 그런데 정적 호출 지도에는 `PspUserThreadStartup` 으로 가는 **화살표가 없다.**
- 누가 이 함수를 "부르는지" 지도로는 안 보인다.

**코드에서 찾은 것** `[정적]` (`PspAllocateThread`, 결과서·run 문서)

```
+0x681  LEA RAX,[0x140910de0]     ; PspUserThreadStartup 의 주소를 손에 쥔다
+0x688  MOV [RSP+0x128],RAX       ; 그 주소를 "시작 정보 묶음" 의 한 칸에 적는다
+0x6e9  LEA RDX,[RSP+0x120]       ; 묶음 전체를 2번째 인자로 준비한다
```

(오른쪽 `;` 뒤 설명은 이 해설이 붙인 것이다.)

- 그리고 `+0x6f4` 에서 `CALL KeInitThread` (ret `+0x6f9`).
- `0x140910de0` 은 rva `0x910de0` = `PspUserThreadStartup` 이다 (함수 목록과 같음).
- 묶음은 `rsp+0x120` 에서 시작하고, 주소는 `rsp+0x128` 에 적었다. 그래서 묶음의 **`+0x8` 칸**이 `PspUserThreadStartup` 이다.

처음 나온 말:

- **LEA** = "이 자리의 **주소를 계산해서 손에 쥐기만** 하는" 명령. 거기로 가지는 않는다 `[추측]` (일반적으로 알려진 설명).
- **"시작 함수 주소를 넘긴다"** = 함수를 지금 부르는 게 아니라, 나중에 부를 함수의 **주소를 값으로** 남에게 건네는 것.
  - 비유: 전화를 **거는 것**(call)과, 전화번호를 **쪽지에 적어 건네는 것**(주소 넘기기)의 차이.
  - 호출 지도는 "**전화를 건 기록**"(CALL 명령)만 모은다. 쪽지 건네기는 기록에 안 남는다.
  - 그래서 지도에 `PspUserThreadStartup` 화살표가 **없었던 것**이다 (run 문서: "호출이 아니라 주소를 값으로 넘기는 것이라, 호출 지도에는 `PspUserThreadStartup` 이 안 나온다").

**시작 정보 묶음 안의 다른 칸** (결과서 표에서)

| 칸 | 들어가는 값 `[정적]` | 칸 이름 `[추측]` |
|---|---|---|
| `+0x8` | `PspUserThreadStartup` 주소 | SystemRoutine |
| `+0x18` | `_ETHREAD.StartAddress` (2.6) | StartContext |
| `+0x20` | 유저 CONTEXT (5번째 인자) | ContextFrame |
| `+0x28` | `MmCreateTeb` 가 낸 TEB | Teb |
| `+0x30` | 프로세스 | Process |

- 칸 이름은 **알려진 `KeInitThread` 인자 순서에 맞춰 본 것**이라 `[추측]` 이다 ([구조체 안의 칸 오프셋](run10-allocate-process-해설.md#구조체-안의-칸-오프셋)과 같은 사정).
- (라) 걸음의 TEB 가 여기 `+0x28` 칸으로 들어간다. 2.3 표의 다섯 걸음이 **이 묶음 하나로 모인다**고 보면 된다.

**정리 — 관찰과 코드가 맞았다**

- `[관찰]` run4·5 "모든 새 스레드가 `PspUserThreadStartup` 에서 출발" + `[정적]` run11 "`PspAllocateThread +0x681` 에서 그 주소를 `KeInitThread` 에 건넨다".
- `PspAllocateThread` 안에서 이 주소를 쓰는 곳은 `+0x681`, `+0x688` **두 줄뿐**이다 `[정적]` (flow.md 2.1, 결과서).
- 비유: 관리사무소가 새 직원 인사 카드에 "**첫 출근하면 `PspUserThreadStartup` 창구부터 들러라**" 라는 쪽지를 붙여 둔다. 나중에 직원이 처음 움직일 때 그 쪽지대로 간다.

**단, "모든" 은 유저 스레드 얘기다** `[정적]`

- 시스템 스레드 길(5번째 인자 0)은 다른 주소를 건넨다: `PspSystemThreadStartup` (`+0x949` LEA).
- 보안 프로세스 조건이면 `PspSecureThreadStartup` (`+0xb98`).
- 둘 다 장부에 없음, 디컴파일로 확인.
- test.exe 의 4개 스레드는 유저 스레드라서 전부 `PspUserThreadStartup` 이었던 것과 앞뒤가 맞는다.

**아직 확인 아님**

- 실행 때 정말 `+0x8` 칸에 그 주소가 들어가는지는 안 봤다 → Q38.
- `KeInitThread` 가 이 쪽지를 **어떻게 쓰는지**(커널 스택에 시작점으로 심는지)는 `[추측]` → Q6·Q7.

### 2.6 유저 쪽 시작 주소는 CONTEXT 에서 옮겨 적는다

2.5 는 **커널 쪽** 첫 자리였다. 그럼 `test!mainCRTStartup` 같은 **유저 쪽** 시작 주소는?

처음 나온 말:

- **CONTEXT** = CPU 의 작업 칸(레지스터)들을 한 묶음으로 적어 둔 것 `[추측]` (일반적으로 알려진 설명).
  - `Rip` 칸 = "다음에 실행할 자리".
  - `Rcx` 칸 = 64비트 Windows 에서 함수의 **첫 번째 인자**를 담는 칸.
  - 비유: 직원에게 주는 **업무 시작 메모**. "어디서부터 일할지(Rip)", "처음 손에 쥘 것(Rcx)" 이 적혀 있다.

코드에서 본 것 `[정적]` (보통 길, run 문서):

| 자리 | 옮기는 것 |
|---|---|
| `+0x640 ~ +0x647` | `CONTEXT.Rip` → `_ETHREAD.StartAddress` |
| `+0x64e ~ +0x655` | `CONTEXT.Rcx` → `_ETHREAD.Win32StartAddress` |

- 칸 이름은 결과서가 **PDB 타입으로 확인**했다 (`_ETHREAD +0x560 Win32StartAddress`, `_CONTEXT +0x80 Rcx`, `+0xf8 Rip`). 그래서 이 칸 이름들은 2.5 표와 달리 `[정적]` 이다.

그래서 이렇게 이어진다:

1. `[관찰]` (01_sprint) 메인 스레드의 Win32 Start Address = `test!mainCRTStartup`.
2. `[정적]` (run11) Win32 Start Address 칸은 `PspAllocateThread` 가 `CONTEXT.Rcx` 를 옮겨 적은 값이다.
3. 그러니 → 그때 `CONTEXT.Rcx` 에 `test!mainCRTStartup` 이 들어 있었다는 뜻이 된다 (run 문서).

- 이 함수는 **받아서 옮기기만** 한다. CONTEXT 를 **누가 채웠는지**는 이 함수 밖이다 `[정적]`.
  - 후보: run9 에서 본 `NtCreateUserProcess +0xd22` `PspCreateUserContext` `[추측]` → Q7.
- `CONTEXT.Rip` → `StartAddress` 쪽 값은?
  - `[추측]` (run 문서) 유저 첫 위치 쪽 값, 예컨대 `ntdll!RtlUserThreadStart` 일 수 있다. 커널 콜스택의 유저 자리에 그 이름이 보였기 때문이다 (run4).
  - 확인 전엔 모른다 → Q38.

정리하면 "시작 주소" 가 **세 군데** 나온다. 헷갈리기 쉬우니 나란히 두자.

| 무엇 | 값 | 어디서 왔나 | 태그 |
|---|---|---|---|
| 커널 쪽 첫 자리 (`KeInitThread` 묶음 `+0x8`) | `PspUserThreadStartup` | `+0x681` LEA | `[정적]` |
| `_ETHREAD.StartAddress` | `CONTEXT.Rip` 값 | `+0x640 ~ +0x647` | `[정적]` (값이 무엇인지는 `[추측]`) |
| `_ETHREAD.Win32StartAddress` | `CONTEXT.Rcx` 값 = `test!mainCRTStartup` | `+0x64e ~ +0x655` | `[정적]` + `[관찰]` 01_sprint |

- 비유: 쪽지는 두 장이다.
  - 관리사무소 안 쪽지: "첫 출근하면 `PspUserThreadStartup` 창구부터".
  - 가게 안 업무 메모: "가게에선 여기부터(Rip), 처음 손에 쥘 건 이것(Rcx = `mainCRTStartup`)".

### 2.7 장부에 없는 호출 10곳

- `PspAllocateThread` 가 직접 부르는 줄은 장부(`_calls.tsv`)에 **34줄** (서로 다른 이름 30, 조각 안 9줄 포함) `[정적]`.
- 그 밖에 **장부 밖 10곳**이 있다. 장부·`.asm`·지도에는 안 잡혔고, **디컴파일(`.c`)과 결과서 표로 확인**했다 `[정적]`.
  - 결과서 표 기준으로 9곳은 `+0x849 ~ +0xbba` 사이, 1곳(`+0x2ca86a` `ExRaiseDatatypeMisalignment`)은 먼 쪽 덩어리다.
- 이 해설에서 "(장부에 없음, 디컴파일로 확인)" 이라고 붙인 줄이 이것들이다 (2.4 의 WOW64·CET 곁가지).
  - 2.5 의 `PspSystemThreadStartup`(`+0x949`)·`PspSecureThreadStartup`(`+0xb98`)은 호출이 아니라 LEA 줄이지만, 같은 덩어리 안이라 역시 디컴파일로 확인한 것이다.
- 대표 다섯 걸음(2.3)은 **모두 장부 안**에 있다. 그러니 이번 결론의 큰 줄기는 장부로도 확인된다.
- 장부의 `tail` 표시 3줄(`+0x7ea`, `+0x2caccb`, `+0x2cad20`)은 진짜 꼬리호출이 아니다 `[정적]`. 조각으로 갔다가 본체로 돌아오는 점프라서 콜스택에 영향이 없다.
- 간접 호출, 별칭 줄은 없다 `[정적]`.

### 2.8 상가 명부에 올리기 — 호출 없이 직접 한다

이제 `PspInsertProcess` 로 넘어간다. 질문은 "새 프로세스를 목록에 **어떻게** 올리나" 였다.

처음 나온 말:

- **프로세스 목록 (`ActiveProcessLinks`)** = 커널이 살아 있는 프로세스를 **줄줄이 이어 둔 목록** `[추측]` (일반적으로 알려진 설명).
  - 각 `_EPROCESS`(가게 장부)에 "**앞 가게 / 뒤 가게**" 를 가리키는 칸이 있고, 그 칸끼리 손에 손 잡듯 이어진다. 그 칸이 `ActiveProcessLinks` 다.
  - 줄의 맨 앞 머리는 `PsActiveProcessHead` 라는 전역 자리다 `[정적]`.
  - 비유: 관리사무소의 **상가 명부**. 가게마다 "내 앞 가게, 내 뒤 가게" 를 적어 둬서 처음부터 끝까지 따라갈 수 있다.
  - WinDbg 의 `!process 0 0` 은 이 줄을 따라가며 가게 이름을 찍는다고 알려져 있다 `[추측]`.
- **잠금(lock)** = 여러 스레드가 동시에 같은 명부를 고치지 못하게, **한 번에 한 사람만** 쓰게 하는 것.
  - 비유: 명부 서랍 열쇠. 한 사람이 들고 가면 다른 사람은 기다린다.

코드에서 본 것 `[정적]` (run 문서):

1. `+0xa6` (call `+0xa1`) `PspLockProcessListExclusive` — 명부 잠금.
2. 그다음 `+0xa6 ~ +0x109` 에는 **호출이 하나도 없다.** 그 사이 명령들이 직접:
   - 목록 **맨 끝 표시**가 머리(`PsActiveProcessHead`)를 제대로 가리키는지 검사한다.
     - 틀리면 `+0xc5` `INT 0x29` — **빠른 실패**. 명부가 망가졌으면 더 진행하지 않고 그 자리에서 멈춘다 (호출이 아니라 멈춤 명령이다).
   - 새 프로세스의 `ActiveProcessLinks`(EPROCESS `+0x1d8`)를 **맨 뒤에 잇는다** (`+0xe4 ~ +0xf1`).
   - 전역 순번을 1 올려서 EPROCESS `+0x678` 칸에 적는다 (`+0xf8 ~ +0x109`). 비유: "몇 번째로 등록된 가게" 번호.
3. `+0x115` (call `+0x110`) `PspUnlockProcessListExclusive` — 잠금 풀기.

비유로 (태그 없음):

- 직원이 명부 서랍을 **잠그고**,
- "맨 끝 줄이 정말 끝인가?" 확인하고 (아니면 바로 경보),
- 맨 끝 줄에 새 가게 이름을 **손으로 직접 적고**, 등록 번호를 매기고,
- 서랍을 **푼다**.
- "명부에 적기" 담당을 **따로 부르지 않는다.** 그 자리에서 자기가 쓴다.

**이게 왜 중요한가** — "목록에 올리기" 가 함수 이름으로 안 보인다:

- 호출 지도나 콜스택에는 "목록에 넣는 함수" 가 **안 나온다.** 잠금과 풀기 두 함수만 보인다.
- 그래서 디버거로 "목록에 올라간 순간" 을 잡으려면, 함수 이름이 아니라 **오프셋** `PspInsertProcess+0x115` (풀기 직후)에 멈춰야 한다 (run 문서 추천 멈춤점).
- 왜 따로 함수로 부르지 않았나: `[추측]` 목록 잇기는 몇 줄짜리 일이라, 컴파일러가 함수 호출 대신 **그 자리에 펼쳐 넣는** 일이 흔하다 (일반적으로 알려진 설명). 결과서도 그 구간을 "`InsertTailList` 모양" 이라고 불렀다.

**run3 관찰과 맞는다** `[관찰]` run3 + `[정적]` run11

- 목록 올리기는 `NtCreateUserProcess +0xfc2` 안이다.
- 알림(`PspCallProcessNotifyRoutines`)은 그 **뒤** `+0x101a`(`PspInsertThread`) 안이다.
- run3 에서 알림 때 `!process 0 0 test.exe` 로 test.exe 가 보인 건, 그 전에 여기서 명부에 올랐기 때문이다.
- 단, "올리는 **코드가 있다**" 까지다. 실행 때 정말 이 길을 탔는지는 → Q38.

### 2.9 `ObInsertObjectEx` 는 부모에게 줄 손잡이가 아니다

`PspInsertProcess` 안에 `ObInsertObjectEx` (`+0x1e3`, call `+0x1de`)가 있다. 이름만 보면 "핸들 만들기" 같아서 run9 의 핸들과 헷갈리기 쉽다.

- `[정적]` 핸들을 **돌려받는 인자 자리에 0** 을 넣는다 (`+0x1c1`, `+0x1ca`).
  - 돌려받을 칸이 없으니, 부모에게 건넬 [핸들](run9-create-user-process-해설.md#핸들)(손잡이)을 여기서 받아 가는 모양이 아니다.
  - run 문서는 이걸 "**객체를 등록만 하는 모양**" 이라고 적었다.
  - 다만 `ObInsertObjectEx` 의 **인자 이름은 확인 못 했다** `[추측]`.
- `[정적]` (run9) 부모(cmd.exe)가 받는 프로세스 핸들은 `NtCreateUserProcess +0x10a7` `PspCreateObjectHandle` 이다.
- 둘은 **다른 일**이다 `[정적]`.
  - 다만 같은 접근 상태 버퍼(`NtCreateUserProcess` 의 `rbp+0x400`)를 함께 쓴다.

비유 (태그 없음):

- `ObInsertObjectEx` = 관리사무소 **물건 목록에 새 가게를 정식 등록**.
- `PspCreateObjectHandle` = 그 가게의 **손잡이**를 만들어 신청자(cmd.exe)에게 건넴.
- 등록과 손잡이 건네기는 **창구가 다르다.**

### 2.10 감사·잡·디버그 포트 (한 줄씩)

`PspInsertProcess` 의 나머지 일 `[정적]`. 뜻 풀이는 `[추측]` (일반적으로 알려진 설명).

- **감사** (보안 기록 남기기): 켜져 있으면 `SeAuditProcessCreation` (`+0x78`).
- **잡(job)** (프로세스 여러 개를 한 묶음으로 관리하는 틀): 부모가 잡에 있으면 `PspImplicitAssignProcessToJob` (`+0xd9`) 으로 새 프로세스도 같은 잡에 넣는다. **명부 잠금 전**이다.
- **디버그 포트** (디버거에게 소식을 전하는 창구): `DbgkCopyProcessDebugPort` (`+0x143`) 로 부모의 것을 새 프로세스에 복사한다.

실행 순서 한 줄 (run 문서 표):

- 감사 → 잡 상속 → 시스템 콜 제공자 상속 → **명부에 올리기** → 디버그 포트 → 객체 등록 → 끝맺음.
- 주의: 이 함수는 **표 순서가 실행 순서**이고, 숫자 크기 순이 아니다.
  - 예: 잡 `+0xd9` 이 시스템 콜 제공자 `+0x94` 보다 숫자는 크지만 먼저 온다.
  - run9 해설 [3.7](run9-create-user-process-해설.md#37-오프셋이-큰데-먼저-실행되는-줄--오프셋-순서--실행-순서) 의 "오프셋 순서 ≠ 실행 순서" 와 같은 얘기다.
- 장부에 직접 호출 **21줄** (서로 다른 이름 20). 간접·꼬리·별칭·조각 없음 `[정적]`.

### 2.11 그 밖에 확인된 것

- `[정적]` 두 함수를 부르는 곳 (머리말 `callers:`):
  - `PspAllocateThread` ← `NtCreateUserProcess`, `PspCreateThread`, `PspCreatePicoThread`
  - `PspInsertProcess` ← `NtCreateUserProcess`, `PspCreateProcess`, `PsCreateMinimalProcess`
  - 그래서 `PspAllocateThread` 에 멈춤점을 걸면 **다른 스레드 만들 때도** 걸린다. 필터가 필요하다 (run 문서).
- `[추측]` (flow.md 2.6) test.exe 의 나머지 스레드들(`test!Worker` 등)은 `PspCreateThread` → `PspAllocateThread` 길로 만들어질 것이다. caller 에 `PspCreateThread` 가 있다는 것까지가 `[정적]` 이고, 실제 그 길인지는 안 봤다.
- `[정적]` 2.3 의 **스레드 락**(`+0x51c` 에서 잡음)을 성공 길에서 푸는 호출이 `PspAllocateThread` 안에 없다.
  - `[추측]` 호출자 쪽 `PspInsertThread` 에서 푼다 → Q4.
  - 비유: 인사 카드에 "작성 중" 자물쇠를 걸어 둔 채 다음 담당에게 넘긴다. 자물쇠를 여는 건 다음 담당일 것이다.
- `[정적]` 함수 몸통 일부가 **멀리 떨어진 곳**에 있다 (오프셋 `+0x2ca…` 줄들, 자주 안 가는 길 모음). 예외표(.pdata)로 같은 함수임을 확인했다.
  - `[추측]` WinDbg 는 그 줄을 `PspAllocateThread+0x2ca…` 가 아니라 **엉뚱한 이름 + 큰 오프셋**으로 보여 줄 수 있다 (그 구간에 공개 심볼이 없음). 그래서 flow.md 그림 라벨로는 안 썼다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만 적는다.

| 자리 | 전 (run10 뒤) | 후 (run11) |
|---|---|---|
| 기준 | 정적 근거 run9 ~ run10 | 정적 근거 **run9 ~ run11** |
| 읽는 법 (라벨) | "call 이 있는 자리(돌아올 주소의 오프셋)" 로 두 말이 한 줄에 섞여 있었음 | 라벨 숫자 = **돌아올 자리(ret)** 라고 분명히 적음. call 자리는 "(call `+0x…`)" 로 따로 |
| 그림 1 | `PspAllocateThread`, `PspInsertProcess` 아래 화살표 없음 | **정적 상자 7개 추가**: `PspAllocateThread` 아래 (1)~(5) (`ObCreateObjectEx`, `PsAssignThreadId`, `PspSetupUserStack`, `MmCreateTeb`, `KeInitThread`), `PspInsertProcess` 아래 (1)(2) (`PspLockProcessListExclusive`, `ObInsertObjectEx`) |
| 2.1 트리 | 두 함수 한 줄씩 | 아래에 7줄 (위 2.1 트리와 같음) |
| 2.1 `PspAllocateThread` 절 | `[추측]` 첫 스레드를 만든다 | `[정적]` 34줄 + 장부 밖 10곳, 다섯 걸음 순서, 갈림길(유저/시스템, 11번째 인자), CONTEXT → `_ETHREAD` 옮겨 적기, 스레드 락, 먼 덩어리. 비유("책상 차리기")는 `[추측]` |
| 2.1 새 절 | — | `MmCreateTeb` (정적), `KeInitThread` (정적, 시작 정보 묶음 `+0x8` = `PspUserThreadStartup`) |
| 2.1 `PspInsertProcess` 절 | `[추측]` 목록에 올린다 + `[관찰]` 참고 한 줄 | **`[정적]`** 목록에 올린다 (잠금 `+0xa6` ~ 풀기 `+0x115` 사이 직접 잇기). `ObInsertObjectEx` = 객체 등록, 부모용 핸들 아님. 감사·잡·디버그 포트 |
| 2.1 "아직 어디서 하는지 모르는 일" | 남은 것 `MiCreateImageFileMap`, `MmCreateTeb` | `MmCreateTeb` **해결** (`PspAllocateThread +0x631`). 남은 것 `MiCreateImageFileMap` 하나 |
| 2.3 `PspUserThreadStartup` | `[관찰]` 모두 여기서 출발 | `[정적]` 근거 추가: `PspAllocateThread +0x681` 에서 주소를 `KeInitThread` 로. Win32StartAddress ← `CONTEXT.Rcx` |
| 2.3 커널 → 유저 전환 공백 | `[추측]` 한 줄 | `[추측]` 한 줄 더: `CONTEXT.Rip` → `StartAddress` 가 `RtlUserThreadStart` 쪽 값일 수 있다 |
| 2.6 다른 스레드 | — | `[추측]` `PspCreateThread` → `PspAllocateThread` 길 |
| 2.10 | `[관찰]` 시작 주소 = `mainCRTStartup` | `[정적]` 그 값은 `CONTEXT.Rcx` 를 옮긴 것. `[추측]` 그래서 `mainCRTStartup` 주소는 `RtlUserThreadStart` 의 첫 인자 자리(`rcx`)로 간다 |
| 4장 공백 표 | — | 해결 2줄, 새 줄 7개 (어느 길을 탔나, 먼 덩어리 표기, 스레드 락, `MmCreateTeb` 본문, `ObInsertObjectEx` 인자, CONTEXT 를 누가 채우나, `KeInitThread` 가 주소를 어떻게 쓰나) |
| 4.1 | — | 5줄 추가 (각각 디버거로 무엇을 보면 되는지) |
| 5장 | run10 까지 | run11 줄, 요청서 번호 표에 run11 줄 |

태그로 보면 핵심 변화는 두 가지다:

- `PspAllocateThread` 역할: `[추측]` → **`[정적]`** (다섯 걸음).
- `PspInsertProcess` 역할: `[추측]` → **`[정적]`** (명부에 직접 잇기).

### 큰 그림에서 어떻게 이어지나

- **가게 다음에 첫 직원.** run10 이 가게(땅·건물·안내판)를 봤고, run11 이 첫 직원(인사 카드·사번·작업대·개인 안내판·출근 쪽지)과 명부 등록을 봤다. `NtCreateUserProcess` 의 큰 줄기 중 `+0xc36` ~ `+0xfc2` 가 코드로 채워졌다.
- **그림 1 과 그림 2 사이에 실이 하나 더 생겼다.**
  - 그림 1(부모 cmd.exe 스레드)의 `PspAllocateThread +0x681` 이 건넨 주소가,
  - 그림 2 쪽 시작(flow.md 2.3) `PspUserThreadStartup` 이 된다.
  - run10 의 실(PEB `+0x10`: 커널이 쓰고 ntdll 이 읽음)에 이어, 커널이 **나중 일을 미리 정해 두는** 두 번째 실이다.
- **`mainCRTStartup` 이 어디서 오는지 반쯤 보였다.**
  - 01_sprint 에서 본 값이 `CONTEXT.Rcx` 를 옮긴 것이라는 데까지 `[정적]`.
  - 남은 절반(누가 CONTEXT 를 채우나)이 Q7 이다.
- **다음 구간:**
  - Q4 — `PspInsertThread`, `PspCallProcessNotifyRoutines` 안쪽. 이번에 남긴 "스레드 락은 누가 푸나" 를 같이 본다.
  - Q38 — 이번 바퀴의 동적 확인 (어느 길을 탔나, 묶음 실제 값, 명부에 올라간 순간).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 첫 스레드가 `PspAllocateThread` 의 **보통 길 / Pico 길 / 본뜨기 길** 중 무엇을 탔나 | 11번째 인자 값이 정한다. `NtCreateUserProcess` 가 `+0xadd`(`rbp+0x720`)·`+0xbb5`(0) 중 무엇을 주는지는 실행 때 값이다 | Q38 (동적): 입구에서 `poi(@rsp+0x58)` 이 0인지. 먼 덩어리 입구 `nt+0xb0d315` 에 걸어서 안 걸리면 보통 길 |
| `KeInitThread` 시작 정보 묶음의 **실제 값** (`+0x8` 이 정말 `PspUserThreadStartup` 인지, `+0x18` 이 무엇인지) | 코드에 넣는 줄은 봤지만 실행 때 값은 안 봤다 | Q38 (동적): `PspAllocateThread+0x6f4` 에서 `dqs @rdx l0n0xc` |
| Win32StartAddress 가 정말 `CONTEXT.Rcx` 와 같은지, `CONTEXT.Rip` 는 무엇인지. `[추측]` `RtlUserThreadStart` 쪽 | 실행 때 값이다 | run 문서 4장, flow.md 4.1 (입구 `dt nt!_CONTEXT poi(@rsp+0x28) Rip Rcx`, `+0x6f9` 에서 `dt nt!_ETHREAD @rsi StartAddress Win32StartAddress`). **Q38 줄에는 따로 안 적혀 있다** |
| 명부에 정말 올라갔나 (`PspInsertProcess+0x115` 에서 test.exe 가 보이나), `+0x1e3` 앞뒤로 부모 핸들 표가 바뀌나 | 코드가 있다는 것까지만 봤다 | Q38 (동적): `+0x115` 에서 `!process 0 0 test.exe`. 핸들 표 비교(`!handle 0 3 <부모>`)는 run 문서 4장·flow.md 4.1 에 있고, **Q38 줄에는 안 적혀 있다** |
| 먼 덩어리(`+0x2ca…`)를 WinDbg 가 **무슨 이름**으로 보이나 | 그 구간에 공개 심볼이 없다. 실제 표기는 디버거로만 안다 | Q38 (동적): `ln nt+0xb0d383`, `ln nt+0xb0d315` |
| 보통 길이면 `PspSetupUserStack`(`+0x5ef`)·`MmCreateTeb`(`+0x631`) 반환값, TEB 주소, 그게 묶음 `+0x28` 칸과 같은지 | 실행 때 값이다 | run 문서 4장, flow.md 4.1. Q38 줄에는 따로 안 적혀 있다 |
| 스레드 락(`+0x51c`)을 성공 길에서 **누가 푸나**. `[추측]` `PspInsertThread` | 이 함수 밖이다 | Q4 (정적) |
| CONTEXT 의 `Rip`·`Rcx` 를 **누가 어떤 값으로 채우나**. 후보 `PspCreateUserContext` `[추측]` | `PspAllocateThread` 는 받아서 옮기기만 한다 | Q7 (정적) |
| `KeInitThread` 가 `PspUserThreadStartup` 주소를 **어떻게 쓰나**. `[추측]` 커널 스택에 시작점으로 심음 | `KeInitThread` 본문은 안 뽑았다 (요청서 범위 밖) | Q6·Q7 (정적) |
| `MmCreateTeb` 본문: TEB 에 무엇을 쓰나 `[공백]` | 본문은 안 뽑았다 | flow.md 4장 공백 줄. 대기열에 따로 줄은 없다 |
| `ObInsertObjectEx` 인자 이름·하는 일 (핸들 안 돌려받는 모양까지만 봄) | 이 함수 본문은 안 뽑았다 | flow.md 4장 공백 줄. 대기열에 따로 줄은 없다 |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run11-allocate-thread.md`](run11-allocate-thread.md)
- 정적 요청서: [`_workspace/sast/06_allocate-thread.md`](../../_workspace/sast/06_allocate-thread.md)
- 정적 결과서 (시작 정보 묶음 칸 전체, 세 갈래 길 조건, 먼 덩어리·.pdata, 장부 밖 10곳 표): [`_workspace/sast/06_allocate-thread_result.md`](../../_workspace/sast/06_allocate-thread_result.md)
- 바퀴 전 관찰: [`runs/01_sprint/`](../01_sprint/) run3 (생성), run4·run5 (스레드 출발)
- 앞 바퀴 해설: [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md), [`run10-allocate-process-해설.md`](run10-allocate-process-해설.md)
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "그림 1 — 부모 cmd.exe 스레드"
  - "2.1" 의 `PspAllocateThread`, `MmCreateTeb`, `KeInitThread`, `PspInsertProcess` 절
  - "2.3" 의 `PspUserThreadStartup` 절, "커널 → 유저 전환" 공백
  - "2.10", "4. 공백 목록", "4.1 추측·정적으로 둔 호출 관계"
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q3, Q4, Q6, Q7, Q38

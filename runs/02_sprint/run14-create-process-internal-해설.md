# run14 create-process-internal — 해설

> 이 글은 [run14 문서](run14-create-process-internal.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q5b** — 2.2 부모 쪽에서 첫 스레드를 풀어 주는 곳: `KERNELBASE!CreateProcessInternalW` 가 `NtCreateUserProcess` 뒤에 `NtResumeThread` 를 부르나 (`_workspace/plan.md`)
  - run13 바퀴 시작 때 Q5 를 **Q5a(nt) / Q5b(KERNELBASE)** 로 나눴다. Q5a 는 run13 에서 끝났고, 이번이 남은 반쪽이다.
- 근거:
  - run 문서 [`run14-create-process-internal.md`](run14-create-process-internal.md) (가장 중요)
  - 요청서 [`_workspace/sast/09_create-process-internal.md`](../../_workspace/sast/09_create-process-internal.md)
  - 결과서 [`_workspace/sast/09_create-process-internal_result.md`](../../_workspace/sast/09_create-process-internal_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q5b, Q19, Q41
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다.
- 이번엔 처음으로 **커널(ntoskrnl)이 아닌 모듈**, `KernelBase.dll` 을 열었다.

이 글은 [run9](run9-create-user-process-해설.md) · [run10](run10-allocate-process-해설.md) · [run11](run11-allocate-thread-해설.md) · [run12](run12-insert-thread-해설.md) · [run13](run13-resume-thread-해설.md) 해설 다음 편이다.

- 기초 말(오프셋, call/ret, 콜스택, 태그, 간접 호출, 비트와 플래그, 핸들 …)은 run9 해설 2절에 있다. 여기선 **링크만** 건다.
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 시스템 콜 = 관리사무소의 **신청 창구**
  - 프로세스 = **가게**, 스레드 = **직원**. 부모 cmd.exe = 새 가게를 **신청하는 쪽 가게**
  - 멈춘 채 만들기 = 목에 건 **"대기" 팻말**, `KeReadyThread` = **출근 대기 줄** (run9, run12)
  - 멈춤 APC = 출근하자마자 **펼쳐 볼 쪽지**, `SuspendEvent` = 직원 앞 **신호등**, `KiTryUnwaitThread` = **어깨 두드려 깨우기** (run13)
  - 핸들 = **손잡이**
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**

한 줄로 먼저 말하면:

- run13 까지 커널 쪽 장치는 다 봤다. **"쪽지 넣고 빨간불"**(멈춰 두기)과 **"초록불 + 어깨 두드리기"**(풀어 주기).
- 남은 질문은 "그 두 장치를 **누가, 언제** 누르나" 였다.
- 이번에 부모 쪽 신청서 작성 코드(`KERNELBASE!CreateProcessInternalW`)를 열어 보니 답이 코드에 있었다 `[정적]`.
  1. 커널에 **늘** "멈춘 채로 만들어 줘" 를 보낸다 (`ThreadFlags` = 1).
  2. 커널이 돌아오면 부모가 할 일을 한다. 대표가 csrss 알림(`+0x4672`).
  3. 마지막에 호출자가 "멈춘 채 둬"(`CREATE_SUSPENDED`)를 안 했으면 **직접** `NtResumeThread`(`+0x5bd3`)로 풀어 준다.
- 그래서 2.2 "첫 스레드는 언제 풀리나" 가 **코드로는 처음부터 끝까지 이어졌다.** 실행으로는 아직 하나도 안 봤다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (유저 쪽): "새 프로그램 띄워 줘"
     └─ KERNELBASE!CreateProcessW → CreateProcessInternalW        ← 이번 바퀴 (처음 엶)
          ├─ 시스템 콜 ① NtCreateUserProcess                       ← 이번: 부르는 자리, 넘기는 값
          │    └─ (커널) … PspInsertThread +0x2e5 KeSuspendThread   (run9 ~ run13) 쪽지 + 빨간불
          ├─ (부모가 할 일: 검사, csrss 알림, 후처리)               ← 이번
          └─ 시스템 콜 ② NtResumeThread                            ← 이번: 부르는 자리, 언제 건너뛰나
               └─ (커널) PsMultiResumeThread → … → KiTryUnwaitThread  (run13) 초록불 + 어깨
 ─ ─ ─
 test.exe 첫 스레드 (커널)
  └─ KiStartUserThread → PspUserThreadStartup → (멈춤 쪽지) 빨간불 앞에서 잠듦  (run13)
  └─ 깨어난 뒤 → ntdll!LdrInitializeThunk → 로더 → main
```

- run9 ~ run13 은 **관리사무소 안**(커널)만 봤다.
- 이번엔 **신청 창구 바깥**, 부모 가게(cmd.exe) 안에서 신청서를 쓰고 창구에 내는 쪽을 봤다.
- 새로 나온 말 하나:
  - **KERNELBASE** (`KernelBase.dll`) = 유저 프로그램이 부르는 Windows API(`CreateProcessW` 같은 것)의 본체가 들어 있는 DLL 로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 모든 가게에 하나씩 비치된 **"신청서 대행 책상"**. 가게 직원이 "새 가게 내고 싶어요" 하면, 이 책상이 신청서를 대신 채워서 창구(시스템 콜)에 낸다.

### 무엇이 궁금했나

run 문서 "목적" 그대로, 네 가지다.

1. `CreateProcessInternalW` 가 `NtCreateUserProcess` 를 부르는 자리는 **몇 곳**인가.
2. 그 뒤에 첫 스레드를 **풀어 주는 호출이 있나.** `NtResumeThread` 인가 `NtAlertResumeThread` 인가. **언제 건너뛰나** (`CREATE_SUSPENDED`).
3. 커널에 넘기는 `ThreadFlags`(8번째 인자) 의 bit0 를 켜나. 켜면 커널 `PspInsertThread +0x2e5` 에서 `KeSuspendThread` 다 (run12).
4. `CreateProcessW` 에서 와도 같은가.

### 바퀴 전에 알던 것

- `[관찰]` (01_sprint run3) cmd.exe 스레드가 `nt!KiSystemServiceCopyEnd+0x28` 에서 `nt!NtCreateUserProcess` 로 들어왔다. **유저 쪽 콜스택은 안 찍었다.**
- `[정적]` (run9) `NtCreateUserProcess` 안에는 첫 스레드를 **풀어 주는 호출이 없다**.
- `[정적]` (run9, run12) `ThreadFlags` bit0 → `PspMapThreadCreationFlags` 가 flags bit0 로 그대로 옮김 → `PspInsertThread +0x2e5` `KeSuspendThread` ([run12 해설 2.5](run12-insert-thread-해설.md#25-flags-는-어디서-오나--전제)).
- `[정적]` (run13) 멈춰 두기 = 횟수 + 쪽지(멈춤 APC) + 빨간불(`SuspendEvent` 0). 풀어 주기 = `NtResumeThread +0x9b` → `PsMultiResumeThread` → `KeResumeThread` → `KiResumeThread` → `KiTryUnwaitThread` ([run13 해설 2.3](run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등), [2.7](run13-resume-thread-해설.md#27-풀어-주는-사슬--그리고-끼지-않는-psresumethread)).
- `[추측]` (run9 해설부터 쭉) "부모 유저 쪽(`KERNELBASE!CreateProcessInternalW` 등)이 시스템 콜로 따로 풀어 준다" ([run9 해설 "스레드를 멈춘 채로 만들기"](run9-create-user-process-해설.md#스레드를-멈춘-채로-만들기)).
- `[공백]` 그걸 **정말** 부르는지, 어느 시스템 콜인지, 언제인지.
- `[추측]` (요청서) cmd.exe 는 보통 `CREATE_SUSPENDED` 없이 `CreateProcessW` 를 부른다. cmd.exe 자체는 분석 범위 밖이다.

---

## 2. 찾은 것

### 2.1 한눈에 — 부모 쪽 세 자리

run 문서 "호출 관계" 를 줄였다. 오프셋은 모두 **돌아올 자리(ret)** 다 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)).

```
KERNELBASE!CreateProcessW                         (부모가 이 입구로 오는지는 [추측])
 └─ +0x66  CreateProcessInternalW                 [정적] dwCreationFlags 를 바꾸지 않고 넘김
            │  +0xc7   dwCreationFlags 사본 → [RSP+0x108]
            │  +0x6cb  ESI = 1 (함수 끝까지 그대로)
            ├─ (1) +0x30ee  NtCreateUserProcess   [정적] 한 곳. ThreadFlags(8번째) = 1 (+0x30af)
            │               ══▶ 커널: … KeSuspendThread → 쪽지 + 빨간불 (run9 ~ run13)
            │    ① 검사 (조건부)
            ├─ (2) +0x4672  CsrClientCallServer   [정적] csrss 알림 (보안 프로세스가 아니면 늘 지남)
            │    ③ 상승·잡·앱 호환·패키지 후처리 (조건부)
            │  +0x5bb8  사본 & 4 (CREATE_SUSPENDED) ?  켜짐 → +0x5bc0 JNZ 로 건너뜀
            └─ (3) +0x5bd3  NtResumeThread(스레드 핸들, NULL)  [정적] 한 곳
                            ══▶ 커널: +0x9b PsMultiResumeThread … → 초록불 + 어깨 (run13)
```

읽을 때 주의:

- 번호 `(1)(2)(3)` 은 결과서가 **점프를 따라 그린 흐름**으로 본 순서다 (→ 2.6). 실행 때 정말 이 길로 갔는지는 안 봤다.
- `══▶` 는 시스템 콜(창구에 신청서 내기)이다. 중간의 ntdll 쪽 한 칸은 안 봤다 (→ 2.3).
- 오른쪽 `[정적]` 은 **그 호출이 코드에 있다**는 것까지다.

### 2.2 한 줄씩 읽는 법

트리 한 줄을 "**누가 / 어디서 / 누구를 / 무엇을 위해**" 로 풀어 읽어 보자. 이번 바퀴의 양 끝 두 줄이다.

**(1) `CreateProcessInternalW +0x30ee` → `NtCreateUserProcess` `[정적]`**

- 누가: `KERNELBASE!CreateProcessInternalW`. 부모 cmd.exe 스레드가 돌리는 유저 쪽 코드다.
- 어디서: 부르는 자리 `+0x30e7`, 돌아올 자리 `+0x30ee`.
  - 이 함수의 ntdll 호출은 7바이트짜리 call 이라 ret = call + 7 이다 (run 문서 "오프셋 읽는 법").
- 누구를: `NtCreateUserProcess` (ntdll 에서 가져온 함수, → 2.3).
  - 넘기는 것 중 핵심: **8번째 인자 `ThreadFlags` = 1** (`+0x30af`) (→ 2.4).
  - 받는 것: 프로세스 핸들 칸, **스레드 핸들 칸**(`[RSP+0x1b8]`). 이 스레드 핸들을 나중에 (3) 에서 쓴다.
- 무엇을 위해: 커널에 "새 가게(test.exe) 내 주세요, **첫 직원은 대기 팻말 걸어서**" 를 신청한다.
- 디버거로 보면: 커널 `nt!NtCreateUserProcess` 에 멈춰 긴 `k` 를 찍으면 유저 쪽에 `KERNELBASE!CreateProcessInternalW+0x30ee` 가 보일 것이다 (run 문서 추천 멈춤점, 아직 안 봄 → Q19).

**(3) `CreateProcessInternalW +0x5bd3` → `NtResumeThread` `[정적]`**

- 누가: 같은 `CreateProcessInternalW`. 같은 부모 스레드다.
- 어디서: 부르는 자리 `+0x5bcc`, 돌아올 자리 `+0x5bd3`.
- 누구를: `NtResumeThread(스레드 핸들, NULL)`.
  - 1번째 = `[RSP+0x1b8]` = (1) 에서 **받아 둔 첫 스레드 손잡이**.
  - 2번째 = 0 (`+0x5bc2 XOR EDX,EDX`). "이전 멈춤 횟수를 돌려받을 칸" 이 필요 없다는 뜻이다.
- 무엇을 위해: 첫 직원의 대기 팻말을 한 장 뗀다. 커널에선 run13 의 사슬로 이어져 신호등이 초록이 되고 어깨를 두드린다.
- 조건: 바로 앞 `+0x5bb8` / `+0x5bc0` 에서 호출자가 `CREATE_SUSPENDED` 를 줬으면 **이 줄을 건너뛴다** (→ 2.8).

### 2.3 KERNELBASE 를 처음 열었다 — 어떤 함수들인가

**`CreateProcessW` 는 얇은 껍데기** `[정적]`

- 107바이트. `+0x61` 에서 `CreateProcessInternalW` 를 한 번 부른다 (ret **`+0x66`**).
- 넘기는 것: 1번째 인자(토큰) = 0, 마지막 인자 = 0, 나머지는 **한 칸씩 밀어서 그대로**.
- `dwCreationFlags` 도 `+0x3c` → `+0x43` 으로 **값을 안 바꾸고** 복사만 한다.
- 비유: 대행 책상의 접수원. 손님 서류를 그대로 안쪽 담당자에게 넘기고, 빈칸 두 개만 0 으로 채운다.
- 그래서 어느 입구로 왔든 이번에 본 세 자리와 `ThreadFlags` = 1 은 **같은 함수 안의 같은 줄**이다. 달라지는 건 `dwCreationFlags` 값뿐이다 (run 문서).
  - `CreateProcessInternalW` 를 부르는 곳은 지도 기준 5곳이다. `CreateProcessW` 말고 4곳은 이번에 안 봤다.
  - `[추측]` cmd.exe 가 이 입구로 오는지(사이에 `kernel32!CreateProcessW` 가 끼는지)는 안 봤다 `[공백]` → Q41, Q19.

**`CreateProcessInternalW` 는 아주 큰 함수** `[정적]`

- **28087바이트.** 직접 호출 자리 **357곳** (`_calls.tsv`). 거의 다 잡일(메모리 해제, 핸들 닫기, 에러 번호, ETW, 기능 켜짐 검사)이다.
- 함수 포인터 [간접 호출](run9-create-user-process-해설.md#직접-호출과-간접-호출)은 **하나도 없다.** run9 의 `NtCreateUserProcess` 처럼 "실행해 봐야 대상을 아는" 호출이 이 함수 자신엔 없다는 뜻이다.
- 그래서 357곳 중 **대표 세 자리**만 골랐다: `+0x30ee`, `+0x4672`, `+0x5bd3`.

**새 말: import (가져온 함수)**

- 뜻: 다른 DLL 에 있는 함수를 부를 때, 그 주소를 적어 두는 **칸**(가져온 함수 칸)을 거쳐 부르는 것. `.asm` 에선 `CALL qword ptr [그 칸 주소]` 모양이다.
  - 비유: 가게 벽에 붙은 **단축번호표**. "NtCreateUserProcess = 몇 번 칸" 이라고 적어 두고, 그 칸에 적힌 번호로 전화를 건다.
- 이 바퀴에서:
  - `NtCreateUserProcess`, `NtResumeThread`, `CsrClientCallServer` 는 KERNELBASE 장부(`_index.tsv`)엔 **없고**, 지도에서 `external` 로 나온다. 이름은 import 이름 그대로 `.asm` 에 찍혀 있다 `[정적]`.
  - 어느 DLL 의 칸인지: `NtCreateUserProcess`, `NtResumeThread`, `CsrClientCallServer` **셋 다 ntdll.dll 에서 가져온다** `[정적]` (KernelBase import 표로 확인, 오케스트레이터가 `llvm-readobj-14 --coff-imports` 로 읽음).
  - ntdll 의 `NtCreateUserProcess`·`NtResumeThread` 가 받자마자 곧장 시스템 콜을 하는 "창구 앞 대리인"(스텁) 인지는 `[추측]` 이다 (run 문서 "정적으로 더 볼 것").
- 그래서 flow.md 그림 1 의 ntdll 상자는 "import 로 부름, 안쪽은 안 봄" 이라고 붙어 있다.

**새 말: SEH 정리 함수 — "한 곳뿐" 을 말하려고 넓힌 범위**

- 뜻: 함수가 예외나 실패로 중간에 빠져나갈 때 **뒷정리하는 코드 조각**. 이름이 `CreateProcessInternalW$fin$0`, `$filt$1`, `$fin$2` 처럼 붙는다.
  - 비유: 가게의 **비상구 뒷정리 담당**. 평소 업무 순서(호출 관계)엔 안 나오고, 비상구로 나갈 때만 일한다.
- 왜 봤나: 이 조각들은 호출로는 닿지 않아서 지도·`.asm` 에 **안 들어온다** (결과서 "스크립트 문제"). 그런데 "`NtResumeThread` 를 부르는 곳이 **한 곳뿐**" 이라고 말하려면 여기까지 봐야 한다.
- 결과 `[정적]`: 세 조각엔 핸들 닫기·메모리 해제·프로세스 끝내기 같은 **정리 호출뿐**이었다. `NtCreateUserProcess`·`NtResumeThread`·`ResumeThread`·`NtAlertResumeThread` 는 없다.

### 2.4 (1) "멈춘 채로 만들어 줘" 를 **늘** 보낸다 — `ThreadFlags` = 1

이번 바퀴의 첫 번째 큰 이야기다. 천천히 가 보자.

**어디에 무엇을 넣나** `[정적]`

- 결과서가 읽은 `NtCreateUserProcess` 인자 자리는 11번째(`[RSP+0x50]`, 속성 목록)까지다. 앞 넷은 레지스터(RCX, RDX, R8, R9)로, **5번째부터는 스택 칸**으로 넘긴다.
- 결과서가 읽은 자리: `[RSP+0x20]` 이 5번째다. 그러니 한 칸(8바이트)씩 가서
  - `[RSP+0x28]` = 6번째, `[RSP+0x30]` = 7번째 (ProcessFlags), **`[RSP+0x38]` = 8번째 (`ThreadFlags`)**.
- 비유: 신청서의 **8번째 칸**. 그중 첫 체크 칸(bit0)이 "첫 직원 대기 팻말 걸어 주세요" 다 ([비트와 플래그](run9-create-user-process-해설.md#비트와-플래그)).

**그 칸에 들어가는 값** — 한 걸음씩 `[정적]`

```
+0x699   XOR EAX,EAX          ; RAX = 0
+0x6cb   LEA ESI,[RAX+1]      ; ESI = 0 + 1 = 1
```

1. `+0x699` 에서 RAX 를 0 으로 만든다. `+0x6cb` 까지 RAX 를 바꾸는 줄이 없다.
2. `+0x6cb` 에서 **ESI = 1**. (ESI = CPU 안의 메모 칸 하나.)
3. 그 뒤 함수 맨 끝 `POP RSI`(`+0x6e0c`) 전까지 **ESI 를 바꾸는 줄이 없다.** `.asm` 전체에서 이 칸에 쓰는 줄은 `+0x3e`, `+0x6cb`, 끝 `POP RSI` 셋뿐이다 (결과서).
4. `+0x30af` `MOV [RSP+0x38],ESI` — 8번째 칸에 그 1 을 넣는다. **조건 없이.**
5. 이 줄 근처 어디에도 `dwCreationFlags`(사본 `[RSP+0x108]`)를 읽어서 이 값을 바꾸는 줄이 없다.

- 덤으로 받쳐 주는 것: 같은 ESI 가 "TRUE 를 돌려줄 때"(`MOV EAX,ESI`)나 다른 함수에 "참" 을 넘길 때도 쓰인다. 상수 1 이라는 읽기와 맞다 (결과서).
- 결과서는 "늘 1" 을 눈으로만 본 게 아니라, 점프를 따라 흐름 그래프를 그려서 **`+0x30e7` 에 닿는 ESI 값은 `+0x6cb` 하나뿐**임을 계산으로도 확인했다 (→ 2.6 에서 이 방법을 푼다).

**그래서** `[정적]`

- 호출자가 `CREATE_SUSPENDED` 를 줬든 안 줬든, 커널에는 **똑같이 `ThreadFlags` = 1** 이 간다.
- 커널 쪽은 run12·run13 대로 이어진다: `PspMapThreadCreationFlags`(bit0 → bit0) → `PspInsertThread +0x2e5` `KeSuspendThread`. 즉 **첫 스레드는 늘 멈춘 채로 만들어진다** (KERNELBASE 길로 왔다면).
- 비유: 대행 책상은 손님이 뭐라고 하든 신청서 8번째 칸의 "대기 팻말" 체크를 **항상 해서** 낸다.

**새 말: `dwCreationFlags` 와 `CREATE_SUSPENDED`**

- `dwCreationFlags` = 프로그램이 `CreateProcess` 를 부를 때 주는 **"만드는 방식" 체크 칸 묶음**. `CreateProcessInternalW` 의 7번째 인자다 (`+0x47` 에서 읽음).
  - 주의: 이건 **`CreateProcessInternalW` 가 받는 칸**이다. 위의 `ThreadFlags` 는 **커널에 내는 칸**이다. 둘은 다른 서류다.
  - 비유: 손님이 대행 책상에 써 낸 **주문서**(`dwCreationFlags`) ↔ 책상이 창구에 내는 **공식 신청서**(`ThreadFlags` 등).
- `CREATE_SUSPENDED` = 그 주문서의 값 **4** 칸. "첫 직원을 대기 팻말 단 채로 **둬 달라**(내가 나중에 풀게)" 는 주문이다. 4 라는 값은 Win32 공개 상수다 (run 문서).
- `[RSP+0x108]` 이 정말 이 주문서라는 근거 `[정적]`: 같은 사본의 `0x3`(디버그 플래그 1·2)을 보고 `DbgUiConnectToDbg`(`+0x81a`)를 부른다. 디버그 관련 주문 칸이 거기 있다는 것과 맞는다.

**한 가지 덧붙임 — 주문서의 4 가 커널에 아예 안 가는 건 아니다** `[정적]`

- `+0x7f0 ~ +0x800`: 사본에 4 가 **꺼져 있으면** 커널에 넘길 다른 칸 ProcessFlags(7번째 인자)의 bit9(`0x200`)를 켠다.
- 그러니 정확히는 "`CREATE_SUSPENDED` 가 **`ThreadFlags` 로는** 안 간다" 이다.
- 커널이 ProcessFlags bit9 를 어디에 쓰는지는 **안 봤다** → Q41.

### 2.5 (2) 커널은 run12·run13 대로 — 쪽지 + 빨간불

여기는 새로 본 게 없다. 이어 붙이기만 한다.

- `[정적]` (run12) `NtCreateUserProcess +0xdd3` `PspMapThreadCreationFlags` 가 bit0 를 flags bit0 로 옮긴다.
- `[정적]` (run9, run12) `PspInsertThread +0x2e5` `KeSuspendThread` 가 flags bit0 일 때만 불린다. 이번 바퀴로 **KERNELBASE 길이면 늘 불린다** `[정적]` (flow.md 2.2 `KeSuspendThread` 절에 추가된 줄).
- `[정적]` (run13) 그 안에서: 팻말 수 +1, 멈춤 쪽지 걸기, 신호등 빨강 ([run13 해설 2.3](run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)).
- `[정적]` (run12) 그래도 `+0x5a6` `KeReadyThread` 로 대기 줄에는 선다.
- `[정적]` (run9) 그리고 커널은 **풀어 주지 않고** 부모에게 돌아온다. 손에는 프로세스 손잡이와 **스레드 손잡이**가 들려 있다 ([핸들](run9-create-user-process-해설.md#핸들)).

### 2.6 (3) 그 사이 부모가 하는 일 — 검사 → csrss 알림 → 후처리

`NtCreateUserProcess` 가 돌아온 뒤, `NtResumeThread` 까지 부모가 하는 큰일 세 묶음이다.

**표** `[정적]` (자리), 역할은 run 문서 표 그대로

| # | 하는 일 | 대표 호출 (ret) | 비유 (태그 없음) |
|---|---|---|---|
| ① | 새 프로세스 검사 | `BasepCheckWebBladeHashes` `+0x3f91`, `BasepIsProcessAllowed` `+0x3fd1`, `BasepCheckWinSaferRestrictions` `+0x40c4` | 새 가게가 규칙에 맞는지 서류 검토 (역할은 이름으로 본 것 `[추측]`) |
| ② | **csrss 에 알림** | `BasepConstructSxsCreateProcessMessage` `+0x445f` → `BasepCsrCaptureSxsMessage` `+0x4640` → **`CsrClientCallServer` `+0x4672`** | 인사팀에 "새 가게·새 직원 생겼어요" 등록 |
| ③ | 상승·잡·앱 호환·패키지 후처리 | `BaseCheckElevation` `+0x47c1`·`+0x48d5`, `AssignProcessToJobList` `+0x55f1`, `NtWriteVirtualMemory` `+0x5795`·`+0x57d7`·`+0x5831`, `BasepInitAppCompatData` `+0x597d`, `BasepPostSuccessAppXExtension` `+0x59cd`, 패키지 마무리 3개 … | 관리자 승인 필요한지, 어느 묶음(잡)에 넣을지, 새 가게 안에 "호환 안내문" 붙여 주기, 앱 가게용 마무리 |

**새 말: csrss 와 `CsrClientCallServer`**

- csrss = **윈도우 서브시스템 서버** (run 문서). 관리사무소(커널)와는 **따로 있는 유저 쪽 프로세스**로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- `CsrClientCallServer` = 그 csrss 에 메시지를 보내는 호출. 여기선 "프로세스 만들었다" 메시지다. ApiNumber `0x1001d` (`+0x4655`), 길이 `0x218` (`+0x464f`) `[정적]`.
  - 이 번호가 csrss 쪽에서 정확히 무슨 일인지는 **안 봤다** → Q41.
- 비유: 관리사무소(커널)와 별개인 **인사팀 사무실**. 관리사무소가 가게를 지어 줘도, 인사팀 명부에 올라가야 일이 매끄럽게 돈다 (flow.md 2.2 의 비유와 같다).

**"반드시 지난다" 는 어떻게 알았나 — 흐름 그래프**

- 새 말: **흐름 그래프** = 함수 안의 점프(`JNZ`, `JMP` …)를 전부 따라가서 그린 **"어느 줄에서 어느 줄로 갈 수 있나" 길 지도**.
- 결과서는 `.asm` 을 읽는 임시 스크립트로 이 지도를 그리고 두 가지를 계산했다 (run 문서 "방법").
  - **도달 정의**: "이 줄에 왔을 때 ESI(또는 R15)의 값은 **어느 줄에서 넣은 것**일 수 있나." → 2.4 의 "늘 1", 2.8 의 "`R15B` 는 4".
  - **지배 관계**: "저 줄에 가려면 **반드시** 이 줄을 지나야 하나."
- 비유: 섬으로 가는 **다리가 하나뿐**이면, 섬에 간 사람은 누구든 그 다리를 건넜다. 지도만 보고도 말할 수 있다.

그 결과 `[정적]`:

- ② 의 `CsrClientCallServer`(`+0x466b`)를 **지나지 않고는** ③ 의 어느 호출에도, `NtResumeThread` 에도 **못 간다.**
- 예외는 하나: `CREATE_SECURE_PROCESS`(0x400000) 가 켜진 길. `+0x4172` / `+0x417d JNZ` 로 ①~③ 거의 전부를 건너뛰고 `+0x5bb2` 의 4 검사로 간다.
- ① 은 ② 앞에만 있다.
- 결과서는 "모든 호출은 돌아온다" 고 가정해서 길을 **더 많이** 잡았다. 그래서 "반드시 지난다" 는 결론은 오히려 **더 엄격한 쪽**이다.

주의할 점 `[정적]`:

- **묶음 순서(①→②→③)만** 흐름으로 확인했다. 묶음 **안** 순서는 코드 배치 순서일 뿐이다.
- ②를 빼면 **전부 조건부**다. 대부분 "어떤 import 가 있는지" 검사(12곳) 나 플래그 뒤에서만 부른다.
  - 그 검사 함수는 링커가 여러 함수를 한 주소로 합친 것이라 Ghidra 가 고른 이름을 믿을 수 없다 (같은 주소 이름 21개). [한 집에 문패 여럿](run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭) 과 같은 이야기다. 그래서 이름 대신 "어떤 import 가 있는지 알려 주는 검사" `[추측]` 로만 적는다.
- 사이에 `NtResumeThread` 에 닿을 수 있는 call 자리는 **76곳**. 잡일은 개수만: 옵션 import 검사 12곳, ETW/로그 12곳, 기능 켜짐 검사 7곳, 정리, 값 읽기.
- test.exe 때 ①~③ 중 **무엇이 실제로 도는지**는 모른다 (실행 때 검사 결과에 달림) → Q19 / flow.md 4장.

③ 에서 하나 눈에 띄는 것:

- `NtWriteVirtualMemory` 두 번째 쓰기가 **자식 PEB**(`[RSP+0x1a0]`) `+0x2d8` 자리에 주소를 쓴다 `[정적]` (자리). ([PEB](run10-allocate-process-해설.md#peb) = 가게 문 앞 안내판.)
- 그 자리가 x64 PEB 의 `pShimData` 자리로 알려진 오프셋이라 "앱 호환 데이터 위치" 로 보인다 `[추측]`.
- 비유: 부모가 자식 가게 **안내판에 "호환 안내문 위치" 를 적어 주는** 일. 직원이 일 시작하기 **전에**.

### 2.7 왜 굳이 늘 멈춰 두나 — 짐작의 고리

코드는 "**어떻게**" 만 말해 준다. "**왜**" 는 짐작이다. 근거와 짐작을 나눠 보자.

**근거** (각각 `[정적]`)

1. 커널에 늘 "멈춘 채로" 를 보낸다 (2.4).
2. 커널은 멈춘 채로 만들고, 스스로는 안 풀어 준다 (2.5, run9).
3. 돌아온 뒤 부모는 csrss 알림을 **반드시** 지나고, 앱 호환 데이터를 **자식 메모리에 써 넣는** 일 등을 한다 (2.6).
4. 풀어 주기(`NtResumeThread`)는 그 **뒤에만** 있다 (흐름 그래프).

**그래서 짐작은** `[추측]` (run 문서 "확인된 것" 마지막 줄)

- 자식이 **한 줄이라도 달리기 전에** csrss 등록, 앱 호환 데이터 써 넣기 같은 준비를 부모가 끝내 두려는 것으로 보인다.
- 일반적인 생각 `[추측]`: 만약 첫 직원이 바로 달리면, 부모가 아직 안 써 넣은 안내문이나 아직 안 된 등록을 **없는 채로** 보고 일을 시작할 수 있다. 늘 멈춰 두면 그런 경주가 생기지 않는다.
- 비유: 신입이 출근하기 전에 **인사팀 등록·자리 세팅을 다 끝내 두고**, 그다음에 문(신호등)을 열어 준다 (flow.md 2.2).

**"늘" 이라서 생기는 깔끔한 점** (위 `[정적]` 에서 바로 나오는 것)

- `CREATE_SUSPENDED` 를 주든 안 주든 **커널 안에서 첫 스레드가 만들어지는 길은 똑같다.**
- 차이는 오직 **마지막에 KERNELBASE 가 풀어 주느냐**(2.8) 하나다. (그리고 2.4 끝의 ProcessFlags bit9 — 뜻은 모름.)

**아직 확인 아님**

- 이 준비들이 정말 "자식이 달리기 전에 끝나야 하는 일" 인지는 코드에 쓰여 있지 않다. 자식 쪽이 그걸 **언제 읽는지**를 봐야 한다 (안 봤다).

### 2.8 (4) 마지막 갈림길 — `CREATE_SUSPENDED` 가 있으면 건너뛴다

이번 바퀴의 두 번째 큰 이야기다.

**주문서 사본 만들기** `[정적]`

- `+0x47` `MOV R14D,[RSP+0x2480]` — 7번째 인자 `dwCreationFlags` 를 읽는다.
- `+0xc7` `MOV [RSP+0x108],R14D` — 지역 칸 `[RSP+0x108]` 에 **사본**을 둔다.
  - 비유: 손님 주문서를 **복사해서 책상 위에** 둔다. 이후 검사는 이 복사본으로 한다.
- 이 사본은 중간에 몇 군데서 바뀐다. 다만 우선순위 비트를 지우는 `+0x7bf` 도 **bit2(4)는 남긴다** (결과서). 4 를 켜는 자리는 아래 `+0x5b5b` 다.

**검사** `[정적]`

```
+0x5bb2  MOV R15D,4
+0x5bb8  TEST byte ptr [RSP+0x108],R15B
+0x5bc0  JNZ  +0x5be9            ; 4 가 켜져 있으면 NtResumeThread 를 건너뜀
```

1. `R15B` = 4. 여기 닿는 길에서 R15 를 마지막으로 넣는 곳은 `+0x4532`, `+0x5bb2` 둘뿐이고 **둘 다 `MOV R15D,4`** 다 (결과서, 도달 정의).
2. 사본 & 4 가 0 이 아니면(= `CREATE_SUSPENDED` 켜짐) → `+0x5be9` 로 **건너뛴다.**
3. 0 이면 → `+0x5bc2` 로 내려가 인자 채우고 → **`+0x5bcc` call / `+0x5bd3` ret `NtResumeThread`**.
4. `+0x5bd8 ~ +0x5be3`: 돌아온 값이 음수(실패)면 실패 길(`BaseSetLastNTError`)로 간다.

| 호출자가 준 주문 | KERNELBASE 가 하는 일 | 첫 직원은 |
|---|---|---|
| `CREATE_SUSPENDED` **없음** (보통) | `+0x5bd3` `NtResumeThread` 로 **직접** 풀어 준다 `[정적]` | 팻말 0장 → 초록불 → 깨어남 (run13) |
| `CREATE_SUSPENDED` **있음** | 건너뛴다 `[정적]` | 팻말 1장인 채로 남는다 |

- `CREATE_SUSPENDED` 있음 쪽: 그땐 **호출한 프로그램이 나중에 `ResumeThread` 로 직접** 풀어 주는 것으로 알려져 있다 `[추측]` (일반적으로 알려진 Win32 사용법).
  - 그 경우 길은 `KERNELBASE!ResumeThread` `+0x15` → `NtResumeThread` 다 `[정적]` (같은 import 칸 `0x18029b9a8`). 이번 test.exe 길에는 **안 낀다** (run 문서).
- `[추측]` cmd.exe 가 test.exe 를 띄울 때 주문서엔 보통 4 가 **없다** → 풀어 주기가 불린다. cmd.exe 는 분석 범위 밖이라 동적으로만 본다 → Q19.

비유 (태그 없음): 대행 책상은 늘 "대기 팻말" 을 달아서 신청하고, 서류 일을 다 마친 뒤 손님 주문서 복사본을 한 번 더 본다. "팻말 그대로 둬" 에 체크가 없으면 책상이 직접 창구에 "팻말 떼 주세요" 를 낸다. 체크가 있으면 손님 몫으로 남겨 둔다.

**건너뛰는 다른 길 둘** (`[추측]` test.exe 와는 관계 없어 보임)

- `+0x5b5b OR [RSP+0x108],R15D` — 사본에 **4 를 켠다.**
  - 패키지(AppX) 앱 마무리 함수들(`CompletePackagedProcessCreationEx` `+0x5ac4`, `CompleteAppExecutionAliasProcessCreationEx` `+0x5b3d`, `BasepFinishPackageActivationForSxS` `+0x5ba8`)이 채우는 칸 `[RSP+0x284]` 가 켜졌을 때다 `[정적]`.
  - 새 말: **패키지(AppX) 앱** = 스토어 앱처럼 "패키지" 로 설치되는 앱. 비유: 프랜차이즈 가게라 본사 쪽 절차가 따로 있는 것.
  - "풀어 주기는 패키지 쪽이 한다" 는 신호로 보인다 `[추측]`.
- `+0x394d JNZ +0x5be9` — `BasepProcessInvalidImage`(`+0x392d`, 정상 PE 가 아닌 이미지 처리) 뒤 `[RSP+0x298] != 0` 이면. 16비트/VDM 쪽 길로 보인다 `[추측]`. test.exe 는 정상 PE 다.

### 2.9 `NtResumeThread` 다, `NtAlertResumeThread` 가 아니다 → run13 사슬과 이어진다

- `[정적]` `NtAlertResumeThread` 는 `CreateProcessInternalW`·`ResumeThread` 어디에도, **지도 전체에도 없다.**
- `[정적]` `CreateProcessInternalW` 는 KERNELBASE 의 `ResumeThread` 함수를 **부르지 않는다.** 두 함수가 같은 import 칸으로 **각자** `NtResumeThread` 를 부른다.
  - 비유: 대행 책상은 "팻말 떼기 대행" 창구(`ResumeThread`)를 거치지 않고, 같은 단축번호로 관리사무소에 **바로** 전화한다.
- 그래서 커널에선 run13 의 길이다 ([run13 해설 2.11](run13-resume-thread-해설.md#211-ntalertresumethread-와-다른-점--q5b-준비-정적) 에서 미리 비교해 둔 것).
  - `NtResumeThread +0x9b` → `PsMultiResumeThread +0x25` → `KeResumeThread +0x87` → `KiResumeThread +0xab` → `KiTryUnwaitThread` `[정적]` (run13).
  - `PsMultiResumeThread` 쪽이라 `ProcessFirstResume` 표시("이 가게 첫 직원 출근 허가 났음" 도장)도 켜는 길이다 `[정적]` (run13). 이 도장을 누가 읽는지는 아직 모른다 → Q40.

### 2.10 "한 곳" 이지만 "한 번" 이라는 뜻은 아니다

- `[정적]` `NtCreateUserProcess` 를 부르는 **자리**는 `+0x30e7` 한 곳뿐이다.
- `[정적]` 그런데 실패 길(`+0x3158`)에서 다시 `+0x30e7` 로 **돌아오는 길**이 코드 흐름에 있다.
  - `NtCreateUserProcess` 반환값이 음수면 `+0x3158` 쪽으로 간다 (`+0x3150 TEST` → `+0x3152 JNS`, 결과서).
- `[추측]` 앱 실행 별칭, 16비트 실행 파일 같은 **재시도**로 보인다.
- 비유: 창구는 하나지만, 신청서가 반려되면 고쳐서 **같은 창구에 다시** 낼 수 있다.
- test.exe 때 몇 번 냈는지는 실행해 봐야 안다 → Q19.

### 2.11 한 장의 시간표 — 부모 → 커널 → 자식 잠듦 → 부모 풀어 줌 → 자식 깨어남

이번 바퀴로 2.2 가 코드로 이어졌으니, run9 ~ run14 를 **시간 순서로 한 장**에 모아 보자.
왼쪽 칸이 어느 직원(스레드)인지, 오른쪽 칸이 근거의 강도다.

| 순서 | 누구 (어디서) | 무슨 일 | 근거 |
|---|---|---|---|
| 1 | 부모 cmd.exe 스레드 (유저, KERNELBASE) | `CreateProcessW +0x66` → `CreateProcessInternalW` | 호출 자리 `[정적]` run14. cmd.exe 가 이 입구로 오는지 `[추측]` |
| 2 | 부모 (유저, KERNELBASE) | `+0x6cb` ESI = 1 → `+0x30af` `ThreadFlags` = 1 | `[정적]` run14 |
| 3 | 부모 (유저 → 커널) | `+0x30ee` `NtCreateUserProcess` → (ntdll) → 창구 `KiSystemServiceCopyEnd+0x28` → `nt!NtCreateUserProcess` | KERNELBASE 호출 자리 `[정적]` run14. ntdll 스텁 `[추측]`. 커널 진입 `[관찰]` run3 |
| 4 | 부모 (커널) | `PspMapThreadCreationFlags` bit0 → bit0 → `PspInsertThread +0x2e5` `KeSuspendThread`: 팻말 +1, 쪽지, **빨간불** | `[정적]` run12·run13. test.exe 때 정말 이 길을 탔는지는 Q19 |
| 5 | 부모 (커널) | `PspInsertThread +0x5a6` `KeReadyThread`: 대기 줄에 세움 | `[정적]` run12 |
| 6 | 부모 (커널 → 유저) | 풀어 주지 않고 돌아옴. 스레드 손잡이를 `[RSP+0x1b8]` 로 받음 | `[정적]` run9 (풀어 주는 호출 없음), run14 (손잡이 칸) |
| 7 | **자식** test.exe 첫 스레드 (커널) | 차례가 오면 출근: `KiStartUserThread+0x28` → `PspUserThreadStartup +0x22` IRQL 0 → 쪽지 펼침 → `KiDeliverApc +0x220` → `KiSchedulerApc +0xf7` **빨간불 앞에서 잠듦** | 서는 자리 `[정적]` run13. `+0x28` 은 `[관찰]` run4. **언제** 거기 가나 `[추측]` |
| 8 | 부모 (유저, KERNELBASE) | ① 검사 → **② `+0x4672` `CsrClientCallServer`** → ③ 후처리 | 자리·묶음 순서 `[정적]` run14. 실제로 무엇이 도나는 모름 |
| 9 | 부모 (유저, KERNELBASE) | `+0x5bb8` 주문서 사본 & 4 ? → 없으면 아래로 | 검사 자리 `[정적]` run14. cmd.exe 값에 4 가 없다는 건 `[추측]` |
| 10 | 부모 (유저 → 커널) | `+0x5bd3` `NtResumeThread(스레드 손잡이, NULL)` → `nt!NtResumeThread` | KERNELBASE 호출 자리 `[정적]` run14. 시스템 콜 진입 칸 `[추측]` |
| 11 | 부모 (커널) | `+0x9b` `PsMultiResumeThread` → `+0x25` `KeResumeThread` (팻말 -1 → 0) → `+0x87` `KiResumeThread` (`+0x20` **초록불**) → `+0xab` `KiTryUnwaitThread` (**어깨 두드림**) | `[정적]` run13 |
| 12 | **자식** (커널 → 유저) | 깨어나 `KiSchedulerApc` → `KiDeliverApc` → `PspUserThreadStartup` 나머지 → 유저 쪽 (2.3) → … → `main` | `[추측]` run13 (고리는 `[정적]`, 잇는 연결이 추측) |

읽을 때 주의:

- **7 과 8~10 은 다른 직원**이 동시에 하는 일이다. 표의 줄 순서는 "보통 이럴 것" 이지, 둘 사이 순서가 정해진 건 아니다 `[추측]`.
  - 부모가 먼저 초록불을 켜(10~11) 버리면, 자식은 7 에서 초록불을 보고 **기다리지 않고 지나간다.** 코드가 이 경우도 처리한다 `[정적]` ([run13 해설 2.9](run13-resume-thread-해설.md#29-경주--부모가-먼저-초록불을-켜면-정적)).
- 그래도 **12(자식이 실제로 일 시작)는 10·11 뒤**일 것이다 `[추측]`. 자식은 빨간불이 초록이 되기 전엔 7 의 자리(`KiSchedulerApc +0xf7`)를 못 지나가기 때문이다. 이게 "늘 멈춰 두기" 가 노리는 것으로 보인다.
  - 고리 하나하나(빨간불, 서는 자리, 초록불)는 `[정적]` 이다.
  - 다만 "쪽지가 유저 쪽으로 나가기 **전에** 펼쳐진다" 는 run13 의 `[추측]`(배달 시점)에 기대고, test.exe 때 `KeSuspendThread` 가 정말 불렸다는 전제(Q19)도 있다. 그래서 이 줄 전체는 `[추측]` 으로 둔다.
- 실행(디버거)으로 본 줄은 **3 의 커널 진입 하나와 7 의 `+0x28` 하나**뿐이다. 나머지는 노선도(코드)만 읽은 것이다.

비유로 한 번 더 (태그 없음):

1. 부모 가게의 대행 책상이 "새 가게 내 주세요, 첫 직원은 대기 팻말 달아서" 신청서를 낸다.
2. 관리사무소는 직원을 뽑아 책상에 쪽지를 붙이고, 신호등을 빨강으로 돌리고, 출근 대기 줄에 세운다. 그리고 손잡이를 건네며 돌려보낸다.
3. 첫 직원은 차례가 오면 출근해서 쪽지를 펼쳐 보고, 빨간불 앞 의자에서 잔다.
4. 그동안 대행 책상은 서류 검토 → 인사팀(csrss) 등록 → 안내판에 호환 안내문 적기 같은 일을 마친다.
5. 손님 주문서에 "팻말 그대로 둬" 체크가 없으니, 책상이 손잡이를 들고 다시 창구에 "팻말 떼 주세요".
6. 관리사무소가 팻말을 떼고, 신호등을 초록으로 바꾸고, 어깨를 두드린다.
7. 직원이 일어나 첫 창구의 남은 일을 하고, 유저 쪽으로 나가 `main` 을 향해 간다.

### 2.12 run3 관찰과는 어긋나지 않는다 — 다만 아직 이어 본 적은 없다

- `[관찰]` (run3) cmd.exe 스레드가 `KiSystemServiceCopyEnd+0x28` 에서 `nt!NtCreateUserProcess` 로 들어왔다.
- `[정적]` (run14) KERNELBASE 쪽엔 `NtCreateUserProcess` 를 부르는 자리가 `+0x30ee` 하나다.
- 둘은 **어긋나지 않는다.** 하지만 그때 **유저 쪽 콜스택을 안 찍었으니**, run3 의 호출이 `CreateProcessInternalW+0x30ee` 에서 왔다는 건 아직 `[추측]` 이다 (run 문서).
- 비유: 노선도엔 "이 정류장에서 저 정류장으로 가는 버스는 이 노선 하나뿐" 이라고 쓰여 있고, 우리는 저 정류장에 버스가 도착한 건 봤다. 그 버스가 정말 이 노선이었는지는 **번호판(콜스택)** 을 봐야 안다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만 적는다.

| 자리 | 전 (run13 뒤) | 후 (run14) |
|---|---|---|
| 기준 | 정적 근거 run9 ~ run13 (ntoskrnl) | 정적 근거 **run9 ~ run14**, kernelbase.pdb GUID 줄 추가 |
| 두 그림 사이 설명 | **nt 쪽만** 코드로 채움. 부모 유저 쪽이 정말 `NtResumeThread` 를 부르는지 모름 (Q5b) | **nt 쪽과 부모 KERNELBASE 쪽**을 코드로 채움. "늘 멈춘 채로 → csrss 알림 등 → `CREATE_SUSPENDED` 없으면 직접 `NtResumeThread`" `[정적]`. "실행으로 본 건 아직 없다" 명시 |
| 그림 1 (부모) | 점선 상자 하나 "부모 유저 쪽 CreateProcess 계열 [KERNELBASE 추정] (이름 확인 필요, Q5b)" | **KERNELBASE 상자**: `CreateProcessW` → `CreateProcessInternalW`. **ntdll 상자**(import 로 부름, 안쪽은 안 봄): `NtCreateUserProcess` / `CsrClientCallServer` / `NtResumeThread`. 화살표 `(1) +0x30ee`, `(2) +0x4672`, `(3) +0x5bd3` |
| 그림 1 풀어 주기 묶음 | "(2) 풀어 주기", `nt!NtResumeThread` "(정적: 부모가 부르는지는 Q5b)" | **"(3) 풀어 주기"** 로 번호 바뀜 (csrss 알림이 (2) 가 됨), `nt!NtResumeThread` "(정적)" |
| 그림 2 노란 메모 | "같은 무렵 부모: `NtResumeThread` → … 로 깨움 (추측, Q5b)" | "`CreateProcessInternalW +0x5bd3` `NtResumeThread` → … (부르는 자리는 정적 run14, **시점은 추측**)" |
| 2.1 `nt!NtCreateUserProcess` | `[관찰]` 시스템 콜로 들어옴. `[추측]` 유저 `CreateProcess` 가 이 시스템 콜 하나로 요청 | `[정적]` 부르는 곳 = `KERNELBASE!CreateProcessInternalW +0x30ee`, 한 곳. `[추측]` run3 도 여기서 왔을 것. 재시도 길이 있어 몇 번인지는 동적 |
| 2.1 `PspMapThreadCreationFlags` | bit0 → bit0 | `[정적]` KERNELBASE 는 `ThreadFlags` 로 **늘 1** → flags bit0 늘 켜짐 |
| 2.2 제목 | nt 쪽 `[정적]`, 부모 유저 쪽 `[공백]` | **nt 쪽·부모 KERNELBASE 쪽 `[정적]`** (run9, run12, run13, run14) |
| 2.2 앞머리 | 비유: 멈춰 두기 / 대기 줄 / 풀어 주기 | 한 줄 추가: "부모는 **늘 빨간불로 시작해 달라**고 하고, 서류를 챙긴 뒤 직접 초록불로." 트리 맨 위에 **부모 KERNELBASE 전체 순서** 추가 |
| 2.2 새 절 | — | `KERNELBASE!CreateProcessW`, `KERNELBASE!CreateProcessInternalW` (세 자리, `ThreadFlags` = 1, `CREATE_SUSPENDED` 검사, 건너뛰는 다른 길, `NtAlertResumeThread` 안 씀, 사이의 큰일, "왜 늘 멈춰 두나" `[추측]`) |
| 2.2 `NtCreateUserProcess` 안 | `[공백]` 부모가 정말 부르나, 어느 쪽인가 → Q5b | **(해결됨, 정적)** `+0x5bd3` `NtResumeThread`, `CREATE_SUSPENDED` 없을 때만. run13 까지의 `[추측]` 이 코드로 맞음. 실제로 지나는지는 동적 |
| 2.2 `KeSuspendThread` | 조건(flags bit0)과 하는 일 | `[정적]` KERNELBASE 로 왔다면 **이 길을 늘 탄다** 한 줄 추가 |
| 2.2 `NtResumeThread` | `[공백]` 둘 중 어느 쪽을 부르나 → Q5b | **(해결됨, 정적)** `NtResumeThread`. 실제 cmd.exe 실행으론 아직 |
| 2.2 run12 와 맞물림 | "나중에 부모가 `NtResumeThread` 를 부르면" | 부르는 자리 `+0x5bd3` (run14 `[정적]`), "KERNELBASE 는 늘 켜서 보낸다" 추가 |
| 4장 공백 표 | 2.2 "남은 것: 부모 쪽 마무리, 정말·언제 부르나, 어느 쪽인가 (Q5b), flags bit0 실제 값 (Q19)" | **해결됨(정적)** 한 줄 + 새 줄 6개: bit0 실제 값(Q19), cmd.exe 의 `dwCreationFlags`, 입구 연결·나머지 4곳, `NtCreateUserProcess` 몇 번, ①~③ 중 실제로 도는 것, ProcessFlags bit9, `CsrClientCallServer` DLL·ApiNumber·ntdll 스텁 |
| 4.1 | "(2) 풀어 주기: 부모 유저 쪽 ==> 진입, 근거 없음 (Q5b)" | KERNELBASE 줄 3개, ntdll ==> nt 줄, "(3) 풀어 주기" 진입 줄로 바뀜. 그림 2 순서 줄에 `+0x466b` 추가 |
| 5장 | run13 까지 | run14 줄, 요청서 번호 표에 run14 줄 |

태그로 보면 핵심 변화는 세 가지다:

- 부모 유저 쪽이 첫 스레드를 풀어 주나: `[추측]` → **`[정적]`** (`KERNELBASE!CreateProcessInternalW +0x5bd3` `NtResumeThread`, `CREATE_SUSPENDED` 없을 때만).
- 커널에 넘기는 `ThreadFlags` bit0: 어디서 오는지 몰랐음 → **`[정적]` 늘 1** (`+0x6cb`, `+0x30af`). 실행 값은 여전히 Q19.
- 그림 1 의 점선 상자 "[KERNELBASE 추정]": 이름 확인 필요 → **`[정적]` 함수 이름·호출 자리 확인**. 단 cmd.exe 가 이 입구로 오는지는 `[추측]`.

### 큰 그림에서 어떻게 이어지나

- **2.2 의 다리가 양쪽 끝까지 놓였다 (코드로는).**
  - run13 에서 커널 쪽 장치(빨간불·초록불)를 봤고, 이번에 그 스위치를 누르는 **부모 쪽 손가락**을 봤다.
  - 그래서 그림 1 은 "KERNELBASE → (1) 만들기 → (2) csrss 알림 → (3) 풀어 주기" 로 **한 줄로 읽힌다.**
  - 그림 1 과 그림 2 는 여전히 화살표로 안 이어진다. **다른 직원(스레드)** 이라 호출 관계가 없다. 노란 메모로 짝을 보인다.
- **"첫 직원이 일 시작하는 순간" 이 무엇에 달렸는지 분명해졌다.** 부모가 csrss 알림·후처리를 마치고 `+0x5bd3` 을 부르는 순간이다 (코드 기준).
- **동적 한 바퀴(Q19)의 질문이 좁혀졌다.** 이제 "부르나?" 가 아니라 "이 자리(`+0x30ee`, `+0x4672`, `+0x5bd3`)를 이 순서로 지나나, 값이 1 인가, 몇 번인가" 를 보면 된다.
- **다음 구간** (`plan.md`: 다음 항목 **Q6**): 2.3 `nt!KiStartUserThread`, `nt!PspUserThreadStartup` callee. 시간표 7·12 줄, 즉 **깨어난 첫 직원이 유저 쪽으로 나가는 길**이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| cmd.exe 가 실제로 넘기는 `dwCreationFlags` 에 `0x4` 가 있나 (`[추측]` 보통 없다 → 풀어 주기가 불림) | cmd.exe 는 분석 범위 밖이고, 값은 실행 때 정해진다 | **Q19** (동적): `+0x5bb8` 에서 `dd @rsp+0x108 L1` |
| `NtCreateUserProcess` 8번째 인자가 **실제로** 1 인가 | 코드로는 상수 1. 실행 값은 따로 봐야 한다 | **Q19** (동적): `KERNELBASE!CreateProcessInternalW+0x30e7` 에서 `dd @rsp+0x38 L1`, 또는 커널 `nt!NtCreateUserProcess` 입구 `poi(@rsp+0x40)` |
| `NtCreateUserProcess` 가 **몇 번** 불리나 | 자리는 한 곳이지만 재시도 길이 코드에 있다 (2.10) | **Q19** (동적): `+0x30e7` 에 몇 번 서나 |
| 풀어 주는 호출이 정말 `CreateProcessInternalW+0x5bd3` 에서 오나. `NtAlertResumeThread` 엔 안 서나 | 노선도만 읽었다. 번호판(콜스택)을 안 봤다 | **Q19** (동적): `nt!NtResumeThread` 입구(cmd.exe 일 때만)에서 긴 `k` |
| run3 의 `NtCreateUserProcess` 가 `+0x30ee` 에서 왔나 (2.12) | run3 때 유저 쪽 콜스택을 안 찍었다 | **Q19** (동적): `nt!NtCreateUserProcess` 입구에서 긴 `k` |
| `CsrClientCallServer`(`+0x4672`)가 `NtResumeThread` **앞에** 실제로 오나 | 흐름상 반드시 앞이지만 실행 순서는 안 봤다 | **Q19** (동적, 선택): `+0x466b` 멈춤점 |
| ①~③ 중 test.exe 때 **실제로 도는 것** ("어떤 import 가 있는지" 검사 12곳이 무엇을 돌려주나) | 실행 때 검사 결과에 달려 있다 | **Q19** (동적) |
| cmd.exe 가 `kernel32!CreateProcessW` → `KERNELBASE!CreateProcessW` 로 오나 `[공백]`. `CreateProcessInternalW` 를 부르는 나머지 4곳의 이름 | kernel32 는 이번에 안 봤고, 4곳은 뽑지 않았다 | **Q41** (정적, kernel32), 입구는 **Q19** (동적: `CreateProcessInternalW` 입구에서 `k`) |
| (해결됨) `CsrClientCallServer` 를 **어느 DLL** 에서 가져오나 | — | `[정적]` ntdll.dll (KernelBase import 표로 확인) |
| ApiNumber `0x1001d` 가 csrss 쪽에서 **무슨 일**인가 | csrss 쪽은 범위 밖 | **Q41** (정적) |
| KERNELBASE 가 `CREATE_SUSPENDED` 가 없을 때 켜는 **ProcessFlags bit9(`0x200`)** 를 커널이 어떻게 쓰나 (2.4 끝) | 커널 `NtCreateUserProcess` 쪽에서 이 비트를 따라가 보지 않았다 | **Q41** (정적, ntoskrnl) |
| ntdll `NtCreateUserProcess`·`NtResumeThread` 가 곧장 시스템 콜을 하는 스텁인가 (`[추측]` 그렇다) | ntdll 쪽은 이번에 안 열었다 | **Q41** (정적, ntdll) |
| "왜 늘 멈춰 두나" 의 진짜 이유 — 자식이 csrss 등록·앱 호환 데이터를 **언제 읽는지** (2.7) | 부모 쪽 코드만 봤다. 자식 쪽이 그걸 읽는 자리는 안 봤다 | 대기열에 따로 줄은 없다. 자식 로더 쪽(Q8 이후 ntdll 바퀴)에서 만나면 이어 볼 것 |
| `PEB +0x2d8` 에 쓰는 게 정말 `pShimData` 인가 `[추측]` | 알려진 오프셋으로 맞춘 것이다. 이 빌드 PDB 로는 안 맞춰 봤다 | 대기열에 따로 줄은 없다 |
| 패키지(AppX)·VDM 쪽에서 건너뛰는 두 길이 test.exe 와 정말 무관한가 `[추측]` | 실행 때 조건이다 | **Q19** 에서 `+0x5bd3` 에 서면 저절로 확인된다 |
| 첫 스레드가 실제로 언제 잠들고 언제 깨나, 부모와 누가 먼저인가 (시간표 7 과 8~10) | 두 스레드 사이 순서는 실행해 봐야 안다 | **Q19** (동적, run13 때부터 이어진 질문) |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run14-create-process-internal.md`](run14-create-process-internal.md)
- 정적 요청서: [`_workspace/sast/09_create-process-internal.md`](../../_workspace/sast/09_create-process-internal.md)
- 정적 결과서 (지도에서 본 것, 질문별 답, 인자 자리 전체, 사본이 바뀌는 자리, 흐름 그래프 방법, 동적 확인 목록): [`_workspace/sast/09_create-process-internal_result.md`](../../_workspace/sast/09_create-process-internal_result.md)
- 바퀴 전 관찰: [`runs/01_sprint/run3-create.md`](../01_sprint/run3-create.md) (`KiSystemServiceCopyEnd+0x28` → `NtCreateUserProcess`)
- 앞 바퀴 해설:
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — "시스템 콜", "비트와 플래그", "스레드를 멈춘 채로 만들기", "핸들", 3.6 멈춤 갈림길
  - [`run12-insert-thread-해설.md`](run12-insert-thread-해설.md) — 2.5 flags 는 어디서 오나
  - [`run13-resume-thread-해설.md`](run13-resume-thread-해설.md) — 2.3 쪽지 + 신호등, 2.7 풀어 주는 사슬, 2.9 경주, 2.11 `NtAlertResumeThread` 와 다른 점
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "그림 1" 의 KERNELBASE 상자, ntdll 상자, "(3) 풀어 주기" 묶음 / "그림 2" 의 노란 메모
  - "2.1" 의 `nt!NtCreateUserProcess`, `PspMapThreadCreationFlags` 절
  - "2.2" 앞머리 트리 (부모 KERNELBASE 전체 순서), `KERNELBASE!CreateProcessW`, `KERNELBASE!CreateProcessInternalW` 절, `KeSuspendThread`·`NtResumeThread` 절의 run14 줄, "run12 `KeReadyThread` 와 맞물림"
  - "4. 공백 목록", "4.1 추측·정적으로 둔 호출 관계"
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q5b, Q19, Q41 (다음 항목 Q6)

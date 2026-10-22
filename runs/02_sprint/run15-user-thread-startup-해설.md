# run15 user-thread-startup — 해설

> 이 글은 [run15 문서](run15-user-thread-startup.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q6** — 2.3 `nt!KiStartUserThread`, `nt!PspUserThreadStartup` callee (`_workspace/plan.md`)
- 근거:
  - run 문서 [`run15-user-thread-startup.md`](run15-user-thread-startup.md) (가장 중요)
  - 요청서 [`_workspace/sast/10_user-thread-startup.md`](../../_workspace/sast/10_user-thread-startup.md)
  - 결과서 [`_workspace/sast/10_user-thread-startup_result.md`](../../_workspace/sast/10_user-thread-startup_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전), run13 문서 맨 위 정정 한 줄 (`git diff -- runs/02_sprint/run13-resume-thread.md`)
  - 진행표: `_workspace/plan.md` 의 Q6, Q7, Q42
  - 바퀴 전 관찰: [`runs/01_sprint/run4-thread-start.md`](../01_sprint/run4-thread-start.md) (콜스택)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다.
- Ghidra 가 함수로 안 잡은 곳은 결과서가 **원본 바이트**(`objdump`)로, 구조체 칸 이름은 **PDB 타입 레코드**로 읽었다. 이 글에서 "(원본 바이트)", "(PDB 타입)" 이라고 붙인 곳이 그것이다.

이 글은 [run9](run9-create-user-process-해설.md) ~ [run14](run14-create-process-internal-해설.md) 해설 다음 편이다.

- 기초 말(오프셋, call/ret, 콜스택, 태그, 간접 호출·CFG …)은 run9 해설 2절에 있다. 여기선 **링크만** 건다.
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, `_ETHREAD` = 직원 **인사 카드**
  - 콜스택 = **책갈피 더미** (run9)
  - `KeReadyThread` = **출근 대기 줄** (run12)
  - `PspUserThreadStartup` 주소 = 인사 카드에 붙인 "**첫 출근하면 이 창구부터**" 쪽지 (run11)
  - CONTEXT = 직원에게 주는 **업무 시작 메모** ("어디서부터 일할지(`Rip`)") (run11)
  - 멈춤 APC = 출근하자마자 **펼쳐 볼 쪽지**, `SuspendEvent` = 직원 앞 **신호등** (run13)
  - IRQL = 직원 머리 위 **"방해 금지" 표지판**, `SwapContext` = 계산대 **교대**, 소프트웨어 인터럽트 = **초인종 예약** (run13)
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- 이번에 flow.md 2.3 에 새로 붙은 비유: 새 직원 첫 출근 — 문(커널) 안쪽에서 **출근 도장**(`PspUserThreadStartup`)을 찍고, 책상에 붙은 메모(트랩 프레임)에 적힌 자리로 **한 번에 순간 이동**(`IRETQ`)해서 일(유저 코드)을 시작한다.
  - flow.md 는 그 메모를 "책상에 붙은 쪽지" 라고 불렀다. 이 글에선 run13 의 **멈춤 쪽지**와 헷갈리지 않게 **"출발 메모"** 라고 부르겠다. 같은 것이다.

한 줄로 먼저 말하면:

- 01_sprint 에서 본 콜스택 네 줄(`PspUserThreadStartup` ← `KiStartUserThread+0x28` ← `KiStartUserThreadReturn` ← `RtlUserThreadStart`)이 이제 **한 줄씩 코드로** 설명된다.
- 새 스레드는 누가 "불러서" 출발하는 게 아니다. **교대(`SwapContext`)의 `RET` 로 "떨어져서"** 출발한다. 길은 스택에 **미리 쌓아 둔 값**이 정한다.
- 커널 → 유저 전환은 **`IRETQ`** 라는 명령 하나다. 유저 첫 위치는 **트랩 프레임의 `Rip` 칸**에서 꺼낸다. 2.3 의 `[공백]` 이 코드로 채워졌다 `[정적]`.
- run13 의 "멈춤 쪽지는 `+0x22` 직후 펼쳐진다" 추측은 고리 3개가 `[정적]` 으로 굳고, **마지막 하나만** `[추측]` 으로 남았다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW              (run14)
     ├─ 시스템 콜 nt!NtCreateUserProcess                 (run9 ~ run12)  첫 직원 대기 팻말 + 대기 줄
     └─ 시스템 콜 nt!NtResumeThread                      (run13, run14)  팻말 떼기

 test.exe 첫 스레드 (커널)
  SwapContext (교대) ─RET─▶ KiStartUserThread             ← 이번 바퀴: 어떻게 들어오나
       └─ PspUserThreadStartup                            ← 이번 바퀴: 안에서 무엇을 하나
            (멈춘 스레드면 여기서 잠듦, run13)
  ═▶ 커널 → 유저 전환                                     ← 이번 바퀴: [공백] 이던 곳
 test.exe 첫 스레드 (유저)
  ntdll!LdrInitializeThunk → 로더 → main                  (01_sprint 관찰)
```

- 이번 구간은 **test.exe 스레드가 커널 안에서 첫 발을 떼서 유저로 넘어가기까지**다.
- flow.md 로는 **2.3 절**, 그림 2 의 맨 앞 `nt` 상자다.

### 무엇이 궁금했나

run 문서 "목적" 그대로, 네 가지다.

1. 콜스택에 보인 `KiStartUserThread` 는 Ghidra 에서 **어디**인가. `+0x28` 바로 앞이 시작 루틴을 부르는 간접 호출인가. 시작 루틴 주소는 **어디서** 오나.
2. 콜스택에 보인 `KiStartUserThreadReturn` 은 **무엇**인가.
3. 2.3 `[공백]` **커널 → 유저 전환**: 어떤 명령으로, 어디서 유저로 넘어가나. 유저 첫 위치(`RIP`)는 **어느 칸**에서 꺼내나.
4. `PspUserThreadStartup` 안의 흐름. 특히 run13 `[추측]` "멈춤 APC 는 `+0x22` 직후" 를 더 굳힐 수 있나. `PspInitializeThunkContext` 는 **첫 스레드만** 부르나.

### 바퀴 전에 알던 것

- `[관찰]` (01_sprint run4) `nt!PspUserThreadStartup` 입구에 멈춰서 `k` 를 찍으면, test.exe 스레드 4개 모두 같은 모양이었다.

  ```
  00  nt!PspUserThreadStartup
  01  nt!KiStartUserThread+0x28
  02  nt!KiStartUserThreadReturn
  03  ntdll!RtlUserThreadStart        ← 유저 스택 쪽
  ```

- `[관찰]` (run4) 같은 스레드가 다음에 유저 쪽에서 처음 멈춘 곳은 `ntdll!LdrInitializeThunk` 이고, 그때 콜스택엔 **부른 곳이 없었다**.
- `[정적]` (run11) `PspAllocateThread +0x681` 이 `PspUserThreadStartup` 주소를 시작 정보 묶음 `+0x8` 칸에 넣어 `KeInitThread` 로 건넨다 ([run11 해설 2.5](run11-allocate-thread-해설.md#25-pspuserthreadstartup-은-불리는-게-아니라-주소가-건네진다)).
- `[정적]` (run13, 원본 바이트) `KiStartUserThread` 는 IRQL 을 1 로 올리고 `+0x23` 에서 시작 루틴을 부른다 → 돌아올 자리 `+0x28`. run4 와 오프셋까지 같다 ([run13 해설 2.12](run13-resume-thread-해설.md#212-kistartuserthread-0x28--관찰과-코드가-맞았다)).
- `[정적]` (run13) 새 스레드 첫 커널 스택에 `KiStartUserThread` 주소를 넣는 곳은 `KiInitializeContextThread +0x1e1`.
- `[추측]` (run11·run13) 시작 정보 `+0x8` 칸이 `KiInitializeContextThread` 를 거쳐 "저장해 둔 시작 루틴" 이 된다. 사이를 한 줄씩은 안 따라갔다.
- `[추측]` (run13) 멈춘 첫 스레드는 `PspUserThreadStartup +0x22` 의 IRQL 내림 **직후** 멈춤 쪽지를 펼치고 잠든다 ([run13 해설 2.6](run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)).
- `[공백]` 커널 → 유저 전환 방법. `LdrInitializeThunk` 가 왜 "부른 곳 없이" 시작하는지, 콜스택의 `RtlUserThreadStart` 와는 무슨 관계인지.

---

## 2. 찾은 것

### 2.1 한눈에 — 커널 안 첫 발부터 유저까지

run 문서 "호출 관계" 를 줄였다. `Kx` = Ghidra 함수 `KxStartUserThread` 기준 오프셋 (→ 2.3). `PspUserThreadStartup` 아래 오프셋은 모두 **돌아올 자리(ret)** 다 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)).

```
SwapContext 끝 조각 +0x2bf RET ── 첫 스택 맨 위 주소로 "돌아감" (call 아님)     [정적]
KiStartUserThread  (= Kx +0x60 이름표)                         [관찰] run4 이름 + [정적]
 │  Kx +0x71  IRQL 1
 │  Kx +0x7e  RAX = [RSP+0x10] = PspUserThreadStartup
 ├─ (1) +0x28 (Kx +0x88)  PspUserThreadStartup                  [관찰] run4 + [정적]
 │       │  +0x22  IRQL 0  ═▶ (멈춘 스레드면) APC 인터럽트 … (2.2)   [추측: 자리]
 │       ├─ (1) +0x3c  PspDisablePrimaryTokenExchange
 │       ├─ (2) +0x78  PsGetSessionLocaleId                          (Minimal = 0)
 │       ├─ (3) +0x99  KeUpdateTebSchedulingPropertiesCurrentThread  (Minimal = 0)
 │       ├─ (4) +0xa1  PspNotifyThreadCreation                       (Minimal = 0)
 │       └─ (5) +0xb3  PspInitializeThunkContext   (Terminated = 0, Minimal = 0. 모든 스레드)
 │  Kx +0x88 ~      레지스터 되살리기 → CLI → 유저 APC 확인 → 마무리
 └─ (2) ═▶ 유저  IRETQ                                            [정적]
          KiKvaShadow 꺼짐: Kx +0x3e1
          KiKvaShadow 켜짐: JMP → KiKernelExit +0xa5
          꺼내는 칸: _KTRAP_FRAME.Rip(+0x168) …
          ─▶ ntdll!LdrInitializeThunk   [관찰] run4 (이 Rip 값을 누가 정하나는 Q7)
```

읽을 때 주의:

- 맨 위 줄은 **호출이 아니다.** `RET`(돌아가기) 명령으로 들어온다 (→ 2.4).
- `═▶` 굵은 화살표는 호출이 아니라 **실행이 넘어감**이다 (인터럽트, 커널 → 유저). flow.md 범례와 같은 뜻이다.
- `PspUserThreadStartup` 아래 다섯 줄의 태그는 "**그 호출이 코드에 있다**" `[정적]` 까지다. test.exe 때 정말 이 길을 탔는지는 아직 안 봤다 (Q42).

### 2.2 한 줄씩 읽는 법

트리에서 두 줄을 "**누가 / 어디서 / 누구를 / 무엇을 위해**" 로 풀어 보자.

**줄 1: `├─ (1) +0x28 (Kx +0x88)  PspUserThreadStartup`**

- 누가: `KiStartUserThread` (Ghidra 로는 `KxStartUserThread`)
- 어디서: 부르는 자리 `Kx +0x83`, 돌아올 자리 `Kx +0x88`. WinDbg 콜스택에선 `KiStartUserThread+0x28` 로 보인다.
  - 같은 주소 `0x1406afbf8` 에 이름이 두 벌 붙는 셈이다 (→ 2.3).
- 누구를: `PspUserThreadStartup`. 이름이 명령에 박혀 있지 않다. `Kx +0x7e` 에서 스택 칸 `[RSP+0x10]` 의 값을 꺼내 [CFG](run9-create-user-process-해설.md#직접-호출과-간접-호출) 검사 함수 `_guard_dispatch_icall` 을 거쳐 부르는 **간접 호출**이다.
  - 장부 `_calls.tsv` 표기: `KxStartUserThread +0x83 +0x88 indirect:cfg:stack[-0x128]`.
  - `stack[-0x128]` 은 입구 RSP 기준이고, 지금 RSP 기준으론 `[RSP+0x10]` 이다. 같은 칸이다 (결과서).
- 무엇을 위해: 커널 쪽 "출근 도장" 을 찍으러 (→ 2.8). 태그: `[관찰]` run4 + `[정적]`.

**줄 2: `└─ (2) ═▶ 유저  IRETQ`**

- 누가: 역시 `KiStartUserThread`. `PspUserThreadStartup` 이 **돌아온 뒤** 이어서 한다.
  - 그래서 flow.md 그림 2 에서 유저로 가는 굵은 화살표가 `PspUserThreadStartup` 상자가 아니라 **`KiStartUserThread` 상자**에서 나가게 바뀌었다 (→ 3절).
- 어디서: 둘 중 한 자리. `Kx +0x3e1` 또는 `KiKernelExit +0xa5`.
- 무엇을: `IRETQ` 명령으로 유저 모드에 들어간다.
- 무엇을 위해: 트랩 프레임 `Rip` 칸에 적힌 자리(관찰로는 `ntdll!LdrInitializeThunk`)에서 유저 일을 시작하려고. 태그: `[정적]` (실제로 탄 자리·값은 동적, Q42).

### 2.3 `KiStartUserThread` 는 함수가 아니라 "이름표"다

**새 말**

- **이름표(label)** = PDB 에 이름은 있는데 "함수" 로 등록되지 않은 주소. 함수 **한가운데**에 붙어 있을 수 있다.
  - 비유: 책 한 장(章) 안 중간 쪽에 붙인 **포스트잇 이름표**. 장 제목(함수 이름)과 따로 논다.

**무엇을 봤나**

- `[정적]` 오케스트레이터가 `pdb_aliases.py --where` 로 확인 (요청서):
  - `KiStartUserThread` = rva `0x6afbd0`. PDB 종류 **none**(함수 아님). `_index.tsv` 에 없음.
  - 그 자리는 Ghidra 함수 **`KxStartUserThread`** (rva `0x6afb70`) 안 **`+0x60`** 이다.
- 그래서 오프셋 환산은 이렇다: **`KiStartUserThread+x` = `KxStartUserThread +(x+0x60)`**.
  - run4 의 `KiStartUserThread+0x28` = `0x28 + 0x60` = **`Kx +0x88`**. 결과서가 같은 주소(`0x1406afbf8`)임을 확인했다.

**왜 WinDbg 는 `KiStartUserThread+0x28` 이라고 보여 줬나**

- `[추측]` WinDbg 는 주소를 **가장 가까운 앞 이름** + 오프셋으로 보여 준다 (run 문서).
- `Kx +0x88` 에서 앞으로 가면 `Kx +0x0`(`KxStartUserThread`)보다 `Kx +0x60`(`KiStartUserThread`)이 **더 가깝다**. 그래서 `KiStartUserThread+0x28`.
- 비유: 쪽 번호를 "가장 가까운 포스트잇에서 몇 쪽 뒤" 로 부르는 버릇.

**그럼 `Kx +0x0 ~ +0x5f` 는?**

- `[정적]+[추측]` 실제로 도는 코드가 아니라 **풀어 보기(unwind)용 앞머리**로 보인다.
  - **풀어 보기(unwind)** = 디버거나 예외 처리가 스택을 **한 칸씩 거슬러** 읽어서 "이 함수는 누가 불렀나" 를 알아내는 일 `[추측]` (일반적으로 알려진 설명). 함수마다 `.pdata` 에 "내 스택은 이렇게 생겼다" 는 설명서(UNWIND_INFO)가 붙어 있다.
  - 비유: 책갈피 더미를 위에서부터 거꾸로 넘길 때 보는 **넘기기 안내서**.
- 근거 (run 문서):
  - 이 범위가 UNWIND_INFO 의 앞머리 설명(스택 `0x138` 확보, `xmm6~15`·`rbx` 등 저장, 길이 `0x5f`)과 **같다**.
  - `0x6afb70` 을 부르는 곳은 아래 2.5 의 `KyStartUserThread` 템플릿 안 `call` **하나뿐**이다 (결과서가 실행 구역 전체를 훑음. 계산해서 만드는 주소는 못 잡음).
  - 새 스레드는 `+0x0` 이 아니라 곧바로 `+0x60` 으로 들어온다 (→ 2.4).
- 비유: 장 첫머리 몇 쪽은 **실제로 읽는 본문이 아니라** "이 장은 이런 구조" 라는 안내문이다. 진짜 읽기는 포스트잇(`+0x60`)부터 한다.

**`+0x60` 부터 하는 일** `[정적]` (`KxStartUserThread.asm`)

| 자리 (Kx) | WinDbg 모양 | 하는 일 |
|---|---|---|
| `+0x60` | `KiStartUserThread` | SMAP 이 켜져 있으면 `STAC` (`+0x69`) |
| `+0x6c`, `+0x71` | `+0xc`, `+0x11` | `MOV ECX,1` / `MOV CR8,RCX` — **IRQL 을 1 로** |
| `+0x75`, `+0x79` | `+0x15`, `+0x19` | 인자 2 = StartContext, 인자 1 = StartRoutine |
| `+0x7e` | `+0x1e` | `MOV RAX,[RSP+0x10]` — **시작 루틴 주소 꺼내기** |
| `+0x83` (ret `+0x88`) | call `+0x23` / ret `+0x28` | `_guard_dispatch_icall` 거쳐 간접 호출 |

- **CR8** = IRQL 값을 담는 CPU 칸. run 문서는 `MOV CR8,…` 를 "IRQL 을 …로" 라고 읽는다. 비유: [방해 금지 표지판](run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)의 **숫자판 자체**.
- `STAC` 는 SMAP 이라는 CPU 보안 장치와 관계된 명령이다. 이번 이야기 줄기와는 상관없어 넘어간다.
- `[정적]` CFG 검사 함수 `_guard_dispatch_icall` 은 앞부분만 원본 바이트로 읽었다. 검사 뒤 RAX 로 **넘어가는(점프)** 모양이라 콜스택엔 안 남는다. run4 `[관찰]` 콜스택에 안 보인 것과 맞는다.
- `[정적]` 넘기는 인자는 StartRoutine(0)·StartContext(`ETHREAD.StartAddress`) 인데, `PspUserThreadStartup` 은 **둘 다 안 읽는다** (`+0x17 MOV RCX,CR8`, `+0x1b XOR EDX,EDX` 로 덮어씀).

### 2.4 새 스레드는 "불려서" 가 아니라 "교대의 `RET` 로 떨어져서" 출발한다

이번 바퀴에서 제일 낯선 그림이다. 천천히 보자.

**보통 함수는 어떻게 시작하나** (run9 해설 [콜스택](run9-create-user-process-해설.md#콜스택) 복습)

- A 가 `call B` 를 하면, CPU 가 "돌아올 자리" 를 스택에 쌓고 B 로 간다.
- B 가 끝나면 `RET` 가 스택 맨 위 값을 꺼내 그리로 **돌아간다.**
- 즉 `RET` 는 "**스택 맨 위에 적힌 주소로 가라**" 일 뿐이다. 그 주소가 정말 "아까 온 곳" 인지는 따지지 않는다.

**새 스레드는 "아까 온 곳" 이 없다**

- 처음 달리는 스레드다. 아무도 `call` 한 적이 없다.
- 그래서 커널은 새 스레드의 첫 스택을 **"누가 불렀다가 잠깐 쉬는 중" 인 것처럼 미리 꾸며 둔다.**
- 그다음 교대(`SwapContext`)가 그 스택으로 바꿔 끼우고 `RET` 를 하면, CPU 는 꾸며 둔 주소로 "돌아간다." 그곳이 `KiStartUserThread` 다.
- 비유: 처음 출근하는 직원인데, 책상 위 책갈피 더미가 **"점심 먹고 돌아온 사람" 모양으로 미리 꾸며져 있다.** 교대 담당이 "자, 하던 일 계속하세요" 하면, 맨 위 책갈피(`KiStartUserThread`)를 펼쳐 거기서부터 일한다.

**미리 꾸며 두는 사람: `KiInitializeContextThread`** `[정적]`

부모 쪽 `KeInitThread +0x2fc` (ret `+0x301`) 가 부르는 함수다 (flow.md 2.1). 새 커널 스택에 이런 값을 쌓는다.

| 자리 | 쌓는 값 | 나중에 쓰는 곳 |
|---|---|---|
| `+0x1e1 LEA` / **`+0x1e8 MOV [R13+0x38],RAX`** | `KiStartUserThread` 주소 (`0x1406afbd0`) — 스택 맨 위 | `SwapContext` 의 `RET` 가 꺼냄 → **출발 자리** |
| `+0x1c7 MOV RAX,[R13+0x8]` → **`+0x1cf MOV [RBX+0x10],RAX`** | `PspUserThreadStartup` 주소 | `Kx +0x7e` 가 `[RSP+0x10]` 로 꺼냄 → **부를 시작 루틴** |
| `+0x1d3 LEA` / **`+0x1da MOV [RBX+0x138],RAX`** | `KiStartUserThreadReturn` 주소 (`0x1406afb60`) | 아무도 안 씀. 콜스택 **표시용** (→ 2.5) |
| `+0x26f MOV byte [R13+0x28],1` | 바이트 1 | `SwapContext` APC 요청 조건 (→ 2.9) |
| `+0x2ce MOV [RSI+0x58],R13` | `R13` 을 새 `KernelStack` 으로 | `SwapContext +0xf7` 이 이 스택으로 바꿈 |

- `R13 = RBX - 0x40` (`+0x1cb`). 그러니 `R13` 쪽이 스택 맨 위, `RBX` 쪽이 그 조금 아래다.
- `[RBX+0x10]` 칸의 이름은 PDB 타입으로 `_KEXCEPTION_FRAME.P3Home` 이다 (→ 아래 "새 말").

**교대가 출발시키는 순간** `[정적]` (`SwapContext.asm`)

1. `SwapContext +0xf7 MOV RSP,[RSI+0x58]` — 새 스레드의 `KernelStack`(= 위 `R13`)으로 스택을 바꿔 끼운다.
2. 끝 조각 `frag:FUN_1406b686f +0x2ba ADD RSP,0x30` → `+0x2be POP RBP` → **`+0x2bf RET`**.
3. `RET` 가 `[R13+0x38]` 을 꺼낸다 = `KiStartUserThread`. 거기로 뛴다.
4. 이 순간 `RSP = RBX` 다. 그래서 `KiStartUserThread` 가 보는 `[RSP+0x10]` = 위 표 2번째 줄 칸 = `PspUserThreadStartup`.
   - 덤으로 `RBP = [R13+0x30]` = 트랩 프레임 + 0x80 (`KiInitializeContextThread +0x274`, `+0x27b`). 이 `RBP` 는 2.6 에서 다시 나온다.

**그래서 무엇이 설명되나**

- `[정적]` `KiStartUserThread` 를 가리키는 코드 쪽 참조는 `KiInitializeContextThread +0x1e1` **하나뿐**이다. 아무도 `call` 하지 않는다.
- `[정적]` 그래서 `PspUserThreadStartup` 은 `NtCreateUserProcess` 에서 출발한 **호출 지도에 안 닿는다** (sast/05 와 맞음). 지도는 `call` 을 따라 그리는데, 이 길은 `call` 로 이어지지 않기 때문이다.

**"첫 출근 창구" 쪽지가 손에 들리기까지** — run11·run13 `[추측]` → 이번 `[정적]`

| # | 자리 | 하는 일 |
|---|---|---|
| 1 | `PspAllocateThread +0x681` / `+0x688` | 시작 정보 묶음 `+0x8` 칸에 `PspUserThreadStartup` (run11). `+0x6e9` 에서 묶음 주소를 `KeInitThread` 2번째 인자로 |
| 2 | `KeInitThread +0x2f6 MOV RDX,R15`, call `+0x2fc` / ret `+0x301` | 그 묶음을 `KiInitializeContextThread` 2번째 인자로 (거기선 `+0x3c MOV R13,RDX`) |
| 3 | `KiInitializeContextThread +0x1c7` → `+0x1cf` | 새 커널 스택의 `P3Home` 칸에 |
| 4 | `KiInitializeContextThread +0x1e1` / `+0x1e8` | 그 위 스택 맨 위에 `KiStartUserThread` 주소 |
| 5 | `SwapContext +0xf7` … `+0x2bf RET` | 그 스택으로 바꾸고 `KiStartUserThread` 로. 이때 `[RSP+0x10]` = 3번 칸 |

- 비유: run11 에서 인사 카드에 붙인 "첫 출근하면 이 창구부터" 쪽지가 → 관리사무소 서류철(시작 정보 묶음) → 직원 책상 위 정해진 칸(`P3Home`) → 출근 첫 발(`KiStartUserThread`)이 그 칸에서 집어 들고 창구로 간다. 이번엔 **손에서 손으로 넘어가는 다섯 번을 다 봤다.**

**새 말** (이 절과 2.5 에 나온 구조체)

- **`_KEXCEPTION_FRAME`** (크기 `0x140`) = 커널 함수가 "**함수를 건너가도 지켜야 하는 레지스터**"(비휘발 레지스터: `rbx`, `rsi`, `rdi`, `r12~r15`, `xmm6~15`)를 적어 두는 묶음 (칸 이름은 PDB 타입). 맨 끝 `+0x138` 칸 이름이 **`Return`**(돌아올 주소)이다.
  - **비휘발 / 휘발 레지스터** = 함수를 부를 때 "부른 쪽이 믿고 맡겨도 되는 칸"(비휘발)과 "불린 쪽이 마음대로 써도 되는 칸"(휘발) `[추측]` (일반적으로 알려진 호출 규약 설명).
  - 비유: 직원 책상의 **개인 서랍.** 퇴근 전 상태 그대로 다시 꺼내야 하는 물건이 들어 있다.

### 2.5 `KiStartUserThreadReturn` — 실제로 안 도는 "가짜 돌아올 주소"

"**콜스택에 보인다 ≠ 거기서 불렀다**" 의 좋은 예다.

**무엇이 들어 있나** `[정적]` (원본 바이트)

- rva `0x6afb60` 에 딱 두 바이트 **`90 C3` = `NOP; RET`**. 뒤는 `INT3` 채움과 `NOP` 정렬이다.
- 자리는 `KxStartUserThread` 바로 **앞 16바이트**.
- PDB 로는 함수 **`KyStartUserThread`** (rva `0x6af810~0x6afb62`) 의 **끝 두 바이트**다. Ghidra 는 `FUN_1406afa82` 의 끝으로 본다.

**`KyStartUserThread` 는 뭔가**

- `[정적]` 트랩 프레임을 만드는 **진입 코드 모양의 템플릿**이고, 끝(`0x6afb4c`)에서 `CALL KxStartUserThread` 한다.
- `[정적]` 이 템플릿으로 **들어오는 참조는 못 찾았다** (원본 바이트·`.pdata`).
- `[추측]` 쓸모는 **풀어 보기 정보**로 보인다. UNWIND_INFO 가 "기계 프레임 있음 + 프레임 레지스터 `rbp`" 모양이다.
- 비유: 실제로 아무도 걷지 않는 **모형 복도**. 넘기기 안내서(풀어 보기)에 "이 복도 끝에서 왔다면, 그 앞은 유저 쪽" 이라고 적어 두려고 만든 것으로 보인다.

**왜 "가짜" 인가** `[정적]`

- `KiInitializeContextThread +0x1da` 가 이 주소를 `_KEXCEPTION_FRAME.Return` 칸(`+0x138`)에 넣는다 (2.4 표 3번째 줄).
- 그 칸은 `KxStartUserThread` 가 확보한 `0x138` 바이트 바로 위, 즉 **`KxStartUserThread` 자신의 "돌아올 주소" 자리**다.
- 그런데 `KxStartUserThread` 는 `RET` 가 아니라 **`IRETQ` 로 끝난다** (→ 2.6). 그러니 이 주소로 돌아가는 일은 **없다.**
- 이 주소를 쓰는 코드도 위 `LEA` 하나뿐이다.
- 비유: 책갈피 더미 맨 아래에 끼워 둔 **"여기서 왔음" 가짜 책갈피.** 실제로 그 쪽으로 돌아가진 않는다. 책갈피 더미를 거꾸로 읽는 사람(디버거)에게 "**이 아래는 유저 쪽**" 이라고 안내하는 역할이다.

**run4 콜스택 네 줄을 한 줄씩**

| 줄 | 보인 것 | 정체 | 태그 |
|---|---|---|---|
| `00` | `nt!PspUserThreadStartup` | 멈춘 함수 자신 (run4 는 입구에 멈춤점) | `[관찰]` |
| `01` | `nt!KiStartUserThread+0x28` | `Kx +0x83` 의 `call` 이 쌓은 **진짜 돌아올 자리** (`Kx +0x88`) | `[관찰]` + `[정적]` |
| `02` | `nt!KiStartUserThreadReturn` | `KiInitializeContextThread +0x1da` 가 `KxStartUserThread` 프레임 위 칸에 **미리 꽂아 둔 가짜 돌아올 주소** | `[정적]` |
| `03` | `ntdll!RtlUserThreadStart` | `02` 주소가 `KyStartUserThread` 범위 안이라, 그 풀어 보기 규칙(트랩 프레임에서 `Rip`/`Rsp` 읽기)으로 **유저 쪽 자리**로 풀린 것으로 보임 | `[추측]` (관찰과 맞지만 WinDbg 쪽 규칙을 코드로 따라가진 않음) |

- 주의: 시작 루틴(`PspUserThreadStartup`)이 돌아갈 곳은 `02` 가 아니라 **`01`** 이다. `02` 는 그 **한 칸 위**다.
- `03` 이 왜 하필 `RtlUserThreadStart` 인지는 2.7 에서 다시 본다.

### 2.6 커널 → 유저 전환 = `IRETQ`

2.3 의 `[공백]` 이 코드로 채워진 곳이다.

**새 말**

- **트랩 프레임(`_KTRAP_FRAME`, 크기 `0x190`)** = 유저 → 커널로 들어올 때 유저 쪽 레지스터를 적어 두고, 나갈 때 그걸 되살리는 묶음 `[추측]` (일반적으로 알려진 설명). 칸 이름은 PDB 타입 `[정적]`: `Rbp +0x158`, **`Rip +0x168`**, `SegCs +0x170`, `EFlags +0x178`, `Rsp +0x180`, `SegSs +0x188`.
  - 비유: 직원 책상에 붙은 **출발 메모** (flow.md 의 "책상에 붙은 쪽지"). "문 밖으로 나가면 **어디서**, **어떤 상태로** 일을 시작할지" 가 적혀 있다.
  - run11 의 **업무 시작 메모**(CONTEXT)를 커널이 이 출발 메모로 옮겨 적는 것으로 보인다 `[추측]` (→ 2.7).
- **`IRETQ`** = 스택 맨 위 다섯 칸 `RIP, CS, RFLAGS, RSP, SS` 를 **한꺼번에** 꺼내 CPU 에 넣는 명령. 그러면 실행 자리·모드·스택이 **동시에** 바뀐다 (run 문서).
  - `CS` 칸이 유저 값이면 그 순간부터 유저 모드다 `[추측]` (일반적으로 알려진 설명).
  - 비유: flow.md 의 **순간 이동**. 문을 걸어서 나가는 게 아니라, 출발 메모에 적힌 자리로 한 번에 옮겨진다.
- `SYSRETQ` 라는 다른 "유저로 돌아가는" 명령도 있지만, **이 길엔 없다** `[정적]`.

**순서** `[정적]` (자리는 `Kx` 기준)

`PspUserThreadStartup` 이 `Kx +0x88` 로 돌아온 뒤:

| # | 자리 | 하는 일 | 비유 (태그 없음) |
|---|---|---|---|
| 1 | `+0x88 ~ +0xdc` | 예외 프레임에서 비휘발 레지스터(`xmm6~15`, `rbx`, `rdi`, `rsi`, `r12~r15`) 되살리기 | 개인 서랍 정리 |
| 2 | `+0xe0 CLI` | 인터럽트 끄기 | "지금부터 방해 사절" |
| 3 | `+0xe1 TEST byte [RBP+0xf0],1` / `+0xe8 JZ → +0x3e3` | 트랩 프레임 `SegCs` 맨 아래 비트가 0 이면 **커널로 돌아가는 가지**로. 유저 스레드는 CS `0x33` 이라 안 탄다 | "나가는 곳이 문 밖인가, 사무소 안인가" |
| 4 | `+0xee ~ +0xf7` | `KeSmapEnabled` 이면 `STAC` | — |
| 5 | `+0xfa ~ +0x125` 되돌이 | **유저 APC** 가 걸려 있으면(`[스레드+0xc2] & 3`) `KiInitiateUserApc` (call `+0x116` / ret `+0x11b`) 하고 처음으로 | 나가기 전 "유저용 쪽지" 확인 |
| 6 | `+0x127` (ret `+0x12c`) | `KiSynchronizeUserIsolationDomainEntry` 가 0 이 아니면 `+0x132 JNZ` 로 `+0xfa` 에 되돌아감 | — |
| 7 | `+0x13d ~ +0x229` | 조건부 마무리 (디버그 레지스터, CET 그림자 스택, 추측 실행 완화 `WRMSR`, 리턴 예측 버퍼 채우기 등). 목적은 `[추측]` | 보안 점검 몇 가지 |
| 8 | 조각 `FUN_1406afeb5` (Kx `+0x345~`) | 휘발 레지스터(`xmm0~5`, `r11 … rax`)를 트랩 프레임에서 되살리기 | 출발 메모의 나머지 칸 채우기 |
| 9 | 조각 `+0x68` / `+0x6b` / `+0x72` | `RBP` 를 유저 값으로, **`RSP = 트랩 프레임 + 0x168`** | 출발 메모를 "읽을 위치" 에 펼쳐 놓기 |
| 10 | 조각 `+0x79 TEST byte [KiKvaShadow],1` | 아래 (가)/(나) 갈림 | 나가는 문 고르기 |

- 3번의 커널로 돌아가는 가지(`+0x3e3 ~ +0x42c`)도 끝이 `+0x42c IRETQ` 다. 하지만 **유저 스레드 길이 아니다.** CS `0x33`·SS `0x2b` 는 `KiInitializeContextThread +0x163`, `+0x16b` 가 넣는다 `[정적]`.
- 5번이 **유저 APC 만** 본다는 점이 2.9 에서 중요해진다. **커널 APC(멈춤 쪽지)는 여기서 안 본다.**

**9번이 왜 핵심인가**

- `IRETQ` 는 `[RSP]` 부터 다섯 칸을 꺼낸다.
- 9번에서 `RSP` 를 **트랩 프레임 + 0x168** 로 맞췄다.
- 그러니 꺼내는 다섯 칸 = 트랩 프레임의 `Rip(+0x168)`, `SegCs(+0x170)`, `EFlags(+0x178)`, `Rsp(+0x180)`, `SegSs(+0x188)`.
- → **유저 첫 `RIP` = `_KTRAP_FRAME.Rip` (`+0x168`) 칸** `[정적]`.

**유저로 나가는 문은 둘 중 하나** `[정적]` — `KiKvaShadow` 값에 따라

- **`KiKvaShadow`** = PDB 공개 심볼 이름의 전역 값. run 문서는 "CPU 보안 완화 설정" 이라고 적었다. 커널 주소 공간을 유저에게서 가리는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

| | (가) bit0 = 0 | (나) bit0 = 1 |
|---|---|---|
| 길 | 조각 `+0x99 SWAPGS` → **조각 `+0x9c IRETQ`** | 조각 `+0x82` (Kx `+0x3c7`) **`JMP KiKernelExit`** (꼬리 점프) |
| `IRETQ` 자리 | `0x1406aff51` = **Kx `+0x3e1`** (= `KiStartUserThread+0x381` `[추측: 표기]`) | **`KiKernelExit +0xa5`** (`0x140bcac25`) |
| 그 앞에 | — | 다섯 칸을 CPU 별 임시 스택(`GS:[0x38]+0x4200`)으로 옮김 (`+0x2e ~ +0x3a PUSH`), 조건부 `+0x83 MOV CR3,RSP` (유저 주소 공간으로), `+0xa2 SWAPGS` |

- **꼬리 점프(tail JMP)** = 함수 끝에서 `call` 대신 `JMP` 로 다른 함수에 넘어가는 것. 돌아오지 않으니 콜스택에 안 쌓인다. flow.md 범례에서 이번에 굵은 화살표 종류로 추가됐다.
- (나) 도 **같은 다섯 칸을 그대로 옮겨 쓴다.** 그러니 어느 문이든 유저 첫 `RIP` 는 트랩 프레임 `Rip` 칸 값이다.
- 어느 쪽인지는 정적으로 못 정한다 → 동적 (Q42, `dd nt!KiKvaShadow L1`).
- 비유: 문이 둘. 하나는 그냥 나가는 문(가), 하나는 **보안 검색대를 지나** 나가는 문(나). 검색대에서 짐(다섯 칸)을 다른 바구니로 옮겨 담고, 건물 지도(CR3)를 유저 쪽으로 바꿔 준 뒤 내보낸다. 나가서 서는 자리는 같다.

**트랩 프레임은 어디 있나** `[정적]`

- **`KTHREAD.InitialStack - 0x190`** — `KiInitializeContextThread +0x1b5 LEA R11,[R15-0x190]`, `+0x2ca MOV [RSI+0x28],R15` (`InitialStack` = `_KTHREAD +0x28`, PDB 타입).
- 시작 루틴이 도는 동안은 **`RBP - 0x80`** 이다 (2.4 의 `RBP` = 트랩 프레임 + 0x80).

### 2.7 그 `Rip` 칸에 누가 `LdrInitializeThunk` 를 넣나 — 다음 Q7

이번 바퀴는 "**어느 칸에서 꺼내나**" 까지다. "**누가 그 칸을 채우나**" 는 이름만 짚었다.

- 처음 채우는 쪽: `KiInitializeContextThread +0x1ac` call / ret `+0x1b1` **`KxContextToKframes`**. 유저 CONTEXT 를 트랩 프레임·예외 프레임으로 옮기는 쪽으로 보인다 `[추측]` (이름·인자 모양만, 본문 안 뽑음).
- 다시 정하는 쪽: **`PspUserThreadStartup +0xb3` `PspInitializeThunkContext`** (Q7 에서 본문).

**왜 이게 run4 의 수수께끼와 이어지나** — flow.md 2.3 이 적은 짐작, 한 걸음씩:

1. `[관찰]` (run4) 커널 콜스택 맨 아래 유저 칸엔 `RtlUserThreadStart` 가 보였다. 그런데 유저에서 처음 멈춘 곳은 `LdrInitializeThunk` 이고 부른 곳이 없었다.
2. `[정적]` 유저 첫 `RIP` 는 트랩 프레임 `Rip` 칸에서 꺼낸다 (2.6). 콜스택 `03` 줄도 같은 트랩 프레임 `Rip`/`Rsp` 로 풀린 것으로 보인다 `[추측]` (2.5).
3. `[관찰]` run4 는 `PspUserThreadStartup` **입구**에 멈춤점을 걸었다 (run4 "방법"). 그러니 그 콜스택은 `+0xb3` `PspInitializeThunkContext` 보다 **앞**의 모습이다.
4. 그래서 짐작 `[추측]`: 트랩 프레임 `Rip` 는 처음엔 CONTEXT 값(`RtlUserThreadStart` 쪽)이고, `PspInitializeThunkContext` 가 이를 `LdrInitializeThunk` 로 **바꾼 뒤** `IRETQ` 로 간다.
   - 그러면 `LdrInitializeThunk` 가 "부른 곳 없이" 보이는 것도, run4 콜스택의 유저 칸에 **바뀌기 전 값**이 보인 것도 설명된다.
5. 아직 확인 아님: Q7(정적, `PspInitializeThunkContext` 본문), Q42(동적, `+0xae`/`+0xb3` 앞뒤로 `Rip` 칸 비교).

- 비유: 업무 시작 메모(CONTEXT)엔 "본 업무 창구(`RtlUserThreadStart`)" 가 적혀 있었다. 그런데 출근 도장 마지막 단계에서 누군가 출발 메모를 "**먼저 신입 교육실(`LdrInitializeThunk`)부터**" 로 고쳐 쓰는 것 같다. 누가 어떻게 고쳐 쓰는지가 Q7 이다.

### 2.8 `PspUserThreadStartup` 안 — 출근 도장 다섯 칸

**정상 길** `[정적: 조건 읽음, 값은 동적]` — 점프를 따라 본 순서, ret 오프셋

| # | 자리 | 하는 일 | 조건 |
|---|---|---|---|
| 0 | `+0x22 MOV CR8,RDI` (`RDI` = 0) | **IRQL 0 으로** (앞 `+0x1d` 는 `KiIrqlFlags` ≠ 0 일 때만 `KiLowerIrqlProcessIrqlFlags`) | 늘 |
| 1 | `+0x3c` | `PspDisablePrimaryTokenExchange(스레드)` | 늘 |
| 2 | `+0x78` | `PsGetSessionLocaleId` → `TEB.CurrentLocale`(`+0x108`)에 씀 | `Minimal` = 0 |
| 3 | `+0x99` | `KeUpdateTebSchedulingPropertiesCurrentThread(스레드)` | `Minimal` = 0 |
| 4 | `+0xa1` | `PspNotifyThreadCreation(스레드)` | `Minimal` = 0 |
| 5 | **`+0xb3`** | **`PspInitializeThunkContext()`** — 인자 레지스터를 새로 안 채움 | `Terminated` = 0 **그리고** `Minimal` = 0 |
| 6 | `+0xb3 ~ +0xc0` | 복원, `RET` → `KiStartUserThread+0x28` | |

- 함수 이름들이 **무슨 일을 하는지**는 이름으로 본 것이다 `[추측]` (본문은 안 뽑음, flow.md).
- 2번의 TEB 쓰기는 SEH `try` 안이다. 처리기 `+0x80` 은 `.asm` 에 빠져서 원본 바이트로 읽었다.
- 비유: 출근 도장 찍는 창구에서 하는 일. "방해 금지" 표지판 내리기 → 신분증 바꿔치기 금지 → 언어 설정 → 근무 성향 메모 → "새 직원 왔어요" 알림 → **출발 메모 고쳐 쓰기**. (가운데 넷은 이름에서 짐작한 비유다.)

**조건 칸 이름** (PDB 타입)

- `Minimal` = 프로세스 `_EPROCESS.Flags3` (`+0x5fc`) bit0.
- `Terminated` = 스레드 `_ETHREAD.CrossThreadFlags` (`+0x5a0`) bit0. `ThreadInserted` = 같은 칸 bit1.

**정상이 아닌 갈래 세 개** `[정적]` — test.exe 스레드는 안 탈 것 `[추측]`, 값은 동적

- `+0xca` `DbgkCreateMinimalThread` ← 프로세스가 **Minimal** 일 때. 2~5 를 모두 건너뛴다.
- `+0xe3` `KeWaitForSingleObject(스레드, UserRequest(6), UserMode, 알림 불가, 시간제한 없음)` ← 이미 "**끝내라**" 표시(`Terminated`)를 받은 채 여기까지 왔을 때. 5 를 건너뛴다.
  - 장부엔 `alias:KeWaitForMutexObject+1` 로 찍힌다. PDB 공개 이름은 `KeWaitForSingleObject` 하나 ([run13 해설 2.10](run13-resume-thread-해설.md#210-이름-하나--kewaitformutexobject-로-찍혔지만-kewaitforsingleobject) 과 같은 얘기).
- `+0xf5` `PspTerminateThreadByPointer(스레드, 0xC000004B, TRUE)` ← `ThreadInserted` = 0, 즉 프로세스에 **끝까지 못 들어간** 스레드. 자기 자신이면 돌아오지 않는 호출로 보인다 `[추측]`.

**`PspInitializeThunkContext` 는 첫 스레드만인가? — 아니다** `[정적]`

- 이 함수(`PspUserThreadStartup`) 안에 "**첫 스레드인지**" 가르는 줄이 **없다.**
- 조건은 `Terminated` = 0, `Minimal` = 0 둘뿐. 만족하는 **모든 스레드**가 부른다.
- run4 `[관찰]` "스레드 4개 모두 `LdrInitializeThunk` 에서 출발" 과 모양이 같다.
- 첫 스레드만 다른 일이 있다면 `PspInitializeThunkContext` **안**일 것이다 (Q7).

### 2.9 run13 의 "쪽지는 `+0x22` 직후 펼쳐진다" — 고리 3개가 굳었다

[run13 해설 2.6](run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리) 에서 이렇게 짐작했다: 멈춘 첫 직원은 `PspUserThreadStartup +0x22` 에서 "방해 금지" 표지판을 0 으로 내리자마자, 예약된 초인종이 울려 멈춤 쪽지를 펼치고 빨간불 앞에서 잔다.

그때 걸려 있던 의문은 둘이었다.

- (ㄱ) 초인종을 예약하는 `SwapContext` 조건의 "**CL 인자**" 가 뭔지 몰랐다.
- (ㄴ) 정말 `+0x22` **그 자리**에서 들어오는지 몰랐다.

이번에 한 덩어리였던 추측이 **고리 3개 `[정적]` + 마지막 1개 `[추측]`** 로 나뉘었다.

**고리 1 `[정적]` — 출발할 때 표지판은 1 이다**

- `Kx +0x6c MOV ECX,1`, `+0x71 MOV CR8,RCX`. `KiStartUserThread` 는 IRQL **1** 로 시작 루틴을 부른다.
- 1 이면 "쪽지는 나중에" 다. 초인종이 예약돼 있어도 아직 안 울린다.
- (run13 도 원본 바이트로 본 내용이다. 이번엔 `.asm` 으로 같은 걸 다시 봤다.)

**고리 2 `[정적]` — 표지판을 1 → 0 으로 내리는 자리는 `+0x22` 하나뿐**

- `+0x22` 앞에 `+0x1d` `KiLowerIrqlProcessIrqlFlags` 호출이 있다. "Lower Irql" 이라는 이름이라 "여기서 먼저 내리나?" 싶다.
- 그런데 이 함수는 **옛 IRQL 이 2 이상일 때만** 일한다 (`KiLowerIrqlProcessIrqlFlags +0x1d CMP CL,2` / `JC → 끝`).
- 지금 옛 IRQL 은 고리 1 대로 **1** 이다. 그러니 **아무것도 안 하고 돌아온다.**
- → `+0x22 MOV CR8,RDI` 가 1 → 0 의 **유일한 자리**다.

**고리 3 `[정적]` — 초인종 예약 조건은 새 스레드에 늘 맞는다** (의문 (ㄱ) 해결)

- `SwapContext` 끝 조각 (`frag:FUN_1406b686f`):
  - `+0x292 CMP [RSI+0xc1],1` — 새 스레드의 `KernelApcPending`(= run13 의 "쪽지 있음" 빨간 깃발) 이 1 인가.
  - `+0x29b MOVZX AX,byte [RSP+0x28]`, `+0x2a1 OR` — **스택 `[RSP+0x28]` 바이트** OR `[스레드+0x1e6]` 이 0 이 아닌가.
  - 둘 다 맞으면 call `+0x2af` `HalRequestSoftwareInterrupt(1)` — APC 단계 **초인종 예약**.
- 새 스레드는 이때 `RSP` 가 **새 스택**이다. 그 `[RSP+0x28]` 바이트는 `KiInitializeContextThread +0x26f` 가 넣은 **1** 이다 (2.4 표).
- → 부모가 멈춤 쪽지를 걸어 둔(깃발 1) 새 스레드는 **처음 CPU 에 오를 때 초인종을 반드시 예약하고 시작한다.**
- run13 이 "CL 인자" 라고 적은 게 바로 이 스택 바이트였다 (→ 2.10 정정).

**마지막 고리 `[추측]` — 정말 `+0x26` 에서 울리나** (의문 (ㄴ), 남음)

- 근거: CR8 을 내리면 걸려 있던 인터럽트가 **다음 명령 앞에서** 들어온다는 CPU 규칙. 그러면 `+0x22` 다음 명령 `+0x26` 앞이다.
- 코드만으론 "실제로 그 순간 들어왔다" 를 못 정한다. → 동적 (Q42·Q19: `nt!KiSchedulerApc` 에서 `k` 로 맨 아래가 `PspUserThreadStartup+0x26` 근처인지).
- `[추측]` `+0x26` 이후 다른 호출 사이에서 잠들 수도 있다 (예: `+0x37` 이 락을 풀 때).

**반대 증거는 없다** `[정적]`

- 유저로 나가는 길(2.6 표 5번)은 **유저 APC 만** 본다. 커널 APC(멈춤 쪽지)는 안 본다.
- 그러니 멈춤 쪽지가 펼쳐질 **첫 기회**는 `PspUserThreadStartup` 안에서 IRQL 이 0 이 되는 순간이다.

**전과 후를 한 줄씩**

| | run13 | run15 |
|---|---|---|
| 출발 IRQL 1 | `[정적]` (원본 바이트) | `[정적]` (`.asm` 으로 다시 확인) |
| `+0x22` 가 유일한 내림 자리 | 확인 안 함 | **`[정적]`** (앞 호출이 아무것도 안 함) |
| 초인종 예약 조건 | "CL 인자" 정체 모름 | **`[정적]`** 스택 바이트, 새 스레드는 늘 1 |
| 실제로 `+0x26` 에서 들어오나 | `[추측]` | `[추측]` (남은 하나) |

- 비유 (태그 없음): 직원이 "방해 금지 1" 표지판을 단 채 출근 도장 창구에 온다. 교대 담당은 출근시킬 때 이미 초인종을 예약해 뒀다(깃발이 있으니까, 그리고 새 직원의 그 칸은 늘 1 이니까). 창구 첫 일이 표지판을 0 으로 내리는 것이고, 그 전에 표지판을 건드리는 사람은 없다. 그러니 **초인종이 울릴 수 있는 첫 순간은 바로 그때**다. 정말 그때 울렸는지만 아직 안 봤다.
- 읽을 때 주의: flow.md 2.2 는 같은 세 고리를 "**고리 3·4·5**" 라고 부른다. flow.md 쪽 목록 번호(1 깃발 → 2 대기 줄 → 3 초인종 예약 → 4 IRQL 1 → 5 `+0x22`)를 따른 것이다. run 문서의 "고리 1·2·3" 과 **같은 내용**이고 번호만 다르다.

### 2.10 run13 정정 두 건

run 문서 "정정" 절이다. run13 문서 맨 위에도 한 줄이 들어갔다.

**정정 1 — "`KiStartUserThread` 는 Ghidra 장부에 없다"**

- run13 이 쓴 것: "`KiStartUserThread` 는 Ghidra 가 함수로 안 잡아서 `ms/decomp`·`ms/disassemble` 에 없다."
- 고친 것: **이름표만 함수가 아니다. 코드는 Ghidra 함수 `KxStartUserThread` 안 `+0x60` 에 있다** (이번에 `KxStartUserThread.asm` 으로 뽑힘).
- 바뀌지 않은 것: run13 이 원본 바이트로 읽은 내용(IRQL 1, call `+0x23` / ret `+0x28`)은 이번 `.asm` 과 **같다** (`Kx +0x83` / `+0x88`).
- 왜 생겼나: run 문서가 원인을 따로 적진 않았다. 근거로 보이는 것만 적으면:
  - `plan.md` Q6 줄: "`_index.tsv` 에 `KiStartUserThread` 없음, `KxStartUserThread` 있음 (PDB 엔 `KiStartUserThread`·`KiStartUserThreadReturn` 이름 있음) → 이름 대응 확인".
  - 즉 이름 `KiStartUserThread` 로 장부를 찾으면 안 나온다. 코드는 **다른 이름(`KxStartUserThread`)의 함수 한가운데**에 있었다.
  - 이번 바퀴 시작 때 `pdb_aliases.py --where` 로 PDB 종류(none)와 자리를 확인하면서 풀렸다 (요청서).

**정정 2 — `SwapContext` APC 요청 조건의 "CL 인자"**

- run13 이 쓴 것: 조건을 "(인자 CL | `[스레드+0x1e6]`)" 로 적고, CL 이 새 스레드의 `WaitIrql` 인지 못 정했다.
- 고친 것: `.asm` 으로 보면 그 줄은 **스택 `[RSP+0x28]` 바이트**를 읽는다 (`+0x29b MOVZX AX,byte [RSP+0x28]`). 새 스레드에선 `KiInitializeContextThread +0x26f` 가 넣은 **1** 이다 `[정적]`.
- 그래서 "CL 이 `WaitIrql` 인지" 라는 질문은 **필요 없어졌다.**
- 왜 생겼나: run 문서가 원인을 따로 적진 않았다. 이번에 `SwapContext.asm` 의 그 줄을 다시 읽어 보니 레지스터 CL 이 아니라 스택 칸이었다는 것까지가 근거다.

### 2.11 그 밖에 확인된 것

- `[정적]` `PspUserThreadStartup` 호출 9줄이 요청서가 적은 9줄과 같고, `NtCreateUserProcess +0xc36`·`PspInsertThread +0x807` 도 장부와 같다 → 버전 일치.
- `[추측]` `IRETQ` 줄 자체(`Kx +0x3e1`, `KiKernelExit +0xa5`)에는 멈춤점을 **걸지 않는 편이 좋다.** 바로 앞 `SWAPGS` 뒤라 GS 가 유저 값이고, `KiKernelExit` 는 CR3 도 바꾼 뒤다. 유저 진입 값은 `KiStartUserThread+0x28` 에서 `dq @rbp+0xe8 L5` 로 읽으면 된다 (run 문서).
- 결과서 "스크립트 문제" 에 제안 5개가 있다 (`.asm` 의 읽지 않은 틈 표시, 리턴 예측 버퍼 덩어리 표시, 조각 오프셋 환산 등). `.claude/` 는 안 고쳤고 제안만이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만 적는다.

| 자리 | 전 (run14 뒤) | 후 (run15) |
|---|---|---|
| 기준 | 정적 근거 run9 ~ run14 | **run9 ~ run15**. 원본 바이트(`objdump`)·PDB 타입(`llvm-pdbutil`)으로 읽은 곳은 글에 밝힌다는 줄 추가 |
| 두 그림 사이 설명 | 멈춰 두기·풀어 주기·KERNELBASE | 한 문단 추가: 그림 2 안의 커널 → 유저 전환도 **코드로 봤다** (`IRETQ`, 두 갈래는 `KiKvaShadow`) |
| 범례 | 굵은 화살표 = 커널 → 유저, 시스템 콜, 인터럽트 | **+ 돌아오지 않는 꼬리 점프 `JMP`**. 같은 번호가 둘이면 **값에 따라 둘 중 하나만** |
| 그림 2 `nt` 상자 | `KiStartUserThread` → `PspUserThreadStartup`. `PspUserThreadStartup` ==> "(2) 커널 → 유저 전환 (공백)" | `PspUserThreadStartup` 아래 정적 상자 5개 (1)~(5). **`KiStartUserThread` 에서** `(2) IRETQ` (꺼짐) / `(2) JMP` → 새 상자 `KiKernelExit` ==> `IRETQ +0xa5` → 유저. APC 화살표 라벨: "멈춘 스레드만, 호출 (1) 앞 (+0x22 직후, 자리 +0x26 은 추측)" |
| 2.1 `KeInitThread` | `[추측]` `+0x8` 칸이 첫 스택까지 가는 길, 한 줄씩 안 따라감 | **`[정적]`** 세 줄로 이음 (`+0x2fc` → `+0x1c7`/`+0x1cf` `P3Home` → `[RSP+0x10]`) |
| 2.2 서는 곳 트리 | `+0x22` IRQL 0 → APC `[추측]` 배달 시점 | IRQL 1 로 들어옴, `+0x22` 가 **유일한 자리**, `[추측]` 은 **배달 자리 `+0x26`** 으로 좁아짐 |
| 2.2 맞물림 3·4·5번 | CL 인자 정체 모름 | 3번: 조건 = 스택 바이트, 새 스레드는 늘 맞음 `[정적]` (정정). 5번: 앞 호출은 아무것도 안 함 `[정적]` |
| 2.2 배달 시점 | `[추측]` 한 덩어리 + 못 정한 것 2개 | 고리 3개 `[정적]`, 남은 건 "`+0x26` 에서 들어오나" 하나 `[추측]`. 반대 증거 없음. CL 질문은 (해결됨) |
| 2.3 제목 | 스레드 출발 | 스레드 출발 **~ 유저로 넘어가기** |
| 2.3 앞머리 | run4 콜스택만 | **코드로 본 흐름 트리** + 비유(출근 도장, 순간 이동) |
| 2.3 `KiStartUserThread` | `[정적]` (run13 원본 바이트) "장부에 없다" | **`KxStartUserThread +0x60` 이름표** (정정), `SwapContext` 의 `RET` 로 들어옴, 앞머리 `+0x0~+0x5f` 는 풀어 보기용 `[정적]+[추측]` |
| 2.3 새 절 `KiStartUserThreadReturn` | — | `NOP; RET`, 가짜 돌아올 주소 `[정적]`, `RtlUserThreadStart` 로 풀린 이유 `[추측]` |
| 2.3 `PspUserThreadStartup` | 첫 일 IRQL 0, 세 호출 자리, 멈추면 잠듦 `[추측]` | 정상 길 **5호출 순서**와 조건, 갈래 3개, **`PspInitializeThunkContext` 는 모든 스레드** `[정적]`. 멈춤은 `[정적]+[추측]` |
| 2.3 커널 → 유저 전환 | **`[공백]`** "전환 방법 자체는 관찰하지 않았다" | **`[정적]`** `IRETQ` 여섯 걸음, 두 문, 첫 `RIP` = `_KTRAP_FRAME.Rip`, 트랩 프레임 = `InitialStack - 0x190`, 채우는 쪽 이름 둘. 실행은 Q42 |
| 2.4 `LdrInitializeThunk` | `[관찰]` 부른 곳 없음 | `[정적]` 줄 추가: 커널에서 여기로 오는 명령은 `IRETQ` |
| 4장 공백 표 | 2.2 배달 시점, 2.3 전환 방법, `+0x8` 칸 길 | 해결 4줄 (전환 방법, `+0x8` 칸 길, `KiStartUserThreadReturn`, `PspInitializeThunkContext` 첫 스레드만?). 일부 해결 2줄 (2.2 배달 시점 — CL 정체는 해결, 2.3 첫 스택 줄에 이름표·`RET` 보강). 새 줄: 2.2 "`+0x26` 에서 들어오나", 2.3 실제 `IRETQ`·값, `RtlUserThreadStart` 관계(Q7), 정상 길 4호출 본문, 유저 복귀 길 조건부 일 |
| 4.1 | — | 3줄 추가 (5호출, `IRETQ` 두 갈래, `KiLowerIrqlProcessIrqlFlags`·`KiInitiateUserApc`). APC 줄 근거 보강 |
| 5장 | run14 까지 | run15 줄, 요청서 번호 표에 run15 줄 |

태그로 보면 핵심 변화는 네 가지다:

- **2.3 커널 → 유저 전환: `[공백]` → `[정적]`** (`IRETQ`, 첫 `RIP` 칸). 실행으로는 아직 0.
- `PspUserThreadStartup` 주소가 첫 스택까지 가는 길: `[추측]` → **`[정적]`**.
- 콜스택의 `KiStartUserThreadReturn`: 이름만 → **`[정적]`** 정체 (가짜 돌아올 주소).
- 멈춤 쪽지 배달 시점: `[추측]` 한 덩어리 → **`[정적]` 고리 3개 + `[추측]` 하나**.

### 큰 그림에서 어떻게 이어지나

- **그림 2 의 커널 쪽이 코드로 끝까지 이어졌다.**
  - 교대(`SwapContext`) → `KiStartUserThread` → `PspUserThreadStartup` → `IRETQ` → 유저.
  - 그림 2 에서 커널과 유저 사이의 `(공백)` 화살표가 사라지고, `[정적]` 굵은 화살표 두 갈래가 됐다.
- **run4 의 수수께끼 두 개 중 하나가 풀렸다.**
  - 풀림: 콜스택 `01`·`02` 줄이 무엇인지 (진짜 돌아올 자리 / 가짜 돌아올 주소).
  - 남음: 유저 첫 자리가 왜 `RtlUserThreadStart` 가 아니라 `LdrInitializeThunk` 인지. "꺼내는 칸" 은 찾았고, "그 칸을 고쳐 쓰는 손" 은 아직이다.
- **다음 구간** (`plan.md`: 다음 항목 **Q7**):
  - Q7 — 2.3 유저 첫 위치가 `ntdll!LdrInitializeThunk` 로 정해지는 곳, `RtlUserThreadStart` 와의 관계. 이번 결과로 **볼 함수가 좁혀졌다**: `PspUserThreadStartup +0xb3` 의 `PspInitializeThunkContext` 본문 (그리고 처음 채우는 `KxContextToKframes`, 후보 `PspCreateUserContext`).
  - Q42 — 이번 그림 전체를 디버거로 (아래 4절). 결과서는 Q7 과 같이 요청서를 쓰면 한 번에 볼 수 있다고 적었다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 트랩 프레임 `Rip` 칸에 `LdrInitializeThunk` 를 넣는 게 정말 `PspInitializeThunkContext` 인가. 첫 스레드만 다른 일이 있나 | 이번엔 부르는 조건만 봤다. 본문은 안 뽑았다 | **Q7** (정적) |
| `KxContextToKframes` 가 정말 유저 CONTEXT → 트랩 프레임인가 `[추측]` | 이름·인자 모양만 봤다 | Q7 출처에 적혀 있음 (정적), flow.md 4장 공백 줄 |
| 유저로 가는 `IRETQ` 가 (가) `Kx +0x3e1` 인가 (나) `KiKernelExit +0xa5` 인가 | `KiKvaShadow` 값에 달렸다. 실행 때 값이다 | **Q42** (동적): `dd nt!KiKvaShadow L1` |
| `KiIrqlFlags` 값 — `+0x1d` 호출이 실제로 들어가나 (들어가도 효과 없음) | 실행 때 값이다 | **Q42** (동적): `dd nt!KiIrqlFlags L1` |
| 멈춤 쪽지가 정말 `PspUserThreadStartup+0x26` 에서 펼쳐지나 | 인터럽트가 실제로 들어오는 자리는 정적으로 못 정한다 | **Q42**·**Q19** (동적): `nt!KiSchedulerApc` 입구에서 `k` |
| `PspInitializeThunkContext` 앞뒤로 트랩 프레임 `Rip`/`Rsp` 가 어떻게 바뀌나. 시작 루틴 동안 `RBP - 0x80` = `InitialStack - 0x190` 인가 | 실행 때 값이다 | **Q42** (동적): `+0xae`/`+0xb3` 에서 `dq (poi(@$thread+0x28)-0x190)+0x168 L5` |
| 유저 진입 직전 값 (`Rip, CS, EFlags, Rsp, SS`) | 실행 때 값이다 | **Q42** (동적): `KiStartUserThread+0x28` 에서 `dq @rbp+0xe8 L5` |
| 유저 APC 되돌이에서 `KiInitiateUserApc` 가 첫 스레드에 불리나 | 코드상 조건부다 | **Q42** (동적, 선택). 본문은 안 뽑음 (대기열에 따로 줄은 없다) |
| `ThreadInserted`·`Terminated`·`Minimal` 실제 값 (정상이면 1, 0, 0) | 실행 때 값이다 | **Q42** (동적) |
| `KyStartUserThread` 템플릿이 정말 안 도나 | 들어오는 참조를 못 찾았을 뿐, 계산해서 만드는 주소는 못 잡는다 | run 문서는 Q42 선택 항목으로 적었다 (plan.md Q42 줄에는 안 적혀 있다). flow.md 4장 공백 줄 |
| `KiStartUserThreadReturn` 다음 칸이 `RtlUserThreadStart` 로 풀리는 WinDbg 쪽 규칙 `[추측]` | WinDbg 의 풀어 보기를 코드로 따라가진 않았다 | flow.md 4장 공백 줄 (대기열에 따로 줄은 없다) |
| 정상 길 4호출(`PspDisablePrimaryTokenExchange`, `PsGetSessionLocaleId`, `KeUpdateTebSchedulingPropertiesCurrentThread`, `PspNotifyThreadCreation`)이 하는 일 | 이름으로만 봤다 `[추측]` | flow.md 4장 공백 줄 (대기열에 따로 줄은 없다) |
| 유저 복귀 길 `Kx +0x13d ~ +0x229` 마무리 중 무엇이 실제로 도나, 그 목적 | CPU 플래그로 갈리는 조건부, 목적은 `[추측]` | **Q42** (동적), flow.md 4장 공백 줄 |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run15-user-thread-startup.md`](run15-user-thread-startup.md)
- 정적 요청서: [`_workspace/sast/10_user-thread-startup.md`](../../_workspace/sast/10_user-thread-startup.md)
- 정적 결과서 (원본 바이트로 읽은 곳과 출처, PDB 자료형 표, 질문별 답, 4-1 고리, 동적 확인 목록·추천 멈춤점, 스크립트 제안 5개): [`_workspace/sast/10_user-thread-startup_result.md`](../../_workspace/sast/10_user-thread-startup_result.md)
- 바퀴 전 관찰: [`runs/01_sprint/run4-thread-start.md`](../01_sprint/run4-thread-start.md) (콜스택 네 줄, `LdrInitializeThunk` 부른 곳 없음)
- run13 문서 맨 위 정정 한 줄: [`run13-resume-thread.md`](run13-resume-thread.md)
- 앞 바퀴 해설:
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — "콜스택", "부르는 자리와 돌아올 자리", "직접 호출과 간접 호출"
  - [`run11-allocate-thread-해설.md`](run11-allocate-thread-해설.md) — 2.5 `PspUserThreadStartup` 주소 건네기, 2.6 CONTEXT
  - [`run13-resume-thread-해설.md`](run13-resume-thread-해설.md) — 2.6 추측의 고리 (IRQL, `SwapContext`, 초인종), 2.12 `KiStartUserThread +0x28`
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - 범례 (굵은 화살표에 꼬리 점프, 같은 번호 둘)
  - "그림 2" 의 `nt` 상자 (`PspUserThreadStartup` 아래 5상자, `KiKernelExit`, `IRETQ` 두 갈래)
  - "2.1" 의 `KeInitThread` 절 (`+0x8` 칸 길)
  - "2.2" 의 "멈춘 첫 스레드가 서는 곳", "run12 `KeReadyThread` 와 맞물림"
  - "2.3" 전체 — 코드로 본 흐름 트리, `KiStartUserThread`, `KiStartUserThreadReturn`, `PspUserThreadStartup`, 커널 → 유저 전환
  - "4. 공백 목록", "4.1 추측·정적으로 둔 호출 관계"
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q6, Q7, Q19, Q42

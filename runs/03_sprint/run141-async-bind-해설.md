# run141 async-bind — 해설

> 이 글은 [run141 문서](run141-async-bind.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q267** — PE_emulator 요청 "비동기 epmapper 연결이 성공한 뒤 rpcrt4 는 bind 를 무엇으로 · 어느 스레드에서 보내나" (`_workspace/plan.md`, 이번에 **끝 (run141)**)
  - 원 요청 `../PE_emulator/_workspace/00_planner/AS02_internals-async-bind.md`, 튜닝 `335_tune15.md` V3 순위 1, 탓 220 (요청 쪽 에뮬레이터 숫자).
  - 사용자 결정 2026-11-25 (PE_emulator D154) 요청 넷의 **첫째**. 정적만.
  - 결과서 판정 **분석성공** (질문 1 ~ 5 모두 정적으로 닫음, `[추측]` / `[공백]` 다섯 남김).
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q271** (결과서 동적 멈춤점 1 ~ 8 — Q258 세션과 합침)
    - 정적 → **Q272** (결과서 다음 요청 후보 1 ~ 4)
    - 덤으로 **Q259 ③ 이 일부 닫혔다** (종류 3 의 뜻 · `0x102` 를 정하는 칸).
- 근거:
  - run 문서 [`run141-async-bind.md`](run141-async-bind.md) (가장 중요)
  - 요청서 [`_workspace/sast/138_async-bind.md`](../../_workspace/sast/138_async-bind.md) · 결과서 [`138_async-bind_result.md`](../../_workspace/sast/138_async-bind_result.md)
    - **처음 analyze 없음.** rpcrt4 · ntdll 은 `^이름$` export 만, ntoskrnl 은 있던 `.asm` 읽기만.
    - kernelbase · kernel32 의 `TrySubmitThreadpoolCallback` 은 같은 시간에 sast/140 이 kernelbase 를 쓰는 중이라 **바이트만** 읽었다 (파일로 안 남김).
    - 스크립트 문제 없음. 핵심 call · jmp · jcc 34개를 PE 바이트와 맞춰 봄 — **전부 일치**.
  - flow.md 변경: docs 커밋 `32795e6` (`git show 32795e6 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 길을 안 탄다.**
   - 초기화 끝 (P2) 모듈 6개에 rpcrt4 · winnsi 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 · run133 · run137 과 같은 줄).
   - `sample/*.c` 에 RPC API 가 없다 `[소스]`.
   - 다만 이번에 읽은 ntdll `TppWorkPost` · `TppWorkerThread` · 커널 일꾼 공장은 test.exe **로더 풀과 같은 몸** 이다 (run72 · run125).
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - "seq 2191", "일꾼 `0x100c`" 같은 것은 모두 PE_emulator 로그다. `[관찰]` 이 아니다. 이 글에서는 "비교용" 으로만 쓴다.
3. **오프셋 읽는 법** (run133 · run137 해설과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문 "`+0x515 Connect`" 처럼 부르는 이름 **앞** 숫자 = **call 자리**.
   - "`+0x51e JNZ → +0x266`" 처럼 함수 안 줄과 그 점프가 가는 곳.
   - ★ `Bind__74b50 +0x217 ~ +0x212` 처럼 **뒤 숫자가 더 작은** 범위가 나온다. 줄이 점프로 앞쪽에 갔다 오는 길 (`+0x217 → +0x21a → +0x1a4 → +0x1a6 → +0x20b → +0x210`) 을 한 덩어리로 적은 것이다.
   - 콜스택으로 본 적은 없다.
4. **`Bind` 라는 이름이 넷이다.** rpcrt4 는 같은 이름 함수가 여럿이라 `.asm` 이 `Bind__<rva>` 로 갈린다. 이 글에서 헷갈리면 안 되는 것:

   | 이름 | 무엇 | 누가 부르나 |
   |---|---|---|
   | `Bind__74b50` | `LRPC_FAST_BINDING_HANDLE::Bind` — 연락 카드 쪽 입구 | 부른 쪽 (winnsi 주 스레드) |
   | `Bind__93cc0` | `LRPC_FAST_BIND_CCALL` 가상 Bind — 호출 쪽 | 일꾼 |
   | `BaseBind` | `LRPC_BIND_CCALL::BaseBind` (rva `0x4c660`) | 위 둘 다 이걸 부른다 |
   | `Bind__4e330` | `LRPC_CASSOCIATION::Bind` — **진짜 bind syscall** 이 있는 곳 (`+0x626`) | `BaseBind +0x1e4` |

   - `CompleteOnePart` 도 둘이다. 이번 것은 **연결 객체 쪽** (rva `0x3b148`). run129 의 바인딩 쪽 (`0x5aeec`) 이 아니다.
5. **값은 코드, 이름은 짐작인 곳이 많다.**
   - 칸 이름 (`[바인딩+0x114]` = 바인딩 상태, `[호출+0x2d0]` 짝, 풀 `[+8]` 의 "깨워 둔 일꾼 수" 등) 은 `[추측: 이름]` 이다. PDB 에 없어서 쓰는 줄에서만 읽었다.
   - epmapper 바인딩이 "빠른" 핸들 (`LRPC_FAST_BINDING_HANDLE`) 이라는 것도 `[추측]` (간접 근거).
6. **비유 이름이 run 문서와 다르다.**
   - run 문서 "결과" 비유: **식당 예약 전화**, 당번, 일감 바구니, 주문.
   - 이 글은 앞 해설들 (run125 · run129 · run133 · run137) 과 맞춰 **전화 부서 · 114 · 첫인사 · 신호등 · 파견소 · 우편함 · 쪽지 · 우체통** 으로 부른다. 그림만 다르고 사실은 같다 (아래 "비유").

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- 토큰 = **신분증** → [run49 해설](run49-kprocess-token-htable-해설.md)
- 이벤트 = **신호등** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md)
- 꼬리 점프 = `call` 대신 `JMP` 로 넘어가 돌아오지 않는 것 → [run15 해설](../02_sprint/run15-user-thread-startup-해설.md)
- 간접 호출 = 번호를 보고 이어 주는 **교환원** → [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md)
- 썽크 = 받은 부탁을 그대로 넘기는 **얇은 다리** → [run136 해설 2.3](run136-winsta-name-해설.md)
- **run129 에서 푼 것** → [run129 해설](run129-winnsi-rpc-해설.md)
  - 전화 부서 (rpcrt4) · 114 (epmapper) · 연락 카드 (바인딩) · 비동기 RPC ("끝나면 신호등 켜 주세요") → 2.4 · 2.5 · 2.7
  - **메모판 두 장** (연결 객체 `+0x4c` · 바인딩 `+0x114`) · **보류 `0xc0021018`** ("번호표 받고 앉아 계세요") → 2.9
- **run133 에서 푼 것** → [run133 해설](run133-lrpc-wire-해설.md)
  - bind = **첫인사** (0x70 B) → 2.5
  - 받기만 하는 부름 · 완료 통지 → 2.3
- **run137 에서 푼 것** → [run137 해설](run137-alpc-completion-해설.md)
  - 완료 포트 붙이기 (종류 2) · 쪽지 `{꼬리표, 0, 0, 0}` · "메시지 하나 = 쪽지 하나" → 2.3 · 2.5
  - 일꾼 → `TppAlpcpExecuteCallback` → `LrpcIoComplete` → 썽크 → `IoCompleted` → 2.6 · 2.7
  - 서버 흉내 차례 ① ~ ⑨ → 2.16 (이번에 한 칸 고쳐 읽는다)
- **run125 · run72 에서 푼 것**
  - 스레드 풀 = **일꾼 사무실**, 일꾼 공장 = **파견소**, 완료 포트 = 파견소의 **우편함**, 패킷 = **쪽지** → [run125 해설](run125-worker-factory-해설.md) 2.2
  - 풀어주기 패킷 = **빈 쪽지**, 메모판 `P` (`P+0x18` 넣은 쪽지 수 · `P+0x1c` 기다리는 일꾼 수) → run125 해설 2.2 · 2.3
  - **멀리 있기 · 묶음 수**, 관리 스레드 = **순찰 직원** → run125 해설 2.5
  - **놀기 제한 · 줄이기 · `0x102` ("빈손")** → run125 해설 2.11
  - 풀 안의 일감 줄 = 사무실 앞 **우체통**, `TppWorkPost` = 우체통에 편지 넣기, `NtReleaseWorkerFactoryWorker` = 파견소에 "**한 명 보내 주세요**" 쪽지 → [run72 해설](run72-pool-work-post-해설.md) 2.2 · 2.3 · 2.4

이번에 처음 나오는 말 — **첫 부름 · 둘째 부름**, **`fAsync`**, **콜백형 IO**, **상태 짝**, **요구 서버 SID**, **연결 응답 (종류 `0xb`)**, **맡김 (`TrySubmitThreadpoolCallback`)**, **자기 몫**, **깨워 둔 일꾼 수 · 줄 카운터**, **가상표**, **보안 문맥 · 가장**, **`AbortCall`** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 핸들 = **손잡이**, 이벤트 = **신호등**, 토큰 = **신분증**.
- run129 · run133 의 그림 그대로:
  - winnsi = 전화 신청을 맡기는 **신청 담당**. rpcrt4 = 가게 안 **전화 부서**. ALPC 포트 = **전화선 끝**.
  - epmapper = **114 안내**. bind = 전화마다 맨 처음 하는 **첫인사**.
  - 비동기 RPC = "끝나면 **신호등** 켜 주세요" 하고 신호등만 쳐다보는 신청.
  - 보류 `0xc0021018` = 창구의 "**번호표 받고 앉아 계세요**".
- run125 · run137 · run72 의 그림 그대로:
  - 스레드 풀 = **일꾼 사무실**, 일꾼 공장 = **파견소**, 완료 포트 = 파견소의 **우편함**.
  - 관리사무소는 전화선 끝에 메시지가 하나 올 때마다 우편함에 **쪽지** `{꼬리표, 0, 0, 0}` 한 장을 넣는다.
  - 풀 안의 일감 줄 = 사무실 앞 **우체통** (우편함과 **다른 통**). 우체통에 편지를 넣고, 필요하면 파견소에 "한 명 보내 주세요" 쪽지를 건넨다.
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 앞 해설 말로 바꾼 것 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - 비동기 연결 = 114 에 전화를 걸고 "**연결되면 알려 주세요**" 만 남기고 끊기. 첫인사는 아직 안 했다.
  - 신청 담당 (winnsi) 은 신호등 앞 소파에서 잔다.
  - 114 쪽이 "**연결됐어요**" 메시지 (연결 응답) 를 보내면, 관리사무소가 우편함에 쪽지를 넣는다.
  - 쪽지를 꺼내는 건 소파의 신청 담당이 아니라 **파견소 일꾼** 이다.
  - 일꾼은 "이어서 첫인사 하기" 를 **편지로 써서 사무실 앞 우체통에 넣는다** (맡김). 자기가 바로 집을 거면 아무도 안 부른다.
  - 그 편지를 집은 일꾼이 **신분증 확인** (보안 문맥) → **첫인사** (bind) 를 한다.
  - 그러니 **"연결됐어요" 메시지와 쪽지가 안 오면 아무 일도 안 일어난다.** 에뮬레이터가 멈춘 까닭이다.
- 헷갈리지 않게 — **쪽지** 와 **편지** 는 다르다.
  - 쪽지 = 관리사무소가 **우편함** (완료 포트) 에 넣는 것. "전화선에 뭔가 왔어요" 알림.
  - 편지 = 일꾼 사무실 안에서 일꾼이 **우체통** (풀 일감 줄) 에 넣는 것. "이 일 좀 해 주세요".

### 한 줄로 먼저 — 무엇을 얻었나

> **비동기로 114 에 처음 전화를 걸면, 전화 부서는 "번호표 받고 앉아 계세요" (보류) 를 받은 그 자리에서 돌아온다. 첫인사 (bind) 는 아직 안 했다. 그런데 연락 카드 입구 (`Bind__74b50`) 가 이 보류를 0 (성공) 으로 바꿔 돌려주고, 신청 담당 (winnsi) 은 신호등 앞에서 잠든다.**
> **첫인사를 미는 것은 114 쪽의 "연결됐어요" 메시지 (ALPC 종류 `0xb`) 와 그 쪽지다. 쪽지를 꺼낸 파견소 일꾼이 `IoCompleted` → `CompleteOnePart` → `SignalAllIos` → `SignalAsync` 를 지나 `TpSimpleTryPost` → `TppWorkPost` 로 "이어서 할 일" 을 우체통에 넣는다. 그 일을 집은 일꾼이 `DriveStateForward` 를 한 번 더 돌려 보안 문맥을 만들고, 곧바로 bind 를 보낸다.**
> **요청서 짐작 "`+0x5d4` 에서 일을 맡긴다" 는 틀렸다. `+0x5d4` 는 같은 함수 맨 앞 (`+0x43`) 으로 돌아가는 고리다.**
> **에뮬레이터가 멈춘 직접 원인은 둘이다. ① "연결됐어요" 메시지 · 쪽지가 아예 안 온다. ② 일꾼이 빈 우편함 앞에서 곧바로 "빈손" (`0x102`) 을 받고 퇴근한다 — 진짜 일꾼은 쪽지가 올 때까지 잔다.**

- 모두 `[정적]`. 단 "커널이 연결 응답 메시지 · 쪽지를 넣는 쪽" 은 `[추측: 커널 안 읽음]`, "이 길이 syscall 없는 맡김 갈래를 타는지" 는 `[추측]` (동적 Q271).

요청서 질문에 대한 답 (run 문서 "요약" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| 1 `+0x5d4` 뒤 어디로 · 같은 스레드에서 bind? · 무엇에 맡기나 · 콜백은? · `[R13]` | `+0x5d4 JZ` = 같은 함수 **`+0x43`** (고리). 같은 스레드 bind **아니오** — 첫 부름은 `+0x51e` 에서 보류로 끝남. `DriveStateForward` 안엔 `Tp*` · `TrySubmit*` · `Bind` **없음**. `[R13]` = `[바인딩+0x118]` = 요구 서버 SID 포인터 `[정적]` | 2.3 · 2.4 |
| 2 맡김 · 깨움 syscall · syscall 없는 길 · `0x102` | 맡김 = **`SignalAsync +0x2f`** → `TrySubmitWork` → `TrySubmitThreadpoolCallback` → **`TpSimpleTryPost +0x107 TppWorkPost`**. 자기 몫 · 깨워 둔 일꾼이면 **syscall 없음**, 아니면 `NtReleaseWorkerFactoryWorker` 1. `TppWorkerThread` 는 `0x102` 면 **큐를 안 보고 끝남** `[정적]` | 2.6 · 2.7 · 2.8 · 2.12 |
| 3 비동기 보안 문맥 | 같은 코드 (`OpenSecurityContextWorker`) · 같은 차례 4 syscall. **일꾼에서, bind 바로 앞** (`DriveStateForward +0x35d`). 실패하면 bind 안 감 (`AbortCall`) `[정적]` | 2.10 |
| 4 `NtSetInformationWorkerFactory(공장, 3, &1, 4)` | 풀 묶음 수가 0 이하 → 양수일 때만 (그 풀 첫 묶임). 커널 종류 3 = `공장+0x194` 묶음 수 — **`0x102` 에 일꾼을 남기는 칸이 아니다** `[정적]` | 2.13 |
| 5 `NtAlpcQueryInformation(0)` 버퍼 안 쓴 성공 | 반환 1 (받기 `0x1000`). **비동기 길은 안 바뀜.** 덤: 일꾼 `IoCompleted +0x6a` 도 한 번 더 부름 `[정적]` | 2.14 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run128 ~ run137 과 같은 자리, **초기화 밖** 이다. `main` 뒤 프로그램이 웹을 쓸 때의 옆길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll: LdrInitializeThunk → LdrpInitializeProcess → DLL 들이기 · 전원 켜기
 └─ NtContinue (은퇴) → exe 진입점 → (CRT 점선 상자) → main
                                                        │
      test.exe 는 여기서 RPC 를 안 쓴다 [소스]          │ 웹을 쓰는 프로그램이라면 (요청 쪽 에뮬레이터 로그):
                                                        └─ WinHttpSendRequest                                   (run128)
                                                             └─ 신청 담당 winnsi → 전화 부서 rpcrt4 → 114 에 전화   (run129)
                                                                  ├─ 전화선 열기 · 우편함에 붙이기                   (run129 · run137)
                                                                  ├─ 붙이기 → 첫인사 사이를 무엇이 미나              ← 이번
                                                                  ├─ 첫인사 · 본론 메시지 꼴                         (run133)
                                                                  └─ 답 배달: 쪽지 → 일꾼 → 받기                     (run137)
```

- run137 해설 2.16 "서버 흉내 차례" 표는 ①c (붙이기) 바로 뒤에 ③ (114 첫인사) 를 놓았다.
- 그 **사이** 를 무엇이 미는지는 기록이 없었다. 이번이 그 빈칸이다.

### 왜 이 질문이 지금 왔나

PE_emulator 쪽에서 막혔다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님).

- 114 (epmapper) 창구를 열었더니:
  - **동기 손님** (`GetAddrInfoW`) 은 같은 스레드에서 차례대로 연결 (5617) → 묻기 (5618) → 신분증 네 줄 (5619 ~ 5622) → **bind** (5623) 까지 갔다.
  - **비동기 손님** (winnsi) 은 연결 (2191) → 묻기 (2192) → 우편함 붙이기 (2193) 뒤, **syscall 없이** `NtWaitForSingleObject` 에서 멈췄다.
  - 일꾼 둘은 `NtWaitForWorkViaWorkerFactory` → `0x102` → `NtTerminateThread`. 스레드가 다 서서 끝 (2201).
- 에뮬레이터는 지금 비동기 epmapper 연결을 예전처럼 `0xc0000034` 로 돌려보내 두고 이 답을 기다린다.

### 무엇이 궁금했나

1. `DriveStateForward +0x589 ~ +0x5d4` 뒤 어디로 가나. 같은 스레드에서 bind 를 부르나, 일을 맡기나 (무엇에 · 어떤 Tp 함수로). `LrpcBHDriveStateForwardRoutine` · `DriveStateForwardAsyncCallback` 이 그 콜백인가. `[R13]` 은 무엇인가.
2. 맡긴 일이 일꾼을 깨우는 syscall · 항목 꼴. syscall 없이 맡기는 길이 있나. `TppWorkerThread` 가 돌아온 뒤 유저 큐를 보는 줄과 `0x102` 뒤 끝나는 줄.
3. 비동기 보안 문맥은 어디서 하나. 실패하면 bind 를 안 보내나.
4. `NtSetInformationWorkerFactory(공장, 3, &1, 4)` 가 안 나오는 조건 · 종류 3 의 커널 뜻.
5. `NtAlpcQueryInformation(0)` 에 버퍼 안 쓴 성공이 비동기 길을 바꾸나.

### 바퀴 전에 알던 것

- `[정적]` (run129) 끝점 풀이 사슬 `DriveStateForward +0x8ca → … → AlpcConnect +0x293 NtAlpcConnectPort`, `DriveStateForward +0x515 Connect`. 비동기 연결이면 `AlpcConnect` 가 보류 `0xc0021018` + `*출력 = 1` → `Connect +0x3b1 EnableAsync`.
- `[정적]` (run133) 동기 / 비동기는 `LRPC_BIND_CCALL::Bind` 첫 정수 인자에서 온다. 그런데 **`DriveStateForward` 의 인자 전달은 안 읽었다** `[공백]`. 그래서 "bind 둘 (epmapper · nsi) 도 비동기" 는 `[추측]` 이었다.
- `[정적]` (run137) 붙이기 `Connect +0x3b1 EnableAsync → CreateAlpc → … → TppAllocAlpcCompletion +0x1d1 NtAlpcSetInformation(2)`, 첫 묶임이면 `TpAdjustBindingCount +0x3b NtSetInformationWorkerFactory(공장, 3, &1, 4)`. 메시지 하나 = 쪽지 하나, 일꾼 → `IoCompleted`.
- `[정적]` (run125) 일꾼은 `NtWaitForWorkViaWorkerFactory` 에서 **시간 제한 없이** 쪽지를 기다린다. `0x102` 는 놀기 제한 · 줄이기 판단. 다만 `0x102` 를 받은 일꾼이 정말 스레드를 끝내는지는 `TppWorkerThread` 를 안 읽어 `[추측]` 이었다 (run125 해설 2.11 끝).
- `[정적]` (run72) `TppWorkPost` 첫 넣기 = `+0x4f3 NtReleaseWorkerFactoryWorker` 1번.
- 몰랐던 것: 비동기 첫 연결 뒤 **누가 · 무엇에 밀려** bind 를 보내는지.

---

## 2. 찾은 것

### 2.1 한눈에 — 장면 셋 `[정적]`

run 문서 "호출 관계" 에서 대표만 뽑았다. `+0x..` 는 ret 자리, `⇢` 는 간접 · 가상 호출 · 꼬리 점프 · 가져오기.

```
장면 1 — 부른 쪽 (winnsi 주 스레드), 첫 부름
rpcrt4!BindToEpMapper +0x146 RpcBindingBind ⇢ Bind__74b50   (비동기 상태 ≠ NULL → fAsync 1)
 ├─ +0x15c  DriveStateForward(this, 1, …)
 │           ├─ IO 를 콜백형으로 {종류 3, LrpcBHDriveStateForwardRoutine}
 │           └─ +0x51a  Connect → AlpcConnect NtAlpcConnectPort (Flags 0) · EnableAsync … (run137)
 │                       ← 보류 0xc0021018   +0x51e JNZ → +0x266 반환
 └─ 보류 → 0 으로 바꿈 (BaseBind 건너뜀) → winnsi WaitForSingleObject (신호등 앞에서 잠)

(커널: 클라이언트 포트에 연결 응답 종류 0xb 메시지 + 쪽지 {키, 0, 0, 0}   [추측: 커널 안 읽음])

장면 2 — 쪽지를 꺼낸 일꾼 W
ntdll!TppWorkerThread +0x37e NtWaitForWorkViaWorkerFactory → … ⇢ IoCompleted   (run137)
 ├─ +0x6f   IsPortWithSharedSection → NtAlpcQueryInformation(0)
 ├─ +0xcf   NtAlpcSendWaitReceivePort  받기만 → 종류 0xb
 └─ +0x3e8  CompleteOnePart ([연결+0x4c] = 4) → SignalAllIos → SignalAsync
             └─ +0x2f JMP TrySubmitWork ⇢ TrySubmitThreadpoolCallback ⇢ ntdll!TpSimpleTryPost
                                                                     └─ +0x10c TppWorkPost   (맡김)

장면 3 — 맡긴 일을 집은 일꾼 (W 자신, 또는 깨운 다른 일꾼)
ntdll!TppWorkerThread +0x702 TppWorkerFindTask · +0x819 TppSimplepExecuteCallback
 ⇢ ThreadPoolInvokeCallback ⇢ … ⇢ LrpcBHDriveStateForwardRoutine
    └─ +0x61  DriveStateForwardAsyncCallback
               ├─ +0xb5  DriveStateForward(this, 1, 0, 호출)  둘째 부름 → +0x362 OpenSecurityContextWorker
               ├─ +0xe5  ⇢ Bind__93cc0 → BaseBind → LRPC_CASSOCIATION::Bind +0x62d NtAlpcSendWaitReceivePort = bind
               └─ +0x109 ⇢ CallBound(호출, 1)
```

- 비유로 한 번: 신청 담당이 114 에 전화를 걸고 "연결되면 알려 주세요" 만 남긴 채 신호등 앞에서 잔다 (장면 1). 114 가 "연결됐어요" 를 보내면 관리사무소가 우편함에 쪽지를 넣고, 일꾼이 그 쪽지를 꺼내 "이어서 첫인사 하기" 편지를 우체통에 넣는다 (장면 2). 그 편지를 집은 일꾼이 신분증 확인 뒤 첫인사를 한다 (장면 3).

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `│ ← 보류 0xc0021018   +0x51e JNZ → +0x266 반환` (장면 1)

- **누가**: `rpcrt4!LRPC_BASE_BINDING_HANDLE::DriveStateForward` (rva `0x44e00`) — 연락 카드의 상태를 한 칸씩 앞으로 미는 함수.
- **어디서**: `+0x515` 에서 `Connect` 를 부르고 (`+0x51a` 로 돌아옴), 바로 다음 `+0x51e` 의 `JNZ`.
- **누구를**: 점프 목적지 `+0x266` = 이 함수의 **반환 자리**.
- **무엇을 위해**: `Connect` 가 0 이 아닌 값 (비동기 첫 연결이면 보류 `0xc0021018`) 을 돌려주면 **그 자리에서 함수를 끝낸다**. 동기는 0 이 오니 `+0x524 / +0x52b JMP +0x43` 으로 같은 호출 안에서 계속 돈다.
- 그래서 이 줄 하나가 "비동기 첫 부름은 bind 까지 안 간다" 의 근거다 `[정적]`.

#### 줄 ②: `└─ +0x2f JMP TrySubmitWork ⇢ … ⇢ ntdll!TpSimpleTryPost` (장면 2)

- **누가**: `rpcrt4!LRPC_CLIENT_IO::SignalAsync` (rva `0x3c1a4`) — 기다리던 호출 하나에 "이제 진행해도 돼요" 를 알리는 함수.
- **어디서**: `+0x2f`. call 이 아니라 **꼬리 점프** 라 ret 자리가 없다.
- **누구를**: `RPC_THREAD_POOL::TrySubmitWork` (rva `0x2f2c0`). 넘기는 것:
  - `RCX` = `ThreadPoolInvokeCallback` (`+0x1b LEA RCX,[0x180098830]`) — 나중에 일꾼이 부를 함수.
  - `RDX` = IO — 그 호출의 IO 칸.
- **무엇을 위해**: "이어서 할 일" 을 일꾼 사무실에 **맡긴다**. `TrySubmitWork +0x26` → `TrySubmitThreadpoolCallback` → `ntdll!TpSimpleTryPost +0x107 TppWorkPost` 로 이어진다 (2.6).

### 2.3 ★ 첫 부름은 bind 를 안 보낸다 — 보류를 0 으로 바꿔 돌려주고 잔다 `[정적]`

run 문서 1-1 · 1-2 (A1 ~ A7).

#### 새 말 — 첫 부름 · 둘째 부름

- `DriveStateForward` 는 비동기 첫 연결에서 **두 번** 불린다 `[정적]`.
  - **첫 부름**: 부른 쪽 스레드 (winnsi 주 스레드) 가 `Bind__74b50 +0x157` 에서.
  - **둘째 부름**: 연결 응답 뒤 일꾼이 `DriveStateForwardAsyncCallback +0xb0` 에서.
- 비유: 같은 신청서를 두 번 들여다보는 것. 처음엔 전화만 걸어 두고, "연결됐어요" 가 온 뒤 다시 꺼내 나머지를 채운다.

#### 새 말 — `fAsync`

- `DriveStateForward` 의 둘째 인자 (EDX). 1 = 비동기, 0 = 동기 `[정적]`.
- 어디서 오나 (flow-writer 가 코드 줄로 본 것):
  1. `Bind__74b50 +0x34 MOV R13,RDX` — 둘째 인자 (비동기 상태) 를 받아 둔다.
  2. `+0x5c MOV EAX,1` · `+0x61 TEST R13,R13` · `+0x64 JNZ → +0x2c3 MOV R14D,EAX` — 비동기 상태가 NULL 이 아니면 1. 아니면 `+0x6a XOR R14D,R14D` (0).
  3. `+0x151 MOV EDX,R14D` → `+0x157 DriveStateForward`.
- **run133 의 `[공백]` "`DriveStateForward` 의 인자 전달을 안 읽음" 이 이걸로 닫혔다.**

#### 새 말 — 콜백형 IO

- 호출 객체의 `[호출+0x288]` 칸 = `LRPC_CLIENT_IO`. 종류 `[+0]` · 콜백 `[+8]` · 상태 `[+0x38]` 를 든다 `[정적]` (칸 이름은 `[추측]`).
  - 종류 1 = 동기 (스레드가 직접 기다림).
  - 종류 3 = **콜백형** — 일이 끝나면 적힌 콜백을 불러 달라는 표시.
- 비유: 창구에 두고 가는 **"다 되면 이 번호로 연락 주세요" 메모**. 동기 손님은 메모 없이 창구 앞에 서서 기다린다.

#### 한 걸음씩 — 첫 부름 (부른 쪽 스레드)

1. `BindToEpMapper +0x141 RpcBindingBind(비동기 상태, …)` ⇢ 가상 `[+0x128]` = `Bind__74b50` → `+0x157 DriveStateForward(this, EDX = 1, …)`. (동기 손님은 `EDX = 0`.)
2. 상태 6 몸: `+0x480 [바인딩+0x114] = 6`, 짝 6 → 7 쓰기 (`+0x4a8 / +0x4b3`, 짝은 2.4).
3. `+0x4be TEST ESI / +0x4c0 JNZ → +0x8f1`: 비동기라 `+0x8f4 ReinitCommon` 뒤 **IO 를 콜백형으로** — `+0x900 [IO] = 3` · `+0x906 [IO+8] = LrpcBHDriveStateForwardRoutine` (`0x18003b000`). (동기면 `+0x4cd [IO] = 1`.)
4. `+0x515 Connect`. 첫 연결이라 `Connect` 안에서:
   - `+0x2c0 LRPC_BASE_MINI_THUNK` — **원래 IO 콜백을 보관해 두고**,
   - `+0x371 ~ +0x37f` IO 콜백 칸을 `LrpcBaseProviderComplete` (`0x96a10`) 로 바꾸고,
   - `+0xd2 Register` · `+0x198 AlpcConnect` → `AlpcConnect +0x293 NtAlpcConnectPort(Flags 0, …)` (로그 2191).
   - `AlpcConnect +0x388 IsPortWithSharedSection` → `NtAlpcQueryInformation` (2192) → 보류 `0xc0021018`.
   - `Connect +0x3b1 EnableAsync` → … → `NtAlpcSetInformation(포트, 2, 16 B, 0x10)` (2193, run137).
5. `Connect` 가 보류를 돌려줌 → **`+0x51e JNZ → +0x266` 반환** (줄 ①).
6. `Bind__74b50 +0x217 / +0x21a JZ → +0x1a4 / +0x1a6 JNZ → +0x20b CMP / +0x210 XOR EBX,EBX` — **보류를 0 으로 바꿔** 돌려준다. `+0x19a BaseBind` (bind 를 보내는 쪽) 는 건너뛴다.
7. winnsi 로 돌아가 `WaitForSingleObject` — 신호등 앞에서 잔다 (`NtWaitForSingleObject`, 로그 2194 뒤).

- 4 의 "원래 콜백 보관 → `LrpcBaseProviderComplete` 로 바꿈" 을 기억해 두자. 2.8 에서 "콜백 칸을 두 번 거친다" 의 이유가 된다.
- 동기 손님은 같은 호출 안에서 6 → 9 를 다 돌고 (`Connect` 가 막고 기다림) `Bind__74b50 +0x19a BaseBind` 로 bind 까지 간다.

비유: 신청 담당이 "연결되면 알려 주세요" 메모를 창구에 두고 (콜백형 IO), 114 에 전화를 건다. 창구는 "번호표 받고 앉아 계세요" (보류) 라고 한다. 그런데 연락 카드 입구 (`Bind__74b50`) 는 신청 담당에게 "**네, 접수됐어요**" (0) 라고만 전한다. 신청 담당은 신호등 앞에서 잔다. **첫인사는 아직 아무도 안 했다.**

#### 갈래 — 이미 연결된 전화선을 다시 쓰면

- 연결 객체가 이미 "연결됨" (`[연결+0x4c] = 4`) 이면 `Connect +0x4b CMP R13D,4 / +0x4f / +0x54 JNZ → +0x388` 이 곧바로 0 을 돌려준다.
- 그러면 첫 부름이 0 으로 끝나 `Bind__74b50 +0x19a BaseBind` 로 **부른 쪽 스레드가 bind 를 보낸다** `[정적: 갈래만, 실제로 타는 쪽은 동적]`.
- winnsi 의 이번 길은 **첫 연결** 이라 해당 없다.

### 2.4 ★ 요청서 짐작 정정 — `+0x5d4` 는 맡김이 아니라 되돌이 고리 `[정적]`

run 문서 1-1 · "정정".

#### 요청서는 이렇게 짐작했다

- `DriveStateForward +0x589 EnableAsyncIfNecessary` 뒤 `+0x5b9 / +0x5c4` 에서 상태 8 / 9 를 쓰고, `+0x5cf CMP [R13],0` · `+0x5d4 JZ` 가 어딘가로 간다.
- "여기서 Tp 로 일을 맡기고 돌아가는 것 아닐까" — PE_emulator 02 가 줄을 열어 보고 한 짐작이다.

#### 새 말 — 상태 짝

- 호출 객체의 `[호출+0x2d0]` · `+0x2d4` = "**바인딩 상태가 이것이면 저것으로**" 짝 (옛, 새) `[정적]` (이름은 `[추측]`).
- 함수 맨 앞 `+0x69 ~ +0x82` 가 바인딩 상태 `[바인딩+0x114]` 를 보고, 옛 값과 같으면 새 값으로 바꾼다.
- 비유: 메모판 옆에 붙인 "**3번이 끝나면 4번으로 넘기세요**" 포스트잇. 함수가 맨 앞으로 돌아올 때마다 이 포스트잇을 보고 메모판을 한 칸 넘긴다.

#### 새 말 — 요구 서버 SID

- `[바인딩+0x118]` = `LPC_NORMALIZED_SID`. 그 첫 8 B = **"상대 서버가 이 신분이어야 한다" 는 SID 의 포인터** `[정적]`.
  - SID = 신분증에 적힌 **신분 번호** `[추측]` (일반적으로 알려진 설명).
- 연결 객체 쪽 `[연결+0xc0]` 엔 연결이 들고 있는 서버 SID 가 있다 (첫 연결 때 `Connect +0x12a DuplicateSID → +0x138`) `[정적]`.
- 비유: 연락 카드에 적힌 "**이 사람과만 통화**" 칸. 0 이면 아무나 괜찮다.

#### 코드가 실제로 하는 것 — 한 걸음씩

1. `+0x5b2 LEA R13,[RDI+0x118]` — `R13` = 요구 서버 SID 칸.
2. `+0x5b9 / +0x5c4` — 짝 8 → 9 를 써 둔다.
3. `+0x5cf CMP [R13],0` · **`+0x5d4 JZ → +0x43`** — 요구 SID 가 없으면 **같은 함수 맨 앞** (`RtlEnterCriticalSection` 다시) 으로.
4. 있으면 (`[R13] ≠ 0`): `+0x60f` 에서 `[연결+0xc0]` 을 보고 (0 이면 `+0x615 JZ → +0x799 AlpcVerifyServerSid`), **`+0x61b EqualSid`** 로 비교 → 같으면 `+0x629 JNZ → +0x43`. 역시 맨 앞.
   - 다르면 `+0x64a PrepareIterationInternal` → `+0xacb IterateAndVerify`.
5. 맨 앞으로 돌아가면 `+0x69 ~ +0x82` 가 짝 8 → 9 를 적용 → `+0x82 JZ → +0x2fe` 상태 9 몸 → **`+0x35d OpenSecurityContextWorker`** (2.10).

- 그리고 결과서가 `DriveStateForward.c` 머리의 callee 목록을 보니 `Bind` · `Tp*` · `TrySubmit*` 가 **아예 없다**.
- 그래서 → **`+0x5d4` 는 맡김이 아니라 "한 칸 넘기고 맨 앞으로" 고리** 다 `[정적]`. 결과서가 PE 바이트로 `+0x5d4 JZ → 0x44e43` (= `+0x43`) 를 맞췄다.
- 하나 더: 이 상태 8 몸 (`+0x55b ~ +0x5d4`) 은 비동기 첫 연결이면 **첫 부름에선 돌지도 않는다** (2.3 에서 `+0x51e` 로 이미 끝났으니). **둘째 부름 (일꾼)** 에서 처음 돈다.

비유: 요청서는 "포스트잇을 붙인 뒤 누구에게 일을 넘기나" 를 찾았다. 그런데 그 줄은 "**포스트잇 붙였으면 메모판 맨 위로 돌아가 다시 읽어라**" 였다. 일을 넘기는 손은 이 함수에 없다. 넘기는 건 "연결됐어요" 를 받은 쪽 (`SignalAsync`) 이다.

#### 같이 정정된 셋 (run 문서 "정정" 2 ~ 4)

| 짐작 | 실제 |
|---|---|
| 비동기 bind 를 같은 스레드가 보내나 | **아니오, 풀 일꾼** 이 보낸다 (첫 연결 때). 부른 쪽은 보류를 0 으로 받고 잔다 |
| `LrpcBHDriveStateForwardRoutine` 이 Tp 콜백 | **아니다.** Tp 가 부르는 콜백은 `ThreadPoolInvokeCallback` (rva `0x98830`, 34 B). 그것이 IO 콜백 칸을 **두 번** 거쳐 `LrpcBHDriveStateForwardRoutine` 에 닿는다 (2.8) |
| 비동기 손님은 보안 문맥 줄이 없다 | 안 하는 게 아니라 **아직 못 간 것** (연결 응답이 없어서) (2.10) |

- 앞 run 이 틀린 게 아니라 **요청서 · 원문의 짐작** 이 틀렸다.

### 2.5 ★ 무엇이 bind 를 미나 — "연결됐어요" 메시지 (종류 `0xb`) 와 그 쪽지

run 문서 1-2 (B1 ~ B3) · 2-1 ①.

#### 새 말 — 연결 응답 (ALPC 메시지 종류 `0xb`)

- 서버가 연결을 받아들였다는 뜻으로 클라이언트 포트에 들어오는 메시지 `[추측: 커널 안 읽음]`.
- rpcrt4 가 받는 쪽은 코드로 보인다: `IoCompleted` 가 받은 메시지의 ALPC 종류 (`[+4] & 0xff`) 가 **`0xb`** 이면 "연결 끝" 으로 처리한다 `[정적]`.
- 비유: 114 쪽이 다시 보내는 "**연결됐어요**" 메시지. 이것도 전화선 끝에 놓이는 **메시지 한 장** 이고, run137 대로라면 메시지 한 장마다 우편함에 쪽지 한 장이 들어간다.

#### 일꾼 W 가 하는 일 — 한 걸음씩

1. `TppWorkerThread +0x379 NtWaitForWorkViaWorkerFactory` 가 항목 `{키, 0, 0, 0}` 하나로 돌아온다 → `+0x55e TppAlpcpExecuteCallback` → `LrpcIoComplete` → 썽크 → `IoCompleted` (run137 2-2) `[정적]`.
2. `IoCompleted +0x6a IsPortWithSharedSection` → `NtAlpcQueryInformation(포트, 0, …)` — **일꾼에서 한 번 더** (2.14).
3. `+0xc8 NtAlpcSendWaitReceivePort(포트, 0, NULL, NULL, 버퍼, &길이, &속성, NULL)` — 받기만. 메시지 종류 **`0xb`**.
4. `+0x13e ~ +0x142` 종류 = `[+4] & 0xff` → **`+0x184 CMP ESI,0xb / +0x187 JZ → +0x3bb`**.
5. `+0x3e3 CompleteOnePart(연결, 0, 0, NULL, 1)` (연결 객체 쪽, rva `0x3b148`):
   - `+0x26e [연결+0x4c] = 4` — 메모판에 "연결됨".
   - **`+0x1c7 SignalAllIos`** — 기다리던 모두에게 알림 → `+0x11c SignalAsync` (IO 종류 3 이니까).

- 동기 손님은? `Connect +0x1c5` 가 `CompleteOnePart` 를 **같은 스레드에서 바로** 부른다. 일꾼도 쪽지도 필요 없다.

#### 어디까지가 코드로 보였나 — 짐작의 단계

- 근거: rpcrt4 가 종류 `0xb` 를 받아 연결 끝으로 처리하는 줄 (`IoCompleted +0x184`) 은 `[정적]`.
- 근거: run137 2-1 은 커널이 보내기 · 답 · 닫기 · 연결 요청 처리 때 쪽지를 넣는 줄을 읽었다.
- 그래서: 연결 응답도 같은 길로 메시지 + 쪽지가 들어온다고 **읽힌다**.
- 아직 아님: **연결 응답이 정말 그 길인지는 커널을 안 읽어서 `[추측]`** 이다. 헤더 칸 값도 `[공백]` (Q272 1).

비유: 신청 담당이 자는 동안, 114 쪽이 "연결됐어요" 메시지를 보낸다. 관리사무소는 그걸 전화선 끝에 놓고 우편함에 쪽지 한 장을 넣는다. 일꾼이 쪽지를 꺼내 전화 부서 창구에 가서 전화선을 들여다보니 "연결됐어요" (`0xb`). 일꾼은 메모판에 "연결됨" 을 적고, "기다리던 분들께 알려 드려야지" 하고 알림을 돌린다.

### 2.6 ★ 맡김 사슬 — `SignalAsync` → `TrySubmitWork` → `TpSimpleTryPost` → `TppWorkPost` `[정적]`

run 문서 1-2 B4 · "정적 — 호출 지점" 표.

#### 새 말 — 맡김 (`TrySubmitThreadpoolCallback`)

- "이 함수를 이 인자로 **일꾼이 불러 주세요**" 하고 스레드 풀에 한 번짜리 일을 넣는 API. 맡긴 쪽은 기다리지 않고 돌아온다 `[추측]` (일반적으로 알려진 설명 — 기다림 없음은 run72 의 `TppWorkPost` 가 `[정적]`).
- 비유: 일꾼 사무실 앞 **우체통에 "이 일 좀 해 주세요" 편지 넣기**.

#### 사슬 — 한 칸씩

| 차례 | 누가 · 어디서 | 무엇을 |
|---|---|---|
| 1 | `rpcrt4!SignalAsync` `+0x22` | `[IO+0x38] = 상태` (이번엔 0) 를 적어 둔다 |
| 2 | 〃 `+0x1b` | 콜백 = `ThreadPoolInvokeCallback` 주소를 `RCX` 에 |
| 3 | 〃 **`+0x2f` (꼬리 점프)** | `RPC_THREAD_POOL::TrySubmitWork(ThreadPoolInvokeCallback, IO)` |
| 4 | `rpcrt4!TrySubmitWork` `+0x10` | `InitializeCallbackEnvironmentIfNecessary` |
| 5 | 〃 **`+0x26`** (ret `+0x2d`) | `TrySubmitThreadpoolCallback` (api-ms 가져오기 칸 `0x1800e9060`) → kernelbase `+0x4` (kernel32 면 `+0x6`) → `ntdll!TpSimpleTryPost` |
| 6 | `ntdll!TpSimpleTryPost` (rva `0x84e00`) `+0xc1` | `TppWorkInitialize` — 일감 하나 차리기 |
| 7 | 〃 **`+0x107`** (ret `+0x10c`) | **`TppWorkPost`** (rva `0x39430`) — 우체통에 넣기 |

- **이 길에 없는 것**: `TpPostWork` · `TpSetWait` · `NtSetIoCompletion*` `[정적]`.
  - 요청서가 후보로 든 `NtSetIoCompletion(Ex)` (우편함에 쪽지 넣기) 는 **안 쓴다**. 맡김은 우편함이 아니라 **우체통** 으로 간다.
- kernelbase 인지 kernel32 인지는 동적 8 (Q271). 어느 쪽이든 `ntdll!TpSimpleTryPost` 다.

비유: 일꾼은 "연결됐어요" 알림을 돌리다가, 그 알림 받을 사람이 "다 되면 이 번호로 연락 주세요" 메모 (콜백형 IO) 를 남겼다는 걸 본다. 그래서 그 자리에서 전화하지 않고 "**이 메모대로 연락하기**" 편지를 써서 우체통에 넣는다.

### 2.7 ★ syscall 없는 맡김 — "자기 몫" 이면 아무도 안 부른다 `[정적: 갈래]`

run 문서 2-1 ② · ②′.

에뮬레이터 로그엔 맡김 쪽 syscall 이 **0개** 였다. 요청서가 "syscall 없이 맡기는 길이 진짜 있나" 를 물었다. → **있다** (코드에).

#### 새 말 — 자기 몫

- 맡기는 스레드가 **바로 그 풀의 일꾼** 이고, 그 일꾼 기록의 상태가 3 이면, `TppWorkPost` 는 "이건 내가 콜백 끝나고 바로 집을게" 표시만 하고 파견소를 안 부른다 `[정적]`.
- 비유: 우체통에 편지를 넣는 사람이 **자기도 일꾼** 이다. "지금 하던 일 끝나면 내가 바로 꺼내 갈게" — 그러니 파견소에 "한 명 보내 주세요" 쪽지를 건넬 필요가 없다.

#### 새 말 — 깨워 둔 일꾼 수 · 쉬는 일꾼 수 · 줄 카운터

- 풀 `[풀+8]` 의 숫자 칸들 `[추측: 이름]` (PDB 에 없고 쓰는 줄에서 읽음):
  - 아래 dword 의 위 16 b = **깨워 둔 일꾼 수** — 이미 "한 명 보내 주세요" 를 건넸는데 아직 안 온 수.
  - 아래 16 b = **쉬는 일꾼 수** (`TppWorkerThread +0x696 MOVSX ECX,AX / +0x699 INC ECX`).
  - 위 dword = **줄에 쌓인 일감 수** (줄 카운터).
- 비유: 사무실 벽 칠판의 숫자 셋 — "부르러 간 사람 수", "쉬는 사람 수", "우체통에 쌓인 편지 수".

#### `TppWorkPost` 안 — 한 걸음씩

1. 우체통에 한 항목을 넣고 `[풀+8]` 위 dword (줄 카운터) `+1` (`+0x48a ~ +0x490` · `+0x4a8 CMPXCHG`).
2. **자기 몫 검사** (`+0x44c ~ +0x474`, flow-writer 가 한 칸 더 따라가 고친 것):
   - `+0x44c RAX = [TEB+0x1778]` (PDB `_TEB.ThreadPoolData`). `+0x453 / +0x456` 0 이면 건너뜀.
   - `+0x458 MOV RAX,[RAX]` — 한 번 더 따라간다.
   - `+0x45b CMP [RAX+0x30],RDI` — 같은 풀인가. `+0x461 CMP [RAX+0x80],3` — 상태 3 인가.
   - 맞으면 `+0x46a` 4 로 바꾸고 `R8D = 1`.
3. `+0x4cd TEST R8D / +0x4d0 JNZ → +0x4a0` — **자기 몫이면 `+0x4f3` 을 건너뛴다.**
4. 아니면 `+0x498 TEST ECX,0xffff0000 / +0x49e JZ → +0x4cd` — **깨워 둔 일꾼이 이미 있으면** 역시 건너뜀.
5. 둘 다 아니면 `+0x4d2 ~ +0x4ed` 깨워 둔 수 `+1` (`+0x4dd ADD EDX,0x10000`) → **`+0x4f3 NtReleaseWorkerFactoryWorker([풀+0x38])`** — 파견소에 "한 명 보내 주세요".

| 경우 | syscall |
|---|---|
| ② 같은 풀 일꾼 · 상태 3 (자기 몫) | **없음** |
| ②′ 깨워 둔 일꾼이 이미 있음 | **없음** |
| ②′ 그 밖 (다른 스레드가 맡김 등) | `NtReleaseWorkerFactoryWorker` 1 |

#### 이번 길이 정말 "자기 몫" 인가 — 짐작의 단계

- 근거: 연결 응답을 처리하는 것은 **풀 일꾼** 이다 (2.5) `[정적]`. 그러니 첫 검사 (같은 풀) 는 맞을 수 있다.
- 그런데: 상태 3 을 **쓰는** 줄을 못 찾았다 `[공백]`. `[[TEB+0x1778]+0]` 을 채우는 줄도 `[공백]` (flow-writer 대조 — `TppAllocThreadData` 는 `+0` 을 안 쓴다).
- 그래서: "연결 응답을 받은 일꾼이 자기 몫 갈래를 탄다" 는 `[추측]`. 아니면 이 자리에서 `NtReleaseWorkerFactoryWorker` 가 한 번 나온다.
- 확인: 동적 Q271 3 (`ntdll!TppWorkPost+0x4cd` 의 `r8d`, `+0x4f3` 이 걸리나).
- 에뮬레이터 로그의 "맡김 쪽 syscall 0개" 는 **어느 쪽이어도 모순이 아니다** — 그 로그는 맡김 자리까지 **못 갔다**.

#### run72 와 어긋나지 않나

- run72: 로더 풀 첫 넣기 = `+0x4f3` 1번 `[정적]`.
- 새 풀이라 깨워 둔 수는 0 이고, 넣는 스레드가 메인 스레드면 `[TEB+0x1778]` 이 0 이라 (`+0x453 / +0x456` 건너뜀) 두 건너뛰기 갈래 모두 안 탄다. → 어긋나지 않는다.
  - 메인 스레드 값이 0 이라는 건 run72 동적 조건 4 가 볼 값이라 `[추측]`.
- 로더 풀 일꾼이 다음 일감을 넣을 때 자기 몫 갈래를 타는지는 안 봤다 `[공백]`.

### 2.8 맡긴 일을 집기까지 — 콜백 칸을 두 번 거친다 `[정적]`

run 문서 1-2 B5 · 2-2.

#### 일꾼이 콜백을 마친 뒤 (`TppWorkerThread`, rva `0x1a2e0`)

1. `+0x5eb TppCallbackEpilog` (쪽지 콜백 = `IoCompleted` 쪽 일을 마침).
2. **`+0x65f ~ +0x68d`** — 줄 카운터 (`[풀+8]` 위 dword) 가 0 이 아니면 `+0x68d JNZ → +0x845` 에서 1 줄이고 → `+0x6ea → +0x6fd TppWorkerFindTask` — 우체통에서 일감을 찾는다.
3. 0 이면 쉬는 수 `+1` · `+0x6e5 JMP → +0x2ce` — 다시 `NtWaitForWorkViaWorkerFactory` (우편함 앞으로).
4. 찾았으면 `+0x814 TppSimplepExecuteCallback` → (간접) `ThreadPoolInvokeCallback`.

- **"유저 큐를 보는 줄" = 2 의 줄들** 이다. 콜백이 끝나자마자 칠판의 "쌓인 편지 수" 를 본다.
- 그래서 자기 몫 맡김은 파견소를 안 불러도 된다 — 맡긴 일꾼이 **콜백 끝나자마자** 이 카운터를 보고 집기 때문이다 `[정적]`.

#### 콜백 칸을 두 번 거치는 길

| 차례 | 함수 · 줄 | 하는 일 |
|---|---|---|
| 1 | `ThreadPoolInvokeCallback` (rva `0x98830`) `+0x11` | `LRPC_CLIENT_IO::InvokeCallback` (rva `0x430f8`) — IO 의 콜백 칸을 부른다 |
| 2 | 그 칸엔 지금 `LrpcBaseProviderComplete` (`0x96a10`) | 2.3 걸음 4 에서 `Connect` 가 바꿔 둔 것 |
| 3 | `LrpcBaseProviderComplete +0x44` | `CLIENT_IO_PROVIDER::Complete` (rva `0x42ed4`) |
| 4 | `Complete +0x93 ~ +0xa4` | IO 칸을 **보관해 둔 원래 값** (종류 3 · `LrpcBHDriveStateForwardRoutine`) 으로 되돌림 |
| 5 | `Complete +0x119` | `InvokeCallback` — 이제 원래 콜백 |
| 6 | `LrpcBHDriveStateForwardRoutine` (rva `0x3b000`) `+0xf` · `+0x5c` | `ESI = [IO+0x38]` (상태) → `DriveStateForwardAsyncCallback` (rva `0x3c264`) |
| 7 | `DriveStateForwardAsyncCallback +0x1b JZ → +0xa3` | 상태 0 이라 성공 길 → **`+0xb0 DriveStateForward(this, 1, 0, 호출)`** = 둘째 부름 |

- `Complete` 사이의 `+0x1ae IsRetryErrorCode` 갈래는 상태 0 에서 안 탄다고 읽음 `[추측: 본문 안 읽음]`.

비유: 메모 위에 `Connect` 가 **임시 메모** ("연결 담당에게 먼저 알려 주세요") 를 덧붙여 놨다. 일꾼이 먼저 임시 메모대로 가면, 거기서 임시 메모를 떼어 내고 **원래 메모** ("첫인사 이어서 하기") 를 보여 준다. 그래서 두 번 거친다.

### 2.9 둘째 부름 — 6 → 9, 그리고 bind `[정적]`

run 문서 1-1 · 1-2 B6 ~ B9.

#### 새 말 — 가상표

- 객체 종류마다 하나씩 있는 **함수 주소 목록** `[추측]` (일반적으로 알려진 설명). "`[vtbl+0x188]`" 은 그 목록의 `0x188` 번째 칸에 적힌 함수를 부른다는 뜻이다.
- 비유: 교환원 (간접 호출) 이 보는 **내선표**. 칸 번호만 알면 누구에게 이어 줄지 안다.
- 이번엔 PE 데이터 (`.rdata`) 에서 칸을 직접 읽었다 `[정적: PE]`: `LRPC_FAST_BIND_CCALL` (`0xe2590`) 의 `+0x188` = `0x93cc0` (Bind) · `+0x190` = `0x76630` (`CallBound`) · `+0x128` = `0x77210` (`AbortCall`).

#### 한 걸음씩 (일꾼)

1. `DriveStateForward` 둘째 부름 — 맨 앞 `+0x69 ~ +0x82` 가 짝 6 → 7 → `+0x551 [바인딩+0x114] = 8` → 상태 8 몸.
2. `+0x589 EnableAsyncIfNecessary` — 연결 `[+0xd0] & 8` 이 이미 켜져 있으면 (`Connect +0x3b1` 이 첫 연결 때 켬) `EnableAsync` 를 안 부르고 바인딩 깃발 `[+0x178] |= 1` 만 (`+0x59`). **syscall 없음, 새 `CreateAlpc` 없음.**
3. 2.4 의 고리: `[R13]` → (`EqualSid` 같음) → 맨 앞 → 8 → 9 → 상태 9 몸.
4. **`+0x35d OpenSecurityContextWorker`** — 보안 문맥 (2.10).
5. `+0x3c6` 가상 `[바인딩 vtbl+0x1e0]` → 상태 9 → `+0x1f1 CMP EDX,9 / +0x1f4 JZ → +0x239` → 반환 (`+0x266`).
6. `DriveStateForwardAsyncCallback` 으로 돌아와 반환 0 → **`+0xe0` 가상 `[vtbl+0x188]` = `Bind__93cc0`** → `+0x30 BaseBind` → `+0x1e4 LRPC_CASSOCIATION::Bind` → **`+0x626 NtAlpcSendWaitReceivePort(포트, Flags 0, bind 0x70 B, …, 받기 NULL)` = bind**.
7. `+0x104` 가상 `[vtbl+0x190]` = `CallBound(호출, 1)`.
8. 그 뒤 bind 답 · 요청 · 응답은 run137 5절 ③ ~ ⑨ (다시 안 함).

- **bind 를 보내는 스레드 = 풀 일꾼** `[정적]` — 연결 응답 쪽지를 받은 일꾼이 자기 몫으로 이어서 하거나, 아니면 `NtReleaseWorkerFactoryWorker` 로 깨운 다른 일꾼. **첫 연결 때 한정.**
- 이걸로 run133 의 "`[추측]` bind 둘이 비동기" 중 **epmapper bind 는 `[정적]`** 이 됐다 (Flags 0). nsi bind 는 이번에 안 따라가서 그대로 `[추측]`.

### 2.10 보안 문맥 — 비동기도 같은 코드 · 같은 차례 `[정적]`

run 문서 3절.

#### 새 말 — 보안 문맥 · 가장

- **가장 (impersonation)** = 스레드가 잠시 **다른 신분증을 빌려 드는 것** `[추측]` (일반적으로 알려진 설명). 여기서는 `NtSetInformationThread(현재, 5, &토큰, 8)` 로 한다 (5 = `ThreadImpersonationToken` `[추측: 이름]`).
- **보안 문맥 (ALPC security context)** = 전화선 (포트) 에 "**지금 이 신분으로 말합니다**" 를 묶어 두는 것 `[추측]` (일반적으로 알려진 설명). `NtAlpcCreateSecurityContext(포트, 0, …)` 로 만든다.
- 비유: 첫인사 전에 **신분증을 빌려 들고 → 전화선에 "이 신분" 도장 → 신분증 돌려놓기**.

#### 갈림 — 줄여서 (전체는 run 문서 3절 표)

`OpenSecurityContextWorker` (rva `0x4599c`, 645 B) 엔 **동기 · 비동기 인자가 없다.** 동기냐 비동기냐는 "어느 스레드가 부르느냐" 일 뿐이다. 부르는 자리는 `DriveStateForward +0x35d` 하나.

| 차례 | 줄 | syscall | 동기 로그 |
|---|---|---|---|
| ① 만들기 자체 | `DriveStateForward +0x327 / +0x32a` — `[바인딩+0x128] == 1` 이면 **안 만든다** | 없음 | — |
| ② 지금 스레드 신분증 열기 | `+0x52` | `NtOpenThreadToken(스레드, 0xc, 1, &토큰)` | 5619 |
| ③ 신분증 빌려 들기 | `+0xb4` | `NtSetInformationThread(현재, 5, &토큰, 8)` | 5620 |
| ④ 전화선에 도장 | **`+0x124`** | **`NtAlpcCreateSecurityContext(포트 [연결+0x88], 0, [문맥+8])`** | 5621 |
| ⑤ 돌려놓기 | `+0x14d RestoreOldThreadToken` → `+0x3f` | `NtSetInformationThread(-2, 5)` | 5622 |

- ② 의 결과에 따라 갈린다: `0xc000007c` (토큰 없음) 는 계속, `0xc0000022` 는 오류 5, 그 밖 낯선 값은 `0xe` 로 끝 (전체는 run 문서).
- 어느 갈래를 타는지는 `[바인딩+0x180]` · `+0x128` 실제 값이 필요해 `[공백]`. 동기 로그가 `NtOpenThreadToken` 뒤 곧바로 `NtSetInformationThread(-2, 5)` 라 "토큰 없음 (`0xfffffffd`)" 갈래로 읽힌다 `[추측]`.

#### 새 말 — `AbortCall`

- 호출 객체 가상표 `+0x128` 칸 (`0x77210`). 호출을 **오류로 끝내는** 함수 `[정적: PE]` (이름은 PDB 가상표).
- 비유: 창구가 "이 신청은 반려합니다" 도장을 찍는 것.

#### 실패하면 — bind 를 안 보낸다

1. `OpenSecurityContextWorker` 가 0 이 아니면 `DriveStateForward +0x364 / +0x366 JNZ → +0x253` (CS 풀고 `+0x266` 반환).
2. `DriveStateForwardAsyncCallback +0xbd JNZ → +0x21` → `+0x43` 가상 `[바인딩 vtbl+0x1b8]` (오류 종류).
3. 다시 시도할 수 있으면 `+0x6b [+0x1c0] PrepareForRetry` → `+0x78 JZ → +0x1d` 다시 `DriveStateForward`.
4. 아니면 `+0x7a ~ +0x93` `[호출 vtbl+0x128]` = **`AbortCall`**. → `+0xe0` bind 로 **못 간다**.

- **차례는 동기와 같다** — 로그 5619 → 5622 ↔ ② → ③ → ④ → ⑤. 비동기는 **일꾼에서, 연결 응답 뒤, bind 바로 앞.**

### 2.11 ★ 일꾼 쪽 syscall 차례 — 에뮬레이터가 보게 될 줄

run 문서 "PE_emulator 쪽" 할 것 · 1-2 표.

연결 응답이 들어온 뒤, 일꾼 쪽에서 나와야 할 syscall 을 차례대로 (run 문서 그대로):

| # | syscall | 어디서 | 장면 |
|---|---|---|---|
| 1 | `NtWaitForWorkViaWorkerFactory` → 0, 항목 1 | `TppWorkerThread +0x379` | 2 |
| 2 | `NtAlpcQueryInformation(포트, 0, …)` | `IoCompleted +0x6a` → `IsPortWithSharedSection` | 2 |
| 3 | `NtAlpcSendWaitReceivePort` (받기만) → 종류 `0xb` | `IoCompleted +0xc8` | 2 |
| 4 | (맡김) **없음** 또는 `NtReleaseWorkerFactoryWorker` 1 | `TppWorkPost` | 2 |
| 5 | `NtOpenThreadToken` | `OpenSecurityContextWorker +0x52` | 3 |
| 6 | `NtSetInformationThread(-2, 5)` | `+0xb4` | 3 |
| 7 | `NtAlpcCreateSecurityContext` | `+0x124` | 3 |
| 8 | `NtSetInformationThread(-2, 5)` | `RestoreOldThreadToken +0x3f` | 3 |
| 9 | **`NtAlpcSendWaitReceivePort(Flags 0, bind 0x70 B, 받기 NULL)`** | `LRPC_CASSOCIATION::Bind +0x626` | 3 |

- 그 뒤는 run137 5절 ③ ~ ⑨.
- 4 가 "없음" 이면 1 ~ 9 가 **한 일꾼** 에서 이어진다. 4 가 `NtReleaseWorkerFactoryWorker` 면 5 ~ 9 는 깨어난 **다른 일꾼** 일 수 있다 (2.9).

#### 동기 손님과 나란히

| | 동기 손님 (한 스레드) | 비동기 손님 |
|---|---|---|
| 연결 | 5617 `NtAlpcConnectPort` Flags `0x20000` | 부른 쪽: 2191 Flags 0 |
| 묻기 | 5618 | 부른 쪽: 2192 · **일꾼: 한 번 더** (#2) |
| 우편함 붙이기 | 없음 (동기는 `EnableAsync` 안 부름) | 부른 쪽: 2193 (+ 첫 묶임이면 종류 3) |
| 연결 끝 알기 | `Connect +0x1c5` 가 바로 `CompleteOnePart` | **일꾼**: 쪽지 → 받기만 → `0xb` (#1 · #3) |
| 신분증 네 줄 | 5619 ~ 5622 | **일꾼** (#5 ~ #8) |
| bind | 5623 Flags `0x20000` | **일꾼** (#9) Flags 0 |

비유: 동기 손님은 창구 앞에 서서 전화 연결 → 신분증 → 첫인사를 혼자 다 한다. 비동기 손님은 전화만 걸어 두고 자러 가고, 나머지 셋은 "연결됐어요" 쪽지를 받은 일꾼이 대신 한다.

### 2.12 ★ 에뮬레이터에서 일꾼이 다 죽은 직접 원인 — 빈 우편함에서 곧바로 `0x102` `[정적]`

run 문서 2-2 · "PE_emulator 쪽" 표.

#### 진짜 일꾼은 쪽지가 올 때까지 잔다

- run125: `NtWaitForWorkViaWorkerFactory` 는 `IoRemoveIoCompletion` 을 **시간 제한 없이** 부른다 `[정적]`. 빈 우편함 앞에서는 그냥 기다린다.
- `0x102` (줄이기) 는 커널이 따로 정한다. 놀기 제한 (파일 값 약 67 초) 이 지나고, `ExpWorkerFactoryWantsToCreate` 가 거짓이고, 움직이는 일꾼 수가 최소보다 많을 때만 (run125 해설 2.11 `0x102` ②).

#### `0x102` 를 받은 일꾼은 큐를 안 보고 끝난다

`TppWorkerThread` 의 돌아온 뒤 갈래 (run 문서 2-2 표):

| 돌아온 값 | 줄 | 하는 일 |
|---|---|---|
| 0, 항목 있음 | `+0x37e ~ +0x402` → … → `+0x55e` | 콜백 실행 (그 뒤 2.8 의 카운터 확인) |
| 0, 항목 없음 (빈 쪽지) | `+0x485 ~ +0x490 JZ → +0x6ea` | 곧바로 `TppWorkerFindTask` |
| `0x80` (마감) | `+0xc03 / +0xc09 JZ → +0xe13` | 끝내기 길 |
| `0xc0` (사용자 APC) | `+0xc0f / +0xc12 JZ → +0xdc0` → … → `+0x2e9` | 다시 기다림 |
| **`0x102` (쉼 시간 끝)** | **`+0xc18 CMP ECX,0x42 / +0xc1b JZ → +0xda4`** → `+0xe13` → `+0xec4 TppPoolRemoveWorker` · … · `+0xf65 TppFreeThreadData` → **`+0xfce RtlExitUserThread`** | **유저 큐를 안 보고 끝난다** |

- run125 해설 2.11 끝의 `[추측]` "`0x102` 를 받은 일꾼이 정말 스레드를 끝내나" — 이번에 `0x102` 쪽은 코드로 보였다: **끝낸다** `[정적]`.
- "유저 큐에 일이 있으면 안 끝나나?" → 유저 쪽은 안 본다. 일이 있을 때 `0x102` 를 **안 주는 건 커널** 이다. `ExpWorkerFactoryWantsToCreate(공장, 1)` 가 참이면 줄이지 않는다:
  - 거짓: `[공장+0x190] ≠ 0` (`+0x3 ~ +0xa`) · `[공장+0x180] ≥ [공장+0x17c] (+1)` (`+0x12 ~ +0x25`) · 기다리는 일꾼 `[P+0x1c]` > 1 (`+0x27 ~ +0x2f`).
  - **참: 포트 큐 `[포트+4] ≠ 0` (`+0x35 ~ +0x3f`)** · **풀어주기 수 `[P+0x18] > 0` (`+0x43 ~ +0x47`)**. `[P+0x18]` 은 `NtReleaseWorkerFactoryWorker +0xea ~ +0xfb` 가 올린다.

#### 그래서 에뮬레이터는 — 한 걸음씩

1. 에뮬레이터 일꾼 둘은 `NtWaitForWorkViaWorkerFactory` 에서 **빈 우편함인데 곧바로** `0x102` 를 받았다 (로그).
2. 진짜라면 여기서 시간 제한 없이 잤을 것이다 (run125).
3. 유저 쪽 `TppWorkerThread` 는 `0x102` 를 받으면 **큐를 안 보고 `RtlExitUserThread`** 로 간다 (위 표). 로그의 `NtTerminateThread` 가 그 끝이다.
4. → **일꾼이 다 죽는 직접 원인 = 빈 포트에서 `0x102` 를 바로 주는 것** (run 문서 판정 "틀림").
5. 그리고 따로, **멈춤 자체의 직접 원인 = 연결 응답 메시지 · 쪽지가 없는 것** (판정 "틀림 (빠진 것)"). 일꾼이 살아 있었어도 깨울 쪽지가 안 오니 bind 는 안 나간다.

비유: 진짜 파견소 일꾼은 빈 우편함 앞에서 **쪽지가 올 때까지 기다린다**. "67초 넘게 놀았고, 우편함도 비었고, 사람도 넉넉하면" 그때 한 명씩 퇴근시킨다. 에뮬레이터 파견소는 일꾼이 우편함 앞에 서자마자 "**빈손이네, 퇴근해**" 라고 했다. 일꾼은 퇴근할 때 우체통을 돌아보지 않는다. 게다가 114 의 "연결됐어요" 쪽지는 처음부터 오지도 않았다.

### 2.13 종류 3 (`NtSetInformationWorkerFactory(공장, 3, &1, 4)`) — 첫 묶임에 한 번, `0x102` 와는 무관 `[정적]`

run 문서 4절. 에뮬레이터 로그엔 이 줄이 없었다.

#### 언제 나오나 — `ntdll!TpAdjustBindingCount(풀, 증감)` (rva `0xc9b90`)

1. `+0x4 TEST EDX / +0x6 JZ` — 증감 0 이면 아무것도 안 함.
2. `+0xa XADD.LOCK [풀+0x1b0], 증감` — 옛 값 / 새 값.
3. **옛 ≤ 0 이고 새 > 0** → `+0x3b NtSetInformationWorkerFactory([풀+0x38], 3, &1, 4)`.
4. **옛 > 0 이고 새 > 0** → syscall 없음 (`+0x1a TEST R8D / +0x1d JG`).
5. 옛 > 0 → 새 ≤ 0 (마지막 묶임이 풀릴 때) 만 `-1`.

- 풀은 0 채움 (`TpAllocPoolInternal +0xa3 OR EDX,8`) 으로 시작하니 **그 풀의 첫 묶임에 한 번만** 나온다.
- 묶이는 것: ALPC 완료 · 파일 IO · 잡 알림 · `RtlpTpIoDllProcessUnloads`.
- 로그에 없는 까닭 후보 `[추측]`: (a) 같은 프로세스 앞쪽 묶임 때 이미 나왔다 — 로그 앞쪽에서 `(…, 3, …)` 를 찾아 볼 것 (b) 풀이 0 으로 시작하지 않았다.

#### 커널 뜻 — 묶음 수 (run125 해설 2.5 "멀리 있기 · 묶음 수")

- 종류 3 = `공장+0x194` (묶음 수 `[추측: 이름]`) 에 부호 있는 값을 더한다 (`+0x404` 옛 값 · `+0x410 JNS → +0x565` 더하기 · `+0x570` 저장, 빼기는 `+0x416 ~ +0x41c` · 0 밑으로 안 감, 넘치면 `0xffffffff`).
- **옛 0 → 새 ≠ 0** 이면 `+0x582 ExpTryEnterWorkerFactoryAwayMode` → 참이면 `+0x59a ExpWorkerFactoryCheckCreate`.
- 묶음 수 ≠ 0 이고 포트가 비었으면 "멀리 있기" 로 들어가 `+0x97 KeRegisterObjectNotification(포트, …)` 로 **우편함에 알림을 걸어 둔다**. 패킷이 들어오면 순찰 직원 (`ExpWorkerFactoryManagerThread +0xc3 CheckCreate`) 이 일꾼을 만든다.
- 결론: 묶음 수 ≠ 0 = "이 풀의 우편함에 **커널이 직접 쪽지를 넣는 것** 이 묶여 있다 → 일꾼이 하나도 없을 때 쪽지가 오면 만들어라".
- **`0x102` 에 일꾼을 남기는 칸은 아니다.** `0x102` 를 정하는 `ExpWorkerFactoryWantsToCreate` (2.12) 는 `[공장+0x194]` 를 안 읽는다.

비유: 묶음 수는 파견소 장부의 "**우편함에 전화선 · 파일이 몇 개 묶여 있나**" 숫자다. 0 이 아니면 "일꾼이 다 퇴근했어도, 쪽지가 오면 순찰 직원이 한 명 불러와라" 는 뜻이다. "퇴근시키지 마라" 는 뜻은 아니다.

### 2.14 `NtAlpcQueryInformation(0)` 버퍼 안 쓴 성공 — 길은 안 바뀐다 `[정적]`

run 문서 5절. `IsPortWithSharedSection` (rva `0x58018`, 83 B):

1. `+0x8 / +0xb` 버퍼 16 B 를 **0 으로 먼저** 채운다.
2. 포트 핸들 0 이면 반환 1.
3. `+0x25 NtAlpcQueryInformation(포트, 0, &버퍼, 0x10, NULL)`.
4. 실패면 반환 1. 성공이면 `+0x35 TEST [버퍼],0x1000000` — **bit 24 켜졌으면 0**, 아니면 1.

- 그래서 "0 돌려주고 버퍼 안 씀" = 버퍼가 0 그대로 = **반환 1** (받기 크기 `0x1000`).
- 갈림은 **받기 버퍼 크기 하나** (`0x1000` / `0xff00`). `AlpcConnect +0x395 / +0x39f` 의 비동기 판정은 이 값과 **무관** 하다.
- 덤: 일꾼 `IoCompleted +0x6a` 도 같은 부름을 **한 번 더** 한다 (2.11 #2). 진짜 Windows 가 bit 24 를 켜 주는지는 동적 (Q271 7).

### 2.15 PE_emulator 판정 — 맞음 · 틀림 (run 문서 "PE_emulator 쪽")

`../PE_emulator` 코드는 flow-writer 가 안 읽었다. **고칠지는 사용자 결정** 이다.

| 로그 줄 (에뮬레이터) | 판정 | 한 줄 |
|---|---|---|
| 2185 `NtCreateEvent` | **맞음** | winnsi `+0x168` (run129) |
| 2191 `NtAlpcConnectPort(Flags 0, SID 있음, …)` | **맞음** | `AlpcConnect` 인자 줄과 같음 |
| 2192 `NtAlpcQueryInformation(0)` 버퍼 안 씀 | **맞음, 길 안 바뀜** | 2.14. 일꾼에서 한 번 더 나온다 |
| 2193 `NtAlpcSetInformation(포트, 2, …)` | **맞음** | run137 |
| 맡김 쪽 syscall 0 · `NtSetIoCompletion*` 0 | `NtSetIoCompletion*` 은 **맞음** | `NtReleaseWorkerFactoryWorker` 는 **0 ~ 1** 번 — 둘 다 받을 준비 (2.7) |
| `(…, 3, …)` 없음 | **조건부로 맞음** | 첫 묶임 때만 (2.13) |
| 보안 문맥 줄 없음 | **지금은 맞고, 고치면 나와야 함** | 2.10 · 2.11 |
| **연결 응답 · 쪽지 없음** | **틀림 (빠진 것) — 멈춤의 직접 원인** | 2.5 |
| 일꾼 `→ 0x102 → NtTerminateThread` | **틀림** | 빈 포트에서 바로 `0x102` — 일꾼이 다 죽는 직접 원인 (2.12) |
| 부른 쪽 `NtWaitForSingleObject(기한 없음)` | **맞음** | 진짜도 bind · 요청 · 응답이 끝날 때까지 여기서 잔다 (winnsi `+0x239`) |

#### 할 것 (run 문서 요약)

- **`epm.cpp accepts_async_connect`**: 비동기 연결 (Flags 에 `0x20000` 없음) 을 받으면 핸들 · 0 을 돌려주고, **클라이언트 포트 받기 줄에 연결 응답 메시지 하나** 를 넣는다.
  - PORT_MESSAGE 헤더 `0x28` B, `[+0x04] & 0xff == 0xb`, 나머지 0 으로 충분 — `IoCompleted` 는 이 종류에서 길이 · 본문을 안 본다. 받기 속성도 0 으로 충분 (`+0x3bb AND R10D,0x50000000`).
  - 진짜 커널이 넣는 헤더 칸 값은 `[공백]` (Q272 1).
- **`src/threadpool/` 우편함**:
  - `NtAlpcSetInformation(2)` 가 포트에 (우편함, 꼬리표) 를 단다. 이미 줄에 있는 메시지 수만큼 (그리고 이후 들어올 때마다) 쪽지 `{꼬리표, 0, 0, 0}`.
    - 붙이기 전에 쌓인 메시지는 붙이는 순간 개수만큼 쪽지가 나간다 (run137 1-3 의 11) → **줄에 먼저 넣어 두면 어느 순서든 맞는다**.
  - 일꾼 `NtWaitForWorkViaWorkerFactory` 는 쪽지가 올 때까지 **잔다** (빈 포트에서 바로 `0x102` 금지).
  - 일꾼이 하나도 없는데 쪽지가 오고 묶음 수 ≠ 0 이면 일꾼 하나를 만든다 (2.13).
- **종류 3** (4 B, 부호 있음) = `[공장+0x194]` 에 더하기. 옛 0 → 새 ≠ 0 이면 "멀리 있기" 판정, 옛 ≠ 0 → 새 0 이면 해제.

비유: 에뮬레이터 관리사무소엔 지금 **114 의 "연결됐어요" 메시지를 놓아 주는 손이 없고**, 파견소는 **일꾼이 오자마자 퇴근시킨다**. 이 둘만 고치면 나머지 (일꾼 → 편지 → 신분증 → 첫인사) 는 rpcrt4 · ntdll 진짜 코드가 알아서 돈다.

### 2.16 앞 run 고쳐 읽기 — 닫힘과 바뀐 곳

run 문서 "고쳐 읽기 (앞 run)". 각 문서 맨 위에 "고쳐 읽기 (run141)" 한 줄이 붙었다.

| 앞 run | 전 | 후 |
|---|---|---|
| **run133** | "`DriveStateForward` 의 인자 전달을 안 읽음" `[공백]` · "bind 둘 (epmapper · nsi) 이 비동기" `[추측]` | 인자 전달 **닫힘** `[정적]` (2.3 `fAsync`). **epmapper bind 는 `[정적]`** (일꾼에서 Flags 0). nsi bind 는 그대로 `[추측]`. 동적 1 은 남되 "보내는 스레드 = 일꾼" 도 함께 (Q271 3) |
| **run129** | `RpcBindingBind +0x4b` 가상 `[RAX+0x128]` 구현 이름 `[추측]` | `LRPC_FAST_BINDING_HANDLE::Bind` (`Bind__74b50`, 가상표 `0xe4440` 슬롯 `+0x128`) `[정적: PE]`. epmapper 핸들이 빠른 핸들이라는 건 `[추측]` 으로 남음 |
| **run137** | 5절 표: ①c (붙이기) 바로 뒤 ③ (114 첫인사), 보내는 쪽 = 부른 쪽처럼 읽힘 | **그 사이에 연결 응답이 빠져 있었다**: 서버 수락 → 종류 `0xb` 메시지 + 쪽지 → 일꾼 `IoCompleted` (+ `NtAlpcQueryInformation(0)` 한 번 더) → `CompleteOnePart` → 맡김 → 보안 문맥 4 줄 → ③ bind. ③ 을 보내는 쪽은 **일꾼**. `IsPortWithSharedSection` 부름은 `IoCompleted +0x6a` 에도 있다. (①d 첫 묶임 조건은 그대로 맞음) |

- run137 해설 2.16 표를 읽을 때는 ①d 와 ③ 사이에 이번 2.11 의 #1 ~ #8 이 들어간다고 보면 된다.

### 2.17 flow-writer 대조 — 결과서와 다르게 적은 곳 (뜻은 같음)

run 문서 "flow-writer 대조".

- `DriveStateForward +0x8f1 ReinitCommon` — `+0x8f1` 은 `+0x4c0 JNZ` 가 가는 자리이고 **call 은 `+0x8f4`** (ret `+0x8f9`). 다른 두 자리도 call 은 `+0x90f` · `+0x946`.
- `TppWorkPost` 자기 몫 검사는 `[TEB+0x1778]` 을 **한 번 더 따라간다** (`+0x458 MOV RAX,[RAX]`). 칸은 `[[TEB+0x1778]+0]` 이 가리키는 것의 `+0x30` · `+0x80` 이다. `TppAllocThreadData` 는 `+0` 을 안 쓰니 결과서 "다른 칸" 은 맞고, 더 정확히는 **다른 객체**. `+0` 을 채우는 줄은 `[공백]` (Q272 2).
- (보탬) `fAsync` 출처를 코드 줄로 — 2.3 "새 말 — `fAsync`".
- `NtWaitForWorkViaWorkerFactory +0x588` 은 `XADD.LOCK [RAX+0x194],EDI` — 읽기 + 쓰기다. 이 길 아님이라 뜻은 그대로.
- `ExpWorkerFactoryWantsToCreate` 첫 비교는 `+0x3 CMP [RCX+0x190],R9D / +0xa JNZ` (`+0x0` 은 `XOR R9D,R9D`).
- kernelbase · kernel32 `TrySubmitThreadpoolCallback` 바이트는 결과서가 파일로 안 남겨 flow-writer 는 다시 못 봤다.
- 그 밖 결과서 오프셋 · 점프 목적지는 `.asm` 과 **일치**.

### 2.18 정리 — 비유로 한 번 더

- 신청 담당 (winnsi) 이 114 에 **비동기로** 전화를 건다. 창구에 "다 되면 이 번호로 연락 주세요" 메모 (콜백형 IO) 를 남긴다.
- 전화 부서는 전화선을 열고, 우편함에 붙이고, "번호표 받고 앉아 계세요" (보류) 를 받은 **그 자리에서** 돌아온다. 연락 카드 입구는 이걸 "접수됐어요" (0) 로 바꿔 전한다. 신청 담당은 신호등 앞에서 잔다. **첫인사는 아직 아무도 안 했다.**
- 요청서는 "포스트잇 (`+0x5d4`) 붙인 뒤 누구에게 넘기나" 를 찾았지만, 그 줄은 "**메모판 맨 위로 돌아가라**" 였다.
- 114 가 "연결됐어요" (종류 `0xb`) 를 보내면 관리사무소가 우편함에 쪽지를 넣는다. 파견소 일꾼이 쪽지를 꺼내 전화선을 들여다보고, 메모판에 "연결됨" 을 적고, 알림을 돌린다.
- 알림을 받을 사람이 메모를 남겼으니, 일꾼은 "이 메모대로 연락하기" **편지를 우체통에 넣는다** (`SignalAsync` → `TrySubmitWork` → `TpSimpleTryPost` → `TppWorkPost`). 자기가 바로 집을 거면 파견소를 안 부른다.
- 편지를 집은 일꾼이 메모판을 9 까지 넘기고, **신분증을 빌려 전화선에 도장** 을 찍고 돌려놓은 뒤, **첫인사** (bind) 를 한다.
- 에뮬레이터에선 ① "연결됐어요" 쪽지가 안 왔고 ② 일꾼이 빈 우편함 앞에서 곧바로 퇴근했다. 그래서 신청 담당은 영영 잔다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 32795e6 -- runs/03_sprint/flow.md`)

그림은 안 바뀌었다. 바뀐 곳:

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — ntkrnlmp · ntdll · rpcrt4 PDB 줄 | 앞 run 까지 | 각각 run141 몫 덧붙임 (다시 analyze 안 함, export · 읽기만 한 함수 목록) |
| 기준 — 근거 줄 | — | **(run141) 정적 바퀴, 사진 값 대조 없음** — test.exe 는 rpcrt4 · winnsi 를 안 올림 `[관찰]` · RPC API 없음 `[소스]` |
| 3장 "일꾼 공장" 절 (run125) | — | + 한 줄: 종류 3 = 묶음 수, "멀리 있기" 에만 · `0x102` 판정엔 안 쓰임, `TppWorkerThread +0xc1b` 는 `0x102` 면 큐 안 보고 끝, `TppWorkPost` 자기 몫이면 `+0x4f3` 건너뜀 |
| 3장 "LRPC 선 위 형식" 절 (run133) | `[추측]` bind 둘 (epmapper · nsi) 도 비동기 | 취소선 → **epmapper bind `[정적]`** (일꾼, Flags 0), nsi bind 는 그대로 `[추측]` |
| 3장 "비동기 LRPC 답 배달" 절 (run137) | — | + 한 줄: 붙이기와 bind 사이를 미는 것 = 연결 응답 메시지 + 쪽지, run137 5절 표 ①c ~ ③ 사이가 빠져 있었다 |
| 3장 run137 절 뒤 | — | **새 절 "비동기 epmapper 연결 뒤 bind 를 미는 길"** — 비유 한 줄, 나무 하나, 요약 아홉 (첫 부름 · `+0x5d4` · 맡김 · syscall 없는 맡김 · `TppWorkerThread` · 보안 문맥 · 종류 3 · `NtAlpcQueryInformation(0)` · PE_emulator) |
| 4장 run133 동적 줄 | — | + "(run141) 1 은 bind 를 보내는 스레드 (일꾼 TID) 도 같이 — Q271 3" |
| 4장 run137 동적 줄 | — | + "(run141) 3 · 7 은 Q271 6 · 7 과 같은 자리 — 한 세션에" |
| 4장 run137 정적 줄 (Q259) | — | + "(run141) **③ 일부 닫힘**" |
| 4장 새 줄 | — | **Q267 해결됨 줄** + **run141 동적 (Q271) · 정적 (Q272) · PE_emulator** |
| 5장 근거 run | — | run141 줄 둘 |

- 태그 변화:
  - run133 "`DriveStateForward` 인자 전달" — `[공백]` → **`[정적]`**.
  - run133 "epmapper bind 가 비동기" — `[추측]` → **`[정적]`**. (nsi bind 는 `[추측]` 그대로.)
  - run129 `RpcBindingBind +0x4b` 가상 대상 이름 — `[추측]` → **`[정적: PE]`** (`Bind__74b50`).
  - run125 해설 2.11 끝 "`0x102` 를 받은 일꾼이 정말 끝내나" `[추측]` — 이번에 `0x102` 쪽 답이 **`[정적]`** 으로 생겼다 (`+0xfce RtlExitUserThread`, flow.md 는 run125 절에 한 줄 덧붙임).
  - Q259 ③ — 종류 3 의 뜻 · `0x102` 를 정하는 칸은 **`[정적]`** 로 닫힘. `TppPoolpReferenceGlobalPool` 접점은 그대로.
  - 새로 남은 `[공백]` / `[추측]`: 커널이 넣는 연결 응답 (`[추측: 커널 안 읽음]`) · 그 헤더 칸 · `[…+0x80] = 3` 을 쓰는 줄 · `[바인딩+0x128]` · `+0x180` 실제 값 · 풀 `[+8]` 칸 이름 · epmapper 바인딩이 빠른 핸들인지.
- 짐 지도 (`baggage.md`) 는 안 바뀌었다 — 초기화 동안의 짐이 아니다.

### 큰 그림에서 어떻게 이어지나

- 흐름도 본선 (프로세스 생성 → `NtContinue` 은퇴 → `main`) 엔 **변화가 없다**. 이번도 `main` 뒤 옆길의 지도다.
- **run128 → run129 → run133 → run137 → run141** 로 "웹 요청 하나가 망 안내 쪽에 묻는 전화" 의 **마지막 빈칸** 이 찼다.
  - run129: 전화를 건다 (연결 · 메모판). 비동기면 우편함에 알림을 건다.
  - run137: 답장이 어떻게 우편함 → 일꾼 → 전화 부서로 돌아오나.
  - **run141: 붙이기와 첫인사 사이 — "연결됐어요" 가 일꾼을 깨우고, 일꾼이 첫인사를 대신 한다.**
  - → run137 해설 2.16 서버 흉내 차례 표가 ① → ①b → ①c → ①d → **(연결 응답 → 일꾼 → 신분증)** → ③ … 로 **끊김 없이** 이어진다.
- **일꾼 사무실 지도와 더 단단히 이어졌다.**
  - run72 의 우체통 (`TppWorkPost`) 에 "자기 몫이면 파견소를 안 부른다" 갈래가 보였다.
  - run125 의 `0x102` 가 유저 쪽에서 **정말 퇴근** 으로 이어진다는 게 코드로 보였다.
  - 이 몸들 (`TppWorkPost` · `TppWorkerThread` · 커널 일꾼 공장) 은 test.exe 로더 풀 일꾼과 **같은 몸** 이다 (run 문서 "지도 범위"). 다만 이번 갈래 (연결 응답 · 자기 몫) 는 test.exe 초기화에서 안 탄다.
- PE_emulator 에게는 고칠 곳이 **둘** 로 좁혀졌다 (2.15): 연결 응답 메시지 하나 넣기 · 빈 우편함에서 일꾼 재우기.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q271** (동적, Q258 세션과 합침 — 미룸)

- **왜 이번에 못 알았나**: 정적만 했다. 어느 갈래를 실제로 타는지 · 진짜 메시지 바이트 · 스레드 TID 는 실행 때 값이다.
- **무엇이 필요한가**: nsisvc 가 도는 VM + **winnsi 등록을 부르는 프로그램** (test.exe 는 안 부름, 고르기는 사용자 결정). 유저 멈춤점은 그 프로세스 조건, ret 자리.

| run 문서 | 자리 | 무엇을 보나 | 닫는 것 |
|---|---|---|---|
| 1 | `rpcrt4!LRPC_BASE_BINDING_HANDLE::DriveStateForward+0x51a` | `eax == 0xc0021018` · TID (= 부른 쪽) | 첫 부름이 보류로 끝나나 (2.3) |
| 2 | `rpcrt4!LRPC_CASSOCIATION::IoCompleted+0x184` (`esi == 0xb`) | `db @rbx L0x28` · `[rbp-0x55]` | **연결 응답의 진짜 꼴** (2.5) |
| 3 | `SignalAsync+0x2f` TID · `ntdll!TppWorkPost+0x4cd` `r8d` · `+0x4f3` · `DriveStateForwardAsyncCallback+0xb0` · `+0xe0` · `rpcrt4+0x4e330+0x626` | TID 들이 같은가, `r8d` 1 인가 | **자기 몫인가 · bind 를 보내는 일꾼** (2.7 · 2.9) |
| 4 | `DriveStateForward+0x5cf` `[r13]` · `+0x622` · 둘째 부름 `+0x69 ~ +0x82` | 요구 SID · `EqualSid` 반환 · 짝 | `+0x5d4` 가 탄 쪽 (2.4) |
| 5 | `OpenSecurityContextWorker+0x59` `eax` · `+0x89` `r15` · `DriveStateForward+0x327` `edx` | 토큰 갈래 · `[바인딩+0x128]` | 보안 문맥 갈림 (2.10) |
| 6 | `ntdll!TpAdjustBindingCount+0x16` `eax` | 옛 묶음 수 | 첫 묶임 (2.13). run137 동적 3 (Q258 3) 과 같은 자리 |
| 7 | `rpcrt4!IsPortWithSharedSection+0x35` `dd @rsp+0x30 L1` | bit 24 | 받기 크기 (2.14). run137 동적 7 (Q258 7) 과 같은 자리 |
| 8 | `rpcrt4!RPC_THREAD_POOL::TrySubmitWork+0x26` 대상 | kernelbase / kernel32 | 맡김 다리 (2.6) |

### 정적으로 남은 것 → **Q272**

| 번호 | 무엇 | 왜 이번에 못 했나 |
|---|---|---|
| 1 | ntoskrnl 비동기 `NtAlpcConnectPort` 안쪽 (`AlpcpConnectPort` · `AlpcpProcessConnectionRequest` · `AlpcpDispatchConnectionRequest` · `AlpcpReceiveLegacyConnectionReply` · `NtAlpcAcceptConnectPort`) — 종류 `0xb` 메시지 헤더 칸, 쪽지가 하나만 나가는지 | 이번 질문은 받는 쪽 (rpcrt4) 이었다. **PE_emulator 가 메시지를 지어 넣을 때 필요해 가장 급하다** (plan) |
| 2 | ntdll `[…+0x80] = 3` 을 쓰는 줄 · `[[TEB+0x1778]+0]` 을 채우는 줄 · `TppWorkerFindTask` · `TppPrepareDirectParams` | 자기 몫 갈래를 정적으로 닫으려면 필요 (2.7) |
| 3 | `BindToEpMapper → RpcBindingCreateW` 가 채우는 `[바인딩+0x128]` · `+0x180` · `+0x12c` | 보안 문맥 갈래 확정 (2.10) |
| 4 | `CompleteOnePart` 실패 갈래 (연결 거절 `0xc0000041` → 오류 `0xe`) · `AbortCall` 뒤 winnsi 쪽 결과 | 이번엔 성공 길만 따라갔다 |

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- epmapper 바인딩이 빠른 핸들이라는 것 (간접 근거만).
- 풀 `[+8]` 칸 이름들 (깨워 둔 수 · 쉬는 수 · 줄 카운터).
- `NtWaitForWorkViaWorkerFactory +0x588` 갈래 (ALPC 보내기와 합친 다른 길).
- nsi bind 가 비동기인지 (이번에 안 따라감).
- `Complete +0x1ae IsRetryErrorCode` 갈래 (본문 안 읽음).

---

## 5. 더 읽기

- run 문서: [`run141-async-bind.md`](run141-async-bind.md)
  - "정적 — 호출 지점" 표 (call · ret 짝 전부), **1-2 비동기 bind 차례표 (A1 ~ C, 동기 손님과 나란히)**, 2-1 맡김 → 깨움 표, **2-2 `TppWorkerThread` 돌아온 뒤 표**, 3절 보안 문맥 갈림 표, 4-1 · 4-2 종류 3, 5절, "정정", "PE_emulator 쪽" 표
- 결과서: [`_workspace/sast/138_async-bind_result.md`](../../_workspace/sast/138_async-bind_result.md) — "먼저 읽는 한 줄 답", "정정", "근거 줄 확인" (PE 바이트 대조 34개), "동적으로 확인할 것", "다음 요청 후보", 판정
- 요청서: [`_workspace/sast/138_async-bind.md`](../../_workspace/sast/138_async-bind.md) · 원 요청 `../PE_emulator/_workspace/00_planner/AS02_internals-async-bind.md` (에뮬레이터 로그 seq 2185 ~ 2201 · 5617 ~ 5623)
- flow.md: [`flow.md`](flow.md) — 3장 **"비동기 epmapper 연결 뒤 bind 를 미는 길"** 절 (새) · "비동기 LRPC 답 배달" 절 (run137) · "LRPC 선 위 형식" 절 (run133) · "일꾼 공장" 절 (run125) 의 run141 한 줄 · [4장 공백 목록](flow.md#4-공백-목록-다음에-채울-곳) "3 (범위 밖 — NSI)" Q267 · run141 줄 · [5장 근거 run](flow.md#5-근거-run) run141 줄
- 바로 앞 바퀴들 (같은 전화 이야기): [run129 해설](run129-winnsi-rpc-해설.md) (2.7 114 · 2.8 보류 · 2.9 메모판 두 장), [run133 해설](run133-lrpc-wire-해설.md) (2.3 받기만 · 2.5 첫인사), [run137 해설](run137-alpc-completion-해설.md) (2.5 쪽지 · 2.6 · 2.7 일꾼 → `IoCompleted` · **2.16 서버 흉내 차례 — 이번에 빈칸을 채운 표**)
- 일꾼 사무실 그림: [run125 해설](run125-worker-factory-해설.md) (2.2 파견소 · 메모판, 2.5 멀리 있기 · 묶음 수, 2.10 쪽지 꺼내기, **2.11 `0x102`**), [run72 해설](run72-pool-work-post-해설.md) (2.2 우체통 · 2.3 `TppWorkPost` · 2.4 "한 명 보내 주세요")
- 대기열: `_workspace/plan.md` Q267 (끝) · **Q271** (동적, Q258 · Q231 세션과 합침) · **Q272** (정적) · Q259 (③ 일부 닫힘)

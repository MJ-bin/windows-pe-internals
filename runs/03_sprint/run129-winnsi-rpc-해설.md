# run129 winnsi-rpc — 해설

> 이 글은 [run129 문서](run129-winnsi-rpc.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q223** — PE_emulator 요청 "winnsi 알림 등록 RPC — 서버가 없으면 클라이언트가 어떻게 되고, 한 번 성공시키려면 무엇을 답해야 하나" (튜닝 `303_tune11.md` T1 · T2, 샘플 212) (`_workspace/plan.md`, 이번에 **끝 (run129, 질문 3 남은 몫 → Q230)**)
  - 사용자 결정 2026-11-22 "이게 제일 급해 바로 진행".
  - 결과서 판정 **부분성공**: 질문 1 · 2 는 닫혔고, 질문 3 (서버가 돌려줘야 하는 최소) 은 **틀만** 닫혔다.
  - 남은 것이 간 곳 (4절):
    - 정적 → **Q230** (LRPC 선 위 형식 · `ept_map` 응답 검사 · nsisvc), **Q232** (낮음 — 완료 통지 · WNF 이름)
    - 동적 → **Q231** (서버 없음일 때 최종 오류 숫자, 첫 부름의 예외 자리, 상태 칸, 연결 인자, 진짜 ALPC 메시지 한 번)
  - 같이 닫힌 것: run128 의 "`NsiConnectToServer` 가 서버를 정말 만나나" `[공백]` (run128 정적 ①). 그리고 run128 을 **하나 정정** 한다 (2.6).
- 근거:
  - run 문서 [`run129-winnsi-rpc.md`](run129-winnsi-rpc.md) (가장 중요)
  - 요청서 [`_workspace/sast/127_winnsi-rpc.md`](../../_workspace/sast/127_winnsi-rpc.md) · 결과서 [`127_winnsi-rpc_result.md`](../../_workspace/sast/127_winnsi-rpc_result.md)
    - 처음 analyze 는 **winnsi 하나** (함수 86, 11초). rpcrt4 · IPHLPAPI · webio · ntdll · kernelbase 는 이미 있던 것을 읽기만. nsisvc 는 안 열었다.
    - 스크립트 문제 없음, 오프셋은 원본 바이트와 맞춰 봄 — **일치**.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 (PDB 하나 · 근거 줄) · 3장 run99 절 한 줄 · 3장 run128 절 winnsi 줄 · **3장 끝 새 절** · 4장 · 5장. **그림은 안 바뀜.**
  - 짐 지도 `baggage.md` 는 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 모듈들을 안 올린다.**
   - 초기화 끝 (P2) 모듈 6개에 winnsi · rpcrt4 · IPHLPAPI · webio 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run124 · run128 과 같은 줄).
   - `sample/*.c` 에 RPC · 네트워크 API 가 없다 `[소스]`.
   - 그래서 이번 이야기도 "**웹을 쓰는 다른 프로그램이라면**" 이다. 그림 · 짐 지도는 그대로다.
2. **"요청 쪽 로그" 는 진짜 Windows 로그가 아니다.**
   - "첫 등록 / 둘째 등록", seq 2196 ~ 2233, "성공인데 핸들 안 씀" 은 모두 **PE_emulator 가 흉내 낸 실행** 의 기록이다. `[관찰]` 이 아니다.
   - 이 글은 그 로그를 "코드로 설명하면 이렇게 읽힌다" 까지만 한다. 로그가 정말 그 자리 것인지는 대부분 `[추측]` 이고 동적 (Q231) 으로 가린다.
3. **"코드가 그렇게 돼 있다" 와 "로그가 그렇게 됐다" 를 나눠 읽자.** 이번 글의 핵심 (2.10 끝없는 기다림) 이 딱 이 경계에 있다.
   - 상태 칸이 3 으로 남으면 둘째 부름이 기다린다 → `[정적]` (코드 줄이 있다).
   - 첫 부름이 정말 그렇게 나갔다 → `[추측]`.
   - 첫 부름이 **왜** 그렇게 나갔나 (어디서 사고가 났나) → `[공백]`.
4. **오프셋 세 가지.** run 문서와 같은 규칙이다.
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문 "`+0x1e8 Ndr64AsyncClientCall`" 처럼 함수 이름 앞 숫자 = **call 자리**.
   - 그냥 "`+0xc9`" · "`+0x30e`" = 함수 안 **줄**.
   - 콜스택으로 본 적은 없다.
5. **rpcrt4 엔 이름이 같은 함수가 여럿이다.** 클래스로 가른다 (run 문서 "방법").
   - `ResolveEndpoint` = **바인딩 쪽** (rva `0x59258`) / `EP_LOOKUP_DATA` 쪽 (`0x59d38`).
   - `CompleteOnePart` = **연결 객체** (`0x3b148`) / 제공자 (`0x43164`) / **바인딩** (`0x5aeec`).
   - 이 글에서 "바인딩 `CompleteOnePart`" 라고 하면 `0x5aeec` 다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- 이벤트 = **신호등** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md), **자동 리셋 이벤트** = 하나를 깨우면 저절로 다시 꺼지는 신호등 → [run24 해설 2.3](../02_sprint/run24-work-queue-해설.md)
- 지연 가져오기 = "필요하면 그때 부를게요" 메모 → [run25 해설 2.5](../02_sprint/run25-dll-entry-해설.md)
- ALPC 포트 = 프로세스끼리 거는 **전화선 끝** → [run68 해설 2.4](run68-dll-init-rest-해설.md)
- 간접 호출 (CFG) = 번호를 보고 이어 주는 **교환원** → [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md) · [run96 해설](run96-bcrypt-interface-해설.md)
- RPC 런타임 = 가게 안 **전화 부서**, `gDefaultSecurityDescriptor` = 전화 부서의 **기본 명부** → [run99 해설](run99-rpc-caller-해설.md) "한 줄로 먼저"
- 필터 = 대피도에서 "이 사고를 여기서 잡을까?" 를 정하는 짧은 함수 (값 1 잡음 / 0 다음으로 넘김) → [run82 해설 2.7](run82-exception-user-해설.md)
- WER = 사고 **신고센터**, `__fastfail` = **비상 정지 버튼** → [run81 해설 2.15 · 2.16](run81-exception-kernel-해설.md)
- 일꾼 공장 (스레드풀) → [run125 해설](run125-worker-factory-해설.md)
- run124 · run126 · run128 에서 푼 말 — nsi = **망 안내 데스크**, IOCTL = 번호 붙은 주문서, compartment (망 구획), winhttp = **웹 편지 담당**, webio = 그 **작업실**, IPHLPAPI = **비서**, 알림 등록 = "길 바뀌면 알려 주세요" **신청**, 모듈 ID · 객체 번호 = 어느 **부서** 의 몇 번 **장부**, F1 · F2 → [run128 해설](run128-nsi-callers-해설.md) (2.6 · 2.8), [run124 해설](run124-afd-socket-해설.md), [run126 해설 2.8](run126-afd-addrlist-해설.md)

이번에 처음 나오는 말 — **계열 (family)**, **바인딩 · 끝점 · `ncalrpc`**, **epmapper · `ept_map` · 탑**, **비동기 RPC**, **인터페이스 · 프로시저 번호**, **컨텍스트 핸들**, **보류 (`0xc0021018`)**, **연결 객체 · 바인딩 핸들의 상태 칸**, **들어오는 속성 (빈 봉투)**, **치명 예외 14종** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 핸들 = **손잡이**, 이벤트 = **신호등**.
- run99 · run128 의 그림 그대로:
  - rpcrt4 (RPC 런타임) = 가게 안 **전화 부서**. ALPC 포트 = **전화선 끝**.
  - winhttp = **웹 편지 담당**, webio = 그 **작업실**, IPHLPAPI = **비서**.
  - 알림 등록 = 작업실 문을 처음 열 때 내는 "길 바뀌면 알려 주세요" **신청**. 안내 데스크 (NSI 장치) 가 아니라 **전화** 로 다른 사무실 (nsisvc) 에 한다.
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 따름 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - winnsi = 비서가 전화 신청을 맡기는 **신청 담당**. 그런데 **전화번호부 없이 회사 이름만** 들고 있다 (끝점 없는 바인딩).
  - epmapper = **114 안내**. 전화 부서는 먼저 114 에 "nsi 사무실 번호가 뭐예요?" 를 묻는다.
  - 비동기 RPC = **"끝나면 신호등 켜 주세요" 방식** 의 신청. (run 문서는 "벨 울려 주세요" 라 썼다. 이 글은 앞 해설들에 맞춰 **신호등** 으로 부른다. 같은 것이다.)
  - 전화 부서의 **메모판** = 바인딩 핸들 `+0x114` 칸. "지금 번호 알아보는 중 (3)" 같은 글이 적힌다.
  - 사고 (치명 예외) → **신고센터 (WER)** 로 가는 길은 run81 · run82 그대로.
- 주의 — 이름이 겹치는 곳:
  - "**NSI**" 는 이번에도 세 겹이다 (run128 해설 조심할 곳 3). 이번 주인공은 장치 (`\\.\Nsi`) 가 아니라 **`winnsi.dll` (RPC)** 이다. 다만 2.6 에서 winnsi 가 장치 주문서 `0x12000f` 도 하나 낸다 — 그래서 헷갈렸던 것이다.

### 한 줄로 먼저 — 무엇을 얻었나

> **로그의 "첫 등록 · 둘째 등록" 은 다른 신청 둘이 아니다. 같은 신청을 IPv4 로 한 번, IPv6 로 한 번 한 것이다.**
> **winnsi 는 번호 없이 회사 이름만 들고 전화한다. 그래서 전화 부서 (rpcrt4) 가 먼저 114 (epmapper) 에 붙는다. 로그의 `\RPC Control\epmapper` 연결은 nsi 사무실이 아니라 이 114 연결이다.**
> **114 에 묻는 동안 전화 부서는 메모판에 "번호 알아보는 중 (3)" 을 적는다. 그 메모를 지우는 줄에 못 닿고 첫 신청이 사고로 나가 버리면, 둘째 신청은 "알아보는 중이네, 기다릴게요" 하고 줄만 선다. 깨워 줄 사람은 없다.**
> **에뮬레이터는 "그런 번호 없어요" (`0xc0000034` → 1722) 로 답하면 깨끗하게 실패로 끝난다. "연결 거부" (`0xc0000041`) 는 rpcrt4 가 "메모리 부족" 으로 읽으니 쓰면 안 된다.**

- 첫 둘은 `[정적]`.
- 셋째 줄은 **코드 줄은 `[정적]`**, "첫 신청이 정말 사고로 나갔다" 는 `[추측]`, "어디서 사고가 났나" 는 `[공백]` (2.10).
- 넷째 줄의 상태 → 오류 표는 `[정적]`, 서버 없음일 때 winnsi 가 **최종으로 받는 숫자** (1722 인지 1752 인지) 는 `[공백]` (2.12).

요청서 질문에 대한 답 (run 문서 "요약" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| (먼저) 로그 읽기 | "첫 / 둘째 등록" = `InternalRegisterChangeNotification +0x9d` / `+0xd5` 의 **IPv4 → IPv6**. webio `Startup` 한 번에 등록 최대 4번 `[정적]` | 2.3 |
| 1 바인딩 | `ncalrpc` · **끝점 없음** · 인증 6 / 10. 연결은 안 한다 → 첫 RPC 때 epmapper `[정적]` | 2.4 · 2.7 |
| 1 호출 꼴 | **비동기** `Ndr64AsyncClientCall`, 인터페이스 `7ea70bcf-…` v1.0, 프로시저 5 등록 · 6 해제 · 7 알림 기다림 `[정적]` | 2.5 |
| 1 앞 이벤트 | winnsi `+0x168` **자동 리셋**, 완료 통지용. 세우는 쪽 rpcrt4 `IssueNotification +0xc5` `[정적]` | 2.5 |
| 1 실패 값 · 치명 | 계열 0 이면 **IPv4 · IPv6 둘 다 실패일 때만** 오류 → webio `Startup +0xbc` 치명 → `WinHttpSendRequest` 0 `[정적]` | 2.3 |
| 2 상태 → RPC 오류 | `0xc0000034` · 표에 없는 것 → `0x6ba` (1722), `0xc0000041` → `0xe` (메모리 부족) … `[정적]` | 2.8 |
| 2 "성공인데 핸들 0" · 끝없는 기다림 | 핸들 값을 안 본다. 바인딩 `+0x114` = 3 이 안 풀리면 둘째 부름은 보류 → 무한 대기 `[정적]` (로그 대응은 `[추측]`) | 2.9 · 2.10 |
| 2 seq 2205 ~ 2224 | rpcrt4 가 아니라 **WER 길** (ntdll `RtlReportExceptionEx` 줄 순서 + kernelbase `WaitForWerSvc`) `[정적]` | 2.11 |
| 3 서버 최소 | ① epmapper 연결 ② `ept_map` ③ nsi 끝점 연결 ④ 프로시저 5 = 컨텍스트 핸들 + 0 ⑤ 프로시저 7 은 답 안 함. 선 위 머리 칸은 `[공백]` → Q230 | 2.13 |
| (덤) run128 정정 | `0x12000f` 는 F1 (winnsi 등록) 안에서도 나간다 `[정적]` | 2.6 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run124 ~ run128 과 같은 자리, **초기화 밖** 이다. `main` 뒤 프로그램 코드가 웹을 쓸 때의 옆길이다. run128 이 "전화 쪽은 다음 바퀴" 라고 남긴 바로 그 자리를 이번에 연다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll: LdrInitializeThunk → LdrpInitializeProcess → DLL 들이기 · 전원 켜기
 └─ NtContinue (은퇴) → exe 진입점 → (CRT 점선 상자) → main
                                                        │
      test.exe 는 여기서 웹 · 소켓을 안 쓴다 [소스]     │ 웹을 쓰는 프로그램이라면 (요청 쪽 에뮬레이터 로그):
                                                        └─ WinHttpSendRequest                            (run128)
                                                             ├─ F1 작업실 문 열기 (webio Startup)
                                                             │    └─ 비서 NotifyIpInterfaceChange
                                                             │         └─ 신청 담당 winnsi → 전화 부서 rpcrt4 → 114 (epmapper)   ← 이번
                                                             └─ F2 프록시 이름 → 안내 데스크 (\\.\Nsi)       (run128)
```

### 왜 이 질문이 지금 왔나

PE_emulator 에서 **샘플 212 개 (악성 203)** 가 같은 자리에서 멈췄다. 요청서가 옮긴 로그는 이렇다 (에뮬레이터 로그 — 진짜 Windows 관찰 아님):

1. **첫 등록**
   - 이벤트 만들기 (2196) → Nsi `0x12000f` 성공 (2197) → 닫음
   - 이벤트 만들기 "수동 리셋" (2199) → 토큰 묻기 셋
   - **`NtAlpcConnectPort \RPC Control\epmapper`** (2204) — 에뮬레이터 대답 = **성공인데 포트 핸들을 안 씀**
   - `NtQueryInformationProcess 0x25` · `NtSetInformationProcess 0x3f` · `NtCreateSection` · `NtDuplicateObject ×2` · `NtOpenEvent \KernelObjects\SystemErrorPortReady` (2205 ~ 2224)
   - 이벤트 닫음 (2226)
2. **둘째 등록** (같은 스레드, 곧바로)
   - 이벤트 (2227) → `0x12000f` 성공 (2228) → 닫음 → 이벤트 (2230) → …
   - **그 이벤트를 끝없이 기다림** (2233). 이번엔 `NtAlpc*` 가 **0 번**.
- 같은 꼴 210 / 210. 부른 API 는 대부분 `WinHttpSendRequest` (201).
- 곁에 있는 사례 둘 (요청서):
  - `\RPC Control\keysvc` · `eventlog` 는 에뮬레이터가 **`0xc0000034`** 로 답하고, 부른 쪽은 멈추지 않고 이어 갔다 (18 샘플).
  - `CoInitializeSecurity` 도 같은 "핸들 없는 성공" 을 받고 `NtAlpcSendWaitReceivePort(핸들 0)` → `0xc0000008` 로 갔다 (22 샘플).

요청한 쪽이 고치려는 것은 둘이다 (튜닝 T1 · T2):

- **T1** — 속성 있는 ALPC 연결을 **어떤 status 로 답할까**.
- **T2** — nsisvc 알림 등록을 **성공처럼 흉내** 내려면 무엇이 필요한가.

### 무엇이 궁금했나

1. **winnsi** — 끝점이 고정인가 epmapper 로 푸는가? 동기 / 비동기? 인터페이스 · 프로시저 번호? 부르기 앞 이벤트는 누구 것인가? 실패하면 무슨 값이고 치명인가?
2. **rpcrt4 연결 길** — `NtAlpcConnectPort` 상태마다 어떤 RPC 오류가 되나? "성공인데 핸들 0" 뒤 무엇을 하고, 왜 둘째 부름이 기다리나? seq 2205 ~ 2224 는 어디서 오나?
3. **서버 쪽 최소** — 등록 한 번을 성공시키려면 무엇을 돌려줘야 하나 (정적으로 틀만).

### 바퀴 전에 알던 것

- `[정적]` (run128, sast/126) F1 사슬: webio `Startup` → `iphlpapi!NotifyIpInterfaceChange` → `InternalRegisterChangeNotification` → `NsiNotificationConnectToServer` → `winnsi!NsiConnectToServer` · `NsiRpcRegisterChangeNotification`. 이 둘이 0 이 아니면 `WebInitialize` 실패 → `WinHttpSendRequest` 0.
- `[공백]` (run128) `NsiConnectToServer` 가 **정말 전화를 거나, 전화번호만 적어 두나**.
- `[공백]` (run128 2.7) "`0x12000f` 가 보인 호출이 왜 0 을 돌려주나" — F1 이 F2 보다 앞이라 F1 은 원인이 아니라고 봤었다. (이번에 정정된다, 2.6.)
- `[정적]` (run99) rpcrt4 `AlpcConnect.asm` 에 `+0x293 NtAlpcConnectPort` 와 상태 견주기 줄들이 있다. **어느 상태가 어느 오류가 되는지는 안 읽었다.**
- `[정적]` (run99) 전화 부서를 차릴 때 **기본 명부** (`gDefaultSecurityDescriptor`) 를 만든다 — 이번 2.8 에서 다시 나온다.

---

## 2. 찾은 것

### 2.1 한눈에 — 신청 한 번이 지나는 길 `[정적]`

run 문서 "호출 관계" 와 4절 나무를 줄였다. 오프셋은 ret 자리, 들여쓴 `+0xc9` · `+0xc7` 은 함수 안 줄이다.

```
webio!Startup
 └─ +0xb4  iphlpapi!NotifyIpInterfaceChange                         (객체 7)
           └─ +0xa0  InternalRegisterChangeNotification
                    ├─ +0x63  NsiNotificationConnectToServer → +0x2d winnsi!NsiConnectToServer
                    │           "ncalrpc", 끝점 NULL · 인증 6/10 — 연락 카드만, 연결 안 함          (2.4)
                    ├─ +0xa4  winnsi!NsiRpcRegisterChangeNotification   IPv4   ← 로그 "첫 등록"   (2.3)
                    │         └─ +0x60  NsiRpcRegisterChangeNotificationEx                          (2.5)
                    │                  ├─ +0x119  nsi!NsiGetAllParameters  NDIS 8 → 0x12000f        (2.6)
                    │                  ├─ +0x16f  CreateEventW  자동 리셋
                    │                  ├─ +0x1ef  Ndr64AsyncClientCall  프로시저 5
                    │                  │          ⇒ ResolveEndpoint  +0xc9 [바인딩+0x114] = 3       (2.7 · 2.9)
                    │                  │             → … → AlpcConnect +0x29a NtAlpcConnectPort(\RPC Control\epmapper)  (2.8)
                    │                  │             → ept_map → … → CompleteOnePart  +0xc7 [+0x114] = 4 / 1
                    │                  ├─ +0x240  WaitForSingleObject  INFINITE
                    │                  └─ +0x259  RpcAsyncCompleteCall
                    └─ +0xdc  winnsi!NsiRpcRegisterChangeNotification   IPv6   ← 로그 "둘째 등록"
```

- `⇒` 는 NDR 해석기 안을 건너뛴 곳이다 (이름만).
- 오른쪽 괄호는 이 글의 절 번호다.

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `├─ +0xa4  winnsi!NsiRpcRegisterChangeNotification   IPv4   ← 로그 "첫 등록"`

- **누가**: `iphlpapi!InternalRegisterChangeNotification` (rva `0x9758`) — 비서가 알림 신청을 실제로 내는 함수.
- **어디서**: call 은 `+0x9d`, 돌아올 자리 (ret) 는 `+0xa4`. 콜스택에는 `InternalRegisterChangeNotification+0xa4` 로 찍힐 것이다 (본 적은 없다).
- **누구를**: `winnsi!NsiRpcRegisterChangeNotification` — 신청 담당 (winnsi) 의 "알림 신청" 함수.
- **무엇을 위해**: **IPv4 부서 (모듈 ID `NPI_MS_IPV4_MODULEID`)** 장부가 바뀌면 알려 달라고 신청하려고.
- 같은 함수가 `+0xd5` (ret `+0xdc`) 에서 **IPv6 모듈 ID** 로 한 번 더 불린다. 그게 로그의 "둘째 등록" 이다 (2.3).

#### 줄 ②: `│ ⇒ ResolveEndpoint  +0xc9 [바인딩+0x114] = 3`

- 이 줄은 **호출이 아니라 쓰기** 줄이다.
- **누가**: `rpcrt4!LRPC_BASE_BINDING_HANDLE::ResolveEndpoint` (rva `0x59258`) — 전화 부서에서 "상대 번호를 알아내는" 함수.
- **어디서**: 함수 안 `+0xc9`.
- **무엇을**: 바인딩 핸들의 `+0x114` 칸에 **3** 을 쓴다. 뜻은 "지금 끝점 (번호) 푸는 중" `[정적]` (칸 이름은 코드 줄에서 읽은 것, 2.9).
- **무엇을 위해**: 같은 연락 카드로 다른 신청이 동시에 오면 "이미 알아보는 중이니 기다려" 라고 하려고.
- 이 3 은 바인딩 `CompleteOnePart` (`0x5aeec`) `+0xc7` 에서만 4 (풀림) 나 1 (처음으로) 로 되돌아간다. **이 한 쌍 (`+0xc9` 쓰기 ↔ `+0xc7` 되돌리기) 이 이번 글의 열쇠** 다 (2.10).

### 2.3 "첫 / 둘째 등록" = 같은 신청을 IPv4 → IPv6 로 `[정적]`

run 문서 1절. 요청 쪽 로그를 **바로잡는** 결과다.

#### 새 말 — 계열 (family)

- 주소 **종류** 를 고르는 숫자다. 이번 코드에서 쓰이는 값은 셋 `[정적]`:
  - `2` = IPv4 만, `0x17` = IPv6 만, `0` = **둘 다**.
- 비유: 신청서의 "어느 쪽 길 소식을 받을까요?" 칸. 0 은 "둘 다 주세요".
- webio `Startup` 은 계열 **0** 을 넘긴다 `[정적]`.

#### 무엇을 하나

- `InternalRegisterChangeNotification` (`.asm +0x73 ~ +0xdc`):
  - 계열이 `0x17` 이 아니면 → `+0x9d` 에서 **IPv4** 모듈 ID 로 부른다.
  - 계열이 2 가 아니면 → `+0xd5` 에서 **IPv6** 모듈 ID 로 또 부른다.
  - 계열 0 이면 **둘 다** 부른다.
- IPv4 가 실패해도 IPv6 부름은 한다. 로그의 "곧바로 둘째 등록" 과 맞는다.

#### 결과를 어떻게 합치나 (`+0xe1 ~ +0xf5`)

| 계열 | 돌려주는 값 |
|---|---|
| 2 | IPv4 결과 |
| `0x17` | IPv6 결과 |
| **0** | IPv4 가 0 이면 0 → 아니면 IPv6 가 0 이면 0 → **둘 다 실패면 IPv4 의 오류** |

- 즉 계열 0 에서는 **둘 중 하나만 성공해도 성공** 이다.
- 둘 다 실패면 그 오류가 `NotifyIpInterfaceChange` → webio `Startup +0xbc` 에서 **치명** → `WebInitialize` 실패 → `WinHttpSendRequest` 0 (run128 F1 사다리 그대로) `[정적]`.

#### 최대 4번

- `NotifyUnicastIpAddressChange` 도 `NetioRegisterAddressChangeNotification +0x9d` 를 거쳐 **같은 함수** 로 온다 (객체 번호 `0xa`; 인터페이스 쪽은 7) `[정적]`.
- 그래서 webio `Startup` 한 번이 등록 RPC 를 **최대 4번** 한다: 인터페이스 v4 · v6, 유니캐스트 v4 · v6.

비유: 비서는 신청서를 "IPv4 부서용" · "IPv6 부서용" 두 장 쓴다. 한 장이라도 접수되면 "신청 완료" 라고 보고한다. 두 장 다 반려돼야 "실패" 다.

### 2.4 `NsiConnectToServer` — 연락 카드만 만든다 (전화는 안 건다) `[정적]`

run 문서 2절. run128 의 `[공백]` "전화번호를 적어 둔 것인가, 정말 전화를 건 것인가" 의 답이다. → **적어 두기만 한다.**

#### 새 말 — 바인딩 · 끝점 · `ncalrpc`

- **바인딩 (binding)** = "누구에게, 어떤 방식으로 전화할지" 를 적은 **연락 카드**. 바인딩 핸들은 그 카드의 손잡이다. 카드를 만든다고 전화가 걸리지는 않는다 (`RpcBindingFromStringBindingW` 는 핸들만 만든다 `[정적]`).
- **끝점 (endpoint)** = 상대 사무실의 **직통 번호**. 같은 컴퓨터 안 전화라면 `\RPC Control\<이름>` 꼴의 ALPC 포트 이름이 된다 (`AlpcConnect` 가 이름 앞에 `\RPC Control\` 을 붙인다, 결과서 2-1) `[정적]`.
- **`ncalrpc`** = 연락 카드의 "전화 방식" 칸에 적는 값. 같은 컴퓨터 안 프로세스끼리 하는 RPC (LRPC) 로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 코드에서는 rpcrt4 프로토콜 표가 `ncalrpc` 의 114 끝점 이름 `epmapper` 를 주고, 114 에 거는 카드도 LRPC 로 만든다 `[정적]` (2.7).

#### 무엇을 하나 — 세 줄뿐

1. `+0x12 TEST RCX` / `+0x15 JNZ` — 서버 이름 인자가 0 이 아니면 아무것도 안 하고 0 (원격은 지원 안 함).
2. `+0x3a RpcStringBindingComposeW(NULL, "ncalrpc", NULL, NULL, "Security=Impersonation Dynamic True", &문자열)` — **네트워크 주소도 끝점도 NULL**.
3. `+0x70 RpcBindingFromStringBindingW` — 핸들만 만든다.
4. `+0xc7 RpcBindingSetAuthInfoW(핸들, NULL, 6, 10, NULL, 0)` — 인증 수준 6, 서비스 10 (상수 이름 PKT_PRIVACY · WINNT 는 `[추측: 지식]`).

- 실패하면 `SetLastError(RPC 오류)` + 0 (`+0x4c` · `+0x95` · `+0xf1`).
- IPHLPAPI 는 이 핸들을 **전역 `NotificationRpcHandle` 에 두고 계속 쓴다** (`NsiNotificationConnectToServer +0x32`). 실패면 NULL 로 남아 다음에 다시 만든다.

#### 끝점이 정말 아무 데도 없나

- 바인딩 문자열에 없다 (위 2).
- winnsi 인터페이스 구조체 (rva `0x67b0`) 의 **기본 끝점 수 (`+0x38`) = 0** `[정적]`.
- → 끝점은 **epmapper 로만** 풀린다 (2.7). 건너뛸 길이 없다.

비유: 신청 담당은 카드에 "nsi 사무실, 같은 건물 내선, 비밀 통화" 만 적고 **번호 칸은 비워 둔다**. 번호는 첫 전화 때 전화 부서가 114 에 물어 채운다.

- 그래서 run128 3절의 "연결 핸들 NULL → 6" 은 **여전히 맞지만**, 서버가 없을 때 실패는 이 줄에서 오지 않는다. 카드 만들기는 서버 없이도 성공하기 때문이다 (run 문서 "정정").

### 2.5 `NsiRpcRegisterChangeNotificationEx` — 신청 한 번의 순서 `[정적]`

run 문서 3절. 여기서 처음으로 진짜 전화가 나간다.

#### 새 말 — 인터페이스 · 프로시저 번호

- **인터페이스** = 상대 사무실의 **이름표** (UUID + 판 번호). winnsi 의 것은 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0 `[정적]`.
- **프로시저 번호** = 그 사무실의 **메뉴 번호**. 무엇을 부탁하는지 번호로 고른다.
  - **5 = 등록 (신청)**, **6 = 해제 (취소)**, **7 = 알림 기다림 (소식 오면 알려 주세요)** `[정적]`.
  - 0 ~ 4 는 값 읽기 · 쓰기 (알림 아님), 8 은 이 모듈에서 부르는 곳 못 찾음 `[공백]`.

#### 새 말 — 비동기 RPC

- 전화를 걸고 **답이 올 때까지 수화기를 들고 서 있지 않는** 방식이다. "끝나면 신호등 켜 주세요" 하고 신호등 하나를 맡긴 뒤, 그 신호등을 지켜본다.
- winnsi 는 `RpcAsyncInitializeHandle` + 통지 종류 1 (이벤트) + `Ndr64AsyncClientCall` 로 부른다 `[정적]`.

#### 일곱 걸음

1. **인자 검사** (`+0xa0 ~ +0xb6`) — 바인딩 · 구조체가 NULL 이면 `0x57`.
2. **compartment 묻기** — compartment 칸이 비었으면 `+0x112 nsi!NsiGetAllParameters(1, NDIS, 8, …, RW 8 B)` = **IOCTL `0x12000f`**. 0 이 아니면 **그 값으로 바로 끝** (`+0x120 JNZ → +0x636`). → 2.6 에서 자세히.
3. **신호등 준비** — `+0x143 RpcAsyncInitializeHandle(&상태, 0x70)` → `+0x153` 통지 종류 1 (이벤트) → `+0x168 CreateEventW(NULL, 0, 0, NULL)`.
   - 인자 셋째 `0` = 수동 아님 → **자동 리셋** (`+0x164`). 처음 꺼짐, 이름 없음.
   - 이 신호등은 연결 잠금이 아니라 **"비동기 RPC 다 끝났어요" 통지용** 이다.
4. **전화 걸기** — `+0x1e8 Ndr64AsyncClientCall(…, 프로시저 5, …)`.
   - `__try / __except(I_RpcExceptionFilter(code))` 안에서 부른다. → 2.11 의 "치명 예외" 이야기가 여기서 갈린다.
5. **기다리기** — 예외 없이 돌아오면 `+0x239 WaitForSingleObject(신호등, INFINITE)` → `+0x252 RpcAsyncCompleteCall(&상태, &반환)`.
   - 이게 0 이 아니면 그 값이 결과. 0 이면 서버가 돌려준 DWORD (`[RSP+0x60]`) 가 0 이어야 성공 (`+0x266` · `+0x26c`).
   - 이 신호등을 켜 주는 쪽은 rpcrt4 `IssueNotification` (rva `0x345c4`) `+0xc5 SetEvent` 다.
6. **성공 길** (서버 없이는 안 감) — 블록 `HeapAlloc 0xb0` (`+0x286`), 신호등 몇 개 더, 수동 리셋 이벤트 (`+0x42c`), `+0x4e0 RegisterWaitForSingleObjectEx(…, NsiWorkerThread, …)`, **`+0x54e` 프로시저 7 걸어 두기**, 목록에 넣기 (처음이면 `+0x6b8 RegisterNsiSvcRestartNotification` — WNF 구독, 이름만), 마지막 `+0x626 CloseHandle(완료 신호등)`.
7. **실패 정리** (`+0x664 ~`) — 만든 것 해제, 서버까지 등록됐으면 프로시저 6 (`+0x83a`), 그리고 `+0x61e` 로 합류해 같은 `+0x626 CloseHandle` → 로그의 "이벤트 닫음 (2226)".

#### 새 말 — 컨텍스트 핸들

- 프로시저 5 가 성공하면 서버가 돌려주는 **접수 번호표** 다. 다음 부탁 (7 기다림, 6 취소) 때 이 번호표를 내민다 `[정적]` (형식 문자열 `0x70` 바이트로 읽음 — 종류 이름은 `[추측]`).
- 받은 번호표는 블록 `+0x28` 에 보관하고, 비서가 준 슬롯에는 마지막에 **블록 포인터** 를 덮어쓴다 (`+0x5ba`) `[정적]`.
- 번호표 값을 검사하는 줄은 못 찾았다 → 안전하게는 0 이 아닌 값 `[추측]` (2.13).

#### 로그와 코드가 다른 곳 하나 — "수동 리셋" (seq 2199)

- 요청 쪽 로그는 2199 를 "수동 리셋" 이라 적었다.
- 코드 `+0x168` 은 **자동 리셋** 이다 (`EDX = 0`). 수동 리셋은 성공 길의 `+0x42c` 하나뿐 `[정적]`.
- 결과서 지적: 로그가 `EventType` 숫자를 읽는 쪽 (0 = 수동, 1 = 자동) 을 확인할 것.

비유: 신청 담당은 ① 망 구획 번호부터 안내 데스크에 묻고 ② 신호등을 하나 세운 뒤 ③ 전화 부서에 "5번 메뉴 (신청) 부탁해요, 끝나면 이 신호등 켜 주세요" 하고 ④ 신호등만 쳐다본다. 시간 제한 없이 (INFINITE).

### 2.6 ★ `0x12000f` 도 여기서 나간다 — run128 정정 `[정적]` (로그 대응은 `[추측]`)

run 문서 "정정". 사용자가 궁금해할 셋째 지점이다. 천천히 간다.

#### run128 은 이렇게 따졌었다 (run128 해설 2.7)

- 근거: `0x12000f` 는 F2 (프록시 이름 길, `+0x2fa` 쪽) 에서 나온다. F1 (`+0x25a`) 은 F2 보다 **앞** 이다.
- 그래서: 로그에 `0x12000f` 가 찍혔다면 그 호출은 F1 을 **이미 지났다**. → F1 은 원인이 아니다.
- 그래서 "`0x12000f` 가 보인 호출의 0 반환" 원인은 `[공백]` 이었다.

#### 이번에 무엇이 새로 보였나

- winnsi `NsiRpcRegisterChangeNotificationEx +0x112` 가 **등록마다** `nsi!NsiGetAllParameters(1, NDIS, 8, RW 8 B)` 를 부른다 `[정적]`.
  - 이것은 run128 의 F2 첫 호출 (`AllocateAndGetAdaptersAddresses +0xc1`) 과 **같은 인자** 다. 즉 같은 `0x12000f` 주문서다.
  - 구조체가 등록마다 0 으로 시작하므로 **등록마다 한 번** 나간다.
- 결과가 0 이 아니면 `+0x120 JNZ → +0x636` — **RPC 도, 신호등도 없이** 그 오류로 끝난다 `[정적]`.
  - flow-writer 보탬: 신호등을 만들기 **전** 이라 `CloseHandle` 도 없다. 그러니 `0x12000f` 가 실패한 등록은 로그에 "이벤트 만들기 → … → 이벤트 닫음" 꼴이 **안 나온다**.

#### 그래서 — 단계별로

1. **근거** `[정적]`: `0x12000f` 는 F2 만의 것이 아니다. **F1 안 (winnsi 등록) 에서도** 나간다.
2. **그래서**: "`0x12000f` 가 찍혔으니 F1 을 지났다" 는 **성립하지 않는다**. run128 의 그 추론이 정정된다.
3. **그리고** `[정적]`: F1 안의 `0x12000f` 가 실패하면 → IPv4 등록 실패 → IPv6 도 같은 길로 실패 → 계열 0 에서 **둘 다 실패** → `NotifyIpInterfaceChange` 오류 → webio `Startup +0xbc` → `WebInitialize` 실패 → `WinHttpSendRequest` 0.
4. **그러니**: run128 이 `[공백]` 으로 둔 "`0x12000f` 가 보인 호출의 0 반환" 에 **정적 후보 사슬** 이 하나 생겼다.
5. **아직 확인 아님** `[추측]`: run128 때의 요청 로그 `0x12000f` 가 **정말 이 자리 (winnsi `+0x112`)** 것인지, 아니면 F2 것인지는 모른다.
   - 가르는 방법: `winnsi!NsiRpcRegisterChangeNotificationEx+0x119` 의 `eax` 를 보면 된다 → 동적 **Q231** (plan.md ⑦, run 문서 "아직 확인 필요" 동적 7) · **Q222** 와 같은 실행.

비유: run128 에서는 "안내 데스크에 질문지를 낸 사람은 프록시 이름 길의 비서뿐" 이라 생각했다. 그런데 알고 보니 **전화 신청 담당도 신청할 때마다 데스크에 같은 질문지를 먼저 낸다**. 데스크가 "모르겠어요" 하면 전화는 걸지도 않고 신청을 접는다. 그러니 "데스크에 질문지가 왔다" 는 기록만으로는 누가 낸 건지 모른다.

### 2.7 전화 부서가 114 에 먼저 묻는다 — 끝점 풀이 `[정적]`

run 문서 4절.

#### 새 말 — epmapper · `ept_map` · 탑

- **epmapper** (엔드포인트 매퍼) = "이 인터페이스는 몇 번 끝점에 있어요" 를 알려 주는 RPC 쪽 **114 안내** 로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 코드에서는 rpcrt4 의 프로토콜 표 (rva `0xe1420`) 가 `ncalrpc` → 끝점 이름 `epmapper` 를 준다 `[정적]`.
- **`ept_map`** = 114 에 거는 **"번호 알려 주세요" 질문**. EPM 인터페이스 `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0 의 프로시저다. 동기면 3 / 7, **이번 (비동기) 은 프로시저 8** `[정적]`.
- **탑 (tower)** = 그 질문에 넣는 **질문지 묶음** — "인터페이스 `7ea70bcf…` + 전송 문법 + `ncalrpc`" 를 층층이 쌓은 꼴. 답도 탑 목록 (끝점 이름이 든다) 으로 온다 `[정적]` (탑 안 층 번호 · 검사 칸은 `[공백]` → Q230).

#### 사슬 (ret 자리)

1. winnsi `Ndr64AsyncClientCall` → (NDR 해석기, 이름만) → `LRPC_BASE_BINDING_HANDLE::DriveStateForward` `+0x8cf` → **`ResolveEndpoint`** (바인딩 `0x59258`)
   - `+0xc9` 메모판 `[바인딩+0x114] = 3` (번호 푸는 중).
2. `+0x130` → `SubmitResolveEndpointRequest`
   - `+0x13b ResolveEndpointWithoutEpMapper` — 기본 끝점을 훑지만 **0 개** 라 못 얻는다.
3. `+0x19a` → `EP_LOOKUP_DATA::ResolveEndpoint` (`0x59d38`) — `+0xbf TowerConstruct` (탑 만들기)
4. `+0x16b` → **`BindToEpMapper`** — `+0x128 RpcBindingCreateW` (끝점 `"epmapper"`, 보안 6 / 10) → `+0x146 RpcBindingBind` ⇢ (교환원, 가상 Bind — 구현 이름은 `[추측]`) ⇢ `DriveStateForward`
5. `+0x51a` → `LRPC_CASSOCIATION::Connect` → `+0x19d` **`AlpcConnect`** → `+0x29a` **`NtAlpcConnectPort(\RPC Control\epmapper)`**
6. (연결 뒤) `LookupNextChunk +0x122` → `Ndr64AsyncClientCall` **EPM 프로시저 8 = `ept_map`**
7. → `LookupNotifyRoutine` → … → `ResolveEndpointComplete` → 바인딩 **`CompleteOnePart`** (`0x5aeec`)
   - `+0xc7` 메모판 `[+0x114] = 4` (성공) / `1` (실패) · `+0xed SignalAllIos` (기다리던 모두 깨움)

- `BindToEpMapper` 가 실패하면 `RpcBindingFree` + `0x6d8` (1752 EPT_S_CANT_PERFORM_OP) `[정적]`.
- 114 가 "그런 인터페이스 등록 안 됐어요" 를 보내면 `FatalLookupErrorEncountered` 가 `0x6d9` (EPT_S_NOT_REGISTERED) 로 바꾼다 (DCE 상태 표 38쌍) `[정적]`.

#### 그래서 로그의 그 줄은

- 요청 쪽 로그의 `NtAlpcConnectPort \RPC Control\epmapper` (2204) 는 **nsi 사무실 (nsisvc) 연결이 아니다**. 번호를 묻는 **114 연결** 이다 `[정적]`.
- nsi 사무실 연결은 그 **다음** 일이다 (2.13 ⑤).

비유: 전화 부서는 카드의 빈 번호 칸을 보고, 메모판에 "nsi 번호 알아보는 중" 이라 적은 뒤 114 에 전화를 건다. 114 에게서 번호를 받으면 메모판을 "번호 앎" 으로 바꾸고, 기다리던 사람들을 모두 부른다.

### 2.8 ★ `AlpcConnect` — 상태 → RPC 오류 표, 그리고 "빈 봉투" `[정적]`

run 문서 5절. 사용자가 궁금해할 둘째 지점 (어떤 status 로 답할까) 의 바탕이다.

#### 표 (요약 — 전체는 run 문서 5절)

`NtAlpcConnectPort` 가 돌려준 상태를 rpcrt4 가 어떤 RPC 오류로 읽는가.

| 관리사무소 대답 (상태) | rpcrt4 가 읽는 것 | 다시 시도 |
|---|---|---|
| `0` 성공 | 동기: `0`. **비동기: 보류 `0xc0021018` + `*출력 = 1`** → `Connect` 가 `EnableAsync` | — |
| **`0xc0000034`** ("그런 이름 없음") | **`0x6ba` (1722, 서버 없음)** | 없음 |
| 표에 없는 모든 상태 | `0x6ba` | 없음 |
| **`0xc0000041`** (연결 거부) · `0xc0000017` · `0xc0000044` | **`0xe` (메모리 부족)** | 없음 |
| `0xc0000022` · `0xc00000a5` | `5` (접근 거부) | 없음 |
| `0x102` · `0xc0000701` | `0x71a` (취소) | 없음 |
| `0xc00002a0` | 재시도 → 안 되면 `0xc0021017` (→ `Connect` 가 `5`) | **서버 SID 를 줬을 때만** |

- 줄 근거: `0xc0000034` → `+0x2ee` · `+0x2f4 → +0x30e`, `0xc0000041` → `+0x2c8 ~ +0x2dc` → `+0x790`. `+0x30e` 가 "기본 착지" 라서 표에 없는 건 모두 1722 다.
- 상태 이름 중 `0xc0000036` · `0xc0000039` · `0xc0000701` · `0xc00002a0` 의 이름은 `[추측]`.
- 반환 직전 (`+0x10e`): 연결 객체 깃발 `0x400` 이 켜져 있으면 `5` · `0xc0021017` 도 `0x6ba` 로 바꾼다.

#### 읽을 것 셋

1. **"연결 거부" 가 "메모리 부족" 으로 둔갑한다.** `0xc0000041` 은 이름만 보면 "서버가 거절했어요" 로 딱 맞아 보이지만, rpcrt4 는 그것을 `0xe` 로 읽는다 (`+0x2c8 CMP EDI,0xc0000041`). 부른 쪽은 "메모리가 모자라서 실패" 로 받는다.
2. **재시도는 거의 없다.** 다시 시도하는 건 `0xc00002a0` 하나인데, **서버 SID 를 지정해 연결했을 때만** 생긴다. 첫 연결은 서버 SID 가 NULL (`[this+0xc0]`) 이라 안 일어난다.
3. **성공이면 핸들 값을 안 본다.** `+0x379 TEST EDI` 는 **상태만** 본다. 핸들은 `NtAlpcConnectPort` 의 첫 인자 = 연결 객체 `+0x88` 칸에 **관리사무소가 직접** 써 넣는 것이고, rpcrt4 는 그 값을 검사하지 않는다 (2.9).

#### 새 말 — 들어오는 속성 (빈 봉투)

- `NtAlpcConnectPort` 의 인자 중 "들어오는 메시지 속성" (InMessageAttributes) 칸 — 답장에 딸려 올 **첨부물을 받을 봉투** 다.
- rpcrt4 는 이 칸에 **늘** 8 B 짜리 **빈 봉투** (`AllocatedAttributes 0`) 의 주소를 넣는다 (`+0x243` · `+0x25a` · `+0x25e`) — **NULL 인 적이 없다** `[정적]`.
- 비유: 전화 부서는 전화를 걸 때마다 "첨부물 받을 칸 0 개짜리 빈 봉투" 를 같이 내민다. 봉투가 있긴 하지만 **안은 비어 있다**.

#### 부를 때 인자 (요약)

- PortHandle = `&this+0x88` (처음 0) · 객체 속성 0x30 B 중 길이만 · 포트 속성 0x48 B (Flags `0x2090000` 등, MaxMessageLength `0x1000`, DupObjectTypes `0xffd`).
- **Flags: 동기 `0x20000` / 비동기 0** (이번 winnsi 는 비동기 → 0).
- 서버 SID `[this+0xc0]` (처음 NULL) · 연결 메시지 · 길이 · 나가는 속성 **0 / 0 / 0** · 들어오는 속성 = **빈 봉투** · Timeout 동기만.

#### plain 인가 Ex 인가 `[정적 + 공백]`

- 호출자가 명부 (SD) 를 안 줬으면 rpcrt4 는 전역 **`gDefaultSecurityDescriptor`** 로 채우고 (`+0x3f2 ~ +0x3f5`), 그게 비-NULL 이면 `NtAlpcConnectPortEx` 로 간다 (`+0x27a TEST RDX / +0x27d JNZ +0x3a9`) `[정적]`.
- 이 전역은 **run99 의 기본 명부** 다 — 전화 부서를 차릴 때 `InitializeDefaultSecurityDescriptor +0x12d` · `+0x13c` 가 채운다 (flow-writer 가 이음).
- run99 에서 "이 초기화가 실패하면 RPC 런타임 전체가 `0xe`" 였으니, 초기화가 끝났다면 이 전역은 채워져 있고 Ex 길이 예상된다 `[추측]`.
- 그런데 요청 쪽 로그는 **plain** `NtAlpcConnectPort` 다. 어느 쪽인지 `[공백]` → Q231 (plan.md ④).

### 2.9 전화 부서의 메모판 두 장 — 상태 칸과 "줄 서기" `[정적]`

run 문서 6절.

#### 새 말 — 연결 객체 · 바인딩 핸들의 상태 칸

rpcrt4 안에는 "지금 어디까지 했나" 를 적어 두는 칸이 둘 있다. 공개 PDB 라 타입 이름은 없고, 칸 뜻은 코드 줄에서 읽은 것이다 (상태 이름은 `[추측]`).

| 메모판 | 무엇 | 칸 | 값 |
|---|---|---|---|
| **연결 객체** `LRPC_CASSOCIATION` | 전화선 하나 | `+0x4c` | 1 처음 · 3 연결 중 · 4 연결됨 · 2 · 5 종료 |
| **바인딩 핸들** `LRPC_BASE_BINDING_HANDLE` | 연락 카드 | `+0x114` | 1 처음 · **3 끝점 푸는 중** · 4 풀림 |

- 연결 객체에는 `+0x88` 포트 핸들, `+0xc0` 서버 SID 칸도 있다.

#### 새 말 — 보류 (`0xc0021018`)

- rpcrt4 안쪽 값으로, "지금 바로 결과는 없어요. **나중에 알려 줄게요**" 라는 뜻이다 `[정적: 쓰이는 자리]`.
- 비유: 창구 직원이 "번호표 받고 앉아 계세요" 하는 것. 줄을 섰다는 표시다.

#### 메모판이 바뀌는 줄

- **연락 카드 (`+0x114`)**:
  - `ResolveEndpoint (0x59258)`: 상태 **1** 이면 `+0xc9` 에서 **3** 으로 쓰고 114 에 묻는다.
  - 상태가 1 이 **아니면** (= 누가 이미 묻는 중) `+0x90 Register` 로 **기다리는 사람** 으로만 올라간다.
    - 동기 부름 → `SyncWait`.
    - **비동기 부름 → `+0x30e` 보류 `0xc0021018` 반환.**
  - 바인딩 `CompleteOnePart (0x5aeec)`: `+0xc7` 에서 **4 / 1** 로 되돌리고 `+0xed SignalAllIos` 로 **기다리던 모두를 깨운다**.
  - **이 `+0xc7` 줄에 닿아야만 3 이 풀린다.**
- **전화선 (`+0x4c`)**:
  - `Connect`: 상태 1 이면 `+0x10c` 에서 3 으로 → `Register` → `AlpcConnect`. 상태 3 이면 기다리는 사람으로만.
  - 연결 객체 `CompleteOnePart (0x3b148)`: 성공 `+0x26e` 에서 4, 실패면 핸들 닫고 상태 2 (오류 보관), 끝에 `SignalAllIos`.
- **비동기 기다리는 사람은 자기 신호등이 따로 없다.** 깨울 때는 **부른 쪽 (winnsi) 의 완료 신호등** 을 `IssueNotification (0x345c4) +0xc5 SetEvent` 가 켠다.

#### "성공인데 핸들 0" 이면 무엇이 일어나나

- 비동기 + 상태 0 → `AlpcConnect` 가 보류 + `*출력 = 1` → `Connect +0x3b1 EnableAsync` → `CreateAlpc` → `TpAllocAlpcCompletionEx(…, 포트 핸들, LrpcIoComplete, …)` `[정적]`.
- 이 길 **어디에도 핸들 값 검사가 없다** `[정적]`.
  - `AlpcConnect +0x379` 는 상태만 본다.
  - `IsPortWithSharedSection` 은 핸들이 0 이면 `NtAlpcQueryInformation` 도 안 부르고 1 을 돌려준다.
- 그러니 핸들 0 은 **아무도 눈치채지 못한 채** 스레드풀 쪽 (일꾼 공장, run125) 으로 흘러간다.

### 2.10 ★ 에뮬레이터에서 왜 끝없이 기다리나 — 한 걸음씩

사용자가 가장 궁금해할 첫째 지점이다. 걸음마다 태그를 붙인다.

#### 0. 시작 — 에뮬레이터가 "빈 봉투" 에서 멈춘다 (결과서 4절 — PE_emulator 코드 읽기)

- 결과서가 PE_emulator `nt_alpc_create_port.cpp:192` 를 읽었다 (flow-writer 는 안 읽음, 우리 태그 밖):
  - `a[8]` 이나 `a[9]` (나가는 · 들어오는 속성) 가 0 이 아니면 `stopped` → **핸들을 쓰기 전에** 나간다.
- 그런데 rpcrt4 는 `a[9]` 에 **늘** 빈 봉투를 준다 (2.8) `[정적]`.
- → 에뮬레이터는 epmapper 연결마다 "속성 있음" 으로 멈추고, 런너가 그걸 성공으로 처리해 **"성공인데 핸들 안 씀"** 이 된다. 결과서는 이 줄을 **"막힘의 직접 원인"** 이라 부른다.

#### 1. 첫 부름 (IPv4) — 메모판에 3 을 쓴다 `[정적]`

- winnsi `+0x112` `0x12000f` (에뮬레이터가 이제 대답함) → `+0x168` 신호등 → `+0x1e8` 프로시저 5.
- rpcrt4 `ResolveEndpoint +0xc9`: **`[바인딩+0x114] = 3`**.
- → … → `AlpcConnect +0x293 NtAlpcConnectPort(\RPC Control\epmapper)`.

#### 2. 대답 = "성공, 핸들 0" → 검사 없이 계속 `[정적]`

- 상태 0 → 비동기 보류 → `EnableAsync` → `CreateAlpc` → `TpAllocAlpcCompletionEx(핸들 0)` (2.9).

#### 3. 어디선가 사고 (치명 예외) → 신고센터 길 `[정적: 길의 정체]` · `[공백: 사고 자리]`

- 로그의 seq 2205 ~ 2224 는 **WER (신고센터) 길** 과 줄 순서가 딱 맞는다 (2.11) `[정적]`.
- 그 길이 열리려면 rpcrt4 예외 필터가 "**치명**" 으로 거른 예외가 바깥으로 나가야 한다 (2.11) `[정적]`.
- → 결과서 읽기: 로그의 첫 부름은 "RPC 오류" 가 아니라 **"치명 예외"** 였다.
- 사고가 **어디서** 났는지는 `[공백]`. 후보 둘 `[추측]`:
  - `CLIENT_IO_PROVIDER::Register +0x267` 의 `INT 0x29` (목록 깨짐 — 비상 정지 버튼)
  - winnsi `+0x925` 의 `INT 0x29` (같은 목록 검사)
  - 동적 후보로는 `EnableAsync` 의 핸들 0 도 적혀 있다 (run 문서 동적 5).

#### 4. 첫 부름이 `+0xc7` 에 못 닿은 채 나간다 `[추측]`

- 사고로 튀어 나가면, 메모판을 되돌리는 **바인딩 `CompleteOnePart +0xc7`** 에 닿지 못한다.
- → `[바인딩+0x114]` 는 **3 인 채로 남는다**.
- 이 걸음이 정말 이렇게 됐는지는 `[추측]` — 코드로 설명되는 길이 이것뿐이라는 뜻이다.
- flow-writer 보탬 `[추측]`: 로그에서 첫 부름은 WER 길을 지난 **뒤에도** 신호등을 닫고 (2226) IPv6 로 이어 갔다. 그러니 어딘가 바깥 처리기가 사고를 보고 (`RtlReportExceptionEx`) 잡은 뒤 winnsi 정리 길로 돌아왔다고 읽힌다. 어디서 잡혔는지는 동적 (Q231 ②) 으로 본다.

#### 5. 둘째 부름 (IPv6) — 같은 연락 카드, 메모판엔 여전히 3 `[정적]`

- IPHLPAPI 는 **같은 전역 바인딩 핸들** 을 계속 쓴다 (2.4) `[정적]`. 그러니 같은 메모판을 본다.
- winnsi `+0x112` `0x12000f` (2228) → `+0x168` 신호등 (2230) → `+0x1e8` 프로시저 5.
- `ResolveEndpoint`: 상태가 1 이 아니다 (3) → **기다리는 사람으로 등록** (`+0x90`) → 비동기라 **`+0x30e` 보류 `0xc0021018`**.
- → 114 에 다시 묻지 않는다. 그래서 로그에 **`NtAlpc*` 가 0 번** 이다.

#### 6. winnsi 는 신호등만 쳐다본다 — 영원히 `[정적]`

- 예외 없이 돌아왔으니 `+0x239 WaitForSingleObject(신호등, INFINITE)` (2233).
- 이 신호등을 켜 줄 것은 `IssueNotification +0xc5 SetEvent` 이고, 기다리는 사람을 깨우는 건 바인딩 `CompleteOnePart +0xed SignalAllIos` 다.
- 그런데 그 줄을 탈 일 (114 질문이 끝나는 일) 이 **이미 사고로 사라졌다**. → 깨울 사람이 없다.

#### 정리 — 무엇이 확실하고 무엇이 아닌가

| 걸음 | 태그 |
|---|---|
| rpcrt4 가 늘 빈 봉투를 준다 / 상태 0 이면 핸들을 안 본다 | `[정적]` |
| `+0x114` = 3 은 `+0xc7` 에서만 풀린다 / 3 이면 비동기 부름은 보류 | `[정적]` |
| seq 2205 ~ 2224 = WER 길, 치명 예외일 때 열린다 | `[정적]` |
| 첫 부름이 `+0xc7` 에 못 닿고 나갔다 | `[추측]` |
| 사고가 어디서, 무슨 예외였나 | `[공백]` |

비유: 첫 손님이 전화 부서에 와서 메모판에 "**nsi 번호 알아보는 중**" 이라 적고 114 에 전화를 건다. 그런데 114 연결이 "됐어요" 소리만 나고 실제 선은 안 이어진 상태다. 손님은 그걸 모르고 다음 단계로 가다가 **사고** 가 나서 실려 나간다 (신고센터). 메모판은 아무도 안 지운다. 두 번째 손님이 와서 메모판을 보고 "아, 알아보는 중이구나. 끝나면 신호등 켜 주세요" 하고 의자에 앉는다. 그 신호등은 영원히 안 켜진다.

### 2.11 seq 2205 ~ 2224 — rpcrt4 가 아니라 신고센터 (WER) 길 `[정적]`

run 문서 7절.

#### 로그 줄 ↔ 코드

| 로그 (요청 쪽) | 누가 부르나 | 거슬러 올라가면 |
|---|---|---|
| `NtQueryInformationProcess 0x25` | `ntdll!WerpIsProcessNative +0x2d` | ← `RtlReportException +0x54` ← `RtlReportExceptionEx +0xb5` |
| `NtSetInformationProcess 0x3f` | `ntdll!WerpSetProcessFaultInformation +0x2c` | ← `RtlReportExceptionEx +0x10d` (kernelbase `UnhandledExceptionFilter +0x110` / `+0x282` 에도 같은 부름) |
| `NtCreateSection` | `ntdll!WerpCreateCrashDataSection +0xa8` | ← `RtlReportExceptionEx +0x13a` |
| `NtDuplicateObject ×2` | `ntdll!RtlReportExceptionEx +0x172` · `+0x1a9` | |
| `NtOpenEvent \KernelObjects\SystemErrorPortReady` | `kernelbase!WaitForWerSvc` | ← `RaiseFailFastException +0x12e` ← `UnhandledExceptionFilter +0x39d` |

- 순서 (조회 → 설정 → 섹션 → 복제 ×2) 가 `RtlReportExceptionEx` 의 줄 순서 (`+0xb5` → `+0x10d` → `+0x13a` → `+0x172` → `+0x1a9`) 와 맞는다.
- kernelbase 쪽 `SystemErrorPortReady` 와 이어지는 정확한 사슬은 `[공백]`.
- 처리 안 된 예외 (`UnhandledExceptionFilter`) 길 자체는 run82 의 유저 쪽 예외 이야기에 있다.

#### 새 말 — 치명 예외 14종

- winnsi 의 `__except` 는 rpcrt4 `I_RpcExceptionFilter` (rva `0x3c900`) 에게 "잡을까?" 를 묻는다 (2.5 걸음 4).
- 이 필터는 `RpcpIsFatalError` 표 (rva `0xeb888`, **14개**) 에 있는 코드면 **"여기 아님" (0, 바깥으로 넘김)** 을 돌려준다 `[정적]`.
  - 예: `0xc0000005` (접근 위반), `0xc00000fd` (스택 넘침), `0xc0000409` (`__fastfail`) 등. 전체 목록은 run 문서 7절.
- 그 밖 (RPC 오류 코드) 은 winnsi 가 **스스로 잡는다**.
- 비유: 신청 담당의 대피도에는 "전화 고장 · 번호 없음 같은 **작은 사고** 는 여기서 처리, **큰 사고** (14종) 는 위로 넘겨라" 라고 적혀 있다. 큰 사고가 위로 올라가면 결국 신고센터 (WER) 가 열린다.
- → 그래서 "WER 길이 찍혔다 = 첫 부름은 RPC 오류가 아니라 큰 사고였다" 로 읽는다 (결과서).

### 2.12 ★ 어떤 status 로 답해야 안전한가 — T1 · T2

사용자가 궁금해할 둘째 지점이다. run 문서 "PE_emulator 쪽" (결과서 4절). `../PE_emulator` 코드는 flow-writer 가 안 읽었고, 고칠지는 **사용자 결정** 이다.

#### 먼저 — "핸들 없는 성공" 은 틀린 답이다

- 2.10 처럼 메모판 (`+0x4c` 와 `+0x114`) 에 3 을 남길 수 있다. 결과서 T1 의 결론이다.

#### 서버 모델이 **없는** 포트 → `0xc0000034`

- 걸음 `[정적]`:
  1. `AlpcConnect` 가 `0xc0000034` 를 `0x6ba` (1722, 서버 없음) 로 읽는다 (`+0x2ee` · `+0x2f4 → +0x30e`).
  2. 비동기 실패면 `Connect` 가 `+0x32f ~ +0x35a` 에서 핸들 칸을 0 · −1 로 치우고 연결 객체 `CompleteOnePart` 로 오류를 알린 뒤 보류를 돌려준다 (결과서 2-2). 연결 객체는 상태 2 (오류 보관).
  3. 끝점 풀이가 실패로 끝나면 바인딩 `CompleteOnePart +0xc7` 이 메모판을 **1** 로 되돌리고 `+0xed SignalAllIos` 로 기다리는 사람을 깨운다.
- → **메모판이 깨끗해진다.** 둘째 부름은 처음부터 다시 하거나 바로 오류를 받는다. 무한 대기가 없다.
- 단, 연결 실패 숫자가 바인딩 완료까지 같은 숫자로 전달되는 안쪽 (`RpcBindingBind` 비동기 완료) 은 안 읽었다 `[추측]` (결과서 2-3).
- 그래서 winnsi 가 **최종으로 받는 숫자** 가 1722 인지 1752 인지는 `[공백]` → Q231 (plan.md ①).
- 곁 근거 (요청서, 에뮬레이터 로그): `keysvc` · `eventlog` 18 샘플이 이미 이 꼴로 "멈추지 않고 이어 간다".

#### `0xc0000041` 은 쓰지 말 것

- 이름 (연결 거부) 은 그럴듯하지만, rpcrt4 는 **`0xe` (메모리 부족)** 로 읽는다 (2.8) `[정적]`.
- 부른 쪽이 "메모리 부족" 을 받고 무엇을 할지는 이번 범위 밖이다. 적어도 "서버 없음" 이라는 뜻은 전달되지 않는다.

#### 서버 모델이 **있는** 포트 → 핸들을 쓰고 성공

- `*PortHandle` 에 진짜 핸들을 쓰고 상태 0. 들어오는 봉투는 `ValidAttributes = 0` 으로 두면 된다.
- 비동기 (Flags 0) 면 이어서 `NtAlpcSetInformation` 이 온다 (`TpAllocAlpcCompletionEx` 안) `[정적]`.

#### 에뮬레이터 줄마다 (결과서 판단)

| 코드 | 지금 | 해야 할 일 |
|---|---|---|
| `nt_alpc_create_port.cpp:192` (`NtAlpcConnectPort`) | **틀림 — 막힘의 직접 원인** | `a[8]` NULL · `a[9]` 의 `AllocatedAttributes == 0` 이면 "속성 없음" 으로 보고 `ValidAttributes = 0` 쓰고 핸들을 쓴다. 속성이 진짜 있을 때만 멈춤 |
| `:346` (`NtAlpcConnectPortEx`) | **같은 이유로 틀림** (Ex 길도 같은 빈 봉투) | 위와 같음 |
| `:305` (`a[5]` 서버 보안 요구) | Ex 길 한정 — 이번 원인 아님 | — |
| `:623` (`NtAlpcSendWaitReceivePort`) | **맞음** (메시지 전달 미구현 선언) | `:192` 를 고치면 처음 쓰인다 → 거기서 "서버 없음" 답 필요 |

#### T2 — 가장 값싼 길

- 알림 등록을 정말 성공시키려면 2.13 의 ① ~ ⑧ 이 다 필요하고, 그중 bind · 머리 칸은 아직 모른다 (Q230).
- 가장 값싼 길 (결과서): **epmapper 연결을 `0xc0000034` 로 실패** 시킨다.
  - → IPv4 · IPv6 둘 다 RPC 오류 → `NotifyIpInterfaceChange` 오류 → `WinHttpSendRequest` 0.
  - **유한하게 끝난다** — 무한 대기도, 치명 예외도 없다.

비유: 114 에 걸었는데 연결이 안 되면, 에뮬레이터 (관리사무소 흉내) 는 "**그런 번호 없어요**" 라고 또렷하게 말해 줘야 한다. 그러면 전화 부서는 메모판을 지우고 손님들에게 "안 됩니다" 라고 알린다. "됐어요" 하고 선을 안 이어 주는 게 제일 나쁘다. 그리고 "거절합니다" 라고 말하면 전화 부서는 엉뚱하게 "**종이가 다 떨어졌어요**" 로 알아듣는다.

### 2.13 서버 쪽 최소 — 틀만 `[정적 + 공백]`

run 문서 8절. 등록을 진짜로 한 번 성공시키려면, 서버 (흉내) 가 이 차례로 답해야 한다.

| 단계 | 클라이언트가 보내는 것 | 서버가 돌려줘야 하는 것 |
|---|---|---|
| ① 114 연결 | `NtAlpcConnectPort(\RPC Control\epmapper, …)` | **핸들을 쓰고 성공** (봉투는 `ValidAttributes = 0`). 비동기면 `NtAlpcSetInformation` 이 이어짐 |
| ② 핸들 질문 | `NtAlpcQueryInformation(핸들, 0, 0x10 B)` | 오류여도 됨 |
| ③ 114 bind | EPM v3.0, 전송 문법 | bind 확인 — `[공백]` → Q230 |
| ④ `ept_map` | 비동기 프로시저 8: 탑 = nsi 인터페이스 + 전송 문법 + `ncalrpc` | 탑 1 개 이상 (끝점 이름) + 상태 0. 비면 `0x6d9` — 탑 층 검사는 `[공백]` → Q230 |
| ⑤ nsi 사무실 연결 | `\RPC Control\<끝점>` | ① · ② 와 같음 |
| ⑥ nsi bind | `7ea70bcf…`, NDR 2.0 + NDR64 | bind 확인 (고른 전송 문법) — `[공백]` → Q230 |
| ⑦ 프로시저 5 | 24 B 구조체 · compartment 8 B | **컨텍스트 핸들 (선 위 20 B) + 반환 DWORD 0** |
| ⑧ 프로시저 7 | 컨텍스트 핸들 | **답하지 않는다** (걸어 둠) |
| ⑨ 성공 뒤 | — | 가짜로 답할 것 아님 — 클라이언트가 스스로 부르는 syscall (힙 · 이벤트 · 대기 등록 · WNF) |

- **`ept_map` 을 건너뛸 길은 없다** — 인터페이스에도 카드에도 끝점이 없다 (2.4).
- compartment 8 B = `NsiGetAllParameters(NDIS 객체 8)` 의 `{번호, 범위}` 를 서로 바꾼 것. run128 9절 (a) 의 `{1, 0}` 대답이면 `{범위 0, 번호 1}` 이 된다.
- 컨텍스트 핸들 값을 검사하는 줄은 못 찾았다. 안전하게는 0 이 아닌 값 (속성 0 + 새 GUID) `[추측]`.
- 서버 (nsisvc) 가 프로시저 5 입력을 어떻게 검사하는지는 안 열었다 `[공백]` → Q230.
- ⑧ 이 재미있다: **프로시저 7 은 답하면 안 된다**. 7 은 "소식이 오면 알려 주세요" 라서, 답이 온다는 건 "소식이 왔다" 는 뜻이다. 에뮬레이터는 알릴 소식이 없으니 걸어만 두면 된다 (결과서 3-2).

비유: 손님 신청 한 번을 받으려면 — 114 가 전화를 받고 (①) · 인사 (③) · 번호를 알려 주고 (④) · nsi 사무실이 전화를 받고 (⑤) · 인사 (⑥) · 접수 번호표를 주고 (⑦) · "소식 오면 연락할게요" 하고 **아직 연락은 안 하는** (⑧) 것까지 다 해야 한다. 인사 (bind) 의 말투가 아직 공란이다.

### 2.14 flow-writer 대조 · 요청 쪽 로그와 다른 곳

run 문서 "flow-writer 대조" · "요청 쪽 로그와 코드가 다른 곳". 뜻이 바뀌는 건 없고, 작게 보탠 것들이다.

- **`0x12000f` 실패 길엔 `CloseHandle` 도 없다** — `+0x120 JNZ → +0x636` 은 신호등을 만들기 전이다 (2.6 에 넣음).
- 실패 정리의 합류 자리: `+0x684 JZ` → `+0x61e` → `+0x626 CloseHandle` (ret `+0x62d`).
- kernelbase `UnhandledExceptionFilter +0x110` / `+0x282` 의 ret = `+0x117` / `+0x289` (`_calls.tsv`).
- `gDefaultSecurityDescriptor` 를 채우는 쪽이 run99 의 `InitializeDefaultSecurityDescriptor` 라는 이음 (2.8 에 넣음).
- 첫 부름이 WER 길 뒤에도 IPv6 로 이어 갔다 = 어딘가 잡혔다 `[추측]` (2.10 걸음 4 에 넣음).
- 요청 쪽 로그와 다른 곳: "첫 / 둘째 등록" (2.3), seq 2199 "수동 리셋" (2.5), plain `NtAlpcConnectPort` (2.8), "성공인데 핸들 안 씀" (2.9 · 2.12).

### 2.15 정리 — 비유로 한 번 더

- 비서 (IPHLPAPI) 는 신청서를 IPv4 · IPv6 두 장 쓴다. 한 장만 접수돼도 성공.
- 신청 담당 (winnsi) 은 카드에 **회사 이름만** 적어 두고 (바인딩, 끝점 없음), 신청할 때마다
  - 안내 데스크에 망 구획 질문지 (`0x12000f`) 를 먼저 내고 — 반려되면 거기서 끝 (run128 정정),
  - 신호등을 세우고, 전화 부서에 "5번 메뉴, 끝나면 신호등" 을 맡기고 쳐다본다.
- 전화 부서 (rpcrt4) 는 메모판에 "번호 알아보는 중 (3)" 을 적고 114 (epmapper) 에 전화한다.
  - "그런 번호 없어요" (`0xc0000034`) 를 들으면 메모판을 지우고 모두에게 "안 됩니다 (1722)".
  - "됐어요" 소리만 듣고 선이 없으면 (핸들 0) 눈치채지 못하고 가다가 — 이번 로그처럼 사고가 나면 메모판이 3 인 채 남는다.
- 다음 손님은 메모판 3 을 보고 줄만 선다. 신호등은 영원히 안 켜진다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git diff -- runs/03_sprint/flow.md`)

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 | winnsi 없음 | **winnsi.pdb 한 줄** (`907CE42FFA71F5085D1468AD2D40D62C1`, 함수 86, 지연 가져오기, `pefile` 로 인터페이스 · 형식 읽음) |
| 기준 — 근거 줄 | — | **(run129) 정적 바퀴, 사진 값 대조 없음** (test.exe 가 안 올림 `[관찰]` · API 없음 `[소스]`, 요청 쪽 값은 에뮬레이터 로그라 `[관찰]` 아님) |
| 1장 그림 | — | **안 바뀜** (범위 밖) |
| 3장 run99 절 "ALPC 는 이 초기화에 없고" 줄 | "(`AlpcConnect` 쪽)" | + "**(run129) 여기서 채운 `gDefaultSecurityDescriptor` 가 `AlpcConnect +0x3f5` 에서 `NtAlpcConnectPortEx` 를 고르는 조건**" |
| 3장 run128 절 나무 winnsi 줄 | "(RPC, `[공백]` → run129)" | "**(바인딩만, 연결 안 함 → run129)**" |
| 3장 run128 절 F1 줄 | "F1 이 F2 보다 앞이라 … 원인은 정적으로 못 찾음 `[공백]`" | 그 말에 **취소선** + "**(run129 정정) F1 안에서도 `0x12000f` 가 난다** … 로그 대응은 `[추측]` (Q222 · Q231)" |
| 3장 끝 | "NSI 부르는 쪽" 절 (run128) 까지 | **새 절 "winnsi 알림 등록 RPC"** `[정적]` — 114 비유, 나무 하나, 일곱 줄 요약 (IPv4 → IPv6 · epmapper · `0x12000f` · 상태 → 오류 · 끝없는 기다림 · WER 길 · 서버 최소) |
| 4장 run128 줄들 | 정적 ① "→ run129 예정", 대기열 번호 없음 | ① **해결됨 → run129**, ② ~ ⑥ = **Q237**. 동적 1 에 "볼 값은 `+0x119` · `+0x259`" 보탬 |
| 4장 새 줄 넷 | — | **"3 (범위 밖 — NSI)" run129**: 해결됨 · 일부 (Q223) · 동적 (Q231) · 정적 (Q230 · Q232) · PE_emulator 쪽 |
| 5장 근거 run | — | **run129 줄** 둘 |

- 태그 변화:
  - run128 "`NsiConnectToServer` 가 서버를 만나나" — `[공백]` → **`[정적]`** (바인딩만, 연결 안 함).
  - run128 "`0x12000f` 가 보인 호출의 0 반환" — `[공백]` → **정적 후보 사슬 `[정적]`** + 로그가 그 자리 것인지는 `[추측]`.
  - 새로 생긴 것은 `[정적]`, 그리고 `[공백]` 넷 (첫 부름의 사고 자리, 서버 없음의 최종 숫자 1722 / 1752, plain vs Ex 의 까닭, 프로시저 8 의 쓰임).
- run128 문서 맨 위에도 "일부 닫힘 · 정정 (run129)" 한 줄이 붙었다.
- 짐 지도 (`baggage.md`) 는 안 바뀌었다 — 초기화 동안의 짐이 아니다.

### 큰 그림에서 어떻게 이어지나

- 흐름도 본선 (프로세스 생성 → `NtContinue` 은퇴 → `main`) 에는 **변화가 없다**. 이번도 `main` 뒤 옆길의 지도다.
- run128 에서 "안내 데스크는 빈 장부로 정직하게 답하면 되고, 진짜 관문은 전화 쪽" 이라 했다. 이번에 그 **전화 쪽의 관문** 이 무엇인지 보였다.
  - 관문 1: 신청마다 나가는 `0x12000f` (안내 데스크 대답이 다시 중요해진다 — run128 정정).
  - 관문 2: 114 (epmapper) 연결에 어떤 status 로 답하나.
- **공통 몫** 이 크다. 같은 연결 틀 (빈 봉투, 상태 → 오류 표, 동기 / 비동기 Flags, `NtAlpcQueryInformation(0, 0x10)`, 끝점 없으면 epmapper + `ept_map`, 메모판 두 장) 이 LSA (`GetUserName*`) · RPCSS (`CoInitializeSecurity`) 에도 쓰인다 (결과서 4절).
  - 요청서의 `CoInitializeSecurity` 22 샘플 (`NtAlpcSendWaitReceivePort(핸들 0)` → `0xc0000008`) 도 같은 "핸들 없는 성공" 에서 갈라진 길이다. 그 `0xc0000008` 처리는 Q230 에서 읽는다.
- 앞 바퀴들과 이어지는 곳:
  - run99 — 전화 부서의 기본 명부 (`gDefaultSecurityDescriptor`) 가 이번엔 plain / Ex 를 가르는 조건으로 다시 나왔다.
  - run125 — 비동기 연결 뒤 `TpAllocAlpcCompletionEx` → `NtAlpcSetInformation` 은 일꾼 공장 이야기와 이어진다 (Q232).
  - run81 · run82 — 신고센터 (WER) 길이 실제 로그의 syscall 순서로 보였다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q231** (동적)

- **왜 이번에 못 알았나**: 정적만 했다. "어느 줄을 탔나", "무슨 예외였나", "최종 숫자" 는 실행 때 값이다. 그리고 test.exe 가 아니라 **winnsi 등록을 부르는 프로그램** (예: `NotifyIpInterfaceChange` 를 부르는 작은 exe, 또는 winhttp) 이 필요하다 — 고르기는 **사용자 결정**. Q222 와 같은 세션에 얹을 수 있다.
- 무엇을 보나 (유저 멈춤점, 프로세스 조건, 모두 ret 자리). plan.md 번호 (①~⑦) 와 run 문서 "아직 확인 필요" 동적 번호 (1~7) 가 **순서가 다르다** — 둘 다 적는다.

| plan.md | run 문서 | 무엇 | 닫는 것 |
|---|---|---|---|
| ① | 3 | `winnsi!NsiRpcRegisterChangeNotificationEx+0x259` `eax` · `[rsp+0x60]`, `+0x1ef` 에서 예외로 나왔나, `iphlpapi!InternalRegisterChangeNotification+0xa4` / `+0xdc` | 서버 없음의 최종 숫자 **1722 / 1752** (2.12) |
| ② | 5 | `ntdll!RtlDispatchException` 에서 `.exr` · `kv` | **사고 자리** (2.10 걸음 3), 어디서 잡혔나 (걸음 4) |
| ③ | 4 | `ResolveEndpoint+0xc9` 뒤 `[rdi+0x114]` = 3, `CompleteOnePart(0x5aeec)+0xc7` 이 불리나, 둘째 `+0x48` `r12d` | **메모판 3 이 정말 남나** (2.10 걸음 4 · 5) |
| ④ | 2 | `rpcrt4!LRPC_CASSOCIATION::AlpcConnect+0x29a` `eax` · `[rsi+0x88]` · 인자, `+0x27a` 의 `rdx` | **plain / Ex 까닭** (2.8) |
| ⑤ | 1 | nsisvc 도는 VM 에서 `ntdll!NtAlpcSendWaitReceivePort` 입구 `kv` + 송수신 `PORT_MESSAGE` | Q230 의 **시작점** (bind → `ept_map` → nsi bind → 프로시저 5, 순서는 `[추측]`) |
| ⑥ | 6 | 성공 쪽 (nsisvc 있는 VM) — `+0x259` 뒤 `[rsp+0x60]` = 0, `+0x555` · `+0x6bd`, 컨텍스트 핸들 8 B | 성공 길 (2.5 걸음 6) |
| ⑦ | 7 (flow-writer 보탬) | `winnsi!NsiRpcRegisterChangeNotificationEx+0x119` `eax` — Q222 ① 과 같은 실행 | 요청 로그의 `0x12000f` 가 **F1 (winnsi) 것인가 F2 것인가** (2.6) |

### 정적으로 남은 것 → **Q230** · **Q232**

| 대기열 | 무엇 | 왜 이번에 못 했나 |
|---|---|---|
| **Q230** | **LRPC 선 위 형식** — `LRPC_CASSOCIATION::Bind` · `CompleteBind` · `LRPC_BIND_CCALL::HandleReply` · `LRPC_CCALL::HandleReply` · `SendReceive` · `RpcpAlpcpGetHeaderSize` · `AlpcSendWaitReceivePort` · `RpcpStrictAlpcToRpcStatus` — 머리 칸 · 길이 · 메시지 종류, 핸들 0 보내기의 `0xc0000008` 처리 | 분량이 커서 요청서가 허락한 대로 다음 요청으로 넘겼다 (2.13 ③ · ⑥) |
| **Q230** | **nsisvc** analyze (필요한 함수만) — 끝점 이름, 프로시저 5 · 6 · 7 입력 검사, 컨텍스트 핸들 만드는 줄 | 질문 1 · 2 가 클라이언트 코드로 닫혀서 서버는 안 열었다 |
| **Q230** | **`ept_map` 응답 검사** — `ProcessNextChunk` · `ReturnLookupValues` · `TowerConstruct` → `OsfTowerConstruct`, 탑 층 번호 · `ncalrpc` 프로토콜 ID | 탑 안쪽은 이름만 봤다 (2.7) |
| **Q232** (낮음) | `CreateAlpc` → `TpAllocAlpcCompletionEx` → `NtAlpcSetInformation` 모양과 완료 통지 (run125 일꾼 공장 이음) | 이번 질문 (연결 · 상태) 밖 |
| **Q232** (낮음) | `RegisterNsiSvcRestartNotification` 의 WNF 상태 이름 | 성공 길에서만 타는 줄 (2.5 걸음 6) |

- run128 의 나머지 정적 (이름 풀이 길 · 커널 nsi 공급자 등) 은 **Q237** 에 그대로 있다.

### 대기열 밖 — 이름 · 짐작으로 남은 것

- 상태 이름 `0xc0000036` · `0xc0000039` · `0xc0000701` · `0xc00002a0`, 메모판 값 이름 (1 처음 · 3 푸는 중 …), 컨텍스트 핸들 종류 이름 — `[추측]`.
- 인증 상수 이름 (PKT_PRIVACY · WINNT) — `[추측: 지식]`.
- 가상 `Bind` 의 구현 이름 — `[추측]`.
- 프로시저 8 (winnsi) 을 누가 부르나 — `[공백]`.
- 요청 쪽 로그의 둘째 등록에 나온 `NtAllocateVirtualMemory ×2` 는 이번 문서들이 따로 설명하지 않았다.

---

## 5. 더 읽기

- run 문서: [`run129-winnsi-rpc.md`](run129-winnsi-rpc.md)
  - 호출 지점 표 전체 · 상태 → 오류 표 전체 · 서버 최소 표 · PE_emulator 표 → 그 문서 "결과" 1 ~ 8절, "PE_emulator 쪽"
- 결과서: [`_workspace/sast/127_winnsi-rpc_result.md`](../../_workspace/sast/127_winnsi-rpc_result.md) — "먼저 읽는 한 줄 답", 1절 (winnsi), 2절 (rpcrt4 연결 길 · 2-1 표 · 2-2 메모판 · 2-4 WER), 3절 (서버 최소), 4절 (PE_emulator)
- 요청서: [`_workspace/sast/127_winnsi-rpc.md`](../../_workspace/sast/127_winnsi-rpc.md) — "왜" 의 에뮬레이터 로그 (seq 2196 ~ 2233)
- flow.md: [`flow.md`](flow.md) — 3장 끝 "winnsi 알림 등록 RPC" 절 · 3장 "NSI 부르는 쪽" 절 (run128) F1 줄 · 3장 run99 절 · [4장 공백 목록](flow.md#4-공백-목록-다음에-채울-곳) "3 (범위 밖 — NSI)" run129 줄 · [5장 근거 run](flow.md#5-근거-run) run129 줄
- 바로 앞 바퀴: [run128 해설](run128-nsi-callers-해설.md) (2.6 F1 · 2.7 "로그의 0 은 왜" — 이번에 정정된 곳)
- 같은 그림 앞 바퀴: [run99 해설](run99-rpc-caller-해설.md) (전화 부서 · 기본 명부), [run97 해설](run97-rpc-sd-query-해설.md) · [run98 해설](run98-rpc-dir-create-해설.md) (`\RPC Control` 방), [run68 해설 2.4](run68-dll-init-rest-해설.md) (ALPC = 전화선 끝), [run81 해설](run81-exception-kernel-해설.md) · [run82 해설](run82-exception-user-해설.md) (사고 · 필터 · 신고센터), [run125 해설](run125-worker-factory-해설.md) (일꾼 공장)
- 대기열: `_workspace/plan.md` Q223 (끝) · Q230 (정적, LRPC 선 위) · Q231 (동적 ① ~ ⑦) · Q232 (정적, 낮음) · Q237 (run128 남은 정적) · Q222 (run128 동적, 같은 세션)

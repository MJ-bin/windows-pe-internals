# run124 afd-socket — 해설

> 이 글은 [run124 문서](run124-afd-socket.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q217** — PE_emulator 요청 "소켓 장치 (AFD)" (튜닝 `278_tune06.md P1`) (`_workspace/plan.md`, 이번에 **끝 (run124)**)
  - 사용자 결정 2026-11-16 "지금 즉시 먼저" (분석 멈춤 중 예외), 2026-11-22 "문서로 만들어서 커밋".
  - 이번에 남은 것이 간 곳 (4절):
    - 동적 → 새 **Q224** (소켓을 쓰는 프로그램 + 선 뽑은 VM 필요, 어떤 프로그램인지는 **사용자 결정**)
    - 정적 → 새 **Q225** (ws2_32 다리 · afd 칸 마저 · mswsock 시작 쪽 · tcpip · nsi · 이벤트 규칙 · provider 값)
  - 다음 문서 바퀴: run125 (Q218 일꾼 공장, sast/122) — plan.md 2026-11-22 결정 기록.
- 근거:
  - run 문서 [`run124-afd-socket.md`](run124-afd-socket.md) (가장 중요)
  - 요청서 [`_workspace/sast/121_afd-socket.md`](../../_workspace/sast/121_afd-socket.md) · 결과서 [`121_afd-socket_result.md`](../../_workspace/sast/121_afd-socket_result.md)
    - 판정 **분석성공**, 스크립트 문제 없음.
    - mswsock · afd · nsi · nsiproxy 넷을 **처음 analyze** 했다 (함수 781 · 1280 · 42 · 77).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 (PDB 넷 · 근거 줄) · **3장 끝 새 절** · 4장 세 줄 · 5장 두 줄. **그림은 안 바뀜.**
  - 짐 지도 `baggage.md` 는 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 소켓을 안 쓴다.** 그래서 이번 이야기는 전부 "**소켓을 쓰는 다른 프로그램이라면**" 이다.
   - 초기화 끝 (P2) 모듈 6개에 mswsock · ws2_32 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run91 · run92 와 같은 줄).
   - `sample/*.c` 에 소켓 API 가 없다 `[소스]` (`grep -i socket` 0건).
   - 그래서 흐름도 **그림은 그대로** 다. 짐 지도도 그대로.
2. **"에뮬레이터 로그" 는 진짜 Windows 로그가 아니다.**
   - 요청서의 syscall 차례 (`0x1207b` ×2 → `0x12047` …) 는 PE_emulator 가 흉내 낸 실행의 기록이다.
   - 이 글의 결론은 전부 코드를 읽은 것 `[정적]` 이다. 실제로 돌려서 본 건 없다 (→ Q224).
3. **IOCTL 에 이름이 없다.** 네 모듈 PDB 가 공개 심볼이라 구조체 타입 · IOCTL 상수 이름이 없다.
   - 그래서 주문서는 **숫자** (`0x1207b` 등) 와 **afd 에서 그 주문을 처리하는 함수 이름** (`AfdGetInformation` 등) 으로 부른다.
4. **IOCTL 이 아주 많이 나온다.** 헷갈리면 이 표로 돌아오자 (이번에 길에서 나온 것만, run 문서 "afd 제어 코드 → 처리 함수").

   | 코드 | afd 처리 함수 | 언제 |
   |---|---|---|
   | `0x1207b` | `AfdGetInformation` | 소켓 만든 직후 (첫 소켓만) |
   | `0x12047` | `AfdSetContext` | 소켓 만든 직후, bind 뒤 등 |
   | `0x120bf` | `AfdTliIoControl` | connect 앞 (옵션 `0x3007`) |
   | `0x12003` | `AfdBind` | connect 앞 (묵시적 bind) |
   | `0x12007` | `AfdConnect` | **connect 본체** |
   | `0x120c7` | `AfdSuperConnect` | `ConnectEx` (다른 길) |

5. **오프셋 두 가지.** 트리 (`├─ +0x..`) 의 숫자는 **ret 자리** (콜스택에 찍힐 값), 본문 "`+0x591 NtCreateFile`" 처럼 함수 이름 앞에 오는 건 **call 자리** 다. run 문서와 같은 규칙이다.
6. **`SockDoConnect` 안 순서는 "주소 순서" 가 아니라 "실행 순서"** 다 (2.11). setsockopt (`+0x4d7`) 가 bind (`+0x2a2`) 보다 뒤 주소인데 먼저 실행된다 `[정적: 갈래 읽기]`.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, [핸들](../02_sprint/run9-create-user-process-해설.md#핸들) = **손잡이**
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = **번호 붙은 주문서**, IRP = 주문서를 담는 **배달 봉투** → [run66 해설 2.7](run66-apphelp-cache-해설.md)
- **EA (확장 속성)** = 파일을 열 때 실어 보내는 "이름 = 값" 꼬리표, 비유로 **이름표를 붙인 서류 봉투** → [run83 해설 2.5](run83-console-kernelbase-해설.md)
- `STATUS_PENDING` (`0x103`) = "아직 처리 중" (음수가 아님) → [run92 해설 2.7](run92-bcrypt-ksecdd-해설.md)
- IOSB · `Information` = 상담실이 답장과 함께 주는 **전표** (위 칸 도장 `Status`, 아래 칸 "몇 바이트 적었음") → [run94 해설 2.6](run94-bcrypt-resolve-해설.md)
- 이벤트 = **신호등** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- IO 완료 포트 = **우편함** → [run62 해설 2.2](run62-tp-etw-init-해설.md#22-새-말--스레드-풀-한-벌의-부품)
- 점프 표 = 번호로 갈 곳을 찾는 표 → [run91 해설 2.9](run91-bcrypt-init-해설.md)
- ws2_32 = 소켓 기능의 바깥 창구, mswsock = 그 뒤에서 실제로 일하는 사무실 (일반적 설명 `[추측]`), TEB `WinSockData` 주인은 mswsock → [run121 해설 2.4](run121-teb-other-dlls-해설.md)
- status 부호 (음수 = 실패) 로 갈리는 꼴, "최소 조건 표" 라는 꼴 → [run92 해설](run92-bcrypt-ksecdd-해설.md) 2.7 · 2.11

이번에 처음 나오는 말 — **소켓**, **Winsock 공급자 (`WSP*`)**, **afd**, **비동기 핸들**, **TL 모드 / TDI 모드**, **정보 클래스 · 창 크기**, **`METHOD_NEITHER`**, **bind · 묵시적 bind**, **sockaddr**, **TL 공급자 (tcpip)**, **WSA 오류 번호**, **블로킹 / 비블로킹**, **nsi** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**.
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = **번호 붙은 주문서**, 핸들 = **손잡이**, 이벤트 = **신호등**, IOSB = **전표**.
- 이번에 새로 붙는 그림 (run 문서 "결과" 의 우체국 비유를 이 시리즈 그림에 끼운다):
  - **afd 상담실 = 우체국 창구.** 관리사무소 안 상담실 가운데, 소켓 일을 맡는 곳이다.
  - **소켓 하나 = 사서함 하나.** 소켓을 만든다 = 창구에서 사서함을 하나 여는 것.
  - **EA = 사서함 신청서.** run83 의 "이름표 붙인 서류 봉투" 그대로다. 이번 봉투 이름표가 `AfdOpenPacketXX` 가 아니면 창구가 안 받는다.
  - **tcpip = 배송 회사.** 창구 뒤에서 진짜로 편지를 실어 나르는 곳. "그 동네로 가는 길이 없어요" 는 이 회사가 말하고, 창구는 그 말을 **그대로** 전한다.
  - **mswsock = 가게 안 우편 담당 직원.** 손님 (프로그램) 의 `socket()` · `connect()` 를 받아 창구에 주문서를 낸다.
- 주의: run62 의 **우편함** (IO 완료 포트) 과 이번 **사서함** (소켓) 은 다른 물건이다. 우편함은 "일 끝났어요" 쪽지가 모이는 곳, 사서함은 우체국에 연 내 통로다.

### 한 줄로 먼저 — 무엇을 얻었나

> **소켓 = `\Device\Afd\Endpoint` 를 EA (`AfdOpenPacketXX`, `0x39` 바이트) 와 함께 여는 것.**
> **그 뒤 주문서 둘 (`0x1207b` · `0x12047`) 은 "성공 도장" 만 맞으면 된다.**
> **`connect()` 는 주문서 넷 (`0x120bf` → `0x12003` → `0x12047` → `0x12007`) 이고, "길 없음" 은 afd 가 아니라 tcpip 가 정해서 afd 가 그대로 넘긴다. mswsock 이 그걸 WSA 번호 (10051 등) 로 바꾼다.**

모두 `[정적]`. 다만 "길 없음" 의 **실제 값** 은 tcpip 몫이라 `[추측]` 이다 (2.13).

요청서 질문에 대한 답 (run 문서 "요약" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| 1 (a) EA 모양 | 이름 `AfdOpenPacketXX`, 값 안 **계열 `+0x20` · 종류 `+0x24` · 프로토콜 `+0x28`**. 기본 TCP/UDP 는 EaLength **`0x39`** `[정적]` | 2.4 · 2.5 |
| 1 (b) 열기 뒤 IOCTL 둘 | `0x1207b` = `SockGetInformation`, 입력 = 출력 `0x10` 칸. `0x12047` = `SockSetHandleContext`, 공유 정보 + 주소 둘 + helper 문맥 `[정적]` | 2.7 · 2.8 |
| 1 (c) mswsock 이 보는 것 | 열기 · `0x12047` 은 **status 부호만**, `0x1207b` 은 **반환값 버림**, 성공이면 출력 `+8` dword 만 `[정적]` | 2.9 |
| 1 (d) 성공 최소 응답 | 셋 다 status 0 + (열기) 새 핸들 + (`0x1207b`) `+8` 값 `[정적]` | 2.10 |
| 2 (a) connect IOCTL | `0x12007`, 입력 = `0x18` 머리 + 날 sockaddr, 출력 없음. 그 앞에 셋 `[정적]` | 2.11 · 2.12 |
| 2 (b) 길 없는 PC status | **afd 는 안 정함**, TL 공급자 값을 그대로. 실제 값은 `[추측]` | 2.13 |
| 2 (c) status → WSA 표 | `NtStatusToSocketError`: `0xC000023C` → 10051 등 `[정적]` | 2.14 |
| 2 (d) 완료 꼴 | 블로킹 · 비블로킹 · `ConnectEx` 셋이 다름 `[정적]` | 2.15 |
| 3 nsi (낮음) | 세 코드 · 입력 길이 · "개수 먼저 묻기" `[정적]` | 2.16 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**초기화 밖** 이다. 프로세스가 다 세워지고 `main` 이 돈 뒤, 프로그램 코드가 소켓을 부를 때의 이야기다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll: LdrInitializeThunk → LdrpInitializeProcess → DLL 들이기 · 전원 켜기
 └─ NtContinue (은퇴) → exe 진입점 → (CRT 점선 상자) → main
                                                        │
      test.exe 는 여기서 소켓을 안 쓴다 [관찰][소스]      │ 소켓을 쓰는 프로그램이라면:
                                                        └─ socket() / connect()
                                                             (ws2_32 → mswsock 다리는 [공백])
                                                             └─ mswsock!WSPSocket / WSPConnect     ← 이번 (run124)
                                                                  └─ NtCreateFile · NtDeviceIoControlFile
                                                                       ⇒ 커널 afd.sys → TL 공급자 (tcpip [추측])
```

- flow.md 에서는 **3장 끝 새 절** "소켓 장치 AFD" 다. 3장은 원래 "같은 시간대의 다른 스레드" 장인데, 그 뒤쪽에 콘솔 · user32 · bcrypt · RPC · `\SystemRoot` 같은 **초기화 밖 이야기 절** 들이 이미 쌓여 있다. 이번 절은 그 맨 끝이다.

### 왜 이 질문이 지금 왔나

- PE_emulator 요청이다. 지금 에뮬레이터는 `socket` / `WSASocketW` / `WSASocketA` 를 **전부 −1** (실패) 로 돌려준다 (run 문서 "목적").
- 바라는 건 "**소켓은 만들어지고, 연결은 망 없는 PC 처럼 정직하게 실패**" 하는 것. 그러려면 무엇을 흉내 내야 하나를 **코드 줄로** 답해 달라는 요청이었다.
- 사용자 결정 2026-11-16: 분석 멈춤 중이지만 "**지금 즉시 먼저**".

### 무엇이 궁금했나

요청서 질문 셋 (run 문서 "목적").

1. **열기** — EA 모양, 열기 뒤 `0x1207b` · `0x12047` 의 칸, mswsock 이 대답에서 보는 것, 성공 최소 응답.
2. **연결** — `connect()` IOCTL 과 입력 모양, 길 없는 PC 의 status, status → WSA 오류 표, 블로킹 · 오버랩 완료 꼴.
3. (낮음) **nsi** IOCTL 셋의 입력 모양 · 길이 먼저 묻기 규칙.

### 바퀴 전에 알던 것

- `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]` (요청서):
  - `NtCreateFile("\Device\Afd\Endpoint", 접근 0xc0140000, …)` 뒤 같은 스레드의 다음 syscall 이 `NtDeviceIoControlFile` `0x1207b` × 2 (501/535 샘플).
  - 셋째가 `0x12047` (449) 또는 `NtAllocateVirtualMemory` (52).
  - `0x1207b` 는 입력 = 출력 `0x10` 바이트 (요청한 쪽 셈).
- `[정적]` 가짜 핸들 (`-1`) 로 IOCTL 을 내면 `0xC0000024` — `IopXxxControlFile.asm +0x1e0 ~ +0x231` (요청서 "이미 아는 것").
- `[정적]` (run121) TEB `WinSockData` 를 쓰는 줄은 mswsock 에 있다 (`SockWaitForSingleObject` · `AllocateWinsockThreadData`).
- `[정적]` (run83) 콘솔 연결도 `NtCreateFile` + EA + `NtDeviceIoControlFile` 꼴이었다. **같은 꼴, 다른 상담실** 이다.
- 이 레포에 afd · mswsock · nsi 분석은 **0** 이었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 두 줄기

run 문서 "호출 관계" 를 줄였다. 모두 `[정적]`. 트리 숫자는 ret 자리.

```
(새 TCP 소켓: socket() → 블로킹 connect())

mswsock!WSPSocket                                         ── 사서함 열기
 ├─ (1) +0x22b  SockSocket
 │        ├─ +0x178  SockGetTdiName                         전송 이름 비어 있음 (TL 모드)
 │        │  +0x3bb ~ +0x41a · +0x489                       EA 0x39 바이트 짓기
 │        ├─ +0x598  NtCreateFile(\Device\Afd\Endpoint)     ⇒ afd!AfdDispatch +0x120 AfdCreate
 │        ├─ +0x7d7  SockGetInformation(7) ─ +0xc2  0x1207b   (첫 소켓만, 반환 버림)
 │        └─ +0x7fa  SockGetInformation(6) ─ +0xc2  0x1207b
 ├─ (2) +0x24d  SockSetHandleContext ─ +0x277  0x12047     (status 부호만)
 └─ (3) +0x27a  [간접 +0x30] WPUCreateSocketHandle [추측]

mswsock!WSPConnect ─ +0x5d SockDoConnect                  ── 줄 대기 (실행 순서)
 ├─ (1) +0x27c  [간접 +0x88] helper 와일드카드 주소
 ├─ (2) +0x4dc  WSPSetSockOpt(0xffff, 0x3007) ─ +0x204  0x120bf
 ├─ (3) +0x2a7  WSPBind ─ +0x3f9  0x12003 · +0x4e9 SockSetHandleContext 0x12047
 └─ (4) +0x3fc  SockDoConnectReal
          ├─ +0x46a  0x12007   ⇒ afd!AfdConnect +0xbb4 [간접 TL +0x28]
          │                      → +0xc14 AfdTLConnectComplete (+0xe4 status 그대로)
          ├─ +0x4a3  SockWaitForSingleObject     (PENDING 일 때만)
          └─ +0x614  NtStatusToSocketError       (음수 → 10051 / 10065 / 10060 …)
```

- 위 줄기 = **사서함 열기**. 유저 쪽 mswsock 이 syscall 셋 (`NtCreateFile` 하나 + IOCTL 둘) 을 낸다. 둘째 `0x1207b` 는 첫 소켓에서만이니, 다음 소켓부터는 `NtCreateFile` + `0x12047` 둘이다.
- 아래 줄기 = **배송 줄 대기 (connect)**. IOCTL 넷. 마지막 `0x12007` 만 afd 너머 tcpip 로 간다.
- `⇒` 는 syscall 너머 커널 드라이버다. 그 사이 (`IopXxxControlFile` → `IofCallDriver`) 는 안 그렸다 (run 문서).

#### 새 말 — 소켓

- 프로그램이 망 너머와 주고받을 때 쓰는 **통로 하나** 다. 프로그램 손에는 정수 하나 (SOCKET 값) 가 쥐어지고, 그 뒤에는 커널의 afd 상담실과 이어진 손잡이가 있다 `[추측]` (일반적으로 알려진 설명).
- 이번 코드에서 확인한 것: 그 뒤의 손잡이는 **`\Device\Afd\Endpoint` 를 연 파일 손잡이** 다 `[정적]`. mswsock 은 그 손잡이를 소켓 구조체 `[sock+8]` 에 둔다 (`SockSocket +0x59d ~ +0x5c6`).
- 비유: 우체국에 연 **사서함 하나**.

#### 새 말 — Winsock 공급자 (`WSP*`)

- 프로그램이 부르는 `socket()` · `connect()` 는 ws2_32 에 있다. ws2_32 는 그걸 **공급자** (provider) DLL 의 `WSPSocket` · `WSPConnect` 로 넘긴다고 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이번 공급자가 mswsock 이다.
- 이번엔 **ws2_32 를 안 봤다.** 그래서 "ws2_32 → mswsock 다리" 는 `[공백]` 이고, 이 글은 `WSPSocket` · `WSPConnect` 부터 시작한다 (→ Q225 ①).
- 비유: 가게 계산대 (ws2_32) 가 손님 부탁을 받아 **우편 담당 직원** (mswsock) 에게 넘긴다.

#### 새 말 — afd

- `System32\drivers\afd.sys`. 소켓 주문서를 받는 커널 드라이버다 `[정적]` (이번에 처음 analyze, 함수 1280).
- 이름 뜻 (Ancillary Function Driver) 은 일반적으로 알려진 설명 `[추측]`. 이번 근거엔 이름 풀이가 없다.
- 비유: 관리사무소 건물 안 **우체국 창구** 상담실.

### 2.2 한 줄씩 읽는 법

run 문서 "정적 — 호출 지점" 표에서 두 줄을 골랐다.

**줄 ①** `| mswsock!SockSocket | +0x591 | +0x598 | NtCreateFile (\Device\Afd\Endpoint) | [정적] |`

- **누가**: mswsock 의 `SockSocket`. `WSPSocket +0x226` 이 부른 "소켓 진짜 만들기" 함수다.
- **어디서**: 함수 시작에서 `+0x591` 에 call 이 있다. 돌아올 자리는 `+0x598`. 이 call 은 7바이트 (`48 ff 15 …`) 라서 ret 가 `+0x598` 이다 — 결과서가 원본 바이트로 맞춰 봤다.
- **누구를**: `NtCreateFile` syscall.
- **무엇을 위해**: 이름 `\Device\Afd\Endpoint` 를 EA 신청서와 함께 열어 **사서함 손잡이** 를 받으려고.
- 디버거로 보면 콜스택에 `mswsock!SockSocket+0x598` 이 찍힐 자리다 (아직 콜스택으로 본 적은 없다 — 정적만).

**줄 ②** `| afd!AfdConnect | +0xbaf | +0xbb4 | 간접 [RAX+0x28] (TL 공급자 connect — tcpip 쪽 [추측]) | [정적] |`

- **누가**: 커널 afd 의 `AfdConnect`. `0x12007` 주문서를 처리하는 함수다.
- **어디서**: `+0xbaf` 에서 call, ret `+0xbb4`.
- **누구를**: 이름이 아니라 **표의 칸** 을 부른다. `[RAX+0x28]` = 어떤 함수 표의 `+0x28` 칸 (간접 호출). 표 이름은 `AfdTlClientConnectDispatch` (`+0xb59` 에서 고름) 다.
- **무엇을 위해**: "이 주소로 줄 대 주세요" 를 아래 계층 (TL 공급자) 에 맡기려고. 받은 status 는 `+0xbb4 MOV R14D,EAX` 로 쥔다.
- 이 칸 너머가 tcpip 라는 건 `[추측]` 이다 (tcpip 는 이번에 안 봤다).

### 2.3 열기 — `NtCreateFile` 인자 `[정적]`

`SockSocket +0x4a8 ~ +0x56e` 가 채운다 (run 문서 1절).

| 인자 | 값 | 줄 |
|---|---|---|
| 이름 | `\Device\Afd\Endpoint` (`.rdata 0x561d0`) | flow-writer 가 `pefile` 로 다시 읽음 |
| 속성 | `0x42` (INHERIT \| CASE_INSENSITIVE). `dwFlags & 0x180` 이면 `0x40` | |
| 접근 | `0xc0140000` (플래그 `0x40` 이면 `0xc1140000`) | `+0x503` · `+0xb53` |
| AllocationSize · 파일 속성 | 0 · 0 | |
| 공유 | 3 | |
| `CreateDisposition` | 3 (`FILE_OPEN_IF`) | |
| **CreateOptions** | **`dwFlags & 0x101` 이면 0 (비동기), 아니면 `0x20`** | `+0x4f2 TEST R14D,0x101` · `+0x4ff CMOVNZ` |
| EaBuffer · EaLength | 스택 `[RSP+0x140]` (0x60 바이트 넘으면 힙) · **`Length + 0x39`** | `+0x3bb` · `+0x3bf` |

#### 새 말 — 비동기 핸들

- CreateOptions `0x20` (`FILE_SYNCHRONOUS_IO_NONALERT`) 으로 열면 **동기** 손잡이, 0 이면 **비동기** 손잡이다 `[정적: 이 두 값으로 갈림]`.
- 일반적으로: 동기 손잡이는 I/O 를 내면 커널이 끝날 때까지 기다려 주고, 비동기 손잡이는 "접수했어요 (`0x103`)" 하고 먼저 돌아올 수 있다 `[추측]` (일반적으로 알려진 설명).
- 왜 중요한가: `socket()` 은 보통 겹침 (overlapped) 소켓이라 **CreateOptions 0 (비동기)** 로 열릴 것이다 `[추측: ws2_32 dwFlags 안 봄]`. 그래서 mswsock 은 IOCTL 마다 **신호등 (이벤트)** 을 들고 가서, `0x103` 이 오면 기다린다 (2.7 · 2.15).
- 비유: 동기 = 창구에서 "기다리세요" 하고 처리 끝날 때까지 서 있는 손님. 비동기 = 번호 받고 자리에 앉아 **신호등** 이 켜지길 보는 손님.

### 2.4 ★ EA 신청서 — 칸 표 `[정적]`

EA 는 `FILE_FULL_EA_INFORMATION` **한 칸** 이다. 오프셋은 EA 시작 기준 (run 문서 "EA 칸 표" 그대로).

| 오프셋 | 크기 | 뜻 | 값 | 근거 (`SockSocket`) |
|---|---|---|---|---|
| `+0x00` | 4 | NextEntryOffset | 0 | `+0x3d5` |
| `+0x04` | 1 | Flags | 0 | `+0x3d8` |
| `+0x05` | 1 | EaNameLength | **`0x0f`** | `+0x3d8` |
| `+0x06` | 2 | EaValueLength | **`Length` + `0x1e`** | `+0x3e9` |
| `+0x08` | 16 | EaName | **`AfdOpenPacketXX\0`** | `+0x3de` · `+0x3e5` |
| `+0x18` | 4 | EndpointFlags (비트 모음) | 0 에서 시작 | `+0x41a` |
| `+0x1c` | 4 | GroupID | `WSPSocket` 인자 `g` | `+0x489` |
| **`+0x20`** | 4 | **주소 계열** (예 AF_INET 2) | | `+0x402` · `+0x409` |
| **`+0x24`** | 4 | **소켓 종류** (예 SOCK_STREAM 1) | | `+0x40c` · `+0x410` |
| **`+0x28`** | 4 | **프로토콜** (예 TCP 6) | | `+0x413` · `+0x417` |
| `+0x2c` | 4 | 전송 이름 길이 (널 제외) | `Length` | `+0x3f5` |
| `+0x30` | `Length + 2` | 전송 이름 (WCHAR) | **이름 있을 때만** | `+0x791 ~ +0x7aa` |

- 앞 `+0x00 ~ +0x17` 은 "봉투 겉면" (EA 머리 + 이름표), `+0x18` 부터가 **신청서 본문** (EA 값) 이다.
- 본문에서 afd 가 꼭 알아야 할 셋이 **`+0x20` 계열 · `+0x24` 종류 · `+0x28` 프로토콜** 이다. 예를 들어 TCP/IPv4 소켓이면 2 · 1 · 6.
- 이름표 `AfdOpenPacketXX` 는 15글자 + 널 = 16바이트. `.rdata 0x56200` 에서 16바이트를 그대로 복사한다. 이것도 flow-writer 가 `pefile` 로 바이트를 다시 읽어 확인했다.

#### 새 말 — TL 모드 / TDI 모드 (전송 이름이 빈가 찬가)

- EA 맨 끝 "전송 이름" 칸 (`+0x30`) 은 **비어 있는 게 기본** 이다. run 문서는 이 상태를 "**TL 모드**" 라고 부른다. 이름 (`\Device\Tcp` 등) 이 들어 있으면 "TDI 모드" 다.
- 그래서 기본 TCP/UDP 는 `Length = 0` → **EaValueLength `0x1e`, EaLength `0x39`** `[정적]`. 요청서 로그의 `0x39` 와 맞는다.
- 왜 비어 있나, 단계별로 `[정적]`:
  1. 이름을 주는 쪽은 `Tcpip4_WSHOpenSocket2` (`+0x80 ~ +0xb2`) 다.
  2. 이 함수는 전역 `Tcpip4_TdiModeStream` 이 **켜졌을 때만** `\Device\Tcp` 를 주고, 아니면 NULL 을 준다.
  3. 그 전역은 `Tcpip4_WSHNotify` 가 알림 `0x80000000` 등을 받을 때만 1 이 된다.
  4. 그 알림을 부르는 곳은 `SockSocket` **실패 갈래** (`+0x916 ~ +0x966`, afd 가 `0x8000001c` 를 주는 TDI 필터 상황) 뿐이다.
  - 그래서 → 보통 PC 에선 이 갈래를 안 타니, 이름은 비어 있다 `[추측]`.
- 하나 더: 스택 버퍼를 안 지워서 `+0x30` 뒤는 **쓰레기** 다 (결과서). 에뮬레이터가 이 부분 값을 기대하면 안 된다는 뜻이다.
- 비유: 신청서 맨 아래 "**지정 배송 회사**" 칸. 보통은 비워 두고 낸다 (창구가 알아서 고름).

#### EndpointFlags (`+0x18`) — 보통은 0 `[정적]` + `[추측]`

- 비트 값 · 켜는 줄 · 조건은 `[정적]`, 비트 이름은 PDB 에 없다. 대표만:
  - `0x1` (`+0x749`): connectionless 이고 종류 2 · 3.
  - `0x10` (`+0x76d`): message oriented 이고 종류 2 ~ 5.
  - `0x100` (`+0x876`): SOCK_RAW.
  - 나머지 (`0x1000` · `0x10000` · `0x100000` 멀티포인트, `0x01000000` TDI 모드 · AppContainer 등, `0x10000000` 등록 I/O) 는 run 문서 1절.
- 조건에 나오는 `[sock+0x50]` = `WSAPROTOCOL_INFOW.dwServiceFlags1` 이다. `XP1_*` 이름 짝은 `[추측]`.
- → 보통 **TCP = 0**, **UDP = `0x11`** `[추측]`. 왜 추측이냐: 그 조건 비트를 정하는 provider 정보 표 (`SockTcpProviderInfo`) 의 `dwServiceFlags1` **값** 을 안 읽었다 (→ Q225 ⑦, Q224 ①).

### 2.5 afd 창구의 신청서 검사 — `AfdCreate` `[정적]`

`AfdDispatch +0x11b` 가 `AfdCreate` 를 부른다 (ret `+0x120`). 여기서 신청서를 본다 (결과서).

- **이름표**: `AfdOpenPacketXX` 인가 (`RtlEqualString` `+0xba`). 다른 이름 `AfdSwOpenPacket` · `AfdRioRDOpenPacket` 은 다른 길 (`+0x39a` · `+0x3fe`).
- **길이**: `EaValueLength ≥ 0x1c`, `전송이름길이 + 2` 짝수, `0x18 + (길이 + 2) ≤ EaValueLength`.
- **비트**: EndpointFlags 는 16진 자리마다 **맨 아래 비트만** 허용 (`& 0xeeeeeeee ≠ 0` → `0xC000000D`). `(flags & 0x11) == 0x10` 이어도 `0xC000000D`.
- **값 범위** (전송 이름 없을 때): 계열 `< 0x23` · `≠ −1`, 종류 · 프로토콜 `< 0x10000`.
- 통과하면 0 → `AfdDispatch` 가 그 값으로 IRP 를 끝낸다.

비유: 창구 직원이 봉투 이름표 → 칸 길이 → 체크 표시 모양 → 숫자 범위 순으로 본다. 하나라도 틀리면 "서식이 틀렸어요" (`0xC000000D`).

### 2.6 주문서가 afd 안에서 갈 곳을 찾는 법 `[정적]`

이건 2.7 · 2.8 을 읽기 전에 알아 두면 좋은 뒷길이다.

- `AfdDispatchDeviceControl` (rva `0x14cc0`) 가 코드에서 `(코드 >> 2) & 0x3ff` 를 꺼내 **칸 번호** 로 쓴다.
  - 그 번호가 `0x4a` 이상이거나, 표 (`AfdIoctlTable`) 의 코드와 다르거나, 칸이 NULL 이면 → **`0xC0000010`** (`+0x18d`).
  - 즉 "afd 가 모르는 주문서 번호" 는 이 status 로 돌아온다.
- 칸에서 처리 함수를 찾는다 (`AfdIrpCallDispatch`). `0x1207b` · `0x12047` 처럼 **바로 끝나는** 주문은 그 칸이 `AfdDispatchImmediateIrp` 이고, 거기서 표 (`AfdImmediateCallDispatch`) 를 **한 번 더** 본다 (`+0x8f` 간접 호출).
- 표 읽기 검증: 결과서가 `AfdIoctlTable` 74칸 전부에서 `(코드>>2)&0x3ff == 칸 번호` 가 맞는 걸 확인했다. 표를 제대로 읽었다는 증거다.

#### 새 말 — `METHOD_NEITHER`

- afd 주문서는 **전부** `METHOD_NEITHER` 다 `[정적]`. run 문서 말로: 입력 · 출력이 **사용자 주소 그대로** 드라이버에 간다.
- 다른 방식은 커널이 버퍼를 따로 복사해 건네준다고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유: 창구 직원이 신청서를 **복사본으로 받지 않고**, 손님이 들고 온 종이를 그대로 들여다보고 그 위에 답을 적는다.
- 왜 중요한가: 에뮬레이터 입장에선 "입력 · 출력 버퍼 = 가상 주소 공간의 그 자리" 를 그대로 읽고 쓰면 된다는 뜻이다.

### 2.7 열기 뒤 주문서 ① `0x1207b` — "창 크기가 얼마예요?" `[정적]`

보낸 곳: `mswsock!SockGetInformation +0xbb` (코드는 `+0xae` 에서 넣음). 처리: `afd!AfdGetInformation`.

#### 새 말 — 정보 클래스 · 창 크기

- **정보 클래스** = "무엇을 묻는지" 를 가리키는 번호. 이번엔 6 = **받기 창**, 7 = **보내기 창**, `0xa` = 그룹 `[정적]`.
- **창 크기** 가 무슨 뜻인지 (한 번에 쌓아 둘 수 있는 바이트 양 정도) 는 일반적으로 알려진 설명이다 `[추측]`. 이번 코드에서 확인한 건 "afd 전역 `AfdReceiveWindowSize` · `AfdSendWindowSize` 값을 돌려준다" 까지다.

**인자**:

- 핸들 = 방금 연 소켓 `[sock+8]`.
- **Event = 스레드별 이벤트** (`TlsGetValue` `+0x5d` 로 얻은 블록 `+0x10`). Apc 둘은 0.
- **입력 = 출력 같은 `0x10` 칸**, 길이 둘 다 `0x10`.

**칸**:

| 오프셋 | 크기 | 뜻 |
|---|---|---|
| `+0x00` | 4 | 정보 클래스 (mswsock 이 넣음, `+0x96`) |
| `+0x04` | 4 | 안 씀 (0) |
| `+0x08` | 4 (또는 8) | **값** — afd 가 씀. 클래스 6 · 7 은 dword (mswsock 은 `+0xe1` · `+0xe4` 에서 읽음) |
| `+0x0c` | 4 | 안 씀 |

- 비유: 질문지 한 장을 내밀면, 창구가 **같은 종이** 의 `+8` 칸에 답을 적어 돌려준다.

**언제 내나** — ★ 여기가 이번 바퀴의 작은 반전이다.

- `SockSocket +0x5e3` 이 `SockSendBufferWindow == 0` 일 때만 묻는다.
  - 그래서 → **프로세스 첫 소켓** 에서만 두 번 (7 → 6) 나간다.
  - 다만 값이 0 으로 남으면 (예: 실패) → 다음 소켓에서 **또 묻는다**.
- 클래스 `0xa` 는 그룹 `g ≠ 0` 일 때만 (`+0xbff`).
- 요청서 로그의 "`0x1207b` × 2 (501/535)" 와 맞는 그림이다. 다만 로그는 에뮬레이터 것이다.

**afd 쪽 대답**:

- 입력 · 출력 `< 0x10` → `0xC0000023`, 모르는 클래스 → `0xC000000D`.
- 클래스 6 → `AfdReceiveWindowSize`. 7 → 연결된 소켓이면 그 연결 값, 아니면 `AfdSendWindowSize`.
- 성공이면 `0x10` 복사 · `Information = 0x10` · status 0.
- **기본값**: `AfdInitializeData +0x18b ~ +0x19e` 가 제품 종류가 워크스테이션 (1) 이면 **`0x10000`**, 서버면 `0x20000`. 레지스트리로 덮을 수 있다 (`AfdReadRegistry +0x113 ~ +0x141`).

### 2.8 열기 뒤 주문서 ② `0x12047` — "정보 카드 맡겨 둘게요" `[정적]`

보낸 곳: `mswsock!SockSetHandleContext +0x270` (코드는 `+0x263`). 처리: `afd!AfdSetContext`.

- **부르는 곳이 여럿** 이다: `WSPSocket +0x248` (만들 때), `WSPBind +0x4e4`, `WSPSetSockOpt +0x290`, `SockPostProcessConnect` 등. 소켓 상태가 바뀔 때마다 카드를 새로 맡기는 셈이다.
- 인자: 핸들 `[sock+8]`, 같은 스레드 이벤트, 입력 = 스택 `[RSP+0x70]` (0x100 넘으면 힙).
  - **출력** = 소켓 상태가 0 (열림) 이면 **입력 안 "상대 주소" 칸**, 길이 `[sock+0x2c]`. 아니면 NULL / 0.

**입력 칸** (총 길이 = `roundup8(최대주소길이) × 2 + 0x80 + helper 문맥 길이`):

| 오프셋 | 크기 | 뜻 |
|---|---|---|
| `+0x00` | `0x78` | 소켓 공유 정보 (`[sock+0x18 .. +0x90)` 그대로 — 상태 · 계열 · 종류 · 프로토콜 · 주소 길이 · SO_* 값 등) |
| `+0x78` | 4 | helper 문맥 길이 |
| `+0x80` | `roundup8(최대주소길이)` | 내 주소 (`[sock+0xa8]`) |
| (다음) | 같음 | 상대 주소 (`[sock+0xb0]`) — 출력 포인터가 가리키는 곳 |
| (다음) | helper 문맥 길이 | helper 문맥 (helper `[RCX+0x70]` 가 채움) |

**afd 쪽 대답**:

- 입력 바이트 **전부** 를 엔드포인트 문맥으로 저장한다.
- 출력 포인터엔 **안 쓴다.** "입력 안 상대 주소 위치" 만 적어 둔다.
- 유저 호출이면 출력이 입력 안이어야 하고 크기 ≤ `0xffff` (아니면 `0xC000000D`).
- **status 0, Information 0.**
- 맡긴 카드는 나중에 `0x12043` (`AfdGetContext`) 이 되돌려준다 — 범위 밖.

**IOCTL 을 아예 안 내는 갈래**:

- helper 가 먼저 실패하면 IOCTL 없이 0 을 돌려준다 (`+0xad ~ +0xaf` → `+0x2cf`) `[정적]`.
- 에뮬레이터 로그에서 `0x12047` 대신 `NtAllocateVirtualMemory` 가 나온 52/501 소켓이 이 갈래일 수 있다 `[추측]`.

비유: 사서함을 연 뒤, 직원이 "이 사서함은 TCP 용, 내 주소는 이것, 상대 주소 칸은 아직 비어 있음" 이라 적은 **정보 카드** 를 창구에 맡긴다. 창구는 카드를 서랍에 넣고 "받았어요" 도장만 찍어 준다.

### 2.9 ★ mswsock 은 대답에서 무엇을 보나 `[정적]`

이게 에뮬레이터에 가장 중요한 표다. "무엇을 꼭 맞춰야 하고, 무엇은 대충이어도 되나" 를 정한다.

| 호출 | **보는 것** | 안 보는 것 | 줄 |
|---|---|---|---|
| `NtCreateFile` | **status 부호** + 핸들 | IoStatusBlock (예외 아래) | `SockSocket +0x59d ~ +0x5c6` |
| 〃 예외 1 | status `0xC000000E` · `0xC0000034` · `0xC000003A` → `SockDemandStartAfd` (`AFD` 서비스 `NtLoadDriver`) 뒤 **한 번 더 열기** | | `+0x9a1` · `+0x9f2` |
| 〃 예외 2 | status `0x8000001C` + `Information == 0xFFFFFFFF8000001C` → TDI 모드로 다시 | | `+0xb77 ~ +0xb8d` |
| `0x1207b` (클래스 7 · 6) | **반환값 안 씀.** PENDING 이면 기다림, 성공이면 출력 `+8` dword | 출력 `+0` · `+4` · `+0xc`, Information | `SockSocket +0x7d2 ~ +0x7fa` |
| `0x1207b` (클래스 `0xa`) | **반환값 씀** — 실패면 소켓 만들기 실패 | | `+0xbff ~ +0xc16` |
| `0x12047` | **status 부호** (`+0x2c2 TEST EBX,EBX`) | 출력 · Information | `SockSetHandleContext +0x277 ~ +0x2cd` |

읽는 법:

- **열기** 는 "도장이 성공인가 + 손잡이를 받았나" 만 본다.
- **`0x1207b`** 는 놀랍게도 **반환값을 버린다.** 실패해도 소켓은 만들어진다. 다만 값이 0 으로 남아 소켓마다 다시 묻는다 (2.7).
- **`0x12047`** 은 도장 부호만 본다. 음수면 WSA 번호로 바꿔 돌려주고, `WSPSocket` 은 정리 뒤 실패한다 (`+0x24d ~ +0x253`).

**실패하면 프로그램은 무엇을 받나**:

- `WSPSocket` 이 `*lpErrno` 에 WSA 번호를 넣고 (`+0x2b5`) **−1 (INVALID_SOCKET)** 을 돌려준다.
- 열기 실패 쪽 번역: `0xC0000034` · `0xC000000E` · `0xC000003A` → **10050** (WSAENETDOWN), `0xC0000022` → 10013, 표에 없는 음수 → 10022.
- 그 번호가 `WSAGetLastError` 에 남는지는 ws2_32 가 `*lpErrno` 를 옮기는 줄에 달렸다 → `[공백]` (ws2_32 안 봄, Q225 ①).

#### 새 말 — WSA 오류 번호

- 소켓 함수가 실패했을 때 프로그램이 `WSAGetLastError` 로 받는 **10000번대 번호** 다 (예: 10051 WSAENETUNREACH). 커널의 NTSTATUS 와는 다른 번호 체계다 `[추측]` (일반적으로 알려진 설명).
- 이번 코드에서 확인한 것: mswsock `NtStatusToSocketError` 가 NTSTATUS 를 이 번호로 바꾼다 `[정적]` (2.14).
- 비유: 관리사무소 도장 (NTSTATUS) 을 우편 담당 직원이 **손님 말로 옮긴 안내문** (WSA 번호).

### 2.10 ★ 소켓 만들기에 성공하려면 — 최소 응답 셋 `[정적]`

2.9 에서 바로 나온다 (run 문서 4절).

1. **`NtCreateFile` (`\Device\Afd\Endpoint`, EA `AfdOpenPacketXX`)** → status 0 + **0 아닌 새 핸들**. IoStatusBlock 은 안 읽힌다.
   - EA `+0x20` · `+0x24` · `+0x28` (계열 · 종류 · 프로토콜) 은 손잡이 뒤 파일 객체에 적어 둘 것. 뒤 IOCTL 들이 이 소켓 것이다.
2. **`0x1207b`** (첫 소켓에서 클래스 7 · 6) → status 0, 같은 `0x10` 칸 `+8` dword = `0x10000`, Information `0x10`. **바로** 끝낼 것 (PENDING 이면 mswsock 이 이벤트를 기다린다).
   - 실패해도 소켓은 만들어진다. 다만 값 0 이면 소켓마다 다시 묻는다.
3. **`0x12047`** → status 0, 출력 안 씀, Information 0. 입력 바이트는 소켓별로 저장.

- 그 뒤 프로그램 손에 쥐어지는 SOCKET 값은 ws2_32 쪽 `WPUCreateSocketHandle` (`WSPSocket +0x275` 간접) 이 만든다 — 유저 안에서 끝남 `[추측]`.
  - 왜 추측이냐: `SockUpcallTable +0x30` 칸을 부르는데, 인자 모양 (서명) 으로 보아 그 함수라고 짐작했을 뿐 ws2_32 를 안 봤다.

비유로: 창구에서 ① 사서함 열쇠를 받고 ② (처음 한 번) 창 크기 답을 받고 ③ 정보 카드에 도장을 받으면, 사서함은 **열린 것** 이다.

### 2.11 연결 (`connect()`) — 주문서 넷의 차례 `[정적]`

#### 앞 검사 — afd 에 가기도 전에 끊기는 것

`SockDoConnect` 가 먼저 본다 (값은 WSA 번호):

- 소켓 없음 10038, 이미 연결 중 10037, 연결됨 10056, 듣기 · 이상한 상태 10022, 주소 짧음 10014, 계열 다름 10047.

#### 새 말 — bind · 묵시적 bind

- **bind** = 소켓에 "**내 쪽 주소 (와 포트)**" 를 붙이는 일이다. `WSPBind` 가 하고, afd 주문서 `0x12003` (`AfdBind`) 으로 간다 `[정적]`.
- **묵시적 bind** = 프로그램이 bind 를 안 부르고 바로 `connect()` 를 부르면, mswsock 이 **알아서 먼저** bind 한다 `[정적]`:
  - 소켓 상태 0 (bind 안 함) 이면 → helper 의 "와일드카드 주소" 함수 (`+0x277` 간접) 로 주소를 만들고 → `WSPBind` (`+0x2a2`).
  - 와일드카드 주소가 "아무 내 주소나" 라는 뜻이라는 건 일반적 설명 `[추측]`.
- 비유: 편지를 부치려는데 사서함에 **보내는 사람 주소** 가 아직 없으면, 직원이 "아무 주소나 · 빈 번호 아무거나" 로 먼저 적어 넣는다.

#### 차례

TCP + AF_INET/AF_INET6 이면 bind 앞에 setsockopt 가 하나 더 있다 (`+0x4d7`).

| 순서 | 코드 | afd 처리 함수 | 내는 줄 (call) | 입력 모양 |
|---|---|---|---|---|
| 1 | `0x120bf` | `AfdTliIoControl` | `WSPSetSockOpt +0x1fd` | `0x20`: 레벨 `0xffff`, 옵션 `0x3007`, 값 포인터 · 길이 |
| 2 | `0x12003` | `AfdBind` | `WSPBind +0x3f2` | 입력 = 출력. `+0` 공유 방식, `+4` 날 sockaddr. 출력 길이 = **Information** |
| 3 | `0x12047` | `AfdSetContext` | `WSPBind +0x4e4` | 2.8 과 같음 |
| 4 | **`0x12007`** | **`AfdConnect`** | `SockDoConnectReal +0x463` | 2.12 |

- `0x3007` 은 `SO_REUSE_UNICASTPORT` 로 보인다 `[추측]`.
- **2번의 출력** 이 중요하다: afd 가 버퍼 `+0` 부터 sockaddr (임시 포트 포함) 를 써 주고, 길이는 **Information** 으로 알린다. mswsock 은 그걸 `[sock+0xa8]` 로 복사한다 (`+0x4bf`). 즉 이 주문서는 **전표 아래 칸까지 본다**.
- **주소 순서 ≠ 실행 순서**: 트리에서 setsockopt 는 `+0x4dc`, bind 는 `+0x2a7` 로 bind 가 앞 주소다. 그런데 갈래를 읽으면 setsockopt 가 먼저 실행된다 `[정적: 갈래 읽기]`. 실제 차례는 동적으로 확인할 것 (Q224 ④).
- 안 나가는 것들 `[추측]`:
  - `0x12037` · `0x1203b` (`SockUpdateWindowSizes`, `WSPBind +0x4f8`) — 앱이 SO_RCVBUF/SNDBUF 를 바꿨을 때만이라고 본다.
  - `0x1204b` · `0x1206b` (연결 데이터) — `WSAConnect` 에 연결 데이터가 있을 때만. `connect()` 는 NULL 이라 안 탄다.

#### 새 말 — sockaddr

- "주소 계열 · 포트 · 주소" 를 담은 **주소 쪽지** 다. 프로그램이 `connect(소켓, name, namelen)` 에 넘기는 `name` 이 이것이다 `[추측]` (일반적으로 알려진 설명 — 이번 코드에서 확인한 건 `name` 을 그대로 복사한다는 것).
- run 문서는 "**날** sockaddr" 라고 부른다. 다른 모양 (TA 꼴) 으로 싸지 않고 **받은 그대로** 넣는다는 뜻이다.

### 2.12 `0x12007` 입력 — `0x18` 머리 + 주소 쪽지 `[정적]`

TL 모드 (기본). 출력은 NULL · 길이 0. **입력 길이 = `0x18 + namelen`** (IPv4 면 `0x28`).

| 오프셋 | 크기 | 뜻 | 근거 (`SockDoConnectReal`) |
|---|---|---|---|
| `+0x00` | 1 | UseSAN = `SockSanEnabled ≠ 0` | `+0x1fc ~ +0x1ff` |
| `+0x01` | 7 | 안 씀 | |
| `+0x08` | 8 | 루트 엔드포인트 = 0 | `+0xf9 ~ +0xfc` |
| `+0x10` | 8 | 연결 엔드포인트 (이 길에선 afd 가 안 읽음 — 쓰레기일 수 있음 `[추측]`) | |
| **`+0x18`** | **namelen** | **날 sockaddr** — `connect()` 의 name 그대로 (helper 최대 길이로 자름) | `+0x104 ~ +0x10e` memcpy |

- TDI 모드일 때만 `+0x18` 이 TA 꼴 (`SockBuildTdiAddress`). 기본은 안 쓴다.
- **afd 검사** (`AfdConnect +0x16e ~ +0x2b1`, 64비트):
  - 입력 ≥ `0x18`. 출력은 0 또는 ≥ `0x10` (1 ~ 15 → `0xC000000D`).
  - TL 모드면 `sa_family < 0x23` 이고 `입력 − 0x18 ≥ SOCKADDR_SIZE(family)` (아니면 `0xC0000141`).
- 비유: 주문서 앞 `0x18` 바이트는 **접수 칸** (거의 0), 그 뒤에 손님이 준 **주소 쪽지** 를 그대로 붙인다.

### 2.13 ★ "길 없음" 은 afd 가 정하지 않는다 `[정적 + 추측]`

이번 바퀴의 가장 중요한 갈림길이다. 단계별로 따라가자.

#### 새 말 — TL 공급자

- afd 아래에서 **실제 전송** (TCP 등) 을 하는 쪽이다. afd 는 그쪽 함수 표 (`AfdTlClientConnectDispatch`) 를 들고 칸을 부른다 `[정적]`.
- 그 쪽이 tcpip.sys 라는 건 `[추측]` (이번에 안 봤다).
- 비유: 우체국 창구 뒤의 **배송 회사**.

#### 근거 → 그래서 → 아직 확인 아님

1. **근거** `[정적]` — `AfdConnect` (rva `0x3c1a0`) 는:
   - `+0xb59` 에서 TL 공급자 표 (`AfdTlClientConnectDispatch`) 를 고른다.
   - `+0xb01 ~ +0xb08` 에서 **완료 함수 `AfdTLConnectComplete`** 를 요청에 적어 넣는다 ("끝나면 이 번호로 연락 주세요").
   - **`+0xbaf` 에서 표 `+0x28` 칸을 간접 호출** 한다. 돌아온 값은 `+0xbb4 MOV R14D,EAX` 로 쥔다.
2. **근거** `[정적]` — 두 갈래:
   - PENDING 이 아니면 (`+0xbf0 CMP R14D,0x103`) → `+0xc0f` 에서 afd 가 **직접** `AfdTLConnectComplete(IRP, R14D)` 를 부른다.
   - PENDING 이면 → `+0xcab AfdTLPendRequest` 로 걸어 두고, 나중에 TL 이 `AfdTLConnectComplete` 를 부른다.
3. **근거** `[정적]` — 어느 갈래든 결국 **`AfdTLConnectComplete +0xe4 MOV [Irp+0x30],EBP`** 를 지난다. `EBP` = 받은 status, 이게 IRP 의 IoStatus.Status 가 된다. 실패 갈래도 `+0xe1` 로 합쳐져 같은 줄이다.
4. **그래서** → afd 는 "길 없음" 값을 **만들지 않고**, 배송 회사가 준 도장을 **그대로 봉투에 찍어** 돌려준다.
5. **아직 확인 아님** `[추측]` — 배송 회사 (tcpip) 가 실제로 주는 값. 후보:
   - 어댑터 없음 · 경로 없음 → `0xC000023C` (NETWORK_UNREACHABLE), **즉시**.
   - 같은 서브넷인데 상대 무응답 → 재전송 뒤 `0xC00000B5` (IO_TIMEOUT) 또는 `0xC000023D` (HOST_UNREACHABLE).
   - → 동적으로 닫는다 (Q224 ⑤, 가장 중요).

#### afd 스스로 정하는 status (경로와 무관) `[정적]`

- 형식 틀림 `0xC000000D`, 주소 길이 `0xC0000141`, 핸들 형식 `0xC0000008`, 메모리 `0xC000009A`.
- SAN `0xC00000FA` — 일반 PC 는 `AfdSanServiceHelper == NULL` 이라 안 탄다 `[추측]`.
- 이것들은 "**서식 · 창구 사정**" 실패다. "길 없음" 과는 다른 종류다.

### 2.14 도장 → 손님 말 — `NtStatusToSocketError` 표 `[정적]`

`mswsock!NtStatusToSocketError` (rva `0xbca0`, 645바이트).

- 점프 표 셋을 `.dll` 에서 읽어 풀었다 (결과서).
- 첫 줄 `+0x8 RtlNtStatusToDosError` 를 부르지만 **반환값을 안 쓴다.** (일반 Win32 번역을 쓰지 않고 자기 표로 바꾼다는 뜻.)

연결에 중요한 줄만 (NTSTATUS 이름은 `[추측]`):

| NTSTATUS | WSA |
|---|---|
| `0xC000023C` NETWORK_UNREACHABLE (· `0xC000023E` · `0xC00000BE`) | **10051** WSAENETUNREACH |
| `0xC000023D` HOST_UNREACHABLE | **10065** WSAEHOSTUNREACH |
| `0xC00000B5` IO_TIMEOUT (· `0xC000013F`) | **10060** WSAETIMEDOUT |
| `0xC0000236` CONNECTION_REFUSED (· `0xC00000BC`) | **10061** WSAECONNREFUSED |
| `0xC0000141` INVALID_ADDRESS | 10049 |
| `0xC000000D` INVALID_PARAMETER | 10022 (표에 없음) |

- 특수: `0` · 양수 → 0, `0x102` → 10060, **`0x103` (PENDING) → 997**, 표에 없는 음수 → **10022**. 나머지 줄은 run 문서 7절.
- 연결에서 부르는 자리: `SockDoConnectReal +0x60f` (ret `+0x614`), status 가 음수이고 `0xC00000FA` 가 아닐 때. 성공이면 `+0x61b SockPostProcessConnect`.
- 바꾼 번호는 `SockDoConnect → WSPConnect +0x6b` 가 `*lpErrno` 로 내보내고 `−1` 을 돌려준다 (`+0x6d`).

이어 읽기: 에뮬레이터가 `0x12007` 에 `0xC000023C` 를 돌려주면 → 이 표로 10051 → `connect()` 는 −1. "망 없는 PC 처럼 정직한 실패" 의 모양이다 (값 자체는 `[추측]`, 2.13).

### 2.15 완료가 오는 꼴 — 블로킹 · 비블로킹 · `ConnectEx` `[정적]`

#### 새 말 — 블로킹 / 비블로킹

- **블로킹** = 결과가 나올 때까지 함수가 안 돌아온다. **비블로킹** = 바로 "아직이에요" 하고 돌아온다 `[추측]` (일반적으로 알려진 설명).
- mswsock 안에서 둘을 가르는 건 소켓 구조체 `[sock+0x44] & 0x40` 비트다 `[정적]`.

**블로킹 `connect()`** (비트 꺼짐, 또는 비연결형):

1. `+0x420` IoStatusBlock 을 `0x103` 으로 미리 채움 → `+0x463 NtDeviceIoControlFile` (스레드 이벤트, 출력 없음).
2. `+0x471 CMP EAX,0x103`: **PENDING 아니면 반환 status 를 그대로** 쓴다 (`+0x46f MOV ESI,EAX`). 전표 (IoStatusBlock) 는 **안 읽는다**.
3. PENDING 이면 임계 구역을 놓고 `+0x49e SockWaitForSingleObject` → 돌아와 **`+0x4a3 MOV ESI,[RSP+0x98]`** (전표의 Status) 를 쓴다.
4. `SockWaitForSingleObject` 는 먼저 짧게 (`NtWaitForSingleObject(이벤트, TRUE, −10ms)`, call `+0x1c6`), 못 받으면 블로킹 후크 (`+0xc1`) 뒤 소켓 시간 제한으로 길게 (`+0x153`) 기다린다.

- 그래서 → 에뮬레이터가 **즉시 실패** 를 주면 2번에서 끝난다. 신호등 · 전표를 안 건드려도 된다 (run 문서 "PE_emulator 쪽").

**비블로킹** (비트 켜짐, 연결형):

- `\Device\Afd\AsyncConnectHlp` 핸들에 같은 `0x12007` 을 낸다 (`+0x34c`).
- status ≥ 0 이면 전표를 안 읽고 **10035 (WSAEWOULDBLOCK)** 를 돌려준다. 완료는 mswsock 비동기 스레드 몫이다.

**오버랩 `ConnectEx`** (`MSAFD_ConnectEx +0x6b SockDoConnectEx`):

- 코드가 다르다: **`0x120c7`** (`AfdSuperConnect`).
- IoStatusBlock = 호출자 `OVERLAPPED` 자체, Event = `hEvent`.
- ApcContext = `hEvent` 맨 아래 비트가 0 이면 `OVERLAPPED*` (완료 포트, 즉 **우편함** 으로 감), 1 이면 0.
- 반환 status 를 그대로 → PENDING 이면 `MSAFD_ConnectEx +0xe8` 에서 997. mswsock 은 **안 기다린다**.

비유로 셋을 나란히:

- 블로킹 = 창구 앞에 서서 결과를 기다리는 손님.
- 비블로킹 = "나중에 다시 올게요" (10035) 하고 바로 나가는 손님.
- `ConnectEx` = 결과 쪽지를 **우편함** 으로 보내 달라고 하고 가는 손님.

### 2.16 nsi — "어댑터 뭐 있어요?" (급함 낮음) `[정적]`

#### 새 말 — nsi

- `nsi.dll` (유저) → `\\.\Nsi` → `nsiproxy.sys` (커널) → 커널 NSI 모듈 로 이어지는 길이다 `[정적]`.
- 쓰임은 "망 어댑터 · 주소 같은 정보를 묻는 것" 이라고 알려져 있다 `[추측]` (일반적으로 알려진 설명, 요청서 제목 "어댑터 묻기").
- 비유: 관리사무소의 **망 안내 데스크**. 소켓 창구와는 다른 상담실이다.

**여는 법**: `NsiOpenDevice +0x3f CreateFileW(L"\\.\Nsi", 접근 0, 공유 3, OPEN_EXISTING, FILE_FLAG_OVERLAPPED)`.

- 전역 `g_NsiAsyncDeviceHandle` 이 `−1` 일 때만 열고, `CMPXCHG` 로 저장한다 (프로세스에 하나). EA 없음.
- NT 이름 `\??\Nsi` 로 바뀌는 줄은 안 봤다 `[추측: kernelbase CreateFile 변환]`.

**주문서 셋** (모두 입력 = 출력 같은 구조체):

| 코드 | 쓰는 함수 | 입력 |
|---|---|---|
| `0x120007` | `NsiGetParameter(Ex)` | `0x50` |
| `0x12000f` | `NsiGetAllParameters(Ex)` | `0x68` |
| `0x12001b` | `NsiEnumerateObjectsAllParameters(Ex)` · **`NsiAllocateAndGetTable`** | `0x70` |

**길이 먼저 묻기** (`NsiAllocateAndGetTable`):

1. 포인터를 모두 0 으로 두고 첫 `0x12001b` → 구조체 `+0x68` 에 **개수** 가 온다.
2. 개수에 개수/8 (최소 10) 여유를 더해 `g_NsiHeap` 에 칸별 배열을 잡는다.
3. 둘째 `0x12001b` 로 진짜 내용을 받는다. 전체 최대 3번 (`+0x48 CMP ECX,3`).

- 비유: "몇 명이에요?" 먼저 묻고, 인원수보다 조금 넉넉하게 의자를 놓은 뒤, "이제 앉으세요".
- 루프백만 있는 PC 의 대답 (개수가 0 인가) 은 공급자 (tcpip 등) 가 정한다 `[추측]` → Q224 ⑦.
- nsiproxy `NsippDispatchDeviceControl` (rva `0x2b40`): 세 코드를 각 처리 함수로, 없으면 `0xC0000002`. `0x12001b` 입력 `< 0x70` → `0xC000000D`. 모두 `METHOD_NEITHER`.

### 2.17 flow-writer 대조 — 결과서와 다르게 적은 곳 (뜻은 같음)

run 문서를 따른다.

1. 결과서 "`SockDoConnectReal +0x60d/+0x614` `NtStatusToSocketError`" → call 자리는 **`+0x60f`** (`+0x60d` 는 `MOV ECX,ESI` 인자 넣기). `SockPostProcessConnect` 도 call **`+0x61b`** (`+0x618` 은 `MOV RCX,RBX`). run 문서는 `_calls.tsv` 값을 썼다. 이 글도 그 값이다.
2. 결과서 "각 `Nsi…` 함수는 같은 틀: `NsiOpenDevice` → …" → **`NsiGetAllParametersEx` 는 `NsiOpenDevice` 를 안 부르고** 열기를 제 몸 안에 펼쳐 둔다 (`+0x6` · `+0xfd CreateFileW` · `+0x119 CMPXCHG`). 결과는 같다 (같은 전역 핸들).

- 이전 run 정정은 없다. run121 의 "`WinSockData` 를 쓰는 줄은 mswsock (`SockWaitForSingleObject` · `AllocateWinsockThreadData`)" 과 부딪히지 않는다 — 이번엔 `SockWaitForSingleObject` 의 기다림 쪽 줄만 봤다.

### 2.18 정리 — 비유로 한 번 더

- 손님 (프로그램) 이 계산대 (ws2_32) 에 "사서함 하나 열어 줘요" → 우편 담당 직원 (mswsock) 이 받는다.
- 직원은 관리사무소 **우체국 창구** (afd) 에 **신청서 봉투** (EA, 이름표 `AfdOpenPacketXX`, 본문에 계열 · 종류 · 프로토콜) 를 낸다 → 창구가 서식을 보고 **사서함 열쇠** (손잡이) 를 준다.
- 가게에서 처음 여는 사서함이면 "창 크기 얼마예요?" 를 두 번 묻는다. 답을 못 들어도 직원은 그냥 넘어간다.
- **정보 카드** 를 창구에 맡기고 "받았어요" 도장을 확인한다. 여기까지가 `socket()`.
- `connect()` 때는 옵션 하나 → 보내는 사람 주소 붙이기 (묵시적 bind) → 카드 다시 맡기기 → **"이 주소로 줄 대 주세요"** (`0x12007`).
- 마지막 주문서만 창구 뒤 **배송 회사** (tcpip) 로 간다. "길이 없어요" 는 배송 회사가 말하고, 창구는 그 도장을 그대로 찍어 돌려준다.
- 직원은 도장을 **손님 말** (WSA 번호, 예: 10051) 로 옮겨 적고, 손님에게 −1 을 준다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 | mswsock · afd · nsi · nsiproxy 없음 | **PDB 넷 한 줄** (GUID+age · 함수 수 781 · 1280 · 42 · 77, 공개 심볼이라 타입 · IOCTL 이름 없음) |
| 기준 — 근거 줄 | — | **(run124) 정적 바퀴, 사진 값 대조 없음** (test.exe 가 mswsock · ws2_32 안 올림 `[관찰]` · 소켓 API 없음 `[소스]`) |
| 1장 그림 | — | **안 바뀜** (범위 밖) |
| 3장 끝 | bcrypt · RPC · `\SystemRoot` 절까지 | **새 절 "소켓 장치 AFD"** `[정적]` — 두 줄기 트리 + EA · 보는 것 · status 넘김 · `METHOD_NEITHER` · nsi 요약, 칸 표는 run 문서로 링크 |
| 4장 공백 목록 | — | **"3 (범위 밖 — 소켓)" 세 줄**: 동적으로 볼 것 ① ~ ⑦ · 정적으로 남은 것 ① ~ ⑦ · PE_emulator 쪽 |
| 5장 근거 run | — | **run124 줄** 둘 (근거 run 표 · 03_sprint 요청서 번호 표) |

- 태그 변화: 원래 이 부분에 대한 기록이 **없었다**. 이번에 처음 `[정적]` 으로 생겼다. `[추측]` 이 `[정적]` 으로 올라간 칸은 없다.
- 짐 지도 (`baggage.md`) 는 안 바뀌었다 — 초기화 동안의 짐이 아니다.

### PE_emulator 쪽 — 무엇을 고치면 되나 (우리 대기열 아님)

run 문서 "PE_emulator 쪽" (결과서 "지금 우리 맞나"). `../PE_emulator` 코드는 flow-writer 가 직접 안 읽었다. 고칠지는 **사용자 결정** 이다.

- **열기: 지금 막혀 있다.** 결과서에 따르면 `nt_create_file.cpp:58` 이 EA 가 있으면 `extended_attributes_unsupported` 로 멈춘다. 이 장치는 EA 가 **필수**. 장치 종류에 afd, EA 이름 확인, `+0x20/+0x24/+0x28` 저장이 필요하다.
- **비동기 핸들**: `socket()` 은 보통 CreateOptions 0 `[추측: ws2_32 dwFlags 안 봄]` → `nt_device_io_control_file.cpp:226` `device_async_handle_unsupported` 에 걸린다.
- 열기 · `0x12047` 은 status 만 보니 **성공 도장만 맞으면** 된다. `0x1207b` 는 `+8` 만. 지금은 afd 코드 몸이 전혀 없다 (`unknown_ioctl` 정책).
- **연결**: `0x120bf` · `0x12003` · `0x12047` · `0x12007` 넷이 필요하다. 정직한 실패는 `0x12007` 에 `0xC000023C` (또는 `0xC000023D`) 를 **즉시 (PENDING 없이)** — 블로킹 길이 `+0x46f` 에서 끝나 신호등 · 전표를 안 쓴다. 값 자체는 `[추측]` (Q224 ⑤).
- **이벤트**: 진짜 커널은 비동기 핸들 IOCTL 에서 호출 전 이벤트를 비우고 (`IopXxxControlFile +0x2db/+0x2e0 KeClearEvent`) 끝나면 세운다 (`+0xbe4/+0xbe9 KeSetEvent`). 어느 갈래에서 세우는지는 안 읽었다 `[추측]` (Q225 ⑥ · Q224 ⑥).
- **nsi**: 지금 `\Device\Nsi` 를 모른다. 필요하면 `0x12001b` 첫 호출에 "개수 `+0x68` = 0, status 0" 이 가장 단순 — 단 개수 0 이어도 최소 10칸 잡고 둘째 호출을 내는 것으로 읽힌다 `[추측]`.

### 큰 그림에서 어떻게 이어지나

- 흐름도 본선 (프로세스 생성 → `NtContinue` 은퇴 → `main`) 에는 **변화가 없다**. 이번은 `main` 뒤 옆길의 지도다.
- 그래도 이 줄기가 이어지는 곳이 셋 있다 (모두 이미 대기열에 있음, plan.md):
  - **`0x120b3` 주소 목록 묻기** (`AfdAddressListQuery`) — sast/123 · 124 → run126 예정 (Q219). 이번 2.6 의 "주문서가 afd 안에서 갈 곳 찾기" 위에서 읽으면 된다.
  - **nsi 를 부르는 쪽** (winhttp · IPHLPAPI · dnsapi …) — sast/126 → run128 예정 (Q221). 이번 2.16 의 `0x70` 구조체를 누가 채우나.
  - **완료 포트 묶기** (`NtSetInformationFile` 종류 `0x1e` · `0x29`) — sast/125 → run127 예정 (Q220). 이번 2.15 의 `ConnectEx` "우편함으로 감" 다음 이야기.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q224** (동적)

- **왜 이번에 못 알았나**: 정적만 했다. 그리고 test.exe 는 소켓을 안 쓰니 **소켓을 쓰는 프로그램 + 선 뽑은 VM** 이 필요하다. 어떤 프로그램인지는 **사용자 결정** 이다.
- 무엇을 보나 (run 문서 "아직 확인 필요" 동적 1 ~ 7):
  1. `NtCreateFile` EA `0x39` 바이트 — EaValueLength `0x1e`, EndpointFlags TCP 0 / UDP `0x11` (`[추측]` 닫기), `+0x2c` 0, CreateOptions 0.
  2. `0x1207b` 출력 `+8` 이 `0x10000` 인지, 첫 소켓에서만 나오는지.
  3. `0x12047` 입력 총 길이 · helper 문맥 길이 (`+0x78`) · 출력 포인터 오프셋.
  4. connect 앞 차례가 실제로 `0x120bf` → `0x12003` → `0x12047` → `0x12007` 인지 (2.11 "실행 순서" 확인), `0x12003` 입력 `+0` 값 · 출력 임시 포트 · Information.
  5. ★ **`0x12007` status** — 어댑터 없음 / 선 뽑힘 두 경우: 즉시인가 PENDING 뒤인가, `0xC000023C` / `0xC000023D` / `0xC00000B5` 중 무엇, 걸린 시간, `WSAGetLastError`. `0xC00000FA` 가 안 나오는지. (2.13 의 `[추측]` 을 닫는 핵심.)
  6. 이벤트가 IOCTL 직후 신호 상태인지 (동기 완료인 `0x1207b` · `0x12047` 도).
  7. nsi `0x12001b` 첫 호출 입출력 `0x70` 과 `+0x68`, 루프백만 있는 PC 에서 0 인지. (Q222 와 같은 세션에 얹을 수 있다 — plan.md Q222.)
- 멈춤점 추천 (`caller+ret`): `mswsock!SockSocket+0x598` · `SockGetInformation+0xc2` · `SockSetHandleContext+0x277` · `WSPBind+0x3f9` · `SockDoConnectReal+0x46a` · `+0x4a3` · `+0x614`, `afd!AfdConnect+0xbb4` · `AfdTLConnectComplete` 입구 (`edx`) · `AfdCreate` 입구.

### 정적으로 남은 것 → **Q225**

| # | 무엇 | 왜 이번에 못 했나 |
|---|---|---|
| ① | **ws2_32 다리** — `socket` · `WSASocketW` · `connect` 가 공급자로 넘기는 `dwFlags` · `lpProtocolInfo`, `*lpErrno` → `SetLastError`, `WPUCreateSocketHandle` | ws2_32 를 안 봤다. 그래서 "CreateOptions 0 (비동기)" 와 "WSAGetLastError 에 남는가" 가 `[추측]` / `[공백]` |
| ② | **afd 칸 마저** — `AfdBind` (TL 출력 쓰기) · `AfdTliIoControl` · `AfdSetInformation` · `AfdQueryHandles` · `AfdSuperConnect` · `AfdGetAddress` / `AfdGetRemoteAddress` | 이번 질문은 열기 · 연결 입구까지 (`0x120b3` 은 sast/123 이 따로) |
| ③ | **mswsock 시작 쪽** — `WSPStartup` · `SockEnterApiSlow` · `SockLoadTransportList` (`NtCreateFile` 앞 레지스트리 · 이벤트 syscall) | 범위가 "소켓 만들기부터" 였다 |
| ④ | **tcpip.sys** TL connect 진입점 (`AfdTlClientConnectDispatch +0x28` 칸 실제 함수) 의 "경로 없음" 줄 | 요청서가 tcpip 는 이름만으로 정했다. Q224 ⑤ 로 먼저 닫고 필요할 때 |
| ⑤ | **nsi** `0x70` 구조체 칸 뜻, 커널 `nsi.sys` 개수 0 대답 | 칸을 채우는 호출자 (iphlpapi 등) 를 안 봤다 (sast/126 이 일부 — run128 예정) |
| ⑥ | **`IopXxxControlFile` 이벤트 규칙** — 비동기 핸들 + 이벤트의 동기 완료에서도 세우나 (`+0xbe4` 갈래) | ntoskrnl 은 이번에 읽기만 했다 (sast/122 와 동시 진행) |
| ⑦ | (낮음) `SockTcpProviderInfo.dwServiceFlags1` 값 — EndpointFlags TCP 0 · UDP `0x11` `[추측]` 닫기 | provider 정보 표의 값을 안 읽었다 |

### 대기열 밖 — 이름 · 짐작으로 남은 것

- `WPUCreateSocketHandle` (`WSPSocket +0x275` 간접) — 서명으로 짐작 `[추측]` (Q225 ① 에서 같이 닫힘).
- `0x3007` = `SO_REUSE_UNICASTPORT` `[추측]`.
- 연결 데이터 함수 이름 후보 (`SockSetConnectData` · `SockGetConnectData` — 같은 주소에 이름 2개).
- NTSTATUS 이름들 (NETWORK_UNREACHABLE 등) 과 `XP1_*` 이름 — 값이 문서 상수와 맞아 붙인 것 `[추측]`.
- nsi `0x70` 의 `+0x28 ~ +0x60` 이 (버퍼 포인터, 칸 크기) 네 쌍처럼 보임 `[추측]` (Q225 ⑤).

---

## 5. 더 읽기

- run 문서: [`run124-afd-socket.md`](run124-afd-socket.md)
  - EA 칸 표 · `0x12047` 입력 칸 표 · `NtStatusToSocketError` 전체 줄 · afd 처리 함수 목록 → 그 문서 1 ~ 9절
- 결과서: [`_workspace/sast/121_afd-socket_result.md`](../../_workspace/sast/121_afd-socket_result.md) — 74칸 전체 표 "참고: afd 제어 코드 → 처리 함수", "동적으로 확인해야 할 것", "다음 요청 후보"
- 요청서: [`_workspace/sast/121_afd-socket.md`](../../_workspace/sast/121_afd-socket.md)
- flow.md: [`flow.md`](flow.md) — 3장 끝 "소켓 장치 AFD" 절 · [4장 공백 목록](flow.md#4-공백-목록-다음에-채울-곳) "3 (범위 밖 — 소켓)" 세 줄 · [5장 근거 run](flow.md#5-근거-run) run124 줄
- 같은 꼴 앞 바퀴: [run83 해설](run83-console-kernelbase-해설.md) (콘솔 — `NtCreateFile` + EA + IOCTL), [run91 해설](run91-bcrypt-init-해설.md) · [run92 해설](run92-bcrypt-ksecdd-해설.md) (DLL 이 장치를 여는 PE_emulator 요청), [run121 해설 2.4](run121-teb-other-dlls-해설.md) (`WinSockData` 주인 mswsock)
- 대기열: `_workspace/plan.md` Q217 (끝) · Q224 (동적) · Q225 (정적)

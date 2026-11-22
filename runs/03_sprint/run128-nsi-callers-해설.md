# run128 nsi-callers — 해설

> 이 글은 [run128 문서](run128-nsi-callers.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q221** — PE_emulator 요청 "NSI 부르는 쪽" (튜닝 `290_tune09.md N1`): `WinHttpSendRequest` 등 API 다섯에서 nsi 까지의 사슬 · 인자 · `0x68` / `0x50` / `0x70` 칸 표, 부른 쪽이 실패 · 빈 표를 읽는 법, `WinHttpSendRequest` 최소 대답 (`_workspace/plan.md`, 이번에 **끝 (run128, 일부 `[공백]` → Q222 · Q223)**)
  - 사용자 결정 2026-11-21 "이것들부터 먼저", 2026-11-22 "문서로 만들어서 커밋".
  - 같이 닫힌 것: run124 남은 정적 **Q225 ⑤ 일부** (nsi `0x70` 칸 뜻 · 부르는 쪽).
  - 이번에 남은 것이 간 곳 (4절):
    - 동적 → **Q222** (요청 처음부터 있던 "VM 실제 바이트" 몫 + 이번 보탬 8)
    - winnsi RPC 정적 → **Q223** (sast/127 결과서 있음) → **run129 예정**
    - 나머지 정적 → 새 **Q237**
- 근거:
  - run 문서 [`run128-nsi-callers.md`](run128-nsi-callers.md) (가장 중요)
  - 요청서 [`_workspace/sast/126_nsi-callers.md`](../../_workspace/sast/126_nsi-callers.md) · 결과서 [`126_nsi-callers_result.md`](../../_workspace/sast/126_nsi-callers_result.md)
    - 판정 **분석성공**, 스크립트 문제 없음.
    - winhttp · webio · IPHLPAPI · ws2_32 · dnsapi 다섯을 **처음 analyze** 했다 (함수 3305 · 2103 · 642 · 1194 · 3405). nsi 는 run124 때 것을 읽기만.
  - flow.md 변경 (docs 커밋 `0f65887` 의 `runs/03_sprint/flow.md`): 기준 (PDB 다섯 · 근거 줄) · 3장 run124 절 nsi 줄 · **3장 끝 새 절** · 4장 · 5장. **그림은 안 바뀜.**
  - 짐 지도 `baggage.md` 는 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 모듈들을 안 올린다.**
   - 초기화 끝 (P2) 모듈 6개에 winhttp · webio · IPHLPAPI · dnsapi · ws2_32 · nsi 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run124 와 같은 줄).
   - `sample/*.c` 에 HTTP · 소켓 · 어댑터 API 가 없다 `[소스]`.
   - 그래서 이번 이야기도 "**웹을 쓰는 다른 프로그램이라면**" 이다. 그림 · 짐 지도는 그대로다.
2. **"에뮬레이터 로그" 는 진짜 Windows 로그가 아니다.**
   - "`WinHttpSendRequest` 가 0", "`0x12000f` 가 `0xc0000024`" 는 PE_emulator 가 흉내 낸 실행의 기록이다. 그래서 `[관찰]` 로 안 쓴다 (flow.md 기준 줄).
3. **"NSI" 라는 이름이 세 겹이다.** 이번 글의 핵심이 이 셋을 가르는 데 있다.

   | 이름 | 무엇 | 이 글 |
   |---|---|---|
   | `nsi.dll` | 유저 쪽 함수 모음 (`NsiGetAllParameters` 등). 아래 장치에 주문서를 낸다 | 2.8 ~ 2.10 |
   | `\\.\Nsi` 장치 | `nsiproxy.sys` (커널) 가 받는 장치. 주문서 셋 `0x120007` · `0x12000f` · `0x12001b` | run124 해설 2.16 |
   | `winnsi.dll` | **장치가 아니라 RPC** 로 nsisvc 쪽에 붙는 함수 모음 (`NsiConnectToServer` 등) | 2.6 |

4. **오프셋 두 가지.** 트리 (`├─ +0x..`) 의 숫자는 **ret 자리** (콜스택에 찍힐 값), 본문 "`+0x123 GetAdaptersAddresses`" 처럼 함수 이름 앞의 숫자는 **call 자리**, 그냥 "`+0xa26`" 은 함수 안 **줄** 이다. run 문서와 같은 규칙. 콜스택으로 본 적은 없다.
5. **칸 이름은 "우리 이름" 이 많다.** 다섯 PDB 모두 공개 심볼이라 구조체 타입 · IOCTL 상수 이름이 없다.
   - 칸의 **위치 · 순서 · 쓰는 줄은 `[정적]`**.
   - 칸 묶음 이름 "키 · RW · ROD · ROS" 는 **`[추측]`** (2.8).
   - 모듈 ID 상수 이름 (`NPI_MS_*_MODULEID`) 만 PDB 에 있다. 객체 번호 (1 · 7 · 8 · 0 · 9 · 10 · `0x10`) 는 이름 없이 숫자뿐이다.
6. **갈래 이름 F1 · F2 · F3 은 결과서가 붙인 이름이다.** `WinHttpSendRequest` 안에서 갈라지는 세 길에 번호를 단 것뿐이다 (2.1).
   - F1 = 웹 I/O 시작 길, F2 = 프록시 이름 길, F3 = 이름 풀이 길.
   - 번호가 시간 순서는 아니다. 코드에선 **F1 (`+0x25a`) 이 F2 (`+0x2fa`) 보다 앞** 이다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = **번호 붙은 주문서** → [run66 해설 2.7](run66-apphelp-cache-해설.md)
- `STATUS_PENDING` (`0x103`) = "아직 처리 중 (접수했어요)" → [run92 해설 2.7](run92-bcrypt-ksecdd-해설.md)
- IOSB = **전표** (위 칸 도장 `Status`, 아래 칸 `Information`), `0x80000005` = "종이가 작아요" 말투 → [run94 해설 2.6](run94-bcrypt-resolve-해설.md)
- 이벤트 = **신호등** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- 지연 가져오기 (delay import) = 시작 때 바로 들이지 않고 **처음 쓸 때** 들이는 목록, "필요하면 그때 부를게요" 메모 → [run25 해설 2.5](../02_sprint/run25-dll-entry-해설.md) · [run37 해설](../02_sprint/run37-k32-import-order-해설.md)
- RPC 런타임 = 가게 안 다른 가게로 일을 부탁하는 **전화 부서** → [run99 해설](run99-rpc-caller-해설.md) "한 줄로 먼저"
- run124 에서 푼 말 — **nsi** (2.16, 세 주문서 · 길이 먼저 묻기), **`METHOD_NEITHER`** (2.6), **비동기 핸들** (2.3), **소켓** → [run124 해설](run124-afd-socket-해설.md)
- run126 에서 푼 말 — **컴파트먼트 (망 구획)** · **루프백** (2.8), **성공 · 경고 · 오류** 도장 색 (2.16) → [run126 해설](run126-afd-addrlist-해설.md)

이번에 처음 나오는 말 — **IPHLPAPI**, **winhttp · webio**, **프록시 · 연결 이름**, **세대값**, **알림 등록**, **모듈 ID · 객체 번호**, **키 · RW · ROD · ROS**, **LUID**, **IfType**, **`RtlNtStatusToDosError`** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 핸들 = **손잡이**.
- run124 · run126 의 그림 그대로:
  - nsi (`\\.\Nsi` 장치) = 관리사무소의 **망 안내 데스크**. "이 동네에 어떤 길 (어댑터) 이 있나요?" 를 묻는 곳.
  - IOCTL = **번호 붙은 주문서**, IOSB = **전표**, 이벤트 = **신호등**.
  - status 도장 색 = 초록 (성공 `0x0…`) · 노랑 (경고 `0x8…`) · 빨강 (오류 `0xC…`).
  - RPC = **전화 부서** (run99).
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 따름):
  - winhttp = 가게 안 **웹 편지 담당 직원**. `WinHttpSendRequest` = 웹 편지 한 통 보내기.
  - IPHLPAPI = 그 직원이 부리는 **비서**. 안내 데스크에 직접 가서 묻는 건 비서다.
  - webio = 웹 편지 담당이 쓰는 **작업실**. 처음 쓸 때 문을 연다 (`WebInitialize`).
  - 알림 등록 = "길이 바뀌면 알려 주세요" **신청**. 이 신청은 안내 데스크가 아니라 **전화 (RPC) 로 다른 사무실 (nsisvc)** 에 한다 `[추측: 비유]` (run 문서 표시 그대로).
- 주의 — 이름이 겹치는 곳:
  - run 문서 "결과" 는 NSI 를 "**주민센터 안내 창구**" 라 부른다. 이 글은 run124 해설에 맞춰 **망 안내 데스크** 로 부른다. 같은 곳이다.
  - run124 의 "**우편 담당 직원**" 은 mswsock (소켓) 이다. 이번 "웹 편지 담당" (winhttp) 과 다른 사람이다.

### 한 줄로 먼저 — 무엇을 얻었나

> **웹 편지 담당 (winhttp) 은 안내 데스크 (NSI 장치) 에 직접 안 간다. 비서 (IPHLPAPI `GetAdaptersAddresses`) 를 보낸다.**
> **비서가 "모르겠대요" 하고 돌아와도 담당은 "연결 이름 없음" 이라 적고 그냥 간다. 그래서 이 길의 NSI 대답으로는 편지 보내기가 안 막힌다.**
> **편지 보내기를 막을 수 있는 건 따로 — 작업실 (webio) 문을 처음 열 때 하는 "길 바뀌면 알려 주세요" 신청이다. 이건 안내 데스크가 아니라 전화 (winnsi RPC) 다.**
> **그런데 그 신청은 `0x12000f` 보다 앞에서 끝난다. 그러니 "`0x12000f` 가 보인 그 호출이 왜 0 을 돌려주나" 는 정적으로 못 찾았다.**

앞 세 줄은 `[정적]`. 마지막 줄은 **`[공백]`** 이다 (2.7). 비유 줄 "전화로 다른 사무실" 은 run 문서가 `[추측: 비유]` 로 둔 것이다.

요청서 질문에 대한 답 (run 문서 "요약" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| 1 누가 묻나 (가장 급함: `WinHttpSendRequest`) | winhttp 스스로가 아니라 **IPHLPAPI `GetAdaptersAddresses(0, 0x80f)`** 를 거친다. 다섯 모듈이 nsi 를 쓰는 건 지연 가져오기뿐 `[정적]` | 2.2 · 2.4 |
| 1 인자 | `NsiGetAllParameters(1, NDIS, 8, 키 없음, RW 8 B)` = `{compartment 번호, 범위}`, 이어서 `NsiAllocateAndGetTable` 셋 (NDIS 1 · IPv4 7 · IPv6 7) `[정적]` | 2.8 |
| 1 칸 표 셋 | 세 코드 모두 **같은 칸 순서** — `+0x10` 모듈 ID · `+0x18` 객체 · `+0x20` 첫 인자 · `+0x28` 키 · (포인터, 크기) 쌍 `[정적]`, 칸 이름 `[추측]` | 2.9 |
| 2 실패 때 | F2 는 **오류를 다 삼킨다** (이름 NULL). F1 (`NotifyIpInterfaceChange` 등) 이 실패하면 `WinHttpSendRequest` 0 — 단 RPC `[정적]` | 2.5 · 2.6 |
| 2 빈 표 | `0x12001b` 가 개수 0 을 두 번 주면 `GetAdaptersAddresses` = `0xe8` `[정적]` | 2.10 |
| 2 최소 대답 | F2 엔 **필수 대답 없음**. F1 은 NSI 장치로 못 고침 → run129 `[정적 + 공백]` | 2.13 |
| (요청 쪽 로그) 왜 0 인가 | **못 찾음** `[공백]` → Q222 · run129 | 2.7 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run124 · run126 · run127 과 같은 자리, **초기화 밖** 이다. `main` 뒤 프로그램 코드가 웹을 쓸 때의 옆길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll: LdrInitializeThunk → LdrpInitializeProcess → DLL 들이기 · 전원 켜기
 └─ NtContinue (은퇴) → exe 진입점 → (CRT 점선 상자) → main
                                                        │
      test.exe 는 여기서 웹 · 소켓을 안 쓴다 [소스]     │ 웹을 쓰는 프로그램이라면 (요청 쪽 에뮬레이터 로그):
                                                        ├─ WinHttpOpen …  (앞 준비 — 작업실 문이 여기서 먼저 열릴 수도, 2.6)
                                                        ├─ WinHttpSendRequest                        ← 이번
                                                        │    ├─ 작업실 문 열기 (webio)  → 전화 (winnsi RPC)   ← 이번 (F1)
                                                        │    └─ 프록시 이름 (IPHLPAPI) → 안내 데스크 (\\.\Nsi) ← 이번 (F2)
                                                        ├─ (이름 풀이 · 소켓)                          (run124 · F3)
                                                        └─ GetAdaptersAddresses · GetAdaptersInfo · GetAddrInfoW 를 직접 부를 때도  ← 이번 (2.14)
```

### 왜 이 질문이 지금 왔나

PE_emulator 에서 이런 일이 있었다 (요청서 "왜", 에뮬레이터 로그 — 진짜 Windows 관찰 아님).

1. `WinHttpSendRequest` 안에서 `NtCreateFile("\??\Nsi")` 가 기본 대답 = **핸들 −1** 을 받았다.
2. 그 −1 로 `NtDeviceIoControlFile(−1, 0x12000f, 입력 = 출력 0x68)` → `0xc0000024`.
3. `WinHttpSendRequest` 가 **0** (실패). 소켓으로 나가 보기도 전이다.

요청한 쪽이 로그에서 센 것 (요청서 표, 에뮬레이터 로그):

| exe 가 부른 API | 샘플 | 그 안의 NSI 코드 · 입력 길이 |
|---|---|---|
| `WinHttpSendRequest` | 213 | `0x12000f` · `0x68` |
| `WinHttpGetIEProxyConfigForCurrentUser` | 105 | `0x12000f` · `0x68` + `0x12001b` · `0x70` |
| `GetAdaptersAddresses` | 129 | `0x12000f` + `0x12001b` |
| `GetAddrInfoW` · `getaddrinfo` · `gethostbyname` | 108 · 23 · 21 | `0x120007` · `0x50` |
| `GetAdaptersInfo` | 67 | `0x12001b` · `0x70` |

- 방향 (요청한 쪽 결정): **루프백 하나만 있는 PC** 로 답한다. MAC 없음. 진짜 망에 안 나감.
- 그러려면 "안내 데스크가 무엇을 어떤 종이에 적어 줘야 하나" 를 알아야 한다. 그게 이번 질문이다.

### 무엇이 궁금했나

1. **누가 무엇을 묻나** — 위 API 다섯에서 nsi 함수까지의 사슬, 넣는 인자, 그리고 `0x68` · `0x50` · `0x70` 구조체 칸 표 셋. 가장 급한 건 `WinHttpSendRequest` 길.
2. **부른 쪽이 대답을 어떻게 읽나** — 실패면 통째로 실패하나, 빈 표면 어떻게 가나. 그래서 `WinHttpSendRequest` 가 이름 풀이 · 소켓까지 가려면 **꼭 받아야 하는 최소 대답** 은?
- (원 요청 3 "VM 실제 바이트" 는 동적이라 이번엔 안 했다 → Q222.)

### 바퀴 전에 알던 것

- `[정적]` (run124, sast/121) nsi.dll 의 틀:
  - 열기 = `CreateFileW(L"\\.\Nsi", 0, 3, OPEN_EXISTING, FILE_FLAG_OVERLAPPED)` → 전역 손잡이 하나.
  - 주문서 셋: `0x120007` (`0x50`) · `0x12000f` (`0x68`) · `0x12001b` (`0x70`). 모두 입력 = 출력 같은 구조체.
  - `0x12001b` 는 포인터 모두 0 으로 한 번 (개수 묻기) → 개수 + 여유로 한 번 더.
  - nsi.dll 은 `status < 0` 이거나 `0x105` 면 `RtlNtStatusToDosError` 값을 돌려준다.
- `[추측]` (run124 9절) `0x70` 의 `+0x28 ~ +0x60` 은 "(버퍼 포인터, 칸 크기) 네 쌍처럼 보인다". "개수 0 이어도 최소 10칸 잡고 둘째 호출" 도 `[추측]` — 이번에 둘 다 풀린다 (2.16).
- 모른 것: 구조체 칸 **뜻** (어느 모듈 · 객체 · 어느 묶음), 그리고 **누가** 이 주문서를 내는지. winhttp · webio · IPHLPAPI · dnsapi 분석은 **0** 이었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 두 갈래 (+ 참고 하나)

run 문서 "호출 관계" 를 줄였다. `+0x..` 는 ret 자리. 모두 `[정적]`, `(…)` 는 줄인 곳.

```
winhttp!WinHttpSendRequest
 ├─ (F1) +0x25f  PrepareWebIOSessionForRequest                    실패 → WinHttpSendRequest 0
 │        └─ … +0x44 webio!WebInitialize   (작업실 문 열기 — 핸들 있으면 건너뜀)
 │                 └─ … WapInitializeNetworkChange → … → Startup
 │                      ├─ +0xb4  iphlpapi!NotifyIpInterfaceChange
 │                      │         └─ +0xa0  InternalRegisterChangeNotification
 │                      │                  └─ +0x63  NsiNotificationConnectToServer
 │                      │                           └─ +0x2d  winnsi!NsiConnectToServer   (RPC — [공백] → run129)
 │                      └─ +0xf1  iphlpapi!NotifyUnicastIpAddressChange → (…) → 같은 InternalRegisterChangeNotification
 └─ (F2) +0x2ff  HTTP_USER_REQUEST::SendRequest                   NSI 오류 삼킴
          └─ +0x299 _CreateProxyResolver → SafeGetProxyResolver → +0xc3 CheckProxySettings   (세대값 다를 때만)
                   └─ +0x381  GetActiveConnectionName → +0x7c GetActiveConnectionNameRAS
                            ├─ +0x12a  iphlpapi!GetAdaptersAddresses(0, 0x80f)
                            │          └─ +0x60  AllocateAndGetAdaptersAddresses
                            │                   ├─ +0xc8   nsi!NsiGetAllParameters     NDIS 8   → 0x12000f (0x68)
                            │                   └─ +0x166 · +0x1d0 · +0x23d  nsi!NsiAllocateAndGetTable  NDIS 1 · IPv4 7 · IPv6 7 → 0x12001b (0x70)
                            └─ +0x1ea / +0x2da  iphlpapi!GetBestInterfaceEx → nsi!NsiGetParameter  0x120007 (0x50)

(참고) dnsapi!Query_PrivateExW  (GetAddrInfoW 길 — F3 이름 풀이 길의 NSI 도 이것 하나)
 └─ +0x612  iphlpapi!GetCurrentThreadCompartmentId → nsi!NsiGetParameter  0x120007   (실패 → 1)
```

- 두 갈래의 성격이 정반대다.
  - **F2** 는 안내 데스크 (NSI 장치) 에 **정말로 간다**. 그런데 무슨 대답을 들어도 **안 막힌다** (2.5).
  - **F1** 은 안내 데스크에 **안 간다** (전화 = RPC). 그런데 실패하면 **막힌다** (2.6).

#### 새 말 — winhttp · webio

- **winhttp** = 프로그램이 웹 요청 (HTTP) 을 보낼 때 부르는 DLL. `WinHttpOpen` · `WinHttpConnect` · `WinHttpOpenRequest` · `WinHttpSendRequest` 같은 API 를 내놓는다 `[정적: 함수 이름]` (`_map` 대상).
- **webio** = winhttp 가 뒤에서 쓰는 DLL. 내보내기가 **번호뿐** 이라 Ghidra 가 `Ordinal_17` 처럼 잡는다 `[정적]`. 결과서가 PDB 공개 심볼과 맞춰 이름을 얻었다: `Ordinal_17 = WebInitialize` · `Ordinal_9 = WebCreateSession` · `Ordinal_25 = WebSendHttpRequest` 등. 이 글은 PDB 이름을 쓴다.
- 비유: winhttp = **웹 편지 담당 직원**, webio = 그 직원의 **작업실**.

### 2.2 누가 nsi 를 부르나 — 가져오기 표부터 `[정적]`

결과서는 무거운 디컴파일 전에 **가져오기 표** 부터 봤다. "nsi.dll 함수를 가져오는 모듈만 보면 된다" 는 가벼운 길이다.

| 모듈 | nsi.dll 에서 가져오는 것 | 이번 길에서 |
|---|---|---|
| winhttp | `NsiAllocateAndGetTable` · `NsiFreeTable` **둘뿐** (지연) | 시작점. NSI 는 IPHLPAPI 를 거쳐 닿음 |
| webio | **없음** | IPHLPAPI 알림 등록만 부름 |
| IPHLPAPI | `NsiGetParameter` · `NsiGetAllParameters` · `NsiAllocateAndGetTable` · … (지연, + `WINNSI.DLL` 의 `NsiRpc*` · `NsiConnectToServer`) | **실제로 묻는 쪽** |
| dnsapi | `NsiGetParameter` · `NsiGetAllParameters` · `NsiAllocateAndGetTable` · `NsiFreeTable` (지연) | `GetAddrInfoW` 길의 compartment 묻기는 IPHLPAPI 를 거침 |
| ws2_32 (배제용) | `NsiGetParameter` · `NsiGetAllParameters` · `NsiSetAllParameters` (지연) | `GetAddrInfoW` 길에서 **안 부름** (부르는 자리 셋의 caller 는 `WSAConnectByNameHelper` 뿐) |

- 가져오는 넷은 **모두 지연 가져오기** 로만 가져온다. 일반 가져오기 표엔 없다 `[정적]`.
  - 그래서 `objdump -p` 의 "DLL Name" 줄만 보면 **놓친다** (결과서 · run 문서 도구 메모). `pefile` 로 지연 표까지 읽었다.
- `System32` 4,346 파일 전수 조사에서 `NSI.dll` 을 가져오는 모듈 중 질문과 닿는 건 ws2_32 · winhttp · dnsapi · IPHLPAPI. 나머지 50여 개 (ipconfig · netprofm · ncsi 등) 는 질문 밖이라 안 봤다.

#### 새 말 — IPHLPAPI

- `IPHLPAPI.DLL` (파일 이름 대문자). 망 어댑터 · 인터페이스 정보를 묻는 API (`GetAdaptersAddresses` · `GetAdaptersInfo` · `GetBestInterfaceEx` · `NotifyIpInterfaceChange` …) 가 들어 있는 DLL 이다 `[정적: 이번에 본 함수들]`.
- 이번 표로 보면 **NSI 를 실제로 묻는 쪽** 이다.
- 비유: 웹 편지 담당 (winhttp) 이 부리는 **비서**. 안내 데스크에 가서 줄 서는 건 비서다.

### 2.3 한 줄씩 읽는 법

run 문서 "정적 — 호출 지점" 표에서 두 줄을 골라 읽어 보자.

**줄 ①** `| winhttp!GetActiveConnectionNameRAS | +0x123 | +0x12a | iphlpapi!GetAdaptersAddresses (가져오기 칸) | [정적] |`

- **누가**: winhttp 의 `GetActiveConnectionNameRAS` (rva `0x525a4`). "지금 쓰는 망 연결의 이름" 을 구하는 함수다 (2.4).
- **어디서**: `+0x123` 에서 call, 돌아올 자리 `+0x12a`.
  - call 과 ret 차이가 **7** 이다. 5 바이트 `e8` 이 아니라 **가져오기 칸을 거쳐 부르는** 7 바이트 꼴 (`48 ff 15`) 이라서다 (결과서 "버전 대조").
  - 콜스택에 찍히면 `winhttp!GetActiveConnectionNameRAS+0x12a` 로 보인다.
- **누구를**: `iphlpapi!GetAdaptersAddresses(0, 0x80f, NULL, 버퍼 0x7800, &크기)`. 다른 DLL (비서) 의 함수다.
- **무엇을 위해**: 어댑터 목록을 받아, `GetBestInterfaceEx` 가 준 번호와 `IfIndex` 가 맞는 어댑터의 **이름 (FriendlyName)** 을 쓰려고 (run 문서 8절).

**줄 ②** `| iphlpapi!InternalRegisterChangeNotification | +0x5e | +0x63 | NsiNotificationConnectToServer — NULL 이면 +0x6b 6 | [정적] |`

- **누가**: IPHLPAPI 의 `InternalRegisterChangeNotification` (rva `0x9758`). "길 바뀌면 알려 주세요" 신청의 공용 몸통이다.
- **어디서**: `+0x5e` call, ret `+0x63`.
- **누구를**: `NsiNotificationConnectToServer` (rva `0x1b18c`) → 그 안 `+0x26` 에서 `winnsi!NsiConnectToServer` (RPC, `ncalrpc`).
- **무엇을 위해**: 전화 연결 (연결 핸들) 을 얻으려고. 돌아온 값이 **NULL 이면** `+0x66 TEST RAX,RAX` · `+0x69 JNZ` 를 못 넘고 `+0x6b` 에서 **6** 을 만든다 (2.6).
  - 6 = `ERROR_INVALID_HANDLE` (run 문서 표기).

### 2.4 F2 — 프록시 이름 길 (안내 데스크에 정말 간다) `[정적]`

run 문서 2절. `WinHttpSendRequest` 에서 `0x12000f` 까지 **열 단계** 다.

#### 새 말 — 프록시 · 연결 이름

- **프록시** = 웹 편지를 대신 받아 부쳐 주는 **중간 집** (일반적으로 알려진 설명 `[추측]`). winhttp 는 편지를 보내기 전에 "프록시를 거쳐야 하나" 를 정하려고 **프록시 해석기** (`_CreateProxyResolver`) 를 만든다 `[정적: 함수 이름]`.
- **연결 이름** = 지금 쓰는 망 연결의 이름. winhttp 는 이 이름을 **프록시 설정 읽기에 넘긴다** `[정적]` (run 문서 8 · 10절). 왜 이름이 필요한가 — 연결마다 프록시 설정이 다를 수 있어서로 보통 설명한다 `[추측]`.
- 비유: 편지 보내기 전에 비서에게 "지금 우리 가게가 어느 길에 붙어 있지? 그 길 이름표를 봐 와" 하고 시키는 것.

#### 열 단계 (run 문서 2절 표를 줄임)

| 단계 | 함수 | 조건 |
|---|---|---|
| 1 | `WinHttpSendRequest +0x2fa` → `SendRequest` | |
| 2 | `SendRequest +0x294` → `_CreateProxyResolver` | `[this+0xcc] == 0` 일 때만 (첫 보냄) |
| 3 | `_CreateProxyResolver +0x2a` / `+0x55` → `SafeGetProxyResolver` | 세션 `[+0x90]` ≠ 0 이면 인자 1 먼저 (`0x80070490` 이면 인자 0 으로 다시), 아니면 인자 0 |
| 4 | `SafeGetProxyResolver +0xbe` → `CheckProxySettings` | 루트 세션 핸들까지 올라가서 |
| **5** | `CheckProxySettings +0x37c` → `GetActiveConnectionName` | **세대값이 다를 때만** (아래) |
| 5' | `LoadAutomaticProxyResolvers +0x224` → `GetActiveConnectionName` | 언제 불리는지 **`[공백]`** |
| 6 | `GetActiveConnectionName +0x77` → `GetActiveConnectionNameRAS` | |
| 7 | `GetActiveConnectionNameRAS +0x123` → `GetAdaptersAddresses(0, 0x80f, …)` | `0x6f` (버퍼 모자람) 면 `+0x800` 키워 최대 3번, 그 밖 오류는 바로 포기 |
| 7' | 같은 함수 `+0x1e3` / `+0x2d3` → `GetBestInterfaceEx` | `GetAdaptersAddresses` 성공일 때만. `::` 먼저, 실패면 `0.0.0.0` |
| 8 | `GetAdaptersAddresses +0x5b` → `AllocateAndGetAdaptersAddresses` | 계열 0 · 2 · 23 만 (아니면 `0x57`) |
| 9 | `AllocateAndGetAdaptersAddresses +0xc1` → `NsiGetAllParameters`, `+0x15f` · `+0x1c9` · `+0x236` → `NsiAllocateAndGetTable` ×3 | 인자 2.8 |
| 10 | `NsiGetAllParameters +0x105` → `NtDeviceIoControlFile(0x12000f, 0x68)` | 칸 2.9 |

- 플래그 `0x80f` = 유니캐스트 · 애니캐스트 · 멀티캐스트 · DNS 서버 **건너뜀** + `0x800` `[추측: SDK 상수 이름]`.
  - 그래서 9 단계에서 주소 붙이기 (`AddUnicastAddresses` 등) 는 안 한다.
  - 대신 `AddDhcpConfiguration` (`+0xb2d`) · `AddNetbtConfiguration` (`+0xb64`) 은 **어댑터가 있으면 늘** 부르고, 0 이 아니면 `GetAdaptersAddresses` 가 통째로 실패한다. 둘은 NSI 가 아니다 (dhcpcsvc RPC · NetBT 장치) — 이름만. 어댑터 0 개면 안 탄다.

#### 새 말 — 세대값 (`g_ullNetworkGeneration`)

- winhttp 전역에 있는 숫자 하나 `[정적]`. **올리는 곳은 `WinHttpNetworkChangeNotification` 뿐** 이다.
- 세션도 "내가 마지막으로 본 값" 을 `[this+0x228]` 에 들고 있다. **둘 다 시작값 0**.
- `CheckProxySettings` 는 두 값을 견줘 (`+0x85` · `+0xa4` 전역, `+0x91` · `+0xab` 세션) **다를 때만** 5 단계 (`+0x37c`) 를 탄다.
- 비유: 동네 지도의 **판 번호**. 길이 바뀌었다는 소식이 올 때마다 판 번호가 1 오른다. 세션은 "내가 본 지도는 몇 판" 을 기억해 두고, 판이 바뀌었을 때만 비서를 다시 보낸다.
- 그래서: **망 바뀜 알림이 없으면 5 단계는 안 탄다** `[정적]`. 첫 보냄에서 이 줄을 타는지는 **`[공백]`** 이다 (→ Q222 동적 8 · Q237 ⑤).

### 2.5 ★ F2 는 오류를 다 삼킨다 `[정적]`

이번 바퀴의 가장 큰 답 하나.

- `GetAdaptersAddresses` 가 실패하면 (NSI 가 무엇을 돌려줬든), `GetActiveConnectionNameRAS` 가 그 HRESULT 로 끝난다.
- `GetActiveConnectionName` 은 RAS 결과가 음수면 **NULL (이름 없음)** 을 돌려줄 뿐이다. **오류를 위로 안 올린다** (run 문서 8절).
- `CheckProxySettings` · `LoadAutomaticProxyResolvers` 는 이름이 NULL 이어도 **레지스트리 프록시 읽기 · 해석기 만들기로 이어 간다** (결과서 A5).
- 결론: **이 길엔 "꼭 받아야 하는 최소 대답" 이 없다.** 장치가 열리고 IOCTL 이 아무 오류를 돌려줘도 `WinHttpSendRequest` 는 이 자리에서 안 멈춘다.

비유: 비서가 안내 데스크에서 "모르겠어요" 를 듣고 와도, 담당은 이름표 칸에 "없음" 이라 쓰고 편지 준비를 계속한다.

### 2.6 ★ F1 — 웹 I/O 시작 길 (안내 데스크가 아니라 전화) `[정적 + 공백]`

run 문서 3절.

#### 새 말 — 알림 등록

- `NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange` = "망 인터페이스 · 주소가 바뀌면 이 함수를 불러 주세요" 하고 **콜백을 맡겨 두는** IPHLPAPI API `[정적: 이번에 본 사슬]`.
- webio `Startup` 은 `InitialNotification = FALSE` 로 등록한다 → 콜백은 **곧바로 안 불린다** (결과서).
- 비유: 작업실 문을 처음 열 때 "길이 바뀌면 알려 주세요" **신청서** 를 낸다. 이 신청은 안내 데스크가 아니라 **전화 부서 (RPC)** 를 통해 다른 사무실 (nsisvc) 로 간다.

#### 사슬 — 작업실 문 열기에서 전화까지

- `WinHttpSendRequest +0x25a` → `PrepareWebIOSessionForRequest +0x46` → `CreateWebIOSession +0x47` → `WINHTTP_GLOBALS::GetWebIOApiHandle +0x4e` → `WINHTTP_DLL::GetWebIOApiHandle +0x3d` → **`webio!WebInitialize`** `[정적]`.
- `WebInitialize` 안은 한 줄씩 caller 하나라 화살표로 이어진다: `WapInitialize` → `WaIncrementDllInitializationCount` → `WapInitializeDll` → `WaInitializeUtil` → `WapInitializeNetworkChange` → `WxRegisterForNetworkChangeNotification` → `StartGlobalNetworkChangeMonitor` → `Startup` `[정적]`.
- `Startup +0xad` → `iphlpapi!NotifyIpInterfaceChange` → `+0x9b InternalRegisterChangeNotification` `[정적]`.
- `Startup +0xea` → `iphlpapi!NotifyUnicastIpAddressChange` → `+0x19 NetioRegisterAddressChangeNotification` → `+0x9d InternalRegisterChangeNotification` — **한 단계 더** 거쳐 같은 함수로 간다 `[정적]` (flow-writer 대조, 2.16).
- `InternalRegisterChangeNotification +0x5e` → `NsiNotificationConnectToServer +0x26` → **`winnsi!NsiConnectToServer`** (RPC, `ncalrpc`) `[정적]`.
  - 같은 함수 `+0x9d` · `+0xd5` 에서 `winnsi!NsiRpcRegisterChangeNotification` 도 부른다 — 이름만 (→ run129).

#### 실패가 올라가는 사다리 `[정적]`

아래 한 칸이 빨강이면 위 칸까지 빨강이 그대로 올라간다.

1. 연결 핸들 NULL → `InternalRegisterChangeNotification +0x66 ~ +0x6b` 가 **6** → `NotifyIpInterfaceChange` 가 그대로 돌려줌.
2. → webio `Startup +0xbc TEST EAX,EAX` 오류.
3. → `WapInitializeNetworkChange` (`+0xa7 WX_WIN32_FROM_HR` · `+0xac MOV EBX,EAX` · 끝 `+0x156 MOV EAX,EBX`) → `WaInitializeUtil` 실패 → **`WebInitialize` 실패**.
4. → `GetWebIOApiHandle` 0 아님 → `CreateWebIOSession` → `PrepareWebIOSessionForRequest +0x4b ~ +0x4f` (`TEST` / `JNZ`).
5. → `WinHttpSendRequest +0x25f ~ +0x263` (`TEST` / `JNZ`) → `SetLastError` 뒤 **0**.

- 그래서 `NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange` 가 0 이 아니면 `WinHttpSendRequest` 는 0 이다 `[정적]`.
- 그리고 이 둘은 **NSI 장치 IOCTL 이 아니라 winnsi RPC** 다. 안내 데스크에 무슨 대답을 놓아도 이 줄은 못 고친다.

#### 한 번 열리면 다시 안 연다 `[정적]`

- `WebInitialize` 가 한 번 성공하면 핸들을 전역에 둔다. `GetWebIOApiHandle__80f4` 는 `[전역+0x28]` 이 0 이 아니면 `+0x3d` 를 **건너뛴다**.
- 앞선 `WinHttpOpen +0x147 → WinHttpSetOptionInternal → CreateStaticResolverCacheEntry → CreateWebIOSession` 에서 먼저 돌았을 **수도** 있다 (결과서).
- 비유: 작업실 문은 한 번 열면 열린 채로 둔다. 두 번째 편지부터는 문 열기 (와 그 전화) 를 안 한다.

#### 아직 모르는 한 가지 — 전화가 정말 걸리나 `[공백]`

- `NsiConnectToServer` 가 **실제로 서버를 만나는지** 는 winnsi 를 안 읽어서 모른다.
  - 결과서 메모: `RpcBindingFromStringBindingW` 는 연결은 안 하고 **바인딩 핸들만** 만든다. 그러니 "전화번호를 적어 둔 것" 인지 "정말 전화를 건 것" 인지가 갈린다.
- → sast/127 (Q223) 결과서 있음 → **run129 예정**.

### 2.7 ★ 그럼 로그의 "0" 은 왜? — 근거 → 그래서 → 아직 확인 아님

요청 쪽의 진짜 질문은 "`0x12000f` 가 `0xc0000024` 를 받은 뒤 왜 `WinHttpSendRequest` 가 0 인가" 였다. 정적으로 한 단계씩 따져 보자.

- **근거 1** `[정적]`: `0x12000f` 는 F2 에서 나온다 (`+0x2fa` 쪽). F2 는 NSI 오류를 다 삼킨다 (2.5).
- **그래서**: `0xc0000024` 자체는 `WinHttpSendRequest` 를 0 으로 만들지 **않는다**.
- **근거 2** `[정적]`: F1 (`+0x25a`) 은 F2 (`+0x2fa`) 보다 **앞** 이다. F1 이 실패하면 F2 에 가기 전에 0 으로 끝난다.
- **그래서**: 로그에 `0x12000f` 가 찍힌 호출은 F1 을 **이미 지났다** (또는 핸들이 있어 건너뛰었다). F1 도 원인이 아니다.
- **아직 확인 아님** `[공백]`: 그러면 그 호출의 0 반환 원인은 **정적으로 못 찾았다.**
  - F1 은 오히려 "**`0x12000f` 가 안 보이고 일찍 0 이 되는 샘플**" 의 후보다.
  - 확인은 동적 → **Q222** (동적 1 · 4), winnsi 쪽 정적 → **run129 예정**.

곁가지 하나 `[추측]`:

- 요청 쪽 로그의 "같은 `0x12000f` 가 한 번 더" — `GetActiveConnectionName` 이 두 곳 (`CheckProxySettings +0x37c` · `LoadAutomaticProxyResolvers +0x224`) 에서 불리기 때문일 것이다 `[추측]`.
- `0x6f` 재시도 (7 단계) 는 NSI 실패 (`ERROR_INVALID_HANDLE` 등) 에선 안 돈다 — 그건 "버퍼 모자람" 일 때만이다 `[정적]`.

### 2.8 `GetAdaptersAddresses(0, 0x80f)` 가 안내 데스크에 묻는 것 `[정적]`

run 문서 5절. 이제 F2 의 맨 끝, 비서가 데스크에 내는 **질문지 넷** 이다.

#### 새 말 — 모듈 ID · 객체 번호

- **모듈 ID** = "어느 **부서** 장부를 볼까요" — NDIS (어댑터) · IPv4 · IPv6 등. 24 바이트 상수이고 IPHLPAPI `.rdata` 에 있으며 PDB 에 이름이 있다 (`NPI_MS_NDIS_MODULEID` 등, 2.15) `[정적]`.
- **객체 번호** = 그 부서의 **몇 번 장부** 인가 (숫자뿐, 이름 없음) `[정적]`.
  - 이번에 나온 것: NDIS **1** (어댑터 표) · NDIS **8** (compartment 번호 · 범위) · IPv4/IPv6 **7** (IP 인터페이스 표) · IPv4/IPv6 **0** (`GetBestInterfaceEx`). 뜻은 그 장부를 읽는 쪽 코드로 붙인 것이다.

#### 새 말 — 키 · RW · ROD · ROS

- 장부 **한 줄** 을 가리키는 **키** 와, 그 줄 안의 **칸 묶음 셋**.
  - RW = 고칠 수 있는 설정, ROD = 읽기만 하는 지금 상태, ROS = 읽기만 하는 고정 정보.
- **이름은 PDB 에 없다** `[추측]`. 코드가 (포인터, 크기) 쌍을 같은 순서로 쓰는 것 + `NsiSetParameter` 가 0 = RW 로 쓰는 것으로 맞춘 이름이다.
- 비유: 장부 한 줄 = 어댑터 한 개. 줄 번호 (키) 를 대면, 그 줄의 "설정 칸 / 상태 칸 / 고정 칸" 을 따로따로 받아 볼 수 있다.

#### 새 말 — LUID

- 이번 키 8 B = **어댑터 LUID** (run 문서 표기). 어댑터 하나를 가리키는 **8 바이트 번호** 다.
- NDIS 표의 LUID 와 IP 인터페이스 표의 LUID 가 같으면 **같은 어댑터** 다 (run 문서 7절).
- 비유: 장부 사이를 잇는 **줄 번호**. 어댑터 장부 3번 줄과 IPv4 장부 3번 줄이 같은 가게 앞 길이라는 표시.

#### 질문지 넷 (`AllocateAndGetAdaptersAddresses` 안)

| 호출 | 첫 인자 | 모듈 | 객체 | 키 | RW | ROD | ROS | 받는 반환 |
|---|---|---|---|---|---|---|---|---|
| `+0xc1` `NsiGetAllParameters` | 1 | NDIS | **8** | 없음 | **8 B** | 0 | 0 | **무엇이든** — 오류면 번호 1 로 이어 감 |
| `+0x15f` `NsiAllocateAndGetTable` #1 | 1 | NDIS | **1** | 8 B (LUID) | 0 | `0x290` /개 | `0x238` /개 | **0 만** (`+0x16f JNZ`) |
| `+0x1c9` #2 | 1 | IPv4 | **7** | 8 B | `0xf0` /개 | 0 | `0x20` /개 | 0 · **`0x32`** |
| `+0x236` #3 | 1 | IPv6 | **7** | 8 B | `0xf0` | 0 | `0x20` | 0 · `0x32` |

- **첫 질문 (NDIS 8, RW 8 B)** = 이 스레드의 `{+0: compartment 번호, +4: 범위}` `[정적]`.
  - 근거: `GetCurrentThreadCompartmentScope` 가 **같은 호출** 로 읽어 `[+4]` 를 범위, `[+0]` 을 번호로 내놓는다 (실패면 `{1, 0}`).
  - [컴파트먼트](run126-afd-addrlist-해설.md) = 망 구획 (run126 해설 2.8, "구역별 주소록").
  - 쓰임: `+0xa26 CMP [ROD(i)+0], R13D` — 플래그 `0x200` 이 없으면 **ROD 첫 dword (CompartmentId) 가 이 번호와 같은 어댑터만** 남긴다.
  - 실패해도 `+0xcf JNZ → +0xf71` · `+0xf74 MOV R13D,1` → 번호 **1** 로 이어 간다. 이 질문은 오류를 삼킨다.
- **둘째 질문 (NDIS 1)** 만 **0 이 아니면 바로 실패** 한다. 어댑터 장부는 꼭 받아야 한다는 뜻이다 — 다만 그 실패도 F2 위쪽 (2.5) 에서 삼켜진다.
- IPv4 · IPv6 장부는 `0x32` ("모듈 없음" 쪽 DOS 값) 도 받아 준다 (`+0x1d9 → +0xfe7 CMP EAX,0x32`).
- `+0xd14` / `+0xf3e` `NsiSetParameter` 는 플래그 `0x200` 일 때만 — 이 길엔 안 탄다.

### 2.9 구조체 칸 표 셋 — 세 질문지는 같은 틀이다 `[정적]`

run 문서 6절. 이번 바퀴 전엔 "`0x70` 의 뒤쪽이 네 쌍처럼 보인다 `[추측]`" 이 전부였다.

#### 공통 틀

- 세 구조체 모두 nsi.dll 이 **같은 칸 순서** 로 채운다.
  - `+0x10` 모듈 ID 포인터 · `+0x18` 객체 번호 · `+0x20` 첫 인자 (모든 호출자 1 — "저장소" 로 짐작 `[추측]`) · `+0x28` 키 포인터 · 그다음 (포인터, 크기) 쌍.
- 구조체 안 포인터 (키 · RW · ROD · ROS) 는 **사용자 주소** 다. 커널이 그 주소를 **따라가서** 읽고 쓴다. 입력 = 출력 같은 버퍼 ([`METHOD_NEITHER`](run124-afd-socket-해설.md), run124 해설 2.6 "손님 종이를 그대로 들여다보고 그 위에 답을 적는다").
- 전표 (IOSB) 는 따로 16 B. nsi.dll 은 **매번 `CreateEventA` 로 신호등을 만들어** 넘기고, `0x103` 이면 `NtWaitForSingleObject` 뒤 `IOSB.Status` 를 본다.
- 비유: 데스크에 내는 질문지 세 종류가 **같은 양식** 을 쓴다. 맨 위 "부서 · 장부 번호 · 1 · 줄 번호" 칸은 똑같고, 아래 "답 받을 칸" 부분만 종류마다 다르다.

#### 셋의 차이

| | `0x12000f` (`0x68`) | `0x120007` (`0x50`) | `0x12001b` (`0x70`) |
|---|---|---|---|
| 쓰는 함수 | `NsiGetAllParameters(Ex)` | `NsiGetParameter(Ex)` | `NsiEnumerateObjectsAllParameters(Ex)` · `NsiAllocateAndGetTable` |
| 한 마디로 | **한 줄 전부** (세 묶음 다) | **한 칸만** | **표 통째** (여러 줄) |
| `+0x10 ~ +0x28` | 공통 | 공통 | 공통 + **`+0x24` 둘째 인자** (`NsiAllocateAndGetTable` 은 늘 1) |
| 뒤쪽 | `+0x38` RW · `+0x48` ROD · `+0x58` ROS (포인터 / 크기) | `+0x38` **어느 묶음** (0 = RW, 1 = ROD) · `+0x40` 결과 칸 포인터 · `+0x48` 결과 크기 · `+0x4c` 묶음 안 시작 오프셋 | `+0x28 ~ +0x60` 키 · RW · ROD · ROS (포인터 / **1개당** 크기) · **`+0x68` 개수** |
| 끝난 뒤 | (비-Ex) `+0x40` RW 크기가 요청보다 **작아지면 `0xc000000d`** (`+0x139` · `+0x22d`) | Ex 는 `IOSB.Information` 을 `+0x48` 에 되씀 | 비-Ex 는 `+0x68` 개수를 `[개수 포인터]` 에 되씀 |

- `0x50` 의 "2 = ROS 일 것" 은 `[추측]`, 묶음 이름도 `[추측]` 이다 (`GetCurrentThreadCompartmentId` 가 0, `GetBestInterfaceEx` 가 1 을 쓴다).
- `0x70` 배열은 **칸 크기 × 개수** 로 이어 붙는다 — 어댑터 i 의 ROD = `ROD 포인터 + i × 0x290` (`AllocateAndGetAdaptersAddresses +0x31e IMUL`).
- nsiproxy 쪽: 입력 < `0x68` → `0xC000000D`, 성공이면 `Information = 0x68`. 공급자 크기가 다르면 사용자 구조체의 크기 칸을 그 값으로 덮어쓴다 (오류 아님).
- 에뮬레이터에 중요한 한 줄: **`0x12000f` 의 `+0x40` (RW 크기 칸) 은 건드리지 말 것.** 줄이면 nsi.dll 이 `0xc000000d` 를 만든다.

### 2.10 개수 먼저 묻기 · 빈 표 `[정적]`

run 문서 5절 끝. run124 해설 2.16 의 "몇 명이에요? → 의자 놓기 → 앉으세요" 를 줄 번호로 닫았다.

1. 첫 `0x12001b` (`+0x119`) — **포인터 모두 0**, 개수만 묻는다.
2. 개수 + `max(개수/8, 10)` 칸을 잡는다 (`+0x16c ~ +0x185`). 그러니 **개수 0 이어도 10 칸** 을 잡는다.
3. 둘째 `0x12001b` (`+0x36b`). status 0 이면 끝 (`+0x39f`).
4. `RtlNtStatusToDosError` 값이 **`0xea`** 면 더 큰 버퍼로 다시 (`+0x3b8`), 최대 3번 (`+0x48`). 첫 호출의 `0xea` 도 받는다 (`+0x4ae`).

#### 새 말 — `RtlNtStatusToDosError`

- 커널식 도장 (NTSTATUS) 을 Win32 오류 번호로 옮겨 적는 함수 (이름 그대로의 뜻). run 문서: `0xea` (ERROR_MORE_DATA) 는 `0x80000005` 에서 온다.
- 비유: run124 해설 2.14 의 "도장 → 손님 말" 표와 같은 일. 이번엔 mswsock 이 아니라 nsi.dll 이 옮긴다.

#### 빈 표 = 이렇게 대답하면 된다

- 첫 호출 status 0 · 개수 0 → 둘째 호출 (칸 수 10) status 0 · 개수 0.
- 그러면 `GetAdaptersAddresses` = **`0xe8`** (ERROR_NO_DATA) `[정적]`. 비서가 "장부가 비었어요" 하고 돌아오는 것이다.
- `GetAdaptersInfo` 는 루프백 (IfType 24) 을 건너뛰어, **루프백만 있어도** `0xe8` 이다 (2.14).

### 2.11 `GetAdaptersAddresses` 가 장부에서 읽는 칸 `[정적 + 추측]`

run 문서 7절. 루프백 줄을 짤 때 "어느 칸이 어디에 쓰이나" 다. **값** 은 공급자 (ndis · tcpip) 가 정해서 정적으론 모른다 → `TODO(VM)`. 칸 **이름** 은 SDK `IP_ADAPTER_ADDRESSES` 칸 순서와 맞춘 `[추측]` 이다.

#### 새 말 — IfType

- 어댑터 **종류 번호** 다. 루프백 = **24 (`0x18`)** (run 문서). ROS `+0x208` 에 있다.
- 비유: 장부 줄의 "길 종류" 칸. 24 = "가게 안에서 가게로 돌아오는 길".

대표만 (전체는 run 문서 7절):

- **NDIS 객체 1 (어댑터 장부)** — ROD `+0x000` CompartmentId (거르기) · `+0x218` OperStatus (1 = 올라감) · `+0x220` Mtu · **`+0x224` / `+0x226` 물리 주소 길이 / 주소 (루프백 = 길이 0)**. ROS `+0x000` IfIndex · **`+0x208` IfType** · `+0x218` 인터페이스 GUID → `AdapterName` `"{…}"`.
- **IPv4 · IPv6 객체 7 (IP 인터페이스 장부)** — 키 = LUID. RW `+0x01c` Metric · `+0x078` Mtu 등.
- **IP 인터페이스가 없는 NDIS 어댑터는 빠진다** (플래그 `0x100` 없을 때, `+0x9df`).
- `GetAdaptersAddresses` 엔 **루프백을 따로 다루는 줄이 없다**. 루프백도 장부의 한 줄일 뿐이다.

### 2.12 부른 쪽이 대답을 읽는 법 `[정적]`

run 문서 8절에서 대표만.

| 누가 | 보는 것 |
|---|---|
| nsi `NsiGetAllParameters` · `NsiAllocateAndGetTable` 등 | `0x103` 이면 기다린 뒤 `IOSB.Status`. **음수 또는 `0x105`** 면 `RtlNtStatusToDosError` 값. **`Information` 은 안 봄** |
| nsi `NsiGetParameter(Ex)` | 빨강 (`0xC…`) 이면 DOS 값만. `0x105` · 노랑 (`0x8…`) 이면 `Information` 을 `+0x48` 에 써 준 뒤 DOS 값 |
| `GetAdaptersAddresses` | 2.8 표의 "받는 반환". 어댑터 0 개면 `0xe8`, 버퍼 모자라면 `0x6f` + 필요 크기 |
| winhttp `GetActiveConnectionName` | RAS 가 음수면 **NULL** — 오류 전파 없음 (2.5) |
| `GetBestInterfaceEx` | `NsiGetParameter` 반환을 **그대로**. 출력 4 B 인터페이스 번호 |
| webio `WapInitializeNetworkChange` (F1) | 알림 등록 둘이 0 이 아니면 **초기화 실패 → `WinHttpSendRequest` 0** (2.6) |

#### ★ 에뮬레이터 로그와 딱 맞는 줄 — 열기 실패 흉내 `[정적]`

- `NsiGetAllParameters` 안에서 `CreateFileW(L"\\.\Nsi", …)` 를 부른다 (`+0x1a6`).
- 결과가 −1 이면 `+0x1e2 CALL GetLastError` → `+0x1ee TEST` → `+0x1f0 JZ`:
  - `GetLastError()` 가 **0 이면 오류로 안 본다** → 그 **−1 을 손잡이 삼아** IOCTL 을 낸다 → `0xc0000024`.
  - 0 이 아니면 그 값을 돌려준다.
- 요청 쪽 로그의 "핸들 −1 로 `0x12000f` → `0xc0000024`" 가 바로 이 모습이다. 에뮬레이터가 열기 실패를 주면서 **마지막 오류 칸을 0 으로 남겨 둔** 경우와 맞는 줄이다.
- 비유: 데스크 문이 잠겨 있었는데 "왜 잠겼는지" 쪽지가 비어 있으니, 비서가 잠긴 문 손잡이를 그대로 잡고 질문지를 밀어 넣는 꼴.

### 2.13 ★ `WinHttpSendRequest` 가 이름 풀이 · 소켓까지 가려면 — 최소 대답 `[정적 + 공백]`

run 문서 9절.

1. **F2 (NSI 장치 — `0x12000f` · `0x12001b` · `0x120007`)**: **필수 대답 없음** (오류를 다 삼킴). 정직한 대답 둘 중 아무거나:
   - **(a) 어댑터 0 개 PC** (가장 단순):
     - 열기 성공 → `0x12000f` (NDIS 객체 8, RW 8 B) 에 `{1, 0}` · status 0
     - → `0x12001b` NDIS 객체 1 첫 호출 status 0 · `+0x68` = 0, 둘째 (칸 수 10) status 0 · 개수 0
     - → IPv4 · IPv6 객체 7 도 개수 0 (또는 "모듈 없음" = DOS `0x32` 가 되는 NTSTATUS — `STATUS_NOT_SUPPORTED` 로 짐작 `[추측]`)
     - → `GetAdaptersAddresses` = `0xe8` → 이름 NULL.
   - **(b) 루프백 1 개 PC**: NDIS 표 1 개 · IPv4 표 1 개 (IPv6 0 또는 1), 칸 값은 2.11.
     - → winhttp 가 `GetBestInterfaceEx` 를 두 번 → 그 `NsiGetParameter` (객체 **0**, 묶음 1 (ROD), 4 B, 오프셋 `0x7c` (v6) / `0x58` (v4)) 가 오류면 **이름 NULL 일 뿐**.
2. **F1 (웹 I/O 시작)**: `WebInitialize` 가 한 번도 성공 못한 프로세스면 `NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange` 가 **0 을 돌려줘야** 한다. 이건 **winnsi RPC** 라 NSI 장치 대답으로는 못 고친다 → run129 예정.
3. **이름 풀이 (F3)**: NSI 는 `GetCurrentThreadCompartmentId` 한 번 (실패하면 1 → 삼킴). 이름 풀이 본문은 `Rpc_ResolverQuery` (DNS 클라이언트 서비스 RPC) — 범위 밖.

- 못 닫은 것: 루프백 줄 실제 바이트, 어댑터 0 개일 때 진짜 `GetBestInterfaceEx` 오류 번호 (`0x4d3` 일지 `0x490` 일지) — `TODO(VM)` → Q222.

비유로: 안내 데스크는 "장부가 비었어요" 라고만 정직하게 말해 주면 된다. 진짜 관문은 데스크가 아니라 **작업실 문 열 때의 전화** 다.

### 2.14 나머지 API 넷 `[정적]`

run 문서 10절. 요청 표의 나머지를 같은 꼴로.

- **`WinHttpGetIEProxyConfigForCurrentUser`** — `+0xc3` → `GetActiveConnectionName` (F2 6 ~ 10 과 같음), `+0x267` → `GetIEProxyConfigForCurrentUserFromService +0x11f` → `GetActiveConnectionNameRAS`. 이름 (NULL 가능) 을 넘길 뿐, **통째 실패 아님**.
  - 요청 쪽 표의 `0x12001b` 는 첫 `0x12000f` 를 통과해야 나오는데, 첫 호출이 늘 실패하는 지금 에뮬레이터에서 같이 보이면 다른 호출자 (`GetAdaptersInfo` 등) 일 것 `[추측]`.
- **`GetAdaptersAddresses` (exe 가 직접, 다른 플래그)** — 어댑터마다 NSI 가 더 붙는다: `AddUnicastAddresses` (객체 `0xa`) · `AddAnycastAddresses` (8) · `AddMulticastAddresses` (9) · `AddGatewaysAndPrefixes` (`0x10`) · `AddDnsConfiguration` (dnsapi 를 거쳐 `NsiGetAllParameters`). 반환 0 · **`0x490` (항목 없음) · `0x32` (모듈 없음)** 은 정상으로 받는다.
- **`GetAddrInfoW` · `getaddrinfo` · `gethostbyname`** — ws2_32 는 이 길에서 NSI 를 **안 부른다**. NSI 는 dnsapi `Query_PrivateExW +0x60b` → `iphlpapi!GetCurrentThreadCompartmentId +0x3f` → `NsiGetParameter(1, NDIS, 8, 키 없음, 묶음 0 (RW), 4 B, 오프셋 0)` (`0x120007` · `0x50`) **하나**. 실패하면 번호 1 → 삼킴.
  - `GetAddrInfoW` → … → mswsock `SaBlob_Query +0x186` → `DnsQueryEx` 사이는 안 봤다 `[공백]`.
  - 이름 풀이의 진짜 막힘은 NSI 가 아니라 `Rpc_ResolverQuery` (`+0x851` · `+0x90d`) 쪽일 수 있다 (범위 밖).
- **F3 — webio 이름 풀이 길 (참고, 처음엔 안 탐)** — webio `WapTcpStartDnsQuery` → `WaStartDnsQuery` → … → `WapDnsResolveGeneric +0x1ef` → `ws2_32!GetAddrInfoExW`. `WaStartDnsQuery` → `WapDnsResolveGeneric` 사이는 안 봤다 `[공백]`. 이 길의 NSI 도 위 `GetCurrentThreadCompartmentId` 하나다.
  - `WapTcpStartDnsQuery +0x28f` → `WaMapInterfaceToAddressFamily` → `GetIpInterfaceEntry` 는 **기본 설정에선 안 탄다** — 인터페이스 지정 (`[연결+0x165]` ≠ 0) · 계열 미지정 (`[+0x3d8]` == 0) · 인터페이스 번호 (`[+0x140]` ≠ 0) 셋이 다 맞아야 한다 (`+0x265 ~ +0x286`). 오류여도 로그만 남긴다 (`+0x294`).
- **`GetAdaptersInfo`** — `+0x24` → `AllocateAndGetAdaptersInfo +0x84 NsiAllocateAndGetTable` (NDIS 객체 1) → `+0xb8 GetCurrentThreadCompartmentId` → 어댑터마다 컴파트먼트 거르기 (`+0xe6`) · **`IfType == 0x18` (루프백) 건너뜀** (`+0xf7`) → `+0x151 NsiGetAllParameters(1, IPv4, 객체 7, 키 LUID, 크기 모두 0)` = "그 LUID 의 IPv4 인터페이스가 **있나만**".
  - → **루프백만 있는 PC = `0xe8`** (표 0 개여도 같음). 첫 `NsiAllocateAndGetTable` 오류는 그대로 돌려준다.

### 2.15 모듈 ID 값 `[정적]`

run 문서 11절. IPHLPAPI `.rdata`. 24 B = `{DWORD 0x18, DWORD 1, GUID}`.

| PDB 이름 | rva | GUID |
|---|---|---|
| `NPI_MS_NDIS_MODULEID` | `0x25350` | `eb004a11-9b1a-11d4-9123-0050047759bc` |
| `NPI_MS_IPV6_MODULEID` | `0x25368` | `eb004a01-9b1a-11d4-9123-0050047759bc` |
| `NPI_MS_IPV4_MODULEID` | `0x25380` | `eb004a00-9b1a-11d4-9123-0050047759bc` |

- 세 GUID 는 맨 앞 칸 (`eb004a11` · `eb004a01` · `eb004a00`) 만 다르다. 구조체 `+0x10` 이 이 24 B 를 가리키니 (2.9), 이걸로 "어느 부서 장부" 인지 갈린다.
- 그 밖 (`NPI_MS_TCP_MODULEID` 등) 은 PDB 이름만 확인, 값은 안 읽었다.

### 2.16 flow-writer 대조 · run124 고쳐 읽기

run 문서 "flow-writer 대조" · "정정". **이전 run 정정은 없다.** 결과서와 다르게 적은 곳은 뜻이 같다.

1. 결과서 F1 표 9 단계는 `NotifyIpInterfaceChange` → `InternalRegisterChangeNotification` 만 적었다. `NotifyUnicastIpAddressChange` 는 `_calls.tsv` 로 보면 `+0x19 NetioRegisterAddressChangeNotification` → `+0x9d InternalRegisterChangeNotification` — **한 단계 더** 거친다.
2. 결과서는 `InternalRegisterChangeNotification +0x9d` · `+0xd5 winnsi!NsiRpcRegisterChangeNotification` 을 안 적었다 (`_calls.tsv` 엔 있음). 이름만 두고 → run129.
3. "연결 핸들 NULL 이면 6" 을 `.asm` `+0x66 TEST RAX,RAX / +0x69 JNZ / +0x6b LEA EAX,[RBP+0x6]` (RBP = 0) 으로 맞췄다.
4. `alias:X+1` 표시는 같은 함수의 C++ 장식 이름 하나 — `pdb_aliases.py --lookup` 결과 **모두 별칭 무리 아님**. PDB 이름을 그대로 썼다.

**run124 — "정정" 이 아니라 "일부 닫힘"** (run124 문서 맨 위에 안내 한 줄):

- 9절 "`0x70` 의 `+0x28 ~ +0x60` 은 네 쌍처럼 보인다 `[추측]`" → 칸 순서 **`[정적]`** (2.9). 네 쌍의 **이름** (키 · RW · ROD · ROS) 만 `[추측]`.
  - run124 글은 `NsiAllocateAndGetTable` 의 인자 번호로 칸을 불렀다 ("`+0x10` 둘째 인자 포인터, `+0x18` 셋째 인자"). 그 함수에서 둘째 인자 = 모듈 ID, 셋째 = 객체라 **어긋나지 않는다**.
- "PE_emulator 쪽" nsi 줄 "개수 0 이어도 최소 10칸 잡고 둘째 호출 `[추측]`" → **`[정적]`** (2.10).
- "아직 확인 필요" 정적 5 → 칸 뜻 · 부르는 쪽 **닫힘**, 커널 `nsi.sys` 개수 0 대답은 **남음** (→ Q237 ③).
- [run124 해설](run124-afd-socket-해설.md) 맨 위에도 "고쳐 읽기 (run126 · run128)" 한 줄을 붙였다.

### 2.17 정리 — 비유로 한 번 더

- 손님 (프로그램) 이 웹 편지 담당 (winhttp) 에게 "편지 한 통 보내 줘요" (`WinHttpSendRequest`).
- 담당은 먼저 **작업실 (webio) 문** 을 연다 (F1). 처음 한 번만이다.
  - 문 열 때 "길 바뀌면 알려 주세요" 신청을 **전화 (winnsi RPC)** 로 한다. 전화가 안 되면 문을 못 열고, 편지 보내기는 **여기서 0** 으로 끝난다.
- 문이 열리면 담당은 비서 (IPHLPAPI) 에게 "우리 길 이름표 봐 와" 를 시킨다 (F2, 지도 판 번호가 바뀌었을 때).
  - 비서는 안내 데스크 (`\\.\Nsi`) 에 질문지 넷을 낸다: "우리 구역 번호?" (`0x12000f`) → "어댑터 장부 통째" · "IPv4 장부" · "IPv6 장부" (`0x12001b` ×3, 몇 줄인지 먼저 묻고 의자 놓기).
  - 질문지 셋은 **같은 양식** — 부서 (모듈 ID) · 장부 번호 (객체) · 줄 번호 (키) · 설정 / 상태 / 고정 칸.
  - 데스크가 "장부가 비었어요" 하면 비서는 `0xe8`, 담당은 "이름 없음" 이라 적고 **그냥 간다.**
- 그러니 에뮬레이터가 데스크에서 할 일은 "정직하게 빈 장부" 면 충분하다. 진짜 관문은 **전화** 쪽이고, 그건 다음 바퀴 (run129) 이야기다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (docs 커밋 `0f65887`)

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 | winhttp · webio · iphlpapi · ws2_32 · dnsapi 없음 | **PDB 다섯 한 줄** (GUID+age · 함수 수 3305 · 2103 · 642 · 1194 · 3405, 공개 심볼이라 타입 · IOCTL 이름 없음, `NPI_MS_*_MODULEID` 만 있음) |
| 기준 — 근거 줄 | — | **(run128) 정적 바퀴, 사진 값 대조 없음** (test.exe 가 여섯 모듈 안 올림 `[관찰]` · API 없음 `[소스]`, 요청 쪽 값은 에뮬레이터 로그라 `[관찰]` 아님) |
| 1장 그림 | — | **안 바뀜** (범위 밖) |
| 3장 run124 "소켓 장치 AFD" 절 nsi 줄 | "… → nsiproxy → 커널 NSI 모듈." · "nsi 부르는 쪽 (sast/126)" | "칸 뜻 · 부르는 쪽은 → run128 (아래 'NSI 부르는 쪽' 절)" · "(sast/126 → **run128**)" |
| 3장 끝 | 완료 포트 묶기 절 (run127) 까지 | **새 절 "NSI 부르는 쪽"** `[정적]` — 비서 · 전화 비유, F1 · F2 트리, 일곱 줄 요약, 칸 표 · PE_emulator 표는 run 문서로 링크 |
| 4장 run124 줄 셋 | 동적 ⑦ · 정적 ⑤ "sast/126 이 일부" · PE_emulator "nsi 장치 모름" | ⑦ "→ run128 동적 2 와 같은 몫 (Q222)" · ⑤ "**일부 해결** — `0x70` 칸 순서 `[정적]`, 남음: `nsi.sys` 개수 0" · PE_emulator "→ run128" |
| 4장 run125 · run126 · run127 줄 | "대기열 번호는 오케스트레이터가 붙임" | **Q226 · Q227 · Q228** (run125) · **Q233 · Q234** (run126) · **Q235 · Q236** (run127) 를 적음 |
| 4장 새 줄 | — | **"3 (범위 밖 — NSI)" 넷**: 해결됨 (Q221) · 동적 1 ~ 8 (Q222) · 정적 ① ~ ⑥ · PE_emulator 쪽 |
| 5장 근거 run | — | **run128 줄** 둘 (근거 run 표 · 03_sprint 요청서 번호 표) |

- 태그 변화:
  - run124 의 `0x70` 칸 순서 · "최소 10칸" — `[추측]` → **`[정적]`**.
  - 새로 생긴 것은 `[정적]`, 그리고 **`[공백]` 셋** ("`0x12000f` 가 보인 호출의 0 반환 원인", "`NsiConnectToServer` 가 서버를 만나나", "첫 보냄에서 `+0x37c` 를 타나").
- 짐 지도 (`baggage.md`) 는 안 바뀌었다 — 초기화 동안의 짐이 아니다.

### PE_emulator 쪽 — 무엇을 고치면 되나 (우리 대기열 아님)

run 문서 "PE_emulator 쪽" (결과서 "지금 우리 맞나"). `../PE_emulator` 코드는 flow-writer 가 직접 안 읽었다. 고칠지는 **사용자 결정** 이다.

- **열기 (`nt_create_file.cpp`) — 틀림.** 이름 분류에 `\??\Nsi` 가 없어 `path_prefix_unsupported` → 로그의 "핸들 −1".
  - 해야 할 일: 이름 통째 (대소문자 무시) `\??\NSI` (nt 쪽 `\Device\Nsi`) 를 afd 곁에 nsi 로. EA 없음. 성공이면 **진짜 핸들** + **비동기 핸들** (`FILE_FLAG_OVERLAPPED` → 동기 옵션 없음 `[추측: kernelbase CreateFile 변환은 안 봄]`).
- **IOCTL (`nt_device_io_control_file.cpp`) — 틀림 (몸 없음).** 코드 셋이 없고, 비동기 핸들이면 `device_async_handle_unsupported` (`:521`), 이벤트도 afd 만.
  - 해야 할 일: ① 장치 종류 nsi (afd 같은 이벤트 처리) ② 입력 길이 `0x68` / `0x50` / `0x70` 미만 → `0xC000000D` ③ 구조체 안의 **사용자 포인터를 따라가 쓰기** ④ 동기 완료 (status 0) 면 기다림 불필요 ⑤ `0x12000f` 의 `+0x40` 은 **건드리지 말 것**.
- **대답 정책** (루프백만 · 진짜 망 안 나감): 칸 뜻은 2.9 · 2.11 — "칸 뜻을 아는 정책 값 + `TODO(VM)`". 가장 단순 = 2.13 (a) 어댑터 0 개 (`{1,0}` · 개수 0 · 0 · 0).
- **F1 은 NSI 장치로 못 고친다** → winnsi RPC (run129).
- **`tests/file.cpp` — 없음.** (1) `\??\Nsi` 열기 → 핸들 ≠ −1 (2) `0x12000f` NDIS 객체 8 → RW `{1, 0}` (3) `0x12001b` 포인터 0 → 개수 `+0x68` (4) 크기 < `0x68` → `0xC000000D`.

### 큰 그림에서 어떻게 이어지나

- 흐름도 본선 (프로세스 생성 → `NtContinue` 은퇴 → `main`) 에는 **변화가 없다**. 이번도 `main` 뒤 옆길의 지도다.
- 이번 결과가 가리키는 다음 자리는 **전화 쪽** 이다.
  - 안내 데스크 (NSI 장치) 는 "빈 장부" 로 정직하게 답하면 된다는 걸 알았다.
  - `WinHttpSendRequest` 를 진짜로 막을 수 있는 건 winnsi RPC (알림 등록) 다 → **run129 (Q223, sast/127)** 이 바로 이어 읽는다.
- 소켓 쪽 앞 바퀴들과도 이어진다.
  - run124 (소켓 열기 · nsi 세 주문서), run126 (afd 주소 목록 — "망 없는 PC 에 남는 주소는 NSI 몫"), run127 (완료 포트). 동적은 Q222 · Q224 · Q233 · Q235 를 **같은 VM 세션** 에 얹을 수 있다 (plan.md).

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q222** (동적)

- **왜 이번에 못 알았나**: 정적만 했다. 칸 **값** 은 공급자 (ndis · tcpip) 가 정하고, "어느 줄을 탔나" 는 실행 때 값 (세대값 · 핸들) 에 달렸다. 그리고 test.exe 가 아니라 **winhttp · iphlpapi · dnsapi 를 쓰는 프로그램 + 랜선 뽑은 VM** 이 필요하다 — 고르기는 **사용자 결정**.
- 무엇을 보나 (run 문서 "아직 확인 필요" 동적 1 ~ 8, 호출마다 `dq <구조체> L0xd` + IOSB):
  1. ★ **`WinHttpSendRequest` 가 NSI 앞에서 끝나나 (F1)** — `winhttp!WinHttpSendRequest+0x25f` · `+0x2ff` `eax`, `WINHTTP_DLL::GetWebIOApiHandle+0x44`, webio `WapInitializeNetworkChange+0xa5` · `Startup+0xb4` / `+0xf1`, `NotifyIpInterfaceChange` · `winnsi!NsiConnectToServer` 반환. (2.7 의 `[공백]` 을 닫는 핵심.)
  2. `GetAdaptersAddresses(0, 0x80f)` 의 NSI 넷 + 반환 — `iphlpapi!AllocateAndGetAdaptersAddresses+0xc8` / `+0x166` / `+0x1d0` / `+0x23d`, 루프백 줄 칸 값 + 개수.
  3. `iphlpapi!GetBestInterfaceEx+0xeb` — 진짜 오류 번호 (`0x4d3` / `0x490`) 와 키.
  4. 어댑터 0 개 · 루프백 1 개일 때 `WinHttpSendRequest` 최종 반환 · `GetLastError`.
  5. `dnsapi!Query_PrivateExW+0x612` — compartment 묻기와 `Rpc_ResolverQuery` ret.
  6. `iphlpapi!AllocateAndGetAdaptersInfo+0x8b` / `+0x158` — 루프백 건너뜀 (`+0xf7`).
  7. `nsi!NsiGetAllParameters` `+0x139` 비교 통과 (RW 크기 칸이 8 로 남나).
  8. (flow-writer 보탬) `iphlpapi!InternalRegisterChangeNotification+0x63` `rax` (연결 핸들 — NULL 이면 F1 의 6), `CheckProxySettings` `+0x85` · `+0x91` 두 값으로 첫 보냄에서 `+0x37c` 를 타나. 1 과 같은 실행에서.
- run124 동적 ⑦ (`0x12001b` 첫 호출 `0x70` · `+0x68`) 은 위 2 와 같은 몫이다.

### winnsi RPC 쪽 → **Q223** (정적, 결과서 있음) → **run129 예정**

- 무엇: `NsiConnectToServer` 가 실제로 무엇을 하나 (끝점 이름, 바인딩만인가 호출인가), `NsiRpcRegisterChangeNotification`.
- 왜 이번에 못 했나: 이번 요청은 NSI **장치** 를 부르는 쪽까지였다. winnsi 는 RPC 라 다른 모듈 (winnsi · rpcrt4) 을 읽어야 한다. 이미 sast/127 로 돌았고, 문서는 다음 바퀴.

### 나머지 정적 → **Q237**

| # | 무엇 | 왜 이번에 못 했나 |
|---|---|---|
| ② | **이름 풀이 길** — webio `WaStartDnsQuery` → `WapDnsResolveGeneric` → `GetAddrInfoExW` → mswsock `SaBlob_Query` → `DnsQueryEx` → `Query_PrivateExW` → `Rpc_ResolverQuery` | 범위가 "NSI 묻기 · 읽기까지" 였다. 사이 두 곳이 `[공백]` (2.14) |
| ③ | **커널 `nsi.sys` · `ndis.sys` · `tcpip.sys`** 가 객체 1 · 7 · 8 · 0 칸을 채우는 줄, 개수 0 일 때 status (run124 정적 5 남은 몫) | 공급자 본문은 이름만으로 정했다. 루프백 줄 값이 정적으로 나오나도 여기서 |
| ④ | `AddDhcpConfiguration` (dhcpcsvc RPC) · `AddNetbtConfiguration` (NetBT 장치) | 어댑터 1 개 이상일 때만 타는 줄. NSI 가 아니라 범위 밖 (2.4) |
| ⑤ | `CheckProxySettings` 가 첫 `WinHttpSendRequest` 에서 `GetActiveConnectionName` 을 타는 조건 · `SetProxySettings` 가 언제 불리나 | 세대값 · 가상 함수 호출이라 정적으로 못 닫음 (2.4 5 · 5') |
| ⑥ | (낮음) kernelbase `CreateFileW` 가 `\\.\Nsi` → `\??\Nsi`, `FILE_FLAG_OVERLAPPED` → CreateOptions 로 옮기는 줄 | kernelbase 변환은 안 봤다. PE_emulator 표의 `[추측]` 닫기 |

- ① 은 = Q223 (위).

### 대기열 밖 — 이름 · 짐작으로 남은 것

- 칸 묶음 이름 키 · RW · ROD · ROS, `0x50` 의 "2 = ROS", 첫 인자 1 = "저장소" — `[추측]`.
- 플래그 `0x80f` 의 SDK 상수 이름 — `[추측]`.
- DOS `0x32` 가 되는 NTSTATUS = `STATUS_NOT_SUPPORTED` — `[추측]`.
- 7절 칸 이름 (`IP_ADAPTER_ADDRESSES` 칸 순서와 맞춤) — `[추측]`, 값은 `TODO(VM)`.
- "같은 `0x12000f` 가 한 번 더" = 두 호출 자리 · "`0x12001b` 는 다른 호출자" — `[추측]` (2.7 · 2.14).

---

## 5. 더 읽기

- run 문서: [`run128-nsi-callers.md`](run128-nsi-callers.md)
  - 호출 지점 표 전체 · 칸 표 셋 · 7절 칸 목록 · PE_emulator 표 → 그 문서 "결과" 1 ~ 11절
- 결과서: [`_workspace/sast/126_nsi-callers_result.md`](../../_workspace/sast/126_nsi-callers_result.md) — "먼저 읽는 한 줄 답", A 절 (`WinHttpSendRequest` 길), B 절 (나머지 API 넷), 4 절 (모듈 ID)
- 요청서: [`_workspace/sast/126_nsi-callers.md`](../../_workspace/sast/126_nsi-callers.md)
- flow.md: [`flow.md`](flow.md) — 3장 끝 "NSI 부르는 쪽" 절 · 3장 "소켓 장치 AFD" 절 nsi 줄 · [4장 공백 목록](flow.md#4-공백-목록-다음에-채울-곳) "3 (범위 밖 — NSI)" 줄 · [5장 근거 run](flow.md#5-근거-run) run128 줄
- 같은 꼴 앞 바퀴: [run124 해설](run124-afd-socket-해설.md) (2.16 nsi 세 주문서 · 길이 먼저 묻기), [run126 해설](run126-afd-addrlist-해설.md) (2.8 컴파트먼트 · "0 개는 정상"), [run127 해설](run127-file-completion-해설.md), [run99 해설](run99-rpc-caller-해설.md) (RPC = 전화 부서)
- 대기열: `_workspace/plan.md` Q221 (끝) · Q222 (동적) · Q223 (winnsi, run129 예정) · Q237 (정적)

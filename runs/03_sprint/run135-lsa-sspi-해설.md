# run135 lsa-sspi — 해설

> 이 글은 [run135 문서](run135-lsa-sspi.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q249** — PE_emulator 요청 "`GetUserName*` · `InternetOpen*` 이 LSA 쪽에 거는 RPC" (`_workspace/plan.md`, 이번에 **끝 (run135)**)
  - 원 요청 `309_internals-lsa-sspi.md`, 튜닝 `309_tune12.md` **V4**, 병목 49 · `GetUserName*` 0 반환 198 · `InternetOpen*` NULL 60 (요청 쪽 에뮬레이터 숫자).
  - 사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 셋째.
  - 결과서 판정 **분석성공** — 질문 1 · 2 는 코드 줄로, 질문 3 은 클라이언트 형식 문자열 + 서버 (`sspisrv` · `lsasrv`) 코드까지 읽어 닫음.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q254** (`GetUserNameW` · `InternetOpenW` 한 번씩 — 포트 · bind 마스크 · 프로시저 0 · 15 바이트)
    - 정적 → **Q255** (결과서 다음 요청 후보 1 ~ 4)
- 근거:
  - run 문서 [`run135-lsa-sspi.md`](run135-lsa-sspi.md) (가장 중요)
  - 요청서 [`_workspace/sast/133_lsa-sspi.md`](../../_workspace/sast/133_lsa-sspi.md) · 결과서 [`133_lsa-sspi_result.md`](../../_workspace/sast/133_lsa-sspi_result.md)
    - **처음 analyze 다섯**: advapi32 · sspicli · wininet · sspisrv · lsasrv (한 번에 하나). secur32 는 PE 내보내기 표만 봤다. rpcrt4 는 다시 analyze 안 하고 `^이름$` export 만.
    - 스크립트 문제 없음. `.asm` 오프셋을 원본 바이트 (`pefile`) 와 36곳 맞춰 봄 — **일치**.
  - flow.md 변경: docs 커밋 `d4f0df4` (`git show d4f0df4 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 모듈들을 안 올린다.**
   - 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) 에 advapi32 · sspicli · wininet · rpcrt4 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 · run133 과 같은 줄).
   - `sample/*.c` · `log.h` 에 `GetUserName` · `InternetOpen` 같은 글자가 없다 `[소스]`.
   - 그래서 이번 이야기도 "**이 API 를 쓰는 다른 프로그램이라면**" 이다. 그림 · 짐 지도는 그대로다.
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - 198 · 60 · "Ex 연결 넷씩", "이벤트 열기 성공", `nt_alpc_create_port.cpp` 줄 번호는 모두 PE_emulator 쪽 이야기다. `[관찰]` 이 아니다.
3. **오프셋 읽는 법은 run133 해설과 같다.**
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문 "`+0x1a7 NdrClientCall3`" 처럼 부르는 함수 이름 **앞** 숫자 = **call 자리**.
   - "`IsOkayToExec +0xf`" 처럼 함수 이름 **뒤** 숫자 = 그 함수 안 자리 (줄).
   - "응답 `+0x08`" 처럼 칸 이름과 붙은 숫자 = **메시지 (스텁) 안 칸 자리**.
   - 콜스택으로 본 적은 없다.
4. **서버 쪽 (lsass) 코드도 읽었다.**
   - `sspisrv.dll` · `lsasrv.dll` 은 **test.exe 쪽이 아니라 lsass 프로세스** 가 올리는 DLL 이다. "서버가 이렇게 답한다" 는 이 코드에서 읽은 것이다.
5. **NDR 바이트 꼴은 "규칙으로 끌어낸 것" 이다.**
   - 형식 문자열은 PE 에서 직접 읽었지만 (`[정적: PE]`), 그걸 바이트로 펼친 건 NDR 2.0 규칙을 적용한 결과다 — run 문서는 `[정적 + 규칙]` 으로 둔다. rpcrt4 마샬러 본문은 안 읽었다 (Q254 ③ 에서 확인).

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- 이벤트 = **신호등** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md)
- 지연 가져오기 = "**필요하면 그때 부를게요**" 메모 → [run25 해설](../02_sprint/run25-dll-entry-해설.md) 2.5
- `SetLastError` = 스레드의 "**마지막 오류 번호**" 칸에 적는 **고장 메모** → [run91 해설](run91-bcrypt-init-해설.md) 2.6
- lsass = Windows 의 로그인 · 보안 담당 프로세스 → [run91 해설](run91-bcrypt-init-해설.md) 2.8 "새 말 — lsass"
- "한 번만" 팻말 (`InitOnce` 류) → [run62 해설 2.7](run62-tp-etw-init-해설.md)
- 필터 = 대피도에서 "이 사고를 여기서 잡을까?" 를 정하는 짧은 함수 → [run82 해설 2.7](run82-exception-user-해설.md)
- 토큰 가장 → [run24 해설 2.5](../02_sprint/run24-work-queue-해설.md)
- RPC 런타임 (rpcrt4) = 가게 안 **전화 부서**, `gDefaultSecurityDescriptor` = 전화 부서의 **기본 명부**, `\RPC Control` SD 두 번 묻기 → [run99 해설](run99-rpc-caller-해설.md) "한 줄로 먼저" · 2.1 · 2.2
- ALPC 포트 = 프로세스끼리 거는 **전화선 끝** → [run68 해설 2.4](run68-dll-init-rest-해설.md)
- **run129 에서 푼 것** → [run129 해설](run129-winnsi-rpc-해설.md)
  - 바인딩 = **연락 카드**, 끝점 = 상대 사무실의 **직통 번호**, `ncalrpc` = "같은 건물 내선" → 2.4
  - 인터페이스 = 사무실 **이름표** (UUID + 판 번호), 프로시저 번호 = **메뉴 번호** → 2.5
  - 컨텍스트 핸들 = **접수 번호표** → 2.5
  - epmapper = **114 안내** → 2.7
  - `AlpcConnect` 상태 → RPC 오류 표 (`0xc0000034` → 1722), 들어오는 속성 = **빈 봉투**, plain / Ex → 2.8
  - "그런 번호 없어요" (`0xc0000034`) 로 답하면 깨끗하게 실패 → 2.12
- **run133 에서 푼 것** → [run133 해설](run133-lrpc-wire-해설.md)
  - bind = **첫인사**, 전송 문법 (NDR 2.0 · NDR64) = **말투**, 제안 · 수락 마스크 → 2.5 · 2.6
  - 스텁 데이터 = 본론 **내용물** (요청 `+0x68`, 응답 `+0x40` 부터) → 2.7
  - 동기 길 (`Flags 0x20000`, 같은 버퍼) → 2.3 "동기 길은?"
  - 다시 시도 종류 (첫 시도 + 다시 3) → 2.10

이번에 처음 나오는 말 — **SSPI · sspicli**, **lsarpc 창구 · lsasspirpc 창구**, **이름 형식 (`NameSamCompatible`, 비트 `0x10000`)**, **동기 스텁 `NdrClientCall3`**, **LSA 준비 신호등 (`LSA_AUTHENTICATION_INITIALIZED`)**, **`DllState` 깃발**, **RPC 실패는 "예외" 로 온다**, **`CLIENT_ID`**, **`RPC_UNICODE_STRING` · 유일 포인터 · 적합 가변 배열** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 핸들 = **손잡이**, 이벤트 = **신호등**.
- run99 · run129 · run133 의 전화 그림 그대로:
  - rpcrt4 = 가게 안 **전화 부서**, ALPC 포트 = **전화선 끝**.
  - 바인딩 = **연락 카드**, 끝점 = **직통 번호**, epmapper = **114 안내**.
  - bind = 전화마다 맨 처음 하는 **첫인사**, 프로시저 번호 = **메뉴 번호**, 컨텍스트 핸들 = **접수 번호표**.
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 따름 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - lsass = 같은 건물의 **보안 사무소**. 이 사무소엔 창구가 여럿이다. 이번 두 API 가 가는 곳은 **SSPI 창구** (`lsasspirpc`) 다.
  - `GetUserName` = 보안 사무소에 "**나 누구예요?**" 를 전화로 묻는 일.
  - 이 사무소의 직통 번호는 **수첩에 이미 적혀 있다**. 그래서 114 를 안 거친다.
  - 처음 한 번은 "저 이 동네 새로 왔어요" 하고 **접수 번호표** 를 받는다 (프로시저 0). 그다음부턴 번호표만 내밀고 "제 이름이요" 만 묻는다 (프로시저 15).
    - run 문서는 이 번호표를 "**회원 카드**" 라 불렀다. 이 글은 run129 · run133 해설과 맞춰 **접수 번호표** 로 부른다. 같은 것이다.
  - wininet = **인터넷 담당**. 인터넷을 처음 열 때 "지금 쓰는 사람 이름" 을 **수첩에 한 번 적어 두려고** 똑같은 전화를 건다. 그 전화가 안 되면 인터넷 열기 자체를 포기한다.

### 한 줄로 먼저 — 무엇을 얻었나

> **`GetUserName*` 도 `InternetOpen*` 도 결국 같은 창구로 간다. lsass 의 SSPI 전용 창구 `\RPC Control\lsasspirpc` 다. 공개 명세 (MS-LSAD · MS-LSAT) 가 다루는 `lsarpc` 가 아니다.**
> **전화는 sspicli 가 건다. 처음 한 번 메뉴 0 (연결 — 접수 번호표 받기), 그다음부터는 메뉴 15 (이름 묻기) 만.**
> **wininet 은 보안 꾸러미를 묻는 게 아니다. "현재 사용자 이름" 을 한 번 적어 두려고 `GetUserNameExA(2)` 를 부른다 — 요청서의 짐작은 틀렸다.**
> **연결이 안 되면 둘 다 LastError 1722 로 끝난다. 다른 길로 물러서지 않는다. 그리고 `InternetOpen*` 은 "다 됐음" 표시를 못 세워서, 부를 때마다 처음부터 다시 해 본다.**
> **에뮬레이터가 막힌 직접 이유는 `NtAlpcConnectPortEx` 줄이다 — 창구 검사를 안 하고, 빈 봉투 때문에 늘 "진짜 서버 필요" 로 보고, 서버 보안 요구가 있으면 멈춘다.**

- 첫째 · 둘째 · 셋째 줄은 `[정적]`. 단 포트 이름 앞머리 `\RPC Control\` 는 rpcrt4 규칙을 적용한 것이라 `[정적 + 규칙]` (2.3).
- 넷째 줄의 "1722 · 물러서지 않음 · 성공 표시 자리" 는 `[정적]`. "`InitOnceExecuteOnce` 가 실패 뒤 다시 콜백을 부르나" 는 안 읽었다 `[추측]` (2.9).
- 다섯째 줄은 결과서가 PE_emulator 코드를 읽고 내린 판정이다. flow-writer 는 그 코드를 안 읽었다 (2.11).

요청서 질문에 대한 답 (run 문서 "요약" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| 1 사슬 · 어디서 RPC 를 부르나 | `advapi32!GetUserNameW` → [지연] **`sspicli!GetUserNameExW`** → `SspipGetUserName` → `NdrClientCall3`. advapi32 · sechost 는 안 낀다 `[정적]` | 2.1 · 2.3 |
| 1 바인딩 | `ncalrpc` + **`lsasspirpc`**, 인증 · QOS 설정 없음. 인터페이스 **`4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0**, **동기** `[정적]` | 2.3 · 2.4 |
| 1 프로시저 | **0 = 연결** (프로세스당 한 번), **15 = 이름 묻기** `[정적]` | 2.5 |
| 1 연결 인자 6 · `\RPC Control` SD | sspicli 가 아니라 rpcrt4 전역 **`gDefaultSecurityDescriptor`**. SD 묻기는 run99 초기화 줄 `[정적]` | 2.6 |
| 1 실패 값 · 물러서기 | `0xc0000034` ×4 → `0x6ba` → 예외 → `0xc0020017` → **LastError 1722**, FALSE. **물러서는 길 없음** `[정적]` | 2.7 |
| 2 wininet → LSA | 꾸러미 묻기 **아님**. `GlobalDataInitialize +0x1076` → … → `_InitOnceGlobalUserName +0x58 GetUserNameExA(2)` `[정적]` | 2.8 |
| 2 왜 NULL · 같은 창구인가 | 이름을 못 얻으면 전역 초기화 실패 → NULL + 1722. **같은 창구** (형식 칸만 2) `[정적]` | 2.8 · 2.9 |
| 3 서버 최소 답 | bind (마스크 1) → 프로시저 0 응답 32 B → 프로시저 15 응답 `28 + Length` B. 이름: 형식에 `0x10000` 있으면 `user`, 없으면 `DOMAIN\user` `[정적 + 규칙]` | 2.10 |
| (PE_emulator) | `lsarpc` 창구는 이 길이 아님. **`NtAlpcConnectPortEx` 줄이 막힘의 직접 원인** (결과서 판정) | 2.11 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run129 · run133 과 같은 자리, **초기화 밖** 이다. `main` 뒤 프로그램 코드가 "나 누구?" 를 묻거나 인터넷을 열 때의 옆길이다. 전화 부서 (rpcrt4) 의 대화 꼴은 run133 이 이미 열었고, 이번엔 **그 전화를 보안 사무소 (lsass) 에 거는 길** 을 연다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll: LdrInitializeThunk → LdrpInitializeProcess → DLL 들이기 · 전원 켜기
 └─ NtContinue (은퇴) → exe 진입점 → (CRT 점선 상자) → main
                                                        │
      test.exe 는 여기서 이 API 를 안 쓴다 [소스]         │ 쓰는 프로그램이라면 (요청 쪽 에뮬레이터 로그):
                                                        ├─ GetUserNameW ─────────────┐
                                                        └─ InternetOpenW              │
                                                             └─ (이름 한 번 적어 두기) ┤
                                                                                       └─ sspicli → 전화 부서 rpcrt4
                                                                                            └─ lsass SSPI 창구 (lsasspirpc)   ← 이번
                                                                                                 메뉴 0 (연결) → 메뉴 15 (이름)
```

### 왜 이 질문이 지금 왔나

- PE_emulator 에서 `GetUserName*` 이 0, `InternetOpen*` 이 NULL 로 끝나는 샘플이 많다 (198 · 60, 요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님).
- 두 길 다 `NtAlpcConnectPortEx` 를 **넷씩** 받고, 에뮬레이터가 `0xc0000034` 로 답해서 실패한다.
- 그런데 **포트 이름이 그쪽 로그에 안 찍혀** 서, 어느 창구에 거는 전화인지부터 몰랐다.
  - 공개 명세 (MS-LSAD · MS-LSAT) 는 `lsarpc` 하나만 다룬다. 이 두 길이 그것인지, 다른 창구인지가 첫 물음이었다.
- 에뮬레이터 쪽엔 이미 `lsarpc` 창구 흉내가 있다. 그런데도 막히니, "창구가 틀렸나, 연결 줄이 틀렸나" 를 가려야 했다.

### 무엇이 궁금했나

1. **`GetUserNameW` 길** — 사슬 (DLL 경계 포함), 바인딩 (포트 이름 · UUID · 버전 · 프로시저 · 동기 / 비동기), 연결 인자 6 이 어디서 오나, `\RPC Control` SD 묻기가 어느 줄인가, 실패가 어떤 값을 거쳐 0 이 되나 · 물러서는 길이 있나.
2. **`InternetOpenW` → LSA** — LSA 준비 이벤트 · 연결이 왜 일어나나 (보안 꾸러미 묻기인가), 실패하면 왜 NULL 인가, 1 과 같은 창구인가.
3. **한 번 성공시키는 서버 최소 답** — bind 뒤 메뉴 차례, 클라이언트가 응답에서 읽는 칸.

### 바퀴 전에 알던 것

- `[정적]` (run99) rpcrt4 가 처음 RPC 를 쓸 때 `\RPC Control` 을 열고 SD 를 두 번 물어 **기본 명부** (`gDefaultSecurityDescriptor`) 를 만든다. 실패하면 RPC 런타임 전체가 오류 14.
- `[정적]` (run129) `AlpcConnect` 의 상태 → RPC 오류 표 — `0xc0000034` → `0x6ba` (1722). 기본 명부가 있으면 `NtAlpcConnectPortEx` (Ex) 로 간다. 들어오는 속성은 늘 **빈 봉투**.
- `[정적]` (run133) 첫인사 · 본론 · 답장 머리, 재시도 (첫 시도 + 다시 3).
- `[추측]` (run133) 동기 길 (`Flags 0x20000`, 같은 버퍼) 은 "LSA 같은 다른 클라이언트가 쓸 것".
- `[추측]` (요청서 · 원문) `GetUserNameW` → sspicli `GetUserNameExW` 로 보임. `InternetOpenW` 는 보안 꾸러미를 묻는 것 (`EnumerateSecurityPackages` 류) 같음.
- 몰랐던 것: 포트 이름, 인터페이스, 메뉴 번호, 동기인지, 실패 값이 어디서 1722 로 바뀌나, wininet 이 왜 LSA 에 가나.

---

## 2. 찾은 것

### 2.1 한눈에 — 두 입구, 한 길, 한 창구 `[정적]`

run 문서 "호출 관계" 에서 대표만 뽑았다. `+0x..` 는 ret 자리, `⇢` 는 간접 · 함수 포인터 호출 (교환원), `⇒` 는 rpcrt4 안을 건너뜀.

```
[입구 둘]
advapi32!GetUserNameW / GetUserNameA                     형식 0x10002
 └─ +0x16  [지연] sspicli!GetUserNameExW (A 는 GetUserNameExA → GetUserNameExW)
wininet!InternetOpenW +0x182 InternalInternetOpen
 └─ +0x39a  GlobalDataInitialize          (GlobalDataInitialized == 0 일 때만)
      └─ +0x107b InitializeGlobalUserName
                  └─ +0x2c InitOnceExecuteOnce ⇢ _InitOnceGlobalUserName
                              └─ +0x5f [지연] sspicli!GetUserNameExA(2) → GetUserNameExW   형식 2

[한 길 — sspicli]
GetUserNameExW
 └─ +0x57  SspipGetUserName
            ├─ +0x5c  IsOkayToExec                 (이미 연결됐으면 바로 끝)
            │          ├─ +0x50  IsSPMgrReady      +0x74 NtOpenEvent · +0x9c NtQueryEvent · +0xae NtClose
            │          └─ +0x59  InitState
            │                     └─ +0x6a CreateRpcConnection
            │                                ├─ +0x2e SecpGetRpcBinding  +0x46 RpcStringBindingComposeW("ncalrpc", "lsasspirpc")
            │                                └─ +0x74 NdrClientCall3  메뉴 0   ⇒ bind · AlpcConnect +0x3c0 NtAlpcConnectPortEx
            ├─ +0xae  NtQueryInformationThread   CLIENT_ID
            ├─ +0x1ae NdrClientCall3  메뉴 15
            └─ +0x1fd RtlCopyUnicodeString

[한 창구 — lsass]
sspisrv!SspiSrvInitialize ├─ +0x8c  RpcServerUseProtseqEpW("ncalrpc", 10, "lsasspirpc", SD)
                          └─ +0x11c RpcServerRegisterIf3(4f32adc8-… v1.0, 콜백 NULL)
SspirConnectRpc  (0)  +0x4d ⇢ lsasrv!SspiExConnectRpc
SspirGetUserName (15) +0x67 ⇢ lsasrv!SspiExGetUserName
```

- sspisrv → lsasrv 의 간접 호출 대상은 `[추측: 칸 오프셋 · 이름이 맞아서]` 이다 (2.12).

### 2.2 한 줄씩 읽는 법

두 줄을 골라 읽어 보자. 하나는 "어디로 거나", 하나는 "무엇을 묻나" 다.

#### 줄 ①: `├─ +0x2e SecpGetRpcBinding  +0x46 RpcStringBindingComposeW("ncalrpc", "lsasspirpc")`

- **누가**: `sspicli!SecpGetRpcBinding` — sspicli 안에서 **연락 카드를 만드는** 함수.
- **어디서**: call `+0x3f`, 돌아올 자리 `+0x46`. 인자는 바로 앞 `+0x1a` · `+0x25` · `+0x2c` 줄에서 채운다.
- **누구를**: rpcrt4 `RpcStringBindingComposeW` — "이 칸들로 연락 카드 글자를 지어 주세요".
- **무엇을 위해**: 카드에 **전화 방식 `ncalrpc` + 직통 번호 `lsasspirpc` + 옵션 `""`** 를 적는다. 객체 UUID · 주소는 NULL.
  - 그 뒤 `+0x5b RpcBindingFromStringBindingW` 로 카드 손잡이를 만들고, `+0x6e RpcStringFreeW` 로 글자를 버린다.
  - 글자 `ncalrpc` · `lsasspirpc` · `""` 는 sspicli PE 의 rva `0x32928` · `0x32938` · `0x32950` 에 그대로 있다 `[정적: PE]`.

#### 줄 ②: `├─ +0x1ae NdrClientCall3  메뉴 15`

- **누가**: `sspicli!SspipGetUserName` — 이름 묻기의 몸통.
- **어디서**: call `+0x1a7`, 돌아올 자리 `+0x1ae`.
- **누구를**: rpcrt4 `NdrClientCall3(sspirpc_ProxyInfo, 15, …)`.
- **무엇을 위해**: 보안 사무소에 **메뉴 15 "제 이름이요"** 를 묻는다. 접수 번호표 · 내 스레드 번호 (`CLIENT_ID`) · 이름 형식을 싸서 보내고, 이름 글자를 받아 온다.

#### 새 말 — 동기 스텁 `NdrClientCall3`

- 인자를 전송 문법 (NDR) 으로 싸서 보내고, **답이 올 때까지 기다렸다가** 풀어 주는 rpcrt4 함수다 `[정적: 이름 · 쓰임]`.
- 비동기 짝은 `NdrAsyncClientCall` 류다. run129 의 winnsi 는 `Ndr64AsyncClientCall` (비동기) 을 썼다.
- 비유: winnsi 는 "끝나면 신호등 켜 주세요" 하고 수화기를 내려놓았다. sspicli 는 **수화기를 들고 답을 기다린다**.

### 2.3 ★ 두 API 가 같은 창구로 간다 — `lsasspirpc`, lsarpc 아님 `[정적]`

run 문서 요약 · 1-1 · 1-2 · 2절. 이번 바퀴의 가장 큰 한 줄이다.

#### 새 말 — SSPI · sspicli

- **SSPI** = 프로그램이 보안 꾸러미 (Kerberos · NTLM · TLS 같은 인증 방식) 를 같은 모양으로 쓰게 해 주는 공통 창구로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- **sspicli** (`sspicli.dll`) = 그 창구의 **손님 쪽 (클라이언트) DLL**. 이번 이야기에서 lsass 에 실제로 전화를 거는 건 이 DLL 이다 `[정적]`.

#### 새 말 — lsarpc 창구 · lsasspirpc 창구

- lsass 는 RPC 창구를 **여럿** 연다.
  - **`lsarpc`** — 공개 명세 MS-LSAD · MS-LSAT 가 다루는 창구. 에뮬레이터의 `lsarpc.cpp` 가 흉내 내는 곳이다 (run 문서는 "`LsaOpenPolicy` 류 몫" 으로 둔다).
  - **`lsasspirpc`** — SSPI 전용 창구. 서버 쪽 `sspisrv!SspiSrvInitialize +0x85` 가 `RpcServerUseProtseqEpW("ncalrpc", 10, "lsasspirpc", SD)` 로 연다 `[정적]`.
- 비유: 보안 사무소 건물 안에 **정책 · 계정 창구** 와 **SSPI 창구** 가 따로 있다. 이번 두 API 는 둘 다 **SSPI 창구** 앞에 줄을 선다.

#### 왜 "같은 창구" 라고 말할 수 있나 — 한 걸음씩

1. **advapi32 는 껍데기다.** `GetUserNameW` (rva `0x30360`, 35 B) 는 인자를 한 칸씩 밀고 형식 `0x10002` 를 넣어 (`+0xa`) `GetUserNameExW` 를 부를 뿐이다 (`+0xf`). 그 `GetUserNameExW` 는 **`SspiCli.dll` 지연 가져오기** 다 (IAT `0x1800b8188`) `[정적]`.
   - `GetUserNameA` 도 같은 꼴로 `GetUserNameExA` (IAT `0x1800b8180`) 를 부른다.
   - 원문 짐작 "`GetUserNameW` → sspicli `GetUserNameExW` 로 보임 `[추측]`" 은 **맞았다** → 이제 `[정적]`.
2. **secur32 도 껍데기다.** secur32 의 `GetUserNameExW/A` · `EnumerateSecurityPackages*` · `QuerySecurityPackageInfo*` · `AcquireCredentialsHandle*` 는 모두 **`SSPICLI.` 로 가는 포워더** 다 — 사슬에 안 낀다 `[정적: PE]`.
3. **wininet 도 sspicli 로 간다.** `_InitOnceGlobalUserName +0x58` 이 부르는 `GetUserNameExA` 도 `SspiCli.dll` 지연 가져오기 (IAT rva `0x28f3a8`) 다 `[정적]`. 그다음 `GetUserNameExA` → `GetUserNameExW` 로 **advapi32 길과 합류** 한다 (2.8).
4. **sspicli 의 카드는 하나뿐이다.** 연락 카드를 만드는 곳은 `SecpGetRpcBinding` 하나, 글자는 `ncalrpc` + `lsasspirpc` 다 (2.2 줄 ①) `[정적]`.
5. **이름표 (인터페이스) 가 클라이언트 · 서버에 똑같이 있다.** **`4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0** — sspicli rva `0x26bb0` (`RPC_CLIENT_INTERFACE`, 길이 `0x60`), sspisrv rva `0x75a0` `[정적: PE]`.
6. → 두 입구 모두 **같은 함수 (`SspipGetUserName`) · 같은 카드 · 같은 이름표 · 같은 메뉴 (0 → 15)** 를 지난다. 다른 점은 둘뿐이다:
   - ① 형식 칸 — advapi32 는 `0x10002`, wininet 은 `2`.
   - ② 이름 자르기 — advapi32 길은 **서버가** `\` 앞을 자르고, wininet 길은 **wininet 이** `\` 뒤만 남긴다.

#### lsarpc 쪽 함수는 따로 있다 — 헷갈리지 말 것

- advapi32 에는 `LsaGetUserName` 이라는 비슷한 이름의 함수가 있다. 이건 `LsapCreateBindingHandleForLocal` (`RpcBindingCreateW` · `RpcBindingBind`) 로 **lsarpc 쪽** 에 가는 **별개 함수** 다 `[정적]`.
- 이번 두 API 의 길에는 안 나온다.

#### 포트 이름은 왜 `[정적 + 규칙]` 인가

- 카드에 적힌 건 `lsasspirpc` 까지다.
- rpcrt4 가 끝점 앞에 `\RPC Control\` 를 붙인다는 건 run129 에서 읽은 **규칙** 이다.
- 그래서 ALPC 포트 이름 **`\RPC Control\lsasspirpc`** 는 "코드 + 규칙" 을 합친 답이다. 최종 `UNICODE_STRING` 을 눈으로 본 건 아니다 → Q254 ①.

비유: "나 누구예요?" 를 물으러 가는 손님 둘 — 계산대 손님 (advapi32) 과 인터넷 담당 (wininet) — 이 결국 **같은 직원 (sspicli)** 에게 부탁하고, 그 직원은 **수첩 한 줄** (`lsasspirpc`) 만 보고 보안 사무소 SSPI 창구로 전화한다.

### 2.4 번호가 수첩에 있다 — 114 를 안 거친다 `[정적]` (epmapper 생략 줄은 `[추측]`)

run 문서 1-2 "epmapper 풀이" 줄. run129 · run133 과 **가장 크게 다른 점** 이다.

| | winnsi (run129 · run133) | sspicli (이번) |
|---|---|---|
| 카드의 끝점 칸 | **비어 있음** | **`lsasspirpc`** |
| 인터페이스 기본 끝점 | 0개 | 0개 (`RpcProtseqEndpointCount 0`) |
| 서버가 번호를 정하나 | 아니 — 전화국이 `LRPC-` + 무작위 18자 (동적 끝점) | **예** — `RpcServerUseProtseqEpW(…, "lsasspirpc", …)` |
| 114 (epmapper) | 꼭 거침 (`ept_map`) | **안 거칠 것** |
| 스텁 | 비동기 (`Ndr64AsyncClientCall`) | **동기 (`NdrClientCall3`)** |
| 인증 설정 | `RpcBindingSetAuthInfoW(…, 6, 10)` | **없음** |

- "끝점이 카드에 있으면 `BindToEpMapper` 를 안 탄다" 는 그 갈림 줄을 안 읽었다 → `[추측]`.
  - 곁 근거: 요청 쪽 로그에 `\RPC Control\epmapper` 연결이 없다 (에뮬레이터 로그, `[관찰]` 아님).
- 서버 쪽도 문지기가 없다: `RpcServerRegisterIf3` 의 보안 콜백이 **NULL** 이다 (`SspiSrvInitialize +0xf3` · `+0x115`). nsisvc 의 "수준 6 이상만" (run133 해설 2.13) 과 다르다 `[정적]`.

비유: nsi 사무실은 번호가 매번 바뀌어서 114 에 물어야 했다. 보안 사무소 SSPI 창구는 **번호가 고정** 이라 수첩에 적혀 있다. 문 앞에 문지기도 없다.

### 2.5 ★ 메뉴 0 (연결) → 메뉴 15 (이름) `[정적]`

run 문서 1-1 (4 ~ 8 번) · 1-2 "프로시저" 줄. 전화 한 번이 **어떤 차례로** 나가는지다.

#### 새 말 — LSA 준비 신호등 (`\SECURITY\LSA_AUTHENTICATION_INITIALIZED`)

- lsass 가 "보안 사무소 문 열었어요" 를 알리는 **이름 붙은 이벤트 (신호등)** 다. sspicli 는 전화 걸기 전에 이 신호등부터 본다 `[정적]`.
- `IsSPMgrReady` 가 하는 일:
  1. `+0x6d NtOpenEvent` (접근 1, 속성 `0x40` = 대소문자 무시) 로 연다.
  2. `+0x95 NtQueryEvent` (클래스 0, 8 B) 로 상태를 묻는다.
  3. `+0xa7 NtClose`.
  4. **`EventState ≠ 0` 이면 0 (준비됨), 0 이면 `0xc0000001`** (`+0xb7 ~ +0xc5`). 열기부터 실패하면 그 상태를 그대로 돌려준다 (`+0x7d JS`).
- 비유: 전화 걸기 전에 보안 사무소 **간판 불** 이 켜졌는지 본다. 꺼져 있으면 전화도 안 건다.

#### 새 말 — `DllState` 깃발

- sspicli 의 **프로세스 전역** 칸. 비트 `0x40000000` 이 켜져 있으면 "이 가게는 이미 보안 사무소에 등록했음" 이다 `[정적]`.
- `IsOkayToExec +0xf` 가 이 비트를 먼저 본다. 켜져 있으면 전역 `SecDllClient` 만 넘기고 **바로 끝** — 신호등도, 메뉴 0 도 안 간다.

#### 처음 한 번 — 다섯 걸음

1. **`IsOkayToExec`** (`SspipGetUserName +0x57`) — `DllState` 비트가 꺼져 있다 → 계속.
2. **`IsSPMgrReady`** (`+0x4b`) — 간판 불 확인 (위).
3. **`InitState`** (`+0x54`) → **`CreateRpcConnection(NULL, 2, &컨텍스트, &SecLsaPackageCount, &깃발)`** (`+0x65`).
   - `SecpGetRpcBinding` (`+0x29`) 로 카드를 만들고,
   - **`+0x6d NdrClientCall3(…, 0, …)` — 메뉴 0 (연결)**. 이 **첫 부름** 이 rpcrt4 안에서 첫인사 (bind, `NdrpClientCall3 +0x81e I_RpcNegotiateTransferSyntax`) 와 전화 연결 (`AlpcConnect +0x3b9 NtAlpcConnectPortEx`) 을 일으킨다.
   - 끝나면 카드를 버린다 (`+0xa0 RpcBindingUnbind` · `+0xb1 RpcBindingFree`).
4. **번호표 보관** — `+0x9e LocalAlloc(0x40, 0x30)` 로 `SecDllClient` 를 만들고 `[+0x18] = 컨텍스트` (`+0xc8`).
   - 받은 깃발에 `0x10000` 이 있을 **때만** `SecpSetSession` + **메뉴 16** (`+0x101`). 그 깃발은 `SecLsaMode` 로 남는다 (`+0x13b`).
   - 자기 DLL 을 한 번 더 올려 내려가지 않게 고정한다 (`+0x1cb LoadLibraryExW("sspicli.dll", NULL, 0x800)`).
5. **`DllState |= 0x40000000`** (`InitState +0x1d7`) — "등록했음" 깃발을 세운다.

#### 그다음부터 — 메뉴 15 만

- `IsOkayToExec +0xf` 에서 바로 끝 → `+0xa7 NtQueryInformationThread(-2, 0, 0x30 B)` 로 내 스레드의 `CLIENT_ID` 를 얻고 → **`+0x1a7` 메뉴 15**.
- 받은 이름은 `+0x1f6 RtlCopyUnicodeString` 으로 사용자 버퍼에 옮기고 (`Length` 가 버퍼보다 크면 `0x80000005`), 받은 글자 버퍼는 `LocalFree` 한다.

#### 새 말 — `CLIENT_ID`

- **PID + TID** 한 쌍 (각 8 B, 합 16 B). "지금 묻는 사람은 몇 번 가게의 몇 번 일꾼" 이라는 표시다 `[정적: 칸 꼴]` (형식 `15 07 10 00 0b 0b 5c 5b`).

#### 카드는 버리는데 어떻게 메뉴 15 를 거나

- 메뉴 15 는 카드 대신 **접수 번호표 (컨텍스트 핸들)** 로 부른다. 형식이 `FC_BIND_CONTEXT` 라 번호표가 그 연결 (바인딩) 을 **들고 간다** `[정적]`.
- 그래서 연결 뒤에는 **같은 ALPC 연결을 계속** 쓰고, `NtAlpcConnectPortEx` 는 **프로세스당 한 번** 이다 (run 문서 3-2 끝).

비유: 처음 온 손님은 ① 간판 불 확인 → ② "저 이 동네 새로 왔어요" (메뉴 0) → ③ **접수 번호표** 받기 → ④ 수첩에 "등록함" 체크. 그다음부턴 체크만 보고 바로 번호표를 내밀며 "제 이름이요" (메뉴 15).

### 2.6 연결 인자 6 · `\RPC Control` SD 묻기는 sspicli 줄이 아니다 `[정적]`

run 문서 1-2 "연결 인자 6" 줄 · 1-3. 요청 쪽 로그에 보인 줄들의 **주인** 을 가린다.

- 요청 쪽 로그: `GetUserNameW` 길에서 `NtOpenDirectoryObject \RPC Control` → `NtQuerySecurityObject 0x17` ×2 → `NtAlpcConnectPortEx` ×4.
- 이 중 앞 셋은 **run99 의 기본 명부 만들기** 다 — sspicli 가 아니라 rpcrt4 가 **프로세스에서 첫 RPC 일 때 한 번** 한다 (`PerformRpcInitialization +0x2b3` → `InitializeDefaultSecurityDescriptor +0x13c` → `RpcpNormalizeSecurityDescriptor +0x12a` · `+0x158` · `+0x19c`).
  - 이번 길에서 그 "첫 RPC" 는 `SecpGetRpcBinding +0x3f RpcStringBindingComposeW` 다.
  - 자세한 걸음은 [run99 해설](run99-rpc-caller-해설.md) 2.1 · 2.2 그대로다. 첫 `NtQuerySecurityObject` 는 `0xc0000023` + 크기, 둘째에 SD.
- **연결 인자 6 (서버 보안 요구)** 도 sspicli 가 주는 게 아니다.
  - sspicli 는 `RpcBindingSetAuthInfo*` · `RpcBindingSetOption` 을 **안 부른다** (`_calls.tsv` 에 없음). 그래서 서버 SID (`[this+0xc0]`) 가 없다.
  - 서버 SID 가 없으면 `AlpcConnect` 가 `+0x24c` · `+0x24f` → `+0x3f5 CMOVZ` 로 rpcrt4 전역 **`gDefaultSecurityDescriptor`** (rva `0x103e60`) 를 채우고, `+0x27a` 에서 그게 NULL 이 아니니 **Ex** 로 간다 (`+0x3b9 NtAlpcConnectPortEx`).
- 그래서 요청 쪽 로그의 "Ex · 인자 6 있음 · Flags `0x20000`" 은 코드와 맞는 모양이다.
  - Flags `0x20000` 은 동기 연결 값이다 (run129 해설 2.8 "Flags: 동기 `0x20000` / 비동기 0").

비유: 전화 부서는 처음 문 열 때 한 번 **기본 명부** 를 만들어 두고, 손님이 따로 "이 사람만 받아 주세요" 를 안 적으면 **모든 전화에 그 기본 명부를 끼워 보낸다**. 명부가 끼워져 있으면 늘 Ex 전화다.

### 2.7 ★ 실패가 LastError 1722 로 올라오는 길 `[정적]`

run 문서 1-4. 에뮬레이터가 `0xc0000034` 로 답했을 때 무슨 일이 일어나는지를 한 칸씩 따라간다.

#### 새 말 — RPC 실패는 "예외" 로 온다

- `NdrClientCall3` 은 실패를 **반환값으로 돌려주지 않고 예외로 던진다** `[정적]` (run 문서 요약 "`NdrClientCall3` 이 예외").
- 그래서 sspicli 는 그 부름을 `__try / __except` 로 감싸고, 처리기에서 `I_RpcMapWin32Status(예외 코드)` 로 RPC 오류 번호를 **상태 (NTSTATUS) 꼴** 로 바꾼다.
- 비유: 전화가 안 되면 전화 부서가 조용히 "안 됐어요" 쪽지를 주는 게 아니라 **비상벨** 을 누른다. sspicli 는 그 비상벨을 받아 "어떤 실패인지" 를 쪽지로 옮겨 적는다.

#### 걸음

1. **관리사무소** — `NtAlpcConnectPortEx` 에 에뮬레이터가 `0xc0000034` ("그런 이름 없음").
2. **전화 부서 (rpcrt4)** — `AlpcConnect` 가 이걸 **`0x6ba` (1722, 서버 없음)** 으로 읽는다 (run129 표).
   - `0x6ba` 는 "다시 시도 종류" 라서 **첫 시도 + 다시 3** → 연결 요청이 **넷** (run133 해설 2.10).
   - → 요청 쪽 로그의 "Ex 연결 넷씩" 이 코드와 맞는다.
3. **예외** — 넷 다 실패하면 `NdrClientCall3` 이 1722 로 예외를 던진다.
4. **sspicli 처리기** — `I_RpcMapWin32Status(1722)` = **`0xc0020017`**.
   - 이번 길에서 첫 RPC 는 **메뉴 0** 이니, 잡는 곳은 `CreateRpcConnection` 의 처리기다 (run 문서 1-4 표 근거 줄).
5. **위로 전달** — `CreateRpcConnection` → `InitState` → `IsOkayToExec` → `SspipGetUserName +0x60 JS` (실패 갈래) → `GetUserNameExW`.
6. **LastError 쓰기** — `GetUserNameExW +0x75 RtlNtStatusToDosError(0xc0020017)` = **1722** → `+0x83 SetLastError(1722)` → 반환 = "상태 ≥ 0 인가" (`+0x94`) = **FALSE**.
7. **advapi32** — `GetUserNameW` 는 그 결과 바이트를 `MOVZX` 로 BOOL 로 바꿀 뿐 (`+0x1b`), LastError 는 안 건드린다 → **0 반환, LastError 1722**.

- 1722 는 "RPC 오류 1722 → 상태 `0xc0020017` → 다시 오류 번호 1722" 로 **한 바퀴 돌아 제자리** 에 온다. 숫자는 결과서가 ntdll 상태 → 오류 표와 rpcrt4 `StatusMap` 을 `pefile` 로 직접 계산한 것이다 `[정적: PE]`.

#### 다른 실패면? (요약 — 전체는 run 문서 1-4 표)

| 어디서 | LastError |
|---|---|
| ALPC 연결 `0xc0000034` ×4 | **1722** |
| ALPC 연결 `0xc0000041` | 8 |
| ALPC 연결 `0xc0000022` / `0xc00000a5` | 5 |
| bind `0x6c2` / `0x6c0` | 1730 / 1728 |
| 보내기 `0xc0000008` | 1727 |
| 신호등을 못 엶 (`0xc0000034`) | 2 |
| 신호등은 열었지만 `EventState == 0` | **31** (연결 시도도 안 함) |
| 서버가 앱컨테이너라 거절 (`0xc0000022` + 오류칸 5) | 5 |
| 서버: 로그온 세션 없음 (`0xc000005f`) | 1312 |
| 버퍼 모자람 (형식 비트 없음 / 있음) | 234 / **122** |

#### 물러서는 길은 없다

- `GetUserNameW` · `GetUserNameExW` · `SspipGetUserName` 어디에도 **토큰 · 레지스트리 · 환경 변수에서 이름을 찾는 호출이 없다** (`_calls.tsv` + `.asm`) `[정적]`.
- "다시 해 보기" 는 둘뿐이다:
  - `IsOkayToExec +0x80` — 앞 단계가 `0xc00000a5` / `0xc0000022` 면 스레드의 **가장 토큰을 잠깐 내리고** (`NtSetInformationThread(-2, 5, 0)`) 다시 해 본 뒤 복구.
  - rpcrt4 연결 재시도 (첫 시도 + 다시 3).
- 비유: 보안 사무소에 전화가 안 되면, 손님은 "그럼 제 명찰이라도 볼게요" 하지 않는다. 그냥 **"모르겠어요" (0) + 고장 메모 1722** 로 끝난다.

### 2.8 ★ wininet 은 꾸러미가 아니라 "이름 한 번 적어 두기" `[정적]`

run 문서 2절. 요청서 짐작을 **바로잡는** 절이다.

#### 요청서는 이렇게 짐작했다

- "`InternetOpenW` 가 LSA 에 가는 건 보안 꾸러미를 묻는 것 (`EnumerateSecurityPackages` / `QuerySecurityPackageInfo` / `AcquireCredentialsHandle` 류) `[추측]`".
- 그럴듯했다 — 인터넷 (HTTPS) 은 TLS 꾸러미를 쓰니까.

#### 코드는 이렇게 말한다 — 사슬

1. `InternetOpenW` (rva `0xcaa40`) — 문자열을 ANSI 로 바꾼 뒤 `InternalInternetOpen` (`+0x17d`), 그 결과를 그대로 반환 (`+0x1ce`). `InternetOpenA` 도 같은 함수 (`+0x4f`).
2. `InternalInternetOpen` (rva `0xcb39c`) — 처음에 반환값 칸 `R13 = 0` (`+0x50`). 전역 **`GlobalDataInitialized` 가 0 이면** (`+0x8c` · `+0x93`) **`GlobalDataInitialize`** (`+0x395`).
3. `GlobalDataInitialize` (rva `0x16be0`, 12,561 B, 부르는 곳 46) — wininet 전역 준비 (설정 · 레지스트리 · `WSAStartup` · ETW …) 한가운데에서 **`+0x1076 InitializeGlobalUserName`**.
4. `InitializeGlobalUserName` (rva `0x1200cc`) — `+0x25 InitOnceExecuteOnce(&once, _InitOnceGlobalUserName)` — "한 번만" 팻말 ([run62 해설 2.7](run62-tp-etw-init-해설.md)). 한 번 성공하면 다시 안 부른다.
5. `_InitOnceGlobalUserName` (rva `0x111520`) — **`+0x58 GetUserNameExA(2, 버퍼 256 B, &0x100)`**.
   - 성공하면 `\` 가 있으면 그 뒤만 남기고 (`+0x76 StrChrA`), 소문자 사본까지 전역 `g_pszCurrentUser` · `g_pszCurrentUserLowerCase` · `g_cchCurrentUserLowerCase` 에 둔다 (`+0x136` · `+0x13c` · `+0x143`).
6. `SspiCli!GetUserNameExA` → `GetUserNameExW(2)` → **2.5 와 완전히 같은 길** (형식 칸만 2).

#### 그럼 꾸러미 함수는 어디서 부르나

- wininet 이 꾸러미 함수를 부르는 곳은 `SecurityPkgInitialize +0x23e AcquireCredentialsHandleA` 하나 — **TLS 손잡기 쪽** 이다 (결과서는 `SecureHandshake_Fsm` · `SecureNegotiate_Fsm` 에서 온다고 적음) `[정적]`.
- `InternetOpenW` 에서 LSA 신호등 · 연결이 나는 까닭은 **`GetUserNameExA`** 다.
- 단, flow-writer 대조로 작은 틈 하나가 남았다 (2.12): `GlobalDataInitialize` **직접** 호출엔 꾸러미 함수가 없지만, 그보다 깊은 곳까지 없다는 건 남은 지도 파일로 못 본다 `[공백: 작음]`. 결론 (LSA 연결은 `GetUserNameExA` 에서) 은 `_InitOnceGlobalUserName +0x58` 로 따로 서 있어 그대로다.

#### 새 말 — 이름 형식 (`NameSamCompatible`, 비트 `0x10000`)

- `GetUserNameEx*` 의 첫 인자 = **어떤 꼴의 이름을 줄까** 를 고르는 번호다.
  - **2 = `NameSamCompatible`** — 서버가 `DOMAIN\user` 꼴로 준다 `[정적]` (서버 규칙은 2.10, 이름 `NameSamCompatible` 은 run 문서 · 결과서 표기).
- advapi32 `GetUserNameW` 는 **`0x10002`** 를 넣는다. 하위 16비트는 2 (`NameSamCompatible`), **비트 16 (`0x10000`) 은 "도메인 없이" 표시** 다 (run 문서 1-1, 3-3).
  - 그래서 `GetUserNameW` 는 `user` 만, wininet 의 `GetUserNameExA(2)` 는 `DOMAIN\user` 를 받는다 — wininet 은 그걸 받아 **스스로** `\` 뒤만 자른다.
  - 비트가 있으면 `*크기` 에 **널 포함 +1** 을 쓰고 (`GetUserNameExW +0xac BT EBP,0x10`), 버퍼 모자람도 `0xc0000023` (→ 122) 으로 바뀐다.

#### 왜 NULL 인가

- `GetUserNameExA` FALSE → 콜백이 `SetLastError` 후 FALSE (`+0x1de` · `+0x160`) → `InitializeGlobalUserName` 이 `WxGetLastError` 값을 돌려줌 (`+0x5f`)
- → `GlobalDataInitialize` 가 그 값을 `[0x18027baa0]` 에 두고 (`+0x107b`) **0 이 아니면 곧장 끝으로** (`+0x1089 JNZ +0x20bd`) 가서 그 값을 반환 (`+0x212e` · `+0x21b5`)
- → `InternalInternetOpen +0x39e JNZ +0x435` → `+0x34e SetLastError(ESI)` → `+0x35a MOV RAX,R13` = **NULL**.
- wininet 은 "**현재 사용자 이름을 못 얻으면 전역 초기화 실패**" 로 본다. LastError 는 2.7 표 그대로 (연결 실패면 **1722**).
- 진짜 Windows 라면: 연결 · `GetUserNameExA(2)` 가 성공하면 핸들 (NULL 아님). 이름 값은 `g_pszCurrentUser` 에만 쓰이고 `InternetOpenW` 결과엔 안 실린다.

비유: 인터넷 담당은 가게 문을 처음 열 때 **"오늘 근무자 이름"** 을 수첩 맨 위에 적어 두는 버릇이 있다. 보안 사무소에 전화해서 이름을 물어야 하는데, 전화가 안 되면 **"근무자를 모르니 문을 안 열겠다"** 고 한다. 보안 꾸러미 (자물쇠) 이야기는 나중에 손님 (HTTPS 사이트) 이 올 때 일이다.

### 2.9 ★ 왜 `InternetOpenW` 는 실패 뒤 매번 다시 해 보나 `[정적]` (InitOnce 쪽은 `[추측]`)

run 문서 2절 3 · "실패하면 다음에 또 시도" 줄. 요청 쪽 로그의 "되풀이" 의 까닭이다.

#### 근거 → 그래서 → 아직 확인 아님

1. **근거 ① — 문지기 칸** `[정적]`: `InternalInternetOpen` 은 매번 전역 **`GlobalDataInitialized`** 를 보고 (`+0x8c CMP` · `+0x93 JZ`), **0 일 때만** `GlobalDataInitialize` 를 부른다.
2. **근거 ② — 칸을 1 로 쓰는 자리는 하나** `[정적]`: `GlobalDataInitialized = 1` 은 `GlobalDataInitialize` 의 **맨 끝 성공 자리 `+0x3103` 에서만** 쓴다.
3. **근거 ③ — 실패는 그 자리 앞에서 빠진다** `[정적]`: 이름 묻기가 실패하면 `+0x1089 JNZ +0x20bd` 로 끝 쪽 반환 줄로 뛰어, `+0x3103` 을 **안 지난다**.
4. **그래서**: 실패한 뒤에도 문지기 칸은 0 그대로 → 다음 `InternetOpen*` 때 **또 `GlobalDataInitialize` 로** 간다.
   - 즉 "실패하면 표시를 못 세워서 부를 때마다 다시" 다.
5. **아직 확인 아님** `[추측]`: 그 안쪽 `InitOnceExecuteOnce` 가 **실패를 기억** 해서 콜백을 다시 안 부르는지, 아니면 다시 부르는지는 안 따졌다. 그 규칙은 kernelbase 몫이라 안 읽었다.
   - 결과서는 "다시 시도" 로 적었고, 요청 쪽 로그의 "되풀이" 와 맞는다 (에뮬레이터 로그).
   - → 4장 "그 밖 `[공백]`" 에 남았다.

#### 이건 `GetUserNameW` 에도 있나

- `GetUserNameW` 쪽은 이런 wininet 문지기 칸이 없다. sspicli 의 `DllState` 비트는 **연결이 성공했을 때만** 선다 (`InitState +0x1d7`, 2.5) `[정적]`.
- 실패 뒤 다음 `GetUserNameW` 가 연결을 다시 시도하는지는 run 문서가 따로 적지 않았다 — 이 글도 거기까지는 말하지 않는다.

비유: 인터넷 담당의 수첩엔 "**오늘 준비 끝**" 도장 칸이 있다. 도장은 준비가 **끝까지 다 됐을 때만** 찍는다. 근무자 이름에서 막히면 도장을 못 찍으니, 손님이 문을 두드릴 때마다 "아 준비 안 됐지" 하고 **처음부터 다시** 한다. 그래서 에뮬레이터 로그에 같은 전화가 한도까지 되풀이된다.

### 2.10 한 번 성공시키는 서버 최소 답 `[정적 + 규칙]`

run 문서 3절. 바이트 꼴은 형식 문자열 (`sspirpc_ProxyInfo`, rva `0x25000` · NDR 2.0 프로시저 형식 rva `0x32ae2` · 타입 형식 rva `0x34d72`) + NDR 2.0 규칙으로 끌어낸 것이다. 칸 오프셋은 **스텁 시작 기준** — 요청은 LRPC 메시지 `+0x68`, 응답은 `+0x40` 부터 (run133 해설 2.7).

#### 먼저 — 첫인사

- bind 에 이름표 `4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0. 문법 표가 두 줄 (NDR 2.0 · NDR64) 이라 제안 마스크는 **3** 일 것 `[추측: run133 "표의 문법마다 OR" 규칙]`.
- 답의 수락 마스크는 **1 (NDR 2.0)** 권장 — 아래 표가 NDR 2.0 기준이라서다. 진짜 서버가 NDR64 (2) 를 고르면 꼴이 다르다 `[추측: 클라이언트가 그 뒤 NDR 2.0 형식을 쓰는 줄은 안 읽음]`.

#### 메뉴 0 — 연결 (프로세스당 한 번)

| 방향 | 칸 | 크기 | 뜻 | 값 |
|---|---|---|---|---|
| 요청 | `+0x00` | u32 | 이름 (`char*`) 의 유일 포인터 참조 ID | **0** (늘 NULL, `InitState +0x63`) |
| 요청 | `+0x04` | u32 | 연결 종류 | **2** (`InitState +0x5e`; `LsaConnectUntrusted` 는 0) |
| 응답 | `+0x00` | u32 | 깃발 → `SecLsaMode` | **`0x10000` 은 피할 것** (켜지면 메뉴 16 이 따라온다). 진짜 서버는 일반 클라이언트에 `0x20000` |
| 응답 | `+0x04` | u32 | 꾸러미 수 | 아무 작은 수 (이 길에선 안 씀) |
| 응답 | `+0x08` | 20 | **접수 번호표** (속성 u32 + GUID 16) | 속성 0 + **0 이 아닌 UUID** `[추측: 0 · 0 은 NULL 핸들로 막힐 것]` |
| 응답 | `+0x1c` | u32 | 상태 | **0** (음수면 `InitState +0x6e JNS` 로 실패) |

- 요청 8 B · 응답 **32 B**.

#### 새 말 — `RPC_UNICODE_STRING` · 유일 포인터 · 적합 가변 배열

- **`RPC_UNICODE_STRING`** = `{Length, MaximumLength, Buffer}` 꼴의 글자열 상자. 선 위로는 상자 머리와 **글자 묶음** 이 따로 실린다 `[정적]` (형식 `1a 03 10 00 …`).
- **유일 포인터 참조 ID** = "여기 가리키는 게 있음 / 없음" 을 알리는 4 B 번호. 0 이면 NULL, 0 이 아니면 뒤에 내용이 온다 `[정적 + 규칙]`.
- **적합 가변 배열** = 글자 묶음 앞에 **`MaxCount` · `Offset` · `ActualCount`** 세 숫자 머리를 붙이는 꼴 `[정적]` (타입 형식 `+0xb8`). "칸은 몇 개 마련했고, 몇 번째부터, 몇 개가 실제로 찼다" 다.
- 비유: 이름표 봉투 겉면에 "길이 · 최대 길이 · **안에 내용물 있음**" 을 적고, 봉투 뒤에 "상자 크기 · 시작 칸 · 실제 개수" 를 적은 글자 상자를 붙여 보낸다.

#### 메뉴 15 — 이름 묻기 (매번)

| 방향 | 칸 | 크기 | 뜻 | 값 |
|---|---|---|---|---|
| 요청 | `+0x00` | 20 | 접수 번호표 | 메뉴 0 이 준 그대로 |
| 요청 | `+0x14` | 4 | 맞춤 (8 정렬) | 0 |
| 요청 | `+0x18` · `+0x20` | 8 · 8 | `CLIENT_ID` PID · TID | 부른 스레드 것 |
| 요청 | `+0x28` | u32 | **형식** | **`0x10002`** (advapi32) / **2** (wininet) |
| 응답 | `+0x00` · `+0x02` | u16 · u16 | `Length` (널 빼고) · `MaximumLength` | 글자 × 2 · **`Length + 2`** (클라이언트는 `MaximumLength` 를 안 읽음) |
| 응답 | `+0x04` | u32 | `Buffer` 유일 포인터 참조 ID | 0 아님 |
| 응답 | `+0x08` · `+0x0c` · `+0x10` | u32 ×3 | `MaxCount` · `Offset` · `ActualCount` | `MaximumLength / 2` · 0 · `Length / 2` (어긋나면 RPC 예외일 것 `[추측]`) |
| 응답 | `+0x14` | `Length` | 글자 (UTF-16LE, 널 없음) | 이름 (값은 사용자 결정) |
| 응답 | 그 뒤 (4 정렬) | u32 | 오류칸 | **0** (상태 `0xc0000001` 일 때만 클라이언트가 읽음) |
| 응답 | 그 뒤 | u32 | 상태 | **0** |

- 요청 `0x2c` B · 응답 **`28 + Length` B** (+ 맞춤 최대 2).

#### 이름 모양 — 서버 (`lsasrv!SspiExGetUserName`) 의 규칙

- **형식에 `0x10000` 비트가 있으면 `user`, 없으면 `DOMAIN\user`** (`+0x174` · `+0x17f`) `[정적]`.
  - 비트가 있으면 `\` 뒤만 (없으면 통째) → `LsapDuplicateString`.
  - 비트가 없고 하위 16비트가 `0xc` 가 아니면 (형식 2) 이름 지도의 이름 **그대로**.
- 서버 쪽 다른 실패: 앱컨테이너가 이름을 못 읽으면 오류칸 5 + `0xc0000022`, 로그온 세션이 없으면 `0xc000005f` (2.7 표).
- 에뮬레이터 답 규칙 (run 문서): 값 자체는 정하지 않음 (사용자 결정), **비어 있지 않게**.

비유: 보안 사무소는 "**성 빼고 이름만**" 표시 (`0x10000`) 가 붙은 질문이면 `user` 만, 아니면 `DOMAIN\user` 로 적어 준다. 계산대 손님 (advapi32) 은 표시를 붙이고, 인터넷 담당은 안 붙이고 받아서 스스로 성을 뗀다.

### 2.11 ★ 에뮬레이터 `NtAlpcConnectPortEx` 가 막힌 직접 이유 (결과서 판정)

run 문서 "PE_emulator 쪽". `../PE_emulator` 코드는 flow-writer 가 안 읽었다. 결과서가 읽은 파일 · 줄 (2026-11-23 시점) 기준이고, **고칠지는 사용자 결정** 이다. 이 절의 판정은 `[관찰]` 이 아니라 결과서의 코드 읽기다.

#### 먼저 — 창구가 틀렸다

- `rpc/lsarpc.cpp` 의 창구 (`\RPC Control\LSARPC_ENDPOINT`, `12345778-1234-abcd-ef00-0123456789ab`) 는 **이 두 길의 창구가 아니다** (2.3).
- `lrpc.cpp:26` 의 `window_definitions` 에 **새 창구 하나** (포트 이름 `lsasspirpc` · UUID `4f32adc8-…` · 버전 1.0 · 메뉴 0 · 15) 가 필요하다. lsarpc 창구는 그대로 둔다.

#### 그런데 창구를 맞게 넣어도 막힌다 — `nt_alpc_create_port.cpp:305 ~ :310`

창구를 하나 더 넣어도, 전화 연결 줄 자체가 그 창구를 안 본다. 이유가 셋이다.

1. **창구 검사를 안 부른다** — 이 Ex 줄은 `service_window_connect` 를 안 부른다. 창구 검사는 plain 연결 (`:189`) 에만 붙어 있다.
   - 그런데 rpcrt4 는 기본 명부가 있으면 **늘 Ex** 로 온다 (2.6). 그러니 창구가 있어도 **Ex 쪽에선 아무도 안 본다**.
2. **빈 봉투 때문에 늘 "진짜 서버 필요"** — `live_server_required = a[5] … a[9]` 가 rpcrt4 의 **빈 8 B 봉투 `a[9]`** (run129 해설 2.8 "빈 봉투") 때문에 늘 참이다. → 이름이 맞아도 `0xc0000034`.
3. **서버 보안 요구가 있으면 멈춤** — `if (a[5]) return stopped(...)` (`:310`). 그런데 `a[5]` = 연결 인자 6 = 기본 명부 (2.6) 는 **늘 있다**.

- 결과서의 처방: 이름이 창구와 같으면 `a[5]` (SD) · `a[9]` (빈 속성) 는 **값을 안 보고 받아** 핸들을 쓰고 성공.
- 비유: 보안 사무소 창구 이름을 안내판에 새로 써 붙여도, **교환대 (Ex 줄)** 가 안내판을 안 보고 ① "봉투 들고 왔네? 진짜 직원 불러야겠다" ② "명부 끼워 왔네? 멈춤" 하고 끊어 버린다. 그래서 막힌다.
- run129 해설 2.12 표에서 같은 빈 봉투 이야기를 plain 줄 (`:192`) 에 대해 했다. 이번엔 **Ex 줄** 에 같은 문제가 있다는 것이다.

#### 맞게 가고 있는 것

| 자리 | 판정 | 한 줄 까닭 |
|---|---|---|
| `sync.cpp:8 ~ :20` (`NtOpenEvent` · `NtQueryEvent`) | **맞음** | 신호등 이름이 이미 있다. `EventState ≠ 0` 이어야 한다 (0 이면 LastError 31, 연결 시도도 안 함). 요청 쪽 로그에서 연결이 이어지니 지금 맞게 나간다 |
| `\RPC Control` 열기 · `NtQuerySecurityObject 0x17` ×2 | **맞음 · 실패시키면 안 됨** | 실패하면 rpcrt4 초기화가 오류 14. 첫째 `0xc0000023` + 크기, 둘째에 SD |
| "Ex 연결 넷씩" | **코드와 맞음** | `0xc0000034` → 첫 시도 + 다시 3 → 0 / NULL + 1722. `InternetOpen*` 은 실패하면 부를 때마다 다시 (2.9) |

#### 최소 순서 (결과서 제안)

연결 (위 처방) → 첫인사 (UUID · 버전, 마스크 1) → 메뉴 0 응답 32 B (깃발 `0x20000` 권장 · 꾸러미 수 아무거나 · 번호표 20 B · 0) → 메뉴 15 응답 (2.10, `형식 & 0x10000` 이면 `user` 아니면 `DOMAIN\user`). 서버 상태는 **연결별 번호표 UUID 하나** 면 충분. 두 번째부터는 메뉴 15 만.

- 공통 몫 (run129 · run133) 은 그대로 쓴다: 연결 인자 · 상태 표, 첫인사 · 본론 · 답장 머리, 첨부물 규칙, 재시도.
- LSA 길의 다른 점은 둘: **끝점 고정 (`ept_map` 없음)** · **동기 스텁**.

### 2.12 flow-writer 대조 · 정정

run 문서 "flow-writer 대조" · "정정". 뜻이 바뀌는 건 없다.

- **예외 처리기 줄은 `.asm` 에 없다** — 결과서의 `SspipGetUserName +0x1c4` · `+0x1c6 I_RpcMapWin32Status`, `CreateRpcConnection +0x87` · `+0x89` · `+0x97` 은 `.asm` · `_calls.tsv` 에 안 나온다. Ghidra 가 `__except` 착지점을 코드로 안 따라가서다. 결과서는 **원본 바이트** (`8b c8 48 ff 15`) 로 확인했다 — 뜻은 그대로.
  - 필터 쪽은 `.asm` 이 있다: `SspipGetUserName$filt$0` (rva `0x23180`) `+0xe I_RpcExceptionFilter`. 1722 가 이 필터에 걸려 처리기로 가는 건 run129 의 "치명 14종만 바깥으로" 와 맞아 보인다 `[추측: 1722 는 그 14종이 아닐 것, 표 안 다시 봄]`.
- **wininet 지도 범위** — 결과서는 "`InternetOpenW` 사슬 (깊이 12) 에 꾸러미 함수가 하나도 없다" 고 적었지만, 남긴 지도 파일은 `GlobalDataInitialize` (caller 46) 에서 **멈춰** 그 아래를 안 담는다. 더 깊은 곳까지 없다는 건 남은 파일로 못 본다 `[공백: 작음]`. 결론은 `_InitOnceGlobalUserName +0x58` 로 따로 서 있어 그대로 (2.8).
- **sspisrv → lsasrv 이음** — `_calls.tsv` 는 `indirect:cfg:?` · `indirect:cfg:[RAX+0x78]`. lsasrv 쪽 표 (`LsapSspiExtensionFunctions+0x18` · `+0x78`) 에 그 함수 주소가 있는 건 맞지만, sspisrv 의 `gLsapSspiExtension` 이 그 표를 가리키게 되는 줄은 안 봤다 → `[추측: 칸 오프셋 · 이름이 맞아서]`.
- **(보탬) `SspiExGetUserName` 의 기능 플래그** — `Feature_LsaGetUserNameExEmpty` 가 켜져 있으면 **이름이 빈 채 성공일 때 원격 측정만** 부르고 길은 같다. 결과서의 "이름이 비면 `0xc0000073`" 은 `SspiExGetUserName` 안이 아니라 `LsapGetNameForLogonSession +0xbd` · 도우미 쪽 줄이다. "빈 이름 성공" 이 어느 갈래인지는 `[공백]` → Q255 ①.
- **(보탬) `InitState` 의 다른 호출** — `+0x15e IsHvsiContainer` · `+0x1ae RtlInitializeGenericTableAvl` · `+0x207 DisconnectRpcConnection`. 갈래는 안 읽음.
- **(보탬) `AlpcConnect` Ex 고르기 순서** — 서버 SID 없는 바인딩은 (SD 인자 → 없으면 `gDefaultSecurityDescriptor`) 가 NULL 이 아닌 한 **늘 Ex**. 결과서와 같은 뜻 (2.6).
- **정정**:
  - 이전 run 문서에 틀린 곳은 없다.
  - 요청서 · 원문 짐작 바로잡기 (고치지 않고 run 문서에 적음): `InternetOpenW` → 꾸러미 묻기 **틀림** (2.8), "lsarpc 인가" → **아니다** (2.3), "`GetUserNameW` → `GetUserNameExW`" → **맞음** (2.3).
  - **run133 의 `[추측]` 하나가 반쯤 올라감** — 아래 3절.

### 2.13 정리 — 비유로 한 번 더

- "나 누구예요?" 를 묻는 손님은 둘 — 계산대 (advapi32 `GetUserName*`) 와 인터넷 담당 (wininet `InternetOpen*`). 둘 다 **같은 직원 sspicli** 에게 부탁한다.
- sspicli 는 수첩에 적힌 **고정 번호 `lsasspirpc`** 로 보안 사무소 **SSPI 창구** 에 직접 전화한다. 114 도, 문지기도 없다. lsarpc 창구가 아니다.
- 처음 한 번은 ① 간판 불 (`LSA_AUTHENTICATION_INITIALIZED`) 확인 → ② 메뉴 0 으로 **접수 번호표** 받기 → ③ "등록함" 체크 (`DllState`). 그다음부턴 번호표만 내밀고 **메뉴 15** 로 이름을 묻는다. 수화기는 답이 올 때까지 들고 있다 (동기).
- 전화가 안 되면 전화 부서가 네 번 걸어 보고 **비상벨 (예외)** → sspicli 가 1722 로 적어 **고장 메모** → 0 / NULL. 다른 데서 이름을 찾지 않는다.
- 인터넷 담당은 "오늘 근무자 이름" 을 못 적으면 문을 안 열고, "준비 끝" 도장을 못 찍어서 **손님이 올 때마다 처음부터 다시** 한다.
- 에뮬레이터는 창구 이름을 안내판에 새로 붙여야 하고, 그보다 먼저 **교환대 (Ex 줄)** 가 안내판을 보게 고쳐야 한다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show d4f0df4 -- runs/03_sprint/flow.md`)

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 | advapi32 · sspicli · wininet · sspisrv · lsasrv 없음 | **두 줄** — advapi32 `43EC8DCD…201` · sspicli `3EDD48FF…9D1` · wininet `409B26F3…F31` 한 줄, sspisrv `AD966CDE…B21` · lsasrv `5F41AB2E…201` 한 줄 (lsass 쪽) |
| 기준 — rpcrt4 PDB 줄 | run133 export 까지 | + run135 export (`I_RpcMapWin32Status` · `NdrClientCall3` · `NdrpClientCall3`) |
| 기준 — 근거 줄 | — | **(run135) 정적 바퀴, 사진 값 대조 없음** (test.exe 가 안 올림 `[관찰]` · API 없음 `[소스]`, 요청 쪽 숫자는 `[관찰]` 아님) |
| 1장 그림 | — | **안 바뀜** (범위 밖) |
| 3장 run133 절 "동기 길" 줄 | "다른 클라이언트 (LSA 등) 몫으로 짐작 `[추측]`" | + **(run135)** LSA 쪽 (sspicli) 이 동기 스텁 `NdrClientCall3` 을 쓰는 건 `[정적]`. rpcrt4 안에서 `0x20000` · 같은 버퍼로 이어지는 줄은 그대로 `[추측]` (Q254 ②) |
| 3장 끝 | — | **새 절 "LSA 이름 묻기 RPC"** `[정적]` — 비유 한 줄, 나무 하나, 여섯 줄 요약 (창구 · 요청서 짐작 바로잡기 · 메뉴 0 은 한 번 · 연결 인자 6 · 실패 · 서버 최소 답) |
| 4장 공백 목록 "3 (범위 밖 — LSA)" | — | **Q249 해결됨 줄** + run135 **동적 (→ Q254) · 정적 (→ Q255) · PE_emulator** 줄 셋 |
| 5장 근거 run | — | **run135 줄** 둘 |

- 태그 변화:
  - 원문 "`GetUserNameW` → sspicli `GetUserNameExW`" — `[추측]` → **`[정적]`**.
  - 요청서 "`InternetOpenW` → 꾸러미 묻기" — `[추측]` → **틀림** (`GetUserNameExA(2)` 가 `[정적]`).
  - run133 "동기 길은 LSA 몫" — `[추측]` 의 **절반** 이 `[정적]` 으로 (LSA 가 동기 스텁을 쓴다). 나머지 절반 (rpcrt4 안 동기 갈래) 은 `[추측]` 그대로. run133 문서는 안 고치고 flow.md 에만 표시했다.
  - 새로 생긴 `[공백]` 넷 (결과서 판정): 연결 인자 6 SD 의 최종 내용 · `GlobalDataInitialize +0x1076` 앞 건너뛰기 갈래 · 형식 3 · 8 · 12 의 서버 결과 · bind 마스크 1 / 2 중 진짜 서버의 선택.
- 짐 지도 (`baggage.md`) 는 안 바뀌었다 — 초기화 동안의 짐이 아니다.

### 큰 그림에서 어떻게 이어지나

- 흐름도 본선 (프로세스 생성 → `NtContinue` 은퇴 → `main`) 엔 **변화가 없다**. 이번도 `main` 뒤 옆길의 지도다.
- run99 → run129 → run133 → run135 로 **전화 부서 (rpcrt4) 의 공통 몫** 이 쌓였고, 이번엔 그 위에 **두 번째 손님 (LSA)** 을 얹었다.
  - run99: 기본 명부 (`\RPC Control` SD) — 이번 로그의 앞 세 줄이 그것.
  - run129: 연결 상태 → 오류 표, 빈 봉투, Ex 길 — 이번 1722 와 "교환대 막힘" 의 바탕.
  - run133: 첫인사 · 본론 · 답장 머리, 재시도 — 이번 "넷씩" 과 서버 최소 답의 바탕.
  - run135: **winnsi 와 다른 점 둘** — 끝점 고정 (114 생략) · 동기 스텁.
- 공통 몫이 맞다는 게 한 번 더 확인된 셈이라, 에뮬레이터는 **창구 하나 (UUID · 이름 · 메뉴 0 · 15) 만 더** 하면 된다 — 단 Ex 연결 줄을 먼저 고쳐야 한다 (2.11).
- 같은 창구의 다른 메뉴 (3 꾸러미 묻기 · 14 계정 조회) 는 Q255 에서 이어진다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q254** (동적, VM 묶음 — 미룸)

- **왜 이번에 못 알았나**: 정적만 했다. 최종 포트 이름 문자열, 진짜 서버가 고르는 말투, 메시지 바이트는 실행 때 값이다.
- **무엇이 필요한가**: test.exe 가 아니라 `GetUserNameW` · `InternetOpenW` 를 한 번씩 부르는 작은 프로그램 (고르기는 사용자 결정). 유저 멈춤점, 그 프로세스 조건, 모두 ret 자리. plan 은 Q231 ④ · Q222 세션과 같이 하기로 적었다.

| run 문서 | 자리 | 무엇을 보나 | 닫는 것 |
|---|---|---|---|
| 1 | `rpcrt4!LRPC_CASSOCIATION::AlpcConnect+0x3b9` (call 직전), `+0x3c0` | `rdx` 이름, `[rsp+0x28]` SD (`!sd`), Flags `0x20000`, `eax` | **포트 이름 `\RPC Control\lsasspirpc`** (2.3), 프로세스당 한 번인지 |
| 2 | `sspicli!SspipGetUserName+0x5c` · `+0x1ae`, `CreateRpcConnection+0x74`, `InitState+0x6a`, `IsSPMgrReady+0x9c` | `[rbp+0x1c]` `EventState`, 콜스택 `GetUserNameW+0x16` → `GetUserNameExW+0x57` → `SspipGetUserName+0x1ae` | 메뉴 차례 (2.5) |
| 3 | bind 답 `+0x48`, 메뉴 0 · 15 요청 · 응답 | 서버가 1 을 고르나 2 를 고르나, 바이트 (`MaximumLength = Length + 2` 인지) | **NDR 도출 확인** (2.10) |
| 4 | `wininet!_InitOnceGlobalUserName+0x5f` | `rax`, `rsp+0x40` ANSI | 형식 2 이름의 진짜 모양 (`DOMAIN` 이 컴퓨터 이름인지) |
| 5 | `sspicli!GetUserNameExW+0xdc` 근처 | `[rdi]` | `*크기` = 글자 수 + 1 인지 |
| 6 | `AlpcConnect+0x3b9` | 4번 · 최종 1722 | 연결 실패 때 재시도 (서버를 못 닿게 할 수 있으면) |
| 7 | `IsOkayToExec+0x80` | 갈래 | 가장 수준 낮은 스레드에서만 도는지 `[추측: 정상 샘플에선 안 돎]` |

- (보탬) 같은 에뮬레이터 로그에서 winnsi 의 epmapper 연결은 **plain**, 이번 LSA 연결은 **Ex** 로 찍혔다. 코드로는 둘 다 Ex 여야 해서 run129 의 "plain 인 까닭 `[공백]`" 은 그대로다 — 위 1 과 같은 자리 (`AlpcConnect+0x27a` `rdx`, Q231 ④) 로 함께 볼 수 있다.

### 정적으로 남은 것 → **Q255**

| 번호 | 무엇 | 왜 이번에 못 했나 |
|---|---|---|
| ① | `lsasrv!LsapGetNameForLogonSessionHelper` (3.6 KB) — 형식 3 · 8 · 12 별로 도메인에 안 붙은 PC 에서 무엇이 실패하나 (`GetUserNameExW` 30 샘플 중 29 가 0 인 이유 후보) + "빈 이름 성공" 갈래 | 이번엔 형식 2 · `0x10002` 길만. 도우미가 커서 형식별은 다음으로 |
| ② | `QuerySecurityPackageInfoW` · `EnumerateSecurityPackagesW` → … → `CallRpcSPM +0xda` (메뉴 `RSI+3` — 3 = `SspirCallRpc` 로 보임 `[추측]`). 같은 창구, 메뉴 3 의 형식 · 응답 | 이번 두 API 의 길이 아니었다 (2.8) |
| ③ | `LookupAccountSidW` · `LookupAccountNameW` — lsarpc 인가, 서버 메뉴 14 (`SspirLookupAccountSid`) 인가 | 요청 범위 밖 |
| ④ | `SHGetKnownFolderPath` · `gethostbyname` 의 RPC 쪽 — 요청 쪽 로그의 "Ex 연결 받은 샘플" 나머지가 같은 창구인지 | 요청 범위 밖 |

### 대기열 밖 — `[공백]` · 짐작으로 남은 것

- 연결 인자 6 SD 의 최종 내용과, 커널이 그걸 어떻게 쓰는지 `[공백]`.
- `GlobalDataInitialize +0x1076` 앞에 이 호출을 건너뛰는 갈래가 있는지 `[공백]`.
- `GlobalDataInitialize` 깊은 곳의 꾸러미 호출 유무 `[공백: 작음]` (2.12).
- `InitOnceExecuteOnce` 가 실패 뒤 콜백을 다시 부르는 규칙 `[추측]` (2.9).
- `gLsapSspiExtension` ↔ `LsapSspiExtensionFunctions` 이음 `[추측]` (2.12).
- 끝점 있는 바인딩이 `BindToEpMapper` 를 안 탄다는 줄 `[추측]` (2.4).

---

## 5. 더 읽기

- run 문서: [`run135-lsa-sspi.md`](run135-lsa-sspi.md)
  - 호출 지점 표 전체 · 사슬 (1-1) · 바인딩 표 (1-2) · 실패 값 표 전체 (1-4) · 서버 쪽 메뉴 표 18개 (1-5) · wininet 사슬 (2절) · NDR 칸 표 (3-1 · 3-2) · 서버 이름 규칙 (3-3) · PE_emulator 표 → 그 문서 "결과" 1 ~ 3절, "PE_emulator 쪽"
- 결과서: [`_workspace/sast/133_lsa-sspi_result.md`](../../_workspace/sast/133_lsa-sspi_result.md) — "먼저 읽는 한 줄 답", 1 ~ 3절, "지금 우리 (PE_emulator) 맞나·틀리나", "동적으로 확인할 것", "다음 요청 후보", 판정
- 요청서: [`_workspace/sast/133_lsa-sspi.md`](../../_workspace/sast/133_lsa-sspi.md) — "왜" (에뮬레이터 로그 두 길)
- flow.md: [`flow.md`](flow.md) — 3장 "LSA 이름 묻기 RPC" 절 (새) · 3장 "LRPC 선 위 형식" 절 (run133) "동기 길" 줄 · [4장 공백 목록](flow.md#4-공백-목록-다음에-채울-곳) "3 (범위 밖 — LSA)" 줄 · [5장 근거 run](flow.md#5-근거-run) run135 줄
- 바로 앞 바퀴들 (같은 전화 그림): [run133 해설](run133-lrpc-wire-해설.md) (2.3 동기 길 · 2.5 첫인사 · 2.7 스텁 · 2.10 재시도), [run129 해설](run129-winnsi-rpc-해설.md) (2.4 연락 카드 · 2.5 메뉴 · 접수 번호표 · 2.7 114 · 2.8 상태표 · 빈 봉투 · 2.12 `0xc0000034`), [run99 해설](run99-rpc-caller-해설.md) (기본 명부)
- 대기열: `_workspace/plan.md` Q249 (끝) · Q254 (동적, 미룸) · Q255 (정적)

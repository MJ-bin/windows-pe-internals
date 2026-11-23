# 133 lsa-sspi — 정적 분석 요청

- 날짜: 2026-11-23
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q249** (PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/309_internals-lsa-sspi.md` — 원문을 먼저 읽을 것. 튜닝 `309_tune12.md` V4, 병목 49 · `GetUserName*` 0 198 · `InternetOpen*` NULL 60). 사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 셋째. **정적만.**
- **먼저 읽는 것**: `_workspace/sast/130_lrpc-wire_result.md` — 1 (bind 메시지·답 칸), 2-1 ~ 2-3 (요청·응답 머리, 동기 길 `Flags 0x20000`), 2-5 (ALPC status → RPC 오류 표), `AttemptRetry` (첫 시도 + 다시 3). `_workspace/sast/127_winnsi-rpc_result.md` 2 (rpcrt4 `AlpcConnect` status 표 — `0xc0000034` → 1722, `+0x3b9 NtAlpcConnectPortEx` 는 `gDefaultSecurityDescriptor` 가 있을 때).
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `advapi32.dll` (778,344 B) · `sechost.dll` (704,008 B) — **아직 analyze 안 됨**, PDB 는 `ms/sym/` 에 있음 (GUID 확인).
  - `sspicli.dll` (307,328 B) · `wininet.dll` (2,716,272 B — 큼) — **아직 analyze 안 됨**, PDB 없음 → `fetch_pdb.py`.
  - `secur32.dll` (53 KB) — 껍데기로 보임 `[추측]`; 사슬에 끼면 그때만.
  - `lsasrv.dll` (서버 쪽) — 질문 3 이 클라이언트 코드로 안 닫힐 때만, 프로시저 하나만. 아니면 "다음 요청으로".
  - `rpcrt4.dll` — 이미 analyze 됨, 필요하면 `^이름$` export 만.
- **필요한 모듈만** analyze: 사슬을 따라가며 (`GetUserNameW` 가 어디로 가는지 import·export 로 먼저 가린 뒤) 하나씩. 서버 메모리 (`free -g`) 를 보고 한 번에 하나.
- 주의: 같은 시간에 다른 decompiler 가 **ntoskrnl** (sast/131) 을 돈다 — 그쪽은 건드리지 않는다. 그 뒤 차례인 sast/132 는 ntdll·ntoskrnl·rpcrt4 export — rpcrt4 export 가 필요하면 sast/131 이 끝나기 전 (sast/132 시작 전) 에 끝내거나, 겹치면 기다린다 (같은 Ghidra 프로젝트 동시 사용 금지).

## 왜 (요청한 쪽 말 요약)

- 에뮬레이터 로그 (진짜 Windows 관찰 아님, r12):
  - `GetUserNameW` 길: `NtOpenDirectoryObject \RPC Control` → `NtQuerySecurityObject 0x17` → `NtAlpcConnectPortEx` ×4 (에뮬레이터 대답 `0xc0000034`) → `GetUserNameW` 0.
  - `InternetOpenW` 길: `NtOpenEvent \SECURITY\LSA_AUTHENTICATION_INITIALIZED` (성공) → `NtQueryEvent` → `NtAlpcConnectPortEx` ×4 (`0xc0000034`) → `InternetOpenW` NULL.
  - Ex 연결 인자: Flags `0x20000` · 서버 보안 요구 (인자 6) 있음 · 연결 메시지 없음 · 받는 속성 버퍼 있음. **포트 이름은 로그에 안 찍힘.**
- 공개 명세 MS-LSAD·MS-LSAT 는 `lsarpc` 만 다룸 — 두 길이 그것인지 SSPI 전용 창구인지부터 모름.

## 알고 싶은 것

1. **`advapi32!GetUserNameW` 길** (`GetUserNameA`·`GetUserNameExW` 는 같은 길에 붙는 자리만 한 줄):
   - 사슬: `GetUserNameW` → … → rpcrt4 바인딩 만들기 / `NdrClientCall*` 까지의 함수 이름 (DLL 경계 포함 — advapi32 / sechost / sspicli 중 어디서 RPC 를 부르나).
   - 바인딩: 포트 (끝점) 이름 문자열 그대로 (`RpcStringBindingComposeW` / `RpcBindingCreateW` 인자, 또는 `\RPC Control\…` 직접), 인터페이스 UUID·버전 (`.rdata` 그대로), 프로시저 번호, 동기 / 비동기 (`NdrClientCall*` / `NdrAsyncClientCall*`).
   - 연결 때 서버에 요구하는 것 — Ex 길의 인자 6 (SD / SID) 이 어디서 오나 (`RpcBindingSetOption` / `RPC_SECURITY_QOS_V*` 의 서버 SID 류). `\RPC Control` SD 묻기 (`NtQuerySecurityObject 0x17`) 가 이 길의 어느 줄인지 한 줄.
   - 실패 (연결 `0xc0000034` → 1722) 가 어떤 값을 거쳐 `GetUserNameW` 0 이 되나, `SetLastError` 값, **다른 길로 물러서는가** (토큰에서 이름 찾기 등).
2. **`wininet!InternetOpenW` → LSA**: LSA 준비 이벤트를 열고 연결하는 사슬 (어느 함수가 왜 — 보안 꾸러미 묻기 `EnumerateSecurityPackages` / `QuerySecurityPackageInfo` / `AcquireCredentialsHandle` 류인지 `[추측]` 확인), 그 호출이 실패하면 왜 `InternetOpenW` 가 NULL 인가 (그 실패를 치명으로 보는 줄). 1 의 창구와 같은 인터페이스인가.
3. **한 번 성공시키는 서버 최소 답** — 분량이 크면 **`GetUserName*` 먼저**:
   - bind 뒤 요청 차례 (연결용 프로시저가 따로 있나 — 핸들·꾸러미 수 등, 그다음 이름 묻기 프로시저 번호).
   - 클라이언트가 응답 스텁에서 읽는 칸 (NDR 꼴 — 형식 문자열 근거): 사용자 이름 글자열이 어디에 어떤 꼴로 오나 (`UNICODE_STRING` / conformant 문자열 / 도메인과 따로), 반환 status 칸.
   - 클라이언트 코드로 안 닫히면 `lsasrv` 의 그 프로시저 하나만 보거나 `[공백]` + "다음 요청으로".

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| advapi32 → (sechost / sspicli) | `GetUserNameW` 와 사슬의 도우미, RPC 바인딩·호출 줄 | 1 | 질문 1 |
| wininet | `InternetOpenW` 에서 LSA 준비 이벤트·보안 꾸러미 묻기까지 (이름만 + 갈림 줄) | 0~1 | 질문 2 |
| sspicli (또는 사슬에 나온 DLL) | LSA 연결 함수 · 형식 문자열 (PE 데이터) | 0~1 | 질문 1·3 |
| lsasrv | (필요할 때만) 이름 묻기 프로시저 하나 | 0 | 질문 3 |

- 범위: 바인딩·호출·실패 갈래·응답 읽기까지. 인증 꾸러미 본문 (Kerberos/NTLM)·wininet 의 HTTP 본문·rpcrt4 본문은 이름만 (rpcrt4 LRPC 머리는 sast/130 을 가리킴).

## 받고 싶은 것

- 결과서 `_workspace/sast/133_lsa-sspi_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄 — 포트 이름·인터페이스·프로시저).
  3. 표: (1) `차례 · 함수 (모듈) · 하는 일 · 실패 때 · 줄` 사슬 표 + 바인딩 표 (`포트 · UUID · 버전 · 프로시저 · 동기/비동기 · 보안 요구`) · (2) wininet 사슬 표 · (3) 요청·응답 NDR 꼴 표 (`오프셋 · 크기 · 뜻 · 근거`).
  4. PDB GUID 확인 줄 (새로 analyze 한 모듈마다).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall` 갈래 svc 의 V4 — 새 `src/rpc/` LSA 창구 (연결 받기 · bind · 요청 답), `src/alpc/nt_alpc_create_port.cpp` (Ex 연결에 지금 `0xc0000034`). 돌려줄 사용자 이름 값은 정하지 않음 (사용자 결정 몫 — 꼴만).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (VM 에서 `GetUserNameW` 한 번 — 포트 이름·메시지 바이트, 멈춤점 `caller+0x…`) 과 **"다음 요청 후보"**.

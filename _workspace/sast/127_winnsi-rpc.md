# 127 winnsi-rpc — 정적 분석 요청

- 날짜: 2026-11-22
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **새 Q223** (PE_emulator 요청 "winnsi 알림 등록 RPC — 서버가 없으면 클라이언트가 어떻게 되고, 한 번 성공시키려면 무엇을 답해야 하나", 튜닝 `303_tune11.md` T1·T2, 샘플 212). 사용자 결정 2026-11-22 "이게 제일 급해 바로 진행". 요청한 쪽의 `298_internals-winnsi-notify.md` 를 대신한다 (그 3 번 — webio 소켓 부름 — 은 뺌). **정적만.** 진짜 메시지 바이트 (질문 3 의 실제 값) 는 동적 — 이번엔 안 하고 결과서 "동적으로 확인할 것" 에 멈춤점만.
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `winnsi.dll` — **아직 analyze 안 됨** (PDB `fetch_pdb.py`, GUID 확인). 작음.
  - `rpcrt4.dll` — 이미 analyze 됨 (`ms/disassemble/rpcrt4/` 에 `AlpcConnect.asm`·`InitializeEPMapperClient.asm`·`TransformLrpcEndpointToAlpcPortName.asm` 등 있음). 다시 analyze 하지 말고 `^이름$` export 만.
  - `IPHLPAPI.DLL`·`webio.dll` — 이미 analyze 됨 (sast/126). 있는 `.asm` 읽기만, 필요하면 `^이름$`.
  - `nsisvc.dll` (서버 쪽) — 질문 3 에서 서버가 돌려주는 출력 꼴이 클라이언트 코드로 안 닫힐 때만 analyze (필요한 함수만).
- 이어 쓰는 것: **sast/126 결과서** A1 9 줄·A6 F1·"다음 요청 후보 1" (`_workspace/sast/126_nsi-callers_result.md`), 이미 있는 `iphlpapi/NotifyIpInterfaceChange.asm`·`InternalRegisterChangeNotification.asm`·`NsiNotificationConnectToServer.asm`, `rpcrt4/AlpcConnect.asm`.
- 주의: 같은 시간에 flow-writer·explainer 가 문서를 쓴다 (runs/, `ms/` 안 건드림). 다른 decompiler 는 없음.

## 왜 (요청한 쪽 말 요약)

- 샘플 212 개 (악성 203 — 악성 전체의 18 %) 가 같은 자리에서 멈춘다. 에뮬레이터 로그 (진짜 Windows 관찰 아님 — r11, Nsi 장치는 이제 대답함):
  - **첫 등록**: `NtCreateEvent` (seq 2196) → Nsi `0x12000f` 성공 (2197) → 닫음 → `NtCreateEvent` 수동 리셋 (2199) → 토큰 묻기 셋 → **`NtAlpcConnectPort` `\RPC Control\epmapper`** (2204 — 연결 메시지 0 · 메시지 속성 있음. 에뮬레이터 대답 = 성공인데 포트 핸들을 안 씀) → `NtQueryInformationProcess` 종류 `0x25` · `NtSetInformationProcess` 종류 `0x3f` · `NtCreateSection` · `NtDuplicateObject` ×2 · `NtOpenEvent` `\KernelObjects\SystemErrorPortReady` (2205 ~ 2224 — 오류 보고 길로 보임 `[추측]`) → 이벤트 닫음 (2226).
  - **둘째 등록** (같은 스레드, 곧바로): `NtCreateEvent` (2227) → Nsi `0x12000f` 성공 (2228) → 닫음 → `NtCreateEvent` (2230) → `NtAllocateVirtualMemory` ×2 → **그 이벤트를 끝없이 기다림** (2233 `NtWaitForSingleObject(0x220, 무한)`). 이번엔 `NtAlpc*` 부름 0.
  - 같은 꼴 210 / 210. 부른 API: `WinHttpSendRequest` 201 · `WinHttpOpen` 4 · `NotifyIpInterfaceChange` 2 · `NotifyRouteChange2` 2 · `CertGetCertificateChain` 1.
  - 같은 대답을 받는 다른 길: `combase!CoInitializeSecurity` → epmapper 연결 (같은 대답) → `NtAlpcSendWaitReceivePort(핸들 0)` `0xc0000008` → combase `+0x55b5a` 가 주소 `0x4` 읽기 (22 샘플).
  - `\RPC Control\keysvc` · `eventlog` 는 에뮬레이터가 `0xc0000034` 로 답하고, 부른 쪽은 멈추지 않고 이어 감 (18 샘플).

## 이미 아는 것

- `[정적]` sast/126 A1: webio `Startup` → `iphlpapi!NotifyIpInterfaceChange` (rva 0x9880) → `InternalRegisterChangeNotification` (0x9758) → `NsiNotificationConnectToServer` (0x1b18c) → `winnsi!NsiConnectToServer` · `NsiRpcRegisterChangeNotification`. 이 둘이 0 이 아니면 `WebInitialize` 실패 → `WinHttpSendRequest` 0.
- `[정적]` `ms/disassemble/rpcrt4/AlpcConnect.asm` `+0x293` `NtAlpcConnectPort` · `+0x2c8 ~ +0x36d` status 견주기 줄 (`0xc0000041`·`0xc0000017`·`0xc0000022`·`0xc0000034`·`0xc0000036`·`0xc0000039`·`0xc00000a5`·`0xc0000044`·`0xc000009a`·`0xc00002a0`·`0xc0000701`) · `+0x3b9` `NtAlpcConnectPortEx`. 어느 status 가 어느 RPC 오류가 되는지는 아직 안 읽음.

## 알고 싶은 것

1. **`winnsi!NsiRpcRegisterChangeNotification` (`…Ex` 가 있으면 그것도)** 와 `NsiConnectToServer`:
   - 바인딩: 끝점 이름이 고정인가 (문자열 그대로), 아니면 끝점 없이 epmapper 로 푸는가 (`RpcBindingFromStringBindingW` 인자 문자열).
   - 동기 / 비동기 RPC 호출인가 (`NdrClientCall*` / `NdrAsyncClientCall*` / `RpcAsyncInitializeHandle` 류). 인터페이스 UUID·프로시저 번호.
   - 부르기 앞에 만드는 이벤트 (위 seq 2199 · 2230) 는 누가 만들고 누가 세우나 (winnsi 의 것인가, rpcrt4 의 것인가 — 비동기 RPC 완료 이벤트인가, 연결 잠금인가).
   - RPC 가 실패하면 (서버에 못 붙음) 돌려주는 값, 그 값이 `NotifyIpInterfaceChange` → webio `Startup` 에서 치명인가 (sast/126 의 줄을 가리키며 짧게).
2. **rpcrt4 LRPC 클라이언트의 연결 길** (`AlpcConnect` 와 부르는 쪽 — 이름은 `_index.tsv`·`pdb_aliases.py` 로 확인):
   - `NtAlpcConnectPort` status 마다 (`0xc0000034`·`0xc0000041`·그 밖 위 줄) 어떤 RPC 오류 (`RPC_S_SERVER_UNAVAILABLE` 1722 류) 로 바뀌나 — 표 하나. 그때 다시 시도하거나 기다리는 것이 있나.
   - **"성공인데 포트 핸들 0"** 일 때: 다음 줄이 무엇을 하나 (핸들을 어디에 저장, 무엇을 검사), 그리고 **같은 바인딩의 다음 호출이 왜 이벤트를 끝없이 기다리나** — 연결 중 / 연결 실패 표시가 바인딩·연결 객체에 남는 칸과 그 칸을 보고 기다리는 줄 (이벤트를 누가 세우기로 돼 있나).
   - epmapper 연결 길에서 seq 2205 ~ 2224 (`NtQueryInformationProcess 0x25`·`NtSetInformationProcess 0x3f`·`NtCreateSection`·`NtDuplicateObject`·`SystemErrorPortReady`) 가 어디서 오나 — rpcrt4 가 부르는가, 그 밖 (오류 보고 / `WerReportException` 류) 인가. 이름만 + 부르는 줄.
3. **알림 등록 한 번을 성공으로 답하려면 서버 쪽이 돌려줘야 하는 최소** — 정적으로 틀만:
   - epmapper 에 묻는가 (1 에서 끝점이 고정이면 "안 묻는다" 로 끝). 묻는다면 묻는 메시지와 답 (끝점 이름) 의 꼴.
   - LRPC 묶기 (bind) 메시지와 답 · 요청 / 응답에서 클라이언트가 검사하는 칸 (머리 꼴 · 길이 · 메시지 종류 값).
   - `NsiRpcRegisterChangeNotification` 의 출력 인자 (핸들 / 컨텍스트 핸들) 꼴과 그 뒤 클라이언트가 쓰는 법.
   - 분량이 크면 3 은 결과서 끝에 "3 은 다음 요청으로" 라고 적고 1·2 만 먼저 끝내도 된다.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| winnsi | `NsiConnectToServer` · `NsiRpcRegisterChangeNotification` (`…Ex`) · 그 사이 도우미 | 1 | 질문 1 |
| rpcrt4 | `AlpcConnect.asm` (있음 — 읽기) + 부르는 쪽 (LRPC 연결 객체의 연결 함수) · 연결 중 기다림 줄 | 0~1 | 질문 2 |
| rpcrt4 | 묶기·요청 보내기·응답 받기 검사 줄 (이름만 + 검사 줄) | 0 | 질문 3 |
| nsisvc | (필요할 때만) 알림 등록 서버 함수 | 0 | 질문 3 |

- 범위: 연결·묶기·호출 한 번까지. rpcrt4 의 NDR 해석기 본문·보안 협상 본문·ALPC 커널 쪽 본문은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/127_winnsi-rpc_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (2) `NtAlpcConnectPort status → RPC 오류 · 다시 시도/기다림 · 줄` · (3) `메시지 · 칸 · 클라이언트가 검사하는 값 · 줄`.
  4. PDB GUID 확인 줄 (새로 analyze 한 모듈마다).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall` 갈래 rpc, `src/alpc/nt_alpc_create_port.cpp:192`·`:305`·`:346`·`:623`. T1 = 속성 있는 ALPC 연결을 어떤 status 로 답할지 (질문 2), T2 = nsisvc 알림 등록 성공 흉내 (질문 1·3). 같은 틀이 LSA (`GetUserName*`)·RPCSS (`CoInitializeSecurity`) 에도 쓰이니 공통 몫과 winnsi 몫을 나눠 적어 주면 좋음.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (ALPC 메시지 한 번 뜨기 — 멈춤점 `caller+0x…`) 과 **"다음 요청 후보"**.

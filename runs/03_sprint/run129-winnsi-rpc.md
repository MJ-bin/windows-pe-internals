# 2026-11-22 (작성일, 정적 분석 2026-11-22) run129 — winnsi 알림 등록 RPC (Q223, 정적만): `NsiConnectToServer` 는 **끝점 없는 `ncalrpc` 바인딩만** 만들고 연결은 안 한다. 그래서 첫 등록 RPC (`NsiRpcRegisterChangeNotificationEx` — 비동기, 프로시저 5) 때 rpcrt4 가 먼저 **`\RPC Control\epmapper` 에 붙어 끝점을 묻는다** (`ept_map`). 요청 쪽 로그의 "첫 등록 / 둘째 등록" 은 **`NotifyIpInterfaceChange` 한 번 안의 IPv4 → IPv6 부름** (`InternalRegisterChangeNotification +0x9d` / `+0xd5`) 이다. `AlpcConnect` 상태 → RPC 오류 표, "성공인데 핸들 0" 뒤 둘째 부름이 끝없이 기다리는 까닭 (바인딩 상태 칸 `+0x114` = 3 이 남음), seq 2205 ~ 2224 는 rpcrt4 가 아니라 **오류 보고 (WER) 길**

> **일부 닫힘 · 고쳐 읽기 (run133, sast/130 — Q230)**: 8절 표 ③ (epmapper bind) · ④ (`ept_map` 탑 층 검사) · ⑥ (nsi bind) 의 `[공백]` → Q230, 그리고 "nsisvc 가 프로시저 5 입력을 어떻게 검사하는지 `[공백]`" 은 **닫혔다** — `[정적]` bind 0x70 B (`+0x28` 1 · `+0x34` 인터페이스 · `+0x48` 제안 마스크) 와 답 검사 셋 (`+0x28` · `+0x48` · `+0x08`), 요청 · 응답 머리 (`+0x34` 호출 ID · `+0x3c` 프로시저 · 응답 종류 3), 질문 탑 4층 · 응답 검사, nsisvc 동적 끝점 + `RpcEpRegisterW` · 프로시저 5 는 입력 값을 안 봄 (run133). "아직 확인 필요" 정적 1 · 2 · 3 닫힘. **고쳐 읽기**: 8절 ③ 의 EPM "전송 문법" 은 **둘** (NDR 2.0 · NDR64, rpcrt4 문법 표 rva `0xe7560` `nCount 2`) 이고 bind 제안 마스크는 **3** 이다 — sast/127 결과서 2-3 단계 8 이 "NDR 2.0" 하나로 적은 것을 sast/130 이 바로잡음 (이 문서 글은 개수를 안 적어 틀린 줄은 없음). ④ "비동기 프로시저 8" · ⑦ · ⑧ 은 그대로 맞다. 그리고 세 대화가 모두 **비동기** 라 서버 답은 `NtAlpcSendWaitReceivePort` 한 번이 아니라 "보내기 (받기 칸 없음) + 완료 통지 + 받기 한 번 더" 로 온다 (run133 2-4).

> **고쳐 읽기 (run141, sast/138 — Q267)**: 호출 관계 각주의 "`RpcBindingBind +0x4b` 가상 `[RAX+0x128]` — 구현 이름은 `[추측]`" → **`LRPC_FAST_BINDING_HANDLE::Bind`** (`Bind__74b50`, rva `0x74b50` — PDB 가상표 `0xe4440` 슬롯 `+0x128`) `[정적: PE]`. epmapper 핸들이 빠른 핸들이라는 것만 `[추측]` 으로 남는다. 그 안 `+0x157` 이 `DriveStateForward(this, fAsync, …)` 를 부르고, 비동기 첫 연결이면 `DriveStateForward +0x51e` 가 보류로 끝나 `Bind__74b50` 가 0 으로 바꿔 돌려준다 (bind 는 연결 응답 뒤 풀 일꾼이 보냄) — `runs/03_sprint/run141-async-bind.md` 1절.

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("winnsi 알림 등록 RPC — 서버가 없으면 클라이언트가 어떻게 되고, 한 번 성공시키려면 무엇을 답해야 하나", 튜닝 `303_tune11.md` T1 · T2, 샘플 212) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 winnsi · rpcrt4 · IPHLPAPI · webio 를 안 올린다 — 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run124 · run128 과 같은 줄), `sample/*.c` 에 RPC · 네트워크 API 가 없다 `[소스]`. 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (PDB 하나 · 근거 줄) · 3장 run128 절 winnsi 줄 화살표 · 3장 run99 절 한 줄 · 3장 끝 새 절 하나 · 4장 (run128 줄 닫힘 표시 · 대기열 번호, run129 줄) · 5장만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q223** (사용자 결정 2026-11-22 "이게 제일 급해 바로 진행"). 결과서 판정 **부분성공** — 질문 1 (winnsi 바인딩 · 호출 꼴 · 이벤트 · 실패 값) · 질문 2 (rpcrt4 연결 길) 닫힘, 질문 3 (서버가 돌려줘야 하는 최소) 은 **틀만**. 남은 몫은 이미 대기열에 있다:
> - **Q230** (정적) — 질문 3 남은 몫: LRPC 선 위 형식 (bind · 요청 · 응답 머리 칸) + `ept_map` 응답 검사 + (필요하면) nsisvc.
> - **Q231** (동적) — 서버 없음일 때 최종 오류 숫자, 첫 부름의 치명 예외 자리, 바인딩 상태 칸, `AlpcConnect` 인자 · 결과, 진짜 ALPC 메시지 한 번.
> - **Q232** (정적, 낮음) — `CreateAlpc → TpAllocAlpcCompletionEx → NtAlpcSetInformation`, `RegisterNsiSvcRestartNotification` WNF 이름.
> - 이번에 닫는 앞 run 몫: **run128** "아직 확인 필요" 정적 1 (= run128 정적 ①, winnsi · `NsiConnectToServer` 가 실제로 하는 일) · 3절 · 9절 2 의 "→ run129 예정". 아래 "정정" 절.

## 요약

- **바로잡음 (요청 쪽 로그 읽기)** `[정적]`: 로그의 "첫 등록 → 둘째 등록" 은 다른 API 두 번이 아니다. `iphlpapi!InternalRegisterChangeNotification` 이 계열 0 이면 **같은 `winnsi!NsiRpcRegisterChangeNotification` 을 IPv4 모듈 ID 로 `+0x9d`, 이어서 IPv6 모듈 ID 로 `+0xd5`** 부른다. `NotifyUnicastIpAddressChange` 도 `NetioRegisterAddressChangeNotification +0x9d` 로 같은 함수에 오니, webio `Startup` 한 번이 등록 RPC 를 **최대 4번** 한다 (인터페이스 v4 · v6, 유니캐스트 v4 · v6).
- **바인딩** `[정적]`: `winnsi!NsiConnectToServer` = `RpcStringBindingComposeW(NULL, "ncalrpc", NULL, NULL, "Security=Impersonation Dynamic True")` → `RpcBindingFromStringBindingW` → `RpcBindingSetAuthInfoW(…, 6, 10, …)` 뿐. **끝점 문자열이 없고**, 인터페이스 구조체의 기본 끝점도 0 개. → rpcrt4 가 첫 호출 때 **epmapper 에 붙어 `ept_map` 으로 끝점을 푼다.** 로그의 `NtAlpcConnectPort \RPC Control\epmapper` 는 nsisvc 연결이 아니라 이 끝점 풀이 연결이다.
- **호출 꼴** `[정적]`: 비동기 RPC — `RpcAsyncInitializeHandle` + 이벤트 통지 + `Ndr64AsyncClientCall`. 인터페이스 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0, 프로시저 **5 = 등록 · 6 = 해제 · 7 = 알림 기다림**.
- **부르기 앞 이벤트** `[정적]`: winnsi 가 `+0x168 CreateEventW(NULL, FALSE, FALSE, NULL)` — **자동 리셋**, 비동기 RPC 완료 통지용. 세우는 쪽 rpcrt4 `IssueNotification (0x345c4) +0xc5 SetEvent`. winnsi 는 `+0x239` 에서 무한히 기다린다.
- **`0x12000f` 도 여기서 난다** `[정적]`: 등록마다 `+0x112` 에서 `nsi!NsiGetAllParameters(1, NDIS, 객체 8, RW 8 B)` 를 먼저 부르고 (run128 의 F2 첫 호출과 같은 인자), 0 이 아니면 RPC 없이 그 값으로 끝 (`+0x120 JNZ → +0x636`).
- **실패가 올라가는 길** `[정적]`: 계열 0 이면 **IPv4 · IPv6 둘 다 실패일 때만** IPv4 오류 (`InternalRegisterChangeNotification +0xe7 ~ +0xf3`) → `NotifyIpInterfaceChange` → webio `Startup +0xbc` 치명 → `WebInitialize` 실패 → `WinHttpSendRequest` 0 (run128 F1).
- **`AlpcConnect` 상태 → RPC 오류** `[정적]`: `0xc0000034` 와 표에 없는 모든 상태 → **`0x6ba` (1722)**, `0xc0000041` (연결 거부) → **`0xe` (메모리 부족)**, 접근 거부 둘 → `5`, 재시도는 서버 SID 불일치 `0xc00002a0` 하나뿐 (서버 SID 를 줬을 때만).
- **"성공인데 핸들 0"** `[정적]`: 포트 핸들은 `NtAlpcConnectPort` 가 연결 객체 `+0x88` 에 바로 쓴다. 상태 0 이면 rpcrt4 는 **핸들 값을 안 본다.** 둘째 부름이 끝없이 기다리는 것은 — 첫 부름이 바인딩 상태 칸 `+0x114` 를 3 (끝점 푸는 중) 으로 올린 뒤 되돌리는 줄 (`CompleteOnePart(0x5aeec) +0xc7`) 에 못 닿으면, 둘째 부름은 "기다리는 사람" 으로만 올라가고 보류 `0xc0021018` 을 받는다 (`ResolveEndpoint +0x30e`) → winnsi `+0x239` 에서 깨울 사람이 없다. 첫 부름이 왜 못 닿았는지는 `[공백]`.
- **seq 2205 ~ 2224** `[정적]`: rpcrt4 가 아니라 **ntdll `RtlReportExceptionEx` 의 줄 순서** (`0x25` 조회 → `0x3f` 설정 → 섹션 → 복제 ×2) 와 kernelbase `WaitForWerSvc` (`SystemErrorPortReady`). rpcrt4 예외 필터가 "치명" (14종) 으로 거른 예외가 바깥으로 나갔을 때 열리는 길. 무슨 예외인지는 `[공백]`.
- **서버 쪽 최소 (틀)** `[정적]`: ① epmapper 연결 ② `ept_map` (EPM `e1af8308-…` v3.0, 비동기 프로시저 8) — 탑 1 개 이상 + 0 ③ nsi 끝점 연결 ④ 프로시저 5 → **컨텍스트 핸들 + 반환 0** ⑤ 프로시저 7 은 **답하지 않음**. LRPC 선 위 머리 칸은 Q230.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든일곱째 바퀴. plan **Q223**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze** 는 winnsi 하나 (11초). rpcrt4 (run99 때 analyze) · IPHLPAPI · webio (run128 때) · ntdll · kernelbase 는 다시 analyze 안 하고 `^이름$` export 와 이미 있는 `.asm` 읽기만. nsisvc 는 analyze 안 함 (질문 3 의 출력 꼴이 클라이언트 코드로 닫혀서).

| 모듈 | 바이너리 (`System32\`) | PDB guid+age | 함수 수 |
|---|---|---|---|
| winnsi | `winnsi.dll` 67,632 B | `907CE42FFA71F5085D1468AD2D40D62C1` | 86 |

- PDB GUID 는 `llvm-pdbutil dump --summary` 의 `{907CE42F-FA71-F508-5D14-68AD2D40D62C}` 와 같음. PDB 안 Age 는 3, 심볼 서버 키는 바이너리의 Age 1 (run128 ws2_32 와 같은 모양). `ms/ghidra/winnsi.analyze.log` 에 `SetPdb:` 줄 있음 — 함수 이름이 PDB 이름으로 잡힘.
- 공개 PDB 라 구조체 타입 이름이 없다. 칸 이름은 코드 줄에서 읽은 것.
- winnsi 는 `rpcrt4.dll` · `NSI.dll` 을 **지연 가져오기** 로 쓴다 (`Ndr64AsyncClientCall` · `RpcAsyncInitializeHandle` · `RpcBindingFromStringBindingW` · `RpcBindingSetAuthInfoW` · `RpcStringBindingComposeW` · `I_RpcExceptionFilter` · `RpcAsyncCompleteCall` · `RpcAsyncCancelCall` …). 첫 호출 때 `__delayLoadHelper2` 가 올린다.
- 버전 대조 (결과서): "알려진 사실" 표에 winnsi · rpcrt4 줄이 없어 `.asm` `+0x` 를 원본 바이트와 직접 맞춤 — winnsi `NsiConnectToServer +0x3a` (`48 ff 15`, ret `+0x41`), rpcrt4 `AlpcConnect +0x293` · `+0x3b9` · `+0x243` · `+0x25a` · `+0x2c8`, `ResolveEndpoint +0xc9` · `+0x30e`, `CompleteOnePart(0x5aeec) +0xc7`, IPHLPAPI `InternalRegisterChangeNotification +0xe7`, webio `Startup +0xbc`, ntdll `RtlReportException +0x54` — **일치**.

## 목적

- PE_emulator 에서 샘플 212 개 (악성 203) 가 같은 자리에서 멈춘다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님): 등록 → Nsi `0x12000f` 성공 → `NtAlpcConnectPort \RPC Control\epmapper` (에뮬레이터 대답 = 성공인데 핸들 안 씀) → `NtQueryInformationProcess 0x25` · `NtSetInformationProcess 0x3f` · `NtCreateSection` · `NtDuplicateObject ×2` · `NtOpenEvent SystemErrorPortReady` → 이벤트 닫음 → 다시 등록 → `0x12000f` → 이벤트 → **그 이벤트를 끝없이 기다림** (`NtAlpc*` 0). 요청서 `_workspace/sast/127_winnsi-rpc.md`:
  1. winnsi — 바인딩 (끝점 고정인가 epmapper 인가), 동기 / 비동기 · 인터페이스 · 프로시저, 앞 이벤트의 주인, 실패값과 치명 여부.
  2. rpcrt4 LRPC 연결 길 — `NtAlpcConnectPort` 상태 → RPC 오류 표, "성공인데 핸들 0" 뒤 무엇을 하나 · 왜 둘째 부름이 기다리나, seq 2205 ~ 2224 의 출처.
  3. 등록 한 번 성공에 서버가 돌려줘야 하는 최소 (정적으로 틀만).
- 쓰는 곳: PE_emulator 튜닝 T1 (속성 있는 ALPC 연결을 어떤 status 로 답할지), T2 (nsisvc 알림 등록 성공 흉내).

## 방법

- 정적 sast/127:
  - winnsi 처음 analyze → `export` depth 0 30개 (`NsiConnectToServer` · `NsiRpcRegisterChangeNotification(Ex)` · `NsiRpcDeregisterChangeNotification(Ex)` · `NsiWorkerThread` · `RegisterNsiSvcRestartNotification` 등).
  - rpcrt4 `^이름$` export (`Connect` · `ResolveEndpoint` ×2 · `SubmitResolveEndpointRequest` · `BindToEpMapper` · `RpcBindingBind` · `EnableAsync` · `CreateAlpc` · `CompleteOnePart` ×3 · `SyncWait` · `Register` · `I_RpcExceptionFilter` · `RpcpIsFatalError` · `LookupNextChunk` · `IssueNotification` ×6 등) + 이미 있던 `AlpcConnect`.
  - ntdll `RtlReportExceptionHelper` · `RtlReportExceptionEx` · `WerpIsProcessNative` · `WerpSetProcessFaultInformation` · `WerpCreateCrashDataSection` 등, webio `Startup`. kernelbase `UnhandledExceptionFilter` · `RaiseFailFastException` · `WaitForWerSvc`, IPHLPAPI `InternalRegisterChangeNotification` · `NsiNotificationConnectToServer` · `NotifyIpInterfaceChange` · `NetioRegisterAddressChangeNotification` · `CancelMibChangeNotify2` 는 이미 있던 `.asm` 읽기.
  - PE 데이터는 `pefile` 로 직접 (파일로 안 남김): winnsi 인터페이스 구조체 rva `0x67b0` · 프록시 정보 `0x6000` · 전송 문법 표 `0x6150` · NDR64 형식 `0x66e0` · `0x6590` · `0x6360`; rpcrt4 바인딩 템플릿 `0xe1188` · `0xe11c0` · EPM 인터페이스 `0xe1220` · 프로토콜 표 `0xe1420` · 치명 오류 표 `0xeb888` · DCE 상태 표 `0xed210`.
- 결과서: `_workspace/sast/127_winnsi-rpc_result.md` (**부분성공**, 스크립트 문제 없음).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` 로 call · ret 짝 — winnsi (`NsiConnectToServer` · `NsiRpcRegisterChangeNotification(Ex)`), IPHLPAPI (`InternalRegisterChangeNotification` · `NotifyIpInterfaceChange` · `NetioRegisterAddressChangeNotification`), rpcrt4 (`AlpcConnect` · `Connect` · `ResolveEndpoint` · `SubmitResolveEndpointRequest` · `BindToEpMapper` · `DriveStateForward` · `EnableAsync` · `CreateAlpc` · `LookupNextChunk`), ntdll (`RtlReportException(Ex)` · `Werp*` 셋), kernelbase (`UnhandledExceptionFilter` · `RaiseFailFastException`), webio `Startup`.
  - `.asm` 줄: winnsi `NsiRpcRegisterChangeNotificationEx +0xc8 ~ +0x26c` · `+0x42c` · `+0x5ba ~ +0x626` · `+0x636` · `+0x664 ~ +0x6b3` · `+0x925`, IPHLPAPI `InternalRegisterChangeNotification +0x63 ~ +0xf5`, rpcrt4 `AlpcConnect +0x270 ~ +0x3a7` · `+0x3f2 ~ +0x3fd` · `+0x454 ~ +0x465` · `+0x72d ~ +0x795`, `ResolveEndpoint +0xc9` · `+0x30e`, `CompleteOnePart(0x5aeec) +0xc7` · `+0xed`, `CompleteOnePart(0x3b148) +0x26e`, `IssueNotification(0x345c4) +0xc5`, `InitializeDefaultSecurityDescriptor +0x12d`. 결과서와 다른 곳 → 아래 "flow-writer 대조".
  - `pdb_aliases.py --lookup rpcrt4.dll` 로 rva `0x38890` (`AlpcConnect`) · `0x48018` (`Connect`) · `0x59258` · `0x59d38` (`ResolveEndpoint` 둘) · `0x595f4` · `0x5aeec` · `0x3b148` · `0x5a5e4` · `0x44e00` · `0x345c4` — **모두 별칭 무리 아님**. `_calls.tsv` 의 `alias:X+1` 은 같은 함수의 C++ 장식 이름 하나 (`.asm` `ALIASES(1)`) 라 PDB 이름을 그대로 썼다.
- 이름이 같은 함수가 여럿인 rpcrt4 는 클래스로 가른다: `ResolveEndpoint` = 바인딩 핸들 쪽 (rva `0x59258`, `ResolveEndpoint.asm`) / EP_LOOKUP_DATA 쪽 (`0x59d38`, `ResolveEndpoint__59d38.asm`), `CompleteOnePart` = 연결 객체 (`0x3b148`) / 제공자 (`0x43164`) / 바인딩 (`0x5aeec`).
- 앞 바퀴 짝: run128 (sast/126 — F1 웹 I/O 시작 길, winnsi 이름만), run99 (sast/95 — rpcrt4 처음 analyze, RPC 런타임 초기화 · `gDefaultSecurityDescriptor`).

## 결과

비유 (run128 의 "전화로 다른 사무실에 알림 신청" 이어서):

- winnsi 는 **전화번호부 없이 회사 이름만** 들고 전화를 건다 (끝점 없는 바인딩). 그래서 rpcrt4 가 먼저 **114 안내 (epmapper)** 에 전화해 "nsi 사무실 번호가 뭐예요?" 를 묻는다.
- 신청은 **"끝나면 벨 울려 주세요" 방식** (비동기) — winnsi 는 벨 (자동 리셋 이벤트) 을 놓고 벨이 울릴 때까지 기다린다.
- 첫 전화가 중간에 **사고 (치명 예외)** 로 끊기면, rpcrt4 의 메모판에 "지금 번호 알아보는 중" 이 그대로 남는다. 다음 전화는 "알아보는 중이네, 끝나면 알려 주세요" 하고 줄만 선다 — 그런데 알려 줄 사람이 이미 없다 `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/{webio,iphlpapi,winnsi,rpcrt4,ntdll,kernelbase}/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `webio!Startup` | `+0xad` · `+0xea` | `+0xb4` · `+0xf1` | `iphlpapi!NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange` | [정적] |
| `iphlpapi!NotifyIpInterfaceChange` | `+0x9b` | `+0xa0` | `InternalRegisterChangeNotification` (객체 번호 7 — `+0x3f`) | [정적] |
| `iphlpapi!NetioRegisterAddressChangeNotification` | `+0x9d` | `+0xa2` | `InternalRegisterChangeNotification` (객체 번호 `0xa`) | [정적] |
| `iphlpapi!InternalRegisterChangeNotification` (rva `0x9758`) | `+0x5e` | `+0x63` | `NsiNotificationConnectToServer` | [정적] |
| 〃 | **`+0x9d`** | **`+0xa4`** | `winnsi!NsiRpcRegisterChangeNotification` — **IPv4** 모듈 ID | [정적] |
| 〃 | **`+0xd5`** | **`+0xdc`** | 같은 함수 — **IPv6** 모듈 ID | [정적] |
| `iphlpapi!NsiNotificationConnectToServer` (rva `0x1b18c`) | `+0x26` | `+0x2d` | `winnsi!NsiConnectToServer` (전역 `NotificationRpcHandle` 이 0 일 때만) | [정적] |
| `winnsi!NsiConnectToServer` (rva `0x2a10`) | `+0x3a` | `+0x41` | `RpcStringBindingComposeW` (`"ncalrpc"`, 끝점 NULL) | [정적] |
| 〃 | `+0x70` | `+0x77` | `RpcBindingFromStringBindingW` | [정적] |
| 〃 | `+0xc7` | `+0xce` | `RpcBindingSetAuthInfoW` (수준 6, 서비스 10) | [정적] |
| `winnsi!NsiRpcRegisterChangeNotification` (rva `0x1180`) | `+0x5b` | `+0x60` | `NsiRpcRegisterChangeNotificationEx` (rva `0x11f0`) | [정적] |
| `winnsi!NsiRpcRegisterChangeNotificationEx` | `+0x112` | `+0x119` | `nsi!NsiGetAllParameters` (NDIS 객체 8 → `0x12000f`) | [정적] |
| 〃 | `+0x143` | `+0x14a` | `RpcAsyncInitializeHandle(…, 0x70)` | [정적] |
| 〃 | `+0x168` | `+0x16f` | `CreateEventW(NULL, 0, 0, NULL)` — **자동 리셋** | [정적] |
| 〃 | **`+0x1e8`** | **`+0x1ef`** | `Ndr64AsyncClientCall` — **프로시저 5 (등록)** | [정적] |
| 〃 | `+0x239` | `+0x240` | `WaitForSingleObject(이벤트, INFINITE)` | [정적] |
| 〃 | `+0x252` | `+0x259` | `RpcAsyncCompleteCall` | [정적] |
| 〃 | `+0x42c` | `+0x433` | `CreateEventW(NULL, 1, …)` — 수동 리셋 (성공 길만) | [정적] |
| 〃 | `+0x4e0` | `+0x4e7` | `RegisterWaitForSingleObjectEx(…, NsiWorkerThread, …)` (성공 길만) | [정적] |
| 〃 | `+0x54e` | `+0x555` | `Ndr64AsyncClientCall` — **프로시저 7 (알림 기다림)** (성공 길만) | [정적] |
| 〃 | `+0x626` | `+0x62d` | `CloseHandle` (완료 이벤트 — 성공 · 실패 합류) | [정적] |
| 〃 | `+0x6b8` | `+0x6bd` | `RegisterNsiSvcRestartNotification` (목록 첫 항목일 때만) | [정적] |
| 〃 | `+0x83a` | `+0x841` | `Ndr64AsyncClientCall` — **프로시저 6 (해제)** (실패 정리) | [정적] |
| `rpcrt4!LRPC_BASE_BINDING_HANDLE::DriveStateForward` (rva `0x44e00`) | `+0x8ca` | `+0x8cf` | `ResolveEndpoint` (바인딩, `0x59258`) | [정적] |
| `rpcrt4!…::ResolveEndpoint` (`0x59258`) | `+0x12b` | `+0x130` | `SubmitResolveEndpointRequest` (`0x595f4`) | [정적] |
| `rpcrt4!SubmitResolveEndpointRequest` | `+0x195` | `+0x19a` | `EP_LOOKUP_DATA::ResolveEndpoint` (`0x59d38`) | [정적] |
| `rpcrt4!EP_LOOKUP_DATA::ResolveEndpoint` | `+0x166` | `+0x16b` | `BindToEpMapper` (`0x5a5e4`) | [정적] |
| `rpcrt4!BindToEpMapper` | `+0x123` · `+0x141` | `+0x128` · `+0x146` | `RpcBindingCreateW` (끝점 `"epmapper"`) · `RpcBindingBind` | [정적] |
| `rpcrt4!…::DriveStateForward` | `+0x515` | `+0x51a` | `LRPC_CASSOCIATION::Connect` (`0x48018`) | [정적] |
| `rpcrt4!LRPC_CASSOCIATION::Connect` | `+0xd2` · `+0x198` · `+0x1c5` · `+0x3b1` · `+0x4e5` | `+0xd7` · `+0x19d` · `+0x1ca` · `+0x3b6` · `+0x4ea` | `Register` · **`AlpcConnect`** · `CompleteOnePart` · `EnableAsync` · `SyncWait` | [정적] |
| `rpcrt4!LRPC_CASSOCIATION::AlpcConnect` (`0x38890`) | **`+0x293`** | **`+0x29a`** | `NtAlpcConnectPort` | [정적] |
| 〃 | `+0x388` · `+0x3b9` | `+0x38d` · `+0x3c0` | `IsPortWithSharedSection` · `NtAlpcConnectPortEx` | [정적] |
| `rpcrt4!EnableAsync` → `CreateAlpc` | `+0x6a` → `+0x66` | `+0x6f` → `+0x6d` | `CreateAlpc` → `ntdll!TpAllocAlpcCompletionEx` | [정적] |
| `rpcrt4!LookupNextChunk` (`0x5a070`) | `+0x11d` | `+0x122` | `Ndr64AsyncClientCall` — EPM **프로시저 8** (`ept_map` 비동기) | [정적] |
| `rpcrt4!IssueNotification` (`0x345c4`) | `+0xc5` | `+0xcc` | `SetEvent` (호출자의 비동기 완료 이벤트) | [정적] |
| `ntdll!RtlReportExceptionEx` | `+0xb5` · `+0x10d` · `+0x13a` · `+0x172` · `+0x1a9` | `+0xba` · `+0x112` · `+0x13f` · `+0x177` · `+0x1ae` | `RtlReportException` · `WerpSetProcessFaultInformation` · `WerpCreateCrashDataSection` · `NtDuplicateObject` ×2 | [정적] |
| `ntdll!RtlReportException` → `WerpIsProcessNative` | `+0x54` → `+0x2d` | `+0x59` → `+0x32` | `WerpIsProcessNative` → `NtQueryInformationProcess(-1, 0x25)` | [정적] |
| `ntdll!WerpSetProcessFaultInformation` · `WerpCreateCrashDataSection` | `+0x2c` · `+0xa8` | `+0x31` · `+0xad` | `NtSetInformationProcess(…, 0x3f)` · `NtCreateSection` | [정적] |
| `kernelbase!UnhandledExceptionFilter` | `+0x110` · `+0x282` · `+0x39d` | `+0x117` · `+0x289` · `+0x3a2` | `NtSetInformationProcess(…, 0x3f)` ×2 · `RaiseFailFastException` | [정적] |
| `kernelbase!RaiseFailFastException` | `+0x12e` | `+0x133` | `WaitForWerSvc` (`NtOpenEvent \KernelObjects\SystemErrorPortReady`) | [정적] |

### 1. 로그의 "첫 / 둘째 등록" = IPv4 → IPv6 [정적]

- `InternalRegisterChangeNotification` (`.asm +0x73 ~ +0xdc`): 계열이 `0x17` (IPv6) 가 아니면 `+0x9d` 에서 IPv4 모듈 ID (`NPI_MS_IPV4_MODULEID`) 로 부르고, 계열이 2 (IPv4) 가 아니면 `+0xd5` 에서 IPv6 모듈 ID (`NPI_MS_IPV6_MODULEID`) 로 또 부른다. 계열 0 (webio `Startup` 이 넘김) 이면 **둘 다**.
- 반환 규칙 (`+0xe1 ~ +0xf5`): 계열 2 → IPv4 결과, 계열 `0x17` → IPv6 결과, **계열 0 → IPv4 가 0 이면 0, 아니면 IPv6 가 0 이면 0, 둘 다 실패면 IPv4 오류.**
- IPv4 가 실패해도 IPv6 부름은 한다 — 로그의 "곧바로 둘째 등록" 과 맞는다.
- `NotifyUnicastIpAddressChange` → `NetioRegisterAddressChangeNotification +0x9d` → 같은 함수 (객체 번호 `0xa`; 인터페이스는 7). → webio `Startup` 한 번에 등록 RPC 최대 4번.

### 2. `NsiConnectToServer` — 바인딩만, 끝점 없음 [정적]

- `+0x12 TEST RCX` / `+0x15 JNZ`: 서버 이름 인자가 0 이 아니면 아무것도 안 하고 0 (원격 미지원).
- `+0x3a RpcStringBindingComposeW(NULL, "ncalrpc" (rva 0x73a8), NULL, NULL, "Security=Impersonation Dynamic True" (rva 0x7360), &문자열)` — **네트워크 주소 · 끝점 없음.**
- `+0x70 RpcBindingFromStringBindingW` — 핸들만 만든다 (연결 안 함).
- `+0xc7 RpcBindingSetAuthInfoW(핸들, NULL, 6, 10, NULL, 0)` — 인증 수준 6 (PKT_PRIVACY), 서비스 10 (WINNT) (상수 이름은 `[추측: 지식]`).
- 실패면 `SetLastError(RPC 오류)` + 0 (`+0x4c` · `+0x95` · `+0xf1`, 설정 실패면 `+0xde RpcBindingFree`).
- IPHLPAPI 는 이 핸들을 **전역 `NotificationRpcHandle` 에 두고 계속 쓴다** (`NsiNotificationConnectToServer +0x32`). 실패면 NULL 로 남아 다음 호출이 다시 만든다.
- 인터페이스 구조체 (rva `0x67b0`) 의 기본 끝점 수 (`+0x38`) = **0**. → 끝점은 epmapper 로만 풀린다.

### 3. `NsiRpcRegisterChangeNotificationEx` — 한 번의 등록 [정적]

1. 인자 검사 (`+0xa0 ~ +0xb6`) — 바인딩 · 구조체 NULL 이면 `0x57` (`+0x70b`).
2. **compartment 칸이 비었으면** (`+0xc8` · `+0xce`) `+0x112 nsi!NsiGetAllParameters(1, NPI_MS_NDIS_MODULEID, 8, NULL, 0, &RW, 8, …)` = **IOCTL `0x12000f`**. 0 이 아니면 그 값으로 끝 (`+0x120 JNZ → +0x636` — RPC 도 이벤트도 없음). 0 이면 RW 두 dword 를 바꿔 구조체 `+0x30` / `+0x34` 에 (`+0x126 ~ +0x132`). 구조체가 등록마다 새로 0 으로 시작하므로 **등록마다 한 번**.
3. `+0x143 RpcAsyncInitializeHandle(&상태, 0x70)` → `+0x153` 통지 종류 1 (이벤트) → `+0x168 CreateEventW(NULL, 0, 0, NULL)` (`+0x15e` 이름 없음 · `+0x161` 처음 꺼짐 · `+0x164` 수동 아님) = **자동 리셋**.
4. `+0x1e8 Ndr64AsyncClientCall(프록시 정보 rva 0x6000, 프로시저 5, NULL, &상태, 바인딩, &{예약 0 · 모듈 ID 포인터 · 객체 번호}, compartment 8 B, 슬롯)` — `__try / __except(I_RpcExceptionFilter(code))` 안 (`$filt$0 +0xe`).
5. 예외 없이 오면 `+0x239 WaitForSingleObject(이벤트, INFINITE)` → `+0x252 RpcAsyncCompleteCall(&상태, &반환)`. 이게 0 이 아니면 그 값이 결과 (`+0x25e JNZ`), 0 이면 서버가 돌려준 DWORD (`[RSP+0x60]`) 가 0 이어야 성공 (`+0x266` · `+0x26c`).
6. **성공 길** (서버 없이는 안 감): 블록 `HeapAlloc 0xb0` (`+0x286`), 비동기 상태 둘 (`+0x2d7` · `+0x37b`, 각각 자동 리셋 이벤트 `+0x347` · `+0x3eb`), 수동 리셋 이벤트 (`+0x42c`), `+0x4e0 RegisterWaitForSingleObjectEx(…, NsiWorkerThread, …)`, **`+0x54e` 프로시저 7 걸어 두기**, 슬롯에 블록 포인터 (`+0x5ba ~ +0x5be`), 목록에 넣기 (`+0x5c8 ~ +0x612`, 목록이 비었으면 `+0x6b8 RegisterNsiSvcRestartNotification` — WNF 구독), 마지막 `+0x626 CloseHandle(완료 이벤트)`.
7. **실패 정리** (`+0x664 ~`): 만든 블록 · 상태 해제, 등록이 서버까지 됐으면 프로시저 6 (`+0x83a`), 그리고 `+0x61e` 로 합류해 같은 `+0x626 CloseHandle` → 로그의 "이벤트 닫음 (2226)".

| 번호 | 쓰는 곳 (winnsi) | 들어감 | 나옴 |
|---|---|---|---|
| 5 | `NsiRpcRegisterChangeNotificationEx +0x1e8`, `NsiReregisterChangeNotification` | 24 B `{예약 0, 모듈 ID 포인터, 객체 번호}` · compartment 8 B | **컨텍스트 핸들 (out)** · 반환 DWORD |
| 6 | `NsiRpcDeregisterChangeNotificationEx +0x12e`, 실패 정리 `+0x83a` | 24 B 구조체 · 컨텍스트 핸들 (in/out) | 반환 DWORD |
| 7 | `+0x54e`, `NsiWorkerThread +0xfe` (알림 올 때마다 다시 검) | 컨텍스트 핸들 (in) | 16 B 구조체 · 24 B 모듈 ID 꼴 · DWORD · 반환 DWORD |
| 0 ~ 4 | `NsiRpcGetParameterEx` · `GetAllParametersEx` · `EnumerateObjectsAllParameters` · `SetParameterEx` · `SetAllParametersEx` | 알림 아님 | |
| 8 | 이 모듈에서 부르는 곳 못 찾음 | | `[공백]` |

- 전송 문법 둘 (NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860`, NDR64 `71710533-beba-4937-8319-b5dbef9ccc36`) 을 제시한다 (rva `0x6150`).
- 컨텍스트 핸들은 형식 문자열 바이트 `0x70` + 깃발 (5 `70 a0` · 7 `70 41` · 6 `70 e0`) 로 읽었다 — 종류 이름은 `[추측]`.
- 서버가 준 컨텍스트 핸들은 블록 `+0x28` 에 보관 (`+0x46b ~ +0x46e`), 슬롯 (iphlpapi `[RBX+0x10]` / `[RBX+0x18]`) 에는 마지막에 **블록 포인터를 덮어쓴다** (`+0x5ba`). 해제 때 iphlpapi 가 이 블록 포인터를 `NsiRpcDeregisterChangeNotification` 에 넘긴다 (`CancelMibChangeNotify2 +0x47 ~ +0xb3`).

### 4. rpcrt4 — 끝점 풀이 사슬 [정적]

```
winnsi Ndr64AsyncClientCall (프로시저 5)  … NDR 해석기 · 전송 문법 협상 (이름만)
 └─ LRPC_BASE_BINDING_HANDLE::DriveStateForward
      ├─ +0x8cf  ResolveEndpoint (바인딩 0x59258)           +0xc9 [바인딩+0x114] = 3 (푸는 중)
      │          └─ +0x130  SubmitResolveEndpointRequest
      │                     ├─ +0x13b  ResolveEndpointWithoutEpMapper   (기본 끝점 0 개 → 못 얻음)
      │                     └─ +0x19a  EP_LOOKUP_DATA::ResolveEndpoint (0x59d38)
      │                                ├─ +0xbf   TowerConstruct (탑 만들기)
      │                                └─ +0x16b  BindToEpMapper
      │                                           ├─ +0x128  RpcBindingCreateW   (LRPC, 끝점 "epmapper", 보안 6/10)
      │                                           └─ +0x146  RpcBindingBind ⇢ (가상 Bind) ⇢ DriveStateForward
      │                                                      └─ +0x51a  LRPC_CASSOCIATION::Connect
      │                                                                 └─ +0x19d  AlpcConnect
      │                                                                            └─ +0x29a  NtAlpcConnectPort(\RPC Control\epmapper)
      └─ (연결 뒤) LookupNextChunk +0x122  Ndr64AsyncClientCall  EPM 프로시저 8 = ept_map
             → LookupNotifyRoutine → … → ResolveEndpointComplete → CompleteOnePart (바인딩 0x5aeec)
                                                                      +0xc7 [바인딩+0x114] = 4 (성공) / 1 (실패) · +0xed SignalAllIos
```

- `+0x..` 는 ret 자리, `+0xc9` · `+0xc7` · `+0xed` 는 그 함수 안 줄. `⇢` 는 가상 호출 (`RpcBindingBind +0x4b indirect:cfg:[RAX+0x128]` — 구현 이름은 `[추측]`).
- `BindToEpMapper` 는 프로토콜 표 (rva `0xe1420`) 에서 `ncalrpc` → 끝점 `epmapper` 를 찾는다 (`+0x97 _wcsicmp`). 실패하면 `RpcBindingFree` + `0x6d8` (1752 EPT_S_CANT_PERFORM_OP). 동기 길은 한 번 더 시도.
- EPM 인터페이스 `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0 (rva `0xe1220`). 동기면 `NdrClientCall3` 프로시저 3 / 7 (`LookupNextChunk +0x16b` / `+0x1ae`), **이번 (비동기) 은 프로시저 8**.
- epmapper 응답 상태는 `FatalLookupErrorEncountered` (rva `0x9c9b0`) 가 바꾼다 — `0x16c9a0cd` → `0x6d8`, `0x16c9a0d6` → `0x6d9` (EPT_S_NOT_REGISTERED), 표 rva `0xed210` 38쌍.

### 5. `AlpcConnect` — 상태 → RPC 오류 · 인자 [정적]

`rpcrt4/AlpcConnect.asm` (rva `0x38890`). 결과는 `EBX`.

| `NtAlpcConnectPort` 상태 | RPC 오류 | 다시 시도 | 줄 |
|---|---|---|---|
| `0` | 동기: `0`. **비동기: 보류 `0xc0021018` + `*출력 = 1`** (그 전에 `IsPortWithSharedSection(this+0x88)`) | 비동기면 `Connect` 가 `EnableAsync` | `+0x379 TEST EDI` · `+0x381 ~ +0x3a5` · `+0x752 ~ +0x75b` |
| `0xc0000034` | **`0x6ba` (1722 RPC_S_SERVER_UNAVAILABLE)** | 없음 | `+0x2ee` · `+0x2f4 → +0x30e` |
| 표에 없는 모든 상태 | `0x6ba` | 없음 | `+0x30e` (기본 착지, `+0x72d CMP EDI,0x102 / JNZ +0x30e` 도) |
| `0xc0000041` · `0xc0000017` · `0xc0000044` | **`0xe` (메모리 부족)** — 연결 거부도 이것 | 없음 | `+0x2c8 ~ +0x2dc` · `+0x349` → `+0x790` |
| `0xc0000022` · `0xc00000a5` | `5` | 없음 | `+0x2e2` · `+0x33d` → `+0x497` |
| `0xc0000036` | `0x72a` | 없음 | `+0x2f6` → `+0x786` |
| `0xc0000039` | `0x6aa` | 없음 | `+0x302` → `+0x770` → `+0x77c` |
| `0xc000009a` | `0x6b9` | 없음 | `+0x355` → `+0x766` |
| `0x102` · `0xc0000701` | `0x71a` (취소) | 없음 | `+0x36d` · `+0x72d` → `+0x739` |
| `0xc00002a0` | 재시도, 안 되면 내부 `0xc0021017` (→ `Connect +0x33f` 가 `5`) | **서버 SID 를 지정한 연결만** — 머신 계정 SID (`+0x5fb LrpcPrepareMachineAccountSid`) 와 같으면 LocalSystem (`+0x6ac`) → LocalService (`+0x697`) → NetworkService (`+0x688`) 로 바꿔 다시 (`+0x44f`) | `+0x361` → `+0x40e ~ +0x44f` |

- 반환 직전 (`+0x10e`): 연결 객체 깃발 `0x400` 이 켜져 있으면 `5` · `0xc0021017` 도 `0x6ba` (`+0x454 ~ +0x465`).
- 상태 이름 (`OBJECT_NAME_NOT_FOUND` 등) 은 확실한 것만 결과서에 붙어 있고, `0xc0000036` · `0xc0000039` · `0xc0000701` · `0xc00002a0` 이름은 `[추측]`.
- **부를 때 인자** (`+0x23c ~ +0x293`): PortHandle = `&this+0x88` (처음 0) · ObjectAttributes = 0x30 B 중 Length 만 · PortAttributes 0x48 B (Flags `0x2090000` 등, MaxMessageLength `0x1000`, DupObjectTypes `0xffd`) · **Flags 동기 `0x20000` / 비동기 0** · 서버 SID `[this+0xc0]` (처음 NULL) · 연결 메시지 · 길이 · 나가는 속성 **0 / 0 / 0** · **들어오는 속성 = 8 B 빈 버퍼 (`AllocatedAttributes 0`) — 늘 비-NULL** (`+0x243` · `+0x25a` · `+0x25e`) · Timeout 동기만.
- **`NtAlpcConnectPortEx` 로 가는 조건**: `+0x27a TEST RDX / +0x27d JNZ +0x3a9`. RDX 는 `+0x3f2 ~ +0x3f5` 에서 호출자 SD 가 없으면 **전역 `gDefaultSecurityDescriptor`** 로 채운다. 이 전역은 run99 의 RPC 런타임 초기화가 채우는 칸이다 (`InitializeDefaultSecurityDescriptor +0x12d LEA R8,[gDefaultSecurityDescriptor]` → `+0x13c RpcpNormalizeSecurityDescriptor`). 요청 쪽 로그가 plain `NtAlpcConnectPort` 인 까닭은 `[공백]` (Q231 ④).

### 6. 연결 객체 · 바인딩 핸들의 상태 칸과 기다리는 줄 [정적]

- **연결 객체 `LRPC_CASSOCIATION`**: `+0x4c` 상태 (1 처음 · 3 연결 중 · 4 연결됨 · 2 · 5 종료), `+0x88` 포트 핸들, `+0x148`, `+0xc0` 서버 SID, `+0xd0` 깃발. 상태 이름은 `[추측]`.
  - `Connect`: 상태 1 이면 `+0x10c` `[+0x4c] = 3` → `Register` → `AlpcConnect`. 상태 3 이면 기다리는 사람으로만 (동기 `SyncWait`, 비동기 보류 `0xc0021018`).
  - 동기 결과는 `CompleteOnePart (0x3b148)` 가 반영 — 성공 `+0x26e [+0x4c] = 4`, 실패면 핸들 닫고 상태 2 (오류 보관) — 단 `0x71a` · `0xc0021017` 이면 상태 1 (다시 가능). 끝에 `SignalAllIos`.
  - **비동기 + 성공**: `AlpcConnect` 가 보류 + `*출력 = 1` → `Connect +0x3b1 EnableAsync` → `CreateAlpc` → `TpAllocAlpcCompletionEx(…, 포트 핸들, LrpcIoComplete, …)`. 핸들 값 검사는 이 길 어디에도 없다 (`AlpcConnect +0x379` 는 상태만 봄, `IsPortWithSharedSection` 은 핸들 0 이면 `NtAlpcQueryInformation` 없이 1).
- **바인딩 핸들 `LRPC_BASE_BINDING_HANDLE`**: `+0x114` 상태 (1 처음 · 3 끝점 푸는 중 · 4 풀림).
  - `ResolveEndpoint (0x59258)`: 상태 1 이면 **`+0xc9 [+0x114] = 3`** → `SubmitResolveEndpointRequest`. 1 이 아니면 기다리는 사람으로 (`+0x90 Register`) — 동기 `SyncWait`, **비동기 `+0x30e` 보류 `0xc0021018`**.
  - `CompleteOnePart (0x5aeec)`: **`+0xc7 [+0x114] = 4 / 1`** → `+0xed SignalAllIos` 로 모두 깨움. **이 줄에 닿아야만 3 이 풀린다.**
  - 비동기 기다리는 사람은 따로 이벤트가 없고, **호출자 (winnsi) 의 완료 이벤트** 를 `IssueNotification (0x345c4) +0xc5 SetEvent` 가 세운다.
- **→ 둘째 부름의 끝없는 기다림** (코드로 설명되는 것): 첫 부름이 `+0x114 = 3` 을 쓴 뒤 `CompleteOnePart +0xc7` 에 못 닿은 채 나가 버리면 (예외), 둘째 부름 (IPv6) 은 `ResolveEndpoint +0x30e` 보류 → winnsi `+0x239` INFINITE → 깨울 사람 없음. 로그의 "둘째 = `NtAlpc*` 0, 이벤트 무한 대기" 와 맞는다. 첫 부름이 정말 이렇게 나갔는지는 `[추측]`.

### 7. seq 2205 ~ 2224 = 오류 보고 (WER) 길 [정적]

| 로그 (요청 쪽) | 코드 | 줄 |
|---|---|---|
| `NtQueryInformationProcess 0x25` | `ntdll!WerpIsProcessNative +0x2d` | ← `RtlReportException +0x54` ← `RtlReportExceptionEx +0xb5` |
| `NtSetInformationProcess 0x3f` | `ntdll!WerpSetProcessFaultInformation +0x2c` | ← `RtlReportExceptionEx +0x10d` (또는 `RtlReportExceptionHelper +0x505`; kernelbase `UnhandledExceptionFilter +0x110` / `+0x282` 에도 같은 부름) |
| `NtCreateSection` | `ntdll!WerpCreateCrashDataSection +0xa8` | ← `RtlReportExceptionEx +0x13a` |
| `NtDuplicateObject ×2` | `ntdll!RtlReportExceptionEx +0x172` · `+0x1a9` | |
| `NtOpenEvent \KernelObjects\SystemErrorPortReady` | `kernelbase!WaitForWerSvc` | ← `RaiseFailFastException +0x12e` ← `UnhandledExceptionFilter +0x39d` |

- 순서가 `RtlReportExceptionEx` 의 줄 순서 (`+0xb5` → `+0x10d` → `+0x13a` → `+0x172` → `+0x1a9`) 와 맞는다. kernelbase 쪽 `SystemErrorPortReady` 와 이어지는 정확한 사슬은 `[공백]`.
- **열리는 조건**: rpcrt4 `I_RpcExceptionFilter` (rva `0x3c900`) 는 `RpcpIsFatalError` (표 rva `0xeb888`, 14개 — `0xc0000005` · `0xc0000194` · `0xc00000aa` · `0x80000002` · `0xc0000096` · `0xc000001d` · `0x80000003` · `0xc00000fd` · `0xc0000235` · `0xc0000006` · `0xc0000420` · `0xc0000409` · `0x80000001` · `0xc00002c9`) 가 참이면 바깥으로 넘긴다. 그 밖 (RPC 오류 코드) 은 winnsi 가 잡는다. → **로그의 첫 부름은 "RPC 오류" 가 아니라 "치명 예외" 였다** (결과서 읽기). 어디서 났는지는 `[공백]` — 후보 `CLIENT_IO_PROVIDER::Register +0x267` · winnsi `+0x925` 의 `INT 0x29` (목록 깨짐) `[추측]`.

### 8. 서버 쪽 최소 — 틀 [정적 + 공백]

| 단계 | 클라이언트가 보내는 것 | 서버가 돌려줘야 하는 것 | 근거 |
|---|---|---|---|
| ① ALPC 연결 (끝점 풀이) | `NtAlpcConnectPort(\RPC Control\epmapper, …)` | **핸들을 `*PortHandle` 에 쓰고 성공**. 들어오는 속성 버퍼는 `ValidAttributes = 0`. 비동기면 이어서 `NtAlpcSetInformation` (`TpAllocAlpcCompletionEx` 안) | `AlpcConnect +0x293` · `Connect +0x3b1` · `CreateAlpc +0x66` |
| ② 핸들 질문 | `NtAlpcQueryInformation(핸들, 0, 0x10 B)` | 오류여도 됨 (1 = 공유 섹션 있음 취급). `Flags & 0x1000000` 이면 `[연결+0x240] = 0xff00` | `AlpcConnect +0x388` · `IsPortWithSharedSection +0x25 ~ +0x3f` |
| ③ epmapper bind | EPM v3.0, 전송 문법 | bind 확인 | `[공백]` → Q230 |
| ④ `ept_map` | 비동기 프로시저 8: 탑 = nsi 인터페이스 `7ea70bcf…` v1.0 + 전송 문법 + `ncalrpc` | 탑 1 개 이상 (끝점 이름) + 상태 0. 비면 `0x6d9` | `LookupNextChunk +0x11d` · 탑 층 검사 `[공백]` → Q230 |
| ⑤ 둘째 ALPC 연결 | `\RPC Control\<끝점>` | ① · ② 와 같음 | 같은 `AlpcConnect` |
| ⑥ nsi bind | `7ea70bcf…`, NDR 2.0 + NDR64 | bind 확인 (고른 전송 문법) | `[공백]` → Q230 |
| ⑦ 프로시저 5 | 24 B 구조체 · compartment 8 B | **컨텍스트 핸들 (선 위 20 B) + 반환 DWORD 0** | `NsiRpcRegisterChangeNotificationEx +0x1e8 ~ +0x26c` |
| ⑧ 프로시저 7 | 컨텍스트 핸들 | **답하지 않는다** (걸어 둠) | `+0x54e`, `NsiWorkerThread` |
| ⑨ 성공 뒤 클라이언트 자체 syscall | — | 가짜로 답할 것 아님: 힙 · 이벤트 4 · `RegisterWaitForSingleObjectEx` · (첫 등록) WNF 질문 · 구독 | `+0x286 ~ +0x6b8` |

- **`ept_map` 을 건너뛸 길은 없다** — 인터페이스에도 바인딩 문자열에도 끝점이 없다.
- compartment 8 B = `NsiGetAllParameters(NDIS 객체 8)` 의 `{번호, 범위}` 를 서로 바꾼 것 (`+0x126 ~ +0x132`). run128 9절 (a) 의 `{1, 0}` 대답이면 `{범위 0, 번호 1}`.
- 컨텍스트 핸들 값을 검사하는 줄은 못 찾았다. 안전하게는 0 이 아닌 값 (속성 0 + 새 GUID) `[추측]`.
- 서버 (nsisvc) 가 프로시저 5 입력을 어떻게 검사하는지는 안 열었다 `[공백]` → Q230.

### 호출 관계

```
(PE_emulator 요청 — test.exe 초기화 밖)
webio!Startup
 ├─ +0xb4  iphlpapi!NotifyIpInterfaceChange                     (객체 7)
 │         └─ +0xa0  InternalRegisterChangeNotification
 │                  ├─ +0x63  NsiNotificationConnectToServer    (전역 핸들 있으면 건너뜀)
 │                  │         └─ +0x2d  winnsi!NsiConnectToServer
 │                  │                   ├─ +0x41  RpcStringBindingComposeW   "ncalrpc", 끝점 NULL
 │                  │                   ├─ +0x77  RpcBindingFromStringBindingW   (연결 안 함)
 │                  │                   └─ +0xce  RpcBindingSetAuthInfoW     6 / 10
 │                  ├─ +0xa4  winnsi!NsiRpcRegisterChangeNotification   IPv4   ← 로그 "첫 등록"
 │                  │         └─ +0x60  NsiRpcRegisterChangeNotificationEx
 │                  │                   ├─ +0x119  nsi!NsiGetAllParameters  NDIS 8 → 0x12000f   (0 아니면 끝)
 │                  │                   ├─ +0x16f  CreateEventW   자동 리셋
 │                  │                   ├─ +0x1ef  Ndr64AsyncClientCall  프로시저 5   → rpcrt4 끝점 풀이 (4절)
 │                  │                   ├─ +0x240  WaitForSingleObject  INFINITE
 │                  │                   ├─ +0x259  RpcAsyncCompleteCall
 │                  │                   ├─ (성공) +0x555 Ndr64AsyncClientCall  프로시저 7 · +0x6bd RegisterNsiSvcRestartNotification
 │                  │                   └─ +0x62d  CloseHandle
 │                  └─ +0xdc  winnsi!NsiRpcRegisterChangeNotification   IPv6   ← 로그 "둘째 등록"
 └─ +0xf1  iphlpapi!NotifyUnicastIpAddressChange               (객체 0xa)
           └─ +0x1e NetioRegisterAddressChangeNotification → +0xa2 InternalRegisterChangeNotification   (위와 같음)

ntdll!RtlReportExceptionEx                                       (seq 2205 ~ 2224 의 출처)
 ├─ +0xba   RtlReportException → +0x59 WerpIsProcessNative → +0x32 NtQueryInformationProcess 0x25
 ├─ +0x112  WerpSetProcessFaultInformation → +0x31 NtSetInformationProcess 0x3f
 ├─ +0x13f  WerpCreateCrashDataSection → +0xad NtCreateSection
 └─ +0x177 · +0x1ae  NtDuplicateObject
kernelbase!UnhandledExceptionFilter +0x3a2 → RaiseFailFastException +0x133 → WaitForWerSvc (SystemErrorPortReady)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). rpcrt4 안쪽은 4절 나무.
- `NetioRegisterAddressChangeNotification` 의 ret `+0x1e` 는 run128 나무 그대로 (`NotifyUnicastIpAddressChange +0x19`).

## 확인된 것

- `[정적]` `InternalRegisterChangeNotification` 은 계열 0 이면 같은 `NsiRpcRegisterChangeNotification` 을 IPv4 (`+0x9d`) → IPv6 (`+0xd5`) 로 두 번 부르고, 둘 다 실패일 때만 IPv4 오류를 돌려준다 (`+0xe7 ~ +0xf3`).
- `[정적]` `NsiConnectToServer` 는 `ncalrpc` · 끝점 없음 · `Security=Impersonation Dynamic True` 바인딩 + 인증 6 / 10 만 만든다. 연결 · RPC 호출 없음. 인터페이스 기본 끝점 0 개 (rva `0x67b0+0x38`).
- `[정적]` 등록은 비동기 `Ndr64AsyncClientCall` 프로시저 5, 인터페이스 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0. 해제 6, 알림 기다림 7.
- `[정적]` 완료 이벤트 = winnsi `+0x168` 자동 리셋, 세움 = rpcrt4 `IssueNotification (0x345c4) +0xc5`, 기다림 = winnsi `+0x239` INFINITE.
- `[정적]` 등록마다 `+0x112 NsiGetAllParameters(NDIS 8)` = `0x12000f` 를 먼저 내고, 실패면 RPC 없이 끝 (`+0x120 → +0x636`).
- `[정적]` 끝점 풀이 사슬 `DriveStateForward +0x8ca` → `ResolveEndpoint +0x12b` → `SubmitResolveEndpointRequest +0x195` → `EP_LOOKUP_DATA::ResolveEndpoint +0x166` → `BindToEpMapper +0x123` · `+0x141` → … → `Connect +0x198` → `AlpcConnect +0x293 NtAlpcConnectPort`, 뒤에 `LookupNextChunk +0x11d` EPM 프로시저 8.
- `[정적]` `AlpcConnect` 상태 → RPC 오류 표 (5절). `0xc0000034` · 그 밖 → `0x6ba`, `0xc0000041` → `0xe`.
- `[정적]` rpcrt4 `NtAlpcConnectPort` 의 들어오는 속성 인자는 늘 8 B 빈 버퍼 (비-NULL), Flags 는 비동기 0 · 동기 `0x20000`.
- `[정적]` 상태 0 이면 핸들 값을 안 본다 (`+0x379` 상태만). 바인딩 상태 칸 `+0x114` 는 `ResolveEndpoint +0xc9` 가 3 으로, `CompleteOnePart(0x5aeec) +0xc7` 만 4 / 1 로 되돌린다. 3 일 때 비동기 부름은 `+0x30e` 보류.
- `[정적]` `NtQueryInformationProcess 0x25` · `NtSetInformationProcess 0x3f` · `NtCreateSection` · `NtDuplicateObject ×2` 는 ntdll `RtlReportExceptionEx` 의 줄 순서, `SystemErrorPortReady` 는 kernelbase `WaitForWerSvc`. rpcrt4 예외 필터가 치명 14종이면 바깥으로 넘긴다.
- `[정적]` winnsi PDB `907CE42FFA71F5085D1468AD2D40D62C1`, 함수 86.

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- 결과서 1-3 의 "`+0x120 JNZ +0x636`" — `.asm` 은 `JNZ 0x180001826` = `+0x636` (함수 끝 쿠키 검사 앞). **이벤트를 만들기 전이라 `CloseHandle` 도 없다.** 그러니 `0x12000f` 가 실패한 등록은 요청 쪽 로그에 "`NtCreateEvent` 수동/자동 → … → 이벤트 닫음" 꼴이 **안 나온다** (보탬).
- 결과서 1-3 의 실패 정리 "`+0x61e` 합류" — `+0x684 JZ 0x18000180e` (= `+0x61e`) 로 합류한 뒤 `+0x626 CloseHandle`. ret 은 `+0x62d`.
- 결과서는 kernelbase `UnhandledExceptionFilter +0x110` / `+0x282` 의 ret 을 안 적었다 — `_calls.tsv` 로 `+0x117` / `+0x289` (7 바이트 부름).
- 결과서 2-1 의 "`NtAlpcConnectPortEx` 로 가는 조건 = `gDefaultSecurityDescriptor`" — 이 전역을 채우는 쪽이 run99 의 `InitializeDefaultSecurityDescriptor +0x12d` · `+0x13c` 임을 `.asm` 으로 이었다 (보탬). run99 는 이 초기화가 실패하면 RPC 런타임 전체가 `0xe` 로 실패한다고 했으니, 초기화가 끝났다면 이 전역은 채워져 있을 것이고 그러면 Ex 길이 예상된다 `[추측]`. 요청 쪽 로그는 plain 이다 — 어느 쪽인지 `[공백]` (Q231 ④).
- (보탬, `[추측]`) 요청 쪽 로그에서 첫 부름이 WER 길 (2205 ~ 2224) 을 지난 **뒤에도** 이벤트를 닫고 (2226) IPv6 부름으로 이어 갔다. 치명 예외가 rpcrt4 필터를 지나 바깥으로 나갔다면, 어딘가 바깥 처리기가 그것을 보고 (`RtlReportExceptionEx`) 잡은 뒤 winnsi 쪽 정리 길로 돌아왔다는 뜻으로 읽힌다. 어디서 잡혔는지는 Q231 ② 에서 `kv` 로 본다.

## 요청 쪽 로그와 코드가 다른 곳 (요청 쪽 에뮬레이터 로그 — 우리 `[관찰]` 아님)

- **"첫 / 둘째 등록"** → 같은 함수의 IPv4 · IPv6 부름 (1절).
- **seq 2199 "수동 리셋"** → 코드 `+0x168` 은 **자동 리셋** (`EDX = 0`). 수동 리셋은 성공 길의 `+0x42c` 하나뿐. 로그가 `EventType` 숫자를 읽는 쪽 (0 = 수동, 1 = 자동) 을 확인할 것 — 결과서 지적.
- **plain `NtAlpcConnectPort`** → 위 "flow-writer 대조" 넷째 줄. `[공백]`.
- **"에뮬레이터 대답 = 성공인데 핸들 안 씀"** → rpcrt4 는 핸들 값을 검사하지 않고 비동기 완료 통지 (`TpAllocAlpcCompletionEx`) 로 간다 (6절). 핸들 없는 성공은 상태 칸 3 을 남길 수 있는 틀린 대답이다 (결과서 4절 T1).

## 정정

- **run128** (`runs/03_sprint/run128-nsi-callers.md`) — **일부 닫힘 + 정정 하나**. 맨 위에 한 줄:
  - **닫힘**: 3절 · 요약의 "`NsiConnectToServer` 가 실제로 서버를 만나는지는 winnsi 를 안 읽어 `[공백]` → run129 예정" → **`[정적]` 바인딩만 만든다 (연결 안 함)**. 서버를 처음 만나는 건 등록 RPC (`NsiRpcRegisterChangeNotificationEx +0x1e8`) 이고, 그때 rpcrt4 가 epmapper 에 먼저 붙는다. "아직 확인 필요" 정적 1 (= 정적 ①, Q223) 닫힘. 9절 2 "→ run129 예정" 의 답 = 이 문서 8절 (서버 최소 틀) · 결과서 4절 T1 · T2.
  - **정정 (추론)**: 3절 · 요약 "이 줄은 F2 (`+0x2fa`) **앞**이라, 로그에 `0x12000f` 가 찍힌 호출은 이 줄을 이미 지난 것이다" 는 **성립하지 않는다.** F1 안에서도 `0x12000f` 가 난다 — winnsi `NsiRpcRegisterChangeNotificationEx +0x112` 가 등록마다 `nsi!NsiGetAllParameters(1, NDIS, 8, RW 8 B)` (F2 첫 호출과 같은 인자) 를 내고, **실패하면 RPC 없이 그 오류로 끝난다** (`+0x120 → +0x636`) → IPv4 · IPv6 둘 다 실패 → `NotifyIpInterfaceChange` 오류 → webio `Startup +0xbc` → `WebInitialize` 실패 → `WinHttpSendRequest` 0. 그러니 run128 이 `[공백]` 으로 둔 "`0x12000f` 가 보인 호출의 0 반환" 에 **정적 후보 사슬** 이 생겼다 `[정적]`. 그 요청 로그의 `0x12000f` 가 정말 이 자리 것인지는 `[추측]` — 동적 Q222 ① · Q231 ⑦ 로 가린다.
  - "연결 핸들 NULL → 6" (3절) 은 그대로 맞다. 다만 서버가 없을 때 실패는 이 줄이 아니라 등록 RPC 오류 (또는 위 `0x12000f` 실패) 로 온다 — 바인딩 만들기는 서버 없이도 성공하므로.

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 코드 | 지금 (결과서) | 해야 할 일 |
|---|---|---|
| `nt_alpc_create_port.cpp:192` (`NtAlpcConnectPort`) | **틀림 — 막힘의 직접 원인.** `a[8] \|\| a[9]` 면 `stopped` → 핸들을 쓰기 전에 나감. rpcrt4 는 a[9] 에 **늘** 8 B 빈 버퍼를 준다 | a[8] NULL · a[9] 의 `AllocatedAttributes == 0` 이면 "속성 없음" 으로 보고 `ValidAttributes = 0` 쓰고 핸들 씀. 속성이 진짜 있을 때만 멈춤 |
| `:346` (`NtAlpcConnectPortEx`) | **같은 이유로 틀림** — Ex 길도 같은 빈 버퍼 | 위와 같음 |
| `:305` (`a[5]` 서버 보안 요구) | Ex 길 한정 — 이번 막힘 원인 아님. Ex 의 a[5] = 호출자 SD 또는 `gDefaultSecurityDescriptor`, plain 의 a[5] = 서버 SID (처음 NULL) | — |
| `:623` (`NtAlpcSendWaitReceivePort`) | **맞음** (메시지 전달 미구현 선언) | ① 을 고치면 처음 쓰인다 — "서버 없음" 답 필요 |
| T1 (어떤 status 로 답할지) | 핸들 없는 성공 = 틀린 답 (상태 칸 3 이 남음) | 서버 모델 **없는** 포트 → **`0xc0000034`** (→ `0x6ba`, 깨끗한 실패, `keysvc` · `eventlog` 18 샘플이 이미 이 꼴). **`0xc0000041` 은 `0xe` 로 읽히니 쓰지 말 것.** 서버 모델 **있는** 포트 → 핸들 쓰고 성공 (비동기면 `NtAlpcSetInformation` 이 이어짐) |
| T2 (nsisvc 등록 성공 흉내) | 8절 ① ~ ⑧ 모두 필요, ③ · ⑥ · 응답 머리는 Q230 | 가장 값싼 길 = epmapper 연결을 `0xc0000034` 로 실패 → IPv4 · IPv6 둘 다 RPC 오류 → `NotifyIpInterfaceChange` 오류 → `WinHttpSendRequest` 0 (**유한하게 끝남**) |
| 공통 몫 (LSA `GetUserName*` · RPCSS `CoInitializeSecurity` 도) | — | 5절 인자 꼴 · 상태 → 오류 표 · 동기 / 비동기 Flags · `NtAlpcQueryInformation(0, 0x10)` · 끝점 없으면 epmapper + `ept_map` · 6절 상태 칸 |
| winnsi 몫 | — | 인터페이스 `7ea70bcf…` v1.0 · 끝점 없음 · 인증 6 / 10 · 프로시저 5 · 6 · 7 · 등록 최대 4번과 "둘 중 하나만 되면 성공" · 성공 뒤 이벤트 · WNF · 스레드풀 대기 |

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 6 — 대기열 **Q231**. test.exe 가 아니라 winnsi 등록을 부르는 프로그램 (예: `NotifyIpInterfaceChange` 를 부르는 작은 exe, 또는 winhttp) — 고르기는 사용자 결정. 유저 멈춤점, 프로세스 조건, 모두 ret 자리):

1. **ALPC 메시지 한 번 뜨기** (nsisvc 도는 VM) — `ntdll!NtAlpcSendWaitReceivePort` 입구 `kv` 로 `rpcrt4!…+0x…` 를 얻고 송신 · 수신 `PORT_MESSAGE` 덤프. 첫 송신 bind → `ept_map` → nsi bind → 프로시저 5 (순서 `[추측]`). Q230 의 시작점.
2. `rpcrt4!LRPC_CASSOCIATION::AlpcConnect+0x29a` — `eax` · `[rsi+0x88]` · `+0x293` 직전 인자 (`[rsp+0x20..+0x50]`), Ex 길이면 `+0x3c0`, plain 인 까닭은 `+0x27a` 의 `rdx`.
3. `winnsi!NsiRpcRegisterChangeNotificationEx+0x259` `eax` · `[rsp+0x60]` — 서버 없음일 때 1722 인가 1752 인가. `+0x1ef` 에서 예외로 나왔는지. `iphlpapi!InternalRegisterChangeNotification+0xa4` / `+0xdc` `eax`, `NotifyIpInterfaceChange+0xa0`.
4. `rpcrt4!LRPC_BASE_BINDING_HANDLE::ResolveEndpoint+0xc9` 직후 `[rdi+0x114]` = 3, `CompleteOnePart(0x5aeec)+0xc7` 이 불리는지, 둘째 부름 `ResolveEndpoint+0x48` 의 `r12d`.
5. 예외 원인 — `ntdll!RtlDispatchException` (또는 `KiUserExceptionDispatcher`) 에서 `.exr` · `kv`. 후보 `CLIENT_IO_PROVIDER::Register+0x267` `INT 0x29`, `EnableAsync` 의 핸들 0.
6. 성공 쪽 (nsisvc 있는 VM) — `+0x259` 뒤 `[rsp+0x60]` = 0, `+0x555` · `+0x6bd`, 슬롯에 쓰이는 컨텍스트 핸들 8 B (`+0x46e` 의 `rcx`).

- (flow-writer 보탬) 7. run128 동적 1 (Q222) 과 같은 실행에서 `winnsi!NsiRpcRegisterChangeNotificationEx+0x119` `eax` — 요청 쪽 로그의 `0x12000f` 가 F1 (winnsi) 것인지 F2 것인지 가른다 (위 "정정").

정적 (결과서 "다음 요청 후보"):

1. **LRPC 선 위 형식** — `LRPC_CASSOCIATION::Bind` · `CompleteBind` · `LRPC_BIND_CCALL::HandleReply` · `LRPC_CCALL::HandleReply` · `SendReceive` · `RpcpAlpcpGetHeaderSize` (rva `0x57650`) · `PreinitRequestAlpcHeaders` · `AlpcSendWaitReceivePort` · `AlpcSend` · `RpcpStrictAlpcToRpcStatus` (rva `0x99d70`) — 머리 칸 · 길이 · 메시지 종류, 핸들 0 보내기의 `0xc0000008` 처리. → **Q230**
2. **nsisvc** analyze (필요한 함수만) — 끝점 이름, 프로시저 5 · 6 · 7 의 입력 검사, 컨텍스트 핸들 만드는 줄. → **Q230**
3. **`ept_map` 응답 검사** — `ProcessNextChunk` · `ReturnLookupValues` · `TowerConstruct` → `OsfTowerConstruct`, 탑 층 번호 · `ncalrpc` 프로토콜 ID. → **Q230**
4. `CreateAlpc` → `ntdll!TpAllocAlpcCompletionEx` → `NtAlpcSetInformation` 모양과 완료 통지 (run125 일꾼 공장 · sast/122 와 이어짐). → **Q232**
5. `RegisterNsiSvcRestartNotification` 의 WNF 상태 이름 (`NtQueryWnfStateData` · 구독). → **Q232**

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서: rpcrt4 · ntdll · webio 는 같은 이름 함수가 여럿이라 `.asm` 이름에 `__<rva>` 가 붙고, `_calls.tsv` 는 같은 이름의 호출을 한 묶음에 섞는다 (예: `ResolveEndpoint +0x166 BindToEpMapper` 는 `0x59d38` 쪽, `+0x12b SubmitResolveEndpointRequest` 는 `0x59258` 쪽). 오프셋은 `.asm` 에서 직접 확인할 것 (기록만, 하네스 제안 아님 — run128 의 같은 메모와 같은 모양).

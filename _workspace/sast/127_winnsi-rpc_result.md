# 127 winnsi-rpc — 정적 분석 결과

- 요청서: `_workspace/sast/127_winnsi-rpc.md` (Q223, PE_emulator 튜닝 T1·T2 — "winnsi 알림 등록 RPC")
- 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/` 아래 바이너리.
- 날짜: 2026-11-22. 새로 analyze 한 것은 **winnsi 하나** (11초). rpcrt4·IPHLPAPI·webio·ntdll·kernelbase 는 다시 analyze 하지 않고 `^이름$` export 와 이미 있는 `.asm` 읽기만 했다. nsisvc 는 analyze 하지 않았다 (질문 3 의 출력 꼴이 클라이언트 코드로 닫혀서).
- 근거 표기: `[정적] <모듈>/<파일>.asm +오프셋` (모두 `ms/disassemble/<모듈>/`). 부르기는 `call 자리 (ret=돌아올 자리)` 둘 다. 이 서버는 PE 데이터 표 (인터페이스 구조체 등) 를 `pefile` 로 직접 읽어 RVA 로 적었다.

## 스크립트 문제

없음. 참고 둘 (고칠 것은 아님):
- rpcrt4·ntdll·webio 는 이름이 같은 함수가 여럿이라 파일 이름에 `__<rva>` 가 붙는다. 이 문서는 `ResolveEndpoint` (바인딩 핸들, rva 0x59258 = `ResolveEndpoint.asm`) / `ResolveEndpoint__59d38.asm` (EP_LOOKUP_DATA), `CompleteOnePart` (연결 객체 0x3b148 = `CompleteOnePart.asm`, 제공자 0x43164 = `__43164`, 바인딩 0x5aeec = `__5aeec`) 처럼 PDB 이름의 클래스를 같이 적는다.
- `_calls.tsv` 는 같은 이름 함수의 호출을 한 줄 묶음에 섞어 놓는다 (예: `ResolveEndpoint +0x166 BindToEpMapper` 는 0x59d38 쪽, `+0x12b SubmitResolveEndpointRequest` 는 0x59258 쪽). 이 문서의 오프셋은 `.asm` 파일에서 직접 확인한 것.

## PDB GUID 확인 (새로 analyze 한 모듈)

| 모듈 | 바이너리 | PDB guid+age | 함수 수 | 로그 |
|---|---|---|---|---|
| winnsi | `winnsi.dll` 67,632 B | `907CE42FFA71F5085D1468AD2D40D62C1` (`llvm-pdbutil dump --summary` 의 GUID `{907CE42F-FA71-F508-5D14-68AD2D40D62C}` 와 같음. PDB 안 Age 3, 심볼 서버 키는 바이너리의 Age 1 — sast/126 의 ws2_32 와 같은 모양) | 86 (`_index.tsv`) | `ms/ghidra/winnsi.analyze.log` — `SetPdb:` 줄 있음, 함수 이름이 PDB 이름 (`NsiConnectToServer`, `NsiRpcRegisterChangeNotificationEx`) 으로 잡힘 |

- 커널 GUID (`C29EBFB0…`) 는 ntoskrnl 만 해당이라 이번엔 해당 없음.
- 공개 PDB 라 구조체 타입 이름이 없다 (이름은 모두 코드 줄에서 읽은 것).
- winnsi 는 `rpcrt4.dll` 과 `NSI.dll` 을 **지연 가져오기** 로 쓴다 (`pefile` 지연 표: `Ndr64AsyncClientCall`, `RpcAsyncInitializeHandle`, `RpcBindingFromStringBindingW`, `RpcBindingSetAuthInfoW`, `RpcStringBindingComposeW`, `I_RpcExceptionFilter`, `RpcAsyncCompleteCall`, `RpcAsyncCancelCall`, …). 첫 호출 때 `__delayLoadHelper2` 가 로드한다.

## 버전·스크립트 대조

"알려진 사실" 표에 winnsi·rpcrt4 줄이 없다. 대신 `.asm` 의 `+0x` 를 **원본 바이트** 와 맞췄다 (스크립트 오프셋 계산 확인):
- `winnsi NsiConnectToServer +0x3a` = `48 ff 15 …` (7바이트 `CALL [rip+…]`, ret `+0x41`), `NsiRpcRegisterChangeNotificationEx +0x1e8` / `+0x239` / `+0x112` 도 같은 꼴 (ret `+0x1ef` / `+0x240` / `+0x119`) — 일치.
- `rpcrt4 AlpcConnect +0x293` (`NtAlpcConnectPort`, ret `+0x29a`), `+0x3b9` (`NtAlpcConnectPortEx`, ret `+0x3c0`) = `48 ff 15 …`; `+0x243` = `48 83 65 70 00` (`AND qword [RBP+0x70],0`); `+0x25a` = `48 8d 45 70 / 48 89 44 24 48` (`LEA RAX,[RBP+0x70]` / `MOV [RSP+0x48],RAX`); `+0x2c8` = `81 ff 41 00 00 c0` (`CMP EDI,0xc0000041`) — 일치.
- `rpcrt4 ResolveEndpoint +0xc9` = `c7 87 14 01 00 00 03 00 00 00` (`MOV [RDI+0x114],3`), `+0x30e` = `bb 18 10 02 c0` (`MOV EBX,0xc0021018`), `CompleteOnePart(바인딩 0x5aeec) +0xc7` = `89 ab 14 01 00 00` (`MOV [RBX+0x114],EBP`) — 일치.
- `IPHLPAPI InternalRegisterChangeNotification +0xe7` = `85 ff 74 08 f7 d8 1b c0 23 c7 eb 09` (`TEST EDI,EDI / JZ / NEG EAX / SBB EAX,EAX / AND EAX,EDI`), `webio Startup +0xbc` = `85 c0 0f 85 14 01 00 00`, `ntdll RtlReportException +0x54` = `e8 …` (ret `+0x59`) — 일치.
- **판정: 일치.** "스크립트 문제" 없음.

---

## 먼저 읽는 한 줄 답

**먼저 바로잡을 것 — 로그의 "첫 등록 / 둘째 등록" 은 서로 다른 API 두 번이 아니라 `NotifyIpInterfaceChange(Family 0)` 한 번 안의 IPv4 부름 → IPv6 부름이다.** `iphlpapi!InternalRegisterChangeNotification` 가 계열 0 이면 `NsiRpcRegisterChangeNotification` 을 IPv4 모듈 ID 로 `+0x9d` (ret `+0xa4`), 이어서 IPv6 모듈 ID 로 `+0xd5` (ret `+0xdc`) 부른다 (`iphlpapi/InternalRegisterChangeNotification.asm`). `NotifyUnicastIpAddressChange` 는 `NetioRegisterAddressChangeNotification +0x9d` 로 같은 함수를 다시 부르니 (객체 번호 `0xa`; 인터페이스 쪽은 `7` — `NotifyIpInterfaceChange +0x3f`), webio `Startup` 한 번이 RPC 등록을 **최대 4번** (인터페이스 v4·v6, 유니캐스트 v4·v6) 한다.

**질문 1 (winnsi)**
1. **바인딩 — 끝점은 고정이 아니다. 끝점 없이 epmapper 로 푼다.** `NsiConnectToServer` 는 `RpcStringBindingComposeW(NULL, "ncalrpc", NULL, NULL, "Security=Impersonation Dynamic True")` (`+0x3a`, ret `+0x41`) → `RpcBindingFromStringBindingW` (`+0x70`, ret `+0x77`) → `RpcBindingSetAuthInfoW(…, 인증 수준 6, 인증 서비스 10, NULL, 0)` (`+0xc7`, ret `+0xce`) 뿐이다. 끝점 문자열이 없다. 인터페이스 구조체의 기본 끝점도 0개 (`RpcProtseqEndpointCount` = 0, rva 0x67b0+0x38). 그래서 rpcrt4 가 `\RPC Control\epmapper` 에 연결해 `ept_map` 으로 끝점을 푼다 — **로그의 `NtAlpcConnectPort \RPC Control\epmapper` 는 nsisvc 서버 연결이 아니라 "끝점 풀이" 연결이다.** `RpcBindingFromStringBindingW` 는 연결하지 않는다 (바인딩 핸들만 만든다).
2. **비동기 RPC.** `NsiRpcRegisterChangeNotificationEx` 는 `RpcAsyncInitializeHandle(…, 0x70)` + 이벤트 통지 (`NotificationType = Event`) 로 `Ndr64AsyncClientCall` 을 부른다. 인터페이스 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0, 제시하는 전송 문법 둘 (NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860`, NDR64 `71710533-beba-4937-8319-b5dbef9ccc36`). 프로시저 9개 (0~8) 중 **5 = 등록 (`+0x1e8`), 7 = 알림 기다림 (`+0x54e`), 6 = 해제 (`+0x83a`)**.
3. **부르기 앞 이벤트 (seq 2199·2230)**: winnsi 가 만든다 — `NsiRpcRegisterChangeNotificationEx +0x168` (ret `+0x16f`) `CreateEventW(NULL, FALSE, FALSE, NULL)`. **자동 리셋** (수동 아님) 이고 "비동기 RPC 완료 통지 이벤트" 다 (연결 잠금 아님). 세우는 쪽은 rpcrt4 `IssueNotification` (rva 0x345c4) `+0xc5` 의 `SetEvent`. winnsi 는 `+0x239` (ret `+0x240`) 에서 `WaitForSingleObject(…, INFINITE)`. (seq 2196 의 이벤트는 sast/126 의 `nsi!NsiGetAllParameters` 가 만든 IOCTL 용.)
4. **RPC 실패값·치명 여부**: 래퍼는 RPC 오류 코드를 그대로 돌려준다 (정확한 숫자는 아래 2·표 참고, 서버 없음이면 대개 `0x6ba` 로 짐작 `[추측]`). `InternalRegisterChangeNotification` 은 **계열 0 이면 IPv4·IPv6 둘 다 실패일 때만** IPv4 의 오류를 돌려준다 (하나라도 0 이면 0). 그 오류가 `NotifyIpInterfaceChange` → webio `Startup +0xbc/+0xbe` 에서 **치명** (HRESULT `0x8007xxxx` 로 바꿔 올려 보냄 — `WinHttpSendRequest` = 0, sast/126 A1 F1).

**질문 2 (rpcrt4 LRPC 클라이언트)**
1. `AlpcConnect` 는 `NtAlpcConnectPort` 상태를 표 (아래 "2-1 표") 대로 RPC 오류로 바꾼다. **`0xc0000034` → `0x6ba` (1722, SERVER_UNAVAILABLE)**, `0xc0000041` (연결 거부) → **`0xe` (메모리 부족)**, 표에 없는 상태 → `0x6ba`. 다시 시도는 `0xc00002a0` (서버 SID 불일치) 하나뿐인데, 서버 SID 를 지정해 연결했을 때만 생긴다 (첫 연결은 SID 없음 → 안 일어남). 다른 상태는 재시도·기다림 없음.
2. **"성공인데 포트 핸들 0"**: 핸들은 `NtAlpcConnectPort` 의 첫 인자가 연결 객체 `this+0x88` 의 주소라 거기에 직접 써진다 (`AlpcConnect +0x23c LEA R10,[RSI+0x88]` → `+0x290 MOV RCX,R10`). 상태 0 이면 rpcrt4 는 **핸들 값을 검사하지 않고** (`+0x379 TEST EDI,EDI`) `IsPortWithSharedSection(this+0x88)` 만 부른다 (핸들 0 이면 `NtAlpcQueryInformation` 도 안 부르고 1 을 돌려줌). 그 뒤 비동기면 `EnableAsync → CreateAlpc → TpAllocAlpcCompletionEx(핸들 0)` 로 간다. **둘째 호출이 영원히 기다리는 이유 (코드로 설명되는 것)**: 첫 호출이 바인딩 핸들 상태 칸 `[바인딩+0x114]` 를 3 (끝점 푸는 중) 으로 올린 뒤 (`ResolveEndpoint +0xc9`) 4 나 1 로 되돌리는 줄 (`CompleteOnePart(0x5aeec) +0xc7`) 에 닿지 못하면, 둘째 호출은 "기다리는 사람"으로 등록하고 보류 값 `0xc0021018` 을 돌려받는다 (`ResolveEndpoint +0x30e`) → winnsi 는 `+0x239` 에서 INFINITE 로 기다리는데 깨울 자가 없다. 로그의 "둘째 등록 = `NtAlpc*` 0, 이벤트 무한 대기" 와 맞는다. 첫 호출이 왜 `CompleteOnePart` 에 못 닿았는지는 `[공백]` (2-4 참고).
3. **seq 2205~2224 는 rpcrt4 가 부르는 것이 아니다.** 윈도우 오류 보고 (WER) 길: `NtQueryInformationProcess 0x25` = `ntdll!WerpIsProcessNative +0x2d`, `NtSetInformationProcess 0x3f` = `ntdll!WerpSetProcessFaultInformation +0x2c` (또는 `kernelbase!UnhandledExceptionFilter +0x110/+0x282`), `NtCreateSection` = `WerpCreateCrashDataSection +0xa8`, `NtDuplicateObject ×2` = `RtlReportExceptionEx +0x172/+0x1a9`, `SystemErrorPortReady` = `kernelbase!WaitForWerSvc` (← `RaiseFailFastException +0x12e`). 이 길은 **rpcrt4 의 예외 필터가 "치명" 으로 거른 예외** (접근 위반 `0xc0000005` 등 14종 — `RpcpIsFatalError` 표) 가 바깥으로 나갔을 때 열린다. 무슨 예외가 어디서 났는지는 정적으로 못 닫음.

**질문 3 (서버 쪽 최소 — 틀만)**
1. **epmapper 에 묻는다.** 끝점이 없으니 `EP_LOOKUP_DATA` 가 EPM 인터페이스 (`e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0) 로 `ept_map` 을 부른다: 동기 `NdrClientCall3` 프로시저 3 / 7, **비동기 (이번 경우) `Ndr64AsyncClientCall` 프로시저 8** (`LookupNextChunk +0x11d`, ret `+0x122`). 묻는 것 = "인터페이스 7ea70bcf… + 전송 문법 + `ncalrpc`" 탑 (tower), 받는 것 = 탑 목록 (끝점 이름) + 상태.
2. **등록 한 번의 출력 꼴**: 프로시저 5 는 입력 (`{예약 0, 모듈 ID 포인터, 객체 번호}` 구조체 · compartment 8바이트 값) → 출력 **컨텍스트 핸들 하나** (네트워크 위 20바이트) + 반환값 DWORD (0 = 성공). 성공이면 클라이언트는 프로시저 7 (그 컨텍스트 핸들을 들고, 알림이 올 때까지 안 끝나는 비동기 호출) 을 바로 보내고 끝난다. **서버가 프로시저 7 에 답하지 않아도 등록은 성공으로 끝난다.**
3. **LRPC 선 위 (bind·요청·응답 머리 칸·길이·종류 값) 는 이번에 안 읽었다 — 3 의 이 부분은 "다음 요청으로".** 이름 후보와 쓰임은 맨 끝 "다음 요청 후보".

**지금 우리 (PE_emulator)**: `nt_alpc_create_port.cpp:192`·`:346` **틀림** — rpcrt4 의 `NtAlpcConnectPort` 는 **항상** `InMessageAttributes` (a[9]) 를 8바이트 빈 버퍼 (`AllocatedAttributes 0`) 가리키게 넘긴다 → 에뮬레이터가 매번 "속성 있음" 으로 멈춘다 (= 핸들 없는 성공). `:305` 는 조건부 (Ex 길만, 아래 4 절). `:623` 은 맞음 (메시지 전달 미구현 그대로). 자세한 건 4 절.

---

## 1. winnsi — 부르는 줄과 바인딩·호출 모양

### 1-1. 사슬 (지도 기준) — 첫 등록 한 번

표기: `함수 +호출위치 (ret=돌아올 자리)`.

| 단계 | 함수 | 호출 줄 | 근거 |
|---|---|---|---|
| 1 | `webio!Startup` (rva 0x4abe8) | `+0xad` (ret `+0xb4`) → `iphlpapi!NotifyIpInterfaceChange(0, 콜백, ctx, FALSE, &핸들)` | `webio/Startup.asm` |
| 2 | `iphlpapi!NotifyIpInterfaceChange` (0x9880) | `+0x9b` (ret `+0xa0`) → `InternalRegisterChangeNotification`. 0x1c8 바이트 블록을 만들고 `[+8] = 7` (`+0x3f`) | `iphlpapi/NotifyIpInterfaceChange.asm` |
| 3 | `InternalRegisterChangeNotification` (0x9758) | `+0x5e` (ret `+0x63`) → `NsiNotificationConnectToServer`; 반환 0 이면 6 (ERROR_INVALID_HANDLE) 로 끝 (`+0x66~+0x6e`) | `iphlpapi/InternalRegisterChangeNotification.asm` |
| 4 | `NsiNotificationConnectToServer` (0x1b18c) | 전역 `NotificationRpcHandle` (rva 0x2d8a0) 가 0 일 때만 `+0x26` (ret `+0x2d`) `winnsi!NsiConnectToServer(NULL)` — **한 번 만든 바인딩 핸들을 전역에 두고 계속 쓴다** (`+0x32` 저장). 실패하면 `GetLastError` 를 돌려주고 핸들은 NULL 로 남아 다음 호출이 다시 만든다 | `iphlpapi/NsiNotificationConnectToServer.asm` |
| 5a | 같은 함수 IPv4 | `+0x9d` (ret `+0xa4`) → `winnsi!NsiRpcRegisterChangeNotification(바인딩, NPI_MS_IPV4_MODULEID, 객체 번호, 콜백, 초기통지, ctx, &슬롯)` | `…InternalRegisterChangeNotification.asm` |
| 5b | 같은 함수 IPv6 | `+0xd5` (ret `+0xdc`) → 같은 함수, `NPI_MS_IPV6_MODULEID` (계열 2 면 건너뜀 `+0xab`, 계열 23 이면 IPv4 를 건너뜀 `+0x73`) | 〃 |
| 6 | `winnsi!NsiRpcRegisterChangeNotification` (0x1180) | `+0x5b` (ret `+0x60`) → `…Ex` (0x11f0). 인자를 스택 구조체 (0x48 바이트) 로 모은다: `+0` 0, `+8` 모듈 ID 포인터, `+0x10` 객체 번호, `+0x18` 콜백, `+0x20` 초기통지 (바이트), `+0x28` ctx, `+0x30` compartment 칸 (0), `+0x40` 슬롯 포인터 | `winnsi/NsiRpcRegisterChangeNotification.asm` |
| 7 | `winnsi!NsiRpcRegisterChangeNotificationEx` | 아래 1-3 | `winnsi/NsiRpcRegisterChangeNotificationEx.asm` |

- 같은 함수를 **IPv4 → IPv6 로 두 번** 부르는 것이 로그의 "첫 등록 / 둘째 등록" 이다 (계열 0).
- 반환값 규칙 (`InternalRegisterChangeNotification +0xe7 ~ +0xf3`): 계열 2 → IPv4 결과. 계열 23 → IPv6 결과. **계열 0 → IPv4 결과가 0 이면 0, 아니면 IPv6 결과가 0 이면 0, 둘 다 실패면 IPv4 의 오류.**

### 1-2. `NsiConnectToServer` (rva 0x2a10) — 줄 요약

`[정적] winnsi/NsiConnectToServer.asm`

| 줄 | 하는 일 |
|---|---|
| `+0x12 TEST RCX / +0x15 JNZ → +0x58` | 인자 (서버 이름) 가 0 이 아니면 아무것도 안 하고 0 반환 (원격 미지원) |
| `+0x3a` (ret `+0x41`) | `RpcStringBindingComposeW(NULL, "ncalrpc" (rva 0x73a8), NULL, NULL, "Security=Impersonation Dynamic True" (rva 0x7360), &문자열)` — 끝점·네트워크 주소 없음. 문자열 모양은 `ncalrpc:[,Security=Impersonation Dynamic True]` 로 짐작 `[추측]` |
| `+0x70` (ret `+0x77`) | `RpcBindingFromStringBindingW` — 핸들만 만든다 (연결 안 함) |
| `+0xc7` (ret `+0xce`) | `RpcBindingSetAuthInfoW(핸들, NULL, 6, 10, NULL, 0)` (`+0xc3 LEA R8D,[RDX+6]`, `+0xbf LEA R9D,[RDX+0xa]`) — 인증 수준 6 (PKT_PRIVACY), 서비스 10 (WINNT) |
| 실패 | `SetLastError(RPC 오류)` + 0 반환 (`+0x4c`, `+0x95`, `+0xf1` 의 `SetLastError`; 설정 실패면 `+0xde RpcBindingFree`) |

### 1-3. `NsiRpcRegisterChangeNotificationEx` (rva 0x11f0) — 흐름

`[정적] winnsi/NsiRpcRegisterChangeNotificationEx.asm`. 부르기마다 이벤트·할당은 아래.

1. 검사 (`+0xa0 ~ +0xb6`): 바인딩·구조체가 NULL 이거나 (`+8` 도 `+0` 도 0) 이면 `0x57` (`+0x70b`).
2. **compartment 칸이 비었으면** (`+0xc8`, `+0xce`) `nsi!NsiGetAllParameters(1, NPI_MS_NDIS_MODULEID, 8, NULL, 0, &RW 8바이트, 8, 0,0,0,0)` (`+0x112`, ret `+0x119` = IOCTL `0x12000f`) 를 먼저 부른다 (sast/126 A2 와 같은 호출). 0 이 아니면 그 값으로 끝 (`+0x120 JNZ +0x636`). 0 이면 RW 의 두 dword 를 서로 바꿔 구조체 `+0x30`/`+0x34` 에 쓴다 (`+0x126 ~ +0x132`). → **이 호출이 로그의 "Nsi `0x12000f`" (2197·2228) 다.** 등록마다 한 번씩 (`[+0x30]` 이 0 으로 시작하므로).
3. `RpcAsyncInitializeHandle(&상태, 0x70)` (`+0x143`, ret `+0x14a`) → `NotificationType = 1` (이벤트) (`+0x153`) → `CreateEventW(NULL, 0, 0, NULL)` (`+0x168`, ret `+0x16f`) = **자동 리셋 이벤트** (`NtCreateEvent` 의 종류 = 동기화 이벤트, `EventType 1`; `CreateEventW` 표준 대응). 인자: `+0x15e R9D=0` (이름 없음), `+0x161 R8D=0` (초기 상태 꺼짐), `+0x164 EDX=0` (수동 리셋 아님). 요청서가 seq 2199 를 "수동 리셋" 이라 적었다면 로그의 `EventType` 숫자 읽는 쪽 (0 = NotificationEvent = 수동, 1 = SynchronizationEvent = 자동) 을 확인할 것 — 이 줄의 코드는 자동이다. 수동 리셋은 성공 길의 `+0x42c` 하나뿐.
4. `Ndr64AsyncClientCall(프록시 정보 rva 0x6000, 프로시저 5, NULL, &상태, 바인딩, &{예약·모듈ID 포인터·객체번호} (24바이트 복사), compartment 8바이트, 슬롯 포인터)` (`+0x1e8`, ret `+0x1ef`). **`__try / __except(I_RpcExceptionFilter(code))`** 로 감싸져 있다 (`$filt$0`, `+0xe` 에서 `I_RpcExceptionFilter`). 예외가 잡히면 코드가 상태 변수 `[RSP+0x60]` 에 들어가 정리 길 (`+0x664`) 로 간다 `[추측: 착지 코드는 `.asm` 맨 끝의 "읽지 않은 틈 `+0x1fa~+0x223`" 로 보임 — 원본 바이트로 안 확인]`.
5. 예외 없이 돌아오면 `WaitForSingleObject(이벤트, INFINITE)` (`+0x239`, ret `+0x240`) → `RpcAsyncCompleteCall(&상태, &반환값)` (`+0x252`, ret `+0x259`). 이 호출이 0 이 아니면 그 값이 결과 (`+0x25e JNZ +0x72f`), 0 이면 서버가 돌려준 DWORD (`[RSP+0x60]`) 가 0 이어야 성공 (`+0x266 / +0x26c`).
6. **성공이면** (등록 블록 만들기, 서버 없이는 안 가는 줄): `HeapAlloc 0xb0` (`+0x286`) 블록, 비동기 상태 둘 (`+0x2d7`, `+0x37b`; 각각 `RpcAsyncInitializeHandle` + 자동 리셋 `CreateEventW` `+0x347`, `+0x3eb`), **수동 리셋 이벤트** `CreateEventW(NULL, 1, (초기통지 == FALSE), NULL)` (`+0x42c`; 호출자가 `InitialNotification = FALSE` 를 넘기면 켜진 채 시작), `RegisterWaitForSingleObjectEx(둘째 이벤트, NsiWorkerThread (rva 0x1010), 블록, INFINITE, 0x10)` (`+0x4e0`), **프로시저 7** 비동기 호출 (`+0x54e`, ret `+0x555` — 끝나지 않고 걸어 둠), 슬롯에 블록 포인터를 써 줌 (`+0x5ba ~ +0x5be`), `NotificationListLock` 잡고 목록에 넣기 (`+0x5c8` ~ `+0x612`; 목록이 비어 있던 첫 번째면 `RegisterNsiSvcRestartNotification` `+0x6b8` — WNF 구독 `NtQueryWnfStateData`·`RtlSubscribeWnfStateChangeNotification` + `CreateEventW` + `RegisterWaitForSingleObjectEx` — `winnsi/RegisterNsiSvcRestartNotification.asm`). 마지막에 비동기 이벤트를 `CloseHandle` (`+0x626`, ret `+0x62d`).
7. **실패 정리** (`+0x664`): 블록 `HeapFree`, 해제 호출 (프로시저 6, `+0x83a`) 등. 이벤트는 같은 `CloseHandle` (`+0x61e` 합류) 로 닫는다 → 로그의 "이벤트 닫음 (2226)".

### 1-4. 프로시저 모양 (NDR64 형식 문자열, `pefile` 로 rva 0x66e0·0x6590·0x6360 해독)

| 번호 | 쓰는 곳 (winnsi) | 들어감 | 나옴 |
|---|---|---|---|
| 5 | `NsiRpcRegisterChangeNotificationEx +0x1e8`, `NsiReregisterChangeNotification` | 0x18 바이트 구조체 (포인터가 들어 있는 모양: 모듈 ID 24바이트를 따라감) · compartment 8바이트 (값) | **컨텍스트 핸들 (out)**, 반환 DWORD |
| 6 | 해제 (`NsiRpcDeregisterChangeNotificationEx +0x12e`, 실패 정리 `+0x83a`) | 0x18 바이트 구조체 · 컨텍스트 핸들 (in/out) | 반환 DWORD |
| 7 | `+0x54e`, `NsiWorkerThread +0xfe` (알림이 와서 끝날 때마다 다시 건다) | 컨텍스트 핸들 (in) | 16바이트 구조체 · 24바이트 모듈 ID 모양 · DWORD (out) · 반환 DWORD |
| 0~4 | `NsiRpcGetParameterEx` 0, `GetAllParametersEx` 1, `EnumerateObjectsAllParameters` 2, `SetParameterEx` 3, `SetAllParametersEx` 4 | 알림 아님 (이번 질문 밖) | |
| 8 | 이 모듈에서 부르는 곳 못 찾음 | | `[공백]` |

- 컨텍스트 핸들 종류는 형식 문자열의 `0x70` 바이트 (bind-context) 와 깃발로 읽었다 (5 의 출력 `70 a0`, 7 의 입력 `70 41`, 6 의 입출력 `70 e0`) `[추측: 종류 이름]`.
- 슬롯 (`out`) 은 iphlpapi 가 준 `[RBX+0x10]` (IPv4) / `[RBX+0x18]` (IPv6) 의 주소다. 서버가 쓴 컨텍스트 핸들을 먼저 블록 `+0x28` 에 옮겨 놓고 (`+0x46b ~ +0x46e`), 슬롯에는 마지막에 **블록 포인터를 덮어쓴다** (`+0x5ba`). 그 뒤 iphlpapi 는 슬롯 (= 블록 포인터) 을 `CancelMibChangeNotify2` 의 `NsiRpcDeregisterChangeNotification` 에 넘긴다 (`iphlpapi/CancelMibChangeNotify2.asm +0x47 ~ +0xb3`).

---

## 2. rpcrt4 LRPC 클라이언트의 연결 길

### 2-1. `NtAlpcConnectPort` 상태 → RPC 오류 표

`[정적] rpcrt4/AlpcConnect.asm` (`LRPC_CASSOCIATION::AlpcConnect`, rva 0x38890; 결과는 `EBX`, 끝 `+0x10e~+0x11e` 에서 반환). 상태 이름은 확실한 것만 적었다 (`[이름 추측]` = 번호만 확인).

| NtAlpcConnectPort 상태 | RPC 오류 (반환값) | 다시 시도 / 기다림 | 줄 |
|---|---|---|---|
| `0` (성공) | **동기 호출**: `0`. **비동기 호출**: `0xc0021018` (보류, rpcrt4 내부) + `*출력 = 1`. 그 전에 `IsPortWithSharedSection(this+0x88)` | 비동기면 호출한 쪽 (`Connect`) 이 `EnableAsync` 로 ALPC 완료 통지를 건다 (2-2) | `+0x379 TEST EDI,EDI`, `+0x381 ~ +0x3a5`, `+0x743`, `+0x752 ~ +0x75b` |
| `0xc0000034` (OBJECT_NAME_NOT_FOUND) | **`0x6ba` (1722 RPC_S_SERVER_UNAVAILABLE)** | 없음 | `+0x2ee CMP`, `+0x2f4 JZ → +0x30e MOV EBX,0x6ba` |
| 표에 없는 모든 상태 | `0x6ba` | 없음 | `+0x30e` 가 기본 착지 (`+0x72d CMP EDI,0x102 / JNZ +0x30e` 도) |
| `0xc0000041` (PORT_CONNECTION_REFUSED) | **`0xe` (14, 메모리 부족)** — "연결 거부" 가 메모리 부족으로 읽힌다 | 없음 | `+0x2c8 CMP`, `+0x2d0 JZ → +0x790 MOV EBX,0xe` |
| `0xc0000017` (NO_MEMORY) · `0xc0000044` (QUOTA_EXCEEDED) | `0xe` | 없음 | `+0x2d6/+0x2dc`, `+0x349/+0x34f` → `+0x790` |
| `0xc0000022` (ACCESS_DENIED) · `0xc00000a5` (BAD_IMPERSONATION_LEVEL) | `5` (접근 거부) | 없음 | `+0x2e2/+0x2e8`, `+0x33d/+0x343` → `+0x497 MOV EBX,5` |
| `0xc0000036` `[이름 추측]` | `0x72a` (1834) | 없음 | `+0x2f6/+0x2fc` → `+0x786 MOV EBX,0x72a` |
| `0xc0000039` `[이름 추측: OBJECT_PATH_INVALID]` | `0x6aa` (1706 RPC_S_INVALID_ENDPOINT_FORMAT) | 없음 | `+0x302/+0x308` → `+0x770` (안의 `CMP EDI,0xc0000037` 은 이 길에선 절대 안 맞는 죽은 비교) → `+0x77c MOV EBX,0x6aa` |
| `0xc000009a` (INSUFFICIENT_RESOURCES) | `0x6b9` (1721 RPC_S_OUT_OF_RESOURCES) | 없음 | `+0x355/+0x35b` → `+0x766` |
| `0x102` (TIMEOUT) · `0xc0000701` `[이름 추측]` | `0x71a` (1818 RPC_S_CALL_CANCELLED) | 없음 | `+0x36d/+0x373`, `+0x72d/+0x733` → `+0x739` |
| `0xc00002a0` (SERVER_SID_MISMATCH `[이름 추측]`) | 아래 재시도, 안 되면 내부 `0xc0021017` → `Connect` 가 `5` 로 바꿈 (`Connect +0x33f`) | **서버 SID 를 지정한 연결에서만.** 처음이면 머신 계정 SID 를 얻고 (`LrpcPrepareMachineAccountSid` `+0x5fb`, 비동기면 `LrpcRetryAlpcConnect` 로 이어짐) 요청한 서버 SID 가 그것과 같으면 LocalSystem 으로 다시 (`+0x6ac`), 이후 불일치마다 LocalService (`+0x697`) → NetworkService (`+0x688`) 로 바꿔 `NtAlpcConnectPort(Ex)` 를 다시 (`+0x44f JMP +0x23c`). 조건이 안 맞으면 (`+0x40e CMP [RBP-0x78]`, `+0x6bb`) 재시도 없이 `0xc0021017` | `+0x361`, `+0x367` → `+0x40e ~ +0x44f` |

- 반환 직전 (`+0x10e TEST [RSI+0xd0],0x400`): 연결 객체 깃발 `0x400` 이 켜져 있으면 `5` 와 `0xc0021017` 도 `0x6ba` 로 바꾼다 (`+0x454 ~ +0x465`) — 엄격 ALPC→RPC 상태 매핑 (`RpcpStrictAlpcToRpcStatus`, rva 0x99d70, 안 읽음).
- **`NtAlpcConnectPort` 를 부를 때의 인자** (T1 에 필요): `+0x293` `NtAlpcConnectPort(&this+0x88, 포트 이름, 객체 속성, 포트 속성, 깃발, 서버 SID, 연결 메시지, 버퍼 길이, 나가는 속성, 들어오는 속성, 타임아웃)`:

| 인자 | 값 | 줄 |
|---|---|---|
| PortHandle | `this+0x88` (연결 객체 안, 처음 0) | `+0x23c LEA R10,[RSI+0x88]`, `+0x290` |
| PortName | `\RPC Control\` + 끝점 (이름에 이미 `\RPC Control\` 이 있으면 그대로, 없으면 앞에 붙임 — `+0xb4 wcsstr`, `+0xf7 DuplicateString3`) | `+0x14b RtlInitUnicodeString` |
| ObjectAttributes | 0x30 바이트, **Length 만 0x30, 나머지 0** | `+0x197 MOV [RBP-0x40],0x30`, `+0x26c LEA R8,[RBP-0x40]` |
| PortAttributes | 0x48 바이트: Flags `0x2090000` (연결 객체 `+0x48` 이 1/2/3 이면 `0x2290000`/`0x2490000`/`0x2890000`), SecurityQos {길이 0xc, 가장 수준 = `[this+0xd8]-1`, 추적 `[this+0xd4]`, effective-only `[this+0xdc]`}, MaxMessageLength `0x1000`, MemoryBandwidth 0, MaxPoolUsage·MaxSectionSize·MaxViewSize·MaxTotalSectionSize 모두 `0xffffffff` (qword 로 `0x00000000ffffffff`, `+0x182 MOV EAX,0xffffffff` 를 그대로 씀), DupObjectTypes `0xffd` | `+0x1b9 ~ +0x1fa` (`[RBP+0x20..+0x60]`), `+0x263 LEA R9,[RBP+0x20]` |
| Flags | **동기 호출이면 `0x20000`, 비동기 호출이면 `0`** (`ALPC_SYNC_CONNECTION` 으로 짐작 `[추측: 상수 이름]`) | `+0x211 TEST R14D / JNZ +0x406`, `+0x225 MOV ECX,0x20000` |
| RequiredServerSid | `[this+0xc0]` (처음 NULL) | `+0x22e`, `+0x283` |
| ConnectionMessage / BufferLength / OutMessageAttributes | **0 / 0 / 0** | `+0x275`, `+0x270`, `+0x267` |
| **InMessageAttributes** | **8바이트 빈 버퍼의 주소** (`AllocatedAttributes = 0, ValidAttributes = 0`, `+0x243 AND qword [RBP+0x70],0`) — **항상 비-NULL** | `+0x25a LEA RAX,[RBP+0x70]`, `+0x25e MOV [RSP+0x48],RAX` |
| Timeout | **동기 호출에서만**: NULL, 또는 `−(밀리초 × 10000)` (`0xffffffff` ms = `0x8000000000000000` 무한). 비동기 호출은 항상 NULL | `+0x21a ~ +0x22a`, `+0x509 ~ +0x531` |

- **`NtAlpcConnectPortEx` 로 가는 조건**: 서버 SID 가 없고 (`[this+0xc0] == 0`) 호출자가 SD 를 안 줬을 때 전역 `gDefaultSecurityDescriptor` 로 채우는데 (`+0x3f2 CMOVZ`), 그게 비-NULL 이면 `NtAlpcConnectPortEx` (`+0x3b9`, ret `+0x3c0`) 를 쓴다 (`+0x27a TEST RDX / JNZ +0x3a9`). 로그가 plain `NtAlpcConnectPort` 라면 그 전역이 NULL 이거나 서버 SID 가 이미 있다 `[공백: 어느 쪽인지]` (동적: `+0x27a` 직전 `rdx`).

### 2-2. 연결 객체·바인딩 핸들에 남는 칸과 기다리는 줄

**연결 객체 `LRPC_CASSOCIATION`** (`this`): `+0x4c` 상태 (1 처음 / 3 연결 중 / 4 연결됨 / 2·5 종료), `+0x88` 포트 핸들, `+0x148` (−1 = 없음), `+0xc0` 서버 SID, `+0xd0` 깃발 (bit0 = 취소됨, bit3·4 = `EnableAsync` 됨, `0x400` = 엄격 매핑), `+0xe8` 스레드풀 ALPC 객체, `+0x240` (`0xff00` = 공유 섹션 없는 포트), `+0x20` 기다리는 사람 제공자.

`LRPC_CASSOCIATION::Connect` (rva 0x48018, `rpcrt4/Connect.asm`):
- 상태 4 (이미 연결) 면 바로 끝 (비동기면 `EnableAsync`). 상태 1 이면 `+0x10c` `[this+0x4c] = 3` 을 쓰고 `CLIENT_IO_PROVIDER::Register` (`+0xd2`, ret `+0xd7`) 에 자기를 올린 뒤 **`AlpcConnect` (`+0x198`, ret `+0x19d`)**. 상태 3 (다른 호출이 연결 중) 이면 자기를 기다리는 사람으로 올리기만 하고: 동기면 `SyncWait` (`+0x4e5`, ret `+0x4ea`), 비동기면 보류 `0xc0021018` 반환.
- **동기 + 성공/실패**: `CompleteOnePart` (`+0x1c5`, ret `+0x1ca`) 가 결과를 반영한다. 연결 객체 `CompleteOnePart` (rva 0x3b148, `rpcrt4/CompleteOnePart.asm`): 성공 0 이면 **`+0x26e` `[this+0x4c] = 4`**. 실패면 포트 핸들을 `NtClose` 하고 `[this+0x88] = 0`, `[this+0x148] = −1`, 스레드풀 객체 해제, 그리고 **상태 2 (실패로 굳음: `+0x1a0`, 오류를 `[this+0x118]` 에, 오류 정보 사슬을 `[this+0x120]` 에 보관)** — 단 상태 코드가 `0x71a` (취소) 이거나 `0xc0021017` (내부) 이면 **상태 1 (다시 시도 가능, `+0x2aa`)**. 끝에 `SignalAllIos` 로 기다리던 모두를 깨운다. 상태 2 인 연결 객체로 다시 `Connect` 가 오면 새로 연결하지 않고 보관된 오류를 돌려준다 (`Connect +0x1cf` `GetShutdownError`, 상태 2·5).
- **비동기 + 성공**: `AlpcConnect` 가 `0xc0021018` + `*출력 = 1` 로 돌아오면 (`+0x328 CMP EAX,0xc0021018 / JZ +0x3a4`) **`EnableAsync` (`+0x3b1`, ret `+0x3b6`)**: `RPC_THREAD_POOL::CreateAlpc(this+0x88 의 값, …)` (`EnableAsync +0x6a`, ret `+0x6f`) → `TpAllocAlpcCompletionEx(&결과, 포트 핸들, LrpcIoComplete, …)` (`CreateAlpc +0x66`, ret `+0x6d`). 0 이면 보류 반환 (`+0x377`). 실패면 `NtClose(this+0x88)` (`+0x403`, ret `+0x40a`) · `[this+0x88] = 0` (`+0x416`) · 스레드풀 객체 해제 · `[this+0x148] = −1` (`+0x42e`) 하고 `CompleteOnePart` (`+0x35a`) 로 오류를 알린다.
- **비동기 + 실패**: `+0x32f ~ +0x35a` 에서 `[this+0x148] = −1`, `[this+0x88] = 0` 을 쓰고 `CompleteOnePart(오류)` 로 알린 뒤 보류 값 `0xc0021018` 을 돌려준다 (오류는 완료 통지로 간다).

**바인딩 핸들 `LRPC_BASE_BINDING_HANDLE`**: `+0x114` 상태 (1 처음 / 3 끝점 푸는 중 / 4 풀림 …), `+0x48` EP_LOOKUP_DATA, `+0xe8` 기다리는 사람 제공자, `+0xb8` 뮤텍스, `+0x170` 연결 객체.
- `ResolveEndpoint` (rva 0x59258, `rpcrt4/ResolveEndpoint.asm`): `+0x48` 상태를 읽음 → 상태 1 (처음) 이면 **`+0xc9` `[this+0x114] = 3`** 을 쓰고 `SubmitResolveEndpointRequest` (`+0x12b`, ret `+0x130`). 상태가 1 이 아니면 (이미 푸는 중) 기다리는 사람으로만 등록하고 (`+0x90 Register`) **동기면 `SyncWait` (`+0x387`), 비동기면 `+0x30e MOV EBX,0xc0021018` 보류 반환**.
- `CompleteOnePart` (바인딩, rva 0x5aeec): **`+0xc7` `[this+0x114] = EBP`** — 성공이면 4 (`+0x99 MOV EBP,4`), 실패면 1 — 쓰고 `+0xed` 에서 `SignalAllIos` 로 **기다리던 모두를 깨운다**. 이 줄에 닿아야만 상태 3 이 풀린다.
- 기다리는 사람이 동기 호출이면 `Register` 가 자동 리셋 이벤트를 새로 만든다 (`Register +0x1cf CreateEventW`). 비동기 호출이면 따로 이벤트가 없고 **호출자 (winnsi) 의 비동기 상태 이벤트** 를 `IssueNotification (0x345c4) +0xc5 SetEvent` 가 세운다 (같은 함수의 `+0xdb QueueAPC`, `+0xea QueueIOCompletion` 은 통지 종류가 다를 때).
- **→ 코드가 보여 주는 것**: 첫 호출의 끝점 풀이가 끝났든 (성공 4 / 실패 1) 중간에 죽었든, `CompleteOnePart +0xc7` 에 닿으면 둘째 호출은 정상적으로 다시 시작하거나 즉시 오류를 받는다. **닿지 못한 채 첫 호출이 나가 버리면** (예외) 상태 3 이 남아 둘째 호출이 `+0x30e` 로 보류 → 무한 대기. 로그가 이 경우와 맞는다 `[추측: 첫 호출이 이렇게 나갔다는 것]`.

### 2-3. 끝점 풀이 사슬 (서버 없음/있음 공통) — 어디서 `NtAlpcConnectPort` 가 나오나

| 단계 | 함수 | 호출 줄 |
|---|---|---|
| 1 | winnsi `Ndr64AsyncClientCall` → (NDR 해석기 본문·`NegotiateTransferSyntax` 등은 이름만) → `LRPC_BASE_BINDING_HANDLE::DriveStateForward` (0x44e00) | `DriveStateForward +0x8ca` (ret `+0x8cf`) → `ResolveEndpoint` |
| 2 | `LRPC_BASE_BINDING_HANDLE::ResolveEndpoint` (0x59258) | `+0x12b` (ret `+0x130`) → `SubmitResolveEndpointRequest` (0x595f4) |
| 3 | `SubmitResolveEndpointRequest` | `+0x136` (ret `+0x13b`) `ResolveEndpointWithoutEpMapper` — **인터페이스의 기본 끝점 목록을 훑는데 winnsi 는 0개라 아무것도 못 얻는다**; 비동기면 `+0x21a` `EpAllocateLookupHandle(LrpcEpResolveCompleteRoutine, …)`; `+0x195` (ret `+0x19a`) → `EP_LOOKUP_DATA::ResolveEndpoint` (0x59d38) |
| 4 | `EP_LOOKUP_DATA::ResolveEndpoint` | `+0xba` `TowerConstruct` (탑 만들기, 본문은 `OsfTowerConstruct`) → `+0x166` (ret `+0x16b`) **`BindToEpMapper`** (동기 길은 실패하면 **한 번 더** — `uVar8 > 1` 에서 포기, 결과 `0x6d8`) |
| 5 | `BindToEpMapper` (0x5a5e4) | 프로토콜 표에서 `ncalrpc` → 끝점 `epmapper`, 형 5 를 찾아 (rva 0xe1420, `+0x97` `_wcsicmp`) → `+0x123` (ret `+0x128`) **`RpcBindingCreateW`** (템플릿 rva 0xe1188: 프로토콜 3 = LRPC, 끝점 `"epmapper"`; 보안 rva 0xe11c0: 수준 6, 서비스 10) → `+0x141` (ret `+0x146`) **`RpcBindingBind`** (비동기면 EP_LOOKUP 의 비동기 상태를 넘김). 실패면 `RpcBindingFree` + `0x6d8` (1752 EPT_S_CANT_PERFORM_OP) |
| 6 | `RpcBindingBind` (0x60650) → 가상 `Bind` (`+0x4b` `indirect:cfg:[RAX+0x128]`; 구현 이름은 `[추측]` — `DriveStateForward` 의 부른 쪽 이름에 `Bind` 가 있다) → `LRPC_BASE_BINDING_HANDLE::DriveStateForward` → `LRPC_CASSOCIATION::Connect` | `DriveStateForward +0x515` (ret `+0x51a`) → `Connect` |
| 7 | `Connect` → `AlpcConnect` | `Connect +0x198` (ret `+0x19d`) → `AlpcConnect +0x293` (ret `+0x29a`) `NtAlpcConnectPort` |
| 8 | 연결이 끝난 뒤 끝점 풀이 | `LookupNextChunk` (0x5a070): 동기 `NdrClientCall3` `+0x16b`/`+0x1ae` (프로시저 3 / 7), **비동기 `Ndr64AsyncClientCall` `+0x11d` (프로시저 8)**, 프록시 정보 rva 0xe11e8 (EPM 인터페이스 `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0 + 전송 문법 NDR 2.0, rva 0xe1220) |
| 9 | 응답 처리 | `LookupNotifyRoutine` (0x5c578) → `RpcAsyncCompleteCall` → `ProcessNextChunk` → `FatalLookupErrorEncountered` (0x9c9b0: epmapper 가 보낸 DCE 상태 `0x16c9a0cd` → `0x6d8`, `0x16c9a0d6` → `0x6d9` (EPT_S_NOT_REGISTERED), 표 rva 0xed210 의 38쌍 (예 `0x1c010003` → `0x6b5`), 그 밖은 그대로) → `LrpcEpResolveCompleteRoutine` → `ResolveEndpointComplete` (0x5bfb0) → 바인딩 `CompleteOnePart (0x5aeec)` |

- 서버 없음 / 연결 실패의 최종 RPC 오류 숫자: `AlpcConnect` 가 `0x6ba` 를 돌려주면 비동기 바인딩의 완료 상태로 `LookupNotifyRoutine` 에 들어가 (`RpcAsyncGetCallStatus`) 같은 숫자로 `ResolveEndpointComplete` → `CompleteOnePart` 에 전달되는 것으로 읽힌다 `[추측: `RpcBindingBind` 비동기 완료의 안쪽은 안 읽음]`. 동기 길은 `0x6d8`. **winnsi 호출의 최종 반환 (1722 인지 1752 인지) 은 `[공백]`** — 동적으로 `RpcAsyncCompleteCall` 반환을 보면 한 번에 닫힌다 (아래 동적 3).
- `LrpcRetryAlpcConnect`/`ContinueAlpcConnectAfterSidNormalize` (SID 정규화 뒤 비동기 재연결), `LRPC_BASE_RETRY_THUNK::IsRetryErrorCode` (오류 코드가 재시도 대상인지) 도 있다 — 이름만 (`CompleteOnePart` 연결 객체 쪽 `+0x…` 에서 불림).

### 2-4. seq 2205~2224 가 어디서 오나 — rpcrt4 가 아님 (이름 + 부르는 줄)

| 로그 | 코드 | 부르는 줄 |
|---|---|---|
| `NtQueryInformationProcess 0x25` | `ntdll!WerpIsProcessNative` | `+0x2d` (ret `+0x32`) `NtQueryInformationProcess(-1, 0x25, buf, 0x40, 0)` — `RtlReportException +0x54` (ret `+0x59`) 에서 부름 (`ntdll/WerpIsProcessNative.asm`, `RtlReportException.asm`) |
| `NtSetInformationProcess 0x3f` | `ntdll!WerpSetProcessFaultInformation` | `+0x2c` (ret `+0x31`); `RtlReportExceptionEx +0x10d` 와 `RtlReportExceptionHelper +0x505` 에서 부름. `kernelbase!UnhandledExceptionFilter +0x110` / `+0x282` 에도 같은 `NtSetInformationProcess(…, 0x3f)` 가 있다 |
| `NtCreateSection` | `ntdll!WerpCreateCrashDataSection` | `+0xa8` (`NtCreateSection`); `RtlReportExceptionEx +0x13a` (ret `+0x13f`) 에서 부름. (`RtlReportExceptionHelper +0x1d0` 에도 `NtCreateSection`) |
| `NtDuplicateObject ×2` | `ntdll!RtlReportExceptionEx` | `+0x172` (ret `+0x177`), `+0x1a9` (ret `+0x1ae`) |
| `NtOpenEvent \KernelObjects\SystemErrorPortReady` | `kernelbase!WaitForWerSvc` | `NtOpenEvent` (문자열은 `kernelbase/WaitForWerSvc.c`); `RaiseFailFastException +0x12e` (ret `+0x133`) 에서만 부름; `RaiseFailFastException` 은 `UnhandledExceptionFilter +0x39d` (ret `+0x3a2`) 에서 부름 |

- 순서 (`0x25` 조회 → `0x3f` 설정 → 섹션 → 복제 ×2) 가 **`RtlReportExceptionEx` 의 줄 순서 (`+0xb5 RtlReportException` → `+0x10d` → `+0x13a` → `+0x172` → `+0x1a9`)** 와 맞는다. `SystemErrorPortReady` 는 kernelbase 쪽이라 둘이 이어지는 정확한 사슬 (`RaiseFailFastException` ↔ `RtlReportExceptionEx`) 은 `[공백]`.
- **이 길이 열리는 조건**: rpcrt4 의 예외 필터 `I_RpcExceptionFilter` (0x3c900) 는 `RpcpIsFatalError` (0x3c920; 표 rva 0xeb888) 가 참이면 0 (= EXCEPTION_CONTINUE_SEARCH, 바깥으로 넘김) 을 돌려준다. 표 14개: `0xc0000005` (접근 위반), `0xc0000194`, `0xc00000aa`, `0x80000002`, `0xc0000096`, `0xc000001d`, `0x80000003`, `0xc00000fd` (스택 넘침), `0xc0000235`, `0xc0000006`, `0xc0000420`, `0xc0000409` (`__fastfail`), `0x80000001`, `0xc00002c9`. 이 밖의 RPC 오류 코드는 `EXCEPTION_EXECUTE_HANDLER` 로 winnsi 가 잡는다 (`Ex$filt$0`/`$filt$2 +0xe`). **그러니 로그의 첫 호출이 WER 길로 갔다는 것은 "RPC 오류"가 아니라 "치명 예외" 였다는 뜻이다.** 어디서 났는지는 정적으로 못 정함 — 후보 한 줄: `CLIENT_IO_PROVIDER::Register +0x267` 의 `INT 0x29` (목록 깨짐 `__fastfail(3)`, `rpcrt4/Register.asm`), winnsi `NsiRpcRegisterChangeNotificationEx +0x925` 의 `INT 0x29` (같은 목록 검사, 성공 길의 맨 끝). 둘 다 "목록 머리가 초기화 안 됨" 같은 조건에서만 난다 `[추측]`.

---

## 3. 서버가 돌려줘야 하는 최소 (정적으로 닫은 틀)

### 3-1. 한 번의 등록 성공에 필요한 것 (논리 순서)

| 단계 | 클라이언트가 보내는 것 | 서버가 돌려줘야 하는 것 | 근거 |
|---|---|---|---|
| ① ALPC 연결 (끝점 풀이용) | `NtAlpcConnectPort(\RPC Control\epmapper, …)` (2-1 표 인자) | **핸들을 `*PortHandle` 에 쓴 상태로 성공**. `InMessageAttributes` 는 빈 버퍼라 `ValidAttributes = 0` 으로 두면 됨. 비동기 호출이면 `NtAlpcSetInformation` (완료 통지 연결, `TpAllocAlpcCompletionEx` 안) 이 이어지고 서버 쪽 연결 수락은 **완료 통지로** 온다 | `AlpcConnect +0x293`, `Connect +0x3b1`, `CreateAlpc +0x66` |
| ② 핸들 질문 | `NtAlpcQueryInformation(핸들, 0, 0x10 바이트)` | 오류여도 됨 (결과 1 = "공유 섹션 있음 취급" 으로 읽음). 성공하면 `Flags & 0x1000000` 가 있으면 `[연결+0x240] = 0xff00` | `AlpcConnect +0x388`, `IsPortWithSharedSection +0x25 ~ +0x3f` |
| ③ epmapper LRPC bind | EPM 인터페이스 `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0, 전송 문법 둘 | bind 확인 응답 | `[공백]` — 다음 요청 |
| ④ `ept_map` 요청 | 비동기 프로시저 8 (또는 3·7): 탑 = nsi 인터페이스 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0 + 전송 문법 + `ncalrpc` | 탑 1개 이상 (끝점 이름 문자열이 든다) + 상태 0. 응답이 비면 `0x6d9` (EPT_S_NOT_REGISTERED). 탑 안 층 번호·검사 칸은 `ProcessNextChunk` 에서 `[공백]` | `LookupNextChunk +0x11d`, `LookupNotifyRoutine`, `FatalLookupErrorEncountered` |
| ⑤ 두 번째 ALPC 연결 | `\RPC Control\<끝점 이름>` (이름 그대로 `\RPC Control\` + 끝점) | 위 ①·② 와 같은 응답 | 같은 `AlpcConnect` |
| ⑥ nsi LRPC bind | 인터페이스 `7ea70bcf…`, 전송 문법 NDR 2.0 + NDR64 | bind 확인 (어느 전송 문법을 골랐는지 포함) | `[공백]` — 다음 요청 |
| ⑦ 프로시저 5 요청 | 3-2 | **출력 컨텍스트 핸들 (20바이트) + 반환 DWORD 0** | `NsiRpcRegisterChangeNotificationEx +0x1e8 ~ +0x266` |
| ⑧ 프로시저 7 요청 | 컨텍스트 핸들 | **답하지 않는다** (걸려 있게 둠) | `+0x54e`, `NsiWorkerThread` |
| ⑨ 성공 뒤 클라이언트가 하는 시스템 호출 | — | 가짜로 답할 것 아님 (클라이언트 코드가 직접 부름): 힙 할당·이벤트 ×4·`RegisterWaitForSingleObjectEx`·(첫 등록이면) WNF 질문·구독 | `+0x286 ~ +0x6b8`, `RegisterNsiSvcRestartNotification` |

- **`ept_map` 을 건너뛰는 방법은 없다.** 인터페이스에 기본 끝점이 없고 (rva 0x67b0), 바인딩 문자열에도 끝점이 없다 (`NsiConnectToServer +0x3a`).
- 서버 쪽 (nsisvc) 이 프로시저 5 의 입력을 어떻게 검사하는지는 안 열었다 `[공백]` — 클라이언트 쪽은 성공 반환 DWORD 가 0 이 아니면 정리 길 (`+0x664`) 로 가서 실패한다.

### 3-2. `NsiRpcRegisterChangeNotification` 의 입출력 꼴 (클라이언트 쪽)

- 입력 (프로시저 5): ① 24바이트 `{ 예약 qword 0, NPI_MODULEID 포인터 (IPv4 `eb004a00-9b1a-11d4-9123-0050047759bc` / IPv6 `eb004a01-…`, 24바이트 {길이 0x18, 형 1, GUID} — sast/126 4 절), 객체 번호 dword (인터페이스 7, 유니캐스트 0xa) }`. ② compartment 8바이트 값 — `nsi!NsiGetAllParameters(NDIS 객체 8)` 로 받은 {번호, 범위} 를 서로 바꾼 것 (`+0x126 ~ +0x132`). sast/126 A6 이 권한 NDIS 객체 8 의 대답 `{1, 0}` 을 주면 이 값은 `{범위 0, 번호 1}` 이 된다.
- 출력: **컨텍스트 핸들 (out)** — 클라이언트 쪽에서는 8바이트 포인터로 보임 (`슬롯`) + 반환 DWORD.
- 그 뒤 클라이언트가 쓰는 법: 컨텍스트 핸들을 블록 `+0x28` 에 보관 → 프로시저 7 의 입력으로 (`+0x54e` 앞 `+0x50f` `R8 = [블록+0x28]`) → 해제 때 프로시저 6 의 입출력 (`NsiRpcDeregisterChangeNotificationEx`). 값 자체를 검사하는 줄은 못 찾았다 (NULL 이 되면 `RPC_X_SS_IN_NULL_CONTEXT` 계열 오류가 날 것으로 짐작 `[추측]` — 안전하게는 **0 이 아닌 컨텍스트 핸들 (속성 0 + 새 GUID)** 을 준다).
- 프로시저 7 의 출력 (알림 올 때 채워짐): 16바이트 구조체, 24바이트 모듈 ID 모양, DWORD. 에뮬레이터는 알림을 보낼 일이 없으니 안 채워도 된다 (`NsiWorkerThread` 가 불리지 않는다).

---

## 4. 지금 우리 (PE_emulator) 맞나·틀리나

(참고: `PE_emulator/ext/syscall/syscall_layer_cpp/src/alpc/nt_alpc_create_port.cpp` — 읽기만. 요청서가 가리킨 줄)

- **`:192` (`NtAlpcConnectPort`, `if (a[8] || a[9]) return stopped("alpc_message_attributes_unsupported")`) — 틀림 (막힘의 직접 원인).** rpcrt4 는 이 호출에서 a[9] 에 **항상** 8바이트 빈 버퍼 (`AllocatedAttributes 0, ValidAttributes 0`) 의 주소를 넘긴다 (`AlpcConnect +0x25a/+0x25e`). 그래서 `known_alpc_ports_` 에 든 `\RPC Control\epmapper` 는 이 줄에서 항상 `stopped` (= 상태 미정 → 런너가 성공으로 처리) 가 되고 **핸들을 `*PortHandle` 에 쓰기 전에** 나간다 (`:194 ~ :199` 의 `connected_client = true` / `handles_.allocate` / `write64(out, handle)` 에 닿지 않음) → 로그의 "성공인데 포트 핸들을 안 씀". 맞는 모습: a[8] 이 NULL 이고 a[9] 가 `AllocatedAttributes == 0` 인 빈 버퍼면 "속성 없음" 으로 보고 (`ValidAttributes` 에 0 쓰기) 핸들을 쓴다. 속성이 진짜 있을 때 (`Allocated != 0`) 만 멈춘다.
- **`:346` (`NtAlpcConnectPortEx`, 같은 검사) — 같은 이유로 틀림.** Ex 길 (`+0x27a TEST RDX / JNZ +0x3a9`) 은 스택 인자 `[RSP+0x30 .. +0x50]` 를 plain 길과 **같은 줄들** (`+0x255 ~ +0x275`) 에서 이미 채운 뒤에 갈라지므로, 연결 메시지·버퍼 길이·나가는 속성은 0, **들어오는 속성은 같은 8바이트 빈 버퍼** 다 (`+0x25e MOV [RSP+0x48],RAX`). Ex 에서만 다른 것: 둘째 인자 (= ConnectionPortObjectAttributes, `[RBP-0x10]`, 길이 0x30 + 안에 포트 이름) 와 a[5] (서버 보안 요구 SD, `+0x3a9 MOV [RSP+0x28],RDX`).
- **`:305` (`if (a[5]) return stopped("alpc_server_security_requirements_unsupported")`) — Ex 길 한정.** rpcrt4 는 Ex 의 a[5] 에 **서버 보안 요구 SD** (`[RBP-0x80]` = 호출자가 준 SD 또는 `gDefaultSecurityDescriptor`) 를 준다 (`+0x3a9 MOV [RSP+0x28],RDX`). Ex 길로 가는 조건은 2-1 마지막. 로그가 plain `NtAlpcConnectPort` 라서 이번 막힘의 원인은 아니다. plain 길의 a[5] 는 **서버 SID** (`[this+0xc0]`, 처음 NULL; 서버 SID 불일치 `0xc00002a0` 뒤에만 채워짐).
- **`:623` (`NtAlpcSendWaitReceivePort`, `alpc_service_protocol_unverified`) — 그대로 맞다.** 메시지 전달 모델이 없다는 선언이고 코드가 그것을 가정하지 않는다. 다만 위 ①을 고치면 이 줄이 처음으로 쓰인다 (epmapper 에 `NtAlpcSendWaitReceivePort` 가 온다) — 거기서 "서버 없음" 답이 필요하다 (아래 T1).
- **T1 — 속성 있는 (실은 빈) ALPC 연결을 어떤 status 로 답할지**:
  - **핸들 없는 성공은 틀린 답이다.** 연결 객체 상태 3 (`[연결+0x4c]`)·바인딩 상태 3 (`[바인딩+0x114]`) 이 남아 둘째 호출이 영원히 기다릴 수 있다 (2-2).
  - 서버 모델이 **없는** 포트: `0xc0000034` → `0x6ba` (1722). 연결 객체는 상태 2 (오류 보관), 바인딩 핸들은 상태 1 로 돌아가고 (`CompleteOnePart +0xc7`) 둘 다 `SignalAllIos` 로 기다리는 사람을 깨운다 → 깨끗한 실패 (2-2). 이 경로는 이미 `keysvc`·`eventlog` 18샘플이 "멈추지 않고 이어 가는" 것으로 확인됐다.
  - 주의: **`0xc0000041` 은 `0xe` (메모리 부족) 로 읽힌다.** "연결 거부" 용으로 쓰지 말 것.
  - 서버 모델이 **있는** 포트: 핸들을 쓰고 성공 (비동기 호출 `Flags 0` 이면 이어서 `NtAlpcSetInformation` 이 올 것).
- **T2 — nsisvc 알림 등록 성공 흉내**: 3-1 의 ①~⑧ 이 모두 필요하다. 그중 ③·⑥ (LRPC bind) 과 응답 머리는 아직 정적으로 안 닫았다. 가장 값싼 대안은 **epmapper 연결을 `0xc0000034` 로 실패시키는 것** — 그러면 IPv4·IPv6 둘 다 RPC 오류로 끝나 (`0x6ba` 로 짐작) `NotifyIpInterfaceChange` 가 오류를 돌려주고 `webio Startup +0xbe → +0x1d8` 로 `WebInitialize` 가 실패해 `WinHttpSendRequest` = 0 (**유한하게 끝남 — 무한 대기·치명 예외 없음**). 등록을 정말 성공시키려면 ①~⑧.
- **공통 몫 (LSA `GetUserName*`·RPCSS `CoInitializeSecurity` 도 같다)**: 2-1 의 인자 모양 (a[9] 빈 버퍼, a[2] 길이 0x30·나머지 0, a[3] 0x48 바이트), 상태 → RPC 오류 표, 비동기 `Flags 0` vs 동기 `0x20000`, `IsPortWithSharedSection` 의 `NtAlpcQueryInformation(0, 0x10)`, 끝점 풀이 = `\RPC Control\epmapper` + `ept_map` (인터페이스에 기본 끝점이 없을 때), 바인딩·연결 객체 상태 칸 (2-2).
- **winnsi 몫**: 인터페이스 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0, `ncalrpc` + 끝점 없음 + 인증 6/10, 프로시저 5·6·7 모양 (3-2), 등록 최대 4번 (v4/v6 × 인터페이스/유니캐스트) 과 "둘 중 하나만 되면 성공" 규칙, 이벤트·WNF 구독·스레드풀 대기 (성공 뒤 클라이언트 쪽 시스템 호출).

---

## 동적으로 확인할 것 (정적으로 못 닫은 것 — 멈춤점은 `caller+0x…` = `ret_off`)

유저 모드 (test.exe 프로세스), 모두 함수 입구가 아니라 **돌아온 자리** 에 건다.

1. **ALPC 메시지 한 번 뜨기 (질문 3 의 선 위 형식)**: 진짜 Windows (nsisvc 가 도는 VM) 에서 `ntdll!NtAlpcSendWaitReceivePort` 입구에 조건 (프로세스 = test.exe) 을 걸고 `kv` 로 `rpcrt4!…+0x…` 를 얻은 뒤, 송신 `PORT_MESSAGE` (`rdx` = 보낼 메시지, `dq @rdx L0x10`, 첨부 `r9`) 와 수신 (`r10`/`r8`) 을 한 번씩 덤프. 연결 직후 첫 송신이 bind 이고, 그 다음이 `ept_map`, 그 다음 nsi 의 bind·프로시저 5 요청이다 (순서 예상 `[추측]`). 이 `caller+0x…` 를 다음 정적 요청의 시작점으로 쓴다.
2. **`NtAlpcConnectPort` 인자·결과**: `rpcrt4!LRPC_CASSOCIATION::AlpcConnect +0x29a` (ret) — `eax`, `[this+0x88]` (`rsi+0x88`), 입력 인자 (`+0x293` 직전의 `rcx/rdx/r8/r9` 와 `[rsp+0x20..+0x50]`: Flags·서버 SID·InMessageAttributes 값), `+0x3c0` (Ex 길이면). 로그에서 plain 이 나온 이유 (`gDefaultSecurityDescriptor`) 확인용으로 `+0x27a` 의 `rdx`.
3. **끝점 풀이 결과 숫자**: `winnsi!NsiRpcRegisterChangeNotificationEx +0x259` (ret `RpcAsyncCompleteCall`) 의 `eax` 와 `[rsp+0x60]` — 서버 없음일 때 1722 인지 1752 인지. `+0x1ef` (ret `Ndr64AsyncClientCall`) 에서 예외로 나왔는지 (`+0x1fa` 착지 도달 여부). 실패하는 NotifyIpInterfaceChange 의 반환: `iphlpapi!InternalRegisterChangeNotification +0xa4`/`+0xdc` 의 `eax`, `NotifyIpInterfaceChange +0xa0`.
4. **상태 칸**: 첫 호출 중 `rpcrt4!LRPC_BASE_BINDING_HANDLE::ResolveEndpoint +0xc9` (쓰기 직후 `[rdi+0x114]` = 3) 과 `CompleteOnePart(0x5aeec) +0xc7` 가 **불리는지** (불리면 4 또는 1). 둘째 호출에서 `ResolveEndpoint +0x48` 의 `r12d` (= 3 이어야 설명이 맞음).
5. **예외 원인**: `ntdll!RtlDispatchException` (또는 `KiUserExceptionDispatch`) 에서 `.exr` 로 코드·주소, `kv` 로 호출 사슬. 후보: `CLIENT_IO_PROVIDER::Register +0x267` `INT 0x29`, `EnableAsync` 의 핸들 0 사용.
6. **성공 쪽 (nsisvc 있는 VM)**: `winnsi!NsiRpcRegisterChangeNotificationEx +0x259` 직후 `[rsp+0x60]` = 0, `+0x555` (프로시저 7 ret), `+0x6bd` (`RegisterNsiSvcRestartNotification` ret). 슬롯에 쓰이는 컨텍스트 핸들 8바이트 (`+0x46e` 의 `rcx`).

## 다음 요청 후보

1. **LRPC 선 위 형식** (3 의 남은 부분): `LRPC_CASSOCIATION::Bind(_RPC_CLIENT_INTERFACE*, int, RPCP_ALPC_MESSAGE_ATTRIBUTES*, LRPC_BASE_CCALL*, …)` (PDB 이름 확인), `LRPC_CASSOCIATION::CompleteBind(_LRPC_BIND_MESSAGE*, tagLRPC_BIND_DATA*, …)`, `LRPC_BIND_CCALL::HandleReply(long, _LRPC_PACKET_HEADER*, RPCP_ALPC_MESSAGE_ATTRIBUTES*)`, `LRPC_CCALL::HandleReply`, `LRPC_CCALL::SendReceive`, `RpcpAlpcpGetHeaderSize` (rva 0x57650), `PreinitRequestAlpcHeaders` (0xbe540·0xbe560), `AlpcSendWaitReceivePort` (0x3fa50·0x6def8), `AlpcSend` (0x42a18·0x56d44), `RpcpStrictAlpcToRpcStatus` (0x99d70). 거기서 머리 칸·길이·메시지 종류·`NtAlpcSendWaitReceivePort` 의 `0xc0000008` 처리 (로그의 핸들 0 보내기) 를 읽는다. 위 동적 1 의 `caller+0x…` 가 있으면 시작점이 된다.
2. **`nsisvc.dll`** analyze (서버 쪽, 필요한 함수만): 끝점 이름 (`RpcServerUseProtseqEp*` 또는 동적 끝점 + `RpcEpRegister`), 프로시저 5·6·7 구현이 입력을 어떻게 검사하는지 (모듈 ID·객체 번호·compartment), 컨텍스트 핸들을 만드는 줄.
3. **epmapper `ept_map` 응답 검사** (클라이언트 쪽): `ProcessNextChunk`, `ReturnLookupValues`, `TowerConstruct` → `OsfTowerConstruct`, 탑 층 (floor) 번호·프로토콜 ID (ncalrpc 의 값) 와 응답에서 클라이언트가 검사하는 칸.
4. `RPC_THREAD_POOL::CreateAlpc` → `ntdll!TpAllocAlpcCompletionEx` → `NtAlpcSetInformation` 이 비동기 연결 뒤 어떤 모양으로 부르는지 + 완료 통지가 어떻게 오는지 (워커 팩토리, sast/122 와 이어짐).
5. `RegisterNsiSvcRestartNotification` 의 WNF 상태 이름 (성공 뒤 첫 등록의 시스템 호출 둘 — `NtQueryWnfStateData`, `NtSubscribeWnfStateChange` 류).

## 뽑은 것

- **winnsi** (`ms/decomp/winnsi/`, `ms/disassemble/winnsi/`, depth 0, 30개): `NsiConnectToServer NsiDisconnectFromServer NsiRpcRegisterChangeNotification NsiRpcRegisterChangeNotificationEx NsiRpcDeregisterChangeNotification NsiRpcDeregisterChangeNotificationEx NsiReregisterChangeNotification NsiReregisterNotificationWorker NsiWorkerThread NsiStartWorkerThread NsiStopWorkerThread NsiCompletePendingRpcCall NsiServiceStatusChangeCallback PrepareRpcAsyncState InitializeNsiRpcClient CleanupNsiRpcClient RegisterNsiSvcRestartNotification DeregisterNsiSvcRestartNotification NsiRpcGetParameter(Ex) NsiRpcGetAllParameters(Ex) NsiRpcSetParameter(Ex) NsiRpcSetAllParameters(Ex) NsiRpcEnumerateObjectsAllParameters MIDL_user_allocate MIDL_user_free DllMain`.
- **rpcrt4** (depth 0, 정규식 `^이름$`): `Connect ContinueAlpcConnectAfterSidNormalize IsPortWithSharedSection LrpcRetryAlpcConnect LrpcRetryConnectFromThunk LrpcMapRpcStatus DriveStateForward ResolveEndpoint(×2: 0x59258·0x59d38) SubmitResolveEndpointRequest ResolveEndpointWithoutEpMapper ResolveEndpointWithEpMapper(×2) ResolveEndpointComplete ResolveBinding(×3) ResolveBindingWorker RpcEpResolveBinding EpResolve OsfEpResolve LrpcEpResolveCompleteRoutine LrpcRetryEpResolve LrpcRetryEpResolveFromThunk BindToEpMapper RpcBindingBind EnableAsync CreateAlpc CompleteOnePart(×3) SyncWait Register I_RpcExceptionFilter RpcpIsFatalError InquireDynamicEndpoint(×2) AllocateCCall(×2) AllocateCCallAsync LookupNextChunk ProcessNextChunk ReturnLookupValues LookupNotifyRoutine FatalLookupErrorEncountered LookupFailedCleanup TowerConstruct EpAllocateLookupHandle EpFreeLookupHandle AlpcVerifyServerSid PickAssociation EnableAsyncIfNecessary LrpcCAssociationConnectIoHandler LrpcBaseProviderComplete LrpcBHDriveStateForwardRoutine DriveStateForwardAsyncCallback IssueNotification(×6)` (+ 이미 있던 `AlpcConnect InitializeEPMapperClient TransformLrpcEndpointToAlpcPortName`).
- **ntdll**: `RtlReportExceptionHelper RtlReportExceptionEx RtlUnhandledExceptionFilter(2) WerpIsProcessNative WerpBreakIntoDebuggerIfPresent WerpWaitForCrashReporting WerpSetProcessFaultInformation WerpCreateCrashDataSection`. **webio**: `Startup`. (kernelbase `UnhandledExceptionFilter`·`RaiseFailFastException`·`WaitForWerSvc` 와 IPHLPAPI 의 `InternalRegisterChangeNotification`·`NsiNotificationConnectToServer`·`NotifyIpInterfaceChange`·`NetioRegisterAddressChangeNotification`·`CancelMibChangeNotify2` 는 이미 있던 `.asm` 읽기.)
- PE 데이터 읽기 (`pefile`, 파일로 안 남김): winnsi 인터페이스 구조체 rva 0x67b0, 프록시 정보 0x6000, 전송 문법 표 0x6150 (두 줄), NDR64 형식 표 0x6940, 프로시저 형식 0x66e0·0x6590·0x6360; rpcrt4 바인딩 템플릿 0xe1188·0xe11c0, EPM 인터페이스 0xe1220, 프로토콜 표 0xe1420, 치명 오류 표 0xeb888, DCE 상태 표 0xed210.

## 판정

**부분성공.** 질문 1·2 는 코드 줄 근거로 닫았다 (바인딩 = 끝점 없음 → epmapper, 비동기 RPC 와 프로시저 5·6·7, 이벤트의 주인, 실패 전파, `AlpcConnect` 상태 표, 핸들 0 일 때와 둘째 호출 대기의 설명, 2205~2224 의 출처). 질문 3 은 "무엇을 답해야 하나" 의 틀 (epmapper 묻기, 프로시저 5 의 출력 = 컨텍스트 핸들 + 0, 프로시저 7 은 답하지 않기) 까지 닫았고, **LRPC 선 위 형식 (bind·요청·응답 머리 칸) 은 다음 요청으로 넘겼다** (요청서가 허락한 분량 조절). `[공백]` 으로 남긴 것: 첫 호출이 낸 치명 예외의 자리, 서버 없음의 최종 오류 숫자 (1722 vs 1752), plain vs Ex 연결 길의 조건 (`gDefaultSecurityDescriptor`), 프로시저 8.

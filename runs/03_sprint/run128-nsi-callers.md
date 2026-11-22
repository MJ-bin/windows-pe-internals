# 2026-11-22 (작성일, 정적 분석 2026-11-21) run128 — NSI 부르는 쪽 (Q221, 정적만): `WinHttpSendRequest` 에서 NSI 장치 (`0x12000f`) 까지는 winhttp 가 직접이 아니라 **IPHLPAPI `GetAdaptersAddresses(0, 0x80f)`** 를 거치고, 이 길은 NSI 오류를 **삼킨다** (연결 이름 NULL 로 이어 감). `WinHttpSendRequest` 를 0 으로 만들 수 있는 줄은 따로 — `+0x25a` 웹 I/O 시작 (`webio!WebInitialize`) 안의 `NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange` 인데, 이 둘은 NSI 장치가 아니라 **winnsi RPC** 다. 구조체 `0x68` · `0x50` · `0x70` 칸 표 셋, 모듈 ID 셋, 빈 표 대답 (`0x12001b` 개수 0 → `GetAdaptersAddresses` = `0xe8`)

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("WinHTTP · iphlpapi 가 NSI (`\\.\Nsi`) 에 무엇을 묻고 대답을 어떻게 읽나 — 루프백만 있는 PC 의 최소 대답", 튜닝 `290_tune09.md N1`) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 winhttp · webio · IPHLPAPI · dnsapi · ws2_32 · nsi 를 안 올린다 — 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run124 와 같은 줄), `sample/*.c` 에 소켓 · HTTP · 어댑터 API 가 없다 `[소스]`. 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (PDB 다섯 · 근거 줄) · 3장 끝 새 절 하나 · 3장 run124 절 끝 줄 · 4장 (run124 줄 일부 닫힘 표시, run125 ~ run127 대기열 번호, run128 줄) · 5장만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q221** (사용자 결정 2026-11-21 "이것들부터 먼저", 2026-11-22 "문서로 만들어서 커밋"). 이번 run 으로 닫는 몫:
> - 요청서 질문 1 (누가 무엇을 묻나 — 사슬 · 인자 · 칸 표 셋) · 질문 2 (부른 쪽이 대답을 어떻게 읽나 · 최소 대답) — 결과서 **분석성공**.
> - **안 닫힌 것 `[공백]`**: 요청 쪽 로그의 **"0x12000f 가 보인 그 `WinHttpSendRequest` 가 왜 0 을 돌려주나"** — 정적으로 본 NSI 장치 길은 오류를 다 삼키므로 원인은 NSI IOCTL 이 아니다. 후보 (`+0x25a` 웹 I/O 시작 → winnsi RPC) 는 "0x12000f 가 안 보이고 일찍 0 이 되는 샘플" 쪽이다. 확인은 동적 **Q222** (아래 동적 1 · 4), winnsi 쪽 정적은 sast/127 (Q223) → **run129 예정**.
> - run124 "아직 확인 필요" 정적 5 (nsi `0x70` 칸 뜻) — **칸 뜻 닫힘**, `nsi.sys` 개수 0 대답은 남음. 아래 "정정" 절.

## 요약

- **부르는 모듈** `[정적]`: winhttp · webio · IPHLPAPI · dnsapi · ws2_32 모두 nsi 를 **지연 가져오기** 로만 쓴다 (일반 가져오기 표엔 없음). winhttp 는 nsi 에서 `NsiAllocateAndGetTable` · `NsiFreeTable` 둘만, webio 는 nsi 를 안 가져온다. **실제로 NSI 를 묻는 쪽은 IPHLPAPI** (그리고 dnsapi 일부).
- **`WinHttpSendRequest` 의 NSI 장치 길 (F2, 프록시 이름)** `[정적]`: `WinHttpSendRequest +0x2fa` → `HTTP_USER_REQUEST::SendRequest +0x294` → `_CreateProxyResolver` → `SafeGetProxyResolver +0xbe` → `CheckProxySettings +0x37c` → `GetActiveConnectionName +0x77` → `GetActiveConnectionNameRAS +0x123` → `iphlpapi!GetAdaptersAddresses(0, 0x80f)` → `AllocateAndGetAdaptersAddresses +0xc1` → `nsi!NsiGetAllParameters` → `NtDeviceIoControlFile(0x12000f)`.
  - 인자: `NsiGetAllParameters(1, NDIS 모듈, 객체 8, 키 없음, RW 8 B)` = 스레드의 `{compartment 번호, 범위}`. 이어서 `NsiAllocateAndGetTable` 셋 (NDIS 객체 1 = 어댑터 표, IPv4 · IPv6 객체 7 = IP 인터페이스 표).
  - **오류를 삼킨다**: NSI 가 무엇을 돌려줘도 `GetActiveConnectionName` 이 NULL (이름 없음) 을 돌려줄 뿐, `WinHttpSendRequest` 는 이어 간다. → **이 길엔 "꼭 받아야 하는 최소 대답" 이 없다.**
  - `CheckProxySettings +0x37c` 는 **네트워크 바뀜 세대값이 달라졌을 때만** 탄다 (둘 다 시작값 0) — 첫 보냄에서 이 줄을 타는지는 `[공백]`.
- **`WinHttpSendRequest` 를 0 으로 만들 수 있는 줄 (F1, 웹 I/O 시작)** `[정적]`: `+0x25a PrepareWebIOSessionForRequest` → `CreateWebIOSession` → `GetWebIOApiHandle` → `webio!WebInitialize` → … → `WapInitializeNetworkChange` → … → webio `Startup` → `iphlpapi!NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange`. 이 둘이 0 이 아니면 `WebInitialize` 실패 → `WinHttpSendRequest` 0.
  - 이 둘은 **NSI 장치 IOCTL 이 아니라** `winnsi!NsiConnectToServer` (RPC) 길이다. `NsiConnectToServer` 가 실제로 서버를 만나는지는 winnsi 를 안 읽어 `[공백]` → run129 예정.
  - 이 줄은 F2 (`+0x2fa`) **앞**이라, 로그에 `0x12000f` 가 찍힌 호출은 이 줄을 이미 지난 것이다 (또는 핸들이 이미 있어 건너뜀).
- **구조체 칸 표 셋** `[정적]` (칸 이름은 `[추측]`): 세 코드 모두 nsi.dll 이 **같은 칸 순서** 로 채운다 — `+0x10` 모듈 ID 포인터 · `+0x18` 객체 번호 · `+0x20` 첫 인자 (늘 1) · `+0x28` 키 포인터 · 그다음 (포인터, 크기) 쌍. `0x12000f` = `0x68`, `0x120007` = `0x50`, `0x12001b` = `0x70` (`+0x68` 개수).
- **모듈 ID** `[정적]`: IPHLPAPI `.rdata` 상수, PDB 에 이름 있음 — `NPI_MS_NDIS_MODULEID` `eb004a11-…`, `NPI_MS_IPV6_MODULEID` `eb004a01-…`, `NPI_MS_IPV4_MODULEID` `eb004a00-…`.
- **빈 표** `[정적]`: `0x12001b` 가 개수 0 (status 0) 을 두 번 주면 `GetAdaptersAddresses` = `0xe8` (ERROR_NO_DATA). `GetAdaptersInfo` 는 루프백 (IfType 24) 을 건너뛰어 루프백만 있어도 `0xe8`.
- **열기 실패 흉내** `[정적]`: `CreateFileW` 가 −1 인데 `GetLastError()` 가 0 이면 nsi.dll 은 오류로 안 보고 핸들 −1 로 `NtDeviceIoControlFile` 을 낸다 → `0xc0000024`. 요청 쪽 에뮬레이터 로그가 본 그 모습과 맞는 줄이다.
- **`GetAddrInfoW`** `[정적]`: ws2_32 는 이 길에서 NSI 를 안 부른다. NSI 는 dnsapi `Query_PrivateExW +0x60b` → `iphlpapi!GetCurrentThreadCompartmentId` → `NsiGetParameter` (`0x120007`) 하나, 실패하면 번호 1 로 삼킨다.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든여섯째 바퀴. plan **Q221**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze** 다섯 모듈 — 서버 메모리 (46G 중 가용 27 ~ 31G) 를 보고 한 번에 하나씩 (winhttp → webio → IPHLPAPI → ws2_32 → dnsapi). PDB GUID 는 바이너리 CodeView 기록과 `llvm-pdbutil dump --summary` 둘 다로 확인 (결과서).

| 모듈 | 바이너리 (`System32\`) | PDB guid+age | 함수 수 |
|---|---|---|---|
| winhttp | `winhttp.dll` 1,232,864 B | `0B998B55BD5357B779887C5DC5BADAAF1` | 3305 |
| webio | `webio.dll` 881,648 B | `AB34B4686EFD631EC4E81A6E6B2618E91` | 2103 |
| IPHLPAPI | `IPHLPAPI.DLL` 220,528 B | `23648DDA4C96DCCCE139DC2340F3030B1` | 642 |
| ws2_32 | `ws2_32.dll` 534,592 B | `387233976BD422D139F79CC7418808251` | 1194 |
| dnsapi | `dnsapi.dll` 1,422,944 B | `50DD7074162A7A959E3E4D82C49E3CBE1` | 3405 |
| nsi (run124 때 analyze) | `nsi.dll` | `662A028F57A7C702B11F583CE15A8E631` | 42 |

- `ws2_32.dll.pdb` 는 이미 `ms/sym/` 에 있었다 (이번에 받은 것 아님, GUID 같음).
- 공개 PDB 라 **구조체 타입 · IOCTL 상수 이름 없음.** 모듈 ID 상수 이름 (`NPI_MS_*_MODULEID`) 만 IPHLPAPI · winhttp PDB 에 있다. 객체 번호 (1 · 7 · 8 · 0 · 9 · 10 · `0x10`) 는 이름이 없고 숫자뿐이다.
- webio 내보내기는 **번호뿐** 이라 Ghidra 이름이 `Ordinal_N` 이다. 결과서가 PDB 공개 심볼과 내보내기 표를 맞춰 얻은 이름: `Ordinal_5 = WebCloseSession` · `Ordinal_9 = WebCreateSession` · `Ordinal_17 = WebInitialize` · `Ordinal_25 = WebSendHttpRequest` · `Ordinal_45 = WebSetHeapExtensionInterfaces`. 이 문서는 PDB 이름을 쓴다.
- 버전 대조 (결과서): "알려진 사실" 표에 이 모듈들이 없어서 `.asm` `+0x` 를 원본 바이트와 직접 맞춤 — `AllocateAndGetAdaptersAddresses +0xc1` (`48 ff 15`, ret `+0xc8`) · `WinHttpSendRequest +0x25a` · `+0x2fa` (`e8`) · `NsiGetAllParameters +0xf8` 즉치 `0x12000f` 등 **일치**.
- 같은 시간에 다른 decompiler 가 ntoskrnl (sast/125, run127) 을 돌았다 — 프로젝트가 달라 부딪히지 않음.

## 목적

- PE_emulator 에서 `WinHttpSendRequest` 가 `NtCreateFile("\??\Nsi")` 기본 대답 (핸들 −1) → `NtDeviceIoControlFile(−1, 0x12000f, 입력 = 출력 0x68)` = `0xc0000024` 를 본 뒤 0 을 돌려준다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님). 방향: **루프백 하나만 있는 PC**, MAC 없음, 진짜 망에 안 나감. 요청서 `_workspace/sast/126_nsi-callers.md`:
  1. 누가 무엇을 묻나 — `WinHttpSendRequest` · `WinHttpGetIEProxyConfigForCurrentUser` · `GetAdaptersAddresses` · `GetAddrInfoW` · `GetAdaptersInfo` 에서 nsi 까지의 사슬 · 인자 · `0x68` / `0x50` / `0x70` 칸 표.
  2. 부른 쪽이 대답을 어떻게 읽나 — 실패 · 빈 표에서 갈림, `WinHttpSendRequest` 최소 대답.
  - 원 요청 3 (VM 실제 바이트) 은 동적 — **Q222** 로 미룸.

## 방법

- 정적 sast/126:
  - **가볍게 먼저**: `pefile` 로 가져오기 표 (일반 + 지연) 를 읽어 nsi 를 부르는 모듈을 가림. `System32` 4,346 파일 전수 조사에서 `NSI.dll` 을 가져오는 모듈 중 질문과 닿는 것 = ws2_32 · winhttp · dnsapi · IPHLPAPI (webio 는 없음). 나머지 50여 개 (ipconfig · netprofm · ncsi 등) 는 질문 밖이라 안 봄.
  - 필요한 다섯만 analyze → `export`: winhttp 27개 (+ 같은 이름 여럿) · webio 9개 · IPHLPAPI 26개 · ws2_32 5개 (배제용) · dnsapi 12개, 지도 `_map/` (winhttp `WinHttpSendRequest` · `WinHttpOpen` · `WinHttpConnect` · `WinHttpOpenRequest` · `WinHttpReceiveResponse` · `WinHttpGetIEProxyConfigForCurrentUser`, webio `Ordinal_9` · `Ordinal_17` · `Ordinal_25`), `_refs/` (가져오기 칸 · `g_ullNetworkGeneration`).
  - 같은 이름 함수가 여럿인 winhttp (`SendRequest` 다섯 · `Init` 넷 · `GetWebIOApiHandle` 둘 · `Startup` 둘) 는 `.asm` 이름에 `__<rva>` 가 붙음 (예: `SendRequest__14880.asm`).
  - nsi · nsiproxy 는 run124 때 analyze 한 것을 읽기만 (`NsiGetAllParameters` · `NsiGetParameter` · `NsiEnumerateObjectsAllParameters` · `NsiAllocateAndGetTable` 과 `…Ex`).
- 결과서: `_workspace/sast/126_nsi-callers_result.md` (**분석성공**, 스크립트 문제 없음). 두 번에 나눠 씀 — A 절 `WinHttpSendRequest` 길, B 절 나머지 API 넷, 4 절 모듈 ID.
- flow-writer 가 다시 본 것:
  - `_calls.tsv` 로 call · ret 짝 — winhttp (`WinHttpSendRequest` · `SendRequest` · `_CreateProxyResolver` · `SafeGetProxyResolver` · `CheckProxySettings` · `GetActiveConnectionName` · `GetActiveConnectionNameRAS` · `LoadAutomaticProxyResolvers` · `PrepareWebIOSessionForRequest` · `CreateWebIOSession` · `GetWebIOApiHandle` · `WinHttpGetIEProxyConfigForCurrentUser`), IPHLPAPI (`GetAdaptersAddresses` · `AllocateAndGetAdaptersAddresses` · `GetAdaptersInfo` · `AllocateAndGetAdaptersInfo` · `GetBestInterfaceEx` · `GetCurrentThreadCompartmentId` · `GetCurrentThreadCompartmentScope` · `NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange` · `NetioRegisterAddressChangeNotification` · `InternalRegisterChangeNotification` · `NsiNotificationConnectToServer`), nsi 넷, webio (`Startup` · `WapInitializeNetworkChange` · `WaInitializeUtil` · `WaMapInterfaceToAddressFamily` · `_map/Ordinal_17_edges.tsv`), dnsapi `Query_PrivateExW`.
  - `.asm` 줄: `AllocateAndGetAdaptersAddresses +0xcd ~ +0xcf` · `+0xf71 ~ +0xf7a` · `+0x16f` · `+0x1d9` · `+0xfe7` · `+0xa26`, `NsiGetAllParameters +0x113` · `+0x139` · `+0x1e2 ~ +0x1f6` · `+0x22d`, `CheckProxySettings +0x85 ~ +0xab`, `AllocateAndGetAdaptersInfo +0xf7` · `+0x15d ~ +0x15f`, webio `Startup +0xbc` · `WapInitializeNetworkChange +0xa5 ~ +0xac` · `+0x156`, `PrepareWebIOSessionForRequest +0x4b ~ +0x4f`, `WinHttpSendRequest +0x25f ~ +0x263`, `InternalRegisterChangeNotification +0x5e ~ +0x6e`. 결과서와 다른 곳 → 아래 "flow-writer 대조".
  - `pdb_aliases.py --lookup` 으로 winhttp 사슬 함수 11개 (rva `0x14880` · `0x15c34` · `0x15cb8` · `0x3b6ec` · `0x5238c` · `0x525a4` · `0x77b4` · `0x7890` · `0x79f4` · `0x80f4` · `0x5c8a4`) 와 webio `StartGlobalNetworkChangeMonitor` (`0x4aab4`) · `Startup` (`0x4abe8`) — **모두 별칭 무리 아님**. `_calls.tsv` 의 `alias:X+1` 표시는 같은 함수의 C++ 장식 이름 하나 (`.asm` 의 `ALIASES(1): ?CreateWebIOSession@INTERNET_SESSION_HANDLE_OBJECT@@QEAAKXZ` 꼴) 라 이름을 그대로 썼다.
- 앞 바퀴 짝: run124 (sast/121 — nsi 열기 · IOCTL 셋 · 길이 먼저 묻기 틀), run126 (afd 주소 목록 — "망 없는 PC 에 남는 주소는 NSI 몫").

## 결과

비유 (run124 의 우체국 이어서):

- NSI = 동네 **주민센터 안내 창구**. "이 동네에 어떤 길 (어댑터) 이 있나요?" 를 묻는 곳이다.
- winhttp 는 직접 안 묻고 **비서 (IPHLPAPI)** 를 시킨다. 비서가 창구에서 "모르겠어요" 를 들어도 winhttp 는 "연결 이름 없음" 으로 적고 그냥 간다 — 그래서 이 창구 대답으론 편지 보내기가 안 막힌다.
- 편지 보내기를 막을 수 있는 건 다른 일 — webio 가 처음 문을 열 때 **"길이 바뀌면 알려 주세요" 알림 신청** 을 하는데, 이 신청은 창구가 아니라 **전화 (RPC) 로 다른 사무실 (nsisvc)** 에 한다. 전화가 안 되면 문을 못 연다 `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/{winhttp,webio,iphlpapi,dnsapi,nsi}/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `winhttp!WinHttpSendRequest` (rva `0x8580`) | `+0x25a` | `+0x25f` | `INTERNET_SESSION_HANDLE_OBJECT::PrepareWebIOSessionForRequest` (F1) | [정적] |
| `winhttp!WinHttpSendRequest` | `+0x2fa` | `+0x2ff` | `HTTP_USER_REQUEST::SendRequest` (rva `0x14880`, F2) | [정적] |
| `winhttp!HTTP_USER_REQUEST::SendRequest` | `+0x294` | `+0x299` | `_CreateProxyResolver` (rva `0x15c34`) | [정적] |
| `winhttp!_CreateProxyResolver` | `+0x2a` · `+0x55` | `+0x2f` · `+0x5a` | `INTERNET_HANDLE_BASE::SafeGetProxyResolver` (인자 1 / 0) | [정적] |
| `winhttp!SafeGetProxyResolver` (rva `0x15cb8`) | `+0xbe` | `+0xc3` | `INTERNET_SESSION_HANDLE_OBJECT::CheckProxySettings` | [정적] |
| `winhttp!CheckProxySettings` (rva `0x3b6ec`) | `+0x37c` | `+0x381` | `GetActiveConnectionName` | [정적] |
| `winhttp!LoadAutomaticProxyResolvers` (rva `0x5c8a4`) | `+0x224` | `+0x229` | `GetActiveConnectionName` | [정적] |
| `winhttp!WinHttpGetIEProxyConfigForCurrentUser` (rva `0x50890`) | `+0xc3` | `+0xc8` | `GetActiveConnectionName` | [정적] |
| `winhttp!WinHttpGetIEProxyConfigForCurrentUser` | `+0x267` | `+0x26c` | `GetIEProxyConfigForCurrentUserFromService` (rva `0x82834`) | [정적] |
| `winhttp!GetIEProxyConfigForCurrentUserFromService` | `+0x11f` | `+0x124` | `GetActiveConnectionNameRAS` | [정적] |
| `winhttp!GetActiveConnectionName` (rva `0x5238c`) | `+0x77` | `+0x7c` | `GetActiveConnectionNameRAS` (rva `0x525a4`) | [정적] |
| `winhttp!GetActiveConnectionNameRAS` | `+0x123` | `+0x12a` | `iphlpapi!GetAdaptersAddresses` (가져오기 칸) | [정적] |
| `winhttp!GetActiveConnectionNameRAS` | `+0x1e3` · `+0x2d3` | `+0x1ea` · `+0x2da` | `iphlpapi!GetBestInterfaceEx` (IPv6 `::` → 실패면 IPv4 `0.0.0.0`) | [정적] |
| `winhttp!PrepareWebIOSessionForRequest` (rva `0x77b4`) | `+0x46` | `+0x4b` | `CreateWebIOSession` (rva `0x7890`) | [정적] |
| `winhttp!CreateWebIOSession` | `+0x47` | `+0x4c` | `WINHTTP_GLOBALS::GetWebIOApiHandle` (rva `0x79f4`) | [정적] |
| `winhttp!WINHTTP_GLOBALS::GetWebIOApiHandle` | `+0x4e` | `+0x53` | `WINHTTP_DLL::GetWebIOApiHandle` (rva `0x80f4`) | [정적] |
| `winhttp!WINHTTP_DLL::GetWebIOApiHandle` | `+0x3d` | `+0x44` | `webio!WebInitialize` (Ghidra `Ordinal_17`) — 핸들이 이미 있으면 건너뜀 | [정적] |
| `webio!WebInitialize` | `+0x187` | `+0x18c` | `WapInitialize` | [정적] |
| `webio!WapInitialize` | `+0x37` | `+0x3c` | `WaIncrementDllInitializationCount` | [정적] |
| `webio!WaIncrementDllInitializationCount` | `+0x56` | `+0x5b` | `WapInitializeDll` | [정적] |
| `webio!WapInitializeDll` | `+0x17` | `+0x1c` | `WaInitializeUtil` | [정적] |
| `webio!WaInitializeUtil` | `+0xfa` | `+0xff` | `WapInitializeNetworkChange` (rva `0x4a748`) | [정적] |
| `webio!WapInitializeNetworkChange` | `+0xa0` | `+0xa5` | `WxRegisterForNetworkChangeNotification` | [정적] |
| `webio!WxRegisterForNetworkChangeNotification` | `+0x23` | `+0x28` | `StartGlobalNetworkChangeMonitor` (rva `0x4aab4`) | [정적] |
| `webio!StartGlobalNetworkChangeMonitor` | `+0x89` | `+0x8e` | `Startup` (rva `0x4abe8`) | [정적] |
| `webio!Startup` | `+0xad` | `+0xb4` | `iphlpapi!NotifyIpInterfaceChange` | [정적] |
| `webio!Startup` | `+0xea` | `+0xf1` | `iphlpapi!NotifyUnicastIpAddressChange` | [정적] |
| `iphlpapi!NotifyIpInterfaceChange` (rva `0x9880`) | `+0x9b` | `+0xa0` | `InternalRegisterChangeNotification` (rva `0x9758`) | [정적] |
| `iphlpapi!NotifyUnicastIpAddressChange` | `+0x19` | `+0x1e` | `NetioRegisterAddressChangeNotification` | [정적] |
| `iphlpapi!NetioRegisterAddressChangeNotification` | `+0x9d` | `+0xa2` | `InternalRegisterChangeNotification` | [정적] |
| `iphlpapi!InternalRegisterChangeNotification` | `+0x5e` | `+0x63` | `NsiNotificationConnectToServer` (rva `0x1b18c`) — NULL 이면 `+0x6b` 6 | [정적] |
| `iphlpapi!InternalRegisterChangeNotification` | `+0x9d` · `+0xd5` | `+0xa4` · `+0xdc` | `winnsi!NsiRpcRegisterChangeNotification` (이름만 → run129 예정) | [정적] |
| `iphlpapi!NsiNotificationConnectToServer` | `+0x26` | `+0x2d` | `winnsi!NsiConnectToServer` (RPC, `ncalrpc`) | [정적] |
| `iphlpapi!GetAdaptersAddresses` (rva `0x1dc0`) | `+0x5b` | `+0x60` | `AllocateAndGetAdaptersAddresses` (rva `0x2990`) | [정적] |
| `iphlpapi!AllocateAndGetAdaptersAddresses` | `+0xc1` | `+0xc8` | `nsi!NsiGetAllParameters` (NDIS 객체 8) | [정적] |
| `iphlpapi!AllocateAndGetAdaptersAddresses` | `+0x15f` · `+0x1c9` · `+0x236` | `+0x166` · `+0x1d0` · `+0x23d` | `nsi!NsiAllocateAndGetTable` (NDIS 1 · IPv4 7 · IPv6 7) | [정적] |
| `iphlpapi!AllocateAndGetAdaptersAddresses` | `+0xb2d` · `+0xb64` | `+0xb32` · `+0xb69` | `AddDhcpConfiguration` · `AddNetbtConfiguration` (어댑터 있으면 늘, NSI 아님) | [정적] |
| `iphlpapi!GetAdaptersInfo` (rva `0x8170`) | `+0x24` | `+0x29` | `AllocateAndGetAdaptersInfo` (rva `0x6c90`) | [정적] |
| `iphlpapi!AllocateAndGetAdaptersInfo` | `+0x84` | `+0x8b` | `nsi!NsiAllocateAndGetTable` (NDIS 객체 1) | [정적] |
| `iphlpapi!AllocateAndGetAdaptersInfo` | `+0xb8` | `+0xbd` | `GetCurrentThreadCompartmentId` | [정적] |
| `iphlpapi!AllocateAndGetAdaptersInfo` | `+0x151` | `+0x158` | `nsi!NsiGetAllParameters` (IPv4 객체 7, 크기 모두 0 — 있나만) | [정적] |
| `iphlpapi!GetBestInterfaceEx` | `+0xe4` | `+0xeb` | `nsi!NsiGetParameter` (IPv4/IPv6 객체 0, ROD 4 B) | [정적] |
| `iphlpapi!GetCurrentThreadCompartmentId` (rva `0xb2d0`) | `+0x3f` | `+0x46` | `nsi!NsiGetParameter` (NDIS 객체 8, RW 4 B) | [정적] |
| `iphlpapi!GetCurrentThreadCompartmentScope` (rva `0x104a0`) | `+0x4f` | `+0x56` | `nsi!NsiGetAllParameters` (NDIS 객체 8, RW 8 B — 같은 칸 뜻의 근거) | [정적] |
| `dnsapi!Query_PrivateExW` (rva `0x1ef8c`) | `+0x60b` | `+0x612` | `iphlpapi!GetCurrentThreadCompartmentId` | [정적] |
| `dnsapi!Query_PrivateExW` | `+0x851` · `+0x90d` | `+0x856` · `+0x912` | `Rpc_ResolverQuery` (DNS 클라이언트 서비스 RPC, 이름만) | [정적] |
| `nsi!NsiGetAllParameters` (rva `0x2150`) | `+0x105` | `+0x10c` | `NtDeviceIoControlFile` (`0x12000f`, `0x68`) | [정적] |
| `nsi!NsiGetAllParameters` | `+0x1a6` | `+0x1ad` | `CreateFileW(L"\\.\Nsi", …, FILE_FLAG_OVERLAPPED)` | [정적] |
| `nsi!NsiGetParameter` | `+0xe0` | `+0xe7` | `NtDeviceIoControlFile` (`0x120007`, `0x50`) | [정적] |
| `nsi!NsiAllocateAndGetTable` | `+0x119` · `+0x36b` | `+0x120` · `+0x372` | `NtDeviceIoControlFile` (`0x12001b`, `0x70` — 개수 묻기 · 받기) | [정적] |

### 1. 부르는 모듈 가리기 (가져오기 표) [정적]

| 모듈 | nsi.dll 에서 가져오는 것 (모두 **지연** 가져오기) | 이번 길에서 |
|---|---|---|
| winhttp | `NsiAllocateAndGetTable` · `NsiFreeTable` 둘뿐 | `WinHttpSendRequest` 시작. NSI 는 IPHLPAPI (`GetAdaptersAddresses` · `GetBestInterfaceEx` · `NotifyIpInterfaceChange` 등) 를 거쳐 닿음 |
| webio | **없음** | IPHLPAPI 알림 등록 (`NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange`), `GetIpInterfaceEntry` |
| IPHLPAPI | `NsiGetParameter` · `NsiGetAllParameters` · `NsiEnumerateObjectsAllParameters` · `NsiAllocateAndGetTable` · `NsiFreeTable` · `NsiSet*` · `NsiRequestChangeNotification` · `NsiCancelChangeNotification` (+ `WINNSI.DLL` 의 `NsiRpc*` · `NsiConnectToServer`) | **실제로 묻는 쪽** |
| dnsapi | `NsiGetParameter` · `NsiGetAllParameters` · `NsiAllocateAndGetTable` · `NsiFreeTable` | `GetAddrInfoW` 길의 compartment 묻기는 IPHLPAPI 를 거침 |
| ws2_32 (배제용) | `NsiGetParameter` · `NsiGetAllParameters` · `NsiSetAllParameters` | `GetAddrInfoW` 길에서 **안 부름** (부르는 자리 셋의 caller 는 `WSAConnectByNameHelper` 뿐) |

- `objdump -p` 의 "DLL Name" 줄만 보면 지연 가져오기를 놓친다 (결과서).

### 2. 갈래 F2 — 프록시 이름 길 (NSI 장치를 부름, 오류 삼킴) [정적]

| 단계 | 함수 | 조건 · 인자 |
|---|---|---|
| 1 | `WinHttpSendRequest +0x2fa` → `SendRequest` | |
| 2 | `SendRequest +0x294` → `_CreateProxyResolver` | `[this+0xcc] == 0` 일 때만 (첫 보냄) |
| 3 | `_CreateProxyResolver +0x2a` / `+0x55` → `SafeGetProxyResolver` | 세션 `[+0x90]` ≠ 0 이면 인자 1 먼저 (`0x80070490` 이면 인자 0 으로 다시), 아니면 인자 0 |
| 4 | `SafeGetProxyResolver +0xbe` → `CheckProxySettings` | 루트 세션 핸들까지 올라가서 |
| 5 | `CheckProxySettings +0x37c` → `GetActiveConnectionName` | **`g_ullNetworkGeneration` (`+0x85` · `+0xa4`) ≠ 세션의 `[this+0x228]` (`+0x91` · `+0xab`) 일 때만.** 둘 다 시작 0, 올리는 것은 `WinHttpNetworkChangeNotification` 뿐 → **네트워크 바뀜 알림이 없으면 이 줄은 안 탄다** |
| 5' | `LoadAutomaticProxyResolvers +0x224` → `GetActiveConnectionName` | 부르는 쪽 = 세션 가상 `SetProxySettings` (vtable `+0x10`) 하나 — **언제 불리는지 `[공백]`** |
| 6 | `GetActiveConnectionName +0x77` → `GetActiveConnectionNameRAS` | |
| 7 | `GetActiveConnectionNameRAS +0x123` → `GetAdaptersAddresses(0, 0x80f, NULL, 버퍼 0x7800, &크기)` | `0x6f` (버퍼 모자람) 면 `+0x800` 키워 최대 3번, 그 밖 오류는 바로 포기 |
| 7' | 같은 함수 `+0x1e3` / `+0x2d3` → `GetBestInterfaceEx` | `GetAdaptersAddresses` 성공일 때만. `sockaddr_in6` (`::`) 먼저, 실패면 `sockaddr_in` (`0.0.0.0`). 둘 다 실패면 `0x80070490` |
| 8 | `GetAdaptersAddresses +0x5b` → `AllocateAndGetAdaptersAddresses` | 계열은 0 · 2 · 23 만 (아니면 `0x57`). 플래그 `0x80f` = 유니캐스트 · 애니캐스트 · 멀티캐스트 · DNS 서버 건너뜀 + `0x800` `[추측: SDK 상수 이름]` |
| 9 | `AllocateAndGetAdaptersAddresses +0xc1` → `NsiGetAllParameters`, `+0x15f` · `+0x1c9` · `+0x236` → `NsiAllocateAndGetTable` ×3 | 인자는 5절 |
| 10 | `NsiGetAllParameters +0x105` → `NtDeviceIoControlFile(0x12000f, 0x68)` | 칸은 6절 |

- 플래그 `0x80f` 라 `AddUnicastAddresses` · `AddAnycastAddresses` · `AddMulticastAddresses` · `AddDnsConfiguration` 은 건너뛴다. 대신 **`AddDhcpConfiguration` (`+0xb2d`) · `AddNetbtConfiguration` (`+0xb64`) 은 어댑터가 있으면 늘** 부르고, 0 이 아니면 `GetAdaptersAddresses` 가 통째로 실패한다 (`+0xb34` · `+0xb6b`). 둘은 NSI 가 아니라 dhcpcsvc RPC · NetBT 장치 — 이름만 (어댑터 0 개면 안 탐).

### 3. 갈래 F1 — 웹 I/O 시작 길 (NSI 장치 아님, `WinHttpSendRequest` 를 0 으로 만들 수 있음) [정적 + 공백]

사슬은 위 호출 지점 표 (`+0x25a` → … → `InternalRegisterChangeNotification` → `NsiNotificationConnectToServer` → `winnsi!NsiConnectToServer`).

- **실패가 올라가는 줄** (결과서 + flow-writer 대조):
  - 연결 핸들 NULL → `InternalRegisterChangeNotification +0x66 ~ +0x6b` 가 6 (`ERROR_INVALID_HANDLE`) → `NotifyIpInterfaceChange` 가 그대로 돌려줌.
  - → webio `Startup +0xbc TEST EAX,EAX` 오류 → `WapInitializeNetworkChange` (`+0xa7 WX_WIN32_FROM_HR` · `+0xac MOV EBX,EAX` · 끝 `+0x156 MOV EAX,EBX`) → `WaInitializeUtil` 실패 → `WebInitialize` 실패.
  - → `GetWebIOApiHandle` 0 아님 → `CreateWebIOSession` → `PrepareWebIOSessionForRequest +0x4b ~ +0x4f` (`TEST` / `JNZ`) → `WinHttpSendRequest +0x25f ~ +0x263` (`TEST` / `JNZ`) → `SetLastError` 뒤 0.
- `WebInitialize` 는 한 번 성공하면 핸들을 전역에 둔다 (`GetWebIOApiHandle__80f4` — `[전역+0x28]` ≠ 0 이면 `+0x3d` 건너뜀). `WinHttpOpen +0x147 → WinHttpSetOptionInternal → CreateStaticResolverCacheEntry → CreateWebIOSession` 에서 먼저 돌았을 수도 있다 (결과서).
- **이 줄이 F2 보다 앞** (`+0x25a` < `+0x2fa`) → 로그에 `0x12000f` 가 찍힌 `WinHttpSendRequest` 는 이 줄을 지난 것이다. 그러니 **"0x12000f 가 보인 호출의 0 반환" 원인은 정적으로 못 찾았다** `[공백]`. 이 줄은 "`0x12000f` 가 안 보이고 일찍 0 이 되는 샘플" 의 후보.
- `NsiConnectToServer` 가 실제로 서버를 만나는지 (`RpcBindingFromStringBindingW` 는 바인딩 핸들만 만든다) 는 winnsi 를 안 읽어 `[공백]` — sast/127 (Q223) → **run129 예정**.
- webio `Startup` 은 `InitialNotification = FALSE` 로 등록 → 콜백은 곧바로 안 불림 (결과서).

### 4. 갈래 F3 — 이름 풀이 길 (참고, 처음엔 안 탐) [정적]

- webio `WapTcpStartDnsQuery` → `WaStartDnsQuery` → … → `WapDnsResolveGeneric +0x1ef` → `ws2_32!GetAddrInfoExW`. `WaStartDnsQuery` → `WapDnsResolveGeneric` 사이는 안 봄 `[공백]`. 이 길의 NSI 는 10절의 `GetCurrentThreadCompartmentId` 하나.
- `WapTcpStartDnsQuery +0x28f` → `WaMapInterfaceToAddressFamily` → `GetIpInterfaceEntry` 는 **기본 설정에선 안 탄다** — 인터페이스 지정 (`[연결+0x165]` ≠ 0) · 계열 미지정 (`[+0x3d8]` == 0) · 인터페이스 번호 (`[+0x140]` ≠ 0) 셋이 다 맞아야 (`+0x265 ~ +0x286`). 오류여도 로그만 (`+0x294`). `0x490` · `0x32` 는 "그 계열엔 없음" 으로 읽음 (`+0xe2 ~ +0xfe`).

### 5. `GetAdaptersAddresses(0, 0x80f)` 가 NSI 에 넣는 인자 [정적]

인자 순서 `(첫 인자, 모듈 ID, 객체, 키, 키 크기, RW, RW 크기, ROD, ROD 크기, ROS, ROS 크기, [개수])` — nsi.dll 이 구조체로 옮기는 줄에서 확인 (6절). 칸 이름 RW · ROD · ROS 는 `[추측]` (아래).

| 호출 (`AllocateAndGetAdaptersAddresses`) | 첫 인자 | 모듈 | 객체 | 키 | RW | ROD | ROS | 받는 반환 |
|---|---|---|---|---|---|---|---|---|
| `+0xc1` `NsiGetAllParameters` | 1 | NDIS | **8** | 없음 | **8 B** | 0 | 0 | 무엇이든 — 오류면 번호 1 로 이어 감 (`+0xcf JNZ → +0xf71` · `+0xf74 MOV R13D,1` · `+0xf7a JMP +0xdc`) |
| `+0x15f` `NsiAllocateAndGetTable` #1 | 1 | NDIS | **1** | 8 B (LUID) | 0 | **`0x290` /개** | **`0x238` /개** | **0 만** (`+0x16f JNZ`) |
| `+0x1c9` #2 | 1 | IPv4 | **7** | 8 B | **`0xf0` /개** | 0 | **`0x20` /개** | 0 · **`0x32`** (`+0x1d9 → +0xfe7 CMP EAX,0x32`) |
| `+0x236` #3 | 1 | IPv6 | **7** | 8 B | `0xf0` | 0 | `0x20` | 0 · `0x32` |

- **RW 8 B 뜻** = `{+0: compartment 번호, +4: 범위}`. 근거 = `GetCurrentThreadCompartmentScope` 가 같은 호출로 읽어 `[+4]` 를 범위, `[+0]` 을 번호로 내놓음 (실패면 `{1, 0}`). `AllocateAndGetAdaptersAddresses` 는 `[+0]` 으로 어댑터를 거른다 (`+0xa26 CMP [ROD(i)+0], R13D`) — 플래그 `0x200` 이 없으면 **ROD 첫 dword (CompartmentId) 가 이 번호와 같은 어댑터만** 남는다.
- `+0xd14` / `+0xf3e` `NsiSetParameter` 는 플래그 `0x200` 일 때만 — 이 길엔 안 탐.
- **`NsiAllocateAndGetTable` 되풀이** (nsi.dll): 첫 호출은 **포인터 모두 0** 으로 개수만 (`+0x119`) → 개수 + `max(개수/8, 10)` 칸 잡기 (`+0x16c ~ +0x185`) → 둘째 호출 (`+0x36b`). 둘째가 status 0 이면 끝 (`+0x39f`). `RtlNtStatusToDosError` 값이 **`0xea` (ERROR_MORE_DATA — `0x80000005` 에서 옴) 면 더 큰 버퍼로 다시** (`+0x3b8`), 최대 3번 (`+0x48`). 첫 호출의 `0xea` 도 받음 (`+0x4ae`). → **빈 표 = 첫 호출 status 0 · 개수 0, 둘째 호출 status 0 · 개수 0.**

### 6. 구조체 칸 표 셋 (입력 = 출력 같은 버퍼, METHOD_NEITHER) [정적]

공통: 구조체 안의 포인터 (키 · RW · ROD · ROS) 는 **사용자 주소** — 커널이 따라가서 읽고 쓴다. IOSB 는 따로 16 B. nsi.dll 은 매번 `CreateEventA` 로 이벤트를 만들어 넘기고, `0x103` 이면 `NtWaitForSingleObject` 뒤 `IOSB.Status`. 칸 이름 "키 / RW (읽고쓰기 설정) / ROD (읽기 전용 동적 = 상태) / ROS (읽기 전용 고정)" 은 PDB 에 없고, 코드가 같은 순서로 (포인터, 크기) 쌍을 쓰는 것 + `NsiSetParameter` 가 0 = RW 로 쓰는 것으로 맞춘 `[추측]`.

**① `0x12000f` — `NsiGetAllParameters(Ex)` — `0x68` B** (입력 · 출력 길이 `0x68`). 비-Ex 가 인자를 옮기는 줄 `+0x20 ~ +0x86`.

| 오프셋 | 크기 | 뜻 |
|---|---|---|
| `+0x00` · `+0x08` | 8 · 8 | 0 (nsiproxy 는 `구조체+8` 을 검사 함수에 넘겨 `[+8+8]` 로 모듈 ID 를 읽음) |
| **`+0x10`** | 8 | **모듈 ID 포인터** (`NPI_MODULEID` 0x18 B) |
| **`+0x18`** | 4 | **객체 번호** |
| **`+0x20`** | 4 | **첫 인자** (모든 호출자 1 — "저장소" 로 짐작 `[추측]`) |
| **`+0x28`** / `+0x30` | 8 / 4 | 키 포인터 / 크기 |
| **`+0x38`** / `+0x40` | 8 / 4 | RW 포인터 / 크기 |
| **`+0x48`** / `+0x50` | 8 / 4 | ROD 포인터 / 크기 |
| **`+0x58`** / `+0x60` | 8 / 4 | ROS 포인터 / 크기 (끝 `0x68`) |

- 끝난 뒤 (비-Ex 만): **`+0x139` — 커널이 돌려준 RW 크기 (`+0x40`) 가 요청보다 작으면 `0xc000000d`** (`+0x22d`). 안 건드리면 통과.
- nsiproxy: 입력 < `0x68` → `0xC000000D`, 성공 `Information = 0x68`. 공급자 크기가 다르면 사용자 구조체의 크기 칸을 그 값으로 덮어씀 (오류 아님).

**② `0x120007` — `NsiGetParameter(Ex)` — `0x50` B** (입력 `0x50`, **출력 길이 = 데이터 크기** (`+0x48`), 출력 버퍼 = 구조체 시작 — 실제 값은 `+0x40` 이 가리키는 곳에 써짐). 비-Ex 옮기는 줄 `+0x22 ~ +0x62`.

| 오프셋 | 크기 | 뜻 |
|---|---|---|
| `+0x00` | 16 | 0 |
| `+0x10` · `+0x18` · `+0x20` · `+0x28` / `+0x30` | | ①과 같음 (모듈 · 객체 · 첫 인자 · 키) |
| **`+0x38`** | 4 | **어느 묶음**: 0 = RW, 1 = ROD (`GetCurrentThreadCompartmentId` 0, `GetBestInterfaceEx` 1 — 이름 `[추측]`, 2 = ROS 일 것 `[추측]`) |
| **`+0x40`** | 8 | **결과 받을 칸 포인터** |
| **`+0x48`** | 4 | **결과 크기** — Ex 는 끝난 뒤 `IOSB.Information` 을 여기 되씀 (`NsiGetParameterEx +0xb5 ~ +0xb9`) |
| **`+0x4c`** | 4 | **묶음 안 시작 오프셋** |

**③ `0x12001b` — `NsiEnumerateObjectsAllParameters(Ex)` · `NsiAllocateAndGetTable` — `0x70` B** (입력 · 출력 `0x70`). 비-Ex 옮기는 줄 `+0x12 ~ +0x7d`, `NsiAllocateAndGetTable` 은 `+0x5c ~ +0x9a`.

| 오프셋 | 크기 | 뜻 |
|---|---|---|
| `+0x00` | 16 | 0 |
| **`+0x10`** · **`+0x18`** · **`+0x20`** | 8 · 4 · 4 | 모듈 ID 포인터 · 객체 번호 · 첫 인자 |
| **`+0x24`** | 4 | **둘째 인자** — `NsiAllocateAndGetTable` 은 늘 1 (`+0x93`) |
| `+0x28` / `+0x30` · `+0x38` / `+0x40` · `+0x48` / `+0x50` · `+0x58` / `+0x60` | 8 / 4 ×4 | 키 · RW · ROD · ROS **버퍼 포인터 / 1개당 크기** (개수 묻기 땐 포인터 0) |
| **`+0x68`** | 4 | **개수** (들어갈 땐 칸 수, 나올 땐 실제 개수) — 비-Ex 는 끝난 뒤 `[개수 포인터]` 에 되씀 (`+0x148` · `+0x14b`) |

- 배열은 **칸 크기 × 개수** 연속 — 어댑터 i 의 ROD = `ROD 포인터 + i × 0x290` (`AllocateAndGetAdaptersAddresses +0x31e IMUL`, ROS `+0x27f`).

### 7. `GetAdaptersAddresses` 가 대답에서 읽는 칸 (루프백 줄 짤 때) [정적 + 추측]

값은 공급자 (ndis · tcpip) 가 정해 정적으론 모른다. 아래는 "어느 칸을 어디에 쓰나" (이름은 SDK `IP_ADAPTER_ADDRESSES` 칸 순서와 맞춘 `[추측]`).

- **NDIS 객체 1 (어댑터 표)** — 키 8 B = 어댑터 LUID. ROD: `+0x000` CompartmentId (거르기) · `+0x004` NetworkGuid · `+0x014` / `+0x016` 별칭 바이트 길이 / UTF-16 · `+0x218` OperStatus (1 = 올라감) · `+0x21c` 상태 비트 · `+0x220` Mtu · `+0x224` / `+0x226` **물리 주소 길이 / 주소 (루프백 = 길이 0)** · `+0x270` / `+0x278` 송 · 수신 속도. ROS: `+0x000` IfIndex · `+0x004` / `+0x006` Description · **`+0x208` IfType (루프백 24 = `0x18`)** · `+0x20c` / `+0x210` 플래그용 · `+0x214` · `+0x218` 인터페이스 GUID → `AdapterName` `"{…}"` · `+0x22c`.
- **IPv4 · IPv6 객체 7 (IP 인터페이스 표)** — 키 8 B = LUID (NDIS 표 LUID 와 같으면 같은 어댑터). RW: `+0x01c` Metric · `+0x038 ~ +0x077` ZoneIndices `[추측]` · `+0x078` Mtu · `+0x00a` / `+0x00b` IPv6 플래그. ROS: `+0x004` / `+0x008` (`ROS+8 == 0xe` 면 IfType 을 `ROS+4` 로).
- **IP 인터페이스가 없는 NDIS 어댑터는 빠진다** (플래그 `0x100` 없을 때, `+0x9df TEST [R15+0x5c],0x180`).
- 루프백을 따로 다루는 줄은 `GetAdaptersAddresses` 에 **없다**. 값 (OperStatus · Mtu · 속도 · 별칭 등) 은 `TODO(VM)`.

### 8. 부른 쪽이 대답을 어떻게 읽나 [정적]

| 호출 | 보는 것 | 줄 |
|---|---|---|
| nsi `NsiGetAllParameters` · `NsiEnumerateObjectsAllParameters` · `NsiAllocateAndGetTable` | `0x103` 이면 기다린 뒤 `IOSB.Status`. **음수 또는 `0x105`** 면 `RtlNtStatusToDosError` 값 (0 이면 성공으로 감). `Information` 은 안 봄. `NsiGetAllParameters` 만 RW 크기 칸 검사, `NsiAllocateAndGetTable` 둘째 호출만 `0xea` 다시 | `NsiGetAllParameters +0x113 ~ +0x150` · `+0x139`; `NsiAllocateAndGetTable +0x127 ~ +0x163` · `+0x377 ~ +0x3b2` |
| nsi `NsiGetParameter(Ex)` | 오류 (`0xC…`) 면 DOS 값만. **`0x105` 와 경고 (`0x8…`) 면 `Information` 을 `+0x48` 에 써 준 뒤** DOS 값 (필요 크기 돌려주기 — 예 `0x80000005` → `0xea` `[추측]`). 성공이면 Ex 는 `+0x48` 에 씀 | `NsiGetParameter +0x112` · `+0x13c ~ +0x158`; `NsiGetParameterEx +0xad` · `+0xb5 ~ +0xb9` |
| 열기 (`NsiGetAllParameters` 안) | `CreateFileW(L"\\.\Nsi", 0, 3, NULL, OPEN_EXISTING, FILE_FLAG_OVERLAPPED)`. **−1 이고 `GetLastError() == 0` 이면 오류로 안 보고 진행** → 핸들 −1 로 IOCTL → `0xc0000024`. `GetLastError` ≠ 0 이면 그 값을 돌려줌 | `+0x1a6` · `+0x1e2 CALL GetLastError` · `+0x1ee TEST` · `+0x1f0 JZ` · `+0x1f6 JMP` |
| `GetAdaptersAddresses` | 5절 표의 "받는 반환". 어댑터 0 개면 **`0xe8`**, 버퍼 모자라면 `0x6f` + 필요 크기 | `AllocateAndGetAdaptersAddresses +0xcd ~ +0xf74` · `+0x16f` · `+0x1d9` · `+0x246` |
| `winhttp!GetActiveConnectionNameRAS` | `0x6f` 면 키워 다시 (최대 3), 다른 오류면 그 HRESULT 로 끝. 성공이면 `GetBestInterfaceEx` 인덱스와 `IfIndex` 가 맞는 어댑터의 **FriendlyName** 을 씀, 못 찾으면 `0x80070490` | `GetActiveConnectionNameRAS.c` (결과서) |
| `winhttp!GetActiveConnectionName` | RAS 가 음수면 **NULL** — 오류 전파 없음 | `GetActiveConnectionName.c` (결과서) |
| `CheckProxySettings` · `LoadAutomaticProxyResolvers` | 이름 NULL 이어도 레지스트리 프록시 읽기 · 해석기 만들기로 이어 감 | 결과서 A5 |
| `GetBestInterfaceEx` | `NsiGetParameter` 반환을 **그대로**. 출력 4 B 인터페이스 번호 | `GetBestInterfaceEx.asm +0xe4 ~ +0xf3` |
| webio `WapInitializeNetworkChange` (F1) | `NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange` 가 0 이 아니면 **초기화 실패 → `WinHttpSendRequest` 0** | 3절 |

### 9. `WinHttpSendRequest` 가 이름 풀이 · 소켓까지 가려면 — 최소 대답 [정적 + 공백]

1. **F2 (NSI 장치 — `0x12000f` · `0x12001b` · `0x120007`)**: 필수 대답 **없음** (오류를 다 삼킴). 정직한 대답 둘 중 아무거나:
   - (a) **어댑터 0 개 PC**: 열기 성공 → `0x12000f` (NDIS 객체 8, RW 8 B) 에 `{1, 0}` · status 0 → `0x12001b` NDIS 객체 1 첫 호출 status 0 · `+0x68` = 0, 둘째 호출 (칸 수 10) status 0 · 개수 0 → IPv4 · IPv6 객체 7 도 개수 0 (또는 "모듈 없음" = DOS `0x32` 가 되는 NTSTATUS — `STATUS_NOT_SUPPORTED` 로 짐작 `[추측]`) → `GetAdaptersAddresses` = `0xe8` → 이름 NULL.
   - (b) **루프백 1 개 PC**: NDIS 표 1 개 · IPv4 표 1 개 (IPv6 0 또는 1), 칸 값은 7절. → winhttp 가 `GetBestInterfaceEx` 두 번 → 그 `NsiGetParameter` (IPv6 / IPv4 모듈, **객체 0**, 키 `{+0: 0, +4: 주소, v6 은 +0x14 범위 ID}` 크기 `0x38` / `0x20`, 묶음 1 (ROD), 4 B, **오프셋 `0x7c` (v6) / `0x58` (v4)**) 가 오류면 이름 NULL 일 뿐.
2. **F1 (웹 I/O 시작)**: `WebInitialize` 가 한 번도 성공 못한 프로세스면 `NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange` 가 **0 을 돌려줘야** 한다. 이 둘은 **winnsi RPC** — NSI 장치 대답으로는 못 고친다 → run129 예정.
3. **이름 풀이 (F3)**: NSI 는 `GetCurrentThreadCompartmentId` 한 번 (실패하면 1 → 삼킴). 이름 풀이 본문은 `Rpc_ResolverQuery` (DNS 클라이언트 서비스 RPC) — 범위 밖.

- 요청 쪽 로그의 "같은 `0x12000f` 가 한 번 더" — `GetActiveConnectionName` 이 두 곳 (`CheckProxySettings +0x37c` · `LoadAutomaticProxyResolvers +0x224`) 에서 불린다 `[추측]`. `0x6f` 재시도는 NSI 실패 (`ERROR_INVALID_HANDLE` 등) 에선 안 돈다.
- 못 닫은 것: 루프백 줄 실제 바이트, 어댑터 0 개일 때 진짜 `GetBestInterfaceEx` 오류 번호 (`0x4d3` 일지 `0x490` 일지) — `TODO(VM)`.

### 10. 나머지 API 넷 [정적]

- **`WinHttpGetIEProxyConfigForCurrentUser`**: `+0xc3` → `GetActiveConnectionName` (F2 6 ~ 10 과 같음), 그리고 `+0x267` → `GetIEProxyConfigForCurrentUserFromService +0x11f` → `GetActiveConnectionNameRAS`. 이름 (NULL 가능) 을 프록시 설정 읽기에 넘길 뿐 — **통째 실패 아님.** 요청 쪽 표의 "`0x12000f` + `0x12001b`" 중 `0x12001b` 는 첫 `0x12000f` 를 통과한 뒤에야 나오는데, 첫 호출이 늘 실패하는 지금 에뮬레이터에서 같이 보이면 다른 호출자 (`GetAdaptersInfo` 등) 일 것 `[추측]`.
- **`GetAdaptersAddresses` (exe 가 직접, 다른 플래그)**: 어댑터마다 NSI 가 더 붙는다 — `AddUnicastAddresses` (`+0xbf8` · `+0xc25`, 객체 **`0xa`**) · `AddAnycastAddresses` (`+0xe1f` · `+0xe40`, 객체 **8**) · `AddMulticastAddresses` (`+0xe7d` · `+0xe9e`, 객체 **9**) · `AddGatewaysAndPrefixes` (`+0xdaa` · `+0xdd1`, 객체 **`0x10`**) · `AddDnsConfiguration` (`+0xcc5`, dnsapi `GetRADnsSettingsImpl` → `NsiGetAllParameters`). 반환 0 · **`0x490` (항목 없음) · `0x32` (모듈 없음)** 은 정상으로 받음.
- **`GetAddrInfoW` · `getaddrinfo` · `gethostbyname`**: ws2_32 는 이 길에서 NSI 를 안 부른다 (1절). NSI 는 dnsapi `Query_PrivateExW +0x60b` → `iphlpapi!GetCurrentThreadCompartmentId +0x3f` → `NsiGetParameter(1, NDIS, 8, 키 없음, 묶음 0 (RW), 4 B, 오프셋 0)` (`0x120007` · `0x50`). **실패하면 번호 1 → 삼킴.** `GetAddrInfoW` → … → mswsock `SaBlob_Query +0x186` → `DnsQueryEx` 사이는 안 봄 `[공백]`. 이름 풀이의 진짜 막힘은 NSI 가 아니라 `Rpc_ResolverQuery` (`+0x851` · `+0x90d`) 쪽일 수 있다 (범위 밖).
- **`GetAdaptersInfo`**: `+0x24` → `AllocateAndGetAdaptersInfo +0x84` `NsiAllocateAndGetTable` (NDIS 객체 1, `GetAdaptersAddresses` 와 같은 인자) → `+0xb8` `GetCurrentThreadCompartmentId` → 어댑터마다 컴파트먼트 거르기 (`+0xe6`) · **`IfType == 0x18` (루프백) 건너뜀** (`+0xf7`) → `+0x151` `NsiGetAllParameters(1, IPv4, 객체 7, 키 LUID, 크기 모두 0)` = **"그 LUID 의 IPv4 인터페이스가 있나" 만** (오류면 건너뜀, `+0x15d ~ +0x15f`). → **루프백만 있는 PC = `0xe8`** (표 0 개여도 같음). 첫 `NsiAllocateAndGetTable` 오류는 그대로 돌려줌 (`+0x90 ~ +0x92`).

### 11. 모듈 ID 값 (IPHLPAPI `.rdata`) [정적]

| PDB 이름 | rva | 24 B = `{DWORD 0x18, DWORD 1, GUID}` | GUID |
|---|---|---|---|
| `NPI_MS_NDIS_MODULEID` | `0x25350` | `18000000 01000000 114a00eb 1a9b d411 9123 0050047759bc` | `eb004a11-9b1a-11d4-9123-0050047759bc` |
| `NPI_MS_IPV6_MODULEID` | `0x25368` | `… 014a00eb …` | `eb004a01-9b1a-11d4-9123-0050047759bc` |
| `NPI_MS_IPV4_MODULEID` | `0x25380` | `… 004a00eb …` | `eb004a00-9b1a-11d4-9123-0050047759bc` |

- 그 밖 PDB 이름만 확인 (`NPI_MS_TCP_MODULEID` · `NPI_MS_UDP_MODULEID` · `NPI_MS_FL4/6…`) — 값은 안 읽음.

### 호출 관계

```
(PE_emulator 요청 — test.exe 초기화 밖)
winhttp!WinHttpSendRequest
 ├─ (F1) +0x25f  PrepareWebIOSessionForRequest                      실패 → WinHttpSendRequest 0
 │        └─ +0x4b  CreateWebIOSession
 │                 └─ +0x4c  WINHTTP_GLOBALS::GetWebIOApiHandle
 │                          └─ +0x53  WINHTTP_DLL::GetWebIOApiHandle
 │                                   └─ +0x44  webio!WebInitialize    (핸들 있으면 건너뜀)
 │                                            └─ +0x18c WapInitialize → +0x3c WaIncrementDllInitializationCount
 │                                                 → +0x5b WapInitializeDll → +0x1c WaInitializeUtil
 │                                                 → +0xff WapInitializeNetworkChange → +0xa5 WxRegisterForNetworkChangeNotification
 │                                                 → +0x28 StartGlobalNetworkChangeMonitor → +0x8e Startup
 │                                                      ├─ +0xb4  iphlpapi!NotifyIpInterfaceChange
 │                                                      │         └─ +0xa0  InternalRegisterChangeNotification
 │                                                      │                  ├─ +0x63  NsiNotificationConnectToServer
 │                                                      │                  │         └─ +0x2d  winnsi!NsiConnectToServer   (RPC — [공백] → run129)
 │                                                      │                  └─ +0xa4 · +0xdc  winnsi!NsiRpcRegisterChangeNotification (이름만)
 │                                                      └─ +0xf1  iphlpapi!NotifyUnicastIpAddressChange
 │                                                                └─ +0x1e NetioRegisterAddressChangeNotification → +0xa2 InternalRegisterChangeNotification
 └─ (F2) +0x2ff  HTTP_USER_REQUEST::SendRequest                     NSI 오류 삼킴
          └─ +0x299  _CreateProxyResolver
                   └─ +0x2f / +0x5a  SafeGetProxyResolver
                            └─ +0xc3  CheckProxySettings          (+0x85 ~ +0xab 세대값 다를 때만)
                                     └─ +0x381  GetActiveConnectionName       (LoadAutomaticProxyResolvers +0x229 도)
                                              └─ +0x7c  GetActiveConnectionNameRAS
                                                       ├─ +0x12a  iphlpapi!GetAdaptersAddresses(0, 0x80f)
                                                       │          └─ +0x60  AllocateAndGetAdaptersAddresses
                                                       │                   ├─ +0xc8   nsi!NsiGetAllParameters      NDIS 8   → +0x10c NtDeviceIoControlFile 0x12000f
                                                       │                   ├─ +0x166  nsi!NsiAllocateAndGetTable   NDIS 1   → +0x120 · +0x372 NtDeviceIoControlFile 0x12001b
                                                       │                   ├─ +0x1d0  nsi!NsiAllocateAndGetTable   IPv4 7
                                                       │                   ├─ +0x23d  nsi!NsiAllocateAndGetTable   IPv6 7
                                                       │                   └─ +0xb32 AddDhcpConfiguration · +0xb69 AddNetbtConfiguration (어댑터 있으면)
                                                       └─ +0x1ea / +0x2da  iphlpapi!GetBestInterfaceEx → +0xeb nsi!NsiGetParameter 0x120007

dnsapi!Query_PrivateExW (GetAddrInfoW 길)
 ├─ +0x612  iphlpapi!GetCurrentThreadCompartmentId → +0x46 nsi!NsiGetParameter  NDIS 8 RW 4 B   (실패 → 1)
 └─ +0x856 / +0x912  Rpc_ResolverQuery                                   (범위 밖)

iphlpapi!GetAdaptersInfo
 └─ +0x29  AllocateAndGetAdaptersInfo
          ├─ +0x8b   nsi!NsiAllocateAndGetTable   NDIS 1
          ├─ +0xbd   GetCurrentThreadCompartmentId
          │  +0xf7 IfType 0x18 건너뜀
          └─ +0x158  nsi!NsiGetAllParameters      IPv4 7, 크기 0 (있나만)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 들여쓴 `+0x..` 는 그 함수 안 줄.
- webio 시작 사슬 (`WapInitialize` ~ `Startup`) 은 한 줄씩 caller 하나라 화살표로 이었다.

## 확인된 것

- `[정적]` winhttp · webio · IPHLPAPI · dnsapi · ws2_32 는 nsi 를 지연 가져오기로만 쓴다. winhttp 는 `NsiAllocateAndGetTable` · `NsiFreeTable` 둘만, webio 는 안 가져옴.
- `[정적]` `WinHttpSendRequest +0x2fa` → … → `GetActiveConnectionNameRAS +0x123 GetAdaptersAddresses(0, 0x80f)` → `AllocateAndGetAdaptersAddresses +0xc1 NsiGetAllParameters` → `+0x105 NtDeviceIoControlFile(0x12000f)`. 이 길의 NSI 오류는 `GetActiveConnectionName` 이 NULL 로 삼킨다.
- `[정적]` `CheckProxySettings +0x37c` 는 `g_ullNetworkGeneration` 과 세션 `+0x228` 이 다를 때만.
- `[정적]` `WinHttpSendRequest +0x25a` → … → `webio!WebInitialize` → … → `Startup +0xad NotifyIpInterfaceChange` · `+0xea NotifyUnicastIpAddressChange` → `InternalRegisterChangeNotification +0x5e NsiNotificationConnectToServer +0x26 winnsi!NsiConnectToServer`. 연결 핸들 NULL → 6 → `WebInitialize` 실패 → `WinHttpSendRequest` 0.
- `[정적]` 구조체 셋의 칸 순서 (`+0x10` 모듈 · `+0x18` 객체 · `+0x20` 첫 인자 · `+0x28` 키 · (포인터, 크기) 쌍), `0x50` 의 `+0x38` 묶음 · `+0x40` 결과 포인터 · `+0x48` 크기 · `+0x4c` 오프셋, `0x70` 의 `+0x24` 둘째 인자 · `+0x68` 개수.
- `[정적]` `NsiGetAllParameters(1, NDIS, 8)` RW 8 B = `{번호, 범위}` (`GetCurrentThreadCompartmentScope` 가 같은 호출).
- `[정적]` `NsiAllocateAndGetTable` 개수 묻기 → `max(개수/8, 10)` 여유 → 둘째, `0xea` 면 다시 (최대 3번).
- `[정적]` 어댑터 0 개 → `GetAdaptersAddresses` `0xe8`. `GetAdaptersInfo` 는 IfType `0x18` 건너뜀 → 루프백만이면 `0xe8`.
- `[정적]` nsi 열기 −1 + `GetLastError` 0 → 핸들 −1 로 IOCTL.
- `[정적]` `GetAddrInfoW` 길 NSI = dnsapi `Query_PrivateExW +0x60b` → `GetCurrentThreadCompartmentId` → `NsiGetParameter` 하나, 실패면 1.
- `[정적]` 모듈 ID 셋 (24 B 원본 · GUID).

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- 결과서 F1 표 9 단계는 `NotifyIpInterfaceChange` → `InternalRegisterChangeNotification` 만 적었다. `NotifyUnicastIpAddressChange` 는 `_calls.tsv` 로 `+0x19 NetioRegisterAddressChangeNotification` → `+0x9d InternalRegisterChangeNotification` — **한 단계 더** 거쳐 같은 함수로 간다.
- 결과서는 `InternalRegisterChangeNotification` 안의 `+0x9d` · `+0xd5 winnsi!NsiRpcRegisterChangeNotification` 을 적지 않았다 (`_calls.tsv` 에 있음). 등록 RPC 본문은 sast/127 몫 — 이 문서는 이름만 두고 → run129 예정.
- 결과서 "연결 핸들이 NULL 이면 6" — `.asm` `+0x66 TEST RAX,RAX / +0x69 JNZ / +0x6b LEA EAX,[RBP+0x6]` (RBP = 반환값 = 0) 으로 맞춤.
- `_calls.tsv` · `_map` 의 `alias:X+1` / "같은 주소 이름 1개 더" 표시 (winhttp 사슬 · webio `StartGlobalNetworkChangeMonitor` · `Startup` 등) — `pdb_aliases.py --lookup` 은 모두 "별칭 무리 아님". 더 있는 이름은 같은 함수의 C++ 장식 이름이라 PDB 이름을 그대로 썼다.

## 정정

- **run124** (`runs/03_sprint/run124-afd-socket.md`) — 정정 아님, **일부 닫힘**. 맨 위에 한 줄:
  - 9절 "`0x12001b` 의 `0x70` 구조체: `+0x10` 둘째 인자 포인터, `+0x18` 셋째 인자 … `+0x28 ~ +0x60` 은 (버퍼 포인터, 칸 크기) 네 쌍처럼 보인다 `[추측]`" → 칸 순서는 **`[정적]`** (`+0x10` 모듈 ID 포인터 · `+0x18` 객체 번호 · `+0x20` 첫 인자 · `+0x24` 둘째 인자 · `+0x28 ~ +0x60` 키 · RW · ROD · ROS (포인터, 1개당 크기) · `+0x68` 개수). `NsiAllocateAndGetTable` 에서는 둘째 인자 = 모듈 ID, 셋째 = 객체라 run124 글과 어긋나지 않는다. 칸 **이름** (키 · RW · ROD · ROS) 만 `[추측]`.
  - "PE_emulator 쪽" nsi 줄 "개수 0 이어도 최소 10칸 잡고 둘째 호출을 내는 것으로 읽힘 `[추측]`" → **`[정적]`** (`NsiAllocateAndGetTable +0x16c ~ +0x185` `max(개수/8, 10)`, 둘째 `+0x36b`).
  - "아직 확인 필요" 정적 5 — 칸 뜻 · 부르는 쪽 닫힘, 커널 `nsi.sys` 개수 0 대답은 남음 (이 문서 정적 3).

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 코드 | 지금 (결과서) | 해야 할 일 |
|---|---|---|
| `nt_create_file.cpp` | **틀림** — 이름 분류에 `\??\Nsi` 가 없어 `path_prefix_unsupported` (로그의 "기본 대답, 핸들 −1") | 이름 통째 (대소문자 무시) `\??\NSI` (nt 쪽 `\Device\Nsi`) 를 afd 곁에 nsi 로. EA 없음. 성공이면 **진짜 핸들** + **비동기 핸들** (`FILE_FLAG_OVERLAPPED` → 동기 옵션 없음 `[추측: kernelbase CreateFile 변환은 안 봄]`) |
| `nt_device_io_control_file.cpp` | **틀림 (몸 없음)** — 코드 셋이 `ioctl_overrides` 에 없고, 비동기 핸들이면 `device_async_handle_unsupported` (`:521`), 이벤트도 afd 만 | ① 장치 종류 nsi (afd 같은 이벤트 처리) ② 입력 길이 `0x68` / `0x50` / `0x70` 미만 → `0xC000000D` ③ 입력 = 출력 구조체를 읽어 **안의 사용자 포인터를 따라가 쓰기** ④ 동기 완료 (status 0) 면 기다림 불필요 ⑤ `0x12000f` 의 RW 크기 칸 `+0x40` 은 **건드리지 말 것** (줄이면 `0xc000000d`) |
| 대답 정책 (루프백만 · 진짜 망 안 나감) | — | 칸 뜻은 6 · 7절 — "칸 뜻을 아는 정책 값 + `TODO(VM)`". 가장 단순 = 9절 (a) 어댑터 0 개 (`{1,0}` · 개수 0 · 0 · 0). F1 (`NotifyIpInterfaceChange`) 은 NSI 장치로 못 고침 → winnsi RPC (run129) |
| `tests/file.cpp` | **없음** | (1) `\??\Nsi` 열기 → 핸들 ≠ −1 (2) `0x12000f` NDIS 객체 8 → RW `{1, 0}` (3) `0x12001b` 포인터 0 → 개수 `+0x68` (4) 크기 < `0x68` → `0xC000000D` |

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 7 + flow-writer 보탬 — 대기열 **Q222**. **랜선 뽑은 VM**, test.exe 가 아니라 winhttp · iphlpapi · dnsapi 를 쓰는 프로그램 — 고르기는 사용자 결정. 유저 멈춤점, 프로세스 조건. 호출마다 `dq <구조체> L0xd` + IOSB):

1. **`WinHttpSendRequest` 가 NSI 앞에서 끝나나 (F1)** — `winhttp!WinHttpSendRequest+0x25f` · `+0x2ff` `eax`, `winhttp!WINHTTP_DLL::GetWebIOApiHandle+0x44` (`WebInitialize` ret), webio `WapInitializeNetworkChange+0xa5` · `Startup+0xb4` / `+0xf1`, `iphlpapi!NotifyIpInterfaceChange` 반환, `winnsi!NsiConnectToServer` 반환.
2. **`GetAdaptersAddresses(0, 0x80f)` 의 NSI 넷 + 반환** — `iphlpapi!AllocateAndGetAdaptersAddresses+0xc8` / `+0x166` / `+0x1d0` / `+0x23d` `eax` + 구조체, `GetAdaptersAddresses+0x60`, `winhttp!GetActiveConnectionNameRAS+0x12a` · `+0x1ea` / `+0x2da`. 루프백 줄 하나의 칸 값 + 개수 (7절 칸마다).
3. `iphlpapi!GetBestInterfaceEx+0xeb` — `::` · `0.0.0.0` 일 때 `eax` (진짜 오류 번호) 와 키 `0x38` / `0x20` B.
4. 어댑터 0 개 (`0xe8`) 와 루프백 1 개일 때 각각 `WinHttpSendRequest` 최종 반환 · `GetLastError`.
5. `dnsapi!Query_PrivateExW+0x612` — `GetCurrentThreadCompartmentId` RW 4 B 와 `NsiGetParameter` 반환, `Rpc_ResolverQuery` ret (`+0x856` · `+0x912`) `eax`.
6. `iphlpapi!AllocateAndGetAdaptersInfo+0x8b` / `+0x158` — 루프백 1 개일 때 `+0xf7` 건너뜀을 지나는지.
7. `nsi!NsiGetAllParameters` `+0x139` 비교 통과 (RW 크기 칸 `+0x40` 이 8 로 남는지).
8. (flow-writer 보탬) `iphlpapi!InternalRegisterChangeNotification+0x63` `rax` (연결 핸들 — NULL 이면 F1 의 6) 와 `CheckProxySettings` 가 첫 보냄에서 `+0x37c` 를 타는지 (`+0x85` · `+0x91` 두 값). 1 과 같은 실행에서.

- run124 동적 7 (`0x12001b` 첫 호출 `0x70` · `+0x68`) 은 위 2 와 같은 몫.

정적 (결과서 "다음 요청 후보" + 보탬):

1. **winnsi · nsisvc (RPC 쪽)** — `NsiConnectToServer` 가 실제로 무엇을 하나 (끝점 이름, 바인딩만인가 호출인가). F1 "치명" 줄을 닫는다. → **sast/127 (Q223) 결과서 있음 → run129 예정.**
2. 이름 풀이 길 — webio `WaStartDnsQuery` → `WapDnsResolveGeneric` → `GetAddrInfoExW` → mswsock `SaBlob_Query` → `DnsQueryEx` → `Query_PrivateExW` → `Rpc_ResolverQuery`.
3. 커널 NSI (`nsi.sys`) 와 공급자 (`ndis.sys` · `tcpip.sys`) 가 객체 1 · 7 · 8 · 0 칸을 채우는 줄 — 루프백 줄 값이 정적으로 나오나, 개수 0 일 때 status (run124 정적 5 남은 몫).
4. `AddDhcpConfiguration` (dhcpcsvc RPC) · `AddNetbtConfiguration` (`\Device\NetBT_Tcpip_{GUID}`) — 어댑터 1 개 이상일 때 `GetAdaptersAddresses` 가 NSI 말고 요구하는 것.
5. `CheckProxySettings` 가 첫 `WinHttpSendRequest` 에서 `GetActiveConnectionName` 을 타는 조건 · 세션 가상 `SetProxySettings` 가 언제 불리나 (2절 5' `[공백]`).
6. (flow-writer 보탬, 낮음) kernelbase `CreateFileW` 가 `\\.\Nsi` 를 `\??\Nsi` 로 바꾸고 `FILE_FLAG_OVERLAPPED` 를 CreateOptions 로 옮기는 줄 — PE_emulator 표의 `[추측]` 닫기 (run124 9절 `[추측]` 과 같은 몫).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서: **지연 가져오기**는 `objdump -p` "DLL Name" 줄에 안 나온다 — `pefile` 로 지연 표까지 읽어야 nsi 를 부르는 모듈이 보인다 (기록만, 하네스 제안 아님).
- webio 처럼 내보내기가 번호뿐인 모듈은 Ghidra 가 `Ordinal_N` 으로 잡고 `pdb_aliases.py --where` 로도 안 나온다. `llvm-pdbutil dump --publics` 와 PE 내보내기 표를 맞춰 이름을 얻었다 (결과서).

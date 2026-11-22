# 126 nsi-callers — 정적 분석 결과

- 요청서: `_workspace/sast/126_nsi-callers.md` (Q221, PE_emulator 튜닝 N1 "WinHTTP·iphlpapi 가 NSI 에 무엇을 묻고 대답을 어떻게 읽나")
- 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/` 아래 바이너리.
- 날짜: 2026-11-21. analyze 는 서버 메모리 (46G 중 가용 27~31G) 를 보고 **한 번에 하나씩** (winhttp → webio → IPHLPAPI → ws2_32 → dnsapi), 각 30초 안팎. ntoskrnl (sast/125) 은 건드리지 않았다 (Ghidra 프로젝트가 모듈마다 따로라 잠금도 안 겹침).
- **이 결과서는 두 번에 걸쳐 쓴다.** 위쪽 (PDB·대조·가리기·한 줄 답) + **A 절** = `WinHttpSendRequest` 길 (먼저 쓴 것), **B 절** = 나머지 API 넷 (이어서 덧붙인 것). 맨 끝의 **4 절** = 모듈 ID 상수.

## 스크립트 문제

없음. 참고 둘 (고칠 것은 아님):
- webio 는 내보내기가 **번호뿐** (이름 없음) 이라 Ghidra 가 `Ordinal_N` 으로 잡는다. PDB 공개 심볼엔 `Web*` 이름이 있다 (`pdb_aliases.py --where` 로는 안 나옴 — 별칭 무리가 아니라서). 번호↔PDB 이름은 `llvm-pdbutil dump --publics` 와 PE 내보내기 표 (구역 주소 → RVA) 를 맞춰 얻었다: `Ordinal_5 = WebCloseSession`, `Ordinal_9 = WebCreateSession`, `Ordinal_17 = WebInitialize`, `Ordinal_25 = WebSendHttpRequest`, `Ordinal_45 = WebSetHeapExtensionInterfaces`. 문서에는 PDB 이름을 쓰고 Ghidra 이름을 한 번 밝힌다.
- 같은 이름 함수가 여럿인 winhttp (`SendRequest` 다섯, `Init` 넷, `GetWebIOApiHandle` 둘, `Startup` 둘) 는 `.asm` 파일 이름에 `__<rva>` 가 붙는다. 이 문서는 PDB 이름 (`?SendRequest@HTTP_USER_REQUEST@@…` 등) 으로 가리킨다.

## PDB GUID 확인 (새로 analyze 한 모듈마다)

`fetch_pdb.py` 가 바이너리의 CodeView 기록에서 GUID+Age 를 읽어 심볼 서버에서 받는다. 다섯 모듈 모두 Ghidra `SetPdb:` 줄이 있고 함수 이름이 PDB 이름으로 잡힌다 (`WinHttpSendRequest`, `AllocateAndGetAdaptersAddresses`, `WaMapInterfaceToAddressFamily`, `Query_PrivateExW`, `WSAGetCurrentThreadCompartmentId` 확인). `llvm-pdbutil dump --summary` 로 PDB 안의 GUID 가 바이너리의 것과 같음을 따로 확인했다.

| 모듈 | 바이너리 | PDB guid+age | 함수 수 (`_index.tsv`) | 로그 |
|---|---|---|---|---|
| winhttp | `winhttp.dll` 1,232,864 B | `0B998B55BD5357B779887C5DC5BADAAF1` | 3305 | `ms/ghidra/winhttp.analyze.log` |
| webio | `webio.dll` 881,648 B | `AB34B4686EFD631EC4E81A6E6B2618E91` | 2103 | `ms/ghidra/webio.analyze.log` |
| IPHLPAPI | `IPHLPAPI.DLL` 220,528 B | `23648DDA4C96DCCCE139DC2340F3030B1` | 642 | `ms/ghidra/iphlpapi.analyze.log` |
| ws2_32 | `ws2_32.dll` 534,592 B | `387233976BD422D139F79CC7418808251` | 1194 | `ms/ghidra/ws2_32.analyze.log` |
| dnsapi | `dnsapi.dll` 1,422,944 B | `50DD7074162A7A959E3E4D82C49E3CBE1` | 3405 | `ms/ghidra/dnsapi.analyze.log` |
| nsi (이미 있음, sast/121) | `nsi.dll` | `662A028F57A7C702B11F583CE15A8E631` | 42 | — |

- `ws2_32.dll.pdb` 는 이미 `ms/sym/` 에 있었다 (11-16, 이번에 받은 게 아님. GUID 같음, PDB 안 Age 3 — 심볼 서버 키는 바이너리의 Age 1 이라 문제 없음). 별칭 표 다섯은 `ms/sym/<이름>.aliases.tsv`.
- 커널 GUID (`C29EBFB0…`) 확인은 ntoskrnl 만 해당이라 이번엔 해당 없음.
- 공개 PDB 라 **구조체 타입·IOCTL 상수 이름이 없다.** 단, `NPI_MS_NDIS_MODULEID` · `NPI_MS_IPV4_MODULEID` · `NPI_MS_IPV6_MODULEID` 같은 **모듈 ID 상수 이름은 IPHLPAPI·winhttp PDB 에 있다** (아래 4 절).

## 버전·스크립트 대조

"알려진 사실" 표에는 이 모듈들이 없다. 대신 `.asm` 의 `+0x` 를 **원본 바이트** 와 직접 맞췄다 (스크립트 오프셋 계산 확인):
- `AllocateAndGetAdaptersAddresses +0xc1` = `48 ff 15 …` (7바이트 `CALL [rip+…]`, ret `+0xc8`), `+0x15f` 도 같은 꼴 — 일치.
- `GetAdaptersAddresses +0x5b` = `e8 …` (5바이트 CALL, ret `+0x60`) — 일치.
- `WinHttpSendRequest +0x25a` = `e8 d5 ef ff ff` (ret `+0x25f`), `+0x2fa` = `e8 01 c0 00 00` (ret `+0x2ff`) — 일치.
- `GetActiveConnectionNameRAS +0x123` = `48 ff 15 …` (GetAdaptersAddresses 가져오기 칸), `WINHTTP_DLL::GetWebIOApiHandle +0x3d` = `48 ff 15 …` (WebInitialize), webio `Startup +0xad` (RVA `0x4ac95`) = `48 ff 15 …` (NotifyIpInterfaceChange), `WaMapInterfaceToAddressFamily +0x94` — 일치.
- nsi: `NsiGetAllParameters +0xf8` = `c7 44 24 28 0f 00 12 00` (0x12000f), `NsiGetParameter +0xd3` = `… 07 00 12 00` (0x120007), `NsiAllocateAndGetTable +0x10c` = `… 1b 00 12 00` (0x12001b) — 일치.
- 보조 증거 (로그 표와 코드가 맞음): 요청서 표의 "API 별 **처음** 부르는 NSI 코드" 가 코드와 같다 — `GetAdaptersAddresses` 의 첫 NSI = `NsiGetAllParameters` (0x12000f), `GetAdaptersInfo` 의 첫 NSI = `NsiAllocateAndGetTable` (0x12001b), `GetAddrInfoW` 길의 첫 NSI = `GetCurrentThreadCompartmentId` 의 `NsiGetParameter` (0x120007).
- **판정: 일치.** "스크립트 문제" 없음.

## 부르는 모듈 가리기 (import 표 — 가장 가벼운 쪽)

`pefile` 로 가져오기 표 (일반 + 지연) 를 읽었다. 네 모듈 모두 NSI 는 **지연 가져오기 (`NSI.dll`)** 로만 쓴다 (일반 가져오기 표엔 없음 → `objdump -p` 의 "DLL Name" 줄만 보면 놓친다).

| 모듈 | nsi.dll 에서 가져오는 함수 | 그 밖 | 이번에 analyze? |
|---|---|---|---|
| winhttp | `NsiAllocateAndGetTable`, `NsiFreeTable` (둘뿐) | 지연: `IPHLPAPI` (`GetAdaptersAddresses`, `GetBestInterfaceEx`, `GetIfEntry2Ex`, `GetIpNetEntry2`, `GetIpForwardTable2`, `ResolveIpNetEntry2`, `ConvertInterfaceGuidToLuid`, `NotifyIpInterfaceChange`, `NotifyUnicastIpAddressChange`, `CancelMibChangeNotify2`, `FreeMibTable`), `webio` (번호 38개), `WS2_32` (`GetAddrInfoExW` 등), `DNSAPI` (`DnsConnection*`) | **예** — `WinHttpSendRequest` 가 여기서 시작 |
| webio | **없음** | 지연: `IPHLPAPI` (`GetIpInterfaceEntry`, `NotifyIpInterfaceChange`, `NotifyUnicastIpAddressChange`, `CancelMibChangeNotify2`, `Get/SetCurrentThreadCompartmentId`), `WS2_32` (`GetAddrInfoExW` 등) | **예** — `WinHttpSendRequest` 가 `WebInitialize` 로 들어감 |
| IPHLPAPI | `NsiGetParameter`, `NsiGetAllParameters`, `NsiEnumerateObjectsAllParameters`, `NsiAllocateAndGetTable`, `NsiFreeTable`, `NsiSetParameter`, `NsiSetAllParameters`, `NsiSetAllPersistentParametersWithMask`, `NsiRequestChangeNotification`, `NsiCancelChangeNotification` (+ `WINNSI.DLL` 의 `NsiRpc*`·`NsiConnectToServer`) | | **예** — NSI 를 실제로 묻는 쪽 |
| dnsapi | `NsiGetParameter`, `NsiGetAllParameters`, `NsiAllocateAndGetTable`, `NsiFreeTable` | 지연: `IPHLPAPI` (`GetCurrentThreadCompartmentId` 포함) | **예** — `GetAddrInfoW` 길 (B3) |
| ws2_32 (요청서 밖 — import 표 전수 조사에서 나옴) | `NsiGetParameter`, `NsiGetAllParameters`, `NsiSetAllParameters` | | **예, 배제용** — `GetAddrInfoW` 가 ws2_32 안에서 NSI 를 부르는지 가리려고 (결과: **안 부름**, 아래 B3) |

- 전수 조사 (`System32` 의 4,346 파일 중 `NSI.dll` 을 가져오는 모듈) 에 `webio` 는 없고, `ws2_32` · `winhttp` · `dnsapi` · `IPHLPAPI` 가 요청서 질문과 닿는다. 이 밖에 ipconfig·w32tm·netprofm·ncsi·dhcpcore 등 50여 개가 더 있으나 질문 밖이라 안 봤다.

---

## 먼저 읽는 한 줄 답

1. **`WinHttpSendRequest` 에서 `NsiGetAllParameters` (0x12000f) 까지는 winhttp 스스로가 아니라 IPHLPAPI 를 거친다.** winhttp 은 `NsiGetAllParameters` 를 가져오지도 않는다. 사슬: `winhttp!WinHttpSendRequest +0x2fa` → `HTTP_USER_REQUEST::SendRequest +0x294` → `_CreateProxyResolver +0x2a` → `INTERNET_HANDLE_BASE::SafeGetProxyResolver +0xbe` → `INTERNET_SESSION_HANDLE_OBJECT::CheckProxySettings +0x37c` → `GetActiveConnectionName +0x77` → `GetActiveConnectionNameRAS +0x123` → `iphlpapi!GetAdaptersAddresses(0, 0x80f, …)` → `AllocateAndGetAdaptersAddresses +0xc1` → `nsi!NsiGetAllParameters`. webio 는 이 갈래에 안 들어간다.
2. 그 호출의 인자 = **`NsiGetAllParameters(1, NPI_MS_NDIS_MODULEID, 8, 키 없음, RW 8바이트)`** — 스레드의 compartment 번호·범위 (`{id, scope}`) 를 묻는다. 이어서 `NsiAllocateAndGetTable` 셋 (NDIS 객체 1 = 어댑터 표 / IPv4·IPv6 객체 7 = IP 인터페이스 표).
3. **이 길은 실패를 삼킨다.** NSI 가 오류를 주면 `GetAdaptersAddresses` 가 오류 코드를 돌려주고, winhttp 의 `GetActiveConnectionName` 은 NULL (연결 이름 없음) 을 돌려줄 뿐 `WinHttpSendRequest` 는 이어 간다 (호출하는 세 자리 모두 확인, A5). **그래서 "NSI 에서 꼭 받아야 하는 최소 대답" 은 이 길엔 없다.** 장치가 열리고 IOCTL 이 아무 오류나 돌려줘도 된다.
4. **그런데 `WinHttpSendRequest` 가 0 을 돌려줄 수 있는 NSI 쪽 줄이 따로 하나 있다 (치명, 이름 풀이 전).** `+0x25a` `PrepareWebIOSessionForRequest` → `CreateWebIOSession` → `WINHTTP_DLL::GetWebIOApiHandle` → `webio!WebInitialize` → … → `NotifyIpInterfaceChange` / `NotifyUnicastIpAddressChange` (IPHLPAPI). 이 둘이 오류를 주면 `WebInitialize` 가 실패하고 `WinHttpSendRequest` 가 0 이다. **이 둘은 `\Device\Nsi` IOCTL 이 아니라 RPC (winnsi.dll → nsisvc 서비스, ncalrpc)** 다. 이 줄 (`+0x25a`) 은 프록시 이름 줄 (`+0x2fa`) 보다 **앞** 이라, 로그에 0x12000f 가 찍힌 호출에선 이 줄은 이미 통과한 것이다 (`WebInitialize` 는 한 번만 돌고 핸들을 전역에 둠 — 앞선 `WinHttpOpen` 길 `WinHttpOpen +0x147 → WinHttpSetOptionInternal → CreateStaticResolverCacheEntry → CreateWebIOSession` 에서 이미 돌았을 수도 있음). 즉 **"0x12000f 가 보인 그 호출" 의 0 반환 원인은 정적으론 못 찾았다** (NSI IOCTL 실패는 삼켜짐) — 이 줄은 **0x12000f 가 안 보이고 일찍 0 이 되는 샘플** 의 후보다. 원인 확인은 동적 (아래 1·2).
5. **코드 셋·구조체**: `0x12000f` → 0x68 바이트, `0x120007` → 0x50, `0x12001b` → 0x70. 칸 표 셋은 A3. 세 구조체 모두 nsi.dll 이 **같은 칸 순서** 로 채운다 (+0x10 모듈 ID 포인터 · +0x18 객체 번호 · +0x20 첫 인자 · +0x28 키 포인터 · 그다음 (포인터, 크기) 쌍).
6. **모듈 ID 는 `.rdata` 상수, PDB 에 이름 있음**: NDIS `eb004a11-9b1a-11d4-9123-0050047759bc`, IPv6 `eb004a01-…`, IPv4 `eb004a00-…` (4 절).
7. **빈 표**: 어댑터 0개 PC 는 `0x12001b` 가 개수 0 (status 0) 을 주면 되고, 그러면 `GetAdaptersAddresses` 가 `ERROR_NO_DATA` (0xe8) 를 돌려준다 (`GetAdaptersInfo` 도 0xe8). winhttp 은 그걸 "연결 이름 없음" 으로만 쓴다.
8. **지금 우리 (PE_emulator)**: **틀림 (막혀 있음)** — 이름 분류·비동기 핸들·이벤트·코드 셋 몸이 모두 없다. 자세한 줄은 A7.

---

## A. `WinHttpSendRequest` 길 (먼저 쓴 것)

### A1. 누가 NSI 를 부르나 — 부름 사슬 (세 갈래)

표기: `함수 +호출위치 (ret=+돌아올 자리)`. 근거는 모두 `[정적] ms/disassemble/<모듈>/<함수>.asm` 줄. WinDbg 콜스택의 `caller+0x…` 는 ret 값.

**갈래 F2 — 프록시 이름 길 (NSI IOCTL 을 부름, 실패 삼킴)**

| 단계 | 함수 (PDB 이름) | 호출 줄 | 근거 `.asm` | 인자·조건 |
|---|---|---|---|---|
| 1 | `winhttp!WinHttpSendRequest` (rva 0x8580) | `+0x2fa` (ret +0x2ff) → `HTTP_USER_REQUEST::SendRequest` (rva 0x14880) | `winhttp/WinHttpSendRequest.asm` | |
| 2 | `HTTP_USER_REQUEST::SendRequest` | `+0x294` (ret +0x299) → `_CreateProxyResolver` | `winhttp/SendRequest__14880.asm` | `[this+0xcc]==0` 일 때만 (첫 보냄) |
| 3 | `HTTP_USER_REQUEST::_CreateProxyResolver` (rva 0x15c34) | `+0x2a` (인자 1) / `+0x55` (인자 0) → `INTERNET_HANDLE_BASE::SafeGetProxyResolver` | `winhttp/_CreateProxyResolver.asm` | 세션 쪽 `[+0x90]≠0` 이면 먼저 인자 1 로 (결과가 `0x80070490` 이면 인자 0 으로 다시), 아니면 바로 인자 0 |
| 4 | `SafeGetProxyResolver` (rva 0x15cb8) | `+0xbe` (ret +0xc3) → `INTERNET_SESSION_HANDLE_OBJECT::CheckProxySettings` | `winhttp/SafeGetProxyResolver.asm` | 루트 세션 핸들까지 올라가서 |
| 5 | `CheckProxySettings` (rva 0x3b6ec) | `+0x37c` (ret +0x381) → `GetActiveConnectionName` | `winhttp/CheckProxySettings.asm` | **조건**: `g_ullNetworkGeneration` (`+0x85`/`+0xa4` 에서 읽음) ≠ 세션이 마지막에 본 값 (`[this+0x228]`, `+0x91`/`+0xab`). 둘 다 시작값 0 (`.bss` / 생성자 `INTERNET_SESSION_HANDLE_OBJECT +0x1d4`) 이고 `WinHttpNetworkChangeNotification` (`INC.LOCK`) 만 올린다 → **네트워크 바뀜 알림이 없으면 이 줄은 안 탄다** |
| 5' | `LoadAutomaticProxyResolvers` (rva 0x5c8a4) | `+0x224` (ret +0x229) → `GetActiveConnectionName` | `winhttp/LoadAutomaticProxyResolvers.asm` | 부르는 쪽 = 세션의 가상 `SetProxySettings` (vtable +0x10, `refs 0x5dba0`) 하나뿐 — **언제 불리는지는 못 닫음** `[공백]` |
| 6 | `GetActiveConnectionName` (rva 0x5238c) | `+0x77` (ret +0x7c) → `GetActiveConnectionNameRAS` | `winhttp/GetActiveConnectionName.asm` | |
| 7 | `GetActiveConnectionNameRAS` (rva 0x525a4) | `+0x123` (ret +0x12a) → `iphlpapi!GetAdaptersAddresses(AF_UNSPEC=0, 플래그 0x80f, NULL, 버퍼 0x7800, &크기)` | `winhttp/GetActiveConnectionNameRAS.asm` | `0x6f` (버퍼 모자람) 이면 크기를 `+0x800` 키워 **최대 3번**. 그 밖 오류는 바로 포기 |
| 7' | 같은 함수 | `+0x1e3`, `+0x2d3` (ret +0x1ea, +0x2da) → `iphlpapi!GetBestInterfaceEx` | 〃 | 앞: `sockaddr_in6` (family 0x17, 주소 0), 뒤: `sockaddr_in` (family 2, 주소 0) — **IPv6 가 실패했을 때만 IPv4**. 둘 다 `GetAdaptersAddresses` 가 **성공했을 때만**. 둘 다 실패면 이름 못 얻고 `0x80070490` |
| 8 | `iphlpapi!GetAdaptersAddresses` (rva 0x1dc0) | `+0x5b` (ret +0x60) → `AllocateAndGetAdaptersAddresses` | `iphlpapi/GetAdaptersAddresses.asm` | 계열 (첫 인자) 은 0·2·23 만 (아니면 0x57). 플래그 0x80f = SKIP_UNICAST(1) · SKIP_ANYCAST(2) · SKIP_MULTICAST(4) · SKIP_DNS_SERVER(8) · 0x800 (DNS 정보 건너뜀) `[추측: SDK 상수 이름]` |
| 9 | `AllocateAndGetAdaptersAddresses` (rva 0x2990) | `+0xc1` (ret +0xc8) → **`NsiGetAllParameters`** · `+0x15f`/`+0x1c9`/`+0x236` → `NsiAllocateAndGetTable` ×3 | `iphlpapi/AllocateAndGetAdaptersAddresses.asm` | 인자는 A2 |
| 10 | `nsi!NsiGetAllParameters` (rva 0x2150) | `+0x105` (ret +0x10c) → `NtDeviceIoControlFile(…, 0x12000f, 입력=출력 0x68)` | `nsi/NsiGetAllParameters.asm` | 칸은 A3 |

- 플래그 0x80f 이므로 단계 9 에서 **`AddUnicastAddresses`·`AddAnycastAddresses`·`AddMulticastAddresses`·`AddDnsConfiguration` 은 건너뛴다** (`AllocateAndGetAdaptersAddresses +0xb2d` 부근의 플래그 검사 — `(플래그>>0xb)&1` 이면 DNS 건너뜀, `플래그&1/2/4` 이면 해당 `Add*` 건너뜀). 대신 **`AddDhcpConfiguration` (`+0xb2d`, ret +0xb32) 과 `AddNetbtConfiguration` (`+0xb64`, ret +0xb69) 는 어댑터가 있으면 항상** 부르고, 둘이 0 이 아닌 값을 돌려주면 `GetAdaptersAddresses` 가 통째로 실패한다 (`+0xb34 TEST EAX,EAX / JNZ`, `+0xb6b`). 둘은 NSI 가 아니라 **dhcpcsvc RPC (`DhcpEnumInterfaces`)** 와 **`NtCreateFile` + `NtDeviceIoControlFile` (NetBT 장치)** 다 — 이 문서 범위 밖 (이름만).

**갈래 F1 — 웹 I/O 시작 길 (NSI IOCTL 아님, 치명적일 수 있음)**

| 단계 | 함수 | 호출 줄 | 근거 `.asm` |
|---|---|---|---|
| 1 | `winhttp!WinHttpSendRequest` | `+0x25a` (ret +0x25f) → `INTERNET_SESSION_HANDLE_OBJECT::PrepareWebIOSessionForRequest` | `winhttp/WinHttpSendRequest.asm` |
| 2 | `PrepareWebIOSessionForRequest` (rva 0x77b4) | `+0x46` (ret +0x4b) → `CreateWebIOSession` | `winhttp/PrepareWebIOSessionForRequest.asm` |
| 3 | `CreateWebIOSession` (rva 0x7890) | `+0x47` (ret +0x4c) → `WINHTTP_GLOBALS::GetWebIOApiHandle` (rva 0x79f4) | `winhttp/CreateWebIOSession.asm` |
| 4 | `WINHTTP_GLOBALS::GetWebIOApiHandle` | `+0x4e` (ret +0x53) → `WINHTTP_DLL::GetWebIOApiHandle` (rva 0x80f4) | `winhttp/GetWebIOApiHandle.asm` |
| 5 | `WINHTTP_DLL::GetWebIOApiHandle` | `+0x3d` (ret +0x44) → **`webio!WebInitialize`** (Ghidra 이름 `Ordinal_17`, 인자 `(0x1000000010000, 0, &핸들)`) | `winhttp/GetWebIOApiHandle__80f4.asm` — 핸들이 이미 있으면 (`[전역+0x28]≠0`) 이 호출을 건너뜀 |
| 6 | `WebInitialize` | `+0x187` (ret +0x18c) → `WapInitialize` → `+0x37` `WaIncrementDllInitializationCount` → `+0x56` `WapInitializeDll` → `+0x17` `WaInitializeUtil` → `+0xfa` `WapInitializeNetworkChange` | `webio/` (지도 `_map/Ordinal_17_edges.tsv`, `WaInitializeUtil.asm`, `WapInitializeNetworkChange.asm`) |
| 7 | `WapInitializeNetworkChange` (rva 0x4a748) | `+0xa0` (ret +0xa5) → `WxRegisterForNetworkChangeNotification` → `+0x23` `StartGlobalNetworkChangeMonitor` → `+0x89` `Startup` | `webio/WapInitializeNetworkChange.asm` |
| 8 | webio `Startup` (rva 0x4abe8 근처) | `+0xad` (ret +0xb4) → **`iphlpapi!NotifyIpInterfaceChange`**(Family 0, 콜백, ctx, `InitialNotification`=FALSE, &핸들), `+0xea` (ret +0xf1) → **`NotifyUnicastIpAddressChange`** | `webio/_refs/0xc1080.tsv`, `_refs/0xc10a0.tsv` (winhttp 도 같은 꼴의 자기 `Startup` 이 있음: `winhttp/Startup__5c148.asm +0xad`·`+0xea`) |
| 9 | `iphlpapi!NotifyIpInterfaceChange` (rva 0x9880) | → `InternalRegisterChangeNotification` (rva 0x9758) → `NsiNotificationConnectToServer` (rva 0x1b18c) → **`winnsi!NsiConnectToServer`** (RPC, `ncalrpc`) | `iphlpapi/NotifyIpInterfaceChange.asm`, `InternalRegisterChangeNotification.asm`, `NsiNotificationConnectToServer.asm` |

- **실패가 어디까지 올라가나** `[정적]`: 연결 핸들이 NULL 이면 `InternalRegisterChangeNotification` 이 6 (ERROR_INVALID_HANDLE) 을 돌려주고 `NotifyIpInterfaceChange` 가 그대로 돌려준다 → webio `Startup` 이 오류 (`+0xbc JNZ`) → `WapInitializeNetworkChange` 가 `WX_WIN32_FROM_HR(…)` 값을 돌려줌 (`+0xac MOV EBX,EAX`, 끝 `+0x156 MOV EAX,EBX`) → `WaInitializeUtil` 이 `iVar3 != 0` 이면 초기화 실패 → `WebInitialize` 실패 → `GetWebIOApiHandle` 이 0 아닌 값 → `CreateWebIOSession` → `PrepareWebIOSessionForRequest` (`+0x4b` 뒤 `TEST`) → `WinHttpSendRequest` 가 `dwErrCode != 0` 으로 `SetLastError` 후 0 (`WinHttpSendRequest.c` 의 `PrepareWebIOSessionForRequest` 반환 검사).
- `NsiConnectToServer` 가 실제로 서버를 만나는지 (`RpcBindingFromStringBindingW` 는 연결 안 하고 바인딩 핸들만 만든다) 는 winnsi 를 안 읽어서 `[공백]`.

**갈래 F3 — 이름 풀이 길 (참고, 처음엔 안 탐)**

- webio `WapTcpStartDnsQuery` (rva 0x3a4e0) → `WaStartDnsQuery` → … → `WapDnsResolveGeneric +0x1ef` → `ws2_32!GetAddrInfoExW` (`webio/_refs/0xc11e8.tsv`; `WaStartDnsQuery`→`WapDnsResolveGeneric` 사이는 안 봄 `[공백]`). 거기서 NSI 는 B3 의 `GetCurrentThreadCompartmentId` (0x120007) 하나.
- **`WaMapInterfaceToAddressFamily` (webio rva 0x396ec) → `GetIpInterfaceEntry`** 는 **기본 설정에선 안 탄다**: 부르는 `WapTcpStartDnsQuery +0x28f` 앞에서 `[연결+0x165]≠0` (인터페이스 지정) · `[연결+0x3d8]==0` (계열 미지정) · `[연결+0x140]≠0` (인터페이스 번호) 세 조건이 모두 맞아야 한다 (`+0x265 ~ +0x286`). 오류가 나도 로그만 남기고 이어 감 (`+0x294 TEST EAX,EAX / JNZ +0x2a1`). 이 함수는 `0x490` (NOT_FOUND) · `0x32` (NOT_SUPPORTED) 를 "그 계열엔 없음" 으로 읽는다 (`+0xe2 ~ +0xfe`).

### A2. `GetAdaptersAddresses(0, 0x80f)` 안에서 NSI 에 넣는 인자

`AllocateAndGetAdaptersAddresses` 의 호출 넷 (`.asm` 줄 = 인자 설정 줄). 인자 순서 `(첫 인자, 모듈 ID, 객체 번호, 키 포인터, 키 크기, RW 포인터, RW 크기, ROD 포인터, ROD 크기, ROS 포인터, ROS 크기, [개수 포인터, 마지막 불])` — 순서는 nsi.dll 이 구조체로 옮기는 줄에서 확인 (A3).

| 호출 | 줄 | 첫 인자 | 모듈 ID | 객체 | 키 | RW | ROD | ROS | 개수 |
|---|---|---|---|---|---|---|---|---|---|
| `NsiGetAllParameters` | `+0xc1` (인자 `+0x3e ~ +0xbd`) | 1 | `NPI_MS_NDIS_MODULEID` | **8** | 없음 | **8 B** (받는 곳 `RBP-0x70`) | 0 | 0 | — |
| `NsiAllocateAndGetTable` #1 | `+0x15f` (인자 `+0x11a ~ +0x157`) | 1 | NDIS | **1** | 8 B (어댑터 LUID) | 0 | **0x290 B/개** | **0x238 B/개** | `RBP-0x58` |
| `NsiAllocateAndGetTable` #2 | `+0x1c9` (인자 `+0x17a ~ +0x1c1`) | 1 | `NPI_MS_IPV4_MODULEID` | **7** | 8 B (LUID) | **0xf0 B/개** | 0 | **0x20 B/개** | `RBP-0x54` |
| `NsiAllocateAndGetTable` #3 | `+0x236` (인자 `+0x1df ~ +0x232`) | 1 | `NPI_MS_IPV6_MODULEID` | **7** | 8 B | 0xf0 | 0 | 0x20 | `RBP-0x50` |

- `GetAdaptersAddresses` 의 NSI 부름은 이게 전부 (플래그 0x80f). `+0xd14` / `+0xf3e` 의 `NsiSetParameter` 는 플래그 0x200 (모든 compartment) 일 때만이라 안 탄다.
- #2·#3 은 반환값 `0` 또는 **`0x32` (ERROR_NOT_SUPPORTED)** 를 받아들인다 (`+0x1d9 JNZ` → `+0xfe7 CMP EAX,0x32`; 디컴파일 `if ((iVar12 == 0) || (iVar12 == 0x32))`). #1 은 0 만 (`+0x16f JNZ +0x5ed`).
- **`NsiGetAllParameters` 의 RW 8바이트 뜻**: `{ +0: compartment 번호, +4: compartment 범위 }`. 근거 = 같은 모듈의 `GetCurrentThreadCompartmentScope` (rva 0x104a0) 가 같은 호출 `NsiGetAllParameters(1, NDIS, 8, 0,0, &8B, 8, 0,0,0,0)` 로 읽어 `*범위 = [+4]`, `*번호 = [+0]` 로 내놓는다 (`iphlpapi/GetCurrentThreadCompartmentScope.asm`). 실패 땐 `{번호 1, 범위 0}` 으로 돌려준다 (같은 함수). `AllocateAndGetAdaptersAddresses` 는 `[+0]` 을 어댑터 거르기에 쓴다: `+0xa26 CMP dword [ROD(i) + 0], R13D` (R13D = 이 번호). **플래그 0x200 이 없으면 ROD 의 첫 dword (CompartmentId) 가 이 번호와 같은 어댑터만 남는다.**
- `NsiAllocateAndGetTable` 의 반복 (nsi.dll): 첫 호출은 **포인터를 모두 0 으로** 개수만 묻고 (`+0x119`), 개수 + `max(개수/8, 10)` 칸을 잡아 (`+0x16c ~ +0x185`) 둘째 호출 (`+0x36b`). **둘째 호출이 status 0 이면 끝** (`+0x39f`), 오류를 `RtlNtStatusToDosError` 로 바꿔 **`0xea` (ERROR_MORE_DATA, STATUS_BUFFER_OVERFLOW 0x80000005 에서 옴) 면 더 큰 버퍼로 다시** (`+0x3b8 CMP R12D,0xea / JZ +0x18000176e`), 최대 3번 (`+0x48 CMP ECX,3`). 첫 호출의 `0xea` 도 받아들인다 (`+0x4ae CMP EBX,0xea`). → **빈 표 = 첫 호출 status 0 · 개수 0, 둘째 호출 status 0 · 개수 0.**

### A3. 구조체 칸 표 셋 (nsi.dll → nsiproxy.sys 로 가는 입력 = 출력 같은 버퍼)

공통: 모두 **METHOD_NEITHER** (코드 하위 2비트 3), **입력 버퍼 = 출력 버퍼 = 같은 구조체**, 구조체 안의 포인터 (키·RW·ROD·ROS) 는 **사용자 주소** 라 커널이 그 주소를 따라가서 읽고 쓴다. IOSB 는 별도 16바이트 (스택). `nsi.dll` 은 매번 `CreateEventA(NULL, FALSE, FALSE, NULL)` 를 만들어 이벤트로 넘기고, 반환이 `0x103` (STATUS_PENDING) 이면 `NtWaitForSingleObject` 뒤 `IOSB.Status` 를 쓴다.

**① `0x12000f` — `NsiGetAllParameters` / `…Ex` — 0x68 바이트** (입력 길이 = 출력 길이 = 0x68, `NsiGetAllParameters +0xcd/+0xe0`, `NsiGetAllParametersEx +0x52/+0x65`). 비-Ex 가 인자에서 옮기는 줄 `NsiGetAllParameters +0x20 ~ +0x86` (구조체 = `RBP-0x51`). Ex 는 호출자가 이 구조체를 직접 채워 넘긴다 (`NsiGetAllParametersEx +0x22` 에서 인자 RCX 가 그대로 입력·출력).

| 오프셋 | 크기 | 뜻 | 넣는 줄 (`NsiGetAllParameters`) |
|---|---|---|---|
| +0x00 | 8 | 예약 (0) | `+0x70` |
| +0x08 | 8 | 예약 (0) — nsiproxy 는 이 주소 (`구조체+8`) 를 검사 함수에 넘겨 `[+8 + 8]` 로 모듈 ID 포인터를 읽는다 (`NsippGetAllParameters +0x1c7`, `NsippProbeAndAllocateParameters +0xd0`) | `+0x70` |
| **+0x10** | 8 | **모듈 ID 포인터** (`NPI_MODULEID` 0x18 바이트) | `+0x74` (인자 2 = RDX) |
| **+0x18** | 4 | **객체 번호** | `+0x78` (인자 3 = R8D) |
| +0x1c | 4 | 0 | `+0x62` |
| **+0x20** | 4 | **첫 인자** (모든 호출자가 1 을 넘김 — "저장소" 로 짐작 `[추측]`) | `+0x80` (인자 1 = ECX) |
| +0x24 | 4 | 0 | `+0x83` |
| **+0x28** | 8 | **키 포인터** (없으면 NULL) | `+0x35` (인자 4 = R9) |
| +0x30 | 4 | 키 크기 | `+0x3d` (인자 5) |
| +0x34 | 4 | 0 | |
| **+0x38** | 8 | **RW 포인터** (읽고쓰기 설정) | `+0x44` (인자 6) |
| +0x40 | 4 | RW 크기 | `+0x7c` (인자 7) |
| +0x44 | 4 | 0 | |
| **+0x48** | 8 | **ROD 포인터** (읽기 전용 동적 = 상태) | `+0x50` (인자 8) |
| +0x50 | 4 | ROD 크기 | `+0x5b` (인자 9) |
| +0x54 | 4 | 0 | |
| **+0x58** | 8 | **ROS 포인터** (읽기 전용 고정) | `+0x66` (인자 10) |
| +0x60 | 4 | ROS 크기 | `+0x6d` (인자 11) |
| +0x64 | 4 | 0 (끝 = 0x68) | |

- 끝난 뒤 (비-Ex 만): `+0x139 CMP [구조체+0x40], R14D / JC` — **커널이 돌려준 RW 크기 (`+0x40`) 가 요청한 크기보다 작으면 `0xc000000d` 를 오류로 돌려준다** (`+0x22d MOV EAX,0xc000000d`). 에뮬레이터가 `+0x40` 을 안 건드리면 통과.
- nsiproxy: 입력 길이 `< 0x68` 이면 `0xC000000D` (`NsippGetAllParameters +0x1ae CMP EBX,0x68 / JC → +0x466`). 32비트 프로세스는 0x38 짜리 (`IoIs32bitProcess`, `+0xca ~ +0x1a7`) — 우리 대상 아님. 성공하면 `Information = 0x68` (`+0x330 MOV [R14],0x68`). 공급자가 돌려준 크기가 요청과 **다르면** 사용자 구조체의 크기 칸을 그 값으로 덮어쓴다 (`+0x3b4 ~ +0x417`: ROD → `+0x50`, ROS → `+0x60`, RW → `+0x40`) — 오류는 아님.

**② `0x120007` — `NsiGetParameter` / `…Ex` — 0x50 바이트** (입력 길이 0x50. **출력 길이 = 데이터 크기** (구조체 `+0x48`), 출력 버퍼 = 구조체 시작 (`NsiGetParameter +0xac MOV [RSP+0x48],EDI`; Ex: `+0x59 MOV [RSP+0x48],EBX`, EBX = 구조체 `+0x48`) — 실제 값은 **구조체 안 데이터 포인터** 가 가리키는 칸에 써진다.) 비-Ex 가 옮기는 줄 `NsiGetParameter +0x22 ~ +0x62` (구조체 = `RBP-0x21`).

| 오프셋 | 크기 | 뜻 | 넣는 줄 (`NsiGetParameter`) |
|---|---|---|---|
| +0x00 | 16 | 예약 (0) | `+0x3f`, `+0x43` |
| **+0x10** | 8 | **모듈 ID 포인터** | `+0x50` (인자 2) |
| **+0x18** | 4 | **객체 번호** | `+0x54` (인자 3) |
| +0x1c | 4 | 0 | `+0x47` |
| **+0x20** | 4 | **첫 인자** (모든 호출자 1) | `+0x5c` (인자 1) |
| +0x24 | 4 | 0 | `+0x5f` |
| **+0x28** | 8 | **키 포인터** | `+0x58` (인자 4) |
| +0x30 | 4 | 키 크기 | `+0x22` (인자 5, `[RBP+0xf]`) |
| +0x34 | 4 | 0 | `+0x4a` |
| **+0x38** | 4 | **어느 칸 묶음인가**: **0 = RW, 1 = ROD** (`GetCurrentThreadCompartmentId` 가 0, `GetBestInterfaceEx` 가 1 을 넘김 → 이름은 `[추측]`, 2 = ROS 일 것) | 인자 6 (`+0x25`·`+0x28`) |
| +0x3c | 4 | 0 | `+0x4d` |
| **+0x40** | 8 | **결과를 받을 칸 포인터** | 인자 7 (`+0x2b`·`+0x2f`) |
| **+0x48** | 4 | **결과 크기** (바이트) — Ex 는 끝난 뒤 `IOSB.Information` 을 여기에 되돌려 씀 (`NsiGetParameterEx +0xb5 ~ +0xb9`) | 인자 8 (`+0x33`·`+0x36`) |
| **+0x4c** | 4 | **그 묶음 안의 시작 오프셋** | 인자 9 (`+0x39`·`+0x3c`) |

**③ `0x12001b` — `NsiEnumerateObjectsAllParameters` / `…Ex` / `NsiAllocateAndGetTable` — 0x70 바이트** (입력 길이 = 출력 길이 = 0x70, `NsiEnumerateObjectsAllParameters +0xc6/+0xd9`; `NsiAllocateAndGetTable +0xe1/+0xf4`). 비-Ex 가 옮기는 줄 `NsiEnumerateObjectsAllParameters +0x12 ~ +0x7d` (구조체 = `RBP-0x61`). `NsiAllocateAndGetTable` 은 같은 틀을 `+0x5c ~ +0x9a` 에서 채운다 (구조체 = `RBP-0x20`, sast/121 3 절).

| 오프셋 | 크기 | 뜻 | 넣는 줄 (`NsiEnumerateObjectsAllParameters`) |
|---|---|---|---|
| +0x00 | 16 | 예약 (0) | `+0x6e` |
| **+0x10** | 8 | **모듈 ID 포인터** | `+0x72` (인자 3 = R8) |
| **+0x18** | 4 | **객체 번호** | `+0x76` (인자 4 = R9D) |
| **+0x20** | 4 | **첫 인자** (표 함수는 항상 호출자의 첫 인자 = 1) | `+0x7a` (인자 1 = ECX) |
| **+0x24** | 4 | **둘째 인자 — `NsiAllocateAndGetTable` 은 항상 1** (`+0x93`) | `+0x7d` (인자 2 = EDX) |
| **+0x28** | 8 | **키 버퍼 포인터** (개수 묻기 땐 0) | `+0x21` (인자 5) |
| +0x30 | 4 | 키 칸 크기 (1개당 바이트) | `+0x2c` (인자 6) |
| **+0x38** | 8 | **RW 버퍼 포인터** | `+0x33` (인자 7) |
| +0x40 | 4 | RW 칸 크기 | `+0x3e` (인자 8) |
| **+0x48** | 8 | **ROD 버퍼 포인터** | `+0x45` (인자 9) |
| +0x50 | 4 | ROD 칸 크기 | `+0x50` (인자 10) |
| **+0x58** | 8 | **ROS 버퍼 포인터** | `+0x57` (인자 11) |
| +0x60 | 4 | ROS 칸 크기 | `+0x62` (인자 12) |
| **+0x68** | 4 | **개수 (들어갈 땐 칸 수, 나올 땐 실제 개수)** — 비-Ex 는 끝난 뒤 `[개수 포인터]` 에 되돌려 씀 (`+0x148 MOV ECX,[RBP+7]` / `+0x14b MOV [RSI],ECX`) | `+0x6b` (`[RSI]` = 인자 13 이 가리키는 값) |

- nsiproxy: 입력 길이 `< 0x70` 이면 `0xC000000D` (`NsippEnumerateObjectsAllParameters +0xd0`, `+0x53a`, sast/121). 성공하면 `Information = 0x70`. 키·RW·ROD·ROS 는 **칸 크기 × 개수** 바이트의 **연속 배열** 이다: 어댑터 i 의 ROD 는 `ROD포인터 + i × ROD칸크기` (`AllocateAndGetAdaptersAddresses +0x31e IMUL R13,R15,0x290`, `+0x27f IMUL R12,R15,0x238`).
- 칸 뜻 이름은 모두 PDB 에 없다 — "키/RW/ROD/ROS" 는 코드가 같은 순서로 (포인터, 크기) 쌍을 쓰는 것 + 호출자가 RW 에 쓰는지 (`NsiSetParameter` 0 = RW) 로 맞춘 `[추측]`.

### A4. 소비자가 응답에서 읽는 칸 (`GetAdaptersAddresses`) — 루프백 줄을 짤 때 필요한 만큼

값은 **진짜 Windows 가 정함 (공급자 = ndis.sys·tcpip.sys)** 이라 정적으로 모른다. 아래는 "코드가 어느 칸을 어디에 쓰나" 다 (이름은 SDK `IP_ADAPTER_ADDRESSES` 칸 순서와 맞춰 본 `[추측]`).

**NDIS 객체 1 (어댑터 표). 키 8 B = 어댑터 LUID** (디컴파일 `_Dst[0x38] = 키[i]` — 어댑터 구조체의 `Luid` 칸). 개수 = 어댑터 수.

| 칸 | 크기 | 쓰이는 곳 | 근거 줄 (`AllocateAndGetAdaptersAddresses`) |
|---|---|---|---|
| ROD +0x000 | 4 | **CompartmentId**. 거르기 (`==` RW[+0]) 와 어댑터 구조체 `CompartmentId` | `+0xa26`, 디컴파일 `_Dst[0x3e]` |
| ROD +0x004 | 16 | NetworkGuid | `_Dst[0x3f..0x42]` |
| ROD +0x014 | 2 | 이름 (별칭 = FriendlyName) **바이트 길이**, 문자열 (UTF-16, 최대 0x100 글자) 은 +0x016 | `+0x328 MOVZX [+0x14]` |
| ROD +0x218 | 4 | **OperStatus** (1 = 올라감) | `+0x7a5` |
| ROD +0x21c | 4 | 상태 비트 (`& 0xb == 8` 이고 OperStatus 5 이면 OperStatus 를 1 로 고침) | `+0xd5b` |
| ROD +0x220 | 4 | **Mtu** (IP 인터페이스 표가 있으면 그쪽 값으로 덮음) | `+0x783` |
| ROD +0x224 / +0x226 | 2 / 최대 8 | **물리 주소 길이 / 주소** (길이 > 8 이면 8 로 자름) — **루프백 = 길이 0 (MAC 없음)** | `+0x707` |
| ROD +0x270 / +0x278 | 8 / 8 | TransmitLinkSpeed / ReceiveLinkSpeed | `+0x7b6`, `+0x7ca` |
| ROS +0x000 | 4 | **IfIndex** (인터페이스 번호) | 디컴파일 `_Dst[1] = ROS[0]` |
| ROS +0x004 / +0x006 | 2 / 최대 0x100 글자 | **Description** 바이트 길이 / 문자열 | `+0x342 MOVZX EAX,word [ROS+4]` |
| ROS +0x208 | 2 | **IfType** (루프백 = 24 = 0x18; `GetAdaptersInfo` 는 0x18 을 건너뜀 — B4) | `+0x793` |
| ROS +0x20c / +0x210 | 4 / 4 | `==4` 면 플래그 0x10, `==2` 면 플래그 0x8 | `+0x764`, `+0x742` |
| ROS +0x214 | 4 | 어댑터 구조체 `+0x10c` 칸 | `+0x816` |
| ROS +0x218 | 16 | **인터페이스 GUID** → `AdapterName` 문자열 `"{%08lX-%04X-…}"` | `+0x56a`, `+0x2fc ~ +0x307 StringCchPrintfA` |
| ROS +0x22c | 4 | `& 0x100` + OperStatus 5 조합 | `+0xd73` |

**IPv4 · IPv6 객체 7 (IP 인터페이스 표). 키 8 B = LUID — NDIS 표의 LUID 와 같으면 같은 어댑터** (`AllocateAndGetAdaptersAddresses` 디컴파일의 `local_a0[j] == local_c0` 루프). 개수 = IP 인터페이스가 있는 어댑터 수.

| 칸 | 크기 | 쓰이는 곳 |
|---|---|---|
| RW +0x01c | 4 | **Ipv4Metric / Ipv6Metric** |
| RW +0x038 ~ +0x077 | 64 | ZoneIndices (16×4) 로 짐작 `[추측]` |
| RW +0x078 | 4 | **Mtu** |
| RW +0x00a / +0x00b | 1 / 1 | IPv6 플래그 (0x200 / 0x20) |
| ROS +0x004 / +0x008 | 4 / 4 | `ROS+8 == 0xe` 면 어댑터 `IfType` 를 `ROS+4` 로 바꿈 |

- **어댑터 거르기** (플래그 0x100 = 모든 인터페이스 가 없을 때): 계열 0 이면 IPv4 또는 IPv6 인터페이스 표에서 짝이 찾아져 어댑터 플래그의 0x80/0x100 중 하나가 켜진 것만 남는다 (`+0x9df TEST [R15+0x5c],0x180`). **IP 인터페이스가 없는 NDIS 어댑터는 빠진다.**
- 루프백 줄에 대한 정적 결론: 어댑터 구조체는 위 칸만 요구한다 (loopback 이라고 따로 다루는 줄 **없음**; `IfType 24` 거르기는 `GetAdaptersInfo` 쪽에만 있다). 값 (OperStatus 1, Mtu, 속도, 별칭 "Loopback Pseudo-Interface 1" 등) 은 `TODO(VM)`.

### A5. 부른 쪽이 대답을 어떻게 읽나 (`부르는 쪽 표`)

| 호출 | 보는 것 | 안 보는 것 | 줄 |
|---|---|---|---|
| `nsi!NsiGetAllParameters` / `NsiEnumerateObjectsAllParameters` / `NsiAllocateAndGetTable` | `NtDeviceIoControlFile` 반환이 `0x103` 이면 이벤트 기다린 뒤 `IOSB.Status`; **음수 또는 `0x105`** 면 `RtlNtStatusToDosError` 값을 돌려줌 (그 값이 0 이면 성공으로 이어 감). `NsiGetAllParameters` 만: RW 크기 칸 `+0x40` < 요청 → `0xc000000d`. `NsiAllocateAndGetTable` 둘째 호출만 `0xea` 에서 다시 시도 (A2) | `Information` | `NsiGetAllParameters +0x113 ~ +0x150`, `+0x139`; `NsiGetParameter +0xee ~ +0x158`; `NsiAllocateAndGetTable +0x127 ~ +0x163`, `+0x377 ~ +0x3b2` |
| `nsi!NsiGetParameter` / `NsiGetParameterEx` | 오류 계열 (`0xC0000000`) 이면 `RtlNtStatusToDosError` 값을 돌려줄 뿐 결과 크기 칸 (`+0x48`) 은 안 건드림. **`0x105` 와 `0x8…` 경고 계열이면 `IOSB.Information` 을 구조체 `+0x48` (결과 크기 칸) 에 써 준 뒤 `RtlNtStatusToDosError(status)` 를 돌려줌** ("필요한 크기를 되돌려 주는" 규칙; 예 `0x80000005` → `0xea` 로 짐작 `[추측]`). 성공 (0, `0x105` 아님) 이면 Ex 는 `Information` 을 `+0x48` 에 쓰고 0 반환, 비-Ex 는 0 반환 | | `NsiGetParameter +0x112`, `+0x13c ~ +0x158`, `+0x1bb ~ +0x1c1`; `NsiGetParameterEx +0xad`, `+0xb5 ~ +0xb9`, `+0xcf ~ +0xf4` |
| **열기 (`NsiOpenDevice`)** | `CreateFileW(L"\\.\Nsi", 0, 3, NULL, OPEN_EXISTING, FILE_FLAG_OVERLAPPED)` (`NsiGetAllParameters +0x1a6`). **`INVALID_HANDLE_VALUE` 이고 `GetLastError() == 0` 이면 오류로 안 보고 그대로 진행** (`+0x1e2 CALL GetLastError / +0x1ee TEST / +0x1f0 JZ +0x1e0`) → 전역 핸들 −1 로 `NtDeviceIoControlFile` → `0xc0000024` (지금 에뮬레이터가 보는 그것). `GetLastError ≠ 0` 이면 그 값을 호출자에게 돌려줌 (`+0x1f6 JMP +0x1bb`) | | `nsi/NsiGetAllParameters.asm +0x181 ~ +0x1f6` (sast/121 3 절과 같은 틀) |
| `iphlpapi!GetAdaptersAddresses` | 첫 `NsiGetAllParameters` 가 0 이 아니면 **계열·번호를 기본값 (번호 1) 으로 두고 이어 감** (`+0xcf JNZ +0xf71` → `+0xf74 MOV R13D,1` → `+0xf7a JMP +0xdc`) — 여기선 실패해도 안 멈춘다. `NsiAllocateAndGetTable` #1 이 0 아니면 오류 반환 (`+0x16f`), #2·#3 은 0·0x32 만 통과 | RW 둘째 dword (범위) 는 플래그 0x200 일 때만 | `AllocateAndGetAdaptersAddresses +0xcd ~ +0xf74`, `+0x16f`, `+0x1d9`, `+0x246` |
| 〃 결과 | 어댑터 0개면 `GetAdaptersAddresses` 가 **`0xe8` (ERROR_NO_DATA)**. 버퍼 모자라면 `0x6f` 와 필요한 크기 (`최소 0x3fff`, 실제의 2배) | | `GetAdaptersAddresses` 디컴파일 (`if (local_38[0] == NULL) iVar6 = 0xe8`) |
| `winhttp!GetActiveConnectionNameRAS` | `GetAdaptersAddresses` 가 `0x6f` 면 크기 키워 다시 (최대 3), 다른 오류면 **그 HRESULT (`0x8007xxxx`) 로 끝** (`+0x9c`, 내부 `local_c4 = 0x9c`); 성공하면 `GetBestInterfaceEx` 로 인덱스를 얻어 목록에서 `IfIndex` (`[어댑터+4]`) 와 `계열 0x17` (`[어댑터+0x64]` = IfType 칸) 이 맞는 것의 **`FriendlyName` (`[어댑터+0x48]`)** 을 연결 이름으로 씀. 못 찾으면 `0x80070490` (NOT_FOUND) | | `winhttp/GetActiveConnectionNameRAS.c` |
| `winhttp!GetActiveConnectionName` | RAS 가 음수면 **NULL (이름 없음)** 을 돌려줌 — 오류 전파 없음 | | `winhttp/GetActiveConnectionName.c` (`if (-1 < uVar3)` 만 이름 변환) |
| `CheckProxySettings` | 이름 NULL 이면 그냥 `[this+0x218] = NULL` 로 두고 레지스트리 프록시 설정 읽기로 이어 감 | | `winhttp/CheckProxySettings.c` (`GetActiveConnectionName` 결과를 `*(this+0x218)` 에 저장) |
| `LoadAutomaticProxyResolvers` | `GetActiveConnectionName` 결과 (NULL 가능) 를 `local_1b0`/`local_170` 에 두고 `CreateBadProxyListInstance`·`CreateProxyManagerResolverInstance` 로 이어 감 | | `winhttp/LoadAutomaticProxyResolvers.c` |
| `GetBestInterfaceEx` | `NsiGetParameter` 반환을 **그대로** 돌려줌. 출력은 4바이트 인터페이스 번호 | | `iphlpapi/GetBestInterfaceEx.asm +0xe4 ~ +0xf3` |
| **webio `WapInitializeNetworkChange`** (F1) | `NotifyIpInterfaceChange` · `NotifyUnicastIpAddressChange` 가 0 이 아니면 **초기화 실패 → `WinHttpSendRequest` 가 0** | | A1 F1 표 |

### A6. `WinHttpSendRequest` 가 이름 풀이·소켓 단계까지 가려면 NSI 에서 받아야 하는 최소 대답 (sast/121 1(d) 꼴)

**정적으로 말할 수 있는 것**

1. **F2 (프록시 이름 길, 0x12000f · 0x12001b · 0x120007)**: 어떤 NSI 오류도 삼킨다. 필수 대답 **없음**. 가장 단순한 정직한 대답 둘 중 아무거나 된다:
   - (a) **어댑터 0개 PC**: 열기 성공 → `0x12000f` (NDIS 객체 8, RW 8 B) 에 `{1, 0}` · status 0 → `0x12001b` NDIS 객체 1 첫 호출 status 0 · **개수 `+0x68` = 0**, 둘째 호출 (키·ROD·ROS 포인터 있음, 칸 수 10) status 0 · 개수 0 → IPv4·IPv6 객체 7 도 개수 0 ("모듈 없음" 으로 보이려면 `RtlNtStatusToDosError` 가 `0x32` 로 바꾸는 NTSTATUS — `STATUS_NOT_SUPPORTED` 로 짐작 `[추측]`) → `GetAdaptersAddresses` = `0xe8` → winhttp 이름 NULL.
   - (b) **루프백 1개 PC**: 위에서 NDIS 표 개수 1, IPv4 표 개수 1 (IPv6 는 0 또는 1), 칸 값은 A4. → `GetAdaptersAddresses` 가 어댑터 1개 (IfType 24) 를 돌려주고 winhttp 이 `GetBestInterfaceEx` 두 번 (`::` · `0.0.0.0`) 을 부름 → 그 NSI 호출 (`NsiGetParameter`, IPv6/IPv4 모듈, **객체 0**, 키 `{+0: 0, +4: 주소 (v4 4 B / v6 16 B), v6 은 +0x14: 범위 ID}` 크기 0x20/0x38, 종류 1 (ROD), 4 바이트, **오프셋 0x58 (v4) / 0x7c (v6)** = 가장 좋은 경로의 인터페이스 번호; `GetBestInterfaceEx +0xe4`) 이 오류면 연결 이름이 NULL 일 뿐. 성공 (IfIndex 1) 이면 목록에서 IfIndex 1·IfType 칸이 맞는 어댑터 이름을 씀 (A5).
   - 어느 쪽이든 **status 는 오류여도 이름 NULL 로 끝나므로 `WinHttpSendRequest` 는 이어 감.**
2. **F1 (웹 I/O 시작 길)**: 한 번도 `WebInitialize` 가 성공하지 못한 프로세스에서는 `NotifyIpInterfaceChange` / `NotifyUnicastIpAddressChange` 두 호출이 **0 을 돌려줘야** 한다 (`InitialNotification` = FALSE 라 콜백은 즉시 안 불림). 이 둘은 NSI **장치** 가 아니라 `winnsi!NsiConnectToServer` (RPC, `ncalrpc`) 길이다 — NSI 장치 대답으로는 못 고친다 → 다음 요청 후보 1. (이 줄이 실패하는 샘플은 `WinHttpSendRequest` 가 프록시 이름 줄 앞에서 0 이 되므로 NSI IOCTL 이 **안 보인다**.)
3. **이름 풀이 (F3)**: NSI 는 `GetCurrentThreadCompartmentId` 한 번 (0x120007, 실패하면 번호 1 을 돌려줌 → 삼킴, B3). 이름 풀이 본문은 dnsapi `Rpc_ResolverQuery` (RPC → DNS 클라이언트 서비스) — 이 요청 범위 밖.

**"같은 0x12000f 가 한 번 더" (요청서 로그)**: 정적으로 가능한 설명 — `GetActiveConnectionName` 이 두 곳에서 불린다 (`CheckProxySettings +0x37c`, `LoadAutomaticProxyResolvers +0x224`) `[추측]`; `GetActiveConnectionNameRAS` 의 `0x6f` 재시도 (최대 3번) 는 오류가 `0x6f` 일 때만이라 NSI 실패 (`ERROR_INVALID_HANDLE` 등) 에선 안 돈다.

**정적으로 못 닫은 것**: 루프백 줄의 실제 바이트, 어댑터 0개일 때 `GetBestInterfaceEx` 가 진짜 Windows 에서 돌려주는 오류 번호 (`0x4d3` ERROR_NETWORK_UNREACHABLE 일지 `0x490` 일지) — 둘 다 `TODO(VM)`.

### A7. 지금 우리 (PE_emulator) 맞나·틀리나

- **`nt_create_file.cpp`**: **틀림.** 이름 분류에 `\??\Nsi` 가 없어 `parse_nt_path` 가 `path_prefix_unsupported` 로 멈추거나 (로그의 "기본 대답, 핸들 −1" 은 이 길). 맞는 모습: 이름 **통째 (대소문자 무시) `\??\NSI`** (nt 쪽에선 `\Device\Nsi`) 를 `is_afd_endpoint` 곁에 `is_nsi` 로 두고, **EA 없음** (`nsi.dll` 은 EA 를 안 넘김), 성공 시 **진짜 핸들** (−1 아님) 과 **비동기 핸들** (`FILE_FLAG_OVERLAPPED` → 동기 옵션 없음 `[추측: kernelbase 의 CreateFile 변환은 이번에 안 봄]`) 을 돌려준다. 근거: `[정적] ms/disassemble/nsi/NsiGetAllParameters.asm +0x181 ~ +0x1ad`, sast/121 3 절.
- **`nt_device_io_control_file.cpp`**: **틀림 (몸 없음).** 코드 셋 `0x12000f` · `0x120007` · `0x12001b` 가 `ioctl_overrides` 에도 없고, 들어와도 `file->synchronous == false` 라 `device_async_handle_unsupported` (`:521`) 로 멈추며, 이벤트 인자도 afd 만 받는다 (`:263` 근처). 맞는 모습: ① 장치 종류에 nsi 추가 (afd 와 같은 이벤트 처리), ② 입력 길이 검사 (0x68 / 0x50 / 0x70 미만이면 `0xC000000D`), ③ 입력 = 출력 같은 구조체를 읽어 **구조체 안 포인터 (사용자 주소) 를 따라가 쓰기**, ④ 동기 완료 (status 0) 로 돌려주면 이벤트 기다림 불필요 (`NsiGetAllParameters +0x113 CMP EAX,0x103`), ⑤ 비-Ex `NsiGetAllParameters` 가 쓰는 RW 크기 칸 (`+0x40`) 은 **건드리지 말 것** (줄이면 `0xc000000d`). 근거: A3.
- **`tests/file.cpp`**: **없음** (NSI 시험 없음). 추가 후보: (1) `\??\Nsi` 열기 → 핸들 ≠ −1, (2) `0x12000f` NDIS 객체 8 → RW `{1, 0}`, (3) `0x12001b` 첫 호출 포인터 0 → 개수 `+0x68`, (4) 크기 < 0x68 → `0xC000000D`.
- 원칙 (루프백만 있는 PC, 진짜 망에 안 나감): 표 내용이 공급자 몫이므로 **"칸 뜻을 아는 정책 값 + `TODO(VM)`"** 로 둔다 (A4 의 칸 표가 그 "칸 뜻").

---

## B. 나머지 API 넷 (이어서 덧붙임)

### B1. `WinHttpGetIEProxyConfigForCurrentUser`

- 사슬: `winhttp!WinHttpGetIEProxyConfigForCurrentUser` (rva 0x50890) `+0xc3` (ret +0xc8) → `GetActiveConnectionName` → (A1 의 6 ~ 10 과 같음); **그리고** `+0x267` (ret +0x26c) → `GetIEProxyConfigForCurrentUserFromService` (rva 0x82834) `+0x11f` (ret +0x124) → `GetActiveConnectionNameRAS`. 같은 `GetAdaptersAddresses(0, 0x80f)` + `GetBestInterfaceEx`. 근거 `winhttp/WinHttpGetIEProxyConfigForCurrentUser.asm`, `GetIEProxyConfigForCurrentUserFromService.asm`, `winhttp/_map/WinHttpGetIEProxyConfigForCurrentUser_edges.tsv`.
- 인자: A2 와 같음 (NDIS 8 · NDIS 1 · IPv4 7 · IPv6 7).
- 실패 읽기: `WinHttpGetIEProxyConfigForCurrentUser` 는 이름 (NULL 가능) 을 `ReadWinInetProxySettings` 에 넘길 뿐 (`+0xf2`). `GetIEProxyConfigForCurrentUserFromService` 는 RAS 가 음수면 **빈 이름 (`auVar9` 기본값)** 으로 `ReadProxySettingsInternal` 을 부름 (`+0x14a`) — **통째 실패 아님.** 호출자 (`IsPACUrlConfigurationAllowedForHvsi`, `IsFirewallIndicable`) 는 레지스트리에서 읽은 프록시 설정 값을 씀.
- 요청서 표의 "0x12000f + 0x12001b" 중 **0x12001b 는 `GetAdaptersAddresses` 가 첫 호출 (0x12000f) 을 통과한 뒤에야 나온다** (`+0xcf JNZ`). 첫 호출이 항상 실패하는 지금 에뮬레이터에서 0x12001b 가 같이 보이면 같은 샘플이 부른 **다른 호출자** (`GetAdaptersInfo` 등) 일 것 `[추측]`.
- 지금 우리: A7 과 같음 (열기·코드 셋 몸 없음).

### B2. `GetAdaptersAddresses` (exe 가 직접 부를 때 — 플래그가 다름)

- exe 가 흔히 쓰는 플래그 (예: 0 · `GAA_FLAG_INCLUDE_PREFIX`) 면 A1 보다 **NSI 호출이 더 붙는다**. 이 줄들은 `AllocateAndGetAdaptersAddresses` 가 어댑터마다 부른다 (`.asm` 줄은 `_calls.tsv` 의 caller=`AllocateAndGetAdaptersAddresses`, callee=`Add*`):

| 플래그가 켜져 있으면 | 함수 (`+호출 줄`) | 안에서 `NsiAllocateAndGetTable` | 칸 |
|---|---|---|---|
| (1 없음) 유니캐스트 | `AddUnicastAddresses` (`+0xbf8`, `+0xc25`) | `+0x86`, 객체 **10 (0xa)**, 모듈 = 호출자가 준 IPv4 (키 0x10) / IPv6 (키 0x18) | 키 = LUID + 주소, RW 0x1c, ROD 8, ROS 8 (`iphlpapi/AddUnicastAddresses.asm`) |
| (2 없음) 애니캐스트 | `AddAnycastAddresses` (`+0xe1f`, `+0xe40`) | `+0x70`, 객체 **8** | 키만 (키 크기 IPv6 0x18 / IPv4 0x10; RW·ROD·ROS 자리 (`[RSP+0x28~0x40]`) 모두 0) |
| (4 없음) 멀티캐스트 | `AddMulticastAddresses` (`+0xe7d`, `+0xe9e`) | `+0x6e`, 객체 **9** | 키만 (키 크기 0x18 / 0x10) |
| (0x80 또는 0x10 이 켜지면) 게이트웨이·접두사 | `AddGatewaysAndPrefixes` (`+0xdaa`, `+0xdd1`) | `+0x6a`, 객체 **0x10** | 키만 (키 크기 IPv6 0x48 / IPv4 0x30, 호출자가 넘김) |
| (8 없음, 0x800 없음) DNS 서버 | `AddDnsConfiguration` (`+0xcc5`) | (dnsapi `GetRADnsSettingsImpl` → `NsiGetAllParameters`, B3 의 dnsapi 표) | |
| 항상 (어댑터 있으면) | `AddDhcpConfiguration` (`+0xb2d`), `AddNetbtConfiguration` (`+0xb64`) | NSI 아님 (RPC·NetBT 장치) — A1 | |

- 실패 읽기: 반환값 0 이 아니면 즉시 오류로 끝 (`+0xb34`, `+0xb6b`, `AddUnicastAddresses` 등은 0·`0x490`·`0x32` 만 받음 `+0xc2a` 뒤 디컴파일 `if (iVar12 != 0 && iVar12 != 0x490 && iVar12 != 0x32)`). 즉 **IPv4/IPv6 모듈이 없을 때의 `0x32`, 항목이 없을 때의 `0x490` 은 정상** 으로 취급.
- 빈 표: 어댑터 0 → `0xe8`. 바깥 `0x6f` 재시도는 호출자 몫.
- 지금 우리: A7 과 같음 + 유니캐스트 표 (객체 10) 몸도 없음.

### B3. `GetAddrInfoW` · `getaddrinfo` · `gethostbyname` (요청서 표의 `0x120007` · `0x50`)

- **ws2_32 는 이 길에서 NSI 를 안 부른다.** ws2_32 가 NSI 를 가져오지만 부르는 자리는 `WSAGetCurrentThreadCompartmentId` (`WSACreateSortedAddressPairs` 만 부름), `WSACreateSortedAddressPairs`, `ValidateStableTeredoAddress` 뿐이고, 셋의 부르는 쪽은 `WSAConnectByNameHelper` 뿐이다 (`ws2_32/_refs/0x6a000.tsv`, `0x6a008.tsv`, `0x6a010.tsv`; 각 `.c` 머리의 `callers:`). `ws2_32!GetAddrInfoW` (rva 0x1c620) 의 callee 에도 없다 (`LookupAddressForName`, `ServiceNameLookup` …).
- NSI 를 부르는 곳은 **dnsapi → IPHLPAPI**: `dnsapi!DnsQueryEx` / `DnsQuery_W` / `DnsQueryRaw` → **`Query_PrivateExW` (rva 0x1ef8c) `+0x60b` (ret +0x612) → `iphlpapi!GetCurrentThreadCompartmentId` (rva 0xb2d0) `+0x3f` → `nsi!NsiGetParameter(1, NPI_MS_NDIS_MODULEID, 8, 키 없음 (0, 0), 종류 0 (RW), 4바이트 칸, 0)`** — 0x120007 · 0x50. 근거 `dnsapi/Query_PrivateExW.asm +0x60b`, `iphlpapi/GetCurrentThreadCompartmentId.asm` (디컴파일 `NsiGetParameter(1,&NPI_MS_NDIS_MODULEID,8,0,0,0,local_res8,4,0)`).
- **`GetAddrInfoW` 에서 `DnsQueryEx` 까지의 다리**: `mswsock!SaBlob_Query +0x186` 이 `DnsQueryEx` 를 부른다 (`mswsock/_refs/0x69090.tsv`). `ws2_32!GetAddrInfoW` → `LookupAddressForName` → (공급자 목록 → mswsock NSP) → `SaBlob_Query` 사이는 안 봄 `[공백]`.
- 그 밖 dnsapi 의 NSI 부름 (같은 API 의 다른 갈래, 조건부): `UpdateDnsConfig +0x51` · `readHostIpAddressFromRecordListFAZ +0x50` (둘 다 같은 `GetCurrentThreadCompartmentId`), `DnsUpdateMachinePresence` (← `Query_InProcess`, `DnsGetPolicyTableInfo`, `DnsGetProxyInformationEx`) 의 `NsiAllocateAndGetTable` ×2 · `IsInterfaceConnected +0x5a` / `IsInterfaceVirtual +0x58` 의 `NsiGetParameter`, `GetRADnsSettingsImpl(Old)` · `AdapterInfo_UpdateNetworkSignature` · `GetTunnelPhysical(Ip)Adapter` 의 `NsiGetAllParameters` (`dnsapi/_refs/0x1440e0.tsv`, `0x1440e8.tsv`, `0x1440f0.tsv`).
- 인자 (0x50 칸): A3 ② 와 같다 — 모듈 NDIS, 객체 8, 키 없음, 종류 0 (RW), 크기 4, 오프셋 0.
- **실패 읽기**: `GetCurrentThreadCompartmentId` 는 `NsiGetParameter` 가 0 이 아니면 **번호 1 을 돌려준다** (`iphlpapi/GetCurrentThreadCompartmentId.c`: `NVar2 = 1; if (iVar1 == 0) NVar2 = local_res8[0]`) → **삼킨다.** 성공하면 RW 4바이트 = 스레드 compartment 번호 (A2 의 `{번호, 범위}` 의 앞 dword).
- 이 NSI 부름이 실패해도 이름 풀이는 `Query_PrivateExW` 가 이어서 `Rpc_ResolverQuery` (`+0x851`, `+0x90d`) 로 DNS 클라이언트 서비스 RPC 를 부른다 (dnsapi) — **이름 풀이의 진짜 막힘은 NSI 가 아니라 RPC 쪽일 수 있다** (요청 범위 밖).
- 지금 우리: `0x120007` 코드 셋 몸 없음 → 최소 대답은 **NDIS 객체 8 의 RW 에서 4바이트 `1` (또는 `{1, 0}` 앞 dword) + status 0** (A6 1(a) 와 같은 값).

### B4. `GetAdaptersInfo`

- 사슬: `iphlpapi!GetAdaptersInfo` (rva 0x8170) `+0x24` (ret +0x29) → `AllocateAndGetAdaptersInfo` (rva 0x6c90) → **`NsiAllocateAndGetTable` `+0x84` (ret +0x8b)** = NDIS 객체 1 표 (첫 인자 1, 키 8 B, ROD 0x290, ROS 0x238 — `GetAdaptersAddresses` 와 **같은 인자**; `+0x18 ~ +0x80` 설정 줄) → `GetCurrentThreadCompartmentId` `+0xb8` (ret +0xbd) → 어댑터마다: 컴파트먼트 거르기 (`+0xe6 CMP [ROD+0], R15D`), **`IfType == 0x18` (루프백) 건너뜀** (`+0xf7 CMP word [ROS+0x208],0x18 / JZ`), 그다음 **`NsiGetAllParameters` `+0x151` (ret +0x158)** = `(1, NPI_MS_IPV4_MODULEID, 객체 7, 키 8 B = LUID, 나머지 크기 모두 0)` — **버퍼 없이 "그 LUID 의 IPv4 인터페이스가 있나" 만 묻는 존재 확인** (오류면 그 어댑터 건너뜀, `+0x15d TEST EAX / +0x15f JZ`). 끝에 `AddUnicastAddressInfo` / `AddGatewayInfo` (둘 다 NSI 표를 더 부름: `+0x7a` / `+0x61`) / DHCP·NetBT.
- 요청서 표의 `GetAdaptersInfo` 의 `0x12001b · 0x70` 이 **첫 NSI 부름** (`+0x84`) 인 것과 코드가 맞다 (대조 보조 증거).
- **루프백만 있는 PC**: 표에 어댑터 1개 (IfType 24) 가 있어도 건너뛰므로 목록이 비어 **`GetAdaptersInfo` = `0xe8`** (`GetAdaptersInfo` 디컴파일 `if (local_res10 == NULL) uVar3 = 0xe8`). 표가 0개여도 같은 결과.
- 실패 읽기: 첫 `NsiAllocateAndGetTable` 이 0 이 아니면 그 오류를 돌려줌 (`+0x90 TEST EAX / +0x92 JNZ +0x6faf`).
- 지금 우리: A7 과 같음.

---

## 동적으로 확인할 것 (원 요청 3 — 랜선 뽑은 VM, 멈춤점 `caller+0x…`)

멈춤점은 **함수 입구** (`nsi!NsiGetAllParameters` 등) 에 걸고 `ret` 값은 콜스택의 `caller+0x…` 와 맞춰 본다. 커널 멈춤점이 아니라 **유저 (test.exe 프로세스)** 라 조건에 프로세스 이름. 호출 하나마다 입력·출력 구조체 바이트 (`dq <struct> L0xd`) 와 IOSB (`status`·`Information`) 를 본다.

1. **`WinHttpSendRequest` 가 NSI 앞에서 끝나는가** (A6 F1): `webio!WebInitialize` 의 반환, `iphlpapi!NotifyIpInterfaceChange` 반환 (에뮬레이터는 6 일 수 있음), `winnsi!NsiConnectToServer` 반환.
   - `winhttp!WinHttpSendRequest +0x25f` (PrepareWebIOSessionForRequest ret) 의 `eax`, `+0x2ff` (SendRequest ret) 의 `eax`.
   - `winhttp!WINHTTP_DLL::GetWebIOApiHandle +0x44` (WebInitialize ret), webio `WapInitializeNetworkChange +0xa5`, webio `Startup +0xb4` / `+0xf1`.
2. **`GetAdaptersAddresses(0, 0x80f)` 의 NSI 4개 + 반환** (A2): `iphlpapi!AllocateAndGetAdaptersAddresses +0xc8` / `+0x166` / `+0x1d0` / `+0x23d` (각 `NsiGetAllParameters`·`NsiAllocateAndGetTable` ret) 의 `eax` + 구조체 바이트, `GetAdaptersAddresses +0x60` 의 `eax`, `winhttp!GetActiveConnectionNameRAS +0x12a` (`GetAdaptersAddresses` ret), `+0x1ea` / `+0x2da` (`GetBestInterfaceEx` ret).
   - 루프백 줄 하나 (NDIS ROD 0x290 / ROS 0x238 / IPv4·IPv6 RW 0xf0 / ROS 0x20) 의 **칸 값 + 개수** — A4 표의 칸마다 (CompartmentId, 별칭, OperStatus, Mtu, 물리 주소 길이, 속도, IfIndex, Description, IfType, GUID).
3. **`GetBestInterfaceEx`** (`iphlpapi!GetBestInterfaceEx +0xeb`): `::` 와 `0.0.0.0` 일 때 `eax` (진짜 Windows 가 주는 오류 번호) 와 NSI 입력 키 0x38 / 0x20 바이트.
4. **`GetAdaptersAddresses` 가 `0xe8` 을 돌려주는 경우** (어댑터 0개) 와 루프백 1개일 때 각각 `WinHttpSendRequest` 가 끝내 돌려주는 값과 `GetLastError`.
5. **`GetAddrInfoW`** (`dnsapi!Query_PrivateExW +0x612`): `GetCurrentThreadCompartmentId` 의 RW 4바이트와 `NsiGetParameter` 반환; `Rpc_ResolverQuery` ret (`+0x856`, `+0x912`) 의 `eax`.
6. **`GetAdaptersInfo`** (`AllocateAndGetAdaptersInfo +0x8b` / `+0x158`): 어댑터 1개 (루프백) 일 때 `+0xf7` 건너뜀을 지나는지.
7. `NsiGetAllParameters` (비-Ex) 의 `+0x139` 비교가 통과하는지 (RW 크기 칸 `+0x40` 이 8 로 남는지).

## 다음 요청 후보

1. **`winnsi.dll` · `nsisvc.dll` (RPC 쪽)**: `NsiConnectToServer` 가 실제로 무엇을 하는지 (ncalrpc 끝점 이름, 바인딩만인지 호출인지) — F1 의 "치명" 줄을 닫는다. `NotifyIpInterfaceChange` / `NotifyUnicastIpAddressChange` 가 에뮬레이터에서 0 을 돌려주려면 필요한 것. (U6 "RPC 서버 흉내" 와 같은 줄기.)
2. **이름 풀이 길**: webio `WaStartDnsQuery` → `WapDnsResolveGeneric` → `ws2_32!GetAddrInfoExW` → (mswsock NSP `SaBlob_Query`) → `dnsapi!DnsQueryEx` → `Query_PrivateExW` → `Rpc_ResolverQuery` (DNS 클라이언트 서비스 RPC). 이 문서는 NSI 한 줄 (`GetCurrentThreadCompartmentId`) 만 닫았다.
3. **커널 쪽 NSI 모듈 (`nsi.sys`) 과 공급자 (`ndis.sys`, `tcpip.sys`) 가 객체 1·7·8·0 의 칸을 채우는 줄** — 루프백 줄의 값 (별칭·MTU·속도·GUID·IfIndex) 이 정적으로 나오는지. 개수 0 일 때의 반환 status.
4. `AddDhcpConfiguration` (dhcpcsvc RPC, 반환 0 이 아니면 `GetAdaptersAddresses` 전체 실패) 와 `AddNetbtConfiguration` (`\Device\NetBT_Tcpip_{GUID}` 열기) — 어댑터가 1개 이상일 때 `GetAdaptersAddresses` 가 NSI 말고 또 무엇을 요구하나.
5. `CheckProxySettings` 가 첫 `WinHttpSendRequest` 에서 `GetActiveConnectionName` 을 타는 조건 (세션의 가상 `SetProxySettings` 가 언제 불리나) — A1 5' 의 `[공백]`.

## 뽑은 것

- **winhttp** (`ms/decomp/winhttp/`, `ms/disassemble/winhttp/`, depth 0): `WinHttpSendRequest SendRequest(HTTP_USER_REQUEST, rva 0x14880 = SendRequest__14880) _CreateProxyResolver SafeGetProxyResolver CheckProxySettings GetActiveConnectionName GetActiveConnectionNameRAS WxGetAdaptersAddresses LoadNetworkIds GetDefaultGatewayMacAddressList WinHttpGetNetworkIdsForNetworks Startup(두 개) WinHttpNetworkChangeNotification GetConnectionNameInternal GetIEProxyConfigForCurrentUserFromService WinHttpGetIEProxyConfigForCurrentUser INTERNET_SESSION_HANDLE_OBJECT LoadAutomaticProxyResolvers SetWpadDecisionForCurrentNetwork Init(넷) _CallGetProxyForUrl _SendRequestWithDrainComplete _SendRequestWithProxyInitialized PrepareWebIOSessionForRequest CreateWebIOSession GetWebIOApiHandle(둘)` (정규식이 같은 이름 함수를 여럿 집어서 `SendRequest` 다섯 · `Init` 넷이 같이 나옴 — 읽은 것은 위 괄호의 것).
- **webio**: `WaMapInterfaceToAddressFamily WapTcpStartDnsQuery WapTcpDnsQueryCompletionRoutine WapHttp3SelectResolutionAddressFamily WapHttp3DnsQueryCompletionRoutine WapInitializeNetworkChange WaInitializeUtil WapInitializeDll WaStartDnsQuery`.
- **IPHLPAPI**: `GetAdaptersAddresses AllocateAndGetAdaptersAddresses GetBestInterfaceEx InternalGetBestInterface GetIpInterfaceEntry InternalGetIpInterfaceEntry NotifyIpInterfaceChange NotifyUnicastIpAddressChange InternalRegisterChangeNotification NetioRegisterAddressChangeNotification NsiNotificationConnectToServer CancelMibChangeNotify2 GetCurrentThreadCompartmentId SetCurrentThreadCompartmentId GetCurrentThreadCompartmentScope GetDefaultCompartmentId AddDhcpConfiguration AddNetbtConfiguration AddUnicastAddresses AddAnycastAddresses AddMulticastAddresses AddGatewaysAndPrefixes AddUnicastAddressInfo AddGatewayInfo GetAdaptersInfo AllocateAndGetAdaptersInfo`.
- **ws2_32** (배제용): `GetAddrInfoW GetAddrInfoExW WSAGetCurrentThreadCompartmentId WSACreateSortedAddressPairs ValidateStableTeredoAddress`.
- **dnsapi**: `Query_PrivateExW Query_InProcess UpdateDnsConfig readHostIpAddressFromRecordListFAZ IsInterfaceConnected IsInterfaceVirtual DnsUpdateMachinePresence GetRADnsSettingsImpl GetRADnsSettingsImplOld AdapterInfo_UpdateNetworkSignature GetTunnelPhysicalIpAdapter GetTunnelPhysicalAdapter`.
- 지도 (`_map/`): winhttp `WinHttpSendRequest · WinHttpOpen · WinHttpConnect · WinHttpOpenRequest · WinHttpReceiveResponse · WinHttpGetIEProxyConfigForCurrentUser`, webio `Ordinal_9 · Ordinal_17 · Ordinal_25`. 지도는 직접 호출만 따라가서 가상·`indirect-cfg` 호출 (프록시 해석기 인터페이스 등) 을 넘지 못한다 — 그 자리는 위에 `[공백]` 으로 적었다.
- 누가 읽고 쓰나 (`refs`): `ms/disassemble/winhttp/_refs/{0x120148,0x120150 (NsiFreeTable·NsiAllocateAndGetTable), 0x120110·0x120118·0x1200e8·0x1200f8·0x120100·0x120108·0x120120·0x120128·0x120138 (IPHLPAPI 가져오기 칸), 0xa4f4c, 0x5c8a4, 0x5dba0, g_ullNetworkGeneration}.tsv`, `webio/_refs/{0xc1080, 0xc1098, 0xc10a0}.tsv`, `iphlpapi/_refs/{0x30068 (NsiGetParameter), 0x30078 (NsiGetAllParameters), 0x30080 (NsiEnumerate…), 0x30090 (NsiAllocateAndGetTable)}.tsv`, `ws2_32/_refs/{0x6a000, 0x6a008, 0x6a010}.tsv`, `dnsapi/_refs/{0x1440b0 (GetCurrentThreadCompartmentId), 0x1440e0, 0x1440e8, 0x1440f0, 0x1440f8}.tsv`. (`_refs` 파일 이름은 가져오기 칸의 RVA.)
- 모듈 ID 바이트 (IPHLPAPI `.rdata`): `NPI_MS_NDIS_MODULEID` rva 0x25350, `NPI_MS_IPV6_MODULEID` 0x25368, `NPI_MS_IPV4_MODULEID` 0x25380 — 각 24 바이트 = `{ DWORD 0x18, DWORD 1 (GUID 형), GUID 16 B }`.

## 4. 모듈 ID 값 (질문 1 "16 B 값 그대로")

| PDB 이름 | `.rdata` (IPHLPAPI) | 원본 24 바이트 | GUID |
|---|---|---|---|
| `NPI_MS_NDIS_MODULEID` | rva 0x25350 | `18000000 01000000 114a00eb 1a9b d411 9123 0050047759bc` | `eb004a11-9b1a-11d4-9123-0050047759bc` |
| `NPI_MS_IPV6_MODULEID` | rva 0x25368 | `18000000 01000000 014a00eb 1a9b d411 9123 0050047759bc` | `eb004a01-9b1a-11d4-9123-0050047759bc` |
| `NPI_MS_IPV4_MODULEID` | rva 0x25380 | `18000000 01000000 004a00eb 1a9b d411 9123 0050047759bc` | `eb004a00-9b1a-11d4-9123-0050047759bc` |

(그 밖 PDB 이름만 확인: `NPI_MS_TCP_MODULEID`, `NPI_MS_UDP_MODULEID`, `NPI_MS_FL4/6…_MODULEID` — 값은 안 읽음.) 위 GUID 는 24 바이트 중 +8 부터 16 바이트를 little-endian GUID 로 읽은 값 (`uuid.UUID(bytes_le=…)`). 객체 번호 (NDIS 1·8, IPv4/IPv6 0·7·8·9·10·0x10) 는 **PDB 에 이름이 없고** 호출 줄의 숫자뿐이다 (이 문서의 표 이름은 모두 `[추측]`).

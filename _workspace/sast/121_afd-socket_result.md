# 121 afd-socket — 정적 분석 결과

- 요청서: `_workspace/sast/121_afd-socket.md` (Q217, PE_emulator "소켓 장치 (AFD)")
- 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/` 아래 바이너리.
- 날짜: 2026-11-16. analyze 는 서버 메모리 (46G 중 가용 22G) 를 보고 **한 번에 하나씩** (mswsock → afd → nsi → nsiproxy), 각 20초 안팎. ntoskrnl (sast/122) 은 건드리지 않았고 `ms/disassemble/ntoskrnl/IopXxxControlFile.asm` 은 읽기만 했다.

## PDB GUID 확인 (모듈마다)

`fetch_pdb.py` 가 바이너리의 CodeView 기록에서 GUID+Age 를 읽어 심볼 서버에서 받는다. 네 개 다 Ghidra `SetPdb:` 줄 있고 함수 이름이 PDB 이름으로 잡힌다 (`WSPSocket`, `AfdConnect`, `NsiOpenDevice`, `NsippDispatchDeviceControl` 확인).

| 모듈 | 바이너리 | PDB guid+age | 함수 수 (`_index.tsv`) | 로그 |
|---|---|---|---|---|
| mswsock | `mswsock.dll` 451,920 B | `A3E5C0BA2810B53B2646FE9284927D821` | 781 | `ms/ghidra/mswsock.analyze.log` |
| afd | `drivers/afd.sys` 738,792 B | `582DC2B31D185EBE4A3FDD4F872920491` | 1280 | `ms/ghidra/afd.analyze.log` |
| nsi | `nsi.dll` 51,128 B | `662A028F57A7C702B11F583CE15A8E631` | 42 | `ms/ghidra/nsi.analyze.log` |
| nsiproxy | `drivers/nsiproxy.sys` 77,824 B | `27E1902069556C2598DF39EC28A3E3B41` | 77 | `ms/ghidra/nsiproxy.analyze.log` |

- `mswsock.dll.pdb` 는 이미 `ms/sym/` 에 있었다 (이번에 받은 게 아님, GUID 같음). 별칭 표 4개는 `ms/sym/<이름>.aliases.tsv`.
- 커널 GUID (`C29EBFB0…`) 확인은 ntoskrnl 만 해당이라 이번엔 해당 없음.
- PDB 는 공개 심볼이라 **구조체 타입·IOCTL 상수 이름이 없다.** 아래 "제어 코드 이름" 은 전부 "이름 없음 (PDB 에 상수 없음) + 처리 함수 이름" 으로 적는다.

## 뽑은 것

- mswsock: `WSPSocket`·`SockSocket` (depth 1, 23개) + 이름으로 고른 함수 (depth 0) `WSPConnect SockDoConnect SockDoConnectReal SockDoConnectEx SockPostProcessConnect SockWaitForSingleObject SockIsSocketConnected MSAFD_ConnectEx SockGetTdiName SockDemandStartAfd SockCloseSocket SockBuildTdiAddress SockBuildSockaddr SockSetConnectData WSPBind WSPSetSockOpt SockUpdateWindowSizes SockGetTdiHandles SockSetInformation Tcpip4_WSHOpenSocket2 Tcpip4_WSHNotify` → `ms/decomp/mswsock/`, `ms/disassemble/mswsock/`
- afd (depth 0): `AfdDispatch AfdDispatchDeviceControl AfdDispatchImmediateIrp AfdCreate AfdFastIoDeviceControl AfdGetInformation AfdSetContext AfdConnect AfdRestartConnect AfdTLConnectComplete AfdTLConnectComplete2 AfdCreateConnection AfdTLIoControl AfdSuperConnect AfdRestartSuperConnect AfdTLSuperConnectComplete AfdInitializeData` → `ms/decomp/afd/`, `ms/disassemble/afd/`
- nsi (depth 0): `NsiOpenDevice NsiIoctl NsiAllocateAndGetTable NsiEnumerateObjectsAllParameters(Ex) NsiGetAllParameters(Ex) NsiGetParameter(Ex) NsiFreeTable`
- nsiproxy (depth 0): `NsippDispatchDeviceControl NsippEnumerateObjectsAllParameters NsippGetAllParameters NsippGetParameter NsippProbeAndAllocateParameters`
- 전역·표 읽기 (`refs`): `ms/disassemble/afd/_refs/{AfdReceiveWindowSize,AfdSendWindowSize,AfdTLConnectComplete,AfdTlClientConnectDispatch}.tsv`, `ms/disassemble/mswsock/_refs/{Tcpip4_TdiModeStream,Tcpip4_WSHOpenSocket2,0x56848,0x56890,0x568a8}.tsv`.
- 지도 (`_map/`) 는 안 만들었다 (뿌리 정의 안 함, 필요한 함수를 이름으로 찾았다).
- afd 표 두 개 (`AfdIoctlTable`, `AfdIrpCallDispatch`, `AfdImmediateCallDispatch`) 는 `.sys` 파일의 데이터 칸을 직접 읽어 (함수 이름은 `_index.tsv` 와 맞춤) 풀었다. `AfdIoctlTable[i]` 의 `(코드>>2)&0x3ff == i` 가 74칸 전부 맞는다 (표를 제대로 읽었다는 증거).

## 버전·스크립트 대조

- "알려진 사실" 표에는 이 네 모듈이 없다. 대신 `.asm` 의 `+0x` 와 원본 바이트를 직접 맞췄다 (스크립트 오프셋 계산 확인):
  - `SockSocket +0x591` = `48 ff 15 …` (7바이트 CALL, ret `+0x598`) 일치. `+0x9f2` 도 같은 꼴.
  - `SockSetHandleContext +0x263` = `c7 44 24 28 47 20 01 00` (IOCTL 0x12047). `SockGetInformation +0xae` = `… 7b 20 01 00` (0x1207b).
  - `SockDoConnectReal +0x330` / `+0x43f` = `… 07 20 01 00` (0x12007). `WSPBind +0x3cf` = `… 03 20 01 00` (0x12003).
  - `AfdDispatchDeviceControl` 가 읽는 표 이름·주소 (`AfdIoctlTable` = `[RDX+RAX*4+0x7b170]`, `AfdIrpCallDispatch` = `+0x78430`) 가 `.asm` 주석의 PDB 이름과 같고 위 표 74칸이 코드와 맞는다.
- **판정: 일치.** "스크립트 문제" 없음.

## 먼저 읽는 한 줄 답

1. **열기**: 소켓 = `NtCreateFile("\Device\Afd\Endpoint", 0xc0140000 …, EA 이름 "AfdOpenPacketXX")`. 기본 TCP/UDP 는 **전송 이름이 비어 있는 EA** (EaLength 0x39, EaValueLength 0x1e). 그 뒤 `0x1207b` ×2 (프로세스 첫 소켓에서만), `0x12047` ×1. mswsock 은 열기·`0x12047` 은 **status 부호만** 보고, `0x1207b` 는 status 가 성공일 때 출력 +8 의 dword 만 읽는다 (호출 쪽은 반환값을 안 쓰므로 실패하면 값이 0 으로 남을 뿐 소켓은 만들어진다).
2. **연결**: TCP 면 먼저 `0x120bf` → `0x12003` (묵시적 bind) → `0x12047` 을 내고, 그 뒤 `0x12007` (입력 = 0x18 머리 + **날 sockaddr**, 출력 0). 길이 없음의 status 는 afd 가 정하지 않고 아래 계층 (tcpip, TL) 이 정한다 — afd 는 그대로 IRP 에 넣는다. mswsock 이 `NtStatusToSocketError` 로 바꾼다 (NETWORK_UNREACHABLE → 10051, HOST_UNREACHABLE → 10065, IO_TIMEOUT → 10060, CONNECTION_REFUSED → 10061).
3. **nsi**: `\\.\Nsi` 를 `CreateFileW` 로 열고 (겹침 핸들) `0x120007` (입력 0x50) · `0x12000f` (0x68) · `0x12001b` (0x70) 를 낸다. 모두 입력 = 출력 같은 버퍼.

---

## 질문별 답 [정적]

### 1. 열기 (mswsock, 유저 쪽)

**호출 사슬 (WSPSocket 부터)** — `ws2_32` → provider `WSPSocket` 로 넘어가는 다리는 이번에 안 봄 `[공백]` (ws2_32 는 아직 분석 안 됨).

| 함수 | 줄 | 하는 일 |
|---|---|---|
| `mswsock!WSPSocket` (rva 0x4480) | `+0x226/+0x22b` | `SockSocket` 부름 |
| 〃 | `+0x248/+0x24d` | `SockSetHandleContext` 부름 (**0x12047**). 바로 앞 `+0x23c` 에서 소켓 상태 `[sock+0x18]=0` 로 둠 |
| 〃 | `+0x275/+0x27a` | `SockUpcallTable +0x30` 칸 간접 호출 (인자 = 카탈로그 번호 `[sock+0x4c]`, AFD 핸들 `[sock+8]`, 오류 칸). 서명으로 보아 `WPUCreateSocketHandle` `[추측]` — ws2_32 쪽 일 |
| 〃 | `+0x2b5` | `*lpErrno = 오류` (`MOV [R12],EAX`). 반환값은 `RDI` (처음 `-1`, `+0x5c/+0x63`) |
| `mswsock!SockSocket` (rva 0x48a0) | `+0x173/+0x178` | `SockGetTdiName` (전송 이름·주소 계열 확정) |
| 〃 | `+0x1d5`, `+0x1f6` | 소켓 구조체 할당·`memset` |
| 〃 | `+0x3c7` ~ `+0x41a`, `+0x489`, `+0x7aa` | **EA 버퍼 짓기** (아래 표) |
| 〃 | `+0x4a8` ~ `+0x511` | `OBJECT_ATTRIBUTES` (길이 0x30, 이름 = `\Device\Afd\Endpoint` (.rdata `0x561d0`), 속성 0x42 = INHERIT\|CASE_INSENSITIVE, `dwFlags & 0x180` 이면 0x40) |
| 〃 | `+0x591/+0x598` | `NtCreateFile` |
| 〃 | `+0x7d2/+0x7d7`, `+0x7f5/+0x7fa` | `SockGetInformation` 클래스 7, 클래스 6 (**0x1207b ×2**) |

#### (a) EA 버퍼 모양 [정적]

NtCreateFile 인자 (x64 스택, `SockSocket +0x554 ~ +0x56e`): 접근 `0xc0140000` (`+0x503`; 플래그 0x40 이 있으면 `0xc1140000`, `+0xb53`), AllocationSize 0, FileAttributes 0, ShareAccess 3, CreateDisposition 3 (FILE_OPEN_IF), **CreateOptions = `dwFlags & 0x101` 이 있으면 0 (비동기), 없으면 0x20 (동기, `FILE_SYNCHRONOUS_IO_NONALERT`)** (`+0x4f2 ~ +0x4ff`), EaBuffer = 스택 `[RSP+0x140]` (0x60 바이트 넘으면 힙, `+0x3c7`/`+0x87f`), EaLength = `R13D`.

EA 는 항목 하나 (`FILE_FULL_EA_INFORMATION`). 칸 표 (오프셋은 EA 시작 기준):

| 오프셋 | 크기 | 뜻 | 값 | 근거 줄 (`SockSocket`) |
|---|---|---|---|---|
| +0x00 | 4 | NextEntryOffset | 0 | `+0x3d5` |
| +0x04 | 1 | Flags | 0 | `+0x3d8` (`word [RSI+4]=0x0f00` 의 아래 바이트) |
| +0x05 | 1 | EaNameLength | **0x0f** | `+0x3d8` (윗 바이트) |
| +0x06 | 2 | EaValueLength | **전송이름 바이트수(`Length`) + 0x1e** | `+0x3af ~ +0x3b7`, `+0x3e9` |
| +0x08 | 16 | EaName | **`AfdOpenPacketXX`** + 널 (15글자) — `.rdata 0x56200` 에서 16바이트 복사 | `+0x3de/+0x3e5` |
| +0x18 | 4 | EndpointFlags (비트 모음, 아래) | 0 에서 시작 | `+0x41a` 에서 0, 이후 OR |
| +0x1c | 4 | GroupID | `WSPSocket` 인자 `g` (= `[RSP+0x210]`) | `+0x489` |
| +0x20 | 4 | 주소 계열 | 예 AF_INET=2 | `+0x402/+0x409` |
| +0x24 | 4 | 소켓 종류 | 예 SOCK_STREAM=1 | `+0x40c/+0x410` |
| +0x28 | 4 | 프로토콜 | 예 IPPROTO_TCP=6 | `+0x413/+0x417` |
| +0x2c | 4 | 전송 이름 길이 (바이트, 널 제외) | `Length` | `+0x3f5` |
| +0x30 | `Length`+2 | 전송 이름 (WCHAR) | **전송 이름이 있을 때만** 복사 | `+0x791 ~ +0x7aa` (`TEST [RDI+0x45],0x10` 이 0 일 때) |

- **EaLength = `Length` + 0x39** (`+0x3bb ~ +0x3bf`). 전송 이름이 비면 `Length`=0 → **EaValueLength 0x1e, EaLength 0x39** (이름 쪽 칸 안 씀, 스택 버퍼 안 지워서 +0x30 뒤는 쓰레기).
- **기본은 전송 이름이 빈 상태 (TL 모드)**: `Tcpip4_WSHOpenSocket2` 가 이름을 `Tcpip4_TdiModeStream` 이 켜져 있을 때만 `\Device\Tcp` 로 주고 (`+0x80 ~ +0xb2`: `AL=TdiModeStream; NEG; SBB; AND` 로 이름 또는 NULL 선택), 꺼져 있으면 NULL → `RtlInitUnicodeString(NULL)` → 길이 0. `Tcpip4_TdiModeStream/Dgram/Raw` 는 `Tcpip4_WSHNotify` 가 알림 번호 `0x80000000/0x40000000/0x20000000` 을 받을 때만 1 로 바뀐다 (`_refs/Tcpip4_TdiModeStream.tsv`). 부르는 쪽은 `SockSocket` 실패 갈래 (`+0x916 ~ +0x966`, afd 가 `0x8000001c` 를 돌려주는 TDI 필터 상황) 뿐 → 에뮬레이터 상황에선 안 탄다 `[추측]`. UDP 는 `\Device\Udp` (`+0x170 ~ +0x183`), IPv6 는 `Tcpip6_WSHOpenSocket2` 의 `\Device\Tcp6/Udp6` 이름. 문자열은 mswsock 안에 박혀 있다 (`.rdata` `\Device\Tcp` `0x56890`, `\Device\Udp` `0x568a8`, `\Device\Tcp6` `0x56848`, `\Device\Udp6` `0x56868`, `\Device\RawIp` `0x5b878`).
- 전송 이름 얻는 길: `SockGetTdiName` 이 helper 표의 `+0x50` 칸을 간접 호출 (`+0x851`, `+0x9f3`, `+0xb09`, `+0xc74` 의 `indirect:cfg:[R15/R10+0x50]`) → `Tcpip4_WSHOpenSocket` (`+0x25` 에서 `Tcpip4_WSHOpenSocket2` 부름). 표를 채우는 줄은 `SockLoadHelperDll +0x38c/+0x393` (`[RDI+0x58]=Tcpip4_WSHOpenSocket2`). 칸 번호↔함수 짝 확인은 `[추측]`.

**EndpointFlags (+0x18) 에 켜지는 비트** — 이름은 PDB 에 없고 값·조건만 `[정적]`. 조건에 나오는 `[sock+0x50]` 은 `WSAPROTOCOL_INFOW.dwServiceFlags1` (`WSPSocket +0x1a7/+0x1f7` 에서 `[RSI]` 를 넘겨 `SockSocket +0x2d2 ~ +0x2d9` 에서 `[RDI+0x50]` 에 저장) 이다. `XP1_*` 이름은 Winsock 문서 상수와 값이 맞아서 붙인 것 `[추측]`.

| 비트 | 켜는 줄 | 조건 |
|---|---|---|
| 0x00000001 | `+0x749` | `[sock+0x50]` & 0x1 (connectionless) 이고 종류가 2 또는 3 (아니면 WSAEINVAL 0x2726, `+0x73d ~ +0x743`) |
| 0x00000010 | `+0x76d` | `[sock+0x50]` & 0x8 (message oriented) 이고 종류가 2~5 (종류 1 이면 `[sock+0x50]` & 0x10 필요, 아니면 0x2726; `+0x752 ~ +0x767`) |
| 0x00000100 | `+0x876` (또는 `+0x43d` 뒤 분기) | 종류가 3 (SOCK_RAW) |
| 0x00001000 | `+0x83c` | `dwFlags & 0x1e` (멀티포인트 계열) 이고 `[sock+0x50]` & 0x400 |
| 0x00010000 / 0x00100000 | `+0xb4a` / `+0x86d` | 멀티포인트 루트 표시 (`dwFlags` 0x2 / 0x8) |
| 0x01000000 | `+0x485` | helper 가 이미 TDI 모드로 바뀐 종류, 또는 전송 이름이 비고 `SockTLNPIListenerCount>0` 이고 종류 1, 또는 `IsAppContainer()` (`+0x452 ~ +0x485`) |
| 0x10000000 | `+0xac0` | `[sock+0x45] & 0x20` (등록 I/O 소켓, `dwFlags & 0x100`) |

→ 보통 데스크톱 프로세스의 TCP `socket()` = **EndpointFlags 0**, UDP = **0x11** `[추측: dwServiceFlags1 값은 이번에 안 읽음 — provider 정보 표 (`SockTcpProviderInfo`) 의 값에 달림]`.

afd 쪽 검사 (`AfdCreate`, 규칙을 에뮬레이터가 맞춰야 `Create` 가 성공): 이름이 `"AfdOpenPacketXX"` (다른 이름 `"AfdSwOpenPacket"`, `"AfdRioRDOpenPacket"` 은 다른 길, `AfdCreate +0xa3 ~ +0x3fe` 의 `RtlEqualString`), `EaValueLength ≥ 0x1c`, `전송이름길이+2` 가 짝수, `0x18 + (길이+2) ≤ EaValueLength`, EndpointFlags 의 비트는 각 16진 자리의 맨 아래 비트 (0x11111111) 만 허용 (`& 0xeeeeeeee != 0` → 0xC000000D), 전송 이름이 없으면 주소 계열 < 0x23 이고 `-1` 아님, 종류·프로토콜 < 0x10000, `(flags & 0x11) == 0x10` 이면 0xC000000D. 성공 시 `AfdCreate` 는 0 을 돌려주고 `AfdDispatch +0x11b/+0x120` 가 그 값으로 IRP 를 끝낸다.

**지금 우리 (PE_emulator)**: **틀림 (막혀 있음)**. `nt_create_file.cpp:58` 이 `a[9] || a[10]` (EA) 이면 `extended_attributes_unsupported` 로 멈춘다. 이 장치는 EA 가 **필수** (EaLength 0x39). `Device` 종류에 afd 를 넣고 EA 이름이 `AfdOpenPacketXX` 인지, +0x20/+0x24/+0x28 (계열·종류·프로토콜) 을 읽어 파일 객체에 저장해 두어야 한다. 또 `socket()` 은 보통 겹침 소켓이라 CreateOptions=0 (비동기) 으로 열린다 `[추측: ws2_32 의 dwFlags 는 안 봄]` → `nt_device_io_control_file.cpp:226` 의 `device_async_handle_unsupported` 에 걸린다 (비동기 afd 핸들 허용 필요).

#### (b) 열기 뒤 0x1207b · 0x12047 [정적]

**제어 코드 이름**: 둘 다 이름 없음 (PDB 에 상수 없음). 처리 함수: `0x1207b` → `afd!AfdGetInformation`, `0x12047` → `afd!AfdSetContext`. 두 코드 모두 `AfdIrpCallDispatch` 칸은 `AfdDispatchImmediateIrp` 이고, 거기서 `AfdImmediateCallDispatch` 표로 한 번 더 넘어간다 (아래 afd 표).

**`0x1207b` — `mswsock!SockGetInformation` (rva 0x1bd50)**
- 나가는 줄: `+0xbb/+0xc2 NtDeviceIoControlFile` (코드 `+0xae`). 부르는 곳: `SockSocket +0x7d2` (클래스 **7**, 결과를 `SockSendBufferWindow` 에), `SockSocket +0x7f5` (클래스 **6**, `SockReceiveBufferWindow` 에). 둘은 `SockSendBufferWindow == 0` 일 때만 (`SockSocket +0x5e3 CMP [SockSendBufferWindow],0 / JZ` → 프로세스에서 첫 소켓. 값이 0 으로 남으면 매번 다시 묻는다). 클래스 **0xa** 는 `g != 0` 일 때만 (`SockSocket +0x5da ~ +0xc04`, 결과 8바이트를 `[sock+0x58]`/`[sock+0x5c]` 에).
- NtDeviceIoControlFile 인자: FileHandle = `[sock+8]` (방금 연 핸들), **Event = 스레드별 이벤트 `[TLS데이터+0x10]`**, ApcRoutine 0, ApcContext 0, IoStatusBlock = 스택, 코드 0x1207b, **InputBuffer = OutputBuffer = 같은 0x10 바이트 칸**, 입력 길이 = 출력 길이 = 0x10.
- 입력·출력 칸 (같은 버퍼):

| 오프셋 | 크기 | 뜻 | 근거 |
|---|---|---|---|
| +0x00 | 4 | 정보 종류 (클래스) — mswsock 이 넣음 (6 = 받기 창, 7 = 보내기 창, 0xa = 그룹) | `SockGetInformation +0x96` (`[RBX]=EDX`) |
| +0x04 | 4 | 안 씀 (0) | `+0x44 ~ +0x4c` |
| +0x08 | 4 또는 8 | **값** (afd 가 씀). 클래스 6/7 은 dword | mswsock 읽기: `+0xe1/+0xe4` (dword → `*RSI`), `+0x151/+0x155` (qword → `*R13`) |
| +0x0c | 4 | 안 씀 | |

- afd 쪽 (`AfdGetInformation`, rva 0x3e350): 입력 길이 < 0x10 이거나 출력 길이 < 0x10 이면 **0xC0000023** (`+0x918`), 모르는 클래스·맞지 않는 엔드포인트면 **0xC000000D** (`+0x91f`). 클래스 분기 (`+0x13b ~ +0x175`): 3, 4, 5, **6**, **7**, 8, 0xa, 0xe. 클래스 6 → 전역 `AfdReceiveWindowSize` (`+0x289`), 클래스 7 → 연결된 소켓이면 연결의 값 아니면 전역 `AfdSendWindowSize` (`+0x24e ~ +0x281`), 클래스 0xa → 엔드포인트 `+0x1d0`/`+0x1d4` 두 dword (`+0x194 ~ +0x1a7`). 성공이면 0x10 바이트를 출력 버퍼에 복사하고 IoStatus.Information = 0x10 (`+0x570 ~`), 상태 0.
- **기본값**: `AfdInitializeData +0x18b ~ +0x19e` 가 `RtlGetVersion` 의 제품 종류가 워크스테이션 (1) 이면 `(0+1)<<16 = 0x10000`, 서버면 `0x20000` 을 `AfdReceiveWindowSize`·`AfdSendWindowSize` 둘 다에 넣는다. 레지스트리 덮어쓰기 (`AfdReadRegistry +0x113 ~ +0x141`) 가 없으면 이 값 → Win11 Education 은 **0x10000**.

**`0x12047` — `mswsock!SockSetHandleContext` (rva 0x4170)**
- 나가는 줄: `+0x270/+0x277 NtDeviceIoControlFile` (코드 `+0x263`). 부르는 곳: `WSPSocket +0x248` (소켓 만들 때), `WSPBind +0x4e4`, `SockPostProcessConnect +0x4a/+0x4f`, `WSPSetSockOpt +0x290` 등.
- 인자: FileHandle `[sock+8]`, Event = 같은 스레드 이벤트, IoStatusBlock 스택, **InputBuffer = 스택 `[RSP+0x70]` (0x100 바이트 안에 들면) 아니면 힙**, 입력 길이 `EBP`, **OutputBuffer = 상태가 0 (열림) 이면 입력 버퍼 안의 "상대 주소" 칸 (`R15`), 길이 `[sock+0x2c]`; 상태가 0 이 아니면 NULL, 0** (`+0x228 ~ +0x255`).
- 입력 칸:

| 오프셋 | 크기 | 뜻 | 근거 |
|---|---|---|---|
| +0x00 | 0x78 | 소켓 공유 정보 (`[sock+0x18 .. +0x90)` 그대로 복사: 상태·계열·종류·프로토콜·주소 길이·SO_* 값 등) | `+0xe0 ~ +0x11b` |
| +0x78 | 4 | helper 문맥 길이 `[RSP+0x50]` | `+0x128 ~ +0x12c` |
| +0x80 | `roundup8(최대주소길이)` | 내 주소 (`[sock+0xa8]` 에서 복사) | `+0x13c ~ +0x14c` |
| (다음) | `roundup8(최대주소길이)` | 상대 주소 (`[sock+0xb0]`) — **출력 버퍼가 가리키는 곳** | `+0x153 ~ +0x16f`, `+0x23d ~ +0x244` |
| (다음) | helper 문맥 길이 | helper 문맥 (helper 호출 `[RCX+0x70]` 가 채움) | `+0x19c ~ +0x1c6` |

  입력 총 길이 `EBP` = `roundup8(최대주소길이)*2 + 0x80 + helper문맥길이` (`+0xbc ~ +0xcc`). `최대주소길이` = helper 정보 `[+0x24]` (IPv4 = sockaddr_in 크기 `[추측]`).
- **helper 가 먼저 실패하면 IOCTL 안 내고 성공 (0) 반환** (`+0xad ~ +0xaf JNZ → +0x2cf XOR EAX,EAX`). 로그에서 `0x12047` 이 빠진 소켓 (52/501) 이 이 갈래일 수 있다 `[추측]`.
- afd 쪽 (`AfdSetContext`, rva 0x20c00): 입력 바이트 전부를 엔드포인트 문맥으로 저장 (`AfdLockEndpointContext`, 더 크면 `ExAllocatePool2` 로 새로 잡아 복사 `+0xf7 ~ +0x119`). 출력 포인터는 **쓰지 않고** "입력 안 어디가 상대 주소인지" 의 오프셋·길이만 엔드포인트 칸에 적는다 (엔드포인트 태그 `0xAFD0` 이고 종류 조건이 맞을 때, `.c` 끝 부분). 사용자 모드 호출이면 출력 포인터가 입력 안이어야 하고 크기 ≤ 0xffff (아니면 `ExRaiseStatus(0xC000000D)`). **상태 0, Information 0.**

#### (c) mswsock 이 대답에서 보는 것 [정적]

| 호출 | 보는 것 | 안 보는 것 | 줄 |
|---|---|---|---|
| NtCreateFile | **status 부호** (`JS`), 핸들 (`[RSP+0xc0]` → `[sock+8]`) | IoStatusBlock (아래 예외 1곳) | `SockSocket +0x59d ~ +0x5c6` |
| 〃 예외 | status `0xC000000E`/`0xC0000034`/`0xC000003A` (`BT 0x104000000001`) 이면 `SockDemandStartAfd` (레지스트리 `AFD` 서비스 시작값 읽고 `NtLoadDriver`) 한 뒤 **NtCreateFile 한 번 더** | | `+0x59f ~ +0x5b8`, `+0x9a1`, `+0x9f2/+0x9f9` |
| 〃 예외 2 | status `0x8000001C` 이고 `IoStatus.Information == 0xFFFFFFFF8000001C` (afd 의 TDI 필터 갈래) 이면 TDI 모드로 다시 시도 (위 EA 절) | | `+0xb77 ~ +0xb8d` |
| 0x1207b (클래스 7, 6) | **반환값을 안 쓴다** (호출 뒤 `TEST` 없음). status 가 `0x103` (PENDING) 이면 `SockWaitForSingleObject`, status<0 이면 이 함수가 오류 코드를 돌려주지만 `SockSocket` 이 버림. 성공이면 출력 `+8` 의 dword 만 읽음 | 출력 `+0`, `+4`, `+0xc`, Information | `SockSocket +0x7d2 ~ +0x7fa`; `SockGetInformation +0xc9 ~ +0xf4` |
| 0x1207b (클래스 0xa) | **반환값을 쓴다** (`+0xc04 MOV EBX,EAX / TEST / JZ`): 실패면 소켓 만들기 실패 | | `SockSocket +0xbff ~ +0xc16` |
| 0x12047 | **status 부호** (`+0x2c2 TEST EBX,EBX / JNS`): 음수면 `NtStatusToSocketError` 로 바꿔 돌려주고 `WSPSocket` 은 정리 (`SockCloseSocket`) 하고 실패 | 출력·Information | `SockSetHandleContext +0x277 ~ +0x2cd`; `WSPSocket +0x24d ~ +0x253`, `+0x3f6` |

**status 가 실패면** (열기 또는 0x12047):
- `SockSocket`/`SockSetHandleContext` 가 `NtStatusToSocketError(status)` (rva 0xbca0) 로 바꾼 WSA 오류 번호를 돌려주고, `WSPSocket` 이 **`*lpErrno` 에 그 번호를 쓰고 `-1` (INVALID_SOCKET) 을 반환** (`+0x2b5`, `+0x2d5`).
- `WSAGetLastError` 에 무엇이 남는지는 ws2_32 의 `WSASocketW` 가 provider 의 `*lpErrno` 를 `SetLastError` 로 옮기는지에 달림 `[공백]` (ws2_32 안 봄). 바꾸기 표 (아래 2(c)) 에서 열기 실패 쪽은: `0xC0000034` (OBJECT_NAME_NOT_FOUND)·`0xC000000E`·`0xC000003A` → **10050 (WSAENETDOWN)**, `0xC0000022` (ACCESS_DENIED) → 10013, 표에 없는 음수 → 10022 (WSAEINVAL).

**지금 우리**: 열기·0x12047 은 status 만 보니 **성공 코드만 맞으면 된다 (맞음)**. `0x1207b` 는 출력 +8 만 읽는다. 지금 에뮬레이터는 afd 제어 코드를 전혀 모른다 (`nt_device_io_control_file.cpp` 의 `unknown_ioctl` 정책으로 갈림 — 코드의 몸이 없음).

#### (d) 소켓을 만들기에 성공하려면 — 최소 응답 (한 줄씩)

1. **NtCreateFile (`\Device\Afd\Endpoint`, EA `AfdOpenPacketXX`)**: `STATUS_SUCCESS` (0) 와 `*FileHandle` = 0 이 아닌 새 핸들. IoStatusBlock 은 안 읽는다. EA 의 계열·종류·프로토콜 (+0x20/+0x24/+0x28) 은 파일 객체에 적어 둔다 (이후 `0x1207b` 의 클래스 7·6, `0x12047` 문맥이 이 소켓 것).
2. **0x1207b** (프로세스 첫 소켓에서 클래스 7, 6 두 번): `STATUS_SUCCESS`, 출력 = 입력과 **같은 0x10 바이트 칸의 `+8` dword 에 `0x10000`**, Information 0x10. `0x103` 을 돌려주면 mswsock 이 이벤트를 기다리므로 바로 0 을 돌려준다. (0 을 쓰지 않고 성공만 돌려주면 창 크기가 0 으로 남아 매 소켓마다 다시 묻지만 소켓은 만들어진다.)
3. **0x12047**: `STATUS_SUCCESS`, 출력 안 씀, Information 0. 입력 바이트는 소켓별로 저장 (나중에 `0x12043` `AfdGetContext` 가 되돌려줌 — 이번 범위 밖).
   - 그리고 이 셋이 끝나면 `WPUCreateSocketHandle` (ws2_32 안, 유저 안에서만 끝남) 이 SOCKET 값을 만든다. 우리 에뮬레이터가 ws2_32 도 진짜 코드를 돌리는 구조라면 syscall 은 더 없음 `[추측]`.

---

### 2. 연결 (mswsock + afd.sys)

#### (a) `connect()` / `WSAConnect` 가 내는 IOCTL [정적]

`connect()` 와 `WSAConnect` 는 ws2_32 에서 provider 의 `WSPConnect` 로 가는 같은 길 `[추측: ws2_32 안 봄]`. `ConnectEx` 는 **다른 길** (`MSAFD_ConnectEx → SockDoConnectEx`, IOCTL `0x120c7`).

**`WSPConnect` (rva 0x13080)**: `+0x58/+0x5d` `SockDoConnect` 를 부르고, 0 이 아닌 값이면 `*lpErrno = 값` (`+0x6b`) + `-1` 반환 (`+0x6d`), 0 이면 0 반환 (`+0x87`).

**`SockDoConnect` (rva 0x13170) — 앞 검사와 그 오류** (모두 `[sock+0x18]` 상태 기준. 값은 WSA 번호):
- 소켓 못 찾음 → 10038 (`+0xd7 ~ +0xda`, `+0x53c`). 이미 연결 중 (`[sock+0xc8]≠0`) 이면 10037 (`+0x480`, 0x2735).
- 상태가 3 (연결됨) 이고 연결형이면 10056 (WSAEISCONN, `+0x5da`); 상태 2 (듣기) 나 알 수 없는 값 → 10022 (`+0x657`).
- 주소 길이 < helper 최소 길이 → 10014 (`+0x61a`); 주소 계열이 소켓 계열과 다름 → 10047 (`+0x195`).
- **상태 0 (아직 bind 안 함)**: helper 의 "와일드카드 주소" 함수 (`indirect:cfg:[RAX+0x88]`, `+0x277`) 로 와일드카드 sockaddr 를 만들고 `WSPBind` (`+0x2a2/+0x2a7`) 를 **먼저** 부른다. `SOCK_STREAM` + 프로토콜 6 + AF_INET(2)/AF_INET6(23) 이면 `WSPBind` 앞에 `WSPSetSockOpt(소켓, 0xffff, 0x3007, …)` 도 부른다 (`+0x4d7/+0x4dc`; 0x3007 = `SO_REUSE_UNICASTPORT` 로 보임 `[추측: 값만 확인]`).
- 마지막에 `SockDoConnectReal` (`+0x3f7/+0x3fc`).

**그래서 새 TCP 소켓의 `connect()` 는 afd 에 이 순서로 IOCTL 을 낸다** (`[정적]`, 갈래 조건은 위):

| 순서 | 코드 | 처리 함수 (afd) | 내는 줄 | 입력 모양 |
|---|---|---|---|---|
| 1 | `0x120bf` | `AfdTliIoControl` | `WSPSetSockOpt +0x1d6` (코드), `+0x1fd/+0x204` (호출) | 입력 0x20: `+0x00`=1, `+0x04`=레벨 0xffff, `+0x08`=옵션 0x3007, `+0x0c` 바이트=1, `+0x10`=값 포인터, `+0x18`=값 길이 (`WSPSetSockOpt +0x192 ~ +0x1c4`) |
| 2 | `0x12003` | `AfdBind` | `WSPBind +0x3f2/+0x3f9` (코드 `+0x3cf`) | 입력=출력 같은 버퍼. **TL 모드**: `+0x00` dword = 공유 방식 (0/1/2, `+0x2c0 ~ +0x2db`), `+0x04` 날 sockaddr (`+0x2e2 ~ +0x2f6`). 출력: 버퍼 +0 부터 날 sockaddr (고른 임시 포트 포함), 길이 = **IoStatus.Information** (`+0x45b`) → mswsock 이 `[sock+0xa8]` 로 복사 (`+0x4bf`) |
| 3 | `0x12047` | `AfdSetContext` | `WSPBind +0x4e4` (바로 앞 `+0x4da` 에서 상태=1) | 위 1(b) 와 같은 모양 |
| 4 | `0x12007` | `AfdConnect` | `SockDoConnectReal +0x463/+0x46a` (코드 `+0x43f`) | 아래 |

(`SockUpdateWindowSizes` 는 전송 이름이 비면 `0x12037` 은 안 내고, `[sock+0x45]` 의 0x40 / 0x80 비트가 켜졌을 때만 `SockSetInformation` (`0x1203b`, 클래스 6 / 7) 을 낸다 — `WSPBind +0x4f8`, `SockUpdateWindowSizes +0xcf ~ +0x10d`. 그 비트가 켜지는 조건은 안 봤다 `[추측: 앱이 SO_RCVBUF/SO_SNDBUF 를 바꿨을 때]`. 기본 TCP 는 안 낸다 `[추측]`.)

**`0x12007` 입력 (TL 모드 = 기본) — 칸 표** (`SockDoConnectReal` 이 만든 버퍼 `R13`, 스택 `[RSP+0xb0]` (≤0x40 바이트) 또는 힙):

| 오프셋 | 크기 | 뜻 | 근거 |
|---|---|---|---|
| +0x00 | 1 | `UseSAN` 표시 = `SockSanEnabled != 0` | `+0x1fc ~ +0x1ff` |
| +0x01 ~ +0x07 | 7 | 안 씀 | |
| +0x08 | 8 | 루트 엔드포인트 = 0 | `+0xf9 ~ +0xfc` |
| +0x10 | 8 | 연결 엔드포인트 (이 길에선 안 씀; 버퍼를 지우지 않아 쓰레기일 수 있음 `[추측]`) | afd 는 `[+0x08]` 이 0 이면 안 읽음 (`AfdConnect +0x462 TEST RCX,RCX; JZ`) |
| **+0x18** | **namelen** | **날 sockaddr** (sa_family 2바이트, 포트, 주소 …) — `connect(…, name, namelen)` 의 name 그대로 (`namelen` 은 helper 최대 길이로 잘림, `SockDoConnect +0x136 ~ +0x13a`) | `+0x104 ~ +0x10e` (`memcpy(R13+0x18, sockaddr, ESI)`) |

- **입력 길이 = 0x18 + namelen** (IPv4 `sockaddr_in` 이면 `0x28`) (`+0xa8 R15D=RCX+0x18`), **출력 버퍼 NULL, 길이 0**. 비동기 길이 변형 아님.
- **전송 이름이 있는 (TDI) 모드**일 때만 +0x18 이 TA 모양: `SockBuildTdiAddress` (rva 0x15844): `+0x00` dword = 1 (주소 개수), `+0x04` word = sockaddr 길이 − 2, `+0x06` ~ = sockaddr 통째 (`SockBuildTdiAddress +0x4 ~ +0x1e`). 입력 길이 = 0x18 + helper 의 TDI 주소 길이 (`+0x9e`: `[helper+0x2c]`). 기본 경로에선 안 쓴다.
- afd 검사 (`AfdConnect`, 64비트 프로세스, `+0x16e ~ +0x2b1`): 입력 길이 ≥ 0x18, 출력 길이는 0 이거나 ≥ 0x10 (1~15 면 0xC000000D), 엔드포인트 태그 확인; TL 모드면 입력 +0x18 의 `sa_family < 0x23` 이고 `입력길이 − 0x18 ≥ SOCKADDR_SIZE(sa_family)` (아니면 `0xC0000141` = STATUS_INVALID_ADDRESS, `AfdConnect +0x4fd ~ +0x52b`).
- `WSAConnect` 의 `lpCallerData`/`lpCalleeData` 가 있으면 앞서 `SockSetConnectData` 로 `0x1204b`/`0x1206b` 가 먼저 나간다 (`SockDoConnect +0x2e9 ~ +0x3c8`). `connect()` 는 NULL 이라 안 탄다 `[추측]`.
- **`ConnectEx`**: `SockDoConnectEx +0xee/+0xf5` 에서 `0x120c7` (`AfdSuperConnect`). 입력: `+0x00` UseSAN 바이트, `+0x04` TA (`SockBuildTdiAddress`: `+0x04` count=1, `+0x08` 길이−2, `+0x0a` sockaddr). 입력 길이 = 주소길이 + 0xa. 보낼 데이터 (`lpSendBuffer`) 는 **출력 버퍼** 칸으로 넘긴다 (`+0xc6 ~ +0xcf`).

**지금 우리**: **전부 없음**. afd 코드 몸이 없으므로 위 네 코드 (`0x120bf`, `0x12003`, `0x12047`, `0x12007`) 가 필요. 입력 모양은 위 칸 표대로 읽으면 된다.

#### (b) 갈 길 없는 PC 에서 afd 가 돌려줄 status [정적 + 추측]

**afd 는 값을 정하지 않고 넘긴다. 아래 계층 (tcpip 의 TL 공급자) 이 정함.**
- 연결 요청을 받는 함수: `afd!AfdConnect` (rva 0x3c1a0). 즉시 처리 표가 아니라 IRP 로 처리되는 칸이고, `AfdIrpCallDispatch[1]` (0x12007) 와 `[0x2e]` (0x120bb) 두 칸이 같은 함수.
- 길을 TL 공급자에 맡기는 줄: `AfdConnect +0xb59` 가 `AfdTlClientConnectDispatch` 표를 고르고 (`_refs/AfdTlClientConnectDispatch.tsv`), `+0xb01/+0xb08` 에서 **완료 함수 `AfdTLConnectComplete` 주소를 요청 구조체에 넣고**, `+0xbaf/+0xbb4` 에서 TL 표의 `+0x28` 칸 (`indirect:cfg:[RAX+0x28]`) 을 부른다. 반환값을 `R14D` 에 받음 (`+0xbb4 MOV R14D,EAX`). 이 칸의 실제 함수는 tcpip.sys 쪽 `[추측]` (이름만, 본문은 범위 밖).
- 길이 없는 경우:
  - 바로 실패 (PENDING 아님): `+0xbf0 CMP R14D,0x103` 이 아니면 `+0xc0f/+0xc14` 에서 `AfdTLConnectComplete(IRP, status=R14D …)` 를 afd 가 직접 부른다.
  - PENDING: `+0xcab/+0xcb0 AfdTLPendRequest` 로 IRP 를 보류하고, 나중에 TL 이 등록해 둔 `AfdTLConnectComplete` 를 부른다.
  - 어느 쪽이든 **`AfdTLConnectComplete +0xe4 MOV [Irp+0x30],EBP`** (`EBP = EDX = 받은 status`) 가 IRP 의 IoStatus.Status 를 정한다. 실패 갈래 (`+0x85 JS`) 도 `+0xe1` 로 합쳐져서 같은 줄을 지난다 (`+0x1a4`, `+0x1c6`, `+0x1d7`, `+0x280`, `+0x2b6` 의 `JMP/JG → +0xe1`). 실패면 엔드포인트 태그를 `0xAFD0` (열림) 로 되돌린다 (`.c` 실패 분기).
- afd 자신이 정하는 status (경로와 무관): 입력 짧음·형식 틀림 `0xC000000D` (`+0x95`, `+0x31a`, `+0x483`, `+0x76e`, `+0xd53`), 주소 길이 틀림 `0xC0000141` (`+0x503`, `+0x526`), 핸들 형식 틀림 `0xC0000008` (`+0x2a1`, `+0x5b3`, `+0xb8c`), 메모리 부족 `0xC000009A` (`+0x4c9`, `+0x756`), **`0xC00000FA`** (`+0x3ae`: `AfdSanServiceHelper` 가 있고 `UseSAN` 이 꺼진 TL 소켓일 때 — mswsock 이 이 값을 특별 취급해 SAN 초기화 후 다시 시도, `SockDoConnectReal +0x557 ~ +0x5f4`. 일반 PC 에선 `AfdSanServiceHelper == NULL` 이라 안 탄다 `[추측]`).
- **`[추측]` 후보** (tcpip 가 정함, 이번에 안 봄): 어댑터 자체가 없음 / 경로 없음 → `0xC000023C` (NETWORK_UNREACHABLE) 즉시; 같은 서브넷인데 상대가 안 응답 → SYN 재전송 후 `0xC00000B5` (IO_TIMEOUT) 또는 `0xC000023D` (HOST_UNREACHABLE). (참고: `0xC0000236` CONNECTION_REFUSED 는 "길은 있는데 상대가 거부" 라 길 없음과 다름.) 이름은 ntstatus.h 기억 기준, 숫자만 `[정적]` (아래 표가 코드의 값).

**지금 우리**: "항상 실패" 를 정직하게 하려면 `0x12007` 에 **`0xC000023C` (또는 `0xC000023D`)** 를 즉시 (PENDING 없이) 돌려주면 mswsock 이 바로 다음 칸 (c) 대로 번역한다. 단 위 `[추측]` 은 동적으로 확인 (아래).

#### (c) status → `WSAGetLastError` 값 바꾸기 표 [정적]

함수: **`mswsock!NtStatusToSocketError` (rva 0xbca0, 645 바이트)**. 점프 표 3개를 `.dll` 에서 읽어 풀었다 (표 위치 `+0x66 ~ +0x7a` (`0xbf6c`/`0xbf44`), `+0xaf ~ +0xca` (`0xc034`/`0xbffc`), `+0x12f ~ +0x14a` (`0xc120`/`0xc0f8`)). 첫 줄 `+0x8 RtlNtStatusToDosError` 를 부르지만 **반환값은 안 쓴다**.

특수·범위 처리 (줄은 `NtStatusToSocketError`):
- `0xC00000D0` → 10035 WSAEWOULDBLOCK (`+0x14 ~ +0x1c`). `0xC0000120` (STATUS_CANCELLED) → 10004 WSAEINTR (`+0x8c`, `+0x229`). `0xC0000350` → 10064 (`+0x48`, `+0x247`). `0xC000A012` → 10052, `0xC000A013` → 10047 (`+0xdc ~ +0xea`).
- `0x00000000` 과 그 밖의 양수 → 0 (`+0xf0`, `+0x1b6`~`+0x1be`). `0x102` (STATUS_TIMEOUT) → 10060 (`+0xf8 ~ +0x104`). `0x103` (PENDING) → **997 (ERROR_IO_PENDING)** (`+0x1b0`, `+0x28e`). `0x80000005` → 10040 WSAEMSGSIZE (`+0x1df ~ +0x1ec`).
- 표에 없는 음수 (그 밖의 `0xC…`) 와 `0xC0000001` → **10022 WSAEINVAL** (`+0x1d1`, `+0x1dd`).

연결 관련 (표 3 개에서 푼 값, 10진은 WSA 번호):

| NTSTATUS | 이름 (ntstatus.h `[추측: 이름만]`) | WSA (16진 / 10진) |
|---|---|---|
| `0xC000023C` | NETWORK_UNREACHABLE | `0x2743` / **10051** WSAENETUNREACH |
| `0xC000023E` | PROTOCOL_UNREACHABLE | `0x2743` / 10051 |
| `0xC00000BE` | BAD_NETWORK_PATH | `0x2743` / 10051 |
| `0xC000023D` | HOST_UNREACHABLE | `0x2751` / **10065** WSAEHOSTUNREACH |
| `0xC0000350` | (HOST_DOWN 류) | `0x2750` / 10064 WSAEHOSTDOWN |
| `0xC0000236` | CONNECTION_REFUSED | `0x274d` / **10061** WSAECONNREFUSED (`+0x11c → +0x180`) |
| `0xC00000BC` | REMOTE_NOT_LISTENING | `0x274d` / 10061 |
| `0xC00000B5` | IO_TIMEOUT | `0x274c` / **10060** WSAETIMEDOUT |
| `0xC000013F` | (연결 시간 초과 류) | `0x274c` / 10060 |
| `0xC000023F` | PORT_UNREACHABLE | `0x2746` / 10054 WSAECONNRESET |
| `0xC0000241` | CONNECTION_ABORTED | `0x2745` / 10053 WSAECONNABORTED |
| `0xC0000240` | REQUEST_ABORTED | `0x2714` / 10004 WSAEINTR |
| `0xC000023B` | CONNECTION_ACTIVE | `0x2748` / 10056 WSAEISCONN |
| `0xC0000141` | INVALID_ADDRESS | `0x2741` / 10049 WSAEADDRNOTAVAIL |
| `0xC0000238 .. 3A` | (주소 연관 류) | 10022 (기본) |
| `0xC000000D` | INVALID_PARAMETER | **표에 없음 → 10022** WSAEINVAL |
| `0xC0000008` | INVALID_HANDLE | 10038 WSAENOTSOCK |
| `0xC000009A` | INSUFFICIENT_RESOURCES | 10055 WSAENOBUFS |
| `0xC0000034`, `0xC000000E`, `0xC000003A` | OBJECT_NAME_NOT_FOUND 등 | 10050 WSAENETDOWN |

(전체 표는 위 3개 점프 표가 근거. 모든 값은 mswsock 의 코드에서 나온 것이고 이름 열만 `[추측]`.)

연결에서 이 바꾸기를 부르는 줄: 블로킹 길 `SockDoConnectReal +0x60d/+0x614` (`CMP ESI,0xc00000fa` 가 아니면). 성공 (status ≥ 0) 이면 안 바꾸고 `SockPostProcessConnect` (`+0x618/+0x620`). 바꾼 값은 `SockDoConnect → WSPConnect` 가 `*lpErrno` 로 내보낸다.

**지금 우리**: 없음 (표를 쓸 일은 mswsock 이 하니 에뮬레이터에선 **status 만 바르게 주면 된다**).

#### (d) 블로킹 길과 오버랩 (완료 포트) 길 [정적]

**블로킹 `connect()` (`SockDoConnectReal`, 비동기가 아닌 소켓)** — 갈래 조건: `[sock+0x44] & 0x40` (비블로킹 표시) 이 **꺼져 있거나** `[sock+0x50] & 1` (연결형이 아님) 이면 이 길 (`+0x258 ~ +0x26c`).
1. `+0x420 MOV [RSP+0x98],0x103` 로 IoStatusBlock 을 PENDING 으로 초기화.
2. `+0x463/+0x46a NtDeviceIoControlFile`: FileHandle `[sock+8]`, **Event = 스레드 이벤트 `[TLS+0x10]`**, Apc 둘 0, 코드 `0x12007`, 입력 = 위 버퍼, **출력 NULL/0**.
3. `+0x471 CMP EAX,0x103`:
   - **PENDING 아님** → `ESI = 반환 status` 를 그대로 쓴다 (IoStatusBlock 안 읽음, `+0x46f MOV ESI,EAX`).
   - **PENDING** → 소켓 임계 구역을 놓고 (`+0x47f LeaveCriticalSection`) `SockWaitForSingleObject(이벤트, 소켓핸들, 2, 4)` (`+0x49e/+0x4a3`), 돌아와서 **`+0x4a3 MOV ESI,[RSP+0x98]`** (IoStatusBlock.Status) 를 읽고 다시 임계 구역 (`+0x4b1`).
4. `SockWaitForSingleObject` (rva 0x93c0) 안: **처음에는 짧게** `NtWaitForSingleObject(이벤트, 경고 가능 TRUE, 상대 시간 −0x186a0 (= 10ms))` (`+0x1bd ~ +0x1c6`, `SockSkipInitialShortWait == 0` 일 때, `+0x52 ~ +0x63`); 신호 받으면 성공 (1) 반환 (`+0x1d2 ~ +0x1da`). 아니면 (시간 초과 0x102 등) 긴 길로: 블로킹 후크 (`SockUpcallTable +0x40` 칸, `WPUQueryBlockingCallback` 류 `[추측]`) 부르고 `NtWaitForSingleObject(…, TRUE, 소켓 시간 제한 `[sock+0x38]`×0x2710 또는 NULL (무한))` (`+0x14b ~ +0x153`, `+0x206 ~ +0x226`).
5. `ESI` 로: 소켓 상태 4 (닫힘) → 10038 (`+0x4c0 ~ +0x4c3`, `+0x68c`); `0xC00000FA` → SAN 길 (`+0x4c9 → +0x54f`); `ESI ≥ 0` → `SockPostProcessConnect`; 음수 → helper 알림 (`[helper+0x68]`, 알림 0x100) 이 재시도 코드 `0x2afa` (11002) 를 주면 같은 IOCTL 을 다시 (`+0x4d5 ~ +0x549`) — 내장 `Tcpip4_WSHNotify` 는 항상 0 이라 안 탄다 `[추측]` — 아니면 `NtStatusToSocketError(ESI)` (`+0x60d`).

**비블로킹 소켓 (`[sock+0x44] & 0x40` 켜짐, 연결형)**: `SockCheckAndInitAsyncConnectHelper` (`+0x26c`, `\Device\Afd\AsyncConnectHlp` 열기: `.rdata 0x568e0`) 를 거쳐 **같은 `0x12007` 를 `SockAsyncConnectHelperHandle` 핸들에** 낸다 (`+0x34c/+0x353`, 입력 = `[RDI+0x28]` 에 복사한 버퍼, 출력 블록 `[RDI+0x10]`, ApcContext = `RDI`). **status ≥ 0 이면 (PENDING 포함) IoStatusBlock 을 읽지 않고** `+0x5f9 ~ +0x608` 에서 `[sock+0xc8]=RDI` 로 표시하고 **10035 (WSAEWOULDBLOCK, `0x2733`) 를 반환**. 완료는 mswsock 의 비동기 스레드 (`SockAsyncThread`, 완료 포트 `SockAsyncQueuePort`) 가 처리한다. test.exe 가 쓰는 길 아님.

**오버랩 (`ConnectEx`, `MSAFD_ConnectEx → SockDoConnectEx`)**: `SockDoConnectEx +0xe8 ~ +0xee`:
- IoStatusBlock = 호출자의 **`OVERLAPPED` 자체** (`[RSP+0x20]=RSI`; `+0x4d [RSI]=0x103` 로 PENDING 으로 시작).
- Event = `OVERLAPPED.hEvent` (`R14=[RSI+0x18]`). **ApcContext = `hEvent` 의 맨 아래 비트가 0 이면 `OVERLAPPED*` (완료 포트 쪽으로 도장 찍힘), 1 이면 0** (`+0xa8 ~ +0xb2`).
- 코드 `0x120c7`, 입력 위 모양, 출력 = 보낼 데이터 (`+0xc6 ~ +0xcf`).
- **반환 status 를 그대로 호출자에게** (`+0xfa MOV EDI,EAX`). PENDING 이면 `MSAFD_ConnectEx` 가 `NtStatusToSocketError(0x103)` = **997 (ERROR_IO_PENDING)** 로 바꾸고 (`MSAFD_ConnectEx +0xe8/+0xed`), 호출자는 완료 포트·이벤트로 기다린다. mswsock 은 기다리지 않는다 (`NtWaitForSingleObject` 없음).
- 완료 포트 쪽 mswsock 이 보는 건 여기까지 (완료 패킷은 커널이 `ApcContext` 를 담아 보낸다).

**afd 쪽 완료**: afd 는 IRP 를 `AfdTLPendRequest` 로 보류했다가 TL 이 `AfdTLConnectComplete` 를 부르면 (`+0xe4` 로 status 기록, `+0xe8/+0xed AfdTLCompleteRequest`, 이어서 `AfdTLConnectComplete2 +0x72 ~ +0xda` 에서 APC `KeInitializeApc/KeInsertQueueApc` 와 `IofCompleteRequest`) 끝낸다. `AfdRestartConnect` (rva 0x2fe30) 는 TDI 모드 (전송 이름 있음) 용 완료 함수 (`AfdConnect +0xccf ~ +0xd44`: IRP 스택에 `0x14002fe30` 을 완료 함수로 걸고 `IofCallDriver`).

**지금 우리**: PENDING 흉내는 필요 없다. **즉시 실패 status 를 돌려주면** 블로킹 길은 `ESI = EAX` 로 끝난다 (이벤트·대기·IoStatusBlock 안 씀). 다만 afd 핸들은 비동기 (CreateOptions 0) 라서 진짜 커널은 호출 전 이벤트를 비우고 (`IopXxxControlFile +0x2db/+0x2e0` `KeClearEvent`) 끝나면 세운다 (`+0xbe4/+0xbe9 KeSetEvent`) — 이 이벤트는 같은 스레드의 다음 대기에도 쓰이므로 **이벤트 핸들이 주어지면 호출 끝에 세워 두거나 비워 두는 규칙을 일관되게** 해야 한다 `[추측: 어느 갈래에서 세우는지는 이번에 안 읽음 — 동적 확인]`.

---

### 3. 어댑터 묻기 (nsi.dll → nsiproxy.sys) — 급함 낮음, 할 수 있는 만큼

**열기 (`nsi!NsiOpenDevice`, rva 0x1a70)**: 전역 `g_NsiAsyncDeviceHandle` 이 `-1` 이면 `CreateFileW(L"\\.\Nsi", 접근 0, 공유 3, 보안 NULL, OPEN_EXISTING (3), 속성 `0x40000000` = FILE_FLAG_OVERLAPPED, 템플릿 NULL)` (`+0x3f/+0x46`; 인자 `+0x19 ~ +0x3b`, 문자열 `.rdata 0x42f0`). 성공하면 `CMPXCHG` 로 전역에 저장, 이미 있으면 새 핸들 닫음 (`+0x5b ~ +0x6d`). NT 이름으로는 `\??\Nsi` 로 바뀐다 (kernelbase 의 CreateFile 변환 — 이미 분석된 `kernelbase`/`kernel32` 몫) `[추측: 변환 줄은 이번에 안 봄]`. 열기에 EA 없음.

각 `Nsi…` 함수는 같은 틀이다: `NsiOpenDevice` → `CreateEventA(NULL, FALSE, FALSE, NULL)` → `NtDeviceIoControlFile(g_NsiAsyncDeviceHandle, 이벤트, 0, 0, &IoStatusBlock(스택), 코드, 입력=출력=호출자 구조체, …)` → status `0x103` 이면 `NtWaitForSingleObject(이벤트, FALSE, NULL)` 후 `IoStatusBlock.Status` 사용 → `CloseHandle(이벤트)` (예: `NsiGetParameterEx +0x26 ~ +0x131`).

| 코드 | 쓰는 함수 | 입력 길이 | 출력 길이 | 근거 줄 |
|---|---|---|---|---|
| `0x120007` | `NsiGetParameter(Ex)` | **0x50** | **호출자 구조체 `+0x48` 의 값** (`MOV EBX,[RSI+0x48]` → `[RSP+0x48]`) | `NsiGetParameterEx +0x26`, `+0x59`, `+0x68`, `+0x78`, `+0x85/+0x8c` |
| `0x12000f` | `NsiGetAllParameters(Ex)` | **0x68** | **0x68** | `NsiGetAllParametersEx +0x52`, `+0x65`, `+0x75`, `+0x82/+0x89` |
| `0x12001b` | `NsiEnumerateObjectsAllParameters(Ex)`, **`NsiAllocateAndGetTable`** | **0x70** | **0x70** | `NsiEnumerateObjectsAllParametersEx +0x51`, `+0x64`, `+0x74`, `+0x81/+0x88`; `NsiAllocateAndGetTable +0xe1`, `+0xf4`, `+0x10c`, `+0x119/+0x120` (첫 호출), `+0x35e`, `+0x36b/+0x372` (둘째 호출) |

세 코드 모두 입력 버퍼와 출력 버퍼가 **같은 호출자 구조체**다. 구조체 칸 뜻은 PDB 에 없다. 코드로 확인되는 것만:
- `0x12001b` 의 0x70 구조체 (`NsiAllocateAndGetTable +0x5c ~ +0x9a` 가 채움 — 구조체 시작 = `RBP-0x20`; nsiproxy 가 읽음 `NsippEnumerateObjectsAllParameters +0xd0 ~ +0x27d`): `+0x00 ~ +0x0f` 0, `+0x10` = 둘째 인자 포인터 (`R12`, `+0x60`), `+0x18` = 셋째 인자 dword (`R13D`, `+0x68`), `+0x20` = 첫째 인자 dword (`+0x90`), `+0x24` = 1 (`+0x93`), 나머지 0. nsiproxy 는 `+0x08`, `+0x28`, `+0x38` 칸 주소를 검사·할당 함수에 넘기고 (`+0x10e ~ +0x1e6`), 결과로 `+0x10`/`+0x28`/`+0x30`/`+0x38`/`+0x40`/`+0x48`/`+0x50`/`+0x58`/`+0x60` 칸을 채운다 (`+0x1fe ~ +0x270`) — `+0x28/+0x30`, `+0x38/+0x40`, `+0x48/+0x50`, `+0x58/+0x60` 은 **(버퍼 포인터, 칸 크기)** 네 쌍처럼 보인다 `[추측]`. **`+0x68` = 개수 (dword)** (`NsiAllocateAndGetTable +0x169 MOV ECX,[RBP+0x48]`: 첫 호출이 돌려준 개수; nsiproxy `+0x285/+0x296` 가 되돌려 씀). 칸 뜻 이름은 전부 `[추측]`.
- **길이 먼저 묻기 규칙**: `NsiAllocateAndGetTable` 은 먼저 `0x12001b` 를 **버퍼 포인터를 모두 0 으로** 한 번 내고 (`+0x119`), 돌려받은 개수 (`+0x68`) 에 **개수/8 (최소 10) 여유를 더해** (`+0x16c ~ +0x185`) 힙 (`g_NsiHeap`) 에 칸별 배열을 잡고 (`+0x1b8 ~ +0x20b`), **둘째 `0x12001b` (`+0x36b`)** 로 채운다. 전체를 최대 3번 반복 (`+0x48 CMP ECX,3`).
- status 처리: nsi.dll 은 `status < 0` 이거나 **`0x105`** (정보성 "더 있음" 코드로 보임) 이면 `RtlNtStatusToDosError(status)` 값을 오류로 돌려준다 (`+0x143 ~ +0x15a` 등; 예 `NsiGetParameterEx +0xa9 ~ +0xe6`). `NsiGetParameterEx` 는 거기에 더해 **`IoStatusBlock.Information` (`[RSP+0x58]`) 을 호출자 구조체 `+0x48` 에 써 준다** (`+0xed ~ +0xf1`, `+0xb5 ~ +0xb9`) — "작은 버퍼면 필요한 길이를 `+0x48` 칸으로 돌려준다" 는 규칙. 작은 버퍼일 때 afd 의 값 아닌 **무슨 status 가 오는지는 커널 NSI 층이 정함** (nsiproxy 코드에 `0x80000005`/`0x105`/`0xC0000023` 상수 없음, `grep` 결과) → `[공백]` 동적.
- nsiproxy: `NsippDispatchDeviceControl` (rva 0x2b40) 가 `+0x3a CMP EAX,0x12000f` → `NsippGetAllParameters` (`+0x67/+0x6c`), `+0x41 CMP EAX,0x12001b` → `NsippEnumerateObjectsAllParameters` (`+0x54/+0x59`), `+0x88 CMP EAX,0x120007` → `NsippGetParameter` (`+0x9b/+0xa0`), 나머지는 `0x12000b + n` 점프 표로 변경 알림·영구 매개변수 (`+0xa2 ~ +0x19d`), 표에 없으면 `0xC0000002` (`+0x1b0`). 모두 `METHOD_NEITHER` (사용자 버퍼 `[IrpSp+0x20]` 를 직접 읽음). 입력 길이 `< 0x70` 이면 `0xC000000D` (`NsippEnumerateObjectsAllParameters +0xd0`, `+0x53a`), 정상이면 `+0x276/+0x27d` 에서 **`NsiEnumerateObjectsAllParametersEx` (커널 NSI 모듈이 내보낸 함수, nsiproxy 의 import)** 에 넘기고 `Information = 0x70` (`+0x2b9`).
- **루프백만 있는 PC 의 대답이 빈 표인지**: nsiproxy 는 받은 요청을 NSI 커널 모듈 → 등록된 NPI 공급자 (tcpip 등) 로 **그대로 넘긴다** → 공급자가 정함 `[추측]` + 동적.

**지금 우리**: 없음 (장치 `\Device\Nsi` 도 모름). 급하지 않음. 필요해지면 `0x12001b` 첫 호출 (포인터 전부 0) 에 "개수 `+0x68` = 0, status 0" 를 주는 것이 가장 단순한 "빈 표" 이다 `[추측: 개수 0 이면 둘째 호출을 안 하는지는 `NsiAllocateAndGetTable +0x16c ~ +0x1d3` 의 `TEST … JZ` 로 확인 필요 — 개수 0 이면 `MOV EDX,10` 최소 여유로 10칸 잡고 둘째 호출을 내는 것으로 읽힘]`.

---

## 참고: afd 제어 코드 → 처리 함수 (`AfdIoctlTable`/`AfdIrpCallDispatch`/`AfdImmediateCallDispatch` 에서 읽음, 74칸)

`AfdDispatchDeviceControl` (rva 0x14cc0): `+0x37 ~ +0x3f` 에서 코드의 `(>>2)&0x3ff` 가 `0x4a` 이상이면 거절, `+0x4f` 에서 표의 코드와 같은지 확인, 다르거나 칸이 NULL 이면 **`0xC0000010` (STATUS_INVALID_DEVICE_REQUEST)** (`+0x18d`). 칸이 `AfdDispatchImmediateIrp` (rva 0x468b0) 면 `AfdImmediateCallDispatch` (`+0x17` `0x781e0`) 로 한 번 더 찾아 `(파일 객체, 코드, 요청 모드, 입력 포인터, 입력 길이, 출력 포인터, 출력 길이, &Information)` 로 부른다 (`+0x5e ~ +0x8f`: 입력 = `IrpSp+0x20` Type3InputBuffer, 출력 = `Irp+0x70` UserBuffer). **이 표의 모든 코드는 하위 2비트가 3 (`METHOD_NEITHER`)** 이라 입력·출력이 사용자 주소 그대로다 — 에뮬레이터도 사용자 메모리를 직접 읽고 쓴다. 코드 이름은 PDB 에 없음.

```
0x12003 AfdBind            0x12007 AfdConnect         0x1200b AfdStartListen    0x1200c AfdWaitForListen
0x12010 AfdAccept          0x12017 AfdReceive         0x1201b AfdReceiveDatagram 0x1201f AfdSend
0x12023 AfdSendDatagram    0x12024 AfdPoll
즉시 처리 (AfdImmediateCallDispatch):
0x1202b AfdPartialDisconnect   0x12033 AfdQueryReceiveInformation   0x12037 AfdQueryHandles
0x1203b AfdSetInformation      0x1203f AfdGetRemoteAddress          0x12043 AfdGetContext
0x12047 AfdSetContext          0x1204b..0x12057, 0x1206b..0x12077 AfdSetConnectData
0x1205b..0x12067 AfdGetConnectData   0x1207b AfdGetInformation
0x12087 AfdEventSelect         0x1208b AfdEnumNetworkEvents         0x120a7 AfdGetUnacceptedConnectData
0x120ab AfdRoutingInterfaceQuery  0x120b3 AfdAddressListQuery       0x12117 AfdSqm   0x12127 AfdNotifySock
0x120d7..0x120df, 0x120e7..0x120f7, 0x120ff..0x1210b  AfdSanFast* (SAN)
그 밖 (IRP 로 처리):
0x1202f AfdGetAddress      0x1207f AfdTransmitFile    0x12083 AfdSuperAccept   0x1208c AfdDeferAccept
0x12090 AfdWaitForListen   0x12094 AfdSetQos          0x12098 AfdGetQos        0x1209f AfdNoOperation
0x120a0 AfdValidateGroup   0x120ac AfdRoutingInterfaceChange   0x120b4 AfdAddressListChange
0x120bb AfdConnect         0x120bf AfdTliIoControl    0x120c3 AfdTransmitPackets  0x120c7 AfdSuperConnect
0x120cb AfdSuperDisconnect 0x120cf AfdReceiveDatagram 0x120d3 AfdSendMessageDispatch
0x120e2 AfdSanConnectHandler  0x120fb AfdSanAcquireContext  0x1210c AfdSanAddrListChange
0x12113 AfdUnBindSocket    0x1211b AfdRio             0x1211f AfdSocketTransferBegin  0x12123 AfdSocketTransferEnd
```

---

## 동적으로 확인해야 할 것 (선 뽑은 VM, `socket` → `connect` 한 번)

정적으로 못 닫은 것 — 한 번의 IOCTL 입출력 바이트로 닫힐 것 위주:

1. **NtCreateFile EA 바이트 (0x39)**: EaValueLength = `0x1e`, EndpointFlags (+0x18) 가 TCP 에서 0, UDP 에서 0x11 인지, 전송 이름 길이 (+0x2c) 가 0 인지 (TL 모드 확인). CreateOptions 가 0 (비동기) 인지. NtCreateFile 의 IoStatus.Information.
2. **0x1207b 출력 +8**: 클래스 7, 6 두 번이 실제로 `0x10000` 을 주는지 (레지스트리 덮어쓰기 영향), 그리고 이 둘이 프로세스에서 **첫 소켓에서만** 나오는지.
3. **0x12047 입력 총 길이** (`EBP`) 와 helper 문맥 길이 (`+0x78` 칸), 출력 포인터의 오프셋.
4. **connect 앞 IOCTL 순서**: `0x120bf` → `0x12003` → `0x12047` → `0x12007` 가 실제로 그 순서인지, 각 입력 바이트 (특히 `0x12003` 의 `+0x00` 공유 방식 값, 와일드카드 sockaddr), `0x12003` 출력의 임시 포트와 `IoStatus.Information` (= sockaddr 길이).
5. **`0x12007` 의 status**: 어댑터 없음 / 선 뽑힘 두 경우에 (a) 즉시 실패인지 PENDING 후 실패인지, (b) status 가 `0xC000023C` / `0xC000023D` / `0xC00000B5` 중 무엇인지, (c) 완료까지 걸린 시간, (d) 그 뒤 `WSAGetLastError`. `0xC00000FA` 가 안 나오는지.
6. **이벤트**: 같은 IOCTL 에서 이벤트가 호출 직후 신호 상태인지 (동기 완료인 `0x1207b`/`0x12047` 도).
7. nsi: `0x12001b` 첫 호출의 입출력 0x70 바이트와 `+0x68` 개수, 루프백만 있는 PC 에서 개수 0 인지.

**추천 멈춤점** (WinDbg `caller+0x..` = `_calls.tsv` 의 `ret_off`):
- `mswsock!SockSocket+0x598` (NtCreateFile 복귀: EAX = status, `[RSP+0xc0]` = 핸들). 그 전 호출 시점의 EA 는 `NtCreateFile` 입구에서 스택 `[RSP+0x50]` (EaBuffer), `[RSP+0x58]` (EaLength) (호출 직전 `[RSP+0x48]/[RSP+0x50]` 에서 복귀 주소 8바이트만큼 밀림).
- `mswsock!SockGetInformation+0xc2`, `mswsock!SockSetHandleContext+0x277` (IOCTL 복귀 직후: EAX = status).
- `mswsock!WSPBind+0x3f9`, `mswsock!SockDoConnectReal+0x46a` (EAX = connect IOCTL 의 즉시 status), `mswsock!SockDoConnectReal+0x4a3` (ESI = IoStatusBlock.Status), `mswsock!SockDoConnectReal+0x614` (EAX = WSA 번호).
- `afd!AfdConnect+0xbb4` (R14D = TL 가 준 status), `afd!AfdTLConnectComplete` 입구 (EDX = status), `afd!AfdCreate` 입구 (`[IRP+0x18]` = EA 복사본).

## 다음 요청 후보

1. **ws2_32** 다리: `WSASocketW`·`socket`·`connect`·`WSAConnect`·`WSAGetLastError` 가 provider 로 넘기는 인자 (특히 `dwFlags`, `lpProtocolInfo` 카탈로그 읽기) 와 `*lpErrno` → `SetLastError` 옮기는 줄. (`ms/sym/ws2_32.dll.pdb` 있음, analyze 아직.)
2. **afd 입출력 칸 마저**: `AfdBind` (`0x12003`, TL 모드 출력 쓰기), `AfdTliIoControl` (`0x120bf`), `AfdSetInformation` (`0x1203b`), `AfdQueryHandles` (`0x12037`), `AfdSuperConnect` (`0x120c7`), `AfdGetAddress`/`AfdGetRemoteAddress` (이름 얻기).
3. **mswsock 시작 쪽**: `WSPStartup` / `SockEnterApiSlow` / `SockLoadTransportList` (NtCreateFile 앞에 나오는 레지스트리·이벤트 syscall) — 에뮬레이터 로그의 NtCreateFile "앞" 을 설명하려면 필요.
4. **tcpip.sys** 의 TL connect/bind 진입점 (`AfdTlClientConnectDispatch` 표 `+0x28` 칸의 실제 함수) 에서 "경로 없음" status 를 정하는 줄. 본문이 커서 동적 (위 5번) 으로 먼저 닫고 필요할 때.
5. **nsi**: 0x70 구조체 칸 뜻 (`NsiAllocateAndGetTable` 의 호출자: iphlpapi `GetAdaptersAddresses` 등), 커널 NSI 모듈 (`nsi.sys`) 의 개수 0 응답.
6. **IopXxxControlFile 이벤트 규칙**: 비동기 핸들에 이벤트를 줬을 때 동기 완료에서도 세우는지 (`+0xbe4` 의 갈래) — 에뮬레이터 `NtDeviceIoControlFile` 일반 규칙.

## 판정

**분석성공.** 네 모듈 PDB 받아 analyze, 질문 1·2·3 모두 코드 줄로 답했다. 정적으로 못 닫은 것 (tcpip 가 정하는 "길 없음" status 값, 루프백 응답, ws2_32 쪽 `WSAGetLastError` 옮김, 이벤트 세우는 갈래) 은 `[추측]`/`[공백]` 으로 표시하고 위 "동적으로 확인할 것" 으로 넘겼다.

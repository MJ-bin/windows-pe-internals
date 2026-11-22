# 2026-11-22 (작성일, 정적 분석 2026-11-16) run124 — 소켓 장치 AFD (Q217, 정적만): `socket()` 은 `\Device\Afd\Endpoint` 를 EA `AfdOpenPacketXX` (0x39 바이트) 로 열고 `0x1207b` ×2 · `0x12047` 을 낸다. `connect()` 는 `0x120bf` → `0x12003` → `0x12047` → `0x12007`. "길 없음" status 는 afd 가 아니라 아래 계층 (tcpip) 이 정하고, mswsock `NtStatusToSocketError` 가 WSA 번호로 바꾼다

> **일부 닫힘 · 고쳐 읽기 (run126, sast/123 · 124 — Q219)**: 아래 "아직 확인 필요" 정적 ② 중 `AfdBind` (TL 출력 — 성공은 `0x103`, `AfdTLBindGetAddrComplete` 가 출력 `+0` 에 로컬 sockaddr · Information = 길이, 임시 포트는 TL) · `AfdTliIoControl` (afd 는 출력에 안 씀, 값은 TL 몫) 과 정적 ⑥ `IopXxxControlFile` 이벤트 규칙은 **닫혔다**. "PE_emulator 쪽" 의 "끝나면 세운다 (`+0xbe4/+0xbe9 KeSetEvent`) `[추측]`" 은 **빠른 길 (afd 즉시 처리) 줄**이다 — IRP 길은 `IopCompleteRequest +0x3d5` 가 세우고, **오류 (`0xC…`) + PENDING 없음이면 IOSB 도 이벤트도 안 건드린다.** 5절 `SOCKADDR_SIZE(family)` 는 2 → 16 · `0x17` → 28. `runs/03_sprint/run126-afd-addrlist.md` 7 ~ 11절 · "정정".

> **일부 닫힘 (run128, sast/126 — Q221)**: 9절 `0x70` 구조체의 칸 순서는 이제 `[정적]` — `+0x10` 모듈 ID 포인터 · `+0x18` 객체 번호 · `+0x20` 첫 인자 · `+0x24` 둘째 인자 (`NsiAllocateAndGetTable` 은 늘 1) · `+0x28 ~ +0x60` (버퍼 포인터, 1개당 크기) 네 쌍 · `+0x68` 개수 (`NsiAllocateAndGetTable` 에선 둘째 인자 = 모듈 ID, 셋째 = 객체라 9절 글과 어긋나지 않음). 네 쌍의 이름 (키 · RW · ROD · ROS) 만 `[추측]`. "PE_emulator 쪽" nsi 줄의 "개수 0 이어도 최소 10칸 잡고 둘째 호출" 도 `[정적]` (`NsiAllocateAndGetTable +0x16c ~ +0x185` `max(개수/8, 10)`). "아직 확인 필요" 정적 5 는 칸 뜻 · 부르는 쪽 닫힘, 커널 `nsi.sys` 개수 0 대답은 남음. `runs/03_sprint/run128-nsi-callers.md` 6절 · "정정".

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 mswsock · ws2_32 를 안 올린다 — 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run91 · run92 와 같은 줄), `sample/*.c` 에 소켓 API 없음 `[소스]` (`grep -i socket` 0건). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (PDB 넷 · 근거 줄) · 3장 끝 새 절 하나 · 4장 · 5장만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

## 요약

- **열기** `[정적]` — `mswsock!WSPSocket +0x226 SockSocket` → `SockSocket +0x591 NtCreateFile`:
  - 이름 `\Device\Afd\Endpoint` (`.rdata 0x561d0`), 접근 `0xc0140000`, 공유 3, `FILE_OPEN_IF` (3), CreateOptions = `dwFlags & 0x101` 이면 **0 (비동기)**, 아니면 `0x20`.
  - EA 항목 하나: 이름 **`AfdOpenPacketXX`** (15글자 + 널, `.rdata 0x56200`), 값 = `EndpointFlags` · 그룹 · **계열 `+0x20` · 종류 `+0x24` · 프로토콜 `+0x28`** · 전송 이름 길이 · 전송 이름.
  - 기본 TCP/UDP 는 **전송 이름이 빈 "TL 모드"** → **EaLength `0x39`, EaValueLength `0x1e`**.
- **열기 뒤** `[정적]`:
  - `0x1207b` (afd `AfdGetInformation`) — 프로세스 **첫 소켓에서만** 클래스 7 (보내기 창) · 6 (받기 창) 두 번. 입력 = 출력 같은 `0x10` 칸, mswsock 은 성공일 때 출력 `+8` dword 만 읽는다. **반환값은 버린다.**
  - `0x12047` (afd `AfdSetContext`) — 소켓 공유 정보 + 주소 둘 + helper 문맥을 넘긴다. mswsock 은 **status 부호만** 본다.
- **연결** `[정적]` — 새 TCP 소켓의 `connect()` 는 `0x120bf` (옵션 `0x3007`) → `0x12003` (묵시적 bind) → `0x12047` → **`0x12007`** (입력 = `0x18` 머리 + 날 sockaddr, 출력 없음).
  - **길이 없을 때 status 는 afd 가 정하지 않는다.** afd `AfdConnect +0xbaf` 가 TL 공급자 표 `+0x28` 칸을 부르고, 받은 값이 `AfdTLConnectComplete +0xe4` 에서 그대로 IRP status 가 된다.
  - mswsock `NtStatusToSocketError` 표: `0xC000023C` → **10051**, `0xC000023D` → **10065**, `0xC00000B5` → **10060**, `0xC0000236` → **10061**, 표에 없는 음수 → 10022.
- **nsi** `[정적]` (급함 낮음) — `\\.\Nsi` 를 겹침 핸들로 열고 `0x120007` (입력 `0x50`) · `0x12000f` (`0x68`) · `0x12001b` (`0x70`). 셋 다 입력 = 출력 같은 구조체. 표 묻기는 "포인터 0 으로 개수 먼저 → 할당 → 다시".
- **정적으로 못 닫은 것**: 길 없음 status 실제 값 (tcpip 몫), ws2_32 → mswsock 다리, 이벤트를 세우는 갈래, 루프백만 있는 PC 의 nsi 대답 → 4장 run124 줄.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든둘째 바퀴. plan **Q217** (PE_emulator 요청 "소켓 장치 (AFD)", 튜닝 `278_tune06.md P1`). 사용자 결정 2026-11-16 "지금 즉시 먼저" (분석 멈춤 중 예외), 2026-11-22 "문서로 만들어서 커밋". **짐 지도 바퀴 아님.**
- 정적: 네 모듈 모두 **처음 analyze** (결과서 PDB 표, GUID+age — `fetch_pdb.py` 가 바이너리 CodeView 에서 읽음):

| 모듈 | 바이너리 | PDB GUID+age | 함수 수 (`_index.tsv`) |
|---|---|---|---|
| mswsock | `System32\mswsock.dll` 451,920 B | `A3E5C0BA2810B53B2646FE9284927D821` | 781 |
| afd | `System32\drivers\afd.sys` 738,792 B | `582DC2B31D185EBE4A3FDD4F872920491` | 1280 |
| nsi | `System32\nsi.dll` 51,128 B | `662A028F57A7C702B11F583CE15A8E631` | 42 |
| nsiproxy | `System32\drivers\nsiproxy.sys` 77,824 B | `27E1902069556C2598DF39EC28A3E3B41` | 77 |

- 공개 심볼이라 **구조체 타입 · IOCTL 상수 이름이 없다.** 아래 "코드 이름" 은 전부 "이름 없음 + afd 처리 함수 이름" 이다.
- 버전 대조 (결과서): 원본 바이트로 `SockSocket +0x591` (`48 ff 15`, ret `+0x598`) · `+0x263` 등 IOCTL 상수 넷을 맞췄고, afd `AfdIoctlTable` 74칸이 `(코드>>2)&0x3ff == 칸 번호` 로 전부 맞음 → **일치**, 스크립트 문제 없음.
- 같은 때 다른 decompiler 가 ntoskrnl (sast/122) 을 돌렸다. 이번엔 `ntoskrnl/IopXxxControlFile.asm` 을 읽기만.

## 목적

- PE_emulator 가 `socket` / `WSASocketW` / `WSASocketA` 를 전부 −1 로 돌려주는 지금, "소켓이 만들어지고, 연결은 망 없는 PC 처럼 정직하게 실패" 하려면 무엇을 흉내 내야 하나를 코드 줄로 답한다 (요청서 `_workspace/sast/121_afd-socket.md`):
  1. 열기 — EA 모양, 열기 뒤 `0x1207b` · `0x12047` 의 칸, mswsock 이 대답에서 보는 것, 성공 최소 응답.
  2. 연결 — `connect()` IOCTL 과 입력 모양, 길 없는 PC 의 status, status → WSA 오류 표, 블로킹 · 오버랩 완료 꼴.
  3. (낮음) nsi IOCTL 셋의 입력 모양 · 길이 먼저 묻기 규칙.

## 방법

- 정적: `_workspace/sast/121_afd-socket.md` — mswsock · afd · nsi · nsiproxy, 한 번에 하나씩 analyze (서버 메모리 보고).
  - mswsock: `WSPSocket` · `SockSocket` depth 1 (23개) + 이름으로 고른 depth 0 22개 (`WSPConnect` · `SockDoConnect*` · `WSPBind` · `WSPSetSockOpt` · `SockWaitForSingleObject` · `Tcpip4_WSHOpenSocket2` 등).
  - afd: depth 0 17개 (`AfdDispatch` · `AfdDispatchDeviceControl` · `AfdDispatchImmediateIrp` · `AfdCreate` · `AfdGetInformation` · `AfdSetContext` · `AfdConnect` · `AfdTLConnectComplete(2)` · `AfdInitializeData` 등).
  - nsi: depth 0 (`NsiOpenDevice` · `NsiIoctl` · `NsiAllocateAndGetTable` · `Nsi*Ex` 등), nsiproxy: depth 0 (`NsippDispatchDeviceControl` 등 5개).
  - 표 · 전역 읽기: afd `_refs/{AfdReceiveWindowSize, AfdSendWindowSize, AfdTLConnectComplete, AfdTlClientConnectDispatch}.tsv`, mswsock `_refs/{Tcpip4_TdiModeStream, Tcpip4_WSHOpenSocket2, 0x56848, 0x56890, 0x568a8}.tsv`. afd 처리 표 셋 (`AfdIoctlTable` · `AfdIrpCallDispatch` · `AfdImmediateCallDispatch`) 과 `NtStatusToSocketError` 점프 표 셋은 `.sys` / `.dll` 데이터 칸을 직접 읽음.
  - 지도 (`_map/`) 는 안 만들었다 (필요한 함수를 이름으로 찾음).
- 결과서: `_workspace/sast/121_afd-socket_result.md` (**분석성공**, 스크립트 문제 없음).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` 로 call · ret 줄 맞춤 — mswsock `WSPSocket` · `SockSocket` · `SockGetInformation` · `SockSetHandleContext` · `WSPBind` · `WSPSetSockOpt` · `SockDoConnect` · `SockDoConnectReal` · `SockDoConnectEx` · `MSAFD_ConnectEx` · `SockWaitForSingleObject`, afd `AfdDispatch` · `AfdCreate` · `AfdConnect` · `AfdTLConnectComplete(2)` · `AfdDispatchImmediateIrp`, nsi `Nsi*` 다섯, nsiproxy `NsippDispatchDeviceControl`.
  - `.asm` 줄: `SockSocket +0x3bb ~ +0x41a` · `+0x489` · `+0x4f2 ~ +0x503` (EA · 접근), `SockGetInformation +0x96` · `+0xae` · `+0xe1 ~ +0xe4`, `SockSetHandleContext +0x263` · `+0x2c2`, `SockDoConnectReal +0x420` · `+0x43f` · `+0x46f ~ +0x471` · `+0x4a3` · `+0x60d ~ +0x620`, `WSPBind +0x3cf`, `WSPSetSockOpt +0x1d6`.
  - 글 상수 둘은 `pefile` 로 mswsock `.rdata` 를 직접 읽음: `0x56200` = `AfdOpenPacketXX\0`, `0x561d0` = `\Device\Afd\Endpoint` (UTF-16).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".
- 앞 바퀴 짝: run121 (sast/117 — TEB `WinSockData` 를 쓰는 줄이 mswsock 에 있음), run83 (콘솔 — `NtDeviceIoControlFile` 일반 길), run91 · run92 (DLL 이 장치를 여는 PE_emulator 요청, 같은 꼴의 문서).

## 결과

비유 (우체국 사서함):

- 소켓을 만든다 = 우체국 (afd) 창구에서 **사서함을 하나 여는 것**. 신청서 (EA) 에 "주소 계열 · 종류 · 프로토콜" 을 적어 낸다. 신청서 이름표가 `AfdOpenPacketXX` 가 아니면 창구가 받지 않는다.
- 첫 사서함을 열 때만 "보내기 · 받기 창 크기가 얼마예요?" 를 두 번 묻는다 (`0x1207b`). 대답을 못 들어도 사서함은 열린다 — 직원 (mswsock) 이 대답 실패를 그냥 넘긴다.
- 사서함 정보 카드를 창구에 맡긴다 (`0x12047`). 이건 "접수 됐어요" 도장 (status) 만 확인한다.
- 편지를 부칠 곳에 줄을 대는 것 (`connect`) 은 창구가 아니라 **배송 회사 (tcpip)** 가 "그 동네로 가는 길이 없어요" 라고 답한다. 창구는 그 말을 그대로 전한다.

### 정적 — 호출 지점 (`ms/disassemble/{mswsock,afd,nsi,nsiproxy}/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `mswsock!WSPSocket` | `+0x226` | `+0x22b` | `mswsock!SockSocket` | [정적] |
| `mswsock!WSPSocket` | `+0x248` | `+0x24d` | `mswsock!SockSetHandleContext` (`0x12047`) | [정적] |
| `mswsock!WSPSocket` | `+0x275` | `+0x27a` | 간접 `[RAX+0x30]` (`SockUpcallTable +0x30` 칸 — `WPUCreateSocketHandle` `[추측]`) | [정적] |
| `mswsock!SockSocket` | `+0x173` | `+0x178` | `mswsock!SockGetTdiName` (전송 이름 · 계열) | [정적] |
| `mswsock!SockSocket` | `+0x591` | `+0x598` | `NtCreateFile` (`\Device\Afd\Endpoint`) | [정적] |
| `mswsock!SockSocket` | `+0x7aa` | `+0x7af` | `memcpy` (EA 에 전송 이름 — 이름 있을 때만) | [정적] |
| `mswsock!SockSocket` | `+0x7d2` | `+0x7d7` | `mswsock!SockGetInformation` (클래스 7) | [정적] |
| `mswsock!SockSocket` | `+0x7f5` | `+0x7fa` | `mswsock!SockGetInformation` (클래스 6) | [정적] |
| `mswsock!SockSocket` | `+0x9a1` | `+0x9a6` | `mswsock!SockDemandStartAfd` (열기 실패 셋일 때) | [정적] |
| `mswsock!SockSocket` | `+0x9f2` | `+0x9f9` | `NtCreateFile` (다시 시도) | [정적] |
| `mswsock!SockSocket` | `+0xbff` | `+0xc04` | `mswsock!SockGetInformation` (클래스 `0xa`, 그룹 `g ≠ 0` 일 때만) | [정적] |
| `mswsock!SockGetInformation` | `+0xbb` | `+0xc2` | `NtDeviceIoControlFile` (`0x1207b`) | [정적] |
| `mswsock!SockGetInformation` | `+0x181` | `+0x186` | `mswsock!SockWaitForSingleObject` (PENDING 일 때) | [정적] |
| `mswsock!SockSetHandleContext` | `+0x270` | `+0x277` | `NtDeviceIoControlFile` (`0x12047`) | [정적] |
| `mswsock!SockSetHandleContext` | `+0x2c8` | `+0x2cd` | `mswsock!NtStatusToSocketError` (status 음수) | [정적] |
| `mswsock!WSPConnect` | `+0x58` | `+0x5d` | `mswsock!SockDoConnect` | [정적] |
| `mswsock!SockDoConnect` | `+0x277` | `+0x27c` | 간접 `[RAX+0x88]` (helper 와일드카드 주소) | [정적] |
| `mswsock!SockDoConnect` | `+0x2a2` | `+0x2a7` | `mswsock!WSPBind` (상태 0 일 때 묵시적 bind) | [정적] |
| `mswsock!SockDoConnect` | `+0x4d7` | `+0x4dc` | `mswsock!WSPSetSockOpt` (`0xffff`, `0x3007`) | [정적] |
| `mswsock!SockDoConnect` | `+0x3f7` | `+0x3fc` | `mswsock!SockDoConnectReal` | [정적] |
| `mswsock!WSPSetSockOpt` | `+0x1fd` | `+0x204` | `NtDeviceIoControlFile` (`0x120bf`) | [정적] |
| `mswsock!WSPBind` | `+0x3f2` | `+0x3f9` | `NtDeviceIoControlFile` (`0x12003`) | [정적] |
| `mswsock!WSPBind` | `+0x4e4` | `+0x4e9` | `mswsock!SockSetHandleContext` (`0x12047`) | [정적] |
| `mswsock!SockDoConnectReal` | `+0x463` | `+0x46a` | `NtDeviceIoControlFile` (`0x12007`, 블로킹 길) | [정적] |
| `mswsock!SockDoConnectReal` | `+0x49e` | `+0x4a3` | `mswsock!SockWaitForSingleObject` (PENDING 일 때) | [정적] |
| `mswsock!SockDoConnectReal` | `+0x60f` | `+0x614` | `mswsock!NtStatusToSocketError` (status 음수) | [정적] |
| `mswsock!SockDoConnectReal` | `+0x61b` | `+0x620` | `mswsock!SockPostProcessConnect` (status ≥ 0) | [정적] |
| `mswsock!SockDoConnectReal` | `+0x34c` | `+0x353` | `NtDeviceIoControlFile` (`0x12007`, 비블로킹 — `\Device\Afd\AsyncConnectHlp` 핸들) | [정적] |
| `mswsock!MSAFD_ConnectEx` | `+0x6b` | `+0x70` | `mswsock!SockDoConnectEx` | [정적] |
| `mswsock!SockDoConnectEx` | `+0xee` | `+0xf5` | `NtDeviceIoControlFile` (`0x120c7`) | [정적] |
| `mswsock!MSAFD_ConnectEx` | `+0xe8` | `+0xed` | `mswsock!NtStatusToSocketError` (PENDING → 997) | [정적] |
| `mswsock!SockWaitForSingleObject` | `+0x1c6` | `+0x1cd` | `NtWaitForSingleObject` (짧은 10ms) | [정적] |
| `mswsock!SockWaitForSingleObject` | `+0x153` | `+0x15a` | `NtWaitForSingleObject` (긴 기다림) | [정적] |
| `afd!AfdDispatch` | `+0x11b` | `+0x120` | `afd!AfdCreate` | [정적] |
| `afd!AfdDispatch` | `+0x2a2` | `+0x2a7` | `afd!AfdDispatchDeviceControl` | [정적] |
| `afd!AfdDispatchImmediateIrp` | `+0x8f` | `+0x94` | 간접 (`AfdImmediateCallDispatch` 표 — `AfdGetInformation` · `AfdSetContext` 등) | [정적] |
| `afd!AfdConnect` | `+0xbaf` | `+0xbb4` | 간접 `[RAX+0x28]` (TL 공급자 connect — tcpip 쪽 `[추측]`) | [정적] |
| `afd!AfdConnect` | `+0xc0f` | `+0xc14` | `afd!AfdTLConnectComplete` (즉시 끝남) | [정적] |
| `afd!AfdConnect` | `+0xcab` | `+0xcb0` | `afd!AfdTLPendRequest` (PENDING) | [정적] |
| `afd!AfdTLConnectComplete` | `+0xe8` | `+0xed` | `afd!AfdTLCompleteRequest` | [정적] |
| `afd!AfdTLConnectComplete2` | `+0xda` | `+0xe1` | `IofCompleteRequest` | [정적] |
| `nsi!NsiOpenDevice` | `+0x3f` | `+0x46` | `CreateFileW` (`\\.\Nsi`) | [정적] |
| `nsi!NsiGetParameterEx` | `+0x85` | `+0x8c` | `NtDeviceIoControlFile` (`0x120007`) | [정적] |
| `nsi!NsiGetAllParametersEx` | `+0x82` | `+0x89` | `NtDeviceIoControlFile` (`0x12000f`) | [정적] |
| `nsi!NsiEnumerateObjectsAllParametersEx` | `+0x81` | `+0x88` | `NtDeviceIoControlFile` (`0x12001b`) | [정적] |
| `nsi!NsiAllocateAndGetTable` | `+0x119` · `+0x36b` | `+0x120` · `+0x372` | `NtDeviceIoControlFile` (`0x12001b` 첫째 · 둘째) | [정적] |
| `nsiproxy!NsippDispatchDeviceControl` | `+0x54` · `+0x67` · `+0x9b` | `+0x59` · `+0x6c` · `+0xa0` | `NsippEnumerateObjectsAllParameters` · `NsippGetAllParameters` · `NsippGetParameter` | [정적] |

### 1. 열기 — `NtCreateFile` 와 EA [정적]

**`NtCreateFile` 인자** (`SockSocket +0x4a8 ~ +0x56e`, 결과서):

- `OBJECT_ATTRIBUTES` 길이 `0x30`, 이름 `\Device\Afd\Endpoint`, 속성 `0x42` (INHERIT | CASE_INSENSITIVE; `dwFlags & 0x180` 이면 `0x40`).
- 접근 `0xc0140000` (`+0x503`; 플래그 `0x40` 이면 `0xc1140000`, `+0xb53`), AllocationSize 0, 속성 0, 공유 3, `CreateDisposition` 3 (`FILE_OPEN_IF`).
- **CreateOptions**: `+0x4f2 TEST R14D,0x101` · `+0x4ff CMOVNZ` → `dwFlags & 0x101` 이면 **0 (비동기)**, 아니면 `0x20` (`FILE_SYNCHRONOUS_IO_NONALERT`).
- EaBuffer = 스택 `[RSP+0x140]` (0x60 바이트 넘으면 힙), EaLength = `R13D` = **`Length + 0x39`** (`+0x3bb MOVZX R13D,BX` · `+0x3bf ADD R13D,0x1b`, `BX` = EaValueLength).

**EA 칸 표** (EA 시작 기준, `FILE_FULL_EA_INFORMATION` 하나):

| 오프셋 | 크기 | 뜻 | 값 | 근거 (`SockSocket`) |
|---|---|---|---|---|
| `+0x00` | 4 | NextEntryOffset | 0 | `+0x3d5` |
| `+0x04` | 1 | Flags | 0 | `+0x3d8` (`word [RSI+4] = 0x0f00`) |
| `+0x05` | 1 | EaNameLength | **`0x0f`** | `+0x3d8` |
| `+0x06` | 2 | EaValueLength | **전송 이름 바이트 수 (`Length`) + `0x1e`** | `+0x3e9` |
| `+0x08` | 16 | EaName | **`AfdOpenPacketXX\0`** (`.rdata 0x56200` 16바이트 복사) | `+0x3de` · `+0x3e5` |
| `+0x18` | 4 | EndpointFlags (비트 모음) | 0 에서 시작, 아래 조건으로 OR | `+0x41a` |
| `+0x1c` | 4 | GroupID | `WSPSocket` 인자 `g` | `+0x489` |
| `+0x20` | 4 | **주소 계열** (예 AF_INET 2) | | `+0x402` · `+0x409` |
| `+0x24` | 4 | **소켓 종류** (예 SOCK_STREAM 1) | | `+0x40c` · `+0x410` |
| `+0x28` | 4 | **프로토콜** (예 TCP 6) | | `+0x413` · `+0x417` |
| `+0x2c` | 4 | 전송 이름 길이 (널 제외) | `Length` | `+0x3f5` |
| `+0x30` | `Length + 2` | 전송 이름 (WCHAR) | **이름 있을 때만** 복사 | `+0x791 ~ +0x7aa` |

- **기본은 전송 이름이 빈 "TL 모드"** → `Length = 0` → **EaValueLength `0x1e`, EaLength `0x39`**. 스택 버퍼를 안 지워서 `+0x30` 뒤는 쓰레기 (결과서).
  - 이름을 주는 쪽은 `Tcpip4_WSHOpenSocket2` (`+0x80 ~ +0xb2`): 전역 `Tcpip4_TdiModeStream` 이 켜졌을 때만 `\Device\Tcp`, 아니면 NULL.
  - 그 전역은 `Tcpip4_WSHNotify` 가 알림 `0x80000000` 등을 받을 때만 1. 부르는 곳은 `SockSocket` 실패 갈래 (`+0x916 ~ +0x966`, afd 가 `0x8000001c` 를 주는 TDI 필터 상황) 뿐 → 보통 PC 에선 안 탄다 `[추측]`.
- **EndpointFlags** — 비트 값 · 켜는 줄 · 조건은 `[정적]`, 이름은 PDB 에 없음. 조건의 `[sock+0x50]` = `WSAPROTOCOL_INFOW.dwServiceFlags1` (`XP1_*` 이름 짝은 `[추측]`):
  - `0x1` (`+0x749`): connectionless 이고 종류 2 · 3. `0x10` (`+0x76d`): message oriented 이고 종류 2 ~ 5. `0x100` (`+0x876`): SOCK_RAW.
  - `0x1000` · `0x10000` · `0x100000`: 멀티포인트 (`dwFlags`). `0x01000000` (`+0x485`): TDI 모드로 바뀐 종류 · AppContainer 등. `0x10000000` (`+0xac0`): 등록 I/O 소켓.
  - → 보통 TCP = **0**, UDP = **`0x11`** `[추측: provider 정보 표의 dwServiceFlags1 값은 안 읽음]`.
- **afd 쪽 검사** (`AfdCreate`, 결과서 — `AfdDispatch +0x11b` 에서 불림):
  - 이름 `AfdOpenPacketXX` (`RtlEqualString` `+0xba`; 다른 이름 `AfdSwOpenPacket` · `AfdRioRDOpenPacket` 은 다른 길 `+0x39a` · `+0x3fe`).
  - `EaValueLength ≥ 0x1c`, `전송이름길이 + 2` 짝수, `0x18 + (길이 + 2) ≤ EaValueLength`.
  - EndpointFlags 는 16진 자리마다 맨 아래 비트만 (`& 0xeeeeeeee ≠ 0` → `0xC000000D`), `(flags & 0x11) == 0x10` 이면 `0xC000000D`.
  - 전송 이름이 없으면 계열 `< 0x23` · `≠ −1`, 종류 · 프로토콜 `< 0x10000`.
  - 통과하면 0 → `AfdDispatch` 가 그 값으로 IRP 를 끝냄.

### 2. 열기 뒤 `0x1207b` · `0x12047` [정적]

둘 다 **코드 이름 없음** (PDB 에 상수 없음). afd 에선 `AfdIrpCallDispatch` 칸 = `AfdDispatchImmediateIrp` → `AfdImmediateCallDispatch` 표로 한 번 더 넘어가 처리 함수를 부른다.

**`0x1207b` → `afd!AfdGetInformation`** (보낸 곳 `mswsock!SockGetInformation +0xbb`, 코드 `+0xae`):

- 인자: 핸들 = 방금 연 소켓 `[sock+8]`, **Event = 스레드별 이벤트** (`TlsGetValue` `+0x5d` 로 얻은 블록 `+0x10`), Apc 둘 0, IoStatusBlock 스택, **입력 = 출력 같은 `0x10` 칸**, 길이 둘 다 `0x10`.
- 칸:

| 오프셋 | 크기 | 뜻 | 근거 |
|---|---|---|---|
| `+0x00` | 4 | 정보 클래스 (6 받기 창 · 7 보내기 창 · `0xa` 그룹) — mswsock 이 넣음 | `SockGetInformation +0x96` |
| `+0x04` | 4 | 안 씀 (0) | `+0x44 ~ +0x4c` |
| `+0x08` | 4 (또는 8) | **값** — afd 가 씀. 클래스 6 · 7 은 dword | 읽기 `+0xe1` · `+0xe4` (dword) / `+0x151` · `+0x155` (qword) |
| `+0x0c` | 4 | 안 씀 | |

- 언제: `SockSocket +0x5e3` `SockSendBufferWindow == 0` 일 때만 → **프로세스 첫 소켓** (값이 0 으로 남으면 매번 다시 묻는다). 클래스 `0xa` 는 그룹 `g ≠ 0` 일 때만 (`+0xbff`).
- afd 쪽: 입력 · 출력 `< 0x10` → `0xC0000023`, 모르는 클래스 → `0xC000000D`. 클래스 6 → 전역 `AfdReceiveWindowSize`, 7 → 연결된 소켓이면 그 값 아니면 `AfdSendWindowSize`. 성공이면 `0x10` 복사 · `Information = 0x10` · status 0.
- 기본값: `AfdInitializeData +0x18b ~ +0x19e` 가 제품 종류 워크스테이션 (1) 이면 **`0x10000`**, 서버면 `0x20000` (레지스트리로 덮을 수 있음 — `AfdReadRegistry +0x113 ~ +0x141`).

**`0x12047` → `afd!AfdSetContext`** (보낸 곳 `mswsock!SockSetHandleContext +0x270`, 코드 `+0x263`):

- 부르는 곳: `WSPSocket +0x248` (만들 때), `WSPBind +0x4e4`, `WSPSetSockOpt +0x290`, `SockPostProcessConnect` 등.
- 인자: 핸들 `[sock+8]`, 같은 스레드 이벤트, 입력 = 스택 `[RSP+0x70]` (0x100 넘으면 힙). **출력** = 소켓 상태가 0 (열림) 이면 입력 안 "상대 주소" 칸, 길이 `[sock+0x2c]`; 아니면 NULL / 0.
- 입력 칸 (총 길이 = `roundup8(최대주소길이) × 2 + 0x80 + helper 문맥 길이`):

| 오프셋 | 크기 | 뜻 | 근거 (`SockSetHandleContext`) |
|---|---|---|---|
| `+0x00` | `0x78` | 소켓 공유 정보 (`[sock+0x18 .. +0x90)` 그대로 — 상태 · 계열 · 종류 · 프로토콜 · 주소 길이 · SO_* 값 등) | `+0xe0 ~ +0x11b` |
| `+0x78` | 4 | helper 문맥 길이 | `+0x128 ~ +0x12c` |
| `+0x80` | `roundup8(최대주소길이)` | 내 주소 (`[sock+0xa8]`) | `+0x13c ~ +0x14c` |
| (다음) | 같음 | 상대 주소 (`[sock+0xb0]`) — 출력 포인터가 가리키는 곳 | `+0x153 ~ +0x16f` |
| (다음) | helper 문맥 길이 | helper 문맥 (helper `[RCX+0x70]` 가 채움) | `+0x19c ~ +0x1c6` |

- helper 가 먼저 실패하면 **IOCTL 안 내고 0 반환** (`+0xad ~ +0xaf` → `+0x2cf`). 에뮬레이터 로그에서 `0x12047` 이 빠진 소켓 (52/501) 이 이 갈래일 수 있다 `[추측]`.
- afd 쪽: 입력 바이트 전부를 엔드포인트 문맥으로 저장, 출력 포인터엔 **쓰지 않고** "입력 안 상대 주소 위치" 만 적는다. 유저 호출이면 출력이 입력 안 · 크기 ≤ `0xffff` (아니면 `0xC000000D`). **status 0, Information 0.**

### 3. mswsock 이 대답에서 보는 것 [정적]

| 호출 | 보는 것 | 안 보는 것 | 줄 |
|---|---|---|---|
| `NtCreateFile` | **status 부호** + 핸들 (`[RSP+0xc0]` → `[sock+8]`) | IoStatusBlock (예외 아래) | `SockSocket +0x59d ~ +0x5c6` |
| 〃 예외 1 | status `0xC000000E` · `0xC0000034` · `0xC000003A` → `SockDemandStartAfd` (`AFD` 서비스 `NtLoadDriver`) 뒤 **한 번 더 열기** | | `+0x59f ~ +0x5b8` · `+0x9a1` · `+0x9f2` |
| 〃 예외 2 | status `0x8000001C` + `Information == 0xFFFFFFFF8000001C` → TDI 모드로 다시 | | `+0xb77 ~ +0xb8d` |
| `0x1207b` (클래스 7 · 6) | **반환값 안 씀**. PENDING 이면 기다림, 성공이면 출력 `+8` dword | 출력 `+0` · `+4` · `+0xc`, Information | `SockSocket +0x7d2 ~ +0x7fa`; `SockGetInformation +0xc9 ~ +0xf4` |
| `0x1207b` (클래스 `0xa`) | **반환값 씀** — 실패면 소켓 만들기 실패 | | `SockSocket +0xbff ~ +0xc16` |
| `0x12047` | **status 부호** (`+0x2c2 TEST EBX,EBX`) — 음수면 WSA 번호로 바꿔 돌려주고 `WSPSocket` 은 정리 뒤 실패 | 출력 · Information | `SockSetHandleContext +0x277 ~ +0x2cd`; `WSPSocket +0x24d ~ +0x253` |

- 실패면 `WSPSocket` 이 `*lpErrno` 에 WSA 번호 (`+0x2b5`) + **−1 (INVALID_SOCKET)** 반환. 열기 실패 쪽 번역: `0xC0000034` · `0xC000000E` · `0xC000003A` → **10050** (WSAENETDOWN), `0xC0000022` → 10013, 표에 없는 음수 → 10022.
- `WSAGetLastError` 에 그 번호가 남는지는 ws2_32 가 `*lpErrno` 를 옮기는 줄에 달림 `[공백]` (이번엔 ws2_32 안 봄).

### 4. 소켓을 만들기에 성공하려면 — 최소 응답 [정적]

1. **`NtCreateFile` (`\Device\Afd\Endpoint`, EA `AfdOpenPacketXX`)** → status 0 + 0 아닌 새 핸들. IoStatusBlock 은 안 읽힘. EA `+0x20` · `+0x24` · `+0x28` (계열 · 종류 · 프로토콜) 은 파일 객체에 적어 둘 것 (뒤 IOCTL 이 이 소켓 것).
2. **`0x1207b`** (첫 소켓에서 클래스 7 · 6) → status 0, 같은 `0x10` 칸 `+8` dword = `0x10000`, Information `0x10`. 바로 끝낼 것 (PENDING 이면 mswsock 이 이벤트를 기다림). 실패해도 소켓은 만들어지지만 값 0 이면 소켓마다 다시 묻는다.
3. **`0x12047`** → status 0, 출력 안 씀, Information 0. 입력 바이트는 소켓별로 저장 (뒤에 `0x12043` `AfdGetContext` 가 되돌려줌 — 범위 밖).
- 그 뒤 SOCKET 값은 ws2_32 쪽 `WPUCreateSocketHandle` (`WSPSocket +0x275` 간접) 이 만든다 — 유저 안에서 끝남 `[추측]`.

### 5. `connect()` — IOCTL 차례와 입력 [정적]

- `connect()` · `WSAConnect` → `WSPConnect +0x58 SockDoConnect` 같은 길 `[추측: ws2_32 안 봄]`. `ConnectEx` 는 다른 길 (아래 7).
- `SockDoConnect` 앞 검사 (WSA 번호): 소켓 없음 10038, 이미 연결 중 10037, 연결됨 10056, 듣기 · 이상한 상태 10022, 주소 짧음 10014, 계열 다름 10047.
- **상태 0 (bind 안 함)** 이면 helper 와일드카드 주소 (`+0x277`) 로 `WSPBind` (`+0x2a2`) 를 먼저. TCP + AF_INET/AF_INET6 이면 그 앞에 `WSPSetSockOpt(0xffff, 0x3007)` (`+0x4d7`; `0x3007` = `SO_REUSE_UNICASTPORT` 로 보임 `[추측]`).

| 순서 | 코드 | afd 처리 함수 | 내는 줄 | 입력 모양 |
|---|---|---|---|---|
| 1 | `0x120bf` | `AfdTliIoControl` | `WSPSetSockOpt +0x1fd` (코드 `+0x1d6`) | `0x20`: `+0` 1, `+4` 레벨 `0xffff`, `+8` 옵션 `0x3007`, `+0xc` 바이트 1, `+0x10` 값 포인터, `+0x18` 값 길이 |
| 2 | `0x12003` | `AfdBind` | `WSPBind +0x3f2` (코드 `+0x3cf`) | 입력 = 출력. TL 모드: `+0` 공유 방식 (0/1/2), `+4` 날 sockaddr. 출력: 버퍼 `+0` 부터 sockaddr (임시 포트 포함), 길이 = **Information** → mswsock 이 `[sock+0xa8]` 로 복사 (`+0x4bf`) |
| 3 | `0x12047` | `AfdSetContext` | `WSPBind +0x4e4` | 2장과 같음 |
| 4 | **`0x12007`** | **`AfdConnect`** | `SockDoConnectReal +0x463` (코드 `+0x43f`) | 아래 |

- 기본 TCP 는 `SockUpdateWindowSizes` (`WSPBind +0x4f8`) 에서 `0x12037` · `0x1203b` 를 안 낸다 `[추측: [sock+0x45] 0x40 · 0x80 비트가 앱이 SO_RCVBUF/SNDBUF 를 바꿨을 때만이라고 봄]`.
- `WSAConnect` 에 연결 데이터가 있으면 그 앞에 `0x1204b` · `0x1206b` (`SockDoConnect +0x308` · `+0x3bd` 의 연결 데이터 함수 — 같은 주소 이름 2개, 후보: `SockSetConnectData`, `SockGetConnectData`). `connect()` 는 NULL 이라 안 탐 `[추측]`.

**`0x12007` 입력 (TL 모드)** — 출력 NULL · 길이 0, **입력 길이 = `0x18 + namelen`** (IPv4 `0x28`):

| 오프셋 | 크기 | 뜻 | 근거 (`SockDoConnectReal`) |
|---|---|---|---|
| `+0x00` | 1 | UseSAN = `SockSanEnabled ≠ 0` | `+0x1fc ~ +0x1ff` |
| `+0x01` | 7 | 안 씀 | |
| `+0x08` | 8 | 루트 엔드포인트 = 0 | `+0xf9 ~ +0xfc` |
| `+0x10` | 8 | 연결 엔드포인트 (이 길에선 afd 가 안 읽음 — `AfdConnect +0x462`, 버퍼를 안 지워 쓰레기일 수 있음 `[추측]`) | |
| **`+0x18`** | **namelen** | **날 sockaddr** — `connect()` 의 name 그대로 (helper 최대 길이로 자름) | `+0x104 ~ +0x10e` memcpy (call `+0x10e`) |

- 전송 이름 있는 TDI 모드일 때만 `+0x18` 이 TA 꼴 (`SockBuildTdiAddress`: count 1 · 길이−2 · sockaddr). 기본은 안 씀.
- afd 검사 (`AfdConnect +0x16e ~ +0x2b1`, 64비트): 입력 ≥ `0x18`, 출력 0 또는 ≥ `0x10` (1 ~ 15 → `0xC000000D`), TL 모드면 `sa_family < 0x23` 이고 `입력 − 0x18 ≥ SOCKADDR_SIZE(family)` (아니면 `0xC0000141`, `+0x4fd ~ +0x52b`).

### 6. 길 없는 PC 에서 afd 가 돌려줄 status [정적 + 추측]

- **afd 는 값을 정하지 않고 넘긴다.** `AfdConnect` (rva `0x3c1a0`, 칸 `AfdIrpCallDispatch[1]` `0x12007` 과 `[0x2e]` `0x120bb` 가 같은 함수):
  - `+0xb59` TL 공급자 표 (`AfdTlClientConnectDispatch`) 고름 → `+0xb01 ~ +0xb08` 완료 함수 `AfdTLConnectComplete` 를 요청에 넣음 → **`+0xbaf` 표 `+0x28` 칸 간접 호출** (ret `+0xbb4 MOV R14D,EAX`).
  - PENDING 아님 (`+0xbf0 CMP R14D,0x103`) → `+0xc0f AfdTLConnectComplete(IRP, R14D)` 를 afd 가 직접. PENDING → `+0xcab AfdTLPendRequest`, 나중에 TL 이 `AfdTLConnectComplete` 를 부름.
  - 어느 쪽이든 **`AfdTLConnectComplete +0xe4 MOV [Irp+0x30],EBP`** (`EBP` = 받은 status) 가 IoStatus.Status. 실패 갈래도 `+0xe1` 로 합쳐져 같은 줄.
- afd 스스로 정하는 status (경로와 무관): 형식 틀림 `0xC000000D`, 주소 길이 `0xC0000141`, 핸들 형식 `0xC0000008`, 메모리 `0xC000009A`, SAN `0xC00000FA` (일반 PC 는 `AfdSanServiceHelper == NULL` 이라 안 탐 `[추측]`).
- **후보** `[추측]` (tcpip 몫, 안 봄): 어댑터 없음 · 경로 없음 → `0xC000023C` (NETWORK_UNREACHABLE) 즉시; 같은 서브넷인데 상대 무응답 → 재전송 뒤 `0xC00000B5` (IO_TIMEOUT) 또는 `0xC000023D` (HOST_UNREACHABLE). → 동적 5번.

### 7. status → WSA 번호 (`mswsock!NtStatusToSocketError`, rva `0xbca0`, 645바이트) [정적]

- 점프 표 셋 (`0xbf6c`/`0xbf44` · `0xc034`/`0xbffc` · `0xc120`/`0xc0f8`) 을 `.dll` 에서 읽어 풀었다 (결과서). `+0x8 RtlNtStatusToDosError` 는 부르지만 **반환값을 안 쓴다.**
- 특수: `0` · 양수 → 0, `0x102` → 10060, **`0x103` (PENDING) → 997**, `0x80000005` → 10040, `0xC00000D0` → 10035, `0xC0000120` → 10004, 표에 없는 음수 → **10022**.

| NTSTATUS (이름은 `[추측]`) | WSA |
|---|---|
| `0xC000023C` NETWORK_UNREACHABLE · `0xC000023E` · `0xC00000BE` | **10051** WSAENETUNREACH |
| `0xC000023D` HOST_UNREACHABLE | **10065** WSAEHOSTUNREACH |
| `0xC0000350` | 10064 WSAEHOSTDOWN |
| `0xC0000236` CONNECTION_REFUSED · `0xC00000BC` | **10061** WSAECONNREFUSED |
| `0xC00000B5` IO_TIMEOUT · `0xC000013F` | **10060** WSAETIMEDOUT |
| `0xC000023F` | 10054 · `0xC0000241` → 10053 · `0xC0000240` → 10004 · `0xC000023B` → 10056 |
| `0xC0000141` INVALID_ADDRESS | 10049 |
| `0xC000000D` INVALID_PARAMETER | 10022 (표에 없음) |
| `0xC0000008` | 10038 · `0xC000009A` → 10055 |
| `0xC0000034` · `0xC000000E` · `0xC000003A` | 10050 WSAENETDOWN |

- 연결에서 부르는 줄: `SockDoConnectReal +0x60f` (ret `+0x614`, `ESI ≠ 0xC00000FA` 이고 음수일 때). 성공이면 `+0x61b SockPostProcessConnect`. 바꾼 값은 `SockDoConnect → WSPConnect +0x6b` 가 `*lpErrno` 로 내보내고 `−1` 반환 (`+0x6d`).

### 8. 블로킹 · 비블로킹 · 오버랩 완료 꼴 [정적]

- **블로킹 `connect()`** (`[sock+0x44] & 0x40` 꺼짐, 또는 비연결형):
  1. `+0x420` IoStatusBlock 을 `0x103` 으로 채움 → `+0x463 NtDeviceIoControlFile` (스레드 이벤트, 출력 없음).
  2. `+0x471 CMP EAX,0x103`: **PENDING 아니면 반환 status 를 그대로** (`+0x46f MOV ESI,EAX`, IoStatusBlock 안 읽음).
  3. PENDING 이면 임계 구역 놓고 `+0x49e SockWaitForSingleObject` → 돌아와 **`+0x4a3 MOV ESI,[RSP+0x98]`** (IoStatusBlock.Status).
  4. `SockWaitForSingleObject`: 먼저 `NtWaitForSingleObject(이벤트, TRUE, −10ms)` (call `+0x1c6`), 못 받으면 블로킹 후크 (`[RAX+0x40]` 간접, `+0xc1`) 뒤 소켓 시간 제한으로 긴 기다림 (`+0x153`).
- **비블로킹** (`[sock+0x44] & 0x40` 켜짐, 연결형): `\Device\Afd\AsyncConnectHlp` 핸들에 같은 `0x12007` (`+0x34c`) → status ≥ 0 이면 IoStatusBlock 안 읽고 **10035 (WSAEWOULDBLOCK)** 반환, 완료는 mswsock 비동기 스레드 몫.
- **오버랩 `ConnectEx`** (`MSAFD_ConnectEx +0x6b SockDoConnectEx`): IoStatusBlock = 호출자 `OVERLAPPED` 자체, Event = `hEvent`, ApcContext = `hEvent` 맨 아래 비트 0 이면 `OVERLAPPED*` (완료 포트로 감), 1 이면 0. 코드 **`0x120c7`** (`AfdSuperConnect`), 보낼 데이터는 출력 버퍼 칸. 반환 status 그대로 → PENDING 이면 `MSAFD_ConnectEx +0xe8` 에서 997. mswsock 은 안 기다린다.
- afd 완료: `AfdTLConnectComplete +0xe8 AfdTLCompleteRequest` · `AfdTLConnectComplete2 +0x72 ~ +0xda` (APC · `IofCompleteRequest`). TDI 모드는 `AfdRestartConnect` 를 완료 함수로 `IofCallDriver` (`AfdConnect +0xd3d`).

### 9. nsi (`nsi.dll` → `nsiproxy.sys`) — 급함 낮음 [정적]

- 열기 `NsiOpenDevice +0x3f CreateFileW(L"\\.\Nsi", 접근 0, 공유 3, OPEN_EXISTING, FILE_FLAG_OVERLAPPED)` — 전역 `g_NsiAsyncDeviceHandle` 이 `−1` 일 때만, `CMPXCHG` 로 저장. EA 없음. NT 이름 `\??\Nsi` 로 바뀌는 줄은 안 봄 `[추측: kernelbase CreateFile 변환]`.
- 틀: 열기 → `CreateEventA` → `NtDeviceIoControlFile(전역 핸들, 이벤트, …, 입력 = 출력 = 호출자 구조체)` → `0x103` 이면 `NtWaitForSingleObject(이벤트, FALSE, NULL)` 뒤 IoStatusBlock.Status → `CloseHandle`.

| 코드 | 쓰는 함수 | 입력 | 출력 | 내는 줄 |
|---|---|---|---|---|
| `0x120007` | `NsiGetParameter(Ex)` | `0x50` | 호출자 구조체 `+0x48` 의 값 | `NsiGetParameterEx +0x85` |
| `0x12000f` | `NsiGetAllParameters(Ex)` | `0x68` | `0x68` | `NsiGetAllParametersEx +0x82` |
| `0x12001b` | `NsiEnumerateObjectsAllParameters(Ex)` · **`NsiAllocateAndGetTable`** | `0x70` | `0x70` | `NsiEnumerateObjectsAllParametersEx +0x81`; `NsiAllocateAndGetTable +0x119` · `+0x36b` |

- `0x12001b` 의 `0x70` 구조체: `+0x10` 둘째 인자 포인터, `+0x18` 셋째 인자, `+0x20` 첫째 인자, `+0x24` = 1, **`+0x68` = 개수**. `+0x28 ~ +0x60` 은 (버퍼 포인터, 칸 크기) 네 쌍처럼 보인다 `[추측]`.
- **길이 먼저 묻기**: `NsiAllocateAndGetTable` 이 포인터 모두 0 으로 첫 `0x12001b` → 개수 `+0x68` 에 개수/8 (최소 10) 여유 → `g_NsiHeap` 에 칸별 배열 → 둘째 `0x12001b`. 전체 최대 3번 (`+0x48 CMP ECX,3`).
- status: 음수 또는 `0x105` 면 `RtlNtStatusToDosError` 값을 돌려줌. `NsiGetParameterEx` 는 `Information` 을 구조체 `+0x48` 에 써 줌 (필요 길이 돌려주기). 작은 버퍼 때 status 는 커널 NSI 층이 정함 → `[공백]` 동적.
- nsiproxy `NsippDispatchDeviceControl` (rva `0x2b40`): `0x12000f` · `0x12001b` · `0x120007` 를 각 처리 함수로, 나머지는 `0x12000b + n` 점프 표, 없으면 `0xC0000002`. 모두 `METHOD_NEITHER`. `0x12001b` 입력 `< 0x70` → `0xC000000D`, 정상이면 커널 NSI 모듈의 `NsiEnumerateObjectsAllParametersEx` 로 넘기고 `Information = 0x70`. 루프백만 있는 PC 의 대답은 공급자 (tcpip 등) 가 정함 `[추측]`.

### afd 제어 코드 → 처리 함수 (참고)

- `AfdDispatchDeviceControl` (rva `0x14cc0`): `(코드 >> 2) & 0x3ff` 가 `0x4a` 이상이거나 표 코드와 다르거나 칸이 NULL 이면 **`0xC0000010`** (`+0x18d`). 즉시 처리 칸은 `AfdDispatchImmediateIrp` 가 `(파일 객체, 코드, 모드, 입력, 입력 길이, 출력, 출력 길이, &Information)` 로 부른다. **모든 코드가 `METHOD_NEITHER`** — 입력 · 출력이 사용자 주소 그대로.
- 74칸 전체 표는 결과서 "참고: afd 제어 코드 → 처리 함수". 이번 길에 나온 것만: `0x12003 AfdBind` · `0x12007 AfdConnect` · `0x12037 AfdQueryHandles` · `0x1203b AfdSetInformation` · `0x12043 AfdGetContext` · `0x12047 AfdSetContext` · `0x1207b AfdGetInformation` · `0x120b3 AfdAddressListQuery` · `0x120bb AfdConnect` · `0x120bf AfdTliIoControl` · `0x120c7 AfdSuperConnect`.

### 호출 관계

```
(PE_emulator 요청 — test.exe 초기화 밖. 새 TCP 소켓: socket() → 블로킹 connect())
mswsock!WSPSocket
 ├─ (1) +0x22b  SockSocket
 │        ├─ (1) +0x178  SockGetTdiName              전송 이름 비어 있음 (TL 모드)
 │        │  +0x3bb ~ +0x41a · +0x489                EA 0x39 바이트 (AfdOpenPacketXX, +0x20/+0x24/+0x28)
 │        ├─ (2) +0x598  NtCreateFile(\Device\Afd\Endpoint, 0xc0140000)   ⇒ afd!AfdDispatch +0x120 AfdCreate
 │        ├─ (3) +0x7d7  SockGetInformation(7) ─ +0xc2 NtDeviceIoControlFile 0x1207b   (첫 소켓만, 반환 버림)
 │        └─ (4) +0x7fa  SockGetInformation(6) ─ +0xc2 NtDeviceIoControlFile 0x1207b
 ├─ (2) +0x24d  SockSetHandleContext ─ +0x277 NtDeviceIoControlFile 0x12047   (status 부호만)
 └─ (3) +0x27a  [간접 +0x30] WPUCreateSocketHandle [추측]

mswsock!WSPConnect
 └─ +0x5d  SockDoConnect
          ├─ (1) +0x27c  [간접 +0x88] helper 와일드카드 주소
          ├─ (2) +0x4dc  WSPSetSockOpt(0xffff, 0x3007) ─ +0x204 NtDeviceIoControlFile 0x120bf
          ├─ (3) +0x2a7  WSPBind ─ (1) +0x3f9 NtDeviceIoControlFile 0x12003
          │                      └ (2) +0x4e9 SockSetHandleContext ─ +0x277 0x12047
          └─ (4) +0x3fc  SockDoConnectReal
                   ├─ (1) +0x46a  NtDeviceIoControlFile 0x12007   ⇒ afd!AfdConnect +0xbb4 [간접 TL +0x28]
                   │                                               → +0xc14 AfdTLConnectComplete (+0xe4 status 기록)
                   ├─ (2) +0x4a3  SockWaitForSingleObject          (PENDING 일 때만)
                   └─ (3) +0x614  NtStatusToSocketError            (음수 → 10051 / 10065 / 10060 …)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 들여쓴 `+0x..` 범위는 그 함수 안 줄. `⇒` 는 syscall 너머 커널 드라이버 (`IopXxxControlFile` → `IofCallDriver` 사이는 안 그림).
- `SockDoConnect` 안 (1) ~ (4) 는 **코드에 나오는 순서가 아니라 실행 순서** `[정적: 갈래 읽기]` — `+0x4d7` (setsockopt) 이 `+0x2a2` (bind) 보다 뒤 주소지만 그 앞에 실행되는 갈래다 (결과서 "WSPBind 앞에 WSPSetSockOpt"). 실제 차례는 동적 4번.

## 확인된 것

- `[정적]` 소켓 열기 = `SockSocket +0x591` (ret `+0x598`) `NtCreateFile`. 이름 `\Device\Afd\Endpoint` · EA 이름 `AfdOpenPacketXX` 는 `.rdata` 바이트로 flow-writer 가 다시 읽음.
- `[정적]` EA 칸 (`+0x00 ~ +0x30`) 과 EaLength = `Length + 0x39` — `SockSocket +0x3bb ~ +0x41a` · `+0x489` 줄과 같음. 기본 TL 모드면 `0x39`.
- `[정적]` CreateOptions 는 `dwFlags & 0x101` 로 0 / `0x20` (`+0x4f2` · `+0x4ff`). 접근 `0xc0140000` (`+0x503`).
- `[정적]` `0x1207b` 는 `SockGetInformation +0xae` · `+0xbb`, 입력 = 출력 `0x10`, mswsock 은 `+8` 만 읽음 (`+0xe1`). 클래스 6 · 7 호출의 반환값은 `SockSocket` 이 안 봄.
- `[정적]` `0x12047` 은 `SockSetHandleContext +0x263` · `+0x270`, 판정은 `+0x2c2 TEST EBX,EBX` (부호만).
- `[정적]` 연결 IOCTL `0x12007` = `SockDoConnectReal +0x43f` · `+0x463`; PENDING 아니면 반환값 (`+0x46f`), PENDING 이면 기다린 뒤 IoStatusBlock (`+0x4a3`).
- `[정적]` afd 는 connect status 를 TL 공급자에서 받아 (`AfdConnect +0xbaf` ret `+0xbb4`) `AfdTLConnectComplete +0xe4` 에서 그대로 IRP 에 넣는다.
- `[정적]` status → WSA 표 (`NtStatusToSocketError`) — 위 7절. `0xC000023C` → 10051, `0xC000023D` → 10065.
- `[정적]` afd 코드는 전부 `METHOD_NEITHER`, 표 밖 코드는 `0xC0000010`.
- `[정적]` nsi 세 코드 · 입력 길이 · 길이 먼저 묻기 (`NsiAllocateAndGetTable` 두 번 호출) — 위 9절.

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- 결과서 "`SockDoConnectReal +0x60d/+0x614` `NtStatusToSocketError`" → call 자리는 **`+0x60f`** (`+0x60d` 는 `MOV ECX,ESI` 인자 넣기), ret `+0x614`. "`+0x618/+0x620` `SockPostProcessConnect`" → call **`+0x61b`** (`+0x618` 은 `MOV RCX,RBX`). 이 문서는 `_calls.tsv` 값을 썼다.
- 결과서 "각 `Nsi…` 함수는 같은 틀: `NsiOpenDevice` → …" — **`NsiGetAllParametersEx` 는 `NsiOpenDevice` 를 안 부르고** 열기를 제 몸 안에 펼쳐 둔다 (`+0x6 CMP [g_NsiAsyncDeviceHandle],-1` · `+0xfd CreateFileW` · `+0x119 CMPXCHG`). 결과는 같다 (같은 전역 핸들).

## 정정

- 이전 run 에 틀린 곳 없음. run121 의 "TEB `WinSockData` 를 쓰는 줄은 mswsock (`SockWaitForSingleObject` · `AllocateWinsockThreadData`)" 과 부딪히지 않는다 — 이번 `SockWaitForSingleObject` 는 기다림 쪽 줄만 봤다.

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- 열기: **틀림 (막혀 있음)** — 결과서에 따르면 `nt_create_file.cpp:58` 이 EA 가 있으면 `extended_attributes_unsupported` 로 멈춘다. 이 장치는 EA 가 필수. 장치 종류에 afd, EA 이름 확인, `+0x20/+0x24/+0x28` 저장이 필요.
- 비동기 핸들: `socket()` 은 보통 겹침 소켓이라 CreateOptions 0 `[추측: ws2_32 dwFlags 안 봄]` → `nt_device_io_control_file.cpp:226` `device_async_handle_unsupported` 에 걸린다 (결과서).
- 열기 · `0x12047` 은 status 만 보니 **성공 코드만 맞으면 됨**. `0x1207b` 는 `+8` 만. 지금은 afd 코드 몸이 전혀 없다 (`unknown_ioctl` 정책).
- 연결: `0x120bf` · `0x12003` · `0x12047` · `0x12007` 넷이 필요. 정직한 실패는 `0x12007` 에 `0xC000023C` (또는 `0xC000023D`) 를 **즉시 (PENDING 없이)** — 블로킹 길이 `+0x46f` 에서 끝나고 이벤트 · IoStatusBlock 을 안 쓴다. 값 자체는 `[추측]` (동적 5번).
- 이벤트: 진짜 커널은 비동기 핸들 IOCTL 에서 호출 전 이벤트를 비우고 (`IopXxxControlFile +0x2db/+0x2e0 KeClearEvent`) 끝나면 세운다 (`+0xbe4/+0xbe9 KeSetEvent`) — 어느 갈래에서 세우는지는 안 읽음 `[추측]` (정적 6번 · 동적 6번).
- nsi: 지금 `\Device\Nsi` 모름. 필요하면 `0x12001b` 첫 호출에 "개수 `+0x68` = 0, status 0" 이 가장 단순 — 단 개수 0 이어도 최소 10칸 잡고 둘째 호출을 내는 것으로 읽힘 `[추측]`.

## 아직 확인 필요

동적 (선 뽑은 VM 에서 `socket` → `connect` 한 번, 결과서 "동적으로 확인할 것" — 대기열 번호는 오케스트레이터가 붙임):

1. `NtCreateFile` EA 0x39 바이트 — EaValueLength `0x1e`, EndpointFlags TCP 0 / UDP `0x11`, `+0x2c` 0 (TL 모드), CreateOptions 0.
2. `0x1207b` 출력 `+8` 이 `0x10000` 인지, 첫 소켓에서만 나오는지.
3. `0x12047` 입력 총 길이 · helper 문맥 길이 (`+0x78`) · 출력 포인터 오프셋.
4. connect 앞 차례 `0x120bf` → `0x12003` → `0x12047` → `0x12007` 가 실제로 그 순서인지, `0x12003` 입력 `+0` 값 · 출력 임시 포트 · Information.
5. **`0x12007` status** — 어댑터 없음 / 선 뽑힘 두 경우: 즉시인가 PENDING 뒤인가, `0xC000023C` / `0xC000023D` / `0xC00000B5` 중 무엇, 걸린 시간, `WSAGetLastError`. `0xC00000FA` 가 안 나오는지.
6. 이벤트가 IOCTL 직후 신호 상태인지 (동기 완료인 `0x1207b` · `0x12047` 도).
7. nsi `0x12001b` 첫 호출 입출력 `0x70` 과 `+0x68`, 루프백만 있는 PC 에서 0 인지.

- 멈춤점 (결과서 추천, `caller+ret`): `mswsock!SockSocket+0x598` · `SockGetInformation+0xc2` · `SockSetHandleContext+0x277` · `WSPBind+0x3f9` · `SockDoConnectReal+0x46a` · `+0x4a3` · `+0x614`, `afd!AfdConnect+0xbb4` · `AfdTLConnectComplete` 입구 (`edx`) · `AfdCreate` 입구. test.exe 가 아니라 소켓을 쓰는 프로그램이 필요 → 고르기는 사용자 결정.

정적 (결과서 "다음 요청 후보"):

1. **ws2_32 다리** — `socket` · `WSASocketW` · `connect` 가 provider 로 넘기는 `dwFlags` · `lpProtocolInfo`, `*lpErrno` → `SetLastError` 줄, `WPUCreateSocketHandle`.
2. **afd 칸 마저** — `AfdBind` (TL 출력 쓰기) · `AfdTliIoControl` · `AfdSetInformation` · `AfdQueryHandles` · `AfdSuperConnect` · `AfdGetAddress` / `AfdGetRemoteAddress`. (`0x120b3` `AfdAddressListQuery` 는 sast/123 이 따로 봄 — run126 예정.)
3. **mswsock 시작 쪽** — `WSPStartup` · `SockEnterApiSlow` · `SockLoadTransportList` (`NtCreateFile` 앞 레지스트리 · 이벤트 syscall).
4. **tcpip.sys** TL connect 진입점 (`AfdTlClientConnectDispatch +0x28` 칸 실제 함수) 의 "경로 없음" 줄 — 동적 5번으로 먼저 닫고 필요할 때.
5. **nsi** `0x70` 구조체 칸 뜻 (호출자 iphlpapi `GetAdaptersAddresses` 등), 커널 `nsi.sys` 개수 0 대답. (sast/126 nsi-callers 가 일부 — run128 예정.)
6. **`IopXxxControlFile` 이벤트 규칙** — 비동기 핸들 + 이벤트의 동기 완료에서도 세우는지 (`+0xbe4` 갈래).
7. (flow-writer 보탬, 낮음) EndpointFlags 를 정하는 `dwServiceFlags1` 값 (provider 정보 표 `SockTcpProviderInfo`) — TCP 0 · UDP `0x11` `[추측]` 을 닫으려면.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서가 afd 처리 표 셋 · `NtStatusToSocketError` 점프 표 셋을 바이너리 데이터 칸에서 직접 읽었다. 표 읽기 검증은 "`AfdIoctlTable[i]` 코드의 `(>>2)&0x3ff == i` 74칸 전부" — 같은 방법을 다른 드라이버 디스패치 표에도 쓸 수 있다 (하네스 제안 아님, 기록만).

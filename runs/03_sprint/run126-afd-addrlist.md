# 2026-11-22 (작성일, 정적 분석 2026-11-17) run126 — AFD 주소 목록 묻기 · IRP 완료 (Q219, 정적만): afd `AfdAddressListQuery` 는 출력 `+0` 에 **주소 개수를 늘 쓴다** (0 개여도 · 버퍼가 모자라도). mswsock `WSPIoctl` 은 그 개수 하나로 `8 + 개수 × (16 + 8올림(MaxSockaddrLength))` 를 셈해 돌려준다. 주소 0 개 = status 0 · Information 4 · `00 00 00 00`. IRP 길 IOCTL 은 오류 (`0xC…`) + PENDING 없음이면 IOSB · 이벤트를 안 건드리고, 성공 · 경고면 `IopCompleteRequest` 가 IOSB 를 쓰고 이벤트를 세운다

> **일부 닫힘 (run127, sast/125 — Q220)**: 11절 "`+0xbb4 ~ +0xbbc` 파일 객체 `+0xb0` 이 있으면 다른 갈래로 (안 읽음)" 은 읽었다 — 빠른 길도 포트 패킷을 따로 넣고, 오류 · (`0x2000000` 켜짐 + 경고 아님) · `ApcContext` 0 이면 패킷만 건너뛴다 (`IopXxxControlFile +0xe82 ~ +0xea9` · `+0x10a9 ~ +0x10df`). 그래서 "아직 확인 필요" 정적 ⑩ 의 뒷몫 (빠른 길 포트 갈래) 은 **닫혔고**, 앞몫 (afd 가 FALSE 를 돌리는 갈래) 은 남는다. 10절 표 "동기 핸들에 붙는지는 `[공백]`" 은 `NtSetInformationFile 0x1e` 로는 **못 붙인다** (동기 핸들 `0xC000000D`, 다른 길은 안 봄). 11절 비트 `0x8000000` 은 `0x29` 입력 비트 `0x4` 가 켠다. `runs/03_sprint/run127-file-completion.md` 3 · 5절 · "정정".

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("AFD 주소 목록 묻기 `0x120b3`", 튜닝 `282_tune07.md T1`, 갈래 K 셋째 바퀴 K03) 이다. afd · mswsock 몫은 test.exe 초기화 **밖**이다 — test.exe 는 mswsock · ws2_32 를 안 올린다 (run124 와 같은 근거: 초기화 끝 P2 모듈 6개 `[관찰]` `ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, `sample/*.c` 에 소켓 API 없음 `[소스]`). I/O 관리자 완료 (`IopSynchronousServiceTail` · `IopCompleteRequest`) 는 커널 공용 길이라 test.exe 초기화의 콘솔 `NtDeviceIoControlFile` (`0x500023` · `0x500016`, 2.5 · run51 · run83) 도 같은 함수를 지날 수 있지만, condrv 가 빠른 길인지 IRP 길인지는 안 봤다 `[공백]`. 그래서 **그림은 안 바꿨다.** flow.md 는 기준 · 3장 끝 새 절 하나 · 4장 (run124 줄 닫힘 표시 + run126 줄) · 5장만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q219** (사용자 결정 2026-11-17 "다른 것보다 이거 먼저", 2026-11-22 "문서로 만들어서 커밋"). 오케스트레이터가 요청서를 둘로 나눠 같은 시간에 돌렸다 — sast/123 (R1 ~ R5, afd · mswsock) · sast/124 (R6, ntoskrnl). 이번 run 으로 닫는 몫:
> - R1 ~ R6 — 결과서 둘 다 **분석성공**. 값이 TL (tcpip) · NSI 에서 오는 칸은 `[공백]` 으로 남김.
> - run124 "아직 확인 필요" 정적 ② 의 `AfdBind` · `AfdTliIoControl` 몫 (= Q225 ② 일부) 과 정적 ⑥ `IopXxxControlFile` 이벤트 규칙 (= Q225 ⑥) — 아래 "정정" 절.

## 요약

- **`0x120b3` = 주소 목록 묻기** `[정적]`:
  - 내는 곳: mswsock `WSPIoctl` 의 `SIO_ADDRESS_LIST_QUERY` (`0x48000016`) 갈래, `+0xe1d NtDeviceIoControlFile`. 입력 2 B = 소켓을 만들 때의 주소 계열, IOSB = 입력 `+0x10`, 출력 = 부른 쪽 `cbOutBuffer` 만큼의 힙 (**0 으로 안 채움**) 또는 버퍼가 없으면 4 B 스택 칸 (**0 으로 채움**).
  - 받는 곳: afd 즉시 처리 표 44번 칸 `AfdAddressListQuery` (빠른 길). 출력 = `DWORD 개수` + TA_ADDRESS 칸들 (IPv4 18 B · IPv6 30 B, 빈틈 없이 이어 붙임). **포인터 칸은 없다.**
  - **개수는 늘 쓴다** — 0 개여도, 버퍼가 모자라도. 모자라면 `0x80000005` + Information = 필요한 전체 크기, 넘친 칸부터는 본문을 안 쓴다. 출력 길이 < 4 만 `0xC000000D`.
  - **주소 0 개는 정상 갈래** (status 0 · Information 4 · 출력 `00 00 00 00`). 루프백 인터페이스의 `127.0.0.1` · `::1` 은 목록에 안 들어간다 → 망 없는 PC 는 0 개일 수 있다 `[추측: 남는 주소는 NSI 몫]`.
  - mswsock 은 status `0` · `0x80000005` 만 통과시키고 **Information 은 안 읽는다**. 출력 `+0` 개수 하나로 `8 + 개수 × (16 + A)` (A = `MaxSockaddrLength` 를 8 로 올림) 를 `*lpcbBytesReturned` 에 쓴다. **재시도 고리 없음** — 로그의 세 번은 부른 쪽 몫 `[추측]`.
  - 에뮬레이터가 개수를 안 쓰면 힙에 있던 쓰레기가 개수가 된다 → 요청 쪽의 64 MiB 넘는 할당과 맞는 모양 `[추측: 쓰레기 값은 동적]`.
- **`0x120bf` = TL 에 넘기는 옵션 · ioctl** `[정적]`: 입력 `0x20` 의 `+0x00` = 1 설정 · 2 읽기 · 3 소켓 ioctl, `+0x0c` 는 반드시 1. **afd 는 출력에 한 바이트도 안 쓴다** — 값 · Information 은 TL (tcpip) 몫 `[공백]`, TL 의 `0xC0000206` 만 `0xC0000023` 으로 바꾼다. 요청 쪽 로그의 "출력 8 B 힙 · IOSB = 입력 + `0x30`" 호출은 `WSPIoctl +0x2007` (종류 3, 앱의 `WSAIoctl`).
- **`0x12003` = `AfdBind`** `[정적]`: TL 모드 검사 (입력 ≥ 6 · 출력 ≥ 입력 − 4 · 계열 < `0x23` · `SOCKADDR_SIZE` · 이미 묶임 `0xC0000238`) 뒤 **`0x103` (PENDING) 으로 돌아간다**. TL bind → 로컬 주소 묻기 → `AfdTLBindGetAddrComplete` 가 출력 `+0` 에 로컬 sockaddr · Information = 그 길이 · status 0 으로 IRP 를 끝낸다. **임시 포트는 TL 이 고른다.**
- **`SOCKADDR_SIZE`** `[정적]`: 계열 2 → 16, `0x17` → 28, 1 → 110, `0x22` → 36, 그 밖 0 (afd `.rdata` 표). 계열 ≥ `0x23` 은 부르는 쪽이 먼저 `0xC0000141`.
- **IRP 길 완료** `[정적]` (sast/124): `NtDeviceIoControlFile` 로 보낸 IRP 를 드라이버가 PENDING 없이 끝내면
  - **성공 · 경고**: `IopCompleteRequest` 가 IOSB 를 쓰고 (Information 먼저, Status 나중) 사용자 이벤트를 세우고, `ApcRoutine` 이면 사용자 APC, 포트 연결 핸들이면 완료 패킷. 반환 = 드라이버 status 그대로 (비동기 + 포트 + 경고만 `0x103`).
  - **오류 (`0xC…`)**: IOSB · 사용자 이벤트 · 파일 객체 이벤트 · `FinalStatus` 를 **안 건드리고** APC · 포트도 없다. 이벤트는 호출 앞에서 비운 채.
- **빠른 길 완료** `[정적]` (flow-writer 보탬): afd 즉시 처리 코드 (`0x120b3` · `0x1207b` · `0x12047`) 처럼 드라이버 빠른 길이 TRUE 를 돌려주면, I/O 관리자는 **status 를 안 보고** IOSB 16 B 를 복사하고 이벤트를 세운다 (`IopXxxControlFile +0x70f → +0xb16 ~ +0xbe4`). 오류여도 IOSB 가 써진다 — IRP 길과 다르다.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든넷째 바퀴. plan **Q219**. **짐 지도 바퀴 아님.**
- 정적:
  - afd · mswsock — sast/121 (run124) 에서 analyze 한 프로젝트를 그대로 썼다 (PDB GUID afd `582DC2B31D185EBE4A3FDD4F872920491`, mswsock `A3E5C0BA2810B53B2646FE9284927D821`). 다시 analyze 안 함, `export` 만.
  - ntoskrnl — pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (기준값과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`). 다시 analyze 안 함, `export` 만.
- 버전 대조:
  - sast/123: `.asm` 의 스택 칸이 요청 쪽 로그 모양과 맞음 — 입력 `RSP+0xe8` · IOSB `RSP+0xf8` (= 입력 `+0x10`) · 4 B 칸 `RSP+0x110` (= 입력 `+0x28`). `AfdIoctlTable` 의 `(0x120b3 >> 2) & 0x3ff = 0x2c` 칸이 `AfdAddressListQuery`. SEH 범위표 풀이가 `.asm` 의 "읽지 않은 틈" 과 같은 자리 → **일치**.
  - sast/124: `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` → **일치**.
  - 스크립트 문제 없음 (둘 다).
- PDB 에 afd · mswsock 구조체 · IOCTL 상수 이름이 없다 → 칸 이름은 쓰임으로 붙인 `[추측]`. ntoskrnl `_IRP` · `_FILE_OBJECT` · `_IO_COMPLETION_CONTEXT` 칸은 PDB 타입으로 맞췄다 (`pdb_struct.py`). 비트 이름 (`FO_SYNCHRONOUS_IO` 등) 은 공개 헤더 값과 맞춘 `[추측]`.

## 목적

- PE_emulator 갈래 K 가 WinHttp 길에서 소켓을 연 뒤 `0x120b3` 을 세 번 내고 64 MiB 넘는 할당으로 멈춘다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님). 요청서 `_workspace/sast/123_afd-addrlist.md` · `_workspace/sast/124_irp-complete.md`:
  1. (R1 · R2) **어느 칸이 다음 버퍼 크기를 정하나**, 주소 0 개 · 작은 버퍼일 때의 정직한 대답.
  2. (R3) `0x120bf` 출력 꼴. (R4 · R5) `AfdBind` · `SOCKADDR_SIZE`.
  3. (R6) IRP 길 IOCTL 을 PENDING 없이 끝냈을 때 IOSB · 이벤트 · 완료 포트 · APC — status 별, 핸들 종류별.

## 방법

- 정적 sast/123 (afd · mswsock):
  - afd depth 1 `^(AfdAddressListQuery|AfdInitializeAddressList|AfdProcessAddressChangeList|AfdNsiAddressChangeEvent|AfdNsiAddressChangeEventAtPassive)$` (27개) + depth 0 7개 (`AfdExceptionFilter` · `AfdCommonAddAddressHandler` · `AfdCommonDelAddressHandler` · `AfdFindAddressInList` · `AfdAddressListChange` · `AfdAddAddressHandler` · `AfdDelAddressHandler`).
  - mswsock depth 1 `^(WSPIoctl|WSPGetSockOpt)$` (31개) + depth 0 `SockLoadHelperDll` · `SockpCreateV6AddressMap`.
  - 묶음 나 · 다: afd `AfdTliIoControl` · `AfdCompleteTliIoControl` · `AfdCompleteTliIoControlCore` (depth 1), `AfdBind` · `AfdTLBind` · `AfdTLBindSecurity` · `AfdTLBindComplete` · `AfdTLBindComplete2` · `AfdTLBindGetAddrComplete` · `SOCKADDR_SIZE` · `AfdAllocateEndpoint` (depth 0). `WSPSetSockOpt` · `WSPBind` 는 sast/121 `.asm` 을 다시 씀.
  - 즉치 `0x120b3` · `0x120bf` 는 mswsock 코드 구역 원본 바이트를 훑어 찾음 (임시 스크립트, 레포 밖). `SOCKADDR_SIZE` 표는 afd `.rdata 0x7ad80` 바이트.
- 정적 sast/124 (ntoskrnl):
  - depth 1 `^(IopSynchronousServiceTail|IopCompleteRequest|IopfCompleteRequest)$` (54개) + depth 0 `^(NtDeviceIoControlFile|NtFsControlFile)$` (요청서에서 넓힘 — 완료를 누가 하나가 마지막 인자 한 칸으로 갈려서).
  - `IopCompleteRequest +0x146 ~ +0x169` (SEH 착지 틈) 은 원본 바이트를 `objdump` 로.
- 결과서: `_workspace/sast/123_afd-addrlist_result.md` · `_workspace/sast/124_irp-complete_result.md` (둘 다 **분석성공**, 스크립트 문제 없음).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` 로 call · ret 짝 — afd `AfdAddressListQuery` · `AfdInitializeAddressList` · `AfdNsiAddressChangeEvent(AtPassive)` · `AfdNsiGetAddressProperties` · `AfdCommonAddAddressHandler` · `AfdQueryCompartmentId` · `AfdTliIoControl` · `AfdCompleteTliIoControl` · `AfdBind` · `AfdTLBind*` · `AfdFastIoDeviceControl`, mswsock `WSPIoctl` · `WSPGetSockOpt` · `SockpCreateV6AddressMap` · `SockLoadHelperDll`, ntoskrnl `NtDeviceIoControlFile` · `IopXxxControlFile` · `IopSynchronousServiceTail` · `IofCompleteRequest` · `IopfCompleteRequest` · `IopCompleteRequest` · `IopWaitForSynchronousIo`.
  - `.asm` 줄: `NtDeviceIoControlFile +0x13`, `IopXxxControlFile +0x6dc ~ +0x70f` · `+0xb16 ~ +0xc30` (빠른 길 완료 — 아래 11절).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".
- 앞 바퀴 짝: run124 (sast/121 — 소켓 열기 · 연결 · afd 처리 표), run93 (완료 포트 · `IoRemoveIoCompletion`), run77 (`KeSetEvent`).

## 결과

비유 (run124 의 우체국 사서함 이어서):

- `0x120b3` = 창구에 "이 우체국 주소 목록 주세요" 하고 **봉투** (출력 버퍼) 를 내미는 것. 창구 (afd) 는 봉투 맨 앞에 **"주소 N 개"** 를 꼭 적는다. 봉투가 작아 다 못 넣어도 N 은 적고 "봉투가 작아요" (`0x80000005`) 라고 한다.
- 직원 (mswsock) 은 **N 만 보고** 손님에게 "이만한 봉투가 필요해요" 를 셈해 준다. 창구가 N 을 안 적으면, 봉투에 원래 묻어 있던 낙서를 N 으로 읽어 버린다 `[추측: 비유]`.
- IRP 길 = 창구가 접수증 (IRP) 을 끊고 처리하는 길. 끝나면 사무실 (I/O 관리자) 이 결과 쪽지 (IOSB) 를 손님 칸에 넣고 벨 (이벤트) 을 울린다. **실패면 쪽지도 벨도 없이** "안 됐어요" 만 돌려준다.

### 정적 — 호출 지점 (`ms/disassemble/{mswsock,afd,ntoskrnl}/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `mswsock!WSPIoctl` | `+0xd6d` | `+0xd72` | 간접 `SockAllocateHeapRoutine` (`cbOutBuffer` B 힙, 0 으로 안 채움) | [정적] |
| `mswsock!WSPIoctl` | `+0xe1d` | `+0xe24` | `NtDeviceIoControlFile` (`0x120b3`) | [정적] |
| `mswsock!WSPIoctl` | `+0xe4c` | `+0xe51` | `mswsock!SockWaitForSingleObject` (PENDING 일 때) | [정적] |
| `mswsock!WSPIoctl` | `+0xe6b` | `+0xe70` | `mswsock!NtStatusToSocketError` (`0` · `0x80000005` 밖) | [정적] |
| `mswsock!WSPIoctl` | `+0x2007` | `+0x200e` | `NtDeviceIoControlFile` (`0x120bf`, 종류 3) | [정적] |
| `mswsock!WSPIoctl` | `+0x2091` | `+0x2096` | `mswsock!SockWaitForSingleObject` (`0x120bf` PENDING) | [정적] |
| `mswsock!WSPGetSockOpt` | `+0x7c4` | `+0x7cb` | `NtDeviceIoControlFile` (`0x120bf`, 종류 2) | [정적] |
| `mswsock!WSPGetSockOpt` | `+0xf3b` | `+0xf40` | `mswsock!SockWaitForSingleObject` | [정적] |
| `mswsock!SockpCreateV6AddressMap` | `+0xbe` | `+0xc5` | `NtDeviceIoControlFile` (`0x120b3`, 계열 `0x17`, 넘치면 다시) | [정적] |
| `mswsock!SockLoadHelperDll` | `+0x2e8` | `+0x2ef` | `RegQueryValueExW` (`"MaxSockaddrLength"`) | [정적] |
| `afd!AfdFastIoDeviceControl` | `+0x1603` | `+0x1608` | 간접 (즉시 처리 표 — 44번 칸 `AfdAddressListQuery`) | [정적] |
| `afd!AfdAddressListQuery` | `+0xdf` | `+0xe6` | `ProbeForWrite` (출력, 4 정렬) | [정적] |
| `afd!AfdAddressListQuery` | `+0x11f` | `+0x124` | `afd!AfdQueryCompartmentId` | [정적] |
| `afd!AfdAddressListQuery` | `+0x3b2` | `+0x3b7` | `afd!AfdGetCompartment` | [정적] |
| `afd!AfdAddressListQuery` | `+0x45f` | `+0x464` | `afd!AfdInitializeAddressList` (처음 묻는 계열) | [정적] |
| `afd!AfdAddressListQuery` | `+0x21a` | `+0x21f` | `afd!IN6ADDR_SETV4MAPPED` (이중 모드) | [정적] |
| `afd!AfdAddressListQuery` | `+0x24b` | `+0x250` | 칸 복사 (`_calls.tsv` 이름 `alias:memmove`) | [정적] |
| `afd!AfdInitializeAddressList` | `+0x170` | `+0x177` | `TdiRegisterPnPHandlers` | [정적] |
| `afd!AfdInitializeAddressList` | `+0x1f9` | `+0x200` | `NsiRegisterChangeNotificationEx` (IPv4) | [정적] |
| `afd!AfdInitializeAddressList` | `+0x277` | `+0x27e` | `NsiRegisterChangeNotificationEx` (IPv6) | [정적] |
| `afd!AfdNsiAddressChangeEvent` | `+0x55` | `+0x5a` | `afd!AfdNsiAddressChangeEventAtPassive` (PASSIVE 일 때) | [정적] |
| `afd!AfdNsiAddressChangeEvent` | `+0xdc` | `+0xe1` | `afd!AfdQueueWorkItem` (아니면 미룸) | [정적] |
| `afd!AfdNsiAddressChangeEventAtPassive` | `+0x17e` | `+0x185` | `NsiAllocateAndGetTable` (처음 전부 읽기) | [정적] |
| `afd!AfdNsiAddressChangeEventAtPassive` | `+0x23d` | `+0x242` | `afd!AfdNsiGetAddressProperties` | [정적] |
| `afd!AfdNsiAddressChangeEventAtPassive` | `+0x2c1` | `+0x2c6` | `afd!AfdCommonAddAddressHandler` | [정적] |
| `afd!AfdNsiGetAddressProperties` | `+0x161` · `+0x176` | `+0x166` · `+0x17b` | `Ipv4UnicastAddressScope` · `Ipv6UnicastAddressScope` | [정적] |
| `afd!AfdCommonAddAddressHandler` | `+0x75` | `+0x7a` | `afd!AfdFindAddressInList` (같은 칸이면 안 넣음) | [정적] |
| `afd!AfdCommonAddAddressHandler` | `+0x272` | `+0x277` | `afd!AfdNotifyIPAvailabilityConsumers` (IP 주소 수 0 → 1) | [정적] |
| `afd!AfdTliIoControl` | `+0x10b` | `+0x110` | `afd!AfdAllowedUserIOControlRequest` (종류 · 레벨 · 표지 검사) | [정적] |
| `afd!AfdTliIoControl` | `+0x7eb` | `+0x7f0` | `afd!AfdTLIoControl` (TL 로 — 일반) | [정적] |
| `afd!AfdTliIoControl` | `+0x868` | `+0x86d` | `afd!AfdHandleISBIoctls` (afd 가 스스로 답함) | [정적] |
| `afd!AfdCompleteTliIoControl` | `+0xa` | `+0xf` | `afd!AfdCompleteTliIoControlCore` | [정적] |
| `afd!AfdCompleteTliIoControl` | `+0x46` | `+0x4d` | `IofCompleteRequest` | [정적] |
| `afd!AfdBind` | `+0x24a` | `+0x24f` | `afd!SOCKADDR_SIZE` | [정적] |
| `afd!AfdBind` | `+0x3a4` | `+0x3a9` | `afd!AfdTLBindSecurity` (→ `0x103`) | [정적] |
| `afd!AfdBind` | `+0xbca` | `+0xbd1` | `IofCompleteRequest` (동기 실패) | [정적] |
| `afd!AfdTLBindSecurity` | `+0x90` | `+0x95` | `afd!AfdTLIoControl` (재사용 · 독점 옵션) | [정적] |
| `afd!AfdTLBindSecurity` | `+0x145` | `+0x14a` | `afd!AfdTLBind` | [정적] |
| `afd!AfdTLBind` | `+0x9e` | `+0xa3` | 간접 (TL 제공자 bind) | [정적] |
| `afd!AfdTLBindComplete` | `+0xcd` | `+0xd2` | `afd!AfdTLBindComplete2` | [정적] |
| `afd!AfdTLBindComplete2` | `+0x13e` | `+0x143` | `afd!AfdTLIoControl` (로컬 주소 묻기, 콜백 `AfdTLBindGetAddrComplete`) | [정적] |
| `afd!AfdTLBindGetAddrComplete` | `+0x54` | `+0x5b` | `TdiCopyBufferToMdl` (출력 `+0` 에 로컬 sockaddr) | [정적] |
| `afd!AfdTLBindGetAddrComplete` | `+0xd1` | `+0xd8` | `IofCompleteRequest` | [정적] |
| `afd!AfdSuperConnect` | `+0x1d3` | `+0x1d8` | `afd!SOCKADDR_SIZE` (`AfdConnect +0x514` 은 run124) | [정적] |
| `nt!NtDeviceIoControlFile` | `+0x59` | `+0x5e` | `nt!IopXxxControlFile` (마지막 인자 1, `+0x13`) | [정적] |
| `nt!IopXxxControlFile` | `+0x2db` | `+0x2e0` | 이벤트 비우기 (`_calls.tsv` 이름 `KeClearEvent`, `.asm` 별칭 표시 `KeResetEvent`) | [정적] |
| `nt!IopXxxControlFile` | `+0x6dc` | `+0x6e1` | `nt!PiCMFastIoDeviceDispatch` (빠른 길 갈래 하나 — 아래 11절) | [정적] |
| `nt!IopXxxControlFile` | `+0x997` | `+0x99c` | `nt!IopSynchronousServiceTail` (IRP 길) | [정적] |
| `nt!IopXxxControlFile` | `+0xbe4` | `+0xbe9` | `KeSetEvent` (빠른 길) | [정적] |
| `nt!IopSynchronousServiceTail` | `+0x306` | `+0x30b` | `nt!IopCallDriverReference` (드라이버 부르기, 반환 = status) | [정적] |
| `nt!IopSynchronousServiceTail` | `+0x39c` | `+0x3a1` | `nt!IopCompleteRequest` (DEFER 길 — `NtFsControlFile`) | [정적] |
| `nt!IopSynchronousServiceTail` | `+0x3e1` | `+0x3e6` | `nt!IopWaitForSynchronousIo` (동기 핸들 + PENDING) | [정적] |
| `nt!IopSynchronousServiceTail` | `+0x3f0` | `+0x3f5` | `nt!IopReleaseFileObjectLock` | [정적] |
| `nt!IofCompleteRequest` | `+0xe` | `+0x13` | `nt!IopfCompleteRequest` | [정적] |
| `nt!IopfCompleteRequest` | `+0x62d` | `+0x632` | `nt!IopCompleteRequest` (그 자리에서) | [정적] |
| `nt!IopfCompleteRequest` | `+0x684` · `+0x3eb` | `+0x689` · `+0x3f0` | `KeInitializeApc` · `KeInsertQueueApc` (특수 커널 APC 로 미룸) | [정적] |
| `nt!IopfCompleteRequest` | `+0x58b` | `+0x590` | `nt!IopCompleteIrpInFileObjectList` (파일 객체 목록 갈래, 안 읽음) | [정적] |
| `nt!IopCompleteRequest` | `+0x94` | `+0x99` | `nt!IopProcessBufferedIoCompletion` | [정적] |
| `nt!IopCompleteRequest` | `+0x3d5` | `+0x3da` | `KeSetEvent` (사용자 이벤트) | [정적] |
| `nt!IopCompleteRequest` | `+0x2ba` | `+0x2bf` | `KeSetEvent` (파일 객체 이벤트) | [정적] |
| `nt!IopCompleteRequest` | `+0x351` | `+0x356` | `KeInsertQueueApc` (사용자 APC) | [정적] |
| `nt!IopCompleteRequest` | `+0x3b4` | `+0x3b9` | `KeInsertQueueEx` (완료 포트 패킷) | [정적] |
| `nt!IopCompleteRequest` | `+0x1db` · `+0x251` | `+0x1e0` · `+0x256` | `IoFreeIrp` (오류 + PENDING 없음 / 보통) | [정적] |

### 1. afd `AfdAddressListQuery` (rva `0x25840`, 1237B) — 검사 차례 [정적]

인자 = (파일 객체, 코드, 요청 모드, 입력, 입력 길이, 출력, 출력 길이, `&Information`) — sast/121 `AfdDispatchImmediateIrp` 꼴과 같다. 줄은 모두 `ms/disassemble/afd/AfdAddressListQuery.asm`.

| 차례 | 조건 | status | Information | 출력 | 줄 |
|---|---|---|---|---|---|
| 0 | 들어오자마자 | — | **0 으로 먼저** | — | `+0x62` |
| 1 | 입력 길이 < 2 | `0xC000000D` | 0 | 안 씀 | `+0x86 ~ +0x89` → `+0x4ef` |
| 2 | 출력 길이 < 4 (0 포함) | `0xC000000D` | 0 | 안 씀 | `+0x8f ~ +0x99` |
| 3 | 요청 모드 커널이면 4 ~ 7 건너뜀 | | | | `+0xa7 ~ +0xaa` |
| 4 | (유저) 입력 포인터 홀수 | `0xC00002C5` (예외 → `AfdExceptionFilter` 가 `0x80000002` 를 바꿈) | 0 | 안 씀 | `+0xac` · `+0xb2` |
| 5 | (유저) 입력 첫 바이트 못 읽음 (NULL 등) | `0xC0000005` | 0 | 안 씀 | `+0xbf ~ +0xd4` |
| 6 · 7 | (유저) 출력 4 정렬 아님 / NULL · 커널 주소 | `0xC00002C5` / `0xC0000005` | 0 | 안 씀 | `+0xd6 ~ +0xdf ProbeForWrite` |
| 8 | 입력 앞 2 B 를 계열로 읽음 · **출력 `+0` = 0** · 쓴 크기 = 4 | — | — | **`+0` DWORD 0** | `+0xeb` · `+0xfe` · `+0x109` |
| 9 · 10 · 11 | 컴파트먼트 번호 · 컴파트먼트 만들기 · 첫 채우기 실패 | 그 status / `0xC000009A` | 0 | 개수 0 남음 | `+0x12a` · `+0x3c4` · `+0x46a` |
| 12 | 칸 본문이 안 들어감 | **`0x80000005`** | **전체 필요 크기** | 개수는 계속 셈, 본문은 안 씀 | `+0x22c ~ +0x23a` → `+0x315` · `+0x31d ~ +0x32d` |
| 13 | 다 들어감 (주소 0 개 포함) | **`0`** | **4 + Σ(칸 길이 + 4)** | 개수 + 칸 본문 | `+0x2bd ~ +0x2c4` |
| 14 | 칸 복사 중 예외 | 예외 코드 | **0** | 그때까지 쓴 것 | 착지 `+0x332` (`AND [RSP+0x48],0`) |

- **엔드포인트 종류 · 상태로 거절하는 줄은 없다.** 묶였든 연결됐든 같은 대답 (`+0x9f` 에서 `FsContext` 를 읽고 바로 씀).
- **계열 값 검사도 없다.** 2 · `0x17` · `0x11` 만 특별 처리하고, 다른 값은 맞는 칸이 없어 개수 0 으로 성공.

### 2. 출력 바이트 꼴 [정적]

| 오프셋 (출력 시작 기준) | 크기 | 뜻 | 값 · 줄 |
|---|---|---|---|
| `+0x00` | 4 | **개수** | 0 으로 시작 (`+0x109`), 칸 하나마다 +1 (`+0x31d ~ +0x322`) — **본문이 안 들어가도 센다** |
| `+0x04` | 2 | 첫 칸 `AddressLength` | IPv4 `0x0e` · IPv6 `0x1a` |
| `+0x06` | 2 | 첫 칸 `AddressType` (계열) | 2 · `0x17` · `0x11` |
| `+0x08` | 길이 B | 주소 본문 | IPv4: 포트 2 B (0) · 주소 4 B · 0 8 B. IPv6: 포트 2 · flow 4 · 주소 16 · 범위 4 |
| 다음 칸 | — | 빈틈 없이 바로 뒤 (정렬 없음) | 복사 위치 = 출력 + 누적 크기 (`+0x240 ~ +0x24b`, `+0x324`) |

- 칸 = TA_ADDRESS 모양 `[추측: 이름은 TDI 문서와 같아서 붙임]`. 칸 하나 = `AddressLength + 4` → IPv4 18 B, IPv6 30 B.
- **포인터 칸 없음.** 사용자에게 줄 `lpSockaddr` 포인터는 mswsock 이 따로 만든다 (5절).
- 포트는 늘 0 — 칸을 만드는 `AfdNsiAddressChangeEventAtPassive` 가 포트 자리를 0 으로 둔다 (`+0x19b ~ +0x1a7`, `+0x276 ~ +0x281`).
- 개수가 0 이면 `+4` 부터는 안 건드린다.
- **이중 모드**: 계열 `0x17` 질의 + 엔드포인트 `+8` 비트 `0x14` 켜짐이면 IPv4 칸을 `::ffff:a.b.c.d` IPv6 칸 (30 B) 으로 바꿔 보낸다 (`+0x27e ~ +0x286`, `+0x1f1 ~ +0x21a`). 비트는 `0x120bf` 로 레벨 `0x29` · 옵션 `0x1b` 를 값 0 으로 설정해 성공했을 때 켜진다 (`AfdCompleteTliIoControlCore +0x1d2 BTS` / `+0x226 BTR`). 새 엔드포인트는 꺼짐.

### 3. 작은 버퍼 [정적]

| 출력 길이 | 주소 0 개 | 주소 1 개 IPv4 (필요 22 B) |
|---|---|---|
| 0 ~ 3 | `0xC000000D`, Info 0, 안 씀 | 같음 |
| 4 | status 0, Info 4, `+0` = 0 | **`0x80000005`, Info 22, `+0` = 1** (본문 없음) |
| 8 | status 0, Info 4, `+0` = 0 (`+4 ~ +7` 그대로) | **`0x80000005`, Info 22, `+0` = 1** (`+4 ~ +7` 그대로) |
| 22 이상 | status 0, Info 4 | status 0, Info 22, 본문 18 B |

- Information = 필요한 전체 크기 (넘쳐도 `+0x2bd ~ +0x2c1` 이 누적 크기를 그대로 씀).

### 4. 목록의 출처 [정적 + 추측]

- 목록 = **컴파트먼트 (망 구획) 객체 `+0xa8` 의 연결 목록**. 컴파트먼트는 엔드포인트 `+0x58` 에 달리고, 없으면 처음 물을 때 만든다 (`AfdGetCompartment`). 번호는 `AfdQueryCompartmentId` — 조건이 맞으면 TL 에 `SO_COMPARTMENT_ID` (레벨 `0xffff` · 옵션 `0x3004`) 를 묻고, 아니면 1 (기본) `[추측: 옵션 이름은 값 기준]`.
- **처음 한 번 채우기** (`AfdInitializeAddressList`, 계열마다 한 번): ① 기본 컴파트먼트면 `TdiRegisterPnPHandlers` ② IPv4 · IPv6 각각 `NsiRegisterChangeNotificationEx` (콜백 `AfdNsiAddressChangeEvent`). 실패는 **물은 계열 것만** 오류로 올림.
- **채우는 쪽** = NSI 콜백. 알림 종류 3 = 처음 전부 읽기 (`NsiAllocateAndGetTable`), 2 = 지움, 그 밖 = 추가 · 바뀜. PASSIVE 가 아니면 작업 항목으로 미룸.
- **거르는 줄** (`AfdNsiGetAddressProperties +0x150 ~ +0x18a`, `AfdNsiAddressChangeEventAtPassive +0x1be`):
  - 인터페이스 칸 `+4` 가 **`0x18`** 이고 주소 범위가 3 보다 작으면 뺀다 → **루프백 `127.0.0.1` · `::1` 은 안 들어감**. `0x18` = `IF_TYPE_SOFTWARE_LOOPBACK` (24) 와 같은 값 `[추측: 칸 이름은 PDB 에 없음]`.
  - 주소 중복 검사 상태 (`DadState` 로 보이는 칸) 가 **4 (선호)** 인 것만 넣는다 `[추측: 칸 이름]`.
- TDI 쪽 `AfdAddAddressHandler` 는 계열 2 · `0x17` 을 버린다 (`+0xa ~ +0x1e`) → IPv4 · IPv6 는 **NSI 로만** 들어온다.
- **주소 0 개는 정상 갈래**: 목록이 비면 루프 (`+0x1a2 ~ +0x1ac`) 가 바로 `+0x298` 로 가서 status 0 · Information 4.
- 망 없는 PC 에 무엇이 남나 (끊긴 어댑터 · 링크 로컬 `169.254.x.x` · `fe80::`) 는 NSI · tcpip 몫 `[공백]`. 정적으로 말할 수 있는 건 "루프백 인터페이스의 주소는 안 들어간다" 까지.

### 5. mswsock `WSPIoctl` (rva `0x191b0`, 8547B) — 내는 줄 · 읽는 줄 · 다음 크기 [정적]

**내는 줄** (`SIO_ADDRESS_LIST_QUERY` = 제어 코드 `0x48000016`, `+0xb20 ~ +0xb26 → +0xce8`):

| 줄 | 하는 일 |
|---|---|
| `+0xcf3 ~ +0xd0f` | IOSB (`RSP+0xf8`, 16 B) 0 · **4 B 스택 칸 (`RSP+0x110`) 0** · 입력 칸 0 |
| `+0xd17 ~ +0xd1f` | `*lpcbBytesReturned = 0` |
| `+0xd22 ~ +0xd3c` | `cbInBuffer ≠ 0` → WSAEINVAL (`0x2726`) |
| `+0xd41 ~ +0xd72` | 출력 버퍼 · 길이가 있으면 **`cbOutBuffer` B 힙** (플래그 0 = 0 으로 안 채움). 못 잡으면 WSAENOBUFS (`0x2747`) |
| `+0xda0 ~ +0xdc0` | 없으면 "크기만 묻기" — 출력 = 4 B 스택 칸, 길이 4 |
| `+0xdc7 ~ +0xdcb` | **입력 2 B = 소켓 구조체 `+0x1c` (만들 때의 계열)** |
| `+0xdd3 ~ +0xe1d` | `NtDeviceIoControlFile(소켓, 이벤트 = 스레드 자료 +0x10, APC 0, IOSB = RSP+0xf8, 0x120b3, 입력 2 B, 출력, 길이)` |

→ 요청 쪽 로그의 "입력 길이 2 · IOSB = 입력 + `0x10` · 스레드 이벤트 · APC 0" 과 모두 같다. "N B 힙 → 4 B 스택 → 8 B 힙" 은 부른 쪽이 `WSAIoctl` 을 (큰 버퍼 → 버퍼 없이 → 8 B) 세 번 부른 모양 `[추측: 부른 쪽 (ws2_32 · 앱) 은 안 봄]`.

**읽는 줄**:

| 무엇 | 보는 것 | 줄 |
|---|---|---|
| 반환 `EAX` | `0x103` 이면 기다린 뒤 `IOSB.Status`, 아니면 반환값 그대로. **PENDING 이 아니면 IOSB 를 안 읽는다** | `+0xe29 ~ +0xe58` |
| status | **`0` 또는 `0x80000005` 만 계속.** 그 밖 → `NtStatusToSocketError` → `*lpErrno` · −1 (`*lpcbBytesReturned` 는 0 그대로) | `+0xe5f ~ +0xe76` |
| 출력 `+0` | **개수** | `+0xe7b` |
| `IOSB.Information` | **읽지 않는다** (함수 전체에 `[RSP+0x100]` 읽기 없음 — 결과서 `grep`) | |
| 소켓 도우미 정보 `+0x24` | `MaxSockaddrLength` (레지스트리 값, `SockLoadHelperDll +0x2e8`), 8 로 올림 = A | `+0xe8a ~ +0xe9d` |

**다음 크기 셈** (`+0xe7b ~ +0xedd`): `*lpcbBytesReturned = 8 + 개수 × (16 + A)`.
- 16 = `SOCKET_ADDRESS` 한 칸 (포인터 8 + 길이 4 + 빈칸 4), A = 계열별 sockaddr 자리 (IPv4 보통 16 · IPv6 보통 32 `[추측: 레지스트리 값은 PC 마다]`).
- 곱셈이 `0x7fffffff` 를 넘으면 크기를 안 쓰고 WSAEFAULT (`+0xeb3 ~ +0xec9` → `+0xfb2`). 그보다 **작은 쓰레기 개수**는 보호를 지나 큰 크기를 쓴 뒤 WSAEFAULT 로 간다 → 부른 쪽이 그 크기로 새로 할당 `[추측: 64 MiB 넘는 할당과 맞는 모양 — 개수 약 `0x200000` 이면 `8 + 32 × 개수` ≈ 64 MiB]`.
- 크기를 쓴 뒤 오류로 가는 갈래 (`*lpcbBytesReturned` = 필요 크기 + **WSAEFAULT** `0x271e`): status `0x80000005` · 버퍼 없음 · 필요 > `cbOutBuffer` (`+0xedf ~ +0xef7`). → 이것이 "크기 묻기" 의 정직한 대답.
- **재시도 고리 없음** — `0x120b3` 을 한 번만 낸다.

**사용자 버퍼에 쓰는 줄** (`SOCKET_ADDRESS_LIST` 꼴, 임시 힙은 읽기만): `[out+0]` = 개수 (`+0xf27`), `[out + 8 + 16·i]` = `lpSockaddr` (**사용자 버퍼 안 절대 주소**), `[out + 0x10 + 16·i]` = `AddressLength + 2`, 본문은 칸의 `AddressType` 부터 복사 (`+0xf46 ~ +0xf7d`). → afd 출력과 사용자 출력은 **다른 버퍼**다.

**다른 부르는 곳**: `SockpCreateV6AddressMap +0xbe` (구역 `SANONTCP`) — 계열 `0x17` 고정이고, **`0x80000005` 면 `IOSB.Information` 만큼 다시 잡아 또 부르는 고리**가 있다 (`+0xca ~ +0xcf → +0x3b`). 여기선 Information 이 필요 크기여야 끝난다. WinHttp 로그 길은 아니다 `[추측: 부르는 곳 안 봄]`.

### 6. 최소 응답 — 망 없는 PC (주소 0 개) [정적 + 추측]

입력 2 B = 계열, 입력 길이 2, 요청 모드 유저.

| 출력 길이 | 반환 status | `IOSB.Status` | `IOSB.Information` | 출력 | mswsock 결과 |
|---|---|---|---|---|---|
| `0x200` (힙) | 0 | 0 | 4 | `00 00 00 00`, 나머지 그대로 | 성공, `*lpcb` = 8 |
| 4 (버퍼 없음) | 0 | 0 | 4 | `00 00 00 00` | `*lpcb` = 8 + WSAEFAULT |
| 8 (힙) | 0 | 0 | 4 | `00 00 00 00`, `+4 ~ +7` 그대로 | 성공, `*lpcb` = 8 |

- 주소 1 개 (IPv4 `192.168.0.5`, 계열 2) 예: `0x200` → status 0 · Info `0x16` · `01 00 00 00 0e 00 02 00 00 00 c0 a8 00 05` + 0 8 B; 4 · 8 → `0x80000005` · Info `0x16` · `+0` = 1 만. mswsock 크기 = `8 + 1 × (16 + 16)` = 40 (A = 16 가정 `[추측]`).
- IOSB 는 빠른 길이 정한다 — `IoStatus.Status` = 반환값, `Information` = 위 값을 `IopXxxControlFile +0xb38 ~ +0xb48` 이 16 B 통째로 복사 (11절).
- "망 없는 PC = 선호 상태의 비루프백 주소 없음" 은 `[추측]` — 진짜 VM 에서 비는지는 동적 1.

### 7. `0x120bf` — afd `AfdTliIoControl` (rva `0x26430`, 3144B) · mswsock 부르는 쪽 [정적]

`0x120bf` → `AfdIoctlTable` 47번 칸 → **IRP 길** (`AfdIrpCallDispatch`).

**입력 `0x20`** (64 비트; 32 비트는 `0x18` 꼴):

| 오프셋 | 크기 | 뜻 | 줄 |
|---|---|---|---|
| `+0x00` | 4 | **종류**: 1 = 설정 (`WSPSetSockOpt`) · 2 = 읽기 (`WSPGetSockOpt`, afd 스스로 컴파트먼트 물을 때도) · 3 = 소켓 ioctl (`WSPIoctl`). 그 밖 `0xC000000D` `[추측: 이름]` | `AfdAllowedUserIOControlRequest +0xb ~ +0x2d` |
| `+0x04` | 4 | 레벨. 종류 1 · 2 는 `0xfffc` · `0xfffd` 거절, 종류 3 은 0 만 | `+0x13 ~ +0x2d` |
| `+0x08` | 4 | 옵션 이름 / ioctl 코드 | `WSPIoctl +0x1fa9` |
| `+0x0c` | 1 | **반드시 1** | `AfdAllowedUserIOControlRequest +0x6 ~ +0x9` |
| `+0x10` · `+0x18` | 8 · 8 | 값 포인터 · 길이 (읽기는 0 · 0) | `WSPSetSockOpt +0x1a7 ~ +0x1b2`, `WSPIoctl +0x1fb8 ~ +0x1fc0` |

**afd 가 스스로 하는 것 · TL 로 넘기는 것**:
- 스스로: 입력 검사 (TL 엔드포인트 아님 `0xC0000010` · 길이 < `0x20` 또는 허용 표 밖 `0xC000000D` · 정렬 `0xC00002C5` · 못 읽음 `0xC0000005` · 묶는 중 / 종류 안 맞음 `0xC0000184`), `SIO_IDEAL_SEND_BACKLOG` 류 (`AfdHandleISBIoctls`, 안 읽음 `[공백]`), QoS 두 코드 앞처리 (안 읽음).
- **그 밖은 TL** (`+0x7eb` 일반 · `+0x8c8` 연결 · `+0xae3` · `+0xb75` TDI). 출력 포인터는 MDL 로 잠가 TL 에 건넬 뿐 (`+0x147 ~ +0x1df`, `+0x77d ~ +0x7b1`) — **afd 는 출력에 안 쓴다.**
- 완료 (`AfdCompleteTliIoControlCore`): IRP status = TL status (단 `0xC0000206` → **`0xC0000023`**, `+0xa6 ~ +0xb5`), Information = TL 이 준 값 (`+0xac`, 안 주면 0). TL 이 `0x103` 이면 IRP 보류 (`AfdTLPendRequest`).
- → 출력 바이트 · Information · 작은 버퍼 status 는 **TL (tcpip) 몫** `[공백]`.

**mswsock 부르는 쪽** (IOSB 위치로 셋이 갈린다):

| 내는 줄 (ret) | 종류 | IOSB | 출력 | 보는 것 |
|---|---|---|---|---|
| `WSPSetSockOpt +0x1fd` (`+0x204`) | 1 | 입력 `+0x20` | 없음 | status 부호만, PENDING 이면 `IOSB.Status` |
| `WSPGetSockOpt +0x7c4` (`+0x7cb`) | 2 | 입력 `−0x10` | `optval` · `*optlen` | 음수면 오류 (`0xC00000BB` → WSAENOPROTOOPT `0x273a`), **`IOSB.Information` → `*optlen`** |
| `WSPIoctl +0x2007` (`+0x200e`) | 3 | 입력 **`+0x30`** | `lpvOutBuffer` · `cbOutBuffer` | 0 → `*lpcbBytesReturned = IOSB.Information`; `0x103` → 겹침이면 997, 아니면 기다림; 음수 → 오류 + Information |

→ 요청 쪽 로그 "입력 `0x20` · 출력 8 B 힙 · IOSB = 입력 + `0x30`" 은 **`WSPIoctl` (종류 3) 만** 맞는다. 어떤 코드인지는 로그의 입력 `+0x08` DWORD 를 보면 안다 — 정적으로는 앱이 고른 값이라 못 가린다 `[공백]`.

### 8. `0x12003` — afd `AfdBind` (rva `0x39160`, 2997B) · TL bind 사슬 [정적]

TL 모드 = 엔드포인트 `+8` 비트 8 (`+0x43 ~ +0x4a`). TDI 모드 갈래 (`+0x4c ~ +0x76` 등) 는 안 풀었다 `[공백]`.

| 차례 | 조건 | status | 줄 (`AfdBind.asm`) |
|---|---|---|---|
| 1 | 엔드포인트 `+5` 비트 `0x10` 과 `+6` 비트 1 이 둘 다 켜짐 | `0xC0000022` | `+0x7a ~ +0x93` |
| 2 · 3 · 4 | 입력 < 6 · 출력 < 2 · 출력 < 입력 − 4 | `0xC000000D` | `+0x98 ~ +0xbf` |
| 5 | `[ep+0x170]` 0 → 3 바꾸기 실패 (다른 TL 요청 중) | `0xC000000D` | `+0xc5 ~ +0xe0` |
| 6 | 엔드포인트 종류 `[ep+2]` 가 1 · 2 아님 (이미 묶임 · 연결됨) | **`0xC0000238`** | `+0xe5 ~ +0x10d` |
| 7 · 8 | 입력 정렬 · 못 읽음 | `0xC00002C5` · `0xC0000005` | `+0x13d ~ +0x177` |
| 9 | 계열 (입력 `+4` WORD) ≥ `0x23` | **`0xC0000141`** | `+0x216 ~ +0x22c` |
| 10 | sockaddr 바이트 (입력 − 4) < `SOCKADDR_SIZE(계열)` | **`0xC0000141`** | `+0x24a ~ +0x26c` |
| 11 | 출력 MDL 실패 | `0xC000009A` / 예외 코드 | `+0x29c ~ +0x351` |
| 12 | **성공 갈래** | **`0x103` (PENDING)** — 결과는 TL 완료가 냄 | `+0x35b ~ +0x3ae` |

- 입력 = 출력 같은 버퍼: `+0x00` 공유 방식, `+0x04 ~` sockaddr.
- 성공 갈래 사슬: `AfdTLBindSecurity` (공유 방식 1 · 3 이면 TL 에 재사용 · 독점 옵션 `[추측: 이름은 값 기준]` 을 먼저) → `AfdTLBind` (TL 제공자 bind, 콜백 `AfdTLBindComplete`) → `AfdTLBindComplete2` (실패: 정리 · `0xC0000043` · `0xC000020A` + 공유 방식 1 이면 `0xC0000022`. 성공: `[ep+2]` ← **3** (연결 객체가 붙는 소켓은 4), TL 에 로컬 주소 묻기) → `AfdTLBindGetAddrComplete` (**출력 `+0` 에 로컬 sockaddr** (`TdiCopyBufferToMdl`), Information = TL 이 준 바이트 수, Status = 복사 결과 보통 0, `[ep+0x170]` ← 0, `IofCompleteRequest`).
- **임시 포트는 TL (tcpip) 이 고른다** — afd 에 포트 칸을 쓰는 줄이 없다.
- 입력 길이 `0x10` 은 sockaddr 12 B 라 **AF_INET 이면 10번에 걸린다** (`SOCKADDR_SIZE(2)` = 16). mswsock `WSPBind` 의 TL 모드 입력은 `namelen + 4` (`+0x229`) 라 IPv4 정상값은 `0x14`. 요청 쪽 로그의 `0x10` 이 어떤 호출인지는 `[공백]`.
- mswsock `WSPBind` 쪽 (sast/121 `.asm`): PENDING 이면 기다린 뒤 `IOSB.Status`, 음수 → `NtStatusToSocketError`, 성공이면 **`IOSB.Information` = 묶인 주소 길이** 만큼 출력에서 소켓 로컬 주소 칸 `[sock+0xa8]` 로 복사 (`+0x45b` · `+0x4bf`).

### 9. afd `SOCKADDR_SIZE` (rva `0x5b40`, 29B) [정적]

계열 < `0x23` 이면 `.rdata 0x7ad80` 표 (`sockaddr_size`) 한 바이트, 아니면 표 `[0]`.

| 계열 | 길이 |
|---|---|
| 0 | 0 |
| 1 (AF_UNIX) | 110 |
| **2 (AF_INET)** | **16** |
| **`0x17` (AF_INET6)** | **28** |
| `0x22` | 36 `[추측: AF_HYPERV 와 같은 번호]` |
| 3 ~ `0x16`, `0x18` ~ `0x21` | 0 (길이 검사가 늘 통과) |
| ≥ `0x23` | 표 `[0]` = 0 — 부르는 쪽 (`AfdBind +0x219` · `AfdConnect`) 이 먼저 `0xC0000141` |

부르는 곳: `AfdBind +0x24a` · `AfdConnect +0x514` (run124) · `AfdSuperConnect +0x1d3` · WSK · 데이터그램 함수들 (안 읽음).

### 10. IRP 길 완료 — `nt!IopSynchronousServiceTail` (rva `0x8eb0e0`) · `IopfCompleteRequest` (`0x2b2318`) · `IopCompleteRequest` (`0x214750`) [정적]

**누가 언제 완료를 하나** (`NtDeviceIoControlFile` 길):
- `NtDeviceIoControlFile +0x13` 이 마지막 인자에 **1** 을 쓴다 (`NtFsControlFile` 은 0). 그래서 `IopXxxControlFile` 이 DEFER 비트 (`Irp.Flags 0x800`) 를 안 세우고 (`+0x937 ~ +0x93b`), `IopSynchronousServiceTail` 넷째 인자 = 0.
- → 드라이버가 `IoCompleteRequest` 를 부르는 **그 자리에서** `IofCompleteRequest +0xe` → `IopfCompleteRequest` → `IopCompleteRequest` 를 바로 (`+0x62d`) 또는 **특수 커널 APC 로 미뤄** (`+0x684` · `+0x3eb`). 미루는 조건: IRP 를 만든 스레드가 지금 스레드가 아님 · 모든 APC 꺼짐 · 다른 프로세스에 붙음 · 취소됨 (`+0x5be ~ +0x5ef`).
- 어느 쪽이든 같은 스레드가 유저로 돌아가기 전에 끝난다 `[추측: 특수 커널 APC 는 IRQL 이 내려갈 때 배달되는 보통 동작]`. 어느 갈래를 타는지는 동적.
- (비교) `NtFsControlFile` 길은 DEFER 라 `IopSynchronousServiceTail +0x39c` 가 직접 부른다. 효과는 같다.

**PENDING 없이 끝났을 때** (`Irp+0x41 PendingReturned` = 0). `NT_ERROR` 검사 = `(Status & 0xC0000000) == 0xC0000000` (`IopCompleteRequest +0xb3 ~ +0xbf`):

| | 반환 | IOSB | 사용자 이벤트 | 파일 객체 이벤트 · `FinalStatus` | 완료 포트 | 사용자 APC |
|---|---|---|---|---|---|---|
| **성공 / 동기 핸들** | 드라이버 status | **씀** (`+0x125 ~ +0x142`) | 줬으면 **세움** (`+0x3d5`) | **세움** + `FinalStatus` (`+0x2ba` · `+0x2c1`) | `CompletionContext` 가 있으면 비동기와 같음 (동기 핸들에 붙는지는 `[공백]`) | `ApcRoutine ≠ 0` 이면 (`+0x351`) |
| **성공 / 비동기 핸들** | 드라이버 status | **씀** | 줬으면 **세움** | 이벤트를 **안 줬을 때만** 세움 (+ `FO_SKIP_SET_EVENT` 꺼짐) | 포트 + `ApcRoutine == 0` + `ApcContext ≠ 0` 이면 패킷 (`+0x3b4`). `FO_SKIP_COMPLETION_PORT` 면 성공은 건너뜀 | 같음 |
| **경고 / 동기** | 드라이버 status | **씀** | 세움 | 성공 / 동기와 같음 | 같음 | 같음 |
| **경고 / 비동기** | 드라이버 status — **포트 연결 핸들이면 `0x103`** (`IopSynchronousServiceTail +0x403 ~ +0x41b`) | **씀** | 세움 | 성공 / 비동기와 같음 | `FO_SKIP_COMPLETION_PORT` 여도 **경고는 넣음** (`+0x5b6 ~ +0x5c3`) | 같음 |
| **오류 / 동기 · 비동기** | 드라이버 status | **안 씀** | **안 세움** (참조만 풂, `+0x4c1 ~ +0x4ec`) | **안 세움**, `FinalStatus` 그대로 | 없음 (`+0x426 → +0x445`) | 없음 (`+0x180 ~ +0x198` → `+0x1db IoFreeIrp`) |

- IOSB 는 Information 먼저 (`+0x133` / `+0x12f`), Status 나중 (`+0x142`). 32 비트 호출자면 4 + 4 (`+0x13c`) — 표식은 `ApcRoutine` 맨 아랫 비트 (`IopSynchronousServiceTail +0x34 ~ +0x40`).
- IOSB 쓰기는 예외를 삼킨다 — 필터가 늘 1 을 돌려주고 착지가 이벤트 단계로 이어진다 (결과서 `+0x146 ~ +0x169` 원본 바이트). IOSB 주소가 나빠도 syscall 은 안 죽는다.
- 사용자 APC = `KeInsertQueueApc(Tail.Apc, UserIosb, 0, 2)`; 커널 루틴 · 정리 루틴은 같은 주소 (PDB 이름 둘: `IopUserCompletion` · `IopUserRundown`), 보통 루틴 = `ApcRoutine`, 문맥 = `ApcContext` → 사용자 APC 는 (ApcContext, IOSB, 0) 으로 불릴 모양 `[정적]` (배달 안쪽은 안 읽음).
- 완료 패킷 = `KeInsertQueueEx(포트, &Irp->Tail.Overlay.ListEntry, 플래그)`, 키는 Tail 첫 칸 (`+0x35b ~ +0x3b4`).
- 반환이 드라이버 값과 **달라지는 경우** (찾은 것 전부): ① 동기 + PENDING → `IopWaitForSynchronousIo` 가 기다린 뒤 `FileObject->FinalStatus` ② 비동기 + 포트 + 경고 → `0x103` ③ IoRing → `0x103` (범위 밖) ④ AppContainer + 특정 파일 객체 → 드라이버 안 부르고 `0xC0000910` (`IopSynchronousServiceTail +0x186 ~ +0x1b6`) `[추측: 대상은 정적으로 모름]`.
- **PENDING 일 때** (참고): 동기 핸들은 `IopSynchronousServiceTail +0x3e1 IopWaitForSynchronousIo` 가 **파일 객체 이벤트** (`FileObject+0x98`) 를 기다린다 (호출 앞 `IopXxxControlFile +0x730` 에서 비워 둔 것과 짝). 비동기 핸들은 바로 `0x103`.

IRP · 파일 객체 칸 (`pdb_struct.py`, 결과서): Irp `+0x10` Flags · `+0x30` Status · `+0x38` Information · `+0x41` PendingReturned · `+0x47` AllocationFlags (`0x10` = 32 비트 IOSB) · `+0x48` UserIosb · `+0x50` UserEvent · `+0x58` ApcRoutine · `+0x60` ApcContext. FileObject `+0x38` FinalStatus · `+0x98` Event · `+0xb0` CompletionContext.

### 11. 빠른 길 완료 — `nt!IopXxxControlFile +0x6dc ~ +0x70f` · `+0xb16 ~ +0xc30` [정적, flow-writer 읽기]

요청서 · 결과서 둘 다 "비교용" 으로만 짚은 줄을 flow-writer 가 `ms/disassemble/ntoskrnl/IopXxxControlFile.asm` 에서 이어 읽었다.

- 빠른 길 호출 (`+0x6dc PiCMFastIoDeviceDispatch`, 또는 떨어진 조각의 간접 호출 — 드라이버 FastIoDeviceControl `[추측]`) 의 반환 바이트 (`+0x6e4`) 가 0 이 아니면 `+0x70d ~ +0x70f` 에서 **`+0xb16` 으로**.
- `+0xb16 ~ +0xb66`: 32 비트 표식 (`+0xb2e`) 이 없으면 **`IoStatus` 16 B 를 사용자 IOSB 로 통째로 복사** (`+0xb38 ~ +0xb48`), 있으면 4 + 4 (`+0xb4d ~ +0xb66`).
- `+0xbb4 ~ +0xbbc`: 파일 객체 `+0xb0` (`CompletionContext`) 가 있으면 다른 갈래로 (안 읽음).
- `+0xbc2 ~ +0xbe4`: 사용자 이벤트가 있으면 (파일 객체 `+0x50` 비트 `0x8000000` 이 꺼져 있을 때) **`KeSetEvent`** → 참조 풂.
- `+0xc30`: 반환 = `IoStatus.Status`.
- **이 구간에 status 를 견주는 줄이 없다** → 빠른 길이 TRUE 면 **오류여도 IOSB 를 쓰고 이벤트를 세운다** `[정적: 줄 읽기]`. afd 는 즉시 처리 코드에서 처리 함수 반환값을 `IoStatus.Status` 에 쓰고 TRUE (요청서 `AfdFastIoDeviceControl +0x15d9 ~ +0x160a`, 결과서 R1) — 그래서 `0x120b3` 이 `0xC000000D` 를 내도 IOSB = (`0xC000000D`, 0) 이 써지고 이벤트가 선다 `[추측: 이 둘을 이은 것. afd 가 어떤 경우 FALSE 를 돌려 IRP 길로 넘기는지는 안 읽음]`.

### 호출 관계

```
(PE_emulator 요청 — test.exe 초기화 밖. 소켓 하나에서 WSAIoctl(SIO_ADDRESS_LIST_QUERY))
mswsock!WSPIoctl                                       코드 0x48000016 → +0xce8
 │  +0xcf3 ~ +0xd0f  IOSB 0 · 4 B 스택 칸 0
 ├─ (1) +0xd72  [간접] SockAllocateHeapRoutine         cbOutBuffer B (0 안 채움) — 버퍼 없으면 4 B 스택
 ├─ (2) +0xe24  NtDeviceIoControlFile 0x120b3 (입력 2 B = 계열)
 │        ⇒ nt!NtDeviceIoControlFile +0x5e IopXxxControlFile
 │             └─ 빠른 길 (+0x70f → +0xb16) ⇒ afd!AfdFastIoDeviceControl +0x1608 [간접 44번 칸] AfdAddressListQuery
 │                    ├─ +0xe6   ProbeForWrite
 │                    │  +0x109 출력 +0 = 0
 │                    ├─ +0x124  AfdQueryCompartmentId
 │                    ├─ +0x464  AfdInitializeAddressList   (계열마다 처음 한 번)
 │                    │            ├─ +0x177 TdiRegisterPnPHandlers
 │                    │            └─ +0x200 · +0x27e NsiRegisterChangeNotificationEx  ⇢ 콜백 AfdNsiAddressChangeEvent
 │                    │  +0x1a2 ~ +0x1ac 목록 돌기 · +0x31d 개수 ++ · +0x24b 칸 복사 (들어갈 때만)
 │                    └─ +0x2bd ~ +0x2c4  *Information = 누적 크기 · 반환 0 / 0x80000005
 │               +0xb38 ~ +0xb48 IOSB 16 B 복사 · +0xbe9 KeSetEvent   (status 안 봄)
 ├─ (3) +0xe51  SockWaitForSingleObject                  (PENDING 일 때만)
 ├─ (4) +0xe70  NtStatusToSocketError                    (0 · 0x80000005 밖)
 │  +0xe7b 개수 → +0xedd *lpcbBytesReturned = 8 + 개수 × (16 + A)
 └─ +0xf27 ~ +0xf7d  사용자 SOCKET_ADDRESS_LIST 짓기

(IRP 길 — 0x120bf · 0x12003 등)
nt!IopXxxControlFile
 │  +0x2db 사용자 이벤트 비움 · (DeviceIoControl=1 → DEFER 안 세움, +0x937)
 └─ +0x99c  IopSynchronousServiceTail
          ├─ (1) +0x30b  IopCallDriverReference ⇒ afd!AfdTliIoControl / AfdBind …
          │                 드라이버의 IoCompleteRequest ─ IofCompleteRequest +0x13 IopfCompleteRequest
          │                     ├─ +0x632  IopCompleteRequest           (그 자리)
          │                     └─ +0x689 KeInitializeApc · +0x3f0 KeInsertQueueApc  (특수 커널 APC 로 미룸)
          │                 IopCompleteRequest: 오류면 +0x4c1 이벤트 참조만 풂 → +0x1e0 IoFreeIrp
          │                                     아니면 +0x125 ~ +0x142 IOSB · +0x3da KeSetEvent · +0x2bf KeSetEvent (파일)
          │                                            · +0x356 KeInsertQueueApc · +0x3b9 KeInsertQueueEx
          ├─ (2) +0x3e6  IopWaitForSynchronousIo                (동기 핸들 + PENDING)
          └─ (3) +0x3f5  IopReleaseFileObjectLock               (동기 핸들)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 들여쓴 `+0x..` 범위는 그 함수 안 줄. `⇒` 는 syscall 너머 / 드라이버로, `⇢` 는 콜백 등록 (나중에 불림).
- `IopCompleteRequest` 안 줄은 한 함수의 갈래라 한 줄에 몰았다.

## 확인된 것

- `[정적]` `0x120b3` 을 내는 mswsock 줄은 둘: `WSPIoctl +0xe1d` (WinHttp 로그 길) · `SockpCreateV6AddressMap +0xbe`. 받는 afd 함수는 `AfdAddressListQuery` 하나 (표 44번 칸).
- `[정적]` afd 는 출력 길이 ≥ 4 이면 `+0x109` 에서 개수 칸을 0 으로 쓰고 `+0x31d` 에서 칸마다 올린다 — 넘쳐도 센다. 넘치면 `0x80000005` + Information 필요 크기.
- `[정적]` 주소 0 개 = status 0 · Information 4. 엔드포인트 상태 · 계열 값으로 거절하는 줄 없음.
- `[정적]` 루프백 인터페이스 (`+4` = `0x18`) + 범위 < 3 인 주소는 목록에 안 들어간다 (`AfdNsiGetAddressProperties +0x17b ~ +0x180`).
- `[정적]` mswsock `WSPIoctl` 은 `IOSB.Information` 을 안 읽고, 개수 하나와 `MaxSockaddrLength` 로 `8 + 개수 × (16 + A)` 를 셈한다 (`+0xe7b ~ +0xedd`). 재시도 고리 없음.
- `[정적]` `0x120bf` 에서 afd 는 출력에 안 쓴다 (TL 몫). 종류 1 · 2 · 3 과 IOSB 위치 (`+0x20` · `−0x10` · `+0x30`) 로 부른 함수가 갈린다.
- `[정적]` `AfdBind` 성공은 `0x103`, 결과 (출력 sockaddr · Information) 는 `AfdTLBindGetAddrComplete` 가 IRP 에. 임시 포트는 TL.
- `[정적]` `SOCKADDR_SIZE` 표 — 2 → 16 · `0x17` → 28.
- `[정적]` IRP 길 + PENDING 없음 + 오류 → IOSB · 이벤트 · `FinalStatus` · APC · 포트 모두 안 건드림 (`IopCompleteRequest +0xb3 ~ +0xbf` → `+0x426` · `+0x4b7`). 성공 · 경고 → IOSB · 이벤트 · (APC 또는 포트).
- `[정적]` `NtDeviceIoControlFile` 은 DEFER 를 안 써서 완료가 드라이버의 `IoCompleteRequest` 자리에서 일어난다 (`NtDeviceIoControlFile +0x13`, `IopXxxControlFile +0x937 ~ +0x93b`).
- `[정적]` (flow-writer) 빠른 길이 TRUE 면 IOSB 복사 · 이벤트 세우기 사이에 status 검사가 없다 (`IopXxxControlFile +0xb16 ~ +0xbe4`).

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- sast/124 "`IopWaitForSynchronousIo +0x60 ~ +0x7a` … `KeWaitForSingleObject`" — `_calls.tsv` 의 이름은 `alias:KeWaitForMutexObject` (call `+0x7a`, ret `+0x7f`). Ghidra 가 같은 주소의 다른 이름을 고른 것으로 보인다 `[추측: 같은 주소 별칭]` — 하는 일 (기다림) 은 같다.
- sast/124 "`IopXxxControlFile +0x2db` `KeClearEvent`" — `.asm` 은 `alias:KeClearEvent` + 별칭 표시 `KeResetEvent`. `pdb_aliases.py --lookup ntoskrnl.exe 0x300f60` 는 "별칭 무리 아님" 이라 답했다 (표기 차이). 이 문서는 "이벤트 비우기" 로 적었다.
- sast/123 은 `0x120bf` 를 내는 줄을 즉치 자리 (`WSPIoctl +0x1fef` · `WSPGetSockOpt +0x7a0` · `WSPSetSockOpt +0x1d6`) 로 적었다. 이 문서의 call 자리는 `_calls.tsv` 값 (`+0x2007` · `+0x7c4` · `+0x1fd`).
- sast/123 은 `AfdAddressListQuery +0x24b` 를 "통째로 복사" 로 적었다. `_calls.tsv` 이름은 `alias:memmove` — 이름 대신 "칸 복사" 로 적었다.

## 정정

- **run124** (`runs/03_sprint/run124-afd-socket.md`) — 정정 아님, **닫힘 · 고쳐 읽기**. 맨 위에 한 줄:
  - "아직 확인 필요" 정적 ② 중 `AfdBind` (TL 출력) · `AfdTliIoControl` 몫 → 닫힘 (위 7 · 8절). `AfdSetInformation` · `AfdQueryHandles` · `AfdSuperConnect` · `AfdGetAddress` / `AfdGetRemoteAddress` 는 그대로 남음.
  - 정적 ⑥ `IopXxxControlFile` 이벤트 규칙 → 닫힘 (10 · 11절).
  - "PE_emulator 쪽" 의 "끝나면 세운다 (`+0xbe4/+0xbe9 KeSetEvent`) … `[추측]`" 은 **빠른 길 줄**이다. IRP 길은 `IopCompleteRequest +0x3d5` 가 세우고, **오류 + PENDING 없음이면 안 세운다.**
  - 5절 afd 검사 "`SOCKADDR_SIZE(family)`" 의 값 — 2 → 16 · `0x17` → 28 (9절).
  - 동적 ④ "`0x12003` 출력 포트 · Information" 은 정적으로 꼴이 정해짐 (출력 `+0` 로컬 sockaddr, Information = 길이, 포트는 TL) — 값은 그대로 동적.

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 코드 | 지금 (결과서) | 해야 할 일 |
|---|---|---|
| `0x120b3` | **틀림 (없음)** — afd 갈래 기본 대답 (`fake()` / `device_control_code_unverified`), 개수 · Information 안 씀 → **K03 막힘** | 6절 최소 응답. **개수를 늘 쓰는 것**이 핵심. 출력 < 4 → `0xC000000D`. 주소 목록은 "망 없는 PC = 0 개" 정책값 + `TODO(VM)` |
| `0x120bf` | 틀림 (없음) | 입력 검사 (길이 < `0x20` · 종류 1 · 2 · 3 밖 · 표지 ≠ 1 → `0xC000000D`) + 그 밖은 TL 몫 정책값 · `TODO(VM)`. 급하지 않음 |
| `0x12003` | 틀림 (기본 대답, 묶임 상태 없음) | 검사 표 + 완료 값 (status 0 · Information = sockaddr 길이 · 출력에 sockaddr). 진짜는 `0x103` 뒤 완료지만 동기 완료로 줘도 `WSPBind` 는 같은 결과 `[정적: PENDING 아니면 반환값만 봄]` |
| `SOCKADDR_SIZE` | 빠짐 (`AfdConnect` 줄임에 "asm 없음") | 9절 표 |
| IRP 길 IOCTL 완료 (K02 "status 만 · IOSB 안 씀 · 이벤트 비운 채") | **오류 때만 맞음**, 성공 · 경고 때 틀림 | 성공 · 경고: (출력) → IOSB (Information 먼저, Status 나중) → 사용자 이벤트 → 파일 객체 이벤트 (이벤트 안 줬거나 동기 핸들) + `FinalStatus` → APC 또는 포트 패킷 → 반환 그대로 (비동기 + 포트 + 경고만 `0x103`). mswsock 은 PENDING 아니면 IOSB · 이벤트를 안 봐서 그쪽 눈에는 안 드러남 |
| 빠른 길 (afd 즉시 처리) 완료 | (결과서 언급 없음) | flow-writer 보탬: 빠른 길은 **status 와 상관없이** IOSB 16 B + 이벤트 (11절). `0x120b3` · `0x1207b` · `0x12047` 은 이쪽 `[추측: afd 가 FALSE 를 돌리는 경우는 안 읽음]` |

- `tests/file.cpp` 에 얹을 케이스 (결과서): 주소 0 개 × 출력 길이 `0x200` · 4 · 8 · 3 · 0, 입력 길이 1, 출력 포인터 홀수 · NULL.

## 아직 확인 필요

동적 (대기열 번호는 오케스트레이터가 붙임. test.exe 가 아니라 **소켓을 쓰는 프로그램 + 선 뽑은 VM** — run124 동적 (Q224) 과 같은 세션에 얹을 수 있음. 멈춤점은 프로세스 조건):

1. 망 없는 VM 에서 목록이 비는가 — `afd!AfdAddressListQuery+0x19b` 에서 `dq @rbx+0xa8 L2` (`Flink == &Blink` 이면 빔), `dd @rbx+0xb8 L1`.
2. afd 가 내는 status · Information — `afd!AfdFastIoDeviceControl+0x1608` 에서 `eax` · `poi(rdi+8)` (출력 길이 `0x200` · 4 · 8 세 번).
3. 출력 본문 — `afd!AfdAddressListQuery+0x2a4` 에서 `db poi(@rsp+0x58) L40`.
4. 컴파트먼트 번호 — `+0x124` 의 `eax` · `dd @rsp+0x4c L1`, `AfdQueryCompartmentId+0x198` 가 걸리나.
5. 첫 채우기가 동기인가 — `AfdInitializeAddressList+0x200` 앞뒤로 `AfdNsiAddressChangeEvent` 입구가 먼저 불리나, `AfdAddressListQuery+0x464` 직후 목록이 찼나.
6. mswsock 값 — `mswsock!WSPIoctl+0xe24` (`eax` · `dq @rsp+0xf8 L2` · `db @r13 L20`), `+0xe9d` `r8d` (A), `+0xedd` 직전 `edx`.
7. "쓰레기 개수" — 에뮬레이터 쪽 `[r13]` (`WSPIoctl+0xe7b` 직전). 힙이 0 으로 오는지는 에뮬레이터 `RtlAllocateHeap` 몫 `[공백]`.
8. `0x120bf` 호출이 어떤 코드인지 — `mswsock!WSPIoctl+0x2007` 에서 `db @rsp+0x190 L20` (`+0x08`), 같은 눈금으로 `WSPSetSockOpt+0x1fd` · `WSPGetSockOpt+0x7c4`.
9. TL 이 준 status · Information — `afd!AfdCompleteTliIoControlCore+0xb9` (`esi` · `rbp`), `AfdTliIoControl+0x7f0` (`eax`), 작은 출력으로 `0xC0000206 → 0xC0000023` (`+0xa09`).
10. bind 최종 값 — `afd!AfdBind+0x3a9` (`eax` = `0x103`), `AfdTLBindGetAddrComplete+0xce` (`edi` · `ebp` · `db poi(@rsi+0xf0) L@ebp`), 임시 포트 범위.
11. bind 입력 길이 `0x10` 의 정체 — `afd!AfdBind+0x219` 의 `ecx` · `r12d`, `mswsock!WSPBind+0x3cf` 의 `[rsp+0x58]`.
12. (sast/124) afd IRP 가 PENDING 없이 끝나나 (`Irp+0x41`) · 완료가 그 자리 (`nt!IopfCompleteRequest+0x62d`) 인가 APC (`+0x684`) 인가 · `IopSynchronousServiceTail` 넷째 인자 0 · `Irp.Flags & 0x800` 없음 · 파일 객체 큐 갈래 (`+0xa4` 기능 스위치, `Flags & 0x2000`) · 동기 핸들에 포트가 붙나. 멈춤점 `nt!IopCompleteRequest+0x120` (IOSB 입구, `rbx` = Irp) · `+0x4c1` (오류 · 이벤트 안 건드림) · `+0x3d5` · `+0x2ba` · `+0x351` · `+0x3b4`, `nt!IopSynchronousServiceTail+0x30b` 와 `+0x420` (`eax` 비교).
13. (flow-writer 보탬) 빠른 길 완료 — `nt!IopXxxControlFile+0xb38` 이 `0x120b3` 오류 (출력 길이 3 등) 에도 걸리나, 그때 IOSB 값. afd 가 FALSE 를 돌려 IRP 길로 넘기는 경우가 있나.

정적 (결과서 "다음 요청 후보" 두 장 + 보탬):

1. ws2_32 `WSAIoctl` · `getsockopt` 바깥 껍질 — 부른 쪽 되풀이 · `*lpcbBytesReturned` 가 앱까지 가는 길 (run124 정적 ① 과 같이, ws2_32 analyze 필요).
2. netio `NsiRegisterChangeNotificationEx` / `NsiAllocateAndGetTable` 첫 알림이 동기인가 — 동적 5 가 먼저.
3. tcpip TL 제공자 (`getsockopt` · `setsockopt` · bind 포트 고르기 · `SO_COMPARTMENT_ID`) — 큰 모듈, 필요한 옵션을 에뮬레이터에서 먼저 좁히고.
4. afd 남은 칸 — `AfdGetAddress` · `AfdGetRemoteAddress` (`[ep+0xf0]` 쓰는 곳) · `AfdHandleISBIoctls` · `AfdSetInformation` · `AfdQueryHandles` · `AfdSuperConnect` (run124 정적 ② 남은 몫과 같음).
5. mswsock `WSPGetSockOpt` 의 SOL_SOCKET 옵션 표 — mswsock 이 스스로 답하는 것 · TL 로 가는 것.
6. ntoskrnl `IopUserCompletion` (= `IopUserRundown`) — 사용자 APC 배달 때 인자를 바꾸나.
7. ntoskrnl `IopCompleteIrpInFileObjectList` · `IopQueueIrpForServiceTail` — 포트 연결 + 이벤트 없는 비동기 IOCTL 길.
8. ntoskrnl `IoRemoveIoCompletion` / `KeInsertQueueEx` 패킷 모양 (`NtRemoveIoCompletion` 이 돌려주는 값) — run93 과 이어짐.
9. ntoskrnl `IopXxxControlFile` 의 METHOD 분기 (`+0x86b ~ +0x8b6`, `+0xf14 ~ +0xfd6`) — 버퍼드 IOCTL 출력이 언제 사용자 버퍼로 가나.
10. (flow-writer 보탬) afd `AfdFastIoDeviceControl` 이 FALSE 를 돌려 IRP 길로 넘기는 갈래 · 빠른 길 포트 연결 갈래 (`IopXxxControlFile +0xbbc` 뒤) — 11절의 `[추측]` 닫기.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서 둘 다 SEH 착지 틈 (Ghidra 가 "읽지 않은 틈" 으로 남김) 을 원본 바이트로 읽어 예외 때 status · IOSB 동작을 닫았다. sast/123 은 `.pdata` 범위표 풀이를 `.asm` 틈과 맞춰 검증 — 다른 드라이버의 `__try` 갈래에도 쓸 수 있다 (하네스 제안 아님, 기록만).

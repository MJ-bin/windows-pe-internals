# 123 afd-addrlist — 정적 분석 결과

- 요청서: `_workspace/sast/123_afd-addrlist.md` (Q219, PE_emulator "AFD 주소 목록 묻기 `0x120b3`")
- 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/` 아래 `drivers/afd.sys`, `mswsock.dll`.
- 날짜: 2026-11-17. **afd·mswsock 은 sast/121 에서 이미 analyze 된 것을 그대로 썼다** (다시 analyze 안 함). `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 셋 다 있음을 확인했고, export 만 했다 (몇 초). ntoskrnl 은 건드리지 않았다 (sast/124 가 같은 시간에 돈다). 이미 있는 `ms/disassemble/ntoskrnl/IopXxxControlFile.asm` 만 읽었다.
- 진행: 묶음 가 (R1·R2) 를 먼저 쓰고 (`SendMessage` 도구가 이 에이전트에 없어 중간 알림은 못 보냈다), 이어서 나 (R3) · 다 (R4·R5) 를 덧붙였다. 절 표시는 맨 아래 "진행 표".
- 추가로 뽑은 것 (묶음 나·다): afd `AfdTliIoControl` `AfdCompleteTliIoControl` `AfdCompleteTliIoControlCore` (depth 1) · `AfdBind` `AfdTLBind` `AfdTLBindSecurity` `AfdTLBindComplete` `AfdTLBindComplete2` `AfdTLBindGetAddrComplete` `SOCKADDR_SIZE` `AfdAllocateEndpoint` (depth 0). mswsock `WSPGetSockOpt` 는 묶음 가 때 같이 뽑았다 (`WSPIoctl`·`WSPSetSockOpt`·`WSPBind` 는 sast/121 `.asm` 재사용). `AfdAllowedUserIOControlRequest`·`AfdTLIoControl`·`AfdTLPendRequest` 는 depth 1 로 같이 나왔다.

## PDB·분석 상태 확인

| 모듈 | PDB guid+age | `.gpr` | `_index.tsv` | `SetPdb:` | 이름 확인 |
|---|---|---|---|---|---|
| afd | `582DC2B31D185EBE4A3FDD4F872920491` (sast/121) | 있음 | 있음 (1280 함수) | 1줄 | `AfdAddressListQuery` `0x25840` 1237 B 등 요청서 크기와 같음 |
| mswsock | `A3E5C0BA2810B53B2646FE9284927D821` (sast/121) | 있음 | 있음 (781 함수) | 1줄 | `WSPIoctl` `0x191b0` 8547 B · `WSPGetSockOpt` `0x9710` 3446 B 같음 |

- 커널 GUID 확인은 ntoskrnl 만 해당이라 해당 없음.
- PDB 에는 구조체·IOCTL 상수 이름이 없다 (공개 심볼). 칸 이름은 "코드가 쓰는 모양" 으로 붙인 우리 이름이고, 아래에서 `[추측]` 으로 표시한다.

## 뽑은 것 (이번 묶음 가)

- afd (depth 1): `^(AfdAddressListQuery|AfdInitializeAddressList|AfdProcessAddressChangeList|AfdNsiAddressChangeEvent|AfdNsiAddressChangeEventAtPassive)$` → 27개. 거기에 depth 0 으로 `AfdExceptionFilter`, `AfdCommonAddAddressHandler`, `AfdCommonDelAddressHandler`, `AfdFindAddressInList`, `AfdAddressListChange`, `AfdAddAddressHandler`, `AfdDelAddressHandler`.
  - 읽은 것: `AfdAddressListQuery` · `AfdInitializeAddressList` · `AfdNsiAddressChangeEvent(AtPassive)` · `AfdNsiGetAddressProperties` · `AfdCommonAddAddressHandler` · `AfdAddAddressHandler` · `AfdQueryCompartmentId` · `AfdGetCompartment` · `AfdExceptionFilter` · `IN6ADDR_SETV4MAPPED`. `AfdProcessAddressChangeList`·`AfdAddressListChange`·`AfdCommonDelAddressHandler`·`AfdFindAddressInList` 은 뽑아만 두고 안 읽었다 (알림·중복 제거·삭제 쪽, R1 질문 밖).
- mswsock (depth 1): `^(WSPIoctl|WSPGetSockOpt)$` → 31개. 거기에 depth 0 으로 `SockLoadHelperDll`, `SockpCreateV6AddressMap`.
- 즉치 `0x120b3` (바이트 `b3 20 01 00`) 를 **mswsock 코드 구역 전체**에서 찾았다 (`scanimm` 임시 스크립트, 원본 바이트): 2곳.
  - `WSPIoctl +0xdf1` (명령 시작; 즉치 바이트는 `+0xdf5`) — **이 길이 WinHttp 로그의 그 호출.**
  - `SockpCreateV6AddressMap +0xb1` (구역 `SANONTCP` — Winsock Direct 쪽 코드, 아래 "다른 부르는 곳" 에서 따로 적음).
  - `0x120bf` 즉치는 3곳: `WSPSetSockOpt +0x1d6`, `WSPGetSockOpt +0x7a0`, `WSPIoctl +0x1fef` (묶음 나 에서 다룸).
- 파일: `ms/disassemble/afd/AfdAddressListQuery.asm` (+ 위 afd 함수들), `ms/disassemble/mswsock/WSPIoctl.asm`, `ms/disassemble/mswsock/WSPGetSockOpt.asm`. `.c` 는 `ms/decomp/<모듈>/`. `_calls.tsv` 는 두 모듈 폴더에 덧붙여 쌓임.
- 임시 도구 (결과 밖): 쓴 임시 스크립트 둘은 scratchpad 에만 있다 — SEH 범위표 읽기(`scope.py`), 즉치 바이트 찾기(`scanimm.py`). 하네스 스크립트는 고치지 않았다. "스크립트 문제" 없음.

## 버전·스크립트 대조

- "알려진 사실" 표에 afd·mswsock 줄은 없다. 대신 `.asm` 의 `+0x` 와 원본 바이트·로그를 맞췄다.
  - **요청한 쪽 로그와 맞음**: 입력 주소 = `RSP+0xe8`, IOSB = `RSP+0xf8` (= 입력 +0x10), 4 B 스택 = `RSP+0x110` (= 입력 +0x28). `WSPIoctl +0xd03/+0xcf3/+0xdb3` 와 `+0xe01` 의 `[RSP+0x20]=&[RSP+0xf8]`. 로그의 "IOSB = 입력 주소 + 0x10", "4 B 스택 (입력 주소 + 0x28)" 과 같다. → 이 `.asm` 이 로그가 찍힌 코드와 같은 빌드라는 증거.
  - `AfdAddressListQuery` 인자 꼴 (RCX 파일 객체, RDX 코드, R8B 요청 모드, R9 입력 포인터, `[RSP+0x100]` 입력 길이, `[+0x108]` 출력 포인터, `[+0x110]` 출력 길이, `[+0x118]` `&Information`) = 요청서가 준 `AfdDispatchImmediateIrp.asm +0x5e ~ +0x8f` 꼴과 같다.
  - `AfdFastIoDeviceControl +0xff` 가 읽는 표 `AfdIoctlTable` 의 칸 번호 `(0x120b3 >> 2) & 0x3ff = 0x2c` (44번 칸) 가 `AfdAddressListQuery` — sast/121 의 표와 같음.
  - SEH 범위표 (`.pdata` 풀이): `[+0xa7,+0x10e)`·`[+0x220,+0x273)`·`[+0x2f6,+0x332)` — 풀이 값이 `.asm` 의 "읽지 않은 틈 `+0x10e~+0x116`, `+0x332~+0x34b`" (예외 착지점) 와 정확히 같은 자리.
- **판정: 일치.**

## 먼저 읽는 한 줄 답 (묶음 가)

1. **크기를 정하는 칸**: 커널이 쓴 출력 **`+0` 의 DWORD (주소 개수)**. mswsock 이 그걸 읽어 `*lpcbBytesReturned = 8 + 개수 × (16 + 8올림(MaxSockaddrLength))` 로 셈 (`WSPIoctl +0xe7b ~ +0xedd`). IPv4 면 개수 1 개당 32 B. **afd 는 이 개수 칸을 주소가 0 개여도, 버퍼가 모자라도 항상 쓴다.** 안 쓰면 부른 쪽 힙 (`0x200` 힙 · 8 B 힙, 0 으로 안 채움) 의 쓰레기가 개수가 된다 — 로그의 64 MiB 넘는 할당과 맞는 모양 `[추측: 쓰레기 값은 동적으로 확인]`. 4 B 스택 칸은 mswsock 이 미리 0 으로 채워 둔다 (`WSPIoctl +0xcfb`).
2. **afd 출력 꼴**: `DWORD 개수` + 그 뒤에 **TA_ADDRESS** 들 (빈틈 없이 이어 붙임, 칸마다 `WORD 길이 · WORD 종류 · 길이 B 주소`). 포인터 칸은 **없다** (포인터는 mswsock 이 사용자 버퍼에 따로 만든다). IPv4 하나 = 18 B (`0e 00 | 02 00 | 포트 2 B(0) | IPv4 4 B | 0 8 B`), IPv6 하나 = 30 B (`1a 00 | 17 00 | …`).
3. **주소 0 개는 정상**: status 0 · **Information 4** · 출력 `00 00 00 00` 만. 출력 길이 `0x200`·4·8 어느 쪽이든 같다. 출력 길이 < 4 만 `0xC000000D`.
4. **모자라면**: status **`0x80000005`** (`STATUS_BUFFER_OVERFLOW`), 출력 `+0` = **전체 개수** (이미 못 채운 것도 센다), Information = **필요한 전체 바이트** (`4 + Σ(길이+4)`), **넘친 칸부터는 본문을 안 쓴다** (그 앞에 이미 들어간 칸은 복사돼 있음; 칸이 하나뿐이면 본문 없음).
5. **mswsock 쪽 읽기**: 반환값은 `0` 과 `0x80000005` 만 통과 (나머지는 `NtStatusToSocketError` → 오류). **`IoStatusBlock.Information` 은 읽지 않고, `IoStatusBlock.Status` 는 PENDING 일 때만 읽는다.** 그래서 에뮬레이터가 맞춰야 하는 것은 ① 반환 status ② 출력 `+0` 개수 ③ (있으면) 칸 본문 — Information 은 afd 쪽 정직한 값 (아래 표) 으로 써 두면 된다.
6. **되풀이 한도 없음 (mswsock)**: `WSPIoctl` 은 `0x120b3` 을 **한 번만** 부른다 (재시도 고리 없음). 로그의 3 연속 호출은 부른 쪽 (ws2_32·앱) 이 `WSAIoctl` 을 세 번 부른 것이다 `[추측: 부른 쪽 코드는 안 봄]`.
7. **목록 출처**: 컴파트먼트 (망 구획) 객체의 목록 `+0xa8`. 처음 물을 때 (`AfdInitializeAddressList`) NSI 에 "주소 바뀜 알림" 을 등록하고, 그 알림 콜백이 채운다. IPv4·IPv6 는 **NSI 로만** 들어온다 (TDI 쪽 `AfdAddAddressHandler` 는 종류 2·0x17 을 버린다). 채울 때 **선호 상태 (4) 인 것만**, **루프백 인터페이스 (종류 칸 값 `0x18` = `IF_TYPE_SOFTWARE_LOOPBACK` 와 같은 값 `[추측]`) 의 범위 < 3 인 주소 (127.0.0.1·::1) 는 뺀다**. → 루프백만 있는 PC 면 IPv4·IPv6 모두 0 개. 어느 주소가 실제로 목록에 남는지는 NSI·tcpip 몫 `[공백]`.

8. **(묶음 나) `0x120bf` = TL 에 넘기는 길**. 입력 `0x20` 의 `+0x00` 이 **`1` 설정 · `2` 읽기 · `3` 소켓 ioctl**, `+0x04` 레벨 (ioctl 은 0), `+0x08` 옵션/ioctl 코드, `+0x0c` 가 반드시 `1`, `+0x10` 값 포인터, `+0x18` 값 길이. **afd 는 출력에 한 바이트도 안 쓴다** — 출력 포인터를 잠가서 TL (tcpip) 에 건네고, status·Information·작은 버퍼 status 는 TL 몫 (TL 이 `0xC0000206` 를 내면 afd 가 `0xC0000023` 으로 바꿈). afd 가 스스로 대답하는 것은 입력 검사 (`0xC000000D`·`0xC0000010`·`0xC0000184`) 와 `SIO_IDEAL_SEND_BACKLOG` 류 (`AfdHandleISBIoctls`) 뿐.
9. **(묶음 나) 로그의 "입력 `0x20` · 출력 8 B 힙 · IOSB = 입력 + `0x30`" 호출은 `WSPIoctl +0x1fef`** — `WSPSetSockOpt` 는 IOSB 가 입력 `+0x20`, `WSPGetSockOpt` 는 `−0x10` 이라 안 맞는다. 즉 앱이 `WSAIoctl` 로 부른 것이고 **어떤 코드인지는 로그의 입력 `+0x08` DWORD 를 보면 된다** (정적으로는 못 가림).
10. **(묶음 다) `AfdBind` (TL 모드)**: 입력 = 출력 같은 버퍼, `+0x00` 공유 방식 · `+0x04 ~` sockaddr. 검사: 입력 길이 ≥ 6, 출력 길이 ≥ 입력 길이 − 4, 계열 < `0x23`, sockaddr 바이트 ≥ `SOCKADDR_SIZE(계열)` (아니면 `0xC0000141`), 이미 묶임 `0xC0000238`. **성공은 동기가 아니라 `STATUS_PENDING` (`0x103`) 이고** TL 이 bind → 로컬 주소 질의 → `AfdTLBindGetAddrComplete` 가 **출력 `+0` 부터 로컬 sockaddr, `Information` = 그 길이, `Status` = 0** 으로 IRP 를 끝낸다. **임시 포트는 TL (tcpip) 이 고른다.** 엔드포인트는 종류 `[ep+2]` 가 3 (묶임) 으로 바뀐다.
11. **(묶음 다) `SOCKADDR_SIZE`**: 표 `[2]`=16 (AF_INET), `[0x17]`=28 (AF_INET6), `[1]`=110 (AF_UNIX), `[0x22]`=36, 그 밖 `0`; 계열 ≥ `0x23` 은 호출 쪽 (`AfdBind`·`AfdConnect`) 이 먼저 `0xC0000141`.

---

## 묶음 가 — 주소 목록 묻기 `0x120b3`

### R1. afd `AfdAddressListQuery` (rva `0x25840`, 1237 B)

**이 함수를 부르는 줄** `[정적]`: 즉시 처리 표 44번 칸 (`AfdImmediateCallDispatch`). fast I/O 길에서 `AfdFastIoDeviceControl +0x1603 ~ +0x1608` (`CALL … ret=+0x1608`, 반환값을 `[RDI]` = `IoStatus.Status` 에 씀 `+0x1608`) 에 쓰고 `&Information` = `IoStatus+8` 로 넘긴다 (`+0x15d9 ~ +0x15dd`). 이 부분은 요청서가 준 줄과 같음 — 새로 본 것은 없다.

**인자** (`AfdAddressListQuery.asm +0x2d ~ +0x72`): RCX 파일 객체 (`+0x6a` `LEA RBX,[RCX+0x18]` = `FsContext` 칸, `+0x9f` 에서 엔드포인트로 읽음), R8B 요청 모드 (0 = 커널), R9 = 입력 포인터 (`R15`), 입력 길이 `EDI`, 출력 포인터 `RSI`, 출력 길이 `[RSP+0x110]`, `&Information` = `[RSP+0x78]`.

#### (a) 검사 표

| 차례 | 조건 | status | Information | 출력에 쓰는 것 | 줄 (`AfdAddressListQuery.asm`) |
|---|---|---|---|---|---|
| 0 | 들어오자마자 | — | **0 으로 먼저 씀** | — | `+0x62` `MOV [RAX],R8` (R8=0) |
| 1 | 입력 길이 < 2 | `0xC000000D` | 0 | 안 씀 (개수 칸도 안 씀) | `+0x86 CMP EDI,R14D` / `+0x89 JC` → `+0x4cd` → `+0x4ef MOV EAX,0xc000000d` / `+0x4f4 JMP +0x2c8` |
| 2 | 출력 길이 < 4 (**0 포함**) | `0xC000000D` | 0 | 안 씀 | `+0x8f ~ +0x99` → 같은 `+0x4cd` |
| 3 | 요청 모드 = 커널 | 아래 검사 건너뜀 | | | `+0xa7 TEST R12B,R12B` / `+0xaa JZ +0xeb` |
| 4 | (유저) 입력 포인터가 홀수 | 예외 → 착지 `+0x10e` → **`0xC00002C5`** (`STATUS_DATATYPE_MISALIGNMENT_ERROR`) | 0 | 안 씀 | `+0xac TEST R15B,1` / `+0xb2 CALL ExRaiseDatatypeMisalignment` (ret `+0xb9`); 변환 `AfdExceptionFilter +0x86 ~ +0xa4` (`0x80000002` → `0xc00002c5`) |
| 5 | (유저) 입력 첫 바이트를 못 읽음 (**NULL**·커널 주소·안 열린 쪽) | **`0xC0000005`** | 0 | 안 씀 | `+0xbf ~ +0xd4` (`MmUserProbeAddress` 와 견줘 그보다 크면 그 주소를 읽음 → 반드시 터짐) |
| 6 | (유저) 출력 포인터가 4 정렬이 아님 | **`0xC00002C5`** | 0 | 안 씀 | `+0xd6 ~ +0xdf CALL ProbeForWrite(out, 출력길이, 4)` (ret `+0xe6`) |
| 7 | (유저) 출력 포인터가 NULL·커널 주소·안 열린 쪽 (길이 ≥ 4) | **`0xC0000005`** | 0 | 안 씀 | 같은 `+0xdf` |
| 8 | 입력 앞 2 B 읽기 · 출력 `+0` = 0 · 쓰는 크기 = 4 | — | — | **출력 `+0` DWORD = 0** | `+0xeb MOVZX EDI,word [R15]`, `+0xfe MOV [RSP+0x48],4`, `+0x109 MOV [RSI],R15D` (R15D=0) |
| 9 | `AfdQueryCompartmentId` 실패 (음수) | 그 status | 0 | 개수 0 이 남음 | `+0x11f CALL` (ret `+0x124`) / `+0x12a JS +0x2c8` |
| 10 | 컴파트먼트를 못 만듦 (풀 부족) | **`0xC000009A`** (`STATUS_INSUFFICIENT_RESOURCES`) | 0 | 개수 0 | `+0x3b2 CALL AfdGetCompartment` (ret `+0x3b7`) / `+0x3c4 MOV [RSP+0x40],0xc000009a` |
| 11 | 처음 묻는 계열이라 `AfdInitializeAddressList` 가 실패 | 그 status | 0 | 개수 0 | `+0x45f CALL` (ret `+0x464`) / `+0x46a JNS` 아니면 `+0x470 JMP +0x3cc` |
| 12 | 칸 본문이 안 들어감 (`필요 > 출력길이`) | **`0x80000005`** | **전체 필요 크기** | **개수만 쓰고 본문은 안 씀** (개수·크기는 계속 셈) | `+0x22c ~ +0x23a` (`EAX=EDI+RCX; CMP EAX,[RSP+0x110]; JA +0x2f6`) → `+0x315 MOV [RSP+0x40],0x80000005` → `+0x31d ~ +0x32d` (개수 `INC`, 크기 `ADD`) |
| 13 | 모두 들어감 · 주소 0 개 포함 | **`0`** | **4 + Σ(칸 길이 + 4)** | 개수 + 칸 본문 | `+0x2bd ~ +0x2c4` (`*Information = [RSP+0x48]`, `EAX = [RSP+0x40]`) |
| 14 | 칸 복사 중 예외 (출력이 도중에 안 열림 등) | 예외 코드 (`0xC0000005`·`0xC00002C5`) | **0** | 개수까지 쓴 것만 남음 | SEH `[+0x220,+0x273)`·`[+0x2f6,+0x332)` → 착지 `+0x332` (`AND [RSP+0x48],0`) → `+0x29d` 로 합침 |

- 예외 처리 (4·5·6·7·14): `AfdExceptionFilter` 가 예외 코드를 호출한 함수의 status 칸에 써 준다 (`+0x7d ~ +0x83`: `R10D = ExceptionRecord->ExceptionCode; [R9] = R10D`). `0x80000002` 는 `0xC00002C5`, `0x80000001` (보호 쪽 접근) 은 `0xC0000005` 로 바꾼다 (`+0x86 ~ +0xa4`). 착지 `+0x10e` 는 `MOV EAX,[RSP+0x40]; JMP +0x2c8` (읽지 않은 틈을 `objdump` 로 읽음). **status 칸은 함수 맨 처음 0 으로 둔다 (`+0x65`) 이므로, 예외 코드가 안 써지는 경로는 없다.**
- **엔드포인트 종류·상태로 거절하는 줄은 없다.** 태그 확인·`AfdEndpoint` 상태 확인이 이 함수에 없다 (`+0x9f` 에서 `[FsContext]` 를 읽고 바로 씀). 소켓이 묶였는지·연결됐는지와 상관없이 같은 대답이다 `[정적: 이 함수 안에는 그런 검사 줄이 없다]`.
- 계열 값 검사: **없다.** 입력 앞 2 B (`WORD`) 를 계열로 읽어 (`+0xeb`) 2·`0x17`·`0x11` 에만 특별 처리 (아래) 하고, 다른 값은 "그 종류인 칸만 고르는 비교" 라 아무것도 안 걸려 개수 0 으로 성공한다.
- WPP 추적 줄 (`+0x79`, `+0x4cd` 등) 은 응답에 영향 없다.

#### (b) 출력 바이트 칸 표 (성공·넘침 공통)

오프셋은 **출력 버퍼 시작** 기준. 칸마다의 반복 단위 = TA_ADDRESS (TDI 의 `TA_ADDRESS` 모양 `[추측: 이름은 윈도 문서의 TDI 이름과 같아서 붙임, PDB 에는 이름 없음]`).

| 오프셋 | 크기 | 뜻 | 값 | 쓰는 줄 (`AfdAddressListQuery.asm`) |
|---|---|---|---|---|
| `+0x00` | 4 | **개수** (맞는 주소 칸 수) | 0 으로 시작 (`+0x109`), 칸 하나 만날 때마다 `+1` — **본문이 안 들어가도 센다** | `+0x109`, `+0x31d ~ +0x322` (`MOV RAX,[RSP+0x58]; INC dword [RAX]`) |
| `+0x04` | 2 | 첫 칸 `AddressLength` (= 아래 "주소 본문" 길이) | IPv4 `0x000e`, IPv6 `0x001a` | 칸을 `+0x24b` 에서 통째로 복사 (`EDI = 길이+4`) · 4 B 앞 머리 포함 |
| `+0x06` | 2 | 첫 칸 `AddressType` (= 주소 계열) | IPv4 `2`, IPv6 `0x17`, NetBIOS `0x11` | 같음 |
| `+0x08` | 길이 B | 주소 본문 | IPv4: `포트 2 B(0)` · `IPv4 4 B(네트워크 순서)` · `0 8 B`. IPv6: `포트 2 B(0)` · `flow 4 B(0)` · `주소 16 B` · `범위 4 B` | 같음 |
| (다음 칸) | — | 두 번째 칸은 **빈틈 없이** 바로 이어서 `+0x04 + 길이1 + 4`. 정렬 없음 | | `+0x240 ~ +0x24b`: 복사 위치 = `출력 + [RSP+0x48]` (칸마다 `+0x324 ADD [RSP+0x48],EDI`) |

- **포인터 칸 없음**: 출력에 가리키는 값이 하나도 없다 (개수·길이·종류·바이트뿐). 포인터는 mswsock 이 사용자 버퍼에 따로 만든다 (R2).
- **칸 하나의 크기** = `AddressLength + 4`. IPv4 `0x12` (18 B), IPv6 `0x1e` (30 B). 계열 `0x17` 질의에서 IPv4 칸을 IPv6 로 바꿔 줄 때도 30 B (`+0x201 MOV EDI,0x1e`).
- 칸 본문의 포트 칸은 **항상 0** — 칸 안 값을 만드는 곳이 `AfdNsiAddressChangeEventAtPassive` 인데 포트 자리를 0 으로 비워 둔다 (`+0x19b ~ +0x1a7` 추가 갈래, `+0x276 ~ +0x281` 처음 전부 읽기 갈래: `MOVDQU [RBP+3]=0`, `[RBP+0x13]=0`, `[RBP+0x1b]=0`). IPv4 주소는 `[RBP+5]` (`+0x1b1 ~ +0x1b4`, `+0x28b ~ +0x28e`), 머리 `0x2000e` (`AddressLength 0xe`, `AddressType 2`) 는 `+0x1b7`·`+0x291`.
- 출력 +4 이후는 **개수 칸이 `0` 이면 안 쓴다** (바이트가 그대로 남는다).
- **계열 `0x17` 질의 + 엔드포인트의 "이중 모드" 비트가 켜진 소켓**이면 IPv4 칸도 **IPv4 매핑 IPv6 칸** (`::ffff:a.b.c.d`) 으로 바꿔서 보낸다: `AddressLength 0x1a`, `종류 0x17`, 포트 2 B (원래 칸의 포트), flow 4 B (0), 주소 16 B (`0…0 ffff` + IPv4 4 B), 범위 4 B (0) — 30 B. 비트는 엔드포인트 `+8` DWORD 의 비트 `0x14` (`+0x27e MOV EAX,[RCX+8]` / `+0x286 BT EAX,0x14`). **켜는 줄**: TL 로 `IPV6_V6ONLY` (레벨 `0x29`·옵션 `0x1b`, 값은 이름이 아니라 값 기준 `[추측]`) 를 설정하는 `0x120bf` 가 성공해 돌아올 때 값이 0 이면 켜고 0 이 아니면 끈다 (`AfdCompleteTliIoControlCore +0x15f ~ +0x1ea`: `+0x1d2 BTS EAX,0x14`, `+0x226 BTR EAX,0x14`). 새 엔드포인트는 0 이라 **IPv6 소켓이 `IPV6_V6ONLY=0` 을 설정하지 않으면 꺼져 있다** → IPv4 칸이 IPv6 질의에 안 섞인다. 변환 `+0x1f1 ~ +0x21a` (`IN6ADDR_SETV4MAPPED`, `AfdAddressListQuery` 이 칸 안에서 `[RSI]=0x1a` 를 써 둠).
- 계열 `0x11` (NetBIOS) 는 엔드포인트가 이름을 들고 있으면 같은 이름인 칸만 (`+0x1da ~ +0x1e2`, `+0x47f ~ +0x4ae` `RtlEqualUnicodeString`). TCP/UDP 소켓은 해당 없음 `[추측]`.

#### (c) 작은 버퍼

| 출력 길이 | 주소 0 개 | 주소 1 개 IPv4 (필요 22 B) |
|---|---|---|
| 0 | `0xC000000D`, Info 0 (검사 2) | 같음 |
| 1 ~ 3 | `0xC000000D`, Info 0 | 같음 |
| 4 | status 0, Info 4, 출력 `+0` = 0 | **`0x80000005`, Info 22, 출력 `+0` = 1** (본문 없음) |
| 8 | status 0, Info 4, `+0` = 0 (`+4~+7` 안 건드림) | **`0x80000005`, Info 22, `+0` = 1** (`+4~+7` 안 건드림) |
| 22 이상 | status 0, Info 4 | status 0, Info 22, 본문 18 B |

- **Information = 필요한 크기**: 맞다. 넘쳐도 `+0x2bd ~ +0x2c1` 이 `[RSP+0x48]` (칸마다 `ADD EDI` 한 누적 크기) 를 그대로 `*Information` 에 쓴다.
- **그때도 개수 4 B 를 쓰나**: **쓴다.** 개수는 칸 복사와 상관없이 `+0x31d` 에서 올라간다. 단 **출력 길이 < 4 면 안 쓴다** (검사 2 에서 `0xC000000D`).
- **출력 포인터 NULL**: 출력 길이 ≥ 4 이고 요청 모드가 유저면 `ProbeForWrite` 가 `0xC0000005` (검사 7). 출력 길이 0 이면 포인터 모양과 상관없이 먼저 `0xC000000D` (검사 2).

#### (d) 목록의 출처

| 칸 | 값 | 줄 |
|---|---|---|
| 어느 목록 | **컴파트먼트 객체 `+0xa8` 의 `LIST_ENTRY` (항목 = 아래 표)**. 컴파트먼트는 엔드포인트 `+0x58` 에 달려 있고, 없으면 처음 물을 때 만든다 (`AfdGetCompartment`). | `+0x143 MOV RBX,[R13+0x58]`, `+0x19b ~ +0x1ac`; `AfdGetCompartment +0x8b ~ +0xf4` |
| 컴파트먼트 번호 | `AfdQueryCompartmentId`: 엔드포인트에 이미 컴파트먼트가 있으면 그 `+0x10`. 없으면 `[엔드포인트+8]` 비트 8·9 가 켜졌을 때 **TL (tcpip) 에 "컴파트먼트 번호 달라"** (`SO_COMPARTMENT_ID`, 레벨 `0xffff`·옵션 `0x3004`·4 B) 를 묻고, 꺼졌거나 TL 이 `0xC00000BB` (지원 안 함) 이거나 0 이면 **번호 1 (기본)** 로 둔다. | `AfdQueryCompartmentId +0x19 ~ +0x2f`, `+0x34 ~ +0x44`, `+0x5f ~ +0x77`, `+0x198 (AfdTLIoControl 호출 ret)`, `+0x1be ~ +0x1d9`, `AfdAddressListQuery +0x11f` |
| 처음 한 번 채우기 | 컴파트먼트 `+0x10 == 1` (기본) 이고 `+0x38 == 0` 이거나, 계열 2 이고 `+0x20 == 0`, 계열 `0x17` 이고 `+0x30 == 0` 이면 `AfdInitializeAddressList(컴파트먼트, 계열)`. 한 번 하면 `+0x20/+0x30/+0x38` 칸이 채워져 다시 안 한다. | `AfdAddressListQuery +0x155 ~ +0x16d`, `+0x40d ~ +0x464` |
| 무엇을 하나 | ① (기본 컴파트먼트) **TDI PnP 처리기 등록** `TdiRegisterPnPHandlers` (주소 추가·삭제 콜백 `AfdAddAddressHandler`·`AfdDelAddressHandler`) ② IPv4 `NsiRegisterChangeNotificationEx` (모듈 `NPI_MS_IPV4_MODULEID`, 대상 번호 `0xa`, 콜백 `AfdNsiAddressChangeEvent`) ③ IPv6 도 같게 (`NPI_MS_IPV6_MODULEID`). **실패는 "물은 계열" 것만 오류로 올린다** (IPv4 등록이 실패해도 물은 게 IPv6 면 무시, 반대도). | `AfdInitializeAddressList +0x170` (`TdiRegisterPnPHandlers`, ret `+0x177`), `+0x1f9` (IPv4, ret `+0x200`), `+0x20b ~ +0x21f`, `+0x277` (IPv6, ret `+0x27e`), `+0x289 ~ +0x2b3` |
| 누가 채우나 | NSI 콜백 `AfdNsiAddressChangeEvent` (`0x29670`): 알림 종류 `R9D` (값의 이름은 PDB 에 없음 `[추측]`) — **`3` = 처음 전부 읽기** (`NsiAllocateAndGetTable`, 대상 번호 `0xa`, 한 칸당 `DadState` 같은 칸이 `4` 인 것만) · `2` = 지움 (`AfdCommonDelAddressHandler`) · 그 밖 = 추가·바뀜 (`AfdNsiGetAddressProperties` 로 확인 후 `AfdCommonAddAddressHandler`). IRQL 이 PASSIVE 가 아니면 작업 항목 (`AfdQueueWorkItem`) 으로 미룬다 (`+0x76 ~ +0xe1`). | `AfdNsiAddressChangeEvent +0x22 ~ +0x55`, `+0x76 ~ +0xe1`; `AfdNsiAddressChangeEventAtPassive +0x5e`, `+0x10b ~ +0x185`, `+0x1f8 ~ +0x2c8`, `+0x1b1 ~ +0x1d6` |
| 계열로 거르는 줄 | 목록 칸의 종류 (`[칸+0x2a]`) 가 물은 계열과 같은 칸만. 계열 `0x17` 질의면 IPv4 칸도 통과시키되 위의 "이중 모드" 비트가 켜져 있을 때만 (IPv6 로 변환). | `AfdAddressListQuery +0x1ce ~ +0x1d8`, `+0x1c1 ~ +0x1c5`, `+0x273 ~ +0x293` |
| **루프백을 빼는 줄** | **채울 때** (`AfdNsiGetAddressProperties`): 주소의 인터페이스 정보를 다시 읽어 `+4` 칸이 **`0x18`** 이고 주소 범위 (`Ipv4UnicastAddressScope`/`Ipv6UnicastAddressScope`) 가 **3 보다 작으면 "빼라" (BL=1)**. `0x18` 은 `IF_TYPE_SOFTWARE_LOOPBACK` (24) 와 같은 값 `[추측: 칸 이름은 PDB 에 없고 값 일치로 붙임]`. 빼라고 하면 `AfdNsiAddressChangeEventAtPassive` 가 `AfdCommonAddAddressHandler` 를 안 부른다. 또 **`DadState` (주소 중복 검사 상태) 가 4 인 것만** 넣는다 (`+0x1be CMP [RBP-0x31],4`). | `AfdNsiGetAddressProperties +0x150 ~ +0x18a` (`+0x15d` IPv4, `+0x172` IPv6, `+0x17b CMP EAX,3`, `+0x180 MOV BL,1`), `AfdNsiAddressChangeEventAtPassive +0x1be`, `+0x218`, `+0x26d ~ +0x270` |
| TDI 쪽 | `AfdAddAddressHandler`: 종류가 **2·0x17 이면 그냥 돌아간다**. 그 밖 종류 (NetBIOS 등) 만 기본 컴파트먼트에 넣는다. | `AfdAddAddressHandler +0xa ~ +0x1e` (`CMP AX,2 / JNZ`, `CMP AX,0x17 / JZ +0x14 (RET)`), `+0x32` |
| 칸 들어갈 때 | `AfdCommonAddAddressHandler`: 같은 칸이 이미 있으면 안 넣는다 (`AfdFindAddressInList`). 새 칸은 풀 (태그 `'Aftd'`=`0x74646641`) 에서 `((AddressLength + 0x2d) & ~1) + 이름 MaximumLength` 바이트 잡고 (`+0x83 ~ +0x9c`) 칸 `+0x28` 에 TA_ADDRESS (`길이+4` B) 를 복사. 종류 2·0x17 이면 컴파트먼트 `+0xb8` (IP 주소 수) 를 올리고, **0 → 1 이 되면 `AfdNotifyIPAvailabilityConsumers`**. | `AfdCommonAddAddressHandler +0x69 ~ +0x7d`, `+0x83 ~ +0xf6`, `+0xfb ~ +0x120`, `+0x1ea ~ +0x1f8`, `+0x26c ~ +0x272` |

항목 하나 (`AfdCommonAddAddressHandler` 가 만든 모양, `[정적]` `+0xb6 ~ +0xfe`): `+0x00` Flink · `+0x08` Blink · `+0x10` 8 B 열쇠 (NSI 열쇠의 앞 8 B) · `+0x18` 이름 `UNICODE_STRING` (NSI 항목은 빈 이름) · `+0x28` TA_ADDRESS (`AddressLength` `+0x28`, `AddressType` `+0x2a`, 포트 `+0x2c`, IPv4 주소 `+0x2e`).

**주소 0 개가 정상 갈래인가**: **그렇다.** 루프 (`+0x1a2 ~ +0x1ac`) 가 목록이 비면 바로 `+0x298` 로 가서 상태 0·Information 4 (`+0x2bd ~ +0x2c4`) 를 돌려준다. 오류 줄이 없다. → "주소 0 개 + 출력 `00 00 00 00`" 이 맞는 정직한 대답.

**망 없는 PC 에 무엇이 남나**: NSI (netio) 에 어떤 주소가 있나는 이 코드 밖이다 `[공백]`. 정적으로 말할 수 있는 건 "루프백 인터페이스의 `127.0.0.1`·`::1` 은 목록에 안 들어간다" 까지. 연결이 끊긴 어댑터의 주소 (선호 상태 4 가 아니면 안 들어감), 링크 로컬 `169.254.x.x`·`fe80::` 가 선호 상태로 있는 어댑터 (인터페이스 종류가 `0x18` 이 아니라서 범위 < 3 이어도 **빼지 않는다**) 는 NSI 몫 `[추측]`.

> **R1 지금 우리 (PE_emulator): 틀림 (빠져 있음).** `0x120b3` 은 afd 갈래의 "기본 대답" (`fake()` / `device_control_code_unverified`) 로 간다 → 출력 `+0` 개수·Information 을 안 쓴다. 아래 "최소 응답" 으로 채우면 된다.

### R2. mswsock — `0x120b3` 을 내는 함수

**`WSPIoctl` 의 `SIO_ADDRESS_LIST_QUERY` 갈래가 맞다** (제어 코드 `0x48000016`, `WSPIoctl +0xb20 CMP EDX,0x48000016` / `+0xb26 JZ +0xce8`). 다른 부르는 곳은 아래 "다른 부르는 곳".

#### (a) 내는 줄 · 인자

`WSPIoctl` 인자 (`+0x25 ~ +0x7d`): `R13D = cbInBuffer` (`R9D`), `R14 = lpvInBuffer`, `EBX = 제어 코드`, `[RSP+0x78]` = 소켓 (`RCX`), `R15 = lpvOutBuffer`, `[RSP+0x2a8]` = cbOutBuffer, `[RSP+0xc8]` = `lpcbBytesReturned`, `[RSP+0x98]` = lpOverlapped, `RSI` 는 처음엔 `lpErrno` 였다가 소켓 구조체로 바뀐다.

| 줄 (`WSPIoctl.asm`) | 하는 일 |
|---|---|
| `+0x13e ~ +0x149` | `lpcbBytesReturned == NULL` 이면 `+0x2730` (`*lpErrno = 0x271e` WSAEFAULT, 반환 -1) |
| `+0xb20 ~ +0xb26` | 코드가 `0x48000016` 이면 `+0xce8` |
| `+0xcf3 ~ +0xd0f` | **IOSB (`RSP+0xf8`, 16 B) 를 0 으로**, **4 B 스택 칸 (`RSP+0x110`) 을 0 으로**, 입력 칸 (`RSP+0xe8`) 0, `R14B = 0` |
| `+0xd17 ~ +0xd1f` | `*lpcbBytesReturned = 0` |
| `+0xd22 ~ +0xd3c` | `cbInBuffer != 0` 이면 WSAEINVAL (`0x2726`) |
| `+0xd41 ~ +0xd53` | `lpvOutBuffer != NULL` 이고 `cbOutBuffer != 0` 이면 → `+0xd5a ~ +0xd72` **`cbOutBuffer` 바이트 힙** (`SockPrivateHeap`, `SockAllocateHeapRoutine` 간접 호출, 플래그 0 = **0 으로 안 채움**) → `R13` / 실패면 `+0xd82` WSAENOBUFS (`0x2747`) |
| `+0xda0 ~ +0xdc0` | 아니면 (`lpvOutBuffer` 가 NULL 이거나 길이 0): `R14B = 1`, **`R13 = RSP+0x110` (4 B 스택)**, `EBX = 4` |
| `+0xdc7 ~ +0xdcb` | **입력 2 B = `word [소켓구조체+0x1c]` (소켓을 만들 때의 주소 계열)** 를 `RSP+0xe8` 에 |
| `+0xdd3 ~ +0xe1d` | `NtDeviceIoControlFile(소켓 핸들 [RSP+0x78], 이벤트 = 스레드 자료 [RBX+0x10], APC 0, 문맥 0, IOSB = RSP+0xf8, 코드 0x120b3, 입력 = RSP+0xe8, 입력 길이 2, 출력 = R13, 출력 길이 = EBX)`. 호출 `+0xe1d` (ret `+0xe24`) |

→ 로그의 "입력 길이 2 · IOSB = 입력 +0x10 · Event = mswsock 스레드 이벤트 · APC 0" 과 모두 일치. **출력이 "N B 힙 / 4 B 스택 / 8 B 힙" 인 이유**: 힙은 부른 쪽의 `cbOutBuffer` 가 그대로 (N = `0x200` 이면 `0x200` B 힙; 8 이면 8 B 힙), 4 B 스택은 "출력 버퍼 없음" 일 때 (크기 묻기). 부른 쪽이 `WSAIoctl` 을 (큰 버퍼 → 버퍼 없이 → 8 B) 세 번 부른 모양 `[추측: 부른 쪽은 ws2_32·앱 — 이번에 안 봄]`.

> 4 B 스택 칸이 **0 으로 미리 채워져 있다** (`+0xcfb`) — 그래서 "출력 없음" 호출은 커널이 개수를 안 써도 개수 0 으로 읽힌다. 반대로 **힙 칸은 안 채워져 있다.** 커널이 `+0` 을 안 쓰면 그 힙에 있던 값이 개수가 된다.

#### (b) 부르는 쪽 표 — 돌아온 뒤 읽는 것

| 호출 | 보는 것 | 안 보는 것 | 줄 (`WSPIoctl.asm`) |
|---|---|---|---|
| `NtDeviceIoControlFile` 반환 `EAX` | **`0x103` (PENDING)** 이면 `SockWaitForSingleObject(이벤트, 소켓, 3, 4)` (ret `+0xe51`) 로 기다린 뒤 **`ECX = IOSB.Status` (`[RSP+0xf8]`)**. 아니면 `ECX = EAX`. | PENDING 이 아니면 **IOSB 를 안 읽는다** | `+0xe29 ~ +0xe58` |
| `ECX` | **`0` 또는 `0x80000005` 만** 계속 (`+0xe5f ~ +0xe69`). 그 밖 → `NtStatusToSocketError(ECX)` (ret `+0xe70`) → `*lpErrno`·반환 -1, 이때 **`*lpcbBytesReturned` 는 안 바뀜** (0) | `0xC0000023` 같은 것은 따로 안 견줌 (오류로 감) | `+0xe5f ~ +0xe76` |
| 출력 `+0` (`[R13]`) | **개수** — `R11D` | 칸 본문은 크기 셈 때는 안 봄 | `+0xe7b` |
| `IoStatusBlock.Information` (`[RSP+0x100]`) | **읽지 않는다** | | 함수 전체에 `[RSP+0x100]` 읽기 없음 (`grep` 확인) |
| 소켓 구조체 `[+0x98]` (도우미 정보) 의 `+0x24` | **`MaxSockaddrLength`** (레지스트리 값 `MaxSockaddrLength` 를 `SockLoadHelperDll` 이 읽어 넣음 — `SockLoadHelperDll +0x2e8` (ret `+0x2ef`) 의 `RDX=.rdata 0x563b0` = `"MaxSockaddrLength"`), 8 로 올림 | | `+0xe8a ~ +0xe9d`; 이름 풀이 `SockLoadHelperDll +0x2c5 ~ +0x2e3` |

#### (c) 다음 버퍼 크기 셈 (mswsock 안, `+0xe7b ~ +0xedd`)

```
EBX = 개수 × 16 + 8              (+0xe82, +0xe86)
A   = (MaxSockaddrLength + 7) & ~7  (+0xe91 ~ +0xe99)   // IPv4 보통 16, IPv6 보통 32 [추측: 레지스트리 값, 아래]
부르는 쪽에 돌려줄 크기 = A × 개수 + 8 + 개수 × 16        (+0xecf, +0xed2)
→ *lpcbBytesReturned = 위 크기                           (+0xedd)
```

- 즉 **`머리 8 + 칸 × (16 + A)`** — 요청한 쪽 짐작 `머리 8 + 칸 × 개수` 와 같은 꼴. **칸 = `SOCKET_ADDRESS` (16 B: 포인터 8 + 길이 4 + 빈칸 4) + 계열별 sockaddr 자리 `A`.**
- **어느 칸이 정하나**: ① 개수 = 커널이 쓴 출력 `+0` ② `A` = 소켓의 도우미 정보 `MaxSockaddrLength` (레지스트리 값; 보통 TCP/IP IPv4 16, IPv6 28 이라 A 는 16·32 `[추측: 레지스트리 값은 이 PC 마다 — 정적으로 못 봄]`).
- 되풀이 한도: **mswsock 에는 재시도 고리가 없다.** 한 번 부르고 끝. 되풀이는 부른 쪽 몫 `[공백]`.
- 크기 계산 보호 (곱셈이 `0x7fffffff` 를 넘는지): `0x7fffffff / A <= 개수` 이면 (`+0xeb3 CMP EAX,R11D / +0xeb6 JBE`), 또는 `0x7fffffff − A×개수 <= EBX` 이면 (`+0xec6 CMP R9D,EBX / +0xec9 JBE`) **크기를 안 쓰고 오류 `+0xfb2` (WSAEFAULT `0x271e`)**. 그러나 개수가 이 보호선 (A=16 이면 약 `0x7ffffff`) 보다 **작은 쓰레기**면 보호를 지나서 `+0xedd` 가 **큰 크기를 `*lpcbBytesReturned` 에 쓴 뒤** 아래 검사 (`+0xedf ~ +0xef7`) 에서 WSAEFAULT 로 간다 → **부른 쪽이 WSAEFAULT + 큰 필요 크기를 받아 그 크기로 새로 할당**한다. 로그의 64 MiB 넘는 할당 (개수 약 200 만 = `0x200000` 이면 `8 + 32 × 개수` ≈ 64 MiB) 이 이 모양으로 설명된다 `[추측: 쓰레기 개수 값은 동적으로 확인]`.
- **크기를 쓴 뒤 오류로 가는 갈래** (`*lpcbBytesReturned` 는 이미 필요 크기): `ECX != 0` (= `0x80000005`) `+0xedf ~ +0xee1`; 버퍼 없음 (`R14B != 0`) `+0xee7 ~ +0xeea`; 필요 크기 > `cbOutBuffer` `+0xef0 ~ +0xef7` → 모두 `+0xfb2 MOV EDI,0x271e` (**WSAEFAULT**, 반환 -1). → 이게 "크기 묻기" 의 정직한 대답 (`*lpcbBytesReturned` 에 필요 크기 + WSAEFAULT).

#### (d) 출력 버퍼에 **쓰는** 줄 (포인터를 만드는 고리)

임시 힙 (`R13`, 커널이 쓴 `TRANSPORT_ADDRESS` 꼴) 은 **읽기만** 한다. 사용자 버퍼 `R15` 에 새로 짓는다 (`SOCKET_ADDRESS_LIST` 꼴):

| 줄 | 쓰는 것 |
|---|---|
| `+0xf27 ~ +0xf2b` | `[R15+0] = [R13+0]` (개수 `iAddressCount`) |
| `+0xf11 ~ +0xf1f` | 첫 sockaddr 자리 `R9 = R15 + (개수×16 + 8)` |
| `+0xf46 ~ +0xf5a` (고리) | `[R15 + 8 + 16·i] = R9` (**`lpSockaddr` = 사용자 버퍼 안의 절대 주소**), `[R15 + 0x10 + 16·i] = AddressLength + 2` (`iSockaddrLength`) |
| `+0xf62 ~ +0xf69` | `memcpy(R9, 임시 칸 + 2, AddressLength + 2)` — 칸의 **`AddressType` 부터** 복사 (= `sa_family` 가 되고 뒤에 포트·주소가 이어짐) |
| `+0xf6e ~ +0xf7d` | `R9 += A` (다음 sockaddr 자리), 임시 칸 한 칸 = `AddressLength + 4` 앞으로 (`+0xf92 ~ +0xf9a`) |

→ **afd 가 쓰는 출력과 mswsock 이 사용자에게 주는 출력은 다른 버퍼**다. afd 출력에는 포인터가 없고, `lpSockaddr` 는 mswsock 이 `lpvOutBuffer` 안 주소로 만든다. (요청서가 걱정한 "포인터 고쳐 쓰기" 는 임시 힙을 고쳐 쓰는 게 아니다.)

#### (e) `*lpcbBytesReturned` · WSA 오류로 가는 값

| 상황 | `*lpcbBytesReturned` | 반환 · `*lpErrno` | 줄 |
|---|---|---|---|
| 성공 (`0` + 칸이 다 들어감) | 필요 크기 (주소 0 개면 **8**) | 0 · 안 건드림 | 크기 쓰기 `+0xedd`, 끝 `+0x2716 ~ +0x271c JZ +0x286` (`XOR EAX,EAX`) |
| 필요 > `cbOutBuffer` · 버퍼 없음 · `0x80000005` | 필요 크기 | -1 · **WSAEFAULT (`0x271e`)** | `+0xedf ~ +0xef7` → `+0xfb2` |
| `0x80000005`/0 외 status | **0** (`+0xd1f` 에서 0 으로 둔 채) | -1 · `NtStatusToSocketError(status)` | `+0xe5f ~ +0xe76` |
| `cbInBuffer != 0` | 0 | -1 · WSAEINVAL (`0x2726`) | `+0xd22 ~ +0xd3c` |
| 힙 못 잡음 | 0 | -1 · WSAENOBUFS (`0x2747`) | `+0xd80 ~ +0xd9b` |

반환은 마지막 공통 꼬리 `+0x254d ~ +0x2760`: `*lpErrno = EDI` 가 0 이 아니면 -1 (`+0x2722 ~ +0x272e`), 0 이면 0 (`+0x271c JZ +0x286`). 겹침 (`lpOverlapped`) 이 NULL 이면 `+0x259d ~ +0x25a8` 에서 바로 `+0x26eb` 로 가 소켓 락 해제만 한다. **겹침을 주면** (이 갈래도 `[RSP+0xec]` 는 공통 경로 `+0x59d ~ +0x5a2` 에서 1 이 돼 있음) 꼬리가 `+0x25cd ~ +0x264d` 에서 사용자 `OVERLAPPED` 의 Internal 에 `0x103` 을 쓰고 **다른 제어 코드 `0x1209f` 를 한 번 더 낸다** (성공 `EDI=0` + 겹침 소켓 `[RSI+0x48]&1` 일 때). 로그의 동기 호출 (`lpOverlapped` = NULL) 에는 안 나온다.

#### 다른 부르는 곳 (참고)

- **`SockpCreateV6AddressMap` (`0x3df6c`, 구역 `SANONTCP`)** `+0xb1` / `+0xbe` (ret `+0xc5`): 입력 2 B = `0x17` (IPv6 고정), 출력 = 힙 (`0xf4 + 0x1e × SockSanV6AddressCount` B), IOSB = `[RSP+0x50]`. **여기서는 `0x80000005` 를 받으면 `IoStatusBlock.Information` (`[RSP+0x58]`, `+0x58 MOV R8D`) 만큼 힙을 새로 잡아 다시 부르는 고리가 있다** (`+0xca CMP EAX,0x80000005 / +0xcf JZ +0x3b` (고리 머리 `+0x3b ~ +0x72` 에서 힙을 새로 잡음)). 읽는 값: 개수 `[RBX]`, 칸 (`+0x4` 부터, `길이+4` 씩). → **afd 쪽에서 "넘침 + Information = 필요 크기" 의 의미가 틀리면 이 길에서 무한 반복할 수 있다.** WinHttp 로그의 길은 아니다 (`WSPIoctl`) — Winsock Direct 초기화 쪽 `[추측: 부르는 곳은 안 봄]`.
- 즉치 `0x120b3` 의 afd 쪽 받는 곳은 `AfdAddressListQuery` 하나 (표 44번 칸) 뿐.

> **R2 지금 우리: 틀림 (afd 쪽이 비어서 생기는 문제).** mswsock 코드는 맞는 입력을 낸다. **고칠 곳은 afd 대답** — 출력 `+0` 에 개수를 항상 쓰고 (모자라도·0 개여도) status 를 `0`/`0x80000005` 로 내면 mswsock 이 알아서 `8 + …` 크기를 만든다.

---

## 최소 응답 (묶음 가) — 에뮬레이터가 내줄 대답

원칙: 진짜 망에 안 나감. 망 없는 PC = "선호 상태의 비루프백 주소 없음" `[추측]` (이 상태가 진짜 VM 에서 나는지는 아래 동적 항목).

### 주소 0 개 (루프백만 있는 PC)

입력 2 B = 계열 (2 또는 `0x17`), 입력 길이 2, 요청 모드 유저.

| 출력 길이 | 반환 status (= `NtDeviceIoControlFile` 반환) | `IoStatusBlock.Status` | `IoStatusBlock.Information` | 출력 바이트 |
|---|---|---|---|---|
| `0x200` | `0` | `0` | `4` | `+0..+3` = `00 00 00 00`, 나머지 **안 건드림** |
| `4` | `0` | `0` | `4` | `+0..+3` = `00 00 00 00` |
| `8` | `0` | `0` | `4` | `+0..+3` = `00 00 00 00`, `+4..+7` **안 건드림** |

- mswsock 이 이걸 받으면: `0x200` 힙 → 성공 (사용자 `iAddressCount`=0, `*lpcb` = **8**, 반환 0); 4 B (버퍼 없음) → `*lpcb` = **8** + WSAEFAULT (정직한 "크기 묻기"); 8 B 힙 → 성공 (`*lpcb` = 8).
- 출력 길이 0..3 → `0xC000000D`, Information 0, 출력 안 씀.
- IOSB (16 B) 는 fast I/O 가 정한다: `IoStatus.Status = 반환값`, `Information = 위 값` 을 `IopXxxControlFile +0xb38 ~ +0xb48` 이 사용자 IOSB 로 복사 (`+0xb4d ~ +0xb66` 는 32 비트 프로세스용 8 B), `KeSetEvent` `+0xbe4`, 반환 `EAX = IoStatus.Status` (`+0xc30`). 요청서가 준 줄과 같음.

### 주소 1 개 (IPv4, 예 192.168.0.5), 계열 2

| 출력 길이 | 반환 status | `IoStatusBlock.Status` | `IoStatusBlock.Information` | 출력 바이트 |
|---|---|---|---|---|
| `0x200` | `0` | `0` | `0x16` (22) | 22 B: `01 00 00 00` · `0e 00` · `02 00` · `00 00` · `c0 a8 00 05` · `00 00 00 00 00 00 00 00` |
| `4` | **`0x80000005`** | `0x80000005` | `0x16` | `+0..+3` = `01 00 00 00`, 나머지 없음 |
| `8` | **`0x80000005`** | `0x80000005` | `0x16` | `+0..+3` = `01 00 00 00`, `+4..+7` **안 건드림** |

- 22 B = 개수 4 + 칸 `AddressLength(2)+AddressType(2)+14`. 4 B 일 때 칸 본문을 쓰면 안 된다 (`+0x22c ~ +0x23a`).
- mswsock 이 이걸 받으면: `0x200` 힙 → `*lpcb` = 8 + 1×(16+16) = **40 (`0x28`)**, 사용자 목록 `{1, [lpSockaddr = lpvOutBuffer+0x18, 길이 16]}`, 반환 0. 4 B → `*lpcb` = 40 + WSAEFAULT. 8 B 힙 → `*lpcb` = 40 + WSAEFAULT.

### 주소 1 개 IPv6 (참고), 계열 `0x17`

칸 = 30 B (`1a 00 | 17 00 | 포트 2 | flow 4 | 주소 16 | 범위 4`), 출력 34 B, Information `0x22`. mswsock 크기 = `8 + 1×(16+32)` = 56 (`A` = 8 올림(28) = 32 가정 `[추측]`).

---

## 지금 우리 (PE_emulator) 맞나·틀리나 — 묶음 가

| 질문 | 지금 | 이유 |
|---|---|---|
| R1 `0x120b3` 받기 | **틀림** | afd 갈래에 코드 없음 → 기본 대답 (`fake()`·`device_control_code_unverified`). `nt_device_io_control_file.cpp` afd 갈래 (`0x1207b`·`0x12047`·`0x12007` 만). |
| 출력 `+0` 개수 쓰기 | **틀림 (없음)** | 이게 `WSPIoctl` 의 크기를 정한다 (한 줄 답 1) |
| Information = 4 (0 개) / 필요 크기 | **틀림 (없음)** | mswsock 은 안 읽지만 SockpCreateV6AddressMap 이 읽음 |
| status `0x80000005` 넘침 | **틀림 (없음)** | 필요 |
| 주소 목록 출처 | 새로 만들 것 | "망 없는 PC = 0 개" 정책값 + `TODO(VM)` |
| 요청 모드 유저 때 입력·출력 검사 | 새로 만들 것 | 아래 줄 4 ~ 7 의 status 표 |

`tests/file.cpp` 에 얹을 케이스 (정적으로 정해진 값): 주소 0 개 × 출력 길이 `0x200`·4·8·3·0, 입력 길이 1, 출력 포인터 홀수·NULL.

## 동적으로 확인할 것 (묶음 가) — 멈춤점 `caller+0x…`

모두 test.exe 일 때만 멈추게 (프로세스 조건). sast/121 "동적" 4·6 과 같은 VM 세션에 얹을 수 있다.

1. **망 없는 VM 에서 진짜 목록이 비는가** (한 줄 답 7·R1(d)): `afd!AfdAddressListQuery+0x19b` (`MOV R12,[RBX+0xa8]` 직전, ret 아님 = 명령 자리) 에서 `dq @rbx+0xa8 L2` — `Flink == &Blink` 이면 빈 목록. 같은 자리에서 `dd @rbx+0xb8 L1` (IP 주소 수). 망 있는 VM 은 비교용.
2. **afd 가 최종으로 내는 status·Information**: `afd!AfdFastIoDeviceControl+0x1608` (`CALL` 의 ret, `ret=+0x1608` — WinDbg 콜스택의 `AfdFastIoDeviceControl+0x1608`) 에서 `eax` (status), `poi(rdi+8)` (Information). 출력 길이 `0x200` / 4 / 8 세 번 모두 본다.
3. **출력 본문**: `afd!AfdAddressListQuery+0x2a4` (`ExReleaseResourceAndLeaveCriticalRegion` 호출 자리, 이 시점 출력 포인터는 `[RSP+0x58]`) 에서 `db poi(@rsp+0x58) L40`. `AfdAddressListQuery+0x2c8` 에서 `eax` 가 status.
4. **컴파트먼트 번호**: `afd!AfdAddressListQuery+0x124` (`AfdQueryCompartmentId` ret) 에서 `eax` (status), `dd @rsp+0x4c L1` (번호). TL 에게 묻는 갈래인지: `afd!AfdQueryCompartmentId+0x198` (`AfdTLIoControl` ret) 가 걸리는지.
5. **처음 한 번 채우기가 동기인가** (콜백이 `AfdInitializeAddressList` 안에서 불리나, 아니면 나중): `afd!AfdInitializeAddressList+0x200` (`NsiRegisterChangeNotificationEx` ret) 직전·직후로 `afd!AfdNsiAddressChangeEvent` 입구 (`afd!AfdNsiAddressChangeEvent`) 가 먼저 불리는지, 그리고 `afd!AfdAddressListQuery+0x464` (ret) 직후 목록 `+0xa8` 이 이미 찼는지.
6. **mswsock 쪽 값**: `mswsock!WSPIoctl+0xe24` (`NtDeviceIoControlFile` ret) — `eax`, `dq @rsp+0xf8 L2` (IOSB), `db @r13 L20` (임시 버퍼); `mswsock!WSPIoctl+0xe9d` (A 계산 뒤) 의 `r8d` = 8 올림한 `MaxSockaddrLength` (TCP IPv4·IPv6 소켓 각각); `mswsock!WSPIoctl+0xedd` 직전 `edx` = 돌려줄 크기.
7. **"쓰레기 개수"를 실제로 본다** (한 줄 답 1 이 맞는지): 에뮬레이터에서 `0x200` 힙 `[r13]` 값 (`mswsock!WSPIoctl+0xe7b` 직전). 힙이 0 으로 오는지 쓰레기인지는 에뮬레이터의 `RtlAllocateHeap` 몫 `[공백]`.

## 판정 (묶음 가)

**분석성공.** R1 (afd 검사·출력 바이트·작은 버퍼·목록 출처) 과 R2 (mswsock 내는 줄·읽는 줄·크기 셈) 모두 `.asm` 줄로 닫았다. 정적으로 못 닫은 것은 모두 NSI·TL 몫 (목록에 실제로 남는 주소, 컴파트먼트 번호를 TL 이 어떻게 답하나, 레지스트리 `MaxSockaddrLength` 값) 이고 위 "동적" 에 있다.

---

## 묶음 나 — `0x120bf` 출력 꼴

### R3. afd `AfdTliIoControl` (rva `0x26430`, 3144 B) · `AfdCompleteTliIoControl` (`0x4880`, 128 B) · `AfdCompleteTliIoControlCore` (`0x4908`, 631 B) · mswsock 에서 `0x120bf` 를 내는 곳

**줄기 전체가 TL (tcpip) 에 넘기는 길이다.** afd 는 입력 검사·복사·잠그기만 하고 값은 TL 이 정한다.

**이 코드를 부르는 줄** `[정적]`: `0x120bf` → `AfdIoctlTable` 칸 `(0x120bf>>2)&0x3ff = 0x2f` (47번) → **IRP 길** (`AfdIrpCallDispatch`, 요청서·sast/121 표). 인자: RCX = IRP, RDX = 현재 스택 위치 (`+0x35 MOV R13,RDX`, `+0x38 MOV RSI,RCX`). 쓰는 칸: 스택 위치 `+0x30` 파일 객체 → `+0x18` 엔드포인트 (`+0x5b ~ +0x5f`), `+0x10` 입력 길이, `+0x08` 출력 길이, `+0x20` 입력 포인터, IRP `+0x70` 출력 포인터 (`+0x15e`), IRP `+0x40` 요청 모드.

#### (a) 입력 `0x20` 의 칸 (64 비트 프로세스; 32 비트는 `0x18` 짜리 `+0x323 ~ +0x41b`)

| 오프셋 | 크기 | 뜻 | 값 | 줄 |
|---|---|---|---|---|
| `+0x00` | 4 | **종류** | **`1` = 옵션 설정** (`WSPSetSockOpt +0x192 ~ +0x197`), **`2` = 옵션 읽기** (`WSPGetSockOpt +0x766` `[RSP+0x78]=2`; afd 가 스스로 컴파트먼트 번호 물을 때도 `AfdQueryCompartmentId +0x5f` `[RSP+0x30]=2`), **`3` = 소켓 ioctl** (`WSPIoctl +0x1f9e` `[RSP+0x190]=3`). **그 밖은 거절** (`0xC000000D`). 이름은 PDB 에 없다 `[추측: 쓰임으로 붙인 이름]` | `AfdAllowedUserIOControlRequest +0xb ~ +0x2d` |
| `+0x04` | 4 | 레벨 | 옵션: 호출한 레벨. ioctl: **0** (`WSPIoctl +0x1e52`). 종류 1·2 는 **`0xfffc`·`0xfffd` 거절**, 종류 3 은 **0 일 때만** 허용 | `+0x13 ~ +0x1c`, `+0x26 ~ +0x2d` |
| `+0x08` | 4 | 옵션 이름 / ioctl 코드 | `getsockopt`·`setsockopt` 의 `optname`, `WSAIoctl` 의 `dwIoControlCode` | `WSPIoctl +0x1fa9 MOV [RSP+0x198],EDX` |
| `+0x0c` | 1 | 표지 | **반드시 `1`** (다르면 거절) | `AfdAllowedUserIOControlRequest +0x6 ~ +0x9`; 내는 쪽 `WSPSetSockOpt +0x1a3`, `WSPGetSockOpt +0x77a`, `WSPIoctl +0x1fb0` |
| `+0x10` | 8 | 값 포인터 | 설정: `optval`. 읽기: 0. ioctl: `lpvInBuffer` | `WSPSetSockOpt +0x1a7`, `WSPIoctl +0x1fb8` |
| `+0x18` | 8 | 값 길이 | 설정: `optlen`. 읽기: 0. ioctl: `cbInBuffer` | `WSPSetSockOpt +0x1af ~ +0x1b2`, `WSPIoctl +0x1fc0` |

**afd 가 스스로 끝내는 것 / TL 로 넘기는 것**

| 갈래 | 어느 쪽 | 줄 (`AfdTliIoControl.asm`) |
|---|---|---|
| 종류 3 + 코드 `0x4004747b` · `0x2000747a` + 표지 ≠ 0 | **afd** (`AfdHandleISBIoctls`, 이름은 값이 `SIO_IDEAL_SEND_BACKLOG_QUERY/CHANGE` 와 같아서 `[추측]`). 이번엔 안 읽음 `[공백]` | `+0xc9 ~ +0x209`, `+0x862 ~ +0x868` |
| 종류 3 + QoS 코드 `0x8800000b`·`0x8800001a` | afd 가 QoS 구조를 먼저 처리한 뒤 TL 로 (`AfdTliIoControlHandleSetQoS`·`…AssociateQoS`) `[추측: 이름은 값 기준]`. 이번엔 안 읽음 | `+0x121 ~ +0x12a`, `+0x23a ~ +0x282`, `+0x550 ~ +0x5de`, `+0x64a ~ +0x692` |
| 종류 1 + 옵션 `0x3004` (컴파트먼트 번호) + 이미 컴파트먼트가 있음 | **afd 가 거절** `0xC0000184` | `+0x2e4 ~ +0x30c`, `+0x5de ~ +0x60f` |
| 종류 1 + 레벨 `0x29` + 옵션 `0x1b` (IPV6_V6ONLY 와 값이 같음) + TL 가 성공 | TL 로 넘긴 뒤 **완료에서 afd 가 엔드포인트 `+8` 비트 `0x14` 를 켜고/끈다** (값 0 이면 켬 = 이중 모드, 아니면 끔) — 아래 R1 의 "이중 모드" 비트 | `AfdCompleteTliIoControlCore +0x15f ~ +0x1ea` |
| 그 밖 모두 | **TL (tcpip)** | `+0x7eb` (엔드포인트) · `+0x8c8` (연결) · `+0xae3` · `+0xb75` (TDI) |

#### (a') 검사 표 (`AfdTliIoControl.asm`, 64 비트·유저 모드 기준)

| 차례 | 조건 | status | Information | 출력에 쓰는 것 | 줄 |
|---|---|---|---|---|---|
| 1 | 엔드포인트 `+8` 비트 8·9 둘 다 꺼짐 (TL 엔드포인트 아님) | **`0xC0000010`** | 0 | — | `+0x63 ~ +0x6b`, `+0x905 ~ +0x913` |
| 2 | 입력 길이 < `0x20` | `0xC000000D` | 0 | — | `+0x9c ~ +0x9f` → `+0x228` |
| 3 | (유저) 입력 포인터 4 정렬 아님 | `0xC00002C5` (예외 → 필터) | 0 | — | `+0x20e ~ +0x21b` |
| 4 | (유저) 입력 못 읽음 (NULL·커널 주소·안 열림) | `0xC0000005` | 0 | — | `+0x614 ~ +0x628` (`MmUserProbeAddress` 위치를 읽어 터뜨림) |
| 5 | 종류·레벨·표지가 허용 표에 없음 | **`0xC000000D`** | 0 | — | `+0x10b ~ +0x112`, `+0x63d ~ +0x645`; 종류 3 경로 `+0x429 ~ +0x432` |
| 6 | (종류 1) `0x3004` + 컴파트먼트 있음 | `0xC0000184` | 0 | — | `+0x2e4 ~ +0x30c`, `+0x5de ~ +0x60f` |
| 7 | 값 길이 ≠ 0 인데 값 범위가 유저 영역 밖·넘침 | `0xC0000005` | 0 | — | `+0x287 ~ +0x318` |
| 8 | 출력 길이 ≠ 0: IRP 출력 포인터를 MDL 로 잠금·시스템 주소로 매핑 | 못 잡으면 `0xC000009A`, 잠금 예외는 예외 코드 | 0 | 출력 +0 부터 TL 이 직접 씀 | `+0x147 ~ +0x1df`, `+0x697`, `+0x6a4` |
| 9 | 엔드포인트 `+0x170` > 0 (묶는 중·닫는 중 `[추측]`) | `0xC0000184` | 0 | — | `+0x6e7 ~ +0x708`, `+0x855` |
| 10 | 엔드포인트 종류 (`[ep+2]`) · 태그가 맞지 않음 | `0xC0000184` | 0 | — | `+0x70e ~ +0x72e`, `+0x898 ~ +0x8b4`, `+0xa15 ~ +0xa3c` |
| 11 | TL 호출 결과 `0xC0000206` | **`0xC0000023`** (`STATUS_BUFFER_TOO_SMALL`) 로 바꿈 | TL 이 준 값 | TL 이 쓴 만큼 | `+0xa02 ~ +0xa09`, `+0xb81 ~ +0xb8b`, `AfdTLIoControl +0xa6 ~ +0xb1`, `AfdCompleteTliIoControlCore +0xa6 ~ +0xb5` |
| 12 | TL 가 `0x103` (PENDING) | `0x103` 반환 (IRP 보류, `AfdTLPendRequest`) | — | — | `+0x7f7 ~ +0x801`, `+0xaf6 ~ +0xb01` |
| 13 | 그 밖 | TL status 그대로 | **TL 이 준 값** (`AfdCompleteTliIoControlCore +0xac` `MOV [RBX+0x38],RBP`; TL 이 안 쓰면 `0`) | TL 이 쓴 만큼 | `AfdCompleteTliIoControlCore +0x89 ~ +0xc4` |

- 예외 착지 `+0x6b3` (`XOR EBX,EBX; …; JMP +0x86f`) 는 상태를 필터가 `[RSP+0x30]` 에 써 준 예외 코드로 IRP 를 끝낸다 (SEH `[+0x71,+0x6b3)`, `.pdata` 풀이). Information 은 0.
- **엔드포인트가 TL 모드인 소켓에선 아래 TL 호출 4 줄 중 하나로 간다.** 줄: `+0x7eb` (일반, `AfdTLIoControl(TL 제공자, TL 엔드포인트 [ep+0x10], 요청 블록, 엔드포인트)`), `+0x8c8` (연결 있는 소켓), `+0xae3`, `+0xb75`.

#### (b) 출력 바이트 칸 표 — **afd 는 출력에 쓰지 않는다**

| 오프셋 | 크기 | 뜻 | 값 | 쓰는 줄 |
|---|---|---|---|---|
| 출력 `+0` ~ | TL 이 정함 (`≤ 출력 길이`) | 옵션 값 / ioctl 결과 | **TL (tcpip) 몫** | afd 가 출력 포인터 (MDL 시스템 매핑, `[RDI+0x48]` = `+0x1d2`) 와 길이 (`[R13+8]`, `+0x7ad ~ +0x7b1`) 를 TL 요청 블록에 적어 건네줄 뿐 (`+0x77d ~ +0x7b1`: `[RSP+0x80]` 출력 포인터, `[RSP+0x88]` 출력 길이) |
| `IoStatus.Status` | 4 | TL 가 준 status (`0xC0000206` → `0xC0000023`) | TL 몫 | `AfdCompleteTliIoControlCore +0xa6 ~ +0xb9` |
| `IoStatus.Information` | 8 | TL 이 준 바이트 수 | TL 몫 (`[요청 블록 +0x48]`, 처음 0) | `+0x6de` (0 으로 비움), `AfdCompleteTliIoControlCore +0xac` |
| 작은 버퍼 | — | TL 가 `0xC0000206` 를 내면 afd 가 **`0xC0000023`** 으로 | | 위 11 번 줄 |

→ 요청서 R3 (b) "출력에 무엇을 몇 바이트 · Information · 작은 버퍼 status" 는 **정적으로 afd 안에서 안 정해진다 (TL 몫)**. 값을 정하는 건 tcpip 의 TL 제공자 `[공백]` (이번에 `tcpip.sys` 분석 안 함).

#### (c) 부르는 쪽 표 — mswsock

| 호출 (`0x120bf` 를 내는 줄) | 보는 것 | 안 보는 것 | 줄 |
|---|---|---|---|
| **`WSPSetSockOpt +0x1d6`** (설정, 종류 1). 입력 `[RSP+0x70]`, IOSB `[RSP+0x90]` (= 입력 `+0x20`), 출력 NULL·0 | 반환 status 의 **부호** (`+0x250 TEST EAX,EAX / +0x252 JS`). PENDING (`0x103`) 이면 기다린 뒤 `IOSB.Status` (`+0x245`) | `IOSB.Information`·출력 | `WSPSetSockOpt +0x1fd ~ +0x252` (ret `+0x204`) |
| **`WSPGetSockOpt +0x7a0`** (읽기, 종류 2). 입력 `[RSP+0x78]`, IOSB `[RSP+0x68]` (= 입력 `−0x10`), **출력 = 호출자의 `optval` (`R12`) · 길이 = `*optlen` (`[R14]`)** | status: **`< 0` 이면 오류** (`+0x82d TEST EAX,EAX` / `+0x82f JNS`). `0xC00000BB` (`STATUS_NOT_SUPPORTED`) → **WSAENOPROTOOPT (`0x273a`)** (`+0x835 ~ +0x848`), 그 밖 음수 → `NtStatusToSocketError`. **`IOSB.Information` → `*optlen`** (`+0x7f8 ~ +0x816`; 4 GiB 넘으면 `*optlen=0`·WSAEFAULT). PENDING 이면 기다린 뒤 `IOSB.Status` (`+0xf40`) | 출력 바이트 (TL 이 `optval` 에 직접 씀) | `WSPGetSockOpt +0x7c4 ~ +0x848`, `+0xf25 ~ +0xf5d` (ret `+0x7cb`) |
| **`WSPIoctl +0x1fef`** (ioctl, 종류 3). 입력 `[RSP+0x190]`, IOSB `R10` (= `[RSP+0x1c0]` = 입력 **`+0x30`**; 겹침을 쓰면 `lpOverlapped`), **출력 = `lpvOutBuffer` (`R15`) · `cbOutBuffer`** | status: `0` → 성공 (`*lpcbBytesReturned = IOSB.Information`, `+0x2196 ~ +0x21a2`); `0x103` → 겹침이면 **WSA_IO_PENDING (`0x3e5`)**, 아니면 기다림 (`+0x2071 ~ +0x2096`); 음수 → `NtStatusToSocketError`·`*lpcbBytesReturned = IOSB.Information` | 출력 바이트 | `WSPIoctl +0x2007 ~ +0x21a5` (ret `+0x200e`) |

**로그 호출 판별 `[정적]`**: 요청서의 "입력 `0x20` · **출력 8 B 힙 · IOSB = 입력 주소 + `0x30`**" 은 위 세 줄 중 **`WSPIoctl +0x1fef` 만** 맞는다 (설정은 +0x20, 읽기는 −0x10). 즉 로그의 그 호출은 `getsockopt` 가 아니라 **앱이 `WSAIoctl` 을 불러 TL 에 넘긴 것** — 입력 `+0x00 = 3`, `+0x04 = 0`, **`+0x08` = 앱이 준 `dwIoControlCode`**, `+0x0c = 1`, `+0x10` = `lpvInBuffer` (입력 버퍼가 없으면 0), `+0x18` = `cbInBuffer`. 어떤 코드인지는 로그의 **입력 `+0x08` DWORD** 를 보면 바로 알 수 있다 — 정적으로는 앱이 고른 값이라 못 가린다 `[공백]`. 출력 8 B 는 호출자의 `cbOutBuffer = 8` 이다.
(참고: 요청서가 말한 "출력 없는 `0x120bf` 의 `+0x00 = 1`" 은 `WSPSetSockOpt` 줄과 일치.)

> **R3 지금 우리: 틀림 (빠져 있음).** `0x120bf` 는 afd 갈래의 기본 대답. 하나로 합칠 수 있는 정직한 규칙: ① 입력 길이 < `0x20` → `0xC000000D`, ② 종류가 1·2·3 이 아니거나 표지 ≠ 1 이거나 레벨이 허용 표 밖 → `0xC000000D`, ③ 그 밖은 **TL 몫** — 정책값 + `TODO(VM)`. (이번 K03 의 막힘은 `0x120b3` 이라 이 줄은 나중에 써도 된다.)

#### 최소 응답 (묶음 나) — TL 몫이라 정책값만

| 호출 | 반환 status | `IoStatusBlock.Status` | `.Information` | 출력 |
|---|---|---|---|---|
| 설정 (종류 1) 알려진 옵션 | `0` `[추측]` | `0` | `0` | 없음 |
| 읽기 (종류 2) 모르는 옵션 | **정하지 않음** — "미확인으로 멈춤" 이 정직 | | | |
| ioctl (종류 3) 모르는 코드 | **정하지 않음** | | | |

TL 이 모르는 옵션에 줄 status (`0xC00000BB` 로 `WSAENOPROTOOPT` 가 되는 길이 읽기 쪽에 있음, 위 표) 를 포함해 값은 `tcpip` 몫이다.

---

## 묶음 다 — 연결 길 남은 칸

### R4. afd `AfdBind` (rva `0x39160`, 2997 B) · `AfdTLBind` · `AfdTLBindSecurity` · `AfdTLBindComplete` · `AfdTLBindComplete2` · `AfdTLBindGetAddrComplete`

**이 코드를 부르는 줄**: `0x12003` → `AfdIoctlTable` 칸 `(0x12003>>2)&0x3ff = 0` → IRP 길 `AfdIrpCallDispatch` 의 0번 칸 (요청서·sast/121 표 `AfdBind`). 인자: RCX = IRP, RDX = 현재 스택 위치 (`+0x1a MOV R15,RCX`, `+0x2f MOV RAX,[RDX+0x30]`). 엔드포인트 = `[[RDX+0x30]+0x18]` (`+0x33 MOV RDI,[RAX+0x18]`).

**TL 모드 = 엔드포인트 `+8` 비트 8** (`+0x43 ~ +0x4a BT EAX,8 / JC +0x7a`). 아래 표는 TL 모드 (기본 TCP/UDP, 전송 이름이 빈 EA) 만. 비트 8 이 꺼진 쪽 (TDI 모드: 입력이 `TRANSPORT_ADDRESS` 모양) 은 `+0x4c ~ +0x76`, `+0x1ca ~ +0x1df`, `+0x3b3 ~ +0x45a` 에 있고 이번에 안 풀었다 `[공백]`.

#### (a) 검사 표 (TL 모드, 유저 모드)

| 차례 | 조건 | status | Information | 출력에 쓰는 것 | 줄 (`AfdBind.asm`) |
|---|---|---|---|---|---|
| 1 | 엔드포인트 `+5` 비트 `0x10` **그리고** `+6` 비트 `0x01` 가 둘 다 켜짐 | **`0xC0000022`** | 0 | — | `+0x7a ~ +0x93` → `+0xbae` |
| 2 | 입력 길이 < 6 | **`0xC000000D`** | 0 | — | `+0x98 ~ +0x9e` → `+0xba0` |
| 3 | 출력 길이 < 2 | `0xC000000D` | 0 | — | `+0xa4 ~ +0xaa` |
| 4 | 출력 길이 < 입력 길이 − 4 (= sockaddr 바이트 수 `R12D`) | `0xC000000D` | 0 | — | `+0xb0 ~ +0xbf` |
| 5 | 엔드포인트 `+0x170` ≠ 0 (`LOCK CMPXCHG 0→3` 실패: 다른 TL 요청 중·이미 묶는 중) | `0xC000000D` | 0 | — | `+0xc5 ~ +0xe0` → `+0xba5` |
| 6 | 엔드포인트 종류 (`[ep+2]`) 가 1·2 가 아님 (이미 묶였거나 연결됨) | **`0xC0000238`** (`STATUS_ADDRESS_ALREADY_ASSOCIATED`) | 0 | — | `+0xe5 ~ +0xf9`, `+0x101 ~ +0x10d` |
| 7 | (유저) 입력 포인터 4 정렬 아님 | `0xC00002C5` | 0 | — | `+0x13d ~ +0x15a` |
| 8 | (유저) 입력 범위가 유저 영역 밖·읽기 실패 | `0xC0000005` | 0 | — | `+0x15f ~ +0x177` (SEH `[+0x13d,+0x35b)` → 착지 `+0x93f`) |
| 9 | **주소 계열 (입력 `+4` 의 WORD) ≥ `0x23`** | **`0xC0000141`** (`STATUS_INVALID_ADDRESS`) | 0 | — | `+0x216 ~ +0x22c` |
| 10 | **`sockaddr` 바이트 수 (입력 길이 − 4) < `SOCKADDR_SIZE(계열)`** | **`0xC0000141`** | 0 | — | `+0x24a ~ +0x26c` |
| 11 | 출력 MDL 만들기·매핑 실패 | `0xC000009A`; 잠금 예외는 예외 코드 | 0 | — | `+0x29c ~ +0x351` |
| 12 | **성공 갈래 (TL 모드)** | **`0x103` (STATUS_PENDING) 로 돌아감** — 진짜 결과는 TL 완료 콜백이 낸다 | — | — | `+0x35b ~ +0x3ae` |

- 입력 칸: 입력 `+0x00` DWORD = 공유 방식 (`[RSP+0x60]`, `+0x1b1 ~ +0x1b3`; mswsock 쪽 값 0/1/2 는 sast/121), `+0x04 ~` = sockaddr (`R12D` 바이트, 풀 태그 `'Afdl'` 로 복사 `+0x17c ~ +0x1c5`, `+0x20d ~ +0x211`).
- 성공 갈래 (12): `[ep+0xf0]` = 복사한 sockaddr 풀, `[ep+0xec]` = 길이 (`+0x364 ~ +0x370`), 스택 위치 `+3` 의 비트 0 (`SL_PENDING_RETURNED`) 켬 (`+0x377 ~ +0x37e`), **`AfdTLBindSecurity`** (`+0x3a4`, ret `+0x3a9`) → `MOV EAX,0x103`.
- 동기 실패 (1~11) 에서는 `[ep+0x170]` 을 0 으로 되돌리고 (`+0xb63 ~ +0xb65 XCHG`) MDL 풀고 IRP 를 `IofCompleteRequest` 로 끝낸다 (`+0xb6b ~ +0xbd1`, ret `+0xbd1`).
- 길이 `0x10` 에 대해: 입력 길이 `0x10` = sockaddr 바이트 `0xc` 라서 **`AF_INET` 이면 10 번에 걸려 `0xC0000141`** (`SOCKADDR_SIZE(2)` = 16). mswsock `WSPBind` 는 `namelen < MinSockaddrLength` (`+0x1a7 ~ +0x1ab`) 를 먼저 거르고, TL 모드 입력 길이 = `namelen + 4` (`+0x229 LEA ESI,[R13+4]`) 라서 IPv4 정상값은 `0x14`. 로그의 `0x10` 이 어떤 호출인지는 로그 쪽 확인이 필요하다 `[공백]`.

#### TL 로 넘어간 뒤 (afd 가 부르는 순서)

| 차례 | 함수 | 하는 일 | 줄 |
|---|---|---|---|
| 1 | `AfdTLBindSecurity(IRP, 엔드포인트, 공유 방식)` | 공유 방식 `1` (재사용) · `3` (독점) · 엔드포인트 비트 `0x1e`·`0x1f` 에 따라 **TL 에 옵션 4 (`SO_REUSEADDR` 와 값이 같음)·옵션 `0xfffffffb` (`SO_EXCLUSIVEADDRUSE` 와 값이 같음) 를 켜거나 끄는 TL 요청 `[추측: 이름은 값 기준]`**을 먼저 보내고 (`AfdTLIoControl` `+0x90`, 콜백 `0x36390`), IRP `+0x78` 에 공유 방식을 적어 둠. 해당 없으면 곧바로 `AfdTLBind`. | `AfdTLBindSecurity +0x1b ~ +0x9a`, `+0x9f ~ +0x14a` |
| 2 | `AfdTLBind(IRP, 엔드포인트)` | TL 요청 블록 (콜백 `AfdTLBindComplete` `0x13d70`, 동작 번호 `8`, sockaddr = `[ep+0xf0]`, 길이 `[ep+0xec]`) 으로 **TL 제공자의 bind** 호출. `0x103` 이면 보류. `[ep+0x100] ≤ 0` (TL 끝점 없음) → `0xC0000008`. `0xC0000206` → `0xC0000023`. | `AfdTLBind +0x1f ~ +0xd5`, `+0x12e ~ +0x13a` |
| 3 | `AfdTLBindComplete(IRP, status)` | status < 0 이거나 엔드포인트 태그가 `0xAFD1` 이 아니면 `AfdTLBindComplete2`. (`0xAFD1` 인 경우 = 데이터그램류 TL 클라이언트 등록 후 `AfdTLDgramBindComplete` — 이번에 안 읽음) | `AfdTLBindComplete +0x1f ~ +0x35`, `+0xc4 ~ +0xd2` |
| 4 | `AfdTLBindComplete2(IRP, 엔드포인트, status, 참조 해제 여부)` | **실패** (`ESI < 0`): 풀 `[ep+0xf0]` 해제, `[ep+0x170]` ← 0, MDL 해제, **status `0xC0000043`·`0xC000020A` 이고 공유 방식 (`[IRP+0x78]`) = 1 이면 `0xC0000022` 로 바꿈**, IRP 완료. **성공**: 엔드포인트 종류 `[ep+2]` ← **3** (또는 연결 객체가 필요한 소켓 (`+5` 비트 `0x10` & `+6` 비트 1) 은 연결 객체 `AfdCreateConnection`, 태그 ← `0xAFD2`, 종류 ← 4, 참조 `+1`) 하고, **TL 에 "로컬 주소 달라" 요청** (콜백 `AfdTLBindGetAddrComplete` `0x4b90`, 동작 번호 `0x10`, 버퍼 = `[ep+0xf0]`, 길이 `[ep+0xec]`) | `AfdTLBindComplete2 +0x4c ~ +0x143` (성공), `+0x148 ~ +0x25f` (실패) |
| 5 | `AfdTLBindGetAddrComplete(IRP, status, 바이트 수)` | status < 0 이면 `[ep+0x170]` ← 0, MDL 해제, IRP 에 그 status. 성공이면 **`TdiCopyBufferToMdl(소스 = [ep+0xf0] (TL 이 채운 로컬 주소), 소스 길이 = TL 이 준 바이트 수, MDL = 출력 버퍼)`** → **출력 버퍼 `+0` 부터 로컬 sockaddr**, `[ep+0xec]` ← 복사한 바이트, `[ep+0x170]` ← 0, **IRP `Information` ← TL 이 준 바이트 수**, **`Status` ← 복사 결과 (보통 0)**, `IofCompleteRequest` | `AfdTLBindGetAddrComplete +0x33 ~ +0x5b`, `+0x85`, `+0xc8 ~ +0xd1` |

#### (b) 출력 바이트 칸 표 (성공)

| 오프셋 | 크기 | 뜻 | 값 | 쓰는 줄 |
|---|---|---|---|---|
| 출력 `+0` ~ `+Info−1` | TL 이 준 바이트 수 (IPv4 `16` · IPv6 `28` `[추측: TL 이 입력 sockaddr 과 같은 길이로 채움]`) | **묶인 로컬 sockaddr** (`sa_family` · 포트 · 주소 · 나머지) | **포트는 TL (tcpip) 이 고른 값**, 주소는 TL 이 답한 값 (와일드카드로 묶었으면 와일드카드 그대로 `[추측]`) | `AfdTLBindGetAddrComplete +0x54` (`TdiCopyBufferToMdl`) |
| `IoStatus.Information` | 8 | 위 바이트 수 | TL 몫 | `AfdTLBindGetAddrComplete +0xc8 ~ +0xca` (`MOV EAX,EBP; MOV [RBX+0x38],RAX`) |
| `IoStatus.Status` | 4 | 복사 결과 | 보통 `0` | `+0xce` (`MOV [RBX+0x30],EDI`) |

- **임시 포트 (포트 0 으로 묶을 때) 를 누가 고르나**: **TL (tcpip) 몫.** afd 에는 포트 칸을 쓰는 줄이 없다 — 입력 sockaddr 을 풀에 복사해 TL 에 그대로 건네고 (`AfdBind +0x17c ~ +0x1c5`, `AfdTLBind +0x58 ~ +0x69`), TL 이 준 로컬 주소를 `TdiCopyBufferToMdl` 로 복사만 한다.
- 출력 길이 ≥ 입력 길이 − 4 가 보장돼서 (검사 4) TL 이 채운 바이트는 항상 출력에 들어간다.

#### 묶인 뒤 바뀌는 엔드포인트 칸 `[정적]` (이름은 모두 PDB 에 없음 — 값·쓰임으로 `[추측]`)

| 칸 | 변화 | 줄 |
|---|---|---|
| `[ep+2]` 종류 | **1·2 → 3** (연결 객체가 붙는 소켓은 → 4, 태그 `0xAFD0` → `0xAFD2`, `[ep+0xc0]` = 연결 객체, `[ep+0x40]` 참조 +1) | `AfdTLBindComplete2 +0xf1` (종류 3), `+0xb4 ~ +0xe9` (연결 객체 경로: 태그 `+0xdb`, 종류 4 `+0xe5`) |
| `[ep+0x170]` | 0 → 3 (묶는 중) → 0 (끝남). 다른 TL 요청·묶기는 이 동안 `0xC000000D`/`0xC0000184` | `AfdBind +0xcc ~ +0xd0`, `AfdTLBindGetAddrComplete +0x85` |
| `[ep+0xf0]`·`[ep+0xec]` | 성공 후에도 **TL 이 준 로컬 sockaddr 풀과 길이**가 남음 (이름 얻기 `AfdGetAddress` 가 쓸 것 `[추측]`) | `AfdBind +0x364 ~ +0x370`, `AfdTLBindGetAddrComplete +0x47 ~ +0x5b` |
| IRP `+0x78` | 공유 방식 (엔드포인트 칸 아님) | `AfdTLBindSecurity +0x53`, `+0xf9`, `+0x138` |

#### (c) 부르는 쪽 표 — mswsock `WSPBind` (`0x12003` 을 내는 줄 `+0x3cf`)

| 호출 | 보는 것 | 안 보는 것 | 줄 (`WSPBind.asm`) |
|---|---|---|---|
| `NtDeviceIoControlFile(…, IOSB `[RSP+0xa8]`, 0x12003, 입력 = 출력 = `R14`, 입력 길이 `[RSP+0x58]`, 출력 길이 `[RSP+0x68]`)` ret `+0x3f9` | 반환 `ECX`; **`0x103` 이면 소켓 락을 풀고 `SockWaitForSingleObject(…, 3, 4)` 로 기다린 뒤 `ECX = IOSB.Status` (`[RSP+0xa8]`)** (`+0x405 ~ +0x447`). **음수 → `NtStatusToSocketError`** (`+0x479 ~ +0x482`). 성공이면 **`IOSB.Information` (`[RSP+0xb0]`) = 묶인 주소 길이 `R13D`** (`+0x45b`), 출력 버퍼에서 그 길이만큼 `memcpy` → 소켓 로컬 주소 칸 `[sock+0xa8]` (`+0x4bf`, ret `+0x4c4`) | 출력 바이트 안의 **개수·포트 검사 없음** | `+0x3cf ~ +0x4bf` |

→ 에뮬레이터는 **status 0 + Information = sockaddr 길이 + 출력에 sockaddr** 을 모두 맞춰야 `WSPBind` 가 정상이다. `IOSB.Status` 는 PENDING 일 때만 쓰이므로 동기 완료라면 반환값만 맞으면 된다.

> **R4 지금 우리: 틀림 (빠져 있음).** 에뮬레이터 주석에 "`0x12003` (AfdBind) 을 기본 대답으로 넘겨 묶임 상태가 없음" 이라 적혀 있고, `0x12007` 연결 쪽이 묶임 상태를 안 보는 것으로 줄인 것과 같은 곳. 아직 K03 막힘과는 무관 (`0x12003` 은 로그의 `0x120b3` 앞에 안 옴).

### R5. afd `SOCKADDR_SIZE` (rva `0x5b40`, 29 B)

**동작**: `SOCKADDR_SIZE(WORD 계열)`: 계열 < `0x23` 이면 표 `sockaddr_size[계열]` 의 한 바이트, 아니면 표 `[0]` 의 한 바이트 (`SOCKADDR_SIZE.asm +0x0 ~ +0x1d`). 표는 `.rdata` `0x7ad80` 의 `.sys` 원본 바이트를 읽었다 (PDB 이름 `sockaddr_size`).

| 계열 (십진·16진) | 길이 (바이트) | 비고 |
|---|---|---|
| 0 (`0x0`) AF_UNSPEC | **0** | 표 `[0]` |
| 1 (`0x1`) AF_UNIX | **110** (`0x6e`) | `sockaddr_un` 크기 |
| **2 (`0x2`) AF_INET** | **16** (`0x10`) | |
| **23 (`0x17`) AF_INET6** | **28** (`0x1c`) | |
| 34 (`0x22`) (AF_HYPERV 와 같은 번호 `[추측]`) | **36** (`0x24`) | |
| 3 ~ 22, 24 ~ 33 (NetBIOS `0x11` 포함) | **0** | 표 안이지만 비어 있음 → 길이 검사가 항상 통과 |
| **≥ 35 (`0x23`) 표 밖** | **0** (표 `[0]` 값) | 호출한 쪽이 먼저 거름 (아래) |

- **부르는 쪽 (afd, 읽은 것)**: `AfdBind +0x24a` (ret `+0x24f`) — `CMP CX,0x23; JC` (`+0x219 ~ +0x21d`) 로 **계열 ≥ `0x23` 은 이 함수에 들어오기 전에 `0xC0000141`**, 그 뒤 `sockaddr 바이트 수 < SOCKADDR_SIZE` 이면 `0xC0000141`; `AfdConnect +0x514` (ret `+0x519`, sast/121) 같은 모양; `AfdSuperConnect +0x1d3` (ret `+0x1d8`). 그 밖 호출 지점은 `.c` 머리의 caller 목록에 WSK 함수 (`Wsk…`) 와 데이터그램 송신 함수들이 더 있다 (이번엔 안 읽음).
- **에뮬레이터에 필요한 것**: 계열 2 → 16, 계열 `0x17` → 28 (이 둘로 `AfdBind`·`AfdConnect` 의 `SOCKADDR_SIZE` 줄이 정확히 맞는다). 표 밖 계열은 `SOCKADDR_SIZE` 에 도달하기 전에 `0xC0000141`.

> **R5 지금 우리: 틀림 (빠져 있음).** 에뮬레이터 `AfdConnect` 갈래 주석이 "공사 중: 주소 길이 ≥ SOCKADDR_SIZE(계열) — SOCKADDR_SIZE asm 없음" 이라 적었던 자리가 이 표로 메워진다.

---

## 동적으로 확인할 것 (묶음 나·다)

멈춤점은 모두 test.exe 일 때만 (프로세스 조건). 오프셋은 `.asm` 의 ret (콜스택 `caller+0x…`) 와 같다. sast/121 "동적" 4·6 과 같은 VM 세션에 얹을 수 있다.

1. **`0x120bf` 호출이 어떤 코드인지** (R3 로그 판별 확인): `mswsock!WSPIoctl+0x2007` (`CALL NtDeviceIoControlFile` 자리) 에서 `db @rsp+0x190 L20` — `+0x00 = 3`, `+0x08` = 코드, `dq @rsp+0x1c0 L2` = IOSB. 같은 눈금으로 `mswsock!WSPSetSockOpt+0x1fd` (`db @rsp+0x70 L20`, IOSB `@rsp+0x90`), `mswsock!WSPGetSockOpt+0x7c4` (`db @rsp+0x78 L20`, IOSB `@rsp+0x68`).
2. **TL 이 준 status·Information·출력**: `afd!AfdCompleteTliIoControlCore+0xb9` (`MOV [RBX+0x30],ESI`) 에서 `esi` (IRP status), `rbp` (Information), `afd!AfdTliIoControl+0x7f0` (`AfdTLIoControl` ret) 에서 `eax` (TL 즉시 반환), `dq @rsp+0x98 L1` (요청 블록 `+0x48` Information). 작은 출력 길이 (4·1) 로도 한 번 — `0xC0000206` → `0xC0000023` 변환 확인 (`afd!AfdTliIoControl+0xa09`).
3. **bind 의 최종 값**: `afd!AfdBind+0x3a9` (ret, `eax` = `0x103`), 이어 `afd!AfdTLBindGetAddrComplete+0xce` (`MOV [RBX+0x30],EDI`) 에서 `edi` (status), `ebp` (Information), `db poi(@rsi+0xf0) L@ebp` (로컬 sockaddr). 포트 `+2..+3` 이 임시 포트 범위인지 (TL 몫 `[공백]`). 와일드카드로 묶었을 때 주소 칸이 와일드카드로 남는지.
4. **bind 입력 길이 `0x10` 의 정체**: `afd!AfdBind+0x219` 에서 `ecx` (계열), `r12d` (sockaddr 바이트). `mswsock!WSPBind+0x3cf` 의 `[RSP+0x58]` 이 `0x10` 인 호출이 어느 계열·`namelen` 인가.
5. **`SOCKADDR_SIZE` 표** 는 정적으로 닫혔다 (`.sys` 원본 바이트) — 동적 확인 불필요.

## 지금 우리 (PE_emulator) 맞나·틀리나 — 요약

| 코드 | 지금 | 해야 할 일 (쓰는 곳 `nt_device_io_control_file.cpp` afd 갈래) |
|---|---|---|
| `0x120b3` | **틀림 (없음)** — K03 막힘 | 묶음 가 "최소 응답". 개수를 **항상** 쓰는 것이 핵심 |
| `0x120bf` | 틀림 (없음) | 묶음 나: 입력 검사 + TL 몫 정책 (`TODO(VM)`). 급하지 않음 |
| `0x12003` | 틀림 (기본 대답) | 묶음 다: 검사 표 + (동기로 가짜 PENDING 은 필요 없음 — 대답을 완료 값으로) + 출력 sockaddr + Information |
| `SOCKADDR_SIZE` | 빠짐 (`AfdConnect` 줄임에 "asm 없음") | 표 (계열 2 → 16, `0x17` → 28 …) |

## 다음 요청 후보

1. **ws2_32 `WSAIoctl` · `getsockopt` 바깥껍질** (부른 쪽 되풀이): 로그의 `0x120b3` 3 연속 호출과 `*lpcbBytesReturned` 가 앱까지 어떻게 가는지 — 이번엔 mswsock 까지만 봄 `[공백]`. (ws2_32 analyze 필요)
2. **netio `NsiRegisterChangeNotificationEx` / `NsiAllocateAndGetTable` 초기 알림이 동기인가**: `AfdInitializeAddressList` 직후 목록이 차는지 — 동적 5 번이 먼저 (정적은 netio.sys 새 analyze).
3. **tcpip TL 제공자** (`getsockopt`·`setsockopt`·`bind` 포트 선택·`SO_COMPARTMENT_ID`): 이번 `0x120bf`·`0x12003` 의 값 쪽이 전부 여기 — 큰 모듈 (analyze 수 분 ~ 수십 분 `[추측]`). 필요한 옵션 목록을 에뮬레이터에서 먼저 좁히고 부르는 것이 좋다.
4. afd `AfdGetAddress` · `AfdGetRemoteAddress` (이름 얻기, `[ep+0xf0]` 쓰는 곳) · `AfdHandleISBIoctls` · `AfdSetInformation` (`0x1203b`) · `AfdQueryHandles` (`0x12037`) · `AfdSuperConnect` (`0x120c7`) — sast/121 후보 2 의 남은 칸.
5. mswsock `WSPGetSockOpt` 의 SOL_SOCKET 옵션 표 (어느 옵션을 mswsock 이 자기 칸에서 답하고 어느 것이 TL 로 가나) — 이번엔 `0x120bf` 를 내는 갈래 (`+0x744 ~ +0x7d4`) 와 몇 개 (`SO_ERROR` 등) 만 봤다.

## 판정

**분석성공 (묶음 가·나·다).** R1·R2 는 `.asm` 줄로 닫았다. R3 는 afd 쪽이 "TL 에 넘김" 임을 닫았고 값은 TL 몫이라 정책값으로 넘겼다 (정직하게 "정적으로 못 정함"). R4·R5 는 닫았고 임시 포트·로컬 주소는 TL 몫.

## 진행 표

| 절 | 상태 |
|---|---|
| 묶음 가 (R1·R2) | 끝 (2026-11-17, 결과서에 먼저 써 둠 — `SendMessage` 중간 알림은 도구가 없어 못 보냄) |
| 묶음 나 (R3) | 끝 |
| 묶음 다 (R4·R5) | 끝 |

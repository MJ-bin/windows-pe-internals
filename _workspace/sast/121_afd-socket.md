# 121 afd-socket — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **새 Q217** (PE_emulator 요청 "소켓 장치 (AFD)" — 튜닝 `278_tune06.md P1`). 사용자 결정 2026-11-16 "지금 즉시 먼저" (분석 멈춤 중 예외). **정적 먼저.** 정적으로 안 닫히는 것 (드라이버가 돌려주는 실제 값) 은 "동적으로 확인할 것" 으로만 적는다 — VM 은 이번에 안 함.
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/` 의 `mswsock.dll` (451,920 B) · `nsi.dll` (51,128 B) · `drivers/afd.sys` (738,792 B) · `drivers/nsiproxy.sys` (77,824 B). **넷 다 아직 분석 안 됨** (`ms/disassemble/` 에 폴더 없음) — PDB 는 `fetch_pdb.py` 로 MS 심볼 서버에서 받고 GUID 를 확인한다.
- 이미 아는 것 (요청한 쪽에서 옴):
  - 에뮬레이터 로그 (진짜 관찰 아님): `NtCreateFile("\Device\Afd\Endpoint", 접근 0xc0140000, …)` 뒤 같은 스레드의 다음 syscall 이 `NtDeviceIoControlFile` `0x1207b` × 2 (501/535 샘플), 셋째가 `0x12047` (449) 또는 `NtAllocateVirtualMemory` (52). `0x1207b` 는 입력 = 출력 `0x10` 바이트 (요청한 쪽 셈).
  - 가짜 핸들 (`-1`) IOCTL → `0xC0000024` `[정적]` `ms/disassemble/ntoskrnl/IopXxxControlFile.asm +0x1e0 ~ +0x231`.
  - 이 레포에 afd·mswsock·nsi 분석은 0 (HEAD `d72abae`).
- 주의: 같은 시간에 다른 decompiler 가 **ntoskrnl** (sast/122 worker-factory) 을 돈다 — ntoskrnl 쪽은 건드리지 않는다 (ntoskrnl `.asm` 읽기만은 됨). 새 모듈 analyze 는 서버 메모리를 보고 **한 번에 하나씩**. 함수는 필요한 것만 `^이름$`.

## 알고 싶은 것

1. **열기** (mswsock, 유저 쪽) — 우선
   - (a) `socket()`·`WSASocketW()` 길에서 mswsock 이 `\Device\Afd\Endpoint` 를 `NtCreateFile` 로 열 때 넘기는 **EA 버퍼 모양**: EA 이름 (글자 그대로), EA 값 안에서 주소 계열·소켓 종류·프로토콜 (그 밖의 칸도 있으면) 이 놓인 오프셋·크기. 그 버퍼를 짓는 함수 이름 + `.asm` `+0x` 줄.
   - (b) 열기 뒤 내는 `0x1207b`·`0x12047` 이 각각 어느 함수의 어느 줄에서 나가나, 입력·출력 버퍼의 칸 (오프셋·크기·뜻), 제어 코드 이름 (PDB·헤더 이름이 있으면 — 없으면 "이름 없음").
   - (c) mswsock 이 그 대답에서 무엇을 보나 — status 만 보나, 출력 칸을 읽나 (어느 칸, 어떤 갈래). status 가 실패면 `socket()` 이 무엇을 돌려주고 `WSAGetLastError` 에 무엇이 남나 (그 바꾸기 표 함수).
   - (d) 우리 에뮬레이터는 지금 `socket`/`WSASocketW`/`WSASocketA` 가 **전부 −1** 입니다. 위 (a)~(c) 로 보아 "소켓을 만들기에 성공하려면 열기·IOCTL 셋이 각각 최소 무엇을 돌려줘야 하나" 를 한 줄씩.
2. **연결** (mswsock + afd.sys)
   - (a) `connect()` (와 `WSAConnect`·`ConnectEx` 가 같은 길이면 같이) 가 내는 IOCTL 코드와 입력 모양 — sockaddr 가 놓인 오프셋, 그 앞뒤 칸.
   - (b) **갈 길이 없는 PC** (어댑터 없음·선 뽑힘) 에서 afd.sys 가 돌려줄 status 후보 — afd.sys 에서 연결 요청을 받는 함수와, 경로가 없을 때 status 를 정하는 줄 (afd 가 아래 전송 계층에서 받은 값을 그대로 넘기면 "아래 계층이 정함, afd 는 넘김" 으로 끝내고 tcpip 는 이름만). 정적으로 값이 안 정해지면 `[추측]` 후보 + "동적으로 확인할 것".
   - (c) mswsock 이 그 status 를 `WSAGetLastError` 값으로 바꾸는 표 (함수 이름 + 표 내용 — 연결 관련 status 몇 개만이면 됨: 네트워크 닿지 않음·호스트 닿지 않음·연결 거부·시간 초과 류).
   - (d) 블로킹 길과 오버랩 (완료 포트) 길에서 완료가 오는 꼴 — IoStatusBlock 을 언제 읽나, 이벤트를 기다리나 (`NtWaitForSingleObject` 류), `STATUS_PENDING` 일 때 갈래. 완료 포트 길은 mswsock 쪽에서 보이는 것만.
3. **어댑터 묻기** (nsi.dll → nsiproxy.sys) — **급함 낮음, 시간이 길어지면 "다음 요청 후보" 로 넘겨도 됨**
   - `\??\Nsi` 열기 뒤 `0x12000f`·`0x12001b`·`0x120007` 의 입력 모양 (nsi.dll 쪽), 길이 먼저 묻기 규칙 (작은 버퍼 → 무슨 status·필요 길이가 어느 칸에). 루프백만 있는 PC 의 대답이 빈 표인지는 nsiproxy 가 정하면 `[추측]` + 동적.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| mswsock | `socket`/`WSASocketW` 가 닿는 소켓 만들기 함수 (PDB 이름으로 찾기 — `WSPSocket` 류) + EA 짓기 + 열기 뒤 IOCTL 둘 | 1 | 질문 1 |
| mswsock | `WSPConnect` 류 + 연결 IOCTL + status → WSA 오류 바꾸기 | 1 | 질문 2 |
| afd.sys | IOCTL 받는 표 (디스패치) 에서 `0x1207b`·`0x12047`·연결 코드의 처리 함수 — 입구와 status 정하는 줄만 | 0~1 | 질문 1 (c)·2 (b) |
| nsi.dll | `\??\Nsi` IOCTL 셋을 내는 함수 | 0 | 질문 3 (낮음) |

- 범위: 연결 **시도까지**. 보내기·받기·이름 풀기 (DNS)·tcpip.sys 본문은 이름만. ws2_32 는 mswsock 으로 넘어가는 다리만 (이미 아는 것이면 한 줄).

## 받고 싶은 것

- 결과서 `_workspace/sast/121_afd-socket_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`/`[추측]`/`[공백]`. EA·IOCTL 입력/출력은 **칸 표** (오프셋 · 크기 · 뜻 · 근거 줄) 로.
- PDB GUID 확인 줄 (모듈마다).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 — 쓰는 곳: `ext/syscall/syscall_layer_cpp/src/file/nt_create_file.cpp` (EA 받기 — 지금 `extended_attributes_unsupported` 멈춤), `src/file/nt_device_io_control_file.cpp` (AFD 제어 코드 몸), `include/syscall_layer/files.hpp` (장치 종류에 afd). 원칙: 진짜 망에 안 나감, 소켓은 만들어지고 연결은 망 없는 PC 처럼 정직하게 실패.
- 끝에 **"동적으로 확인할 것"** (선 뽑은 VM 에서 `socket` → `connect` 한 번의 IOCTL 입출력 바이트로 닫힐 것 위주) 과 **"다음 요청 후보"**.

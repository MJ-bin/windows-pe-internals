# 126 nsi-callers — 정적 분석 요청

- 날짜: 2026-11-21
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **새 Q221** (PE_emulator 요청 "WinHTTP·iphlpapi 가 NSI (`\\.\Nsi`) 에 무엇을 묻고 대답을 어떻게 읽나 — 루프백만 있는 PC 의 최소 대답", 튜닝 `290_tune09.md N1`). 사용자 결정 2026-11-21 "이것들부터 먼저". **이번엔 정적 (질문 1·2) 만.** 원 요청의 3 (VM 실제 바이트) 은 동적 — 이번에 안 하고 오케스트레이터가 대기열 (Q222) 에 둔다. 결과서 "동적으로 확인할 것" 에 멈춤점만 적는다.
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/` 의 `winhttp.dll` (1,232,864 B) · `IPHLPAPI.DLL` (220,528 B — 파일 이름 대문자) · `dnsapi.dll` (1,422,944 B) · `webio.dll` (881,648 B) — **넷 다 아직 analyze 안 됨**. 부르는 쪽이 어느 모듈인지부터 가려서 **필요한 모듈만** analyze (PDB `fetch_pdb.py`, GUID 확인, 서버 메모리 보고 한 번에 하나씩). nsi·nsiproxy 는 sast/121 에서 analyze 됨 (nsi GUID `662A028F57A7C702B11F583CE15A8E631`).
- 이어 쓰는 것: **sast/121 결과서** 3 절 (`_workspace/sast/121_afd-socket_result.md` — nsi IOCTL 셋·길이 먼저 묻기) 의 "다음 요청 후보 5"·"동적으로 확인할 것 7".
- 주의: 같은 시간에 다른 decompiler 가 **ntoskrnl** (sast/125) 을 돈다 — 그쪽은 건드리지 않는다. 방법은 가벼운 쪽 먼저: import 표·objdump 로 `NsiGetAllParameters(Ex)`·`NsiGetParameter(Ex)`·`NsiEnumerateObjectsAllParameters(Ex)`·`NsiAllocateAndGetTable` 을 부르는 모듈을 먼저 가린다.

## 왜 (요청한 쪽 말 요약)

- 에뮬레이터 로그 (진짜 Windows 관찰 아님): `WinHttpSendRequest` 안에서 `NtCreateFile("\??\Nsi")` 가 기본 대답 (핸들 −1) → `NtDeviceIoControlFile(−1, 0x12000f, 입력 = 출력 0x68)` 가 `0xc0000024` → `WinHttpSendRequest` 가 0. 소켓으로 나가 보기도 전에 실패.
- 방향 (요청한 쪽 사용자 결정): **루프백 하나만 있는 PC** 로 답함 · MAC 없음 · 진짜 망에 안 나감.
- 로그에서 센 것 (exe 가 부른 API → 그 안의 NSI 코드·입력 길이):

| API | 샘플 | NSI 코드 · 입력 길이 | 줄 |
|---|---|---|---|
| `WinHttpSendRequest` | 213 | `0x12000f` · `0x68` | 177,890 |
| `WinHttpGetIEProxyConfigForCurrentUser` | 105 | `0x12000f` · `0x68` + `0x12001b` · `0x70` | 17,377 |
| `GetAdaptersAddresses` | 129 | `0x12000f` + `0x12001b` | 10,432 |
| `GetAddrInfoW` · `getaddrinfo` · `gethostbyname` | 108 · 23 · 21 | `0x120007` · `0x50` | 9,778 |
| `GetAdaptersInfo` | 67 | `0x12001b` · `0x70` | 132 |

## 이미 아는 것 (sast/121)

- 열기: `nsi!NsiOpenDevice` → `CreateFileW(L"\\.\Nsi", 0, 3, OPEN_EXISTING, FILE_FLAG_OVERLAPPED)` → 전역 `g_NsiAsyncDeviceHandle` `[정적] ms/disassemble/nsi/NsiOpenDevice.asm +0x19 ~ +0x6d`.
- 틀: 이벤트 → `NtDeviceIoControlFile(핸들, 이벤트, …, 코드, 입력 = 출력 같은 버퍼)` → `0x103` 이면 기다림 → `IoStatusBlock.Status` `[정적] NsiGetParameterEx.asm +0x26 ~ +0x131`.
- 코드·길이: `0x120007` `0x50` (`NsiGetParameterEx +0x59`) · `0x12000f` `0x68` (`NsiGetAllParametersEx +0x52`) · `0x12001b` `0x70` (`NsiEnumerateObjectsAllParametersEx +0x51`).
- `0x12001b` 는 포인터 모두 0 으로 한 번 (개수 묻기) → 개수 + 여유로 한 번 더 `[정적] NsiAllocateAndGetTable.asm +0x119, +0x16c ~ +0x185, +0x36b`.
- nsi.dll 은 `status < 0` 이거나 `0x105` 면 `RtlNtStatusToDosError` 값을 돌려줌.
- 모르는 것: `0x68`·`0x50`·`0x70` 구조체 칸 뜻 (어느 모듈 ID·객체·키/설정/상태/고정 칸) · 부른 쪽이 실패·빈 대답에 어떻게 갈리나.

## 알고 싶은 것 — **`WinHttpSendRequest` 길을 먼저**

1. **누가 무엇을 묻나** — 위 표의 API 다섯에서 nsi 함수까지의 부름 사슬과 넣는 인자.
   - **(가장 급함)** `WinHttpSendRequest` → … → `NsiGetAllParameters(Ex)`: 어느 DLL 의 어느 함수가 부르나 (winhttp 스스로 / webio / IPHLPAPI 를 거치나).
   - 인자마다: 모듈 ID (16 B 값 그대로 — `.rdata` 상수, 이름이 PDB 에 있으면 이름도 `NPI_MS_*` 류) · 객체 번호 · 키/설정(RW)/상태(ROD)/고정(ROS) 칸의 포인터와 길이.
   - → `0x68`·`0x50`·`0x70` **구조체 칸 표** 셋 (`오프셋 · 크기 · 뜻 · 넣는 줄`). nsi.dll 쪽에서 인자 → 구조체 칸으로 옮기는 줄 (`NsiGetAllParametersEx` 등, 이미 있는 `.asm`).
2. **부른 쪽이 대답을 어떻게 읽나** — 1 의 부르는 자리마다:
   - 실패 status 면 그 API 가 통째로 실패하나, 그 정보 없이 이어 가나. "없는 줄" 을 뜻하는 status 가 따로 있나 (`STATUS_NOT_FOUND` 류 견주기 줄).
   - `0x12001b` 가 개수 0 (빈 표) 을 주면 부른 쪽이 어떻게 가나.
   - → **`WinHttpSendRequest` 가 이름 풀이·소켓 단계까지 가려면 NSI 에서 꼭 받아야 하는 최소 대답** 한 절 (sast/121 1(d) 꼴) — 열기·코드별 status·출력 칸.
   - 나머지 API (`WinHttpGetIEProxyConfigForCurrentUser`·`GetAdaptersAddresses`·`GetAddrInfoW`·`GetAdaptersInfo`) 는 1·2 를 같은 꼴로, 시간이 길어지면 `WinHttpSendRequest` 길을 결과서에 먼저 써 두고 나머지를 이어서 덧붙인다 (요청한 쪽: `WinHttpSendRequest` 길만 먼저 와도 시작).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| (가릴 것) winhttp / webio / IPHLPAPI / dnsapi | nsi 함수를 import·부르는 자리 → 그 함수 | 1 | 질문 1·2 |
| winhttp | `WinHttpSendRequest` 에서 그 자리까지의 사슬 (이름만 + 갈림 줄) | 0~1 | 질문 1 (가장 급함) |
| nsi | `NsiGetAllParametersEx`·`NsiGetParameterEx`·`NsiEnumerateObjectsAllParametersEx`·`NsiAllocateAndGetTable` (있음 — 읽기만, 인자 → 구조체 칸) | — | 질문 1 표 |

- 범위: NSI 묻기·읽기까지. 이름 풀이 본문 (DNS 패킷)·proxy 자동 찾기 본문·tcpip/nsiproxy 공급자 본문은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/126_nsi-callers_result.md`.
  1. **asm 을 남겨 줌**: 부르는 함수들 `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** + 구조체 칸 표 셋 + 부르는 쪽 표 (`호출 · 보는 것 · 안 보는 것 · 줄`).
  3. PDB GUID 확인 줄 (새로 analyze 한 모듈마다).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall/syscall_layer_cpp/src/file/nt_create_file.cpp` (`\??\Nsi` 열기 — 지금 `path_prefix_unsupported` 기본 대답) · `nt_device_io_control_file.cpp` (NSI 코드 셋 — 지금 몸 없음) · `tests/file.cpp`. 원칙: 루프백만 있는 PC, 진짜 망에 안 나감 — 표 내용이 공급자 몫이면 "칸 뜻을 아는 정책 값 + `TODO(VM)`".
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (원 요청 3: 랜선 뽑은 VM 에서 `WinHttpSendRequest`·`GetAdaptersAddresses`·`GetAddrInfoW` 한 번씩, NSI IOCTL 마다 입출력 바이트·IOSB — 멈춤점 `caller+0x…`) 과 **"다음 요청 후보"**.

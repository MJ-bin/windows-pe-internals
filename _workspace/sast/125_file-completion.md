# 125 file-completion — 정적 분석 요청

- 날짜: 2026-11-21
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **새 Q220** (PE_emulator 요청 "`NtSetInformationFile` 로 파일 (소켓) 을 완료 포트에 묶기 — 종류 `0x1e`·`0x29`", 튜닝 `290_tune09.md N2`). 사용자 결정 2026-11-21 "이것들부터 먼저". **정적만** (요청한 쪽 짐작: VM 필요 없음).
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인). 이미 analyze 된 프로젝트 — 다시 analyze 하지 말고 `^이름$` export 만.
- `_index.tsv`: `NtSetInformationFile` `0x3ab2d0` (4323 B, `.asm` 없음) · `IoSetIoCompletion` `0xa116c0` (38 B).
- 이어 쓰는 것: **sast/124 결과서** (`_workspace/sast/124_irp-complete_result.md` — IRP 완료 때 IOSB·이벤트·포트·`FO_SKIP_COMPLETION_PORT`), sast/122 결과서 (완료 포트 `_KQUEUE` 칸), sast/121·123 (afd).
- 주의: 같은 시간에 다른 decompiler 가 **winhttp·IPHLPAPI·dnsapi·webio·nsi** (sast/126) 를 돈다 — 그쪽은 건드리지 않는다.

## 왜 (요청한 쪽 말 요약)

- 에뮬레이터 로그 (진짜 Windows 관찰 아님): Go 런타임이 새 소켓마다 `NtSetInformationFile(소켓, …, 종류 0x1e)` · `(…, 종류 0x29)` 를 부름 → 에뮬레이터가 두 종류를 몰라 "성공인데 아무것도 안 함" 기본 대답. 그 뒤 `ConnectEx` 는 망 없는 PC 정책 실패 (`0xC000023C`) 로 바로 돌아옴.
- 이미 아는 것: 종류 이름 (공개 문서) `0x1e` = `FileCompletionInformation` (입력 = 포트 핸들 + 키, 16 B) · `0x29` = `FileIoCompletionNotificationInformation` (입력 = 플래그 4 B). 값·길이 검사·커널 칸은 모름. ReactOS 의 "이미 묶였거나 동기 핸들이면 `STATUS_INVALID_PARAMETER`" 는 Windows 11 미확인.
- sast/124: 드라이버가 오류 status 로 바로 돌아오면 IOSB·이벤트·포트를 안 건드림 `[정적]`.

## 알고 싶은 것

1. **종류 `0x1e` (완료 포트 묶기)** — `NtSetInformationFile` 안 이 갈래의 검사 차례와 status:
   - 입력 길이 · 포트 핸들 풀기 (요구 접근 권한·객체 종류가 다를 때 status) · 동기 핸들 (`FO_SYNCHRONOUS_IO`) 일 때 · 이미 묶였을 때.
   - 성공 때 파일 객체 어느 칸에 무엇을 쓰나 (포트·키 — `CompletionContext` 류, PDB `_FILE_OBJECT` 이름·오프셋과 그 가리키는 덩어리의 칸) · 포트 참조를 잡나 · IOSB 에 쓰는 값 (Status·Information).
   - 드라이버 (afd) 로 IRP 가 내려가나, I/O 관리자가 스스로 끝내나 (내려가지 않는 종류 목록이 있으면 그 줄).
2. **종류 `0x29` (완료 알림 방식)** — 입력 4 B 에서 받는 비트 · 못 받는 비트의 status · 파일 객체 어느 칸/비트에 옮기나 (`FO_SKIP_COMPLETION_PORT`·`FO_SKIP_SET_EVENT`·`FO_SKIP_SET_FAST_IO` 류 — PDB/헤더 이름이 없으면 비트 값만) · 묶기 (`0x1e`) 전에 불러도 되나 · 한 번 켠 비트를 끌 수 있나.
3. **묶인 파일의 I/O 가 끝날 때** (sast/124 를 잇는 한 절 — sast/124 줄을 가리키며 짧게) — 완료 패킷이 포트로 가는 조건:
   - 바로 성공으로 끝난 I/O + 2 의 "성공이면 포트 건너뛰기" 비트 · 바로 오류로 끝난 I/O · `ApcContext` 가 0 일 때.
   - → **"`ConnectEx` 를 바로 오류로 돌려줄 때 패킷을 안 보내는 것이 맞나"** 예/아니오 + 근거 줄. (fast I/O 길 — `IopXxxControlFile` 즉시 처리 쪽 — 이 따로 포트에 넣는지도, 이미 있는 `.asm` 으로 되면 한 줄)

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^NtSetInformationFile$` | 1 | 질문 1·2 |
| ntoskrnl | 종류 `0x1e`·`0x29` 갈래가 다른 함수로 나가면 그 함수 (`IoSetIoCompletion` 등, 필요한 것만) | 0~1 | 질문 1·2 |
| ntoskrnl | `IopCompleteRequest.asm`·`IopSynchronousServiceTail.asm`·`IopXxxControlFile.asm` (있음 — 읽기만) | — | 질문 3 |

- 범위: 이 두 종류만. 다른 정보 종류는 이름만 (점프표가 있으면 표 위치 한 줄).

## 받고 싶은 것

- 결과서 `_workspace/sast/125_file-completion_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/ntoskrnl/NtSetInformationFile.asm` (+ 갈래가 나간 함수). 근거는 `[정적] ms/disassemble/ntoskrnl/<함수>.asm +오프셋` 꼴 (요청한 쪽이 코드에 붙임).
  2. 맨 위 **"먼저 읽는 한 줄 답"**.
  3. 종류마다 **검사 표** — `차례 · 조건 · status · 쓰는 칸 · 줄 (+오프셋)`. 파일 객체 칸은 PDB 이름·오프셋과 함께.
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall/syscall_layer_cpp/src/file/nt_set_information_file.cpp:145` (지금 종류 14·16 만 받음) · `src/files.hpp` 파일 객체 (포트·키·알림 비트 칸) · `tests/file.cpp`; 3 의 답에 따라 `src/threadpool/nt_io_completion.cpp` (패킷 넣기) 를 부를지.
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

# 132 alpc-completion — 정적 분석 요청

- 날짜: 2026-11-23
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q248** (PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/309_internals-alpc-completion.md` — 원문을 먼저 읽을 것. 튜닝 `309_tune12.md` V3, 병목 199). Q246 (run133 남은 정적) 의 ②·③·⑤ 를 "필수" 로 당긴 것. 사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 둘째. **정적만.** 패킷 실제 값 확인은 VM (Q231 ④·⑤) — 이번엔 안 함.
- **먼저 읽는 것**: `_workspace/sast/130_lrpc-wire_result.md` 의 2-4 (비동기 답이 오는 길 — `AlpcSend +0x128`, `LrpcIoComplete +0xc3` → `IoCompleted +0xc8`), 3-1·3-3 (질문 탑·최소 탑), 4-2 (프로시저 5), 5 (서버 흉내 차례), "다음 요청 후보" 2·3·5. `_workspace/sast/127_winnsi-rpc_result.md` 162 줄 근처 (`EnableAsync` → `CreateAlpc +0x66` → `TpAllocAlpcCompletionEx(&결과, 포트 핸들, LrpcIoComplete, …)`). 일꾼 공장·완료 포트·기다림 패킷 앞 결과: `runs/03_sprint/run125-worker-factory.md`, `run127-file-completion.md`, `run93-wait-packet.md`.
- 대상 빌드: Windows 11 24H2 26100.9457 x64. **모두 이미 analyze 됨** — 다시 analyze 하지 말고 `^이름$` export 만.
  - ntdll (`ms/decomp/ntdll/_index.tsv`): `TpAllocAlpcCompletionEx` `0xc9580` (29 B, 썽크) · `TppAllocAlpcCompletion` `0xc95a4` (701 B) · `TppAlpcpExecuteCallback` `0x19aa0` (2075 B) · `TppWorkerThread` `0x1a2e0` (3824 B — 크다, ALPC 갈래만) · `TppAlpcpValidateAlpc` `0xc824c` · `TpReleaseAlpcCompletion` `0xc81a0`. 있음: `TppAlpcpFree.asm`.
  - ntoskrnl: `NtAlpcSetInformation` `0xa285b0` (1216 B) · `AlpcpAssociateIoCompletionPort` `0xa28a7c` (616 B) · `AlpcpQueueIoCompletion` `0x32d4c0` · `AlpcpQueueIoCompletionPort` `0x32dd10` · `AlpcpCompleteDispatchMessage` `0x8ae700` (4707 B — 크다, 완료 포트 갈래만) · `AlpcpDispatchNewMessage` `0x8ae2e0` · `AlpcpReceiveMessage` `0x8b45f0` · `AlpcpReceiveMessagePort` `0x8b2ae0`. 있음: `NtAlpcSendWaitReceivePort.asm`.
  - rpcrt4: 있음 `CreateAlpc.asm`·`EnableAsync.asm`·`LrpcIoComplete.asm`·`IoCompleted.asm` (읽기). 형식 문자열은 PE 데이터 읽기 (`pefile`, sast/130 처럼).
  - winnsi: 형식 문자열 rva `0x66e0`·`0x6590`·`0x6360`, 문법 표 `0x6150` (sast/127·130).
- 주의: 같은 시간에 다른 decompiler 는 없음 (sast/131 은 끝난 뒤 시작). flow-writer 가 runs/ 문서를 쓸 수 있음.

## 왜 (요청한 쪽 말 요약)

- 샘플 199 (악성 197) 가 `WinHttpSendRequest` 0 에서 막힘. 에뮬레이터는 지금 epmapper 연결을 `0xc0000034` 로 답해서 이 길에 아예 안 들어감 → 우리 로그로 인자를 못 봄.
- 서버 흉내 (epmapper `ept_map` · nsi 프로시저 5) 를 지금 코드 수준에서 돌게 하려면, sast/130 이 남긴 **비동기 답 배달의 나머지 반쪽** (ALPC 포트 ↔ 완료 포트 이음매와 패킷 꼴) 과 **NDR 본문 바이트** 가 필요함.
- 에뮬레이터 `NtAlpcSetInformation` 은 종류 5 (`AlpcMessageZoneInformation`) 밖이면 멈춤 → 기본 대답 (`nt_alpc_create_port.cpp:602`). 완료 포트 · 일꾼 공장 · 기다림 패킷 몸은 이미 있음 (run93·run125·run127).
- 같은 비동기 틀을 RPCSS (`CoInitializeSecurity` 안 널 읽기 21 샘플) 도 씀.

## 알고 싶은 것

1. **완료 포트 붙이기** — rpcrt4 `CreateAlpc +0x66` → ntdll `TpAllocAlpcCompletionEx` → `TppAllocAlpcCompletion`:
   - 부르는 syscall 차례 (같은 포트·완료 포트에 `NtAlpcSetInformation` 말고 다른 것이 있나 — 예 `NtAlpcQueryInformation`·`NtSetIoCompletion*` 류) 와 각 줄.
   - `NtAlpcSetInformation` 의 정보 종류 번호 · 입력 구조 칸 (오프셋·크기·값: 완료 포트 핸들 = 풀의 어느 칸 · 키 = 무엇 · 그 밖) · 길이.
   - 커널 `NtAlpcSetInformation` 그 종류 갈래 → `AlpcpAssociateIoCompletionPort`: 검사 차례 (길이·핸들 종류·이미 붙어 있음 등) · 각 실패 status · 포트 객체에 남기는 칸 (이름은 PDB 구조체 칸 이름이 있으면 그것).
   - → 표: `차례 · 부르는 것 · 인자/칸 · 실패 status · 줄`.
2. **통지 한 번의 꼴**:
   - (a) 커널: 완료 포트가 붙은 포트에 메시지 (답) 가 들어오면 어디서 (`AlpcpCompleteDispatchMessage` → `AlpcpQueueIoCompletion(Port)` 류) 완료 포트에 패킷을 넣나, 그 패킷의 **키 · ApcContext · `IoStatus.Status` · `Information`** 값 (어느 칸에서 오나). 메시지마다 하나인가, 이미 넣은 패킷이 안 꺼졌으면 건너뛰나 (한 번만 알림 표시 칸).
   - (b) ntdll: 일꾼 (`TppWorkerThread` 의 ALPC 갈래 — `NtWaitForWorkViaWorkerFactory` 출력 패킷을 보고 고르는 줄) → `TppAlpcpExecuteCallback` → 콜백 (`LrpcIoComplete`) 을 **어떤 인자**로 부르나. 콜백 뒤 다시 알림을 켜는 syscall 이 있나.
   - (c) `IoCompleted +0xc8` 의 받기만 하는 `NtAlpcSendWaitReceivePort` 가 **쌓인 메시지 없이** 불렸을 때 (Flags 0 / `0x1000000` 각각) 커널이 돌려주는 정상 status (`AlpcpReceiveMessage` 의 "메시지 없음" 갈래 — 예 `0xc0000708` 류인지 `0x102` 인지) 와 그때 받기 버퍼·속성에 쓰는 것. rpcrt4 가 그 status 를 어떻게 다루나 (`0xc0`·`0xc0000041` 밖이면 `RpcpReportFatalError` — sast/130 2-4). 통지 하나에 받기 부름이 몇 번 오나 (돌림 조건 줄).
3. **NDR 본문 바이트** (형식 문자열 풀기, `[정적]` 은 형식 문자열 바이트 근거로, 해석이 공개 NDR 규칙에 기대면 그렇다고 적음):
   - EPM 프로시저 8 (`ept_map_auth_async` `[추측: 이름 대응]` — 프록시 `0xe11e8` → 프로시저 형식 `0xefeb2`): 요청 스텁 (`+0x68` 부터) 인자 차례 · 정렬 · 크기 (객체 UUID · 질문 탑 · SID · 끝점 핸들 · `max_towers`), 응답 (끝점 핸들 · 탑 개수 · 탑 배열 꼴 · 상태).
   - nsi 프로시저 5 (winnsi `0x66e0` 류): 요청 · 응답 (컨텍스트 핸들 20 B + 반환값 — 정렬 포함).
   - 위 둘을 **NDR 2.0 판과 NDR64 판** 중 어느 쪽으로 적어야 하나 → 질문 4.
4. **bind 에서 고르는 문법** — `NegotiateTransferSyntax` (rva `0x43a20` 류, sast/130 에 export 됨): 클라이언트가 bind 답의 `+0x48` 마스크를 받고 NDR 2.0 / NDR64 중 무엇을 쓰나 (둘 다 켜져 있을 때 순서). 서버 흉내가 **하나만 골라 답할 때** 어느 쪽이 안전한가 (그 문법 판의 형식 문자열로 3 을 적음).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntdll | `TpAllocAlpcCompletionEx` · `TppAllocAlpcCompletion` | 1 | 질문 1 |
| ntoskrnl | `NtAlpcSetInformation` (그 종류 갈래만) · `AlpcpAssociateIoCompletionPort` | 1 | 질문 1 |
| ntoskrnl | `AlpcpCompleteDispatchMessage` (완료 포트 갈래만) · `AlpcpQueueIoCompletion` · `AlpcpQueueIoCompletionPort` | 0~1 | 질문 2(a) |
| ntdll | `TppWorkerThread` (ALPC 갈래만) · `TppAlpcpExecuteCallback` | 0~1 | 질문 2(b) |
| ntoskrnl | `AlpcpReceiveMessage` · (필요하면) `AlpcpReceiveMessagePort` — "메시지 없음" 갈래만 | 0 | 질문 2(c) |
| rpcrt4 · winnsi | 형식 문자열 (PE 데이터) · `NegotiateTransferSyntax` (있음 — 읽기) | — | 질문 3·4 |

- 범위: 이음매·패킷·status·NDR 바이트 꼴까지. 완료 포트·일꾼 공장 본문 (run125·run127 에 있음 — 가리키기만), ALPC 섹션·뷰·보안 속성 본문, NDR 해석기 본문은 이름만.
- 분량이 크면 **1 → 2 → 4 → 3** 차례로 결과서에 먼저 쓰고, 나머지는 "다음 요청으로" 라고 적어도 된다 (요청 원문: 1·2 가 "가장 큰 덩어리").

## 받고 싶은 것

- 결과서 `_workspace/sast/132_alpc-completion_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1) 붙이기 차례·입력 칸·커널 검사 표 · (2) 패킷 칸 표 (`칸 · 값 · 어디서 · 줄`) + 일꾼 → 콜백 인자 표 + 빈 받기 status 표 · (3) NDR 바이트 꼴 표 (`오프셋 · 크기 · 정렬 · 뜻 · 근거`) 프로시저 8 요청/응답 · 프로시저 5 요청/응답 · (4) 문법 고르기 한 줄.
  4. **sast/130 5 절 (서버 흉내 차례) 을 고쳐 다시 적은 것** — 완료 포트 붙이기 · 패킷 넣기 · 빈 받기 대답이 들어간 판.
  5. PDB GUID 확인 줄 (쓴 모듈마다 — 이미 쓰는 프로젝트와 같은지).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall` `src/alpc/nt_alpc_create_port.cpp:581 ~ :602` (`NtAlpcSetInformation` 완료 포트 종류) · `:612 ~ :626` (`NtAlpcSendWaitReceivePort` — 비동기 보내기 = 곧바로 0, 받기만 = 쌓인 답 넘김) · `src/threadpool/nt_io_completion.cpp` (패킷 넣기) · 새 `src/rpc/` 창구 (epmapper `ept_map` · nsi 프로시저 5).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (Q231 ④·⑤ 와 합치게 — 멈춤점 `caller+0x…`) 과 **"다음 요청 후보"**.

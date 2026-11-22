# 122 worker-factory — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q139 (d) + Q134 (b) 를 앞으로 당김 + 덧붙임** (PE_emulator 요청 "일꾼 공장" — 튜닝 `278_tune06.md P2`). 사용자 결정 2026-11-16 "지금 즉시 먼저" (분석 멈춤 중 예외). Q139·Q134 의 다른 몫 ((a)(c)(e) 등) 은 이번에 안 함. **정적만** (요청한 쪽 짐작: VM 필요 없음).
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인). `_index.tsv` (`ms/decomp/ntoskrnl/_index.tsv`):
  - `NtReleaseWorkerFactoryWorker` `0x260220` (1145 B) · `NtWorkerFactoryWorkerReady` `0x475400` (200) · `NtWaitForWorkViaWorkerFactory` `0x32ae60` (2887) · `NtSetInformationWorkerFactory` `0x3a6e10` (2436, `.asm` 있음) · `NtCreateWorkerFactory` `0x9b95a0` (954) · `NtQueryInformationWorkerFactory` `0x4d8a10`
  - 이름으로 보이는 도우미 (본문 안 봄): `ExpWorkerFactoryCheckCreate` `0x260cf0` · `ExpWorkerFactoryCreateThread` `0x3cbb38` · `ExpWorkerFactoryWantsToCreate` `0x452474` · `ExpWorkerFactoryDeferredThreadCreation` `0x4a1d88` · `ExpSetWorkerFactoryDeferredCreateTimer` `0xa6facc` · `ExpWorkerFactoryManagerThread` `0x65a3d0` · `ExpWorkerFactoryCompletionPacketRoutine` `0x2610a0` · `ExpTryEnterWorkerFactoryAwayMode` `0x32e460` · `ExpLeaveWorkerFactoryAwayMode` `0x260be0` · `ExpWorkerFactoryFinishDeferredWork` `0x32bbf0`
- 이미 아는 것:
  - ntdll `TppWorkPost` 첫 넣기 = `NtReleaseWorkerFactoryWorker(풀 +0x38)` 한 번, 기다림 없음 `[정적]` run72. "첫 넣기가 일꾼을 새로 만드는 계기인가" 는 run72 `[추측]`.
  - `NtCreateWorkerFactory`·`NtSetInformationWorkerFactory` 클래스 2·3·5·`0xd`·`0xe` 의 길이·검사 차례 `[정적]` run62·run69·`ms/disassemble/ntoskrnl/NtSetInformationWorkerFactory.asm`.
  - 스레드 만들기 길 (`NtCreateThreadEx` → `PspCreateThread` …) 은 run 문서 여럿에 있음 (PE_emulator 요청 25 ㄱ 답 — flow.md 2.2·2.3 근처). 비교에 재사용.
  - 에뮬레이터 로그 (진짜 관찰 아님): 공장 만들기 뒤 설정 클래스 5 → **4** → 3 차례, 클래스 4 와 `0xb` 가 "지원 안 함" 대답.
- 주의: 같은 시간에 다른 decompiler 가 **mswsock·afd·nsi** (sast/121) 를 돈다 — 그쪽은 건드리지 않는다. 이미 있는 ntoskrnl `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`.

## 알고 싶은 것

1. **`NtReleaseWorkerFactoryWorker`** (+ `NtWorkerFactoryWorkerReady`) 본문 — 공장 객체의 어떤 칸을 바꾸나 (오프셋·뜻: 넣은 일감 수·기다리는 일꾼 수·지금 일꾼 수·최소/최대 스레드 수 등), 그리고 **어떤 조건에서 새 일꾼 스레드를 만드나** (조건 식 한 줄 — `ExpWorkerFactoryCheckCreate`·`WantsToCreate` 를 거치면 그 안 조건까지). 바로 만드나, 미뤄 만드나 (`Deferred…`·관리 스레드 `ExpWorkerFactoryManagerThread` 로 넘기나). 첫 넣기가 계기인가 (run72 `[추측]` 닫기).
2. **일꾼 스레드를 만드는 길** — 만드는 함수 (`ExpWorkerFactoryCreateThread` 류) 와 그 아래 스레드 만들기 호출. 새 스레드의 **시작 주소·인자·스택 크기 (예약/커밋)** 가 어디서 오나: `NtCreateWorkerFactory` 인자 → 공장 칸 (오프셋) → 스레드 첫 문맥. `NtCreateThreadEx` 가 타는 만들기 길과 같은 길인가 (갈리는 곳이 있으면 그 줄). 만들어진 스레드는 누구 프로세스에서 도나 (공장 만든 프로세스 — 인자로 받은 프로세스 핸들 칸).
3. **`NtWaitForWorkViaWorkerFactory`** — 일꾼이 일감을 받는 꼴: 완료 포트에서 꺼내는 함수·꺼낸 것을 돌려주는 출력 칸 (몇 개까지·크기), 일감이 없을 때 기다림 (시간 제한 칸), 마감·줄이기 때 일꾼이 스스로 끝나는 길 (어떤 status·조건).
4. **`NtSetInformationWorkerFactory` 클래스 4** (= Q134 (b) 몫) 의 뜻 — 점프표에서 클래스 4 갈래가 쓰는 칸과 입력 길이·검사. 클래스 `0xb` 도 같은 김에 (싸면). 클래스 이름은 PDB 열거가 없으면 칸 뜻으로 가름 (`[추측]` 표시).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^NtReleaseWorkerFactoryWorker$` · `^NtWorkerFactoryWorkerReady$` | 1 | 질문 1 |
| ntoskrnl | `^ExpWorkerFactoryCheckCreate$` · `^ExpWorkerFactoryWantsToCreate$` · `^ExpWorkerFactoryCreateThread$` · `^ExpWorkerFactoryDeferredThreadCreation$` (필요한 것만) | 0~1 | 질문 1·2 |
| ntoskrnl | `^NtCreateWorkerFactory$` (인자 → 칸, 있으면 읽기만) | 0 | 질문 2 |
| ntoskrnl | `^NtWaitForWorkViaWorkerFactory$` | 1 | 질문 3 |
| ntoskrnl | `NtSetInformationWorkerFactory.asm` (있음 — 읽기만) | 0 | 질문 4 |

- 범위: 공장 쪽만. 완료 포트 내부 (`IoRemoveIoCompletion`·`KeRemoveQueueEx` 류) · 스레드 만들기 본문은 이름만 (이미 본 함수면 run 번호로 가리킴).

## 받고 싶은 것

- 결과서 `_workspace/sast/122_worker-factory_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`/`[추측]`/`[공백]`. **공장 객체 칸 표** (오프셋 · 크기 · 뜻 · 쓰는 함수/줄 · 읽는 함수/줄) 하나로 모아 주세요.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 — 쓰는 곳: `ext/syscall/syscall_layer_cpp/src/threadpool/nt_release_worker_factory_worker.cpp:15` · `src/threadpool/nt_create_io_completion.cpp:145` (공장 상태·"일꾼이 필요함" 알림), `Src/nt/TeamSyscall.cpp` 스레드 만들기 다리 (지금 "일꾼 스레드 안 만듦"). 특히: **일감을 넣었는데 일꾼이 0 이면 커널은 무엇을 하나** (지금 우리는 아무것도 안 해서 모두 잠듦).
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

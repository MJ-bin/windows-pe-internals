# 07 insert-thread — 정적 분석 요청

- 날짜: 2026-10-21
- 스프린트: 02_sprint
- 대기열: **Q4** — 2.1 `nt!PspInsertThread`, `nt!PspCallProcessNotifyRoutines` callee (`_workspace/plan.md`)
- 관련: `runs/02_sprint/flow.md` 그림 1, 2.1·2.2 / 이전 요청 sast/03 (run9 — `PspInsertThread` 를 알려진 사실 대조용으로 처음 뽑음), sast/06 (run11 — 스레드 락)

## 지도에서 본 것 (`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*`, 하네스 묶음 M 반영본)

- `PspInsertThread`: 깊이 1, caller 3곳, expanded. 장부 59줄(조각 `FUN_140b0d692`, `FUN_140b0d71b` 안 7줄 포함).
  - 이미 아는 것 (run9 `[정적]`): `+0x2e5 KeSuspendThread`, `+0x5a6 KeReadyThread`, `+0x807 PspCallProcessNotifyRoutines`. 실제 흐름 순서는 멈춤(+0x2e5) → 알림(+0x807) → 준비(+0x5a6) 로 flow-writer 가 점프를 따라 확인함 (run9).
  - 눈에 띄는 것: `+0x3ea ObInsertObjectEx`, `+0x4fa PspCallThreadNotifyRoutines`, `+0x525 PspCreateObjectHandle`, **`+0x5e2 KeStartThread`**, `+0x7d3 EtwTraceProcess`, `+0x8ef PspAssignProcessToJobList`, `+0x93d PspCreateSecureThread`, 조각 안 `KeForceResumeThread`·`KeRaiseUserException`·`KeRequestTerminationThread` (실패 길로 보임).
- `PspCallProcessNotifyRoutines`: 깊이 2, caller 2곳, expanded. 장부 15줄 중 **`indirect-cfg` 4곳(+0xc3, +0x152, +0x1d4, +0x321, 출처 `cfg:?`)** — 드라이버가 등록한 알림 함수를 부르는 자리로 보인다 `[추측]`. 그 밖에 `ExReferenceCallBackBlock`, `ExGetExtensionTable`, `PsTerminateProcess` (+0x176), `PsReferenceProcessFilePointer`.
- `KeStartThread` 깊이 2, `PspCallThreadNotifyRoutines` 깊이 2 (둘 다 caller 2곳).

## 알고 싶은 것

1. `PspInsertThread` 가 직접 부르는 함수를 **하는 일 단위로** 묶고, 단위마다 대표 1~3개 (이름, ret_off, 역할 한 줄). 그리고 **test.exe 첫 스레드가 지나갈 보통 길의 흐름 순서**(오프셋 순서가 아님, 점프를 따라간 순서)를 한 줄로. 특히 아래가 어디에 끼는지:
   - 스레드 객체 등록(`ObInsertObjectEx` +0x3ea), 스레드 알림(`PspCallThreadNotifyRoutines` +0x4fa), 스레드 핸들(`PspCreateObjectHandle` +0x525), `KeStartThread`(+0x5e2), 프로세스 알림 덩어리(+0x7d3 ~ +0x807), 잡(job) 배정(+0x8ef).
   - run9 에서 본 "멈춤 → 알림 → 준비" 순서와 맞는지 / 다른지.
2. **스레드 락**: `PspAllocateThread` 가 잡고 풀지 않은 스레드 락(run11)을 `PspInsertThread` 가 푸나? 푼다면 어느 자리(ret_off 또는 명령 줄)인가. 못 찾으면 "못 찾음".
3. **`KeStartThread`(+0x5e2) 와 `KeReadyThread`(+0x5a6) 의 차이**: 이름과 호출 위치·조건으로 보이는 만큼 (본문은 뽑지 않는다). 첫 스레드가 "실행될 수 있는 상태"가 되는 건 어느 쪽인지, 멈춘 채(suspend)일 때는 어떻게 되는지 — 코드로 보이는 만큼만, 나머지는 `[추측]` 이나 "동적 확인 거리".
4. `PspCallProcessNotifyRoutines`:
   - 간접 호출 4곳이 각각 **무엇을 부르는 자리**인가 (어느 전역 목록·표에서 함수 주소를 꺼내나 — 데이터 이름이 있으면 그 이름, 예: 알림 등록 목록). `cfg:?` 라도 코드로 출처를 따라가 본다.
   - `PsTerminateProcess`(+0x176) 는 어떤 조건에서 부르나 (알림을 받은 쪽이 프로세스 생성을 거부하면? — 코드로 보이는 만큼).
   - 이 함수가 test.exe 처럼 보통 프로세스를 만들 때 "생성"과 "종료" 중 어느 쪽 길로 가나 (인자 조건).
5. `PspCallThreadNotifyRoutines`(+0x4fa) 는 본문을 뽑지 말고, 지도·이름으로 역할 한 줄만.
6. 표기 규칙(tail, indirect-cfg, alias, frag, 주인 없는 덩어리)은 reference.md 대로. `alias:` 이름은 대표로 고르지 않는다.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^(PspInsertThread\|PspCallProcessNotifyRoutines)$` | 0 | Q4 본체 (`PspInsertThread` 는 묶음 M 반영본으로 이미 뽑혀 있지만 같이 다시 뽑아도 된다) |

- depth 0. 다른 함수가 꼭 필요하면 지도로 길을 찾고, 넓힌 이유를 결과서에 쓴다.
- analyze 다시 하지 않는다. `_calls.tsv` 는 이어서 쌓는다.

## 이미 아는 것

- `[관찰]` (run3) `NtCreateUserProcess+0x101a` → `PspInsertThread`, `PspInsertThread+0x807` → `PspCallProcessNotifyRoutines` (부모 cmd.exe 스레드). 그 시점에 `!process 0 0 test.exe` 로 test.exe 가 보였다.
- `[정적]` (run9) `+0x2e5 KeSuspendThread`(ThreadFlags bit0 가 켜졌을 때), `+0x5a6 KeReadyThread`(멈춤 여부와 상관없이 끝 쪽), 프로세스 알림 덩어리는 첫 스레드일 때만 들어가는 모양 `[추측]`.
- `[정적]` (run11) `PspAllocateThread` 는 스레드 락을 잡고 풀지 않는다. 부모에게 줄 프로세스 핸들은 `NtCreateUserProcess +0x10a7 PspCreateObjectHandle`.

## 받고 싶은 것

- 질문 1~6 답 + 근거 (`.asm` 의 `+0x` 줄, `.c` 요약). 대표 지점은 **ret_off**, call_off 는 괄호.
- 동적으로 확인할 것 + 추천 멈춤점 (있으면).
- 결과서: `_workspace/sast/07_insert-thread_result.md`

# 91 timer2-set — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q176a** (= Q176 ②, run93 남은 정적). 사용자 결정 2026-11-15: **Q176 ② · Q178 ① 먼저** (Q104 VM 단계 앞 멈춤 자리에서 끼움). Q176 ① ③ ④ ⑤ 는 Q176b 로 남김. **정적만** — VM 몫은 결과서 "동적으로 확인할 것" 으로만 적는다.
- 빌드: 26100.9457. ntoskrnl 은 analyze 됨 (PDB 붙음). `_index.tsv`: `NtSetTimer2` rva `0x3ac3d0` (26 B) · `ExpSetTimer2` `0x3ac400` (474 B) · `KeSetTimer2` `0x3ac870` (2030 B) · `NtCreateTimer2` `0xa34f80` (458 B) · `KeInitializeTimer2` `0x3afae0` (19 B) · `KiExpireTimer2` `0x3aee60` (1957 B).
- 관련: **sast/89 결과서** `_workspace/sast/89_wait-packet_result.md` (82~84줄 타이머2 만기 표, 151줄, "다음 요청 후보" 2) · run 문서 `runs/03_sprint/run93-wait-packet.md`. run78 (Q141c2, 타이머 만기 `KiTimerExpirationDpc`·`KiCheckForTimerExpiration`) 도 참고.

## 배경 (PE_emulator 쪽)

- `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]` (원문 `../PE_emulator/_workspace/05_tuning_planner/256_tune04.md` T8 줄, `256_internals-wait-packet.md` 3줄 — 읽어도 됨): 받은 샘플 중 `NtSetTimer2` 820 (악성 224). 병목 중 "`NtSetTimer2` 뒤 끝" 7. Go 런타임과 스레드 풀 타이머가 `NtCreateTimer2` → `NtSetTimer2` → (wait packet 맡김) → `NtRemoveIoCompletionEx` 로 기다린다.
- 에뮬레이터가 모르는 것: 타이머를 **이미 지난 시각 (또는 0)** 으로 걸면 그 자리에서 울리나, 다음 시계 틱까지 기다리나. 그리고 `NtCreateTimer2` 가 만든 타이머가 동기 (형 `0x19`) 인지 알림 (형 `0x18`) 인지 정하는 인자.

## 알고 싶은 것

1. **이미 지난 시각이면 그 자리에서 만기 처리하나**: `NtSetTimer2` → `ExpSetTimer2` → `KeSetTimer2` 에서
   - 시각을 절대 / 상대로 나누는 줄, 상대 (음수) 를 지금 시각에 더하는 줄 (어느 시계 — 인터럽트 시간 / 시스템 시간 — 를 읽나).
   - 계산된 만기 시각이 **지금 이하** 이면 어디로 가나: `KiExpireTimer2` (또는 `KiTimer2Expiration`·신호 함수) 를 **바로 부르나** (오프셋), 아니면 타이머 줄에 넣고 다음 틱 (`KiTimer2Expiration`) 에 맡기나. 예/아니오 + 줄.
   - 0 (`DueTime = 0`) 을 따로 다루나.
   - 바로 만기라면: 신호 상태 (`SignalState`) 와 맡긴 wait packet (`WaitType` 2, sast/89) 배달이 `NtSetTimer2` 를 부른 스레드 안에서 일어나나 — 부르는 줄만.
2. **주기 (Period) 와 다시 걸기**: `NtSetTimer2` 인자 (매개변수 구조체 꼴 — 칸 오프셋·뜻) 중 주기 칸, 이미 걸린 타이머를 다시 걸 때 앞 것을 빼는 줄. status 표 (성공 · 실패 조건).
3. **`NtCreateTimer2` 의 동기/알림 인자**: 어느 인자 (번호·비트) 가 형 `0x18` (알림) / `0x19` (동기) 를 고르나 (`KeInitializeTimer2` 로 넘기는 값). 그리고 보탬:
   - ntdll `TppInitializeTimerSubQueue +0x37 R9D=8` · `+0x44 0x100002` 가 `NtCreateTimer2` 의 어느 인자 · 어떤 뜻인가 (스레드 풀 타이머 = 동기인가 알림인가).
   - 다른 플래그 비트 (높은 해상도 등) 가 있으면 이름·비트만.
4. **지금 우리 (PE_emulator) 맞나·틀리나**: 질문 1·3 답으로 "지난 시각이면 바로 울림 / 다음 틱" 과 "Go·스레드 풀 타이머의 형" 을 한 줄씩.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^NtSetTimer2$` | 1 | 질문 1·2 입구 (26 B — 바로 `ExpSetTimer2`) |
| ntoskrnl | `^ExpSetTimer2$` | 1 | 질문 1·2 — 인자 꼴, 객체 참조, `KeSetTimer2` 로 넘기는 값 |
| ntoskrnl | `^KeSetTimer2$` | 1 | 질문 1 핵심 — 시각 계산, 지금 이하 갈림 |
| ntoskrnl | `^NtCreateTimer2$` | 1 | 질문 3 |
| ntoskrnl | `^KeInitializeTimer2$` | 0 | 질문 3 — 형 바이트 쓰는 줄 |
| ntoskrnl | `^KiExpireTimer2$` | 0 | sast/89 에서 읽음 — `KeSetTimer2` 가 부르면 입구 인자만 |
| ntdll | `^TppInitializeTimerSubQueue$` | 0 | 질문 3 보탬 — `+0x37`·`+0x44` 앞뒤 인자 줄만 (sast/89 에서 뽑혔으면 그대로) |

- 범위: 걸기 (set) 와 만들기 (create) 의 인자·갈림만. 만기 뒤 배달 (`KiExpireTimer2` 본문, `KiInsertQueueInternal`) 은 sast/89 에서 읽음 — 다시 안 읽는다.
- Q176 ① (`NtCreateWaitCompletionPacket`), ③ (`NtRemoveIoCompletionEx`), ④ (`ObGetAssociatedWaitObject`), ⑤ (닫기 처리기) 는 **이번에 안 한다** (Q176b).
- `NtCancelTimer2`, 타이머 해상도 (`ExSetTimerResolution`), DPC 타이머 내부 (`KiInsertTimer2` 계열 줄 구조) 는 이름만. 단 질문 1 의 "지금 이하" 갈림이 그 안에 있으면 그 갈림 줄까지만 따라간다.
- 공용 부품 (락, `ObReferenceObjectByHandle`, `ProbeForRead` 류) 은 이름만.

## 지도에서 본 것

- ntoskrnl `_map/` 에는 `KeClockInterruptNotify`·`KeSetEvent` 등 다른 진입 지도만 있고 `NtSetTimer2` 지도는 없음. 필요하면 `run_ghidra.sh map` 으로 `NtSetTimer2`·`NtCreateTimer2` 진입 지도를 먼저 만든다 (깊이·caller·직접 호출).
- sast/89 결과서 82줄: `NtSetTimer2 +0x9/+0xe → ExpSetTimer2 +0x17d/+0x182 → KeSetTimer2` (call/ret 자리).

## 이미 아는 것

- `[정적]` sast/89: 타이머2 만기 `KiTimer2Expiration +0xd7/+0xdc → KiExpireTimer2`. 동기 (형 `0x19`) 는 `KiExpireTimer2 +0x3a8/+0x3ad KiInsertQueueInternal` 뒤 `SignalState` 를 1 내림, 알림 (형 `0x18`) 은 `+0x702/+0x707` 뒤 신호 그대로.
- `[정적]` sast/89: wait packet 을 안 울린 대상에 맡기면 `KWAIT_BLOCK` (`WaitType` 2) 으로 대상 `WaitListHead` 에 달림, 대상이 울리면 신호 함수가 패킷을 포트 줄에 넣음.
- `[추측]` (sast/89 151줄) Go 런타임 타이머는 동기 (자동 리셋). 코드로 볼 것.

## 받고 싶은 것

- 결과서 `_workspace/sast/91_timer2-set_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: 차례 표 (함수+오프셋 · 하는 일 · 갈림 조건) + "지난 시각 / 0 / 미래" 세 경우의 길 표.
- 질문 2: `NtSetTimer2` 인자 칸 표, status 표.
- 질문 3: `NtCreateTimer2` 인자 → 형 표, `TppInitializeTimerSubQueue` 두 값 뜻.
- 끝에 **"동적으로 확인할 것"** (추천 멈춤점) 과 **"다음 요청 후보"**.

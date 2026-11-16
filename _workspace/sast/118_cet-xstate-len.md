# 118 cet-xstate-len — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint
- 대기열: **Q105** (CET 스레드의 맡긴 문맥 길이, 작은 정적 — 원래 "필요할 때만"). 사용자 결정 2026-11-15 "남은 정적 분석 전부" 라 지금 한다. **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인). `_index.tsv`: `RtlpGetEntireXStateAreaLength2` `0x4eea6c` (77 B).
- 관련: sast/49 결과서 (`_workspace/sast/49_context-length2_result.md` — `RtlGetExtendedContextLength2(ecx, rdx, r8)` = `RtlGetExtendedContextLength3(…, r9 = 0xFFFFF780000003D8)`; CET 길에서 `Length3` 길이 = `0x32f + 이 함수 값`), run55 (아직 확인 필요 — test.exe 는 비-CET 로 보여 `0x4ff`, run44 와 어긋나지 않음), sast/106 결과서 (CET_U 칸 SSP — 시동 문맥 길, 이 VM 은 `0x10001b`).
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/116) 과 **다른 DLL** (sast/117, objdump 위주) 을 돌린다 — ntdll 쪽은 건드리지 않는다. 이미 있는 ntoskrnl `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`.

## 알고 싶은 것

1. `RtlpGetEntireXStateAreaLength2` (77 B) 가 돌려주는 값의 식 — 읽는 KUSD 칸 (`XState` `+0x3d8` 안 칸 이름 — PDB `_XSTATE_CONFIGURATION`: `AllFeatureSize`·`EnabledFeatures`·`EnabledSupervisorFeatures`·`AlignedFeatures` 등) 과 계산 (정렬·더하기).
2. 그 값으로 CET 스레드의 맡긴 문맥 길이 (`0x32f + 값`) 를 이 VM 의 KUSD `XState` 값 (run44 사진 `ms/logs/04_baggage-read_supp_*` 에 KUSD `+0x3d8` 줄이 있으면) 으로 숫자로 계산. 비-CET 길 `0x4ff` 와 나란히.
3. 이 함수를 부르는 자리 (caller 전수) 와 CET 갈래 조건 한 줄.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^RtlpGetEntireXStateAreaLength2$` | 0 | 질문 1 |
| ntoskrnl | caller (`_calls.tsv`) — 있으면 읽기만 | 0 | 질문 3 |

## 받고 싶은 것

- 결과서 `_workspace/sast/118_cet-xstate-len_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`, 계산한 숫자 (입력 값 근거와 함께).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (비-CET 만 쓰면 해당 없음).
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

# 107 tls-lock-small — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q74** (2.7 TLS 락 소유 칸 표 남은 것, 작음). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 빌드: 26100.9457. ntdll analyze 됨. `_index.tsv`: `RtlpAcquireSRWLockSharedContended` `0x15f0c` (591) · `RtlAbPreAcquire` `0x748a0` (70) · `LdrpAcquireSchedulerSharedDataSlot` `0xf0ea4` (178) · `LdrpAllocateSchedulerSharedData` `0xf96b0` (217).
- 관련: run41 (`runs/02_sprint/run41-tls-lock-detail.md`, sast/36) — TLS 락 (SRW) 과 TEB `+0x1850` 소유 칸 표.
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/106) 과 **sample dll** (sast/105) 을 돌린다 — **ntoskrnl Ghidra 프로젝트는 열지 않는다.** 질문 4 는 `ms/sym` 의 PDB 를 파일로 읽거나 (`pdb_struct.py` 류) ntdll PDB 로. 이미 있는 ntdll `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`. ntdll 전체 줄 찾기는 스크래치의 `ntdll.dis` (sast/100·102·103 이 만든 objdump 풀이) 를 재사용.

## 알고 싶은 것

1. **(a) 소유 칸 bit 0 을 켜는 곳**: run41 의 TLS 락 소유 칸 (TEB `+0x1850` 이 가리키는 칸 — run41 기준 이름·오프셋 확인) 에 bit 0 을 넣는 줄 (ntdll 전수).
2. **(b) `RtlpAcquireSRWLockSharedContended +0x126` 의 `RtlAbPreAcquire`** 가 왜 효과 없는 호출인가 — `RtlAbPreAcquire` 본문 (70B) 이 무엇을 보고 바로 돌아오나 (전역 칸·조건 — 그 칸이 test.exe 에서 0 인가).
3. **(c) `LdrpAcquireSchedulerSharedDataSlot` 진입 조건 둘**: `LdrpSchedulerSharedDataListHead` 비어 있지 않음, PEB `NtGlobalFlag2 & 0x10` 꺼짐 — 뜻. `LdrpAllocateSchedulerSharedData` (`LdrpInitializeProcess +0x182c`) 본문과 같이: 목록에 무엇을 넣나 (syscall 이 있나 — 이름), 안 맞으면 TEB `+0x1850` 이 0 으로 남아 칸 적기를 건너뜀 (run41) — test.exe 에서 어느 쪽인가 (run44 사진 값이 있으면 그걸로).
4. **(d) `THREADINFOCLASS` `0x38` · `0x39` 공식 이름** — PDB 에 enum 이 없다고 했음 (run41). ntdll·ntoskrnl PDB 의 다른 타입 (`_THREADINFOCLASS` 이름 변형, 공개 심볼 문자열) 으로 한 번 더 찾고, 없으면 "PDB 에 없음" 과 커널 `NtSetInformationThread` 점프표 칸 (`_index.tsv`·기존 `.asm` 읽기만) 으로 그 클래스가 하는 일 한 줄.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^RtlAbPreAcquire$` | 0 | 질문 2 |
| ntdll | `^RtlpAcquireSRWLockSharedContended$` | 0 | 질문 2 — `+0x126` 앞뒤 |
| ntdll | `^LdrpAcquireSchedulerSharedDataSlot$`, `^LdrpAllocateSchedulerSharedData$` | 0 | 질문 3 |
| ntdll | 소유 칸 bit 0 쓰기 전수 (objdump) | — | 질문 1 |
| ntdll·ntoskrnl PDB | `THREADINFOCLASS` | — | 질문 4 |

- 범위: 위 질문만. 공용 부품은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/107_tls-lock-small_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

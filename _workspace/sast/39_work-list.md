# 39 work-list — 정적 분석 요청 (일 목록)

- 날짜: 2026-10-26
- 스프린트: 03_sprint (짐 지도 단계 4) / 예정 run: run45
- 대기열 항목: **Q81 일 목록** (사용자 결정 2026-10-26: Q78 앞에)
- 관련: `runs/03_sprint/flow.md` (그림 1·2, 2.1~2.10, 4장·4.1), `runs/03_sprint/baggage.md` (2장·3장·7장), `runs/03_sprint/run44-baggage-snap.md` (지점 8개·구간), `_workspace/sast/38_baggage-checklist_result.md` (체크리스트 범위), `_workspace/plan.md` 대기열

## 배경 (대화에서 정한 것)

- 지도가 답하려는 질문은 둘이다 (사용자, 2026-10-26):
  1. **짐**: 여러 구조체가 언제 초기화되고 값이 정해지나 → `baggage.md` (Q76·Q77 끝, Q78 남음)
  2. **일**: 프로세스 초기화 동안 nt·ntdll 이 무슨 일을 해 줘야 하나 → 더 넓다 (PE 로드, DLL 로드, 그 밖의 여러 작업)
- 흐름도 (`flow.md`) 는 "단계마다 대표 지점만" 이라 일의 **순서·대표 함수**는 있지만, 일을 빠짐없이 센 목록은 없다. Q78 은 짐(칸)에서 거꾸로 줄기를 따라가므로, **sast/38 체크리스트에 없는 짐을 남기는 일**은 Q78 로는 안 보인다 `[추측]`.
- 이번 바퀴는 **목록 만들기와 분류**만 한다. 일마다 줄기를 끝까지 파지 않는다 (그건 Q78 이나 뒤 항목). "구현할지" 로 거르지 않는다 (CLAUDE.md).
- 범위: 프로세스 생성 (부모 cmd 의 `KERNELBASE!CreateProcessInternalW` → `NtCreateUserProcess`) → test.exe 첫 스레드의 `NtContinue` 은퇴까지. 진입점 뒤 CRT 는 범위 밖. 부모 유저 쪽 (KERNELBASE 의 csrss 알림 등) 과 csrss 쪽은 **이름·한 줄 설명까지만** 목록에 넣는다.

## 알고 싶은 것

1. **일 목록**: 위 범위에서 nt·ntdll 이 하는 일을 빠짐없이 표로. 일 하나 = 한 덩어리 작업 (예: "exe 이미지 섹션 만들기", "주소 공간 만들기", "ntdll·시스템 DLL 매핑", "PEB 만들기", "매개변수 복사·고치기", "시스템 DLL 시작 정보 블록", "스레드 객체·ID·TEB·유저 스택", "출발 문맥 만들기·바꿔 끼우기", "프로세스·스레드 넣기 (핸들 표·목록)", "알림 콜백", "부모의 csrss 알림", "스레드 풀기", 로더 쪽 "힙 만들기", "ntdll 항목", "실행 옵션", "kernel32·kernelbase 올리기·초기화", "import DLL 찾기·올리기 (경로 검색·섹션·매핑)", "재배치", "IAT 연결 (snap)", "의존 그래프", "일꾼 스레드", "TLS 준비·콜백", "DllMain 부르기", "매개변수 블록 힙으로 옮기기 (run44)", "NtContinue" …). 위 예시는 출발점일 뿐 — 근거 문서·callee 표에서 빠진 일을 찾아 더한다.
   - 열: 번호 · 일 · 공간 (커널/유저/둘 다/다른 프로세스) · 단계 (flow.md 절) · run44 지점 구간 (알면, 예: `P1→P1a`) · 대표 함수 사슬 (지도 기준) · 근거 (run·결과서·`_calls.tsv` 줄) · 태그 (`[관찰]`/`[정적]`/`[추측]`)
2. **분류**: 일마다 하나 (또는 둘):
   - (가) **짐 지도 칸으로 덮임** — 결과가 sast/38 체크리스트의 구조체 칸·ntdll 전역에 남는다. 어느 구조체·칸인지 적는다 (Q78 이 볼 것).
   - (나) **체크리스트 밖 짐을 남김** — 결과가 체크리스트에 없는 곳에 남는다. **어디에** (예: test.exe·dll1·dll2·kernel32 이미지 페이지 안 IAT·재배치 값, dll1·dll2·kernel32·kernelbase 의 섹션·파일 객체·VAD 노드, 힙 안의 다른 블록, KERNELBASE·KERNEL32 전역, 다른 커널 전역 (예: `PspCidTable` 항목, 프로세스 목록 머리), csrss 쪽 기록 …) 와 **이미 있는 덤프 (dast/03 8개, `ms/dump/03_baggage-snap/<P>/`) 로 읽을 수 있을지** `[추측]` 을 적는다.
   - (다) **흐름도에만 있음** — 짐을 남기지 않거나 잠깐 쓰고 지움 (검사, 락, 조회, 알림만 등).
3. **기존 대기 항목과 잇기**: 일마다 그 일의 실행 확인·남은 정적이 걸린 대기 항목 번호 (Q19~Q75, Q78~Q80, plan.md 대기열). 어느 항목에도 안 걸리는 일은 "새 할 일" 로 따로 모은다.
4. **빠짐 점검**: sast/38 의 callee 표 (`ms/disassemble/{ntoskrnl,ntdll}/_baggage/callees.tsv`, 요약 `callees_summary.tsv` — 15 함수 639줄, `doc_none` = 문서 어디에도 없음 57) 의 줄이 1번 목록의 어느 일에 들어가는지. 어느 일에도 안 들어가는 callee 는 이름과 짐작 역할을 적는다 (새 일인지, 공용 부품인지).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| (문서) | flow.md 그림 1·2, 2.1~2.10, 3장, 4장·4.1 | — | 일 후보 원본 |
| (문서) | run9 ~ run44 run 문서, sast/01~38 결과서 | — | 근거 |
| ntoskrnl·ntdll | sast/38 `callees.tsv`·`callees_summary.tsv` 의 15 함수 | 1 (이미 뽑음) | 빠짐 점검 |
| ntdll·ntoskrnl·kernelbase | 분류에 필요할 때만 `_calls.tsv`·`.asm` 을 본다 (예: 어떤 callee 가 이미지 페이지에 쓰는지) | 0~1 | (나)/(다) 가르기 |

- 새로 Ghidra 를 돌려야 하는 경우는 분류가 정말 안 될 때만. 돌렸으면 무엇을 왜 돌렸는지 적는다.

## 이미 아는 것

- `[관찰]` run44: 8 지점 (K1 `PspAllocateThread` · K2 `PspInsertProcess` · K3 `PspCallProcessNotifyRoutines` · P0 `PspUserThreadStartup` · P1 `LdrInitializeThunk` · P1a `LdrpInitializeProcess+0x199d` · P1b `+0x1bb6` · P2 `NtContinue`). HandleCount 4·4·4·4·4·26·44·46, VAD 8·9·9·11·11·21·32·32, 모듈 —·…·2·6·6. `ProcessParameters` 가 P1→P1a 사이에 힙으로 옮겨짐, 일꾼 2개는 P1a~P1b 사이.
- `[정적]` sast/38: 체크리스트 = 구조체 21종 (커널 14: `_EPROCESS` `_KPROCESS` `_ETHREAD` `_KTHREAD` `_KTRAP_FRAME` `_HANDLE_TABLE` `_MMVAD` `_MMVAD_SHORT` `_TOKEN` `_SECTION` `_CONTROL_AREA` `_SEGMENT` `_SECTION_IMAGE_INFORMATION` `_FILE_OBJECT` / 유저 7: `_PEB` `_RTL_USER_PROCESS_PARAMETERS` `_TEB` `_PEB_LDR_DATA` `_LDR_DATA_TABLE_ENTRY` `_LDR_DDAG_NODE` `_KUSER_SHARED_DATA`) 직접 칸 1,456 + ntdll 전역 `.data`·`.mrdata`·`.00cfg` 493. **exe 의 섹션 사슬·파일 객체만** 읽었고 다른 이미지 것은 없음.
- `[추측]` (오케스트레이터가 사용자에게 말한 것, 확인 대상) 체크리스트 밖 짐 후보: 이미지 페이지 안 IAT·재배치 값, 다른 DLL 의 섹션·파일 객체, KERNELBASE·KERNEL32 자기 전역, csrss 쪽 기록.

## 받고 싶은 것

- 결과서 `_workspace/sast/39_work-list_result.md`:
  - 질문 1·2·3 을 합친 **일 목록 표** (번호를 붙여 다음 바퀴에서 "일 12" 처럼 부를 수 있게)
  - (나) 로 분류된 일만 모은 표: 어디에 남나 · 덤프로 읽힐지 · 제안 (Q78 범위를 넓힐지 / 새 항목 / 덤프 보충 읽기 Q79 에 붙일지) — **정하는 건 사용자**, 결과서는 제안만
  - 질문 4 빠짐 점검 결과 (callee 줄 → 일 번호, 남은 것)
  - "새 할 일" 목록 (어느 대기 항목에도 안 걸리는 일)
- 동적으로 확인할 것이 생기면 따로 적는다 (대기열 맨 뒤로 갈 것).
- 문서 반영 (flow-writer, run45): 일 목록 표는 run45 문서에, (나) 목록은 `baggage.md` 7장 "빈 곳" 에 요약 — 템플릿 바깥 새 장을 만들지는 않는다 (하네스 변경이 필요해지면 제안만).

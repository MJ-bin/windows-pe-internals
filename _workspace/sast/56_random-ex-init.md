# 56 random-ex-init — 정적 분석 요청

- 날짜: 2026-11-03
- 스프린트: 03_sprint (짐 지도 바퀴)
- 대기열: **Q116** (PE_emulator 요청 9, 사용자 결정 2026-11-03: 맨 앞, 작고 급함 — PE_emulator 52 바퀴가 멈춰 기다림)
- 관련: run46 (sast/40 process-heap — 힙 구간 시스템 콜 #1~#8, `NtQueryInformationProcess` 1번 = #3 `RtlpGetModifiedProcessCookie +0x27`), run59 (sast/54 — 쿠키 사슬 `InitSecurityCookie`·`LdrInitSecurityCookie`·`LdrpGenRandom`·`RtlEncodePointer +0x12f`, `LdrInitializePerThreadRng`), baggage 3.8 `.data` (`RtlpRandomExInit` 은 7장 `≤P1a` 88 목록 안 — 사진상 P1a 에 이미 0 아님?)
- 원문: `../PE_emulator/_workspace/01_coder/52_heap-manager_codex.md` 맨 앞 "멈춤 보고" (읽기만)

## 무슨 일이 있었나

- PE_emulator 가 진짜 ntdll `RtlInitializeHeapManager` (`LdrpInitializeProcess +0x9e5 ~ +0x9f4`) 를 처음 돌렸더니 `NtQueryInformationProcess` 가 **2번** 나왔다 (run46 은 1번).
  - 첫째: `NtQueryInformationProcess(-1, 0x24, …, 4)`, 돌아올 자리 `[RSP]` = ntdll + `0x95f45` = `RtlpInitRandomExVector +0x29`
  - 둘째: `RtlpGetModifiedProcessCookie +0x22` (call) / `+0x27` (ret) — run46 #3 과 같음
- 사슬 (오케스트레이터가 `.asm`·지도로 확인): `RtlInitializeHeapManager` → `RtlHpGlobalsInitialize +0x4c` (그리고 `+0x5c`) `CALL RtlRandom` (같은 주소 PDB 이름 `RtlRandomEx`, rva `0x95db0`) → `RtlRandom +0xa MOV RAX,[RtlpRandomExInit]` (rva `0x1ce878`) → … `+0x34 CMPXCHG.LOCK [RtlpRandomExInit],RDI` → `+0x4e CALL RtlpInitRandomExVector` (rva `0x95f1c`, 218B, `.asm` 없음)
- PE_emulator 쪽은 `RtlpRandomExInit` 이 파일 값 0 그대로라 여기서 처음 초기화가 일어났다. 진짜 Windows 에선 힙 전에 누가 켜 둔 것 `[추측]` — **아니면 run46 이 이 호출을 빠뜨린 것**. 어느 쪽인지 가르는 게 이번 바퀴 목적

## 알고 싶은 것

1. **`RtlpRandomExInit` (rva `0x1ce878`) 을 쓰는 곳 전부** — 함수 + 오프셋 + 쓰는 값. `RtlRandom` 안 `+0x34 CMPXCHG`·`+0x45`·`+0x56`·`+0x152` (주소 넘김) 이 무엇인가 (한 번만 하기 = run-once 꼴인가: 0 → "하는 중" → "끝" 값). ntdll 전체에서 이 주소를 쓰거나 주소를 넘기는 곳 (`refs`)
2. **`RtlRandom`(= `RtlRandomEx`) 을 부르는 곳 전부** 중 `LdrpInitializeProcess +0x9e5` 보다 **시간상 앞**에 도는 것이 있나 — 예/아니오 + 이름. 지도 caller 3 (`RtlHpGlobalsInitialize +0x4c/+0x5c`, `RtlpCreateIFEOKeyFilterKey +0x7e`) 밖에도 간접·꼬리 호출이 있는지. run59 쿠키 사슬 (`InitSecurityCookie`·`LdrInitSecurityCookie`·`LdrpGenRandom`·`LdrInitializePerThreadRng`·`RtlEncodePointer`) 과 `LdrpInitializeProcess` 앞 묶음 (`+0x0 ~ +0x9e5`) 에서 `RtlRandom`·`RtlpInitRandomExVector`·`RtlpRandomExInit` 에 닿는 길이 있나
3. **`RtlpInitRandomExVector` 본문** (`.asm` export) — 무엇을 쓰나 (전역 이름·크기, 예: RandomEx 벡터), syscall (`+0x24` 쯤 `NtQueryInformationProcess(-1, 0x24, &x, 4, 0)` 인가, 다른 syscall 도 있나), 끝에 `RtlpRandomExInit` 을 켜나 (아니면 `RtlRandom` 이 켜나)
4. **`RtlRandom` 의 `RtlpRandomExInit` 갈래** — `+0xa ~ +0x56` 의 조건: 값이 무엇이면 `RtlpInitRandomExVector` 를 부르고 무엇이면 건너뛰나, 다른 스레드가 "하는 중" 일 때 기다리는 길
5. **run46 #1 ~ #8 다시 보기** — run46 문서·sast/40 결과서 표가 `RtlHpGlobalsInitialize` 의 `RtlRandom` 호출을 빠뜨렸나. 아래 넷으로 가른다:
   - (a) 정적: `RtlInitializeHeapManager` 에서 `RtlHpGlobalsInitialize` 까지 길이 test.exe 조건에서 늘 타는가 (갈래 조건)
   - (b) 사진: run44 덤프 로그 (`ms/logs/03_baggage-snap*.txt`, `04_baggage-read_*.txt`) 에 `RtlpRandomExInit` (ntdll+`0x1ce878`) 값이 P0·P1·P1a 에 있나 — baggage 7장은 `≤P1a` 로 셈. P1 (힙 전) 에 이미 0 아니면 "앞에서 켬", P1 에 0 이고 P1a 에 0 아니면 "힙 구간에서 켬 = run46 빠뜨림"
   - (c) 결론: run46 정정이 필요한가 (몇 번째 시스템 콜로 넣어야 하나), 구간 시스템 콜 수 12 → 13 인가
   - (d) PE_emulator 에 줄 답: "힙 전에 켜 둬야 함 (누가, 어디)" 또는 "힙 안에서 켜는 게 맞음 — 약속 횟수를 2로"

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^RtlRandom$` (PDB 별칭 `RtlRandomEx`) | 1 | 1·4 |
| ntdll | `^RtlpInitRandomExVector$` | 1 | 3 |
| ntdll | `^RtlHpGlobalsInitialize$` | 0 | 2·5(a) (`.asm` 있음) |
| ntdll | `^RtlInitializeHeapManager$` | 0 | 5(a) (`.asm` 있음) |
| ntdll | (refs) `RtlpRandomExInit` `0x1ce878`, `RtlRandom` `0x95db0`, `RtlpInitRandomExVector` `0x95f1c` | — | 1·2 |

- `RtlRandomEx` 는 `_index.tsv` 에 없고 `pdb_aliases.py --where ntdll.dll RtlRandomEx` → rva `0x95db0` (`_index.tsv` 이름 `RtlRandom`). 문서에는 PDB 이름 둘을 같이 적는다.
- `RtlpRandomExInit` 은 Ghidra 함수 범위 밖 데이터 (`--where`: `[none]`).

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `RtlInitializeHeapManager` 깊이 5 caller 1 · `RtlHpGlobalsInitialize` 6/1 · `RtlRandom` 7/3 · `RtlpInitRandomExVector` 8/1
- `RtlRandom` 의 caller (지도): `RtlHpGlobalsInitialize +0x4c/+0x51`, `+0x5c/+0x61`, `RtlpCreateIFEOKeyFilterKey +0x7e/+0x83`
- `RtlRandom` callee: `+0x4e/+0x53 RtlpInitRandomExVector`

## 이미 아는 것

- `[정적]` run46: 힙 구간 시스템 콜 12 (`NtQueryInformationProcess` 1 = #3)
- `[관찰]` PE_emulator (진짜 ntdll 코드를 Unicorn 에서 실행): 첫 `NtQueryInformationProcess` 돌아올 자리 `0x180095f45`, 인자 `(-1, 0x24, 스택 주소, 4, 0)` — PE_emulator 쪽 실행이라 26100.9457 VM 관찰은 아님
- `[정적]` run59: `RtlEncodePointer` 캐시 전역 `RtlpGetCookieValue` 뒤에만 `NtQueryInformationProcess 0x24` (`+0x12f` 첫 호출)

## 이번에 안 하는 것

- 힙 관리자 나머지 본문 (run46 몫)
- `RtlRandom` 의 난수 계산 자체 (벡터 섞기) — 쓰는 전역 이름·크기만

## 받고 싶은 것

- 질문별 답 (예/아니오 또는 이름) + 근거 `+0x` 줄 (call/ret 둘 다, 점프 목적지는 "목적지 − 함수 시작")
- 5(d) PE_emulator 에 줄 한 줄 결론
- run46 정정이 필요하면 정정할 줄 (표 번호·숫자)
- 동적으로 확인할 것 (있으면 — Q104 VM 단계에 얹을 수 있게)

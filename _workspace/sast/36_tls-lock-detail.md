# 36 tls-lock-detail — 정적 분석 요청

- 날짜: 2026-10-24
- 스프린트: 02_sprint
- 대기열: Q56 (단계 3, 정적 묶음 — 대표 지점 목표엔 덜 중요한 "작은 정적")
- 관련: flow.md 2.7 (`LdrpCallTlsInitializers` 락 잡기·풀기), run28 "아직 확인 필요" 정적 Q56 / 이전 요청 sast/23 (tls-init-call, run28), sast/18 (load-dependent, run23 — 같은 TEB `+0x1850` 표 패턴)

## 알고 싶은 것

작은 질문 넷. 각각 **이름 하나 또는 예/아니오** 로 답할 수 있게.

1. **TEB `+0x1850` 이 가리키는 8칸 표의 정체.**
   - PDB `_TEB` 의 `+0x1850` 칸 이름·타입 (sast/23 은 `SchedulerSharedDataSlot` 이라 적었음 — 맞나, 같은 오프셋에 다른 이름(union)이 있나).
   - 이 칸에 표 주소를 **넣는** 곳 (함수 + 오프셋). `refs` 또는 `_TEB +0x1850` 쓰기 검색으로.
   - "SRW 락 소유 추적 표" `[추측]` (sast/18·23) 이 맞나: 예/아니오 + 근거 줄.
2. **추적 칸 값의 맨 위 비트(바이트 `+7` 의 bit 7) 를 켜는 곳.**
   - `LdrpCallTlsInitializers +0x1ab ~ +0x1af` 가 읽고, 켜져 있으면 `+0x1e8 NtSetInformationThread(-2, 클래스 0x38, {칸 번호, 0}, 0x10)` 을 부름 (sast/23). 이 함수 안에는 켜는 줄이 없음.
   - 후보: `RtlpAcquireSRWLockSharedContended` (`+0x113` 에서 부름) 안. 맞나 (예/아니오 + 오프셋), 언제 켜나 (예: 기다려야 했을 때).
   - `THREADINFOCLASS` 0x38 의 PDB 이름 (enum 에서).
3. **`ImageTlsCallbackCaller` 의 `RtlAcquireSRWLockExclusive` 직접 호출 갈래가 왜 있나.**
   - `+0x7 LEA R8,[RtlAcquireSRWLockExclusive]` / `+0xe CMP RAX,R8` / `+0x11 JZ +0x26` → `+0x29 CALL RtlAcquireSRWLockExclusive` (sast/23).
   - 컴파일러가 간접 호출을 "자주 오는 대상이면 직접 부르기" 로 바꾼 모양 (PGO 간접 호출 승격) `[추측]` 인가 — 같은 모양(`LEA 함수 / CMP / JZ / 직접 CALL`)이 ntdll 의 다른 CFG 간접 호출 자리에도 있나 (두세 곳 예), 아니면 이 함수에만 있나.
   - test.exe·dll1·dll2 의 TLS 콜백(`ExeTls`·`Dll1Tls`·`Dll2Tls`) 은 이 갈래를 안 탄다 — 예/아니오 (`[소스]` + 콜백 주소 비교로).
4. **`LdrShutdownProcess +0x168` 앞의 `[RDI+0x58] ≠ 0` 조건.**
   - `RDI` 가 무엇인가 (모듈 항목 `_LDR_DATA_TABLE_ENTRY`? 그러면 `+0x58` = `BaseDllName`), 이 조건이 뜻하는 것 한 줄.
   - 프로세스 끝 길이라 sample 시동(진입점까지) 범위 밖 — 이름·뜻만.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^RtlpAcquireSRWLockSharedContended$` | 1 | 질문 2 (591바이트, 이미 `.asm` 있음 — 본문을 이번에 읽음) |
| ntdll | `^LdrpCallTlsInitializers$` | 0 | 질문 1·2 (이미 있음, `+0x26~+0x63`·`+0xbd~+0x1f0` 다시 보기) |
| ntdll | `^ImageTlsCallbackCaller$` | 0 | 질문 3 (55바이트, 이미 있음) |
| ntdll | `^LdrShutdownProcess$` | 0 | 질문 4 (930바이트, 이미 있음 — `+0x100~+0x168` 근처만) |

- 질문 1 의 "표 주소를 넣는 곳" 이 위 함수 밖이면 그 함수 이름·오프셋만 찾고 본문은 안 읽어도 된다.
- PDB 타입(`_TEB`, `THREADINFOCLASS`) 은 `pdb_aliases.py` 나 PDB 타입 덤프 중 decompiler 가 쓰는 방법으로.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `RtlpAcquireSRWLockSharedContended` rva `0x15f0c`, 591바이트, 깊이 5, caller 9곳 (expanded).
- `LdrpCallTlsInitializers` rva `0x10e50`, 963바이트, 깊이 5, caller 6곳.
- `RtlpAcquireSRWLockExclusiveContended` rva `0xa2d80`, 821바이트, caller 35곳 — 지도에서 "공용 도구" 로 안 펼침. 질문 2 답에 같은 비트 켜기가 있으면 언급만.
- `ImageTlsCallbackCaller` rva `0xe83e0`, 55바이트. `LdrShutdownProcess` rva `0xadda0`, 930바이트 (`_index.tsv`).

## 이미 아는 것

- `[정적]` (sast/23, run28) `LdrpCallTlsInitializers`: `+0x6d CMPXCHG.LOCK [LdrpTlsLock],0x11` 로 공유 잡기, 실패하면 `+0x113 RtlpAcquireSRWLockSharedContended`. 앞뒤로 TEB `+0x1850` 이 가리키는 8칸 표에 락 주소를 적고(`+0x26 ~ +0x63`) 지움(`+0xbd ~ +0x1f0`). 풀기 쪽 `+0x1e8 NtSetInformationThread(-2, 0x38, {칸 번호,0}, 0x10)` 는 그 칸 맨 위 비트가 켜졌을 때만.
- `[정적]` (sast/18, run23) `LdrpLoadDependentModuleInternal +0xe47~+0xe91` 에도 같은 "8칸 표에 락 주소 적기" 덩어리.
- `[정적]` (sast/23) `ImageTlsCallbackCaller`: 콜백 ≠ `RtlAcquireSRWLockExclusive` 면 `+0x16` CFG 간접 호출, 같으면 `+0x29` 직접 호출.
- `[정적]` (sast/23) `LdrShutdownProcess +0x168`/`+0x16d` 에서 TLS 호출 (`ECX=0`, `RDX=RBX`), 추가 조건 `[RDI+0x58] ≠ 0` (뜻 모름).
- `[소스]` sample TLS 콜백은 `ExeTls`·`Dll1Tls`·`Dll2Tls` — 로그만 찍음.

## 범위 밖

- SRW 락 알고리즘 전체 (기다림 목록, 깨우기) — 비트 하나 켜는 줄과 그 조건만.
- `RtlpAcquireSRWLockExclusiveContended` 본문 (caller 35곳 공용 도구).
- 커널 쪽 `NtSetInformationThread` 클래스 0x38 처리 — 이름만.
- 프로세스 끝(`LdrShutdownProcess`) 의 나머지 흐름.

## 받고 싶은 것

- 질문별 답 + 근거 (asm `+0x` 줄, 점프 목적지는 "주소 − 함수 시작")
- `_TEB +0x1850` 표 칸 하나의 모양 (락 주소 + 맨 위 비트 뜻) 짧은 표
- 동적으로 확인할 멈춤점 추천 (있으면 — 이번 항목은 작아서 없어도 됨)
- 새로 생긴 정적 공백 (있으면)

# 04 baggage-read — 동적 분석 요청 (오프라인 덤프 읽기)

- 날짜: 2026-10-25
- 스프린트: 03_sprint / 예정 run: **run44** (Q77 전체를 이 바퀴에서 닫음 — dast/01~04)
- 대기열 항목: **Q77c 오프라인 읽기** (Q77a 시험 끝 · Q77b 캡처 끝)
- 관련: `_workspace/sast/38_baggage-checklist_result.md` ("Q77(사진)에서 떠야 할 것 — 추천" 표, 156~197줄), `ms/logs/03_baggage-snap.txt`·`_result.md` (라이브 값·지점 정의), 칸 목록 `ms/disassemble/{ntoskrnl,ntdll}/_baggage/struct__*.tsv`·`globals.tsv`·`globals_rdata.tsv` (커밋 안 함, 서버에 있음)
- 재시도: 없음

## 대화에서 정한 것

- 라이브는 끝났다. **타깃 VM 은 꺼도 된다** (사용자 질문에 답함). 디버거 VM `Windows 11 x64` 의 WinDbg 로 덤프만 연다. 공유 폴더 `dump` (읽기, = 서버 `ms/dump`) · `logs` (쓰기, = 서버 `ms/logs`) 를 그대로 쓴다. 로그는 `.logopen "\\vmware-host\Shared Folders\logs\..."` 로 서버에 바로 써진다.
- 실행 중 안내는 오케스트레이터가 runbook 대로 직접. 서버 쪽은 오케스트레이터가 실행.
- 스냅샷 (Snapshot7~14) 과 덤프는 이 바퀴가 끝날 때까지 지우지 않는다.

## 이번에 할 것

덤프 8개 (`ms/dump/03_baggage-snap/<P>/memory.dmp`, `<P>` = `K1 K2 K3 P0 P1 P1a P1b P2`) 각각에서, sast/38 표의 대상을 **같은 명령 묶음**으로 읽어 지점별 로그로 남긴다. 그 로그를 나중에 서버에서 지점끼리 비교해 "칸이 언제 처음 값을 갖나" 를 지점 사이로 좁힌다 (비교·짐 지도 반영은 flow-writer, run44).

### 사용자가 덜 치게 — 명령 묶음을 파일 하나로

- dast-mentor 가 **WinDbg 명령 파일**을 만든다. 사용자는 덤프를 열고 **한 줄** (`$$>a< "<명령 파일>" <P>`) 만 친다. 인자 `<P>` 로 로그 파일 이름·`.echo` 머리를 정한다 (dast/03 에서 사용자가 `<K1>` 처럼 꺾쇠를 그대로 친 일이 있었다 — 그래서 인자로).
- 명령 파일은 디버거 VM 이 읽을 수 있어야 하므로 공유 폴더 안에 둔다. 제안: `ms/logs/04_baggage-read_cmds.txt` (= `\\vmware-host\Shared Folders\logs\04_baggage-read_cmds.txt`). 이 파일은 커밋 대상 (로그 폴더지만 명령 파일이라 이름으로 구분). 다른 자리가 낫다면 dast-mentor 가 정하고 이유를 적는다.
- 지점별 로그: `ms/logs/04_baggage-read_<P>.txt` (8개). dast-mentor 가 빈 파일 8개를 만든다.
- 명령 파일이 처음엔 틀릴 수 있다 → runbook 은 **K1 하나로 먼저 돌려 보고** (오케스트레이터가 서버에서 로그를 읽어 확인) 나머지 7개로 간다.

### 덤프마다 공통 앞부분

- `.process /p /r ffff82859908b080` (test.exe, K1·K2 는 프로세스 목록에 없으므로 이름 말고 주소로).
- K2 는 `!process ffff82859908b080 0` 으로 먼저 존재 확인 (dast/03 판정의 빈틈).
- ntdll 심볼: ① 이전 (K1~P1) 은 `PEB.Ldr` 가 null 이라 `.reload /user` 로 안 잡힌다. `.reload /f ntdll.dll=7ffafb660000,<크기>` 처럼 베이스를 주고 올리는 법을 시도 `[추측]` (크기는 dast/03 `lm m ntdll` 의 end−start, 또는 dast/02 의 `0x267000`). 안 되면 숫자 주소로 읽는다.
- 덤프의 CPU 문맥 (`r`) 은 디버거 루프라 안 쓴다 (dast/02).

### 읽을 대상 (sast/38 표 기준, 주소는 이번 실행의 라이브 값 — 덤프마다 같음)

라이브 값 (dast/03 로그·판정):

| 이름 | 값 |
|---|---|
| test.exe EPROCESS | `ffff82859908b080` |
| 첫 ETHREAD | `ffff828598445080` (K1 엔 아직 없음, K2 부터) |
| 첫 스레드 InitialStack / 트랩 프레임 | `ffffb58b9ad075f0` / `ffffb58b9ad07460` (= InitialStack−0x190) |
| PEB / TEB | `9b2fa66000` / `9b2fa67000` |
| ntdll 베이스 | `7ffafb660000` (`PebLdr` = +`0x1d2880` = `7ffafb832880`) |
| `_CONTEXT` (맡긴 문맥) | `9b2fcffa40` (① ② 에서 `rcx`) |
| cmd EPROCESS / cmd 스레드 / cmd InitialStack | `ffff828598add080` / `ffff828598adc080` / `ffffb58b991175f0` |
| dll1 / dll2 / test 베이스 | `7ffae12e0000` / `7ffadb950000` / `7ff61a370000` (①-b 부터 목록에) |
| 힙 / ProcessParameters | 힙 `25996f30000` (② 에서), 매개변수 ① `25996cb0000` / ② `25996f37a50` |

**커널** (주소는 EPROCESS·KTHREAD 칸을 따라간다 — 칸 오프셋은 sast/38 표):
1. `_EPROCESS` (안의 `_KPROCESS` 따로도) — `ffff82859908b080`
2. 첫 `_ETHREAD` (안의 `_KTHREAD` 따로도) — `ffff828598445080` (K2~)
3. `_KTRAP_FRAME` — `InitialStack−0x190` (`KTHREAD.TrapFrame` 은 ⓪·① 에서 null 이라 믿지 않음, dast/03)
4. `_HANDLE_TABLE` (`EPROCESS.ObjectTable` +0x300) + 핸들 목록 (`!handle 0 <flags> ffff82859908b080` 꼴, 핸들 수 4~46)
5. `_TOKEN` (`EPROCESS.Token` +0x248, 아래 4비트 지움) — `dt` + `!token`
6. VAD 나무 (`EPROCESS.VadRoot` +0x558) — `!vad` 목록 + 노드 `_MMVAD_SHORT`/`_MMVAD` (모두 `dt` 할지, 목록만 둘지는 dast-mentor 가 출력 양 보고 정함 — 최소 목록은 반드시)
7. exe 섹션 사슬 — `EPROCESS.SectionObject` +0x2a8 → `_SECTION` → `_CONTROL_AREA` → `_SEGMENT` → `_MI_SECTION_IMAGE_INFORMATION` (앞 0x40 = `_SECTION_IMAGE_INFORMATION`)
8. `_FILE_OBJECT` (`EPROCESS.ImageFilePointer` +0x330)
9. 전역 `nt!PsNtdllExports` (`_PS_NTDLL_EXPORTS` 0xa8) — 한 번이면 되지만 매 덤프 넣어도 됨

**유저** (test.exe 문맥):
10. `_PEB` (`9b2fa66000`), `_TEB` (`9b2fa67000`)
11. `_RTL_USER_PROCESS_PARAMETERS` (`PEB.ProcessParameters` 를 **그 덤프에서** 따라감 — ① 과 ② 주소가 다름) + 환경 블록 크기·앞부분
12. `_PEB_LDR_DATA` (`PEB.Ldr`, null 이면 건너뜀) + 모듈 목록 (`InLoadOrderModuleList` 따라 `_LDR_DATA_TABLE_ENTRY` 마다, 항목의 `DdagNode` → `_LDR_DDAG_NODE`)
13. `_KUSER_SHARED_DATA` (`0x7ffe0000`) — 시각 칸은 계속 변하니 비교 때 소음 (sast/38). 덤프마다 넣되 비교에서 거른다
14. 맡긴 `_CONTEXT` (`9b2fcffa40`) — ① 이후
15. API set 지도 머리 (`PEB.ApiSetMap` +0x68) — 머리글만
16. PEB·TEB 가 가리키는 그 밖의 영역 (sast/38 표: `LeapSecondData`·`SharedData`·`TlsBitmap`·`ActivationContextData`·`WaitOnAddressHashTable`·`ThreadLocalStoragePointer`·스택 범위) — 포인터 값과 영역 첫 줄 정도
17. **ntdll 쓰기 가능 구역 raw**: `.data` (rva `0x1cc000`, 크기 `0x8f58`), `.mrdata` (rva `0x1e7000`, `0x35b8`), `.00cfg` (rva `0x1e6000`, `0x28`) — `dq` 로 통째 (비교용)

### 출력 모양 (서버에서 지점끼리 비교하려고)

- 구조체는 `dt <type> <addr>` 기본 꼴 (`+0x오프셋 이름 : 값` 줄) — 칸 목록 TSV 의 오프셋·이름과 맞출 수 있게. 펼친 구조체는 따로 `dt` 를 한 번 더 (위 `_KPROCESS`·`_KTHREAD` 처럼).
- 같은 구조체를 `dq <addr> L<크기/8>` 로 **raw 도** 남긴다 (이름 없는 바이트까지 비교).
- 구획마다 `.echo ==== <P> <대상>` 머리를 붙인다 (서버 스크립트가 자를 수 있게).
- 읽기 실패 (`Memory access error`, `????`) 는 그대로 남긴다 — 그 자체가 결과 (예: K1 에 아직 없는 TEB).

## 성공 기준

- 로그 8개 모두에 공통 앞부분과 위 1~17 구획 머리가 있고, K3~P2 에서 1·10 이 라이브 값 (dast/03) 과 맞는다 (EPROCESS 칸 `UniqueProcessId 0x1dd0`, PEB `ImageBaseAddress 7ff61a370000`, ① 의 `Ldr` null / ② 의 `Ldr 7ffafb832880` 등).
- K2 에서 test.exe EPROCESS 가 주소로 읽힌다 (목록 빈칸 이유 확인).
- 읽기 실패 구획은 어느 지점·어느 대상인지 판정에 적는다.

## 끝 지점

- 덤프 8개를 다 읽고 로그 8개가 서버에 있으면 끝. 그다음 판정 → flow-writer (run44, 짐 지도 바퀴: baggage.md 2장 "사진" 칸, 3장 쓴 자리 후보를 지점 구간으로) → explainer → committer.

## 범위 밖

- 지점끼리 비교 스크립트·짐 지도 반영 (flow-writer, run44).
- 쓴 자리를 정적으로 찾기 (Q78).
- 스냅샷·덤프·백업 지우기 (사용자 결정, 이 바퀴 뒤).
- 하네스 고치기 (사용자 검토 뒤 따로).

# 2026-11-03 run61 — RandomEx 난수 벡터를 처음 켜는 곳 (Q116, PE_emulator 요청 9): `RtlpRandomExInit` 은 **파일 값 0**, 이 프로세스에서 처음 켜는 곳은 힙 구간 맨 앞 `RtlInitializeHeapManager +0xd → RtlHpGlobalsInitialize +0x4c → RtlRandom` 첫 호출이다. 그 안 `RtlpInitRandomExVector +0x24` 의 `NtQueryInformationProcess(0x24)` 를 run46 이 빠뜨렸다 → 힙 구간 시스템 콜 12 → **13** (정적)

## PE_emulator 쪽에서 쓸 것

**결론: 힙 안에서 켜는 게 맞다.** `RtlpRandomExInit` (ntdll rva `0x1ce878`) 은 파일 값 0 그대로 두고, 진짜 `RtlRandom` 이 `RtlHpGlobalsInitialize +0x4c` 에서 처음 켜게 둔다. 그러면 힙 구간의 `NtQueryInformationProcess` 는 **2번**이 맞다 — 첫째 = `RtlpInitRandomExVector +0x29` (ret 자리, 클래스 `0x24`), 둘째 = `RtlpGetModifiedProcessCookie +0x27` (run46 의 옛 #3). 미리 값 2 로 켜 두면 syscall 이 한 번 빠지고 `RtlpRandomExConstantVector` (`0x200`B)·`RtlpRandomExAuxVarY` 가 진짜와 달리 0 으로 남는다. 에뮬레이터가 맞춰 줄 입력은 셋이다: (1) `LdrSystemDllInitBlock +0x98` (커널이 쓰는 난수 4바이트, `RtlpInitRandomExVector +0x3d` 가 읽음) 이 이미 채워져 있어야 한다 `[정적]`, (2) 이 syscall 이 돌려줄 4바이트는 앞서 `RtlEncodePointer +0x48` 이 받은 것과 **같은 프로세스 쿠키** 가 자연스럽다 `[추측]` (같은 값인지는 동적), (3) KUSD `+0x290` 이 0 이 아니면 `RtlRandom +0x103` 에서 `RDRAND` 가 돈다 (최대 10번) — Unicorn 이 `RDRAND` 를 어떻게 다루는지에 따라 갈린다 `[정적]`.

오프셋 읽는 법 (run60 과 같음):

- `call +0x..` = CALL 명령 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 값 (예: `RtlRandom` 시작 `0x180095db0`, `+0x27 JZ 0x180095e4a` → `+0x9a`).

### 1. 힙 구간 `NtQueryInformationProcess` 약속 `[정적]`

| 차례 | ret 자리 (← 사슬, call/ret) | 인자 | 횟수 | 조건 |
|---|---|---|---|---|
| 1 | `RtlpInitRandomExVector +0x29` (call `+0x24`) ← `RtlRandom` call `+0x4e` / ret `+0x53` ← `RtlHpGlobalsInitialize` call `+0x4c` / ret `+0x51` ← `RtlInitializeHeapManager` call `+0xd` / ret `+0x12` ← `LdrpInitializeProcess` call `+0x9ed` / ret `+0x9f2` | 핸들 `-1` (`+0x1c OR RCX,-1`), 클래스 `0x24` (`+0x20 LEA EDX,[R9+0x20]`), 출력 = 지역 4바이트 `[RSP+0x58]` (`+0xb`), 길이 4 (`+0x16`), 반환 길이 `NULL` (`+0x10` → `[RSP+0x20]`) | 1 | `RtlpRandomExInit & 3 == 0` (첫 `RtlRandom`). 실패면 `+0x2b JS → +0xd3` → `RtlRaiseStatus` call `+0xd5` / ret `+0xda` |
| 2 | `RtlpGetModifiedProcessCookie +0x27` (call `+0x22`) ← `RtlInitializeHeapManager` call `+0x9d` / ret `+0xa2` | run46 옛 #3 그대로 (핸들 `-1`, 클래스 `0x24`, 4바이트) | 1 | 항상 |

- 프로세스 전체로는 `0x24` 가 **3번**: `LdrpInitializeProcess +0x12f` (ret `+0x134`) `RtlEncodePointer` 안 call `+0x43` / ret `+0x48` (run59, 이 프로세스 첫 호출) → 위 1 → 위 2.
- 둘째 `RtlRandom` (`RtlHpGlobalsInitialize` call `+0x5c` / ret `+0x61`) 는 상태가 이미 2 라 syscall 이 없다.

### 2. `RtlpRandomExInit` 값 — 한 번만 하기 (run-once) `[정적]`

값의 **아래 두 비트**가 상태다. `RtlRandom +0xa` 가 읽어 가른다.

| `& 3` | 뜻 | `RtlRandom` 안 갈래 | 하는 일 |
|---|---|---|---|
| 0 | 아직 안 함 (파일 값) | `+0x24 CMP DL,2` 아님 → `+0x2f JNZ` 안 탐 → `+0x34 CMPXCHG.LOCK [RtlpRandomExInit],RDI` (RDI = 1) | 이긴 쪽만: `+0x4e` `RtlpInitRandomExVector` → `+0x67` `RtlRunOnceComplete(&RtlpRandomExInit, 0, 0)` → 값 **2**. 진 쪽 (값이 바뀜) 은 `+0x40 JNZ → +0x29` 로 다시 읽음 |
| 1 | 다른 스레드가 하는 중 | `+0x2f JNZ → +0x77`, `+0x7a JZ → +0x152` | `RtlpRunOnceWaitForInit` call `+0x15c` / ret `+0x161` (안에서 `+0x4a CMPXCHG.LOCK` 로 "기다리는 줄 \| 1" 을 끼우고 `NtWaitForAlertByThreadId` call `+0x73` / ret `+0x78`) → `+0x161 JMP → +0x29` |
| 2 | 끝 | `+0x27 JZ → +0x9a` | 곧장 난수 계산. 둘째 호출부터 전부 |
| 3 | 비동기 (여기선 잘못) | `+0x80 CMP RCX,3` → `+0x86 MOV EAX,0xc00000f0` | `RtlReportCriticalFailure` call `+0x95` / ret `+0x9a`. 정상에선 안 옴 |

- 끝 값 2 를 쓰는 줄 = `RtlRunOnceComplete +0x67 XCHG [R11],R8` (R11 = 넘겨받은 `&RtlpRandomExInit`, flags 0 → `2 | Context 0`).
- `RtlpInitRandomExVector` 가 0 을 돌려주면 `+0x5f JZ → +0x136` 에서 `RtlRunOnceComplete(…, 4, 0)` (call `+0x13b` / ret `+0x140`) 로 값을 0 으로 되돌린다. 하지만 이 함수는 실패 때 `RtlRaiseStatus` 로 가지 0 을 돌려주지 않아서 안 탄다.
- test.exe 첫 스레드 혼자 도는 동안엔 상태 1 을 볼 일이 없다 (기다림 길 안 탐).

### 3. `RtlpInitRandomExVector` (rva `0x95f1c`, 218바이트) `[정적]`

- 인자 셋 (RunOnce, Parameter, Context) 은 **안 쓴다** (`+0x1c` 에서 RCX 를 덮음). 반환 1 = 성공 (`+0xaa LEA EAX,[R10+1]`, 루프가 끝나 R10 = 0).
- syscall 은 위 1 표의 하나뿐.
- 씨앗 = 받은 4바이트 XOR `LdrSystemDllInitBlock +0x98` (`+0x3d`).
- 루프 `+0x5a ~ +0x92` (카운트 `0x80`, `+0x4e`; `+0x8e SUB R10,1` / `+0x92 JNZ → +0x5a`): `x = (x × 0x7fffffed + 0x7fffffc3) mod 0x7fffffff`, 돌 때마다 `RtlpRandomExConstantVector[i]` 에 dword 로 씀 (`+0x87`) → 128칸 = `0x200`B (rva `0x1d39c0 ~ 0x1d3bc0`).
- 한 번 더 돌린 값 → `RtlpRandomExAuxVarY` (rva `0x1d3bc0`, dword, `+0xc5`).
- `RtlpRandomExInit` 은 **안 건드린다** (켜는 건 부른 쪽 `RtlRandom` 이 `RtlRunOnceComplete` 로).

### 4. 에뮬레이터 상태 기대 값 (힙 구간 뒤 = run44 P1a 사진)

| 칸 | 힙 전 | 힙 구간 뒤 | 쓰는 줄 |
|---|---|---|---|
| `RtlpRandomExInit` (`0x1ce878`, 8B) | 0 (파일 값) | **2** (`[관찰]` P1a) | `RtlRandom +0x34` (0 → 1) · `RtlRunOnceComplete +0x67` (1 → 2) |
| `RtlpRandomExConstantVector` (`0x1d39c0`, `0x200`B) | 0 | 128칸 모두 채워짐 (`[관찰]` P1a 첫 칸 `619336c3`, `7e5153be` …) | `RtlpInitRandomExVector +0x87` (128번) → 그 뒤 `RtlRandom +0xe6` 이 부를 때마다 한 칸 맞바꿈 |
| `RtlpRandomExAuxVarY` (`0x1d3bc0`, 4B) | 0 | 0 아님 (`[관찰]` P1a `0xc899afee`) | `RtlpInitRandomExVector +0xc5` → 그 뒤 `RtlRandom +0x11f ADD.LOCK` 이 부를 때마다 더함 |

- 값 자체는 씨앗 (쿠키·커널 난수·틱) 에 달려 실행마다 다르다. 맞춰 볼 것은 "0 이 아님 / 2" 다.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 값 대조는 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P0.txt` · `_P1.txt` · `_P1a.txt` · `_P1b.txt` · `_P2.txt`) 를 다시 읽은 것. 날짜는 결과서 날짜.
- 03_sprint 열아홉째 바퀴. plan **Q116** — 사용자 결정 2026-11-03 (PE_emulator 요청 9, 맨 앞, 작고 급함 — PE_emulator 52 바퀴가 멈춰 기다림). **짐 지도 바퀴** — `baggage.md` 에 새 쓴 자리 (아래 "짐 지도 반영").
- 정적: ntdll (`ntdll.pdb`) `C3093720177DA6851519DCFFE8DF53FE1` — flow.md 기준과 같다 (결과서, run44 덤프의 ntdll PDB 와도 같음).
- 버전 대조 (결과서, `_calls.tsv`): `LdrInitializeThunk +0x9/+0xe → LdrpInitialize`, `LdrpInitializeInternal +0x55/+0x5a → _LdrpInitialize`, `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess`, `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` — 모두 일치. PE_emulator 가 본 돌아올 자리 ntdll `+0x95f45` = `RtlpInitRandomExVector +0x29` 도 정적과 같다.

## 목적

- PE_emulator 가 진짜 ntdll `RtlInitializeHeapManager` 를 처음 돌렸더니 `NtQueryInformationProcess` 가 run46 의 1번이 아니라 **2번** 나왔다. 첫째는 `RtlpInitRandomExVector +0x29` 로 돌아왔다.
- 가를 것: 진짜 Windows 에선 힙 전에 누가 `RtlpRandomExInit` 을 켜 두나, 아니면 run46 이 이 호출을 빠뜨렸나 (요청서 1 ~ 5).
- 짐 지도: baggage 7.3 `≤P1a` 목록의 `RtlpRandomExInit`·`RtlpRandomExConstantVector*`·`RtlpRandomExAuxVarY*` 세 전역.
- 범위 밖 (요청서): 힙 관리자 나머지 본문 (run46 몫), `RtlRandom` 난수 계산 자체 (쓰는 전역 이름·크기만).

## 방법

- 정적: `_workspace/sast/56_random-ex-init.md` — ntdll `^RtlRandom$` (PDB 별칭 `RtlRandomEx`) depth 1, `^RtlpInitRandomExVector$` depth 1, `^RtlHpGlobalsInitialize$`·`^RtlInitializeHeapManager$` depth 0, refs 셋.
  - 이미 analyze 된 프로젝트 — **다시 analyze 안 함**. 새로 뽑은 것 (모두 depth 0): `RtlpInitRandomExVector`·`RtlRunOnceComplete`·`RtlpRunOnceWaitForInit`·`RtlHeapTrkInitialize`, IFEO 길 확인용 `LdrQueryImageFileExecutionOptionsEx`·`LdrQueryImageFileExecutionOptions`·`LdrpQueryAndUpdateVerifierLaunchCounter`.
  - 이미 있던 것은 읽기만: `RtlRandom`·`RtlHpGlobalsInitialize`·`RtlInitializeHeapManager`·`RtlpOpenImageFileOptionsKeyEx`·`RtlpCreateIFEOKeyFilterKey`·`LdrpInitializeExecutionOptions`·`LdrpInitializeProcess`·`RtlpGetModifiedProcessCookie`.
  - `_refs` 새로 7개: `RtlpRandomExInit`·`RtlRandom`·`RtlpInitRandomExVector`·`RtlpRandomExConstantVector`·`RtlpRandomExAuxVarY`·`RtlpOpenImageFileOptionsKeyEx`·`RtlHeapTrkInitialize`.
  - 앞쪽에서 닿는 길 찾기는 지도 `_map/LdrInitializeThunk_edges.tsv` 로 (결과서, 1080개 함수).
- 결과서: `_workspace/sast/56_random-ex-init_result.md` (**분석성공**, PDB 일치, 1 ~ 5(a)~(d) 모두 답).
- 이 문서를 쓰며 다시 맞춘 것:
  - 오케스트레이터 (`.asm`): `RtlInitializeHeapManager +0xd/+0x12`, `RtlRandom +0x27 JZ → +0x9a`·`+0x34 CMPXCHG.LOCK`, `RtlpInitRandomExVector +0x24/+0x29 NtQueryInformationProcess`·`+0x3d XOR [LdrSystemDllInitBlock+0x98]`, `RtlRunOnceComplete +0x67 XCHG`. 사진 `04_baggage-read_P1a.txt` 4097줄 `7ffa'fb82e878` = 2.
  - flow-writer (`.asm`): `RtlRandom` 의 CALL 줄 전부 (`+0x4e`·`+0x67`·`+0x95`·`+0x13b`·`+0x15c`) 와 점프 목적지 (`+0x27 → +0x9a`, `+0x2f → +0x77`, `+0x40 → +0x29`, `+0x5f → +0x136`, `+0x7a → +0x152`, `+0x161 → +0x29`), `RtlpInitRandomExVector +0x0 ~ +0xda` 전부, `RtlHpGlobalsInitialize +0x0 ~ +0x63`, `RtlpRunOnceWaitForInit +0x4a`·`+0x73/+0x78`, `RtlpGetModifiedProcessCookie +0x22/+0x27`, `RtlEncodePointer +0x43/+0x48`, `LdrpInitializeProcess +0x12f`·`+0x531`·`+0x9e5`·`+0x9ed`·`+0x9f4 JS → +0xa7b`, IFEO 길 (`LdrpInitializeExecutionOptions +0xbd/+0xc5`, `RtlpOpenImageFileOptionsKeyEx +0x184`·`+0x29f`·`+0x2a2 JZ → +0x265`·`+0x2a4 JMP → +0x175`, `RtlpCreateIFEOKeyFilterKey +0x7e/+0x83`), `RtlHeapTrkInitialize +0x1a0/+0x1a5`, `_refs/RtlpRandomExInit.tsv` 5줄. 사진 로그 다섯 장의 해당 줄 (아래 표). 결과서와 같다.
- 문서: 이 run 문서, `run46-process-heap.md` (정정), `run46-process-heap-해설.md` (맨 위 한 줄만), `flow.md` (기준, 2.4 힙 절·run51 절, 그림 2 `p2h` 상자 글, 4장, 4.1, 5장), `baggage.md` (아래).

## 결과

비유 (주사위 상자):

- 힙 관리자가 처음 주사위를 굴리려고 상자 (`RtlRandom`) 를 열었더니, 상자에 "아직 주사위 안 넣음" (값 0) 이라고 붙어 있다.
- 그래서 첫 번째로 연 사람이 본사에 "우리 가게 번호 (쿠키)" 를 한 번 묻고 (syscall), 그걸 씨앗으로 주사위 128개를 채운 뒤, 상자에 "다 넣음" (값 2) 을 붙인다.
- 그다음부터는 누구든 상자를 열면 바로 굴린다. run46 은 "이 상자는 본사에 안 묻는다" 고 적었는데, **첫 번째 열 때만** 묻는다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/*.asm` `CALL … ret=`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x9ed` | `+0x9f2` | `ntdll!RtlInitializeHeapManager` (인자 `+0x9e5 LEA RCX,[RSP+0x98]`, 실패 `+0x9f4 JS → +0xa7b`) | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0xd` | `+0x12` | `ntdll!RtlHpGlobalsInitialize` (함수 맨 처음 call, 조건 없음) | [정적] |
| `ntdll!RtlHpGlobalsInitialize` | `+0x4c` · `+0x5c` | `+0x51` · `+0x61` | `ntdll!RtlRandom` — `_calls.tsv` 에 `alias:RtlRandom+1`, 같은 주소 PDB 이름 `RtlRandomEx` (rva `0x95db0`) | [정적] |
| `ntdll!RtlRandom` | `+0x4e` | `+0x53` | `ntdll!RtlpInitRandomExVector` (상태 0 일 때만) | [정적] |
| `ntdll!RtlRandom` | `+0x67` | `+0x6c` | `ntdll!RtlRunOnceComplete(&RtlpRandomExInit, 0, 0)` | [정적] |
| `ntdll!RtlRandom` | `+0x13b` | `+0x140` | `ntdll!RtlRunOnceComplete(…, 4, 0)` — 되돌리기, 안 탐 | [정적] |
| `ntdll!RtlRandom` | `+0x15c` | `+0x161` | `ntdll!RtlpRunOnceWaitForInit` — 상태 1 일 때만, 안 탐 | [정적] |
| `ntdll!RtlRandom` | `+0x95` | `+0x9a` | `ntdll!RtlReportCriticalFailure` — 상태 3, 안 탐 | [정적] |
| `ntdll!RtlpInitRandomExVector` | `+0x24` | `+0x29` | `ntdll!NtQueryInformationProcess` (클래스 `0x24`) | [정적] |
| `ntdll!RtlpInitRandomExVector` | `+0xd5` | `+0xda` | `ntdll!RtlRaiseStatus` — 실패 때만 | [정적] |
| `ntdll!RtlpRunOnceWaitForInit` | `+0x73` | `+0x78` | `ntdll!NtWaitForAlertByThreadId` | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x9d` | `+0xa2` | `ntdll!RtlpGetModifiedProcessCookie` (run46) | [정적] |
| `ntdll!RtlpGetModifiedProcessCookie` | `+0x22` | `+0x27` | `ntdll!NtQueryInformationProcess` (클래스 `0x24`, run46 옛 #3) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x531` | `+0x536` | `ntdll!LdrpInitializeExecutionOptions` (힙 앞) | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0xc5` | `+0xca` | `ntdll!RtlpOpenImageFileOptionsKeyEx` — 바로 앞 `+0xbd XOR R8D,R8D` (만들기 표시 0). test.exe 는 이 call 자체를 건너뜀 (run46) | [정적] |
| `ntdll!RtlpOpenImageFileOptionsKeyEx` | `+0x184` | `+0x189` | `ntdll!RtlpCreateIFEOKeyFilterKey` — 만들기 표시 ≠ 0 일 때만 (`+0x29f TEST R14B,R14B` / `+0x2a2 JZ → +0x265`) | [정적] |
| `ntdll!RtlpCreateIFEOKeyFilterKey` | `+0x7e` | `+0x83` | `ntdll!RtlRandom` — 위 조건 때문에 초기화 길에선 안 탐 | [정적] |
| `ntdll!RtlHeapTrkInitialize` | `+0x1a0` | `+0x1a5` | `ntdll!RtlRandom` — 이 함수는 내보내기만 있고 ntdll 안에서 부르는 곳 0 | [정적] |

### 질문별 답 (결과서 요약)

1. **`RtlpRandomExInit` 을 쓰거나 주소를 넘기는 곳 = `RtlRandom` 안 5곳뿐** (`_refs/RtlpRandomExInit.tsv`: `+0xa` 읽기 · `+0x34` 읽고 쓰기 · `+0x45`·`+0x56`·`+0x152` 주소 넘김. 포인터 표로 가리키는 곳 0). 파일 값 0 (`.data` 디스크 크기 `0x2000` 너머, 0 채움 쪽). `[정적]`
2. **`LdrpInitializeProcess +0x9ed` 보다 앞에서 `RtlRandom` 에 닿는 길: 없다.** `[정적]`
   - `RtlRandom` 을 부르는 곳은 위 표의 넷 (함수 셋). 간접·꼬리 호출 없음 — 나머지 참조는 내보내기 표 슬롯 둘 (서수 1421 `RtlRandom`·1422 `RtlRandomEx`, 둘 다 `0x95db0`)·`.pdata`·CFG 표뿐.
   - 앞 묶음에서 닿는 길은 하나 (`+0x531 LdrpInitializeExecutionOptions → RtlpOpenImageFileOptionsKeyEx → RtlpCreateIFEOKeyFilterKey → RtlRandom`) 인데, 만들기 표시가 0 이라 `+0x2a2 JZ` 로 건너뛰고, test.exe 는 그 앞 `LdrpInitializeExecutionOptions +0xa6`·`+0xad JNZ` (매개변수 `Flags & 0x4000`) 로 call 자체를 건너뛴다 (run46). `RtlpOpenImageFileOptionsKeyEx` 를 부르는 지도 안 다른 곳도 모두 직전 `XOR R8D,R8D`.
   - run59 쿠키 사슬 (`InitSecurityCookie`·`LdrInitSecurityCookie`·`LdrpGenRandom`·`LdrInitializePerThreadRng`·`RtlEncodePointer`·`LdrInitializeMrdata`) 은 `RtlRandom`·`RtlpInitRandomExVector`·`RtlpRandomExInit` 에 닿지 않는다.
3. **`RtlpInitRandomExVector` 본문** — 위 "PE_emulator 쪽" 3절. `[정적]`
4. **`RtlRandom` 갈래** — 위 2절 표. `[정적]`
5. **run46 다시 보기 → 정정 필요** (아래 "정정"). 길이 늘 탄다: `+0x9ed` 를 건너뛰는 점프는 실패 출구 `+0xa7b` (`+0x3b3`·`+0x3e0`·`+0x575`·`+0x630`·`+0x9d4`) 와 `+0x1e3b` (`RtlRaiseStatus`, `+0x784 JNZ`) 뿐, `+0x9e5 ~ +0x9fa` 안으로 뛰어드는 점프 0. `RtlHpGlobalsInitialize +0x0 ~ +0x5c` 사이 분기 0. `[정적]` (결과서)

### 사진 대조 (run44 덤프 읽기 로그)

`RtlpRandomExInit` 실제 주소 = ntdll 베이스 `0x7ffafb660000` + `0x1ce878` = `7ffa'fb82e878` (run44 부팅 기준).

| 사진 | 지점 | `7ffa'fb82e878` | 줄 |
|---|---|---|---|
| P0 | `PspUserThreadStartup` 입구 | 읽기 불가 `????` | `04_baggage-read_P0.txt:3706` |
| P1 | `LdrInitializeThunk` 입구 | 읽기 불가 `????` | `04_baggage-read_P1.txt:3705` |
| P1a | `LdrpInitializeProcess +0x199d` | **`0x2`** (앞 칸 `…e870` = 0) | `04_baggage-read_P1a.txt:4097` |
| P1b | `+0x1bb6` | `0x2` | `04_baggage-read_P1b.txt:5215` |
| P2 | `NtContinue` 입구 | `0x2` | `04_baggage-read_P2.txt:5227` |

- P0·P1 은 ntdll `.data` 9쪽 전부 (`0x1cc000 ~ 0x1d4000`) 가 읽기 불가라, 사진만으로는 "P1 에 0" 을 못 보인다. 가르는 건 정적 (쓰는 곳이 `RtlRandom` 뿐, 첫 도달이 `+0x9ed` 안). 사진 P1a 값 `2` 는 run-once 끝 값 (`2 | Context 0`) 과 딱 맞는다 `[관찰]` + `[정적]`.
- `RtlpRandomExConstantVector` (`7ffa'fb8339c0 ~`) 는 P1a 에 이미 채워져 있다 (`619336c3`, `7e5153be` …, `_P1a.txt:5398`).
- `RtlpRandomExAuxVarY` (`7ffa'fb833bc0`): P1a `0xc899afee` (`_P1a.txt:5430`) → P1b·P2 `0xb6cd45cc` (`_P1b.txt:6548`, `_P2.txt:6560`). 이 칸을 쓰는 곳은 `RtlpInitRandomExVector +0xc5` 와 `RtlRandom +0x11f` 뿐이라 **P1a ~ P1b 사이에 `RtlRandom` 이 또 불렸다** (결과서).
  - (flow-writer, 로그 줄 비교) 같은 구간에 벡터도 **두 칸** 바뀌었다: `7ffa'fb833b20` (벡터 `+0x160`) `6c75474a` → `0025bf3c`, `7ffa'fb833b78` (`+0x1b8`) `3aecf307` → `7ffde723` (`_P1a.txt:5420`·`:5425` ↔ `_P1b.txt:6538`·`:6543`, 칸 `7ffa'fb833b78` 은 `…b70` 줄의 둘째 8바이트 아래쪽). 한 번 부를 때 한 칸만 맞바꾸니 (`+0xe6`) **최소 두 번** 불린 것으로 보인다 `[추측]` (같은 칸을 두 번 고르면 더 많을 수도). P1b ~ P2 사이엔 두 칸 모두 그대로.
  - 누가 불렀는지는 `[공백]` (**Q117**). ntdll 안 caller 는 위 표 밖에 없어서 다른 모듈이 내보내기 `RtlRandomEx`/`RtlRandom` 을 부른 것 `[추측]` (결과서). 동적 확인 거리 (아래).

### 호출 관계

```
LdrpInitializeProcess
 ├─ +0x134  RtlEncodePointer(0)                     NtQueryInformationProcess 0x24 (+0x48) — 프로세스 첫 0x24 (run59)
 │  …
 └─ +0x9f2  RtlInitializeHeapManager                                                  [정적]
          ├─ +0x12  RtlHpGlobalsInitialize            씨앗 = TEB+0x48 ^ (KUSD +0x320 × +0x4 >> 0x18)
          │        ├─ +0x51  RtlRandom (첫 호출, 상태 0)
          │        │        │  +0x34  CMPXCHG RtlpRandomExInit 0 → 1
          │        │        ├─ +0x53  RtlpInitRandomExVector
          │        │        │        ├─ +0x29  NtQueryInformationProcess 0x24   ← 힙 구간 시스템 콜 #1 (run61 새로)
          │        │        │        │  +0x3d  ^ LdrSystemDllInitBlock+0x98
          │        │        │        │  +0x87  RtlpRandomExConstantVector[0..127]
          │        │        │        │  +0xc5  RtlpRandomExAuxVarY
          │        │        ├─ +0x6c  RtlRunOnceComplete(&RtlpRandomExInit, 0, 0)
          │        │        │        │  +0x67  XCHG → 값 2
          │        │        │  +0xe6  벡터 한 칸 맞바꿈 · +0x11f AuxVarY 더함 (매 호출)
          │        └─ +0x61  RtlRandom (둘째, 상태 2 → +0x27 JZ → +0x9a, syscall 없음)
          │  … (+0x2d · +0x75 · +0x7d — run46 #2·#3)
          ├─ +0xa2  RtlpGetModifiedProcessCookie
          │        └─ +0x27  NtQueryInformationProcess 0x24   ← #4 (run46 옛 #3)
          └─ …
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 두 칸 들여쓴 `+0x..` 줄 (호출 기호 없음) 은 그 함수 안 줄.
- 콜스택 기대 (첫 syscall 에서 `k`): `NtQueryInformationProcess` ← `RtlpInitRandomExVector+0x29` ← `RtlRandom+0x53` (디버거 표시 이름은 `RtlRandom` 또는 `RtlRandomEx` 중 하나 `[추측]`) ← `RtlHpGlobalsInitialize+0x51` ← `RtlInitializeHeapManager+0x12` ← `LdrpInitializeProcess+0x9f2` ← `_LdrpInitialize+0x16c`. 여기선 꼬리 JMP 전이라 `RtlInitializeHeapManager` 가 보인다. `[정적]`

## 확인된 것

- **`RtlpRandomExInit` 을 켜는 곳은 `RtlRandom` 안뿐이고, 이 프로세스의 첫 `RtlRandom` 은 `RtlHpGlobalsInitialize +0x4c`** (힙 구간 맨 앞). 힙 전에 켜는 길은 없다. `[정적]` (결과서 1·2·5(a))
- **힙 구간 시스템 콜은 13개** (무작위 몫 n = 0 이면 12개). `NtQueryInformationProcess` 는 구간에 **2번** (`RtlpInitRandomExVector +0x29`, `RtlpGetModifiedProcessCookie +0x27`), 프로세스 전체로 3번. `[정적]`
- **`RtlpInitRandomExVector` 의 syscall 은 하나** (`NtQueryInformationProcess(-1, 0x24, &지역, 4, NULL)`), 쓰는 전역은 `RtlpRandomExConstantVector` (128 dword)·`RtlpRandomExAuxVarY`, 읽는 전역은 `LdrSystemDllInitBlock +0x98`. `RtlpRandomExInit` 은 부른 쪽이 `RtlRunOnceComplete +0x67` 로 2 를 쓴다. `[정적]`
- **사진 P1a ~ P2 의 `RtlpRandomExInit` = 2** — run-once 끝 값과 같다. `[관찰]` run44 + `[정적]`
- **P1a ~ P1b 사이에도 `RtlRandom` 이 불린다** (`AuxVarY` 값이 바뀜). `[관찰]` run44 사진 + `[정적]` (쓰는 줄이 둘뿐). 부른 주인은 `[공백]`.

## 결과서와 다르게 적은 곳

- 결과서 5(c) 5 는 run46 호출 관계 그림에 `└─ +0x4c RtlRandom → +0x4e RtlpInitRandomExVector (#1)` 를 넣으라고 했다. run46 그림은 **ret 자리** 기준이라 `+0x51 RtlRandom` → `+0x53 RtlpInitRandomExVector` → `+0x29 NtQueryInformationProcess` 로 넣었다 (call 자리는 같은 줄의 `+0x4c`·`+0x4e`·`+0x24`).
- 결과서 2번 "앞 묶음에서 닿는 길 찾기" 의 `RtlpHpGlobalsInitialize` 는 오타 — 맞는 이름은 `RtlHpGlobalsInitialize` (`_index.tsv`·`.asm`). 결과서는 고치지 않음.
- 결과서 "동적" 3 은 `bp ntdll!RtlRandom` "두 번째 이후 히트" 라 했지만, 힙 구간 `RtlHpGlobalsInitialize` 가 이미 두 번 부르니 P1a ~ P1b 몫은 **셋째 이후** 히트다 (아래 "아직 확인 필요" 3 은 셋째 이후로 적음).
- 결과서 5(b) 는 P1a ~ P1b 에 "`RtlRandom` 이 또 불렸다" 까지만 적었다. 이 문서는 같은 로그 줄에서 벡터 두 칸도 바뀐 것을 더 적었다 ("최소 두 번" 은 `[추측]`).

## 정정

- **run46 — 힙 구간 시스템 콜 12 → 13.** run46 은 `RtlHpGlobalsInitialize` 의 `RtlRandom` 두 번을 "시스템 콜 없이" 로 적었다. 실제로는 **첫 호출만** 상태 0 이라 `RtlpInitRandomExVector` 안 `NtQueryInformationProcess(0x24)` (call `+0x24` / ret `+0x29`) 를 한 번 부른다. `RtlHpGlobalsInitialize` 가 `RtlInitializeHeapManager` 의 첫 call 이라 구간 맨 앞이다.
  - 맨 앞 표에 새 **#1** 을 넣고 옛 #1 ~ #12 → **#2 ~ #13** (옛 #3 `RtlpGetModifiedProcessCookie` = 새 #4). 합 12 → **13** (n = 0 이면 #12 (옛 #11) 이 빠져 12). 요약 `NtQueryInformationProcess` 1 → **2**.
  - "힙 주소 무작위" 줄의 "시스템 콜 없이" 를 고침. 호출 관계 그림 `+0x12 RtlHpGlobalsInitialize` 아래 두 줄 더함.
  - run46 문서 맨 위 정정 한 줄 + 위 줄들 (취소선 + 새 값). 해설서 `run46-process-heap-해설.md` 는 맨 위 한 줄만 (본문 47·174·334·356·359·361·380 줄 근처 "12개"·"시스템 콜 없이" 는 explainer 몫).
- **짐 지도 6장 `RtlRandom` 줄** "시스템 콜 없음" → "첫 호출만 `NtQueryInformationProcess(0x24)` 1번" (이 스프린트 문서라 바로 고침).
- **flow.md 2.4 힙 절** 트리의 `RtlHpGlobalsInitialize … — 시스템 콜 없음` 과 "시스템 콜 12개" 를 고침. **2.4 run51 절** 의 "힙 관리자 쪽 = 둘째" 는 셋째 (그 앞 둘째 = `RtlpInitRandomExVector`).

## 짐 지도 반영 (`baggage.md`)

- 3.8 `.data` 에 **7줄** (근거 칸 `sast56`, 줄 끝 **"Q116 으로 닫힘"**): `RtlpRandomExInit` 3줄 (`RtlRandom +0x34` 직접 · `RtlRunOnceComplete +0x67` 주소 넘김 · `RtlpRunOnceWaitForInit +0x4a` 주소 넘김, 조건부 안 탐), `RtlpRandomExConstantVector` 2줄 (`RtlpInitRandomExVector +0x87` · `RtlRandom +0xe6`), `RtlpRandomExAuxVarY` 2줄 (`RtlpInitRandomExVector +0xc5` · `RtlRandom +0x11f`).
- 모르던 전역 3개가 아는 것이 됨 (모두 `≤P1a`, 정확): `.data` 아는 것 91 → **94**, 모름 347 → **344**, ntdll 이 씀 90 → **93**. ntdll 전역 3구역 합 아는 것 113 → **116**, 모름 380 → **377**. 7.3 `.data` `≤P1a` 88 → **85**. 사진 칸 수 (`끝까지 0` 265) 는 그대로.
- 6장 `RtlRandom` 줄 고침 + 새 부품 줄 둘 (`RtlRandom`·`RtlpInitRandomExVector` RandomEx 벡터, run-once `RtlRunOnceComplete`·`RtlpRunOnceWaitForInit`).
- 3.11 `Cookie` 줄·4장 쿠키 짝 줄·5장 ② 4 의 힙 부분에 `RtlpInitRandomExVector` 를 이음. 3.8 `0x1ce430` 줄의 "구간 시스템 콜 #1" → #2.
- 새 **7.21** (동적 확인 — Q104 VM 단계).

## 아직 확인 필요

- 동적 (결과서 "동적" 번호 그대로, 급하지 않음 — 정적 근거로 닫힘) → 대기열 **Q104 VM 단계**:
  - **1** `RtlRandom` 첫 호출의 돌아올 자리와 값 변화: `bp ntdll+0x95db0` 첫 히트의 `dq @rsp L1` = `ntdll+0x95641` (`RtlHpGlobalsInitialize +0x51`) 인지. 그때 `dq ntdll+0x1ce878 L1` = 0 (더 앞 `ntdll+0x9c334` `RtlInitializeHeapManager` 입구에서도 0). `ntdll+0x95e03` (`RtlRandom +0x53`) 에서 **1**, `ntdll+0x95e1c` (`RtlRandom +0x6c`) 에서 **2**.
  - **2** `ntdll+0x95f45` (`RtlpInitRandomExVector +0x29`) 에서 `r eax`, `dd @rsp+0x58 L1` = 쿠키. `RtlEncodePointer +0x48`·`RtlpGetModifiedProcessCookie +0x27` 에서 본 값과 같은지 (run46 D5 와 같이).
  - **3** (보너스, 대기열 **Q117**) P1a ~ P1b 사이 `RtlRandom` 을 또 부른 주인 `[공백]`: `bp ntdll!RtlRandom` 셋째 이후 히트의 `k`. 또는 `RtlpRandomExAuxVarY` (`ntdll+0x1d3bc0`) 에 `ba w4`. 필수 아님.
  - **4** 멈춤점 정리: `ntdll!RtlInitializeHeapManager` 입구 (`+0x9c334`) · `RtlRandom +0x53`/`+0x6c` · `RtlpInitRandomExVector +0x29`. test.exe 조건은 기존 약속 그대로.
- 정적: 없음.

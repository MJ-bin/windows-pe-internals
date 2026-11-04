# run64 apiset-phase — 해설

> 이 글은 [run64 문서](run64-apiset-phase.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q120c** — PE_emulator 요청 11 의 8번: run57 의 "ApiSet 섹션은 부팅 위상 0" 이 맞나 확인·정정 (`_workspace/plan.md`, 이번에 **끝**)
  - 다음 항목: **Q120b** (run65 예정) → Q104 VM 단계 → …
  - 새로 생긴 일 둘 (맨 뒤): **Q122** (동적, 선택·낮음 — 부팅 위상을 실제로 멈춰서 보기), **Q123** (작은 문서 정정 — run57 트리의 `ExInitializeNls` 숫자).
- 근거:
  - run 문서 [`run64-apiset-phase.md`](run64-apiset-phase.md) (가장 중요)
  - 요청서 [`_workspace/sast/60_apiset-phase.md`](../../_workspace/sast/60_apiset-phase.md)
  - 결과서 [`_workspace/sast/60_apiset-phase_result.md`](../../_workspace/sast/60_apiset-phase_result.md) (판정: **분석성공**, PDB 일치)
- **정적만** 했다. Ghidra 도 안 열었다. 이미 뽑아 둔 `.asm` 과 PDB 타입만 다시 읽었다.
- **정정 바퀴다.** 새 짐은 없다. run57 에서 틀린 한 군데를 바로잡은 바퀴다.

기초 말은 다시 안 푼다. 링크만.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 부팅 = 관리사무소가 **문 여는 날**, `Phase1Initialization` 이 세 함수를 차례로 부르는 그림 → [run48 해설](run48-system-dll-boot-해설.md) 2.1
- ApiSet = **별명 사전**, 섹션 = 관리사무소 **창고의 원본 물건**, `nt+0xe2d818` = 관리사무소 **장부** → [run57 해설](run57-nls-apiset-해설.md) 2.3 · 2.15

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

test.exe 를 실행하기 **한참 전**, 컴퓨터가 켜질 때 이야기다. 흐름도 범위 앞의 단계 `Ⓑ` (부팅).

```
(컴퓨터 켜기 = 관리사무소 문 여는 날)
  위상 0  InitBootProcessor           NLS 상태 처음 값 (RtlNlsInitState)
  위상 1  Phase1Initialization 스레드
            ├─ Phase1InitializationDiscard   ApiSet 별명 사전 만들기   ← 이번 (어느 위상인가)
            ├─ IoInitSystem                  PsLocateSystemDlls (Ⓑ1)
            └─ Phase1InitializationIoReady   ExInitializeNls (ⒷN) … 첫 유저 프로세스
(나중) cmd.exe 의 부탁 → 커널 → MmMapApiSetView 가 새 가게에 별명 사전을 펼침 → ntdll → main
```

### 무엇이 궁금했나

- run57 은 "별명 사전 (ApiSet 섹션) 은 부팅 **위상 0** 에서 만든다" 고 적었다.
- PE_emulator 쪽이 같은 코드를 읽고 "**위상 1** 아니냐" 고 물었다. 누가 맞나?
- 덤으로: 사전 위치를 적어 둔 장부 칸 세 개 (`nt+0xe2d818`·`820`·`828`) 에 정말 이름이 없나?

### 바퀴 전에 알던 것

- `[정적]` (run57) `MiInitSystem +0x638` 이 `MiInitializeApiSets` 를 부르고, 그 함수가 전역 세 칸을 쓴다 (`+0xb1`·`+0xb8`·`+0xce`).
- `[정적]` (run57) 위상 0 사슬: `InitBootProcessor +0x595 MmInitSystem(0)` → `MmInitSystem +0x144 MiInitSystem` → `+0x638`. ← 이번에 **틀렸다고** 밝혀진 줄.
- `[정적]` (run48) `Phase1Initialization` 이 `Phase1InitializationDiscard` → `IoInitSystem` → `Phase1InitializationIoReady` 차례로 부른다.

---

## 2. 찾은 것

### 2.1 "위상" 이 뭐냐부터

- **새 말 — 위상** (phase) = 부팅을 몇 덩어리로 나눈 것 중 **몇 번째 덩어리**인지 붙인 번호. 0, 1, 2 …
  - 비유: 문 여는 날의 **1교시, 2교시, 3교시**. 같은 담당자 (`MmInitSystem`) 가 교시마다 와서, 그 교시에 맞는 일만 한다.
- 담당 함수는 **첫 인자 (ECX) 로 교시 번호를 받는다.** 그래서 "몇 위상인가" = "그 호출 때 ECX 가 몇이었나".
- run57 해설 2.3 에선 "위상 0 = 아주 이른 단계" 를 `[추측]` 으로 적었다. 이번엔 근거가 하나 더 생겼다 `[정적]`:
  - 커널 전역 `InitializationPhase` (`0x140fc37e0`) 에 `InitBootProcessor +0x178` 이 **0**, `Phase1InitializationDiscard +0xcb` 가 **1** 을 쓴다. `Phase1InitializationIoReady +0x22f` 는 `INC` (하나 올림).
  - 즉 커널도 스스로 "지금 몇 교시" 를 칠판에 적어 둔다.

### 2.2 맞는 사슬 (대표만)

run 문서 "호출 관계" 를 줄였다. `+0x..` 는 ret 자리 (콜스택 숫자).

```
InitBootProcessor                                    위상 0
 ├─ +0x111  RtlNlsInitState                          NLS 상태 처음 값 (ⒷA 의 위상 0 몫)
 ├─ +0x59a  MmInitSystem(0) → +0x149 MiInitSystem(0)  ApiSet 아님
 └─ +0xbb2  PsInitSystem(0) → +0x51 PspInitPhase0
                              └─ +0xb8f PsCreateSystemThread ─┐ 시작 함수
                                                              ▼
Phase1Initialization (시스템 스레드)                     위상 1
 ├─ +0x23  Phase1InitializationDiscard
 │          └─ +0xc6a  MmInitSystem(1)
 │                      └─ +0xf5  MiInitSystem(1)
 │                                 └─ +0x63d  MiInitializeApiSets   ← 별명 사전 만들기 (ⒷA 의 위상 1 몫)
 ├─ +0x3b  IoInitSystem → … PsLocateSystemDlls     (Ⓑ1)
 └─ +0x66  Phase1InitializationIoReady → +0x2c ExInitializeNls (ⒷN) …
```

- `[정적]` 전부 결과서 1-1 ~ 1-4.
- `Phase1Initialization` 은 `PspInitPhase0 +0xb67 LEA` 로 주소를 잡아 `PsCreateSystemThread` 에 넘기는 **시스템 스레드 시작 함수**다 `[정적]`.
  - 비유: 1교시 끝에 "2교시 담당 직원" 을 한 명 새로 불러 세우는 것. 그 직원이 위상 1 일을 한다.

**한 줄 읽기** — `Phase1InitializationDiscard +0xc65 MmInitSystem(1)` (ret `+0xc6a`):

- **누가**: `Phase1InitializationDiscard` (위상 1 스레드 안).
- **어디서**: `+0xc65` 의 CALL. 콜스택엔 ret `+0xc6a` 로 보인다.
- **누구를**: `MmInitSystem`.
- **무엇을 위해**: 메모리 담당에게 "**1교시** 일 해 줘". 1 이라는 숫자는 `+0xa8f MOV EBX,1` 로 넣고, `+0xc63 MOV ECX,EBX` 로 첫 인자에 옮긴다. 그 사이 EBX 를 바꾸는 명령은 없다 `[정적]`.

WinDbg 로 멈춘다면 콜스택은 이렇게 보일 것이다 (ret 기준, 정적으로 센 값):
`MiInitializeApiSets` ← `MiInitSystem+0x63d` ← `MmInitSystem+0xf5` ← `Phase1InitializationDiscard+0xc6a` ← `Phase1Initialization+0x23`.

### 2.3 왜 run57 이 틀렸나 — 갈림길 표지판을 거꾸로 읽었다

`MiInitSystem` 은 입구 가까이에서 교시 번호로 길을 가른다. 결과서 1-1 의 줄들 `[정적]`:

| 자리 | 하는 일 |
|---|---|
| `+0x71 LEA EBX,[R14+0x1]` | EBX = 1 (R14 는 앞에서 0) |
| `+0x75 TEST ECX,ECX` · `+0x77 JNZ 0x140c5c2c8` | ECX ≠ 0 이면 **`+0x428`** 로 점프. ECX = 0 이면 안 뛰고 `+0x7d` 로 |
| `+0x7d ~ +0x427` | **위상 0 길.** 이 안엔 `MiInitializeApiSets` 호출이 없다 |
| `+0x428 CMP ECX,EBX` · `+0x42a JNZ 0x140c5c4ea` | ECX ≠ 1 이면 **`+0x64a`** 로 |
| `+0x430 ~ +0x649` | **위상 1 길.** 여기 `+0x638 MiInitializeApiSets` |
| `+0x64a CMP ECX,2` … | 위상 2 길. ApiSet 없음 |

- **새 말 — `JNZ`** = "0 이 **아니면** 점프" 하는 명령. 바로 앞 `TEST`/`CMP` 결과를 본다.
- 목적지는 `.asm` 의 점프 주소에서 함수 시작을 빼서 센다: `0x140c5c2c8 − 0x140c5bea0` = `+0x428` (결과서가 스크립트로 42개 점프 전부 다시 셈).

단계별로 따라가 보자.

1. `+0x77 JNZ` 는 "ECX 가 0 이 아니면 `+0x428` 로 가라" 다.
2. 그러니 **점프하는 쪽 (`+0x428`) 이 위상 1·2**, **점프 안 하는 쪽 (`+0x7d`) 이 위상 0** 이다.
3. `+0x638` 은 점프하는 쪽 안에서도, `+0x42a JNZ` 가 **안 뛰는** 쪽 (`+0x430 ~ +0x649`) 에 있다. 거기서 안 뛴다 = ECX 가 1 이다.
4. 이 구간 안으로 들어오는 점프 15개는 전부 같은 구간 안에서 출발한다. 위상 0 구간에서 밖으로 나가는 점프는 `+0x419 → +0x684` (끝맺음) 하나뿐. 그래서 **`+0x638` 은 ECX = 1 일 때만 닿는다** `[정적]`.

run57 은 1번을 반대로 읽었다. `+0x428` 을 "위상 0 쪽" 으로 보고, 위상 0 의 자리 `InitBootProcessor +0x595` → `MmInitSystem +0x144` 에 이어 붙였다 `[정적]` (결과서 1-5).

- 비유: 갈림길 표지판에 "**0 교시가 아니면 → 오른쪽**" 이라 적혀 있는데, "**0 교시면 → 오른쪽**" 으로 읽은 꼴이다. 오른쪽 길 끝에 있는 가게 (별명 사전 만들기) 를 1교시가 아니라 0교시 길에 있다고 지도에 그려 버렸다.
- run 문서 "도구 메모" 에 따르면 이 꼴 (점프 목적지 거꾸로 / 끝자리 잘못 읽기) 은 run51·run56 ~ run61 에서도 나왔다. "목적지 = 주소 − 함수 시작" 을 결과서에 적게 한 규칙이 이번에 잡았다.

### 2.4 `MmInitSystem` 쪽도 맞춰 보면 — 넷 다 들어맞는다

`MmInitSystem` 을 부르는 곳은 넷이고, 각자 넘기는 교시 번호가 다르다 `[정적]` (결과서 1-3):

| 부르는 곳 | call / ret | 위상 |
|---|---|---|
| `KiInitializeBootStructures` | `+0x2ad` / `+0x2b2` | −1 |
| `InitBootProcessor` | `+0x595` / `+0x59a` | 0 |
| **`Phase1InitializationDiscard`** | **`+0xc65` / `+0xc6a`** | **1** |
| `Phase1InitializationIoReady` | `+0x15b` / `+0x160` | 2 |

그리고 `MmInitSystem` 안에서 `MiInitSystem` 을 부르는 자리는 **둘뿐** `[정적]`:

- `+0xf0` (ret `+0xf5`) — 위상 1 전용 자리, ECX = 1 (`+0xe3 MOV ECX,EBX`).
- `+0x144` (ret `+0x149`) — 위상 0 과 2 가 같이 쓰는 자리.

run57 이 "위상 0·2 는 같은 자리 `+0x144`" 라고 쓴 건 **맞았다.** 위상 1 의 따로 된 자리 `+0xf0` 을 **빠뜨린 것**이 문제였다.

- 비유: 담당자 책상에 서랍이 둘. 0교시·2교시 서류는 같은 서랍, 1교시 서류만 따로 된 서랍. run57 은 둘째 서랍이 있는 줄 몰랐다.

덤 `[정적]`: 위상 1 길은 `+0x638` 보다 먼저 `+0x497 MiSectionInitialization` (ret `+0x49c`) 을 부른다 (코드 순서). "그래서 섹션 쪽 준비가 끝난 뒤에야 `MmCreateSection` 이 돈다" 는 `[추측]`.

### 2.5 무엇이 바뀌고, 무엇은 그대로인가

**바뀐 것**

- 별명 사전 만들기: ~~위상 0 `InitBootProcessor` 안~~ → **위상 1 `Phase1Initialization` 스레드 안** `[정적]`.
- 짐 지도의 `ⒷA` 묶음: run57 은 NLS 상태 처음 값 (`RtlNlsInitState`) 과 별명 사전을 **같은 위상 한 덩어리**로 적었다. 실제로는 `[정적]`
  - `RtlNlsInitState` (`InitBootProcessor +0x10c`, ret `+0x111`) = **위상 0** (이건 원래 맞음)
  - `MiInitializeApiSets` = **위상 1**
  - → 짐 지도에서 줄마다 위상을 따로 적었다. 이름 `ⒷA` 와 짐 수는 그대로.

**그대로인 것** (PE_emulator 가 제일 신경 쓰는 곳)

- **순서는 안 바뀐다** `[정적]`. 위상 1 스레드 안에서 `Phase1Initialization` 이 세 함수를 이 차례로 부르기 때문이다 (run48 에서 본 그대로):
  1. `+0x1e` `Phase1InitializationDiscard` → 별명 사전
  2. `+0x36` `IoInitSystem` → `PsLocateSystemDlls` (`Ⓑ1`)
  3. `+0x61` `Phase1InitializationIoReady` → `ExInitializeNls` (`ⒷN`)
- 그래서 **`MiInitializeApiSets` < `PsLocateSystemDlls` < `ExInitializeNls`** 는 위상 0 이든 1 이든 같다.
- PE_emulator 원문 "`PsLocateSystemDlls` 보다 앞이라 순서엔 영향 없음" 은 그대로 맞다. PE_emulator 쪽 읽기 (`Discard +0xc65` = `MmInitSystem(1)`, `MmInitSystem +0xf0` = 위상 1 자리) 도 맞았다.
- run57 의 나머지도 그대로: `MiInitializeApiSets` 안 (`+0x52`·`+0x73`·`+0xac`·`+0xb1`·`+0xb8`·`+0xce`), `MmMapApiSetView +0x92` (PEB `+0x68` 쓰기), `ExInitializeNls` 쪽 전부.
- 비유: 교시 번호만 0 → 1 로 고쳤다. "관리실 세트 꺼내기 (`Ⓑ1`) 보다 먼저 별명 사전을 만든다" 는 시간표 순서는 그대로다.

### 2.6 장부 칸 이름 — `MiState.Sections.ApiSet*`

- **새 말 — `_MI_SECTION_STATE.ApiSet*`** = 커널 메모리 담당의 큰 전역 장부 `MiState` 안, 섹션 쪽 칸 묶음 (`+0x1c0 Sections`, 타입 `_MI_SECTION_STATE`) 에 있는 ApiSet 칸 셋.
  - 비유: run57 은 장부에서 "몇 번째 줄" 만 알고 칸 이름표를 못 찾았다. 알고 보니 **장부 양식 (PDB 타입) 에 칸 이름이 인쇄돼 있었다.**

| 주소 (rva) | 이름 | 담는 것 | 쓰는 줄 (`MiInitializeApiSets`) |
|---|---|---|---|
| `0xe2d818` | `MiState.Sections.ApiSetSection` | 섹션 개체 (`void*`) | `+0xb1` |
| `0xe2d820` | `MiState.Sections.ApiSetSchema` | 커널 뷰 주소 (`void*`) | `+0xb8` |
| `0xe2d828` | `MiState.Sections.ApiSetSchemaSize` | 크기 (`unsigned __int64`) | `+0xce` |

- 셈 `[정적]`: `MiState` rva `0xe2cb80` + `Sections` `0x1c0` + 칸 `0xad8` / `0xae0` / `0xae8`.
- "`MiState` 의 타입이 `_MI_SYSTEM_INFORMATION`" 이라는 건 PDB 가 직접 말해 주진 않는다. 결과서가 넷으로 맞춰 봤다 `[정적]`: 다음 공개 심볼까지 거리 `0xc1c0` = 구조체 크기, `MiInitSystem +0x222` 의 `+0xe00` 칸, 같은 셈의 다른 칸 셋, run48 의 `MiState+0x1000 Processes`.
- 그래서 run57 의 "PDB 공개 이름 없음" 은 **반만 맞다**: 세 주소에 공개 심볼은 없지만, 타입에 칸 이름이 있다.
- 덤 `[정적]`: run63 이 이름 없이 적은 ScpCfg 커널 전역 셋도 같은 장부 칸이다 — `0xe2d8e0` = `ImageViewExtensionOffsets`, `0xe2d8f0` = `TotalImageViewReservationSize`, `0xe2d8f8` = `ScpCfgPages`.
- 왜 `.asm` 주석에 이름이 안 붙었나: 스크립트가 심볼 앞 `0x100` 안에서만 이름을 붙이게 짜여 있어서다 (설계). 주석이 없다고 "PDB 이름 없음" 으로 읽으면 안 된다 (run 문서 "도구 메모").

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 곳

- **2.1 `nt!MmMapApiSetView` 절 "부팅 때" 줄**
  - 전: `InitBootProcessor +0x595 MmInitSystem(0)` → `MmInitSystem +0x144` → `+0x638` (위상 0 쪽), "PDB 공개 이름은 없다" `[정적]`
  - 후: 줄 그어 지우고 **(run64 정정) 위상 1** 사슬 `Phase1Initialization +0x1e` → `Phase1InitializationDiscard +0xc65` → `MmInitSystem +0xf0` → `MiInitSystem +0x638` `[정적]`. 위상 0 길은 ApiSet 을 안 만든다는 줄, 순서는 그대로라는 줄을 덧붙임.
- **4장**: 정정 한 줄 + 선택 동적 한 줄 (Q122) 새로.
- **5장**: run57 줄의 "위상 0" → ~~위상 0~~ 위상 1 (run64 정정), run64 줄 새로.
- 태그는 그대로 `[정적]` 이다. 올라간 것도 내려간 것도 없고, **내용**만 고쳤다.
- 같이 고친 곳 (run 문서 "정정"): `run57-nls-apiset.md`, `run57-nls-apiset-해설.md` (맨 위 정정 한 줄 + 해당 줄 표시), baggage.md 쓰는 말 `ⒷA` · 3장 줄임 표 · 5장 `Ⓑ` · 7.6.3 · 7.6.4.

### 큰 그림과 이어지는 곳

- PE_emulator 는 부팅 쪽을 흉내 낼 때 "별명 사전을 언제 만들어 두나" 를 정해야 한다. 이번 결과로 **위상 1, `Ⓑ1` 앞** 이 확정됐고, 순서상 할 일은 바뀌지 않는다.
- 에뮬레이터에서 이 전역 칸에 이름을 붙일 때 `MiState.Sections.ApiSetSection` 등 진짜 이름을 쓸 수 있다 (CLAUDE.md "진짜 nt·ntdll 과 같은 이름").

---

## 4. 아직 모르는 것

- **실제 부팅에서 정말 ECX 가 0 → 1 → 2 로 세 번 들어오나**
  - 왜 이번에 못 봤나: 정적만 했다. 다만 `CMP`/`JNZ` 와 점프 목적지 표로 정해진 일이라 실행에서 뒤집힐 데가 없다 → **필수 아님**.
  - 보려면 부팅 아주 초기에 KDNET 으로 **첫 멈춤**이 있어야 한다.
    - **새 말 — `INIT` 구역** = ntoskrnl PE 안 구역 이름 (`0xbec000 ~ 0xc8ec1c`). `MiInitSystem`·`MiInitializeApiSets`·`MmInitSystem` 등이 여기 있다 `[정적]` (구역표). 부팅이 끝나면 버려져서 멈춤점을 못 걸 수 있다는 건 `[추측]` (일반 지식).
    - 비유: 문 여는 날에만 쓰고 치우는 임시 접수대. 다음 날 가면 이미 없다.
  - 어디서 보나: **Q122** (동적, 선택·낮음). `bp nt!MiInitSystem` 세 번 `r ecx`, `bp nt!MiInitializeApiSets` 의 `k`.
- **세 칸의 실제 값**: 이미 있는 **Q110** 의 `dq nt+0xe2d818 L3` 그대로. 덤으로 `dt nt!_MI_SECTION_STATE ApiSet* (nt!MiState+0x1c0)` 한 줄로 이름도 같이 확인 (위상과 무관).
- **run57 트리 숫자 하나**: run57 호출 관계 트리의 `└─ +0x27 ExInitializeNls` 는 트리 규칙 (ret 자리) 대로면 `+0x2c` 다 (call `+0x27`, ret `+0x2c`). 이번엔 안 고쳤다 → **Q123** (문서, flow-writer).
  - 이 글 2.2 트리는 run64 문서를 따라 `+0x2c` 로 적었다.

---

## 5. 더 읽기

- run 문서: [`run64-apiset-phase.md`](run64-apiset-phase.md) — "결과" 의 갈래 표 셋, "정정"
- 결과서: [`_workspace/sast/60_apiset-phase_result.md`](../../_workspace/sast/60_apiset-phase_result.md) — 1-1 (점프 따라가기), 1-3 (`MmInitSystem` 을 부르는 곳), 2 (칸 이름 셈), 3-1 (위상 1 안 순서)
- flow.md: [2.1 `nt!MmMapApiSetView`](flow.md) 절 "부팅 때", 4장, 5장 run57·run64 줄
- 고쳐진 옛 문서: [`run57-nls-apiset.md`](run57-nls-apiset.md), [`run57-nls-apiset-해설.md`](run57-nls-apiset-해설.md) 2.3
- 짐 지도: [`baggage.md`](baggage.md) 쓰는 말 `ⒷA`, 7.6.3, 7.6.4
- 부팅 세 함수 순서: [run48 해설](run48-system-dll-boot-해설.md) 2.1

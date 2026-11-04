# 2026-11-05 run64 — ApiSet 섹션은 부팅 **위상 1** 에서 만든다 (Q120c, PE_emulator 요청 11 의 8번 — **정정 바퀴**): run57 의 "위상 0 (`InitBootProcessor +0x595 MmInitSystem(0)`)" 은 틀렸다. 맞는 사슬은 `Phase1Initialization +0x1e` → `Phase1InitializationDiscard +0xc65 MmInitSystem(1)` → `MmInitSystem +0xf0 MiInitSystem(1)` → `MiInitSystem +0x638 MiInitializeApiSets`. 전역 세 칸 `nt+0xe2d818`·`820`·`828` 의 이름은 PDB 타입에 있다 — `MiState +0x1c0 Sections` (`_MI_SECTION_STATE`) 의 `ApiSetSection`·`ApiSetSchema`·`ApiSetSchemaSize`

## PE_emulator 쪽에서 쓸 것

- **순서는 안 바뀐다.** ApiSet 섹션 만들기 (`Phase1InitializationDiscard +0xc65`) 는 여전히 `PsLocateSystemDlls` (`Ⓑ1`, `IoInitSystem` 안) 보다 앞이고 `ExInitializeNls` (`ⒷN`) 보다 앞이다. PE_emulator 원문 "`PsLocateSystemDlls` 보다 앞이라 순서엔 영향 없음" 은 그대로 맞다.
- **PE_emulator 쪽 읽기가 맞았다**: `Phase1InitializationDiscard +0xc65` = `MmInitSystem(1)`, `MmInitSystem +0xf0` = 위상 1 자리.
- **NLS 상태 처음 값과 ApiSet 은 다른 위상이다**: `RtlNlsInitState` (`InitBootProcessor +0x10c`) 는 위상 0, ApiSet 은 위상 1. 둘을 한 묶음 (`ⒷA`) 으로 같은 위상처럼 쓰던 곳을 나눴다 (짐 지도).
- **전역 칸 이름** (에뮬레이터에서 이름을 붙일 때):

| 주소 (rva) | 이름 | 타입 | 쓰는 줄 |
|---|---|---|---|
| `0xe2d818` | `MiState.Sections.ApiSetSection` | `void*` (섹션 개체) | `MiInitializeApiSets +0xb1` |
| `0xe2d820` | `MiState.Sections.ApiSetSchema` | `void*` (커널 뷰 주소) | `+0xb8` |
| `0xe2d828` | `MiState.Sections.ApiSetSchemaSize` | `unsigned __int64` (크기) | `+0xce` |

- 셈: `MiState` rva `0xe2cb80` + `Sections` `0x1c0` + 칸 `0xad8`/`0xae0`/`0xae8`. 덤으로 run63 이 이름 없이 적은 ScpCfg 전역 셋도 같은 구조체 칸이다 (아래 "확인된 것").

오프셋 읽는 법 (run60 ~ run63 과 같음):

- `call +0x..` = CALL 명령 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 값.

## 환경

- 26100.9457, 정적만 (VM 안 씀)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서 버전 대조 — `_calls.tsv` 로 알려진 사실 둘 확인)

## 목적

- `MiInitializeApiSets` 가 부팅 몇 위상에서 불리나 (run57 은 "위상 0") — 맞는 사슬을 call/ret 로.
- 전역 `nt+0xe2d818`·`820`·`828` 에 PDB 이름이 정말 없나.
- 위상이 바뀌면 run57·flow.md·baggage.md 에서 무엇이 틀리나 (고칠 줄 목록).

## 방법

- 정적: `_workspace/sast/60_apiset-phase.md` — ntoskrnl `^MiInitSystem$`·`^MmInitSystem$`·`^Phase1InitializationDiscard$`·`^InitBootProcessor$`·`^MiInitializeApiSets$` depth 0 (**`.asm` 이미 있음 — 다시 뽑지 않음**), PDB 타입 `_MI_SYSTEM_INFORMATION`·`_MI_SECTION_STATE`, 공개 심볼 `MiState`.
- 결과서: `_workspace/sast/60_apiset-phase_result.md` (**분석성공**). Ghidra 실행 0번 (analyze·export·map·refs 없음). 이미 있는 `ms/disassemble/ntoskrnl/*.asm`·`_calls.tsv`·`_refs/` 와 `pdb_struct.py`·`llvm-pdbutil dump -publics` 만.
  - 점프 목적지는 결과서가 스크립트로 전부 다시 셌다 (`MiInitSystem` 42개, `MmInitSystem` 11개).
- 오케스트레이터가 `.asm` 으로 한 번 더 본 줄: `MiInitSystem +0x77 JNZ 0x140c5c2c8` (= `+0x428`) · `+0x428 CMP ECX,EBX` · `+0x42a JNZ 0x140c5c4ea` (= `+0x64a`) · `+0x638`/`+0x63d`, `MmInitSystem +0x14 MOV R9D,ECX` · `+0xb6 XOR ECX,ECX` · `+0xe3 MOV ECX,EBX` · `+0xf0`/`+0xf5` · `+0x13c MOV ECX,2` · `+0x144`/`+0x149`, `Phase1InitializationDiscard +0xa8f MOV EBX,1` · `+0xc65`/`+0xc6a`, `InitBootProcessor +0x593 XOR ECX,ECX` · `+0x595`/`+0x59a`.
- flow-writer 가 `.asm` 으로 한 번 더 본 줄: `Phase1Initialization +0x1e`/`+0x23` (→ `Phase1InitializationDiscard`) · `+0x36`/`+0x3b` (→ `IoInitSystem`), `Phase1InitializationDiscard +0xcb` (`InitializationPhase` 쓰기) · `+0xc1b MOV ECX,EBX` · `+0xc24`/`+0xc29` (→ `PsInitSystem`) · `+0xc63 MOV ECX,EBX`, `PspInitPhase0 +0xb67 LEA RAX,[0x140705b30]` · `+0xb8a`/`+0xb8f` (→ `PsCreateSystemThread`), `PsInitSystem +0x6 JZ` · `+0x4c`/`+0x51`, `InitBootProcessor +0x10c`/`+0x111` · `+0x178` · `+0xbab` · `+0xbad`/`+0xbb2` (`InitializationPhase` 쓰기), `MmInitSystem +0x53`·`+0x57`·`+0xa9`·`+0xac`·`+0xbd`·`+0xc5`·`+0xfd`·`+0x101`, `MiInitSystem +0x419`·`+0x497`/`+0x49c`·`+0x63f`·`+0x64a`·`+0x64d`, `Phase1InitializationIoReady +0x148`·`+0x155`·`+0x15b`/`+0x160`·`+0x22f`, `KiInitializeBootStructures +0x2aa`·`+0x2ad`/`+0x2b2`.
- 로그: 없음 (정적만, 동적 필수 아님).

## 결과

### 정적 — 갈래 (`ms/disassemble/ntoskrnl/MiInitSystem.asm`, 시작 `0x140c5bea0`)

| 자리 | 뜻 | 표시 |
|---|---|---|
| `+0x71 LEA EBX,[R14+0x1]` | EBX = 1 (`+0x25 XOR R14D,R14D` 뒤 R14·ECX 안 바뀜) | [정적] |
| `+0x75 TEST ECX,ECX` · `+0x77 JNZ 0x140c5c2c8` | ECX (= 위상) ≠ 0 이면 **`+0x428`** 로. ECX = 0 은 안 뛰고 `+0x7d` 로 떨어짐 | [정적] |
| `+0x7d ~ +0x427` | **위상 0 길.** 정상 끝 `+0x419 JNZ` → `+0x684` (공용 끝맺음), 실패 끝 `+0x422 MxInstallMoreMemory` · `+0x427 INT3`. 이 안에 `MiInitializeApiSets` 호출 없음 | [정적] |
| `+0x428 CMP ECX,EBX` · `+0x42a JNZ 0x140c5c4ea` | 들어오는 길은 `+0x77` 하나 (`+0x427` 이 `INT3`). ECX ≠ 1 이면 **`+0x64a`** 로 | [정적] |
| `+0x430 ~ +0x649` | **위상 1 길.** 안: `+0x497 MiSectionInitialization` (ret `+0x49c`) … `+0x638 MiInitializeApiSets` (ret `+0x63d`) → `+0x63f JNS` 성공이면 `+0x684`, 아니면 `MxInstallMoreMemory(0x2d)` · `+0x649 INT3` | [정적] |
| `+0x64a CMP ECX,2` · `+0x64d JNZ` | **위상 2 길** (`+0x64f ~ +0x683`) — ApiSet 없음 | [정적] |

- `+0x430 ~ +0x649` 안으로 들어오는 점프는 모두 같은 구간 안에서 출발한다 (결과서 1-1, 15개). 그래서 `+0x638` 은 **ECX = 1 일 때만** 닿는다.

### 정적 — `MmInitSystem` 갈래 (`MmInitSystem.asm`, 시작 `0x140bec990`, `+0x14 MOV R9D,ECX` = 위상)

| 위상 | 가르는 줄 | `MiInitSystem` 부르기 | 표시 |
|---|---|---|---|
| −1 | `+0x53 CMP R9D,-1` · `+0x57 JNZ` 안 뜀 | 안 부름 (`MiInitializeStrongCode`·`MiInitializeSystemVa` 뒤 `+0x149` 로) | [정적] |
| 0 | `+0xa9 TEST R9D,R9D` · `+0xac JNZ` 안 뜀 → `+0xb1 MiInitNucleus` → `+0xb6 XOR ECX,ECX` · `+0xb8 JMP +0x141` | `+0x144` / ret `+0x149`, ECX = 0 | [정적] |
| **1** | `+0xbd MOV EBX,1` · `+0xc2 CMP R9D,EBX` · `+0xc5 JNZ` 안 뜀 → … `+0xe3 MOV ECX,EBX` | **`+0xf0` / ret `+0xf5`, ECX = 1** | [정적] |
| 2 | `+0xfd CMP R9D,2` · `+0x101 JNZ` 안 뜀 → … `+0x13c MOV ECX,2` | `+0x144` / ret `+0x149`, ECX = 2 (위상 0 과 같은 자리) | [정적] |

- `MiInitSystem` 을 부르는 코드는 `+0xf0`·`+0x144` 둘뿐 (`_refs/MiInitSystem.tsv`·`_calls.tsv`, 남은 DATA 한 줄은 `.pdata` 칸).

### 정적 — `MmInitSystem` 을 부르는 곳 (`_refs/MmInitSystem.tsv`)

| caller | call | ret (= 콜스택 오프셋) | 위상 근거 | 위상 | 표시 |
|---|---|---|---|---|---|
| `nt!KiInitializeBootStructures` | `+0x2ad` | `+0x2b2` | `+0x2aa OR ECX,0xffffffff` | −1 | [정적] |
| `nt!InitBootProcessor` | `+0x595` | `+0x59a` | `+0x593 XOR ECX,ECX` | 0 | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xc65` | `+0xc6a` | `+0xc63 MOV ECX,EBX` ← `+0xa8f MOV EBX,1` (사이에 EBX 쓰기 없음) | **1** | [정적] |
| `nt!Phase1InitializationIoReady` | `+0x15b` | `+0x160` | `+0x155 MOV ECX,R12D` ← `+0x148 MOV R12D,2` | 2 | [정적] |

- `Phase1InitializationDiscard +0xc65` 를 건너뛰는 점프는 버그체크 길 셋뿐 (`KeBugCheck(0x61)`·`(0xa0)`) — 결과서 1-3.
- 커널 전역 `InitializationPhase` (`0x140fc37e0`) 와도 맞는다: `InitBootProcessor +0x178` 0 쓰기 · `Phase1InitializationDiscard +0xcb` 1 쓰기 · `Phase1InitializationIoReady +0x22f INC`.

### 정적 — 호출 지점 (맞는 사슬, `_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!InitBootProcessor` | `+0xbad` | `+0xbb2` | `nt!PsInitSystem(0)` (`+0xbab XOR ECX,ECX`) | [정적] (부팅) |
| `nt!PsInitSystem` | `+0x4c` | `+0x51` | `nt!PspInitPhase0` (`+0x6 JZ` → `+0x49`) | [정적] (부팅) |
| `nt!PspInitPhase0` | `+0xb8a` | `+0xb8f` | `nt!PsCreateSystemThread` (시작 함수 = `+0xb67 LEA` `0x140705b30` = `Phase1Initialization`) | [정적] (부팅) |
| `nt!Phase1Initialization` (시스템 스레드 시작 함수) | `+0x1e` | `+0x23` | `nt!Phase1InitializationDiscard` | [정적] (부팅) |
| `nt!Phase1InitializationDiscard` | `+0xc24` | `+0xc29` | `nt!PsInitSystem(1)` (`+0xc1b MOV ECX,EBX`) | [정적] (부팅) |
| `nt!Phase1InitializationDiscard` | `+0xc65` | `+0xc6a` | `nt!MmInitSystem(1)` | [정적] (부팅) |
| `nt!MmInitSystem` | `+0xf0` | `+0xf5` | `nt!MiInitSystem(1)` | [정적] (부팅) |
| `nt!MiInitSystem` | `+0x497` | `+0x49c` | `nt!MiSectionInitialization` (위상 1 길, ApiSet 앞) | [정적] (부팅) |
| `nt!MiInitSystem` | `+0x638` | `+0x63d` | `nt!MiInitializeApiSets(로더블록)` — 부르는 곳은 이 하나 (`_refs/MiInitializeApiSets.tsv`) | [정적] (부팅) |
| `nt!Phase1Initialization` | `+0x36` | `+0x3b` | `nt!IoInitSystem` (→ `IoInitSystemPreDrivers +0x10f5 PsLocateSystemDlls`, `Ⓑ1`) | [정적] (부팅, run48) |
| `nt!Phase1Initialization` | `+0x61` | `+0x66` | `nt!Phase1InitializationIoReady` (→ `+0x27 ExInitializeNls`, `ⒷN`) | [정적] (부팅, run57) |

- WinDbg 콜스택으로 보면 (ret): `MiInitializeApiSets` ← `MiInitSystem+0x63d` ← `MmInitSystem+0xf5` ← `Phase1InitializationDiscard+0xc6a` ← `Phase1Initialization+0x23`.

### 호출 관계

```
(부팅 — 흐름도 범위 앞, 단계 Ⓑ)
KiInitializeKernel +0x7fe → InitBootProcessor           위상 0 (+0x178 InitializationPhase = 0)
 ├─ +0x111  RtlNlsInitState                             커널 NLS 상태 = UTF-8 기본값   (ⒷA 위상 0 몫)
 ├─ +0x59a  MmInitSystem(0) → +0x149 MiInitSystem(0)    ApiSet 아님 (+0x7d ~ +0x427 길)
 └─ +0xbb2  PsInitSystem(0)
          └─ +0x51  PspInitPhase0
                   └─ +0xb8f  PsCreateSystemThread ─┐ 시작 함수 (+0xb67 LEA)
                                                    ▼
Phase1Initialization (시스템 스레드)                     위상 1 (Discard +0xcb InitializationPhase = 1)
 ├─ (1) +0x23  Phase1InitializationDiscard
 │            ├─ +0xc29  PsInitSystem(1)
 │            └─ +0xc6a  MmInitSystem(1)
 │                      └─ +0xf5  MiInitSystem(1)        +0x428 CMP ECX,EBX · +0x42a JNZ 안 뜀
 │                               ├─ +0x49c  MiSectionInitialization
 │                               └─ +0x63d  MiInitializeApiSets(로더블록)   (ⒷA 위상 1 몫, 안은 run57 그대로)
 │                                        │  +0xb1 · +0xb8 · +0xce   MiState.Sections.ApiSetSection · ApiSetSchema · ApiSetSchemaSize
 ├─ (2) +0x3b  IoInitSystem → … IoInitSystemPreDrivers +0x10fa PsLocateSystemDlls   (Ⓑ1)
 └─ (3) +0x66  Phase1InitializationIoReady
              ├─ +0x2c   ExInitializeNls                 (ⒷN)
              ├─ +0x160  MmInitSystem(2) → +0x149 MiInitSystem(2)
              ├─ +0x1da  PsInitSystem(2)                 (Ⓑ2)
              ├─ +0x1e7  MmInitSystemDll                 (Ⓑ3)
              └─ +0x228  StartFirstUserProcess           첫 유저 프로세스 (smss)
```

## 확인된 것

- `[정적]` **`MiInitializeApiSets` 는 부팅 위상 1** 에서, `Phase1Initialization` 시스템 스레드 안 `Phase1InitializationDiscard +0xc65 MmInitSystem(1)` → `MmInitSystem +0xf0` → `MiInitSystem +0x638` 길로 한 번 불린다 (결과서 1-1 ~ 1-4, 위 표들).
- `[정적]` 위상 0 의 `MmInitSystem(0)` (`InitBootProcessor +0x595`) → `MiInitSystem(0)` 은 ApiSet 을 만들지 않는다 (`+0x7d ~ +0x427` 안에 호출 없음).
- `[정적]` 위상 1 길은 `+0x497 MiSectionInitialization` 을 `+0x638` 보다 먼저 부른다 (코드 순서). "그래서 섹션 쪽 준비 뒤에야 `MmCreateSection` 이 돈다" 는 `[추측]`.
- `[정적]` 위상 1 안 순서: `Phase1InitializationDiscard` (ApiSet) → `IoInitSystem` (`Ⓑ1`) → `Phase1InitializationIoReady` (`ⒷN` → `Ⓑ2` → `Ⓑ3` → `StartFirstUserProcess`). 그래서 **`MiInitializeApiSets` < `PsLocateSystemDlls` < `ExInitializeNls`** 는 위상 0 이든 1 이든 같다 — run57·짐 지도의 앞뒤 말은 그대로 맞다.
- `[정적]` `RtlNlsInitState` (`InitBootProcessor +0x10c`, ret `+0x111`) 는 위상 0 이다. ApiSet 과 **같은 위상이 아니다**. 둘 다 `Ⓑ1` 보다 앞이라는 것만 같다.
- `[정적]` 전역 이름: `MiState` (PDB 공개 심볼 `S_PUB32`, rva `0xe2cb80`, `.data`) 는 `_MI_SYSTEM_INFORMATION` (`0xc1c0`B) 이고 `+0x1c0 Sections` = `_MI_SECTION_STATE` (`0xc00`B), 그 `+0xad8 ApiSetSection` · `+0xae0 ApiSetSchema` · `+0xae8 ApiSetSchemaSize`. `0xe2cb80 + 0x1c0 + 0xad8 = 0xe2d818` (820·828 도) — run57 의 쓰는 줄 `+0xb1`·`+0xb8`·`+0xce` 와 겹친다.
  - `MiState` 의 타입은 공개 PDB 가 직접 말해 주지 않는다. 결과서가 넷으로 맞춰 봤다: 다음 공개 심볼 `MiSystemPartition` 과의 거리 `0xc1c0` = 구조체 크기 · `MiInitSystem +0x222` 의 `+0xe00` = `SystemImages.SystemBase` · 같은 셈의 다른 칸 셋 (아래) · run48 의 `MiState+0x1000 Processes` 와 같은 기준.
  - "PDB 공개 이름 없음" 은 반만 맞다 — 세 주소에 공개 심볼은 없지만 **PDB 타입에 칸 이름**이 있다.
  - 칸 크기도 코드와 맞다: `ApiSetSchemaSize` 8B 를 `+0xc6 MOV ECX,[RAX+0xaa8]` (32비트 읽기) → `+0xce MOV [..828],RCX` (64비트 쓰기).
- `[정적]` (덤) 같은 셈으로 run63 의 ScpCfg 커널 전역 셋도 이름이 붙는다: `0xe2d8e0` = `Sections +0xba0 ImageViewExtensionOffsets` (12B) · `0xe2d8f0` = `+0xbb0 TotalImageViewReservationSize` · `0xe2d8f8` = `+0xbb8 ScpCfgPages` (32B). run63 이 적은 뜻 ("ScpCfg 오프셋 dword 표" · "확장 전체 크기" · "미리 만든 쪽 넷") 과 맞는다 (결과서 질문 2 의 3).
- `[정적]` `MiInitSystem`·`MiInitializeApiSets`·`MmInitSystem`·`Phase1InitializationDiscard`·`Phase1InitializationIoReady`·`InitBootProcessor` 는 PE 구역 `INIT` (`0xbec000 ~ 0xc8ec1c`) 안, `Phase1Initialization` 은 `PAGE` (구역표). 부팅 뒤 `INIT` 이 버려지는지는 `[추측]` (일반 지식).

## 정정

- **run57 "ApiSet 섹션은 부팅 위상 0"** → **위상 1.** run57 은 `MiInitSystem +0x77 JNZ` 의 목적지 `+0x428` 을 "위상 0 쪽" 으로 거꾸로 읽고, 위상 0 의 자리 `InitBootProcessor +0x595` → `MmInitSystem +0x144` 에 이어 붙였다 (`[정적]`, 결과서 1-5). `MmInitSystem` 의 위상 1 자리 `+0xf0` 이 빠졌다.
  - run57 에서 맞는 것: `MmInitSystem +0x144/+0x149` 가 위상 0·2 의 같은 자리라는 것, `MiInitializeApiSets` 안 (`+0x52`·`+0x73`·`+0xac`·`+0xb1`·`+0xb8`·`+0xce`), `MmMapApiSetView +0x92`, `ExInitializeNls` 쪽 전부.
  - 고친 곳: `run57-nls-apiset.md` (맨 위 한 줄 + 34·35·148·149·150·그림·229·252줄 쯤), `run57-nls-apiset-해설.md` (맨 위 한 줄 + 해당 줄에 짧은 정정 표시), flow.md 2.1 `MmMapApiSetView` 절 "부팅 때" · 4장 · 5장 run57 줄, baggage.md 쓰는 말 `ⒷA` · 3장 줄임 표 · 5장 `Ⓑ` · 7.6.3.
- **run57 "전역 셋은 PDB 공개 이름 없음"** → 공개 심볼은 없고 **타입 이름이 있다** (`MiState.Sections.ApiSetSection` 등).
- **`ⒷA` 묶음** (run57, 짐 지도) 은 위상 0 (`RtlNlsInitState`) 과 위상 1 (ApiSet) 을 한 덩어리로 "위상 0 `InitBootProcessor` 안" 이라 적었다 → 짐 지도에서 둘로 나눔. 줄임 이름 `ⒷA` 와 7.6.3 짐 수는 그대로 두고, 위상을 줄마다 적었다.
- 결과서와 다르게 적은 곳: 결과서 "고칠 줄 목록" 의 flow.md 4683줄 안내는 "(run65 정정)" 이지만 이 문서가 **run64** 라 "run64 정정" 으로 적었다 (요청서도 "예정 run65" — run 번호는 `next_index.py` 로 64).
- 안 고친 기록 (오케스트레이터 몫): `_workspace/sast/52_nls-apiset_result.md` 163·249줄 쯤, `_workspace/plan.md` Q108 끝 줄 — 그때의 기록이라 그대로.

## 아직 확인 필요

- **동적은 필수 아님** — `CMP`/`JNZ` 와 점프 목적지 표로 정해져 실행에서 뒤집힐 데가 없다 (결과서).
- (선택, 낮은 우선순위) 부팅 초기 KDNET 첫 멈춤이 있어야 한다 (`INIT` 구역 코드라 부팅 뒤엔 못 걺 `[추측]`):
  - `bp nt!MiInitSystem` → 세 번, `r ecx` 0 → 1 → 2 (ret `MmInitSystem+0x149` 두 번, `+0xf5` 한 번).
  - `bp nt!MiInitializeApiSets` → `k` 가 `MiInitSystem+0x63d` ← `MmInitSystem+0xf5` ← `Phase1InitializationDiscard+0xc6a` ← `Phase1Initialization+0x23`.
- 이미 있는 Q110 (2) `dq nt+0xe2d818 L3` 에 이름 확인 덤: `dt nt!_MI_SECTION_STATE ApiSet* (nt!MiState+0x1c0)` 한 줄로 같은 세 칸 (위상과 무관).

## 도구 메모

- `.asm` 의 전역 주석이 `0xe2d818` 등에 `MiState+0xc98` 같은 꼬리를 안 다는 건 스크립트 고장이 아니다 — `ExportFunctions.java` 가 심볼 앞 `0x100` 안에서만 이름을 붙인다 (설계, 결과서 질문 2). 주석이 없다고 "PDB 이름 없음" 으로 읽지 말고 `pdb_struct.py` 로 큰 구조체 전역 (`MiState` 등) 의 칸을 셀 것.
- 이번 실수 꼴 (점프 목적지를 거꾸로 / 끝자리 잘못 읽기) 은 run51·run56 ~ run61 과 같다 (요청서). "점프 목적지 = 주소 − 함수 시작" 을 결과서에 적게 한 규칙이 이번에 잡았다.

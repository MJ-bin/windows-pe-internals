# 60 apiset-phase — 정적 결과서

- 날짜: 2026-11-05
- 요청서: `_workspace/sast/60_apiset-phase.md` (Q120c)
- 판정: **분석성공.** 질문 1~3 모두 줄 근거와 함께 답했다.
- 방법: **Ghidra 를 열지 않았다** (analyze·export·map·refs 0번). 이미 있는 `ms/disassemble/ntoskrnl/*.asm`·`_calls.tsv`·`_refs/` 와 PDB 타입 (`pdb_struct.py`, `ms/sym/ntoskrnl.exe.pdb`) 만 썼다. 시작할 때 `pgrep -fa analyzeHeadless` = 도는 것 없음.
- 스크립트 문제: 없음.
- 버전 대조: **일치.** PDB `guid_age=C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`). `_calls.tsv` 로 알려진 사실 둘 확인 — `NtCreateUserProcess` ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` ret `+0x807` → `PspCallProcessNotifyRoutines`.
- 오프셋 대조: 아래 점프 목적지는 전부 "목적지 − 함수 시작" 을 스크립트로 한 번 더 셌고 (MiInitSystem 점프 42개 · MmInitSystem 점프 11개 전부), call/ret 는 `.asm` 줄과 `_calls.tsv` 로 맞췄다. 줄 번호는 `.asm` 파일 안 줄.

## 한 줄 답

| # | 답 |
|---|---|
| 1 | **run57 의 "위상 0" 은 틀렸다 (아니오). 위상 1 이다.** `+0x77 JNZ` 는 ECX ≠ 0 일 때 `+0x428` 로 간다 (run57 이 반대로 읽음). `+0x638 CALL MiInitializeApiSets` 는 `ECX == 1` 갈래 (`+0x430 ~ +0x649`) 안이다. 맞는 사슬: `Phase1Initialization +0x1e` → `Phase1InitializationDiscard +0xc65` → `MmInitSystem +0xf0` → `MiInitSystem +0x638` → `MiInitializeApiSets`. `InitBootProcessor +0x595 MmInitSystem(0)` 은 이 일과 상관없다. PE_emulator 쪽 읽기 (`Discard +0xc65`·`MmInitSystem +0xf0` = 위상 1) 가 맞다. |
| 2 | 전역 이름 = `MiState` (rva `0xe2cb80`) `+0x1c0 Sections` (`_MI_SECTION_STATE`) `+0xad8 ApiSetSection` (`void*`) · `+0xae0 ApiSetSchema` (`void*`) · `+0xae8 ApiSetSchemaSize` (`unsigned __int64`). 셈 `0xe2cb80 + 0x1c0 + 0xad8 = 0xe2d818` 맞다 (820·828 도). "PDB 공개 이름 없음" 은 반쪽만 맞다: 세 주소에 공개 심볼은 없지만 PDB **타입**에 칸 이름이 있다. |
| 3 | 위상을 바탕으로 쓴 말 중 틀린 것: ① "`InitBootProcessor` 안" (사슬 통째) ② "`RtlNlsInitState` 와 같은 위상" 으로 읽히는 `ⒷA` 묶음 (NLS 상태는 위상 0, ApiSet 은 위상 1). **안 틀리는 것**: "`Ⓑ1` (`PsLocateSystemDlls`) 보다 앞" · "`ExInitializeNls` (`ⒷN`) 보다 앞" · "PE_emulator 순서에 영향 없음" — 위상 1 이어도 `Phase1InitializationDiscard` 가 `IoInitSystem`·`Phase1InitializationIoReady` 보다 먼저라 순서는 그대로다. |

## 질문 1 — 위상 몇 번째에서 불리나

### 1-1. `MiInitSystem` 안 갈래 (함수 시작 `0x140c5bea0`, rva `0xc5bea0`)

| 자리 | 줄 | 뜻 |
|---|---|---|
| `+0x25 XOR R14D,R14D` | 12 | R14 = 0. `+0x25 ~ +0x71` 사이에 R14·ECX 를 쓰는 명령 없음 |
| `+0x71 LEA EBX,[R14 + 0x1]` | 27 | **EBX = 1** (예, 1 맞다) |
| `+0x75 TEST ECX,ECX` | 28 | ECX = 첫 인자 = 위상 (`MmInitSystem` 이 `MOV ECX,…` 로 넘김, 아래 1-2) |
| `+0x77 JNZ 0x140c5c2c8` | 29 | **ECX ≠ 0 이면 점프.** 목적지 `0x140c5c2c8 − 0x140c5bea0 =` **`+0x428`**. 즉 ECX = 0 이 점프 **안** 하고 `+0x7d` 로 떨어진다 |
| `+0x7d AND [MmTrackLockedPages],EBX` | 30 | **위상 0 길 시작** (`+0x7d ~ +0x427`) |
| `+0x419 JNZ 0x140c5c524` | 249 | 위상 0 길의 정상 끝 → 목적지 `+0x684` (공용 끝맺음, `__security_check_cookie` 후 `RET`) |
| `+0x422 CALL MxInstallMoreMemory` · `+0x427 INT3` | 251·252 | 위상 0 길의 실패 끝 (돌아오지 않음) |
| `+0x428 CMP ECX,EBX` | 253 | `+0x428` 로 들어오는 점프는 `+0x77` 하나뿐 (점프 목적지 표에서 `0x428` 은 `+0x77` 하나). 바로 앞 `+0x427` 은 `INT3` 라 떨어져 들어오는 길 없음. EBX 는 `+0x71` 이후 이 길에서 안 바뀜 → **ECX 를 1 과 비교** |
| `+0x42a JNZ 0x140c5c4ea` | 254 | ECX ≠ 1 이면 점프. 목적지 `0x140c5c4ea − 0x140c5bea0 =` **`+0x64a`** |
| `+0x430 MOV RAX,GS:[0x188]` | 255 | **위상 1 길 시작** (`+0x430 ~ +0x649`) |
| `+0x638 CALL MiInitializeApiSets` (ret `+0x63d`) | 369 | **위상 1 길 안.** 이 자리로 들어오는 점프 없음 (아래), 바로 앞 `+0x635 MOV RCX,RSI` · `+0x62e ADD.LOCK` · `+0x625 CALL MiEnumerateSystemImages` 에서 떨어져 옴 |
| `+0x63f JNS 0x140c5c524` | 371 | 성공이면 `+0x684` 로 (`MiInitializeApiSets` 반환값 ≥ 0) |
| `+0x641 LEA ECX,[RBP+0x2e]` ~ `+0x649 INT3` | 372~374 | 실패면 `MxInstallMoreMemory(0x2d)` 를 부르고 `INT3` (돌아오지 않는 실패 길; RBP = −1 이라 `ECX = 0x2d`) |
| `+0x64a CMP ECX,0x2` · `+0x64d JNZ 0x140c5c524` | 375·376 | **위상 2 길** (`+0x64f ~ +0x683`): `MiRegisterForHardwareAcceleratorChanges` (ret `+0x669`) · `MiEnumerateSystemImages` (ret `+0x67a`) · `MiFlushStrongCodeDriverLoadFailures` (ret `+0x67f`) · `MiFlushDeferredRetpolineImageLoadEvents` (ret `+0x684`) |

**`+0x638` 이 위상 1 갈래 안인 근거 (점프 따라가기):**

- `MiInitSystem` 의 점프는 42개. 이 중 목적지가 `+0x430 ~ +0x649` 안인 것은 전부 **출발점도 같은 구간 안**이다: `+0x473→+0x497` · `+0x48a→+0x497` · `+0x49e→+0x4a9` · `+0x4b0→+0x4bd` · `+0x4c7→+0x4d2` · `+0x4ef→+0x4fa` · `+0x503→+0x50c` · `+0x50a→+0x512` · `+0x50e→+0x512` · `+0x526→+0x531` · `+0x533→+0x54b` · `+0x549→+0x555` · `+0x5cd→+0x619` · `+0x5fc→+0x60e` · `+0x60c→+0x5ca`(되돌이). 위상 0 구간 (`+0x7d ~ +0x427`) 에서 밖으로 나가는 점프는 `+0x419 → +0x684` 하나뿐이다.
- 그래서 `+0x638` 에 오려면 `+0x77` 에서 ECX ≠ 0 → `+0x428` 에서 ECX = EBX = 1 이어야 한다. **ECX = 1 일 때만.**
- 참고 (갈래 안 일, `[정적]`): 위상 1 길은 `+0x497 MiSectionInitialization` (ret `+0x49c`) 를 `+0x638` 앞에서 부른다. `MiInitializeApiSets` 안 `+0x52 MmCreateSection` 이 섹션 쪽 준비가 끝난 뒤에 도는 순서다. (그래서 위상 0 에서는 못 돌 것 — 이 문장은 `[추측]`, 코드 순서만 `[정적]`.)

### 1-2. `MmInitSystem` 갈래와 `MiInitSystem` 을 부르는 자리 (시작 `0x140bec990`, rva `0xbec990`)

`MmInitSystem(위상, 로더블록)`: `+0x14 MOV R9D,ECX` (줄 7) 로 위상을 R9D 에 둔다.

| 위상 (R9D) | 가르는 줄 | 이 갈래에서 | `MiInitSystem` 을 부르는 call / ret · 넘기는 ECX |
|---|---|---|---|
| **−1** | `+0x53 CMP R9D,-1` · `+0x57 JNZ 0x140beca39` (= `+0xa9`) 안 점프 | `+0x7d MiInitializeStrongCode` (ret `+0x82`) · `+0x9f MiInitializeSystemVa` (ret `+0xa4`) → `+0xa4 JMP 0x140becad9` (= `+0x149` 끝) | **안 부른다** |
| **0** | `+0xa9 TEST R9D,R9D` · `+0xac JNZ 0x140beca4d` (= `+0xbd`) **안** 점프 | `+0xb1 MiInitNucleus` (ret `+0xb6`) → `+0xb6 XOR ECX,ECX` → `+0xb8 JMP 0x140becad1` (= `+0x141`) | `+0x141 MOV RDX,RDI` → **`+0x144 CALL MiInitSystem` (ret `+0x149`), ECX = 0** |
| **1** | `+0xbd MOV EBX,1` · `+0xc2 CMP R9D,EBX` · `+0xc5 JNZ 0x140beca8d` (= `+0xfd`) **안** 점프 | `+0xc7 MiComputeNumaCosts` (ret `+0xcc`) · `+0xcc OR.LOCK MiFlags,0x10` · … · `+0xe3 MOV ECX,EBX` | **`+0xf0 CALL MiInitSystem` (ret `+0xf5`), ECX = 1** → `+0xf5 MOV [0x140e37654],BL` → `+0xfb JMP +0x149` |
| **2** | `+0xfd CMP R9D,2` · `+0x101 JNZ 0x140becad9` (= `+0x149`) **안** 점프 | `+0x103 ~ +0x13a` `MiFlags` 비트 바꾸기 (`CMPXCHG.LOCK` 되풀이) → `+0x13c MOV ECX,0x2` | `+0x141 MOV RDX,RDI` → **`+0x144 CALL MiInitSystem` (ret `+0x149`), ECX = 2** (위상 0 과 같은 call 자리) |

- `MiInitSystem` 을 부르는 코드는 **이 둘 (`+0xf0`, `+0x144`) 뿐**이다 (`_refs/MiInitSystem.tsv` 와 `_calls.tsv`; `(함수 밖) DATA` 한 줄 rva `0x14e250` 은 PE `.pdata` 표 안 칸 `[정적]` — 구역표 `.pdata` `0xe0000 ~ 0x14f108` 안이다. 호출 아님).
- run57 이 맞게 쓴 것: "위상 0·2 가 같은 자리 (`+0x144`/`+0x149`), `+0xb6 XOR ECX,ECX` / `+0x13c MOV ECX,2`" — 이건 맞다. **빠뜨린 것**: 위상 1 의 자리 `+0xf0`. run57 은 `+0x77 JNZ` 를 "위상 0 쪽" 이라 읽고 `+0x144` 에서 이어 붙였다.

### 1-3. `MmInitSystem` 을 부르는 곳과 넘기는 위상

`_refs/MmInitSystem.tsv` 4곳 (+ `.pdata` 한 줄). `MmInitSystemDll` 은 다른 함수다.

| 부르는 곳 | call / ret | 위상 근거 줄 | 위상 |
|---|---|---|---|
| `KiInitializeBootStructures` | `+0x2ad` / `+0x2b2` | `+0x2aa OR ECX,0xffffffff` (줄 144) | **−1** |
| `InitBootProcessor` | `+0x595` / `+0x59a` | `+0x593 XOR ECX,ECX` (줄 312) | **0** |
| `Phase1InitializationDiscard` | `+0xc65` / `+0xc6a` | `+0xc63 MOV ECX,EBX` (줄 753) ← `+0xa8f MOV EBX,0x1` (줄 640). `+0xa8f ~ +0xc63` 에 EBX 를 쓰는 명령 없음 (`+0xa94`·`+0xaa2`·`+0xabf`·`+0xb03`·`+0xc1b` 은 읽기) | **1** |
| `Phase1InitializationIoReady` | `+0x15b` / `+0x160` | `+0x155 MOV ECX,R12D` ← `+0x148 MOV R12D,0x2` (줄 83·85) | **2** |

- `Phase1InitializationDiscard +0xc65` 는 **언제나 지나간다**: 이 함수의 점프 145개 중 `+0xc65` 를 건너뛰는 것은 `+0x36c`·`+0x99a → +0x1412` (`KeBugCheck(0x61)`), `+0x63f → +0x1407` (`KeBugCheck(0xa0)`) 셋뿐이고 전부 버그체크 길이다. `+0xa8f` 뒤로 밖에서 뛰어들어오는 점프는 없다.
- `InitializationPhase` (커널 전역 `0x140fc37e0`) 와 맞춰 보면: `InitBootProcessor +0x178` 0 쓰기 (줄 85, R13D = 0) · `Phase1InitializationDiscard +0xcb` 1 쓰기 (줄 50) · `Phase1InitializationIoReady +0x22f INC` (줄 138). `MmInitSystem(1)` 때 이 전역은 1 이다. (`MmInitSystem(2)` 호출 `+0x15b` 은 `INC` `+0x22f` 보다 앞이라 그때 전역은 아직 1 — 인자 2 와 전역 값은 위상 2 에서만 어긋난다. 이번 질문엔 영향 없음.)

### 1-4. 올바른 사슬 (call / ret 둘 다)

`Phase1Initialization` 은 `PspInitPhase0 +0xb67 LEA RAX,[0x140705b30]` 로 주소를 잡아 `+0xb8a CALL PsCreateSystemThread` (ret `+0xb8f`) 의 시작 함수 자리에 넘기는 **시스템 스레드 시작 함수**다 (`PspInitPhase0.asm` 줄 585·593). 그 스레드를 만드는 길: `InitBootProcessor +0xbad CALL PsInitSystem(0)` (ret `+0xbb2`, `+0xbab XOR ECX,ECX`) → `PsInitSystem +0x4c CALL PspInitPhase0` (ret `+0x51`, `+0x6 JZ 0x140becda1` = `+0x49`).

| 단계 | caller | call | ret | callee |
|---|---|---|---|---|
| 스레드 시작 | (시스템 스레드) | — | — | `Phase1Initialization` (`0x140705b30`, rva `0x705b30`) |
| 1 | `Phase1Initialization` | `+0x1e` | `+0x23` | `Phase1InitializationDiscard` |
| 2 | `Phase1InitializationDiscard` | `+0xc65` | `+0xc6a` | `MmInitSystem(1)` |
| 3 | `MmInitSystem` | `+0xf0` | `+0xf5` | `MiInitSystem(ECX = 1)` |
| 4 | `MiInitSystem` | `+0x638` | `+0x63d` | `MiInitializeApiSets(로더블록)` |

- WinDbg 콜스택으로 보면 (ret 기준): `MiInitializeApiSets` ← `MiInitSystem+0x63d` ← `MmInitSystem+0xf5` ← `Phase1InitializationDiscard+0xc6a` ← `Phase1Initialization+0x23`.
- `MiInitSystem`·`MiInitializeApiSets`·`MmInitSystem`·`Phase1InitializationDiscard`·`Phase1InitializationIoReady`·`InitBootProcessor` 모두 PE 구역 `INIT` (`0xbec000 ~ 0xc8ec1c`) 안 코드다 (`Phase1Initialization` 은 rva `0x705b30` 이라 구역 `PAGE` `0x6fc000 ~ 0xb58660`). 부팅 때만 쓰는 코드다 `[정적]` (구역표). 부팅 뒤 버려지는 구역인지는 `[추측]` (일반 지식).
- **`MiInitializeApiSets` 를 부르는 곳은 `MiInitSystem +0x638` 하나** (`_refs/MiInitializeApiSets.tsv`).

### 1-5. 결론 (예/아니오)

- run57 "`MiInitializeApiSets` 는 부팅 위상 0 (`InitBootProcessor +0x595 MmInitSystem(0)` → `MmInitSystem +0x144 MiInitSystem` → `+0x638`)": **아니오, 틀렸다.** 위상 1 이다.
- 틀린 이유 (`[정적]`): `+0x77 JNZ` 의 목적지 `+0x428` 을 "위상 0 쪽" 으로 읽었다. 실제로는 점프 안 하는 쪽 (`+0x7d ~ +0x427`) 이 위상 0 이고, `+0x638` 은 점프하는 쪽 안에서도 `+0x42a JNZ` 가 안 뛰는 쪽 (`+0x430 ~ +0x649`) 이다. 주소 끝자리 읽기 실수 (요청서 말대로 run51·run56~61 과 같은 꼴).
- run57 의 나머지 (`MiInitializeApiSets` 본문 · 전역 세 칸에 쓰는 줄 · PEB `ApiSetMap` 쓰는 `MmMapApiSetView +0x92`) 는 이번에 건드리지 않았고, 이 정정과 무관하다.

## 질문 2 — 전역 칸 이름

| 항목 | 값 | 근거 |
|---|---|---|
| `MiState` | rva **`0xe2cb80`** (`0x140e2cb80`), 구역 `.data` (`0xe00000 ~ 0xfc4408`) | PDB 공개 심볼 `S_PUB32 MiState` (`llvm-pdbutil dump -publics`), `pdb_aliases.py --where ntoskrnl.exe MiState` → `rva=0xe2cb80`. `MiInitSystem +0x20f LEA RCX,[0x140e2cb80] ; MiState` (줄 132) |
| `_MI_SYSTEM_INFORMATION` | sizeof **`0xc1c0`**, `+0x1c0 Sections` (`_MI_SECTION_STATE`) | `pdb_struct.py ntoskrnl.exe _MI_SYSTEM_INFORMATION` |
| `_MI_SECTION_STATE` | sizeof `0xc00`, `+0xad8 ApiSetSection` (`void*`) · `+0xae0 ApiSetSchema` (`void*`) · `+0xae8 ApiSetSchemaSize` (`unsigned __int64`) | `pdb_struct.py ntoskrnl.exe _MI_SECTION_STATE` |

**셈**: `0xe2cb80 + 0x1c0 = 0xe2cd40`, `+ 0xad8 = 0xe2d818` (섹션) · `+ 0xae0 = 0xe2d820` (커널 뷰) · `+ 0xae8 = 0xe2d828` (크기). **맞다.** 쓰는 줄과 겹친다: `MiInitializeApiSets +0xb1/+0xb8/+0xce` 가 각각 `[0x140e2d818]`·`[…820]`·`[…828]` 에 쓴다 (`.asm` 줄 44·45·49, `_refs/0xe2d818.tsv`).

- **PDB 가 변수 `MiState` 의 타입을 직접 말해 주진 않는다.** 공개 PDB 에는 `MiState` 가 `S_PUB32` (이름 + 주소) 만 있고 타입이 없다. `MiState = _MI_SYSTEM_INFORMATION` 이라는 건 아래 맞춰 보기로 세운 것이다 (`[정적]`, 넷 다 맞음):
  1. 다음 공개 심볼 `MiSystemPartition` = `0xe38d40`. `0xe38d40 − 0xe2cb80 = 0xc1c0` = `sizeof(_MI_SYSTEM_INFORMATION)`. (공개 심볼 이웃: `HvSymcryptSeed` `0xe2cb60` → `MiState` → `MiSystemPartition` `0xe38d40`. `0xe2cb80 ~ 0xe38d40` 사이에 다른 공개 심볼 없음.)
  2. `MiInitSystem +0x20f` (RCX = `MiState`) → `+0x222 MOV RAX,[RCX + RAX*8 + 0xe00]`: `0xe00 = SystemImages (+0xdc0) + _MI_SYSTEM_IMAGE_STATE.SystemBase (+0x40)`. 이 칸은 8바이트 하나 (`?[8B]`) 이고 코드 반복도 `EDI < EBX(=1)` 이라 한 번만 돈다 — 맞는다.
  3. 같은 식으로 센 다른 전역: `MiState + 0x1c0 + 0xba0 = 0xe2d8e0` = `ImageViewExtensionOffsets` (12B), `+0xbb0 = 0xe2d8f0` = `TotalImageViewReservationSize`, `+0xbb8 = 0xe2d8f8` = `ScpCfgPages` (32B). baggage.md 1995줄 (run63) 이 이 셋을 "ScpCfg 오프셋 dword 표" · "확장 전체 크기 `[0xe2d8f0]`" · "미리 만든 쪽 넷 `DAT_140e2d8f8`" 로 이름 모른 채 적은 것과 뜻이 맞는다. (덤으로: 그 줄도 PDB 이름을 달 수 있다.)
  4. run48 이 이미 쓰는 `MiState+0x1000` = `_MI_SYSTEM_INFORMATION.Processes` (`_MI_PROCESS_STATE`) 와 같은 기준이다.
- "PDB 공개 이름 없음" 은 **세 주소 자체엔 공개 심볼이 없다는 뜻으로는 맞다** (`0xe2cb80` 다음 공개 심볼은 `0xe38d40`). 하지만 "PDB 에 이름이 없다" 로 읽히게 쓴 건 틀리다: **PDB 타입에 칸 이름이 있다.** sast/52 는 `MmApiSetSection` 같은 추측 이름만 찾아 봤고 타입 쪽은 안 봤다.
- 칸 크기도 코드와 맞다: 크기 칸 `ApiSetSchemaSize` 는 8바이트인데 `MiInitializeApiSets +0xc6 MOV ECX,[RAX+0xaa8]` (32비트 읽기, 상위 0 으로 채워짐) → `+0xce MOV [0x140e2d828],RCX` (64비트 쓰기) 로 쓴다.
- `.asm` 전역 주석이 이 세 칸에 `MiState+0xc98` 같은 꼬리를 안 단 건 스크립트 문제가 아니다. `ExportFunctions.java` 142~155줄 주석대로 앞쪽 `0x100` 안에서만 이름을 붙이고, `0xc98` 은 멀다 (설계). 이 사실을 몰랐다면 `ms/disassemble/.../MiInitializeApiSets.asm` 줄 44·45·49 에 주석이 없다고 "이름 없음" 이라 읽을 수 있다 — 같은 실수 방지용으로 적어 둔다.

## 질문 3 — 위상이 바뀌면 바뀌는 것

### 3-1. 위상 1 안 순서 (call / ret, 한 스레드 `Phase1Initialization` 안)

| 순서 | caller | call | ret | callee | 비고 |
|---|---|---|---|---|---|
| ① | `Phase1Initialization` | `+0x1e` | `+0x23` | `Phase1InitializationDiscard` | |
| ①-a | `Phase1InitializationDiscard` | `+0xc24` | `+0xc29` | `PsInitSystem(1)` | 같은 EBX = 1 (`+0xc1b MOV ECX,EBX`) |
| ①-b | `Phase1InitializationDiscard` | `+0xc65` | `+0xc6a` | **`MmInitSystem(1)`** → `MiInitSystem(1)` → **`MiInitializeApiSets`** (`MiInitSystem +0x638`) | **ApiSet 섹션 만들기** |
| ② | `Phase1Initialization` | `+0x36` | `+0x3b` | `IoInitSystem` | |
| ②-a | `IoInitSystem` | `+0x12` | `+0x17` | `IoInitSystemPreDrivers` | |
| ②-b | `IoInitSystemPreDrivers` | `+0x10f5` | `+0x10fa` | **`PsLocateSystemDlls`** (`Ⓑ1`) | |
| ③ | `Phase1Initialization` | `+0x61` | `+0x66` | `Phase1InitializationIoReady` | |
| ③-a | `Phase1InitializationIoReady` | `+0x27` | `+0x2c` | **`ExInitializeNls`** (`ⒷN`) | |
| ③-b | `Phase1InitializationIoReady` | `+0x15b` | `+0x160` | `MmInitSystem(2)` | |
| ③-c | `Phase1InitializationIoReady` | `+0x1d5` | `+0x1da` | `PsInitSystem(2)` (`Ⓑ2`) | |
| ③-d | `Phase1InitializationIoReady` | `+0x1e2` | `+0x1e7` | `MmInitSystemDll` (`Ⓑ3`) | |
| ③-e | `Phase1InitializationIoReady` | `+0x223` | `+0x228` | `StartFirstUserProcess` | 첫 유저 프로세스 (smss) 만들기 — ApiSet 섹션보다 한참 뒤 |

(`Phase1Initialization.asm` 줄 10·16·27, 점프는 `+0x3d JNS 0x140705b8b` (= `+0x5b`) 하나로 `IoInitSystem` 실패 때만 건너뛰어 버그체크 `+0x55 KeBugCheckEx`. `Phase1InitializationDiscard +0xc65` 는 위 1-3 대로 언제나 지나감. `Phase1InitializationIoReady` 안 `+0x15b` 를 건너뛰는 점프는 없다.)

→ **`MiInitializeApiSets` (①-b) < `PsLocateSystemDlls` (②-b) < `ExInitializeNls` (③-a)** — 코드 순서로 위상 1 이든 위상 0 이든 앞뒤는 같다. 그래서 PE_emulator 원문 "`PsLocateSystemDlls` 보다 앞이라 순서엔 영향 없음" 은 그대로 맞다.

### 3-2. 위상 0 에서 일어나는 것과의 관계

- `RtlNlsInitState` 는 `InitBootProcessor +0x10c` (ret `+0x111`) → **위상 0 쪽** 이다 (`InitBootProcessor +0x178` 이 `InitializationPhase = 0`). 즉 NLS 상태 처음 값은 위상 0, ApiSet 섹션은 위상 1. **같은 위상이 아니다.** 둘 다 `Ⓑ1` 보다 앞이라는 것만 같다.
- 위상 0 의 `MmInitSystem(0)` (`InitBootProcessor +0x595`) → `MiInitSystem(0)` 은 ApiSet 을 만들지 않는다 (위 1-1 의 `+0x7d ~ +0x427` 안에 `MiInitializeApiSets` 호출 없음: `_calls.tsv` 에서 `MiInitSystem` 의 `MiInitializeApiSets` 호출은 `+0x638` 하나뿐).
- `RtlNlsInitState` → `MmInitSystem(0)` 의 순서 (`InitBootProcessor +0x10c` < `+0x595`) 와 ApiSet 의 순서는 서로 상관없다.

### 3-3. 요청서가 물은 말들 판정

| 말 | 위치 | 판정 |
|---|---|---|
| "`MiInitializeApiSets` 는 부팅 위상 0" | run57:34·229·150, flow:1410·4683, baggage:36·283·1388·1948 | **틀림** → 위상 1 |
| 사슬 `InitBootProcessor → MmInitSystem (+0x595) → MiInitSystem (+0x144) → MiInitializeApiSets (+0x638)` | run57:34·149·162~164·(해설), baggage:283·1948·1388, flow:1410 | **틀림** → 1-4 의 사슬 |
| "`MmInitSystem` 위상 0·2 가 같은 자리 (`+0x144`)" | run57:149 | **맞음** (단, 위상 1 의 자리 `+0xf0` 이 빠졌다) |
| "`ⒷA` = `InitBootProcessor` 안 (`RtlNlsInitState` + `MmInitSystem(0)…`)" | baggage:36·1388 | 반만: `RtlNlsInitState` 쪽은 맞고 (위상 0, `InitBootProcessor +0x10c`), `MmInitSystem(0)` 뒤쪽 사슬은 틀림 |
| "`Ⓑ1` 보다 **앞**" | baggage:36·283·1388 | **맞음** (위상 1 이라 `Discard` 가 `IoInitSystem` 보다 먼저) |
| "`ⒷN` 은 코드 순서로 `Ⓑ1` 뒤 · `Ⓑ2` 앞" | baggage:36·284·1390 | **맞음** (위 3-1: ②-b `+0x10f5` < ③-a `+0x27` < ③-c `+0x1d5`) |
| "`RtlNlsInitState` 와 같은 위상" (그렇게 읽히는 `ⒷA` 묶음) | baggage:36·283·1388·1955 | **틀림** — NLS 상태 = 위상 0, ApiSet = 위상 1 |
| `ExInitializeNls` 와의 순서 | run57 5장·baggage | 안 틀림: ApiSet 이 앞 |
| `PsLocateSystemDlls` 보다 앞 | PE_emulator 원문 | 안 틀림 |
| "PDB 공개 이름 없음" (전역 세 칸) | run57:35, 해설:216, flow:1410, baggage:1956 의 "PDB 이름 없음" | **반만 맞음** → 질문 2 |

## 고칠 줄 목록 (줄 번호는 2026-11-05 지금 기준 — 다른 일로 밀리면 큰따옴표 안 말로 grep)

### `runs/03_sprint/run57-nls-apiset.md`

| 줄 | 지금 | 고칠 말 |
|---|---|---|
| 34 | "부팅 위상 0: `InitBootProcessor +0x595 MmInitSystem(0)` → `MmInitSystem +0x144 MiInitSystem` → **`MiInitSystem +0x638`** (ret `+0x63d`)" | "**부팅 위상 1**: `Phase1Initialization +0x1e` → `Phase1InitializationDiscard +0xc65 MmInitSystem(1)` → `MmInitSystem +0xf0 MiInitSystem(1)` → **`MiInitSystem +0x638`** (ret `+0x63d`)" |
| 35 | "PDB 공개 이름 없음." | "PDB 공개 심볼은 없다. 칸 이름은 PDB 타입: `MiState` (rva `0xe2cb80`) `+0x1c0 Sections` → `_MI_SECTION_STATE` `+0xad8 ApiSetSection` · `+0xae0 ApiSetSchema` · `+0xae8 ApiSetSchemaSize` (sast/60)." |
| 148 | `InitBootProcessor +0x10c · +0x595` → `RtlNlsInitState · MmInitSystem` | 줄은 맞음. 뒤에 "(`+0x595` 는 `MmInitSystem(0)` — ApiSet 길 아님)" 덧붙임 |
| 149 | "`nt!MmInitSystem` `+0x144` `+0x149` → `MiInitSystem` (위상 0·2 가 같은 자리, …)" | 줄 앞뒤는 맞음. "위상 0·2" 앞에 "위상 1 은 `+0xf0` / `+0xf5` (이 길이 ApiSet)" 덧붙이고, 새 줄 둘 추가: `Phase1Initialization` `+0x1e`/`+0x23` → `Phase1InitializationDiscard`, `Phase1InitializationDiscard` `+0xc65`/`+0xc6a` → `MmInitSystem(1)` |
| 150 | "`nt!MiInitSystem` `+0x638` `+0x63d` → `MiInitializeApiSets` (위상 0 쪽, `+0x75 TEST ECX` / `+0x77 JNZ`)" | "(위상 1 쪽: `+0x77 JNZ` 는 ECX ≠ 0 이면 `+0x428` 로 → `+0x42a JNZ +0x64a` 가 ECX = 1 에서 안 뜀 → `+0x430 ~ +0x649`)" |
| 160~164 (그림) | `InitBootProcessor` 아래 `+0x59a MmInitSystem(0)` → `+0x149 MiInitSystem(0)` → `+0x63d MiInitializeApiSets` | `InitBootProcessor` 아래엔 `+0x111 RtlNlsInitState` 만 두고 (`+0x59a MmInitSystem(0)` 은 한 줄 "ApiSet 아님"), 새 나무: `Phase1Initialization` → `+0x23 Phase1InitializationDiscard` → `+0xc6a MmInitSystem(1)` → `+0xf5 MiInitSystem(1)` → `+0x63d MiInitializeApiSets` (이하 그대로) |
| 229 | "부팅 위상 0 `MiInitSystem +0x638 MiInitializeApiSets`" | "부팅 위상 1 `MiInitSystem +0x638 MiInitializeApiSets`" |
| 252 | "`ⒷA` 위상 0 ApiSet" | "`ⒷA` 위상 1 ApiSet (NLS 상태 `RtlNlsInitState` 는 위상 0)" |

### `runs/03_sprint/run57-nls-apiset-해설.md` (explainer 쪽, 같이 맞추면 좋음)

| 줄 | 고칠 말 |
|---|---|
| 76 | "InitBootProcessor → MmInitSystem → MiInitSystem → MiInitializeApiSets" → "Phase1Initialization → Phase1InitializationDiscard → MmInitSystem(1) → MiInitSystem(1) → MiInitializeApiSets" |
| 143~144 | 그림: `InitBootProcessor` 아래 `+0x59a MmInitSystem(0) → +0x149 MiInitSystem(0) └ +0x63d MiInitializeApiSets` → `Phase1Initialization` 쪽으로 옮김 (`+0xc6a MmInitSystem(1)` → `+0xf5 MiInitSystem(1)`) |
| 202~204 | "부르는 길 (부팅 위상 0)" → "(부팅 위상 1)"; `InitBootProcessor +0x595 …` 사슬 → 1-4 사슬; "`MmInitSystem` 은 위상 0·2 가 같은 call 자리를 쓰고, `MiInitSystem` 안에서 위상 0 쪽일 때만 이 함수로 간다 (`+0x75 TEST ECX` / `+0x77 JNZ`)" → "위상 1 은 `MmInitSystem +0xf0` 자리를 따로 쓰고, `MiInitSystem` 안에서 ECX = 1 일 때만 이 함수로 간다 (`+0x428 CMP ECX,EBX` · `+0x42a JNZ`)". "위상 0 = 아주 이른 단계 `[추측]`" 은 근거를 `InitializationPhase` 전역 (`InitBootProcessor +0x178` = 0 쓰기, `Phase1InitializationDiscard +0xcb` = 1 쓰기) 로 `[정적]` 승급 가능 |
| 216 | "PDB 에 공개 이름이 없다" → "PDB 공개 심볼은 없고, 타입에 칸 이름이 있다 (`MiState+0x1c0` 의 `ApiSetSection`·`ApiSetSchema`·`ApiSetSchemaSize`)" |
| 542 | "`ⒷA` 위상 0 ApiSet" → "위상 1" |

### `runs/03_sprint/flow.md`

| 줄 | 지금 | 고칠 말 |
|---|---|---|
| 1410 | "`InitBootProcessor +0x595 MmInitSystem(0)` → `MmInitSystem +0x144 MiInitSystem` → **`MiInitSystem +0x638 MiInitializeApiSets`** (위상 0 쪽). … PDB 공개 이름은 없다." | "`Phase1Initialization +0x1e` → `Phase1InitializationDiscard +0xc65 MmInitSystem(1)` → `MmInitSystem +0xf0 MiInitSystem` → **`MiInitSystem +0x638 MiInitializeApiSets`** (위상 1 쪽, `+0x42a JNZ` 가 ECX = 1 에서 안 뛰는 길). … 전역 셋은 PDB 공개 심볼은 없고 타입 이름이 있다: `MiState+0x1c0` `_MI_SECTION_STATE` 의 `+0xad8 ApiSetSection` · `+0xae0 ApiSetSchema` · `+0xae8 ApiSetSchemaSize`." (`~~위상 0~~ → 위상 1 (sast/60)` 꼴 가능) |
| 4683 (run57 로그 줄) | "섹션 = 부팅 위상 0 `MiInitSystem +0x638 MiInitializeApiSets`" | "`~~부팅 위상 0~~` 부팅 위상 1 (run65 정정)". 그 줄의 나머지는 그대로 |

### `runs/03_sprint/baggage.md`

| 줄 | 지금 | 고칠 말 |
|---|---|---|
| 36 | "**`ⒷA`** = 위상 0 `InitBootProcessor` 안 (`RtlNlsInitState`, `MmInitSystem(0) → MiInitSystem → MiInitializeApiSets`) — `Ⓑ1` 보다 **앞**" | "**`ⒷA`** = `Ⓑ1` 보다 **앞** 둘: ① 위상 0 `InitBootProcessor +0x10c RtlNlsInitState` (NLS 상태) ② 위상 1 `Phase1Initialization → Phase1InitializationDiscard (+0xc65) → MmInitSystem(1) → MiInitSystem(1) → MiInitializeApiSets` (ApiSet 섹션)" |
| 283 | "(run57, 부팅 위상 0 — `Ⓑ1` 보다 앞) `KiInitializeKernel → InitBootProcessor (호출 +0x7fe) → MmInitSystem (호출 +0x595) → MiInitSystem (호출 +0x144) → MiInitializeApiSets (호출 +0x638)`" | "(run57, 부팅 위상 1 — `Ⓑ1` 보다 앞) `Phase1Initialization → Phase1InitializationDiscard (호출 +0x1e) → MmInitSystem (호출 +0xc65) → MiInitSystem (호출 +0xf0) → MiInitializeApiSets (호출 +0x638)`. NLS 상태는 따로 `KiInitializeKernel → InitBootProcessor (호출 +0x7fe) → RtlNlsInitState (호출 +0x10c)`, 위상 0" |
| 1388 | "`ⒷA` **위상 0 `InitBootProcessor`** — `+0x10c RtlNlsInitState` … → `+0x595 MmInitSystem(0) → MiInitSystem +0x638 MiInitializeApiSets`: …" | "`ⒷA` — **위상 0 `InitBootProcessor +0x10c RtlNlsInitState`** (커널 NLS 상태 = UTF-8 기본값), 이어서 **위상 1 `Phase1InitializationDiscard +0xc65 MmInitSystem(1) → MiInitSystem +0x638 MiInitializeApiSets`**: …" (나머지 `+0x52`…`+0xce` 는 그대로) |
| 1948 | "`ⒷA…` | `KiInitializeKernel → InitBootProcessor … MiInitializeApiSets (호출 +0x638)` — 위상 0 (`MmInitSystem +0xb6 XOR ECX,ECX`, `MiInitSystem +0x75`·`+0x77` 의 0 쪽)" | "`ⒷA…` | `Phase1Initialization → Phase1InitializationDiscard (호출 +0x1e) → MmInitSystem (호출 +0xc65) → MiInitSystem (호출 +0xf0) → MiInitializeApiSets (호출 +0x638)` — 위상 1 (`MmInitSystem +0xe3 MOV ECX,EBX` = 1, `MiInitSystem +0x428 CMP ECX,EBX` · `+0x42a JNZ` 안 뜀)" |
| 1955 | `ⒷA` · `InitBootProcessor … RtlNlsInitState (호출 +0x10c)` | 그대로 맞음 (위상 0). 말만 "(위상 0)" 덧붙이면 ApiSet 줄과 안 섞인다 |
| 1956~1958 | `ⒷA… +0x52 …` (`MiInitializeApiSets` 안 줄) | 줄 번호·내용 그대로. 사슬 (283·1948) 만 바뀐다. 1956 의 "PDB 이름 없음" → "공개 심볼 없음, 타입 이름 `MiState+0x1c0+0xad8 ApiSetSection`" |
| 1995 (덤, 이번 질문 밖) | `nt+0xe2d8e0` dword 표 · `[0xe2d8f0]` · `DAT_140e2d8f8[형]` | PDB 이름 달 수 있음: `MiState+0x1c0` `_MI_SECTION_STATE` 의 `ImageViewExtensionOffsets` (`+0xba0`) · `TotalImageViewReservationSize` (`+0xbb0`) · `ScpCfgPages` (`+0xbb8`) — 쓸지는 오케스트레이터가 |

### 안 고치는 기록 (필요하면 오케스트레이터가 정정 메모만)

- `_workspace/sast/52_nls-apiset_result.md` 249줄 ("`MmInitSystem` 위상 0 ← `InitBootProcessor +0x595`"), 163줄 ("PDB 공개 심볼에 이름이 없다") — 그때의 결과서라 그대로 두고 "정정은 sast/60" 한 줄만 달 수 있다.
- `_workspace/plan.md` 405줄 (Q108 끝 줄) 같은 말 — 결정 기록이라 그대로, Q120c 끝 줄에 "run57 위상 0 → 1 정정" 적기.
- `PE_emulator` 쪽 (이 레포 밖): `MmInitSystem +0xf0` = 위상 1 자리, `Phase1InitializationDiscard +0xc65 MmInitSystem(1)` 이 맞다고 확인됨.

## 동적으로 확인할 것

- **필수 아님 (정적으로 닫힘).** `CMP`/`JNZ` 와 점프 목적지 표로 정해진 일이라 실행에서 뒤집힐 데가 없다.
- 보고 싶다면 (낮은 우선순위): 부팅 초기에 KDNET 으로 **첫 멈춤** (`bcdedit /debug on` + WinDbg "Break on first connection" 같은 부팅 때 멈추기) 이 필요하다. `MiInitSystem`·`MiInitializeApiSets` 는 `INIT` 구역 코드라 부팅이 끝나면 버려질 수 있어서 `[추측]` 부팅 뒤엔 멈춤점을 못 건다.
  - `bp nt!MiInitSystem` → 세 번 걸림, `r ecx` 가 0 → 1 → 2 (call 자리: `MmInitSystem+0x149` ret 두 번 (0·2) 과 `+0xf5` 한 번 (1)).
  - `bp nt!MiInitializeApiSets` → `k` 가 `MiInitSystem+0x63d` ← `MmInitSystem+0xf5` ← `Phase1InitializationDiscard+0xc6a` ← `Phase1Initialization+0x23`.
- 이미 대기열에 있는 Q110 (4) `dq nt+0xe2d818 L3` 은 그대로 쓰되 이름을 달 수 있다: `dt nt!_MI_SECTION_STATE ApiSet* (nt!MiState+0x1c0)` 같은 한 줄로 같은 세 칸이 보인다 (이름이 맞는지 한 번 더 확인하는 덤). 위상과 상관없음.

## 만든 파일·목록

- 새 `.asm`·`.c`·지도: **없음** (재사용). 이 결과서만 새 파일.
- 읽은 것: `ms/disassemble/ntoskrnl/{MiInitSystem,MmInitSystem,MiInitializeApiSets,Phase1Initialization,Phase1InitializationDiscard,Phase1InitializationIoReady,IoInitSystem,IoInitSystemPreDrivers,InitBootProcessor,KiInitializeBootStructures,PspInitPhase0,PsInitSystem,MmInitSystemDll}.asm` · `_calls.tsv` · `_refs/{MiInitSystem,MmInitSystem,MiInitializeApiSets,0xe2d818}.tsv` · PDB 타입 `_MI_SYSTEM_INFORMATION`·`_MI_SECTION_STATE`·`_MI_SYSTEM_IMAGE_STATE` · PDB 공개 심볼 (`llvm-pdbutil dump -publics`) · PE 구역표 (`ms/bin/.../ntoskrnl.exe` 읽기만).

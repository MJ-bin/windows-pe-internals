# 100 snap-map-small — 정적 분석 결과

- 요청서: `_workspace/sast/100_snap-map-small.md` (Q69 + Q70, 정적만)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`). PDB guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`ntdll.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 셋 다 있음). ntoskrnl 쪽 (sast/98) 은 **안 건드렸다** (ntoskrnl Ghidra 프로젝트·`ms/*/ntoskrnl/` 안 염).
- 뽑은 함수 (ntdll, 정규식 정확히 `^이름$`):
  - 요청 대상 5개 `^(LdrpCondenseGraphRecurse|LdrpDoPostSnapWork|LdrpSnapModule|LdrpMapAndSnapDependency|LdrpMinimalMapModule)$` depth 0 → 5개 (로그 `ms/ghidra/ntdll.export.sast100a.stdout`). 이미 있던 **같은 5개 파일만** 다시 써졌다 (`.asm` 개수 681 그대로, `_calls.tsv` 는 정렬해서 비교하면 줄 하나도 안 바뀜).
  - 요청서 밖 둘 (길 따라가기, depth 0, 새 파일 2개): `^(LdrLoadEnclaveModule|LdrpEnclaveAddDependentModule)$` → `ms/disassemble/ntdll/LdrLoadEnclaveModule.asm`, `LdrpEnclaveAddDependentModule.asm` (+ `.c`). 이유: Q70-③ 의 `0x800000` 을 **처음 넣는 줄**이 이 둘 안이라서 (아래). `.asm` 개수 681 → 683. 로그 `…sast100b.stdout`.
- 전수 검색 방법: 이미 있는 `.asm` 은 ntdll 함수 4594개 중 681개뿐이라 "전수"가 안 된다. 그래서 **`objdump -d -M intel` 로 ntdll 전체 `.text` 를 한 번 풀어** (421,500줄, 스크래치 파일) 거기서 찾았다 (`_index.tsv` 로 주소 → 함수+오프셋 변환). 바이트 패턴 (`C7 /0 … FD FF FF FF`) 검색도 같이 했다. 이 방법이 맞게 도는지는 **대조군**으로 확인: 같은 방식으로 `State = −5` (`LdrpMergeNodes +0xd9`, 노드 `+0x40` 기준 `[RDI−8]` 꼴) 을 잘 찾았다.
- 버전 확인: **일치.** `_calls.tsv`: `LdrpInitializeProcess +0x1bb6 / ret +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x166 / ret +0x16b → LdrpCallTlsInitializers`, `+0x197 / ret +0x19c → LdrpCallInitRoutine`, `LdrpLoadDll +0xfb / ret +0x100 → LdrpLoadDllInternal`.
- 스크립트 문제: **없음.**

## 한눈에

| 질문 | 한 줄 답 | 지금 PE_emulator |
|---|---|---|
| Q69-① `State = −3` 을 **처음** 쓰는 줄 | **없다.** ntdll 전체에서 노드 `State` 에 −3 을 쓰는 줄은 `LdrpCondenseGraphRecurse +0x14e` (전파) **딱 하나**. 후보 넷 (`DoPostSnapWork`·`SnapModule` caller·`ProcessWork`·`MapAndSnap` 뒤 갈래) 다 State 를 −3 으로 안 쓴다. −3 을 **읽는** 줄도 `+0x148` 하나뿐 | 맞다 (우리 쪽에 −3 쓰는 코드 없음 — 만들 것도 없음) |
| Q69-② 실패 반환이 `*(ctx+0x28)` 에 들어간 뒤 | **State 로 바꾸는 사람이 없다.** 노드는 snap 시작 때 쓴 **`State 4` 그대로**. 상태 칸은 `LdrpLoadDllInternal +0x274/+0x277` 에서 읽혀 `LdrpPrepareModuleForExecution` 을 건너뛰고, `+0x2e6 LdrpDecrementModuleLoadCountEx` → (로드 수가 0 이면) `LdrpUnloadNode` 길로 간다 (그 안 State 4 갈래는 일부만 읽음) | 해당 없음 (정상 흐름 밖, 에뮬레이터에 실패 길 없음) |
| Q70-③ `ctx+0x20` 비트 `0x800` · `0x800000` 켜는 줄 | `0x800` = `LdrpLoadWow64 +0x98 MOV R8D,0x800` (WoW64 프로세스 전용). `0x800000` = `LdrLoadEnclaveModule +0xe5` · `LdrpEnclaveAddDependentModule +0x9a` (enclave 전용). 둘 다 **의존 DLL 에 물려준다** (`LdrpLoadDependentModuleInternal +0x1e2 AND ESI,0x1800800`). `OR`/`BTS` 로 켜는 줄은 없음. **test.exe (x64) 는 둘 다 안 탄다** | 맞다 (exe 문맥 `0x200`→`0x201` 만) |
| Q70-④ `0x40000036` 이름 | **PDB·ntdll 문자열에 이름 없음.** ntdll 자기 메시지 표 글은 "{Image Relocated} … 다른 주소에 매핑했지만 **fixup 은 자동으로 해 준다**". 이름은 `STATUS_IMAGE_AT_DIFFERENT_BASE` `[추측: 기억]`. ntdll 은 이 값으로는 재배치를 안 한다 | 맞다 (뜻만 채움) |

---

## Q69 — snap 실패 길

### ① `State = −3` (`LdrModulesSnapError`) 을 처음 쓰는 줄

- **답: 없다.** `[정적]`
  - 노드 `State` 칸 = `_LDR_DDAG_NODE +0x38` (PDB `pdb_struct.py ntdll.dll _LDR_DDAG_NODE`, 노드 크기 `0x50`).
  - 전체 `.text` (`0x16a09c` 바이트) 에서 `MOV r/m32, imm32` 의 imm 이 `0xfffffffd` 인 **메모리 쓰기를 모양 가리지 않고** 찾으니 (기준 레지스터·오프셋 상관없이) **1곳**: `LdrpCondenseGraphRecurse +0x14e` (`.asm` 줄 `MOV dword ptr [RDI + 0x38],0xfffffffd`).
  - 이 줄은 **전파**다: `+0x13d` (재귀 호출 ret) 뒤 `+0x148 CMP [RSI+0x38],−3` (자식 노드) → `+0x14c JNZ` 안 뛰면 `+0x14e` 부모 `[RDI+0x38] = −3`. 자식이 이미 −3 이어야 켜진다.
  - 레지스터에 −3 을 담아 쓰는 길도 봤다: `MOV r32,−3` 은 ntdll 전체에 `inflate` (zlib) 두 곳뿐이고, `[reg+0x38]` 에 레지스터를 쓰는 줄 중 노드를 가리킬 수 있는 것은 `LdrpInitializeNode +0x2ba` 하나 (그 값은 `+0x2a5 MOV ECX,9` / `+0x2aa MOV EAX,0xfffffffc` 를 `CMOVNZ` 로 고른 것 = **9 또는 −4**). 나머지 레지스터 쓰기 (`LdrpInitializeNtdllDataTableEntry +0x150` = `R14D = 9`, `LdrpCreateSoftwareEnclave +0x64` = 다른 구조체 등) 도 −3 아님.
- **요청서 후보 넷** (`.asm` 줄):

  | 후보 | State 쓰는 줄 | 판정 |
  |---|---|---|
  | `LdrpDoPostSnapWork` | **없음.** `+0xf MOV RSI,[RCX+0x38]` 은 `ctx+0x38` (importer 항목) 읽기 | 아님 |
  | `LdrpSnapModule` | `+0xb87` `State = 5` 하나 (`LdrpDoPostSnapWork` ret `+0xb66` 뒤) | 아님 |
  | `LdrpProcessWork` | **없음.** 읽기 `+0x34 CMP [RAX+0x38],0` · `+0x38 JNZ +0x129` (State ≠ 0 이면 snap 길). 쓰기는 상태 칸 `+0x13b` 뿐 (아래 ②) | 아님 |
  | `LdrpMapAndSnapDependency` | `+0x4ac` `State = 4` (snap 하러 가기 직전), `+0x5ae` `State = 5` (IAT 없는 모듈) | 아님 |

- **sast/31 `[공백]` 닫힘**: "처음 −3 을 쓰는 곳" 이 안 보인 건 못 찾아서가 아니라 **없어서**다. 이 빌드 (26100.9457) 에서 ntdll 코드 안으로는 `LdrModulesSnapError` 가 노드에 들어가지 않는다 (−3 은 전파 줄을 위한 값만 남은 셈).
- **쓴 줄 전수 표** (참고 — `MOV dword [reg+0x38], 즉시값`·`[RDI−8]` 모양, 노드 기준. 줄마다 기준 레지스터가 노드인지는 `.asm` 을 읽은 것만 확인, 나머진 모양만):

  | State | 쓰는 줄 |
  |---|---|
  | 2 | `LdrpProcessMappedModule +0x1c2`, `LdrpInitializeProcess +0x1a60` |
  | 3 | `LdrpLoadDependentModuleInternal +0x162d` |
  | 4 | `LdrpMapAndSnapDependency +0x4ac`, `LdrpLoadContextReplaceModule +0x103`, `LdrpSignalModuleMapped +0x52` |
  | 5 | `LdrpMapAndSnapDependency +0x5ae`, `LdrpSnapModule +0xb87` |
  | 6 | `LdrpCondenseGraphRecurse +0xa3`, `LdrpNotifyLoadOfGraph +0x29`, `LdrpCorProcessImports +0x14` |
  | 7 | `LdrpNotifyLoadOfGraph +0x19`, `LdrpCorProcessImports +0x4d`, `LdrpInitializeProcess +0x16b0` |
  | 8 | `LdrpInitializeNode +0x1f` |
  | 9 | `LdrpInitializeNode +0x2ba` (CMOV), `LdrpMapDllWithSectionHandle +0x3ac`, `LdrpInitializeProcess +0x1508`·`+0x1735`, `LdrpInitializeNtdllDataTableEntry +0x150`, `LdrpLoadEnclaveModule +0x190`, `AvrfMiniLoadDll +0xa7a` |
  | −1 | `LdrpUnloadNode +0x32` |
  | −2 | `LdrpUnloadNode +0x1fb`, `LdrpEnclaveFreeModule +0x2f` |
  | **−3** | **`LdrpCondenseGraphRecurse +0x14e` (전파) 뿐** |
  | −4 | `LdrpInitializeGraphRecurse +0x6e`, `LdrpInitializeNode +0x2ba` (CMOV) |
  | −5 | `LdrpMergeNodes +0xd9` (`[RDI−8]`, RDI = 노드+0x40) |

  (`LdrpUnloadNode`·`LdrpEnclaveFreeModule`·`LdrpMergeNodes` 이외 일부는 `.asm` 이 없어 `objdump` 줄 — 함수 시작은 `_index.tsv` 와 같다.)
- 못 본 것 `[한계]`: 다른 DLL (shim·verifier 등) 이 노드 칸을 직접 쓰는 경우, 실행 중 만든 주소로 쓰는 경우 (정적으로 못 잡음). 이 둘은 test.exe 길과 무관해 보여 안 파헤침.
- **지금 우리**: 맞다. `PE_emulator/Src` 에 `SnapError`·State −3 을 쓰는 코드 없음 (grep). 로더 구간은 진짜 ntdll 코드가 Unicorn 안에서 돌아서, 우리가 −3 을 따로 만들어 줄 필요가 없다.

### ② snap 실패 반환이 `*(ctx+0x28)` 에 들어간 뒤 — 누가 State 로 바꾸나

- **답: 상태 칸을 보고 State 를 −3 으로 바꾸는 줄은 없다 (①).** 노드는 **State 4 그대로** 남고, 상태 칸은 "실패했으니 뒷일 하지 마라" 표지로만 쓰인다. `[정적]`
- **사슬 (함수+오프셋, `.asm` 줄 확인):**
  1. `LdrpSnapModule` 이 음수를 돌려줌: `+0xbc1 MOV EAX,R12D` (음수면 그 앞 `+0xbbc LdrpLogError(…,0x19,…)`, ret `+0xbc1`). State 는 안 씀 (State 쓰는 줄은 `+0xb87` 하나, 성공 쪽).
  2. 부른 자리 둘이 상태 칸에 씀 (같은 칸 `*(ctx+0x28)`):
     - 직접 길: `LdrpMapAndSnapDependency +0x4bd (call) / +0x4c2 (ret) → +0x4c4 TEST ESI → +0x4c6 JNS` (음수면 안 뜀) `→ +0x4cc → +0x4d1 JMP +0x69 → +0x69 MOV RAX,[RBX+0x28] → +0x6d MOV [RAX],ESI`. 이 직접 길은 `+0x4b6 CMP [R9+0x30],0` (부모 문맥 없음) 일 때만; 있으면 `+0x4bb JNZ +0x4f2 LdrpQueueWork` 로 일감에 넣는다.
     - 일감 길: `LdrpProcessWork +0x12c (call) / +0x131 (ret) → +0x133 TEST ESI → +0x135 JNS +0x15d` (음수면 안 뜀) `→ +0x137 MOV RAX,[RBX+0x28] → +0x13b MOV [RAX],ESI`. 이 길로 오는 조건은 `+0x34 CMP [RAX+0x38],0` → `+0x38 JNZ +0x129` (State ≠ 0).
     - (곁가지) forwarder 를 올리다 실패하면 `LdrpSnapModule +0xdff MOV RAX,[R14+0x28] → +0xe03 MOV [RAX],R12D` 가 **importer 쪽 문맥** (`R14 = 항목+0xb0`, `+0x4c3`) 의 칸에 씀. 역시 State 는 안 씀.
     - 예외: `LdrpProcessWork$fin$0 +0x12 / +0x16` 는 같은 칸에 `0xc0000005` 를 씀 (예외 정리 함수).
  3. 이 칸이 가리키는 곳 = **맨 바깥 `LdrpLoadDllInternal` 의 상태 변수** (`+0x1c0 MOV RBX,[RSP+0xc8]` → `+0x1c8 MOV [RSP+0x30],RBX` 로 문맥 만드는 함수에 넘김). 그래서 안쪽 어디서 쓰든 바깥이 그 값을 본다.
  4. 누가 읽고 무엇을 하나:
     - 일감 큐의 **다른 항목**: `LdrpProcessWork +0x1c (MOV RAX,[RCX+0x28]) / +0x20 CMP [RAX],0 / +0x23 JL +0x15d` → 남은 일감은 아무것도 안 하고 끝.
     - `LdrpLoadDllInternal` (바깥, ret `+0x21a LdrpDrainWorkQueue` 뒤): `+0x274 CMP [RBX],0 → +0x277 JL +0x2b1` — **음수면 `+0x27f LdrpPrepareModuleForExecution` (그래프 켜기) 를 통째로 건너뜀**. 건너뛰기 전에 `+0x25d LdrpCondenseGraph` 는 (항목 `+0xb0` ≠ 0 이면, `+0x24c CMP [RSI+0xb0],0`) 돌지만, State 4 인 실패 노드는 `LdrpCondenseGraphRecurse +0x96 CMP [RDI+0x38],5 → +0xa1 JNZ +0xaa` 로 `+0xa3 State = 6` 쓰기를 건너뛴다 (순환 묶음이 있으면 `LdrpMergeNodes +0xd9` 의 −5 가 따로 있지만 이 경우 아님 `[추측: 노드 하나짜리]`).
     - 뒤처리: `+0x2b1 … +0x2c3 (call) / +0x2c8 (ret) LdrpFreeLoadContextOfNode → +0x2c8 CMP [RBX],0 → +0x2cb JGE` (음수라 안 뜀) `→ +0x2da MOV [R15],0` (돌려줄 모듈 핸들 = 0) `→ +0x2e6 (call) / +0x2eb LdrpDecrementModuleLoadCountEx(항목, 0) → +0x2ee (call) / +0x2f3 LdrpDereferenceModule(항목)`. (`+0x2d1 CMP EDI,9` = 이유 9 면 다른 길로 `+0x3fb`.)
     - `LdrpDecrementModuleLoadCountEx`: `+0x8d (call) / +0x92 LdrpDecrementNodeLoadCountLockHeld(노드, 0, &지역)` → `+0xa1 CMP [RSP+0x48],ESI → +0xa5 JZ +0xf1` (지역이 0 이면 안 지움) → ≠ 0 이면 **`+0xaf (call) / +0xb4 LdrpUnloadNode`**.
     - `LdrpUnloadNode` (rva `0x865c0`, `.asm` 없음 — `objdump`): 입구 `+0x1e MOV EAX,[RCX+0x38]` 로 State 를 읽어 **−4 이면 `+0x32` 에서 −1**, 7·9 도 따로 가르고 (`+0x29a CMP EAX,7`, `+0x2a3 CMP EAX,9`), **그 밖 값 (4 포함) 은 `+0x2a6 JNE +0x138`** 길로 간다. 그 길에서 조건 (`+0x141 TEST RCX,RCX → +0x144 JE +0x1f8` 등) 에 따라 `+0x1fb State = −2` 에 닿을 수 있다. **이 갈래 본문은 안 읽었다 `[공백]`** (State 4 노드가 실제로 −2 가 되는지는 모름).
- 하나 더: 만약 State 가 −3·4 같은 노드로 `LdrpPrepareModuleForExecution` 이 불렸다면 `+0x40 SUB R8D,5 … +0x123 MOV EBX,0xc00000e5` (State 5~9 밖이면 `0xc00000e5`, ntdll 글 "An internal error occurred.") — 실제론 위 `+0x277` 이 건너뛰어서 안 닿는다.
- **지금 우리**: 해당 없음. test.exe 정상 흐름엔 snap 실패가 없다. `PE_emulator/Src/ntdll/LdrInit.cpp:6457` `LdrpFreeLoadContextOfNode` 는 빈 몸통 ("공사 중") — 실패 길을 만들게 되면 그 길도 **진짜 코드 (위 사슬)** 에 맡기는 쪽이 맞다.

---

## Q70 — 매핑 갈래

### ③ 로드 문맥 `ctx+0x20` 비트 `0x800` · `0x800000` 을 켜는 줄

- **먼저 한 줄: 두 비트를 `OR`·`BTS` 로 켜는 줄은 ntdll 전체에 없다.** `.text` 전체에서 `OR`/`BTS` 로 `0x800`·`0x800000` (또는 `BTS 0xb/0x17`) 를 넣는 줄을 찾았더니 로더 쪽은 `LdrpGetDllPath +0x162 OR EBX,0xa00` 하나뿐인데, 이건 DLL **검색 경로 옵션** 레지스터 (`EBX`) 라 문맥에 안 들어간다 (`+0x15c BT EBX,0xc` 와 같이 경로 계산용). 문맥 `+0x20` 을 쓰는 줄 전수 (로더 함수, 스택 아닌 것): `LdrpMapDllSearchPath +0x3c4 OR 0x10000`, `LdrpAppCompatRedirect +0xb1 BTS 0x10`, `LdrpMapAndSnapDependency +0xd6 OR 0x2000000`, `LdrpProcessMachineMismatch +0x37 BTS 0x15`, `LdrpMapDllPatchImage +0x70 BTS 0x1a`, `LdrpLoadDependentModuleInternal +0x1ca1 BTS 0x1a` — 다 **다른 비트**. 나머지 `MOV [ctx+0x20], 레지스터` 4곳은 **문맥을 새로 만들 때 한 번 채우는 줄** (아래).
- **비트는 "만들 때 받은 값"이 들어온다. 값이 태어나는 곳** `[정적]`:

  | 비트 | 태어나는 줄 | 어떤 때 |
  |---|---|---|
  | `0x800` | `LdrpLoadWow64 +0x98 MOV R8D,0x800` → `+0xa8 (call) / +0xad LdrpLoadDll(이름, 경로, 0x800, &핸들)` | WoW64 프로세스가 64비트 wow64 DLL 을 올릴 때 |
  | `0x800000` | `LdrLoadEnclaveModule +0xe5 MOV [RSP+0x40],0x800000` (→ `+0xf0 LdrpPreprocessDllName` → `+0xff MOV R8D,[RSP+0x40]` → `+0x133 (call) LdrpAllocatePlaceHolder`), `LdrpEnclaveAddDependentModule +0x9a MOV [RSP+0x40],0x800000` (→ `+0xae LdrpPreprocessDllName` → `+0xc1 MOV R9D,[RSP+0x40]` → `+0xe9 LdrpFindOrPrepareEnclaveModule`) | enclave 모듈 올리기 |

- **값이 문맥 `+0x20` 에 들어가는 길** (`.asm` 줄):
  - `LdrpLoadDll +0x2d MOV [RSP+0x58],R8D` (flags 보관) → `+0xbd LdrpPreprocessDllName(…,&flags)` (여기서 flags 에 `0x4`·`0x20`·`0x200`·`0x600` 만 더함 — `0x800`·`0x800000` 은 안 더함) → `+0xca MOV R8D,[RSP+0x58]` → `+0xfb (call) / +0x100 LdrpLoadDllInternal` → `+0x1d MOV R12D,R8D` → `+0x1df MOV R8D,R12D` → `+0x1ed LdrpFindOrPrepareLoadingModule` → `+0x37 MOV ESI,R8D` → `+0x40c BTS ESI,0xf` → **`+0x427 MOV [RAX+0x20],ESI`** (새 문맥).
  - enclave: `LdrpAllocatePlaceHolder +0x2c MOV R15D,R8D → +0x7c BTS R15D,0xf → +0x81 MOV [RAX+0x20],R15D`.
  - **의존 DLL 에 물려줌**: `LdrpLoadDependentModuleInternal +0x3b MOV R15,RDX` (인자 2 = importer 문맥) → **`+0x1de MOV ESI,[R15+0x20]` → `+0x1e2 AND ESI,0x1800800`** (importer 의 `0x800`·`0x800000`·`0x1000000` 세 비트만 남김) → … `+0x947 MOV R15D,ESI` → `+0x10e7 BTS R15D,0xf` → `+0x10ec MOV [RAX+0x20],R15D`. (enclave 이면 `+0x8f8 BT ESI,0x17 → JC +0x1bb1` 로 `LdrpFindOrPrepareEnclaveModule` 쪽 길.)
  - 곧, 비트는 뿌리 (`LdrpLoadWow64`·enclave 둘) 에서 한 번 켜지면 **그 뿌리가 부르는 모든 import 에 이어진다**.
- **사용자 `LdrLoadDll` 길로는 못 켠다**: `LdrLoadDll +0x64 ~ +0xa5` 가 사용자 값 `*DllCharacteristics` 를 문맥 flags 로 **한 비트씩 옮긴다** — `0x4→0x8`, `0x2→0x40`, `0x800000→0x80`, `0x1000→0x100`, 부호 → `0x400000` — 결과는 `{0x8, 0x40, 0x80, 0x100, 0x400000}` 안. `0x800`·`0x800000` 은 못 나온다. (`LdrLoadDll +0x16b (call) / +0x170 LdrpLoadDll(…, R8D=EBX)`.)
- **`LdrpLoadDll` 을 부르는 다른 곳의 flags** (objdump 로 모두 봄): `LdrpInitializeImportRedirection +0xc2 R8D=0x1000001` (비트 `0x1000000` 만, `0x800` 아님), `LdrpLoadShimEngine +0x1c9`·`LdrpCorInitialize +0x16a` = 1, `LdrpInitShimEngine +0xcd`·`LdrpGetProcApphelpCheckModule +0x255` = 0, `AVrfpLoadAndInitializeProvider +0xec` = 1 (`+0x62 LEA ESI,[RBX+1]`, RBX=0). `LdrpLoadForwardedDll` 은 `+0x27b XOR EDI,EDI → +0x27d MOV [RSP+0x58],EDI` 로 flags 0 (`+0x2e1 LdrpLoadDllInternal`), `LdrpLoadPatchImage` 는 `+0xbf` 호출 앞에서 `XOR R8D,R8D`.
- **읽는 곳** (참고 — 비트가 뭘 바꾸나):
  - `0x800000`: `LdrpMinimalMapModule +0xee/+0xf2` (`AND R8D,R12D` — 이때 `R12D = 0x800000`), `LdrpMapDllWithSectionHandle +0xb0`·`+0x2a5`, `LdrpCompleteMapModule +0x109`, `LdrpMapAndSnapDependency +0x33` (→ `+0x4f JNZ +0x96`: 활성화 컨텍스트 찾기 건너뜀), `LdrpPrepareImportAddressTableForSnap +0x37`, `LdrpPreprocessDllName +0x3e TEST [R12],0x800008` (이름 다듬기 건너뜀).
  - `0x800`: `LdrpMinimalMapModule +0x11d TEST [RSI+0x20],0x800 → +0x12c JNZ +0x30b` (`MaximumUserModeAddress` 를 주소 상한 칸에 넣고 종류 1 인자 `+0x323`), `LdrpMapDllNtFileName +0x4a0` (꺼져 있으면 `R13D = 0x1100000` — 이번엔 안 읽음).
  - → `NtMapViewOfSectionEx`: `+0x141 TEST ECX,ECX → +0x346 → +0x36c (call) / +0x371` (run38 그대로).
  - 같은 숫자 `0x800000` 이 `LdrpMinimalMapModule +0xa7 MOV R12D,0x800000` 에선 **NtMapViewOfSection 의 "할당 종류" 값**이다. 문맥 비트와 이름만 같은 별개 값이니 섞지 말 것.
- **그 조건 — test.exe 에서 타나: 안 탄다.** `[정적 조건 + 샘플]`
  - `0x800`: `LdrpInitializeProcess +0x1766 CMP [UseWOW64],R15D(0) → +0x176d JZ +0x1806` 이 켜짐 (≠ 0) 일 때만 `+0x17c1 (call) / +0x17c6 LdrpLoadWow64`. `UseWOW64 = (TEB.WowTebOffset > 0)` (flow.md: `+0x5f2`). x64 test.exe 면 0 `[추측: 값 안 봄]` → **`[VM]` `ntdll+0x8db6a` 에서 `dd ntdll!UseWOW64 L1`** (rva `0x8c404 + 0x1766`).
  - `0x800000`: enclave 를 올리는 `LdrLoadEnclaveModule` (내보내기 함수, 부르는 곳은 이 빌드 ntdll 안에 직접 호출 없음) · `LdrpEnclaveAddDelayloadModules +0x9e`·`LdrpEnclaveAddForwarderModules +0xf7` → `LdrpEnclaveAddDependentModule`. sample (`sample/*.c`) 에 enclave 없음 (grep 0건). 안 탄다.
  - 그래서 test.exe 의 문맥 `+0x20` 은 exe 는 `LdrpInitializeProcess +0x1328 MOV [RSP+0x2d0],0x200` → `+0x1a8e/+0x1a94 OR …,R12D(1)` = `0x201`, DLL 들은 `LdrpLoadDependentModuleInternal +0x1e2` 마스크로 `0x800/0x800000/0x1000000` 가 **0** → `LdrpMinimalMapModule` 은 `+0x174 NtMapViewOfSection` 쪽 (Ex 아님) `[정적 조건]`.
  - 실제 값은 `[VM]`: `ntdll+0xa30e` (`LdrpMinimalMapModule +0xee`, RSI = 문맥) 에서 `dd @rsi+0x20 L1` — `0x800`·`0x800000` 비트가 0 인지.
- **지금 우리**: 맞다. `PE_emulator/Src/ntdll/LdrInit.cpp:3945~3951` 가 exe 문맥 `0x200`, `:5383~5388` 이 `|= 1`. `0x800`·`0x800000` 은 안 켠다. **이름 겹침 주의**: `Src/nt/AddressSpace.cpp:663` 의 `LOADER_VIEW_ALLOCATION_TYPE = 0x800000` 은 위 `+0xa7` 의 "할당 종류" (맞음) 이지 문맥 비트가 아니다.

### ④ 결과 코드 `0x40000036` 의 이름

- **답: 이름은 PDB·ntdll 에 없다. 뜻은 확인했다.** `[정적]`
  - PDB (`ntdll.dll.pdb`): `llvm-pdbutil dump -types` · `-publics` 에서 `STATUS_IMAGE`·`DIFFERENT_BASE` 0건, `strings` 로도 `STATUS_` 는 어서션 글 둘 (`Status != STATUS_NOT_FOUND` 등) 뿐. ntdll 본문 문자열에도 없음.
  - ntdll 자기 **메시지 표 (`RT_MESSAGETABLE`, 영어)** 에 글은 있다 (번호 → 글만, 이름은 없음. 작은 파서로 읽음):
    - `0x40000036` = "{Image Relocated} An image file was mapped at a different address from the one specified in the image file but **fixups will still be automatically performed** on the image."
    - 비교 `0x40000003` = "{Image Relocated} An image file could not be mapped at the address specified in the image file. **Local fixups must be performed** on this image." (ko-KR `ntdll.dll.mui` 도 같은 뜻)
  - 이름 `STATUS_IMAGE_AT_DIFFERENT_BASE` 는 **기억에서 나온 `[추측]`** (글은 맞음). 문서엔 "`0x40000036` (이름은 PDB 에 없음 — 글: 이미지가 다른 주소에 붙었고 fixup 은 자동)" 로 쓰길.
  - 코드에서 이 값이 쓰이는 방식 (`+0x` 는 `.asm` 줄): 리터럴 `0x40000036` 은 ntdll 에 없고 **`0x40000003 + 0x33`** 모양으로만 나온다.
    - `LdrpMinimalMapModule +0x192 SUB ECX,0x40000003 → +0x198 JZ +0x288` · `+0x19e SUB ECX,0xb → +0x1a1 JZ +0x2fc` (=`0x4000000e` Mismatch) · `+0x1a7 CMP ECX,0x28 → +0x1aa JZ +0x288` (=`0x40000036`) — 둘 다 같은 `+0x288` (= 경고 코드 그대로 두고 재시도 검사 갈래) 로 간다.
    - `LdrpCompleteMapModule +0xe4 SUB ECX,0x40000003 → +0xea JZ +0x167` · `+0xec CMP ECX,0x33 → +0xef JZ +0x167` — 같은 `+0x167` (끝 로그 갈래).
    - **갈림**: `LdrpCompleteMapModule +0x1b4 CMP R12D,0x40000003 → +0x1bb JZ +0x309` 는 **정확히 `0x40000003` 일 때만** `+0x31b (call) / +0x320 LdrpRelocateImage`. `0x40000036` 은 `+0x1c1` 로 내려가 재배치를 **안 한다** — 글 ("fixup 은 자동") 과 맞다: 커널이 이미 해 줬다고 본다 `[정적: 줄 + 글]`.
- **지금 우리**: 맞다 / 뜻만 보태면 된다. `PE_emulator/Src/nt/AddressSpace.cpp:752~759` 의 `TODO(VM): 0x40000036 을 고르는 조건` 은 **커널 쪽** (`MiMapViewOfImageSection +0xc82`) 질문이라 이 답과 별개로 남는다. 우리가 `0x40000036` 을 돌려주면 ntdll 은 재배치를 안 하고 (위 `+0x1b4`), `0x40000003` 을 돌려주면 한다. 이름은 코드 상수 주석에 위 글 한 줄로 달아 두면 된다.

---

## 동적분석으로 확인해야 할 것

- (필수 아님) test.exe 정상 흐름에선 Q69 길이 안 선다. 확인하고 싶으면 **snap 실패를 일부러 만든 변종** (없는 export 를 import 하는 dll) 이 필요 — 그때 볼 자리:
  - `ntdll+0x21562` `LdrpMapAndSnapDependency +0x4c2` (`r esi` 음수) / `ntdll+0x17271` `LdrpProcessWork +0x131` (rva `0x17140 + 0x131`) → 상태 칸 쓰기 (`+0x6d` / `+0x13b`).
  - `ntdll+0x1fea7` `LdrpLoadDllInternal +0x277` (rva `0x1fc30 + 0x277`) 에서 `dd @rbx L1` 음수 · 노드 `+0x38` 이 `4` 로 남는지 (`dd poi(@rsi+0x98)+0x38 L1`).
  - `ntdll+0x86431` `LdrpDecrementModuleLoadCountEx +0xa1` → `+0xaf LdrpUnloadNode` 에 들어가는지, 들어가면 State 가 어떻게 변하나 (−2 인가).
- test.exe 길 확인 (이미 있는 멈춤점 재활용):
  - `ntdll+0x8db6a` (`LdrpInitializeProcess +0x1766`) `dd ntdll!UseWOW64 L1` = 0 → `0x800` 길 안 탐.
  - `ntdll+0xa30e` (`LdrpMinimalMapModule +0xee`) `dd @rsi+0x20 L1` → `0x800`·`0x800000` 비트 0, 그러면 `+0x174` (`ntdll+0xa394`) 로 `NtMapViewOfSection`.
  - run73 의 `ntdll+0xa399 r eax` 는 `0x40000036` 인지 `0` 인지 보는 자리 (그대로).
- 정적으로 못 정한 것: 다른 DLL 이 노드 State 를 쓰는지 (위 한계).

## 다음 요청 후보

1. `LdrpUnloadNode` (rva `0x865c0`) 와 `LdrpDecrementNodeLoadCountLockHeld` (rva `0x86530`) depth 0 — 실패 뒤 노드가 지워지는 본문. 짐 지도 범위 밖 (초기화 실패 길) 이라 **우선순위 낮음**.
2. (낮음) `LdrpFindOrPrepareEnclaveModule` · `LdrpCreatePendingEnclaveModule` — `0x800000` 이 `LdrpAllocatePlaceHolder` 의 `R8D` 로 정확히 어떻게 넘어가는지 (이번엔 `LdrLoadEnclaveModule +0x133` 쪽만 줄 확인).
3. 하네스 제안 (사용자 검토 뒤 오케스트레이터가): 이번 "ntdll **전체**에서 그 줄 찾기" 는 `objdump -d` 한 번 + `_index.tsv` 로 함수 이름 붙이기로 5분 만에 됐다. `.asm` 모음 (683/4594개) 으론 "전수"를 못 한다. `ghidra-headless-export/scripts/` 에 `objdump` 풀이·검색 도우미 (`ms/disassemble/<모듈>/_objdump_full.dis` 만들기 + `함수+오프셋` 붙이기) 가 있으면 sast 마다 임시 스크립트를 안 짜도 된다. (이번엔 스크래치 폴더에 임시로 썼음.)

## 판정

**분석성공.** Q69·Q70 네 질문 모두 줄 단위 근거로 답함. 핵심: ① −3 은 처음 쓰는 줄이 **없고** 전파 줄 하나뿐 (sast/31 `[공백]` 닫힘), ② 상태 칸 뒤 State 로 바꾸는 사람 없음 (State 4 유지 → 해제 길), ③ `0x800`·`0x800000` 은 `LdrpLoadWow64`·enclave 가 인자로 넣고 의존 DLL 이 마스크로 물려받음 (`OR`/`BTS` 없음, test.exe 안 탐), ④ `0x40000036` 이름은 PDB 에 없고 글만 확인 (재배치 안 함). 정적으로 못 정한 것 = 다른 DLL 의 노드 쓰기, `UseWOW64`·`ctx+0x20` 실제 값 (`[VM]`).

# 2026-11-16 (작성일) run106 — snap 실패 길 · 매핑 갈래 작은 정적 (Q69 + Q70, 정적만): **노드 `State = −3` (`LdrModulesSnapError`) 을 "처음" 쓰는 줄은 ntdll 에 없다** — ntdll 전체 `.text` 에서 State 칸 (`_LDR_DDAG_NODE +0x38`) 에 −3 을 쓰는 줄은 전파 `LdrpCondenseGraphRecurse +0x14e` 딱 하나 `[정적]`. **snap 이 실패하면 노드는 `State 4` 그대로**고, 실패는 문맥 상태 칸 `*(ctx+0x28)` 으로만 위로 간다 → 바깥 `LdrpLoadDllInternal +0x277` 이 음수를 보고 `LdrpPrepareModuleForExecution` 을 통째로 건너뛰고 해제 길 (`+0x2e6 LdrpDecrementModuleLoadCountEx` → `LdrpUnloadNode`) 로 간다. **로드 문맥 `ctx+0x20` 비트 `0x800` · `0x800000` 은 `OR`/`BTS` 로 켜는 줄이 없고, 만들 때 받은 flags 로 들어온다** — `0x800` = `LdrpLoadWow64 +0x98` (WoW64 만), `0x800000` = `LdrLoadEnclaveModule +0xe5` · `LdrpEnclaveAddDependentModule +0x9a` (enclave 만), 의존 DLL 은 `LdrpLoadDependentModuleInternal +0x1e2 AND ESI,0x1800800` 로 물려받는다. test.exe 는 둘 다 안 탄다 (`UseWOW64` 0 `[관찰]` run44). **`0x40000036` 은 PDB 에 이름이 없다** — ntdll 메시지 표 글은 "{Image Relocated} … 다른 주소에 붙었지만 fixup 은 자동으로 된다", 그래서 ntdll 은 이 값으론 재배치를 안 한다 (`LdrpCompleteMapModule +0x1b4`).

> **닫힘 (run123, sast/119 Q112 덤)**: 아래 `0x40000036` 이름 `[추측: 기억]` → **`[정적]` `STATUS_IMAGE_AT_DIFFERENT_BASE`** (`0x40000003` = `STATUS_IMAGE_NOT_AT_BASE`) — PDB 엔 여전히 없고, 같은 빌드 `nltest.exe` 의 값 → 이름 표 (rva `0x64770` · `0x64440`) 에서 바이트로 확정. → `run123-run58-60-leftovers.md` Q112 (1).

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 값 대조는 run44 사진 덤프 읽기 로그와 flow.md · 짐 지도에 이미 옮겨 둔 사진 값을 다시 읽은 것뿐이다 (아래 "사진 로그 대조").
- 정적: **ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`** (기준과 같음). 다시 analyze 안 함, export 둘 (결과서 — `ms/ghidra/ntdll.export.sast100a.stdout` · `…sast100b.stdout`).
  - 버전 대조 (결과서): `LdrpInitializeProcess +0x1bb6 / ret +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x166 / +0x16b → LdrpCallTlsInitializers`, `+0x197 / +0x19c → LdrpCallInitRoutine`, `LdrpLoadDll +0xfb / +0x100 → LdrpLoadDllInternal` 그대로 (`_calls.tsv`).
- **"ntdll 전체에서 그 줄 찾기" 는 Ghidra `.asm` 모음 (683 / 4594 함수) 이 아니라 `objdump -d -M intel` 로 ntdll `.text` 전체 (421,500줄, 스크래치 파일 — 커밋 안 함) 를 풀어 찾았다** (결과서). 주소 → 함수+오프셋은 `_index.tsv`. 바이트 꼴 (`C7 /0 … FD FF FF FF`) 검색도 같이 했고, 같은 방법이 `State = −5` (`LdrpMergeNodes +0xd9`, `[RDI−8]` 꼴) 를 잘 찾는 것으로 방법을 맞춰 봤다 (대조군).
  - 한계 (결과서): 다른 DLL (shim · verifier 등) 이 노드 칸을 직접 쓰는 경우, 실행 중 만든 주소로 쓰는 경우는 못 잡는다. 숫자는 "검색한 꼴 기준" 이다 (run41 · run72 와 같은 방식).
- `LdrpUnloadNode.asm` 은 결과서 때는 없어서 결과서가 `objdump` 로 읽었다. 이 문서를 쓰는 지금은 `ms/disassemble/ntdll/LdrpUnloadNode.asm` 이 있다 (2026-11-16 03:38, 다른 바퀴가 뽑은 것). flow-writer 가 결과서가 든 줄 (`+0x1e` · `+0x32` · `+0x29a` · `+0x2a3` · `+0x2a6` · `+0x141` · `+0x144` · `+0x1fb`) 만 그 파일로 맞춰 봤다 — 같다. 갈래 본문은 이번에도 안 읽었다.
- 다른 decompiler 가 같은 시간에 ntdll (sast/113) · ntoskrnl (sast/115) 을 돌리는 중이다. **sast/102 · 103 · 105 ~ 115 결과는 이 문서에 안 넣었다.**

## 목적

- **Q69** (run36 · sast/31 `[공백]`, snap 실패 길):
  - ① 노드 `State = −3` (`LdrModulesSnapError`) 을 **처음** 쓰는 줄은 어디인가 (아는 건 전파 `LdrpCondenseGraphRecurse +0x14e` 뿐).
  - ② `LdrpSnapModule` 실패 반환이 caller `*(ctx+0x28)` 에 들어간 뒤 **누가** 그걸 보고 State 로 바꾸나.
- **Q70** (run38 · sast/33 "정적으로 못 정한 것", 매핑 갈래):
  - ③ 로드 문맥 `ctx+0x20` 비트 `0x800` · `0x800000` (→ `LdrpMinimalMapModule` 의 `NtMapViewOfSectionEx` 길) 을 **켜는 줄**과 그 조건, test.exe 에서 타나.
  - ④ 결과 코드 `0x40000036` 의 이름.
- 앞 바퀴 짝: run36 (`runs/02_sprint/run36-snap-module.md` — snap 실패 `[공백]`), run38 (`runs/02_sprint/run38-map-module.md` — `ctx+0x20` 비트 · `0x40000036`), run102 (`runs/03_sprint/run102-image-reloc.md` — 커널 쪽 `0x40000036` 이 나오는 줄).

## 방법

- 정적: `_workspace/sast/100_snap-map-small.md` — ntdll
  - `^(LdrpCondenseGraphRecurse|LdrpDoPostSnapWork|LdrpSnapModule|LdrpMapAndSnapDependency|LdrpMinimalMapModule)$` depth 0 (5개 — 이미 있던 같은 5개 파일만 다시 써짐, `_calls.tsv` 는 정렬 비교로 한 줄도 안 바뀜).
  - 요청서 밖 depth 0 새로 둘: `LdrLoadEnclaveModule` · `LdrpEnclaveAddDependentModule` (`0x800000` 이 태어나는 줄이 이 안이라서). `.asm` 681 → 683.
  - 전수 검색: `objdump` 풀이 + `_index.tsv` (위 환경).
- 결과서: `_workspace/sast/100_snap-map-small_result.md` (**분석성공** — 정적으로 못 정한 것 = 다른 DLL 의 노드 쓰기, `ctx+0x20` 실제 값).
- flow-writer 가 다시 본 것 (`.asm` 줄):
  - `LdrpCondenseGraphRecurse` (`+0x96` · `+0xa1` · `+0xa3` · `+0x138` · `+0x13d` · `+0x148` · `+0x14c` · `+0x14e` · `+0x155`)
  - `LdrpMapAndSnapDependency` (`+0x69` · `+0x6d` · `+0x4ac` · `+0x4b6` · `+0x4bb` · `+0x4bd` · `+0x4c4` · `+0x4c6` · `+0x4d1` · `+0x5ae`)
  - `LdrpProcessWork` (`+0x1c` · `+0x20` · `+0x23` · `+0x34` · `+0x38` · `+0x12c` · `+0x133` · `+0x135` · `+0x137` · `+0x13b`)
  - `LdrpSnapModule` (`+0xb87` · `+0xbbc` · `+0xbc1` · `+0xdff` · `+0xe03`)
  - `LdrpLoadDllInternal` (`+0x1c0` · `+0x1c8` · `+0x215` · `+0x24c` · `+0x25d` · `+0x274` · `+0x277` · `+0x27f` · `+0x2c3` · `+0x2c8` · `+0x2cb` · `+0x2d1` · `+0x2da` · `+0x2e6` · `+0x2ee`)
  - `LdrpDecrementModuleLoadCountEx` (`+0x8d` · `+0xa1` · `+0xa5` · `+0xaf`), `LdrpUnloadNode` (위 환경)
  - `LdrpLoadWow64` (`+0x98` · `+0xa8`), `LdrLoadEnclaveModule` (`+0xe5` · `+0xf0` · `+0xff` · `+0x133`), `LdrpEnclaveAddDependentModule` (`+0x9a` · `+0xae` · `+0xc1` · `+0xe9`)
  - `LdrpLoadDependentModuleInternal` (`+0x3b` · `+0x1de` · `+0x1e2` · `+0x8f8` · `+0x947` · `+0x10e7` · `+0x10ec`), `LdrpFindOrPrepareLoadingModule` (`+0x37` · `+0x40c` · `+0x427`), `LdrpLoadDll` (`+0x2d` · `+0xbd` · `+0xca` · `+0xfb`), `LdrLoadDll` (`+0x64` · `+0xa5` · `+0x16b`), `LdrpGetDllPath +0x162`
  - `LdrpMinimalMapModule` (`+0xa7` · `+0xee` · `+0xf2` · `+0x11d` · `+0x12c` · `+0x132` · `+0x13f` · `+0x141` · `+0x174` · `+0x192` · `+0x198` · `+0x19e` · `+0x1a7` · `+0x1aa`), `LdrpCompleteMapModule` (`+0xe4` · `+0xec` · `+0xef` · `+0x1b4` · `+0x1bb` · `+0x31b`)
  - `LdrpInitializeProcess` (`+0x1328` · `+0x1766` · `+0x176d` · `+0x17c1`)
  - `_calls.tsv` (ntdll): 아래 표의 줄 전부.
  - `alias:` 없음 (이번에 적은 callee 는 모두 이름 하나).

## 결과

비유 (관리실 = ntdll, 작업 지시서 = 로드 문맥 `ctx`, 장부 = 노드):

- 주소 잇기 (snap) 가 실패하면 장부의 "단계" 칸은 **"잇는 중 (4)" 그대로** 둔다. "잇기 실패 (−3)" 도장을 처음 찍는 사람은 없다. 실패는 **지시서 맨 위 "결과" 칸** (`*(ctx+0x28)`) 에만 적히고, 그 칸은 맨 처음 일을 시킨 사람 (`LdrpLoadDllInternal`) 의 메모지를 가리킨다. 그 사람은 메모지가 빨간 숫자면 "전원 켜기" 단계를 건너뛰고 짐을 도로 싼다.
- `0x800` · `0x800000` 은 지시서에 **중간에 덧칠하는 표시가 아니라, 처음 지시서를 쓸 때부터 받아 온 표시**다. 그 표시를 처음 주는 사람은 "32비트 손님 (WoW64) 준비 담당" 과 "금고방 (enclave) 담당" 뿐이고, 그 사람이 부른 DLL 의 지시서에는 같은 표시가 대물림된다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpCondenseGraphRecurse` | `+0x138` | `+0x13d` | `LdrpCondenseGraphRecurse` (자기 재귀) | 번호 없는 자식. 돌아와 `+0x148 CMP [RSI+0x38],−3` (자식) → `+0x14c JNZ` 안 뛰면 **`+0x14e [RDI+0x38] = −3`** (부모) | [정적] (run24 · run36 그대로) |
| `ntdll!LdrpMapAndSnapDependency` | `+0x4bd` | `+0x4c2` | `LdrpSnapModule` | `+0x4ac` State = 4 바로 뒤. 부모 문맥 없음 (`+0x4b6 CMP [R9+0x30],0`) 일 때만 — 있으면 `+0x4bb JNZ` → `+0x4f2 LdrpQueueWork` (ret `+0x4f7`) | [정적] |
| `ntdll!LdrpProcessWork` | `+0x12c` | `+0x131` | `LdrpSnapModule` | 노드 State ≠ 0 (`+0x34` · `+0x38 JNZ`) | [정적] (run24 그대로) |
| `ntdll!LdrpSnapModule` | `+0xbbc` | `+0xbc1` | `LdrpLogError(…, 0x19, …)` | 끝 상태가 음수일 때. `+0xbc1 MOV EAX,R12D` 로 그 값을 돌려줌 | [정적] |
| `ntdll!LdrpLoadDllInternal` | `+0x215` | `+0x21a` | `LdrpDrainWorkQueue` | 일감을 다 비운 뒤 상태 칸을 본다 | [정적] (run21 그대로) |
| `ntdll!LdrpLoadDllInternal` | `+0x25d` | `+0x262` | `LdrpCondenseGraph` | 항목 `+0xb0` ≠ 0 (`+0x24c`). **실패여도 돈다** (상태 칸 검사 `+0x274` 보다 앞) | [정적] |
| `ntdll!LdrpLoadDllInternal` | `+0x27f` | `+0x284` | `LdrpPrepareModuleForExecution` | **상태 칸 `[RBX] ≥ 0` 일 때만** (`+0x274 CMP` · `+0x277 JL → +0x2b1`) | [정적] |
| `ntdll!LdrpLoadDllInternal` | `+0x2c3` · `+0x2e6` · `+0x2ee` | `+0x2c8` · `+0x2eb` · `+0x2f3` | `LdrpFreeLoadContextOfNode` · `LdrpDecrementModuleLoadCountEx(항목, 0)` · `LdrpDereferenceModule(항목)` | 뒤 둘은 상태 칸 음수일 때 (`+0x2c8 CMP` · `+0x2cb JGE` 안 뜀, `+0x2da` 돌려줄 핸들 = 0). `+0x2d1 CMP EDI,9` 면 `+0x3fb` 다른 길 | [정적] |
| `ntdll!LdrpDecrementModuleLoadCountEx` | `+0x8d` · `+0xaf` | `+0x92` · `+0xb4` | `LdrpDecrementNodeLoadCountLockHeld(노드, 0, &지역)` · `LdrpUnloadNode` | 둘째는 지역 칸 ≠ 0 일 때 (`+0xa1 CMP` · `+0xa5 JZ → +0xf1`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x17c1` | `+0x17c6` | `LdrpLoadWow64` | `UseWOW64 ≠ 0` (`+0x1766 CMP` · `+0x176d JZ → +0x1806`) — test.exe 안 탐 | [정적] (run19 그대로), 안 탐 `[정적+관찰]` |
| `ntdll!LdrpLoadWow64` | `+0xa8` | `+0xad` | `LdrpLoadDll(이름, 경로, 0x800, &핸들)` | **`+0x98 MOV R8D,0x800`** | [정적] |
| `ntdll!LdrLoadEnclaveModule` | `+0xf0` · `+0x133` | `+0xf5` · `+0x138` | `LdrpPreprocessDllName` · `LdrpAllocatePlaceHolder` | **`+0xe5 [RSP+0x40] = 0x800000`** → `+0xff MOV R8D,[RSP+0x40]` | [정적] |
| `ntdll!LdrpEnclaveAddDependentModule` | `+0xae` · `+0xe9` | `+0xb3` · `+0xee` | `LdrpPreprocessDllName` · `LdrpFindOrPrepareEnclaveModule` | **`+0x9a [RSP+0x40] = 0x800000`** → `+0xc1 MOV R9D,[RSP+0x40]` | [정적] |
| `ntdll!LdrpLoadDll` | `+0xbd` · `+0xfb` | `+0xc2` · `+0x100` | `LdrpPreprocessDllName` · `LdrpLoadDllInternal` | flags 보관 `+0x2d` → `+0xca MOV R8D,[RSP+0x58]` | [정적] |
| `ntdll!LdrpLoadDllInternal` | `+0x1ed` | `+0x1f2` | `LdrpFindOrPrepareLoadingModule` | 그 안 `+0x37 MOV ESI,R8D` → `+0x40c BTS ESI,0xf` → **`+0x427 [RAX+0x20] = ESI`** (새 문맥) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x174` · `+0x36c` | `+0x179` · `+0x371` | `NtMapViewOfSection` · `NtMapViewOfSectionEx` | `Ex` 는 `+0x11d TEST [RSI+0x20],0x800` 또는 `+0xee ~ +0xf2` (`& 0x800000`) 일 때 (`+0x13f TEST` · `+0x141 JNZ`) | [정적] (run38 그대로) |
| `ntdll!LdrpCompleteMapModule` | `+0x31b` | `+0x320` | `LdrpRelocateImage` | 맵 결과가 **정확히** `0x40000003` (`+0x1b4 CMP` · `+0x1bb JZ`). `0x40000036` 은 안 옴 | [정적] (run38 그대로) |

### Q69-① `State = −3` 을 처음 쓰는 줄 — 없다 (결과서 Q69 ①)

- **State 칸** = `_LDR_DDAG_NODE +0x38` (PDB, 노드 `0x50`).
- **답** `[정적]`: ntdll 전체 `.text` 에서 `MOV r/m32, imm32` 의 imm 이 `0xfffffffd` 인 메모리 쓰기는 (기준 레지스터 · 오프셋 가리지 않고) **1곳 — `LdrpCondenseGraphRecurse +0x14e`** (`MOV dword ptr [RDI + 0x38],0xfffffffd`).
  - 이 줄은 **전파**다. `+0x138` 재귀 호출 (ret `+0x13d`) → `+0x148 CMP [RSI+0x38],−3` (자식) → `+0x14c JNZ` 를 안 뛰면 → `+0x14e` 부모 `[RDI+0x38] = −3` → `+0x155 JMP`. **자식이 이미 −3 이어야** 켜진다. −3 을 **읽는** 줄도 `+0x148` 하나뿐.
  - 레지스터에 −3 을 담아 쓰는 길도 봤다: `MOV r32,−3` 은 ntdll 전체에 `inflate` (zlib) 두 곳뿐. `[reg+0x38]` 에 레지스터를 쓰는 줄 중 노드를 가리킬 수 있는 건 `LdrpInitializeNode +0x2ba` 하나 — 값은 `+0x2a5 MOV ECX,9` / `+0x2aa MOV EAX,0xfffffffc` 를 `CMOVNZ` 로 고른 9 또는 −4 (결과서).
- **요청서 후보 넷** — 다 아님 `[정적]`:

| 후보 | State 를 쓰는 줄 |
|---|---|
| `LdrpDoPostSnapWork` | 없음 (`+0xf MOV RSI,[RCX+0x38]` 은 문맥 `+0x38` 읽기) |
| `LdrpSnapModule` | `+0xb87` State = 5 하나 (성공 쪽) |
| `LdrpProcessWork` | 없음. 읽기 `+0x34` · 쓰기는 상태 칸 `+0x13b` 뿐 |
| `LdrpMapAndSnapDependency` | `+0x4ac` State = 4, `+0x5ae` State = 5 |

- **State 값별 쓰는 줄 (결과서 전수 표 — 노드 기준 꼴. 기준 레지스터가 노드인지 `.asm` 으로 확인한 건 일부, 나머진 꼴만)**:

| State | 쓰는 줄 |
|---|---|
| 2 | `LdrpProcessMappedModule +0x1c2`, `LdrpInitializeProcess +0x1a60` |
| 3 | `LdrpLoadDependentModuleInternal +0x162d` |
| 4 | `LdrpMapAndSnapDependency +0x4ac`, `LdrpLoadContextReplaceModule +0x103`, `LdrpSignalModuleMapped +0x52` |
| 5 | `LdrpMapAndSnapDependency +0x5ae`, `LdrpSnapModule +0xb87` |
| 6 | `LdrpCondenseGraphRecurse +0xa3`, `LdrpNotifyLoadOfGraph +0x29`, `LdrpCorProcessImports +0x14` |
| 7 | `LdrpNotifyLoadOfGraph +0x19`, `LdrpCorProcessImports +0x4d`, `LdrpInitializeProcess +0x16b0` |
| 8 | `LdrpInitializeNode +0x1f` |
| 9 | `LdrpInitializeNode +0x2ba` (CMOV), `LdrpMapDllWithSectionHandle +0x3ac`, `LdrpInitializeProcess +0x1508` · `+0x1735`, `LdrpInitializeNtdllDataTableEntry +0x150`, `LdrpLoadEnclaveModule +0x190`, `AvrfMiniLoadDll +0xa7a` |
| −1 | `LdrpUnloadNode +0x32` |
| −2 | `LdrpUnloadNode +0x1fb`, `LdrpEnclaveFreeModule +0x2f` |
| **−3** | **`LdrpCondenseGraphRecurse +0x14e` (전파) 뿐** |
| −4 | `LdrpInitializeGraphRecurse +0x6e`, `LdrpInitializeNode +0x2ba` (CMOV) |
| −5 | `LdrpMergeNodes +0xd9` (`[RDI−8]`, RDI = 노드 + `0x40`) |

  - `.asm` 이 없던 함수 줄은 결과서 `objdump` 줄이다 (함수 시작은 `_index.tsv` 와 같다).
  - `LdrpInitializeProcess +0x16b0` · `+0x1735` 는 App Verifier 묶음 (`+0x1568 ~ +0x1753`, run105) 안 오프셋이다 — test.exe 는 그 묶음을 건너뛴다 `[정적: 오프셋 범위]`.
- **sast/31 `[공백]` 닫힘**: "처음 −3 을 쓰는 곳" 이 안 보인 건 못 찾아서가 아니라 **이 빌드 ntdll 에 없어서**다. −3 은 전파 줄을 위한 값만 남아 있다 `[정적]`. 그 전파 줄이 실제로 켜질 길이 있는지 (다른 DLL 이 노드에 −3 을 쓰는지) 는 정적 한계 밖 `[공백]`.

### Q69-② 실패 반환 → `*(ctx+0x28)` → 누가 보나 (결과서 Q69 ②)

- **State 로 바꾸는 사람은 없다** (①). 노드는 snap 시작 때 쓴 **`State 4` 그대로** 남는다 `[정적]`. 상태 칸은 "실패했으니 뒷일 하지 마라" 표지로만 쓰인다.
- **사슬** `[정적]`:
  1. `LdrpSnapModule` 이 음수를 돌려줌: `+0xbbc LdrpLogError` (ret `+0xbc1`) → `+0xbc1 MOV EAX,R12D`. State 는 안 씀 (State 쓰는 줄은 성공 쪽 `+0xb87` 하나 — run36 그대로).
  2. 부른 자리 둘이 **같은 칸** `*(ctx+0x28)` 에 쓴다:
     - 직접 길: `LdrpMapAndSnapDependency +0x4bd` (ret `+0x4c2`) → `+0x4c4 TEST ESI` → `+0x4c6 JNS` (음수면 안 뜀) → `+0x4d1 JMP +0x69` → `+0x69 MOV RAX,[RBX+0x28]` → **`+0x6d MOV [RAX],ESI`**. 이 길은 부모 문맥이 없을 때 (`+0x4b6`) 만.
     - 일감 길: `LdrpProcessWork +0x12c` (ret `+0x131`) → `+0x133 TEST ESI` → `+0x135 JNS +0x15d` (음수면 안 뜀) → `+0x137 MOV RAX,[RBX+0x28]` → **`+0x13b MOV [RAX],ESI`**.
     - 곁가지: forwarder 대상 올리기가 실패하면 `LdrpSnapModule +0xdff MOV RAX,[R14+0x28]` → `+0xe03 MOV [RAX],R12D` 가 importer 쪽 문맥 칸에 쓴다 (run36 "`+0xe03`" 그대로). 역시 State 는 안 씀.
     - 예외 정리 함수 `LdrpProcessWork$fin$0 +0x12 / +0x16` 은 같은 칸에 `0xc0000005` (결과서).
  3. 그 칸이 가리키는 곳 = **맨 바깥 `LdrpLoadDllInternal` 의 상태 변수** — `+0x1c0 MOV RBX,[RSP+0xc8]` → `+0x1c8 MOV [RSP+0x30],RBX` 로 문맥 만드는 함수에 넘김. 그래서 안쪽 어디서 쓰든 바깥이 본다.
  4. 누가 읽나:
     - 같은 큐의 **다른 일감**: `LdrpProcessWork +0x1c MOV RAX,[RCX+0x28]` → `+0x20 CMP [RAX],0` → `+0x23 JL` → 남은 일감은 아무것도 안 하고 끝.
     - 바깥 `LdrpLoadDllInternal` (`+0x215 LdrpDrainWorkQueue` 뒤):
       - `+0x25d LdrpCondenseGraph` 는 (항목 `+0xb0` ≠ 0 이면) **돈다**. 그래도 State 4 인 실패 노드는 `LdrpCondenseGraphRecurse +0x96 CMP [RDI+0x38],5` → `+0xa1 JNZ` 로 `+0xa3 State = 6` 을 건너뛴다.
       - **`+0x274 CMP [RBX],0` → `+0x277 JL → +0x2b1`**: 음수면 `+0x27f LdrpPrepareModuleForExecution` (그래프 켜기) 를 **통째로 건너뜀**.
       - 뒤처리: `+0x2c3 LdrpFreeLoadContextOfNode` (ret `+0x2c8`) → `+0x2c8 CMP [RBX],0` · `+0x2cb JGE` 안 뜀 → `+0x2da MOV [R15],0` (돌려줄 모듈 핸들 0) → `+0x2e6 LdrpDecrementModuleLoadCountEx(항목, 0)` → `+0x2ee LdrpDereferenceModule(항목)`.
     - `LdrpDecrementModuleLoadCountEx`: `+0x8d LdrpDecrementNodeLoadCountLockHeld(노드, 0, &지역)` → `+0xa1 CMP [RSP+0x48],ESI` · `+0xa5 JZ` → 지역 ≠ 0 이면 **`+0xaf LdrpUnloadNode`**.
     - `LdrpUnloadNode` (rva `0x865c0`): 입구 `+0x1e MOV EAX,[RCX+0x38]` 로 State 를 읽어 −4 면 `+0x32` 에서 −1, 7 · 9 는 따로 (`+0x29a` · `+0x2a3`), **그 밖 (4 포함) 은 `+0x2a6 JNE +0x138`** 길. 그 길은 조건 (`+0x141 TEST RCX,RCX` · `+0x144 JE +0x1f8` 등) 에 따라 `+0x1fb State = −2` 에 닿을 수 있다. **이 갈래 본문은 안 읽었다 `[공백]`** — State 4 노드가 실제로 −2 가 되는지 모름.
- 덧: 만약 State 가 5 ~ 9 밖인 노드로 `LdrpPrepareModuleForExecution` 이 불렸다면 `+0x40 SUB R8D,5 … +0x123 MOV EBX,0xc00000e5` ("An internal error occurred.") 로 끝난다 (결과서). 실제론 위 `+0x277` 이 건너뛰어 안 닿는다 `[정적]`.

### Q70-③ `ctx+0x20` 비트 `0x800` · `0x800000` — 켜는 줄 없음, 받은 flags 로 들어옴 (결과서 Q70 ③)

- **`OR` · `BTS` 로 켜는 줄은 ntdll 전체에 없다** `[정적: objdump 꼴]`.
  - `.text` 전체에서 `OR` / `BTS` 로 `0x800` · `0x800000` (또는 `BTS 0xb` / `0x17`) 을 넣는 로더 쪽 줄은 `LdrpGetDllPath +0x162 OR EBX,0xa00` 하나 — 이건 검색 경로 옵션 레지스터라 문맥에 안 들어간다.
  - 문맥 `+0x20` 에 비트를 켜는 줄 전수 (로더 함수, 스택 아닌 것): `LdrpMapDllSearchPath +0x3c4 OR 0x10000` · `LdrpAppCompatRedirect +0xb1 BTS 0x10` · `LdrpMapAndSnapDependency +0xd6 OR 0x2000000` · `LdrpProcessMachineMismatch +0x37 BTS 0x15` · `LdrpMapDllPatchImage +0x70 BTS 0x1a` · `LdrpLoadDependentModuleInternal +0x1ca1 BTS 0x1a` — 다 **다른 비트**. 나머지 `MOV [ctx+0x20], 레지스터` 는 문맥을 새로 만들 때 한 번 채우는 줄이다 (결과서).
- **비트가 태어나는 곳** `[정적]`:

| 비트 | 태어나는 줄 | 어떤 때 |
|---|---|---|
| `0x800` | `LdrpLoadWow64 +0x98 MOV R8D,0x800` → `+0xa8 LdrpLoadDll(이름, 경로, 0x800, &핸들)` | WoW64 프로세스가 64비트 wow64 DLL 을 올릴 때 |
| `0x800000` | `LdrLoadEnclaveModule +0xe5` (→ `+0xff` → `+0x133 LdrpAllocatePlaceHolder`), `LdrpEnclaveAddDependentModule +0x9a` (→ `+0xc1` → `+0xe9 LdrpFindOrPrepareEnclaveModule`) | enclave 모듈 올리기 |

- **문맥 `+0x20` 에 들어가는 길** `[정적]`:
  - 보통 길: `LdrpLoadDll +0x2d` (flags 보관) → `+0xbd LdrpPreprocessDllName` (여기서 더하는 건 `0x4` · `0x20` · `0x200` · `0x600` 뿐 — 결과서) → `+0xca` → `+0xfb LdrpLoadDllInternal` → `+0x1ed LdrpFindOrPrepareLoadingModule` → `+0x37 MOV ESI,R8D` → `+0x40c BTS ESI,0xf` → **`+0x427 MOV [RAX+0x20],ESI`**.
  - enclave 길: `LdrpAllocatePlaceHolder +0x2c` → `+0x7c BTS R15D,0xf` → `+0x81 MOV [RAX+0x20],R15D` (결과서 줄).
  - **의존 DLL 에 물려줌**: `LdrpLoadDependentModuleInternal +0x3b MOV R15,RDX` (importer 문맥) → **`+0x1de MOV ESI,[R15+0x20]` → `+0x1e2 AND ESI,0x1800800`** (importer 의 `0x800` · `0x800000` · `0x1000000` 셋만 남김) → `+0x947 MOV R15D,ESI` → `+0x10e7 BTS R15D,0xf` → `+0x10ec MOV [RAX+0x20],R15D`. enclave 면 `+0x8f8 BT ESI,0x17` 로 enclave 쪽 길.
  - 곧 비트는 **뿌리 (`LdrpLoadWow64` · enclave 둘) 에서 한 번 들어오면 그 뿌리가 부르는 import 에 이어진다**. (run23 이 flow.md 2.6 에 적은 "부모 `+0x20 & 0x1800800` 에서 시작" 과 같은 줄.)
- **사용자 `LdrLoadDll` 로는 못 켠다**: `LdrLoadDll +0x64 ~ +0xa5` 가 사용자 `*DllCharacteristics` 를 문맥 flags 로 **한 비트씩 옮긴다** — `0x4 → 0x8`, `0x2 → 0x40`, `0x800000 → 0x80`, `0x1000 → 0x100`, 부호 → `0x400000`. 결과는 `{0x8, 0x40, 0x80, 0x100, 0x400000}` 안이라 `0x800` · `0x800000` 은 못 나온다 (`+0x16b LdrpLoadDll`, ret `+0x170`) `[정적]`.
- **`LdrpLoadDll` 을 부르는 다른 곳의 flags** (결과서 `objdump`): `LdrpInitializeImportRedirection +0xc2` = `0x1000001` (비트 `0x1000000` — 이 비트의 출처), `LdrpLoadShimEngine +0x1c9` · `LdrpCorInitialize +0x16a` · `AVrfpLoadAndInitializeProvider +0xec` = 1, `LdrpInitShimEngine +0xcd` · `LdrpGetProcApphelpCheckModule +0x255` = 0. `LdrpLoadForwardedDll` (`+0x27b` · `+0x27d`) · `LdrpLoadPatchImage` (`+0xbf` 앞) 도 0.
- **이름 겹침 주의**: `LdrpMinimalMapModule +0xa7 MOV R12D,0x800000` 은 `NtMapViewOfSection` 의 **할당 종류** 값이다 (run38 `MEM_DIFFERENT_IMAGE_BASE_OK` `[추측: 이름]`). 문맥 비트와 숫자만 같은 별개 값. 같은 함수 `+0xee MOV R8D,[RSI+0x20]` → `+0xf2 AND R8D,R12D` 가 문맥 비트 `0x800000` 을 이 레지스터로 걸러 읽는 자리라 헷갈리기 쉽다.
- **test.exe 에서 타나 — 안 탄다**:
  - `0x800`: `LdrpInitializeProcess +0x1766 CMP [UseWOW64],R15D` → `+0x176d JZ +0x1806` (0 이면 `LdrpLoadWow64` 를 건너뜀) `[정적]`. `UseWOW64` 값은 **run44 사진에 이미 있다** — 0 (flow.md 2.4 `LdrpInitializeProcess` run44 "갈림 값" `[관찰]`, 짐 지도 3.8 `UseWOW64` 줄 사진 `끝까지 0*`). EPROCESS `WoW64Process : (null)` (`ms/logs/04_baggage-read_P1a.txt:160` · `_P2.txt:160`) 도 같은 쪽. → **`LdrpLoadWow64` 안 불림** `[정적+관찰]`.
  - `0x800000`: `LdrLoadEnclaveModule` 은 내보내기 함수로 이 빌드 ntdll 안에 직접 부르는 곳이 없고 (결과서), `LdrpEnclaveAddDependentModule` 은 `LdrpEnclaveAddDelayloadModules +0x9e` · `LdrpEnclaveAddForwarderModules +0xf7` 에서만. sample 에 enclave 없음 (`sample/` grep 0건) `[소스]` → 안 탐 `[정적+소스]`.
  - 그래서 test.exe 의 문맥 `+0x20`: exe 는 `LdrpInitializeProcess +0x1328 MOV [RSP+0x2d0],0x200` → `+0x1a8e` / `+0x1a94` `OR …,R12D(1)` = `0x201` (결과서), DLL 들은 `+0x1e2` 마스크로 세 비트가 0 → `LdrpMinimalMapModule` 은 `+0x174 NtMapViewOfSection` 길 (Ex 아님) `[정적 조건]`. 문맥 값 자체는 사진에 없다 → 동적 1.

### Q70-④ `0x40000036` — PDB 에 이름 없음, 글은 "fixup 은 자동" (결과서 Q70 ④)

- **이름** `[정적]`: ntdll PDB 의 타입 · 공개 이름 (`llvm-pdbutil dump -types` · `-publics`) 에 `STATUS_IMAGE` · `DIFFERENT_BASE` 0건, `strings` 로도 없음, ntdll 본문 문자열에도 없음 (결과서). **"PDB 에 이름 없음"**.
  - `STATUS_IMAGE_AT_DIFFERENT_BASE` 는 기억에서 나온 이름이다 `[추측: 기억]`. 문서엔 "`0x40000036` (이름은 PDB 에 없음)" 로 적는다.
- **뜻** `[정적: 메시지 표]`: ntdll 자기 메시지 표 (`RT_MESSAGETABLE`, 영어 — 번호 → 글만, 이름은 없음. 결과서가 작은 파서로 읽음):
  - `0x40000036` = "{Image Relocated} An image file was mapped at a different address from the one specified in the image file but **fixups will still be automatically performed** on the image."
  - 비교 `0x40000003` = "{Image Relocated} An image file could not be mapped at the address specified in the image file. **Local fixups must be performed** on this image." (ko-KR `ntdll.dll.mui` 도 같은 뜻)
- **코드에서 쓰이는 법** `[정적]` (run38 줄 그대로 + 글):
  - 리터럴 `0x40000036` 은 ntdll 에 없고 `0x40000003 + 0x33` 모양으로만 나온다: `LdrpMinimalMapModule +0x192 SUB ECX,0x40000003` → `+0x198 JZ` · `+0x19e SUB ECX,0xb` (`0x4000000e`) · `+0x1a7 CMP ECX,0x28` → `+0x1aa JZ` (둘 다 같은 `+0x288` 재시도 검사 갈래), `LdrpCompleteMapModule +0xe4 SUB` · `+0xec CMP ECX,0x33` → `+0xef JZ`.
  - **갈림**: `LdrpCompleteMapModule +0x1b4 CMP R12D,0x40000003` → `+0x1bb JZ` 는 **정확히 `0x40000003` 일 때만** `+0x31b LdrpRelocateImage`. `0x40000036` 은 `+0x1c1` 로 내려가 재배치를 안 한다 — 글 ("fixup 은 자동") 과 맞다.
- **run102 (Q62a) 와 맞춰 보기**: run102 는 커널 쪽에서 이 값이 나오는 자리를 `MiMapViewOfImageSection +0xc1b ~ +0xc82` ("`0x40000003` 갈래 안에서 `BL` 비트 1 · 3 이 같이 켜짐", `BL` 출처 `[공백]`) 로 적었고, flow.md 2.6 은 "`0x40000036` 은 `0x40000003` 갈래 안의 다른 비트 조합이라 '섹션 만들 때 고쳐 둠' 의 표시가 아니다" 로 적었다.
  - **어긋나지 않는다.** 글은 "다른 주소에 붙었다" (= `0x40000003` 갈래 안 — run102 와 같음) + "fixup 은 자동으로 **된다**" (= 유저가 할 일 아님 — ntdll `+0x1b4` 와 같음) 다. "섹션 만들 때 이미 고쳐 둠" 이라는 뜻은 아니다.
  - 남는 것: 자동 fixup 을 **커널 어느 줄이 하나**, `BL` 비트 1 · 3 이 무엇인가 — run102 정적 ⑥ 과 같은 질문 `[공백]`.

### 사진 로그 대조 (run44, 새로 돌린 것 아님 — flow-writer)

| 정적 | 사진 값 | 맞나 |
|---|---|---|
| `0x800` 은 `UseWOW64 ≠ 0` 일 때만 (`+0x176d`) | `UseWOW64` 0 (flow.md 2.4 run44 "갈림 값" `[관찰]`, 짐 지도 3.8 사진 `끝까지 0*`), EPROCESS `WoW64Process : (null)` (`04_baggage-read_P1a.txt:160` · `_P2.txt:160`), `Wow64VaSpace4Gb : 0y0` (`_P1a.txt:106`) | **맞음** — `LdrpLoadWow64` 안 탐 → `0x800` 안 생김 |
| 실패 길 (State 4 그대로 · `−3` 없음) | 노드 `State` 는 exe P1a 9 → P1b 7 → P2 9, dll1 · dll2 P1b 7 → P2 9, ntdll · k32 · kb 9 (짐 지도 3.6 머리) — 음수 State 없음 | **맞음** — test.exe 는 snap 실패 길을 안 탔다 |
| 문맥 `+0x20` 비트 (`0x201` / DLL 은 세 비트 0) | 로드 문맥은 사진에 칸이 없다 | 못 봄 — 동적 1 |

### 호출 관계

```
(snap 실패 길 — test.exe 는 안 탐)
LdrpLoadDllInternal                                  상태 변수 = [RSP+0xc8] (+0x1c0 / +0x1c8 → 문맥 +0x28)
 ├─ +0x1f2  LdrpFindOrPrepareLoadingModule           +0x427 새 문맥 +0x20 = flags | 0x8000
 ├─ +0x21a  LdrpDrainWorkQueue
 │           └─ LdrpProcessWork                       +0x20 상태 칸 < 0 → 할 일 없이 끝
 │               └─ +0x131  LdrpSnapModule            실패: +0xbc1 EAX = 음수, State 안 씀 (4 그대로)
 │                  +0x13b  *(ctx+0x28) = 음수
 │   (직접 길: LdrpMapAndSnapDependency +0x4ac State = 4 → +0x4c2 LdrpSnapModule → +0x6d *(ctx+0x28) = 음수)
 ├─ +0x262  LdrpCondenseGraph → LdrpCondenseGraphRecurse   +0x96 State ≠ 5 → +0xa3 State 6 건너뜀
 │                                                         (+0x148 자식 −3 → +0x14e 부모 −3: 전파만)
 │  +0x277  상태 < 0 → +0x2b1   (+0x284 LdrpPrepareModuleForExecution 건너뜀)
 ├─ +0x2c8  LdrpFreeLoadContextOfNode
 ├─ +0x2eb  LdrpDecrementModuleLoadCountEx(항목, 0)
 │           ├─ +0x92  LdrpDecrementNodeLoadCountLockHeld(노드, 0, &지역)
 │           └─ +0xb4  LdrpUnloadNode (지역 ≠ 0)       State 4 → +0x2a6 → +0x138 길 → (조건) +0x1fb State −2 ?
 └─ +0x2f3  LdrpDereferenceModule(항목)

(ctx+0x20 비트가 태어나는 곳)
LdrpInitializeProcess
 │  +0x1766 UseWOW64 == 0 → +0x176d JZ +0x1806         ← test.exe (사진 0)
 └─ +0x17c6  LdrpLoadWow64
              └─ +0xad  LdrpLoadDll(…, R8D = 0x800 (+0x98), …)
                         └─ +0x100  LdrpLoadDllInternal → +0x1f2 LdrpFindOrPrepareLoadingModule → +0x427 [ctx+0x20]
LdrLoadEnclaveModule (내보내기, ntdll 안 부르는 곳 없음)
 │  +0xe5  [RSP+0x40] = 0x800000
 ├─ +0xf5   LdrpPreprocessDllName
 └─ +0x138  LdrpAllocatePlaceHolder                  +0x81 [ctx+0x20]
LdrpEnclaveAddDependentModule (← LdrpEnclaveAddDelayloadModules +0x9e · LdrpEnclaveAddForwarderModules +0xf7)
 │  +0x9a  [RSP+0x40] = 0x800000
 ├─ +0xb3   LdrpPreprocessDllName
 └─ +0xee   LdrpFindOrPrepareEnclaveModule
LdrpLoadDependentModuleInternal (의존 DLL)
    +0x1de ESI = importer [ctx+0x20] → +0x1e2 AND 0x1800800 → … → +0x10ec 새 [ctx+0x20]

(맵 결과)
LdrpMinimalMapModule
 ├─ +0x179  NtMapViewOfSection          ← test.exe 길 (세 비트 0)
 └─ +0x371  NtMapViewOfSectionEx        (+0x11d 0x800 · +0xf2 0x800000)
LdrpCompleteMapModule
    +0xe4 / +0xec  0x40000003 · 0x40000036 → 같은 갈래
    +0x1b4  정확히 0x40000003 → +0x320 LdrpRelocateImage    (0x40000036 은 +0x1c1, 재배치 안 함)
```

## 확인된 것

- `[정적]` **노드 `State = −3` 을 처음 쓰는 줄은 ntdll 에 없다.** −3 을 쓰는 줄은 전파 `LdrpCondenseGraphRecurse +0x14e` 하나, 읽는 줄은 `+0x148` 하나 (ntdll 전체 `.text` `objdump` 꼴). run36 · sast/31 `[공백]` 닫힘.
- `[정적]` **snap 실패면 노드는 `State 4` 그대로**, 실패는 `*(ctx+0x28)` (= 바깥 `LdrpLoadDllInternal` 상태 변수) 로만 간다 — `LdrpMapAndSnapDependency +0x6d` · `LdrpProcessWork +0x13b` (· forwarder 곁가지 `LdrpSnapModule +0xe03`).
- `[정적]` 바깥 `LdrpLoadDllInternal +0x277` 이 음수를 보고 **`LdrpPrepareModuleForExecution` 을 건너뛰고** 뒤처리 (`+0x2e6 LdrpDecrementModuleLoadCountEx` → 지역 ≠ 0 이면 `+0xaf LdrpUnloadNode`, `+0x2ee LdrpDereferenceModule`) 로 간다. 그 앞 `LdrpCondenseGraph` 는 돌지만 State 4 노드엔 6 을 안 쓴다.
- `[정적]` **`ctx+0x20` 비트 `0x800` · `0x800000` 을 `OR`/`BTS` 로 켜는 줄은 없다.** `0x800` = `LdrpLoadWow64 +0x98` 이 `LdrpLoadDll` 에 넘기는 인자, `0x800000` = `LdrLoadEnclaveModule +0xe5` · `LdrpEnclaveAddDependentModule +0x9a`. 문맥을 만들 때 `LdrpFindOrPrepareLoadingModule +0x427` (enclave 는 `LdrpAllocatePlaceHolder +0x81`) 이 넣고, 의존 DLL 은 `LdrpLoadDependentModuleInternal +0x1e2 AND ESI,0x1800800` 로 물려받는다. 사용자 `LdrLoadDll` 로는 못 켠다.
- `[정적+관찰]` **test.exe 는 `0x800` 길을 안 탄다** — `+0x176d` 의 `UseWOW64` 가 run44 사진 0. `[정적+소스]` `0x800000` 도 안 탄다 (enclave 없음).
- `[정적]` **`0x40000036` 은 PDB 에 이름이 없다.** ntdll 메시지 표 글 = "다른 주소에 붙었지만 fixup 은 자동". ntdll 은 이 값으로 재배치를 안 한다 (`LdrpCompleteMapModule +0x1b4`). run102 커널 쪽 줄과 어긋나지 않는다.

## 결과서와 다르게 적은 곳 · 보탠 것

- **다르게 적은 곳 하나**: 결과서 Q70-③ "그 조건" 의 **`UseWOW64` 값 `[추측: 값 안 봄]` → `[VM]`** → run44 사진에 이미 있다. 0 (flow.md 2.4 run44 "갈림 값" `[관찰]`, 짐 지도 3.8 `끝까지 0*`) 이고 EPROCESS `WoW64Process` 도 null (`04_baggage-read_P1a.txt:160`). 그래서 "`0x800` 길 안 탐" 은 `[정적+관찰]` 로 적었다. 결과서 동적 "`ntdll+0x8db6a` 에서 `dd ntdll!UseWOW64`" 는 필요 없어졌다 (확인용으로만).
- **보탠 것** (flow-writer):
  - 실패 길 "State 4 그대로" 를 run44 노드 State 사진과 맞춰 봄 (음수 State 없음) — test.exe 가 이 길을 안 탔다는 것과 맞다.
  - 전수 표의 `LdrpInitializeProcess +0x16b0` (7) · `+0x1735` (9) 는 run105 App Verifier 묶음 (`+0x1568 ~ +0x1753`) 오프셋 안이다 `[정적: 오프셋 범위]`.
  - `LdrpMinimalMapModule` 의 `Ex` 갈림 줄은 결과서 "`+0x141 TEST ECX,ECX`" 가 아니라 **`+0x13f TEST ECX,ECX` · `+0x141 JNZ`** (`.asm`). run38 이 적은 "`+0x13f TEST ECX,ECX` → `+0x141 JNZ`" 와 같다.
  - 결과서가 "`.asm` 없음" 이라 한 `LdrpUnloadNode` 는 지금은 `.asm` 이 있어 줄만 맞춰 봤다 (위 환경). 같다.
  - `0x40000036` 글을 run102 · flow.md 2.6 의 "고쳐 둠의 표시가 아니다" 와 맞춰 봄 (위 ④) — 어긋남 없음.

## 정정

- 확정 사실을 고친 곳은 없다 → 이전 run 문서 맨 위 정정 줄은 안 넣었다. `[공백]` · `[추측]` 으로 두었던 것들이 `[정적]` · `[정적+관찰]` 로 바뀌었을 뿐이다:
  - flow.md 2.6 `LdrpSnapModule` 절 · 2.7 `LdrpCondenseGraphRecurse` 절 "`−3` 을 처음 쓰는 곳은 못 찾았다 `[공백]`" (run36) → "**없다**" `[정적]`.
  - flow.md 2.6 "두 비트를 켜는 줄은 뽑아 둔 함수에 없다 → Q70" (run38) → "켜는 줄 없음, 뿌리 셋이 인자로 넣음" `[정적]`, test.exe 안 탐 `[정적+관찰]`.
  - flow.md 2.6 "`0x40000036` 의 이름은 못 정함 (`STATUS_IMAGE_AT_DIFFERENT_BASE` 기억 `[추측]`)" (run38) → "PDB 에 이름 없음 (이름은 여전히 `[추측: 기억]`), 글 확인" `[정적]`.
  - flow.md 2.4 "`UseWOW64` … x64 test.exe 면 0 `[추측]`" (`+0x5f2` 줄) 은 이미 run44 사진 `[관찰]` 이 있었다 — 이번에 그 줄에 사진 근거를 이어 적었다.

## 이전 run 에서 닫은 것

- run36 · sast/31 `[공백]` **Q69** — ① −3 처음 쓰는 줄 (없음) ② 상태 칸 뒤 (State 로 안 바뀜, 해제 길). 닫힘. 남은 건 `LdrpUnloadNode` State 4 갈래 본문 (아래 정적 1).
- run38 · sast/33 "정적으로 못 정한 것" **Q70** — ③ 켜는 줄 (`LdrpLoadWow64` · enclave, 물려받기) ④ 이름 (PDB 에 없음). 닫힘.
- flow.md 4장 2.6 run26 "이름·뜻 못 정한 것" 줄의 `+0x20` 비트 `0x8` · `0x100` · `0x800000` · `0x1000000` — 이름은 여전히 없지만 **출처**가 생겼다: `0x8` · `0x100` (· `0x40` · `0x80` · `0x400000`) 는 `LdrLoadDll +0x64 ~ +0xa5` 의 사용자 `DllCharacteristics` 옮기기, `0x800000` 은 enclave, `0x1000000` 은 `LdrpInitializeImportRedirection +0xc2` (`0x1000001`) `[정적: 결과서 objdump]`.

## 아직 확인 필요

### 동적 (`[VM]`, 결과서 "동적으로 확인할 것" — 사진으로 줄인 만큼 고침)

1. **test.exe 문맥 `+0x20` 실제 값** (정상 길 확인, 기존 멈춤점과 같은 실행): `ntdll+0xa30e` (`LdrpMinimalMapModule +0xee`, `RSI` = 문맥) 에서 `dd @rsi+0x20 L1` — `0x800` · `0x800000` 비트가 0 인지, 그러면 `ntdll+0xa394` (`+0x174`) 로 `NtMapViewOfSection`. run73 의 `ntdll+0xa399 r eax` (`0` / `0x40000003` / `0x40000036`) 와 같은 자리에서 같이 본다 (Q20 · Q23).
2. (선택, 필수 아님) **snap 실패를 일부러 만든 변종** (없는 export 를 import 하는 dll) 이 있어야 Q69 길이 선다. 볼 자리: `ntdll+0x21562` (`LdrpMapAndSnapDependency +0x4c2`) 또는 `ntdll+0x17271` (`LdrpProcessWork +0x131`) `r esi` 음수 → `ntdll+0x1fea7` (`LdrpLoadDllInternal +0x277`) 에서 `dd @rbx L1` 음수 · 노드 State 가 4 로 남는지 → `ntdll+0x86431` (`LdrpDecrementModuleLoadCountEx +0xa1`) 뒤 `LdrpUnloadNode` 에 들어가나, State 가 −2 가 되나. 변종 빌드는 sample 을 바꾸는 일이라 사용자 결정.
- 결과서의 `ntdll+0x8db6a` `dd ntdll!UseWOW64` 는 사진으로 답이 있어 뺐다.

### 정적 (결과서 "다음 요청 후보" + 보탬)

1. (낮음 — 초기화 실패 길, 짐 지도 범위 밖) `LdrpUnloadNode` (rva `0x865c0`, 이제 `.asm` 있음) `+0x138 ~ +0x1fb` 갈래 · `LdrpDecrementNodeLoadCountLockHeld` (rva `0x86530`) — State 4 노드가 −2 가 되는지, 지역 칸이 언제 ≠ 0 인지.
2. (낮음) `LdrpFindOrPrepareEnclaveModule` · `LdrpCreatePendingEnclaveModule` — `0x800000` 이 `LdrpAllocatePlaceHolder` 의 `R8D` 로 넘어가는 길 끝까지 (이번엔 `LdrLoadEnclaveModule +0x133` 쪽만).
3. (보탬, 작음 — 커널 쪽) `0x40000036` 의 "자동 fixup" 을 커널 어느 줄이 하나, `MiMapViewOfImageSection +0xc1b ~ +0xc82` `BL` 비트 1 · 3 출처 — **run102 정적 ⑥ 과 같은 질문** (새 번호 없이 거기 붙이면 됨).

### PE_emulator 쪽 (결과서 "지금 우리 맞나" 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

- Q69-① `SnapError` · State −3 을 쓰는 코드 없음 — **맞음** (만들 것도 없음. 로더 구간은 진짜 ntdll 코드가 돈다).
- Q69-② 실패 길 — **해당 없음** (test.exe 정상 흐름엔 snap 실패 없음). `Src/ntdll/LdrInit.cpp:6457` `LdrpFreeLoadContextOfNode` 는 빈 몸통 ("공사 중") — 실패 길을 만들게 되면 위 사슬 (진짜 코드) 에 맡기는 쪽.
- Q70-③ exe 문맥 `0x200` (`LdrInit.cpp:3945~3951`) → `|= 1` (`:5383~5388`), `0x800` · `0x800000` 안 켬 — **맞음**. 이름 겹침: `Src/nt/AddressSpace.cpp:663` `LOADER_VIEW_ALLOCATION_TYPE = 0x800000` 은 할당 종류 (`LdrpMinimalMapModule +0xa7`) 라 맞고, 문맥 비트가 아니다.
- Q70-④ `0x40000036` — **맞음 / 뜻만 보태면 됨** (상수 주석에 위 글 한 줄). 우리가 `0x40000036` 을 돌려주면 ntdll 은 재배치를 안 하고 (`+0x1b4`), `0x40000003` 이면 한다. `Src/nt/AddressSpace.cpp:752~759` `TODO(VM)` (커널이 언제 `0x40000036` 을 고르나) 는 커널 쪽 질문이라 그대로 남는다 (위 정적 3).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntdll 줄에 run106, 근거 로그 (run106) 한 줄), 그림 2 (`jm1` 상자 글, 제목 run106), 2.4 (절 머리 · `+0x5f2` `UseWOW64` 줄 아래 사진 근거 한 줄), 2.6 (절 머리 · run23 `+0x10ec` 줄 아래 마스크 `0x1800800` 출처 · 로드 문맥 `+0x20` 비트 줄 · 붙이기 시스템 콜 줄 · 결과 코드 줄 · `CompleteMap` 5번 줄 · `LdrpSnapModule` −3 줄 · **새 하위 절 "snap 실패 뒤"** (`#### 일감 한 바퀴` 앞)), 2.7 (절 머리 · `LdrpCondenseGraphRecurse` −3 줄), 4장 (2.4 "뒤쪽 실행 값" 줄의 `UseWOW64` 해결됨 · 2.6 · 2.7 Q69 줄 해결됨 · 2.6 run26 이름 줄 비트 출처 · 2.6 Q70 줄 해결됨 · run106 새 줄 셋 — 동적 · 정적 · PE_emulator), 4.1 (`LdrpMinimalMapModule → NtMapViewOfSection` 줄), 5장 (run106 · sast/100).
- baggage.md: 기준 (run106 줄 · 판 쉰여섯째), 3.6 머리 메모 (State 쓰는 줄 전수 · −3 처음 쓰는 줄 없음 · 실패면 4 그대로), 8장 (run106). **칸 수 · 줄 수 · 2장 숫자 그대로** (실패 길은 test.exe 가 안 타서 줄로 안 더함. 문맥 `+0x20` 은 체크리스트 밖 힙 개체 칸이라 표에 안 넣음).

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 중 디버거 칸을 읽는 것은 없다 `[정적: 읽은 줄 안에서]`. (`LdrpLoadWow64` 뒤 `+0x17dd LdrpDoDebuggerBreak` 는 WoW64 갈래라 test.exe 와 무관 — run19.)
- 도구 (결과서 "다음 요청 후보" 3, 하네스 제안 — 오케스트레이터가 사용자 검토 뒤): "ntdll **전체**에서 그 줄 찾기" 를 `objdump -d` 한 번 + `_index.tsv` 로 함수 이름 붙이기로 했다. `.asm` 모음 (683 / 4594) 으로는 전수가 안 된다. `ghidra-headless-export/scripts/` 에 `objdump` 풀이 · 함수+오프셋 붙이기 도우미가 있으면 sast 마다 임시 스크립트를 안 짜도 된다 (run41 · run72 · run104 · run105 · run106 같은 방식이 이어짐).
- pitfalls 제안 없음.

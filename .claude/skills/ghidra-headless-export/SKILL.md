---
name: ghidra-headless-export
description: Ghidra headless로 MS 바이너리(ntoskrnl.exe, ntdll.dll, kernel32.dll, kernelbase.dll 등)를 PDB 심볼과 함께 분석하고, 필요한 함수만 디컴파일(C)·디스어셈블·호출 지점 표(_calls.tsv)로 뽑아 ms/decomp, ms/disassemble 에 정리하는 절차. "디컴파일 해줘", "정적 분석", "함수 코드 뽑아줘", "기드라로 봐줘", "이 함수 어디서 부르는지", "PDB 받아줘", "+0x 오프셋이 코드 어디냐" 같은 요청이나 _workspace/sast 요청서를 처리할 때 쓴다.
allowed-tools: Bash Read Write Glob Grep
---

# ghidra-headless-export Skill

MS 바이너리를 **PDB와 함께** Ghidra로 분석해서, 요청서가 묻는 함수들의 디컴파일 코드·디스어셈블·호출 지점을 파일로 남긴다.
이 결과는 `[정적]` 근거가 된다. "코드에 이렇게 쓰여 있다"이지, "실행 때 이 길로 갔다"는 아니다.

## 고정값

| 항목 | 값 |
|---|---|
| Ghidra | `GHIDRA_HOME` (기본 `~/tools/ghidra_11.3.2_PUBLIC_20250415/ghidra_11.3.2_PUBLIC`, 11.3.2) |
| 바이너리 | `ms/bin/` 아래 (하위 폴더 이름에 띄어쓰기 있음. 스크립트가 `*/System32/<이름>` 을 찾아준다) |
| PDB | `ms/sym/<바이너리이름>.pdb` (예: `ms/sym/ntoskrnl.exe.pdb`) |
| Ghidra 프로젝트 | `ms/ghidra/<모듈>` (모듈 = 소문자 이름, 예 `ntoskrnl`, `ntdll`) |
| 출력 | `ms/decomp/<모듈>/<함수>.c`, `ms/disassemble/<모듈>/<함수>.asm`, `ms/disassemble/<모듈>/_calls.tsv`, `ms/decomp/<모듈>/_index.tsv` |
| 기준 빌드 | 26100.9457. 커널 pdb GUID+Age = `C29EBFB06B78B3C020DCA66D99713F9E1` |

스크립트는 전부 `.claude/skills/ghidra-headless-export/scripts/` 에 있다.

## Workflow

1. **요청서 읽기**: `_workspace/sast/` 에서 번호가 가장 큰 `NN_키워드.md` (이름에 `_result` 가 없는 것)를 읽는다. 모듈, 함수 이름/정규식, depth, 답해야 할 질문을 뽑는다.
2. **PDB 준비** (모듈마다 처음 한 번):
   ```bash
   python3 .claude/skills/ghidra-headless-export/scripts/fetch_pdb.py ntoskrnl.exe ntdll.dll
   ```
   - 커널은 출력된 `guid` 가 위 기준 GUID와 같은지 확인한다. 다르면 **멈추고** 빌드가 다르다고 보고한다.
   - 이어서 **별칭 표**를 만든다 (몇 초):
     ```bash
     python3 .claude/skills/ghidra-headless-export/scripts/pdb_aliases.py ntoskrnl.exe ntdll.dll
     ```
     → `ms/sym/<바이너리>.aliases.tsv`. 한 주소에 함수 이름이 여러 개인 곳(ICF 등) 전부. 커밋하지 않는다.
3. **분석** (모듈마다 처음 한 번). 아래 셋이 **다 있으면** 이미 분석된 것으로 보고 건너뛴다:
   - `ms/ghidra/<모듈>.gpr` (프로젝트)
   - `ms/decomp/<모듈>/_index.tsv` (ExportIndex가 끝까지 돌았다는 증거)
   - `ms/ghidra/<모듈>.analyze.log` 의 `SetPdb:` 줄 (PDB가 붙었다는 증거)
   - `.gpr` 은 있는데 나머지 중 하나라도 없으면 = **이전 analyze가 실패했거나 아직 도는 중**이다. 다시 analyze 하거나 지우지 말고, 로그(`ms/ghidra/<모듈>.analyze.log`, `.stdout`)·프로세스(`ps`)·잠금 파일(`<모듈>.lock`)을 보고 원인을 결과서에 쓴 뒤 멈춘다. 다시 할지는 사용자가 정한다 (오케스트레이터가 묻는다).
   ```bash
   bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh analyze ntdll.dll
   ```
   - ntoskrnl 은 오래 걸린다(수십 분~수 시간). `run_in_background` 로 돌리고 `ms/ghidra/<모듈>.analyze.log` 를 본다.
   - 끝나면 `ms/decomp/<모듈>/_index.tsv` (함수 전체 목록)가 생긴다.
   - **지도(색인)**: analyze 가 끝난 모듈은 뿌리에서 지도를 한 번 만들어 둔다 (몇 초). 뿌리는 커널 `NtCreateUserProcess`, ntdll `LdrInitializeThunk`.
     ```bash
     bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh map ntoskrnl.exe NtCreateUserProcess
     bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh map ntdll.dll LdrInitializeThunk
     ```
     → `ms/disassemble/<모듈>/_map/<뿌리>_{edges,nodes}.tsv`, `_summary.txt`. 형식은 [reference.md](reference.md) "지도".
     - 지도는 **읽을 목록이 아니라 색인**이다 (커널은 끝까지 1만 개가 넘는다, sast/05). "이 함수는 뿌리에서 몇 단계, 누가 부르나, caller 몇 곳"을 찾는 데 쓴다.
     - 스크립트(`ExportFunctions`·`CallGraphMap`)를 바꾼 뒤엔 지도를 다시 만든다.
   - **누가 읽고 쓰나**: 전역 변수·함수 주소를 누가 읽고 쓰는지(호출로 안 닿는 쓰기·포인터 표 포함)는 `run_ghidra.sh refs <바이너리> <이름|0xRVA> [바이트수]` → `ms/disassemble/<모듈>/_refs/` (형식은 reference.md "누가 읽고 쓰나").
4. **이름 찾기·주변 보기**: 요청서 함수 이름이 정확하지 않으면 `_index.tsv` 를 grep 해서 실제 이름을 고른다. **`_index.tsv` 에 없으면** `python3 .claude/skills/ghidra-headless-export/scripts/pdb_aliases.py --where <바이너리> <이름>` 으로 PDB 이름을 찾는다 — Ghidra 가 같은 함수를 다른 이름(내보내기 이름 등)으로 잡았으면 그 이름이 나온다 (예: `KeWaitForSingleObject` → Ghidra `KeWaitForMutexObject`). 문서에는 PDB 이름을 쓰고 Ghidra 이름을 한 번 밝힌다. 대상 함수의 위치는 지도에서 본다: `_nodes.tsv` 의 깊이·caller 수·상태, `_edges.tsv` 에서 caller 열이 대상인 줄(= 직접 부르는 함수)과 callee 열이 대상인 줄(= 누가 부르나). 요청서 범위를 넓혀야 할 것 같으면(예: 일이 한두 단계 아래에서 일어남) 지도로 길을 먼저 찾고, 넓힌 이유를 결과서에 쓴다.
5. **뽑기**:
   ```bash
   bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh export ntdll.dll '^(LdrpLoadDllInternal|LdrpPrepareModuleForExecution)$' 1
   ```
   - 정규식은 함수 이름 기준. depth=1 이면 그 함수가 부르는 함수까지 같이 뽑는다. 넓게 잡지 말고 요청서 범위만.
6. **호출 지점 읽기**: `_calls.tsv` 의 `ret_off` 는 WinDbg 콜스택의 `caller+0x...` 와 같은 값이다. 동적 로그의 오프셋과 맞춰 본다.
7. **결과서 쓰기**: `_workspace/sast/NN_키워드_result.md` (형식은 [reference.md](reference.md)).
8. **자체 검증**: 아래 "버전 확인용 알려진 사실"과 최소 1개를 대조한다.

## 버전 확인용 알려진 사실 (01_sprint 동적 관찰)

처음 분석한 모듈이면 이 중 해당하는 것을 `_calls.tsv` 로 확인한다.
- 다 맞으면: 스크립트의 오프셋 계산과 바이너리 둘 다 믿을 수 있다.
- ntdll 줄은 sast/02(2026-10-19)에서 정적으로 대조해 전부 맞았다 (관찰 값 = `ret_off`).
- 어긋나면: 먼저 스크립트(오프셋 계산, 함수 시작 주소)를 의심해 asm 파일로 직접 확인한다. 그래도 다르면 바이너리가 타깃 VM 것과 다를 수 있다고 보고한다. (빌드 확정은 커널 PDB GUID로 한다)

| 모듈 | caller | ret_off | callee |
|---|---|---|---|
| ntoskrnl | `NtCreateUserProcess` | `+0xc36` | `PspAllocateProcess` |
| ntoskrnl | `PspInsertThread` | `+0x807` | `PspCallProcessNotifyRoutines` |
| ntdll | `LdrpInitializeProcess` | `+0x1bbb` | `LdrpInitializeGraphRecurse` (call 명령은 `+0x1bb6`) |
| ntdll | `LdrpInitializeNode` | `+0x16b` / `+0x19c` | `LdrpCallTlsInitializers` / `LdrpCallInitRoutine` |
| ntdll | `LdrInitializeThunk` | `+0xe` | `LdrpInitialize` (이 함수가 `+0x3c` 에서 `JMP` 꼬리호출로 `LdrpInitializeInternal` 에 넘겨서, 콜스택엔 `LdrpInitialize` 프레임이 안 보인다) |
| ntdll | `LdrpInitializeInternal` | `+0x5a` | `_LdrpInitialize` |
| ntdll | `_LdrpInitialize` | `+0x16c` | `LdrpInitializeProcess` (01_sprint 문서엔 caller가 `LdrpInitialize` 로 적혀 있음. 밑줄 붙은 `_LdrpInitialize` 가 맞다. `LdrpInitialize` 는 99바이트라 `+0x16c` 가 없다) |
| ntdll | `LdrpLoadDll` | `+0x100` | `LdrpLoadDllInternal` |

## 에러가 나면

- 스크립트(`scripts/`)는 **고치지 않는다.** 문제가 있으면 결과서 맨 위 "스크립트 문제" 절에 파일·줄·증상·원인 추정·고칠 방법 제안을 쓴다. 고치는 건 오케스트레이터가 사용자 검토 뒤에 한다.
- `java` 를 못 찾음 / 버전 에러: Ghidra 11.x는 JDK 21이 필요하다. `java -version` 확인 결과를 보고한다 (설치는 사용자에게 맡긴다).
- `SetPdb.java` 컴파일 에러: Ghidra 버전이 다를 수 있다. `$GHIDRA_HOME/Ghidra/Features/PDB` 안의 `PdbUniversalAnalyzer` 메서드 이름을 확인해 "스크립트 문제"로 보고한다.
- `run_ghidra.sh` 종료 코드 4: 로그에 `SCRIPT ERROR` 가 있다 (Ghidra 자체는 끝났지만 SetPdb·ExportIndex·ExportFunctions 중 하나가 죽음). 결과물을 쓰지 말고 출력된 에러 줄을 결과서 "스크립트 문제" 절에 옮긴다.
- 메모리 부족(`OutOfMemoryError`): `MAXMEM=16G bash run_ghidra.sh analyze ...`
- `osgi.ee=UNKNOWN` / `ClassNotFoundException: SetPdb` 로 스크립트가 로딩 단계에서 죽음: Ghidra 11.3.2는 JDK 21이 필요하다 (`$GHIDRA_HOME/support/launch.properties` 의 `JAVA_HOME_OVERRIDE`).
  - **JDK를 바꾼 뒤엔 `~/.config/ghidra/<Ghidra버전>/osgi/compiled-bundles/` 의 옛 컴파일 캐시를 지운다.** 안 지우면 옛 JDK로 만든 캐시를 계속 써서 같은 에러가 난다 (sast/01·02, 2026-10-19).
- 함수 이름이 `FUN_xxxxxxxx` 로만 나옴: PDB가 안 붙은 것. `ms/ghidra/<모듈>.analyze.log` 에서 PDB 관련 줄을 찾는다.
- 자세한 출력 형식과 결과서 양식: [reference.md](reference.md)

# 05 callgraph-size — 정적 분석 요청 (실험: 지도 크기 재기)

- 날짜: 2026-10-21
- 스프린트: 02_sprint
- 대기열: **Q37** — 사용자 결정으로 Q3 앞에서 처리 (`_workspace/plan.md` 결정 기록 2026-10-21)
- 관련: 사용자 질문 "정적으로 전체 호출 지도를 먼저 뽑고 함수 하나씩 의미를 보는 게 더 빠를까?" → 먼저 지도가 얼마나 큰지 잰다

## 이번 요청만의 규칙

1. **실험용 스크립트를 쓴다 (하네스 밖).** `run_ghidra.sh` 는 export(디컴파일) 전용이라 이번엔 쓰지 않는다.
   - 스크립트: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/3f4d15aa-16f7-4279-8177-858e012325c7/scratchpad/exp/CallGraphMap.java` (오케스트레이터가 씀, JDK 21 컴파일 확인)
   - 디컴파일 안 함. 명령을 읽어 호출만 모은다. 머리 주석에 출력 형식과 "끊김 이유" 목록이 있다.
   - **스크립트를 고치지 않는다.** 문제가 보이면 결과서 "스크립트 문제" 절에 적는다 (하네스 밖이지만 같은 원칙).
   - `.claude/` 도 고치지 않는다.
2. analyze 는 다시 하지 않는다 (두 모듈 다 끝나 있음). `-process … -noanalysis -readOnly` 로 기존 프로젝트에서 돌린다.
3. 실행 (레포 루트에서, 모듈마다 한 번):
   ```bash
   GH=~/tools/ghidra_11.3.2_PUBLIC_20250415/ghidra_11.3.2_PUBLIC
   EXP=/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/3f4d15aa-16f7-4279-8177-858e012325c7/scratchpad/exp
   # 커널
   "$GH/support/launch.sh" fg jdk Ghidra-Headless 8G "-XX:ParallelGCThreads=2 -XX:CICompilerCount=2 -Djava.awt.headless=true" \
     ghidra.app.util.headless.AnalyzeHeadless ms/ghidra ntoskrnl -process ntoskrnl.exe -noanalysis -readOnly \
     -scriptPath "$EXP" -postScript CallGraphMap.java "$PWD/ms/disassemble/ntoskrnl/_map" NtCreateUserProcess 10 20 \
     -log ms/ghidra/ntoskrnl.map.log > ms/ghidra/ntoskrnl.map.stdout 2>&1
   # 유저
   "$GH/support/launch.sh" fg jdk Ghidra-Headless 8G "-XX:ParallelGCThreads=2 -XX:CICompilerCount=2 -Djava.awt.headless=true" \
     ghidra.app.util.headless.AnalyzeHeadless ms/ghidra ntdll -process ntdll.dll -noanalysis -readOnly \
     -scriptPath "$EXP" -postScript CallGraphMap.java "$PWD/ms/disassemble/ntdll/_map" LdrInitializeThunk 10 20 \
     -log ms/ghidra/ntdll.map.log > ms/ghidra/ntdll.map.stdout 2>&1
   ```
   - 끝나면 로그에서 `SCRIPT ERROR` 를 직접 grep 한다 (이 명령은 run_ghidra.sh 의 종료코드 4 검사를 거치지 않는다).
   - 너무 오래 걸리면(한 모듈 30분 넘게) 멈추고 그때까지의 상황을 보고한다.
   - 출력: `ms/disassemble/<모듈>/_map/<뿌리>_{edges.tsv,nodes.tsv,summary.txt}` (ms/ 아래라 커밋 안 됨)

## 알고 싶은 것

1. **지도 크기**: `_summary.txt` 의 숫자를 그대로.
   - (A) 거르지 않은 전체: 깊이 1~10 에서 새 함수가 몇 개씩 늘어나나. 어느 깊이에서 멈추나(포화), 끝까지 가면 모두 몇 개인가.
   - (B) "공용 도구"(프로그램 전체에서 caller 가 10 / 20 / 50곳 이상인 함수)에서 멈춘 지도: 기준마다 깊이별 개수와 합계.
2. **흐름도 함수가 지도에 들어오나** (가장 중요). 아래 함수마다: 지도에 있나 / 처음 나온 깊이 / 상태(펼침·공용 도구로 잘림·syscall 등). **공용 도구로 잘린 흐름도 함수가 있으면 맨 위에 크게** 적는다 (그러면 이 거르는 방법은 대표 지점을 잃는다는 뜻).
   - 커널: `NtCreateUserProcess`, `PspAllocateProcess`, `MmCreatePeb`, `PspAllocateThread`, `PspInsertProcess`, `PspInsertThread`, `PspCallProcessNotifyRoutines`, `PspBuildCreateProcessContext`, `IopCreateFile`, `MmCreateSpecialImageSection`, `PspCreateUserContext`, `PspMapThreadCreationFlags`, `PsTestProtectedProcessIncompatibility`, `PspCreateObjectHandle`, `PspUpdateCreateInfo`, `KeSuspendThread`, `KeReadyThread`, `MmCreateProcessAddressSpace`, `MmInitializeProcessAddressSpace`, `MiMapProcessExecutable`, `MmMapViewOfSectionEx`, `PsMapSystemDlls`, `PspMapSystemDll`, `PspSetupUserProcessAddressSpace`
   - 유저: `LdrInitializeThunk`, `LdrpInitialize`, `LdrpInitializeInternal`, `_LdrpInitialize`, `LdrpInitializeProcess`, `LdrpInitializeKernel32Functions`, `LdrLoadDll`, `LdrpLoadDll`, `LdrpLoadDllInternal`, `LdrpPrepareModuleForExecution`, `LdrpInitializeGraphRecurse`, `LdrpInitializeNode`, `LdrpCallTlsInitializers`, `LdrpCallInitRoutine`, `ImageTlsCallbackCaller`, `NtContinue`, `RtlUserThreadStart`
   - `RtlUserThreadStart` 은 호출이 아니라 `NtContinue` 로 "실행이 넘어가서" 닿는다고 짐작한다 `[추측]`. 지도에 없으면 그게 맞는지(어디서 끊겼나) 적는다.
3. **알려진 호출 줄이 지도(`_edges.tsv`)에 같은 오프셋으로 있나** (스크립트 검증):
   - 커널: `NtCreateUserProcess +0xc36 PspAllocateProcess`, `PspAllocateProcess +0x2095 MmInitializeProcessAddressSpace`, `MiMapProcessExecutable +0xdc PsMapSystemDlls`, `PspInsertThread +0x807 PspCallProcessNotifyRoutines`, `PspInsertThread` 조각 안 `KeForceResumeThread` (frag:)
   - 유저: `LdrInitializeThunk +0xe LdrpInitialize`, `LdrpInitialize +0x3c tail LdrpInitializeInternal`, `_LdrpInitialize +0x16c LdrpInitializeProcess`, `LdrpInitializeProcess +0x1bbb LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x19c LdrpCallInitRoutine`
4. **끊긴 곳**: 상태별 개수(expanded / helper / syscall / depth), 끊긴 끝 종류별 개수(indirect / indirect-cfg / external). 종류마다 예 3개씩.
5. **공용 도구로 잘린 것 중 caller 가 가장 많은 15개** (이름, caller 수) — 걸러진 게 정말 "잡일"인지 사람이 보고 판단하게.
6. **읽을 만한 크기인가**: 기준 20 지도에서 펼친 함수(expanded) 수. 수십 / 수백 / 수천 중 어디인가. 결과서 끝에 한 문단으로 의견 (단, 의견은 `[추측]`).
7. 걸린 시간 (모듈마다).

## 이미 아는 것

- `[정적]` (run9, run10) 직접 호출 수: `NtCreateUserProcess` 83줄, `PspAllocateProcess` 144줄. ntoskrnl 함수 34982개, ntdll 4593개 (`_index.tsv`).
- `[정적]` 별칭·CFG·조각·꼬리호출 처리는 하네스 묶음 B·F 와 같은 규칙 (스크립트 머리 주석).
- 오케스트레이터 짐작 `[추측]`: (A) 는 수천 개로 커진다. `NtCreateUserProcess` 지도엔 ntdll 이 안 나온다 (첫 스레드 출발은 호출이 아님).

## 받고 싶은 것

- 결과서: `_workspace/sast/05_callgraph-size_result.md` — 질문 1~7, 숫자는 파일에서 그대로.
- 동적 확인 거리는 없어도 된다.

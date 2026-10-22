# 09 create-process-internal — 정적 분석 결과

- 요청서: `_workspace/sast/09_create-process-internal.md` (Q5b)
- 모듈 / 바이너리: KernelBase (`ms/bin/.../System32/KernelBase.dll`), pdb guid_age=`72CCE506944DE0267C172AD64287F9BD1` (`ms/sym/KernelBase.dll.pdb.info.txt`)
- analyze: **다시 하지 않음.** `.gpr` + `_index.tsv`(6448개) + 로그 `SetPdb:` 줄 세 조건 다 있었다 (sast/08 때 끝난 것).
- 지도: `run_ghidra.sh map KernelBase.dll CreateProcessInternalW` → `ms/disassemble/kernelbase/_map/CreateProcessInternalW_{edges,nodes}.tsv`, `_summary.txt` (3초)
- 뽑은 함수: 6개
  - 요청 범위 3개: 정규식 `^(CreateProcessInternalW|CreateProcessW|ResumeThread)$`, depth 0
  - **넓힌 것 3개**: `CreateProcessInternalW$fin$0`, `$filt$1`, `$fin$2` (SEH 정리 함수, 아래 "왜 넓혔나")
  - 위치: `ms/decomp/kernelbase/`, `ms/disassemble/kernelbase/` (`_calls.tsv` 포함)
- 버전 확인: **표 대조는 못 했다.** "버전 확인용 알려진 사실" 표에 KernelBase 줄이 없다. 대신 아래로 안쪽 일관성만 봤다 (전부 일치).
  - 함수 위치·크기가 `_index.tsv` 와 같음: `CreateProcessW` 0x7d5f0 (107B), `CreateProcessInternalW` 0x7d6e0 (28087B), `ResumeThread` 0xf1f00 (51B)
  - `.asm` 의 call 길이 점검: `CALL qword ptr [IAT]` 가 7바이트이고 바로 뒤에 5바이트 NOP 이 온다. 그래서 `ret_off` = call 위치 + 7 (예: `+0x30e7` → `+0x30ee`). 세 곳 다 같은 모양이다.
  - `CreateProcessW +0x61` → `CreateProcessInternalW` 가 `.c` 와 맞음
  - `ResumeThread +0xe` 와 `CreateProcessInternalW +0x5bcc` 가 **같은 import 칸**(`0x18029b9a8`)을 부름 (아래 Q2)

## 스크립트 문제

- **없음.** `map` 도 `export` 도 `SCRIPT ERROR` 없이 끝났다.
- 한계 하나 (버그는 아님): `CreateProcessInternalW$fin$0/$filt$1/$fin$2` 같은 SEH 정리 함수는 **호출 관계로는 안 닿아서** 지도(`_map`)와 `CreateProcessInternalW.asm` 에 안 들어온다. 큰 함수에서 "여기가 유일한 호출 자리다"라고 말하려면 이것들을 따로 뽑아 확인해야 한다. 필요하면 reference.md "지도 > 읽을 때 주의" 에 한 줄 넣을 만하다 (제안만).

## 지도에서 본 것 (`_map/`, 기준 20)

- 뿌리 `CreateProcessInternalW`: **28087바이트**, 거르지 않은 전체 깊이별 새 함수 수 = 1, 63, 42, 35, 17, 20, 12, 3, 2, 10, 3 (깊이 0~10).
  - 파일에 남은 지도: 함수 168개 (펼침 150, 공용 도구 18).
  - 끊긴 끝: external(다른 모듈 import) 191종 · 822곳, indirect-cfg 10종 · 11곳.
  - **`CreateProcessInternalW` 자신은 간접 호출이 하나도 없다.** `indirect-cfg` 는 전부 깊이 2 이상 함수(`BaseFormatObjectAttributes`, `GetModuleHandleA`, `CreateFileInternal` 쪽 등) 안에 있다.
  - 깊이 1 줄은 357개 (call 자리 수). 거의 다 잡일(메모리 해제, 핸들 닫기, 에러 번호 설정, ETW 기록, 기능 켜짐 검사).
- 눈에 띄는 external (깊이 1, 이름은 그대로 import 이름):
  - `NtCreateUserProcess` — **한 줄만** (`+0x30e7`). 지도 전체를 통틀어서도 이 한 줄.
  - `NtResumeThread` — **한 줄만** (`+0x5bcc`).
  - `NtAlertResumeThread` — 지도에 **없음.** KernelBase 의 `ResumeThread` 도 지도에 없음 (이 트리 안에서 부르는 곳이 없다).
  - `CsrClientCallServer` (`+0x466b`), `NtAllocateVirtualMemory`/`NtWriteVirtualMemory` (`+0x574e` 근처): 일반 import 칸(`0x18029bxxx`)으로 부른다.
  - `BasepPostSuccessAppXExtension`, `BaseCheckElevation`, `BasepGetAppCompatData` 같은 `Basep*`/`Base*` 여러 개: 다른 칸(`0x1803c6xxx`)으로 부르는 import. 어느 DLL 에서 오는지는 안 봤다.
- 깊이 1 에서 큰 함수: `BasepConvertWin32AttributeList`(3177B), `BasepCreateLowBox`(1941B), `BasepCreateAppContainer`(1823B), `BasepCreateProcessParameters`(1411B, `+0x2ddf` 에서 부름 = `NtCreateUserProcess` **앞**), `BaseFormatObjectAttributes`(598B).
- 이름 확인: `NtCreateUserProcess`·`NtResumeThread`·`NtAlertResumeThread` 는 `_index.tsv` 에 없고 지도에서 `external` 로 나온다 — 요청서의 `[추측]`(ntdll import) 이 맞다. `pdb_aliases.py --where` 는 필요 없었다 (이름이 import 이름 그대로 `.asm` 에 찍힘).

## 왜 넓혔나

- "`NtResumeThread` 부르는 곳이 **하나뿐**이다" 를 말하려면 같은 함수에 속한 SEH 정리 함수(`CreateProcessInternalW$fin$0`, `$filt$1`, `$fin$2`, 101B·37B·4537B)까지 봐야 해서 이 셋만 depth 0 으로 더 뽑았다.
- 결과: 여기엔 `NtClose`, `RtlFreeHeap`, `NtTerminateProcess`, `NtRemoveProcessDebug` 등 **정리 호출뿐**이다. `NtCreateUserProcess`·`NtResumeThread`·`ResumeThread`·`NtAlertResumeThread` 는 없다. (`_calls.tsv` 의 `CreateProcessInternalW$fin$…` 줄)

## 질문별 답 [정적]

### 1. `CreateProcessInternalW` 가 `NtCreateUserProcess` 를 부르는 자리

- 답: **한 곳.** call_off `+0x30e7`, ret_off **`+0x30ee`**. `CALL qword ptr [0x18029bbc0]` (ntdll 에서 가져온 함수 칸을 통한 간접 호출, 이름은 import 그대로 `NtCreateUserProcess`).
- 근거:
  - `ms/disassemble/kernelbase/CreateProcessInternalW.asm` `+0x30e7` 줄, `_calls.tsv` 줄 `CreateProcessInternalW +0x30e7 +0x30ee NtCreateUserProcess`
  - `NtCreateUserProcess` 를 부르는 call 은 `.asm` 전체에 이 한 줄뿐이다 (`.c` 도 1곳).
  - 지도: `_map/CreateProcessInternalW_edges.tsv` 128번 줄.
- 인자 자리 (`.asm` `+0x30af` ~ `+0x30e7`, 알려진 선언 순서에 맞춰 읽음):
  - RCX = `&[RSP+0x118]` (ProcessHandle 받을 칸), RDX = `&[RSP+0x1b8]` (ThreadHandle 받을 칸)
  - R8D = R9D = `0x2000000` (두 접근 권한)
  - `[RSP+0x30]` = `[RSP+0x110]` 의 값 (**ProcessFlags**), `[RSP+0x38]` = **ESI** (**ThreadFlags**, Q3)
  - `[RSP+0x40]` = R14 (프로세스 매개변수), `[RSP+0x48]` = `&[RSP+0x550]` (CreateInfo), `[RSP+0x50]` = `&[RSP+0xfa0]` (속성 목록)
- 반환 값 처리: `+0x30f3 MOV EBX,EAX` → `+0x3150 TEST EBX,EBX` → `+0x3152 JNS 0x1800815af` (= `+0x3ecf`, 성공 쪽). 음수면 `+0x3158` 쪽(실패 처리).
- **"한 곳" 이지만 한 번만 불린다는 뜻은 아니다.** 실패 가지(`+0x3158`)에서 다시 `+0x30e7` 로 돌아올 길이 코드 흐름에 있다 (앱 실행 별칭, 16비트 실행 파일 처리 같은 재시도로 보임 `[추측]`). 실제로 몇 번 불리는지는 동적.
- `CreateProcessW` 는 이 함수를 `+0x61` 에서 부른다 (ret `+0x66`). 부르는 쪽에서 본 `ret_off` 는 `_calls.tsv` 줄 `CreateProcessW +0x61 +0x66 CreateProcessInternalW`.

### 2. 그 뒤에 첫 스레드를 풀어 주는 호출이 있나

- 답: **있다.** `NtResumeThread` **한 곳**: call_off `+0x5bcc`, ret_off **`+0x5bd3`**. `CALL qword ptr [0x18029b9a8]`.
  - `NtAlertResumeThread` 는 `CreateProcessInternalW`·`ResumeThread` 어디에도 없다.
  - **KERNELBASE 의 `ResumeThread` 함수를 거치지 않는다.** `CreateProcessInternalW` 안에 `ResumeThread` 를 부르는 줄이 없고, `ResumeThread +0xe` (ret `+0x15`) 도 **같은 import 칸**(`0x18029b9a8`)으로 `NtResumeThread` 를 직접 부른다. 즉 두 함수가 각자 `ntdll!NtResumeThread` 를 부른다.
  - 인자: RCX = `[RSP+0x1b8]` (= 위 `NtCreateUserProcess` 가 돌려준 **스레드 핸들**), EDX = 0 (`+0x5bc2 XOR EDX,EDX`, 이전 멈춤 횟수는 안 받음).
  - 반환 처리: `+0x5bd8 MOV EBX,EAX` → `+0x5be1 TEST EAX,EAX` → `+0x5be3 JS 0x180081f42` (음수면 `BaseSetLastNTError` 로 가서 실패).
- **언제 부르고 언제 건너뛰나** — 근거 줄:
  - `+0x5bb8 TEST byte ptr [RSP+0x108],R15B` → `+0x5bc0 JNZ 0x1800832c9` (= `+0x5be9`, `NtResumeThread` 를 **건너뜀**).
  - `R15B` = **4**: 여기에 닿는 길에서 R15 를 마지막으로 쓰는 곳이 `+0x4532` 와 `+0x5bb2` 둘뿐이고 둘 다 `MOV R15D,0x4` 다 (CFG 위 "도달 정의" 분석, 아래 "방법" 참고).
  - `[RSP+0x108]` = **호출자가 준 `dwCreationFlags` 사본**: `+0x47 MOV R14D,[RSP+0x2480]` (7번째 인자) → `+0xc7 MOV [RSP+0x108],R14D`. 같은 사본을 `+0x809 TEST byte [RSP+0x108],0x3` 로 검사해 `DbgUiConnectToDbg`(+0x813) 를 부르는 것도 이 값이 `DEBUG_PROCESS(1)/DEBUG_ONLY_THIS_PROCESS(2)` 를 담은 CreateProcess 플래그라는 걸 받쳐 준다.
  - 그러므로 **`CREATE_SUSPENDED`(값 4, Win32 공개 상수) 가 켜져 있으면 `NtResumeThread` 를 안 부르고, 꺼져 있으면 부른다.**
  - 같은 비트를 일찍 한 번 더 본다: `+0x7f0 MOV R15D,4` / `+0x7f6 TEST byte [RSP+0x108],R15B` / `+0x7fe JNZ` / `+0x800 BTS [RSP+0x110],9`. 4 가 **꺼져 있을 때** `ProcessFlags`(커널에 넘길 값) 의 bit9(`0x200`) 를 켠다. 이 비트가 커널에서 무슨 뜻인지는 이번 범위 밖 `[확인 필요]`.
  - 사본이 중간에 바뀌는 자리: `+0x749`(0x800 보정), `+0x7bf` (`AND 0xffff3e1f`, 우선순위 클래스 비트 지움. **bit2(4) 는 남김**), `+0xadc`, `+0xeca`, `+0xf1f`, `+0x4000`, **`+0x5b5b`**.
  - **`+0x5b5b OR [RSP+0x108],R15D` (4 를 켬)**: `+0x5b51 TEST byte [RSP+0x284],SIL` 가 1일 때. `[RSP+0x284]` 는 `CompletePackagedProcessCreationEx`(+0x5abd) / `CompleteAppExecutionAliasProcessCreationEx`(+0x5b36) / `BasepFinishPackageActivationForSxS`(+0x5ba1) 가 채워 주는 출력 칸(`+0x5a2f` 에서 0 으로 시작). 그 칸이 켜지면 `CreateProcessInternalW` 가 스스로 풀어 주지 않는다. 패키지(AppX) 앱 활성화 쪽이 풀어 주겠다는 신호로 보이나 `[추측]`.
  - 건너뛰는 길이 하나 더: `+0x394d JNZ 0x1800832c9`. `BasepProcessInvalidImage`(+0x3926, 정상 PE 가 아닌 이미지를 처리하는 import) 뒤에서 `[RSP+0x298] != 0` 이면 `NtResumeThread` 없이 `+0x5be9` 로 간다. 16비트/VDM 쪽 길로 보이며 `[추측]`. test.exe(정상 PE) 길과는 관계가 없어 보인다.
  - `CREATE_SECURE_PROCESS`(0x400000) 를 켜면 `+0x4172 TEST [RSP+0x108],0x400000` → `+0x417d JNZ 0x180083292`(= `+0x5bb2`) 로 가운데 큰 일(CSRSS 알림 포함)을 거의 다 건너뛰고 4 검사로 간다 (4 검사는 그대로 한다).
- **`NtCreateUserProcess` 와 풀어 주기 사이의 큰 일 (일 단위 3개 + 잡일 개수)**
  - 코드 흐름(CFG)으로 확인한 순서: 아래 ①→②→③ 이다. ② 의 `CsrClientCallServer`(`+0x466b`) 가 ③ 의 모든 호출을 앞선다 (이 호출을 지나지 않고는 ③ 에 못 간다). ① 은 ② 앞에만 있다 (② 뒤로는 닿지 않음). ①~③ 안의 순서는 코드 배치 순서일 뿐 CFG 가 못박은 것은 아니다.
  - 각 줄은 **조건부**다. 대부분 `IsBasep…Present`(필요한 import 가 있는지 보는 검사) 나 플래그 뒤에서만 부른다. `NtCreateUserProcess` 성공 뒤 `NtResumeThread` 까지 **반드시** 지나는 호출은 사실상 검사용 `IsBasep…Present` 와 `memset`(+0x40e5) 뿐이다.

  | # | 하는 일 | 이름 (call_off / ret_off) | 역할 한 줄 |
  |---|---|---|---|
  | ① | 새 프로세스 검사 | `BasepCheckWebBladeHashes` (+0x3f8a / +0x3f91), `BasepIsProcessAllowed` (+0x3fca / +0x3fd1), `BasepCheckWinSaferRestrictions` (+0x40bd / +0x40c4) | 만든 프로세스를 정책·Safer 규칙으로 걸러 본다 (이름으로 본 역할 `[추측]`) |
  | ② | **CSRSS 에 알림** | `BasepConstructSxsCreateProcessMessage` (+0x4458 / +0x445f), `BasepCsrCaptureSxsMessage` (+0x463b / +0x4640), **`CsrClientCallServer` (+0x466b / +0x4672)** | 윈도우 서브시스템 서버(csrss)에 "프로세스 만들었다" 메시지를 보낸다. `CsrClientCallServer` 의 ApiNumber = `0x1001d`, DataLength = `0x218` (`+0x464f R9D=0x218`, `+0x4655 R8D=0x1001d`). `CREATE_SECURE_PROCESS` 가 아니면 모든 길이 이 호출을 지난다. |
  | ③ | 상승·앱 호환·패키지 후처리 | `BaseCheckElevation` (+0x47ba / +0x47c1, +0x48ce / +0x48d5), `BaseMarkProcessTokenForInstallers` (+0x4821 / +0x4828), `AssignProcessToJobList` (+0x55ec / +0x55f1), `BasepGetAppCompatData` (+0x56bb / +0x56c2, +0x5952 / +0x5959), `NtAllocateVirtualMemory` (+0x574e) + `NtWriteVirtualMemory` (+0x578e, +0x57d0, +0x582a), `BaseElevationPostProcessing` (+0x5885 / +0x588c), `BasepInitAppCompatData` (+0x5976 / +0x597d), `BasepPostSuccessAppXExtension` (+0x59c6 / +0x59cd), `CompletePackagedProcessCreationEx` (+0x5abd / +0x5ac4), `CompleteAppExecutionAliasProcessCreationEx` (+0x5b36 / +0x5b3d), `BasepFinishPackageActivationForSxS` (+0x5ba1 / +0x5ba8) | 관리자 상승 필요 여부 확인, 잡(job) 연결, 앱 호환 데이터를 자식 프로세스 메모리에 써 넣기(`NtWriteVirtualMemory` 의 두 번째 쓰기가 `[RSP+0x1a0]+0x2d8` 자리에 주소를 씀 — x64 PEB 의 `pShimData` 자리로 알려진 오프셋이라 그것으로 보이나 `[추측]`), 패키지 앱 마무리. 전부 조건부. |

  - 잡일: 이어지는 길에서 `NtResumeThread` 에 닿을 수 있는 call 자리는 모두 **76곳**. 위 ①~③ 외에는 옵션 import 있는지 검사(`IsBasep…Present` 12곳), ETW/로그(`_tlg…`, `Etw…` 12곳), 기능 켜짐 검사(`Feature_…` 7곳), 메모리·핸들·문자열 정리, `NtQueryInformationProcess`(+0x41b4)·`NtQueryInformationToken`(+0x4561)·`GetModuleHandleA`/`RtlImageNtHeader`(+0x450a/+0x4512) 같은 값 읽기, `BasepUpdateProcessParametersField`(+0x46e8, +0x5a1b) 등.
- 근거 파일: `ms/disassemble/kernelbase/CreateProcessInternalW.asm`, `_calls.tsv`; `ms/decomp/kernelbase/CreateProcessInternalW.c` 2803번 줄 근처가 `NtResumeThread` (앞 줄 `(local_2340 & 4) != 0` 이 `goto` 로 건너뜀. 흐름·플래그는 `.asm` 으로 확인).

### 3. 커널에 넘기는 ThreadFlags 의 bit0

- 답: **항상 켠다. 조건이 없다.** `NtCreateUserProcess` 의 8번째 인자(ThreadFlags) 값 = **1** (`CREATE_SUSPENDED` 에 해당하는 bit0 만).
- 근거:
  - `+0x30af MOV dword ptr [RSP+0x38],ESI` — `[RSP+0x20]`이 5번째 인자이므로 `[RSP+0x38]` 이 **8번째**다 (`+0x30b3`~`+0x30cc` 가 5·6·7번째).
  - `ESI` 는 함수 앞부분에서 `+0x6cb LEA ESI,[RAX+1]` 로 **1** 이 된다 (`+0x699 XOR EAX,EAX` 로 RAX = 0, 그 사이 RAX 를 바꾸는 줄 없음). 그 전에는 `+0x3e MOV RSI,R8` (명령줄 포인터 보관) 이었다.
  - **`+0x6cb` 이후 `RSI/ESI/SI/SIL` 에 쓰는 줄이 없다** (`.asm` 전체에서 쓰기는 `+0x3e`, `+0x6cb`, 맨 끝 `POP RSI`(+0x6e0c) 셋뿐). 코드 흐름 전체에서 `+0x30e7` 에 닿는 ESI 값의 정의는 `+0x6cb` 하나다.
  - 같은 ESI 가 `NtOpenThreadToken` 의 `OpenAsSelf` 인자(`+0x300f MOV R8B,SIL`)와 "TRUE" 반환(`MOV EAX,ESI`, 실패는 `MOV EAX,EDI`(=0))에도 쓰이는 걸 보면 상수 1 이라는 읽기와 맞다.
  - `dwCreationFlags` 의 어떤 비트도 ThreadFlags 를 바꾸지 않는다 (ThreadFlags 쪽에는 `[RSP+0x108]` 을 읽는 줄이 없음). 호출자가 `CREATE_SUSPENDED` 를 줘도 안 줘도 커널에는 똑같이 1 이 간다.
- 결론: **"항상 켬 + 나중에 직접 풀어 줌" 모양이 맞다.** 항상 멈춘 채로 만들고(커널 `PspInsertThread +0x2e5` 에서 `KeSuspendThread`, run9·run12 `[정적]`), 호출자가 `CREATE_SUSPENDED` 를 안 줬을 때만 Q2 의 `NtResumeThread`(`+0x5bcc`)로 풀어 준다. 2.2 그림은 이 모양으로 완성된다.
- (같이 본 것) ProcessFlags(`[RSP+0x110]`) 는 `dwCreationFlags` 에 따라 바뀌는 여러 비트를 가진다 (`+0x7cc`, `+0x7da`, `+0x7e7`, `+0x800` 등). 이번 질문 밖이라 비트 하나하나의 뜻은 안 봤다.

### 4. `CreateProcessW` → `CreateProcessInternalW` 길에서 1~3 이 그대로인가

- 답: **그대로다.** `CreateProcessW` (107B) 는 `CreateProcessInternalW` 를 `+0x61` 에서 부를 뿐이다 (ret `+0x66`).
  - 넘기는 것: 1번째 인자(hUserToken) = 0 (`+0x5f XOR ECX,ECX`), 12번째 인자(마지막 토큰 칸) = 0 (`+0x7 AND qword ptr [R11-0x10],0`), 나머지 열 개는 한 칸씩 밀어서 그대로 넘긴다 (`+0xc`~`+0x5c`). `dwCreationFlags` 도 `+0x3c MOV EAX,[RSP+0x98]` → `+0x43 MOV [RSP+0x30],EAX` 로 **값을 바꾸지 않고** 복사한다 (AND/OR 없음).
  - 그래서 `CreateProcessW` 에서 왔든 다른 길(`CreateProcessAsUserW` 등, 이 함수를 부르는 곳은 지도 기준 5곳)에서 왔든, `NtCreateUserProcess` 자리(`+0x30e7`)·`NtResumeThread` 자리(`+0x5bcc`)·ThreadFlags=1 은 **같은 함수 안의 같은 줄**이다. 달라지는 건 `dwCreationFlags` 값뿐.
  - 나머지 4곳(호출자 이름)은 이번에 뽑지 않았다.
- 근거: `ms/disassemble/kernelbase/CreateProcessW.asm`, `ms/decomp/kernelbase/CreateProcessW.c` (선언 순서대로 `CreateProcessInternalW(0, …, lpProcessInformation, 0)` 한 번 호출)

### 5. 표기 규칙

- `.asm` / `_calls.tsv` 를 그대로 썼다. `CreateProcessInternalW` 줄에 `frag`·`tail`·`indirect` 는 없다. 간접으로 보이는 것은 전부 import 칸을 통한 호출로 `external` 이름이 붙어 있고, `alias:…+N` 은 `IsBasepReleaseSxsCreateProcessUtilityStructPresent+20` / `?StringCbCopyW@@YAJPEAG_KPEBG@Z+1` / `IsBasepBrokerSandboxedCreateProcessPresent+1` 이다.
  - **`alias:IsBasepReleaseSxsCreateProcessUtilityStructPresent+20` 의 이름은 믿지 말 것.** 같은 주소에 이름이 20개 더 있다 (링커가 합친 함수). `.asm` 의 `ALIASES(20)` 에는 `IsBasepQueryAppCompatPresent`, `IsBaseCheckElevationPresent`, `IsBasepIsProcessAllowedPresent` … 이 보인다. 이 함수들은 "그 import 가 있는지"를 알려 주는 검사 함수로 보인다 `[추측]`. 문서에는 "옵션 import 있는지 검사" 정도로만 쓰는 게 안전하다.
  - `Feature_…IsEnabledDeviceUsageNoInline` 은 기능 켜짐 검사 함수라 흐름에 영향이 작다.
- `.c` 는 reference 주의대로 **흐름·플래그 검사에는 쓰지 않았다** (`.asm` 으로 확인). `.c` 는 `NtCreateUserProcess(&local_2330,&local_2290,0x2000000)` 처럼 인자 목록을 줄여 보여서 인자 자리 확인에 쓸 수 없었다.

## 방법 (결과를 만든 방식)

- 호출 위치는 `_calls.tsv` / `.asm` 의 `ret=` 값을 그대로 썼다.
- "ESI 가 항상 1 이다", "`R15B` 는 4 다", "이 호출은 이 호출을 지나야만 닿는다" 는 **`.asm` 을 읽어 만든 임시 파이썬 스크립트**로 확인했다. 점프/분기를 따라 명령 단위 흐름 그래프를 만들고 (간접 점프 0개, 점프 대상이 함수 밖인 것 0개) 도달 정의·순서(지배 관계)를 계산했다.
  - 위 "CsrClientCallServer 를 지나지 않고는 `NtResumeThread` 에 못 간다" 는 `+0x30ee`(NtCreateUserProcess 복귀) 부터 함수 **전체** 흐름으로도 확인했다: `+0x466b` 와 `+0x5bb2`(= `CREATE_SECURE_PROCESS` 로 건너뛰는 길) 둘 다 막으면 `NtResumeThread` 에 닿을 길이 없다.
  - 이 스크립트는 저장소에 넣지 않았다 (임시 폴더에 있음). 같은 방식으로 다시 확인하고 싶으면 `.asm` 의 `+0x6cb`, `+0x4532`, `+0x5bb2`, `+0x5bb8` 줄을 직접 읽어 보면 된다.
  - 호출은 모두 돌아온다고 가정했다 (되돌아오지 않는 호출을 따로 가려내지 않음) → 실제보다 길이 더 많게 잡힌 쪽이라 "반드시 지난다"는 결론은 더 엄격한 쪽이다.

## 동적분석으로 확인해야 할 것

- 정적으로는 못 정한 것:
  1. 실제 부모(`cmd.exe`)가 넘기는 `dwCreationFlags` 에 `0x4` 가 있는지 (정적으로는 "없으면 풀어 준다"까지만 안다).
  2. `NtCreateUserProcess` 가 한 번만 불리는지, 재시도로 여러 번 불리는지 (코드엔 다시 닿는 길이 있음).
  3. `NtCreateUserProcess` 8번째 인자(ThreadFlags)가 실제로 1 인지 (정적: 상수 1).
  4. 풀어 주는 호출이 정말 `KERNELBASE!CreateProcessInternalW+0x5bd3` 에서 오는지, 아니면 다른 길(`ResumeThread +0x15` 등)에서 오는지.
  5. `IsBasep…Present` 12곳이 실제로 참을 돌려줘서 ①~③ 의 어느 호출이 진짜 실행되는지 (특히 `CsrClientCallServer`, `BasepPostSuccessAppXExtension`).
  6. 부모가 `kernel32!CreateProcessW` → `KERNELBASE!CreateProcessW` 로 오는지 (kernel32 쪽은 이번에 안 봄 `[공백]`).
- 추천 멈춤점 (후보만 — 유저 모드 함수라 커널 디버거에서 `cmd.exe` 프로세스 문맥으로 거는 법은 dast 가 정한다):
  - `KERNELBASE!CreateProcessInternalW+0x30e7` 입구와 `+0x30ee` 복귀: 8번째 인자가 1 인지, 몇 번 오는지.
  - `ntdll!NtResumeThread` 입구 (또는 `nt!NtResumeThread` 입구 + 부모가 `cmd.exe` 일 때만): 복귀 주소가 `KERNELBASE!CreateProcessInternalW+0x5bd3` 인지 확인. 대상 핸들 = 방금 만든 스레드.
  - 새 스레드의 정지 횟수: `NtCreateUserProcess` 가 돌아온 직후 새 스레드가 멈춘 채인지(`PspInsertThread +0x2e5` 뒤, run9·run12 와 이어짐).
  - 선택: `KERNELBASE!CreateProcessInternalW+0x466b` (`CsrClientCallServer`): csrss 쪽 알림이 `NtResumeThread` 앞에 오는지 순서 확인.
  - 선택: 부모가 `CREATE_SUSPENDED` 로 부를 때 `+0x5bb8`~`+0x5bc0` 에서 건너뛰는지 (별도 부모 프로그램이 필요, `sample/` 범위 밖).

## 판정

**분석성공.** 질문 1~4 에 모두 답이 있고 근거 줄도 있다. 핵심 결론은 세 가지다.
1. `NtCreateUserProcess` 는 `+0x30e7` 한 곳, 스레드는 항상 멈춘 채(ThreadFlags=1)로 만들어진다.
2. `NtResumeThread` 는 `+0x5bcc` 한 곳이고 `ResumeThread` 함수를 거치지 않으며, `dwCreationFlags` 의 `0x4` 가 꺼져 있을 때만 부른다. (패키지 앱 쪽 신호로 4 가 켜지는 길이 하나 더 있음)
3. 둘 사이의 큰 일은 보안 검사 → CSRSS 알림 → 상승·앱 호환·패키지 후처리, 모두 조건부.

(감점 요소: 처음 분석한 모듈이 아니라 "알려진 사실" 표 대조는 해당 없음. 대신 안쪽 일관성은 확인.)

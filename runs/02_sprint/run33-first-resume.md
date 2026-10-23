# 2026-10-23 run33 — 첫 풀어줌 표시: **`ProcessFirstResume` 를 읽는 곳은 둘** (`PsMultiResumeThread +0x45`, `PspProcessClose +0xf`), 조각 `FUN_1406b2db3` = **`KiApcInterrupt +0x273`** (정적)

> **정정 (run34)**: 아래 "일반 생성에서는 이 비교(`OwnerProcessId` == 닫는 쪽 PID)가 맞기 어려워 보인다" 는 틀렸다. `[RSP+0x90]` bit 9 는 KERNELBASE 가 **`CREATE_SUSPENDED` 가 없을 때** 켜는 ProcessFlags bit 9 라서, 보통 생성에선 `|2` 가 안 붙고 `OwnerProcessId` = 부모 PID 그대로다 (`|2` 는 `CREATE_SUSPENDED` 때만). 나머지(읽는 곳 둘, Q64 짐작)는 그대로. → `run34-parent-leftovers.md` "정정"

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntoskrnl (`ntoskrnl.exe`), pdb `ntkrnlmp.pdb` GUID+age `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음).
  - analyze 는 다시 안 걸었다 (이미 돼 있음).
- 버전 대조 (결과서): `NtCreateUserProcess` ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` ret `+0x807` → `PspCallProcessNotifyRoutines` 가 run9·run12 와 같다.
- 이번엔 하네스에 없는 임시 도구를 썼다 (결과서가 만듦, `ms/ghidra/_tmp_sast28/`, 커밋 안 함):
  - 모든 명령의 피연산자에서 `+0x5fc` 를 찾는 Ghidra 스크립트 둘 (`ScanDisp2.java`, `ScanLoads.java`)
  - PE `.pdata` (함수 범위 표) 를 읽는 파서 (`pelib.py`, `q2.py`)
  - PDB 공개 이름·구조체 칸·비트 이름은 `llvm-pdbutil` 덤프

| 함수 | rva | 이번에 본 것 |
|---|---|---|
| `nt!PsMultiResumeThread` | `0x2c9aa0` | `+0x20 ~ +0x6f` (run13 에 있던 파일) |
| `nt!PspProcessClose` | `0x9b3b50` | 전체 (짧다, `+0xf3` 까지) |
| `nt!KiApcInterrupt` | `0x6b2b40` | `.pdata` 범위 전체 모양, `+0x38a ~ +0x3ab`, `+0x401`, `+0x6b2` |
| `nt!PspInitPhase0` | — | `+0x47e ~ +0x4eb` (객체 형식 등록) 만 |
| `nt!NtClose` · `nt!ObCloseHandleTableEntry` | — | `+0xe4` / `+0x2b0`·`+0x30c`·`+0x7a9`·`+0xc0a` 만 |
| `nt!PspAllocateProcess` | (run10) | `+0x6f1 ~ +0x710` (`OwnerProcessId` 쓰기) 만 |
| 그 밖 (`PsIsProcessLoggingEnabled`, `PsSetVmProcessorHostProcess`, `NtQueryInformationProcess` 등) | — | `+0x5fc` 를 읽는 줄 앞뒤만 (결과서) |

## 목적

- Q40: 작은 정적 확인 둘.
  - flow.md 2.2 `[공백]` — `PsMultiResumeThread +0x4e` 가 켜는 `_EPROCESS +0x5fc` bit 15 `Flags3.ProcessFirstResume` 를 **누가 읽나**.
  - flow.md 2.2 "멈춘 첫 스레드가 서는 곳" 6번 `[추측]` — `KiDeliverApc` 를 부르는 조각 `FUN_1406b2db3` 이 **정말 `KiApcInterrupt` 인가**.

## 방법

- 정적: `_workspace/sast/28_first-resume.md` — ntoskrnl 전체 훑기 (`+0x5fc` bit 15 읽기), `^KiApcInterrupt$` depth 0, rva `0x6b2db3` 를 감싸는 범위
- 결과서: `_workspace/sast/28_first-resume_result.md` (**분석성공**)
  - decompiler 첫 시도는 로그인 오류로 산출물 없이 끊겼고, 같은 요청서로 다시 불러 성공했다 (plan.md 결정 기록).
- 이번에 `.asm` 과 다시 맞춰 본 줄 (flow-writer):
  - `PsMultiResumeThread` `+0x20/+0x25`, `+0x32`, `+0x36`, `+0x38`, `+0x3f`, `+0x45`, `+0x4c`, `+0x4e`, `+0x56`, `+0x6a/+0x6f`
  - `PspProcessClose` `+0x9`·`+0xd`, `+0xf`·`+0x19`·`+0x1b`, `+0x22 ~ +0x3d`, `+0x47/+0x4c`, `+0x4e ~ +0x56`, `+0x76/+0x7b`, `+0x97`, `+0xa9/+0xae`, `+0xcf/+0xd4`
  - `KiApcInterrupt` `+0x157` (가짜 호출), 조각 시작 `+0x273`, `+0x38a/+0x38f`, `+0x390`, `+0x3a6/+0x3ab`, `+0x401`, `+0x6b2`
  - `PspInitPhase0` `+0x47e`·`+0x489`·`+0x490`·`+0x4a8`·`+0x4bb`·`+0x4e6/+0x4eb`
  - `NtClose` `+0xe4/+0xe9`, `ObCloseHandleTableEntry` `+0x2b0`·`+0x30c`·`+0x7a9/+0x7ae`·`+0xc0a/+0xc0f`
  - `PspAllocateProcess` `+0x6f1 ~ +0x710`
  - 결과서와 다른 곳은 없었다.

### 찾는 법과 한계 — "읽는 곳 둘뿐" 은 어느 범위의 결론인가

**어떻게 찾았나 (결과서 1-g, 네 겹)**

1. **모든 명령 훑기.** Ghidra 가 읽은 ntoskrnl 명령 **2,645,299개** 전부에서 피연산자 값이 `0x5f8 ~ 0x5ff` 인 것을 뽑았다 (`[reg+0x5fc]` 변위, 즉값 둘 다). `+0x5fc` 는 226개 / 149개 함수.
2. **원본 바이트로 다시 확인.** 실행 구역 18개 전부에서 `fc 05 00 00` 바이트를 찾았다: 233곳. 226곳은 1번과 같고, 7곳은 명령이 아니다 (분기 변위 6곳, 표 데이터 1곳). → **Ghidra 가 못 읽은 코드에 숨은 `[reg+0x5fc]` 는 없다.**
   - bit 15 가 든 바이트 `+0x5fd` 를 직접 건드리는 명령은 0개.
3. **포인터를 레지스터에 들고 있는 꼴.** `&Flags3` 를 `LEA`/`ADD 상수` 로 만든 곳 6곳을 따라가 읽는 줄을 봤다. 일반화한 추적은 그 6곳 말고 0건 (추적이 동작하는지는 `+0x5f8` 로 시험, 18건).
4. **읽어 온 값 따라가기.** `[reg+0x5fc]` 를 레지스터로 읽는 `MOV` 103개마다 뒤따르는 직선 코드(최대 120명령)에서 어떤 마스크·비트로 쓰는지 봤다. bit 15 를 보는 것은 0건.

**못 보는 꼴 (한계)**

- **범위는 이 `ntoskrnl.exe` 파일 하나.** `hal`, `win32k*`, 드라이버, 실행 중 고쳐지는 코드(핫패치)는 안 봤다.
- 포인터를 **다른 함수에 넘겨** 거기서 읽는 꼴은 못 따라간다 (예: `PsSetVmProcessorHostProcess +0x12a` → `ExBlockOnAddressPushLock`. 4바이트 통째 비교라 bit 15 판단은 아닐 것 `[추측]`).
- 포인터를 **메모리(스택·구조체)에 저장했다 다시 꺼내** 쓰는 꼴은 안 따라간다 (`PspUserThreadStartup +0x61` 한 곳은 원본 바이트로 직접 봤고 bit 0 만 읽음).
- 치우친 기준을 `LEA`/`ADD 상수` 가 아닌 식(곱셈, `SUB`, 레지스터 둘 더하기)으로 만든 꼴, 분기·함수 경계를 넘는 추적, 실행 중에 오프셋을 계산하는 접근은 못 찾는다.
- 같은 `+0x5fc` 를 쓰는 **다른 구조체**는 이름·쓰임으로 걸렀다 (`_EPROCESS` 아님 `[추측]`). 그것들은 bit 15 판단이 없어 결론엔 영향이 없다.

→ 그래서 "읽는 곳 둘뿐" 은 **ntoskrnl 한 파일 안, 위 네 겹으로 잡히는 꼴 안에서** 의 결론이다.

## 결과

비유 (관리사무소 = 커널, 관리실 = ntdll):

- 관리사무소 장부의 가게 카드(`_EPROCESS`)에 **"첫 개업 허락 도장"** 칸이 있다 (`Flags3.ProcessFirstResume`, bit 15).
- 도장을 찍는 곳은 한 군데다: 부모가 "직원 일 시작해도 돼요" (`NtResumeThread`) 하면, `PsMultiResumeThread` 가 처음 한 번 찍는다 (run13).
- 이번에 찾은 것: 그 도장을 **보는 곳**도 딱 둘이다.
  - 찍는 사람 자신 — "벌써 찍혀 있나?" 보고, 찍혀 있으면 다시 안 찍는다.
  - 가게 열쇠(프로세스 핸들)를 **반납 받는 창구** (`PspProcessClose`) — 도장이 있으면 그냥 돌려받고 끝. 도장이 **없으면** "열쇠 주인이 막 문 닫고 떠나는 중이면, 한 번도 안 연 이 가게도 같이 정리" 하는 지름길을 검사한다.
- 도장을 지우는 곳은 없다. 관리실(ntdll)이나 바깥 기록(ETW)으로 알려 주지도 않는다.
- 또 하나: 멈춰 둔 직원을 "쪽지 먼저 읽어" 하고 부르는 **벨(APC 인터럽트)** 처리 함수의 이름이 이제 확실하다 — `KiApcInterrupt`.

### 정적 — 호출 지점 (`.asm` CALL 줄, 라벨은 ret)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `nt!PsMultiResumeThread` | `+0x20` | `+0x25` | `nt!KeResumeThread` | 늘 | [정적] (run13 자리) |
| `nt!PsMultiResumeThread` | `+0x6a` | `+0x6f` | `nt!EtwTiLogSuspendResumeThread` | `+0x56` Flags3 bit 20 켜짐 | [정적] (run13 자리) |
| `nt!PspProcessClose` | `+0x47` | `+0x4c` | `nt!PsTerminateProcess(닫히는 프로세스, 0xc000010a)` | bit 15 **꺼짐** · 주인 PID 일치 · 닫는 쪽이 지워지는 중 (아래) | [정적] |
| `nt!PspProcessClose` | `+0x76` | `+0x7b` | `nt!PspLockProcessExclusive` | 핸들 1개 · 스레드 0 (마지막 핸들 길) | [정적] |
| `nt!PspProcessClose` | `+0xa9` | `+0xae` | `nt!PspUnlockProcessExclusive` | 위와 같음 | [정적] |
| `nt!PspProcessClose` | `+0xcf` | `+0xd4` | `nt!PspRundownProcess` | 위 길에서 조건 맞을 때 | [정적] |
| `nt!NtClose` | `+0xe4` | `+0xe9` | `nt!ObCloseHandleTableEntry` | 늘 | [정적] |
| `nt!ObCloseHandleTableEntry` | `+0x7a9` | `+0x7ae` | 간접 (CFG) — 객체 형식 `CloseProcedure` | 알려진 몇 개가 아닐 때. 대상이 `PspProcessClose` 인지는 정적으로 못 정함 | [정적] (대상 `[추측]`) |
| `nt!PspInitPhase0` | `+0x4e6` | `+0x4eb` | `nt!ObCreateObjectType` (프로세스 객체 형식, `R9 = &PsProcessType`) | 부팅 때 한 번 | [정적] |
| `nt!KiApcInterrupt` | `+0x38a` | `+0x38f` | `nt!HalPerformEndOfInterrupt` | | [정적] |
| `nt!KiApcInterrupt` | `+0x3a6` | **`+0x3ab`** | `nt!KiDeliverApc(0, 0, 트랩 프레임 추정)` | `+0x390` `CMP byte GS:[0x187],0` 검사가 걸리면 건너뜀 | [정적] |
| `nt!KiApcInterrupt` | `+0x401` | `+0x406` | `nt!KiInitiateUserApc` | | [정적] |
| `nt!KiApcInterrupt` | `+0x6b2` (`JMP`) | — | `nt!KiKernelExit` (꼬리 점프) | 끝 갈래 | [정적] |

- `KiApcInterrupt` 오프셋은 `.asm` 의 `(= KiApcInterrupt +0x…)` 표시다. `_calls.tsv` 는 조각 기준(`frag:FUN_1406b2db3+0x133` / `+0x138`)으로 적혀 있다. 둘은 같은 줄이다 (절대 `0x1406b2ee6` / `0x1406b2eeb`).

### 정적 — 질문 1: `ProcessFirstResume` (bit 15) 를 읽는 곳

**읽고 판단하는 곳은 둘이다.**

| 자리 | rva | 무엇을 하나 | 켜짐 / 꺼짐 |
|---|---|---|---|
| `PsMultiResumeThread +0x45` | `0x2c9ae5` | `+0x3f MOV R8D,0x8000` → `+0x45 TEST [RAX+0x5fc],R8D` → `+0x4c JNZ +0x56`. RAX = 그 스레드의 프로세스 (`+0x38`, `[스레드+0x220]`). 자기가 켜기 전 "이미 켜졌나" 검사 | 켜짐: `+0x4e` 쓰기 건너뜀 / 꺼짐: `+0x4e OR.LOCK` 으로 켬 |
| `PspProcessClose +0xf` | `0x9b3b5f` | `TEST [RDX+0x5fc],0x8000` → `+0x19 JZ +0x22`. RDX = 닫히는 프로세스. **프로세스 객체 형식의 닫기 콜백** | 켜짐: `+0x1b` 로 바로 return / 꺼짐: `+0x22` 아래 검사 |

**`PspProcessClose` 안 (읽는 줄 앞뒤만)**

- 인자 모양 `[추측]`: `RCX` = 핸들을 닫는 프로세스, `RDX` = 닫히는 프로세스 객체, `R9` = 닫는 쪽이 가진 이 객체 핸들 수 (공개 문서의 닫기 콜백 모양으로 맞춘 이름. 확인은 Q65).
- `+0xf` 에 오는 조건: `+0x9 CMP R9,1` / `+0xd JBE +0x4e` → `+0x4e` 에서 `ActiveThreads` (`+0x380`) ≠ 0 이면 `+0x56 JNZ +0xf`. 곧 **핸들이 둘 이상이거나, 스레드가 하나라도 살아 있을 때.**
  - 핸들 하나 + 스레드 0 이면 `+0x58` 아래 "마지막 핸들" 길로 간다 (잠금 → `Flags` bit 3 cmpxchg → 풀기 → `PspRundownProcess`). 거기엔 bit 15 검사가 없다.
- bit 15 가 **꺼져 있으면** (`+0x22 ~ +0x3d`):
  - 닫히는 프로세스의 `OwnerProcessId` (`+0x2d8`) 가 닫는 프로세스의 `UniqueProcessId` (`+0x1d0`) 와 같고,
  - 닫는 프로세스 `Flags` (`+0x1f4`) 에 bit 3 `ProcessDelete` 또는 bit 30 `ProcessSelfDelete` (`0x40000008`) 가 있으면
  - → `PsTerminateProcess(닫히는 프로세스, 0xc000010a)` (call `+0x47` / ret `+0x4c`). 아니면 return.
- 읽은 뜻 `[추측]`: "한 번도 안 풀린 자식 프로세스를, 주인이 죽으면서 핸들을 닫으면 같이 끝낸다." bit 15 = "이미 한 번 풀렸음" 이라 그 지름길을 막는다.
- **일반 생성에서는 이 비교가 맞기 어려워 보인다** `[정적 읽기 + 추측]`: `OwnerProcessId` 를 쓰는 `PspAllocateProcess +0x6f1 ~ +0x710` 은 **부모 PID 에 `|2`** 를 붙여 쓴다 (`+0x703 OR RAX,2`). 인자 `[RSP+0x90]` 의 bit 9 가 켜졌을 때만 안 붙인다 (`+0x707 BT R8D,9`, `+0x70c CMOVC`). PID 는 4 의 배수라 `|2` 가 붙으면 닫는 쪽 PID 와 같을 수 없다. bit 9 가 무슨 플래그인지는 안 봤다 (Q65).

**이 닫기 콜백은 어떻게 불리나**

- 직접 `CALL` 은 없다 (`.c` 머리 `callers:` 비어 있음).
- 등록: `PspInitPhase0` 가 프로세스 객체 형식을 만들 때 `+0x4a8 LEA RAX,[PspProcessClose]` → `+0x4bb` 스택 저장. 앞의 `+0x47e` (`PspProcessDelete`), `+0x490` (`PspProcessOpen`) 와 간격이 PDB `_OBJECT_TYPE_INITIALIZER` 의 `OpenProcedure +0x38` / `CloseProcedure +0x40` / `DeleteProcedure +0x48` 과 같다. → `+0x4e6` (ret `+0x4eb`) `ObCreateObjectType`.
- 부름 (간접): `NtClose +0xe4` (ret `+0xe9`) → `ObCloseHandleTableEntry` → 객체 형식 `+0x80` (= `TypeInfo.CloseProcedure`) 을 읽어 (`+0x2b0`, `+0x30c`) 알려진 몇 개와 비교 → 아니면 **CFG 간접 호출 `+0x7a9` (ret `+0x7ae`)**. 대상이 `PspProcessClose` 인지는 정적으로 못 정한다.
- 그래서 **어느 프로세스든 그 핸들이 하나 닫힐 때마다** 이 함수가 돈다 `[정적 + 간접 호출 대상은 추측]`.

**test.exe 시동 길에서 서나**

- `PsMultiResumeThread +0x45` — **코드상 지나는 줄이다** `[정적]`. 부모 `NtResumeThread` (call `+0x96` / ret `+0x9b`, run13·run14) 가 부르고, `KeResumeThread` 가 돌려준 이전 횟수가 0 이 아니고 (`+0x32 JZ`) 요청 횟수 1 이하일 때 (`+0x36 JA`) 온다. 새 스레드는 `KeSuspendThread` 로 횟수 1 이라 통과한다. 실제로 지났는지, 그때 bit 15 가 꺼져 있었는지는 동적 (Q19).
- `PspProcessClose` — **정적으로는 모른다.** "누가 test.exe 프로세스 핸들을 언제 닫나" 에 달렸다. 부모가 `CreateProcess` 끝에 `CloseHandle(hProcess)` 하는 게 `main` 전인지 후인지 코드로 못 정한다.
  - 만약 `main` 전에 닫으면: 부모의 `NtResumeThread` 가 이미 `+0x4e` 를 지났으니 bit 15 는 켜져 있어야 하고, 스레드가 살아 있으니 `+0xf` 로 와서 `+0x1b` 로 바로 return 한다 → **아무 일도 안 한다** `[정적 읽기 + 추측: 순서]`. 동적 Q64.

**쓰는 곳**

- bit 15 를 켜는 곳은 **`PsMultiResumeThread +0x4e` 하나뿐**이다. 끄는 줄은 없다.
  - `+0x5fc` 에 쓰는 명령 40곳을 다 봤고, 마스크가 상수가 아닌 4곳(`PspAllocateProcess +0x640`, `NtSetInformationProcess +0x203`, `PspSetProcessForegroundBackgroundRequest +0xd7`·`+0x125`, `ExUpdateTimerResolutionPolicy +0x62`)도 값을 따라가 bit 15 가 아님을 확인했다 (결과서).
  - 새 프로세스를 만들 때 `PspAllocateProcess` 가 Flags3 에 쓰는 비트는 0·5·7·12·23·27·30. **bit 15 없음.** 처음 값은 0 일 것 `[추측: 객체가 0 으로 채워져 나옴]`.
  - 부팅 코드 한 곳(`FUN_140bef6d0 +0x3b57`, INIT 구역)이 4바이트를 통째로 쓴다. 처음 프로세스용으로 보이고 test.exe 길과 상관없다 `[추측]`.

**밖으로 나가는 길**

- `NtQueryInformationProcess` 가 Flags3 를 읽는 곳 셋 (`+0x251a`, `+0x2ecd`, `+0x3ff3`) 은 bit 19·20·28·29 / 2 / 9 만 본다. **bit 15 없음.**
- ETW 쪽 (`EtwpPsProvProcessEnumCallback` 등 5개)·`ExpGetProcessInformation` 도 bit 15 를 안 읽는다. `EtwTiLogSuspendResumeThread` 는 Flags3 를 아예 안 읽는다.
- → bit 15 는 커널 안 위 두 곳에서만 쓰이고 **관리실(ntdll)이나 ETW 로 안 나간다** (위 한계 범위 안에서).

### 정적 — 질문 2: `FUN_1406b2db3` 의 정체

**답: `nt!KiApcInterrupt +0x273`.** 떨어진 조각도, 다른 함수도 아니다.

| 항목 | 값 |
|---|---|
| `0x6b2db3` 를 감싸는 `.pdata` 범위 | `[0x6b2b40, 0x6b3259)`, **1817바이트** |
| unwind 정보 | `UNW_FLAG_CHAININFO` **없음** → 부모 함수 없음. 예외 처리기 `KiFatalExceptionHandler` |
| 이 범위로 체인되는 다른 범위 | 없음 |
| PDB 공개 이름 | 이 범위 안에 `KiApcInterrupt` (`0x6b2b40`) **하나**. 다음 이름 `KiInitiateUserApc` (`0x6b3260`) |
| `pdb_aliases.py --lookup ntoskrnl.exe 0x6b2db3` | 별칭 무리 아님. 앞 이름 `KiApcInterrupt` 에서 `+0x273` |

- Ghidra 는 이 범위를 다섯 토막으로 쪼갰다: `KiApcInterrupt` 348B + 사다리 279B + `FUN_1406b2db3` 753B + 사다리 279B + `FUN_1406b3170` 158B = 1817B. 사다리는 `ADD RSP,8; CALL` 짝을 31칸 늘어놓은 것(추측 실행 막기용 RSB 채우기 `[추측]`)이라 읽을 코드가 아니다. 그래서 `KiApcInterrupt +0x157` 의 `CALL` 은 함수 호출이 아니다 (`.asm` 이 FAKECALL 로 표시).
- `KiDeliverApc` 부르는 줄: **call `+0x3a6` / ret `+0x3ab`**. 바로 앞에 `HalPerformEndOfInterrupt` (ret `+0x38f`) 와 `STI` (`+0x38f`), 그리고 `+0x390 CMP byte GS:[0x187],0` 검사 (걸리면 `KiDeliverApc` 를 건너뜀. 그 바이트의 뜻은 안 봤다). → 동적 Q42
- WinDbg 가 `nt!KiApcInterrupt+0x3ab` 로 보여 줄지는 PDB 공개 이름이 하나뿐이라 그럴 것 `[추측]` (동적 Q42).

### 호출 관계

```
(부모 cmd.exe 스레드, 나중 시스템 콜) 풀어 주기                 (run13 + run33)
NtResumeThread
 └─ +0x9b  PsMultiResumeThread
      ├─ +0x25  KeResumeThread
      │  +0x32/+0x36  "이번에 완전히 풀림" 일 때만 ↓
      │  +0x45  Flags3 bit15 이미 켜졌나 (읽는 곳 ①)
      │  +0x4e  안 켜졌으면 OR.LOCK 으로 켬 (쓰는 곳, 유일)
      └─ +0x6f  EtwTiLogSuspendResumeThread   (bit 20 일 때)

(누가 언제인지 모름) 프로세스 핸들 닫기                         (run33, 대상은 간접)
NtClose
 └─ +0xe9  ObCloseHandleTableEntry
      └─ +0x7ae  (CFG 간접) 객체 형식 CloseProcedure  = PspProcessClose [추측: 대상]
            │  +0xf  Flags3 bit15 켜졌나 (읽는 곳 ②) → 켜짐: +0x1b return
            └─ +0x4c  PsTerminateProcess(0xc000010a)   (꺼짐 + 주인 PID 일치 + 주인 지워지는 중)

(test.exe 첫 스레드) APC 인터럽트                               (run13 + run15 + run33)
PspUserThreadStartup +0x22 IRQL 0
 ═▶ KiApcInterrupt                       [추측] 들어오는 자리 +0x26
      │  +0x38f HalPerformEndOfInterrupt 뒤 STI,  +0x390 GS:[0x187] 검사
      └─ +0x3ab  KiDeliverApc
            └─ +0x220  KiSchedulerApc
                  └─ +0xf7  KeWaitForSingleObject(SuspendEvent)   ← 여기서 잠듦
```

### 그림 반영 (flow.md 그림 2)

- `nt` 상자 안 `(1) 멈춤 APC 처리` 의 `h0` 상자 글을 **`nt!KiApcInterrupt`** 로 바꿨다 ("추정", "장부: 조각 FUN_1406b2db3" 뗌). 상자는 `(정적)` 점선 그대로다 (실행으로 들어가는 걸 본 적은 없다).
- `h0 → h1` (`KiDeliverApc`) 화살표 라벨을 `?` 에서 **`+0x3ab`** 로 바꿨다 (`[정적]`, ret).
- `PspUserThreadStartup ==> h0` 굵은 화살표는 그대로다. 들어오는 자리 `+0x26` 은 아직 `[추측]` (Q42).
- 상자 제목의 "(추측 연결)" 을 "(들어오는 자리는 추측)" 으로 고쳤다. 이제 추측은 굵은 화살표 하나뿐이라서다.
- **`PspProcessClose` 는 그림에 안 넣었다.**
  - 누가(어느 스레드가) 언제 부르는지 모른다. 간접 호출이라 caller 도 정적으로 못 정한다.
  - `main` 전에 선다 해도 bit 15 가 켜져 있으면 바로 return 하는 자리라, "시동에서 무엇이 일어나나" 의 대표 지점이 아니다.
  - Q64 에서 `main` 전에 선다는 게 보이면 그때 그 스레드 그림에 넣는다.

## 확인된 것

- (`[정적]`) `ProcessFirstResume` (bit 15) 를 **읽고 판단하는 곳은 ntoskrnl 안에 둘**: `PsMultiResumeThread +0x45` (켜기 전 이미 켜졌나), `PspProcessClose +0xf` (켜졌으면 바로 return). 범위는 위 "찾는 법과 한계".
- (`[정적]`) bit 15 를 **켜는 곳은 `PsMultiResumeThread +0x4e` 하나, 끄는 곳은 없다.** 새 프로세스를 만들 때 `PspAllocateProcess` 는 이 비트를 안 쓴다.
- (`[정적]`) bit 15 는 `NtQueryInformationProcess`·ETW 로 **밖에 안 나간다.**
- (`[정적]`) `PspProcessClose` 는 프로세스 객체 형식의 **닫기 콜백**이다: `PspInitPhase0 +0x4a8` 에서 등록, `ObCloseHandleTableEntry +0x7a9` 간접 호출로 불릴 자리 (대상 `[추측]`).
- (`[정적]`) bit 15 가 꺼져 있고 주인 PID 가 같고 주인이 지워지는 중이면 `PspProcessClose +0x47` (ret `+0x4c`) 에서 `PsTerminateProcess(…, 0xc000010a)`.
- (`[정적]`) `OwnerProcessId` 는 부모 PID `|2` 로 적힌다 (`PspAllocateProcess +0x703`, 인자 bit 9 면 안 붙임).
- (`[정적]`) **`FUN_1406b2db3` = `KiApcInterrupt +0x273`** (같은 `.pdata` 범위, 체인 없음, PDB 이름 하나). APC 인터럽트가 `KiDeliverApc` 를 부르는 자리 = **call `+0x3a6` / ret `+0x3ab`**.

### flow.md 판정

| 예전 글 | 판정 | 근거 |
|---|---|---|
| 2.2 `PsMultiResumeThread` `[공백]` "`ProcessFirstResume` 를 읽는 곳은 안 봤다" | **해결 → `[정적]`** 둘: `+0x45`, `PspProcessClose +0xf` | 위 표 |
| 2.2 "멈춘 첫 스레드가 서는 곳" 6번 `[추측]` "`FUN_1406b2db3` … `KiApcInterrupt` 의 떨어진 조각" | **이름은 맞다, "떨어진 조각" 은 아니다 → `[정적]`** `KiApcInterrupt +0x273` (같은 범위 한가운데) | `.pdata`, PDB 공개 이름 |
| 그림 2 `h0` "nt!KiApcInterrupt 추정" / `h0 → h1` `?` | **`nt!KiApcInterrupt` / `+0x3ab`** | `KiApcInterrupt.asm` `+0x3a6` |
| 4장 2.2 두 줄, 4.1 그림 2 nt 줄 | **해결됨** (남은 건 동적 Q19·Q42·Q64) | 위 |

## 아직 확인 필요

동적 확인거리는 오케스트레이터가 대기열에 넣었다. 커널 멈춤점이라 test.exe 일 때만 서게 조건이 필요하다 (dast-mentor 몫).

- **Q19** (첫 스레드가 풀리는 시점, 출처 추가): `nt+2c9ae5` (`PsMultiResumeThread +0x45`) 에서 `dd poi(@rsi+0x220)+0x5fc L1` → bit 15 꺼짐 기대. `nt+2c9af6` (`+0x56`) 에서 같은 칸 → 켜짐 기대.
- **Q42** (APC 인터럽트 자리, 출처 추가): 멈춘 스레드 `k` 에서 `KiDeliverApc+0x220` 바로 아래가 `nt!KiApcInterrupt+0x3ab` 로 보이나. `ln nt+6b2db3` → `nt!KiApcInterrupt+0x273` 기대. 멈춤점 후보 `nt+6b2ee6` (call) / `nt+6b2eeb` (ret).
- **Q64** (새, 동적): `PspProcessClose` 가 test.exe 프로세스 객체로 `main` 전에 서나. `nt+9b3b50` 입구, 조건 `@rdx == <test.exe EPROCESS>`. 서면 `k` (기대 `NtClose` → `ObCloseHandleTableEntry+0x7ae`) 와 `dd @rdx+0x5fc L1` (bit 15 켜짐 → `nt+9b3b6b` return 기대). 안 서면 그것이 답.
- **Q65** (새, 정적): `PspAllocateProcess` 인자 `[RSP+0x90]` bit 9 의 뜻 (`OwnerProcessId` 에 `|2` 를 안 붙이는 조건), `OwnerProcessId` 를 쓰는 다른 줄 `NtSetInformationProcess +0x1ecd` (어느 정보 클래스), `ObCloseHandleTableEntry +0x7a9` 앞 인자 채움 (`PspProcessClose` 인자 이름 확인).

## 디버거 메모 / 도구 메모

- (도구, 하네스 제안 — 결과서 "스크립트 문제") **"구조체 칸을 누가 읽고 쓰나" 를 묻는 명령이 스킬에 없다.** `refs` 는 주소 하나를 가리키는 곳만 찾는다. 이번엔 임시 Ghidra 스크립트로 명령 264만 개를 15초에 훑었다. `scan <바이너리> <변위>` 같은 명령이 있으면 Q36(PEB)·이번(Flags3) 같은 질문을 바로 할 수 있다.
- (도구, 하네스 제안) **조각 이름이 `callers:` 와 `_calls.tsv` 에 그대로 남는다.** `KiDeliverApc.c` 머리의 `FUN_1406b2db3` 은 실은 `KiApcInterrupt +0x273` 이다. `.asm` 의 `(= KiApcInterrupt +0x…)` 표시는 이미 맞으니, 같은 `.pdata` 범위면 caller 이름·오프셋도 본래 함수 기준으로 적으면 좋다. 그때까지 장부의 `FUN_1406b2db3` 은 `KiApcInterrupt` 로 읽는다.

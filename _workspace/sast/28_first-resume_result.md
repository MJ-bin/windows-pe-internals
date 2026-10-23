# 28 first-resume — 정적 분석 결과

- 요청서: `_workspace/sast/28_first-resume.md` (Q40)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`ntoskrnl.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 셋 다 있음, 돌고 있는 Ghidra 프로세스 없음).
- 뽑은 함수 (전부 depth 0, `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`):
  - 요청한 것: `KiApcInterrupt` (rva `0x6b2b40`, 348B). `PsMultiResumeThread` 는 이미 있어서 다시 안 뽑았다.
  - 질문 1 에서 "그 칸을 읽는 줄 앞뒤"를 `.asm` 으로 확인하려고 더 뽑은 것: `PspProcessClose`, `PsIsProcessLoggingEnabled`, `PsSetVmProcessorHostProcess`, `PspProcessDelete`, `SepMandatorySubProcessToken`, `MiForceCrashForInvalidAccess`, `PspSetProcessForegroundBackgroundRequest`, `NtQueryInformationProcess` (읽는 줄 앞뒤만 봄, 본문 전체는 안 읽음). `KiUpdateStibpPairing`·`PspUserThreadStartup`·`PspAllocateProcess` 는 이미 있었다.
  - `PspProcessClose` 가 어떻게 불리는지 보려고: `PspInitPhase0`, `NtClose`, `ObCloseHandle`, `ObpCloseHandle`, `ObCloseHandleTableEntry`, 그리고 `refs` 표 `_refs/PspProcessClose.tsv`.
- 버전 확인: **일치.** `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess`, `PspInsertThread` call `+0x802` / ret **`+0x807`** → `PspCallProcessNotifyRoutines` (`_calls.tsv`).
- 임시 도구 (하네스는 안 고침): 전체 훑기용 Ghidra 스크립트 2개, PE 파서(.pdata), 실행기, 결과 표를 `ms/ghidra/_tmp_sast28/` 에 남겼다 (`.gitignore` 대상). 아래 "찾는 법과 한계".

## 스크립트 문제

고칠 버그는 없다. 불편 2건 제안.

- **"모든 명령에서 피연산자 값 훑기" 도구가 스킬에 없다.** `refs` 는 "한 주소를 가리키는 곳"만 찾고, `_EPROCESS +0x5fc` 처럼 레지스터+변위로 읽는 칸은 못 찾는다. 이번엔 임시 Ghidra 스크립트(`ms/ghidra/_tmp_sast28/ScanDisp2.java`, `ScanLoads.java`)로 했다. 스킬에 `scan <바이너리> <변위>` 같은 명령이 있으면 "이 구조체 칸을 누가 읽고 쓰나"를 바로 물을 수 있다 (구조체 칸 질문이 계속 나온다: Q36 의 PEB, 이번 Flags3). 15초면 돈다 (명령 264만 개).
- **조각 이름이 `callers:` 줄에 그대로 남는다.** `KiDeliverApc.c` 머리 `callers:` 에 `FUN_1406b2db3` 이 있지만 정체는 `KiApcInterrupt` 의 한가운데다 (질문 2). `.asm` 의 `(= KiApcInterrupt +0x…)` 표시는 이미 맞게 나오니, `.c` 머리·`_calls.tsv` 의 caller 이름도 같은 `.pdata` 범위면 본래 함수로 바꾸면 좋다. 지금은 `_calls.tsv` 가 `KiApcInterrupt  frag:FUN_1406b2db3+0x133 …` 꼴로 오프셋이 조각 기준이다.

## 한눈에

- **질문 1: `ProcessFirstResume` (비트 15) 를 읽고 판단하는 곳은 ntoskrnl 에 딱 둘이다.**
  - `PsMultiResumeThread +0x45`: 자기가 켜기 전에 "이미 켜졌나" 검사 (켜져 있으면 `+0x4e` 쓰기를 건너뜀).
  - `PspProcessClose +0xf`: 프로세스 핸들을 닫을 때 부르는 콜백. **켜져 있으면 곧장 return.** 꺼져 있으면 "주인이 죽는 중이면 이 프로세스도 끝낸다" 검사로 내려간다.
  - **켜는 곳은 `PsMultiResumeThread +0x4e` 하나뿐이다.** 끄는 곳은 없다.
  - 유저(`NtQueryInformationProcess`)나 ETW 로 비트 15 를 내보내는 길은 없다. 거기서 읽는 Flags3 비트는 2·9·19·20·28·29 뿐이다.
- **질문 2: `FUN_1406b2db3` = `nt!KiApcInterrupt +0x273`.** 떨어진 조각이 아니라 **같은 `.pdata` 범위 안**이다 (`[0x6b2b40, 0x6b3259)`, 1817B, CHAININFO 없음). PDB 공개 이름도 이 범위에 `KiApcInterrupt` 하나뿐이다.
  - `KiDeliverApc` 를 부르는 줄: `KiApcInterrupt` call **`+0x3a6`** / ret **`+0x3ab`**.
  - flow.md 그림 2 `h0` 상자는 **`nt!KiApcInterrupt`** 로 쓰면 된다 (추정 딱지 뗌).

## 질문별 답 [정적]

### 질문 1. `ProcessFirstResume` 를 읽는 곳

#### 1-a. 읽고 판단하는 곳 (둘)

| 자리 | rva | 명령 | 하는 일 | 켜짐 / 꺼짐 |
|---|---|---|---|---|
| `PsMultiResumeThread +0x45` | `0x2c9ae5` | `+0x3f MOV R8D,0x8000` → `+0x45 TEST dword [RAX+0x5fc],R8D` → `+0x4c JNZ +0x56` | RAX = `[스레드+0x220]` (그 스레드의 프로세스). 자기가 켜기 전 검사 | 켜짐: `+0x4e OR.LOCK` 을 건너뜀 (다시 안 씀). 꺼짐: `+0x4e` 에서 켬 |
| `PspProcessClose +0xf` | `0x9b3b5f` | `TEST dword [RDX+0x5fc],0x8000` → `JZ +0x22` | RDX = 닫히는 프로세스 객체. 프로세스 객체 형식의 **닫기 콜백** | 켜짐: `+0x1b` 로 바로 return. 꺼짐: `+0x22` 이하 검사 (아래) |

**`PspProcessClose` 안을 한 번에 (읽는 줄 앞뒤만):**

- 부르는 쪽이 `Process`(닫는 프로세스, `RCX`), `Object`(닫히는 프로세스, `RDX`), `R9`(닫는 쪽이 이 객체에 가진 핸들 수) 를 넘기는 모양이다 (`OB_CLOSE_METHOD` 모양 `[추측]`, 이름만. 아래 `ObCloseHandleTableEntry` 쪽 인자 채움과 같은 자리).
- `+0x9 CMP R9,1`, `+0xd JBE +0x4e` → `+0x4e` 에서 `Object` 의 `ActiveThreads` (`+0x380`) 를 본다 (`+0x56 JNZ +0xf`).
  - 그래서 **`+0xf` 로 오는 조건 = 핸들이 둘 이상이거나, 아니면 스레드가 하나라도 살아 있을 때.**
  - 핸들 하나 + 스레드 0 이면 `+0x58` 쪽 "마지막 핸들, 스레드 없음" 길로 간다 (`PspLockProcessExclusive` `+0x76`/`+0x7b`, `Flags |= 8` 를 cmpxchg 로, `PspUnlockProcessExclusive` `+0xa9`/`+0xae`, `PspRundownProcess` `+0xcf`/`+0xd4`). 거기엔 비트 15 검사가 없다.
- `+0xf` 에서 **비트 15 가 꺼져 있으면:** `Object+0x2d8` (`OwnerProcessId`) 가 닫는 프로세스의 `UniqueProcessId` (`+0x1d0`) 와 같고 (`+0x22`~`+0x30`), 닫는 프로세스의 `Flags` (`+0x1f4`) 에 **bit 3 `ProcessDelete` 또는 bit 30 `ProcessSelfDelete`** (`0x40000008`, `+0x32`~`+0x3d`) 가 있으면 `PsTerminateProcess(Object, 0xc000010a)` 를 부른다 (call `+0x47` / ret `+0x4c`). 아니면 return.
- 읽은 뜻 `[추측]`: "한 번도 안 풀린 자식 프로세스를, 그 주인이 죽으면서 핸들을 닫을 때 같이 끝낸다" 는 지름길이다. 비트 15 가 켜졌다는 건 "이미 한 번 풀렸다" 는 뜻이라 그 지름길이 막힌다.
- **다만 test.exe 같은 일반 생성에서는 이 비교가 맞을 일이 거의 없어 보인다** `[정적 읽기 + 추측]`. `OwnerProcessId` 를 쓰는 줄 `PspAllocateProcess +0x710` 의 값은 **부모 PID 에 `|2` 를 붙인 것**이고, `PspAllocateProcess` 인자(`[RSP+0x90]`)의 bit 9 가 켜졌을 때만 붙이지 않는다 (`+0x6f1`~`+0x710`: `MOV RDX,[부모+0x1d0]`, `OR RAX,2`, `BT R8D,9`, `CMOVC`). PID 는 4 의 배수라 `|2` 가 붙으면 닫는 쪽 PID 와 같을 수 없다. 그 인자 bit 9 가 무슨 플래그인지는 안 봤다.
- 근거: `ms/disassemble/ntoskrnl/PspProcessClose.asm`, `PspAllocateProcess.asm` (`+0x6f1`~`+0x710`), `ms/decomp/ntoskrnl/PspProcessClose.c` (같은 모양으로 읽힘).

#### 1-b. 이 닫기 콜백이 어떻게 불리나

- `PspProcessClose` 를 **직접 부르는 `CALL` 은 없다** (`.c` 머리 `callers:` 비어 있음).
- **등록하는 곳:** `PspInitPhase0` 가 프로세스 객체 형식을 만들면서 `OBJECT_TYPE_INITIALIZER` 를 스택에 채운다. `+0x47e` `PspProcessDelete`, `+0x490` `PspProcessOpen`, **`+0x4a8` `LEA RAX,[PspProcessClose]` → `+0x4bb` 스택 저장** (`[RBP-0x60]`). 세 개가 PDB 구조 `_OBJECT_TYPE_INITIALIZER` 의 `OpenProcedure +0x38` / `CloseProcedure +0x40` / `DeleteProcedure +0x48` 간격과 같다. `R9 = &PsProcessType` (`+0x489`), `ObCreateObjectType` call `+0x4e6` / ret `+0x4eb`.
- **부르는 곳 (간접):** `NtClose` call `+0xe4` / ret `+0xe9` → `ObCloseHandleTableEntry`. 그 안에서 객체 형식의 `+0x80` (= `TypeInfo.CloseProcedure`) 을 읽어 (`+0x2b0` `CMP`, `+0x30c` `MOV RAX,[R14+0x80]`) 알려진 몇 개(`CmpCloseKeyObject`, `IopCloseFile` 등)와 비교해 직접 부르고, **아니면 CFG 간접 호출 `+0x7a9` (ret `+0x7ae`)**. 같은 함수 `+0xc0a` / `+0xc0f` 의 간접 호출은 다른 콜백이다 (`+0x70a MOV RAX,[RSI+0xa8]` = `TypeInfo.OkayToCloseProcedure`, `ExpWin32OkayToCloseProcedure` 와 비교한 뒤 CFG 호출 `[추측]`: RSI 가 객체 형식이라고 보면 `+0x40+0x68`). 대상이 `PspProcessClose` 인지는 정적으로 못 정한다 (간접).
- 그러니 **프로세스 핸들이 하나 닫힐 때마다** (어느 프로세스든) 이 함수가 돈다. 근거: `ms/disassemble/ntoskrnl/PspInitPhase0.asm`, `ObCloseHandleTableEntry.asm`, `_calls.tsv`, `_refs/PspProcessClose.tsv`.

#### 1-c. test.exe 시동 길에서 불리나

- `PsMultiResumeThread +0x45` — **코드상 지나는 줄이다.** 부모 `NtResumeThread` call `+0x96` / ret `+0x9b` 가 부르고 (run13·14), 이 줄에 오려면 `KeResumeThread` 가 돌려준 이전 횟수가 0 이 아니고 (`+0x32 JZ`) 요청 횟수(1) 이하여야 한다 (`+0x36 JA`). 새 스레드는 `KeSuspendThread` 로 SuspendCount 가 1 이라 이전 횟수 1 → 통과 `[정적]`. **실제로 지났는지, 비트가 그때 꺼져 있었는지는 동적** (Q19).
- `PspProcessClose` — **정적으로는 불리는지 모른다.** 길은 "누가 test.exe 프로세스 핸들을 닫나" 에 달렸다. 부모 cmd.exe 가 `CreateProcess` 끝나고 `CloseHandle(hProcess)` 하는 줄이 `main` 전인지 후인지 정적으로 알 수 없다. 만약 `main` 전에 닫으면: 부모의 `NtResumeThread` 가 이미 `+0x4e` 를 지났으니 **비트 15 는 켜져 있어야 하고** (코드 순서상 resume → CreateProcess 반환 → 핸들 닫기), 이때 스레드가 살아 있으니 (`ActiveThreads` ≠ 0) `+0xf` 로 와서 `+0x1b` 로 바로 return 한다 → 아무 일도 안 한다.
- 근거: `PsMultiResumeThread.asm` `+0x32`~`+0x4e`, `PspProcessClose.asm` `+0x4e`~`+0x56`·`+0xf`~`+0x1b`.

#### 1-d. 쓰는 곳

- **비트 15 를 켜거나 끄는 곳은 `PsMultiResumeThread +0x4e` (`OR.LOCK dword [RAX+0x5fc],R8D`, R8D = `0x8000`) 하나뿐이다.** 끄는 줄은 없다.
- `_EPROCESS +0x5fc` 에 쓰는 명령 40곳을 다 봤다. 마스크가 상수가 아닌 4곳도 값을 따라가 확인했다:
  - `PspAllocateProcess +0x640` `OR [..],EAX` → EAX = `0x20` (bit 5, `+0x634`).
  - `NtSetInformationProcess +0x203` → 비트 19·20·28·29·31 만 켠다 (바로 앞 `AND.LOCK 0x4fe7ffff` `+0x19f` 가 같은 비트들을 먼저 끈다. 로깅 정보 설정으로 보임 `[추측]`).
  - `PspSetProcessForegroundBackgroundRequest +0xd7`·`+0x125` → `0x10000` 또는 `0x20000` (bit 16·17).
  - `ExUpdateTimerResolutionPolicy +0x62` → `0x4000000` (bit 26).
- 새 프로세스를 만들 때 `PspAllocateProcess` 가 Flags3 에 쓰는 줄: `+0x61d` bit 0, `+0x640` bit 5, `+0x685` bit 7, `+0x6c0` bit 12, `+0x6ce` bit 23, `+0x6db` bit 27, `+0x6e8` bit 30. **비트 15 는 없다.** 처음엔 0 (객체가 0 으로 채워져 나온다 `[추측]`).
- 한 가지 통째 쓰기: `FUN_140bef6d0 +0x3b57` (rva `0xbf3227`, INIT 구역 부팅 코드) 이 전역(`0x140fc507c`)의 값을 `[R14+0x5fc]` 에 4바이트로 쓴다. 부팅 때 한 번, 처음 만드는 프로세스용으로 보인다 `[추측]`. test.exe 길과 상관 없다.

#### 1-e. 비트 15 를 판단에 안 쓰는 Flags3 읽기들 (헷갈리지 않게)

- 통째 복사·비교 (비트별 판단 없음): `PsSetVmProcessorHostProcess +0x37`·`+0x132` (복사본을 `ExBlockOnAddressPushLock` 의 비교 값으로 씀, call `+0x12d` / ret `+0x132`), cmpxchg 되쓰기 4곳 (`DbgkForwardException +0x78`, `PsSetProcessFaultInformation +0x90`, `MiLogReserveVaFailed +0x84`, `MiLogCommitRequestFailed +0x55`) — 비트 15 는 그대로 되쓴다.
- 읽는 비트가 다른 것: bit 0 `Minimal` (가장 많음), bit 12 `SystemProcess`, bit 7, 18, 22·23, 26, 30 …. 로깅 켜짐 검사 `PsIsProcessLoggingEnabled` 는 `&Flags3` (`LEA RCX,[RDX+0x5fc]`) 를 만들어 마스크 `0x80000000`·`0x80000`·`0x20000000`/`0x10000000`·`0x100000` 로만 검사한다 (`+0x1f`~`+0xaa`).
- `PspUserThreadStartup +0x5a` 의 `ADD RSI,0x5fc` 도 `&Flags3` 를 만든다. 읽는 곳 `+0x66`·`+0xa8` 은 `TEST AL,1` → bit 0 만 본다 (test.exe 첫 스레드가 도는 함수).

#### 1-f. 밖으로 나가는 길 (유저·ETW)

- `NtQueryInformationProcess` 가 Flags3 를 읽는 곳 셋 — `+0x251a` (비트 19·20·28·29), `+0x2ecd` (비트 2), `+0x3ff3` (비트 9). 비트 15 없음.
- `NtQuerySystemInformation` 쪽 `ExpGetProcessInformation` (`+0x5f0` bit 12, `+0xab7` bit 30), ETW (`EtwpPsProvProcessEnumCallback`, `EtwpObjectHandleRundown`, `EtwpWriteAppStateChangeWithStats`, `EtwpCovSampProcessEnsureContext`, `EtwQueryPerformanceTraceInformation`) 도 비트 15 를 안 읽는다. `EtwTiLogSuspendResumeThread` 는 Flags3 칸을 아예 안 읽는다 (그 함수에 hit 없음).
- 결론: **비트 15 는 커널 안에서 위 둘에만 쓰이고 바깥으로 안 나간다** (이 이미지 안에서, 아래 한계 조건 안에서).

#### 1-g. 찾는 법과 한계 (요청서 요구)

**방법 (세 겹)**

1. **모든 명령 훑기.** Ghidra 가 읽은 ntoskrnl 의 **명령 2,645,299개** 전부에서 피연산자에 값 `0x5f8`~`0x5ff` 가 든 것을 뽑았다 (직접 변위 `[reg+0x5fc]`, 즉값 둘 다). 364개, 그중 `+0x5fc` 는 **226개 / 149개 함수.** (임시 스크립트 `ScanDisp2.java`, 결과 `flags3_disp_hits.tsv`.)
2. **원본 바이트 교차 확인.** 실행 구역 전부(`.text`, `PAGE`, `INIT` 등 18개)에서 `fc 05 00 00` (변위 `0x5fc` 의 바이트) 를 찾았다: **233곳.** 그중 226곳이 1번의 명령과 같고, 나머지 7곳은 명령이 아니다 (rel32 분기·호출의 변위 6곳 `0x258d5c`·`0x324275`·`0x91ad08`·`0x91f2a1`·`0x93a7d3`·`0x9cdb3c`, 표 데이터 1곳 `0xb51743`, `objdump` 로 확인). 즉 **Ghidra 가 못 읽은 코드 속에 숨은 `[reg+0x5fc]` 는 없다.** 폭이 다른 접근도 봤다: 비트 15 가 든 바이트 `+0x5fd` 를 쓰는 명령은 0개, `+0x5f8`~`+0x5fb` 의 넓은(dword·qword) 접근은 `_EPROCESS` 가 아닌 구조체(ETW 버퍼, 스택)뿐.
3. **포인터를 레지스터에 들고 있는 꼴.** `LEA`/`ADD 레지스터,상수` 로 `&Flags3` 를 만든 곳 6곳(`PsIsProcessLoggingEnabled` 4, `PsSetVmProcessorHostProcess` 1, `PspUserThreadStartup` 1)을 따라가 읽는 줄을 봤다. 일반화한 추적(상수 `0x100`~`0x1000` 치우친 기준 + 변위가 `0x5fc`~`0x5ff` 가 되는 접근)은 이 6곳 말고 **0건.** 추적이 동작하는 건 `+0x5f8` 로 시험해서 확인했다 (18건).
4. **읽어 온 값 따라가기.** `[reg+0x5fc]` 를 레지스터로 읽는 `MOV` 103개를 각각 뒤따르는 직선 코드(최대 120명령)에서 어떻게 쓰는지 뽑아 (`flags3_loads_uses.tsv`) 마스크·비트 번호를 읽었다. 비트 15 를 마스크로 쓴 로드는 **0건** (자동 표시 2건은 `AND RAX,-8` 오탐: `KiGetProcessCpuSetMaskPointer`, `KiQueryCpuSetsProcessWithLock` 은 비트 7 만 봄). 상수가 아닌 마스크 레지스터(`TEST R13B,CL` 류)는 바이트 레지스터라 비트 0~7 뿐이다.

**놓칠 수 있는 꼴 (한계)**

- **범위는 이 `ntoskrnl.exe` 파일 하나.** `hal`, `win32k*`, 드라이버, 실행 중에 고쳐지는 코드(핫패치)는 안 봤다.
- **포인터를 다른 함수에 넘겨 거기서 읽는 꼴**은 못 따라간다. 예: `PsSetVmProcessorHostProcess +0x12a` 가 `&Flags3` 를 `ExBlockOnAddressPushLock` 에 넘긴다. 그 함수는 4바이트를 통째로 비교하는 쓰임이라 비트 15 를 판정에 쓰지 않을 걸로 `[추측]`, 안 읽었다.
- **포인터를 메모리(스택·구조체)에 저장했다 다시 불러 쓰는 꼴**은 안 따라간다. 예: `PspUserThreadStartup +0x61` 이 `&Flags3` 를 `[RSP+0x78]` 에 저장하고, 예외 착지점(`.asm` 의 "읽지 않은 틈" `+0x80`~`+0x90`)에서 `RSI` 로 되살린다 (`objdump` 로 확인). 그 뒤에 읽는 줄은 `+0xa8` 의 bit 0 뿐이었다.
- **치우친 기준을 `LEA`/`ADD 상수` 가 아닌 식**(곱셈, `SUB`, 두 번 더하기, 레지스터 둘 더하기)으로 만든 꼴, 분기를 넘나드는 추적, 함수 경계를 넘는 추적은 안 한다. 추적은 "주소 순서 직선, 그 레지스터를 덮어쓰면 끊음" 근사다.
- **실행 중에 오프셋을 계산하는 접근**(필드 오프셋 표를 돌며 읽기)은 정적으로 못 찾는다.
- **같은 `+0x5fc` 를 쓰는 다른 구조체**는 이름·쓰임으로 걸렀다 (`FsRtlMdlReadCompleteDevEx +0x11f54`, `FUN_14050bbb0 +0xe24a`, `KiSwInterruptDispatch +0x10c0`, `FUN_140be6df0 +0x8a0` 의 `MOV EAX,[reg+0x5fc]; SUB RCX,RAX` 는 스택 한계 계산으로 보이며 `_EPROCESS` 아님 `[추측]`). 이것들은 비트 15 를 판정하지 않아 결론엔 영향이 없다.
- 읽는 비트가 레지스터 마스크인 `TEST` 몇 개(`PsMultiResumeThread`, `MiForceCrashForInvalidAccess` 등)는 마스크를 만든 줄(`MOV R8D,0x8000` 등)을 `.asm` 에서 직접 읽어 확인했다. 자동으로 못 가려낸 건 사람이 읽었다.

### 질문 2. `FUN_1406b2db3` 의 정체

- **답: `nt!KiApcInterrupt +0x273`.** 이름은 `FUN_1406b2db3` 이지만 `KiApcInterrupt` 의 **같은 `.pdata` 범위 안**이다. 떨어진 조각도 다른 함수도 아니다.
- 근거 (`.pdata` = RUNTIME_FUNCTION, 직접 파서로 읽음. `ms/ghidra/_tmp_sast28/q2.py`):

| 항목 | 값 |
|---|---|
| `0x6b2db3` 를 감싸는 RUNTIME_FUNCTION | `[0x6b2b40, 0x6b3259)`, **1817바이트**, 배열 번호 19047 |
| unwind 정보 | 버전 2, flags 3 (예외·종료 처리기 있음, 처리기 = `KiFatalExceptionHandler` rva `0x5b1764`), **`UNW_FLAG_CHAININFO` 없음** → 부모 함수 없음 |
| 이 범위로 체인되는 다른 RUNTIME_FUNCTION | 없음 |
| PDB 공개 이름 (`llvm-pdbutil dump --publics`) | 이 범위에 `KiApcInterrupt` (`0x6b2b40`) 하나. 다음 이름은 `KiInitiateUserApc` (`0x6b3260`) |
| `pdb_aliases.py --lookup ntoskrnl.exe 0x6b2db3` | "별칭 무리 아님" — 그 주소에 PDB 이름이 따로 없다. 바로 앞 이름 `KiApcInterrupt` 에서 `+0x273` |

- Ghidra 가 이 범위를 쪼갠 모양: `KiApcInterrupt` 348B (`0x6b2b40`~`0x6b2c9c`) + RSB 채우기 사다리 279B + **`FUN_1406b2db3` 753B** + 사다리 279B + `FUN_1406b3170` 158B = **1817B** (`.pdata` 범위와 딱 맞는다). 사다리는 `ADD RSP,8; CALL` 9바이트 쌍 31칸 (`KiApcInterrupt +0x15c`~`+0x272`, `+0x519`~`+0x62f`) 이고 읽을 코드가 아니다. `FUN_1406b3170` = `KiApcInterrupt +0x630` (끝 갈래, `+0x6b2` 에서 `JMP KiKernelExit`).
- **`KiDeliverApc` 부르는 줄:** 조각 기준 `FUN_1406b2db3 +0x133` / ret `+0x138` = **`KiApcInterrupt` call `+0x3a6` / ret `+0x3ab`** (절대 `0x1406b2ee6` / `0x1406b2eeb`). 인자는 `ECX = 0`, `EDX = 0`, `R8 = [RBP-0x80]` (트랩 프레임으로 보임 `[추측]`), 바로 앞에 `HalPerformEndOfInterrupt` (call `+0x38a` / ret `+0x38f`) 와 `STI` (`+0x38f`) 가 있다. `KiDeliverApc` 를 부르기 전 `CMP byte GS:[0x187],0` 검사 (`+0x390`) 가 있어 걸리면 건너뛴다.
- 같은 함수의 다른 눈에 띄는 호출 (참고): `KiInitiateUserApc` call `+0x401` / ret `+0x406`, 끝에 `KiKernelExit` 꼬리호출 `+0x6b2`.
- 이름을 어떻게 쓰나: flow.md 그림 2 `h0` 상자와 2.2 6번은 **`nt!KiApcInterrupt`** 로 쓰면 된다. WinDbg 가 `nt!KiApcInterrupt+0x3ab` 로 보여 줄지는 PDB 공개 이름이 하나뿐이라 그럴 걸로 `[추측]` (동적에서 확인).
- 근거: `ms/disassemble/ntoskrnl/KiApcInterrupt.asm` (`+0x157` FAKECALL, `(= KiApcInterrupt +0x…)` 표시 전부), `_calls.tsv` (`KiApcInterrupt` 줄 14개), `ms/decomp/ntoskrnl/KiApcInterrupt.c` (`HalPerformEndOfInterrupt(0)` → `KiDeliverApc(0,0,&…)`).

## flow.md 에 넣을 글 (제안, 요청서 "받고 싶은 것")

- **2.2 `[공백]` 한 줄 → 해결:** "`ProcessFirstResume` 를 읽고 판단하는 곳은 ntoskrnl 에 둘 (`[정적]`, run33): `PsMultiResumeThread +0x45` (자기가 켜기 전 '이미 켜졌나' 검사)와 `PspProcessClose +0xf` (프로세스 핸들 닫기 콜백, 켜져 있으면 곧장 return, 꺼져 있으면 '주인이 죽는 중이면 끝낸다' 검사). 켜는 곳은 `+0x4e` 하나, 끄는 곳 없음. 유저·ETW 로 나가는 길 없음. 시동 길에서 실제로 서는지는 동적."
- **2.2 6번 `[추측]` 한 줄 → 해결:** "`FUN_1406b2db3` 은 `nt!KiApcInterrupt +0x273` 이다 (같은 `.pdata` 범위 `[0x6b2b40, 0x6b3259)`, 체인 없음, PDB 이름 하나 `[정적]`). APC 인터럽트가 `KiDeliverApc` 를 부르는 자리는 `KiApcInterrupt` call `+0x3a6` / ret `+0x3ab`."
- **그림 2 `h0` 상자 글:** `nt!KiApcInterrupt<br/>(정적: .pdata·PDB 로 확정. +0x3a6 에서 KiDeliverApc 호출, 돌아올 자리 +0x3ab)`.
- **4장 2.2 두 줄 판정:**
  1. `ProcessFirstResume` 읽는 곳 — **(해결, 정적)** 둘: `PsMultiResumeThread +0x45`, `PspProcessClose +0xf`. 남은 동적: `PspProcessClose` 가 `main` 전에 서나, 그때 비트 15 값 (Q19 에 합침).
  2. `FUN_1406b2db3` 의 정체 — **(해결, 정적)** `KiApcInterrupt +0x273`. 그림 2 표·`4.1` 그림 2 nt 줄의 "인터럽트 조각 이름 `[추측]`" 은 이걸로 닫힌다. 남은 동적: `k` 에서 실제로 `nt!KiApcInterrupt+0x3ab` 로 보이나 (Q42·Q19 에 합침).
- 그 밖에 장부에서 caller 이름이 `FUN_1406b2db3` 로 적힌 곳(`KiDeliverApc` 의 `callers:` 등)은 `KiApcInterrupt` 로 읽으면 된다.

## 동적분석으로 확인해야 할 것

이미 대기 중인 **Q19** (첫 스레드가 풀리는 시점)·**Q42** (멈춤 APC 인터럽트가 들어오는 자리) 에 얹는 걸 제안. 새 번호는 오케스트레이터가.

- **dast 요청 필요: Q19 에 합치기** — 비트 15 가 풀기 전엔 0, 후엔 1 인가.
  - `nt+2c9ae5` (`PsMultiResumeThread +0x45`, test.exe 스레드일 때만) 에서 `dd poi(@rsi+0x220)+0x5fc L1` → 기대: 비트 15(`0x8000`) 꺼짐. `nt+2c9af6` (`+0x56`) 에서 같은 칸 → 켜짐.
- **dast 요청 필요: 새 항목 하나 (Q19 와 같은 실행)** — `PspProcessClose` 가 test.exe 프로세스 객체로 `main` 전에 서나, 서면 `+0xf` 에서 값이 무엇인가.
  - `nt+9b3b50` 입구, 조건 `@rdx == <test.exe EPROCESS>` (`!process 0 0 test.exe` 로 구함). 서면 `k` (누가 닫나: `NtClose` → `ObCloseHandleTableEntry`) 와 `dd @rdx+0x5fc L1`. 기대: 비트 15 켜짐 → `+0x1b` (`nt+9b3b6b`) 로 return.
  - 안 서면 그 자체가 답이다 (`main` 까지 test.exe 프로세스 핸들을 닫는 곳이 없다).
- **dast 요청 필요: Q42 에 합치기** — 멈춘 스레드 스택에서 `KiDeliverApc+0x220` 바로 아래가 `nt!KiApcInterrupt+0x3ab` 로 보이나.
  - `KiSchedulerApc +0xf2` 에서 `k`. 또는 `nt+6b2ee6` (`KiApcInterrupt +0x3a6`, call) / `nt+6b2eeb` (`+0x3ab`, ret). `ln nt+6b2db3` → `nt!KiApcInterrupt+0x273` 기대.

## 새로 생긴 공백 (대기열 제안)

- (정적, 작게) `PspAllocateProcess` 인자 `[RSP+0x90]` 의 bit 9 가 무슨 플래그인가 (`OwnerProcessId` 에 `|2` 를 안 붙이는 조건, `+0x707 BT R8D,9`). 그리고 `OwnerProcessId` (`+0x2d8`) 를 쓰는 다른 줄 `NtSetInformationProcess +0x1ecd` (rva `0x93487d`, 어떤 정보 클래스인지) — `PspProcessClose` 의 검사가 일반 생성에서 맞는지 가르는 값이다.
- (정적, 작게) `ObCloseHandleTableEntry` 의 간접 호출 `+0x7a9` 앞에서 인자를 어떻게 채우나 (`R8` 이 `GrantedAccess` 인지) — `PspProcessClose` 인자 이름은 지금 `[추측]`.

## 판정

**분석성공.** 질문 1·2 모두 `.asm`·`.pdata`·PDB 줄 근거로 답했다. 질문 1 은 "읽고 판단하는 곳 둘" 이 결론이고, 놓칠 수 있는 꼴(포인터를 다른 함수로 넘김·메모리에 저장, 다른 모듈)은 위 한계에 적었다. `PspProcessClose` 가 test.exe 시동 길에서 서는지, 질문 1 의 `OwnerProcessId` 비교가 일반 생성에서 맞는지는 정적으로 못 정해 동적·공백으로 넘겼다.

## 메모 (이번에 만든 임시 파일)

- `ms/ghidra/_tmp_sast28/` (커밋 대상 아님): `ScanDisp2.java` (변위 훑기 + 치우친 기준 추적), `ScanLoads.java` (읽어 온 값 따라가기), `run_ghidra.sh` 의 `headless()` 와 같은 모양의 `run_scan.sh`, `pelib.py`·`q2.py` (`.pdata` 읽기), 결과 `flags3_disp_hits.tsv`·`flags3_bias_hits.tsv`·`flags3_loads_uses.tsv`.
- 구조체 칸·비트 이름은 PDB `llvm-pdbutil-14 dump -types`, 공개 심볼 위치는 `dump --publics` 로 읽었다 (세션 임시 폴더).
- `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/` 에 위 "뽑은 함수" 의 `.c`·`.asm` 이 추가되었고 `_calls.tsv` 에 줄이 덧붙었다. `_refs/PspProcessClose.tsv` 가 생겼다 (커밋 대상 아님).

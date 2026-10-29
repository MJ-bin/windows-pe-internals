# 45 eprocess-later — 정적 분석 결과 (Q78c2b)

- 요청서: `_workspace/sast/45_eprocess-later.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`, 기준과 같음).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 다 있음, Ghidra 프로세스 없음).
- **새로 뽑은 함수 27개, 전부 depth 0** (`ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`). `_calls.tsv` 는 매번 `comm -23` 0줄 (줄이 안 사라짐). ntoskrnl 3,744 → 4,281줄.
  - ntoskrnl 21: `NtSetInformationProcess` (큰 함수, 아래 갈래만 읽음), `PsSetProcessWin32Process`, `ExpWnfCreateProcessContext` (요청서 표) + **요청서 표에 없던 18개 — 칸을 실제로 쓰는 줄이 있어서 넣음**: `PspNotifyThreadCreation`, `DbgkCreateThread`, `DbgkCreateMinimalProcess`, `PsCallImageNotifyRoutines`, `PfProcessCreateNotification`, `PfCalculateProcessHash`, `PfSnActivateTrace`, `PfSnAddProcessTrace`, `PfSnBeginAppLaunch`, `ObpSetCurrentProcessDeviceMap`, `ObpReferenceCurrentDeviceMap`, `EtwpAddDataSource`, `EtwpAddNotificationEvent`, `EtwpQueueNotification`, `RtlInsertDynamicUserFunctionTable`, `NtSetWnfProcessNotificationEvent`, `ExpWnfResolveScopeInstance`, `EtwExitProcess`.
  - ntdll 4 (`EtwpRegisterTpNotificationOnce`, `LdrpEnableUMGLTracingStateSync`, `RtlpInitializeWnf`, `RtlpWnfRegisterTpNotification`), kernelbase 1 (`ConsoleCommitState`), csrsrv 1 (`CsrSetExceptionPort`) — 부르는 쪽 이름·정보 클래스를 찾느라 한 단계씩만 열었다 (사용자 모드 본문은 이 줄들만).
  - 이미 `.c` 가 있어서 **읽기만 한 것** (다시 안 뽑음): `PspInsertProcess`, `PspInsertThread`, `PspUserThreadStartup`, `PspDisablePrimaryTokenExchange`, `PspSetProcessPpmPolicy`, `PspCallProcessNotifyRoutines`, `NtQueryInformationProcess`, `ObCloseHandleTableEntry`, `PspProcessClose`, `PspAllocateProcess`, `ObpLookupObjectName`, `ObInitProcess`, `ExDupHandleTable`, `MiMapProcessExecutable`.
- **Ghidra 로 안 뽑고 `objdump -d` / PE 직접 읽기로만 본 곳** (함수+오프셋으로만 적는다, `.asm` 없음): 아래 목록 전부 이름을 `_index.tsv` 로 한 번 더 확인했다 (모두 있음). 전역 변수 이름 (`PspBamHostInterface` 등) 은 함수 목록이 아니라 확인 대상 아님.
  - 커널: `MiInsertPageInList +0x1fc`·`MiInsertPagesInList +0x64a` (rva `0x323f30`·`0x25a340`), `MiInsertSharedCommitNode` (`0x909480`) 호출자 줄, `MiMapViewOfImageSection +0x4e9` (`0x9794ec`), `PfSnBeginScenario` (`0x944930`)·`PfSnBeginTrace` (`0x946758`)·`PfSnCheckScenario` (`0x9448cc`), `PsConvertToGuiThread +0x1e0 / +0x238` (`0x9b3d60`), `NtTraceControl +0x69f ~ +0x775` (`0x9cc370`), `RtlpLookupDynamicUserFunctionTable`, 그리고 **칸 오프셋으로 쓰는 줄을 커널 전체에서 훑은 것** (`[reg+칸오프셋]`, 오탐 있음).
  - 사용자 모드·드라이버: 아래 "부르는 쪽" 줄 중 `[objdump]` 표시. win32kbase/win32kfull/win32k 는 **공개 PDB 를 새로 받아** (`ms/sym/win32k*.pdb`, 커밋 안 함) 가장 가까운 앞쪽 공개 함수 이름으로 호출 자리를 찾았다. `mssecflt.sys`·`bam.sys` PDB 도 받았지만 함수 이름이 없어 rva 로만 적는다.
- 버전 확인: **일치.** `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess`; `PspInsertThread` ret **`+0x807`** → `PspCallProcessNotifyRoutines`; ntdll `LdrpInitializeProcess` ret **`+0x1bbb`** → `LdrpInitializeGraphRecurse`. 값 대조: `OwnerProcessId` K1 `0x19a8` 는 `PspAllocateProcess +0x710` 뒤 값 (4의 배수 = 태그 `|2` 가 안 붙은 모양, sast/29 의 "ProcessFlags 비트 9" 와 맞음); `0x729` 의 아래 2비트 = `1` 은 커널이 요구하는 모양과 맞음 (5번).

## 스크립트 문제

없다. (도구 쪽 제안은 맨 아래 "도구 평가".)

## 요약 표 (30칸 + `OwnerProcessId` 둘째 쓰기 → ?)

"쓰는 줄" = 그 칸을 쓰는 커널 줄 (`.asm` 에서 확인). "누가 부르나" = 그 줄까지 이어지는 부르는 쪽. 둘을 갈랐다.

| 구간 | 칸 | nt 직접 (커널 단계, syscall 아님) | syscall 안 (test.exe 자신이 부름) | 다른 프로세스·드라이버가 부름 | 여전히 모름 |
|---|---|---|---|---|---|
| `K1 이전` | 1 | — | — | — | **1** (`SharedCommitCharge`, 줄 후보 좁힘) |
| `K2~K3` | 3 | **3** | — | — | — |
| `K3~P0` | 11 | — | — | **11** (csrss 2 · `mssecflt.sys` 6 · bam 3 `[추측]`) | — |
| `P0~P1` | 9 | **9** | — | — | — |
| `P1~P1a` | 5 | — | **5** | — | — |
| `P1a~P1b` | 1 | — | — | **1** (win32k 콜아웃) | — |
| **합 (30)** | 30 | **12** | **5** | **12** | **1** |
| `OwnerProcessId` 둘째 쓰기 | +1 | — | **1** (KERNELBASE 가 부른 `NtSetInformationProcess`) | — | — |

- **쓰는 줄은 29칸 확정** (정확). 모르는 건 `SharedCommitCharge` 1칸.
- 부르는 쪽 이름이 `[정적]` 확정인 것: csrss (`ExceptionPort*`), test.exe 자신의 ntdll·KERNELBASE (P1~P1b 6개 — 단 `DeviceMap` 의 ntdll 쪽 호출자는 `[공백]`). `[정적]` 호출 줄은 있지만 "test.exe 실행에서 정말 그 호출자가 불렸는지" 는 동적인 것: `mssecflt.sys`, `bam.sys`, win32k.
- 단계 표시: `K2~K3` · `P0~P1` 은 `①` (커널 안, 유저로 넘어가기 전). `K3~P0` 도 `①` (부모의 `NtCreateUserProcess` 끝 ~ 자식 첫 스레드 시작 사이). `P1~P1a`·`P1a~P1b` 는 `②` (ntdll 이 부른 syscall).
- **새 칸 이름 짚기** (3.11 에 반영할 정정):
  - `OwnerProcessId` 는 "부모 PID" 가 아니라 **"NtCreateUserProcess 를 부른 프로세스의 PID"** 다 (`PspAllocateProcess +0x6f1 ~ +0x710`: `[RSP+0xe8]` = `GS:[0x188]→+0xb8` 현재 프로세스). cmd 가 부모이자 호출자라 값은 같다.
  - 같은 칸이 **콘솔 호스트 프로세스 표시**로 다시 쓰인다 (정보 클래스 `0x31`). 5번.
  - `Cookie` 는 복제 길 (`PspAllocateProcess +0x2183`, 섹션 없음, test.exe 는 안 탐) 에서도 쓴다 — 부모 값 복사.

## 질문별 답 [정적]

정보 클래스 번호는 `[정적]` (코드의 점프 표·비교 상수). **이름은 `[추측]`**: ntoskrnl PDB 에 `PROCESSINFOCLASS` 열거형이 **없다** (`llvm-pdbutil dump -types` 에서 `ProcessCookie`·`ProcessBasicInformation` 등 하나도 안 나옴). 이름은 공개 문서·phnt 표 기억과 코드 동작이 맞는 것만 붙였다.

| 번호 | 이름 `[추측]` | 맞는 근거 (코드) |
|---|---|---|
| `0x8` | `ProcessExceptionPort` | 포트 핸들을 `LpcPortObjectType` 으로 참조 + 권한 `SeTcbPrivilege` + 접근 `0x800` |
| `0x24` | `ProcessCookie` | 0 이면 난수 만들어 한 번만 넣음 (아래 4번) |
| `0x31` | `ProcessConsoleHostProcess` | 조회 (`NtQueryInformationProcess`) 클래스 `0x31` 이 `OwnerProcessId` 를 그대로 돌려줌 (5번) |
| `0x35` | `ProcessDynamicFunctionTableInformation` | 커널 함수 이름이 `RtlInsertDynamicUserFunctionTable` / `RtlRemove…`, 입력 16바이트 (+8 에 지움 표시) |
| `0x51` | `ProcessWakeInformation` | 출력 `0x30` 바이트 = `WakeInfo` 크기, `bam.sys` 가 `0x30` 으로 묻는 줄 (3번) |
| `0x57` | `ProcessEnableReadWriteVmLogging` | 입력 1바이트, 비트 0·1 → `Flags2` 비트 24·25 |
| `0x60` | `ProcessEnableLogging` | 입력 4바이트, 비트 0~6 → `Flags2` 24·25 + `Flags3` 19·20·28·29·31 |

### 1. "Q78c2b 에서 볼 것" 메모 확인 — 전부 **예**

| 메모 | 확인 | 줄 · 조건 |
|---|---|---|
| `Flags.ProcessInserted` `PspInsertProcess +0x202` | **예** | `OR.LOCK [RBX+0x1f4],0x4000000` (비트 26). `ObInsertObjectEx` (`+0x1de`) 가 성공한 뒤. **`Job` (`+0x2a0`) 이 0 이면 이 줄**, 0 이 아니면 `+0x25b → PspValidateJobAffinityState (+0x25e)` 가 같은 비트를 켠다 (그 함수 `+0x33` 에 `lock or … 0x4000000`). test.exe 는 `Job`=0 (sast/43) → `+0x202`. `.asm`: `PspInsertProcess.asm` |
| `ActiveThreadsHighWatermark` `PspInsertThread +0x678` | **예** | `ActiveThreads` (`+0x380`) 를 `+0x65a` 에서 1 올린 뒤 (`+0x661 ~ +0x66f`) 더 크면 `+0x678` 이 `+0x5d8` 에 씀. **스레드를 달 때마다** 돈다 (뒤의 일꾼 스레드 때도) |
| `Flags4` `PspInsertThread +0x67f` | **예** | `BTS.LOCK [R15+0x7dc],0` = `ThreadWasActive`. 옛 값을 받아 (`SETC CL`) `+0x696` 에서 `ActiveThreads == 1` 이고 옛 값이 0 이면 지역 `EDI |= 2` (첫 활성 스레드 표시). 짐작 → **정확**. `Flags4` 다른 비트 쓰는 곳: `PspAllocateProcess +0x660 / +0xea2 / +0xecd` (K1 이전, 안 탐 포함), `PspMarkSessionReferenceProcess +0x38` (비트 3), `PsTerminateMinimalProcess +0x9f` |
| `PrimaryTokenFrozen` 두 줄 | **예, 둘 다** | ① `PspUserThreadStartup +0x37 (호출) → PspDisablePrimaryTokenExchange +0x22` (`BTS.LOCK [RSI+0x1f0],0xf`): **스레드가 시작할 때마다** 부르지만 `+0x16` 에서 이미 켜져 있으면 건너뜀 → **첫 스레드가 켠다**. ② `PspInsertThread +0x77b`: 조건 = `ActiveThreads` 가 올린 뒤 **정확히 2** (`+0x69c ~ +0x6a6`) 이고 아직 안 켜졌을 때 (`+0x76a ~ +0x775`) = **둘째 스레드를 달 때**. test.exe 는 첫 스레드 시작 (P0) 에서 이미 켜져서 둘째 줄은 안 탄다 (일꾼 스레드는 P1a~P1b) |
| `AddressPolicyFrozen`·`SecurityFreezeComplete` `PspDisablePrimaryTokenExchange +0xbb · +0xa4 · +0x111` | **예** | 입구에서 `SecurityFreezeComplete` (`Flags3` 비트 22) 가 이미 켜져 있으면 바로 끝 (`+0x36`). 아니면 `KeKvaShadowingActive` (`+0xb2`) 가 0 이면 `+0xbb` (비트 14 = `AddressPolicyFrozen`) → `+0xc4` (비트 22 = `SecurityFreezeComplete`, 이미 켜져 있었으면 끝, 아니면 `PspOneDirectionSecurityDomainCombine`). 0 이 아니면 `+0xed ~ +0x111` 로 가서 주소 정책 동기화 (`KeSynchronizeAddressPolicy +0x10c`) 뒤 `+0x111` (비트 14) → `+0xa4`. 메모의 "`+0xa4 · +0xbb · +0x111`" 맞음. 지움 줄 `+0x94 · +0xa2` (`SecurityDomain`·`ParentSecurityDomain` = 0) 은 `EBX ≠ 0` 일 때만 (`+0x90`): 토큰이 관리자이거나 `Flags3` 비트 23 이 아니면 — 조건 값은 동적 |
| `BamPpmPolicy` `PspSetProcessPpmPolicy +0xa5` | **예** | `MOV [RBP+0x820],EDI` (둘째 인자). 조건: `Feature_UserPresenceThrottling` 켜짐 (`+0x1d`) 이고 `Feature_DisableUserPresenceQosByPolicy` 켜짐 (`+0x4a`) → 락 `+0x818` 잡고 씀. 둘 중 하나라도 꺼지면 `KeSetProcessPpmPolicy` 만 부르고 이 칸은 안 씀 (`+0x26 ~ +0x2b`). **`PspSetProcessPpmPolicy` 를 직접 부르는 커널 줄은 없다** (objdump 훑기) → 함수표 `PspBamHostInterface` 경유 (run49) `[정적 부재 + 추측]` |

### 2. K3~P0 11칸 — 누가 어떤 syscall·정보 클래스로

**답: 11칸 모두 `NtSetInformationProcess`·`NtQueryInformationProcess` 갈래와 `PspSetProcessPpmPolicy` 가 쓴다. 만들기 길 (`PspAllocateProcess`) 에는 쓰는 줄이 없다는 sast/44 와 맞다. 커널 안에서 이 칸들을 스스로 쓰는 호출자도 없다** (`ZwSetInformationProcess` 호출자는 `CmpInitializeRegistryProcess`·`RtlCreateUserStack`·`StartFirstUserProcess` 뿐, 클래스 달라서 이 칸 아님) → **전부 바깥 (유저 프로세스·드라이버) 이 부른다.**

| 칸 | 쓰는 줄 (닫힘) | 누가 부르나 |
|---|---|---|
| `ExceptionPortData` · `ExceptionPortValue` (+ `ExceptionPortState` 0:3) `+0x240` | `NtSetInformationProcess` **클래스 `0x8`**, 길이 8 (`+0xcb5`) 또는 16 (`+0xcbf`). 16바이트 모양 = {포트 핸들, 상태 플래그}. 상태 `> 7` 이면 거절 (`+0x1022`). **권한 `SeTcbPrivilege`** (`+0x103a`, 없으면 `0xc0000061`), 포트 핸들은 `LpcPortObjectType` 으로 참조 (`+0x1071`), 대상 프로세스는 접근 `0x800` 으로 참조 (`+0x10c9`). `RDX = 포트 객체 주소 \| 상태` 를 **`lock cmpxchg [Process+0x240]`** (`+0x1128`, 기대값 = 읽어 둔 옛 값). 옛 값이 0 이 아니면 옛 포트에 알림 메시지 (`LpcRequestPort`, `+0x229c ~ +0x2313`) | **csrss** `[정적]`: 부모(cmd) KERNELBASE `CreateProcessInternalW` (호출 +0x466b) → `CsrClientCallServer` → csrss `BaseSrvCreateProcess2` (호출 +0x216) → `InternalBaseSrvCreateProcess` (호출 +0x175) → `CsrCreateProcess` → `CsrpCreateProcess` (호출 +0x34d) → **`CsrSetExceptionPort` (호출 +0x4f 8바이트 / +0x7b 16바이트) → `NtSetInformationProcess(자식 핸들, 8, {CsrApiPort, 상태}, 8 or 16)`**. 값 = csrss 의 `CsrApiPort` 객체 주소 (+ 상태 비트). 부모 쪽 호출 순서가 `NtResumeThread` (호출 +0x5bcc) 앞이라 K3~P0 와 맞음 |
| `Flags2.EnableWriteVmLogging` (25) · `Flags3` · `.EnableProcessSuspendResumeLogging` (19) · `.EnableThreadSuspendResumeLogging` (20) · `.EnableProcessRemoteExecProtectVmLogging` (28) · `.EnableProcessLocalExecProtectVmLogging` (29) | `NtSetInformationProcess` **클래스 `0x60`** (또는 `0x57`). 길이 4 (`0x57` 은 1 이상). 먼저 **`Flags3 &= 0x4fe7ffff`** (`+0x19f`, 비트 19·20·28·29·31 지움) → 입력 비트로 다시 켬 (`+0x203`): 입력 비트 2→19, 3→20, 4→29(**로컬** 실행 보호), 5→28(**원격** 실행 보호), 6→31. **`Flags2 &= 0xfcffffff`** (`+0x20f`) → 입력 비트 0→24 (`EnableReadVmLogging`), 1→25 (`EnableWriteVmLogging`) 로 켬 (`+0x23d`). 앞에서 `EtwCheckSecurityLoggerAccess` (`+0xfa`) 와 권한 `SeDebugPrivilege` 또는 `SeTcbPrivilege` (`+0x112`, `+0xd1a`) 검사, 입력 상위 비트가 있으면 거절 (`+0x151`), 대상은 접근 `0x2000` 으로 참조 (`+0x18d`) | **드라이버 `mssecflt.sys`** `[정적: 호출 줄] / [추측: 이 호출이 test.exe 에서 불림]`: 함수 rva `0xbe90` (이름 없음) 안 rva `0xbef6` 에서 `ZwSetInformationProcess(핸들 = [컨텍스트+0x258], 클래스 0x60 (= 4+0x5c), 값 4바이트, 길이 4)`. 값 = 컨텍스트 `+0x2d0` 의 하위 비트들(0~3, 조건부 4·5, 6)을 전역 설정 비트로 걸러 만든 것. 이 함수를 부르는 곳 6곳 (rva `0xb661`·`0x2dbe8`·`0x30c99`·`0x3c1b9`·`0x3c219`·`0x3c29d`). `mssecflt.sys` 는 `PsSetCreateProcessNotifyRoutineEx`·`PsSetLoadImageNotifyRoutine` 를 import → `PspCallProcessNotifyRoutines` (K3) 안에서 도는 콜백이 유력 `[추측]`. 드라이버 폴더 전체에서 `ZwSetInformationProcess` 를 import 하는 건 `mssecflt.sys`·`condrv.sys` (클래스 `0x1f`)·`ahcache.sys` (클래스 값을 안 따라감) 셋뿐이고, `bam.sys`·`WdFilter.sys` 는 import 자체가 없다 |
| `WakeChannel` · `WakeInfo` `+0x720` (48B) | `NtQueryInformationProcess` **클래스 `0x51`** (`+0xcac` 갈래, `+0xd0d` 에서 `PspAllocateAndQueryProcessNotificationChannel` 를 부름). 그 함수: `WakeChannel` 이 0 이면 (`+0x5e`) 알림 채널 이름을 새로 만들고 (`+0xa1 ~ +0xf4`), **프로세스 락 안에서 `[Process+0x720]` 에 씀 (`+0x11d`)**, `[Process+0x744]` 에도 한 칸 씀 (`+0x124`), 결과 48바이트를 호출자 버퍼로 복사. **묻기만 해도 만들어진다 (게으른 만들기).** 이 칸을 쓰는 다른 줄은 없음. `PspChargeProcessWakeCounter` 는 읽기만 (ALPC 메시지 보낼 때) | **드라이버 `bam.sys`** `[정적: 호출 줄] / [추측: 시점]`: rva `0x161f4` 에서 `ESI=0` → `ZwQueryInformationProcess(핸들 = [컨텍스트+0x68], 클래스 = 0x51 (rsi+0x51), 출력 [컨텍스트+0x78], 길이 0x30 (rsi+0x30))` (call rva `0x1621b`, ret `0x16222`). 길이 `0x30` = `WakeInfo` 크기와 같음. `WdFilter.sys` 는 `ZwQueryInformationProcess` 를 클래스 `0x4b`·`0x3d`·`0x3c`·`0x1b` 로만 부르고 `0x51` 은 없음 |
| `BamPpmPolicy` `+0x820` | `PspSetProcessPpmPolicy +0xa5` (1번 표) | `bam.sys` 가 함수표 경유로 부름 `[추측]` (run49 와 같음) |

- `Flags3` 덩어리 자체는 `nt` 가 이미 쓰는 줄 여럿이 있다 (`PspAllocateProcess` 비트 0·5·7·12·23·27·30, `PsMultiResumeThread +0x4e` 비트 15 — run49) 에 K3~P0 에서 **로깅 비트 4+1(31)** 이 더해진다. 비트 31 (`EnableProcessImpersonationLogging`) 은 이 요청서 30칸 밖이지만 같은 줄에서 켜진다.
- 사진 `Enable…Logging 비트 5개` (7.8) = `EnableWriteVmLogging` + `Flags3` 비트 4개 (입력 비트 1·2·3·4·5 가 켜진 값, `EnableReadVmLogging` 은 0) 와 모양이 맞다 `[추측: 값 대조는 Q79 에서]`.
- 가를 수 있는 것: **ExceptionPort = csrss (부모 쪽 CSR 호출이 시킴)**, **로깅 비트 = 드라이버 (`mssecflt.sys`)**, **Wake = 드라이버 (`bam.sys`)**. 부모 cmd 의 KERNELBASE 자신이 부르는 칸은 이 11칸에 **없다** (`CreateProcessInternalW` 가 `NtSetInformationProcess` 를 부르는 줄 자체가 없음 — 호출 목록 `+0x30e7` 이후에 `NtQueryInformationProcess (+0x41b4, 클래스 0x3d)` 뿐).

### 3. P0~P1 9칸 — 전부 **첫 스레드가 시작할 때** 커널이 직접 쓴다

경로 머리: 새 프로세스의 첫 스레드가 `PspUserThreadStartup` (P0) 에 들어간 뒤 `LdrInitializeThunk` (P1) 에 닿기 전. **syscall 아님** (커널이 스레드를 처음 돌리는 길).

| 칸 | 쓰는 줄 | 조건 · 모양 |
|---|---|---|
| `Flags2.PrimaryTokenFrozen` (15) | `PspUserThreadStartup +0x37 → PspDisablePrimaryTokenExchange +0x22` | 1번 표 |
| `Flags3.AddressPolicyFrozen` (14) · `SecurityFreezeComplete` (22) | `PspDisablePrimaryTokenExchange +0xbb / +0x111` · `+0xa4` | 1번 표 |
| `Flags.CreateReported` (0) · `Flags.ImageNotifyDone` (22) | **`DbgkCreateThread +0x85`** `lock cmpxchg [RDI+0x1f4]` 로 `OR 0x400001` — 호출 `PspUserThreadStartup +0x9c → PspNotifyThreadCreation +0x1c` (둘 다 `.asm` 있음) | **무조건** 켜고 (반복 루프), 옛 값을 받아 `ImageNotifyDone` 이 **0 이었으면** (`+0x96 BT 0x16`) 이미지 알림 (`PsIsImageNotifyEnabled` 가 켜져 있을 때 `PsCallImageNotifyRoutines` — exe, 이어서 `PsQuerySystemDllInfo` 0~5 의 시스템 DLL 들). 디버거 포트 (`+0x308`) 가 있으면 CreateProcess/모듈 메시지. 분리 길: `DbgkCreateMinimalProcess +0x30` 이 같은 값 `0x400001` 을 최소 프로세스에 씀 (test.exe 아님) |
| `Flags.LaunchPrefetched` (19) | **`PspNotifyThreadCreation +0xc1`** `lock bts [RBX+0x1f4],0x13` | 처음일 때만 (이미 켜져 있으면 건너뜀, `+0xca JC`) → 바로 `PfProcessCreateNotification` (호출 `+0xda`) |
| `ImagePathHash` `+0x384` | **`PfCalculateProcessHash +0x137`** `MOV [RBP+0x384],ECX` — 호출 `PfProcessCreateNotification +0x70`, 또는 `PfSnBeginAppLaunch +0xa6` | 전체 이미지 경로 (`PsGetAllocatedFullProcessImageName`) 를 대문자로 바꿔 (`RtlUpcaseUnicodeString`) 해시 (`×0x25`, 시작값 `0x4cb2f`). 이미 값이 있으면 (`+0x73`) 다시 안 만듦. 섹션 객체 (`+0x2a8`) 가 0 이면 안 함. **조건**: 파티션 쪽 프리페치 구조체 `+0x48` 비트 0 또는 전역 `0x140e67140` 비트 0 이 켜져 있으면 바로, 아니면 `PfSnBeginAppLaunch` 가 `PfSnCheckScenario` 를 통과해야 (`+0x8a`) — 프리페치·시나리오 설정에 달림 |
| `PrefetchTrace` `+0x390` (`_EX_FAST_REF`) | **`PfSnAddProcessTrace +0x42`** `MOV [RDI+0x390],RCX` (값 = 흔적 객체 + 0xf, 이미 있으면 거절 `+0xa` / 실행 중 보호 실패면 거절 `+0x31`) ← `PfSnActivateTrace +0x47` ← `PfSnBeginTrace +0x227` ← `PfSnBeginScenario +0x1b7` ← `PfSnBeginAppLaunch +0x254` `[objdump: 뒤 셋]` | 위 `ImagePathHash` 와 같은 길 `PfProcessCreateNotification +0x3e → PfSnBeginAppLaunch` 에서 **시나리오가 허락될 때만**. 읽는 쪽 `PfSnReferenceProcessTrace` (페이지 오류 기록 `PfSnLogPageFault +0x39`, `MiCompleteProtoPteFault +0x64b`), 지우는 쪽 `PfSnRemoveProcessTrace` |
| `ModifiedPageCount` `+0x550` | **`MiInsertPageInList +0x1fc`** `lock inc [프로세스+0x550]`, **`MiInsertPagesInList +0x64a`** `lock add … ,r12d` | 프로세스 = **지금 도는 스레드의 프로세스** (`GS:[0x188] → +0xb8`). 페이지를 목록에 넣을 때마다 (파티션 카운터 `+0x4ca0` / `+0x4bc0` 도 같이 오름) — **런타임 카운터**라 "한 번 쓰고 끝" 이 아님. 어느 목록 (변경 목록?) 인지는 `[추측]` |

- 한 번 더: `PspUserThreadStartup` 은 **스레드마다** 같은 길을 돈다. 칸을 처음 켜는 건 첫 스레드뿐이고 (`ImageNotifyDone`·`LaunchPrefetched`·`PrimaryTokenFrozen`·`SecurityFreezeComplete` 에 "이미 켜졌으면 건너뜀" 검사가 있음), 일꾼 스레드 (P1a~P1b) 때는 건너뛴다.
- 이미지 알림이 부르는 드라이버 콜백 (`PsCallImageNotifyRoutines` 안 간접 호출) 은 이름만 — 안 읽음.

### 4. P1~P1a 5칸 — test.exe 자신이 부른 syscall 안에서 처음 쓴다

모두 **자기 프로세스에 대해서만** 쓴다 (대상 핸들 `-1` 또는 `GS:[0x188]` 의 프로세스). 게으른 만들기 (처음 필요할 때 만들기).

| 칸 | syscall (정보 클래스) | 커널 쓰는 줄 | 만들기 · 조건 | ntdll 쪽 부르는 자리 (`LdrInitializeThunk` 지도 기준, 호출 자리 오프셋) |
|---|---|---|---|---|
| `Cookie` `+0x2b8` | `NtQueryInformationProcess` **`0x24`** (길이 4, 핸들 `-1` 만) | `NtQueryInformationProcess +0x2b42` `lock cmpxchg [RCX+0x2b8],R8D` (기대값 0) | 읽어서 0 이면 (`+0x1b8b`) `KeQuerySystemTimePrecise` + `KeQueryPerformanceCounter` + `ExGenRandom` + `RDTSC` + `PRCB` 칸들을 섞어 (`+0x2ad3 ~ +0x2b3b`) 한 번만 넣고, 진 쪽은 다시 읽음 (`+0x2b54`). 별도 도우미 함수 없음. 복제 길은 부모 값 복사 (`PspAllocateProcess +0x2183`, 안 탐). `ProcessCookie` 이름 = **`[추측]`** (PDB 열거형 없음). 번호 `0x24` 와 동작은 `[정적]` | `LdrpInitializeProcess` (호출 +0x9ed) → `RtlInitializeHeapManager` (호출 +0x9d) → `RtlpGetModifiedProcessCookie` (호출 +0x22, ret +0x27, `.c` 있음 — run46). 같은 클래스를 부르는 `RtlEncodePointer +0x43`·`RtlDecodePointer +0x4f` 도 있음 |
| `DeviceMap` `+0x318` (`_EX_FAST_REF`) | **이름 찾기 syscall 아무거나** (`NtOpenFile`/`NtCreateFile`/… → `IopCreateFile`·`ObOpenObjectByNameEx` → `ObpLookupObjectName`) — 이름이 **`\??\`** 로 시작할 때 (`ObpLookupObjectName +0x143d` 의 `0x3f005c` 비교) | `ObpSetCurrentProcessDeviceMap +0xf8` `XCHG [R15+0x318],RBX` (R15 = 현재 프로세스). 부르는 줄 둘: `ObpReferenceCurrentDeviceMap +0x286` ← `ObpLookupObjectName +0x145e`, 또는 `ObpLookupObjectName +0x1911` 직접 | 프로세스의 `DeviceMap` 이 비어 있으면 (`+0x1b3` 이하 갈래) 토큰의 장치 맵 (`SeGetTokenDeviceMap`, `+0x76`) 또는 서버 사일로 기본값으로 **그 자리에서 채움**. 만들기 길 (`ObInitProcess`) 에는 안 채움 → 사진 `K1~P1` 비어 있는 것과 맞음. **어느 syscall 이 첫 번째인지는 정적으로 못 가름** | `[공백]` — `\??\` 경로를 처음 여는 ntdll 호출 (P1 이후 `P1a` 앞) 은 이름을 못 정함 → 동적 F5 |
| `EtwDataSource` `+0x320` | `NtTraceControl` **함수 코드 `0x1b`** (입력 4바이트 = 이벤트 핸들) | `NtTraceControl +0x775` → `EtwpAddNotificationEvent +0x54` → **`EtwpAddDataSource +0x49`** `lock cmpxchg [RBX+0x320],RCX` (기대값 0, 풀 `0x28`바이트 새로 받음, 진 쪽은 풀 반납 `+0x5e`). 지우는 쪽 `EtwExitProcess +0x19` (← `PspProcessDelete +0x2b0`). 다른 호출자 `EtwpQueueNotification +0x32` (← `EtwpSendDataBlock +0x50`) | 현재 프로세스에 "알림 받는 쪽" 자리를 만들고 거기에 이벤트 객체 참조를 매단다 (`+0x68` 의 `CMPXCHG [RCX],RDI`) | `LdrpInitializeProcess` (호출 +0xc06 / +0xc23) → `EtwEventRegister` (호출 +0x1e) → `EtwNotificationRegister` (호출 +0x15c) → `EtwpRegisterProvider` (호출 +0xaf) → `EtwpRegisterTpNotificationOnce` (**호출 +0x8c, 함수 코드 `ECX=R9+0x1b`=`0x1b`, 길이 `R8D=4`**, `.asm` 있음). 이 함수 이름의 `Once` = 처음 한 번 |
| `InvertedFunctionTable` `+0x5c8` | `NtSetInformationProcess` **`0x35`** (핸들 `-1` 만, 길이 16) | `RtlInsertDynamicUserFunctionTable +0x20c` `MOV [RAX+0x5c8],RDI` (RAX = 프로세스; 풀 `0x10 + 24×N` 바이트, 헤더에 개수·지움 표시). 락 `+0x5d0` (`InvertedFunctionTableLock`). 지우는 쪽 `RtlRemoveDynamicUserFunctionTable`, 읽는 쪽 `RtlpLookupDynamicUserFunctionTable` (← `RtlpLookupUserFunctionTable +0x26`), 마지막 정리 `PspExitLastThread +0x105`. **PDB 칸 이름은 `InvertedFunctionTable`, 커널 함수 이름은 `…DynamicUserFunctionTable`** | **처음 한 번 함수표를 넣을 때** 만들어짐 (`+0xe1` 에서 비어 있는지 읽음) | `LdrpInitializeProcess` (호출 +0x119d / +0x126a) → `LdrpInitializeNtdllDataTableEntry` (호출 +0x11f) → `LdrpProcessMappedModule` (호출 +0x19e) → `RtlpInsertOrRemoveScpCfgFunctionTable` (호출 +0xc4) → **`RtlAddGrowableFunctionTable` (호출 +0x14f, `EDX = 0x10+0x25 = 0x35`)** → `NtSetInformationProcess`. ntdll 안 두 갈래 (`LdrControlFlowGuardEnforced` 검사 `+0x4e`·`+0xdf`) 에 따라 syscall 까지 가는지는 `[공백]` (test.exe 는 `MitigationFlags` 에 CFG 없음) → 동적 F7 |
| `WnfContext` `+0x5e8` | `NtSetWnfProcessNotificationEvent` (이벤트 핸들) | `NtSetWnfProcessNotificationEvent +0x33` 이 `[프로세스+0x5e8]` 이 0 이면 (`+0x3f`) `ExpWnfCreateProcessContext` (호출 `+0xa8`, ret `+0xad`) → **`ExpWnfCreateProcessContext +0x104`** `lock cmpxchg [RSI+0x5e8],RDI` (풀 `0x88`바이트, 전역 목록 `ExpWnfProcessesListHead` 에 매달고 락 `ExpWnfProcessesListLock`; 진 쪽은 `ExpWnfDeleteProcessContext` `+0x14c`). 다른 호출자 `ExpWnfResolveScopeInstance +0x46a` (프로세스 범위 WNF 상태 이름 쓸 때) | 이벤트 객체 참조를 문맥 `+0x80` 에 매담 (`+0x7e CMPXCHG`) | `LdrpInitializeProcess` (호출 +0xbe2) → `LdrpEnableUMGLTracingStateSync` (호출 +0x7a) → `RtlpSubscribeWnfStateChangeNotificationInternal` (호출 +0x189) → `RtlpInitializeWnf` (호출 +0x98) → `RtlpWnfRegisterTpNotification` (**호출 +0x77, ret +0x7c**) → `NtSetWnfProcessNotificationEvent`. 이 길이 `RtlpSubscribeWnfStateChangeNotificationInternal` 의 `RunOnce` 안 → 처음 한 번 |

- 5칸 모두 "syscall 안 · 자기 프로세스 · 처음 필요할 때". `DeviceMap` 만 "어느 syscall" 이 정해지지 않는다.
- run46 이 `[추측]` 으로 적은 `ProcessCookie` 는 **이름만 여전히 추측, 번호·동작은 확정**.

### 5. P1a~P1b — `Win32Process` 와 `OwnerProcessId` 둘째 쓰기

**`Win32Process` `+0x298`**: 쓰는 줄은 `PsSetProcessWin32Process` 안 둘 — 켜기 `+0x86` (`MOV [RBX+0x298],RBP`, 조건: 새 값 ≠ 0, 현재 값 = 0, `Flags.ProcessDelete` (비트 3) 꺼져 있음; 이어서 `+0x8d ~ +0xa7` 은 `ProcessTimerDelay` (`+0x6f0`) 값이 0 이 아니면 풀 'TiDe' 를 받아 작업 항목 (`ExQueueWorkItem`) 을 큐에 넣음 — 조각 `FUN_140b3c930`, 안쪽은 안 읽음), 지우기 `+0x43` (옛 값 = 인자와 같을 때만). 모두 프로세스 락 안.
- **이 함수를 부르는 커널 줄은 없다** (공개 함수, `_refs` 에 코드 참조 0). 부르는 곳은 win32k 쪽: 공개 심볼로 찾은 호출 자리 (가장 가까운 앞쪽 공개 함수 이름, `[objdump + PDB 공개 이름]`):
  - `win32kbase.sys` `AllocateW32ProcessImpl +0xe0` (**켜는 쪽**), `UserDeleteW32Process +0xbd` (지움)
  - `win32kfull.sys` `DereferenceW32ProcessEx +0x54` (지움)
  - `win32k.sys` `W32CalloutDispatchThunk +0x292` (콜아웃 입구)
- 커널이 win32k 를 부르는 길: `PsConvertToGuiThread +0x1e0 / +0x238` (`PsSessionGetWin32Callouts` → `ExCallCallBack`, 프로세스 콜아웃·스레드 콜아웃) ← `KiConvertToGuiThread +0x4`·`PspEnsureGuiThreadAndBatchFlush +0x1e` `[objdump]`. 즉 **프로세스가 처음 win32k 시스템 콜을 부를 때** 변환 길에서 콜아웃이 돈다고 읽힌다 `[추측]`. test.exe 는 user32 를 안 올리므로 (모듈 4개) 누가 첫 win32k 호출을 일으키는지는 정적으로 못 가름 (후보: KERNELBASE 시작 중 `CsrClientConnectToServer` (`_KernelBaseBaseDllInitialize` 호출 +0x2d2) 뒤 csrss 가 대신 win32k 를 부르는 경우) `[공백]` → 동적 F9.

**`OwnerProcessId` 둘째 쓰기** — **옛 Q65 의 `NtSetInformationProcess +0x1ecd` = 이 쓰기: 예.**
- 커널: `NtSetInformationProcess` **클래스 `0x31`** (`+0x1a32`, 길이 8 아니면 `0xc0000004`) → `+0x1a3c JMP +0x1e94` → 핸들이 `-1` (현재 프로세스) 아니면 거절 (`+0x1e94 ~ +0x1e98`) → 값 = 입력 8바이트 (`+0x1e9e`) → **하위 2비트가 `01` 이어야** (`+0x1eb0 ~ +0x1eb7`, 아니면 `0xc000000d`) → **`+0x1ecd MOV [RCX+0x2d8],RDX`** (RCX = `GS:[0x188]→+0xb8` 현재 프로세스). 권한 검사 없음. 읽는 쪽: `NtQueryInformationProcess` 클래스 `0x31` (`+0x2cd0 ~ +0x2d1c`, 접근 `0x1000`) 이 이 칸을 그대로 돌려줌 → **`OwnerProcessId` 는 "콘솔 호스트 프로세스" 표시이기도 하다.** 또 하나의 읽는 쪽 `PspProcessClose +0x22 ~ +0x30` (닫는 쪽 PID 와 같을 때 + `Flags` 비트 3·30 이면 `PsTerminateProcess(…, 0xc000010a)`) — 하위 비트 태그 (`\|1` 또는 `\|2`) 가 붙으면 PID (4의 배수) 와 절대 같지 않아 이 검사가 안 걸린다.
- 부르는 쪽 `[정적]`: **`KERNELBASE!ConsoleCommitState +0xe9`** (ret `+0xf0`, `.asm` 있음) — `NtDeviceIoControlFile(…, 0x500023, …)` (호출 +0x8a, 출력 8바이트) 으로 받은 값에 **`\| 1`** (`+0xd8`) 을 붙여 `NtSetInformationProcess(-1, 0x31 (= 8+0x29), &값, 8)`. `ConsoleCommitState` ← **`ConsoleInitialize +0x1ca`** ← `_KernelBaseBaseDllInitialize +0x521` (ret +0x526, 이 함수 `_calls.tsv` 에 있음). 다른 호출자: `AttachConsole +0x89`, `AllocConsoleWithOptions +0xa6`, `FreeConsole +0x4a` `[objdump]`. 즉 KERNELBASE DLL 초기화 (P1a~P1b).
- **값 `0x729` 의 뜻** `[추측]`: `0x729 = 0x728 \| 1` → IOCTL 이 돌려준 `0x728` = 콘솔 호스트 프로세스 ID (conhost.exe) + 태그 1. `0x728` 이 정말 conhost PID 인지는 동적 (F10, 프로세스 목록).
- **같은 함수가 매개변수 `ConsoleHandle` 도 쓴다** (Q78f 몫인데 한 줄로 이어 줌): `ConsoleCommitState +0x3c ~ +0x40` `[ProcessParameters+0x10] = [상태+8]` (RDI = `PEB→+0x20` 프로세스 매개변수; `+0x10` = `ConsoleHandle`). 4 → `0x60` 과 `0x19a8` → `0x729` 는 **같은 함수의 같은 호출** 에서 나온다. (`ConsoleHandle` 자체 값 `0x60` 은 위 IOCTL/연결 결과.)
- 이 칸에서 "옛 Q65" 의 남은 의문 하나가 닫힘: `PspAllocateProcess +0x710` 의 `\|2` 와 이 쓰기의 `\|1` 은 **같은 칸의 두 태그** (생성자 PID vs 콘솔 호스트 PID).

### 6. 옛 Q65 남은 것

1. `NtSetInformationProcess +0x1ecd`: **위 5번**. 클래스 `0x31`, 현재 프로세스만, 값 하위 2비트 = 1, 부르는 쪽 `KERNELBASE!ConsoleCommitState`.
2. `PspAllocateProcess +0x710` 부모 PID `\|2` (옛 Q65 앞 반쪽): 확인만 — `RDX = [현재 프로세스+0x1d0]` (호출한 프로세스의 PID), `OR 2`, 단 인자 `R8D` 비트 9 가 켜져 있으면 (`+0x707 BT R8D,9`) `\|2` 를 안 붙이고 (`CMOVC`) `+0x710` 이 저장. 비트 9 = `CREATE_SUSPENDED` 없을 때 KERNELBASE 가 켜는 ProcessFlags (sast/29) → cmd 가 만든 test.exe 는 `\|2` 가 **안 붙은** `0x19a8` ✓ 사진 값과 맞음.
3. `ObCloseHandleTableEntry +0x7a9` 간접 호출 앞 인자 채움: **짐에 영향 없음 → 짧게.** `+0x7a9` 은 "닫기 함수" 호출 한 곳 (`CALL _guard_dispatch_icall`, `RAX` = 개체 형식의 `CloseProcedure`) 이고 `+0x631 JNZ` 로만 들어오는 조각이다. 같은 자리 바로 앞 (`+0x61d ~ +0x62e`) 에서 `CloseProcedure == IopCloseFile` 이면 직접 호출 (`+0x637`), 아니면 이 간접. 인자: `RCX = R10` (= `[RSP+0x40]`, 핸들 표 주인 프로세스, `+0x302 KeStackAttachProcess` 로 붙인 대상), `RDX = RDI+0x30` (개체 본체), `R8D = ESI` (+0x31a, 접근 권한), `R9 = RBX` (핸들 개수, `+0x351 CMP RBX,1` 로도 쓰임). **다섯째 인자는 이 줄 앞에서 안 채운다.** `PspProcessClose` 는 `RCX`(닫는 프로세스, `[RCX+0x1d0]` PID 비교)·`RDX`(닫히는 프로세스, `[RDX+0x2d8]`·`[RDX+0x5fc]`·`[RDX+0x380]`)·`R9`(`CMP R9,1`) 만 읽는다 → 이름은 `(Process, Object, GrantedAccess, ProcessHandleCount, SystemHandleCount)` `[추측: ntddk/WRK 의 `OB_CLOSE_METHOD` 모양과 일치]`.

### 7. 줄기와 경로 (3.11 양식) — 칸마다 쓰는 곳

경로 약어: `①I…` = `NtCreateUserProcess → PspInsertProcess`; `①T…` = 새 프로세스 첫 스레드 `PspUserThreadStartup`; `NSIP` = `NtSetInformationProcess`; `NQIP` = `NtQueryInformationProcess`; `LIP` = `LdrpInitializeProcess`.

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x1f4` `Flags` · `.ProcessInserted` (26) | ① | 켜기 (`lock or 0x4000000`) — 핸들 표에 넣은 뒤. 작업이 있으면 `PspValidateJobAffinityState +0x33` 이 같은 비트 | 직접 | nt | `①I… +0x202` | `[정적]` sast45 · 정확 · 사진 `K2~K3` · Q78c2b 로 닫힘 |
| `+0x5d8` `ActiveThreadsHighWatermark` | ① | `ActiveThreads` 가 더 크면 따라 올림 (첫 스레드 1). 스레드를 달 때마다 | 직접 | nt | `①… → PspInsertThread +0x678` | `[정적]` sast45 · 정확 · 사진 `K2~K3` (뒤 구간 오름 `P1a~P1b`) |
| `+0x7dc` `Flags4` · `.ThreadWasActive` (0) | ① | `bts` 로 켬, 옛 값 0 + `ActiveThreads == 1` 이면 "첫 활성 스레드" | 직접 | nt | `①… → PspInsertThread +0x67f` (판정 `+0x696 ~ +0x792`) | `[정적]` sast45 · 짐작→정확 · 사진 `K2~K3` |
| `+0x240` `ExceptionPortData` · `ExceptionPortValue` · `ExceptionPortState` (0:3) | ① | LPC 포트 객체 주소 `\|` 상태 3비트 (csrss `CsrApiPort`) | syscall (다른 프로세스: csrss) | nt (부르는 쪽 csrss) | `CsrpCreateProcess +0x34d → CsrSetExceptionPort (+0x4f / +0x7b) → NSIP (정보 클래스 8) +0x1128` | `[정적]` sast45 · 정확 · 사진 `K3~P0` |
| `+0x1f0` `Flags2` · `.EnableWriteVmLogging` (25) | ① | 로깅 입력 비트 1 → 켬 (비트 24 `EnableReadVmLogging` 은 입력 비트 0) | syscall (다른 드라이버: `mssecflt.sys` `[추측]`) | nt | `NSIP (정보 클래스 0x60 / 0x57) +0x20f (지움) · +0x23d (켬)` | `[정적]` sast45 · 정확 (칸) / 부르는 쪽 `[추측]` · 사진 `K3~P0` |
| `+0x5fc` `Flags3` (DWORD) · `.EnableProcessSuspendResumeLogging` (19) · `.EnableThreadSuspendResumeLogging` (20) · `.EnableProcessRemoteExecProtectVmLogging` (28) · `.EnableProcessLocalExecProtectVmLogging` (29) | ① | 먼저 비트 19·20·28·29·31 지움, 입력 비트 2·3·5·4 로 다시 켬 (31 은 입력 6) | syscall (다른 드라이버: `mssecflt.sys` `[추측]`) | nt | `NSIP (정보 클래스 0x60) +0x19f (지움) · +0x203 (켬)` | `[정적]` sast45 · 정확 · 사진 `K3~P0` |
| `+0x720` `WakeChannel` · `WakeInfo` (48B) | ① | 알림 채널 이름을 새로 만들어 `+0x720` 에, 한 칸 `+0x744` 에, 나머지는 0 | syscall (다른 드라이버: `bam.sys` `[추측]`; 게으른 만들기) | nt | `NQIP (정보 클래스 0x51) +0xd0d → PspAllocateAndQueryProcessNotificationChannel +0x11d` | `[정적]` sast45 · 정확 (칸) / 부르는 쪽 `[추측]` · 사진 `K3~P0` |
| `+0x820` `BamPpmPolicy` | ① | 둘째 인자 값 (조건: 기능 플래그 둘 켜짐) | 직접 (nt 함수, 부르는 쪽 `bam.sys` `[추측]`) | nt | `PspSetProcessPpmPolicy +0xa5` | `[정적]` sast45 · run49 · 정확 · 사진 `K3~P0` |
| `+0x1f0` `Flags2` · `.PrimaryTokenFrozen` (15) | ① | 켜기. 첫 스레드가 켠 뒤 둘째 스레드 길은 건너뜀 | 직접 | nt | `①T… → PspDisablePrimaryTokenExchange +0x22`; 둘째 길 `PspInsertThread +0x77b` (`ActiveThreads == 2`) | `[정적]` sast45 · run49 · 정확 · 사진 `P0~P1` |
| `+0x5fc` `Flags3` · `.AddressPolicyFrozen` (14) · `.SecurityFreezeComplete` (22) | ① | 켜기 (비트 14 → 22 순서, KVA 그림자 켜짐이면 주소 정책 동기화 뒤) | 직접 | nt | `①T… → PspDisablePrimaryTokenExchange +0xbb · +0x111 (14) · +0xa4 / +0xc4 (22)` | `[정적]` sast45 · 정확 · 사진 `P0~P1` |
| `+0x1f4` `Flags` · `.CreateReported` (0) · `.ImageNotifyDone` (22) | ① | 둘 다 한 번에 켬 (`OR 0x400001`), 옛 `ImageNotifyDone`=0 이면 이미지 알림 뿌림 | 직접 | nt | `①T… +0x9c → PspNotifyThreadCreation +0x1c → DbgkCreateThread +0x85` | `[정적]` sast45 · 정확 · 사진 `P0~P1` |
| `+0x1f4` `Flags` · `.LaunchPrefetched` (19) | ① | `bts` 로 켬 (처음이면 프리페치 알림) | 직접 | nt | `①T… +0x9c → PspNotifyThreadCreation +0xc1` | `[정적]` sast45 · 정확 · 사진 `P0~P1` |
| `+0x384` `ImagePathHash` | ① | 대문자 전체 경로의 해시 (`×0x25`, 시작 `0x4cb2f`) | 직접 (조건부: 프리페치 설정) | nt | `①T… → PspNotifyThreadCreation +0xda → PfProcessCreateNotification +0x70 → PfCalculateProcessHash +0x137` | `[정적]` sast45 · 정확 · 사진 `P0~P1` |
| `+0x390` `PrefetchTrace` (`_EX_FAST_REF`) | ① | 프리페치 흔적 객체 포인터 (+0xf) | 직접 (조건부: 시나리오 허락) | nt | `…PfProcessCreateNotification +0x3e → PfSnBeginAppLaunch +0x254 → PfSnBeginScenario +0x1b7 → PfSnBeginTrace +0x227 → PfSnActivateTrace +0x47 → PfSnAddProcessTrace +0x42` | `[정적]` sast45 · 정확 (뒤 셋 `.asm` 없음) · 사진 `P0~P1` |
| `+0x550` `ModifiedPageCount` | ① | 현재 스레드 프로세스의 카운터를 올림 (런타임) | 직접 (nt 메모리 관리) | nt | `MiInsertPageInList +0x1fc` · `MiInsertPagesInList +0x64a` (페이지 넣을 때마다) | `[정적]` sast45 · 정확 (줄) / 의미 `[추측]` · 사진 `P0~P1` |
| `+0x2b8` `Cookie` | ② | 난수 4바이트 (한 번만) | syscall | nt | `NQIP (정보 클래스 0x24) +0x2b42` ← ntdll `LIP (+0x9ed) → RtlInitializeHeapManager (+0x9d) → RtlpGetModifiedProcessCookie (+0x22)` | `[정적]` sast45 · run46 · 정확 (줄) / 이름 `[추측]` · 사진 `P1~P1a` |
| `+0x318` `DeviceMap` (`_EX_FAST_REF`) | ② | 프로세스 장치 맵 (토큰의 장치 맵 또는 사일로 기본) | syscall (게으른) | nt | `이름 찾기 syscall (\??\) → ObpLookupObjectName +0x145e → ObpReferenceCurrentDeviceMap +0x286 → ObpSetCurrentProcessDeviceMap +0xf8` (또는 `ObpLookupObjectName +0x1911`) | `[정적]` sast45 · 정확 (nt 쪽) / ntdll 호출자 `[공백]` · 사진 `P1~P1a` |
| `+0x320` `EtwDataSource` | ② | 풀 0x28 바이트 알림 자리 (`'EtwS'`) | syscall | nt | `NtTraceControl (함수 코드 0x1b) +0x775 → EtwpAddNotificationEvent +0x54 → EtwpAddDataSource +0x49` ← ntdll `LIP (+0xc06/+0xc23) → EtwEventRegister (+0x1e) → EtwNotificationRegister (+0x15c) → EtwpRegisterProvider (+0xaf) → EtwpRegisterTpNotificationOnce (+0x8c)` | `[정적]` sast45 · 정확 · 사진 `P1~P1a` |
| `+0x5c8` `InvertedFunctionTable` | ② | 풀 `0x10 + 24×N` 바이트 동적 함수표 (처음 한 번) | syscall | nt | `NSIP (정보 클래스 0x35) → RtlInsertDynamicUserFunctionTable +0x20c` ← ntdll `LIP (+0x119d/+0x126a) → LdrpInitializeNtdllDataTableEntry (+0x11f) → LdrpProcessMappedModule (+0x19e) → RtlpInsertOrRemoveScpCfgFunctionTable (+0xc4) → RtlAddGrowableFunctionTable (+0x14f)` | `[정적]` sast45 · 정확 (nt 쪽) / ntdll 안 갈래 `[공백]` · 사진 `P1~P1a` |
| `+0x5e8` `WnfContext` | ② | 풀 0x88 바이트 WNF 프로세스 문맥, 전역 목록에 달림 | syscall | nt | `NtSetWnfProcessNotificationEvent +0xa8 → ExpWnfCreateProcessContext +0x104` ← ntdll `LIP (+0xbe2) → LdrpEnableUMGLTracingStateSync (+0x7a) → RtlpSubscribeWnfStateChangeNotificationInternal (+0x189) → RtlpInitializeWnf (+0x98) → RtlpWnfRegisterTpNotification (+0x77)` | `[정적]` sast45 · 정확 · 사진 `P1~P1a` |
| `+0x298` `Win32Process` | ② | win32k 의 프로세스 구조 포인터 | 직접 (nt 공개 함수 `PsSetProcessWin32Process`, 부르는 쪽 win32k) | nt (부르는 쪽 `win32kbase.sys` `AllocateW32ProcessImpl +0xe0`) | `PsConvertToGuiThread +0x1e0 (콜아웃) → … → PsSetProcessWin32Process +0x86` `[추측: 중간 칸]` | `[정적]` sast45 · 정확 (nt 쪽) / 트리거 `[공백]` · 사진 `P1a~P1b` |
| `+0x2d8` `OwnerProcessId` (둘째 쓰기) | ② | 콘솔 호스트 프로세스 ID `\| 1` (`0x729`) | syscall | nt (부르는 쪽 KERNELBASE) | `NSIP (정보 클래스 0x31) +0x1ecd` ← `KERNELBASE!_KernelBaseBaseDllInitialize (+0x521) → ConsoleInitialize (+0x1ca) → ConsoleCommitState (+0xe9)` | `[정적]` sast45 · 정확 · 사진 `P1a~P1b` (`0x19a8` → `0x729`) |
| `+0x6a8` `SharedCommitCharge` | ? | `lock add` — 줄 후보는 아래 | ? | nt `[추측]` | `[공백]` | `[공백]` 줄 후보 · 사진 `K1 이전` → 바뀜 |

#### 칸 하나 더: `SharedCommitCharge` (K1 이전 1칸) — **못 닫음, 후보만 좁힘**

- 쓰는 줄은 `MiInsertSharedCommitNode +0x33e` (`lock add [R13+0x6a8],R8`) 하나 + 지우는 `MiRemoveSharedCommitNode +0x173` (sast/44).
- 부르는 곳을 커널 전체에서 다시 훑음 `[objdump]`: **`MiMapViewOfImageSection +0x4e9`**, **`MiMapViewOfDataSection +0x3df`**, `MiAllocateSplitVads +0xf9`, `MiAllocateChildVads +0x249` (복제), `MmLinkJobProcess +0x195` (작업), `MiSectionOpen +0x18` (← `ObpIncrementHandleCountEx +0x77e` ← `ObpCreateHandle`/`ObDuplicateObject`/`ObInheritObjectHandle +0x74`).
- **새로 보인 것**: 이미지 섹션 보기를 붙이는 `MiMapViewOfImageSection +0x4e9` 가 후보에 있다. 새 프로세스는 `MmInitializeProcessAddressSpace → MiMapProcessExecutable` (`+0xb3 MmMapViewOfSectionEx`, `+0xd7 PsMapSystemDlls`) 에서 exe 와 ntdll 을 붙이므로 (`①M…`, K1 앞), K1 `0x2b` 은 **여기서 올랐을 가능성이 가장 높다** `[추측]`. 어느 줄인지 (그리고 `0x2b` 이 exe 몫인지 ntdll 몫인지) 는 줄 수가 아닌 인자 값이라 정적으로 못 가름 → F12.

## 동적분석으로 확인해야 할 것

"사진" = 이미 있는 덤프 (`ms/logs/04_baggage-read_*.txt`, `ms/dump/03_baggage-snap/`) 로 답할 수 있는지. 번호는 이 조각 안 번호 (F1~F12). 모든 멈춤점은 `nt` 기준 rva 는 `함수 rva + 오프셋`. 커널 멈춤점은 모든 프로세스에 걸리니 조건을 붙인다.

| # | 질문 | 추천 멈춤점 / 찍을 것 | 사진으로? |
|---|---|---|---|
| F1 | `ExceptionPortData` 를 쓰는 주체가 csrss 이고 값이 `CsrApiPort` 객체인가 | `nt!NtSetInformationProcess+0x1128` (rva `0x933ad8`, `lock cmpxchg [rcx+0x240],rdx`). 조건: **대상 EPROCESS `rcx` 의 `ImageFileName` = test.exe**. 찍을 것: `rdx` (포트 주소 `\|` 상태), 현재 프로세스 이미지 (csrss.exe 여야), `k`. 추가로 `csrsrv!CsrSetExceptionPort+0x4f / +0x7b` (rva `0x5f5f`·`0x5f8b`) 에서 `rdx` (클래스 8), `r9` (길이 8/16) | 값은 가능성 있음: 사진 P0 의 `+0x240` 값 & `!object` — 포트 주소가 csrss 의 `CsrApiPort` 인지는 새 요청 필요 (Q79 읽기 추가) |
| F2 | 로깅 비트를 쓰는 주체가 `mssecflt.sys` 인가, 입력 값은 | `nt!NtSetInformationProcess+0x203` (rva `0x932bb3`, `ecx` = 새 `Flags3` 비트, `[rsp+0x90]` = 입력 비트). 조건: 대상 `[rsp+0x40]` 의 `ImageFileName` = test.exe. 찍을 것: `k` (콜스택에 `mssecflt` 가 있는지), 입력 값. 대안: `bp mssecflt+0xbef6` (모듈 로드 뒤) 에서 `rcx` (핸들), `r8` (값) | 불가 (호출 순간). 결과 값은 사진 `K3~P0` `Flags3` 와 대조 가능 |
| F3 | `WakeChannel` 을 처음 만드는 주체 (`bam.sys` 가 맞나) | `nt!PspAllocateAndQueryProcessNotificationChannel+0x11d` (rva `0x8d368d`, `mov [rsi],rax`) 조건: `rbp` (대상 EPROCESS) 의 이미지 = test.exe. 찍을 것: `k`. 대안 `bp bam+0x1621b` | 불가 |
| F4 | `Cookie` 의 값과 부르는 호출자 (확인용, 급하지 않음) | `nt!NtQueryInformationProcess+0x2b42` (rva `0x9bde22`, `r8d` = 새 쿠키, `rcx` = EPROCESS) 조건: 현재 프로세스 = test.exe. 값은 이후 ntdll `RtlpGetModifiedProcessCookie+0x27` 반환 `eax` 와 같아야 | 불가 (값은 힙 구간 사진에 섞여 못 봄, run46 D5) |
| F5 | `DeviceMap` 을 처음 채우는 syscall 과 ntdll 호출자 | `nt!ObpSetCurrentProcessDeviceMap+0xf8` (rva `0x96bad4`, `xchg [r15+0x318],rbx`) 조건: `r15` 의 이미지 = test.exe. 찍을 것: `k` (어느 `Nt…` 가 이름을 찾았나, 그 경로 `NtOpenFile`? 의 ntdll 호출자) | 불가 |
| F6 | `EtwDataSource` (좋은 확인, 급하지 않음) | `nt!EtwpAddDataSource+0x49` (rva `0x882cf1`) 조건: 현재 프로세스 = test.exe. `k` 로 `NtTraceControl` 갈래 확인 + ntdll `EtwpRegisterTpNotificationOnce+0x8c` (함수 코드 `0x1b`) | 불가 |
| F7 | `InvertedFunctionTable` — test.exe 가 정말 `NtSetInformationProcess(0x35)` 를 부르나 (CFG 없는 exe) | `nt!RtlInsertDynamicUserFunctionTable+0x20c` (rva `0x451310`) 조건: 현재 프로세스 = test.exe. 안 걸리면 칸 값이 다른 경로에서 온 것 (정적 재조사). `k` → `RtlAddGrowableFunctionTable+0x14f` 인지 | 가능성 있음: 사진 `P1~P1a` 에 값이 있으니 풀 크기·내용 (`dq <값> L6`) 으로 함수 개수 (`+0`)·종류 확인 가능 → Q79 |
| F8 | `WnfContext` (확인용) | `nt!ExpWnfCreateProcessContext+0x104` (rva `0xa50f28`) 조건: 현재 프로세스 = test.exe. `k` → `NtSetWnfProcessNotificationEvent+0xa8` ← ntdll `RtlpWnfRegisterTpNotification+0x77` | 불가 |
| F9 | `Win32Process` 를 처음 켜는 주체와 트리거 | `nt!PsSetProcessWin32Process+0x86` (rva `0xa62d56`) 조건: `rbx` (대상 EPROCESS) 이미지 = test.exe. 찍을 것: 현재 프로세스 (test.exe 자신이냐 csrss 냐), `k` (`win32kbase!AllocateW32ProcessImpl`, 그 위 syscall 이름) | 불가 (포인터 형식은 사진으로 가능: `dq` 값 vs win32k 풀) |
| F10 | `OwnerProcessId` `0x729` 의 뜻 (conhost PID?), `ConsoleHandle` `0x60` | `nt!NtSetInformationProcess+0x1ecd` (rva `0x93487d`) 조건: 현재 프로세스 = test.exe. 찍을 것: `rdx` (= `0x729`), `k` (`KERNELBASE!ConsoleCommitState+0xe9`). 이어서 `!process 728 0` (또는 프로세스 목록) 에서 PID `0x728` 의 이미지 이름 (conhost.exe?). `kernelbase!ConsoleCommitState+0x91` 직후 `[rsp+0x70]` (IOCTL 출력) | **가능**: 사진 P1b (또는 P2) 시점의 프로세스 목록에 PID `0x728` 이 있는지, 이미지 이름이 conhost.exe 인지 — Q79 읽기 추가 |
| F11 | `ImagePathHash` · `PrefetchTrace` · `LaunchPrefetched` 가 이 VM 에서 정말 켜지는가 (프리페처 설정) | `nt!PfCalculateProcessHash+0x137` (rva `0x945db7`, `ecx` = 해시), `nt!PfSnAddProcessTrace+0x42` (rva `0x402af2`), `nt!PspNotifyThreadCreation+0xc1` (rva `0x9110d9`). 조건: 현재 스레드 프로세스 = test.exe (`PspUserThreadStartup` 안). 안 걸리는 줄이 있으면 그 칸은 "쓰는 줄은 있으나 이 설정에선 안 탐" | 불가. 단 사진 P0~P1 값이 있으니 `ImagePathHash` 값 vs 직접 계산 (대문자 전체 경로 해시) 대조는 정적으로 가능 (새 요청 없이) |
| F12 | `SharedCommitCharge` K1 `0x2b` 을 올린 줄 (E5 와 같음) | `nt!MiInsertSharedCommitNode+0x33e` (rva `0x9097be`, `lock add [r13+0x6a8],r8`). 조건: `r13` = 새 프로세스 EPROCESS (K1 앞, `PspAllocateProcess` 안). 찍을 것: `r8` (양), `k` (`MiMapViewOfImageSection+0x4e9` 가 exe/ntdll 어느 쪽인지) | 불가. 낮은 우선순위 |
| F13 | `ModifiedPageCount` 가 P0~P1 사이에 처음 오르는 줄 | `ba w4 <EPROCESS>+0x550` (EPROCESS 주소는 `PspAllocateProcess` 뒤), 또는 `nt!MiInsertPageInList+0x1fc` (rva `0x32412c`) 조건: `rcx` = test.exe. `k` | 불가. 낮은 우선순위 |

- 급한 것은 없다. F2·F3·F9 은 "정말 그 주체인가" 를 보는 것이고, 나머지는 칸 표를 닫는 데 꼭 필요하진 않다.
- 디버거가 붙어서 달라지는 점: 없음 (모든 근거가 정적). 단 `bam.sys`·`mssecflt.sys`·win32k 호출은 이 VM 에서 실제로 불리는지에 달렸다 (드라이버 로딩·기능 플래그).

## 정정 · 새로 알게 된 것 (flow-writer 용)

1. **`OwnerProcessId` 의 뜻이 두 겹**: ① 만들 때 = **호출한 프로세스의 PID** (`\|2`, 비트 9 이면 `\|2` 없음) — "부모 PID" 가 아니다 (PROC_THREAD_ATTRIBUTE_PARENT_PROCESS 로 부모를 바꿔도 호출한 쪽). ② 이후 = **콘솔 호스트 프로세스 ID `\|1`** (정보 클래스 `0x31`, KERNELBASE `ConsoleCommitState`). 3.11 `+0x2d8` 줄 "무엇을" 에 둘째 쓰기를 더하고, 7.8 "`OwnerProcessId` `0x19a8` → `0x729`" 는 닫음 (부르는 쪽 `KERNELBASE!ConsoleCommitState +0xe9`).
2. **`ConsoleHandle` 4 → `0x60`** (7.8, 매개변수) 도 같은 `ConsoleCommitState` (`+0x3c ~ +0x40`) 에서 쓴다 — 3.2 매개변수 `ConsoleHandle` 줄의 "쓰는 쪽" 을 이 함수로. (`ConsoleCommitState` 의 호출자 사슬: `_KernelBaseBaseDllInitialize +0x521 → ConsoleInitialize +0x1ca → ConsoleCommitState`.)
3. **`Cookie` 복제 길**: `PspAllocateProcess +0x2183` 이 부모 값을 복사 (섹션 없음, 안 탐). sast/44 의 "조건부 안 탐" 목록에 `Cookie` 추가.
4. **K3~P0 는 csrss + 드라이버 몫**: `ExceptionPort*` = csrss (`CsrSetExceptionPort`), 로깅 비트 = `mssecflt.sys` (rva `0xbef6`), `WakeChannel` = `bam.sys` (rva `0x1621b`) `[추측]`, `BamPpmPolicy` = bam 함수표 `[추측]`. 7.8 "부모 쪽이 ⓪ 전에 자식에 쓰는 칸" 의 "다른 DLL 일 수 있는 첫 후보" 를 이름 붙여 갈랐다 — 부모 KERNELBASE 자신이 쓰는 칸은 없음.
5. **`PrimaryTokenFrozen`**: 첫 스레드 시작 (`PspDisablePrimaryTokenExchange +0x22`) 이 켜고, `PspInsertThread +0x77b` 는 "둘째 스레드를 달 때" 의 안전 줄 (test.exe 에선 안 탐). `AddressPolicyFrozen`·`SecurityFreezeComplete` 도 같은 함수.
6. **`CreateReported`·`ImageNotifyDone` = `DbgkCreateThread +0x85` 한 줄이 같이 켠다** (`OR 0x400001`). `LaunchPrefetched` 는 `PspNotifyThreadCreation +0xc1`. 3.11 `Flags` 비트 표에 줄 추가.
7. **`InvertedFunctionTable` = 동적 함수표** (커널 이름 `DynamicUserFunctionTable`, 클래스 `0x35`). ntdll 쪽은 `RtlAddGrowableFunctionTable` (SCP CFG 함수표).
8. **클래스 이름은 전부 `[추측]`** — ntoskrnl PDB 에 `PROCESSINFOCLASS` 가 없다. run46 의 "`ProcessCookie` `[추측]`" 은 번호·동작은 확정, 이름만 추측으로 유지.
9. **SharedCommitCharge 후보에 이미지 섹션 붙이기가 추가** (`MiMapViewOfImageSection +0x4e9`) — 3.11 `K1 이전` 마지막 칸의 줄 후보 (7.5) 를 갱신.
10. **새 짐 후보 (칸 표 없음, 7.6 에 올릴지 사용자와)**: `ExpWnf` 프로세스 문맥 (`_WNF_PROCESS_CONTEXT`, 0x88B), ETW 데이터 소스 (0x28B), 동적 함수표 (`0x10 + 24×N`), `_PS_PROCESS_WAKE_INFORMATION` (0x30B, `_PS_PROCESS_WAKE_INFORMATION` 은 PDB 에 있음), `ExceptionPort` 가 가리키는 LPC 포트 (csrss 의 `CsrApiPort`).

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **고칠 버그는 없다.** `run_ghidra.sh export`(ntoskrnl·ntdll·kernelbase·csrsrv), `refs`, `pdb_struct.py` 모두 문제없이 돌았다. 약 14초 (ntoskrnl export).
- **제안 1 (sast/43·44 와 같은 것, 이번에 세 번째)**: "이 오프셋을 쓰는 줄이 커널 어디 있나" + "이 함수를 부르는 곳이 어디 있나" 를 한 번에 주는 스크립트. 이번에도 `objdump -d` 한 번 + `_index.tsv` 로 함수 이름을 붙이는 임시 스크립트 (`scratchpad` 에만 있음: `scanw.py` 쓰기 훑기 · `callers.py` 함수 호출자 · `scanany.py` 읽기·lea 포함) 를 짰다. `_calls.tsv` 는 **뽑은 함수의 호출만** 있어 "누가 이 함수를 부르나" 를 모르고, `refs` 는 주소 하나씩이다. 제안: `scripts/field_writers.py <바이너리> --off 0x5e8 [--type _EPROCESS]` 와 `scripts/callers.py <바이너리> <함수>` (둘 다 `objdump` + `_index.tsv`).
- **제안 2**: `pdb_struct.py` 에 열거형 (`LF_ENUM`) 지원 — `--list` 가 구조체만 보여서 `PROCESSINFOCLASS` 가 PDB 에 없다는 확인에 `llvm-pdbutil dump -types` 를 직접 써야 했다 (이번엔 없다는 것까지 확인).
- **제안 3**: 사용자 모드 호출자 찾기 (이번 5번·4번에 많이 씀) 를 위해 `imports_callers.py <바이너리> <import 이름>` — IAT 슬롯에서 `call [rip+x]` 호출 자리 + 앞쪽 `edx` 를 붙여 주는 것 (이번엔 `scratchpad` 의 `impcalls.py`·`pecalls.py`). 정보 클래스 상수 (`edx`) 가 줄 앞에 같이 나와야 쓸모가 있다.

## 판정

**분석성공.** 요청서 질문 1~7 모두 답했다. 30칸 → 쓰는 줄 확정 29 (nt 직접 12 · syscall 안 5 · 다른 프로세스·드라이버가 부름 12) · 여전히 모름 1 (`SharedCommitCharge`, 후보만 좁힘) + `OwnerProcessId` 둘째 쓰기 확정 (`NtSetInformationProcess +0x1ecd` = 클래스 `0x31`, 부르는 쪽 `KERNELBASE!ConsoleCommitState`). 버전은 `NtCreateUserProcess`/`PspInsertThread`/ntdll 알려진 사실과 일치.

정적으로 못 닫은 것:
- `SharedCommitCharge` K1 `0x2b` 를 만든 줄 (후보 `MiMapViewOfImageSection +0x4e9` 등 — F12).
- `mssecflt.sys`·`bam.sys` 호출이 **이 VM 의 test.exe 에서 실제로 불리는지**, 값 (F2·F3).
- `DeviceMap` 을 처음 채우는 syscall (F5), `Win32Process` 의 트리거 (F9), `InvertedFunctionTable` 이 test.exe 에서 syscall 까지 가는지 (F7).
- `ImagePathHash`·`PrefetchTrace` 의 프리페치 설정 게이트 (F11), `ModifiedPageCount` 의 정확한 목록 이름.
- `OwnerProcessId` `0x729` 가 conhost PID `0x728` | 1 인지 (F10 — `[추측]`).

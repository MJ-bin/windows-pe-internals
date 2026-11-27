# 153 dbgk-events — 정적 분석 결과

- 요청서: `_workspace/sast/153_dbgk-events.md` (Q291) · 원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` ⑥ 절 (읽기만).
- 읽기만 한 PE_emulator 쪽: `ext/syscall/syscall_layer_cpp/src/exception/nt_debug_more.cpp` (`:60 ~ :120` 팀원 주석) · `src/process/nt_create_user_process.cpp` (`:166 ~ :172`).
- 날짜: 2026-11-27. 빌드: Windows 11 24H2 26100.9457 x64.
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`).
- **PDB GUID 확인**: `ms/sym/ntoskrnl.exe.pdb.info.txt` 의 `guid_age=C29EBFB06B78B3C020DCA66D99713F9E1` = 기준 GUID 와 **같다.** (`ms/ghidra/ntoskrnl.analyze.log` 에 `SetPdb:` 줄 있음 · `_index.tsv` 있음 → 이미 분석됨. analyze 다시 안 함.)
- 뽑은 함수: **23개** (ntoskrnl 20 + ntdll 3), depth 0 (callee 안 따라감) → `ms/decomp/{ntoskrnl,ntdll}/`, `ms/disassemble/{ntoskrnl,ntdll}/`.
  - 첫 번: 17개 `^(DbgkpSendApiMessage|DbgkpQueueMessage|DbgkpPostModuleMessages|DbgkpSectionToFileHandle|DbgkSendSystemDllMessages|DbgkpMarkProcessPeb|NtCreateDebugObject|NtWaitForDebugEvent|DbgkpConvertKernelToUserStateChange|DbgkpOpenHandles|NtDebugContinue|DbgkpWakeTarget|DbgkpFreeDebugEvent|DbgkpSuspendProcess|DbgkpResumeProcess|DbgkpCloseObject|NtSetInformationDebugObject)$` (로그 `ms/ghidra/ntoskrnl.export.sast153a.stdout`).
  - 둘째 번: 3개 `^(DbgkPostModuleMessage|DbgkMapViewOfSection|PsQuerySystemDllInfo)$` (`...sast153b.stdout`). 넓힌 이유: `DbgkpPostModuleMessages` 가 실제 메시지를 만드는 도우미 (`DbgkPostModuleMessage` 단수) · DLL 올림 이벤트가 나중 DLL 에서 오는 줄 (`DbgkMapViewOfSection`) · `DbgkSendSystemDllMessages` 가 읽는 표 (`PsQuerySystemDllInfo`).
  - ntdll 2번: `^(DbgUiConvertStateChangeStructure|DbgUiWaitStateChange)$` (2개) 와 그 꼬리호출 대상 `^DbgUiConvertStateChangeStructureWorker$` (1개) (로그 `ms/ghidra/ntdll.export.sast153a.stdout` · `...sast153b.stdout`). 요청서 ⚠ 조건: **`_workspace/sast/148_rpcss-connect-sd_result.md` 가 이 요청 도중 (14:39) 생겨서** 그 뒤 (14:50 무렵) 열었다. 열기 전 `ps` 에 java 없음 · `ntdll.lock` 없음 확인.
  - 각 export 전에 `free -g` 확인: available 27 ~ 28 G (기준 12 G 이상). 각 4초 안팎. 다른 Ghidra 실행은 그때 안 돌고 있었다 (`ps` 에 java 없음).
- 이미 있던 asm 도 읽음: `DbgkCreateThread` · `PspNotifyThreadCreation` · `DbgkCopyProcessDebugPort` · `PspInsertProcess` · `PspUserThreadStartup` · `PspBuildCreateProcessContext` · `NtCreateUserProcess` · `MmCreatePeb`(grep 만) · 그리고 kernelbase `CreateProcessInternalW.asm` 한 군데 (**기존 파일 읽기만, Ghidra 안 열음**).
- **ntdll 은 요청서 ⚠ 조건대로 148 결과서가 생긴 뒤에만 열었고, `DbgUi*` 셋 (위) 만 뽑았다.** 그 밖 ntdll 함수 · 다른 모듈 Ghidra 프로젝트는 안 열었다. (처음엔 148 결과서가 없어서 ntdll 없이 썼다가, 끝 무렵 생긴 걸 보고 3-3b 를 보탰다.)
- 버전 대조 (알려진 사실): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치 · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv`). 스크립트 오프셋 · 바이너리 둘 다 믿어도 된다.
- 이 문서에 디컴파일 코드를 붙이지 않았다. 오프셋 · 상수 · 우리 말 요약만.

## 스크립트 문제

없음. 다만 읽을 때 주의 하나:
- `ms/decomp/ntoskrnl/NtDebugContinue.c` 는 `ObReferenceObjectByHandle` 의 첫 인자를 `*param_2` (= ClientId 첫 8 B) 로 그렸다. **`.asm` 이 맞다**: `NtDebugContinue +0x0` 부터 `+0x95` 호출까지 RCX 를 바꾸는 줄이 없다 → 핸들 = 첫 인자 RCX. (Ghidra 인자 추정 오류, 스크립트 문제 아님.)

## 이 PDB 에 없는 것 (칸 이름 출처)

- `_DEBUG_OBJECT` · `_DEBUG_EVENT` · `_DBGKM_APIMSG` · `_DBGUI_WAIT_STATE_CHANGE` 는 `ntoskrnl.exe.pdb` 에 **없다** (`pdb_struct.py` → "없음"). `_DBGKM_EXCEPTION64` (`sizeof 0xa0`) · `_PORT_MESSAGE` (`sizeof 0x28`) 만 있다.
- 그래서 아래 표의 **오프셋 · 크기 · 값은 `[정적]` (asm 에서 셈)**, 칸 **이름은 `[추측: 이름]`** (공개 문서의 관례 이름). EPROCESS · ETHREAD · KTHREAD · PEB 칸 이름은 `[정적] PDB`.

---

## 먼저 읽는 한 줄 답

**1. 붙는 길과 첫 이벤트**
- 속성 `0x60001` (크기 8) 의 값 = **디버그 객체 핸들** → 문맥 `+0x90` 에 그대로 저장 (`PspBuildCreateProcessContext +0x17a3 ~ +0x17b2`, 이때는 해석 안 함) → `PspInsertProcess` 5번째 인자 → **`+0x2f3` `ObReferenceObjectByHandle(핸들, 접근 2, DbgkDebugObjectType)`** → **`+0x13e` `DbgkCopyProcessDebugPort`** 가 `EPROCESS.DebugPort` (+0x308) 에 객체를 쓰고, `DbgkpMarkProcessPeb` 가 `PEB.BeingDebugged` (+0x2) = 1 로 쓴다 (PEB 는 `PspAllocateProcess +0x230f MmCreatePeb` 가 이미 지음).
- 첫 이벤트는 **자식 첫 스레드가 처음 커널에서 돌 때 그 스레드가 직접** 보낸다 (`PspUserThreadStartup +0x9c` → `PspNotifyThreadCreation +0x1c` → `DbgkCreateThread`). 순서: **① CreateProcess (ApiNumber 2)** → **② LoadDll = ntdll (ApiNumber 5)** (WoW64 자식이면 32비트 ntdll LoadDll 하나 더) → (클론 스레드일 때만) 모듈 LoadDll 들 → 유저로 감. **exe 는 따로 LoadDll 이 없고 CreateProcess 메시지 안에 (파일 핸들 + 이미지 기준 주소 + 진입점) 들어 있다.**
- 깃발: `+0x2c4` = `ETHREAD.CrossThreadFlags.HideFromDebugger` (bit 2) · `+0x327` = `ETHREAD.SameThreadPassiveFlags.ClonedThread` (bit 4) · `+0x2b3` = `EPROCESS.Flags.CreateReported` (bit 0, 이미 1 이면 스레드 길).
- 메시지 `0x400018` = `DataLength 0x18 · TotalLength 0x40`, `8` = 종류 (`LPC_DEBUG_EVENT`), `1` = `ApiNumber` (CreateThread). CreateProcess 쪽은 `0x600038` · `8` · `2`.

**2. 큐에 넣기 · 기다림 · 깨움**
- 보낸 스레드는 **자기 커널 스택**의 0x168 B 이벤트에 메시지를 복사해 (`DbgkpQueueMessage +0x253` 길), 디버그 객체 `EventList` (+0x50) **꼬리에 끼우고** (`+0x162 ~ +0x16c`) `EventsPresent` 를 `KeSetEvent` (`+0x17e`) 한 뒤, **`KeWaitForSingleObject(자기 이벤트의 ContinueEvent, Executive, KernelMode, 비경보, 무한)`** (`+0x1f0`) 에서 멈춘다.
- 깨움: `DbgkpWakeTarget +0x51` 의 `KeSetEvent(이벤트+0x10)` ← **`NtDebugContinue +0x148`** (정상) 또는 **`DbgkpCloseObject +0xe6`** (디버거가 객체를 닫음 → `STATUS_DEBUGGER_INACTIVE` 0xc0000354). 깨어나면 이벤트 안 메시지 0x110 B 를 보낸 쪽 메시지로 되복사 (`ReturnedStatus` = ContinueStatus).
- **같은 프로세스 다른 스레드는: 얼리는 경우와 안 얼리는 경우가 있다.** `DbgkpSendApiMessage` 의 `Flags` bit 0 이 켜진 보냄 (스레드 만듦 `EDX=1` · DLL 올림 `DbgkMapViewOfSection` `EDX=1` · ntdll LoadDll `EDX=3`) 만 `DbgkpSuspendProcess` → `PsFreezeProcess` 로 얼린다 (`+0x65`), 답이 오면 `DbgkpResumeProcess` (`+0x95`). **CreateProcess 이벤트는 `EDX=0` 이라 안 얼린다.**

**3. `NtWaitForDebugEvent` · `NtDebugContinue`**
- **고르는 규칙**: 큐를 앞에서부터 훑어 `Flags & 5 == 0` (READ · INACTIVE 둘 다 아님) 이고 **앞쪽에 같은 `ClientId.UniqueProcess` 이벤트가 없는** 첫 이벤트. 앞에 같은 프로세스 것이 있으면 그 이벤트에 INACTIVE(4) 를 달고 건너뜀. 고른 이벤트에는 READ(1) 를 단다 (= "이미 건넨 것"). 큐에서는 **안 지운다** (지우는 건 `NtDebugContinue`).
- **빈 큐**: `KeWaitForSingleObject(디버그 객체 자체 = EventsPresent 알림 이벤트, Executive, 호출자 모드, Alertable, Timeout)`. 돌려주는 status: 시간 끝 `0x102` · alert `0x101` · 유저 APC `0xc0` — 셋 다 **출력 구조 0xB8 B 를 0 으로 채워 써 준다** (NewState 0). 객체가 닫는 중이면 `0xc0000354`.
- **출력 구조 크기 `0xB8` 맞다** (`NtWaitForDebugEvent +0x4d MOV R8D,0xb8` · 쓰기 확인 첫 B `+0xc2/+0xc4` · 끝 B `+0xc6/+0xcc` (= 버퍼 바이트 +0xb7)). 칸 표는 아래 3-3 (유저 쪽 `ntdll` 이 읽는 오프셋과 칸마다 일치 — 3-3b).
- **`NewState 3` (프로세스 만듦) 일 때 디버거 핸들 표에 여는 것**: 프로세스 핸들 (접근 `0x12067b`) · 스레드 핸들 (`0x12007b`) · 파일 핸들 (커널 핸들을 같은 접근으로 복제 후 원본 닫음). 세 개 다 호출한 디버거 프로세스의 핸들 표에 들어간다.
- **`NtDebugContinue`**: ClientId (UniqueProcess · UniqueThread) 둘 다 같고 **READ 가 켜진** 이벤트를 큐에서 빼고 → `ReturnedStatus (+0x84) = ContinueStatus` · `Status (+0x48) = 0` → `DbgkpWakeTarget`. 그 프로세스의 **다음 이벤트 하나**의 INACTIVE 를 풀고 `EventsPresent` 를 켠다.
- **ContinueStatus 허용값 = 팀원 주석의 여섯 값과 같다**: `0x10002 · 0x80010001 · 0x10001 · 0x40010001 · 0x40010003 · 0x40010004`. 그 밖 · 못 찾음 = `0xc000000d`. **팀원 주석 `0xB8` · 여섯 값 둘 다 맞다.**
- (정적 부수 발견) `0x40010001` 로 이어 주면 보낸 쪽 `DbgkpSendApiMessage` 가 **같은 메시지를 다시 큐에 넣는다** (`+0x9e ~ +0xa6`) — 이 값은 "이어가기"가 아니라 "나중에 답함" 이다.

---

## 1. 자식에 디버그 객체가 붙는 길과 첫 이벤트

### 1-1. 붙는 길 (`NtCreateUserProcess` 안 차례) `[정적]`

| 차례 | 어디 (`ms/disassemble/…`) | 줄 | 하는 일 |
|---|---|---|---|
| a | kernelbase `CreateProcessInternalW.asm` (기존, 읽기만) | `+0x809` `TEST [RSP+0x108],3` → `+0x813/+0x81a DbgUiConnectToDbg` → `+0x82a/+0x831 DbgUiGetThreadDebugObject` → `+0x8ec ~ +0x908` 속성 칸 `{0x60001, 8, 핸들}` (0x20 B 짜리) | `CreationFlags & 3` (DEBUG_PROCESS · DEBUG_ONLY_THIS_PROCESS) 가 있으면 디버그 객체 핸들을 얻어 속성으로 건넨다. `+0x841 ~ +0x84b` 는 bit 1 이면 내부 플래그 bit 1 도 켠다 `[추측: 이게 PspInsertProcess 4번째 인자 bit 1 로 감]` |
| b | `PspBuildCreateProcessContext.asm` | 번호 분기 `+0x164d SUB RAX,0x60000` ~ `+0x165d JZ`; **0x60001 가지 `+0x17a3 CMP [RDI+8],8`** (크기가 8 이 아니면 `JNZ → +0x1cd MOV EDI,0xc000000d`) · `+0x17ae/+0x17b2 MOV [R12+0x90],RAX` | 문맥 +0x90 = 핸들 값. **여기서는 핸들을 열어 보지 않는다.** (0x60000 → 문맥 +0x80 · 0x60002 → +0x98) |
| c | `NtCreateUserProcess.asm` | 문맥 칸 = `[RBP+0x1a0]` (`+0x274 LEA RCX,[RBP+0x1a0]` · `memset` 0x260 `+0x27b` · `+0x285 LEA R9,[RBP+0x1a0]` → `+0x295/+0x29a PspBuildCreateProcessContext`). 0x1a0 + 0x90 = `[RBP+0x230]` → `+0xfa3/+0xfaa` 가 `PspInsertProcess` 5번째 인자 `[RSP+0x20]` 로 넘김 · `+0xfbd/+0xfc2` 호출 | 순서: `PspAllocateProcess +0xc31/+0xc36` (안에서 `MmCreatePeb +0x230f/+0x2314`) → `PspAllocateThread +0xe57` → **`PspInsertProcess +0xfbd`** → `PspInsertThread +0x1015` |
| d | `PspInsertProcess.asm` | `+0x115 ~ +0x11e` 만든 쪽(현재) 프로세스가 `ProcessDelete` (Flags bit 3) 면 `+0x273 → 0xc000010a` · `+0x124 ~ +0x12b` 5번째 인자 `≠ 0` 이면 `+0x2c5` 로 | 핸들 있을 때만 객체를 연다 |
| e | `PspInsertProcess.asm` | **`+0x2f3/+0x2f8 ObReferenceObjectByHandle`** (RCX = 핸들 · EDX = **2** · R8 = `DbgkDebugObjectType` · R9B = 호출 스레드 `PreviousMode`) → 실패면 `+0x300 JS → +0x24d` (정리 뒤 `EDI` = 그 status 를 `+0x257/+0x259` 로 돌려줌) | 접근 2 = `DEBUG_PROCESS_ASSIGN` `[추측: 이름]`. 거절 status (잘못된 핸들 · 형식 · 접근) 는 `ObReferenceObjectByHandle` 쪽 값이고 이 asm 에 상수는 없다 |
| f | `PspInsertProcess.asm` | `+0x131 ~ +0x13e` `DbgkCopyProcessDebugPort(자식, 만든 쪽 프로세스, 객체 또는 0, &bool)` (ret `+0x143`) · 뒤 `+0x145 ~ +0x148` → `+0x30b ~ +0x313` 객체 참조 하나 내려놓음 (`ObfDereferenceObject`) · `+0x156 ~ +0x322` bool 이 참이고 4번째 인자 bit 1 이면 자식 `Flags \|= 2` | **핸들이 없을 때도 이 호출은 한다** (R8 = 0): 만든 쪽 프로세스에 `DebugPort` 가 있고 그쪽 `NoDebugInherit`(Flags bit 1) 가 꺼져 있으면 **같은 객체를 물려받는다** (`DbgkCopyProcessDebugPort +0x3c ~ +0xe0`) |

### 1-2. `DebugPort` · `BeingDebugged` 를 누가 언제 쓰나 `[정적]`

| 칸 (PDB) | 쓰는 줄 | 값 · 조건 |
|---|---|---|
| `EPROCESS.DebugPort` (+0x308) | `DbgkCopyProcessDebugPort +0x27` `MOV [RCX+0x308],RDI(0)` | 자식 칸을 **먼저 0 으로** (`PspAllocateProcess` 가 아니라 여기서 명시 0 — run50 의 "`+0x27` 0" 이 이것) |
| 〃 | 조각 `FUN_140845053` 안 `(= DbgkCopyProcessDebugPort +0x2c89e1)` `MOV [RSI+0x308],RBX` | 객체 `Flags` bit 0 (닫는 중) 이 꺼져 있을 때만, **객체 자기 뮤텍스(+0x18) 를 쥔 채** 씀. 켜져 있으면 안 쓰고 객체 참조를 내려놓고 **status 0 으로 돌아감** (붙지 않은 채 성공) |
| `PEB.BeingDebugged` (+0x2) | `DbgkpMarkProcessPeb +0x84` `MOV [RAX+2],CL` (CL = `DebugPort ≠ 0`) · WoW64 이면 `+0xca` 로 PEB32 도 (`EWOW64PROCESS.Peb`) | 부르는 곳: **붙을 때** `DbgkCopyProcessDebugPort` 조각 `(= +0x2c8a0c)`, **뗄 때** `DbgkpCloseObject` 조각 `+0x18` (그땐 0 으로). `EPROCESS.Peb` (+0x2e0) 가 0 이면 아무것도 안 함 (`+0x4a`) |
| `EPROCESS.Flags.NoDebugInherit` (bit 1) | `PspInsertProcess +0x322` `OR.LOCK [RBX+0x1f4],2` | 붙었고 (`bool ≠ 0`) 4번째 인자 bit 1 이 켜졌을 때 |

- 때: `PspAllocateProcess` 가 PEB 를 짓고 (`+0x230f`) → 첫 스레드 틀 (`PspAllocateThread`) 을 만든 **뒤**, 스레드를 끼우기 (`PspInsertThread`) **앞**. 자식 스레드는 아직 한 번도 안 돌았다.
- 그 밖 객체 참조 규칙 (`DbgkCopyProcessDebugPort`): 객체 `+0x60` (Flags) bit 2 가 켜져 있으면 자식이 `WoW64Process ≠ 0` 이고 `Machine` (`EPROCESS +0x6ec`) 이 `0x14c` 또는 `0x1c4` 여야 한다 — 아니면 `0xc00000bb` (조각 `+0x2c894f ~ +0x2c8978`). `PsTestProtectedProcessIncompatibility` (`+0x88`) 가 참이면 `0xc0000712` (`+0x95`). 자식 `KPROCESS.SecureState` (+0x170) bit 0 `[추측: 보안 프로세스 표시]` 가 켜졌을 때만 보안 호출 (`VslpEnterIumSecureMode` 이름의 호출, 조각 `+0x2c89b4`) — 일반 test.exe 와 무관 `[추측: 이름은 별칭 하나]`.

### 1-3. 첫 이벤트 차례 표 `[정적]`

보내는 스레드 = **자식 첫 스레드 자신**. 호출 사슬: `PspUserThreadStartup +0x9c/+0xa1` → `PspNotifyThreadCreation +0x1c/+0x21` → `DbgkCreateThread`. (`PspUserThreadStartup +0x66 ~ +0x6a` 에서 `EPROCESS.Flags3.Minimal` (bit 0) 이 꺼져 있을 때만; 켜져 있으면 `+0xc2 ~ +0xc5` 로 `DbgkCreateMinimalThread`. 이 일은 `+0xae` `PspInitializeThunkContext` (유저로 가기) **앞**이다. 스레드가 `ThreadInserted` (CrossThreadFlags bit 1) 가 아니면 `+0x4b ~ +0x4d` → `+0xe5 ~ +0xf0` 로 먼저 `PspTerminateThreadByPointer` 를 부르고 이벤트는 그대로 이어진다.)

| 차례 | 종류 (ApiNumber / NewState) | 보내는 함수 · 줄 | `Flags` (EDX) | 안 보내는 조건 |
|---|---|---|---|---|
| 0 | (사전) | `DbgkCreateThread +0x7d ~ +0x91` 프로세스 `Flags \|= CreateReported(1) \| ImageNotifyDone(0x400000)` 를 원자적으로, 옛 값을 R12 에 | — | `+0x9c ~ +0xa4`: `DebugPort == 0` 이면 아무것도 안 보내고 끝 |
| 1 | **CreateProcess** (2 / 3) | `DbgkCreateThread +0x207ad6/+0x207adb` → `DbgkpSendApiMessage(프로세스, 0, 메시지)` (`+0x2b3 TEST R12B,1` 이 0 → `+0x2b7 JZ` 로 조각 `+0x2079e6` 으로) | **0** (얼림 없음) | 옛 `CreateReported` 가 이미 1 이면 이 길 안 탐 (스레드 길). 보낸 스레드 `SkipCreationMsg` (bit 6) 이면 큐 단계에서 `0xc0000353` 로 버려짐 (`DbgkpQueueMessage +0x2ae`) |
| 2 | **LoadDll = ntdll** (5 / 9) | `DbgkCreateThread +0x207afb/+0x207b00` → `DbgkSendSystemDllMessages(0,0,메시지)` → `+0x2dd/+0x2e2` `DbgkpSendApiMessage(프로세스, 3, …)` (메시지 머리 `+0x228 ~ +0x239`) | **3** (bit 0 = 얼림, bit 1 = 큐에 0x40 로 바뀌어 건너감 `SkipCreationMsg` 대상) | `PsQuerySystemDllInfo(0)` 이 0 이면 건너뜀 (`+0xa9 ~ +0xb4`). `ZwOpenFile` 이 실패해도 파일 핸들 0 으로 **보낸다** (`+0x2d4`) |
| 3 | LoadDll = 32비트 ntdll (5 / 9) — **WoW64 자식만** | 같은 루프 `ESI = 1 ~ 5` (`+0x99 ~ +0x273`): 항목 `Flags & 8` · `WoW64Process ≠ 0` · `ESI == PsWow64GetProcessNtdllType` (`+0x29c ~ +0x2bd`) 일 때 | 3 | 위 조건 불일치 시 건너뜀 |
| 4 | (클론 스레드만) 모듈마다 LoadDll (5 / 9) | `DbgkCreateThread +0x320 ~ +0x338` (`ClonedThread` 켜졌을 때) → `DbgkpPostModuleMessages(프로세스, 스레드, 0)` → 모듈마다 `DbgkPostModuleMessage +0x14e` → `DbgkpSendApiMessage(.., 3, …)` | 3 | 보통 test.exe 자식은 클론이 아니라 안 탄다. 로더 목록 앞 둘 (`CMP EAX,1 / JBE`, `DbgkpPostModuleMessages +0xa5 ~ +0xa8`) 은 건너뜀 `[추측: 앞 둘 = exe · ntdll]` |
| — | (이벤트 아님) | `+0x320` 이후 `PspUserThreadStartup +0xae` `PspInitializeThunkContext` → 유저 `LdrInitializeThunk` | | |

- **ReturnedStatus 는 이 자리에서 읽지 않는다.** `DbgkCreateThread` 는 `DbgkpSendApiMessage` 의 반환 · 메시지 `ReturnedStatus` 를 안 본다 (`+0x320` · `+0x207adb` 뒤에 비교 없음). `DbgkpSendApiMessage` 가 보는 값은 `0x40010001` 하나뿐 (2절).
- **처음 CreateProcess 이후 이어지는 것 (밖)**: 이 스레드가 만든 새 스레드는 `DbgkCreateThread` 의 스레드 길 (CreateThread, ApiNumber 1, `Flags 1`). 이후 DLL 은 `NtMapViewOfSection +0x2dc` / `MiMapViewOfSectionExCommon +0x1fd` → `DbgkMapViewOfSection` → `+0x164 DbgkpSendApiMessage(프로세스, 1, …)` (LoadDll). 종료는 `PspExitThread +0x7b9 DbgkExitProcess` · `DbgkExitThread`. 예외는 `DbgkForwardException +0x1ee DbgkpQueueMessage`. → 다음 요청 후보.

### 1-4. `DbgkCreateThread` 깃발 · 조각 줄 표 `[정적]`

| 줄 | 명령 | 뜻 (PDB) |
|---|---|---|
| `+0x2c4` | `MOV EAX,[R14+0x5a0]` → `TEST AL,0x4` → `JNZ +0x320` | `ETHREAD.CrossThreadFlags` (+0x5a0) **`HideFromDebugger` (bit 2)**. 켜졌으면 CreateThread 메시지 **통째로 건너뜀**. 스레드 길에서만 검사 (CreateProcess 조각은 검사 없음) |
| `+0x327` | `MOV EAX,[R14+0x5a4]` → `TEST AL,0x10` | `ETHREAD.SameThreadPassiveFlags` (+0x5a4) **`ClonedThread` (bit 4)**. 켜졌으면 `+0x338/+0x33d DbgkpPostModuleMessages(프로세스, 스레드, 0)` |
| `+0x2b3` | `TEST R12B,1` | `EPROCESS.Flags` (+0x1f4) 의 옛 값 **`CreateReported` (bit 0)** — 0 이면 첫 스레드 (조각으로), 1 이면 스레드 길 |
| `+0x96` | `BT EAX,0x16` | `EPROCESS.Flags.ImageNotifyDone` (bit 22) — 이미지 알림 (PsCallImageNotifyRoutines) 을 이미 했나. 디버거와 무관 |
| `+0xd9 ~ +0xf7` | `[RDI+0x310]` ≠ 0 이면 `Machine (+0x6ec)` 이 `0x14c` / `0x1c4` 인지 → `[RSP+0x20]` byte | "32비트 WoW 프로세스" 표시. 조각에서 진입점 계산을 가른다 |
| `+0x9c ~ +0xa4` | `CMP [RDI+0x308],0` | `EPROCESS.DebugPort` — 0 이면 끝 |

조각 `+0x2079e6 ~ +0x207b01` (같은 함수의 떨어진 범위, `JZ 0x140b18aea` 목적지 − 시작 = `+0x2079e6`) = **CreateProcess 메시지 만들기**:

| 줄 | 하는 일 | 메시지 칸 (메시지 시작 = `[RSP+0x90]`) |
|---|---|---|
| `+0x2079f4/+0x2079f9` | `memset(msg, 0, 0x60)` (`LEA R8D,[RDX+0x60]`) | 0x60 B 지움 |
| `+0x207a00/+0x207a05` | `DbgkpSectionToFileHandle(프로세스.SectionObject [RDI+0x2a8])` → `[RSP+0xc8]` | **`+0x38` FileHandle** (커널 핸들, 실패 시 0) |
| `+0x207a0d` | `[RSP+0xd0] = [RDI+0x2b0]` (`EPROCESS.SectionBaseAddress`) | **`+0x40` BaseOfImage** |
| `+0x207a1c ~ +0x207a2d` | `[RSP+0xe8]=0 · [RSP+0xd8]=0 · [RSP+0xdc]=0` | 먼저 0 |
| `+0x207a3c/+0x207a41` | `RtlImageNtHeader(SectionBase)` | |
| `+0x207a49 ~ +0x207a67` | 32비트 WoW 이면 `[NT+0x34]` (ImageBase32) + `[NT+0x28]` (EntryRVA), 아니면 `[NT+0x28]` + `[NT+0x30]` (ImageBase) → `[RSP+0xe8]` | **`+0x58` InitialThread.StartAddress** = 이미지 진입점 (스레드의 Win32StartAddress 가 **아니다**) |
| `+0x207a6f ~ +0x207a7c` | `[RSP+0xd8] = [NT+0xc]` · `[RSP+0xdc] = [NT+0x10]` | **`+0x48` DebugInfoFileOffset** (PointerToSymbolTable) · **`+0x4c` DebugInfoSize** (NumberOfSymbols) |
| `+0x207aa8/+0x207ab3/+0x207abe` | `[RSP+0x90]=0x600038` · `[RSP+0x94]=8` · `[RSP+0xb8]=2` | 머리: DataLength `0x38` · TotalLength `0x60` · 종류 8 · **`+0x28` ApiNumber = 2** |
| `+0x207ad6/+0x207adb` | `DbgkpSendApiMessage(RDI, 0, msg)` | |
| `+0x207ae3 ~ +0x207aea` | 되돌아온 `[RSP+0xc8]` ≠ 0 이면 `ObCloseHandle(핸들, 0)` | 디버거가 읽었으면 `DbgkpConvertKernelToUserStateChange +0x11d` 가 이벤트 안 칸을 0 으로 만들어 둬서 여기선 0 → 안 닫음. 안 읽혔으면 여기서 닫음 |
| `+0x207afb/+0x207b00` | `DbgkSendSystemDllMessages(0, 0, msg)` | 같은 `msg` 를 재사용 (내부에서 처음부터 다시 채움) |
| `+0x207b01` | `JMP +0x320` | `ClonedThread` 검사로 |

스레드 길 (`+0x2c4 ~ +0x320`) 메시지 칸:

| 줄 | 칸 |
|---|---|
| `+0x2c8 ~ +0x2d6` | `memset(msg, 0, 0x40)` |
| `+0x2db/+0x2e2` | `[RSP+0xc8] = [R14+0x560]` = `ETHREAD.Win32StartAddress` → **`+0x38` StartAddress** (`+0x30` SubSystemKey 는 0) |
| `+0x2ea/+0x2f5/+0x300` | `[+0x90]=0x400018` (DataLength 0x18 · TotalLength 0x40) · `[+0x94]=8` · `[+0xb8]=1` (ApiNumber 1 = CreateThread) |
| `+0x31b/+0x320` | `DbgkpSendApiMessage(RDI, 1, msg)` — **`Flags` 1 = 다른 스레드 얼림** |

### 1-5. 메시지 칸 표 (0x110 B, `DBGKM_APIMSG`) `[정적]` 오프셋 · `[추측: 이름]`

크기 근거: `DbgkCreateThread +0x3e/+0x45` `memset(.., 0, 0x110)` · `DbgkpQueueMessage +0xc7 ~ +0x11a` 가 0x80 × 2 + 0x10 = 0x110 B 복사.

| 메시지 칸 | 크기 | 이름 | 채우는 쪽 · 뜻 |
|---|---|---|---|
| `+0x00` | u16 · u16 | `u1` DataLength · TotalLength | CreateThread `0x18` · `0x40` / CreateProcess `0x38` · `0x60` / LoadDll `0x28` · `0x50` (`PORT_MESSAGE` 0x28 B 머리 뒤) |
| `+0x04` | u16 | `u2.Type` | 8 (`LPC_DEBUG_EVENT`) 항상 |
| `+0x08 ~ +0x27` | | ClientId · MessageId … | **안 채움 (0)**. 이벤트 안 ClientId (+0x28) 는 `DbgkpQueueMessage +0x11d/+0x125` 가 스레드 `Cid` (`ETHREAD +0x508`) 에서 따로 복사 |
| `+0x28` | u32 | `ApiNumber` | 0 예외 · **1 CreateThread · 2 CreateProcess** · 3 ExitThread · 4 ExitProcess · **5 LoadDll** · 6 UnloadDll (3·4·6 은 `DbgkpQueueMessage +0x284 ~ +0x28c` · `DbgkpConvertKernelToUserStateChange +0xb2 ~ +0xdd` 로 확인, 보내는 줄은 이 요청 밖) |
| `+0x2c` | u32 | `ReturnedStatus` | 보낼 때 `DbgkpSendApiMessage +0x79` 가 **`0x103` (STATUS_PENDING)** 로 씀. 이어줄 때 `NtDebugContinue +0x13b` 가 ContinueStatus 로 덮음 |
| `+0x30 ~` | | 공용체 | 아래 |

공용체 (메시지 `+0x30` 부터):

| ApiNumber | 칸 (메시지 오프셋) |
|---|---|
| 1 CreateThread | `+0x30` SubSystemKey (u32, 0) · `+0x38` StartAddress (= `Win32StartAddress`) |
| 2 CreateProcess | `+0x30` SubSystemKey (0) · `+0x38` FileHandle · `+0x40` BaseOfImage · `+0x48` DebugInfoFileOffset (u32) · `+0x4c` DebugInfoSize (u32) · `+0x50` InitialThread.SubSystemKey (0) · `+0x58` InitialThread.StartAddress |
| 5 LoadDll | `+0x30` FileHandle · `+0x38` BaseOfDll · `+0x40` DebugInfoFileOffset (u32) · `+0x44` DebugInfoSize (u32) · `+0x48` NamePointer |
| 0 예외 | `_DBGKM_EXCEPTION64` (PDB: 0xa0 B — `ExceptionRecord` 0x98 B + `FirstChance` +0x98) — 이 요청 밖 |

- LoadDll 의 `NamePointer` = **TEB 의 `NtTib.ArbitraryUserPointer` 칸의 주소** (`DbgkSendSystemDllMessages +0x175 ~ +0x17c`: `TEB+0x28`). 그 칸에 `TEB+0x1268` (StaticUnicodeBuffer) 포인터를 넣고 (`RtlStringCbCopyW +0x170` 로 시스템 DLL 경로 복사), 보낸 뒤 **0 으로 지운다** (조각 `FUN_140b36a80 +0xa`). 즉 디버거는 이 포인터를 따라 **자식 메모리에서** 이름을 읽는다. (`DbgkMapViewOfSection +0x1b2 ~ +0x1b6` 도 같은 꼴.)
- 파일 핸들은 `DbgkpSectionToFileHandle +0x7d` / `DbgkSendSystemDllMessages +0x21b` 의 **`ZwOpenFile` (접근 `0x80100000`, `ObjectAttributes.Attributes 0x640` = 대소문자 무시 · 커널 핸들 · 접근 검사 강제 `[추측: 이름]`, 공유 7, 옵션 0x20)** 로 **경로 이름으로 다시 연** 커널 핸들이다. 섹션의 파일 객체를 복제한 게 아니다.

---

## 3. `NtWaitForDebugEvent` · `NtDebugContinue` (요청서가 1 → 3 → 2 순으로 하라고 해서 3 을 먼저)

### 3-1. 디버그 객체 (`NtCreateDebugObject`) 칸 `[정적]` 오프셋 · `[추측: 이름]`

크기 **0x68** (`NtCreateDebugObject +0x93 MOV [RSP+0x28],0x68` → `ObCreateObjectEx` `+0xa1/+0xa6`). `Flags` 인자가 `0xfffffffe` 와 겹치면 `0xc000000d` (`+0x5f ~ +0x68`).

| 칸 | 크기 | 이름 | 초기값 · 쓰이는 곳 |
|---|---|---|---|
| `+0x00` | 0x18 | `EventsPresent` (KEVENT) | **알림 이벤트 (NotificationEvent)**, 꺼진 채 시작 (`+0xe5` `KeInitializeEvent(obj, 0, 0)`). `NtWaitForDebugEvent` 가 이걸 기다린다 |
| `+0x18` | 0x38 | `Mutex` (가드 뮤텍스) | `Count = 1` (`+0xb8`) · 문 이벤트 `+0x30` (`+0xc4 ~ +0xcd`). 큐 · 플래그는 이걸 쥐고 건드림 (`KeAcquireGuardedMutex` 별칭) |
| `+0x50` | 0x10 | `EventList` | 빈 원형 목록 (`+0xd2 ~ +0xda`) |
| `+0x60` | u32 | `Flags` | bit 0 = **닫는 중** (`DbgkpCloseObject +0x2f` 가 켬; 켜지면 새 이벤트 `0xc0000354`) · bit 1 = **닫을 때 디버기 끝냄** (`+0xf0 ~ +0xf2`: 만들 때 `Flags인자 & 1` 을 << 1; `NtSetInformationDebugObject +0x104 ~ +0x116` 이 같은 bit 를 켜고 끔) · bit 2 = **만든 프로세스가 32비트 WoW (x86/ARM32)** (`+0x105 ~ +0x137`) → 붙는 자식도 32비트 WoW 여야 함 (`DbgkCopyProcessDebugPort` 조각 `+0x2c8948`) |

접근 권한 값 (`ObReferenceObjectByHandle` 의 `DesiredAccess`): `NtWaitForDebugEvent`·`NtDebugContinue` = **1** (`DEBUG_READ_EVENT`) · `PspInsertProcess` = **2** (`DEBUG_PROCESS_ASSIGN`) · `NtSetInformationDebugObject` = **4** (`DEBUG_SET_INFORMATION`) `[추측: 이름]`. (팀원 `reference_debug_object(…, 1 / 2 / 4)` 와 같다.)

### 3-2. `NtWaitForDebugEvent` 줄 지도 `[정적]` (`ms/disassemble/ntoskrnl/NtWaitForDebugEvent.asm`)

| 줄 | 하는 일 |
|---|---|
| `+0x3b ~ +0x44` | `PreviousMode` (스레드 `+0x232`) → R13B |
| `+0x4d ~ +0x58` | **`memset(지역 0xb8 B, 0)`** — 커널 쪽 `DBGUI_WAIT_STATE_CHANGE` 지역 복사 |
| `+0x5e ~ +0xb1` | Timeout 포인터가 있으면 (유저 모드면 읽어 봄) 8 B 를 지역에 복사하고 **`KUSER_SHARED_DATA.InterruptTime` (`0xFFFFF78000000014`) 을 기억** — 상대 시간 (`< 0`) 을 다시 셀 때 쓴다 |
| `+0xb6 ~ +0xcc` | 출력 포인터의 **첫 바이트와 `+0xb7` 바이트를 읽고 씀** (쓰기 가능 확인) |
| `+0xd9 ~ +0xff` | `ObReferenceObjectByHandle(핸들, 1, DbgkDebugObjectType, PreviousMode)` → 실패면 **출력을 안 쓰고** status 돌려줌 (`+0x106 JS → +0x1b7`) |
| **`+0x12c/+0x131`** | **`KeWaitForSingleObject(객체 자체, Executive(0), PreviousMode, Alertable, Timeout)`** |
| `+0x133 ~ +0x13c` | status < 0 이면 바로 끝 |
| `+0x1d5 ~ +0x1e9` | status 가 `0xc0` · `0x101` · `0x102` 면 바로 끝 (이 셋만 "깨움 아님") |
| `+0x1f4 ~ +0x1fd` | 객체 뮤텍스 잡음 · `+0x1fd ~ +0x202` `Flags & 1` (닫는 중) 이면 `+0x2dc EBX = 0xc0000354` |
| `+0x208 ~ +0x20f` | `EventList` 가 비었나 |
| `+0x214 ~ +0x221` | **비었고** 아직 못 골랐으면 **`KeResetEvent(객체)`** (알림 이벤트 끄기) |
| `+0x223 ~ +0x22b` | 한 항목 보기: **`Flags & 5` (READ=1 · INACTIVE=4) 둘 중 하나라도 켜졌으면 건너뜀** (`+0x32b` 다음 항목) |
| `+0x231 ~ +0x25a` | **같은 프로세스 앞선 이벤트 찾기**: 목록 머리부터 이 항목 앞까지 `ClientId.UniqueProcess` (`+0x28`) 가 같은 항목이 있으면 이 항목에 `Flags \|= 4` (INACTIVE) (`+0x24b`), `[+0x50] = 0` (`+0x24f`) 하고 건너뜀 |
| `+0x260 ~ +0x28a` | 고름: 프로세스 · 스레드 (`+0x38` · `+0x40`) 에 참조 하나씩 (`'DbgO'` 태그) → **`DbgkpConvertKernelToUserStateChange(지역, 이벤트)`** |
| `+0x28f` | **`이벤트.Flags \|= 1` (READ — "이미 건넨 것" 표시)** |
| `+0x299 ~ +0x29e` | 뮤텍스 놓음 |
| `+0x2b6/+0x2bb` | **`DbgkpOpenHandles(지역, 프로세스, 스레드)`** (뮤텍스 밖) → 참조 내려놓음 (`+0x2c7`, `+0x2d2`) |
| `+0x145 ~ +0x1ad` | 지역 0xb8 B 를 호출자 출력 버퍼로 복사 (고른 경우 · **시간 끝 · alert · APC 로 끝난 경우도 같이** — 그땐 지역이 0) |
| `+0x2e3 ~ +0x326` | 못 골랐을 때 (이벤트가 전부 READ·INACTIVE) : Timeout ≥ 0 (없음 포함) 이면 다시 `KeWait…` (`→ +0x114`); 상대 시간 (< 0) 이면 `지금 InterruptTime − 아까` 만큼 줄여서 (`+0x304 ~ +0x311`) 아직 < 0 이면 다시 기다림, 아니면 **`EBX = 0x102`** (`+0x321`) |

### 3-3. 출력 구조 칸 표 (`DBGUI_WAIT_STATE_CHANGE`, **0xB8 B**) `[정적]` (`DbgkpConvertKernelToUserStateChange` + `DbgkpOpenHandles`)

공통: `+0x00` **NewState** (u32) · `+0x08` **AppClientId** = 이벤트의 ClientId 16 B (`+0x08` UniqueProcess · `+0x10` UniqueThread) — `ConvertKernelToUserStateChange +0x0/+0x4`. 그 밖 칸은 `NewState` 따라.

| `NewState` | 어떤 이벤트 (ApiNumber) | 채우는 줄 | 칸 (출력 오프셋) | 핸들 (디버거 표에 여는 것) |
|---|---|---|---|---|
| **0** (`DbgIdle`) | 없음 · 시간 끝 `0x102` · alert `0x101` · APC `0xc0` | `NtWaitForDebugEvent +0x4d` memset | **전부 0** | 없음 |
| **2** CreateThread | 1 | `ConvertKernelToUserStateChange +0x127 ~ +0x139` | `+0x18` **HandleToThread** · `+0x20` SubSystemKey (u32) · `+0x28` StartAddress | **스레드 핸들**: `DbgkpOpenHandles +0x91 ~ +0xb5` `ObOpenObjectByPointer(스레드, 속성 0, 접근 `0x12007b`, `PsThreadType`, KernelMode)`; 실패하면 칸 0 (`+0xc6`) |
| **3** CreateProcess | 2 | `+0xf6 ~ +0x125` (0x30 B 복사, 이벤트 안 FileHandle 칸 `[이벤트+0x90]` 은 **0 으로**) | `+0x18` **HandleToProcess** · `+0x20` **HandleToThread** · `+0x28` SubSystemKey (u32) · `+0x30` **FileHandle** · `+0x38` BaseOfImage · `+0x40` DebugInfoFileOffset (u32) · `+0x44` DebugInfoSize (u32) · `+0x48` InitialThread.SubSystemKey · `+0x50` InitialThread.StartAddress | **프로세스 핸들** (접근 **`0x12067b`**, `PsProcessType`, 조각 `FUN_140b39ca6 +0x10 ~ +0x2d`) · **스레드 핸들** (접근 **`0x12007b`**, `+0xd6 ~ +0x105`) · **파일 핸들** = 메시지의 커널 핸들을 `ObDuplicateObject(현재, 핸들, 현재, 칸, 접근 0, 속성 0, 옵션 2 = 같은 접근, 0)` 로 복제 (조각 `+0x73`) 후 **원본 `ObCloseHandle`** (`DbgkpOpenHandles +0x8a`); 복제 실패 시 칸 0 |
| **4** ExitThread | 3 | `+0xb8 ~ +0xc4` | `+0x18` ExitStatus (u32) | 없음 |
| **5** ExitProcess | 4 | 조각 `+0xeaa9e` 후 `+0xb8` 과 합류 | `+0x18` ExitStatus (u32) | 없음 |
| **6** Exception | 0 (코드 ≠ `0x80000003`·`0x80000004`) | `+0x19 ~ +0x9c` (0xa0 B 복사) | `+0x18 ~ +0xb7` `_DBGKM_EXCEPTION64` | 없음 |
| **7** Breakpoint | 0 (코드 `0x80000003`) | `+0x23 ~ +0x2a → +0x9e` | 〃 | 없음 |
| **8** SingleStep | 0 (코드 `0x80000004`) | `+0x31 ~ +0x40` | 〃 | 없음 |
| **9** LoadDll | 5 | 조각 `+0xeaa78 ~ +0xeaa9c` (0x20 B 복사, 이벤트 안 FileHandle `[이벤트+0x88]` 0 으로) | `+0x18` **FileHandle** · `+0x20` BaseOfDll · `+0x28` DebugInfoFileOffset (u32) · `+0x2c` DebugInfoSize (u32) · `+0x30` NamePointer | **파일 핸들 복제** (`DbgkpOpenHandles +0x42 ~ +0x8f`, 같은 방식, 원본 닫음) |
| **10** UnloadDll | 6 | `+0xe3 ~ +0xf0` | `+0x18` BaseAddress (8 B) | 없음 |

- 핸들 열기는 **`NtWaitForDebugEvent` 를 부른 디버거 스레드의 컨텍스트**에서 한다 (현재 프로세스 = 디버거) → 핸들이 디버거의 핸들 표에 생긴다. `ObOpenObjectByPointer` 의 `AccessMode` 는 **0 (KernelMode)** (`[RSP+0x28]` byte 0) → 접근 검사 없이 위 접근을 준다 `[추측: 이 함수 안은 안 읽음]`. 핸들 속성 0 (상속 안 됨 · 유저 핸들).
- `NewState` 이름 (`DbgCreateThreadStateChange` 등) 은 `[추측: 이름]` (ntdll PDB 의 `DBG_STATE` 는 안 봄). **값 → 유저 쪽 이벤트 코드 대응은 아래 3-3b 에서 ntdll `.asm` 으로 확인했다.**
- ApiNumber 7 이상 (에러 보고 등) 은 `ConvertKernelToUserStateChange` 가 아무것도 안 채운다 (NewState 0 으로 남음, `+0xdd ~ +0xe1`).

### 3-3b. 유저 쪽 (ntdll) 이 이 0xB8 구조를 읽는 줄 `[정적]` (`ms/disassemble/ntdll/DbgUi*.asm`)

- `DbgUiWaitStateChange` (`ntdll`): `+0x0 MOV RAX,GS:[0x30]` (TEB) → **`+0x11 MOV RCX,[RAX+0x16a8]`** (= `TEB.DbgSsReserved` (+0x16a0, 16 B) 의 둘째 칸 = 이 스레드의 **디버그 객체 핸들** `[정적] PDB` 칸 · 뜻은 `[추측]`) · **`+0xf MOV DL,1` = Alertable TRUE** · `+0x9 ~ +0xc` 인자 자리 바꿈 → **`+0x18 JMP NtWaitForDebugEvent`** (꼬리호출). 즉 유저 `WaitForDebugEvent` 길은 **항상 Alertable 로 `NtWaitForDebugEvent` 를 부른다** (그래서 `0x101` · `0xc0` 가 실제로 유저까지 올라올 수 있다).
- `DbgUiConvertStateChangeStructure`: `+0x0 XOR R8D,R8D` → `+0x3 JMP DbgUiConvertStateChangeStructureWorker` (R8B = 0. `...Ex` 는 8 B 짜리 따로 있고 R8B 를 다르게 줄 것 `[추측]`).
- `DbgUiConvertStateChangeStructureWorker` (입력 = 0xB8 구조 RCX, 출력 = Win32 `DEBUG_EVENT` RDX):

| 줄 | NewState | 쓰는 `DEBUG_EVENT.dwDebugEventCode` | 읽는 칸 (0xB8 구조 오프셋 → 출력 칸) |
|---|---|---|---|
| `+0x1b ~ +0x24` | 공통 | — | `[+0x08]` UniqueProcess → `dwProcessId` (`+0x4`) · `[+0x10]` UniqueThread → `dwThreadId` (`+0x8`) — **3-3 의 `AppClientId` 칸과 일치** |
| `+0x157` | **2** | 2 (CREATE_THREAD) | `[+0x18]` HandleToThread → `hThread` · `[+0x28]` StartAddress → `lpStartAddress` · `lpThreadLocalBase` = `NtQueryInformationThread(hThread, 0, …, 0x30 B)` 결과 (`+0x183/+0x188`) |
| `+0x22d` | **3** | 3 (CREATE_PROCESS) | `[+0x30]` FileHandle → `hFile` · `[+0x18]` HandleToProcess → `hProcess` · `[+0x20]` HandleToThread → `hThread` · `[+0x38]` BaseOfImage → `lpBaseOfImage` · `[+0x40]` → `dwDebugInfoFileOffset` · `[+0x44]` → `nDebugInfoSize` · `[+0x50]` InitialThread.StartAddress → `lpStartAddress` · `lpThreadLocalBase` (`+0x27d/+0x282` 같은 호출) · `lpImageName` = 0 · `fUnicode` = 1 |
| `+0x21c` / `+0x214` | **4** / **5** | 4 (EXIT_THREAD) / 5 (EXIT_PROCESS) | `[+0x18]` ExitStatus → `dwExitCode` (`+0x222 ~ +0x225`) |
| `+0x6a ~ +0xfc` | **6 · 7 · 8** | 1 (EXCEPTION) — 단 예외 코드가 `0x4001000a` · `0x40010006` 면 8 (OUTPUT_DEBUG_STRING, `+0x1e5` · `+0x1b2`), `0x40010007` 면 9 (RIP, `+0x19c`) | 예외 기록 0x98 B 복사 (`[+0x18 ~ +0xaf]` → 출력 `+0x10 ~`, `+0x9c ~ +0xf1`) · `[+0xb0]` FirstChance → 출력 `+0xa8` (`+0xf6/+0xfc`) — 커널 쪽 `_DBGKM_EXCEPTION64` (0xa0 B = 기록 0x98 + FirstChance) 와 맞음 |
| `+0x125` | **9** | 6 (LOAD_DLL) | `[+0x18]` FileHandle → `hFile` · `[+0x20]` BaseOfDll → `lpBaseOfDll` · `[+0x28]` → `dwDebugInfoFileOffset` · `[+0x2c]` → `nDebugInfoSize` · `[+0x30]` NamePointer → `lpImageName` · `fUnicode` = 1 |
| `+0x1d2` | **10** | 7 (UNLOAD_DLL) | `[+0x18]` BaseAddress → `lpBaseOfDll` |
| `+0x11e` | 그 밖 (0 · 1 · 11 …) | — | `EAX = 0xc0000001` 돌려줌 |

- **3-3 의 칸 표와 ntdll 이 읽는 오프셋이 칸마다 맞는다** (CreateProcess `+0x18 / +0x20 / +0x30 / +0x38 / +0x40 / +0x44 / +0x50`, LoadDll `+0x18 / +0x20 / +0x28 / +0x2c / +0x30`, CreateThread `+0x18 / +0x28`, ExitThread · ExitProcess `+0x18`) — 커널 쪽 (`DbgkpConvertKernelToUserStateChange`) 과 유저 쪽을 따로 읽어 서로 대조한 셈이다.
- (이름 / 코드) 이벤트 코드 번호의 이름 (`CREATE_PROCESS_DEBUG_EVENT` 등) 은 Win32 공개 이름 `[추측: 이름]`, 번호는 asm 의 상수.

### 3-4. `NtDebugContinue` 줄 지도 `[정적]`

| 줄 | 하는 일 |
|---|---|
| `+0x23 ~ +0x4f` | `PreviousMode` · 유저면 ClientId 포인터를 읽어 봄 · **16 B 를 지역에 복사** (`[RSP+0x40]` UniqueProcess · `[+0x48]` UniqueThread) |
| **`+0x5c`** | `ContinueStatus == 0x10002` 면 통과 |
| **`+0x19c ~ +0x1d2`** | 아니면 `0x80010001` · `0x10001` · `0x40010001` · `0x40010003` · `0x40010004` 중 하나면 통과 |
| `+0x1d8` | 그 밖 → **`0xc000000d`** (**핸들을 열기 전에** 거절) |
| `+0x90/+0x95` | `ObReferenceObjectByHandle(핸들, 1, DbgkDebugObjectType, PreviousMode)` — 실패 status 그대로 |
| `+0xb0 ~ +0xb5` | 객체 뮤텍스 |
| `+0xcb` | 항목의 `ClientId.UniqueProcess` (`+0x28`) 가 같은가 |
| **`+0xda ~ +0xe9`** | 아직 못 찾았으면: **`UniqueThread` (`+0x30`) 도 같고 `Flags & 1` (READ) 가 켜졌는가** — 아니면 다음 항목 |
| `+0xef ~ +0x17c` | **목록에서 뺌** (링크 검사 실패 시 `INT 0x29`) · 찾음 표시 (`R15B = 1`, `RSI = 이벤트`) |
| `+0xd5 ~ +0xd8` · `+0x108 ~ +0x119` | 이미 찾은 뒤 **같은 프로세스의 다음 항목을 만나면**: `Flags &= ~4` (INACTIVE 풀기) → `KeSetEvent(객체, 0, 0)` (EventsPresent 켜기) → **거기서 끝** (딱 하나만 풂) |
| `+0x119 ~ +0x125` | 뮤텍스 놓음 · 객체 참조 내려놓음 |
| `+0x12a ~ +0x12d`, `+0x16b` | **못 찾았으면 `EDI = 0xc000000d`** |
| `+0x12f ~ +0x139`, `+0x187 ~ +0x19a` | ETW 추적 (`PerfGlobalGroupMask & 0x400000`) 일 때 `EtwTraceDebuggerEvent` |
| **`+0x13b/+0x141`** | **`[이벤트+0x84] = ContinueStatus`** (= 메시지 `ReturnedStatus`) · **`[이벤트+0x48] = 0`** (Status = 성공) |
| **`+0x148/+0x14d`** | **`DbgkpWakeTarget(이벤트)`** → 반환 `EAX = EDI` (0) |

### 3-5. status 표 `[정적]` 값 · `[추측: 이름]` 이름

| 함수 | status | 언제 (줄) |
|---|---|---|
| `NtWaitForDebugEvent` | 0 | 이벤트를 골라 출력함 (`+0x2a6 ~ +0x2d7`) |
| 〃 | `0x102` (시간 끝) | 상대 시간이 다 지남 `+0x321` · 시간 0 이면 첫 `KeWait…` 가 돌려줌 (`+0x1d5 ~ +0x1e9`) — 출력 0 으로 채움 |
| 〃 | `0x101` (alert) · `0xc0` (유저 APC) | `KeWait…` 가 돌려줌, Alertable 일 때만 — 출력 0 으로 채움 (`+0x1d5 ~ +0x1da`) |
| 〃 | `0xc0000354` (디버거 비활성) | 객체 `Flags & 1` (`+0x2dc`) |
| 〃 | `ObReferenceObjectByHandle` 거절들 | 잘못된 핸들 · 형식 다름 · 접근 모자람 — **출력을 안 씀** |
| 〃 | 접근 위반 등 | 유저 모드면 SEH 필터 `$filt$0/$filt$1` 이 `PreviousMode ≠ 0` 일 때만 잡음 → 예외 코드 돌려줌 (`NtWaitForDebugEvent$filt$0 +0x1a ~ +0x2a`) `[추측: 코드는 0xc0000005 꼴]` |
| `NtDebugContinue` | 0 | 찾고 깨움 (`+0x14d`) |
| 〃 | `0xc000000d` | ContinueStatus 가 여섯 값이 아님 (`+0x1d8`) **또는** READ 가 켜진 그 ClientId 이벤트가 없음 (`+0x16b`) — **같은 코드라 구분 못 함** |
| 〃 | `ObReferenceObjectByHandle` 거절들 | `+0x99 JS` |
| `DbgkpQueueMessage` (보낸 스레드가 받는 값) | `0xc0000353` (포트 안 정해짐) | `DebugPort == 0` 이거나 `SkipCreationMsg`/`SkipTerminationMsg` 로 건너뜀 (`+0x15b`) |
| 〃 | `0xc0000354` | 객체 `Flags & 1` (`+0x2dc`) · **디버거가 객체를 닫아 깨웠을 때** (`DbgkpCloseObject +0xdf`) |
| 〃 | `0xc000009a` | NOWAIT 이벤트 풀 할당 실패 (`+0x2a4`) — 스택 이벤트 길에서는 안 남 |

---

## 2. `DbgkpSendApiMessage` · `DbgkpPostModuleMessages`

### 2-1. `DbgkpSendApiMessage(프로세스, Flags, 메시지)` `[정적]`

| 줄 | 하는 일 |
|---|---|
| `+0x1d ~ +0x39` | 보내는 스레드 = 현재 스레드 · ETW (`PerfGlobalGroupMask & 0x400000`) 면 `EtwTraceDebuggerEvent` (`+0xe3`) |
| `+0x3f ~ +0x45` | `EBX = (Flags & 2) << 5` → **큐에 넘기는 `Flags` 는 `0` 또는 `0x40`** |
| `+0x53 ~ +0x6a` | `프로세스 == 현재 스레드가 속한 프로세스` **이고 `Flags & 1`** 이면 **`DbgkpSuspendProcess`** (`+0x65/+0x6a`) → `EBP` = 성공 여부. (`DbgkpSuspendProcess`: `KernelApcDisable--` (임계 영역) → `PsFreezeProcess` → 실패하면 임계 영역 나옴) |
| **`+0x79`** | **`[메시지+0x2c] = 0x103`** |
| `+0x84 ~ +0x8c` | **`DbgkpQueueMessage(프로세스, 현재 스레드, 메시지, 0 또는 0x40, 0)`** (5번째 = 디버그 객체 0 → 큐가 `프로세스.DebugPort` 를 씀) |
| `+0x90 ~ +0x9a` | 얼렸었으면 **`DbgkpResumeProcess`** (`PsThawMultiProcess` → `KeLeaveCriticalRegion`) |
| `+0x9c ~ +0xa6` | status ≥ 0 이고 **`[메시지+0x2c] == 0x40010001`** 이면 `+0x48` 로 되돌아가 **같은 메시지 다시 보냄** |
| 반환 | `DbgkpQueueMessage` 의 status (`ESI`) |

`Flags` 부르는 쪽 정리: `DbgkCreateThread` CreateProcess **0** · CreateThread **1** · `DbgkSendSystemDllMessages` **3** · `DbgkPostModuleMessage` (객체 없을 때) **3** · `DbgkMapViewOfSection` **1**. `DbgkpQueueMessage` 의 `Flags & 0x40` 은 **LoadDll (api 5) 일 때 보낸 스레드의 `SkipCreationMsg` 와 맞물려 건너뛰게** 한다 (`+0x2bc ~ +0x2c9`).

### 2-2. 큐 항목 (`DEBUG_EVENT`, **0x168 B**) 칸 표 `[정적]` 오프셋 · `[추측: 이름]`

크기 근거: `DbgkpQueueMessage +0x3a ~ +0x4c` `memset(스택 +0x40, 0, 0x168)` · 풀 할당 `+0x6c ~ +0x73` (크기 0x168, 태그 `'DbgE'` = `0x45676244`).

| 칸 | 크기 | 이름 | 쓰는 줄 · 뜻 |
|---|---|---|---|
| `+0x00` | 0x10 | `EventList` (LIST_ENTRY) | 큐에 끼울 때 (`+0x162 ~ +0x16c`) |
| `+0x10` | 0x18 | `ContinueEvent` (KEVENT) | **동기 이벤트 (SynchronizationEvent), 꺼진 채** (`+0x29a KeInitializeEvent(.., 1, 0)` — `LEA EDX,[R8+1]`). 보낸 스레드가 기다리는 것 |
| `+0x28` | 0x10 | `ClientId` | 스레드 `Cid` (`ETHREAD +0x508`) 복사 (`+0x11d/+0x125`). `NtWaitForDebugEvent` 가 `+0x28` 으로 같은 프로세스 비교 · `NtDebugContinue` 가 `+0x28`/`+0x30` 으로 찾음 |
| `+0x38` | 8 | `Process` | 보낸 프로세스 (NOWAIT 길에서만 참조 하나 더 `'DbgO'`, `+0x86 ~ +0xa0`) |
| `+0x40` | 8 | `Thread` | 보낸 스레드 |
| `+0x48` | 4 | `Status` | 깨울 때 쓰는 값 (`NtDebugContinue +0x141` = 0 · `DbgkpCloseObject +0xdf` = `0xc0000354`) |
| `+0x4c` | 4 | `Flags` | **1 READ** (건넴, `NtWaitForDebugEvent +0x28f`) · **2 NOWAIT** (풀 이벤트, 기다리는 스레드 없음) · **4 INACTIVE** (같은 프로세스 앞선 것이 안 이어져 보류) · **8 RELEASE** (`DbgkpWakeTarget +0x58 ~ +0x64` 가 스레드 `RundownProtect +0x588` 놓음) · **0x20 SUSPEND** (`+0x1d` 가 `PsMultiResumeThread`) · `0x40` (스택 이벤트에 `DbgkpSendApiMessage` 가 넘긴 값, 읽는 줄 못 찾음) `[공백]` |
| `+0x50` | 8 | `BackoutThread` | NOWAIT 길에서 현재 스레드 (`+0xa5 ~ +0xae`), `NtWaitForDebugEvent +0x24f` 가 0 으로 |
| `+0x58` | 0x110 | `ApiMsg` | 메시지 복사 (0x80 × 2 + 0x10). 안 `+0x80` = ApiNumber · `+0x84` = ReturnedStatus · `+0x88` = 공용체 |

두 길 (**`DbgkpQueueMessage` 5번째 `Flags & 2`**):
- **기다림 길 (`Flags & 2 == 0`)** — `DbgkpSendApiMessage` 가 부르는 길: 이벤트가 **보낸 스레드의 커널 스택** (`RSP+0x40`) 에 있음. 큐 끼운 뒤 그 스레드가 기다림. 항상 객체 = `프로세스.DebugPort` (`+0x270`).
- **NOWAIT 길 (`Flags & 2`)** — attach · 모듈 보내기에서 (`DbgkPostModuleMessage +0x112 ~ +0x120` 가 `Flags 2` + 객체 인자): 풀에서 할당 · `Flags = 인자 | 4` (INACTIVE 로 시작) · 큐에만 끼우고 `KeSetEvent` 도 **안 함** (`+0x170 CMP [RSP+0x30],EBP ; JNZ`) · 안 기다림. 풀리는 곳은 attach 쪽 `DbgkpSetProcessDebugObject` `[공백: 안 읽음]`.

### 2-3. `DbgkpQueueMessage` 줄 지도 (기다림 길) `[정적]`

| 줄 | 하는 일 |
|---|---|
| `+0x253 ~ +0x266` | `Flags` 를 이벤트 `+0x4c` 에 (`+0x25a`) · **전역 `DbgkpProcessDebugPortMutex` 잡음** |
| `+0x270` | `디버그 객체 = [프로세스+0x308]` (**`EPROCESS.DebugPort`**) |
| `+0x277 ~ +0x2d6` | ApiNumber 별 걸러내기: **1 · 2 → 스레드 `CrossThreadFlags & 0x40` (`SkipCreationMsg`) 이면 객체 = 0** (`+0x2ae`) · **5 → `스레드플래그 & Flags & 0x40`** (`+0x2bc ~ +0x2c9`) · **3 · 4 → `& 0x80` (`SkipTerminationMsg`)** (`+0x2ce`) |
| `+0x28e ~ +0x29f` | 이벤트 `ContinueEvent` 초기화 → `+0xb2` 로 합류 |
| `+0xb2 ~ +0x125` | `프로세스 · 스레드` 칸 · 메시지 복사 · ClientId 복사 |
| `+0x12b ~ +0x15b` | **객체가 0 이면 `EDI = 0xc0000353`** |
| `+0x130 ~ +0x141` | 객체 뮤텍스 · **`Flags & 1` 이면 `0xc0000354`** (`+0x2dc`) |
| **`+0x147 ~ +0x16c`** | **`EventList` 꼬리에 끼움** (`+0x162 [이벤트]=머리 · +0x165 [이벤트+8]=옛꼬리 · +0x169 · +0x16c`; 링크 검사 실패 시 `INT 0x29`) |
| **`+0x17e/+0x183`** | **`KeSetEvent(객체 = EventsPresent, 0, 0)`** (기다림 길에서만) |
| `+0x188 ~ +0x18d` | 객체 뮤텍스 놓음 |
| `+0x1cf ~ +0x1db` | 전역 뮤텍스 놓음 — 객체 뮤텍스를 먼저 놓고 전역을 나중에 놓는다 |
| **`+0x1df ~ +0x1f0`** | **`KeWaitForSingleObject(이벤트+0x10, Executive 0, KernelMode 0, Alertable 0, Timeout NULL = 무한)`** (call `+0x1f0`, ret `+0x1f5`) |
| `+0x1f5` | `EDI = 이벤트.Status` (`+0x48`) |
| `+0x1fe ~ +0x24e` | **이벤트 안 메시지 (`+0x58`, 0x110 B) 를 보낸 쪽 메시지로 되복사** |
| `+0x1a1` | 반환 `EDI` |

보낸 스레드는 **커널 모드 · 비경보 · 무한 기다림**이라 유저 APC · alert 로는 깨어나지 않는다. 깨우는 길은 `DbgkpWakeTarget` 의 `KeSetEvent` 하나뿐.

### 2-4. 기다림 / 깨움 표 `[정적]`

| 누가 | 어디서 기다리나 (줄) | 무엇을 | 누가 깨우나 (줄) |
|---|---|---|---|
| 보낸 스레드 (이벤트 보낸 쪽) | `DbgkpQueueMessage +0x1f0/+0x1f5` | 자기 스택 이벤트의 `ContinueEvent` (동기 이벤트), Executive · 커널 모드 · 무한 | **`DbgkpWakeTarget +0x4c ~ +0x56` `KeSetEvent(이벤트+0x10)`** ← `NtDebugContinue +0x148/+0x14d` (status 0 · `ReturnedStatus` = ContinueStatus) · `DbgkpCloseObject +0xe6/+0xeb` (status `0xc0000354`) |
| 디버거 스레드 (`NtWaitForDebugEvent`) | `+0x12c/+0x131` | **디버그 객체 자체 (`EventsPresent` 알림 이벤트)**, 호출자 모드 · Alertable · Timeout | 큐에 끼운 쪽 `DbgkpQueueMessage +0x17e` · `NtDebugContinue +0x114` (다음 이벤트 풀 때) · `DbgkpCloseObject +0x52` (객체 닫을 때) |
| 같은 프로세스의 **다른 스레드들** | (기다림이 아니라 **얼림**) `DbgkpSuspendProcess +0x19` `PsFreezeProcess` | `Flags & 1` 인 보냄 (`DbgkCreateThread` 스레드 길 · `DbgkMapViewOfSection` · LoadDll 들) | `DbgkpResumeProcess +0xa/+0xf` `PsThawMultiProcess` (`DbgkpSendApiMessage +0x95/+0x9a` 가 부름) |
| 보낸 스레드 자신 | 임계 영역 (`KernelApcDisable--`) 안에서 기다림 (얼렸을 때만) | | `DbgkpResumeProcess +0xf` `KeLeaveCriticalRegion` |

- `DbgkpWakeTarget` 안 (`+0x14 ~ +0x56`): `Flags & 0x20` → `PsMultiResumeThread` (`+0x1d ~ +0x26`) · `Flags & 8` → `ExReleaseRundownProtection` (`+0x58 ~ +0x64`) · **`Flags & 2` (NOWAIT) → `DbgkpFreeDebugEvent`** (파일 핸들 닫고 · 프로세스 · 스레드 참조 놓고 · 풀 해제) · 아니면 `KeSetEvent`.
- `DbgkpFreeDebugEvent +0x6 ~ +0x2c`: ApiNumber 2 면 `[이벤트+0x90]` (FileHandle), 5 면 `[이벤트+0x88]` 이 0 이 아닐 때 `ObCloseHandle`.
- **PsFreezeProcess 가 호출 스레드 자신은 얼리지 않는다는 것은 안 읽음** `[추측]`.
- **디버거가 객체를 닫을 때** (`DbgkpCloseObject`, `R9 ≤ 1` 일 때만 = 프로세스 안 마지막 핸들 `[추측]`): `Flags |= 1` (`+0x2f`) → 목록을 통째로 떼어 비움 (`+0x33 ~ +0x42`) → `KeSetEvent` (`+0x52`) → **모든 프로세스를 훑어 `DebugPort == 이 객체` 이면 칸을 0 으로** (`+0xc9`) · `DbgkpMarkProcessPeb` (BeingDebugged 0, 조각 `+0x18`) · `객체 Flags & 2` (KillOnClose) 이면 `PsTerminateProcess(프로세스, 0xc0000354)` (조각 `+0x2a`) · 떼어낸 목록의 모든 이벤트에 `Status = 0xc0000354` + `DbgkpWakeTarget` (`+0xdf ~ +0xe6`).

### 2-5. `DbgkpPostModuleMessages` · `DbgkPostModuleMessage` `[정적]`

| 줄 | 하는 일 |
|---|---|
| `DbgkpPostModuleMessages +0x2f ~ +0x36` | `KPROCESS.SecureState` (+0x170) bit 0 `[추측: 보안 프로세스 표시]` 이면 `VslSendDebugAttachNotifications` 만 하고 끝 (`+0x2e6`) |
| `+0x3c ~ +0x54` | `PEB.Ldr` (`EPROCESS.Peb +0x2e0` → `[+0x18]`) 의 `InLoadOrderModuleList` (`Ldr +0x10`) 를 걸어감 (유저 메모리를 읽어 봄, 순환 검사 포함) |
| `+0xa5 ~ +0xa8` | 처음 둘 건너뜀 `[추측: exe · ntdll]` |
| `+0xb4 ~ +0xf6` | 나머지 모듈마다 `RtlImageNtHeader(DllBase [+0x30])` → `DbgkPostModuleMessage(프로세스, 스레드, DllBase, DebugInfoFileOffset, DebugInfoSize, 객체)` |
| `+0x173 ~ +0x2c0` | WoW64 (`+0x190 ~ +0x1b7`, `Machine` 0x14c · 0x1c4) 면 PEB32 의 로더 목록도 같은 방식 · 엔클레이브 모듈 (`PsGetProcessEnclaveModuleInfo`, 조각 `DbgkPostEnclaveModuleMessages`) |
| `DbgkPostModuleMessage +0x50 ~ +0x91` | 지역 0x110 B 메시지: `[+0x28]=5` (LoadDll) · `[+0x38]=BaseOfDll` · `[+0x40]=DebugInfoFileOffset` · `[+0x44]=DebugInfoSize` · `MmGetFileNameForAddress` 로 경로 → `ZwOpenFile` (접근 `0x80100000`, 속성 `0x640`, 공유 7, 옵션 0x20) → `[+0x30]=FileHandle`. **머리 길이 칸 (+0x00 ~ +0x07) 은 안 채움**, NamePointer 도 0 |
| `+0x10d ~ +0x125` | **객체 인자가 있으면 `DbgkpQueueMessage(프로세스, 스레드, 메시지, Flags 2 = NOWAIT, 객체)`** (attach 길) · 없으면 `+0x149/+0x14e DbgkpSendApiMessage(프로세스, 3, …)` (**기다림 길** — `DbgkCreateThread +0x338` 가 부를 때) |
| `+0x153 ~ +0x163` | 큐 실패 또는 보낸 뒤 파일 핸들이 남았으면 `ObCloseHandle` |

- 정리: **test.exe 를 디버그 속성으로 지은 자식의 첫 스레드 길에서는 `DbgkpPostModuleMessages` 를 안 탄다** (`ClonedThread` 가 꺼져 있다 `[추측: 새 프로세스의 첫 스레드는 클론이 아님 — NtCreateUserProcess 의 스레드 플래그는 이 요청에서 안 봄]`). 이 함수는 (가) 클론 스레드, (나) attach (`NtDebugActiveProcess` 쪽 — 이번 요청 밖) 에서 쓰인다.

---

## PE_emulator 쪽 — stub 이 해야 할 최소

### 나누기

**엔진 몫 (자식 첫 스레드 — `Src/nt/` 레 자식 짓기 · 첫 스레드 알림 자리)**
1. 속성 `0x60001` 받기: **크기 8 아니면 `0xc000000d`** (`PspBuildCreateProcessContext +0x17a3 ~ +0x1cd`), 값은 **열지 말고 저장만**.
2. 짓는 순서 끝, **`PspInsertProcess` 에 해당하는 자리**에서 (`NtCreateUserProcess +0xfbd`):
   - 핸들 ≠ 0 이면 접근 **2** 로 디버그 객체 참조 (실패 status 는 `ObReferenceObjectByHandle` 쪽 — 팀원 `reference_debug_object(…, 2)` 와 같다).
   - 핸들 = 0 이면 만든 쪽 프로세스 `DebugPort ≠ 0` 이고 그쪽 `Flags.NoDebugInherit == 0` 일 때 **같은 객체를 물려받음**.
   - 객체 `Flags & 1` (닫는 중) 이면 **붙이지 않고 성공**. `Flags & 4` 면 자식이 32비트 WoW (x86/ARM32) 아닐 때 `0xc00000bb`.
   - 붙이면: 자식 `DebugPort = 객체` · `PEB.BeingDebugged = 1` (자식 PEB 가 있으면; WoW64 면 PEB32 도) · 4번째 인자 bit 1 (kernelbase `DEBUG_ONLY_THIS_PROCESS` 쪽) 이면 자식 `NoDebugInherit = 1`.
3. **첫 스레드가 처음 돌 자리** (`PspUserThreadStartup +0x9c`, 유저 코드가 한 줄도 돌기 전): `DebugPort ≠ 0` 이면 이벤트 **① CreateProcess → ② LoadDll(ntdll) → (WoW64 만 ③ LoadDll(ntdll32))** 를 **차례로** 큐에 넣고, **하나 넣을 때마다 그 스레드를 멈춰 둔다** (답이 와야 다음 이벤트를 넣는다 — 진짜는 보낸 스레드가 막혀서 다음 이벤트가 안 생긴다). 메시지 `ReturnedStatus == 0x40010001` 로 이어 주면 **같은 이벤트를 다시** 넣는다. 반환 status · 그 밖 `ReturnedStatus` 는 안 본다.
   - ① 칸: ClientId = {자식 PID, 첫 TID} · FileHandle = (이미지 파일 커널 핸들, 실패 시 0) · BaseOfImage = `SectionBaseAddress` · DebugInfo = PE `PointerToSymbolTable` · `NumberOfSymbols` · **StartAddress = ImageBase + AddressOfEntryPoint** (32비트 WoW 면 ImageBase32 + EntryRVA).
   - ② 칸: FileHandle = ntdll 파일 · BaseOfDll = 자식에 올라간 ntdll 기준 주소 · NamePointer = 자식 TEB `+0x28` 칸의 주소 (가상 자식이 TEB 를 안 가지면 이름을 못 읽는다 — 이 값만 `[공백: 가상 자식이 TEB 를 갖는지]`).
   - 진짜는 첫 스레드가 `ResumeThread` 뒤 처음 스케줄될 때 보낸다 (`PspUserThreadStartup`). 가상 자식이 안 돈다면: 부모가 `NtWaitForDebugEvent` 를 **처음 부를 때** 이 두 이벤트가 이미 큐에 있어야 진짜와 같다 (CreateProcessA 가 돌아온 뒤 곧 이벤트가 오는 모양).
4. 이후 (두 이벤트가 이어진 뒤): 진짜 자식은 유저로 가서 LoadDll 들 · 시작 브레이크포인트 예외 · 스레드 · 종료 이벤트를 더 보낸다 (1-3 끝 문단). 이 stub 이 그 뒤를 안 만들면 **"자식이 안 돎 / 이벤트 더는 안 옴"** 을 정직하게 `stop("…")` 로 남기는 편이 맞다.

**팀원 몫 (큐 · 기다림 — `nt_debug_more.cpp`)**
- 객체: `Flags` bit 0 닫는 중 · **bit 1 KillOnClose** · bit 2 WoW 만. (**주의**: 지금 `nt_set_information_debug_object` 는 `flags` 의 **bit 0** 을 켠다 (`nt_debug_more.cpp:71`, `debug.hpp` 주석 "DEBUG_KILL_ON_CLOSE(1)" = 사용자 쪽 값을 그대로 둔 내부 표현). 진짜 객체는 **bit 1 (0x2)** 이고 bit 0 은 "닫는 중" 이라 뜻이 다르다. 내부 표현이라 지금은 문제 없지만, "닫는 중" 을 새로 만들 때는 `flags` 에 섞지 말고 따로 칸을 두기.) 만들 때 `Flags & ~1 → 0xc000000d`, `만든 프로세스가 32비트 WoW` 면 bit 2.
- 이벤트 칸: `Flags` (READ 1 · NOWAIT 2 · INACTIVE 4) · ClientId · ApiNumber · ReturnedStatus · 공용체 (1-5).
- `NtWaitForDebugEvent` (`:94` 의 `attached_pids.empty()` 가지를 이것으로 바꿈): 위 3-2 규칙.
  1. 큐를 앞에서부터: `Flags & 5` 면 건너뜀 · 앞에 같은 PID 가 있으면 `Flags |= 4` 하고 건너뜀 · 고르면 `Flags |= 1`.
  2. 고른 이벤트를 0xB8 B 로 바꿔 쓰고 (3-3), **디버거 핸들 표에** 핸들을 연다: CreateProcess = 프로세스 (접근 `0x12067b`) · 스레드 (`0x12007b`) · 파일 (복제), LoadDll = 파일 (복제), CreateThread = 스레드 (`0x12007b`).
  3. (참고) **유저 `WaitForDebugEvent` 길은 `Alertable = 1` 로 부른다** (`ntdll!DbgUiWaitStateChange +0xf`, 3-3b) — 팀원의 `alertable` 가지 (`user_alerted` · APC) 가 실제로 타는 길이다.
  4. 못 고르면: 큐에 "읽히지 않은 이벤트" 가 없으니 `EventsPresent` 를 끄고 (`KeResetEvent`) 기다림 — **시간 0 → `0x102`, 그 밖은 지금처럼 "기다림 모델 없음" 으로 멈추되 `0x101`/`0xc0` 도 출력 0 으로 채운다.**
- `NtDebugContinue` (`:119` 의 `return err(invalid)` 자리): ClientId 의 PID · TID 가 같고 **READ 가 켜진** 이벤트를 큐에서 빼고 → 보낸 쪽(엔진)에 `ReturnedStatus` = ContinueStatus 를 돌려주며 깨움 → 같은 PID 의 **다음 이벤트 하나** 의 INACTIVE 를 풀고 `EventsPresent` 를 켬. 못 찾으면 `0xc000000d`.
- 객체의 마지막 핸들 닫기: 모든 대기 이벤트를 `Status = 0xc0000354` 로 깨우고, 붙은 프로세스는 `DebugPort = 0` + `PEB.BeingDebugged = 0`, KillOnClose 면 프로세스 끝냄 (status `0xc0000354`).

**쓰는 곳 세 자리**
- `src/process/nt_create_user_process.cpp:169` (모르는 속성 가지): `0x60001` 가지를 앞에 따로 둔다 (크기 8 아니면 invalid, 값 저장). 진짜 해석은 짓기 끝자리 (`PspInsertProcess` 자리) 에서.
- `exception/nt_debug_more.cpp:94` : "붙은 프로세스가 있으면 멈춤" → 위 큐 규칙.
- `exception/nt_debug_more.cpp:119` : 항상 invalid → 위 이어주기 규칙.

### "기다림 가짜 성공" (D155) 이 진짜와 어떻게 다른가 — 한 줄

진짜는 **status 0 이면 반드시 `NewState ≥ 2` 이고 ClientId · 핸들이 차 있고, 그 이벤트는 `NtDebugContinue` 가 올 때까지 같은 프로세스의 다음 이벤트를 막는다**; 출력이 0 으로 채워진 "빈" 구조는 `0x102`·`0x101`·`0xc0` 일 때만 나온다 — 그래서 "status 0 + 0 으로 채운 구조" 같은 가짜 성공은 디버거 루프에서 진짜에 없는 조합이다 (그리고 `NtDebugContinue` 는 그 가짜 이벤트의 ClientId 를 못 찾아 `0xc000000d`).

### 팀원 주석이 맞나

| 주석 | 판정 | 근거 |
|---|---|---|
| 출력 구조 `0xB8` B | **맞다** | `NtWaitForDebugEvent +0x4d` `memset(…, 0xb8)` · `+0xc2 ~ +0xcc` 첫 B · `+0xb7` B 쓰기 확인 · `+0x145 ~ +0x1ad` 0xb8 B 복사. 구성: 머리 0x18 + `_DBGKM_EXCEPTION64` 0xa0 (PDB `sizeof 0xa0`) |
| `ContinueStatus` 여섯 값 `{0x10002, 0x80010001, 0x10001, 0x40010001, 0x40010003, 0x40010004}` · 그 밖 `0xc000000d` | **맞다 (순서 · 값 · 거절 시점 모두)** | `NtDebugContinue +0x5c`, `+0x19c ~ +0x1d8`; 거절은 **핸들 참조 앞** (`+0x90`) |
| `NtDebugContinue` 순서 (ClientId 읽기 → 상태 → 객체 참조) | **맞다** | `+0x4a` · `+0x5c` · `+0x90` |
| "출력 구조는 시간 초과일 때도 0 으로 채워 써 준다" | **맞다** (+ `0x101` · `0xc0` 도) | `+0x1d5 ~ +0x1e9` 가 셋 다 `+0x13c` 로 보내고, 거기서 지역 0 구조를 복사 |
| "핸들은 `DEBUG_READ_EVENT(1)`" | **맞다** | `+0xf7 MOV EDX,1` · `NtDebugContinue +0x8b` |
| "ClientId 로 전달된 이벤트를 찾고 없으면 invalid" | **맞다** (더: 찾으려면 READ 가 켜져 있어야 함) | `+0xda ~ +0xe9` |
| `NtSetInformationDebugObject` `flags` bit 0 (`debug.hpp` 주석 "DEBUG_KILL_ON_CLOSE(1)") | 사용자 쪽 값으로는 맞다 (info 값 0 · 1). **진짜 객체 칸은 bit 1** | `NtSetInformationDebugObject +0x104 ~ +0x116` (`& 0xfffffffd` · `\| 2`) |
| `NtWaitForDebugEvent` 가 객체에 붙은 프로세스가 있으면 큐가 비었다고 말 못 함 | **맞다** (진짜는 붙는 순간 큐가 이미 차 있다) | 1-3 |

---

## 정적으로 못 닫은 줄 `[추측]` · `[공백]`

- `[공백]` 자식 첫 스레드가 **언제 처음 스케줄되나** — kernelbase 가 `NtResumeThread` 를 부르는 줄 · `NtCreateUserProcess` 에 `CREATE_SUSPENDED` 가 켜지는지는 이번에 안 봄. `PspUserThreadStartup` 이 이벤트를 만드는 것만 확실.
- `[공백]` `ClonedThread` (`SameThreadPassiveFlags` bit 4) 를 **켜는 곳** 은 안 찾음. 새 프로세스의 첫 스레드에서 꺼져 있다는 것은 `[추측]`.
- `[공백]` NOWAIT 이벤트의 **INACTIVE 를 처음 푸는 곳** (attach 쪽 `DbgkpSetProcessDebugObject`) · `Flags 0x10` · `0x40` (이벤트 안) 을 읽는 곳.
- `[추측]` `PsFreezeProcess` 가 호출 스레드를 안 얼리는 것 · `DbgkpCloseObject` 가 객체 형식의 닫기 함수이고 `R9` 가 프로세스 핸들 수라는 것 (`DbgkInitialize` · `DbgkpInitializePhase0` 안 읽음) · `ObOpenObjectByPointer` 의 KernelMode 접근 검사 건너뜀.
- `[추측: 이름]` `NewState` · `ApiNumber` · `STATUS_*` · `DEBUG_*` 이름들은 공개 문서 관례. 값은 모두 asm 에서 읽음.
- `[공백]` 가상 자식에 TEB · 로더 이름 칸이 있어야 LoadDll `NamePointer` 가 의미를 가지는지 (엔진 설계).

---

## VM 으로 확인할 것 — 디버거 필요

시험 exe 하나 (**새 동적 후보**): 타깃 VM 에서 `CreateProcessA(자식 exe, DEBUG_PROCESS = 1)` 로 자식 하나를 띄우고 `WaitForDebugEvent` 루프 몇 번만 돔. 멈춤점 (모두 테스트 exe 일 때만 멈추게 조건은 dast 에서):

| 멈춤점 | 찍을 값 | 확인하는 것 |
|---|---|---|
| `nt!PspInsertProcess+0x2f3` → `+0x2f8` | 입구 `rcx` (핸들) · `edx` (2) · 반환 뒤 `[rbp-0x30]` (객체) | 속성 핸들이 이 길로 오는지 · 접근 2 |
| `nt!PspInsertProcess+0x13e` | `r8` (객체) · `rdx` (만든 쪽 프로세스) · `rcx` (자식) | 객체가 그대로 넘어가는지 · 반환 뒤 자식 `dt nt!_EPROCESS <자식> DebugPort Peb` · `!peb` 의 `BeingDebugged` |
| `nt!DbgkpMarkProcessPeb+0x84` | `rax` (PEB) · `cl` | `BeingDebugged` 가 이 줄에서 1 로 쓰이는지 |
| `nt!PspUserThreadStartup+0x9c` | `!thread` · `!process` | 이벤트를 보내는 스레드가 자식 첫 스레드인지 · 유저 코드가 한 줄도 돌기 전인지 |
| `nt!DbgkpSendApiMessage` 입구 | `rcx` (프로세스) · `edx` (Flags) · `dwo(@r8+0x28)` (ApiNumber) · `dd @r8 L0x44` | **이벤트 차례** (2 → 5 → …) · `Flags` (0 · 3 · 1) · 머리 `0x600038 · 8` |
| `nt!DbgkpQueueMessage+0x1f0` | `r14` 를 시작으로 `dq @r14 L0x2d` (이벤트 0x168) · `!thread` (보낸 스레드) | 스택 이벤트 칸 (`+0x28` ClientId · `+0x4c` Flags) · 보낸 스레드의 wait 상태 (Executive) · 스택에 `DbgkpQueueMessage` |
| `nt!NtWaitForDebugEvent+0x2bb` (`DbgkpOpenHandles` 돌아온 직후) | `dq @rsp+0x60 L0x17` (0xb8 B) · `!handle` | **출력 구조 실제 바이트** · 핸들 값 · **접근 권한 `0x12067b` / `0x12007b`** · 파일 핸들 복제본 |
| `nt!DbgkpWakeTarget+0x51` | `rbx` (이벤트) · `dwo(@rbx+0x4c)` · `qwo(@rbx+0x28)` | 깨우는 이벤트 · 호출 스택 (`NtDebugContinue +0x14d` 인지) |
| `nt!NtDebugContinue+0x13b` | `ebx` (ContinueStatus) · `rsi` (이벤트) | 유저 `ContinueDebugEvent` 가 어떤 값을 넘기나 (`DBG_CONTINUE` 등) |

- 확인 못 하는 것: 위 asm 줄들은 정적이라 실행 흐름 (예: 첫 스레드가 정말 `PspUserThreadStartup` 에서 보내나, `+0x2c4 HideFromDebugger` 를 어떤 값으로 보는가) 만 VM 이 닫는다.
- 조각 줄 (`DbgkCopyProcessDebugPort` 조각 `+0x2c89e1` · `+0x2c8a0c`) 은 WinDbg 가 그 이름으로 보여 줄지 모른다 `[추측]` → 필요하면 절대 주소 `nt+0xb0d995` (DebugPort 쓰기, `0x140b0d995`) · `nt+0xb0d9c0` (DbgkpMarkProcessPeb 부르기) 로.

## VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것

디버거 없이 **유저 모드 시험 exe 만으로** (Win32 디버그 API + `ntdll` 내보내기) 뜨는 것:
- `WaitForDebugEvent` 로 받은 **이벤트 코드 차례** (CREATE_PROCESS → LOAD_DLL (ntdll) → … → 시작 브레이크포인트 EXCEPTION …) · 각 `dwProcessId` · `dwThreadId` · `CREATE_PROCESS_DEBUG_INFO` 의 `hFile` · `hProcess` · `hThread` · `lpBaseOfImage` · `lpStartAddress` (= 이미지 기준 + 진입점 맞나) · `LOAD_DLL_DEBUG_INFO.lpBaseOfDll` · `lpImageName` 이 가리키는 문자열.
- 받은 핸들의 **접근 권한** `NtQueryObject(ObjectBasicInformation).GrantedAccess` (기대 `0x12067b` · `0x12007b`) · 파일 핸들 접근.
- 자식 `NtQueryInformationProcess(ProcessDebugPort)` ≠ 0 · `ReadProcessMemory` 로 자식 PEB `+0x2` (BeingDebugged = 1).
- **`ntdll!NtWaitForDebugEvent` 를 직접** 불러 (Timeout 0 → 이벤트가 있으면 0 · 없으면 `0x102`) 받은 **0xB8 B 를 16진 덤프** — 칸 표 (3-3) 를 바이트로 대조 (NewState · AppClientId · 핸들 칸).
- `ContinueDebugEvent` 에 `DBG_CONTINUE (0x10002)` · `DBG_EXCEPTION_NOT_HANDLED (0x80010001)` 를 주었을 때 자식이 계속 도는지 · 그 밖 값은 `ERROR_INVALID_PARAMETER` 로 돌아오는지 (`NtDebugContinue` 직접 호출로 `0xc000000d` 확인).
- 디버거가 핸들 닫았을 때 (`CloseHandle` 로 디버그 객체 핸들 닫기) 자식이 끝나는지 (`DebugSetProcessKillOnExit(TRUE)` 와 짝).

## 다른 프로젝트 몫

- **ntdll** (`DbgUiWaitStateChange` · `DbgUiConvertStateChangeStructure(Worker)` 는 3-3b 에서 읽음 — 나머지는 안 읽음): `DbgUiContinue` (→ `NtDebugContinue`) · `DbgUiConnectToDbg` (→ `NtCreateDebugObject` 인자, kernelbase 에서 부르는 곳 1-1 a행) · `DbgUiGetThreadDebugObject` · `DbgUiConvertStateChangeStructureEx` (R8B 의 뜻) · `DbgUiRemoteBreakin` · `LdrpInitializeProcess` 의 `BeingDebugged` 분기 (`DbgBreakPoint`, 시작 브레이크포인트 예외) · ntdll PDB 의 `DBG_STATE` 이름 확인.
- **kernelbase**: `CreateProcessInternalW` 의 디버그 갈래 (`+0x809 ~ +0x908`, 위 1-1 a행) 를 `DEBUG_ONLY_THIS_PROCESS` 와 `PspInsertProcess` 4번째 인자 bit 1 로 이어 보기 · `WaitForDebugEvent` · `ContinueDebugEvent` · `DebugActiveProcess` · `CreateProcessInternalW +0x1376` 쪽 `NtRemoveProcessDebug` (실패 시 정리). (기존 asm 만 읽음 — 열지 않음)

## 다음 요청 후보

1. **attach 길**: `NtDebugActiveProcess` · `DbgkpSetProcessDebugObject` (INACTIVE 푸는 곳 · NOWAIT 이벤트) · `DbgkpPostFakeProcessCreateMessages` · `DbgkpPostFakeThreadMessages` · `DbgkpSuspendProcess` 쪽 — 서 `nt_debug_active_process.cpp:42` (`attached_pids`) 와 같이.
2. **첫 이벤트 뒤**: `DbgkForwardException` (예외 이벤트 · `DbgkpSendErrorMessage` 와의 갈림) · `DbgkExitThread` · `DbgkExitProcess` · `DbgkUnMapViewOfSection` · `NtRemoveProcessDebug` · `DbgkClearProcessDebugObject`.
3. **스레드 플래그 쪽**: `ClonedThread` · `HideFromDebugger` · `SkipCreationMsg` 를 켜는 곳 (`NtCreateThreadEx` 의 생성 플래그 → `PspAllocateThread`), `NtCreateUserProcess` 첫 스레드 플래그.
4. **ntdll 쪽 나머지**: 위 "다른 프로젝트 몫" 첫 줄 (`DbgUiContinue` · `DbgUiConnectToDbg` · `LdrpInitializeProcess` 의 `BeingDebugged` 분기).

## 판정

**분석성공.** 요청서 1 · 2 · 3 모두 답했다 (정적 근거 줄 포함). 이벤트 차례 · 메시지 칸 · 큐 항목 칸 · 출력 구조 칸 · 기다림/깨움 · status 표 다 채움. 못 닫은 것은 위 `[공백]` (스레드가 처음 도는 때 · attach 길의 INACTIVE 풀기 · 클론 플래그 켜는 곳) 이고 모두 "다음 요청 후보" 로 넘김. ntoskrnl 은 새 export 두 번 (20함수, depth 0), ntdll 은 ⚠ 조건 (148 결과서가 생긴 뒤) 을 확인하고 `DbgUi*` 3함수만 depth 0 으로 뽑았다. 모든 export 는 `free -g` available 27 ~ 28 G 에서 했다.

# 2026-11-27 (작성일, 정적 분석 2026-11-27) run154 — 디버그 속성으로 지은 자식의 첫 이벤트 · 기다림 · 이어가기 (Q291, 정적만): 속성 `0x60001` 값 = **디버그 객체 핸들**. `PspBuildCreateProcessContext` 는 크기 8 만 보고 문맥 `+0x90` 에 **저장만** 하고, `PspInsertProcess +0x2f3` 이 접근 **2** 로 열어 `+0x13e DbgkCopyProcessDebugPort` 에 넘긴다 → 자식 `EPROCESS.DebugPort` (`+0x308`) = 객체 · `DbgkpMarkProcessPeb +0x84` 가 `PEB.BeingDebugged` = 1. 첫 이벤트는 **자식 첫 스레드가 유저로 가기 전에 스스로** 보낸다 (`PspUserThreadStartup +0x9c → PspNotifyThreadCreation → DbgkCreateThread`): **① CreateProcess (ApiNumber 2, 얼림 없음) → ② LoadDll ntdll (5, 다른 스레드 얼림)** (WoW64 면 32비트 ntdll 하나 더). exe 는 LoadDll 이 따로 없고 ① 안에 들어 있다. 보낸 스레드는 **자기 커널 스택의 0x168 B 이벤트**를 디버그 객체 큐 꼬리에 끼우고 그 이벤트의 `ContinueEvent` 를 **커널 모드 · 비경보 · 무한**으로 기다린다 — 깨우는 건 `NtDebugContinue → DbgkpWakeTarget` 하나 (또는 디버거가 객체를 닫을 때). `NtWaitForDebugEvent` 출력은 **0xB8 B 맞음**, `NtDebugContinue` ContinueStatus **여섯 값 맞음** (팀원 주석 둘 다 맞다)

(글의 오프셋은 **함수 시작 기준** `+0x…` — call 자리, ret 는 아래 "정적 — 호출 지점" 표. 떨어진 조각 (`frag:FUN_…`) 안의 줄도 결과서가 조각 시작 기준으로 적은 곳은 **함수 기준으로 바꿔** 적고 조각 기준은 괄호에만 둔다 (아래 "flow-writer 대조"). 주소 끝자리는 안 섞는다. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** (원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` ⑥ 절, 읽기만 — 튜닝 DBG · r19 8 샘플) 이고 **디버그 속성 (`DEBUG_PROCESS`) 으로 지은 자식** 길이다. test.exe 는 cmd 가 디버그 속성 없이 띄우고, 부모 cmd 도 디버거 아래가 아니다 — 사진 여덟 장 모두 `DebugPort : (null)` · `BeingDebugged : 0` `[관찰]` (`ms/logs/04_baggage-read_K1.txt:156` · `:1548`, `_K2.txt:156` · `:1549`, `_P2.txt:159` · `:2433` 등, run44). 그래서 이 길은 **test.exe 에선 `DbgkCopyProcessDebugPort +0x27` (0 쓰기) 와 `DbgkCreateThread +0x9c` (`DebugPort == 0` → 끝) 두 줄만** 지나간다. 그림 1 · 2 에 Dbgk 노드가 없고 새 노드를 넣을 실행 길도 없어 **그림은 안 바꿨다.** flow.md 는 기준 (ntkrnlmp · ntdll 줄 한 마디 · 근거 줄) · 2.1 디버그 포트 글머리 · 2.3 `DbgkCreateThread` 글머리 · 3장 새 절 · 4장 (Q291 해결됨 하나 · run154 새 셋) · 5장 (run154 두 줄) 만 고쳤다. 짐 지도 (`baggage.md`) 는 셈을 안 바꾸고 **3.1 `BeingDebugged` · 3.11 `DebugPort` 두 줄에 경로 한 마디씩** 만 보탰다.

> **대기열**: **Q291** (새, 380 ⑥). 사용자 결정 2026-11-27 (PE_emulator D179) 380 묶음 정적 차례 ⑥. 결과서 판정 **분석성공** — 물음 1 · 3 · 2 를 `.asm` 줄로 닫음.
> - 이어 쓰는 앞 run: **run50** (`PspInsertProcess +0x13e DbgkCopyProcessDebugPort` · `+0x27 DebugPort = 0` · `+0x2f3` 핸들 갈래 "안 탐"), **run51** (`DbgkCreateThread +0x85 Flags |= 0x400001` · 이미지 알림), **run113** (`DbgkpMarkProcessPeb +0x84` `BeingDebugged`), **run15** (`PspUserThreadStartup` 정상 길 차례).
> - 남은 것 → 동적은 **새 시험 exe** (디버그 속성 자식 하나) 가 필요한 새 후보, 디버거 없이 떠 올 것 **있음** (유저 모드 디버그 루프), 정적은 결과서 "다음 요청 후보" 1 ~ 4 — 대기열 자리는 오케스트레이터가 정함.

## 요약

- **붙는 길** `[정적]`: kernelbase `CreateProcessInternalW +0x809` (`CreationFlags & 3`) → 속성 칸 `{0x60001, 8, 핸들}` → nt `PspBuildCreateProcessContext +0x17a3` (크기 8 아니면 `0xc000000d`) · `+0x17b2` 문맥 `+0x90` = 핸들 → `NtCreateUserProcess +0xfa3/+0xfaa` 가 `PspInsertProcess` 5번째 인자로 → **`+0x2f3 ObReferenceObjectByHandle(핸들, 2, DbgkDebugObjectType, PreviousMode)`** → **`+0x13e DbgkCopyProcessDebugPort(자식, 만든 쪽 프로세스, 객체, &붙음)`**.
- **`DebugPort` · `BeingDebugged` 를 누가 언제** `[정적]`: `DbgkCopyProcessDebugPort +0x27` 이 먼저 0 → `+0x2c89e1` 이 객체를 씀 (객체 `Flags` bit 0 "닫는 중" 이 꺼졌을 때만, 객체 뮤텍스 쥔 채) → `+0x2c8a0c DbgkpMarkProcessPeb` → `+0x84 PEB.BeingDebugged = (DebugPort ≠ 0)` (WoW64 면 `+0xca` PEB32 도). 때 = PEB (`MmCreatePeb`) · 첫 스레드 틀 (`PspAllocateThread`) 을 지은 **뒤**, 스레드를 끼우기 (`PspInsertThread`) **앞**.
- **핸들이 없어도** `DbgkCopyProcessDebugPort` 는 불린다: 그땐 **만든 쪽 (현재) 프로세스**의 `DebugPort` 가 있고 그쪽 `NoDebugInherit` (Flags bit 1) 가 꺼졌을 때 같은 객체를 물려받는다 (`+0x3c` · `+0xbe ~ +0xd2`).
- **첫 이벤트 차례** `[정적]`: 자식 첫 스레드가 `PspUserThreadStartup +0x9c` (유저로 가기 `+0xae` **앞**) 에서 → `DbgkCreateThread` 가 `Flags |= CreateReported | ImageNotifyDone` (`+0x85`) 하고 `DebugPort ≠ 0` (`+0x9c`) · 옛 `CreateReported` 0 (`+0x2b3`) 이면 조각으로 → **① CreateProcess** (`+0x207ad6`, `DbgkpSendApiMessage` Flags **0**) → **② LoadDll ntdll** (`+0x207afb DbgkSendSystemDllMessages → +0x2e2`, Flags **3**) → (WoW64 만) ③ LoadDll 32비트 ntdll → (클론 스레드만) 모듈 LoadDll 들.
- **깃발** `[정적] PDB`: `+0x2c4 TEST AL,4` = `ETHREAD.CrossThreadFlags.HideFromDebugger` (bit 2) · `+0x327 TEST AL,0x10` = `ETHREAD.SameThreadPassiveFlags.ClonedThread` (bit 4) · `+0x2b3 TEST R12B,1` = `EPROCESS.Flags.CreateReported` (bit 0) 의 옛 값.
- **메시지 머리** `[정적]`: `0x400018` = DataLength `0x18` · TotalLength `0x40`, `8` = 종류 (LPC 디버그 이벤트 `[추측: 이름]`), `1` = ApiNumber (CreateThread). CreateProcess 는 `0x600038` · `8` · `2`. 메시지 전체는 0x110 B.
- **큐에 넣고 멈춤** `[정적]`: `DbgkpSendApiMessage +0x79 ReturnedStatus = 0x103` → `+0x87 DbgkpQueueMessage` 가 이벤트 (0x168 B, **보낸 스레드 스택**) 를 객체 `EventList` (`+0x50`) 꼬리에 (`+0x162 ~ +0x16c`) → `+0x17e KeSetEvent(EventsPresent)` → **`+0x1f0 KeWaitForSingleObject(이벤트+0x10, Executive, KernelMode, 비경보, 무한)`**. `Flags` bit 0 인 보냄만 같은 프로세스의 다른 스레드를 `PsFreezeProcess` 로 얼린다 (CreateProcess 는 0 이라 안 얼림).
- **고르기** `[정적]`: `NtWaitForDebugEvent` 는 큐 앞에서부터 `Flags & 5 == 0` 이고 **앞쪽에 같은 PID 이벤트가 없는** 첫 이벤트를 고르고 READ(1) 를 단다 (큐에서 안 뺌). 앞에 같은 PID 가 있으면 INACTIVE(4) 를 달고 건너뜀 = **한 프로세스는 한 번에 이벤트 하나만** 디버거에게 보인다.
- **빈 큐** `[정적]`: 객체 자체 (알림 이벤트) 를 호출자 모드 · `Alertable` · `Timeout` 으로 기다림 → `0x102` · `0x101` · `0xc0` 셋 다 출력 0xB8 B 를 0 으로 써 준다. ntdll `DbgUiWaitStateChange +0xf` 는 **늘 `Alertable = 1`** 로 부른다.
- **이어가기** `[정적]`: `NtDebugContinue` 는 핸들을 열기 **전에** ContinueStatus 여섯 값을 보고 (`+0x5c` · `+0x19c ~ +0x1d8`), PID · TID 가 같고 **READ 가 켜진** 이벤트를 빼서 `ReturnedStatus = ContinueStatus` (`+0x13b`) · `Status = 0` (`+0x141`) → `+0x148 DbgkpWakeTarget` → `+0x51 KeSetEvent(이벤트+0x10)`. 같은 PID 의 **다음 이벤트 하나**의 INACTIVE 를 풀고 `EventsPresent` 를 켠다 (`+0x114`).
- **덤** `[정적]`: `0x40010001` 로 이어 주면 보낸 쪽 `DbgkpSendApiMessage +0x9e ~ +0xa6` 이 **같은 메시지를 다시 큐에 넣는다** — "이어가기" 가 아니라 "나중에 답함" `[추측: 이름 DBG_REPLY_LATER]`.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백열두째 바퀴. plan **Q291**. **짐 지도 바퀴 아님** (3.1 · 3.11 두 줄에 경로 한 마디만).
- 정적: ntoskrnl · ntdll 둘 다 **다시 analyze 안 함** — 있던 프로젝트에 export 만 (결과서: ntoskrnl 2번 20함수 · ntdll 2번 3함수, depth 0, 각 export 앞 `free -g` available 27 ~ 28 G).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| ntoskrnl (`ntkrnlmp.pdb`) | `C29EBFB06B78B3C020DCA66D99713F9E1` | 같음 (결과서 확인) |
| ntdll | `C3093720177DA6851519DCFFE8DF53FE1` | 같음 (`DbgUi*` 셋만 export — 요청서 ⚠ 조건: sast/148 결과서가 생긴 뒤에만 열었음, 결과서 머리) |
| kernelbase (64비트) | `72CCE506944DE0267C172AD64287F9BD1` | 같음 (있던 `CreateProcessInternalW.asm` 읽기만) |

- 버전 대조 (결과서 "버전 대조" — 일치): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` (`_calls.tsv`). flow-writer 도 `NtCreateUserProcess +0xfbd/+0xfc2 → PspInsertProcess` (run50 표) 를 `.asm` 에서 다시 봄 — 같음.
- `_DEBUG_OBJECT` · `_DEBUG_EVENT` · `_DBGKM_APIMSG` · `_DBGUI_WAIT_STATE_CHANGE` 는 **이 PDB 에 없다** (결과서 `pdb_struct.py` → 없음). 있는 건 `_DBGKM_EXCEPTION64` (`sizeof 0xa0`) · `_PORT_MESSAGE` (`sizeof 0x28`) 뿐. 그래서 아래 표에서 이 넷의 **오프셋 · 크기 · 값은 `[정적]` (asm 에서 셈), 칸 이름은 `[추측: 이름]`**. EPROCESS · ETHREAD · KTHREAD · PEB · TEB 칸 이름은 `[정적] PDB` (flow-writer 가 `pdb_struct.py` 로 다시 봄 — 아래 "방법").

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): `r19 6a3a4b0552cc seq 849 CreateProcessA(…WUDFHost.exe, 플래그 1)` → 850 `NtCreateDebugObject` 성공 → 887 `NtCreateUserProcess` 기본 대답 `process_attribute_unsupported:0x60001` → 888 `CreateProcessA` 0. 속성만 받아 주면 부모 `NtWaitForDebugEvent` 가 팀원 멈춤 (`debug_event_queue_unmodeled`) 에 닿는다.
  1. 자식에 디버그 객체가 붙는 길과 첫 이벤트 — `DebugPort` · `BeingDebugged` 를 누가 언제, `DbgkCreateThread` 이벤트 종류 · 차례, `+0x2c4` · `+0x327` 깃발, 조각 `+0x2079f4 ~ +0x207afb`, 메시지 칸.
  2. `DbgkpSendApiMessage` · `DbgkpPostModuleMessages` — 큐에 넣는 줄 · 큐 항목 칸 · 보낸 스레드가 어디서 무엇을 기다리나 · 다른 스레드 · 깨어나는 줄.
  3. `NtWaitForDebugEvent` · `NtDebugContinue` — 고르는 규칙 · 빈 큐 기다림 · status · **0xB8 출력 구조 칸 표** (`NewState` 마다, 여는 핸들 · 접근) · 찾아 깨우는 줄 · 실패 status.
- 받을 꼴 (원문): 이벤트 차례 표 · `0xB8` 칸 표 · "stub 이 해야 할 최소" → 이 문서 끝 **"PE_emulator 쪽"** 절.

## 방법

- 정적 sast/153 (요청서 `_workspace/sast/153_dbgk-events.md`, 결과서 `_workspace/sast/153_dbgk-events_result.md`, **분석성공**, 스크립트 문제 없음 — 읽을 때 주의 하나: `ms/decomp/ntoskrnl/NtDebugContinue.c` 가 `ObReferenceObjectByHandle` 첫 인자를 ClientId 첫 8 B 로 그렸다. **`.asm` 이 맞다** — `+0x0 ~ +0x90` 사이에 RCX 를 바꾸는 줄이 없어 핸들 = 첫 인자. 이 문서도 `.asm` 만 근거로 씀):
  - ntoskrnl 첫 번 17개: `DbgkpSendApiMessage` · `DbgkpQueueMessage` · `DbgkpPostModuleMessages` · `DbgkpSectionToFileHandle` · `DbgkSendSystemDllMessages` · `DbgkpMarkProcessPeb` · `NtCreateDebugObject` · `NtWaitForDebugEvent` · `DbgkpConvertKernelToUserStateChange` · `DbgkpOpenHandles` · `NtDebugContinue` · `DbgkpWakeTarget` · `DbgkpFreeDebugEvent` · `DbgkpSuspendProcess` · `DbgkpResumeProcess` · `DbgkpCloseObject` · `NtSetInformationDebugObject` (로그 `ms/ghidra/ntoskrnl.export.sast153a.stdout`).
  - ntoskrnl 둘째 번 3개: `DbgkPostModuleMessage` · `DbgkMapViewOfSection` · `PsQuerySystemDllInfo` (`…sast153b.stdout`).
  - ntdll 3개: `DbgUiConvertStateChangeStructure` · `DbgUiWaitStateChange` · 꼬리호출 대상 `DbgUiConvertStateChangeStructureWorker`.
  - 있던 `.asm` 읽기: `DbgkCreateThread` · `PspNotifyThreadCreation` · `DbgkCopyProcessDebugPort` · `PspInsertProcess` · `PspUserThreadStartup` · `PspBuildCreateProcessContext` · `NtCreateUserProcess` · kernelbase `CreateProcessInternalW`.
- flow-writer 가 다시 본 것:
  - `.asm` (ntoskrnl): `PspInsertProcess +0x0 ~ +0x70 · +0x115 ~ +0x160 · +0x2c5 ~ +0x322` · `NtCreateUserProcess +0x0 ~ +0x2d` (프레임) · `+0xf8b ~ +0xfc2` · `PspBuildCreateProcessContext +0x164d · +0x1cd · +0x17a3 ~ +0x17ba` · `DbgkCopyProcessDebugPort` 전부 (본체 · 조각 `FUN_140845053` · 떨어진 범위) · `DbgkpMarkProcessPeb +0x4a · +0x84 · +0xca` · `DbgkCreateThread +0x3e · +0x45 · +0x70 ~ +0xaa · +0x2b3 ~ +0x2ca · +0x2db ~ +0x342 · +0x2079e6 ~ +0x207b01` (CALL 줄 전부) · `PspUserThreadStartup +0x43 ~ +0xb3 · +0xc2 · +0xc5 · +0xe3 ~ +0xf5` · `PspNotifyThreadCreation +0x0 ~ +0x2b` · `DbgkpSendApiMessage +0x3f ~ +0xa6` · `DbgkpQueueMessage +0x12b ~ +0x183 · +0x1df ~ +0x1f5 · +0x270 · +0x29a · +0x2a4 ~ +0x2b6` · `DbgkSendSystemDllMessages +0x0 ~ +0x3f · +0xa4 · +0x175 · +0x17c · +0x242 ~ +0x2e7` (CALL 줄 전부) · 조각 `FUN_140b36a80` · `DbgkpSectionToFileHandle` CALL 줄 · `DbgkpWakeTarget` CALL 줄 · `DbgkpCloseObject +0x2f · +0xdf · +0xe6` · 조각 `FUN_140b11c12` · `DbgkpSuspendProcess +0x19` · `DbgkpResumeProcess +0xa · +0xf` · `NtCreateDebugObject +0x5f ~ +0x68 · +0x93 · +0xe5 ~ +0x140` · `NtSetInformationDebugObject +0x104 ~ +0x116` · `NtWaitForDebugEvent +0x4d · +0x58 · +0xf7 · +0x12c · +0x145 · +0x1ad · +0x1d5 ~ +0x1e9 · +0x214 · +0x223 · +0x24b · +0x28a ~ +0x2bb · +0x2dc · +0x321` (CALL 줄 전부) · `NtDebugContinue +0x0 · +0x5c · +0x8b ~ +0x95 · +0x13b ~ +0x14d · +0x16b · +0x1d8` (CALL 줄 전부) · `DbgkpConvertKernelToUserStateChange +0x0 · +0x4 · +0xdd · +0xe1 · +0x11d · +0x127 · +0xeaa78 ~ +0xeaa9e` · `DbgkpOpenHandles` CALL 줄 전부 · `+0x0 ~ +0x1f · +0x91 ~ +0xca` · 조각 `FUN_140b39ca6 +0x0 ~ +0x85` · `DbgkPostModuleMessage +0x112 ~ +0x14e` · `DbgkpPostModuleMessages +0xa5 · +0xa8` · `DbgkMapViewOfSection +0x157 ~ +0x169`.
  - `.asm` (ntdll): `DbgUiWaitStateChange` 전부 · `DbgUiConvertStateChangeStructure` 전부 · `DbgUiConvertStateChangeStructureWorker +0x11e · +0x125 · +0x157 · +0x1d2 · +0x214 · +0x21c · +0x22d`.
  - `pdb_struct.py ntoskrnl.exe`: `_EPROCESS` (`Flags +0x1f4` 비트 0 `CreateReported` · 1 `NoDebugInherit` · 3 `ProcessDelete` · 22 `ImageNotifyDone`, `SectionObject +0x2a8` · `SectionBaseAddress +0x2b0` · `Peb +0x2e0` · `DebugPort +0x308` · `Flags3 +0x5fc` 비트 0 `Minimal`) · `_ETHREAD` (`Cid +0x508` · `Win32StartAddress +0x560` · `RundownProtect +0x588` · `CrossThreadFlags +0x5a0` 비트 1 `ThreadInserted` · 2 `HideFromDebugger` · 6 `SkipCreationMsg` · 7 `SkipTerminationMsg` · `SameThreadPassiveFlags +0x5a4` 비트 4 `ClonedThread`) · `_KTHREAD` (`ApcState +0x98` · `Process +0x220` · `PreviousMode +0x232`) · `_KAPC_STATE.Process +0x20` · `_KPROCESS.SecureState +0x170` · `_PEB.BeingDebugged +0x2` · `_TEB` (`StaticUnicodeBuffer +0x1268` · `DbgSsReserved +0x16a0`).
  - 사진 로그 (값 대조): `ms/logs/04_baggage-read_{K1,K2,K3,P0,P1,P1a,P1b,P2}.txt` 의 `+0x308 DebugPort : (null)` · `+0x002 BeingDebugged : 0` 줄 (여덟 장 모두).
  - PE_emulator 쪽 (읽기만): 원문 380 ⑥ 절 · "받을 꼴" · `ext/syscall/syscall_layer_cpp/include/syscall_layer/debug.hpp:8` · `src/exception/nt_debug_more.cpp:60 ~ :120` · `src/exception/nt_create_debug_object.cpp:17` · `:40` · `src/process/nt_create_user_process.cpp:166 ~ :172`. 결과서가 적은 줄 번호가 있는지 · 맞는지만 봄.
  - 결과서와 다르게 적은 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **디버그 객체 = 우편함.** 부모 (디버거) 가 우편함을 하나 만들고, 아이 (자식 프로세스) 를 낳을 때 "이 우편함으로 편지 보내" 하고 주소를 쥐여 준다 (속성 `0x60001`).
- 아이는 **눈을 뜨자마자, 밖에 나가기 전에** 편지를 쓴다: "태어났어요 (CreateProcess)" → "ntdll 챙겼어요 (LoadDll)". 편지 한 장을 넣을 때마다 **답장이 올 때까지 문 앞에 서서 기다린다** (보낸 스레드가 멈춤).
- 부모는 우편함을 열어 (`NtWaitForDebugEvent`) 한 장씩 읽고, "읽었다, 계속해" (`NtDebugContinue`) 하고 답장을 넣는다. 같은 아이의 편지는 **한 번에 한 장만** 보여 준다 `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{ntoskrnl,ntdll,kernelbase}/`)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `kernelbase!CreateProcessInternalW` | `+0x813` · `+0x82a` | `+0x81a` · `+0x831` | `DbgUiConnectToDbg` · `DbgUiGetThreadDebugObject` (`+0x809 TEST [RSP+0x108],3` 일 때) → `+0x8ec ~ +0x908` 속성 칸 `{0x60001, 8, 핸들}` | [정적] (결과서, 기존 asm) |
| `nt!NtCreateUserProcess` | `+0x295` | `+0x29a` | `PspBuildCreateProcessContext` (문맥 `[RBP+0x1a0]`, 0x260 B) | [정적] |
| `nt!PspBuildCreateProcessContext` | — | — | `+0x164d SUB RAX,0x60000` 번호 가지 · **`+0x17a3 CMP [RDI+8],8`** · `+0x17a8 JNZ → +0x1cd MOV EDI,0xc000000d` · `+0x17ae/+0x17b2` 문맥 `+0x90` = 값 | [정적] |
| `nt!NtCreateUserProcess` | `+0xfbd` | `+0xfc2` | `PspInsertProcess` (5번째 `[RSP+0x20]` ← `+0xfa3 [RBP+0x230]` = 문맥 `+0x90` · 4번째 R9D ← `+0xfaf [RBP+0x7d0]`) | [정적] (run50) |
| `nt!PspInsertProcess` | **`+0x2f3`** | **`+0x2f8`** | `ObReferenceObjectByHandle(핸들, 2, DbgkDebugObjectType, PreviousMode)` (5번째 인자 ≠ 0 일 때만 — `+0x124 ~ +0x12b JNZ +0x2c5`, 끝나면 `+0x306 JMP +0x131`) | [정적] |
| 〃 | **`+0x13e`** | **`+0x143`** | `DbgkCopyProcessDebugPort(자식, 만든 쪽 프로세스 R13, 객체 또는 0, &붙음)` | [정적] (run50) |
| 〃 | `+0x30e` | `+0x313` | `ObfDereferenceObject(객체)` (핸들로 연 참조 하나 내려놓음) | [정적] |
| `nt!DbgkCopyProcessDebugPort` | `+0x88` | `+0x8d` | `PsTestProtectedProcessIncompatibility` (참이면 `+0x95 0xc0000712`) | [정적] |
| 〃 | `+0x2c89b4` (조각 `FUN_140845053 +0x2c8915`) | `+0x2c89b9` | `VslpEnterIumSecureMode` (자식 `KPROCESS.SecureState` bit 0 일 때만 — 별칭 하나 `[추측: 이름]`) | [정적] |
| 〃 | **`+0x2c8a0c`** (조각 `+0x2c896d`) | **`+0x2c8a11`** (조각 `+0x2c8972`) | **`DbgkpMarkProcessPeb(자식)`** | [정적] |
| `nt!PspUserThreadStartup` | `+0x9c` | `+0xa1` | `PspNotifyThreadCreation` (`Flags3.Minimal` 0 일 때, 유저로 가기 `+0xae` 앞) | [정적] (run15) |
| `nt!PspNotifyThreadCreation` | `+0x1c` | `+0x21` | `DbgkCreateThread` | [정적] (run51) |
| `nt!DbgkCreateThread` | `+0x2079f4` | `+0x2079f9` | `memset(msg, 0, 0x60)` | [정적] |
| 〃 | `+0x207a00` | `+0x207a05` | `DbgkpSectionToFileHandle(EPROCESS.SectionObject)` → `+0x30 MmGetFileNameForSection` · `+0x7d ZwOpenFile` | [정적] |
| 〃 | `+0x207a3c` | `+0x207a41` | `RtlImageNtHeader(SectionBaseAddress)` | [정적] |
| 〃 | **`+0x207ad6`** | **`+0x207adb`** | **`DbgkpSendApiMessage(프로세스, 0, CreateProcess)`** | [정적] |
| 〃 | `+0x207aea` | `+0x207aef` | `ObCloseHandle(FileHandle, 0)` (남았을 때) | [정적] |
| 〃 | **`+0x207afb`** | **`+0x207b00`** | **`DbgkSendSystemDllMessages(0, 0, msg)`** | [정적] |
| 〃 | `+0x31b` | `+0x320` | `DbgkpSendApiMessage(프로세스, 1, CreateThread)` (스레드 길 — 이번 자식 첫 스레드는 안 탐) | [정적] |
| 〃 | `+0x338` | `+0x33d` | `DbgkpPostModuleMessages(프로세스, 스레드, 0)` (`ClonedThread` 일 때만) | [정적] |
| `nt!DbgkSendSystemDllMessages` | `+0xa4` · `+0xf5` · `+0x104` · `+0x170` · `+0x1b8` · `+0x21b` | `+0xa9` · `+0xfa` · `+0x109` · `+0x175` · `+0x1bd` · `+0x220` | `PsQuerySystemDllInfo` · `KiStackAttachProcess` · `RtlImageNtHeader` · `RtlStringCbCopyW` · `KiUnstackDetachProcess` · `ZwOpenFile` | [정적] |
| 〃 | **`+0x2e2`** | **`+0x2e7`** | **`DbgkpSendApiMessage(프로세스, 3, LoadDll)`** (첫 인자 객체 = 0 일 때 — `+0x248 TEST R13 · +0x24b JZ +0x2dd`) | [정적] |
| 〃 | `+0x264` | `+0x269` | `DbgkpQueueMessage(…, Flags 2, 객체)` (첫 인자 객체 ≠ 0 일 때 — attach 쪽 길, 이번 길 아님) | [정적] (flow-writer 보탬) |
| 〃 | `+0x2b0` | `+0x2b5` | `PsWow64GetProcessNtdllType` (WoW64 32비트 ntdll 고르기) | [정적] |
| `nt!DbgkpSendApiMessage` | `+0x65` | `+0x6a` | `DbgkpSuspendProcess` → `+0x19/+0x1e PsFreezeProcess` (현재 프로세스 · `Flags & 1` 일 때) | [정적] |
| 〃 | **`+0x87`** | **`+0x8c`** | **`DbgkpQueueMessage(프로세스, 현재 스레드, 메시지, 0 또는 0x40, 0)`** | [정적] |
| 〃 | `+0x95` | `+0x9a` | `DbgkpResumeProcess` → `+0xa/+0xf PsThawMultiProcess` · `+0xf/+0x14 KeLeaveCriticalRegion` | [정적] |
| `nt!DbgkpQueueMessage` | `+0x29a` | `+0x29f` | `KeInitializeEvent(이벤트+0x10, 동기, 꺼짐)` | [정적] |
| 〃 | `+0x17e` | `+0x183` | `KeSetEvent(객체 = EventsPresent)` | [정적] |
| 〃 | **`+0x1f0`** | **`+0x1f5`** | **기다림** (`KeWaitForSingleObject(이벤트+0x10, 0, 0, 0, NULL)`) — 장부 표시 `alias:KeWaitForMutexObject+1`, 같은 주소 이름 1개 더 `KeWaitForSingleObject` | [정적] |
| `nt!NtWaitForDebugEvent` | `+0xff` | `+0x104` | `ObReferenceObjectByHandle(핸들, 1, DbgkDebugObjectType, PreviousMode)` | [정적] |
| 〃 | **`+0x12c`** | **`+0x131`** | **기다림** (`KeWaitForSingleObject(객체, Executive, PreviousMode, Alertable, Timeout)`, 같은 별칭) | [정적] |
| 〃 | `+0x21c` | `+0x221` | 이벤트 끄기 (`KeResetEvent(객체)` — 장부 표시 `alias:KeClearEvent+1`, 같은 주소 이름 1개 더 `KeResetEvent`) | [정적] |
| 〃 | `+0x28a` | `+0x28f` | `DbgkpConvertKernelToUserStateChange(지역 0xb8, 이벤트)` | [정적] |
| 〃 | `+0x2b6` | `+0x2bb` | `DbgkpOpenHandles(지역, 프로세스, 스레드)` | [정적] |
| `nt!DbgkpOpenHandles` | `+0xb9` · `+0xfe` · `+0xeab32` (조각 `FUN_140b39ca6 +0x28`) | `+0xbe` · `+0x103` · `+0xeab37` | `ObOpenObjectByPointer` ×3 (CreateThread 스레드 `0x12007b` · CreateProcess 스레드 `0x12007b` · CreateProcess 프로세스 `0x12067b`) | [정적] |
| 〃 | `+0x7c` · `+0xeab7d` (조각 `+0x73`) | `+0x81` · `+0xeab82` | `ObDuplicateObject` (LoadDll · CreateProcess 파일 핸들) → `+0x8a/+0x8f ObCloseHandle` (원본) | [정적] |
| `nt!NtDebugContinue` | `+0x90` | `+0x95` | `ObReferenceObjectByHandle(핸들 = RCX, 1, …)` (`.c` 가 아니라 `.asm` 근거) | [정적] |
| 〃 | `+0x114` | `+0x119` | `KeSetEvent(객체)` (같은 PID 다음 이벤트 INACTIVE 풀 때) | [정적] |
| 〃 | **`+0x148`** | **`+0x14d`** | **`DbgkpWakeTarget(이벤트)`** | [정적] |
| `nt!DbgkpWakeTarget` | `+0x21` · `+0x37` · **`+0x51`** · `+0x5f` | `+0x26` · `+0x3c` · **`+0x56`** · `+0x64` | `PsMultiResumeThread` (`Flags & 0x20`) · `DbgkpFreeDebugEvent` (NOWAIT) · **`KeSetEvent(이벤트+0x10)`** · `ExReleaseRundownProtection` (`Flags & 8`) | [정적] |
| `nt!DbgkpCloseObject` | `+0x52` · `+0xe6` · `+0x26d2da` (조각 `FUN_140b11c12 +0x18`) · `+0x26d2ec` (조각 `+0x2a`) | `+0x57` · `+0xeb` · `+0x26d2df` · `+0x26d2f1` | `KeSetEvent(객체)` · `DbgkpWakeTarget` (`Status 0xc0000354`) · `DbgkpMarkProcessPeb` (뗄 때 0) · `PsTerminateProcess` (KillOnClose) | [정적] |
| `nt!DbgkPostModuleMessage` | `+0x120` · `+0x14e` | `+0x125` · `+0x153` | `DbgkpQueueMessage(…, 2, 객체)` (객체 있을 때) · `DbgkpSendApiMessage(…, 3, …)` (없을 때) | [정적] |
| `nt!DbgkMapViewOfSection` | `+0x164` | `+0x169` | `DbgkpSendApiMessage(프로세스, 1, LoadDll)` (`+0x15c MOV EDX,1`) — 나중 DLL | [정적] |
| `ntdll!DbgUiWaitStateChange` | `+0x18` (`JMP`) | — | `NtWaitForDebugEvent` 꼬리호출 (`+0x11` 핸들 = `TEB+0x16a8` · `+0xf MOV DL,1`) | [정적] |
| `ntdll!DbgUiConvertStateChangeStructure` | `+0x3` (`JMP`) | — | `DbgUiConvertStateChangeStructureWorker` (`+0x0 XOR R8D,R8D`) | [정적] |

### 1. 붙는 길 — 속성 `0x60001` 에서 `DebugPort` 까지 (물음 1 앞) [정적]

- **a. kernelbase** (결과서 1-1 a, 기존 asm): `CreateProcessInternalW +0x809` 가 `CreationFlags & 3` (`DEBUG_PROCESS` · `DEBUG_ONLY_THIS_PROCESS` `[추측: 이름]`) 을 보고 `DbgUiConnectToDbg` · `DbgUiGetThreadDebugObject` 로 핸들을 얻어 `+0x8ec ~ +0x908` 에서 속성 칸 `{0x60001, 8, 핸들}` 을 짓는다. `+0x841 ~ +0x84b` 는 bit 1 이면 내부 플래그 bit 1 도 켠다 → 이게 아래 `PspInsertProcess` 4번째 인자 bit 1 로 가는지는 kernelbase 쪽이라 `[추측]`.
- **b. 받기**: `PspBuildCreateProcessContext` 번호 가지 (`+0x164d SUB RAX,0x60000` ~) 의 0x60001 칸 `+0x17a3 CMP [RDI+8],8` — 크기가 8 이 아니면 `+0x17a8 JNZ → +0x1cd` `0xc000000d`. 맞으면 `+0x17ae/+0x17b2` 문맥 `+0x90` = 값. **여기서는 핸들을 열어 보지 않는다.** (같은 가지에서 0x60000 → 문맥 `+0x80` · 0x60002 → `+0x98`, 결과서.)
- **c. 넘기기**: `NtCreateUserProcess` 문맥 = `[RBP+0x1a0]` (`+0x274 ~ +0x29a`). `+0xfa3/+0xfaa` 가 `[RBP+0x230]` (= 문맥 `+0x90`) 을 `PspInsertProcess` 5번째 인자 `[RSP+0x20]` 로. 차례: `PspAllocateProcess` (`+0xc36`, 안에서 `MmCreatePeb`) → `PspAllocateThread` → **`PspInsertProcess` (`+0xfbd`)** → `PspInsertThread`.
- **d. 열기**: `PspInsertProcess +0x115 ~ +0x11e` 현재 프로세스 `ProcessDelete` 면 `0xc000010a` · **`+0x124 ~ +0x12b` 5번째 인자 ≠ 0 이면 `+0x2c5` 로** → `+0x2ce MOV EDX,2` · `+0x2d3` `DbgkDebugObjectType` · `+0x2e3` R9B = 현재 스레드 `PreviousMode` (`+0x232`) → **`+0x2f3 ObReferenceObjectByHandle`** → 실패면 `+0x300 JS → +0x24d` (그 status 를 돌려줌), 성공이면 `+0x306 JMP +0x131`. 접근 2 = `DEBUG_PROCESS_ASSIGN` `[추측: 이름]`.
  - **코드 순서 주의**: `+0x2f3` 은 오프셋으로는 `+0x13e` 뒤지만 **실행은 앞**이다 (떨어진 덩어리에서 열고 `+0x131` 로 돌아와 `+0x13e` 를 부름).
- **e. 붙이기**: `+0x131 ~ +0x13e` `DbgkCopyProcessDebugPort(자식 RBX, 만든 쪽 프로세스 R13, 객체 R14 또는 0, &붙음 [RBP+0x48])`. **R13 = `[현재 스레드 + 0xb8]` = `KTHREAD.ApcState.Process`** (`+0x1b` · `+0x40`, `[정적] PDB` `ApcState +0x98` + `Process +0x20`) — "부모" 가 아니라 **`NtCreateUserProcess` 를 부른 현재 프로세스**다 (cmd → test.exe 에선 같다). 뒤 `+0x145 ~ +0x148` → `+0x30e ObfDereferenceObject` (핸들 참조 내려놓음) · `+0x156 ~ +0x15a` 붙음이 참이면 `+0x318 TEST [RBP+0x58],2` (4번째 인자 bit 1) → `+0x322 OR.LOCK [자식+0x1f4],2` (`NoDebugInherit`).
  - 4번째 인자 = `NtCreateUserProcess +0xfaf R9D = [RBP+0x7d0]`. flow-writer 가 프레임을 셈: `+0x0 ~ +0xb` 레지스터 8개 · `+0xd SUB RSP,0x7d8` · `+0x14 LEA RBP,[RSP+0x80]` → `[RBP+0x7d0]` = 입구 `RSP+0x38` = **`NtCreateUserProcess` 의 7번째 인자** (0 부터 세면 6). 그 인자 이름 (`ProcessFlags`) 은 `[추측: 이름]`.
- **핸들이 없을 때도** `+0x13e` 는 불린다 (R8 = 0): `DbgkCopyProcessDebugPort +0x37 TEST R8 · +0x3a JNZ` 가 안 뛰면 `+0x3c CMP [만든 쪽+0x308],0` → 있으면 `+0xb2 ~ +0xe0` 에서 전역 `DbgkpProcessDebugPortMutex` 를 잡고 `+0xbe` 그 객체 · `+0xca ~ +0xd2` 만든 쪽 `Flags & 2` (`NoDebugInherit`) 면 안 물려받음 · 아니면 참조 하나 올려 같은 길로.

### 2. `DebugPort` · `BeingDebugged` 를 누가 언제 쓰나 (물음 1) [정적]

| 칸 (`[정적] PDB`) | 쓰는 줄 (함수 기준) | 값 · 조건 |
|---|---|---|
| `EPROCESS.DebugPort` (`+0x308`) | `DbgkCopyProcessDebugPort +0x27` `MOV [RCX+0x308],RDI` (RDI = 0) | **늘 먼저 0** — run50 의 "`+0x27` 0" 이 이 줄. test.exe 는 여기까지만 |
| 〃 | **`+0x2c89e1`** `MOV [RSI+0x308],RBX` (조각 `FUN_140845053 +0x2c8942`) | 객체 `Flags` (`+0x60`) bit 0 (닫는 중) 이 꺼졌을 때만 (`+0x2c89da ~ +0x2c89df`), **객체 뮤텍스 (`+0x18`) 를 쥔 채**. 켜져 있으면 안 쓰고 `+0x2c89ea` 표시 → 참조 내려놓고 **status 0 으로 돌아감 (안 붙은 채 성공)** |
| `PEB.BeingDebugged` (`+0x2`) | **`DbgkpMarkProcessPeb +0x84`** `MOV [RAX+2],CL` · WoW64 면 `+0xca` `MOV [RDX+2],CL` (PEB32) | CL = `DebugPort ≠ 0`. `EPROCESS.Peb` (`+0x2e0`) 가 0 이면 아무것도 안 함 (`+0x4a`). 부르는 곳: **붙을 때** `DbgkCopyProcessDebugPort +0x2c8a0c` (ret `+0x2c8a11`) · **뗄 때** `DbgkpCloseObject +0x26d2da` (조각 `FUN_140b11c12 +0x18`, 그땐 `DebugPort` 를 먼저 0 으로 해서 0) |
| `EPROCESS.Flags.NoDebugInherit` (bit 1) | `PspInsertProcess +0x322` `OR.LOCK [RBX+0x1f4],2` | 붙었고 (`[RBP+0x48]` ≠ 0) 4번째 인자 bit 1 일 때 |

- **때**: PEB 는 이미 있다 (`PspAllocateProcess` 안 `MmCreatePeb`). 첫 스레드 틀 (`PspAllocateThread`) 도 있다. 스레드를 프로세스에 끼우기 (`PspInsertThread`) **앞**이라 자식 스레드는 아직 한 번도 안 돌았다.
- **그 밖 거절** (`DbgkCopyProcessDebugPort`): 객체 `Flags` bit 2 (만든 프로세스가 32비트 WoW) 면 자식도 `WoW64Process ≠ 0` 이고 `Machine` (`+0x6ec`) 이 `0x14c` · `0x1c4` 여야 함 — 아니면 `0xc00000bb` (`+0x2c8948 ~ +0x2c8978`). `PsTestProtectedProcessIncompatibility` 참이면 `0xc0000712` (`+0x95`). 거절이면 `PspInsertProcess +0x150 JS` 로 프로세스 짓기가 실패한다.
- **누가 읽나** (이번에 본 줄): `DebugPort` ← `DbgkCreateThread +0x9c` (0 이면 이벤트 없음) · `DbgkpQueueMessage +0x270` (보낼 객체) · `DbgkCopyProcessDebugPort +0x3c` (손자에게 물려줄 때). `BeingDebugged` ← 유저 ntdll (flow.md 2.4 `LdrpInitializeProcess +0x1b35` 등 · 2.10 `RtlRaiseException +0x17e`, 앞 run).

### 3. 첫 이벤트 — 누가 언제 보내나 (물음 1) [정적]

- **보내는 스레드 = 자식 첫 스레드 자신.** 사슬: `KiStartUserThread` → `PspUserThreadStartup +0x9c/+0xa1` → `PspNotifyThreadCreation +0x1c/+0x21` → `DbgkCreateThread`.
  - `PspUserThreadStartup +0x5a ~ +0x6a`: `EPROCESS.Flags3` (`+0x5fc`) bit 0 `Minimal` 이 켜졌으면 `+0x6a JNZ +0xc2` → `+0xc5 DbgkCreateMinimalThread` (이번 길 아님).
  - `+0x48 ~ +0x4d`: `CrossThreadFlags.ThreadInserted` (bit 1) 가 0 이면 `+0xe5 ~ +0xf0 PspTerminateThreadByPointer(…, 0xc000004b, 1)` 뒤 `+0xf5 JMP +0x33` 으로 돌아와 **알림은 그대로 지나간다** (run15 의 "갈래 3개" 와 같은 줄).
  - 알림 (`+0x9c`) 은 유저로 가기 (`+0xae PspInitializeThunkContext`) **앞**. 유저 코드가 한 줄도 돌기 전이다.
- **`DbgkCreateThread` 앞부분**: `+0x77 ~ +0x8d` `CMPXCHG.LOCK` 로 프로세스 `Flags |= 0x400001` (`CreateReported` · `ImageNotifyDone`), 옛 값 → `+0x8f R12D` · `+0x96 BT EAX,0x16` 옛 `ImageNotifyDone` 0 이면 이미지 알림 (run51) · **`+0x9c CMP [RDI+0x308],0` → `+0xa4 JNZ +0x2b3`** (`DebugPort` 있을 때만 이벤트 쪽으로) · `+0x2b3 TEST R12B,1` · `+0x2b7 JZ` → 옛 `CreateReported` 0 = 첫 스레드 → 조각 `+0x2079e6`.
- **두 이벤트 사이에 스레드가 멈춘다.** ① 을 보내면 `DbgkpQueueMessage +0x1f0` 에서 답이 올 때까지 막히고, 답이 와야 `+0x207afb` 로 가서 ② 를 만든다. 그래서 디버거 쪽에서 보면 ① 을 이어 주기 전엔 ② 가 큐에 **없다**.
- `DbgkCreateThread` 는 **답 (`ReturnedStatus`) 을 안 읽는다** (`+0x207adb` · `+0x320` 뒤에 비교 없음, 결과서). 답을 보는 곳은 `DbgkpSendApiMessage` 의 `0x40010001` 하나 (6절).
- **처음 둘 뒤 (밖)**: 이 자식이 만드는 새 스레드 → `DbgkCreateThread` 스레드 길 (CreateThread, ApiNumber 1, Flags 1, `+0x2c4` `HideFromDebugger` 면 건너뜀). 나중 DLL → `DbgkMapViewOfSection +0x164` (LoadDll, Flags 1). 예외 · 끝냄 (`DbgkForwardException` · `DbgkExitThread` · `DbgkExitProcess`) 은 이번 요청 밖 (결과서 1-3 끝).
- 차례 표 (원문 받을 꼴) 는 끝 **"PE_emulator 쪽"** 절.

### 4. `DbgkCreateThread` 깃발 · 조각 메시지 줄 (물음 1) [정적]

| 줄 | 명령 | 뜻 (`[정적] PDB`) |
|---|---|---|
| `+0x2bd` · **`+0x2c4`** · `+0x2c6` | `MOV EAX,[R14+0x5a0]` · `TEST AL,0x4` · `JNZ +0x320` | `ETHREAD.CrossThreadFlags` **`HideFromDebugger` (bit 2)** — 켜졌으면 CreateThread 메시지를 통째로 건너뜀. **스레드 길에서만** (CreateProcess 조각은 이 검사 없음) |
| `+0x320` · **`+0x327`** · `+0x329` | `MOV EAX,[R14+0x5a4]` · `TEST AL,0x10` · `JZ +0xaa` | `ETHREAD.SameThreadPassiveFlags` **`ClonedThread` (bit 4)** — 켜졌으면 `+0x338 DbgkpPostModuleMessages(프로세스, 스레드, 0)` |
| `+0x2b3` | `TEST R12B,1` | `EPROCESS.Flags` 옛 값 **`CreateReported` (bit 0)** — 0 이면 첫 스레드 (조각), 1 이면 스레드 길 |
| `+0x96` | `BT EAX,0x16` | `EPROCESS.Flags.ImageNotifyDone` (bit 22) — 이미지 알림 했나. 디버거와 무관 |
| `+0x9c ~ +0xa4` | `CMP [RDI+0x308],0` | `EPROCESS.DebugPort` |
| `+0xd9 ~ +0xf7` | `[RDI+0x310]` ≠ 0 이면 `Machine (+0x6ec)` 이 `0x14c` / `0x1c4` → `[RSP+0x20]` byte | "32비트 WoW 프로세스" — 조각에서 진입점 셈을 가름 (결과서) |

조각 `+0x2079e6 ~ +0x207b01` = CreateProcess 메시지 (메시지 시작 = `[RSP+0x90]`):

| 줄 | 하는 일 | 메시지 칸 |
|---|---|---|
| `+0x2079f4` | `memset(msg, 0, 0x60)` | |
| `+0x207a00` | `DbgkpSectionToFileHandle(EPROCESS.SectionObject [RDI+0x2a8])` → `[RSP+0xc8]` | **`+0x38` FileHandle** (커널 핸들, 실패면 0) |
| `+0x207a0d` | `[RSP+0xd0] = EPROCESS.SectionBaseAddress [RDI+0x2b0]` | **`+0x40` BaseOfImage** |
| `+0x207a3c` | `RtlImageNtHeader(SectionBase)` | |
| `+0x207a49 ~ +0x207a67` | 64비트: `[NT+0x30]` (ImageBase) + `[NT+0x28]` (EntryRVA) / 32비트 WoW: `[NT+0x34]` + `[NT+0x28]` → `[RSP+0xe8]` | **`+0x58` InitialThread.StartAddress = 이미지 진입점** (`Win32StartAddress` 가 아님) |
| `+0x207a6f ~ +0x207a7c` | `[NT+0xc]` · `[NT+0x10]` | **`+0x48` DebugInfoFileOffset** (`PointerToSymbolTable`) · **`+0x4c` DebugInfoSize** (`NumberOfSymbols`) |
| `+0x207aa8` · `+0x207ab3` · `+0x207abe` | `[+0x90] = 0x600038` · `[+0x94] = 8` · `[+0xb8] = 2` | 머리 DataLength `0x38` · TotalLength `0x60` · 종류 8 · **`+0x28` ApiNumber 2** |
| `+0x207ad6` | `DbgkpSendApiMessage(RDI, 0, msg)` | |
| `+0x207ae3 ~ +0x207aea` | `[RSP+0xc8]` ≠ 0 이면 `ObCloseHandle` | 디버거가 읽었으면 `DbgkpConvertKernelToUserStateChange +0x11d` 가 이벤트 안 칸을 0 으로 만들어 되복사 뒤 0 → 안 닫음 |
| `+0x207afb` | `DbgkSendSystemDllMessages(0, 0, msg)` | 같은 `msg` 를 다시 씀 (안에서 처음부터 채움) |
| `+0x207b01` | `JMP +0x320` | `ClonedThread` 검사로 |

스레드 길 (`+0x2c8 ~ +0x320`, 이번 자식 첫 스레드는 안 탐): `+0x2c8 ~ +0x2d6` `memset 0x40` · `+0x2db/+0x2e2` `[+0xc8] = ETHREAD.Win32StartAddress` (`+0x560`) = **`+0x38` StartAddress** · `+0x2ea` `0x400018` · `+0x2f5` `8` · `+0x300` `1` (CreateThread) · `+0x31b` `DbgkpSendApiMessage(RDI, 1, msg)`.

- LoadDll 메시지 (`DbgkSendSystemDllMessages`): 머리 `+0x228 ~ +0x239` (DataLength `0x28` · TotalLength `0x50`, 결과서). **`NamePointer` = 자식 TEB 의 `NtTib.ArbitraryUserPointer` 칸 주소** (`+0x175 LEA RAX,[R15+0x28]` · `+0x17c`) — 그 칸에 `TEB+0x1268` (`StaticUnicodeBuffer`, `[정적] PDB`) 포인터를 넣고 `+0x170 RtlStringCbCopyW` 로 시스템 DLL 경로를 복사, 보낸 뒤 `+0x109cfe` (조각 `FUN_140b36a80 +0xa`) 가 `[TEB+0x28] = 0`. 디버거는 이 포인터를 따라 **자식 메모리에서** 이름을 읽는다.
- 파일 핸들은 `ZwOpenFile` 로 **경로 이름으로 다시 연** 커널 핸들 (`DbgkpSectionToFileHandle +0x7d` · `DbgkSendSystemDllMessages +0x21b`, 접근 `0x80100000` · 속성 `0x640` · 공유 7 · 옵션 `0x20` — 결과서). `PsQuerySystemDllInfo(0)` 이 0 이면 ② 를 건너뜀 (`+0xa4 ~ +0xb4`). `ZwOpenFile` 이 실패해도 파일 핸들 0 으로 **보낸다** (`+0x2d4`).

### 5. 메시지 칸 (0x110 B — `DBGKM_APIMSG` `[추측: 이름]`) [정적] 오프셋

크기: `DbgkCreateThread +0x3e/+0x45 memset(.., 0, 0x110)` · `DbgkpQueueMessage +0xc7 ~ +0x11a` 가 0x80 × 2 + 0x10 복사 (결과서).

| 칸 | 크기 | 이름 `[추측: 이름]` | 값 |
|---|---|---|---|
| `+0x00` | u16 · u16 | DataLength · TotalLength | CreateThread `0x18` · `0x40` / CreateProcess `0x38` · `0x60` / LoadDll `0x28` · `0x50` (`_PORT_MESSAGE` 0x28 B 머리 뒤, `[정적] PDB` 크기) |
| `+0x04` | u16 | Type | 8 늘 |
| `+0x08 ~ +0x27` | | ClientId · MessageId … | **안 채움 (0)**. 이벤트 안 ClientId 는 `DbgkpQueueMessage +0x11d/+0x125` 가 `ETHREAD.Cid` (`+0x508`) 에서 따로 |
| `+0x28` | u32 | ApiNumber | 0 예외 · **1 CreateThread · 2 CreateProcess** · 3 ExitThread · 4 ExitProcess · **5 LoadDll** · 6 UnloadDll |
| `+0x2c` | u32 | ReturnedStatus | 보낼 때 `DbgkpSendApiMessage +0x79` = `0x103`. 이어 줄 때 `NtDebugContinue +0x13b` 가 ContinueStatus 로 덮음 (이벤트 기준 `+0x84`) |
| `+0x30 ~` | | 공용체 | CreateThread: `+0x30` SubSystemKey (0) · `+0x38` StartAddress. CreateProcess: `+0x30` SubSystemKey (0) · `+0x38` FileHandle · `+0x40` BaseOfImage · `+0x48` DebugInfoFileOffset (u32) · `+0x4c` DebugInfoSize (u32) · `+0x50` InitialThread.SubSystemKey (0) · `+0x58` InitialThread.StartAddress. LoadDll: `+0x30` FileHandle · `+0x38` BaseOfDll · `+0x40` DebugInfoFileOffset · `+0x44` DebugInfoSize · `+0x48` NamePointer. 예외: `_DBGKM_EXCEPTION64` (0xa0 B, `[정적] PDB`) |

### 6. 큐에 넣기 · 기다림 · 깨움 (물음 2) [정적]

**`DbgkpSendApiMessage(프로세스, Flags, 메시지)`**:

| 줄 | 하는 일 |
|---|---|
| `+0x3f ~ +0x45` | `EBX = (Flags & 2) << 5` → 큐에 넘기는 값은 `0` 또는 `0x40` |
| `+0x53 ~ +0x6a` | 프로세스 = 현재 스레드의 프로세스 **이고** `Flags & 1` 이면 `+0x65 DbgkpSuspendProcess` (`KernelApcDisable--` → `+0x19 PsFreezeProcess`) |
| **`+0x79`** | **`[메시지+0x2c] = 0x103`** |
| `+0x87` | **`DbgkpQueueMessage(프로세스, 현재 스레드, 메시지, 0/0x40, 0)`** (객체 인자 0 → 큐가 `DebugPort` 를 씀) |
| `+0x8e ~ +0x95` | 얼렸으면 `DbgkpResumeProcess` |
| **`+0x9a ~ +0xa6`** | status ≥ 0 이고 **`[메시지+0x2c] == 0x40010001`** 이면 `+0x48` 로 돌아가 **같은 메시지를 다시 보냄** |

- `Flags` 정리: `DbgkCreateThread` CreateProcess **0** · CreateThread **1** · `DbgkSendSystemDllMessages` **3** · `DbgkPostModuleMessage` (객체 없을 때) **3** · `DbgkMapViewOfSection` **1**.

**큐 항목 (0x168 B — `DEBUG_EVENT` `[추측: 이름]`)**: 크기 `DbgkpQueueMessage +0x3a ~ +0x4c memset 0x168` · 풀 할당 `'DbgE'` (결과서).

| 칸 | 크기 | 이름 `[추측: 이름]` | 뜻 |
|---|---|---|---|
| `+0x00` | 0x10 | EventList | 큐 고리 |
| `+0x10` | 0x18 | ContinueEvent | **동기 이벤트, 꺼진 채** (`+0x29a`). 보낸 스레드가 이걸 기다림 |
| `+0x28` | 0x10 | ClientId | `ETHREAD.Cid`. `NtWaitForDebugEvent` 가 `+0x28` 로 같은 PID 비교 · `NtDebugContinue` 가 `+0x28` · `+0x30` 으로 찾음 |
| `+0x38` · `+0x40` | 8 · 8 | Process · Thread | 보낸 쪽 |
| `+0x48` | 4 | Status | 깨울 때 (`NtDebugContinue +0x141` = 0 · `DbgkpCloseObject +0xdf` = `0xc0000354`) → 보낸 쪽 `+0x1f5 EDI` |
| `+0x4c` | 4 | Flags | **1 READ** (건넴, `NtWaitForDebugEvent +0x28f`) · **2 NOWAIT** (풀 이벤트) · **4 INACTIVE** (`+0x24b`) · 8 RELEASE · 0x20 SUSPEND · 0x40 (`DbgkpSendApiMessage` 가 넘김, 읽는 줄 `[공백]`) |
| `+0x50` | 8 | BackoutThread | NOWAIT 길 · `NtWaitForDebugEvent +0x24f` 가 0 으로 |
| `+0x58` | 0x110 | ApiMsg | 메시지 복사 (이벤트 기준 `+0x80` ApiNumber · `+0x84` ReturnedStatus · `+0x88 ~` 공용체) |

**`DbgkpQueueMessage` 기다림 길 (5번째 `Flags & 2 == 0`, 이벤트 = 보낸 스레드 스택 `RSP+0x40`)**:

| 줄 | 하는 일 |
|---|---|
| `+0x25a ~ +0x266` | `Flags` → 이벤트 `+0x4c` · 전역 `DbgkpProcessDebugPortMutex` 잡음 |
| `+0x270` | **객체 = `EPROCESS.DebugPort` (`[R13+0x308]`)** |
| `+0x277 ~ +0x2d6` | ApiNumber 1 · 2 → 보낸 스레드 `SkipCreationMsg` (bit 6) 면 객체 = 0 (`+0x2ae`) · 5 → `스레드 플래그 & Flags & 0x40` · 3 · 4 → `SkipTerminationMsg` (bit 7) |
| `+0x12b ~ +0x15b` | 객체 0 이면 **`0xc0000353`** |
| `+0x137 ~ +0x141` | 객체 뮤텍스 · `Flags & 1` (닫는 중) 이면 `0xc0000354` |
| **`+0x147 ~ +0x16c`** | **`EventList` 꼬리에 끼움** (`+0x14f` 링크 검사 → 틀리면 `+0x159 INT 0x29`) |
| **`+0x17e`** | **`KeSetEvent(EventsPresent)`** (기다림 길만) |
| **`+0x1df ~ +0x1f0`** | **`KeWaitForSingleObject(이벤트+0x10, 0, 0, 0, NULL)`** — Executive · KernelMode · 비경보 · 무한 |
| `+0x1f5` · `+0x1fe ~ +0x24e` | `EDI = 이벤트.Status` · 이벤트 안 메시지 0x110 B 를 보낸 쪽으로 되복사 (`ReturnedStatus` 가 돌아옴) |

- NOWAIT 길 (`Flags & 2`): 풀에서 할당 · `Flags = 인자 | 4` (INACTIVE 로 시작) · 큐에만 끼우고 `KeSetEvent` 안 함 · 안 기다림 — attach 쪽 (`DbgkPostModuleMessage +0x120` · `DbgkSendSystemDllMessages +0x264`). INACTIVE 를 처음 푸는 곳은 `[공백]` (attach 쪽 `DbgkpSetProcessDebugObject` 안 읽음).

**기다림 / 깨움 표**:

| 누가 | 어디서 | 무엇을 | 누가 깨우나 |
|---|---|---|---|
| 보낸 스레드 (자식 첫 스레드) | `DbgkpQueueMessage +0x1f0/+0x1f5` | 자기 스택 이벤트의 `ContinueEvent` (동기), 커널 모드 · 비경보 · 무한 → **유저 APC · alert 로 안 깨어남** | `DbgkpWakeTarget +0x51` `KeSetEvent(이벤트+0x10)` ← `NtDebugContinue +0x148` (status 0) · `DbgkpCloseObject +0xe6` (`0xc0000354`) |
| 디버거 스레드 | `NtWaitForDebugEvent +0x12c/+0x131` | 디버그 객체 자체 (`EventsPresent` 알림 이벤트), 호출자 모드 · Alertable · Timeout | `DbgkpQueueMessage +0x17e` · `NtDebugContinue +0x114` · `DbgkpCloseObject +0x52` |
| 같은 프로세스 다른 스레드 | (기다림이 아니라 **얼림**) `DbgkpSuspendProcess +0x19 PsFreezeProcess` | `Flags & 1` 인 보냄만 (② LoadDll · CreateThread · 나중 DLL) | `DbgkpResumeProcess +0xa PsThawMultiProcess` |

- 자식 첫 스레드가 ① · ② 를 보낼 때는 **다른 스레드가 아직 없을 것** — ② 는 얼림을 걸지만 얼릴 스레드가 없다 `[추측: 이 순간 스레드는 첫 스레드 하나 — 일꾼 스레드는 ntdll 초기화 뒤에 생김 (flow.md 3장), 밖에서 원격 스레드를 넣는 경우는 안 봄]`. `PsFreezeProcess` 가 호출 스레드 자신을 안 얼린다는 것은 `[추측]` (안 읽음).
- **디버거가 객체를 닫으면** (`DbgkpCloseObject`): `+0x2f Flags |= 1` → 목록 통째로 떼어 비움 → `+0x52 KeSetEvent` → 모든 프로세스를 훑어 `DebugPort == 이 객체` 면 0 · `DbgkpMarkProcessPeb` (BeingDebugged 0) · `Flags & 2` (KillOnClose) 면 `PsTerminateProcess(프로세스, 0xc0000354)` → 떼어낸 이벤트마다 `Status = 0xc0000354` + `DbgkpWakeTarget`. `R9 ≤ 1` (마지막 핸들) 일 때만 `[추측]`.

### 7. `NtWaitForDebugEvent` — 고르기 · 빈 큐 (물음 3) [정적]

**디버그 객체 (0x68 B — `DEBUG_OBJECT` `[추측: 이름]`)**: `NtCreateDebugObject +0x93 MOV [RSP+0x28],0x68` (객체 크기).

| 칸 | 크기 | 이름 `[추측: 이름]` | 뜻 |
|---|---|---|---|
| `+0x00` | 0x18 | EventsPresent | **알림 이벤트**, 꺼진 채 (`+0xe5 KeInitializeEvent(obj, 0, 0)`) |
| `+0x18` | 0x38 | Mutex | 가드 뮤텍스 (`KeAcquireGuardedMutex` 류 — 장부엔 `alias:ExAcquireFastMutex+1`) |
| `+0x50` | 0x10 | EventList | 빈 원형 목록 |
| `+0x60` | u32 | Flags | **bit 0 닫는 중** (`DbgkpCloseObject +0x2f`) · **bit 1 KillOnClose** (`NtCreateDebugObject +0xea ~ +0xf2` = `(Flags 인자 & 1) << 1`, `NtSetInformationDebugObject +0x104 ~ +0x116` 이 같은 bit 를 켜고 끔) · **bit 2 만든 프로세스가 32비트 WoW** (`+0x105 ~ +0x137`) |

- 만들 때 `Flags` 인자 `& 0xfffffffe` ≠ 0 이면 `0xc000000d` (`NtCreateDebugObject +0x5f ~ +0x68`).
- 접근 값: `NtWaitForDebugEvent` (`+0xf7`) · `NtDebugContinue` (`+0x8b`) **1** · `PspInsertProcess` (`+0x2ce`) **2** · `NtSetInformationDebugObject` **4** (결과서) — 이름 `DEBUG_READ_EVENT` · `DEBUG_PROCESS_ASSIGN` · `DEBUG_SET_INFORMATION` 은 `[추측: 이름]`.

**줄 지도**:

| 줄 | 하는 일 |
|---|---|
| `+0x4d ~ +0x58` | **`memset(지역 0xb8, 0)`** |
| `+0x5e ~ +0xb1` | Timeout 있으면 8 B 복사 + `KUSER_SHARED_DATA.InterruptTime` 기억 (상대 시간 다시 셈) |
| `+0xb6 ~ +0xcc` | 출력 첫 B · `+0xb7` B 쓰기 확인 |
| `+0xff` | `ObReferenceObjectByHandle(핸들, 1, …)` — 실패면 **출력 안 씀** |
| **`+0x12c`** | **기다림** (객체, Executive, PreviousMode, Alertable, Timeout) |
| `+0x1d5 ~ +0x1e9` | status `0xc0` · `0x101` · `0x102` 면 바로 끝 (출력은 0 구조 복사 `+0x145 ~ +0x1ad`) |
| `+0x1f8 ~ +0x202` | 객체 뮤텍스 · `Flags & 1` 이면 `+0x2dc 0xc0000354` |
| `+0x214 ~ +0x221` | 목록이 비었고 못 골랐으면 **`+0x21c` 이벤트 끄기 (`KeResetEvent`)** |
| `+0x223 ~ +0x22b` | 항목 `Flags & 5` (READ · INACTIVE) 면 건너뜀 |
| `+0x231 ~ +0x25a` | **앞쪽에 같은 `UniqueProcess` 항목이 있으면** 이 항목 `Flags |= 4` (`+0x24b`) · `[+0x50] = 0` 하고 건너뜀 |
| `+0x260 ~ +0x28a` | 고름 → 프로세스 · 스레드 참조 (`'DbgO'`) → `DbgkpConvertKernelToUserStateChange` |
| **`+0x28f`** | **`Flags |= 1` (READ)** — 큐에서 **안 뺀다** |
| `+0x2b6` | `DbgkpOpenHandles` (뮤텍스 밖) |
| `+0x2e3 ~ +0x326` | 못 골랐으면 (전부 READ · INACTIVE): 다시 기다림. 상대 시간이면 지난 만큼 줄이고 다 지나면 **`+0x321 EBX = 0x102`** |

- 유저 쪽: `ntdll!DbgUiWaitStateChange` = **`+0x11` 핸들 = `TEB+0x16a8`** (`DbgSsReserved` `+0x16a0` 16 B 의 둘째 칸, `[정적] PDB` 칸 · 뜻 `[추측]`) · **`+0xf MOV DL,1` (Alertable 1)** → `+0x18 JMP NtWaitForDebugEvent`. 그래서 `0x101` · `0xc0` 도 유저까지 올라올 수 있다.

### 8. 출력 구조 0xB8 B (`DBGUI_WAIT_STATE_CHANGE` `[추측: 이름]`) — 커널 · 유저 대조 [정적]

- 크기 **0xB8 맞다**: `NtWaitForDebugEvent +0x4d MOV R8D,0xb8` · `+0xb6 ~ +0xcc` 끝 바이트 `+0xb7` · `+0x145 ~ +0x1ad` 0xb8 복사. 구성 = 머리 0x18 + `_DBGKM_EXCEPTION64` 0xa0 (`[정적] PDB`).
- 공통 칸: `+0x00` NewState (u32) · `+0x08` AppClientId (`+0x08` UniqueProcess · `+0x10` UniqueThread) — `DbgkpConvertKernelToUserStateChange +0x0/+0x4` (이벤트 `+0x28` 16 B 복사).
- `NewState` 마다 칸 표는 끝 **"PE_emulator 쪽"** 절 (원문 받을 꼴). 핸들은 **`NtWaitForDebugEvent` 를 부른 디버거 프로세스의 핸들 표**에 생긴다 (`ObOpenObjectByPointer` 의 접근 모드 `[RSP+0x28]` byte 0 = KernelMode · 속성 0).
- **ntdll 이 같은 칸을 읽는다** (`DbgUiConvertStateChangeStructureWorker`): `+0x1b ~ +0x24` `[+0x08]` → `dwProcessId` · `[+0x10]` → `dwThreadId` / `+0x22d` NewState 3 → 이벤트 코드 3 (`[+0x30]` hFile · `[+0x18]` hProcess · `[+0x20]` hThread · `[+0x38]` · `[+0x40]` · `[+0x44]` · `[+0x50]`) / `+0x125` NewState 9 → 6 (`[+0x18]` hFile · `[+0x20]` · `[+0x28]` · `[+0x2c]` · `[+0x30]` lpImageName) / `+0x157` 2 → 2 / `+0x21c` · `+0x214` 4 · 5 → 4 · 5 / `+0x1d2` 10 → 7 / 6 · 7 · 8 → 1 (예외 코드 `0x4001000a` · `0x40010006` 면 8, `0x40010007` 면 9) / 그 밖 `+0x11e 0xc0000001`. **커널 쪽과 유저 쪽을 따로 읽어 오프셋이 칸마다 맞는다** (결과서 3-3b). `DbgUiConvertStateChangeStructure +0x0 XOR R8D,R8D · +0x3 JMP Worker`.

### 9. `NtDebugContinue` · status (물음 3 끝) [정적]

| 줄 | 하는 일 |
|---|---|
| `+0x23 ~ +0x4f` | 유저면 ClientId 포인터 읽어 봄 · 16 B 지역 복사 |
| **`+0x5c`** · **`+0x19c ~ +0x1d2`** | ContinueStatus = `0x10002` · `0x80010001` · `0x10001` · `0x40010001` · `0x40010003` · `0x40010004` 중 하나 |
| `+0x1d8` | 아니면 **`0xc000000d`** — **핸들 열기 전** |
| `+0x90` | `ObReferenceObjectByHandle(RCX, 1, …)` |
| `+0xcb` · **`+0xda ~ +0xe9`** | `UniqueProcess` 같고 · `UniqueThread` (`+0x30`) 같고 · **`Flags & 1` (READ)** 인 항목 |
| `+0xef ~ +0x17c` | 목록에서 뺌 (링크 검사 실패면 `INT 0x29`) |
| `+0xd5 ~ +0xd8` · `+0x108 ~ +0x119` | 찾은 뒤 같은 PID 의 다음 항목: `Flags &= ~4` → `+0x114 KeSetEvent(객체)` → **하나만 풀고 끝** |
| `+0x12a ~ +0x12d` · `+0x16b` | 못 찾으면 **`0xc000000d`** |
| **`+0x13b`** · **`+0x141`** | `[이벤트+0x84] = ContinueStatus` · `[이벤트+0x48] = 0` |
| **`+0x148`** | `DbgkpWakeTarget(이벤트)` → 반환 0 |

| 함수 | status | 언제 |
|---|---|---|
| `NtWaitForDebugEvent` | 0 | 골라 출력 |
| 〃 | `0x102` | 시간 끝 (`+0x321`, 또는 시간 0 이면 첫 기다림이 돌려줌) — 출력 0 |
| 〃 | `0x101` · `0xc0` | alert · 유저 APC (Alertable 일 때) — 출력 0 |
| 〃 | `0xc0000354` | 객체 닫는 중 (`+0x2dc`) |
| 〃 | 핸들 참조 거절 | 출력 안 씀 |
| `NtDebugContinue` | 0 | 찾고 깨움 |
| 〃 | `0xc000000d` | 값이 여섯 밖 (`+0x1d8`) **또는** READ 인 그 ClientId 없음 (`+0x16b`) — **같은 코드라 구분 못 함** |
| `DbgkpQueueMessage` (보낸 스레드가 받음) | `0xc0000353` | `DebugPort` 0 · `Skip*Msg` 로 건너뜀 (`+0x15b`) |
| 〃 | `0xc0000354` | 객체 닫는 중 · 디버거가 닫아 깨움 |
| 〃 | `0xc000009a` | NOWAIT 풀 할당 실패 (`+0x2a4`) — 기다림 길엔 없음 |

- status 이름 (`STATUS_TIMEOUT` · `STATUS_ALERTED` · `STATUS_USER_APC` · `STATUS_DEBUGGER_INACTIVE` · `STATUS_PORT_NOT_SET` · `STATUS_INVALID_PARAMETER`) 은 `[추측: 이름]`, 값은 asm 상수.

### 호출 관계

```
(부모 스레드 — CreateProcessA(…, DEBUG_PROCESS))
kernelbase!CreateProcessInternalW  +0x809 CreationFlags & 3 → DbgUiConnectToDbg · DbgUiGetThreadDebugObject → 속성 {0x60001, 8, 핸들}
 ═▶ nt!NtCreateUserProcess
     ├─ +0x29a  PspBuildCreateProcessContext      +0x17a3 크기 8 (아니면 +0x1cd 0xc000000d) · +0x17b2 문맥+0x90 = 핸들 (안 엶)
     ├─ +0xc36  PspAllocateProcess                (안에서 MmCreatePeb — PEB 지음)
     └─ +0xfc2  PspInsertProcess                  5번째 = 핸들, 4번째 = NtCreateUserProcess 7번째 인자
             ├─ +0x2f8  ObReferenceObjectByHandle(핸들, 2, DbgkDebugObjectType)   핸들 ≠ 0 일 때만 (실행은 +0x13e 앞)
             ├─ +0x143  DbgkCopyProcessDebugPort                +0x27 DebugPort = 0
             │        │  (핸들 없음) +0x3c 만든 쪽 DebugPort · +0xd0 NoDebugInherit → 물려받기
             │        │  +0x2c89e1 DebugPort = 객체 (객체 Flags bit 0 꺼짐)
             │        └─ +0x2c8a11 DbgkpMarkProcessPeb          +0x84 PEB.BeingDebugged = 1 (+0xca PEB32)
             ├─ +0x313  ObfDereferenceObject
             │  +0x322  Flags.NoDebugInherit (붙음 + 4번째 인자 bit 1)
(자식 첫 스레드 — 유저로 가기 전)
nt!PspUserThreadStartup
 ├─ +0xa1  PspNotifyThreadCreation
 │        └─ +0x21  DbgkCreateThread             +0x85 Flags |= 0x400001 · +0x9c DebugPort 0 이면 끝 · +0x2b3 옛 CreateReported
 │                 ├─ (첫 스레드 — 조각)
 │                 │   ├─ +0x207a05 DbgkpSectionToFileHandle  → +0x82 ZwOpenFile
 │                 │   ├─ +0x207a41 RtlImageNtHeader
 │                 │   ├─ +0x207adb DbgkpSendApiMessage(P, 0, ① CreateProcess)   ── 답 올 때까지 멈춤
 │                 │   ├─ +0x207aef ObCloseHandle (남았으면)
 │                 │   └─ +0x207b00 DbgkSendSystemDllMessages(0, 0, msg)
 │                 │            └─ +0x2e7 DbgkpSendApiMessage(P, 3, ② LoadDll ntdll)  ── 멈춤 (WoW64 면 한 번 더)
 │                 ├─ (그 밖 스레드) +0x320 DbgkpSendApiMessage(P, 1, CreateThread)   HideFromDebugger 꺼짐일 때
 │                 └─ (ClonedThread) +0x33d DbgkpPostModuleMessages → DbgkPostModuleMessage +0x153 DbgkpSendApiMessage(3)
 └─ +0xb3  PspInitializeThunkContext  ═▶ 유저 LdrInitializeThunk
nt!DbgkpSendApiMessage                          +0x79 ReturnedStatus = 0x103
 ├─ +0x6a  DbgkpSuspendProcess → +0x1e PsFreezeProcess        (Flags bit 0 · 자기 프로세스)
 ├─ +0x8c  DbgkpQueueMessage                    +0x270 객체 = DebugPort · +0x162 꼬리에 끼움
 │        ├─ +0x183 KeSetEvent(EventsPresent)
 │        └─ +0x1f5 기다림 (이벤트+0x10, 커널 · 비경보 · 무한)   ◀── 깨움: DbgkpWakeTarget +0x56
 ├─ +0x9a  DbgkpResumeProcess → +0xf PsThawMultiProcess
 │  +0x9e  ReturnedStatus == 0x40010001 이면 다시 보냄
(디버거 스레드)
ntdll!DbgUiWaitStateChange  +0xf Alertable 1 · +0x11 TEB+0x16a8 ─JMP▶ NtWaitForDebugEvent
nt!NtWaitForDebugEvent                          +0x4d 지역 0xb8 = 0
 ├─ +0x104 ObReferenceObjectByHandle(핸들, 1)
 ├─ +0x131 기다림 (객체, Alertable, Timeout)
 ├─ +0x221 이벤트 끄기 (빈 큐)
 ├─ +0x28f DbgkpConvertKernelToUserStateChange  → +0x28f READ
 └─ +0x2bb DbgkpOpenHandles  → ObOpenObjectByPointer (0x12067b · 0x12007b) · ObDuplicateObject · ObCloseHandle
nt!NtDebugContinue                              +0x5c / +0x19c ~ +0x1d8 여섯 값
 ├─ +0x95  ObReferenceObjectByHandle(RCX, 1)
 ├─ +0x119 KeSetEvent(객체)                     같은 PID 다음 이벤트 하나 INACTIVE 풀 때
 └─ +0x14d DbgkpWakeTarget  → +0x56 KeSetEvent(이벤트+0x10)
```

- `+0x..` 는 ret 자리. 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 쓰는 줄. 기다림 두 곳의 이름은 장부 `alias:KeWaitForMutexObject+1` (같은 주소 이름 `KeWaitForSingleObject`).

## 확인된 것

- (`[정적]`, 위 1 · 2절) 속성 `0x60001` 은 `PspBuildCreateProcessContext` 에서 저장만, `PspInsertProcess +0x2f3` 이 접근 2 로 열고 `+0x13e` 가 붙인다. `DebugPort` 쓰기 `DbgkCopyProcessDebugPort +0x2c89e1` · `BeingDebugged` 쓰기 `DbgkpMarkProcessPeb +0x84`, 둘 다 `PspInsertThread` 앞.
- (`[정적]`, 3 · 4절) 첫 이벤트는 자식 첫 스레드가 `PspUserThreadStartup +0x9c` (유저로 가기 앞) 에서: ① CreateProcess (Flags 0) → ② LoadDll ntdll (Flags 3). 깃발 셋은 PDB 칸 이름으로 닫힘.
- (`[정적]`, 6절) 보낸 스레드는 스택 이벤트를 큐 꼬리에 끼우고 `DbgkpQueueMessage +0x1f0` 에서 커널 모드 · 비경보 · 무한으로 멈춤. 깨움은 `DbgkpWakeTarget +0x51` 하나.
- (`[정적]`, 7 ~ 9절) 고르기 · 빈 큐 · 0xB8 칸 · 이어가기 규칙. 0xB8 칸은 커널 (`DbgkpConvertKernelToUserStateChange`) 과 유저 (`DbgUiConvertStateChangeStructureWorker`) 가 칸마다 같다.
- (`[관찰]` 값 대조, run44 사진) test.exe 는 여덟 장 모두 `DebugPort (null)` · `BeingDebugged 0` — 이 길은 `+0x27` 0 쓰기와 `DbgkCreateThread +0x9c` 끝만 지나간다.
- (`[정적]`) PE_emulator 팀원 주석 `0xB8` · ContinueStatus 여섯 값 **둘 다 맞다** (끝 절).

## flow-writer 대조 (결과서와 다르게 적은 곳 — 결론 바꾸는 어긋남 없음)

1. **조각 기준 → 함수 기준** (결과서가 조각 시작 기준으로 적은 곳, 오케스트레이터 주의 그대로): `DbgkpOpenHandles` 프로세스 핸들 "조각 `FUN_140b39ca6 +0x10 ~ +0x2d`" = 함수 기준 **`+0xeab1a ~ +0xeab37`** (call `+0xeab32`), 파일 핸들 복제 "조각 `+0x73`" = **`+0xeab7d`** · `DbgkpCloseObject` "조각 `+0x18`" (MarkPeb) = **`+0x26d2da`** · "조각 `+0x2a`" (PsTerminateProcess) = **`+0x26d2ec`** · `DbgkSendSystemDllMessages` "조각 `FUN_140b36a80 +0xa`" (TEB `+0x28` 지움) = **`+0x109cfe`**. `DbgkCopyProcessDebugPort +0x2c89e1` · `+0x2c8a0c` · `+0x2c8948 ~ +0x2c8978` · `DbgkpConvertKernelToUserStateChange +0xeaa78 ~ +0xeaa9e` 는 결과서도 함수 기준 (`.asm` 의 `(= 함수 +0x…)` 표시와 같음).
2. `DbgkpOpenHandles` CreateThread 스레드 핸들 "`+0x91 ~ +0xb5`" → call 은 **`+0xb9`** (ret `+0xbe`), `+0x91 ~ +0xb6` 은 인자 채우기. CreateProcess 스레드 핸들 "`+0xd6 ~ +0x105`" → 접근 `+0xe6` · call **`+0xfe`** (ret `+0x103`).
3. `DbgkCreateThread` 사전 줄 "`+0x7d ~ +0x91`" → `+0x77 MOV EAX,[RDI+0x1f4]` ~ `+0x85 CMPXCHG.LOCK` ~ `+0x8f MOV R12D,EAX` · `+0x92`. 깃발 줄 "`+0x2c4 MOV EAX,[R14+0x5a0]`" → `MOV` 은 `+0x2bd`, `+0x2c4` 는 `TEST AL,0x4` (원문 · 요청서 표기가 맞음). `+0x327` 도 `MOV` 은 `+0x320`.
4. `NtWaitForDebugEvent` 핸들 참조 "`+0xd9 ~ +0xff`" → call `+0xff` (ret `+0x104`). 빈 큐 이벤트 끄기 call 은 `+0x21c` (장부 `alias:KeClearEvent+1`, 같은 주소 이름 `KeResetEvent`). `DbgkpSendApiMessage` 큐 부름 "`+0x84 ~ +0x8c`" → call `+0x87`.
5. (보탬) `DbgkSendSystemDllMessages` 는 첫 인자 (객체) 로 두 길: 0 이면 `+0x2e2 DbgkpSendApiMessage(3)` (이번 길), ≠ 0 이면 `+0x264 DbgkpQueueMessage(…, Flags 2, 객체)` (NOWAIT, attach 쪽) — `+0x248 TEST R13 · +0x24b JZ +0x2dd`.
6. (보탬) `PspInsertProcess` 의 "만든 쪽 프로세스" = `+0x40 R13 = [현재 스레드+0xb8]` = `KTHREAD.ApcState.Process` (`[정적] PDB`) · 4번째 인자 = `NtCreateUserProcess` 7번째 인자 (프레임 셈, 1절 e).

## 앞 run 고쳐 읽기

- **run50 · run113 · flow.md 2.1 · baggage.md 3.1 / 3.11 의 "부모 `DebugPort` 복사"** → 정확히는 **`NtCreateUserProcess` 를 부른 현재 프로세스** (`PspInsertProcess +0x40` · `+0x138 MOV RDX,R13`) 의 `DebugPort` 를, **핸들 속성이 없을 때만**, 그쪽 `NoDebugInherit` 가 꺼졌을 때 물려받는다. cmd → test.exe 에선 부모 = 현재 프로세스라 값은 같다. 핸들 속성이 있으면 그 객체를 쓰고 만든 쪽 것은 안 본다 (`+0x37 TEST R8 · +0x3a JNZ +0x6b`). 틀린 건 아니고 좁힘 — 이전 문서 맨 위 줄은 안 넣음, flow.md 2.1 · 짐 지도 두 줄에 한 마디.
- **run113 (와 flow.md 2.1 run113 글머리) 의 "`DbgkpMarkProcessPeb` (ret `+0x2c8972`)"** → 이 수는 **조각 `FUN_140845053` 시작 기준**. 함수 기준으로는 **call `+0x2c8a0c` · ret `+0x2c8a11`** (`.asm` 줄 `frag:FUN_140845053+0x2c896d … (= DbgkCopyProcessDebugPort +0x2c8a0c)`). 같은 문서 · 같은 줄에서 `+0x3c` · `+0x43` (함수 기준) 과 섞여 있었다. run113 문서 맨 위에 한 줄, flow.md 2.1 글머리를 고쳤다. (run113 해설서도 같은 수를 쓴다 — explainer 몫, 안 고침.)
- **run50 "test.exe 가 안 타는 큰 갈래" 표 "디버그 포트"** 줄은 그대로 맞다 (`+0x2f3` 핸들 갈래 · 복사 갈래 둘 다 안 탐, 사진 `DebugPort` 0). 이번에 그 두 갈래의 **실행 순서** (핸들 열기 `+0x2f3` 이 `+0x13e` 보다 먼저) 와 그 뒤 일 (`+0x2c89e1` · `+0x2c8a0c` · `+0x322`) 이 보태졌다.
- **baggage.md 3.11 `Flags.NoDebugInherit` 줄의 "`PspInsertProcess` 인자 6 비트 1"** (sast/44 결과서 표기) → `PspInsertProcess` 로 세면 **4번째 인자** (`+0x0 MOV [RSP+0x20],R9D` · `+0x318 TEST [RBP+0x58],2`). 그 값이 `NtCreateUserProcess` 의 7번째 (0 부터 6) 인자라 "인자 6" 은 그 셈일 수 있다 `[추측: 셈법]`. 짐 지도 셈과 무관 — 오케스트레이터 판단 거리로 보고만.

## 짐 지도 반영 (`baggage.md` 두 줄 — 셈 안 바꿈)

- test.exe 는 디버그 속성 없이 뜨므로 3장 셈 · 2장 숫자 그대로.
- 3.1 `_PEB +0x2 BeingDebugged` 줄 끝에: (run154) 쓰는 경로 `PspInsertProcess +0x13e → DbgkCopyProcessDebugPort +0x2c8a0c → DbgkpMarkProcessPeb +0x84` (핸들 속성 `0x60001` 이면 `+0x2f3` 이 먼저 엶) · 뗄 때 `DbgkpCloseObject +0x26d2da` · "부모" = 만든 쪽 (현재) 프로세스 · `+0x2c8972` 은 조각 기준.
- 3.11 `_EPROCESS +0x308 DebugPort` 줄 끝에: (run154) 객체를 쓰는 줄 `+0x2c89e1` 의 조건 (객체 닫는 중 아님 · 객체 뮤텍스) · 그 뒤 읽는 쪽 `DbgkCreateThread +0x9c` · `DbgkpQueueMessage +0x270`.

## 아직 확인 필요

- **동적 — 디버거 필요** (결과서 "VM 으로 확인할 것 — 디버거 필요", **새 시험 exe** 필요: 타깃 VM 에서 `CreateProcessA(자식, DEBUG_PROCESS)` 로 자식 하나 띄우고 `WaitForDebugEvent` 루프 몇 번. 멈춤점은 시험 exe / 자식일 때만 — 조건은 dast 에서):
  1. `nt!PspInsertProcess+0x2f3` → `+0x2f8` `rcx` · `edx` (2) · `[rbp-0x30]` (객체)
  2. `nt!PspInsertProcess+0x13e` `r8` · `rdx` · `rcx` → 돌아온 뒤 `dt nt!_EPROCESS <자식> DebugPort Peb` · `!peb` `BeingDebugged`
  3. `nt!DbgkpMarkProcessPeb+0x84` `rax` · `cl`
  4. `nt!PspUserThreadStartup+0x9c` `!thread` · `!process` — 보내는 스레드가 자식 첫 스레드인가 · 유저 코드 전인가
  5. `nt!DbgkpSendApiMessage` 입구 `edx` · `dwo(@r8+0x28)` · `dd @r8 L0x44` — **이벤트 차례 2 → 5 → …** · Flags `0 · 3`
  6. `nt!DbgkpQueueMessage+0x1f0` `dq @r14 L0x2d` · `!thread` — 스택 이벤트 칸 · wait 상태
  7. `nt!NtWaitForDebugEvent+0x2bb` `dq @rsp+0x60 L0x17` · `!handle` — **0xB8 실제 바이트 · 접근 `0x12067b` / `0x12007b`**
  8. `nt!DbgkpWakeTarget+0x51` `rbx` · `dwo(@rbx+0x4c)` · 콜스택
  9. `nt!NtDebugContinue+0x13b` `ebx` · `rsi` — 유저 `ContinueDebugEvent` 가 넘기는 값
  - 조각 줄은 WinDbg 가 이름으로 못 보여 줄 수 있음 `[추측]` → 절대 주소 `nt+0xb0d995` (DebugPort 쓰기) · `nt+0xb0d9c0` (MarkPeb 부르기) (결과서).
- **동적 — 디버거 없이 떠 올 수 있는 것** (유저 모드 시험 exe 만으로): `WaitForDebugEvent` 이벤트 코드 차례 · `dwProcessId` / `dwThreadId` · `CREATE_PROCESS_DEBUG_INFO` (`lpStartAddress` = 기준 + 진입점인가) · `LOAD_DLL_DEBUG_INFO.lpImageName` 문자열 · 받은 핸들 `GrantedAccess` (`NtQueryObject`) · 자식 `ProcessDebugPort` ≠ 0 · 자식 PEB `+0x2` = 1 · `NtWaitForDebugEvent` 직접 호출 (Timeout 0) 로 0xB8 16진 · `ContinueDebugEvent` 값별 결과 · 디버그 객체 핸들을 닫았을 때 자식이 끝나나.
- **정적으로 남은 것** (결과서 "다음 요청 후보"): **1** attach 길 (`NtDebugActiveProcess` · `DbgkpSetProcessDebugObject` — INACTIVE 푸는 곳 · NOWAIT · `DbgkpPostFakeProcessCreateMessages` · `…FakeThreadMessages`) **2** 첫 이벤트 뒤 (`DbgkForwardException` · `DbgkExitThread` · `DbgkExitProcess` · `DbgkUnMapViewOfSection` · `NtRemoveProcessDebug` · `DbgkClearProcessDebugObject`) **3** 스레드 플래그 켜는 곳 (`ClonedThread` · `HideFromDebugger` · `SkipCreationMsg` — `NtCreateThreadEx` 생성 플래그 → `PspAllocateThread`, `NtCreateUserProcess` 첫 스레드) **4** ntdll 나머지 (`DbgUiContinue` · `DbgUiConnectToDbg` · `DbgUiGetThreadDebugObject` · `…StructureEx` R8B · `DbgUiRemoteBreakin` · `LdrpInitializeProcess` 의 `BeingDebugged` 분기 · ntdll PDB `DBG_STATE` 이름).
- `[공백]`: 자식 첫 스레드가 **언제 처음 스케줄되나** (kernelbase `NtResumeThread` 줄 · `CREATE_SUSPENDED` 여부 — 이번에 안 봄) · `ClonedThread` 를 켜는 곳 · 이벤트 `Flags 0x10` · `0x40` 읽는 곳 · NOWAIT INACTIVE 처음 푸는 곳.
- `[추측]`: `PsFreezeProcess` 가 호출 스레드를 안 얼림 · `DbgkpCloseObject` 의 `R9` = 프로세스 안 핸들 수 · `ObOpenObjectByPointer` KernelMode 접근 검사 건너뜀 · kernelbase 내부 플래그 bit 1 → `NtCreateUserProcess` 7번째 인자 bit 1.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (VM 안 함).
- 도구 메모: `ms/decomp/ntoskrnl/NtDebugContinue.c` 는 첫 인자를 잘못 그린다 (Ghidra 인자 추정) — `.asm` 을 근거로. 결과서가 짚었고 이 문서도 `.asm` 만 씀.
- 표기 메모: 조각 줄은 `.asm` 에 두 기준 (`frag:FUN_…+0x..` 조각 기준 · `(= 함수 +0x..)` 함수 기준) 이 같이 찍힌다. run113 처럼 한 줄에 섞이기 쉽다 → 문서엔 함수 기준만, 조각 기준은 괄호에. (하네스 제안 거리 — "windbg-kd-runbook pitfalls" 가 아니라 flow-doc 표기 규칙 쪽.)

## PE_emulator 쪽 — stub 이 해야 할 최소 (원문 380 ⑥ "받을 꼴")

> 읽은 PE_emulator 줄 (읽기만, 고칠지는 팀 결정): `include/syscall_layer/debug.hpp:8` · `src/exception/nt_debug_more.cpp:71` · `:94` · `:119` · `src/exception/nt_create_debug_object.cpp:17` · `:40` · `src/process/nt_create_user_process.cpp:169` (모두 `ext/syscall/syscall_layer_cpp/` 아래). 요청 쪽 숫자 (r19 8 샘플 · seq 849 ~ 888) 는 에뮬레이터 로그.

### 이벤트 차례 표 (자식 첫 스레드, `PspUserThreadStartup +0x9c` 안) [정적]

| 차례 | 종류 (ApiNumber → NewState) | 보내는 줄 | `Flags` | 메시지 칸 (메시지 오프셋) | 안 보내는 조건 |
|---|---|---|---|---|---|
| 0 | (사전) | `DbgkCreateThread +0x85` 프로세스 `Flags |= 0x400001` | — | — | `+0x9c DebugPort == 0` 이면 아무것도 안 보냄 (test.exe) |
| **1** | **CreateProcess (2 → 3)** | `DbgkCreateThread +0x207ad6` | **0** (안 얼림) | 머리 `0x600038` · 8 · `+0x38` FileHandle · `+0x40` BaseOfImage · `+0x48` / `+0x4c` 헤더 `PointerToSymbolTable` / `NumberOfSymbols` · **`+0x58` 이미지 진입점** | 옛 `CreateReported` 1 (그땐 CreateThread 길). 보낸 스레드 `SkipCreationMsg` 면 `0xc0000353` 로 버려짐 |
| **2** | **LoadDll ntdll (5 → 9)** | `DbgkSendSystemDllMessages +0x2e2` | **3** (얼림 · `0x40`) | 머리 `0x28` · `0x50` · 8 · `+0x30` FileHandle · `+0x38` BaseOfDll · `+0x40` / `+0x44` · `+0x48` NamePointer = 자식 `TEB+0x28` 주소 | `PsQuerySystemDllInfo(0)` 0. 파일 열기 실패해도 핸들 0 으로 보냄 |
| 3 | LoadDll 32비트 ntdll (5 → 9) — **WoW64 자식만** | 같은 루프 (`+0x99 ~ +0x273`, `+0x2b0 PsWow64GetProcessNtdllType`) | 3 | 〃 | 64비트 자식 |
| 4 | 모듈마다 LoadDll (5 → 9) — **클론 스레드만** | `DbgkCreateThread +0x338 → DbgkpPostModuleMessages → DbgkPostModuleMessage +0x14e` | 3 | 〃 (머리 길이 · NamePointer 0) | 보통 자식 (첫 스레드는 클론 아님 `[추측]`). 로더 목록 앞 둘 건너뜀 `[추측: exe · ntdll]` |
| — | (이벤트 아님) 유저로 감 | `PspUserThreadStartup +0xae` | | | |

- 각 줄 사이에 **보낸 스레드가 멈춘다** — 앞 이벤트를 `NtDebugContinue` 로 이어 줘야 다음 이벤트가 생긴다. `ReturnedStatus == 0x40010001` 로 이어 주면 **같은 이벤트를 다시** 넣는다.
- 그 뒤 (자식이 유저로 간 뒤): 나중 DLL (`DbgkMapViewOfSection`, Flags 1) · 시작 브레이크포인트 예외 (ntdll `BeingDebugged` 분기, `[공백: 이번에 안 읽음]`) · 새 스레드 (CreateThread, Flags 1) · 끝 (ExitThread · ExitProcess) — 이번 요청 밖.

### `0xB8` 칸 표 (`NewState` 마다) [정적] 오프셋 · `[추측: 이름]`

공통: `+0x00` NewState (u32) · `+0x08` UniqueProcess · `+0x10` UniqueThread. 핸들은 **부른 디버거 프로세스의 핸들 표**에, 속성 0.

| `NewState` | 이벤트 (ApiNumber) | 칸 | 여는 핸들 · 접근 | ntdll → `dwDebugEventCode` |
|---|---|---|---|---|
| **0** | 없음 (`0x102` · `0x101` · `0xc0`) | **전부 0** | 없음 | 바꾸면 Worker `+0x11e` 가 `0xc0000001` (0 · 1 · 11 이상 모두) |
| **2** | CreateThread (1) | `+0x18` HandleToThread · `+0x20` SubSystemKey (u32) · `+0x28` StartAddress | 스레드 **`0x12007b`** (`DbgkpOpenHandles +0xb9`), 실패면 칸 0 | 2 |
| **3** | CreateProcess (2) | `+0x18` HandleToProcess · `+0x20` HandleToThread · `+0x28` SubSystemKey (u32) · `+0x30` FileHandle · `+0x38` BaseOfImage · `+0x40` DebugInfoFileOffset (u32) · `+0x44` DebugInfoSize (u32) · `+0x48` InitialThread.SubSystemKey · `+0x50` InitialThread.StartAddress | 프로세스 **`0x12067b`** (`+0xeab32`) · 스레드 **`0x12007b`** (`+0xfe`) · 파일 = 커널 핸들을 `ObDuplicateObject` (같은 접근, `+0xeab7d`) 후 **원본 닫음** (`+0x8a`), 실패면 칸 0 | 3 |
| **4** | ExitThread (3) | `+0x18` ExitStatus (u32) | 없음 | 4 |
| **5** | ExitProcess (4) | `+0x18` ExitStatus (u32) | 없음 | 5 |
| **6** · **7** · **8** | 예외 (0) — 그 밖 · `0x80000003` · `0x80000004` | `+0x18 ~ +0xb7` `_DBGKM_EXCEPTION64` (기록 0x98 + FirstChance `+0xb0`) | 없음 | 1 (코드 `0x4001000a` · `0x40010006` 면 8, `0x40010007` 면 9) |
| **9** | LoadDll (5) | `+0x18` FileHandle · `+0x20` BaseOfDll · `+0x28` DebugInfoFileOffset (u32) · `+0x2c` DebugInfoSize (u32) · `+0x30` NamePointer | 파일 복제 (`+0x7c`) · 원본 닫음 | 6 |
| **10** | UnloadDll (6) | `+0x18` BaseAddress | 없음 | 7 |

- ApiNumber 7 이상은 아무것도 안 채운다 (NewState 0 그대로, `DbgkpConvertKernelToUserStateChange +0xdd ~ +0xe1`). 이름 (`DbgCreateProcessStateChange` 등) 은 `[추측: 이름]`.

### stub 이 해야 할 최소 — 엔진 몫 (자식 짓기 · 첫 스레드 알림 자리, 레 `Src/nt/`)

1. **속성 받기** (`nt_create_user_process.cpp:169` 앞, 모르는 속성 가지 전에): `0x60001` 크기 8 아니면 `0xc000000d`, 값은 **열지 말고 저장만** (`PspBuildCreateProcessContext +0x17a3 ~ +0x17b2`).
2. **짓기 끝 (`PspInsertProcess` 자리)**:
   - 핸들 ≠ 0 → 접근 **2** 로 디버그 객체 참조 (실패 status 그대로, 짓기 실패). 핸들 = 0 → 만든 쪽 프로세스 `DebugPort ≠ 0` 이고 그쪽 `NoDebugInherit == 0` 이면 같은 객체.
   - 객체 "닫는 중" 이면 **안 붙이고 성공**. 객체 WoW 표시인데 자식이 32비트 WoW 아니면 `0xc00000bb`.
   - 붙이면: 자식 `DebugPort = 객체` · 자식 `PEB.BeingDebugged = 1` (WoW64 면 PEB32 도) · 4번째 인자 bit 1 이면 자식 `NoDebugInherit = 1`.
3. **첫 스레드 알림 (유저 코드 한 줄 전)**: `DebugPort ≠ 0` 이면 이벤트 ① → ② (WoW64 면 ③) 를 **차례로**, 하나 넣을 때마다 **보낸 스레드를 멈춰 두고** 답 (`NtDebugContinue`) 이 와야 다음을 넣는다. 답 `0x40010001` 이면 같은 이벤트를 다시. 그 밖 답 · 반환 status 는 안 본다.
   - ① 칸: ClientId = {자식 PID, 첫 TID} · FileHandle (이미지 파일 커널 핸들, 없으면 0) · BaseOfImage · 헤더 두 값 · **StartAddress = ImageBase + AddressOfEntryPoint** (32비트 WoW 면 ImageBase32 + EntryRVA).
   - ② 칸: FileHandle (ntdll 파일) · BaseOfDll (자식에 올라간 ntdll) · NamePointer = 자식 `TEB+0x28` 주소 (그 칸에 `TEB+0x1268` 를 가리키는 포인터 + 경로 글, 보낸 뒤 0) — 가상 자식에 TEB 가 없으면 이름을 못 읽는다 `[공백: 엔진 설계]`.
   - 가상 자식이 실제로 안 돈다면: 부모가 `NtWaitForDebugEvent` 를 **처음 부를 때 ① 이 이미 큐에 있어야** 진짜와 같다. ② 는 ① 을 이어 준 **뒤에** 생긴다.
4. 그 뒤 (자식이 유저로 간 뒤) 이벤트를 안 만들면 "더는 안 옴" 을 `stop("…")` 로 정직하게 남기는 편이 맞다 (결과서).

### stub 이 해야 할 최소 — 팀원 몫 (큐 · 기다림, `nt_debug_more.cpp`)

- **이벤트 칸**: `Flags` (READ 1 · NOWAIT 2 · INACTIVE 4) · ClientId · ApiNumber · ReturnedStatus · 공용체 (5절) · 보낸 쪽을 깨울 고리.
- **`NtWaitForDebugEvent`** (`:94` `attached_pids.empty()` 가지 자리):
  1. 큐 앞에서부터: `Flags & 5` 면 건너뜀 · 앞에 같은 PID 가 있으면 `Flags |= 4` 하고 건너뜀 · 고르면 `Flags |= 1` (큐에서 안 뺌).
  2. 0xB8 로 바꿔 쓰고 디버거 핸들 표에 핸들을 연다 (위 칸 표, 접근 `0x12067b` / `0x12007b`, 파일은 복제).
  3. 못 고르면 `EventsPresent` 를 끄고 기다림 — 시간 0 → `0x102`. 유저 `WaitForDebugEvent` 는 **늘 Alertable 1** (`DbgUiWaitStateChange +0xf`) 이라 지금 `alertable` 가지 (`user_alerted` · APC) 가 실제로 타는 길 — `0x101` / `0xc0` 도 출력 0 으로 채운다.
- **`NtDebugContinue`** (`:119` `return err(invalid)` 자리): PID · TID 같고 **READ 켜진** 이벤트를 큐에서 빼고 → 보낸 쪽 (엔진) 에 `ReturnedStatus = ContinueStatus` 를 주며 깨움 → 같은 PID 다음 이벤트 **하나** 의 INACTIVE 를 풀고 `EventsPresent` 를 켬. 못 찾으면 `0xc000000d`.
- **마지막 핸들 닫기**: 대기 이벤트마다 `Status = 0xc0000354` 로 깨움 · 붙은 프로세스 `DebugPort = 0` · `BeingDebugged = 0` · KillOnClose 면 프로세스 끝냄 (`0xc0000354`).

### "기다림 가짜 성공" (D155) 과 진짜의 차이 — 한 줄

진짜 `NtWaitForDebugEvent` 의 **status 0 은 늘 `NewState ≥ 2` · ClientId · 핸들이 찬 구조**이고 그 이벤트는 `NtDebugContinue` 가 올 때까지 같은 프로세스의 다음 이벤트를 막는다; 0 으로 채운 구조는 `0x102` · `0x101` · `0xc0` 일 때만 나온다 — 그래서 "status 0 + 빈 구조" (D155) 는 진짜엔 없는 조합이고, 그 뒤 `NtDebugContinue` 도 찾을 이벤트가 없어 `0xc000000d` 가 된다.

### 팀원 주석이 맞나

| 주석 (자리) | 판정 | 근거 |
|---|---|---|
| 출력 구조 `0xB8` B (`nt_debug_more.cpp` `NtWaitForDebugEvent` 주석 · `:91` `output + 0xb7` · `:100` `zeros(0xb8)`) | **맞다** | `NtWaitForDebugEvent +0x4d` · `+0xb6 ~ +0xcc` · `+0x145 ~ +0x1ad`. 머리 0x18 + `_DBGKM_EXCEPTION64` 0xa0 |
| ContinueStatus 여섯 값 · 그 밖 `invalid` (`:114`) | **맞다** (값 · 순서 · 거절이 핸들 참조 **앞**) | `NtDebugContinue +0x5c` · `+0x19c ~ +0x1d8` · `+0x90` |
| `NtDebugContinue` 순서 (ClientId 읽기 → 값 → 참조 1) | **맞다** | `+0x4a` · `+0x5c` · `+0x8b/+0x90` |
| "시간 초과일 때도 0 으로 채워 써 준다" | **맞다** (`0x101` · `0xc0` 도 같음) | `+0x1d5 ~ +0x1e9` → `+0x13c` → 0 구조 복사 |
| 핸들 접근 `DEBUG_READ_EVENT(1)` | **맞다** | `+0xf7` · `NtDebugContinue +0x8b` |
| "ClientId 로 전달된 이벤트를 찾고 없으면 invalid" | **맞다** (더: READ 가 켜져 있어야 찾음) | `+0xda ~ +0xe9` · `+0x16b` |
| "붙은 프로세스가 있으면 큐가 비었다고 말 못 함" (`:94` 멈춤) | **맞다** (진짜는 첫 스레드가 돌면 큐에 ① 이 있다) | 3절 |
| `NtCreateDebugObject` `flags & ~1` → invalid (`nt_create_debug_object.cpp:17`) | **맞다** | `NtCreateDebugObject +0x5f ~ +0x68` |

### `debug.hpp` flags 비트 메모

- 팀원 `DebugObject::flags` (`debug.hpp:8`) 는 **사용자 쪽 값** 을 그대로 둔다: `NtCreateDebugObject` 가 `object->flags = flags` (`nt_create_debug_object.cpp:40`, bit 0 = KillOnClose), `NtSetInformationDebugObject` 가 **bit 0** 을 켜고 끈다 (`nt_debug_more.cpp:71`).
- 진짜 커널 객체 `+0x60` 은 **bit 0 = 닫는 중 · bit 1 = KillOnClose · bit 2 = 만든 프로세스가 32비트 WoW** (7절). 사용자 값 1 → 커널 칸 2 (`+0xea ~ +0xf2` `<< 1`, `NtSetInformationDebugObject +0x10c OR EDX,2`).
- 지금은 내부 표현이라 문제 없다. 다만 "닫는 중" · "WoW" 를 새로 모델할 때 **`flags` bit 0 에 섞지 말 것** — 커널 배치 (1 / 2 / 4) 로 옮기거나, 칸을 따로 둔다. 붙이기 (엔진 2번) 의 "닫는 중이면 안 붙이고 성공" · `0xc00000bb` 검사가 이 두 비트를 본다.

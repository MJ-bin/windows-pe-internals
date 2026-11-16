# 115 eprocess-locks — 정적 분석 결과

- 요청서: `_workspace/sast/115_eprocess-locks.md` (Q99, 정적만)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음 — `ms/sym/ntoskrnl.exe.pdb.info.txt`). ntdll 은 **Ghidra 프로젝트를 열지 않고** 기존 `ms/disassemble/ntdll/*.asm`·`_calls.tsv`·PE 파일 바이트 (읽기만, `-I` 파이썬) 로 봄.
- 새로 뽑은 것 (이미 있는 `.asm`·`.c` 는 다시 안 뽑음. `^이름$` 로 필요한 것만, 모두 depth 0):
  - `^(MiInsertPageInList|MiInsertPagesInList|MiDetermineModifiedPageListHead)$` → 3개 (질문 3. 둘은 baggage 가 "`.asm` 없음" 으로 적어 둔 것)
  - `^MiUserFault$` → 1개 (질문 3. `+0x550` 을 **0 으로 돌리는** 쓰기가 nt.dis 전수에서 나와서 — 요청서 범위 안의 "`+0x550` 쓰기 전수" 몫)
  - 위치: `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/` (`_calls.tsv` 에 덧붙음). 로그: `ms/ghidra/ntoskrnl.export.sast115a.stdout`·`...115b.stdout`.
  - 이미 있던 것만 읽음: `RtlInsertDynamicUserFunctionTable.asm`·`NtSetInformationProcess.asm`·`PspInsertThread.asm`·`ObpLookupObjectName.asm`·`ObpReferenceCurrentDeviceMap.asm` (ntoskrnl) / `LdrpInitializeProcess.asm`·`RtlpInitCurrentDir.asm`·`RtlpCreateNewDirectoryReference.asm`·`RtlpDosPathNameToRelativeNtPathName.asm` (ntdll).
- 버전 확인: **일치.** `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 에 그대로 있음 (스킬 "알려진 사실" 둘). 그리고 사진 값 (run44 덤프 읽기 로그) 과도 맞음 — 아래.
- 스크립트 문제: 없음.

## 한 줄 요약

1. **두 락 모두 "잡았다 놓고 0 으로 돌아옴" 이 코드로 맞다.** 가름을 옮겨도 된다: `InvertedFunctionTableLock` (+0x5d0) → ② 에서 쓰고 0 으로, `ThreadListLock` (+0x5e0) → ① 에서 쓰고 0 으로.
   - 둘 다 `ExAcquirePushLockExclusiveEx`·`ExReleasePushLockEx` 를 **부르지는 않는다.** 같은 일을 함수 안에 펼쳐 놓은 꼴이다: `KeAbPreAcquire` 부름 → `BTS.LOCK` (0 → 1) … `XADD.LOCK −1` (1 → 0) → `KeAbPostRelease` 부름. 느린 길 (`ExfAcquirePushLockExclusiveEx`·`ExfTryToWakePushLock`) 은 남이 같은 락을 쥐고 있을 때만.
2. **`DeviceMap` 을 처음 채우는 ntdll 호출자**: `LdrpInitializeProcess +0x1517` (ret `+0x151c`) `RtlpInitCurrentDir` → (`+0x32/+0x37`) `RtlpCreateNewDirectoryReference` → (`+0xbb/+0xc0`) **`NtOpenFile` (syscall `0x33`)**, 이름은 현재 디렉터리의 `\??\C:\…`. 사진 구간 `P1 ~ P1a` (P1a = `+0x199d` 라 `+0x1517` 이 앞) 과 맞다.
3. **`ModifiedPageCount` (+0x550)**: 올리는 줄 둘 (`MiInsertPageInList +0x1fc`, `MiInsertPagesInList +0x64a`), **0 으로 돌리는 줄 하나 (`MiUserFault +0x116` — baggage 에 없던 것)**. 목록은 파티션의 **변경 목록 (`ModifiedPageList` = 3)**. 이 빌드엔 `MmModifiedPageListHead` 라는 전역 이름이 없다 (PDB 에 없음) — 목록 머리는 `_MI_PARTITION` 안 칸이다.

## 질문별 답 [정적]

### 1. 두 락이 초기화 길에서 잡혔다 놓이나

#### 1-가. `InvertedFunctionTableLock` (`_EPROCESS +0x5d0`, `_EX_PUSH_LOCK` 8바이트)

- 답: **잡았다 놓는다. 한 번 부를 때마다 0 → 1 → 0.** 클래스 `0x35` 길은 `NtSetInformationProcess` 가 아니라 그 callee `RtlInsertDynamicUserFunctionTable` 안에서 락을 잡는다.
- 근거 (`ms/disassemble/ntoskrnl/NtSetInformationProcess.asm`): 클래스 `0x35` 갈래 `+0x394 CMP EBX,0x35` → `+0x52d CMP R10,-1` (핸들 −1) → `+0x537 CMP R13D,0x10` (길이 16) → `+0x55c CMP byte [버퍼+8],0` — 0 이면 **`+0x566` (ret `+0x56b`) `RtlInsertDynamicUserFunctionTable`**, 아니면 `+0x584` (ret `+0x589`) `RtlRemoveDynamicUserFunctionTable`. 이 함수 안엔 락이 없다.
- 근거 (`RtlInsertDynamicUserFunctionTable.asm`, 락 주소 = 부른 스레드의 프로세스 `[스레드 +0x220] + 0x5d0`):
  - 들어가기: `+0x7b MOV RAX,GS:[0x188]` → `+0x8c MOV R15,[RAX+0x220]` (프로세스) → `+0x9f ADD word [RAX+0x1e6],−1` (PDB `_KTHREAD +0x1e6 SpecialApcDisable` −1 = 가드 영역 들어가기를 펼친 꼴).
  - 잡기: `+0xa7 LEA RDI,[R15+0x5d0]` → `+0xb4` (ret `+0xb9`) `KeAbPreAcquire` → **`+0xbc BTS.LOCK [RDI],0`** (0 → 1) → 이미 켜져 있었으면 (`+0xc2 JNC` 안 탐) `+0xcd` (ret `+0xd2`) `ExfAcquirePushLockExclusiveEx` → `+0xdd MOV [R14+0xa],R12B` (추적 항목 표시).
  - 락 안에서 하는 일: `+0xe1 MOV RDI,[R15+0x5c8]` (표 읽기 — 없으면 `+0x2d0` 으로) → 모자라면 `+0x1bf` (ret `+0x1c4`) `ExAllocatePool2` (`0x10 + 24×N` 바이트, 태그 `0x46447452`) → 복사 `+0x1f4` (ret `+0x1f9`) → 머리 칸 → **`+0x20c MOV [RAX+0x5c8],RDI`** (표를 프로세스에 꽂음) → 항목 끼우기 `+0x299` (ret `+0x29e`) · `+0x29e ~ +0x2bf`.
  - 놓기: 모든 길이 `+0x115` 로 모여 **`+0x125 XADD.LOCK [RDI+0x5d0],RBX`** (`+0x9b OR RBX,−1` 이라 −1 → 1 → 0) → `+0x12e AND BL,6` · `+0x131 CMP BL,2` · `+0x134 JZ +0x2fb` (대기자가 있을 때만 `+0x302` (ret `+0x307`) `ExfTryToWakePushLock` 하고 `+0x13a` 로) → `+0x141` (ret `+0x146`) `KeAbPostRelease` → `+0x14f ADD word [R8+0x1e6],+1` (가드 영역 나오기, 남은 APC 있으면 `+0x166` (ret `+0x16b`) `KiCheckForKernelApcDelivery`) → 옛 블록이 있었으면 `+0x178` (ret `+0x17d`) `ExFreePoolWithTag` (**락 풀고 난 뒤**).
  - 락 앞에서 끝나는 길 (잡지도 않음): `+0x19 JNZ +0x5a` 정렬 틀림 (`ExRaiseDatatypeMisalignment`) · `+0x60` 범위 오류 `0xc0000095`.
- 0 으로 돌아오나: 대기자가 없으면 **돌아온다** (`BTS` 로 비트 0 만 켬 → `XADD −1` 로 끔; 중간 길 모두 `+0x125` 를 지남). 사진도 맞다 (아래).
- test.exe 가 이 길을 타나: **탄다** `[정적 + 관찰]`. `+0x5c8` 가 `P1` 에선 0, `P1a` 에선 `ffffb689'b064d820` (`ms/logs/04_baggage-read_P1a.txt:489`) — 표는 `+0x20c` 에서만 꽂힌다. 락 칸은 8장 모두 0 (`K3` 라인 492 · `P1` 492 · `P1a` 490 · `P1b` 490 · `P2` 490 의 첫 8바이트; 같은 줄 둘째 8바이트는 `ActiveThreadsHighWatermark`·`LargePrivateVadCount`). 호출 횟수는 run63 이 정한 "ntdll·kernel32·kernelbase 세 번 `[추측: 개수]`" 그대로 — 매번 0 → 1 → 0 쌍.
- 가름: **"② 에서 쓰고 0 으로 돌아옴"** 으로 옮길 수 있다 (`끝까지 0` 인 아는 칸 — "썼다 되돌림").
- 지금 우리 (PE_emulator): **맞음.** `ProcessObject::dynamic_function_tables` 는 락 값을 안 두고 (`ext/syscall/syscall_layer_cpp/include/syscall_layer/processes.hpp:29~31` 주석이 `+0x5d0` 을 짚고 있음) 끝 값이 0 이라 같다. EPROCESS 날 바이트를 만들어 비교하는 쪽이 있다면 `+0x5d0` 을 0 으로 두면 된다 (그 쪽 여부는 확인 필요).

#### 1-나. `ThreadListLock` (`_EPROCESS +0x5e0`)

- 답: **잡았다 놓는다 (0 → 1 → 0).** 모든 스레드가 같은 길로 지난다 (첫 스레드·둘째 스레드 모두).
- 근거 (`PspInsertThread.asm`, R15 = 프로세스 인자, R14 = 스레드 인자):
  - 잡기 앞 갈림 셋이 모두 `+0x6ac` 로 모인다: `+0x696 JZ +0x78a` (`ActiveThreads == 1`) → `+0x799 JMP +0x6ac` (또는 `+0x78c JNZ +0x69c`), `+0x6a6 JZ +0x76a` (`== 2`) → `+0x785 JMP +0x6ac`, 아니면 그냥 아래로. (`ActiveThreads` = `[R15+0x380]`, `+0x657 ~ +0x65a` 에서 이미 올림.)
  - 잡기: **`+0x6ac LEA R12,[R15+0x5e0]`** → `+0x6bb` (ret `+0x6c0`) `KeAbPreAcquire` → **`+0x6c3 BTS.LOCK [R12],0`** → (남이 쥐고 있을 때만 `+0x6cc ~ +0x6d5` (ret `+0x6da`) `ExfAcquirePushLockExclusiveEx`) → `+0x6df MOV [RBX+0xa],R13B`.
  - 락 안: `+0x6e3 LEA RCX,[R14+0x578]` (`ETHREAD.ThreadListEntry`) · **`+0x6ea LEA RAX,[R15+0x370]`** (`ThreadListHead`) · `+0x6f1 MOV RDX,[RAX+8]` · `+0x6f5 CMP [RDX],RAX` · `+0x6f8 JZ` → 목적지 `0x14084378d − 0x140843554 =` **`+0x239`** (맞음). 안 맞으면 `+0x6fe ~ +0x703 INT 0x29` (코드 3, 목록 깨짐). 달기 네 줄 `+0x239 ~ +0x243`.
  - 놓기: `+0x247 OR RBX,−1` · `+0x24b MOV RAX,RBX` · **`+0x24e XADD.LOCK [R12],RAX`** (1 → 0) → `+0x254 AND AL,6` · `+0x256 CMP AL,2` · `+0x258 JZ` → 목적지 `0x140843e06 − 0x140843554 =` **`+0x8b2`** → `+0x8b5` (ret `+0x8ba`) `ExfTryToWakePushLock` → `+0x8ba JMP +0x25e` / 대기자 없으면 바로 `+0x25e MOV RCX,R12` → **`+0x261` (ret `+0x266`) `KeAbPostRelease`**.
  - 이 함수엔 가드·크리티컬 영역 들어가기·나오기 (`+0x1e4/+0x1e6` 가산) 가 없다 — 그 몫은 부른 쪽 `[추측]`.
- 사진: `+0x5e0` 첫 8바이트 = 0 (`K3` 라인 493 · `P0` 493 · `P1` 493 · `P1a` 491 · `P1b` 491 · `P2` 491. 같은 줄 둘째 8바이트는 `WnfContext` `+0x5e8`). 목록 머리 `+0x370` 은 `K3` 에서 `ffff8285'984455f8` (첫 스레드, 라인 454), `P1b` 에서 `Blink` 가 `ffff8285'997625f8` (둘째 스레드, 라인 452) 로 바뀌었는데 락은 둘 다 0 — 잡았다 놓은 뒤 값과 맞다.
- 가름: **"① 에서 쓰고 0 으로 돌아옴"** 으로 옮길 수 있다. 줄: `PspInsertThread +0x6ac / +0x6c3 → +0x24e → +0x261`.
- 지금 우리 (PE_emulator): **맞음.** `ext/syscall` 소스에 `ThreadListLock` 이름이 없다 (grep) — 스레드 목록을 락 없이 두므로 끝 값 0 과 같다. EPROCESS 날 바이트 비교가 있다면 0 (그 쪽 여부는 확인 필요).

### 2. `DeviceMap` 을 처음 채우는 ntdll 쪽 호출자

- 답: **`NtOpenFile` (syscall `0x33`) — `\??\C:\…` 현재 디렉터리 열기.** 사슬은 `②… → LdrpInitializeProcess (호출 +0x1517 / ret +0x151c) → RtlpInitCurrentDir (호출 +0x32 / ret +0x37) → RtlpCreateNewDirectoryReference (호출 +0xbb / ret +0xc0) → NtOpenFile`. 사진 구간 **`P1 ~ P1a`** (`P1a` 는 `LdrpInitializeProcess +0x199d`, `+0x1517` 은 그 앞).
- 근거:
  - `LdrpInitializeProcess.asm +0x1517 CALL 0x1800070e8 ; ret=+0x151c -> RtlpInitCurrentDir` (`+0x150f LEA RCX,[RSP+0xb0]` = 현재 디렉터리 글).
  - `RtlpInitCurrentDir.asm`: `+0x1c` (ret `+0x21`) `RtlDetermineDosPathNameType_Ustr` → 종류 1·2·6·7 (절대 경로 꼴) 이면 `+0x32` (ret `+0x37`) `RtlpCreateNewDirectoryReference` (종류 3·4·5 면 오류 `0xc000000d`, `+0x6a ~ +0x8f` 분기).
  - `RtlpCreateNewDirectoryReference.asm`: `+0x62` (ret `+0x67`) `RtlpDosPathNameToRelativeNtPathName` 이 NT 이름을 만든다 → `+0x73 / +0x7e` 옵션 `0x21` · `0x3` → `+0x94 MOV EDX,0x100020` (접근) → **`+0xbb` (ret `+0xc0`) `NtOpenFile`** (`+0xa0 MOV [RBP−1],0x30` OA 크기, `+0xab MOV [RBP+0x17],0x40` 속성, `+0xa7` RootDirectory = 0, `+0xb2` ObjectName = 앞에서 만든 이름) → `+0xf7` (ret `+0xfc`) `NtQueryVolumeInformationFile`. (run65 표 5 번 줄과 같다.)
  - **이름이 `\??\` 로 시작하나**: `RtlpDosPathNameToRelativeNtPathName` 이 `RtlpDosDevicesPrefix` (`+0x201 LEA R9,[0x180173200]`) 를 새 버퍼 앞에 복사한다 (`+0x272 MOVZX EBP,word [R9]` 길이 → `+0x28f MOV RDX,[R9+8]` 글 → `+0x2a8` (ret `+0x2ad`) 복사). ntdll 파일 바이트로 읽으면 그 칸은 `{Length 8, MaxLength 10, Buffer → "\??\"}` (rva `0x173200` → 글 `0x17aa80`). 반대편 UNC 칸 (`0x173210`) 은 `\??\UNC\`.
  - **그 이름이 `DeviceMap` 을 만들게 하는 커널 쪽**: `ObpLookupObjectName +0x143d SUB ECX,0x3f005c` (이름 앞 두 글자 `\?`) → `+0x1445 ~ +0x144d` 셋째 글자 `?` 대조 → 맞으면 `+0x145e` (ret `+0x1463`) `ObpReferenceCurrentDeviceMap` → (칸이 비었으면) `+0x286` (ret `+0x28b`) `ObpSetCurrentProcessDeviceMap`. (baggage 3.11 `DeviceMap` 줄 그대로.)
- **그 앞에 `\??\` 이름을 넘기는 syscall 이 test.exe 길에 없나** — `LdrpInitializeProcess` 의 `+0x1517` 앞 callee 를 `_calls.tsv` 로 깊이 5 까지 훑어 이름을 쓰는 syscall 을 모두 모았다 (읽기만). `NtOpenKey` (레지스트리 `\Registry\…` — 접두 `\?` 아님), `NtOpenDirectoryObject` `\KnownDlls` (`+0xdb8`), `NtOpenSymbolicLinkObject` `KnownDllPath` (`+0xe47`, 뿌리 = 위 핸들), `NtCreateEvent` 들 (이름 없음) 뿐이다. `\??\` 이름 후보는 **조건부 둘**:
  - `+0x117e` (ret `+0x1183`) `LdrpCheckAppDirType` → `NtOpenFile` (exe 경로 `\??\…`): `+0x1169 MOV EAX,[LdrpIllegalCWDDevices]` → `+0x116f DEC EAX` → `+0x1171 CMP EAX,−3` → `+0x1174 JA` (목적지 `0x18008d587 − 0x18008c404 =` `+0x1183`, 곧 `CALL` 바로 다음) — 마스크가 **0 또는 `0xffffffff` 이면 건너뜀**. test.exe 는 마스크 0 (run65 — `LdrpQueryIllegalCWDDevices` 가 0) 이라 안 탐 → 정책 마스크가 켜지면 이 줄이 **먼저** 불린다.
  - `+0x531` (ret `+0x536`) `LdrpInitializeExecutionOptions` → `LdrpInitializeApplicationVerifierPackage → AVrfInitializeVerifier → AvrfMiniLoadDll` (`NtOpenFile`·`NtQueryAttributesFile`·`NtCreateSection`): App Verifier 가 켜진 프로세스만 — test.exe 안 탐 (run105 `AvrfpLoaderEntry` 조건부).
  - `+0x1302` (ret `+0x1307`) `LdrpDrainWorkQueue` 아래 DLL 파일 길 (`LdrpMapDllNtFileName +0x132 NtOpenFile`, `LdrpGetNtPathFromDosPath +0xee NtQueryAttributesFile`): 이때 일감 큐는 아직 비어 있을 것이다 `[정적 추론]` (의존 DLL 일감은 baggage 5장 ② 순서상 `+0x1a60` 뒤 `LdrpMapAndSnapDependency` 가 넣음) — 지도상 길일 뿐. DLL 을 파일로 여는 run21·run39 길은 모두 **`P1a ~ P1b`** (kernel32 준비 `+0x19a2` 뒤) 라 `DeviceMap` 칸 시점 (`P1 ~ P1a`) 보다 늦다.
- 가름·고칠 줄: baggage 3.11 `+0x318 DeviceMap` 줄 근거 칸 "ntdll 첫 호출자 `[공백]` (F5)" → **"`[정적]` `LdrpInitializeProcess +0x1517 → … → RtlpCreateNewDirectoryReference +0xbb NtOpenFile` (sast115)"** 로, 단 "실제로 탔는지·그 호출이 정말 첫 `\??\` 인지" 는 동적 F5 로 닫는다. 5장 ② 4 번 줄의 "이 목록 어디쯤인지 `[공백]` (동적 F5)" 도 같은 자리로 (`(run65) 현재 디렉터리 +0x1517` 바로 옆).
- 지금 우리 (PE_emulator): **ntdll 순서는 맞음** (진짜 ntdll 을 돌리면 같은 길이 저절로 간다 — 단 `ProcessParameters.CurrentDirectory.DosPath` 가 절대 경로여야 `+0x2f ~ +0x32` 를 탐, run65 가 이미 적은 조건). **커널 `DeviceMap` 은 에뮬레이터가 안 둔다** (`ext/syscall` 에 `DeviceMap` 이름 없음, grep) → 칸 값을 비교하는 곳이 없으면 무관, 있으면 **확인 필요.**

### 3. `ModifiedPageCount` (`_EPROCESS +0x550`, `unsigned long`) 가 오르는 페이지 목록

- **`+0x550` 쓰기 전수** (nt.dis 에서 `+0x550]` 가 **목적지**인 명령 39개를 모두 훑고 베이스 레지스터 출처로 가름): EPROCESS 것은 **셋**.

  | 줄 | 명령 | 베이스 | 하는 일 |
  |---|---|---|---|
  | `MiInsertPageInList +0x1fc` | `INC.LOCK dword [RCX+0x550]` | `+0x1ec GS:[0x188]` → `+0x1f5 [RAX+0xb8]` (스레드 `ApcState.Process`) | 변경 목록에 **한 쪽** 넣을 때 +1 |
  | `MiInsertPagesInList +0x64a` | `ADD.LOCK dword [RCX+0x550],R12D` | `+0x63a GS:[0x188]` → `+0x643 [RAX+0xb8]` | 변경 목록에 **R12 쪽 한꺼번에** 넣을 때 +R12 |
  | **`MiUserFault +0x116`** | `MOV dword [RSI+0x550],R13D` (R13D = 0, `+0x20 XOR`) | `+0x2d MOV RSI,[R9+0xb8]` | **0 으로 돌림** (아래) |

  나머지 36개는 다른 구조체: `_ETHREAD +0x550 TopLevelIrp` (qword — `IoSetTopLevelIrp +0x9` 등 `Cc*`·`FsRtl*` 둘레), 저장소 관리자 `StDmStart +0xe6` (`[rdx+0x550]` ← `[rbx+0x12c0]` 복사), 캐시 파티션 `CcInitializePartition +0x244 / +0x24b`, 감사·추적 qword (`Etw*`·`Pop*`·`Vf*`·`Kasani*`·`MiDeleteCachedSubsection` 둘레) — 어느 것도 `[스레드+0xb8]` 에서 온 베이스가 아니다.
- **어느 줄이 올리나, 어느 목록인가**:
  - `MiInsertPageInList(PFN, 플래그 EDX)` 는 플래그로 목록을 고른다: `0x4` → 파티션 `+0xc40` = `PageLists.StandbyPageListHead` (`+0x106`) · **`0x8` → 파티션 `+0x4bc0` = `Vp.ModifiedPageListHead`** (`+0xfd`) · `0x10` → `+0x4c40` = `Vp.ModifiedNoWritePageListHead` (`+0x939`) · 그 밖 → `+0x1b80` = `PageLists.BadPageListHead` (`+0xa73`). 고른 머리의 `ListName` (`_MMPFNLIST +0xc`, `+0x118 MOV EDI,[R15+0xc]`) 로 가른다 (`_MMLISTS`: 2 = StandbyPageList, **3 = ModifiedPageList**, 4 = ModifiedNoWritePageList — PDB 열거값): `+0x15e CMP EDI,2` 대기 길 / `+0x167 CMP EDI,3` → **`+0x170 ...` 변경 길** / `+0x652 CMP EDI,4` → 변경-쓰지-않음 길: 그 쪽 섹션의 `ControlArea.u` 가 "고쳐 쓰기 금지" 가 아니면 (`+0x65b ~ +0x679` 로 PFN 원래 PTE → `_SUBSECTION` → `ControlArea` 를 RSI 에 담고, `+0x6b6 MOV EAX,[RSI+0x38]` · `+0x6b9 TEST AL,8` — PDB `_CONTROL_AREA.u` `+0x38` 비트 3 = `NoModifiedWriting`) `+0x6d4 MOV EDI,3` 로 **변경 목록으로 바꿔** `+0x185` 로 합류 (켜졌으면 `+0x6bb JNZ +0x291` 로 변경-쓰지-않음 목록 그대로).
  - 변경 길의 순서: 전이 쪽 카운터 +1 (`+0x17d INC.LOCK [R13+0x1c40]` = `PageLists.TransitionSharedPages` — PFN `+0x28` 맨 윗 비트 `u4.PrototypePte` 가 켜졌을 때 / `+0x645 INC.LOCK [R13+0x3fc0]` = `TransitionPrivatePages` — 꺼졌을 때 (`+0x177 JNS +0x645`). 칸 이름은 `_MI_PARTITION +0xb40` 기준 PDB 오프셋 계산. 변경-쓰지-않음 길은 `+0x698` 에서 같은 둘 중 하나를 올리고 `+0x6d9 JMP +0x185` 로 합류) → **`+0x185 INC.LOCK [R13+0x4bc0]` = `Vp.ModifiedPageListHead.Total`** → `+0x1b9` (ret `+0x1be`) **`MiDetermineModifiedPageListHead`** (실제 머리 고르기: `+0x63 LEA [RBX+0xf80]` = `PageLists.ModifiedPageListNoReservation` · `+0x7e LEA [RBX+0x1000]` = `…NoReservationCompress` · `+0x91 ~ +0xa0` `(0x30 + 인덱스) × 0x58 + 파티션` = `PageLists.ModifiedPageListByReservation[인덱스]` (`0x1080 + i×0x58`)) → `+0x1d0` (ret `+0x1d5`) `MiLockPageListAndLastPage` → `+0x1e8 INC.LOCK [R13+0x4ca0]` (= `Vp.TotalPagesForPagingFile`, PDB 오프셋 계산) → **`+0x1fc INC.LOCK [RCX+0x550]`**.
  - `+0x1a4 BT RAX,0xa; JC +0x5fd` (PFN 의 다른 모양) 갈래도 `+0x634 JMP +0x1ec` 로 같은 `+0x1fc` 에 모인다.
  - `MiInsertPagesInList(체인, 플래그)`: 체인 첫 PFN 의 `u3.e1.Modified` 비트 (`+0xee MOVZX R15D,byte [RBX+0x22]` → `+0xff AND R15B,0x10` — PDB `_MMPFN.u3` `+2` 의 `Modified` = 비트 4) 가 켜졌으면 목록 = `+0xe7 LEA RAX,[R10+0x4bc0]` (변경 목록), 꺼졌으면 `+0x105 LEA RAX,[R10+0xc40]` (대기 목록). 켜졌을 때만 `+0x40b JNZ +0x62d` → `+0x632 ADD.LOCK [R14+0x4bc0],R12` (머리 `Total` += 개수) → **`+0x64a` 카운터 += 개수**.
  - **그래서 목록 이름: `ModifiedPageList` (변경 목록, `_MMLISTS` = 3).** 이 빌드 PDB 에 `MmModifiedPageListHead` 는 **없다** (`pdb_aliases.py --where` 결과 "공개 심볼에 없음"). 머리는 `MiSystemPartition` (`_MI_PARTITION`, 전역 `0x140e38d40`) 안 칸: 한 곳 모음 `Vp.ModifiedPageListHead` (`+0x4840 + 0x380`) + 실제 줄 세우는 곳 `PageLists.ModifiedPageListNoReservation` · `…NoReservationCompress` · `…ByReservation[]` (`MiDetermineModifiedPageListHead` 가 고름).
  - 카운터는 **페이지 주인 프로세스가 아니라 "지금 코드가 도는 스레드가 붙어 있는 프로세스"** (`[GS:[0x188] + 0xb8]`) 의 것이다.
- **0 으로 돌리는 쪽 (새로 나옴)**: `MiUserFault` (사용자 영역 페이지 폴트 입구, caller `MmAccessFault`) 가 매 폴트마다 `+0x58 MOV EAX,[RSI+0x550]` → `+0x5e CMP EAX,0x10` → `+0x61 JBE +0x152` (**16 이하면 아무것도 안 함**). 16 을 넘고 `Flags3.SystemProcess` (`+0x5fc` 비트 12, `+0x67 TEST …,0x1000`) 가 꺼져 있고 파티션 변경 목록 합 (`+0xf6 MOV RAX,[R11+0x4bc0]`) 이 기준 이상이고 `+0x10d` (ret `+0x112`) `MiCanFlushMakeProgress` 가 참이면 **`+0x116` 에서 0 으로 되돌리고** 폴트 낸 스레드를 `+0x68f` (ret `+0x694`) `MiDelayFaultingThread` 로 잠깐 늦춘다 (`.c` 에 인자 `0x101f4` 또는 `0x1001e`). 곧 `ModifiedPageCount` 는 **"이 프로세스가 변경 목록으로 보낸 쪽 수 — 많이 보내면 다음 폴트에서 늦추는 속도 조절 카운터"** 다 `[정적]` (뜻은 코드 모양 근거, 이름 풀이는 `[추측]`). test.exe 초기화는 1 → 2 라 `+0x61 JBE` 에서 걸러져 **이 쓰기는 안 탄다.**
- **test.exe 초기화에서 오르는 이유 한 줄**: 사진은 `P0 = 0 → P1 = 1` (라인 484) 과 `P1a = 1 → P1b = 2` (라인 482). 지금 도는 test.exe 스레드가 부른 메모리 관리 코드가 **고친(dirty) 쪽을 변경 목록으로 보냈다** `[추측]` — 후보 호출자는 `MiInsertPageInList` 를 플래그 `0x8` (변경) 로 부르는 줄: `MiPfnShareCountIsZero +0x267` · `MiPfnReferenceCountIsZero +0x9b` · `MiWsleFree +0x9e8 / +0xa3f` · `MiActOnPte +0xb1b` · `MiRemoveLockedPageChargeAndDecRef +0x2e4` · `MiUnlockProtoPoolPage +0x46e` · `MiDecommitFreePage +0x36a` · `MiOutSwapWorkingSetPte +0x3d7` · `MiCompletePrivateZeroFault +0xb42` · `MiReservePageFileSpaceForPage +0x3e2` · `MiBuildReservationCluster +0x5c5` · `MiFreeModifiedReservations +0xf1` · `MiMoveModifiedPagesToCompressList +0x87` · `MiZeroCfgSystemWideBitmapWorker +0x584` (+ 플래그 `0x10`: `MiGatherMappedPages +0x48d`), 한꺼번에 넣는 쪽 (`MiInsertPagesInList`) 은 `MiUnlockMdlWritePages` · `MmUnmapViewInSystemCache` · `MiFinishHardFault` · `MiEnqueuePageList` · `MiAddPageToInsertList` · `MiInsertProtectedStandbyPage` (플래그 값은 `call` 앞 `MOV EDX,imm` 를 nt.dis 로 본 것). `NtCreateUserProcess` 뿌리 지도 (`_map`) 안에서 두 함수로 닿는 길은 예: `…MiCloneVads → MiBuildForkPte → MiHandleForkTransitionPte +0x3f4b4e → MiInsertPageInList` · `…CcPurgeCacheSection → CcUnmapVacb → MmUnmapViewInSystemCache +0x324 → MiInsertPagesInList` (둘 다 깊이 7) — 하지만 이건 **부모 스레드가 `NtCreateUserProcess` 를 도는 길 (`K1` 앞)** 이고 `K1 = 0` 이라 `P0 ~ P1` 증가의 원인이 아니다 `[추측]`. 첫 스레드 시작 길 (`PspUserThreadStartup` 이후) 은 이 지도 뿌리 밖이라 지도로 좁혀지지 않는다. 어느 것인지는 정적으로 **못 정한다** → 동적 D1.
- 가름·고칠 줄: baggage 3.11 `+0x550` 줄 — "어느 목록 (변경 목록?) 인지는 `[추측]`" → **"변경 목록 (`ModifiedPageList`) 이다 `[정적]`"**, 근거 칸에 `MiUserFault +0x116` (0 으로 돌림) · `MiInsertPagesInList +0x64a` 의 켜진 비트 (`+0x40b`) 조건 · 목록 머리 이름 (`_MI_PARTITION` 안) 을 보탬. 단계 ① · 칸 수 · 정확 (줄) 은 그대로, **의미 `[추측]` → `[정적]` (뜻 이름만 `[추측]`)**.
- 지금 우리 (PE_emulator): **확인 필요.** `ext/syscall` 소스에 `ModifiedPageCount` 이름이 없고 에뮬레이터엔 페이지 목록이 없다. 사진 `K1 ~ P0 = 0` 은 처음 값 그대로 맞고, `P1` 이후 1 ~ 2 는 진짜 VM 메모리 관리자의 값이라 맞출 이유가 약하다 `[추측]` — EPROCESS 날 바이트를 맞춘다면 "0 이 아닌 값이 와도 된다" 칸.

## baggage 3.11 에 고칠 줄 (flow-writer 몫 — 이 결과서가 정하는 숫자)

| 줄 | 지금 | 고칠 것 |
|---|---|---|
| 3.11 `+0x5d0 InvertedFunctionTableLock` (지금 "범위 밖 길에서만 씀 52" 줄 안, 목록 칸) | 범위 밖 `[추측]` | **새 줄**, 단계 ②, 놓는 법 `직접 (nt 락)` (부르는 쪽 syscall `0x35`), 쓴 주인 nt, 값 `0 → 1 (BTS) → 0 (XADD −1)`, 경로 `NtSetInformationProcess (0x35) +0x566 → RtlInsertDynamicUserFunctionTable +0xa7 · +0xb4 · +0xbc · (+0xcd) · +0x125 · +0x141`, 근거 `[정적]` sast115 · 정확 · 사진 `끝까지 0` (8장 0, 표 `+0x5c8` 는 `P1a` 부터 ≠ 0) |
| 3.11 `+0x5e0 ThreadListLock` (같은 "범위 밖 52" 줄 안) | 범위 밖 `[추측]` | **새 줄**, 단계 ①, 놓는 법 `직접`, 쓴 주인 nt, 값 `0 → 1 → 0`, 경로 `①… → PspInsertThread +0x6ac · +0x6c3 → (+0x239 ~ +0x243) → +0x24e → +0x261`, 근거 `[정적]` sast115 · run79 · 정확 · 사진 `끝까지 0` |
| 3.11 "범위 밖 길에서만 씀 52" 줄 | 52 칸 이름에 위 둘이 있음 | 두 이름을 뺌 → **50** |
| 3.11 `+0x550 ModifiedPageCount` | "어느 목록 (변경 목록?) 인지는 `[추측]`" · 경로 `.asm 없음, 지도 밖` | 목록 = `ModifiedPageList` `[정적]`, 경로에 `MiUserFault +0x116` (0 으로 돌림, `+0x58 CMP 0x10`) · `MiInsertPagesInList +0x40b` 조건 추가, `.asm` 있음 (이번에 뽑음) |
| 3.11 `+0x318 DeviceMap` | ntdll 첫 호출자 `[공백]` (F5) | `LdrpInitializeProcess +0x1517 → RtlpInitCurrentDir +0x32 → RtlpCreateNewDirectoryReference +0xbb NtOpenFile` `[정적]`, 조건부 앞 갈래 `+0x117e LdrpCheckAppDirType` (마스크 0 이라 안 탐), 실제 탔는지는 동적 F5 |
| 5장 ② 4 번 `(run51) EPROCESS.DeviceMap …` 줄 | "이 목록 어디쯤인지 `[공백]`" | `(run65) 현재 디렉터리 +0x1517` 바로 그 자리 |

- **3.11 머리 숫자**: `칸 263` 그대로 · **`아는 칸 183 → 185`** (정확 `179 → 181`, 짐작 4 그대로) · `아무도 안 씀 26` 그대로 · **`범위 밖 길에서만 씀 53 → 51`** · `모름 1` 그대로 · `줄 109 → 111` (새 줄 2). 합 `185 + 26 + 51 + 1 = 263` ✓.
- **2장 `_EPROCESS` 줄**: `nt 가 씀 183 → 185` (0 그대로 167 · 아직 모름 1 은 그대로 — 두 락 모두 사진 8장 0 이라 `0 그대로` 는 이미 포함).
- **2장 아래 `끝까지 0 160` 가름**: `0 을 씀 50 · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 52` → `0 을 씀 50 · 썼다 되돌림 2 (새 — 잡았다 놓음) · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 50`. 7.1 "아는 칸 중 끝까지 0 구조체" 숫자는 +2, `_EPROCESS` 의 "nt 183 + 안 씀 26 + 범위 밖 53 + 모름 1 = 263" 은 "nt 185 + 안 씀 26 + 범위 밖 51 + 모름 1 = 263", "범위 밖 길 (커널 198 = `_EPROCESS` 53 …)" 은 `_EPROCESS` 51 · 합 196 (전체 257 → 255) 로.
- 같은 푸시락 부품 (잡기 `KeAbPreAcquire` + `BTS.LOCK` / 놓기 `XADD.LOCK −1` + `KeAbPostRelease`) 은 6장 공용 부품에 **한 줄** (이름 `ExAcquirePushLockExclusive`·`ExReleasePushLockExclusive` 인라인 `[추측: 헤더 인라인]`, 느린 길 `ExfAcquirePushLockExclusiveEx`·`ExfTryToWakePushLock`, 값 0 ↔ 1).
- 7.9 F5 (`DeviceMap` 호출자) 는 "정적 답 있음 → 동적은 확인 한 줄".

## 동적으로 확인할 것

- **D1 (질문 3)** `ModifiedPageCount` 가 0 → 1 → 2 로 오를 때 누가 부르나: `nt!MiInsertPageInList+0x1fc`·`nt!MiInsertPagesInList+0x64a` 에 test.exe 일 때만 서는 멈춤점 (`GS:[0x188]+0xb8` 이 test.exe EPROCESS 일 때) + `k` → 첫 호출 (P0 ~ P1) 과 둘째 호출 (P1a ~ P1b) 의 부르는 함수·플래그 `EDX`. 대안: `ba w4 <EPROCESS+0x550>`. 함께 `nt!MiUserFault+0x116` 는 **안 서야 정상** (서면 카운터가 `0x10` 넘은 것).
- **D2 (질문 1)** (선택) 락 칸을 잡힌 동안 읽어 1 인지: `nt!RtlInsertDynamicUserFunctionTable+0x125` 에서 `dq @rdi+0x5d0 L1` = 1, `+0x13a` 에서 0. `nt!PspInsertThread+0x24e` 에서 `dq @r12 L1` = 1, `+0x261` 에서 0. 이미 사진 (값 0) 과 코드가 맞아서 급하지 않다.
- **D3 (질문 2, 기존 F5)** `nt!ObpSetCurrentProcessDeviceMap+0xf8` 에서 `k`: `ntdll!NtOpenFile` ← `RtlpCreateNewDirectoryReference +0xc0` 가 첫 번째인지. 또 `LdrpIllegalCWDDevices` 가 test.exe 에서 0 인지 (`+0x1169` 읽는 값) — 아니면 `LdrpCheckAppDirType` 이 앞선다.
- **추천 멈춤점 정리**: `nt!ObpSetCurrentProcessDeviceMap+0xf8` (DeviceMap 쓰는 순간) · `nt!MiInsertPageInList+0x1fc` / `nt!MiInsertPagesInList+0x64a` (카운터 +) · `nt!RtlInsertDynamicUserFunctionTable+0xbc` / `+0x125` · `nt!PspInsertThread+0x6c3` / `+0x24e`. 모두 `test.exe` 프로세스 조건을 붙인다.

## 다음 요청 후보

- **정적 Q99-2 (D1 뒤)**: D1 이 호출자를 정하면 그 함수 하나를 `^이름$` 로 뽑아 "어느 페이지를 왜 변경 목록으로 보냈나" 를 닫는다 (지금은 후보 15 + 6).
- **정적**: `RtlRemoveDynamicUserFunctionTable` 도 같은 `+0x5d0` 락을 쓰는지 (은퇴 뒤 길이라 급하지 않음 — `.asm` 없음).
- **정적 (선택)**: `PsGetNextProcessThread`·`PspThreadDelete +0x374` 등 `ThreadListLock` 을 **공유로** 잡는 쪽 (은퇴 뒤, 같은 락 칸에 대기자가 생길 수 있는 곳) — 초기화 안에선 필요 없음.
- **동적**: D1 ~ D3 를 dast 요청으로 (D3 는 기존 7.9 F5 와 합침).

## 판정

**분석성공.** 세 질문 모두 `.asm` 줄로 답이 났다: ① 두 락 모두 잡았다 놓고 0 으로 돌아옴 (가름 옮김 가능, 사진 값과 맞음), ② `DeviceMap` 의 ntdll 호출자 = `LdrpInitializeProcess +0x1517 → … → RtlpCreateNewDirectoryReference +0xbb NtOpenFile` (단 정책 마스크가 켜지면 `+0x117e` 가 앞서는 조건부 갈래 하나), ③ `ModifiedPageCount` = 변경 목록 (`ModifiedPageList`) 카운터 + 새로 찾은 `MiUserFault +0x116` 0 으로 돌림. 못 정한 것은 "test.exe 에서 올리는 호출자가 후보 중 누구인가" 하나 (동적 D1).

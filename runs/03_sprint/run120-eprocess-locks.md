# 2026-11-16 (작성일) run120 — `_EPROCESS` 작은 정적 묶음 (Q99, 정적만): 두 락 (`InvertedFunctionTableLock +0x5d0` · `ThreadListLock +0x5e0`) 은 **초기화 길에서 잡았다 놓고 0 으로 돌아옴** → 3.11 아는 칸으로 (183 → **185**), `DeviceMap` 을 처음 채우는 ntdll 호출자 = **`LdrpInitializeProcess +0x1517 → RtlpInitCurrentDir +0x32 → RtlpCreateNewDirectoryReference +0xbb NtOpenFile`** (`\??\C:\…`, 구간 `P1~P1a`), `ModifiedPageCount` = **변경 목록 (`ModifiedPageList`)** 카운터 + 새로 찾은 **`MiUserFault +0x116` 0 으로 되돌림** (속도 조절, test.exe 는 안 탐)

(글의 오프셋은 call 자리 · 읽는/쓰는 줄 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 줄은 모두 이미 그림에 있는 상자 안쪽이다 (`nt!PspInsertThread` · ntdll `LdrpInitializeProcess` 뒤1 `RtlpInitCurrentDir` · `NtSetInformationProcess(0x35)` 길). 대표 지점 규칙으로 노드를 늘리지 않았다 — **그림은 안 바꿨다.** 글 (flow.md 2.1 · 2.4 · 4장) 과 짐 지도 (3.11 · 2장 · 5장 ② · 6장 · 7장) 에만 넣었다.

## 요약

- **두 락** `[정적]` — 둘 다 `ExAcquirePushLockExclusiveEx` · `ExReleasePushLockEx` 를 **부르지 않고** 같은 일을 함수 안에 펼쳐 놓았다: `KeAbPreAcquire` 부름 → `BTS.LOCK [락],0` (0 → 1) … `XADD.LOCK [락],−1` (1 → 0) → `KeAbPostRelease` 부름. 느린 길 (`ExfAcquirePushLockExclusiveEx` · `ExfTryToWakePushLock`) 은 남이 같은 락을 쥐고 있을 때만.
  - `InvertedFunctionTableLock` (`+0x5d0`) — 잡는 곳은 `NtSetInformationProcess` 가 아니라 그 callee **`RtlInsertDynamicUserFunctionTable`** (`+0xa7 LEA` · `+0xbc BTS` … 모든 길이 모이는 `+0x125 XADD` · `+0x141 KeAbPostRelease`). 표를 꽂는 `+0x20c` 는 락 안. test.exe 는 이 길을 탄다 (run51 · run63) → **② 에서 잡고 풂, 0 으로 돌아옴.**
  - `ThreadListLock` (`+0x5e0`) — `PspInsertThread +0x6ac LEA` · `+0x6c3 BTS` → 목록 꼬리 달기 `+0x239 ~ +0x243` → `+0x24e XADD` · `+0x261 KeAbPostRelease`. 스레드마다 같은 길 → **① 에서 잡고 풂, 0 으로 돌아옴.**
  - 사진 (run44): 두 칸 모두 여덟 장 0 `[관찰]` — 잡았다 놓은 뒤 값과 맞다.
- **`DeviceMap` 의 ntdll 호출자** `[정적]`: `LdrpInitializeProcess +0x1517` (ret `+0x151c`) `RtlpInitCurrentDir` → `+0x32` (ret `+0x37`) `RtlpCreateNewDirectoryReference` → **`+0xbb` (ret `+0xc0`) `NtOpenFile`** (syscall `0x33`). 이름은 `RtlpDosPathNameToRelativeNtPathName` 이 앞에 `\??\` (`RtlpDosDevicesPrefix`) 를 붙인 현재 디렉터리. 그 앞에 `\??\` 이름을 넘기는 syscall 은 test.exe 길에 없다 (조건부 갈래 셋만 — 아래 2절). 실제로 탔는지·정말 첫 번째인지는 동적 (F5).
- **`ModifiedPageCount` (`+0x550`)** `[정적]`: 쓰는 줄 **셋** (nt 전체 `+0x550]` 목적지 39개 중 EPROCESS 것) — 올림 `MiInsertPageInList +0x1fc` (+1) · `MiInsertPagesInList +0x64a` (+개수), **0 으로 되돌림 `MiUserFault +0x116`** (새). 목록은 파티션의 **변경 목록 (`_MMLISTS` 3 = `ModifiedPageList`)**. 이 빌드 PDB 엔 `MmModifiedPageListHead` 가 **없다** — 머리는 `MiSystemPartition` (`_MI_PARTITION`) 안 칸이다. 누가 test.exe 에서 올렸나는 정적으로 못 정함 (동적 D1).
- 짐 지도: **2장 `_EPROCESS` "nt 가 씀" 183 → 185**, 3.11 아는 칸 183 → 185 (정확 179 → 181) · 범위 밖 53 → 51 · 줄 109 → 111. 끝까지 0 160 가름에 **잡고 푸는 잠금 칸 2** (결과서 말로 "썼다 되돌림 2"). 6장 공용 부품에 푸시락 한 줄.
- **PE_emulator** (결과서가 본 줄, flow-writer 는 코드 안 읽음): 두 락 — **맞음** (락 값을 안 둠 = 끝 값 0). `DeviceMap` · `ModifiedPageCount` — 에뮬레이터에 칸이 없음, 비교하는 곳이 있으면 **확인 필요**.

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: **ntoskrnl pdb guid_age `C29EBFB06B78B3C020DCA66D99713F9E1`** (`ms/sym/ntoskrnl.exe.pdb.info.txt` — 기준과 같음, 결과서). 다시 analyze 안 함.
- ntdll 은 **Ghidra 프로젝트를 안 열었다** (그때 sast/113 이 ntdll 을 돌리는 중). 이미 있는 `ms/disassemble/ntdll/*.asm` · `_calls.tsv` 와 PE 파일 바이트만 읽음 (결과서).
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다.
- **sast/115 결과만 넣었다.** sast/117 ~ 120 은 안 넣었다 (다음 바퀴 몫).

## 목적

- run51 (sast/45) 이 넘긴 Q99 — 3.11 `_EPROCESS` 의 작은 정적 셋:
  1. "범위 밖 길에서만 씀 `[추측]`" 으로 둔 두 락이 초기화 길에서 잡혔다 놓이나, 놓으면 0 으로 돌아오나.
  2. `DeviceMap` 을 처음 채우는 **ntdll 쪽** 호출자 (커널 쪽 `ObpLookupObjectName +0x143d` 는 run51).
  3. `ModifiedPageCount` 가 오르는 페이지 목록 이름 (run51 "변경 목록? `[추측]`").

## 방법

- 정적: `_workspace/sast/115_eprocess-locks.md` — ntoskrnl.
  - 새로 뽑음: `^(MiInsertPageInList|MiInsertPagesInList|MiDetermineModifiedPageListHead)$` depth 0 (3개 — 둘은 짐 지도가 "`.asm` 없음" 으로 적어 둔 것) · `^MiUserFault$` depth 0 (`+0x550` 을 0 으로 되돌리는 쓰기가 전수에서 나와서).
  - 읽기만 (있던 것): `RtlInsertDynamicUserFunctionTable` · `NtSetInformationProcess` · `PspInsertThread` · `ObpLookupObjectName` · `ObpReferenceCurrentDeviceMap` (ntoskrnl) / `LdrpInitializeProcess` · `RtlpInitCurrentDir` · `RtlpCreateNewDirectoryReference` · `RtlpDosPathNameToRelativeNtPathName` (ntdll).
  - `+0x550` 쓰기 전수: 스크래치 `nt.dis` 에서 목적지가 `+0x550]` 인 명령 39개 → 베이스 레지스터 출처로 가름 (결과서).
  - `RtlpDosDevicesPrefix` 칸 (rva `0x173200`) 은 ntdll 파일 바이트로 읽음 (결과서).
  - 목록 칸 이름 (`_MI_PARTITION` · `_MMPFNLIST` · `_MMLISTS` · `_MMPFN` · `_CONTROL_AREA`) 은 PDB, `MmModifiedPageListHead` 없음은 `pdb_aliases.py --where` (결과서).
- 결과서: `_workspace/sast/115_eprocess-locks_result.md` (**분석성공** — 스크립트 문제 없음).
- flow-writer 가 다시 맞춘 것: 아래 표의 call / ret 를 `.asm` 의 `CALL ret=` 줄로 — `NtSetInformationProcess +0x394 · +0x55c · +0x566 · +0x584`, `RtlInsertDynamicUserFunctionTable +0x9f · +0xa7 · +0xb4 · +0xbc · +0xcd · +0xe1 · +0x125 · +0x141 · +0x14f · +0x166 · +0x178 · +0x1bf · +0x20c · +0x302`, `PspInsertThread +0x239 · +0x243 · +0x24e · +0x258 · +0x25e · +0x261 · +0x6ac · +0x6bb · +0x6c3 · +0x6cc · +0x6d5 · +0x6e3 · +0x6ea · +0x6f8 · +0x8b5`, `MiInsertPageInList +0xfd · +0x167 · +0x185 · +0x1b9 · +0x1d0 · +0x1ec · +0x1f5 · +0x1fc`, `MiInsertPagesInList +0xe7 · +0xff · +0x105 · +0x40b · +0x632 · +0x64a`, `MiUserFault +0x20 · +0x2d · +0x58 · +0x5e · +0x61 · +0x67 · +0xf6 · +0x10d · +0x116 · +0x68f`, `MiDetermineModifiedPageListHead +0x63 · +0x7e`, `LdrpInitializeProcess +0x1169 · +0x117e · +0x1517`, `RtlpInitCurrentDir +0x1c · +0x32`, `RtlpCreateNewDirectoryReference +0x62 · +0xbb · +0xf7`, `RtlpDosPathNameToRelativeNtPathName +0x201 · +0x2a8`, `ObpLookupObjectName +0x143d · +0x145e`, `ObpReferenceCurrentDeviceMap +0x286`. 모두 결과서와 같다.
- 앞 바퀴 짝: run51 (sast/45 — Q99 를 넘긴 바퀴, `DeviceMap` 커널 쪽 · `ModifiedPageCount` 줄), run79 (sast/75 — `ThreadListHead` · `ThreadListLock` 줄), run65 (sast/59 — 현재 디렉터리 본문 · `LdrpIllegalCWDDevices` 마스크 0), run63 (클래스 `0x35` 길을 test.exe 가 탄 것 — 비트 21 `[관찰]`), run116 (잠금 칸 셈 규칙).

## 결과

비유 (푸시락 = 화장실 문 **"사용 중" 팻말**):

- 들어갈 때 팻말을 "사용 중" (1) 으로 돌리고, 나올 때 다시 "비어 있음" (0) 으로 돌린다.
- 줄 선 사람이 없으면 팻말만 돌리고 끝 (빠른 길). 누가 줄 서 있을 때만 "다음 분" 을 부른다 (느린 길).
- 그래서 나중에 와서 보면 팻말은 늘 "비어 있음" 이다. 사진 여덟 장이 모두 0 인 까닭이다.

### 정적 — 호출 지점 (`ms/disassemble/<m>/*.asm` 의 `CALL ret=` 줄)

| caller | call | ret | callee | 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtSetInformationProcess` | `+0x566` | `+0x56b` | `nt!RtlInsertDynamicUserFunctionTable` | 클래스 `0x35` 갈래 (`+0x394 CMP EBX,0x35` → 핸들 −1 `+0x52d` → 길이 16 `+0x537` → `+0x55c` 버퍼 `+8` 바이트 0) | [정적] |
| `nt!NtSetInformationProcess` | `+0x584` | `+0x589` | `nt!RtlRemoveDynamicUserFunctionTable` | 같은 갈래, 버퍼 `+8` ≠ 0 일 때 (빼기 — test.exe 넣기 길엔 없음) | [정적] |
| `nt!RtlInsertDynamicUserFunctionTable` | `+0xb4` | `+0xb9` | `nt!KeAbPreAcquire` | 락 `[프로세스+0x5d0]` 잡기 앞 (`+0xa7 LEA RDI,[R15+0x5d0]`) → `+0xbc BTS.LOCK [RDI],0` | [정적] |
| `nt!RtlInsertDynamicUserFunctionTable` | `+0xcd` | `+0xd2` | `nt!ExfAcquirePushLockExclusiveEx` | 느린 길 — 비트가 이미 켜져 있을 때만 | [정적] |
| `nt!RtlInsertDynamicUserFunctionTable` | `+0x1bf` | `+0x1c4` | `nt!ExAllocatePool2` | 락 안 — 표가 모자랄 때 `0x10 + 24×N`B (태그 `0x46447452`). 뒤 `+0x20c MOV [RAX+0x5c8],RDI` 표 꽂기 | [정적] |
| `nt!RtlInsertDynamicUserFunctionTable` | `+0x302` | `+0x307` | `nt!ExfTryToWakePushLock` | 놓기 `+0x125 XADD.LOCK [RDI+0x5d0],RBX` (−1) 뒤 대기자가 있을 때만 (`+0x12e ~ +0x134`) | [정적] |
| `nt!RtlInsertDynamicUserFunctionTable` | `+0x141` | `+0x146` | `nt!KeAbPostRelease` | 놓기 마무리 | [정적] |
| `nt!RtlInsertDynamicUserFunctionTable` | `+0x166` | `+0x16b` | `nt!KiCheckForKernelApcDelivery` | 가드 영역 나오기 (`+0x14f` `_KTHREAD +0x1e6` +1) 뒤 남은 APC 가 있을 때 | [정적] |
| `nt!RtlInsertDynamicUserFunctionTable` | `+0x178` | `+0x17d` | `nt!ExFreePoolWithTag` | 옛 블록 — **락을 푼 뒤** | [정적] |
| `nt!PspInsertThread` | `+0x6bb` | `+0x6c0` | `nt!KeAbPreAcquire` | 락 `[프로세스+0x5e0]` 잡기 앞 (`+0x6ac LEA R12,[R15+0x5e0]`) → `+0x6c3 BTS.LOCK [R12],0` | [정적] |
| `nt!PspInsertThread` | `+0x6d5` | `+0x6da` | `nt!ExfAcquirePushLockExclusiveEx` | 느린 길 — 남이 쥐고 있을 때만 (`+0x6cc ~`) | [정적] |
| `nt!PspInsertThread` | `+0x8b5` | `+0x8ba` | `nt!ExfTryToWakePushLock` | 놓기 `+0x24e XADD.LOCK [R12],RAX` (−1) 뒤 `+0x258 JZ +0x8b2` (대기자 있을 때만) | [정적] |
| `nt!PspInsertThread` | `+0x261` | `+0x266` | `nt!KeAbPostRelease` | 놓기 마무리 (`+0x25e MOV RCX,R12`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x117e` | `+0x1183` | `ntdll!LdrpCheckAppDirType` | **조건부 안 탐** — `+0x1169` `LdrpIllegalCWDDevices` 가 0 또는 `0xffffffff` 면 `+0x1174 JA` 가 `+0x1183` 으로 건너뜀. test.exe 는 0 (run65) | [정적] |
| `ntdll!LdrpInitializeProcess` | **`+0x1517`** | **`+0x151c`** | `ntdll!RtlpInitCurrentDir` | 뒤1 현재 디렉터리 (`+0x150f LEA RCX,[RSP+0xb0]`) | [정적] |
| `ntdll!RtlpInitCurrentDir` | `+0x1c` | `+0x21` | `ntdll!RtlDetermineDosPathNameType_Ustr` | 종류 1·2·6·7 이면 아래 줄, 3·4·5 면 `0xc000000d` (`+0x6a ~ +0x8f`) | [정적] |
| `ntdll!RtlpInitCurrentDir` | **`+0x32`** | **`+0x37`** | `ntdll!RtlpCreateNewDirectoryReference` | | [정적] |
| `ntdll!RtlpCreateNewDirectoryReference` | `+0x62` | `+0x67` | `ntdll!RtlpDosPathNameToRelativeNtPathName` | NT 이름 만들기 — `+0x201 LEA R9,[RtlpDosDevicesPrefix]` 를 새 버퍼 앞에 (`+0x2a8` 복사) | [정적] |
| `ntdll!RtlpDosPathNameToRelativeNtPathName` | `+0x2a8` | `+0x2ad` | 메모리 복사 함수 | 접두 `\??\` 복사. 같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove` | [정적] |
| `ntdll!RtlpCreateNewDirectoryReference` | **`+0xbb`** | **`+0xc0`** | **`ntdll!NtOpenFile`** | syscall `0x33`, 접근 `0x100020` (`+0x94`), 옵션 `0x21` · 공유 `3`, OA `RootDirectory` 0 · 속성 `0x40` · 이름 = 위 `\??\C:\…` | [정적] |
| `ntdll!RtlpCreateNewDirectoryReference` | `+0xf7` | `+0xfc` | `ntdll!NtQueryVolumeInformationFile` | (run65 그대로) | [정적] |
| `nt!ObpLookupObjectName` | `+0x145e` | `+0x1463` | `nt!ObpReferenceCurrentDeviceMap` | 이름 앞 `\??` 맞으면 (`+0x143d SUB ECX,0x3f005c` · `+0x1445 ~ +0x144d`) — run51 | [정적] |
| `nt!ObpReferenceCurrentDeviceMap` | `+0x286` | `+0x28b` | `nt!ObpSetCurrentProcessDeviceMap` | 칸이 비었으면 — `+0xf8` 이 `EPROCESS.DeviceMap` 을 씀 (run51) | [정적] |
| `nt!MiInsertPageInList` | `+0x1b9` | `+0x1be` | `nt!MiDetermineModifiedPageListHead` | 변경 길 — 실제 줄 세울 머리 고르기 (`+0x63` · `+0x7e` · `+0x91 ~ +0xa0`) | [정적] |
| `nt!MiInsertPageInList` | `+0x1d0` | `+0x1d5` | `nt!MiLockPageListAndLastPage` | 그 뒤 `+0x1fc INC.LOCK [RCX+0x550]` | [정적] |
| `nt!MiUserFault` | `+0x10d` | `+0x112` | `nt!MiCanFlushMakeProgress` | 참이면 `+0x116` 0 으로 되돌림 | [정적] |
| `nt!MiUserFault` | `+0x68f` | `+0x694` | `nt!MiDelayFaultingThread` | 폴트 낸 스레드를 잠깐 늦춤 (`.c` 인자 `0x101f4` / `0x1001e`) | [정적] |

### 호출 관계

```
[① 부모 cmd 스레드 — 스레드마다]
PspInsertThread (R15 = 프로세스, R14 = 스레드)
 ├─ +0x657 ~ +0x65a  ActiveThreads +1 → 첫/둘째 판정 갈래 셋 → 모두 +0x6ac 로
 ├─ +0x6ac  LEA R12,[R15+0x5e0]  ThreadListLock
 ├─ +0x6bb  KeAbPreAcquire · +0x6c3 BTS.LOCK (0 → 1)   (+0x6d5 ExfAcquirePushLockExclusiveEx — 남이 쥘 때만)
 ├─ +0x6e3 · +0x6ea  ThreadListEntry · ThreadListHead (+0x370) → +0x6f8 JZ → +0x239 ~ +0x243 꼬리 달기   (run79)
 └─ +0x24e  XADD.LOCK −1 (1 → 0) → (+0x8b5 ExfTryToWakePushLock — 대기자 있을 때만) → +0x261 KeAbPostRelease

[② test.exe 첫 스레드 — ntdll·kernel32·kernelbase 마다, run63 "세 번 [추측: 개수]"]
…RtlAddGrowableFunctionTable +0x14f NtSetInformationProcess(0x35)
 └─ nt NtSetInformationProcess +0x566  RtlInsertDynamicUserFunctionTable
          ├─ +0x9f   가드 영역 들어가기 (_KTHREAD +0x1e6 −1)
          ├─ +0xa7   LEA RDI,[R15+0x5d0]  InvertedFunctionTableLock
          ├─ +0xb4   KeAbPreAcquire · +0xbc BTS.LOCK (0 → 1)   (+0xcd ExfAcquirePushLockExclusiveEx — 남이 쥘 때만)
          ├─ +0xe1   표 읽기 [+0x5c8] → (+0x1bf ExAllocatePool2) → +0x20c 표 꽂기 [+0x5c8] → 항목 끼우기
          ├─ +0x125  XADD.LOCK −1 (1 → 0) → (+0x302 ExfTryToWakePushLock) → +0x141 KeAbPostRelease
          ├─ +0x14f  가드 영역 나오기 (+0x166 KiCheckForKernelApcDelivery — 남은 APC 있을 때)
          └─ +0x178  ExFreePoolWithTag (옛 블록, 락 밖)

[② test.exe 첫 스레드 — 뒤1, P1 ~ P1a]
LdrpInitializeProcess
 ├─ (+0x117e LdrpCheckAppDirType → NtOpenFile — LdrpIllegalCWDDevices 0 이라 안 탐)
 └─ +0x1517  RtlpInitCurrentDir
               ├─ +0x1c   RtlDetermineDosPathNameType_Ustr        (종류 1·2·6·7)
               └─ +0x32   RtlpCreateNewDirectoryReference
                            ├─ +0x62  RtlpDosPathNameToRelativeNtPathName   "\??\" + 현재 디렉터리
                            ├─ +0xbb  NtOpenFile (syscall 0x33)        ← 이번 (DeviceMap 첫 호출자 후보)
                            │     └─ nt … → ObpLookupObjectName +0x145e ObpReferenceCurrentDeviceMap
                            │                 └─ +0x286 ObpSetCurrentProcessDeviceMap +0xf8   EPROCESS.DeviceMap   (run51)
                            └─ +0xf7  NtQueryVolumeInformationFile

[페이지 목록 — 지금 도는 스레드의 프로세스 카운터]
MiInsertPageInList (플래그 0x8 = 변경)
 └─ +0x185 Vp.ModifiedPageListHead.Total +1 → +0x1b9 MiDetermineModifiedPageListHead → +0x1d0 MiLockPageListAndLastPage
     → +0x1ec · +0x1f5 [GS:0x188 → +0xb8] → +0x1fc INC.LOCK [프로세스+0x550]
MiInsertPagesInList (첫 PFN Modified 비트 켜짐)
 └─ +0x40b JNZ → +0x632 Total += 개수 → +0x64a ADD.LOCK [프로세스+0x550],개수
MiUserFault (사용자 영역 페이지 폴트마다, 결과서: caller MmAccessFault)
 └─ +0x58 읽기 → +0x5e CMP 0x10 → +0x61 JBE (16 이하면 끝 — test.exe 는 여기서 끝)
     → +0x67 SystemProcess 아님 · +0xf6 파티션 변경 목록 합 · +0x10d MiCanFlushMakeProgress → +0x116 [프로세스+0x550] = 0 → +0x68f MiDelayFaultingThread
```

### 1. 두 락 — 결과서 질문 1 `[정적]`

| 칸 | 단계 | 잡기 | 락 안 | 놓기 | 느린 길 (남이 쥘 때만) | 사진 (run44) |
|---|---|---|---|---|---|---|
| `+0x5d0` `InvertedFunctionTableLock` (`_EX_PUSH_LOCK` 8B) | ② (syscall `0x35`) | `RtlInsertDynamicUserFunctionTable +0xa7 LEA` · `+0xb4 KeAbPreAcquire` · **`+0xbc BTS.LOCK`** | `+0xe1` 표 읽기 · `+0x1bf` 할당 · **`+0x20c` 표 꽂기 (`+0x5c8`)** · 항목 끼우기 `+0x299 ~ +0x2bf` | **`+0x125 XADD.LOCK`** (`+0x9b OR RBX,−1`) · `+0x141 KeAbPostRelease` | `+0xcd ExfAcquirePushLockExclusiveEx` · `+0x302 ExfTryToWakePushLock` | 여덟 장 0 (`K3` 492 · `P1` 492 · `P1a` 490 · `P1b` 490 · `P2` 490 줄의 첫 8B — 결과서) |
| `+0x5e0` `ThreadListLock` | ① (스레드마다) | `PspInsertThread +0x6ac LEA` · `+0x6bb KeAbPreAcquire` · **`+0x6c3 BTS.LOCK`** | `+0x6e3 ~ +0x6f8` 안전 검사 → **`+0x239 ~ +0x243` 꼬리 달기 (`+0x370`)** (run79) | **`+0x24e XADD.LOCK`** (`+0x247 OR RBX,−1`) · `+0x261 KeAbPostRelease` | `+0x6d5 ExfAcquirePushLockExclusiveEx` · `+0x8b5 ExfTryToWakePushLock` | 여덟 장 0 (`K3` 493 · `P0` 493 · `P1` 493 · `P1a` 491 · `P1b` 491 · `P2` 491 — 결과서). 같은 사이 `+0x370` `Blink` 는 첫 스레드 → 둘째 스레드로 바뀜 (`K3` 454 · `P1b` 452) |

- **0 으로 돌아오나**: 대기자가 없으면 돌아온다. `BTS` 는 비트 0 만 켜고, `XADD −1` 이 끈다. `RtlInsertDynamicUserFunctionTable` 은 락을 잡은 뒤의 길이 모두 `+0x115` 로 모여 `+0x125` 를 지난다. 락 앞에서 끝나는 길 (`+0x19 JNZ` 정렬 틀림 · `+0x60` 범위 오류 `0xc0000095`) 은 잡지도 않는다.
- **test.exe 가 이 길을 타나**: `+0x5d0` — 탄다 `[정적 + 관찰]`. 표 칸 `+0x5c8` 은 `P1` 0 → `P1a` `ffffb689'b064d820` (`ms/logs/04_baggage-read_P1a.txt:489`, 결과서) 이고 그 칸을 쓰는 줄은 `+0x20c` 뿐이다 → 락 안을 지났다. 횟수는 run63 "세 번 `[추측: 개수]`" 그대로. `+0x5e0` — 모든 스레드가 같은 길 (갈래 셋이 모두 `+0x6ac` 로).
- `RtlInsertDynamicUserFunctionTable` 은 가드 영역 들어가기·나오기를 펼쳐 놓았고 (`+0x9f` · `+0x14f`, `_KTHREAD +0x1e6 SpecialApcDisable`), `PspInsertThread` 는 그게 없다 — 그 몫은 부른 쪽 `[추측]` (결과서).
- 공용 부품 이름: 펼친 꼴이라 `ExAcquirePushLockExclusive` · `ExReleasePushLockExclusive` 를 헤더 인라인으로 본 것은 `[추측: 헤더 인라인]` (결과서).

### 2. `DeviceMap` 을 처음 채우는 ntdll 호출자 — 결과서 질문 2 `[정적]`

- **이름이 `\??\` 로 시작하는 까닭**: `RtlpDosPathNameToRelativeNtPathName +0x201` 이 `RtlpDosDevicesPrefix` (rva `0x173200`) 를 집어 `+0x272` 길이 · `+0x28f` 글 · `+0x2a8` 복사로 새 버퍼 앞에 붙인다. 파일 바이트로 그 칸은 `{Length 8, MaxLength 10, Buffer → "\??\"}` (글 rva `0x17aa80`), 옆 칸 `0x173210` 은 `\??\UNC\` (결과서).
- **커널 쪽** (run51 그대로): `ObpLookupObjectName +0x143d` 이름 앞 `\?` · 셋째 `?` 대조 → `+0x145e ObpReferenceCurrentDeviceMap` → 칸이 비었으면 `+0x286 ObpSetCurrentProcessDeviceMap` → `+0xf8` 이 `EPROCESS.DeviceMap` 을 씀.
- **그 앞에 `\??\` 이름을 넘기는 syscall 은 없나** (결과서: `LdrpInitializeProcess` 의 `+0x1517` 앞 callee 를 `_calls.tsv` 로 깊이 5 까지):
  - 이름을 쓰는 syscall 은 `NtOpenKey` (`\Registry\…`) · `NtOpenDirectoryObject` `\KnownDlls` (`+0xdb8`) · `NtOpenSymbolicLinkObject` `KnownDllPath` (`+0xe47`, 뿌리 = 위 핸들) · 이름 없는 `NtCreateEvent` 들뿐이다.
  - `\??\` 후보는 **조건부 셋**, test.exe 는 셋 다 안 탄다:
    - `+0x117e LdrpCheckAppDirType` (exe 경로 `NtOpenFile`) — 마스크 `LdrpIllegalCWDDevices` 가 0 · `0xffffffff` 면 건너뜀 (`+0x1169 ~ +0x1174`). test.exe 는 0 (run65). **마스크가 켜진 프로세스는 이 줄이 먼저다.**
    - `+0x531 LdrpInitializeExecutionOptions → … → AvrfMiniLoadDll` — App Verifier 켜진 프로세스만 (run105).
    - `+0x1302 LdrpDrainWorkQueue` 아래 DLL 파일 길 (`LdrpMapDllNtFileName +0x132 NtOpenFile` · `LdrpGetNtPathFromDosPath +0xee NtQueryAttributesFile`) — 이때 일감 큐는 비어 있을 것 `[정적 추론]`. DLL 을 파일로 여는 길 (run21 · run39) 은 `P1a~P1b` 라 `DeviceMap` 시점 (`P1~P1a`) 보다 늦다.
- **사진 구간과 맞나**: `P1a` 는 `LdrpInitializeProcess +0x199d` 라 `+0x1517` 은 그 앞 → `P1~P1a` (run44 `DeviceMap` 처음 값 구간) 와 맞다.
- 남은 것: 실제로 그 `NtOpenFile` 이 첫 `\??\` 이름인지 (동적 F5 = 결과서 D3).

### 3. `ModifiedPageCount` (`+0x550`, `unsigned long`) — 결과서 질문 3 `[정적]`

| 줄 | 명령 | 베이스 | 하는 일 |
|---|---|---|---|
| `MiInsertPageInList +0x1fc` | `INC.LOCK dword [RCX+0x550]` | `+0x1ec GS:[0x188]` → `+0x1f5 [RAX+0xb8]` | 변경 목록에 한 쪽 넣을 때 +1 |
| `MiInsertPagesInList +0x64a` | `ADD.LOCK dword [RCX+0x550],R12D` | `+0x63a GS:[0x188]` → `+0x643 [RAX+0xb8]` (결과서) | 변경 목록에 여러 쪽 한꺼번에 넣을 때 +개수 |
| **`MiUserFault +0x116`** | `MOV dword [RSI+0x550],R13D` (`+0x20 XOR R13D` = 0) | `+0x2d MOV RSI,[R9+0xb8]` | **0 으로 되돌림** (아래) |

- 나머지 36개 `+0x550]` 쓰기는 다른 구조체다 (`_ETHREAD +0x550 TopLevelIrp`, 캐시 파티션, 저장소 관리자, 추적 칸 등 — 결과서). 어느 것도 베이스가 `[스레드+0xb8]` 에서 오지 않는다.
- **어느 목록인가**: `MiInsertPageInList` 의 플래그 `0x8` → 파티션 `+0x4bc0` (`+0xfd LEA R15,[R13+0x4bc0]`) = `Vp.ModifiedPageListHead`, 머리 `ListName` 3 (`_MMLISTS` `ModifiedPageList`, `+0x167 CMP EDI,3`) 이 변경 길. 변경-쓰지-않음 (4) 도 섹션이 "고쳐 쓰기 금지" 가 아니면 3 으로 바꿔 같은 길로 합류 (`+0x6d4`). 그 길: `+0x185` 머리 `Total` +1 → `+0x1b9 MiDetermineModifiedPageListHead` (실제 줄: `PageLists.ModifiedPageListNoReservation` · `…NoReservationCompress` · `…ByReservation[i]`) → `+0x1d0 MiLockPageListAndLastPage` → `+0x1fc` 카운터 +1. `MiInsertPagesInList` 는 첫 PFN 의 `u3.e1.Modified` 비트 (`+0xff AND R15B,0x10`) 가 켜졌을 때만 변경 목록 (`+0xe7`) → `+0x40b JNZ` → `+0x632` → `+0x64a`.
- **이름**: 목록 = **`ModifiedPageList` (변경 목록)**. 이 빌드 PDB 엔 `MmModifiedPageListHead` 가 없다 (`pdb_aliases.py --where` "공개 심볼에 없음", 결과서). 머리는 `MiSystemPartition` (`_MI_PARTITION`, 전역 `0x140e38d40`) 안 칸이다. 파티션 칸 이름은 PDB 오프셋 계산 (결과서).
- **누구의 카운터인가**: 페이지 주인이 아니라 **지금 코드가 도는 스레드가 붙어 있는 프로세스** (`[GS:[0x188]+0xb8]`) 다.
- **0 으로 되돌림 (새)**: `MiUserFault` 가 폴트마다 `+0x58` 읽고 `+0x5e CMP EAX,0x10` → `+0x61 JBE` (16 이하면 아무것도 안 함). 16 을 넘고 · `Flags3.SystemProcess` 꺼짐 (`+0x67`) · 파티션 변경 목록 합 (`+0xf6`) 이 기준 이상 · `+0x10d MiCanFlushMakeProgress` 참이면 `+0x116` 0 으로 되돌리고 `+0x68f MiDelayFaultingThread` 로 스레드를 늦춘다. 뜻은 **"변경 목록으로 많이 보낸 프로세스의 폴트를 늦추는 속도 조절 카운터"** — 코드 모양은 `[정적]`, 이름 풀이는 `[추측]`.
- **test.exe**: 사진 `P0 0 → P1 1` · `P1a 1 → P1b 2` (결과서 — 라인 484 · 482). 2 라 `+0x61 JBE` 에서 걸러져 **되돌림 줄은 안 탄다** `[정적 + 관찰]`. 누가 올렸나는 후보만 (플래그 `0x8` 로 부르는 줄 15 · `MiInsertPagesInList` 부르는 쪽 6 — 결과서 목록) — 정적으로 못 정함 → 동적 D1. 지도 `_map/NtCreateUserProcess_*` 안에서 닿는 길 (`MiHandleForkTransitionPte` · `MmUnmapViewInSystemCache`, 깊이 7) 은 부모 스레드 길 (`K1` 앞) 이고 `K1 = 0` 이라 원인이 아니다 `[추측]`.

### 짐 지도 셈 (결과서 "baggage 3.11 에 고칠 줄", 오케스트레이터 셈 기준)

- **셈 기준** (오케스트레이터): 두 락은 잡았다 놓고 0 으로 돌아옴 → 3.11 "범위 밖 길에서만 씀" 에서 빼고 아는 줄로. run116 의 ntdll 잠금 칸 10 과 같은 규칙 — 잡고 푸는 줄이 초기화 길에 있으면 3장.

| | 전 | 뒤 |
|---|---|---|
| 3.11 칸 | 263 | 263 |
| 3.11 아는 칸 (정확 · 짐작) | 183 (179 · 4) | **185 (181 · 4)** |
| 3.11 아무도 안 씀 | 26 | 26 |
| 3.11 범위 밖 길에서만 씀 `[추측]` | 53 (줄 안 52 + `NumberOfPrivatePages` 1) | **51** (줄 안 50 + 1) |
| 3.11 모름 | 1 | 1 |
| 3.11 줄 | 109 | **111** (새 줄 2) |
| 2장 `_EPROCESS` nt 가 씀 | 183 | **185** |
| 2장 `_EPROCESS` 0 그대로 (사진) · 아직 모름 | 167 · 1 | 167 · 1 (두 칸 모두 이미 `끝까지 0` 에 들어 있음) |
| 2장 커널 14개 합 nt 가 씀 | 605 | **607** |
| 2장 아래 구조체 21개 | 아는 칸 938 (정확 907) · 범위 밖 267 | **아는 칸 940 (정확 909) · 범위 밖 265** (합 1,456 그대로) |
| `_EPROCESS` 끝까지 0 160 가름 | 0 을 씀 50 · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 52 | 0 을 씀 50 · **잡고 푸는 잠금 칸 2** · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 **50** (7.2 의 run50 가름 줄 숫자 그대로 고침 — 그 줄의 "아무도 안 씀 28" 은 run50 때 숫자, run51 뒤 3.11 은 26) |
| 7.1 아는 칸 중 끝까지 0 (구조체) | 381 · 셈 맞춰 보기 466 | **383 · 464** |

- 가름 이름은 결과서의 "썼다 되돌림" 대신 run116 (3.8) 이 쓴 **"잡고 푸는 잠금 칸"** 으로 맞췄다 (7.1 의 run49 "썼다 되돌림 1" 과도 뜻은 같다).
- 고친 줄: 3.11 새 줄 2 (`+0x5d0` · `+0x5e0`) + 고친 줄 4 (`+0x318 DeviceMap` 근거 · `+0x370 ThreadListHead` 근거에 락 줄 이름 · `+0x550 ModifiedPageCount` · "범위 밖 52 → 50" 줄) + `+0x5c8 InvertedFunctionTable` 무엇을 칸에 락 줄 한 마디.

## 확인된 것

- `InvertedFunctionTableLock` 은 `RtlInsertDynamicUserFunctionTable` 안에서 `+0xbc BTS.LOCK` 으로 잡고 `+0x125 XADD.LOCK −1` 로 놓는다. 표 꽂기 `+0x20c` 는 그 사이다 `[정적]` (`.asm`).
- `ThreadListLock` 은 `PspInsertThread +0x6c3 BTS.LOCK` 으로 잡고 `+0x24e XADD.LOCK −1` 로 놓는다. 목록 달기 `+0x239 ~ +0x243` 은 그 사이다 `[정적]` (run79 와 같은 줄).
- 두 락 모두 대기자가 없으면 끝 값 0 — 사진 여덟 장 0 과 맞다 `[정적 + 관찰]` (결과서가 run44 로그 줄을 짚음).
- `LdrpInitializeProcess +0x1517 → RtlpInitCurrentDir +0x32 → RtlpCreateNewDirectoryReference +0xbb NtOpenFile` 의 이름은 `\??\` 로 시작한다 `[정적]` (`.asm` + ntdll 파일 바이트).
- `ModifiedPageCount` 를 올리는 두 줄은 변경 목록 (`ModifiedPageList`) 에 넣을 때만 닿는다. 0 으로 되돌리는 줄은 `MiUserFault +0x116` 하나다 `[정적]` (nt 전수).
- 이 빌드 PDB 에 `MmModifiedPageListHead` 는 없다 `[정적]` (결과서 `pdb_aliases.py --where`).

## 정정

- **run50 (sast/44) 가름 정정** — `InvertedFunctionTableLock` (`+0x5d0`) · `ThreadListLock` (`+0x5e0`) 을 "범위 밖 길에서만 씀 `[추측]`" 으로 셌는데, 둘 다 **초기화 길에서 잡고 푼다** (② `RtlInsertDynamicUserFunctionTable` · ① `PspInsertThread`). run50 "범위 밖 길에서 씀" 표가 이미 `PspInsertThread` (푸시락 잡았다 놓음) 를 적어 두었지만 가름은 범위 밖이었다. 짐 지도 3.11 아는 칸으로 옮김. run50 문서 맨 위에 한 줄.
- **run51 (sast/45) 공백 닫음 · 추측 올림** — 3.11 `DeviceMap` 의 "ntdll 첫 호출자 `[공백]`" → `[정적]` 위 사슬 (탔는지는 F5 그대로). `ModifiedPageCount` 의 "어느 목록 (변경 목록?) `[추측]`" → **변경 목록 `[정적]`**, 그리고 되돌리는 줄 `MiUserFault +0x116` 을 run51 이 몰랐다 ("런타임 카운터" 설명에 보탬). run51 문서 맨 위에 한 줄.

## 아직 확인 필요

### 동적 (결과서 "동적으로 확인할 것" D1 ~ D3 — 급하지 않음. 대기열 번호는 오케스트레이터가 붙임, 멈춤점은 모두 test.exe 조건)

- **D1** `ModifiedPageCount` 0 → 1 (`P0~P1`) · 1 → 2 (`P1a~P1b`) 를 누가 부르나: `nt!MiInsertPageInList+0x1fc` · `nt!MiInsertPagesInList+0x64a` (`GS:[0x188]+0xb8` 이 test.exe EPROCESS 일 때) + `k` · 플래그 `EDX`. 대안 `ba w4 <EPROCESS>+0x550`. 함께 `nt!MiUserFault+0x116` 은 **안 서야 정상**. (= 기존 F13)
- **D2** (선택) 락이 잡힌 동안 1 인지: `nt!RtlInsertDynamicUserFunctionTable+0x125` 에서 `dq @rdi+0x5d0 L1` = 1 → `+0x13a` 에서 0 · `nt!PspInsertThread+0x24e` 에서 `dq @r12 L1` = 1 → `+0x261` 에서 0. 코드와 사진이 이미 맞아 급하지 않다.
- **D3** (= 기존 F5) `nt!ObpSetCurrentProcessDeviceMap+0xf8` 에서 `k` — `ntdll!NtOpenFile` ← `RtlpCreateNewDirectoryReference+0xc0` ← `RtlpInitCurrentDir+0x37` ← `LdrpInitializeProcess+0x151c` 가 첫 번째인지. 그리고 `ntdll!LdrpInitializeProcess+0x1169` 가 읽는 `LdrpIllegalCWDDevices` 가 0 인지 (아니면 `+0x117e LdrpCheckAppDirType` 이 앞선다).

### 정적 (결과서 "다음 요청 후보")

- ① (D1 뒤) D1 이 정한 호출자 하나를 `^이름$` 로 뽑아 "어느 페이지를 왜 변경 목록으로 보냈나" 를 닫는다 (지금 후보 15 + 6).
- ② (낮음) `RtlRemoveDynamicUserFunctionTable` 도 같은 `+0x5d0` 락을 쓰는지 — 은퇴 뒤 길, `.asm` 없음.
- ③ (낮음, 선택) `PsGetNextProcessThread` · `PspThreadDelete +0x374` 등 `ThreadListLock` 을 **공유로** 잡는 쪽 — 은퇴 뒤, 초기화 안에선 필요 없음.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 결과서가 본 줄만. 고칠지는 사용자 결정)

- **맞음** — 두 락: `ProcessObject::dynamic_function_tables` 는 락 값을 안 둔다 (`ext/syscall/syscall_layer_cpp/include/syscall_layer/processes.hpp:29~31` 주석이 `+0x5d0` 을 짚음). `ThreadListLock` 이름은 `ext/syscall` 에 없다 (grep). 끝 값 0 과 같다. EPROCESS 날 바이트를 만들어 비교하는 쪽이 있다면 두 칸 0 — 그 쪽이 있는지는 **확인 필요**.
- **ntdll 순서는 맞음** — `DeviceMap` 호출자: 진짜 ntdll 을 돌리면 같은 길이 간다. 단 `ProcessParameters.CurrentDirectory.DosPath` 가 절대 경로여야 `RtlpInitCurrentDir +0x32` 를 탄다 (run65 조건 그대로).
- **확인 필요** — 커널 `DeviceMap` · `ModifiedPageCount` 는 에뮬레이터에 칸이 없다 (`ext/syscall` grep 0). 칸 값을 비교하는 곳이 없으면 무관. `ModifiedPageCount` 의 `P1` 이후 1 ~ 2 는 진짜 VM 메모리 관리자 값이라 맞출 이유가 약하다 `[추측]`.

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntkrnlmp · ntdll pdb 줄에 run120 · run 메모 한 줄 — 새 로그 없음이라 근거 로그 줄은 그대로), 그림 메모 (run120 도 그림 안 고침), 2.1 머리 run 목록 · `PspInsertThread` 3번 아래 한 줄, 2.4 머리 run 목록 · "ntdll 이 부르는 syscall 이 커널 프로세스 객체 칸을 처음 만드는 자리" 나무의 `?` 줄 · 그 아래 한 줄 · run65 절 현재 디렉터리 한 줄, 4장 run51 "작은 정적" 줄 `(해결됨, 정적 → run120)` · run51 F5 · F13 줄 보탬 + 새 줄 셋 (동적 D1 ~ D3 · 정적 ① ~ ③ · PE_emulator), 5장 근거 run 표 · 03_sprint 요청서 번호 표.
- 짐 지도 (`baggage.md`): 기준 run120 줄 · 판 줄 · 1장 `_EPROCESS` 줄 · 2장 표 둘 (`_EPROCESS` · 커널 합) · 2장 아래 구조체 줄 · 범위 밖 줄 · run120 한 줄, 3장 머리 run120 줄, **3.11 머리 · 새 줄 2 · 고친 줄 4 + `+0x5c8` 한 마디 · "범위 밖 52 → 50" 줄**, 5장 ② 4번 `DeviceMap` 문장, **6장 푸시락 한 줄**, 7.1 셈 · 7.2 `_EPROCESS` 가름 줄 · 7.4 `ModifiedPageCount` 줄 · 7.14 F5 · F13 보탬 · 새 7.51, 8장 run120 줄.
- 이전 run 문서 맨 위 정정 한 줄: **run50** (두 락 가름) · **run51** (`DeviceMap` ntdll 호출자 · `ModifiedPageCount` 목록).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (정적만).
- 메모 (flow-writer): 커널 푸시락은 이번처럼 **함수 안에 펼쳐진 꼴** (`KeAbPreAcquire` + `BTS.LOCK` / `XADD.LOCK −1` + `KeAbPostRelease`) 이 많아서, `_calls.tsv` 에서 `ExAcquirePushLock*` 이름으로만 찾으면 놓친다. 다음 락 칸 질문은 `KeAbPreAcquire` · `KeAbPostRelease` 짝과 `LEA [reg+칸]` 으로 찾는 편이 낫다 — 짐 지도 6장 푸시락 줄에 적었다.

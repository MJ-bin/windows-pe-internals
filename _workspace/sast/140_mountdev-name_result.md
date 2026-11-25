# 140 mountdev-name — 정적 분석 결과

- 요청서: `_workspace/sast/140_mountdev-name.md` (원문 `../PE_emulator/_workspace/05_tuning_planner/335_internals-mountdev-name.md` 먼저 읽음)
- 빌드: Windows 11 24H2 26100.9457 x64 (`ms/bin/.../System32`)
- 모듈
  - kernelbase.dll — analyze 됨 (이번엔 새로 안 돌림). 볼륨 관련 15개가 오늘 17:30:58 에 이미 export 돼 있고 (`ms/decomp|disassemble/kernelbase/`), 나머지 몇 개도 이전 요청에서 있어서 그대로 읽음: `GetVolumeNameForRoot` · `GetVolumeNameForVolumeMountPointW` · `GetVolumePathNameW` · `GetVolumePathNameInternalW` · `GetDriveTypeW` · `DeviceIoControl` · `CreateFileInternal` · `BaseSetLastNTError` · `BasepGetVolumeNameFromReparsePoint` (+ 후보 이웃 `…DosLetterNameFromNTName`, `…GUIDFromNTName`, `GetFinalPathNameByHandleW`, `GetVolumePathNamesForVolumeNameW` — 이번 답엔 안 씀).
  - mountmgr.sys — **이 요청서가 시작할 때 이미 analyze·export 가 끝나 있었음** (`ms/ghidra/mountmgr.analyze.log` 17:30:34 · `mountmgr.export.log` 17:34:13, 12함수 depth 0). 로그에 `SetPdb:` 줄 있음 (65줄), PDB 적용 "Terminated Normally" (70·76줄), `_index.tsv` 194함수. 그래서 **다시 analyze 안 함**. 내가 Ghidra 를 돌린 건 0번. 시작 때 메모리: 전체 46 GB, 사용 21 GB, 쓸 수 있는 것 23 GB, 도는 Ghidra 프로세스 없음. mountmgr analyze 는 총 8초였음.
  - shell32.dll · windows.storage.dll — **Ghidra analyze 안 함** (8.0 MB · 9.2 MB, 무겁다고 보고 요청서 지시대로). 대신 PDB 만 받고 (`ms/sym/shell32.dll.pdb`, `ms/sym/windows.storage.dll.pdb`), objdump + `.pdata` + PDB 공개 이름으로 가볍게 훑음 (질문 3 만, 아래 "방법과 한계").
- 새로 뽑은 함수: 0개 (있던 것 재사용). 기존 파일의 `_calls.tsv` 를 다시 쓰지 않음.
- 버전 대조 (스킬 표엔 kernelbase·mountmgr 줄이 없어서 직접 함)
  - `0x4d0008` 을 즉시값으로 싣는 줄을 kernelbase 전체 objdump 에서 찾음 (`grep 0x4d00` → 같은 값은 한 줄, 다른 `0x4d00135f` 는 별개 상수). `mov edx,0x4d0008` 이 **딱 한 곳**: VA `0x180024d92` = RVA `0x24d92` = `GetVolumeNameForRoot`(RVA `0x24c30`) `+0x162`. objdump (원본 바이트) 와 `.asm` 줄이 같음. → 일치.
  - mountmgr: objdump 로 `MountMgrQueryPoints +0x3b` `cmp [r9+0x10],0x18` · `+0xeb` `cmp [r9+0x8],0x20` · `QueryPointsFromMemory +0x141` `mov ecx,0x4d0008` 이 `.asm` 줄과 같음. → 일치.
  - 커널 PDB GUID(`C29EBFB…`) 검사는 이번엔 해당 없음 (ntoskrnl 안 씀). 빌드는 아래 "PDB GUID 확인" 의 바이너리 GUID 로 확인.

## 먼저 읽는 한 줄 답

1. **`0x4d0008` 을 싣는 함수 = kernelbase `GetVolumeNameForRoot`** (RVA `0x24c30`, export 아님). 값을 싣는 줄 `+0x162`, 부르는 줄 `+0x16c`/ret `+0x171`. 같은 함수에서 `NtOpenFile(\??\C:, 0x100080, 공유 3, 옵션 0x10)` 이 나옴 (`+0x126`/`+0x12d`). **`NtQueryInformationProcess(0x17)` 은 이 함수가 아니라 `GetDriveTypeW`** 안에 있고 (`+0xe1`/`+0xe8`), export 쪽 호출자 `GetVolumeNameForVolumeMountPointW` 가 먼저 부름 (`+0x7d`/`+0x82`).
2. 출력 버퍼 0x208 B: **앞 2 B = 이름 길이(바이트, u16)**, **이름은 +2 부터**, 길이 단위 **바이트** (널 문자는 안 믿고 kernelbase 가 붙임). **길이 칸을 상한 검사 없이 `memcpy` 길이로 씀** (`+0x1f0` → `+0x204`). 믿는 한계는 0x208 안 (이름 최대 0x206 B) 인데, 그걸 코드가 지키지 않고 드라이버가 `0x80000005` 로 거르는 데 기댐. → 에뮬레이터의 "성공인데 출력 안 씀" 이 정확히 이 줄에서 터짐.
3. 실패 길: 이 IOCTL 이 음수 status 면 (`0x80000005` 포함) **재시도 없이** FALSE (`+0x1a6` → `+0x611`), 버퍼는 안 읽음. LastError = `DeviceIoControl` 이 `BaseSetLastNTError(status)` 로 세운 값.
4. **바로 뒤 `\??\MountPointManager` IOCTL = `0x6d0008`** (mountmgr 안 이름 문자열 `IOCTL_MOUNTMGR_QUERY_POINTS`). 입력 = 0x18 바이트 머리 + 장치 이름 (머리의 `+0x10` 에 이름 오프셋 0x18, `+0x14` 에 이름 길이), 출력 첫 시도 0x20 B → 진짜 mountmgr 는 **`0x80000005`** + 첫 4 B 에 필요한 크기를 주고, kernelbase 는 그 크기로 **한 번(이상) 다시 부름**. 출력에서 `\??\Volume{GUID}` 를 항목 표(0x18 B 씩)의 `+0/+4` 칸(이름 오프셋·길이)으로 찾아 꺼냄. 길이가 **정확히 0x60 B** 인 것만 받음.
5. `NtCreateFile` 이 실패하면 kernelbase 는 **FALSE** (`GetVolumeNameForRoot` `+0x6eb`), LastError 는 `CreateFileInternal` 이 NtCreateFile status 로 세운 값. 그러고 나서 `GetVolumeNameForVolumeMountPointW` 는 **다른 길(`BasepGetVolumeNameFromReparsePoint`)로 한 번 더** 시도하고 (`+0xf0`), 또 안 되면 출력 첫 글자를 0 으로 하고 FALSE.
6. shell 쪽 (질문 3): windows.storage 의 `CInvokeCreateProcessVerb::_FixupWorkingDirectory` 가 `CMountPoint::GetMountPoint(int)` 의 실패 HRESULT 를 **그냥 지나감** (음수면 함수 끝으로 점프, 반환형 void) — 이 자리 기준으로 "정직한 실패" 로 길이 열림. 다만 ShellExecute\* → 이 자리까지의 중간은 **간접 호출이라 정적으로 못 이음** `[공백]`.

## 질문별 답 [정적]

### 질문 1 — `0x4d0008` 을 부르는 함수와 출력 읽기

**1-a. 함수와 호출 자리** `[정적] ms/disassemble/kernelbase/GetVolumeNameForRoot.asm`

- `GetVolumeNameForRoot(경로, 출력 버퍼, 출력 글자 수)` — 경로는 RCX (`+0x56` 에서 바로 씀), 출력 버퍼 RDX → R15 (`+0x32`), 글자 수 R8D → R12D (`+0x2c`).
- 순서 (오프셋은 call 위치/ret 위치):
  1. `+0x56/+0x5d` `RtlDosPathNameToNtPathName_U(경로, &이름, 0, 0)`. 0(실패) 이면 `+0x64` → `+0x618` 에서 `SetLastError(3)`.
  2. `+0x6a ~ +0xdb` 이름 끝이 `\` 면 지우고 길이 2 B 줄임 (`\??\C:\` → `\??\C:`), 길이 ≥ 4 글자이고 끝이 `:` 면 `toupper` 로 드라이브 글자를 대문자로 (`+0xc2/+0xc9`).
  3. `+0x126/+0x12d` `NtOpenFile` — 접근 `+0x110` `0x100080`, 공유 `+0x104` 3, 옵션 `+0xe5` `0x10`, 속성 칸 `+0x115` `0x40`, 개체 이름 = 위 `\??\C:`. 음수면 `+0x136` → `+0x630` (`RtlNtStatusToDosError` → `SetLastError`, FALSE).
  4. `+0x15a` 출력 크기 `0x208`, `+0x162` `MOV EDX,0x4d0008`, 입력 0·0 (`+0x14b`, `+0x153`), 출력 버퍼 `[RBP-0x40]` (`+0x156`, `+0x167`), `+0x16c/+0x171` `DeviceIoControl`.
  5. `+0x178/+0x17f` `NtClose`, `+0x198/+0x19f` 이름 버퍼 `RtlFreeHeap`.
- `DeviceIoControl` (RVA `0x23660`) 은 `코드 >> 16 == 9` 면 `NtFsControlFile`, 아니면 `NtDeviceIoControlFile` (`.asm` `+0x2b` `CMP EAX,9`). `0x4d0008 >> 16 = 0x4d` 라 **`NtDeviceIoControlFile`** (`+0x6c/+0x73`), 인자: 핸들, 이벤트 0, APC 0, APC문맥 0, IOSB(스택), 코드, 입력, 입력 길이, 출력, 출력 길이 (`+0x55 ~ +0x6c`).
- **호출자 (export 까지)** `[정적] _calls.tsv` · `.asm`
  | 호출자 | 호출 자리 / ret |
  |---|---|
  | `GetVolumeNameForVolumeMountPointW` (export, RVA `0x268f0`) | `+0xd9/+0xde`, `+0x14e/+0x153` (경로가 `X:\` = UNICODE 길이 6 일 때, `+0xbc` `CMP [RSP+0x20],6` → `+0x139`), `+0x16d/+0x172` (뒤에 reparse 이름 얻은 뒤 다시) |
  | `GetVolumePathNameInternalW` (RVA `0x26b10`) ← `GetVolumePathNameW` (export, RVA `0xff290`, `+0xb` 에서 꼬리점프 `JMP`) | `+0x288/+0x28d`, `+0x3f2/+0x3f7`, `+0x4a9/+0x4ae`, `+0x54f/+0x554` |
  - `GetVolumeNameForVolumeMountPointW` 를 kernelbase 안에서 부르는 곳은 없음 (`_calls.tsv` 에 callee 로 안 나옴).
  - 이 함수를 shell 이 부를 땐 windows.storage `_InitLocalDrive` 가 부름 → 질문 3.
- 참고: MountPointManager 쪽 코드는 kernelbase 안 다른 곳도 보냄 (`mov edx,…` 즉시값): `0x6d0008` — `BasepGetVolumeGUIDFromNTName +0x131`, `FindFirstVolumeW +0xf9`; `0x6d0030` — `BasepGetVolumeDosLetterNameFromNTName +0x152`; `0x6d0034` — `GetVolumePathNamesForVolumeNameW +0x26b`. 이번 질문 범위는 `GetVolumeNameForRoot +0x372` 한 곳이고 나머지는 안 읽음.

**1-b. 같은 함수 안에서 나오나?**
- `NtOpenFile(\??\C:, 0x100080, 공유 3, 옵션 0x10)`: **같은 함수** (`+0x126`).
- `NtQueryInformationProcess(0x17)`: **다른 함수**.
  - `GetDriveTypeW` (RVA `0x253c0`) `+0xdd` `LEA EDX,[R9-0xd]` (R9D=0x24 → EDX=0x17), `+0xe1/+0xe8` `NtQueryInformationProcess(-1, 0x17, 버퍼 [RBP-0x40], 0x24, NULL)`.
  - 부르는 자리: `GetVolumeNameForVolumeMountPointW +0x7d/+0x82`, `GetVolumePathNameInternalW +0x226/+0x22b`, `DeleteVolumeMountPointW +0x6f/+0x74`.
  - `GetDriveTypeW` 는 `X:\` 꼴이면 이 호출 한 번으로 끝낼 수 있음: 버퍼 첫 4 B(`DriveMap`)에서 글자 번호 비트가 서 있고(`+0xf5 ~ +0xff`) 그 글자 칸의 1 B(`DriveType[n]`, `+0x101` `[RBP+RCX-0x3c]`)가 3 이면 3 을 돌려줌 (`+0x106 ~ +0x10f`). 아니면 느린 길 (`NtOpenFile`·`NtOpenSymbolicLinkObject`·`NtQueryVolumeInformationFile`…).
  - **로그의 "둘" 의 두 번째**: 이 export 경로는 `GetDriveTypeW` 를 한 번만 부름. 둘째는 다른 호출자(예: shell 쪽이 먼저 부르는 `GetDriveTypeW`)일 수 있음 `[추측]` → 동적 확인.

**1-c. 출력 버퍼에서 읽는 칸** `[정적] GetVolumeNameForRoot.asm`
- 이름 길이: `+0x1ac` `MOVZX EAX, word ptr [RBP-0x40]` → **2 B, u16, 오프셋 0**.
- 이름 시작: `+0x1f5` `LEA RDX,[RBP-0x3e]` → **오프셋 2**. 복사 길이 `+0x1f0` `MOVZX R8D, word ptr [RBP-0x40]` → **길이 칸 값 그대로 = 바이트** (`memcpy` `+0x204/+0x209`). 글자 수로 바꾸는 건 널 문자를 놓는 줄뿐 (`+0x20e ~ +0x216` `SHR RCX,1` 뒤 `MOV [RAX+RCX*2],R13W`).
- 길이 칸은 곧바로 UNICODE_STRING 으로 감: `Length = 칸값` (`+0x1b0`), `MaximumLength = 칸값 + 2` (`+0x1b5 ~ +0x1bd`), 힙 할당 크기 = `칸값 + 2` (`+0x1d6`). 16비트 덧셈이라 칸값이 `0xFFFE`·`0xFFFF` 면 할당이 1~0 바이트로 돌아감 → 복사가 힙을 넘음 `[정적]`.
- **검사 없음**: 길이 칸 ≤ 0x206 비교가 `+0x1a6` 와 `+0x204` 사이에 **없음** (`.asm` 에서 `CMP`·`JA` 없음). 믿는 한계 = 0x208 버퍼 안(= 이름 0x206 B)이지만 그건 코드가 아니라 드라이버 약속에 기댄 것 `[정적: 검사 줄 없음]`. 버퍼는 호출 전에 0 으로 안 채움 (`+0x52 ~ +0x156` 에 버퍼 0 채움 줄 없음, `+0x52` 의 `MOVUPS [RBP-0x50]` 은 IOSB 쪽).
- `lpBytesReturned` (`+0x14e` `[RSP+0x30]=RSP+0x50`) 는 이후 읽지 않음 (덮어씀, `+0x58b`).

**1-d. status 별 가지 (첫 IOCTL)** `[정적] GetVolumeNameForRoot.asm +0x16c ~ +0x1a6`, `DeviceIoControl.asm`
- `DeviceIoControl` 은 `status ≥ 0` 이면 TRUE (`+0x87` `JS` 안 탐, `+0xa0` `MOV EAX,1`), `0x103`(PENDING) 이면 `NtWaitForSingleObject` (`+0x7f` → `+0x1de`) 뒤 다시 판정.
- `status < 0` 이면 `+0x192 ~ +0x1b3`: 상위 두 비트가 모두 1(오류)이 아니면(= `0x80000005` 같은 경고) `lpBytesReturned` 에 IOSB.Information 을 넣고, 그다음 `BaseSetLastNTError(status)` (`+0x154/+0x159`) → FALSE. (`BaseSetLastNTError` = `RtlNtStatusToDosError` → `RtlSetLastWin32Error`, `.asm` `+0x6`, `+0x16`.)
- `GetVolumeNameForRoot` 는 FALSE 면 **어떤 status 든 같은 길** (`+0x1a6` `JZ` → `+0x611` `XOR EAX,EAX` → 끝). `0x80000005` 를 따로 보지 **않고**, 더 큰 버퍼로 다시 부르지도 **않음**.
- 성공(`≥ 0`) 이면 `+0x1ac` 부터 위 칸을 읽음.

### 질문 2 — `\??\MountPointManager` 와 IOCTL

**2-a. kernelbase 쪽** `[정적] GetVolumeNameForRoot.asm +0x238 ~ +0x5e9`
- `+0x1d6 ~ +0x286`: 입력 구조를 힙에 만듦 (크기 = 이름길이 + 0x18). 칸 표는 아래 표 (3).
- `+0x2c2` 출력 버퍼 힙 할당, 크기 `LEA R8D,[RDI+0x1d]` = 0x20 (`+0x2b4`).
- `+0x32f/+0x334` `CreateFileInternal(L"\\.\MountPointManager", 접근 0, 공유 3, 처분 3(OPEN_EXISTING), 확장 칸 구조 &[RSP+0x60], 기능 플래그)` — 문자열은 `[0x1802a0ba8]` = `\\.\MountPointManager` (원본 바이트로 읽음). 확장 칸 구조: 크기 0x20, 속성 0x80 (`+0x2e8`, `+0x2f0`).
- `CreateFileInternal` 이 `\\.\` 를 `\??\MountPointManager` 로 바꿔 `NtCreateFile` 을 부름: `+0x36c/+0x373` (본 길), 인자 — 접근 `[RSP+0x68] | 0x100080` (`+0x337 OR ECX,0x100080` → 0x100080), 속성 `ESI & 0x5affa7` (0x80), 공유 `[RBP-0x70]` (3), 처분 `[RSP+0x64]` (1 = FILE_OPEN, `+0x481`), 옵션 `[RSP+0x60]` (`+0x366 ~ +0x36c`). 옵션 최종값 `0x20060` 은 에뮬레이터 로그 값을 믿음 — 이 줄에서는 비트 계산(`+0x25b ~ +0x279`)을 끝까지 따라가지 않음 `[공백]`.
  - 다른 길 하나: STATUS_ACCESS_DENIED(`0xC0000022`) 면 옵션의 0x20000 비트를 지우고 한 번 더 `NtCreateFile` (`+0x37a` → `+0x696` → 조각 `FUN_18019b82e +0x22 BTR EAX,0x11`, `+0x47`).
- `+0x377/+0x37c` `DeviceIoControl(핸들, 0x6d0008, 입력, 이름길이+0x18, 출력, 0x20, &바이트, 0)`.
- 실패(`+0x380` `JZ` → `+0x567`): `+0x567` `GS:[0x68]` (= LastError) 를 읽어 `+0x56f` `CMP ECX,0xea` — **0xea(ERROR_MORE_DATA)** 이면 재시도 갈래, 아니면 `+0x386` 로 가서 핸들 닫고 FALSE (LastError 는 `0x57`, `+0x673` 이하).
  - 재시도: `+0x587` `MOV EBX,[RSI]` (**출력 첫 4 B = 필요한 크기**), 이전 버퍼 해제, `+0x5b5` 새 버퍼 할당(EBX 바이트), `+0x5e9` `JMP 0x180024f90` (= `+0x360`) 로 다시 `DeviceIoControl` — 출력 길이 `[RSP+0x28]` ← EBX (`+0x5e5`).
  - **횟수 상한이 없음** (되돌아가는 `JMP` 가 무조건, 힙 할당이 실패해야 빠짐 `+0x5cc` `JZ` → `+0x74d`). 엔진이 계속 `0x80000005` 만 주면 끝없이 돎 `[정적]`.
- 성공하면 `+0x38b/+0x390` 핸들 닫고(`CloseHandle`), 입력 구조 해제 `+0x3a2`, 항목 표를 훑음 (`+0x3b6 ~ +0x5f9`):
  - 시작 번호 EDX=0 (`+0x3b6`), `+0x3b9` `CMP EDX,[RSI+4]` — **`+4` = 항목 수**. 끝에 닿으면(`JNC`) `+0x673` → LastError `0x57` FALSE.
  - 항목 i 는 출력 버퍼의 `+8 + 0x18*i` (`+0x3c4` `LEA RCX,[RAX+RAX*2]`, `[RSI+RCX*8+8]`). 읽는 칸: `+0x3c8` `[..+0xc]` u16 = 이름 **길이**, `+0x3ce` `[..+8]` u32 = 이름 **오프셋(출력 버퍼 시작 기준)**.
  - 받는 조건 (`+0x3d2 ~ +0x49f`): 길이가 `0x60` (아니면 `+0x5ee` 에서 `0x62` 인지 보고, `0x62` 는 끝 글자가 `\` 일 때 앞 검사만 통과하다가 `+0x49f` `CMP R8W,0x60` 에서 떨어짐), 그리고 글자 위치 검사: `\`(0) `?`(1, 또는 `\`) `?`(2) `\`(3) `V o l u m e`(4~9, **대소문자 구분**) `{`(10, +0x14) `-`(19, +0x26) `-`(24, +0x30) `-`(29, +0x3a) `-`(34, +0x44) `}`(47, +0x5e). **16진 숫자 자리는 안 봄** (GUID 값 아무거나 가능). 최종 `+0x49f ~ +0x4aa`: 길이 0x60 이고 2번째 글자가 `?`.
  - 안 맞으면 `+0x5f9` `INC EDX` → 다음 항목.
  - 맞으면: 호출자 버퍼 글자 수 `R12*2 ≥ 0x64` 가 아니면 `+0x4bf` `JC` → `+0x766` → LastError `0xce` FALSE. 맞으면 이름 0x60 B 복사 (`+0x4c9 ~ +0x503` MOVUPS 6번), **2번째 글자를 `\` 로 바꿈** (`+0x508`), **끝에 `\` + NUL** (`+0x50d` `MOV dword [R15+0x60], 0x5c`), `+0x52a` `MOV EAX,1`.
  - 그래서 호출자가 받는 건 `\\?\Volume{GUID}\` (49 글자 + NUL = 50 글자). windows.storage `_InitLocalDrive` 가 정확히 50 글자(`+0x30` `MOV R8D,0x32`)를 줌 → 딱 맞음.

**2-b. mountmgr.sys 쪽** `[정적] ms/disassemble/mountmgr/`
- 장치: `DriverEntry +0x6a/+0x71` `IoCreateDevice` (장치 이름 = `.data` `0x140009000` → `\Device\MountPointManager`, 장치 확장 0x178, 종류 0x12). `+0x2df/+0x2e4` `GlobalCreateSymbolicLink` (링크 이름 `\DosDevices\MountPointManager` `.data 0x140009078`, 대상 위 장치) → `\??\MountPointManager` 로 열림 `[정적: 문자열 두 개를 원본 바이트로 읽음]`.
- 함수표: `+0x3e5`/`+0x3e9` → `DRIVER_OBJECT+0x70`·`+0x80` (CREATE·CLOSE) = `MountMgrCreateClose`, `+0x3f7` → `+0xe0` (DEVICE_CONTROL) = `MountMgrDeviceControl`.
- **열기**: `MountMgrCreateClose` `+0x10` 에서 `MajorFunction == 0`(CREATE) 이면 `+0x3c ~ +0x4c`: 옵션(`IrpSp+0x10`)의 **0번 비트(FILE_DIRECTORY_FILE)** 가 서 있으면 `0xC0000103`, 아니면 0(성공). kernelbase 옵션 `0x20060` 은 0번 비트 0 → 성공. 나머지 MajorFunction 은 항상 성공 (`+0x15`).
- **디스패치**: `MountMgrDeviceControl` (RVA `0x17940`) `+0x2e` 코드 읽음(R13D), `+0x66` 뮤텍스 대기, `+0x79` `PsIsHostSilo`. 보통(호스트 silo) 길: `+0x23c` `CMP R13D,0x6d0008` → `+0x252/+0x257` `MountMgrQueryPoints`. (호스트 silo 가 아닌 길은 `+0x349` `SUB EAX,0x6d0008` → `+0x377/+0x37c`.) 모르는 코드는 `0xC0000010` (decomp 의 `"IOCTL UNKNOWN"`). 이름 문자열 `IOCTL_MOUNTMGR_QUERY_POINTS` (=0x6d0008), `IOCTL_MOUNTMGR_QUERY_POINTS_ADMIN` (=0x6d4008) 가 **mountmgr 안에 있음**. `IOCTL_MOUNTDEV_*` 문자열은 mountmgr 안에 **없음** → `0x4d0008` 의 이름 `IOCTL_MOUNTDEV_QUERY_DEVICE_NAME` 은 `[추측: 공개 헤더]`.
- **입력을 검사하는 차례** `MountMgrQueryPoints` (RVA `0x19ac0`) — 표 (5) 에 정리.
- **출력 만들기** `QueryPointsFromMemory` (RVA `0x19fe0`):
  1. 입력에 장치 이름이 있으면: `+0x79/+0x80` `IoGetDeviceObjectPointer(이름, 0x80)` 로 장치를 열고(실패하면 그 status 를 그대로 돌려줌, 흔히 이름 없음 `0xC0000034`), **그 장치에 `0x4d0008` 을 보내** 정식 이름을 다시 받음 (`+0x141` `MOV ECX,0x4d0008`, `+0x155` `IoBuildDeviceIoControlRequest`, `+0x180` `IofCallDriver`; 처음 0x102 B 버퍼, `0x80000005` 면 `(칸값&~1)+2` 로 한 번 더, `+0x1c4` 로 복사) → 이 이름을 장부 (장치 목록) 의 `DeviceName` 과 `RtlEqualUnicodeString` 으로 비교 (`+0x250`).
  2. 장부에 없으면 `+0x2ce` `MOV EAX,0xc000000d` (STATUS_INVALID_PARAMETER).
  3. 있으면 크기 계산: `Size = 8 + 0x18 × 항목수 + (고유ID 길이 + 장치이름 길이 + 모든 심볼릭 링크 이름 길이)`. `+0x39c` 출력 `+0` ← Size, `+0x3a0` 출력 `+4` ← 항목수, IRP `Information`(`+0x38`) ← Size. 출력 버퍼(`[IrpSp+8]`) < Size 면 `Information = 8` (`+0x3b2`) 하고 `+0x3f0` **`0x80000005`** 로 끝 (고정 머리 8 B 는 이미 써 둠).
  4. 출력이 충분하면: `+0x41f` `memset(출력+8, 0, 항목수×0x18)`, 항목마다 `+0x4e8 ~ +0x51f` 칸 쓰기 (이름 오프셋 / 길이 / 고유ID 오프셋 / 길이 / 장치이름 오프셋 / 길이), 이름 본문은 항목 표 뒤에 **[고유ID][장치이름][링크 이름들…]** 순서로 (`+0x4a5`, `+0x4b7`, `+0x534` `memcpy`). 고유ID 칸은 그 링크의 표지(`[링크+0x10]` 1 B)가 0 이면 오프셋·길이 0.
- 장부(이름 표) 본문은 이름만: 장치 목록 머리 = 장치 확장 `+0x10` (`QueryPointsFromMemory` 의 `param_1+0x10`), 장치 항목 안 `DeviceName` (`+0x50`), 고유ID 포인터 (`+0x60`), 링크 목록 (`+0x10` 목록 머리), 링크 항목 `Length` (`+0x18`) · `Buffer` (`+0x20`) · 표지 (`+0x10`) `[추측: decomp 에서 읽은 칸 번호, 구조체 이름은 확인 안 함]`.
- mountmgr 는 볼륨 장치에 `0x4d0008` 을 **자기가 보내는 쪽**이다 (`QueryDeviceName +0x14a` 값 · `+0x15d` 요청 만들기 · `+0x188` `IofCallDriver`; `QueryDeviceNameInternal +0xb7/+0xca/+0xf5`; `QueryPointsFromMemory +0x141/+0x155/+0x180`). 이 코드가 알려 주는 "볼륨 장치 쪽 약속": 출력 첫 u16 = 이름 길이(바이트), 이름은 +2, 버퍼가 작으면 `0x80000005` + 칸에 필요한 이름 길이, 호출자는 `(길이&~1)+2` 크기로 한 번 더. 진짜 볼륨 드라이버(volmgr·partmgr) 본문은 요청서대로 안 봄.

**2-c. `NtCreateFile` 이 실패하면 kernelbase 는 무엇을 돌려주나**
- `CreateFileInternal`: `NtCreateFile` status 가 음수면 `BaseSetLastNTError(status)` 를 부르고 핸들 `-1` (INVALID_HANDLE_VALUE) 로 끝 (`+0x395` `JS` → 조각 `FUN_18002635d +0x0` 가 `[RSP+0x70] = -1`, 같은 조각 `+0xb1/+0xb6` `BaseSetLastNTError`; 기능 플래그가 꺼진 길은 조각 `FUN_18019b82e +0xb0/+0xb5` 가 같은 일을 함). 일부 status 는 LastError 를 바꿔 씀: `0xC0000035` → `0x50`, `0xC00000BA` → `3` 또는 `5` (`frag:FUN_18002635d +0xbc ~ +0xe2`).
- `GetVolumeNameForRoot`: `+0x334` 뒤 `+0x337` `CMP RAX,-1`, `+0x33b` `JZ` → `+0x6eb` → 입력·출력 힙 해제 (`+0x707`, `+0x6c7`) → `+0x6d3` `XOR EAX,EAX` → **FALSE**. 자기가 LastError 를 새로 세우지 않음 → 값은 `CreateFileInternal` 이 세운 것 (예: `0xC0000034` 이면 흔히 2).
- 이어서 `GetVolumeNameForVolumeMountPointW` `+0xe2` → `+0xf0/+0xf5` `BasepGetVolumeNameFromReparsePoint(경로, 출력, 글자수, NULL)` 를 부름:
  - `+0xb0/+0xb5` `CreateFileInternal(경로 `X:\`, 접근 0, 공유 3, 처분 3, 확장 {속성 0x80, 플래그 `0x2200000` (공개 값으로 보면 백업 의미 `0x2000000` + reparse 열기 `0x200000` `[추측: 공개 헤더]`)})`, 실패면 `+0xbc` → `+0x26c` → 반환 0 (LastError 는 그 호출이 세운 값).
  - 열리면 `+0x100/+0x105` `DeviceIoControl(핸들, 0x900a8, 0, 0, 버퍼, 0x1c, …)` — 코드 `0x900a8 >> 16 = 9` 라 **`NtFsControlFile`** 이 나감 (`DeviceIoControl +0x168`). 실패(`ERROR_MORE_DATA` 가 아니면) → `+0x116` `JNZ` → `+0x4c0` → 반환 0.
  - 이 갈래가 성공하면(reparse 이름을 얻으면) `GetVolumeNameForRoot` 를 그 이름으로 한 번 더 부름 (`+0x16d`) — 평범한 C: 루트는 reparse 아님 `[추측]`.
- 그래도 안 되면 `GetVolumeNameForVolumeMountPointW` `+0xfb ~ +0x104`: 출력 글자 수 ≥ 1 이면 `[출력] = 0` 으로 쓰고 FALSE (EBX=0).
- 에뮬레이터의 `path_prefix_unsupported` 는 **status 가 아니라 "멈춤"** (`nt_create_file.cpp:209` `stop(...)`) — kernelbase 가 받는 값이 아니다. 정직한 실패는 아래 "엔진 쪽 제안".

### 질문 3 — shell 쪽 (가볍게 훑음, 부분)

**방법과 한계**: Ghidra analyze 안 함. `objdump -d`(원본 바이트) + `.pdata` 함수 시작 + PDB 공개 이름(`llvm-pdbutil-14 dump -publics`)으로 (a) 세 kernelbase API 를 부르는 자리 찾기 (`import` 칸 번호 → `call [rip+…]`), (b) **직접 `call` 만** 따라 거슬러/내려가 봄. 간접 호출(가상 함수·COM·함수 포인터) 은 못 따라감. 공개 PDB 라 이름 없는 함수가 있음. 임시 파일은 `scratchpad/s140/` (레포 밖).

- **부르는 자리 수**: windows.storage 에서 `GetVolumeNameForVolumeMountPointW` 13곳 · `GetVolumePathNameW` 8곳 · `GetVolumePathNamesForVolumeNameW` 4곳, shell32 에서 10 · 5 · 2곳 (`call [IAT]` 꼴만 셈, 꼬리점프 `jmp [IAT]` 는 안 셈). 함수 이름 목록은 이 결과서 끝 부록.
- **ShellExecute 가 타는 걸로 이어지는 직접 호출 사슬 하나** (windows.storage RVA, `call` 위치 오프셋):
  `CInvokeCreateProcessVerb::Execute` (`0xe1de0`) `+0x58` → `Launch` (`0xe102c`) `+0x10d` → `_TryCreateProcess` (`0x17d118`) `+0x3c` → `_PrepareAndCallCreateProcess` (`0x17d38c`) `+0xdf` → `_FixupWorkingDirectory` (`0x17e25c`) `+0x84` → `CMountPoint::GetMountPoint(int, CMountPoint**)` (`0xf0b5c`) `+0x6c` → `_GetMountPointForDriveLetter` (`0xf0bfc`) `+0x250` → `_InitLocalDrive` (`0x21a58c`) `+0x40` → **`GetVolumeNameForVolumeMountPointW`**.
  - `Execute` 는 가상 함수(`UEAAJXZ`)라 `ShellExecute*` → `Execute` 사이는 직접 호출로 안 이어짐. `ShellExecuteExW`·`ShellExecuteW`·`ShellExecuteNormal` 에서 직접 호출로 12 단계까지 내려가 1001개 함수에 닿았지만 위 사슬엔 안 닿음 `[공백]`. **"ShellExecuteW 가 이 사슬을 탄다" 는 정적으로 못 정함**; 이름과 모양(실행 파일 띄우기 전에 작업 폴더를 고치는 단계)이 맞아서 후보로만 둠 `[추측]`.
- **FALSE 를 받으면** (이 사슬 기준, windows.storage):
  1. `_InitLocalDrive +0x40` 호출 → `+0x4c` `test eax,eax` → `+0x4e` `je` → `+0xff` `ResultFromKnownLastError()` (= 마지막 오류를 HRESULT 로) → 그 값을 돌려줌 (성공 갈래는 레지스트리 도우미로 볼륨 객체를 만듦, `+0xaa`). 호출 인자: 경로 `X:\` 를 만든 칸 `[rsp+0x28]`, 출력 `[rsp+0x30]`, 글자 수 `0x32` (`+0x30`).
  2. `GetMountPoint(int)` 는 그 HRESULT 를 `+0x71` `mov ebx,eax` 로 받아 그대로 돌려줌.
  3. `_FixupWorkingDirectory` `+0x89` `test eax,eax` / `+0x8b` `js 0x18017e32c` (= `+0xd0`, 함수 끝) → **음수면 아무것도 안 하고 끝** (반환 void). → **그냥 지나감**. (이 함수는 `+0x71` `PathGetDriveNumberW(작업 폴더)` 로 드라이브 번호를 얻어 부름.)
  - 같은 모양(`call … ; test eax,eax ; jne ok ; call 0x18008b650` = `GetLastError` 를 HRESULT 로 바꾸는 작은 함수)이 shell32 의 `_GetVolumeNameForVolumeMountPoint` (`+0x66` 다음) 와 `CDriveLetterCache::GetVolumePathNameAndCachedMountPoint` (`+0x1a6` 다음) 에도 있음 — 실패를 HRESULT 로 올림. 그 위에서 멈추는지는 안 봄 `[공백]`.
- 정리: "FALSE → 이 자리에서는 지나감" 은 `[정적]`. "ShellExecuteW 가 이 자리를 지나는가" 와 "다른 자리(예: 실행 전 `PathResolveReparsePoints`, `CFSFolder` 쪽) 에서도 지나가나" 는 `[공백]`.

## 표

### (1) syscall 차례 표 — `GetVolumeNameForVolumeMountPointW(L"C:\\", 출력, 50)` 이 성공할 때

로그 꼴(요청서 원문) 과 나란히. "결과 갈래" 는 kernelbase 가 status 로 가르는 것.

| 차례 | syscall | 인자 (코드 줄) | 결과 갈래 | 줄 | 로그와 |
|---|---|---|---|---|---|
| 1 | `NtQueryInformationProcess` | 핸들 -1, 클래스 `0x17`, 버퍼 `[RBP-0x40]`(0x24 B), 길이반환 NULL | `status<0` → `+0xef` `JS` → `+0x518` (느린 길); 비트가 서고 `DriveType==3` 이면 3 돌려주고 끝; 아니면 `NtOpenFile` 쪽 느린 길 | `GetDriveTypeW +0xe1/+0xe8`, `+0xef`, `+0xf5 ~ +0x10f` (부름: `GetVolumeNameForVolumeMountPointW +0x7d/+0x82`) | 로그 "둘" 중 첫째 일치. 둘째는 이 경로엔 없음 `[추측]` |
| 2 | `NtOpenFile` | `\??\C:`(끝 `\` 떼고 대문자), 접근 `0x100080`, 공유 3, 옵션 `0x10`, 속성 `0x40` | 음수 → `SetLastError(RtlNtStatusToDosError)` FALSE | `GetVolumeNameForRoot +0x126/+0x12d`, `+0x136` | 일치 |
| 3 | `NtDeviceIoControlFile` | 핸들, 0, 0, 0, IOSB, `0x4d0008`, 입력 0·0, 출력 `[RBP-0x40]`, 0x208 | 음수 → FALSE (재시도 없음, 출력 안 읽음); `≥0` → 출력 읽음 | `GetVolumeNameForRoot +0x16c/+0x171` → `DeviceIoControl +0x6c/+0x73`, `+0x1a6` | 일치 |
| 4 | `NtClose` | 위 핸들 | — | `+0x178/+0x17f` | 일치 |
| 5 | `NtCreateFile` | `\??\MountPointManager`, 접근 `0x100080`, 속성 `0x80`, 공유 3, 처분 1, 옵션 `0x20060`(로그값) | 음수 → FALSE → fallback (표 (4)) | `GetVolumeNameForRoot +0x32f/+0x334` → `CreateFileInternal +0x36c/+0x373` | 일치 (옵션값은 로그를 믿음) |
| 6 | `NtDeviceIoControlFile` | MPM 핸들, `0x6d0008`, 입력 = 표 (3) (이름길이+0x18 B), 출력 0x20 B | `0x80000005` + 출력 첫 4 B = Size → 7번; 그 밖 음수 → LastError `0x57` FALSE; `≥0` 면 훑기 | `+0x377/+0x37c`, `+0x380`, `+0x567 ~ +0x5e9` | 로그엔 5번에서 멈춰 아직 안 나옴 |
| 7 | `NtDeviceIoControlFile` | 같은 입력, 출력 Size B | 성공 → 항목 훑기; 또 `0x80000005` 면 8번(상한 없음) | 같은 줄 (`+0x360` 로 되돌아옴) | — |
| 8 | `NtClose` (via `CloseHandle`) | MPM 핸들 | — | `+0x38b/+0x390` | — |

실패로 이어지는 길(표 (4) 의 fallback): 9. `NtCreateFile(\??\C:\ …)` (`BasepGetVolumeNameFromReparsePoint +0xb0/+0xb5` → `CreateFileInternal`) → 10. `NtFsControlFile(…, 0x900a8, …, 출력 0x1c)` (`+0x100/+0x105` → `DeviceIoControl +0x168`) → 11. `NtClose`.

### (2) 출력 버퍼 칸 표

| IOCTL | 오프셋 | 크기 | 뜻 | 읽는 줄 |
|---|---|---|---|---|
| `0x4d0008` | `+0x0` | 2 B (u16) | 이름 길이, **바이트** | `GetVolumeNameForRoot +0x1ac`, `+0x1b0`, `+0x1f0` |
| `0x4d0008` | `+0x2` | 이름 길이 B (UTF-16LE, 널 없음) | 장치 이름 (예 `\Device\HarddiskVolume3`) | `+0x1f5` `LEA RDX,[RBP-0x3e]`, `+0x204` |
| `0x4d0008` | `+0x2 + 길이` ~ `+0x208` | — | 안 읽음 | — |
| `0x6d0008` | `+0x0` | 4 B | `Size` — 필요한 총 바이트. **`0x80000005` 뒤 재시도 때만 읽음** | `+0x587` |
| `0x6d0008` | `+0x4` | 4 B | 항목 수 | `+0x3b9` |
| `0x6d0008` | `+0x8 + 0x18*i + 0x0` | 4 B | 심볼릭 링크 이름 오프셋 (출력 버퍼 시작 기준) | `+0x3ce` |
| `0x6d0008` | `+0x8 + 0x18*i + 0x4` | 2 B | 심볼릭 링크 이름 길이 (바이트) | `+0x3c8` |
| `0x6d0008` | `+0x8 + 0x18*i + 0x8 ~ +0x17` | — | 고유ID·장치이름의 오프셋·길이 — **kernelbase 는 안 읽음** | — |
| `0x6d0008` | 오프셋이 가리키는 곳 | 0x60 B | `\??\Volume{GUID}` (UTF-16LE, 널 없음) | `+0x3dd ~ +0x493` 검사, `+0x4c9 ~ +0x503` 복사 |

(항목 한 칸 안 오프셋은 공개 헤더의 `MOUNTMGR_MOUNT_POINT` 와 모양이 같음 `[추측: 공개 헤더]` — 칸 이름은 헤더 지식, **바이트 근거는 위 줄**과 mountmgr `QueryPointsFromMemory +0x4e8 ~ +0x51f` 의 쓰기 줄.)

### (3) 입력 구조 칸 표 — `0x6d0008` (kernelbase 가 만드는 것)

크기 = 이름길이 + 0x18 (`GetVolumeNameForRoot +0x366 ~ +0x369` `ADD R9D,0x18`). 힙 할당 `+0x230 ~ +0x238`.

| 오프셋 | 크기 | 값 | 줄 |
|---|---|---|---|
| `+0x00` | 4 B | 0 (링크 이름 오프셋) | `+0x261` `MOVUPS [R14],XMM0` (16 B 0) |
| `+0x04` | 2 B | 0 (링크 이름 길이) | 같음 |
| `+0x08` | 4 B | 0 (고유ID 오프셋) | 같음 |
| `+0x0c` | 2 B | 0 (고유ID 길이) | 같음 |
| `+0x10` | 4 B | **`0x18`** (장치 이름 오프셋) | `+0x265` qword 0 쓴 뒤 `+0x269` `MOV dword [R14+0x10],0x18` |
| `+0x14` | 2 B | **이름 길이 (바이트)** | `+0x276` `MOV word [R14+0x14],AX` (AX = 0x4d0008 이 준 길이) |
| `+0x16` | 2 B | 0 (채움) | `+0x265` |
| `+0x18` | 이름 길이 B | 장치 이름 UTF-16LE (널 없음) | `+0x25d` `LEA RCX,[R14+0x18]`, `+0x286/+0x28b` `memcpy` |

(칸 이름 `SymbolicLinkNameOffset/Length`, `UniqueIdOffset/Length`, `DeviceNameOffset/Length` 는 공개 헤더 `[추측: 공개 헤더]`. mountmgr 가 읽는 칸 번호는 `MountMgrQueryPoints +0x4a`(`+4`), `+0x5f`(`+0xc`), `+0x6c`(`+0x14`), `+0x92`(`+0x8`), `+0xac`(`+0x10`) — 위와 같음.)

### (4) status → 반환값 표 (kernelbase)

`GetVolumeNameForRoot` 와 그 export 호출자 `GetVolumeNameForVolumeMountPointW`.

| 갈래 | 조건 | 반환 | LastError | 줄 |
|---|---|---|---|---|
| 경로 변환 실패 | `RtlDosPathNameToNtPathName_U` = 0 | FALSE | 3 | `GetVolumeNameForRoot +0x64`, `+0x618 ~ +0x624` |
| 열기 실패 | `NtOpenFile` 음수 | FALSE | `RtlNtStatusToDosError(status)` | `+0x136`, `+0x630 ~ +0x660` |
| 이름 IOCTL 실패 | `0x4d0008` 이 음수 (`0x80000005` 포함) | FALSE | `DeviceIoControl` 이 세운 값 = `RtlNtStatusToDosError(status)` (`0x80000005` → 0xea 로 보임 `[추측: ntdll 표는 안 읽음]`) | `+0x1a6` → `+0x611`, `DeviceIoControl +0x154` |
| 힙 실패 (이름 복사본) | `RtlAllocateHeap` = 0 | FALSE | 8 | `+0x1ea` → `+0x600` |
| MPM 열기 실패 | `CreateFileInternal` = -1 | FALSE | `CreateFileInternal` 이 세운 값 | `+0x33b` → `+0x6eb` |
| MPM IOCTL 실패, LastError == 0xea | 첫 호출 `0x80000005` | 크기 다시 받아 재호출 | — | `+0x567 ~ +0x5e9` |
| MPM IOCTL 실패, 그 밖 | 음수 | FALSE | `0x57` | `+0x575`, `+0x673 ~ +0x696` |
| 항목 없음 / 0x60 형 이름 없음 | 훑기가 끝까지 감 | FALSE | `0x57` | `+0x3bc` `JNC` → `+0x673` |
| 호출자 버퍼 작음 | 글자 수 < 50 | FALSE | `0xce` | `+0x4bf` → `+0x766 ~ +0x77f` |
| 성공 | — | TRUE (`EAX=1`), 출력 = `\\?\Volume{GUID}\` + NUL | 바뀌지 않음 | `+0x4c9 ~ +0x52a` |
| export 의 fallback | `GetVolumeNameForRoot` = FALSE | `BasepGetVolumeNameFromReparsePoint` 시도 → 실패면 출력[0] = 0, **FALSE** | 그 함수가 마지막으로 세운 값 | `GetVolumeNameForVolumeMountPointW +0xe2 ~ +0x104` |
| export 입력 오류 | 인자 NULL / 길이 0 | FALSE | `0x57` | `+0x24 ~ +0x3f`, `+0x124 ~ +0x137` |
| export 입력 오류 | 끝이 `\` 아님 | FALSE | `BaseSetLastNTError(0xC0000033)` (이름 잘못) | `+0x6e`, `+0x20f`, `+0x202` |

### (5) mountmgr `0x6d0008` 입력 검사 차례 (`MountMgrQueryPoints`, RVA `0x19ac0`)

| 차례 | 검사 | 실패 status | 줄 |
|---|---|---|---|
| 1 | `InputBufferLength` < 0x18 (길이 짧음) | `0xC000000D` | `+0x3b ~ +0x40`, `+0x1fe ~ +0x215` |
| 2 | 링크·고유ID·장치이름의 길이가 0 이면 그 오프셋을 0 으로 고침 | — | `+0x4f ~ +0x77` |
| 3 | 오프셋·길이 **홀수** | `0xC000000D` | `+0x7b ~ +0xc0` → `0x140019fab` |
| 4 | 세 칸의 (오프셋+길이) 중 최댓값 > `InputBufferLength` | `0xC000000D` | `+0xc6 ~ +0xe5` → `+0xe5 JA` |
| 5 | `OutputBufferLength` < 0x20 | `0xC000000D` | `+0xeb/+0xf0` |
| 6 | 링크 이름 길이 > 0xf000 / 장치 이름 길이 > 0xf000 | `0xC000000D` | `+0xfb`→`+0x224/+0x228`, `+0x122/+0x125` |
| 7 | 이름 복사용 풀 할당 실패 | `0xC000009A` | `+0x153 ~ +0x15b` |
| 8 | 어느 칸이 채워졌나로 갈래: 고유ID ≠ 0 → 고유ID 갈래; 장치이름 ≠ 0 → 장치이름 갈래 (kernelbase 가 타는 것); 링크이름만 → 링크 갈래; 다 0 → 전부 | — | `+0x108 ~ +0x1fa` |
| 9 | (장치이름 갈래) `IoGetDeviceObjectPointer` 실패 / 열린 개체의 이름 칸이 비어 있지 않음 | 그 status / `0xC0000034` | `QueryPointsFromMemory +0x79/+0x80` |
| 10 | 장부에 그 장치 없음 | `0xC000000D` | `QueryPointsFromMemory +0x2ce` |
| 11 | 출력 < Size | `0x80000005` (`Information = 8`) | `QueryPointsFromMemory +0x3a9 ~ +0x3f0` |
| 12 | 성공 | `0`, `Information = Size` | `+0x39c ~ +0x3a5` |

"이름 없음" 은 두 가지: 장치 이름 ≠ 0 인데 `IoGetDeviceObjectPointer` 가 못 열면 그 status (흔히 `0xC0000034`), 열렸는데 장부에 없으면 `0xC000000D`.

## 지금 우리 (PE_emulator) 맞나·틀리나

읽은 곳: `ext/syscall/syscall_layer_cpp/src/file/nt_device_io_control_file.cpp` (읽기만), `nt_create_file.cpp`. 요청서가 적은 `:617 ~ :654` 는 지금 파일에서 `NUL`·`CNG`·`KsecDD` 갈래라 **줄 번호가 어긋남**; 볼륨 핸들 갈래는 `:675 ~ :742`.

- **`0x4d0008` 볼륨 IOCTL — 빠짐 (틀림)**: 볼륨 갈래(`:675 ~ :742`)의 코드 목록(`kStorageQueryProperty`, `DiskGetDriveGeometry`, `DiskGetLengthInfo`, `StorageGetDeviceNumber`, `StorageCheckVerify`, `VolumeGetDiskExtents`)에 `0x4d0008` 이 없어 `fake()`(설정 응답) 아니면 `device_control_code_unverified` 로 감 (`:726 ~ :727`). 로그의 "성공인데 출력 안 씀" 은 설정 응답이 status 0 만 준 것으로 보임 `[추측: fake() 본문 일부만 읽음]`. 부르는 쪽은 길이 칸을 그대로 믿고 복사하니 (`GetVolumeNameForRoot +0x1f0 → +0x204`) **성공을 주려면 반드시 길이 칸과 이름을 써야 함**.
- **`\??\MountPointManager` 열기 — 빠짐 (틀림)**: `nt_create_file.cpp:209` `unsupported_prefix → stop("path_prefix_unsupported")` 는 status 가 아니라 멈춤이라 kernelbase 로 값이 안 돌아감. 이 장치를 지원 장치로 넣든가(열기 성공, 아래), 최소한 status 를 돌려줘야 함.
- **`NtQueryInformationProcess(0x17)` — 로그가 이미 맞음**: 두 번 불러서 (적어도 첫째가) 고정 디스크로 답해 `GetDriveTypeW` 가 3 을 얻고 `NtOpenFile` 로 간 것으로 보임 (로그 순서와 코드 순서가 같음).
- **`NtOpenFile(\??\C:, 0x100080, 3, 0x10)`**: 코드와 값 일치 (`+0x110`, `+0x104`, `+0xe5`).

### 엔진이 돌려줄 최소 바이트 (C: = `\Device\HarddiskVolume3`)

**`0x4d0008`** — status `0`, `Information = 0x30`, 출력 0x30 B (나머지 0x208 은 건드리지 않아도 됨):

```
2e 00  5c 00 44 00 65 00 76 00 69 00 63 00 65 00 5c 00 48 00 61 00 72 00 64 00 64 00 69 00 73 00 6b 00 56 00 6f 00 6c 00 75 00 6d 00 65 00 33 00
```
- 앞 2 B = `0x2e` (이름 23 글자 × 2 B), 이름 UTF-16LE 46 B, 널 문자 없음 (kernelbase 가 붙임). `Information`/`lpBytesReturned` 는 이 호출자가 안 읽음 — 그래도 `0x30` 이 진짜 약속에 맞음 `[추측]`.

**`NtCreateFile(\??\MountPointManager …)`** — status `0` 이면 충분 (`MountMgrCreateClose` 가 `FILE_DIRECTORY_FILE` 아니면 성공, `+0x3c ~ +0x4c`). 열린 핸들은 `0x6d0008` 한 가지만 받으면 됨.

**`0x6d0008`** (장부는 `\Device\HarddiskVolume3` 하나, GUID 는 `MountedDevices` 수집값):
- 입력 검사 (엔진이 진짜 mountmgr 흉내): 입력 길이 ≥ 0x18, 출력 길이 ≥ 0x20, `DeviceNameOffset = 0x18`, 이름 = `\Device\HarddiskVolume3` 이면 아래로 답.
- **첫 호출 (출력 0x20 B)**: status `0x80000005`, 출력 `+0` = Size, `+4` = 항목 수, `Information = 8`. 최소 Size = `8 + 0x18 + 0x60 = 0x80`, 항목 수 1 (링크 이름 하나만 줄 때). `[정적: QueryPointsFromMemory +0x39c ~ +0x3f0]`
- **둘째 호출 (출력 Size B 이상)**: status `0`, `Information = Size`. 최소 0x80 B:
  ```
  +0x00  80 00 00 00        Size
  +0x04  01 00 00 00        항목 수
  +0x08  20 00 00 00        링크 이름 오프셋 (= 0x20)
  +0x0c  60 00 00 00        링크 이름 길이 0x60  (u16 + 2 B 채움)
  +0x10  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00   고유ID·장치이름 칸 (kernelbase 안 읽음)
  +0x20  \??\Volume{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}  UTF-16LE 0x60 B
  ```
  - 진짜 mountmgr 처럼 하려면 항목을 링크마다 하나씩 (예: `\DosDevices\C:` 0x1c B 와 `\??\Volume{GUID}` 0x60 B) 두고, 이름 본문 순서는 **[고유ID][장치이름][링크 이름들]**, Size = `8 + 0x18 × 항목수 + 모든 길이 합`. 길이 0x60 짜리가 하나라도 있으면 kernelbase 는 그것을 고름 (다른 길이는 건너뜀).
  - GUID 글자는 `{`·`-`·`}` 자리만 맞으면 됨 (표 (2) 아래 설명). 호출자 출력 `\\?\Volume{GUID}\` 로 바뀌어 나감.
- **금지 꼴**: 첫 호출에 `0x80000005` 를 주면서 Size 를 안 쓰거나, 둘째 호출에도 계속 `0x80000005` 를 주면 `GetVolumeNameForRoot` 가 **끝없이 돎** (`+0x5e9`).

### "정직한 실패" 로 줄 status

- **`0x4d0008` 을 못 채울 때**: 음수 status 아무거나 → kernelbase 는 출력을 안 읽고 FALSE (`+0x1a6`). 권장 `0xC0000010` (STATUS_INVALID_DEVICE_REQUEST — mountmgr 가 모르는 IOCTL 에 주는 값, `MountMgrDeviceControl` 디스패치 끝 "IOCTL UNKNOWN") `[정적: mountmgr; 볼륨 드라이버가 같은 값을 주는지는 [공백]]`. 이때 LastError 는 `RtlNtStatusToDosError(0xC0000010)` (공개 값 1 `[추측]`).
- **`\??\MountPointManager` 를 못 열 때**: 음수 status (권장 `0xC0000034` 이름 없음) → `CreateFileInternal` 이 LastError 를 그 변환값(공개 값 2 `[추측]`)으로 세우고 -1 → `GetVolumeNameForRoot` FALSE. 단, `0xC0000022`(접근 거부)는 `NtCreateFile` 을 옵션 바꿔 **한 번 더** 부르게 함 (`+0x37a` → `+0x696`) — 피하는 게 쉬움.
- 둘 다 실패해도 `GetVolumeNameForVolumeMountPointW` 는 **그다음 syscall 을 더 부름** (표 (1) 9~11번: `NtCreateFile(\??\C:\)`, `NtFsControlFile(0x900a8)`). 엔진이 `\??\C:\` 열기(루트 디렉터리)·`FSCTL 0x900a8` 을 정직하게 못 답하면 거기서 또 멈춤. 정직한 답: 열기 성공 + `0x900a8` 은 reparse 아님 (공개 값 `0xC0000275` `[추측]`) → 출력 [0]=0, FALSE, LastError 는 그 변환값 (공개 값 4390 `[추측]`).
- **status `0`(성공) 인데 출력을 안 쓰는 대답은 최악**: 길이 칸이 스택 쓰레기라 `memcpy` 가 최대 64 KB 읽음.

## PDB GUID 확인

| 바이너리 | PDB | GUID+Age | 확인 |
|---|---|---|---|
| mountmgr.sys | `mountmgr.pdb` | `B5FC1BC04B06273FE01BB638A26661D51` | `fetch_pdb.py --info mountmgr.sys` (바이너리에서 읽음) = `ms/sym/mountmgr.sys.pdb.info.txt` = analyze 로그 `SetPdb` 65줄 |
| KernelBase.dll | `kernelbase.pdb` | `72CCE506944DE0267C172AD64287F9BD1` | `fetch_pdb.py --info KernelBase.dll` = `ms/sym/KernelBase.dll.pdb.info.txt` |
| shell32.dll | `shell32.pdb` | `011DE059CF3D0B8174FA9B5324EA76541` | 이번에 받음 (`fetch_pdb.py` 출력) |
| windows.storage.dll | `Windows.Storage.pdb` | `EF42E52BBA15CEFDA22CB070B2FB69881` | 이번에 받음 |

ntoskrnl PDB GUID(`C29EBFB06B78B3C020DCA66D99713F9E1`) 는 이 요청에서 안 씀 (확인 안 함).

## 동적으로 확인해야 할 것

- 진짜 볼륨(`\Device\HarddiskVolume3`)의 `0x4d0008` 출력: 길이 칸 값 · IOSB.Information 이 정말 `0x30` 인가. 멈춤점 `KERNELBASE!GetVolumeNameForRoot+0x171` (= `DeviceIoControl` 돌아온 자리) 에서 `eax`, `db @rbp-0x40 L0x30`.
- 첫 `0x6d0008` 이 정말 `0x80000005` 로 오는가, Size 값과 항목 수·항목 순서·링크 이름들 (`\DosDevices\C:` 외에 몇 개): `KERNELBASE!GetVolumeNameForRoot+0x37c` (첫째·둘째 호출 둘 다 서야 함), `+0x56f`(LastError 가 0xea 인가), 둘째 호출 뒤 `+0x37c` 에서 `db @rsi L<Size>`.
- `RtlNtStatusToDosError(0x80000005) = 0xea` 인지 (ntdll 쪽 표 — sast/138 이 ntdll 을 보고 있어 겹침 주의).
- 로그의 `NtQueryInformationProcess(0x17)` 둘 중 둘째의 호출자: `ntdll!NtQueryInformationProcess` 멈춤점 + 반환 주소를 `shell32.dll.pdb`/`windows.storage.dll.pdb` 공개 이름으로 찾기.
- `ShellExecute*` → `windows.storage!CInvokeCreateProcessVerb::Execute` (RVA `0xe1de0`) 가 실제로 불리는가, 그 안에서 `_FixupWorkingDirectory` (`0x17e25c`) `+0x84` 가 `CMountPoint::GetMountPoint` 를 부르는가 (멈춤점 `windows_storage!CInvokeCreateProcessVerb::_FixupWorkingDirectory` 와 `...::CMountPoint::_InitLocalDrive+0x47` 에서 `eax`).
- `NtCreateFile` 최종 옵션값 `0x20060` 이 코드의 어느 비트 합인지 (정적으로 끝까지 안 따라감).

## 다음 요청 후보

1. **windows.storage.dll · shell32.dll Ghidra analyze + 지도** (뿌리 `ShellExecuteExW`, `CInvokeCreateProcessVerb::Execute`) — 지금은 직접 호출만 봐서 `ShellExecute*` → `Execute` 가 이어지지 않음. PDB 는 이미 받아 둠 (`ms/sym/`). 메모리·시간이 있을 때 하나씩.
2. **`BasepGetVolumeNameFromReparsePoint` 실패 길 전체** — `NtCreateFile(\??\C:\)` 옵션과 `NtFsControlFile(0x900a8)` 뒤 갈래, 성공 길 (reparse 있을 때). 엔진의 "정직한 실패" 다음 벽.
3. **ntdll `RtlDosPathNameToNtPathName_U` · `RtlNtStatusToDosError`** — `C:\` → `\??\C:\` 와 `0x80000005` → 오류 번호 (sast/138 과 겹치니 그쪽 끝난 뒤).
4. **partmgr.sys / volmgr.sys** 의 `0x4d0008` 처리 — 진짜 볼륨 장치 쪽 출력 바이트 (요청서 범위 밖이었음).
5. `GetVolumePathNameInternalW` 가 실패 때 경로를 위로 잘라 가며 다시 부르는 루프 (`+0x2c4 ~ +0x4f4`) — `GetVolumePathNameW` 를 쓰는 다른 호출자용.

## 부록

### 근거 파일 (모두 `ms/` 아래, 레포 미커밋)
- kernelbase: `ms/disassemble/kernelbase/{GetVolumeNameForRoot,GetVolumeNameForVolumeMountPointW,GetVolumePathNameW,GetVolumePathNameInternalW,GetDriveTypeW,DeviceIoControl,CreateFileInternal,BaseSetLastNTError,BasepGetVolumeNameFromReparsePoint}.asm`, `_calls.tsv`, 같은 이름 `.c` (`ms/decomp/kernelbase/`).
- mountmgr: `ms/disassemble/mountmgr/{DriverEntry,MountMgrCreateClose,MountMgrDeviceControl,MountMgrQueryPoints,QueryPointsFromMemory,QueryDeviceName,QueryDeviceNameInternal,MountMgrSendDeviceControl}.asm` · `.c` (`ms/decomp/mountmgr/`).
- 질문 3 은 `.asm` 가 없고 objdump·PDB 조회 (재현: `objdump -d -M intel --no-show-raw-insn <dll>` 와 `llvm-pdbutil-14 dump -publics ms/sym/<dll>.pdb`, `.pdata` 함수 시작).

### shell 쪽 부르는 자리 이름 (직접 `call [import]` 만, 가장 가까운 PDB 공개 이름)
- windows.storage `GetVolumeNameForVolumeMountPointW`: `CFSFolder::_GetMountingPointInfo`, `CFSFolder::_GetSize`, `TryGetDeviceInterfaceFromVolume`, `CStorageFolderChangeTracker::RuntimeClassInitialize`, `CMountPoint::_InitLocalDrive`, `CGetVolumeNameForVolumeMountPointParams::Call`, `RebaseOnVolumeID`, `CFSTransfer::_ResolveAccessDeniedCDRom`, `RegistryHelper::DeleteVolume/ReadRegistry/WriteRegistry` (StorSvc), `GetRecreatableRecycleBinLocation`, `PathResolveReparsePoints`.
- windows.storage `GetVolumePathNameW`: `CMountPoint::GetMountPoint(PCWSTR…)`, `CStorageFolderChangeTracker::RuntimeClassInitialize`, `CFSFolder::GetAllowedEDPIdentities`, `IsIndexerSupportedDriveType`, `CEnumFiles::_IsMountedFolder`, `CDriveLetterCache::GetVolumePathNameAndCachedMountPoint`, `CDrivesFolder::GetAllowedEDPIdentities`.
- shell32 `GetVolumeNameForVolumeMountPointW`: `TryGetDeviceInterfaceFromVolume`, `CCheckDiskDialog::ConfigureVolumeStateDialog/_ThreadProc`, `SHChkDskDriveEx`, `_DrvPrshtApply`, `MountPointProperties`, `_GetVolumeNameForVolumeMountPoint`, `CEncryptFilePropSheetPage::InitSinglePrshtNoDlg`, `CGetVolumeNameForVolumeMountPointParams::Call`, `CMountPoint::_InitLocalDrive`.

## 판정

**분석성공** (질문 1·2 는 코드 줄로 닫힘, 질문 3 은 **부분**: "FALSE → 이 자리에서 지나감" 은 정적으로 닫고, "ShellExecute\* 가 그 자리를 타는가" 는 간접 호출이라 `[공백]`).
- 닫지 못한 것: 옵션 `0x20060` 의 비트 계산 · `RtlNtStatusToDosError` 표 값 · 진짜 볼륨 드라이버가 `0x4d0008` 에 주는 정확한 `Information` · ShellExecute\* → `Execute` 의 간접 연결.
- 스크립트 문제: 없음.

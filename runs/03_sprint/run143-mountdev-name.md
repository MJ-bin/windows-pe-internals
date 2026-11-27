# 2026-11-25 (작성일, 정적 분석 2026-11-25) run143 — 볼륨 이름 묻기 길 (Q269, 정적만): `\??\C:` 에 IOCTL `0x4d0008` 을 보내는 함수는 kernelbase **`GetVolumeNameForRoot`** (rva `0x24c30`, export 아님) 하나다 (`+0x162 MOV EDX,0x4d0008` · `+0x16c DeviceIoControl`). 출력 0x208 B 의 **앞 2 B = 이름 길이 (바이트, u16), 이름은 `+2` 부터** — 그 길이 칸을 **상한 검사 없이** 힙 할당 크기 (`칸 + 2`) 와 `memcpy` 길이로 쓴다 (`+0x1ac` · `+0x1f0 → +0x204`). 그래서 에뮬레이터의 "성공인데 출력 안 씀" 이 바로 이 `memcpy` 에서 터진다. 이 IOCTL 이 음수면 (`0x80000005` 포함) 재시도 없이 FALSE. 성공하면 같은 함수가 `\\.\MountPointManager` 를 열고 (`CreateFileInternal` → `NtCreateFile`) **`0x6d0008`** 을 보낸다 — 입력 = `0x18` B 머리 + 장치 이름, 출력 첫 0x20 B 는 진짜 mountmgr 가 `0x80000005` + 필요한 크기로 답하고, kernelbase 는 그 크기로 **횟수 상한 없이** 다시 부른다 (`+0x5e9 JMP → +0x360`). 항목 표에서 길이 정확히 `0x60` 인 `\??\Volume{GUID}` 를 골라 `\\?\Volume{GUID}\` 로 돌려준다. `NtQueryInformationProcess(0x17)` 은 이 함수가 아니라 export 호출자 `GetVolumeNameForVolumeMountPointW +0x7d` 가 먼저 부르는 **`GetDriveTypeW`** 안에 있다

> **고쳐 읽기 · 닫힘 (run156, sast/152 — Q290 + Q276a)**: 아래 "`ShellExecute*` → `Execute` 는 가상 함수라 직접 호출로 안 이어진다 `[공백]`" (요약 shell 쪽 · 2절 끝 · 나무 · PE_emulator 절) 과 "다음 요청 후보" 1 (windows.storage · shell32 analyze + 지도) 은 **정적으로 닫혔다** `[정적]` — shell32 `ShellExecuteExW` 는 지연 로드 썽크 한 줄, windows.storage `ShellExecuteExW +0xbc` → `ShellExecuteNormal +0x55` → `ExecuteNormal +0x265` → `_DoExecute +0xca` → `_ExecuteAssoc +0x175` ⇢ `[+0x48]` `CBindAndInvokeStaticVerb::Execute +0x18f` → `TryCreateProcessDdeHandler +0xc0` → `InitAndCallExecute +0x214` ⇢ `[+0x48]` **`CInvokeCreateProcessVerb::Execute`** (가상 칸 대상은 PE 가상표 값 `0x3212d0` · `0xe1de0`). 그 뒤는 이 문서 사슬 그대로 (`+0x58 Launch` → … → `_PrepareAndCallCreateProcess +0xdf _FixupWorkingDirectory`). 이 문서의 줄은 틀린 게 없다. "실제로 불리나" (동적 5 · Q275) 는 그대로 남는다. → `runs/03_sprint/run156-appinfo-runas.md` 1절 · 5절

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("`\??\C:` 에 IOCTL `0x4d0008` 을 보내는 함수가 출력에서 읽는 칸과, 바로 뒤 `\??\MountPointManager` 에 보내는 IOCTL", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/335_internals-mountdev-name.md`, 튜닝 `335_tune15.md` MD1, 병목 샘플 98 · 기본 대답 받은 샘플 124) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 볼륨 API · `ShellExecute*` 를 안 부른다 — `sample/*.c` · `log.h` 에 그런 이름 없음 `[소스]`, shell32 · windows.storage 는 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run142 와 같은 줄). mountmgr.sys 는 부팅 때 올라오는 드라이버지만 test.exe 길에 그 장치를 여는 syscall 이 없다. 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (kernelbase 줄 · 새 mountmgr 줄 · shell32 / windows.storage 줄 · 근거 줄) · 3장 파이프 절 뒤 새 절 · 4장 (Q269 닫힘 줄 · run143 줄 셋) · 5장 (run143 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q269** (PE_emulator 요청 335 mountdev-name, 사용자 결정 2026-11-25 — PE_emulator D154 의 셋째, 정적만). 결과서 판정 **분석성공** (질문 1 · 2 는 코드 줄로 닫힘, 질문 3 은 부분 — "FALSE → 그 자리에서 지나감" 은 닫고 "`ShellExecute*` 가 그 자리를 타나" 는 간접 호출이라 `[공백]`).
> - 이어 쓰는 앞 run: **run91** (kernelbase `DeviceIoControl` 갈래 — 장치 종류 9 면 `NtFsControlFile`, 아니면 `+0x6c NtDeviceIoControlFile`, 음수면 `+0x87 JS` → 경고면 바이트 수 씀 → `+0x154 BaseSetLastNTError` → FALSE. 이번 줄과 같음), **run84** (`CreateFileW` · `CreateFileInternal` depth 1 읽기 전용 — `CONOUT$` 길), **run118** (`CreateFileInternal +0x2f5` · `+0x61f` — `pShimData` 읽는 쪽), **run132** (`BaseSetLastNTError` 뽑음 · `GetFileType` → `NtQueryVolumeInformationFile`). 앞 run 과 어긋나는 곳은 없다.
> - 남은 동적 → **Q275 (동적: 결과서 동적 — GetVolumeNameForRoot+0x171 출력·Information · +0x37c 첫 0x6d0008 · NtQueryInformationProcess(0x17) 둘째 호출자 · ShellExecute→CInvokeCreateProcessVerb::Execute 불리나)**, 남은 정적 → **Q276 (정적: 결과서 다음 요청 후보 1~5 — windows.storage/shell32 analyze·지도 · BasepGetVolumeNameFromReparsePoint · RtlDosPathNameToNtPathName_U/RtlNtStatusToDosError 표 · partmgr/volmgr 0x4d0008 · GetVolumePathNameInternalW 고리 · NtCreateFile 옵션 0x20060 계산)**.

## 요약

- **`0x4d0008` 을 싣는 줄은 kernelbase 전체에 하나** `[정적]`: `GetVolumeNameForRoot +0x162 MOV EDX,0x4d0008` (결과서가 kernelbase 전체 `objdump` 에서 즉시값을 찾음). 같은 함수 안 차례: `RtlDosPathNameToNtPathName_U` → 끝 `\` 떼고 드라이브 글자 대문자 → **`NtOpenFile(\??\C:, 0x100080, 공유 3, 옵션 0x10)`** (`+0x126`) → **`DeviceIoControl(0x4d0008, 입력 0·0, 출력 [RBP-0x40], 0x208)`** (`+0x16c`) → `NtClose` (`+0x178`).
  - `DeviceIoControl` 은 `0x4d0008 >> 16 = 0x4d` ≠ 9 라 `NtDeviceIoControlFile` (`+0x6c`) 을 낸다 (run91 과 같은 갈래).
- **출력에서 읽는 칸** `[정적]`: `+0` u16 = 이름 길이 (**바이트**), `+2` 부터 이름 (널 없음 — kernelbase 가 붙임). 길이 칸 → `Length` (`+0x1b0`) · `MaximumLength = 칸 + 2` (`+0x1b5 ~ +0x1bd`) · 힙 크기 `칸 + 2` (`+0x1d6`) · **`memcpy` 길이** (`+0x1f0 → +0x204`). `+0x1a6` 와 `+0x204` 사이에 `≤ 0x206` 비교가 **없다**. 16비트 덧셈이라 칸이 `0xFFFE` · `0xFFFF` 면 할당이 0 ~ 1 B 로 돌아 힙을 넘는다.
- **첫 IOCTL 실패** `[정적]`: 음수 status 면 어떤 값이든 (`0x80000005` 포함) `+0x1a6 JZ → +0x611` FALSE. **더 큰 버퍼로 다시 부르지 않는다.** LastError = `DeviceIoControl` 이 세운 `RtlNtStatusToDosError(status)`.
- **바로 뒤 `\??\MountPointManager`** `[정적]`: `CreateFileInternal(L"\\.\MountPointManager", 접근 0, 공유 3, OPEN_EXISTING, {크기 0x20, 속성 0x80})` (`+0x32f`) → 안에서 `NtCreateFile` (`CreateFileInternal +0x36c`, 접근 `| 0x100080`, 처분 1). 그다음 **`DeviceIoControl(0x6d0008, 입력 = 이름길이 + 0x18 B, 출력 0x20 B)`** (`+0x377`).
  - 입력 머리 `0x18` B: `+0x10` = `0x18` (장치 이름 오프셋), `+0x14` = 이름 길이, 나머지 0, `+0x18` 부터 장치 이름 (`+0x261 ~ +0x286`).
  - 실패면 LastError (`GS:[0x68]`) 가 `0xea` 인지 봄 (`+0x567 ~ +0x56f`). 맞으면 **출력 첫 4 B 를 새 크기로** 버퍼를 다시 잡고 (`+0x587` · `+0x5b5`) **무조건** `+0x5e9 JMP → +0x360` 으로 다시 부른다. 빠져나가는 길은 힙 할당 실패 (`+0x5cc`) 뿐 — **상한 없음.** `0xea` 가 아니면 LastError `0x57` 로 FALSE.
  - 성공하면 `+4` = 항목 수, 항목 i = `+8 + 0x18*i`, 그 `+0` (이름 오프셋, u32) · `+4` (이름 길이, u16) 만 읽는다 (`+0x3b9` · `+0x3c8` · `+0x3ce`). 길이가 **정확히 `0x60`** 이고 `\??\Volume{` · `-` 넷 · `}` 자리가 맞는 항목 하나를 골라 (16진 자리는 안 봄) 2번째 글자를 `\` 로 바꾸고 끝에 `\` + NUL 을 붙여 `\\?\Volume{GUID}\` (50 글자) 를 돌려준다.
- **mountmgr 쪽** `[정적]`: `DriverEntry` 가 `\Device\MountPointManager` 장치와 `\DosDevices\MountPointManager` 링크를 만들고, 열기 (`MountMgrCreateClose`) 는 옵션 0번 비트 (`FILE_DIRECTORY_FILE` `[추측: 공개 헤더]`) 만 거른다. `MountMgrDeviceControl +0x23c CMP R13D,0x6d0008` → `MountMgrQueryPoints` (입력 검사 7가지) → `QueryPointsFromMemory` (볼륨 장치에 **자기가** `0x4d0008` 을 보내 정식 이름을 다시 받고 장부와 견줌, 출력이 작으면 머리 8 B 만 쓰고 `0x80000005`).
- **`NtCreateFile` 실패면** `[정적]`: `CreateFileInternal` 이 `BaseSetLastNTError(status)` 뒤 `-1` → `GetVolumeNameForRoot +0x33b → +0x6eb` FALSE. export `GetVolumeNameForVolumeMountPointW` 는 그 뒤 **`BasepGetVolumeNameFromReparsePoint` 로 한 번 더** 시도 (`+0xf0`, 안에서 `NtCreateFile(경로)` · `NtFsControlFile(0x900a8)`) → 또 실패면 출력 첫 글자 0, FALSE.
- **shell 쪽 (부분)** `[정적: objdump · PDB 공개 이름 — .asm 없음]`: windows.storage `CInvokeCreateProcessVerb::_FixupWorkingDirectory +0x84` → `CMountPoint::GetMountPoint(int)` → … → `_InitLocalDrive +0x40 GetVolumeNameForVolumeMountPointW`. FALSE 는 HRESULT 로 올라가고 `_FixupWorkingDirectory +0x8b js` → 함수 끝 (반환 void) — **그 자리에선 그냥 지나간다.** `ShellExecute*` → `Execute` 사이는 가상 함수라 `[공백]`.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백한째 바퀴. plan **Q269**. **짐 지도 바퀴 아님.**
- 정적: kernelbase 는 이미 analyze 됨 (`^이름$` export 만, 볼륨 함수 15개는 결과서보다 먼저 export 돼 있던 것 재사용). **mountmgr.sys 는 처음 analyze** — 결과서에 따르면 요청서가 시작할 때 이미 analyze (17:30:34, 8초) · export (17:34:13, 12함수 depth 0) 가 끝나 있었고, 결과서는 다시 돌리지 않았다 (`ms/ghidra/mountmgr.analyze.log` 65줄 `SetPdb:`, `_index.tsv` 194함수). shell32 · windows.storage 는 **analyze 안 함**, PDB 만 받음.

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| kernelbase | `72CCE506944DE0267C172AD64287F9BD1` | 같음 |
| mountmgr.sys | `B5FC1BC04B06273FE01BB638A26661D51` | **새 줄** (처음 analyze) |
| shell32 | `011DE059CF3D0B8174FA9B5324EA76541` | **새 줄** (PDB 받기만) |
| windows.storage (`Windows.Storage.pdb`) | `EF42E52BBA15CEFDA22CB070B2FB69881` | **새 줄** (PDB 받기만) |

- 버전 대조 (결과서): `0x4d0008` 즉시값 줄은 kernelbase 전체 `objdump` 에서 한 곳 (VA `0x180024d92` = `GetVolumeNameForRoot +0x162`) — `.asm` 과 원본 바이트 같음. mountmgr `MountMgrQueryPoints +0x3b` · `+0xeb`, `QueryPointsFromMemory +0x141` 도 `objdump` 와 `.asm` 같음. flow-writer 가 `.asm` 으로 다시 본 줄은 아래 "방법".

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): `NtQueryInformationProcess(0x17)` 둘 → `NtOpenFile(\??\C:, 0x100080, 3, 0x10)` → `NtDeviceIoControlFile(…, 0x4d0008, 입력 0·0, 출력, 0x208)` (기본 대답 — 성공인데 출력 안 씀) → `NtClose` → `NtCreateFile(\??\MountPointManager, 0x100080, 0x80, 3, 1, 0x20060)` (`path_prefix_unsupported`). 예외 자리 ntdll `RtlCopyMemory +0x100 · +0x104 · +0x109` (스택 끝 읽기). 그때 맨 위 API 는 `ShellExecuteW` 51 · `ShellExecuteA` 7 · `ShellExecuteExW` 7 (65 / 98). 요청서 `_workspace/sast/140_mountdev-name.md`:
  1. `0x4d0008` 을 부르는 함수 · 호출 자리 · 출력에서 읽는 칸 · status 갈래 · 길이 칸을 검사 없이 복사 길이로 쓰는 줄.
  2. 바로 뒤 `\??\MountPointManager` IOCTL — 코드 · 입력 칸 · 출력 칸 · 실패 갈래, mountmgr 쪽 검사 차례, `NtCreateFile` 실패 때 kernelbase 반환.
  3. (되면) shell 쪽이 FALSE 를 받으면 지나가나 멈추나.
- 쓰는 곳 (원문): 팀원 `ext/syscall/syscall_layer_cpp/src/file/nt_device_io_control_file.cpp:617 ~ :654` (볼륨 핸들 IOCTL) · `src/file/nt_create_file.cpp` (`\??\MountPointManager`). **요청서의 줄 번호는 밀렸다** — 아래 "정정" 2.

## 방법

- 정적 sast/140 (결과서 `_workspace/sast/140_mountdev-name_result.md`, **분석성공**, 스크립트 문제 없음):
  - kernelbase: 있던 `.asm` 읽기 — `GetVolumeNameForRoot` · `GetVolumeNameForVolumeMountPointW` · `GetVolumePathNameW` · `GetVolumePathNameInternalW` · `GetDriveTypeW` · `DeviceIoControl` · `CreateFileInternal` · `BaseSetLastNTError` · `BasepGetVolumeNameFromReparsePoint` (+ 이웃 `…DosLetterNameFromNTName` · `…GUIDFromNTName` · `GetFinalPathNameByHandleW` · `GetVolumePathNamesForVolumeNameW` — 답엔 안 씀). 새로 뽑은 함수 0개.
  - mountmgr: `DriverEntry` · `MountMgrCreateClose` · `MountMgrDeviceControl` · `MountMgrQueryPoints` · `QueryPointsFromMemory` · `QueryDeviceName` · `QueryDeviceNameInternal` · `MountMgrSendDeviceControl` 등 12개 depth 0.
  - shell32 · windows.storage: **Ghidra 안 씀.** `objdump -d` + `.pdata` 함수 시작 + PDB 공개 이름 (`llvm-pdbutil-14 dump -publics`) 으로 가져오기 칸 → `call [IAT]` 자리를 찾고 **직접 `call` 만** 따라감. 임시 파일은 레포 밖. 이 줄들은 `.asm` 이 없어 flow-writer 가 대조하지 못했다 — 결과서 근거 수준 그대로 옮긴다.
- PE_emulator 줄 번호는 flow-writer 가 `../PE_emulator` 의 두 파일을 **줄 번호만** 훑어 맞췄다 (읽기만, 고치지 않음): `nt_device_io_control_file.cpp` 750줄, `:673 ~ :678` 에서 `is_volume` 갈래 시작 · `:726 ~ :727` `fake()` / `device_control_code_unverified`, `:617 ~ :620` · `:652 ~ :656` 은 다른 장치 갈래 (`fake()` · `kKsecConfigRequest`). `nt_create_file.cpp:209` `unsupported_prefix → stop("path_prefix_unsupported")`.
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - kernelbase `GetVolumeNameForRoot +0x56 · +0x64 · +0xc2 · +0xe5 · +0x104 · +0x110 · +0x115 · +0x126 · +0x136 · +0x14b · +0x14e · +0x153 · +0x156 · +0x15a · +0x162 · +0x167 · +0x16c · +0x178 · +0x198 · +0x1a6 · +0x1ac · +0x1b0 · +0x1b5 · +0x1bd · +0x1d6 · +0x1ea · +0x1f0 · +0x1f5 · +0x204 · +0x20e · +0x216 · +0x230 · +0x238 · +0x25d · +0x261 · +0x265 · +0x269 · +0x276 · +0x286 · +0x2b4 · +0x2c2 · +0x2e8 · +0x2f0 · +0x305 · +0x32f · +0x337 · +0x33b · +0x360 ~ +0x37e · +0x380 · +0x386 · +0x38b · +0x3a2 · +0x3b6 · +0x3b9 · +0x3bc · +0x3c4 · +0x3c8 · +0x3ce · +0x49f · +0x4a4 ~ +0x4cd · +0x4f4 ~ +0x51e · +0x52a · +0x567 · +0x56f · +0x575 · +0x584 ~ +0x5fb · +0x600 · +0x611 · +0x618 · +0x630 · +0x673 · +0x6a2 ~ +0x75b · +0x766`.
  - kernelbase `GetVolumeNameForVolumeMountPointW +0x24 · +0x3f · +0x6e · +0x7d · +0xbc · +0xd9 · +0xe2 · +0xf0 · +0xfb · +0x104 · +0x124 · +0x137 · +0x139 · +0x14e · +0x16d · +0x172 · +0x202 · +0x20f`; `GetVolumePathNameW +0xb`; `GetVolumePathNameInternalW +0x226 · +0x288 · +0x2c4 · +0x3f2 · +0x4a9 · +0x4f4 · +0x54f`; `GetDriveTypeW +0xdd · +0xe1 · +0xef · +0xf5 · +0xff · +0x101 · +0x106 · +0x10f · +0x518`; `DeleteVolumeMountPointW +0x6f`; `DeviceIoControl +0x2b · +0x55 · +0x6c · +0x7f · +0x87 · +0xa0 · +0x154 · +0x159 · +0x168 · +0x192 · +0x1b3 · +0x1de`; `BaseSetLastNTError +0x6 · +0x16`; `CreateFileInternal +0x251 ~ +0x26e · +0x279 · +0x337 · +0x366 · +0x36c · +0x373 ~ +0x39d · +0x481 · +0x696 ~ +0x6ab` · 조각 `FUN_18019b82e +0x0 ~ +0x22 · +0x47 · +0xb0 · +0xb5` · 조각 `FUN_18002635d +0x0 · +0xb1 · +0xb6 · +0xbc · +0xe2`; `BasepGetVolumeNameFromReparsePoint +0xb0 · +0xbc · +0x100 · +0x116 · +0x26c · +0x4c0`; `BasepGetVolumeGUIDFromNTName +0x131`; `FindFirstVolumeW +0xf9`; `BasepGetVolumeDosLetterNameFromNTName +0x152`; `GetVolumePathNamesForVolumeNameW +0x26b`; `_calls.tsv` 의 `GetVolumeNameForRoot` · `GetDriveTypeW` · `GetVolumeNameForVolumeMountPointW` callee 줄 전부.
  - mountmgr `DriverEntry +0x6a · +0x2df · +0x3e5 · +0x3e9 · +0x3f7`; `MountMgrCreateClose +0x10 · +0x15 · +0x3c · +0x4c`; `MountMgrDeviceControl +0x2e · +0x66 · +0x79 · +0x23c · +0x252 · +0x349 · +0x377`; `MountMgrQueryPoints +0x3b · +0x40 · +0x4a · +0x4f · +0x5f · +0x6c · +0x77 · +0x7b · +0x92 · +0xac · +0xc0 · +0xc6 · +0xe5 · +0xeb · +0xf0 · +0xfb · +0x108 · +0x122 · +0x125 · +0x153 · +0x15b · +0x1fa · +0x1fe · +0x215 · +0x224 · +0x228`; `QueryPointsFromMemory +0x79 · +0x141 · +0x155 · +0x180 · +0x1c4 · +0x250 · +0x2ce · +0x39c · +0x3a0 · +0x3a5 · +0x3a9 · +0x3b2 · +0x3f0 · +0x41f · +0x4a5 · +0x4b7 · +0x4e8 · +0x51f · +0x534`; `QueryDeviceName +0x14a · +0x15d · +0x188`; `QueryDeviceNameInternal +0xb7 · +0xca · +0xf5`.
  - `pdb_aliases.py --lookup KernelBase.dll 0x128bf4` (아래 "flow-writer 대조").
  - 결과서와 다른 곳 → 아래 "flow-writer 대조". 전부 뜻은 같다.

## 결과

비유:

- **`0x4d0008` 은 "이 방의 정식 주소를 알려 주세요" 라는 쪽지다.** 볼륨 장치는 쪽지 첫 칸에 "주소 길이" 를, 그 뒤에 주소를 적어 돌려준다.
- kernelbase 는 첫 칸의 길이를 **의심 없이 믿고** 그만큼 베껴 간다. 에뮬레이터는 "네" 라고만 하고 쪽지를 빈 채로 돌려줘서, 첫 칸에 남아 있던 아무 숫자만큼 베끼다가 스택 끝을 넘었다 `[추측: 비유]`.
- 그다음 **`0x6d0008` 은 주소록 관리소 (MountPointManager) 에 "이 주소의 별명들을 다 주세요" 라는 부탁이다.** 관리소는 먼저 "종이가 모자라요, 이만큼 필요해요" 라고 하고, kernelbase 는 그만큼 종이를 다시 준비해 또 부탁한다 — **몇 번이든.** 그 별명들 가운데 `\??\Volume{…}` 모양 하나를 골라 간다.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{kernelbase,mountmgr}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `kernelbase!GetVolumeNameForVolumeMountPointW` (export, rva `0x268f0`) | `+0x7d` | `+0x82` | `GetDriveTypeW` (`0x253c0`) | [정적] |
| 〃 | `+0xd9` · `+0x14e` · `+0x16d` | `+0xde` · `+0x153` · `+0x172` | **`GetVolumeNameForRoot`** (`0x24c30`) — `+0x14e` 는 경로가 `X:\` (길이 6 B, `+0xbc CMP [RSP+0x20],6`) 일 때, `+0x16d` 는 reparse 이름을 얻은 뒤 | [정적] |
| 〃 | `+0xf0` | `+0xf5` | `BasepGetVolumeNameFromReparsePoint` (`0x25950`) — `GetVolumeNameForRoot` FALSE 뒤 (`+0xe2`) | [정적] |
| 〃 | `+0x202` | `+0x207` | `BaseSetLastNTError(0xC0000033)` — 끝이 `\` 아님 (`+0x6e` · `+0x20f`) | [정적] |
| `kernelbase!GetVolumePathNameW` (export, `0xff290`) | `+0xb` `JMP` | — | `GetVolumePathNameInternalW` (`0x26b10`) — 꼬리 점프 | [정적] |
| `kernelbase!GetVolumePathNameInternalW` | `+0x226` | `+0x22b` | `GetDriveTypeW` | [정적] |
| 〃 | `+0x288` · `+0x3f2` · `+0x4a9` · `+0x54f` | `+0x28d` · `+0x3f7` · `+0x4ae` · `+0x554` | `GetVolumeNameForRoot` | [정적] |
| 〃 | 조각 `FUN_1800272d2 +0x2f` | `+0x34` | `GetDriveTypeW` (결과서엔 없음 — flow-writer 대조) | [정적] |
| `kernelbase!DeleteVolumeMountPointW` | `+0x6f` | `+0x74` | `GetDriveTypeW` | [정적] |
| `kernelbase!GetDriveTypeW` | **`+0xe1`** | **`+0xe8`** | **`NtQueryInformationProcess(-1, 0x17, [RBP-0x40], 0x24, NULL)`** — `+0xdd LEA EDX,[R9-0xd]` (R9D = `0x24`) | [정적] |
| `kernelbase!GetVolumeNameForRoot` | `+0x56` | `+0x5d` | `RtlDosPathNameToNtPathName_U(경로, &이름, 0, 0)` — 0 이면 `+0x64 → +0x618` LastError 3 | [정적] |
| 〃 | `+0xc2` | `+0xc9` | `toupper` (드라이브 글자) | [정적] |
| 〃 | **`+0x126`** | **`+0x12d`** | **`NtOpenFile`** — 접근 `0x100080` (`+0x110`) · 공유 3 (`+0x104`) · 옵션 `0x10` (`+0xe5`) · 속성 `0x40` (`+0x115`) | [정적] |
| 〃 | **`+0x16c`** | **`+0x171`** | **`DeviceIoControl(h, 0x4d0008, 0, 0, [RBP-0x40], 0x208, …)`** (`+0x14b` · `+0x153` · `+0x156` · `+0x15a` · `+0x162` · `+0x167`) | [정적] |
| 〃 | `+0x178` · `+0x198` | `+0x17f` · `+0x19f` | `NtClose` · `RtlFreeHeap` (경로 이름) | [정적] |
| 〃 | `+0x1d6` · `+0x204` | `+0x1dd` · `+0x209` | `RtlAllocateHeap(칸 + 2)` · **`memcpy(새 버퍼, [RBP-0x3e], 칸)`** | [정적] |
| 〃 | `+0x238` · `+0x286` | `+0x23f` · `+0x28b` | `RtlAllocateHeap(이름길이 + 0x18)` (`+0x230 ADD R8,0x18`) · `memcpy` (입력 머리 뒤 이름) | [정적] |
| 〃 | `+0x2c2` | `+0x2c9` | `RtlAllocateHeap(0x20)` — 출력 버퍼 (`+0x2b4 LEA R8D,[RDI+0x1d]`) | [정적] |
| 〃 | **`+0x32f`** | **`+0x334`** | **`CreateFileInternal(L"\\.\MountPointManager", 0, 3, 3, {0x20, 0x80}, 기능 플래그)`** — 문자열 `[0x1802a0ba8]` (`+0x305`) | [정적] |
| 〃 | **`+0x377`** | **`+0x37c`** | **`DeviceIoControl(h, 0x6d0008, 입력, 이름길이 + 0x18, 출력, 0x20 / 새 크기, …)`** (`+0x360 ~ +0x372`) — 재시도는 `+0x5e9 JMP → +0x360` | [정적] |
| 〃 | `+0x38b` · `+0x3a2` | `+0x390` · `+0x3a9` | `CloseHandle` (MPM 핸들) · `RtlFreeHeap` (입력) | [정적] |
| 〃 | `+0x593` · `+0x5b5` | `+0x59a` · `+0x5bc` | `RtlFreeHeap` (옛 출력) · `RtlAllocateHeap(출력 첫 4 B)` — `0xea` 재시도 갈래 | [정적] |
| 〃 | `+0x6eb` | `+0x6f0` | 예 / 아니오 검사 함수 (`0x128bf4`, 전역 칸 `0x1803a9d80` 캐시) — 같은 주소 이름 11개, 후보: `IsBrokeredGetFileAttributesExWPresent` · `IsBrokeredCreateFile2Present` … 반환은 이 자리에서 안 씀. MPM 열기 실패 갈래 | [정적] |
| `kernelbase!DeviceIoControl` (`0x23660`) | `+0x6c` | `+0x73` | `NtDeviceIoControlFile` — 종류 ≠ 9 (`+0x2b CMP EAX,9`) | [정적] |
| 〃 | `+0x168` | `+0x16f` | `NtFsControlFile` — 종류 9 (`0x900a8` 는 이쪽) | [정적] |
| 〃 | `+0x154` | `+0x159` | `BaseSetLastNTError(status)` → `+0x6 RtlNtStatusToDosError` · `+0x16 RtlSetLastWin32Error` | [정적] |
| 〃 | `+0x1de` | `+0x1e5` | `NtWaitForSingleObject` — `0x103` 일 때 (`+0x7f`) | [정적] |
| `kernelbase!CreateFileInternal` (`0x25ea4`) | **`+0x36c`** | **`+0x373`** | **`NtCreateFile`** — 접근 `\| 0x100080` (`+0x337`), 처분 1 (`+0x481`), 옵션 `[RSP+0x60]` | [정적] |
| 〃 | `+0x388` | `+0x38d` | 기능 검사 `Feature_Servicing_CD_PathRedirectionAPI_2503__private_IsEnabledDeviceUsageNoInline` — 0 이면 조각 `FUN_18019b82e` 길 | [정적] |
| 〃 | 조각 `FUN_18019b82e +0x47` | `+0x4e` | `NtCreateFile` 한 번 더 — `0xC0000022` 일 때 옵션 비트 `0x20000` 지움 (`+0x37a` → `+0x696 ~ +0x6ab` → 조각 `+0x22 BTR EAX,0x11`) | [정적] |
| 〃 | 조각 `FUN_18002635d +0xb1` · 조각 `FUN_18019b82e +0xb0` | `+0xb6` · `+0xb5` | `BaseSetLastNTError(status)` — 실패, 핸들 `-1` (조각 `FUN_18002635d +0x0 OR [RSP+0x70],-1`) | [정적] |
| `kernelbase!BasepGetVolumeNameFromReparsePoint` | `+0xb0` | `+0xb5` | `CreateFileInternal(경로, 0, 3, 3, {속성 0x80, 플래그 0x2200000})` — `-1` 이면 `+0xbc → +0x26c` | [정적] |
| 〃 | `+0x100` | `+0x105` | `DeviceIoControl(h, 0x900a8, 0, 0, 버퍼, 0x1c)` → `NtFsControlFile` — 실패면 `+0x116 → +0x4c0` | [정적] |
| `mountmgr!DriverEntry` (rva `0x1f078`) | `+0x6a` · `+0x2df` | `+0x71` · `+0x2e4` | `IoCreateDevice(\Device\MountPointManager)` · `GlobalCreateSymbolicLink(\DosDevices\MountPointManager)` — 함수표 `+0x3e5` · `+0x3e9` · `+0x3f7` | [정적] |
| `mountmgr!MountMgrDeviceControl` (`0x17940`) | `+0x66` · `+0x79` | `+0x6d` · `+0x80` | `KeWaitForSingleObject` (뮤텍스) · `PsIsHostSilo` | [정적] |
| 〃 | **`+0x252`** · `+0x377` | **`+0x257`** · `+0x37c` | **`MountMgrQueryPoints`** (`0x19ac0`) — `+0x23c CMP R13D,0x6d0008` (호스트 silo) · `+0x349 SUB EAX,0x6d0008` (아닌 길) | [정적] |
| `mountmgr!QueryPointsFromMemory` (`0x19fe0`) | `+0x79` | `+0x80` | `IoGetDeviceObjectPointer(장치 이름, 0x80)` | [정적] |
| 〃 | `+0x155` · `+0x180` | `+0x15c` · `+0x187` | `IoBuildDeviceIoControlRequest(0x4d0008)` (`+0x141`) · `IofCallDriver` — 볼륨 장치에 이름 묻기 | [정적] |
| 〃 | `+0x1c4` · `+0x250` | `+0x1cb` · `+0x257` | `RtlDuplicateUnicodeString` · `RtlEqualUnicodeString` (장부 `DeviceName` 과) | [정적] |
| 〃 | `+0x41f` · `+0x4a5` · `+0x4b7` · `+0x534` | `+0x424` · `+0x4aa` · `+0x4bc` · `+0x539` | `memset(출력 + 8, 0, 항목수 × 0x18)` · `memcpy` ×3 (고유ID · 장치 이름 · 링크 이름) | [정적] |
| `mountmgr!QueryDeviceName` · `QueryDeviceNameInternal` | `+0x15d / +0x188` · `+0xca / +0xf5` | `+0x164 / +0x18f` · `+0xd1 / +0xfc` | 같은 `0x4d0008` 요청 (`+0x14a` · `+0xb7`) | [정적] |
| windows.storage `CInvokeCreateProcessVerb::_FixupWorkingDirectory` (`0x17e25c`) | `+0x84` | — | `CMountPoint::GetMountPoint(int, CMountPoint**)` (`0xf0b5c`) — objdump · 결과서만 | [정적] |
| windows.storage `CMountPoint::_InitLocalDrive` (`0x21a58c`) | `+0x40` | — | `GetVolumeNameForVolumeMountPointW` (글자 수 `0x32`, `+0x30`) — objdump · 결과서만 | [정적] |

### 1. `0x4d0008` — 출력 칸과 갈래 (질문 1) [정적]

| 칸 / 갈래 | 내용 | 줄 |
|---|---|---|
| 길이 칸 | `+0` u16, **바이트** | `+0x1ac MOVZX EAX, word [RBP-0x40]` |
| 이름 | `+2` 부터, 널 없음 | `+0x1f5 LEA RDX,[RBP-0x3e]` |
| 길이 쓰는 곳 | `Length` (`+0x1b0`) · `MaximumLength = 칸 + 2` (`+0x1b5 ADD AX,2` ~ `+0x1bd`) · 힙 `칸 + 2` (`+0x1d6`) · `memcpy` 길이 (`+0x1f0 MOVZX R8D` → `+0x204`) | 위 |
| 널 놓기 | `칸 >> 1` 자리에 0 (글자 수로 바꾸는 곳은 여기뿐) | `+0x20e ~ +0x216` |
| **검사** | `≤ 0x206` 비교 **없음** (`+0x1a6 ~ +0x204` 에 `CMP` · `JA` 없음). 버퍼를 미리 0 으로 안 채움 (`+0x52` 의 0 채우기는 IOSB 쪽) | — |
| 반환 바이트 수 | `lpBytesReturned` (`+0x14e`) 는 안 읽음 (`+0x58b` 에서 덮어씀) | — |
| `≥ 0` | 위 칸 읽기 | `+0x1ac ~` |
| `< 0` (어떤 값이든) | `+0x1a6 JZ → +0x611 XOR EAX,EAX` — 출력 안 읽음, **재시도 없음** | `+0x1a6` · `+0x611` |
| LastError | `DeviceIoControl +0x154 BaseSetLastNTError(status)` — 경고 (`0x8…`) 면 그 전에 IOSB.Information 을 바이트 수로 (`+0x192 ~ +0x1b3`, run91 과 같음) | `DeviceIoControl` |

- **에뮬레이터 예외 자리와 맞물림** `[정적 + 추측]`: "성공인데 출력 안 씀" 이면 `[RBP-0x40]` 의 쓰레기 u16 이 길이가 되어 `+0x204 memcpy` 가 `RBP-0x3e` 부터 최대 64 KB 를 읽는다. 원문 예외 자리 (ntdll `RtlCopyMemory +0x100 · +0x104 · +0x109`) 는 이 `memcpy` 의 몸으로 보인다 `[추측: kernelbase 의 memcpy (`0x196e77`) → ntdll 연결은 안 따라감]`.

### 2. `\??\MountPointManager` 와 `0x6d0008` (질문 2) [정적]

**2-a. kernelbase 쪽 — 입력 머리 (`0x18` B + 이름)**

| 오프셋 | 크기 | 값 | 줄 |
|---|---|---|---|
| `+0x00` · `+0x04` · `+0x08` · `+0x0c` | 4 · 2 · 4 · 2 | 0 (링크 이름 · 고유ID 오프셋 · 길이) | `+0x261 MOVUPS [R14],XMM0` |
| `+0x10` | 4 | **`0x18`** (장치 이름 오프셋) | `+0x265` 뒤 `+0x269 MOV dword [R14+0x10],0x18` |
| `+0x14` | 2 | **이름 길이 (바이트)** = `0x4d0008` 의 길이 칸 | `+0x276 MOV word [R14+0x14],AX` |
| `+0x16` | 2 | 0 | `+0x265` |
| `+0x18` | 이름 길이 | 장치 이름 UTF-16LE (널 없음) | `+0x25d LEA RCX,[R14+0x18]` · `+0x286 memcpy` |

(칸 이름 `SymbolicLinkNameOffset` · `UniqueIdOffset` · `DeviceNameOffset` 류는 공개 헤더 `[추측: 공개 헤더]`. mountmgr 가 읽는 칸 번호는 `MountMgrQueryPoints +0x4a` (`+4`) · `+0x5f` (`+0xc`) · `+0x6c` (`+0x14`) · `+0x92` (`+8`) · `+0xac` (`+0x10`) — 위와 같다.)

**2-b. kernelbase 쪽 — 출력에서 읽는 칸**

| 오프셋 | 크기 | 뜻 | 줄 |
|---|---|---|---|
| `+0x0` | 4 | `Size` — 필요한 총 바이트. **`0xea` 재시도 때만 읽음** | `+0x587 MOV EBX,[RSI]` |
| `+0x4` | 4 | 항목 수 | `+0x3b9 CMP EDX,[RSI+4]` |
| `+8 + 0x18*i + 0` | 4 | 링크 이름 오프셋 (출력 버퍼 시작 기준) | `+0x3ce` |
| `+8 + 0x18*i + 4` | 2 | 링크 이름 길이 (바이트) | `+0x3c8` |
| `+8 + 0x18*i + 8 ~ +0x17` | — | 고유ID · 장치 이름 칸 — **안 읽음** | — |
| 오프셋이 가리키는 곳 | `0x60` | `\??\Volume{GUID}` (널 없음) | 검사 `+0x3dd ~ +0x4aa`, 복사 `+0x4c9 ~ +0x503` |

- 고르는 조건: 길이 `0x60` (`0x62` 는 끝이 `\` 면 앞 검사까지 가다 `+0x49f CMP R8W,0x60` 에서 떨어짐), 글자 자리 `\` `?`(또는 `\`) `?` `\` `Volume` (대소문자 구분) `{` (10) `-` (19 · 24 · 29 · 34) `}` (47). 16진 자리는 안 봄. 안 맞으면 `+0x5f9 INC EDX` 다음 항목, 끝까지 없으면 `+0x3bc JNC → +0x673` LastError `0x57` FALSE.
- 맞으면: 호출자 글자 수 `× 2 ≥ 0x64` 가 아니면 `+0x4c3 JC → +0x766` LastError `0xce` FALSE. 맞으면 `0x60` B 복사 → `+0x508` 2번째 글자 `\` → `+0x50d` `[R15+0x60]` 에 `\` + NUL (`ECX = 0x5c`, `+0x4cd`) → `+0x52a MOV EAX,1`. 결과 `\\?\Volume{GUID}\` 50 글자 = windows.storage `_InitLocalDrive` 가 주는 `0x32` 와 딱 맞음 (결과서).

**2-c. 실패 갈래와 끝없는 고리**

- `+0x380 JZ → +0x567`: `GS:[0x68]` (LastError) 가 **`0xea`** 면 재시도, 아니면 `+0x575 JNZ → +0x386` 핸들 닫고 LastError `0x57` FALSE.
- 재시도: `+0x587` 출력 첫 4 B → EBX → `[RSP+0x50]` (`+0x58b`) → 옛 출력 해제 (`+0x593`) → `+0x5b5` 새 할당 (EBX B) → `+0x5e1 / +0x5e5` 출력 길이 칸 ← 그 값 → **`+0x5e9 JMP 0x180024f90` (= `+0x360`, 같은 `DeviceIoControl` 앞).**
- **횟수 상한이 없다.** 되돌아가는 `JMP` 가 무조건이고, 빠지는 길은 `+0x5cc JZ → +0x74d` (힙 할당 실패 → `CloseHandle`) 하나뿐. 그래서:
  - 엔진이 `0x80000005` 를 계속 주면 → **끝없이 돈다.**
  - 엔진이 `0x80000005` 를 주면서 **첫 4 B (Size) 를 안 쓰면** → 출력 버퍼에 남은 값 (힙에서 받은 0x20 B 의 쓰레기, 0 으로 채우는지는 `[공백]`) 을 크기로 믿고 다시 부른다. 그 값이 작으면 같은 답을 또 받는다 → 사실상 끝없는 고리 `[정적 + 추측]`.
  - `0x80000005` → LastError `0xea` 는 ntdll `RtlNtStatusToDosError` 표 값 `[추측: 표 안 읽음 — Q276]`. 이 값이 다르면 재시도 갈래 자체를 안 탄다.

**2-d. mountmgr.sys 쪽** `[정적] ms/disassemble/mountmgr/`

- 장치 · 링크: `DriverEntry +0x6a IoCreateDevice` (`\Device\MountPointManager`, 확장 0x178, 종류 0x12) · `+0x2df GlobalCreateSymbolicLink` (`\DosDevices\MountPointManager`) → 사용자 쪽 `\??\MountPointManager` `[정적: 문자열 원본 바이트 — 결과서]`. 함수표 `DRIVER_OBJECT +0x70` · `+0x80` = `MountMgrCreateClose`, `+0xe0` = `MountMgrDeviceControl`.
- 열기: `MountMgrCreateClose +0x10` CREATE 면 옵션 (`IrpSp+0x10`, `+0x3c`) 0번 비트가 서 있으면 `0xC0000103`, 아니면 0. `0x20060` 은 0번 비트 0 → 성공. 나머지 MajorFunction 은 늘 성공.
- 디스패치: `+0x2e` 코드 → `+0x66` 뮤텍스 → `+0x79 PsIsHostSilo` → `+0x23c CMP R13D,0x6d0008` → `+0x252 MountMgrQueryPoints`. 모르는 코드는 `0xC0000010` (decomp `"IOCTL UNKNOWN"`). mountmgr 안 문자열에 `IOCTL_MOUNTMGR_QUERY_POINTS` (= `0x6d0008`) · `…_ADMIN` (= `0x6d4008`) 는 있고 `IOCTL_MOUNTDEV_*` 는 **없다** → `0x4d0008` = `IOCTL_MOUNTDEV_QUERY_DEVICE_NAME` 은 `[추측: 공개 헤더]`.

| 차례 | `MountMgrQueryPoints` (`0x19ac0`) → `QueryPointsFromMemory` (`0x19fe0`) 검사 | 실패 status | 줄 |
|---|---|---|---|
| 1 | 입력 < `0x18` | `0xC000000D` | `+0x3b ~ +0x40` · `+0x1fe ~ +0x215` |
| 2 | 길이 0 인 칸은 오프셋도 0 으로 | — | `+0x4f ~ +0x77` |
| 3 | 오프셋 · 길이 홀수 | `0xC000000D` | `+0x7b ~ +0xc0` |
| 4 | 세 칸 (오프셋 + 길이) 최댓값 > 입력 길이 | `0xC000000D` | `+0xc6 ~ +0xe5` |
| 5 | 출력 < `0x20` | `0xC000000D` | `+0xeb / +0xf0` |
| 6 | 링크 · 장치 이름 길이 > `0xf000` | `0xC000000D` | `+0xfb` → `+0x224 / +0x228`, `+0x122 / +0x125` |
| 7 | 이름 복사 풀 할당 실패 | `0xC000009A` | `+0x153 ~ +0x15b` |
| 8 | 어느 칸이 채워졌나로 갈래 — 장치 이름 갈래가 kernelbase 몫 | — | `+0x108 ~ +0x1fa` |
| 9 | `IoGetDeviceObjectPointer` 실패 / 열린 개체 이름 칸 이상 | 그 status / `0xC0000034` | `QueryPointsFromMemory +0x79` |
| 10 | 장부에 그 장치 없음 | `0xC000000D` | `+0x2ce` |
| 11 | 출력 < Size | **`0x80000005`**, `Information = 8` (머리 8 B 만 씀) | `+0x39c ~ +0x3b2` · `+0x3f0` |
| 12 | 성공 | 0, `Information = Size` | `+0x39c ~ +0x3a5` |

- 출력 꼴: `+0` Size · `+4` 항목 수 · 항목 `0x18` B 씩 (`+0x4e8 ~ +0x51f`) · 그 뒤 이름 본문 **[고유ID][장치 이름][링크 이름들…]** (`+0x4a5` · `+0x4b7` · `+0x534`). `Size = 8 + 0x18 × 항목수 + 모든 길이 합`.
- mountmgr 는 볼륨 장치에 `0x4d0008` 을 **보내는 쪽** 이기도 하다 (`QueryPointsFromMemory +0x141` · `QueryDeviceName +0x14a` · `QueryDeviceNameInternal +0xb7`). 그 코드가 알려 주는 볼륨 장치 쪽 약속: 출력 첫 u16 = 이름 길이 (바이트), 이름은 `+2`, 작으면 `0x80000005` + 필요한 길이, 호출자는 `(길이 & ~1) + 2` 로 한 번 더 (결과서). 진짜 볼륨 드라이버 (volmgr · partmgr) 본문은 안 봄 `[공백 → Q276]`.
- 장부 칸 (장치 확장 `+0x10` 목록 머리 · 장치 항목 `+0x50` `DeviceName` · `+0x60` 고유ID · 링크 항목 `+0x10` 표지 · `+0x18` 길이 · `+0x20` 버퍼) 은 이름만 `[추측: decomp 칸 번호, 구조체 이름 확인 안 함]`.

**2-e. `NtCreateFile (MountPointManager)` 이 실패하면** `[정적]`

- `CreateFileInternal`: `+0x397 JS` → 조각 `FUN_18002635d` — 핸들 `-1` (`+0x0`) · `BaseSetLastNTError(status)` (`+0xb1`). 일부는 LastError 를 바꿔 씀: `0xC0000035` → `0x50`, `0xC00000BA` → 3 또는 5 (`+0xb6 ~ +0xe2`). 기능 플래그가 꺼진 길은 조각 `FUN_18019b82e +0xb0` 이 같은 일. `0xC0000022` 는 옵션 `0x20000` 을 지우고 한 번 더 (`+0x37a` → `+0x696` → 조각 `+0x22` · `+0x47`).
- `GetVolumeNameForRoot`: `+0x337 CMP RAX,-1` · `+0x33b JZ → +0x6eb` (예 / 아니오 검사 함수 한 번 — 반환 안 봄) → `+0x707` 출력 해제 → `+0x713 JMP → +0x6b5` → `+0x6c7` 입력 해제 → `+0x6d3 XOR EAX,EAX` → **FALSE**. 자기 LastError 는 안 세움.
- export `GetVolumeNameForVolumeMountPointW`: `+0xe2` → `+0xf0 BasepGetVolumeNameFromReparsePoint` — `CreateFileInternal(경로 X:\, 플래그 0x2200000)` (`+0xb0`) → `DeviceIoControl(0x900a8, 출력 0x1c)` → `NtFsControlFile` (`+0x100`). 둘 중 하나라도 실패면 0. 또 FALSE 면 `+0xfb ~ +0x104` 출력 첫 글자 0, FALSE.
- 에뮬레이터의 `path_prefix_unsupported` 는 status 가 아니라 **멈춤** (`nt_create_file.cpp:209 stop(...)`) — kernelbase 까지 값이 안 돌아간다.

### 3. shell 쪽 — FALSE 를 받으면 (질문 3, 부분) [정적: objdump · PDB 공개 이름 — `.asm` 없음, 결과서 근거 그대로]

- 부르는 자리 수 (직접 `call [IAT]` 만): windows.storage `GetVolumeNameForVolumeMountPointW` 13 · `GetVolumePathNameW` 8 · `GetVolumePathNamesForVolumeNameW` 4, shell32 10 · 5 · 2. 이름 목록은 결과서 부록.
- 직접 호출 사슬 하나 (windows.storage rva, call 위치): `CInvokeCreateProcessVerb::Execute` (`0xe1de0`) `+0x58` → `Launch` (`0xe102c`) `+0x10d` → `_TryCreateProcess` (`0x17d118`) `+0x3c` → `_PrepareAndCallCreateProcess` (`0x17d38c`) `+0xdf` → `_FixupWorkingDirectory` (`0x17e25c`) `+0x84` → `CMountPoint::GetMountPoint(int, CMountPoint**)` (`0xf0b5c`) `+0x6c` → `_GetMountPointForDriveLetter` (`0xf0bfc`) `+0x250` → `_InitLocalDrive` (`0x21a58c`) `+0x40` → **`GetVolumeNameForVolumeMountPointW`**.
- FALSE 의 길: `_InitLocalDrive +0x4c test / +0x4e je → +0xff ResultFromKnownLastError()` → HRESULT 반환 → `GetMountPoint(int) +0x71` 그대로 반환 → `_FixupWorkingDirectory +0x89 test / +0x8b js` → `+0xd0` 함수 끝 (void). **이 자리에선 그냥 지나간다.**
- `[공백]`: `ShellExecute*` → `Execute` 는 가상 함수라 직접 호출로 안 이어진다 (직접 호출 12 단계 · 1001 함수 안에 위 사슬 없음). 그래서 **"`ShellExecuteW` 가 이 사슬을 탄다" 는 정적으로 못 정함** — 이름과 모양이 맞아 후보 `[추측]`. shell32 `_GetVolumeNameForVolumeMountPoint` · `CDriveLetterCache::GetVolumePathNameAndCachedMountPoint` 도 실패를 HRESULT 로 올리는 같은 꼴, 그 위는 안 봄.

### 4. syscall 차례 — `GetVolumeNameForVolumeMountPointW(L"C:\\", 출력, 50)` 이 성공할 때 [정적]

| 차례 | syscall | 인자 | 갈래 | 줄 | 에뮬레이터 로그와 |
|---|---|---|---|---|---|
| 1 | `NtQueryInformationProcess` | -1, `0x17`, 0x24 B, NULL | `DriveMap` 비트 + `DriveType[n] == 3` 이면 3 으로 끝, 아니면 느린 길 | `GetDriveTypeW +0xe1` (← export `+0x7d`) | "둘" 의 첫째와 같음. 둘째는 이 길에 없음 `[추측 → Q275]` |
| 2 | `NtOpenFile` | `\??\C:`, `0x100080`, 3, `0x10`, 속성 `0x40` | 음수 → FALSE | `GetVolumeNameForRoot +0x126` | 같음 |
| 3 | `NtDeviceIoControlFile` | `0x4d0008`, 입력 0 · 0, 출력 0x208 | 음수 → FALSE (재시도 없음) | `+0x16c` → `DeviceIoControl +0x6c` | 같음 (여기서 터짐) |
| 4 | `NtClose` | 위 핸들 | — | `+0x178` | 같음 |
| 5 | `NtCreateFile` | `\??\MountPointManager`, `0x100080`, `0x80`, 3, 1, 옵션 (`0x20060` 은 로그 값 — 비트 계산 `[공백 → Q276]`) | `-1` → FALSE → fallback (9 ~ 11) | `+0x32f` → `CreateFileInternal +0x36c` | 같음 (여기서 멈춤) |
| 6 | `NtDeviceIoControlFile` | `0x6d0008`, 입력 이름길이 + 0x18, 출력 0x20 | `0x80000005` (+ LastError `0xea`) → 7; 그 밖 음수 → `0x57` FALSE | `+0x377` | 아직 안 나옴 |
| 7 | `NtDeviceIoControlFile` | 같은 입력, 출력 Size | 성공 → 훑기; 또 `0x80000005` → 8, … (**상한 없음**) | `+0x5e9 → +0x360` | — |
| 8 | `NtClose` (`CloseHandle`) | MPM 핸들 | — | `+0x38b` | — |
| 9 ~ 11 | (실패 fallback) `NtCreateFile(\??\C:\)` · `NtFsControlFile(0x900a8, 출력 0x1c)` · `NtClose` | — | — | `BasepGetVolumeNameFromReparsePoint +0xb0` · `+0x100` | — |

### 5. status → 반환값 (kernelbase) [정적]

| 갈래 | 반환 | LastError | 줄 |
|---|---|---|---|
| 경로 변환 실패 | FALSE | 3 | `+0x64` · `+0x618` |
| `NtOpenFile` 음수 | FALSE | `RtlNtStatusToDosError(status)` | `+0x136` · `+0x630` |
| `0x4d0008` 음수 | FALSE | `DeviceIoControl` 이 세운 값 | `+0x1a6 → +0x611` |
| 이름 복사본 힙 실패 | FALSE | 8 | `+0x1ea → +0x600` |
| MPM 열기 실패 | FALSE | `CreateFileInternal` 이 세운 값 | `+0x33b → +0x6eb` |
| `0x6d0008` 실패 + LastError `0xea` | 크기 다시 받아 재호출 (상한 없음) | — | `+0x567 ~ +0x5e9` |
| `0x6d0008` 실패, 그 밖 | FALSE | `0x57` | `+0x575` · `+0x673` |
| `0x60` 꼴 이름 없음 | FALSE | `0x57` | `+0x3bc → +0x673` |
| 호출자 버퍼 < 50 글자 | FALSE | `0xce` | `+0x4c3 → +0x766` |
| 성공 | TRUE, `\\?\Volume{GUID}\` + NUL | 그대로 | `+0x4c9 ~ +0x52a` |
| export fallback | `BasepGetVolumeNameFromReparsePoint` → 실패면 출력[0] = 0, FALSE | 그 함수가 세운 값 | `GetVolumeNameForVolumeMountPointW +0xe2 ~ +0x104` |
| export 인자 NULL / 길이 0 | FALSE | `0x57` | `+0x24 ~ +0x3f` · `+0x124 ~ +0x137` |
| export 경로 끝이 `\` 아님 | FALSE | `BaseSetLastNTError(0xC0000033)` | `+0x6e` · `+0x20f` · `+0x202` |

### 호출 관계

```
kernelbase!GetVolumeNameForVolumeMountPointW (export)                                          [정적]
 ├─ (1) +0x82   GetDriveTypeW
 │               └─ +0xe8  NtQueryInformationProcess(-1, 0x17, 0x24 B)                ── SYSCALL
 ├─ (2) +0xde   GetVolumeNameForRoot   (X:\ 일 때 +0x153, reparse 뒤 +0x172)
 │               ├─ +0x5d   RtlDosPathNameToNtPathName_U          C:\ → \??\C:\ → 끝 \ 떼기 · 대문자
 │               ├─ +0x12d  NtOpenFile(\??\C:, 0x100080, 3, 0x10)                     ── SYSCALL
 │               ├─ +0x171  DeviceIoControl(0x4d0008, 0, 0, 출력, 0x208)
 │               │           └─ +0x73  NtDeviceIoControlFile                           ── SYSCALL
 │               │          음수 → +0x1a6 → +0x611 FALSE (재시도 없음)
 │               │          +0x1ac u16 길이 (검사 없음) → +0x1dd RtlAllocateHeap(칸+2) → +0x209 memcpy(칸)
 │               ├─ +0x17f  NtClose                                                    ── SYSCALL
 │               ├─ +0x28b  memcpy  입력 = 0x18 B 머리 (+0x10 = 0x18 · +0x14 = 길이) + 이름
 │               ├─ +0x334  CreateFileInternal(\\.\MountPointManager)
 │               │           └─ +0x373  NtCreateFile(\??\MountPointManager, 0x100080, 0x80, 3, 1)   ── SYSCALL
 │               │          -1 → +0x6eb → FALSE
 │               ├─ +0x37c  DeviceIoControl(0x6d0008, 입력, 길이+0x18, 출력, 0x20)
 │               │           └─ +0x73  NtDeviceIoControlFile                           ── SYSCALL
 │               │               ⇒ mountmgr!MountMgrDeviceControl +0x257 MountMgrQueryPoints
 │               │                   └─ QueryPointsFromMemory +0x80 IoGetDeviceObjectPointer
 │               │                                            +0x187 IofCallDriver(0x4d0008)  (볼륨 장치에)
 │               │                                            출력 < Size → 머리 8 B · 0x80000005
 │               │          LastError 0xea → +0x587 Size → +0x5bc 새 버퍼 → +0x5e9 JMP +0x360   (상한 없음)
 │               ├─ +0x390  CloseHandle                                                ── SYSCALL NtClose
 │               └─        항목 표 → 길이 0x60 \??\Volume{…} → \\?\Volume{…}\ → +0x52a TRUE
 └─ (3) +0xf5   BasepGetVolumeNameFromReparsePoint   ((2) 가 FALSE 일 때)
                 ├─ +0xb5   CreateFileInternal(X:\, 0x2200000) → NtCreateFile               ── SYSCALL
                 └─ +0x105  DeviceIoControl(0x900a8, 출력 0x1c) → NtFsControlFile          ── SYSCALL

windows.storage (objdump · 결과서만)
CInvokeCreateProcessVerb::Execute ⇢? (ShellExecute* 에서 — 가상 함수, [공백])
 └─ +0x58 Launch → +0x10d _TryCreateProcess → +0x3c _PrepareAndCallCreateProcess → +0xdf _FixupWorkingDirectory
     └─ +0x84 CMountPoint::GetMountPoint(int) → +0x6c _GetMountPointForDriveLetter → +0x250 _InitLocalDrive
         └─ +0x40 GetVolumeNameForVolumeMountPointW   FALSE → HRESULT → _FixupWorkingDirectory +0x8b js → 끝 (지나감)
```

- (kernelbase 나무 오프셋은 ret 자리, windows.storage 줄은 결과서의 call 자리. `⇒` 는 syscall 을 건너뜀, `⇢?` 는 못 이은 간접 호출. 콜스택으로 본 적 없음.)

## 확인된 것

- `[정적]` `0x4d0008` 을 즉시값으로 싣는 kernelbase 줄은 `GetVolumeNameForRoot +0x162` 하나. 같은 함수에서 `NtOpenFile(\??\C:, 0x100080, 3, 0x10)` (`+0x126`) 과 `NtClose` (`+0x178`) 가 나온다.
- `[정적]` 출력 `+0` u16 = 이름 길이 (바이트), 이름은 `+2`. 그 길이를 상한 검사 없이 힙 크기 · `memcpy` 길이로 쓴다 (`+0x1ac ~ +0x204`).
- `[정적]` `0x4d0008` 이 음수면 (`0x80000005` 포함) 출력을 안 읽고 FALSE. 재시도 없음.
- `[정적]` 그 뒤 `\\.\MountPointManager` 를 `CreateFileInternal` 로 열고 (`NtCreateFile` 은 `CreateFileInternal +0x36c`) `0x6d0008` 을 보낸다. 입력 머리 `+0x10 = 0x18` · `+0x14 = 길이` + 이름.
- `[정적]` `0x6d0008` 실패 + LastError `0xea` 면 출력 첫 4 B 를 새 크기로 **상한 없이** 다시 부른다 (`+0x5e9 JMP → +0x360`). 빠지는 길은 힙 할당 실패뿐.
- `[정적]` 출력 항목 표에서 길이 `0x60` · `\??\Volume{…}` 꼴 하나를 골라 `\\?\Volume{GUID}\` 로 돌려준다.
- `[정적]` mountmgr: 장치 `\Device\MountPointManager` + 링크 `\DosDevices\MountPointManager`, 열기는 옵션 0번 비트만 거름, `0x6d0008` → `MountMgrQueryPoints` → `QueryPointsFromMemory` (볼륨에 `0x4d0008` 을 다시 보내 장부와 견줌, 출력 작으면 머리 8 B + `0x80000005`).
- `[정적]` `NtQueryInformationProcess(0x17)` 은 `GetDriveTypeW +0xe1` — `GetVolumeNameForRoot` 가 아니다.
- `[정적: objdump]` windows.storage `_FixupWorkingDirectory` 는 그 아래에서 올라온 실패 HRESULT 를 무시하고 끝난다.

## 정정 (요청서 · 원문의 짐작이 틀린 곳)

1. **"`NtQueryInformationProcess(0x17)` 둘 · `NtOpenFile` · `0x4d0008` 이 같은 함수에서"** → `NtOpenFile` 과 `0x4d0008` 만 같은 함수 (`GetVolumeNameForRoot`). `NtQueryInformationProcess(0x17)` 은 export 호출자가 먼저 부르는 **`GetDriveTypeW +0xe1`** 이고, 이 export 길에선 **한 번**이다. 로그의 둘째는 다른 호출자 `[추측 → Q275]`.
2. **요청서의 PE_emulator 줄 번호가 밀렸다**: `nt_device_io_control_file.cpp:617 ~ :654` 는 지금 파일에서 다른 장치 (`fake()` · `kKsecConfigRequest` 갈래 — 결과서는 `NUL` · `CNG` · `KsecDD`) 이고, **볼륨 핸들 갈래는 `:675 ~ :742`** 다 (`:675 is_volume` · `:726 ~ :727` `fake()` / `device_control_code_unverified`). 결과서 + flow-writer 가 줄 번호만 다시 확인.
3. **"`0x80000005` 면 더 큰 버퍼로 다시 부르나"** → `0x4d0008` 쪽은 **안 부른다** (FALSE). 다시 부르는 것은 `0x6d0008` 쪽이고, 그쪽은 **상한이 없다**.
4. **"`path_prefix_unsupported` 가 kernelbase 에 돌아가는 값"** → status 가 아니라 엔진 멈춤 (`nt_create_file.cpp:209 stop`). kernelbase 는 아무 값도 못 받는다.
5. **코드 이름 `IOCTL_MOUNTDEV_QUERY_DEVICE_NAME`** → mountmgr 안에 그 문자열이 없다. 이름은 `[추측: 공개 헤더]` 그대로, 바이트 근거는 코드 줄.

- 고쳐 읽기 (앞 run): 없음. run91 의 `DeviceIoControl` 갈래 (`+0x2b` · `+0x6c` · `+0x87` · `+0x154` · `+0x1de`) 는 이번 줄과 같다.

## PE_emulator 쪽 (결과서 "지금 우리 맞나 · 틀리나" — flow-writer 는 `../PE_emulator` 를 줄 번호만 훑었고 내용은 안 읽음, 고칠지는 사용자 결정)

| 지금 에뮬레이터 | 판단 | 근거 · 할 것 |
|---|---|---|
| `0x4d0008` (볼륨 갈래 `:675 ~ :742`) — 목록에 없어 `fake()` 또는 `device_control_code_unverified` (`:726 ~ :727`) | **틀림 (빠짐)** | "성공인데 출력 안 씀" 은 `fake()` 가 status 0 만 준 것으로 보임 `[추측: fake() 본문 일부만 — 결과서]`. 부르는 쪽이 길이 칸을 그대로 믿으니 (`+0x1f0 → +0x204`) **성공을 주려면 길이 칸 + 이름을 반드시 써야 한다** |
| `NtCreateFile(\??\MountPointManager)` → `stop("path_prefix_unsupported")` (`nt_create_file.cpp:209`) | **틀림 (빠짐)** | 멈춤이라 kernelbase 로 값이 안 감. 장치를 지원하거나 (열기 성공) 최소한 음수 status 를 |
| `NtQueryInformationProcess(0x17)` | **맞음** | 고정 디스크 답 → `GetDriveTypeW` 3 → `NtOpenFile` 로 (로그 순서 = 코드 순서) |
| `NtOpenFile(\??\C:, 0x100080, 3, 0x10)` | **맞음** | `+0x110` · `+0x104` · `+0xe5` |

**엔진이 돌려줄 최소 바이트 (C: = `\Device\HarddiskVolume3`)** — 결과서 그대로:

- `0x4d0008`: status 0, `Information = 0x30`, 출력 `2e 00` + `\Device\HarddiskVolume3` UTF-16LE 46 B (널 없음). `Information` 은 이 호출자가 안 읽지만 `0x30` 이 약속에 맞음 `[추측]`.
- `NtCreateFile(\??\MountPointManager)`: status 0 (옵션 0번 비트가 0 이면 진짜도 성공).
- `0x6d0008`:
  - 입력 검사 흉내: 입력 ≥ `0x18`, 출력 ≥ `0x20`, `+0x10 == 0x18`, 이름 = `\Device\HarddiskVolume3`.
  - **첫 호출 (출력 0x20)**: status `0x80000005`, `+0` = Size, `+4` = 항목 수, `Information = 8`. 최소 Size = `8 + 0x18 + 0x60 = 0x80`, 항목 1 `[정적: QueryPointsFromMemory +0x39c ~ +0x3f0]`.
  - **둘째 호출 (출력 ≥ Size)**: status 0, `Information = Size`. `+0` `0x80` · `+4` 1 · `+8` 링크 이름 오프셋 `0x20` · `+0xc` 길이 `0x60` · `+0x10 ~ +0x1f` 0 · `+0x20` `\??\Volume{GUID}` UTF-16LE `0x60` B (GUID 는 `MountedDevices` 수집값).
  - 진짜처럼 하려면 링크마다 항목 (`\DosDevices\C:` `0x1c` B 등), 본문 순서 [고유ID][장치 이름][링크들]. 길이 `0x60` 짜리가 하나라도 있으면 kernelbase 는 그것을 고른다.
- **금지 꼴 (끝없는 고리)**: 첫 호출에 `0x80000005` 를 주면서 `+0` Size 를 안 쓰거나, 둘째 호출에도 계속 `0x80000005` 를 주면 `GetVolumeNameForRoot` 가 `+0x5e9` 에서 **끝없이 돈다** (상한 없음, 빠지는 길은 힙 실패뿐).
- **"정직한 실패" 로 줄 status**:
  - `0x4d0008` 을 못 채우면: 음수 아무거나 → 출력 안 읽고 FALSE. 권장 `0xC0000010` (mountmgr 가 모르는 IOCTL 에 주는 값) `[정적: mountmgr — 볼륨 드라이버가 같은 값인지는 공백]`.
  - MPM 을 못 열면: 음수 (권장 `0xC0000034`) → FALSE. **`0xC0000022` 는 피할 것** (옵션 바꿔 `NtCreateFile` 한 번 더).
  - 둘 다 실패해도 export 는 9 ~ 11 (`NtCreateFile(\??\C:\)` · `NtFsControlFile(0x900a8)`) 을 더 부른다. 정직한 답: 열기 성공 + `0x900a8` 은 reparse 아님 (공개 값 `0xC0000275` `[추측]`) → 출력[0] = 0, FALSE.
  - **status 0 인데 출력을 안 쓰는 대답이 최악** (길이 칸이 쓰레기 → `memcpy` 최대 64 KB).
- shell 쪽: 위 사슬 기준으론 FALSE 를 받아도 `_FixupWorkingDirectory` 가 지나간다 → "정직한 실패" 로도 그 자리는 열린다. 다만 `ShellExecuteW` 가 그 사슬을 타는지는 `[공백 → Q275]`.

## 아직 확인 필요

동적 (결과서 "동적으로 확인해야 할 것" — 대기열 **Q275 (동적: 결과서 동적 — GetVolumeNameForRoot+0x171 출력·Information · +0x37c 첫 0x6d0008 · NtQueryInformationProcess(0x17) 둘째 호출자 · ShellExecute→CInvokeCreateProcessVerb::Execute 불리나)**. test.exe 는 이 길을 안 탐 — `ShellExecuteW` 를 부르는 프로세스 · 그 조건은 사용자 결정):

1. `KERNELBASE!GetVolumeNameForRoot+0x171` (`DeviceIoControl` 돌아온 자리): `eax`, `db @rbp-0x40 L0x30` — 진짜 `\Device\HarddiskVolume3` 의 길이 칸 · IOSB.Information 이 `0x30` 인가.
2. `+0x37c` (첫째 · 둘째 둘 다): 첫 `0x6d0008` 이 정말 `0x80000005` 인가, Size · 항목 수 · 항목 순서 · 링크 이름들. `+0x56f` 에서 LastError `0xea` 인가. 둘째 뒤 `db @rsi L<Size>`.
3. `RtlNtStatusToDosError(0x80000005) = 0xea` 인지.
4. 로그의 `NtQueryInformationProcess(0x17)` 둘째의 호출자 — `ntdll!NtQueryInformationProcess` + 반환 주소를 shell32 / windows.storage PDB 공개 이름으로.
5. `ShellExecute*` → `windows_storage!CInvokeCreateProcessVerb::Execute` 가 불리나, `_FixupWorkingDirectory +0x84` 가 `GetMountPoint` 를 부르나 (`_InitLocalDrive+0x47` 에서 `eax`).

정적 (결과서 "다음 요청 후보" 1 ~ 5 + 옵션 계산 — 대기열 **Q276 (정적: 결과서 다음 요청 후보 1~5 — windows.storage/shell32 analyze·지도 · BasepGetVolumeNameFromReparsePoint · RtlDosPathNameToNtPathName_U/RtlNtStatusToDosError 표 · partmgr/volmgr 0x4d0008 · GetVolumePathNameInternalW 고리 · NtCreateFile 옵션 0x20060 계산)**):

1. windows.storage · shell32 Ghidra analyze + 지도 (뿌리 `ShellExecuteExW` · `CInvokeCreateProcessVerb::Execute`) — 간접 호출 이음. PDB 는 받아 둠. 하나씩.
2. `BasepGetVolumeNameFromReparsePoint` 실패 길 전체 — `NtCreateFile(\??\C:\)` 옵션, `0x900a8` 뒤 갈래, reparse 있을 때 성공 길.
3. ntdll `RtlDosPathNameToNtPathName_U` (`C:\` → `\??\C:\`) · `RtlNtStatusToDosError` 표 (`0x80000005` → ?) — sast/138 과 겹치니 그 뒤.
4. partmgr.sys / volmgr.sys 의 `0x4d0008` 처리 — 진짜 볼륨 장치 출력 바이트.
5. `GetVolumePathNameInternalW` 의 경로 잘라 가며 다시 부르는 고리 (`+0x2c4 ~ +0x4f4`).
6. (보탬) `CreateFileInternal` 이 옵션 `0x20060` 을 만드는 비트 계산 (`+0x251 ~ +0x279` 근처) — 결과서는 로그 값을 믿음 `[공백]`.

- 그 밖 `[공백]` / `[추측]`: 출력 버퍼 (`+0x2c2` 힙) 를 0 으로 채우는지 (Size 안 쓴 답의 결과가 갈림), 원문 예외 자리 `RtlCopyMemory` 가 kernelbase `memcpy` (`0x196e77`) 의 몸인지, `+0x6eb` 검사 함수의 뜻.

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- 결과서 2-a "`+0x50d MOV dword [R15+0x60], 0x5c`" — `.asm` 은 **`MOV dword ptr [R15+0x60],ECX`** 이고 `ECX = 0x5c` 는 `+0x4cd MOV ECX,0x5c` 에서 온다. 결과 (`\` + NUL) 는 같음.
- 결과서 2-a "`+0x5e5` 출력 길이 `[RSP+0x28]` ← EBX" — EBX 는 `+0x58b` 에서 `[RSP+0x50]` 에 두고 `+0x5e1 MOV EAX,[RSP+0x50]` · `+0x5e5 MOV [RSP+0x28],EAX` 로 간다 (`[RSP+0x50]` 은 `lpBytesReturned` 자리이기도 — 결과서 1-c "덮어씀" 과 같은 칸).
- 결과서 2-c "`+0x33b JZ → +0x6eb` → 입력 · 출력 힙 해제" — `+0x6eb` 는 해제가 아니라 **먼저 함수 하나를 부른다** (`0x180128bf4`, `_calls.tsv` 에 `alias:IsBrokeredGetFileAttributesExWPresent`). `pdb_aliases.py --lookup KernelBase.dll 0x128bf4` = 같은 주소 이름 11개 (`IsBrokered…Present` 무리). `objdump` 로 보면 전역 칸 `0x1803a9d80` 에 캐시한 예 / 아니오 (`AL`) 를 돌려주는 검사다. 반환은 이 자리에서 안 쓰이고 (`+0x6f0` 뒤 `AL` 안 봄), 그다음 `+0x707` 출력 해제 → `+0x713 JMP → +0x6b5` → `+0x6c7` 입력 해제 → `+0x6d3` FALSE. 같은 호출이 `BasepGetVolumeNameFromReparsePoint +0x26c` · `CreateFileW +0xcb` · `DeleteVolumeMountPointW +0x43b` 의 실패 갈래에도 있다. 결과에는 영향 없음.
- 결과서 2-c "`CreateFileInternal` `+0x395 JS`" — `+0x395` 는 `TEST EBX,EBX`, **`JS` 는 `+0x397`** (→ `0x18002635d`). 그 앞 `+0x388` 에 기능 검사 (`Feature_Servicing_CD_PathRedirectionAPI_2503…`) 와 `+0x38f JZ` (→ 조각 `FUN_18019b82e` 길) 가 있다.
- 결과서 1-b `GetDriveTypeW` 부르는 자리 셋 — `_calls.tsv` 엔 넷째 `GetVolumePathNameInternalW` 조각 `FUN_1800272d2 +0x2f / +0x34` 가 더 있다.
- 결과서 2-c "`+0x4bf JC`" — `+0x4bf` 는 `CMP RCX,0x64`, **`JC` 는 `+0x4c3`** (→ `+0x766`).
- 결과서 "`+0x1c4` 로 복사" (`QueryPointsFromMemory`) — 그 줄은 `RtlDuplicateUnicodeString` 이다 (사본 만들기). 뜻은 같음.
- 그 밖 결과서 오프셋 · 점프 목적지 · 즉시값은 `.asm` 과 **일치** (위 "방법" 의 줄 전부). shell32 · windows.storage 줄은 `.asm` 이 없어 대조 못 함.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- `pdb_aliases.py` 의 모듈 이름은 **`ms/sym/` 파일 이름 대소문자 그대로**다: `kernelbase.dll` 로 주면 "PDB 없음" 이고 `KernelBase.dll` 이어야 찾는다. CLAUDE.md 예 (`ntdll.dll`) 는 마침 소문자라 드러나지 않았다 — 기록만 (보고에 제안).

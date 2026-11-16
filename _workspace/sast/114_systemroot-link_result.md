# 114 systemroot-link — 정적 분석 결과

- 요청서: `_workspace/sast/114_systemroot-link.md` (Q95, 정적만)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음 — `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 새로 뽑은 것 (이미 있는 `.asm`·`.c` 는 다시 안 뽑음. 요청서가 정한 depth 는 지켰고, 길을 찾느라 `^이름$` 로 한 개씩만 더 뽑음. ntdll 은 안 건드림):
  - `^CreateSystemRootLink$` depth 1 → 8개 (본체 + callee 7: `NtCreateDirectoryObject`·`NtCreateSymbolicLinkObject`·`NtClose`·`RtlStringCbPrintfW`·`RtlInitUnicodeString`·`KeBugCheckEx`·`__security_check_cookie`. callee 몇 개는 전에 뽑은 것과 같은 내용으로 다시 쓰였을 수 있음)
  - `^IopReassignSystemRoot$` depth 0 (요청서)
  - 길 찾기용 depth 0 한 개씩: `IopCreateArcNames`·`IopCreateArcName` · `IopInitializeBootDrivers` · `SeInitSystem` · `SepInitSystemDacls` · `ObCreateSymbolicLink`
  - `refs`: `CreateSystemRootLink` · `IopReassignSystemRoot` · `IopCreateArcNames` · `SePublicDefaultUnrestrictedSd` · `SepInitSystemDacls` · `SepVariableInitialization` · `SeInitSystem` · `IoInitSystem_deviceNameBuffer` · `PspSystemRootSymlinkName` · `PspSiloInitializeSystemRootSymlink` · 글 상수 3곳 (`0x3f120`·`0xaf6f40`·`0xb52470`)
  - 위치: `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/` (+ `_refs/`)
- 버전 확인: 일치. `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 에 그대로 있음 (알려진 사실 표 `+0xc36`·`+0x807` 과 같음). 커널 PDB GUID 도 기준과 같음.
- 스크립트 문제: 없음.

## 한 줄 요약

- `\SystemRoot` 는 `Phase1InitializationDiscard +0xc31` 이 부르는 `CreateSystemRootLink` 가 만든다. 부르는 곳은 이 한 곳뿐이다.
- 이 함수는 한 번에 **디렉터리 둘 + 심볼릭 링크 넷**을 만든다. 하나라도 실패하면 버그체크다 (코드 `0x64`).
- `\SystemRoot` 의 대상 글은 `\Device\BootDevice` + 로더 블록 `NtBootPathName` (끝 한 글자 뺌). **`\SystemRoot` 링크 자체는 나중에 안 바뀐다.** 나중에 바뀌는 건 `\Device\BootDevice` 링크다 (`IopReassignSystemRoot`).
- 코드 순서: `CreateSystemRootLink` (Discard `+0xc31`) → `IopCreateArcNames` (`IopInitializeBootDrivers +0x7f8`) → `PsLocateSystemDlls` (`IoInitSystemPreDrivers +0x10f5`) → `IopReassignSystemRoot` (`+0x1139`).

## 질문별 답 [정적]

### 1. 만드는 곳 · 대상 글 · 속성 · SD

- 답 (만드는 곳): `CreateSystemRootLink` 가 `NtCreateSymbolicLinkObject` 를 **직접 불러** 만든다. 유저에서 부르는 syscall 이 아니라 커널 안 직접 호출이다. 이 함수는 다시 `ObCreateSymbolicLink` 를 부른다. `ObCreateSymbolicLink` 는 안에서 `ObCreateObjectEx` (`+0x89`, ret `+0x8e`) 와 `ObInsertObjectEx` (`+0x191`, ret `+0x196`) 를 부른다.
  - `\SystemRoot` 를 만드는 줄: `CreateSystemRootLink +0x2b4` (ret `+0x2b9`) `NtCreateSymbolicLinkObject`.
  - 그 앞: `+0x218` (ret `+0x21d`) `RtlInitUnicodeString` 이 링크 이름 `\SystemRoot` 를 만들고, `+0x266` (ret `+0x26b`) `RtlStringCbPrintfW` 가 대상 글을 짓는다. `+0x291` (ret `+0x296`) 이 대상 `UNICODE_STRING` 을 만들고, `+0x2a0` 에서 `Length` 를 2 줄인다.
  - `NtCreateSymbolicLinkObject` 는 `+0x196` (ret `+0x19b`) 에서 `ObCreateSymbolicLink` 를 부른다 (`NtCreateSymbolicLinkObject.asm`).
- 답 (대상 글): 글은 `"%s%S"` 모양이다 (상수 `0x140c76a80`, 파일 바이트로 확인).
  - `%s` (wide) = `\Device\BootDevice` (`+0x22a` `LEA R9,[0x140c76a10]`).
  - `%S` (ANSI) = `LoaderBlock +0xc8 NtBootPathName` (`+0x248` `MOV RAX,[RBX+0xc8]`, `+0x252` 스택 5번째 인자).
  - 완성 글 길이에서 2바이트 (끝 한 글자) 를 뺀다 (`+0x296 MOV EAX,0xfffe`, `+0x2a0 ADD word ptr [RSP+0x68],AX`). 그래서 `NtBootPathName` 이 `\Windows\` 처럼 `\` 로 끝난다는 걸 믿고 짠 코드다. 결과는 `\Device\BootDevice\Windows` 꼴이다 `[추측: 실제 NtBootPathName 값은 정적으로 모름 → 동적 1]`.
  - `\SystemRoot` 가 가리키는 **디스크 이름 자체는 여기서 안 정한다.** `\Device\BootDevice` 링크가 정한다.
- `\Device\BootDevice` 링크 (같은 함수가 먼저 만듦):
  - 대상 = `\ArcName\` + `LoaderBlock +0xb8 ArcBootDeviceName` (`"\ArcName\%S"` 상수 `0x140c76a40`).
  - 줄: `+0x155` (ret `+0x15a`) 이름 만들기 → `+0x15a MOV R9,[RBX+0xb8]` → `+0x19d` (ret `+0x1a2`) `RtlStringCbPrintfW` → `+0x1c8` 대상 문자열 → **`+0x1e1` (ret `+0x1e6`) `NtCreateSymbolicLinkObject`**.
- 로더 블록은 어디서 오나: `Phase1InitializationDiscard +0x38 MOV RSI,RCX` 로 인자를 받고, `+0xc2e MOV RCX,RSI` 로 `CreateSystemRootLink` 에 그대로 넘긴다 (`+0xc31`). 읽는 칸 `+0xb8`·`+0xc8`·`+0x150` 은 `_LOADER_PARAMETER_BLOCK` PDB 의 `ArcBootDeviceName` · `NtBootPathName` · `ArcOSDataDeviceName` 와 같다 (`pdb_struct.py`).
- `IoAssignArcName` 류 호출은 이 함수에 **없다.** 이 함수는 `\ArcName` **디렉터리만** 만든다. 안쪽 `\ArcName\multi(0)disk(0)…` 링크는 나중에 `IopCreateArcNames` 가 채운다 (질문 2 와 3 의 "같이 만드는 것" 참고).
- 답 (링크 속성): 만든 개체 여섯 모두 `OBJECT_ATTRIBUTES` 의 `Attributes = 0x50` = `OBJ_PERMANENT (0x10) | OBJ_CASE_INSENSITIVE (0x40)`. 영구 + 대소문자 무시다. `OBJ_KERNEL_HANDLE (0x200)` 는 **없다.**
  - 근거: 디렉터리 첫 번째 `+0x42 MOV [RSP+0x50],0x50`, 이후 `+0xa8 LEA R14D,[RDI+0x50]` 를 `+0x114`·`+0x193`·`+0x25c`·`+0x31a`·`+0x3e3` 에서 `[RSP+0x50]` 에 씀. `Length = 0x30` (`ESI`), `RootDirectory = 0`, `SecurityQualityOfService = 0`.
  - 접근 권한: 디렉터리 `0xf000f` (`DIRECTORY_ALL_ACCESS` 모양), 링크 `0xf0001` (`STANDARD_RIGHTS_REQUIRED | SYMBOLIC_LINK_QUERY`).
  - 만든 뒤 핸들은 바로 `NtClose` 로 닫는다. 영구 개체라 이름은 남는다.
  - 대소문자 무시는 만들 때 넘긴 속성값이다. 개체에 남는 표시인지는 안 읽었다. 영구 표시는 `_OBJECT_HEADER +0x1b Flags` 비트 4 `PermanentObject` 다 (PDB).
- 답 (SD): 여섯 개체 전부 `SecurityDescriptor = SePublicDefaultUnrestrictedSd` (전역 포인터, `+0x8a`·`+0xf9`·`+0x171`·`+0x231`·`+0x300`·`+0x3cd` 에서 읽음).
  - 쓰는 곳: `SepInitSystemDacls +0x683` (`MOV [SePublicDefaultUnrestrictedSd],RBX`). 이 함수는 `SepVariableInitialization +0x1923` → `SepInitializationPhase0 +0x6` → `SeInitSystem +0x33` 길로 닿고, `SeInitSystem` 은 `InitBootProcessor +0xb94` 가 위상 0 에 부른다 (`refs`). 곧 Discard 의 `+0xc31` 보다 훨씬 전이다.
  - Discard 쪽 `SeInitSystem` (`+0xbdf`, ret `+0xbe4`) 은 `InitializationPhase = 1` (`+0xcb` 에서 씀) 이라 `SepInitializationPhase1` 갈래다 (`SeInitSystem +0x10 CMP EAX,1`, `+0x2c` call).
  - 안 내용 (`SepInitSystemDacls.c` 57·63·70·76 줄): DACL 만 (소유자·그룹·SACL 없음), 허용 규칙 넷 = Everyone `GENERIC_EXECUTE (0x20000000)` · LocalSystem `GENERIC_ALL (0x10000000)` · Administrators `GENERIC_ALL` · Restricted `GENERIC_EXECUTE`. (읽기·실행만 모두에게, 쓰기는 SYSTEM·관리자만 하는 모양.)
  - SD 가 실제로 개체에 붙는 줄 (`ObCreateObjectEx`/`ObInsertObjectEx` 안) 은 안 읽었다 `[공백, 작음]`.
- 실패하면: 단계 1 ~ 9 마다 `KeBugCheckEx(0x64, status, 단계 번호, 0)` (`LEA ECX,[…+0x64]`, 단계 번호 `R8D`). 호출 줄 `+0xc7` · `+0x13a` · `+0x1b9` · `+0x1fd` · `+0x282` · `+0x2d0` · `+0x355` · `+0x399` · `+0x422`. 정상 끝은 `+0x432 XOR EAX,EAX` 로 0 을 돌려준다 (RET `+0x45f`).
  - 그래서 호출자 `Phase1InitializationDiscard +0xc36 TEST EAX,EAX` 뒤의 실패 갈래 (`+0xc4c KeBugCheckEx`, 첫 인자 `0x64`) 는 이 함수가 0 만 돌려주므로 닿지 않는 코드다 `[추측: 반환값이 0 말고 있는 줄 못 찾음]`.
- 지금 우리 (PE_emulator): **맞음** (모양). `ext/syscall/syscall_layer_cpp/src/namespace.cpp:31` 에 `\SystemRoot → \Device\BootDevice\Windows` 가 있다. 영구·SD 칸은 이름 공간 `Entry` (링크 여부·이름·대상) 에 없다 → 확인 필요.

### 2. 시점 · `IopReassignSystemRoot`

- 답 (+0xc31 이 맞나): **맞다.** `Phase1InitializationDiscard +0xc31` 이 call 명령 자리, ret 는 **`+0xc36`**. 인자 `MOV RCX,RSI` 는 바로 앞 `+0xc2e`.
  - **부르는 곳 전수**: `refs` 결과 코드 호출 1줄 (`Phase1InitializationDiscard+0xc31`) + `.pdata` 데이터 참조 1줄 (함수 밖). 함수 포인터 표 속 값 **0곳** (`ms/disassemble/ntoskrnl/_refs/CreateSystemRootLink.tsv`).
  - 같은 `.asm` 앞뒤: `+0xbdf SeInitSystem` → `+0xbfd AlpcpInitSystem` → `+0xc24 PsInitSystem` (ret `+0xc29`) → `+0xc29 ExInitLicenseCallback` (ret `+0xc2e`) → **`+0xc31 CreateSystemRootLink`** → `+0xc54 KeQueryPerformanceCounter` → `+0xc65 MmInitSystem` → `+0xc7a CcInitializeCacheManager` → `+0xc91 CmInitSystem1`.
  - 앞서 `ObInitSystem` (`+0xa96`) 와 `IoCreateObjectTypes` (`+0xadb`) 가 끝났다. 그래서 링크 개체 종류가 있다 `[추측: 개체 종류 이름·생성 줄은 안 봄]`.
- 답 (`PsLocateSystemDlls` 보다 앞인가): **앞이다.**
  - 순서 근거: `Phase1Initialization +0x1e` (ret `+0x23`) `Phase1InitializationDiscard` 가 먼저 **돌아온 뒤** `+0x36` (ret `+0x3b`) `IoInitSystem` 을 부른다. `IoInitSystem +0x12` (ret `+0x17`) → `IoInitSystemPreDrivers +0x10f5` (ret `+0x10fa`) `PsLocateSystemDlls` (`refs` 결과 `IoInitSystemPreDrivers+0x10f5` 한 곳, sast/42 와 같음).
  - 그래서 `\SystemRoot` 링크는 `\SystemRoot\System32\ntdll.dll` 을 여는 `PsLocateSystemDlls → PspLocateSystemDll` (`ZwOpenFile`) 보다 항상 먼저 있다 `[정적, 호출 순서]`. (`\SystemRoot\System32\ntdll.dll` 글은 `PAGE` 구역 rva `0xb53370` 에 있음 — 파일 바이트로 확인.)
- 답 (`IopReassignSystemRoot` `+0x1139` 가 `\SystemRoot` 를 바꾸나): **`\SystemRoot` 는 안 바꾼다.** 바꾸는 건 **`\Device\BootDevice`** 링크다.
  - 호출: `IoInitSystemPreDrivers +0x1139` (ret `+0x113e`). 인자 = `RCX` 로더 블록, `RDX` = 이 함수의 지역 `ANSI_STRING` (`+0x1132 LEA RDX,[RBP-0x50]`, 버퍼는 전역 `IoInitSystem_deviceNameBuffer` 256바이트, 시작 때 `+0x4b`·`+0x56` 에서 연결). 이 버퍼를 읽는 다른 코드는 `refs` 에 없다 (`IoInitSystemPreDrivers` 안 두 줄뿐).
  - 하는 일 (`IopReassignSystemRoot.asm`):
    1. `+0x28 MOV R9,[RCX+0xb8]` → `+0x61` (ret `+0x66`) `RtlStringCchPrintfW`: `\ArcName\<ArcBootDeviceName>` 글 (`+0x7c` 이후 `MaximumLength = 0x200`).
    2. 고리 (`+0x86` 부터, 속성 `0x240` = 대소문자 무시 + 커널 핸들): `+0xbd` (ret `+0xc2`) `NtOpenSymbolicLinkObject`. 성공하면 `+0xdf` (ret `+0xe4`) `NtQuerySymbolicLinkObject` 로 대상 글을 같은 칸에 읽고, `+0xed` (ret `+0xf2`) 핸들을 닫고, `+0xf4 JNS` 로 `+0x86` 에 돌아가 그 대상 글을 또 심볼릭 링크로 열어 본다.
    3. 열기가 `0xc0000024` (`STATUS_OBJECT_TYPE_MISMATCH`, `+0xc2 CMP`, `+0xc7 JZ` → `+0x11b`) 로 실패하면 = **링크가 아닌 개체 (장치) 에 닿았다**는 뜻이다. 이때 `+0x126` (ret `+0x12b`) `RtlUnicodeStringToAnsiString` 으로 그 이름을 호출자 `ANSI_STRING` 에 복사한다.
    4. `+0x13b` `\Device\BootDevice` 열기 (`+0x177`, ret `+0x17c`) → **`+0x189` (ret `+0x18e`) `NtMakeTemporaryObject`** → `+0x195` (ret `+0x19a`) 핸들 닫기 (옛 링크 삭제) → **`+0x1d6` (ret `+0x1db`) `NtCreateSymbolicLinkObject`** 로 같은 이름을 **새로 만들어** 대상 = 4번에서 찾은 장치 이름 (`+0x1af LEA R9,[RSP+0x28]`) → `+0x1e2` (ret `+0x1e7`) 핸들 닫기 → `+0x1e7 MOV AL,1` (성공).
    5. 새 링크 속성 `0x250` (`+0x1be`) = 영구 + 대소문자 무시 + 커널 핸들. SD 칸은 0 (`+0x1d0 MOVDQU [RSP+0x58],XMM0`), 곧 **`SePublicDefaultUnrestrictedSd` 를 안 준다.**
  - **결과**: `\SystemRoot` → `\Device\BootDevice\Windows` 는 그대로, `\Device\BootDevice` 의 대상이 `\ArcName\…` 에서 **`\Device\<실제 장치 이름>`** 으로 바뀐다. `\SystemRoot` 를 열면 두 링크를 차례로 따라 간다.
  - 바뀌는 조건: 호출 앞에 **조건이 없다** (`+0x10f5` 뒤 `+0x1105 JNS` 로 `PsLocateSystemDlls` 가 성공했을 때만 오는 직선). 갈림은 이름 사슬뿐이다. VHD·BitLocker 를 따로 보는 줄은 이 함수에 **없다**. (VHD 부팅 확인은 `IopCreateArcName +0x489 VhdVerifyBootDisk` 쪽이고 이 함수가 아님.)
  - 실패하면 (`NtOpenSymbolicLinkObject` 가 다른 음수, 변환 실패, `\Device\BootDevice` 열기 실패) `AL = 0` (`+0xf6 XOR AL,AL`) → `IoInitSystemPreDrivers +0x1140 JNZ` 안 뜀 → `+0x114c IopInitFailCode = 9` → `+0x1156 JMP`. 이 길은 `Phase1Initialization` 쪽 `IoInitSystem` 실패 처리 (`+0x3d JNS` 안 뜀 → `+0x55` `KeBugCheckEx`, 첫 인자 `0x69`) 로 이어지는 실패 길로 보인다 `[추측: 0x140c29006 도착 뒤 이어지는 줄은 안 읽음]`.
- `\ArcName\…` 가 reassign 전에 있나: **있다.** `IopCreateArcNames` 는 `IopInitializeBootDrivers +0x7f8` (ret `+0x7fd`) 가 부르고 (`refs`), 그 앞에 `+0x7d3 PnpWaitForDevicesToStart` (ret `+0x7d8`). `IopInitializeBootDrivers` 는 `IoInitSystemPreDrivers +0xf3b` (ret `+0xf40`) 이므로 `PsLocateSystemDlls` (`+0x10f5`) 와 `IopReassignSystemRoot` (`+0x1139`) 보다 앞이다.
  - 그래서 `PsLocateSystemDlls` 가 ntdll 을 여는 순간 `\SystemRoot` 의 풀이 길은 `\SystemRoot → \Device\BootDevice → \ArcName\<ArcBootDeviceName> → (IopCreateArcNames 가 만든 링크 →) 장치` 이다. reassign 뒤에는 중간 `\ArcName` 칸을 건너뛴다 `[정적, 호출 순서 + 링크 대상 글]`. 링크 안 중간 단계 수와 이름은 동적 확인 (동적 1 · 2 · 3).
- 이 VM (test.exe) 에서 타나: 두 함수 모두 갈림 없는 직선 길이라 **부팅이 Phase1 을 지나면 반드시 탄다** (`CreateSystemRootLink` 는 안 되면 버그체크). `IopReassignSystemRoot` 가 `AL=1` 로 끝나는지는 동적. `[VM]` 부팅 뒤 `\Device\BootDevice` 대상이 `\ArcName…` 이 아니라 `\Device\HarddiskVolumeN` 이면 `+0x1d6` 까지 온 것이다 (동적 2).
- 지금 우리 (PE_emulator): **한 군데 다름 + 확인 필요.** `namespace.cpp:34` 가 `\Device\BootDevice → \Device\HarddiskVolume3` 로 처음부터 **reassign 뒤 모양**이다 (맞는 쪽). 그러나 `\ArcName` 디렉터리가 없고 (`:20~22` 디렉터리 목록에 없음), reassign 앞 (`\ArcName\…` 경유) 풀이를 따로 두지 않는다. test.exe 시동이 reassign 뒤 (`IoInitSystemPreDrivers +0x113e` 이후) 만 필요하면 문제없다. `PsLocateSystemDlls` 가 reassign 앞이라 앞 모양을 보이게 할지는 오케스트레이터가 정할 일 → 확인 필요.

### 3. 같이 만드는 것 (이름만)

`CreateSystemRootLink` 가 만드는 **여섯 개** (순서·줄은 `.asm` 의 call 자리):

| # | 종류 | 이름 | 대상 글 | 만드는 줄 (call/ret) | 실패 시 단계 번호 |
|---|---|---|---|---|---|
| 1 | 디렉터리 | `\ArcName` | — | `+0xac`/`+0xb1` `NtCreateDirectoryObject` | 1 |
| 2 | 디렉터리 | `\Device` | — | `+0x11e`/`+0x123` | 2 |
| 3 | 링크 | `\Device\BootDevice` | `\ArcName\<ArcBootDeviceName>` | `+0x1e1`/`+0x1e6` `NtCreateSymbolicLinkObject` (글 짓기 `+0x19d` 가 3, 만들기 4) | 3 · 4 |
| 4 | 링크 | **`\SystemRoot`** | `\Device\BootDevice` + `NtBootPathName` 끝 한 글자 뺀 것 | `+0x2b4`/`+0x2b9` (글 짓기 `+0x266` 가 5, 만들기 6) | 5 · 6 |
| 5 | 링크 | `\Device\OSDataDevice` | `\ArcName\<ArcOSDataDeviceName>` (`LoaderBlock +0x150`, 비어 있으면 `+0xb8 ArcBootDeviceName` 로 갈아끼움: `+0x31f TEST R9,R9`, `+0x322 JNZ +0x32b`, `+0x324 MOV R9,[RBX+0xb8]`) | `+0x37d`/`+0x382` (글 짓기 `+0x339` 가 7, 만들기 8) | 7 · 8 |
| 6 | 링크 | `\OSDataRoot` | `\Device\OSDataDevice` | `+0x406`/`+0x40b` | 9 |

- `\Device` 는 만든 뒤 `\Device\BootDevice` 를 그 안에 만드는 순서이므로 2 가 3 보다 먼저다.
- 이 6 글자열 (`\ArcName`·`\Device`·`\Device\BootDevice`·`\SystemRoot`·`\Device\OSDataDevice`·`\OSDataRoot`) 의 읽기 전용 위치는 INIT 구역 rva `0xc769e0` · `0xc76a00` · `0xc76a10` · `0xc76a60` · `0xc76a90` · `0xc76ac0` (파일 바이트로 확인). `\Device\OSDataDevice` 글은 nt 파일 전체에서 **이 한 곳뿐**이다 (스캔).
- **`IopReassignSystemRoot` 는 `\Device\OSDataDevice` 를 안 바꾼다.** 그래서 정적으로는 `\Device\OSDataDevice` 는 `\ArcName\…` 꼴 대상 그대로 남는다 `[정적: 이 함수 한 곳만 봄 — 다른 곳이 바꾸는지는 안 봄]`.
- 다른 곳에서 `\SystemRoot` 를 만드는 곳: 서버 silo 용 `PspSiloInitializeSystemRootSymlink` 가 있다 (`PspSystemRootSymlinkName` 글을 씀). 부르는 곳은 `PspInitializeServerSiloDeferred +0x4c` 한 곳뿐 (`refs`). test.exe (호스트) 길과 무관해 보이지만 안 읽었다 `[추측]`.
- 이 함수가 `IoAssignArcName`·`IoCreateSymbolicLink` 는 안 부른다 (`.asm` 의 호출 전체: `RtlInitUnicodeString` · `NtCreateDirectoryObject` · `NtCreateSymbolicLinkObject` · `NtClose` · `RtlStringCbPrintfW` · `KeBugCheckEx` · `__security_check_cookie`).
- `\ArcName` 안쪽을 채우는 곳 (이 요청서 밖, 이름만): `IopCreateArcNames` (`+0x109 IopCreateArcNamesDisk`, `+0x115 IopCreateArcNamesCd`). 링크를 실제로 만드는 `IoCreateSymbolicLink` 는 `IopCreateArcName` 의 조각 안 (`frag:FUN_140b44c9e +0x8f`, `frag:FUN_140b44da4 +0x82`) 에 있다. `IopCreateArcNames` 가 `IoArcBootDeviceName` · `IoArcHalDeviceName` · `IoLoaderArcBootDeviceName` 전역을 채운다 (`+0x6f`·`+0xac` 변환, `+0xd4`/`+0xd9` 풀 복사본). `IopCreateArcName` 이 불리는 길은 안 봤다 `[공백]`.
- 지금 우리 (PE_emulator): `\Device` 디렉터리는 **있음** (`namespace.cpp:21` 디렉터리 목록). `\ArcName` 디렉터리·`\Device\OSDataDevice`·`\OSDataRoot` 는 **없음** (PE_emulator 소스·Test 에서 `OSData`·`ArcName` 글을 못 찾음). test.exe 시동 길이 읽는지는 확인 필요.

### 4. 짐 지도 7.6 에 넣을 3장 양식 줄 (공간 = 커널 객체 이름 공간)

기존 7.6.2 "부팅 때 (`Ⓑ`)" 표의 열 (짐 | 단계 | 무엇을 | 놓는 법 | 쓴 주인 | 경로 | 근거) 을 따랐다. **단계 표시는 제안**이다 (`ⒷR` = `\SystemRoot` 링크, `ⒷR2` = reassign. 정하는 건 오케스트레이터·flow-writer). 공간 열은 이 표에 없어서 말로 붙인다: **전부 커널 객체 이름 공간 (에뮬레이터 바깥, nt 엔진·syscall 로만 닿음)**.

| 짐 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `\ArcName` 디렉터리 개체 | `ⒷR` (위상 1) | 영구 · 대소문자 무시 (속성 `0x50`, 커널 핸들 아님), 접근 `0xf000f`, SD = `SePublicDefaultUnrestrictedSd`. 안은 비어 있다가 `IopCreateArcNames` 가 채움 | 직접 | nt | `Phase1Initialization → Phase1InitializationDiscard (호출 +0x1e) → CreateSystemRootLink (호출 +0xc31) +0xac NtCreateDirectoryObject` | `[정적]` sast114 |
| `\Device` 디렉터리 개체 | `ⒷR` | 같음 (속성 `0x50`, 접근 `0xf000f`, 같은 SD) | 직접 | nt | `ⒷR… +0x11e NtCreateDirectoryObject` | `[정적]` sast114 · 이전에 같은 이름이 있었으면 이 호출이 실패해 버그체크 (코드가 충돌을 치명으로 봄) → 첫 생성 `[추측: 충돌 반환값은 안 읽음]` |
| `\Device\BootDevice` 링크 (처음) | `ⒷR` | 대상 = `\ArcName\` + `LoaderBlock +0xb8 ArcBootDeviceName` (ANSI → 글). 영구·대소문자 무시·같은 SD | 입력 (로더 블록 `winload`) → 직접 | nt | `ⒷR… +0x19d RtlStringCbPrintfW · +0x1e1 NtCreateSymbolicLinkObject` | `[정적]` sast114 · 실제 값 동적 1·2 |
| **`\SystemRoot` 링크** | `ⒷR` | 대상 = `\Device\BootDevice` + `LoaderBlock +0xc8 NtBootPathName` 에서 끝 한 글자 뺀 것 (예상 `\Device\BootDevice\Windows`). 영구·대소문자 무시·같은 SD. 만든 뒤 안 바뀜 | 입력 (로더 블록) → 직접 | nt | `ⒷR… +0x266 RtlStringCbPrintfW · +0x2a0 (Length −2) · +0x2b4 NtCreateSymbolicLinkObject` | `[정적]` sast114 · 읽는 곳 `PsLocateSystemDlls → PspLocateSystemDll ZwOpenFile` (`\SystemRoot\System32\ntdll.dll`, sast/42) · 값 동적 1 |
| `\Device\OSDataDevice` 링크 | `ⒷR` | 대상 = `\ArcName\` + (`LoaderBlock +0x150 ArcOSDataDeviceName`, 없으면 `+0xb8 ArcBootDeviceName`) | 입력 (로더 블록) → 직접 | nt | `ⒷR… +0x339 RtlStringCbPrintfW · +0x37d NtCreateSymbolicLinkObject` | `[정적]` sast114 · 읽는 곳 안 봄 · reassign 이 안 바꿈 |
| `\OSDataRoot` 링크 | `ⒷR` | 대상 = `\Device\OSDataDevice` (글 상수) | 직접 | nt | `ⒷR… +0x406 NtCreateSymbolicLinkObject` | `[정적]` sast114 · 읽는 곳 안 봄 |
| `\Device\BootDevice` 링크 (다시 만든 것) | `ⒷR2` (`Ⓑ1` 안, `PsLocateSystemDlls` 뒤) | 옛 링크를 임시로 바꿔 지우고 새로 만듦. 대상 = 이름 사슬 끝의 장치 이름 (`\Device\…`). 영구·대소문자 무시·커널 핸들 (`0x250`), **SD 없음** | 직접 | nt | `Phase1Initialization → IoInitSystem (호출 +0x36) → IoInitSystemPreDrivers (호출 +0x12) → IopReassignSystemRoot (호출 +0x1139) +0x189 NtMakeTemporaryObject · +0x1d6 NtCreateSymbolicLinkObject` | `[정적]` sast114 · 값 동적 2 |
| 장치 이름 ANSI 사본 (`IoInitSystem_deviceNameBuffer`, 256B) | `ⒷR2` | 사슬 끝 이름 (`IopReassignSystemRoot +0x126` 가 복사). 이 뒤 읽는 곳 못 찾음 | 직접 | nt | `ⒷR2… +0x126 RtlUnicodeStringToAnsiString` | `[정적]` sast114 · 칸 크기는 `+0x4b` 의 256 (`refs` `len=0x100`) |
| `\ArcName\…` 링크들 (참고, 이 요청서 밖) | `Ⓑ1` (`IopInitializeBootDrivers` 안) | `IopCreateArcNames` 가 디스크·CD 마다 만듦. 이름 모양은 안 읽음 | 직접 | nt | `IoInitSystemPreDrivers (호출 +0xf3b) → IopInitializeBootDrivers (호출 +0x7f8) → IopCreateArcNames` | `[정적]` 호출 자리만 · 안쪽 `[공백]` (다음 요청 후보) |

- 단계 순서 (7.6.2 에 끼우면): `ⒷA`(위상 1, `Discard +0xc65 MmInitSystem` 안) 보다 **앞** (`Discard +0xc31` 이 `+0xc65` 보다 먼저), `Ⓑ1` (`PsLocateSystemDlls`) 보다 **앞**, `ⒷR2` 는 `Ⓑ1` 뒤.
- 체크리스트 밖 (커널 객체) 이므로 **2장 숫자에 안 센다** (7.6 의 다른 "기록만" 줄과 같은 처리) — 정하는 건 flow-writer.

## 동적분석으로 확인해야 할 것

1. **`\SystemRoot` 링크 대상 글** (`[VM]` 부팅 뒤 읽기만): `!object \SystemRoot` 로 개체 주소를 얻고 `dt nt!_OBJECT_SYMBOLIC_LINK <본체 주소> LinkTarget` 의 글. 기대: `\Device\BootDevice\Windows` (끝 `\` 없음). 정적으로 아는 건 "끝 한 글자 뺌" 뿐이라 `NtBootPathName` 이 `\Windows\` 인지 확인. 영구 표시: `_OBJECT_HEADER +0x1b Flags` 비트 4 `PermanentObject`, SD 는 `_OBJECT_HEADER +0x28 SecurityDescriptor`.
2. **`\Device\BootDevice` 대상 글** (부팅 뒤 읽기만): 기대 `\Device\HarddiskVolumeN`. 대신 `\ArcName\…` 로 남아 있으면 reassign 이 안 끝났거나 다른 길이다. 이 값 하나로 "`+0x1d6 NtCreateSymbolicLinkObject` 까지 왔다" 를 판정한다.
3. **`\ArcName` 안 목록과 사슬**: `!object \ArcName` 의 항목 (예: `multi(0)disk(0)rdisk(0)partition(N)`) 과 각 대상 글. 부팅 때 `ArcBootDeviceName` 원문은 `IoArcBootDeviceName` (유니코드 문자열 전역)·`IoLoaderArcBootDeviceName` (ANSI 복사본, `da poi(nt!IoLoaderArcBootDeviceName)`) 로 부팅 뒤 읽힌다 (`IopCreateArcNames +0xa5`/`+0xd9`). 사슬이 몇 단계인지 (`\ArcName\… → \Device\HarddiskN\PartitionM → \Device\HarddiskVolumeK`?) `[추측]`.
4. **`\Device\OSDataDevice` · `\OSDataRoot` 대상 글**: 기대 `\ArcName\…` (`ArcOSDataDeviceName` 이 비면 `ArcBootDeviceName` 과 같은 글) · `\Device\OSDataDevice`.
5. (부팅 중 멈춰야 하는 질문, 필요하면) `ntdll` 을 여는 시점 (`PsLocateSystemDlls`) 의 `\Device\BootDevice` 대상이 아직 `\ArcName\…` 인지: 멈춤점 `nt!PsLocateSystemDlls` 입구에서 `\Device\BootDevice` 링크 읽기. 어렵다면 정적 순서 (질문 2) 로 충분하다.
- 추천 멈춤점 (필요하면): `nt!CreateSystemRootLink +0x2b4` (`\SystemRoot` 만드는 call, 인자 `R9` = 대상 `UNICODE_STRING`), `nt!IopReassignSystemRoot +0x126` (사슬 끝 이름이 나온 순간, `RDX` = 이름), `+0x1d6` (새 링크 call). 부팅 초기라 test.exe 필터가 필요 없다 (PID 4 `System`) — 설계는 dast-mentor.

## 다음 요청 후보

1. **`\ArcName\…` 링크 채우기**: `IopCreateArcNames` → `IopCreateArcNamesDisk` (395B) · `IopCreateArcNamesCd` (1322B) → `IopCreateArcName` (1206B + 조각) `IoCreateSymbolicLink`. 이름 모양과 대상 (`\Device\HarddiskN\PartitionM`) 이 `\SystemRoot` 풀이 길의 중간이다. 짐 지도 `Ⓑ` 의 작은 요청.
2. **이름 공간 처음 모양** (`ObInitSystem` 이 만든 것과 `IopCreateRootDirectories` 가 `\Driver`·`\FileSystem` 등을 만드는 줄): `\Device` 가 첫 생성인지 확인용. 낮음.
3. (낮음) `PspSiloInitializeSystemRootSymlink` (서버 silo `\SystemRoot`) — test.exe 와 무관해 보이므로 미룸.
4. flow-writer: 7.6.2 에 위 3장 줄을 넣고 Q95 를 닫을 때, `Ⓑ1` 문장의 "`IopReassignSystemRoot` `+0x1139` 앞" 옆에 "`\Device\BootDevice` 를 바꾼다 (`\SystemRoot` 자체는 안 바뀜)" 를 보탤 것.

## 판정

**분석성공** — 요청 질문 4개 모두 `.asm` 줄로 답했다. 정적으로 못 정한 것은 값 (`NtBootPathName`·`ArcBootDeviceName` 실제 글, 사슬 단계 수) 이고 동적 1 ~ 4 로 넘겼다. `ObCreateObjectEx`/`ObInsertObjectEx` 안 SD 적용 줄과 `IopCreateArcName` 이 불리는 길은 안 읽었다 (`[공백]`).

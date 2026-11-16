# 2026-11-16 (작성일) run119 — 부팅 때 `\SystemRoot` 링크를 만드는 곳 (Q95, 정적만): **`Phase1InitializationDiscard +0xc31 CreateSystemRootLink`** 한 곳 (부르는 곳 1), 한 번에 **디렉터리 둘 (`\ArcName` · `\Device`) + 심볼릭 링크 넷 (`\Device\BootDevice` · `\SystemRoot` · `\Device\OSDataDevice` · `\OSDataRoot`)**, `\SystemRoot` = `\Device\BootDevice` + 로더 블록 `NtBootPathName` (끝 한 글자 뺌), 속성 `0x50` (영구 · 대소문자 무시) · SD `SePublicDefaultUnrestrictedSd`. 순서 `CreateSystemRootLink` → `IopCreateArcNames` → `PsLocateSystemDlls` → `IopReassignSystemRoot` — 마지막이 바꾸는 건 **`\Device\BootDevice` 뿐** (`\SystemRoot` 는 그대로)

(글의 오프셋은 call 자리 · 읽는/쓰는 줄 자리. "호출 관계" 나무의 callee 앞 오프셋도 call 자리 — 부팅 길이라 콜스택으로 본 적이 없어서 결과서 꼴 그대로 둔다. ret 는 아래 "정적 — 호출 지점" 표.)

> **지도 범위**: 이번 줄은 모두 **흐름도 범위 앞** (부팅 `Ⓑ`, 프로세스 생성 전) 이다. 그림은 안 바꿨다. test.exe 와 닿는 곳은 부팅 때 `PsLocateSystemDlls` 가 `\SystemRoot\System32\ntdll.dll` 을 여는 자리 (run48) 하나다 — 그 이름이 풀리려면 이번 링크가 먼저 있어야 한다. 글 (flow.md 2.1 부팅 메모 · 3장 새 절) 과 짐 지도 (새 7.6.16) 에만 넣었다.

> **단계 이름**: 결과서는 `ⒷR` · `ⒷR2` 를 제안했지만, 짐 지도의 `ⒷR` 은 run115 가 이미 "부팅 뒤 · 프로세스 생성 앞" 으로 썼다. 겹치지 않게 **`ⒷT`** (`\SystemRoot` 링크 — T = 링크가 가리키는 대상 글) · **`ⒷT2`** (`IopReassignSystemRoot` 가 `\Device\BootDevice` 를 다시 만듦) 로 바꿨다. `Ⓑ0` (숫자 0) 과 헷갈리지 않게 O 는 피했다.

## 요약

- **만드는 곳** `[정적]`: `CreateSystemRootLink` (rva `0xc18a18`) 가 커널 안에서 `NtCreateSymbolicLinkObject` 를 **직접** 부른다 (유저 syscall 길 아님). `\SystemRoot` 줄은 `+0x2b4` (ret `+0x2b9`). 그 함수가 `+0x196 ObCreateSymbolicLink` → `+0x89 ObCreateObjectEx` · `+0x191 ObInsertObjectEx`.
- **부르는 곳 전수** `[정적]`: `Phase1InitializationDiscard +0xc31` (ret `+0xc36`) **한 곳**. `refs` 의 나머지 한 줄은 `.pdata` (함수 밖), 함수 포인터 표 속 값 0곳 (`_refs/CreateSystemRootLink.tsv`).
- **한 번에 여섯** (아래 표 1): `\ArcName` 디렉터리 → `\Device` 디렉터리 → `\Device\BootDevice` 링크 (대상 `\ArcName\` + `ArcBootDeviceName`) → **`\SystemRoot` 링크** (대상 `\Device\BootDevice` + `NtBootPathName`, 끝 2바이트 뺌) → `\Device\OSDataDevice` 링크 (대상 `\ArcName\` + `ArcOSDataDeviceName`, 비면 `ArcBootDeviceName`) → `\OSDataRoot` 링크 (대상 `\Device\OSDataDevice`).
  - 여섯 모두 속성 `0x50` (영구 `0x10` · 대소문자 무시 `0x40`, 커널 핸들 `0x200` 아님) · SD `SePublicDefaultUnrestrictedSd` · 만든 뒤 핸들은 곧장 `NtClose` (영구라 이름은 남음).
  - 하나라도 실패하면 `KeBugCheckEx(0x64, status, 단계 1 ~ 9, 0)`. 정상이면 0.
- **`\SystemRoot` 가 가리키는 디스크는 여기서 안 정한다** — `\Device\BootDevice` 링크가 정한다.
- **순서** `[정적, 호출 순서]`: `Phase1Initialization +0x1e` Discard (`+0xc31 CreateSystemRootLink`) **돌아온 뒤** `+0x36 IoInitSystem` → `IoInitSystemPreDrivers` `+0xf3b IopInitializeBootDrivers` (`+0x7f8 IopCreateArcNames` — `\ArcName\…` 안쪽 링크) → `+0x10f5 PsLocateSystemDlls` (ntdll 열기) → `+0x1139 IopReassignSystemRoot`. 그래서 ntdll 을 여는 순간 `\SystemRoot` 는 늘 있다.
- **`IopReassignSystemRoot`** `[정적]`: `\ArcName\<ArcBootDeviceName>` 부터 링크 사슬을 따라가 **링크가 아닌 개체 (장치)** 에 닿으면 (`0xc0000024`) 그 이름으로 `\Device\BootDevice` 를 **지우고 (`NtMakeTemporaryObject`) 새로 만든다** (`+0x1d6`, 속성 `0x250`, SD 칸 0). 조건 없이 늘 부름 (`PsLocateSystemDlls` 성공 뒤 직선). **`\SystemRoot` · `\Device\OSDataDevice` 는 안 건드린다.**
- 짐 지도: **2장 숫자 그대로** (커널 객체 이름 공간 — 체크리스트 밖). 새 **7.6.16** (9줄) · 3장 줄임 둘 (`ⒷT…` · `ⒷT2…`) · 쓰는 말 단계 둘 · 5장 `Ⓑ` 한 줄 + `Ⓑ1` 문장 보탬 · 7.11 공백 닫힘 · 새 7.50.
- **PE_emulator** (오케스트레이터가 준 줄, flow-writer 는 코드 안 읽음): `\SystemRoot → \Device\BootDevice\Windows` 있음 (모양 맞음), `\Device\BootDevice → \Device\HarddiskVolume3` 은 **처음부터 reassign 뒤 모양**, `\ArcName` · `\Device\OSDataDevice` · `\OSDataRoot` 없음, 영구 표시 · SD 칸 **확인 필요**.

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: **ntoskrnl pdb guid_age `C29EBFB06B78B3C020DCA66D99713F9E1`** (`ms/sym/ntoskrnl.exe.pdb.info.txt` — 기준과 같음, 결과서). 다시 analyze 안 함.
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다.
- **sast/114 결과만 넣었다.** sast/115 · 117 ~ 120 은 안 넣었다 (다음 바퀴 몫).

## 목적

- run48 (sast/42) 공백 2 — "`\SystemRoot` 링크를 만드는 곳과 그 시점 (`PsLocateSystemDlls` 앞)" 을 닫는다. 후보 줄 `Phase1InitializationDiscard +0xc31 CreateSystemRootLink` `[추측]` (이름으로만 본 것) 이 맞나.
- 링크 대상 글의 출처 · 속성 · SD, 나중에 `IopReassignSystemRoot` 가 바꾸나, 같은 함수가 같이 만드는 것.
- 짐 지도 7.6 (체크리스트 밖, 부팅 `Ⓑ` 커널 짐) 에 넣을 줄.

## 방법

- 정적: `_workspace/sast/114_systemroot-link.md` — ntoskrnl.
  - 새로 뽑음: `^CreateSystemRootLink$` depth 1 (8개 — 본체 + `NtCreateDirectoryObject` · `NtCreateSymbolicLinkObject` · `NtClose` · `RtlStringCbPrintfW` · `RtlInitUnicodeString` · `KeBugCheckEx` · `__security_check_cookie`), `^IopReassignSystemRoot$` depth 0.
  - 길 찾기로 depth 0 한 개씩: `IopCreateArcNames` · `IopCreateArcName` · `IopInitializeBootDrivers` · `SeInitSystem` · `SepInitSystemDacls` · `ObCreateSymbolicLink`.
  - 읽기만 (있던 것): `Phase1InitializationDiscard` · `Phase1Initialization` · `IoInitSystem` · `IoInitSystemPreDrivers` · `PsLocateSystemDlls`.
  - `refs`: `CreateSystemRootLink` · `IopReassignSystemRoot` · `IopCreateArcNames` · `SePublicDefaultUnrestrictedSd` · `SepInitSystemDacls` · `SepVariableInitialization` · `SeInitSystem` · `IoInitSystem_deviceNameBuffer` · `PspSystemRootSymlinkName` · `PspSiloInitializeSystemRootSymlink` + 글 상수 3곳. 글 상수 (`"%s%S"` · `"\ArcName\%S"` · 이름 여섯) 는 파일 바이트로 읽음 (결과서).
  - 로더 블록 칸 `+0xb8` · `+0xc8` · `+0x150` 이름은 PDB `_LOADER_PARAMETER_BLOCK` (`pdb_struct.py`, 결과서).
- 결과서: `_workspace/sast/114_systemroot-link_result.md` (**분석성공** — 스크립트 문제 없음).
- flow-writer 가 다시 맞춘 것: 아래 "정적 — 호출 지점" 표를 `.asm` 의 `CALL ret=` 줄로 (`CreateSystemRootLink` call 35줄 전부, `Phase1InitializationDiscard +0x38 · +0xa96 · +0xadb · +0xbdf · +0xc24 · +0xc29 · +0xc2e · +0xc31 · +0xc36 · +0xc4c · +0xc65`, `Phase1Initialization +0x1e · +0x36`, `IoInitSystem +0x12`, `IoInitSystemPreDrivers +0xf3b · +0x10f5 · +0x1105 · +0x1132 · +0x1139 · +0x1140`, `IopInitializeBootDrivers +0x7d3 · +0x7f8`, `IopCreateArcNames +0x109 · +0x115`, `IopReassignSystemRoot` call 10줄 + `+0xc2 · +0xc7 · +0xf4 · +0xf6 · +0x1af · +0x1be · +0x1d0`, `NtCreateSymbolicLinkObject +0x196`, `ObCreateSymbolicLink +0x89 · +0x191`, `SeInitSystem +0x10 · +0x2c · +0x33`, `SepInitializationPhase0 +0x6`, `InitBootProcessor +0xb94`, `SepInitSystemDacls +0x683`), 줄 대상 글은 `CreateSystemRootLink +0x42 · +0xa8 · +0x22a · +0x248 · +0x296 · +0x2a0`, `refs` 두 장 (`CreateSystemRootLink.tsv` · `SepInitSystemDacls.tsv`) 과 `PspSiloInitializeSystemRootSymlink.tsv` 4행. 모두 결과서와 같다.
- 앞 바퀴 짝: run48 (sast/42 — 부팅 준비 갈래, 공백 2), run98 (smss 가 만드는 객체 디렉터리 — 같은 "부팅 때 이름 공간" 짐), run115 (`Ⓑ` 단계 줄임 · KUSD `NtSystemRoot`), run57 · run117 (`NtGetNlsSectionPtr` 가 부팅 때 `\SystemRoot\System32\c_%.3d.nls` 를 여는 갈래).

## 결과

비유 (객체 이름 공간 = 동네 **길 안내 표지판** 들):

- 커널 안에는 이름으로 물건을 찾는 큰 안내판 나무가 있다. `\SystemRoot` 는 그중 "윈도우 폴더 가는 길" 표지판이다.
- 이 표지판은 건물 (디스크) 을 직접 가리키지 않는다. **"`\Device\BootDevice` 표지판을 따라가서 `\Windows`"** 라고만 적혀 있다.
- `\Device\BootDevice` 표지판은 처음엔 **"`\ArcName\…` 표지판으로 가시오"** (부팅 로더가 쓴 옛 주소 꼴) 다.
- 디스크 장치가 다 준비된 뒤 관청 (nt) 이 길을 끝까지 걸어가 본다. 진짜 건물에 닿으면, `\Device\BootDevice` 표지판을 떼고 **"그 건물로 바로 가시오"** 로 새로 단다.
- `\SystemRoot` 표지판은 그대로다. 중간 표지판 하나만 바뀌어서 길이 한 칸 짧아진다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/*.asm` 의 `CALL ret=` 줄 · `_refs`)

| caller | call | ret | callee | 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!Phase1Initialization` | `+0x1e` | `+0x23` | `nt!Phase1InitializationDiscard` | `ⒷP` — 위상 1 본체. 인자 = 로더 블록 (`Discard +0x38 MOV RSI,RCX`) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xa96` | `+0xa9b` | `nt!ObInitSystem` | 개체 관리자 (앞서 끝남) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xadb` | `+0xae0` | `nt!IoCreateObjectTypes` | (앞서 끝남) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xbdf` | `+0xbe4` | `nt!SeInitSystem` | 위상 1 갈래 (`SeInitSystem +0x10 CMP EAX,1` → `+0x2c SepInitializationPhase1`) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xc24` | `+0xc29` | `nt!PsInitSystem` | | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xc29` | `+0xc2e` | `nt!ExInitLicenseCallback` | | [정적] |
| `nt!Phase1InitializationDiscard` | **`+0xc31`** | **`+0xc36`** | **`nt!CreateSystemRootLink`** | `ⒷT` — 인자 `+0xc2e MOV RCX,RSI` (로더 블록). 뒤 `+0xc36 TEST EAX,EAX` · 실패 갈래 `+0xc4c KeBugCheckEx` (`0x64`) — 반환이 늘 0 이라 닿지 않음 `[추측]` | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xc65` | `+0xc6a` | `nt!MmInitSystem` (위상 1) | `ⒷA` (ApiSet, run64) — `ⒷT` 뒤 | [정적] |
| `nt!CreateSystemRootLink` | `+0xac` | `+0xb1` | `nt!NtCreateDirectoryObject` | ① `\ArcName` | [정적] |
| `nt!CreateSystemRootLink` | `+0x11e` | `+0x123` | `nt!NtCreateDirectoryObject` | ② `\Device` | [정적] |
| `nt!CreateSystemRootLink` | `+0x19d` | `+0x1a2` | `nt!RtlStringCbPrintfW` | `"\ArcName\%S"` + `[RBX+0xb8]` (`+0x15a`) | [정적] |
| `nt!CreateSystemRootLink` | `+0x1e1` | `+0x1e6` | `nt!NtCreateSymbolicLinkObject` | ③ `\Device\BootDevice` | [정적] |
| `nt!CreateSystemRootLink` | `+0x266` | `+0x26b` | `nt!RtlStringCbPrintfW` | `"%s%S"` — `%s` = `\Device\BootDevice` (`+0x22a`), `%S` = `[RBX+0xc8]` (`+0x248`) | [정적] |
| `nt!CreateSystemRootLink` | **`+0x2b4`** | **`+0x2b9`** | **`nt!NtCreateSymbolicLinkObject`** | ④ **`\SystemRoot`** — 그 앞 `+0x291` (ret `+0x296`) `RtlInitUnicodeString` · `+0x296 MOV EAX,0xfffe` · `+0x2a0 ADD [RSP+0x68],AX` (길이 −2) | [정적] |
| `nt!CreateSystemRootLink` | `+0x339` | `+0x33e` | `nt!RtlStringCbPrintfW` | `"\ArcName\%S"` + `[RBX+0x150]` (0 이면 `+0x324` 에서 `[RBX+0xb8]`) | [정적] |
| `nt!CreateSystemRootLink` | `+0x37d` | `+0x382` | `nt!NtCreateSymbolicLinkObject` | ⑤ `\Device\OSDataDevice` | [정적] |
| `nt!CreateSystemRootLink` | `+0x406` | `+0x40b` | `nt!NtCreateSymbolicLinkObject` | ⑥ `\OSDataRoot` | [정적] |
| `nt!NtCreateSymbolicLinkObject` | `+0x196` | `+0x19b` | `nt!ObCreateSymbolicLink` | 공용 (링크 만들기 본체) | [정적] |
| `nt!ObCreateSymbolicLink` | `+0x89` · `+0x191` | `+0x8e` · `+0x196` | `nt!ObCreateObjectEx` · `nt!ObInsertObjectEx` | 공용 부품 — SD 가 붙는 줄은 안 읽음 `[공백]` | [정적] |
| `nt!Phase1Initialization` | `+0x36` | `+0x3b` | `nt!IoInitSystem` | Discard 가 돌아온 뒤 | [정적] |
| `nt!IoInitSystem` | `+0x12` | `+0x17` | `nt!IoInitSystemPreDrivers` | `Ⓑ…` (run48) | [정적] |
| `nt!IoInitSystemPreDrivers` | `+0xf3b` | `+0xf40` | `nt!IopInitializeBootDrivers` | | [정적] |
| `nt!IopInitializeBootDrivers` | `+0x7d3` | `+0x7d8` | `nt!PnpWaitForDevicesToStart` | | [정적] |
| `nt!IopInitializeBootDrivers` | `+0x7f8` | `+0x7fd` | `nt!IopCreateArcNames` | `\ArcName\…` 안쪽 링크 (refs 한 곳) | [정적] |
| `nt!IopCreateArcNames` | `+0x109` · `+0x115` | `+0x10e` · `+0x11a` | `nt!IopCreateArcNamesDisk` · `nt!IopCreateArcNamesCd` | 이름만 (안쪽 안 읽음) | [정적] |
| `nt!IoInitSystemPreDrivers` | `+0x10f5` | `+0x10fa` | `nt!PsLocateSystemDlls` | `Ⓑ1` — `\SystemRoot\System32\ntdll.dll` 열기 (run48) | [정적] |
| `nt!IoInitSystemPreDrivers` | **`+0x1139`** | **`+0x113e`** | **`nt!IopReassignSystemRoot`** | `ⒷT2` — `+0x1105 JNS` (`PsLocateSystemDlls` 성공) 뒤 직선. 인자 `RDX` = `+0x1132 LEA RDX,[RBP-0x50]` (`ANSI_STRING`, 버퍼 `IoInitSystem_deviceNameBuffer`). 실패 `AL=0` → `+0x1140 JNZ` 안 뜀 → `+0x114c IopInitFailCode = 9` | [정적] |
| `nt!IopReassignSystemRoot` | `+0x61` | `+0x66` | `nt!RtlStringCchPrintfW` | `\ArcName\` + `[RCX+0xb8]` (`+0x28`) | [정적] |
| `nt!IopReassignSystemRoot` | `+0xbd` · `+0xdf` · `+0xed` | `+0xc2` · `+0xe4` · `+0xf2` | `nt!NtOpenSymbolicLinkObject` · `nt!NtQuerySymbolicLinkObject` · `nt!ObCloseHandle` | 사슬 따라가기 고리 (`+0xf4 JNS` → `+0x86`). 열기가 `0xc0000024` (`+0xc2` · `+0xc7 JZ`) 면 장치에 닿음 | [정적] |
| `nt!IopReassignSystemRoot` | `+0x126` | `+0x12b` | `nt!RtlUnicodeStringToAnsiString` | 장치 이름 → 호출자 `ANSI_STRING` | [정적] |
| `nt!IopReassignSystemRoot` | `+0x177` · `+0x189` · `+0x195` | `+0x17c` · `+0x18e` · `+0x19a` | `nt!NtOpenSymbolicLinkObject` · **`nt!NtMakeTemporaryObject`** · `nt!ObCloseHandle` | 옛 `\Device\BootDevice` 지우기 | [정적] |
| `nt!IopReassignSystemRoot` | **`+0x1d6`** · `+0x1e2` | `+0x1db` · `+0x1e7` | **`nt!NtCreateSymbolicLinkObject`** · `nt!ObCloseHandle` | 새 `\Device\BootDevice` (대상 `+0x1af LEA R9,[RSP+0x28]`, 속성 `+0x1be` `0x250`, SD `+0x1d0` 0) → `+0x1e7 MOV AL,1` | [정적] |
| `nt!InitBootProcessor` | `+0xb94` | `+0xb99` | `nt!SeInitSystem` (위상 0) | SD 출처 사슬 첫 칸 | [정적] |
| `nt!SeInitSystem` | `+0x33` | `+0x38` | `nt!SepInitializationPhase0` | | [정적] |
| `nt!SepInitializationPhase0` | `+0x6` | `+0xb` | `nt!SepVariableInitialization` | (`.asm` 없음 — 아래 줄은 `refs`) | [정적] |
| `nt!SepVariableInitialization` | `+0x1923` | — | `nt!SepInitSystemDacls` | `refs` 한 줄 (`UNCONDITIONAL_CALL`). 안에서 `+0x683` `MOV [SePublicDefaultUnrestrictedSd],RBX` | [정적] (`refs`) |

- `CreateSystemRootLink` 의 나머지 call 은 이름 짓기 (`RtlInitUnicodeString` `+0x72` · `+0xe2` · `+0x155` · `+0x1c8` · `+0x218` · `+0x291` · `+0x2eb` · `+0x364` · `+0x3b4` · `+0x3ed`), 버그체크 (`+0xc7` · `+0x13a` · `+0x1b9` · `+0x1fd` · `+0x282` · `+0x2d0` · `+0x355` · `+0x399` · `+0x422`), 핸들 닫기 (`NtClose` `+0xd2` · `+0x145` · `+0x208` · `+0x2db` · `+0x3a4` · `+0x42d`), `+0x43e __security_check_cookie` 다. 이 밖의 callee 는 없다 (`IoAssignArcName` · `IoCreateSymbolicLink` 안 부름).

### 호출 관계

```
[위상 0 — BSP]
KiInitializeKernel → +0x7fe InitBootProcessor
 └─ +0xb94  SeInitSystem(0) ─ +0x33 SepInitializationPhase0 ─ +0x6 SepVariableInitialization
                               └─ +0x1923 SepInitSystemDacls     +0x683 SePublicDefaultUnrestrictedSd ← DACL 만 (허용 넷)
[위상 1 — 시스템 스레드]
Phase1Initialization
 ├─ (1) +0x1e  Phase1InitializationDiscard (RSI = 로더 블록)
 │        ├─ +0xa96  ObInitSystem · +0xadb IoCreateObjectTypes · +0xbdf SeInitSystem(1) · +0xc24 PsInitSystem · +0xc29 ExInitLicenseCallback
 │        ├─ +0xc31  CreateSystemRootLink                                  ← 이번 (ⒷT)
 │        │     ├─ +0xac   NtCreateDirectoryObject      \ArcName
 │        │     ├─ +0x11e  NtCreateDirectoryObject      \Device
 │        │     ├─ +0x19d  RtlStringCbPrintfW · +0x1e1 NtCreateSymbolicLinkObject   \Device\BootDevice → \ArcName\<ArcBootDeviceName>
 │        │     ├─ +0x266  RtlStringCbPrintfW · +0x2a0 길이 −2 · +0x2b4 NtCreateSymbolicLinkObject   \SystemRoot → \Device\BootDevice<NtBootPathName − 끝 한 글자>
 │        │     │            └─ +0x196 ObCreateSymbolicLink ─ +0x89 ObCreateObjectEx · +0x191 ObInsertObjectEx   (공용)
 │        │     ├─ +0x339  RtlStringCbPrintfW · +0x37d NtCreateSymbolicLinkObject   \Device\OSDataDevice → \ArcName\<ArcOSDataDeviceName | ArcBootDeviceName>
 │        │     └─ +0x406  NtCreateSymbolicLinkObject   \OSDataRoot → \Device\OSDataDevice
 │        └─ +0xc65  MmInitSystem(1)                                       (ⒷA, run64)
 └─ (2) +0x36  IoInitSystem ─ +0x12 IoInitSystemPreDrivers
          ├─ +0xf3b   IopInitializeBootDrivers
          │     ├─ +0x7d3  PnpWaitForDevicesToStart
          │     └─ +0x7f8  IopCreateArcNames ─ +0x109 IopCreateArcNamesDisk · +0x115 IopCreateArcNamesCd   \ArcName\… 안쪽 (이름만)
          ├─ +0x10f5  PsLocateSystemDlls                                   (Ⓑ1, run48 — \SystemRoot\System32\ntdll.dll 열기)
          └─ +0x1139  IopReassignSystemRoot                                ← 이번 (ⒷT2)
                ├─ +0x61   RtlStringCchPrintfW                \ArcName\<ArcBootDeviceName>
                ├─ +0xbd   NtOpenSymbolicLinkObject ⟲ +0xdf NtQuerySymbolicLinkObject · +0xed ObCloseHandle   (장치에 닿을 때까지)
                ├─ +0x126  RtlUnicodeStringToAnsiString       장치 이름 → IoInitSystem_deviceNameBuffer
                ├─ +0x177  NtOpenSymbolicLinkObject · +0x189 NtMakeTemporaryObject · +0x195 ObCloseHandle   옛 \Device\BootDevice 지움
                └─ +0x1d6  NtCreateSymbolicLinkObject · +0x1e2 ObCloseHandle   새 \Device\BootDevice → \Device\<장치>
```

### 1. 만드는 여섯 개 — 결과서 질문 1 · 3 `[정적]`

| # | 종류 | 이름 (INIT 구역 글 rva) | 대상 글 | 만드는 줄 (call / ret) | 실패 단계 |
|---|---|---|---|---|---|
| 1 | 디렉터리 | `\ArcName` (`0xc769e0`) | — (안은 비어 있다가 `IopCreateArcNames` 가 채움) | `+0xac` / `+0xb1` | 1 |
| 2 | 디렉터리 | `\Device` (`0xc76a00`) | — | `+0x11e` / `+0x123` | 2 |
| 3 | 링크 | `\Device\BootDevice` (`0xc76a10`) | `\ArcName\` + 로더 블록 `+0xb8 ArcBootDeviceName` (ANSI, `"\ArcName\%S"` 상수 `0x140c76a40`) | `+0x1e1` / `+0x1e6` | 3 (글) · 4 (만들기) |
| 4 | 링크 | **`\SystemRoot`** (`0xc76a60`) | `\Device\BootDevice` + 로더 블록 `+0xc8 NtBootPathName` 에서 **끝 2바이트 (한 글자) 뺀 것** (`"%s%S"` 상수 `0x140c76a80`). 예상 `\Device\BootDevice\Windows` `[추측: 실제 NtBootPathName 은 정적으로 모름]` | `+0x2b4` / `+0x2b9` | 5 · 6 |
| 5 | 링크 | `\Device\OSDataDevice` (`0xc76a90`) | `\ArcName\` + 로더 블록 `+0x150 ArcOSDataDeviceName` — 0 이면 `+0xb8` 로 갈아끼움 (`+0x31f TEST R9,R9` · `+0x322 JNZ` · `+0x324`) | `+0x37d` / `+0x382` | 7 · 8 |
| 6 | 링크 | `\OSDataRoot` (`0xc76ac0`) | `\Device\OSDataDevice` (글 상수) | `+0x406` / `+0x40b` | 9 |

- `\Device` 를 만든 뒤에 그 안의 `\Device\BootDevice` 를 만든다 (2 → 3). `\Device\OSDataDevice` 글은 nt 파일 전체에서 이 한 곳뿐이다 (결과서 스캔).
- "끝 한 글자 뺌" 은 `NtBootPathName` 이 `\` 로 끝난다는 걸 믿고 짠 코드로 읽힌다 `[추측: 뜻]`.
- 로더 블록 칸 이름 `+0xb8` · `+0xc8` · `+0x150` = PDB `_LOADER_PARAMETER_BLOCK` 의 `ArcBootDeviceName` · `NtBootPathName` · `ArcOSDataDeviceName` (결과서 `pdb_struct.py`). 로더 블록은 `Phase1InitializationDiscard` 인자 그대로 (`+0x38` → `+0xc2e`).

### 2. 속성 · 접근 · SD — 결과서 질문 1 `[정적]`

- **속성** 여섯 모두 `0x50` = 영구 (`OBJ_PERMANENT 0x10`) · 대소문자 무시 (`0x40`). 커널 핸들 (`0x200`) 없음. 근거 `+0x42 MOV [RSP+0x50],0x50` (첫째), 이후 `+0xa8 LEA R14D,[RDI+0x50]` 값을 다섯 번 씀 (`+0x114` · `+0x193` · `+0x25c` · `+0x31a` · `+0x3e3`, 결과서). `RootDirectory` 0 · `SecurityQualityOfService` 0.
  - 대소문자 무시는 만들 때 넘긴 속성값이다. 개체에 따로 남는 표시인지는 안 읽었다. 영구 표시는 `_OBJECT_HEADER +0x1b Flags` 비트 4 `PermanentObject` (PDB, 결과서).
- **접근**: 디렉터리 `0xf000f`, 링크 `0xf0001` (결과서).
- **SD** 여섯 모두 `SePublicDefaultUnrestrictedSd` (전역 포인터를 `+0x8a` · `+0xf9` · `+0x171` · `+0x231` · `+0x300` · `+0x3cd` 에서 읽음, 결과서).
  - 채우는 곳: 위상 0 `InitBootProcessor +0xb94 SeInitSystem` → `+0x33 SepInitializationPhase0` → `+0x6 SepVariableInitialization` → `+0x1923 SepInitSystemDacls` → `+0x683` (위 표). 위상 1 `+0xc31` 보다 훨씬 앞이다.
  - 모양 (결과서, `SepInitSystemDacls.c` 요약): DACL 만 (소유자 · 그룹 · SACL 없음), 허용 넷 — Everyone `GENERIC_EXECUTE` · LocalSystem `GENERIC_ALL` · Administrators `GENERIC_ALL` · Restricted `GENERIC_EXECUTE`. 비유로는 "누구나 지나갈 수는 있고, 고치는 건 관리인 · 관리자만".
  - 그 SD 가 개체에 실제로 붙는 줄 (`ObCreateObjectEx` · `ObInsertObjectEx` 안) 은 안 읽었다 `[공백, 작음]`. 붙는 계산은 run100 (`ObpAssignSecurity → SeAssignSecurityEx2`) 과 같은 길일 것 `[추측]`.
- **실패**: 단계마다 `KeBugCheckEx(0x64, status, 단계 번호, 0)` (위 표 버그체크 9줄), 정상은 `+0x432 XOR EAX,EAX` → 0. 이미 같은 이름이 있으면 만들기가 실패해 버그체크다 → `\Device` · `\ArcName` 은 여기가 **처음 생성** 으로 읽힌다 `[추측: 충돌 반환값은 안 읽음]`.

### 3. 시점 · 순서 — 결과서 질문 2 `[정적]`

- **`+0xc31` 이 맞다** — call 자리 `+0xc31`, ret `+0xc36`. run48 후보 `[추측]` → `[정적]`.
- 그 앞 `ObInitSystem` (`+0xa96`) · `IoCreateObjectTypes` (`+0xadb`) 가 끝났다. 링크 · 디렉터리 개체 종류가 이미 있다는 뜻으로 읽힌다 `[추측: 개체 종류 만드는 줄은 안 봄]`.
- **`PsLocateSystemDlls` 보다 앞** — `Phase1Initialization` 이 `+0x1e` Discard 를 **다 돌고 나서** `+0x36 IoInitSystem` → `+0x12 IoInitSystemPreDrivers` → `+0x10f5 PsLocateSystemDlls`. 그래서 `PspLocateSystemDll` 의 `ZwOpenFile` (`\SystemRoot\System32\ntdll.dll`, run48 표 `PspSystemDlls` 칸 `+0x10`) 때 `\SystemRoot` 는 늘 있다.
- **ntdll 을 여는 순간의 풀이 길** `[정적, 호출 순서 + 링크 대상 글]`: `\SystemRoot` → `\Device\BootDevice` → `\ArcName\<ArcBootDeviceName>` → (`IopCreateArcNames` 가 만든 링크) → 장치. `IopCreateArcNames` (`IopInitializeBootDrivers +0x7f8`, 그 앞 `+0x7d3 PnpWaitForDevicesToStart`) 는 `IoInitSystemPreDrivers +0xf3b` 안이라 `+0x10f5` 보다 앞이다. 사슬 가운데 단계 수 · 이름은 동적 (아래 동적 3).
- 부팅 때 `ExInitializeNls` (`ⒷN`, `Phase1InitializationIoReady +0x27`) 가 `NtGetNlsSectionPtr` 갈래로 `\SystemRoot\System32\c_%.3d.nls` 를 여는 것 (flow.md 2.4 NLS 절, run57 · run117) 도 `ⒷT` 뒤다 (`Phase1Initialization +0x61` > `+0x1e`).

### 4. `IopReassignSystemRoot` — 결과서 질문 2 `[정적]`

- **`\SystemRoot` 는 안 바꾼다. 바꾸는 건 `\Device\BootDevice` 다.**
- 하는 일: `\ArcName\<ArcBootDeviceName>` 글 (`+0x61`) → 링크로 열고 (`+0xbd`, 속성 `0x240` = 대소문자 무시 + 커널 핸들) 대상 글을 같은 칸에 읽고 (`+0xdf`) 닫고 (`+0xed`) 다시 열기 (`+0xf4 JNS` → `+0x86`) — 열기가 **`0xc0000024`** (`STATUS_OBJECT_TYPE_MISMATCH`, 링크가 아닌 개체 = 장치) 면 멈춤 → 그 이름을 ANSI 로 (`+0x126`) → `\Device\BootDevice` 열기 (`+0x177`) · **임시로 바꿔 (`+0x189 NtMakeTemporaryObject`) 닫기 (`+0x195`) = 지우기** → **같은 이름으로 새로 만들기 (`+0x1d6`)**, 대상 = 찾은 장치 이름 → `+0x1e7 MOV AL,1`.
- 새 링크 속성 `0x250` (영구 · 대소문자 무시 · 커널 핸들), **SD 칸 0** (`+0x1d0`) — `SePublicDefaultUnrestrictedSd` 를 안 준다. 붙는 SD 가 처음 링크와 같은지는 모름 (기본 SD 계산 안 읽음) `[공백]`.
- **조건 없음**: `+0x1105 JNS` (`PsLocateSystemDlls` 성공) 뒤 직선. VHD · BitLocker 를 따로 보는 줄은 이 함수에 없다 (VHD 확인은 `IopCreateArcName +0x489 VhdVerifyBootDisk` 쪽, 결과서). 갈림은 이름 사슬뿐.
- 실패 (`NtOpenSymbolicLinkObject` 가 다른 음수 · 변환 실패 · `\Device\BootDevice` 열기 실패) 면 `+0xf6 XOR AL,AL` → `IoInitSystemPreDrivers +0x114c IopInitFailCode = 9` → `IoInitSystem` 실패 → `Phase1Initialization` 쪽 버그체크 (`0x69`) 로 이어지는 것으로 보인다 `[추측: 도착 뒤 줄은 안 읽음]`.
- 장치 이름 ANSI 사본은 `IoInitSystem_deviceNameBuffer` (256B, `IoInitSystemPreDrivers +0x4b` · `+0x56` 에서 연결) 에 남는다. 이 버퍼를 읽는 다른 코드는 `refs` 에 없다 (`IoInitSystemPreDrivers` 안 두 줄뿐).
- `\Device\OSDataDevice` 도 안 바꾼다 — `\ArcName\…` 꼴 대상 그대로 `[정적: 이 함수만 봄 — 다른 곳이 바꾸는지는 안 봄]`.
- 이 VM 에서 타나: 두 함수 모두 갈림 없는 길이라 부팅이 위상 1 을 지나면 탄다 `[추측: 실행 확인 없음]`. `IopReassignSystemRoot` 가 `AL=1` 로 끝났는지는 부팅 뒤 `\Device\BootDevice` 대상 글로 가린다 (동적 2).

### 5. 그 밖 — `\SystemRoot` 를 만드는 다른 곳

- 서버 silo 용 `PspSiloInitializeSystemRootSymlink` (글 `PspSystemRootSymlinkName`) 가 있다. 부르는 곳은 `PspInitializeServerSiloDeferred +0x4c` 한 곳 (`_refs/PspSiloInitializeSystemRootSymlink.tsv` 4행). test.exe (호스트 실로) 길과는 무관해 보이지만 본문은 안 읽었다 `[추측]`.

## 확인된 것

- `[정적]` `\SystemRoot` 링크는 `CreateSystemRootLink +0x2b4 NtCreateSymbolicLinkObject` 가 만든다. 부르는 곳은 `Phase1InitializationDiscard +0xc31` 하나 (`refs` 코드 호출 1줄 · 포인터 값 0곳).
- `[정적]` 같은 함수가 `\ArcName` · `\Device` 디렉터리와 `\Device\BootDevice` · `\Device\OSDataDevice` · `\OSDataRoot` 링크를 같이 만든다. 여섯 모두 속성 `0x50` · SD `SePublicDefaultUnrestrictedSd`, 실패면 버그체크 `0x64`.
- `[정적]` `\SystemRoot` 대상 = `\Device\BootDevice` + `NtBootPathName` (끝 2바이트 뺌). `\Device\BootDevice` 처음 대상 = `\ArcName\` + `ArcBootDeviceName`.
- `[정적]` 호출 순서: `CreateSystemRootLink` → `IopCreateArcNames` → `PsLocateSystemDlls` → `IopReassignSystemRoot`. ntdll 을 열 때 `\SystemRoot` 는 늘 있다.
- `[정적]` `IopReassignSystemRoot` 는 조건 없이 불리고 `\Device\BootDevice` 만 지우고 다시 만든다 (대상 = 이름 사슬 끝 장치, 속성 `0x250`, SD 칸 0). `\SystemRoot` · `\Device\OSDataDevice` 는 그대로.
- `[정적]` `SePublicDefaultUnrestrictedSd` 는 위상 0 `SepInitSystemDacls +0x683` 이 채운다 (`+0xc31` 보다 앞).

## 결과서와 다르게 적은 곳 · 보탠 것

- **단계 이름**: 결과서 `ⒷR` · `ⒷR2` → 이 문서 · 짐 지도 **`ⒷT` · `ⒷT2`** (run115 `ⒷR` = "부팅 뒤 · 프로세스 생성 앞" 과 겹쳐서. 위 머리말).
- **`\ArcName\…` 링크들 줄의 단계**: 결과서 `Ⓑ1` (`IopInitializeBootDrivers` 안) → 짐 지도는 **`Ⓑ`** 로 적었다. 7.6.2 의 `Ⓑ1` 은 "`PsLocateSystemDlls` 구간" 이라, 그보다 앞인 `IopInitializeBootDrivers` (`+0xf3b`) 는 run115 KUSD `NtSystemRoot` 줄처럼 그냥 `Ⓑ` 다.
- **`IopReassignSystemRoot` 의 핸들 닫기** 는 `NtClose` 가 아니라 `ObCloseHandle` (`+0xed` · `+0x195` · `+0x1e2`, `.asm`). 결과서 "핸들 닫기" 와 뜻은 같다.
- **`SeInitSystem` 사슬 방향**: 결과서는 `refs` 를 거꾸로 (`SepVariableInitialization +0x1923` → `SepInitializationPhase0 +0x6` → `SeInitSystem +0x33`) 적었다. 이 문서는 부르는 순서로 (`SeInitSystem +0x33` → `SepInitializationPhase0 +0x6` → `SepVariableInitialization +0x1923` → `SepInitSystemDacls`) 적었다. 뜻은 같다.
- **보탬 (flow.md 에서)**: 부팅 NLS 섹션 갈래 (`NtGetNlsSectionPtr +0x3ac RtlpInitNlsFileName` `\SystemRoot\System32\c_%.3d.nls`, run57 · run117) 도 `\SystemRoot` 를 읽는 곳이고 코드 순서로 `ⒷT` 뒤다.

## 정적과 동적이 다른 곳

- 없다 (새 동적 없음).
- **맞춰 볼 것 하나 (모순 아님, 짐작만)**: run115 사진 KUSD `NtSystemRoot` = `"C:\WINDOWS"` (끝 `\` 없음 — `ms/logs/04_baggage-read_supp_<K1 ~ P2>.txt:190`) 은 `IopStoreBootDriveLetter +0x136` 이 `"%C:%S"` 로 같은 `NtBootPathName` 을 써서 만든다 (run115). 이번 `CreateSystemRootLink` 는 같은 글에서 **끝 한 글자를 뺀다**. 둘이 맞으려면 ① `NtBootPathName` 이 `\` 로 안 끝나고 `\SystemRoot` 쪽이 `\Device\BootDevice\WINDOW` 가 되거나 ② `\` 로 끝나고 `IopStoreBootDriveLetter` 쪽이 `%S` 인자 (`+0x10c MOV R9,RSI`) 를 따로 다듬거나 — 어느 쪽인지 정적으로 안 봤다 `[추측: ② 쪽 — 이 코드가 일부러 −2 를 함]`. 동적 1 (`!object \SystemRoot` 대상 글) 하나로 닫힌다. → flow.md 4장.
- 디버거가 붙어서 달라지는 점: 이 길에서 디버거 깃발을 보는 줄은 못 봤다 (결과서 줄 범위 안). 부팅 디버깅 (KDNET) 이 링크 값을 바꿀 까닭은 안 보인다 `[추측]`.

## 정정

- 이전 run 이 틀린 곳은 없다. 이전 run 문서 맨 위 정정 한 줄: 없음.
- 태그 올림 (틀린 게 아니라 근거가 생김): run48 "아직 확인 필요" · flow.md 4장 run48 줄 · 짐 지도 7.11 의 "`\SystemRoot` 링크 만드는 곳 — `Phase1InitializationDiscard +0xc31 CreateSystemRootLink` 후보 `[추측]`" → **`[정적]`** (이번).

## 이전 run 에서 닫은 것

- **run48 (sast/42) 공백 2** "`\SystemRoot` 링크를 만드는 곳과 그 시점 (`PsLocateSystemDlls` 앞)" → 닫힘. **Q95 끝.**
- run48 "`\SystemRoot` 링크가 이미 있어야 한다 `[정적]`" (호출 순서로만 말한 것) → 만드는 줄로 확인.

## 아직 확인 필요

### 동적 (결과서 "동적분석으로 확인해야 할 것" — 대기열 번호는 오케스트레이터가 붙임. 부팅 뒤 읽기만, test.exe 조건 필요 없음 — 전역 개체)

1. **`\SystemRoot` 대상 글**: `!object \SystemRoot` → `dt nt!_OBJECT_SYMBOLIC_LINK <본체> LinkTarget`. 기대 `\Device\BootDevice\Windows` (끝 `\` 없음). 영구 표시 `_OBJECT_HEADER +0x1b` 비트 4, SD `_OBJECT_HEADER +0x28`. 위 "맞춰 볼 것" 도 이걸로 닫힘.
2. **`\Device\BootDevice` 대상 글**: 기대 `\Device\HarddiskVolumeN`. `\ArcName\…` 로 남아 있으면 reassign 이 안 끝났거나 다른 길 — 이 값 하나로 `+0x1d6` 까지 왔는지 판정. 새 링크의 SD (SD 칸 0 으로 만든 것) 도 같이.
3. **`\ArcName` 안 목록과 사슬**: `!object \ArcName` 항목과 대상 글, 사슬 단계 수. 부팅 때 원문은 `IoArcBootDeviceName` · `da poi(nt!IoLoaderArcBootDeviceName)` (`IopCreateArcNames +0xa5` / `+0xd9`, 결과서).
4. **`\Device\OSDataDevice` · `\OSDataRoot` 대상 글**: 기대 `\ArcName\…` · `\Device\OSDataDevice`.
5. (선택, 부팅 중 멈춤) `nt!PsLocateSystemDlls` 입구에서 `\Device\BootDevice` 가 아직 `\ArcName\…` 인지. 정적 순서로 충분하면 생략. 멈춤점 후보 `nt!CreateSystemRootLink+0x2b4` (`R9` = 대상 `UNICODE_STRING`) · `nt!IopReassignSystemRoot+0x126` · `+0x1d6` — 부팅 초기라 System (PID 4) 문맥, 설계는 dast-mentor.

### 정적 (결과서 "다음 요청 후보" + 못 읽은 것)

1. (작음) **`\ArcName\…` 안쪽 링크 채우기** — `IopCreateArcNames` → `IopCreateArcNamesDisk` · `IopCreateArcNamesCd` → `IopCreateArcName` (조각 `FUN_140b44c9e +0x8f` · `FUN_140b44da4 +0x82` 의 `IoCreateSymbolicLink`). 이름 꼴과 대상 (`\Device\HarddiskN\PartitionM` 꼴 `[추측]`) — `\SystemRoot` 풀이 길의 중간. `IopCreateArcName` 이 불리는 길도 `[공백]`.
2. (낮음) 이름 공간 처음 모양 — `ObInitSystem` (`+0x557` 등, run98) · `IopCreateRootDirectories` 가 만드는 디렉터리. `\Device` 가 첫 생성인지 확인용.
3. (낮음) `PspSiloInitializeSystemRootSymlink` (서버 silo `\SystemRoot`) — test.exe 와 무관해 보여 미룸.
4. (작음) `ObCreateObjectEx` / `ObInsertObjectEx` 안에서 SD 가 개체에 붙는 줄 (run100 길과 같은지), `IopReassignSystemRoot` 새 링크 (SD 칸 0) 가 받는 기본 SD.
5. (작음) `IopStoreBootDriveLetter +0x10c` 의 `RSI` (`%S` 인자) 출처 — 위 "맞춰 볼 것" 을 정적으로 닫으려면.

### PE_emulator 쪽 (오케스트레이터가 준 줄 + 결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음. 고칠지는 사용자 결정)

- `ext/syscall/syscall_layer_cpp/src/namespace.cpp:31` `\SystemRoot → \Device\BootDevice\Windows` — **모양 맞음**.
- `namespace.cpp:34` `\Device\BootDevice → \Device\HarddiskVolume3` — **처음부터 reassign 뒤 모양** (test.exe 시동은 reassign 뒤라 맞는 쪽). reassign 앞 (`\ArcName\…` 경유) 풀이는 안 둔다. 부팅을 흉내 내지 않으면 문제없다 — 앞 모양을 보일지는 오케스트레이터 · 사용자 결정.
- `namespace.cpp:21` 디렉터리 목록에 `\Device` **있음**, `\ArcName` **없음**. `\Device\OSDataDevice` · `\OSDataRoot` **없음** (결과서가 소스 · 시험에서 `OSData` · `ArcName` 글을 못 찾음). test.exe 시동 길이 이 이름들을 읽는지 **확인 필요**.
- 이름 공간 `Entry` (링크 여부 · 이름 · 대상) 에 **영구 표시 · SD 칸이 없다** — 진짜는 속성 `0x50` (영구 · 대소문자 무시) · SD `SePublicDefaultUnrestrictedSd` (DACL 허용 넷), reassign 링크는 `0x250` · SD 칸 0. 보안 묻기 · 지우기 시험이 이 값을 보는지 **확인 필요**.

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntkrnlmp pdb 줄에 run119, 근거 로그 run119 한 줄), 1장 그림 메모 (run119 — 그림 안 바꿈), 2.1 머리 · 부팅 준비 줄 (`\SystemRoot` 하위 줄), 3장 새 절 "부팅 때 nt 가 만드는 `\SystemRoot` 링크", 4장 run48 줄 (`\SystemRoot` 공백 해결됨 → run119) · 새 줄 넷 (해결 · 동적 · 정적 · PE_emulator), 5장 근거 run 표 · 03_sprint 요청서 번호 표.
- 짐 지도 (`baggage.md`): 기준 run119 줄 · 판 줄, 쓰는 말 단계 `ⒷT` · `ⒷT2`, 1장 run119 메모, 2장 아래 (run119) 한 줄 (**숫자 그대로**), 3장 줄임 표 둘 (`ⒷT…` · `ⒷT2…`), 5장 `Ⓑ` 4번 문장 보탬 · 새 10번, 7.6 머리 한 줄 · 7.6.2 칸 `+0x10` 메모 한 줄 · **새 7.6.16 (9줄)**, 7.11 정적 공백 닫힘, 새 7.50, 8장.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (정적만). 결과서: 스크립트 문제 없음.
- 읽을 때 주의 (결과서 그대로): `IopCreateArcName` 의 `IoCreateSymbolicLink` 는 Ghidra 가 떼어 놓은 조각 (`FUN_140b44c9e` · `FUN_140b44da4`) 안에 있다 — 호출 그래프를 거꾸로 따라가면 주인이 안 보인다 (run115 조각 메모와 같은 꼴).

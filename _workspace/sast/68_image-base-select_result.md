# 68 image-base-select — 정적 분석 결과

- 요청서: `_workspace/sast/68_image-base-select.md` (Q139a, run73 예정, PE_emulator 요청 23-3)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID와 같음). **analyze 다시 안 함** (`.gpr` · `_index.tsv` · analyze 로그 `SetPdb:` 줄 모두 있음). 다른 사용자 Ghidra 프로세스 없음 (`pgrep` 확인).
- 이번에 새로 뽑은 것 (depth 0, `export` 3번): `MiGetSystemAddressForImage` · `MiRelocateImageAgain` · `MiObtainRelocationBits` · `MiUpdateImageSystemWideBitmaps` · `MmLoadSystemImageEx` → `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`.
- 누가 부르나 표 (`refs`, 6번): `ms/disassemble/ntoskrnl/_refs/{MiRelocateImage,MiSelectImageBase,MiRelocateImageAgain,MiGetSystemAddressForImage,MiValidateExistingImage,0xb3f3dc}.tsv`.
- 있던 것 그대로 읽음 (다시 안 뽑음): `MiCreateNewSection` · `MiRelocateImage` · `MiSelectImageBase` · `MiInitializeCreateSectionPacket` · `MiCreateImageOrDataSection` · `MiShareExistingControlArea` · `MiValidateExistingImage` · `MiCreateSectionCommon` · `NtCreateSection` · ntdll `LdrpMapDllNtFileName` · `LdrpProcessMappedModule`.
- 버전 확인 (이 모듈은 이미 분석됨, 그래도 대조): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv`).
- 스크립트 문제: 없음.
- 교차 확인 방법 (레포에는 안 남김): 점프 목적지를 "목적지 − 함수 시작" 으로 세어 `.asm` 을 줄 단위 흐름 그래프로 읽고, 레지스터 값이 어느 줄에서 만들어져 어느 줄에 닿는지 (정의 → 사용) 를 끝까지 따라가는 임시 스크립트를 썼다 (`/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/6efa2847-7e1a-4305-bdd8-c7034320cbfe/scratchpad/rd.py`, `rd2.py`). 사람 눈으로 본 점프 표와 둘이 같은 답을 냈다. Ghidra `.c` (`MiCreateNewSection.c` 249·296행 `(uVar12 >> 0x1b) & 2`) 도 같다.

## 한 줄 결론 (PE_emulator 용)

- **`MiCreateNewSection` 이 `MiRelocateImage` 에 주는 여섯째 인자의 비트 0 은 두 부르는 자리 모두에서 늘 0 이다** (`+0x391 AND ESI,2` 하나로 만든 값만 두 자리에 닿는다). 값은 0 또는 2.
- 그래서 test.exe 의 dll1·dll2 는 `MiSelectImageBase` 가 헤더 `ImageBase` (`0x180000000`) 를 쓰는 갈래를 안 타고, **DLL 이므로 전역 비트맵에서 파일마다 다른 칸**을 받는다. 그 주소가 `_SEGMENT.BasedAddress` 에 들어간다. 헤더 값이 서로 같아도 상관없다.
- 기대값: 그 주소 범위가 test.exe 안에서 비어 있으면 `NtMapViewOfSection` 은 `0`. 범위가 차 있을 때만 `0x40000003` → 재시도 길 (run72). 어느 쪽인지는 **동적** (Q104 `ntdll!LdrpMinimalMapModule +0x179` 의 `eax`).

## 질문별 답 [정적]

### 1. `MiCreateNewSection` → `MiRelocateImage` 여섯째 인자 (정적으로 닫힘)

**답: 비트 0 은 늘 0 이다.** (예)

**인자 자리 맞추기** (두 부르는 자리 같음):

| 인자 | 어디서 | 값 |
|---|---|---|
| 1 | `+0x3c4` / `+0x684` `MOV RCX,[RSP+0x60]` | 세그먼트 `_SEGMENT*` (`MiCreateImageFileMap` 이 `+0x203 LEA RCX,[RSP+0x60]` 로 받아 채움; `+0x114 ~ +0x122` 에서 둘째 인자 출력 칸에 넣는 값이고 `MiCreateImageOrDataSection +0x394 MOV RCX,[RAX]` 가 그 앞 8바이트(컨트롤 영역)를 패킷 `+0x40` 에 둠) |
| 2 | `+0x3c0` / `+0x680` `LEA RDX,[RBP-0x70]` | 지역 버퍼 (맨 앞 `+0x48 memset` 으로 0x70바이트 지움) |
| 3 | `+0x3bb` / `+0x67b` `MOV R8D,[RSP+0x58]` | |
| 4 | `+0x3c9` / `+0x689` `MOV R9D,R12D` | R12D = `(플래그 >> 5) & 1` (`+0x384/+0x387/+0x38e`) |
| 5 | `+0x3d0` / `+0x690` `MOV [RSP+0x20],RAX` | 원래 기준 주소 (`-1` 또는 `FsRtlGetDirectImageOriginalBase` 결과) |
| **6** | **`+0x3cc` / `+0x68c` `MOV dword ptr [RSP+0x28],ESI`** | **ESI** |

- 부르는 줄: `+0x3d5/+0x3da`, `+0x695/+0x69a` (`_calls.tsv` 와 같음). 여섯째 인자 칸이 `[RSP+0x28]` 인 것은 받는 쪽으로도 맞춰 보았다: `MiRelocateImage +0x67 MOV EBX,[RSP+0x188]` (받는 쪽에서 `+0x188` = 부르는 쪽 `+0x28`, 다섯째는 `+0x180` = `+0x20` 으로 `+0x33e` 에서 읽음).

**ESI 의 출처 (두 자리에 닿는 정의는 이 두 줄 하나로 이어진 사슬뿐)**

| 줄 | 명령 | 뜻 |
|---|---|---|
| `+0x31` | `MOV RBX,RCX` | RBX = 첫째 인자 = 섹션 만들기 상태 덩어리 (패킷). **PDB 에 이 형의 이름이 없다** (공개 PDB 에 없음, `pdb_struct --list` 로 찾음). 칸 이름은 `MiInitializeCreateSectionPacket` 쓰는 모양으로 읽은 것 `[추측]` |
| `+0x361` | `MOV ESI,dword ptr [RBX]` | ESI = 패킷 맨 앞 4바이트 (플래그 모음). 이 칸에서 `+0x31` 뒤 RBX 는 바뀌지 않는다 |
| `+0x38b` | `SHR ESI,0x1b` | |
| `+0x391` | `AND ESI,0x2` | **ESI = 플래그 비트 28 만 남김 → 0 또는 2.** 비트 27 (이동 뒤 비트 0 자리) 은 여기서 버려진다 |

**요청서의 짐작 줄 4개 (`+0x38b` · `+0x391` · `+0x3cc` · `+0x68c`) 는 모두 맞았다.** 보탠 것: ESI 의 시작점은 `+0x361` 이고, 같은 곳에서 R12D (`+0x384 MOV R12D,ESI`, `+0x387 SHR 5`, `+0x38e AND R12D,R8D`, R8D=1 은 `+0x355`) 도 갈라져 나가 네 번째 인자가 된다.

**`+0x391` 뒤 ESI 를 바꾸는 줄이 두 자리에 닿나 — 아니오.**
- 함수 안에서 ESI 를 쓰는 줄 (정의) 은 `+0x2a` · `+0xa8` · `+0x169` · `+0x1ac`(POP, 끝) · `+0x1b0` · `+0x24c` · `+0x29d` · `+0x29f` · `+0x2a6` · `+0x361` · `+0x38b` · `+0x391` · `+0x404` · `+0x40b` · `+0x431` · `+0x46d` · `+0x5e5` · 조각 `FUN_140b23f93 +0x4` 의 18개.
- 이 중 **`+0x3cc` 와 `+0x68c` 에 닿는 것은 `+0x391` 하나**다. 앞의 것들(`+0x2a` ~ `+0x2a6`, `+0x5e5`)은 `+0x361` 에서 어느 길로 와도 덮이고, 뒤의 것들(`+0x404` ~ `+0x46d`, 조각 `FUN_140b23f93 +0x4`) 은 첫째 호출이 끝난 뒤 MDL 정리 고리 안이라 `+0x462 → MiReleaseImageSection → +0x470 JMP +0x17e` 로 함수를 끝낸다 (부르는 자리로 되돌아가는 길 없음).
- 호출(`CALL`) 사이에서 ESI 는 안 바뀐다고 본다 (x64 규약상 RSI 는 호출받는 쪽이 지켜 줌, 이 함수도 `+0x6 PUSH RSI` 로 지킴) `[정적: 규약]`.

**두 부르는 자리에 들어오는 모든 길 — 착지점 표** (목적지 − 함수 시작, `.asm` 점프 줄로 다시 셈)

| 착지점 | 어디서 오나 | `+0x391` 뒤인가 |
|---|---|---|
| `+0x37f` | `+0x375` 에서 이어짐 · `+0x493 JZ` · `+0x49c JMP` | 앞 (`+0x38b`·`+0x391` 으로 이어짐) |
| `+0x39f` | `+0x399 JNZ` 안 뛰고 이어짐 · `+0x651 JZ` · `+0x65c JZ` (둘 다 `+0x633` 갈래, `FsRtlGetDirectImageOriginalBase` 오류 `0xc00000bb`/`0xc0000010`) | 뒤 |
| `+0x3a8` | `+0x3a3` 에서 이어짐 · 조각 `FUN_140b23ed3 +0x2f JMP` (= `+0x1b4a32`, `+0x646 JNS` 갈래) | 뒤 |
| `+0x3bb` | `+0x3b6` 에서 이어짐 · 조각 `FUN_140b23ed3 +0xbb JMP` (= `+0x1b4abe`, 아래 설명) | 뒤 |
| `+0x671` (둘째 자리 입구) | `+0x3b0 JNZ` 하나뿐 | 뒤 |
| `+0x38b` ~ `+0x394` 한가운데 | **밖에서 뛰어드는 점프 없음** | — |

- `+0x361` 에는 `+0x35b JNZ → +0x611` 를 거쳐 `+0x625 JMP` 로, 또는 그냥 이어서 들어온다. `+0x375` 는 `+0x62e JMP` (← `+0x365`·`+0x478`·`+0x488`) 와 이어짐으로, 모두 `+0x361` 뒤다.
- **둘째 자리(`+0x695`)로 가는 법**: `+0x3a8 MOV ECX,6 / +0x3ad TEST CL,R13B / +0x3b0 JNZ → +0x671` (R13B 의 비트 1·2 중 하나가 켜졌을 때). 거기서 `+0x671 CMP RAX,-1 / +0x675 JZ` 이면 둘째 호출을 건너뛴다 (조각 `FUN_140b23ed3 +0x4b` 로).
- **첫째 자리로 되돌아오는 길** (`+0x3bb`): `+0x675 JZ` 로 둘째 호출을 건너뛴 경우에만 `[RSP+0x68]` 이 0 으로 남아, 조각 `FUN_140b23ed3 +0xab CMP [RSP+0x68],0` 이 0 → `+0xb6/+0xbb` 로 `+0x3bb` 에 돌아가 첫째 호출 (`+0x3d5`) 을 다시 한다. 이때도 ESI 는 `+0x391` 값 그대로 (조각 안에 ESI 를 쓰는 줄 없음).
- 이 모든 것이 위 임시 스크립트의 "정의 → 사용" 결과와 같다: `+0x3cc`, `+0x68c` 에 닿는 ESI 정의 = `{+0x391}`, `+0x391` 에 닿는 정의 = `{+0x38b}`, `+0x38b` 에 닿는 정의 = `{+0x361}`. 읽지 못한 점프 목적지 0개.

**플래그 비트 28 은 어디서 켜지나** (참고, 비트 0 이 늘 0 이라는 결론과는 무관)
- `MiInitializeCreateSectionPacket +0x9d TEST SIL,0x40 / +0xa1 JNZ → +0x306 BTS dword ptr [RBX],0x1c`. 즉 그 함수의 여섯째 인자에 `0x40` 이 있으면 패킷 비트 28 이 켜진다. 그 인자는 `MiCreateSection` (`+0xb0 MOV EAX,[RSP+0x1a8]` / `+0xb7 MOV [RSP+0x28],EAX`) · `MmCreateSectionEx` 가 받은 여섯째 인자를 그대로 넘긴 것. **누가 `0x40` 을 주는지는 안 따라감** `[공백]`.
- 옆 비트: 비트 27 은 `MiInitializeCreateSectionPacket +0x373 BTS [RBX],0x1b` (`Feature_TrustedLaunchHosts…` 가 켜졌고 인자 `0x20` 일 때) 인데 `AND ESI,2` 에 걸려 이 값에는 안 들어간다.
- `MiCreateImageOrDataSection` · `MiCreateNewSection` 안에서 이 4바이트 값을 `OR/AND/BTS` 로 건드리는 줄을 훑어 보면 비트 1·2·4·11·13·22 뿐이다 — 비트 27·28 은 안 건드림. (어차피 비트 0 이 늘 0 이라는 결론은 `AND ESI,2` 하나로 서므로 이 훑기에 기대지 않는다.)

### 2. `MiRelocateImage` 가 여섯째 인자를 쓰는 법 (정적으로 닫힘)

**답: 두 줄에서 읽는다.** `.c` (`param_6`) 와 같다.

| 줄 | 명령 | 뜻 |
|---|---|---|
| `+0x67` | `MOV EBX,dword ptr [RSP+0x188]` | 여섯째 인자 읽기 |
| `+0x6e/+0x74/+0x77/+0x7a` | `MOV R11D,1` · `AND EBX,R11D` · `SHL EBX,8` · `ADD EBX,2` | **EBX = (인자 & 1) × 0x100 + 2** (`.c` 63행 `uVar12 = (param_6 & 1) * 0x100 + 2`) |
| `+0x2b0/+0x2b6` | `CMP EBX,0x100` · `SETNC AL` | AL = 인자 비트 0 |
| `+0x2b9/+0x2bc` | `MOV R9D,EAX` · `OR R9D,2` | |
| `+0x2c0/+0x2c8` | `TEST byte ptr [RSP+0x188],2` · `CMOVZ R9D,EAX` | 인자 비트 1 이 0 이면 `2` 를 뺌 |
| `+0x2cc ~ +0x2de` | `LEA RAX,[RSP+0x58]` → 5번째 `[RSP+0x20]`, `MOV R8D,[RSP+0x38]` (= `MiRelocateImage` 의 4번째), `MOV RDX,R14`, `MOV RCX,[RSP+0x90]` | |
| `+0x2e6/+0x2eb` | `CALL MiSelectImageBase` | |

- **`MiSelectImageBase` 의 넷째 인자 (R9D) = 여섯째 인자 `& 3`.** 비트 0 = 비트 0, 비트 1 = 비트 1. 즉 run72 결과서의 "넷째 인자 비트 0 = `MiRelocateImage` 여섯째 `& 1`" 이 `.asm` 으로도 맞다.
- `+0x2b0` 까지 EBX 비트 8 을 지우는 줄은 없다 (EBX 를 쓰는 줄은 `+0x7a` · `+0x229 OR 1` · `+0x644 OR 0x40` · `+0x67d AND ~2` 가 닿고 모두 비트 8 은 그대로).
- `MiSelectImageBase` 안에서 이 인자는 R10D (`+0x29 MOV R10D,R9D`) 로만 쓰이고, 읽는 곳은 **`+0xd6 TEST DL,R10B`** (DL = 1: `+0xcf OR RBP,-1`, `+0xd3 LEA EDX,[RBP+2]`) 와 **`+0x10b TEST R10B,2`** 둘뿐 (R10 정의는 `+0x29` 하나).
  - 비트 0 켜짐 (`+0xd9 JNZ → +0x2b6`): `+0x2ba MOV RDI,R13` (R13 = `+0xcb MOV R13,[R14+0x20]` = 세그먼트 `BasedAddress`, 처음 값은 헤더 `ImageBase`) → `+0x2bf JMP → +0x1d4` → `+0x215 MOV [RAX],RDI` 로 출력. 비트맵 안 만짐.
  - 비트 1 켜짐: `+0x10b TEST R10B,2 / +0x10f SETZ AL` 이 거짓이 되어 **EXE 도 `+0x114 JNZ → +0x1df` 를 안 타고 비트맵 갈래**로 간다. DLL 은 비트 1 과 상관없이 비트맵 갈래.
- 보태기 (비트 0 의 뜻): 같은 EBX 비트 8 이 조각 `FUN_140b24658 +0xd6 ~ +0xe6` (= `MiRelocateImage +0x1b12ea ~ +0x1b12fa`) 에서 `EBX & 0x188 == 0x100` 이면 재배치 걷기 (`MiWalkEntireImage`) 를 건너뛰게 하고, `+0x139 BT EBX,8 / +0x13d JC` (= `+0x1b134d/+0x1b1351`) 가 꺼져 있을 때만 `+0x143 OR byte ptr [R15+0x33],4` (= `+0x1b1357`, `_SECTION_IMAGE_INFORMATION.ImageFlags` 비트 2 `ImageDynamicallyRelocated`) 를 켠다. 읽은 대로 요약하면 **비트 0 = "기준 주소 고정(옮기지 않음)", 0 = "옮겨질 수 있음 + 옮겼다고 표시"** `[정적: 흐름으로 읽음, 이름은 PDB 비트 이름]`.

### 3. `MiRelocateImage`·`MiSelectImageBase` 를 부르는 다른 곳 (정적으로 닫힘, 한계 아래)

`_calls.tsv` 는 이미 뽑은 함수만 있어서 전부가 아니다. 그래서 Ghidra 참조 (`refs`) 로 전부 찾았다. 함수 주소를 담은 표 (`ptr`) 로 부르는 곳은 둘 다 **0곳**, 나머지 참조는 `.pdata`/UNWIND 데이터뿐.

| 대상 | 부르는 곳 (call/ret) | 여섯째(넷째) 인자의 비트 0 |
|---|---|---|
| `MiRelocateImage` (rva `0x973444`) | `MiCreateNewSection +0x3d5/+0x3da` | **늘 0** (위 1번) |
| | `MiCreateNewSection +0x695/+0x69a` | **늘 0** |
| | **`MiGetSystemAddressForImage` 조각 `FUN_140b3f3dc +0x3ae/+0x3b3`** (= `MiGetSystemAddressForImage +0xc6e6e/+0xc6e73`) | **1** — `조각 +0x392 MOV [RSP+0x28],R15D`, R15D 정의는 `MiGetSystemAddressForImage +0x9c LEA R15D,[RBX+1]` 하나뿐이고 RBX 는 `+0x7b XOR EBX,EBX` → 1. 넷째 인자도 `조각 +0x39d MOV R9D,R15D` = 1, 다섯째 = -1 (`조각 +0x397`) |
| `MiSelectImageBase` (rva `0x972de0`) | `MiRelocateImage +0x2e6/+0x2eb` | 위 2번 (= 여섯째 `& 3`) |
| | `MiRelocateImageAgain +0xd3/+0xd8` | **늘 0** — `+0xbc NEG ESI` · `+0xc6 SBB R9D,R9D` · `+0xcc AND R9D,2` 라 값은 0 또는 2 |

- **비트 0 을 켜서 부르는 곳은 `MiGetSystemAddressForImage` 하나.** 그 함수의 부르는 곳은 `MmLoadSystemImageEx +0x202/+0x207` 하나뿐 → **커널 드라이버·시스템 이미지를 시스템 영역에 올리는 길**이다. 이미지 섹션 만들기 길 (`NtCreateSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection`) 이 아니다. test.exe 의 dll1·dll2 길과 무관.
  - 이 호출에 닿는 조건 (이름·조건만): `MiGetSystemAddressForImage +0xa3 JZ` (컨트롤 영역 `+0x60` 이 가리키는 블록의 `+0x20` 칸 = 재배치 정보가 아직 0) → `+0x13c MOV R8D,EBX` (0) → `+0x146 JZ → +0x192` (`MiMapImageInSystemSpace`, ret `+0x1a4`) → `+0x1ae JMP → FUN_140b3f3dc` 안 `+0x3ae` 의 호출. R8D 가 1 이면 대신 `+0x14a MiSelectSystemImageAddress` 갈래.
- `MiRelocateImageAgain` 의 부르는 곳은 `MiValidateExistingImage +0x51/+0x56` 하나, 그 부르는 곳은 `MiShareExistingControlArea +0xf4/+0xf9` (`+0xe8 TEST [RBX+0x10],0x1000000` = `SEC_IMAGE` 일 때), 그 부르는 곳은 `MiCreateImageOrDataSection +0x1d0/+0x1d5`. (= **이미 컨트롤 영역이 있는 파일을 다시 열 때의 길**, 아래 4번.)
- 지도 `callers_total`: `MiRelocateImage` 2 · `MiSelectImageBase` 2 (`_nodes.tsv`) 로 위 표와 함수 수가 같다.
- 한계: Ghidra 가 코드로 안 읽은 곳과 레지스터 간접 호출은 참조에 안 나온다. 함수 주소를 담은 표는 `ptr` 검색으로 0곳을 확인했다.

### 4. `MiSelectImageBase` 비트 0 꺼짐 갈래 — 칸, 파일마다 다른가, `BasedAddress` (정적으로 닫힘, 값은 동적)

**4-1. 갈래 (dll1·dll2, 64비트 DLL, 헤더 `ImageBase` `0x180000000` > 4GB) — run72 결과서 그대로, 줄만 보탬**
- `+0x44 ~ +0x4f`: `CMP [R14+0x20],0xffffffff / SETBE SIL` → 헤더 기준 주소가 4GB 이하가 아니므로 ESI = 0 ("네이티브 높은 구역"). `+0x23 BT word [RCX+0xc],0xf` (`_SEGMENT_FLAGS.Binary32`) 와 `+0x3a TEST byte [RCX+0x33],0x10` (`_SECTION_IMAGE_INFORMATION.ImageFlags` 의 `BaseBelow4gb`) 는 둘 다 꺼짐으로 본다 (64비트 DLL, 기준 주소 > 4GB) `[추측: 실제 값은 안 봄]`.
- `+0xfc ~ +0x114`: DLL 이므로 (`ImageCharacteristics & 0x2000`) `+0x114 JNZ → +0x1df` 안 뜀.
- `+0x126 CMP [0x140fc5210],EBX / +0x12c JNZ → +0x2df`: **이 값이 0 이 아니면 `MiImageCanUseHighOverflowArea` 갈래.** 값은 실행 때 `[공백]` (rva `0xfc5210`, `.asm` 주석은 `MmRegistryState+0x20`).
- `+0x140 ~ +0x154` 에서 ESI = 0 이라 `+0x15a` 로: `+0x15a MOV R8,[0x140e2d780]` (= `MiState.Sections.ImageBiasNative`, 찾기 시작 힌트) · `+0x161 LEA RBP,[0x140e2d770]` (= `ImageBitMapNative`) · `+0x168 MOV RAX,0x7fffffff0000` (꼭대기) · `+0x17a MOVZX EAX,R12W` (길이 = `(_SEGMENT.TotalNumberOfPtes(+0x8) + 0xf) >> 4` = `+0x8f`·`+0xe7`·`+0xeb`, 64KB 칸 수; dll1·dll2 는 `SizeOfImage 0x8c000` → 9칸) · `+0x18d RtlFindClearBitsEx` (ret `+0x192`, `+0x199 JZ` 실패면 `+0x2fa`) · `+0x1ab MiObtainRelocationBits` (ret `+0x1b0`, `+0x1b7 JZ` 실패면 `+0x2fa`) · `+0x1bd ~ +0x1d1`: **기준 주소 = `0x7fffffff0000 − ((시작 비트 + 길이) << 16)`** (위에서 아래로, `SUB RDI,RAX`).
  - 전역 이름 확인: `MiState` rva `0xe2cb80` + `Sections` (`_MI_SYSTEM_INFORMATION +0x1c0`) → `_MI_SECTION_STATE` 의 `+0xa28 RelocateBitmapsLock` = `0x140e2d768`, `+0xa30 ImageBitMapNative` = `0x140e2d770`, `+0xa40 ImageBiasNative` = `0x140e2d780`. `.asm` 주소와 산수가 맞는다 (`+0xbb0 TotalImageViewReservationSize` = `0x140e2d8f0` = `MiSelectImageBase +0xa1`, `+0xaf8 ImageFailureReason` = `0x140e2d838` = `MiCreateNewSection` 이 쓰는 실패 이유 칸).
- 비트맵이 꽉 찼으면 (`+0x199`/`+0x1b7 JZ` → `+0x2fa → +0x1df`) 다른 갈래 (`ExGenRandom` 등). 이때도 헤더 값은 아니다 (`[R15+0x5c]` 비트 23 이 이미 켜져 있으면 `+0x1f3 MOV RDI,[R14+0x20]` 로 헤더 값) — 실제로 탈 일이 드문 갈래 `[추측]`, 안 따라감.

**4-2. 이 칸이 파일(섹션)마다 다른가 — 예**
- `MiObtainRelocationBits` 는 `RelocateBitmapsLock` (`0x140e2d768`) 을 잡은 채 `+0x86 RtlFindClearBitsAndSetEx` (ret `+0x8b`) 로 **빈 칸 찾기와 켜기를 한 번에** 한다 (`+0x7d ~ +0x86`). 켜진 칸은 다른 파일이 못 받는다 → 동시에 살아 있는 서로 다른 이미지 파일은 겹치지 않는 칸을 받는다. dll1·dll2 는 다른 파일이므로 **서로 다른 기준 주소**.
- 고른 칸 (시작 비트·길이) 은 **컨트롤 영역에 적힌다**: `MiSelectImageBase +0x1d4 MOV [R15+0x58],EBP` · `+0x1d8 MOV word ptr [R15+0x5c],R12W` (R15 = `+0x1e MOV R15,[RCX]` = 첫째 인자 세그먼트의 `ControlArea`). 컨트롤 영역은 파일(이미지 섹션 오브젝트)당 하나다. PDB 는 `_CONTROL_AREA +0x58` 을 이름 없는 합집합 (`u2`) 으로 줘서 칸 이름은 못 붙인다.

**4-3. 같은 파일을 다시 열면 같은 칸인가 — 컨트롤 영역이 살아 있는 동안은 예**
- 이미 컨트롤 영역이 있는 파일 (`MiCreateImageOrDataSection +0x1d0 MiShareExistingControlArea`, `SEC_IMAGE` 일 때 `+0xf4 MiValidateExistingImage`) 은 `MiCreateNewSection` 을 안 타고 `MiValidateExistingImage +0x51/+0x56 MiRelocateImageAgain` 만 탄다.
- `MiRelocateImageAgain +0x80 TEST [RBX+0x5c],0x400000` / `+0x87 JNZ → +0x117` : **컨트롤 영역 `+0x5c` 비트 22 가 이미 켜져 있으면 다시 안 고르고 바로 끝낸다.** 이 비트는 처음 고른 직후 켜진다 (`MiRelocateImage` 조각 `FUN_140b24658 +0x68 BTS [R13+0x5c],0x16` = `+0x1b127c`, `MiRelocateImageAgain +0xde`). 뜻 = "이미 기준 주소를 골랐음" `[정적: 흐름으로 읽음, PDB 에 이름 없음]`.
- 컨트롤 영역이 사라졌다 다시 만들어질 때 (모든 보기가 닫힌 뒤) 는 비트맵에서 새로 고르므로 같은 칸이 나올지는 그때 비트맵 상태 — **동적**.

**4-4. 기준 주소가 `_SEGMENT.BasedAddress` 에 들어가는 줄 (한 줄)**
- **`MiRelocateImage` 조각 `FUN_140b24658 +0x55  MOV [RCX+0x20],RAX`** (= `MiRelocateImage +0x1b1269`). 바로 앞 `+0x48 MOV RAX,[RSP+0x58]` (= `MiSelectImageBase` 가 출력한 기준 주소, `MiSelectImageBase +0x215 MOV [RAX],RDI`), `+0x4d MOV RCX,[RSP+0x90]` (= `MiRelocateImage` 첫째 인자 = 세그먼트). `_SEGMENT +0x20 = BasedAddress` (PDB 확인). 이 조각에는 `+0x58e JNZ` · `+0x5ab JNS` (목적지 `+0x1b1240`) 로 성공 끝에서 들어온다.
- 같은 조각이 `+0x44 MOV [RSI+0x20],R14` (재배치 정보 블록을 컨트롤 영역 `+0x60` 이 가리키는 곳 `+0x20` 에), `+0x65 ADD [R15],RAX` (`TransferAddress += 새 기준 − 옛 기준`, flow.md 의 `+0x1b1271 ~ +0x1b1279` 와 같음) 도 한다.
- 옛 값 → 새 값: `MiSelectImageBase` 가 읽는 `[세그먼트+0x20]` (`+0xcb`) 는 헤더 `ImageBase`, 끝난 뒤 `MiRelocateImage` 가 거기에 고른 기준 주소를 덮는다.

### 5. 결론과 run44 P1b 대조 (기대값은 정적으로 닫힘, 실제 값은 동적)

- **기대값 [정적]**: dll1·dll2 는 둘 다 비트 0 = 0 (1번) + DLL (4-1) 이므로 헤더 `ImageBase` (`0x180000000`) 를 쓰는 `+0xd9 JNZ → +0x2b6` 갈래를 **안 타고**, 파일마다 서로 다른 칸을 받아 `_SEGMENT.BasedAddress` 에 둔다 (4-4). 그러므로 "둘째로 붙는 DLL 은 반드시 `0x40000003`" (run72 결과서 3-3 의 반대 가정) 은 **이 두 DLL 에는 해당 없다**. 그 갈래(헤더 값 그대로, 비트 0 = 1)는 커널 드라이버 로드 `MmLoadSystemImageEx` 길뿐이다 (3번).
- 그 한 줄을 PE_emulator 에: **"`MiCreateNewSection` 이 두 DLL 에 서로 다른 기준 주소를 줌 → 그 주소 범위가 test.exe 안에서 비어 있으면 `MiMapViewOfImageSection` 이 붙이고 `NtMapViewOfSection` 결과는 `0`."**
- **남는 동적 몫** (정적으로 못 정함):
  1. 그 범위가 프로세스에서 실제로 비었나. 차 있으면 `MiMapViewOfImageSection` 이 다른 곳에 붙이고 `0x40000003` (run72 3-2).
  2. 4-1 의 `MmRegistryState+0x20` (rva `0xfc5210`) 이 0 인가, 비트맵 찾기가 실패하지 않나.
  3. 같은 부팅에 이미 test.exe 를 돌렸다면 컨트롤 영역이 남아 `MiCreateNewSection`/`MiSelectImageBase` 를 아예 안 타고 `MiRelocateImageAgain +0x87 JNZ` 로 끝날 수 있다 (4-3). 이때 값은 먼저 고른 것.
- **run44 P1b 대조 (dll1 `DllBase` = `OriginalBase` = `0x7ffae12e0000`, dll2 `0x7ffadb950000`)**
  - **주소 모양은 맞는다 `[추측: 주소 모양으로만]`**: 둘 다 `0x180000000` 이 아니고, 서로 다르고, 64KB 로 떨어지는 `0x7fff…` 아래쪽 값. 4-1 의 "꼭대기 `0x7fffffff0000` 에서 64KB 칸씩 아래로" 와 어긋나지 않는다 (정확한 칸 번호는 모름).
  - **`OriginalBase` = `DllBase` 부분은 이번 요청으로는 못 가림 `[공백]`**: `OriginalBase` 는 ntdll `LdrpProcessMappedModule +0x90/+0x97` (`MOV RAX,[RSI+0x30]` → `MOV [RBX+0xf8],RAX`) 이 **메모리에 올라온 NT 헤더의 `ImageBase`** 를 읽은 값이다. 헤더 칸이 각자 주소로 고쳐져 있었다는 뜻인데, 고친 쪽이 커널인지 ntdll 재배치 (`0x40000003` 뒤 `LdrpRelocateImage`) 인지는 P1b 로 안 가려진다 (flow.md 3553행 서술 그대로). 커널이 헤더 `ImageBase` 를 쓰는 줄은 이번에 못 찾음 (`MiRelocateImage` 안에서 본 헤더 쪽 쓰기는 `TransferAddress` 더하기뿐).
- 샘플 헤더 값 (`ImageBase` 둘 다 `0x180000000`, `SizeOfImage 0x8c000`, `DYNAMIC_BASE`, 재배치 있음) 은 run72 결과서 3-1 값을 그대로 썼고 이번에 다시 읽지 않음.
- ntdll 쪽 확인 (인자 `AllocationAttributes` 가 `SEC_IMAGE` 하나인지): `LdrpMapDllNtFileName +0x14e MOV R13D,0x1000000` → `+0x17b MOV [RSP+0x28],R13D` → `+0x18f/+0x194 NtCreateSection`. 32비트 프로세스 (`+0x165 JNZ` `UseWOW64`) 만 `+0x4a7 MOV EAX,0x1100000` 갈래라 test.exe (64비트) 는 해당 없음. `MiRelocateImage` 의 넷째 인자 (= `MiSelectImageBase` 셋째 인자, 비트맵 갈래 앞의 `+0x9b JNZ → +0x28a` `MiBytesToMapSystemImage` 갈래를 켬) 는 패킷 비트 5 에서 오고 (`MiCreateNewSection +0x387/+0x38e`), 그 비트는 `MiInitializeCreateSectionPacket +0x326 OR EAX,0x20` (`+0xf2 BT EDI,0x14 / +0xf6 JC → +0x31b`) 이 **`AllocationAttributes` 비트 20 (`0x100000`) 이 켜지고 `0x1000000` 은 없을 때만** 켠다 (`+0xe9 CMP EAX,R9D / +0xec JZ → +0x30f` 로 둘 다 켜진 경우는 비트 20 을 지우고 비트 8 만 켬). ntdll 이 주는 `0x1000000` 만으로는 안 켜진다 → 셋째 인자 0. `[정적]` 은 ntdll 쪽 값과 이 줄까지다. 중간 길 (`NtCreateSection → MiCreateSectionCommon → MiCreateSection`) 에서 이 비트를 더하는 줄은 안 봤다 `[공백]`.

## 동적분석으로 확인해야 할 것

- (dast 요청 필요: Q104 VM 단계에 아래 1·2번을 얹음) 정적으로 못 정한 것은 위 5번의 1~3.
1. **맵 결과 값**: `ntdll!LdrpMinimalMapModule +0x179` (`NtMapViewOfSection` 돌아온 직후) `eax` — dll1·dll2·kernel32·kernelbase. 기대: dll1·dll2 `0` (이제 근거가 있음), 서로 다른 `DllBase`. (run72 와 같은 멈춤점, 이번 결과로 기대값의 근거가 `[추측]` → `[정적]` 로 올라감)
2. **기준 주소가 서로 다른지 커널에서도 확인 (선택)**: `nt!MiSelectImageBase +0x215` (`MOV [RAX],RDI`) 의 `rdi` = 고른 기준 주소, 직전 `+0x1d4`·`+0x1d8` 에서 `ebp` = 시작 비트, `r12w` = 칸 수. 호출한 쪽은 `nt!MiRelocateImage +0x2eb`. test.exe 일 때만 멈추게 조건. **같은 부팅에서 이미 돌렸다면 안 설 수 있음** (5번 3, Q61 과 같은 갈림). 안 서면 `nt!MiRelocateImageAgain +0x87` 쪽을 본다.
3. 사진 대용 (선택): `dt nt!_SEGMENT` 의 `BasedAddress` (`+0x20`) 가 dll1·dll2 서로 다른지, `_CONTROL_AREA +0x58/+0x5c` (시작 비트·길이·비트 22/23).
4. `dq nt+0xfc5210 L1` (`MmRegistryState+0x20`) — 0 이 아니면 `MiSelectImageBase +0x12c JNZ` 갈래.
5. `OriginalBase` = `DllBase` 인 이유 (헤더 `ImageBase` 를 누가 고쳤나) — 정적으로 더 닫고 싶으면 새 요청서: `MiRelocateImage` 아래 `MiWalkEntireImage` · `MiScanRelocationPage` 에서 헤더 `+0x30` 쓰기 찾기 + ntdll `LdrpRelocateImage`. 이번 Q139a 범위 밖이라 안 함.

## run72 문서·flow·baggage 에 고칠 줄 목록 (고치는 것은 flow-writer, 여기서는 안 고침)

- `runs/03_sprint/run72-pool-work-post.md` **16행**: "그걸 가르는 게 … 안 닫혔다" → 닫힘 (비트 0 늘 0). **233~234행**: "호출자 `MiCreateNewSection` 가 무엇을 주는지는 안 읽음 `[공백]`" → `+0x391 AND ESI,2` 라 비트 0 늘 0. **281행**: "닫으면 위 동적 1 의 기대값이 정적으로 정해진다" → 닫힘, 기대 `0` 의 근거가 `[정적]` 으로.
- `runs/03_sprint/flow.md` **3551~3552행**: "그 비트를 주는 `MiCreateNewSection` 줄은 안 읽었다 `[공백]`" → 닫힘. "헤더 그대로 갈래면 둘째로 붙는 DLL 은 반드시 `0x40000003`" 은 이 두 DLL 에는 안 맞는 갈래 (그 갈래는 `MmLoadSystemImageEx` 길뿐). **3553행** `OriginalBase` 문장은 그대로 맞음 (이번에도 못 가림). **1270~1301행** `nt!MiCreateNewSection` 절에 "`MiRelocateImage` 여섯째 인자 = `(플래그 >> 27) & 2`, 비트 0 늘 0" 한 줄, `MiRelocateImage` 절에 "`+0x1b1269` 가 `_SEGMENT.BasedAddress` 를 씀" 한 줄 보태기.
- `runs/03_sprint/baggage.md` **2755행**: "정적으로 남은 것 ① `MiCreateNewSection` 이 … 여섯째 인자 `& 1`" → 닫힘. 세그먼트 `BasedAddress` (`+0x20`) 쓴 자리 = `MiRelocateImage` 조각 `+0x1b1269` (짐 지도 칸이 있으면).
- `_workspace/sast/67_pool-work-post_result.md` **196행·201~202행**: 고치지 말고 "정정: sast/68 에서 닫힘" 한 줄만 붙이는 쪽을 권함 (결과서는 그때 기록).
- `_workspace/plan.md` **Q139 / Q139a (194~195행)**, 결정 기록 **477행**: 요청서에 적힌 오케스트레이터 짐작 줄 4개는 모두 맞았음, (a) 끝. 남는 Q139 (b)~(f) 그대로.

## 판정

분석성공 — 1~4번은 정적으로 닫혔고 (비트 0 늘 0, 읽는 줄, 다른 부르는 곳, 비트맵 갈래·`BasedAddress` 줄), 5번은 기대값 `0` 을 코드로 세웠으나 실제 값(범위 점유)은 Q104 동적으로 남음. 버전 대조 일치.

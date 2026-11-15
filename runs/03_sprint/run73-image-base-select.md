# 2026-11-07 run73 — 이미지 기준 주소 고르기 (Q139a, PE_emulator 요청 23 의 3번): `MiCreateNewSection` 은 `MiRelocateImage` 여섯째 인자의 **비트 0 을 늘 0** 으로 준다 (`+0x391 AND ESI,2`). 그래서 dll1·dll2 는 **파일마다 다른 기준 주소** (전역 비트맵 칸) 를 받는다. 맵 결과 기대 `0` 의 근거가 `[추측]` → `[정적]`

> 정정 (run103, sast/101): 아래 "같은 파일 컨트롤 영역이 살아 있으면 `MiRelocateImageAgain +0x87 JNZ` 로 먼저 고른 주소를 그대로 쓴다" 는 **반만 맞다.** 그대로 쓰는 건 컨트롤 영역 `+0x5c` 비트 22 (`ImageActive`) 가 켜져 있을 때뿐이다. 뷰 · 섹션 참조가 다 없어지면 `MiCheckControlArea +0xc9 → MiImageUnused +0x83 BTR [RBX+0x5c],0x16` 이 비트를 끄고 (컨트롤 영역은 살아 있어도), 다음 공유 길은 `+0xd3 MiSelectImageBase` 로 **다시 고른다** (값이 다르면 `+0x17e MiSwitchBaseAddress`). `runs/03_sprint/run103-image-share.md` "정정".

## PE_emulator 쪽에서 쓸 것

- **한 줄**: `MiCreateNewSection` 은 여섯째 인자 비트 0 을 늘 0 으로 줌 (`AND ESI,2`) → 커널이 dll1·dll2 에 파일마다 다른 기준 주소 (전역 비트맵) 를 줌 → 그 범위가 test.exe 에서 비어 있으면 맵 결과 `0`, 재시도 길 안 탐 `[정적]`.
- 비트 0 을 켜서 부르는 곳은 **`MiGetSystemAddressForImage` 하나** (← `MmLoadSystemImageEx +0x202`, 커널 드라이버·시스템 이미지를 올리는 길). test.exe 의 DLL 길이 아니다 `[정적]`.
- 그래서 run72 의 "헤더 그대로 갈래면 **둘째로 붙는 DLL 은 반드시 `0x40000003`**" 은 **이 두 DLL 에는 해당 없는 갈래**다.
- 헤더 `ImageBase` 가 둘 다 `0x180000000` 이어도 상관없다. 커널은 헤더 값을 안 쓰고 비트맵에서 따로 고른다.
- 아직 실제 값은 아니다. 그 범위가 실제로 비었나, 비트맵 찾기가 실패하지 않나는 동적 (Q104 VM 단계, 아래 "아직 확인 필요").

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID 와 같음). 다시 analyze 안 함
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 둘 다 `_calls.tsv` 와 일치

## 목적

- PE_emulator 요청 23 (3): dll1·dll2 이미지 맵 결과가 `0` 인가 `0x40000003` 인가 — 그 갈림을 정하는 `MiSelectImageBase` 넷째 인자 비트 0 (= `MiRelocateImage` 여섯째 인자 `& 1`) 을 `MiCreateNewSection` 이 어떻게 주는지 정적으로 닫는다 (run72 가 남긴 `[공백]`).

## 방법

- 정적: `_workspace/sast/68_image-base-select.md` — ntoskrnl
  - 새로 뽑은 것 (depth 0): `MiGetSystemAddressForImage` · `MiRelocateImageAgain` · `MiObtainRelocationBits` · `MiUpdateImageSystemWideBitmaps` · `MmLoadSystemImageEx`
  - 있던 것 읽기만: `MiCreateNewSection` · `MiRelocateImage` · `MiSelectImageBase` · `MiInitializeCreateSectionPacket` · `MiCreateImageOrDataSection` · `MiShareExistingControlArea` · `MiValidateExistingImage` · `MiCreateSectionCommon` · `NtCreateSection`, ntdll `LdrpMapDllNtFileName` · `LdrpProcessMappedModule`
  - 누가 부르나 (`refs`): `ms/disassemble/ntoskrnl/_refs/{MiRelocateImage,MiSelectImageBase,MiRelocateImageAgain,MiGetSystemAddressForImage,MiValidateExistingImage,0xb3f3dc}.tsv`
  - 레지스터 값 따라가기는 점프 목적지를 "목적지 − 함수 시작" 으로 세어 사람 눈 + 임시 스크립트 둘로 했고, 같은 답이 나왔다 (결과서)
- 결과서: `_workspace/sast/68_image-base-select_result.md` (**분석성공**)
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `MiCreateNewSection +0x361` · `+0x384` · `+0x38b` · `+0x391` · `+0x3cc` · `+0x68c` · `+0x3d5/+0x3da` · `+0x695/+0x69a`, `MiRelocateImage +0x67` · `+0x2c0` · `+0x2e6/+0x2eb`
- flow-writer 도 `.asm` 으로 다시 봤다: 위 줄 + `MiRelocateImage +0x74/+0x77/+0x7a` · `+0x2b0/+0x2b6` · `+0x2c8` · 조각 `FUN_140b24658 +0x55` (= `+0x1b1269`), `MiSelectImageBase +0xd6/+0xd9` · `+0x1d4` · `+0x1d8` · `+0x215` · `+0x2ba`, `MiRelocateImageAgain +0x80/+0x87` · `+0xbc` · `+0xc6` · `+0xcc` · `+0xd3/+0xd8`, `MiGetSystemAddressForImage +0x7b` · `+0x9c` · `+0x1ae` · 조각 `FUN_140b3f3dc +0x392` · `+0x397` · `+0x39d` · `+0x3ae/+0x3b3`, `MmLoadSystemImageEx +0x202`, `_refs/MiRelocateImage.tsv` (CALL 3줄) · `_refs/MiGetSystemAddressForImage.tsv` (CALL 1줄)

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/*.asm`, `_refs`)

| caller | call | ret | callee | 여섯째(넷째) 인자 비트 0 | 표시 |
|---|---|---|---|---|---|
| `nt!MiCreateNewSection` | `+0x3d5` | `+0x3da` | `nt!MiRelocateImage` | **늘 0** | [정적] |
| `nt!MiCreateNewSection` | `+0x695` | `+0x69a` | `nt!MiRelocateImage` | **늘 0** | [정적] |
| `nt!MiGetSystemAddressForImage` 조각 `FUN_140b3f3dc` | `+0x3ae` (= `+0xc6e6e`) | `+0x3b3` (= `+0xc6e73`) | `nt!MiRelocateImage` | **1** | [정적] |
| `nt!MmLoadSystemImageEx` | `+0x202` | `+0x207` | `nt!MiGetSystemAddressForImage` | — (유일한 부르는 곳) | [정적] |
| `nt!MiRelocateImage` | `+0x2e6` | `+0x2eb` | `nt!MiSelectImageBase` | 여섯째 `& 3` 을 넷째로 | [정적] |
| `nt!MiRelocateImageAgain` | `+0xd3` | `+0xd8` | `nt!MiSelectImageBase` | **늘 0** (값 0 또는 2) | [정적] |
| `nt!MiValidateExistingImage` | `+0x51` | `+0x56` | `nt!MiRelocateImageAgain` | — | [정적] |
| `nt!MiShareExistingControlArea` | `+0xf4` | `+0xf9` | `nt!MiValidateExistingImage` | — (`SEC_IMAGE` 일 때) | [정적] |
| `nt!MiCreateImageOrDataSection` | `+0x1d0` | `+0x1d5` | `nt!MiShareExistingControlArea` | — | [정적] |
| `nt!MiSelectImageBase` | `+0x1ab` | `+0x1b0` | `nt!MiObtainRelocationBits` | — (비트맵 갈래) | [정적] |
| `nt!MiObtainRelocationBits` | `+0x86` | `+0x8b` | `RtlFindClearBitsAndSetEx` | — | [정적] |

- `MiRelocateImage`·`MiSelectImageBase` 를 함수 주소 표로 부르는 곳은 둘 다 0곳. 나머지 참조는 `.pdata`/UNWIND 뿐 (결과서 3번, `_refs`).
- 한계: Ghidra 가 코드로 안 읽은 곳과 레지스터 간접 호출은 참조에 안 나온다 (결과서).

### `MiCreateNewSection` → `MiRelocateImage` 여섯째 인자의 출처 (질문 1)

- 여섯째 인자 칸은 두 자리 모두 `[RSP+0x28]` — `+0x3cc` / `+0x68c MOV dword ptr [RSP+0x28],ESI` `[정적]`.
  - 받는 쪽에서도 맞다: `MiRelocateImage +0x67 MOV EBX,[RSP+0x188]` (받는 쪽 `+0x188` = 부르는 쪽 `+0x28`).
- ESI 가 만들어지는 사슬 (이 하나뿐) `[정적]`:

| 줄 | 명령 | 뜻 |
|---|---|---|
| `+0x31` | `MOV RBX,RCX` | RBX = 첫째 인자 = 섹션 만들기 상태 덩어리 (패킷). 공개 PDB 에 이 형 이름 없음 |
| `+0x361` | `MOV ESI,[RBX]` | 패킷 맨 앞 4바이트 (플래그 모음) |
| `+0x384` | `MOV R12D,ESI` | 같은 값에서 넷째 인자도 갈라져 나감 (`+0x387 SHR 5` · `+0x38e AND R12D,R8D` → 플래그 비트 5) |
| `+0x38b` | `SHR ESI,0x1b` | |
| `+0x391` | `AND ESI,0x2` | **플래그 비트 28 만 남김 → 값은 0 또는 2.** 비트 27 (옮긴 뒤 비트 0 자리) 은 여기서 버려진다 |

- `+0x391` 뒤에 ESI 를 바꾸는 줄이 `+0x3cc`·`+0x68c` 에 닿나 — **아니오** `[정적]` (결과서 1번 착지점 표).
  - 함수 안 ESI 정의 18곳 중 두 자리에 닿는 건 `+0x391` 하나. 뒤쪽 정의 (`+0x404` ~ `+0x46d`, 조각 `FUN_140b23f93 +0x4`) 는 첫째 호출 뒤 정리 고리라 `+0x470 JMP +0x17e` 로 함수를 끝낸다.
  - `+0x38b ~ +0x394` 한가운데로 밖에서 뛰어드는 점프는 없다.
  - 둘째 자리 (`+0x695`) 입구는 `+0x3b0 JNZ → +0x671` 하나. 둘째 호출을 건너뛴 경우 첫째 자리 `+0x3bb` 로 되돌아와도 ESI 는 `+0x391` 값 그대로.
  - 호출 사이 ESI 는 x64 규약상 지켜진다 (`+0x6 PUSH RSI`) `[정적: 규약]`.
- **결론: 비트 0 은 두 자리 모두 늘 0** `[정적]`.

### `MiRelocateImage` 가 여섯째 인자를 쓰는 법 (질문 2)

- `+0x67 MOV EBX,[RSP+0x188]` → `+0x74 AND EBX,R11D` (R11D = 1) · `+0x77 SHL EBX,8` · `+0x7a ADD EBX,2` → **EBX = (인자 & 1) × 0x100 + 2** `[정적]`.
- `+0x2b0 CMP EBX,0x100` · `+0x2b6 SETNC AL` (= 인자 비트 0) → `+0x2bc OR R9D,2` → `+0x2c0 TEST byte [RSP+0x188],2` · `+0x2c8 CMOVZ R9D,EAX` → `+0x2e6 CALL MiSelectImageBase` `[정적]`.
  - **`MiSelectImageBase` 넷째 인자 = 여섯째 인자 `& 3`.** run72 결과서의 "넷째 비트 0 = 여섯째 `& 1`" 이 `.asm` 으로도 맞다.
- `MiSelectImageBase` 안에서 이 인자를 읽는 곳은 `+0xd6 TEST DL,R10B` (DL = 1) 와 `+0x10b TEST R10B,2` 둘뿐 `[정적]`.
  - 비트 0 켜짐: `+0xd9 JNZ → +0x2b6` → `+0x2ba MOV RDI,R13` (R13 = 세그먼트 `BasedAddress`, 처음엔 헤더 `ImageBase`) → `+0x215` 로 출력. 비트맵 안 만짐.
  - 비트 1 켜짐: EXE 도 비트맵 갈래로. DLL 은 비트 1 과 상관없이 비트맵 갈래.
- 비트 0 의 뜻 (흐름으로 읽음) `[정적]`: 1 = "기준 주소 고정, 안 옮김" (조각 `+0x1b12ea ~ +0x1b12fa` 에서 재배치 걷기 건너뜀), 0 = "옮겨질 수 있음 + 옮겼다고 표시" (`+0x1b134d/+0x1b1351 BT EBX,8 / JC` 가 꺼져 있을 때만 `+0x1b1357` 이 `ImageFlags` 비트 2 `ImageDynamicallyRelocated` 를 켬). 비트 이름은 PDB.

### 비트 0 이 꺼졌을 때 — dll1·dll2 의 갈래 (질문 4)

- 64비트 DLL, 헤더 `ImageBase` `0x180000000` (> 4GB) 이면 `MiSelectImageBase` 는 (결과서 4-1) `[정적]`:
  - `+0xfc ~ +0x114` DLL 이라 EXE 갈래 (`+0x114 JNZ → +0x1df`) 안 탐.
  - `+0x126 CMP [0x140fc5210],EBX` / `+0x12c JNZ`: 이 전역 (`.asm` 주석 `MmRegistryState+0x20`) 이 0 이 아니면 `MiImageCanUseHighOverflowArea` 쪽. 실행 때 값 `[공백]`.
  - 0 이면 `+0x15a` 이후: 찾기 시작 힌트 `MiState.Sections.ImageBiasNative` (`0x140e2d780`) · 비트맵 `ImageBitMapNative` (`0x140e2d770`) · 꼭대기 `0x7fffffff0000` · 길이 = 64KB 칸 수 (dll1·dll2 `SizeOfImage 0x8c000` → 9칸) → `+0x18d RtlFindClearBitsEx` → `+0x1ab MiObtainRelocationBits` → **기준 주소 = `0x7fffffff0000 − ((시작 비트 + 길이) << 16)`** (위에서 아래로).
  - 전역 이름은 PDB `MiState` + `_MI_SYSTEM_INFORMATION` · `_MI_SECTION_STATE` 칸 오프셋으로 맞췄다 (결과서). `ImageFailureReason` (`0x140e2d838`) 은 flow.md 2.1 의 "이유 번호 전역 (rva `0xe2d838`)" 과 같은 칸.
- **파일마다 다른 칸** `[정적]`: `MiObtainRelocationBits` 가 `RelocateBitmapsLock` (`0x140e2d768`) 을 잡은 채 `+0x86 RtlFindClearBitsAndSetEx` 로 **찾기와 켜기를 한 번에** 한다. 켜진 칸은 다른 파일이 못 받는다 → dll1·dll2 는 서로 다른 기준 주소.
  - 비유: 주차장 빈 칸을 찾자마자 그 자리에 "예약" 표지를 세우는 것. 다음 차는 그 칸을 못 받는다.
- 고른 칸 (시작 비트·길이) 은 컨트롤 영역에 적힌다: `+0x1d4 MOV [R15+0x58],EBP` · `+0x1d8 MOV word [R15+0x5c],R12W` (R15 = 세그먼트의 `ControlArea`). PDB 는 `_CONTROL_AREA +0x58` 을 이름 없는 합집합 (`u2`) 으로 줘서 칸 이름은 못 붙인다.
- 비트맵이 꽉 찼을 때 (`+0x199`/`+0x1b7 JZ → +0x2fa`) 는 다른 갈래 (`ExGenRandom` 등) — 안 따라감, 탈 일이 드물 것 `[추측]`.

### 기준 주소가 `_SEGMENT.BasedAddress` 에 들어가는 줄

- **`MiRelocateImage` 조각 `FUN_140b24658 +0x55 MOV [RCX+0x20],RAX`** (= `MiRelocateImage +0x1b1269`) `[정적]`.
  - RAX = `MiSelectImageBase +0x215 MOV [RAX],RDI` 로 받은 기준 주소, RCX = 첫째 인자 세그먼트. `_SEGMENT +0x20 = BasedAddress` (PDB).
  - 같은 조각이 `+0x65 ADD [R15],RAX` (`TransferAddress += 새 기준 − 옛 기준`, flow.md 2.1 의 `+0x1b1271 ~ +0x1b1279`) 도 한다.
- run72 가 `MiMapViewOfImageSection +0x412 ~ +0x42a` 에서 비교하는 "섹션 `_SEGMENT.BasedAddress`" 가 바로 이 칸이다.

### 같은 파일을 다시 열 때 (질문 3·4-3)

- 이미 컨트롤 영역이 있는 파일은 `MiCreateNewSection` 을 안 타고 `MiCreateImageOrDataSection +0x1d0 MiShareExistingControlArea → +0xf4 MiValidateExistingImage → +0x51 MiRelocateImageAgain` 으로 간다 `[정적]`.
- `MiRelocateImageAgain +0x80 TEST [RBX+0x5c],0x400000` / `+0x87 JNZ`: 컨트롤 영역 `+0x5c` 비트 22 (처음 고른 직후 켜짐, `MiRelocateImage +0x1b127c BTS [R13+0x5c],0x16`) 가 켜져 있으면 다시 안 고르고 끝 → **먼저 고른 기준 주소 그대로** `[정적: 흐름으로 읽음, PDB 에 비트 이름 없음]`.
- 다시 고를 때도 `MiRelocateImageAgain` 의 넷째 인자는 `+0xbc NEG ESI` · `+0xc6 SBB R9D,R9D` · `+0xcc AND R9D,2` 라 0 또는 2 — 비트 0 은 늘 0 `[정적]`.

### 호출 관계

```
NtCreateSection → MiCreateSectionCommon → MiCreateSection → MiCreateImageOrDataSection
 ├─ (새 파일) +0x385  MiCreateNewSection
 │            │  +0x361 ESI = 패킷 플래그 → +0x38b SHR 27 → +0x391 AND 2   (비트 0 늘 0)
 │            ├─ +0x3d5/+0x3da  MiRelocateImage(…, 여섯째 = ESI)
 │            └─ +0x695/+0x69a  MiRelocateImage(…, 여섯째 = ESI)
 │                               ├─ +0x2e6/+0x2eb  MiSelectImageBase(넷째 = 여섯째 & 3)
 │                               │    ├─ 비트 0 = 1: +0xd9 JNZ → +0x2ba 헤더 ImageBase 그대로   ← dll1·dll2 는 안 탐
 │                               │    └─ 비트 0 = 0 · DLL: +0x18d RtlFindClearBitsEx
 │                               │         └─ +0x1ab/+0x1b0  MiObtainRelocationBits
 │                               │              └─ +0x86/+0x8b  RtlFindClearBitsAndSetEx  (락 안에서 찾기+켜기)
 │                               └─ 조각 +0x1b1269  _SEGMENT.BasedAddress = 고른 주소
 └─ (이미 있음) +0x1d0/+0x1d5  MiShareExistingControlArea
              └─ +0xf4/+0xf9  MiValidateExistingImage
                   └─ +0x51/+0x56  MiRelocateImageAgain   (+0x87 JNZ: 이미 골랐으면 끝)
                        └─ +0xd3/+0xd8  MiSelectImageBase(넷째 = 0 또는 2)

(test.exe 길 밖) MmLoadSystemImageEx +0x202/+0x207  MiGetSystemAddressForImage
                   └─ 조각 FUN_140b3f3dc +0x3ae/+0x3b3  MiRelocateImage(…, 여섯째 = 1)   ← 비트 0 을 켜는 유일한 곳
```

- `NtCreateSection → MiCreateSectionCommon → MiCreateSection` 중간 길은 결과서가 이름으로 적은 것이고, 이번에 ntdll `LdrpMapDllNtFileName +0x18f/+0x194 NtCreateSection` (`+0x14e MOV R13D,0x1000000` → `+0x17b` 여섯째 인자 `SEC_IMAGE`) 까지만 줄로 봤다.

## 확인된 것

- `[정적]` `MiCreateNewSection` 이 `MiRelocateImage` 에 주는 여섯째 인자 = 패킷 플래그 `(>> 27) & 2` — **비트 0 은 두 부르는 자리 모두 늘 0** (`+0x361` · `+0x38b` · `+0x391` → `+0x3cc` · `+0x68c`).
- `[정적]` `MiRelocateImage` 는 그 값 `& 3` 을 `MiSelectImageBase` 넷째 인자로 준다 (`+0x2b0 ~ +0x2c8`).
- `[정적]` 비트 0 을 1 로 부르는 곳은 `MiGetSystemAddressForImage` 조각 `+0x3ae` 하나 (`+0x7b XOR EBX,EBX` · `+0x9c LEA R15D,[RBX+1]` → 조각 `+0x392 MOV [RSP+0x28],R15D`), 그 함수의 부르는 곳은 `MmLoadSystemImageEx +0x202` 하나. `MiRelocateImageAgain` 의 `MiSelectImageBase` 호출도 비트 0 은 늘 0.
- `[정적]` 그래서 dll1·dll2 (64비트 DLL) 는 `MiSelectImageBase` 의 비트맵 갈래를 탄다. `MiObtainRelocationBits +0x86` 이 락 안에서 찾기+켜기를 하므로 **두 DLL 은 서로 다른 기준 주소**를 받는다.
- `[정적]` 고른 주소가 `_SEGMENT.BasedAddress` 에 들어가는 줄 = `MiRelocateImage +0x1b1269` (조각 `FUN_140b24658 +0x55`).
- `[정적]` 같은 파일 컨트롤 영역이 살아 있으면 `MiRelocateImageAgain +0x87 JNZ` 로 먼저 고른 주소를 그대로 쓴다.
- `[정적]` ntdll 이 `NtCreateSection` 에 주는 할당 속성은 `0x1000000` (`SEC_IMAGE`) 하나 (`LdrpMapDllNtFileName +0x14e` → `+0x17b`). 64비트라 `+0x4a7` (`0x1100000`, WOW64) 갈래는 안 탐.
  - `MiRelocateImage` 넷째 인자 (= 패킷 비트 5) 는 `MiInitializeCreateSectionPacket +0x326 OR EAX,0x20` 이 `0x100000` 이 켜지고 `0x1000000` 은 없을 때만 켠다 → ntdll 값만으로는 0. 중간 길 (`NtCreateSection → MiCreateSectionCommon → MiCreateSection`) 에서 이 비트를 더하는 줄은 안 봤다 `[공백]`.
- 요청서의 오케스트레이터 짐작 줄 넷 (`+0x38b` · `+0x391` · `+0x3cc` · `+0x68c`) 은 모두 맞았다 (결과서).

## 정정

- run72 문서 (한 줄 결론 2 · "dll1·dll2 맵 결과" 절 · "아직 확인 필요" 정적 후보 첫 줄): "그걸 가르는 `MiCreateNewSection` 줄은 안 읽음 `[공백]`" → **닫힘**: 비트 0 은 늘 0. "헤더 그대로 갈래면 둘째 DLL 은 반드시 `0x40000003`" 은 틀린 셈은 아니지만 **dll1·dll2 에는 해당 없는 갈래**다 (그 갈래는 `MmLoadSystemImageEx` 길뿐). run72 문서 맨 위에 정정 한 줄을 넣었다.
- run72 의 동적 1 기대값 "dll1·dll2 `0` `[추측]`" → 근거가 `[정적]` (이번 run). 실제 값은 여전히 동적.

## 사진과 맞춰 본 것 (run44, 새 실행 없음)

- `[관찰]` run44 P1b: dll1 `DllBase` = `OriginalBase` = `0x7ffae12e0000`, dll2 `0x7ffadb950000` (`ms/logs/04_baggage-read_P1b.txt` 3802 · 3853 · 3907 · 3958줄, run72 가 옮김).
- 주소 모양은 이번 결론과 맞는다 `[추측: 주소 모양으로만]`: 둘 다 `0x180000000` 이 아니고, 서로 다르고, 64KB 로 떨어지는 `0x7fffffff0000` 아래 값. "꼭대기에서 64KB 칸씩 아래로" 와 어긋나지 않는다. 정확한 칸 번호는 모른다.
- `OriginalBase` = `DllBase` 인 이유 (메모리 헤더 `ImageBase` 를 커널이 고쳤나, ntdll 재배치가 고쳤나) 는 **이번에도 못 가렸다** `[공백]`. `MiRelocateImage` 안에서 본 헤더 쪽 쓰기는 `TransferAddress` 더하기뿐이다 (결과서 5번).

## 아직 확인 필요

동적 — **Q104 VM 단계** 몫 (멈춤점은 `process test.exe` 조건):

1. **맵 결과 값** (가장 중요, run72 동적 1 과 같은 멈춤점): `ntdll!LdrpMinimalMapModule +0x179` (rva `0xa399`) `r eax` — dll1·dll2·kernel32·kernelbase. 기대 dll1·dll2 `0`, 서로 다른 `DllBase` (이제 근거 `[정적]`). `0x40000003` 이면 그 범위가 차 있었다는 뜻 → 재시도 길.
2. (선택) `nt!MiSelectImageBase +0x215` (`MOV [RAX],RDI`) `rdi` = 고른 기준 주소, 직전 `+0x1d4`·`+0x1d8` 의 `ebp` (시작 비트) · `r12w` (칸 수). 부른 쪽 `nt!MiRelocateImage +0x2eb`. **같은 부팅에 이미 돌렸다면 안 설 수 있다** (컨트롤 영역 재사용 → `nt!MiRelocateImageAgain +0x87`, Q61 과 같은 갈림).
3. (선택) `dt nt!_SEGMENT` `BasedAddress` (`+0x20`) 가 dll1·dll2 서로 다른지, `_CONTROL_AREA +0x58/+0x5c` (시작 비트·길이·비트 22/23).
4. `dq nt+0xfc5210 L1` (`MmRegistryState+0x20`) — 0 이 아니면 `MiSelectImageBase +0x12c JNZ` (`MiImageCanUseHighOverflowArea`) 갈래.

정적 — 다음 바퀴 후보 (대기열 번호는 오케스트레이터가):

- 패킷 비트 5 (`MiRelocateImage` 넷째 인자) 를 `NtCreateSection → MiCreateSectionCommon → MiCreateSection` 중간에서 더하는 줄이 있나 `[공백]`.
- 패킷 비트 28 (여섯째 인자 비트 1, `MiInitializeCreateSectionPacket +0x9d TEST SIL,0x40` → `+0x306 BTS [RBX],0x1c`) 의 `0x40` 을 누가 주나 `[공백]` — 비트 0 결론과는 무관.
- `OriginalBase` = `DllBase` 의 이유: `MiRelocateImage` 아래 `MiWalkEntireImage` · `MiScanRelocationPage` 에서 헤더 `+0x30` 쓰기 + ntdll `LdrpRelocateImage` (결과서 동적 5, 이번 범위 밖).
- (run72 에서 이어짐) `EPROCESS.Flags3` 비트 13 `AllImagesAtBasePristineBase` (`MiSelectImageBase +0xba`) 의 뜻.

## 디버거 메모 / 도구 메모

- 없음 (정적만). 결과서 스크립트 문제 없음. 레지스터 값 따라가기 임시 스크립트는 레포 밖 (세션 임시 폴더) 이라 커밋 안 됨.

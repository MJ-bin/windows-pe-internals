# 2026-10-23 run31 — exe 이미지 섹션: `MmCreateSpecialImageSection` → … → **`MiCreateImageFileMap` 은 안쪽이 맞다** (단 같은 파일 컨트롤 영역이 있으면 공유 길로 건너뜀). 진입점이 처음 적히는 줄 = **`MiBuildImageControlArea +0x172`** (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntoskrnl (`ntoskrnl.exe`), pdb `ntkrnlmp.pdb` GUID+age `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음).
  - analyze 는 다시 안 걸었다 (이미 돼 있음). 지도는 기존 `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*` 를 그대로 썼다.
- 버전 대조 (결과서): `NtCreateUserProcess` ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` ret `+0x807` → `PspCallProcessNotifyRoutines` 가 run9·run12 와 같다. 다섯 함수의 rva·크기·호출 수가 요청서 지도 값과 같다.
- test.exe 헤더 값은 결과서가 `ms/bin/test.exe` (sample 빌드, run30 과 같은 파일) 를 직접 읽은 것이다.

| 함수 | rva | 크기 | 이번에 본 것 |
|---|---|---|---|
| `nt!MmCreateSpecialImageSection` | `0x96bd64` | 371바이트 | 전체 |
| `nt!MiCreateSection` | `0x96d5e0` | 617바이트 | 전체 |
| `nt!MiCreateImageOrDataSection` | `0x96dfa0` | 1584바이트 | 전체 |
| `nt!MiCreateNewSection` | `0x96f4d0` | 1842바이트 | 전체 |
| `nt!MiCreateImageFileMap` | `0x96ff50` | 2916바이트 | 전체 + 멀리 떨어진 조각 |
| `nt!MiVerifyImageHeader` | `0x9707fc` | 1443바이트 | 전체 |
| `nt!MiBuildImageControlArea` | `0x970fc4` | 1278바이트 | 전체 (질문 4 의 답이 여기라 더 내려감) |
| `nt!MiRelocateImage` | — | — | 앞부분 + `TransferAddress` 를 고치는 조각만 |

## 목적

- Q33: 그림 1 `s3` `MmCreateSpecialImageSection` 이 무엇을 부르나. points.md 단계 3 `MiCreateImageFileMap` 이 이 함수 안쪽인가 (flow.md 2.1 `[추측]`).
- run30 과 잇기: PE 헤더의 `AddressOfEntryPoint` 가 이미지 정보 `TransferAddress` 로 **처음** 옮겨지는 줄은 어디인가 (run16 은 `PspAllocateProcess +0x991` `MmGetSectionInformation` 에서 꺼내는 쪽만 봤다).
- 참고: 단계 3 첫 항목 Q32 (`PspMapThreadCreationFlags` bit0) 는 run12 가 이미 답해서 바퀴 없이 닫았다.

## 방법

- 정적: `_workspace/sast/26_image-section.md` — ntoskrnl, 다섯 함수 + `MiVerifyImageHeader`. 결과서는 callee 가 100개 가까이 되어 **depth 0 으로 21개**를 이름으로 골라 뽑았다 (결과서 맨 위 목록).
  - 부르는 곳 표: `ms/disassemble/ntoskrnl/_refs/{MiCreateSection,MiCreateImageOrDataSection,MiCreateNewSection,MiCreateImageFileMap,MiVerifyImageHeader,MiBuildImageControlArea}.tsv`.
  - 구조체 칸 이름은 PDB 타입 덤프 (`llvm-pdbutil`) 로 맞췄다 (`_SECTION_IMAGE_INFORMATION`, `_MI_SECTION_IMAGE_INFORMATION`, `_SEGMENT` 등).
  - 핵심 네 줄 (`MiVerifyImageHeader +0x79~+0x8a`, `MiBuildImageControlArea +0x16b~+0x172`, `MiRelocateImage` 조각 `ADD`, `MmCreateSpecialImageSection` 할당 속성)은 결과서가 원본 바이트로 다시 봤다.
- 결과서: `_workspace/sast/26_image-section_result.md` (**분석성공**)
- 이번에 `.asm` 과 다시 맞춰 본 줄 (flow-writer): `MmCreateSpecialImageSection +0xcf/+0xd4`·`+0x141/+0x146`·`+0x14f`, `MiCreateSection +0xf3/+0xf8`, `MiCreateImageOrDataSection +0x19e/+0x1a3`·`+0x1ba~+0x1bf`·`+0x1d0/+0x1d5`·`+0x380/+0x385`, `MiCreateNewSection +0xd2~+0xd9`·`+0x162/+0x167`·`+0x210/+0x215`·`+0x3d5/+0x3da`, `MiCreateImageFileMap +0x157/+0x15c`·`+0x17f~+0x188`·`+0x1f1/+0x1f6`·`+0x2e1/+0x2e6`·`+0x4fc/+0x501`, `MiVerifyImageHeader +0x79~+0x8a`, `MiBuildImageControlArea +0x16b~+0x172`, `MiRelocateImage` 조각 `FUN_140b24658 +0x5d/+0x65/+0x143` (= `+0x1b1271`/`+0x1b1279`/`+0x1b1357`), `MmInitializeProcessAddressSpace +0x1c9/+0x1ce` (`_calls.tsv`). 어긋난 곳 없음.

## 결과

비유 (관리사무소 = 커널, 관리실 = ntdll): 관리사무소가 **설계도(exe 파일)를 읽어 조립식 세트(이미지 섹션)를 만든다.** 세트 속을 보면 "어느 방에 뭐가 들어가나" 배치표가 있고, 그 맨 위 칸에는 "첫 출근하면 이 방부터"(진입점)를 적어 둔다. 배치표 원본은 서랍(컨트롤 영역)에 넣어 둔다. 같은 설계도로 만든 배치표가 서랍에 이미 있으면 새로 안 그리고 그걸 같이 쓴다.

### 정적 — 사슬의 호출 지점 (`.asm` CALL 줄, 라벨은 ret)

| caller | call | ret (= 콜스택 오프셋) | callee | 이 고리를 타는 조건 | 표시 |
|---|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0x937` | `+0x93c` | `nt!MmCreateSpecialImageSection` | 서명 재시도 고리 안 (run9) | [정적] |
| `nt!MmCreateSpecialImageSection` | `+0xcf` | `+0xd4` | `nt!MiCreateSection` | 늘. 상태 `0xc0000054` 면 `+0xea` (ret `+0xef`) `KeDelayExecutionThread` 로 쉬고 다시 | [정적] |
| `nt!MiCreateSection` | `+0xf3` | `+0xf8` | `nt!MiCreateImageOrDataSection` | 패킷의 파일 핸들(`+0x28`)이나 파일 객체(`+0x30`)가 0 이 아닐 때. 여기선 핸들을 넘기니 늘 | [정적] |
| `nt!MiCreateImageOrDataSection` | `+0x19e` | `+0x1a3` | `nt!MiReferenceControlArea` | 늘. 파일의 `ImageSectionObject` 를 봄 | [정적] |
| `nt!MiCreateImageOrDataSection` | `+0x1d0` | `+0x1d5` | `nt!MiShareExistingControlArea` | **컨트롤 영역이 이미 있을 때** (`+0x38` bit1 `BeingCreated` 꺼짐) | [정적] |
| `nt!MiCreateImageOrDataSection` | `+0x380` | `+0x385` | `nt!MiCreateNewSection` | **없을 때** (`+0x1bd TEST AL,2` / `+0x1bf JNZ`) | [정적] |
| `nt!MiCreateNewSection` | `+0x210` | `+0x215` | `nt!MiCreateImageFileMap` | 패킷 `+0x10` 에 `SEC_IMAGE`(`0x1000000`) (`+0xd2`, `+0xd9 JNZ`). 아니면 `MiCreateDataFileMap` | [정적] |
| `nt!MiCreateNewSection` | `+0x162` | `+0x167` | `nt!MiSectionCreated` | 위 뒤 | [정적] |
| `nt!MiCreateNewSection` | `+0x3d5` | `+0x3da` | `nt!MiRelocateImage` | 새 기본 주소를 골랐을 때 (아래) | [정적] |
| `nt!MiCreateImageFileMap` | `+0x157` | `+0x15c` | `nt!MiCopyHeaderIfResident` | 늘. 헤더 첫 페이지가 메모리에 있으면 복사 | [정적] |
| `nt!MiCreateImageFileMap` | `+0x4fc` | `+0x501` | `nt!MiReadImageHeaders` | 위가 0 을 돌려주면 (디스크에서 읽기) | [정적] |
| `nt!MiCreateImageFileMap` | `+0x1f1` | `+0x1f6` | `nt!MiVerifyImageHeader` | `MZ`·`e_lfanew` 검사(`+0x17f ~ +0x1b9`) 통과 뒤 | [정적] |
| `nt!MiCreateImageFileMap` | `+0x2e1` | `+0x2e6` | `nt!MiBuildImageControlArea` | 검사 통과 뒤 | [정적] |
| `nt!MmCreateSpecialImageSection` | `+0x141` | `+0x146` | `nt!ObInsertObjectEx` | 섹션이 만들어졌을 때. 핸들을 `+0x14f` 에서 호출자 칸에 씀 | [정적] |

- `MiCreateNewSection` 을 부르는 곳, `MiCreateImageFileMap` 을 부르는 곳은 이 빌드에 **각각 하나뿐**이다 (`_refs`, CALL 참조 기준. 함수 포인터 길은 `refs` 가 못 봄). 그래서 "새 이미지 컨트롤 영역 만들기"는 모두 이 사슬을 지난다.

### 정적 — 갈림길: 같은 파일의 서랍(컨트롤 영역)이 이미 있나 (`MiReferenceControlArea`)

- 파일 객체의 `SectionObjectPointer`(`+0x28`) 안 **`ImageSectionObject`(`+0x10`)** 를 전역 잠금 안에서 본다. 세 갈래:
  1. **비었음** → 자리표시 컨트롤 영역(`BeingCreated` 켜짐)을 꽂고 돌려줌 → `MiCreateNewSection` (`+0x385`) → `MiCreateImageFileMap`.
  2. **있고 멀쩡함** → 사용 수를 1 올리고(`+0x11d`) 돌려줌 → `MiShareExistingControlArea` (`+0x1d5`). `MiCreateImageFileMap` 은 **안 탄다.**
  3. **만드는 중/지우는 중** → 기다렸다가(`KeWaitForGate`) 처음부터 다시.
- test.exe 처음 실행 때 `MiCreateImageFileMap` 까지 가나: `SEC_IMAGE` 갈래는 **예** `[정적]`, 서랍(컨트롤 영역)에 배치표가 이미 있나는 **실행 때 값**이다. 같은 부팅에서 test.exe 를 이미 돌렸고 그 컨트롤 영역이 남아 있으면 공유 길로 간다. 프로세스가 끝난 뒤 얼마나 남는지는 안 봤다 `[추측]`.

### 정적 — `MmCreateSpecialImageSection` 이 "특별한" 이유

- **사용자 모드 `NtCreateSection` 의 커널 안 버전**이다. 같은 `MiCreateSection` 을 쓴다.
- 다른 점 (일반 길 `MiCreateSectionCommon` 과 비교, 결과서):
  1. **토큰·서명 수준을 인자로 받는다** (3번째·4번째 인자 → `MiCreateSection` 7번째·8번째). 일반 길은 현재 프로세스에서 구한다 (`PsReferencePrimaryTokenWithTag`, `_EPROCESS.SectionSignatureLevel`). 이유 `[추측]`: 새 프로세스가 아직 없어서, 부모 것이 아니라 새 프로세스 기준으로 검사하려는 것.
  2. 사용자 인자 처리가 없다. 할당 속성은 이미지, 페이지 보호 `0x10`, 접근 모드 = 커널.
  3. **핸들**을 돌려준다 (`+0x141` `ObInsertObjectEx`, 접근권 `0xf001f`). 객체는 `NtCreateUserProcess +0x96d` (ret `+0x972`) `ObReferenceObjectByHandle` 로 다시 얻어 `PspAllocateProcess` 로 넘긴다 (run10 과 맞음).
- `SEC_IMAGE` 갈래인 근거: `NtCreateUserProcess +0x7bc` 가 플래그 bit0 를 늘 켜고, `MmCreateSpecialImageSection +0x3f ~ +0x56` 이 bit0 로 할당 속성 `0x01000000` 을 고른다 (원본 바이트).
- 그 밖에 `+0x64` (ret `+0x69`) `Feature_TrustedLaunchHosts…` 검사, `+0x86` (ret `+0x8b`) 세션 id, `+0xf6 ~ +0x11c` 파일 마지막 페이지 꼬리 0 채우기(`CcZeroEndOfLastPage`). 세션 id 를 구하는 함수는 이 주소에 PDB 이름이 둘이다 (`.asm` `ALIASES`: `PsGetProcessSessionIdEx`, `PsGetSessionIdEx`). 결과서는 어느 쪽인지 안 가렸다.
- **서명 재시도 고리(`NtCreateUserProcess +0xa08 ~ +0xa2a`)는 이 사슬의 상태값 때문이 아니다.** 섹션을 만든 뒤 `NtCreateUserProcess` 가 서명 수준을 다시 알아내서 (`+0x995` `PspGetProcessProtectionRequirementsFromImage`, `+0x9dd` `SeQuerySigningPolicy`) 처음 넘긴 값과 다르면 섹션을 닫고 새 수준으로 다시 부른다. 몇 번 도는지는 실행 때 값 (Q34).

### 정적 — `MiCreateImageFileMap` 이 하는 일 (5묶음)

| 묶음 | 하는 일 | 대표 줄 (ret) |
|---|---|---|
| A. 준비 | 파일 크기 (4GB 넘으면 거절), 헤더 한 페이지 버퍼, 파일 끝 정리 | `+0x96` `FsRtlGetFileSize`, `+0xdb` `MiCreateMdl` |
| B. **헤더 읽기** | 메모리에 있으면 복사, 없으면 디스크에서 | `+0x15c` `MiCopyHeaderIfResident` / `+0x501` `MiReadImageHeaders` |
| C. **헤더 검사** | `MZ`·`e_lfanew` 범위는 여기서, 나머지 PE 검사는 맡김 | `+0x188` `CMP word [RDX],AX` (`0x5a4d`), `+0x1f6` `MiVerifyImageHeader` |
| D. **배치표 원본·서랍 만들기** (프로토 PTE·컨트롤 영역) | 컨트롤 영역·세그먼트·서브섹션·프로토 PTE 배열, 이미지 정보 칸 채우기 | `+0x2e6` `MiBuildImageControlArea` |
| E. 뒷정리·옵션 | 커밋 청구, (조건부) 작은 이미지 통째 읽기 / 헤더 페이지 꼬리 0 | `+0x3e1` `MiInitializeImageProtos`, `+0x3fb` `MiInitializeImageHeaderPage` |

- **`MiVerifyImageHeader` 가 보는 것 (대표)**: `PE\0\0` 서명(`+0x21`), 실행 가능 비트, `Magic` = `0x10b`/`0x20b`, 파일·섹션 정렬, `SizeOfImage` ≤ `0x77000000`, 머신 (`0x8664` 등), `ImageBase` 64KB 정렬, `SizeOfOptionalHeader` 8의 배수. 틀리면 이유 번호를 전역(rva `0xe2d838`, 공개 이름 없음)에 적고 `0xc000007b`/`0xc0000130`.
  - test.exe 헤더 값(`Magic 0x20b`, `Machine 0x8664`, `SectionAlignment 0x1000`, `FileAlignment 0x200`, `ImageBase 0x140000000` …)을 대입하면 **모두 통과**한다 `[정적]`. 헤더가 첫 페이지 안이라 멀리 떨어진 "더 읽기" 조각도 안 탄다.
- **프로세스 메모리에 올리지 않는다** `[정적]`: 이 사슬에 `MmMapViewOfSection*` 이 없다. 헤더 한 페이지만 물리 메모리에 읽어 두고, 나머지는 "이 조각은 파일의 이 자리" 라는 PTE 표시만 채운다 (`MiMakeSubsectionPte` 등 — PTE 뜻은 이름으로 본 것이라 `[추측]`).
  - 실제로 올리는 건 나중 `MmInitializeProcessAddressSpace +0x1ce` → `MiMapProcessExecutable +0xb8` `MmMapViewOfSectionEx` (run10 `[정적]`, 2.1 그림 1 `p2`→`p3`→`p4`).

### 정적 — 진입점이 `TransferAddress` 로 옮겨지는 줄

| 단계 | 어디 | 무엇 | 표시 |
|---|---|---|---|
| ① | PE 파일 | NT 헤더 `+0x28` `AddressOfEntryPoint`, `+0x30` `ImageBase`. test.exe: `0x1d40` / `0x140000000` | [정적] (PE 파일) |
| ② | `MiVerifyImageHeader +0x79 ~ +0x8a` (PE32+ 갈래) | 칸 모음 `+0x0` ← `ImageBase`, **`+0x1c` ← `AddressOfEntryPoint`** (복사만) | [정적] |
| ③ | **`MiBuildImageControlArea +0x16b ~ +0x172`** | `[R15+0x1c]` + `[R15]` → **`[R8]` = 이미지 정보 `+0x0` `TransferAddress`** = `ImageBase + rva`. 아직 헤더 기준 주소 | [정적] |
| ④ | `MiRelocateImage` 조각 `+0x1b1279` (`MiCreateNewSection +0x3da` 에서) | `ADD [R15],RAX` — 새로 고른 기본 주소 − 헤더 기본 주소 를 더함. 같은 조각이 `ImageFlags` 의 `ImageDynamicallyRelocated` 를 켬 (`+0x1b1357`) | [정적], 실행 때 타는지는 Q61 |
| ⑤ | `PspAllocateProcess +0x991` `MmGetSectionInformation` | 세그먼트 `+0x38` 이 가리키는 이미지 정보 0x40바이트를 복사 | [정적] run16 |

- 이미지 정보가 사는 곳: `MiBuildImageControlArea` 가 만든 세그먼트(0x98바이트) 안 `+0x48` 쪽, 세그먼트 `+0x38` (`_SEGMENT.u2.ImageInformation`) 이 그 주소를 가리킨다. 앞 0x40 이 `_SECTION_IMAGE_INFORMATION` 이고 `+0x0` 이 `TransferAddress` 다 (PDB 타입).
- 같은 함수가 스택 크기·서브시스템·OS 버전·`Machine`·`DllCharacteristics` 등 다른 칸도 채운다 (결과서 표).
- test.exe 에서 ④ 가 실제로 더해지나는 **실행 때 값**이다. `DllCharacteristics 0x8160` (`DYNAMIC_BASE` 포함) 이고 재배치 디렉터리가 있다. run30 이 run5·run7 `[관찰]` 값으로 계산한 베이스 `0x7ff70ee10000` 은 헤더의 `0x140000000` 과 다르니 어디선가는 옮겨진다.
- run16 이 본 `PspAllocateProcess +0x20b3 ~ +0x20ce` 보정은 "④ 까지 반영된 값이 실제로 올라간 주소와 다를 때" 쓰는 것으로 보인다 `[추측]` (이번에 `PspAllocateProcess` 는 다시 안 읽음).
- 이 사슬에서 `TransferAddress` 를 쓰는 줄은 ③·④ 둘이다. 안 읽은 함수에 더 있는지는 모른다 `[추측: 이름으로는 아닐 것 같음]`.

### 호출 관계

```
NtCreateUserProcess
 └─ (3) +0x93c  MmCreateSpecialImageSection         핸들을 돌려줌
      ├─ (1) +0xd4   MiCreateSection
      │     └─ +0xf8  MiCreateImageOrDataSection
      │          ├─ (1) +0x1a3  MiReferenceControlArea     서랍(컨트롤 영역)이 이미 있나
      │          ├─ (2) +0x1d5  MiShareExistingControlArea  있으면 (아래 안 탐)        ┐ 둘 중
      │          └─ (2) +0x385  MiCreateNewSection          없으면                    ┘ 하나
      │                ├─ (1) +0x215  MiCreateImageFileMap      SEC_IMAGE
      │                │     ├─ (1) +0x15c  MiCopyHeaderIfResident  / +0x501 MiReadImageHeaders
      │                │     ├─ (2) +0x1f6  MiVerifyImageHeader      +0x8a 진입점 rva 복사
      │                │     └─ (3) +0x2e6  MiBuildImageControlArea  +0x172 TransferAddress = ImageBase + rva
      │                ├─ (2) +0x167  MiSectionCreated
      │                └─ (3) +0x3da  MiRelocateImage          조각 +0x1b1279 TransferAddress += 델타 (조건부)
      └─ (2) +0x146  ObInsertObjectEx                → +0x14f 핸들을 호출자 칸에
```

- `(n)` 은 성공 길로 보이는 흐름 순서다 (`MiCreateNewSection` 은 `+0x215` 에서 `JMP` 로 앞쪽 `+0x5da` 로 돌아가서 오프셋 순서와 다르다).

### 진입점 값이 흘러가는 길 (한 줄로)

```
PE AddressOfEntryPoint (NT 헤더 +0x28, test.exe 0x1d40)
 → MiVerifyImageHeader +0x8a          칸 모음 +0x1c 에 복사                      [정적] run31
 → MiBuildImageControlArea +0x172     이미지 정보 TransferAddress = ImageBase + rva [정적] run31
 → MiRelocateImage 조각 +0x1b1279      새 기본 주소면 델타 더함 (조건부)            [정적] run31, 실행 Q61
 → PspAllocateProcess +0x991 MmGetSectionInformation   이미지 정보 복사           [정적] run16
 → PspCreateUserContext  CONTEXT.Rcx                                          [정적] run16
 → (run29) NtContinue → RtlUserThreadStart(rcx) → BaseThreadInitThunk +0x17 → 진입점   [정적] run29, 대상 Q48·Q30
 → test!mainCRTStartup (rva 0x1d40) — run30 경계, 여기부터 CRT
```

### 그림 반영 (flow.md 그림 1)

- `s3` 아래에 **`(정적)` 점선 상자 다섯**을 한 줄 사슬로 넣었다: `MiCreateSection` → `MiCreateImageOrDataSection` (갈림길) → `MiCreateNewSection` → `MiCreateImageFileMap` (exe 헤더 읽기·검사) → `MiBuildImageControlArea` (`+0x172` 진입점).
  - 대표 지점은 결과서 후보 셋 (`MiCreateImageOrDataSection`, `MiCreateImageFileMap`, `MiBuildImageControlArea`) 이다. 사이의 `MiCreateSection`·`MiCreateNewSection` 은 화살표 라벨을 진짜 ret 로 쓰려고 넣었다. 빼면 화살표를 `?` (추측) 로 그려야 해서, 코드로 확인한 길이 추측처럼 보인다.
  - `MiShareExistingControlArea`·`MiVerifyImageHeader`·`MiRelocateImage` 는 상자 글에만 적었다 (그림 1 의 `PspCreateUserContext` "갈림길 한쪽" 과 같은 방식).
- `s3` 상자 글에 "핸들을 돌려줌, 서명 재시도 고리 안" 을 더했다.

## 확인된 것

- (`[정적]`) `MmCreateSpecialImageSection` → `MiCreateSection` → `MiCreateImageOrDataSection` → `MiCreateNewSection` → `MiCreateImageFileMap` 사슬 (ret `+0xd4` / `+0xf8` / `+0x385` / `+0x215`) 이 `.asm` CALL 줄과 맞는다. **flow.md 2.1 의 `[추측]` "`MiCreateImageFileMap` 은 이 함수 안쪽인지 모른다" → 안쪽이 맞다.**
- (`[정적]`) 단 **늘 타는 길은 아니다.** 같은 파일의 이미지 컨트롤 영역이 이미 있으면 `MiCreateImageOrDataSection +0x1d5` `MiShareExistingControlArea` 로 가서 `MiCreateNewSection`·`MiCreateImageFileMap` 을 건너뛴다.
- (`[정적]`) test.exe 를 만드는 길은 `SEC_IMAGE` 갈래다 (`NtCreateUserProcess +0x7bc`, `MmCreateSpecialImageSection +0x3f ~ +0x56`).
- (`[정적]`) `MmCreateSpecialImageSection` 은 `NtCreateSection` 의 커널 안 버전이다. 토큰·서명 수준을 인자로 받고, 핸들을 돌려준다.
- (`[정적]`) exe 헤더를 읽는 곳 = `MiCopyHeaderIfResident` / `MiReadImageHeaders`, 검사하는 곳 = `MiCreateImageFileMap +0x188` (`MZ`) + `MiVerifyImageHeader`. test.exe 헤더는 검사를 다 통과한다.
- (`[정적]`) **진입점이 이미지 정보 `TransferAddress` 에 처음 적히는 줄 = `MiBuildImageControlArea +0x172`** (`ImageBase + AddressOfEntryPoint`). 헤더에서 꺼내는 줄은 `MiVerifyImageHeader +0x87 ~ +0x8a`. 새 기본 주소를 고르면 `MiRelocateImage` 조각 `+0x1b1279` 가 같은 칸에 델타를 더한다.
- (`[정적]`) 이 사슬은 exe 를 프로세스 메모리에 올리지 않는다. 올리는 건 run10 의 `MiMapProcessExecutable +0xb8` 이다.
- (`[정적]`) 서명 재시도 고리는 이 사슬의 실패 상태 때문이 아니라 `NtCreateUserProcess` 가 섹션을 만든 뒤 서명 수준을 다시 비교해서 돈다.

### flow.md 2.1 `[추측]` 판정

| 예전 `[추측]` | 판정 | 근거 |
|---|---|---|
| "`MiCreateImageFileMap` 은 `MmCreateSpecialImageSection` 안쪽인지 모른다" | **맞다 (안쪽)** `[정적]`. 단 같은 파일 컨트롤 영역이 있으면 공유 길로 건너뜀 | 위 호출 지점 표, `_refs/MiCreateNewSection.tsv`·`_refs/MiCreateImageFileMap.tsv` (호출자 각 1곳) |
| 2.1 끝 "남은 것: `MiCreateImageFileMap`" | **해결됨** → run31 | 같음 |

- **points.md 단계 3 판정** (points.md 는 고치지 않음, 오케스트레이터가 판단): `nt!MiCreateImageFileMap` 을 대표 지점으로 둬도 된다. 단 "이 파일의 컨트롤 영역이 아직 없을 때만" 서고, 값이 바뀌는 순간 지점으로 `nt!MiBuildImageControlArea +0x172` 를 하나 더 두면 좋다 (결과서 질문 6).

## 아직 확인 필요

동적 확인거리는 오케스트레이터가 이미 대기열에 넣었다. 커널 멈춤점이라 test.exe 일 때만 서게 조건이 필요하다 (`_FILE_OBJECT.FileName`, dast-mentor 몫).

- **Q61** (결과서 D1 ~ D5, 한 번의 실행으로):
  - `nt+0x96f4d0` (`MiCreateNewSection` 입구) / `nt+0x96e6cc` (`MiShareExistingControlArea` 입구) 중 어느 쪽이 서나. 같은 부팅에 이미 돌렸으면 공유 쪽 → 재부팅 직후 첫 실행, 또는 파일을 복사해서.
  - `nt+0x96ff50` (`MiCreateImageFileMap` 입구) 에서 `k`: `MiCreateNewSection+0x215` ← `MiCreateImageOrDataSection+0x385` ← `MiCreateSection+0xf8` ← `MmCreateSpecialImageSection+0xd4` ← `NtCreateUserProcess+0x93c` 인가.
  - `nt+0x970886` (`MiVerifyImageHeader +0x8a`) 에서 `eax` = `0x1d40` 인가.
  - `nt+0x971136` (`MiBuildImageControlArea +0x172`) 에서 `rax` = `0x140001d40` 인가, 한 칸 실행 뒤 `dq @r8 L1`.
  - `nt+0xb246bd` (`MiRelocateImage` 조각 `ADD [R15],RAX`) 가 서나, 서면 `rax` (델타). 안 서면 이 길로는 델타를 안 더한 것.
- **Q35** (D6): `nt+0xa02ef9` (`PspAllocateProcess +0x991` ret 직후) `dq @r15+0x38 L1` 이 위 값과 같은가.
- **Q34**: `nt+0x96bd64` (`MmCreateSpecialImageSection` 입구) 를 몇 번 지나나, ret `nt+0xa01c7c` (`NtCreateUserProcess +0x93c`) 의 `eax`.
- **Q62** (정적으로 남은 것): (a) `TransferAddress` 를 쓰는 다른 줄 (안 읽은 함수들), (b) `PspAllocateProcess +0x20b3 ~ +0x20ce` 보정 조건과 `MiRelocateImage` 델타 관계, (c) 공유 길 `MiShareExistingControlArea` → `MiValidateExistingImage`, (d) 헤더 페이지 → 프로토 PTE (`MiInitializeNewImageSectionProtos`), (e) 작은 것들 (`MiVerifyImageHeader` 0x57~0x59 검사, `Feature_TrustedLaunchHosts`, 세션 id 별칭, `MiCreateSection` 안 재시도).
  - "컨트롤 영역이 프로세스가 끝난 뒤 얼마나 남나" 는 정적 대신 Q61 재실행 비교로 본다.

## 디버거 메모 / 도구 메모

- (동적 함정 후보) **이미지 섹션은 파일마다 한 번 만들어 두고 같이 쓴다.** 같은 부팅에서 test.exe 를 두 번째로 돌리면 `MiCreateImageFileMap` 이하가 안 설 수 있다. → windbg-kd-runbook pitfalls 에 넣자고 보고함.
- (도구) PDB 구조체 칸 표를 뽑는 명령이 스킬에 없어서 결과서가 이번에도 `llvm-pdbutil` 덤프를 임시 스크립트로 읽었다 (sast/11 도 같음). 결과서 "스크립트 문제" 제안.
- (도구) `.asm` 의 전역 주석은 PDB 공개 이름이 있는 것만 붙는다. `[0x140e2d838]` (실패 이유 번호 전역) 은 이름이 없어서 주석이 없다. `refs` 의 `(함수 밖) DATA` 줄은 `.pdata` 항목이다.

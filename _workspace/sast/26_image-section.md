# 26 image-section — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run31)
- 대기열: Q33 (`_workspace/plan.md`) — 2.1 `nt!MmCreateSpecialImageSection` callee (exe 이미지 섹션, `MiCreateImageFileMap` 과의 관계). 단계 3 "작은 정적부터 몰아서" (사용자 결정 2026-10-23) 의 둘째 항목 (Q32 는 run12 로 이미 답해 바퀴 없이 닫음).
- 관련: flow.md 그림 1 `s3` `nt!MmCreateSpecialImageSection (정적: 이미지 섹션)`, 2.1 `nt!MmCreateSpecialImageSection` 절 (`[추측]` "`MiCreateImageFileMap` 은 이 함수 안쪽인지 모른다"), 2.1 끝 "남은 것: `MiCreateImageFileMap`", 4장 2.1 줄 / `points.md` 단계 3 "exe 헤더 읽기 `nt!MiCreateImageFileMap` [확인 필요]" / 이전 요청 sast/03 (run9), sast/04 (run10, 섹션이 `PspAllocateProcess` 로 넘어감), sast/11 (run16, `TransferAddress` → `Rcx`)

## 범위

- **목표 수준은 "대표 지점"** (CLAUDE.md). 함수마다 callee 를 하나하나 해설하지 말고, 각 함수가 하는 일을 3 ~ 6 묶음으로 나누고, 사슬로 이어지는 호출과 질문에 걸린 호출만 줄 단위로.
- 대상 다섯 (depth 1, 지도 `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*` 에 모두 있음):
  - `MmCreateSpecialImageSection` (rva `0x96bd64`, 371B, 깊이 1, 호출 9)
  - `MiCreateSection` (`0x96d5e0`, 617B, 깊이 2, 호출 10)
  - `MiCreateImageOrDataSection` (`0x96dfa0`, 1584B, 깊이 3, 호출 29)
  - `MiCreateNewSection` (`0x96f4d0`, 1842B, 깊이 4, 호출 32)
  - `MiCreateImageFileMap` (`0x96ff50`, 2916B, 깊이 5, 호출 37 + 조각 `FUN_140b24268`)
- 질문 4 (진입점이 `TransferAddress` 로 옮겨지는 곳) 때문에 **한 단계 더** 내려가도 된다: `MiVerifyImageHeader` (`MiCreateImageFileMap +0x1f6`), 필요하면 `MiInitializeImageHeaderPage`·`MiBuildImageControlArea` 중 하나. 내려간 이유를 결과서에.
- 오프셋은 call·ret 둘 다. 점프 목적지는 `절대 주소 − 함수 시작`. `__except`/`__finally` 착지점 안 호출도.
- 구조체 칸은 PDB 타입으로 (`_SECTION_IMAGE_INFORMATION`, `_MI_SECTION_IMAGE_INFORMATION`, `_IMAGE_NT_HEADERS64`, `_CONTROL_AREA`, `_SEGMENT` 등).

## 알고 싶은 것

1. **사슬 확인**: 지도의 길 `MmCreateSpecialImageSection +0xd4` → `MiCreateSection +0xf8` → `MiCreateImageOrDataSection +0x385` → `MiCreateNewSection +0x215` → `MiCreateImageFileMap` (ret 기준) 이 맞나. 각 고리를 타는 **조건** (이미지 섹션 `SEC_IMAGE` 갈래인가, 캐시에 이미 있으면 건너뛰나 — 같은 파일의 섹션이 이미 있으면 `MiCreateNewSection` 을 안 타는 길이 있나).
   - test.exe 처음 실행 때 `MiCreateImageFileMap` 까지 갈 것인지 정적 조건으로 (예/아니오/실행 때 값).
2. **`MmCreateSpecialImageSection` 이 "특별한" 이유**: 일반 `MmCreateSection` 과 무엇이 다른가 (인자로 받는 것, `+0x64` `Feature_TrustedLaunchHosts…`, `+0x86` 세션, `+0xea` `KeDelayExecutionThread` 재시도 고리, `+0x141` `ObInsertObjectEx`). 돌려주는 것 (섹션 핸들 / 객체).
3. **`MiCreateImageFileMap` 이 하는 일 (exe 헤더 읽기)**: 묶음으로. 특히
   - PE 헤더를 **읽는 곳** (`MiReadImageHeaders`, `MiCopyHeaderIfResident`) 과 **검사하는 곳** (`MiVerifyImageHeader +0x1f6`) — 무엇을 검사하나 (서명 `MZ`/`PE`, 머신, 정렬, 크기 …) 대표만.
   - 섹션 표대로 "이미지 조각(프로토 PTE)" 을 만드는 곳 (`MiBuildImageControlArea`, `MiInitializeImageProtos`) — 여기서 실제 메모리에 올리지는 않는다는 게 맞나 (예/아니오). 실제 매핑은 나중 `MmInitializeProcessAddressSpace`/`MiMapViewOfImageSection` 쪽 `[추측]`.
4. **진입점 → `TransferAddress`** (run30 과 이어짐):
   - PE 헤더의 `OptionalHeader.AddressOfEntryPoint` (+0x28 from NT 헤더 시작) 를 읽어 이미지 정보의 `TransferAddress` (`_SECTION_IMAGE_INFORMATION +0x0` 또는 `_MI_SECTION_IMAGE_INFORMATION` 안) 에 **ImageBase + rva** 로 적는 줄은 어느 함수 어느 오프셋인가.
   - run16 은 `PspAllocateProcess +0x991 MmGetSectionInformation` 이 이 값을 꺼내고, 다른 주소에 올라가면 `+0x20b3 ~ +0x20ce` 에서 보정한다고 봤다. 그 값의 **처음 출처**를 찾는 것이 목표. 못 찾으면 후보 함수 이름만.
5. **서명 재시도 고리 (sast/03)**: `NtCreateUserProcess +0xa08 ~ +0xa2a` 가 섹션을 닫고 `MmCreateSpecialImageSection` 을 다시 부르는 조건 — 이번 대상 안에서 돌려주는 상태값으로 설명되면 한 줄 (아니면 생략).
6. **points.md 판정**: 단계 3 "exe 헤더 읽기 = `nt!MiCreateImageFileMap`" 을 대표 지점으로 둬도 되나 (예/아니오, 이유 한 줄). 더 나은 대표 지점이 있으면 (예: `MiVerifyImageHeader`) 이름.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^MmCreateSpecialImageSection$` | 1 | 질문 1·2·5 |
| ntoskrnl | `^MiCreateSection$` | 1 | 질문 1 |
| ntoskrnl | `^MiCreateImageOrDataSection$` | 1 | 질문 1 |
| ntoskrnl | `^MiCreateNewSection$` | 1 | 질문 1 |
| ntoskrnl | `^MiCreateImageFileMap$` | 1 | 질문 1·3·4·6 |
| ntoskrnl | `^MiVerifyImageHeader$` (필요하면 하나 더) | 1 | 질문 3·4 |

## 지도에서 본 것 (`_map/NtCreateUserProcess_*`, ret 기준)

- 역방향 최단 길: `MiCreateImageFileMap` ← `MiCreateNewSection +0x215` ← `MiCreateImageOrDataSection +0x385` ← `MiCreateSection +0xf8` ← `MmCreateSpecialImageSection +0xd4` ← `NtCreateUserProcess +0x93c`. 각 함수 caller 는 지도 안에서 1곳씩.
- `MmCreateSpecialImageSection` 호출 9: `+0x69` `Feature_TrustedLaunchHosts__private_IsEnabledDeviceUsageNoInline`, `+0x8b` `PsGetProcessSessionIdEx` (별칭 있음, 이름 믿지 말 것), `+0xd4` `MiCreateSection`, `+0xef` `KeDelayExecutionThread`, `+0xfb` `MiSectionControlArea`, `+0x106` `MiReferenceControlAreaFile`, `+0x111` `CcZeroEndOfLastPage`, `+0x11c` `MiDereferenceControlAreaFile`, `+0x146` `ObInsertObjectEx`.
- `MiCreateImageFileMap` 호출 37 중 눈에 띄는 것 (ret): `+0x96` `FsRtlGetFileSize`, `+0x15c` `MiCopyHeaderIfResident`, `+0x1f6` `MiVerifyImageHeader`, `+0x2e6` `MiBuildImageControlArea`, `+0x3e1` `MiInitializeImageProtos`, `+0x3fb` `MiInitializeImageHeaderPage`, `+0x501` `MiReadImageHeaders`, `+0x75f` `MiLogCreateImageFileMapFailure`, `+0x852` `MiEnableImageDirectMap`, 멀리 떨어진 조각들 (`+0x1b40b3` ~ `+0x1b42f6` — `MiReadImageHeaders`·`MiAllocateEntireImageFileExtents`·`MiLegacyImageArchitecture`), 조각 `FUN_140b24268`.
- `MmCreateSection` (`0x96cfd0`) 은 지도에서 깊이 10 끝 — 이번 길과 관계없음 `[추측]`.

## 이미 아는 것

- `[정적]` (run9) `NtCreateUserProcess +0x937` (ret `+0x93c`) `MmCreateSpecialImageSection` — 연 파일로 이미지 섹션을 만든다. 서명 수준이 안 맞으면 섹션을 닫고(`+0xa1e`, `+0xa2a`) 다시 만드는 고리 안.
- `[정적]` (run9) `MiCreateImageFileMap` 은 `NtCreateUserProcess` 가 직접 부르지 않는다.
- `[정적]` (run10) 결과 섹션은 `[RBP+0x260]` → `[RBP+0x270]` (`+0x978`) 으로 옮겨져 `PspAllocateProcess` 에 넘어간다.
- `[정적]` (run16) `PspCreateUserContext` 가 `CONTEXT.Rcx` ← `_SECTION_IMAGE_INFORMATION.TransferAddress` (출처 `PspAllocateProcess +0x991 MmGetSectionInformation`, 재배치 보정 `+0x20b3 ~ +0x20ce`).
- `[정적]` (run30) test.exe `AddressOfEntryPoint` = rva `0x1d40` = `mainCRTStartup`. 커널은 이 값을 `Rcx` 로 넘길 뿐 — 그 사이 첫 고리가 이번 질문 4.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄, call·ret 둘 다). 태그 `[정적]` / `[추측]`. 라벨은 ret.
- `NtCreateUserProcess +0x93c` 에서 `MiCreateImageFileMap` 안 헤더 검사·진입점 기록까지 **이어진 사슬 하나** (대표 지점만).
- "PE 헤더 `AddressOfEntryPoint` → 이미지 정보 `TransferAddress` → `MmGetSectionInformation` → `PspCreateUserContext` `Rcx` → (run29) `RtlUserThreadStart` → `BaseThreadInitThunk` → 진입점" 줄에서 이번에 채운 칸.
- flow.md 2.1 `[추측]` 판정, 그림 1 `s3` 아래에 넣을 `(정적)` 상자 후보 1 ~ 3개 (대표 지점만).
- 동적으로 확인할 것 + 멈춤점 (`nt+rva`, 짧게). 이미 대기 중인 동적 항목과 겹치면 그 번호를 (예: Q19 쪽 `NtCreateUserProcess` 실행, Q34·Q35).
- 스크립트 문제는 "스크립트 문제" 절에.

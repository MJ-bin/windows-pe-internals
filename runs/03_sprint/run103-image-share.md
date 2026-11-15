# 2026-11-16 (작성일) run103 — 이미지 섹션 공유 길 · 섹션 보호값 · 헤더 페이지 · 작은 것 넷 (Q62b = Q62 (c)·(d)·(e), 정적만): **같은 파일의 컨트롤 영역이 이미 있으면 `MiShareExistingControlArea` 는 새로 만들지 않고, 검사 (`MiValidateExistingImage`) 가 실패하면 status 로 끝난다** `[정적]` (예외 하나 — 파티션 불일치 `0xC0000476` 만 `MiCreateSection` 이 패킷부터 다시). 검사는 이동식 미디어 · 기준 주소 (`MiRelocateImageAgain`) · 서명 셋이고, **파일이 바뀌었나 (시각 · 크기) 비교 줄은 없다.** **기준 주소는 컨트롤 영역 `+0x5c` 비트 22 (`ImageActive`) 가 꺼져 있을 때만 다시 고른다** — 앞 실행의 뷰 · 섹션 참조가 다 없어지면 `MiImageUnused` 가 이 비트를 끈다 (run73 "살아 있으면 그대로" 정정). **섹션마다 보호값은 `MiInitializeNewImageSectionProtos` 가 아니라 더 앞의 `MiParseImageSectionHeaders` 가 표 `MiImageProtectionArray` 로 채운다** (요청서 전제 정정). `MiInitializeNewImageSectionProtos` 는 헤더 페이지 한 쪽을 프로토 PTE 에 transition 으로 꽂고, 나중에 `MiFinalizeImageHeaderPage` 가 내려놓는다. `MiVerifyImageHeader` `0x57` · `0x58` · `0x59` 는 test.exe 가 안 탄다. 세션 번호 함수는 이름 둘 (`PsGetProcessSessionIdEx` · `PsGetSessionIdEx`). `MiCreateSection` 안쪽 재시도 = `0xC0000054` · `0xC0000476`.

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 값 대조는 run44 사진 덤프 읽기 로그를 다시 읽은 것뿐이다 (아래 "사진 로그 대조").
- 정적: **ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1`** (기준과 같음). 다시 analyze 안 함, `export` 만 (결과서).
  - 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 그대로 (`_calls.tsv`).
- test.exe 섹션 특성 값은 결과서가 `sample/bin/test.exe` PE 섹션 표를 파이썬으로 읽은 것 `[소스: 빌드 산출물 헤더]`.
- 다른 decompiler 가 같은 시간에 ntdll (sast/109) · ntoskrnl (sast/110) 을 돌리는 중이다. **sast/99 ~ 110 결과는 이 문서에 안 넣었다.**

## 목적

- **Q62b** (= Q62 (c)·(d)·(e), run31 의 "정적으로 남은 것". Q62a 는 run102):
  - (c) 공유 길 `MiShareExistingControlArea → MiValidateExistingImage` 가 무엇을 검사하나, 실패하면 새로 만드나 status 로 끝나나, `MiRelocateImageAgain → MiSwitchBaseAddress` 는 언제 타고 `BasedAddress` · `TransferAddress` 가 어떻게 바뀌나.
  - (d) `MiSectionCreated +0x103 MiInitializeNewImageSectionProtos` 가 하는 일, 섹션 특성 → 보호 표, 헤더 페이지 차례 (`MiFinalizeImageHeaderPage`).
  - (e) `MiVerifyImageHeader` 이유 `0x57` ~ `0x59` · `Feature_TrustedLaunchHosts…` · 세션 번호 함수 별칭 · `MiCreateSection` 안 재시도.
- 앞 바퀴 짝: run31 (`runs/02_sprint/run31-image-section.md`, 갈림 `MiCreateNewSection` / `MiShareExistingControlArea`), run47 (공유 길은 카운터 · `SizeOfSection` 만), run73 (`MiRelocateImageAgain +0x87 JNZ`), run102 (`MiSwitchBaseAddress +0x46` · `+0x4e`).

## 방법

- 정적: `_workspace/sast/101_image-share.md` — ntoskrnl
  - `^MiShareExistingControlArea$` · `^MiValidateExistingImage$` · `^MiSectionCreated$` · `^MiInitializeNewImageSectionProtos$` depth 1, `^MiRelocateImageAgain$` · `^MiSwitchBaseAddress$` · `^MiVerifyImageHeader$` · `^MiCreateSection$` depth 0 (모두 이미 있던 `.asm` 을 읽기만).
  - 새로 뽑은 것 13개 (depth 0, "조건 칸" 을 읽으려고 넓힘): `MiMakeSubsectionPte` · `MiMakeDemandZeroPte` · `MiReferenceControlAreaPfn` · `PsGetProcessSessionIdEx` · `Feature_TrustedLaunchHosts__private_IsEnabledDeviceUsageNoInline` · `…IsEnabledFallback` · `wil_details_IsEnabledFallback` · `MiImageUnused` · `MiDereferenceFailedControlArea` · `MiValidateControlAreaPartition` · `MiGetControlAreaLoadConfig` · `MiCheckControlArea` · `MiMakeProtectionMask`.
  - 원본 바이트: `MiImageProtectionArray` (`.rdata` rva `0x1a6b0`, 16바이트), `MmUserProtectionToMask1/2` (rva `0x3b020` · `0x3b030`).
- 결과서: `_workspace/sast/101_image-share_result.md` (**분석성공** — 정적으로 못 정한 것: Feature 값, `ImageActive` 실제 값, test.exe 가 공유 길을 타는지, `MiApplyBytestreamFixup` 뜻).
- flow-writer 가 다시 본 것:
  - `.asm`: `MiRelocateImageAgain` (`+0x7b` · `+0x80` · `+0x87` · `+0xa3` · `+0xd3` · `+0xde` · `+0x10b` · `+0x113` · `+0x165` · `+0x17e` · `+0x1a1` · `+0x1b5`), `MiImageUnused` (`+0x76 ~ +0x83`), `MiCheckControlArea` (`+0xc9` · `+0x3be`), `MiInitializeNewImageSectionProtos` (`+0x2e` · `+0x3d` · `+0x41` · `+0xe5` · `+0xf5` · `+0x110` · `+0x115` · `+0x1a2 ~ +0x1d2`), `MiReferenceControlAreaPfn` (`+0x14 ~ +0x46` 전부), `MiSectionCreated` (`+0x42` · `+0x4b` · `+0x57` · `+0xa6` · `+0xfe` · `+0x124`), `MiParseImageSectionHeaders` (`+0x176` · `+0x17d` · `+0x1a8` · `+0x1af` · `+0x1b4` · `+0x1bf` · `+0x1c9` · `+0x1d1` · `+0x206` · `+0x20a` · `+0x240` · `+0x249` · `+0x258` · `+0x25c` · `+0x34b ~ +0x399`), `MiCreateSection` (`+0xfa ~ +0x10a` · `+0x237`), `MiVerifyImageHeader` (`+0x259` · `+0x263` · `+0x274` · `+0x27e` · `+0x40b` · `+0x54f` · `+0x56b`), `MmCreateSpecialImageSection` (`+0x64` · `+0x69` · `+0x86` · `+0x8b`).
  - `_calls.tsv` (ntoskrnl): `MiShareExistingControlArea` 11줄 · `MiValidateExistingImage` 7줄 (`+0x2a6/+0x2ab` 는 `indirect:cfg:?`), `MiBuildImageControlArea +0x362/+0x367 MiParseImageSectionHeaders`, `MiParseImageSectionHeaders +0x1d5/+0x1da MiMakeSubsectionPte` · `+0x1e0/+0x1e5 MiMakeDemandZeroPte`.
  - `alias:` 한 곳: `MmCreateSpecialImageSection +0x86` · `MiRelocateImageAgain +0x165` — 세션 번호를 묻는 함수 (같은 주소 이름 2개, 후보: `PsGetProcessSessionIdEx` · `PsGetSessionIdEx`). 이번 질문 (e) 가 바로 이 별칭이라 아래에선 두 이름을 같이 쓴다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!MiCreateImageOrDataSection` | `+0x1d0` | `+0x1d5` | `nt!MiShareExistingControlArea` | 컨트롤 영역이 이미 있고 `BeingCreated` 꺼짐 | [정적] (run31 · run47 그대로) |
| `nt!MiShareExistingControlArea` | `+0x28` | `+0x2d` | `nt!MiValidateControlAreaPartition` | 파티션 검사. 실패 `0xC0000476` | [정적] |
| `nt!MiShareExistingControlArea` | `+0x132` · `+0x157` | `+0x137` · `+0x15c` | `PsIsCurrentThreadPrefetching` · `MmChangeSectionBackingFile` | 컨트롤 영역 `PrefetchCreated` (bit9) 이고 지금 스레드가 프리페처가 아닐 때만 | [정적] |
| `nt!MiShareExistingControlArea` | `+0xf4` | `+0xf9` | `nt!MiValidateExistingImage` | 패킷 `+0x10 & 0x1000000` (`SEC_IMAGE`) 일 때 | [정적] (run47 은 이름만) |
| `nt!MiValidateExistingImage` | `+0x51` | `+0x56` | `nt!MiRelocateImageAgain` | 기준 주소 다시 고르기 (아래) | [정적] (run73 그대로) |
| `nt!MiValidateExistingImage` | `+0xd6` · `+0x16a` | `+0xdb` · `+0x16f` | `SeGetImageRequiredSigningLevel` · `Feature_TrustedLaunchHosts__private_IsEnabledDeviceUsageNoInline` | 서명 요구 수준 · 기능 스위치 | [정적] |
| `nt!MiValidateExistingImage` | `+0x1e7` · `+0x209` · `+0x2a6` | `+0x1ec` · `+0x20e` · `+0x2ab` | `SeCompareSigningLevels` ×2 · 간접 (`SeCiCallbacks+0x98` 칸, CFG 거침) | 서명 수준 비교 | [정적] (`+0x2a6` 대상은 결과서가 `.asm` 으로 따라감) |
| `nt!MiValidateExistingImage` | `+0x258` | `+0x25d` | `nt!MiValidateSectionSigningPolicy` | 서명 정책. 비교 비트 `ESI & 6` 이 0 이면 바로 0 | [정적] |
| `nt!MiRelocateImageAgain` | `+0xa3` · `+0xd3` | `+0xa8` · `+0xd8` | `MiReservePtes` · `MiSelectImageBase` | **`ImageActive` 꺼짐일 때만** (`+0x80` · `+0x87`) | [정적] |
| `nt!MiRelocateImageAgain` | `+0x165` | `+0x16a` | 세션 번호를 묻는 함수 (같은 주소 이름 2개, 후보: `PsGetProcessSessionIdEx` · `PsGetSessionIdEx`) | 패킷 비트 5 꺼짐일 때 | [정적] |
| `nt!MiRelocateImageAgain` | `+0x17e` | `+0x183` | `nt!MiSwitchBaseAddress` | 새 기준 ≠ 옛 `BasedAddress` | [정적] (run102 그대로) |
| `nt!MiRelocateImageAgain` | `+0x1b5` | `+0x1ba` | `nt!MiImageUnused` | 비트맵 · CI 갱신 실패 때 되돌리기 | [정적] |
| `nt!MiCheckControlArea` | `+0xc9` · `+0x3be` | `+0xce` · `+0x3c3` | `nt!MiImageUnused` | 뷰 0 · 섹션 참조 0 · PFN 참조 ≠ 0 등 (아래) → `ImageActive` 끔 | [정적] |
| `nt!MiBuildImageControlArea` | `+0x362` | `+0x367` | `nt!MiParseImageSectionHeaders` | **섹션마다 보호값** (`MiImageProtectionArray`) | [정적] (run31 · run47 은 "서브섹션별 PTE") |
| `nt!MiParseImageSectionHeaders` | `+0x1d5` · `+0x1e0` | `+0x1da` · `+0x1e5` | `MiMakeSubsectionPte` · `MiMakeDemandZeroPte` | 프로토 PTE 모양 두 가지 | [정적] |
| `nt!MiSectionCreated` | `+0xfe` | `+0x103` | `nt!MiInitializeNewImageSectionProtos` | 이미지일 때. 헤더 페이지를 프로토 PTE 에 꽂음 | [정적] (run47 은 이름만) |
| `nt!MiInitializeNewImageSectionProtos` | `+0x1a2` · `+0x1cd` | `+0x1a7` · `+0x1d2` | `MiInitializeTransitionPfn` · `MiReferenceControlAreaPfn(CA, 0, 1)` | 프로토 PTE 가 비어 있고 `Prototype` 비트일 때 | [정적] |
| `nt!MiCreateNewSection` | `+0x6e2` | `+0x6e7` | `nt!MiFinalizeImageHeaderPage` | 꽂힌 쪽만 (MDL 칸 ≠ −1) | [정적] |
| `nt!MmCreateSpecialImageSection` | `+0x64` · `+0x86` | `+0x69` · `+0x8b` | `Feature_TrustedLaunchHosts…NoInline` · 세션 번호 함수 | run31 의 두 자리 | [정적] (run31 그대로) |

### (c) 공유 길 검사 (결과서 1-2 · 1-3 요약)

`MiShareExistingControlArea`:

| # | 줄 | 검사 | 실패하면 |
|---|---|---|---|
| S1 | `+0x28` `MiValidateControlAreaPartition` | 패킷 파티션 · 컨트롤 영역 파티션 번호 (`+0x3c & 0x3ff`) · `NoCrossPartitionAccess` | `0xC0000476` (패킷 비트 23 · 24 켬). test.exe (파티션 하나) 는 `+0x44` 로 통과 `[정적 + 가정]` |
| S2 | `+0x132 ~ +0x15c` | `PrefetchCreated` (CA `+0x38` bit9) 면 `MmChangeSectionBackingFile` 로 파일 객체 바꿈 | 반환값 안 봄 |
| S3 · S4 | `+0x45 ~ +0x4d` · `+0xcc ~ +0xe5` | 패킷 비트 2 켬 · 파일 잠금 풀기 | 없음 |
| S5 | `+0xe8 ~ +0xfb` | `SEC_IMAGE` 면 `MiValidateExistingImage` | 그 status 그대로 |
| S6 | `+0xfd ~ +0x17b` | `MaximumSize` 0 이면 패킷 `+0x80` ← `SizeOfSegment`. 있으면 더 클 때 쓰기 보호 필요 | `0xC0000040`. test.exe 는 `MaximumSize` 0 (`MmCreateSpecialImageSection +0xa1`) → 안 탐 |

`MiValidateExistingImage` (이유 전역 `nt+0xe2d838` 에 `0xc` · `0xd` · `0xe` · `0xf`):

| # | 줄 | 검사 | 실패하면 |
|---|---|---|---|
| V1 | `+0x31 ~ +0x35`, `+0x2bb ~ +0x2d6` | 이동식 미디어 (CA `+0x38` bit11) + 패킷 비트 18 | 이유 `0xc`, `0xC0000433` |
| V2 | `+0x51/+0x56` | `MiRelocateImageAgain` (아래) | 이유 `0xd`, 그 status |
| V3 | `+0xd6/+0xdb` | `SeGetImageRequiredSigningLevel` (패킷 비트 9 켜짐 · 비트 4 꺼짐일 때만) → 요구 수준을 패킷 `+0x18` 에 | 이유 `0xe`, 그 status |
| V4 ~ V6 | `+0x11b ~ +0x179`, `+0x307 ~ +0x347` | 지금 프로세스 `MitigationFlags` 비트 23 · 24 · 25, 패킷 비트 18 · 21 · 27, `Feature_TrustedLaunchHosts` → 정책 인자 모으기 | 실패 없음 |
| V7 | `+0x17f ~ +0x220` | 올라온 이미지 서명 수준 (세그먼트 `+0xf & 0xf`) vs 요구 수준 (CI 콜백 또는 `SeCompareSigningLevels`), `StrongCode`, `FORCE_INTEGRITY`, 감사 수준 → 비트 `ESI` 만 모음 | **여기선 status 안 만듦** |
| V8 | `+0x226 ~ +0x25d` | `MiValidateSectionSigningPolicy` — `ESI & 6` 이 0 이면 바로 0 (`+0x37 TEST SIL,6`) | 이유 `0xf`, 그 status |
| V9 | — | **파일 수정 시각 · 크기 · 내용 비교 — 두 함수 안에 없다** | — |

- 실패하면 (결과서 1-7): `MiCreateImageOrDataSection +0x1da JS → +0x521 MiDereferenceFailedControlArea` (컨트롤 영역은 안 지우고 참조만 되돌림) → status 반환 → `MiCreateSection` 정리. **`MiCreateNewSection` 으로 다시 가는 줄 없음** (Share 의 호출 11줄에 없음, flow-writer 도 `_calls.tsv` 로 확인).
- 예외 `0xC0000476` 만: `+0x26f JZ → +0x43e` (`CcWaitForUninitializeCacheMap` · `MiForceSectionClosed`) → `MiCreateSection +0x10a JZ → +0x21a` 가 패킷부터 다시 (둘째 바퀴의 `MiReferenceControlArea` 가 다시 갈림).
- 기준 주소 충돌 검사 ("이 프로세스 주소에 이미 뭔가 있나") 는 이 길에 없다. 그건 매핑 때 `MiMapViewOfImageSection +0x42a` (run102).
- CFG: `MiRelocateImageAgain +0xfc MiUpdateImageSystemWideBitmaps` — 새 기준을 골랐을 때만. 본문 이름만.

### (c) 기준 주소를 다시 고르는 조건 (`MiRelocateImageAgain`, 결과서 1-4)

| 줄 | 하는 일 | 갈림 |
|---|---|---|
| `+0x16 ~ +0x35` | 재배치 기록 (`[CA+0x60] & -8` 의 `+0x20`) 읽기 | **0 이면 바로 0 반환** (`RELOCS_STRIPPED` · `DYNAMICBASE` 없음 등 — run102 일찍 끝) |
| `+0x7b` · **`+0x80/+0x87`** | 재배치 락 → CA `+0x5c` 비트 22 `ImageActive` | **켜져 있으면 `+0x117` → 0 = 먼저 고른 주소 그대로.** 꺼졌을 때만 아래 |
| `+0xa3/+0xa8` | `MiReservePtes` (임시 PTE 하나) | 실패면 `+0x1a1 BTS [RBX+0x5c],0x16` (`ImageActive` 만 켬) 후 0 |
| `+0xd3/+0xd8` | `MiSelectImageBase` (넷째 인자 비트 0 늘 0, run73) | 음수면 되돌리고 status → V2 |
| **`+0xde`** | `BTS [RBX+0x5c],0x16` — `ImageActive` 켬 | |
| `+0xe6` · `+0xfc` | `MiGetControlAreaLoadConfig` · `MiUpdateImageSystemWideBitmaps` | 음수면 `+0x1b5 MiImageUnused` 로 끄고 status |
| `+0x10b/+0x113` | 새 기준 vs 옛 `BasedAddress` | **같으면 `MiSwitchBaseAddress` 안 탐** |
| `+0x13a ~ +0x14e` | `MiFlags & 0x2000` 이고 `StrongCode` ≠ 0 이면 `SeSetImageBaseAddress` | 음수면 되돌림 |
| `+0x150 ~ +0x16a` | 세션: 패킷 비트 5 면 `0xFFFFFFFF`, 아니면 세션 번호 함수 | |
| **`+0x17e/+0x183`** | **`MiSwitchBaseAddress(CA, 새 기준, 임시 PTE, 세션)`** | |

- `ImageActive` 를 **끄는** 곳: `MiImageUnused +0x83 BTR [RBX+0x5c],0x16` (같은 곳 `+0x7f` 에서 CA `+0x58` 에 −1 = 64KB 칸 반납, `+0x79 BT EAX,0x1d` 로 `SystemVaAllocated` 면 건너뜀). 부르는 곳: `MiCheckControlArea +0xc9` · `+0x3be`, `MiRelocateImageAgain +0x1b5`.
  - `MiCheckControlArea +0xc9` 조건 (결과서): 매핑된 뷰 (CA `+0x28`) 0 · 섹션 참조 (`+0x18`) 0 · `FilePointer` ≠ 0 · PFN 참조 (`+0x20`) ≠ 0 · `Image`. 곧 **뷰도 섹션 참조도 없어졌는데 페이지가 남아 컨트롤 영역이 살아 있으면 기준 주소 칸만 반납**한다.
  - 그래서 **그 컨트롤 영역을 다시 쓰는 다음 `SEC_IMAGE` 는 공유 길로 와서 기준을 새로 고른다** `[정적]`. 앞 test.exe 가 끝난 뒤 이 줄이 실제로 도는 때는 `[추측]` (동적 2).
  - 다른 프로세스에 계속 매핑된 DLL (`kernelbase` 류) 은 `ImageActive` 가 켜져 있어 같은 기준 그대로 `[정적: 흐름]`. KnownDlls 섹션은 섹션이 이미 있어 이 길 자체를 안 탄다.

`MiSwitchBaseAddress` (결과서 1-5, run102 `+0x46` · `+0x4e` 와 이어서):

| 줄 | 칸 | 전 → 후 |
|---|---|---|
| `+0x3a ~ +0x3e` | 재배치 기록 `+0x28` | 누적 델타 → 이번 델타 (걷는 동안) |
| **`+0x46`** | `_SEGMENT.BasedAddress` | 옛 → 새 |
| **`+0x4a ~ +0x4e`** | `TransferAddress` (`[세그먼트+0x38]` 의 `+0x0`) | `+= 새 − 옛` |
| `+0x51 ~ +0x58`, `+0xeb ~ +0xfa` (call `+0xf2`) | 기록 `+0x10` 목록마다 `MiApplyBytestreamFixup` (걷기 앞) | 뜻 `[공백]` |
| `+0x5e ~ +0x8a` | 적재 구성 `+0x18` 이 가리키는 칸 `+0x8` · `+0x10` 을 옮김 | 칸 이름 `[공백]` |
| `+0x9a/+0x9f` | `MiWalkEntireImage(…, 2, 세션)` — 올라와 있던 쪽 즉시 고침 | (run102) |
| `+0x9f ~ +0xa3` | 기록 `+0x28` | 이번 → 누적 (안 올라온 쪽은 나중 page-in 때 이 값으로) |
| `+0xa7 ~ +0xae`, `+0xd2 ~ +0xe9` (call `+0xdc`) | 같은 목록 한 번 더 (걷기 뒤) | 뜻 `[공백]` |
| `+0xb0 ~ +0xb6`, `+0xff ~ +0x129` | `SegmentFlags` 비트 11 (`DebugSymbolsLoaded`) 켜져 있으면 `DbgUnLoadImageSymbolsUnicode` 후 끔 (`AND word [RDI+0xc],0xf7ff`) | 켜짐 → 꺼짐 |

### (d) 섹션 특성 → 보호값 (`MiParseImageSectionHeaders +0x176 ~ +0x1e5`, 결과서 2-1)

- 인덱스 = `EXECUTE` (bit29) · `READ` (bit30) · `WRITE` (bit31) · `SHARED` (bit28) 네 비트 (`+0x17d ~ +0x1a5`). 값 = `MiImageProtectionArray` (`+0x1a8 LEA RCX,[0x14001a6b0]` · `+0x1af MOVSX R15D,[RAX+RCX]`).
- 배열 원본 바이트 `18 02 01 03 05 07 05 07 18 02 01 03 04 06 04 06`. `PAGE_*` 짝은 `MmUserProtectionToMask1/2` 원본 바이트로 맞춤 `[정적]`.

| EXEC · READ · WRITE | `SHARED` 꺼짐 | `SHARED` 켜짐 |
|---|---|---|
| — — — | `0x18` `PAGE_NOACCESS` | 같음 |
| X — — | 2 `PAGE_EXECUTE` | 같음 |
| — R — | 1 `PAGE_READONLY` | 같음 |
| X R — | 3 `PAGE_EXECUTE_READ` | 같음 |
| — — W 또는 — R W | **5 `PAGE_WRITECOPY`** | 4 `PAGE_READWRITE` |
| X — W 또는 X R W | **7 `PAGE_EXECUTE_WRITECOPY`** | 6 `PAGE_EXECUTE_READWRITE` |

- 요점: `WRITE` 가 있고 `SHARED` 가 없으면 **쓰기 시 복사**. `DISCARDABLE` 등 다른 비트는 안 본다.
- 이 값이 들어가는 곳:
  - 서브섹션 `+0x20` 비트 1 ~ 5 (`Protection`) — `+0x1c9 ~ +0x1d1`.
  - 프로토 PTE 비트 5 ~ 9 — 파일에서 읽을 쪽은 `MiMakeSubsectionPte` (`Prototype` = 비트 10, 서브섹션 주소 비트 16~), 0 으로 채울 쪽 (`VirtualSize > SizeOfRawData`) 은 `MiMakeDemandZeroPte` (비트 10 = 0), 이미지 밖은 `ZeroPte` (0).
  - 값 `& 2` (실행) 면 `ImageContainsCode` = 1 (`+0x1b4 ~ +0x1bf`).
  - 값 `& 4` (쓰기) 면: 쓰기 시 복사 (5 · 7) → 쪽마다 세그먼트 `+0x30` `ImageCommitment` +1 (`+0x249`) / 공유 쓰기 (4 · 6) → 서브섹션 `GlobalMemory` · CA `+0x38` 비트 17 `GlobalMemory` (`+0x381 MOV EAX,0x20000` · `+0x395 OR [R14+0x38],EAX`, flow-writer 가 줄 확인) · 쪽마다 세그먼트 `+0x10` `NumberOfCommittedPages` +1 (`+0x240`).
- 헤더 서브섹션 = 보호 1 (`PAGE_READONLY`, `+0x2e6 ~ +0x2f3`). .NET IL-only 만 `MiCreateNewSection +0x6b6/+0x6bb MiMakeImageReadOnly` (갈래 첫 줄 `+0x6b3`) 가 모두 1 로 덮음 (test.exe 는 CLR 디렉터리 비어 안 탐). 통째 한 덩어리 갈래 (`ImageMappedFlat`) 는 보호 7 하나 — test.exe 해당 없음.

test.exe 값 대입 (결과서 2-4):

| 서브섹션 | 특성 | 보호 | `PAGE_*` |
|---|---|---|---|
| 헤더 | — | 1 | `PAGE_READONLY` |
| `.text` | `0x60000020` | 3 | `PAGE_EXECUTE_READ` |
| `.rdata` · `.pdata` · `.reloc` | `0x40000040` · `0x40000040` · `0x42000040` | 1 | `PAGE_READONLY` |
| `.data` · `.fptable` | `0xC0000040` | 5 | `PAGE_WRITECOPY` |

### (d) 헤더 페이지 차례 (결과서 2-3)

| 순서 | 줄 | 하는 일 |
|---|---|---|
| 1 | `MiCreateNewSection +0x215 MiCreateImageFileMap` | `+0xdb MiCreateMdl(…, 0x1000, 1)` 이 `MiGetPageForHeader` 로 **물리 페이지 하나** → 헤더 읽기 → `MiVerifyImageHeader` → `MiBuildImageControlArea` (위 보호값) → `MiInitializeImageHeaderPage` (꼬리 0) |
| 2 | `+0x167 MiSectionCreated → +0x103 MiInitializeNewImageSectionProtos` | CA 를 `ImageSectionObject` 에 꽂고 (`+0x124`) `BeingCreated` 끈 (`+0xa6`) **뒤**. MDL 마다 (`0x4` 풀 버퍼 MDL 은 건너뜀 `+0x3d/+0x41`): 프로토 PTE 가 비어 있고 `Prototype` 이면 **`+0x1a2 MiInitializeTransitionPfn`** 로 그 물리 페이지를 transition 으로 꽂고, `OriginalPte` 비트 10 이면 **`+0x1cd MiReferenceControlAreaPfn(CA, 0, 1)`** (= `+0x36 ADD [RDI+0x20],RBP` — CA `NumberOfPfnReferences` += 1, flow-writer 확인). 이미 다른 페이지가 있으면 우리 페이지를 버리고 MDL 칸 −1 (`+0x110` · `+0x115`) |
| 3 | `+0x3da MiRelocateImage` | (run102) |
| 4 | `+0x6e2 MiFinalizeImageHeaderPage` | MDL 칸이 −1 이 아닌 쪽만 — MDL 이 쥔 잠금 · 참조를 풂 → 프로토가 가진 대기 페이지 |
| 5 · 6 | `+0x41e ~ +0x460` · `+0x468 MiReleaseImageSection` | MDL 해제 · (run102) |

- test.exe: `SizeOfHeaders 0x400` → 헤더 쪽 1 → **2 번이 한 쪽을 꽂고 4 번이 그 한 쪽을 내려놓는다고 읽힌다** `[정적]` (실제 갈래는 동적 7).
- `MiCreateImageFileMap +0x3e1 MiInitializeImageProtos` (작은 이미지 통째 읽기) 는 별개의 길 — 안 읽음.

### (e) 작은 것 넷 (결과서 3)

- **`MiVerifyImageHeader` `0x57` · `0x58` · `0x59`** (헤더 마지막 검사, `+0x259 ~ +0x27e`, 모두 `0xC000007B`):

| 이유 | 조건 |
|---|---|
| `0x57` | x86/x64 (`MiLegacyImageArchitecture`) **이고** `RELOCS_STRIPPED` (`+0x259` · `+0x263`) **이고** `DllCharacteristics` 비트 12 `APPCONTAINER` (`+0x54f BT word [R11+0x3c],0xc`) |
| `0x58` | x86/x64 아님 **이고** `RELOCS_STRIPPED` |
| `0x59` | x86/x64 아님 · `RELOCS_STRIPPED` 꺼짐 **이고** `DllCharacteristics & 0x140 ≠ 0x140` (`DYNAMIC_BASE` + `NX_COMPAT` 둘 다 필요) |

  - test.exe: `Machine 0x8664` · `Characteristics 0x22` (`RELOCS_STRIPPED` 꺼짐) → `+0x274 TEST R8W,R8W` 0 → `+0x27e` 성공. **셋 다 안 탄다** `[정적]` (값은 run44 K1 `ImageCharacteristics 0x22` `[관찰]` 과 같음).
- **`Feature_TrustedLaunchHosts…`**: 한 비트짜리 WIL 기능 스위치 (전역 `…featureState` `.data` rva `0xfc40f8`, 파일 초기값 없음 — 실행 때 채움). 켜졌을 때 갈래 (결과서 3-2):
  - `MmCreateSpecialImageSection +0x64` → 호출자 플래그 비트 3 이 있으면 만들기 플래그 `|= 0x20`.
  - `MiInitializeCreateSectionPacket +0x1a5` → 만들기 플래그 `& 0x20` 이면 패킷 비트 27.
  - `MiCreateNewSection +0x338` · `MiValidateExistingImage +0x16a` → 패킷 비트 27 이면 서명 정책 인자 비트 21.
  - `NtCreateUserProcess +0x7c3` → 토큰 복제 함수가 갈림 (`SeDuplicateTokenWithPredictedClaims` / `SeDuplicateTokenAndAddOriginClaim`).
  - test.exe 길에서 **부르는 줄은 늘 지난다** `[정적]`. 값 (켜짐/꺼짐) 은 실행 때 값 → 동적. 켜져도 서명 정책까지 닿으려면 `NtCreateUserProcess [RBP+0x328]` 비트 3 이 서야 하는데 그 출처는 `[공백]`.
- **세션 번호 함수** (run31 `+0x86`, ret `+0x8b`): nt rva `0x2e8280`, PDB 이름 **둘** — `PsGetProcessSessionIdEx` · `PsGetSessionIdEx` (`pdb_aliases.py --lookup ntoskrnl.exe 0x2e8280`, 결과서). 하는 일 (34바이트): `EPROCESS +0x2e8` (`Session`) 이 0 이거나 `Flags3.SystemProcess` (`+0x5fc & 0x1000`) 면 `0xFFFFFFFF`, 아니면 `[Session+0x8]`. 인자 = **지금 프로세스 (만드는 쪽 = 부모 cmd)**. 결과는 패킷 `+0xa8`. `PsGetProcessSessionId` (rva `0x379000`) 는 다른 함수.
- **`MiCreateSection` 안 재시도**: `+0xfa CMP EAX,0xC0000054 / +0xff JZ` · `+0x105 CMP EAX,0xC0000476 / +0x10a JZ` → `+0x21a` (파티션 참조 풀고 패킷 비트 24 만 남김) → `+0x237 JMP +0x48` (`memset` 부터 다시). 횟수 세는 줄 없음, 안 쉼. `0xC0000054` 는 `MiCreateImageFileMap +0x108 MiFlushDataSection` · `MiReadImageHeaders` 에서 올 수 있음 (이름 `STATUS_FILE_LOCK_CONFLICT` 는 `[추측]`). 바깥쪽 `MmCreateSpecialImageSection +0xea KeDelayExecutionThread` (반 초 쉬고) 는 run31 그대로.

### 사진 로그 대조 (run44, 새로 돌린 것 아님 — flow-writer)

| 무엇 | 값 | 근거 | 표시 |
|---|---|---|---|
| exe VAD 보호 | `EXECUTE_WRITECOPY` (exe · ntdll 둘 다) | `ms/logs/04_baggage-read_P1a.txt:1550` · `1551` | [26100.9457 확인] |
| exe CA `u` (`+0x38`) · `u2` 둘째 dword (`+0x5c`) · `NumberOfPfnReferences` | `0xa0` · `0x00c00009` · 3 | 짐 지도 3.21 사진 칸 (run47 이 K1 로그에서 옮긴 값) | [26100.9457 확인] (run47 경유) |

- 읽는 법 (값 `[관찰]` + 뜻 `[정적]`/`[추측]`):
  - VAD 보호 `EXECUTE_WRITECOPY` = 보호 값 7 = 세그먼트 `DefaultProtectionMask` 7 (짐 지도 3.22, `MiParseImageSectionHeaders +0x3c ~ +0x43`) 과 같은 수다. 섹션별 보호 (위 표) 는 VAD 한 줄로는 안 보인다 `[추측: 짝 맞추기]`.
  - `u` `0xa0` 에 비트 9 (`PrefetchCreated`) · 비트 17 (`GlobalMemory`) 이 없다 → 프리페처가 만든 컨트롤 영역이 아니고, 공유 쓰기 섹션도 없다 (test.exe 특성 값과 맞음).
  - `+0x5c` `0x00c00009` 의 비트 22 (`ImageActive`) · 23 (`ImageBaseOkToReuse`) 켜짐, 비트 18 ~ 19 (`StrongCode`) 0. 사진 K1 때 기준 주소를 쥐고 있었다는 것뿐, 이번 실행이 새 길이었는지는 그대로 Q61.
  - `NumberOfPfnReferences` 3 중 1 은 헤더 페이지 꽂기 (`MiReferenceControlAreaPfn`) 로 읽힌다 `[추측: 새 길일 때]`. 나머지 2 는 그대로 모름.

### 호출 관계

섹션 만들 때 (부모 cmd 스레드, 그림 1. 트리 숫자는 ret 자리):

```
nt!MiCreateSection
 ├─ +0xf8  MiCreateImageOrDataSection                              [정적] run31
 │    ├─ +0x1d5  MiShareExistingControlArea   (있을 때)             [정적] run47 · run103
 │    │    ├─ +0x2d   MiValidateControlAreaPartition    실패 0xC0000476 → MiCreateSection +0x21a 재시도
 │    │    ├─ +0x15c  MmChangeSectionBackingFile        (PrefetchCreated 일 때만)
 │    │    └─ +0xf9   MiValidateExistingImage          실패 → status 로 끝 (새로 안 만듦)
 │    │         ├─ +0x56   MiRelocateImageAgain         ImageActive 켜짐 → 그대로 (+0x87)
 │    │         │    ├─ +0xd8   MiSelectImageBase       꺼짐일 때만, +0xde ImageActive 켬
 │    │         │    └─ +0x183  MiSwitchBaseAddress     새 ≠ 옛: +0x46 BasedAddress · +0x4e TransferAddress
 │    │         ├─ +0xdb   SeGetImageRequiredSigningLevel
 │    │         ├─ +0x16f  Feature_TrustedLaunchHosts…
 │    │         ├─ +0x1ec · +0x20e  SeCompareSigningLevels  (또는 +0x2ab CI 콜백)
 │    │         └─ +0x25d  MiValidateSectionSigningPolicy
 │    └─ +0x385  MiCreateNewSection           (없을 때)             [정적] run31
 │         ├─ +0x215  MiCreateImageFileMap
 │         │    └─ … +0x2e6  MiBuildImageControlArea
 │         │              └─ +0x367  MiParseImageSectionHeaders  ★ 섹션마다 보호값 (MiImageProtectionArray)   [정적] run103
 │         ├─ +0x167  MiSectionCreated
 │         │    └─ +0x103  MiInitializeNewImageSectionProtos  헤더 페이지 → 프로토 PTE (transition)          [정적] run103
 │         ├─ +0x3da  MiRelocateImage                                                                          [정적] run102
 │         └─ +0x6e7  MiFinalizeImageHeaderPage                꽂힌 쪽만 내려놓기                               [정적] run103
 └─ +0xfa ~ +0x10a  0xC0000054 · 0xC0000476 이면 +0x237 JMP +0x48 (패킷부터 다시)                               [정적] run103
```

`ImageActive` 를 끄는 쪽 (앞 실행이 끝난 뒤 — 지도 범위 밖):

```
nt!MiCheckControlArea  +0xce · +0x3c3
 └─ MiImageUnused  +0x7f CA+0x58 = −1 (64KB 칸 반납) · +0x83 BTR [CA+0x5c],22 (ImageActive 끔)
```

## 확인된 것

- `[정적]` **공유 길은 새 컨트롤 영역을 만들지 않는다.** 검사 실패는 status 로 끝나고 (`MiDereferenceFailedControlArea` 로 참조만 되돌림), `MiCreateNewSection` 으로 다시 가는 줄이 없다. 예외는 파티션 불일치 `0xC0000476` 하나 — `MiCreateSection +0x21a` 가 처음부터 다시 갈림을 한다.
- `[정적]` 공유 길 검사 = 파티션 · 이동식 미디어 (`0xC0000433`) · 기준 주소 · 서명 (요구 수준 · 비교 · 정책). **파일 바뀜 비교 줄은 없다.**
- `[정적]` **기준 주소는 `ImageActive` 가 꺼졌을 때만 다시 고른다** (`MiRelocateImageAgain +0x80/+0x87`). 끄는 곳 = `MiImageUnused +0x83` (← `MiCheckControlArea +0xc9` — 뷰 0 · 섹션 참조 0 인데 페이지가 남은 컨트롤 영역). `MiSwitchBaseAddress` 가 도는 조건 여섯: 재배치 기록 있음 · `ImageActive` 꺼짐 · 임시 PTE 성공 · `MiSelectImageBase` 성공 · 비트맵 · CI 갱신 성공 · 새 ≠ 옛.
- `[정적]` **섹션 보호값은 `MiParseImageSectionHeaders +0x1a8 ~ +0x1d1` 가 `MiImageProtectionArray` 로 채운다.** `WRITE` 만 있으면 쓰기 시 복사 (5 · 7), `SHARED` 까지면 진짜 읽기쓰기 (4 · 6). test.exe `.data` · `.fptable` = `PAGE_WRITECOPY`.
- `[정적]` `MiInitializeNewImageSectionProtos` 는 보호값을 안 채운다 — 헤더 페이지 물리 쪽을 프로토 PTE 에 transition 으로 꽂고 (`+0x1a2`), CA `NumberOfPfnReferences` 를 1 올린다 (`+0x1cd` → `MiReferenceControlAreaPfn +0x36`). 차례: `MiCreateImageFileMap` → `MiSectionCreated +0x103` (꽂기) → `MiRelocateImage` → `MiCreateNewSection +0x6e2 MiFinalizeImageHeaderPage` (내려놓기).
- `[정적]` 프로토 PTE 모양: 파일 쪽 `MiMakeSubsectionPte` (`Prototype` 비트 10 · 보호 비트 5 ~ 9 · 서브섹션 주소), 0 쪽 `MiMakeDemandZeroPte`, 이미지 밖 `ZeroPte`. run31 의 "PTE 뜻은 이름으로 본 것 `[추측]`" 이 닫힌다.
- `[정적]` `0x57` ~ `0x59` 조건 셋 (위 표), test.exe 는 셋 다 안 탐.
- `[정적]` 세션 번호 함수 = rva `0x2e8280`, 이름 둘 `PsGetProcessSessionIdEx` · `PsGetSessionIdEx`. 만드는 쪽 (부모) 프로세스의 세션, 시스템 프로세스면 `0xFFFFFFFF`.
- `[정적]` `MiCreateSection` 안 재시도 = `0xC0000054` · `0xC0000476` (안 쉬고 패킷부터).
- `[정적]` `Feature_TrustedLaunchHosts` = WIL 한 비트 스위치. test.exe 길에서 부르는 줄은 늘 지나고, 값은 실행 때.
- `[관찰]` + `[추측: 짝]` exe VAD 보호 `EXECUTE_WRITECOPY` (`_P1a.txt:1550`) = `DefaultProtectionMask` 7.

## 결과서와 다르게 적은 곳 · 보탠 것

- 다르게 적은 곳: 하나. 결과서 2-1 의 `MiCreateNewSection +0x3e4/+0x3e9 → +0x6b3 MiMakeImageReadOnly` 의 `+0x6b3` 은 갈래 첫 줄이고, CALL 은 `+0x6b6` (ret `+0x6bb`) 이다 (`_calls.tsv`). 다른 줄은 모두 일치.
- 보탠 것 (flow-writer `.asm` · `_calls.tsv`):
  - `MiReferenceControlAreaPfn +0x36 ADD [RDI+0x20],RBP` (`RDI` = CA, `RBP` = 셋째 인자 1 — `MiInitializeNewImageSectionProtos +0x1c4 ~ +0x1c9` 가 `EDX=0` · `R8D=1`, `RCX=R13` = 첫 인자 CA `+0x2e`) — 결과서는 "CA `+0x20` += 1" 까지, 쓰는 줄을 짚음. 짐 지도 3.21 `NumberOfPfnReferences` 에 첫 `[정적]` 쓰는 줄.
  - CA `GlobalMemory` 를 켜는 줄 `MiParseImageSectionHeaders +0x381 MOV EAX,0x20000` · `+0x395 OR [R14+0x38],EAX` (결과서는 구간 `+0x381 ~ +0x399`).
  - `MiImageUnused +0x7f MOV [RBX+0x58],R8D` (64KB 칸 반납) · `+0x79 BT EAX,0x1d` (`SystemVaAllocated` 면 건너뜀).
  - `MiShareExistingControlArea` 호출 11줄에 `+0x69/+0x6e FsRtlGetFileSize` · `+0x77/+0x7c` · `+0x7f/+0x84` 잠금 풀기 짝이 더 있다 (결과서 1-1 줄 순서에 빠진 것 — 뜻은 안 따라감, `MiCreateNewSection` 은 없음 그대로).
  - 사진 대조 표 (위).

## 정정

- **run73 (`runs/03_sprint/run73-image-base-select.md`) "같은 파일 컨트롤 영역이 살아 있으면 `MiRelocateImageAgain +0x87 JNZ` 로 먼저 고른 주소를 그대로 쓴다"** → 반만 맞다. 그대로 쓰는 건 CA `+0x5c` 비트 22 (`ImageActive`) 가 **켜져 있을 때뿐**이다. 컨트롤 영역은 살아 있어도 뷰 · 섹션 참조가 다 없어지면 `MiImageUnused +0x83` 이 비트를 끄고 (← `MiCheckControlArea +0xc9`), 그다음 공유 길은 `MiSelectImageBase` 로 **다시 고른다** (`+0xd3`). run73 문서 맨 위에 정정 줄을 넣었다.
  - 같은 표현이 flow.md 2.1 `MiRelocateImage` 절 (run73 줄) · 2.6 (run73 아래 줄) 에 있어 고쳤다.
  - 짐 지도 3.22 `BasedAddress` · 3.23 `TransferAddress` 의 공유 길 줄 (run102 때 씀) "공유 길이어도 앞 실행의 `MiRelocateImage` 가 비트 22 를 켜 두므로 안 탐 `[정적: 흐름]`" 도 같은 잘못이라 고쳤다 (run102 문서에는 이 문장이 없어 정정 줄은 안 넣음).
- **요청서 전제 "`MiInitializeNewImageSectionProtos` 가 프로토 PTE 를 섹션마다 보호값으로 채운다"** → 보호값은 `MiParseImageSectionHeaders` (`MiImageProtectionArray`) 가 더 앞에서 채운다. 이 전제는 sast/101 요청서에만 있다 — run31 · run47 · run102 · flow.md 는 "헤더 페이지 → 프로토 PTE (`MiInitializeNewImageSectionProtos`)" · "이름만" 으로만 적었다. 그래서 이전 run 문서 맨 위 정정 줄은 넣지 않았다. 요청서 파일은 고치지 않는다.
- PE_emulator `Section.cpp:418` 주석 ("`MiRelocateImageAgain` 으로 먼저 고른 주소를 씀") 도 위 run73 과 같은 반쪽이라고 결과서가 적었다 (그 파일은 flow-writer 가 안 읽음 — 아래 PE_emulator 쪽).

## 이전 run 에서 닫은 것

- run31 "아직 확인 필요" **Q62 (c) · (d) · (e)** — 닫힘 (Q62b). Q62 전체 정적 끝 (a · b · f 는 run102).
- run102 "아직 확인 필요" 정적 1 (Q62b) — 닫힘.
- run47 "이름만 둔 줄기 밖 함수" 중 `MiInitializeNewImageSectionProtos` · `MiValidateExistingImage` — 본문 읽음.
- flow.md 2.1 `MiCreateImageFileMap` 절 "(PTE 의 뜻은 함수 이름으로 본 것 `[추측]`)" → `[정적]`.

## 아직 확인 필요

### 동적 (`[VM]`, 결과서 "동적분석으로 확인해야 할 것")

멈춤점은 모두 커널이라 test.exe 조건을 붙인다. 주소는 이 빌드 rva 기준 (`nt+…`).

1. **test.exe 가 공유 길을 타나** (Q61 과 같은 실험 — 같은 부팅에서 두 번째 실행): `nt+0x96e6cc` (`MiShareExistingControlArea` 입구) vs `nt+0x96f4d0` (`MiCreateNewSection` 입구).
2. **`ImageActive` 값**: `nt+0x971d68` (`MiRelocateImageAgain +0x80`) 에서 `dd @rbx+0x5c L1` 비트 22 (`0x400000`). 첫 test.exe 가 끝난 뒤면 꺼져 있을 것 `[추측]`. 꺼졌으면 `nt+0x97330c` (`MiSwitchBaseAddress` 입구) 가 서고, `nt+0x97335a` (`+0x4e`) 직전 `rcx` · `rax`, 전후 `dq @rdi+0x20 L1` (`BasedAddress`). 같이 `nt+0x479bac` (`MiImageUnused` 입구) 가 첫 실행 끝 무렵 서나.
3. **서명 검사 갈래**: `nt+0x96fcaf` (`MiValidateExistingImage +0xdb`) `eax` (0 기대) · `[rsp+0x90]` (요구 수준), `nt+0x96fd43` (`+0x16f`) `eax` (Feature 값), `nt+0x96fe31` (`+0x25d`) `eax` (0 기대). `dd nt+0xfc40f8 L1` (feature 상태, 비트 4 · 0), `dd nt+0xe2d838 L1` (이유 전역).
4. **비교 비트 `ESI`**: `nt+0x96fe2c` (`+0x258`) `esi` — `0x6` 이 0 이면 정책 함수가 바로 0.
5. **재시도 안 타나**: `nt+0x96d7fa` (`MiCreateSection +0x21a`) 가 안 서야 맞음. 서면 직전 `MiCreateImageOrDataSection` 반환 `eax` (`nt+0x96d6d8` 직후).
6. **보호값** (새 길일 때): `nt+0x971844` (`MiParseImageSectionHeaders +0x1b4`) 에서 `r15d` 가 `3, 1, 5, 1, 5, 1` 순서. 상태 사진이 더 싸다 — 만든 CA 의 서브섹션 사슬 `[sub+0x20]` 비트 1 ~ 5 가 `1, 3, 1, 5, 1, 5, 1` (헤더 포함 7칸). run44 덤프 (`ms/dump/03_baggage-snap/` K1) 를 오프라인으로 읽어도 될 것 `[추측]`.
7. **헤더 페이지 꽂기**: `nt+0x315c52` (`MiInitializeNewImageSectionProtos +0x1a2`) 1번, `nt+0x4ab9a8` (`MiFinalizeImageHeaderPage` 입구) 1번 서나.
8. 추천 한 번에: `nt+0x96e6cc`, `nt+0x971d68`, `nt+0x97330c`, `nt+0x96fd43` (test.exe 조건).

### 정적 (결과서 "다음 요청 후보" + 보탬)

1. **`MiReferenceControlArea` 세 갈래** (비었음 / 멀쩡 / 만드는 중 · 지우는 중) 와 status (`0xC000060B` · `0xC0000476`), `MiCheckControlArea +0xb1 JZ → +0x29c` (PFN 참조 0 — 지우는 길로 읽힘). "컨트롤 영역이 언제 사라지나" 를 닫음.
2. **`MiApplyBytestreamFixup` + 재배치 기록 `+0x10` 목록**, 적재 구성 `+0x18` 이 가리키는 칸 둘 (`MiSwitchBaseAddress`), 출처 `MiParseImageLoadConfig`.
3. **`MiParseImageSectionHeaders` 이유 `0x10 ~ 0x19` · `MiBuildImageControlArea` `0x1a ~ 0x1f`** 표 — `MiVerifyImageHeader` 표와 한 장으로 (PE_emulator `_CheckImageHeader` 빠진 검사 채우기).
4. `SeGetImageRequiredSigningLevel` · `SeCompareSigningLevels` · `MiValidateSectionCreate` 본문.
5. `NtCreateUserProcess [RBP+0x328]` 비트 3 의 출처 — `Feature_TrustedLaunchHosts` 가 test.exe 길에서 실제 갈래를 바꾸나.
6. (보탬, 작음) `MiShareExistingControlArea +0x69 FsRtlGetFileSize` · `+0x77 ~ +0x84` 갈래가 무엇을 보나.
7. (보탬, 작음) `NumberOfPfnReferences` 사진 K1 값 3 의 나머지 2 — 누가 올리나 (짐 지도 3.21, D5 와 같이).

### PE_emulator 쪽 (결과서 1-8 · 2-5 · 3-5 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- 공유 길 `MiShareExistingControlArea`: 우리에 없음 (늘 새 섹션). test.exe 한 실행에 같은 파일을 두 번 `SEC_IMAGE` 하지 않으면 결과 같음 — **빠짐, 영향 작음**.
- `Section.cpp:418` 주석 "`MiRelocateImageAgain` 으로 먼저 고른 주소를 씀" — **반만 맞음** (`ImageActive` 켜졌을 때만). 정정 후보.
- 보호: `AddressSpace.cpp::_GetSectionProtection` 은 `EXECUTE/READ/WRITE` 만 옮김 → 효과는 같음. 그러나 **`.data` 를 `PAGE_*` 로 되읽으면 진짜 `PAGE_WRITECOPY` (`0x08`), 우리 `VirtualMemory.cpp::_FromUnicornProtection` 은 `PAGE_READWRITE`** (`SHARED` 구분도 없음). 게스트가 `VirtualQuery` / `NtQueryVirtualMemory` 로 이미지 쪽을 되읽는 길은 확인 필요.
- `ImageContainsCode` 를 쓰는 곳 못 찾음 — 확인 필요.
- **`0x57` 검사 빠짐** (`_CheckImageFields` 주석 "Q62(e) 미분석" — 이제 근거 있음). x64 만이면 `RELOCS_STRIPPED` + `APPCONTAINER` → `0xC000007B` 하나. test.exe · dll1 · dll2 는 안 걸림.
- `Feature_TrustedLaunchHosts` 없음 · 세션 나누지 않음 · `MiCreateSection` 재시도 없음 — 맞음 (세션: 부모가 시스템 프로세스 취급이면 `0xFFFFFFFF` 인 점은 확인 필요).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntkrnlmp 줄에 run103, 근거 로그 (run103) 한 줄), 그림 1 (`i2` · `i5` 상자 글만 — 노드 · 화살표 그대로, 제목 run103), 2.1 (절 머리 · 트리 네 줄 (`+0xf9 MiValidateExistingImage` · `+0x367 MiParseImageSectionHeaders` · `+0x103 MiInitializeNewImageSectionProtos` · `+0x6e7 MiFinalizeImageHeaderPage`) · `MmCreateSpecialImageSection` (Feature · 세션 번호) · `MiCreateSection` → `MiCreateImageOrDataSection` (공유 길 검사 · 실패 · 안 재시도) · `MiCreateNewSection` (헤더 페이지 차례) · `MiCreateImageFileMap` (`[추측]` → `[정적]`) · `MiVerifyImageHeader` (`0x57` ~ `0x59`) · `MiBuildImageControlArea` (보호값 표) · `MiRelocateImage` (run73 줄 정정 · 둘째 길 조건)), 2.6 (run73 아래 줄 정정), 4장 (2.1 Q62 줄 (c)(d)(e) 해결됨 · Q61 줄 끝에 run103 메모 · run102 정적 ① (해결됨), run103 새 줄 셋 — 동적 · 정적 · PE_emulator), 4.1 (그림 1 nt 섹션 줄 메모), 5장 (run103 · sast/101).
- baggage.md: 기준 (run103), 판 (쉰셋째판), 2장 합 문장 (정확 684 → 685 · 짐작 54 → 53 — `NumberOfPfnReferences` 짐작 → 정확), 3장 머리 메모, 3.20 `SessionId` 줄 (세션 번호 출처), 3.21 (머리 숫자 · 만드는 곳 메모 · 사진 메모 · `NumberOfPfnReferences` 첫 줄 고침 · 새 줄 6 — `NumberOfPfnReferences` 헤더 페이지 · `u` `PrefetchCreated` · `u` `GlobalMemory` · `ImageActive` 다시 켬 · `ImageActive` 끔 (범위 밖) · `FilePointer` 프리페처 갈래), 3.22 (머리 숫자 · 만드는 곳 메모 · 새 줄 1 `DebugSymbolsLoaded` 끔 · 고친 줄 3 `NumberOfCommittedPages` · `ImageCommitment` 조건 · `BasedAddress` 공유 길 줄 정정), 3.23 (고친 줄 1 `TransferAddress` 공유 길 줄 정정), 7.6 메모 (서브섹션 보호 · 프로토 PTE 모양 — 체크리스트 밖), 새 7.35, 8장. **새 짐 칸 없음** (서브섹션 · 프로토 PTE 는 체크리스트 밖이라 7.6 에만).

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이 길이 디버거 칸을 읽는 줄은 없다 `[정적: 읽은 줄 안에서]`. 단 **같은 부팅에서 test.exe 를 여러 번 돌리면** 공유 길 · `ImageActive` 상태가 바뀌어 `MiSwitchBaseAddress` 가 설 수 있다 (동적 1 · 2 — run102 7.34 의 메모와 같은 갈림, 이번엔 "다시 고를 수도 있다" 쪽이 더해짐). 프리페처가 먼저 만든 컨트롤 영역이면 `MmChangeSectionBackingFile` 갈래가 생긴다 (사진 K1 은 아님).
- 도구 (결과서): `MiValidateExistingImage +0x2a6/+0x2ab` 가 `_calls.tsv` 에 `indirect:cfg:?` 로 찍힌다 — reference.md "알려진 한계" (호출 자리가 `Jcc` 로만 들어오는 갈래) 그대로라 pitfalls 제안은 안 한다.

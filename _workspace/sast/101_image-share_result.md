# 101 image-share — 정적 분석 결과

- 요청서: `_workspace/sast/101_image-share.md` (Q62b = Q62 (c)·(d)·(e))
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID와 같음). **analyze 다시 안 함** (`.gpr` · `_index.tsv` · analyze 로그 `SetPdb:` 줄 모두 있음, 도는 Ghidra 없음). ntdll 은 안 건드렸다.
- 새로 뽑은 것 (모두 depth 0, `export ... '^이름$' 0`): `MiMakeSubsectionPte` · `MiMakeDemandZeroPte` · `MiReferenceControlAreaPfn` · `PsGetProcessSessionIdEx` · `Feature_TrustedLaunchHosts__private_IsEnabledDeviceUsageNoInline` · `Feature_TrustedLaunchHosts__private_IsEnabledFallback` · `wil_details_IsEnabledFallback` · `MiImageUnused` · `MiDereferenceFailedControlArea` · `MiValidateControlAreaPartition` · `MiGetControlAreaLoadConfig` · `MiCheckControlArea` · `MiMakeProtectionMask`.
  - 넓힌 이유: 요청서 표 밖이지만 질문의 "조건 칸"을 읽으려면 필요한 작은 함수들 (각 20 ~ 160 바이트, `MiCheckControlArea` 만 1069 바이트 — "기준 주소 칸이 언제 반납되나" 한 줄 때문에 `+0xc9` 근처만 읽음).
- 있던 것 그대로 읽음 (다시 안 뽑음): `MiShareExistingControlArea` · `MiValidateExistingImage` · `MiRelocateImageAgain` · `MiSwitchBaseAddress` · `MiSectionCreated` · `MiInitializeNewImageSectionProtos` · `MiFinalizeImageHeaderPage` · `MiVerifyImageHeader` · `MiLegacyImageArchitecture` · `MiCreateSection` · `MiCreateNewSection` · `MiCreateImageOrDataSection` · `MiCreateImageFileMap` · `MiBuildImageControlArea` · `MiParseImageSectionHeaders` · `MiInitializeImageProtos` · `MiInitializeImageHeaderPage` · `MiCreateMdl` · `MiMakeImageReadOnly` · `MiInitializeCreateSectionPacket` · `MmCreateSpecialImageSection` · `MiValidateSectionSigningPolicy` · `MiUpdateImageSystemWideBitmaps` · `NtCreateUserProcess`.
- 원본 바이트로 읽은 것: `MiImageProtectionArray` (`.rdata` rva `0x1a6b0`, 16바이트), `MmUserProtectionToMask1/2` (rva `0x3b020`, `0x3b030`), 특성 값은 `sample/bin/test.exe` 의 PE 섹션 표 (파이썬으로 헤더 읽음).
- 버전 확인: `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 둘 다 `_calls.tsv` 와 일치. **일치.**
- 스크립트 문제: 없음. 참고 하나: `MiValidateExistingImage +0x2a6/+0x2ab` 가 `_calls.tsv` 에 `indirect:cfg:?` 로 찍힌다 (reference.md 의 "알려진 한계" — 호출 자리가 `Jcc` 로만 들어오는 갈래). `.asm` 으로 직접 확인: `+0x1ca MOV R10,[0x140f04cf8]` (`SeCiCallbacks+0x98` 주석) → `+0x291 MOV RAX,R10` → `+0x2a6 CALL` 이라 대상은 그 칸의 함수 포인터.

## 한눈에 (답만)

1. **(c)** 공유 길에서 `MiShareExistingControlArea` 는 새 컨트롤 영역을 **만들지 않는다.** 검사 실패는 status 로 끝난다 (`MiCreateNewSection` 으로 다시 안 감). 한 가지 예외: 파티션 불일치 `0xC0000476` 만 `MiCreateSection` 이 패킷부터 다시 한다.
2. **(c)** `MiValidateExistingImage` 의 검사 = (1) 이동식 미디어 + 패킷 비트 18 → `0xC0000433`, (2) `MiRelocateImageAgain` (기준 주소·CFG 비트맵), (3) 서명: 요구 수준 → 이미 올라온 이미지의 수준과 비교 → `MiValidateSectionSigningPolicy`. **파일 바뀜(시간·크기) 비교 줄은 없다.**
3. **(c)** 기준 주소는 컨트롤 영역 `+0x5c` 비트 22 (`ImageActive`) 가 **꺼져 있을 때만** 다시 고른다. 켜져 있으면 먼저 고른 주소를 그대로 쓴다 (`MiRelocateImageAgain +0x80/+0x87`). 다시 골라 값이 다르면 `MiSwitchBaseAddress` 가 `BasedAddress` ← 새 값, `TransferAddress += (새 − 옛)`.
4. **(d) 전제 정정:** 프로토 PTE 에 **보호값을 채우는 곳은 `MiInitializeNewImageSectionProtos` 가 아니다.** 보호값은 `MiParseImageSectionHeaders +0x176 ~ +0x1c9` (표 `MiImageProtectionArray`) 가 섹션 만들기 **앞에서** 이미 채운다. `MiInitializeNewImageSectionProtos` 는 **헤더 페이지의 물리 페이지를 프로토 PTE 에 transition 으로 꽂는 일**을 한다.
5. **(d)** 차례: `MiCreateImageFileMap` (헤더 읽기 · 검사 · 보호값 채우기) → `MiSectionCreated +0x103 MiInitializeNewImageSectionProtos` (헤더 페이지를 프로토에 꽂음) → `MiRelocateImage` → `MiCreateNewSection +0x6e2 MiFinalizeImageHeaderPage` (꽂은 페이지의 MDL 잠금·참조를 풂).
6. **(e)** `0x57` = x86/x64 + `RELOCS_STRIPPED` + `DllCharacteristics` 비트 12 (AppContainer). `0x58` = x86/x64 아닌 머신 + `RELOCS_STRIPPED`. `0x59` = x86/x64 아닌 머신 + `DllCharacteristics & 0x140 ≠ 0x140`. 셋 다 `0xC000007B`. **test.exe 는 셋 다 안 탄다.**
7. **(e)** `Feature_TrustedLaunchHosts` = 기능 스위치. test.exe 길에서 부르는 줄은 늘 탄다 (값은 실행 때 값). 켜져도 갈래가 바뀌려면 만들기 정보 비트가 더 서야 한다.
8. **(e)** `+0x86` 세션 id 함수 = nt `+0x2e8280`, PDB 이름 **둘**: `PsGetProcessSessionIdEx`, `PsGetSessionIdEx`.
9. **(e)** `MiCreateSection` 재시도 = `MiCreateImageOrDataSection` 이 `0xC0000054` 또는 `0xC0000476` 을 돌려줄 때 (`+0xfa ~ +0x10a`).

---

## 질문 1. (c) 공유 길 검사

### 1-0. 말 정리 (이 문서에서 쓰는 이름)

- **패킷** = `MiCreateSection` 이 스택에 만드는 `0xc8` 바이트 작업 구조 (`MiCreateSection +0x52 MOV R8D,0xc8` / `+0x58 memset`, PDB 에 이름 없음). 호출마다 `MiInitializeCreateSectionPacket` 이 채운다. 아래 "패킷 +0xNN" 은 그 칸.
- **CA** = 컨트롤 영역 (`_CONTROL_AREA`, PDB 칸 표 `pdb_struct.py ntoskrnl.exe _CONTROL_AREA`). `CA +0x38` = `u` (`_MMSECTION_FLAGS`: 비트 1 `BeingCreated`, 비트 5 `Image`, 비트 9 `PrefetchCreated`, 비트 11 `ImageControlAreaOnRemovableMedia`, 비트 29 `SystemVaAllocated`), `CA +0x5c` = `u2.e2` 의 둘째 dword (비트 18~19 `StrongCode`, 비트 20~21 `BitMap`, 비트 22 `ImageActive`, 비트 23 `ImageBaseOkToReuse`), `CA +0x60` = `ImageInfoRef` (하위 3비트는 표시, `& -8` 이 `_MI_IMAGE_ADDITIONAL_INFO*`; 그 `+0x20` 칸 = **재배치 기록 포인터** — PDB 이름 `DynamicRelocations`, sast/98 에서 "기록" 이라 부른 것).
- **이유 전역** = `nt+0xe2d838` (PDB 이름 없음). 이미지 만들기 중 어디서 실패했는지 번호를 적는 칸. 마지막으로 쓴 줄이 이긴다.

### 1-1. 공유 길 줄 순서

```
MiCreateImageOrDataSection +0x19e/+0x1a3 MiReferenceControlArea   (ImageSectionObject 에서 CA 찾기 — run31)
  └ CA +0x38 비트 1 (BeingCreated) 꺼짐 → +0x1d0/+0x1d5 MiShareExistingControlArea
       ├ +0x28/+0x2d  MiValidateControlAreaPartition
       ├ +0x157/+0x15c MmChangeSectionBackingFile   (프리페처가 만든 CA 일 때만)
       ├ +0xd8~+0xe5  IoSetTopLevelIrp·FsRtlReleaseFile (파일 잠금 풀기, 잡았을 때만)
       ├ +0xf4/+0xf9  MiValidateExistingImage         (패킷 +0x10 & 0x1000000 = SEC_IMAGE 일 때)
       │    ├ +0x51/+0x56  MiRelocateImageAgain → (+0x17e) MiSwitchBaseAddress
       │    ├ +0xd6/+0xdb  SeGetImageRequiredSigningLevel
       │    ├ +0x16a/+0x16f Feature_TrustedLaunchHosts…
       │    ├ +0x1e7/+0x1ec, +0x209/+0x20e SeCompareSigningLevels   (또는 +0x2a6 CI 콜백)
       │    └ +0x258/+0x25d MiValidateSectionSigningPolicy
       └ +0xfd~+0x10a, +0x9d~+0xb3  섹션 크기 읽어 패킷 +0x80 에 적기
```
- 근거: `ms/disassemble/ntoskrnl/MiShareExistingControlArea.asm`, `MiValidateExistingImage.asm`, `_calls.tsv`. `MiShareExistingControlArea` 의 callee 에 `MiCreateNewSection` 은 없다 (`_calls.tsv` 에서 Share 의 호출 11줄 전부 확인).

### 1-2. 검사 표 — `MiShareExistingControlArea`

| # | 줄 (`.asm`) | 칸 | 조건 | 실패하면 (status / 이유 전역) |
|---|---|---|---|---|
| S1 | `+0x28/+0x2d` → `MiValidateControlAreaPartition` | 패킷 `+0xb0` (파티션 객체), CA `+0x3c & 0x3ff` (파티션 번호 → 표 `[nt+0xe300c8 + 번호×8]`), CA `+0x3e` 비트 0 (`NoCrossPartitionAccess`), 패킷 `+0x0` 비트 22 · 23 · 24, 전역 `MiSystemPartition` (`nt+0xe38d40`) | 패킷에 파티션 있음: CA 파티션과 같고 `NoCross` 비트 = 패킷 비트 22 → 통과 (`MiValidateControlAreaPartition +0x48 ~ +0x5b`). 패킷에 파티션 없음: CA 가 `NoCross` 아니고 시스템 파티션이면 통과 (`+0x26 ~ +0x44`). 그 밖: 패킷 비트 24 (`0x1000000`) 가 꺼져 있으면 켜고 실패, 켜져 있으면 통과 (`+0x38 ~ +0x44`, `+0x9c`) | `0xC0000476`; 패킷 비트 23 (`0x800000`, `+0x5d` · `+0x8e`) 또는 24 (`+0x9c`) 켬. `MiShareExistingControlArea +0x31 JS → +0x161` 이 파일 잠금 풀고 (`+0x180 ~ +0x197`) `+0x168` 로 돌려줌 |
| S2 | `+0x37/+0x3f` → `+0x132 ~ +0x15c` | CA `+0x38` 비트 9 (`PrefetchCreated`, 0x200) | 켜져 있고 지금 스레드가 프리페처가 아니면 `MmChangeSectionBackingFile(0, 파일, 이미지 2 / 아니면 1)` 로 CA 의 파일 객체를 이번 것으로 바꿈. (비트 9 는 만든 스레드가 프리페처일 때 `MiSectionCreated +0x42/+0x4b` 가 켬: `TEST byte [KTHREAD+0x5a8],0x40`) | 반환값 안 봄 (`+0x15c JMP +0x45`) |
| S3 | `+0x45 ~ +0x4d` | 패킷 `+0x0` 비트 2 | 늘 켬 | 없음. 뒤에서 `MiDereferenceFailedControlArea +0xd TEST DL,4` 가 이 비트로 되돌리는 방법을 고름 |
| S4 | `+0xcc ~ +0xe5` | 패킷 `+0x0` 비트 1 (`MiCreateImageOrDataSection +0x18b/+0x190 OR [RDI],2` 가 켠 "파일 잠금 잡음" 표시) | 켜져 있으면 `IoSetTopLevelIrp([패킷+0xb8])`, `FsRtlReleaseFile`, 비트 끔 | 없음 |
| S5 | `+0xe8 ~ +0xfb` | 패킷 `+0x10` (할당 속성) & `0x1000000` | 켜져 있으면 `MiValidateExistingImage` (1-3) | 그 status 를 그대로 돌려줌 (`+0xfb JS → +0xb5`) |
| S6 | `+0xfd ~ +0x10a` → `+0x9d ~ +0xb3`, `+0x10c ~ +0x17b` | 세그먼트 `+0x18` (`SizeOfSegment`, `CMPXCHG.LOCK` 로 읽기만), 패킷 `+0x98` (`MaximumSize`), 패킷 `+0x1c` (페이지 보호) | `MaximumSize` 가 0 이면 패킷 `+0x80` ← `SizeOfSegment`, 패킷 비트 3 켬, 0 반환. 있으면 `SizeOfSegment < MaximumSize` 일 때 보호에 `0x44` (쓰기 가능) 가 있어야 통과 | `0xC0000040` (`+0x176`). test.exe: `MmCreateSpecialImageSection +0xa1 XOR R8D,R8D` 로 `MaximumSize` 0 → 안 탐 |

### 1-3. 검사 표 — `MiValidateExistingImage` (이 길의 본 검사)

> 이 함수가 이유 전역 `nt+0xe2d838` 에 쓰는 번호: `0xc` · `0xd` · `0xe` · `0xf`. 새 길 `MiCreateNewSection` 의 같은 검사와 짝: 아래 1-6.

| # | 줄 (`.asm`) | 칸 | 조건 | 실패하면 |
|---|---|---|---|---|
| V1 | `+0x31/+0x35` → `+0x2bb ~ +0x2d6` | CA `+0x38` 비트 11 (`ImageControlAreaOnRemovableMedia`, 0x800), 패킷 `+0x0` 비트 18 (0x40000) | 둘 다 켜짐 | 이유 `0xc`, **`0xC0000433`** (`+0x2c7 ~ +0x2d1`). (비트 18 이 켜지는 곳: `MiInitializeCreateSectionPacket +0x35f BTS [RBX],0x12`, 만들기 플래그 `& 8` 일 때. 같은 검사가 새 길에도 있음: `MiCreateNewSection +0x55e ~ +0x574`, 이유 `5`) |
| V2 | `+0x3b ~ +0x56` → `MiRelocateImageAgain(CA, 패킷비트5, 패킷비트28)` | (1-4 에서 따로) | — | 음수면 이유 `0xd` (`+0x58 JS → +0x2db`), status 는 그 함수가 준 값 그대로 |
| V3 | `+0x62 ~ +0x8b`, `+0x91 ~ +0xdb` → `SeGetImageRequiredSigningLevel(파일, 종류 EBP, 패킷+0x18, 세그먼트+0xf & 0xf, &결과)` | 패킷 `+0x0` 비트 9 (속성이 `SEC_IMAGE` 이고 `SEC_IMAGE_NO_EXECUTE` 가 아님 — `MiInitializeCreateSectionPacket +0x1c0 JNZ → +0x212 BTS EAX,9`) 켜짐 · 비트 4 꺼짐일 때만. 현재 프로세스 `MitigationFlags` (`EPROCESS +0x750`) 를 `R15D` 에 읽어 둠 (`+0xb4`) | 부름 | 음수면 이유 `0xe` (`+0xdf`), status 그대로. 성공하면 요구 수준을 패킷 `+0x18` 에 적음 (`+0x151 MOV [RDI],AL`) |
| V4 | `+0x11b ~ +0x142` | `MitigationFlags` 비트 23 `SignatureMitigationOptIn` · 24 `AuditBlockNonMicrosoftBinaries` · 25 `AuditBlockNonMicrosoftBinariesAllowStore` (PDB `_EPROCESS` 의 `MitigationFlagsValues` 칸 순서) | 비트 23 켜짐 + 패킷 `+0x18` 이 0 아님 → `ESI = 1` (`+0x307 ~ +0x312`). 비트 24 → `R12B = 8` (`+0x320`). 비트 25 → `R12B = 6` (`+0x328`) | 실패 없음. 정책 검사로 넘길 인자 모으기 |
| V5 | `+0x153 ~ +0x167`, `+0x330 ~ +0x342` | 패킷 비트 18 · 21 | 비트 18: 플래그 비트 30 켬, 요구 수준이 0 이면 4 로 (`+0x33e MOV [RBX+0x18],4`). 비트 21: 플래그 비트 29 켬 | 실패 없음 |
| V6 | `+0x16a/+0x16f` → `+0x173/+0x179` → `+0x347` | `Feature_TrustedLaunchHosts` 결과 · 패킷 비트 27 (0x8000000) | 둘 다 켜짐 → 플래그 비트 21 켬 (`BTS EDI,0x15`) | 실패 없음 (1-9 에서 자세히) |
| V7 | `+0x17f ~ +0x220` (패킷 비트 9 일 때만) | 올라온 이미지의 서명 수준 = 세그먼트 `+0xf & 0xf`, 서명 종류 = `(세그먼트 +0xf >> 4) & 7`, 이미지 정보 `+0x2e` (DllCharacteristics), CA `+0x5c` 비트 18~19 | ① `SeCiCallbacks+0x98` 가 0 아니면 그 콜백을 `+0x2a6` 에서 (CFG 거쳐) 부름 — 인자: 플래그 · 올라온 수준 · 종류 · 보안 문맥 · 파일. 0 이 아닌 값이 돌아오면 `ESI \|= 2`. 콜백 없으면 `SeCompareSigningLevels(올라온 수준, 요구 수준)` (`+0x1e7`) 이 0 이면 `ESI \|= 2`. ② 플래그 비트 30 이면 `StrongCode` 가 2 가 아니면 `ESI \|= 2` (`+0x350 ~ +0x364`). ③ 올라온 수준이 0 인데 `DllCharacteristics` 비트 7 (`FORCE_INTEGRITY`, `+0x274 ~ +0x27d`) 이 켜져 있으면 `ESI \|= 2`. ④ 올라온 수준이 감사 수준 `R12B` 보다 낮으면 `ESI \|= 4` (`+0x209/+0x20e`). ⑤ 패킷 비트 26 (0x4000000) 이면 `ESI \|= 8` (`+0x218`) | **여기서는 실패 status 를 안 만든다.** 비트만 모음 |
| V8 | `+0x226 ~ +0x25d` → `MiValidateSectionSigningPolicy(1, 파일, CA, 패킷+0xa8, 패킷+0xa0, 플래그, ESI, 수준, R12B)` | `ESI` 비트 1 · 2 (`0x2`, `0x4`) | 둘 다 꺼짐이면 정책 함수가 **바로 0** (`MiValidateSectionSigningPolicy +0x37 TEST SIL,6` / `+0x3b JNZ → +0x5e` / `+0x3d XOR EAX,EAX`). 하나라도 켜짐이면 `MiValidateSectionCreate` 를 부름 (본문 이름만) | 음수면 이유 `0xf` (`+0x265`), status 그대로. (정책 함수 안에 `0xC0000428` 을 따로 보는 줄이 있음 `+0xf3`.) 정책 함수가 `+0x11a ~ +0x124` 에 쓰는 이유 번호 (기존 1 / 새 것 2) 는 호출한 쪽이 `0xf` 로 덮어씀 |
| V9 | 위 어디에도 없음 | 파일 수정 시각 · 크기 · 내용 비교 | **이 두 함수 안에 그런 줄이 없다** `[정적]` | 컨트롤 영역이 "만드는 중/지우는 중" 인 경우는 이미 `MiReferenceControlArea` 가 걸러 (기다렸다 처음부터 — run31, 범위 밖) |

- **CFG**: `MiRelocateImageAgain +0xfc/+0x101 MiUpdateImageSystemWideBitmaps` 가 CA `+0x58 ≠ −1` 이고 CA `+0x38` 비트 29 꺼짐일 때 `MiUpdateCfgSystemWideBitmap` 을 부른다 (`MiUpdateImageSystemWideBitmaps +0x4 ~ +0x1b`). 새 기준 주소를 골랐을 때만 간다. CFG 본문은 이름만.
- **기준 주소 충돌**: 이 길에는 "이 프로세스 주소 공간에 이미 뭔가 있나" 검사가 없다. 있는 건 (a) 컨트롤 영역이 쥔 64KB 칸 (`ImageActive`) 을 다시 고르는 것 (1-4), (b) 매핑 때 뷰가 `BasedAddress` 에 안 붙으면 `0x40000003` (sast/98 `MiMapViewOfImageSection +0x42a`).

### 1-4. `MiRelocateImageAgain` 은 언제 기준 주소를 다시 고르나

줄 순서 (`MiRelocateImageAgain.asm`):

| 줄 | 하는 일 | 갈림 |
|---|---|---|
| `+0x16 ~ +0x35` | `RBP = [CA+0x60] & -8`, `R15 = [RBP+0x20]` (재배치 기록) | **0 이면 아무것도 안 하고 0 반환** (`+0x30 TEST R15,R15` / `+0x33 JNZ` / `+0x35 XOR EAX,EAX`). 기록이 안 생기는 이미지 = sast/98 1-1 ① "일찍 끝": x86/x64 + `RELOCS_STRIPPED`, `DYNAMICBASE` 없음 등 |
| `+0x7b/+0x80` | 재배치 락 독점 (`MI_LOCK_RELOCATIONS_EXCLUSIVE`) | |
| `+0x80/+0x87` | CA `+0x5c` 비트 22 (`ImageActive`) 확인 | **켜져 있으면 `+0x117` 로 가서 락 풀고 0 반환 = 먼저 고른 기준 주소 그대로.** 꺼져 있을 때만 아래로 |
| `+0xa3/+0xa8` | `MiReservePtes(…, 1)` 시스템 PTE 하나 (임시 자리) | 실패 (`+0xae JZ → +0x1a1`) → `ImageActive` 만 켜고 (`+0x1a1 BTS [RBX+0x5c],0x16`) 0 반환 (기준 안 바꿈) |
| `+0xd3/+0xd8` | `MiSelectImageBase(세그먼트, 기록, 패킷비트5, (패킷비트28 ? 2 : 0), &새기준)` | 음수면 `+0x117` 로 가 PTE 돌려주고 (`+0x19a MiReleasePtes`, `+0x12e MiReturnImageBase`) 그 status 반환 → V2 이유 `0xd` |
| `+0xde` | `ImageActive` 켬 | |
| `+0xe6/+0xeb`, `+0xfc/+0x101` | `MiGetControlAreaLoadConfig` · `MiUpdateImageSystemWideBitmaps` (CFG 비트맵) | 음수면 `+0x1ab` → `+0x1b5 MiImageUnused` 로 `ImageActive` 도로 끄고 status 반환 |
| `+0x10b/+0x113` | 새 기준 `RSI` 와 옛 `BasedAddress` (`+0x8d MOV RAX,[R13+0x20]`) 비교 | **같으면 0 반환, `MiSwitchBaseAddress` 안 탐** (`+0x115 XOR EDI,EDI`) |
| `+0x13a ~ +0x14e`, `+0x1bf ~ +0x1d7` | `MiFlags & 0x2000` 이고 CA `+0x5c & 0xc0000` (`StrongCode`) ≠ 0 이면 `SeSetImageBaseAddress([기록+0x28]…, 새기준)` (CI 에 새 기준 알림) | 음수면 `+0x1ab` 로 되돌리고 status 반환 |
| `+0x150 ~ +0x16a` | 패킷 비트 5 가 켜져 있으면 세션 `0xFFFFFFFF`, 아니면 `PsGetProcessSessionIdEx(지금 프로세스)` | |
| **`+0x17e/+0x183`** | **`MiSwitchBaseAddress(CA, 새기준, 임시 PTE, 세션)`** | |

- 그래서 `MiSwitchBaseAddress` 가 도는 조건 `[정적]`: ① 재배치 기록 있음, ② `ImageActive` 꺼짐, ③ 임시 PTE 예약 성공, ④ `MiSelectImageBase` 성공, ⑤ 비트맵·CI 갱신 성공, ⑥ 새 기준 ≠ 옛 `BasedAddress`.
- `ImageActive` 가 꺼지는 곳 (`MiImageUnused +0x83 BTR [RBX+0x5c],0x16`, 같은 곳에서 `CA+0x58 = −1` 로 64KB 칸 반납): `MiCheckControlArea +0xc9` (CA `+0x28` 매핑된 뷰 = 0 `+0x58` · `+0x18` 섹션 참조 = 0 `+0x97` · `+0x40` FilePointer 포인터 ≠ 0 `+0xa0/+0xa7` · `+0x20` PFN 참조 ≠ 0 `+0xad/+0xb1` · CA `+0x38` 비트 5 `Image` `+0xb7/+0xb9`. PFN 참조가 0 인 쪽 `+0xb1 JZ → +0x29c` 는 다른 길 — 안 따라감 `[공백]`), `MiCheckControlArea +0x3be`, `MiRelocateImageAgain +0x1b5`.
  → 뷰도 섹션 참조도 없어졌는데 페이지가 대기 목록에 남아 CA 가 살아 있으면 **기준 주소 칸만 반납**. 그 CA 를 다시 쓰는 다음 `SEC_IMAGE` 가 공유 길로 와서 **새로 고른다.** (프로세스가 끝나는 타이밍은 `[추측]`, 동적으로 봄.)
- DLL 이 다른 프로세스에 계속 매핑돼 있으면 `ImageActive` 가 켜져 있어 같은 기준 주소가 그대로 쓰인다. 그래서 `kernelbase` 류가 프로세스마다 같은 주소다 (KnownDll 은 섹션이 이미 있어 이 길 자체를 안 탐).

### 1-5. `MiSwitchBaseAddress` — `BasedAddress` · `TransferAddress` 가 바뀌는 방식 (sast/98 `+0x4e` 와 이어서)

인자: `RCX` = CA, `RDX` = 새 기준, `R8` = 임시 PTE, `R9D` = 세션 (그대로 `MiWalkEntireImage` 넷째 인자로).

| 줄 | 일 | 칸 | 전 → 후 |
|---|---|---|---|
| `+0x18 ~ +0x33` | `RAX = [CA+0x60] & -8`, 기록 `RBP = [RAX+0x20]`, 세그먼트 `RDI = [CA]`, 옛 기준 `R10 = [RDI+0x20]` | | |
| `+0x37` | 이번 델타 `R14 = 새 − 옛` | | |
| `+0x3a ~ +0x3e` | 옛 누적 델타 `RBX = [기록+0x28]` 저장, **`[기록+0x28] = R14`** (걷는 동안은 이번 한 번의 델타) | 기록 `+0x28` | 누적 → 이번 델타 |
| **`+0x46`** | **`[세그먼트+0x20] = 새 기준`** | `_SEGMENT.BasedAddress` | 옛 → 새 |
| **`+0x4a ~ +0x4e`** | **`ADD [이미지정보+0x0], 이번 델타`** (`RCX = [세그먼트+0x38]`) | `TransferAddress` | `+= 새 − 옛` |
| `+0x51 ~ +0x58`, `+0xeb ~ +0xfa` (호출 `+0xf2`/ret `+0xf7`) | 기록 `+0x10` 목록을 돌며 `MiApplyBytestreamFixup(…, 노드+0x18, R8 = 옛 누적 델타)` (걷기 **앞**) | 기록 `+0x10` 목록 | 뜻은 안 따라감 `[공백]` |
| `+0x5e ~ +0x8a` | 적재 구성 (`MiGetControlAreaLoadConfig` = 기록 `+0x48`) 의 `+0x18` 이 가리키는 칸 `+0x8`·`+0x10` 을 옛↔새 만큼 옮김 | | 칸 이름 안 따라감 `[공백]` |
| `+0x9a/+0x9f` | **`MiWalkEntireImage(CA, 임시 PTE, 2, 세션)`** | 올라와 있던 페이지 | 이번 델타로 즉시 고침 (sast/98 모드 2) |
| `+0x9f ~ +0xa3` | **`[기록+0x28] = 이번 델타 + 옛 누적 델타`** | 기록 `+0x28` | 이번 → 누적 (헤더 `ImageBase` 대비). 아직 안 올라온 페이지는 나중 page-in 때 이 누적 값으로 (`MiRelocateImagePfn`, sast/98) |
| `+0xa7 ~ +0xae`, `+0xd2 ~ +0xe9` (호출 `+0xdc`/ret `+0xe1`) | 같은 목록을 한 번 더 돎: `R8 = −옛 누적 델타` (걷기 **뒤**) | 기록 `+0x10` 목록 | 뜻은 안 따라감 `[공백]` |
| `+0xb0 ~ +0xb6`, `+0xff ~ +0x129` | 세그먼트 플래그 비트 11 (`DebugSymbolsLoaded`) 켜져 있으면 `DbgUnLoadImageSymbolsUnicode(파일이름, 옛 기준…)` 후 비트 끔 (`AND word [RDI+0xc],0xf7ff`) | `SegmentFlags` | 기호 올림 표시 끔 |

- 프로세스 사본 `TransferAddress` 는 이 길이 아니라 나중 `PspAllocateProcess +0x20b3 ~ +0x20ce` (sast/98) 가 (실제 `SectionBaseAddress` − `BasedAddress`) 를 더한다. 섹션 사본이 바뀌는 건 `MiRelocateImage +0x1b1279` (처음) 와 여기 `+0x4e` (다시 고를 때) 둘뿐.
- 한계: 같은 CA 로 `MiSwitchBaseAddress` 가 돌면 `PspAllocateProcess +0x20bb` 가 `BasedAddress` 를 **다시 읽는** 갈래와 어긋날 수 있다는 sast/98 `[추측]` 은 그대로 (동적).

### 1-6. 새 길과 짝 (이유 번호)

| 검사 | 공유 길 `MiValidateExistingImage` | 새 길 `MiCreateNewSection` |
|---|---|---|
| 이동식 미디어 + 패킷 비트 18 | `0xc` / `0xC0000433` | `5` / `0xC0000433` (`+0x55e ~ +0x574`) |
| 기준 주소 | `0xd` (`MiRelocateImageAgain`) | `0xb` (첫 `MiRelocateImage +0x3d5`), `9` (둘째 `+0x695`) |
| 요구 서명 수준 | `0xe` | `7` (`+0x2ee/+0x2f3` 뒤 `+0x2f7 JS`) |
| 서명 정책 | `0xf` | `0xa` (`MiValidateSectionSigningPolicy` 호출, 조각 `+0x1b4a9f`) |
| 그 밖 (새 길만) | — | `4` (`MiParseComAndCetHeaders`), `6` (`MiSetPagesModified`), `8` (`FsRtlGetDirectImageOriginalBase`) |

이유 전역 `nt+0xe2d838` 대역 (읽은 줄 기준): `1·2` 정책 함수 안, `4 ~ 0xf` 위 표, `0x10 ~ 0x19` `MiParseImageSectionHeaders`, `0x1a ~ 0x1f` `MiBuildImageControlArea`, `0x3b ~ 0x59` `MiVerifyImageHeader`.

### 1-7. 검사 실패하면 어디로 가나

- `MiCreateImageOrDataSection +0x1d5` 뒤 `+0x1da JS → +0x51e`: `+0x521/+0x526 MiDereferenceFailedControlArea` (CA 는 지우지 않고 참조만 되돌림. 패킷 비트 2 가 켜져 있어 `MiDereferenceControlAreaBySection` 쪽, `MiDereferenceFailedControlArea +0xd/+0x21/+0x26`) → `+0x526 JMP → +0x26f` → `+0x26f CMP R14D,0xc0000476` 아니면 `+0x27c ~ +0x290` 에서 status 반환 → `MiCreateSection +0x110/+0x112 JS → +0x1e9` 정리. **새 컨트롤 영역을 만들러 가는 줄 없음 → status 로 끝.**
- 예외 (`0xC0000476` 만): `+0x26f JZ → +0x43e` (`CcWaitForUninitializeCacheMap` `+0x447`, `MiForceSectionClosed` `+0x456`) → `+0x45b CMP EAX,1 / +0x45e JBE → +0x27c` → 상태 그대로 올라가 → `MiCreateSection +0x105/+0x10a JZ → +0x21a` 가 **패킷부터 다시 만든다** (아래 3-4). 그 둘째 바퀴의 `MiReferenceControlArea` 가 CA 를 다시 찾으니, 그 사이 CA 가 없어졌으면 새로 만들기로 갈 수도 있다 (갈림은 매번 새로 함). `MiForceSectionClosed` 가 2 이상을 주고 패킷 비트 23 이 켜져 있으면 `+0x470 ~ +0x476` 이 `0xC000060B` 로 끝 (재시도 아님).
- test.exe (파티션 하나): `S1` 이 `+0x44` 로 통과 → 이 예외 길은 안 탈 것 `[정적 + 가정]`.

### 1-8. 우리 (PE_emulator) 맞나

- **틀림 (빠져 있음, 영향 작음).** `Src/nt/Section.cpp` `MiCreateImageOrDataSection` 은 늘 새 섹션 (주석에 "안 함: `MiShareExistingControlArea`"). 한 실행에 같은 파일을 `NtCreateSection(SEC_IMAGE)` 하는 길이 test.exe 에 없으면 결과 같음.
- 그 주석 (Section.cpp:418 "…`MiRelocateImageAgain` 으로 먼저 고른 주소를 씀") 은 **반만 맞다**: 먼저 고른 주소를 쓰는 건 `ImageActive` 가 켜져 있을 때뿐, 꺼져 있으면 `MiSelectImageBase` 로 다시 고른다 (`+0x80/+0x87`). 정정 후보.
- 파티션·`0xC0000476` · `MiCreateNewSection` 으로의 되돌아감은 우리에 없고 필요 없음 (단일 파티션).

---

## 질문 2. (d) 헤더 페이지 → 프로토 PTE

### 2-0. 전제 정정

요청서 문장 "`MiInitializeNewImageSectionProtos` 가 프로토 PTE 를 섹션마다 어떤 보호값으로 채우나" 의 답은 **그 함수가 보호값을 안 채운다**이다. 보호값은 더 앞의 `MiBuildImageControlArea +0x362/+0x367 MiParseImageSectionHeaders` (`MiCreateImageFileMap +0x2e1/+0x2e6` 안) 가 서브섹션 표와 프로토 PTE 배열에 채운다.

### 2-1. 섹션 특성 → 보호 표 (`MiParseImageSectionHeaders +0x176 ~ +0x1e5`)

인덱스 계산 (`+0x17d ~ +0x1a5`): `idx = (Characteristics>>29 &1) \| (>>30 &1)<<1 \| (>>31 &1)<<2 \| (>>28 &1)<<3`, 곧 `EXECUTE(0x20000000)` · `READ(0x40000000)` · `WRITE(0x80000000)` · `SHARED(0x10000000)` 네 비트. 값은 표 `MiImageProtectionArray` (`+0x1a8 LEA RCX,[0x14001a6b0]`, `+0x1af MOVSX R15D,[RAX+RCX]`) 에서 읽는다. 이름은 `MiMakeProtectionMask` 가 쓰는 표 (`MmUserProtectionToMask1/2`) 를 거꾸로 맞춘 것.

| 특성 (EXEC · READ · WRITE) | 공유 안 함 (`SHARED` 꺼짐) | 값 | `PAGE_*` | 공유 (`SHARED` 켜짐) | 값 | `PAGE_*` |
|---|---|---|---|---|---|---|
| — — — | idx 0 | `0x18` | `PAGE_NOACCESS` | idx 8 | `0x18` | `PAGE_NOACCESS` |
| X — — | 1 | 2 | `PAGE_EXECUTE` | 9 | 2 | `PAGE_EXECUTE` |
| — R — | 2 | 1 | `PAGE_READONLY` | 10 | 1 | `PAGE_READONLY` |
| X R — | 3 | 3 | `PAGE_EXECUTE_READ` | 11 | 3 | `PAGE_EXECUTE_READ` |
| — — W | 4 | 5 | `PAGE_WRITECOPY` | 12 | 4 | `PAGE_READWRITE` |
| X — W | 5 | 7 | `PAGE_EXECUTE_WRITECOPY` | 13 | 6 | `PAGE_EXECUTE_READWRITE` |
| — R W | 6 | 5 | `PAGE_WRITECOPY` | 14 | 4 | `PAGE_READWRITE` |
| X R W | 7 | 7 | `PAGE_EXECUTE_WRITECOPY` | 15 | 6 | `PAGE_EXECUTE_READWRITE` |

- 배열 원본 바이트: `18 02 01 03 05 07 05 07 18 02 01 03 04 06 04 06` (`.rdata` rva `0x1a6b0`). `PAGE_*` 짝은 `MmUserProtectionToMask1` (rva `0x3b020`: `NOACCESS→0x18`, `READONLY→1`, `READWRITE→4`, `WRITECOPY→5`) 와 `Mask2` (rva `0x3b030`: `EXECUTE→2`, `EXECUTE_READ→3`, `EXECUTE_READWRITE→6`, `EXECUTE_WRITECOPY→7`) 원본 바이트로 확인 `[정적]`.
- **요점**: `WRITE` 가 있고 `SHARED` 가 없으면 **쓰기 시 복사** (5·7), `SHARED` 까지 있으면 **진짜 읽기쓰기 공유** (4·6). 다른 특성 비트 (`DISCARDABLE` 등) 는 안 봄.

섹션을 서브섹션 칸에 쓰는 줄:
- `+0x1c9 ~ +0x1d1`: `EDX = ((값 & 0x1f) << 1) \| (섹터끝오프셋 << 0x14)` → `[서브섹션+0x20]` (`_MMSUBSECTION_FLAGS`: `Protection` 비트 1~5).
- `+0x1b4 ~ +0x1bf`: 값 `& 2` (실행 있음) 이면 `[이미지정보+0x32] = 1` (`_SECTION_IMAGE_INFORMATION.ImageContainsCode`).
- `+0x206 ~ +0x20a`, `+0x34b ~ +0x355`, `+0x381 ~ +0x399`: 값 `& 4` (쓰기) 가 있으면: `(값 & 5) == 5` (쓰기 시 복사) → 쪽마다 세그먼트 `+0x30` (`ImageCommitment`) 올림 (`+0x249`). 아니면 (공유 쓰기 4·6) → 서브섹션 플래그 비트 17 (`GlobalMemory`) · CA `+0x38` 비트 17 (`GlobalMemory`) 켬, 쪽마다 세그먼트 `+0x10` (`NumberOfCommittedPages`) 올림 (`+0x240`).
- 이미지가 "통째 한 덩어리" (`MiBuildImageControlArea` 인자 `& 0x100000`) 인 갈래: 서브섹션 하나, 보호 7 (`+0x3fa OR EDX,0xe`), `ImageFlags` 비트 3 (`ImageMappedFlat`) · `ImageContainsCode` 켬 (`+0x3ed ~ +0x3f2`). test.exe 는 해당 없음 (`SectionAlignment 0x1000`).
- 헤더 서브섹션 (첫째): 보호 1 (`+0x2e6 ~ +0x2f3 … OR EAX,2` → `Protection` = 1) = `PAGE_READONLY`.
- .NET `IL-only` 이미지만: `MiCreateNewSection +0x3e4/+0x3e9 → +0x6b3 MiMakeImageReadOnly` 가 모든 서브섹션과 안 올라온 프로토 PTE 의 보호를 1 로 덮어씀 (`MiMakeImageReadOnly +0x47 ~ +0x4d`, `+0x92/+0x95`). test.exe 는 CLR 디렉터리 (14번) 가 비어 있어 안 탐.

### 2-2. 프로토 PTE 모양 (보호값이 들어가는 칸)

| 종류 | 만드는 줄 | 모양 | 어디에 |
|---|---|---|---|
| 파일에서 읽을 쪽 | `MiMakeSubsectionPte` (`+0x4 ~ +0x19`): `((서브섹션주소 << 12) \| (서브섹션플래그 & 0x3e) \| 0x40) << 4` 뒤 `MiSwizzleInvalidPte` | `_MMPTE_SUBSECTION`: 보호 = 비트 5~9, **`Prototype` = 비트 10**, 서브섹션 주소 = 비트 16~ (PDB 칸 표) | 섹션 파일 내용이 있는 쪽 (`MiParseImageSectionHeaders +0x25c MOV [R12],R8`, 쪽마다 `+0x258 INC [R14+0x78]`) |
| 0 으로 채울 쪽 | `MiMakeDemandZeroPte` (`+0x0 ~ +0x1a`): `(값 & 0x1f) << 5` (전역 `nt+0xe2dcc0` 가 있으면 섞음) | 보호 = 비트 5~9, 비트 10 = 0 | `VirtualSize > SizeOfRawData` 구간 (`+0x2d8 ~ +0x2db MOV [R12],R9`), `PointerToRawData == 0` 이면 섹션 전부 (`+0x1e5 ~ +0x1f0 CMOVZ R8,RAX`) |
| 이미지 밖 | 전역 `ZeroPte` (`+0x226` 읽음, `+0x2b2 MOV [R12],RAX` 씀) | 0 | `VirtualSize` 넘는 쪽, 그 서브섹션 `UnusedPtes` (`+0x34`) 올림 (`+0x2b6 ~ +0x2ce`) |

### 2-3. `MiInitializeNewImageSectionProtos` 가 하는 일 + 헤더 페이지 차례

**누가·언제 부르나**: `MiSectionCreated +0xfe/+0x103`. 이미지 (CA `+0x38` 비트 5) 일 때만 (`MiSectionCreated +0x57 TEST AL,0x20`, `+0xcb TEST EBP,EBP`), CA 를 `ImageSectionObject` 에 꽂고 (`+0x124 MOV [RSI+0x10],RBX`) `BeingCreated` 를 끈 (`+0xa6 AND [RBX+0x38],~2`) **뒤**. 둘째 인자는 `MiCreateNewSection +0x155 MOV R9,[RBP−0x80]` 의 MDL 사슬.

**헤더 페이지는 어디서 오나**: `MiCreateImageFileMap +0xd6/+0xdb MiCreateMdl(…, 0x1000, 1)` 이 `MiGetPageForHeader` (`MiCreateMdl +0xa2/+0xa7`) 로 **물리 페이지 하나**를 받아 MDL 에 넣는다 (플래그 `0x4002`, `MiCreateMdl +0xb2 ~ +0xb7`). `+0xe7 AND [RAX+0x10],R12` 로 MDL `+0x10` (원래 `Process` 칸) 에 "첫 프로토 인덱스 0" 을 적어 재활용. 그 위에 파일 첫 4KB 를 읽고 (`+0x15c MiCopyHeaderIfResident` / `+0x501 MiReadImageHeaders`) `MiVerifyImageHeader` (`+0x1f1/+0x1f6`) → `MiBuildImageControlArea` (`+0x2e1/+0x2e6`, 위 2-1) → `MiInitializeImageHeaderPage (+0x3f6/+0x3fb)` (헤더 페이지 꼬리 `SizeOfHeaders` 부터 0 채움, `MiInitializeImageHeaderPage +0x12 ~ +0x47`).

**함수가 하는 일 (줄 순서, `MiInitializeNewImageSectionProtos.asm`)**:
1. `+0x0 ~ +0x3 TEST RDX,RDX / JZ → RET`: MDL 사슬이 없으면 아무것도 안 함.
2. MDL 마다 `+0x3d TEST byte [RBX+0xa],4` — `0x4` 가 켜진 MDL (풀 버퍼 MDL, `MiCreateMdl` R9=0 갈래) 은 건너뜀 (`+0x41 JNZ → +0x78`). 헤더 페이지 MDL 은 `0x4` 가 없어 처리.
3. `+0x43 ~ +0x4a MmUnmapLockedPages(MDL+0x18, MDL)` 시스템 주소 매핑 풀기. 쪽 수 `EBP = [MDL+0x28] >> 12`, 첫 프로토 칸 `R14 = [CA+0x88 (첫 서브섹션 SubsectionBase)] + [MDL+0x10]×8` (`+0x70`), 헤더 서브섹션 쪽 수 `ESI = [CA+0xac]` (`+0x28`).
4. 쪽마다 (`+0x9a ~ +0x13b`): PFN `[MDL+0x30+…]` → `MMPFN` (`× 0x30 − 0x220000000000`, `+0xa5 ~ +0xb7`).
   - 프로토 인덱스 ≥ 헤더 서브섹션 쪽 수 (`+0xba CMP ECX,ESI / +0xbc JNC → +0x17f`) 면: `MiReturnPfnReferenceCount` 로 참조 반납, 칸 `OR [R15],−1` (`+0x182 ~ +0x187`).
   - 아니면 프로토 풀 페이지를 잠그고 (`+0xe5 MiLockProtoPoolPageForce`) 그 프로토 PTE 를 `MiLockLeafPage(R14, 0)` (`+0xf5`):
     - 이미 다른 페이지가 있음 (`RAX ≠ 0`, `+0x100 JNZ → +0x16e`), 또는 PTE 비트 10 (`Prototype`) 이 꺼져 있음 (`+0x102 BT RCX,0xa / +0x107 JC` 안 감): **우리 페이지를 버림** — `MiReturnPfnReferenceCountAtDpc` (`+0x110`), 칸 `OR [R15],−1` (`+0x115`).
     - PTE 가 아직 `Prototype`(= `MiMakeSubsectionPte` 가 만든 "파일에서 읽을 쪽") 이고 비어 있음 (`+0x107 JC → +0x21e → +0x18d`): **PFN 잠금 비트 63 잡고** (`+0x18d BTS.LOCK [RDI+0x18],0x3f`, 못 잡으면 `+0x1d7` 에서 기다림) **`MiInitializeTransitionPfn(PFN, 프로토 PTE 주소, −1)`** (`+0x1a2/+0x1a7`) 로 이 물리 페이지를 프로토 PTE 의 transition 으로 꽂음, 잠금 풀고, 그 PFN 의 `OriginalPte` 비트 10 이 켜져 있으면 `MiReferenceControlAreaPfn(CA, 0, 1)` (CA `+0x20` `NumberOfPfnReferences` += 1, `+0x1b6 ~ +0x1cd`). **이 갈래는 `OR [R15],−1` 을 안 한다** (`+0x1be JNC → +0x119`, `+0x1d2 JMP → +0x119` 가 `+0x115` 를 건너뜀).
5. 끝나면 풀 페이지 잠금 풀기 (`+0x15f MiUnlockProtoPoolPage`).

**`MiFinalizeImageHeaderPage` 와의 차례** (`MiCreateNewSection.asm`):

| 순서 | 줄 | 하는 일 |
|---|---|---|
| 1 | `+0x210/+0x215 MiCreateImageFileMap` | 헤더 페이지 읽기·검사·**보호값 채우기** (위 2-1, 2-3) |
| 2 | `+0x162/+0x167 MiSectionCreated` → `+0x103 MiInitializeNewImageSectionProtos` | **헤더 페이지를 프로토에 꽂음** (transition). 꽂은 쪽은 MDL 칸에 PFN 이 남고, 버린 쪽은 `−1` |
| 3 | `+0x3d5/+0x3da MiRelocateImage` (또는 `+0x695`) | 기준 주소·재배치 기록 (sast/98) |
| 4 | `+0x3ef ~ +0x6c0` 루프: `0x4` 없는 MDL 의 칸 `[R15]` 가 `−1` 이 **아니면** (`+0x6c3 CMP RAX,−1 / +0x6c7 JZ` 건너뜀) **`+0x6e2/+0x6e7 MiFinalizeImageHeaderPage(MMPFN)`** | 2 번이 꽂은 페이지의 MDL 이 쥔 잠금·참조를 풂: `MiUseSlabAllocator` (`+0x7d`) 로 슬랩 자리 시도 (조각 `MiGetSlabPage`·`MiReplaceTransitionPage`), `+0x89 MiLockPageInline`, `+0x94 MiRemoveLockedPageChargeAndDecRef`, `+0xac AND.LOCK [RBX+0x18],0x7fff…` (PFN 잠금 풀기), `+0xcb MOV CR8,RSI` (IRQL 복구). 곧 페이지는 프로토가 소유한 transition(대기) 페이지가 됨 |
| 5 | `+0x41e ~ +0x460` | MDL 해제 (`0x4` 있으면 풀 버퍼 `ExFreePoolWithTag`, 아니면 `MmUnmapLockedPages` · `IoFreeMdl`) |
| 6 | `+0x468/+0x46d MiReleaseImageSection` | (sast/98) |

- 그래서 순서: **`MiInitializeNewImageSectionProtos` (꽂기) → `MiFinalizeImageHeaderPage` (내려놓기)**. 이전에 쓰던 `MiInitializeImageProtos` (`MiCreateImageFileMap +0x3dc/+0x3e1`) 는 `[RBP−0x40]` 에 둘째 MDL 이 있을 때만 (`+0x3d1 TEST RAX / +0x3d4 JZ`), 새 페이지에 파일 내용을 복사해 프로토에 꽂는 별개의 길 — 이번엔 안 읽음.
- test.exe: 헤더 `SizeOfHeaders 0x400` → 헤더 서브섹션 쪽 수 1, MDL 쪽 1 개 → **2 번이 쪽 하나를 꽂고 4 번이 그 하나를 내려놓는다고 읽힘** `[정적]` (프로토 PTE 가 비어 있고 `Prototype` 일 때. 실제 갈래는 동적).

### 2-4. test.exe 값 대입 (`sample/bin/test.exe` 섹션 표)

| 서브섹션 | 특성 | idx | 보호 값 | `PAGE_*` | 서브섹션 `Protection` 필드 |
|---|---|---|---|---|---|
| 헤더 | — | — | 1 | `PAGE_READONLY` | 1 |
| `.text` | `0x60000020` (X R) | 3 | 3 | `PAGE_EXECUTE_READ` | 3 |
| `.rdata` | `0x40000040` (R) | 2 | 1 | `PAGE_READONLY` | 1 |
| `.data` | `0xC0000040` (R W) | 6 | 5 | `PAGE_WRITECOPY` | 5 |
| `.pdata` | `0x40000040` (R) | 2 | 1 | `PAGE_READONLY` | 1 |
| `.fptable` | `0xC0000040` (R W) | 6 | 5 | `PAGE_WRITECOPY` | 5 |
| `.reloc` | `0x42000040` (R, `DISCARDABLE` 안 봄) | 2 | 1 | `PAGE_READONLY` | 1 |

- `ImageContainsCode` = 1 (`.text` 가 실행 가능). `.data` · `.fptable` = 쓰기 시 복사 → 쪽마다 세그먼트 `ImageCommitment` 올림 (`GlobalMemory` 안 켬).
- 보호 칸에 들어가는 모양: 서브섹션 `+0x20` 의 비트 1~5 = 위 값, 프로토 PTE 비트 5~9 = 같은 값.

### 2-5. 우리 (PE_emulator) 맞나

- **프로토 PTE · 헤더 페이지 · `MiSectionCreated` 일을 안 두는 건 맞다** (`Section.cpp` `MiSectionCreated` 가 성공만 돌려줌, `MiBuildImageControlArea` 가 `TransferAddress` 만 채움). 게스트에 보이는 건 보호뿐.
- **보호는 반만 맞다**: `Src/nt/AddressSpace.cpp::_GetSectionProtection` 은 `EXECUTE/READ/WRITE` 만 `UC_PROT_*` 로 옮긴다 → 효과는 같다. 그러나 (a) `PAGE_*` 로 되읽을 때 `.data` 는 진짜 `PAGE_WRITECOPY (0x08)` 인데 우리 `VirtualMemory.cpp::_FromUnicornProtection` 은 `PAGE_READWRITE` 로 돌려줌 (그 파일 주석에도 "쓰기 시 복사 구분 없음"), (b) `SHARED` 구분 없음, (c) 아무 특성도 없으면 진짜 `PAGE_NOACCESS` 인데 우리는 `UC_PROT_NONE` (같은 뜻). 게스트가 `VirtualQuery`/`NtQueryVirtualMemory` 로 이미지 쪽을 되읽는 길은 **확인 필요** (run44 관찰 `[관찰]` 이 VAD 보호 값이 `EXECUTE_WRITECOPY` 라는 건 있음).
- `ImageContainsCode` (`_SECTION_IMAGE_INFORMATION +0x32`) 를 쓰는 곳을 우리 코드에서 못 찾음 → **확인 필요**.

---

## 질문 3. (e) 작은 것

### 3-1. `MiVerifyImageHeader` 이유 `0x57` · `0x58` · `0x59`

이 줄들은 헤더의 **마지막** 검사다 (앞의 `0x3b ~ 0x56` 을 다 통과한 뒤, `+0x259 ~ +0x27e`).

| 이유 | 줄 | 조건 (모두 `[정적]`) | status |
|---|---|---|---|
| `0x57` | `+0x259 ~ +0x26e`, `+0x274 ~ +0x278`, `+0x54f ~ +0x566` | 머신이 x86 (`0x14c`) 또는 x64 (`0x8664`) (`MiLegacyImageArchitecture`: `+0x0 ~ +0x1e` 가 이 둘이면 1) **이고** `FileHeader.Characteristics` 비트 0 `RELOCS_STRIPPED` (`+0x259 MOVZX R8D,word [R10+0x16]` / `+0x263 AND R8W,R9W`, `R9W=1`) 켜짐 **이고** `DllCharacteristics` 비트 12 (`0x1000` `APPCONTAINER`, 헤더 복사본 `[R11+0x3c]`, `+0x54f BT word [R11+0x3c],0xc`) 켜짐 | `0xC000007B` (`+0x40b`) |
| `0x58` | `+0x26e JZ → +0x56b`, `+0x56b ~ +0x57f` | 머신이 x86/x64 **아님** (예 ARM64) **이고** `RELOCS_STRIPPED` 켜짐 | `0xC000007B` |
| `0x59` | `+0x56f JZ → 조각 +0x1b3b5c ~ +0x1b3b78` | 머신이 x86/x64 **아님** 이고 `RELOCS_STRIPPED` 꺼짐 **이고** `DllCharacteristics & 0x140 ≠ 0x140` (`DYNAMIC_BASE 0x40` 과 `NX_COMPAT 0x100` 가 **둘 다** 있어야 통과) | `0xC000007B` |

- 정리: x86/x64 이미지는 `RELOCS_STRIPPED` 와 `APPCONTAINER` 가 같이 서면 안 된다 (`0x57`). 그 밖 머신은 재배치 있는 이미지 (`RELOCS_STRIPPED` 없음) + `DYNAMIC_BASE` + `NX_COMPAT` 가 모두 필요 (`0x58`·`0x59`). 통과하면 `+0x27e XOR EAX,EAX`.
- **test.exe**: `Machine 0x8664` (→ `MiLegacyImageArchitecture` 1), `Characteristics 0x22` (`RELOCS_STRIPPED` 꺼짐 → `R8W = 0`) → `+0x274 TEST R8W,R8W` 가 0 → `+0x27e` 성공. **`0x57 ~ 0x59` 안 탐.** (`DllCharacteristics 0x8160` 은 안 보임.)

### 3-2. `Feature_TrustedLaunchHosts…` 가 무엇을 켜고 끄나, test.exe 에서 타나

**함수 자체** (`Feature_TrustedLaunchHosts__private_IsEnabledDeviceUsageNoInline`, nt `+0x4f3a04`, 49바이트): 전역 `Feature_TrustedLaunchHosts__private_featureState` (`.data` rva `0xfc40f8`, **파일에 초기값 없음** — 실행 때 채움) 의 비트 4 (`+0x14 TEST AL,0x10`) 가 켜져 있으면 `state & 1` 반환 (`+0x18`), 아니면 `…IsEnabledFallback(state, 3)` (`+0x22 ~ +0x27`) → `wil_details_IsEnabledFallback(state, 3, &서술자 nt+0xde388)`: 상태 비트 1 이 꺼져 있으면 `…ReevaluateCachedFeatureEnabledState` 로 다시 계산하고, 사용 보고 (`…ReportUsageToService`) · 빠른 길 켜기 (`…TryEnableDeviceUsageFastPath`) 를 하고 `state & 1` 반환. **한 비트짜리 기능 스위치** (WIL 기능 플래그).

**이 스위치가 갈라놓는 줄** (부르는 곳은 `Feature…NoInline` 의 호출자 전부: `NtCreateUserProcess` · `MmCreateSpecialImageSection` · `MiInitializeCreateSectionPacket` · `MiCreateNewSection` · `MiValidateExistingImage` · `SepVerifyDesktopAppxImage`):

| 줄 | 켜짐 | 꺼짐 |
|---|---|---|
| `NtCreateUserProcess +0x7c3/+0x7c8`, `+0x7ca JZ → +0x8bf` | `+0x7ff SeDuplicateTokenWithPredictedClaims` 길 (+ 만들기 정보 `[RBP+0x328]` 비트 3 이 켜져 있으면 `SeTcbPrivilege` 확인 뒤 만들기 플래그에 `\|= 8`, `+0x838 ~ +0x8b2`) | `+0x8dd SeDuplicateTokenAndAddOriginClaim` 길 (`[RBP+0x330] ≠ 0` 일 때) |
| `MmCreateSpecialImageSection +0x64/+0x69 / +0x6b / +0x6d ~ +0x73` | 호출자 플래그 비트 3 (`TEST DIL,8`) 이 있으면 만들기 플래그 `\|= 0x20` | 안 함 |
| `MiInitializeCreateSectionPacket +0x1a5/+0x1aa / +0x1ac / +0x1ae ~ +0x1b2` | 만들기 플래그 `& 0x20` 이면 패킷 비트 27 켬 (`+0x373 BTS [RBX],0x1b`) | 안 함 |
| `MiCreateNewSection +0x338/+0x33d / +0x33f`, `MiValidateExistingImage +0x16a/+0x16f / +0x171 / +0x173 ~ +0x179` | 패킷 비트 27 이 켜져 있으면 서명 정책 인자 플래그 비트 21 켬 (`MiCreateNewSection +0x5fb ~ +0x607`, `MiValidateExistingImage +0x347`) | 안 함 |

- 그러니 **이 스위치가 켜져도, 서명 정책에 닿으려면** 호출자가 준 플래그 비트 3 (`NtCreateUserProcess` 의 `[RBP+0x328]` 비트 3 → `SeTcbPrivilege`) 이 같이 서야 한다. 그 출처 (누가 `[RBP+0x328]` 비트 3 을 주나) 는 안 따라감 `[공백]`.
- **test.exe 에서 타나**: 부르는 줄은 test.exe 길에 **늘 있다** (`NtCreateUserProcess +0x7c3` 은 직선, `MmCreateSpecialImageSection +0x64` 직선, `MiInitializeCreateSectionPacket +0x1a5` 는 속성 `SEC_IMAGE` (`0x1000000`) 가 켜져 있으면 닿음 (`+0x116 TEST R13D,EDI / +0x119 JNZ → +0x17e`; test.exe 는 `SEC_IMAGE`, run31), 새 길은 `MiCreateNewSection +0x338`, 공유 길은 `MiValidateExistingImage +0x16a`) `[정적]`. **값 (켜짐/꺼짐)** 은 실행 때 값이라 정적으로 못 정함 → 동적. 비트 27 이 서는 조건은 보통의 cmd → test.exe 길에선 아닐 것으로 읽힘 `[추측]` (`[RBP+0x328]` 출처 안 따라갔으므로).

### 3-3. run31 `+0x86` (ret `+0x8b`) 세션 id 함수

- `MmCreateSpecialImageSection +0x86 CALL 0x1402e8280` → nt rva `0x2e8280`. PDB 이름 **둘** (`pdb_aliases.py --lookup ntoskrnl.exe 0x2e8280`): **`PsGetProcessSessionIdEx`**, **`PsGetSessionIdEx`**. (Ghidra 가 고른 이름은 `PsGetProcessSessionIdEx`. 같은 주소 호출은 `MiCreateSectionCommon +0x199`, `MiRelocateImageAgain +0x165`, `MiRelocateImage` 조각, `MiMapViewOfImageSection +0xeda/+0xf0a`, `MmCreateSectionEx +0xcc`, `PspAllocateProcess +0x14cb/+0x153a/+0x1550` 등. `PsGetProcessSessionId` (rva `0x379000`) 는 다른 함수.)
- 동작 (`PsGetProcessSessionIdEx.asm`, 34바이트): `RAX = [EPROCESS+0x2e8]` (`Session`), 0 이거나 `[EPROCESS+0x5fc] & 0x1000` (`Flags3.SystemProcess`) 이면 `0xFFFFFFFF`, 아니면 `[Session+0x8]`. 인자 = `MmCreateSpecialImageSection +0x76 ~ +0x7f` 에서 `GS:[0x188]→[+0xb8]` = **지금 프로세스** (만드는 쪽). 결과는 `+0x93 MOV EDI,EAX` → `+0xa4 [RSP+0x58]` 로 `MiCreateSection` 에 넘어가 패킷 `+0xa8` 이 된다.

### 3-4. `MiCreateSection` 안쪽 재시도 (같은 함수 안 되돌아가는 갈림)

- 줄: `+0xf3/+0xf8 MiCreateImageOrDataSection` 뒤 `+0xfa CMP EAX,0xC0000054 / +0xff JZ → +0x21a`, `+0x105 CMP EAX,0xC0000476 / +0x10a JZ → +0x21a`.
- `+0x21a ~ +0x237`: 패킷의 파티션 객체 (`[RSP+0x130]`) 가 있으면 `PsDereferencePartition(…, 'MMCp')` (`+0x21a ~ +0x225 JNZ → +0x252 ~ +0x25c`), `EBX = (패킷 비트 24 만 남김)` (`+0x227 ~ +0x234`), **`+0x237 JMP +0x48`** (맨 앞의 `memset` 부터) → 패킷 `memset` (`+0x58`) → `MiInitializeCreateSectionPacket` 다시 (`+0xc6`) → `+0xd5 OR [패킷],EBX` 로 비트 24 만 되살림 → `MiCreateImageOrDataSection` 다시.
- 조건 = **`MiCreateImageOrDataSection` 이 `0xC0000054` 또는 `0xC0000476` 을 돌려줄 때.** 횟수 세는 줄은 없다.
  - `0xC0000476`: 위 1-2 S1 / 1-7 (파티션 불일치, 비트 24 를 남겨 둘째 바퀴엔 통과). test.exe 에선 안 탈 것.
  - `0xC0000054`: `MiCreateImageFileMap +0x103/+0x108 MiFlushDataSection` 이 이 값을 주면 (`+0x10b CMP EAX,0xc0000054 / +0x110 JZ → +0x5bb`), 또는 `MiReadImageHeaders +0x14e/+0x154` 가 헤더 읽기 I/O 상태를 그대로 올릴 때. 이름은 `STATUS_FILE_LOCK_CONFLICT` 로 알려진 값 `[추측]`.
- 바깥쪽에 또 하나 (위 단계): `MmCreateSpecialImageSection +0xd8 CMP 0xc0000054 / +0xdd JNZ` 아니면 `+0xea KeDelayExecutionThread (MiHalfSecond)` 로 쉬고 `+0xef JMP +0x8b` 로 되돌아감 (run31). 안쪽 `MiCreateSection` 은 쉬지 않고 바로 다시 한다.

### 3-5. 우리 (PE_emulator) 맞나

- **`0x57 ~ 0x59`: 빠짐.** `Section.cpp::_CheckImageFields` 주석 "안 함: `MiLegacyImageArchitecture` 디컴파일 없음, Q62(e) 미분석" — 이제 근거 있음. x64 만 다루면 필요한 건 **`0x57` 하나** (`RELOCS_STRIPPED` + `APPCONTAINER`, `0xC000007B`). test.exe·dll1·dll2 는 안 걸림 (`Characteristics 0x22`).
- `Feature_TrustedLaunchHosts`: 우리에 없음 — **맞다** (켜진대도 test.exe 길의 갈래는 `[RBP+0x328]` 비트 3 이 서야 달라짐 `[추측]`. 값은 확인 필요).
- 세션 id: 우리 `MmCreateSpecialImageSection` 은 "세션을 나누지 않는다" — **맞다** (진짜도 서명 정책 인자와 `MiWalkEntireImage` 넷째 인자뿐). 단 `PsGetProcessSessionIdEx` 가 `SystemProcess` 이면 `0xFFFFFFFF` 를 돌려주는 점은 **확인 필요** (우리 부모가 시스템 프로세스 취급인지).
- `MiCreateSection` 재시도: 우리 "안 둠" — **맞다** (우리 사슬이 `0xC0000054`·`0xC0000476` 을 안 돌려줌).

---

## 동적분석으로 확인해야 할 것

(주소는 `nt+rva`, rva 는 이 문서 값. 모두 `[정적]` 이 못 정한 값.)

1. **test.exe 가 공유 길을 타나** (Q61 D1 과 같은 실험, 같은 부팅에서 두 번째 실행): `nt+0x96e6cc` (`MiShareExistingControlArea` 입구) 와 `nt+0x96f4d0` (`MiCreateNewSection` 입구) 중 어느 쪽이 서나. 공유 쪽이 서면 아래 2 ~ 4.
2. **`ImageActive` 값**: `nt+0x971d68` (`MiRelocateImageAgain +0x80`) 에서 `dd @rbx+0x5c L1` 비트 22 (`0x400000`). 첫 test.exe 가 끝난 뒤 두 번째 실행이면 **꺼져 있어야** 새 기준을 고른다 (`[추측]`). 켜져 있으면 `nt+0x97330c` (`MiSwitchBaseAddress` 입구) 안 서고 기준 주소가 첫 실행과 같음. 서면 `+0x4e` (`nt+0x97335a`) 직전 `rcx`·`rax`, 전후 `dq @rdi+0x20 L1` (`BasedAddress`).
3. **서명 검사 갈래**: `nt+0x96fcaf` (`MiValidateExistingImage +0xdb`) 의 `eax` (0 기대) 와 `[rsp+0x90]` (요구 수준), `nt+0x96fd43` (`+0x16f`) 의 `eax` (Feature 켜짐/꺼짐), `nt+0x96fe31` (`+0x25d`) 의 `eax` (0 기대). `dd nt+0xfc40f8 L1` (feature 상태, 비트 4 · 0). `dd nt+0xe2d838 L1` (이유 전역, 성공이면 이전 값 그대로).
4. **서명 비교 비트 `ESI`**: `nt+0x96fe2c` (`MiValidateExistingImage +0x258`, `MiValidateSectionSigningPolicy` 부르기 직전) `esi` — 비트 1·2 (`0x6`) 가 둘 다 0 이면 정책 함수가 바로 0 (정적 읽기: `MiValidateSectionSigningPolicy +0x37 TEST SIL,0x6`).
5. **재시도 안 타나**: `nt+0x96d7fa` (`MiCreateSection +0x21a`) 가 안 서야 맞음. 서면 직전 `MiCreateImageOrDataSection` 반환 `eax` (`nt+0x96d6d8` 직후).
6. **프로토 보호 값** (새 길일 때): `nt+0x971844` (`MiParseImageSectionHeaders +0x1b4`) 에서 `r15d` 가 섹션 6 번 `3, 1, 5, 1, 5, 1` 순서로 나오나 (표 2-4). 상태 사진이 더 싸다: 만든 CA 의 서브섹션 사슬 `[sub+0x20]` 비트 1~5 가 `1, 3, 1, 5, 1, 5, 1` (헤더 포함 7 칸).
7. **헤더 페이지 꽂기**: `nt+0x315c52` (`MiInitializeNewImageSectionProtos +0x1a2`) 가 test.exe 한 번에 1 번 서나, `nt+0x4ab9a8` (`MiFinalizeImageHeaderPage` 입구) 가 1 번 서나 (정적 읽기: 꽂은 쪽만 Finalize).
8. 추천 멈춤점 (한 번에 쓸 것): `nt+0x96e6cc`, `nt+0x971d68`, `nt+0x97330c`, `nt+0x96fd43`. test.exe 조건을 붙여 (`.if` / 프로세스 이름 비교) 모든 프로세스에서 안 멈추게.

## 다음 요청 후보

1. **`MiReferenceControlArea` 세 갈래** (비었음 / 멀쩡 / 만드는 중·지우는 중) 와 그 status (`0xC000060B`, `0xC0000476`): `.asm` 은 이미 있고 run31 은 대표만 봄. `MiCheckControlArea +0x16c` (PFN 참조 0 인 쪽 = CA 지우는 길로 읽힘) 와 같이 "CA 가 언제 사라지나" (공유 길 대 새 길 갈림의 수명) 를 닫을 수 있다.
2. **`MiApplyBytestreamFixup` + 기록 `+0x10` 목록** (`MiSwitchBaseAddress` 의 두 고리 · 적재 구성 `+0x18` 가 가리키는 칸 둘): 뜻을 못 읽음 `[공백]`. 기록 `+0x48` 적재 구성의 출처 `MiParseImageLoadConfig` 와 함께.
3. **`MiParseImageSectionHeaders` / `MiBuildImageControlArea` 실패 이유 `0x10 ~ 0x1f`** 표: 이번에 asm 을 읽으며 번호·줄이 거의 나왔다 (`MiParseImageSectionHeaders` `0x10 ~ 0x19`, `MiBuildImageControlArea` `0x1a ~ 0x1f`) — `MiVerifyImageHeader` 표와 한 장으로 묶으면 PE_emulator `_CheckImageHeader` 의 빠진 검사를 채울 수 있다.
4. `SeGetImageRequiredSigningLevel` / `SeCompareSigningLevels` / `MiValidateSectionCreate` 본문 (이번엔 이름만, 요청서 범위).
5. `[RBP+0x328]` (`NtCreateUserProcess` 만들기 정보) 비트 3 의 출처 — `Feature_TrustedLaunchHosts` 가 test.exe 길에서 실제로 갈래를 바꾸는지 닫으려면.
6. (PE_emulator 쪽) `Section.cpp:418` 주석 정정과 `0x57` 검사 추가는 구현 요청에서 다룰 몫.

## 판정

**분석성공.** 세 질문 모두 `.asm` 줄로 답했다. 단 (d) 는 요청서 전제를 정정했고 (보호값을 채우는 곳은 `MiParseImageSectionHeaders`, `MiInitializeNewImageSectionProtos` 는 헤더 페이지를 프로토에 꽂음), 정적으로 못 정한 것 (Feature 값, `ImageActive` 실제 값, 공유 길을 test.exe 가 타는지, `MiApplyBytestreamFixup` 뜻) 은 `[공백]`/동적으로 남겼다. 버전 대조 일치.

# 41 section-chain — 정적 분석 결과 (Q78b)

- 요청서: `_workspace/sast/41_section-chain.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`, 기준과 같음).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` 34,983줄 + analyze 로그 `SetPdb:` 다 있음). ntfs.sys 는 요청대로 안 뽑았다.
- 새로 뽑은 함수 35개, 전부 depth 0 (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`). 넓힌 이유는 아래 "한눈에" 와 질문 1·4.
  - 섹션 쪽: `MiFinishCreateSection` (**`_SECTION` 객체를 만드는 함수 — 요청서 표에 없던 것. `MmSectionObjectType` 참조 `refs` 로 찾음**), `MiParseComAndCetHeaders`, `MiSelectImageBase`, `MiUpdateImageSystemWideBitmaps`, `MiInitializeNewImageSectionProtos`, `MiValidateExistingImage`, `MiMakeImageReadOnly`, `MiFinalizeImageHeaderPage`, `MiReferenceExistingControlArea`, `MiReferenceControlAreaFile`, `MiDereferenceControlAreaFile`, `MiSectionControlArea`, `MiCallCreateSectionFilters`, `MiCreateMdl`, `MiAllocatePool`.
  - 파일 객체 쪽: `IopCreateFile`, `IoCreateFileEx`, `IopParseDevice` (9085바이트), `IopParseFile`, `IopAllocRealFileObject` (**파일 객체를 할당하고 칸을 채우는 함수 — 요청서 표에 없던 것, `IopParseDevice +0xe19` 가 부름**), `IopAllocateIrpExReturn`, `IopQueueThreadIrp`, `IopSetFileObjectExtensionFlag`, `ObOpenObjectByNameEx`, `ObpLookupObjectName`, `IopCloseFile`, `PspDeleteCreateProcessContext`, `IoSetShareAccess`·`IoSetShareAccessEx`·`IoCheckShareAccess`·`IoCheckShareAccessEx`·`IoUpdateShareAccess` 와 그 아래 `IoSetLinkShareAccess`·`IoCheckLinkShareAccess`·`IoUpdateLinkShareAccess`.
  - 이미 있던 `.asm` (sast/26): `MmCreateSpecialImageSection`, `MiCreateSection`, `MiCreateImageOrDataSection`, `MiCreateNewSection`, `MiCreateImageFileMap`, `MiBuildImageControlArea`, `MiSectionCreated`, `MiShareExistingControlArea`, `MiReferenceControlArea`, `MiInitializeCreateSectionPacket`, `MiParseImageSectionHeaders`, `MiRelocateImage`, `MiVerifyImageHeader` 는 다시 안 뽑고 읽었다.
- `refs` 5번: `MmSectionObjectType`, `IopParseDevice`, `IopParseFile`, `IoSetLinkShareAccess`, `IoCheckLinkShareAccess` → `ms/disassemble/ntoskrnl/_refs/`.
- 구조체 칸 표: **`pdb_struct.py` 로 뽑았다** (도구 평가는 아래 "도구 평가"). 사진 값은 `ms/logs/04_baggage-read_K1.txt` 1381~1500줄 (`K1 07` 섹션 사슬, `K1 08` 파일 객체).
- 버전 확인: **일치.**
  - `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess`. `PspInsertThread` call `+0x802` / ret **`+0x807`** → `PspCallProcessNotifyRoutines` (`_calls.tsv`).
  - 사진 K1 의 `_SECTION_IMAGE_INFORMATION` 값이 `ms/bin/test.exe` 헤더와 칸마다 맞는다: `TransferAddress` `7ff61a371d40` (= 새 기본 주소 + `0x1d40`), 스택 `0x100000`/`0x1000`, 부시스템 3, 버전 6.0, `DllCharacteristics` `0x8160`, `Machine` `0x8664`, `ImageFileSize` `0x86a00` (551,424), `SizeOfHeaders` `0x400`, `SizeOfImage` `0x8c000`, 시간 도장 `6ad22b72`. 디버그 디렉터리(rva `0x791d0`)는 있고 COM 디렉터리는 0 (직접 읽음). 바이너리와 타깃 VM 이 같다고 볼 수 있다.

## 도구 평가 (`pdb_struct.py`)

고칠 버그는 없다. 참고 3건 (스크립트는 안 고쳤다).

- **잘 된 것**: 다섯 구조체 칸 수가 baggage 7.2 와 정확히 같다 (`_SECTION` 9, `_CONTROL_AREA` 17, `_SEGMENT` 11, `_SECTION_IMAGE_INFORMATION` 26, `_FILE_OBJECT` 30; 비트 칸 포함). 이름 있는 안쪽 형 (`_MMSECTION_FLAGS`, `_SEGMENT_FLAGS`, `_EX_FAST_REF`, `_MI_IMAGE_INFO_REF`, `_OPEN_PACKET` 0xe0 등) 을 이름으로 따로 뽑는 것도 됐다. 한 번에 0.1초.
- **한계 1 — 이름 없는 공용체 (`<unnamed-tag>`) 는 안쪽 칸을 안 펼친다.** `_SECTION.u1`·`.u`, `_CONTROL_AREA.u`·`.u1`·`.u2`·`.u3`, `_SEGMENT.u1`·`.u2` 는 한 줄로만 나온다. 이번엔 sast/38 이 만든 `ms/disassemble/ntoskrnl/_baggage/struct__*.tsv` 의 펼친 칸을 대신 봤다 (예: `u2.e2.ImageRelocationSizeIn64k` 0:16, `u3.ImageCrossPartitionCharge`). 제안: `--expand` 옵션으로 `<unnamed-tag>` 안 멤버를 `u2.e2.…` 꼴로 펼치기.
- **한계 2 — 겹치는 칸 표시가 없다.** `SubSystemMinorVersion`(+0x24)·`SubSystemMajorVersion`(+0x26)·`SubSystemVersion`(+0x24) 처럼 같은 바이트를 가리키는 칸이 나란히 나오고, 겹침인지는 오프셋을 눈으로 비교해야 한다. 제안: 앞 칸과 바이트가 겹치면 `overlap` 열에 앞 칸 이름.

## 한눈에

- **`_SECTION` 객체는 `MiFinishCreateSection` 이 만든다** (`MiCreateSection +0x120` → ret `+0x125`). 컨트롤 영역·세그먼트 사슬(`MiCreateImageOrDataSection` 이하)이 **다 끝난 뒤** `+0x143 ObCreateObjectEx` (`MmSectionObjectType`, 몸통 0x40바이트). 사슬 안(`MiCreateNewSection` 이하)에서는 섹션 객체를 안 만든다. 몸통 0x40바이트는 **패킷(스택 0xc8바이트) `+0x50~+0x8f` 에 모아 둔 사본을 한 번에 복사**한다 (`+0x15b~+0x179`).
- **컨트롤 영역·세그먼트 칸은 거의 `MiBuildImageControlArea` 한 함수가 쓴다** (새로 만드는 길). 풀에서 받은 칸은 0 으로 채워져 오고 (`MiAllocatePool` 플래그에 "초기화 안 함" 비트가 없음), 안 쓰는 칸은 그대로 0. 다른 칸 몇 개는 `MiParseImageSectionHeaders`(`DefaultProtectionMask`·`ImageCommitment`·`u3`), `MiCreateImageFileMap`(`u1.PartitionId`·`Binary32`), `MiSectionCreated`(`FilePointer`·`WaitList`·`BeingCreated` 끄기), `MiSelectImageBase`·`MiRelocateImage`(`u2`·`BasedAddress`) 가 쓴다.
- **파일 객체는 `IopParseDevice +0xe19 → IopAllocRealFileObject` 가 할당** (`+0x16a ObpAllocateObject`, 크기 `0xd8`, 바로 뒤 `memset`). 칸 30개 중 **nt 가 쓰는 건 8개**, **`FsContext`·`FsContext2`·`SectionObjectPointer`·`Vpb` 4개는 nt 에 쓰는 줄이 없다 (파일 시스템 몫)**, `ReadAccess`·`SharedRead`·`SharedDelete` 3개는 **파일 시스템이 nt 공개 함수 `IoSetShareAccess` 를 불러서 nt 코드가 쓴다**.
- **`_SECTION_IMAGE_INFORMATION` 남은 10칸**: 겹침 2 (`SubSystemVersion`·`OperatingSystemVersion`), `ImageFlags` 비트는 **전부 조건부** (COM 디렉터리·플랫 매핑·4GB 아래 요청이 있을 때만) 라 test.exe 는 `ImageDynamicallyRelocated` 하나만 켜진다.
- **test.exe 가 공유 길인지 새 길인지는 사진 값으로 못 가른다** (두 길이 같은 숫자를 만든다). 동적 질문 D1.

## 요약 표 (구조체별 모르는 칸 74 → ?)

분류: **아는 칸(nt)** = nt 의 쓰는 줄 찾음 / **0 그대로** = 쓰는 줄이 조건부거나 0 을 쓰거나 풀이 0 으로 줌 (이 실행에서 값 0) / **다른 드라이버** = ntfs.sys 몫, nt 에 쓰는 줄 없음 / **nt 함수·FS 가 부름** = 쓰는 코드는 nt 인데 부르는 쪽이 ntfs.sys / **범위 밖 길** = 쓰는 길은 이름만 (페이지 폴트·매핑).

| 구조체 | 모르는 칸 | 아는 칸(nt) | 0 그대로 | 다른 드라이버 | nt 함수·FS 가 부름 | 범위 밖 길 | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `_SECTION` | 9 | 5 | 4 | 0 | 0 | 0 | 0 |
| `_CONTROL_AREA` | 15 | 8 | 5 | 0 | 0 | 2 | 0 |
| `_SEGMENT` | 10 | 8 | 2 | 0 | 0 | 0 | 0 |
| `_SECTION_IMAGE_INFORMATION` | 10 | 3 (겹침 2 + `ImageFlags`) | 7 | 0 | 0 | 0 | 0 |
| `_FILE_OBJECT` | 30 | 8 | 15 | 4 | 3 | 0 | 0 |
| **합** | **74** | **32** | **33** | **4** | **3** | **2** | **0** |

- "아는 칸(nt)" 중 줄은 알아도 **어느 갈래를 탔는지는 동적**인 것: `_CONTROL_AREA.u2` (줄 후보 5곳), `_FILE_OBJECT.Flags` (nt 가 켜는 비트 둘 + ntfs 가 켜는 비트).
- `_CONTROL_AREA` 범위 밖 2개 = `NumberOfPfnReferences`, `NumberOfMappedViews`. (`FilePointer` 는 포인터 부분은 아는 칸, 아래 비트(`RefCnt` 0:4)는 범위 밖 — 질문 2.)
- **`_FILE_OBJECT` 의 0 그대로 15개 중 7개는 코드가 0 을 명시적으로 쓴다** (nt 4: `RelatedFileObject`·`CurrentByteOffset`·`Waiters`·`IrpListLock`, FS 가 부르는 nt 함수 3: `WriteAccess`·`DeleteAccess`·`SharedWrite`). 나머지 8개는 `memset` 만.

## 질문별 답 [정적]

### 1. `_SECTION` 만들기와 칸 채우기

**답: 객체 할당은 사슬의 맨 끝 형제 `MiFinishCreateSection +0x143` (ret `+0x148`) `ObCreateObjectEx`. 칸 값은 사슬 안에서 패킷에 모아 두었다가 여기서 복사.**

- **어디서 부르나**: `NtCreateUserProcess +0x937` (ret `+0x93c`) → `MmCreateSpecialImageSection +0xcf` (ret `+0xd4`) → `MiCreateSection +0xf3` (ret `+0xf8`, 컨트롤 영역·세그먼트를 만드는 `MiCreateImageOrDataSection`) → **같은 `MiCreateSection` 이 `+0x120` (ret `+0x125`) 에서 `MiFinishCreateSection` 를 부름**. 즉 `MiCreateImageOrDataSection → MiCreateNewSection → …` 사슬이 끝나고 돌아온 뒤다. `ObInsertObjectEx` 로 핸들 만들기는 그 다음 `MmCreateSpecialImageSection +0x141`.
- **`ObCreateObjectEx` 호출 인자**: 객체 형식 `MmSectionObjectType` (`+0x11d`), 속성 = 패킷 `+0x8`, 몸통 크기 `0x40` (`+0x13b MOV [RSP+0x28],0x40`), 쿼터 요금은 `0x40 + 세그먼트 PTE수×8` / `0x80 + 0x38×서브섹션수` 꼴 (`+0xee~+0x105`). `ObCreateObjectEx` 는 공용 부품(지도 `helper`)이라 이름으로만 가리킨다.
- **패킷 → 객체 복사**: 객체를 받으면 패킷 `+0x50~+0x8f` 64바이트를 `MOVUPS` 4번으로 객체 `+0x00~+0x3f` 에 통째 복사 (`+0x15b/+0x162/+0x16a/+0x172`). 그래서 **패킷 `+0x50+X` = `_SECTION +X`** 이다. 그 뒤 `+0x17d MOV [RCX+0x18],0`.
- **칸마다** (패킷 사본에 값이 들어가는 줄 → 복사):
  - `u1` (+0x28) = 컨트롤 영역 주소. `+0x2a MOV [RCX+0x78],R14` (R14 = 패킷 `+0x40` = 컨트롤 영역). 원격 파일이면 `+0x266~+0x27b` 가 파일 객체 주소에 `|1`(이미지) / `|2`(데이터) 를 붙여 덮음 (이 실행은 아님: K1 값이 컨트롤 영역 주소 `…983992b0`, 아래 비트 0).
  - `SizeOfSection` (+0x30) = 세그먼트 `SizeOfSegment` 를 원자적으로 읽은 값. 새 길 `MiCreateNewSection +0x21d CMPXCHG.LOCK [RCX+0x18]` → `+0x228 MOV [RBX+0x80],RAX`. 공유 길 `MiShareExistingControlArea +0x104` → `+0xac MOV [RBX+0x80],RAX`. 최대 크기 인자가 있으면 그 값 (`MiCreateNewSection +0x12e`). K1 `0x8c000`.
  - `u` (+0x38): 컨트롤 영역 `u`(+0x38) 를 패킷 `+0x88` 로 복사하며 bit19(`FilePointerNull`) 끔 (`+0x31 MOV EDX,[R14+0x38]`, `+0x3b BTR EDX,0x13`, `+0x43 MOV [RDI+0x88],EDX`). 객체를 만든 뒤 `+0x1b3~+0x1ba BTS EDX,0x10` → **`UserReference`(bit16)** 켬 (패킷 플래그 bit0 가 0 일 때, `+0x18a~+0x18e`). `Based`(bit6) 는 `SEC_BASED` 일 때만 (`+0x1ca` 검사 → `+0x1d3 OR EDX,0x40`), `NoChange`(bit14) 는 패킷 `0x400000` 일 때만 (`+0x252`). K1 `u`=`0x000100a0` = 컨트롤 영역 `u`(`0xa0` = `Image`·`File`) + `UserReference`.
  - `InitialPageProtection` (+0x3c, 0:12): 값 `0x10` 은 `MmCreateSpecialImageSection +0xb4 LEA R9D,[RAX+0x10]` (페이지 보호 인자) → `MiInitializeCreateSectionPacket +0x68 [RCX+0x1c]` 와 `+0xd7 MOV [RBX+0x8c],ECX` (패킷 `+0x8c` 의 0:12 비트 갈아 끼움) → 객체로 복사.
  - `NoValidationNeeded` (+0x3c, bit31): 먼저 꺼 둠 (`+0x35 MOV ECX,[RCX+0x8c]`, `+0x3f BTR ECX,0x1f`), 이미지(`SEC_IMAGE`)이고 **패킷 서명 수준 바이트(`[RDI+0x18]`) 가 1 이하**이고 패킷 플래그 bit18 이 꺼져 있으면 `+0x243 BTS ECX,0x1f` → `+0x247 MOV [RDI+0x8c],ECX`. K1 값 1 → test.exe 는 서명 수준이 낮게 넘어간 쪽 (`SeGetImageRequiredSigningLevel` 결과가 `MiCreateNewSection +0x318` 로 패킷 `+0x18` 에 들어감).
- **0 인 4칸의 이유**:
  - `SectionNode` (+0x00~+0x17), `EndingVpn` (+0x20): 패킷 사본이 `MiCreateSection +0x58 memset(…,0xc8)` 로 0 이고 이 칸을 쓰는 코드는 **`SEC_BASED`(패킷 `0x200000`) 섹션일 때만** 탄다 (조각 `FUN_140b23aba`: `+0x3b MiFindEmptyAddressRangeDownTree`, `+0xb8~+0xbf` 시작·끝 VPN, `+0x111 RtlAvlInsertNodeEx` 로 전역 AVL 트리 `140e2cd48` 에 잇기). test.exe 는 `SEC_IMAGE` 만이라 안 탄다.
  - `StartingVpn` (+0x18): **0 을 명시적으로 쓴다** (`+0x17d`). 같은 `SEC_BASED` 길에서만 값을 씀.
  - `SessionId` (+0x3c, 12:19): 컨트롤 영역이 `GlobalOnlyPerSession`(`u` bit27) 일 때만 `+0x2b3~+0x2e5` 가 패킷 `+0xa8`(세션 id) 를 넣는다 (`MiCreatePerSessionProtos` 도 같이). 일반 이미지는 0.
- 객체 머리(`_OBJECT_HEADER`) 는 범위 밖.

### 2. `_CONTROL_AREA`·`_SEGMENT` 칸 채우기와 두 길

**답: 새 길은 `MiCreateImageFileMap +0x2e1` → `MiBuildImageControlArea` 가 풀 3개(컨트롤 영역+서브섹션, 프로토 PTE, 세그먼트 0x98)를 받아 대부분을 쓴다. 공유 길은 칸을 거의 안 쓰고 카운터만 올린다.**

**새로 만드는 길** (`MiCreateImageOrDataSection +0x380` → `MiCreateNewSection` → `+0x210 MiCreateImageFileMap` → `+0x2e1 MiBuildImageControlArea`). 풀: 컨트롤 영역 `+0x8d MiAllocatePool` (태그 `MmCi`, 크기 `0xc8 + 0x38×(구역 수+1)`), 프로토 PTE `+0xdb` (태그 `MmSt`, 뒤에 `+0xf4 memset`), 세그먼트 `+0x107` (태그 `MSeg`, `0x98`). `MiAllocatePool` 첫 인자(플래그) `0x40`·`0x100` 에는 "초기화 안 함" 비트(`0x2`)가 없어서 **0 으로 채워져 온다** (`0x112` 짜리 프로토 PTE 만 `0x2` 가 있어서 직접 `memset`) `[정적]` (플래그 비트 뜻은 `ExAllocatePool2` 의 POOL_FLAGS 이름 기억 `[추측]`).

| 칸 (컨트롤 영역) | 쓰는 줄 (`MiBuildImageControlArea`) |
|---|---|
| `Segment` +0x00 | `+0x1f8 MOV [RSI],R12` |
| `ListHead` +0x08 (= `AweContext`) | `+0x1f4~+0x1ff` 자기 자신을 가리키는 빈 목록. 뒤에 VAD 가 이어짐 → 범위 밖 (매핑, 이름 `MiAddViewsForSection` `[추측]`) |
| `NumberOfSectionReferences` +0x18 | `+0x20b MOV [RSI+0x18],1` |
| `NumberOfUserReferences` +0x30 (아는 칸) | `+0x214 MOV [RSI+0x30],1` (패킷 플래그 bit0 가 0 일 때) |
| `u` +0x38 (아는 칸으로 승격) | `+0x9e OR [RAX+0x38],0x20` (`Image`), `+0x222 OR EAX,0x82` (`File`+`BeingCreated`), `+0x22b~+0x238` 선호 노드 bit20~26. `BeingCreated` 는 `MiSectionCreated +0xa6` 이 끔 |
| `u2` +0x58 | `+0x218 OR [RSI+0x58],-1` (`ImageRelocationStartBit` 초기값), `+0xb4 MOV [RAX+0x60],RCX` (`ImageInfoRef` = 할당 꼬리 `CA+0x80+0x38×n`, `_MI_IMAGE_ADDITIONAL_INFO` 0x48) |
| `FileObjectLock` +0x68 | `+0x227 MOV [RSI+0x68],R9` (R9=0) |
| `LockedPages` +0x70 | `+0x207 MOV [RSI+0x70],1` |
| `u3` +0x78 | 쓰는 곳은 `MiParseImageSectionHeaders +0x258 INC [R14+0x78]` (구역 훑으며 쪽마다 +1 → `ImageCrossPartitionCharge`, K1 `0x89`) |

- 안 쓰는 칸(풀 0): `NumberOfPfnReferences` +0x20, `NumberOfMappedViews` +0x28, `ControlAreaLock` +0x48, `ModifiedWriteCount` +0x4c, `WaitList` +0x50, `u1` +0x3c, `FilePointer` +0x40.
- 세그먼트 칸 (`R12`): `ControlArea` `+0x138 MOV [RAX],RSI` · `TotalNumberOfPtes` `+0x13b MOV [RAX+8],EBP` (EBP = `SizeOfImage` 를 쪽으로 올림 = `0x8c`) · `SizeOfSegment` `+0x145~+0x149` (`EBP<<12`) · `PrototypePte` `+0x134 MOV [RAX+0x40],R14` · `SegmentLock` `+0x130 MOV [RAX+0x28],R9` (0 을 씀) · `BasedAddress` +0x20 `+0x25e~+0x261` (헤더 `ImageBase`) · `SegmentFlags.ContainsDebug` `+0x246~+0x256` (디버그 디렉터리 있으면 bit5) · `u2.ImageInformation` `+0x13e` (아는 칸).
- 그 뒤 다른 함수가 쓰는 칸 (사슬 위):
  - `SegmentFlags.DefaultProtectionMask` = 7: `MiParseImageSectionHeaders +0x3c~+0x43` (바이트 `+0xe` 의 0:5).
  - `u1.ImageCommitment` +0x30: `MiParseImageSectionHeaders +0x249 INC [RDI+0x30]` (쓰기 가능 구역의 쪽마다). K1 `4`.
  - `NumberOfCommittedPages` +0x10: `MiParseImageSectionHeaders +0x240 INC [RDI+0x10]` — 조건 `CL` (`+0x381~+0x38a` 갈래) 가 있는 구역만. test.exe K1 값 0 → 안 탄 것.
  - `SegmentFlags.Binary32` (bit15): `MiCreateImageFileMap +0x415 OR word [RDI+0xc],0x8000` (헤더 `Magic` 이 PE32 일 때만). `ForceCollision` (bit14): `MiParseComAndCetHeaders +0x3aa` (COM 디렉터리가 있을 때만). 둘 다 test.exe 는 아님 (K1 `Short0`=0).
  - `u1.PartitionId` (컨트롤 영역 +0x3c 0:10): `MiCreateImageFileMap +0x31e MOV [R15+0x3c],CX` (파티션 번호 = 시스템 파티션 0). 즉 `u1` 은 **0 이 쓰인 것**.
  - `BasedAddress` 덮어쓰기: `MiRelocateImage` 조각 `+0x55` (= `+0x1b1269`) `MOV [RCX+0x20],RAX` (RCX = 세그먼트, RAX = `MiSelectImageBase` 가 고른 새 기본 주소). K1 `0x7ff61a370000`.
  - `u2.e2` 나머지: `ImageRelocationStartBit`·`ImageRelocationSizeIn64k`(0:16)·`BitMap`(20:2)·`ImageBaseOkToReuse`(bit23) = `MiSelectImageBase +0x66`, `+0x1d4`, `+0x1d8`, `+0x209` (그리고 조각 `+0x1b16d6~+0x1b16e2`); `ImageActive`(bit22) = `MiRelocateImage` 조각 `+0x1b127c BTS [R13+0x5c],0x16`. K1 `+0x58`=`ffffffff`, `+0x5c`=`0x00c00009` (크기 9 = `ceil(0x8c/16)`, bit22·23 켜짐). 어느 갈래로 이 값이 나왔는지는 못 정함 (D4).
  - `FilePointer` +0x40 와 `WaitList` +0x50: `MiSectionCreated` — 아래 "새 길의 마무리".

**공유 길** (`MiReferenceControlArea` 가 이미 있는 컨트롤 영역을 돌려줄 때, `BeingCreated` 꺼져 있음 → `MiCreateImageOrDataSection +0x1d0 MiShareExistingControlArea`)
- `MiReferenceControlArea +0x107 INC [RBX+0x18]` (`NumberOfSectionReferences`), **`+0x11d INC [RBX+0x30]`** (`NumberOfUserReferences`; 패킷 플래그 bit0 가 켜져 있으면 대신 `+0x1fa BTS` `WasPurged`), 이어서 `MiShareExistingControlArea` 가 `MiValidateExistingImage` (`+0xf4`) 를 부르고 `SizeOfSegment` 를 읽어 `SizeOfSection` 으로 줌 (`+0x104`, `+0xac`). **컨트롤 영역·세그먼트의 다른 칸은 안 건드린다.**
- ⚠ **정정**: baggage 3.21·run31 이 "`NumberOfUserReferences` +1 = `MiCreateImageOrDataSection +0x11d`" 라고 적었다. `.asm` 으로 보면 그 `INC` 는 **`MiReferenceControlArea +0x11d`** (`MiCreateImageOrDataSection` 에는 `[…+0x30]` 증가가 없음). sast/26 본문은 `MiReferenceControlArea` 를 말하고 있어서 옮겨 적다 틀린 것으로 보인다 `[정적]`.

**새 길의 마무리 (`MiSectionCreated`, 새 길에서만; `MiCreateNewSection +0x162`, ret `+0x167`)**
- `FilePointer` (+0x40): `+0x6f MOV RAX,[RDI+0x40]` → `+0x7a MOV [RBX+0x40],RAX`. RDI = 파일 객체의 `SectionObjectPointer->ImageSectionObject` (`+0x105 MOV RDI,[RDI+0x10]`), 즉 **자리표시용 컨트롤 영역** (`MiCreateImageOrDataSection` 의 스택 `RBP-0x69`, 0x80바이트) 의 `+0x40` = 파일 객체 주소. 그 자리표시용 칸은 `MiCreateImageOrDataSection +0x120 MOV [RBP-0x29],R15` 로 채움. **쓴 값은 포인터만 (아래 4비트 `RefCnt` = 0)**; K1 `…dc5d` 의 아래 4비트 `0xd` 는 뒤에 `_EX_FAST_REF` 참조를 쓴 흔적 (이름만: `MiReferenceControlAreaFile`/`MiDereferenceControlAreaFile` — `MmCreateSpecialImageSection +0x101/+0x117`, `MiParseComAndCetHeaders +0x9f` 도 부름 `[추측]` 어느 호출이 바꿨는지는 D5).
- `WaitList` (+0x50): `+0x11b MOV [RBX+0x50],RAX` — 자리표시용 컨트롤 영역의 `WaitList` 를 복사 (기다린 스레드가 없으면 0. 기다릴 때는 `MiReferenceControlArea +0x2bf` 가 채움). K1 null.
- `BeingCreated` 끄기 `+0xa6 AND [RBX+0x38],~2`, 파일의 `ImageSectionObject` 를 진짜 컨트롤 영역으로 교체 `+0x124 MOV [RSI+0x10],RBX`. (자리표시용 컨트롤 영역을 `SectionObjectPointer` 에 처음 꽂는 줄은 `MiReferenceControlArea +0x178 MOV [R15],RSI`.)
- `ControlAreaLock` (+0x48): `+0x31` 스핀락 잡고 `+0xc6 MiReleaseSpinLockExclusive` 로 놓음 → 값은 0 으로 돌아옴.
- 그 다음 `MiInitializeNewImageSectionProtos` (`+0xfe`) = 프로토 PTE 표 안 내용 → 범위 밖 (이름만).

**`FilePointer` 아래 비트 (`P0~P1`)·`NumberOfPfnReferences` (3→`0x2d`)·`NumberOfMappedViews` (K1 1) 누가 바꾸나**: 이 사슬에서 안 쓴다 (풀 0 에서 시작, K1 값은 이미 3·1). 매핑 (`MmInitializeProcessAddressSpace +0x1c9` → `MiMapProcessExecutable +0xb3` → `MmMapViewOfSectionEx` → `MiMapViewOfImageSection`; 이름만, Q78c/Q79) 과 페이지 폴트·프로토 PTE 처리 길 `[추측]`. `FilePointer` 아래 비트는 `_EX_FAST_REF` 참조 (PDB `RefCnt` 0:4 `[정적] PDB`).

**test.exe 가 어느 길이었나: 사진 값으로 못 가른다.**
- K1: `NumberOfSectionReferences`=1, `NumberOfUserReferences`=2, `NumberOfMappedViews`=1, `LockedPages`=1, `u`=`0xa0`.
- 새 길: 섹션 참조 1 (`+0x20b`) + 사용자 참조 1 (`+0x214`), 거기에 이 프로세스의 exe 매핑이 사용자 참조를 1 더 올리면 2 (매핑이 올린다는 것은 이름·옛 소스 기억 기준 `[추측]`, 매핑 코드는 이번에 안 읽음). 공유 길 (이전 프로세스의 섹션이 이미 닫혀 컨트롤 영역이 놀고 있던 경우): 섹션 참조 0→1 (`+0x107`) + 사용자 참조 0→1 (`+0x11d`) + 매핑 → 2. **같은 숫자.** `LockedPages=1`·`u2`·`BasedAddress` 도 두 길에서 같다.
- 가르는 방법은 동적: D1 (`MiCreateNewSection`/`MiShareExistingControlArea` 입구 중 어디가 서나, 또는 test.exe 를 재부팅 없이 두 번 돌려서 K1 의 컨트롤 영역 주소가 같은지).

**값 출처 (입력 / 계산)** — `_SEGMENT`·`_CONTROL_AREA` 칸:

| 칸 | 값 | 출처 |
|---|---|---|
| `TotalNumberOfPtes` | `0x8c` | 계산: 헤더 `SizeOfImage`(`0x8c000`) 쪽 수 (`ctx+0x10`) |
| `SizeOfSegment` | `0x8c000` | 계산: `TotalNumberOfPtes<<12` |
| `BasedAddress` | `0x140000000` → `0x7ff61a370000` | 입력 (헤더 `ImageBase`) → 계산 (`MiSelectImageBase`, ASLR) |
| `PrototypePte` | `0xffffb689a88c6b00` | 풀 주소 (실행 때 값) |
| `ControlArea` | `…983992b0` | 풀 주소 (실행 때 값) |
| `NumberOfCommittedPages`·`ImageCommitment` | 0 · 4 | 계산: 구역 표 속성 (`MiImageProtectionArray`) |
| `FilePointer` | 파일 객체 주소 | 입력 (열린 exe 파일 객체, 질문 4) |

### 3. `_SECTION_IMAGE_INFORMATION` 남은 10칸

- **`SubSystemVersion`·`OperatingSystemVersion`: 예, 겹침이다** `[정적] PDB`: `SubSystemVersion`(ULONG, +0x24) = `SubSystemMinorVersion`(+0x24)+`SubSystemMajorVersion`(+0x26) 같은 4바이트, `OperatingSystemVersion`(ULONG, +0x28) = `MajorOperatingSystemVersion`(+0x28)+`MinorOperatingSystemVersion`(+0x2a). K1 값도 맞다 (`SubSystemVersion` `0x60000` = 6<<16, `OperatingSystemVersion` 6). → 아는 칸으로 옮길 수 있다 (쓴 줄 `MiBuildImageControlArea +0x193/+0x19d/+0x1a7/+0x1b1`).
- **`ImageFlags` 비트** (모두 `MiBuildImageControlArea` 의 이미지 정보 R8 = 세그먼트 `+0x48`, 바이트 `+0x33`):
  - `ImageMappedFlat` (bit3): `MiBuildImageControlArea +0x3ed OR [R8+0x33],8` — 패킷 플래그 `0x100000` (R13D = `EDI & 0x100000`, `+0x5d`) 이 켜진 **플랫 매핑 요청**일 때만 (`+0x270 JNZ +0x3c3`). 같은 갈래가 `+0x3f2 [R8+0x32]=1` (`ImageContainsCode`) 도 씀. test.exe 는 아님.
  - `BaseBelow4gb` (bit4): `MiCreateImageFileMap +0x790 OR [RCX+0x33],0x10` — 패킷 플래그 bit8 (`+0x323 BT EBX,8`) 이 켜졌을 때만. test.exe 는 아님.
  - `ComPlusILOnly` (bit1)·`ComPlusNativeReady` (bit0)·`ComPlusPrefer32bit` (bit5): `MiParseComAndCetHeaders +0x3d2~+0x40c` — 헤더 COM 디렉터리(ctx `+0x50`) 가 있고 CLR 플래그가 맞을 때만 (`MiCreateNewSection +0x267`, ret `+0x26c` 가 부름). test.exe 는 COM 디렉터리 0 (직접 읽음) → 안 탐.
  - `Reserved` (6:2): 사슬에서 쓰는 줄을 못 찾았다 (예약 비트) `[추측]` 0.
  - `ImageDynamicallyRelocated` (bit2, 아는 칸): `MiRelocateImage` 조각 `+0x1b1357`. K1 `ImageFlags`=`0x4`.
- **`ZeroBits`** (+0x8): 이 사슬에 쓰는 줄이 없다 (`MiBuildImageControlArea` 가 쓰는 R8 오프셋: `0x0·0x10·0x18·0x20·0x24·0x26·0x28·0x2a·0x2c·0x2e·0x30·0x32·0x34·0x38·0x3c` 와 꼬리 `0x40·0x44·0x48`). 풀이 0 으로 줌 → 0 `[정적]`.
- **3.23 "오프셋 미상" 줄 채우기** (`MiBuildImageControlArea`, `R15`=헤더 칸 모음, `R8`=이미지 정보):

| 칸 | 줄 |
|---|---|
| `MaximumStackSize` +0x10 | `+0x175 MOV RAX,[R15+0x20]` → `+0x179 MOV [R8+0x10],RAX` |
| `CommittedStackSize` +0x18 | `+0x17d` → `+0x181 MOV [R8+0x18],RAX` |
| `SubSystemType` +0x20 | `+0x185 MOVZX EAX,word [R15+0x32]` → `+0x18a MOV [R8+0x20],EAX` |
| `SubSystemMajorVersion` +0x26 / `Minor` +0x24 | `+0x193` / `+0x19d` |
| `MajorOperatingSystemVersion` +0x28 / `Minor` +0x2a | `+0x1a7` / `+0x1b1` |
| `DllCharacteristics` +0x2e | `+0x1bb` |
| `CheckSum` +0x3c | `+0x1c4 MOV [R8+0x3c],EAX` |
| `ImageContainsCode` +0x32 | `+0x1d7 MOV [R8+0x32],AL` (AL=1: `SizeOfCode`≠0 `+0x1cb` 또는 진입점≠0 `+0x45e`). 추가로 실행 가능 구역이면 `MiParseImageSectionHeaders +0x1bf MOV byte [RAX+0x32],1` |
| `ImageCharacteristics` +0x2c | `+0x1df` (파일 헤더 `+0x12`) |
| `Machine` +0x30 | `+0x1e7` (파일 헤더 `+0x0`) |
| `LoaderFlags` +0x34 | `+0x1f0 MOV [R8+0x34],EAX` |
| `ImageFileSize` +0x38 | `+0x167 MOV [R8+0x38],EAX` (파일 크기 하위 32비트, `FsRtlGetFileSize` 결과) |

### 4. `_FILE_OBJECT` — exe 파일 객체

**사슬** (오프셋은 call → ret):
1. `NtCreateUserProcess +0x6c6` (ret `+0x6cb`) **`IopCreateFile`** — 인자: 접근 `SYNCHRONIZE|FILE_EXECUTE` (`0x100020`), 공유 5 (읽기+삭제), 처분 `FILE_OPEN`, 옵션 `0x60` (`FILE_NON_DIRECTORY_FILE|FILE_SYNCHRONOUS_IO_NONALERT`), 속성 `0x80`, 객체 속성 `[RBP+0xb8]` (`OBJ_KERNEL_HANDLE|OBJ_CASE_INSENSITIVE`, 사용자 모드 호출이면 `OBJ_FORCE_ACCESS_CHECK` 도: `+0x56e~+0x57e` 의 `0x240`/`0x640`). **`IoCreateFileEx` (`+0x73b`) 는 첫 번째가 실패했을 때만** 다시 부르는 재시도다 (`+0x6cf JNS +0x764` 가 성공이면 건너뜀) → exe 파일 객체를 만드는 쪽은 `+0x6cb` `[정적]`.
2. `IopCreateFile` 안: `_OPEN_PACKET` (0xe0바이트, PDB) 을 룩어사이드 (`+0x96`·`+0xba` ret) 나 풀 (`+0x34d`) 에서 받아 `CreateOptions`(`+0x40`)·`ShareAccess`(`+0x46`)·`RelatedFileObject`(`+0x28`)=0 등을 채움 (`+0x21b~+0x2ea`). 그 다음 조각 `FUN_1408c7e1f +0x6a` (ret `+0x6f`; = `IopCreateFile +0x4d1`, WinDbg 표시 미확인) **`ObOpenObjectByNameEx`**.
3. `ObOpenObjectByNameEx +0x21f` (ret `+0x224`) **`ObpLookupObjectName`**. 이름 길 `\??\…` → 심볼릭 링크 (`ObpParseSymbolicLinkEx +0xc80`) → 장치 객체. 장치 형식의 Parse 가 `IopParseDevice` 이면 직접 호출: `ObpLookupObjectName +0xb75 LEA RAX,[IopParseDevice]` 로 비교 → **`+0xbc8` (ret `+0xbcd`)** 와 `+0xc90` (ret `+0xc95`) 두 곳에서 `IopParseDevice`. `IopParseDevice` 포인터는 `IoCreateObjectTypes +0xeb/+0xfd` 가 장치 형식에 꽂음 (`refs`). `IopParseFile` 은 파일 객체 밑으로 상대 열기일 때 (`ObpLookupObjectName +0x135b`) 오는 징검다리일 뿐이다: `+0xbe MOV [RBX+0x28],RSI` (OpenPacket.RelatedFileObject = 부모 파일 객체) 하고 `+0xc2 IopParseDevice` 를 부름 → **절대 경로인 exe 는 안 탄다** `[정적 조건, D8]`.
4. **`IopParseDevice +0xe19` (ret `+0xe1e`) `IopAllocRealFileObject`** — 이게 할당·초기화. (`IopParseDevice` 에는 `+0x10d7` 의 두 번째 호출도 있는데, `OpenPacket.QueryOnly`(`+0x89`) 갈래이고 `IopCreateFile +0x29e` 가 `QueryOnly`=0 으로 둠 → exe 는 `+0xe19`.)
   - `IopAllocRealFileObject`: 룩어사이드 (`GS:[0x20]` 프로세서 블록 `+0x840`/`+0x848`, `ExpInterlockedPopEntrySList` `+0x9a`·`+0xb7`) 에서 받거나 `+0x16a` (ret `+0x16f`) **`ObpAllocateObject`** (크기 `0xd8`, 형식 `IoFileObjectType`) → `+0x21e` (ret `+0x223`) `memset(…,0xd8)` → 칸 채우기.
   - `ObOpenObjectByNameEx` 로 돌아오면 `+0x405` (ret `+0x40a`) `ObpCreateHandle` 로 핸들 만들기.
5. `IopCreateFile` 조각 `FUN_1408c7e1f +0xbb` (= `+0x522`) `BTS [FO+0x50],0x12` (`FO_HANDLE_CREATED` `0x40000`).
6. **파일 시스템 호출**: `IopParseDevice +0xcb7` (ret `+0xcbc`) `IopAllocateIrpExReturn` 로 IRP (스택 `MajorFunction`=0 `IRP_MJ_CREATE`: `+0xd3c MOV [R13],R12B`, 스택 `FileObject`=`+0xe5d`), `+0xf2f` `IopQueueThreadIrp`, **`+0xf5a` (ret `+0xf5f) IofCallDriver`** → 마운트된 볼륨의 파일 시스템 (`Vpb->DeviceObject`, `+0x71e`). 여기서부터는 ntfs.sys 몫 (안 뽑음).

**칸마다 주인 셋**

| 분류 | 칸 | 쓰는 줄 |
|---|---|---|
| (가) nt | `Type`·`Size` | `IopAllocRealFileObject +0x375 MOV [RBX],0xd80005` (Type 5 = 파일, Size `0xd8`) |
| (가) | `DeviceObject` | `+0x383~+0x387 MOV [RBX+8],R8` (3번째 인자 = `IopParseDevice` 의 `[RSP+0x70]` 장치) |
| (가) | `FileName` | `IopParseDevice +0xe95` (`MaximumLength`) · `+0xea8` `ExAllocatePool2` (태그 `IoNm`) → `+0xead MOV [R15+0x60],RAX` (버퍼) · `+0xec6 RtlCopyUnicodeString(&FO+0x58, 남은 이름)` |
| (가) | `Lock` | `IopAllocRealFileObject +0x319` (ret `+0x31e`) `KeInitializeEvent(FO+0x80, SynchronizationEvent)` — **동기 파일(`Flags & 2`)일 때만** (`+0x307`). K1 `0x00060001` 맞음 |
| (가) | `Event` | `IopParseDevice +0xf1a` (ret `+0xf1f`) `KeInitializeEvent(FO+0x98, NotificationEvent)`. K1 `SignalState`=1 은 IRP 완료가 켠 것 `[추측]` (이 요청에서 완료 코드는 안 읽음) |
| (가) | `IrpList` | `IopAllocRealFileObject +0x39d/+0x3a1` (`FO+0xc0` 자기 자신을 가리키는 빈 목록). K1 맞음 |
| (가) | `Flags` | 아래 표 |
| (가) 0 을 씀 | `RelatedFileObject` `+0x37f` (OpenPacket 값 = 0), `IrpListLock` `+0x38b`, `CurrentByteOffset`·`Waiters` `+0x320~+0x323` (동기일 때) | |
| (나) 다른 드라이버 (ntfs.sys) | `FsContext` +0x18, `FsContext2` +0x20, `SectionObjectPointer` +0x28, `Vpb` +0x10 | `IopParseDevice`·`IopAllocRealFileObject` 에 이 칸을 쓰는 줄이 **없다** (`IopAllocRealFileObject +0x419~+0x44d` 의 복사는 `[RBP+0x7f]`≠0 인 다시 열기 갈래). IRP 를 받은 FS 가 채움 `[정적: 쓰는 줄 부재]` + `[추측: 쓰는 주체 = ntfs.sys]` |
| (나') nt 함수·FS 가 부름 | `ReadAccess` +0x4a, `SharedRead` +0x4d, `SharedDelete` +0x4f | **`IoSetLinkShareAccess +0x16/+0x5b/+0x63`** (`IoSetShareAccess +0xf`·`IoSetShareAccessEx +0x1a` 가 부름) / `IoCheckLinkShareAccess +0x46~+0x99`. nt 안에서 이 함수를 부르는 곳은 `IoSetShareAccess`·`IoSetShareAccessEx`·`IoCheckShareAccess*`·`RawCreate`·`FsRtlpCheckSharingAgainstOplockOwner` 뿐이고 `IopParseDevice` 는 안 부른다 (`refs`) → 부르는 쪽 = FS `[추측]` |
| (다) 0 그대로 | `WriteAccess` +0x4b, `DeleteAccess` +0x4c, `SharedWrite` +0x4e | 같은 `IoSetLinkShareAccess` 가 0 을 씀 (`+0x23/+0x2a/+0x67`; 접근 `0x100020` 에 쓰기·삭제 없음, 공유 5 에 쓰기 없음) |
| (다) | `PrivateCacheMap` +0x30, `FinalStatus` +0x38, `LockOperation` +0x48, `DeletePending` +0x49, `Busy` +0x74, `LastLock` +0x78, `CompletionContext` +0xb0, `FileObjectExtension` +0xd0 | `memset` 만 (`IopAllocRealFileObject +0x21e`). 캐시 맵·완료 포트·확장은 이 열기에서 안 씀 |

- `ReadAccess`/`Shared*` 값 풀이: `IoSetLinkShareAccess` 는 `ReadAccess = (접근 & 0x21) ≠ 0`, `WriteAccess = (접근 & 6) ≠ 0`, `DeleteAccess = (접근 & 0x10000) ≠ 0`, 공유 비트 0/1/2 → `SharedRead/Write/Delete`. 접근 `0x100020`·공유 5 이면 1·0·0 / 1·0·1 → K1 과 맞음 `[정적]`.
- **`Flags` 비트 풀이** (이름은 ntifs.h 기억이라 `[추측]`):

| 비트 | 켜는 줄 | 이 실행 |
|---|---|---|
| `0x2` (동기 IO) | `IopAllocRealFileObject +0x2ee~+0x2f4` (`CreateOptions & 0x30` → `\|=2`), `0x4` (알림 가능) `+0x2fe` | K1 켜짐 |
| `0x40000` (핸들 만들어짐) | `IopCreateFile` 조각 `FUN_1408c7e1f +0xbb` (= `+0x522`) | K1 켜짐 |
| `0x40` (캐시 지원), `0x80000` | nt 에 이 값을 켜는 줄이 없음 → 파일 시스템 몫 `[추측]` | K1 켜짐 |
| `0x800` (장치 직접 열기) | `IopParseDevice +0xe37` | 아님 |
| `0x20000` | `IopAllocRealFileObject +0x36e` (인자 `[RBP-0x79]` bit6 이 꺼졌을 때) | K1 꺼짐 → 인자 bit6 켜져 있었음 |
| `0x400000` | `IopParseDevice +0xaa5` | 아님 |

- **`Flags` `0xc0042 → 0xc4442` (`K3~P0`, 비트 `0x4400`) 누가 켜나**: 섹션 만들기/매핑 쪽이 아니라 **exe 핸들을 닫는 때**로 보인다. `NtCreateUserProcess +0x11d0` (ret `+0x11d5`) `PspDeleteCreateProcessContext` 가 `+0x134` (ret `+0x139`) `ObCloseHandle` 로 문맥 `+0xb0` (= `NtCreateUserProcess` 의 `[RBP+0x250]` = exe 파일 핸들) 을 닫고 `+0x13e` 로 `+0xb8` (`[RBP+0x258]` 파일 객체 참조) 을 놓는다. 이 호출은 `PspInsertThread +0x1015` 뒤라서 `K3` (`PspInsertThread +0x807`) 다음 구간이다. 핸들을 닫으면 `IopCloseFile` 이 불리고 `+0x138` 에서 `IRP_MJ_CLEANUP`(0x12, `+0x10a`) 으로 FS 를 부른다. `0x4000` 은 `FO_CLEANUP_COMPLETE` 로 보이고 FS 의 cleanup 처리가 켜는 것 `[추측]`; `0x400` 은 이름 못 정함 `[공백]`. `IopCloseFile` 에 `0x4000`·`0x400` 상수는 없다. → D6.
- `Event*` (뒤에 또 바뀜): `IopCloseFile +0xb8` `IopResetEvent` 가 닫을 때 `Event` 를 다시 세운다 `[추측]` (같은 구간).

**다른 드라이버(ntfs.sys) 를 짐 지도 "다른 DLL" 열처럼 쓸 수 있나 — 의견**:
- 쓸 수 있다. 뜻이 같다 (범위 밖, 표시만, 값은 입력으로 취급). 열 이름은 `다른 DLL / 다른 드라이버` 처럼 합치거나 `쓴 주인` 값에 `다른 드라이버 (ntfs.sys)` 를 추가하면 된다.
- 다만 두 가지는 다르다: (1) **nt 가 이 값을 곧바로 읽는다.** `SectionObjectPointer` 는 `MiReferenceControlArea +0x44` 와 `MiCreateImageFileMap`·`MiSectionCreated +0x53` 가 읽고, 가리키는 `_SECTION_OBJECT_POINTERS`(0x18, FS 메모리) 의 **`ImageSectionObject` 칸은 nt 가 쓴다** (`MiReferenceControlArea +0x178` 자리표시, `MiSectionCreated +0x124` 진짜). → "다른 드라이버 몫이지만 에뮬레이터가 대신 채워야 하는 칸" 꼬리표 필요. (2) `ReadAccess`·`SharedRead`·`SharedDelete` 는 쓰는 코드가 nt (`IoSetLinkShareAccess`) 라서 "쓴 주인 nt, 부르는 쪽 다른 드라이버" 로 따로 적는 편이 PE_emulator 구현 이름과 맞는다.

### 5. 줄기와 경로

아래 "칸별 쓰는 곳" 표가 baggage 3장 양식이다. 줄임: `§섹션` = `①… → MmCreateSpecialImageSection → MiCreateSection`, `§새로` = `§섹션 → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap`, `§열기` = `①… → IopCreateFile → ObOpenObjectByNameEx → ObpLookupObjectName → IopParseDevice`. 줄기 위는 `MiBuildImageControlArea`·`MiFinishCreateSection`·`IopAllocRealFileObject` 처럼 더 부르는 게 없거나 공용 부품(`MiAllocatePool`·`ObCreateObjectEx`·`ObpAllocateObject`·`ExAllocatePool2`·스핀락)만 부르는 곳까지 봤다. 줄기 밖 이름만: `MiInitializeNewImageSectionProtos`·`MiValidateExistingImage`·`MiCallCreateSectionFilters`·`MiParseComAndCetHeaders` 의 COM 갈래·`MiCreatePerSessionProtos`·`MiFindEmptyAddressRangeDownTree`·`MiMakeSubsectionPte`·`MiMakeDemandZeroPte`·`MiUpdateSystemProtoPtesTree`.

### 6. 시스템 콜·입력, 갈림길

- **시스템 콜**: 이 조각 안에는 `syscall` 이 없다 (전부 커널 함수). 입구는 부모 cmd.exe 의 `NtCreateUserProcess` 시스템 콜 안.
- **읽는 입력**:
  - exe 이름: `NtCreateUserProcess` 의 `[RBP+0x290]` `UNICODE_STRING` 이 `ObjectName` (`[RBP+0xc8]`, `+0x584`). 이 칸을 쓰는 줄은 `NtCreateUserProcess` 안에서 못 찾았다 (앞 함수가 채움) `[공백]` — 부모가 넘긴 이미지 NT 경로로 보임 `[추측]`.
  - 파일 시스템에 묻는 것: 파일 크기 (`MiCreateImageFileMap +0x91 FsRtlGetFileSize`, 4GB 넘으면 거절), 헤더 첫 쪽 (`+0x157 MiCopyHeaderIfResident` 또는 `+0x4fc MiReadImageHeaders`), 열기 IRP 결과 (`FsContext*`·`SectionObjectPointer`·`Vpb`·공유 접근).
  - PE 헤더 칸 → `MiVerifyImageHeader` 가 `ctx`(0x70) 로 모음 → `MiBuildImageControlArea` 가 이미지 정보로 옮김 (질문 3 표): `ImageBase`·`AddressOfEntryPoint`·`SizeOfImage`·`SizeOfHeaders`·스택·부시스템·OS 버전·`DllCharacteristics`·`CheckSum`·`LoaderFlags`·`SizeOfCode`·디버그/COM/재배치 디렉터리 + 파일 헤더 `Machine`·`Characteristics`·`TimeDateStamp`.
  - 호출자가 넘기는 값: 페이지 보호 `0x10`, 서명 수준 바이트 (`NtCreateUserProcess` `R13B` → 패킷 `+0x18`), 세션 id (`MmCreateSpecialImageSection +0x86`), 플래그 (`+0x7bc` `LEA EBX,[1+RAX*4]` → 인자 6).
  - 시스템 상태: 시스템 파티션 `MiSystemPartition` (`MiCreateNewSection +0x8e`), 무작위 값 (`MiSelectImageBase` 의 ASLR), `MiHalfSecond` (재시도 쉬는 시간, `0xc0000054`).
- **이 조각 안 갈림길** (test.exe 는?):

| 갈림길 | 줄 | test.exe |
|---|---|---|
| 같은 파일 이미지 컨트롤 영역이 이미 있나 | `MiReferenceControlArea +0x66` 이하 → `MiCreateImageOrDataSection +0x1bf` (`BeingCreated`) | **동적** (D1). 사진으로 못 가름 |
| 컨트롤 영역이 만드는 중/지우는 중 → 기다림 | `MiReferenceControlArea +0x2fb KeWaitForGate` → `0xc000022d` 로 처음부터 다시 | 사진 K1 `WaitList` null → 안 기다림 |
| 헤더가 메모리에 있나 | `MiCreateImageFileMap +0x157` (있으면 복사, 없으면 `+0x4fc` 디스크 읽기) | 동적 (프리페치·캐시) |
| 프리페치 중인 스레드인가 | `MiCreateImageFileMap +0x133`, `MiShareExistingControlArea +0x132 PsIsCurrentThreadPrefetching` → `MmChangeSectionBackingFile` / `MiSectionCreated +0x4b` (`PrefetchCreated`) | K1 `u`에 bit9 없음 → 아님 |
| 원격 파일인가 | `MiFinishCreateSection +0xb8~+0xc2` (`DeviceObject->Characteristics & 0x10`) → `u1` 에 파일 객체 | K1 `u1`=컨트롤 영역 주소 → 아님 |
| `SEC_BASED` 인가 | `MiFinishCreateSection +0x1ca` | 아님 (`StartingVpn`·`SectionNode` 0) |
| 세션 전역 이미지인가 | `MiFinishCreateSection +0x63` (`GlobalOnlyPerSession`) | 아님 (`SessionId` 0) |
| COM(.NET) 디렉터리가 있나 | `MiParseComAndCetHeaders` | 아님 (헤더 직접 확인) |
| 플랫 매핑 / 4GB 아래 요청인가 | `MiBuildImageControlArea +0x270`, `MiCreateImageFileMap +0x323` | 아님 (`ImageFlags`=`0x4`) |
| 필터 드라이버가 막나 | `MiCreateImageOrDataSection +0x14a MiCallCreateSectionFilters` | 안 읽음 (이름만) |

## 칸별 쓰는 곳 (baggage 3.20 ~ 3.24 에 옮길 양식)

단계는 전부 `①`. 놓는 법은 `직접` 이 기본. "사진" 은 K1 값.

### 3.20 `_SECTION` (0x40)

경로 끝 `MiFinishCreateSection` = `①… → MmCreateSpecialImageSection +0xcf → MiCreateSection +0x120 → MiFinishCreateSection`. 값을 정하는 함수는 근거 칸.

| 칸 | 무엇을 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|
| `+0x28` `u1` | 컨트롤 영역 주소 (원격 파일이면 파일 객체 \| 1/2) | nt | `§섹션 → MiFinishCreateSection +0x2a`, 복사 `+0x15b` (원격 `+0x266~+0x27b`) | `[정적]` 정확 · 사진 `…983992b0` |
| `+0x30` `SizeOfSection` | `SizeOfSegment` 읽은 값 (최대 크기 인자가 있으면 그 값) | nt | `§섹션 → MiCreateImageOrDataSection → MiCreateNewSection +0x21d/+0x228` (공유: `MiShareExistingControlArea +0x104/+0xac`) → `MiFinishCreateSection +0x172` 복사 | `[정적]` 정확 · 사진 `0x8c000` |
| `+0x38` `u` | 컨트롤 영역 `u` 복사 − `FilePointerNull` + `UserReference` (+조건부 `Based`·`NoChange`) | nt | `…→ MiFinishCreateSection +0x31~+0x43` · `+0x1b3~+0x1ba` · 복사 `+0x172` | `[정적]` 정확 · 사진 `0x000100a0` |
| `+0x3c` `InitialPageProtection` | `0x10` (페이지 보호 인자) | nt | `…→ MiInitializeCreateSectionPacket +0xd7` → `MiFinishCreateSection +0x172` | `[정적]` 정확 · 사진 `0x10` |
| `+0x3c` `NoValidationNeeded` | 1 (서명 수준 ≤ 1, 이미지) | nt | `…→ MiFinishCreateSection +0x243~+0x247`, 복사 `+0x172` | `[정적]` 정확 · 사진 1 |
| `+0x00` `SectionNode`·`+0x20` `EndingVpn` | 0 — `SEC_BASED` 섹션만 씀 | nt | `…→ MiFinishCreateSection` 조각 `FUN_140b23aba +0xbf/+0x111` (안 탐) | `[정적]` · 사진 끝까지 0 |
| `+0x18` `StartingVpn` | 0 을 명시적으로 씀 | nt | `…→ MiFinishCreateSection +0x17d` | `[정적]` 정확 |
| `+0x3c` `SessionId` | 0 — 세션 전역 이미지만 씀 | nt | `…→ MiFinishCreateSection +0x2db~+0x2e5` (안 탐) | `[정적]` · 사진 끝까지 0 |
| (객체 할당) | `MmSectionObjectType`, 몸통 `0x40` | nt | `…→ MiFinishCreateSection +0x143` `ObCreateObjectEx` (공용) | `[정적]` |

### 3.21 `_CONTROL_AREA` (0x80)

| 칸 | 무엇을 | 쓴 주인 | 경로 (끝) | 근거 |
|---|---|---|---|---|
| `+0x00` `Segment` | 세그먼트 주소 | nt | `§새로 → MiBuildImageControlArea +0x1f8` | `[정적]` 정확 · 사진 K1 이전 |
| `+0x08` `ListHead`=`AweContext` | 빈 목록 (자기 자신). 뒤에 VAD 가 이어짐 (범위 밖 매핑 길) | nt | `§새로 → MiBuildImageControlArea +0x1f4~+0x1ff` | `[정적]` 정확 (초기값) · 사진 항목 1개 |
| `+0x18` `NumberOfSectionReferences` | 1 (새 길) / +1 (공유 길) | nt | `§새로 → MiBuildImageControlArea +0x20b` / `§섹션 → MiCreateImageOrDataSection → MiReferenceControlArea +0x107` | `[정적]` 정확 · 사진 1 |
| `+0x20` `NumberOfPfnReferences` | 0 으로 시작, 뒤에 페이지 폴트 쪽이 올림 (3 → `0x2d`) | 범위 밖 길 | 쓰는 줄 못 찾음 `[추측]` | 사진 3 → `0x2d` · D5 |
| `+0x28` `NumberOfMappedViews` | 0 으로 시작, 매핑이 +1 | 범위 밖 길 | `MmInitializeProcessAddressSpace +0x1c9 → MiMapProcessExecutable +0xb3 → MmMapViewOfSectionEx` (이름만) | `[추측]` · 사진 1 |
| `+0x30` `NumberOfUserReferences` (아는 칸 정정) | 1 (새 길) / +1 (공유 길) | nt | `§새로 → MiBuildImageControlArea +0x214` / `§섹션 → MiCreateImageOrDataSection +0x19e → MiReferenceControlArea +0x11d` | `[정적]` 정확 · **기존 "MiCreateImageOrDataSection +0x11d" 는 오기** |
| `+0x38` `u` (짐작 → 정확) | `Image`·`File`·`BeingCreated`·선호 노드, 나중에 `BeingCreated` 끔 | nt | `§새로 → MiBuildImageControlArea +0x9e·+0x222~+0x238`; `MiCreateNewSection +0x162 → MiSectionCreated +0xa6` | `[정적]` 정확 · 사진 `0xa0` |
| `+0x3c` `u1` | `PartitionId`=0 을 씀 | nt | `§새로 → MiCreateImageFileMap +0x31e` | `[정적]` · 사진 끝까지 0 |
| `+0x40` `FilePointer` | 파일 객체 주소 (자리표시용 컨트롤 영역에서 복사; `RefCnt` 0) | nt | `…→ MiCreateNewSection +0x162 → MiSectionCreated +0x6f~+0x7a` (자리표시 값은 `MiCreateImageOrDataSection +0x120`) | `[정적]` 정확 · 사진 `…dc5d` |
| `+0x40` `FilePointer` 아래 4비트 | `_EX_FAST_REF` 참조 수 | 범위 밖 길 | `MiReferenceControlAreaFile`/`MiDereferenceControlAreaFile` 후보 | `[추측]` · 사진 `P0~P1` · D5 |
| `+0x48` `ControlAreaLock` | 스핀락 (잡았다 놓음 → 0) | nt | `MiSectionCreated +0x31`/`+0xc6` | `[정적]` · 사진 끝까지 0 |
| `+0x4c` `ModifiedWriteCount` | 0 (쓴 줄 없음, 풀 0) | — | — | `[정적]` 부재 · 사진 끝까지 0 |
| `+0x50` `WaitList` | 자리표시용 컨트롤 영역 값 복사 (대기자 없으면 0) | nt | `MiSectionCreated +0x11b` (대기자 채우기 `MiReferenceControlArea +0x2bf`) | `[정적]` · 사진 끝까지 0 |
| `+0x58` `u2` | `-1` 초기값, `ImageInfoRef`, 재배치 비트·크기, `ImageBaseOkToReuse`, `ImageActive` | nt | `§새로 → MiBuildImageControlArea +0x218·+0xb4`; `MiCreateNewSection +0x3d5 → MiRelocateImage +0x2e6 → MiSelectImageBase +0x66·+0x1d4·+0x1d8·+0x209`; `MiRelocateImage` 조각 `+0x1b127c` | `[정적]` 줄 · 갈래 `[공백]` (D4) · 사진 `ffffffff`/`0x00c00009` |
| `+0x68` `FileObjectLock` | 0 을 씀 | nt | `§새로 → MiBuildImageControlArea +0x227` | `[정적]` · 사진 끝까지 0 |
| `+0x70` `LockedPages` | 1 | nt | `§새로 → MiBuildImageControlArea +0x207` | `[정적]` 정확 · 사진 1 |
| `+0x78` `u3` | `ImageCrossPartitionCharge` (구역 쪽마다 +1) | nt | `§새로 → MiBuildImageControlArea +0x362 → MiParseImageSectionHeaders +0x258` | `[정적]` 정확 · 사진 `0x89` |

### 3.22 `_SEGMENT` (0x48)

| 칸 | 무엇을 | 쓴 주인 | 경로 (끝) | 근거 |
|---|---|---|---|---|
| `+0x00` `ControlArea` | 컨트롤 영역 주소 | nt | `§새로 → MiBuildImageControlArea +0x138` | `[정적]` 정확 |
| `+0x08` `TotalNumberOfPtes` | `SizeOfImage` 쪽 수 (`0x8c`) | nt | `…→ MiBuildImageControlArea +0x13b` (EBP `+0x2c~+0x44`) | `[정적]` 정확 · 값 헤더에서 계산 |
| `+0x0c` `SegmentFlags` | `DefaultProtectionMask`=7, `ContainsDebug`; 조건부 `Binary32`·`ForceCollision` | nt | `…→ MiBuildImageControlArea +0x362 → MiParseImageSectionHeaders +0x3c~+0x43`; `MiBuildImageControlArea +0x246~+0x256`; (안 탐) `MiCreateImageFileMap +0x415`, `MiParseComAndCetHeaders +0x3aa` | `[정적]` 정확 · 사진 `0x270000` |
| `+0x10` `NumberOfCommittedPages` | 조건부 +1 (이 실행 0) | nt | `…→ MiParseImageSectionHeaders +0x240` (안 탄 갈래) | `[정적]` · 사진 끝까지 0 |
| `+0x18` `SizeOfSegment` | `TotalNumberOfPtes<<12` | nt | `…→ MiBuildImageControlArea +0x145~+0x149` | `[정적]` 정확 |
| `+0x20` `ExtendInfo`=`BasedAddress` | 헤더 `ImageBase` → 새 기본 주소로 덮음 | nt | `…→ MiBuildImageControlArea +0x25e~+0x261`; `MiCreateNewSection +0x3d5 → MiRelocateImage` 조각 `+0x55` (= `+0x1b1269`) | `[정적]` 정확 · 사진 `0x7ff61a370000` |
| `+0x28` `SegmentLock` | 0 을 씀 | nt | `…→ MiBuildImageControlArea +0x130` | `[정적]` · 사진 끝까지 0 |
| `+0x30` `u1` | `ImageCommitment` (+1 씩; 플랫이면 쪽 수) | nt | `…→ MiParseImageSectionHeaders +0x249` (플랫 `MiBuildImageControlArea +0x428`) | `[정적]` 정확 · 사진 4 |
| `+0x40` `PrototypePte` | 프로토 PTE 풀 주소 | nt | `…→ MiBuildImageControlArea +0x134` (풀 `+0xdb`) | `[정적]` 정확 |

### 3.23 `_SECTION_IMAGE_INFORMATION` (0x40)

남은 10칸: `SubSystemVersion`·`OperatingSystemVersion` (겹침, 위 질문 3 표 줄), `ImageFlags` (`ImageDynamicallyRelocated` 외 비트 전부 조건부: 질문 3), `ZeroBits` (쓰는 줄 없음, 0), 비트 6개 중 이 실행에서 켜진 것 없음. 줄 → 오프셋 미상 채우기는 질문 3 두 번째 표.

### 3.24 `_FILE_OBJECT` (0xd8)

경로 `§열기` = `NtCreateUserProcess +0x6c6 → IopCreateFile → ObOpenObjectByNameEx → ObpLookupObjectName → IopParseDevice`.

| 칸 | 무엇을 | 쓴 주인 | 경로 (끝) | 근거 |
|---|---|---|---|---|
| `+0x00` `Type`·`+0x02` `Size` | 5, `0xd8` | nt | `§열기 +0xe19 → IopAllocRealFileObject +0x375` | `[정적]` 정확 · 사진 5·216 |
| `+0x08` `DeviceObject` | 장치 | nt | `… → IopAllocRealFileObject +0x387` | `[정적]` 정확 |
| `+0x10` `Vpb` | 볼륨 VPB | 다른 드라이버 (ntfs.sys) | `…→ IofCallDriver +0xf5a` 안 | `[정적]` 쓰는 줄 부재 · `[추측]` 주체 |
| `+0x18` `FsContext` · `+0x20` `FsContext2` · `+0x28` `SectionObjectPointer` | FS 가 채움 | 다른 드라이버 (ntfs.sys) | 위와 같음 | 같음 |
| `+0x4a` `ReadAccess` · `+0x4d` `SharedRead` · `+0x4f` `SharedDelete` | 1·1·1 | nt (부르는 쪽 ntfs.sys) | `IoSetShareAccess +0xf → IoSetLinkShareAccess +0x16/+0x5b/+0x63` | `[정적]` · 부르는 쪽 `[추측]` |
| `+0x50` `Flags` | 아래 비트 표 | nt + 다른 드라이버 | `IopAllocRealFileObject +0x2ee`, `IopCreateFile` 조각 `+0xbb`, FS | `[정적]` 일부 · 사진 `0xc0042` → `0xc4442` |
| `+0x58` `FileName` | 남은 이름 복사 | nt | `§열기 +0xea8 → +0xec6 RtlCopyUnicodeString` | `[정적]` 정확 |
| `+0x80` `Lock` | 동기 이벤트 | nt | `… → IopAllocRealFileObject +0x319` | `[정적]` 정확 |
| `+0x98` `Event` | 알림 이벤트 | nt | `§열기 +0xf1a` | `[정적]` 정확 |
| `+0xc0` `IrpList` · `+0xb8` `IrpListLock` | 빈 목록 · 0 | nt | `… → IopAllocRealFileObject +0x39d/+0x3a1/+0x38b` | `[정적]` 정확 |
| `+0x40` `RelatedFileObject` · `+0x68` `CurrentByteOffset` · `+0x70` `Waiters` | 0 을 씀 | nt | `… → IopAllocRealFileObject +0x37f / +0x323 / +0x320` | `[정적]` |
| `+0x4b` `WriteAccess` · `+0x4c` `DeleteAccess` · `+0x4e` `SharedWrite` | 0 을 씀 | nt (부르는 쪽 ntfs.sys) | `IoSetLinkShareAccess +0x23/+0x2a/+0x67` | `[정적]` |
| `+0x30`·`+0x38`·`+0x48`·`+0x49`·`+0x74`·`+0x78`·`+0xb0`·`+0xd0` | 0 (`memset` 만) | — | `… → IopAllocRealFileObject +0x21e` | `[정적]` 부재 |

## 동적분석으로 확인해야 할 것 (dast 요청 필요)

사진 덤프로 답할 수 있는지는 오른쪽 열. 멈춤점은 `nt+rva` (바이너리 `_index.tsv` 기준), test.exe 일 때만 서게 `_FILE_OBJECT.FileName` 조건은 dast-mentor 몫.

| # | 질문 | 추천 멈춤점 / 찍을 것 | 사진으로? |
|---|---|---|---|
| D1 | 공유 길인가 새 길인가 (sast/26 D1 = Q61 와 같은 질문) | `nt+0x96f4d0` (`MiCreateNewSection` 입구) / `nt+0x96e6cc` (`MiShareExistingControlArea` 입구) 중 서는 쪽. 또는 test.exe 를 **재부팅 없이 두 번** 돌려 두 K1 의 `_SECTION.u1` (컨트롤 영역 주소) 비교 — 같으면 공유 | 두 번 찍은 사진 비교로 가능 |
| D2 | `_SECTION` 사본이 맞게 모이나 | `nt+0x96dd64` (`MiFinishCreateSection +0x148`, ObCreateObjectEx ret) 직전 `dq @rdi+0x50 L8` (패킷 `+0x50~+0x8f`), 직후 `dq @rcx L8` 에서 `+0x18` 이 0 인지 | 아니오 (K1 값은 이미 최종) |
| D3 | `u1` 의 `RemoteImageFileObject` 갈래 / `SEC_BASED` / 세션 갈래 안 탄 것 확인 | `nt+0x96dc1c` 입구 `r rdi`, `dd @rdi+0x10 L1` (패킷 할당 속성), `dd @rdi L1` (패킷 플래그) | 아니오 |
| D4 | `CA.u2` (`+0x58`/`+0x5c`) 어느 갈래로 `ffffffff`/`0x00c00009` 가 되나 | `nt+0x972de0` (`MiSelectImageBase` 입구) 와 `+0x20d` 근처 (`nt+0x972fed`), 끝에서 `dq @r15+0x58 L1`, `r ebx`, `r ebp` | 아니오 |
| D5 | `NumberOfPfnReferences` 3 → `0x2d`, `FilePointer` 아래 비트가 누구 때문에 바뀌나 | K1 에서 `ba w8 <CA+0x20>`, `ba w8 <CA+0x40>`, 서면 `k` 한 줄 (모든 프로세스에 걸리니 조건: CA 주소 고정) | 아니오 (구간만 사진 있음) |
| D6 | `FILE_OBJECT.Flags` 비트 `0x4400` 누가 켜나, 닫을 때가 맞나 | `nt+0x842838` (`PspDeleteCreateProcessContext +0x134 ObCloseHandle`) 입구 `r rcx`, `nt+0x86c660` (`IopCloseFile` 입구), `ba w4 <FO+0x50>` 로 서면 `k` 와 `ln @rip` | 아니오 (구간 `K3~P0` 만) |
| D7 | `FsContext`·`FsContext2`·`SectionObjectPointer`·`Vpb`·`Flags` `0x40/0x80000` 쓴 모듈 | `nt+0x8bbb8f` (`IopParseDevice +0xf5f`, IofCallDriver 직후) 에서 파일 객체 주소를 잡고 `ba w8 <FO+0x10>` / `+0x18` / `+0x20` / `+0x28` — 서면 `lm a @rip` 가 `ntfs` 인지 | 아니오 |
| D8 | exe 열기가 `IopParseFile` 안 타고 `+0xe19` 로 가는가 | `nt+0xa11580` (`IopParseFile`) 안 서는지, `nt+0x8b5e40` (`IopAllocRealFileObject`) 입구 `k` 에서 호출 위치가 `IopParseDevice +0xe1e` 인지 | 아니오 |
| D9 | `ReadAccess/Shared*` 를 부르는 쪽이 ntfs 인가 | `nt+0x9b5660` (`IoSetShareAccess`) / `nt+0x9b57c0` (`IoCheckShareAccess`) 서면 `k` | 아니오 |
| D10 | `NoValidationNeeded` 의 서명 수준 값 | `nt+0x96dc1c` 입구 `db @rdi+0x18 L1` (패킷 서명 수준), `dd @rdi L1` bit18 | 아니오 |

D1 은 sast/26 과 같으니 Q61 에 합쳐도 된다. D5·D6·D7 은 `ba w` (데이터 멈춤점) 라서 하나의 실행에서 같은 파일 객체·컨트롤 영역 주소로 같이 볼 수 있다 (실행마다 주소는 바뀜).

## 나머지 Q78 로 넘길 것

- **Q78c (프로세스 객체)**: `EPROCESS.SectionObject` (+0x2a8, `ObReferenceObjectByHandle` 로 받은 `_SECTION`, `NtCreateUserProcess +0x96d`) 쓰기, `ImageFilePointer` (+0x330) 를 `PspAllocateProcess` 안 누가 쓰나, `SectionBaseAddress` — `PspAllocateProcess` 아래. 이번 요청서의 `NtCreateUserProcess +0x78c ObReferenceObjectByHandle` 가 파일 핸들 → 파일 객체 (`IoFileObjectType`) 를 받아 `[RBP+0x258]` 에 둠.
- **Q78c/Q79 (매핑)**: `MiMapProcessExecutable → MmMapViewOfSectionEx → MiMapViewOfImageSection` 이 `NumberOfMappedViews`·`ListHead` 연결·`NumberOfUserReferences` 를 올린다 `[추측]` (이름만).
- **칸 표가 아직 없는 짐 (PDB 정의 있음, run43 방식으로 칸 표 가능)**: `_SECTION_OBJECT_POINTERS` (0x18, FS 메모리, `ImageSectionObject` 는 nt 가 씀), `_MI_IMAGE_ADDITIONAL_INFO` (0x48, `CA.u2.ImageInfoRef` 가 가리키는 컨트롤 영역 할당 꼬리), `_MI_EXTRA_IMAGE_INFORMATION` (0x10, 이미지 정보 `+0x40`), `_SUBSECTION`·프로토 PTE 표 안 내용, `_OPEN_PACKET` (0xe0, 짐이 아니라 임시이지만 PDB 있음). baggage 7.6 후보로 기록만 제안.
- **baggage 정정 제안 (오케스트레이터 몫)**: ① 3.21 `NumberOfUserReferences` 경로를 `MiReferenceControlArea +0x11d` 로 (그리고 새 길 `MiBuildImageControlArea +0x214` 추가). ② 3.21 `u` 줄 [추측] → `[정적]` 정확, 쓰는 줄 위 표. ③ 3.23 "오프셋 미상" 14줄을 위 표 오프셋으로 채움, `ImageContainsCode` 에 `MiParseImageSectionHeaders +0x1bf` 추가. ④ 3.23 `SubSystemVersion`·`OperatingSystemVersion` 은 겹침이라 따로 안 세도 됨 (칸 수 26 그대로 두고 "아는 칸" 만 늘리기).
- **`PspAllocateProcess` 쪽**: `EPROCESS.ImageFilePointer` 에 넣는 파일 객체는 질문 4의 `[RBP+0x258]` (참조 `NtCreateUserProcess +0x791`). 이 프로세스 문맥 정리 (`PspDeleteCreateProcessContext +0x13e ObfDereferenceObject`) 가 그 참조를 놓는다.

## 읽은 파일 / 만든 파일

- `.asm` 전체를 줄 단위로 읽은 것: `MiFinishCreateSection`, `MiBuildImageControlArea`, `MiSectionCreated`, `MiShareExistingControlArea`, `MiReferenceControlArea` (앞 `+0x2d0` 까지), `MiCreateImageOrDataSection`, `MiCreateNewSection` (앞 `+0x240`), `MiInitializeCreateSectionPacket` (칸 쓰는 줄 위주), `IopAllocRealFileObject` (앞 `+0x550`), `PspDeleteCreateProcessContext`, `IoSetLinkShareAccess`, `IopParseFile`. 쓰는 줄만 `grep` 으로 골라 읽은 것: `MiCreateImageFileMap`, `MiParseImageSectionHeaders`, `MiParseComAndCetHeaders`, `MiSelectImageBase`, `MiRelocateImage` (조각 포함), `IopParseDevice`, `IopCreateFile`, `IopCloseFile`, `ObpLookupObjectName` (`+0xb60~+0xca0`), `NtCreateUserProcess` (`+0x500~+0x7a0`, `+0x1110~+0x1210`). 호출 표만 본 것: `ObOpenObjectByNameEx`, `MiParseImageSectionHeaders` 본문 일부. 안 읽은 것: `MiInitializeNewImageSectionProtos`·`MiValidateExistingImage`·`MiCallCreateSectionFilters`·`MiMakeImageReadOnly`·`MiFinalizeImageHeaderPage`·`MiUpdateImageSystemWideBitmaps` 본문 (이름만; 뽑아 둠).
- 만든 것: `ms/decomp/ntoskrnl/<35개>.c`, `ms/disassemble/ntoskrnl/<35개>.asm`, `_calls.tsv` 덧붙임, `ms/disassemble/ntoskrnl/_refs/{MmSectionObjectType,IopParseDevice,IopParseFile,IoSetLinkShareAccess,IoCheckLinkShareAccess}.tsv`. 로그 `ms/ghidra/ntoskrnl.export.log`, `ms/ghidra/ntoskrnl.refs.log`.
- 이 결과서에는 디컴파일 코드를 붙이지 않았다. 명령 한두 개와 오프셋은 줄 근거용이다.

## 판정

**분석성공.** 요청서 질문 1~6 모두 정적으로 답했고 모르는 칸 74개 중 **여전히 모름 0**.
- 정적으로 못 닫은 것 (동적으로 넘김): ① test.exe 가 공유 길인가 새 길인가 (D1), ② `CA.u2` 가 어느 갈래로 `ffffffff`/`0x00c00009` 가 됐나 (D4), ③ `NumberOfPfnReferences` 3→`0x2d` 와 `FilePointer` 아래 비트를 누가 바꾸나 (D5), ④ `FILE_OBJECT.Flags` `0x4400` 과 `FsContext*`·`SectionObjectPointer`·`Vpb`·`Flags` `0x40/0x80000` 를 쓴 주체가 정말 ntfs.sys 인가 (D6·D7; nt 에 쓰는 줄이 없다는 것은 정적으로 확인), ⑤ 파일 이름 문자열(`[RBP+0x290]`)의 출처.

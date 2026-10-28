# 2026-10-28 run47 — exe 섹션 묶음 (Q78b): 다섯 구조체 모르는 칸 74 → **여전히 모름 0**. `_SECTION` 객체는 사슬 끝 형제 `MiFinishCreateSection` (`MiCreateSection +0x125`) 이, exe 파일 객체는 `IopParseDevice +0xe1e IopAllocRealFileObject` 가 만든다. 파일 시스템 칸 4개는 nt 에 쓰는 줄이 없다 (정적)

## 구조체별 요약 (모르는 칸 74 → ?)

- 결과서 맨 앞 표 그대로. 분류 뜻:
  - **아는 칸 (nt)** = nt 에서 그 칸을 쓰는 줄을 찾음
  - **0 그대로** = 이 실행에서 값 0. 이유는 셋 중 하나: 쓰는 줄이 조건부라 안 탐 / 코드가 0 을 씀 / 할당 때 0 으로 채운 그대로
  - **다른 드라이버** = nt 에 쓰는 줄이 없음 `[정적]`, 쓰는 쪽은 파일 시스템 (ntfs.sys) `[추측]`
  - **nt 함수·FS 가 부름** = 쓰는 코드는 nt (`IoSetLinkShareAccess`) 인데 부르는 쪽이 파일 시스템 `[추측]`
  - **범위 밖 길** = 쓰는 길이 매핑·페이지 폴트 쪽 (이름만)

| 구조체 | 모르는 칸 | 아는 칸 (nt) | 0 그대로 | 다른 드라이버 | nt 함수·FS 가 부름 | 범위 밖 길 | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `_SECTION` | 9 | 5 | 4 | 0 | 0 | 0 | 0 |
| `_CONTROL_AREA` | 15 | 8 | 5 | 0 | 0 | 2 | 0 |
| `_SEGMENT` | 10 | 8 | 2 | 0 | 0 | 0 | 0 |
| `_SECTION_IMAGE_INFORMATION` | 10 | 3 (겹침 2 + `ImageFlags`) | 7 | 0 | 0 | 0 | 0 |
| `_FILE_OBJECT` | 30 | 8 | 15 | 4 | 3 | 0 | 0 |
| **합** | **74** | **32** | **33** | **4** | **3** | **2** | **0** |

- "0 그대로" 33 을 짐 지도 (`baggage.md` 3장) 에 옮길 때 다시 가른 것:
  - nt 에 쓰는 줄이 있음 (조건부 안 탐 또는 0 을 씀) **22** → 짐 지도 "nt 가 씀" 으로 셈
    - `_SECTION` 4 (`SectionNode`·`EndingVpn`·`SessionId` 조건부, `StartingVpn` 0 을 씀)
    - `_CONTROL_AREA` 4 (`u1` `PartitionId`=0, `ControlAreaLock` 잡았다 놓음, `WaitList` 복사, `FileObjectLock` 0 을 씀)
    - `_SEGMENT` 2 (`NumberOfCommittedPages` 조건부, `SegmentLock` 0 을 씀)
    - `_SECTION_IMAGE_INFORMATION` 5 (`ImageFlags` 비트 5개 조건부)
    - `_FILE_OBJECT` 7 (nt 가 0 을 씀 4, `IoSetLinkShareAccess` 가 0 을 씀 3)
  - 쓰는 줄 없음 — 할당 때 0 으로 채운 그대로 **11** → 짐 지도 "아무도 안 씀"
    - `_CONTROL_AREA.ModifiedWriteCount`, `_SECTION_IMAGE_INFORMATION.ZeroBits`·`ImageFlags.Reserved` (`Reserved` 는 "못 찾음" 이라 `[추측]`), `_FILE_OBJECT` 8칸 (`memset` 만)
- "범위 밖 길" 2 (`NumberOfPfnReferences`·`NumberOfMappedViews`) 는 짐 지도에 nt **짐작** 으로 셈 (처음 0 은 풀 0 `[정적]`, 올리는 쪽은 이름만 `[추측]`).

짐 지도 2장 숫자 변화 (커널 구조체 다섯):

| 구조체 | nt 가 씀 | 다른 DLL (드라이버) | 아무도 안 씀 | 아직 모름 |
|---|---|---|---|---|
| `_SECTION` | 0 → **9** | 0 | 0 | 9 → **0** |
| `_CONTROL_AREA` | 2 → **16** | 0 | 1 | 15 → **0** |
| `_SEGMENT` | 1 → **11** | 0 | 0 | 10 → **0** |
| `_SECTION_IMAGE_INFORMATION` | 16 → **24** | 0 | 2 | 10 → **0** |
| `_FILE_OBJECT` | 0 → **18** | 0 → **5** | 8 | 30 → **0** |

- `_FILE_OBJECT.Flags` 는 nt 와 다른 드라이버 **양쪽에 셌다** (비트 `0x2`·`0x4`·`0x40000` 은 nt 줄, `0x40`·`0x80000` 은 nt 에 켜는 줄이 없음). 짐 지도 규칙 "한 칸을 둘이 쓰면 양쪽에 센다" 그대로라 다른 드라이버 열은 결과서 4 가 아니라 5 다.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 03_sprint 다섯째 바퀴 (짐 지도 바퀴, plan **Q78b** — Q78 나머지를 시간 순서로 나눈 첫 조각, 사용자 결정 2026-10-28).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음). 이미 analyze 된 프로젝트에서 뽑기만 했다 (다시 analyze 안 함).
- 버전 대조 (결과서): `NtCreateUserProcess` ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` ret `+0x807` → `PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다. run44 K1 사진의 `_SECTION_IMAGE_INFORMATION` 값이 `ms/bin/test.exe` 헤더와 칸마다 맞는다 (`TransferAddress` `7ff61a371d40`, `ImageFileSize` `0x86a00`, `SizeOfImage` `0x8c000` 등).
- 이 문서를 쓰며 다시 맞춘 것: 아래 호출 지점 표의 call/ret 을 `ms/disassemble/ntoskrnl/_calls.tsv` 로, 정정 줄 (`MiReferenceControlArea +0x107 INC [RBX+0x18]`·`+0x11d INC [RBX+0x30]`·`+0x178 MOV [R15],RSI`) 과 `MiFinishCreateSection` 복사 줄 (`+0x15b ~ +0x179`) 을 `.asm` 으로 한 번 더 봤다. `_CONTROL_AREA`·`_SEGMENT` 칸 오프셋은 `pdb_struct.py` 로 봤다.

## 목적

- 짐 지도 Q78 의 둘째 조각. 시간 순서로 맨 앞인 **exe 섹션 묶음** 다섯 구조체 (baggage 3.20 ~ 3.24) 의 모르는 칸 74 를 닫는다.
  - run44 사진은 이 칸들이 거의 다 **`K1 이전`** (= `PspAllocateThread` 전) 에 값이 있다는 것만 보여 줬다. 누가 썼는지는 몰랐다.
- 요청서 질문 6개: (1) `_SECTION` 만들기, (2) 컨트롤 영역·세그먼트 칸과 두 길 (새로 만들기 / 공유), (3) 이미지 정보 남은 10칸, (4) exe 파일 객체 30칸과 주인 셋 (nt / 파일 시스템 / 0), (5) 칸별 경로, (6) 입력·갈림길.
- 범위 밖: 프로세스 객체 (`_EPROCESS` 등, Q78c), 매핑 (`MmMapViewOfSection`·VAD), 서브섹션·프로토 PTE 표 안 내용, 파일 시스템 내부 (ntfs.sys 는 안 뽑음).

## 방법

- 정적: `_workspace/sast/41_section-chain.md` — ntoskrnl. 이미 있던 섹션 사슬 `.asm` 13개 (sast/26) 는 읽기만, 새로 depth 0 **35개** (섹션 쪽 15 · 파일 객체 쪽 20). 넓힌 이유: `_SECTION` 을 만드는 함수 (`MiFinishCreateSection`) 와 파일 객체를 만드는 함수 (`IopAllocRealFileObject`) 가 요청서 표에 없었다.
  - `refs` 5번 (`MmSectionObjectType`·`IopParseDevice`·`IopParseFile`·`IoSetLinkShareAccess`·`IoCheckLinkShareAccess`) → `ms/disassemble/ntoskrnl/_refs/`.
  - 구조체 칸 표: 이번에 하네스에 들어온 `pdb_struct.py`. 이름 없는 공용체 안쪽은 sast/38 의 `struct__*.tsv` 로 대신 봄.
  - 값 대조: run44 덤프 읽기 로그 `ms/logs/04_baggage-read_K1.txt` 1381 ~ 1500줄 (`K1 07` 섹션 사슬, `K1 08` 파일 객체). 새 동적 실행은 없다.
- 결과서: `_workspace/sast/41_section-chain_result.md` (**분석성공**)
- 문서: 이 run 문서, `baggage.md` (3.20 ~ 3.24, 2장·2.1·7.2·7.10), `flow.md` 2.1 (`MiCreateSection`·`IopCreateFile` 절), 그림 1 (`MiFinishCreateSection` 상자 하나), 4장·5장.

## 결과

비유 (가구 공장):

- 설계도 (exe 파일) 를 창고에서 꺼내 올 때 **대출 카드** (파일 객체) 를 한 장 만든다. 카드의 이름·종류 칸은 공장 (nt) 이 적고, "책이 서가 어디 있나" 칸은 **창고지기** (파일 시스템 ntfs.sys) 가 적는다.
- 설계도를 읽어 **배치표 원본** (컨트롤 영역·세그먼트·이미지 정보) 을 서랍에 넣는다. 서랍 칸은 거의 다 `MiBuildImageControlArea` 한 사람이 적는다.
- 마지막에 손님에게 줄 **배치표 사본 봉투** (`_SECTION` 객체) 를 만든다. 봉투 내용은 앞에서 메모지 (스택 패킷) 에 모아 둔 걸 한 번에 옮겨 적는다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0x6c6` | `+0x6cb` | `nt!IopCreateFile` (exe 열기) | [정적] |
| `nt!NtCreateUserProcess` | `+0x73b` | `+0x740` | `nt!IoCreateFileEx` — `+0x6cf JNS` 가 첫 번째 성공이면 건너뜀. **실패 때만 타는 재시도** | [정적] |
| `nt!NtCreateUserProcess` | `+0x78c` | `+0x791` | `nt!ObReferenceObjectByHandle` (파일 핸들 → 파일 객체 참조, `[RBP+0x258]`) | [정적] |
| `nt!NtCreateUserProcess` | `+0x937` | `+0x93c` | `nt!MmCreateSpecialImageSection` | [정적] |
| `nt!NtCreateUserProcess` | `+0x11d0` | `+0x11d5` | `nt!PspDeleteCreateProcessContext` (exe 핸들 닫기, `PspInsertThread` 뒤) | [정적] |
| `nt!IopCreateFile` | 조각 `FUN_1408c7e1f +0x6a` | `+0x6f` | `nt!ObOpenObjectByNameEx` (조각 자리 = `IopCreateFile +0x4d1`, WinDbg 표시는 안 봄) | [정적] |
| `nt!ObOpenObjectByNameEx` | `+0x21f` | `+0x224` | `nt!ObpLookupObjectName` | [정적] |
| `nt!ObOpenObjectByNameEx` | `+0x405` | `+0x40a` | `nt!ObpCreateHandle` (핸들 만들기) | [정적] |
| `nt!ObpLookupObjectName` | `+0xbc8` · `+0xc90` | `+0xbcd` · `+0xc95` | `nt!IopParseDevice` (장치 형식의 Parse 가 `IopParseDevice` 이면 직접 호출, `+0xb75 LEA` 로 비교) | [정적] |
| `nt!IopParseFile` | `+0xc2` | `+0xc7` | `nt!IopParseDevice` — 파일 객체 밑 상대 열기 징검다리. **절대 경로 exe 는 안 탐** | [정적] |
| `nt!IopParseDevice` | `+0xcb7` | `+0xcbc` | `nt!IopAllocateIrpExReturn` (`IRP_MJ_CREATE` IRP) | [정적] |
| `nt!IopParseDevice` | `+0xe19` | `+0xe1e` | **`nt!IopAllocRealFileObject`** (파일 객체 할당·칸 채우기) | [정적] |
| `nt!IopParseDevice` | `+0x10d7` | `+0x10dc` | `nt!IopAllocRealFileObject` — `OpenPacket.QueryOnly` 갈래, exe 는 안 탐 (`IopCreateFile +0x29e` 가 0 으로 둠) | [정적] |
| `nt!IopParseDevice` | `+0xea8` · `+0xec6` | `+0xead` · `+0xecb` | `ExAllocatePool2` (태그 `IoNm`) · `RtlCopyUnicodeString` (`FileName`) | [정적] |
| `nt!IopParseDevice` | `+0xf1a` | `+0xf1f` | `KeInitializeEvent` (`FO+0x98 Event`) | [정적] |
| `nt!IopParseDevice` | `+0xf2f` · `+0xf5a` | `+0xf34` · `+0xf5f` | `IopQueueThreadIrp` · **`IofCallDriver`** (→ 볼륨의 파일 시스템. 여기부터 ntfs.sys) | [정적] |
| `nt!IopAllocRealFileObject` | `+0x16a` | `+0x16f` | `nt!ObpAllocateObject` (크기 `0xd8`, `IoFileObjectType`). 룩어사이드에서 받으면 안 부름 | [정적] |
| `nt!IopAllocRealFileObject` | `+0x21e` · `+0x319` | `+0x223` · `+0x31e` | `memset(…,0xd8)` · `KeInitializeEvent` (`FO+0x80 Lock`, 동기 파일일 때만) | [정적] |
| `nt!MmCreateSpecialImageSection` | `+0xcf` | `+0xd4` | `nt!MiCreateSection` | [정적] |
| `nt!MmCreateSpecialImageSection` | `+0x101` · `+0x117` | `+0x106` · `+0x11c` | `MiReferenceControlAreaFile` · `MiDereferenceControlAreaFile` (`FilePointer` 아래 비트 후보, 이름만) | [정적] |
| `nt!MmCreateSpecialImageSection` | `+0x141` | `+0x146` | `nt!ObInsertObjectEx` (섹션 핸들) | [정적] |
| `nt!MiCreateSection` | `+0x58` | `+0x5d` | `memset` (패킷 0xc8바이트) | [정적] |
| `nt!MiCreateSection` | `+0xf3` | `+0xf8` | `nt!MiCreateImageOrDataSection` | [정적] |
| `nt!MiCreateSection` | `+0x120` | `+0x125` | **`nt!MiFinishCreateSection`** (`_SECTION` 객체 만들기 — 사슬이 끝난 **뒤** 형제 호출) | [정적] |
| `nt!MiFinishCreateSection` | `+0x143` | `+0x148` | `nt!ObCreateObjectEx` (`MmSectionObjectType`, 몸통 `0x40`) — 공용 부품 | [정적] |
| `nt!MiCreateImageOrDataSection` | `+0x14a` | `+0x14f` | `MiCallCreateSectionFilters` (이름만) | [정적] |
| `nt!MiCreateImageOrDataSection` | `+0x19e` | `+0x1a3` | `nt!MiReferenceControlArea` (갈림길 — 공유 길이면 여기서 카운터 +1) | [정적] |
| `nt!MiCreateImageOrDataSection` | `+0x1d0` | `+0x1d5` | `nt!MiShareExistingControlArea` (공유 길) | [정적] |
| `nt!MiCreateImageOrDataSection` | `+0x380` | `+0x385` | `nt!MiCreateNewSection` (새 길) | [정적] |
| `nt!MiShareExistingControlArea` | `+0xf4` | `+0xf9` | `MiValidateExistingImage` (이름만) | [정적] |
| `nt!MiCreateNewSection` | `+0x210` | `+0x215` | `nt!MiCreateImageFileMap` | [정적] |
| `nt!MiCreateNewSection` | `+0x162` | `+0x167` | `nt!MiSectionCreated` (새 길 마무리) | [정적] |
| `nt!MiCreateNewSection` | `+0x267` | `+0x26c` | `nt!MiParseComAndCetHeaders` (COM 디렉터리 있을 때만 비트를 켬) | [정적] |
| `nt!MiCreateNewSection` | `+0x3d5` | `+0x3da` | `nt!MiRelocateImage` | [정적] |
| `nt!MiRelocateImage` | `+0x2e6` | `+0x2eb` | `nt!MiSelectImageBase` (`CA.u2` 재배치 칸, 새 기본 주소) | [정적] |
| `nt!MiCreateImageFileMap` | `+0x91` | `+0x96` | `FsRtlGetFileSize` (파일 크기 — 입력) | [정적] |
| `nt!MiCreateImageFileMap` | `+0x2e1` | `+0x2e6` | `nt!MiBuildImageControlArea` | [정적] |
| `nt!MiBuildImageControlArea` | `+0x8d` · `+0xdb` · `+0x107` | `+0x92` · `+0xe0` · `+0x10c` | `MiAllocatePool` ×3 (태그 `MmCi` 컨트롤 영역+서브섹션, `MmSt` 프로토 PTE, `MSeg` 세그먼트 0x98) | [정적] |
| `nt!MiBuildImageControlArea` | `+0x362` | `+0x367` | `nt!MiParseImageSectionHeaders` (`u3`·`ImageCommitment`·`DefaultProtectionMask` 등) | [정적] |
| `nt!MiSectionCreated` | `+0xfe` | `+0x103` | `MiInitializeNewImageSectionProtos` (프로토 PTE 표 안 — 범위 밖, 이름만) | [정적] |
| `nt!PspDeleteCreateProcessContext` | `+0x134` · `+0x13e` | `+0x139` · `+0x143` | `ObCloseHandle` (exe 파일 핸들) · `ObfDereferenceObject` (파일 객체 참조) | [정적] |
| `nt!IopCloseFile` | `+0x138` | `+0x13d` | `IofCallDriver` (`IRP_MJ_CLEANUP`) — `Flags` `0x4400` 후보 구간 | [정적] |
| `nt!IoSetShareAccess` · `IoSetShareAccessEx` | `+0xf` · `+0x1a` | `+0x14` · `+0x1f` | `nt!IoSetLinkShareAccess` (`ReadAccess`·`Shared*` 쓰기). nt 안에서 `IopParseDevice` 는 이걸 안 부름 (`refs`) | [정적] |

### 호출 관계 (대표 지점)

```
NtCreateUserProcess
 ├─ (1) +0x6cb  IopCreateFile(접근 0x100020, 공유 5, FILE_OPEN, 옵션 0x60)         [정적]  exe 파일 객체
 │       │  +0x21b ~ +0x2ea  _OPEN_PACKET (0xe0) 채우기 (RelatedFileObject 0, QueryOnly 0)
 │       ├─ 조각 +0x6f  ObOpenObjectByNameEx
 │       │       ├─ +0x224  ObpLookupObjectName       \??\… → 심볼릭 링크 → 장치
 │       │       │       └─ +0xbcd (또는 +0xc95)  IopParseDevice
 │       │       │               ├─ +0xcbc  IopAllocateIrpExReturn    IRP_MJ_CREATE
 │       │       │               ├─ +0xe1e  IopAllocRealFileObject    ObpAllocateObject 0xd8 → memset → Type·Size·DeviceObject·Lock·IrpList·Flags
 │       │       │               │  +0xead · +0xecb  FileName (IoNm 버퍼 + 남은 이름 복사)
 │       │       │               ├─ +0xf1f  KeInitializeEvent         Event
 │       │       │               └─ +0xf5f  IofCallDriver             → ntfs.sys: Vpb·FsContext·FsContext2·SectionObjectPointer, IoSetShareAccess  [추측]
 │       │       └─ +0x40a  ObpCreateHandle
 │       │  조각 +0xbb  Flags |= 0x40000 (핸들 만들어짐)
 │  (+0x740 IoCreateFileEx — 위가 실패했을 때만)
 ├─ (2) +0x791  ObReferenceObjectByHandle   파일 객체 → [RBP+0x258] (뒤에 EPROCESS.ImageFilePointer, Q78c)
 ├─ (3) +0x93c  MmCreateSpecialImageSection                                          [정적]
 │       └─ +0xd4  MiCreateSection                 (+0x5d memset 패킷 0xc8)
 │               ├─ (1) +0xf8   MiCreateImageOrDataSection
 │               │       ├─ +0x1a3  MiReferenceControlArea      갈림길. 공유 길이면 +0x107 / +0x11d 카운터 +1
 │               │       ├─ +0x1d5  MiShareExistingControlArea  (공유 길) SizeOfSegment → SizeOfSection 만
 │               │       └─ +0x385  MiCreateNewSection          (새 길)
 │               │               ├─ +0x215  MiCreateImageFileMap
 │               │               │       └─ +0x2e6  MiBuildImageControlArea   풀 3개, 컨트롤 영역·세그먼트·이미지 정보 칸 거의 다
 │               │               │               └─ +0x367  MiParseImageSectionHeaders   u3·ImageCommitment·DefaultProtectionMask
 │               │               ├─ +0x167  MiSectionCreated    FilePointer·WaitList, BeingCreated 끔, ImageSectionObject 교체
 │               │               ├─ +0x26c  MiParseComAndCetHeaders   (COM 없음 → 안 켬)
 │               │               └─ +0x3da  MiRelocateImage
 │               │                       └─ +0x2eb  MiSelectImageBase   u2 재배치 칸, BasedAddress
 │               └─ (2) +0x125  MiFinishCreateSection       _SECTION 객체
 │                       ├─ +0x148  ObCreateObjectEx (MmSectionObjectType, 0x40)
 │                       │  +0x15b ~ +0x179  패킷 +0x50~+0x8f → 객체 +0x00~+0x3f 통째 복사
 │                       │  +0x17d  StartingVpn = 0 · +0x1b3 ~ +0x1ba  u.UserReference
 │       (+0x146 ObInsertObjectEx 섹션 핸들)
 └─ (4) +0x11d5 PspDeleteCreateProcessContext   +0x139 ObCloseHandle (exe 핸들) → IopCloseFile → IRP_MJ_CLEANUP   Flags 0x4400 후보 [추측]
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 두 칸 들여쓴 `+0x..` 줄은 그 함수 안 쓰는 줄.
- 번호 `(n)` 은 이 트리 안 코드 순서 (flow.md 2.1 트리 번호와 다름).

### 두 길 — 새로 만들기 / 공유 (질문 2)

- **새 길** (`MiCreateNewSection` → `MiCreateImageFileMap` → `MiBuildImageControlArea`): 풀 3개를 받아 칸 대부분을 쓴다. 풀 플래그 (`0x40`·`0x100`) 에 "초기화 안 함" 비트가 없어서 **0 으로 채워져 온다** (프로토 PTE 풀만 따로 `memset`). 안 쓰는 칸은 그대로 0. `[정적]` (플래그 비트 뜻은 `ExAllocatePool2` POOL_FLAGS 이름 기억 `[추측]`)
- **공유 길** (`MiReferenceControlArea` 가 이미 있는 컨트롤 영역을 돌려줌 → `MiShareExistingControlArea`): 카운터 둘만 올린다 (`MiReferenceControlArea +0x107` `NumberOfSectionReferences`, `+0x11d` `NumberOfUserReferences`). 다른 칸은 안 건드린다. `[정적]`
- **test.exe 가 어느 길이었나: 사진 값으로 못 가른다.** K1: `NumberOfSectionReferences` 1 · `NumberOfUserReferences` 2 · `NumberOfMappedViews` 1 · `LockedPages` 1 · `u` `0xa0`. 두 길이 같은 숫자를 만든다 (새 길 1·1 / 공유 길 0→1·0→1, 그 위에 매핑이 사용자 참조를 1 더 올려 2 — 매핑 쪽은 `[추측]`). → 동적 **D1** (= Q61).

### `_FILE_OBJECT` 주인 셋 (질문 4)

- **(가) nt** — `IopAllocRealFileObject` (`Type`·`Size` `+0x375`, `DeviceObject` `+0x387`, `Lock` `+0x319`, `IrpList` `+0x39d/+0x3a1`, `Flags` `0x2`·`0x4`, 0 을 씀 `RelatedFileObject`·`IrpListLock`·`CurrentByteOffset`·`Waiters`), `IopParseDevice` (`FileName` `+0xea8 ~ +0xec6`, `Event` `+0xf1a`), `IopCreateFile` 조각 (`Flags` `0x40000`).
- **(나) 다른 드라이버 (ntfs.sys)** — `Vpb`·`FsContext`·`FsContext2`·`SectionObjectPointer`. `IopParseDevice`·`IopAllocRealFileObject` 에 이 칸을 쓰는 줄이 **없다** `[정적]` (`IopAllocRealFileObject +0x419 ~ +0x44d` 의 복사는 다시 열기 갈래). IRP 를 받은 파일 시스템이 채운다 `[추측]`. `Flags` `0x40`·`0x80000` 도 nt 에 켜는 줄이 없다.
  - **`SectionObjectPointer` 는 nt 가 곧바로 읽고 쓴다.** `MiReferenceControlArea +0x44`·`MiSectionCreated +0x53` 이 읽고, 가리키는 `_SECTION_OBJECT_POINTERS` (0x18, FS 메모리) 의 `ImageSectionObject` 칸은 **nt 가 쓴다** (`MiReferenceControlArea +0x178` 자리표시, `MiSectionCreated +0x124` 진짜). → 에뮬레이터는 ntfs.sys 가 없으니 **대신 채워 줘야 하는 칸**이다 (결과서 의견).
- **(나') nt 함수·FS 가 부름** — `ReadAccess`·`SharedRead`·`SharedDelete` (와 0 을 쓰는 `WriteAccess`·`DeleteAccess`·`SharedWrite`). 쓰는 코드는 `IoSetLinkShareAccess +0x16 ~ +0x67` (nt). 부르는 곳은 `IoSetShareAccess`·`IoSetShareAccessEx`·`IoCheckShareAccess*`·`RawCreate`·`FsRtlpCheckSharingAgainstOplockOwner` 뿐이고 `IopParseDevice` 는 아니다 (`refs`) → 부르는 쪽 = 파일 시스템 `[추측]`. 값 (`0x100020`·공유 5 → 1·0·0 / 1·0·1) 이 K1 과 맞는다.
- **(다) 아무도 안 씀** — `PrivateCacheMap`·`FinalStatus`·`LockOperation`·`DeletePending`·`Busy`·`LastLock`·`CompletionContext`·`FileObjectExtension`. `memset` (`+0x21e`) 만.

### 갈림길 — test.exe 가 가는 쪽 (질문 6)

| 갈림길 | 줄 | test.exe | 근거 |
|---|---|---|---|
| 같은 파일 이미지 컨트롤 영역이 이미 있나 | `MiReferenceControlArea +0x66` 이하 → `MiCreateImageOrDataSection +0x1bf` | **못 정함** | 사진 값이 두 길에서 같음 → D1 |
| 컨트롤 영역 만드는 중 → 기다림 | `MiReferenceControlArea +0x2fb KeWaitForGate` | 안 기다림 | 사진 `WaitList` null |
| 헤더가 메모리에 있나 | `MiCreateImageFileMap +0x157` / `+0x4fc MiReadImageHeaders` | 동적 (캐시) | — |
| 프리페치 스레드인가 | `MiCreateImageFileMap +0x133` 등 | 아님 | 사진 `u` bit9 없음 |
| 원격 파일인가 | `MiFinishCreateSection +0xb8 ~ +0xc2` | 아님 | 사진 `u1` = 컨트롤 영역 주소 |
| `SEC_BASED` 인가 | `MiFinishCreateSection +0x1ca` | 아님 | 사진 `StartingVpn`·`SectionNode` 0 |
| 세션 전역 이미지인가 | `MiFinishCreateSection +0x63` | 아님 | 사진 `SessionId` 0 |
| COM (.NET) 디렉터리 | `MiParseComAndCetHeaders` | 아님 | 헤더 COM 디렉터리 0 (결과서가 파일 직접 읽음) |
| 플랫 매핑 / 4GB 아래 요청 | `MiBuildImageControlArea +0x270`, `MiCreateImageFileMap +0x323` | 아님 | 사진 `ImageFlags` `0x4` |
| exe 열기가 상대 열기인가 | `ObpLookupObjectName +0x135b` → `IopParseFile` | 아님 (절대 경로) | `[정적 조건]` → D8 |
| 첫 열기 실패 → 재시도 | `NtCreateUserProcess +0x6cf JNS` → `+0x73b IoCreateFileEx` | 첫 번째 성공 쪽 `[추측]` | — |
| 필터 드라이버 | `MiCreateImageOrDataSection +0x14a MiCallCreateSectionFilters` | 안 읽음 | 이름만 |

### 읽는 입력

- exe 이름: `NtCreateUserProcess` `[RBP+0x290]` `UNICODE_STRING` → 객체 속성 `ObjectName` (`+0x584`). 이 칸을 채우는 줄은 못 찾았다 `[공백]` (앞 함수가 채움 — 부모가 넘긴 NT 경로로 보임 `[추측]`).
- 파일 시스템에 묻는 것: 파일 크기 (`FsRtlGetFileSize`), 헤더 첫 쪽 (`MiCopyHeaderIfResident` / `MiReadImageHeaders`), 열기 IRP 결과 (`FsContext*`·`SectionObjectPointer`·`Vpb`·공유 접근).
- PE 헤더 칸 → `MiVerifyImageHeader` 칸 모음 (0x70) → `MiBuildImageControlArea` 가 이미지 정보로 (3.23).
- 호출자 값: 페이지 보호 `0x10` (`MmCreateSpecialImageSection +0xb4`), 서명 수준 바이트 (패킷 `+0x18`), 세션 id, 플래그.
- 시스템 상태: `MiSystemPartition` (파티션 0), 무작위 값 (`MiSelectImageBase` ASLR), `MiHalfSecond` (재시도 쉬는 시간).
- 이 조각 안에는 `syscall` 이 없다 (전부 부모 `NtCreateUserProcess` 시스템 콜 안).

## 확인된 것

- **`_SECTION` 객체는 `MiFinishCreateSection` 이 만든다** — `MiCreateSection +0x120` (ret `+0x125`), 컨트롤 영역·세그먼트 사슬 (`+0xf8 MiCreateImageOrDataSection` 아래) 이 **다 끝난 뒤**의 형제 호출이다. `+0x143 ObCreateObjectEx` (`MmSectionObjectType`, 몸통 `0x40`). 사슬 안 (`MiCreateNewSection` 이하) 에서는 섹션 객체를 안 만든다. `[정적]`
- **`_SECTION` 몸통 0x40 바이트는 패킷 사본을 한 번에 복사한다** — `MiCreateSection +0x58` 이 0 으로 지운 스택 패킷 `+0x50 ~ +0x8f` 에 사슬이 값을 모으고, `MiFinishCreateSection +0x15b ~ +0x179` 가 `MOVUPS` 4번으로 옮긴다. 그 뒤 `+0x17d` `StartingVpn` = 0, `+0x1ba` `u.UserReference` 켬. `[정적]` · 사진 `u` `0x000100a0` = 컨트롤 영역 `u` `0xa0` + `UserReference`
- **컨트롤 영역·세그먼트 칸은 거의 `MiBuildImageControlArea` 한 함수가 쓴다** (새 길). 나머지는 `MiParseImageSectionHeaders` (`u3`·`ImageCommitment`·`DefaultProtectionMask`·`NumberOfCommittedPages`), `MiCreateImageFileMap` (`u1.PartitionId`, 조건부 `Binary32`·`BaseBelow4gb`), `MiSectionCreated` (`FilePointer`·`WaitList`·`BeingCreated` 끔), `MiSelectImageBase`·`MiRelocateImage` (`u2` 재배치 칸·`BasedAddress`·`ImageActive`). `[정적]`
- **`_SECTION_IMAGE_INFORMATION` 의 `SubSystemVersion`·`OperatingSystemVersion` 은 겹침이다** (`[정적] PDB`: 같은 4바이트) — 쓰는 줄은 이미 아는 Minor/Major 줄 (`MiBuildImageControlArea +0x193 ~ +0x1b1`). `ImageFlags` 비트는 `ImageDynamicallyRelocated` 말고 **전부 조건부**라 test.exe 는 `0x4` 하나. `ZeroBits` 는 쓰는 줄 없음. `[정적]`
- **3.23 "오프셋 미상" 14줄이 채워졌다** (`MiBuildImageControlArea +0x167 ~ +0x1f0`). `ImageContainsCode` 는 실행 가능 구역이면 `MiParseImageSectionHeaders +0x1bf` 도 1 을 쓴다. `[정적]`
- **exe 파일 객체 사슬**: `IopCreateFile → (조각) ObOpenObjectByNameEx → ObpLookupObjectName +0xbcd → IopParseDevice +0xe1e → IopAllocRealFileObject`. 할당은 `ObpAllocateObject` (`0xd8`) → `memset` → 칸 채우기. `IopParseFile` 은 상대 열기 징검다리라 절대 경로 exe 는 안 탄다. `[정적]`
- **exe 를 여는 쪽은 `+0x6cb IopCreateFile`** 이다. `+0x740 IoCreateFileEx` 는 첫 번째가 실패했을 때만 타는 재시도 (`+0x6cf JNS +0x764`). run9 의 "exe 열기 = `+0x6cb`" 가 맞다. `[정적]`
- **파일 시스템 칸 4개 (`Vpb`·`FsContext`·`FsContext2`·`SectionObjectPointer`) 는 nt 에 쓰는 줄이 없다** `[정적]`. 공유 접근 칸은 nt 공개 함수 `IoSetLinkShareAccess` 가 쓴다 (부르는 쪽은 FS `[추측]`).

## 정정

- **baggage 3.21 · 5장 ①1.1 — `NumberOfUserReferences` +1 은 `MiCreateImageOrDataSection +0x11d` 가 아니라 `MiReferenceControlArea +0x11d`** (공유 길). `.asm` `+0x11d INC qword ptr [RBX + 0x30]` (오케스트레이터·이 문서 둘 다 확인). `MiCreateImageOrDataSection` 에는 `[…+0x30]` 을 올리는 줄이 없다 (결과서). 새 길에서는 `MiBuildImageControlArea +0x214 MOV [RSI+0x30],1` 이 쓴다. 짐 지도는 이 스프린트 문서라 바로 고쳤다.
  - 출처: run31 은 `MiReferenceControlArea` 갈림길 절 안에서 "사용 수를 1 올리고 (`+0x11d`)" 라고 함수 이름 없이 썼다. run43 (sast/38 대조) 이 이걸 옮기며 `MiCreateImageOrDataSection` 에 붙였다. run31 맨 위에 정정 한 줄을 넣었다.
- **baggage 3.21 `u` 줄 — `[추측]` 짐작 → `[정적]` 정확.** 옛 줄은 "자리표시용 컨트롤 영역 (스택 사본) 에 `BeingCreated` 켬 — 진짜 칸 아님" 이었다. 진짜 컨트롤 영역 `u` 는 `MiBuildImageControlArea +0x9e` (`Image` `0x20`) · `+0x222` (`File`+`BeingCreated` `0x82`) · `+0x22b ~ +0x238` (선호 노드 비트) 이 쓰고 `MiSectionCreated +0xa6` 이 `BeingCreated` 를 끈다. 사진 `0xa0` 과 맞다.
- **baggage 3.20 "만드는 길" — `_SECTION` 은 `MiCreateImageFileMap` 쪽 사슬이 아니라 형제 `MiFinishCreateSection`** 이 만든다 (위 확인된 것 첫 줄).
- flow.md 2.1 `MiCreateSection` 절은 "`+0xf8` 에서 `MiCreateImageOrDataSection` 을 부른다" 까지만 적어 객체 만들기가 빠져 있었다 → `+0x125 MiFinishCreateSection` 을 더했다 (틀린 말은 아니라 정정이 아니라 보탬).

## 아직 확인 필요

- 동적 (결과서 번호 그대로). D1 은 plan **Q61** 에 합침, D4 ~ D7 은 새 **Q92** (낮은 우선순위 D2·D3·D8·D9·D10 도 Q92). 멈춤점은 `nt+rva` (`_index.tsv` 기준), test.exe 일 때만 서게 하는 조건은 dast-mentor 몫.
  - **필요 (정적으로 못 정함)**:
    - **D1** 공유 길인가 새 길인가 — `nt+0x96f4d0` (`MiCreateNewSection` 입구) / `nt+0x96e6cc` (`MiShareExistingControlArea` 입구) 중 서는 쪽. 또는 test.exe 를 **재부팅 없이 두 번** 돌려 두 K1 의 `_SECTION.u1` (컨트롤 영역 주소) 비교 — 같으면 공유. 사진 두 번으로 답할 수 있다. → Q61
    - **D4** `CA.u2` (`+0x58`/`+0x5c`) 가 어느 갈래로 `ffffffff`/`0x00c00009` 가 되나 — `nt+0x972de0` (`MiSelectImageBase` 입구), `nt+0x972fed` (`+0x20d` 근처), 끝에서 `dq @r15+0x58 L1`, `r ebx`, `r ebp`. → Q92
    - **D5** `NumberOfPfnReferences` 3 → `0x2d`, `FilePointer` 아래 비트를 누가 바꾸나 — K1 에서 `ba w8 <CA+0x20>`, `ba w8 <CA+0x40>`, 서면 `k` (조건: 컨트롤 영역 주소 고정). → Q92
    - **D6** `FILE_OBJECT.Flags` `0x4400` 을 누가 켜나 (exe 핸들 닫을 때가 맞나) — `nt+0x842838` (`PspDeleteCreateProcessContext +0x134`) `r rcx`, `nt+0x86c660` (`IopCloseFile` 입구), `ba w4 <FO+0x50>` 서면 `k`·`ln @rip`. → Q92
    - **D7** `FsContext`·`FsContext2`·`SectionObjectPointer`·`Vpb`·`Flags 0x40/0x80000` 쓴 모듈이 ntfs 인가 — `nt+0x8bbb8f` (`IopParseDevice +0xf5f`, `IofCallDriver` 직후) 에서 파일 객체 주소를 잡고 `ba w8 <FO+0x10>`·`+0x18`·`+0x20`·`+0x28`, 서면 `lm a @rip`. → Q92
    - D5·D6·D7 은 `ba w` (데이터 멈춤점) 라 한 실행에서 같은 주소로 같이 본다 (주소는 실행마다 바뀜).
  - **확인만 (낮은 우선순위, Q92)**:
    - **D2** `_SECTION` 사본이 맞게 모이나 — `nt+0x96dd64` (`MiFinishCreateSection +0x148`) 앞뒤 `dq @rdi+0x50 L8` / `dq @rcx L8` (`+0x18` 0). K1 은 이미 최종 값이라 사진으론 안 됨.
    - **D3** 원격 파일·`SEC_BASED`·세션 갈래 안 탄 것 — `nt+0x96dc1c` 입구 `dd @rdi+0x10 L1`, `dd @rdi L1`.
    - **D8** exe 열기가 `IopParseFile` 안 타고 `+0xe19` 로 가나 — `nt+0xa11580` 안 서는지, `nt+0x8b5e40` (`IopAllocRealFileObject`) 입구 `k` 에서 `IopParseDevice +0xe1e`.
    - **D9** `ReadAccess`/`Shared*` 를 부르는 쪽이 ntfs 인가 — `nt+0x9b5660` (`IoSetShareAccess`) / `nt+0x9b57c0` (`IoCheckShareAccess`) 서면 `k`.
    - **D10** `NoValidationNeeded` 의 서명 수준 값 — `nt+0x96dc1c` 입구 `db @rdi+0x18 L1`, `dd @rdi L1` bit18.
  - 정적으로 못 찾은 것 (Q92 에 같이 적힘): 파일 이름 문자열 `NtCreateUserProcess [RBP+0x290]` 을 채우는 줄 `[공백]`.
- 정적 (나머지 Q78 이 넘겨받는 것 — 결과서 "나머지 Q78 로 넘길 것"):
  - **Q78c**: `EPROCESS.SectionObject` (+0x2a8) · `ImageFilePointer` (+0x330, 파일 객체 = `[RBP+0x258]`, 참조 `NtCreateUserProcess +0x791`) · `SectionBaseAddress` 쓰기.
  - **Q78c / Q79 (매핑)**: `MiMapProcessExecutable → MmMapViewOfSectionEx → MiMapViewOfImageSection` 이 `NumberOfMappedViews`·`ListHead` 연결·`NumberOfUserReferences` 를 올린다 `[추측]`.
  - 칸 표가 아직 없는 짐 (PDB 정의 있음): `_SECTION_OBJECT_POINTERS` (0x18, `ImageSectionObject` 는 nt 가 씀), `_MI_IMAGE_ADDITIONAL_INFO` (0x48, `CA.u2.ImageInfoRef` 가 가리키는 할당 꼬리), `_MI_EXTRA_IMAGE_INFORMATION` (0x10, 이미지 정보 `+0x40`), `_SUBSECTION`·프로토 PTE 표, `_OPEN_PACKET` (0xe0, 임시) → baggage 7.6 에 기록만.
  - 이름만 둔 줄기 밖 함수: `MiInitializeNewImageSectionProtos`·`MiValidateExistingImage`·`MiCallCreateSectionFilters`·`MiCreatePerSessionProtos`·`MiFindEmptyAddressRangeDownTree` (뽑아 둠, 본문 안 읽음).

## 도구 메모

- `pdb_struct.py` 첫 실사용 (결과서 평가): 다섯 구조체 칸 수가 baggage 7.2 와 정확히 같고 한 번에 0.1초. 한계 둘 — 하네스라 오케스트레이터가 사용자에게:
  - 이름 없는 공용체 (`<unnamed-tag>`) 안쪽을 안 펼친다 (`_SECTION.u1`·`.u`, `_CONTROL_AREA.u`·`u1`·`u2`·`u3`, `_SEGMENT.u1`·`u2`). 제안: `--expand` 로 `u2.e2.…` 꼴 펼치기.
  - 겹치는 칸 표시가 없다 (`SubSystemVersion` 과 Minor/Major, `ListHead`/`AweContext`, `ExtendInfo`/`BasedAddress`). 제안: `overlap` 열.
- `MiFinishCreateSection` 복사 줄: 결과서 3.20 표의 "복사 `+0x172`" 는 마지막 `MOVUPS` **읽기** 자리다. 객체에 쓰는 자리는 `+0x15f`·`+0x166`·`+0x16e`·`+0x179` (16바이트씩). 짐 지도에는 범위 `+0x15b ~ +0x179` 로 적었다.

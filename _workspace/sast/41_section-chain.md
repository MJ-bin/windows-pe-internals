# 41 section-chain — 정적 분석 요청

- 날짜: 2026-10-28
- 스프린트: 03_sprint (짐 지도 바퀴, 예정 run47)
- 대기열: **Q78b** — Q78 나머지를 시간 순서로 나눈 첫 조각 (사용자 결정 2026-10-28: Q78b 섹션 묶음 → Q78c 프로세스 객체 → Q78d 스레드 → Q78e ntdll 앞쪽 → Q78f KERNELBASE·KERNEL32 구간 → Q78g KUSD·남은 전역)
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` 3.20 `_SECTION` · 3.21 `_CONTROL_AREA` · 3.22 `_SEGMENT` · 3.23 `_SECTION_IMAGE_INFORMATION` · 3.24 `_FILE_OBJECT`, 7.2 의 같은 다섯 구조체 모르는 칸 이름
  - `runs/02_sprint/run31-image-section.md` (sast/26: 이미지 섹션 만들기 사슬), `run9` (`NtCreateUserProcess +0x6cb IopCreateFile` exe 열기)
  - `runs/03_sprint/run44-baggage-snap.md` (사진: 다섯 구조체 칸 거의 다 `K1 이전`)
  - 이전 요청 sast/26 (섹션 사슬), sast/38 (체크리스트), sast/40 (Q78a 양식)

## 범위

- 구조체 다섯 개의 **모르는 칸 74개** (baggage 7.2):
  - `_SECTION` 9: `K1 이전` u1, SizeOfSection, u, .InitialPageProtection, .NoValidationNeeded / 끝까지 0 SectionNode, StartingVpn, EndingVpn, .SessionId
  - `_CONTROL_AREA` 15: `K1 이전` Segment, ListHead, AweContext, NumberOfSectionReferences, NumberOfPfnReferences*, NumberOfMappedViews, FilePointer*, u2, LockedPages, u3 / 끝까지 0 u1, ControlAreaLock, ModifiedWriteCount, WaitList, FileObjectLock
  - `_SEGMENT` 10: `K1 이전` ControlArea, TotalNumberOfPtes, SegmentFlags, SizeOfSegment, ExtendInfo, BasedAddress, u1, PrototypePte / 끝까지 0 NumberOfCommittedPages, SegmentLock
  - `_SECTION_IMAGE_INFORMATION` 10: `K1 이전` SubSystemVersion, OperatingSystemVersion, ImageFlags / 끝까지 0 ZeroBits, ImageFlags 의 비트 6개
  - `_FILE_OBJECT` 30: `K1 이전` Type, Size, DeviceObject, Vpb, FsContext, FsContext2, SectionObjectPointer, ReadAccess, SharedRead, SharedDelete, Flags*, FileName, Lock, Event*, IrpList / 끝까지 0 15개 (7.2 그대로)
  - (`*` = 사진에서 뒤에 또 바뀐 칸. `_CONTROL_AREA.NumberOfPfnReferences` 3 → `0x2d`, `FilePointer` 아래 비트 `P0~P1`, `_FILE_OBJECT.Flags` `0xc0042` → `0xc4442` `K3~P0`)
- 이미 아는 칸 (3.21 2줄, 3.22 1줄, 3.23 17줄) 은 다시 보지 않는다. 3.23 의 "오프셋 미상" 줄은 `.asm` 오프셋을 채워 주면 좋다 (선택).
- 칸 이름·오프셋은 새 도구 `python3 .claude/skills/ghidra-headless-export/scripts/pdb_struct.py ntoskrnl.exe _SECTION _CONTROL_AREA _SEGMENT _SECTION_IMAGE_INFORMATION _FILE_OBJECT` 로 뽑는다 (이번에 하네스에 들어감).
- **범위 밖**: 프로세스 객체 (`_EPROCESS` 등, Q78c), 매핑 (`MmMapViewOfSection`·VAD, Q78c/Q79), 서브섹션·프로토타입 PTE 표 안 내용 (칸 표가 없는 짐 — 이름만), 파일 시스템 내부.

## 알고 싶은 것

1. **`_SECTION` 객체를 만들고 칸을 채우는 줄** (함수+오프셋)
   - 객체 할당 (`ObCreateObjectEx` 등) 은 섹션 사슬 (`MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection / MiShareExistingControlArea`) 중 어디인가?
   - `u1` (ControlArea 등), `SizeOfSection`, `u` (플래그 — `InitialPageProtection`, `NoValidationNeeded` 비트) 를 쓰는 줄. `SectionNode`·`StartingVpn`·`EndingVpn`·`.SessionId` 가 0 인 이유 (이미지 섹션이라 안 씀 / 0 으로 씀).
   - 객체 머리 (`_OBJECT_HEADER`) 는 범위 밖 — `ObCreateObjectEx` 는 공용 부품으로 이름만.
2. **`_CONTROL_AREA`·`_SEGMENT` 칸을 채우는 줄** — `MiBuildImageControlArea`, `MiCreateImageFileMap`, `MiCreateNewSection`, `MiSectionCreated`, `MiReferenceControlArea` 등.
   - 3.21 에 적힌 대로 **새로 만드는 길** (`MiCreateNewSection → MiCreateImageFileMap → MiBuildImageControlArea`) 과 **같은 파일 공유 길** (`MiShareExistingControlArea`, `MiCreateImageOrDataSection +0x1d5`) 이 있다. 두 길 각각에서 칸마다 누가 쓰나 적고, run44 의 test.exe 실행이 어느 길이었을지 사진 값 (예: `NumberOfSectionReferences`, `NumberOfMappedViews`, `NumberOfUserReferences`) 으로 가를 수 있으면 가른다. 못 가르면 동적 질문.
   - `_CONTROL_AREA.FilePointer` (`_EX_FAST_REF`, 아래 비트가 참조 수) 와 `_SEGMENT.ControlArea`·`PrototypePte`·`TotalNumberOfPtes`·`SizeOfSegment`·`BasedAddress` 값의 출처 (파일 헤더 = 입력 / 계산).
   - 사진에서 `K1 이전` 이후 바뀐 `NumberOfPfnReferences` (3 → `0x2d`) 와 `FilePointer` 아래 비트 (`P0~P1`) 는 누가 바꾸나 — 이름만 (페이지 폴트·매핑 길이면 "범위 밖 길, 이름" 으로).
3. **`_SECTION_IMAGE_INFORMATION` 남은 10칸**
   - `SubSystemVersion`·`OperatingSystemVersion` 은 이미 아는 `SubSystemMinor/MajorVersion` 등의 union 겹침인가 (PDB 로 예/아니오) — 그러면 아는 칸으로 옮길 수 있다.
   - `ImageFlags` 비트 (`ComPlusNativeReady`·`ComPlusILOnly`·`ImageMappedFlat`·`BaseBelow4gb`·`ComPlusPrefer32bit`·`Reserved`) 를 켜는 조건과 줄, `ZeroBits`.
4. **`_FILE_OBJECT` 30칸** — exe 파일 객체
   - 만드는 사슬: `NtCreateUserProcess +0x6c6/+0x6cb IopCreateFile` → `ObOpenObjectByNameEx` → `ObpLookupObjectName` → `IopParseDevice` (`0x8bac30`, 9085바이트) / `IopParseFile` (`0xa11580`) → 파일 객체 할당·칸 채우기. 정확한 사슬과 할당 줄.
   - 칸마다 주인을 셋으로 나눈다: (가) ntoskrnl 이 쓴다 (`Type`·`Size`·`DeviceObject`·`Vpb`·`FileName`·`Flags`·`Event`·`Lock`·`IrpList`·`Shared*`·`ReadAccess` 등 — 줄과 함께), (나) **파일 시스템 드라이버가 쓴다** (`FsContext`·`FsContext2`·`SectionObjectPointer` 등 IRP_MJ_CREATE 처리 안 — `ntfs.sys` 가 `ms/bin/.../System32/drivers/ntfs.sys` 에 있지만 **이번엔 디컴파일하지 않고** "다른 드라이버 (ntfs.sys)" 로만 분류. 짐 지도 "다른 DLL" 열과 같은 뜻으로 쓸 수 있는지 의견), (다) 0 그대로.
   - `Flags` 의 `K3~P0` 변화 (`0xc0042` → `0xc4442`, 비트 `0x4400`) 는 누가 켜나 — 섹션 만들기 뒤 (`MmCreateSpecialImageSection`) / 매핑 쪽 후보를 이름으로.
5. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거) 으로. 경로는 `NtCreateUserProcess` 부터 지도 기준. 줄기 위는 끝까지, 줄기 밖 callee·공용 부품 (`ObCreateObjectEx`, `ExAllocatePool*`, 락, `RtlCopyUnicodeString` 등) 은 이름만.
6. **시스템 콜·입력**
   - 이 조각에서 읽는 입력 (exe 파일 헤더·파일 크기·부모가 넘긴 경로 문자열 등) 을 칸 이름과 함께.
   - 이 조각 안에서 다른 프로세스·부팅 상태에 따라 갈리는 곳 (예: 같은 파일 섹션이 이미 있음, 프리페치·캐시) 을 갈림길로 적고, test.exe (run44) 가 어느 쪽인지 정적·사진으로 정하거나 동적 질문으로.

## 대상

rva 는 `ms/decomp/ntoskrnl/_index.tsv` 기준 (2026-10-28 확인).

| 모듈 | 함수 | rva | depth | 이유 |
|---|---|---|---|---|
| ntoskrnl | `MmCreateSpecialImageSection`·`MiCreateSection`·`MiCreateImageOrDataSection`·`MiCreateNewSection`·`MiCreateImageFileMap`·`MiBuildImageControlArea`·`MiSectionCreated`·`MiShareExistingControlArea` | 이미 `.c`/`.asm` 있음 (sast/26) | 0 | 칸 쓰기 줄 찾기 (다시 뽑을 필요 없으면 그대로 읽기) |
| ntoskrnl | `MiReferenceControlArea` | `0x3260c8` | 1 | 공유 길 |
| ntoskrnl | `IopCreateFile` | `0x8c79b8` | 1 | 파일 객체 사슬 입구 |
| ntoskrnl | `IopParseDevice` | `0x8bac30` | 1 | 파일 객체 할당·칸 채우기 후보 (큰 함수) |
| ntoskrnl | `IopParseFile` | `0xa11580` | 0 | 사슬 확인 |
| ntoskrnl | (필요하면) 섹션 객체 할당 함수, 파일 객체 초기화 도우미 | `_index.tsv` / `pdb_aliases.py --where` 로 | 1 | 1·4번 답에 따라 |

- `ObOpenObjectByNameEx`·`ObpLookupObjectName`·`ObCreateObjectEx` 는 지도에서 `helper` (공용 부품) — 사슬 확인에 필요한 만큼만 보고 이름으로 가리킨다.

## 지도에서 본 것 (`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*`)

- `NtCreateUserProcess` 직접 호출: `+0x6c6/+0x6cb IopCreateFile`, `+0x73b/+0x740 IoCreateFileEx`, `+0x78c/+0x791 ObReferenceObjectByHandle`, `+0x937/+0x93c MmCreateSpecialImageSection`, `+0x96d/+0x972 ObReferenceObjectByHandle`.
- 깊이·caller: `IopCreateFile` 1·17 expanded, `IoCreateFileEx` 1·3, `MmCreateSpecialImageSection` 1·2, `MiCreateSection` 2·3, `MiCreateImageOrDataSection` 3·3, `MiReferenceControlArea` 4·1, `MiCreateNewSection` 4·1, `MiSectionCreated` 5·1, `MiCreateImageFileMap` 5·1, `MiBuildImageControlArea` 6·1 — 모두 expanded.
- `ObOpenObjectByNameEx` (2·22)·`ObCreateObjectEx` (2·47) 는 helper. `IopParseDevice`·`IopParseFile`·`ObpLookupObjectName` 은 지도에 없음 (간접 호출 — 객체 형식의 parse 프로시저). 사슬은 정적으로 설명하고 못 닫으면 동적 질문.
- `IopCreateFile` (`+0x6cb`) 과 `IoCreateFileEx` (`+0x740`) 둘 다 있다 — exe 파일 객체를 만드는 쪽이 어느 것인지 (run9 는 `+0x6cb`) 확인.

## 이미 아는 것

- `[정적]` run31/sast26: 섹션 사슬 call/ret 오프셋 (run31 표), `MiBuildImageControlArea` 가 컨트롤 영역 + 서브섹션 (풀), 세그먼트 0x98바이트 (풀) 를 만든다, `_SEGMENT.u2.ImageInformation` = `MiBuildImageControlArea +0x13e`, `TransferAddress` `+0x16b~+0x172`, 공유 길 `MiCreateImageOrDataSection +0x11d` 에서 `NumberOfUserReferences` +1. `MmCreateSpecialImageSection` 은 `0xc0000054` 면 쉬고 다시.
- `[정적]` run9: exe 열기 = `NtCreateUserProcess +0x6cb IopCreateFile`.
- `[관찰]` run44: 다섯 구조체 칸이 거의 다 `K1 이전` (= `PspAllocateThread` 전) 에 이미 값이 있음. 바뀐 칸 3개는 위 범위 `*`.
- `[추측]` `FsContext`·`FsContext2`·`SectionObjectPointer` 는 파일 시스템 (ntfs.sys) 이 IRP_MJ_CREATE 에서 채운다.

## 받고 싶은 것 (결과서 `_workspace/sast/41_section-chain_result.md`)

- 맨 앞 요약 표: 구조체별 "모르는 칸 → 아는 칸 (nt) / 다른 드라이버 / 0 그대로 / 여전히 모름" 숫자.
- 질문 1~6 답 + 근거 (`.asm` 의 +0x 줄). 태그 `[정적]` / `[정적] PDB` / `[추측]` / `[공백]`.
- 칸별 "쓰는 곳" 표 (baggage.md 3장 양식, 3.20 ~ 3.24 에 바로 옮길 수 있게).
- 동적 질문 목록 (번호, 추천 멈춤점, 찍을 것, 사진 덤프로 답할 수 있는지).
- 나머지 Q78 (Q78c~) 로 넘길 것.
- 코드는 길게 붙이지 않고 우리 말로.

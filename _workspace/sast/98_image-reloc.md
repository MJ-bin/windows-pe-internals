# 98 image-reloc — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q62a** (= Q62 (a)·(b)·(f)). Q62 가 한 바퀴에 커서 그 자리에서 Q62a (재배치·진입점 주소) · Q62b ((c) 공유 길 · (d) 프로토 PTE · (e) 작은 것) 로 나눔 (2026-11-15). 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례. **정적만.**
- 빌드: 26100.9457. ntoskrnl analyze 됨 (PDB 붙음). `_index.tsv` (rva·크기): `MiRelocateImage` `0x973444` (2093) · `MiCreateNewSection` `0x96f4d0` (1842) · `MiParseComAndCetHeaders` `0x92d374` (1041) · `MiMakeImageReadOnly` `0x49f654` (283) · `MiInitializeNewImageSectionProtos` `0x315ab0` (548) · `MiValidateSectionSigningPolicy` `0x92b940` (342) · `MiReleaseImageSection` `0x4941c4` (93) · `MiFinalizeImageHeaderPage` `0x4ab9a8` (295).
- 관련: run31 (`runs/02_sprint/run31-image-section.md`, sast/26) — `MiBuildImageControlArea +0x172 TransferAddress = ImageBase + 진입점 rva`, `MiCreateNewSection +0x3da MiRelocateImage` (새 기본 주소면 `TransferAddress += 델타` `[정적]`). run38 (`runs/02_sprint/run38-map-module.md`) — ntdll `LdrpCompleteMapModule +0x31b LdrpRelocateImage` 는 맵 결과가 정확히 `0x40000003` (`STATUS_IMAGE_NOT_AT_BASE`) 일 때만. exe 는 `LdrpInitializeProcess +0x1435 LdrpProtectAndRelocateImage` (그 문 안 거침).

## 알고 싶은 것

1. **(f, 가장 중요) 커널이 이미지 안 번지수를 미리 고쳐 두나**: `MiRelocateImage` 가 하는 일 — `TransferAddress` 델타 말고 **이미지 페이지 안 재배치 항목 (`.reloc`) 을 적용** 하나, 아니면 "새 기본 주소" 만 정하고 실제 고치기는 페이지 들어올 때 (`MiRelocateImagePfn` 류 — 이름 확인) 하나. DLL 이미지 섹션 (run26 `NtCreateSection` — ntdll `LdrpCreateDllSection`) 도 이 길을 지나나 (exe 만인가). 그래서 `NtMapViewOfSection` 이 DLL 에 `0` 을 주나 `0x40000003` 을 주나 — 그 status 를 정하는 줄 (`MiMapViewOfImageSection` 류, 이름 확인 — 이름·오프셋만).
   - 답이 "커널이 고침 → 0" 이면 ntdll `LdrpRelocateImage` 문은 언제 타나 (KASLR 이미지 vs `/DYNAMICBASE` 없는 이미지 등 조건).
2. **(b) `PspAllocateProcess +0x20b3 ~ +0x20ce` 보정 조건** 과 `MiRelocateImage` 델타의 관계 — 같은 델타를 두 번 더하지 않게 막는 조건인가 (run31·run35 의 그 줄 뜻).
3. **(a) 안 읽은 함수에도 `TransferAddress` (`_SECTION_IMAGE_INFORMATION +0x0`) 를 쓰는 줄이 있나**: `MiParseComAndCetHeaders` · `MiMakeImageReadOnly` · `MiInitializeNewImageSectionProtos` · `MiValidateSectionSigningPolicy` · `MiReleaseImageSection` · `MiFinalizeImageHeaderPage` — 함수마다 O/X (+ 줄). 이미지 정보 구조체 기준 오프셋을 PDB 타입으로 확인.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^MiRelocateImage$` | 1 | 질문 1 핵심 |
| ntoskrnl | `MiRelocateImage` 가 부르는 재배치 적용 함수 (`MiRelocateImagePfn`·`MiPerformFixups`·`LdrRelocateImage*`·`MiApplyDynamicRelocations` 류 — `_index.tsv`/`pdb_aliases.py` 로 이름 확인) | 0 | 질문 1 — 언제 고치나 |
| ntoskrnl | `^MiCreateNewSection$` | 0 | `+0x3da` 앞뒤 조건만 (run31 에서 읽음) |
| ntoskrnl | `NtMapViewOfSection` 아래 이미지 뷰 status 를 정하는 함수 (`MiMapViewOfImageSection` 류) | 0 | 질문 1 — `0x40000003` 을 만드는 줄만 |
| ntoskrnl | `^PspAllocateProcess$` | 0 | 질문 2 — `+0x20b3 ~ +0x20ce` 만 |
| ntoskrnl | 질문 3 의 여섯 함수 | 0 | `TransferAddress` 쓰기 O/X |

- 범위: 재배치·진입점 주소만. 공유 길 (`MiShareExistingControlArea` → `MiValidateExistingImage`), 프로토 PTE 만들기, `MiVerifyImageHeader` 이유 코드는 **Q62b** 몫 — 이번엔 안 본다.
- ntdll `LdrpRelocateImage` 본문은 안 본다 (run38 이름만 — 질문 1 의 "언제 타나" 는 커널 쪽 status 로 답한다).
- 공용 부품은 이름만.

## 지도에서 본 것

- ntoskrnl `_map/` 에 이 길 지도가 없으면 `run_ghidra.sh map` (진입 `MiRelocateImage`) 으로 깊이·직접 호출을 결과서에 옮긴다.

## 이미 아는 것

- `[정적]` run31: `MiBuildImageControlArea +0x172` 진입점 = `ImageBase + rva`; `MiCreateNewSection +0x3da MiRelocateImage` 새 기본 주소면 델타 더함.
- `[정적]` run38: ntdll 재배치 문은 맵 결과 `0x40000003` 일 때만.
- `[관찰]` (있으면 run 문서에서) test.exe·DLL 이 실제로 올라간 주소 vs 헤더 `ImageBase` — run44 사진의 `lm` 으로 대조해도 됨.

## 받고 싶은 것

- 결과서 `_workspace/sast/98_image-reloc_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: "누가·언제 이미지 안 번지수를 고치나" 표 (exe / DLL / ntdll 각각) + `NtMapViewOfSection` status 표.
- 질문 3: 여섯 함수 O/X 표.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (에뮬레이터는 ntdll 재배치 문을 쓰나, 커널 몫으로 미리 고치나 — 모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

# 101 image-share — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q62b** (= Q62 (c)·(d)·(e)). Q62a (sast/98 image-reloc, 재배치·진입점) 는 정적 끝. 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 빌드: 26100.9457. ntoskrnl analyze 됨. `_index.tsv` (rva·크기): `MiShareExistingControlArea` `0x96e6cc` (408) · `MiValidateExistingImage` `0x96fbd4` (883) · `MiSectionCreated` `0x315948` (352) · `MiInitializeNewImageSectionProtos` `0x315ab0` (548) · `MiVerifyImageHeader` `0x9707fc` (1443) · `MiCreateSection` `0x96d5e0` (617) · `MiRelocateImageAgain` `0x971ce8` (481) · `MiSwitchBaseAddress` `0x97330c` (302).
- 관련: run31 (`runs/02_sprint/run31-image-section.md`, sast/26) — 이미지 섹션 만들기, `MiCreateNewSection` (없을 때) vs `MiShareExistingControlArea` (이미 있을 때) 갈림. sast/98 결과서 (`_workspace/sast/98_image-reloc_result.md`) — 재배치는 `MiRelocateImage` 가 표만 만들고 페이지는 `MiRelocateImagePfn`/`MiPerformFixups` (page-in 또는 `MiWalkEntireImage`), status `0`/`0x40000003` 은 `MiMapViewOfImageSection +0x42a`; 공유 길에서 `MiRelocateImageAgain → MiSwitchBaseAddress` 까지 닿음.
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/100) 을 돌린다 — ntdll 쪽은 건드리지 않는다.

## 알고 싶은 것

1. **(c) 공유 길**: 같은 파일의 이미지 섹션이 이미 있을 때 (`kernel32`·`kernelbase` 처럼 다른 프로세스가 이미 올린 DLL — KnownDlls 섹션이면 이 길이 아님) `MiShareExistingControlArea` → `MiValidateExistingImage` 가 무엇을 검사하나 (서명·CFG·파일 바뀜·기준 주소 충돌 — 칸·조건·실패 status). 검사 실패면 새 컨트롤 영역을 만드나 (`MiCreateNewSection` 으로 다시), status 로 끝나나. `MiRelocateImageAgain`/`MiSwitchBaseAddress` 는 언제 타나 (기준 주소를 다시 고르는 조건) — 그때 `BasedAddress`·`TransferAddress` 가 어떻게 바뀌나 (sast/98 의 `MiSwitchBaseAddress +0x4e` 와 이어서).
2. **(d) 헤더 페이지 → 프로토 PTE**: `MiSectionCreated +0x103 MiInitializeNewImageSectionProtos` 가 하는 일 — 프로토 PTE 를 섹션마다 어떤 보호값 (`PAGE_*` → PTE 보호) 으로 채우나, 헤더 페이지는 어떻게 다루나 (`MiFinalizeImageHeaderPage` 와의 차례). 결과 표 (섹션 특성 → 보호).
3. **(e) 작은 것**:
   - `MiVerifyImageHeader` 이유 코드 `0x57` · `0x58` · `0x59` 가 무엇을 검사하다 실패한 것인가 (조건 줄 하나씩).
   - `Feature_TrustedLaunchHosts…` 검사 (run31 `+0x64/+0x69`) 가 무엇을 켜고 끄나 — test.exe 에서 타나.
   - run31 `+0x86` (ret `+0x8b`) 세션 id 함수의 PDB 별칭 (이 주소의 이름이 둘 이상이면 모두).
   - `MiCreateSection` 안쪽 재시도 (같은 함수 안 되돌아가는 갈림) 조건.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^MiShareExistingControlArea$` | 1 | 질문 1 |
| ntoskrnl | `^MiValidateExistingImage$` | 1 | 질문 1 |
| ntoskrnl | `^MiRelocateImageAgain$`, `^MiSwitchBaseAddress$` | 0 | 질문 1 — 조건과 칸 바꾸기만 |
| ntoskrnl | `^MiSectionCreated$`, `^MiInitializeNewImageSectionProtos$` | 1 | 질문 2 |
| ntoskrnl | `^MiVerifyImageHeader$` | 0 | 질문 3 — `0x57`~`0x59` 줄만 |
| ntoskrnl | `^MiCreateSection$` | 0 | 질문 3 — 재시도·`+0x64`·`+0x86` 자리만 |

- 범위: 위 질문만. 서명 검사 본문 (`SeValidateImageHeader`·`CI.dll`), CFG 비트맵 본문은 이름만.
- 이미 있는 `.asm`·`.c` 는 다시 뽑지 말고 그대로 읽는다 (sast/98 이 뽑은 것 포함). 새로 필요한 것만 `^이름$` 로.
- 공용 부품은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/101_image-share_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: 공유 길 검사 표 (칸 · 조건 · 실패하면 가는 곳 · status).
- 질문 2: 섹션 특성 → PTE 보호 표.
- 질문 3: 이유 코드 표, 작은 것 셋 한 줄씩.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

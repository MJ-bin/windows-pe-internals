# 33 map-module — 정적 분석 요청

- 날짜: 2026-10-24
- 스프린트: 02_sprint (예정 run38)
- 관련: 대기열 Q52 (단계 3 정적 묶음, 출처 sast/21 "다음 정적 후보 — 가치 높음"), flow.md 2.6 `LdrpMapDllWithSectionHandle` 트리 `+0x4e`/`+0x24f`, 그림 2 `jm1`·`jm2`·`jn1`·`jn4` ("재배치 후보 추측, Q52"), 2.6 "재배치는 이 세 함수 안에 없다 — 후보 둘" `[추측]`
- 이전 요청: sast/21 (run26, `LdrpMapDllWithSectionHandle`), sast/26 (run31, 커널 쪽 이미지 섹션·`MiRelocateImage`)

## 알고 싶은 것

1. **`LdrpMinimalMapModule` (rva `0xa220`, 915B) 가 메모리에 붙이는 길**
   - 1a. 기본 길은 `+0x174 NtMapViewOfSection` 인가, `+0x36c NtMapViewOfSectionEx` 인가? 어떤 조건에서 다른 쪽을 타나 (예/아니오 + 조건 줄). 인자 한 줄: 섹션 핸들은 어디서(`ctx`/항목 칸), 기준 주소 받는 칸, 보기 크기 받는 칸, 보호값.
   - 1b. 맵 결과로 항목의 `DllBase`·`SizeOfImage` (`_LDR_DATA_TABLE_ENTRY` 칸) 를 여기서 채우나? 줄 번호.
   - 1c. 결과 코드: `STATUS_IMAGE_NOT_AT_BASE`(`0x40000003`)·`STATUS_IMAGE_MACHINE_TYPE_MISMATCH`(`0x4000000e`) 등 성공-경고 코드를 어떻게 다루나 — `+0x2ff LdrpProcessMachineMismatch`, `+0x2d4 NtUnmapViewOfSection`, `+0x2a9 LdrpCheckForRetryLoading` 갈래 조건. 돌려주는 값(caller 가 `EBP` 로 받아 `LdrpCompleteMapModule` 셋째 인자로 넘기는 것, sast/21) 은 무엇인가.
   - 1d. `+0x257 LdrQueryImageFileKeyOption`(별칭 주의 — `pdb_aliases.py --where ntdll` 로 이름 확인)·`+0x278 RtlAcquirePrivilege`/`+0x37a RtlReleasePrivilege` 갈래는 무엇이고, 보통 test.exe 의 dll1·dll2·kernel32 에서 탈 것 같은가.
2. **`LdrpCompleteMapModule` (rva `0xa5c0`, 940B) 이 재배치 담당인가?** (예/아니오)
   - 2a. `+0x31b LdrpRelocateImage` 를 부르는 조건 — 셋째 인자(맵 결과 코드) 가 `0x40000003` 일 때인가? 그 밖의 조건(항목 Flags, `DllCharacteristics`, `.reloc` 유무).
   - 2b. `LdrpRelocateImage` (rva `0xe3984`, 359B) 는 depth 0 으로 이름·인자·안에서 부르는 것만 (예: `LdrRelocateImageWithBias`/`LdrProcessRelocationBlock`, 보호 바꾸기). 본문 깊이 읽지 않음.
   - 2c. 나머지 갈래 한 줄씩: `+0x285 LdrpCorValidateImage`·`+0x2a6 LdrpCorFixupImage`(.NET), `RtlGetCurrentServiceSessionId` ×4, `+0x129 RtlAddressInSectionTable`, ETW 두 곳.
3. **커널 재배치와의 관계** (run31 `[정적]`: 커널이 이미지 섹션을 만들 때 `MiRelocateImage` 가 기준 주소를 옮겨 둠): ASLR 로 커널이 이미 옮겨 둔 이미지에서 `NtMapViewOfSection` 은 `0x40000003` 을 돌려주나, `STATUS_SUCCESS` 를 돌려주나? 정적으로 ntdll 쪽에서 보이는 만큼만 — 예를 들어 `0x40000003` 이 아닐 때 `LdrpRelocateImage` 를 안 부른다면 "보통 DLL 은 유저 재배치 안 함" `[추측]` 으로 적고 동적으로 넘김.
4. 이 둘이 **exe** 에도 불리나? (지도상 caller 는 `LdrpMapDllWithSectionHandle` 하나씩 — exe 는 커널이 이미 붙였으니 안 불린다고 보면 되는지 한 줄)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpMinimalMapModule$` | 1 | 질문 1 (`.asm` 이미 있음) |
| ntdll | `^LdrpCompleteMapModule$` | 1 | 질문 2 (`.asm` 이미 있음) |
| ntdll | `^LdrpRelocateImage$` | 0 | 질문 2b — 지도에서 깊이 10 에 닿아 안 펼쳐짐 |
| ntdll | `^LdrpProcessMachineMismatch$` | 0 | 질문 1c — 이름·갈래만 (185B) |

- depth 1 callee 본문은 이름·인자·갈래 조건만. 로그(`LdrpLog*`), `RtlEqualUnicodeString`, `RtlImageNtHeaderEx`, `.NET` 둘의 본문은 읽지 않음.
- 범위 밖: 커널 `NtMapViewOfSection` 안쪽 (run31 Q61·Q62 에서), `LdrpProcessMappedModule` 본문 (sast/18·19·30 에서 일부).

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpMinimalMapModule` rva `0xa220`, 915B, 깊이 9, caller 1곳 (`LdrpMapDllWithSectionHandle +0x49/+0x4e`), expanded. 직접 호출 13줄: `+0x50`/`+0x7f`/`+0x1e6`/`+0x214 LdrpLogInternal`, `+0x92 RtlEqualUnicodeString`, `+0x174 NtMapViewOfSection`, `+0x257 LdrQueryImageFileKeyOption`(별칭 1개 더), `+0x278 RtlAcquirePrivilege`, `+0x2a9 LdrpCheckForRetryLoading`, `+0x2d4 NtUnmapViewOfSection`, `+0x2ff LdrpProcessMachineMismatch`, `+0x36c NtMapViewOfSectionEx`, `+0x37a RtlReleasePrivilege`. (지도 표의 오프셋은 call 자리. flow.md 의 `+0x179` 는 ret)
- `LdrpCompleteMapModule` rva `0xa5c0`, 940B, 깊이 9, caller 1곳 (`LdrpMapDllWithSectionHandle +0x24a/+0x24f`), expanded. 직접 호출 13줄: `+0x58 RtlImageNtHeaderEx`, `+0x129 RtlAddressInSectionTable`, `+0x16b`/`+0x1c5`/`+0x204`/`+0x354 RtlGetCurrentServiceSessionId`, `+0x24f`/`+0x39e LdrpLogEtwEvent`, `+0x285 LdrpCorValidateImage`, `+0x2a6 LdrpCorFixupImage`, `+0x2fa LdrpLogInternal`, `+0x31b LdrpRelocateImage`, `+0x338 LdrpLogError`.
- `LdrpRelocateImage` rva `0xe3984`, 359B, 깊이 10 (최대 깊이라 안 펼침). `LdrpProcessMachineMismatch` rva `0x10af3c`, 185B, 깊이 10.

## 이미 아는 것

- `[정적]` (run26, sast/21) `LdrpMapDllWithSectionHandle`: `+0x4e LdrpMinimalMapModule` → 락 안 `+0x146 LdrpInsertDataTableEntry` → `+0x151` 색인 → `+0x24f LdrpCompleteMapModule(…, …, EBP=맵 결과 코드)` → `+0x26c LdrpProcessMappedModule`. 재배치하는 줄은 `LdrpMapDllWithSectionHandle`·`LdrpMapDllNtFileName`·`LdrpMapDllSearchPath` 안에 없음.
- `[정적]` (run21) KnownDll 길(kernel32·kernelbase)도 파일 길(dll1·dll2)도 같은 `LdrpMapDllWithSectionHandle +0x4e` → `LdrpMinimalMapModule` 로 붙임.
- `[정적]` (run31, sast/26) 커널: 이미지 섹션을 만들 때 `MiRelocateImage` 조각이 델타를 더함 (ASLR). 진입점 `TransferAddress` 도 그때.
- `[추측]` (run26) 재배치 후보 = `LdrpCompleteMapModule` 또는 `LdrpProcessMappedModule` (`+0x24d LdrpIsExecutableRelocatedImage`).

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x` 줄, call/ret 구분). 칸 이름은 PDB 타입으로.
- 두 함수 각각 큰 구간 표 (4~6행).
- 대표 지점 1~3개 (그림 2 `jm1`/`jm2` 상자 글 고칠 것: 붙이는 시스템 콜, 재배치 조건).
- 동적으로 확인할 멈춤점 (`ntdll+rva`, test.exe 만): `NtMapViewOfSection` 반환 `eax`(dll1·dll2·kernel32 각각 `0x40000003` 인가 0 인가), `LdrpRelocateImage` 가 서나. Q20·Q23 과 같은 실행에서 볼 수 있으면 그렇다고.

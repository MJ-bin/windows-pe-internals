# 2026-11-15 run102 — 커널은 이미지 섹션을 만들 때 `.reloc` 를 바로 안 고친다 (Q62a = Q62 (a)·(b)·(f), 정적만): **`MiRelocateImage` 는 새 기준 주소를 골라 `_SEGMENT.BasedAddress` 에 적고, `.reloc` 를 "이 쪽은 이 블록" 표로만 만들고, `TransferAddress` 에 델타를 더한다 `[정적]`. 실제로 페이지 안 번지수를 고치는 함수는 `MiPerformFixups` (← `MiRelocateImagePfn`) — 페이지가 파일에서 들어올 때 (`MiValidateInPage +0x233`) 와, `MiRelocateImage` 끝에 그때 이미 올라와 있던 쪽만 (`MiWalkEntireImage +0x4cd`).** 헤더 쪽의 `ImageBase` 칸도 이 함수가 `BasedAddress` 쪽으로 옮긴다 (`MiPerformFixups +0x278` / `+0x37a`). DLL · ntdll 도 exe 와 같은 함수 사슬이고, 다른 건 기준 주소 고르는 방식뿐 (DLL · ntdll = 비트맵, 64비트 exe = `ExGenRandom` `0x7ff6…` 대역). **`NtMapViewOfSection` 이 `0` 이냐 `0x40000003` 이냐는 `MiMapViewOfImageSection +0x42a CMP` / `+0x42e JNZ → +0xc0a` 한 줄이 정한다** — 뷰 시작 (− `SectionOffset` 낮은 32비트) 이 `BasedAddress` 와 다르면 `0x40000003`. 헤더 `ImageBase` 와는 비교하지 않는다. `PspAllocateProcess +0x20ce` 는 "같은 델타 두 번 막기" 가 아니라 **기준이 다른 두 번째 보정** (붙은 주소 − `BasedAddress`) 이고, 같으면 `+0x20c9 JZ` 로 건너뛴다. 요청 여섯 함수는 `TransferAddress` 를 **안 쓴다** (모두 X). 더하는 줄이 하나 더 나왔다 (`MiSwitchBaseAddress +0x4e`).

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 값 대조는 run44 사진 덤프 읽기 로그를 다시 읽은 것뿐이다 (아래 "사진 로그 대조").
- 정적: **ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1`** (기준과 같음). 다시 analyze 안 함, `export` 만 여러 번 (결과서).
  - 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 그대로 (`_calls.tsv`).
- 다른 decompiler 가 같은 시간에 ntoskrnl (sast/106) · ntdll (sast/107) · sample dll (sast/105) 을 돌리는 중이다. **sast/99 ~ 107 결과는 이 문서에 안 넣었다.**

## 목적

- **Q62a** (= Q62 (a)·(b)·(f), Q62 가 커서 2026-11-15 에 Q62a · Q62b 로 나눔):
  - (f, 가장 중요) 커널이 이미지 안 번지수 (`.reloc`) 를 미리 고쳐 두나, 아니면 기준 주소만 정하고 페이지 들어올 때 고치나. DLL 이미지 섹션도 그 길을 지나나. `NtMapViewOfSection` 이 `0` / `0x40000003` 을 정하는 줄은 어디인가.
  - (b) `PspAllocateProcess +0x20b3 ~ +0x20ce` 보정과 `MiRelocateImage` 델타의 관계.
  - (a) 안 읽은 여섯 함수에도 `TransferAddress` 를 쓰는 줄이 있나.
- 앞 바퀴 짝: run31 (`MiCreateNewSection +0x3da MiRelocateImage` "새 기본 주소면 `TransferAddress` 에 델타"), run38 (ntdll `LdrpCompleteMapModule +0x31b LdrpRelocateImage` 는 맵 결과 정확히 `0x40000003` 일 때만), run72 · run73 (`MiMapViewOfImageSection +0xc0a`, `MiSelectImageBase` 비트맵), sast/11 (run16, "컨트롤 영역 `+0x20`").

## 방법

- 정적: `_workspace/sast/98_image-reloc.md` — ntoskrnl
  - `^MiRelocateImage$` depth 1, 그 아래 재배치 적용 함수 · `MiCreateNewSection` (`+0x3da` 앞뒤) · `MiMapViewOfImageSection` (status 줄만) · `PspAllocateProcess` (`+0x20b3 ~ +0x20ce` 만) · 질문 3 의 여섯 함수 depth 0.
  - 결과서가 "재배치 적용 함수 (이름 확인)" 줄과 "status 를 정하는 함수" 줄을 닫으려고 `MiRelocateImage` 지도 (`ms/disassemble/ntoskrnl/_map/MiRelocateImage_{edges,nodes}.tsv`, `_summary.txt`, 새로 만듦 9초) 로 한 줄기만 넓혀 뽑았다 — 새로 뽑은 것 21개 (결과서 머리).
  - 지도 깊이: `MiRelocateImage` → `MiWalkEntireImage` (1) → `MiRelocateImagePfn` (2) → `MiPerformFixups` (3) → `MiApplyRawFixups` · `MiApplyCompressedFixups` · `MiApplyStraddleFixups` · `MiApplyDynamicRelocationFixups` (4). 직접 부르는 곳 27개.
- 결과서: `_workspace/sast/98_image-reloc_result.md` (**분석성공** — 정적으로 못 닫은 것 둘: 섹션 만드는 순간 올라와 있는 쪽, `MiValidateInPage` 의 `MiCanPageMove` 조건).
- flow-writer 가 다시 본 것:
  - `.asm`: `MiMapViewOfImageSection` (`+0x353` · `+0x424` · `+0x427` · `+0x42a` · `+0x42e` · `+0x64c` · `+0xc0a`), `PspAllocateProcess` (`+0x98c` · `+0x991` · `+0x20a6 ~ +0x20d2` 열두 줄), `MiRelocateImage` (`+0xcc` · `+0xd6` · `+0x2e6` · `+0x2eb` · `+0x3aa` · `+0x4cb` · `+0x4d0` · `+0x575` · `+0x581` · `+0x6c6` · `+0x7ac` · `+0x7df` · `+0x7eb`, 조각 `FUN_140b24658 +0x44` · `+0x55` · `+0x65` · `+0x106` · `+0x143`), `MiValidateInPage` (`+0x14e` · `+0x152` · `+0x233` · `+0x238`), `MiWalkEntireImage` (`+0x496` · `+0x4cd` · `+0x4d2`), `MiRelocateImagePfn` (`+0x141` · `+0x146`), `MiPerformFixups` (`+0x131` · `+0x15f` · `+0x1b6` · `+0x1c4` · `+0x278` · `+0x2c5` · `+0x37a`), `MiSwitchBaseAddress` (`+0x46` · `+0x4e` · `+0x9a`), `MiSelectImageBase` (`+0x1df` · `+0x304` · `+0x30c` · `+0x338`), `MiMapProcessExecutable` (`+0xb3` · `+0xb8` · `+0xbd` · `+0xc5`), `PspMapSystemDll` (`+0x132` · `+0x195` · `+0x19e`), `MmCreateSpecialImageSection` (`+0x3f` · `+0x73` · `+0xc7`), `MiMapViewOfSection` 조각 `FUN_1408b5a43 +0x33` (`= MiMapViewOfSection +0x446`).
  - `_calls.tsv` (ntoskrnl): `MiCreateNewSection` 37줄 (`+0x3d5/+0x3da` · `+0x695/+0x69a MiRelocateImage` 두 자리), `MiRelocateImage` 36줄, `MmMapViewOfSectionEx` 1줄 (`+0x74/+0x79 MiMapViewOfSectionExCommon`), `MiMapViewOfSectionExCommon` (`+0x1c8/+0x1cd MiMapViewOfSection`), `NtMapViewOfSection` (`+0x248/+0x24d MiMapViewOfSection`).
  - `alias:` 두 곳: `MiRelocateImage +0x3aa` (메모리 복사 함수 — 같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`), 조각 `+0xc8` (세션 번호를 묻는 함수 — 같은 주소 이름 2개, 후보: `PsGetProcessSessionIdEx` · `PsGetSessionIdEx`).
  - 결과서 값과 다른 줄 없음.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!MiCreateNewSection` | `+0x3d5` | `+0x3da` | `nt!MiRelocateImage` | 새 컨트롤 영역 길. 여섯째 인자 비트 0 은 늘 0 (run73). 둘째 자리 `+0x695/+0x69a` | [정적] (run31 · run73 그대로) |
| `nt!MiRelocateImage` | `+0x2e6` | `+0x2eb` | `nt!MiSelectImageBase` | 새 기준 주소 고르기 (아래 "기준 주소") | [정적] (run73 그대로) |
| `nt!MiRelocateImage` | `+0x3aa` | `+0x3af` | 메모리 복사 함수 (같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`) | `.reloc` 바이트를 기록 풀 블록 뒤쪽에 복사 | [정적] |
| `nt!MiRelocateImage` | `+0x4cb` | `+0x4d0` | `nt!MiScanRelocationPage` | `.reloc` 블록마다 (고리 `+0x3ed ~ +0x4e6`). 종류 0 · 3 · `0xA` 만 받음, 다른 종류는 `0xC0000269` | [정적] |
| `nt!MiRelocateImage` 조각 `FUN_140b24658` | `+0x106` | `+0x10b` | `nt!MiWalkEntireImage(…, 4, …)` | `[R13+0x3e] & 0xc == 4` 일 때만 (= `MiRelocateImage +0x1b131a`) | [정적] |
| `nt!MiRelocateImage` 조각 `FUN_140b24658` | `+0x11d` | `+0x122` | `nt!MiWalkEntireImage(R13, 0, 2, 세션)` | `EBX & 0x188 == 0x100` 이 아니면 (이 길에선 늘) (= `+0x1b1331`) | [정적] |
| `nt!MiWalkEntireImage` | `+0x4cd` | `+0x4d2` | `nt!MiRelocateImagePfn` | **메모리에 이미 올라와 있는 쪽만** (`+0x496` 이 그 표시를 읽음, `TEST AL,2`) | [정적] |
| `nt!MiValidateInPage` | `+0x233` | `+0x238` | `nt!MiRelocateImagePfn` | **페이지가 파일에서 들어올 때**, 쪽마다. 재배치 기록 포인터 ≠ 널 (`+0x14e` · `+0x152`) 일 때 켜지는 비트로 거름 (조건 끝까지는 `[공백]`) | [정적] |
| `nt!MiRelocateImagePfn` | `+0x141` | `+0x146` | `nt!MiPerformFixups` | 쪽 번호 < 쪽 수 · 그 쪽 칸 ≠ 0 일 때 | [정적] |
| `nt!MiPerformFixups` | `+0x131` · `+0x2c5` · `+0x15f` | `+0x136` · `+0x2ca` · `+0x164` | `MiApplyCompressedFixups` · `MiApplyRawFixups` · `MiApplyStraddleFixups` | 실제로 번지수를 고치는 셋 (쪽 경계 항목은 `Straddle`) | [정적] |
| `nt!MiRelocateImageAgain` | `+0x17e` | `+0x183` | `nt!MiSwitchBaseAddress` | 같은 파일 컨트롤 영역이 살아 있고 먼저 고른 기준이 없을 때 (공유 길, Q62b 몫) | [정적] |
| `nt!MiSwitchBaseAddress` | `+0x9a` | `+0x9f` | `nt!MiWalkEntireImage(…, 2, …)` | 기준을 다시 바꿀 때 올라와 있는 쪽 고치기 | [정적] |
| `nt!MiMapProcessExecutable` | `+0xb3` | `+0xb8` | `nt!MmMapViewOfSectionEx` | exe 붙이기. 주소 NULL 로 부름. 반환이 음수일 때만 실패 (`+0xbd JS`) → `0x40000003` 도 통과 | [정적] |
| `nt!MmMapViewOfSectionEx` | `+0x74` | `+0x79` | `nt!MiMapViewOfSectionExCommon` | (flow-writer 보탬, `_calls.tsv` 한 줄) | [정적] |
| `nt!MiMapViewOfSectionExCommon` · `nt!NtMapViewOfSection` | `+0x1c8` · `+0x248` | `+0x1cd` · `+0x24d` | `nt!MiMapViewOfSection` | 둘 다 같은 함수로 모임 | [정적] |
| `nt!MiMapViewOfSection` 조각 `FUN_1408b5a43` | `+0x33` | `+0x38` | `nt!MiMapViewOfImageSection` | 컨트롤 영역이 이미지일 때 (`+0x2a7 TEST AL,0x20`) (= `MiMapViewOfSection +0x446`) | [정적] |
| `nt!PspAllocateProcess` | `+0x98c` | `+0x991` | `nt!MmGetSectionInformation(섹션, 4, [R15+0x38])` | 섹션 이미지 정보를 프로세스용 사본에 복사 (첫 8바이트 = `TransferAddress`) | [정적] (run16 그대로) |
| `nt!PspAllocateProcess` | `+0x20b3` | `+0x20b8` | `nt!MiSectionControlArea` | 아래 "보정" 표 | [정적] |

- exe 쪽 `MmMapViewOfSectionEx → MiMapViewOfSectionExCommon → MiMapViewOfSection → MiMapViewOfImageSection` 이 실제로 이 갈래로 이어지는지 (`MiMapViewOfSectionExCommon` 안 `+0xb6 MiMapViewOfSectionCommon` 과의 갈림) 는 안 봤다 `[정적: 호출 줄만]`. 결과서는 `NtMapViewOfSection` 쪽 길만 따라갔다.

### `MiRelocateImage` 가 하는 일 (결과서 1-1 요약)

| 순서 | 줄 | 하는 일 | 이미지 페이지를 쓰나 |
|---|---|---|---|
| ① 일찍 끝 (0 반환, 아무것도 안 바꿈) | `+0xcc ~ +0xd6` · `+0x647 ~ +0x64b` · `+0x130 ~ +0x6c0` | 세 경우: 세그먼트 `ForceCollision` (비트 14, IL-only .NET 이 켬) · x86/x64 인데 `RELOCS_STRIPPED` · x86/x64 인데 `DYNAMICBASE` 없음 (`MmRegistryState` 첫 칸 −1 이거나 AppContainer 면 계속) → `+0x6c6 XOR EAX,EAX` | 아니오 |
| ② 검사 | `+0xa1 ~ +0x15f` | 재배치 디렉터리 크기·범위. 둘 다 0 이면 "항목 없음" (오류 아님) | 아니오 |
| ③ 기록 풀 블록 | `+0x1a4 MiAllocatePool` (태그 `MmRe`) | 디렉터리 크기 + `0x68` + 쪽 수 × 8. 앞부분이 쪽마다 칸 | 아니오 |
| ④ 읽기 | `+0x1fc MiMapImageInSystemSpace` · `+0x24f MiParseImageLoadConfig` | 커널 주소에 맵해서 적재 구성 읽기 | 읽기만 |
| ⑤ 기준 주소 | `+0x2e6 MiSelectImageBase` | 새 기준. 기록 `+0x28` = 새 − 옛 `BasedAddress` (옛 값 = `+0x124` 가 읽은 세그먼트 `+0x20`) | 아니오 |
| ⑥ `.reloc` 걷기 | `+0x3aa` 복사 · 고리 `+0x3ed ~ +0x4e6` · `+0x4cb MiScanRelocationPage` | 블록 검사 뒤 **"이 쪽 = 이 블록" 을 쪽 칸에 적기만** (`+0x48a`). 종류가 낯설면 `0xC0000269` → `+0x7df ~ +0x7eb` 가 **0 으로 바꿔 끝냄** (⑧ 안 탐 = 이동 안 함) | 아니오 |
| ⑦ 헤더 쪽 표시 | `+0x575 ~ +0x581 OR [쪽 칸],1` | 헤더 `ImageBase` 칸이 든 쪽에 `1` (블록은 없고 그 칸만 고치라는 표시) | 아니오 |
| ⑧ 확정 (조각 `FUN_140b24658`, `+0x7ac JMP`) | 조각 `+0x44` / `+0x55` / `+0x65` (= `+0x1b1258` / `+0x1b1269` / `+0x1b1279`) | 이미지 부가 정보 `+0x20` = 기록 블록 → **`_SEGMENT.BasedAddress` = 새 기준** → **`TransferAddress += 델타`**. 이어 컨트롤 영역 비트 22 (`+0x1b127c`), 조건부 `SeSetImageBaseAddress` | 아니오 |
| ⑨ 올라와 있던 쪽 즉시 | 조각 `+0x106` · `+0x11d` `MiWalkEntireImage` | 이미 메모리에 있는 쪽만 `MiRelocateImagePfn` | **예** (그 쪽만) |
| ⑩ 표시 | 조각 `+0x143` (= `+0x1b1357`) `OR byte [R15+0x33],4` | `ImageFlags.ImageDynamicallyRelocated` | 아니오 |

- 기록 블록을 가리키는 칸 이름 (`_MI_IMAGE_ADDITIONAL_INFO +0x20 DynamicRelocations`) 은 오프셋이 맞아서 붙인 이름이다 `[추측: 이름만, 칸 쓰임은 [정적]]` (결과서 칸 이름 메모). 같은 칸을 `MiRelocateImagePfn +0x20/+0x3e` · `MiValidateInPage +0x14e` · `MiRelocateAmount` 가 읽는다.
- `MiCreateNewSection` 은 여섯째 인자 비트 0 을 늘 0 으로 준다 (run73) → ⑨ "건너뜀" 은 이 길에서 안 일어난다. 섹션을 막 만든 순간에 올라와 있는 쪽이 헤더 쪽뿐인지는 `[공백]`.

### 누가 · 언제 이미지 안 번지수를 고치나 (결과서 1-3)

| | exe (test.exe) | 보통 DLL (dll1 · dll2 · kernel32 …) | ntdll |
|---|---|---|---|
| 섹션 만드는 길 | `NtCreateUserProcess +0x93c MmCreateSpecialImageSection` → `MiCreateSection` | `NtCreateSection` → `MiCreateSectionCommon` → `MiCreateSection` (run73) | 부팅 `PspLocateSystemDll +0xf4 MmCreateSpecialImageSection` (run48) |
| 공통 안쪽 | `MiCreateImageOrDataSection +0x385 MiCreateNewSection +0x3da MiRelocateImage` (이미 있으면 `+0x1d5 MiShareExistingControlArea`, Q62b) | 같음 | 같음 |
| 기준 주소 | **임의** `MiSelectImageBase +0x30c ~ +0x338` (`ExGenRandom` → `0x7ff600000000` 부터 64KB 칸) | 비트맵 (run73) | 비트맵 |
| 이미지 안 번지수 | **커널 `MiPerformFixups`** — 페이지 들어올 때 + 올라와 있던 쪽 | 같음 | 같음 |
| 메모리 헤더 `ImageBase` | 커널이 `BasedAddress` 쪽으로 (`MiPerformFixups +0x278` 32비트 · `+0x37a` 64비트) | 같음 | 같음 (`PspMapSystemDll` 이 이 값을 기준 칸 `+0x20` 으로 읽음) |
| 진입점 `TransferAddress` | 커널 2번: `MiRelocateImage +0x1b1279` (섹션 사본) + `PspAllocateProcess +0x20ce` (프로세스 사본, 다를 때만) | 안 씀 (ntdll 이 헤더로 계산) | 안 씀 |
| 맵 결과 | `MiMapProcessExecutable +0xb8` — `0x40000003` 도 통과 | `NtMapViewOfSection` 아래 표 | `PspMapSystemDll +0x132 CMP EDI,0x40000003` → 시스템 프로세스면 통과, 아니면 `+0x19e` `0xc0000018` |
| ntdll 재배치 문 | `LdrpInitializeProcess +0x1430 LdrpProtectAndRelocateImage` (run38, 헤더 `ImageBase` ≠ `PEB.ImageBaseAddress` 일 때) | `LdrpCompleteMapModule +0x31b LdrpRelocateImage` — 맵 결과 정확히 `0x40000003` 일 때만 (run38) | 해당 없음 |

- exe 가 임의 갈래인 근거: exe 가 비트맵을 타려면 여섯째 인자 비트 1 이 필요한데, 그 출처 (패킷 비트 28 ← `MiInitializeCreateSectionPacket +0x9d` 의 `0x40`) 를 `MmCreateSpecialImageSection` 이 안 준다 (`+0x3f ~ +0x73` 이 만드는 값은 `{1, 8, 0x20}` 조합뿐, `+0xc7` 로 넘김) `[정적: 인자 따라가기]`.
- 샘플 헤더 (결과서가 임시로 읽음): `dll1.dll` · `dll2.dll` `DllCharacteristics 0x160`, `test.exe` `0x8160` (모두 `DYNAMICBASE`), 셋 다 `.reloc` 있음, `RELOCS_STRIPPED` 없음 → ① 일찍 끝에 안 걸린다 `[소스: 빌드 산출물 헤더]`.

### `NtMapViewOfSection` status 를 정하는 줄 (결과서 1-5)

| 값 | 때 | 줄 (`MiMapViewOfImageSection`) |
|---|---|---|
| **`0`** | 뷰 시작 − `SectionOffset` 낮은 32비트 == `_SEGMENT.BasedAddress` | 처음값 `+0x353` (0), 비교 `+0x424 ~ +0x42a CMP RCX,[RBP-0x60]`, `+0x42e JNZ` 안 탐, 반환 `+0x64c` |
| **`0x40000003`** | 위가 **다름** | `+0x42e JNZ → +0xc0a MOV [RSP+0x60],0x40000003` |
| `0x40000003` | 큰 페이지 이미지 뷰 마무리 | `MiMapViewOfSection` 조각 `FUN_1408b5a43 +0x132` |
| `0x40000036` | `0x40000003` 갈래에서 `BL` 비트 1 · 3 이 같이 켜짐 | `+0xc1b ~ +0xc82` (`BL` 출처 `[공백]`) |
| `0x4000000e` | 기계 종류 불일치 | 조각 `FUN_140b24c86 +0x6` |
| `0xc0000018` | 호출자가 준 범위가 차 있음 | `+0x40a MiIsVaRangeAvailable` → `+0xe72` |
| `0xc0000017` · `0xc000010a` | 범위 넘음 · 프로세스 플래그 `[R13+0x1f4] & 0x20` | `+0x6a2` · `+0xb5c` / `+0x3cc` |

- **주소를 안 줬을 때 `BasedAddress` 를 먼저 시도한다**: 시작 후보 = `BasedAddress + SectionOffset` (`+0x365 ~ +0x373`). 비면 그대로 비교 → `0`. 차 있으면 `+0x9ae MiSelectUserAddress` 로 다른 자리 → 비교에서 다름 → `0x40000003`. "주소를 줌" 비트 (`[R12+0x3c]` 비트 0) 의 뜻은 흐름으로 읽은 것 `[정적: 흐름]`.
- 이 줄이 run72 의 "`MiMapViewOfImageSection +0xc0a`" 와 같은 줄이다. 결과서가 그 앞 비교 줄 (`+0x42a`) 까지 닫았다.

### `PspAllocateProcess +0x20a6 ~ +0x20d2` 보정 (결과서 2)

| 줄 | 명령 | 뜻 |
|---|---|---|
| `+0x20a6` · `+0x20a9` | `TEST R15,R15` / `JZ → +0x20d2` | 이미지 정보 사본이 있을 때만 (복사 때 `+0x966` 과 같은 문턱) |
| `+0x20ab` · `+0x20ae` | `TEST BL,2` / `JNZ → +0x20d2` | 만들기 플래그 비트 11 · 13 · 14 에서만 켜지는 비트 (최소 프로세스 · 복제 갈래로 보임 `[추측: 이름]`). 평범한 exe 는 꺼짐 |
| `+0x20b3` (ret `+0x20b8`) | `MiSectionControlArea(섹션)` | 컨트롤 영역 |
| `+0x20b8` · `+0x20bb` | `MOV RCX,[RAX]` · `MOV RDX,[RCX+0x20]` | 컨트롤 영역 `+0x0` = `Segment` → **세그먼트 `+0x20` = `_SEGMENT.BasedAddress`** |
| `+0x20bf` | `MOV RAX,[R14+0x2b0]` | `EPROCESS.SectionBaseAddress` (붙은 주소) |
| `+0x20c6` · `+0x20c9` | `CMP RDX,RAX` / `JZ → +0x20d2` | **같으면 보정 없음** |
| `+0x20cb` · `+0x20ce` | `SUB` · `ADD [R15+0x38],RAX` | 사본 `TransferAddress += (붙은 주소 − BasedAddress)` |
| `+0x20d2` | `BTS EBX,7` | 어느 쪽이든 |

- 두 보정은 **이어 붙는다** (두 번 세지 않음): ① 섹션 만들 때 `MiRelocateImage +0x1b1279` 가 (새 − 옛 `BasedAddress`) 를 섹션 사본에, ② 프로세스 만들 때 `+0x20ce` 가 (붙은 주소 − `BasedAddress`) 를 프로세스 사본에. 합 = 붙은 주소 + 진입점 RVA. `MiMapProcessExecutable` 은 주소 NULL 로 불러 `BasedAddress` 를 먼저 시도하므로 보통은 같아서 ② 는 안 탄다 `[정적: 흐름]`.
- 한계 `[추측]`: `+0x20bb` 는 `BasedAddress` 를 복사 (`+0x98c`) 보다 **뒤에 다시** 읽는다. 그 사이 같은 컨트롤 영역에서 `MiSwitchBaseAddress` 가 돌면 사본과 어긋날 수 있다 (같은 파일을 동시에 여는 드문 경우).
- 이 보정은 `TransferAddress` 사본만 바꾼다. 헤더 페이지 · `.reloc` 는 `BasedAddress` 기준으로만 고쳐지므로 뷰가 다른 곳에 붙으면 나머지는 ntdll 몫이다.

### 여섯 함수 `TransferAddress` (`_SECTION_IMAGE_INFORMATION +0x0`) 쓰기 (결과서 3)

| 함수 | 쓰나 | 이미지 정보에 쓰는 것 |
|---|---|---|
| `MiParseComAndCetHeaders` | **X** | CET 비트 (`+0x4c`), `ImageFlags` (`+0x33`), 세그먼트 `ForceCollision` (`+0x3aa`) |
| `MiMakeImageReadOnly` | **X** | 서브섹션 플래그 · 프로토 PTE 만 |
| `MiInitializeNewImageSectionProtos` | **X** | 쪽 번호 배열 칸 −1 |
| `MiValidateSectionSigningPolicy` | **X** | 이유 번호 전역 하나 |
| `MiReleaseImageSection` | **X** | 둘째 인자 쪽 칸 |
| `MiFinalizeImageHeaderPage` | **X** | PFN 정리 |

- 여섯 밖에서 `TransferAddress` 를 더하는 줄 (이번에 읽은 범위): ① `MiBuildImageControlArea +0x172` (처음 값, run31) ② `MiRelocateImage +0x1b1279` ③ **`MiSwitchBaseAddress +0x4e ADD [RCX],RAX`** (새 `BasedAddress` 를 `+0x46` 에 쓰고 이번 델타를 더함 — 공유 길에서 기준을 다시 고를 때) ④ `PspAllocateProcess +0x20ce` (프로세스 사본). "이 넷뿐" 이라고는 못 한다 (포인터를 거쳐 쓰는 줄은 정적으로 다 못 찾음).

### 사진 로그 대조 (run44, 새로 돌린 것 아님 — flow-writer)

| 무엇 | 값 | 로그 줄 | 표시 |
|---|---|---|---|
| exe `_SEGMENT.BasedAddress` (`+0x20`) | `0x7ff61a370000` | `ms/logs/04_baggage-read_K1.txt:1433` | [26100.9457 확인] |
| exe `_EPROCESS.SectionBaseAddress` (`+0x2b0`) | `0x7ff61a370000` | `_K1.txt:145` | [26100.9457 확인] |
| exe 이미지 정보 `TransferAddress` | `0x7ff61a371d40` (= `BasedAddress` + `0x1d40`) | `_K1.txt:1447` | [26100.9457 확인] |
| exe `ImageFlags` | `0x4` (`ImageDynamicallyRelocated` 만) | `_K1.txt:1462` | [26100.9457 확인] |
| exe `ImageCharacteristics` · `DllCharacteristics` | `0x22` (`RELOCS_STRIPPED` 꺼짐) · `0x8160` | `_K1.txt:1458` · `1459` | [26100.9457 확인] |
| exe 세그먼트 `+0x8` qword (날 바이트) | `00270000`0000008c` → `+0xc` 낮은 word 0 (`ForceCollision` · `Binary32` 꺼짐) | `_K1.txt:1438` | [26100.9457 확인] |
| `PEB.ImageBaseAddress` | `0x7ff61a370000` | `_K1.txt:1560` | [26100.9457 확인] |
| exe 모듈 항목 `DllBase` · `EntryPoint` · `OriginalBase` (P1a) | `0x7ff61a370000` · `0x7ff61a371d40` · `0x7ff61a370000` | `ms/logs/04_baggage-read_P1a.txt` 2684 · 2685 · 2735 | [26100.9457 확인] |

- 읽는 법 (값 `[관찰]` + 길 `[정적]`):
  - `BasedAddress` 가 헤더 `0x140000000` 이 아니고 `0x7ff6…` 대역 (`0x7ff61a370000 − 0x7ff600000000 = 0x1a37 × 0x10000`, 한계 `0x20001` 안) 이다. `ImageFlags` 비트 2 도 켜져 있다 → 이 섹션에서 `MiRelocateImage` 가 ⑧ ~ ⑩ 까지 갔다 (이번 실행인지, 같은 부팅의 앞 실행인지는 Q61 — 공유 길이면 앞 실행의 값).
  - `TransferAddress` = `BasedAddress` + 진입점 RVA → ⑧ 의 델타가 더해진 값이다.
  - `SectionBaseAddress` == `BasedAddress` → `PspAllocateProcess +0x20c6 CMP` 가 같다 → `+0x20c9 JZ` 로 **`+0x20ce` 보정은 안 탔을 것** (값으로 읽은 갈래, 멈춤점으로 본 건 아님). 같은 이유로 exe 의 `MiMapViewOfImageSection +0x42a` 는 같음 → 맵 결과 `0` 일 것.
  - exe `OriginalBase` (= `LdrpProcessMappedModule +0x97` 이 메모리 헤더 `ImageBase` 로 덮는 칸, run72) 가 `0x7ff61a370000` 이다. 메모리 헤더 `ImageBase` 가 이미 붙은 주소였다는 뜻이고, `MiPerformFixups` 가 헤더 칸을 옮긴다는 정적과 맞는다. 다만 ntdll `+0x1430` 재배치가 헤더를 고쳐도 같은 값이 나올 수 있어 이 칸만으로 "ntdll 재배치 안 함" 을 가리지는 못한다 `[추측]` (동적 3).
- 이 값들은 run30 의 `0x7ff70ee10000` (run5 · run7, 다른 부팅) 과 다르다 — 부팅마다 고르는 임의 갈래와 어긋나지 않는다 `[추측: 두 값으로만]`.

### 호출 관계

섹션 만들 때 (부모 cmd 스레드, 그림 1):

```
nt!MiCreateNewSection
 └─ (3) +0x3da  MiRelocateImage                                         [정적] run31 · run102
      │  ① 일찍 끝 (ForceCollision · RELOCS_STRIPPED · DYNAMICBASE 없음) → 0, 아무것도 안 바꿈
      ├─ (1) +0x2e6  MiSelectImageBase       exe: ExGenRandom 0x7ff6… / DLL·ntdll: 비트맵 (run73)
      ├─ (2) +0x3aa  (메모리 복사)            .reloc → 기록 풀 블록 (MmRe)
      ├─ (3) +0x4cb  MiScanRelocationPage    블록마다: 쪽 칸에 블록 주소만 적음 (고치지 않음)
      │      +0x581  헤더 쪽 칸에 1            ImageBase 칸 표시
      └─ 조각 FUN_140b24658 (+0x7ac JMP)
            +0x44   부가 정보 +0x20 = 기록 블록
            +0x55   _SEGMENT.BasedAddress = 새 기준                     [정적] run73 · 값 [관찰] run44 K1
            +0x65   TransferAddress += 델타                             [정적] run31 · 값 [관찰] run44 K1
            ├─ +0x106 / +0x11d  MiWalkEntireImage
            │                    └─ +0x4cd  MiRelocateImagePfn          올라와 있던 쪽만
            │                                └─ +0x141  MiPerformFixups  ★ 실제 고치기 (+0x278/+0x37a 헤더 ImageBase)
            +0x143  ImageFlags |= 4 (ImageDynamicallyRelocated)         [정적] · 값 [관찰] run44 K1
```

페이지가 들어올 때 (어느 스레드든, 페이지 폴트 길 — 지도 대표 지점 밖):

```
nt!MiValidateInPage
 └─ +0x233  MiRelocateImagePfn        (부가 정보 +0x20 ≠ 널일 때 켜지는 비트로 거름, 조건 끝은 [공백])
      └─ +0x141  MiPerformFixups
           ├─ +0x131  MiApplyCompressedFixups
           ├─ +0x2c5  MiApplyRawFixups
           └─ +0x15f  MiApplyStraddleFixups
```

뷰 붙일 때 (status):

```
nt!NtMapViewOfSection +0x248 · nt!MiMapViewOfSectionExCommon +0x1c8
 └─ MiMapViewOfSection  조각 +0x33 (= +0x446)
      └─ MiMapViewOfImageSection
           +0x365 ~ +0x373  후보 = BasedAddress + SectionOffset (주소 안 줬을 때)
           +0x42a CMP (뷰 시작 − SectionOffset 낮은 32비트) vs BasedAddress
           +0x42e JNZ → +0xc0a  0x40000003      / 안 탐 → 0
```

## 확인된 것

- `[정적]` **`MiRelocateImage` 는 섹션을 만들 때 이미지 페이지 안의 `.reloc` 를 (거의) 적용하지 않는다.** 하는 일은 새 기준 고르기 · `.reloc` 를 쪽별 표로 만들기 · `BasedAddress` 쓰기 · `TransferAddress` 에 델타 더하기 · `ImageDynamicallyRelocated` 켜기. 예외는 끝의 `MiWalkEntireImage` — **그때 이미 메모리에 있던 쪽만** 곧바로 고친다.
- `[정적]` **실제로 번지수를 고치는 함수 = `MiPerformFixups`** (← `MiRelocateImagePfn`). 부르는 때는 둘: 페이지가 파일에서 들어올 때 (`MiValidateInPage +0x233`), 섹션을 막 만들었을 때 올라와 있던 쪽 (`MiWalkEntireImage +0x4cd`). 기준을 다시 바꿀 때 (`MiSwitchBaseAddress +0x9a`) 도 같은 길.
- `[정적]` **헤더 쪽 `ImageBase` 칸도 커널이 고친다**: `MiRelocateImage +0x581` 이 그 쪽에 표시 `1`, `MiPerformFixups +0x1b6 ~ +0x1c4` 가 그 쪽이면 `+0x278` (32비트) / `+0x37a` (64비트) 로 `BasedAddress` 쪽으로 옮긴다. run73 · flow.md 2.6 의 "커널이 메모리 헤더 `ImageBase` 를 고치는 줄은 못 찾았다 `[공백]`" 이 닫힌다.
  - 덤 증거 (결과서): `PspMapSystemDll` 이 ntdll 의 기준 칸 `+0x20` 을 **메모리 헤더** `ImageBase` 에서 읽는다 (`RtlImageNtHeader` 뒤 `[RAX+0x30]`) — 헤더가 이미 옮겨져 있어야 run48 `+0x172` 검사와 맞는다.
- `[정적]` **DLL · ntdll 도 exe 와 같은 함수 사슬** (`MiCreateImageOrDataSection → MiCreateNewSection → MiRelocateImage`). 다른 건 `MiSelectImageBase` 갈래뿐 — DLL · ntdll 비트맵, 64비트 exe 임의 (`ExGenRandom`, `+0x30c ~ +0x338`). exe 가 임의 갈래라는 건 run73 까지 `[추측]` 이었고 이번에 인자 따라가기로 `[정적]`.
- `[정적]` **맵 결과 `0` / `0x40000003` = `MiMapViewOfImageSection +0x42a` 한 줄** (뷰 시작 − `SectionOffset` vs `BasedAddress`). 헤더 `ImageBase` 와는 비교하지 않는다. 그래서 "커널이 고침 → `0`" 이고, ntdll `LdrpRelocateImage` 문은 **뷰가 `BasedAddress` 말고 다른 곳에 붙었을 때만** 탄다.
  - `/DYNAMICBASE` 없는 x86/x64 이미지는 `MiRelocateImage` 가 일찍 끝나 `BasedAddress` = 헤더 `ImageBase` 그대로 → 그 자리가 비면 `0`, 차면 `0x40000003` (→ ntdll 이 `.reloc` 로 직접).
- `[정적]` **`PspAllocateProcess +0x20ce` = 두 번째 보정** (붙은 주소 − `BasedAddress`), 같으면 `+0x20c9 JZ`. 델타 두 번 막기가 아니라 기준 칸이 이어져서 두 번 안 센다.
  - `[관찰]` 값 + `[정적]` 길: run44 K1 에서 `SectionBaseAddress` == `BasedAddress` == `0x7ff61a370000` → test.exe 는 `+0x20ce` 를 안 탔을 것, `TransferAddress` `0x7ff61a371d40` 은 `MiRelocateImage` 델타로 이미 맞음.
- `[정적]` 요청 여섯 함수는 **모두 X**. 더하는 줄 새로 하나: `MiSwitchBaseAddress +0x4e` (공유 길 재선택 때).
- `[정적]` `.reloc` 항목 종류가 낯설면 (`0` · `3` · `0xA` 밖) `MiRelocateImage` 는 오류 없이 **0 으로 끝내고 이동을 안 한다** (`+0x7df ~ +0x7eb`, ⑧ 안 탐).

## 결과서와 다르게 적은 곳 · 보탠 것

- 다르게 적은 곳: 없음.
- 보탠 것 (flow-writer):
  - run44 사진 로그 대조 표 (위). 결과서는 run30 의 `0x7ff70ee10000` 만 썼고, 사진 K1 의 `BasedAddress` · `SectionBaseAddress` · `TransferAddress` · `ImageFlags` 는 안 읽었다.
  - exe 맵 길 `MmMapViewOfSectionEx +0x74 → MiMapViewOfSectionExCommon` (`_calls.tsv` 한 줄). 그 안 갈림은 안 봄.

## 정정

- **sast/11 결과서 (run16) "컨트롤 영역 `+0x20` 칸 (선호 주소)"** → 바르게는 **컨트롤 영역 `+0x0` (`Segment`) → 세그먼트 `+0x20` = `_SEGMENT.BasedAddress`** (`PspAllocateProcess +0x20b8 MOV RCX,[RAX]` · `+0x20bb MOV RDX,[RCX+0x20]`, PDB). 컨트롤 영역 `+0x20` 은 `NumberOfPfnReferences` 다. 값도 "헤더의 선호 주소" 가 아니라 **`MiSelectImageBase` 가 고른 기준** (처음엔 헤더 값, `MiRelocateImage` 가 덮음).
  - 그 표현은 sast/11 결과서에만 있고 run16 문서 · flow.md 에는 안 옮겨져 있다 (run16 은 "차이만큼 더함" 만). 그래서 이전 run 문서 맨 위 정정 줄은 넣지 않았다. 결과서 파일은 고치지 않는다.
- 정정이라기보다 보탬: run31 · flow.md 의 "`MiRelocateImage` 가 새 기본 주소면 `TransferAddress` 에 델타" 는 그대로 맞다. 다만 그 함수의 본업은 기준 고르기와 `.reloc` 표 만들기이고, 페이지 고치기는 나중 (`MiPerformFixups`) 이라는 걸 flow.md 글에 보탰다.

## 이전 run 에서 닫은 것

- run31 "아직 확인 필요" Q62 (a) · (b) — 닫힘 (Q62a). (c) · (d) · (e) 는 Q62b.
- run38 explainer 질문 (2026-10-24 결정으로 Q62 (f)) "DLL 이미지도 커널 `MiRelocateImage` 를 지나나, 이미지 안 번지수 전체를 고치나" — 닫힘.
- run73 · flow.md 2.6 "커널이 메모리 헤더 `ImageBase` 를 고치는 줄 `[공백]`" — 닫힘 (`MiPerformFixups +0x278` / `+0x37a`).
- flow.md 2.1 `MiRelocateImage` 절 `[추측]` "test.exe 는 이 길을 탈 것" — `[정적]` (헤더 조건) + 사진 값 `[관찰]` 로 올림. "이 조각에서 더하는지, `+0x20b3 ~ +0x20ce` 에서 보정하는지" — 둘 다이고 이어 붙음 `[정적]`.

## 아직 확인 필요

### 동적 (`[VM]`, 결과서 "동적으로 확인해야 할 것")

멈춤점은 모두 커널이라 test.exe 조건을 붙인다. 주소는 이 빌드 rva 기준 (`nt+…`).

1. **맵 결과와 `BasedAddress` 비교** — `nt+0x97991a` (`MiMapViewOfImageSection +0x42e`) 에서 `rcx` 와 `poi(@rbp-0x60)` 를 같이, `+0xc0a` (`nt+0x97a0f6`) 를 지나나. 대상 test.exe 의 exe · dll1 · dll2 · kernel32. (run72 · run73 동적 1 과 같은 질문, Q104)
2. **누가 `.reloc` 를 실제로 적용하나** — `nt+0x92aca8` (`MiRelocateImagePfn` 입구) 콜스택 caller 가 `MiWalkEntireImage+0x4d2` (`nt+0x2dc712`) 인가 `MiValidateInPage+0x238` (`nt+0x372684`) 인가 (+ `MiPrivateFixup`), DLL 마다 몇 번. `MiPerformFixups +0x278/+0x37a` (`nt+0x92b26c` / `nt+0x92b36e`) 가 헤더 쪽에서 도나.
3. **exe 헤더 `ImageBase` 와 ntdll 재배치** — `ntdll+0x8d834` (`LdrpInitializeProcess +0x1430`) 가 안 서나, 그 앞 `+0x1419` 에서 `[NT+0x30]` 값. (run38 · Q61 과 같은 실행)
4. **섹션 사본 `TransferAddress` 가 더해지는 순간** — `nt+0xb246bd` (`MiRelocateImage` 조각 `+0x65`) 에서 `poi(@r15)` · `rax`, `nt+0xb246ad` (`+0x55`) 의 `rax`. 같은 부팅 첫 실행이면 서야 하고, 공유 길이면 안 선다 (Q61).
5. **`PspAllocateProcess +0x20c6`** (`nt+0xa0462e`) 의 `rdx` · `rax` — 같아서 `+0x20c9 JZ` 를 타나 (사진 값으로는 같음, 기대: 탐).
6. exe 기준 주소 갈래 — `MiSelectImageBase +0x304` (`nt+0x9730e4`) 를 test.exe 에서 지나나.

### 정적 (결과서 "다음 요청 후보" + 보탬)

1. **Q62b (이미 대기열)**: 공유 길 `MiShareExistingControlArea → MiValidateExistingImage`, 프로토 PTE, `MiVerifyImageHeader` 이유 코드. 이번에 `MiRelocateImageAgain → MiSwitchBaseAddress` 까지 닿아서 이어 읽기 좋다.
2. ntdll 반대편: `LdrpRelocateImage` · `LdrpProtectAndRelocateImage` 본문 — 메모리 헤더 `ImageBase` (커널이 옮긴 값) 로 델타를 어떻게 정하나, `.reloc` 없는 이미지에서 무엇을 하나, `LdrRelocateImageWithBias` 가 헤더 `ImageBase` 를 고치나 (위 사진 대조의 `[추측]` 을 닫음).
3. 작은 것: `MiCanPageMove` + `MiValidateInPage +0x14e ~ +0x351` 비트 3 · 5 조건 정확히 (평범한 DLL 이 page-in 에서 `MiRelocateImagePfn` 을 타나), `MiPrivateFixup +0x340` 의 쓰임.
4. 작은 것: `MiSelectImageBase` 의 `ESI` 갈래 (1 · 2 · 3 · 4 — 4GB 아래 · WOW) 와 `MiBuildImageControlArea +0x25e/+0x261` (`BasedAddress` 처음 값) 을 이번 길과 한 장으로.
5. `0xC0000269` 로 끝났을 때 `MiSelectImageBase` 가 켜 둔 비트맵 칸이 어떻게 되나 (`MiReturnImageBase` 류를 안 부르는 것으로 읽힘 `[추측]`).
6. `MiMapViewOfImageSection` 의 `BL` 비트 1 · 3 (`0x40000036`) 과 `[R12+0x3c]` 비트 0 의 이름 · 출처 (run72 부터 `[공백]`).
7. (flow-writer 보탬) `MiMapViewOfSectionExCommon` 이 exe 길 (`MmMapViewOfSectionEx`) 에서 `+0x1c8 MiMapViewOfSection` 쪽으로 가는지 (`+0xb6 MiMapViewOfSectionCommon` 과의 갈림).

### PE_emulator 쪽 (결과서 1-6 · 2 · 3 요약 — 고칠지는 사용자 결정)

- DLL 길 결과 값은 맞다 (같은 델타, 같은 헤더 칸). 때만 다르다 (진짜는 page-in, 우리는 뷰 붙일 때 한 번).
- `.reloc` 없는 DLL: 우리는 실패, 진짜는 오류 아님 (헤더 칸만 고치는 길). 샘플엔 해당 없음 (`확인 필요`).
- 낯선 재배치 종류: 우리는 `STATUS_NOT_SUPPORTED`, 진짜는 0 으로 끝내고 이동 안 함. 샘플엔 해당 없음.
- exe · ntdll 기준 주소: 우리는 헤더 값 (D8 · 12 결정으로 막아 둠), 진짜는 exe 임의 · ntdll 비트맵.
- 진입점 보정 둘: 우리는 앞의 것만 있고 exe 가 늘 `BasedAddress` 에 붙으므로 지금은 맞다.

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntkrnlmp 줄에 run102, 근거 로그 (run102) 한 줄), 그림 1 (새 노드 `i7` `nt!MiRelocateImage` — `i3 -->|"(3) +0x3da"| i7`, `i3 → i4` 를 `(1) +0x215` 로 번호, `i3` · `p4` · `i2` 글 다듬음), 2.1 (절 머리 · 트리 `(3) +0x3da` · `(1) +0xb8` 줄, `MiCreateNewSection` · `MiRelocateImage` 절 (본문 bullet · `[추측]` 두 곳 정리) · 진입점 흐름 (`+0x20ce` 줄 더함) · `PspAllocateProcess` 보정 bullet · `PspCreateUserContext` `Rcx` 줄 · `MiMapProcessExecutable` · `PspMapSystemDll`), 2.4 (exe 재배치 `+0x1430` 줄), 2.6 (절 머리 · run72 커널 쪽 조건 아래 · run44 P1b `OriginalBase` 아래 · 붙이기 · 재배치 절 "커널 재배치와의 관계" `[공백]` → (해결됨)), 4장 (2.1 Q61 · Q35 · Q62 줄, 2.5 · 2.6 Q20 · Q23 줄, run101 두 줄 대기열 번호, run102 새 줄 셋), 4.1 (그림 1 nt 섹션 줄, 그림 2 회색 상자 줄 번호 (12)·(15) → (13)·(16)), 5장 (run102 · sast/98).
- baggage.md: 기준 run102, 3장 머리 메모, 3.21 `ImageActive` 경로 표기, 3.22 `BasedAddress` 둘째 줄 + 새 조건부 줄 (`MiSwitchBaseAddress +0x46`), 3.23 머리 메모 (프로세스 사본) · `TransferAddress` 둘째 줄 + 새 조건부 줄 (`+0x4e`) · `ImageFlags` 두 줄 표기, 7.6 메모 둘, 새 7.34, 8장. **새 짐 칸 없음, 2장 숫자 그대로** (3.22 줄 13 → 14, 3.23 줄 26 → 27).

## 디버거 메모 / 도구 메모

- Ghidra 주의 (결과서): `MiValidateInPage.c` 는 `!bVar20 ||` 로 `MiCanPageMove` 를 건너뛰는 것처럼 묶었는데 `.asm` 은 늘 부르는 흐름 — `.asm` 을 믿었다. 새 함정이라기보다 "`.c` 의 짧은 평가 묶음은 `.asm` 으로 확인" 이라 pitfalls 제안은 안 한다.
- `MiRelocateImage` 의 확정 부분은 Ghidra 가 조각 `FUN_140b24658` 로 떼어 놓았다. 오프셋은 조각 기준 (`+0x55`) 과 함수 기준 (`+0x1b1269`) 이 둘 다 쓰인다 — 멈춤점 주소는 `nt+0xb246ad` (run73 에서 고친 표기 그대로).

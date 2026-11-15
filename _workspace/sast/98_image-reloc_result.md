# 98 image-reloc — 정적 분석 결과

- 요청서: `_workspace/sast/98_image-reloc.md` (Q62a = Q62 (a)·(b)·(f))
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID와 같음). **analyze 다시 안 함** (`.gpr` · `_index.tsv` · analyze 로그 `SetPdb:` 줄 모두 있음, 도는 Ghidra 없음).
- 새로 뽑은 것 (depth 0, `export` 여러 번): `MiRelocateImagePfn` · `MiScanRelocationPage` · `MiValidateSectionSigningPolicy` · `MiReleaseImageSection` · `MiWalkEntireImage` · `MiPerformFixups` · `MiApplyRawFixups` · `MiApplyCompressedFixups` · `MiValidateInPage` · `MiSwitchBaseAddress` · `MiSectionControlArea` · `MmGetSectionInformation` · `MiRelocateAmount` · `MiLegacyImageArchitecture` · `NtMapViewOfSection` · `NtMapViewOfSectionEx` · `MmMapViewOfSectionEx` · `MiMapViewOfSectionExCommon` · `MiMapViewOfSectionCommon` · `MiMapProcessExecutable` · `PspMapSystemDll`. 뽑았지만 안 읽음: `MiCreateFileOnlyImageFixupList` · `MiCreateFixupRecord` · `MiPageHasRelocations` · `MiRevertRelocatedImagePfn` · `MiApplyDynamicRelocations` · `MiCompressRelocations` · `MiPrivateFixup` · `MiWalkEntireImagePageNeedsProcessing`.
  - 범위를 넓힌 이유: 요청서 표의 "재배치 적용 함수 (이름 확인)" 줄과 "status 를 정하는 함수" 줄을 닫으려고 `MiRelocateImage` 지도 (`_map/MiRelocateImage_*`, 아래) 로 길을 찾아 그 한 줄기만 뽑았다.
- 있던 것 그대로 읽음: `MiRelocateImage` · `MiCreateNewSection` · `MiMapViewOfImageSection` · `MiMapViewOfSection` · `MiSelectImageBase` · `MiRelocateImageAgain` · `PspAllocateProcess` · `MiParseComAndCetHeaders` · `MiMakeImageReadOnly` · `MiInitializeNewImageSectionProtos` · `MiFinalizeImageHeaderPage` · `MiInitializeCreateSectionPacket` · `MiCreateSection` · `MmCreateSpecialImageSection`.
- 지도: `ms/disassemble/ntoskrnl/_map/MiRelocateImage_{edges,nodes}.tsv`, `_summary.txt` (이 길 지도가 없어서 새로 만듦, 9초). 깊이: `MiRelocateImage` → `MiWalkEntireImage` (1) → `MiRelocateImagePfn` (2) → `MiPerformFixups` (3) → `MiApplyRawFixups` · `MiApplyCompressedFixups` · `MiApplyStraddleFixups` · `MiApplyDynamicRelocationFixups` (4). 직접 부르는 곳 27개 (`_edges.tsv` 깊이 1).
- 버전 확인: `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv`).
- 스크립트 문제: 없음.
- 칸 이름 메모: `_CONTROL_AREA +0x60` 은 공개 PDB 에서 이름 없는 합집합 (`u2`, `+0x58`) 안이다. `& -8` 한 값이 가리키는 곳의 `+0x20` 을 재배치 기록 블록으로 읽는 줄이 `MiRelocateImage` 조각 `+0x44`, `MiRelocateImagePfn +0x20/+0x3e`, `MiRelocateAmount +0x0 ~ +0xc`, `MiValidateInPage +0x14e` 에서 모두 같아서, PDB 형 `_MI_IMAGE_INFO_REF` / `_MI_IMAGE_ADDITIONAL_INFO +0x20 DynamicRelocations` (void*) 와 오프셋이 맞는다고 보고 그 이름으로 부른다 `[추측: 이름만, 칸 쓰임은 [정적]]`. `MiApplyDynamicRelocations` 는 이름만 비슷하고 이 길이 아니다 (부르는 곳 `MiRebaseDynamicRelocationRegions` 뿐, 지도에 없음).

## 한 줄 결론 (PE_emulator 용)

- **`MiRelocateImage` 는 이미지 페이지 안의 `.reloc` 항목을 (거의) 안 적용한다.** `.reloc` 바이트를 풀 블록으로 베끼고 검사하고, 페이지마다 "어느 블록이 이 페이지 몫인가" 표를 만들고, 새 기준 주소를 `_SEGMENT.BasedAddress` 에 쓰고, `TransferAddress += 델타` 를 한다. 실제로 페이지 안 번지수를 고치는 함수는 `MiPerformFixups` (← `MiRelocateImagePfn`) 이고, 부르는 때는 둘이다: (1) **페이지가 파일에서 들어올 때** `MiValidateInPage +0x233` (조건 있음, 아래 1-2), (2) `MiRelocateImage` 끝에서 **그때 이미 메모리에 올라와 있는 페이지** 만 `MiWalkEntireImage` 로 곧바로 `[정적]`.
- DLL 이미지 섹션도 exe 와 **같은 함수 사슬** (`MiCreateImageOrDataSection → MiCreateNewSection → MiRelocateImage`) 을 지난다. ntdll 도 같다 (부팅 때 `PspLocateSystemDll → MmCreateSpecialImageSection`). 차이는 기준 주소 고르는 방식뿐: DLL·ntdll = 비트맵, 64비트 exe = `ExGenRandom` 임의 `0x7ff6…` 대역 `[정적]`.
- `NtMapViewOfSection` 이 `0` 이냐 `0x40000003` 이냐는 **딱 한 줄**이 정한다: `MiMapViewOfImageSection +0x42a CMP` / `+0x42e JNZ → +0xc0a MOV [RSP+0x60],0x40000003`. (뷰 시작 주소 − SectionOffset 낮은 32비트) 가 `_SEGMENT.BasedAddress` 와 **다르면** `0x40000003`, 같으면 `0`. 헤더 `ImageBase` 와는 비교하지 않는다 `[정적]`.
- 그래서 "커널이 고침 → 0" 이다. ntdll `LdrpRelocateImage` 문 (run38: 정확히 `0x40000003` 일 때만) 은 **뷰가 `BasedAddress` 말고 다른 곳에 붙었을 때** 만 탄다. `/DYNAMICBASE` 없는 x86/x64 이미지는 `MiRelocateImage` 가 일찍 끝내서 `BasedAddress` 가 헤더 `ImageBase` 그대로 → 그 자리가 비면 `0`, 차면 `0x40000003`.
- 질문 2: `PspAllocateProcess +0x20ce` 보정은 **같은 델타를 막는 가드가 아니라 다른 기준의 두 번째 보정**이다. `MiRelocateImage` 는 (새 `BasedAddress` − 옛 `BasedAddress`) 를 섹션 사본에, `PspAllocateProcess` 는 (실제 `SectionBaseAddress` − `BasedAddress`) 를 프로세스 사본에 더한다. 둘이 이어져서 두 번 세지 않고, 실제 주소가 `BasedAddress` 와 같으면 `+0x20c9 JZ` 로 건너뛴다.
- 질문 3: 요청서의 여섯 함수 **전부 X** — `TransferAddress` (`+0x0`) 를 쓰는 줄 없음. 여섯 밖에서 더하는 줄은 `MiSwitchBaseAddress +0x4e` 하나가 새로 나왔다.

## 질문별 답 [정적]

### 1. (f) 커널이 이미지 안 번지수를 미리 고쳐 두나

**답: 고치는 일은 한다. 다만 "새 기준 주소와 기록만 정하고, 고치기는 페이지 들어올 때 (+ 올라와 있던 쪽만 즉시)" 이다.** 이름: `MiRelocateImagePfn` → `MiPerformFixups`.

#### 1-1. `MiRelocateImage` 가 하는 일 (줄 순서, `.asm` 오프셋)

| 순서 | 줄 | 하는 일 | 이미지 페이지를 쓰나 |
|---|---|---|---|
| ① 일찍 끝 (0 반환, 아무것도 안 바꿈) | `+0xcc ~ +0xd6` (`BT AX,0xe` · `JC → +0x6c6`) | 세그먼트 플래그 비트 14 (PDB `ForceCollision`) 가 켜져 있으면 끝. 그 비트는 `MiParseComAndCetHeaders +0x3aa OR word [RDI+0xc],0x4000` 이 켠다 (.NET 헤더 플래그 비트 0 = IL-only 일 때로 읽힘) | 아니오 |
| | `+0x8e/+0x93 MiLegacyImageArchitecture` (`+0x95 JNZ → +0x644`) 가 x86 (`0x14c`) · x64 (`0x8664`) 면 1 → `+0x644 OR EBX,0x40`, `+0x647 TEST [R8+0x2c],1 / +0x64b JNZ → +0x6c6` (파일 특성 `RELOCS_STRIPPED`) | 끝 | 아니오 |
| | `+0x130 TEST BL,0x40 / +0x133 JNZ → +0x693`: 넷째 인자 0, `DllCharacteristics` (`[R8+0x2e]`) 에 `0x40` (DYNAMICBASE) 없음 (`+0x6a3/+0x6a7`), `MmRegistryState` 첫 칸 ≠ −1 (`+0x6ad/+0x6b4`), `0x1000` (AppContainer) 없음 (`+0x6ba/+0x6c0`) → 모두 만족하면 `+0x6c6 XOR EAX,EAX` 로 끝 | 끝 | 아니오 |
| ② 검사 | `+0xa1 ~ +0xc6`, `+0x142 ~ +0x15f` | 재배치 디렉터리 (`[R14+0x58]` RVA, `[R14+0x5c]` 크기로 읽힘): 둘 다 0 이면 "항목 없음" (`+0x67d AND EBX,~2`), 하나만 0 이면 오류 (`+0x66c ~ +0x677 → +0x709`), 범위가 이미지를 넘어도 오류 (`+0x156 CMP / +0x159 JA → +0x709`) | 아니오 |
| ③ 기록 풀 블록 | `+0x1a4 MiAllocatePool(0x100, 크기, 0x65526d4d)` (태그 `MmRe`) | 크기 = 디렉터리 크기 + `0x68` + 페이지 수 × 8 (`+0x167 ~ +0x172`). 앞부분은 페이지별 칸 (`[R14+0x68+페이지×8]`) | 아니오 |
| ④ 읽기 | `+0x1fc MiMapImageInSystemSpace` · `+0x24f MiParseImageLoadConfig` | 이미지를 커널 주소에 맵해서 적재 구성 읽기 | 읽기만 |
| ⑤ 기준 주소 | `+0x2e6/+0x2eb MiSelectImageBase`, `+0x301/+0x306 MiUpdateImageSystemWideBitmaps` | 새 기준 주소 고르기 (아래 1-4). `[R14+0x28] = 새 − 옛 BasedAddress` (`+0x32a ~ +0x33a`, 옛 값은 `+0x124 MOV RDX,[R15+0x20]`) | 아니오 |
| ⑥ `.reloc` 걷기 | `+0x3aa memcpy` (`.reloc` 바이트를 풀 블록 뒤쪽에 복사), 고리 `+0x3ed ~ +0x4e6` (`+0x4e6 JMP → +0x3ed`) | 블록마다 길이 · 짝수 · 쪽 정렬 · 순서 검사. 맞으면 `+0x48a OR [R14+쪽×8+0x68],R8` 로 **이 쪽의 블록 주소를 칸에 적고**, `+0x4cb/+0x4d0 MiScanRelocationPage` 부름 | 아니오 |
| | `MiScanRelocationPage` | 항목 종류 0 (건너뜀) · 3 (HIGHLOW) · `0xA` (DIR64) 만 받음. 쪽 경계를 걸치는 항목만 `+0x12e MiCreateFixupRecord`. 다른 종류는 `0xC0000269` (`+0xdb`) | 아니오 |
| | `+0x7df ~ +0x7eb` | `0xC0000269` 는 **오류로 안 돌려주고 0 으로 바꿔서** 끝낸다 → 이 경우 ⑧ 의 `BasedAddress`·`TransferAddress` 갱신 조각 (`+0x1b1269`) 은 안 탄다 | 아니오 |
| ⑦ 정리 | `+0x80c MiCompressRelocations` (`EBX & 0x14 == 0x14`), **`+0x575 ~ +0x581 OR [RDX+쪽×8],1`** | 헤더 쪽 (`[R14+0x3c] >> 12`, = 헤더 `ImageBase` 칸이 든 쪽) 칸에 `1` 을 적는다 (블록은 없고 `ImageBase` 칸만 고치라는 표시) | 아니오 |
| ⑧ 확정 (조각 `FUN_140b24658`, `+0x7ac JMP`; 아래 `+0x…` 는 조각 시작 기준, 괄호는 `MiRelocateImage` 기준) | 조각 `+0x37` (`+0x1b124b`) `MI_LOCK_RELOCATIONS_EXCLUSIVE` → **`+0x44` (`+0x1b1258`) `[이미지 정보 +0x20] = 풀 블록`** → **`+0x55` (`+0x1b1269`) `[세그먼트 +0x20] = BasedAddress`** → **`+0x65` (`+0x1b1279`) `ADD [R15],RAX` (`TransferAddress += [R14+0x28]`)** → `+0x68` (`+0x1b127c`) 컨트롤 영역 `+0x5c` 비트 22 켬 → `+0xa6` (`+0x1b12ba`) `SeSetImageBaseAddress` (`MiFlags & 0x2000` 이고 `[R13+0x5c] & 0xc0000` 일 때) | 아니오 (쓰기는 ⑨ 에서) |
| ⑨ 올라와 있던 쪽 즉시 | 조각 `+0xd6 ~ +0xe6` (`+0x1b12ea ~ +0x1b12fa`): `EBX & 0x188 == 0x100` 이면 건너뜀 — 곧 비트 8 (= **여섯째 인자 비트 0 = 고정 기준**, `+0x74 ~ +0x77`) 만 켜져 있고 비트 3 (적재 구성에 `[R14+0x58]` 있음, `+0x371 ~ +0x376`, `+0x783 ~ +0x787`) · 비트 7 (`[R14+0x60]` 있음, `+0x37e ~ +0x387`) 이 꺼진 때. 아니면 조각 `+0x106` (`+0x1b131a`) `MiWalkEntireImage(…, 4, …)` (`[R13+0x3e] & 0xc == 4` 일 때만, 조각 `+0xe8 ~ +0xf0`), 조각 `+0x11d` (`+0x1b1331`) `MiWalkEntireImage(R13, 0, 2, 세션)` | 메모리에 **이미 올라와 있는** 이미지 페이지만 `MiWalkEntireImage +0x4cd MiRelocateImagePfn` (ret `+0x4d2`; 조건 `+0x496 TEST AL,2`) | **예** (그 쪽만) |
| ⑩ 표시 | 조각 `+0x139 ~ +0x143` (`+0x1b134d ~ +0x1b1357`) | 여섯째 인자 비트 0 이 꺼져 있으면 `ImageFlags |= 4` (PDB `ImageDynamicallyRelocated`) | 아니오 |

- `MiCreateNewSection` 이 여섯째 인자 비트 0 을 늘 0 으로 준다 (run73) → ⑨ 의 "건너뜀" 은 이 길에선 안 일어난다. 그래서 `MiRelocateImage` 가 끝날 때 **올라와 있던 쪽은 곧바로 고친다** `[정적]`. 섹션을 막 만든 순간에 올라와 있는 쪽이 헤더 쪽뿐인지는 정적으로 못 정한다 `[공백]`.

#### 1-2. 실제로 페이지 안을 고치는 길

| 길 | 줄 | 하는 일 |
|---|---|---|
| 페이지 들어올 때 | `MiValidateInPage +0x233` (ret `+0x238`) → `MiRelocateImagePfn` | 쪽마다 반복 (`+0x1a3 ~ +0x275`). 부르기 전 `+0x1ca ~ +0x1d6` 이 모드 비트 3 을 비트 5 로 옮기고 `+0x20a/+0x20d TEST BL,0x20 / JZ → +0x517` 로 거른다. 비트 3 은 **재배치 기록 포인터 (`[이미지 정보 +0x20]`) 가 널이 아닐 때** (`+0x14e CMP [R14+0x20],RBX / +0x152 JNZ → +0x318`) `+0x343 OR R8D,4`, `+0x351 OR R8D,8` 로 켜진다. 그 앞 조건: `MiFlags` 비트 17 (`+0x318`) 또는 `+0x323 ~ +0x32d` (컨트롤 영역 `+0x5c & 0xc0000` 과 PFN 비트) 와 `+0x336 MiCanPageMove` 결과 |
| 섹션 만들 때 올라와 있던 쪽 | 위 ⑨ `MiWalkEntireImage +0x4cd` (ret `+0x4d2`) | 위 표 |
| 기준 주소를 다시 바꿀 때 | `MiRelocateImageAgain +0x17e MiSwitchBaseAddress` (ret `+0x183`) → `MiSwitchBaseAddress +0x9a MiWalkEntireImage(…, 2, …)` | 같은 파일 컨트롤 영역이 살아 있고 (`MiRelocateImageAgain +0x80 TEST [RBX+0x5c],0x400000` 비트 22 꺼짐) 먼저 고른 기준이 없을 때 (Q62b 쪽) |
| 그 밖 | `MiPrivateFixup +0x340` 도 `MiRelocateImagePfn` 을 부름 (`_refs/MiRelocateImagePfn.tsv`) | 안 읽음 |

- `MiRelocateImagePfn` (`.asm`): `+0x20 ~ +0x3e` 에서 `[컨트롤 영역 +0x60] & -8` → `[+0x20]` 기록 블록을 꺼내고, `+0x6b CMP R12D,[R14+0x38]` (쪽 번호 < 쪽 수) · `+0x81 CMP [RDI+RAX*8],0` (쪽 칸이 비면 건너뜀) 으로 거른 뒤 `+0x141/+0x146 MiPerformFixups` 를 부른다. 델타는 `[R14+0x28]` (`+0x10e/+0x11d`).
- `MiPerformFixups`: 쪽 칸 값이 1 보다 크면 블록 → `MiApplyRawFixups` (`+0x2c5`) 또는 `MiApplyCompressedFixups` (`+0x131`), 쪽 경계 항목은 `MiApplyStraddleFixups` (`+0x15f`). 실제 쓰기는 `MiApplyCompressedFixups +0xc7 ADD qword [RAX],RBP` / `+0xe9 ADD dword [RAX],EBP` 와 `MiApplyRawFixups → MiDoSingleFixup`.
- **헤더 쪽의 `ImageBase` 칸**: `MiPerformFixups +0x1b6 ~ +0x1c4` 가 이 쪽이 `[기록 +0x3c]` (= 셋째 인자 + `0x30`, 32비트면 `+0x34` — `MiRelocateImage +0xf7 LEA R9D,[RSI+0x30]` / `+0x685 LEA R9D,[RSI+0x34]` → `+0x326 MOV [R14+0x3c],EAX`. 셋째 인자가 NT 헤더 오프셋이라는 읽기는 `[추측]`, `PspMapSystemDll` 이 `RtlImageNtHeader` 결과 `+0x30/+0x34` 에서 `ImageBase` 를 읽는 것과는 맞는다) 가 든 쪽이면 `+0x252 ~ +0x278 ADD dword [RCX],R15D` (세그먼트 플래그 비트 15 `Binary32`) 또는 `+0x37a ADD qword [RCX],R15` 로 **메모리 헤더의 `ImageBase` 를 `BasedAddress` 쪽으로 옮긴다** `[정적]`. 증거 하나 더: `PspMapSystemDll +0x1ae RtlImageNtHeader → +0x207050 MOV RDX,[RAX+0x30]` (32비트는 `+0x1c2 [RAX+0x34]`) → `+0x207054 MOV [RBX+0x20],RDX` 가 ntdll 의 기준 주소를 **메모리 헤더 `ImageBase`** 에서 읽는다 (run48 `+0x172 CMP [RBX+0x20],RAX` 검사와 이어짐).
- 델타의 뜻: `[기록+0x28]` = 새 `BasedAddress` − **헤더 `ImageBase`** (처음), `MiSwitchBaseAddress` 가 다시 바꾸면 누적 (`+0x3e/+0x9f ~ +0xa3`). `MiRelocateAmount` (= `MiRelocateImagePfn` 이 쓰는 같은 칸) 가 이 값을 돌려주고 (`MmGetSectionInformation +0x17e`, 정보 종류 2) `PspLocateSystemDll +0x17e` 가 ntdll 의 "재배치량" 으로 칸 `+0x38` 에 둔다 (run48 7g).

#### 1-3. 누가·언제 이미지 안 번지수를 고치나

| | exe (test.exe) | 보통 DLL (dll1·dll2·kernel32 …) | ntdll |
|---|---|---|---|
| 섹션 만드는 길 | `NtCreateUserProcess +0x937/+0x93c MmCreateSpecialImageSection` → `+0xcf/+0xd4 MiCreateSection` | `NtCreateSection +0x70/+0x75 MiCreateSectionCommon` → `+0x200/+0x205 MiCreateSection` (ntdll 쪽 `LdrpMapDllNtFileName +0x18f/+0x194`, run73) | 부팅 `PspLocateSystemDll +0xef/+0xf4 MmCreateSpecialImageSection` → `MiCreateSection` (run48) |
| 공통 안쪽 | `MiCreateSection +0xf3/+0xf8 MiCreateImageOrDataSection` → 새 컨트롤 영역이면 `+0x380/+0x385 MiCreateNewSection` → `+0x3d5/+0x3da MiRelocateImage` (둘째 자리 `+0x695/+0x69a`), 이미 있으면 `+0x1d0/+0x1d5 MiShareExistingControlArea` (Q62b) | 같음 | 같음 |
| 기준 주소 | **임의**: `MiSelectImageBase +0x30c ~ +0x338` (`ExGenRandom` → `(나머지 + 0x7ff60000) << 16`, 범위 `0x7ff600000000` ~ `+(0x20001 − 64KB칸수) × 0x10000`) | 비트맵 (`+0x15a ~ +0x1d1`, run73) | 비트맵 (DLL 이라) |
| 이미지 안 번지수 | 커널 (`MiPerformFixups`) — 페이지 들어올 때 + 올라와 있던 쪽 | 같음 | 같음 |
| 메모리 헤더 `ImageBase` | 커널이 `BasedAddress` 쪽으로 | 같음 | 같음 (`PspMapSystemDll` 이 이 값을 읽음) |
| 진입점 (`TransferAddress`) | 커널 2번: `MiRelocateImage +0x1b1279` (섹션 사본) + `PspAllocateProcess +0x20ce` (프로세스 사본, 다를 때만) | 안 씀 (DLL 진입점은 ntdll 이 헤더로 계산) | 안 씀 |
| 맵 결과 | `MiMapProcessExecutable +0xb3/+0xb8 MmMapViewOfSectionEx` (BaseAddress NULL 로 부름 `+0x51 AND [RBP+0x6f],0`, `+0x56 LEA R8,[RBP+0x6f]`), 반환 음수만 실패 (`+0xbd JS`) → `0x40000003` 도 통과, `+0xc5 [RSI+0x2b0] = 붙은 주소` | `NtMapViewOfSection` 아래 표 | `PspMapSystemDll +0x118/+0x11d`: `+0x132 CMP EDI,0x40000003 / JZ → +0x195`: **시스템 프로세스 (`PsInitialSystemProcess`) 면 통과, 아니면 `+0x19e MOV ESI,0xc0000018`** |
| ntdll 재배치 문 | `LdrpInitializeProcess +0x1435 LdrpProtectAndRelocateImage` (run38, 이번엔 안 읽음) | `LdrpCompleteMapModule +0x31b LdrpRelocateImage` — 맵 결과가 정확히 `0x40000003` 일 때만 (run38) | 해당 없음 (커널이 늘 같은 칸에 붙임) |

#### 1-4. 기준 주소 (`BasedAddress`) 를 누가 정하나 — "ntdll 재배치 문은 언제 타나" 의 앞쪽

- **`/DYNAMICBASE` 없음 (x86·x64)**: 위 1-1 ① 세 번째 줄 조건이면 `MiRelocateImage` 가 `+0x6c6` 로 일찍 끝난다. `BasedAddress` 는 `MiBuildImageControlArea` 가 채운 **헤더 `ImageBase`** 그대로 (run47, 이번에 안 읽음). 그 자리가 이 프로세스에서 비면 맵 결과 `0`, 이미 차 있으면 `0x40000003` (→ ntdll 이 `.reloc` 로 직접 재배치).
  - 강제 켜는 길: `MmRegistryState` 첫 칸이 −1 이면 (`+0x6ad ~ +0x6b4`) `DYNAMICBASE` 없어도 계속 진행 `[정적]` (레지스트리 이동 정책 값 같지만 이름은 `[추측]`). `AppContainer` (`DllCharacteristics & 0x1000`) 도 계속.
- **`/DYNAMICBASE` 있음**: 64비트 exe → 임의 (`MiSelectImageBase +0x1df → +0x304 …`; `+0x114 JNZ → +0x1df` 가 "EXE 이고 넷째 인자 비트 1 꺼짐", `+0x1e5 TEST [R15+0x5c],0x800000 / JZ → +0x304`). exe 가 비트맵을 타려면 여섯째 인자 비트 1 이 켜져야 하는데, 그 비트의 출처 (패킷 비트 28) 는 `MiInitializeCreateSectionPacket +0x9d TEST SIL,0x40 / +0xa1 JNZ → +0x306 BTS [RBX],0x1c` 이고, exe 를 만드는 `MmCreateSpecialImageSection +0x3f ~ +0x73` (`EDI & 1`, `EDI & 4 → OR EBX,8`, `OR EBX,0x20`) 이 `+0xc7 MOV [RSP+0x28],EBX` 로 넘기는 여섯째 인자는 `{1, 8, 0x20}` 의 조합뿐이라 `0x40` 이 없다 → exe 는 임의 갈래 `[정적: 인자 따라가기, 사람 눈 한 번]`. 관찰값 `0x7ff70ee10000` (run30) 이 그 범위 안이다 (`0x7ff70ee10000 − 0x7ff600000000 = 0x10ee10000 = 0x10ee1 × 0x10000`, 한계 `0x20001`).
  - DLL → 비트맵 (run73). `MiSelectImageBase` 는 DLL 이면 인자 비트 1 과 상관없이 비트맵.
- **샘플 확인 (임시로 헤더를 읽음, 레포에 안 남김)**: `sample/bin` 의 `dll1.dll` · `dll2.dll` 은 `DllCharacteristics 0x160` (DYNAMICBASE 켜짐), `test.exe` 는 `0x8160`. 셋 다 `.reloc` 디렉터리 있음 (`0x800`/`0x804`/`0x810` 바이트), `RELOCS_STRIPPED` 없음 → 세 파일 모두 위 ① 의 세 "일찍 끝" 조건에 안 걸리고 풀 길을 탄다 `[소스: 빌드 산출물 헤더]`.

#### 1-5. `NtMapViewOfSection` status 표

길: `NtMapViewOfSection +0x248/+0x24d MiMapViewOfSection` (또는 `NtMapViewOfSectionEx +0x6c/+0x71 MiMapViewOfSectionExCommon +0x1c8/+0x1cd MiMapViewOfSection`) → 컨트롤 영역이 이미지면 (`MiMapViewOfSection +0x299 MOV EAX,[R14+0x38] / +0x2a7 TEST AL,0x20 / +0x2a9 JNZ`) 조각 `FUN_1408b5a43 +0x33/+0x38` (= `MiMapViewOfSection +0x446/+0x44b`) `MiMapViewOfImageSection`. 반환값은 그대로 올라가 `NtMapViewOfSection +0x24f MOV EBX,EAX` → `+0x290 MOV EAX,EBX` (`+0x258 JNS` 로 양수·0 은 성공 길) `[정적]`.

| 값 | 때 | 줄 (`MiMapViewOfImageSection` 기준) |
|---|---|---|
| **`0`** | 뷰 시작 − SectionOffset 낮은 32비트 == `_SEGMENT.BasedAddress` | 초기값 `+0x353 MOV [RSP+0x60],R15D` (R15D 는 `+0x2b7 XOR` 로 0), 비교 `+0x412 MOV R8,[RBP+0xd8]` · `+0x41c` · `+0x424 MOV EAX,[R8]` · `+0x427 SUB RCX,RAX` · `+0x42a CMP RCX,[RBP-0x60]` · `+0x42e JNZ` (안 탐), 반환 `+0x64c MOV EAX,[RSP+0x60]` |
| **`0x40000003`** | 위가 **다름** | `+0xc0a MOV [RSP+0x60],0x40000003` (`+0x42e JNZ → +0xc0a`). `+0xc12 TEST BL,2 / JZ → +0x434` 로 이어짐 |
| `0x40000003` | 큰 페이지 이미지 뷰 마무리 | `MiMapViewOfSection` 조각 `FUN_1408b5a43 +0x132 MOV EDI,0x40000003` (`MiMapViewOfSection +0x545`) |
| `0x40000036` | `0x40000003` 갈래에서 `BL` 비트 1 · 3 이 같이 켜졌을 때 | `+0xc1b TEST BL,8 / +0xc1e JNZ → +0xc79` → `+0xc82` (`BL` 출처 칸 뜻은 모름, 아래 `[공백]`) |
| `0x4000000e` | 기계 종류 불일치 (`+0xa8f ~ +0xa9a` 에서 `[R12+0x3a]` 계열 비교 뒤) | 조각 `FUN_140b24c86 +0x6/+0xb` (`+0x1ab7a0/+0x1ab7a5`), 들어오는 줄 `+0xaa0 ~ +0xac7` |
| `0xc0000018` | 호출자가 준 주소 범위가 차 있음 | `+0x405/+0x40a MiIsVaRangeAvailable` → `+0x40c JZ → +0xe72 MOV ESI,0xc0000018`. 다른 길 `+0xe0c MOV EAX,0xc0000018` |
| `0xc0000017` | 끝 주소가 `0x7fffffff0000` 넘음 / 빈 자리 못 고름 | `+0x69f CMP / +0x6a2 JA → +0xe65`, `+0xb5c MOV ESI,0xc0000017` |
| `0xc000010a` | 프로세스 쪽 플래그 (`[R13+0x1f4] & 0x20`) 가 켜짐. 값 이름은 "프로세스 끝나는 중" 으로 보이지만 그 비트 이름은 안 확인 `[추측]` | `+0x3ca TEST AL,0x20 / +0x3cc JNZ → +0xe5e` |

- **`BasedAddress` 먼저 시도**: 호출자가 주소를 안 줬을 때 (`[R12+0x3c]` 비트 0 꺼짐: `+0x35d TEST AL,1 / +0x35f JNZ → +0xe38`, `+0x3d7 TEST AL,1 / +0x3d9 JZ → +0x691`) 시작 후보 = `BasedAddress + SectionOffset` (`+0x365 ~ +0x373`). 그 범위가 비면 (`+0x6c8/+0x6cd MiIsVaRangeAvailable`, `+0x6d2 JNZ → +0x412`) 바로 비교로, 차면 `+0x6d8 JMP → +0x932` → `+0x9ae MiSelectUserAddress` 로 다른 자리를 골라서 비교에서 **다름** → `0x40000003`. 비트 0 의 뜻 ("호출자가 주소를 줌") 은 흐름으로 읽은 것 `[정적: 흐름으로 읽음, 비트 이름은 PDB 에 없음]`.
- 호출자가 준 주소 (`[R12+0x3c]` 비트 0 켜짐) 가 `BasedAddress` 와 다르면 비어 있어도 `0x40000003`, 차 있으면 `0xc0000018`.
- 이 줄은 DLL 의 "먼저 붙는 DLL 은 `0`, 같은 범위를 쓰는 둘째 DLL 은 `0x40000003`" (run72) 과 같은 줄이다. dll1·dll2 는 서로 다른 비트맵 칸을 받으므로 (run73) 이 줄에서 **다르지 않을 것** `[정적 + 동적 확인 필요]`.

#### 1-6. 질문 1 에 대한 "지금 우리 (PE_emulator) 맞나·틀리나"

- **DLL 길 — 맞다 (결과 값 기준).** `AddressSpace.cpp` 의 `_RelocateImageView` 가 `BasedAddress ≠ 헤더 ImageBase` 일 때 `.reloc` 로 델타 = `BasedAddress − 헤더 ImageBase` 를 더하고 메모리 헤더 `ImageBase` 를 `BasedAddress` 로 쓴다. 진짜도 같은 델타 · 같은 헤더 칸 (`MiPerformFixups +0x278/+0x37a`). **때만 다르다**: 진짜는 페이지 들어올 때 (+ 올라와 있던 쪽 즉시), 우리는 뷰 붙일 때 한 번 (이 점은 코드 주석에 이미 "누가·언제는 internals 분석 없음"). 이번 분석으로 "누가 = `MiPerformFixups` (`MiRelocateImagePfn` 경유), 언제 = 페이지 들어올 때 + `MiRelocateImage` 끝의 `MiWalkEntireImage`" 가 닫힌다.
- `_SelectViewBase` (`BasedAddress` 비면 `0`, 아니면 위쪽 빈 자리 + `0x40000003`) — **맞다.** 한 가지 빠짐: 진짜는 호출자가 준 주소 · `SectionOffset` 도 비교에 들어간다 (`+0x424 ~ +0x42a`). 우리는 로더 꼴 (`SectionOffset` 0, 주소 NULL) 만 받아서 영향 없음.
- 재배치 항목 종류: 진짜는 0 · 3 (HIGHLOW) · `0xA` (DIR64), 우리는 0 · `0xA` 만 (`_ApplyRelocationBlock`). 64비트 DLL 만 쓰는 지금은 **맞다**. 진짜는 다른 종류를 `0xC0000269` 로 받아 **오류 없이 0 으로 끝내고 이동을 안 한다** (`MiRelocateImage +0x7df ~ +0x7eb`) — 우리는 `STATUS_NOT_SUPPORTED` (엔진 쪽 규칙, 틀린 점이지만 해당 DLL 이 없음).
- `.reloc` 디렉터리가 없는 DLL: 우리는 "공사 중" 실패 (`base relocation directory missing`). 진짜는 오류가 아니고 (`RELOCS_STRIPPED` 만 `+0x64b` 로 일찍 끝, 그 밖엔 디렉터리 둘 다 0 → "항목 없음" `+0x67d`) 기준 주소는 그대로 고르고 (`+0x2e6`), 블록 걷기는 건너뛰고 (`+0x391 JZ → +0x579`), **헤더 쪽 `ImageBase` 칸만 고치는 길** (`+0x579 ~ +0x581 OR …,1`) 로 간다 `[정적: 흐름으로 읽음]` → 우리 동작과 다름 (`확인 필요`: 샘플 DLL 은 모두 `.reloc` 있음이라 지금은 안 걸린다).
- exe · ntdll — **틀림, 사용자 결정으로 막아 둠 (D8·12).** 진짜 exe 는 DYNAMICBASE 64비트면 임의 `0x7ff6…` 대역 (`ExGenRandom`), ntdll 은 비트맵. 우리는 헤더 값 · 델타 0 · `bLoaderSection=false` 로 `MiRelocateImage` 를 건너뜀. `Section.cpp` 의 `_IsNativeBitmapImage` 주석 ("EXE 갈래 +0x1df → ExGenRandom") 이 이미 이 갈래를 가리킨다. 이번 정적으로 **"exe 는 임의 갈래를 탄다" 가 확인**된 것 (`+0x30c ~ +0x338`).
- ntdll 재배치 문: 우리 `0x40000003` 길 (`LdrpCompleteMapModule +0x31b`) 은 진짜와 같은 조건 (뷰가 `BasedAddress` 말고 다른 곳) — **맞다.**

### 2. (b) `PspAllocateProcess +0x20b3 ~ +0x20ce` 보정 조건

**답: 같은 델타를 막는 가드가 아니라, 기준이 다른 두 번째 보정이다. 두 번 더하지 않는 것은 기준 칸이 이어지기 때문이다.**

| 줄 | 명령 | 뜻 |
|---|---|---|
| `+0x978 ~ +0x98c` (ret `+0x991`) | `LEA R8,[R15+0x38]` → `MmGetSectionInformation(섹션, 4, R8)` | 섹션 이미지 정보 (0x40바이트 + 부가 8바이트, `MmGetSectionInformation +0xb3 ~ +0xbe`, `+0x162`) 를 **프로세스용 사본** `[R15+0x38]` 에 복사. 첫 8바이트 = `TransferAddress`. 이 시점 값 = (헤더 `ImageBase` + 진입점 RVA) + `MiRelocateImage` 델타 = `BasedAddress` + RVA |
| `+0x2090/+0x2095` | `MmInitializeProcessAddressSpace` | exe 를 맵 (`MiMapProcessExecutable`), `EPROCESS.SectionBaseAddress = 붙은 주소` (`+0x2b0`, `+0xc5`) |
| `+0x20a6 TEST R15,R15` / `+0x20a9 JZ → +0x20d2` | | 복사 때와 같은 문턱 (`+0x966 TEST R15,R15`) — 이미지 정보 사본이 있을 때만 |
| `+0x20ab TEST BL,2` / `+0x20ae JNZ → +0x20d2` | | `+0x96f TEST BL,2` 와 같은 비트 (`+0x95c MOV R12D,EBX` 로 옮겨 `+0x9c8 OR EBX,R12D` 로 되살림). 이 비트는 입력 플래그 `[RSP+0x8b0]` 의 비트 11 · 13 · 14 에서만 켜진다 (`+0x2ce LEA EBX,[RSI+2]`, `+0x2de [RSI+6]`, `+0x2f2 [RSI+0xe]`) — 공개 헤더 값으로는 최소 프로세스 · 복제 갈래로 보임 `[추측: 이름]`. 평범한 exe 생성은 이 비트가 꺼진 길 |
| `+0x20b0` / `+0x20b3` (ret `+0x20b8`) | `MiSectionControlArea(섹션 R12)` | 컨트롤 영역 포인터 (`MiSectionControlArea` 는 `[섹션+0x28]` 의 비트로 갈라 `& -4` 값 또는 `[..+0x28]→[+0x10]` / `[..+0x28]→[+0x0]` 를 돌려줌) |
| `+0x20b8 MOV RCX,[RAX]` / `+0x20bb MOV RDX,[RCX+0x20]` | | 컨트롤 영역 `+0x0` = `Segment` → `Segment +0x20` = **`_SEGMENT.BasedAddress`** (PDB). **sast/11 의 "컨트롤 영역 `+0x20` 칸" 은 세그먼트 `+0x20` 이 맞다** (컨트롤 영역 `+0x20` 은 `NumberOfPfnReferences`) |
| `+0x20bf MOV RAX,[R14+0x2b0]` | | `EPROCESS.SectionBaseAddress` (PDB `+0x2b0`) |
| `+0x20c6 CMP RDX,RAX` / `+0x20c9 JZ → +0x20d2` | | **같으면 보정 없음** |
| `+0x20cb SUB RAX,RDX` / `+0x20ce ADD [R15+0x38],RAX` | | `사본 TransferAddress += (SectionBaseAddress − BasedAddress)` |
| `+0x20d2 BTS EBX,7` | | 어느 쪽이든 비트 7 켬 |

- 두 보정의 이어짐 (두 번 안 셈):
  1. 섹션 만들 때: `MiRelocateImage +0x1b1279 ADD [R15],RAX` → `TransferAddress` (섹션 사본, 공유) += (새 `BasedAddress` − 옛 `BasedAddress`). 처음 옛 값은 헤더 `ImageBase`. 이후 `MiSwitchBaseAddress +0x4e ADD [RCX],RAX` 가 다시 바뀔 때마다 같은 방식으로 더한다 (새 `[Segment +0x20]` 를 `+0x46` 에 쓰고 `+0x3e` 의 델타를 `+0x4a ~ +0x4e` 에서 더함).
  2. 프로세스 만들 때: 그 사본을 복사한 뒤 (`+0x991`) `PspAllocateProcess +0x20ce` 가 (실제 붙은 주소 − **`BasedAddress`**) 만 더한다. `BasedAddress` 를 기준으로 삼기 때문에 ① 이 이미 반영된 값 위에 얹는 것이고, 붙은 주소가 `BasedAddress` 와 같으면 (`MiMapProcessExecutable` 은 BaseAddress NULL 로 부르고 `BasedAddress` 를 먼저 시도하므로 보통) `+0x20c9 JZ` 로 건너뛴다.
  - 따라서 합 = 실제 붙은 주소 + 진입점 RVA. test.exe 가 `BasedAddress` 자리에 붙는 한 `+0x20ce` 는 안 탄다 `[정적: 흐름]`; 안 붙을 때 (그 범위가 이미 쓰는 중) 만 탄다.
  - 한계 `[추측]`: `+0x20bb` 는 `BasedAddress` 를 **다시 읽는다** (복사 `+0x98c` 보다 뒤). 그 사이에 같은 컨트롤 영역으로 `MiSwitchBaseAddress` 가 돌면 사본과 어긋날 수 있다. 같은 파일을 동시에 여는 드문 경우 (동적).
- 이 보정은 헤더 페이지·`.reloc` 에는 아무 영향이 없다 (`TransferAddress` 사본만). 뷰가 `BasedAddress` 말고 다른 곳에 붙어도 페이지는 `BasedAddress` 기준으로만 고쳐져 있으므로, 나머지 (붙은 주소 − `BasedAddress`) 는 ntdll 몫이다.

**질문 2 에 대한 PE_emulator 맞나·틀리나**: `Section.cpp:367` TODO 의 두 갈래 답이 이제 **둘 다 있다**: 섹션 사본은 `MiRelocateImage +0x1b1279` 가 새 − 옛 `BasedAddress`, 프로세스 사본은 `PspAllocateProcess +0x20ce` 가 붙은 − `BasedAddress` (같으면 0). 우리는 앞의 것 (`+= qwBase − qwHeaderBase`) 만 있고, exe 가 늘 `BasedAddress` 에 붙으므로 **지금은 맞다** (뒤의 것은 항상 0). exe 를 `BasedAddress` 가 아닌 곳에 붙이는 경우가 생기면 (우리 코드로는 없음) 뒤의 보정이 필요하다. **확인 필요**: exe 를 "작은 ASLR" 로 옮길 때 이 두 줄을 같이 넣을지.

### 3. (a) 안 읽은 함수의 `TransferAddress` (`_SECTION_IMAGE_INFORMATION +0x0`) 쓰기 O/X

기준: PDB `_SECTION_IMAGE_INFORMATION +0x0 TransferAddress`, `_MI_SECTION_IMAGE_INFORMATION +0x0 ExportedImageInformation` (그 안 `+0x0`), 이미지 정보 포인터 = `_SEGMENT.u2` (`+0x38`, `MiRelocateImage` · `MiSwitchBaseAddress` · `MiParseComAndCetHeaders` 가 모두 `[세그먼트+0x38]` 로 읽음). 즉 "`[세그먼트 +0x38]` 가 가리키는 곳의 `+0x0`" 을 쓰는 줄을 찾았다.

| 함수 | 쓰나 | 이미지 정보에 쓰는 줄 / 하는 일 (`.asm`) |
|---|---|---|
| `MiParseComAndCetHeaders` | **X** | `+0x20c OR [RAX+0x4c],R14D` · `+0x41f/+0x42c/+0x439/+0x446/+0x453 OR [RAX+0x4c],…` = `InternalImageInformation +0xc` 의 CET 비트 0~6 (PDB `_MI_EXTRA_IMAGE_INFORMATION +0xc`), `+0x245 OR [RAX+0x4c],0x40` (`ImageExpansionCompatible`); `+0x3d8 / +0x3fb / +0x40c MOV [RDX+0x33],CL` = `ImageFlags` (`ComPlusILOnly` 등, PDB `+0x33`); `+0x3aa OR word [RDI+0xc],0x4000` = 세그먼트 `ForceCollision`. `+0x0` 칸 쓰기 없음 |
| `MiMakeImageReadOnly` | **X** | 서브섹션 사슬 (`[RCX+0x80]`) 의 플래그 `+0x4d MOV [RDI+0x20],EAX` 와 프로토 PTE (`+0x99 / +0xae / +0xbc MOV [RBX],RCX` 등). 이미지 정보는 안 만짐 |
| `MiInitializeNewImageSectionProtos` | **X** | `+0x115` · `+0x187 OR qword [R15],-1` (R15 = `[RBX+0x30]`, 둘째 인자의 쪽 번호 배열 칸) 을 −1 로. 이미지 정보 안 만짐 |
| `MiValidateSectionSigningPolicy` | **X** | 전역 `+0x124 MOV [0x140e2d838],ECX` (이유 번호 전역 하나) 와 스택 쓰기뿐. 이미지 정보 안 만짐 |
| `MiReleaseImageSection` | **X** | `+0x2c AND [RSI+0x50],0` · `+0x33 AND [RSI+0x38],~2` (둘째 인자 쪽 칸). `+0x0` 칸 쓰기 없음 |
| `MiFinalizeImageHeaderPage` | **X** | PFN 정리 (`+0xac AND.LOCK [RBX+0x18]`, 조각 `FUN_1406f88ec +0xc9 MOV [RBX+0x10],RAX` = `ZeroPte`). 이미지 정보 안 만짐 |

- **여섯 밖에서 `TransferAddress` 를 더하는 줄 (이번에 읽은 범위)**: ① `MiBuildImageControlArea +0x172` (처음 값 = 헤더 `ImageBase` + 진입점 RVA, run31), ② `MiRelocateImage +0x1b1279`, ③ **`MiSwitchBaseAddress +0x4e ADD [RCX],RAX`** (RCX = `[세그먼트+0x38]`, RAX = 방금 `[기록+0x28]` 에 쓴 이번 델타), ④ `PspAllocateProcess +0x20ce` (프로세스 사본). "이 넷뿐" 이라고는 못 한다 (포인터를 거쳐 쓰는 다른 줄은 정적으로 다 못 찾음).
- **질문 3 에 대한 PE_emulator 맞나·틀리나**: **맞다.** 우리는 `Section.cpp:382` 한 줄 (`+= qwBase − qwHeaderBase`) 이 ② 에 해당하고, ③ 은 "같은 파일을 다시 열면 새 기준을 고르는" 길이라 우리 코드에 없지만 우리도 "늘 새 섹션" (`MiCreateImageOrDataSection` 주석) 이라 그 길을 안 탄다.

## 정적으로는 못 정한 것 `[공백]`

- 섹션을 막 만든 순간 (`MiRelocateImage` 끝) 에 **메모리에 올라와 있는 이미지 페이지가 어느 것인가** (헤더 쪽뿐일 것 `[추측]`) — `MiWalkEntireImage` 가 실제로 `MiRelocateImagePfn` 을 몇 번 부르나.
- 페이지 들어올 때 `MiValidateInPage +0x336 MiCanPageMove` 와 `+0x323 ~ +0x32d` 조건이 평범한 DLL 에서 어느 쪽으로 가나 (Ghidra `.c` 는 `!bVar20 ||` 로 묶었는데 `.asm` 은 `MiCanPageMove` 를 늘 부르는 흐름 — `.asm` 을 믿었다).
- `[R12+0x3c]` 비트 0 (호출자가 주소를 줌) 과 `MiMapViewOfImageSection` 의 `BL` 비트 1·3 (`+0xc12 ~ +0xc82`, `0x40000036`) 의 이름·출처 (run72 에서도 `[공백]`).
- `0xC0000269` 로 끝난 경우 `MiSelectImageBase` 가 이미 켜 둔 비트맵 칸이 어떻게 되나 (`+0x7df ~ +0x7eb` 는 `MiReturnImageBase` 류를 안 부르는 것으로 읽힘 `[추측]`).
- ntdll 쪽에서 메모리 헤더 `ImageBase` (커널이 옮긴 값) 를 어떻게 읽어 `LdrpRelocateImage` 의 델타를 정하나 — 이번 범위 밖 (`LdrpRelocateImage` 본문은 안 봄).

## 동적분석으로 확인해야 할 것

실제 값이 필요한 것만. (동적 요청서는 따로 안 씀 — 오케스트레이터가 정한다.)

1. 맵 결과와 `BasedAddress` 비교 (run73 동적 1 과 이어짐): `nt+0x97991a` (`MiMapViewOfImageSection +0x42e`) 에서 `rcx` (뷰 시작 − 낮은 32비트) 와 `poi(@rbp-0x60)` (`_SEGMENT.BasedAddress`) 를 같이 찍기 — 같으면 `0`, 다르면 `+0xc0a` (`nt+0x97a0f6`) 를 지남. 대상은 test.exe 의 dll1 · dll2 · kernel32.
2. 누가 `.reloc` 를 실제로 적용하나: `nt+0x92aca8` (`MiRelocateImagePfn` 입구) 에서 콜스택의 caller 가 `MiWalkEntireImage+0x4d2` (`nt+0x2dc712`) 인지 `MiValidateInPage+0x238` (`nt+0x372684`) 인지 (+ `MiPrivateFixup`), 그리고 `MiPerformFixups +0x278/+0x37a` (`nt+0x92b26c` / `nt+0x92b36e`) 가 헤더 쪽 (`rcx & 0xfff` = `e_lfanew + 0x30`) 에서 도는지. dll1 · dll2 각각 몇 번 도는지.
3. 섹션 사본 `TransferAddress`: `MiRelocateImage` 조각 `+0x1b1279` (`nt+0xb246bd`) 에서 `poi(@r15)` 와 `rax` (델타), 그리고 `+0x1b1269` (`nt+0xb246ad`) 의 `rax` (새 `BasedAddress`) — exe 의 값 `0x7ff6…` 범위인지.
4. `PspAllocateProcess +0x20c6` (`nt+0xa0462e`): `rdx` (`BasedAddress`) 와 `rax` (`SectionBaseAddress`) — 같아서 `+0x20c9 JZ` (`nt+0xa04631`) 를 타는지 (기대: 같음). 값이 다르면 `+0x20ce` 이후 `[r15+0x38]` 가 어떻게 바뀌나 (run31 Q35 의 `nt+0xa02ef9` `dq @r15+0x38` 과 비교).
5. exe 의 `BasedAddress` 가 임의 갈래인지: `MiSelectImageBase +0x304` (`nt+0x972de0+0x304 = nt+0x9730e4`) 가 test.exe 에서 지나는지, 반환 `[기록…]` 값이 관찰 `0x7ff70ee10000` 과 같은지 (run30 · run44 `lm` 대조).
- 추천 멈춤점 요약: `nt!MiRelocateImagePfn` (`nt+0x92aca8`, 호출자 구분), `nt!MiMapViewOfImageSection +0x42e` (`nt+0x97991a`), `nt!PspAllocateProcess +0x20c6` (`nt+0xa0462e`). 커널 멈춤점은 모든 프로세스에 걸리므로 `test.exe` 일 때만 멈추는 조건을 붙인다 (CLAUDE.md).

## 다음 요청 후보

1. **Q62b (이미 큐)**: 공유 길 `MiShareExistingControlArea → MiValidateExistingImage`, 프로토 PTE 만들기, `MiVerifyImageHeader` 이유 코드. 이번에 `MiRelocateImageAgain → MiSwitchBaseAddress` 까지 닿았으므로 이어서 읽기 좋다.
2. ntdll 반대편 (정적): `LdrpRelocateImage` 본문 + `LdrpProtectAndRelocateImage` — 메모리 헤더 `ImageBase` (커널이 옮긴 값) 로 델타를 어떻게 정하나, `.reloc` 없는 이미지에서 무엇을 하나 (`STATUS_CONFLICTING_ADDRESSES` 류).
3. 작은 정적: `MiCanPageMove` + `MiValidateInPage +0x14e ~ +0x351` 의 비트 3·5 조건 정확히 닫기 (평범한 DLL 이 page-in 에서 `MiRelocateImagePfn` 을 타는지), `MiPrivateFixup +0x340` 의 쓰임.
4. 작은 정적: `MiSelectImageBase` 의 `ESI` 값 (1·2·3·4 갈래: 4GB 아래 이미지 · WOW) 과 `MiBuildImageControlArea` 가 `BasedAddress` 를 헤더 값으로 채우는 줄 (`+0x25e/+0x261`, run47) 을 이번 길과 한 장으로 묶기 (flow-writer 쪽 정리).
5. (PE_emulator 쪽) 샘플 DLL 에 `.reloc` 없는 변형이나 `DYNAMICBASE` 없는 변형을 쓸 계획이 있으면 위 1-6 의 "틀린 점" 두 줄 (`.reloc` 없음 · `DYNAMICBASE` 없음 일찍 끝) 을 먼저 닫는다.

## 판정

**분석성공** — 질문 1 (재배치 책임·때, DLL·ntdll 같은 길, `0` / `0x40000003` 을 정하는 줄과 조건), 질문 2 (보정 관계), 질문 3 (여섯 함수 O/X) 모두 `.asm` 줄로 답했다. 정적으로 못 닫은 것은 "섹션 만드는 순간 올라와 있는 쪽" 과 `MiValidateInPage` 의 `MiCanPageMove` 조건 두 가지 (동적 1·2 로 닫힘).

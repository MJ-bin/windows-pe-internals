# 26 image-section — 정적 분석 결과

- 요청서: `_workspace/sast/26_image-section.md` (Q33)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`ntoskrnl.gpr` + `_index.tsv` + analyze 로그의 `SetPdb:` 줄 셋 다 있음). 지도는 기존 `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*` 를 그대로 썼다.
- 뽑은 함수: **21개, 전부 depth 0** (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`). 요청서 표는 depth 1 이었는데, 다섯 함수의 호출이 117곳이라 depth 1 이면 callee 가 100개 가까이 되어서 depth 0 으로 하고 필요한 callee 만 이름으로 골랐다 (넓힌 이유는 아래).
  - 요청한 6개: `MmCreateSpecialImageSection`, `MiCreateSection`, `MiCreateImageOrDataSection`, `MiCreateNewSection`, `MiCreateImageFileMap`, `MiVerifyImageHeader` (지도 깊이 1·2·3·4·5·6).
  - 질문 1·3·4 에 필요해서 더 내려간 9개: `MiBuildImageControlArea` (**질문 4 의 답이 여기**), `MiReadImageHeaders`, `MiCopyHeaderIfResident`, `MiInitializeImageProtos`, `MiInitializeImageHeaderPage` (질문 3), `MiInitializeCreateSectionPacket`, `MiReferenceControlArea`, `MiShareExistingControlArea`, `MiSectionCreated` (질문 1 의 갈림길).
  - 질문 2 비교와 질문 4 확인용 6개: `MiCreateSectionCommon`, `MmCreateSectionEx`, `MmCreateSection`, `NtCreateSection` (일반 길), `MiRelocateImage` (**`TransferAddress` 를 한 번 더 고치는 줄이 여기**), `MiParseImageSectionHeaders` (뽑기만 하고 안 읽음).
  - 내려간 이유 한 줄: 요청서가 허락한 `MiVerifyImageHeader` 는 진입점 rva 를 **복사**만 한다. `TransferAddress` 를 **쓰는** 줄은 그 아래가 아니라 옆 (`MiBuildImageControlArea`) 이었다.
- 호출자 표: `refs` 로 `MiCreateSection`·`MiCreateImageOrDataSection`·`MiCreateNewSection`·`MiCreateImageFileMap`·`MiVerifyImageHeader`·`MiBuildImageControlArea` 의 부르는 곳을 전부 뽑았다 → `ms/disassemble/ntoskrnl/_refs/<이름>.tsv`.
- 구조체 칸은 PDB 타입 덤프 (`llvm-pdbutil-14 dump -types ms/sym/ntoskrnl.exe.pdb`) 로 확인했다 (아래 "스크립트 문제" 참고).
- 버전 확인: **일치.**
  - `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess` (`_calls.tsv`). `PspInsertThread` call `+0x802` / ret **`+0x807`** → `PspCallProcessNotifyRoutines`.
  - 요청서 지도 값: 다섯 함수의 rva·크기 (`0x96bd64`/371, `0x96d5e0`/617, `0x96dfa0`/1584, `0x96f4d0`/1842, `0x96ff50`/2916) 가 `_index.tsv` 와 같고, 호출 수 9·10·29·32·37 이 `.asm` 의 CALL 줄 수와 같다.
  - 핵심 4곳은 원본 바이트로 다시 봤다 (아래 질문 4).

## 스크립트 문제

고칠 버그는 없다. 제안 1건, 참고 2건.

- **PDB 구조체 칸을 뽑는 도구가 스킬에 없다.** 이번에도 `llvm-pdbutil-14 dump -types` 결과를 임시 스크립트로 읽어서 칸 이름·오프셋을 맞췄다 (sast/11 도 손으로 했다). `run_ghidra.sh` 쪽에 "구조체 이름 → 칸 표" 한 줄 명령이 있으면 `_SECTION_IMAGE_INFORMATION`·`_CONTROL_AREA`·`_SEGMENT` 를 볼 때마다 편하다.
- `.asm` 의 전역 변수 주석은 PDB **공개 이름이 있는 것**만 붙는다. `MiVerifyImageHeader`·`MiCreateImageFileMap` 곳곳의 `[0x140e2d838]` (실패한 이유 번호를 적는 전역, rva `0xe2d838`) 는 공개 이름이 없어서 주석이 없다. 오류가 아니라 이름이 없는 것이다.
- `refs` 결과의 `(함수 밖) DATA` 줄은 `.pdata` (예외표) 항목이다. 함수를 부르는 곳이 아니다.

## 한눈에

- **사슬은 맞다, 그런데 "항상 타는 길"은 아니다.** `NtCreateUserProcess +0x93c` → `MmCreateSpecialImageSection +0xd4` → `MiCreateSection +0xf8` → `MiCreateImageOrDataSection +0x385` → `MiCreateNewSection +0x215` → `MiCreateImageFileMap` 각 고리 모두 `.asm` 의 CALL 줄과 ret 이 일치한다. 이 사슬 말고 `MiCreateNewSection` 과 `MiCreateImageFileMap` 을 부르는 곳은 이 빌드에 **없다** (CALL 참조 기준, 각각 호출자 1곳).
- **`MiCreateNewSection` 앞에 갈림길이 있다.** 같은 파일의 이미지 컨트롤 영역이 이미 있으면 `MiShareExistingControlArea` 로 가서 `MiCreateNewSection` 을 건너뛴다. test.exe 가 어느 쪽인지는 실행 때 값이다 (이 부팅에서 처음 만드는가).
- **`MmCreateSpecialImageSection` 은 `NtCreateSection` 의 커널 안 버전이다.** 같은 `MiCreateSection` 을 쓰고, 다른 점은 토큰·서명 수준을 현재 프로세스에서 구하지 않고 **인자로 받는다**는 것, 그리고 **핸들**을 돌려준다는 것이다.
- **`TransferAddress` 가 처음 쓰이는 줄: `MiBuildImageControlArea +0x16b ~ +0x172`.** `ImageBase`(헤더) + `AddressOfEntryPoint`(헤더) 를 이미지 정보 `+0x0` 에 쓴다. 헤더에서 꺼내 복사해 두는 줄은 `MiVerifyImageHeader +0x79 ~ +0x8a`. 그리고 **같은 칸을 한 번 더 고치는 줄이 있다**: `MiRelocateImage` 조각 `+0x1b1279` 가 재배치 델타를 더한다 (이미지 기본 주소를 새로 골랐을 때).
- **섹션 만들기는 프로세스 주소 공간에 올리지 않는다.** 헤더 한 페이지는 물리 메모리에 읽어 두고, 나머지는 프로토 PTE 만 채운다. 실제 매핑은 `MmInitializeProcessAddressSpace +0x1c9` → `MiMapProcessExecutable +0xb3` `MmMapViewOfSectionEx` (코드의 call 자리 확인).
- **서명 재시도 고리(`+0xa08 ~ +0xa2a`)는 이번 사슬의 상태값 때문이 아니다.** 섹션을 만든 뒤 `NtCreateUserProcess` 쪽이 서명 수준을 다시 알아내서 처음 넘긴 값과 다를 때 돈다.
- **points.md 단계 3 은 `MiCreateImageFileMap` 으로 둬도 된다.** 다만 이 함수는 "파일마다 처음 한 번" 서는 함수라서 조건을 붙여야 하고, 값이 바뀌는 순간 지점으로 `MiBuildImageControlArea +0x172` 를 하나 더 두는 게 좋다.

## 질문별 답 [정적]

### 1. 사슬 확인과 각 고리의 조건

**답: 사슬은 맞다.** 아래 표는 `.asm` 의 CALL 줄이다 (`call` → 돌아올 자리 `ret`). 라벨은 ret 로 쓴다.

| # | 부르는 함수 | call | ret | 부르는 대상 | 이 고리를 타는 조건 |
|---|---|---|---|---|---|
| 1 | `NtCreateUserProcess` | `+0x937` | **`+0x93c`** | `MmCreateSpecialImageSection` | run9 의 서명 재시도 고리 안 |
| 2 | `MmCreateSpecialImageSection` | `+0xcf` | **`+0xd4`** | `MiCreateSection` | 없음 (무조건). 상태가 `0xc0000054` 면 `+0xdf` `MiHalfSecond` 만큼 쉬고 (`+0xea` ret `+0xef`) `+0x95` 로 돌아가 같은 호출을 다시 한다 |
| 3 | `MiCreateSection` | `+0xf3` | **`+0xf8`** | `MiCreateImageOrDataSection` | 패킷의 **파일 핸들(`+0x28`) 또는 파일 객체(`+0x30`)가 0 이 아닐 때** (`+0xdc`, `+0x1a9`). 둘 다 0 이면 대신 `MiCreatePagingFileMap` (`+0x1c0`, ret `+0x1c5`). 여기선 핸들을 넘기므로 항상 이쪽 |
| 4 | `MiCreateImageOrDataSection` | `+0x380` | **`+0x385`** | `MiCreateNewSection` | `MiReferenceControlArea` (`+0x19e`, ret `+0x1a3`) 가 돌려준 컨트롤 영역에 **`BeingCreated`(`+0x38` bit1) 가 켜져 있을 때** (`+0x1ba` `MOV EAX,[RAX+0x38]`, `+0x1bd` `TEST AL,2`, `+0x1bf` `JNZ`). 꺼져 있으면 `MiShareExistingControlArea` (`+0x1d0`, ret `+0x1d5`) 로 가고 `MiCreateNewSection` 은 안 탄다 |
| 5 | `MiCreateNewSection` | `+0x210` | **`+0x215`** | `MiCreateImageFileMap` | 패킷 `+0x10` 에 **`SEC_IMAGE`(0x1000000) 켜짐** (`+0xd2` `AND R15D,0x1000000`, `+0xd9` `JNZ`). 꺼져 있으면 `MiCreateDataFileMap` (`+0x105`, ret `+0x10a`) |

- "패킷" = `MiCreateSection` 이 스택에 만드는 0xc8 바이트 (`+0x52` `memset(…,0xc8)`, `+0xc6` `MiInitializeCreateSectionPacket` 이 채움). 칸 몇 개는 `MiInitializeCreateSectionPacket` 의 쓰기로 확인했다: `+0x00` 패킷 플래그, `+0x08` 섹션 속성, `+0x10` 할당 속성(`SEC_IMAGE` 등), `+0x18` 서명 수준 바이트, `+0x1c` 페이지 보호 인자, `+0x28` 파일 핸들, `+0x30` 파일 객체, `+0x38` (나중에 채움) 참조한 파일 객체, `+0x40` 컨트롤 영역, `+0xa0` 호출자가 넘긴 3번째 인자(토큰으로 보임), `+0xa8` 세션 id.

**`SEC_IMAGE` 갈래인가: 예.**
- `NtCreateUserProcess +0x7bc` `LEA EBX,[1+RAX*4]` — 플래그의 bit0 가 **항상 켜진다**. 이 값이 `MmCreateSpecialImageSection` 의 6번째 인자 (`+0x912` `[RSP+0x28]`).
- `MmCreateSpecialImageSection +0x3f ~ +0x56` 이 bit0 로 할당 속성을 고른다: 켜짐 → `0x01000000`, 꺼짐 → `0x02400000`. 원본 바이트 `81 e5 00 00 c0 fe` (`AND EBP,0xfec00000`) · `81 c5 00 00 40 02` (`ADD EBP,0x2400000`) 로 확인했다. 꺼진 쪽(`0x2400000`)도 `MiInitializeCreateSectionPacket +0xfc` (`BT EDI,0x19`) → `+0x346` → 조각 `FUN_140b23a4f` 에서 `SEC_IMAGE` 로 바뀐다.

**같은 파일의 섹션이 이미 있으면 `MiCreateNewSection` 을 안 타는 길: 있다.**
- `MiReferenceControlArea` 가 파일 객체의 `SectionObjectPointer`(`+0x28`) 의 **`ImageSectionObject`(`+0x10`)** 를 본다 (`+0x2b ~ +0x4c`: `SEC_IMAGE` 면 `+0x10`, 아니면 `+0x0`). 전역 잠금 `140e2cd40` 을 잡고 (`+0x57`, ret `+0x5c`) 세 갈래:
  1. **비어 있음** (`+0x66` `JZ +0x175`): 자리표시용 컨트롤 영역(`MiCreateImageOrDataSection` 의 스택 `RBP-0x69`, 0x80 바이트, 플래그 `+0x38`=2 `BeingCreated`, `+0x110`)을 `ImageSectionObject` 에 꽂고 (`+0x178` `[R15]=RSI`) 그걸 돌려준다 → 위 4번에서 `BeingCreated` 켜짐 → `MiCreateNewSection`.
  2. **있고 멀쩡함**: `NumberOfUserReferences`(`+0x30`) 를 하나 올리고 (`+0x11d` `INC [RBX+0x30]`) 그 컨트롤 영역을 돌려준다 → `BeingCreated` 꺼짐 → `MiShareExistingControlArea` (`+0x1d0`). 이 함수 안에서 `MiValidateExistingImage` (`+0xf4`, ret `+0xf9`) 를 부른다 (내용은 안 읽음 — 다시 쓸 만한 이미지인지 확인으로 보임 `[추측]`). `+0x1e0` `DEC ESI` → `+0x27c` (끝맺음) 로 간다.
  3. **있는데 만드는 중/지우는 중** (`+0xdb ~ +0xe7` `TEST CL,1`): `KeWaitForGate` (`+0x2fb`) 로 기다렸다가 `0xc000022d` 를 돌려준다. 위(`MiCreateImageOrDataSection +0x4d8 ~ +0x4e4`)에서 `0xc0000476`·`0xc000060b` 가 아니면 `+0x147` (`MiCallCreateSectionFilters` 앞) 로 돌아가 처음부터 다시 한다.
- `MiCreateNewSection` 의 호출자는 `MiCreateImageOrDataSection +0x380` **하나뿐**이고, `MiCreateImageFileMap` 의 호출자도 `MiCreateNewSection +0x210` 하나뿐이다 (`_refs/MiCreateNewSection.tsv`, `_refs/MiCreateImageFileMap.tsv`; CALL 참조 기준 — 함수 포인터로 부르는 길은 `refs` 가 못 본다). 그래서 이 빌드의 모든 "새 이미지 컨트롤 영역 만들기"는 이 사슬을 지난다.
- 참고: 지도가 "각 함수 caller 는 지도 안에서 1곳"이라 한 건 맞다. 지도 밖까지 세면 `MiCreateSection` 호출자는 3곳 (`MiCreateSectionCommon +0x200`, `MiCreateSystemSection +0x9d`, `MmCreateSpecialImageSection +0xcf`), `MiCreateImageOrDataSection` 도 3곳 (`MmCreateSectionEx +0x16a`, `MiCreateSection +0xf3`, `MmCreateCacheManagerSection +0xae`).

**test.exe 처음 실행 때 `MiCreateImageFileMap` 까지 갈 것인가: "실행 때 값".**
- 갈림길 1 (`SEC_IMAGE`): 예 `[정적]`.
- 갈림길 2 (컨트롤 영역이 이미 있나): 정적으로 못 정한다. 이 부팅에서 test.exe 파일(같은 파일 객체의 `SectionObjectPointer`)로 이미지를 만든 적이 없고 이전 컨트롤 영역이 남아 있지 않으면 → 새로 만든다 → 간다. 남아 있으면 → 안 간다. 컨트롤 영역이 프로세스가 끝난 뒤에도 얼마나 남는지는 이 요청에서 안 봤다 `[추측]`.

### 2. `MmCreateSpecialImageSection` 이 "특별한" 이유

**답: 사용자 모드 `NtCreateSection` 의 몸통과 거의 같은 모양인데, 새 프로세스용으로 토큰·서명 수준을 인자로 받고 핸들을 돌려주는 커널 안 버전이다.**

인자 6개 (`NtCreateUserProcess +0x916 ~ +0x937` 에서 넘기는 모양 + 함수 안 사용처로 읽음):

| # | 레지스터/스택 | 값 (호출자) | 어디로 가나 |
|---|---|---|---|
| 1 | `RCX` → `R14` | 섹션 핸들을 쓸 곳 (`[RBP+0x260]`) | 맨 끝 `+0x14f` 에서 씀 |
| 2 | `RDX` → `R13` | `[RBP+0xb8]` — 파일을 열 때 쓴 `OBJECT_ATTRIBUTES`(`NtCreateUserProcess +0x55d` 에서 길이 `0x30` 을 적는 칸, `+0x6b6` `IopCreateFile` 의 인자와 같은 칸) | `MiCreateSection` 2번째 → 패킷 `+0x08` |
| 3 | `R8` → `R12` | `[RBP+0x40]` — `SeDuplicateTokenAndAddOriginClaim`/`[RBP+0x240]` 에서 온 값 (토큰으로 보임 `[추측]`) | `MiCreateSection` 7번째 → 패킷 `+0xa0` → `MiCreateNewSection` 조각의 `MiValidateSectionSigningPolicy` 5번째 인자 |
| 4 | `R9B` → `R15B` | 서명 수준 바이트 (`NtCreateUserProcess` 의 `R13B`) | `MiCreateSection` 8번째 → 패킷 `+0x18` |
| 5 | `[RSP+0xd0]` → `RSI` | 연 파일 핸들 (`[RBP+0x250]`, `IopCreateFile` 의 결과) | `MiCreateSection` 9번째 → 패킷 `+0x28` |
| 6 | `[RSP+0xd8]` → `EDI` | 플래그 (`[RBP+0x38]`, 위 `+0x7bc`) | bit0·bit2·bit3 로 할당 속성과 `MiCreateSection` 6번째(`EBX`)를 만든다 |

일반 길과 다른 점:
1. **토큰·서명 수준을 인자로 받는다.** 사용자 모드 `NtCreateSection → MiCreateSectionCommon (0x8c61c4)` 는 `+0x179` (ret `+0x17e`) `PsReferencePrimaryTokenWithTag`(현재 프로세스) 로 토큰을 구하고, `+0x16a` `[프로세스+0x5f9]` (`_EPROCESS.SectionSignatureLevel`, PDB 확인) 에서 서명 수준을 읽는다. 특별한 쪽엔 이 두 호출이 **없고**, 대신 3번·4번 인자를 같은 자리(`MiCreateSection` 의 7번째·8번째)에 그대로 넘긴다. 이유 `[추측]`: 만들려는 프로세스가 아직 없어서, 부모가 아니라 **새 프로세스의 토큰·서명 수준으로** 이미지를 검사하려는 것.
2. **사용자 인자 처리가 없다.** `MiCreateSectionCommon` 에 있는 사용자 포인터 읽기·`MiCaptureSectionCreateExtendedParameters` (`+0x130`) 가 없다. 할당 속성은 항상 이미지, 페이지 보호는 `0x10` (`+0xb4` `LEA R9D,[RAX+0x10]`), 최대 크기 없음 (`+0xa1` `R8D=0`), 파일 객체 없음 (핸들만, `+0xaf` `[RSP+0x48]=0`), 접근 모드 0 = 커널 (`+0xab` `[RSP+0x50]=AL=0`).
3. **`+0x64` (ret `+0x69`) `Feature_TrustedLaunchHosts…IsEnabledDeviceUsageNoInline`**: 이 기능이 켜져 있고 플래그의 bit3(0x8)가 켜져 있으면 6번째 인자(`EBX`)에 `0x20` 을 더한다 (`+0x73`). `MiInitializeCreateSectionPacket +0x1a5` 에도 같은 검사가 있어 패킷 플래그 `0x8000000` 로 이어진다 (`+0x373`). 이 기능이 무엇인지는 이름 말고는 안 읽었다.
4. **`+0x86` (ret `+0x8b`) 세션 id**: 현재 스레드의 프로세스 (`GS:[0x188]` → `+0xb8`) 의 세션 id 를 `PsGetProcessSessionIdEx` 로 (이 주소엔 PDB 이름이 둘이다 — `.asm` 의 `ALIASES` 줄 `PsGetSessionIdEx`; 어느 쪽이 쓰임새에 맞는지는 안 가렸다) 구해 `MiCreateSection` 12번째 인자 → 패킷 `+0xa8` 에 넣는다. `MiCreateSectionCommon +0x199` 도 같은 호출을 한다 (대상 프로세스만 다름). 패킷 `+0xa8` 은 `MiCreateNewSection` 조각에서 `MiValidateSectionSigningPolicy` 의 4번째 인자로 간다.
5. **`+0xea` `KeDelayExecutionThread` 재시도 고리**: `MiCreateSection` 이 `0xc0000054` 를 돌려주면 `MiHalfSecond`(전역 이름, 값은 안 읽음) 만큼 쉬고 같은 호출을 다시 한다 (`+0xdf ~ +0xef`). `MiCreateSection` 도 안에서 같은 `0xc0000054`·`0xc0000476` 이면 패킷을 다시 만들어 도는데 (`+0xfa ~ +0x105`, `+0x21a ~ +0x237`), 어떤 경우에 위까지 올라오는지는 안 따졌다. `MiCreateSectionCommon +0x205 ~ +0x222` 에도 같은 고리가 있다 — 이 고리는 "특별해서"가 아니라 일반 길과 같다.
6. **`+0xf6 ~ +0x11c`**: 섹션 객체의 컨트롤 영역 (`MiSectionControlArea`) → 그 파일 (`MiReferenceControlAreaFile`) → **`CcZeroEndOfLastPage`** (파일 마지막 페이지에서 파일 끝 뒤를 0 으로) → `MiDereferenceControlAreaFile`. `MiCreateSectionCommon +0x2e8 ~ +0x314` 와 같은 순서다.
7. **`+0x141` (ret `+0x146`) `ObInsertObjectEx`**: `DesiredAccess = 0xf001f` (`SECTION_ALL_ACCESS` 모양), 객체를 핸들 표에 넣고 새 핸들을 `[RSP+0x78]` 로 받는다. 성공 (`+0x148` `JS` 안 탐) 이면 `+0x14f` 에서 호출자의 `[RBP+0x260]` 에 쓴다.

**돌려주는 것: 섹션 "핸들"** (객체 포인터 아님). 객체는 `NtCreateUserProcess +0x96d` (ret `+0x972`) `ObReferenceObjectByHandle` 로 다시 얻어 `[RBP+0x58]` → `[RBP+0x270]` 에 둔다 (run10 과 맞음).

**커널 안 일반 길 `MmCreateSection (0x96cfd0)`**: 38줄짜리 얇은 껍데기로 `MmCreateSectionEx` (`+0x67`) 만 부른다. `MmCreateSectionEx` (0x96d0ec) 는 `MiCreateSection` 을 **거치지 않고** `MiInitializeCreateSectionPacket` (`+0x149`) 와 `MiCreateImageOrDataSection` (`+0x16a`) 를 직접 부르고, 객체를 돌려준다 (`ObInsertObjectEx` 호출 없음). 지도에서 `MmCreateSection` 이 이번 길과 관계없다고 한 건 맞다.

### 3. `MiCreateImageFileMap` 이 하는 일

**답: 5묶음.** (함수 전체 = 2916바이트 + 멀리 떨어진 조각들.)

| 묶음 | 하는 일 | 대표 줄 (call → ret) |
|---|---|---|
| A. 준비 | 파일 크기를 얻고 (4GB 넘으면 거절 `+0x9e`), 헤더 한 페이지짜리 버퍼 MDL 을 만들고, 파일 끝 정리, 파일의 데이터 섹션을 디스크로 내린다 | `FsRtlGetFileSize` `+0x91`→`+0x96`, `MiCreateMdl` `+0xd6`→`+0xdb`, `CcZeroEndOfLastPage` `+0xf6`→`+0xfb`, `MiFlushDataSection` `+0x103`→`+0x108` |
| B. **헤더 읽기** | 이미 메모리에 있으면 복사, 없으면 디스크에서 읽는다 | `MiCopyHeaderIfResident` `+0x157`→`+0x15c` (0 이면 `+0x15e` 에서 `+0x411` 로), 못 찾으면 `MiReadImageHeaders` `+0x4fc`→`+0x501` |
| C. **헤더 검사** | `MZ`, `e_lfanew` 범위를 여기서 보고, 나머지 PE 검사는 `MiVerifyImageHeader` 에 맡긴다. 그 뒤 이미지 크기·섹션 표 범위 | `cmp word [rdx],0x5a4d` `+0x17f ~ +0x18b`, `MiVerifyImageHeader` `+0x1f1`→`+0x1f6` |
| D. **조각(프로토 PTE) 만들기** | 컨트롤 영역·세그먼트·서브섹션·프로토 PTE 배열을 풀에 만들고, 이미지 정보 칸을 채우고, 섹션 표를 훑어 서브섹션을 만든다 | `MiBuildImageControlArea` `+0x2e1`→`+0x2e6` (안: `MiAllocatePool`×3, `MiParseImageSectionHeaders` `+0x362`) |
| E. 뒷정리·옵션 | 이미지 정보 `SizeOfHeaders` 기록, 파티션 번호 기록, 커밋 청구, (조건부) 프로토 PTE 에 헤더 복사본 / 헤더 페이지 꼬리 0 채우기, 결과 돌려주기 | `MiChargeCommit` `+0x5c6`, `MiInitializeImageProtos` `+0x3dc`→`+0x3e1` (조건부), `MiInitializeImageHeaderPage` `+0x3f6`→`+0x3fb` (조건부), 결과 `+0x435 ~ +0x443` |

멀리 떨어진 조각 (`+0x1b40b3 ~ +0x1b42f6`, 요청서가 물은 것): **헤더가 처음 읽은 한 페이지보다 크거나, 섹션 표가 그 밖으로 나갈 때 더 읽는 갈래**다. `+0x1b40ae`(ret `+0x1b40b3`)·`+0x1b4202`(ret `+0x1b4207`) `MiCreateMdl` 로 더 큰 버퍼를 만들고, `MiAllocateEntireImageFileExtents` (`+0x1b4110`, `+0x1b424b`) 와 `MiReadImageHeaders` (`+0x1b4146`, `+0x1b4286`) 로 다시 읽는다. `+0x1b42f1` (ret `+0x1b42f6`) `MiLegacyImageArchitecture` 는 `SectionAlignment` 가 0x1000 보다 작은 옛 이미지 갈래 (`+0x2b5`·`+0x73f`) 에서만 간다. **test.exe 는 이 갈래를 안 탄다** `[정적, test.exe 헤더 값 대입]`: `e_lfanew=0x108` → NT 헤더 끝 `0x210`, 섹션 6개 → 표 끝 `0x300`, `SizeOfHeaders=0x400`, `SectionAlignment=0x1000` 이 모두 첫 페이지(0x1000) 안이다.

**읽는 곳 vs 검사하는 곳**
- **읽는 곳**
  - `MiCopyHeaderIfResident` (rva `0x482b64`, 473바이트): 파일 객체의 `SectionObjectPointer` 로 컨트롤 영역을 잠그고 (`+0x27` `MiLockSectionControlArea`), 첫 페이지가 아직 메모리에 있고 내용이 멀쩡하면 (`+0xc5` `MiStandbyPageContentsIntact`) 복사한다 (`+0x191` `MiCopyPage`). 없으면 0 을 돌려주고 디스크에서 읽는다.
  - `MiReadImageHeaders` (rva `0x9714cc`, 492바이트): `MiPageRead` (`+0x9e`) 로 헤더 범위를 읽고, 끝나길 기다리고 (`+0xc1`, Ghidra 별칭 `KeWaitForMutexObject` = `KeWaitForSingleObject`), 읽은 길이가 모자라면 나머지를 0 으로 채운다 (`+0xe5`).
- **검사하는 곳**: 두 군데로 나뉜다.
  - `MiCreateImageFileMap` 안: 파일 크기 상위 32비트가 0 아님 → `0xc0000020` (`+0x9e`) / **`MZ`(`0x5a4d`)** 아님 → `0xc000012f` (`+0x188` `JNZ`, `+0x6bc`) / `e_lfanew + 0x108` 이 넘치거나 (`+0x198`) 파일 크기를 넘으면 (`+0x1b9`) `0xc0000130`.
  - **`MiVerifyImageHeader` (rva `0x9707fc`, 1443바이트)**: 인자는 (`RCX` 칸 모음 `ctx`, `RDX` NT 헤더, `R8` 파일 맨 앞 버퍼, `R9` 남은 크기). 대표 검사 (실패 이유 번호를 `[0x140e2d838]` 에 적고 `0xc000007b` 또는 `0xc0000130` 을 돌려줌):

| 검사 | 줄 | 이유 번호 |
|---|---|---|
| NT 헤더 주소 4바이트 정렬 | `+0x18` `TEST DL,3` | 0x3b |
| **서명 `PE\0\0`** | `+0x21` `CMP [RDX],0x4550` (아니면 `MiComputeBadImageHeaderType` `+0x29a`→`+0x29f` 로 어떻게 틀렸는지 분류) | |
| 실행 가능 이미지 비트 (`Characteristics & 2`) | `+0x39` | 0x47 |
| **`Magic` = `0x10b`(PE32) 또는 `0x20b`(PE32+)** | `+0x1ae ~ +0x1c7` | 0x49 |
| 파일 정렬: `0x200` 배수이거나 `SectionAlignment` 와 같아야 / 0 아님 / 2의 거듭제곱 | `+0x1cd ~ +0x1f9` | 0x4c, 0x4d, 0x4f |
| 섹션 정렬: 2의 거듭제곱, 파일 정렬보다 작지 않음 | `+0x1e5 ~ +0x201` | 0x4e, 0x50 |
| `SizeOfImage` ≤ `0x77000000` | `+0x20b` | 0x51 |
| **머신**: PE32 면 `0x14c`·`0x1c4`, PE32+ 면 `0x8664`·`0xaa64` | `+0x217 ~ +0x234`, `+0x3bc ~ +0x3dd`, `+0x523 ~ +0x531` | 0x52, 0x53 |
| `SizeOfHeaders` < `SizeOfImage` | `+0x23a` | 0x54 |
| `ImageBase` 가 64KB 정렬 (하위 16비트 0) | `+0x244` `CMP word [R11],0` | 0x55 |
| `SizeOfOptionalHeader` 가 8의 배수 | `+0x24e` `TEST byte [R10+0x14],7` | 0x56 |
| 옛 아키텍처·`DllCharacteristics` 검사 | `+0x263` `MiLegacyImageArchitecture` (ret `+0x26c`) 뒤 | 0x57, 0x58, 0x59 (내용은 안 풀었음) |

  - 같은 함수가 **검사하면서 칸을 채운다** (`ctx`, 0x70바이트 — 아래 질문 4 표). PE32 와 PE32+ 두 벌이다 (`Magic` 로 갈림: `+0x66` `CMP CX,SI`, PE32+ 가 `+0x6f ~ +0x1a0`, PE32 가 `+0x2aa ~ +0x3b7`).
  - test.exe 헤더 값을 대입하면 위 검사를 전부 통과한다 `[정적]`: `Magic=0x20b`, `Machine=0x8664`, `FileAlignment=0x200`, `SectionAlignment=0x1000`, `SizeOfImage=0x8c000`, `SizeOfHeaders=0x400`, `ImageBase=0x140000000`, `SizeOfOptionalHeader=0xf0`, `Characteristics=0x22`, `DllCharacteristics=0x8160` (`ms/bin/test.exe` 를 직접 읽음).

**섹션 표대로 이미지 조각을 만드는 곳과 "메모리에 올리나": 대체로 아니오, 헤더 한 페이지만 예외.**
- `MiBuildImageControlArea` (rva `0x970fc4`, 1278바이트): 풀 3개를 만든다 — 컨트롤 영역+서브섹션 (`+0x8d` `MiAllocatePool`, 크기 `0xc8 + 0x38 × 섹션수+1`), 프로토 PTE 배열 (`+0xdb`, 페이지 수 × 8), 세그먼트 (`+0x107`, 0x98바이트). `+0x2fc` (ret `+0x301`)·`+0x403` (ret `+0x408`) `MiMakeSubsectionPte` 로 PTE 를 "서브섹션 모양"으로 채우고, `+0x410` 은 `MiMakeDemandZeroPte`, `+0x362` (ret `+0x367`) `MiParseImageSectionHeaders` 가 섹션 표를 훑어 서브섹션별 PTE 를 채운다. 마지막 `+0x37b` `MiUpdateSystemProtoPtesTree`.
  - 이 PTE 들은 "이 조각은 파일의 이 자리" 라는 표시이지 읽어 둔 페이지가 아니다 `[추측]` (이름 `MiMakeSubsectionPte`·`MiMakeDemandZeroPte` 기준). 코드 구역에서 파일 내용을 읽어 오는 호출은 이 함수에 없다.
- 예외 둘:
  1. 헤더 한 페이지는 물리 메모리에 이미 읽혀 있다 (위 B 묶음, `MiCreateMdl` 버퍼). 이 페이지는 `MiCreateNewSection +0x162` (ret `+0x167`) `MiSectionCreated` 안 `+0xfe` (ret `+0x103`) `MiInitializeNewImageSectionProtos` 로 프로토 PTE 쪽에 넘어간다 `[추측]` (이 함수 내용은 안 읽음). `MiInitializeImageHeaderPage` (rva `0x3c155c`, 142바이트) 는 그 페이지에서 `SizeOfHeaders` 뒤 꼬리를 0 으로 채운다 (`+0x2b` `MiMapPageInHyperSpaceWorker`, `+0x42` `memset`). 조건: 패킷 플래그 `&0x104000` 이 0 이고, 파일 전체 버퍼(`[RBP-0x40]`)가 없고 MDL 이 하나뿐이고, `SizeOfHeaders < 0x1000` (`MiCreateImageFileMap +0x333 ~ +0x36d`) → test.exe(`0x400`)는 `SizeOfHeaders` 쪽은 해당 `[정적 조건, 나머지는 실행 때 값]`.
  2. `MiInitializeImageProtos` (rva `0x315cdc`, 874바이트, `+0x3d1 ~ +0x3dc` 에서 `[RBP-0x40]` 이 0 이 아닐 때만) 는 파일 전체를 임시 버퍼에 읽어 둔 **작은 이미지**용이다: 페이지를 받아 (`+0x12f` `MiGetPageForHeader`) 내용을 복사하고 (`+0x18e` `KeCopyPage`) 전환(transition) 상태로 만든다 (`+0x245` `MiInitializeTransitionPfn`). `[RBP-0x40]` 을 채우는 조건이 "헤더가 메모리에 없어 디스크에서 읽는 길(`+0x15e` → `+0x411`)로 왔고, 패킷 플래그 `0x20` 이 켜졌거나 (플래그 `0x200` 이면서 파일 크기 ≤ `0x20000`), 그 파일에 데이터 섹션이 없을 때" (`+0x4c1 ~ +0x573`) 이므로, 551,424바이트인 test.exe 는 플래그 `0x20` 이 켜져 있지 않으면 **안 탄다** `[정적 조건, 플래그는 실행 때 값]`.
- **프로세스 주소 공간에는 올리지 않는다** `[정적]`. 이 사슬 어디에도 `MmMapViewOfSection*` 이나 `MiMapViewOfImageSection` 이 없다. 단 `MiRelocateImage` 는 재배치 정보를 훑으려고 시스템 주소 공간에 잠깐 매핑한다 (`+0x1fc` `MiMapImageInSystemSpace` ~ `+0x4fa` `MiUnmapImageInSystemSpace`) — 프로세스 것이 아니다.
- **실제 매핑은 나중이다, 코드의 call 자리로 확인**: `MmInitializeProcessAddressSpace +0x1c9` (ret `+0x1ce`) → `MiMapProcessExecutable` → `+0xb3` (ret `+0xb8`) `MmMapViewOfSectionEx` (`_calls.tsv`). 앞의 `[추측]` 이 `[정적]` 이 된다 (이 두 함수 안은 이번에 안 읽음).

### 4. 진입점 → `TransferAddress` 처음 출처

**답: `MiBuildImageControlArea +0x16b ~ +0x172` 가 `ImageBase + AddressOfEntryPoint` 를 이미지 정보 `+0x0` 에 쓴다. 그 값의 헤더 쪽 출처는 `MiVerifyImageHeader +0x79 ~ +0x8a`.**

| 단계 | 어디 | 무엇 | 근거 |
|---|---|---|---|
| ① | 파일 | NT 헤더 `OptionalHeader.AddressOfEntryPoint` = NT 헤더 시작 `+0x28`, `ImageBase` = `+0x30` (PDB `_IMAGE_OPTIONAL_HEADER64` `+0x10`, `+0x18` + `OptionalHeader` `+0x18`). test.exe: `0x1d40` / `0x140000000` | `ms/bin/test.exe` 를 직접 읽음 |
| ② | `MiVerifyImageHeader` (PE32+) | `ctx +0x0` ← `ImageBase` (`+0x79` `MOV RAX,[RDX+0x30]`, `+0x7d` `MOV [R11],RAX`). **`ctx +0x1c` ← `AddressOfEntryPoint`** (`+0x87` `MOV EAX,[RDX+0x28]`, `+0x8a` `MOV [R11+0x1c],EAX`). PE32 갈래는 `+0x2b1`·`+0x2be ~ +0x2c1` | `MiVerifyImageHeader.asm`, 원본 바이트 `48 8b 42 30 49 89 03 … 8b 42 28 41 89 43 1c` |
| ③ | `MiBuildImageControlArea` | **`+0x16b` `MOV EAX,[R15+0x1c]` · `+0x16f` `ADD RAX,[R15]` · `+0x172` `MOV [R8],RAX`** — `R15 = ctx`, `R8 = 이미지 정보`. 원본 바이트 `41 8b 47 1c 49 03 07 49 89 00` | 이 값이 **`_SECTION_IMAGE_INFORMATION.TransferAddress`**. 이 시점엔 아직 헤더의 `ImageBase` 기준 |
| ④ | `MiRelocateImage` 조각 (`MiCreateNewSection +0x3d5` ret `+0x3da` 에서 부름) | **`+0x1b1271` `MOV R15,[RSP+0xc0]` · `+0x1b1279` `ADD [R15],RAX`** — `[RSP+0xc0]` = 세그먼트 `+0x38` 이 가리키는 이미지 정보, `RAX` = 새로 고른 기본 주소 − 헤더 기본 주소 (`+0x32f ~ +0x33a`: `[RSP+0x58]`(`MiSelectImageBase` `+0x2e6` 결과) − `[RSP+0x88]`(세그먼트 `+0x20` `BasedAddress`)). 같은 조각이 `ImageFlags` bit2 `ImageDynamicallyRelocated`(`+0x33`) 를 켠다 (`+0x1b1357` `OR byte [R15+0x33],4`). 원본 바이트 `4c 8b bc 24 c0 00 00 00 49 01 07` | 새 기본 주소가 헤더 것과 다를 때 `TransferAddress` 를 그만큼 옮긴다 |
| ⑤ | `MmGetSectionInformation` (run16) | 세그먼트 `+0x38` 의 이미지 정보 0x40바이트를 호출자 버퍼로 복사 (`+0x84` `MOV R8,[RAX+0x38]`, `+0x88 ~ +0xbe` `MOVUPS`) | `PspAllocateProcess +0x98c` (ret `+0x991`) |

- 이미지 정보가 사는 곳: `MiBuildImageControlArea` 가 만든 세그먼트(0x98바이트)의 `+0x48` 쪽 (`+0x123` `LEA R8,[R12+0x48]`). 세그먼트 `+0x38` (PDB `_SEGMENT.u2.ImageInformation`) 이 그 주소를 가리킨다 (`+0x13e` `MOV [R12+0x38],R8`). 크기 80바이트 `_MI_SECTION_IMAGE_INFORMATION` = 앞 0x40 `ExportedImageInformation`(`_SECTION_IMAGE_INFORMATION`) + 뒤 0x10 `InternalImageInformation`(`_MI_EXTRA_IMAGE_INFORMATION`).
- `MiBuildImageControlArea` 가 채우는 칸 (`R8` = 이미지 정보, 오른쪽 = `ctx` 칸 ← 헤더). PDB 타입으로:

| 이미지 정보 칸 | 값 |
|---|---|
| `+0x0` `TransferAddress` | `ImageBase + AddressOfEntryPoint` (`+0x16b ~ +0x172`) |
| `+0x10` `MaximumStackSize` / `+0x18` `CommittedStackSize` | `SizeOfStackReserve` / `SizeOfStackCommit` |
| `+0x20` `SubSystemType` | `Subsystem` |
| `+0x24` `SubSystemMinorVersion` / `+0x26` `SubSystemMajorVersion` | `Minor/MajorSubsystemVersion` |
| `+0x28` `MajorOperatingSystemVersion` / `+0x2a` `Minor…` | `Major/MinorOperatingSystemVersion` |
| `+0x2c` `ImageCharacteristics` / `+0x30` `Machine` | 파일 헤더 `Characteristics` / `Machine` |
| `+0x2e` `DllCharacteristics`, `+0x3c` `CheckSum`, `+0x34` `LoaderFlags` | 선택 헤더 값 |
| `+0x32` `ImageContainsCode` | `SizeOfCode` 가 0 이 아니거나 진입점이 0 이 아니면 1 |
| `+0x38` `ImageFileSize` | 파일 크기 하위 32비트 (`MiCreateImageFileMap` 의 `FsRtlGetFileSize` 결과) |
| `+0x40` `SizeOfHeaders`, `+0x44` `SizeOfImage`, `+0x48` `TimeDateStamp` (`_MI_EXTRA_IMAGE_INFORMATION`) | 선택 헤더 값, 파일 헤더 `TimeDateStamp` |

- `ctx` (`MiVerifyImageHeader` 가 채우는 0x70바이트) 칸 이름: `+0x00` `ImageBase`, `+0x08` `FileAlignment`, `+0x0c` `SectionAlignment`, `+0x10` `SizeOfImage`, `+0x14` `LoaderFlags`, `+0x18` `SizeOfHeaders`, **`+0x1c` `AddressOfEntryPoint`**, `+0x20` `SizeOfStackReserve`, `+0x28` `SizeOfStackCommit`, `+0x30` `Magic`, `+0x32` `Subsystem`, `+0x34`/`+0x36` 부시스템 버전, `+0x38`/`+0x3a` OS 버전, `+0x3c` `DllCharacteristics`, `+0x40` `CheckSum`, `+0x44` `SizeOfCode`, `+0x48/+0x4c` IAT 디렉터리, `+0x50/+0x54` COM 디렉터리, `+0x58/+0x5c` 재배치 디렉터리, `+0x60/+0x64` 로드 구성 디렉터리, `+0x68/+0x6c` 디버그 디렉터리 (디렉터리 번호는 오프셋으로 맞춰 봄. `MiRelocateImage +0xa1` 이 `ctx +0x58/+0x5c` 를 재배치 디렉터리로 읽는 것과 맞는다).
- run16 이 본 `PspAllocateProcess +0x20b3 ~ +0x20ce` 보정은 **이 값(④까지 반영된 값)이 프로세스에 실제로 올라간 주소와 다를 때** 쓰는 것으로 보인다 `[추측]` (이번엔 `PspAllocateProcess` 를 다시 안 읽음).
- **test.exe 에서 ④ 가 실제로 더해지는가는 실행 때 값.** test.exe 는 `DllCharacteristics=0x8160` (`DYNAMIC_BASE`+`HIGH_ENTROPY_VA`) 이고 재배치 디렉터리가 있다 (rva `0x8b000`, 크기 `0x810`). run7 `[관찰]` 의 모듈 베이스 `0x7ff70ee10000` 은 헤더의 `0x140000000` 과 달라서 어디선가는 옮겨진다.
- 이 사슬에서 `TransferAddress` 를 쓰는 줄은 ③ 과 ④ 둘이다. 이번에 안 읽은 함수(`MiParseComAndCetHeaders`, `MiMakeImageReadOnly`, `MiInitializeNewImageSectionProtos`, `MiValidateSectionSigningPolicy`, `MiReleaseImageSection`, `MiFinalizeImageHeaderPage`)에 같은 칸을 쓰는 줄이 더 있는지는 모른다 `[추측: 이름으로는 아닐 것 같음]`.

### 5. 서명 재시도 고리 (sast/03)

**답: 이번 대상 안에서 돌려주는 상태값 때문이 아니다.** 한 줄로:
- 고리는 `NtCreateUserProcess` 쪽에서 돈다: 섹션을 만든 뒤 `+0x990` (ret `+0x995`) `PspGetProcessProtectionRequirementsFromImage` 와 `+0x9d8` (ret `+0x9dd`) `SeQuerySigningPolicy` 로 알아낸 서명 수준 `BL` 이 처음 넘긴 `R13B` 와 다르고 (`+0x9ea` `CMP BL,R13B`), `SeCompareSigningLevels(BL, R13B)` (`+0xa08`, ret `+0xa0d`) 가 0 이 아니면 → `ObCloseHandle` (`+0xa1e`) → `ObfDereferenceObject` (`+0xa2a`) → `R13B = BL` (`+0xa3d`) → `+0x912` 로 돌아가 `MmCreateSpecialImageSection` 을 `R9B = 새 수준` 으로 다시 부른다.
- 이번 사슬은 그 `R9B` 를 나를 뿐이다: `MmCreateSpecialImageSection` 4번째 인자 → 패킷 `+0x18` → `MiCreateNewSection +0x2ee` (ret `+0x2f3`) `SeGetImageRequiredSigningLevel` 의 결과로 패킷 `+0x18` 을 다시 덮어씀 (`+0x318` `MOV [RBX+0x18],R12B`, 패킷 플래그 `0x400` 이 켜져 있을 때 — 이 플래그는 `MmCreateSpecialImageSection` 이 플래그 bit0 를 켜서 만든 것: `MiInitializeCreateSectionPacket +0x2c9` `BTS [RCX],0xa`).
- 재시도가 몇 번 도는지는 실행 때 값 (Q34).

### 6. points.md 단계 3 판정

**예, `nt!MiCreateImageFileMap` 을 대표 지점으로 둬도 된다. 조건 둘을 붙인다.**
1. **이 함수는 "이 파일의 이미지 컨트롤 영역이 아직 없을 때만" 선다** (질문 1). 같은 부팅에서 test.exe 를 이미 한 번 돌렸다면 안 설 수 있다. 입구에서 `rcx` (파일 객체 `_FILE_OBJECT`) 의 `FileName` 이 test.exe 일 때만 멈추게 하고, 안 서면 `MiCreateNewSection` 입구 / `MiShareExistingControlArea` 입구 중 어느 쪽이 서는지 본다.
2. **"읽는" 일과 "값이 만들어지는" 일은 이 함수 안의 더 작은 함수에 있다.** 읽기 = `MiCopyHeaderIfResident`/`MiReadImageHeaders`, 검사 = `MiVerifyImageHeader`, 진입점 값 = `MiBuildImageControlArea +0x172`. 대표 지점(함수 입구)으로는 `MiCreateImageFileMap` 하나가 "읽기·검사·조각 만들기" 한 묶음을 대표하니 맞다. 더 나은 대표 함수(예: `MiVerifyImageHeader`)는 아니다 — 그 함수는 검사만 하고 `AddressOfEntryPoint` 를 복사만 한다.
- 권고: 단계 3 에 **값이 바뀌는 순간** 한 곳을 더한다: **`nt!MiBuildImageControlArea +0x172`** (`TransferAddress` 가 처음 적히는 줄). 함수 입구가 아니라 이 줄이다.
- points.md 문구: `[확인 필요]` → `[정적] 사슬 확인 (NtCreateUserProcess → … → MiCreateImageFileMap). 실행 때는 [확인 필요]` 로.

## 이어진 사슬 (대표 지점만, ret 기준)

`NtCreateUserProcess +0x93c`
→ `MmCreateSpecialImageSection` (`+0xd4` `MiCreateSection`, 앞서 `+0x69` Feature 검사·`+0x8b` 세션 id)
→ `MiCreateSection` (`+0xcb` `MiInitializeCreateSectionPacket` → `+0xf8` `MiCreateImageOrDataSection`; 이어서 `+0x125` `MiFinishCreateSection`)
→ `MiCreateImageOrDataSection` (`+0x1a3` `MiReferenceControlArea` → **갈림길**: `+0x1d5` `MiShareExistingControlArea` / `+0x385` `MiCreateNewSection`)
→ `MiCreateNewSection` (`+0x215` `MiCreateImageFileMap` → `+0x167` `MiSectionCreated` → `+0x3da` `MiRelocateImage`)
→ `MiCreateImageFileMap` (`+0x96` 파일 크기 → `+0x15c` `MiCopyHeaderIfResident` / `+0x501` `MiReadImageHeaders` → `MZ` `+0x188` → **`+0x1f6` `MiVerifyImageHeader`** → **`+0x2e6` `MiBuildImageControlArea`**)
→ `MiVerifyImageHeader` (`+0x8a` `AddressOfEntryPoint` → `ctx+0x1c`)
→ `MiBuildImageControlArea` (**`+0x172` `TransferAddress` = `ImageBase + rva`**)
→ (돌아와) `MiRelocateImage` 조각 (**`+0x1b1279` `TransferAddress += 델타`**)
→ (돌아와) `MmCreateSpecialImageSection +0x146` `ObInsertObjectEx` → 핸들 `+0x14f`.

## 줄에서 이번에 채운 칸

```
PE 헤더 AddressOfEntryPoint (파일 NT+0x28, test.exe 0x1d40)
 → [채움] MiVerifyImageHeader +0x87~+0x8a      ctx+0x1c 에 복사 (ImageBase 는 +0x79~+0x7d)
 → [채움] MiBuildImageControlArea +0x16b~+0x172 이미지 정보 +0x0 TransferAddress = ImageBase + rva
 → [채움] MiRelocateImage 조각 +0x1b1279         새 기본 주소를 골랐으면 델타를 더함 (코드에 있음, test.exe 에서 더해지는지는 동적)
 → (run16) PspAllocateProcess +0x991 MmGetSectionInformation   이미지 정보 0x40바이트 복사
 → (run16) PspCreateUserContext  CONTEXT.Rcx
 → (run29) RtlUserThreadStart → BaseThreadInitThunk +0x17 → 진입점 (test+0x1d40)
```

## flow.md 에 넣을 것

**2.1 `nt!MmCreateSpecialImageSection` 절 `[추측]` 판정**: "`MiCreateImageFileMap` 은 이 함수 안쪽인지 모른다" → **`[정적]` 로 고쳐 쓴다**: `MmCreateSpecialImageSection` 이 `MiCreateSection` → `MiCreateImageOrDataSection` → `MiCreateNewSection` → `MiCreateImageFileMap` 으로 이어진다 (ret `+0xd4` / `+0xf8` / `+0x385` / `+0x215`). 단 "항상"은 아니다: 파일의 이미지 컨트롤 영역이 이미 있으면 `MiShareExistingControlArea` 로 가서 `MiCreateNewSection` 을 건너뛴다. 2.1 끝 "남은 것: `MiCreateImageFileMap`" 줄은 지운다. 4장 2.1 줄은 "해결됨".

**그림 1 `s3` 아래 `(정적)` 상자 후보 (대표 지점만, 1 ~ 3개)**
1. `nt!MiCreateImageOrDataSection` `(정적)` — 이미지 컨트롤 영역이 이미 있나? 있으면 공유(`+0x1d5`), 없으면 새로(`+0x385`). 갈림길 상자.
2. `nt!MiCreateImageFileMap` `(정적)` — exe 헤더 읽기·검사 (`MiVerifyImageHeader` `+0x1f6`), 조각 표 만들기 (`MiBuildImageControlArea` `+0x2e6`). 이 상자가 points.md 단계 3.
3. `nt!MiBuildImageControlArea` `(정적)` — `TransferAddress` = `ImageBase + AddressOfEntryPoint` (`+0x172`). run30 (진입점 경계)과 이어지는 상자.

(세 상자는 `s3` → 1 → 2 → 3 순서의 한 줄 사슬. `MiRelocateImage` 의 델타 더하기는 3 의 주석 한 줄로 충분.)

## 동적분석으로 확인해야 할 것

**새 dast 항목 제안: Q61 "이미지 섹션 만들기 길 + `TransferAddress` 값"** (한 번의 실행으로 아래를 같이 보는 것을 권한다). `nt+rva`, test.exe 일 때만 서도록 조건은 dast-mentor 몫 (`_FILE_OBJECT.FileName`).

| # | 멈춤점 | 볼 것 | 기대 (정적) |
|---|---|---|---|
| D1 | `nt+0x96f4d0` (`MiCreateNewSection` 입구) 와 `nt+0x96e6cc` (`MiShareExistingControlArea` 입구) | 어느 쪽이 서나. 입구에서 `rcx` = 패킷, `poi(@rcx+0x38)` = `_FILE_OBJECT` (패킷 `+0x38`). 새로 만드는 쪽이면 ret `+0x385` | 이 부팅 첫 실행이면 `MiCreateNewSection`. 이미 돌린 적 있으면 `MiShareExistingControlArea` (그땐 D2~D5 가 안 선다. 확인하려면 재부팅 직후 첫 실행, 또는 다른 파일로 복사해서 실행) |
| D2 | `nt+0x96ff50` (`MiCreateImageFileMap` 입구) | `k` — `MiCreateNewSection+0x215` ← `MiCreateImageOrDataSection+0x385` ← `MiCreateSection+0xf8` ← `MmCreateSpecialImageSection+0xd4` ← `NtCreateUserProcess+0x93c`. `rcx` = `_FILE_OBJECT` | 위 정적 사슬과 그대로 |
| D3 | `nt+0x970886` (`MiVerifyImageHeader +0x8a`) | `r eax` | `0x1d40` (진입점 rva) |
| D4 | `nt+0x971136` (`MiBuildImageControlArea +0x172`) | `r rax`, `r r8` (이미지 정보 주소). 한 칸 실행 뒤 `dq @r8 L1` | `rax = 0x140001d40` (헤더 기본 주소 + `0x1d40`) |
| D5 | `nt+0xb246bd` (`MiRelocateImage` 조각 `ADD [R15],RAX`) | 서면: `r rax` (델타), `dq @r15 L1` (더하기 전). 한 칸 실행 뒤 `dq @r15 L1` | 델타 = 새 기본 주소 − `0x140000000`. **안 서면**: 이 실행에선 이 길로 델타를 안 더한 것 (그러면 D4 값이 그대로 `PspAllocateProcess` 로 간다) |
| D6 | `nt+0xa02ef9` (`PspAllocateProcess +0x991` ret) 직후 | `dq @r15+0x38 L1` (run16: `MmGetSectionInformation` 의 출력 버퍼 `R15+0x38`) | D4 (+D5) 값과 같으면 그 사이 안 바뀐 것 |

- 겹침: **D6 은 Q35** (`PspAllocateProcess` 실행 길, `+0x20b3 ~ +0x20ce` 보정이 실제로 타는지) 에 붙이면 같은 실행에서 본다. **Q34** (이미지 섹션 재시도): `nt+0x96bd64` (`MmCreateSpecialImageSection` 입구) 를 몇 번 지나는지, ret `nt+0xa01c7c` (`NtCreateUserProcess +0x93c`) 의 `eax` (상태). D1~D5 는 새 항목 (Q61 제안).
- D3·D4·D5 는 `PspAllocateProcess` 보다 앞이므로 같은 `NtCreateUserProcess` 한 번 안에서 D1 → D2 → D3 → D4 → D5 → (섹션 반환) → D6 순서로 선다.

## 읽은 파일 / 만든 파일

- 새로 뽑은 `.asm`·`.c` (21개): `MmCreateSpecialImageSection`, `MiCreateSection`, `MiCreateImageOrDataSection`, `MiCreateNewSection`, `MiCreateImageFileMap`, `MiVerifyImageHeader`, `MiBuildImageControlArea`, `MiReadImageHeaders`, `MiCopyHeaderIfResident`, `MiInitializeImageProtos`, `MiInitializeImageHeaderPage`, `MiInitializeCreateSectionPacket`, `MiReferenceControlArea`, `MiShareExistingControlArea`, `MiSectionCreated`, `MmCreateSection`, `NtCreateSection`, `MiRelocateImage`, `MiParseImageSectionHeaders`, `MmCreateSectionEx`, `MiCreateSectionCommon` → `ms/disassemble/ntoskrnl/<이름>.asm`, `ms/decomp/ntoskrnl/<이름>.c`. 호출 표는 `ms/disassemble/ntoskrnl/_calls.tsv` 에 추가.
- 호출자 표: `ms/disassemble/ntoskrnl/_refs/{MiCreateSection,MiCreateImageOrDataSection,MiCreateNewSection,MiCreateImageFileMap,MiVerifyImageHeader,MiBuildImageControlArea}.tsv`.
- 로그: `ms/ghidra/ntoskrnl.export.sast26{a,b,c,d,e}.stdout`.
- `.asm` 전체를 줄 단위로 읽은 것: 요청 6개 + `MiBuildImageControlArea`, `MiInitializeCreateSectionPacket`, `MiReferenceControlArea`, `MiSectionCreated`, `MiInitializeImageHeaderPage`. 호출 표와 일부 줄만 본 것: `MiShareExistingControlArea`, `MiCopyHeaderIfResident`, `MiReadImageHeaders`, `MiInitializeImageProtos`, `MiRelocateImage`(앞부분 + 조각), `MiCreateSectionCommon`. 안 읽은 것: `MiParseImageSectionHeaders`, `MmCreateSectionEx` 본문(호출 표만).
- 이 결과서에는 디컴파일 코드를 붙이지 않았다. 원본 바이트·명령 몇 줄은 오프셋 근거용이다.

## 판정

**분석성공.** 요청서 질문 1~6 모두 정적으로 답했다. 질문 4 는 처음 출처 (`MiBuildImageControlArea +0x172`, 헤더 쪽 `MiVerifyImageHeader +0x8a`) 와 두 번째 쓰기 (`MiRelocateImage` 조각 `+0x1b1279`) 까지 찾았다. 정적으로 못 정한 것은 셋: test.exe 첫 실행이 새로 만드는 쪽인가 공유하는 쪽인가 (질문 1), `MiRelocateImage` 의 델타가 test.exe 에서 실제로 더해지는가 (질문 4), 서명 재시도 횟수 (질문 5). 모두 위 D1 ~ D6 / Q34 로 넘긴다.

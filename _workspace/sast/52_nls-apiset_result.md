# 52 nls-apiset — 정적 분석 결과 (Q108)

- 요청서: `_workspace/sast/52_nls-apiset.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), ntdll (`.../System32/ntdll.dll`), 덤으로 `apisetschema.dll` 의 PE 머리만.
  - 커널 PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준값과 같음 (`ms/sym/ntoskrnl.exe.pdb.info.txt`).
  - ntdll PDB guid+age `C3093720177DA6851519DCFFE8DF53FE1`.
- 분석: 두 모듈 다 `.gpr` + `_index.tsv` + `SetPdb:` 줄이 있어서 **다시 안 걸었다.**
- 뽑은 것 (로그 `ms/ghidra/{ntoskrnl,ntdll}.export.sast52*.stdout`, 전부 `SCRIPT ERROR` 0):
  - 요청서 대로: ntoskrnl `NtGetNlsSectionPtr`·`MmMapApiSetView`·`MiInitializeApiSets` depth 1 (26개, 그 안에 `RtlpInitNlsFileName`·`MmMapViewOfSection`·`MiMapViewOfSection`·`MiInsertInSystemSpace` 등), `RtlpInitNlsSectionName` depth 0. ntdll `RtlInitCodePageTable`·`LdrpFusionManifestCodePages` depth 1 (7개), `LdrpInitializeNlsInfo`·`RtlpInitCodePageTables`·`RtlpInitUppercaseTables`·`LdrpPreprocessDllName` depth 0.
  - **요청서 밖으로 조금 넓힌 것 (이유 포함, 전부 depth 0):**
    - ntoskrnl `ExInitializeNls` + 커널 사본 `RtlpInitCodePageTables`·`RtlpInitUppercaseTables`·`RtlpQueryNlsSystemCodePages`·`RtlNlsInitState`·`RtlInitCodePageTable` — 질문 1 "부팅 때 누가 만드나" 의 caller 사슬을 찾으려고.
    - ntoskrnl `MiInitSystem`·`MmInitSystem`·`InitBootProcessor`·`KiInitializeBootStructures` — 질문 6 의 caller 사슬(어느 단계에서 `MiInitializeApiSets` 가 도나) 확인용.
    - ntoskrnl `PspCreateProcess` — 질문 5 둘째 caller 가 어떤 길인지(마지막 스택 인자) 보려고.
    - ntdll `RtlpLocateActivationContextSection` — 질문 3 에서 "빈 활성화 문맥" 갈래가 실패 코드를 주는지 확인하려고.
  - `refs` (`ms/disassemble/ntoskrnl/_refs/`): `0xe2d818`(길이 0x18), `ApiSetLoadSchemaWithExtensions`, `ApiSetLoadSchemaEx`, `ApiSetComposeSchema`, `MiInitSystem`, `MiInitializeApiSets`, `MmInitSystem`, `PspSiloLoadApiSets`, `PspApiSetCopyToSystemSpace`, `ExInitializeNls`, 커널 `RtlpInitCodePageTables`·`RtlpInitUppercaseTables`·`RtlpQueryNlsSystemCodePages`.
  - **이름 주의**: ntoskrnl 에도 ntdll 과 같은 이름의 함수가 **따로 있다** (`RtlInitCodePageTable` 0x78a980, `RtlpInitCodePageTables` 0x78ad34, `RtlpInitUppercaseTables` 0x78aedc, `RtlpQueryNlsSystemCodePages` 0x78af7c). `ms/disassemble/ntoskrnl/` 쪽 같은 이름 파일은 **커널 사본**이고 `ms/disassemble/ntdll/` 쪽이 유저 사본이다.
  - 값 확인용으로 `pefile` 로 ntoskrnl·ntdll·apisetschema.dll 의 문자열·머리를 읽었다 (스크래치에서만, 파일 안 남김).
- **버전 확인: 일치.**
  - `_calls.tsv` `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess`.
  - ntdll `LdrpInitializeProcess` call `+0x1bb6` / ret **`+0x1bbb`** → `LdrpInitializeGraphRecurse`.
  - 덤으로 `[관찰]` 쪽과도 맞았다: ApiSet 뷰 = 읽기 전용 pagefile 섹션 42쪽 (`ms/logs/04_baggage-read_K1.txt:1373`), `C_949.NLS`·`l_intl.nls` 읽기 전용 파일 뷰 (`04_baggage-read_P1a.txt:1539~1540`) — 아래 답과 같은 모양.

## 스크립트 문제

없음. (`NtGetNlsSectionPtr.asm` 맨 뒤에 `$filt$0` 예외 함수와 "읽지 않은 틈 +0xd5~+0xd9, +0x330~+0x331" 이 붙는데, 둘 다 예외 착지점이라 읽는 데 지장 없었다.)

---

## 먼저 바로잡기 — 질문 4 (자리 오프셋)

**결론: `+0x46`·`+0x90`·`+0xa3` 은 `LdrpInitializeNlsInfo` 가 아니라 `RtlpInitCodePageTables` (ntdll rva `0xec2fc`, 349B = `0x15d`) 의 자리다.** 그리고 그 셋 중 `NtGetNlsSectionPtr` 를 부르는 건 `+0x46` 하나뿐이고, `+0x90`·`+0xa3` 은 **`RtlInitCodePageTable`** 을 부른다.

- PE_emulator 가 적은 숫자는 **call 명령이 있는 자리** (`call_off`) 라서 숫자 자체는 `RtlpInitCodePageTables` / `RtlpInitUppercaseTables` 와 맞는다. 틀린 건 함수 이름이다.
- WinDbg 콜스택의 `함수+0x..` 는 `ret_off` (call 다음 명령) 다. 그래서 둘 다 적는다.
- `LdrpInitializeNlsInfo` (143B = `0x8f`) 안에서 `+0x46` 은 call 이 아니다 (`MOV ECX,[RSP+0x30]` — 매니페스트·레지스트리로 얻은 코드 페이지 번호를 읽는 합류점). 이 함수는 `NtGetNlsSectionPtr` 를 직접 부르지 않는다.

### `NtGetNlsSectionPtr` / `RtlInitCodePageTable` 를 부르는 자리 (ntdll, 전부 `[정적]`)

| PE_emulator 가 적은 자리 | 진짜 함수 | call_off | ret_off | 부르는 것 | 인자 (`.asm` 근거) |
|---|---|---|---|---|---|
| `LdrpInitializeNlsInfo +0x46` | `RtlpInitCodePageTables` | `+0x46` | `+0x4b` | `NtGetNlsSectionPtr` ① | 종류 `0xb` (`+0x43 LEA ECX,[RBX+0xb]`), 둘째 = **ANSI 코드 페이지** (`+0x33 MOV EDX,ESI`), 셋째 0, 넷째 `&GlobalRtlNlsState+0x80` (`+0x39`), 다섯째(스택) 0 |
| (적지 않음) | `RtlpInitCodePageTables` | `+0x6c` | `+0x71` | `NtGetNlsSectionPtr` ② | 종류 `0xb`, 둘째 = **OEM 코드 페이지** (`+0x58 MOV EDX,EDI`), 넷째 `&GlobalRtlNlsState+0x88` (`+0x5f`), 다섯째 0. **ANSI == OEM 이면 이 call 은 건너뜀** (`+0x4f CMP DI,SI` / `+0x52 JZ +0x13c` → `+0x80` 값을 `+0x88` 에 복사) |
| `LdrpInitializeNlsInfo +0x90` | `RtlpInitCodePageTables` | `+0x90` | `+0x95` | **`RtlInitCodePageTable`** ① | (`+0x89 LEA RDX,[GlobalRtlNlsState]`) 첫째 = `+0x80` 칸 값(섹션 뷰 주소), 둘째 = `&GlobalRtlNlsState` (ANSI 쪽 `CPTABLEINFO`) |
| `LdrpInitializeNlsInfo +0xa3` | `RtlpInitCodePageTables` | `+0xa3` | `+0xa8` | **`RtlInitCodePageTable`** ② | 첫째 = `+0x88` 칸 값 (`+0x95 MOV RCX,[...+0x88]`), 둘째 = `&GlobalRtlNlsState+0x40` (OEM 쪽 `CPTABLEINFO`, `+0x9c`) |
| `RtlpInitUppercaseTables +0x3b` | `RtlpInitUppercaseTables` (ntdll rva `0xf9804`, 143B) | `+0x3b` | `+0x40` | `NtGetNlsSectionPtr` ③ | 종류 `0xe` (`+0x38 LEA ECX,[RDX+0xe]`), 둘째 `0` (`+0x29 XOR EDX,EDX`), 셋째 0, 넷째 `&지역변수[RSP+0x48]` (`+0x30`), 다섯째 0. **입구에서 `CX`(= `PEB.UseCaseMapping`) 가 0 이면 이 call 을 건너뜀** (`+0xd TEST CX,CX` / `+0x10 JNZ +0x29`, 0 이면 `+0x12~+0x20` 에서 세 칸을 0 으로) |

- 이 두 함수를 부르는 쪽 (`LdrpInitializeNlsInfo`): `+0x2a`/`+0x2f` `LdrpFusionManifestCodePages` → `+0x3d`/`+0x42` `RtlpQueryNlsSystemCodePages` → `+0x5c`/`+0x61` `RtlpInitCodePageTables` (인자 `CX` = ANSI, `DX` = OEM, `+0x46`~`+0x4a` 에서 읽은 값) → `+0x6d` 꼬리 JMP `RtlpInitUppercaseTables` (인자 `CX` = `PEB+0x350` `UseCaseMapping`, `+0x61 MOVZX ECX,[RBX+0x350]`).
- **run45 의 "`NtGetNlsSectionPtr` ×3 (코드 페이지 표 2 + 대문자 표 1)" 은 "부르는 자리 3곳" 으로는 맞다.** 다만 **실행 횟수는 조건에 따라 다르다.**
  - ANSI == OEM 이면 코드 페이지 쪽은 1번 (`+0x46`), 아니면 2번.
  - `UseCaseMapping` 이 0 이면 대문자 쪽은 0번.
  - 코드 페이지가 `0xfde9` (UTF-8) 이면 (둘 중 하나라도) `+0x21 CMP SI,BP` / `+0x24 JZ +0x130` 으로 **섹션을 아예 안 얻는다.**
  - `[관찰]` 기존 사진과 맞춰 보면: ACP = OCP = `0x3b5` (949), `UseCaseMapping` = `0x6a5` → **실행되는 건 2번** (`+0x46` 한 번 + `+0x3b` 한 번). VAD 에 `C_949.NLS`·`l_intl.nls` 가 **한 개씩** 나오는 것 (P1a, 아래 질문 1) 과 맞는다. P1b 이후 같은 두 이름이 **한 개씩 더** 보이는 건 이 경로가 아니다 (`[공백]`, 아래 동적 항목).
- 표를 얻은 뒤 `RtlpInitCodePageTables +0xa8~+0x11e` 가 전역에 쓰는 것 (참고): `NlsAnsiCodePage` (`+0xd6`), `NlsMbCodePageTag` (`+0xe8`), `GlobalRtlNlsState+0x90 LeadByteInfo` (`+0xef`), `+0x98 OemLeadByteInfo` (`+0x10b`), `NlsMbOemCodePageTag` (`+0x11e`). ANSI·OEM 중 하나라도 `CodePage == 0xfde9` 면 `+0x44b` 로 가서 `NlsAnsiCodePage = 0xfde9`.
- 전역 `GlobalRtlNlsState` 의 PDB 형은 `_RTL_NLS_STATE` (크기 `0xb8`): `+0x0 DefaultAcpTableInfo`, `+0x40 DefaultOemTableInfo` (둘 다 `_CPTABLEINFO`, 크기 `0x40`), `+0x80 ActiveCodePageData`, `+0x88 OemCodePageData`, `+0x90 LeadByteInfo`, `+0x98 OemLeadByteInfo`, `+0xa0 CaseMappingData`, `+0xa8 UnicodeUpcaseTable844`, `+0xb0 UnicodeLowercaseTable844`. (`pdb_struct.py ntdll.dll _RTL_NLS_STATE`)

---

## 질문별 답 [정적]

### 질문 1. `nt!NtGetNlsSectionPtr` (rva `0xa27da0`, 1298B)

원형(레지스터로 읽은 것): `NtGetNlsSectionPtr(종류 ECX, 데이터 EDX, 섹션객체출력 R8, 뷰주소출력 R9, 뷰크기출력 [스택])`. syscall 번호 `0x102`, 인자 5개 (스택 1개) — `_workspace/sast/51_syscall-args_table.tsv`. 이름은 PDB 가 아니라 인자 쓰임으로 붙인 것이다 `[추측-이름]`.

**① 섹션 이름·파일 이름 꼴** (`RtlpInitNlsSectionName` rva `0xa282c0` 195B, `RtlpInitNlsFileName` rva `0xa2838c` 461B. 문자열은 `ntoskrnl.exe` 파일 바이트에서 읽음)

| 종류 | 섹션 개체 이름 (`\NLS\` 밑) | 파일 이름 | 근거 |
|---|---|---|---|
| `0xb` 코드 페이지 | `\NLS\NlsSectionCP%d` (`%d` = 둘째 인자, 예 `\NLS\NlsSectionCP949`) | `\SystemRoot\System32\c_%.3d.nls` (예 `c_949.nls`) | `RtlpInitNlsSectionName +0x88~+0x9a` (`RtlStringCchPrintfW`, 40자) · `RtlpInitNlsFileName +0x192~+0x1a9` |
| `0xe` 대문자 표 | `\NLS\NlsSectionCASE` (고정, 둘째 인자 안 씀) | `\SystemRoot\System32\l_intl.nls` (고정) | `RtlpInitNlsSectionName +0x24~+0x55` (고정 글자 복사) · `RtlpInitNlsFileName +0x1b1~+0x1c5` (`RtlStringCchCopyW`) |
| `0xc` (이름만) | `\NLS\NlsSectionNORM%.8x` (둘째 인자 16진 8자리) | 레지스트리 `\Registry\Machine\System\CurrentControlSet\Control\Nls\Normalization` 의 값(이름 = 둘째 인자를 16진 문자열로, 데이터 = 문자열형)을 `\SystemRoot\System32\%s` 로 | `RtlpInitNlsSectionName +0xbb` · `RtlpInitNlsFileName +0x7f~+0x158` (`ZwOpenKey`·`RtlIntegerToUnicodeString`(16진)·`ZwQueryValueKey`, 값 형이 문자열이 아니면 `+0x1c7` 에서 `0xc0000001`) |
| 그 밖 (`0xd` 포함) | — | — | `RtlpInitNlsSectionName +0xb4` → **`0xc00000ef`** (`STATUS_INVALID_PARAMETER_1`). `NtGetNlsSectionPtr +0x144~+0x155` 에도 같은 뜻의 거름(`(종류-0xb) & ~3` 이거나 `0xd` 면 `+0x505` 에서 `0xc0000001`)이 있지만 앞의 이름 단계에서 먼저 걸린다. |

**② 섹션이 없을 때 이 함수 안에서 처음 만드나? — 둘 다다.** 함수 안에 "열기 실패 → 파일로 만들기" 갈래가 **있고**, 부팅 때 커널이 이 갈래로 한 번 만들어 둔다.

- 이 함수 안의 순서 (`NtGetNlsSectionPtr.asm`):
  1. `+0xfd`/`+0x102` `RtlpInitNlsSectionName` (이름 만들기). 음수면 그대로 반환 (`+0x104 JS +0x334`).
  2. `+0x16d`/`+0x172` **`ZwOpenSection`** (접근 `4` = 읽기 매핑, 이름 붙은 개체 열기). 속성 `0x2d0` (`+0x11d`) = 영구 · 대소문자 무시 · 열려 있으면 열기 · 커널 핸들.
  3. 성공 → `+0x1af`/`+0x1b4` `ObReferenceObjectByHandle` (형 `MmSectionObjectType`) → `+0x1bb`/`+0x1c0` `ZwClose`. **여기서 새로 만들지 않는다.**
  4. 실패 (`+0x176 JS +0x370`) → **만들기 갈래**: `+0x3ac`/`+0x3b1` `RtlpInitNlsFileName` → `+0x41d`/`+0x422` `ZwOpenFile` (접근 `0x100000`, 공유 읽기, 속성 `0x240`) → `+0x458`/`+0x45d` **`ZwCreateSection`** (접근 `4`, 크기 NULL, 보호 `2`(PAGE_READONLY), 할당 속성 `0x8000000`(SEC_COMMIT), 파일 핸들, **②번 단계와 같은 `OBJECT_ATTRIBUTES` = 같은 이름·영구**) → `+0x464`/`+0x469` `ZwClose`(파일). 그리고 3 번으로 합류 (`+0x469 JMP +0x17f`).
- 부팅 때 만드는 쪽 (caller 사슬, 이름만):
  - `Phase1InitializationIoReady +0x27` → **`ExInitializeNls`** (rva `0xae0a0c`, 163B) — 같은 함수를 `PspInitializeServerSiloDeferred +0x152` (서버 silo 만들 때)도 부른다 (`_refs/ExInitializeNls.tsv`).
  - `ExInitializeNls` 안: `+0x28` `ZwCreateDirectoryObject` (**`\NLS` 디렉터리 개체**, 속성 `0x250` = 영구·대소문자 무시·커널 핸들, 접근 `0xf000f`) → `+0x4b` `ExAllocatePool2` (플래그 `0x100`, 16바이트, 태그 `NlsX`) → `+0x6f` `PsGetCurrentServerSiloGlobals` → `+0x79` 커널 사본 `RtlpQueryNlsSystemCodePages` → **`+0x91` 커널 사본 `RtlpInitCodePageTables`** → **`+0x96` 커널 사본 `RtlpInitUppercaseTables`**.
  - 그 커널 사본들이 `NtGetNlsSectionPtr` 를 부른다: `RtlpInitCodePageTables +0x69`·`+0x91` (종류 `0xb`, 둘째 = ANSI·OEM), `RtlpInitUppercaseTables +0x2b` (종류 `0xe`). 불리는 곳은 이 둘뿐이다 (`NtGetNlsSectionPtr.c` 머리 `callers:`). 이때 호출자는 커널 모드(`PreviousMode == 0`)다.
  - 막 만든 `\NLS` 는 비어 있으니 `ZwOpenSection` 이 실패 → **위 4 번(만들기) 갈래가 첫 섹션들을 만든다.** `[정적]` 코드가 그렇게 쓰여 있다는 뜻. 부팅 때 실제로 탔는지는 동적(부팅 중이라 어렵다).
  - 커널의 NLS 상태는 서버 silo 전역 `+0x408` 의 `_RTL_NLS_STATE` 다 (`RtlNlsInitState` rva `0x78ab04` 가 `InitBootProcessor +0x10c` 에서 UTF-8 기본값 `0xfde9` 로 먼저 채운다. `ExInitializeNls` 는 이 뒤에 도는 단계).
- 결론: **test.exe 가 유저 모드에서 부를 때는 보통 "열기 → 성공 → 매핑" 이다.** 만들기 갈래는 아직 만들어 둔 적 없는 코드 페이지가 요청될 때만 탄다 (예: 매니페스트 `activeCodePage` 가 부팅 코드 페이지와 다를 때). `[추측]` 이 마지막 한 줄은 코드 읽은 추론이고, test.exe 에서 안 탄다는 건 동적으로 확인할 수 있다.

**③ 호출한 프로세스 주소 공간에 올리는 줄 — 유저 모드 호출(`PreviousMode != 0`)**

- 길: `+0x1c8 TEST R14,R14`(뷰 주소 출력 칸이 NULL 이면 매핑 건너뜀, `+0x1cb JZ +0x109`) → `+0x1d7 TEST R12B,R12B`(`R12B` = `KTHREAD.PreviousMode`, `+0x78~+0x81`) → 유저면 `+0x1da` 이후 **`MiMapViewOfSection`** (`+0x2e1` call, ret `+0x2eb`). 공개 API `MmMapViewOfSection` 이 아니라 안쪽 `MiMapViewOfSection` 을 곧바로 부른다.
- 인자 (`MiMapViewOfSection` 에 넘기는 `0x80` 바이트 지역 구조체 `[RSP+0x110]` 와 스택 칸). 칸 뜻은 `MmMapViewOfSection` 이 같은 구조체를 만드는 `MmMapViewOfSection +0x3e~+0x15a` 와 나란히 읽어서 정했다 `[추측-칸 이름]` (값은 `[정적]`):
  - 섹션 = 방금 얻은 섹션 개체 (`[RSP+0x48]`), 프로세스 = **현재 프로세스** (`GS:[0x188]+0xb8`, `+0x222~+0x22a`).
  - **크기**: 뷰 크기 칸(구조체 `+0x18` = `[RSP+0x128]`)을 **0** 으로 넣는다 (`+0x27d MOV [RSP+0x128],R13`, `R13 = 0`) → 섹션 전체. 돌아올 때 그 칸에 실제 크기가 채워진다.
  - **보호**: 구조체 `+0x2c` = **`2` (PAGE_READONLY)** (`+0x25b MOV [RSP+0x13c],2`).
  - **할당 종류**: 구조체 `+0x28` = **`0x400000`** (`+0x250`). `SEC_NO_CHANGE` 로 보이는 값 — 이름은 `[추측]`. (`MmMapApiSetView +0x45` 의 `0x400000, 2` 와 짝이 같다.)
  - 상속 `1` (`+0x2bf [RSP+0x28]=1`), 마지막 칸도 `1` (`+0x2b7 [RSP+0x30]=1`, 뜻은 안 정함).
  - **주소 고르는 법**: 시작 주소 입력 = 0 (`[RSP+0x50]`=0, `+0x65`), 정렬 `0x10000` (64KB, 구조체 `+0x10` = `[RSP+0x120]`, `+0x2ab`), 위쪽 한계 = `min(EPROCESS.HighestUserAddress - 1, 0x7ffffffeffff)` (구조체 `+0x8` = `[RSP+0x118]`, `+0x288~+0x2a3`; `EPROCESS +0x368` 은 PDB `HighestUserAddress`). 방향 플래그는 없다 — 아래쪽부터 비는 곳을 찾는 걸로 보이지만 `MiMapViewOfSection` 본문은 안 봤다 `[추측]`. `[관찰]` run44 P1a: `C_949.NLS` 가 `25996d20` (ApiSet 뷰 `25996cd0` 바로 뒤쪽).
  - 커널 호출자(`PreviousMode == 0`, 커널 사본 `RtlpInit...`)는 이 갈래 대신 **시스템 공간**에 올린다: `+0x46e~+0x4e3` `MiSectionControlArea` → `MiCheckPurgeAndUpMapCount` → `MiInsertInSystemSpace`.
- 성공하면 `+0x2eb~+0x2ef` 에서 실패 거름, 이어서 `+0x2f9~+0x303` (셋째 출력 칸이 NULL 이면 `ObfDereferenceObject` 로 섹션 참조를 놓는다 — **유저 호출은 항상 이 쪽**).

**④ 유저에게 돌려주는 것 · 실패 상태값**

- 출력 (`+0x30c~+0x32e`):
  - `*R9` (뷰 주소 출력, 넷째) = 올린 **뷰 주소** (`[RSP+0x50]`).
  - `*[스택 다섯째]` (뷰 크기 출력) = 뷰 크기 (`R12 = [RSP+0x128]`). 두 칸 다 NULL 이면 안 씀. **ntdll 은 다섯째를 늘 NULL 로 부른다** (위 표) — 그래서 크기는 못 받는다.
  - `*R8` (섹션 개체 출력, 셋째) = 섹션 개체 포인터. **유저는 못 쓴다**: 유저 호출인데 NULL 이 아니면 `+0xda~+0xdd` → `+0x4fb` 에서 **`0xc00000f1`** (`STATUS_INVALID_PARAMETER_3`). 커널 호출자(`R8` 로 받으면 섹션을 안 놓음 `+0x2fc JNZ`)용 칸이다.
- 유저 포인터 검사: `+0x88~+0xd3` 에서 세 출력 칸을 "읽고 같은 값 다시 쓰기" 로 쓰기 가능한지 건드려 본다 (사용자 주소 한계 `0x7fffffff0000` 로 눌러서). 잘못된 주소면 예외 → `NtGetNlsSectionPtr$filt$0` (`rva 0xb0a173`) 이 `PreviousMode` 로 처리.
- 상태값:
  - `0xc000000d` (`STATUS_INVALID_PARAMETER`) — `R9` 와 `R8` 이 둘 다 NULL (`+0x6f~+0x72` → `+0x4e8~+0x4f1`).
  - `0xc00000f1` — 위.
  - `0xc00000ef` — 종류가 `0xb`·`0xc`·`0xe` 가 아님.
  - 그 밖: `ZwOpenFile`·`ZwCreateSection`·`ObReferenceObjectByHandle`·`MiMapViewOfSection` 의 상태가 그대로 (예: 파일이 없으면 `ZwOpenFile` 의 "개체 이름 없음" 류). `MiMapViewOfSection` 이 실패하면 전역 카운터 `0x140e302e8` (`+0x363`) 또는 `0x140e302ec` (`+0x50f`) 를 하나 올린다 (뜻은 안 정함).

**⑤ 다른 갈래 — 이름·번호만**

- 종류 `0xc` → 이름 `\NLS\NlsSectionNORM%.8x` (정규화 표; 파일 이름은 레지스트리에서). 이 함수 본문 흐름은 `0xb`·`0xe` 와 같고 이름 단계만 다르다. `0xd` 와 그 밖은 `0xc00000ef`. 더 안 봄.

### 질문 2. ntdll `RtlInitCodePageTable` (rva `0xec460`, 224B)

- **하위 호출·syscall 없음** (`.asm` 에 `CALL` 이 하나도 없다). 단말 함수. 전역 `Utf8TableInfo` (rva `0x1cd050`, 64바이트) 만 읽는다.
- 입력: 첫째 `RCX` = 표 주소 (섹션 뷰 주소, 없으면 NULL), 둘째 `RDX` = 출력 `_CPTABLEINFO` (크기 `0x40`).
- **갈래 1** (`+0x6 TEST RCX,RCX` / `+0x9 JZ +0x16`, 또는 `+0x10 CMP word[RCX+2],0xfde9` 이 같으면 `+0x14 JNZ` 안 탐): `Utf8TableInfo` 64바이트를 출력에 그대로 복사하고 끝 (`+0x16~+0x41`). 곧 **표가 NULL 이거나 표의 코드 페이지가 `0xfde9` 면 UTF-8 기본 표.**
- **갈래 2** (표를 읽는 길, `+0x43~+0xe0`). 표(워드 배열) → `CPTABLEINFO` 칸:

| 입력 표에서 읽는 곳 | 출력 칸 (PDB `_CPTABLEINFO`) | 줄 |
|---|---|---|
| 워드 `[1]` (`+0x2`) | `+0x0 CodePage` | `+0xca~+0xcf` (`XCHG [RDX],AX`) |
| 워드 `[2]` (`+0x4`) | `+0x2 MaximumCharacterSize` | `+0x43~+0x54` |
| 워드 `[3]` (`+0x6`) | `+0x4 DefaultChar` | `+0x58~+0x5c` |
| 워드 `[4]` (`+0x8`) | `+0x6 UniDefaultChar` | `+0x60~+0x64` |
| 워드 `[5]` (`+0xa`) | `+0x8 TransDefaultChar` | `+0x68~+0x6c` |
| 워드 `[6]` (`+0xc`) | `+0xa TransUniDefaultChar` | `+0x70~+0x74` |
| 바이트 `+0xe` 부터 12바이트 | `+0xe LeadByte[12]` | `+0x78~+0x85` (8바이트 + 4바이트) |
| 워드 `[0]` = 머리 길이 `h` (워드 수) | `+0x20 MultiByteTable` = 표 + `(h+1)*2` | `+0x4b`, `+0x88~+0x93` |
| 워드 `[h]` (MB 표 뒤 크기) | `+0x28 WideCharTable` = 표 + `(h + 워드[h] + 1)*2` | `+0x4f`, `+0xbb~+0xc6` |
| MB 표(256워드 = `0x200`바이트) 바로 뒤 워드 ≠ 0 이면 `0x200` 더 건너뛰고, 그 뒤 첫 워드 > 0 이면 | `+0xc DBCSCodePage` = 1, `+0x38 DBCSOffsets` = (그 자리 + 4) | `+0x8c~+0xe0` · 아니면 둘 다 0 (`+0xaf~+0xb7`) |

  - `+0x30 DBCSRanges` 는 이 함수가 안 쓴다 (코드에 없다).
  - 곧 NLS 파일 (`c_xxx.nls`) 안 모양: 첫 워드 = 머리 길이, 다음 워드 = 코드 페이지 번호, 그 뒤 최대 문자 크기·기본 문자 넷·리드바이트 12바이트…, 이어서 MB 표 256워드, (있으면 또 256워드), DBCS 표, 와이드문자 표. 파일 내용 자체는 이번에 안 봄.

### 질문 3. ntdll `LdrpFusionManifestCodePages` (rva `0xec548`, 257B)

- 원형: `BOOLEAN LdrpFusionManifestCodePages(ULONG *ACP, ULONG *OCP)` (`RCX`·`RDX` 가 출력 칸, 돌려주는 값은 `AL`).
- **test.exe(매니페스트 없음) 에서 "없음"(`AL = 0`, 두 번호 0) 을 돌려주나? — 예 `[정적]`.**
  - 시작에서 두 칸을 0 으로 (`+0x1b AND [RCX],0`, `+0x2e AND [RDX],0`).
  - `+0x96` `RtlQueryActivationContextApplicationSettings(0, 0, L"http://schemas.microsoft.com/SMI/2019/WindowsSettings", L"activeCodePage", 버퍼(15자), 15, NULL)` (ret `+0x53`). 두 문자열은 `ntdll.dll` 바이트에서 읽음 (`0x180180a60`, `0x1801809d0`).
  - 그 결과가 음수면 `+0x55 JNS +0x79` 를 안 타고 `+0x57 CMP [RBX],0` → `+0x5a SETNZ AL` → **`AL = (ACP != 0)` = 0**.
  - 그 함수가 음수를 주는 이유: 활성화 문맥 인자 `0` → `PEB +0x2f8` (`ActivationContextData`) 를 읽고 (`+0x55~+0x5e`), **NULL 이면 정적 빈 데이터 `RtlpTheEmptyActivationContextData`** (`+0x65~+0x6c`) 를 쓴다. 이 빈 데이터는 머리 `'Actx'`, 머리 크기 `0x20`, 전체 크기 `0x20`, 기본 목차 오프셋 `0` 이다 (파일 바이트 `41637478 20000000 01000000 20000000 00…`). `RtlpLocateActivationContextSection` (rva `0x42120`) 가 이걸 받으면 `+0x58 MOV EAX,[RCX+0x10]` / `+0x5d JZ +0x239` → `MOV EBX,0xc0150001` (`STATUS_SXS_SECTION_NOT_FOUND`) 로 끝 → `RtlQuery… +0x8c JS +0x1b4` 에서 `0xc0150001` 을 `0xc0150008` 로 바꿔 음수로 돌려준다 (`+0x1b9~+0x1bf`).
  - `PEB +0x2f8` 이 NULL 이 아니어도 "응용프로그램 설정 섹션(번호 10)" 이 없으면 똑같이 음수다 (`RtlQuery… +0x83` `RtlpLocateActivationContextSection` ret `+0x88` → `+0x8c JS` 로 실패). 곧 **매니페스트가 없거나 `activeCodePage` 설정이 없는 exe 는 모두 "없음".**
- 돌려주는 값 꼴: 코드 페이지 번호 둘 (`ULONG` 둘, 호출자가 낮은 16비트만 PEB 에 쓴다). 상태값은 안 돌려준다 (`AL` 만).
- 설정이 **있을 때**(참고, 이번 test.exe 와 무관): 값 글자가 `"UTF-8"` (`0x180180b68`) 이면 둘 다 `0xfde9` (`+0xc5`), `"Legacy"` (`0x18017e730`) 이면 `RtlpGetProcessCodepagesForLocale(NULL,…)` 로 시스템 로캘 코드 페이지 (결과가 `0xfde9` 나 0 이면 `0x4e4`(1252)·`0x1b5`(437), `+0xf1~+0xf7`), 그 밖이면 `+0xa8` `MayBeLocaleName` 이 참일 때만 `+0xbc` `RtlpGetProcessCodepagesForLocale(이름,…)` (음수면 `+0xc5` 에서 `0xfde9` 둘), 로캘 이름 모양이 아니면 `+0xaf JZ +0x57` 로 두 번호 0 그대로 = "없음".
- `LdrpInitializeNlsInfo` 가 이 값을 쓰는 법 (`LdrpInitializeNlsInfo.asm`):
  - 입구: `PEB.ProcessParameters` (`[RCX+0x20]`) 의 `Flags` 에 **`0x8000000`** 이 켜져 있으면 곧장 `+0x72` 로 가서 `PEB +0x34c` 에 `0xfde9fde9` (ACP·OCP 둘) 를 쓰고 `RtlpInitCodePageTables(0xfde9, 0xfde9)` 로 (`+0x72~+0x84`). 이때는 `FusionManifest`·레지스트리를 아예 안 부른다.
  - 아니면 `+0x2a` `LdrpFusionManifestCodePages` → **`AL != 0` 이면 `+0x46` 으로 건너뜀** (`+0x31 JNZ`), **`AL == 0`(test.exe)** 이면 `+0x3d` `RtlpQueryNlsSystemCodePages` 로 레지스트리에서 얻는다. 그 결과가 음수면 `+0x44 JS +0x86` → 둘 다 `0xfde9`.
  - 마지막: 두 번호의 낮은 16비트를 `PEB +0x34c` (`+0x4e`)·`PEB +0x34e` (`+0x55`) 에 쓴다. 이 값이 커널이 `MmCreatePeb` 에서 이미 써 둔 값과 같으면 같은 값을 다시 쓰는 것이다 — 커널 쪽 쓰는 줄은 질문 4 뒤 "짐 지도" 항목.

### 질문 4. 부르는 쪽 자리 확인 — 위 "먼저 바로잡기" 표로 대신함.

### 질문 5. `nt!MmMapApiSetView` (rva `0x930e78`, 273B)

원형: `NTSTATUS MmMapApiSetView(PEPROCESS 새 프로세스)` (인자 `RCX` → `RBX`).

- **어느 섹션을 올리나**: 전역 **`[0x140e2d818]`** (= `ntoskrnl +0xe2d818`, 섹션 개체 포인터). PDB 공개 심볼에 이름이 없다 (`pdb_aliases.py --where ntoskrnl.exe MmApiSetSection` 도 없음). 같은 곳 `+0xe2d820` = 커널 쪽 뷰 주소, `+0xe2d828` = 크기 (`MmQueryApiSetSchema` rva `0x4c2d1c` 21B 가 두 칸 주소를 돌려준다). 쓰는 곳은 `MiInitializeApiSets` 하나뿐 (`_refs/0xe2d818.tsv`).
- **silo 갈래** (`MmMapApiSetView.asm`):
  - `+0x1e`/`+0x23` `PsGetProcessServerSilo` (= `[EPROCESS+0x5f0]` `ServerSilo`, 한 줄 함수) → `+0x29`/`+0x2e` `PsIsHostSilo` (= 인자가 NULL 인가, 한 줄 함수) → `+0x30 JZ +0x100`.
  - **보통 프로세스(서버 silo 없음 → `ServerSilo == NULL` → "호스트")** 는 `+0x36 MOV RCX,[0x140e2d818]` 로 위 전역 섹션을 쓴다.
  - 서버 silo 안 프로세스는 `+0x100`/`+0x108` `PsGetServerSiloGlobals` → `+0x108 MOV RCX,[RAX+0x3e8]` 로 **그 silo 자기 섹션**을 쓴다 (그 섹션은 `PspSiloLoadApiSets rva 0x77fb28` → `PspApiSetCopyToSystemSpace` 가 만드는 걸로 보임, 본문은 안 봄 `[추측]`).
- **`+0x80` `MmMapViewOfSection`** (`+0x7b` call, ret `+0x80`) 인자 — 공개 API 순서:

| 인자 | 값 | 줄 |
|---|---|---|
| 섹션 | 위 전역 (또는 silo 섹션) | `+0x36` |
| 프로세스 | 새 프로세스 `RBX` | `+0x78 MOV RDX,RBX` |
| 시작 주소 (입출력) | 지역 `[RSP+0x68]` = 0 → **커널이 고름** | `+0x73 LEA R8,[RSP+0x68]` (초기화 `+0xf`·`+0x14`·`+0x19`) |
| ZeroBits / CommitSize | 0 / 0 | `+0x70`, `+0x6a` |
| 섹션 안 시작 위치 | 0 (`[RSP+0x78]`) | `+0x60` |
| **크기** (입출력) | `[RSP+0x70]` = **0 → 섹션 전체** | `+0x56` |
| 상속 | `1` (`ViewShare`) | `+0x52` |
| 할당 종류 | `0x400000` | `+0x45` |
| **보호** | **`2` (PAGE_READONLY)** | `+0x3d` |

  - 곧 읽기 전용 · 섹션 전체 · 주소는 커널이 정함. `[관찰]` 뷰는 `25996cd0`~`25996cf9` = **0x2a쪽 (0x2a000)**, 읽기 전용, "Pagefile section, shared commit 0x2a" (`ms/logs/04_baggage-read_K1.txt:1373`). 위 정적 값과 맞는다.
- **PEB `+0x68` (`ApiSetMap`) 을 누가 쓰나 — 이 함수 안이다 (부르는 쪽이 아니다).**
  - `+0x86 MOV RCX,[RBX+0x2e0]` (`EPROCESS.Peb`) → `+0x8d MOV RAX,[RSP+0x68]` (방금 올린 뷰 주소) → **`+0x92 MOV [RCX+0x68],RAX`** (64비트 PEB `ApiSetMap`). `+0x84 JS` 로 실패면 건너뛴다.
  - **`Peb` 가 NULL 인지 검사하지 않는다** → 이 함수는 PEB 가 이미 만들어진 뒤에만 불려야 한다 (`MmCreatePeb` 가 먼저, 질문 5 아래 caller 표).
  - WoW64 프로세스면 그 뒤 32비트 PEB 에도 쓴다: `+0x98 MOV RAX,[RBX+0x310]` (`WoW64Process`) → `+0xa4 MOV RCX,[RAX]` (`EWOW64PROCESS.Peb`) → `Machine` (`[RBX+0x6ec]`) 이 `0x14c`(i386) 또는 `0x1c4`(ARMNT) 면 **`+0xd8 MOV [RCX+0x38],EAX`** (32비트 PEB 의 `ApiSetMap`), 그 밖이면 `+0xb6 MOV [RCX+0x68],RAX`. **test.exe 는 64비트라 `WoW64Process == NULL` → 이 줄들은 안 탄다.**
  - 반환값 = `MmMapViewOfSection` 의 상태 (`+0xf2 MOV EAX,EDX`).
- **둘째 caller `PspAllocateProcess +0x25ba` (ret `+0x25bf`) 는 어떤 길인가 — test.exe 는 안 탄다.**
  - `PspAllocateProcess` 안에서는 두 갈래:
    - **길 1**: `+0x236c TEST BL,BL` (플래그 비트 7 — `MmInitializeProcessAddressSpace` 뒤 `+0x20d2 BTS EBX,7` 로 켬) · `+0x2374 TEST R15,R15` (R15 = 11번째 인자 = 스택 일곱째, 만든 정보 구조체로 읽힘 `[추측]`) · `+0x237d TEST BL,2` 가 모두 "통과" 면 `+0x2397` **`PspSetupUserProcessAddressSpace`** (그 안 `+0x250` 이 `MmMapApiSetView`). `[관찰]` run32 `+0x255` 가 잡혔다 (`runs/02_sprint/run32-params-initblock.md` 표 6번 줄).
    - **길 2**: 위 셋 중 하나라도 걸리면 `+0x2574` 로 가서 (`+0x2574 BT EBX,8` 켜짐 · `+0x257e TEST BL,0x10` 꺼짐 · 새 프로세스에 붙기 `+0x2596 KiStackAttachProcess` → `+0x259e KeCopyXfdMaskToPeb` → `+0x25ae PspUpdatePebForAffinityChange` → `+0x25b5 TEST BL,BL`(비트 7 켜짐)) **`+0x25ba MmMapApiSetView`** → `+0x25c8 PspMapSiloSharedDataView` → `+0x25d7 PspPrepareSystemDllInitBlock(0,0)` → (WoW64 면 `+0x25ee PsWow64GetProcessNtdllType` → `+0x25f7 PspPrepareSystemDllInitBlock`) → `+0x2608 KiUnstackDetachProcess`. 매개변수 복사(`PspCopyAndFixupParameters`)·표준 핸들 목록은 **안 한다.**
    - 길 2 조건을 합치면 `비트7 && 비트8 && !비트(0x10) && (R15 == 0 || 비트1)`. 비트 8·4·1 의 뜻은 이름을 못 정했다 `[확인 필요]`.
  - **`PspAllocateProcess` 의 caller 는 셋**: `NtCreateUserProcess` (지도), `PspCreateProcess` (= `NtCreateProcess`·`NtCreateProcessEx` 길), `PsCreateMinimalProcess`. 11번째 인자(R15)를 보면 — `NtCreateUserProcess +0xbe6` 는 `LEA RAX,[RBP+0x1a0]` (구조체 주소, NULL 아님), **`PspCreateProcess +0x460` 은 `RDI` = 0** (`+0x62 XOR EDI,EDI` 뒤 안 바뀜). 그래서 **길 2 는 `PspCreateProcess` 로 만든 프로세스(`NtCreateProcessEx` 류, 섹션 있고 복제 아님)가 타는 길**로 읽힌다 `[추측]` (`PsCreateMinimalProcess` 는 안 봄). `NtCreateUserProcess` 로 만드는 test.exe 는 길 1.

### 질문 6. ApiSet 섹션은 부팅 때 누가 만드나 — `MiInitializeApiSets` (rva `0xc6920c`, 221B)

- **예, 이 함수다.** 원형 `NTSTATUS MiInitializeApiSets(PLOADER_PARAMETER_BLOCK 로더블록)`.
- **caller 사슬 (이름만)** `[정적]`:
  - `KiInitializeKernel +0x7fe` → `InitBootProcessor` → `InitBootProcessor +0x595` `MmInitSystem(0, 로더블록)` (`+0x593 XOR ECX,ECX`) → `MmInitSystem +0x144` `MiInitSystem(0, …)` (`MmInitSystem +0xa9 TEST R9D,R9D` 위상 0: `+0xae MiInitNucleus` 뒤 `+0x144`) → **`MiInitSystem +0x638` (ret `+0x63d`) `MiInitializeApiSets(RSI = 로더블록)`** (위상 0 본문 안, `MiInitSystem +0x75 TEST ECX,ECX / +0x77 JNZ` 를 안 탄 쪽).
  - 참고: `KiInitializeBootStructures +0x2ad` 는 `MmInitSystem(-1, …)` (`+0x2aa OR ECX,0xffffffff`), 위상 1·2 는 `Phase1InitializationDiscard +0xc65`·`Phase1InitializationIoReady +0x15b` 쪽 (여기선 안 봄).
- **섹션이 어떻게 만들어지나** (`MiInitializeApiSets.asm`):
  - `+0x13 MOV RAX,[RCX+0xf0]` (`LOADER_PARAMETER_BLOCK.Extension`), `+0x33 MOV EDX,[RAX+0xaa8]` → **`ApiSetSchemaSize`** (PDB `_LOADER_PARAMETER_EXTENSION +0xaa8`).
  - `+0x52`/`+0x57` **`MmCreateSection`**: 접근 `0xf001f`, 개체 속성 NULL, 최대 크기 `&ApiSetSchemaSize`, 보호 `4`(PAGE_READWRITE), 할당 속성 `0x8000000`(SEC_COMMIT), 파일 핸들 0·파일 개체 0 → **파일 없는 pagefile 섹션**.
  - `+0x73`/`+0x78` `MmMapViewInSessionSpace` (이 주소는 `MmMapViewInSystemSpace` 와 같은 코드로 합쳐져 있다: `alias`) 로 **커널 쪽 읽기 쓰기 뷰**. 실패면 `+0x86 ObfDereferenceObjectWithTag` (`MmSc` 태그).
  - `+0xac`/`+0xb1` **`RtlCopyMemory(뷰, [Extension+0xaa0] = ApiSetSchema 포인터, ApiSetSchemaSize)`** — 로더가 넘긴 표를 **바이트 그대로 복사.**
  - `+0xb1`/`+0xb8`/`+0xce` 전역 세 칸 `ntoskrnl +0xe2d818` (섹션) · `+0xe2d820` (커널 뷰 주소) · `+0xe2d828` (크기) 에 기록. 커널 뷰는 안 놓는다 (`MmQueryApiSetSchema` 용).
- **`apisetschema.dll` 의 표(`.apiset`)를 그대로 올리나, 합친 새 표인가 — 이 함수는 "그대로 복사" 이고 합치기는 없다. 단 "로더가 넘긴 표" 가 `.apiset` 원본인지는 이 분석으로는 못 정한다.**
  - 이 함수 `callees:` 에 `ApiSetComposeSchema*` 가 없다 (`MmCreateSection`·`RtlCopyMemory`·`MmMapViewInSessionSpace`·`ObfDereferenceObjectWithTag` 뿐).
  - 커널에서 `ApiSetLoadSchemaWithExtensions` (→ `ApiSetLoadSchemaEx`·`ApiSetpLoadSchemaExtension` → `ApiSetComposeSchema`) 를 부르는 곳은 **`PspSiloLoadApiSets +0x4e` 하나** (`_refs/ApiSetLoadSchemaWithExtensions.tsv`) — 곧 **합치기는 서버 silo 쪽 길뿐**, 호스트(보통 프로세스)의 전역 섹션에는 안 쓰인다.
  - 로더블록의 `ApiSetSchema`·`ApiSetSchemaSize`·`ApiSetSchemaExtensions`(`+0xab0`, 확장 목록) 는 부트 로더(`winload`)가 채운다 — **이 정적 범위 밖.** 확장이 섞여 들어갔는지는 아래 동적 항목 "뷰 머리말 대조" 로 본다.
  - 서버 `apisetschema.dll` (`ms/bin/.../System32/apisetschema.dll`) 의 `.apiset` 섹션: RVA `0x3000`, VirtualSize `0x29c60` = **171104**, 머리 = 버전 6, 전체 크기 171104, 플래그 0, 개수 984, 항목 오프셋 152, 해시 오프셋 43568, 해시 인자 31. 머리 `Size` 가 섹션 크기와 **정확히 같다.** 171104 바이트 = 41.77쪽 → **42쪽 (0x2a쪽)** — `[관찰]` 뷰 0x2a쪽과 맞는다. (확장을 붙여서 늘었다면 보통 쪽 수가 달라질 텐데 같다. 증거는 약하다 `[추측]`.)

### 질문 7. `ntdll!LdrpPreprocessDllName` (rva `0x3df80`) `+0x82`·`+0xc3`

- **`+0x82` 는 `PEB.ApiSetMap` (`+0x68`) 을 읽는다 — 예.** `+0x62 MOV RAX,GS:[0x60]` (PEB) → `+0x82 MOV RBX,[RAX+0x68]`. **`+0xc3 CMP byte ptr [RBX],7`** 이 그 첫 바이트를 읽는다 (7 이면 새 모양 → `+0xfe` `ApiSetpResolveHost`, 아니면 `+0xc6 JNZ +0x236` 옛 모양: `+0x6ff ApiSetpSearchForApiSet`·`+0x734 ApiSetpSearchForApiSetHost`).
- **`+0x82` 에서 0 검사가 있나 — 없다.** `+0x82` 와 `+0xc3` 사이에 있는 `TEST` 는 `PEB.SharedData`(`+0x96 TEST RCX,RCX`, `PEB +0x90`), `SharedUserData` 바이트(`+0xa2 CMP [RCX],0` 의 `0x7ffe0384`), `PEB +0x378` 비트 2 (`+0xb6`) 뿐이고 `RBX` 는 안 본다. 그 갈래들은 로그(`+0xa89 RtlGetCurrentServiceSessionId`, `LdrpLogEtwEvent`) 만 거치고 `+0xc3` 로 합류한다.
  - 그래서 **"칸이 0 이면 `+0xc3` 이 주소 0 을 읽는다" 는 PE_emulator 말이 맞다** `[정적]`.
  - 단, 함수 앞머리 `+0x3e TEST [R12],0x800008 / +0x46 JNZ +0x366` 으로 이 길 전체를 건너뛰는 갈래가 있다 (네 번째 인자 `[R9]` 의 플래그 중 `0x800008` 이 켜진 경우; 뜻은 안 정함). 그 갈래에선 `+0x82` 에 안 닿는다.
- **run23 `ApiSetpResolveHost` 보다 앞의 첫 읽기인가?**
  - 이 함수 안에서는 **예**: 읽기(`+0x82`)가 `ApiSetpResolveHost` 호출(`+0xfe`, ret `+0x103`)보다 앞이다.
  - 이미 뽑아 둔 ntdll 함수 362개 안에서 `PEB +0x68` 을 읽는 자리를 훑으면 **4곳** (`GS:[0x60]` → `[reg+0x68]` 꼴): `LdrpPreprocessDllName +0x82`, `LdrpLoadDependentModuleInternal +0x278` (run23 의 `+0x2ff ApiSetpResolveHost` 가 이 읽기 뒤), `LdrpSnapKernelBaseExtensions +0x90` (`+0x1c5 ApiSetpResolveHost`), `LdrpLogRelativePathWithAlteredSearchError +0x3e` (로그 전용; `LdrpGetDllPath +0x1f9` 아래). 이 훑기는 362개 안에서만이고, 더 있을 수 있다 `[한계]`.
  - **코드 순서(`LdrpInitializeProcess` 의 호출 자리 번호)로는**: `+0x199d`/`+0x19a2` `LdrpInitializeKernel32Functions` 가 `+0x1aa4`/`+0x1aa9` `LdrpMapAndSnapDependency` 보다 앞이다. 앞쪽 안에서 `LdrpInitializeKernel32Functions +0xbc`/`+0xc0` `LdrLoadDll(KERNEL32)` → `LdrpLoadDll +0xbd`/`+0xc2` **`LdrpPreprocessDllName`** (KERNEL32 가 아직 안 올라왔을 때만; `[관찰]` run7 에서 이 `LdrLoadDll` 이 잡힘) 이 첫 읽기 후보, 이어서 `+0x130`/`+0x135` `LdrpSnapKernelBaseExtensions`, 그 뒤 `LdrpMapAndSnapDependency +0x349`/`+0x34e` `LdrpLoadDependentModuleInternal`. (`LdrpInitializeImportRedirection +0xd0 LdrpLoadDll` 도 `+0x195e` 에서 먼저 올 수 있는데 조건부.)
  - 실제 실행 순서는 정적으로 못 정한다 → 동적 항목.

---

## 작은 표 둘 (PE_emulator 용)

### NLS (종류 × …)

| | `0xb` 코드 페이지 | `0xe` 대문자 표 |
|---|---|---|
| 섹션 이름 꼴 | `\NLS\NlsSectionCP<번호 10진>` (예 `…CP949`) | `\NLS\NlsSectionCASE` |
| 파일 (섹션을 만들 때 쓰는 원본) | `\SystemRoot\System32\c_<번호 3자리 이상>.nls` | `\SystemRoot\System32\l_intl.nls` |
| 둘째 인자 | 코드 페이지 번호 (ANSI·OEM) | 0 (안 씀) |
| 만드는 때 | **부팅** `Phase1InitializationIoReady` → `ExInitializeNls` → 커널 사본 `RtlpInitCodePageTables` → `NtGetNlsSectionPtr` 만들기 갈래 (커널 모드 호출). 이름 붙은 **영구** 섹션, `\NLS` 디렉터리 (속성 `0x250`). 새 코드 페이지가 요청되면 그때 이 함수 안에서 같은 갈래로 만든다 | 같음 (`RtlpInitUppercaseTables`) |
| 유저 프로세스가 부를 때 | `ZwOpenSection` 으로 열기 → 현재 프로세스에 뷰 올리기 (만들기 갈래 안 탐) | 같음 |
| 뷰 크기 | 섹션 전체 (뷰 크기 입력 0). 크기 = 파일 크기를 쪽 단위로. `[관찰]` P1a: `C_949.NLS` **0x31쪽 (0x31000)** | 섹션 전체. `[관찰]` P1a: `l_intl.nls` **3쪽 (0x3000)** |
| 보호 | PAGE_READONLY (`2`) · 할당 종류 `0x400000` · 정렬 64KB · 상속 1. 섹션 자체는 파일 기반(SEC_COMMIT, 보호 `2`) `[관찰]` "Mapped READONLY" | 같음 |
| 돌려주는 출력 (유저) | `*뷰주소출력` = 뷰 주소 (ntdll 은 `GlobalRtlNlsState +0x80`(ANSI)·`+0x88`(OEM) 칸 자체를 출력 칸으로 넘김). 크기 출력은 ntdll 이 NULL. 섹션 객체 출력은 유저 금지(`0xc00000f1`) | `*뷰주소출력` = 뷰 주소 (ntdll 은 지역 `[RSP+0x48]` → 이어서 `GlobalRtlNlsState +0xa0 CaseMappingData` = 뷰 주소, `+0xa8 UnicodeUpcaseTable844` = +4, `+0xb0 UnicodeLowercaseTable844` = +4 + 워드`[1]`×2, 줄 `RtlpInitUppercaseTables +0x4c~+0x79`) |
| 실패 | `0xc00000ef`(종류)·`0xc000000d`(출력 둘 다 NULL)·`0xc00000f1`(셋째 인자 유저)·열기·만들기·매핑 상태 | 같음 |
| ntdll 이 부르는 자리 | `RtlpInitCodePageTables +0x46`(ANSI) / `+0x6c`(OEM, ANSI≠OEM 일 때만), ret `+0x4b` / `+0x71` | `RtlpInitUppercaseTables +0x3b`, ret `+0x40` (`PEB.UseCaseMapping ≠ 0` 일 때만) |

### ApiSet

| 항목 | 값 |
|---|---|
| 섹션 출처 | 부트 로더가 `LOADER_PARAMETER_BLOCK.Extension` 의 `ApiSetSchema` / `ApiSetSchemaSize` (`+0xaa0` / `+0xaa8`) 에 넘긴 표를 **복사**. 서버의 `apisetschema.dll` `.apiset` 은 V6, 171104바이트. 로더가 이걸 그대로 넘기는지는 정적으로 못 정함 `[공백]` |
| 만드는 함수 | `MiInitializeApiSets` (`MiInitSystem +0x638` ← `MmInitSystem` 위상 0 ← `InitBootProcessor +0x595`), pagefile 섹션 (`MmCreateSection`, 보호 `4`, SEC_COMMIT) + 커널 뷰에 `RtlCopyMemory`. 전역 `ntoskrnl +0xe2d818`(섹션)·`+0xe2d820`(커널 뷰)·`+0xe2d828`(크기) |
| 프로세스에 올리는 함수 | `MmMapApiSetView` ← `PspSetupUserProcessAddressSpace +0x250` (ret `+0x255`) — `NtCreateUserProcess` 길. (`NtCreateProcessEx` 류는 `PspAllocateProcess +0x25ba`) |
| 뷰 크기 | 섹션 전체 (뷰 크기 입력 0) = 표 크기를 쪽 단위로. `[관찰]` **0x2a쪽 = 0x2a000** (표 171104B → 42쪽) |
| 보호 | **PAGE_READONLY (`2`)**, 할당 종류 `0x400000`, 상속 1 (`MmMapApiSetView +0x3d / +0x45 / +0x52`). `[관찰]` "Mapped READONLY, Pagefile section, shared commit 0x2a" |
| 주소 | 커널이 고름 (입력 0). `[관찰]` run44 `25996cd0000` |
| PEB `+0x68` 쓰는 줄 | **`MmMapApiSetView +0x92 MOV [RCX+0x68],RAX`** (`RCX = [EPROCESS+0x2e0]` = PEB, `+0x86`). WoW64(i386/ARMNT) 면 32비트 PEB `+0x38` 에 `+0xd8` 로 낮은 4바이트. 부르는 쪽(`PspSetupUserProcessAddressSpace`)은 안 쓴다 |
| 시점 | `PspSetupUserProcessAddressSpace` 안: `+0x1e6 PspPrepareSystemDllInitBlock` 뒤, `+0x255` 가 돌아오기 전, `+0x263 PspMapSiloSharedDataView` 앞. `[관찰]` PEB `+0x68` 이 K1 에 이미 있음 (run44) |

---

## 짐 지도 반영거리

1. **`PEB.ApiSetMap` (`+0x68`)** — "칸 쓰는 줄 `[공백]`" 을 채울 수 있다.
   - 쓰는 줄 = **`MmMapApiSetView +0x92`** (WoW64 면 `+0xd8` 로 PEB32 `+0x38`). 경로 = `NtCreateUserProcess` → `PspAllocateProcess +0x2397` → `PspSetupUserProcessAddressSpace +0x250` → `MmMapApiSetView`. (baggage.md 1192줄·1541줄·7장 표.)
   - 짐의 주인: 커널(nt), 공간 = 유저(새 프로세스 가상 주소 공간), 놓는 법 = 읽기 전용 pagefile 섹션 뷰를 커널이 고른 주소에 매핑 + 그 주소를 PEB 에 쓰기. 섹션 자체는 부팅 때 `MiInitializeApiSets` 가 만든 전역.
   - 유저 쪽 첫 읽는 자리: `LdrpPreprocessDllName +0x82` (읽기)·`+0xc3` (첫 바이트 검사) 등 질문 7.
2. **NLS 뷰 (C8 "주소 아는 유저 구역") 의 주인** — **ntdll 이 시킨 syscall 로 커널이 올린 것.**
   - 짐: `GlobalRtlNlsState` (`_RTL_NLS_STATE`) 의 `ActiveCodePageData`(+0x80)·`OemCodePageData`(+0x88)·`CaseMappingData`(+0xa0) 가 뷰 주소를 담고, `UnicodeUpcaseTable844`(+0xa8)·`UnicodeLowercaseTable844`(+0xb0)·`DefaultAcpTableInfo`(+0x0)·`DefaultOemTableInfo`(+0x40)·`LeadByteInfo`(+0x90)·`OemLeadByteInfo`(+0x98) 가 뷰 안(또는 `NlsEmptyLeadByteInfoTable`)을 가리킨다. 전역은 ntdll 이미지 안.
   - 단계: ntdll 초기화 `LdrpInitializeProcess +0x1c8` → `LdrpInitializeNlsInfo`. 경로 사슬 = `LdrpInitializeNlsInfo +0x5c` `RtlpInitCodePageTables` `+0x46/+0x6c` → syscall `NtGetNlsSectionPtr` (0x102) → `NtGetNlsSectionPtr +0x2e1` `MiMapViewOfSection`; 대문자는 `+0x6d` 꼬리 JMP `RtlpInitUppercaseTables +0x3b` → 같은 syscall.
   - 놓는 법: 이름 붙은 영구 섹션을 열어 현재 프로세스에 읽기 전용으로 매핑. 섹션 자체의 주인은 커널(`\NLS` 디렉터리, 부팅 때 `ExInitializeNls` 가 파일에서 만듦).
   - 에뮬레이터 쪽으로 옮길 때: 섹션 객체 + `\NLS` 이름 공간은 nt 엔진 쪽, 뷰와 `GlobalRtlNlsState` 는 유저 가상 주소 공간.
3. **PEB `ActiveCodePage`(+0x34c)·`OemCodePage`(+0x34e)·`UseCaseMapping`(+0x350)** — 커널도 쓴다 (baggage 3.1 에 보탤 거리).
   - 쓰는 줄: **`MmCreatePeb +0x546` / `+0x555` / `+0x564`** (`MOV word [RBX+0x34c/0x34e/0x350],AX`, 값 출처 = 서버 silo 전역 `+0x408` 의 `_RTL_NLS_STATE`: `+0x408` 의 `CodePage`(=ANSI), `+0x448` 의 `CodePage`(=OEM) — `MmCreatePeb +0x245`·`+0x254`; `UseCaseMapping` 은 `[silo전역+0x4b0]`(대문자 표 포인터) 가 NULL 이 아니면 **상수 `0x6a5`**, 아니면 0 — `+0x263`~`+0x275`, `+0x613`).
   - 그래서 `[관찰]` 사진의 "K1 부터 이미 `0x3b5`·`0x6a5`" 와 맞는다: **커널이 `MmCreatePeb` 에서 먼저 쓰고, ntdll `LdrpInitializeNlsInfo` 가 (같은 값이면) 다시 쓴다.** run45 "K1 전에 처음 쓴 쪽은 모른다 `[공백]`" 의 답.
   - 단, 커널이 쓴 `UseCaseMapping` 이 0 이 아니라서 ntdll 이 대문자 섹션도 얻는다. 커널의 silo 전역 NLS 값은 `ExInitializeNls` 가 채운다.

## 동적분석으로 확인해야 할 것 (Q104 VM 단계에 얹을 수 있는 것)

1. **test.exe 때 `nt!NtGetNlsSectionPtr` 입구** (프로세스 조건 필요; 커널 멈춤점은 모든 프로세스에 걸림):
   - 호출 횟수·콜스택: `RtlpInitCodePageTables +0x4b`(종류 `0xb`, ACP) · `RtlpInitUppercaseTables +0x40` (종류 `0xe`) 가 와야 한다 (ACP == OCP 면 `+0x71` 은 안 옴). **P1b 이후 보이는 두 번째 `C_949.NLS` (`25996eb0`)·`l_intl.nls` (`25996dc0`) 뷰가 어느 호출자(콜스택)인지** — 정적으로는 찾지 못함 `[공백]`.
   - `ZwOpenSection` 실패 갈래(`NtGetNlsSectionPtr +0x176` 의 `JS` 를 탄 적이 있나, 곧 `+0x3b1 RtlpInitNlsFileName` 이 test.exe 때 도나) — 정적 읽기로는 "안 돈다".
   - 돌아올 때 `R9` 가 가리킨 칸·`ReturnValue` (`+0x332 MOV EAX,EBX` 에서 `EBX`).
2. **ApiSet 표 머리말 대조**: 부팅 후 `dq nt+0xe2d818 L3` (섹션 객체, 커널 뷰, 크기) → 커널 뷰 첫 `0x20` 바이트를 `apisetschema.dll` `.apiset` 머리 (`06000000 60 9c 02 00 00000000 d8030000 98000000 30aa0000 1f000000`: 버전 6, 크기 171104, 플래그 0, 개수 984, 항목 오프셋 152, 해시 오프셋 43568, 인자 31) 와 비교. 다르면(특히 크기·개수) 로더가 확장을 합쳐 넘긴 것이다. 크기 칸(`+0xe2d828`)이 `171104` 인지도 본다.
3. **`PEB +0x68` 첫 읽는 자리 · 순서**: 후보 멈춤점 `ntdll!LdrpPreprocessDllName` (입구) · `ntdll!LdrpSnapKernelBaseExtensions` · `ntdll!LdrpLoadDependentModuleInternal` 중 test.exe 에서 **무엇이 맨 먼저 도나** (`LdrpInitializeKernel32Functions +0xc0` 이 KERNEL32 를 "못 찾음"으로 가서 `LdrLoadDll` 을 도는지 포함). 더 확실히는 PEB `+0x68` 에 읽기 데이터 멈춤점(`ba r8 <PEB+0x68>`, test.exe 문맥에서) 을 걸어 첫 명령 주소를 본다.
4. **`LdrpInitializeNlsInfo` 호출 때 `PEB +0x2f8` (`ActivationContextData`) 값** (예상 0) 과 `LdrpFusionManifestCodePages` 의 반환 `AL` (`LdrpInitializeNlsInfo +0x2f` 직후 `r al`, 예상 0). baggage.md 1756줄 D3("매니페스트 `heapType` 조회가 `ActivationContextData` null 이라 음수로 끝나는지") 도 같은 호출 경로(`RtlQueryActivationContextApplicationSettings`, 이번에 `PEB+0x2f8`==NULL 일 때 정적 빈 데이터 갈래 + `0xc0150001`→`0xc0150008` 확인)라 이 값으로 같이 풀린다.
5. **읽을 전역**: ntdll `GlobalRtlNlsState` (rva `0x1ccf90`, `dt ntdll!_RTL_NLS_STATE`), `NlsAnsiCodePage` (rva `0x1cd048`); 커널 서버 silo 전역 `+0x408`(`_RTL_NLS_STATE`).
6. **NLS 파일 내용·크기** — 서버에 `.nls` 가 없다. VM 에서 `c_949.nls`·`l_intl.nls`·`locale.nls` 복사 + 크기 (관찰 뷰는 0x31쪽·3쪽).

## 판정

**분석성공.** 질문 1~7 모두 줄 근거와 함께 답했다 (질문 4 는 오프셋을 바로잡는 쪽으로 정리). 정적으로 못 정한 것: (a) 로더(`winload`)가 `ApiSetSchema` 에 확장을 합쳐 넘기는지, (b) 실행 순서상 `PEB.ApiSetMap` 첫 읽기가 어느 함수인지, (c) 두 번째 `C_949.NLS`·`l_intl.nls` 뷰의 주인 — 모두 동적 항목에 올렸다. 동적분석 요청 필요: 위 1~4.

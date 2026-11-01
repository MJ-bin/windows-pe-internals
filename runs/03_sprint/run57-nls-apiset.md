# 2026-11-01 run57 — NLS 글자표와 ApiSet 지도 (Q108, PE_emulator 요청 4-A): NLS 는 **부팅 때 커널이 `\NLS\NlsSectionCP<번호>`·`\NLS\NlsSectionCASE` 이름의 영구 섹션을 파일에서 만들어 두고**, ntdll 이 syscall `NtGetNlsSectionPtr` (종류 `0xb`·`0xe`) 로 그 섹션을 열어 **읽기 전용 뷰**로 받는다. ApiSet 은 **부팅 때 `MiInitializeApiSets` 가 로더가 넘긴 표를 그대로 복사한 pagefile 섹션**이고, `MmMapApiSetView +0x92` 가 새 PEB `+0x68 ApiSetMap` 에 뷰 주소를 쓴다. PEB 코드 페이지 세 칸의 첫 주인은 `MmCreatePeb +0x546/+0x555/+0x564` (정적)

## PE_emulator 쪽에서 쓸 것

결과서 (`_workspace/sast/52_nls-apiset_result.md`) "작은 표 둘" 을 옮기고, 오프셋을 `.asm` 으로 한 번 더 맞췄다 (flow-writer). 오프셋은 모두 `ms/disassemble/<모듈>/<함수>.asm` 의 `+0x` 줄이다 (이 폴더는 커밋 안 함). ntoskrnl 에도 ntdll 과 같은 이름의 커널 사본 (`RtlInitCodePageTable`·`RtlpInitCodePageTables`·`RtlpInitUppercaseTables`·`RtlpQueryNlsSystemCodePages`) 이 있다. 이 문서에서 모듈을 안 붙인 이 네 이름은 **ntdll 사본**이고, 커널 사본은 "커널 사본" 이라고 붙인다.

### 1. NLS — 종류 × 무엇 `[정적]`

| | `0xb` 코드 페이지 | `0xe` 대문자 표 |
|---|---|---|
| 섹션 개체 이름 | `\NLS\NlsSectionCP<번호 10진>` (예 `\NLS\NlsSectionCP949`). `RtlpInitNlsSectionName +0x88 ~ +0x9a` (`RtlStringCchPrintfW`) | `\NLS\NlsSectionCASE` (고정, 둘째 인자 안 씀). `RtlpInitNlsSectionName +0x24 ~ +0x55` |
| 섹션을 만들 때 원본 파일 | `\SystemRoot\System32\c_%.3d.nls` (예 `c_949.nls`). `RtlpInitNlsFileName +0x192 ~ +0x1a9` | `\SystemRoot\System32\l_intl.nls` (고정). `RtlpInitNlsFileName +0x1b1 ~ +0x1c5` |
| 둘째 인자 | 코드 페이지 번호 (ANSI 한 번, OEM 한 번) | 0 |
| 만드는 때 | **부팅**: `Phase1InitializationIoReady +0x27` → `ExInitializeNls` → `+0x91` 커널 사본 `RtlpInitCodePageTables` → `+0x69`·`+0x91` `NtGetNlsSectionPtr` (커널 모드 호출). 이 함수의 "열기 실패 → 파일로 만들기" 갈래 (`+0x176 JS +0x370` → `+0x3ac RtlpInitNlsFileName` → `+0x41d ZwOpenFile` → `+0x458 ZwCreateSection`) 가 이름 붙은 **영구** 섹션을 만든다. 아직 없는 번호가 나중에 요청되면 그때 같은 갈래로 만든다 | 같음 — `ExInitializeNls +0x96` 커널 사본 `RtlpInitUppercaseTables` → `+0x2b NtGetNlsSectionPtr` |
| 섹션 모양 | 파일 기반, `ZwCreateSection` 접근 `4`, 크기 NULL (= 파일 크기), 보호 `2` (PAGE_READONLY), 할당 속성 `0x8000000` (SEC_COMMIT), 개체 속성 `0x2d0` (영구 · 대소문자 무시 · 열려 있으면 열기 · 커널 핸들, `+0x11d`) | 같음 |
| 유저 프로세스가 부를 때 (test.exe) | `+0x16d ZwOpenSection` (접근 `4`) 성공 → `+0x1af ObReferenceObjectByHandle` → `+0x1bb ZwClose` → **현재 프로세스에 뷰** `+0x2e6 MiMapViewOfSection` (ret `+0x2eb`). 만들기 갈래는 안 탄다 `[추측]` (부팅 때 이미 있음) | 같음 |
| 뷰 크기 | 입력 0 (`+0x27d`) = 섹션 전체. `[관찰]` run44 P1a: `C_949.NLS` **0x31쪽** (`25996d20 ~ 25996d50`) | 섹션 전체. `[관찰]` P1a: `l_intl.nls` **3쪽** (`25996d60 ~ 25996d62`) |
| 보호·종류 | PAGE_READONLY `2` (`+0x25b`) · 할당 종류 `0x400000` (`+0x250`, 이름은 `SEC_NO_CHANGE` 로 보임 `[추측]`) · 정렬 64KB (`+0x2ab`) · 상속 1. `[관찰]` "Mapped READONLY" | 같음 |
| 주소 고르기 | 시작 주소 입력 0 → 커널이 고름. 위쪽 한계 `min(EPROCESS.HighestUserAddress − 1, 0x7ffffffeffff)` (`+0x288 ~ +0x2a3`). 방향 플래그 없음 (`MiMapViewOfSection` 본문은 안 봄 `[추측]`). `[관찰]` `C_949.NLS` 는 ApiSet 뷰 `25996cd0` 바로 뒤쪽 | 같음 |
| 돌려주는 것 (유저) | `*R9` (넷째, 뷰 주소 칸) = 뷰 주소 (`+0x316 MOV [R14],RAX`). `*[스택 다섯째]` = 뷰 크기 (`+0x31e`) — **ntdll 은 늘 NULL 로 넘겨 크기는 안 받음**. ntdll 은 넷째로 `&GlobalRtlNlsState+0x80` (ANSI) · `+0x88` (OEM) **칸 자체**를 넘긴다 | `*R9` = 뷰 주소. ntdll 은 넷째로 지역 `[RSP+0x48]` 을 넘기고 받은 뒤 `GlobalRtlNlsState` 세 칸에 옮긴다: `+0xa0 CaseMappingData` = 뷰 (`+0x79`), `+0xa8 UnicodeUpcaseTable844` = 뷰 + 4 (`+0x55`), `+0xb0 UnicodeLowercaseTable844` = 뷰 + 4 + 워드`[1]` × 2 (`+0x6d`) |
| 셋째 인자 (섹션 개체 출력) | 커널 호출자용. **유저가 NULL 아닌 값을 주면 `0xc00000f1`** (`+0xda TEST RDI` → `+0x4fb`) | 같음 |
| 실패 상태값 | `0xc00000ef` 종류가 `0xb`·`0xc`·`0xe` 아님 (`RtlpInitNlsSectionName +0xb4`) · `0xc000000d` 넷째·셋째 둘 다 NULL (`+0x6f ~ +0x72`) · `0xc00000f1` 위 · 그 밖은 `ZwOpenFile`·`ZwCreateSection`·`ObReferenceObjectByHandle`·`MiMapViewOfSection` 상태 그대로 | 같음 |
| ntdll 이 부르는 자리 (call / ret) | `RtlpInitCodePageTables +0x46` / `+0x4b` (ANSI) · `+0x6c` / `+0x71` (OEM, **ANSI ≠ OEM 일 때만**) | `RtlpInitUppercaseTables +0x3b` / `+0x40` (**`PEB.UseCaseMapping ≠ 0` 일 때만**) |

- 원형 (레지스터로 읽음): `NtGetNlsSectionPtr(종류 ECX, 데이터 EDX, 섹션개체출력 R8, 뷰주소출력 R9, 뷰크기출력 [스택])`. syscall 번호 `0x102`, 인자 5개 (스택 1개, `_workspace/sast/51_syscall-args_table.tsv`). 인자 이름은 PDB 가 아니라 쓰임으로 붙인 것 `[추측-이름]`.
- 다른 종류 (이름·번호만): `0xc` → `\NLS\NlsSectionNORM%.8x` (정규화 표, 파일 이름은 레지스트리 `…\Control\Nls\Normalization` 값), `0xd` 와 그 밖은 `0xc00000ef`.
- 커널 호출자 (`PreviousMode == 0`, 부팅 때 커널 사본) 는 유저 뷰 대신 **시스템 공간**에 올린다 (`+0x46e ~ +0x4e3` `MiSectionControlArea` → `MiCheckPurgeAndUpMapCount` → `MiInsertInSystemSpace`).

### 2. ApiSet — 한 장 `[정적]`

| 항목 | 값 |
|---|---|
| 섹션 출처 | 부트 로더가 `LOADER_PARAMETER_BLOCK.Extension` 의 `ApiSetSchema` (`+0xaa0`) / `ApiSetSchemaSize` (`+0xaa8`) 로 넘긴 표를 **바이트 그대로 복사** (합치기 없음). 그 표가 `apisetschema.dll` `.apiset` 원본 그대로인지는 정적으로 못 정함 `[공백]` |
| 만드는 함수 | `MiInitializeApiSets` (rva `0xc6920c`, 221B) — 부팅 위상 0: `InitBootProcessor +0x595 MmInitSystem(0)` → `MmInitSystem +0x144 MiInitSystem` → **`MiInitSystem +0x638`** (ret `+0x63d`). 안: `+0x52 MmCreateSection` (접근 `0xf001f`, 크기 `&ApiSetSchemaSize`, 보호 `4` PAGE_READWRITE, SEC_COMMIT, 파일 없음 = **pagefile 섹션**) → `+0x73` 커널 시스템 공간 뷰 (같은 주소 이름 2개, 후보: `MmMapViewInSessionSpace`·`MmMapViewInSystemSpace`) → `+0xac` 메모리 복사 (같은 주소 이름 4개, 후보: `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) |
| 커널 쪽 전역 | `ntoskrnl +0xe2d818` (섹션 개체, `+0xb1`) · `+0xe2d820` (커널 뷰 주소, `+0xb8`) · `+0xe2d828` (크기, `+0xce`). PDB 공개 이름 없음. 쓰는 곳은 `MiInitializeApiSets` 하나 (`_refs/0xe2d818.tsv`). 두 칸 주소는 `MmQueryApiSetSchema` (rva `0x4c2d1c`) 가 돌려줌 |
| 프로세스에 올리는 함수 | `MmMapApiSetView` (rva `0x930e78`, 273B) ← `PspSetupUserProcessAddressSpace +0x250` (ret `+0x255`) ← `PspAllocateProcess +0x2397` — `NtCreateUserProcess` 길 (test.exe). 다른 caller `PspAllocateProcess +0x25ba` (ret `+0x25bf`) 는 `PspCreateProcess` (`NtCreateProcessEx` 류) 로 만든 프로세스 길로 보임 `[추측]` |
| 어느 섹션 | 보통 프로세스 (`EPROCESS.ServerSilo` = NULL → `PsIsHostSilo` 참): `+0x36` 위 전역 `[0x140e2d818]`. 서버 silo 안 프로세스: `+0x103 PsGetServerSiloGlobals` → `+0x108 [RAX+0x3e8]` (그 silo 자기 섹션, 만드는 쪽 `PspSiloLoadApiSets` 는 이름만 `[추측]`) |
| `MmMapViewOfSection` 인자 (`+0x7b`, ret `+0x80`) | 프로세스 = 새 프로세스 (`+0x78`) · 시작 주소 0 → 커널이 고름 (`+0x73`) · ZeroBits 0 · CommitSize 0 · 섹션 안 위치 0 · **크기 0 = 섹션 전체** (`+0x56`) · 상속 1 (`+0x52`) · 할당 종류 `0x400000` (`+0x45`) · **보호 `2` PAGE_READONLY** (`+0x3d`) |
| 뷰 크기 | 섹션 전체 = 표 크기를 쪽 단위로. `[관찰]` run44 K1: `25996cd0 ~ 25996cf9` = **0x2a쪽 (0x2a000)**, "Mapped READONLY, Pagefile section, shared commit 0x2a" (`ms/logs/04_baggage-read_K1.txt:1373`). 서버의 `apisetschema.dll` `.apiset` = 171104B (버전 6, 개수 984) → 42쪽 = 0x2a쪽과 맞음 |
| 주소 | 커널이 고름. `[관찰]` run44 `25996cd0000` |
| **PEB `+0x68` 쓰는 줄** | **`MmMapApiSetView +0x92 MOV [RCX+0x68],RAX`** (`+0x86 RCX = [EPROCESS+0x2e0]` = PEB, `+0x8d RAX` = 뷰 주소). `+0x84 JS` 로 실패면 안 씀. **PEB NULL 검사 없음** → `MmCreatePeb` 뒤에만 불려야 함. WoW64 (`[EPROCESS+0x310]` ≠ 0) 이고 `Machine` 이 i386·ARMNT 면 32비트 PEB `+0x38` 에 `+0xd8`, 그 밖 WoW64 면 `+0xb6` — **test.exe 는 64비트라 안 탐**. 부르는 쪽 `PspSetupUserProcessAddressSpace` 는 PEB 를 안 씀 (run32 와 같음) |
| 시점 | `PspSetupUserProcessAddressSpace` 안 `+0x1e6 PspPrepareSystemDllInitBlock` 뒤 → `+0x255` → `+0x263 PspMapSiloSharedDataView` 앞. `[관찰]` PEB `+0x68` 이 K1 에 이미 있음 (run44) — 맞음 |
| 반환 | `MmMapViewOfSection` 상태 (`+0xf2`) |

### 3. ntdll 쪽 — 받은 뷰를 어디에 두나 `[정적]`

- `LdrpInitializeProcess +0x1c8` (ret `+0x1cd`) `LdrpInitializeNlsInfo(PEB)`:
  - `PEB.ProcessParameters.Flags` 에 `0x8000000` 이면 `+0x72 ~ +0x84` 로 곧장: `PEB +0x34c` 에 `0xfde9fde9` (`+0x77`) → `RtlpInitCodePageTables(0xfde9, 0xfde9)`. 매니페스트·레지스트리 안 봄.
  - 아니면 `+0x2a LdrpFusionManifestCodePages` → `AL ≠ 0` 이면 `+0x46` 으로, **`AL = 0` (test.exe)** 이면 `+0x3d RtlpQueryNlsSystemCodePages` (레지스트리). 그것도 음수면 `+0x44 JS +0x86` → 둘 다 `0xfde9`.
  - 두 번호의 낮은 16비트를 `PEB +0x34c` (`+0x4e`) · `+0x34e` (`+0x55`) 에 쓰고 → `+0x5c RtlpInitCodePageTables(ANSI, OEM)` → `+0x61` `PEB +0x350 UseCaseMapping` 읽기 → `+0x6d` 꼬리 JMP `RtlpInitUppercaseTables(UseCaseMapping)`.
- `LdrpFusionManifestCodePages(ULONG *ACP, ULONG *OCP)` — **매니페스트 없는 exe 는 "없음" (`AL = 0`, 두 칸 0)** 이다:
  - 두 칸 0 (`+0x1b`·`+0x2e`) → `+0x4e RtlQueryActivationContextApplicationSettings(0, 0, L"http://schemas.microsoft.com/SMI/2019/WindowsSettings", L"activeCodePage", …)` (ret `+0x53`) → 음수면 `+0x57 CMP [RBX],0` / `+0x5a SETNZ AL` = 0.
  - 음수가 되는 까닭: 활성화 문맥 0 → `PEB +0x2f8 ActivationContextData` 가 NULL 이면 정적 빈 데이터 `RtlpTheEmptyActivationContextData` (머리 `'Actx'`, 크기 `0x20`, 목차 오프셋 0) → `RtlpLocateActivationContextSection +0x58 / +0x5d` 에서 `0xc0150001` → 바깥에서 `0xc0150008` 로 바꿔 돌려줌. `+0x2f8` 이 있어도 "응용프로그램 설정 섹션" 이 없으면 똑같이 음수.
- `RtlpInitCodePageTables` (rva `0xec2fc`, 349B) 가 `GlobalRtlNlsState` (`_RTL_NLS_STATE`, rva `0x1ccf90`, `0xb8`B) 를 채우는 순서:
  1. `+0x21 CMP SI,BP` / `+0x24 JZ +0x130` — ANSI 가 `0xfde9` 면 섹션을 안 얻고 `+0x80` 칸 그대로 (UTF-8 표).
  2. `+0x46` syscall ① → `+0x80 ActiveCodePageData` = 뷰 (커널이 씀).
  3. `+0x4f CMP DI,SI` / `+0x52 JZ +0x13c` — **ANSI == OEM 이면** `+0x143` 에서 `+0x80` 값을 `+0x88` 에 복사, 아니면 `+0x6c` syscall ② → `+0x88 OemCodePageData`.
  4. `+0x90 RtlInitCodePageTable(+0x80 값, &GlobalRtlNlsState+0x0)` · `+0xa3 RtlInitCodePageTable(+0x88 값, &GlobalRtlNlsState+0x40)` — `DefaultAcpTableInfo`·`DefaultOemTableInfo` (`_CPTABLEINFO` 각 `0x40`).
  5. `+0xd6 NlsAnsiCodePage` = ANSI 표 `CodePage` · `+0xe8 NlsMbCodePageTag` = ANSI 표 `DBCSCodePage ≠ 0` · `+0xef +0x90 LeadByteInfo` = `DBCSOffsets` 또는 `NlsEmptyLeadByteInfoTable` · `+0x10b +0x98 OemLeadByteInfo` · `+0x11e NlsMbOemCodePageTag`.
  6. 두 표 중 하나라도 `CodePage == 0xfde9` 면 (`+0xb2`·`+0xbf JZ +0x14f`) `+0x14f NlsAnsiCodePage = 0xfde9` · `+0x156 NlsMbCodePageTag`.
- `RtlpInitUppercaseTables` (rva `0xf9804`, 143B): `+0xd TEST CX,CX` / `+0x10 JNZ +0x29` — `UseCaseMapping` 이 0 이면 `+0x12 ~ +0x20` 에서 `+0xa8`·`+0xb0`·`+0xa0` 세 칸 0 으로 끝. 아니면 `+0x3b` syscall ③ → 위 1번 표 `0xe` 줄.
- `RtlInitCodePageTable` (rva `0xec460`, 224B) — **CALL·syscall 없음** (단말). 표가 NULL 이거나 워드`[1]` = `0xfde9` 면 전역 `Utf8TableInfo` (rva `0x1cd050`, 64B) 를 통째 복사 (`+0x16 ~ +0x41`). 아니면 표 → `_CPTABLEINFO`:

| 표에서 읽는 곳 | `_CPTABLEINFO` 칸 (PDB) | 줄 |
|---|---|---|
| 워드`[1]` (`+0x2`) | `+0x0 CodePage` | `+0xca ~ +0xcf` |
| 워드`[2]` (`+0x4`) | `+0x2 MaximumCharacterSize` | `+0x43 ~ +0x54` |
| 워드`[3]` ~ `[6]` | `+0x4 DefaultChar` · `+0x6 UniDefaultChar` · `+0x8 TransDefaultChar` · `+0xa TransUniDefaultChar` | `+0x58 ~ +0x74` |
| 바이트 `+0xe` 부터 12B | `+0xe LeadByte[12]` | `+0x78 ~ +0x85` |
| 워드`[0]` = 머리 길이 `h` | `+0x20 MultiByteTable` = 표 + `(h+1)×2` | `+0x4b`, `+0x88 ~ +0x93` |
| 워드`[h]` | `+0x28 WideCharTable` = 표 + `(h + 워드[h] + 1)×2` | `+0x4f`, `+0xbb ~ +0xc6` |
| MB 표 256워드 뒤 워드 ≠ 0 이면 `0x200` 더 건너뛰고, 그 뒤 첫 워드 > 0 이면 | `+0xc DBCSCodePage` = 1, `+0x38 DBCSOffsets` = 그 자리 + 4 (아니면 둘 다 0, `+0xaf ~ +0xb7`) | `+0x8c ~ +0xe0` |

  - `+0x30 DBCSRanges` 는 안 쓴다. NLS 파일 내용 자체는 안 봤다 (서버에 `.nls` 0개 → Q104 VM 단계에서 복사).

### 4. PEB 코드 페이지 세 칸 — 커널이 먼저 쓴다 `[정적]`

| PEB 칸 | 커널 `MmCreatePeb` 쓰는 줄 | 값 출처 | ntdll 이 다시 |
|---|---|---|---|
| `+0x34c ActiveCodePage` | `+0x546 MOV word [RBX+0x34c],AX` | 서버 silo 전역 `+0x408` (`_RTL_NLS_STATE.DefaultAcpTableInfo.CodePage`) — `+0x245` 읽기 → `[RSP+0xe8]` → `+0x53e` | `LdrpInitializeNlsInfo +0x4e` (같은 값이면 같은 값을 다시) |
| `+0x34e OemCodePage` | `+0x555 MOV word [RBX+0x34e],AX` | 같은 전역 `+0x448` (`DefaultOemTableInfo.CodePage`) — `+0x254` → `[RSP+0xea]` → `+0x54d` | `+0x55` |
| `+0x350 UseCaseMapping` | `+0x564 MOV word [RBX+0x350],AX` | 전역 `+0x4b0` (대문자 표 포인터 칸) 이 NULL 이 아니면 **상수 `0x6a5`** (`+0x263 CMP` / `+0x26a JZ` / `+0x270 MOV EAX,0x6a5` / `+0x275`), NULL 이면 0 (`+0x613`) | 안 씀 (`+0x61` 에서 읽기만) |

- `[관찰]` run44 사진 8장 모두 `ActiveCodePage` = `OemCodePage` = `0x3b5` (949), `UseCaseMapping` = `0x6a5` — **K1 부터**. 이 표와 맞는다.
- 커널 쪽 NLS 상태 (silo 전역 `+0x408` 의 `_RTL_NLS_STATE`) 는 `InitBootProcessor +0x10c RtlNlsInitState` 가 UTF-8 기본값 `0xfde9` 로 먼저 채우고, 뒤에 `ExInitializeNls` 가 레지스트리 코드 페이지로 채운다 (결과서. `RtlNlsInitState +0x18 LEA RDI,[RCX+0x408]` 은 flow-writer 가 확인).

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 03_sprint 열다섯째 바퀴. plan **Q108** (PE_emulator 요청 4-A, 사용자 결정 2026-11-01 로 맨 앞). **짐 지도 바퀴** — `baggage.md` 고침 (아래 "짐 지도 반영").
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID+age `C29EBFB06B78B3C020DCA66D99713F9E1`, ntdll (`ntdll.pdb`) `C3093720177DA6851519DCFFE8DF53FE1` — flow.md 기준과 같다 (결과서 `ms/sym/*.pdb.info.txt` 대조).
- 버전 대조 (결과서): `_calls.tsv` `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`, ntdll `LdrpInitializeProcess` call `+0x1bb6` / ret `+0x1bbb` → `LdrpInitializeGraphRecurse`.
- 값 대조: 새 동적 실행은 없다. run44 덤프 읽기 로그의 VAD 줄 (`ms/logs/04_baggage-read_K1.txt:1373`, `04_baggage-read_P1a.txt:1539 ~ 1540`, `_P1b.txt:2229 ~ 2238`, `_P2.txt:2241 ~ 2250`) 을 flow-writer 가 다시 읽었다.

## 목적

- PE_emulator 가 2-I (kernel32·kernelbase 올리기) 전에 "공사 중" 으로 둔 셋을 진짜 길로 채우게:
  - NLS 글자표 — `nt!NtGetNlsSectionPtr` 종류 `0xb`·`0xe` 가 무엇을 어디에 올리고 무엇을 돌려주나, 섹션은 누가 언제 만드나, ntdll 이 받은 뷰를 어디에 두나.
  - ApiSet 지도 — `MmMapApiSetView` 가 무엇을 올리고 PEB `+0x68` 을 누가 쓰나, 섹션은 부팅 때 누가 만드나.
  - ntdll 이 `PEB.ApiSetMap` 을 처음 읽는 자리와 0 검사.
- 범위 밖 (요청서): Q109 몫 (B~E), NLS 파일 내용·표 바이트, `ApiSetpResolveHost`·`ApiSetComposeSchema*` 본문.

## 방법

- 정적: `_workspace/sast/52_nls-apiset.md` — ntoskrnl·ntdll. 두 모듈 다 이미 analyze 돼 있어 다시 안 걸었다.
  - 요청서 대로: ntoskrnl `NtGetNlsSectionPtr`·`MmMapApiSetView`·`MiInitializeApiSets` depth 1, `RtlpInitNlsSectionName` depth 0. ntdll `RtlInitCodePageTable`·`LdrpFusionManifestCodePages` depth 1, `LdrpInitializeNlsInfo`·`RtlpInitCodePageTables`·`RtlpInitUppercaseTables`·`LdrpPreprocessDllName` depth 0.
  - 결과서가 넓힌 것 (전부 depth 0): ntoskrnl `ExInitializeNls` + 커널 사본 넷 · `RtlNlsInitState` · `MiInitSystem`·`MmInitSystem`·`InitBootProcessor`·`KiInitializeBootStructures` · `PspCreateProcess`, ntdll `RtlpLocateActivationContextSection`.
  - `_refs` (`ms/disassemble/ntoskrnl/_refs/`): `0xe2d818`, `ApiSetLoadSchemaWithExtensions`·`ApiSetLoadSchemaEx`·`ApiSetComposeSchema`, `MiInitSystem`, `MiInitializeApiSets`, `MmInitSystem`, `PspSiloLoadApiSets`, `PspApiSetCopyToSystemSpace`, `ExInitializeNls`, 커널 사본 셋.
  - 문자열·PE 머리 (`ntoskrnl.exe`·`ntdll.dll`·`apisetschema.dll`) 는 결과서가 `pefile` 로 읽었다 (스크래치).
- 결과서: `_workspace/sast/52_nls-apiset_result.md` (**분석성공**).
- 이 문서를 쓰며 다시 맞춘 것 (flow-writer, `.asm`):
  - ntoskrnl: `NtGetNlsSectionPtr.asm` CALL 줄 17개 전부 · `+0x6f ~ +0x88`·`+0xda`·`+0x11d`·`+0x144 ~ +0x155`·`+0x176`·`+0x1c8 ~ +0x1da`·`+0x250 ~ +0x2ab`·`+0x2eb ~ +0x334`·`+0x4fb`. `MmMapApiSetView.asm` `+0x1e ~ +0x108`. `MmCreatePeb.asm` `+0x22a ~ +0x275`·`+0x532 ~ +0x56b`·`+0x609 ~ +0x61c`. `MiInitializeApiSets.asm` CALL 넷·`+0x13`·`+0x33`·`+0xb1`·`+0xb8`·`+0xce`. `ExInitializeNls.asm` CALL 일곱. `MmInitSystem.asm +0xa9 ~ +0x149`, `MiInitSystem.asm +0x75`·`+0x77`·`+0x638`, `InitBootProcessor.asm +0x10c`·`+0x595`, `PspAllocateProcess.asm +0x2397`·`+0x2574`·`+0x25ba`, `PspSetupUserProcessAddressSpace.asm +0x250`, 커널 사본 `RtlpInitCodePageTables`·`RtlpInitUppercaseTables` CALL 줄, `_refs/ExInitializeNls.tsv`·`_refs/MiInitializeApiSets.tsv`.
  - ntdll: `LdrpInitializeNlsInfo.asm` `+0x2a ~ +0x77`, `RtlpInitCodePageTables.asm` `+0x21 ~ +0x15c` (쓰는 줄 전부), `RtlpInitUppercaseTables.asm` 전체, `LdrpPreprocessDllName.asm` `+0x3e`·`+0x46`·`+0x62`·`+0x82`·`+0xc3`·`+0xc6`·`+0xfe`.
  - ntdll `_baggage/globals.tsv`: `GlobalRtlNlsState` (`.data` `0x1ccf90`) · `NlsAnsiCodePage` (`0x1cd048`) · `Utf8TableInfo` (`0x1cd050`) · `NlsMbOemCodePageTag` (`0x1ce438`) · `NlsEmptyLeadByteInfoTable` (`0x1ce440`) · `NlsMbCodePageTag` (`0x1ce640`) — 모두 짐 지도에서 "모름" 이던 전역.
- 문서: 이 run 문서, `flow.md` (기준·2.1 `MmCreatePeb`·`PspSetupUserProcessAddressSpace`·새 절 `MmMapApiSetView`·짝 표, 2.4 트리·새 절 NLS, 2.5 `LdrpPreprocessDllName`, 4장, 5장), `baggage.md` (아래).

## 결과

비유 (관리사무소 = 커널, 관리실 = ntdll):

- 관리사무소는 문을 열 때 (부팅) **글자 사전** (NLS 표) 과 **전화번호 바꿈표** (ApiSet 지도) 를 창고에 한 부씩 만들어 둔다.
- 새 가게 (프로세스) 를 지을 때 관리사무소가 바꿈표 사본 한 장을 가게 벽에 **읽기 전용으로 붙이고**, 안내판 (PEB) 에 "바꿈표는 여기" (`+0x68`) 와 "이 동네 글자는 949번" (`+0x34c` 등) 을 적어 둔다.
- 관리실이 출근하면 안내판의 번호로 창고에 전화 (`NtGetNlsSectionPtr`) 를 걸어 **그 번호의 사전**을 받아 책상 서랍 (`GlobalRtlNlsState`) 에 꽂는다. 사전도 읽기 전용이다.

### 정적 — 호출 지점

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x1c8` | `+0x1cd` | `ntdll!LdrpInitializeNlsInfo` | [정적] (run18·run45) |
| `ntdll!LdrpInitializeNlsInfo` | `+0x2a` | `+0x2f` | `ntdll!LdrpFusionManifestCodePages` | [정적] |
| `ntdll!LdrpInitializeNlsInfo` | `+0x3d` | `+0x42` | `ntdll!RtlpQueryNlsSystemCodePages` | [정적] (`AL = 0` 일 때만) |
| `ntdll!LdrpInitializeNlsInfo` | `+0x5c` | `+0x61` | `ntdll!RtlpInitCodePageTables` | [정적] |
| `ntdll!LdrpInitializeNlsInfo` | `+0x6d` (꼬리 JMP) | — | `ntdll!RtlpInitUppercaseTables` | [정적] |
| `ntdll!RtlpInitCodePageTables` | `+0x46` | `+0x4b` | `ntdll!NtGetNlsSectionPtr` (종류 `0xb`, ANSI) | [정적] |
| `ntdll!RtlpInitCodePageTables` | `+0x6c` | `+0x71` | `ntdll!NtGetNlsSectionPtr` (종류 `0xb`, OEM — ANSI ≠ OEM 일 때만) | [정적] |
| `ntdll!RtlpInitCodePageTables` | `+0x90` | `+0x95` | `ntdll!RtlInitCodePageTable` (ANSI 표) | [정적] |
| `ntdll!RtlpInitCodePageTables` | `+0xa3` | `+0xa8` | `ntdll!RtlInitCodePageTable` (OEM 표) | [정적] |
| `ntdll!RtlpInitUppercaseTables` | `+0x3b` | `+0x40` | `ntdll!NtGetNlsSectionPtr` (종류 `0xe` — `UseCaseMapping ≠ 0` 일 때만) | [정적] |
| `nt!NtGetNlsSectionPtr` | `+0xfd` | `+0x102` | `nt!RtlpInitNlsSectionName` | [정적] |
| `nt!NtGetNlsSectionPtr` | `+0x16d` | `+0x172` | `nt!ZwOpenSection` | [정적] |
| `nt!NtGetNlsSectionPtr` | `+0x1af` | `+0x1b4` | `nt!ObReferenceObjectByHandle` | [정적] |
| `nt!NtGetNlsSectionPtr` | `+0x2e6` | `+0x2eb` | `nt!MiMapViewOfSection` (유저 호출만) | [정적] |
| `nt!NtGetNlsSectionPtr` | `+0x3ac` · `+0x41d` · `+0x458` | `+0x3b1` · `+0x422` · `+0x45d` | `RtlpInitNlsFileName` · `ZwOpenFile` · `ZwCreateSection` (열기 실패 갈래) | [정적] |
| `nt!PspAllocateProcess` | `+0x2397` | `+0x239c` | `nt!PspSetupUserProcessAddressSpace` | [정적] (run10·run32) |
| `nt!PspSetupUserProcessAddressSpace` | `+0x250` | `+0x255` | `nt!MmMapApiSetView` | [정적] (run10·run32) |
| `nt!PspAllocateProcess` | `+0x25ba` | `+0x25bf` | `nt!MmMapApiSetView` (다른 길, test.exe 아님 `[추측]`) | [정적] |
| `nt!MmMapApiSetView` | `+0x1e` · `+0x29` · `+0x7b` · `+0x103` | `+0x23` · `+0x2e` · `+0x80` · `+0x108` | `PsGetProcessServerSilo` · `PsIsHostSilo` · `MmMapViewOfSection` · `PsGetServerSiloGlobals` (silo 만) | [정적] |
| `nt!InitBootProcessor` | `+0x10c` · `+0x595` | `+0x111` · `+0x59a` | `nt!RtlNlsInitState` · `nt!MmInitSystem` | [정적] (부팅) |
| `nt!MmInitSystem` | `+0x144` | `+0x149` | `nt!MiInitSystem` (위상 0·2 가 같은 자리, `+0xb6 XOR ECX,ECX` / `+0x13c MOV ECX,2`) | [정적] (부팅) |
| `nt!MiInitSystem` | `+0x638` | `+0x63d` | `nt!MiInitializeApiSets` (위상 0 쪽, `+0x75 TEST ECX` / `+0x77 JNZ`) | [정적] (부팅) |
| `nt!Phase1InitializationIoReady` | `+0x27` | — | `nt!ExInitializeNls` (`_refs`) | [정적] (부팅) |
| `nt!ExInitializeNls` | `+0x28` · `+0x63` · `+0x79` · `+0x91` · `+0x96` | `+0x2d` · `+0x68` · `+0x7e` · `+0x96` · `+0x9b` | `ZwCreateDirectoryObject` (`\NLS`) · `PsGetCurrentServerSiloGlobals` · 커널 사본 `RtlpQueryNlsSystemCodePages` · `RtlpInitCodePageTables` · `RtlpInitUppercaseTables` | [정적] (부팅) |
| 커널 사본 `RtlpInitCodePageTables` | `+0x69` · `+0x91` | `+0x6e` · `+0x96` | `nt!NtGetNlsSectionPtr` (종류 `0xb`) | [정적] (부팅) |
| 커널 사본 `RtlpInitUppercaseTables` | `+0x2b` | `+0x30` | `nt!NtGetNlsSectionPtr` (종류 `0xe`) | [정적] (부팅) |

### 호출 관계

```
(부팅 — 흐름도 범위 앞, 단계 Ⓑ)
KiInitializeKernel +0x7fe → InitBootProcessor                                    (결과서)
 ├─ +0x111  RtlNlsInitState              커널 silo 전역 +0x408 NLS 상태 = UTF-8 기본값
 └─ +0x59a  MmInitSystem(0)
          └─ +0x149  MiInitSystem(0)
                   └─ +0x63d  MiInitializeApiSets(로더블록)
                            ├─ +0x57  MmCreateSection        pagefile 섹션, 크기 = ApiSetSchemaSize
                            ├─ +0x78  (시스템 공간 뷰, 이름 2개)
                            ├─ +0xb1  (메모리 복사, 이름 4개)  로더 표 → 뷰, 그대로
                            │  +0xb1 · +0xb8 · +0xce          전역 nt+0xe2d818 · 820 · 828
Phase1Initialization → … → Phase1InitializationIoReady
 └─ +0x27  ExInitializeNls
          ├─ +0x2d  ZwCreateDirectoryObject   \NLS (영구)
          ├─ +0x96  (커널 사본) RtlpInitCodePageTables
          │        └─ +0x6e · +0x96  NtGetNlsSectionPtr(0xb, ANSI/OEM)   커널 모드 → 열기 실패 → 파일로 만들기 → 시스템 공간 뷰
          └─ +0x9b  (커널 사본) RtlpInitUppercaseTables
                   └─ +0x30  NtGetNlsSectionPtr(0xe)

(부모 cmd 스레드, ① — NtCreateUserProcess 안)
PspAllocateProcess
 ├─ +0x2314  MmCreatePeb               +0x546 · +0x555 · +0x564  PEB ActiveCodePage · OemCodePage · UseCaseMapping
 └─ +0x239c  PspSetupUserProcessAddressSpace
           └─ +0x255  MmMapApiSetView
                    ├─ +0x23  PsGetProcessServerSilo
                    ├─ +0x2e  PsIsHostSilo           참 (silo 없음) → 전역 섹션 [nt+0xe2d818]
                    ├─ +0x80  MmMapViewOfSection    읽기 전용 · 섹션 전체 · 주소는 커널이
                    │  +0x92  PEB+0x68 ApiSetMap = 뷰 주소

(test.exe 첫 스레드, ② — LdrpInitializeProcess 안)
LdrpInitializeProcess
 └─ +0x1cd  LdrpInitializeNlsInfo
          ├─ +0x2f  LdrpFusionManifestCodePages     test.exe: AL = 0 ("없음")
          ├─ +0x42  RtlpQueryNlsSystemCodePages     레지스트리
          │  +0x4e · +0x55                          PEB +0x34c · +0x34e 다시 씀
          ├─ +0x61  RtlpInitCodePageTables(ANSI, OEM)
          │        ├─ +0x4b  NtGetNlsSectionPtr(0xb, ANSI) ══▶ nt!NtGetNlsSectionPtr +0x172 ZwOpenSection · +0x2eb MiMapViewOfSection · +0x316 *R9 = 뷰
          │        │         → GlobalRtlNlsState+0x80
          │        │  (ANSI == OEM) +0x143  +0x88 = +0x80     / 아니면 +0x71 NtGetNlsSectionPtr(0xb, OEM) → +0x88
          │        ├─ +0x95  RtlInitCodePageTable   → +0x0  DefaultAcpTableInfo
          │        ├─ +0xa8  RtlInitCodePageTable   → +0x40 DefaultOemTableInfo
          │        │  +0xd6 · +0xe8 · +0xef · +0x10b · +0x11e  NlsAnsiCodePage · NlsMbCodePageTag · +0x90 · +0x98 · NlsMbOemCodePageTag
          └─ ⇒ +0x6d  RtlpInitUppercaseTables(UseCaseMapping)
                   └─ +0x40  NtGetNlsSectionPtr(0xe)   (UseCaseMapping ≠ 0 일 때만)
                      +0x55 · +0x6d · +0x79   GlobalRtlNlsState +0xa8 · +0xb0 · +0xa0

(그 뒤 — 2.5 kernel32 준비)
… LdrpLoadDll +0xc2 → LdrpPreprocessDllName
                       +0x62  PEB (GS:[0x60]) → +0x82  RBX = PEB+0x68 ApiSetMap   (0 검사 없음)
                       +0xc3  CMP byte [RBX],7  → 7 이면 +0x103 ApiSetpResolveHost, 아니면 +0xc6 JNZ +0x2b6 (6 이면 +0x5a7 판정 → +0xd4 합류 / 아니면 +0x2bf API-·EXT- 검사)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 줄. `══▶` 는 syscall (호출이 아닌 실행 넘어감). `⇒` 는 꼬리 JMP.

### `LdrpPreprocessDllName` — `PEB.ApiSetMap` 첫 읽기 `[정적]`

- **`+0x82` 는 `PEB.ApiSetMap` 을 읽는다 — 예.** `+0x62 MOV RAX,GS:[0x60]` → `+0x82 MOV RBX,[RAX+0x68]` → **`+0xc3 CMP byte ptr [RBX],7`** (첫 바이트 = 버전). 7 이면 `+0xfe` (ret `+0x103`) `ApiSetpResolveHost`. 버전 6 이면 `+0xc6 JNZ` → `+0x2b6 CMP dword [RBX],6` / `+0x2b9 JZ +0x5a7` 에서 판정: `+0x5a7 CMP dword [RBX+0x10],8` (`JBE`) · `+0x5b1 CMP byte [RBX+0x1c],7` (`JNZ`) · `+0x5bb TEST byte [RBX+0x1e],4` (`JNZ`) 를 다 통과하면 `+0x5cd ADD RBX,0x1c` → `+0x5d1 JMP +0xd4` (지도 시작을 `+0x1c` 로 옮겨 버전 7 길에 합류), 하나라도 걸리면 `+0x2bf`. 6 도 7 도 아니면 (또는 위에서 걸리면) `+0x2bf` 뒤 `+0x2c6 ~ +0x302` 이름 앞 8바이트를 `API-`·`EXT-` 와 비교 (그 뒤 함수 안 `+0x6ff ApiSetpSearchForApiSet`·`+0x734 ApiSetpSearchForApiSetHost` 호출이 어느 갈래 몫인지는 안 읽음 `[공백]`). 서버 `apisetschema.dll` 은 버전 6 · 머리 `+0x10` = 152 (> 8) 라 첫 검사는 통과, `+0x1c`·`+0x1e` 바이트는 결과서 머리 덤프 밖이라 모름 → test.exe 가 합류하는지 `+0x2bf` 로 가는지 `[추측]` 으로 둠 (메모리 표 버전·바이트는 Q110 (2)).
  - (정정, 오케스트레이터 `.asm` 확인) 결과서의 "아니면 `+0xc6 JNZ +0x236` 옛 모양" 의 `+0x236` 은 목적지 주소 `0x18003e236` 끝자리다. 오프셋은 **`+0x2b6`** (함수 시작 `0x18003df80`) 이고, 버전 6 은 자기 갈래 (`+0x5a7`) 가 있다.
- **`+0x82` 와 `+0xc3` 사이에 `RBX` 0 검사가 없다.** 사이의 `TEST` 는 `PEB.SharedData` (`+0x96`), `0x7ffe0384` 바이트 (`+0xa2`), `PEB +0x378` 비트 2 (`+0xb6`) 뿐 (결과서). 그래서 **칸이 0 이면 `+0xc3` 이 주소 0 을 읽는다** — PE_emulator 말이 맞다 `[정적]`.
  - 단 입구 `+0x3e TEST [R12],0x800008` / `+0x46 JNZ +0x366` (넷째 인자 플래그) 이 켜지면 이 길 전체를 건너뛴다. 뜻은 안 정함.
- 이 함수 안에서는 `+0x82` 읽기가 `ApiSetpResolveHost` 호출보다 앞이다. ntdll 이미 뽑은 함수 362개 안에서 `GS:[0x60]` → `[reg+0x68]` 꼴은 4곳 (결과서, `[한계]` — 더 있을 수 있음): `LdrpPreprocessDllName +0x82` · `LdrpLoadDependentModuleInternal +0x278` (run23 `+0x2ff ApiSetpResolveHost` 앞) · `LdrpSnapKernelBaseExtensions +0x90` (`+0x1c5 ApiSetpResolveHost` 앞) · `LdrpLogRelativePathWithAlteredSearchError +0x3e` (로그 전용).
- 코드 순서로는 `LdrpInitializeProcess +0x199d LdrpInitializeKernel32Functions` → (KERNEL32 를 못 찾았을 때) `+0xc0 LdrLoadDll` → `LdrpLoadDll +0xbd / +0xc2 LdrpPreprocessDllName` 이 **첫 읽기 후보**다 (`[관찰]` run7 에서 이 `LdrLoadDll` 이 잡힘). 그 뒤 `LdrpSnapKernelBaseExtensions` (`+0x135`), `LdrpMapAndSnapDependency +0x34e → LdrpLoadDependentModuleInternal`. `+0x195e LdrpInitializeImportRedirection` 안 `LdrpLoadDll` 이 더 앞일 수 있지만 조건부 (test.exe 는 안 탐 — run45). 실제 실행 순서는 동적 (아래 3).

## 확인된 것

- `[정적]` `NtGetNlsSectionPtr` 는 종류 `0xb`·`0xe` 모두 **이름 붙은 섹션을 먼저 연다** (`+0x16d ZwOpenSection`). 없을 때만 같은 이름으로 파일에서 **영구** 섹션을 만든다 (`+0x3ac ~ +0x469`). 부팅 때 `ExInitializeNls` 가 `\NLS` 를 만들고 커널 사본으로 이 함수를 불러 첫 섹션들을 만드는 길이 코드에 있다.
- `[정적]` 유저 호출이면 **현재 프로세스**에 읽기 전용 · 섹션 전체 · 64KB 정렬 · 주소는 커널이 고른 뷰를 올리고 (`+0x2e6 MiMapViewOfSection`), 뷰 주소를 넷째 출력 칸에 쓴다 (`+0x316`). 유저는 셋째 (섹션 개체) 를 못 쓴다 (`0xc00000f1`).
- `[정적]` + `[관찰]` run44: NLS 뷰는 P1a 에 `C_949.NLS` 0x31쪽 · `l_intl.nls` 3쪽, 둘 다 "Mapped READONLY" (`04_baggage-read_P1a.txt:1539 ~ 1540`). 섹션을 만들 때 파일 이름 꼴 (`c_949.nls`·`l_intl.nls`) 과 같다. 이 VAD 를 `RtlpInitCodePageTables +0x46`·`RtlpInitUppercaseTables +0x3b` 호출이 만들었다는 건 콜스택을 안 봐서 `[추측]` (P1a 에 이름이 맞는 VAD 가 한 개씩이라는 것과는 맞음).
- `[정적]` ntdll 의 `NtGetNlsSectionPtr` 부르는 자리는 3곳이고, 실행 횟수는 조건에 달린다: ANSI == OEM 이면 코드 페이지 쪽 1번, `UseCaseMapping` 이 0 이면 대문자 쪽 0번, 둘 중 하나라도 `0xfde9` 면 섹션 안 얻음. test.exe 는 사진 값 (ACP = OCP = `0x3b5`, `UseCaseMapping` `0x6a5`) 으로 보면 **2번** (`+0x46` + `+0x3b`) `[추측]` — P1a 에 NLS VAD 가 한 개씩인 것과 맞음.
- `[정적]` `LdrpFusionManifestCodePages` 는 매니페스트 없는 exe 에서 "없음" (`AL = 0`). 그래서 test.exe 의 코드 페이지는 레지스트리 (`RtlpQueryNlsSystemCodePages`) 에서 온다.
- `[정적]` `RtlInitCodePageTable` 은 CALL·syscall 이 없는 단말 함수다. 표가 NULL 이거나 `0xfde9` 면 `Utf8TableInfo` 를 복사한다.
- `[정적]` ApiSet 섹션은 부팅 위상 0 `MiInitSystem +0x638 MiInitializeApiSets` 가 로더 표를 복사해 만든 pagefile 섹션이다. 호스트 쪽 섹션엔 합치기 (`ApiSetComposeSchema`) 가 없다 — 그걸 부르는 곳은 `PspSiloLoadApiSets +0x4e` 하나 (서버 silo).
- `[정적]` **`PEB.ApiSetMap` 을 쓰는 줄은 `MmMapApiSetView +0x92`** 다 (부르는 쪽이 아님). `[관찰]` run44 K1 값 `25996cd0000` = 읽기 전용 pagefile 섹션 0x2a쪽 VAD 와 맞는다.
- `[정적]` **PEB `ActiveCodePage`·`OemCodePage`·`UseCaseMapping` 은 커널 `MmCreatePeb +0x546`·`+0x555`·`+0x564` 가 먼저 쓴다.** run45 의 "K1 전에 처음 쓴 쪽은 모름" 의 답이다. `[관찰]` run44 "K1 부터 `0x3b5`·`0x6a5`" 와 맞는다.
- `[정적]` ntdll `LdrpPreprocessDllName +0x82` 가 `PEB.ApiSetMap` 을 읽고 `+0xc3` 이 0 검사 없이 첫 바이트를 읽는다.

## 정정

- **PE_emulator 가 적은 자리** (원문 `../PE_emulator/_workspace/orchestrator/37_nls-codepage.md`, 이 레포 문서 아님 — 고치지 않음):
  - "`LdrpInitializeNlsInfo +0x46`·`+0x90`·`+0xa3`" 은 **`RtlpInitCodePageTables`** (ntdll rva `0xec2fc`, 349B) 의 **call 자리**다. `LdrpInitializeNlsInfo` 는 143B (`0x8f`) 라 `+0x90`·`+0xa3` 은 함수 밖이고, 그 함수 `+0x46` 은 call 이 아니라 `MOV ECX,[RSP+0x30]` (합류점) 이다.
  - 그리고 `+0x90`·`+0xa3` 이 부르는 건 `NtGetNlsSectionPtr` 가 아니라 **`RtlInitCodePageTable`** 이다. `NtGetNlsSectionPtr` 는 `+0x46` (와 조건부 `+0x6c`) 뿐.
  - WinDbg 콜스택 숫자로 쓰려면 ret 자리: `+0x4b`·`+0x71` (syscall), `+0x95`·`+0xa8` (`RtlInitCodePageTable`), `RtlpInitUppercaseTables +0x40`.
- **run45 "`NtGetNlsSectionPtr` ×3 (코드 페이지 표 2 + 대문자 표 1)"** (일 47) — 틀린 건 아니고 **"부르는 자리 3곳"** 이다. 실행 횟수는 조건부이고 test.exe 는 2번으로 보인다 `[추측]` (위 "확인된 것"). `runs/03_sprint/run45-work-list.md` 맨 위에 정정 안내를 한 줄 넣었다. flow.md 2.4 의 "×2"·"×1" 도 "자리" 로 고쳐 썼다.
- **결과서 오프셋 셋** (뜻은 같고 숫자만 — 이 문서·flow.md·baggage.md 는 고친 값으로 씀):
  - `NtGetNlsSectionPtr` 의 `MiMapViewOfSection` call 은 `+0x2e1` 이 아니라 **`+0x2e6`** (ret `+0x2eb`). `+0x2e1` 은 `MOV RCX,[RSP+0x48]` (섹션 인자).
  - `RtlpInitCodePageTables` 의 UTF-8 갈래 `NlsAnsiCodePage = 0xfde9` 는 `+0x44b` 가 아니라 **`+0x14f`** (주소 `…ec44b` 끝자리를 적은 것 — run56 의 `+0x17` 과 같은 꼴).
  - `LdrpPreprocessDllName +0xc6 JNZ` 목적지는 `+0x236` 이 아니라 **`+0x2b6`** (주소 `…e236` 끝자리, 오케스트레이터 확인). 버전 6 은 `+0x2b9 JZ +0x5a7` 로 따로 판정한다 (위 "`LdrpPreprocessDllName`" 절).
  - `MmMapApiSetView` 의 silo 갈래 `PsGetServerSiloGlobals` call 은 `+0x100` 이 아니라 **`+0x103`** (ret `+0x108`). `ExInitializeNls` 의 `PsGetCurrentServerSiloGlobals` call 은 `+0x6f` 가 아니라 **`+0x63`** (ret `+0x68`).

## 짐 지도 반영 (`baggage.md`)

- 3.1 `_PEB` 줄 3개 더함: `+0x68 ApiSetMap` (① nt `MmMapApiSetView +0x92`), `+0x34c`·`+0x34e` (① nt `MmCreatePeb +0x546`·`+0x555`, 기존 ② ntdll 줄 앞), `+0x350 UseCaseMapping` (① nt `MmCreatePeb +0x564`). 모르던 칸 2개 (`ApiSetMap`·`UseCaseMapping`) 가 아는 칸이 됨.
- 3.8 ntdll `.data` 줄 9개 더함: `GlobalRtlNlsState` 6줄 · `NlsAnsiCodePage` · `NlsMbOemCodePageTag` · `NlsMbCodePageTag`. 모르던 전역 4개가 "쓴 자리 아는 것" 이 됨. `+0x80`·`+0x88` 은 syscall 로 커널이 씀 → 그 전역은 nt·ntdll 양쪽에 셈.
- 4장 짝 `PEB.ApiSetMap` 줄 고침 (쓰는 줄 `[공백]` → `+0x92`) + NLS 짝 두 줄 (뷰 ↔ `\NLS` 섹션, PEB 코드 페이지 ↔ silo 전역 NLS 상태).
- 5장 `Ⓑ` 에 둘 (`ⒷA` 위상 0 ApiSet, `ⒷN` `ExInitializeNls`), ① `MmCreatePeb`·`MmMapApiSetView`, ② NLS 순서.
- 7.6.3 새 절 (체크리스트 밖 — NLS·ApiSet 뷰와 부팅 때 놓인 섹션, C8 일부), 7.18 새 절 (동적 질문 1~5).
- 2장 숫자: `_PEB` nt 22 → 26 · 모름 88 → 86, `.data` nt 1 → 2 · ntdll 64 → 68 · 모름 373 → 369. 구조체 아는 칸 702 → 704, 모름 337 → 335. 전역 쓴 자리 아는 것 76 → 80, 모름 417 → 413.

## 아직 확인 필요

결과서 "동적분석으로 확인해야 할 것" 1 ~ 5 (번호 그대로) = 대기열 **Q110**. 6 (NLS 파일 내용·크기) = **Q104 VM 단계**. 결과서는 Q104 VM 단계에 얹을 수 있다고 봤다.

1. **test.exe 때 `nt!NtGetNlsSectionPtr` 입구** (커널 멈춤점 → 프로세스 조건 필요): 몇 번 서나·콜스택 — 기대 `RtlpInitCodePageTables +0x4b` (종류 `0xb`) · `RtlpInitUppercaseTables +0x40` (종류 `0xe`), `+0x71` 은 안 옴.
   - **P1b 이후 두 번째 `C_949.NLS` (`25996eb0`, 0x31쪽) · `l_intl.nls` (`25996dc0`, 3쪽) 뷰는 누가 부른 것인가** — 정적으로 못 찾음 `[공백]` (`ms/logs/04_baggage-read_P1b.txt:2236 ~ 2238`, P1a 엔 없음). `P1a~P1b` 구간이라 KERNELBASE 쪽 `[추측]`.
   - 열기 실패 갈래 (`+0x176 JS` → `+0x3b1 RtlpInitNlsFileName`) 를 test.exe 때 타나 — 정적 읽기로는 "안 탐".
   - 돌아올 때 넷째 칸이 가리킨 값·`+0x332` 의 `EBX` (반환 상태).
2. **ApiSet 표 머리말 대조**: `dq nt+0xe2d818 L3` (섹션 개체·커널 뷰·크기) → 커널 뷰 첫 `0x20` 바이트를 `apisetschema.dll` `.apiset` 머리 (버전 6, 크기 171104, 플래그 0, 개수 984, 항목 오프셋 152, 해시 오프셋 43568, 해시 인자 31) 와 비교. 크기·개수가 다르면 로더가 확장을 합쳐 넘긴 것. 크기 칸이 `171104` 인지. 그리고 머리 **`+0x1c`·`+0x1e` 바이트** (`LdrpPreprocessDllName +0x5b1`·`+0x5bb` 판정 — 버전 6 표가 `+0xd4` 길로 합류하나).
3. **`PEB +0x68` 첫 읽기 자리·순서**: 후보 `ntdll!LdrpPreprocessDllName` · `LdrpSnapKernelBaseExtensions` · `LdrpLoadDependentModuleInternal` 중 맨 먼저 서는 것 (`LdrpInitializeKernel32Functions +0xc0` 이 `LdrLoadDll` 을 타는지 포함). 더 확실하게는 test.exe 문맥에서 `ba r8 <PEB+0x68>` 로 첫 읽는 명령 주소.
4. **`LdrpInitializeNlsInfo` 때 `PEB +0x2f8` (`ActivationContextData`)** (예상 0) 과 `LdrpInitializeNlsInfo +0x2f` 직후 `r al` (예상 0). baggage 7.9 D3 ("매니페스트 `heapType` 조회가 `ActivationContextData` null 이라 음수로 끝나는지") 도 같은 `RtlQueryActivationContextApplicationSettings` 길이라 이 값으로 같이 닫힌다.
5. **읽을 전역**: ntdll `GlobalRtlNlsState` (rva `0x1ccf90`, `dt ntdll!_RTL_NLS_STATE`) · `NlsAnsiCodePage` (rva `0x1cd048`), 커널 silo 전역 `+0x408` (`_RTL_NLS_STATE`). P1a 덤프로도 읽을 수 있을 것 `[추측]` (`.data` 는 P1a 부터 읽힘).

- (결과서 6, 이미 Q104 VM 단계 몫) NLS 파일 내용·크기 — VM 에서 `c_949.nls`·`l_intl.nls`·`locale.nls` 복사. 관찰 뷰는 0x31쪽·3쪽.
- 정적으로 남은 것 (대기열 없음, 필요해지면): 로더 (`winload`) 가 `ApiSetSchema` 에 확장을 합치는지 (범위 밖 바이너리), `PspAllocateProcess` 길 2 의 비트 8·4·1 뜻, `PsCreateMinimalProcess` 가 어느 길인지, `MiMapViewOfSection` 본문 (주소 고르는 방향), `0x400000` 할당 종류 이름.

## 도구 메모

- 결과서 "스크립트 문제" 없음. `NtGetNlsSectionPtr.asm` 끝의 `$filt$0` 예외 함수·"읽지 않은 틈" 은 예외 착지점이라 지장 없음 (결과서).
- **같은 이름이 두 모듈에 있다** (`RtlInitCodePageTable`·`RtlpInitCodePageTables`·`RtlpInitUppercaseTables`·`RtlpQueryNlsSystemCodePages`). `ms/disassemble/ntoskrnl/` 쪽은 커널 사본 (rva `0x78a980`·`0x78ad34`·`0x78aedc`·`0x78af7c`), `ms/disassemble/ntdll/` 쪽이 유저 사본. 오프셋이 다르다 (예: `NtGetNlsSectionPtr` 부르는 자리 커널 `+0x69`·`+0x91` / ntdll `+0x46`·`+0x6c`). 문서에 쓸 때 모듈을 꼭 붙인다.
- 결과서 오프셋 중 넷은 다른 줄 (call 앞 `MOV`) 이나 주소 끝자리를 적은 것이었다 (위 "정정"). run56 에 이어 두 번째 — 결과서 오프셋은 flow-writer 가 `.asm` CALL 줄로 한 번 더 맞추는 게 좋다 (하네스 제안은 보고에만).

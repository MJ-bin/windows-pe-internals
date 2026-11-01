# 52 nls-apiset — 정적 분석 요청

- 날짜: 2026-11-01
- 스프린트: 03_sprint (짐 지도 바퀴)
- 대기열: **Q108** (PE_emulator 요청 4-A, 사용자 결정 2026-11-01: 맨 앞)
- 관련: run45 일 47 (NLS)·일 19 (읽기 전용 뷰 셋)·결과서 sast/39 4.3 #5 (`NtGetNlsSectionPtr` 커널 쪽 "정한 곳 없음"), run32 (`PspSetupUserProcessAddressSpace` 뷰 올리기), run20 (`LdrpLoadDll +0xc2 LdrpPreprocessDllName`), run23 (`ApiSetpResolveHost`), baggage.md `PEB.ApiSetMap` (+0x68) "칸 쓰는 줄 `[공백]`" (1192줄 근처, 7장 1541줄 근처), baggage 3.1 `ActiveCodePage`·`OemCodePage`
- 왜: PE_emulator 가 2-I (kernel32·kernelbase 올리기) 에 닿기 전에 NLS 글자표와 ApiSet 지도를 진짜 길로 채워야 한다. 지금 그쪽은 이 셋을 "공사 중" 으로 둠
- 원문: `../PE_emulator/_workspace/orchestrator/37_nls-codepage.md` "internals 요청 4" 절 A (읽기만, 고치지 않음)

## 알고 싶은 것

### A-1. NLS 글자표 (ⓞ)

1. **`nt!NtGetNlsSectionPtr`** (rva `0xa27da0`, 1298B) — 종류 `0xb` (코드 페이지) 와 `0xe` (대문자 표) 일 때 각각:
   - 어떤 섹션 객체를 쓰나 (객체 이름 / 파일 이름 — 예: `C_<번호>.NLS`, `l_intl.nls` 인가). 이름 만드는 곳 `RtlpInitNlsSectionName` (rva `0xa282c0`) 이 쓰는 꼴
   - 섹션이 없을 때 **이 함수 안에서 처음 만드나** (파일 열기 → 섹션 만들기 → 이름 붙여 넣기), 아니면 **부팅 때 누가 이미 만들어 둔 것**을 여나. 부팅이면 만드는 함수 이름 (caller 사슬 이름만)
   - 호출한 프로세스 주소 공간에 올리는 줄 (`MmMapViewOfSection` 등) 과 인자: 크기, 보호 (`PAGE_READONLY`?), 주소 고르는 법
   - 유저에게 돌려주는 것 (출력 인자마다: 뷰 주소, 크기 등) 과 실패 상태값
   - 종류 `0xb`·`0xe` 말고 다른 종류 갈래는 **이름·번호만** (본문 안 봄)
2. **ntdll `RtlInitCodePageTable`** (rva `0xec460`, 224B) 본문 — 입력 (표 주소) 에서 무엇을 읽어 어느 구조체 칸에 쓰나 (`CPTABLEINFO` 칸 이름은 PDB 로 확인), 하위 호출·syscall 있나
3. **ntdll `LdrpFusionManifestCodePages`** (rva `0xec548`, 257B) 본문 — 매니페스트 없는 exe (test.exe: 리소스 디렉터리 0) 에서 **0 (또는 "없음") 을 돌려주나?** 예/아니오 + 그 갈래 줄. 돌려주는 값 꼴 (코드 페이지 번호 둘? 상태값?) 과 `LdrpInitializeNlsInfo` 가 그 값을 어떻게 쓰나
4. **부르는 쪽 자리 확인** — PE_emulator 가 적은 자리: `LdrpInitializeNlsInfo` (rva `0xec264`) `+0x46`·`+0x90`·`+0xa3`, `RtlpInitUppercaseTables` `+0x3b`
   - **주의**: 지도에서 `LdrpInitializeNlsInfo` 크기는 143B (`0x8f`) 라 `+0x90`·`+0xa3` 은 함수 밖이다. 어느 함수의 자리인지 (예: `RtlpInitCodePageTables` rva `0xec2fc`, 349B?) 먼저 바로잡고, 그 자리마다 무엇을 부르나 (`NtGetNlsSectionPtr` 종류 번호·두째 인자) 표로
   - run45 가 적은 `NtGetNlsSectionPtr ×3` (코드 페이지 표 2 + 대문자 표 1) 과 맞는지

### A-2. ApiSet 뷰 (ⓚ)

5. **`nt!MmMapApiSetView`** (rva `0x930e78`, 273B) 본문:
   - 어느 섹션을 올리나 — 전역 이름 (PDB), silo 갈래 (`PsGetProcessServerSilo`·`PsIsHostSilo`·`PsGetServerSiloGlobals`) 에서 보통 프로세스 (silo 없음) 가 타는 쪽
   - `+0x80 MmMapViewOfSection` 인자: 크기, 보호, 주소 고르는 법
   - **PEB `+0x68` (`ApiSetMap`) 을 누가 쓰나** — 이 함수 안인가, 부르는 쪽 (`PspSetupUserProcessAddressSpace +0x255` 뒤) 인가. 쓰는 줄 +0x
   - 둘째 caller `PspAllocateProcess +0x25ba` (지도 `_edges.tsv`) 는 어떤 길인가 (test.exe 가 타나, 이름·조건만)
6. **그 섹션은 부팅 때 누가 만드나** — `MiInitializeApiSets` (rva `0xc6920c`, 221B) 인가, 그 caller 사슬 (이름만). **`apisetschema.dll` 의 표 (`.apiset` 섹션?) 를 그대로 올리나**, 아니면 `ApiSetComposeSchema*` 로 합친 새 표인가 — 예/아니오 + 근거 줄. 합친다면 보태는 입력이 무엇인지 이름만

### A-3. 알림 확인 (ⓜ)

7. `ntdll!LdrpPreprocessDllName` (rva `0x3df80`) `+0x82`·`+0xc3` 이 `PEB.ApiSetMap` (+0x68) 을 읽나? (`LdrpLoadDll +0xbd/+0xc2`, run20) — 예/아니오 + 줄
   - `+0x82` 에서 0 검사가 있나. 칸이 0 이면 `+0xc3` 이 주소 0 을 읽는다는 PE_emulator 말이 맞나
   - 이게 run23 `ApiSetpResolveHost` (2-J) 보다 **시간상 앞의 첫 읽기**인가. ntdll 초기화 길 (`LdrpInitializeProcess` 아래) 에서 그보다 앞에 `+0x68` 을 읽는 함수가 있으면 **이름·자리만** (본문 안 봄)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^NtGetNlsSectionPtr$` | 1 | 1 |
| ntoskrnl | `^RtlpInitNlsSectionName$` | 0 | 1 (이름 꼴) |
| ntoskrnl | `^MmMapApiSetView$` | 1 | 5 |
| ntoskrnl | `^MiInitializeApiSets$` | 1 | 6 (caller 사슬은 이름만) |
| ntdll | `^RtlInitCodePageTable$` | 1 | 2 |
| ntdll | `^LdrpFusionManifestCodePages$` | 1 | 3 |
| ntdll | `^LdrpInitializeNlsInfo$`, `^RtlpInitCodePageTables$`, `^RtlpInitUppercaseTables$` | 0 | 4 (부르는 자리) |
| ntdll | `^LdrpPreprocessDllName$` | 0 | 7 (`+0x82`·`+0xc3` 근처만) |

- 정확한 이름은 `_index.tsv` 로 확인하고, 없으면 `pdb_aliases.py --where` 로 한 번 더 찾는다.
- 공용 부품 (`MmMapViewOfSection`, `ObOpenObjectByName`, `ZwCreateSection`, 문자열·락) 은 본문 안 보고 이름·인자만.

## 지도에서 본 것

- ntoskrnl (`_map/NtCreateUserProcess_*`): `MmMapApiSetView` 깊이 2, caller 2 (`PspSetupUserProcessAddressSpace +0x250/+0x255`, `PspAllocateProcess +0x25ba/+0x25bf`), 직접 호출 `+0x23 PsGetProcessServerSilo` · `+0x2e PsIsHostSilo` · `+0x80 MmMapViewOfSection` · `+0x108 PsGetServerSiloGlobals`
- ntoskrnl `_index.tsv`: `NtGetNlsSectionPtr` `0xa27da0` 1298B, `ZwGetNlsSectionPtr` `0x6abe50`, `RtlpInitNlsSectionName` `0xa282c0` 195B, `MiInitializeApiSets` `0xc6920c` 221B, `MmQueryApiSetSchema` `0x4c2d1c` 21B. `NtGetNlsSectionPtr` 는 `NtCreateUserProcess` 지도에 없음 (유저가 syscall 로 부름)
- ntdll (`_map/LdrInitializeThunk_*`): `LdrpInitializeNlsInfo` `0xec264` 143B 깊이 5 · `LdrpFusionManifestCodePages` `0xec548` 257B 깊이 6 · `RtlpQueryNlsSystemCodePages` `0xec890` 606B 깊이 6 · `RtlpInitCodePageTables` `0xec2fc` 349B 깊이 6 · `RtlpInitUppercaseTables` `0xf9804` 143B 깊이 6 · `RtlInitCodePageTable` `0xec460` 224B 깊이 7 · `NtGetNlsSectionPtr` 스텁 `0x162de0` 깊이 7 caller 3 (syscall) · `LdrpPreprocessDllName` `0x3df80` 3052B 깊이 7 caller 6

## 이미 아는 것

- `[정적]` `LdrpInitializeNlsInfo` 호출: `+0x2f LdrpFusionManifestCodePages` → `+0x42 RtlpQueryNlsSystemCodePages` → `+0x61 RtlpInitCodePageTables` (`NtGetNlsSectionPtr ×2`) → 꼬리 JMP `RtlpInitUppercaseTables` (`NtGetNlsSectionPtr`). 매개변수 `Flags` 비트 27 이면 곧장 `0xfde9` (`+0x77`). PEB `ActiveCodePage` (+0x34c, `+0x4e`)·`OemCodePage` (+0x34e, `+0x55`) 를 씀 (run45)
- `[관찰]` run44 사진: `ActiveCodePage`·`OemCodePage` 는 8장 모두 `0x3b5` (949), K1 부터 이미 값. VAD 이름에 `C_949.NLS`·`l_intl.nls` 가 보임 (run44)
- `[정적]` `PspSetupUserProcessAddressSpace` 순서 `… +0x1e6 PspPrepareSystemDllInitBlock → +0x255 MmMapApiSetView → +0x263 PspMapSiloSharedDataView → KiUnstackDetachProcess` (run10·run32)
- `[관찰]` run44: PEB `+0x68 ApiSetMap` = `25996cd0000`, K1 에 이미 있음 (커널 몫)
- `[정적]` run23: `LdrpLoadDependentModule +0x2ff ApiSetpResolveHost(PEB.ApiSetMap, …)` — 맵 모양 (첫 바이트 7, 또는 버전 6 …) 갈래
- `[추측]` (PE_emulator) `LdrpPreprocessDllName +0x82`·`+0xc3` 이 `ApiSetMap` 첫 읽기, 칸이 0 이면 `+0xc3` 이 주소 0 을 읽음
- 서버 `ms/bin/.../System32/` 에 `apisetschema.dll` 은 있고 `.nls` 파일은 0개 — NLS 파일 **내용**은 이번에 안 본다 (Q104 VM 단계에서 복사)

## 이번에 안 하는 것

- Q109 몫 (B·C·D·E: `InitSecurityCookie` 등 ntdll 본문, ntoskrnl 비트 뜻, 기록 정정 — `RtlpQueryNlsSystemCodePages` 문자열 셋 ⓠ 도 Q109)
- NLS 파일 내용·표 바이트 (VM 복사 뒤)
- `ApiSetpResolveHost` 본문 (run23 에 있음), `ApiSetComposeSchema*` 본문 (이름만)

## 받고 싶은 것

- 질문 1~7 별 답 (예/아니오 또는 이름) + 근거 (`.asm` 의 `+0x` 줄)
- PE_emulator 가 그대로 쓸 수 있는 **작은 표 둘**:
  - NLS: 종류 (`0xb` / `0xe`) × (섹션 이름 꼴 · 만드는 때 · 뷰 크기 · 보호 · 돌려주는 출력)
  - ApiSet: (섹션 출처 · 만드는 함수 · 뷰 크기 · 보호 · PEB `+0x68` 쓰는 줄)
- 짐 지도 반영거리: `PEB.ApiSetMap` 쓴 자리·경로, NLS 뷰 (C8 "주소 아는 유저 구역") 의 주인
- 동적으로 확인할 것 (있으면 — Q104 VM 단계에 얹을 수 있게 멈춤점·읽을 전역)

# 2026-10-22 run18 — `LdrpInitializeProcess` 앞쪽(`+0x0 ~ +0x12dd`): PEB.Ldr → IFEO → 프로세스 힙 → `\KnownDlls` → ntdll 모듈 항목. 이 구간은 아무것도 메모리에 올리지 않는다 (정적)

> **정정 (run19)**: 묶음 10 의 "exe 폴더 잘라 두기(`[RSP+0x130]`)" 는 틀렸다. `ImagePathName` 의 마지막 `\` **뒤** (= 파일 이름 부분) 를 잘라 두는 것이고, 뒤쪽 `+0x1408` 이 exe 항목 `BaseDllName`(test.exe) 에 넣는다. 입구 `RCX` 의 "로더 문맥" 은 정확히는 CONTEXT 레코드 (첫 스레드의 맡긴 문맥, `+0x18b4`) 다. → `run19-proc-init-modules.md` "정정"

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: ntdll (`ntdll.dll`, 26100.9457), pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`)
  - analyze 는 다시 안 했다. `LdrpInitializeProcess.asm` 은 하네스 O1a·O1b 뒤에 뽑은 파일이라 다시 안 뽑았다 (결과서).
  - 이름 확인: `pdb_aliases.py --where` 로 `LdrpInitializeProcess` = rva `0x8c404`, 이름 하나뿐 (run17 의 `LdrpInitialize` 같은 겹침 없음).
  - 버전 대조: 일치. `_calls.tsv` 의 `LdrpInitializeProcess +0x1bb6 → ret +0x1bbb LdrpInitializeGraphRecurse` (run7 콜스택 `LdrpInitializeProcess+0x1bbb` 와 맞음), `_LdrpInitialize +0x167 → ret +0x16c LdrpInitializeProcess` (run7).
- 근거로 읽은 함수
  - `LdrpInitializeProcess` (rva `0x8c404`, 7750바이트) 의 `+0x0 ~ +0x12dd` 만.
  - 넓힌 것 1개 (depth 0, 한 곳만): `LdrpInitializeExecutionOptions` — `+0x587` 조건 값이 어디서 오나 보려고.
  - 전역 변수 참조(`refs`) 5개 → `ms/disassemble/ntdll/_refs/{LdrpIsHotPatchingEnabled,UseWOW64,LdrpNtdllHotPatchContext,PebLdr,0x1d2978}.tsv`

## 목적

- 대기열 **Q9a** (Q9 를 둘로 나눈 앞쪽). flow.md 2.4 `LdrpInitializeProcess` 절은 `[관찰]` 3줄(전부 뒤쪽) + `[추측]` 1줄뿐이었다. 앞쪽 구간이 **무엇을 어떤 순서로** 하는지 묶음으로 정리한다.
- 덤으로: 입구 인자 `RCX`·`RDX` 를 어디에 쓰나 (run17 이 Q9 로 미룸), 이 구간에서 DLL 을 올리나, PEB.Ldr 가 언제 생기나, 디버거 때문에 갈리는 길.
- 뒤쪽 `+0x12de ~ 끝` 은 **이번에 안 봤다** (Q9b).

## 방법

- 정적: `_workspace/sast/13_proc-init-setup.md` — ntdll, `^LdrpInitializeProcess$` depth 1, 범위 `+0x0 ~ +0x12dd` (+ `LdrpInitializeExecutionOptions` depth 0, refs 5개)
- 결과서: `_workspace/sast/13_proc-init-setup_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntdll/LdrpInitializeProcess.asm`, `ms/disassemble/ntdll/LdrpInitializeExecutionOptions.asm`
  - `ms/disassemble/ntdll/_calls.tsv`, `ms/disassemble/ntdll/_refs/*.tsv`
- 문자열(`\KnownDlls`, `KnownDllPath`, IFEO 값 이름)은 결과서가 **원본 바이트**로 읽었다. 구조체 칸 이름(`_PEB_LDR_DATA`, `_TEB.WowTebOffset`)은 PDB 타입으로 맞췄다.
- 오프셋 읽는 법 (run9 ~ 17 과 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**.
- **호출 개수 정정**: 요청서·plan 은 "앞쪽 95곳 / 뒤쪽 74곳" 이었는데, `+0x12dd` 까지 세면 **94곳** 이다 (`_calls.tsv`·지도·`.asm` 셋 다). 경계 `+0x12de LdrpInitParallelLoadingSupport` 를 뒤쪽에 넣으면 뒤쪽은 **75곳** (합 169).

## 결과

한 줄로: **앞쪽은 "이사 들어가기 전 집 안 정리" 다. 모듈 목록 칸(PEB.Ldr)을 비워서 걸고, 실행 옵션(IFEO)을 읽고, 프로세스 힙을 만들고, 시스템 폴더·`\KnownDlls` 위치를 알아 두고, 마지막에 ntdll 자기 자신을 모듈 목록에 처음 올린다. 새 DLL 은 하나도 안 올린다.**

- 비유: 이삿짐(다른 DLL)은 아직 안 들어왔다. 지금은 주소록 공책을 펴 두고(PEB.Ldr), 집주인 메모를 읽고(IFEO), 창고를 만들고(힙), 동네 공용 창고 위치를 적어 두고(`\KnownDlls`), 주소록 첫 줄에 집주인 자기(ntdll) 이름을 쓴다. exe 이름은 다음 장(뒤쪽)에서 쓴다.

### 정적 — 입구 인자

| 인자 | 받는 곳 | 앞쪽에서 처음 쓰는 곳 | 다른 쓰임 (앞쪽) | 표시 |
|---|---|---|---|---|
| `RDX` (커널이 넘긴 값, run17) | `+0x2d MOV RSI,RDX`, `+0x30 MOV [RSP+0xd8],RDX` | **`+0x3f9 MOV RDX,RSI`** → `+0x400 RtlImageNtHeaderEx` 로 이 주소의 헤더를 찾음. 곧 `+0x415 RtlInsertInvertedFunctionTable(RSI, SizeOfImage)` | `+0x684`→`+0x68c` 전역 **`LdrpSystemDllBase`** 에 저장, `+0x1195` → `+0x119d LdrpInitializeNtdllDataTableEntry` 첫째 인자 | [정적] |
| `RCX` (로더 문맥, run17) | `+0x38 MOV [RSP+0xe0],RCX` | **앞쪽에선 안 씀.** 처음 읽는 곳은 뒤쪽 `+0x17f4` (Q9b) | — | [정적] |

- 그래서 **본문은 `RDX` 를 ntdll 이미지 시작 주소로 쓴다** [정적] (ntdll 주소를 담는 이름의 전역에 넣고, 그 주소로 헤더를 읽고, ntdll 모듈 항목을 만든다). run16·17 의 "`[추측: ntdll 주소]`" 를 코드 쓰임으로 뒷받침한다. 실제 값이 ntdll 시작 주소와 같은지는 동적.
- 입구에서 같이 하는 일: 저장 레지스터 5개, 지역 변수 `0x410` 바이트, 보안 쿠키, 지역 구조체 0 채우기. `+0x13b R13 = GS:[0x30]`(TEB), `+0x14c RDI = [TEB+0x60]`(PEB) — 이후 `RDI` 가 PEB 다.

### 정적 — 앞쪽 큰 순서 (호출 94곳, 10묶음)

"잡일" = `memset`, `RtlAppendUnicodeStringToString`, `RtlAppendUnicodeToString`, `LdrpLogInternal`, `RtlAllocateHeap`, `RtlFreeHeap`, `__security_check_cookie`.

| # | 범위 | 요약 | 대표 호출 (call / ret) | 호출 수 (잡일) | 표시 |
|---|---|---|---|---|---|
| 1 | `+0x0 ~ +0x157` | 입구 정리, 예외 필터 자리에 "빈 값" 암호화 저장(`+0x12f RtlEncodePointer`), TEB·PEB 꺼내기, 첫 스레드 난수 준비 | `LdrInitializePerThreadRng` `+0x153 / +0x158` (RCX = TEB, 조건 없음) | 4 (2) | [정적] |
| 2 | `+0x158 ~ +0x28d` | **PEB.Ldr 만들기** (아래 표), NLS 준비, CPU 2개 이상이면 스핀 횟수 3곳 설정, 비휘발 플러시 준비 | `LdrpInitializeNlsInfo` `+0x1c8 / +0x1cd` (RCX = PEB) | 6 (4) | [정적] |
| 3 | `+0x292 ~ +0x4a1` | exe 경로 앞 `\??\` 떼기, exe NT 헤더 찾기(`+0x35e` → `LdrpAppHeaders`), 보안 프로세스 검사, `LdrpHashTable` 32칸 비우기, **ntdll 을 역함수 표에 등록**, `.mrdata` 쓰기 열고 닫기, 중요 thunk 잡기, CFG 확인 | `RtlInsertInvertedFunctionTable` `+0x415 / +0x41a` | 12 (1) | [정적] |
| 4 | `+0x4a8 ~ +0x579` | 무결성 설정 비트 읽기(`LdrSystemDllInitBlock+0xa0`) 후 **실행 옵션(IFEO) 읽기** | `LdrpInitializeExecutionOptions` `+0x531 / +0x536` | 2 (1) | [정적] |
| 5 | `+0x57a ~ +0x773` | 전역 켜고 끄기: 핫패치 끄기(`+0x587`), `UseWOW64` 정하기(`+0x5f2`), .NET(COR) 이미지 검사, 프로세스 매개변수 정규화, LoadConfig 의 `NtGlobalFlag`·타임아웃 반영, `LdrpDebugFlags` | `RtlNormalizeProcessParams` `+0x6c0 / +0x6c5` | 5 (1) | [정적] |
| 6 | `+0x774 ~ +0x9e4` | 임계 구역 디버그 정보 목록, **PEB 의 TLS 비트맵**(PEB+0x78/+0x238)·FLS 자리, 스택 추적 DB 옵션, **`FastPebLock`** 만들어 PEB+0x38 에 연결 | `RtlInitializeCriticalSectionEx` `+0x9cd / +0x9d2` | 8 (2) | [정적] |
| 7 | `+0x9e5 ~ +0xb7d` | **프로세스 힙 만들기** (힙 관리자 → 힙 정보 → `LdrpInitializeProcessHeap`, 결과가 PEB+0x30 `ProcessHeap`). 이어 기능 스위치 2개, 로더 스냅 버퍼, 활성화 컨텍스트 스택, `LdrpHeap` = PEB 힙, 시스템 정보 조회(클래스 0xc5), 태그 힙 | `LdrpInitializeProcessHeap` `+0xa4b / +0xa50` | 13 (2) | [정적] |
| 8 | `+0xb83 ~ +0xc41` | 스레드 풀 패키지 초기화, ETW 공급자 등록 3곳, 힙 로깅, **환경 블록·매개변수 블록 준비** | `TpInitializePackage` `+0xb83 / +0xb88` (같은 묶음 `RtlpInitEnvironmentBlock` `+0xc2d / +0xc32`, `RtlpInitParameterBlock` `+0xc3a / +0xc3f`) | 11 (0) | [정적] |
| 9 | `+0xc47 ~ +0x1168` | 디버그 페이지 힙 옵션, **시스템 경로 문자열**(`<NtSystemRoot>\System32` → `LdrpSystem32`, `RtlpSystem32Dirs`), **`\KnownDlls` 디렉터리 열기 → `KnownDllPath` 링크 열기·읽기** (보호 프로세스가 아닐 때, `+0xd57`), DLL 정책, 대체 forwarder 경로 | `NtOpenDirectoryObject` `+0xdb8 / +0xdbd` (뒤이어 `NtOpenSymbolicLinkObject` `+0xe47 / +0xe4c`, `NtQuerySymbolicLinkObject` `+0xe90 / +0xe95`, `LdrpInitializePolicy` `+0xfc5 / +0xfca`) | 29 (17) | [정적] |
| 10 | `+0x1169 ~ +0x12dd` | exe 폴더 종류 검사, exe 폴더 잘라 두기(`[RSP+0x130]`), **ntdll 모듈 항목 만들기**, (있으면) 핫패치 ntdll 항목, 히스토리 표, ntdll 항목을 초기화 순서 목록에 연결 | `LdrpInitializeNtdllDataTableEntry` `+0x119d / +0x11a2` | 4 (0) | [정적] |
| 합 | | | | **94 (30)** | |

- 잡일 30 = `memset` 7, `RtlAppendUnicodeStringToString` 7, `LdrpLogInternal` 6, `RtlAllocateHeap` 4, `RtlAppendUnicodeToString` 4, `RtlFreeHeap` 1, `__security_check_cookie` 1.
- `LdrProtectMrdata` 5번(묶음 3 에 3번, 묶음 9 에 2번)은 `.mrdata` 쓰기 보호 껐다 켜기 쌍이라 잡일로 안 셌다.
- 이름 주의 (`_calls.tsv` 의 `alias:` 2곳):
  - `+0x7c4` (ret `+0x7c9`): 잠금 없는 목록에 한 묶음 넣기. 같은 주소 이름 2개 — Ghidra 는 내보내기 이름 `RtlInterlockedPushListSList`, PDB 이름은 `InterlockedPushListSList`.
  - `+0xb4d` (ret `+0xb52`): 시스템 정보 묻는 시스템 콜 (클래스 0xc5, 8바이트). 같은 주소 이름 — PDB `NtQuerySystemInformation`·`ZwQuerySystemInformation`, 내보내기 `RtlGetNativeSystemInformation`.
- 문자열 (원본 바이트, 결과서): `+0xdb8` 이름 `\KnownDlls`, `+0xe47` 링크 이름 `KnownDllPath`, IFEO 값 이름 `StackTraceDatabaseSizeInMb`(`+0x8f2`)·`DebugProcessHeapOnly`(`+0xcae`).

### 정적 — PEB.Ldr (모듈 목록)

| 자리 | 하는 일 | 표시 |
|---|---|---|
| `+0x158 ~ +0x1b7` | ntdll 전역 **`PebLdr`** (rva `0x1d2880`) 채우기: `Length = 0x58`, `Initialized = 1`, 목록 머리 3개(`+0x10` 적재 순서, `+0x20` 메모리 순서, `+0x30` 초기화 순서) 모두 **자기를 가리키는 빈 목록** | [정적] |
| **`+0x1c1`** | `MOV [RDI+0x18], &PebLdr` — **PEB.Ldr 가 여기서 생긴다.** PEB 는 ntdll 안의 전역을 가리킨다 | [정적] |
| `+0x119d / +0x11a2` | 첫째 `LdrpInitializeNtdllDataTableEntry` — **조건 없음**. RCX = ntdll 주소(`[RSP+0xd8]`), RDX = `&LdrpNtDllDataTableEntry`(만든 항목을 받는 전역), R9 = `<NtSystemRoot>\System32\` 문자열. 실패면 `+0x11a7 JS` 로 끝 | [정적] |
| `+0x1233 ~ +0x1256` | `LdrpNtdllHotPatchContext` 를 읽어 `[ctx+0] != 0` 일 때만 둘째 호출로 | [정적] |
| `+0x126a / +0x126f` | 둘째 `LdrpInitializeNtdllDataTableEntry` — RCX = `[ctx+0]`, R8 = 첫째로 만든 ntdll 항목. 실패면 `+0x1271 JS` 로 끝 | [정적] |
| `+0x1283 ~ +0x12d7` | ntdll 항목(`+0x20` 칸)을 **초기화 순서 목록**(`PebLdr+0x30`) 맨 앞에 끼움. 끼우기 전 연결 검사(어긋나면 `__fastfail(3)`). 둘째 항목이 있으면 그것도 | [정적] |

- 적재 순서·메모리 순서 목록(`+0x10`, `+0x20`)에 넣는 줄은 **이 구간에 없다.** `refs PebLdr` 로 보면 그 두 칸을 쓰는 건 `LdrpInsertDataTableEntry` 뿐이고, 지도상 `LdrpInitializeNtdllDataTableEntry +0xf2` 가 그것을 부른다 → 첫째 호출 안에서 넣는 것 `[추측]` (callee 본문 안 읽음).
- **exe 의 모듈 항목은 이 구간에서 안 만든다.** 뒤쪽 `+0x1359 LdrpAllocateModuleEntry` → `LdrpImageEntry` 에 저장 (`refs` 첫 쓰기). 앞쪽에서 exe 는 `PEB.ImageBaseAddress`(`[RDI+0x10]`)로 헤더만 읽는다 (`+0x35e`, `+0x627`, `+0x662`, `+0x6ef`). [정적]
- 둘째 호출을 타는지: `LdrpNtdllHotPatchContext` 는 `LdrpInitializeInternal` 의 지역 칸(0 으로 비움)을 가리키고, 채우는 건 핫패치가 켜졌을 때만 부르는 `LdrpLoadPatchedNtdll` 이다 (run17). 그래서 **핫패치가 꺼진 보통 프로세스는 한 번만, 켜진 프로세스는 "패치된 ntdll" 항목을 하나 더** `[추측]`. 값은 동적.

### 정적 — 메모리에 올리는 호출: **없다**

- `+0x0 ~ +0x12dd` 직접 호출 94곳에 `NtMapViewOfSection`, `NtCreateSection`, `NtOpenSection`, `LdrpMapDll*`, `LdrpMinimalMapModule` 이 **없다** (`_calls.tsv`). [정적]
- 비슷해 보이는 것 (모두 올리기가 아님):
  - `\KnownDlls` 쪽 `+0xdb8`·`+0xe47`·`+0xe90`·`+0xf04 NtClose` = 디렉터리·링크를 열고 **경로 문자열만 읽는다.** 섹션을 여는 호출은 없다.
  - `+0x627 LdrpCorFixupImage` 는 `PE32(0x10b)` 헤더 + `TEB.WowTebOffset <= 0` 일 때만 (`+0x602 ~ +0x611`). x64 exe(`PE32+`)면 건너뜀.
- 지도 한 단계 아래(앞쪽 callee 61개)에도 섹션 만들기·올리기 호출은 없다. 한계: 지도가 공용 도구 안까지는 안 펼쳐서 그 안은 못 봤다 (결과서).
- 핫패치가 켜진 프로세스는 이 함수 **앞**(`LdrpInitializeInternal +0xc2 LdrpLoadPatchedNtdll`, run17)에서 이미지를 하나 더 올렸을 수 있다 `[추측]`.

### 정적 — 일찍 끝나는 길

- 상태값을 돌려주고 끝나는 갈림길 **19개** — 모두 같은 에필로그 `+0xa7b ~ +0xaab` 로 간다. 대표:
  1. `+0x37f ~ +0x3b3`: 보안 프로세스(`LdrpIsSecureProcess != 0`)인데 exe 헤더에 "큰 주소 인식"(`Characteristics & 0x20`)이 없음 → 로그 `Secure processes must be large address aware` 후 `0xc00000bb`.
  2. `+0x53c`: `LdrpInitializeExecutionOptions` 실패 → 로그 후 그 상태.
  3. `+0xa55`: `LdrpInitializeProcessHeap` 실패 → 로그 후 `0xc0000017`.
  - 나머지: `+0x3e0`, `+0x62e`, `+0x9d4`, `+0x9f4`, `+0xb8a`, `+0xc34`, `+0xc41`, `+0xd10`, `+0xdcf`, `+0xe52`, `+0xe9f`, `+0xecd`, `+0xf71`, `+0x1052`, `+0x11a7`, `+0x1271`.
- 강제 종료로 가는 갈림길 **5개** (함수 끝 조각 `+0x1e34`, `+0x1e3b`):
  - `+0x784`, `+0xb22` → `RtlRaiseStatus(0x80000002)` (상수 주소 정렬 검사, 평소엔 안 탐 `[추측]`)
  - `+0x1299`, `+0x12c6` → `__fastfail(3)` (초기화 순서 목록 연결 검사)
  - `+0x1249 ~ +0x124e` → `__fastfail(0x45)` (`[ctx+8] < 0` 이고 핫패치 켜짐)
- 뒤쪽으로 바로 뛰는 갈림길은 `+0x12bc JZ → +0x12de` (둘째 항목 없으면 경계로) 하나뿐.
- 돌아간 뒤: `_LdrpInitialize +0x172 JS` → 실패 길 (run17).
- test.exe 는 정상 흐름이라 이 길은 안 탈 것 `[추측]`.

### 정적 — run17 이 넘긴 연결점

| 자리 | 하는 일 | 표시 |
|---|---|---|
| `+0x57f` → `+0x587` | `LdrpImageExpansionMitigation == 2` 일 때만 **`LdrpIsHotPatchingEnabled = 0`** (핫패치 끄기). 그 값은 `LdrpInitializeExecutionOptions +0x4d0` 이 IFEO 값 `ImageExpansionMitigation` 으로 쓴다 (읽기 실패면 안 씀 = 0). `refs`: 쓰기·읽기 한 곳씩 | [정적]. IFEO 에 값이 없으면 안 탐 `[추측]` |
| `+0x5e3` → `+0x5f2` | **`UseWOW64 = (TEB.WowTebOffset > 0)`** (`TEB+0x180c`, PDB 이름). 쓰는 곳은 여기 하나 (`refs UseWOW64`). 앞쪽에서 6곳(`+0x63b`, `+0x696`, `+0x6d5`, `+0x875`, `+0xbd0`, `+0xf0c`)이 이 값으로 갈림 | [정적]. x64 test.exe 면 0 `[추측]` |
| `+0x1233` | `LdrpNtdllHotPatchContext` 읽기 → (a) `[ctx+8] < 0` + 핫패치 켜짐 이면 `__fastfail(0x45)`, (b) `[ctx+0] != 0` 이면 둘째 ntdll 항목. `[ctx+8]` = `LdrpLoadPatchedNtdll` 반환값으로 보임 `[추측]`. 뒤쪽 `+0x18ef` 에서 또 읽음 (Q9b) | [정적] |
| `+0x153` | `LdrInitializePerThreadRng(TEB)` — 조건 없이. `_calls.tsv` 상 부르는 곳은 이곳과 `_LdrpInitialize +0x83`(나중 스레드) 둘뿐. run17 결과 맞음 | [정적] |

### 정적 — 디버거 때문에 갈리는 길 (CLAUDE.md "따로 적기")

- **`PEB.BeingDebugged`(PEB+0x2) 는 앞쪽에서 안 읽는다.** 뒤쪽 `+0x17d2`, `+0x1b35` (Q9b). [정적]
- **`KUSER_SHARED_DATA` 도 앞쪽에서 안 읽는다** (뒤쪽 `+0x188c`, `+0x1cb0` 의 `0x7ffe0330`). 그러니 `KdDebuggerEnabled` 를 앞쪽에서 보는 줄은 없다. [정적]
- **`NtGlobalFlag`(PEB+0xbc) 는 읽는다** [정적] (비트 이름은 공개 문서 기준 `[추측]`):
  - `+0x58e` `& 0x2` (로더 스냅 보이기) → `LdrpDebugFlags |= 1`. LoadConfig 반영(`+0x70d`, `+0x71d`) 뒤 `+0x74b` 에서 한 번 더.
  - `+0x8a3`, `+0x989` `& 0x1000` (사용자 스택 추적 DB) → `+0x979 RtlControlStackTraceDataBase`.
  - `+0xaf4` `& 0x800000` (예외 로그) → `+0xb00 RtlInitializeExceptionLog`.
- 디버그 페이지 힙(`+0xcae DebugProcessHeapOnly`)·`BreakOnInitializeProcessFailure`(`LdrpInitializeExecutionOptions +0x462`)는 **IFEO 값**에 따른다. 디버거 부착을 직접 보지 않는다. [정적]
- PEB+0xbc 값은 커널이 PEB 를 만들 때 넣는 값이라 (2.1 `MmCreatePeb`) 이 구간 코드로는 정할 수 없다. 커널 디버거만 붙어서는 안 바뀔 것 `[추측]` → 동적.

### 호출 관계

```
_LdrpInitialize +0x16c
 └─ LdrpInitializeProcess (0x8c404)            ← RCX → [RSP+0xe0] (앞쪽 안 씀), RDX → RSI·[RSP+0xd8] = ntdll 주소로 씀
      │  앞쪽 +0x0 ~ +0x12dd  [정적] run18
      ├─ +0x158  LdrInitializePerThreadRng          ① 입구
      │  +0x1c1  PEB.Ldr = &PebLdr (빈 목록 3개)    ② (직접 코드)
      ├─ +0x1cd  LdrpInitializeNlsInfo
      ├─ +0x41a  RtlInsertInvertedFunctionTable     ③ ntdll 역함수 표 등록
      ├─ +0x536  LdrpInitializeExecutionOptions     ④ IFEO
      │  +0x587  (IFEO 값 2 면) 핫패치 끄기 / +0x5f2 UseWOW64 = WowTebOffset > 0
      ├─ +0x6c5  RtlNormalizeProcessParams          ⑤
      ├─ +0x9d2  RtlInitializeCriticalSectionEx     ⑥ FastPebLock
      ├─ +0xa50  LdrpInitializeProcessHeap          ⑦ PEB.ProcessHeap
      ├─ +0xb88  TpInitializePackage                ⑧ (+0xc32 RtlpInitEnvironmentBlock, +0xc3f RtlpInitParameterBlock)
      ├─ +0xdbd  NtOpenDirectoryObject (\KnownDlls) ⑨ (+0xe4c NtOpenSymbolicLinkObject, +0xe95 NtQuerySymbolicLinkObject, +0xfca LdrpInitializePolicy)
      ├─ +0x11a2 LdrpInitializeNtdllDataTableEntry  ⑩ ntdll 항목 (조건 없음)
      ├─ +0x126f LdrpInitializeNtdllDataTableEntry     (핫패치 컨텍스트 [ctx] != 0 일 때만)
      │  +0x1283 ~ +0x12d7  ntdll 항목 → PebLdr+0x30 초기화 순서 목록
      │  뒤쪽 +0x12de ~ 끝  [아직 안 봄, Q9b] — 관찰된 +0x19a2 / +0x1bbb / +0x1c99 는 여기
      ├─ +0x19a2 LdrpInitializeKernel32Functions    [관찰] run7
      ├─ +0x1bbb LdrpInitializeGraphRecurse          [관찰] run7·run8 (call +0x1bb6)
      └─ +0x1c99 LdrpCallTlsInitializers             [관찰] run7·run8
```

- ①~⑩ = 위 묶음 번호. 트리는 묶음 대표만. 호출 94곳 전부는 `_calls.tsv` 의 `LdrpInitializeProcess` 줄.

## 확인된 것

- **앞쪽 구간 순서**: 입구 → PEB.Ldr·NLS → ntdll 역함수 표 → IFEO → 전역 켜고 끄기 → TLS 비트맵·`FastPebLock` → 프로세스 힙 → 스레드 풀·ETW·환경 블록 → 시스템 경로·`\KnownDlls` → ntdll 모듈 항목. [정적] 결과서 표, `.asm` 각 대표 줄.
- **`RDX` 는 본문에서 ntdll 이미지 시작 주소로 쓰인다** (`+0x3f9` 헤더 찾기, `+0x68c` `LdrpSystemDllBase`, `+0x1195` ntdll 항목). **`RCX` 는 앞쪽에서 안 쓴다** (처음 `+0x17f4`). [정적] run17 이 Q9 로 넘긴 질문의 앞쪽 몫.
- **PEB.Ldr 는 `LdrpInitializeProcess +0x1c1` 에서 생긴다** — ntdll 전역 `PebLdr` 를 빈 목록 3개로 채워 PEB+0x18 에 건다. 커널(`MmCreatePeb`)이 아니라 ntdll 이 채운다. [정적]
- **앞쪽 끝 무렵 모듈 목록에 들어 있는 건 ntdll 뿐** (핫패치 때는 패치 ntdll 하나 더 `[추측]`). exe 항목은 뒤쪽 `+0x1359` 에서 만든다. [정적]
- **앞쪽에서는 DLL·이미지를 하나도 메모리에 올리지 않는다.** [정적] flow.md 2.5 `[추측]` "시작할 때 exe·ntdll 만 올라와 있다" 중 "이 함수 앞쪽에선 더 안 올린다" 까지 뒷받침.
- **프로세스 힙은 `+0xa4b` `LdrpInitializeProcessHeap` 에서 생긴다** (PEB+0x30). 이 함수가 직접 부르는 `RtlAllocateHeap` 4곳은 모두 그 뒤(묶음 9)다. [정적]
- **`UseWOW64` 는 `+0x5f2` 한 곳에서 `TEB.WowTebOffset > 0` 으로 정한다.** [정적] run17 이 본 `_LdrpInitialize +0x9a` 간접 호출의 갈림 값이 여기서 온다.
- **앞쪽은 `BeingDebugged`·`KUSER_SHARED_DATA` 를 안 읽는다.** 디버거 부착 여부로 직접 갈리는 길은 앞쪽에 없다. `NtGlobalFlag`·IFEO 로만 갈린다. [정적]

## 정정

- **요청서·plan 의 호출 개수**: 앞쪽 95곳 → **94곳**, 뒤쪽 74곳 → **75곳** (경계 `+0x12de` 를 뒤쪽에 셈). 요청서는 그대로 두고 이 문서·flow.md 에 실제 수를 쓴다. 이전 run 문서는 틀린 게 없어 안 고친다.
- **flow.md 의 `LdrpInitializeProcess +0x1bb6` 라벨** (그림 2 `(2) +0x1bb6`, 2.4·2.7 트리): 읽는 법대로면 라벨은 돌아올 자리(ret)인데 `+0x1bb6` 은 **call 자리**다. ret 은 **`+0x1bbb`** — run7 콜스택 `LdrpInitializeProcess+0x1bbb` `[관찰]`, `_calls.tsv` `+0x1bb6 → ret +0x1bbb` `[정적]`. run8 이 멈춤점을 `+0x1bb6`(call 자리, `ub +0x1bbb` 로 확인)에 건 것이 라벨로 옮겨진 것으로 보인다 `[추측]`. flow.md 의 라벨·트리를 `+0x1bbb` 로 고치고, 2.6 의 "`+0x1bb6` 에서 `lm`" 은 멈춤 자리라 그대로 두되 "(call 자리)" 를 붙였다. 01_sprint 의 run7·run8 문서는 각각 맞게 썼으므로 안 고친다.

## 아직 확인 필요

- 정적 → **Q9b** (뒤쪽 `+0x12de ~ 끝`, 75곳): `+0x12de LdrpInitParallelLoadingSupport`, `+0x1302 LdrpDrainWorkQueue`, `+0x1313 LdrpInitializeDllPath`, `+0x1359 LdrpAllocateModuleEntry`(exe 항목 → `LdrpImageEntry`), `RCX` 처음 쓰는 곳 `+0x17f4`, `BeingDebugged` `+0x17d2`·`+0x1b35`, `KUSER_SHARED_DATA+0x330` `+0x188c`·`+0x1cb0`, `LdrpNtdllHotPatchContext` `+0x18ef`, `NtGlobalFlag & 0x100` `+0x16b7`.
- 정적 → **Q45** (새 항목): **`LdrpInitializeNtdllDataTableEntry`** (633바이트) 본문 — 적재·메모리 순서 목록에 넣는 곳(`LdrpInsertDataTableEntry`), 색인, 둘째 호출(패치 ntdll)의 정체.
- 정적 → **Q10** (DLL 로드)로 넘길 연결점: 앞쪽이 준비한 전역 `LdrpKnownDllDirectoryHandle`, `LdrpKnownDllPath`, `LdrpSystem32`, `RtlpSystem32Dirs`, `LdrpHashTable`, `LdrpHeap`, `LdrpSystemDllBase`, `LdrpAppHeaders` 를 DLL 로드 코드가 쓰는지 `refs` 로.
- 정적 → **Q16** (TLS): 앞쪽 `+0x774 ~ +0x8a3` 이 PEB TLS 비트맵 칸(PEB+0x78, +0x80, +0x238, +0x240)을 걸어 둔다 — `LdrpInitializeTls` 가 쓰는 바탕.
- 동적 → **Q44** (`ntdll+rva` = `0x8c404 + 오프셋`, test.exe 일 때만):
  1. `ntdll+0x8c404` 입구 `r rcx rdx` — `rdx` = `lm m ntdll` 시작 주소인지.
  2. `ntdll+0x8c9f6` (`+0x5f2`) `r eax` (`UseWOW64` 0 인지), `ntdll+0x8c98b` (`+0x587`) 에 서는지.
  3. `ntdll+0x8c992` (`+0x58e`) 에서 `dd @rdi+0xbc L1` (`NtGlobalFlag`) — 커널 디버거만 붙었을 때 값.
  4. `ntdll+0x8ce4f` / ret `ntdll+0x8ce54` (`LdrpInitializeProcessHeap`) `r eax`, `dq @rdi+0x30 L1`.
  5. `ntdll+0x8d1bc` (`+0xdb8`) 와 ret `ntdll+0x8d1c1` 의 `eax`, `KnownDllPath`·`LdrpSystem32` 문자열.
  6. `ntdll+0x8d5a1` / ret `ntdll+0x8d5a6`, `ntdll+0x8d637` (`+0x1233`) 에서 `[ctx]`·`[ctx+8]` — 둘째 `+0x126a` 를 타나.
  7. `ntdll+0x8d6e2` (`+0x12de`, 경계) 에서 `PebLdr`(rva `0x1d2880`) 목록 3개 — ntdll 만 들어 있나.
  8. `ntdll+0x8ce7f` (`+0xa7b`, 에필로그) 에 서면 실패 길 — 안 서야 정상.

## 디버거 메모 / 도구 메모

- 도구 문제 없음 (결과서). `alias:` 표시 2곳은 별칭 규칙대로다 (위 "이름 주의").

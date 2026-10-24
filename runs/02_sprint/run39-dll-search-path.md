# 2026-10-24 run39 — DLL 검색 경로: 목록은 첫 `LdrpSearchPath` 때 지어지고 (`+0x36d LdrpComputeLazyDllPath` → … → `RtlpComputePath`), 첫 칸은 exe 폴더 (`RtlpGetDirPath`, exe 항목 `FullDllName`) (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 정적: ntdll 26100.9457, pdb GUID `C3093720177DA6851519DCFFE8DF53FE1` (기준 절과 같음). 이미 analyze 된 프로젝트라 다시 analyze 하지 않았다.
- 버전 대조 (결과서): `_calls.tsv` 의 `LdrpInitializeProcess +0x1bb6 / +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpLoadDll +0xfb / +0x100 → LdrpLoadDllInternal` 이 알려진 사실과 **일치.** 요청서가 지도에서 옮겨 적은 call 자리 (`LdrpInitializeProcess +0x1313`, `LdrpSearchPath +0x36d/+0x225/+0x244/+0x4f7`, `LdrpComputeLazyDllPath +0x62`, `LdrpGetDllPath +0xa8`) 도 전부 `_calls.tsv` 와 같다.
- 이름 (결과서, `_index.tsv`): 요청서 다섯 함수 모두 PDB 이름 그대로.
  - `LdrpInitializeDllPath` `0x899a0` / 167B, `LdrpSearchPath` `0x8eb0` / 1463B, `LdrpComputeLazyDllPath` `0xb9870` / 295B, `LdrpGetDllPath` `0xb9f30` / 635B, `RtlpGetCachedPath` `0xba2f0` / 364B.
  - 요청서 밖으로 더 본 것: `RtlpComputeDllPath` `0xba560` / 222B, `RtlpComputePath` `0xba7a0` / 1885B, `RtlpGetDirPath` `0xb9638` / 149B, `LdrpResolveDllName` `0x10120` / 880B (앞부분과 호출만), `LdrpGetNtPathFromDosPath` `0xefe0` / 276B, `LdrpGetFullPath` `0xe4d20` / 232B.
  - **별칭 하나**: `LdrpSearchPath +0x29a` 의 callee 는 같은 주소 이름이 여럿인 "버퍼 해제 함수" 다 (`_calls.tsv` 에 `alias:RtlDeleteBoundaryDescriptor+9`). 이 문서는 "버퍼 해제" 로만 쓴다.
- 경로 구조체(아래 `P`)는 PDB 에 타입이 없다 (결과서 `llvm-pdbutil` 확인). 오프셋 + 쓰는 줄로만 적는다.
- 순서 표(`ntdll+0x17e340`)·문자열 상수(`PATH`, `.`, `SafeDllSearchMode`, `\system;`, `forwarders\alt;`)는 결과서가 원본 바이트로 읽었다. 순서 표 4줄은 이 문서를 쓰며 `objdump -s` 로 한 번 더 맞춰 봤다 (같음).

## 목적

- Q53 (sast/21 "다음 정적 후보 — 중간", flow.md 2.6 "exe 폴더를 넣는 줄은 이 세 함수에 없다"):
  1. `LdrpInitializeProcess +0x1313 LdrpInitializeDllPath(0, 0, &P)` 는 폴더 목록을 **그 자리에서** 만드나, "나중에" 표시만 하나.
  2. 실제 목록 문자열은 어디서 짓나 (`LdrpComputeLazyDllPath` → `LdrpGetDllPath` → `RtlpGetCachedPath` 사슬이 맞나).
  3. exe 폴더는 **어떤 값**에서 오나, 어디서 잘라 폴더로 만드나.
  4. 기본 목록의 **순서**.
  5. `LdrpSearchPath` 가 목록을 어떻게 도나, 처음 찾은 곳에서 멈추나, 못 찾으면 무엇을 돌려주나.
  6. dll1·dll2 는 목록의 몇 번째 칸에서 찾히나, dll2 도 같은 경로 구조체를 쓰나.

## 방법

- 정적: `_workspace/sast/34_dll-search-path.md` — ntdll
  - 1차 `^(LdrpInitializeDllPath|LdrpSearchPath|LdrpComputeLazyDllPath|LdrpGetDllPath|RtlpGetCachedPath|RtlpLookupSafeCurDirList)$` depth 1 (35개)
  - 요청서 밖으로 더: 목록을 실제로 짓는 곳이 `RtlpGetCachedPath` 안이 아니라 **함수 포인터로 넘기는 콜백** (`RtlpComputeDllPath`) 이라 요청서 depth 로는 안 닿았다.
    - 2차 `^(RtlpComputeDllPath|RtlpComputeDllPathWithOptions|RtlGetExePath|RtlGetSearchPath)$` depth 1
    - 3차 `^(RtlpGetDirPath|RtlpNeedCurrentDirectoryForExePath)$` depth 0 (exe 폴더 자르기)
    - 4차 `^(LdrpGetNtPathFromDosPath|LdrpGetFullPath)$` depth 0 (파일 있는지 보는 곳)
  - `refs` 3개: `RtlpSystemDirs`, `RtlpSystem32Dirs`, `0x1d2380` (SafeDllSearchMode 값을 담아 두는 칸).
- 결과서: `_workspace/sast/34_dll-search-path_result.md` (**분석성공**)
- 점프 목적지는 결과서가 스크립트로 전부 "주소 − 함수 시작" 으로 바꿔 적었다 (run38 때 생긴 문제 재발 방지). 이 문서를 쓰며 `LdrpInitializeDllPath`·`LdrpSearchPath`·`LdrpComputeLazyDllPath`·`RtlpComputeDllPath`·`RtlpGetDirPath`·`LdrpResolveDllName` 의 주요 줄을 `.asm` 으로 다시 맞춰 봤다 (한 곳 빼고 같음 — 아래 "결과서 읽기 메모").

## 결과

비유 (주소록):

- `P` = dll 을 찾을 때 펼쳐 보는 **주소록 한 권.** 프로세스 시작 때는 **빈 표지만** 만들어 둔다 (`LdrpInitializeDllPath`).
- 처음 누가 주소록을 펼치면 그때 **안쪽 페이지를 채운다** (`LdrpComputeLazyDllPath` → … → `RtlpComputePath`). 첫 줄은 "exe 가 사는 폴더" 다.
- `LdrpSearchPath` 는 주소록을 **위에서부터 한 줄씩** 보며 "그 폴더에 `dll1.dll` 있어요?" 하고 묻는다 (`NtQueryAttributesFile`). **처음 "있어요" 에서 멈춘다.**

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x1313` | `+0x1318` | `ntdll!LdrpInitializeDllPath` (`0, 0, &P`) | [정적] (run19 이름, 이번 본문) |
| `ntdll!LdrpMapDllSearchPath` | `+0x330` | `+0x335` | `ntdll!LdrpInitializeDllPath` (부모 `DependentLoadFlags` 가 겹칠 때만) | [정적] (run26) |
| `ntdll!LdrpMapDllSearchPath` | `+0x12c` | `+0x131` | `ntdll!LdrpSearchPath` | [정적] (run26) |
| `ntdll!LdrpSearchPath` | `+0x172` | `+0x177` | `ntdll!LdrpAllocateUnicodeString` (후보 버퍼) | [정적] |
| `ntdll!LdrpSearchPath` | `+0x225` | `+0x22a` | `ntdll!RtlAppendUnicodeStringToString` ("폴더\" + 이름) | [정적] |
| `ntdll!LdrpSearchPath` | `+0x244` | **`+0x249`** | **`ntdll!LdrpResolveDllName`** (후보마다, 파일 있나) | [정적] |
| `ntdll!LdrpSearchPath` | `+0x36d` | **`+0x372`** | **`ntdll!LdrpComputeLazyDllPath`** (`P+0` 이 0 일 때만) | [정적] |
| `ntdll!LdrpSearchPath` | `+0x4f7` | `+0x4fc` | `ntdll!RtlpLookupSafeCurDirList` (현재 폴더 칸 + 플래그 `0x2000` 일 때만) | [정적] |
| `ntdll!LdrpComputeLazyDllPath` | `+0x27` / `+0xc6` | `+0x2c` / `+0xcb` | `RtlAcquireSRWLockExclusive` / `RtlReleaseSRWLockExclusive` (`LdrpPathLock`) | [정적] |
| `ntdll!LdrpComputeLazyDllPath` | `+0x62` | **`+0x67`** | **`ntdll!LdrpGetDllPath`** | [정적] |
| `ntdll!LdrpGetDllPath` | `+0xa8` | **`+0xad`** | **`ntdll!RtlpGetCachedPath`** (`&RtlpDllSearchPath, RtlpComputeDllPath, 0, 0`) | [정적] |
| `ntdll!LdrpGetDllPath` | `+0x1ad` / `+0x1d3` | `+0x1b2` / `+0x1d8` | `RtlDetermineDosPathNameType_U` (플래그 `0x100`·`8` 일 때만) | [정적] |
| `ntdll!RtlpGetCachedPath` | `+0xbe` | **`+0xc3`** | **`ntdll!RtlpComputeDllPath`** (캐시에 없을 때, 콜백 주소 비교 뒤 직접 호출) | [정적] |
| `ntdll!RtlpGetCachedPath` | `+0xe9` | `+0xee` | `ntdll!RtlpComputeDllPathWithOptions` (플래그 갈래 콜백) | [정적] |
| `ntdll!RtlpGetCachedPath` | `+0x152` | `+0x157` | 간접 호출 (CFG, 둘 다 아닌 콜백) | [정적] |
| `ntdll!RtlpComputeDllPath` | `+0x4b` | **`+0x50`** | **`ntdll!RtlpLookupCurDirSetting`** (`"SafeDllSearchMode"`, 담아 둔 값이 0 일 때만) | [정적] |
| `ntdll!RtlpComputeDllPath` | `+0x59` / `+0x89` | `+0x5e` / `+0x8e` | `RtlAcquireSRWLockShared` / `RtlReleaseSRWLockShared` (`LdrpDllDirectoryLock`) | [정적] |
| `ntdll!RtlpComputeDllPath` | `+0x7a` | **`+0x7f`** | **`ntdll!RtlpComputePath`** (순서 표 한 줄, 5칸) | [정적] |
| `ntdll!RtlpLookupCurDirSetting` | `+0x5f` / `+0xa1` | `+0x64` / `+0xa6` | `NtOpenKey` / `NtQueryValueKey` (`…\Session Manager` 의 `SafeDllSearchMode`) | [정적] |
| `ntdll!RtlpComputePath` | `+0xfb` | `+0x100` | `RtlAllocateHeap` (목록 블록) | [정적] |
| `ntdll!RtlpComputePath` | `+0x2bc` / `+0x324` | `+0x2c1` / `+0x329` | `RtlQueryEnvironmentVariable` (`PATH`, 길이 → 값) | [정적] |
| `ntdll!RtlpComputePath` | `+0x555` | **`+0x55a`** | **`ntdll!RtlpGetDirPath`** (인자 0 = exe 폴더) | [정적] |
| `ntdll!RtlpComputePath` | `+0x3f6` | `+0x3fb` | `ntdll!RtlpGetDirPath` (부모 경로를 줄 때 — 우리 길 아님) | [정적] |
| `ntdll!RtlpGetDirPath` | `+0x3b` / `+0x50` | `+0x40` / `+0x55` | `wcschr` / `wcsrchr` (첫 `\` / 마지막 `\`) | [정적] |
| `ntdll!LdrpResolveDllName` | `+0x298` | `+0x29d` | `ntdll!LdrpGetFullPath` (→ `+0x4e RtlGetFullPathName_Ustr`) | [정적] |
| `ntdll!LdrpResolveDllName` | `+0x13b` | `+0x140` | `ntdll!LdrpGetNtPathFromDosPath` | [정적] |
| `ntdll!LdrpGetNtPathFromDosPath` | `+0x6c` | `+0x71` | `RtlpDosPathNameToRelativeNtPathName` | [정적] |
| `ntdll!LdrpGetNtPathFromDosPath` | `+0xee` | **`+0xf3`** | **`ntdll!NtQueryAttributesFile`** (파일 있나 — 시스템 콜) | [정적] |

- 그 밖 직접 호출 (로그 `LdrpLogInternal`·`LdrpLogDllStateEx2`, `LdrpSaveLocationsSearched` (`SearchPath +0x3a0`), `RtlCompareUnicodeStrings` ×2, `RtlCopyUnicodeString`, `RtlFreeHeap` 등) 은 결과서.
- `LdrpResolveDllName` 은 코드 순서로 `+0x92 TEST …,0x200` → `+0x9d JZ +0x292` → `+0x298 LdrpGetFullPath` → `+0x2a1 JNS +0x22d` → … → `+0x13b LdrpGetNtPathFromDosPath` 다. 그래서 오프셋은 뒤가 작아도 **`GetFullPath` 가 먼저**다 (`.asm` 으로 확인).

### 정적 — 경로 구조체 `P` (0x80바이트, `ctx+0x10` 이 가리킴)

exe 문맥의 `P` 는 `LdrpInitializeProcess` 스택 `[RSP+0x380]` 이다 (`+0x1318 LEA RAX,[RSP+0x380]` → `+0x1320 MOV [RSP+0x2c0],RAX` = exe 문맥 `[RSP+0x2b0]` 의 `+0x10` 칸).

| 칸 | 쓰는 곳 | 뜻 |
|---|---|---|
| `+0x00` qword | `LdrpComputeLazyDllPath +0x91` (밖에서 문자열을 줄 때는 `LdrpInitializeDllPath +0x5b`) | **폴더 목록 문자열** (`"폴더;폴더;…;"` UTF-16, 끝에 `;`). **0 이면 "아직 안 지음"**. `LdrpSearchPath +0xe4`·`LdrpComputeLazyDllPath +0x2c` 가 본다 |
| `+0x08` qword | `LdrpComputeLazyDllPath +0x84` | 목록 안 `.` (현재 폴더) 칸의 시작. **플래그 `0x2000` 일 때만**, 아니면 0 (`LdrpGetDllPath +0x197`) |
| `+0x10` qword | `LdrpComputeLazyDllPath +0x94` | 패키지 앱 폴더 목록. 보통 프로세스는 0 `[추측]`. 있으면 `LdrpSearchPath +0x102` 가 이쪽부터 |
| `+0x18` dword | `LdrpInitializeDllPath +0x46` | 플래그 = `인자2 & ~1` (부모 `DependentLoadFlags` 에서 온 값). **`(0, 0)` 이면 0** |
| `+0x20` qword | `LdrpInitializeDllPath +0x42` | 부모 모듈 `FullDllName.Buffer` (플래그 갈래). **`(0, 0)` 이면 0** |
| `+0x28 ~ +0x77` | `LdrpGetDllPath +0xca ~ +0xec` (복사) | 목록 블록 머리 사본 (칸 종류 코드 5개, 칸마다 시작, 칸 수). `LdrpSearchPath` 는 안 읽는다. 읽는 곳은 못 찾음 |
| `+0x78` dword | `LdrpGetDllPath +0x118` | 목록 문자열 바이트 수 |
| `+0x7c` byte | `LdrpComputeLazyDllPath +0x8d` | 지음 끝 표시 (1) |

- `P+0x00` 은 따로 복사한 문자열이 아니라 **캐시 블록 `+0x80`** 을 가리킨다 (`LdrpGetDllPath +0xf0 LEA RCX,[RAX+0x80]` → `+0xf7`). 캐시 블록은 `RtlpComputePath` 가 프로세스 힙에 할당하고 전역 `RtlpDllSearchPath` (`ntdll+0x1d3050`) 가 가리킨다.

### 정적 — 기본 목록 순서 표 (`RtlpComputeDllPath +0x65 LEA RAX,[ntdll+0x17e340]`, 줄 = dword 5개)

| 줄 | 주소 | 칸 코드 | 뜻 | 언제 |
|---|---|---|---|---|
| 0 | `ntdll+0x17e340` | `5 4 2 3 8` | exe 폴더, **현재 폴더**, 시스템, PATH, 패키지 | SafeDllSearchMode = 0 |
| 1 | `ntdll+0x17e354` | `5 2 4 3 8` | exe 폴더, 시스템, **현재 폴더**, PATH, 패키지 | SafeDllSearchMode = 1 (**기본**) |
| 2 | `ntdll+0x17e368` | `1 2 3 8 9` | — | `RtlpComputeDllPath` 로는 못 닿음 |
| 3 | `ntdll+0x17e37c` | `5 0 2 3 8` | exe 폴더, **SetDllDirectory 폴더**, 시스템, PATH, 패키지 (현재 폴더 빠짐) | `LdrpDllDirectory` 버퍼(`ntdll+0x1d2378`) ≠ 0 |

| 칸 코드 | 넣는 것 (`RtlpComputePath`, 읽는 전역) |
|---|---|
| 0 | `LdrpDllDirectory` (SetDllDirectory 폴더) |
| 1 | exe 폴더 (`RtlpGetDirPath(0)`) |
| 2 | **`RtlpSystemDirs`** 한 덩어리 = `<System32>;<Windows>\system;<Windows>;` (이미 `;` 로 끝남) |
| 3 | 환경 변수 **`PATH`** (`FastPebLock` 안, 없으면 빈 칸) |
| 4 | 글자 `.` (현재 폴더, 상대) |
| 5 | 응용 프로그램 폴더: 부모 경로 인자가 있으면 그 폴더, **없으면 exe 폴더** |
| 6 | `LdrpUserDllDirectories` (AddDllDirectory 목록 `[추측: 이름]`) |
| 7 | `RtlpSystem32Dirs` (System32 하나 — 시스템 폴더 대체 모드에서만) |
| 8 | `LdrpAppPackagesPath` (패키지 앱, 길이 0 이면 건너뜀) |

### 호출 관계

```
LdrpInitializeProcess
 ├─ +0x1065 ~ +0x1161  시스템 폴더 문자열 짓기: +0x109e RtlpSystem32Dirs · LdrpSystem32, +0x1161 RtlpSystemDirs
 ├─ +0x1318  LdrpInitializeDllPath(0, 0, &P)     P 0x80 바이트를 0 으로. +0x18 = 0, +0x20 = 0. 호출·할당 없음
 │           → +0x1320  exe 문맥 +0x10 = &P
 ├─ +0x135e  LdrpAllocateModuleEntry → +0x1374 LdrpImageEntry, +0x13b9 항목 FullDllName = ProcessParameters.ImagePathName 사본
 └─ … (10) +0x1ab1 LdrpDrainWorkQueue → LdrpProcessWork +0x6a LdrpMapDllSearchPath(dll1 문맥)        (run24·run26)
          (dll1·dll2 문맥 +0x10 = 부모 문맥 +0x10 = &P  — LdrpLoadDependentModuleInternal +0x959 → +0x10f8)
          └─ +0x131 LdrpSearchPath(ctx, P, …)
               ├─ (1) [P+0 == 0 일 때만]  +0x372 LdrpComputeLazyDllPath(P)          LdrpPathLock 독점, 안에서 한 번 더 확인
               │      └─ +0x67 LdrpGetDllPath(P+0x20 = 0, P+0x18 = 0, …)
               │           └─ +0xad RtlpGetCachedPath(&RtlpDllSearchPath, RtlpComputeDllPath, 0, 0)
               │                ├─ 캐시 있고 판 같음 → 참조 수 +1, 그대로
               │                └─ 없으면 +0xc3 RtlpComputeDllPath(0, 0)
               │                     ├─ (1) [담아 둔 값 0 일 때만] +0x50 RtlpLookupCurDirSetting("SafeDllSearchMode", 기본 1)
               │                     │        └─ +0x64 NtOpenKey · +0xa6 NtQueryValueKey   (보안 프로세스면 1, 값이 이상하면 1)
               │                     └─ (2) +0x7f RtlpComputePath(줄 1 = 5 2 4 3 8, 5칸, 0, 0)
               │                              ├─ 1차: 칸마다 길이 (0xfffe 넘으면 0xc0000106) → +0x100 RtlAllocateHeap
               │                              ├─ 칸 5: +0x55a RtlpGetDirPath(0)   ★ exe 폴더 = LdrpImageEntry FullDllName, 마지막 \ 앞까지
               │                              ├─ 칸 3: +0x2c1 / +0x329 RtlQueryEnvironmentVariable("PATH")
               │                              └─ 2차: 칸마다 복사 + ';'
               └─ (2) 칸마다 (';' 로 끊음, 빈 칸 건너뜀):  "폴더" + '\' + 이름 (+0x22a)
                      +0x249 LdrpResolveDllName(후보, …)
                        ├─ (1) +0x29d LdrpGetFullPath → RtlGetFullPathName_Ustr        전체 DOS 경로
                        └─ (2) +0x140 LdrpGetNtPathFromDosPath
                                 └─ +0xf3 NtQueryAttributesFile                       ★ 파일 있나
                        없음 계열 상태 → 0xc0000135 로 바꿈 (+0x28b)
                      0 이상 → 멈춤 (+0x256 JNS +0x282)
                      0xc0000135 · 0xc0000022 · 0xc0000043 · 0xc000006d → 다음 칸
                      그 밖 음수 → 그 자리에서 그 오류로 끝
                    목록 끝 → +0x27d 0xc0000135
```

## 확인된 것

### 1. `LdrpInitializeDllPath(0, 0, &P)` 는 "나중에" 표시만 한다 `[정적]`

- `+0x13 ~ +0x35` `MOVUPS` 8번으로 `P` 0x80 바이트를 0 으로 민다.
- `+0x3a TEST DL,1` 로 인자 2 비트 0 을 본다. 우리 경우 `(0, 0)`: `+0x3d JZ +0x56` → `+0x56 TEST RDX,RDX` → `+0x59 JZ +0x3f` → `P+0x20 = 0` (`+0x42`), `P+0x18 = 0` (`+0x46`) → `+0x54 RET`.
  - **호출 없음, 할당 없음.** 목록 칸 `P+0` 은 0 그대로 = "아직 안 지음".
- 나머지 갈래 (`+0x330` 의 caller 가 쓰는 쪽):
  - 비트 0 이 켜짐 → 같은 `+0x3f`: 부모 경로·플래그만 적어 둔다 (`LdrpMapDllSearchPath +0x330`, sast/21).
  - 인자 2 가 0 이 아니고 비트 0 이 꺼짐 → `P+0 = 인자2` (`+0x5b`), 로그 "DLL search path passed in externally" (`+0x88`), `+0xa3` 꼬리 `JMP LdrpLogDllStateEx2`. 밖에서 지은 문자열을 받는 갈래 (shim·verifier 쪽 caller 일 것 `[추측]`, 범위 밖).
- **왜 나중이어야 하나** `[정적]`: 목록 첫 칸(exe 폴더)은 `LdrpImageEntry` 에서 읽는데, exe 항목은 `+0x1313` **뒤** `+0x1359 LdrpAllocateModuleEntry` → `+0x1374 MOV [LdrpImageEntry],RAX` 에서야 생긴다.

### 2. 목록을 짓는 사슬 — 요청서 사슬은 맞고, 짓는 곳은 한 단계 더 아래 `[정적]`

- **`LdrpSearchPath +0xe4 CMP [R12],RBP / +0xe8 JZ +0x36a`** → `+0x36d LdrpComputeLazyDllPath(P)`. `P+0` 이 0 일 때만. 실패(음수)면 `+0x37c JMP +0x2e9` 로 오류 반환, 성공이면 `+0x376 JNS` 로 돌아가 이어 한다.
- **`LdrpComputeLazyDllPath`**: `+0x27` `LdrpPathLock` 독점 → `+0x2c CMP [RBX],RDI / +0x2f JNZ` 로 **락 안에서 한 번 더** `P+0` 확인 (이미 채워졌으면 건너뜀) → `+0x62 LdrpGetDllPath(P+0x20 값, P+0x18 값, 출력들)` → 성공이면 `+0x84 P+8`, `+0x8d P+0x7c = 1`, `+0x91 P+0`, `+0x94 P+0x10` → 로그 "DLL search path computed: %ws" (`+0xba`) → `+0xc6` 락 풀기.
- **`LdrpGetDllPath`**: 플래그 `P+0x18` 로 계산기를 고른다. 0 (우리 경우) 이면 `+0x94 ~ +0xa8` `RtlpGetCachedPath(&RtlpDllSearchPath, RtlpComputeDllPath, 0, 0)`. 플래그가 있으면 `+0x141 ~ +0x192` 에서 `RtlpDllSearchPathWithOptions`/`RtlpComputeDllPathWithOptions`. 이름에 `\` 가 있는 경로를 보는 줄 (`+0x1ad`·`+0x1d3`) 은 플래그 `0x100`·`8` 일 때만 (`+0x73`·`+0x7b JNZ`).
- **`RtlpGetCachedPath`** = 캐시 창구. `R8`·`R9` 가 둘 다 0 일 때만 캐시를 쓴다 (`+0x27`·`+0x2c JNZ +0xa7`).
  - 캐시 블록이 있고 (`+0x43`), `LdrpAppPackagesPathVersion` 이 같고 (`+0x54`), 환경 블록 판(`ProcessParameters +0x3f8`)이 같으면 (`+0x78`) 참조 수만 올려 돌려준다.
  - 아니면 콜백 주소를 비교해 (`+0xb9 CMP RBP,RAX`) `+0xbe RtlpComputeDllPath` 를 **직접** 부른다.
- **`RtlpComputeDllPath`** (콜백): `+0x1b` SetDllDirectory 버퍼가 있으면 `+0x2e JNZ +0xaf` → 줄 3. 없으면 `+0x30` 담아 둔 SafeDllSearchMode 값 (`ntdll+0x1d2380`) → 0 이면 `+0x4b RtlpLookupCurDirSetting` → `+0x5e ~ +0x6c` 줄 고르기 → `+0x7a RtlpComputePath(줄, 5, R8, 0)` → 블록 `+0x58` 에 환경 판, `+0x74 = 0` (`+0x93`·`+0x97`).
  - `RtlpLookupCurDirSetting`: 보안 프로세스(`LdrpIsSecureProcess`)면 1. 아니면 레지스트리 `\Registry\MACHINE\System\CurrentControlSet\Control\Session Manager` 의 `SafeDllSearchMode` 를 읽어 (`+0x5f NtOpenKey`, `+0xa1 NtQueryValueKey`) 길이가 맞고 값이 0 또는 1 이면 그 값, **아니면 기본 1** (`+0xe3 ~ +0xef`).
  - 줄 2 는 `EBX` 가 0·1 뿐이라 이 콜백으로는 못 닿는다 (그 칸을 쓰는 코드는 이 함수 하나, `refs 0x1d2380`).
- **`RtlpComputePath`** = 실제로 문자열을 짓는 곳. 1차 `+0x7c ~ +0xd9` 칸마다 바이트 수 → `+0xe3 JA` 0xfffe 넘으면 `0xc0000106` → `+0xfb RtlAllocateHeap` → 2차 `+0x8f3 ~ +0x9a6` 칸마다 복사 + `;`.
- 이름이 비슷한 다른 창구 `RtlGetExePath` (`RtlpExeSearchPath`, `RtlpComputeExePath`)·`RtlGetSearchPath` (`RtlpSearchPath`, `RtlpComputeSearchPath`) 는 **같은 `RtlpGetCachedPath` 를 쓰지만 다른 목록**이다. 로더의 DLL 검색은 이 둘을 안 쓴다 `[정적: 호출 관계]`.

### 3. exe 폴더 = exe 항목 `FullDllName` 의 마지막 `\` 앞까지 `[정적]`

- 읽는 줄: `RtlpGetDirPath +0x28 MOV RAX,[LdrpImageEntry]` → `+0x2f MOV RBX,[RAX+0x50]` (`FullDllName.Buffer`).
  - 그 앞 `+0x1a ~ +0x26`: `GS:[0x18]` (TEB `NtTib.SubSystemTib`) 가 0 이 아니면 `+0x87` 로 가서 다른 칸 `[[SubSystemTib+8]+8]` 을 쓴다. 보통 Win32 프로세스는 0 일 것 `[추측]`.
- 값의 출처: `LdrpInitializeProcess +0x13aa MOVAPS XMM0,[RSP+0xf0]` → `+0x13b9 MOVDQU [RCX+0x48],XMM0` (`RCX` = exe 항목). `[RSP+0xf0]` 은 `ProcessParameters.ImagePathName` (sast/14). 곧 `FullDllName` 은 16바이트 구조체 사본이고 **글자 버퍼는 `ProcessParameters` 것을 그대로 가리킨다.** `RtlGetExePath`·`GetModuleFileName` 류가 아니다.
- 자르기: `+0x33 MOV EDX,0x5c` → `+0x3b wcschr` (첫 `\`) → `+0x50 wcsrchr` (**마지막** `\`) → `+0x55 CMP` / `+0x58 LEA RCX,[RAX+2]` / `+0x5c CMOVNZ` → `+0x65 SUB RCX,RBX` → `+0x6b MOV [RDI],RCX`.
  - 첫 `\` 와 마지막 `\` 가 다르면 길이 = 마지막 `\` 자리 − 시작 (끝 `\` 빠짐). 같으면 (`C:\test.exe` 처럼 루트) `\` 까지 포함.
  - 예: `C:\a\bin\test.exe` → `C:\a\bin`.
- 부르는 곳: `RtlpComputePath +0x555 RtlpGetDirPath(0, …)` (칸 5, 부모 경로 인자 없음). 1차 `+0x56c` 길이, 2차 `+0x590` 복사, `;` (`+0x4c8`).
- 끝 `\` 는 `LdrpSearchPath +0x202 ~ +0x211` 이 후보를 만들 때 붙인다 (끝 글자가 `\`·`/` 가 아니면).

### 4. 기본 목록 순서 `[정적]` (실제 문자열은 동적)

```
exe 폴더 ; <System32> ; <Windows>\system ; <Windows> ; . ; (PATH 값) ; (패키지: 보통 빈칸)
 [칸 5]    └──────── 칸 2 (RtlpSystemDirs 한 덩어리) ─────────┘  [칸 4] [칸 3]   [칸 8]
```

- SafeDllSearchMode 가 기본(1) 일 때 = 줄 1. 0 이면 `.` 이 둘째로 온다 (줄 0). SetDllDirectory 가 있으면 `.` 대신 그 폴더 (줄 3). **세 줄 모두 첫 칸은 exe 폴더.**
- `RtlpSystemDirs` 내용 (`LdrpInitializeProcess +0x1065 ~ +0x1161`, `refs`):
  - `<NtSystemRoot>\System32;` + `<Windows>\system;` + `<Windows>;`. 첫 조각은 `RtlpSystem32Dirs` 와 같은 버퍼 앞부분 (`+0x1092` 에서 끝 `\` 를 `;` 로, `+0x109e` 에서 `RtlpSystem32Dirs`·`LdrpSystem32` 에 저장).
  - `ProcessParameters.Flags & 0x20000000` (`+0x10b9 ~ +0x10c4`) 이면 System32 조각 뒤에 `…\forwarders\alt;` (`LdrpIncludeAlternateForwarders` 참일 때)·`…\forwarders;` 가 붙는다. `LdrpPolicyBits & 1` 이면 Windows 두 조각을 안 붙인다. 보통 어떤지는 동적.
  - `<Windows>` 문자열의 정확한 출처 줄은 안 봤다 `[추측: NtSystemRoot]`.
- **시스템 폴더 대체 모드** `[정적]`: `RtlpComputePath +0x21 / +0x2b / +0x32` 가 `LdrSystemDllInitBlock+0xa0` 비트 60~61 을 읽어 1 이면, 1차 고리 뒤 `+0x135 / +0x138 JNZ +0x671` 로 **목록 전체를 시스템 폴더 하나로** 바꾼다. 보통 0 일 것 `[추측]`.
- **`RtlpLookupSafeCurDirList` (`SearchPath +0x4f7`) 는 test.exe 길에 관여하지 않는다** `[정적]`. "지금 칸 == `P+8`(`.` 칸)" 일 때만 가는데 (`+0x1cc CMP RBX,[R12+8]` / `+0x1d4 SETZ SIL` → `+0x24b`·`+0x24e JNZ +0x4ef`), `P+8` 은 플래그 `0x2000` 일 때만 채워지고 우리 플래그는 0 이다.
  - 그 갈래가 가면 `…\Session Manager\Safe Load Prefixes` 키·`ProgramFiles` 등으로 "현재 폴더에서 받아도 되는 목록" 을 만들어, 목록에 없으면 결과를 버리고 다음 칸으로 간다 (`+0x52c ~ +0x573`).

### 5. `LdrpSearchPath` 는 처음 찾은 칸에서 멈추고, 끝까지 없으면 `0xc0000135` `[정적]`

- 시작 칸: `R13`(인자 `&B`, 이어하기 표시) 첫 칸이 0 이 아니면 `+0xfc JNZ +0x3ce` → **지난번 멈춘 칸 다음**부터. 아니면 `P+0x10`(패키지) 이 있으면 그것 (`+0x102`), 없으면 `P+0` (`+0x10c`).
- `+0x113 ~ +0x157` 가장 긴 칸 길이를 구해 `+0x172` 후보 버퍼를 한 번 잡는다.
- 칸 고리 (`+0x1b1`/`+0x1b6`): 빈 칸 건너뜀 (`+0x1c6`), `;` 까지 복사, 끝에 `\` 붙임 (`+0x202 ~ +0x211`), `+0x225` 이름 붙이기, **`+0x244 LdrpResolveDllName`**.
- 결과:
  - **0 이상이면 멈춘다** (`+0x256 JNS +0x282`). `0x4000000e` 같은 경고값도 성공 취급. 나가기 전 `+0x2b7`·`+0x2bb` 에서 이어하기 표시에 "이 목록 / 찾은 칸 다음" 을 적는다 → run26 의 `0x4000000e` (기계 종류 불일치) 재시도 때 **다음 칸부터** 찾는 장치.
  - `0xc0000135` (`+0x258`)·`0xc0000022` (`+0x4c9`)·`0xc0000043` (`+0x4d4`)·`0xc000006d` (`+0x4df`) 이면 다음 칸 (`+0x263` 버퍼 길이 0 → `+0x26d JMP +0x1b1`).
  - 그 밖 음수는 그 자리에서 그 오류로 끝 (`+0x4ea JMP +0x282`).
  - 목록 끝 (`+0x1b9`·`+0x1bc`): 패키지 목록을 막 끝냈고 `R8B` (`ctx.flags>>3 & 1`) 가 0 이면 `P+0` 목록으로 이어서, 아니면 **`+0x27d MOV EDI,0xc0000135`**.
- 파일 확인은 **`LdrpResolveDllName`** 이 맞다: 플래그 `0x200` 꺼짐 → `+0x298 LdrpGetFullPath` (전체 DOS 경로) → `+0x13b LdrpGetNtPathFromDosPath` → 그 안 **`+0xee NtQueryAttributesFile`**. "없음" 계열 상태 9개 (`0xc000000f`·`0xc0000034`·`0xc000003a` 등) 는 `+0x28b` 에서 `0xc0000135` 로 바뀐다.
- 이름에 `\` 가 있는지 보는 줄은 **`LdrpSearchPath` 안에 없다** (`0x5c` 비교는 `+0x202` 뿐). 경로가 있는 이름은 caller 쪽에서 다른 길로 간다 (sast/18 flags `0x400`, sast/19 `LdrpMapDllFullPath`) → `LdrpSearchPath` 는 "맨 이름" 일 때만 불린다 `[정적: 앞선 결과]`.
- `LdrpFindLoadedDllInternal +0x226`·`LdrpFindLoadedDll +0x2f5` 도 이 함수를 부른다 (이미 올라온 모듈 찾기 — 범위 밖, 있다는 것만).

### 6. dll1·dll2 — 첫 칸 (exe 폴더) 에서 찾힐 것, 세 모듈이 `P` 하나를 같이 쓴다

- `[정적]` 기본 목록 맨 앞은 SafeDllSearchMode 가 0 이든 1 이든, SetDllDirectory 가 있든 없든 **exe 폴더**다 (줄 0·1·3 첫 코드 5).
- `[소스]` `sample/build.bat` 가 dll2·dll1·test.exe 를 모두 `bin\` 한 폴더에 만든다.
- `[추측: 위 둘 + 실행 파일이 그 폴더에서 돈다고 칠 때]` 그래서 첫 후보 `<bin>\dll1.dll` 에서 `NtQueryAttributesFile` 이 성공 → `+0x256 JNS` 로 나간다. `+0x244` 는 DLL 하나당 **한 번**. dll2 도 같다. 순서가 갈리는 건 둘째 칸부터라 이번 sample 에선 상관없다.
- 어느 `P` 를 쓰나:
  - `[정적]` `LdrpMapDllSearchPath +0xb0 ~ +0xdb`: 부모 항목(`ctx+0x30`)의 `DependentLoadFlags`(`+0x118`) 가 마스크 `0x7b00` (`LdrpPolicyBits & 4` 면 `0x7f00`) 와 겹치면 `+0x330` 에서 새 `P` 를 만들고, 아니면 **`+0xe1 MOV R14,[RBX+0x10]`** = 문맥이 들고 온 `P`.
  - `[추측: 링크 옵션]` `build.bat` 에 `/DEPENDENTLOADFLAG` 가 없어 test.exe·dll1 항목 `+0x118` 은 0 일 것. (그 칸에 값을 넣는 줄은 이번에 못 찾음.)
  - `[정적]` **자식 문맥 `+0x10` = 부모 문맥 `+0x10`**: `LdrpLoadDependentModuleInternal +0x959 MOV RAX,[RAX+0x10]` (부모 문맥) → `+0x95d [RSP+0xf0]` → `+0x10f8 MOV [RBX+0x10],RAX` (새 문맥). 그래서 exe·dll1·dll2 문맥의 `+0x10` 은 모두 `LdrpInitializeProcess` 스택의 같은 `&P`.
  - 그러니 **목록 문자열은 한 번만 지어진다**: 처음 `LdrpSearchPath` 에서 `P+0` 이 채워지면 그 뒤 호출은 `+0xe4` 에서 건너뛴다 `[정적]`. 그 처음이 dll1 을 찾을 때인지 (`LdrpFindLoadedDll*` 쪽이 먼저인지) 는 동적.
- 인자 (`LdrpMapDllSearchPath`): `+0x116 MOV RCX,RBX` (ctx, `ctx+0` = DLL 이름 UNICODE_STRING), `+0x10e MOV RDX,R14` (`P`), `+0xe5 ~ +0x105 R8B = (ctx+0x20 >> 3) & 1`, `+0x12c CALL`.

### 7. 같이 보인 것 — 시스템 폴더 문자열을 쓰는 둘째 곳 `[정적: refs 줄]`

- 결과서의 `_refs/RtlpSystemDirs.tsv` 에 `LdrpInitializeProcess +0x1161` 말고 **`LdrpInitializePerUserWindowsDirectory +0x157` 도 `RtlpSystemDirs` 를 쓰는 줄**이 있다 (`+0x9d`·`+0x77`·`+0x142` 에서 읽기도 함). `_refs/RtlpSystem32Dirs.tsv` 에도 같은 함수 `+0x164` 의 버퍼 칸 쓰기가 있다.
- 그 함수는 `LdrpInitializeProcess +0x1b72` (ret `+0x1b77`) 에서 불린다 (`_calls.tsv`). 곧 dll1·dll2 올리기(`+0x1ab1 Drain`) **뒤**, 그래프 켜기(`+0x1bb6`) 앞이다.
- 결과서는 이 함수를 다루지 않았다. 무엇을 바꾸는지, test.exe 에서 타는지, 그 뒤 지은 목록(캐시)이 다시 지어지는지는 모른다 `[공백]`. 캐시를 새로 짓는 조건 (`RtlpGetCachedPath +0x54`·`+0x78`) 에 `RtlpSystemDirs` 는 없다 `[정적]`. 그래서 dll1·dll2 검색에는 영향이 없을 것이고, 뒤에 오는 `LoadLibraryExW` ×3 (run7) 검색에 영향이 있을 수 있다 `[추측]`.

## 정정

- 이전 run 은 틀린 곳이 없다.
  - run26·flow.md 2.6 의 "`LdrpInitializeProcess +0x1313 LdrpInitializeDllPath` 로 **만든** 경로" 는 "빈 표지를 만든" 이 정확하다 (목록은 나중). 틀린 말은 아니라 이전 문서는 안 고친다.
  - flow.md 의 `[추측]` "dll1·dll2 는 KnownDll 이 아니라 exe 폴더에서 `LdrpSearchPath` 로 찾음" 은 이번에 코드 쪽 근거가 붙었다 (실행은 그대로 `[추측]`).

## 아직 확인 필요

동적 (결과서 "동적분석으로 확인해야 할 것" 1~7 → 동적 **Q23** 에 출처로 붙임, 새 항목 없음). 모두 ntdll 유저 주소라 **test.exe 조건 필수**:

| 함수 + 오프셋 | `ntdll+rva` | 볼 것 |
|---|---|---|
| `LdrpInitializeProcess +0x1318` (ret) | `ntdll+0x8d71c` | `dq @rsp+0x380 L10` = `P` 가 전부 0 |
| `LdrpInitializeDllPath` 입구 | `ntdll+0x899a0` | `rcx`·`rdx`·`r8` = `(0, 0, &P)` (이 함수는 다른 caller 도 있어 여러 번 설 수 있음) |
| `LdrpSearchPath` 입구 | `ntdll+0x8eb0` | `dS @rcx` (이름), `dq @rdx L3` (`P+0/+8/+0x10`), `r8`, `dq @r9 L2` |
| `LdrpComputeLazyDllPath` 입구 | `ntdll+0xb9870` | `k` — **처음 지음을 일으킨 호출** (`LdrpSearchPath +0x372` 위가 `LdrpMapDllSearchPath` 인가 `LdrpFindLoadedDll*` 인가) |
| `LdrpComputeLazyDllPath +0x98` | `ntdll+0xb9908` | `du @rcx` = **지어진 폴더 목록**. 기대 `<exe 폴더>;<System32>;<Windows>\system;<Windows>;.;<PATH>;` |
| `RtlpComputeDllPath +0x5e` | `ntdll+0xba5be` | `ebx` = 줄 번호 (기대 1) |
| `RtlpComputePath` 입구 | `ntdll+0xba7a0` | `dd @rcx L5` = `5 2 4 3 8`, `edx` = 5, `r8` = 0 |
| `RtlpGetDirPath +0x6b` | `ntdll+0xb96a3` | `rcx` = 폴더 길이(바이트), `du @rax L(@rcx/2)` = exe 폴더 |
| `LdrpSearchPath +0x244` (call) | `ntdll+0x90f4` | `dS @rcx` = 이번 후보 |
| `LdrpSearchPath +0x249` (ret) | `ntdll+0x90f9` | `r eax` (0 = 찾음, `c0000135` = 다음 칸). **dll1·dll2 둘 다 한 번에 0** 기대 |
| `LdrpSearchPath +0x282` | `ntdll+0x9132` | 나가는 자리, `edi` |
| `LdrpGetNtPathFromDosPath +0xf3` (ret) | `ntdll+0xf0d3` | `eax` = `NtQueryAttributesFile` 상태 |
| `LdrpMapDllSearchPath +0xd8` 직전 | — | 부모 `[+0x118]` (0 이면 `P` 공유) — sast/21 과 같은 요청 |
| 전역 | `ntdll+0x1d3050` | `du poi(ntdll+0x1d3050)+0x80` (캐시 목록) |
| 전역 | `ntdll+0x1d2360` | `dS ntdll+0x1d2360` (`RtlpSystemDirs`). `LdrSystemDllInitBlock+0xa0` 비트 60~61 이 0 인가 |

정적 (새 공백 — 1~7 중 묶음은 새 **Q71** (정적, 정적 묶음 끝). Q71 에 든 것: 2·4·5·7, 나머지 1·3·6 은 번호 없이 남김):

1. `RtlpComputeDllPathWithOptions` (`0xba650`, 315B) 안쪽 — 플래그 갈래. test.exe 는 안 탈 것 `[추측]` (`P+0x18 = 0`).
2. `LdrSystemDllInitBlock+0xa0` 비트 60~61 의 뜻과 보통 값 (1 이면 목록이 시스템 폴더 하나로). ntdll PDB 에 타입 없음 → 커널 쪽 (run32 `PspPrepareSystemDllInitBlock` 이 `+0xa0` 을 씀 — 짝 표와 이어 볼 것).
3. `P+0x28 ~ +0x77` 사본을 읽는 곳 (`LdrpSaveLocationsSearched` 쪽일 것 `[추측]`).
4. 항목 `+0x118 DependentLoadFlags` 를 쓰는 줄 (sample 이 0 이라는 건 링크 옵션 근거뿐).
5. `<Windows>` 문자열(`LdrpInitializeProcess [RSP+0x120]`) 의 출처 줄, `ProcessParameters.Flags & 0x20000000` (forwarders 폴더) 이 보통 꺼져 있나 — run32 `PspSetupUserProcessAddressSpace` 비트 29 (`+0xe6`) 와 같은 비트인지.
6. 현재 폴더 칸 `.` 이 `LdrpGetFullPath → RtlGetFullPathName_Ustr` 에서 PEB `CurrentDirectory` 로 풀리나 (sample 은 첫 칸에서 끝나 영향 없음).
7. (이 문서가 refs 에서 찾음) `LdrpInitializePerUserWindowsDirectory` (`LdrpInitializeProcess +0x1b72`) 가 `RtlpSystemDirs`·`RtlpSystem32Dirs` 를 바꾸는 조건, test.exe 에서 타나, 그 뒤 캐시 목록이 다시 지어지나 (위 확인된 것 7).

## 디버거 메모 / 도구 메모

- 디버거 안 씀.
- 도구: 결과서 "스크립트 문제 없음". 점프 목적지를 스크립트로 한 번에 바꿔 적어서 run38 같은 "주소 끝 세 자리" 섞임은 없었다.
- 결과서 읽기 메모: 결과서 3번 답의 "`RtlpGetDirPath +0x48 wcsrchr`" 는 `.asm` 에서 `+0x48 MOV EDX,0x5c` (인자 준비) 이고 call 은 **`+0x50`** (ret `+0x55`) 이다. 이 문서는 `+0x50` 으로 적었다. 결론은 같다.
- 멈춤점 메모 (다음 dast 용, 제안): `LdrpSearchPath`·`LdrpResolveDllName` 쪽 주소는 `LoadLibraryExW` ×3 (run7) 때도 선다. dll1·dll2 몫만 보려면 `LdrpSearchPath` 입구 `dS @rcx` 로 이름을 같이 찍어 가려 읽는다. `LdrpComputeLazyDllPath` 는 `P` 를 공유하면 dll1·dll2 길에서는 **많아야 한 번** 서야 한다 (dll1·dll2 둘 다에서 서면 `P` 가 갈라진 것). `LoadLibraryExW` 쪽은 다른 경로 구조체일 수 있어 따로 셀 것 `[추측]`.

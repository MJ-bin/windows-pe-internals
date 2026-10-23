# 2026-10-23 run26 — DLL 을 파일로 올리는 길: `LdrpMapDllSearchPath` → `LdrpMapDllNtFileName` (`NtOpenFile` → `NtCreateSection` `SEC_IMAGE`) → `LdrpMapDllWithSectionHandle` (매핑 → **PEB.Ldr 명부 `+0x146`** → 색인 → `CompleteMap` → `ProcessMapped` → 마지막에 `MapAndSnap` 재귀) (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntdll 26100.9457, pdb GUID `C3093720177DA6851519DCFFE8DF53FE1` (flow.md 기준과 같음). 이미 analyze 된 프로젝트를 다시 썼다.
- 버전 대조 (결과서): `_calls.tsv` 의 `LdrpLoadDll` call `+0xfb` / ret `+0x100` → `LdrpLoadDllInternal` 이 run7 콜스택과 같다. 세 함수의 `_calls.tsv` 줄 수 (23 / 21 / 22) 와 `.asm` 의 `CALL ... ret=` 줄이 전부 같다.
- 이름: 세 함수 모두 `_index.tsv` 에 PDB 이름 그대로 있다.

| 함수 | rva | 크기 | 직접 호출 |
|---|---|---|---|
| `ntdll!LdrpMapDllSearchPath` | `0x8a50` | 1102바이트 | 23곳 |
| `ntdll!LdrpMapDllNtFileName` | `0xd210` | 1261바이트 | 21곳 |
| `ntdll!LdrpMapDllWithSectionHandle` | `0x9e50` | 967바이트 | 22곳 |

## 목적

- Q14 (2026-10-23 사용자 결정으로 좁힘): dll1·dll2 를 **파일로** 메모리에 올리는 길의 본문.
- run24 일감 한 바퀴 그림의 `LdrpProcessWork +0x6a` → `LdrpMapDllSearchPath` → **`?`** → 매핑 함수 칸을 채운다.
- dll1·dll2 가 PEB.Ldr 명부에 들어가는 순간이 어디인가.
- `LdrpMapDllWithSectionHandle` 안에서 "자기 import 펼치기(재귀)" 와 "마무리" 중 무엇이 먼저인가.

## 방법

- 정적: `_workspace/sast/21_map-dll-file.md` — ntdll, `^(LdrpMapDllSearchPath|LdrpMapDllNtFileName|LdrpMapDllWithSectionHandle)$` depth 1 (대상 3 + callee = 48개 함수). callee 본문은 안 읽음.
- 결과서: `_workspace/sast/21_map-dll-file_result.md` (**분석성공**)
- 근거 파일 (레포 밖, 커밋 안 함): `ms/disassemble/ntdll/{LdrpMapDllSearchPath,LdrpMapDllNtFileName,LdrpMapDllWithSectionHandle}.asm`, `ms/disassemble/ntdll/_calls.tsv`.
- 칸 이름은 PDB 타입(`llvm-pdbutil`)으로 맞췄다. 로드 문맥(`_LDRP_LOAD_CONTEXT`)은 PDB 에 칸 정보가 없어 오프셋 + 역할로만 쓴다.
- 요청서는 몇 곳에서 call 자리와 ret 자리를 섞어 적었다. 결과서가 `_calls.tsv` 로 바로잡았고, 이 문서는 결과서를 따른다 (아래 "도구 메모").

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`, 대표만)

잡일(로그·ETW·`RtlGetCurrentServiceSessionId`·`memset`·버퍼 놓기·쿠키)은 뺐다. 버퍼 놓기 4곳(`SearchPath +0x29e`·`+0x2c2`·`+0x3da`·`+0x42c`)은 `_calls.tsv` 에 `alias:` 로 나오는 함수다 — rva `0x3d630`, 같은 주소 이름 9개, 후보: `RtlpFtFree`, `RtlpCtFreeMemory`, `RtlDeleteBoundaryDescriptor`, `RtlpFreeAtom`, `IdnaMemFree`, `RtlpFreeEnvironmentHashTable`, `RtlpFreeEnvBlock`, `NtdllpFreeStringRoutine`, `RtlpSysVolFree` (`pdb_aliases.py --lookup ntdll.dll 0x3d630` 로 확인함).

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 (요약) | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpMapDllSearchPath` | `+0x330` | `+0x335` | `LdrpInitializeDllPath` | 부모 `DependentLoadFlags` 가 마스크와 겹칠 때만 | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x12c` | `+0x131` | `LdrpSearchPath` | 늘 (재시도 때 한 번 더) | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x3b4` | `+0x3b9` | 간접 (`g_pfnSE_LdrResolveDllName`, CFG) | shim 켜짐일 때만 | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x40d` | `+0x412` | `LdrpResolveDllName` | shim 켜짐 + shim 이 바이트 ≠ 0 | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x1b4` | `+0x1b9` | `LdrpHashUnicodeString` | 찾기 성공 뒤 | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x1d4` | `+0x1d9` | `RtlAcquireSRWLockExclusive` (`LdrpModuleDatatableLock`) | | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x1f8` | `+0x1fd` | `LdrpFindLoadedDllByNameLockHeld` | 락 안, 이미 있나 | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x355` | `+0x35a` | `LdrpFindLoadedDllByNameLockHeld` | 위가 못 찾음 + `ctx.flags & 8` 일 때 전체 경로로 한 번 더 | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x217` | `+0x21c` | `RtlReleaseSRWLockExclusive` | | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x257` | `+0x25c` | **`LdrpMapDllNtFileName`** | 명부에 없음 (`0xc0000135`) | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x30d` | `+0x312` | `LdrpLoadContextReplaceModule` | 이미 있는 항목을 찾았을 때 (매핑 안 함) | [정적] |
| `ntdll!LdrpMapDllSearchPath` | `+0x2d2` | `+0x2d7` | `LdrpReleaseDllPath` | 끝에서 늘 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x46` | `+0x4b` | `LdrpCheckForRetryLoading` | 늘. 참이면 `0xc000022d` 로 끝 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x109` | `+0x10e` | `NtSystemDebugControl` | `PEB.NtGlobalFlag & 0x40000` 일 때만 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x132` | `+0x137` | **`NtOpenFile`** | 늘 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x407` | `+0x40c` | `LdrpValidateIntegrityContinuity` | 전역 `LdrpAuditIntegrityContinuity ≠ 0` 일 때만 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x47e` | `+0x483` | `LdrpSetModuleSigningLevel` | `ctx.flags & 0x1000000` + 패키지 프로세스 아님 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x18f` | `+0x194` | **`NtCreateSection`** | 파일 열기 성공 뒤 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x201` | `+0x206` | `NtRaiseHardError` | `NtCreateSection` 실패(일부 코드 빼고)일 때만 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x494` | `+0x499` | `LdrAppxHandleIntegrityFailure` | `NtCreateSection` 이 `0xc000047e`/`0xc000047f`/`0xc0000462` 일 때만 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x2e5` | `+0x2ea` | `LdrpCodeAuthzCheckDllAllowed` | WOW64 아님 + `ctx.flags & 0x100 == 0` | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x287` | `+0x28c` | **`LdrpMapDllWithSectionHandle`** | 섹션 만들기 성공 뒤 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x29b` | `+0x2a0` | `NtClose` (섹션 핸들) | `ctx+0xb0 == 0` 일 때 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x2a4` | `+0x2a9` | `NtClose` (파일 핸들) | `ctx+0xb0 == 0` 일 때 | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x49` | `+0x4e` | **`LdrpMinimalMapModule`** | 늘 (지도: 그 안 `+0x179 NtMapViewOfSection`) | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x30e` | `+0x313` | `NtQueryVirtualMemory` | `E.LoadReason == 9` (패치 이미지) 만 | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0xa1` | `+0xa6` | `RtlImageNtHeaderEx` | | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0xd4` | `+0xd9` | `RtlAcquireSRWLockExclusive` (`LdrpModuleDatatableLock`) | `ctx.flags & 0x800000 == 0` | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0xfd` | `+0x102` | `LdrpFindLoadedDllByNameLockHeld` | 락 안 | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x12f` | `+0x134` | `LdrpFindLoadedDllByMappingLockHeld` | 이름으로 못 찾았을 때만 | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x141` | **`+0x146`** | **`LdrpInsertDataTableEntry`** | 이름·매핑 둘 다 못 찾았을 때 (락 안) | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x14c` | `+0x151` | `LdrpInsertModuleToIndexLockHeld` | 바로 위와 같이 | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x158` | `+0x15d` | `RtlReleaseSRWLockExclusive` | | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x3be` | `+0x3c3` | `LdrpLoadContextReplaceModule` | 이미 있는 모듈을 찾았을 때 → 끝 | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x24a` | `+0x24f` | **`LdrpCompleteMapModule`** | 새 모듈 | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x267` | `+0x26c` | **`LdrpProcessMappedModule`** | 앞이 성공 | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x27d` | `+0x282` | `LdrpLogNewDllLoad` | 앞이 성공 | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x1c3` | `+0x1c8` | **`LdrpMapAndSnapDependency`** | 코드 흐름상 **마지막**. `CorILOnly` 꺼짐 (아래) | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x2d0` | `+0x2d5` | `LdrpCorProcessImports` | `CorILOnly` 켜짐 (위 대신) | [정적] |

### 정적 — 먼저 알아 둘 칸

- PDB `_LDR_DATA_TABLE_ENTRY` (`E`): `+0x30 DllBase`, `+0x40 SizeOfImage`, `+0x48 FullDllName`, `+0x58 BaseDllName`, `+0x68 Flags` (비트 0 `PackagedBinary`, 2 `ImageDll`, 24 `CorILOnly`), `+0x80 TimeDateStamp`, `+0x98 DdagNode`, `+0xb0 LoadContext`, `+0xb8 ParentDllBase`, `+0x108 BaseNameHashValue`, `+0x10c LoadReason` (9 = `PatchImage`), `+0x118 DependentLoadFlags`, `+0x120 CheckSum`.
- 로드 문맥 `ctx` (PDB 에 칸 없음, 이번에 코드에서 읽은 역할. run22·23 표에 더함):

| 칸 | 역할 | 표시 |
|---|---|---|
| `+0x10` | **DLL 검색 경로 구조체 포인터.** `LdrpSearchPath` 의 둘째 인자. exe 문맥에는 `LdrpInitializeProcess +0x1313 LdrpInitializeDllPath(0,0,&경로)` 로 만든 것을 `+0x1320` 이 넣는다 (결과서) | [정적] |
| `+0x18` | 섹션 핸들 보관 칸 — `ctx+0xb0 ≠ 0` 이고 성공일 때만 (`NtFileName +0x43f`) | [정적] |
| `+0x20` | 플래그. 이번에 읽은 비트: `0x8`, `0x20`(맨 이름만 비교), `0x100`(켜지면 CodeAuthz 건너뜀), `0x800`(WOW64 섹션 속성), `0x10000`(shim 이 이름을 바꿈), `0x800000`(켜지면 명부에 안 넣음), `0x1000000`(서명 수준 설정). 이름은 모름 | [정적], 뜻 [추측] |
| `+0x28` | 결과 상태를 쓸 곳의 포인터 (`WithSectionHandle +0x1c8` 뒤에서 읽어 돌려줌) | [정적] |
| `+0x30` | 부모 항목 | [정적] run22 |
| `+0x38` | 이 문맥의 모듈 항목 `E` | [정적] run22 |
| `+0xa8` | `RtlImageNtHeaderEx` 의 크기 인자로 읽힘 → 맵 크기일 것 | [추측] |
| `+0xb0` | **뜻 모름.** `≠ 0` 이면 `SearchPath` 가 겹침 검사를 건너뛰고(`+0x158`), `NtFileName` 이 핸들을 안 닫고 문맥에 보관 | [정적: 쓰임], 뜻 [공백] |
| `+0xb8` | 파일 핸들 보관 칸 (`-1` 로 시작, run22). `ctx+0xb0 ≠ 0` 이고 성공일 때 `NtFileName +0x434` 가 씀 | [정적] |

### 정적 — `LdrpMapDllSearchPath` (인자 `ctx`, 반환 `NTSTATUS`)

1. 검색 경로 고르기: 부모 `DependentLoadFlags` 가 마스크 `0x7b00`(정책 비트 따라 `0x7f00`) 와 겹치면 `+0x335 LdrpInitializeDllPath` 로 새 경로, 아니면 **`ctx+0x10`** (프로세스 시작 때 만든 경로).
   - sample `build.bat` 에 `/DEPENDENTLOADFLAG` 가 없어 dll1·dll2 쪽은 `ctx+0x10` 을 쓸 것 `[추측: 링크 옵션 근거]`.
2. `+0x131 LdrpSearchPath` 로 파일을 찾는다. 돌려받는 문자열 셋: NT 경로 `A` (→ 다음 `NtFileName` 의 둘째 인자), 전체 이름 `C` (→ `E.FullDllName`), 기본 이름 `D` (→ `E.BaseDllName`, 해시해서 `E+0x108`). 결과 바이트가 켜지면 `E.Flags |= 1` (`PackagedBinary`).
   - 못 찾으면 (`0xc0000135`): shim 이 살려 주지 않는 한 **`NtFileName` 은 부르지도 않고** 오류로 돈다.
3. shim (`g_ShimsEnabled`/`g_ShimsLoading` ≠ 0) 일 때만 `+0x3b9` 간접 호출 → `+0x412 LdrpResolveDllName`. 꺼져 있으면 안 탄다.
4. 락(`LdrpModuleDatatableLock`) 안에서 `+0x1fd LdrpFindLoadedDllByNameLockHeld` 로 **이미 명부에 있나** 본다. 있으면 끝에서 `+0x312 LdrpLoadContextReplaceModule` (매핑 안 함).
   - 그 사이 다른 스레드가 같은 DLL 을 먼저 올렸을 때를 위한 검사라는 이유는 `[추측]` (코드엔 이유가 없다).
5. 없으면 `+0x25c LdrpMapDllNtFileName(ctx, &A)`.
6. 결과가 `0x4000000e` (이미지 기계 종류 불일치 — 이 값은 `LdrpMinimalMapModule` 이 돌려준다) 면 **`LdrpSearchPath` 부터 다시** 찾는다. 두 번째에 못 찾으면 `0xc000007b`.
7. 끝: `+0x2d7 LdrpReleaseDllPath` 는 늘.

- **exe 폴더가 검색 경로에 들어가는 자리는 이 세 함수 안에 없다.** `ctx+0x10` 을 만드는 `LdrpInitializeDllPath`, 실제로 폴더를 도는 `LdrpSearchPath` 몫이다 → 정적 Q53.

### 정적 — `LdrpMapDllNtFileName` (인자 `ctx`, NT 파일 이름)

1. `+0x4b LdrpCheckForRetryLoading(ctx, 0)` — 참이면 파일을 안 열고 `0xc000022d` 로 끝. 안의 조건은 안 읽음.
2. `OBJECT_ATTRIBUTES`: 이름 = 받은 NT 경로, `Attributes` = `0x840` (전역 `LdrpUseImpersonatedDeviceMap` 이 0 일 때) 또는 `0x40`. `0x40` = `OBJ_CASE_INSENSITIVE` `[추측: SDK 상수]`.
3. (`NtGlobalFlag & 0x40000` 일 때만) `+0x10e NtSystemDebugControl(0x26, 이름)`. 비트 이름은 gflags 의 디버거 심볼 적재 알림 쪽으로 알려진 값 `[추측: SDK 상수]`.
4. **`+0x137 NtOpenFile`**: 원하는 권한 `0x100021`, 공유 `5`, 옵션 `0x60` (숫자는 `.asm` 그대로. 읽기+실행+동기화 / 읽기·삭제 공유 / 파일만·동기 I/O 라는 이름은 `[추측: SDK 상수]`).
   - 실패가 `0xc0000034`·`0xc000003a` (이름·경로 없음) 면 **`0xc0000135` 로 바꿔** 돌려준다. `0xc0000022` (접근 거부) 면 조건부로 한 번만 다시 연다.
5. (`LdrpAuditIntegrityContinuity ≠ 0` 일 때만) `+0x40c LdrpValidateIntegrityContinuity`. (`ctx.flags & 0x1000000` + 패키지 프로세스 아님) `+0x483 LdrpSetModuleSigningLevel`.
6. **`+0x194 NtCreateSection`**: 원하는 권한 `0xd`, 페이지 보호 `0x10` (`PAGE_EXECUTE` `[추측: SDK 상수]`), 할당 속성 **`0x1000000`** (`SEC_IMAGE` `[추측: SDK 상수]`. WOW64 이고 `ctx.flags & 0x800` 꺼짐이면 `0x1100000`), 파일 = 방금 연 것.
   - 실패면: 몇 코드는 `+0x499 LdrAppxHandleIntegrityFailure`, 자원 부족류는 로그만, **그 밖은 `+0x206 NtRaiseHardError(0xc000007b, …)`** (사용자에게 띄우는 오류 창 쪽 `[추측]`) → 파일 핸들 닫고 반환.
7. (WOW64 아님 + `ctx.flags & 0x100 == 0`) `+0x2ea LdrpCodeAuthzCheckDllAllowed`. 막히면 핸들 둘 닫고 오류.
8. **`+0x28c LdrpMapDllWithSectionHandle(ctx, 섹션 핸들)`**.
9. 평소 (`ctx+0xb0 == 0`) **`+0x2a0 NtClose(섹션)` → `+0x2a9 NtClose(파일)`**. `ctx+0xb0 ≠ 0` 이고 성공이면 안 닫고 `ctx+0xb8`·`ctx+0x18` 에 맡긴다.
   - 핸들을 닫아도 이미 붙인 뷰는 남는다는 건 일반 지식 `[추측]` (이 코드의 근거 아님).

- test.exe 길에서 **안 탈 것** (정적 조건): `NtSystemDebugControl` (`NtGlobalFlag` 비트 없으면), `LdrpValidateIntegrityContinuity` (정책 바이트 0 이면), `LdrpSetModuleSigningLevel` (`ctx.flags` 비트 24 없으면), `NtRaiseHardError`·`LdrAppxHandleIntegrityFailure` (`NtCreateSection` 이 성공하면). 실제 값은 동적.
- **탈 것**: `LdrpCheckForRetryLoading` (부르는 건 늘, 결과는 모름), `NtOpenFile`, `NtCreateSection`, `LdrpCodeAuthzCheckDllAllowed` (`ctx.flags & 0x100` 이 0 이면).

### 정적 — `LdrpMapDllWithSectionHandle` (인자 `ctx`, 섹션 핸들)

주소 순서와 실행 순서가 다르다. `+0x1c0 ~ +0x1cc` 블록(`MapAndSnap`)은 주소로는 위에 있지만 `+0x2c7 JNC` 로 **맨 뒤에** 뛰어 들어온다 (`.asm` 160줄, 목적지 `0x18000a010 − 0x180009e50 = +0x1c0`).

1. **`+0x4e LdrpMinimalMapModule(ctx, 섹션)`** — 메모리에 붙는 곳 (지도: 안에 `+0x179 NtMapViewOfSection`, 본문 안 읽음).
   - `0x4000000e` 나 음수면 그대로 반환. 그 밖의 0 이상 값은 보관했다가 `LdrpCompleteMapModule` 셋째 인자로 넘긴다.
   - 바로 아래에서 `E.DllBase` 와 `ctx+0xa8` 을 읽는다. 이 둘을 `MinimalMap` 이 채운다는 건 `[추측]`.
2. `E.LoadReason == 9` (패치 이미지) 일 때만 `+0x313 NtQueryVirtualMemory` → `+0x331 LdrpResolvePatchDllName` → `+0x348` 해시. dll1·dll2 (정적 import) 는 안 탈 것 `[추측: LoadReason 0]`.
3. `+0xa6 RtlImageNtHeaderEx`.
4. `ctx.flags & 0x800000` 이 켜져 있으면 락·명부를 건너뛰고 헤더 값만 항목에 채운다.
5. **명부에 넣기 (락 안)**: `+0xd9` 락 → `+0x102` 이름으로 찾기 → (없으면) `+0x134` 매핑으로 찾기 → **둘 다 없으면 `+0x146 LdrpInsertDataTableEntry(E)` → `+0x151 LdrpInsertModuleToIndexLockHeld(E, NT헤더)`** → `+0x15d` 락 풀기.
6. 찾은 게 있었으면 `+0x3c3 LdrpLoadContextReplaceModule` 하고 **끝** (아래 7·8 안 함). 패치 이미지 짝이 안 맞으면 오류 `0xc00004c0`.
7. **새 모듈 마무리 (코드 순서)**:
   - 지금 스레드가 `LdrpDllNotificationLock` 주인이면 `0xc0000225` 로 끝 (DLL 알림 콜백 안에서 새 DLL 올리기를 막는 것 `[추측]`).
   - **`+0x24f LdrpCompleteMapModule(ctx, NT헤더, MinimalMap 결과)`** → 음수면 반환.
   - **`+0x26c LdrpProcessMappedModule(E, ctx.flags, 1)`** → 음수면 반환. 그 안에서 노드 `State = 2` + `LdrpSignalModuleMapped` (run24).
   - `+0x282 LdrpLogNewDllLoad` → 부모가 있으면 `E.ParentDllBase = 부모.DllBase` (`+0x286 ~ +0x28f`).
8. **그다음 마지막에 import 펼치기**:
   - `ctx.flags & 0x800000 == 0` **그리고** `E.Flags & 4 (ImageDll) == 0` 이면 펼치지 않고 노드 `State = 9` 만 쓰고 0 반환 (`+0x387 ~ +0x3b3`). (패치 이미지 + 부모가 exe 인 경우만 예외)
   - 아니면 `E.Flags` 비트 24 (`CorILOnly`) 가 꺼져 있으면 **`+0x1c8 LdrpMapAndSnapDependency(ctx)`** (재귀, 결과는 `*(ctx+0x28)`), 켜져 있으면 `+0x2d5 LdrpCorProcessImports`.

### 정적 — 재배치는 어느 호출 몫인가 (못 정함)

- 이 세 함수 안에 재배치(기준 주소가 바뀌었을 때 주소 고치기)를 하는 줄은 없다 `[정적]`.
- 후보 둘, 둘 다 `[추측]`:
  1. `LdrpCompleteMapModule` — `MinimalMap` 의 결과 코드를 셋째 인자로 받는다. 기준 주소가 바뀌면 매핑 쪽이 성공-경고 코드를 돌려줄 것이고, 그게 여기로 넘어간다.
  2. `LdrpProcessMappedModule` — 안에 `+0x24d LdrpIsExecutableRelocatedImage` 가 있다 (지도 이름).
- 정적 Q52 에서 본다. run8 `[관찰]` "dll 주소는 실행마다 바뀔 수 있다" 와 이어진다.

### 호출 관계

```
LdrpProcessWork  +0x6a  (노드 State 0, LoadReason ≠ 9, ctx+0x20 비트 20·9 꺼짐 — run24)
└─ LdrpMapDllSearchPath(ctx)                                        [정적] rva 0x8a50
   ├─ (부모 DependentLoadFlags 겹치면) +0x335 LdrpInitializeDllPath, 아니면 ctx+0x10
   ├─ (1) +0x131 LdrpSearchPath → A(NT 경로) · C(전체 이름) · D(기본 이름)
   │        못 찾음(0xc0000135) + shim 없음 → 오류로 끝
   ├─ [shim 켜짐만] +0x3b9 간접(g_pfnSE_LdrResolveDllName) → +0x412 LdrpResolveDllName
   ├─ (2) 락 안 +0x1fd LdrpFindLoadedDllByNameLockHeld   이미 있나
   │        찾음 → +0x312 LdrpLoadContextReplaceModule, 끝 (매핑 안 함)
   └─ (3) +0x25c LdrpMapDllNtFileName(ctx, A)                       [정적] rva 0xd210
        ├─ (1) +0x4b  LdrpCheckForRetryLoading     참이면 0xc000022d 로 끝
        ├─ (2) +0x137 NtOpenFile                   0x100021 · share 5 · options 0x60
        ├─ (3) +0x194 NtCreateSection              0xd · 0x10 · 0x1000000 (SEC_IMAGE)
        ├─ (4) +0x2ea LdrpCodeAuthzCheckDllAllowed (WOW64 아님 · ctx.flags & 0x100 == 0)
        ├─ (5) +0x28c LdrpMapDllWithSectionHandle(ctx, 섹션)        [정적] rva 0x9e50
        │     ├─ (1) +0x4e  LdrpMinimalMapModule       (지도: → +0x179 NtMapViewOfSection) 메모리에 붙음
        │     ├─ (2) +0xa6  RtlImageNtHeaderEx
        │     ├─ 락 안: +0x102 ByName → +0x134 ByMapping
        │     │     ├─ 이미 있음 → +0x3c3 LdrpLoadContextReplaceModule, 끝
        │     │     └─ 없음 → (3) +0x146 LdrpInsertDataTableEntry   ← PEB.Ldr 명부에 들어감
        │     │              → (4) +0x151 LdrpInsertModuleToIndexLockHeld   (색인)
        │     ├─ (5) +0x24f LdrpCompleteMapModule
        │     ├─ (6) +0x26c LdrpProcessMappedModule   (State = 2, +0x1e2 LdrpSignalModuleMapped — run24)
        │     ├─ (7) +0x282 LdrpLogNewDllLoad;  ParentDllBase 기록 (+0x28f)
        │     └─ (8) +0x1c8 LdrpMapAndSnapDependency(ctx)   ← 마지막. 자기 import 펼치기 (재귀)
        │              (ImageDll·0x800000 둘 다 꺼짐 → 안 하고 State = 9 / CorILOnly → +0x2d5 대신)
        ├─ (6) +0x2a0 NtClose(섹션)  ┐ ctx+0xb0 == 0 일 때
        └─ (7) +0x2a9 NtClose(파일)  ┘
   (NtFileName 결과 0x4000000e 면 +0x131 LdrpSearchPath 부터 다시)
```

## 확인된 것

- (`[정적]`) run24 일감 한 바퀴 그림의 `LdrpMapDllSearchPath → ? → 매핑 함수` 칸 = **`+0x25c LdrpMapDllNtFileName`** → (`+0x137 NtOpenFile` → `+0x194 NtCreateSection`) → **`+0x28c LdrpMapDllWithSectionHandle`** → **`+0x4e LdrpMinimalMapModule`** (그 안 `NtMapViewOfSection` 은 run21 지도). 지도 이름 사슬(run21)이 본문으로도 맞다.
- (`[정적]`) **dll1·dll2 가 PEB.Ldr 명부에 들어가는 순간 = `LdrpMapDllWithSectionHandle +0x146 LdrpInsertDataTableEntry`** (call `+0x141`). 매핑(`+0x4e`) 바로 뒤, 같은 이름·같은 매핑의 모듈이 이미 없고 `ctx.flags & 0x800000` 이 꺼진 새 DLL 일 때. 근거: run18 (sast/13 `refs PebLdr`) 에서 PEB.Ldr 적재·메모리 순서 목록(`+0x10`/`+0x20`)에 쓰는 함수가 `LdrpInsertDataTableEntry` 뿐이었다 (결과서). 그 호출자 4곳은 run20 (flow.md 2.5). 색인은 바로 뒤 `+0x151`.
  - 이게 정말 dll1·dll2 에서 불리는지는 `[추측]` (모듈 대응, dll1·dll2 가 이 파일 길을 탄다는 것) → 동적 Q23.
- (`[정적]`) **순서 = 매핑 → 명부·색인 → `CompleteMap` → `ProcessMapped`(State 2, Signal) → `LogNewDllLoad`·`ParentDllBase` → 마지막에 `MapAndSnap` 재귀.** 요청서가 가정한 "자기 import 를 먼저 펼치고 그다음 마무리" 와 **반대**. 그래서 자식이 Mapped 가 되어 부모 카운터를 줄이는 일(run24 `Signal`)은 자식의 import 를 펼치기 **전에** 일어난다.
  - run22·run24 문서·flow.md 의 기존 글과 그림은 이미 `+0x26c`(ProcessMapped) 가 `+0x1c8`(MapAndSnap) 보다 앞으로 적혀 있어 **어긋나는 곳이 없다** (아래 "정정").
- (`[정적]`) 파일 길과 KnownDll 길(`LdrpLoadKnownDll +0xf1`, run21)은 `LdrpMapDllWithSectionHandle` 에서 만난다. 이 세 함수 안에 KnownDll 갈림은 없다.
- (`[정적]`) **이미 올라온 모듈은 새로 매핑하지 않는다**: 두 군데 — `SearchPath +0x312` (파일 열기 전), `WithSectionHandle +0x3c3` (매핑 뒤 락 안) — 에서 `LdrpLoadContextReplaceModule` 로 바꿔치기.
- (`[정적]`) 평소 파일 핸들·섹션 핸들은 `WithSectionHandle` 이 돌아온 뒤 바로 닫는다 (`+0x2a0`, `+0x2a9`). 예외는 `ctx+0xb0 ≠ 0` (뜻 모름).
- (`[정적]`) 검색 경로는 `ctx+0x10` (프로세스 시작 때 만든 것) 이 기본이다. exe 폴더를 넣는 줄은 이 세 함수에 없다.

## 정정

- 이전 run 문서를 고칠 곳은 없다. run22 (`+0x1c8` 위주 그림)·run24 (`(1) +0x26c` → `(2) +0x1c8`) 는 이번 순서와 맞다.
- 요청서(sast/21) 4번 예상 순서 "`MapAndSnap` 재귀 → `CompleteMap` → `ProcessMapped`" 는 틀렸다 (요청서는 run 이 아니라 정정 한 줄을 안 단다).
- flow.md 2.5 의 "`LdrpMapDllWithSectionHandle +0x141` (`LdrpInsertDataTableEntry` 호출자)" 는 **call 자리**였다. 그림 라벨 규칙(ret)으로는 `+0x146`. flow.md 에 ret 를 같이 적었다.

## 아직 확인 필요

- **동적 (Q23 에 붙임 — dll1·dll2 가 올라오는 순간)**. 오프셋은 call 자리 (`ntdll + rva`), 이번 부팅 값 아님:
  - dll1·dll2 가 `LdrpMapDllSearchPath` 를 타나: 입구 `ntdll+0x8a50`, `LdrpSearchPath` call `ntdll+0x8b7c` (`rcx` = ctx, `ctx+0x10` 경로).
  - `NtOpenFile` 의 파일 이름: `NtFileName` 입구 `ntdll+0xd210` 에서 `du poi(@rdx+8)`, call `ntdll+0xd342`.
  - 섹션 속성 `0x1000000`: `NtCreateSection` call `ntdll+0xd39f` 에서 `dd @rsp+0x28 L1`.
  - 명부에 들어가는 순간·순서: `LdrpInsertDataTableEntry` call `ntdll+0x9f91` (dll 이름·주소를 `lm` 과 비교).
  - 순서 `CompleteMap` → `ProcessMapped` → `MapAndSnap`: call `ntdll+0xa09a` → `ntdll+0xa0b7` → `ntdll+0xa013` 가 같은 DLL 에서 이 순서로 서나.
  - `LdrpMinimalMapModule` 결과·재배치 여부: ret `ntdll+0x9e9e` 에서 `r eax`.
  - `ctx+0xb0` 값: `ntdll+0x8ba8` (`SearchPath +0x158`) 에서 `dq @rbx+0xb0 L1`.
  - 안 탈 갈림이 정말 안 타나: 정책 바이트 `ntdll+0x1d2918`(Audit)·`ntdll+0x1d2900`(Enforce), shim `ntdll+0x1d23cc`·`ntdll+0x1d23cf`.
- **동적 (Q44)**: `PEB.NtGlobalFlag & 0x40000` (`dd @$peb+0xbc L1`) — 켜져 있으면 `NtSystemDebugControl` 을 탄다.
- **정적 Q52**: `LdrpMinimalMapModule` (기준 주소·크기 칸을 누가 채우나, 결과 코드 종류, `NtMapViewOfSection` 인자), `LdrpCompleteMapModule` (재배치 담당인가, 셋째 인자 쓰임).
- **정적 Q53**: `LdrpSearchPath`·`LdrpInitializeDllPath` — exe 폴더가 검색 경로에 들어가는 자리, 후보 순서.
- 뜻을 못 정한 것: `ctx+0xb0`, `ctx+0x20` 비트 `0x8`·`0x100`·`0x800000`·`0x1000000` 의 이름, `0xc000022d` 의 이름, `LdrpCheckForRetryLoading` 안의 조건.

## 디버거 메모 / 도구 메모

- 요청서(sast/21)가 지도(`_map/`)의 **ret** 오프셋과 `.asm` 의 **call** 오프셋을 섞어 적었다 (예: `+0x3b4` 와 `+0x3b9`, `+0x141` 과 `+0x146`). 결과서가 `_calls.tsv` 로 둘을 나눠 적었고, 이 문서와 flow.md 는 결과서를 따랐다 (그림 라벨 = ret).
- `alias:RtlDeleteBoundaryDescriptor+9` (버퍼 놓기) 는 Ghidra 가 고른 이름을 쓰지 않고 "버퍼 놓기" 로 적었다. 별칭 표 `ms/sym/ntdll.dll.aliases.tsv` 로 확인함: `pdb_aliases.py --lookup ntdll.dll 0x3d630` → 같은 주소 이름 9개 (`.asm` 주석의 `ALIASES(9)` 와 개수 일치). 모듈 인자는 `ntdll` 이 아니라 `ntdll.dll` 로 줘야 한다.

# 21 map-dll-file — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run26)
- 대기열: Q14 (`_workspace/plan.md`) — 2026-10-23 사용자 결정으로 "dll1·dll2 를 파일로 올리는 길의 본문" 으로 좁힌 항목
- 관련: flow.md 2.6 (이야기 그림 — `LdrpMapDllSearchPath` → `?` → 매핑 함수 칸), 2.5 "올리는 순간" 절 (run21 이름 사슬), 4장 2.6 / 이전 요청 sast/19 (run24, `LdrpProcessWork +0x6a`), sast/16 (run21, 지도 사슬)

## 범위

- 대상 세 함수, depth 1: `LdrpMapDllSearchPath`, `LdrpMapDllNtFileName`, `LdrpMapDllWithSectionHandle`.
- callee 본문은 읽지 않는다. 특히 `LdrpSearchPath`, `LdrpMinimalMapModule`(`NtMapViewOfSection` 을 부르는 곳), `LdrpCompleteMapModule`, `LdrpProcessMappedModule`(→ `LdrpSignalModuleMapped`, run24), `LdrpMapAndSnapDependency`(run22) 는 이름·인자·결과만.
- 잡일(로그·ETW·`RtlGetCurrentServiceSessionId`·`memset`·버퍼 놓기·쿠키)은 개수만.
- 오프셋은 call·ret 둘 다. 점프 목적지는 `절대 주소 − 함수 시작`. 다 쓴 뒤 `.asm` 과 다시 대조.
- 구조체 칸은 PDB 타입으로. `_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없다 — 오프셋+역할로.

## 알고 싶은 것

1. **`LdrpMapDllSearchPath` (23곳)**: 순서와 갈림.
   - `+0x12c LdrpSearchPath` 에 넘기는 것 (DLL 이름, 검색 경로 — `LdrpInitializeDllPath` 가 만든 것인가) 과 결과 (찾은 전체 경로를 어디에 받나).
   - `+0x1f8 LdrpFindLoadedDllByNameLockHeld` (락 안): 찾는 사이에 **다른 스레드가 같은 DLL 을 먼저 올렸는지** 다시 보는 것인가 `[추측]`. 찾으면 어디로 (`+0x30d LdrpLoadContextReplaceModule`?).
   - 못 찾으면 `+0x25c LdrpMapDllNtFileName` 으로 (call/ret). 넘기는 인자.
   - `+0x3b4` 간접 호출(`cfg:?`) 의 출처와 조건, `+0x40d LdrpResolveDllName` 의 조건.
2. **`LdrpMapDllNtFileName` (21곳)**: 파일 → 섹션 → 매핑.
   - `+0x46 LdrpCheckForRetryLoading` 조건.
   - `+0x132 NtOpenFile` (ret `+0x137`): 무엇을 여나 (경로·권한 플래그).
   - `+0x18f NtCreateSection` (ret `+0x194`): 섹션 속성 (`SEC_IMAGE` 류 값 그대로, 이름은 `[추측]` 가능).
   - **`+0x287 LdrpMapDllWithSectionHandle` (ret `+0x28c`)** 에 넘기는 것.
   - 그 뒤 `NtClose` 둘 (`+0x2a0`, `+0x2a9`) — 파일 핸들·섹션 핸들을 닫는가.
   - 서명·무결성 쪽 (`+0x2e5 LdrpCodeAuthzCheckDllAllowed`, `+0x407 LdrpValidateIntegrityContinuity`, `+0x47e LdrpSetModuleSigningLevel`, `+0x494 LdrAppxHandleIntegrityFailure`) 과 `+0x109 NtSystemDebugControl`, `+0x201 NtRaiseHardError` 는 **언제 타나** (test.exe 길에서 안 탈 것인지 정적 조건까지).
3. **`LdrpMapDllWithSectionHandle` (22곳)**: 매핑 뒤 명부에 넣기까지의 순서.
   - `+0x49 LdrpMinimalMapModule` (ret `+0x4e`) — 여기서 메모리에 붙는다 (run21 지도: 그 안 `+0x179 NtMapViewOfSection`). 결과로 받는 것 (기준 주소·크기 칸).
   - 락 안 (`+0xd4 ~ +0x158`): `LdrpFindLoadedDllByNameLockHeld`(`+0xfd`)·`LdrpFindLoadedDllByMappingLockHeld`(`+0x12f`) 로 **이미 있나 한 번 더** 본 뒤 `+0x141 LdrpInsertDataTableEntry`(PEB.Ldr 목록) · `+0x14c LdrpInsertModuleToIndexLockHeld`(색인). → **dll1·dll2 가 PEB.Ldr 명부에 들어가는 순간이 여기인가** (예/아니오).
   - **`+0x1c3 LdrpMapAndSnapDependency` (ret `+0x1c8`)** 를 부르는 조건 (run22: `CorILOnly` 꺼진 새 모듈). 그다음 `+0x24a LdrpCompleteMapModule`·`+0x267 LdrpProcessMappedModule` 순서 — **자기 import 를 먼저 펼치고(재귀) 그다음 마무리** 인가, 반대인가. 코드 순서와 갈림을 정확히.
   - 재배치(기준 주소가 바뀌었을 때 주소 고치기)는 어느 호출 몫인가 (`LdrpCompleteMapModule`?, `LdrpProcessMappedModule` 안 `LdrpIsExecutableRelocatedImage`?) — 이름·인자로 `[추측]` 까지. (run8: dll 주소는 실행마다 바뀜 `[관찰]`)
   - `+0x30e NtQueryVirtualMemory`, 패치 쪽(`LdrpResolvePatchDllName`), `LdrpLoadContextReplaceModule` 의 조건.
4. **한 바퀴 그림 잇기**: run24 일감 한 바퀴 그림의 `LdrpProcessWork +0x6a` → `LdrpMapDllSearchPath` → **`?`** → 매핑 함수 칸을 이번 결과로 채운다: `LdrpProcessWork` → `LdrpMapDllSearchPath` → `LdrpMapDllNtFileName` (`NtOpenFile` → `NtCreateSection`) → `LdrpMapDllWithSectionHandle` (`LdrpMinimalMapModule` → 명부·색인 → `LdrpMapAndSnapDependency` 재귀 → `LdrpCompleteMapModule` → `LdrpProcessMappedModule` → `LdrpSignalModuleMapped`). 순서가 다르면 코드대로.
5. **dll1·dll2 길 확인 (정적 조건까지)**: dll1·dll2 는 KnownDll 이 아니라 이 파일 길을 탈 것 `[추측]`. `sample/` 빌드에서 dll1·dll2 는 test.exe 와 같은 폴더 (소스로 확인). 검색 경로에 exe 폴더가 들어가는 자리가 이 세 함수 안에 보이나 (안 보이면 "`LdrpSearchPath` 몫" 으로).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpMapDllSearchPath$` | 1 | 질문 1·5 |
| ntdll | `^LdrpMapDllNtFileName$` | 1 | 질문 2 |
| ntdll | `^LdrpMapDllWithSectionHandle$` | 1 | 질문 3·4 (`.asm` 은 sast/17 에서 depth 0 으로 뽑았음 — depth 1 로 다시) |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`, 오프셋은 ret)

- `LdrpMapDllSearchPath` rva `0x8a50`, 1102바이트, 깊이 7, caller `LdrpProcessWork +0x6a`. 호출 23: `+0x131 LdrpSearchPath`, `+0x1b9 LdrpHashUnicodeString`, 락 짝 `+0x1d9`/`+0x21c`, `+0x1fd LdrpFindLoadedDllByNameLockHeld`, **`+0x25c LdrpMapDllNtFileName`**, `+0x312 LdrpLoadContextReplaceModule`, `+0x335 LdrpInitializeDllPath`, `+0x35a LdrpFindLoadedDllByNameLockHeld`, `+0x379` 락 놓기, `+0x3b9 cfg:?`, `+0x412 LdrpResolveDllName`, `+0x2d7 LdrpReleaseDllPath`, 잡일(memset 2, 버퍼 놓기 4, ETW 2, 쿠키 1, `LdrpFreeUnicodeString` 1).
- `LdrpMapDllNtFileName` rva `0xd210`, 1261바이트, 깊이 8, caller 3 (`LdrpMapDllSearchPath +0x25c`, `LdrpMapDllFullPath +0xf5`, `LdrpMapDllRetry +0xb2`). 호출 21: `+0x4b LdrpCheckForRetryLoading`, `+0x10e NtSystemDebugControl`, **`+0x137 NtOpenFile`**, **`+0x194 NtCreateSection`**, `+0x206 NtRaiseHardError`, **`+0x28c LdrpMapDllWithSectionHandle`**, `+0x2a0`/`+0x2a9 NtClose`, `+0x2ea LdrpCodeAuthzCheckDllAllowed`, `+0x40c LdrpValidateIntegrityContinuity`, `+0x483 LdrpSetModuleSigningLevel`, `+0x499 LdrAppxHandleIntegrityFailure`, `+0x4dc LdrpCheckComponentOnDemandEtwEvent`, 잡일(로그·ETW·세션 ID 8).
- `LdrpMapDllWithSectionHandle` rva `0x9e50`, 967바이트, 깊이 8, caller 3 (`LdrpMapDllPatchImage +0x81`, `LdrpLoadKnownDll +0xf1`, `LdrpMapDllNtFileName +0x28c`). 호출 22: `+0x4e LdrpMinimalMapModule`, `+0xa6 RtlImageNtHeaderEx`, 락 짝 `+0xd9`/`+0x15d`, `+0x102 LdrpFindLoadedDllByNameLockHeld`, `+0x134 LdrpFindLoadedDllByMappingLockHeld`, **`+0x146 LdrpInsertDataTableEntry`**, `+0x151 LdrpInsertModuleToIndexLockHeld`, `+0x1b6 LdrpLogEtwHotPatchStatus`, `+0x1be LdrpDereferenceModule`, **`+0x1c8 LdrpMapAndSnapDependency`**, **`+0x24f LdrpCompleteMapModule`**, **`+0x26c LdrpProcessMappedModule`**, `+0x282 LdrpLogNewDllLoad`, `+0x2d5 LdrpCorProcessImports`, `+0x313 NtQueryVirtualMemory`, `+0x331 LdrpResolvePatchDllName`, `+0x348 LdrpHashUnicodeString`, `+0x3a2 LdrpLogDllState`, `+0x3c3 LdrpLoadContextReplaceModule`, 쿠키.
- 참고 (이번엔 이름만): `LdrpMinimalMapModule` (0xa220, 호출 13 — `+0x179 NtMapViewOfSection`, `+0x371 NtMapViewOfSectionEx`, `+0x2d9 NtUnmapViewOfSection`, `+0x304 LdrpProcessMachineMismatch`), `LdrpProcessMappedModule` (0xa974, 호출 13 — `+0x10c LdrInitSecurityCookie`, `+0x14a LdrpCfgProcessLoadConfig`, `+0x17c RtlInsertInvertedFunctionTable`, `+0x1e2 LdrpSignalModuleMapped`, `+0x24d LdrpIsExecutableRelocatedImage`).

## 이미 아는 것

- `[정적]` (sast/19·run24) `LdrpProcessWork`: 노드 `State == 0` 이면 올리기 — `LoadReason == 9` 면 `LdrpMapDllPatchImage`, `ctx+0x20` 비트 20 이면 `LdrpMapDllRetry`, 비트 9 면 `LdrpMapDllFullPath`, 나머지는 **`+0x6a LdrpMapDllSearchPath`**. `LdrpSignalModuleMapped` 는 `LdrpProcessMappedModule +0x1e2` 에서 `State = 2` 를 쓴 직후 (락 안).
- `[정적]` (sast/17·run22) `LdrpMapDllWithSectionHandle +0x1c8` 에서 `LdrpMapAndSnapDependency` 가 다시 불린다 (`CorILOnly` 꺼진 새 모듈만). `+0x28f` 근처에서 `ParentDllBase` 기록.
- `[정적]` (sast/18·run23) `LdrpLoadDependentModuleInternal`: KnownDll(`+0x1792`) 을 먼저 시도, 아니면 `+0x182c LdrpQueueWork` (경로 직접 지정 아님) — dll1·dll2 는 이 일감으로 `LdrpProcessWork` 에 온다 `[추측]`.
- `[정적]` (sast/16·run21) KnownDll 길은 `LdrpLoadKnownDll +0xf1` 에서 같은 `LdrpMapDllWithSectionHandle` 로 온다 (`NtOpenSection` 으로 섹션을 열고, 파일은 안 연다). 파일 길과 여기서 만난다.
- `[관찰]` (run8) dll1·dll2 는 `LdrpInitializeProcess +0x1bb6`(call) 전에 이미 올라와 있었고, 주소는 실행마다 바뀔 수 있었다.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄, call·ret 둘 다). 태그 `[정적]` / `[추측]`. 라벨은 ret.
- 세 함수 순서를 **이어진 트리 하나** (`LdrpProcessWork +0x6a` 에서 출발해 `LdrpSignalModuleMapped` 까지, 갈림은 짧게).
- run24 일감 한 바퀴 그림 / run22·23 이야기 그림에서 이번에 채운 `?` 칸 목록.
- 그림 2 `(정적)` 상자 후보 2 ~ 4개 (이유 한 줄씩).
- 동적으로 확인할 것 + 멈춤점 (`ntdll+rva`, 짧게). 이미 대기 중인 Q20·Q23·Q47 과 겹치면 그 번호를.
- 다음 정적 후보 (예: `LdrpMinimalMapModule`, `LdrpCompleteMapModule`, `LdrpSearchPath`) 를 따로 볼 가치가 있는지 한 줄씩.
- 스크립트 문제는 "스크립트 문제" 절에.
